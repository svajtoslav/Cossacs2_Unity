using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cossacks2Bridge.Core;

namespace Cossacks2Bridge.UnityAdapters.BigMap
{
    /// <summary>
    /// V396A8.4: source-faithful GetTextByID supplement for the final-1.4 BigMap only.
    ///
    /// Why this exists:
    /// - CSectStatData::ReadInitData in 1.1 resolves the SectorName token through
    ///   GetTextByID while Sectors.dat is read.
    /// - final engine_1.4.exe keeps exactly the same projection.
    /// - final 1.4 expands the country registry to 9 entries, while the bridge's
    ///   generic LocDb intentionally loads only a curated subset of Text files.
    ///
    /// This resolver NEVER invents country/sector names. It first trusts LocDb.
    /// Only unresolved BigMap keys are looked up in the actual final-1.4 DataRoot
    /// Text tree. Conflicting duplicate definitions are marked ambiguous and are
    /// not chosen by guesswork.
    /// </summary>
    internal sealed class C2BigMapFinal14TextResolverV396A8_4
    {
        private readonly LocDb _loc;
        private readonly string _dataRoot;
        private readonly Dictionary<string, string> _supplement =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _source =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _conflicts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _wanted =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int FilesScanned { get; private set; }
        public int SupplementMatches => _supplement.Count;
        public int AmbiguousCount => _conflicts.Count;
        public string DataRoot => _dataRoot;

        public C2BigMapFinal14TextResolverV396A8_4(
            string dataRoot,
            LocDb loc,
            IList<C2BigMapData14.SectorDefinition> sectors)
        {
            _dataRoot = dataRoot ?? string.Empty;
            _loc = loc;

            for (int i = 0; i < C2Bfe14ContractV396A.BigMapCountryTextKeys.Length; i++)
                AddWanted(C2Bfe14ContractV396A.BigMapCountryTextKeys[i]);

            // Final data historically uses both EGYPT and EGIPET spellings.
            AddWanted("#EGIPET");

            if (sectors != null)
            {
                for (int i = 0; i < sectors.Count; i++)
                {
                    C2BigMapData14.SectorDefinition s = sectors[i];
                    if (s != null) AddWanted(s.SectorName);
                }
            }

            BuildSupplementFromFinal14TextTree();
        }

        public bool TryResolve(string key, out string value, out string source, out bool ambiguous)
        {
            value = string.Empty;
            source = string.Empty;
            ambiguous = false;
            if (string.IsNullOrWhiteSpace(key)) return false;

            string normalized = key.Trim();
            string local = _loc != null ? _loc.Resolve(normalized) : normalized;
            if (IsResolved(normalized, local))
            {
                value = local;
                source = "LocDb";
                return true;
            }

            string canonical = CanonicalKey(normalized);
            if (_conflicts.TryGetValue(canonical, out string conflict))
            {
                ambiguous = true;
                source = conflict;
                return false;
            }

            if (_supplement.TryGetValue(canonical, out string extra) && !string.IsNullOrWhiteSpace(extra))
            {
                value = extra;
                _source.TryGetValue(canonical, out source);
                return true;
            }

            return false;
        }

        public string Resolve(string key, out string source, out bool ambiguous)
        {
            if (TryResolve(key, out string value, out source, out ambiguous)) return value;
            return key ?? string.Empty;
        }

        private void BuildSupplementFromFinal14TextTree()
        {
            if (_wanted.Count == 0 || string.IsNullOrWhiteSpace(_dataRoot)) return;

            string textRoot = Path.Combine(_dataRoot, "Text");
            if (!Directory.Exists(textRoot)) return;

            TryRegisterCodePages();
            Encoding cp1251;
            try { cp1251 = Encoding.GetEncoding(1251); }
            catch { cp1251 = Encoding.Default; }

            string[] files;
            try
            {
                files = Directory.GetFiles(textRoot, "*", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return;
            }

            for (int fi = 0; fi < files.Length; fi++)
            {
                string path = files[fi];
                try
                {
                    var info = new FileInfo(path);
                    if (!info.Exists || info.Length <= 0 || info.Length > 16L * 1024L * 1024L) continue;

                    string text = File.ReadAllText(path, cp1251);
                    FilesScanned++;
                    string rel = MakeRelativePath(_dataRoot, path);
                    ParseWantedKeys(text, rel);
                }
                catch
                {
                    // A non-text/broken file in Text must not break BigMap startup.
                    // Unresolved keys remain visible in the audit instead of being guessed.
                }
            }
        }

        private void ParseWantedKeys(string text, string sourcePath)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = (lines[i] ?? string.Empty).Trim().TrimStart('\uFEFF');
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                    continue;

                int sp = line.IndexOfAny(new[] { ' ', '\t' });
                if (sp <= 0) continue;

                string rawKey = line.Substring(0, sp).Trim();
                string val = line.Substring(sp).Trim();
                if (rawKey.Length == 0 || val.Length == 0) continue;

                string canonical = CanonicalKey(rawKey);
                if (!_wanted.Contains(canonical)) continue;

                if (_conflicts.ContainsKey(canonical)) continue;
                if (_supplement.TryGetValue(canonical, out string oldValue))
                {
                    if (!string.Equals(oldValue, val, StringComparison.Ordinal))
                    {
                        string oldSource = _source.TryGetValue(canonical, out string os) ? os : "unknown";
                        _supplement.Remove(canonical);
                        _source.Remove(canonical);
                        _conflicts[canonical] = oldSource + " <> " + sourcePath;
                    }
                    continue;
                }

                _supplement[canonical] = val;
                _source[canonical] = sourcePath;
            }
        }

        private void AddWanted(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _wanted.Add(CanonicalKey(key));
        }

        private static bool IsResolved(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (string.Equals(key, value, StringComparison.OrdinalIgnoreCase)) return false;

            string alt = key.StartsWith("#", StringComparison.Ordinal) ? key.Substring(1) : "#" + key;
            return !string.Equals(alt, value, StringComparison.OrdinalIgnoreCase);
        }

        private static string CanonicalKey(string key)
        {
            string k = (key ?? string.Empty).Trim();
            return k.StartsWith("#", StringComparison.Ordinal) ? k.Substring(1) : k;
        }

        private static string MakeRelativePath(string root, string path)
        {
            try
            {
                string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string p = Path.GetFullPath(path);
                if (p.StartsWith(r, StringComparison.OrdinalIgnoreCase)) return p.Substring(r.Length).Replace('/', '\\');
            }
            catch { }
            return path ?? string.Empty;
        }

        private static void TryRegisterCodePages()
        {
            try
            {
                Type t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                if (t == null) return;
                var prop = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                object inst = prop != null ? prop.GetValue(null, null) : null;
                if (inst == null) return;
                var m = typeof(Encoding).GetMethod("RegisterProvider", new[] { typeof(EncodingProvider) });
                if (m != null) m.Invoke(null, new[] { inst });
            }
            catch { }
        }
    }
}
