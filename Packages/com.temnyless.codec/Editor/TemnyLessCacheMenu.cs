#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TemnyLessCodec.Editor
{
    public static class TemnyLessCacheMenu
    {
        [MenuItem("Tools/TemnyLess/Clear Cache")]
        public static void ClearCache()
        {
            var dir = TemnyLessCacheRuntime.CacheRootAbsolute;
            if (!EditorUtility.DisplayDialog("TemnyLess Cache", "Delete cache folder?\n" + dir, "Delete", "Cancel"))
                return;

            TemnyLessCacheEditor.SafeDeleteDir(dir);
            Debug.Log("[TemnyLess] Cache cleared: " + dir);
        }

        [MenuItem("Tools/TemnyLess/Open Cache Folder")]
        public static void OpenCacheFolder()
        {
            var dir = TemnyLessCacheRuntime.CacheRootAbsolute;
            System.IO.Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }
    }
}
#endif
