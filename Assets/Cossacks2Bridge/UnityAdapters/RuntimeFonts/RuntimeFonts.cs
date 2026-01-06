using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.RuntimeFonts
{
    /// <summary>
    /// Loads bitmap "fonts" where each character is a separate frame texture (frame_0000..frame_0255).
    /// Mapping is 1:1 with original engine: glyphIndex == byte(codepage1251Char).
    /// 
    /// Place frames under:
    ///   Assets/Resources/Fonts/<fontName>_frames/frame_0000..frame_0255 (Texture2D or Sprite)
    /// Example:
    ///   Resources/Fonts/interf3_Fonts_FontG30_frames/frame_0065  (ASCII 'A')
    /// </summary>
    public static class RuntimeFonts
    {
        private static readonly Dictionary<string, Sprite[]> _cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Load a 256-glyph sprite table for a given font frames folder.
        /// </summary>
        public static Sprite[] LoadFont256(string fontFramesFolderName, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrWhiteSpace(fontFramesFolderName))
                throw new ArgumentException("fontFramesFolderName is null/empty", nameof(fontFramesFolderName));

            if (_cache.TryGetValue(fontFramesFolderName, out var cached) && cached != null && cached.Length == 256)
                return cached;

            var sprites = new Sprite[256];

            // Resources path (no extension)
            // e.g. "Fonts/interf3_Fonts_FontG30_frames/frame_0065"
            string basePath = $"Fonts/{fontFramesFolderName}/frame_";

            for (int i = 0; i < 256; i++)
            {
                string p = basePath + i.ToString("D4");

                // Try Sprite first (if importer is set to Sprite)
                var sp = Resources.Load<Sprite>(p);
                if (sp != null)
                {
                    sprites[i] = sp;
                    continue;
                }

                // Fallback: load Texture2D and make Sprite at runtime
                var tex = Resources.Load<Texture2D>(p);
                if (tex != null)
                {
                    var rect = new Rect(0, 0, tex.width, tex.height);
                    sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
                }
            }

            _cache[fontFramesFolderName] = sprites;
            return sprites;
        }

        /// <summary>
        /// Clear cache (useful while iterating in Editor).
        /// </summary>
        public static void ClearCache() => _cache.Clear();
    }
}
