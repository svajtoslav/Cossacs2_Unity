using System;
using System.IO;
using System.Linq;
using System.Text;
using Cossacks2Bridge.Core;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// V392: binds non-XML menu data to the exact source bundle that supplied
    /// the current DialogsSystem XML.  If XML came from the bundled clean 1.4
    /// tree, dependent menu data (AI/ai.dat, Missions/Heroes/*, etc.) is read
    /// from that same tree and never silently falls back to DataRoot.
    /// </summary>
    public sealed class Menu14SourceContext
    {
        private readonly CoreFileSystem _fallbackFs;

        public string BundleId { get; }
        public string DataRoot { get; }
        public bool StrictBundle { get; }

        private Menu14SourceContext(string bundleId, string dataRoot, bool strictBundle, CoreFileSystem fallbackFs)
        {
            BundleId = bundleId ?? string.Empty;
            DataRoot = dataRoot ?? string.Empty;
            StrictBundle = strictBundle;
            _fallbackFs = fallbackFs;
        }

        public static Menu14SourceContext FromDesk(UiDesk desk, CoreFileSystem fs)
        {
            string bundle = desk?.SourceBundleId;
            if (string.IsNullOrWhiteSpace(bundle))
                bundle = desk?.XmlSource ?? "DataRoot";

            string root = desk?.SourceDataRoot;
            if (string.IsNullOrWhiteSpace(root))
                root = fs?.DataRoot ?? string.Empty;

            bool strict = string.Equals(bundle, "clean14", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(desk?.XmlSource, "StreamingAssets-clean14", StringComparison.OrdinalIgnoreCase);

            return new Menu14SourceContext(bundle, root, strict, fs);
        }

        public bool Exists(string relativePath)
        {
            string resolved = ResolveCaseInsensitive(DataRoot, relativePath);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                return true;

            return !StrictBundle && _fallbackFs != null && _fallbackFs.Exists(relativePath);
        }

        public string ReadAllText(string relativePath, Encoding encoding = null)
        {
            string resolved = ResolveCaseInsensitive(DataRoot, relativePath);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
            {
                byte[] bytes = File.ReadAllBytes(resolved);
                if (encoding != null)
                    return encoding.GetString(bytes);
                return DecodeText(bytes);
            }

            if (!StrictBundle && _fallbackFs != null)
            {
                return encoding == null
                    ? _fallbackFs.ReadAllText(relativePath)
                    : _fallbackFs.ReadAllText(relativePath, encoding);
            }

            return string.Empty;
        }

        public string ResolvePath(string relativePath)
        {
            string resolved = ResolveCaseInsensitive(DataRoot, relativePath);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                return resolved;
            return string.Empty;
        }

        private static string DecodeText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            try { return new UTF8Encoding(false, true).GetString(bytes); }
            catch
            {
                try { return Encoding.GetEncoding(1251).GetString(bytes); }
                catch { return Encoding.ASCII.GetString(bytes); }
            }
        }

        private static string ResolveCaseInsensitive(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            string[] parts = relativePath.Replace('/', '\\')
                .Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            string cur = root;
            foreach (string part in parts)
            {
                string direct = Path.Combine(cur, part);
                if (File.Exists(direct) || Directory.Exists(direct))
                {
                    cur = direct;
                    continue;
                }

                if (!Directory.Exists(cur)) return string.Empty;
                string match = Directory.EnumerateFileSystemEntries(cur)
                    .FirstOrDefault(p => string.Equals(Path.GetFileName(p), part, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(match)) return string.Empty;
                cur = match;
            }
            return cur;
        }
    }
}
