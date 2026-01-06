using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// Emulates classic engine UI scaling:
    /// - UI is authored in a fixed "virtual" resolution (baseResolution)
    /// - The whole UI is uniformly scaled to fit the current screen (letterboxed)
    /// This keeps bitmap-font glyphs visually stable at any window size.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class C2VirtualScaler : MonoBehaviour
    {
        public Vector2Int baseResolution = new Vector2Int(1024, 768);

        [Tooltip("If enabled, rounds the letterbox offset to whole pixels to avoid subpixel blur.")]
        public bool pixelSnapOffset = true;

        [Tooltip("If enabled, rounds the scale to 1/1000 steps to reduce jitter while resizing.")]
        public bool stabilizeScale = true;

        private RectTransform _rt;

        private void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null) return;

            int bw = Mathf.Max(1, baseResolution.x);
            int bh = Mathf.Max(1, baseResolution.y);

            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);

            float scale = Mathf.Min(sw / bw, sh / bh);
            if (stabilizeScale)
                scale = Mathf.Round(scale * 1000f) / 1000f;

            // Size of scaled virtual surface in screen pixels
            float vw = bw * scale;
            float vh = bh * scale;

            // Letterbox offsets in screen pixels (from bottom-left)
            float offX = (sw - vw) * 0.5f;
            float offY = (sh - vh) * 0.5f;

            if (pixelSnapOffset)
            {
                offX = Mathf.Round(offX);
                offY = Mathf.Round(offY);
            }

            // Our RectTransform is anchored to center, so anchoredPosition is offset from screen center.
            float centerX = sw * 0.5f;
            float centerY = sh * 0.5f;

            // Position of the virtual surface center in screen pixels:
            float surfCenterX = offX + vw * 0.5f;
            float surfCenterY = offY + vh * 0.5f;

            // Convert to anchoredPosition relative to screen center.
            _rt.anchoredPosition = new Vector2(surfCenterX - centerX, surfCenterY - centerY);
            _rt.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
