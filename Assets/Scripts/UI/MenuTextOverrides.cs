using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// Optional text overrides + normalization for UI labels.
    /// Format (UTF-8): one entry per line: KEY=VALUE
    /// Lines starting with # or ; are comments.
    /// File: StreamingAssets/menu_override.txt
    /// </summary>
    public static class MenuTextOverrides
    {
        private static bool _loaded;
        private static readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.Ordinal);

        public static string Resolve(string key, string fallback)
        {
            EnsureLoaded();

            if (!string.IsNullOrEmpty(key))
            {
                if (_map.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                    return Normalize(v);

                // also try without leading '#'
                if (key.Length > 0 && key[0] == '#')
                {
                    var k2 = key.Substring(1);
                    if (_map.TryGetValue(k2, out v) && !string.IsNullOrEmpty(v))
                        return Normalize(v);
                }
            }

            return Normalize(fallback);
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // The original game text files sometimes use NBSP or narrow NBSP between words.
            // Our bitmap font maps only CP1251/ASCII, so normalize those whitespace chars to a regular space.
            s = s.Replace('\u00A0', ' ')
                 .Replace('\u202F', ' ')
                 .Replace('\u2007', ' ')
                 .Replace('\t', ' ');

            // Also collapse accidental double-spaces (optional, keep conservative)
            // s = System.Text.RegularExpressions.Regex.Replace(s, @"\s{2,}", " ");

            return s;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "menu_override.txt");
                if (!File.Exists(path)) return;

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#") || line.StartsWith(";")) continue;

                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    var k = line.Substring(0, eq).Trim();
                    var v = line.Substring(eq + 1).Trim();

                    if (k.Length == 0) continue;
                    _map[k] = v;
                }

                Debug.Log($"[C2:UI] MenuTextOverrides loaded: {_map.Count} entries");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:UI] MenuTextOverrides load failed: " + ex.Message);
            }
        }
    }
}
