using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    /// <summary>
    /// Базовый класс с общей логикой рендеринга UI
    /// </summary>
    public abstract class BaseUiRenderer
    {
        public sealed class RenderOptions
        {
            public string FontResourcePath = "Fonts/Slovic";
            public float FontSize = 29f;

            public Color32 NormalColor = new Color32(40, 10, 10, 255);
            public Color32 HoverColor = new Color32(95, 30, 30, 255);
            public Color32 DisabledColor = new Color32(90, 90, 90, 255);

            public CanvasScaler.ScaleMode CanvasScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            public Vector2 ReferenceResolution = new Vector2(1024, 768);
            public bool VerboseLogs = true;
            public bool DrawDebugOutline = false;
        }

        protected static int RenderCounter = 0;

        private static readonly string[] AllCanvasNames =
        {
            "C2_MainMenuCanvas",
            "C2_OptionsCanvas",
            "C2_MenuCanvas"
        };

        public abstract void Render(UiDesk desk, CoreFileSystem fs, RenderOptions opt, IUiActionSink sink, LocDb loc);

        #region Canvas Setup

        protected static RectTransform CreateCanvas(string canvasName, RenderOptions opt)
        {
            foreach (var name in AllCanvasNames)
            {
                var old = GameObject.Find(name);
                if (old != null)
                {
                    Log(opt, $"[BaseRenderer] Destroying old canvas: {name}");
                    UnityEngine.Object.Destroy(old);
                }
            }

            var canvasGO = new GameObject(canvasName);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 1000;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = opt.CanvasScaleMode;
            scaler.referenceResolution = opt.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var gr = canvasGO.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = true;
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            var root = canvasGO.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            EnsureEventSystem(opt);

            return root;
        }

        protected static void EnsureEventSystem(RenderOptions opt)
        {
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
                Log(opt, "[BaseRenderer] Created EventSystem");
            }
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Log(opt, "[BaseRenderer] Added InputSystemUIInputModule");
            }
#else
            if (UnityEngine.Object.FindFirstObjectByType<StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
                Log(opt, "[BaseRenderer] Added StandaloneInputModule");
            }
#endif
        }

        #endregion

        #region Common Element Creation

        protected static void CreateBitPicture(RectTransform parent, UiBitPicture pic, CoreFileSystem fs, RenderOptions opt)
        {
            var go = new GameObject($"BitPicture_{SafeName(pic.FileName)}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(pic.X, -pic.Y);
            rt.sizeDelta = new Vector2(pic.Width, pic.Height);

            var img = go.AddComponent<RawImage>();
            img.raycastTarget = false;

            try
            {
                byte[] bytes = fs.ReadAllBytes(pic.FileName);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);
                img.texture = tex;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BaseRenderer] Failed to load image '{pic.FileName}': {e.Message}");
            }
        }

        protected static void CreateTextButton(
            RectTransform parent,
            UiTextButton btn,
            RenderOptions opt,
            IUiActionSink sink,
            LocDb loc,
            Func<string, LocDb, bool, string> textResolver = null)

        {
            // --- SKIP debug keys like "#MO_..." ---
            string resolved = textResolver != null
                ? textResolver(btn.MessageKey, loc, opt.VerboseLogs)
                : loc?.Resolve(btn.MessageKey) ?? btn.MessageKey;

            // убираем строки вида "#MO_ArcadeMode", "#MO_...."
            if (!string.IsNullOrEmpty(resolved) && resolved.StartsWith("#MO_", StringComparison.OrdinalIgnoreCase))
                return;
            var go = new GameObject($"TextButton_{SafeName(btn.MessageKey)}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(btn.X, -btn.Y);
            rt.sizeDelta = new Vector2(btn.Width, btn.Height);

            var image = go.AddComponent<Image>();
            image.raycastTarget = true;
            image.color = opt.DrawDebugOutline ? new Color(1f, 0f, 0f, 0.12f) : new Color(1f, 1f, 1f, 0f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = btn.Enabled;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);

            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            string textResolved = resolved;

            bool isVersionLine = LooksLikeVersionString(textResolved);

            if (isVersionLine)
            {
                var uiText = textGO.AddComponent<Text>();
                uiText.raycastTarget = false;
                uiText.text = textResolved;
                uiText.font = Resources.Load<Font>("Fonts/arial") ?? Resources.Load<Font>("Fonts/arialbd");
                uiText.fontSize = 10;
                uiText.fontStyle = FontStyle.Normal;
                uiText.color = Color.white;
                uiText.alignment = TextAnchor.MiddleRight;
                uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
                uiText.verticalOverflow = VerticalWrapMode.Overflow;
            }
            else
            {
                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;

                tmp.alignment =
                    (btn.Style == UiTextStyle.OptionLabel || btn.Style == UiTextStyle.SectionTitle)
                        ? TextAlignmentOptions.MidlineLeft
                        : TextAlignmentOptions.Center;

                tmp.richText = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.text = textResolved;

                ApplyTextStyle(tmp, btn.Style, opt);

            // Special-case: nickname label in Multiplayer screen ("Имя игрока:")
            if (!string.IsNullOrEmpty(textResolved) &&
                textResolved.IndexOf("имя игрока", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ApplyFontFromPath(tmp, OptionsTextStyleConfig.OptionLabel.FontPath);
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Normal;
                tmp.fontWeight = FontWeight.Regular;
            }

                var hover = go.AddComponent<TmpHoverStyle>();
                hover.Target = tmp;
                hover.Interactable = btn.Enabled;
                hover.Id = btn.MessageKey;
                hover.Verbose = opt.VerboseLogs;

                ConfigureHoverColors(hover, btn.Style, opt);
            }

            button.onClick.AddListener(() =>
            {
                //Debug.Log($"[BaseRenderer] CLICK '{btn.MessageKey}' (actions={btn.Actions.Count})");
                foreach (var a in btn.Actions)
                {
                    try { sink?.OnAction(btn.MessageKey, a); }
                    catch (Exception e) { Debug.LogError($"[BaseRenderer] Action error: {e}"); }
                }
            });
        }

        #endregion

        #region Style Application

        /// <summary>
        /// Применяет стиль шрифта к TextMeshPro элементу
        /// </summary>
        protected static void ApplyTextStyle(TextMeshProUGUI tmp, UiTextStyle style, RenderOptions opt)
        {
            // Сразу выключаем autosize, чтобы размер/стиль реально применялись
            tmp.enableAutoSizing = false;

            // Диагностика
            //Debug.Log($"[ApplyTextStyle] style={style}");
            //Debug.Log($"[ApplyTextStyle PRE] name='{tmp.name}' style={style} size={tmp.fontSize} fontStyle={tmp.fontStyle} weight={tmp.fontWeight} scale={tmp.rectTransform.localScale}");

            switch (style)
            {
                case UiTextStyle.SectionTitle:
                    {
                        ApplyFontFromPath(tmp, OptionsTextStyleConfig.SectionTitle.FontPath);

                        tmp.fontSize = OptionsTextStyleConfig.SectionTitle.FontSize;
                        tmp.color = OptionsTextStyleConfig.SectionTitle.Color;
                        tmp.characterSpacing = OptionsTextStyleConfig.SectionTitle.CharacterSpacing;

                        tmp.fontStyle = OptionsTextStyleConfig.SectionTitle.Bold ? FontStyles.Bold : FontStyles.Normal;
                        tmp.fontWeight = OptionsTextStyleConfig.SectionTitle.Bold ? FontWeight.Bold : FontWeight.Regular;
                        break;
                    }

                case UiTextStyle.OptionLabel:
                    {
                        //Debug.Log($"[OptionLabel dbg PRE] color={tmp.color} alpha={tmp.alpha} " +
                        //$"crAlpha={tmp.canvasRenderer.GetAlpha()} " +
                        //$"parentCanvasGroupAlpha={(tmp.GetComponentInParent<CanvasGroup>()?.alpha.ToString() ?? "none")}");

                        ApplyFontFromPath(tmp, OptionsTextStyleConfig.OptionLabel.FontPath);

                        tmp.fontSize = OptionsTextStyleConfig.OptionLabel.FontSize;
                        tmp.color = OptionsTextStyleConfig.OptionLabel.Color;
                        tmp.characterSpacing = OptionsTextStyleConfig.OptionLabel.CharacterSpacing;

                        // базово — обычный, финальный проход ниже усилит
                        tmp.fontStyle = FontStyles.Normal;
                        tmp.fontWeight = FontWeight.Regular;

                        // --- Anti "grey/muddy" TMP material pass ---
                        var shared = tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial : tmp.fontMaterial;
                        var mat = new Material(shared);
                        tmp.fontSharedMaterial = mat;

                        tmp.alpha = 1f;
                        tmp.canvasRenderer.SetAlpha(1f);

                        mat.SetColor(TMPro.ShaderUtilities.ID_FaceColor, Color.white);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate, 0f);

                        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0f);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineSoftness, 0f);
                        mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color32(0, 0, 0, 0));

                        mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayDilate, 0f);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlaySoftness, 0f);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetX, 0f);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetY, 0f);

                        tmp.ForceMeshUpdate();
                        break;
                    }

                case UiTextStyle.MainMenuTitle:
                    {
                        ApplyFontFromPath(tmp, OptionsTextStyleConfig.MainMenuTitle.FontPath);

                        tmp.fontSize = OptionsTextStyleConfig.MainMenuTitle.FontSize;
                        tmp.color = OptionsTextStyleConfig.MainMenuTitle.Color;
                        tmp.characterSpacing = OptionsTextStyleConfig.MainMenuTitle.CharacterSpacing;

                        tmp.fontStyle = FontStyles.Normal;
                        tmp.fontWeight = FontWeight.Regular;

                        var mat = tmp.fontMaterial;
                        mat.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate, -0.1f);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0f);
                        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineSoftness, 0f);
                        break;
                    }

                case UiTextStyle.WindowTitle:
                    {
                        ApplyFontFromPath(tmp, OptionsTextStyleConfig.WindowTitle.FontPath);

                        tmp.fontSize = OptionsTextStyleConfig.WindowTitle.FontSize;
                        tmp.color = OptionsTextStyleConfig.WindowTitle.Color;
                        tmp.characterSpacing = OptionsTextStyleConfig.WindowTitle.CharacterSpacing;

                        tmp.fontStyle = OptionsTextStyleConfig.WindowTitle.Bold ? FontStyles.Bold : FontStyles.Normal;
                        tmp.fontWeight = OptionsTextStyleConfig.WindowTitle.Bold ? FontWeight.Bold : FontWeight.Regular;
                        break;
                    }

                case UiTextStyle.GoldenTitle:
                    {
                        ApplyFontFromPath(tmp, OptionsTextStyleConfig.GoldenTitle.FontPath);

                        tmp.fontSize = OptionsTextStyleConfig.GoldenTitle.FontSize;
                        tmp.color = OptionsTextStyleConfig.GoldenTitle.Color;
                        tmp.characterSpacing = OptionsTextStyleConfig.GoldenTitle.CharacterSpacing;

                        tmp.fontStyle = FontStyles.Normal;
                        tmp.fontWeight = FontWeight.Regular;
                        break;
                    }

                case UiTextStyle.Button:
                    {
                        ApplyFontFromPath(tmp, OptionsTextStyleConfig.Button.FontPath);

                        tmp.fontSize = OptionsTextStyleConfig.Button.FontSize;
                        tmp.color = OptionsTextStyleConfig.Button.NormalColor;
                        tmp.characterSpacing = OptionsTextStyleConfig.Button.CharacterSpacing;

                        tmp.fontStyle = FontStyles.Normal;
                        tmp.fontWeight = FontWeight.Regular;
                        break;
                    }

                case UiTextStyle.Default:
                default:
                    {
                        ApplyFontFromPath(tmp, opt.FontResourcePath);

                        tmp.fontSize = opt.FontSize;
                        tmp.color = opt.NormalColor;

                        tmp.fontStyle = FontStyles.Normal;
                        tmp.fontWeight = FontWeight.Regular;
                        break;
                    }
            }

            // --- Final readability tweaks (после базовой установки из конфигов) ---
            switch (style)
            {
                case UiTextStyle.OptionLabel:
                    {
                        tmp.fontSize = Mathf.Round(tmp.fontSize * 1.2f); // +12%

                        // --- ЖИРНОСТЬ через множитель ---
                        float mul = Mathf.Clamp(OptionsTextStyleConfig.OptionLabel.BoldMul, 0f, 1.5f);

                        if (mul <= 0.01f)
                        {
                            tmp.fontStyle = FontStyles.Normal;
                            tmp.fontWeight = FontWeight.Regular;

                            // убрать утолщение из материала, если оно было
                            var m0 = tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial : tmp.fontMaterial;
                            if (m0 != null) m0.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate, 0f);
                        }
                        else
                        {
                            tmp.fontStyle |= FontStyles.Bold;
                            tmp.fontWeight = FontWeight.Bold;

                            // плавное “утолщение” (множитель)
                            var m1 = tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial : tmp.fontMaterial;
                            if (m1 != null) m1.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate, 0.15f * mul);
                        }

                        // "растяжка"
                        tmp.characterSpacing = Mathf.Max(tmp.characterSpacing, 2.0f);
                        tmp.ForceMeshUpdate();
                        break;
                    }

                case UiTextStyle.SectionTitle:
                    {
                        tmp.fontSize = Mathf.Round(tmp.fontSize * 1.08f); // +8%
                        tmp.fontStyle |= FontStyles.Bold;
                        tmp.fontWeight = FontWeight.Bold;

                        tmp.characterSpacing = Mathf.Max(tmp.characterSpacing, 1.5f);
                        tmp.ForceMeshUpdate();
                        break;
                    }
            }

            //Debug.Log($"[StyleFinal {style}] size={tmp.fontSize} style={tmp.fontStyle} weight={tmp.fontWeight} spacing={tmp.characterSpacing}");
            //Debug.Log($"[ApplyTextStyle POST] name='{tmp.name}' style={style} size={tmp.fontSize} fontStyle={tmp.fontStyle} weight={tmp.fontWeight} scale={tmp.rectTransform.localScale}");
        }


        /// <summary>
        /// Настраивает цвета hover эффекта
        /// </summary>
        protected static void ConfigureHoverColors(TmpHoverStyle hover, UiTextStyle style, RenderOptions opt)
        {
            switch (style)
            {
                // Заголовки не реагируют на hover
                case UiTextStyle.SectionTitle:
                case UiTextStyle.WindowTitle:
                case UiTextStyle.MainMenuTitle:
                case UiTextStyle.GoldenTitle:
                    hover.Normal = hover.Target.color;
                    hover.Hover = hover.Target.color;
                    hover.Disabled = hover.Target.color;
                    hover.enabled = false;
                    break;

                case UiTextStyle.OptionLabel:
                    hover.Normal = OptionsTextStyleConfig.OptionLabel.Color;
                    hover.Hover = OptionsTextStyleConfig.OptionLabel.Color;
                    hover.Disabled = OptionsTextStyleConfig.OptionLabel.Color;
                    break;

                case UiTextStyle.Button:
                    hover.Normal = OptionsTextStyleConfig.Button.NormalColor;
                    hover.Hover = OptionsTextStyleConfig.Button.HoverColor;
                    hover.Disabled = OptionsTextStyleConfig.Button.DisabledColor;
                    break;

                case UiTextStyle.Default:
                default:
                    hover.Normal = opt.NormalColor;
                    hover.Hover = opt.HoverColor;
                    hover.Disabled = opt.DisabledColor;
                    break;
            }
        }

        protected static void ApplyFontFromPath(TextMeshProUGUI tmp, string fontPath)
        {
            var fontAsset = Resources.Load<TMP_FontAsset>(fontPath);
            if (fontAsset != null)
            {
                tmp.font = fontAsset;
                return;
            }

            var font = Resources.Load<Font>(fontPath);
            if (font != null)
            {
                var tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    tmp.font = tmpFont;
                    return;
                }
            }

            Debug.LogWarning($"[BaseRenderer] Font not found: '{fontPath}'");
        }

        #endregion

        #region Utilities

        protected static bool LooksLikeVersionString(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();

            if (s.StartsWith("TP", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.StartsWith("Version", StringComparison.OrdinalIgnoreCase)) return true;

            int lb = s.LastIndexOf('[');
            int rb = s.LastIndexOf(']');
            if (lb >= 0 && rb > lb + 1)
            {
                string inside = s.Substring(lb + 1, rb - lb - 1).Trim();
                if (inside.Length == 4)
                {
                    foreach (char c in inside)
                        if (!Uri.IsHexDigit(c)) return false;
                    return true;
                }
            }
            return false;
        }

        protected static string SafeName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "EMPTY";
            s = s.Replace("#", "").Replace(" ", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return s.Length > 64 ? s.Substring(0, 64) : s;
        }

        protected static void Log(RenderOptions opt, string msg)
        {
            if (opt?.VerboseLogs == true) Debug.Log(msg);
        }

        #endregion
    }

    /// <summary>
    /// Hover effect component
    /// </summary>
    public sealed class TmpHoverStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text Target;
        public bool Interactable = true;
        public Color32 Normal, Hover, Disabled;
        public bool Verbose;
        public string Id = "";

        void OnEnable() => ApplyCurrent();
        void OnDisable() => ApplyCurrent();

        void ApplyCurrent()
        {
            if (Target) Target.color = Interactable ? Normal : Disabled;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (Verbose) Debug.Log($"[Hover] ENTER '{Id}'");
            if (Target) Target.color = Interactable ? Hover : Disabled;
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (Verbose) Debug.Log($"[Hover] EXIT '{Id}'");
            ApplyCurrent();
        }
    }
}