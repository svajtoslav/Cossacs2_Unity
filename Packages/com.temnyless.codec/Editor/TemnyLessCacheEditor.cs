#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TemnyLessCodec.Editor
{
    public static class TemnyLessCacheEditor
    {
        public static string CacheRoot => TemnyLessCacheRuntime.CacheRootAbsolute;

        public static string EnsureDir(string p)
        {
            Directory.CreateDirectory(p);
            return p;
        }

        public static string MakeCacheDir(string kind, string sha1)
        {
            // kind: "g16" or "g2d"
            var dir = Path.Combine(CacheRoot, kind, sha1);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string CopySourceToCache(string sourceAbs, string cacheDir, string fileName)
        {
            var dst = Path.Combine(cacheDir, fileName);
            if (!File.Exists(dst))
                File.Copy(sourceAbs, dst, overwrite: false);
            return dst;
        }

        public static void SafeDeleteDir(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TemnyLessCache] Failed to delete dir: " + dir + " :: " + ex.Message);
            }
        }
    }
}
#endif
