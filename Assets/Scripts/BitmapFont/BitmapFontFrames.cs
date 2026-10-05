using System;
using System.Collections.Generic;
using UnityEngine;

namespace TemnyLess.BitmapFonts
{
    /// <summary>
    /// Loads bitmap-font glyph frames from Resources folder:
    /// Resources/Fonts/<fontFramesFolder>/frame_XXXX (tga/png imported as Texture2D).
    /// Builds Sprites for each frame index found.
    /// </summary>
    [CreateAssetMenu(menuName = "TemnyLess/Bitmap Font Frames", fileName = "BitmapFontFrames")]
    public class BitmapFontFrames : ScriptableObject
    {
        [Tooltip("Resources path under Assets/Resources (without extension). Example: Fonts/interf3_Fonts_FontG30_frames")]
        public string resourcesFolder = "Fonts/interf3_Fonts_FontG30_frames";

        [Tooltip("Pixels-per-unit used for sprites.")]
        public float pixelsPerUnit = 100f;

        [Tooltip("Extra spacing between glyphs (pixels).")]
        public float letterSpacingPx = 0f;

        [Tooltip("Extra spacing between lines (pixels).")]
        public float lineSpacingPx = 0f;

        private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>(256);
        private bool _loaded;

        public bool IsLoaded => _loaded;

        public void EnsureLoaded()
        {
            if (_loaded) return;

            _sprites.Clear();

            // Load all textures from folder
            var tex = Resources.LoadAll<Texture2D>(resourcesFolder);
            if (tex == null || tex.Length == 0)
            {
                Debug.LogWarning($"[BitmapFontFrames] No textures in Resources/{resourcesFolder}. " +
                                 $"Check that frames exist and are marked as Texture2D in Resources.");
                _loaded = true;
                return;
            }

            foreach (var t in tex)
            {
                if (t == null) continue;

                int idx = ParseFrameIndex(t.name);
                if (idx < 0) continue;

                var sp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                _sprites[idx] = sp;
            }

            _loaded = true;
            Debug.Log($"[BitmapFontFrames] Loaded { _sprites.Count } glyph frames from Resources/{resourcesFolder}");
        }

        public bool TryGetSprite(int frameIndex, out Sprite sprite)
        {
            EnsureLoaded();
            return _sprites.TryGetValue(frameIndex, out sprite);
        }

        public float LetterSpacingPx => letterSpacingPx;
        public float LineSpacingPx => lineSpacingPx;

        private static int ParseFrameIndex(string texName)
        {
            // frame_0123
            if (string.IsNullOrEmpty(texName)) return -1;

            int u = texName.LastIndexOf("frame_", StringComparison.OrdinalIgnoreCase);
            if (u < 0) return -1;

            string tail = texName.Substring(u + 6);
            // Some importers rename, keep digits only
            int n = 0;
            int countDigits = 0;
            for (int i = 0; i < tail.Length; i++)
            {
                char c = tail[i];
                if (c >= '0' && c <= '9')
                {
                    n = (n * 10) + (c - '0');
                    countDigits++;
                    if (countDigits >= 6) break;
                }
                else break;
            }
            if (countDigits == 0) return -1;
            return n;
        }
    }
}
