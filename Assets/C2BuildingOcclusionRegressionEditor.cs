#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cossacks2Bridge.UnityAdapters.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Actual MD building + actual batched units; no saved scene or gameplay changes.
[InitializeOnLoad]
public static class C2BuildingOcclusionRegressionEditor
{
    const string Key = "C2.BuildingOcclusionCapture";
    static int phase, phaseFrame;
    static C2BattleTerrainMode mode;
    static C2RuntimeConstructionSitePseudo3DV245LikeOriginal site;
    static RenderTexture target;
    static Camera camera;
    static C2BuildingOcclusionRegressionEditor() { EditorApplication.update += Tick; }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only");
        C2RegressionSceneScopeEditor.Begin();
        try
        {
            EditorSceneManager.OpenScene(C2RegressionSceneScopeEditor.MenuScene);
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); C2RegressionSceneScopeEditor.Finish(1); }
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".start", 0) > 150)
                throw new TimeoutException("Occlusion capture timed out");
            if (phase == 0)
            {
                if (Time.frameCount < 10) return;
                if (!(bool)typeof(C2FoundationBenchmarkEditor).GetMethod("TryOpenEditorPlateauLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null)) return;
                phaseFrame = Time.frameCount; phase = 1; return;
            }
            if (Time.frameCount - phaseFrame < 60) return;
            if (phase == 1)
            {
                mode = UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>();
                if (!mode.C2NoUnitWorldToOriginalPixelLikeOriginal(Vector3.zero, out float x, out float y)) throw new Exception("No map point");
                if (!mode.C2BuildRuntimeCreateConstructionLikeOriginal("EngKaz", "BldKaz(EN)", 0, Mathf.RoundToInt(x*16),
                    Mathf.RoundToInt(y*16), "occlusion_capture", out GameObject go, out string audit)) throw new Exception(audit);
                site = go.GetComponent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();
                var item = new C2OriginalProduceItemV13 { MdName = "EngKri", UnitId = "EngKri", Peasant = true };
                int count = 0;
                for (int row = -2; row <= 2; row++)
                for (int col = -8; col <= 8; col++)
                {
                    if (!C2UnitOriginalRuntimeAndRendererV1.TrySpawnEditorUnitAtRealV332LikeOriginal(mode, item, 0,
                        site.RealX + col * 640, site.RealY + row * 2240, out var u, out string unitAudit)) throw new Exception(unitAudit);
                    u.SetSelected(true); count++;
                }
                camera = mode.GetActiveBattleCameraLikeOriginal();
                target = new RenderTexture(1904,1040,24,RenderTextureFormat.ARGB32); target.Create();
                camera.targetTexture = target;
                Debug.Log("[C2 ACTUAL OCCLUSION] created selected peasants=" + count + " building=EngKaz");
                phaseFrame = Time.frameCount; phase = 2; return;
            }
            if (phase == 2)
            {
                if (mode.SmpPendingChunksForDiagnostics != 0) return;
                Save("actual_barracks_construction_selection");
                site.CompleteInstantForEditorV332LikeOriginal();
                phase = 3; phaseFrame = Time.frameCount; return;
            }
            if (phase == 3)
            {
                Save("actual_barracks_ready_selection");
                foreach (Renderer r in site.GetComponentsInChildren<Renderer>()) r.enabled = false;
                phase = 4; phaseFrame = Time.frameCount; return;
            }
            if (phase == 4)
            {
                Save("actual_barracks_hidden_control");
                AddActualNature();
                phase = 5; phaseFrame = Time.frameCount; return;
            }
            Save("actual_nature_selected_peasants");
            Debug.Log("[C2 ACTUAL OCCLUSION] CAPTURE COMPLETE; images require visual review");
            Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int result)
    {
        if (camera != null && camera.targetTexture == target) camera.targetTexture = null;
        if (target != null) UnityEngine.Object.DestroyImmediate(target);
        target = null;
        SessionState.SetBool(Key, false);
        C2RegressionSceneScopeEditor.Finish(result);
    }
    static void AddActualNature()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(C2BattleTerrainMode);
        var defs = (IList)type.GetMethod("LoadOriginalResourceCatalogV1LikeOriginal", flags)
            .Invoke(mode, new object[] { "treelist.lst", "treelist.rsr" });
        var sprites = (IList)type.GetField("_c2OriginalResourcesV1", flags).GetValue(mode);
        Type spriteType = type.GetNestedType("C2OriginalResourceSpriteV1", BindingFlags.NonPublic);
        for (int i = 0; i < 4; ++i)
        {
            object def = defs[i];
            object sprite = Activator.CreateInstance(spriteType);
            spriteType.GetField("Sign").SetValue(sprite, "GA");
            spriteType.GetField("X").SetValue(sprite, site.RealX / 16 + (i * 4 - 6) * 40);
            spriteType.GetField("Y").SetValue(sprite, site.RealY / 16 + ((i % 2) * 2 - 1) * 140);
            spriteType.GetField("Def").SetValue(sprite, def);
            sprites.Add(sprite);
            Debug.Log("[C2 ACTUAL OCCLUSION] nature catalog=" + def.GetType().GetField("Name").GetValue(def) +
                " frame=" + def.GetType().GetField("Frame").GetValue(def));
        }
        type.GetMethod("BuildOriginalNatureVisualsV1LikeOriginal", flags).Invoke(mode, null);
        var root = (GameObject)type.GetField("_c2OriginalNatureRootV1", flags).GetValue(mode);
        if (root.GetComponentsInChildren<MeshRenderer>().Length < 4) throw new Exception("Missing real nature sprites");
    }
    static void Save(string name)
    {
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
        File.WriteAllBytes("C2DiagnosticLogs/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image); RenderTexture.active = previous;
    }
}
#endif
