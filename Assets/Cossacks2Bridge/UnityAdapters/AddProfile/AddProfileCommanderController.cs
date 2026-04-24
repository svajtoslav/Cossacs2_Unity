using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cossacks2Bridge.Core;
using TemnyLessCodec; // com.temnyless.codec (Melinoja.dll) facade

namespace Cossacks2Bridge.UnityAdapters.AddProfile
{
    /// <summary>
    /// Runtime glue for AddProfile: nation -> commander portraits + description + portrait scroller.
    /// Portraits: Interf3_TotalWarGraph_lva_XXs.g16 from Data\Cash (or project Resources cache if present).
    /// Descriptions: Missions\Heroes\heroinf{nationIndex}{heroId}.txt.
    /// </summary>
    public sealed class AddProfileCommanderController : MonoBehaviour
    {
        private const bool DBG = true;

        // ===== TUNABLE LAYOUT =====
        // Text zone (red rectangle)
        public static Vector2 TextZonePosition = new Vector2(133f, -5f);
        public static Vector2 TextZoneSize = new Vector2(332f, 142f);

        // Scroll zone (white rectangle)
        public static float ScrollOffsetFromText = 1f;
        public static float ScrollWidth = 11f;
        public static float ScrollButtonHeight = 11f;
        public static float ScrollThumbHeight = 14f;

        // Fine tuning
        public static Vector2 DescContentOffset = new Vector2(7f, -5f);
        public static Vector2 DescTextSize = new Vector2(300f, 118f);
        public static float TextBorderThickness = 1f;
        public static float TextZoneBackgroundAlpha = 0.98f;
        public static string TextZonePaperResource = "ui/paper_bg_1";
        public static float TextZonePaperAlpha = 0.30f;
        public static bool TextZonePaperTiled = false;
        // ===== END TUNABLE LAYOUT =====


        private static void Log(string msg) { if (DBG) Debug.Log("[AddProfile] " + msg); }
        private static void LogW(string msg) { if (DBG) Debug.LogWarning("[AddProfile] " + msg); }
        private static void LogE(string msg) { Debug.LogError("[AddProfile] " + msg); }

        // Nation order as in original AddProfile combo
        private static readonly string[] NationIdOrder =
        {
            "FRANCE", "RUSSIA", "ENGLAND", "PRUSSIA", "AUSTRIA", "EGIPET"
        };

        // heroinf.dat mapping fallback EN=0 FR=1 RU=2 AU=3 PR=4 EG=5
        private static readonly Dictionary<string, int> HeroInfIndexFallback = new(StringComparer.OrdinalIgnoreCase)
        {
            { "EN", 0 },
            { "FR", 1 },
            { "RU", 2 },
            { "AU", 3 },
            { "PR", 4 },
            { "EG", 5 },
        };

        private CoreFileSystem _fs;
        private LocDb _loc;

        private RectTransform _portraitBorderFrame;
        private RectTransform _portraitSlot;
        private Image _portraitImage;
        private bool _didLatePlace;
        private TextMeshProUGUI _descText;
        private ScrollRect _descScroll;
        private HorizontalScrollbarController _portraitScroll;
        private bool _didLateRestyle;

        private RectTransform _commanderZoneBg;
        private RectTransform _descViewport;
        private RectTransform _descContent;
        private VerticalScrollbarController _descScrollbarCtrl;
        private RectTransform _customDescScrollbarRoot;
        private bool _didBuildDescriptionUi;

        private int _currentNationIdx;
        private int _currentHeroIdx;
        private Sprite[] _currentPortraitSprites;

        private readonly Dictionary<string, Sprite[]> _portraitCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(int nation, int hero), string> _descCache = new();
        private static readonly Dictionary<string, Sprite> _paperSpriteCache = new(StringComparer.OrdinalIgnoreCase);

        public void Init(CoreFileSystem fs, LocDb loc)
        {
            _fs = fs;
            _loc = loc;
        }


private void Start()
{
    Log("Start()");

    if (_fs == null)
    {
        var boot = FindFirstObjectByType<MenuBootstrap>();
        Log($"MenuBootstrap found: {boot != null}");
        if (boot != null)
        {
            _fs = boot.Fs;
            _loc = boot.Loc;
            Log("Got Fs/Loc from MenuBootstrap");
        }
    }

    var borderGO = GameObject.Find("GPPicture_ProfAdd_PortFrame");
    var slotGO = GameObject.Find("GPPicture_ProfAdd_Port");
    if (slotGO == null)
        slotGO = borderGO?.transform.Find("GPPicture_ProfAdd_Port")?.gameObject;

    Log($"Find border GO 'GPPicture_ProfAdd_PortFrame': {(borderGO != null ? borderGO.name : "<null>")}");
    Log($"Find slot GO 'GPPicture_ProfAdd_Port': {(slotGO != null ? slotGO.name : "<null>")}");

    _portraitBorderFrame = borderGO != null ? borderGO.GetComponent<RectTransform>() : null;
    _portraitSlot = slotGO != null ? slotGO.GetComponent<RectTransform>() : null;

    _portraitImage = EnsurePortraitImage(_portraitSlot);
    _descText = FindDescText();
    _descScroll = _descText != null ? _descText.GetComponentInParent<ScrollRect>() : null;

    var scrollGO = GameObject.Find("HScroll_ProfAdd_PortScr");
    _portraitScroll = scrollGO != null ? (scrollGO.GetComponent<HorizontalScrollbarController>() ?? scrollGO.GetComponentInChildren<HorizontalScrollbarController>(true)) : null;
    Log($"Find portrait HScroll 'HScroll_ProfAdd_PortScr': {_portraitScroll != null}");

    HookNationCombo();
    HookPortraitScroller();

    // First pass.
    BuildDescriptionUi();
    RestylePortraitScroller();
    RestyleDescriptionScroller();

    _currentNationIdx = GuessNationIndexFromComboText() ?? 0;
    ApplyNation(_currentNationIdx, resetHero: true);
}

private void LateUpdate()
{
    if (_portraitBorderFrame == null && _portraitSlot == null)
        return;

    if (!_didLatePlace)
    {
        BuildDescriptionUi();
        RestylePortraitScroller();
        RestyleDescriptionScroller();
        _didLatePlace = true;
    }
    else if (!_didLateRestyle)
    {
        // one more pass next frame after layout settles
        BuildDescriptionUi();
        RestylePortraitScroller();
        RestyleDescriptionScroller();
        RefreshDescriptionScrollRange();
        _didLateRestyle = true;
    }
}
        private static Image EnsurePortraitImage(RectTransform portraitFrame)
        {
            if (portraitFrame == null) return null;

            var existing = portraitFrame.Find("PortraitImage");
            if (existing != null)
            {
                var img = existing.GetComponent<Image>();
                if (img != null) return img;
            }

            var go = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(portraitFrame, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3, 3);
            rt.offsetMax = new Vector2(-3, -3);

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }


private static void PlacePortraitScrollerUnderFrame(RectTransform frame, HorizontalScrollbarController scroll)
{
    if (frame == null || scroll == null) return;

    var root = scroll.GetComponent<RectTransform>();
    if (root == null) return;

    var parent = frame.parent as RectTransform;
    if (parent == null) return;
    if (root.parent != parent)
        root.SetParent(parent, false);

    // Original AddProfile portrait scroller is a rotated vertical scrollbar:
    // native size in XML is 15x119 with angle=270, so visually it becomes 119x15.
    const float nativeWidth = 119f;
    const float nativeHeight = 15f;
    const float margin = 3f;

    root.anchorMin = root.anchorMax = new Vector2(0, 1);
    root.pivot = new Vector2(0, 1);
    root.sizeDelta = new Vector2(nativeWidth, nativeHeight);
    root.anchoredPosition = new Vector2(
        frame.anchoredPosition.x + Mathf.Round((frame.rect.width - nativeWidth) * 0.5f),
        frame.anchoredPosition.y - frame.rect.height - margin
    );

    root.SetAsLastSibling();
    Log($"Portrait scroller placed: parent={parent.name} pos={root.anchoredPosition} size={root.sizeDelta}");
}

private static Image CreateBorderedBg(RectTransform parent, string name, Vector2 pos, Vector2 size, Color outer, Color inner)
{
    var go = parent.Find(name) as RectTransform;
    if (go == null)
    {
        var ngo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        ngo.transform.SetParent(parent, false);
        go = (RectTransform)ngo.transform;
    }

    go.anchorMin = go.anchorMax = new Vector2(0, 1);
    go.pivot = new Vector2(0, 1);
    go.anchoredPosition = pos;
    go.sizeDelta = size;

    var img = go.GetComponent<Image>();
    img.color = inner;
    img.raycastTarget = false;

    var outline = go.GetComponent<Outline>();
    if (outline == null) outline = go.gameObject.AddComponent<Outline>();
    outline.effectColor = outer;
    outline.effectDistance = new Vector2(TextBorderThickness, -TextBorderThickness);
    outline.useGraphicAlpha = true;
    return img;
}

private static Sprite LoadResSprite(string folder, string frame)
        {
            return Resources.Load<Sprite>($"{folder}/{frame}");
        }

private static Sprite LoadPaperSprite(string resourcePath)
{
    if (string.IsNullOrWhiteSpace(resourcePath)) return null;
    if (_paperSpriteCache.TryGetValue(resourcePath, out var cached) && cached != null)
        return cached;

    var tex = Resources.Load<Texture2D>(resourcePath);
    if (tex == null) return null;

    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    sprite.name = "Paper_" + Path.GetFileNameWithoutExtension(resourcePath);
    _paperSpriteCache[resourcePath] = sprite;
    return sprite;
}

private static void ApplyTextZonePaper(Image viewportImg)
{
    if (viewportImg == null) return;

    var viewport = viewportImg.rectTransform;
    var fill = viewport.Find("PaperFill") as RectTransform;
    if (fill == null)
    {
        var go = new GameObject("PaperFill", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(viewport, false);
        fill = (RectTransform)go.transform;
    }

    fill.anchorMin = Vector2.zero;
    fill.anchorMax = Vector2.one;
    fill.pivot = new Vector2(0.5f, 0.5f);
    fill.offsetMin = Vector2.zero;
    fill.offsetMax = Vector2.zero;
    fill.SetAsFirstSibling();

    var fillImg = fill.GetComponent<Image>();
    fillImg.raycastTarget = false;
    fillImg.preserveAspect = false;

    var paper = LoadPaperSprite(TextZonePaperResource);
    if (paper == null)
    {
        fillImg.sprite = null;
        fillImg.color = Color.clear;
    }
    else
    {
        fillImg.sprite = paper;
        fillImg.type = TextZonePaperTiled ? Image.Type.Tiled : Image.Type.Simple;
        fillImg.color = new Color(1f, 1f, 1f, Mathf.Clamp01(TextZonePaperAlpha));
    }

    // Base viewport should keep only mask + layout, no fill.
    viewportImg.sprite = null;
    viewportImg.type = Image.Type.Simple;
    viewportImg.color = new Color(1f, 1f, 1f, 0f);
    viewportImg.raycastTarget = false;
}

private static void EnsureViewportFrame(RectTransform viewport)
{
    if (viewport == null) return;

    var root = viewport.Find("FrameRoot") as RectTransform;
    if (root == null)
    {
        var go = new GameObject("FrameRoot", typeof(RectTransform));
        go.transform.SetParent(viewport, false);
        root = (RectTransform)go.transform;
    }

    root.anchorMin = Vector2.zero;
    root.anchorMax = Vector2.one;
    root.pivot = new Vector2(0.5f, 0.5f);
    root.offsetMin = Vector2.zero;
    root.offsetMax = Vector2.zero;
    root.SetAsLastSibling();

    float t = Mathf.Max(1f, TextBorderThickness);
    var borderColor = new Color(0.58f, 0.16f, 0.16f, 0.95f);

    void MakeLine(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = root.Find(name) as RectTransform;
        if (rt == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            rt = (RectTransform)go.transform;
        }

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        var img = rt.GetComponent<Image>();
        img.color = borderColor;
        img.raycastTarget = false;
    }

    MakeLine("Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -t), new Vector2(0f, 0f));
    MakeLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),  new Vector2(0f, t));
    MakeLine("Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f),  new Vector2(t, 0f));
    MakeLine("Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-t, 0f), new Vector2(0f, 0f));
}

private static void EnsureFillTile(RectTransform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
{
    if (sprite == null) return;
    var t = parent.Find(name) as RectTransform;
    if (t == null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        t = (RectTransform)go.transform;
    }

    t.anchorMin = anchorMin;
    t.anchorMax = anchorMax;
    t.pivot = new Vector2(0.5f, 0.5f);
    t.offsetMin = Vector2.zero;
    t.offsetMax = Vector2.zero;

    var img = t.GetComponent<Image>();
    img.sprite = sprite;
    img.type = Image.Type.Tiled;
    img.raycastTarget = false;
    img.color = Color.white;
    t.SetAsFirstSibling();
}

private static void ApplyBdViewportFill(RectTransform viewport)
{
    if (viewport == null) return;

    var fillRoot = viewport.Find("BdFillRoot") as RectTransform;
    if (fillRoot == null)
    {
        var go = new GameObject("BdFillRoot", typeof(RectTransform));
        go.transform.SetParent(viewport, false);
        fillRoot = (RectTransform)go.transform;
    }

    fillRoot.anchorMin = Vector2.zero;
    fillRoot.anchorMax = Vector2.one;
    fillRoot.pivot = new Vector2(0.5f, 0.5f);
    fillRoot.offsetMin = Vector2.zero;
    fillRoot.offsetMax = Vector2.zero;
    fillRoot.SetAsFirstSibling();

    var s00 = LoadResSprite("interf3_elements_border_BD_frames", "frame_0006");
    var s01 = LoadResSprite("interf3_elements_border_BD_frames", "frame_0009");
    var s10 = LoadResSprite("interf3_elements_border_BD_frames", "frame_0010");
    var s11 = LoadResSprite("interf3_elements_border_BD_frames", "frame_0011");

    EnsureFillTile(fillRoot, "TL", s00 ?? s10 ?? s11 ?? s01, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f));
    EnsureFillTile(fillRoot, "TR", s01 ?? s10 ?? s11 ?? s00, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f));
    EnsureFillTile(fillRoot, "BL", s10 ?? s11 ?? s00 ?? s01, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f));
    EnsureFillTile(fillRoot, "BR", s11 ?? s10 ?? s01 ?? s00, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f));
}

private static void DestroyChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var ch = t.GetChild(i);
                if (Application.isPlaying) UnityEngine.Object.Destroy(ch.gameObject);
                else UnityEngine.Object.DestroyImmediate(ch.gameObject);
            }
        }


private void RestylePortraitScroller()
{
    if (_portraitScroll == null) return;
    var root = _portraitScroll.GetComponent<RectTransform>();
    if (root == null) return;
    var target = _portraitBorderFrame != null ? _portraitBorderFrame : _portraitSlot;
    if (target == null) return;

    PlacePortraitScrollerUnderFrame(target, _portraitScroll);

    // Use the same visual family as the description scrollbar on the right.
    // In the original game this widget is just a VScrollBar rotated by 270°.
    var spArrowLeft = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0000");
    var spArrowRight = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0002");
    var spTrack = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0005")
                  ?? LoadResSprite("Interf3_elements_scroll3_frames", "frame_0007");
    var spThumb = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0004");

    DestroyChildren(root);

    float rootW = root.sizeDelta.x;
    float rootH = root.sizeDelta.y;
    float barThickness = Mathf.Max(rootH, Mathf.Max(spTrack != null ? spTrack.rect.width : rootH, spThumb != null ? spThumb.rect.width : rootH));
    float btnLen = spArrowLeft != null ? spArrowLeft.rect.height : 18f; // rotated 15x18 => 18x15 visually
    float trackLen = Mathf.Max(24f, rootW - btnLen * 2f);
    float thumbLen = spThumb != null ? spThumb.rect.height : 45f;       // rotated 15x45 => 45x15 visually

    root.sizeDelta = new Vector2(rootW, barThickness);

    Button MakeArrow(string name, float x, Sprite sprite, float rotZ)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x + btnLen * 0.5f, -barThickness * 0.5f);
        rt.sizeDelta = new Vector2(btnLen, barThickness);
        rt.localEulerAngles = new Vector3(0f, 0f, rotZ);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = true;
        return go.GetComponent<Button>();
    }

    var leftBtn = MakeArrow("ArrowLeft", 0f, spArrowLeft, 90f);
    var rightBtn = MakeArrow("ArrowRight", rootW - btnLen, spArrowRight, 90f);

    var trackGO = new GameObject("Track", typeof(RectTransform));
    trackGO.transform.SetParent(root, false);
    var trackRT = (RectTransform)trackGO.transform;
    trackRT.anchorMin = trackRT.anchorMax = new Vector2(0, 1);
    trackRT.pivot = new Vector2(0, 1);
    trackRT.anchoredPosition = new Vector2(btnLen, 0f);
    trackRT.sizeDelta = new Vector2(trackLen, barThickness);

    var trackVisualGO = new GameObject("TrackVisual", typeof(RectTransform), typeof(Image));
    trackVisualGO.transform.SetParent(trackRT, false);
    var trackVisualRT = (RectTransform)trackVisualGO.transform;
    trackVisualRT.anchorMin = trackVisualRT.anchorMax = new Vector2(0.5f, 0.5f);
    trackVisualRT.pivot = new Vector2(0.5f, 0.5f);
    trackVisualRT.sizeDelta = new Vector2(barThickness, trackLen);
    trackVisualRT.localEulerAngles = new Vector3(0f, 0f, -90f);
    var trackImg = trackVisualGO.GetComponent<Image>();
    trackImg.sprite = spTrack;
    trackImg.type = Image.Type.Tiled;
    trackImg.raycastTarget = true;
    trackImg.color = Color.white;

    var thumbGO = new GameObject("Thumb", typeof(RectTransform));
    thumbGO.transform.SetParent(trackRT, false);
    var thumbRT = (RectTransform)thumbGO.transform;
    thumbRT.anchorMin = thumbRT.anchorMax = new Vector2(0, 1);
    thumbRT.pivot = new Vector2(0, 1);
    thumbRT.sizeDelta = new Vector2(thumbLen, barThickness);
    thumbRT.anchoredPosition = Vector2.zero;

    var thumbVisualGO = new GameObject("ThumbVisual", typeof(RectTransform), typeof(Image));
    thumbVisualGO.transform.SetParent(thumbRT, false);
    var thumbVisualRT = (RectTransform)thumbVisualGO.transform;
    thumbVisualRT.anchorMin = thumbVisualRT.anchorMax = new Vector2(0.5f, 0.5f);
    thumbVisualRT.pivot = new Vector2(0.5f, 0.5f);
    thumbVisualRT.sizeDelta = new Vector2(barThickness, thumbLen);
    thumbVisualRT.localEulerAngles = new Vector3(0f, 0f, -90f);
    var thumbImg = thumbVisualGO.GetComponent<Image>();
    thumbImg.sprite = spThumb;
    thumbImg.type = Image.Type.Simple;
    thumbImg.preserveAspect = false;
    thumbImg.raycastTarget = true;

    _portraitScroll.Initialize(
        trackRT,
        thumbRT,
        Mathf.Max(1, (_currentPortraitSprites?.Length ?? 1) - 1),
        Mathf.Clamp(_currentHeroIdx, 0, Mathf.Max(0, (_currentPortraitSprites?.Length ?? 1) - 1)));

    leftBtn.onClick.AddListener(() => _portraitScroll.SetValue(_portraitScroll.Value - 1));
    rightBtn.onClick.AddListener(() => _portraitScroll.SetValue(_portraitScroll.Value + 1));
}

private void BuildDescriptionUi()
{
    if (_portraitBorderFrame == null) return;
    if (_descText == null) _descText = FindDescText();
    if (_descText == null) return;
    var parent = _portraitBorderFrame.parent as RectTransform;
    if (parent == null) return;

    // Hide legacy scrollbar visuals if any.
    if (_descScroll != null)
    {
        _descScroll.enabled = false;
        if (_descScroll.verticalScrollbar != null)
            _descScroll.verticalScrollbar.gameObject.SetActive(false);
    }

    // Keep commander container for local coordinates, but make its background almost invisible for comparison test.
    var bgImg = CreateBorderedBg(
        parent,
        "ProfAdd_CommanderZoneBg",
        new Vector2(_portraitBorderFrame.anchoredPosition.x - 2f, _portraitBorderFrame.anchoredPosition.y + 2f),
        new Vector2(470f, 157f),
        new Color(0.70f, 0.60f, 0.50f, 0.00f),
        new Color(0.95f, 0.93f, 0.89f, 0.00f));

    _commanderZoneBg = bgImg.rectTransform;
    _commanderZoneBg.SetSiblingIndex(Mathf.Max(0, _portraitBorderFrame.GetSiblingIndex() - 1));

    var viewportImg = CreateBorderedBg(
        _commanderZoneBg,
        "DescViewport",
        TextZonePosition,
        TextZoneSize,
        new Color(0.60f, 0.18f, 0.18f, 0.82f),
        new Color(1f, 1f, 1f, 0f));
    ApplyTextZonePaper(viewportImg);

    _descViewport = viewportImg.rectTransform;
    RemoveChildIfExists(_descViewport, "BdFillRoot");

    var legacyMask = _descViewport.GetComponent<Mask>();
    if (legacyMask != null)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(legacyMask);
        else Destroy(legacyMask);
#else
        Destroy(legacyMask);
#endif
    }

    var rectMask = _descViewport.GetComponent<RectMask2D>();
    if (rectMask == null) rectMask = _descViewport.gameObject.AddComponent<RectMask2D>();

    var outline = _descViewport.GetComponent<Outline>();
    if (outline != null)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(outline);
        else Destroy(outline);
#else
        Destroy(outline);
#endif
    }

    EnsureViewportFrame(_descViewport);

    var content = _descViewport.Find("DescContent") as RectTransform;
    if (content == null)
    {
        var go = new GameObject("DescContent", typeof(RectTransform));
        go.transform.SetParent(_descViewport, false);
        content = (RectTransform)go.transform;
    }
    _descContent = content;
    _descContent.anchorMin = _descContent.anchorMax = new Vector2(0, 1);
    _descContent.pivot = new Vector2(0, 1);
    _descContent.anchoredPosition = DescContentOffset;
    _descContent.sizeDelta = new Vector2(_descViewport.rect.width - 18f, _descViewport.rect.height - 10f);

    var descGO = _descText.transform as RectTransform;
    if (descGO != null && descGO.parent != _descContent)
        descGO.SetParent(_descContent, false);

    _descText.transform.SetAsLastSibling();

    var rt = _descText.rectTransform;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(0f, 0f);
    rt.sizeDelta = DescTextSize;
    _descText.enableWordWrapping = true;
    _descText.overflowMode = TextOverflowModes.Overflow;
    _descText.raycastTarget = false;
    _descText.color = new Color32(32, 24, 20, 255);

    BuildDescriptionScrollbar();
    RefreshDescriptionScrollRange();

    _didBuildDescriptionUi = true;
}


private void BuildDescriptionScrollbar()
{
    if (_commanderZoneBg == null) return;

    if (_customDescScrollbarRoot == null)
    {
        var old = _commanderZoneBg.Find("ProfAdd_DescScrollbar") as RectTransform;
        if (old != null) _customDescScrollbarRoot = old;
        else
        {
            var go = new GameObject("ProfAdd_DescScrollbar", typeof(RectTransform));
            go.transform.SetParent(_commanderZoneBg, false);
            _customDescScrollbarRoot = (RectTransform)go.transform;
        }
    }

    var spUp = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0000");
    var spDown = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0002");
    var spTrack = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0005") ?? LoadResSprite("Interf3_elements_scroll3_frames", "frame_0007");
    var spThumb = LoadResSprite("Interf3_elements_scroll3_frames", "frame_0004");

    float upW = spUp != null ? spUp.rect.width : ScrollWidth;
    float upH = spUp != null ? spUp.rect.height : ScrollButtonHeight;
    float downW = spDown != null ? spDown.rect.width : ScrollWidth;
    float downH = spDown != null ? spDown.rect.height : ScrollButtonHeight;
    float trackW = spTrack != null ? spTrack.rect.width : ScrollWidth;
    float trackUnitH = spTrack != null ? spTrack.rect.height : 8f;
    float thumbW = spThumb != null ? spThumb.rect.width : ScrollWidth;
    float thumbH = spThumb != null ? spThumb.rect.height : ScrollThumbHeight;

    float nativeW = Mathf.Max(Mathf.Max(upW, downW), Mathf.Max(trackW, thumbW));
    float trackH = Mathf.Max(trackUnitH, TextZoneSize.y - upH - downH);

    var root = _customDescScrollbarRoot;
    root.anchorMin = root.anchorMax = new Vector2(0, 1);
    root.pivot = new Vector2(0, 1);
    root.anchoredPosition = new Vector2(TextZonePosition.x + TextZoneSize.x + ScrollOffsetFromText, TextZonePosition.y);
    root.sizeDelta = new Vector2(nativeW, upH + trackH + downH);

    DestroyChildren(root);

    Button MakeVButton(string name, Sprite sp, bool top)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = top ? new Vector2(0, 1) : new Vector2(0, 0);
        rt.pivot = top ? new Vector2(0, 1) : new Vector2(0, 0);
        rt.anchoredPosition = Vector2.zero;
        float bw = sp != null ? sp.rect.width : nativeW;
        float bh = sp != null ? sp.rect.height : (top ? upH : downH);
        rt.sizeDelta = new Vector2(bw, bh);
        var img = go.GetComponent<Image>();
        img.sprite = sp;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = true;
        return go.GetComponent<Button>();
    }

    var up = MakeVButton("ArrowUp", spUp, true);
    var down = MakeVButton("ArrowDown", spDown, false);

    var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
    trackGO.transform.SetParent(root, false);
    var trackRT = (RectTransform)trackGO.transform;
    trackRT.anchorMin = trackRT.anchorMax = new Vector2(0, 1);
    trackRT.pivot = new Vector2(0, 1);
    trackRT.anchoredPosition = new Vector2(0f, -upH);
    trackRT.sizeDelta = new Vector2(trackW, trackH);
    var trackImg = trackGO.GetComponent<Image>();
    trackImg.sprite = spTrack;
    trackImg.type = Image.Type.Tiled;
    trackImg.raycastTarget = true;

    var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
    thumbGO.transform.SetParent(trackGO.transform, false);
    var thumbRT = (RectTransform)thumbGO.transform;
    thumbRT.anchorMin = thumbRT.anchorMax = new Vector2(0, 1);
    thumbRT.pivot = new Vector2(0, 1);
    thumbRT.sizeDelta = new Vector2(thumbW, thumbH);
    var thumbImg = thumbGO.GetComponent<Image>();
    thumbImg.sprite = spThumb;
    thumbImg.type = Image.Type.Simple;
    thumbImg.preserveAspect = false;
    thumbImg.raycastTarget = true;

    _descScrollbarCtrl = root.GetComponent<VerticalScrollbarController>();
    if (_descScrollbarCtrl == null) _descScrollbarCtrl = root.gameObject.AddComponent<VerticalScrollbarController>();
    _descScrollbarCtrl.Initialize(trackRT, thumbRT, 1, 0);
    _descScrollbarCtrl.OnValueChanged = pos =>
    {
        if (_descContent == null || _descViewport == null) return;
        _descContent.anchoredPosition = new Vector2(0f, pos);
    };

    up.onClick.AddListener(() => _descScrollbarCtrl.SetValue(_descScrollbarCtrl.Value - 18));
    down.onClick.AddListener(() => _descScrollbarCtrl.SetValue(_descScrollbarCtrl.Value + 18));
}

private void RefreshDescriptionScrollRange()
{
    if (_descText == null || _descViewport == null || _descContent == null || _descScrollbarCtrl == null) return;

    _descText.ForceMeshUpdate();
    float textW = 300f;
    float prefH = Mathf.Max(_descViewport.rect.height - 10f, _descText.GetPreferredValues(_descText.text, textW, 0f).y + 6f);
    _descText.rectTransform.sizeDelta = new Vector2(textW, prefH);
    _descContent.sizeDelta = new Vector2(_descViewport.rect.width - 18f, prefH);

    int max = Mathf.Max(0, Mathf.CeilToInt(prefH - _descViewport.rect.height));
    _descScrollbarCtrl.SetMax(Mathf.Max(1, max));
    _descScrollbarCtrl.SetValue(0, notify: true);
}

private void RestyleDescriptionScroller()
{
    BuildDescriptionUi();
    RefreshDescriptionScrollRange();
}


private static void RemoveChildIfExists(RectTransform parent, string childName)
{
    if (parent == null) return;
    var ch = parent.Find(childName);
    if (ch != null)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(ch.gameObject);
        else Destroy(ch.gameObject);
#else
        Destroy(ch.gameObject);
#endif
    }
}

        private TextMeshProUGUI FindDescText()
        {
            // Description renderer names it "TextButton_ProfAdd_Desc".
            var go = GameObject.Find("TextButton_ProfAdd_Desc");
            if (go == null) return null;
            return go.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        }

        private void HookNationCombo()
        {
            var comboGO = GameObject.Find("ComboBox_ProfAdd_Race");
            if (comboGO == null)
            {
                LogW("ComboBox_ProfAdd_Race not found");
                return;
            }

            var c = comboGO.GetComponentsInChildren<MonoBehaviour>(true)
                .FirstOrDefault(mb => mb != null && mb.GetType().Name.Contains("ComboBoxController"));
            if (c == null)
            {
                LogW("ComboBoxController not found on ComboBox_ProfAdd_Race");
                return;
            }

            var field = c.GetType().GetField("OnSelected");
            if (field == null)
            {
                LogW("ComboBoxController.OnSelected field not found (reflection)");
                return;
            }

            Action<int, string> handler = (index, _text) =>
            {
                _currentNationIdx = Mathf.Clamp(index, 0, NationIdOrder.Length - 1);
                ApplyNation(_currentNationIdx, resetHero: true);
            };

            try
            {
                // If field is exactly Action<int,string> (or compatible) - assign directly
                if (field.FieldType.IsAssignableFrom(handler.GetType()))
                {
                    field.SetValue(c, handler);
                }
                else
                {
                    // Create delegate of required type if possible
                    var del = Delegate.CreateDelegate(field.FieldType, handler.Target, handler.Method, throwOnBindFailure: false);
                    if (del != null) field.SetValue(c, del);
                    else LogW($"OnSelected type mismatch: fieldType={field.FieldType.FullName}");
                }

                Log("Nation combo hooked");
            }
            catch (Exception e)
            {
                LogW("HookNationCombo failed: " + e.Message);
            }
        }

        private void HookPortraitScroller()
        {
            if (_portraitScroll == null) return;

            _portraitScroll.OnValueChanged += pos =>
            {
                _currentHeroIdx = Mathf.Clamp(pos, 0, _portraitScroll.Max);
                ApplyHero(_currentNationIdx, _currentHeroIdx);
            };

            Log("Portrait scroller hooked");
        }

        private int? GuessNationIndexFromComboText()
        {
            var selectedText = GameObject.Find("ComboBox_ProfAdd_Race")
                ?.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t != null && t.gameObject.name == "SelectedText");

            if (selectedText == null) return null;

            string val = (selectedText.text ?? "").Trim();
            if (string.IsNullOrEmpty(val)) return null;

            // Map displayed strings back to our fixed order by loc keys (fallback: compare localized)
            for (int i = 0; i < NationIdOrder.Length; i++)
            {
                string nationId = NationIdOrder[i];
                string key = $"#Nation_{nationId}";
                string loc = _loc?.Resolve(key) ?? "";
                if (!string.IsNullOrEmpty(loc) && string.Equals(loc.Trim(), val, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return null;
        }

        private void ApplyNation(int nationIdx, bool resetHero)
        {
            if (_portraitImage == null) return;

            string nationId = NationIdOrder[Mathf.Clamp(nationIdx, 0, NationIdOrder.Length - 1)];

            if (!_portraitCache.TryGetValue(nationId, out _currentPortraitSprites))
            {
                _currentPortraitSprites = LoadNationPortraitSprites(nationId);
                _portraitCache[nationId] = _currentPortraitSprites;
            }

            int maxHero = Mathf.Max(0, (_currentPortraitSprites?.Length ?? 1) - 1);
            if (_portraitScroll != null)
            {
                _portraitScroll.SetMax(maxHero);
                if (resetHero) _portraitScroll.SetValue(0, notify: false);
            }

            if (resetHero) _currentHeroIdx = 0;
            ApplyHero(nationIdx, _currentHeroIdx);
        }


private void ApplyHero(int nationIdx, int heroIdx)
{
    Log($"ApplyHero nationIdx={nationIdx} heroIdx={heroIdx} sprites={(_currentPortraitSprites != null ? _currentPortraitSprites.Length : 0)}");

    if (_portraitImage != null)
    {
        Sprite spr = null;
        if (_currentPortraitSprites != null && _currentPortraitSprites.Length > 0)
        {
            int idx = Mathf.Clamp(heroIdx, 0, _currentPortraitSprites.Length - 1);
            spr = _currentPortraitSprites[idx];
        }

        _portraitImage.sprite = spr;
        _portraitImage.enabled = spr != null;

        if (spr != null)
        {
            _portraitImage.SetNativeSize();
            var rt = _portraitImage.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.localScale = Vector3.one;

            var slot = _portraitSlot != null ? _portraitSlot : (_portraitImage.transform.parent as RectTransform);
            float pad = 0f;
            float sw = slot != null ? Mathf.Max(1f, slot.rect.width - pad * 2f) : rt.rect.width;
            float sh = slot != null ? Mathf.Max(1f, slot.rect.height - pad * 2f) : rt.rect.height;
            float iw = rt.sizeDelta.x;
            float ih = rt.sizeDelta.y;
            float scale = Mathf.Min(sw / Mathf.Max(1f, iw), sh / Mathf.Max(1f, ih));
            scale = Mathf.Max(0.01f, scale);
            float tw = iw * scale;
            float th = ih * scale;
            rt.sizeDelta = new Vector2(tw, th);
            float x = pad + (sw - tw) * 0.5f;
            float y = -(pad + (sh - th) * 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
        }
    }

    Log($"Portrait sprite set direct: {(_portraitImage != null && _portraitImage.sprite != null ? _portraitImage.sprite.name : "<null>")}");

    if (_descText != null)
    {
        _descText.text = LoadHeroDescription(nationIdx, heroIdx);
        RefreshDescriptionScrollRange();
    }
}

private Sprite[] LoadNationPortraitSprites(string nationId)
        {
            string cashDir = ResolveCashDir();

            // Nation -> suffix for lva_XXs
            string suffix = nationId switch
            {
                "FRANCE" => "FRs",
                "RUSSIA" => "RSs",
                "ENGLAND" => "EGs", // Britain in TotalWarGraph uses EG
                "PRUSSIA" => "PRs",
                "AUSTRIA" => "ASs",
                "EGIPET" => "EPs",
                _ => "FRs"
            };

            string g16Name = $"Interf3_TotalWarGraph_lva_{suffix}.g16";
            string g16Path = Path.Combine(cashDir, g16Name);

            if (!File.Exists(g16Path))
            {
                LogW($"Portrait G16 not found: {g16Path}");
                return Array.Empty<Sprite>();
            }

            string framesDir = Path.Combine(
                Path.GetDirectoryName(g16Path) ?? cashDir,
                Path.GetFileNameWithoutExtension(g16Path) + "_frames"
            );

            Log($"Portrait G16 found: {g16Path}");
            Log($"framesDir={framesDir} exists={Directory.Exists(framesDir)}");

            if (!Directory.Exists(framesDir) || Directory.GetFiles(framesDir, "frame_*.tga").Length == 0)
                TryDecodeG16(g16Path);

            var frameFiles = Directory.Exists(framesDir)
                ? Directory.GetFiles(framesDir, "frame_*.tga").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray()
                : Array.Empty<string>();

            var sprites = new List<Sprite>(frameFiles.Length);
            foreach (var ff in frameFiles)
            {
                var spr = LoadTgaAsSprite(ff);
                if (spr != null) sprites.Add(spr);
            }

            Log($"sprites loaded={sprites.Count}");
            return sprites.ToArray();
        }

        private static string ResolveCashDir()
        {
            // 1) Assets/Resources/Cash (if later moved into project)
            string projectCash = Path.Combine(Application.dataPath, "Resources", "Cash");
            if (Directory.Exists(projectCash)) return projectCash;

            // 2) External game cache (current workflow)
            return @"C:\GSC Game World\Cossacks II\Data\Cash";
        }

        private static void TryDecodeG16(string g16Path)
        {
            Log($"TryDecodeG16: {g16Path} exists={File.Exists(g16Path)}");
            try
            {
                // Writes into <g16>_frames next to the file.
                MelinojaCodecBridge.DecodeG16ToLogAndFrames(g16Path, out var logPath, out var err, doubleOverlay: false);
                Log($"CodecFacade done. log={logPath} err={(string.IsNullOrEmpty(err) ? "<none>" : err)}");
                if (!string.IsNullOrWhiteSpace(err))
                    LogW("Decode error: " + err);
            }
            catch (Exception e)
            {
                LogW("Decode failed: " + e.Message);
            }
        }

        private static Sprite LoadTgaAsSprite(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < 18) return null;

                int idLength = bytes[0];
                int colorMapType = bytes[1];
                int imageType = bytes[2];
                if (colorMapType != 0) return null;
                if (imageType != 2) return null; // uncompressed true-color

                int width = bytes[12] | (bytes[13] << 8);
                int height = bytes[14] | (bytes[15] << 8);
                int bpp = bytes[16];
                if (bpp != 32) return null;

                int dataOffset = 18 + idLength;
                int pixelCount = width * height;
                int needed = pixelCount * 4;
                if (dataOffset + needed > bytes.Length) return null;

                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var cols = new Color32[pixelCount];

                // TGA stored BGRA. Melinoja-exported frames arrive vertically inverted for Unity UI,
                // so normalize them here and keep RectTransform scale positive later.
                bool originTop = (bytes[17] & 0x20) != 0;
                for (int y = 0; y < height; y++)
                {
                    int sy = originTop ? (height - 1 - y) : y;
                    int rowOff = dataOffset + (sy * width * 4);
                    for (int x = 0; x < width; x++)
                    {
                        int i = rowOff + x * 4;
                        byte b = bytes[i + 0];
                        byte g = bytes[i + 1];
                        byte r = bytes[i + 2];
                        byte a = bytes[i + 3];
                        cols[y * width + x] = new Color32(r, g, b, a);
                    }
                }

                tex.SetPixels32(cols);
                tex.Apply(false, false);

                var spr = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
                spr.name = Path.GetFileNameWithoutExtension(path);
                return spr;
            }
            catch
            {
                return null;
            }
        }

        private string LoadHeroDescription(int nationIdx, int heroIdx)
        {
            if (_fs == null) return "";

            var key = (nationIdx, heroIdx);
            if (_descCache.TryGetValue(key, out var cached)) return cached;

            int heroNationIndex = DetermineHeroInfNationIndex(nationIdx);
            string rel = $"Missions/Heroes/heroinf{heroNationIndex}{heroIdx}.txt";

            string raw = _fs.ReadAllText(rel, Encoding.GetEncoding(1251));
            string parsed = ParseHeroText(raw);

            _descCache[key] = parsed;
            return parsed;
        }

        private int DetermineHeroInfNationIndex(int nationIdx)
        {
            string nationId = NationIdOrder[Mathf.Clamp(nationIdx, 0, NationIdOrder.Length - 1)];
            string code = nationId switch
            {
                "FRANCE" => "FR",
                "RUSSIA" => "RU",
                "ENGLAND" => "EN",
                "PRUSSIA" => "PR",
                "AUSTRIA" => "AU",
                "EGIPET" => "EG",
                _ => "FR"
            };

            try
            {
                string dat = _fs.ReadAllText("Missions/Heroes/heroinf.dat", Encoding.GetEncoding(1251));
                if (!string.IsNullOrEmpty(dat))
                {
                    foreach (string line in dat.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // e.g. (FR) 1
                        var m = Regex.Match(line, "\\((?<c>[A-Za-z]{2})\\)\\s*(?<n>\\d+)");
                        if (m.Success && string.Equals(m.Groups["c"].Value, code, StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(m.Groups["n"].Value, out int n)) return n;
                        }
                    }
                }
            }
            catch { }

            return HeroInfIndexFallback.TryGetValue(code, out int v) ? v : nationIdx;
        }
        private void OnDisable()
        {
            G16PortraitSessionCache.ClearSession();
        }
        private static string ParseHeroText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            string s = raw;

            // common escapes from scripts/texts
            s = s.Replace("\\\r\\\n", "\n");
            s = s.Replace("\\\n", "\n");

            // engine conventions
            s = s.Replace("{CR}", "\n");
            s = s.Replace("\\\\", "\n");

            // unescape \{TAG}
            s = s.Replace("\\{", "{");

            // strip formatting tags {...}
            s = Regex.Replace(s, "\\{[^}]*\\}", "");

            // cleanup whitespace
            s = Regex.Replace(s, "[ \t]+\n", "\n");
            s = Regex.Replace(s, "\n{3,}", "\n\n");

            return s.Trim();
        }
    }
}
