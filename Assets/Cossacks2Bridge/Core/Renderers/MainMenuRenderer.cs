using Cossacks2Bridge.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    /// <summary>
    /// Рендерер главного меню
    /// </summary>
    public sealed class MainMenuRenderer : BaseUiRenderer
    {
        private static readonly HashSet<string> HiddenTextKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "#Info",
            "#C2intfPreTut",
            "#MO_ArcadeMode"
        };

        private static readonly HashSet<string> SingleMenuVitKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "#EW2_Campaign",
            "#EW2_Battle4Europe",
            "#MM_Single_Skirmish",
            "#MM_Single_ChangeProfile",
            "#MM_Single_Back"
        };

        

private static void CreatePseudoCombo(RectTransform parent, int x, int y, int w, int h, string text, string actionName, IUiActionSink sink)
{
    var go = new GameObject("ArcadeCombo", typeof(RectTransform), typeof(Image), typeof(Button));
    go.transform.SetParent(parent, false);
    var rt = (RectTransform)go.transform;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(x, -y);
    rt.sizeDelta = new Vector2(w, h);

    var bg = go.GetComponent<Image>();
    bg.sprite = LoadResSprite("Interf3_elements_combo_frames/frame_0004");
    bg.type = bg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
    bg.color = Color.white;
    bg.raycastTarget = true;

    var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
    arrowGo.transform.SetParent(go.transform, false);
    var art = (RectTransform)arrowGo.transform;
    art.anchorMin = new Vector2(0, 0);
    art.anchorMax = new Vector2(0, 1);
    art.pivot = new Vector2(0, 0.5f);
    art.anchoredPosition = new Vector2(0, 0);
    art.sizeDelta = new Vector2(24, h);
    var aimg = arrowGo.GetComponent<Image>();
    aimg.sprite = LoadResSprite("Interf3_elements_combo_frames/frame_0000");
    aimg.preserveAspect = false;

    var labelGo = new GameObject("Label", typeof(RectTransform));
    labelGo.transform.SetParent(go.transform, false);
    var lrt = (RectTransform)labelGo.transform;
    lrt.anchorMin = Vector2.zero;
    lrt.anchorMax = Vector2.one;
    lrt.offsetMin = new Vector2(28, 0);
    lrt.offsetMax = new Vector2(-8, 0);

    var label = labelGo.AddComponent<TextMeshProUGUI>();
    label.font = Resources.Load<TMP_FontAsset>("Fonts/Slovic");
    label.fontSize = 16f;
    label.text = text ?? "";
    label.color = new Color32(25, 18, 10, 255);
    label.alignment = TextAlignmentOptions.MidlineLeft;
    label.enableWordWrapping = false;
    label.raycastTarget = false;

    var btn = go.GetComponent<Button>();
    btn.onClick.AddListener(() => sink?.OnAction(text, new UiAction { Name = actionName, Payload = "" }));
}

private static void CreateDecorLine(RectTransform parent, int x, int y, int w, int h)
{
    var sp = LoadResSprite("Interf3_elements_combo_frames/frame_0006");
    if (sp == null)
        return;

    var go = new GameObject("DecorLine", typeof(RectTransform), typeof(Image));
    go.transform.SetParent(parent, false);
    var rt = (RectTransform)go.transform;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(x, -y);
    rt.sizeDelta = new Vector2(w, h);
    var img = go.GetComponent<Image>();
    img.sprite = sp;
    img.type = Image.Type.Sliced;
    img.color = Color.white;
}

private static Sprite LoadResSprite(string key)
{
    var sp = Resources.Load<Sprite>(key);
    if (sp != null) return sp;
    var tex = Resources.Load<Texture2D>(key);
    if (tex == null) return null;
    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
}

private static void CreateBattleBottomButton(RectTransform parent, UiTextButton btn, RenderOptions opt, IUiActionSink sink, LocDb loc)
{
    string text = btn.MessageKey != null && btn.MessageKey.StartsWith("#", StringComparison.Ordinal)
        ? (loc?.Resolve(btn.MessageKey) ?? btn.MessageKey)
        : btn.MessageKey;

    var go = new GameObject($"BattleBtn_{SafeName(text)}", typeof(RectTransform), typeof(Image), typeof(Button));
    go.transform.SetParent(parent, false);

    var rt = (RectTransform)go.transform;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(btn.X, -btn.Y);
    rt.sizeDelta = new Vector2(btn.Width, btn.Height);

    var img = go.GetComponent<Image>();
    img.color = new Color(1, 1, 1, 0f);
    img.raycastTarget = btn.Enabled;

    var labelGo = new GameObject("Label", typeof(RectTransform));
    labelGo.transform.SetParent(go.transform, false);
    var lrt = (RectTransform)labelGo.transform;
    lrt.anchorMin = Vector2.zero;
    lrt.anchorMax = Vector2.one;
    lrt.offsetMin = Vector2.zero;
    lrt.offsetMax = Vector2.zero;

    var label = labelGo.AddComponent<TextMeshProUGUI>();
    label.text = text;
    label.font = Resources.Load<TMP_FontAsset>("Fonts/Slovic");
    label.fontSize = 24f;
    label.color = btn.Enabled ? new Color32(255, 238, 190, 255) : new Color32(120, 120, 120, 255);
    label.alignment = TextAlignmentOptions.Center;
    label.raycastTarget = false;

    if (btn.Enabled && btn.Actions != null && btn.Actions.Count > 0)
    {
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            foreach (var a in btn.Actions)
            {
                try { sink?.OnAction(text, a); }
                catch (Exception e) { Debug.LogError($"BattleButton action error: {e}"); }
            }
        });

        var hover = go.AddComponent<TmpHoverStyle>();
        hover.Target = label;
        hover.Interactable = true;
        hover.Normal = label.color;
        hover.Hover = new Color32(255, 255, 255, 255);
        hover.Disabled = new Color32(120, 120, 120, 255);
        hover.Id = text;
    }
}

private sealed class BattleEntry
        {
            public string Id = "";
            public string DisplayName = "";
            public string Description = "";
            public string PreviewPath = "";
            public bool IsBattle;
        }

        public override void Render(UiDesk desk, CoreFileSystem fs, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            RenderCounter++;
            Log(opt, $"[MainMenuRenderer] Render #{RenderCounter} source='{desk?.SourcePath}' children={desk?.Children?.Count ?? 0}");

            var root = CreateCanvas("C2_MainMenuCanvas", opt);
            if (desk?.Children == null) return;

            bool isSingleMenu = !string.IsNullOrEmpty(desk.SourcePath) &&
                                desk.SourcePath.IndexOf("M_Single", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isBattlesMenu = !string.IsNullOrEmpty(desk.SourcePath) &&
                                 desk.SourcePath.IndexOf("M_Battles", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isBattlesMenu)
            {
                RenderSingleBattles(root, fs, opt, sink, loc);
                return;
            }

            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiBitPicture pic)
                    CreateBitPicture(root, pic, fs, opt);
            }

            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiListDesk ld)
                    CreateListDesk(ld, root, opt);
            }

            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is not UiTextButton btn) continue;

                if (ShouldSkipTextButton(btn, isSingleMenu))
                    continue;

                var renderBtn = btn;
                if (isSingleMenu && IsCurrentProfileValue(btn))
                {
                    renderBtn = CloneTextButton(btn);
                    renderBtn.MessageKey = string.IsNullOrWhiteSpace(MenuActionSink.CurrentProfileName)
                        ? btn.MessageKey
                        : MenuActionSink.CurrentProfileName;
                }

                CreateTextButton(root, renderBtn, opt, sink, loc, MenuOverrideDb.Resolve);
            }

            if (!isSingleMenu) return;

            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is not UiVitButton vb) continue;
                if (string.IsNullOrWhiteSpace(vb.MessageKey)) continue;
                if (!SingleMenuVitKeys.Contains(vb.MessageKey)) continue;

                var btn = new UiTextButton
                {
                    Name = vb.Name,
                    X = vb.X,
                    Y = vb.Y,
                    Width = vb.Width,
                    Height = vb.Height,
                    Visible = vb.Visible,
                    Enabled = vb.Enabled,
                    MessageKey = vb.MessageKey,
                    HintKey = vb.HintKey,
                    PassiveFont = "fonBlow_S",
                    ActiveFont = "fonBlow_A",
                    DisabledFont = "fonBlow_S",
                    Align = "Center",
                    Style = UiTextStyle.Default
                };

                foreach (var a in vb.Actions)
                    btn.Actions.Add(a);

                if (vb.MessageKey.Equals("#EW2_Campaign", StringComparison.OrdinalIgnoreCase) ||
                    vb.MessageKey.Equals("#EW2_Battle4Europe", StringComparison.OrdinalIgnoreCase))
                {
                    btn.Actions.Clear();
                }

                CreateTextButton(root, btn, opt, sink, loc, MenuOverrideDb.Resolve);
            }
        }

        

private void RenderSingleBattles(RectTransform root, CoreFileSystem fs, RenderOptions opt, IUiActionSink sink, LocDb loc)
{
    string[] bgCandidates =
    {
        @"Interf3\background\single_battles.jpg",
        @"Interf3\background\single_battles_2.jpg",
        @"Interf3\background\single_scenario.jpg",
        @"Interf3\background\main_menu.jpg"
    };
    string bg = bgCandidates.FirstOrDefault(fs.Exists);
    if (!string.IsNullOrWhiteSpace(bg))
    {
        CreateBitPicture(root, new UiBitPicture
        {
            FileName = bg, X = 0, Y = 0, Width = 1024, Height = 768, Visible = true
        }, fs, opt);
    }

    CreateStaticText(root, 540, 28, 430, 34, "СРАЖЕНИЯ И БАТАЛИИ", 26,
        new Color32(255, 250, 230, 255), TextAlignmentOptions.Center);

    CreateTextButton(root, new UiTextButton
    {
        X = 168, Y = 117, Width = 230, Height = 18,
        MessageKey = "ИГРОВАЯ КОМНАТА",
        PassiveFont = "MenuTitleWhite",
        ActiveFont = "MenuTitleWhite",
        DisabledFont = "MenuTitleWhite",
        Align = "Center",
        Enabled = false
    }, opt, sink, loc, MenuOverrideDb.Resolve);

    CreateTextButton(root, new UiTextButton
    {
        X = 670, Y = 117, Width = 181, Height = 18,
        MessageKey = "#MAP_DESCRIPTION",
        PassiveFont = "MenuTitleWhite",
        ActiveFont = "MenuTitleWhite",
        DisabledFont = "MenuTitleWhite",
        Align = "Center",
        Enabled = false
    }, opt, sink, loc, MenuOverrideDb.Resolve);

    CreateListDesk(new UiListDesk { X = 539, Y = 160, Width = 428, Height = 518, Border = "BD", Visible = true }, root, opt);
    CreateListDesk(new UiListDesk { X = 573, Y = 160, Width = 375, Height = 235, Border = "BD", Visible = true }, root, opt);
    CreateListDesk(new UiListDesk { X = 569, Y = 463, Width = 366, Height = 214, Border = "BD", Visible = true }, root, opt);

    CreateListDesk(new UiListDesk { X = 40, Y = 160, Width = 470, Height = 518, Border = "BD", Visible = true }, root, opt);

    List<BattleEntry> entries = LoadBattleEntries(fs, loc, MenuActionSink.SingleBattlesShowBattles);
    if (entries.Count == 0)
    {
        CreateStaticText(root, 88, 210, 330, 30, "Нет карт.", 28, new Color32(20, 20, 20, 255), TextAlignmentOptions.Left);
    }
    else
    {
        if (string.IsNullOrWhiteSpace(MenuActionSink.SingleBattlesSelectedId) ||
            !entries.Any(e => e.Id.Equals(MenuActionSink.SingleBattlesSelectedId, StringComparison.OrdinalIgnoreCase)))
        {
            MenuActionSink.SingleBattlesSelectedId = entries[0].Id;
        }

        var selected = entries.FirstOrDefault(e => e.Id.Equals(MenuActionSink.SingleBattlesSelectedId, StringComparison.OrdinalIgnoreCase)) ?? entries[0];

        var tabSk = BuildSimpleTextButton(83, 180, 108, 24, "Сражения", true,
            "cva_Battles_Mode_Skirmish", "", isSelected: !MenuActionSink.SingleBattlesShowBattles);
        var tabBa = BuildSimpleTextButton(194, 180, 108, 24, "Баталии", true,
            "cva_Battles_Mode_Battles", "", isSelected: MenuActionSink.SingleBattlesShowBattles);
        CreateSimpleTextButton(root, tabSk, opt, sink, loc);
        CreateSimpleTextButton(root, tabBa, opt, sink, loc);

        float y = 468f;
        foreach (var entry in entries)
        {
            bool isSel = entry.Id.Equals(selected.Id, StringComparison.OrdinalIgnoreCase);
            var item = BuildSimpleTextButton(66, (int)y, 375, 20, entry.DisplayName, true,
                "cva_Battles_Select", entry.Id, isSel, align: "Left");
            CreateSimpleTextButton(root, item, opt, sink, loc);
            y += 24f;
            if (y > 640f) break;
        }

        CreateDraggablePreview(root, fs, selected.PreviewPath, 583, 176, 344, 180);

        CreateDecorLine(root, 700, 401, 120, 6);

        string arcadeLabel = loc?.Resolve("#MO_ArcadeMode") ?? "Аркадный режим";
        if (string.Equals(arcadeLabel, "#MO_ArcadeMode", StringComparison.OrdinalIgnoreCase))
            arcadeLabel = "Аркадный режим";
        CreateStaticText(root, 571, 436, 160, 21, arcadeLabel, 17, new Color32(25, 18, 10, 255), TextAlignmentOptions.Left);
        CreatePseudoCombo(root, 726, 434, 226, 21,
            MenuActionSink.SingleBattlesArcadeModeEnabled ? "Включен" : "Выключен",
            "cva_Battles_ArcadeToggle", sink);

        string descText = string.IsNullOrWhiteSpace(selected.Description) ? selected.DisplayName : selected.Description;
        CreateDescriptionScrollArea(root, 569, 463, 366, 214, descText);
    }

    var start = BuildSimpleTextButton(280, 708, 225, 43, "#SingleBattle_Start", false, "", "", false);
    CreateBattleBottomButton(root, start, opt, sink, loc);

    var back = BuildSimpleTextButton(523, 708, 225, 43, "CHAT_IG_BACK_BUTTON_TEXT", true, "cva_Battles_Back", "", false);
    CreateBattleBottomButton(root, back, opt, sink, loc);
}




private static void CreateDescriptionScrollArea(RectTransform parent, int x, int y, int w, int h, string text)
{
    // XML original:
    // outer desk 366x214 at (569,463)
    // inner text starts at (11,6) with size 340x167, MaxWidth=350
    const float innerX = 11f;
    const float innerY = 6f;
    const float innerW = 340f;
    const float innerH = 167f;
    const float scrollbarW = 14f;

    var root = new GameObject("BattleDescScroll", typeof(RectTransform));
    root.transform.SetParent(parent, false);

    var rt = (RectTransform)root.transform;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(x, -y);
    rt.sizeDelta = new Vector2(w, h);

    // Viewport matches the original inner text zone.
    var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
    viewport.transform.SetParent(root.transform, false);
    var vrt = (RectTransform)viewport.transform;
    vrt.anchorMin = vrt.anchorMax = new Vector2(0, 1);
    vrt.pivot = new Vector2(0, 1);
    vrt.anchoredPosition = new Vector2(innerX, -innerY);
    vrt.sizeDelta = new Vector2(innerW, innerH);

    var vimg = viewport.GetComponent<Image>();
    vimg.color = new Color(1f, 1f, 1f, 0.001f);
    vimg.raycastTarget = false;
    viewport.GetComponent<Mask>().showMaskGraphic = false;

    var content = new GameObject("Content", typeof(RectTransform));
    content.transform.SetParent(viewport.transform, false);
    var crt = (RectTransform)content.transform;
    crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
    crt.pivot = new Vector2(0, 1);
    crt.anchoredPosition = Vector2.zero;
    crt.sizeDelta = new Vector2(innerW, innerH);

    var textGO = new GameObject("Text", typeof(RectTransform));
    textGO.transform.SetParent(content.transform, false);
    var trt = (RectTransform)textGO.transform;
    trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
    trt.pivot = new Vector2(0, 1);
    trt.anchoredPosition = Vector2.zero;
    trt.sizeDelta = new Vector2(innerW, innerH);

    var label = textGO.AddComponent<TextMeshProUGUI>();
    label.text = text ?? "";
    label.font = Resources.Load<TMP_FontAsset>("Fonts/Slovic");
    label.fontSize = 15f;
    label.color = new Color32(25, 18, 10, 255);
    label.alignment = TextAlignmentOptions.TopLeft;
    label.textWrappingMode = TextWrappingModes.Normal;
    label.richText = false;
    label.raycastTarget = false;
    label.overflowMode = TextOverflowModes.Overflow;
    label.margin = new Vector4(0, 0, 0, 0);

    Canvas.ForceUpdateCanvases();
    float preferred = label.GetPreferredValues(text ?? "", innerW, 10000).y;
    float contentH = Mathf.Max(innerH, preferred + 4f);
    trt.sizeDelta = new Vector2(innerW, contentH);
    crt.sizeDelta = new Vector2(innerW, contentH);

    var scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
    scrollbarGO.transform.SetParent(root.transform, false);
    var srt = (RectTransform)scrollbarGO.transform;
    srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
    srt.pivot = new Vector2(0, 1);
    // Visually keep the bar inside the outer desk on the right like the original.
    srt.anchoredPosition = new Vector2(innerX + innerW, -innerY);
    srt.sizeDelta = new Vector2(scrollbarW, innerH);

    var sbBg = scrollbarGO.GetComponent<Image>();
    sbBg.color = new Color(1f, 1f, 1f, 0.001f);
    sbBg.raycastTarget = false;

    var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
    scrollbar.direction = Scrollbar.Direction.BottomToTop;

    var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
    slidingArea.transform.SetParent(scrollbarGO.transform, false);
    var sart = (RectTransform)slidingArea.transform;
    sart.anchorMin = Vector2.zero;
    sart.anchorMax = Vector2.one;
    sart.offsetMin = new Vector2(2, 2);
    sart.offsetMax = new Vector2(-2, -2);

    var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
    handle.transform.SetParent(slidingArea.transform, false);
    var hrt = (RectTransform)handle.transform;
    hrt.anchorMin = Vector2.zero;
    hrt.anchorMax = Vector2.one;
    hrt.offsetMin = Vector2.zero;
    hrt.offsetMax = Vector2.zero;

    var hImg = handle.GetComponent<Image>();
    hImg.color = new Color32(190, 150, 130, 220);

    scrollbar.handleRect = hrt;
    scrollbar.targetGraphic = hImg;

    var scrollRect = root.AddComponent<ScrollRect>();
    scrollRect.viewport = vrt;
    scrollRect.content = crt;
    scrollRect.horizontal = false;
    scrollRect.vertical = true;
    scrollRect.scrollSensitivity = 18f;
    scrollRect.verticalScrollbar = scrollbar;
    scrollRect.verticalScrollbarVisibility = contentH > innerH
        ? ScrollRect.ScrollbarVisibility.Permanent
        : ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    scrollRect.movementType = ScrollRect.MovementType.Clamped;
}

private static void CreateDraggablePreview(RectTransform parent, CoreFileSystem fs, string previewPath, int x, int y, int w, int h)
{
    var root = new GameObject("BattlePreview", typeof(RectTransform), typeof(Image));
    root.transform.SetParent(parent, false);
    var rt = (RectTransform)root.transform;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(x, -y);
    rt.sizeDelta = new Vector2(w, h);
    var bg = root.GetComponent<Image>();
    bg.color = new Color(1f, 1f, 1f, 0f);
    bg.raycastTarget = false;

    var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
    viewport.transform.SetParent(root.transform, false);
    var vrt = (RectTransform)viewport.transform;
    vrt.anchorMin = Vector2.zero;
    vrt.anchorMax = Vector2.one;
    vrt.offsetMin = Vector2.zero;
    vrt.offsetMax = Vector2.zero;
    var vimg = viewport.GetComponent<Image>();
    vimg.color = new Color(1f,1f,1f,0.01f);
    vimg.raycastTarget = false;
    viewport.GetComponent<Mask>().showMaskGraphic = false;

    var content = new GameObject("Content", typeof(RectTransform));
    content.transform.SetParent(viewport.transform, false);
    var crt = (RectTransform)content.transform;
    crt.anchorMin = new Vector2(0, 1);
    crt.anchorMax = new Vector2(0, 1);
    crt.pivot = new Vector2(0, 1);
    crt.anchoredPosition = Vector2.zero;

    var rawGO = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
    rawGO.transform.SetParent(content.transform, false);
    var irt = (RectTransform)rawGO.transform;
    irt.anchorMin = new Vector2(0, 1);
    irt.anchorMax = new Vector2(0, 1);
    irt.pivot = new Vector2(0, 1);

    Texture2D tex = null;
    try
    {
        if (!string.IsNullOrWhiteSpace(previewPath) && fs.Exists(previewPath))
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, fs.ReadAllBytes(previewPath));
        }
    }
    catch (Exception e)
    {
        Debug.LogError("Preview load failed: " + e);
    }

    if (tex != null)
    {
        irt.sizeDelta = new Vector2(tex.width, tex.height);
        crt.sizeDelta = new Vector2(tex.width, tex.height);
        var raw = rawGO.GetComponent<RawImage>();
        raw.texture = tex;
        raw.raycastTarget = false;

        float startX = tex.width < w ? (w - tex.width) * 0.5f : 0f;
        float startY = tex.height < h ? -((h - tex.height) * 0.5f) : 0f;
        crt.anchoredPosition = new Vector2(startX, startY);
        viewport.AddComponent<PreviewDragHandler>().Init(crt, w, h);
    }
}

private sealed class PreviewDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform _content;
    private float _viewW;
    private float _viewH;

    public void Init(RectTransform content, float viewW, float viewH)
    {
        _content = content;
        _viewW = viewW;
        _viewH = viewH;
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (_content == null) return;
        _content.anchoredPosition += eventData.delta;
        Clamp();
    }

    private void Clamp()
    {
        var size = _content.sizeDelta;
        float minX = Mathf.Min(0f, _viewW - size.x);
        float maxX = size.x < _viewW ? (_viewW - size.x) * 0.5f : 0f;
        float minY = Mathf.Min(0f, _viewH - size.y);
        float maxY = size.y < _viewH ? -((_viewH - size.y) * 0.5f) : 0f;
        var p = _content.anchoredPosition;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        _content.anchoredPosition = p;
    }
}

private static UiTextButton BuildSimpleTextButton(int x, int y, int w, int h, string message, bool enabled,
            string actionName, string payload, bool isSelected = false, string align = "Center")
        {
            var btn = new UiTextButton
            {
                X = x,
                Y = y,
                Width = w,
                Height = h,
                MessageKey = message,
                PassiveFont = "BlackFont",
                ActiveFont = "RedFont",
                DisabledFont = "BlackFont",
                Align = align,
                Enabled = enabled,
                Visible = true,
                Style = UiTextStyle.Default
            };

            if (isSelected)
            {
                btn.PassiveFont = "RedFont";
                btn.ActiveFont = "RedFont";
            }

            if (!string.IsNullOrWhiteSpace(actionName))
            {
                btn.Actions.Add(new UiAction { Name = actionName, Payload = payload ?? "" });
            }
            return btn;
        }

        private static List<BattleEntry> LoadBattleEntries(CoreFileSystem fs, LocDb loc, bool battles)
        {
            var list = new List<BattleEntry>();
            if (battles)
            {
                string[] ids =
                {
                    "Battle1","Battle2","Battle3","Battle4",
                    "HBattleAspern","HbattleAusterlitz","Hbattleegp",
                    "HbattleEilau","HbattleUlm","HbattleVaagram"
                };

                foreach (string id in ids)
                {
                    string descPath = $@"Missions\Battles\{id}.txt";
                    string previewPath = $@"Interf3\maps\battles\{id}.jpg";
                    if (!fs.Exists(descPath)) continue;

                    string raw = ReadCp1251(fs, descPath);
                    string display = ExtractBattleTitle(raw);
                    if (string.IsNullOrWhiteSpace(display)) display = id;

                    list.Add(new BattleEntry
                    {
                        Id = id,
                        DisplayName = display,
                        Description = CleanupMissionText(raw, keepTitle:true),
                        PreviewPath = previewPath,
                        IsBattle = true
                    });
                }
            }
            else
            {
                for (int i = 1; i <= 10; i++)
                {
                    string id = $"Skirmish{i}";
                    string descPath = $@"Missions\Skirmish\{id}.txt";
                    string previewPath = $@"Interf3\maps\skirmish\{id.ToLowerInvariant()}.jpg";
                    string key = $"#Skirmish{i}_TXT";
                    string display = loc?.Resolve(key) ?? key;
                    if (string.Equals(display, key, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(display))
                    {
                        display = id;
                    }

                    string raw = fs.Exists(descPath) ? ReadCp1251(fs, descPath) : "";

                    list.Add(new BattleEntry
                    {
                        Id = id,
                        DisplayName = display,
                        Description = CleanupMissionText(raw, keepTitle:false),
                        PreviewPath = previewPath,
                        IsBattle = false
                    });
                }
            }

            return list;
        }

        private static string ReadCp1251(CoreFileSystem fs, string relPath)
        {
            try
            {
                return fs.ReadAllText(relPath, System.Text.Encoding.GetEncoding(1251));
            }
            catch
            {
                try { return fs.ReadAllText(relPath); } catch { return ""; }
            }
        }

        private static string ExtractBattleTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string s = raw;
            int slash = s.IndexOf('\\');
            if (slash >= 0) s = s.Substring(0, slash);
            s = Regex.Replace(s, @"\{[^}]*\}", "");
            return s.Trim();
        }

        private static string CleanupMissionText(string raw, bool keepTitle)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string s = raw.Replace("\\\r\n", "\n").Replace("\\\n", "\n").Replace("\\", "\n");
            s = Regex.Replace(s, @"\{[^}]*\}", "");
            s = s.Replace("\r", "");
            var lines = s.Split('\n')
                         .Select(l => l.Trim())
                         .Where(l => !string.IsNullOrWhiteSpace(l))
                         .ToList();
            if (!keepTitle && lines.Count > 0 && !lines[0].Contains(":"))
            {
                lines.RemoveAt(0);
            }
            return string.Join("\n\n", lines);
        }

        private static void CreateStaticText(RectTransform parent, int x, int y, int w, int h, string text, float fontSize, Color32 color, TextAlignmentOptions align)
        {
            var go = new GameObject("StaticText", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text ?? "";
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = align;
            label.enableWordWrapping = true;
            label.richText = false;
            label.raycastTarget = false;
            label.font = Resources.Load<TMP_FontAsset>("Fonts/Slovic");
        }

        private static void CreateSimpleTextButton(RectTransform parent, UiTextButton btn, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            string text = btn.MessageKey != null && btn.MessageKey.StartsWith("#", StringComparison.Ordinal)
                ? (loc?.Resolve(btn.MessageKey) ?? btn.MessageKey)
                : btn.MessageKey;

            var go = new GameObject($"SimpleBtn_{SafeName(text)}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(btn.X, -btn.Y);
            rt.sizeDelta = new Vector2(btn.Width, btn.Height);

            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0f);
            img.raycastTarget = btn.Enabled;

            var button = go.GetComponent<Button>();
            button.interactable = btn.Enabled;
            button.targetGraphic = img;

            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var label = textGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = Resources.Load<TMP_FontAsset>("Fonts/Slovic");
            label.fontSize = 28f;
            label.color = btn.PassiveFont.Equals("RedFont", StringComparison.OrdinalIgnoreCase)
                ? new Color32(136, 18, 3, 255)
                : new Color32(20, 18, 10, 255);
            label.alignment = btn.Align.Equals("Left", StringComparison.OrdinalIgnoreCase) ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            label.margin = btn.Align.Equals("Left", StringComparison.OrdinalIgnoreCase) ? new Vector4(10, 0, 0, 0) : Vector4.zero;
            label.raycastTarget = false;

            if (btn.Enabled && btn.Actions != null && btn.Actions.Count > 0)
            {
                button.onClick.AddListener(() =>
                {
                    foreach (var a in btn.Actions)
                    {
                        try { sink?.OnAction(text, a); }
                        catch (Exception e) { Debug.LogError($"SimpleTextButton action error: {e}"); }
                    }
                });

                var hover = go.AddComponent<TmpHoverStyle>();
                hover.Target = label;
                hover.Interactable = true;
                hover.Normal = label.color;
                hover.Hover = new Color32(136, 18, 3, 255);
                hover.Disabled = new Color32(90, 90, 90, 255);
                hover.Id = text;
            }
        }

        private static bool ShouldSkipTextButton(UiTextButton btn, bool isSingleMenu)
        {
            if (btn == null) return true;
            if (HiddenTextKeys.Contains(btn.MessageKey ?? string.Empty)) return true;

            if (!isSingleMenu) return false;

            if (IsCurrentProfileValue(btn)) return false;

            return !(btn.MessageKey.Equals("#MM_Single_Window", StringComparison.OrdinalIgnoreCase) ||
                     btn.MessageKey.Equals("#CUR_PROFILE:", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCurrentProfileValue(UiTextButton btn)
        {
            if (btn == null) return false;
            if (!string.IsNullOrWhiteSpace(btn.MessageKey) && btn.MessageKey.StartsWith("#", StringComparison.Ordinal)) return false;
            return btn.X >= 450 && btn.X <= 560 && btn.Y >= 560 && btn.Y <= 590;
        }

        private static UiTextButton CloneTextButton(UiTextButton src)
        {
            var dst = new UiTextButton
            {
                Name = src.Name,
                Hint = src.Hint,
                X = src.X,
                Y = src.Y,
                Width = src.Width,
                Height = src.Height,
                Visible = src.Visible,
                Enabled = src.Enabled,
                MessageKey = src.MessageKey,
                HintKey = src.HintKey,
                PassiveFont = src.PassiveFont,
                ActiveFont = src.ActiveFont,
                DisabledFont = src.DisabledFont,
                Align = src.Align,
                Style = src.Style
            };

            foreach (var a in src.Actions)
                dst.Actions.Add(a);

            return dst;
        }
    }
}
