#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Cossacks2Bridge.UnityAdapters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[InitializeOnLoad]
public static class C2MenuStartupRegressionEditor
{
    const string Key = "C2.MenuStartupRegression";
    const string ImagePath = "C2DiagnosticLogs/menu_startup_restored_20260907.png";
    static int phase;
    static C2MenuStartupRegressionEditor() { EditorApplication.update += Tick; }

    public static void ReimportMenuScript()
    {
        C2RegressionSceneScopeEditor.Begin();
        try
        {
            LogSceneBinding("before-reimport");
            var sourceScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/MenuBootstrap.cs");
            var properties = new SerializedObject(sourceScript).GetIterator();
            while (properties.Next(true))
                if (properties.propertyType == SerializedPropertyType.String && properties.stringValue.Length < 200)
                    Debug.Log("[C2 MENU SCRIPT METADATA] " + properties.propertyPath + "=" + properties.stringValue);
            foreach (var s in MonoImporter.GetAllRuntimeMonoScripts().Where(s => s.name.Contains("MenuBootstrap") || s.name == "MenuActionSink"))
                Debug.Log("[C2 MENU SCRIPT METADATA] runtimeScript=" + s.name + " path=" + AssetDatabase.GetAssetPath(s) + " class=" + s.GetClass());
            var host = new GameObject("MenuBindingProbe");
            var component = host.AddComponent<MenuBootstrap>();
            var boundScript = MonoScript.FromMonoBehaviour(component);
            Debug.Log("[C2 MENU SCRIPT METADATA] AddComponent script=" + boundScript + " path=" + AssetDatabase.GetAssetPath(boundScript) +
                " class=" + boundScript?.GetClass() + " serialized=" + EditorJsonUtility.ToJson(component));
            UnityEngine.Object.DestroyImmediate(host);
            AssetDatabase.ImportAsset("Assets/MenuBootstrap.cs", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            LogSceneBinding("after-reimport");
            C2RegressionSceneScopeEditor.Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); C2RegressionSceneScopeEditor.Finish(1); }
    }

    // Explicitly exercise cleanup on a failed batch run, not just the happy path.
    public static void RunExpectedFailureCleanup()
    {
        C2RegressionSceneScopeEditor.Begin();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("TemporaryRegressionObject_MustNotBeSaved");
        Debug.Log("[C2 MENU STARTUP] expected failure; cleanup must still restore the menu");
        C2RegressionSceneScopeEditor.Finish(7);
    }

    public static void Run()
    {
        C2RegressionSceneScopeEditor.Begin();
        try
        {
            // This Unity batch entry point starts with an unnamed scene instead
            // of opening the interactive editor's last setup. Verify the persisted
            // setup independently, then load that exact asset for the Play test.
            string startup = File.ReadAllText("Library/LastSceneManagerSetup.txt");
            Require(startup.Contains("- path: " + C2RegressionSceneScopeEditor.MenuScene) &&
                startup.Contains("isLoaded: 1") && startup.Contains("isActive: 1"), "Persisted startup scene missing");
            Debug.Log("[C2 MENU STARTUP] persisted scene=SampleScene; loading it explicitly in batch");
            EditorSceneManager.OpenScene(C2RegressionSceneScopeEditor.MenuScene, OpenSceneMode.Single);
            LogSceneBinding("before-play");
            Require(SceneManager.GetActiveScene().path == C2RegressionSceneScopeEditor.MenuScene,
                "Menu scene could not be opened: " + SceneManager.GetActiveScene().path);
            var first = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            Require(first != null && first.path == C2RegressionSceneScopeEditor.MenuScene, "Missing first build scene");
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".started", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); C2RegressionSceneScopeEditor.Finish(1); }
    }

    static void Tick()
    {
        if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".started", 0) > 90)
                throw new TimeoutException("Menu startup timed out");
            if (!EditorApplication.isPlaying || Time.frameCount < 30) return;
            if (phase == 0)
            {
                var bootstrap = UnityEngine.Object.FindFirstObjectByType<MenuBootstrap>();
                LogSceneBinding("in-play");
                Require(bootstrap != null && bootstrap.CurrentScreenId == "Main", "Main menu not initialized");
                var root = GameObject.Find("C2_MainMenuCanvas");
                Require(root != null && root.GetComponent<Canvas>().isActiveAndEnabled, "Main menu canvas missing/disabled");
                Require(root.GetComponent<GraphicRaycaster>().isActiveAndEnabled, "Menu cannot receive clicks");
                Require(EventSystem.current != null && EventSystem.current.currentInputModule != null, "Menu input module missing");
                int buttons = root.GetComponentsInChildren<Button>().Count(b => b.IsActive() && b.IsInteractable());
                int pictures = root.GetComponentsInChildren<RawImage>().Count(p => p.texture != null && p.isActiveAndEnabled);
                Require(buttons >= 3 && pictures > 0, "Menu buttons or background missing");
                Debug.Log("[C2 MENU STARTUP] Main visible, activeButtons=" + buttons + " texturedPictures=" + pictures);
                Directory.CreateDirectory("C2DiagnosticLogs");
                CaptureMenuForReview(root.GetComponent<Canvas>());
                phase = 1;
                return;
            }
            if (!File.Exists(ImagePath) || new FileInfo(ImagePath).Length < 10000) return;
            Debug.Log("[C2 MENU STARTUP] PASS persisted setup + SampleScene -> Play -> Main canvas/buttons/background/input; screenshot=" + ImagePath);
            SessionState.SetBool(Key, false);
            C2RegressionSceneScopeEditor.Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SessionState.SetBool(Key, false);
            C2RegressionSceneScopeEditor.Finish(1);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void CaptureMenuForReview(Canvas canvas)
    {
        // Batch mode does not present a Game view, so CaptureScreenshot never
        // completes here. Render the actual menu canvas to a test-only camera
        // target, then restore its normal ScreenSpaceOverlay configuration.
        Camera camera = Camera.main;
        Require(camera != null, "Menu camera missing");
        RenderMode oldMode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
        var target = new RenderTexture(1280, 960, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1280, 960, TextureFormat.RGB24, false);
        try
        {
            target.Create();
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(ImagePath, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = oldActive;
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            camera.targetTexture = oldTarget;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            Canvas.ForceUpdateCanvases();
        }
    }

    static void LogSceneBinding(string stage)
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/MenuBootstrap.cs");
        Debug.Log("[C2 MENU BINDING] " + stage + " scene=" + SceneManager.GetActiveScene().path +
            " scriptClass=" + script?.GetClass()?.FullName + " guid=" + AssetDatabase.AssetPathToGUID("Assets/MenuBootstrap.cs") +
            " resolved=" + AssetDatabase.GUIDToAssetPath("96d2455f92407494995263a404219b8a") +
            " compiledType=" + typeof(MenuBootstrap).AssemblyQualifiedName);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            Debug.Log("[C2 MENU BINDING] root=" + root.name + " active=" + root.activeSelf + " components=" +
                string.Join(",", root.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().FullName)));
    }
}
#endif
