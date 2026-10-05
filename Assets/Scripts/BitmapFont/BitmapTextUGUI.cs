using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TemnyLess.BitmapFonts
{
    /// <summary>
    /// UGUI bitmap text (Cossacks2 style): each character -> CP1251 byte -> frameIndex -> sprite.
    /// Fixes:
    /// - NBSP (0xA0) treated as normal space
    /// - Spaces/newlines NEVER draw any sprite (no "black bar")
    /// - Default = draw sprites "as is": Color.white, no shadow
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BitmapTextUGUI : MonoBehaviour
    {
        public enum Align { Left, Center, Right }

        [Header("Font")]
        public BitmapFontFrames font;

        [Header("Layout")]
        public Align alignment = Align.Center;
        public bool useNativeSpriteSize = true;

        [Tooltip("Optional: force glyph height (px). If <=0 uses sprite height.")]
        public float forceGlyphHeightPx = 0f;

        [Header("Color (tint)")]
        [Tooltip("Keep WHITE if you want font frames 'as is' (no darkening).")]
        public Color color = Color.white;

        [Header("Shadow")]
        public bool enableShadow = false;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.65f);
        public Vector2 shadowOffset = new Vector2(1f, -1f);

        [Header("Whitespace")]
        [Tooltip("Width for spaces (px).")]
        public float spaceWidthPx = 10f;

        [Header("Advanced")]
        [Tooltip("If parent is scaled non-uniformly (X != Y), counter-scale this object so glyphs aren't squashed.")]
        public bool fixNonUniformParentScale = true;

        private readonly List<Image> _glyphs = new List<Image>(128);
        private readonly List<Image> _shadows = new List<Image>(128);

        private void LateUpdate()
        {
            // UI hierarchies sometimes apply non-uniform scaling to fit aspect.
            // Bitmap glyphs must stay 1:1, so we counter-scale this container.
            ApplyParentScaleFix();
        }

        private void ApplyParentScaleFix()
        {
            if (!fixNonUniformParentScale) return;
            var p = transform.parent as RectTransform;
            if (p == null) return;

            var ps = p.lossyScale;
            if (ps.x == 0f || ps.y == 0f) return;

            // Only counter when clearly non-uniform.
            if (Mathf.Abs(ps.x - ps.y) < 0.0001f)
            {
                // Ensure we don't keep a stale inverse if hierarchy changes back to uniform.
                if (transform.localScale != Vector3.one)
                    transform.localScale = Vector3.one;
                return;
            }

            transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f);
        }

    private float GetCanvasRefPPU()
    {
        var c = GetComponentInParent<UnityEngine.Canvas>();
        if (c == null) return 100f;
        var scaler = c.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler != null) return scaler.referencePixelsPerUnit;
        return 100f;
    }

    private float GetDefaultUnitPerPixel(TemnyLess.BitmapFonts.BitmapFontFrames font)
    {
        try
        {
            // Try common glyphs
			Sprite s = null;
			if (font != null)
			{
				// BitmapFontFrames.TryGetSprite uses (int frameIndex, out Sprite)
				if (!font.TryGetSprite((int)'A', out s) || s == null)
					if (!font.TryGetSprite((int)'a', out s) || s == null)
						font.TryGetSprite((int)'0', out s);
			}
            if (s == null) return 1f;
            var refPpu = GetCanvasRefPPU();
            if (s.pixelsPerUnit <= 0f) return 1f;
            return refPpu / s.pixelsPerUnit;
        }
        catch { return 1f; }
    }


        public void SetColor(Color c)
        {
            color = c;
            for (int i = 0; i < _glyphs.Count; i++)
                if (_glyphs[i] != null) _glyphs[i].color = color;
        }

        public void SetShadow(bool enabled, Color c, Vector2 offset)
        {
            enableShadow = enabled;
            shadowColor = c;
            shadowOffset = offset;

            for (int i = 0; i < _shadows.Count; i++)
            {
                var sh = _shadows[i];
                if (sh == null) continue;
                sh.color = shadowColor;
                bool on = enableShadow && sh.sprite != null;
                sh.enabled = on;
                sh.gameObject.SetActive(on);
            }
        }

        public void SetText(string s)
        {
            if (font == null)
            {
                Debug.LogWarning("[BitmapTextUGUI] font is null");
                Clear();
                return;
            }

            font.EnsureLoaded();
            ApplyParentScaleFix();

            float unitPerPixel = GetDefaultUnitPerPixel(font);
            float letterSpacing = font.LetterSpacingPx * unitPerPixel;
            float spaceWidth = spaceWidthPx * unitPerPixel;


            if (string.IsNullOrEmpty(s))
            {
                Clear();
                return;
            }

            // Normalize common "spaces" BEFORE encoding to cp1251
            s = s
                .Replace('\u00A0', ' ') // NBSP
                .Replace('\u2007', ' ') // Figure space
                .Replace('\u202F', ' ') // Narrow NBSP
                .Replace('\t', ' ');

            // Convert to CP1251 bytes
            var bytes = Cp1251.Encode(s);

            EnsureGlyphCount(bytes.Length);

            // pass 1: assign sprites + measure
            float totalW = 0f;
            float maxH = 0f;

            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];

                bool isSpace = (b == 0x20 || b == 0x09 || b == 0xA0);
                bool isNewline = (b == 0x0A || b == 0x0D);

                var img = _glyphs[i];
                var sh = (i < _shadows.Count) ? _shadows[i] : null;

                Sprite sp = null;
                if (!isSpace && !isNewline)
                {
                    if (!font.TryGetSprite(b, out sp) || sp == null)
                        font.TryGetSprite((int)'?', out sp);
                }

                // IMPORTANT: spaces/newlines draw nothing at all
                if (isSpace || isNewline || sp == null)
                {
                    img.sprite = null;
                    img.enabled = false;
                    img.color = color;

                    if (sh != null)
                    {
                        sh.sprite = null;
                        sh.enabled = false;
                        sh.gameObject.SetActive(false);
                    }
                }
                else
                {
                    img.enabled = true;
                    img.sprite = sp;
                    img.color = color; // keep Color.white for "as is"
                    img.raycastTarget = false;
                    img.preserveAspect = true;

                    if (sh != null)
                    {
                        sh.sprite = sp;
                        sh.color = shadowColor;
                        sh.raycastTarget = false;
                        sh.preserveAspect = true;
                        bool on = enableShadow;
                        sh.enabled = on;
                        sh.gameObject.SetActive(on);
                    }
                }

                float w = 0f;
                float h = 0f;

                if (sp != null && !isSpace && !isNewline)
                {
                    var r = sp.rect;
                    w = r.width;
                    h = r.height;
                }
                else if (isSpace)
                {
                    w = spaceWidthPx;
                    h = 0f;
                }

                if (forceGlyphHeightPx > 0f && h > 0f)
                {
                    float k = (forceGlyphHeightPx * unitPerPixel) / h;
                    w *= k;
                    h = forceGlyphHeightPx;
                }

                totalW += w;
                if (i != bytes.Length - 1) totalW += font.LetterSpacingPx;
                if (h > maxH) maxH = h;
            }

            // alignment
            float startX = 0f;
            if (alignment == Align.Center) startX = -totalW * 0.5f;
            else if (alignment == Align.Right) startX = -totalW;

            // pass 2: layout
            float xPos = startX;

            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                bool isSpace = (b == 0x20 || b == 0x09 || b == 0xA0);
                bool isNewline = (b == 0x0A || b == 0x0D);

                var img = _glyphs[i];
                var sp = img.sprite;

                float w = 0f;
                float h = 0f;

                if (sp != null && !isSpace && !isNewline)
                {
                    var r = sp.rect;
                    w = r.width;
                    h = r.height;
                }
                else if (isSpace)
                {
                    w = spaceWidthPx;
                    h = 0f;
                }

                if (forceGlyphHeightPx > 0f && h > 0f)
                {
                    float k = (forceGlyphHeightPx * unitPerPixel) / h;
                    w *= k;
                    h = forceGlyphHeightPx;
                }

                var rt = (RectTransform)img.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(xPos + w * 0.5f, 0f);

                // keep native size; for non-drawing chars keep 0 size
                if (img.enabled && sp != null)
                {
                    if (useNativeSpriteSize) rt.sizeDelta = new Vector2(w, h);
                    else rt.sizeDelta = new Vector2(w, maxH);
                }
                else
                {
                    rt.sizeDelta = new Vector2(w, 0f);
                }

                var sh = (i < _shadows.Count) ? _shadows[i] : null;
                if (sh != null)
                {
                    var shRt = (RectTransform)sh.transform;
                    shRt.anchorMin = rt.anchorMin;
                    shRt.anchorMax = rt.anchorMax;
                    shRt.pivot = rt.pivot;
                    shRt.sizeDelta = rt.sizeDelta;
                    shRt.anchoredPosition = rt.anchoredPosition + shadowOffset;

                    bool shOn = enableShadow && img.enabled && sp != null;
                    sh.enabled = shOn;
                    sh.gameObject.SetActive(shOn);
                }

                xPos += w + font.LetterSpacingPx;
            }

            // disable unused
            for (int i = bytes.Length; i < _glyphs.Count; i++)
            {
                if (_glyphs[i] != null) _glyphs[i].gameObject.SetActive(false);
                if (i < _shadows.Count && _shadows[i] != null) _shadows[i].gameObject.SetActive(false);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < _glyphs.Count; i++)
            {
                if (_glyphs[i] != null)
                {
                    _glyphs[i].sprite = null;
                    _glyphs[i].enabled = false;
                    _glyphs[i].gameObject.SetActive(false);
                }

                if (i < _shadows.Count && _shadows[i] != null)
                {
                    _shadows[i].sprite = null;
                    _shadows[i].enabled = false;
                    _shadows[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsureGlyphCount(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (i < _glyphs.Count && _glyphs[i] != null)
                {
                    _glyphs[i].gameObject.SetActive(true);
                    continue;
                }

                var go = new GameObject($"g{i:D3}");
                go.transform.SetParent(transform, false);

                // Shadow (always created for stable hierarchy; enabled only when needed)
                var shGo = new GameObject("shadow");
                shGo.transform.SetParent(go.transform, false);
                var sh = shGo.AddComponent<Image>();
                sh.raycastTarget = false;
                sh.color = shadowColor;
                sh.enabled = false;
                shGo.SetActive(false);

                var shRt = (RectTransform)sh.transform;
                shRt.anchorMin = new Vector2(0.5f, 0.5f);
                shRt.anchorMax = new Vector2(0.5f, 0.5f);
                shRt.pivot = new Vector2(0.5f, 0.5f);

                // Main
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                img.color = color;
                img.enabled = false;

                _glyphs.Add(img);
                _shadows.Add(sh);
            }

            while (_shadows.Count < _glyphs.Count) _shadows.Add(null);

            for (int i = 0; i < _glyphs.Count && i < n; i++)
                if (_glyphs[i] != null) _glyphs[i].gameObject.SetActive(true);
        }
    }
}
