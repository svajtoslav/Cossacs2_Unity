using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.Core
{
    public sealed class LocDb
    {
        private readonly Dictionary<string, string> _map =
            new(StringComparer.OrdinalIgnoreCase);

        public int Count => _map.Count;

        public void LoadDefault(CoreFileSystem fs)
        {
            // Основной файл локализации
            LoadKeyValueFile(fs, @"Text\dialogs.txt");

            // Дополнительные словари (в них как раз INTF_OPT_*)
            LoadKeyValueFile(fs, @"Text\textV0.txt");
            LoadKeyValueFile(fs, @"Text\textV1.txt");
            LoadKeyValueFile(fs, @"Text\textV2.txt");
            LoadKeyValueFile(fs, @"Text\textV3.txt");
            LoadKeyValueFile(fs, @"Text\BigMapData.txt");
        }


        private static void TryRegisterCodePages()
        {
            try
            {
                // Unity иногда не регистрирует codepages provider, из-за этого cp1251 недоступен.
                // Делаем мягкую регистрацию через reflection, без жёсткой зависимости от System.Text.Encoding.CodePages.
                var t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                if (t == null) return;
                var instProp = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var inst = instProp != null ? instProp.GetValue(null, null) : null;
                if (inst == null) return;
                var m = typeof(System.Text.Encoding).GetMethod("RegisterProvider", new[] { typeof(System.Text.EncodingProvider) });
                if (m == null) return;
                m.Invoke(null, new[] { inst });
            }
            catch
            {
                // ignore
            }
        }

        public void LoadKeyValueFile(CoreFileSystem fs, string relPath)
        {
            TryRegisterCodePages();

            string text;
            try
            {
                // dialogs.txt и textV*.txt чаще всего в cp1251
                text = fs.ReadAllText(relPath, Encoding.GetEncoding(1251));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocDb] Skip '{relPath}': {e.Message}");
                return;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // комменты
                if (line.StartsWith("//") || line.StartsWith(";")) continue;

                // Берём первый токен как key, остальное как value
                int sp = line.IndexOfAny(new[] { ' ', '\t' });
                if (sp <= 0) continue;

                string key = line.Substring(0, sp).Trim();
                string val = line.Substring(sp).Trim();

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val)) continue;

                // dialogs.txt может быть "#KEY", textV*.txt может быть "KEY"
                // Сохраняем обе формы, чтобы Resolve работал всегда.
                if (!_map.ContainsKey(key))
                    _map[key] = val;

                if (key[0] == '#')
                {
                    string keyNoHash = key.Substring(1);
                    if (!string.IsNullOrEmpty(keyNoHash) && !_map.ContainsKey(keyNoHash))
                        _map[keyNoHash] = val;
                }
                else
                {
                    string keyHash = "#" + key;
                    if (!_map.ContainsKey(keyHash))
                        _map[keyHash] = val;
                }
            }

            Debug.Log($"[LocDb] Loaded {Count} keys from {relPath}");
        }

        public string Resolve(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";

            key = key.Trim();

            if (_map.TryGetValue(key, out var v))
                return v;

            // fallback: пробуем с/без '#'
            if (key[0] != '#')
            {
                if (_map.TryGetValue("#" + key, out v))
                    return v;
            }
            else
            {
                var k2 = key.Substring(1);
                if (_map.TryGetValue(k2, out v))
                    return v;
            }

            return key; // fallback
        }
    }
}