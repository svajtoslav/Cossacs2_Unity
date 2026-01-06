#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TemnyLessCodec;

public class TemnyLessOpenCommand : EditorWindow
{
    private string _cmd = "open Assets/Units/icons.g16";

    [MenuItem("Tools/TemnyLess/Open Command %#o")]
    public static void ShowWindow()
    {
        var w = GetWindow<TemnyLessOpenCommand>("TemnyLess Open");
        w.minSize = new Vector2(520, 90);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Command:", EditorStyles.boldLabel);
        _cmd = EditorGUILayout.TextField(_cmd);

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Run", GUILayout.Height(26)))
            Run(_cmd);

        EditorGUILayout.HelpBox("Commands:\n  open <assetPath>\nExamples:\n  open Assets/Units/icons.g16\n  open Assets/Units/AusFlgC.g2d", MessageType.Info);
    }

    private static void Run(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;

        cmd = cmd.Trim();
        if (!cmd.StartsWith("open ", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("[TemnyLess] Unknown command. Use: open <assetPath>");
            return;
        }

        var arg = cmd.Substring(5).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(arg)) return;

        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(arg);
        if (obj == null)
        {
            Debug.LogError("[TemnyLess] Asset not found: " + arg);
            return;
        }

        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);

        // If it's our imported G16/G2D asset — spawn viewer GO in current scene
        if (obj is G16Asset g16)
        {
            SpawnG16Viewer(g16);
        }
        else if (obj is G2DAsset g2d)
        {
            SpawnG2DAnimator(g2d);
        }
        else
        {
            Debug.Log("[TemnyLess] Selected: " + arg);
        }
    }

    private static string GuessCachedSourceAbsolute(string sourceAssetPath, string cacheDirAbsolute)
    {
        // cacheDirAbsolute points to Library/TemnyLessCache/<kind>/<sha1>
        // original filename is taken from source asset path
        try
        {
            var fileName = Path.GetFileName(sourceAssetPath);
            if (string.IsNullOrEmpty(fileName)) return null;
            var p = Path.Combine(cacheDirAbsolute ?? "", fileName);
            return File.Exists(p) ? p : null;
        }
        catch { return null; }
    }

    private static void SpawnG16Viewer(G16Asset a)
    {
        var srcAbs = GuessCachedSourceAbsolute(a.sourceAssetPath, a.cacheDirAbsolute)
                     ?? TemnyLessCacheRuntime.ToAbsoluteFromAssetPath(a.sourceAssetPath);

        var go = new GameObject("[G16] " + Path.GetFileName(a.sourceAssetPath));
        var viewerType = Type.GetType("G16SpriteStackViewer,Assembly-CSharp");
        if (viewerType == null)
        {
            Debug.LogError("[TemnyLess] G16SpriteStackViewer not found in project. Add your script first.");
            return;
        }

        var comp = go.AddComponent(viewerType);

        // set g16Path via reflection (field is public in your script)
        var f = viewerType.GetField("g16Path");
        if (f != null) f.SetValue(comp, srcAbs);

        // call Load if exists
        var m = viewerType.GetMethod("LoadG16AndBuildList");
        m?.Invoke(comp, null);

        Selection.activeGameObject = go;
        Debug.Log("[TemnyLess] Spawned G16 viewer: " + srcAbs);
    }

    private static void SpawnG2DAnimator(G2DAsset a)
    {
        var srcAbs = GuessCachedSourceAbsolute(a.sourceAssetPath, a.cacheDirAbsolute)
                     ?? TemnyLessCacheRuntime.ToAbsoluteFromAssetPath(a.sourceAssetPath);

        var go = new GameObject("[G2D] " + Path.GetFileName(a.sourceAssetPath));
        var animatorType = Type.GetType("G2DSceneAnimator,Assembly-CSharp");
        if (animatorType == null)
        {
            Debug.LogError("[TemnyLess] G2DSceneAnimator not found in project. Add your script first.");
            return;
        }

        var comp = go.AddComponent(animatorType);

        var f = animatorType.GetField("g2dPath");
        if (f != null) f.SetValue(comp, srcAbs);

        var m = animatorType.GetMethod("LoadAndPlay");
        m?.Invoke(comp, null);

        Selection.activeGameObject = go;
        Debug.Log("[TemnyLess] Spawned G2D animator: " + srcAbs);
    }
}
#endif
