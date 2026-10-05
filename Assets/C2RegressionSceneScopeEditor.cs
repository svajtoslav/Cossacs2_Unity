#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch tests may replace scenes, but must not leave their temporary scene as
// the next interactive editor startup. SessionState survives Play Mode reloads.
[InitializeOnLoad]
public static class C2RegressionSceneScopeEditor
{
    public const string MenuScene = "Assets/Scenes/SampleScene.unity";
    const string Key = "C2.RegressionSceneScope";

    [Serializable] sealed class SavedSetup { public SavedScene[] scenes; }
    [Serializable] sealed class SavedScene
    {
        public string path;
        public bool isLoaded, isActive;
    }

    static C2RegressionSceneScopeEditor() { EditorApplication.update += FinishAfterPlayMode; }

    public static void Begin()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Scene scope must begin in batch Edit Mode.");
        if (SessionState.GetBool(Key + ".active", false))
            throw new InvalidOperationException("A scene-restoring test is already running.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        // An empty/Untitled startup has no restorable asset. Recover the menu in
        // that case; never save the contents of a temporary test scene over it.
        var saved = setup.Where(s => !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
            .Select(s => new SavedScene { path = s.path, isLoaded = s.isLoaded, isActive = s.isActive }).ToArray();
        if (saved.Length == 0) saved = ReadPersistedSetup();
        SessionState.SetString(Key + ".setup", JsonUtility.ToJson(new SavedSetup { scenes = saved }));
        SessionState.SetBool(Key + ".active", true);
        SessionState.SetBool(Key + ".finish", false);
        Debug.Log("[C2 TEST SCENE] captured=" + saved.Length);
    }

    static SavedScene[] ReadPersistedSetup()
    {
        const string path = "Library/LastSceneManagerSetup.txt";
        if (!File.Exists(path)) return Array.Empty<SavedScene>();
        var scenes = new List<SavedScene>();
        SavedScene current = null;
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Trim();
            if (line.StartsWith("- path: ", StringComparison.Ordinal))
            {
                string assetPath = line.Substring(8).Trim();
                if (assetPath.StartsWith("\"", StringComparison.Ordinal))
                    assetPath = JsonUtility.FromJson<SavedScene>("{\"path\":" + assetPath + "}").path;
                current = new SavedScene { path = assetPath };
                scenes.Add(current);
            }
            else if (current != null && line.StartsWith("isLoaded:", StringComparison.Ordinal)) current.isLoaded = line.EndsWith("1");
            else if (current != null && line.StartsWith("isActive:", StringComparison.Ordinal)) current.isActive = line.EndsWith("1");
        }
        return scenes.Where(s => !string.IsNullOrEmpty(s.path) && File.Exists(s.path)).ToArray();
    }

    public static void Finish(int exitCode)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        SessionState.SetInt(Key + ".exitCode", exitCode);
        SessionState.SetBool(Key + ".finish", true);
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
    }

    static void FinishAfterPlayMode()
    {
        if (!Application.isBatchMode || !SessionState.GetBool(Key + ".finish", false) ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        SessionState.SetBool(Key + ".finish", false);
        int exitCode = SessionState.GetInt(Key + ".exitCode", 1);
        try
        {
            var saved = JsonUtility.FromJson<SavedSetup>(SessionState.GetString(Key + ".setup", "{}"));
            var scenes = saved?.scenes?.Where(s => File.Exists(s.path)).ToArray();
            if (scenes == null || scenes.Length == 0 || !scenes.Any(s => s.isLoaded))
                EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            else
            {
                if (!scenes.Any(s => s.isActive && s.isLoaded)) scenes.First(s => s.isLoaded).isActive = true;
                EditorSceneManager.RestoreSceneManagerSetup(scenes.Select(s => new SceneSetup
                    { path = s.path, isLoaded = s.isLoaded, isActive = s.isActive && s.isLoaded }).ToArray());
            }
            Debug.Log("[C2 TEST SCENE] RESTORED exitCode=" + exitCode + " scenes=" +
                string.Join(",", EditorSceneManager.GetSceneManagerSetup().Select(s => s.path)));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            exitCode = 1;
            // A deleted/missing captured asset must not strand startup at Untitled.
            try { EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single); }
            catch (Exception recoveryError) { Debug.LogException(recoveryError); }
        }
        finally
        {
            SessionState.SetBool(Key + ".active", false);
            SessionState.EraseString(Key + ".setup");
        }
        // Let scene-open notifications complete before Unity persists editor state.
        int result = exitCode;
        EditorApplication.delayCall += () => EditorApplication.Exit(result);
    }
}
#endif
