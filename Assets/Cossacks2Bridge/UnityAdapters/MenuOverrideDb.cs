using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters
{
    internal static class MenuOverrideDb
    {
        private static Dictionary<string, string> _map;
        private static bool _loaded;

        private static void EnsureLoaded(bool verbose)
        {
            if (_loaded) return;
            _loaded = true;

            _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "menu_override.txt");
                if (!File.Exists(path))
                {
                    if (verbose) Debug.Log($"[MenuOverrideDb] No overrides file: {path}");
                    return;
                }

                var lines = File.ReadAllLines(path);
                foreach (var raw in lines)
                {
                    var line = raw?.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("//", StringComparison.Ordinal)) continue;
                    if (line.StartsWith(";", StringComparison.Ordinal)) continue;

                    // Разрешаем ключи, начинающиеся с # (это НЕ комментарий)
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    var k = line.Substring(0, eq).Trim();
                    var v = line.Substring(eq + 1).Trim();
                    if (k.Length == 0) continue;

                    // нормализуем: поддерживаем и "#KEY", и "KEY"
                    if (k[0] != '#') _map[k] = v;
                    _map["#" + k.TrimStart('#')] = v;
                }

                if (verbose) Debug.Log($"[MenuOverrideDb] Loaded overrides: {_map.Count}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuOverrideDb] Failed to load overrides: " + e);
            }
        }

        public static string Resolve(string key, Cossacks2Bridge.Core.LocDb loc, bool verbose)
        {
            EnsureLoaded(verbose);

            if (!string.IsNullOrEmpty(key) && _map != null && _map.TryGetValue(key, out var v))
                return v;

            // fallback: оригинальная локализация
            return (loc != null) ? loc.Resolve(key) : (key ?? "");
        }
    }
}
