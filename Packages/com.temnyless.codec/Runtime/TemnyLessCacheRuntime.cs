using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TemnyLessCodec
{
    public static class TemnyLessCacheRuntime
    {
        public static string CacheRootAbsolute =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Library", "TemnyLessCache"));

        public static string ToAbsoluteFromAssetPath(string assetPath)
        {
            // "Assets/..." -> absolute
            if (string.IsNullOrWhiteSpace(assetPath)) return assetPath;
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(assetPath, "Assets", StringComparison.OrdinalIgnoreCase))
                return assetPath;

            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        public static string ComputeSha1OfFile(string absolutePath)
        {
            using var fs = File.OpenRead(absolutePath);
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(fs);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
