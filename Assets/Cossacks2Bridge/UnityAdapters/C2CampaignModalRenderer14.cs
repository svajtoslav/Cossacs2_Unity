using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.Core.Loaders;
using RawNode = Cossacks2Bridge.Core.Loaders.Menu14UnifiedLoader.LiteNode;
using TemnyLessViewer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// V396A7R5_UIA3_2 — source-driven renderer for the original first-entry Campaign modal
    /// from Dialogs/v/M_Single.DialogsSystem.xml.
    ///
    /// Visual contract:
    ///   * geometry, visibility, text keys, GP banks, sprite ids and button actions
    ///     come from M_Single.DialogsSystem.xml;
    ///   * StdBorder data comes from Dialogs/borders.xml;
    ///   * menu font aliases map to the original Cossacks II bitmap GP fonts and
    ///     colors from Dialogs/InitFonts.h (MenuText=FontG14, MenuTitle=FontG16,
    ///     MenuTitle2=FontG18);
    ///   * GP/font frames are loaded by Menu14ActionStateRuntime from the active
    ///     game DataRoot (with the project's clean game-data copy only as fallback).
    ///
    /// There is deliberately NO synthetic panel/button/color/text fallback here.
    /// If source data is absent, rendering fails loudly rather than inventing UI.
    /// </summary>
    public sealed class C2CampaignModalRenderer14
    {
        private const string SingleXml = @"Dialogs\v\M_Single.DialogsSystem.xml";
        private const string HelpXml = @"Dialogs\BM_Help.DialogsDesk.Dialogs.xml";
        private const string BordersXml = @"Dialogs\borders.xml";
        private const string FontsXml = @"Dialogs\fonts.xml";
        private const string TextIconsXml = @"Dialogs\TextIcons.xml";
        private const string CampaignDeskName = "Campaign";
        private const string CanvasName = "C2_BFE14_CampaignModal_XML_V396A7R5_UIA1";
        private const string HelpCanvasName = "C2_BFE14_BigMapHelp_XML_V396A7R5_UIA1";

        private GameObject _canvas;
        private CoreFileSystem _fs;
        private LocDb _loc;
        private IUiActionSink _sink;
        private readonly Dictionary<string, BorderSpec> _borders = new Dictionary<string, BorderSpec>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FontSpec> _fonts = new Dictionary<string, FontSpec>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FontSpec> _fontParams = new Dictionary<string, FontSpec>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TextIconSpec> _textIcons = new Dictionary<string, TextIconSpec>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sprite> _glyphCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        // V396A5: original GN16 font banks are decoded directly from the game's Cash files.
        // Melinoja's session cache can expose only a subset of compressed font segments; the
        // direct bank contains the full 256 CP1251 frames used by the original RLCFont path.
        private readonly Dictionary<string, C2DirectSpriteBank> _fontBanks = new Dictionary<string, C2DirectSpriteBank>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _fontBankAudit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // UIA3.2 diagnostic only: unique source FileID/frame misses for the current modal.
        // Rendering decisions still use the exact same UIA3.1 source path.
        private readonly HashSet<string> _missingAssetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _renderedControls;
        private int _missingAssets;
        private bool _helpMode;
        private Action _helpClosed;

        public bool IsOpen => _canvas != null;
        public bool IsHelpOpen => _canvas != null && _helpMode;

        public bool Show(CoreFileSystem fs, LocDb loc, IUiActionSink sink)
        {
            Close();
            _fs = fs;
            _loc = loc;
            _sink = sink;
            _renderedControls = 0;
            _missingAssets = 0;
            _missingAssetKeys.Clear();
            BuildFontTable();

            string singleRaw = ReadGameMenuText(SingleXml);
            if (string.IsNullOrWhiteSpace(singleRaw))
                return Fail("M_Single.DialogsSystem.xml is missing/empty");

            RawNode document = ParseSharedXml(singleRaw);
            RawNode campaign = FindDialogsDeskByName(document, CampaignDeskName);
            if (campaign == null)
                return Fail("DialogsDesk Name=Campaign not found in M_Single.DialogsSystem.xml");

            string bordersRaw = ReadGameMenuText(BordersXml);
            if (string.IsNullOrWhiteSpace(bordersRaw))
                return Fail("Dialogs/borders.xml is missing/empty");
            ParseBorders(ParseSharedXml(bordersRaw));

            int refW = Math.Max(1, campaign.Int("Width", 1024));
            int refH = Math.Max(1, campaign.Int("Height", 768));

            _canvas = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1400; // Unity layering only; not a visual style value.
            canvas.pixelPerfect = true;

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            int seamScale = ConfigureIntegerPixelCanvasUIA3(canvas, scaler);

            RectTransform screen = (RectTransform)_canvas.transform;
            screen.anchorMin = Vector2.zero;
            screen.anchorMax = Vector2.one;
            screen.offsetMin = Vector2.zero;
            screen.offsetMax = Vector2.zero;

            RectTransform root = CreatePixelStage(screen, "Campaign_SourceStage_1024x768", refW, refH);

            EnsureEventSystem();

            RawNode childDialogs = campaign.Child("ChildDialogs");
            if (childDialogs == null)
            {
                Close();
                return Fail("Campaign desk has no ChildDialogs");
            }

            // Campaign itself is Visible=false in the source because it is a modal desk.
            // Activating ModalDesk=Campaign makes THIS root visible; descendants retain
            // their own source Visible flags exactly.
            RenderChildDialogs(childDialogs, root, true);

            Debug.Log(
                $"[C2:BFE14 CAMPAIGN XML V396A7R5_UIA1] shown source='{SingleXml}' desk='{CampaignDeskName}' " +
                $"controls={_renderedControls} borders={_borders.Count} missingAssets={_missingAssets} " +
                "visuals=SOURCE_ONLY syntheticFallback=NO parser=Menu14UnifiedLoader.LiteDocument rawNodeParser=NO " +
                "cp1251=manual metrics=fonts.xml textBaseline=lineTop+metricHeight-Bottom textHeight=Bottom-Top lineAdvance=Height+YShift " +
                "pixelStage=1024x768 align=ProcessAligning+ROUND14 gpPictureSize=native_GP seamFix=UIA3_1_integerCanvas+screenDerivedStage+R7HeaderGuard pointFilter=YES " +
                $"screen={Screen.width}x{Screen.height} canvasScale={canvas.scaleFactor:0.###} integerScale={seamScale}");
            LogSeamExperimentUIA3("Campaign", root, canvas, seamScale);
            LogGpSourceAuditUIA3_2("Campaign");
            return true;
        }

        public bool ShowBigMapHelp(CoreFileSystem fs, LocDb loc, IUiActionSink sink, int page, Action onClosed)
        {
            Close();
            _fs = fs;
            _loc = loc;
            _sink = sink;
            _helpMode = true;
            _helpClosed = onClosed;
            _renderedControls = 0;
            _missingAssets = 0;
            _missingAssetKeys.Clear();
            BuildFontTable();

            string helpRaw = ReadGameMenuText(HelpXml);
            if (string.IsNullOrWhiteSpace(helpRaw))
                return Fail("BM_Help.DialogsDesk.Dialogs.xml is missing/empty");

            string bordersRaw = ReadGameMenuText(BordersXml);
            if (string.IsNullOrWhiteSpace(bordersRaw))
                return Fail("Dialogs/borders.xml is missing/empty");
            ParseBorders(ParseSharedXml(bordersRaw));
            ParseTextIcons();

            RawNode document = ParseSharedXml(helpRaw);
            int helpW = Math.Max(1, document.Int("Width", 880));
            int helpH = Math.Max(1, document.Int("Height", 680));

            _canvas = new GameObject(HelpCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            int seamScale = ConfigureIntegerPixelCanvasUIA3(canvas, scaler);

            RectTransform screen = (RectTransform)_canvas.transform;
            screen.anchorMin = Vector2.zero;
            screen.anchorMax = Vector2.one;
            screen.offsetMin = Vector2.zero;
            screen.offsetMax = Vector2.zero;
            RectTransform stage = CreatePixelStage(screen, "BigMapHelp_SourceStage_1024x768", 1024, 768);
            EnsureEventSystem();

            // Original CBigMapHelp::CreateElements: full-screen blackscreen GP with 0x88202020 diffuse.
            RectTransform blocker = CreateAbsoluteRect(stage, "BigMapHelp_BlackScreen", 0f, 0f, 1024f, 768f);
            Image blockerImage = blocker.gameObject.AddComponent<Image>();
            blockerImage.sprite = LoadGp(@"Interf3\TotalWarGraph\blackscr", 0);
            blockerImage.type = Image.Type.Simple;
            blockerImage.preserveAspect = false;
            blockerImage.color = new Color32(0x20, 0x20, 0x20, 0x88);
            blockerImage.raycastTarget = true;

            // Do not hard-center this desk.  The original XML uses
            // HorizontalCenterAlign=RelativeAlign(0.5) and
            // VerticalCenterAlign=RelativeAlign(0.48).  ProcessAligning() applies
            // those integer rules every frame; reproduce that source geometry here.
            RectTransform helpRoot = CreateRect(stage, "BigMapHelp_SourceRoot", document);

            RawNode childDialogs = document.Child("ChildDialogs");
            if (childDialogs == null)
            {
                Close();
                return Fail("BM_Help source has no ChildDialogs");
            }
            RenderChildDialogs(childDialogs, helpRoot, true);
            RenderBigMapHelpText(helpRoot, page, helpW, helpH);

            Debug.Log($"[C2:BFE14 HELP V396A7R5_UIA1] shown page={page} source='{HelpXml}' text='#BigMapHelp{page}' " +
                      $"controls={_renderedControls} borders={_borders.Count} icons={_textIcons.Count} missingAssets={_missingAssets} " +
                      "visuals=SOURCE_ONLY parser=Menu14UnifiedLoader.LiteDocument rawNodeParser=NO " +
                      "text=DrawMultilineSubset verticalMetrics=SOURCE_EXACT baseline=Height-Bottom lineAdvance=Height+YShift wrap=PARTIAL " +
                      "scroll=UnityScrollRect_content+original_GP_fixed_thumb_UIA2 pixelStage=1024x768 align=ProcessAligning_ROUND14 pointFilter=YES " +
                      $"screen={Screen.width}x{Screen.height} canvasScale={canvas.scaleFactor:0.###} integerScale={seamScale} seamFix=UIA3_1_integerCanvas+screenDerivedStage+R7HeaderGuard");
            LogSeamExperimentUIA3("Help", stage, canvas, seamScale);
            LogGpSourceAuditUIA3_2("Help");
            return true;
        }

        public void Close()
        {
            if (_canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas);
                _canvas = null;
            }
            _helpMode = false;
            _helpClosed = null;
        }

        private bool Fail(string reason)
        {
            Debug.LogError($"[C2:BFE14 SOURCE UI V396A7R5_UIA1] FAIL {reason}; syntheticFallback=NO");
            return false;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private void RenderChildDialogs(RawNode childDialogs, RectTransform parent, bool inheritedVisible)
        {
            if (childDialogs == null || parent == null) return;
            for (int i = 0; i < childDialogs.Children.Count; i++)
            {
                RawNode n = childDialogs.Children[i];
                if (!LooksLikeControl(n.Tag)) continue;
                RenderControl(n, parent, inheritedVisible);
            }
        }

        private void RenderControl(RawNode node, RectTransform parent, bool inheritedVisible)
        {
            if (node == null || parent == null) return;
            bool localVisible = node.Bool("Visible", true);
            bool visible = inheritedVisible && localVisible;
            if (!visible) return;

            string tag = node.Tag ?? string.Empty;
            if (tag.Equals("DialogsDesk", StringComparison.OrdinalIgnoreCase))
                RenderDialogsDesk(node, parent);
            else if (tag.Equals("GPPicture", StringComparison.OrdinalIgnoreCase))
                RenderGPPicture(node, parent);
            else if (tag.Equals("VitButton", StringComparison.OrdinalIgnoreCase))
                RenderVitButton(node, parent);
            else if (tag.Equals("TextButton", StringComparison.OrdinalIgnoreCase) || tag.Equals("Text", StringComparison.OrdinalIgnoreCase))
                RenderTextButton(node, parent);
            else
                RenderGenericContainer(node, parent);
        }

        private void RenderDialogsDesk(RawNode node, RectTransform parent)
        {
            RectTransform rt = CreateRect(parent, "DialogsDesk_" + Safe(node.Value("Name", string.Empty)), node);
            _renderedControls++;

            string border = node.Value("Border", string.Empty);
            if (!string.IsNullOrWhiteSpace(border) && !border.Equals("NullBorder", StringComparison.OrdinalIgnoreCase))
            {
                if (_borders.TryGetValue(border, out BorderSpec spec))
                    DrawFilledBorder(rt, Mathf.Max(1f, rt.sizeDelta.x), Mathf.Max(1f, rt.sizeDelta.y), spec);
                else
                    Debug.LogError($"[C2:BFE14 CAMPAIGN XML V396A7R5_UIA1] source border '{border}' not found in {BordersXml}");
            }

            RenderChildDialogs(node.Child("ChildDialogs"), rt, true);
        }

        private void RenderGenericContainer(RawNode node, RectTransform parent)
        {
            RectTransform rt = CreateRect(parent, (node.Tag ?? "Control") + "_" + Safe(node.Value("Name", string.Empty)), node);
            _renderedControls++;
            RenderChildDialogs(node.Child("ChildDialogs"), rt, true);
        }

        private void RenderGPPicture(RawNode node, RectTransform parent)
        {
            RectTransform rt = CreateRect(parent, "GPPicture_" + Safe(node.Value("FileID", string.Empty)), node);
            _renderedControls++;

            string fileId = node.Value("FileID", string.Empty);
            int spriteId = node.Int("SpriteID", -1);
            Sprite sp = LoadGp(fileId, spriteId);

            // Original GPPicture does not stretch a GP frame to XML Width/Height.
            // GPPicture_OnDraw / SetFileID reset x1/y1 from native GP dimensions.
            // Keep the aligned source position, but use the actual source frame size.
            if (sp != null)
                rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = ParseArgb(node.Value("Color", "FFFFFFFF"), Color.white);
            img.raycastTarget = HasAction(node, "cva_ClearClick");

            RenderChildDialogs(node.Child("ChildDialogs"), rt, true);
        }

        private void RenderVitButton(RawNode node, RectTransform parent)
        {
            RectTransform rt = CreateRect(parent, "VitButton_" + Safe(node.Value("Name", node.Value("Message", string.Empty))), node);
            _renderedControls++;

            string gp = node.Value("GP_File", string.Empty);
            int state = node.Int("State", 0);
            int passiveFrame = node.Int("SpritePassive" + state, -1);
            int overFrame = node.Int("SpriteOver" + state, -1);
            int spriteDx = node.Int("SpriteDx" + state, 0);
            bool oneSprited = node.Bool("OneSprited", false);
            bool disableCycling = node.Bool("DisableCycling", false);
            bool enabled = node.Bool("Enabled", true);

            float width = Mathf.Max(1f, rt.sizeDelta.x);
            float height = Mathf.Max(1f, rt.sizeDelta.y);

            GameObject passiveVisual = CreateVitVisual(rt, gp, passiveFrame, spriteDx, width, height, oneSprited, disableCycling, "Passive");
            GameObject overVisual = CreateVitVisual(rt, gp, overFrame, spriteDx, width, height, oneSprited, disableCycling, "Over");
            if (overVisual != null) overVisual.SetActive(false);

            List<SourceAction> actions = ParseActions(node);
            Button button = null;
            Image hit = null;
            if (actions.Count > 0)
            {
                hit = rt.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                hit.raycastTarget = true;
                button = rt.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                button.interactable = enabled;
            }

            RawNode messageNode = node.Child("Message");
            string messageKey = messageNode != null ? messageNode.DirectText() : string.Empty;
            GameObject label = null;
            if (!string.IsNullOrEmpty(messageKey))
            {
                string passiveFont = node.Value("FontPassive", string.Empty);
                string overFont = node.Value("FontOver", passiveFont);
                int fontDx = node.Int("FontDx", 0);
                int fontDy = node.Int("FontDy", 0);
                string align = node.Value("Align", "Center");

                string text = ResolveSourceText(messageKey);
                FontSpec normalFont = ResolveFont(passiveFont);
                float normalHeight = MeasureMultilineHeight(text, normalFont, width, align, out List<TextLine> normalLines);
                float normalY = Mathf.Floor(((height - 1f) - normalHeight) / 2f) + fontDy;
                label = CreateBitmapLines(rt, "ButtonText", text, normalFont, normalLines, fontDx, normalY, width, align);

                if (button != null)
                {
                    var hover = rt.gameObject.AddComponent<SourceButtonHover>();
                    hover.PassiveVisual = passiveVisual;
                    hover.OverVisual = overVisual;
                    hover.NormalTextRoot = label;
                    hover.Renderer = this;
                    hover.TextParent = rt;
                    hover.Text = text;
                    hover.PassiveFontName = passiveFont;
                    hover.OverFontName = overFont;
                    hover.FontDx = fontDx;
                    hover.FontDy = fontDy;
                    hover.Width = width;
                    hover.Height = height;
                    hover.Align = align;
                    hover.Enabled = enabled;
                }
            }
            else if (button != null)
            {
                var hover = rt.gameObject.AddComponent<SourceButtonHover>();
                hover.PassiveVisual = passiveVisual;
                hover.OverVisual = overVisual;
                hover.Enabled = enabled;
            }

            if (button != null)
            {
                string buttonKey = node.Value("Name", messageKey);
                button.onClick.AddListener(() =>
                {
                    if (_helpMode && IsHelpCloseAction(actions))
                    {
                        Action cb = _helpClosed;
                        Close();
                        cb?.Invoke();
                        return;
                    }
                    for (int i = 0; i < actions.Count; i++)
                    {
                        SourceAction a = actions[i];
                        _sink?.OnAction(buttonKey, new UiAction { Name = a.Name, Payload = a.Payload });
                    }
                });
            }

            RenderChildDialogs(node.Child("ChildDialogs"), rt, true);
        }

        private GameObject CreateVitVisual(RectTransform parent, string gp, int baseFrame, int spriteDx,
            float width, float height, bool oneSprited, bool disableCycling, string suffix)
        {
            if (baseFrame < 0 || string.IsNullOrWhiteSpace(gp)) return null;

            var visual = new GameObject("Visual_" + suffix, typeof(RectTransform));
            visual.transform.SetParent(parent, false);
            RectTransform vrt = (RectTransform)visual.transform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(0f, 1f);
            vrt.pivot = new Vector2(0f, 1f);
            vrt.anchoredPosition = new Vector2(spriteDx, 0f);
            vrt.sizeDelta = new Vector2(width, height);

            if (disableCycling)
            {
                Sprite sp = LoadGp(gp, baseFrame);
                if (sp != null) CreateSpriteAt(visual.transform, suffix + "_F" + baseFrame, sp, 0f, 0f);
            }
            else if (oneSprited)
            {
                DrawHeaderEx2(visual.transform, width, gp, -1, -1, baseFrame, baseFrame, baseFrame);
            }
            else
            {
                DrawHeaderEx2(visual.transform, width, gp,
                    baseFrame, baseFrame + 1, baseFrame + 2, baseFrame + 3, baseFrame + 4);
            }

            return visual;
        }

        private void RenderTextButton(RawNode node, RectTransform parent)
        {
            RectTransform rt = CreateRect(parent, "TextButton_" + Safe(node.Value("Message", string.Empty)), node);
            _renderedControls++;

            string messageKey = node.Value("Message", string.Empty);
            string text = ResolveSourceText(messageKey);
            string fontName = node.Value("PassiveFont", node.Value("ActiveFont", string.Empty));
            FontSpec font = ResolveFont(fontName);
            string align = node.Value("Align", "Left");
            int maxWidth = node.Int("MaxWidth", 10000);
            float layoutWidth = maxWidth > 0 && maxWidth < 2000 ? maxWidth : Mathf.Max(1f, rt.sizeDelta.x);
            if (layoutWidth < 60f) layoutWidth = 60f; // exact TextButton_OnDraw minimum.

            float drawX = 0f;
            int controlW = Mathf.Max(0, Mathf.RoundToInt(rt.sizeDelta.x));
            if (align.Equals("Center", StringComparison.OrdinalIgnoreCase) && maxWidth > 0 && maxWidth < 2000)
                drawX = ((controlW - 1) / 2) - (maxWidth / 2);
            else if (align.Equals("Right", StringComparison.OrdinalIgnoreCase))
                drawX = (controlW - 1) - Mathf.RoundToInt(layoutWidth);

            MeasureMultilineHeight(text, font, layoutWidth, align, out List<TextLine> lines);
            CreateBitmapLines(rt, "Text", text, font, lines, drawX, 0f, layoutWidth, align);
            RenderChildDialogs(node.Child("ChildDialogs"), rt, true);
        }

        private string ResolveSourceText(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string value = _loc?.Resolve(key);
            if (string.IsNullOrEmpty(value)) return key;
            return value;
        }

        private sealed class SourceButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public GameObject PassiveVisual;
            public GameObject OverVisual;
            public GameObject NormalTextRoot;
            public C2CampaignModalRenderer14 Renderer;
            public RectTransform TextParent;
            public string Text;
            public string PassiveFontName;
            public string OverFontName;
            public int FontDx;
            public int FontDy;
            public float Width;
            public float Height;
            public string Align;
            public bool Enabled;
            private GameObject _hoverText;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (!Enabled) return;
                if (PassiveVisual != null) PassiveVisual.SetActive(false);
                if (OverVisual != null) OverVisual.SetActive(true);
                if (NormalTextRoot != null) NormalTextRoot.SetActive(false);
                if (Renderer != null && TextParent != null && !string.IsNullOrEmpty(Text))
                {
                    FontSpec f = Renderer.ResolveFont(OverFontName);
                    float h = Renderer.MeasureMultilineHeight(Text, f, Width, Align, out List<TextLine> lines);
                    float y = Mathf.Floor(((Height - 1f) - h) / 2f) + FontDy;
                    _hoverText = Renderer.CreateBitmapLines(TextParent, "ButtonTextHover", Text, f, lines, FontDx, y, Width, Align);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (PassiveVisual != null) PassiveVisual.SetActive(true);
                if (OverVisual != null) OverVisual.SetActive(false);
                if (NormalTextRoot != null) NormalTextRoot.SetActive(true);
                if (_hoverText != null)
                {
                    UnityEngine.Object.Destroy(_hoverText);
                    _hoverText = null;
                }
            }
        }

        private sealed class FontSpec
        {
            public string Alias = string.Empty;
            public string GPFile = string.Empty;
            public Color32 Color = new Color32(255, 255, 255, 255);
            public int Top;
            public int Bottom;
            public int YShift;
            public int LineHeight => Math.Max(1, Bottom - Top);
        }

        private void BuildFontTable()
        {
            _fonts.Clear();
            _fontParams.Clear();
            ParseFontParamsFromSource();
            AddMenuFamily("MenuText", @"interf3\Fonts\FontG14");
            AddMenuFamily("MenuTitle", @"interf3\Fonts\FontG16");
            AddMenuFamily("MenuTitle2", @"interf3\Fonts\FontG18");

            // Standard aliases from InitFonts.h; included for source controls that may
            // be made visible by profile state in future without inventing a font.
            AddFont("BlackFont", @"interf3\Fonts\FontC14", C2Color("Black"));
            AddFont("RedFont", @"interf3\Fonts\FontC14", C2Color("Red"));
            AddFont("YellowFont", @"interf3\Fonts\FontC14", C2Color("Yellow"));
            AddFont("WhiteFont", @"interf3\Fonts\FontC14", C2Color("White"));
            AddFont("GrayFont", @"interf3\Fonts\FontC14", C2Color("Gray"));
            AddFont("OrangeFont", @"interf3\Fonts\FontC14", C2Color("Orange"));
            AddFont("SmallWhiteFont", @"interf3\Fonts\FontC12", C2Color("White"));
        }

        private void AddMenuFamily(string prefix, string gp)
        {
            AddFont(prefix + "Black", gp, C2Color("Black"));
            AddFont(prefix + "Red", gp, C2Color("Red"));
            AddFont(prefix + "Yellow", gp, C2Color("Yellow"));
            AddFont(prefix + "White", gp, C2Color("White"));
            AddFont(prefix + "Gray", gp, C2Color("Gray"));
            AddFont(prefix + "Orange", gp, C2Color("Orange"));
            AddFont(prefix + "Disable", gp, C2Color("Disable"));
        }

        private void AddFont(string alias, string gp, Color32 color)
        {
            FontSpec metric = FindMetricByGp(gp);
            _fonts[alias] = new FontSpec
            {
                Alias = alias, GPFile = gp, Color = color,
                Top = metric != null ? metric.Top : 0,
                Bottom = metric != null ? metric.Bottom : 14,
                YShift = metric != null ? metric.YShift : 0
            };
        }

        private void ParseFontParamsFromSource()
        {
            string raw = ReadGameMenuText(FontsXml);
            if (string.IsNullOrWhiteSpace(raw)) return;
            RawNode root = ParseSharedXml(raw);
            var nodes = new List<RawNode>();
            Collect(root, "OneFontParam", nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                RawNode n = nodes[i];
                string name = n.Value("Name", string.Empty);
                string gp = n.Value("gpFont", string.Empty);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(gp)) continue;
                _fontParams[name] = new FontSpec
                {
                    Alias = name,
                    GPFile = gp,
                    Color = ParseArgb(n.Value("DefColor", "FF2E2317"), C2Color("Black")),
                    Top = n.Int("Top", 0),
                    Bottom = n.Int("Bottom", 14),
                    YShift = n.Int("YShift", 0)
                };
            }
        }

        private FontSpec FindMetricByGp(string gp)
        {
            if (string.IsNullOrWhiteSpace(gp)) return null;
            foreach (FontSpec f in _fontParams.Values)
                if (f != null && string.Equals(NormPath(f.GPFile), NormPath(gp), StringComparison.OrdinalIgnoreCase)) return f;
            return null;
        }

        private FontSpec ResolveMarkupFont(string name, FontSpec fallback)
        {
            if (!string.IsNullOrWhiteSpace(name) && _fontParams.TryGetValue(name.Trim(), out FontSpec f)) return f;
            return fallback;
        }

        private static string NormPath(string s)
        {
            return (s ?? string.Empty).Trim().Replace('/', '\\');
        }

        private FontSpec ResolveFont(string alias)
        {
            if (!string.IsNullOrWhiteSpace(alias) && _fonts.TryGetValue(alias.Trim(), out FontSpec f))
                return f;

            Debug.LogError($"[C2:BFE14 CAMPAIGN XML V396A7R5_UIA1] source font alias '{alias}' has no original mapping; no synthetic font substituted");
            return new FontSpec { Alias = alias ?? string.Empty, GPFile = string.Empty, Color = new Color32(255,255,255,255), Top = 0, Bottom = 14 };
        }

        private static Color32 C2Color(string name)
        {
            // Dialogs/InitFonts.h exact ARGB palette.
            switch ((name ?? string.Empty).ToUpperInvariant())
            {
                case "BLACK": return new Color32(0x2E, 0x23, 0x17, 0xFF);
                case "RED": return new Color32(0x8A, 0x10, 0x00, 0xFF);
                case "YELLOW": return new Color32(0xD4, 0xC1, 0x9C, 0xFF);
                case "WHITE": return new Color32(0xFF, 0xF7, 0xEF, 0xFF);
                case "GRAY": return new Color32(0x6D, 0x68, 0x62, 0xFF);
                case "ORANGE": return new Color32(0x6A, 0x30, 0x00, 0xFF);
                case "DISABLE": return new Color32(0x66, 0x5F, 0x57, 0xC0);
                default: return Color.white;
            }
        }

        private sealed class TextIconSpec
        {
            public string Name = string.Empty;
            public string GPFile = string.Empty;
            public int Sprite = -1;
            public int Dx;
            public int Dy;
            public int Lx;
            public int Ly;
        }

        private void ParseTextIcons()
        {
            _textIcons.Clear();
            string raw = ReadGameMenuText(TextIconsXml);
            if (string.IsNullOrWhiteSpace(raw)) return;
            RawNode root = ParseSharedXml(raw);
            var nodes = new List<RawNode>();
            Collect(root, "OneTextIcon", nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                RawNode n = nodes[i];
                string name = n.Value("Name", string.Empty);
                if (string.IsNullOrWhiteSpace(name)) continue;
                _textIcons[name] = new TextIconSpec
                {
                    Name = name,
                    GPFile = n.Value("gpFile", string.Empty),
                    Sprite = n.Int("Sprite", -1),
                    Dx = n.Int("dx", 0),
                    Dy = n.Int("dy", 0),
                    Lx = n.Int("Lx", 0),
                    Ly = n.Int("Ly", 0)
                };
            }
        }

        private sealed class RichUnit
        {
            public int Kind; // 0=text, 1=space, 2=icon, 3=break, 4=align
            public string Text = string.Empty;
            public FontSpec Font;
            public Color32 Color;
            public TextIconSpec Icon;
            public string Align = "Left";
            public float Width;
            public float Height;
            public float DownShift;
        }

        private sealed class RichLine
        {
            public readonly List<RichUnit> Units = new List<RichUnit>();
            public string Align = "Left";
            public float Width;
            public float Height;
            public float DownShift;
        }

        private void RenderBigMapHelpText(RectTransform helpRoot, int page, int helpW, int helpH)
        {
            string key = "#BigMapHelp" + Mathf.Clamp(page, 0, 4).ToString(CultureInfo.InvariantCulture);
            string text = ResolveSourceText(key);
            FontSpec defaultFont = ResolveFont("MenuTextBlack");

            // Exact CBigMapHelp::CreateElements geometry from BigMapDataStr.h.
            const int dx = 75;
            const int dy = 78;
            // CBigMapHelp::CreateElements passes widths derived from x1-x / y1-y,
            // not inclusive GetWidth/GetHeight.  A source Width=880 therefore produces
            // tw=(879)-150+10=739 and th=(679)-156-4=519.
            int deskW = Math.Max(1, (helpW - 1) - 2 * dx + 10);
            int deskH = Math.Max(1, (helpH - 1) - 2 * dy - 4);
            BorderSpec empty = null;
            _borders.TryGetValue("EmptyBorder", out empty);
            int rightMargin = empty != null ? empty.RightMargin : 26;
            int viewW = Math.Max(1, deskW - rightMargin);
            // Original: ptbHelpText->MaxWidth = m_pTextDesk->x1 - m_pTextDesk->x - 30.
            int maxWidth = Math.Max(1, (deskW - 1) - 30);

            List<RichUnit> units = ParseLegacyRichText(text, defaultFont);
            List<RichLine> lines = LayoutRichText(units, maxWidth, defaultFont);
            float contentH = 2f;
            for (int i = 0; i < lines.Count; i++)
            {
                contentH += Mathf.Max(1f, lines[i].Height);
                if (i + 1 < lines.Count) contentH += Mathf.Max(0f, lines[i].DownShift);
            }
            contentH = Mathf.Max(contentH, deskH);

            RectTransform scrollHost = CreateAbsoluteRect(helpRoot, "BigMapHelp_TextDesk", dx, dy, deskW, deskH);
            ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 28f;

            RectTransform viewport = CreateAbsoluteRect(scrollHost, "Viewport", 0f, 0f, viewW, deskH);
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(1f, 1f, 1f, 0.001f);
            viewportHit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateAbsoluteRect(viewport, "Content", 0f, 0f, viewW, contentH);
            content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(viewW, contentH);

            float yy = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                RichLine line = lines[i];
                float x = 0f;
                if (line.Align.Equals("Center", StringComparison.OrdinalIgnoreCase)) x = Mathf.Floor((maxWidth - line.Width) * 0.5f);
                else if (line.Align.Equals("Right", StringComparison.OrdinalIgnoreCase)) x = maxWidth - line.Width;
                RenderRichLine(content, line, Mathf.Max(0f, x), yy);
                yy += Mathf.Max(1f, line.Height);
                if (i + 1 < lines.Count) yy += Mathf.Max(0f, line.DownShift);
            }

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalNormalizedPosition = 1f;

            if (empty != null && !string.IsNullOrWhiteSpace(empty.VScrollerGPFile))
                BuildOriginalVScroller(scrollHost, scroll, empty, deskW, deskH, contentH);
        }

        private List<RichUnit> ParseLegacyRichText(string text, FontSpec defaultFont)
        {
            var units = new List<RichUnit>();
            FontSpec font = defaultFont;
            Color32 color = defaultFont != null ? defaultFont.Color : C2Color("Black");
            var colorStack = new Stack<Color32>();
            string align = "Left";
            var word = new StringBuilder();

            Action flushWord = () =>
            {
                if (word.Length == 0) return;
                string s = word.ToString();
                float h;
                float w = MeasureString(s, font, out h);
                units.Add(new RichUnit
                {
                    Kind = 0, Text = s, Font = font, Color = color, Align = align,
                    Width = w, Height = Mathf.Max(FontLineHeight(font), h),
                    DownShift = font != null ? Mathf.Max(0, font.YShift) : 0f
                });
                word.Length = 0;
            };

            string src = text ?? string.Empty;
            for (int i = 0; i < src.Length; i++)
            {
                char ch = src[i];
                if (ch == '{')
                {
                    int e = src.IndexOf('}', i + 1);
                    if (e >= 0)
                    {
                        flushWord();
                        string cmd = src.Substring(i + 1, e - i - 1).Trim();
                        i = e;
                        if (cmd.Equals("AC", StringComparison.OrdinalIgnoreCase))
                        {
                            align = "Center";
                            units.Add(new RichUnit { Kind = 4, Align = align });
                        }
                        else if (cmd.Equals("AR", StringComparison.OrdinalIgnoreCase))
                        {
                            align = "Right";
                            units.Add(new RichUnit { Kind = 4, Align = align });
                        }
                        else if (cmd.Equals("AL", StringComparison.OrdinalIgnoreCase) || cmd.Equals("A", StringComparison.OrdinalIgnoreCase))
                        {
                            align = "Left";
                            units.Add(new RichUnit { Kind = 4, Align = align });
                        }
                        else if (cmd.Equals("F", StringComparison.OrdinalIgnoreCase))
                        {
                            font = defaultFont;
                            color = font != null ? font.Color : C2Color("Black");
                            colorStack.Clear();
                        }
                        else if (cmd.Length > 1 && (cmd[0] == 'F' || cmd[0] == 'f'))
                        {
                            font = ResolveMarkupFont(cmd.Substring(1), defaultFont);
                            color = font != null ? font.Color : color;
                            colorStack.Clear();
                        }
                        else if (cmd.Equals("C", StringComparison.OrdinalIgnoreCase))
                        {
                            color = colorStack.Count > 0 ? colorStack.Pop() : (font != null ? font.Color : C2Color("Black"));
                        }
                        else if (cmd.Length >= 2 && (cmd[0] == 'C' || cmd[0] == 'c'))
                        {
                            colorStack.Push(color);
                            color = LegacyCommandColor(cmd, color);
                        }
                        else if (cmd.Length > 1 && (cmd[0] == 'I' || cmd[0] == 'i'))
                        {
                            string iconName = cmd.Substring(1).Trim();
                            if (_textIcons.TryGetValue(iconName, out TextIconSpec icon))
                            {
                                units.Add(new RichUnit
                                {
                                    Kind = 2, Icon = icon, Align = align,
                                    Width = Mathf.Max(1f, icon.Lx - icon.Dx), Height = Mathf.Max(1f, icon.Ly - icon.Dy)
                                });
                            }
                            else
                            {
                                Debug.LogWarning($"[C2:BFE14 HELP V396A7R5_UIA1] text icon not found name='{iconName}' source='{TextIconsXml}'");
                            }
                        }
                        // {R ...}, {P ...}, {G ...} are not used by BigMapHelp0..4.
                        continue;
                    }
                }

                if (ch == '\\' || ch == '\n' || ch == '\r')
                {
                    flushWord();
                    if (ch == '\r' && i + 1 < src.Length && src[i + 1] == '\n') i++;
                    units.Add(new RichUnit
                    {
                        Kind = 3, Align = align, Height = FontLineHeight(font),
                        DownShift = font != null ? Mathf.Max(0, font.YShift) : 0f
                    });
                    continue;
                }

                if (ch == ' ' || ch == '\t' || ch == '\u00A0')
                {
                    flushWord();
                    float sw = MeasureSpace(font) * (ch == '\t' ? 4f : 1f);
                    units.Add(new RichUnit
                    {
                        Kind = 1, Text = " ", Font = font, Color = color, Align = align,
                        Width = sw, Height = FontLineHeight(font),
                        DownShift = font != null ? Mathf.Max(0, font.YShift) : 0f
                    });
                    continue;
                }

                word.Append(ch);
            }
            flushWord();
            return units;
        }

        private List<RichLine> LayoutRichText(List<RichUnit> units, float maxWidth, FontSpec defaultFont)
        {
            var lines = new List<RichLine>();
            string align = "Left";
            RichLine line = new RichLine { Align = align, Height = FontLineHeight(defaultFont) };

            Action finish = () =>
            {
                // trim trailing source spaces before measuring/rendering the line
                while (line.Units.Count > 0 && line.Units[line.Units.Count - 1].Kind == 1)
                {
                    line.Width -= line.Units[line.Units.Count - 1].Width;
                    line.Units.RemoveAt(line.Units.Count - 1);
                }
                line.Width = Mathf.Max(0f, line.Width);
                lines.Add(line);
                line = new RichLine { Align = align, Height = FontLineHeight(defaultFont) };
            };

            for (int i = 0; i < (units != null ? units.Count : 0); i++)
            {
                RichUnit u = units[i];
                if (u == null) continue;
                if (u.Kind == 4)
                {
                    align = string.IsNullOrEmpty(u.Align) ? "Left" : u.Align;
                    if (line.Units.Count == 0) line.Align = align;
                    continue;
                }
                if (u.Kind == 3)
                {
                    line.Height = Mathf.Max(line.Height, Mathf.Max(1f, u.Height));
                    line.DownShift = Mathf.Max(line.DownShift, Mathf.Max(0f, u.DownShift));
                    finish();
                    continue;
                }
                if (u.Kind == 1 && line.Units.Count == 0) continue;

                if (line.Units.Count > 0 && line.Width + u.Width > maxWidth)
                {
                    finish();
                    if (u.Kind == 1) continue;
                }

                line.Units.Add(u);
                line.Width += u.Width;
                line.Height = Mathf.Max(line.Height, Mathf.Max(1f, u.Height));
                line.DownShift = Mathf.Max(line.DownShift, Mathf.Max(0f, u.DownShift));
            }
            if (line.Units.Count > 0 || lines.Count == 0) finish();
            return lines;
        }

        private void RenderRichLine(RectTransform parent, RichLine line, float startX, float y)
        {
            float x = startX;
            if (line == null) return;
            for (int i = 0; i < line.Units.Count; i++)
            {
                RichUnit u = line.Units[i];
                if (u.Kind == 0)
                {
                    DrawRichGlyphs(parent, u.Text, u.Font, u.Color, x, y, line.Height);
                    x += u.Width;
                }
                else if (u.Kind == 1)
                {
                    x += u.Width;
                }
                else if (u.Kind == 2 && u.Icon != null)
                {
                    Sprite sp = LoadGp(u.Icon.GPFile, u.Icon.Sprite);
                    if (sp != null)
                    {
                        // Dialogs.cpp: GPS.ShowGP(xL+CurLx-dx, yL-Ly, ...), CurLx += Lx-dx.
                        float iy = y + line.Height - u.Icon.Ly;
                        CreateSpriteAt(parent, "TextIcon_" + Safe(u.Icon.Name), sp, x - u.Icon.Dx, iy);
                    }
                    x += u.Width;
                }
            }
        }

        private void DrawRichGlyphs(RectTransform parent, string text, FontSpec font, Color32 color, float x, float y, float lineHeight)
        {
            byte[] bytes = C2LegacyText14.EncodeCp1251(text ?? string.Empty);
            float xx = x;
            for (int i = 0; i < bytes.Length; i++)
            {
                int code = bytes[i];
                if (code == 0x20 || code == 0xA0) { xx += MeasureSpace(font); continue; }
                if (code == 0x09) { xx += MeasureSpace(font) * 4f; continue; }
                Sprite sp = LoadGlyph(font, code);
                if (sp == null) continue;
                var go = new GameObject("RG" + code.ToString("D3", CultureInfo.InvariantCulture), typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                RectTransform rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                float gy = Mathf.Floor(y + lineHeight - (font != null ? font.Bottom : Mathf.RoundToInt(lineHeight)));
                rt.anchoredPosition = new Vector2(xx, -gy);
                rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);
                Image img = go.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = color;
                img.raycastTarget = false;
                xx += sp.rect.width;
            }
        }

        private static Color32 LegacyCommandColor(string cmd, Color32 fallback)
        {
            if (string.IsNullOrEmpty(cmd)) return fallback;
            string c = cmd.Trim().ToUpperInvariant();
            if (c == "CR" || c == "CD") return new Color32(0xB8, 0x3B, 0x3F, 0xFF);
            if (c == "CG") return new Color32(0x60, 0xA0, 0x5A, 0xFF);
            if (c == "CB") return new Color32(0x2E, 0x23, 0x17, 0xFF);
            if (c == "CW") return new Color32(0xFF, 0xF7, 0xEF, 0xFF);
            if (c == "CY") return new Color32(0xD4, 0xC1, 0x9C, 0xFF);
            if (c == "CN") return new Color32(0x6A, 0x30, 0x00, 0xFF);
            if (c.Length > 2)
            {
                string h = c.Substring(1).Trim();
                if (h.Length == 8 && uint.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                {
                    byte a = (byte)((v >> 24) & 255);
                    byte r = (byte)((v >> 16) & 255);
                    byte g = (byte)((v >> 8) & 255);
                    byte b = (byte)(v & 255);
                    return new Color32(r, g, b, a);
                }
            }
            return fallback;
        }

        private void BuildOriginalVScroller(RectTransform host, ScrollRect scroll, BorderSpec border, int deskW, int deskH, float contentH)
        {
            string gp = border.VScrollerGPFile;
            Sprite up = LoadGp(gp, 0);
            Sprite down = LoadGp(gp, 2);
            Sprite thumb = LoadGp(gp, 4);
            if (up == null || down == null || thumb == null) return;

            // Source pipeline (Dialogs.cpp / DrawForms.cpp):
            //   VS.x      = DD->x1 - VScroller_DX_right
            //   VS.y      = DD->y  + VScroller_DY_top
            //   VS.Ly     = deskH - DY_top - DY_bottom
            //   btnly     = GPHeight(frame 0)
            //   LineLy    = Ly - 2*btnly
            //   ScrLy     = GPHeight(frame 4)  <-- FIXED native thumb height
            // DrawVScroller separately tiles frames 5/6/7 through an IntersectWindows clip.
            // It never resizes frame 4 according to viewport/content ratio.
            float x = (deskW - 1f) - border.VScrollerDxRight;
            float top = border.VScrollerDyTop;
            float fullLy = Mathf.Max(1f, deskH - border.VScrollerDyTop - border.VScrolledDyBottom);
            float upH = Mathf.Max(1f, up.rect.height);
            float downH = Mathf.Max(1f, down.rect.height);
            float thumbH = Mathf.Max(1f, thumb.rect.height);
            float barW = Mathf.Max(up.rect.width, Mathf.Max(down.rect.width, thumb.rect.width));
            float btnLy = upH;
            float lineLy = Mathf.Max(1f, fullLy - 2f * btnLy);

            // Original DrawVScroller:
            // ULY=GPHeight(Up)/2; IntersectWindows(... y+ULY ... y+Ly-1-ULY);
            // center frames 5/6/7 are tiled from y+ULY and CLIPPED at the lower edge.
            float halfUp = Mathf.Floor(upH * 0.5f);
            float centerClipY = top + halfUp;
            float centerClipBottomInclusive = top + fullLy - 1f - halfUp;
            float centerClipH = Mathf.Max(1f, centerClipBottomInclusive - centerClipY + 1f);
            RectTransform centerClip = CreateAbsoluteRect(host, "VScroll_CenterClip", x, centerClipY, barW, centerClipH);
            centerClip.gameObject.AddComponent<RectMask2D>();

            float cy = 0f;
            int tile = 0;
            while (cy < centerClipH && tile < 512)
            {
                int frame = 5 + (tile % 3);
                Sprite sp = LoadGp(gp, frame);
                if (sp == null || sp.rect.height <= 0f) break;
                CreateSpriteAt(centerClip, "Center_" + tile, sp, 0f, cy);
                cy += sp.rect.height;
                tile++;
            }

            RectTransform upRt = CreateAbsoluteRect(host, "VScroll_Up", x, top, barW, upH);
            AddSourceSprite(upRt, up);
            Button upButton = AddTransparentButton(upRt);

            RectTransform downRt = CreateAbsoluteRect(host, "VScroll_Down", x, top + fullLy - downH, barW, downH);
            AddSourceSprite(downRt, down);
            Button downButton = AddTransparentButton(downRt);

            // Keep ScrollRect for clipping/content motion, but DO NOT bind its verticalScrollbar.
            // ScrollRect would overwrite Scrollbar.size every layout pass with viewport/content ratio,
            // stretching the 45px original GP thumb into the giant handle seen in the screenshot.
            // Instead this Scrollbar is an independent input/sync adapter whose handle size is
            // permanently GPHeight(frame 4), exactly like VScrollBar::ScrLy in the original.
            RectTransform interaction = CreateAbsoluteRect(host, "VScroll_Interaction", x, top + btnLy, barW, lineLy);
            Image trackHit = interaction.gameObject.AddComponent<Image>();
            trackHit.color = new Color(1f, 1f, 1f, 0.001f);
            trackHit.raycastTarget = true;

            Scrollbar sb = interaction.gameObject.AddComponent<Scrollbar>();
            sb.transition = Selectable.Transition.None;
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.targetGraphic = trackHit;

            RectTransform sliding = CreateAbsoluteRect(interaction, "SlidingArea", 0f, 0f, barW, lineLy);
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(sliding, false);
            RectTransform handle = (RectTransform)handleGo.transform;
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            Image handleImage = handleGo.GetComponent<Image>();
            handleImage.sprite = thumb;
            handleImage.type = Image.Type.Simple;
            handleImage.preserveAspect = false;
            handleImage.raycastTarget = true;
            sb.handleRect = handle;

            float fixedHandleSize = Mathf.Clamp01(thumbH / lineLy);
            sb.size = fixedHandleSize;
            sb.value = 1f;

            // Original OnesDy=32.  Map that source pixel step into ScrollRect's normalized range.
            float scrollRange = Mathf.Max(0f, contentH - deskH);
            float oneStep = scrollRange > 0.001f ? Mathf.Clamp01(32f / scrollRange) : 0f;
            upButton.onClick.AddListener(() =>
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + oneStep));
            downButton.onClick.AddListener(() =>
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - oneStep));

            // Two-way value synchronization without giving ScrollRect ownership of sb.size.
            sb.onValueChanged.AddListener(v =>
            {
                if (scroll != null)
                    scroll.verticalNormalizedPosition = Mathf.Clamp01(v);
            });
            scroll.onValueChanged.AddListener(v =>
            {
                if (sb != null)
                    sb.SetValueWithoutNotify(Mathf.Clamp01(v.y));
            });
            sb.SetValueWithoutNotify(Mathf.Clamp01(scroll.verticalNormalizedPosition));

            Debug.Log($"[C2:BFE14 HELP SCROLL V396A7R5_UIA2] gp='{gp}' desk={deskW}x{deskH} " +
                      $"x={x:0.##} fullLy={fullLy:0.##} btnLy={btnLy:0.##} lineLy={lineLy:0.##} " +
                      $"thumbNative={thumb.rect.width:0.##}x{thumbH:0.##} fixedSize={fixedHandleSize:0.####} " +
                      $"contentH={contentH:0.##} binding=independent_fixed_GP_thumb centerClip=IntersectWindows_equivalent");
        }

        private static void AddSourceSprite(RectTransform parent, Sprite sprite)
        {
            if (parent == null || sprite == null) return;
            Image img = parent.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }

        private static Button AddTransparentButton(RectTransform parent)
        {
            Image hit = parent.gameObject.GetComponent<Image>();
            if (hit == null)
            {
                hit = parent.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0.001f);
            }
            hit.raycastTarget = true;
            Button b = parent.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            return b;
        }

        // V396A7R5_UIA3 seam experiment.
        // The original UI is rasterized on an integer framebuffer. OptionsRenderer R7
        // already preserves that contract with ConstantPixelSize + an integer scale.
        // Campaign/Help used ScaleWithScreenSize and a center-anchored 1024x768 stage;
        // at e.g. 1031px width the stage begins at x=3.5, putting every GP edge and
        // RectMask2D boundary on a half pixel. Retain centering, but quantize the stage
        // origin to an integer logical pixel BEFORE child projection.
        private static int ConfigureIntegerPixelCanvasUIA3(Canvas canvas, CanvasScaler scaler)
        {
            if (canvas != null) canvas.pixelPerfect = true;
            if (scaler == null) return 1;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            int scaleX = Mathf.Max(1, Screen.width / 1024);
            int scaleY = Mathf.Max(1, Screen.height / 768);
            int integerScale = Mathf.Max(1, Mathf.Min(scaleX, scaleY));
            scaler.scaleFactor = integerScale;
            return integerScale;
        }

        private static RectTransform CreatePixelStage(RectTransform parent, string name, int width, int height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            // UIA3.1: immediately after creating a ScreenSpaceOverlay Canvas the
            // Canvas RectTransform can still report rect=0x0 until Unity performs
            // its first canvas/layout update. UIA3 used parent.rect here, so the
            // first frame computed (-width/2,+height/2) and moved the whole 1024x768
            // source stage into the upper-left corner. Derive the logical viewport
            // directly from Screen / Canvas.scaleFactor instead. For a
            // ConstantPixelSize canvas this is the actual coordinate space used by
            // child RectTransforms and is available synchronously.
            Canvas ownerCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            float sf = ownerCanvas != null ? Mathf.Max(0.0001f, ownerCanvas.scaleFactor) : 1f;
            float parentW = Screen.width / sf;
            float parentH = Screen.height / sf;
            float stageX = Mathf.Floor((parentW - width) * 0.5f);
            float stageY = Mathf.Floor((parentH - height) * 0.5f);
            rt.anchoredPosition = new Vector2(stageX, -stageY);
            rt.sizeDelta = new Vector2(Mathf.Max(1, width), Mathf.Max(1, height));
            return rt;
        }

        private static void LogSeamExperimentUIA3(string screenName, RectTransform stage, Canvas canvas, int integerScale)
        {
            if (stage == null) return;
            Vector2 p = stage.anchoredPosition;
            Vector2 sz = stage.sizeDelta;
            bool integerOrigin = Mathf.Abs(p.x - Mathf.Round(p.x)) < 0.001f &&
                                 Mathf.Abs(p.y - Mathf.Round(p.y)) < 0.001f;
            Debug.Log(
                $"[C2:BFE14 SEAM EXP V396A7R5_UIA3_1] screen='{screenName}' " +
                $"mode=ConstantPixelSize integerScale={integerScale} canvasScale={(canvas != null ? canvas.scaleFactor : 0f):0.###} " +
                $"stagePos=({p.x:0.###},{p.y:0.###}) stageSize=({sz.x:0.###},{sz.y:0.###}) " +
                $"integerOrigin={(integerOrigin ? 1 : 0)} headerGuard=1 sourcePhase=i_mod_3 " +
                "DrawRect4=source_exact pointClamp=1 useSpriteMesh=0");
        }

        private static RectTransform CreateAbsoluteRect(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, w), Mathf.Max(1f, h));
            return rt;
        }

        private sealed class TextLine
        {
            public string Text = string.Empty;
            public float Width;
            public float Height;
        }

        private float MeasureMultilineHeight(string text, FontSpec font, float maxWidth, string align, out List<TextLine> lines)
        {
            lines = WrapText(text ?? string.Empty, font, Mathf.Max(1f, maxWidth));
            float h = 0f;
            float downShift = font != null ? Mathf.Max(0, font.YShift) : 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                h += Mathf.Max(1f, lines[i].Height);
                if (i + 1 < lines.Count) h += downShift;
            }
            return h;
        }

        private List<TextLine> WrapText(string text, FontSpec font, float maxWidth)
        {
            var result = new List<TextLine>();
            string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] paragraphs = normalized.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                string paragraph = paragraphs[p];
                if (paragraph.Length == 0)
                {
                    result.Add(new TextLine { Text = string.Empty, Width = 0f, Height = FontLineHeight(font) });
                    continue;
                }

                string[] words = paragraph.Split(new[] { ' ' }, StringSplitOptions.None);
                string current = string.Empty;
                float currentW = 0f;
                float lineH = FontLineHeight(font);
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];
                    float wordW = MeasureString(word, font, out float wordH);
                    float spaceW = current.Length == 0 ? 0f : MeasureSpace(font);
                    if (current.Length > 0 && currentW + spaceW + wordW > maxWidth)
                    {
                        result.Add(new TextLine { Text = current, Width = currentW, Height = Mathf.Max(lineH, wordH) });
                        current = word;
                        currentW = wordW;
                        lineH = Mathf.Max(FontLineHeight(font), wordH);
                    }
                    else
                    {
                        if (current.Length > 0)
                        {
                            current += " ";
                            currentW += spaceW;
                        }
                        current += word;
                        currentW += wordW;
                        lineH = Mathf.Max(lineH, wordH);
                    }
                }
                result.Add(new TextLine { Text = current, Width = currentW, Height = lineH });
            }
            if (result.Count == 0) result.Add(new TextLine { Text = string.Empty, Height = FontLineHeight(font) });
            return result;
        }

        private GameObject CreateBitmapLines(RectTransform parent, string name, string fullText, FontSpec font,
            List<TextLine> lines, float x, float y, float width, string align)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, SumLineHeight(lines, font)));

            float yy = 0f;
            float downShift = font != null ? Mathf.Max(0, font.YShift) : 0f;
            for (int li = 0; li < lines.Count; li++)
            {
                TextLine line = lines[li];
                float startX = 0f;
                if (string.Equals(align, "Center", StringComparison.OrdinalIgnoreCase))
                    startX = Mathf.Floor((width - line.Width) * 0.5f);
                else if (string.Equals(align, "Right", StringComparison.OrdinalIgnoreCase))
                    startX = width - line.Width;

                DrawGlyphLine(rt, line.Text, font, startX, yy, line.Height);
                yy += Mathf.Max(1f, line.Height);
                if (li + 1 < lines.Count) yy += downShift;
            }
            return root;
        }

        private void DrawGlyphLine(RectTransform parent, string text, FontSpec font, float x, float y, float lineHeight)
        {
            byte[] bytes = C2LegacyText14.EncodeCp1251(text ?? string.Empty);
            float xx = x;
            for (int i = 0; i < bytes.Length; i++)
            {
                int code = bytes[i];
                if (code == 0x20 || code == 0xA0)
                {
                    xx += MeasureSpace(font);
                    continue;
                }
                if (code == 0x09)
                {
                    xx += MeasureSpace(font) * 4f;
                    continue;
                }

                Sprite sp = LoadGlyph(font, code);
                if (sp == null)
                {
                    xx += MeasureSpace(font);
                    continue;
                }

                var go = new GameObject("G" + code.ToString("D3", CultureInfo.InvariantCulture), typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                RectTransform rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                // Dialogs.cpp DrawMultilineText: baseline yL = lineTop + Height,
                // ShowChar(..., yL - FontParam.Bottom). Keep Top/Bottom source metrics.
                float gy = Mathf.Floor(y + lineHeight - (font != null ? font.Bottom : Mathf.RoundToInt(lineHeight)));
                rt.anchoredPosition = new Vector2(xx, -gy);
                rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);
                Image img = go.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = font.Color;
                img.raycastTarget = false;
                xx += sp.rect.width;
            }
        }

        private float MeasureString(string text, FontSpec font, out float height)
        {
            byte[] bytes = C2LegacyText14.EncodeCp1251(text ?? string.Empty);
            float width = 0f;
            // Dialogs.cpp DrawMultilineText uses OneFontParam.Bottom-Top for ordinary
            // text line height; glyph bitmap height is not the line metric.
            height = FontLineHeight(font);
            for (int i = 0; i < bytes.Length; i++)
            {
                int code = bytes[i];
                if (code == 0x20 || code == 0xA0)
                {
                    width += MeasureSpace(font);
                    continue;
                }
                if (code == 0x09)
                {
                    width += MeasureSpace(font) * 4f;
                    continue;
                }
                Sprite sp = LoadGlyph(font, code);
                if (sp == null) continue;
                width += sp.rect.width;
            }
            return width;
        }

        private float MeasureSpace(FontSpec font)
        {
            // Fastdraw.cpp/GetRLCWidth: GP fonts use the width of lowercase 'c'
            // for space and four times that for TAB. Frame 32 is deliberately not read.
            Sprite c = LoadGlyph(font, (byte)'c');
            if (c != null && c.rect.width > 0f) return c.rect.width;
            return 4f;
        }

        private float FontLineHeight(FontSpec font)
        {
            if (font != null && font.Bottom > font.Top) return font.Bottom - font.Top;
            Sprite w = LoadGlyph(font, (byte)'W');
            if (w != null && w.rect.height > 0f) return w.rect.height;
            return 14f;
        }

        private static float SumLineHeight(List<TextLine> lines, FontSpec font)
        {
            float h = 0f;
            if (lines == null) return h;
            float downShift = font != null ? Mathf.Max(0, font.YShift) : 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                h += Mathf.Max(1f, lines[i].Height);
                if (i + 1 < lines.Count) h += downShift;
            }
            return h;
        }

        private Sprite LoadGlyph(FontSpec font, int code)
        {
            if (font == null || string.IsNullOrWhiteSpace(font.GPFile) || code < 0) return null;
            string key = font.GPFile + "|" + code.ToString(CultureInfo.InvariantCulture);
            if (_glyphCache.TryGetValue(key, out Sprite cached)) return cached;

            // V396A5: this is the original Cossacks II font path. RLCFont with a GP index
            // draws frame byte(cp1251) directly (Fastdraw.cpp ShowCharEx/GetRLCWidth).
            // Decode that exact GN16 bank from Data\\Cash in memory instead of asking the
            // Melinoja session cache for a frame that may not have had its compressed segment
            // expanded yet. FontG14/16/18 in the real 1.4 cache each contain 256 frames.
            if (TryLoadGlyphFromOriginalFontBank(font.GPFile, code, out Sprite direct))
            {
                _glyphCache[key] = direct;
                return direct;
            }

            // Keep the old source-only path as a diagnostic fallback. It is still the same
            // FileID/frame, never replacement art.
            Sprite sp = Menu14ActionStateRuntime.TryLoadGpSpriteForRenderer(font.GPFile, code, true);
            _glyphCache[key] = sp;
            if (sp == null)
            {
                _missingAssets++;
                _missingAssetKeys.Add($"GLYPH:{font.GPFile}#{code}");
                Debug.LogWarning($"[C2:BFE14 CAMPAIGN XML V396A7R5_UIA1] missing glyph source='{font.GPFile}' frame={code}");
            }
            return sp;
        }

        private bool TryLoadGlyphFromOriginalFontBank(string gpFile, int code, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(gpFile) || code < 0 || code > 255)
                return false;

            string path = ResolveOriginalG16Path(gpFile);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            if (!_fontBanks.TryGetValue(path, out C2DirectSpriteBank bank) || bank == null)
            {
                bank = new C2DirectSpriteBank();
                if (!bank.Load(path, out string loadError))
                {
                    Debug.LogWarning($"[C2:BFE14 FONT V396A7R5_UIA1] load failed source='{gpFile}' path='{path}' err='{loadError}'");
                    return false;
                }
                _fontBanks[path] = bank;
                if (_fontBankAudit.Add(path))
                {
                    Debug.Log($"[C2:BFE14 FONT V396A7R5_UIA1] source='{gpFile}' path='{path}' frames={bank.FrameCount} decoder=direct_GN16_CP1251");
                }
            }

            if (code >= bank.FrameCount)
                return false;

            if (!bank.RenderFrame(code, out C2RenderedFrame frame, out string renderError) ||
                frame == null || frame.Width <= 0 || frame.Height <= 0 || frame.Rgba == null)
            {
                Debug.LogWarning($"[C2:BFE14 FONT V396A7R5_UIA1] frame failed source='{gpFile}' frame={code} err='{renderError}'");
                return false;
            }

            byte[] rgba = FlipTopLeftRgbaForUnity(frame.Rgba, frame.Width, frame.Height);
            var tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, false);

            sprite = Sprite.Create(tex, new Rect(0, 0, frame.Width, frame.Height), new Vector2(0f, 1f), 1f);
            sprite.name = $"{Path.GetFileNameWithoutExtension(path)}_glyph_{code:000}";
            return true;
        }

        private string ResolveOriginalG16Path(string gpFile)
        {
            string normalized = (gpFile ?? string.Empty).Trim().Replace('\\', '_').Replace('/', '_');
            if (normalized.Length == 0) return string.Empty;
            string fileName = normalized + ".g16";

            var roots = new List<string>();
            AddUniqueRoot(roots, Menu14ActionStateRuntime.CurrentLogicalDataRoot);
            AddUniqueRoot(roots, _fs?.DataRoot);
            AddUniqueRoot(roots, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));

            for (int i = 0; i < roots.Count; i++)
            {
                string cash = Path.Combine(roots[i], "Cash");
                string candidate = Path.Combine(cash, fileName);
                if (File.Exists(candidate)) return candidate;

                // Only needed on case-sensitive hosts. Windows finds the exact final-data file directly.
                if (Directory.Exists(cash))
                {
                    try
                    {
                        foreach (string f in Directory.EnumerateFiles(cash, "*.g16", SearchOption.TopDirectoryOnly))
                        {
                            if (string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase))
                                return f;
                        }
                    }
                    catch { }
                }
            }

            // Editor/project fallback: still the exact cached game bank bundled in Resources.
            string resourceFile = Path.Combine(Application.dataPath, "Resources", "Interf3", fileName);
            if (File.Exists(resourceFile)) return resourceFile;
            return string.Empty;
        }

        private static void AddUniqueRoot(List<string> roots, string root)
        {
            if (roots == null || string.IsNullOrWhiteSpace(root)) return;
            for (int i = 0; i < roots.Count; i++)
                if (string.Equals(roots[i], root, StringComparison.OrdinalIgnoreCase)) return;
            roots.Add(root);
        }

        private static byte[] FlipTopLeftRgbaForUnity(byte[] src, int width, int height)
        {
            if (src == null || width <= 0 || height <= 1) return src;
            int rowBytes = width * 4;
            var dst = new byte[src.Length];
            for (int y = 0; y < height; y++)
                Buffer.BlockCopy(src, y * rowBytes, dst, (height - 1 - y) * rowBytes, rowBytes);
            return dst;
        }

        private Sprite LoadGp(string fileId, int spriteId)
        {
            if (string.IsNullOrWhiteSpace(fileId) || spriteId < 0) return null;
            Sprite sp = Menu14ActionStateRuntime.TryLoadGpSpriteForRenderer(fileId, spriteId, true);
            if (sp != null && sp.texture != null)
            {
                sp.texture.filterMode = FilterMode.Point;
                sp.texture.wrapMode = TextureWrapMode.Clamp;
            }
            if (sp == null)
            {
                _missingAssets++;
                _missingAssetKeys.Add($"GP:{fileId}#{spriteId}");
                Debug.LogError(
                    $"[C2:BFE14 CAMPAIGN XML V396A7R5_UIA1] missing source GP frame file='{fileId}' sprite={spriteId} " +
                    $"logicalRoot='{Menu14ActionStateRuntime.CurrentLogicalDataRoot}' fsRoot='{(_fs != null ? _fs.DataRoot : string.Empty)}' " +
                    $"sourceBundle='{Menu14ActionStateRuntime.CurrentSourceBundleId}'");
            }
            return sp;
        }

        private void LogGpSourceAuditUIA3_2(string screen)
        {
            string fsRoot = _fs != null ? (_fs.DataRoot ?? string.Empty) : string.Empty;
            string logicalRoot = Menu14ActionStateRuntime.CurrentLogicalDataRoot ?? string.Empty;
            string sourceBundle = Menu14ActionStateRuntime.CurrentSourceBundleId ?? string.Empty;
            string sourceRoot = Menu14ActionStateRuntime.CurrentSourceDataRoot ?? string.Empty;
            string missing = _missingAssetKeys.Count == 0
                ? "<none>"
                : string.Join(";", _missingAssetKeys);
            string status = _missingAssets == 0 ? "PASS" : "FAIL";
            string line =
                $"[C2:BFE14 GP SOURCE V396A7R5_UIA3_2] screen='{screen}' status={status} " +
                $"fsRoot='{fsRoot}' logicalRoot='{logicalRoot}' sourceBundle='{sourceBundle}' sourceRoot='{sourceRoot}' " +
                $"missingAssets={_missingAssets} uniqueMissing={_missingAssetKeys.Count} missing='{missing}'";

            if (_missingAssets == 0) Debug.Log(line);
            else Debug.LogError(line);
        }

        private void DrawHeaderEx2(Transform parent, float width, string gp,
            int frameL, int frameR, int frameC1, int frameC2, int frameC3)
        {
            if (parent == null || string.IsNullOrWhiteSpace(gp)) return;
            if (width < 24f) width = 24f;

            Sprite left = frameL >= 0 ? LoadGp(gp, frameL) : null;
            Sprite right = frameR >= 0 ? LoadGp(gp, frameR) : null;
            float leftW = left != null && left.rect.width <= 2048f ? left.rect.width : 0f;
            float rightW = right != null && right.rect.width <= 2048f ? right.rect.width : 0f;

            // Original DrawHeaderEx2: inclusive IntersectWindows
            // [x0+frWidthL, x0+Lx-frWidthR]. OptionsRenderer R7 adds a one-pixel
            // RectMask2D guard UNDER the edge sprites. This compensates Unity's
            // half-open mask/raster boundary without changing source geometry.
            const float clipGuard = 1f;
            float logicalClipStart = leftW;
            float logicalEndInclusive = width - rightW;
            float clipStart = Mathf.Max(0f, logicalClipStart - clipGuard);
            float clipEndExclusive = Mathf.Min(width, logicalEndInclusive + 1f + clipGuard);
            float clipW = Mathf.Max(1f, clipEndExclusive - clipStart);
            Transform clip = CreateClipRect(parent, "HeaderCenterClip", clipStart, -32f, clipW, 161f);

            int n = 0;
            float sourceX = 0f;
            while (sourceX < width && n < 300)
            {
                // Exact original phase: switch(i % 3), i = source pixel X.
                int pattern = ((int)sourceX) % 3;
                if (pattern < 0) pattern += 3;
                int frame = pattern == 0 ? frameC1 : pattern == 1 ? frameC2 : frameC3;
                Sprite sp = LoadGp(gp, frame);
                if (sp == null || sp.rect.width <= 0f) break;
                CreateSpriteAt(clip, "Center_" + n, sp, sourceX - clipStart, 32f);
                sourceX += sp.rect.width;
                n++;
            }

            // Edge sprites render last and cover the guard band.
            if (left != null) CreateSpriteAt(parent, "HeaderLeft", left, 0f, 0f);
            if (right != null) CreateSpriteAt(parent, "HeaderRight", right, width - rightW, 0f);
        }

        private sealed class BorderSpec
        {
            public string Name = string.Empty;
            public string GPFile = string.Empty;
            public int LeftTop = -1, RightTop = -1, LeftBottom = -1, RightBottom = -1;
            public int TopLine = -1, BottomLine = -1, LeftLine = -1, RightLine = -1;
            public int LeftMargin, RightMargin, TopMargin, BottomMargin;
            public int NFillers, StartFiller = -1;
            public string VScrollerGPFile = string.Empty;
            public int VScrollerDxRight;
            public int VScrollerDyTop;
            public int VScrolledDyBottom;
        }

        private void ParseBorders(RawNode root)
        {
            _borders.Clear();
            var nodes = new List<RawNode>();
            Collect(root, "StdBorder", nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                RawNode n = nodes[i];
                string name = n.Value("Name", string.Empty);
                if (string.IsNullOrEmpty(name)) continue;
                _borders[name] = new BorderSpec
                {
                    Name = name,
                    GPFile = n.Value("GP_File", string.Empty),
                    LeftTop = n.Int("LeftTop", -1),
                    RightTop = n.Int("RightTop", -1),
                    LeftBottom = n.Int("LeftBottom", -1),
                    RightBottom = n.Int("RightBottom", -1),
                    TopLine = n.Int("TopLine", -1),
                    BottomLine = n.Int("BottomLine", -1),
                    LeftLine = n.Int("LeftLine", -1),
                    RightLine = n.Int("RightLine", -1),
                    LeftMargin = n.Int("LeftMargin", 0),
                    RightMargin = n.Int("RightMargin", 0),
                    TopMargin = n.Int("TopMargin", 0),
                    BottomMargin = n.Int("BottomMargin", 0),
                    NFillers = n.Int("NFillers", 0),
                    StartFiller = n.Int("StartFiller", -1),
                    VScrollerGPFile = n.Value("VScroller_GP_File", string.Empty),
                    VScrollerDxRight = n.Int("VScroller_DX_right", 0),
                    VScrollerDyTop = n.Int("VScroller_DY_top", 0),
                    VScrolledDyBottom = n.Int("VScrolled_DY_bottom", 0)
                };
            }
        }

        private void DrawFilledBorder(Transform parent, float width, float height, BorderSpec spec)
        {
            if (spec.NFillers > 0 && spec.StartFiller >= 0)
            {
                Sprite first = LoadGp(spec.GPFile, spec.StartFiller);
                if (first != null)
                {
                    float tileW = Mathf.Max(1f, first.rect.width);
                    float tileH = Mathf.Max(1f, first.rect.height);
                    Transform clip = CreateClipRect(parent, "Fill_FullRect", 0f, 0f, width, height);
                    int nx = Mathf.FloorToInt((width - 1f) / tileW);
                    int ny = Mathf.FloorToInt((height - 1f) / tileH);
                    for (int ix = 0; ix <= nx; ix++)
                    {
                        for (int iy = 0; iy <= ny; iy++)
                        {
                            int pattern = (ix * ix + iy * iy * iy) % Math.Max(1, spec.NFillers);
                            int frame = spec.StartFiller + pattern;
                            Sprite sp = LoadGp(spec.GPFile, frame);
                            if (sp != null) CreateSpriteAt(clip, $"Fill_{ix}_{iy}_F{frame}", sp, ix * tileW, iy * tileH);
                        }
                    }
                }
            }
            DrawRect4(parent, width, height, spec);
        }

        private void DrawRect4(Transform parent, float width, float height, BorderSpec spec)
        {
            Sprite clu = LoadGp(spec.GPFile, spec.LeftTop);
            Sprite cru = LoadGp(spec.GPFile, spec.RightTop);
            Sprite cld = LoadGp(spec.GPFile, spec.LeftBottom);
            Sprite crd = LoadGp(spec.GPFile, spec.RightBottom);
            Sprite lu = LoadGp(spec.GPFile, spec.TopLine);
            Sprite ld = LoadGp(spec.GPFile, spec.BottomLine);
            Sprite ll = LoadGp(spec.GPFile, spec.LeftLine);
            Sprite lr = LoadGp(spec.GPFile, spec.RightLine);

            float ullx = clu != null ? clu.rect.width : 32f; if (ullx <= 0f) ullx = 32f;
            float lx2 = Mathf.Floor(ullx / 2f);
            float dllx = cld != null ? cld.rect.width : (clu != null ? clu.rect.width : 32f); if (dllx <= 0f) dllx = 32f;
            float lx3 = Mathf.Floor(dllx / 2f);
            float uplx = lu != null ? lu.rect.width : 32f; if (uplx <= 0f) uplx = 32f;
            float dnlx = ld != null ? ld.rect.width : 32f; if (dnlx <= 0f) dnlx = 32f;
            float lsly = clu != null ? clu.rect.height : 0f;
            float lly2 = Mathf.Floor(lsly / 2f);
            float ldly = cld != null ? cld.rect.width : 0f; if (ldly > 1000f) ldly = 0f;
            float ldy2 = Mathf.Floor(ldly / 2f);
            float leftly = ll != null ? ll.rect.height : 32f; if (leftly <= 0f) leftly = 32f;
            float rightly = lr != null ? lr.rect.height : 32f; if (rightly <= 0f) rightly = 32f;
            float x1 = width - 1f;
            float y1 = height - 1f;
            float midY = Mathf.Floor(y1 / 2f);

            if (lu != null)
            {
                float cx0 = lx2, cy0 = -lly2, cx1 = x1 - lx2 - 1f, cy1 = midY;
                Transform clip = CreateClipRectInclusive(parent, "Border_Top_Clip", cx0, cy0, cx1, cy1);
                int n = Mathf.FloorToInt((width - ullx) / uplx);
                for (int i = 0; i <= n + 2; i++) CreateSpriteAt(clip, "Top_" + i, lu, i * uplx + lx2 - cx0, -lly2 - cy0);
            }
            if (ld != null)
            {
                float cx0 = lx3, cy0 = midY + 1f, cx1 = x1 - lx3 - 1f, cy1 = y1 + lly2;
                Transform clip = CreateClipRectInclusive(parent, "Border_Bottom_Clip", cx0, cy0, cx1, cy1);
                int n = Mathf.FloorToInt((width - dllx) / dnlx);
                for (int i = 0; i <= n + 2; i++) CreateSpriteAt(clip, "Bottom_" + i, ld, i * dnlx + lx3 - cx0, y1 - ldy2 - cy0);
            }
            {
                float cx0 = -lx3, cy0 = lly2, cx1 = x1 + lx3, cy1 = y1 - ldy2;
                Transform clip = CreateClipRectInclusive(parent, "Border_Vertical_Clip", cx0, cy0, cx1, cy1);
                if (ll != null)
                {
                    int n = Mathf.FloorToInt((height - lly2 - ldy2) / leftly);
                    for (int i = 0; i <= n + 1; i++) CreateSpriteAt(clip, "Left_" + i, ll, -lx3 - cx0, i * leftly + lly2 - cy0);
                }
                if (lr != null)
                {
                    int n = Mathf.FloorToInt((height - lly2 - ldy2) / rightly);
                    for (int i = 0; i <= n + 1; i++) CreateSpriteAt(clip, "Right_" + i, lr, x1 - lx3 - cx0, i * rightly + lly2 - cy0);
                }
            }

            float cornerMid = Mathf.Floor((y1 - ldy2 + lly2) / 2f);
            float xMid = Mathf.Floor(x1 / 2f);
            if (clu != null) DrawCorner(parent, "Corner_LT", clu, -lx2, -lly2, -lx2, -lly2, xMid - 1f, cornerMid - 1f);
            if (cru != null) DrawCorner(parent, "Corner_RT", cru, x1 - lx2, -lly2, xMid, -lly2, x1 + lx2, cornerMid - 1f);
            if (cld != null) DrawCorner(parent, "Corner_LB", cld, -lx3, y1 - ldy2, -lx3, cornerMid, xMid - 1f, y1 + lly2);
            if (crd != null) DrawCorner(parent, "Corner_RB", crd, x1 - lx3, y1 - ldy2, xMid, cornerMid, x1 + lx3, y1 + lly2);
        }

        private static void DrawCorner(Transform parent, string name, Sprite sp, float sx, float sy,
            float cx0, float cy0, float cx1, float cy1)
        {
            Transform clip = CreateClipRectInclusive(parent, name + "_Clip", cx0, cy0, cx1, cy1);
            CreateSpriteAt(clip, name, sp, sx - cx0, sy - cy0);
        }

        private static Transform CreateClipRectInclusive(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            return CreateClipRect(parent, name, x0, y0, Mathf.Max(1f, x1 - x0 + 1f), Mathf.Max(1f, y1 - y0 + 1f));
        }

        private static Transform CreateClipRect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, w), Mathf.Max(1f, h));
            return go.transform;
        }

        private static void CreateSpriteAt(Transform parent, string name, Sprite sprite, float x, float y)
        {
            if (parent == null || sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.useSpriteMesh = false;
            img.raycastTarget = false;
        }

        private static RectTransform CreateRect(RectTransform parent, string name, RawNode node)
        {
            ResolveSourceRect(node, parent, out int x, out int y, out int w, out int h);
            var go = new GameObject(string.IsNullOrEmpty(name) ? "SourceControl" : name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(0, w), Mathf.Max(0, h));
            return rt;
        }

        // V396A7R5_UIA1: final-1.4 port of ParentFrame::ProcessAligning() for XML-authored controls.
        // The old renderer treated x/y as final positions and ignored RelativeAlign /
        // AbsoluteRightAlign / AbsoluteBottomAlign.  That shifted the Help root, title,
        // buttons and several nested decorations away from the original coordinates.
        private static void ResolveSourceRect(RawNode node, RectTransform parent,
            out int x, out int y, out int width, out int height)
        {
            x = node != null ? node.Int("x", 0) : 0;
            y = node != null ? node.Int("y", 0) : 0;
            width = Math.Max(0, node != null ? node.Int("Width", 0) : 0);
            height = Math.Max(0, node != null ? node.Int("Height", 0) : 0);
            int x1 = x + Math.Max(0, width - 1);
            int y1 = y + Math.Max(0, height - 1);
            int pw = parent != null ? Mathf.RoundToInt(parent.rect.width) : 1024;
            int ph = parent != null ? Mathf.RoundToInt(parent.rect.height) : 768;

            string la = node != null ? node.Value("LeftAlign", "AbsoluteLeftAlign") : "AbsoluteLeftAlign";
            string ra = node != null ? node.Value("RightAlign", "AbsoluteLeftAlign") : "AbsoluteLeftAlign";
            string hca = node != null ? node.Value("HorizontalCenterAlign", "AbsoluteLeftAlign") : "AbsoluteLeftAlign";
            string ta = node != null ? node.Value("TopAlign", "AbsoluteTopAlign") : "AbsoluteTopAlign";
            string ba = node != null ? node.Value("BottomAlign", "AbsoluteTopAlign") : "AbsoluteTopAlign";
            string vca = node != null ? node.Value("VerticalCenterAlign", "AbsoluteTopAlign") : "AbsoluteTopAlign";

            if (la.Equals("AbsoluteRightAlign", StringComparison.OrdinalIgnoreCase))
                x = pw - Round14(node.Float("LeftAlignParam", 0f));
            else if (la.Equals("RelativeAlign", StringComparison.OrdinalIgnoreCase))
                x = Round14(pw * node.Float("LeftAlignParam", 0f));

            if (ra.Equals("AbsoluteRightAlign", StringComparison.OrdinalIgnoreCase))
                x1 = pw - Round14(node.Float("RightAlignParam", 0f)) - 1;
            else if (ra.Equals("RelativeAlign", StringComparison.OrdinalIgnoreCase))
                x1 = Round14(pw * node.Float("RightAlignParam", 0f));

            if (hca.Equals("AbsoluteRightAlign", StringComparison.OrdinalIgnoreCase))
            {
                int dx = pw - Round14(node.Float("HCenterAlignParam", 0f)) - ((x + x1) / 2);
                x += dx; x1 += dx;
            }
            else if (hca.Equals("RelativeAlign", StringComparison.OrdinalIgnoreCase))
            {
                int dx = Round14(pw * node.Float("HCenterAlignParam", 0f)) - ((x + x1) / 2);
                x += dx; x1 += dx;
            }

            if (ta.Equals("AbsoluteBottomAlign", StringComparison.OrdinalIgnoreCase))
                y = ph - Round14(node.Float("TopAlignParam", 0f));
            else if (ta.Equals("RelativeAlign", StringComparison.OrdinalIgnoreCase))
                y = Round14(ph * node.Float("TopAlignParam", 0f));

            if (ba.Equals("AbsoluteBottomAlign", StringComparison.OrdinalIgnoreCase))
                y1 = ph - Round14(node.Float("BottomAlignParam", 0f)) - 1;
            else if (ba.Equals("RelativeAlign", StringComparison.OrdinalIgnoreCase))
                y1 = Round14(ph * node.Float("BottomAlignParam", 0f));

            if (vca.Equals("AbsoluteBottomAlign", StringComparison.OrdinalIgnoreCase))
            {
                int dy = ph - Round14(node.Float("VCenterAlignParam", 0f)) - ((y + y1) / 2);
                y += dy; y1 += dy;
            }
            else if (vca.Equals("RelativeAlign", StringComparison.OrdinalIgnoreCase))
            {
                int dy = Round14(ph * node.Float("VCenterAlignParam", 0f)) - ((y + y1) / 2);
                y += dy; y1 += dy;
            }

            width = Math.Max(0, x1 - x + 1);
            height = Math.Max(0, y1 - y + 1);
        }

        // engine_1.4.exe ParentFrame::ProcessAligning uses ROUND for all
        // alignment parameters/products.  Target final 1.4 semantics, not 1.1 truncation.
        private static int Round14(float value) => Mathf.RoundToInt(value);

        private static Color ParseArgb(string value, Color fallback)
        {
            string h = (value ?? string.Empty).Trim().TrimStart('#');
            if (h.Length != 8 || !uint.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                return fallback;
            byte a = (byte)((v >> 24) & 0xFF);
            byte r = (byte)((v >> 16) & 0xFF);
            byte g = (byte)((v >> 8) & 0xFF);
            byte b = (byte)(v & 0xFF);
            return new Color32(r, g, b, a);
        }

        private sealed class SourceAction
        {
            public string Name = string.Empty;
            public string Payload = string.Empty;
        }

        private static List<SourceAction> ParseActions(RawNode node)
        {
            var list = new List<SourceAction>();
            RawNode va = node?.Child("v_Actions");
            if (va == null) return list;
            for (int i = 0; i < va.Children.Count; i++)
            {
                RawNode a = va.Children[i];
                if (string.IsNullOrWhiteSpace(a.Tag)) continue;
                list.Add(new SourceAction { Name = a.Tag, Payload = SerializeChildren(a) });
            }
            return list;
        }

        private static bool IsHelpCloseAction(List<SourceAction> actions)
        {
            if (actions == null) return false;
            for (int i = 0; i < actions.Count; i++)
            {
                string n = actions[i]?.Name ?? string.Empty;
                if (n.Equals("cva_ItemChoose_Set", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("cva_M_ModalDeskSet", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasAction(RawNode node, string actionName)
        {
            RawNode va = node?.Child("v_Actions");
            if (va == null) return false;
            for (int i = 0; i < va.Children.Count; i++)
                if (string.Equals(va.Children[i].Tag, actionName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string SerializeChildren(RawNode node)
        {
            if (node == null) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < node.Children.Count; i++) SerializeNode(node.Children[i], sb);
            return sb.ToString();
        }

        private static void SerializeNode(RawNode node, StringBuilder sb)
        {
            if (node == null || sb == null || string.IsNullOrEmpty(node.Tag)) return;
            sb.Append('<').Append(node.Tag).Append('>');
            string direct = node.DirectText();
            if (!string.IsNullOrEmpty(direct)) sb.Append(direct);
            for (int i = 0; i < node.Children.Count; i++) SerializeNode(node.Children[i], sb);
            sb.Append("</").Append(node.Tag).Append('>');
        }

        // Single parser source for all menu XML-like data.  The previous local
        // RawNode/ParseLooseXml path duplicated layout parsing and was the same
        // architectural failure already identified in the A7 audit.
        private static RawNode ParseSharedXml(string raw)
        {
            return Menu14UnifiedLoader.LiteDocument.Parse(raw);
        }

        private static RawNode FindDialogsDeskByName(RawNode root, string name)
        {
            if (root == null) return null;
            if (root.Tag.Equals("DialogsDesk", StringComparison.OrdinalIgnoreCase) &&
                root.Value("Name", string.Empty).Equals(name ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                return root;
            for (int i = 0; i < root.Children.Count; i++)
            {
                RawNode r = FindDialogsDeskByName(root.Children[i], name);
                if (r != null) return r;
            }
            return null;
        }

        private static void Collect(RawNode root, string tag, List<RawNode> output)
        {
            if (root == null || output == null) return;
            if (root.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)) output.Add(root);
            for (int i = 0; i < root.Children.Count; i++) Collect(root.Children[i], tag, output);
        }

        private static bool LooksLikeControl(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            return tag.Equals("DialogsDesk", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("GPPicture", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("VitButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("TextButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("BitPicture", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("GP_TextButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("GP_Button", StringComparison.OrdinalIgnoreCase);
        }

        private string ReadGameMenuText(string relativePath)
        {
            // Same source policy as Menu14UnifiedLoader: bundled clean 1.4 menu
            // XML first, installed DataRoot second. Both are original game files.
            string cleanRoot = System.IO.Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data");
            string clean = ResolveCaseInsensitive(cleanRoot, relativePath);
            if (!string.IsNullOrEmpty(clean) && System.IO.File.Exists(clean))
                return ReadTextEncodingAware(clean);

            try
            {
                if (_fs != null && _fs.Exists(relativePath))
                    return _fs.ReadAllText(relativePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[C2:BFE14 CAMPAIGN XML V396A7R5_UIA1] DataRoot text read failed '{relativePath}': {ex.GetType().Name}: {ex.Message}");
            }
            return string.Empty;
        }

        private static string ResolveCaseInsensitive(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
            string[] parts = relativePath.Replace('/', '\\').Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string cur = root;
            for (int i = 0; i < parts.Length; i++)
            {
                string direct = System.IO.Path.Combine(cur, parts[i]);
                if (System.IO.File.Exists(direct) || System.IO.Directory.Exists(direct))
                {
                    cur = direct;
                    continue;
                }
                if (!System.IO.Directory.Exists(cur)) return string.Empty;
                string[] entries = System.IO.Directory.GetFileSystemEntries(cur);
                string match = string.Empty;
                for (int j = 0; j < entries.Length; j++)
                {
                    if (string.Equals(System.IO.Path.GetFileName(entries[j]), parts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        match = entries[j];
                        break;
                    }
                }
                if (string.IsNullOrEmpty(match)) return string.Empty;
                cur = match;
            }
            return cur;
        }

        private static string ReadTextEncodingAware(string path)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            try { return new UTF8Encoding(false, true).GetString(bytes); }
            catch { return C2LegacyText14.DecodeCp1251(bytes); }
        }

        private static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unnamed";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else if (c == '#' || c == '\\' || c == '/' || char.IsWhiteSpace(c)) sb.Append('_');
            }
            return sb.Length == 0 ? "unnamed" : sb.ToString();
        }
    }
}
