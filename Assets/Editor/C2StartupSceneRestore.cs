#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class C2StartupSceneRestore
{
    private const string StartupScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SessionKey = "C2.StartupSceneRestore.Done";

    static C2StartupSceneRestore()
    {
        EditorApplication.delayCall += RestoreStartupSceneOnce;
    }

    private static void RestoreStartupSceneOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var active = SceneManager.GetActiveScene();

        // Normal project start after Library/session state was lost: Unity opens an empty Untitled scene.
        // Do not replace a real scene and never discard unsaved user changes.
        if (!string.IsNullOrEmpty(active.path) || active.isDirty)
            return;

        var startupScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartupScenePath);
        if (startupScene == null)
        {
            Debug.LogError($"[C2 STARTUP SCENE] Missing startup scene: {StartupScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(StartupScenePath, OpenSceneMode.Single);
        Debug.Log($"[C2 STARTUP SCENE] restored: {StartupScenePath}");
    }
}
#endif
