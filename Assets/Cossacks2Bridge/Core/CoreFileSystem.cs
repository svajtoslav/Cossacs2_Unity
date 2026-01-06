using System;
using System.IO;
using System.Text;

namespace Cossacks2Bridge.Core
{
    /// <summary>
    /// Simple root-based filesystem. Points at ".../Cossacks2/Data".
    /// Keeps all engine-like paths (Interf3\background\main_menu.JPG) working.
    /// </summary>
    public sealed class CoreFileSystem
    {
        public string DataRoot { get; }

        public CoreFileSystem(string dataRoot)
        {
            DataRoot = dataRoot ?? throw new ArgumentNullException(nameof(dataRoot));
        }

        public string ResolvePath(string gameRelativePath)
        {
            if (string.IsNullOrWhiteSpace(gameRelativePath))
                return "";

            // game uses backslashes; normalize for OS
            string p = gameRelativePath.Replace('/', Path.DirectorySeparatorChar)
                                      .Replace('\\', Path.DirectorySeparatorChar);

            // some xml may contain weird prefixes like "#work#\..."
            if (p.StartsWith("#work#", StringComparison.OrdinalIgnoreCase))
                p = p.Substring("#work#".Length).TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(DataRoot, p);
        }

        public bool Exists(string gameRelativePath) => File.Exists(ResolvePath(gameRelativePath));

        public byte[] ReadAllBytes(string gameRelativePath)
        {
            string p = ResolvePath(gameRelativePath);
            return File.ReadAllBytes(p);
        }

        // Default: "try utf8 then cp1251 then ascii"
        public string ReadAllText(string gameRelativePath)
        {
            string p = ResolvePath(gameRelativePath);

            byte[] bytes = File.ReadAllBytes(p);

            try { return Encoding.UTF8.GetString(bytes); }
            catch { /* ignore */ }

            try { return Encoding.GetEncoding(1251).GetString(bytes); }
            catch { /* ignore */ }

            return Encoding.ASCII.GetString(bytes);
        }

        // Explicit encoding (для Text\dialogs.txt и др.)
        public string ReadAllText(string gameRelativePath, Encoding enc)
        {
            string p = ResolvePath(gameRelativePath);
            return File.ReadAllText(p, enc);
        }
    }
}
