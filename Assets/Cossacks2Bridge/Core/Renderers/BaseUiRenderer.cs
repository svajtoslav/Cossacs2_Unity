using System;
using System.Collections.Generic;  // <-- ДОБАВИТЬ для List<>
using System.Linq;
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
        // Single menu tuning (можно менять прямо в файле)
        public static float SingleCurrentPlayerLabelOpacity = 0.70f; // 0..1
        public static float SingleTitleFontSize = 18f;
        public static float SingleTitleCharacterSpacing = 33f;
        public static FontWeight SingleTitleFontWeight = FontWeight.Thin;

        // Lightweight Resources sprite cache (cannot reuse OptionsRenderer.ResFrames because it is private).
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> _resSpriteCache = new();

        private static Sprite GetResFrame(string folder, string frameName)
        {
            string key = folder + "/" + frameName;
            if (_resSpriteCache.TryGetValue(key, out var sp) && sp != null) return sp;

            // Try Sprite first (if imported as Sprite), then Texture2D.
            sp = Resources.Load<Sprite>(key);
            if (sp == null)
            {
                var tex = Resources.Load<Texture2D>(key);
                if (tex == null) return null;
                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
                sp.name = frameName;
            }

            _resSpriteCache[key] = sp;
            return sp;
        }
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
            public bool FillResolutionCombos = true; // только для Options
        }

        /// <summary>
        /// Структура границы (аналог Corners из C++)
        /// </summary>
        [Serializable]
        public struct BorderCorners
        {
            public int CLU, CRU, CLD, CRD;  // Углы
            public int LL, LR, LU, LD;      // Линии
            public int FillerStart;         // Начальный индекс наполнителя
            public int FillerCount;         // Количество вариантов наполнителя

            public static BorderCorners BD => new BorderCorners
            {
                CLU = 2,
                CRU = 3,
                CLD = 0,
                CRD = 1,
                LL = 8,
                LR = 7,
                LU = 4,
                LD = 5,
                FillerStart = 6,
                FillerCount = 1
            };
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
            string resolved = textResolver != null
                ? textResolver(btn.MessageKey, loc, opt.VerboseLogs)
                : loc?.Resolve(btn.MessageKey) ?? btn.MessageKey;

            if (!string.IsNullOrEmpty(resolved) && resolved.StartsWith("#MO_", StringComparison.OrdinalIgnoreCase))
                return;

            bool isAddProfileDesc = btn?.Actions != null && btn.Actions.Any(a => a != null &&
                !string.IsNullOrEmpty(a.Name) && a.Name.Equals("cva_ProfAdd_Desc", StringComparison.OrdinalIgnoreCase));

            var go = new GameObject(isAddProfileDesc ? "TextButton_ProfAdd_Desc" : $"TextButton_{SafeName(btn.MessageKey)}");
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

            // For AddProfile commander description we build a ScrollRect (1=1 behavior: long text scrolls inside area).
            GameObject labelParent = go;
            ScrollRect scrollRect = null;
            if (isAddProfileDesc)
            {
                scrollRect = go.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 20f;

                var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportGO.transform.SetParent(go.transform, false);
                var vrt = (RectTransform)viewportGO.transform;
                vrt.anchorMin = Vector2.zero;
                vrt.anchorMax = Vector2.one;
                vrt.offsetMin = new Vector2(0, 0);
                vrt.offsetMax = new Vector2(-16, 0); // leave space for scrollbar
                viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0f);

                var contentGO = new GameObject("Content", typeof(RectTransform));
                contentGO.transform.SetParent(viewportGO.transform, false);
                var crt = (RectTransform)contentGO.transform;
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0, 1);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(0, 0);

                scrollRect.viewport = vrt;
                scrollRect.content = crt;
                labelParent = contentGO;

                // Scrollbar (visual)
                var sbGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
                sbGO.transform.SetParent(go.transform, false);
                var sbRt = (RectTransform)sbGO.transform;
                sbRt.anchorMin = new Vector2(1, 0);
                sbRt.anchorMax = new Vector2(1, 1);
                sbRt.pivot = new Vector2(1, 1);
                sbRt.anchoredPosition = Vector2.zero;
                sbRt.sizeDelta = new Vector2(16, 0);

                var sbImg = sbGO.GetComponent<Image>();
                sbImg.color = Color.white;
                sbImg.sprite = GetResFrame("Interf3_elements_scroll3_frames", "frame_0004");
                sbImg.type = Image.Type.Sliced;

                var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handleGO.transform.SetParent(sbGO.transform, false);
                var hRt = (RectTransform)handleGO.transform;
                hRt.anchorMin = new Vector2(0.5f, 1);
                hRt.anchorMax = new Vector2(0.5f, 1);
                hRt.pivot = new Vector2(0.5f, 1);
                hRt.sizeDelta = new Vector2(14, 22);
                handleGO.GetComponent<Image>().sprite = GetResFrame("Interf3_elements_scroll3_frames", "frame_0005");
                handleGO.GetComponent<Image>().type = Image.Type.Sliced;

                var sb = sbGO.GetComponent<Scrollbar>();
                sb.direction = Scrollbar.Direction.BottomToTop;
                sb.handleRect = hRt;
                sb.targetGraphic = handleGO.GetComponent<Image>();

                scrollRect.verticalScrollbar = sb;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scrollRect.verticalScrollbarSpacing = 0;
            }

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(labelParent.transform, false);

            var trt = textGO.AddComponent<RectTransform>();
            if (isAddProfileDesc)
            {
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(1, 1);
                trt.pivot = new Vector2(0, 1);
                trt.anchoredPosition = Vector2.zero;
                trt.sizeDelta = new Vector2(0, 0);
            }
            else
            {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }

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
                tmp.alignment = (btn.Style == UiTextStyle.OptionLabel || btn.Style == UiTextStyle.SectionTitle)
                    ? TextAlignmentOptions.MidlineLeft
                    : TextAlignmentOptions.Center;
                tmp.richText = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;

                // FIX(AddProfile): commander description must be multiline and clipped
                if (isAddProfileDesc || (!string.IsNullOrEmpty(textResolved) &&
                    textResolved.StartsWith("Description:", StringComparison.OrdinalIgnoreCase)))
                {
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    tmp.overflowMode = TextOverflowModes.Truncate;
                }

                if (isAddProfileDesc)
                {
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    var fitter = textGO.AddComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                tmp.text = textResolved;

                ApplyTextStyle(tmp, btn.Style, opt);

                // FIX(AddProfile): exact fonts/colors for key labels & titles
                if (btn.MessageKey.Equals("#AddProfile_PlayerSettings", StringComparison.OrdinalIgnoreCase) ||
                    btn.MessageKey.Equals("#AddProfile_HistoricalSettings", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyFontFromPath(tmp, "Fonts/arial");
                    tmp.fontSize = 15;
                    tmp.color = new Color32(0x89, 0x10, 0x01, 0xFF);
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                }
                else if (btn.MessageKey.Equals("#AddProfile_ProfileName", StringComparison.OrdinalIgnoreCase) ||
                         btn.MessageKey.Equals("#AddProfile_Nation", StringComparison.OrdinalIgnoreCase) ||
                         btn.MessageKey.Equals("#AddProfile_Difficulty", StringComparison.OrdinalIgnoreCase) ||
                         btn.MessageKey.Equals("#ChangeProfile_Name", StringComparison.OrdinalIgnoreCase) ||
                         (!string.IsNullOrEmpty(textResolved) &&
                          (textResolved.IndexOf("имя игрока", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           textResolved.IndexOf("нация", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           textResolved.IndexOf("сложност", StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.OptionLabel.FontPath); // Slovic
                    tmp.fontSize = 12;
                    tmp.color = Color.black;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                }
                else if (btn.MessageKey.Equals("#AddProfile_Title", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyFontFromPath(tmp, "Fonts/seminaria");
                    tmp.fontSize = 16;
                    tmp.color = Color.white;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                    tmp.characterSpacing = 50; // +50%
                }
                else if (btn.MessageKey.Equals("#AddProfile_Window", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyFontFromPath(tmp, "Fonts/seminaria");
                    tmp.fontSize = 16;
                    tmp.color = Color.white;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                }
                else if (btn.MessageKey.Equals("#CUR_PROFILE:", StringComparison.OrdinalIgnoreCase) ||
                         btn.MessageKey.Equals("#CUR_PROFILE", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.OptionLabel.FontPath);
                    tmp.fontSize = 15;
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(SingleCurrentPlayerLabelOpacity * 255f), 0, 255);
                    tmp.color = new Color32(0, 0, 0, a);
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                }
                else if (btn.MessageKey.Equals("#MM_Single_Window", StringComparison.OrdinalIgnoreCase) &&
                         (btn.Actions == null || btn.Actions.Count == 0))
                {
                    // Только заголовок окна Single. Пункт главного меню с тем же ключом имеет action и не трогается.
                    ApplyFontFromPath(tmp, "Fonts/seminaria");
                    tmp.fontSize = SingleTitleFontSize;
                    tmp.characterSpacing = SingleTitleCharacterSpacing;
                    tmp.color = new Color32(255, 255, 255, 255);
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = SingleTitleFontWeight;
                }

                bool isStaticTitle =
                    btn.MessageKey.Equals("#AddProfile_Window", StringComparison.OrdinalIgnoreCase) ||
                    btn.MessageKey.Equals("#AddProfile_Title", StringComparison.OrdinalIgnoreCase) ||
                    btn.MessageKey.Equals("#CUR_PROFILE:", StringComparison.OrdinalIgnoreCase) ||
                    btn.MessageKey.Equals("#CUR_PROFILE", StringComparison.OrdinalIgnoreCase) ||
                    (btn.MessageKey.Equals("#MM_Single_Window", StringComparison.OrdinalIgnoreCase) &&
                     (btn.Actions == null || btn.Actions.Count == 0));

                if (isStaticTitle)
                {
                    // не реагировать на курсор
                    button.interactable = false;
                    button.transition = Selectable.Transition.None;
                    image.raycastTarget = false;
                    tmp.raycastTarget = false;
                }
                else
                {
                    var hover = go.AddComponent<TmpHoverStyle>();
                    hover.Target = tmp;
                    hover.Interactable = btn.Enabled;
                    hover.Id = btn.MessageKey;
                    hover.Verbose = false; // не спамить лог

                    ConfigureHoverColors(hover, btn.Style, opt);
                }

}

            button.onClick.AddListener(() =>
            {
                foreach (var a in btn.Actions)
                {
                    try { sink?.OnAction(btn.MessageKey, a); }
                    catch (Exception e) { Debug.LogError($"[BaseRenderer] Action error: {e}"); }
                }
            });
        }

        #endregion

        #region Tiled Border System

        /// <summary>
        /// Загружает текстуру фрейма границы
        /// </summary>
        protected static Texture2D LoadBorderFrame(string borderName, int frameIndex)
        {
            string path = $"interf3_elements_border_{borderName}_frames/frame_{frameIndex:D4}";
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
                Debug.LogWarning($"[Border] Frame not found: {path}");
            return tex;
        }

        /// <summary>
        /// Создаёт границу из тайлов (аналог DrawRect4 / DrawFilledRect)
        /// </summary>
        protected static void CreateTiledBorder(
            RectTransform parent,
            float x, float y, float width, float height,
            string borderName,
            BorderCorners corners,
            RenderOptions opt,
            bool filled = true)
        {
            const int TILE_SIZE = 32;

            var container = new GameObject($"Border_{borderName}");
            container.transform.SetParent(parent, false);

            var rt = container.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);

            float innerWidth = width - TILE_SIZE * 2;
            float innerHeight = height - TILE_SIZE * 2;

            // === УГЛЫ ===
            PlaceBorderTile(container.transform, borderName, corners.CLU,
                0, 0, TILE_SIZE, TILE_SIZE, "Corner_LU");
            PlaceBorderTile(container.transform, borderName, corners.CRU,
                width - TILE_SIZE, 0, TILE_SIZE, TILE_SIZE, "Corner_RU");
            PlaceBorderTile(container.transform, borderName, corners.CLD,
                0, height - TILE_SIZE, TILE_SIZE, TILE_SIZE, "Corner_LD");
            PlaceBorderTile(container.transform, borderName, corners.CRD,
                width - TILE_SIZE, height - TILE_SIZE, TILE_SIZE, TILE_SIZE, "Corner_RD");

            // === ГОРИЗОНТАЛЬНЫЕ ЛИНИИ ===
            if (corners.LU >= 0 && innerWidth > 0)
                CreateTiledLine(container.transform, borderName, corners.LU,
                    TILE_SIZE, 0, innerWidth, TILE_SIZE, true, "Line_Top");

            if (corners.LD >= 0 && innerWidth > 0)
                CreateTiledLine(container.transform, borderName, corners.LD,
                    TILE_SIZE, height - TILE_SIZE, innerWidth, TILE_SIZE, true, "Line_Bottom");

            // === ВЕРТИКАЛЬНЫЕ ЛИНИИ ===
            if (corners.LL >= 0 && innerHeight > 0)
                CreateTiledLine(container.transform, borderName, corners.LL,
                    0, TILE_SIZE, TILE_SIZE, innerHeight, false, "Line_Left");

            if (corners.LR >= 0 && innerHeight > 0)
                CreateTiledLine(container.transform, borderName, corners.LR,
                    width - TILE_SIZE, TILE_SIZE, TILE_SIZE, innerHeight, false, "Line_Right");

            // === НАПОЛНИТЕЛЬ ===
            if (filled && corners.FillerCount > 0 && innerWidth > 0 && innerHeight > 0)
                CreateFilledCenter(container.transform, borderName,
                    corners.FillerStart, corners.FillerCount,
                    TILE_SIZE, TILE_SIZE, innerWidth, innerHeight);
        }

        /// <summary>
        /// Размещает один тайл границы
        /// </summary>
        private static void PlaceBorderTile(
            Transform parent, string borderName, int frameIndex,
            float x, float y, float w, float h, string name)
        {
            if (frameIndex < 0) return;

            var tex = LoadBorderFrame(borderName, frameIndex);
            if (tex == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт тайлируемую линию
        /// </summary>
        private static void CreateTiledLine(
            Transform parent, string borderName, int frameIndex,
            float x, float y, float width, float height,
            bool horizontal, string name)
        {
            if (frameIndex < 0) return;

            var tex = LoadBorderFrame(borderName, frameIndex);
            if (tex == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);

            var mask = go.AddComponent<RectMask2D>();

            int tileSize = 32;
            int tilesNeeded = horizontal
                ? Mathf.CeilToInt(width / tileSize) + 1
                : Mathf.CeilToInt(height / tileSize) + 1;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}");
                tile.transform.SetParent(go.transform, false);

                var trt = tile.AddComponent<RectTransform>();
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                if (horizontal)
                    trt.anchoredPosition = new Vector2(i * tileSize, 0);
                else
                    trt.anchoredPosition = new Vector2(0, -i * tileSize);

                trt.sizeDelta = new Vector2(tileSize, tileSize);

                var img = tile.AddComponent<RawImage>();
                img.texture = tex;
                img.raycastTarget = false;
            }
        }

        /// <summary>
        /// Заполняет центр наполнителем
        /// </summary>
        private static void CreateFilledCenter(
            Transform parent, string borderName,
            int fillerStart, int fillerCount,
            float x, float y, float width, float height)
        {
            const int TILE_SIZE = 32;

            var go = new GameObject("Filler");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);

            var mask = go.AddComponent<RectMask2D>();

            var fillerTextures = new List<Texture2D>();
            for (int i = 0; i < fillerCount; i++)
            {
                var tex = LoadBorderFrame(borderName, fillerStart + i);
                if (tex != null) fillerTextures.Add(tex);
            }

            if (fillerTextures.Count == 0) return;

            int tilesX = Mathf.CeilToInt(width / TILE_SIZE) + 1;
            int tilesY = Mathf.CeilToInt(height / TILE_SIZE) + 1;

            for (int ix = 0; ix < tilesX; ix++)
            {
                for (int iy = 0; iy < tilesY; iy++)
                {
                    var tile = new GameObject($"Fill_{ix}_{iy}");
                    tile.transform.SetParent(go.transform, false);

                    var trt = tile.AddComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0, 1);
                    trt.anchorMax = new Vector2(0, 1);
                    trt.pivot = new Vector2(0, 1);
                    trt.anchoredPosition = new Vector2(ix * TILE_SIZE, -iy * TILE_SIZE);
                    trt.sizeDelta = new Vector2(TILE_SIZE, TILE_SIZE);

                    var img = tile.AddComponent<RawImage>();
                    int idx = (ix * ix + iy * iy * iy) % fillerTextures.Count;
                    img.texture = fillerTextures[idx];
                    img.raycastTarget = false;
                }
            }
        }

        #endregion

        #region Style Application

        protected static void ApplyTextStyle(TextMeshProUGUI tmp, UiTextStyle style, RenderOptions opt)
        {
            tmp.enableAutoSizing = false;

            switch (style)
            {
                case UiTextStyle.SectionTitle:
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.SectionTitle.FontPath);
                    tmp.fontSize = OptionsTextStyleConfig.SectionTitle.FontSize;
                    tmp.color = OptionsTextStyleConfig.SectionTitle.Color;
                    tmp.characterSpacing = OptionsTextStyleConfig.SectionTitle.CharacterSpacing;
                    tmp.fontStyle = OptionsTextStyleConfig.SectionTitle.Bold ? FontStyles.Bold : FontStyles.Normal;
                    tmp.fontWeight = OptionsTextStyleConfig.SectionTitle.Bold ? FontWeight.Bold : FontWeight.Regular;
                    break;

                case UiTextStyle.OptionLabel:
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.OptionLabel.FontPath);
                    tmp.fontSize = OptionsTextStyleConfig.OptionLabel.FontSize;
                    tmp.color = OptionsTextStyleConfig.OptionLabel.Color;
                    tmp.characterSpacing = OptionsTextStyleConfig.OptionLabel.CharacterSpacing;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;

                    var shared = tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial : tmp.fontMaterial;
                    var mat = new Material(shared);
                    tmp.fontSharedMaterial = mat;
                    tmp.alpha = 1f;
                    tmp.canvasRenderer.SetAlpha(1f);
                    mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
                    mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
                    mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                    mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
                    mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(0, 0, 0, 0));
                    mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                    mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                    tmp.ForceMeshUpdate();
                    break;

                case UiTextStyle.MainMenuTitle:
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.MainMenuTitle.FontPath);
                    tmp.fontSize = OptionsTextStyleConfig.MainMenuTitle.FontSize;
                    tmp.color = OptionsTextStyleConfig.MainMenuTitle.Color;
                    tmp.characterSpacing = OptionsTextStyleConfig.MainMenuTitle.CharacterSpacing;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                    var mat2 = tmp.fontMaterial;
                    mat2.SetFloat(ShaderUtilities.ID_FaceDilate, -0.1f);
                    mat2.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                    mat2.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
                    break;

                case UiTextStyle.WindowTitle:
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.WindowTitle.FontPath);
                    tmp.fontSize = OptionsTextStyleConfig.WindowTitle.FontSize;
                    tmp.color = OptionsTextStyleConfig.WindowTitle.Color;
                    tmp.characterSpacing = OptionsTextStyleConfig.WindowTitle.CharacterSpacing;
                    tmp.fontStyle = OptionsTextStyleConfig.WindowTitle.Bold ? FontStyles.Bold : FontStyles.Normal;
                    tmp.fontWeight = OptionsTextStyleConfig.WindowTitle.Bold ? FontWeight.Bold : FontWeight.Regular;
                    break;

                case UiTextStyle.GoldenTitle:
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.GoldenTitle.FontPath);
                    tmp.fontSize = OptionsTextStyleConfig.GoldenTitle.FontSize;
                    tmp.color = OptionsTextStyleConfig.GoldenTitle.Color;
                    tmp.characterSpacing = OptionsTextStyleConfig.GoldenTitle.CharacterSpacing;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                    break;

                case UiTextStyle.Button:
                    ApplyFontFromPath(tmp, OptionsTextStyleConfig.Button.FontPath);
                    tmp.fontSize = OptionsTextStyleConfig.Button.FontSize;
                    tmp.color = OptionsTextStyleConfig.Button.NormalColor;
                    tmp.characterSpacing = OptionsTextStyleConfig.Button.CharacterSpacing;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                    break;

                default:
                    ApplyFontFromPath(tmp, opt.FontResourcePath);
                    tmp.fontSize = opt.FontSize;
                    tmp.color = opt.NormalColor;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.fontWeight = FontWeight.Regular;
                    break;
            }

            // Final tweaks
            switch (style)
            {
                case UiTextStyle.OptionLabel:
                    tmp.fontSize = Mathf.Round(tmp.fontSize * 1.2f);
                    float mul = Mathf.Clamp(OptionsTextStyleConfig.OptionLabel.BoldMul, 0f, 1.5f);
                    if (mul <= 0.01f)
                    {
                        tmp.fontStyle = FontStyles.Normal;
                        tmp.fontWeight = FontWeight.Regular;
                        var m0 = tmp.fontSharedMaterial ?? tmp.fontMaterial;
                        if (m0 != null) m0.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
                    }
                    else
                    {
                        tmp.fontStyle |= FontStyles.Bold;
                        tmp.fontWeight = FontWeight.Bold;
                        var m1 = tmp.fontSharedMaterial ?? tmp.fontMaterial;
                        if (m1 != null) m1.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f * mul);
                    }
                    tmp.characterSpacing = Mathf.Max(tmp.characterSpacing, 2.0f);
                    tmp.ForceMeshUpdate();
                    break;

                case UiTextStyle.SectionTitle:
                    tmp.fontSize = Mathf.Round(tmp.fontSize * 1.08f);
                    tmp.fontStyle |= FontStyles.Bold;
                    tmp.fontWeight = FontWeight.Bold;
                    tmp.characterSpacing = Mathf.Max(tmp.characterSpacing, 1.5f);
                    tmp.ForceMeshUpdate();
                    break;
            }
        }

        protected static void ConfigureHoverColors(TmpHoverStyle hover, UiTextStyle style, RenderOptions opt)
        {
            switch (style)
            {
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

        #region ListDesk

        /// <summary>
        /// ListDesk: область списка с тайловой границей
        /// </summary>
        protected void CreateListDesk(UiListDesk ld, RectTransform root, RenderOptions opt)
        {
            if (ld == null || root == null) return;

            CreateTiledBorder(
                root,
                ld.X, ld.Y, ld.Width, ld.Height,
                "BD",
                BorderCorners.BD,
                opt,
                filled: true
            );

            Log(opt, $"[ListDesk] Created tiled border at ({ld.X},{ld.Y}) size {ld.Width}x{ld.Height}");
        }

        #endregion

    } // <-- КОНЕЦ класса BaseUiRenderer

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

} // <-- КОНЕЦ namespace