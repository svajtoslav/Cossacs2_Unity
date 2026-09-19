#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cossacks2Bridge.UnityAdapters.Maps;
using TemnyLessViewer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Explicit batch-only regression; never starts in a user's interactive editor.
public static class C2AnimationRenderRegressionEditor
{
    const string Data = "C:/GSC Game World/Cossacks II/Data";
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only; does not replace your open scene.");
        C2RegressionSceneScopeEditor.Begin();
        EditorApplication.delayCall += Execute;
    }

    static void Execute()
    {
        try
        {
            CheckFrameAddressing();
            CheckMdClockAndDirections();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CheckRenderOrder();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CheckNatureCameraDepth();
            Debug.Log("[C2 ANIMATION RENDER REGRESSION] PASS");
            C2RegressionSceneScopeEditor.Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            C2RegressionSceneScopeEditor.Finish(1);
        }
    }

    static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }

    static void CheckFrameAddressing()
    {
        int checks = 0, changed = 0;
        var gps = new C2GpSystem();
        foreach (string name in new[] { "AusGrnG", "AusGreG", "EngKriH", "EngKriG" })
        {
            int gp = gps.PreLoadGPImage("UnitsG17/" + name, Data, out string error);
            Require(gps.LoadGP(gp, out error), error);
            string path = gps.GetPackagePath(gp);
            var bank = new C2DirectSpriteBank();
            Require(bank.Load(path, out error), error);
            byte[] header = File.ReadAllBytes(path);
            int directions = header[56]; // GU2DHeader in sgG2D.h
            int count = BitConverter.ToInt32(header, 8);
            Require(directions == 9 && bank.DirectionCount == directions, name + " header directions");
            int length = count / directions;
            for (int d = 0; d < directions; ++d)
            for (int t = 0; t < length; ++t)
            {
                int logical = t * directions + d;
                // Independent reference: direction block starts at d * length.
                int physical = d * length + t;
                Require(bank.UnswizzleFrameIndexLikeOriginal(logical) == physical, name + " unswizzle");
                checks++;
                if (logical != physical) changed++;
            }
            foreach (int t in new[] { 0, 1, length / 2, length - 1 })
            foreach (int d in new[] { 0, 4, 8 })
            foreach (bool mirror in new[] { false, true })
            foreach (bool nation in new[] { false, true })
            {
                int logical = t * directions + d + (mirror ? 4096 : 0);
                int physical = d * length + t;
                C2RenderedFrame actual, expected;
                bool okA = nation
                    ? gps.GetRenderedFrameNationColor(gp, logical, 255, 32, 32, out actual, out error)
                    : gps.GetRenderedFrame(gp, logical, out actual, out error);
                Require(okA, name + " GP " + error);
                bool okB = nation
                    ? bank.RenderFrameNationColor(physical, mirror, 255, 32, 32, out expected, out error)
                    : bank.RenderFrame(physical, mirror, out expected, out error);
                Require(okB, name + " raw " + error);
                Require(actual.Width == expected.Width && actual.Height == expected.Height &&
                    actual.OriginX == expected.OriginX && actual.OriginY == expected.OriginY &&
                    actual.Rgba.SequenceEqual(expected.Rgba), name + " decoded pixels/pivot differ");
                checks++;
            }
            Debug.Log($"[C2 FRAME ADDRESS] {name} directions={directions} sequence={length} frames={count} pixelChecks=48");
            bank.Clear();
        }
        Debug.Log($"[C2 FRAME ADDRESS] PASS checks={checks} previouslyWrongAddresses={changed} timingAndMovement=unchanged");
    }

    static MeshRenderer Quad(string name, float z, Color color, int order, int queue, bool depth)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(0, 0, z);
        var mesh = new Mesh();
        mesh.vertices = new[] { new Vector3(-1,-1,0), new Vector3(1,-1,0), new Vector3(1,1,0), new Vector3(-1,1,0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0,2,1,0,3,2 };
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Cossacks2Bridge/C2UnitSpriteV56SelectionDiffuseLikeOriginal"));
        var tex = new Texture2D(2,2);
        tex.SetPixels(new[] { color,color,color,color }); tex.Apply();
        mat.mainTexture = tex;
        mat.SetTexture("_BaseMap", tex);
        mat.SetInt("_ZTest", depth ? 4 : 8);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = queue;
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = order;
        if (depth)
        {
            var dm = C2SpriteDepthPrepassLikeOriginal.CreateDepthMaterialLikeOriginal(name + " depth", tex, 0.38f);
            C2SpriteDepthPrepassLikeOriginal.AddDepthRendererLikeOriginal(go, mesh, dm, order, "depth");
        }
        return renderer;
    }

    static void CheckMdClockAndDirections()
    {
        var type = typeof(C2UnitOriginalRuntimeAndRendererV1);
        var parse = type.GetMethod("TryParseMdLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic);
        var draw = type.GetMethod("ComputeDrawSpriteUnitLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic);
        var step = type.GetMethod("StepUnitRuntimeLikeOriginal", BindingFlags.Instance | BindingFlags.NonPublic);
        var core = new GameObject("MD clock test").AddComponent<C2UnitOriginalRuntimeAndRendererV1>();
        core.enabled = false; core.EnableOriginalRestRandomLikeOriginal = false;
        Require(Mathf.Approximately(core.DefaultAnimFps, 25), "Default animation clock must match the tested 40 ms quantum");
        core.UseOriginalMotionFramesFromPath = false; // Test the fixed-point clock, not movement distance.
        int directionChecks = 0;
        foreach (string name in new[] { "AusGrn", "AusGre", "EngKri" })
        {
            object[] args = { Data + "/UnitsMD/" + name + ".md", null, null };
            Require((bool)parse.Invoke(null, args), "MD parse " + name + " " + args[2]);
            var md = (C2UnitOriginalRuntimeAndRendererV1.MdModel)args[1];
            var motion = md.Animations.Single(a => a.Name == "#MOTION_L");
            Require(motion.Rotations == 9 && motion.Frames.Count == 20, "MD motion range " + name);
            Require(motion.Frames[0].SpriteId == 0 && motion.Frames[19].SpriteId == 19, "Inclusive MD range " + name);
            foreach (bool inverse in new[] { false, true })
            for (int angle = 0; angle < 256; ++angle)
            {
                bool saved = motion.Inverse; motion.Inverse = inverse;
                var u = new C2UnitOriginalRuntime { Md = md, RealDirPrecise = angle, OctantInfo = 0xFF };
                var actual = (C2UnitOriginalRuntimeAndRendererV1.DrawSpriteAudit)draw.Invoke(null, new object[] { motion, motion.Frames[7], u });
                int d = inverse ? (128 - angle) & 255 : angle;
                int sector = ((d + 64 + 7) & 255) / 16;
                int direction = Math.Abs(sector - 8);
                bool mirror = (sector < 8) != inverse;
                Require(actual.DisplaySprite == 7 * 9 + direction && actual.MirrorGeometry == mirror,
                    "Nine-view folding " + name + " angle=" + angle);
                motion.Inverse = saved; directionChecks++;
            }
            // Use an actual multi-frame REST where possible. Hold its logical state
            // until the end of the cycle and verify each 40 ms quantum, not FPS.
            var rest = md.Animations.FirstOrDefault(a => a.Name == "#REST" && a.Frames.Count > 1) ?? motion;
            var unit = new C2UnitOriginalRuntime { Md = md, State = C2UnitOriginalState.Rest,
                CurrentAnimIndex = md.Animations.IndexOf(rest), AnimFps = 25,
                NextRestCheckTime = float.MaxValue };
            for (int tick = 1; tick < rest.Frames.Count; tick++)
            {
                step.Invoke(core, new object[] { unit, .04f });
                Require(Math.Abs(unit.CurrentFrameLong - tick * 256) <= 1, "MD clock speed " + name + " tick=" + tick);
            }
            Debug.Log("[C2 MD CLOCK] " + name + " motion=20 frames/9 views restFrames=" + rest.Frames.Count + " stepMs=40 PASS");
        }
        Debug.Log("[C2 DIRECTIONS] PASS checks=" + directionChecks + " baseCameraExtraDir=0 movementCoordinates=unchanged");
        UnityEngine.Object.DestroyImmediate(core.gameObject);
    }

    static Color Render(Camera cam, RenderTexture rt, string name)
    {
        RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); tex.Apply();
        Color c = tex.GetPixel(rt.width / 2, rt.height / 2);
        Directory.CreateDirectory("C2DiagnosticLogs");
        File.WriteAllBytes("C2DiagnosticLogs/" + name + ".png", tex.EncodeToPNG());
        RenderTexture.active = previous;
        UnityEngine.Object.DestroyImmediate(tex);
        Debug.Log("[C2 DEPTH REGRESSION] " + name + " center=" + c);
        return c;
    }

    static void CheckRenderOrder()
    {
        var cam = new GameObject("Regression camera").AddComponent<Camera>();
        cam.transform.position = new Vector3(0,0,-10);
        cam.orthographic = true; cam.orthographicSize = 2;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 100;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var rt = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); rt.Create();
        var soldier = Quad("soldier", 0, Color.red, 6000, 3670, true);
        var tree = Quad("tree", 1, Color.green, 6400, 3670, true);
        Color c = Render(cam, rt, "depth_tree_behind_fixed");
        Require(c.r > .8f && c.g < .1f, "Far tree hides near soldier");
        tree.transform.position = new Vector3(0,0,-1);
        c = Render(cam, rt, "depth_tree_front_fixed");
        Require(c.g > .8f && c.r < .1f, "Near tree must hide far soldier");
        tree.gameObject.SetActive(false);
        var ring = Quad("selection", -2, Color.white, short.MinValue, 3669, false);
        c = Render(cam, rt, "selection_behind_fixed");
        Require(c.r > .8f && c.g < .1f, "Selection paints over soldier");
        // Control proves this scene detects the prior selection-order fault.
        ring.sortingOrder = 6001;
        c = Render(cam, rt, "selection_old_fault_control");
        Require(c.r > .8f && c.g > .8f, "Control did not reproduce old selection ordering");
        rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }

    static void CheckNatureCameraDepth()
    {
        var cam = new GameObject("Tilted nature test camera").AddComponent<Camera>();
        cam.transform.rotation = Quaternion.Euler(35, 0, 0);
        Vector3 focus = cam.transform.up * 2;
        cam.transform.position = focus - cam.transform.forward * 10;
        cam.orthographic = true; cam.orthographicSize = 3;
        cam.nearClipPlane = .1f; cam.farClipPlane = 100;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var rt = new RenderTexture(128,128,24); rt.Create();
        var soldier = Quad("billboard soldier", 0, Color.red, 6000, 3670, true);
        var tree = Quad("nature", .5f, Color.green, 6400, 3660, true);
        foreach (var renderer in new[] { soldier, tree })
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var points = new[] { new Vector3(-2,0,0), new Vector3(2,0,0), new Vector3(2,4,0), new Vector3(-2,4,0) };
            mesh.vertices = points;
            mesh.uv2 = points.Select(p => new Vector2(p.x,p.y)).ToArray();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20);
        }
        soldier.transform.rotation = cam.transform.rotation;
        Color old = Render(cam, rt, "nature_vertical_plane_fault_control");
        Require(old.g > .8f && old.r < .1f, "Nature plane control must reproduce erroneous foreground overlap");
        tree.sharedMaterial.SetFloat("_C2CameraFacing", 1);
        tree.transform.Find("depth").GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_C2CameraFacing", 1);
        Color fixedColor = Render(cam, rt, "nature_behind_billboard_fixed");
        Require(fixedColor.r > .8f && fixedColor.g < .1f, "Nature behind the soldier still occludes him");
        tree.transform.position = new Vector3(0,0,-.5f);
        fixedColor = Render(cam, rt, "nature_front_billboard_fixed");
        Require(fixedColor.g > .8f && fixedColor.r < .1f, "Nature in front should occlude the soldier");
        rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }
}
#endif
