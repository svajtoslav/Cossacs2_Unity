
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TemnyLessCodec;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cossacks2Bridge.Core;

namespace Cossacks2Bridge.UnityAdapters.Battles
{
    /// <summary>
    /// Renders the "Сражения и Баталии" screen.
    /// All coordinates match the original C++ engine (SelectSingleBattle.h).
    /// Sprites are loaded at runtime from Cash via Melinoja (CodecFacade).
    /// </summary>
    internal sealed class MbattlesXmlRenderer
    {
        // ── sprite cache ──────────────────────────────────────────────────────
        private readonly Dictionary<string, Sprite> _cache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private Sprite _previewHintTriangleSprite;
        private Sprite _whiteUiSprite;
        private readonly Dictionary<string, Sprite> _lineOnlySpriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<int, int> _playerFlagColorBySlot =
            new Dictionary<int, int>();
        private readonly Dictionary<int, int> _playerRaceBySlot =
            new Dictionary<int, int>();
        private readonly Dictionary<int, int> _playerTeamBySlot =
            new Dictionary<int, int>();
        private readonly Dictionary<int, int> _playerDifficultyBySlot =
            new Dictionary<int, int>();
        private int _leftDifficultyIndex = 0;

        private int? _playerColorFrameCount;

        private static readonly string[] DebugRaceNames =
        {
            "Случайно",
            "Франция",
            "Австрия",
            "Россия",
            "Пруссия",
            "Англия",
            "Испания",
            "Турция"
        };

        private static readonly string[] DebugDifficultyNames =
        {
            "Легко",
            "Нормально",
            "Тяжело"
        };

        private static readonly Color32[] FallbackPlayerFlagTints =
        {
            new Color32(210, 60, 48, 255),
            new Color32(56, 96, 188, 255),
            new Color32(44, 150, 82, 255),
            new Color32(225, 190, 32, 255),
            new Color32(150, 72, 172, 255),
            new Color32(36, 192, 198, 255),
            new Color32(230, 230, 230, 255),
            new Color32(28, 28, 28, 255)
        };

        // ── manual tuning for the description scrollbar ─────────────────────
        // Move the WHOLE scrollbar gutter left/right or up/down without touching XML sizes.
        public static float DescScrollbarOffsetX = 17f;
        public static float DescScrollbarOffsetY = 0f;

        // Fine tuning for the moving thumb only.
        public static float DescScrollbarThumbHeightAdjust = 0f;
        public static float DescScrollbarThumbOffsetY = 0f;
        public static float BottomButtonsOffsetY = -5f;   // ← НОВОЕ: отрицательное = выше

        private sealed class PointerClickRelay : MonoBehaviour, IPointerClickHandler
        {
            public Action<PointerEventData.InputButton> Clicked;

            public void OnPointerClick(PointerEventData eventData)
            {
                Clicked?.Invoke(eventData.button);
            }
        }

        private sealed class MissionListRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public GameObject HoverVisual;
            public GameObject SelectedVisual;
            public TextMeshProUGUI Label;
            public Color32 PassiveColor;
            public Color32 HoverColor;
            public Color32 SelectedColor;
            public bool Selected;

            public void Refresh()
            {
                if (HoverVisual != null)
                    HoverVisual.SetActive(false);
                if (SelectedVisual != null)
                    SelectedVisual.SetActive(Selected);
                if (Label != null)
                    Label.color = Selected ? SelectedColor : PassiveColor;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Selected)
                {
                    if (SelectedVisual != null)
                        SelectedVisual.SetActive(true);
                }
                else if (HoverVisual != null)
                {
                    HoverVisual.SetActive(true);
                }

                if (Label != null)
                    Label.color = Selected ? SelectedColor : HoverColor;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (HoverVisual != null)
                    HoverVisual.SetActive(false);
                if (SelectedVisual != null)
                    SelectedVisual.SetActive(Selected);
                if (Label != null)
                    Label.color = Selected ? SelectedColor : PassiveColor;
            }
        }

        private sealed class OriginalBottomButtonState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Button Button;
            public GameObject PassiveVisual;
            public GameObject HoverVisual;
            public TextMeshProUGUI Label;
            public Color32 PassiveColor;
            public Color32 HoverColor;
            public Color32 DisabledColor;
            public bool Interactable = true;

            private bool _hovered;

            private void OnEnable()
            {
                ApplyState();
            }

            private void OnDisable()
            {
                _hovered = false;
                ApplyState();
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                _hovered = true;
                ApplyState();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                _hovered = false;
                ApplyState();
            }

            public void ApplyState()
            {
                bool showHover = Interactable && _hovered;
                if (PassiveVisual != null)
                    PassiveVisual.SetActive(!showHover);
                if (HoverVisual != null)
                    HoverVisual.SetActive(showHover);
                if (Label != null)
                    Label.color = !Interactable ? DisabledColor : (showHover ? HoverColor : PassiveColor);
                if (Button != null)
                    Button.interactable = Interactable;
            }
        }

        // ── public entry point ───────────────────────────────────────────────
        public void Render(
            MbScene scene,
            CoreFileSystem fs,
            Cossacks2Bridge.UnityAdapters.Renderers.BaseUiRenderer.RenderOptions opt,
            IUiActionSink sink,
            LocDb loc)
        {
            RectTransform root = CreateCanvas(opt);
            string cashDir = Path.Combine(fs.DataRoot, "Cash");

            // 1. BACKGROUND
            string bgPath = fs.ResolvePath(@"Interf3\background\single_battles.jpg");
            if (!File.Exists(bgPath)) bgPath = fs.ResolvePath(@"Interf3\background\single_battles_2.jpg");
            if (!File.Exists(bgPath)) bgPath = fs.ResolvePath(@"Interf3\background\single_scenario.jpg");
            if (File.Exists(bgPath)) PlaceBitmap(root, bgPath, 0, 0, 1024, 768);

            // 2. BD FILLS
            // Original logic: the screen does NOT tile BD paper across the whole left/right columns.
            // Keep only the description desk fill below; large column fills are intentionally omitted.

            // 3. LEFT PANEL (tabs + upper controls + mission list)
            bool showLoad = MenuActionSink.SingleBattlesShowLoad;
            bool showBattles = !showLoad && MenuActionSink.SingleBattlesShowBattles;
            List<BattleEntrySimple> entries = showLoad ? new List<BattleEntrySimple>() : LoadEntries(fs, loc, showBattles);
            string selectedId = showLoad ? string.Empty : (MenuActionSink.SingleBattlesSelectedId ?? "");
            if (!showLoad && entries.Count > 0 && !entries.Exists(e => string.Equals(e.Id, selectedId, StringComparison.OrdinalIgnoreCase)))
                selectedId = entries[0].Id;

            var sel = showLoad ? null : entries.Find(e => string.Equals(e.Id, selectedId, StringComparison.OrdinalIgnoreCase));
            int rosterSlots = Mathf.Clamp(sel != null ? GetMissionPlayerCount(fs, sel.Id) : 2, 1, 7);

            RenderLeftTabsFromScene(root, scene, loc, cashDir, sink, showBattles, showLoad);
            RenderLeftUpperFromScene(root, scene, cashDir, sink, showBattles, rosterSlots);
            RenderMissionListFromScene(root, scene, cashDir, entries, selectedId, sink, showBattles);

            // 5. MAP PREVIEW — original XML host is 375x235 at x=573,y=160.
            // The JPG itself is ActualSize=true and moves inside the viewport.
            if (sel != null)
            {
                var previewMeta = LoadPreviewMeta(fs, sel.Id, showBattles);
                string previewAbs = ResolvePreviewAbsolutePath(fs, previewMeta, sel.PreviewPath);
                Debug.Log($"[MBattles] sel={sel.Id} preview='{previewAbs}' exists={(!string.IsNullOrWhiteSpace(previewAbs) && File.Exists(previewAbs))} center=({previewMeta.CenterX},{previewMeta.CenterY}) ss={previewMeta.ScreenSaver.Count}");
                if (!string.IsNullOrWhiteSpace(previewAbs) && File.Exists(previewAbs))
                    PlaceMapPreview(root, scene, cashDir, previewAbs, previewMeta, 573, 160, 375, 235);
            }

            // 6. ARCADE MODE — exact XML-driven label + combo
            RenderArcadeFromScene(root, scene, loc, cashDir, sink);

            // 7. DESCRIPTION DESK (original XML-driven geometry)
            // Original description desk: x=569 y=463 w=366 h=214
            string descText = showLoad ? string.Empty : ResolveMissionDescriptionText(fs, showBattles, selectedId, sel);
            Debug.Log($"[MBattles] desc id='{selectedId}' len={(descText != null ? descText.Length : 0)}");
            DrawBDFill(root, cashDir, 569, 463, 366, 214, "DescFill");
            PlaceDescriptionScroll(root, cashDir, loc, 569, 463, 366, 214, descText);

            // 8. BD FRAME LINES on top of all content
            // Experiment: disable the two giant outer frames so we can verify whether
            // these are the thin white rectangles visible across almost the whole left/right areas.
            //DrawBDLines(root, cashDir, 40,  137, 468, 541, "LeftFrame");
            //DrawBDLines(root, cashDir, 539, 137, 471, 541, "RightFrame");
            DrawBDLines(root, cashDir, 569, 463, 366, 214, "DescFrame");

            // 9. TITLE TEXTS on top of everything
            string title    = loc?.Resolve("INTF_SBATL_T0") ?? "";
            string leftHdr  = loc?.Resolve("INTF_SBATL_T1") ?? "";
            string rightHdr = ResolveLocOrFallback(loc, "#MAP_DESCRIPTION", "ОПИСАНИЕ КАРТЫ");
            if (string.IsNullOrWhiteSpace(title)    || title.StartsWith("INTF_"))    title    = "СРАЖЕНИЯ И БАТАЛИИ";
            if (string.IsNullOrWhiteSpace(leftHdr)  || leftHdr.StartsWith("INTF_"))  leftHdr  = "ИГРОВАЯ КОМНАТА";
            // Original: title x=512,y=18; leftHdr x=303,y=107; right description title XML x=670,y=117,w=181,h=14, Align=Center
            PlaceText(root, title,    512, 18,  500, 40, 26, Color.white, TextAlignmentOptions.Center, true);
            PlaceText(root, leftHdr,  303, 107, 400, 22, 16, Color.white, TextAlignmentOptions.Center);
            PlaceText(root, rightHdr, 760.5f, 117f, 181f, 14f, 16f, Color.white, TextAlignmentOptions.Center);

            // 10. BOTTOM BUTTONS
            PlaceBottomButtonBar(root, cashDir);
            PlaceBottomButton(root, cashDir, "Начать", 280, 705 + BottomButtonsOffsetY, 225, 43, true, "cva_Battles_Start", "", sink);
            PlaceBottomButton(root, cashDir, "Вернуться", 523, 705 + BottomButtonsOffsetY, 225, 43, true, "cva_Battles_Back", "", sink);

            // populate list
            foreach (MbListNode list in scene.Nodes.OfType<MbListNode>())
                if (list.Name.Equals("S", StringComparison.OrdinalIgnoreCase))
                {
                    list.Items.Clear();
                    foreach (var e in entries) list.Items.Add(e.Id);
                }
        }

        private void RenderArcadeFromScene(RectTransform root, MbScene scene, LocDb loc, string cashDir, IUiActionSink sink)
        {
            var arcadeLabelNode = scene.Nodes.OfType<MbTextNode>()
                .FirstOrDefault(n => string.Equals(n.Role, "ArcadeLabel", StringComparison.OrdinalIgnoreCase));

            string arcadeLabel = loc?.Resolve("#MO_ArcadeMode") ?? "Аркадный режим";
            if (arcadeLabelNode != null)
            {
                string resolved = arcadeLabelNode.Message ?? arcadeLabel;
                if (!string.IsNullOrWhiteSpace(resolved) && loc != null)
                {
                    string locResolved = loc.Resolve(resolved);
                    if (!string.IsNullOrWhiteSpace(locResolved) && !locResolved.StartsWith("#"))
                        resolved = locResolved;
                }

                if (string.IsNullOrWhiteSpace(resolved) || resolved.StartsWith("#"))
                    resolved = "Аркадный режим";

                PlaceText(root, resolved,
                    arcadeLabelNode.X,
                    437,
                    arcadeLabelNode.Width,
                    arcadeLabelNode.Height,
                    15,
                    new Color32(25, 18, 10, 255),
                    TextAlignmentOptions.Left);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(arcadeLabel) || arcadeLabel.StartsWith("#"))
                    arcadeLabel = "Аркадный режим";
                PlaceText(root, arcadeLabel, 571, 437, 92, 12, 15, new Color32(25, 18, 10, 255), TextAlignmentOptions.Left);
            }

            var arcadeComboNode = scene.Nodes.OfType<MbComboBoxNode>()
                .FirstOrDefault(n => string.Equals(n.Role, "ArcadeCombo", StringComparison.OrdinalIgnoreCase));

            string displayText = MenuActionSink.SingleBattlesArcadeModeEnabled ? "Включен" : "Выключен";
            if (arcadeComboNode != null)
            {
                Debug.Log($"[MBattles] XML ArcadeCombo x={arcadeComboNode.X} y={arcadeComboNode.Y} w={arcadeComboNode.Width} h={arcadeComboNode.Height} gp='{arcadeComboNode.GP_File}' text='{displayText}'");
                PlacePseudoCombo(root, cashDir, arcadeComboNode.X, arcadeComboNode.Y, arcadeComboNode.Width, arcadeComboNode.Height,
                    displayText, "cva_Battles_ArcadeToggle", sink);
            }
            else
            {
                Debug.Log("[MBattles] XML ArcadeCombo not found, using fallback coords");
                PlacePseudoCombo(root, cashDir, 726, 434, 226, 21, displayText, "cva_Battles_ArcadeToggle", sink);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CANVAS
        // ─────────────────────────────────────────────────────────────────────
        private RectTransform CreateCanvas(Cossacks2Bridge.UnityAdapters.Renderers.BaseUiRenderer.RenderOptions opt)
        {
            foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null) continue;
                if (c.gameObject.name == "C2_MBattlesCanvas")
                {
                    foreach (Transform ch in c.transform) UnityEngine.Object.DestroyImmediate(ch.gameObject);
                    return c.GetComponent<RectTransform>();
                }
                if (c.gameObject.name.StartsWith("C2_"))
                    UnityEngine.Object.DestroyImmediate(c.gameObject);
            }

            var go = new GameObject("C2_MBattlesCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = opt.CanvasScaleMode;
            scaler.referenceResolution = opt.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            go.AddComponent<GraphicRaycaster>();

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SPRITE LOADING via Melinoja
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerable<string> EnumerateG16LookupNames(string g16Name)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    seen.Add(value);
            }

            Add(g16Name);

            string underscored = g16Name?
                .Replace("\\", "_")
                .Replace("/", "_");
            Add(underscored);

            string slashNormalized = g16Name?
                .Replace("\\", "/");
            Add(slashNormalized);

            if (!string.IsNullOrWhiteSpace(g16Name))
            {
                Add(Path.GetFileName(g16Name));
                Add(Path.GetFileNameWithoutExtension(g16Name));
            }

            if (!string.IsNullOrWhiteSpace(slashNormalized))
            {
                Add(Path.GetFileName(slashNormalized));
                Add(Path.GetFileNameWithoutExtension(slashNormalized));
            }

            if (!string.IsNullOrWhiteSpace(underscored))
            {
                Add(Path.GetFileName(underscored));
                Add(Path.GetFileNameWithoutExtension(underscored));
            }

            return seen;
        }

        private Sprite LoadG16Sprite(string cashDir, string g16Name, int frameIndex)
        {
            string key = $"{g16Name}|{frameIndex}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var lookupNames = EnumerateG16LookupNames(g16Name).ToList();
            Sprite sp = null;

            // Try Resources first (pre-extracted _frames folders)
            foreach (string lookupName in lookupNames)
            {
                string resFolder = lookupName.Replace("\\", "_").Replace("/", "_").ToUpperInvariant();
                string resKey = resFolder + "_frames/frame_" + frameIndex.ToString("0000");

                sp = Resources.Load<Sprite>(resKey);
                if (sp == null)
                {
                    var tex = Resources.Load<Texture2D>(resKey);
                    if (tex != null)
                        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 1), 1f);
                }

                if (sp != null)
                    break;
            }

            // Fallback: load from Cash / Resources/Interf3 via Melinoja
            if (sp == null)
            {
                var candidates = new List<string>();
                var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                void AddPath(string path)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return;
                    string full;
                    try { full = Path.GetFullPath(path); }
                    catch { full = path; }
                    if (seenPaths.Add(full))
                        candidates.Add(path);
                }

                foreach (string lookupName in lookupNames)
                {
                    AddPath(Path.Combine(cashDir, lookupName + ".g16"));
                    AddPath(Path.Combine(cashDir, lookupName.ToUpperInvariant() + ".g16"));
                    AddPath(Path.Combine(cashDir, lookupName.ToLowerInvariant() + ".g16"));
                }

                string interf3Dir = Path.Combine(Application.dataPath, "Resources", "Interf3");
                if (Directory.Exists(interf3Dir))
                {
                    foreach (var f in Directory.GetFiles(interf3Dir, "*.g16", SearchOption.TopDirectoryOnly))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(f);
                        foreach (string lookupName in lookupNames)
                        {
                            if (string.Equals(fileName, lookupName, StringComparison.OrdinalIgnoreCase))
                            {
                                AddPath(f);
                                break;
                            }
                        }
                    }
                }

                foreach (var path in candidates)
                {
                    if (!File.Exists(path)) continue;
                    if (!CodecFacade.LoadG16ToMemory(path, out var err))
                    {
                        Debug.LogWarning($"[MBattles] LoadG16 failed {path}: {err}");
                        continue;
                    }
                    if (!CodecFacade.TryGetG16FrameRGBA(path, frameIndex, out int w, out int h, out byte[] rgba, out var err2))
                    {
                        Debug.LogWarning($"[MBattles] GetFrame {frameIndex} failed {path}: {err2}");
                        continue;
                    }
                    var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    t.LoadRawTextureData(rgba);
                    t.Apply();
                    sp = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0, 1), 1f);
                    break;
                }
            }

            _cache[key] = sp;
            return sp;
        }

        private int GetPlayerColorFrameCount(string cashDir, string fileId)
        {
            if (_playerColorFrameCount.HasValue)
                return _playerColorFrameCount.Value;

            int count = 0;
            for (int frame = 1; frame <= 16; frame++)
            {
                if (LoadG16Sprite(cashDir, fileId, frame) != null)
                    count++;
                else if (count > 0)
                    break;
            }

            _playerColorFrameCount = count;
            return count;
        }

        private Color32 GetFallbackFlagTint(int colorIndex)
        {
            if (FallbackPlayerFlagTints.Length == 0)
                return Color.white;

            int idx = ((colorIndex % FallbackPlayerFlagTints.Length) + FallbackPlayerFlagTints.Length) %
                      FallbackPlayerFlagTints.Length;
            return FallbackPlayerFlagTints[idx];
        }

        private Sprite GetLineOnlyOverlaySprite(Sprite source, string cacheKey)
        {
            if (source == null)
                return null;

            if (_lineOnlySpriteCache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                Rect rect = source.rect;
                int w = Mathf.RoundToInt(rect.width);
                int h = Mathf.RoundToInt(rect.height);
                if (w <= 0 || h <= 0)
                    return source;

                Color32[] src = source.texture.GetPixels32();
                int texW = source.texture.width;
                int x0 = Mathf.RoundToInt(rect.x);
                int y0 = Mathf.RoundToInt(rect.y);

                byte[] lum = new byte[w * h];
                for (int y = 0; y < h; y++)
                {
                    int sy = y0 + y;
                    for (int x = 0; x < w; x++)
                    {
                        int sx = x0 + x;
                        Color32 c = src[sy * texW + sx];
                        lum[y * w + x] = (byte)((c.r * 299 + c.g * 587 + c.b * 114) / 1000);
                    }
                }

                var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var dst = new Color32[w * h];

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int idx = y * w + x;
                        Color32 c = src[(y0 + y) * texW + (x0 + x)];
                        int l = lum[idx];

                        int edge = 0;
                        if (x > 0) edge = Mathf.Max(edge, Mathf.Abs(l - lum[idx - 1]));
                        if (x + 1 < w) edge = Mathf.Max(edge, Mathf.Abs(l - lum[idx + 1]));
                        if (y > 0) edge = Mathf.Max(edge, Mathf.Abs(l - lum[idx - w]));
                        if (y + 1 < h) edge = Mathf.Max(edge, Mathf.Abs(l - lum[idx + w]));

                        int chroma = Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Abs(c.g - c.b), Mathf.Abs(c.r - c.b));

                        bool keep =
                            edge >= 5 ||          // faint grid / borders
                            l <= 210 ||           // darker ornaments / divider
                            chroma >= 10;         // colored decoration

                        if (!keep)
                        {
                            dst[idx] = new Color32(c.r, c.g, c.b, 0);
                            continue;
                        }

                        int a = Mathf.Max(edge * 20, (230 - l) * 4);
                        if (chroma >= 10)
                            a = Mathf.Max(a, 120);

                        a = Mathf.Clamp(a, 50, 255);
                        dst[idx] = new Color32(c.r, c.g, c.b, (byte)a);
                    }
                }

                outTex.SetPixels32(dst);
                outTex.Apply();
                var sprite = Sprite.Create(outTex, new Rect(0, 0, w, h), new Vector2(0, 1), 1f);
                _lineOnlySpriteCache[cacheKey] = sprite;
                return sprite;
            }
            catch
            {
                return null;
            }
        }

        private Sprite GetWhiteUiSprite()
        {
            if (_whiteUiSprite != null)
                return _whiteUiSprite;

            _whiteUiSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                1f);

            return _whiteUiSprite;
        }

        private void ApplyPlayerFlagVisual(Image img, string cashDir, string fileId, Sprite baseSprite, int colorIndex)
        {
            int frameCount = GetPlayerColorFrameCount(cashDir, fileId);
            if (frameCount > 0)
            {
                int wrapped = ((colorIndex % frameCount) + frameCount) % frameCount;
                Sprite colored = LoadG16Sprite(cashDir, fileId, wrapped + 1);
                if (colored != null)
                {
                    img.sprite = colored;
                    img.color = Color.white;
                    return;
                }
            }

            img.sprite = baseSprite;
            img.color = GetFallbackFlagTint(colorIndex);
        }

        // ─────────────────────────────────────────────────────────────────────
        // BD BORDER using pre-extracted frames (Assets/Resources)
        // ─────────────────────────────────────────────────────────────────────
        private void DrawBDFill(RectTransform parent, string cashDir, float x, float y, float w, float h, string name)
        {
            Sprite[] fills = {
                GetBDSprite(cashDir, 6),
                GetBDSprite(cashDir, 9),
                GetBDSprite(cashDir, 10),
                GetBDSprite(cashDir, 11)
            };

            // The previous 15px inset was correct for the old oversized fake desks,
            // but it is too aggressive for the real description desk (366x214):
            // it leaves a visible empty gap before the thin red inner border.
            // Keep the fill almost flush for DescFill only.
            float inset = string.Equals(name, "DescFill", StringComparison.OrdinalIgnoreCase) ? 3f : 15f;
            float fx = x + inset, fy = y + inset, fw = w - inset * 2f, fh = h - inset * 2f;
            if (fw <= 0 || fh <= 0) return;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(fx, -fy);
            rt.sizeDelta = new Vector2(fw, fh);
            TileFill(rt, fills, fw, fh);
        }

        private void DrawPaperFillExact(RectTransform parent, string cashDir, float x, float y, float w, float h, string name)
        {
            Sprite[] fills = {
                GetBDSprite(cashDir, 6),
                GetBDSprite(cashDir, 9),
                GetBDSprite(cashDir, 10),
                GetBDSprite(cashDir, 11)
            };

            if (w <= 0 || h <= 0)
                return;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            TileFill(rt, fills, w, h);
        }

        private void DrawBDLines(RectTransform parent, string cashDir, float x, float y, float w, float h, string pfx)
        {
            Sprite spTL  = GetBDSprite(cashDir, 2);
            Sprite spTR  = GetBDSprite(cashDir, 3);
            Sprite spBL  = GetBDSprite(cashDir, 0);
            Sprite spBR  = GetBDSprite(cashDir, 1);
            Sprite spTop = GetBDSprite(cashDir, 5);
            Sprite spBot = GetBDSprite(cashDir, 4);
            Sprite spL   = GetBDSprite(cashDir, 7);
            Sprite spR   = GetBDSprite(cashDir, 8);

            PlaceSprTiled(parent, spTop, x + 16, y - 15, w - 32, 32, true);
            PlaceSpr(parent, spTL, x - 15,     y - 15,     32, 32, pfx + "_TL");
            PlaceSpr(parent, spTR, x + w - 16, y - 15,     32, 32, pfx + "_TR");

            PlaceSprTiled(parent, spBot, x + 16, y + h - 16, w - 32, 32, true);
            PlaceSprTiled(parent, spL,   x - 15, y + 16, 32, h - 32, false);
            PlaceSprTiled(parent, spR,   x + w - 16, y + 16, 32, h - 32, false);
            PlaceSpr(parent, spBL, x - 15,     y + h - 16, 32, 32, pfx + "_BL");
            PlaceSpr(parent, spBR, x + w - 16, y + h - 16, 32, 32, pfx + "_BR");
        }

        private void DrawBDFrame(RectTransform parent, string cashDir, float x, float y, float w, float h)
        {
            DrawBDFill(parent, cashDir, x, y, w, h, "Fill");
            DrawBDLines(parent, cashDir, x, y, w, h, "Frame");
        }

        private Sprite GetBDSprite(string cashDir, int idx)
        {
            // Try pre-extracted Resources first
            string resKey = $"interf3_elements_border_BD_frames/frame_{idx:0000}";
            var sp = Resources.Load<Sprite>(resKey);
            if (sp == null)
            {
                var tex = Resources.Load<Texture2D>(resKey);
                if (tex != null)
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 1), 1f);
            }
            if (sp == null)
                sp = LoadG16Sprite(cashDir, "interf3_elements_border_BD", idx);
            return sp;
        }

        private void TileFill(RectTransform parent, Sprite[] fills, float w, float h)
        {
            Sprite fill = null;
            foreach (var s in fills) { if (s != null) { fill = s; break; } }
            if (fill == null) { FallbackFill(parent, w, h); return; }

            int tw = Mathf.Max(1, (int)fill.rect.width);
            int th = Mathf.Max(1, (int)fill.rect.height);
            int nx = Mathf.CeilToInt(w / tw);
            int ny = Mathf.CeilToInt(h / th);
            int fi = 0;
            for (int iy = 0; iy < ny; iy++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    var sp = fills[fi % fills.Length] ?? fill;
                    float px = ix * tw, py = iy * th;
                    float pw = Mathf.Min(tw, w - px), ph = Mathf.Min(th, h - py);
                    PlaceSpr(parent, sp, px, py, pw, ph, $"F{fi}");
                    fi++;
                }
            }
        }

        private void FallbackFill(RectTransform parent, float w, float h)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            go.GetComponent<Image>().color = new Color32(232, 222, 200, 240);
        }


        // ─────────────────────────────────────────────────────────────────────
        // LEFT PANEL / MISSION LIST
        // ─────────────────────────────────────────────────────────────────────
        private void RenderLeftTabsFromScene(RectTransform parent,
            MbScene scene,
            LocDb loc,
            string cashDir,
            IUiActionSink sink,
            bool showBattles,
            bool showLoad)
        {
            // Original XML contains extra GPPicture strips from Interf3\elements\tab
            // under and around the three top tab buttons. Render them first.
            var tabDecor = scene.Nodes.OfType<MbGpPictureNode>()
                .Where(n => n.Visible &&
                            n.X < 500 &&
                            n.Y < 250 &&
                            n.FileID.IndexOf(@"Interf3\elements\tab", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(n => n.Y)
                .ThenBy(n => n.X)
                .ThenBy(n => n.SpriteID)
                .ToList();

            foreach (var gp in tabDecor)
            {
                var sp = LoadG16Sprite(cashDir, gp.FileID, gp.SpriteID);
                if (sp != null)
                {
                    TryForcePointFiltering(sp);
                    PlaceSpr(parent, sp, gp.X, gp.Y, gp.Width, gp.Height, $"TopTabDecor_{gp.SpriteID}_{gp.X}_{gp.Y}");
                    PlaceSpr(parent, sp, gp.X, gp.Y, gp.Width, gp.Height, $"TopTabDecor_{gp.SpriteID}_{gp.X}_{gp.Y}_Overlay");
                }
            }

            var tabs = scene.Nodes.OfType<MbGpTextButtonNode>()
                .Where(n => n.Visible &&
                            n.Role.Equals("TabButton", StringComparison.OrdinalIgnoreCase) &&
                            n.X < 500)
                .OrderBy(n => n.X)
                .ThenBy(n => n.Y)
                .ToList();

            foreach (var tab in tabs)
            {
                string actionName = tab.Actions
                    .Select(a => a.Name ?? "")
                    .FirstOrDefault(a =>
                        string.Equals(a, "cva_Battles_Mode_Skirmish", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a, "cva_Battles_Mode_Battles", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a, "cva_Battles_Mode_Load", StringComparison.OrdinalIgnoreCase))
                    ?? "";

                string label = ResolveLocOrFallback(loc, tab.Message, tab.Message);
                if (string.IsNullOrWhiteSpace(label) || label.StartsWith("#"))
                {
                    if ((tab.Message ?? "").IndexOf("Skirmish", StringComparison.OrdinalIgnoreCase) >= 0) label = "Сражение";
                    else if ((tab.Message ?? "").IndexOf("Battles", StringComparison.OrdinalIgnoreCase) >= 0) label = "Баталии";
                    else label = "Загрузить";
                }

                string group = DetectBattlesTopTabGroup(tab, actionName);
                if (string.IsNullOrWhiteSpace(actionName) && string.Equals(group, "Load", StringComparison.OrdinalIgnoreCase))
                    actionName = "cva_Battles_Mode_Load";
                bool active = IsBattlesTopTabActive(group, showBattles, showLoad);
                int baseFrame = ResolveTopTabBaseFrame(tab, group, active);

                PlaceOriginalTopTabButton(
                    parent,
                    cashDir,
                    tab.FileID,
                    baseFrame,
                    label,
                    tab.X,
                    tab.Y,
                    tab.Width,
                    tab.Height,
                    tab.FontDx,
                    tab.FontDy,
                    actionName,
                    sink,
                    active);
            }
        }

        private static string DetectBattlesTopTabGroup(MbGpTextButtonNode tab, string actionName)
        {
            if (string.Equals(actionName, "cva_Battles_Mode_Skirmish", StringComparison.OrdinalIgnoreCase))
                return "Skirmish";
            if (string.Equals(actionName, "cva_Battles_Mode_Battles", StringComparison.OrdinalIgnoreCase))
                return "Battles";

            string message = tab != null ? (tab.Message ?? "") : "";
            if (message.IndexOf("LoadAccept", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(message, "#LoadAccept", StringComparison.OrdinalIgnoreCase))
                return "Load";
            if (message.IndexOf("Skirmish", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Skirmish";
            if (message.IndexOf("Battles", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Battles";
            return "";
        }

        private static bool IsBattlesTopTabActive(string group, bool showBattles, bool showLoad)
        {
            if (string.Equals(group, "Skirmish", StringComparison.OrdinalIgnoreCase))
                return !showBattles && !showLoad;
            if (string.Equals(group, "Battles", StringComparison.OrdinalIgnoreCase))
                return showBattles && !showLoad;
            if (string.Equals(group, "Load", StringComparison.OrdinalIgnoreCase))
                return showLoad;
            return false;
        }

        private static int ResolveTopTabBaseFrame(MbGpTextButtonNode tab, string group, bool active)
        {
            if (tab == null)
                return 0;

            int passiveFrame = tab.Sprite;
            int activeFrame = tab.Sprite1 >= 0 ? tab.Sprite1 : tab.Sprite;

            if (!active)
                return passiveFrame;

            string fileId = tab.FileID ?? "";
            bool looksLikeOriginalTabStrip =
                fileId.IndexOf(@"Interf3\elements\tab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fileId.IndexOf(@"INTERF3\ELEMENTS\TAB", StringComparison.OrdinalIgnoreCase) >= 0;

            if (looksLikeOriginalTabStrip && activeFrame == passiveFrame && passiveFrame == 10)
                activeFrame = 5;

            if (string.Equals(group, "Load", StringComparison.OrdinalIgnoreCase) &&
                looksLikeOriginalTabStrip &&
                activeFrame == passiveFrame)
            {
                activeFrame = 5;
            }

            return activeFrame;
        }

        private void PlaceOriginalTopTabButton(
            RectTransform parent,
            string cashDir,
            string fileId,
            int baseFrame,
            string label,
            float x,
            float y,
            float w,
            float h,
            int fontDx,
            int fontDy,
            string actionName,
            IUiActionSink sink,
            bool active)
        {
            Sprite spL = LoadG16Sprite(cashDir, fileId, baseFrame);
            Sprite spR = LoadG16Sprite(cashDir, fileId, baseFrame + 1);
            Sprite spC1 = LoadG16Sprite(cashDir, fileId, baseFrame + 2);
            Sprite spC2 = LoadG16Sprite(cashDir, fileId, baseFrame + 3);
            Sprite spC3 = LoadG16Sprite(cashDir, fileId, baseFrame + 4);

            if (spL == null && spC1 != null) spL = spC1;
            if (spR == null && spC1 != null) spR = spC1;
            if (spC1 == null) spC1 = spL ?? spR;
            if (spC2 == null) spC2 = spC1;
            if (spC3 == null) spC3 = spC1;
            if (spL == null || spR == null || spC1 == null)
                return;

            TryForcePointFiltering(spL);
            TryForcePointFiltering(spR);
            TryForcePointFiltering(spC1);
            TryForcePointFiltering(spC2);
            TryForcePointFiltering(spC3);

            float widthL = Mathf.Max(1f, spL.rect.width);
            float widthR = Mathf.Max(1f, spR.rect.width);
            float nativeH = Mathf.Max(
                1f,
                Mathf.Max(
                    spL.rect.height,
                    Mathf.Max(
                        spR.rect.height,
                        Mathf.Max(
                            spC1.rect.height,
                            Mathf.Max(spC2 != null ? spC2.rect.height : 0f, spC3 != null ? spC3.rect.height : 0f)))));

            var root = new GameObject("LeftTab_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, Mathf.Max(h, nativeH));

            var hitImage = root.GetComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0.01f);
            hitImage.raycastTarget = true;

            var visualMask = new GameObject("VisualMask", typeof(RectTransform), typeof(RectMask2D));
            visualMask.transform.SetParent(root.transform, false);
            var maskRt = visualMask.GetComponent<RectTransform>();
            maskRt.anchorMin = maskRt.anchorMax = new Vector2(0, 1);
            maskRt.pivot = new Vector2(0, 1);
            maskRt.anchoredPosition = Vector2.zero;
            maskRt.sizeDelta = new Vector2(w, nativeH);

            void AddSpriteLayer(RectTransform host, string objName, Sprite sprite, float px, float py)
            {
                if (sprite == null) return;
                TryForcePointFiltering(sprite);

                float sw = Mathf.Max(1f, sprite.rect.width);
                float sh = Mathf.Max(1f, sprite.rect.height);

                int passCount = active ? 3 : 2;
                for (int pass = 0; pass < passCount; pass++)
                {
                    string passName = pass == 0 ? objName : (pass == 1 ? objName + "_Overlay" : objName + "_ActiveOverlay");
                    var go = new GameObject(passName, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(host, false);

                    var srt = go.GetComponent<RectTransform>();
                    srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
                    srt.pivot = new Vector2(0, 1);
                    srt.anchoredPosition = new Vector2(px, -py);
                    srt.sizeDelta = new Vector2(sw, sh);

                    var img = go.GetComponent<Image>();
                    img.sprite = sprite;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                    img.raycastTarget = false;
                    img.color = Color.white;
                }
            }

            Sprite[] centerSprites = { spC1, spC2, spC3 };
            float xPos = 0f;
            int tileIndex = 0;
            while (xPos < w && tileIndex < 300)
            {
                Sprite tileSp = centerSprites[tileIndex % 3] ?? spC1;
                if (tileSp == null)
                    break;

                AddSpriteLayer(maskRt, "Tile_" + tileIndex, tileSp, xPos, 0f);
                xPos += Mathf.Max(1f, tileSp.rect.width);
                tileIndex++;
            }

            AddSpriteLayer(maskRt, "EdgeL", spL, 0f, 0f);
            AddSpriteLayer(maskRt, "EdgeR", spR, w - widthR, 0f);

            var btn = root.GetComponent<Button>();
            if (!string.IsNullOrWhiteSpace(actionName))
            {
                btn.onClick.AddListener(() =>
                    sink.OnAction(label, new UiAction { Name = actionName, Payload = label }));
            }
            else
            {
                btn.interactable = false;
            }

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(root.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, 1);

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = 13f;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.text = label;
            tmp.color = active ? new Color32(30, 22, 14, 255) : new Color32(38, 28, 18, 255);
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;

            Vector2 pref = tmp.GetPreferredValues(label, w, nativeH);
            float textH = Mathf.Max(1f, Mathf.Ceil(pref.y));
            const float tabLabelRaisePx = 6f;
            float activeLabelShiftPx = active ? 1f : 0f;
            float textY = Mathf.Round(((nativeH - textH) * 0.5f) + fontDy - tabLabelRaisePx - activeLabelShiftPx);

            trt.anchoredPosition = new Vector2(fontDx, -textY);
            trt.sizeDelta = new Vector2(w, textH + 2f);
        }

        private static void TryForcePointFiltering(Sprite sp)
        {
            try
            {
                if (sp != null && sp.texture != null)
                    sp.texture.filterMode = FilterMode.Point;
            }
            catch { }
        }




        private void RenderLeftUpperFromScene(RectTransform parent,
            MbScene scene,
            string cashDir,
            IUiActionSink sink,
            bool showBattles,
            int visibleRosterSlots)
        {
            const float blockX = 63f;
            const float blockY = 231f;
            const float blockW = 399f;
            const float blockH = 224f;

            var upperBlockSprite = LoadG16Sprite(cashDir, @"INTERF3\ELEMENTS\BACKGROUND", 0);
            if (upperBlockSprite != null)
                PlaceSpr(parent, upperBlockSprite, blockX, blockY, blockW, blockH, "LeftUpperRoomBackground");

            var nameNode = scene.Nodes.OfType<MbTextNode>()
                .Where(n => n.Visible &&
                            n.Actions.Any(a => a.Name.Equals("cva_BR_PlName", StringComparison.OrdinalIgnoreCase)) &&
                            n.X < 150 &&
                            n.Y >= 190 && n.Y < 230)
                .OrderByDescending(n => n.Width)
                .ThenBy(n => Mathf.Abs(n.Y - 197f))
                .ThenBy(n => n.X)
                .FirstOrDefault();

            if (nameNode != null)
            {
                string playerName = string.IsNullOrWhiteSpace(MenuActionSink.CurrentProfileName)
                    ? "1"
                    : MenuActionSink.CurrentProfileName.Trim();

                // Keep the current profile name in one stable line. The XML source node is
                // narrower than the real visible field, so TMP starts wrapping vertically after
                // screen rebuilds if we use the raw XML width here.
                float nameW = 122f;
                float nameH = 18f;
                PlaceSingleLineText(parent, playerName, nameNode.X + 2f, nameNode.Y + 1f,
                    nameW, nameH, 17f, new Color32(170, 40, 40, 255), TextAlignmentOptions.Left);
            }


            int slotCount = Mathf.Clamp(visibleRosterSlots, 1, 7);

            var diffNodes = scene.Nodes.OfType<MbComboBoxNode>()
                .Where(n => n.Actions.Any(a => a.Name.Equals("cva_BR_PlName", StringComparison.OrdinalIgnoreCase)) &&
                            n.X < 180 &&
                            n.Y >= 220 && n.Y < 380)
                .OrderBy(n => n.Y).ThenBy(n => n.X)
                .ToList();

            for (int slot = 1; slot < slotCount; slot++)
            {
                int nodeIndex = slot - 1;
                float diffX = nodeIndex < diffNodes.Count ? diffNodes[nodeIndex].X : 75f;
                float diffY = nodeIndex < diffNodes.Count ? diffNodes[nodeIndex].Y : 222f + 25f * (slot - 1);
                float diffW = nodeIndex < diffNodes.Count ? diffNodes[nodeIndex].Width : 121f;
                float diffH = nodeIndex < diffNodes.Count ? diffNodes[nodeIndex].Height : 21f;

                if (!_playerDifficultyBySlot.ContainsKey(slot))
                    _playerDifficultyBySlot[slot] = _leftDifficultyIndex;

                int slotIndex = slot;
                PlaceRosterDropdown(parent, cashDir, "RosterDiff_" + slotIndex, diffX, diffY, diffW, diffH,
                    DebugDifficultyNames,
                    () => _playerDifficultyBySlot.TryGetValue(slotIndex, out var current) ? current : 0,
                    idx =>
                    {
                        _playerDifficultyBySlot[slotIndex] = idx;
                        if (slotIndex == 1)
                            _leftDifficultyIndex = idx;
                    },
                    "cva_BR_PlName", sink);
            }

            var raceNodes = scene.Nodes.OfType<MbComboBoxNode>()
                .Where(n => n.Actions.Any(a => a.Name.Equals("cva_BR_PlRace", StringComparison.OrdinalIgnoreCase)) &&
                            n.X >= 180 && n.X < 340 &&
                            n.Y >= 190 && n.Y < 380)
                .OrderBy(n => n.Y).ThenBy(n => n.X)
                .ToList();

            for (int slot = 0; slot < slotCount; slot++)
            {
                float rx = slot < raceNodes.Count ? raceNodes[slot].X : 201f;
                float ry = slot < raceNodes.Count ? raceNodes[slot].Y : 197f + 25f * slot;
                float rw = slot < raceNodes.Count ? raceNodes[slot].Width : 121f;
                float rh = slot < raceNodes.Count ? raceNodes[slot].Height : 21f;

                if (!_playerRaceBySlot.ContainsKey(slot))
                    _playerRaceBySlot[slot] = 0;

                int slotIndex = slot;
                PlaceRosterDropdown(parent, cashDir, "RosterRace_" + slotIndex, rx, ry, rw, rh,
                    DebugRaceNames,
                    () => _playerRaceBySlot.TryGetValue(slotIndex, out var current) ? current : 0,
                    idx => _playerRaceBySlot[slotIndex] = idx,
                    "cva_BR_PlRace", sink);
            }

            var flagNodes = scene.Nodes.OfType<MbGpPictureNode>()
                .Where(n => n.Visible &&
                            n.X >= 318 && n.X < 380 &&
                            n.Y >= 190 && n.Y < 380 &&
                            (n.Actions.Any(a => a.Name.Equals("cva_BR_PlColor", StringComparison.OrdinalIgnoreCase)) ||
                             n.FileID.IndexOf("PLAYERCOLOR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             n.FileID.IndexOf("RoomNatColor", StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(n => n.Y).ThenBy(n => n.X)
                .ToList();

            for (int slot = 0; slot < slotCount; slot++)
            {
                MbGpPictureNode gp = slot < flagNodes.Count ? flagNodes[slot] : null;
                float fx = gp != null ? gp.X : 335f;
                float fy = gp != null ? gp.Y : 199f + 25f * slot;
                float fw = gp != null ? gp.Width : 30f;
                float fh = gp != null ? gp.Height : 20f;

                Sprite baseSprite = gp != null
                    ? (LoadG16Sprite(cashDir, gp.FileID, gp.SpriteID)
                       ?? LoadG16Sprite(cashDir, @"INTERF3\PLAYERCOLOR", gp.SpriteID)
                       ?? LoadG16Sprite(cashDir, @"INTERF3\RoomNatColor", gp.SpriteID))
                    : null;
                baseSprite = baseSprite
                    ?? LoadG16Sprite(cashDir, @"INTERF3\PLAYERCOLOR", 0)
                    ?? LoadG16Sprite(cashDir, @"INTERF3\RoomNatColor", 0)
                    ?? GetWhiteUiSprite();

                var go = new GameObject("PlayerFlag_" + slot, typeof(RectTransform), typeof(Image), typeof(PointerClickRelay));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(fx, -fy);
                rt.sizeDelta = new Vector2(fw, fh);

                var img = go.GetComponent<Image>();
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = true;

                int slotIndex = slot;
                if (!_playerFlagColorBySlot.ContainsKey(slotIndex))
                    _playerFlagColorBySlot[slotIndex] = slotIndex;

                img.sprite = baseSprite;
                img.color = GetFallbackFlagTint(_playerFlagColorBySlot[slotIndex]);

                var relay = go.GetComponent<PointerClickRelay>();
                relay.Clicked = button =>
                {
                    int colorCount = FallbackPlayerFlagTints.Length;
                    int current = _playerFlagColorBySlot.TryGetValue(slotIndex, out var stored) ? stored : slotIndex;
                    if (button == PointerEventData.InputButton.Right)
                        current = (current - 1 + colorCount) % colorCount;
                    else
                        current = (current + 1) % colorCount;

                    _playerFlagColorBySlot[slotIndex] = current;
                    img.sprite = baseSprite;
                    img.color = GetFallbackFlagTint(current);

                    sink.OnAction("flag",
                        new UiAction
                        {
                            Name = "cva_Battles_PlayerColor",
                            Payload = slotIndex.ToString()
                        });
                };
            }

            var teamNodes = scene.Nodes.OfType<MbTextNode>()
                .Where(n => n.Visible &&
                            n.Actions.Any(a => a.Name.Equals("cva_BR_PlTeam", StringComparison.OrdinalIgnoreCase)) &&
                            n.X >= 370 && n.X < 430 &&
                            n.Y >= 190 && n.Y < 380)
                .OrderBy(n => n.Y).ThenBy(n => n.X)
                .ToList();

            float teamCenterX = teamNodes.Count > 0
                ? teamNodes[0].X + teamNodes[0].Width * 0.5f
                : 395.5f;
            const float normalizedTeamWidth = 41f;

            for (int slot = 0; slot < slotCount; slot++)
            {
                float tx;
                float ty;
                float tw;
                float th;

                if (slot < teamNodes.Count)
                {
                    ty = teamNodes[slot].Y;
                    th = teamNodes[slot].Height;
                }
                else if (slot == 0)
                {
                    ty = 197f; th = 23f;
                }
                else
                {
                    ty = 224f + 25f * (slot - 1); th = 23f;
                }

                tw = normalizedTeamWidth;
                tx = Mathf.Round(teamCenterX - tw * 0.5f);

                if (!_playerTeamBySlot.ContainsKey(slot))
                {
                    int initial = slot == 0 ? 1 : 0;
                    if (slot < teamNodes.Count)
                    {
                        string msg = (teamNodes[slot].Message ?? "").Trim();
                        if (int.TryParse(msg, out var parsed))
                            initial = parsed;
                    }
                    _playerTeamBySlot[slot] = initial;
                }

                RenderTeamValue(parent, tx, ty + 1f, tw, th, slot, sink);
            }
        }

        private void PlaceRosterDropdown(RectTransform parent, string cashDir, string objectName,
            float x, float y, float w, float h, string[] items, Func<int> getIndex, Action<int> setIndex,
            string actionName, IUiActionSink sink)
        {
            if (items == null || items.Length == 0)
                return;

            Sprite closedNormalSp = LoadComboFrameSprite(1) ?? LoadComboFrameSprite(0) ?? LoadG16Sprite(cashDir, "Interf3_elements_combo", 1);
            Sprite closedOpenSp = LoadComboFrameSprite(0) ?? closedNormalSp;
            Sprite rowNormalSp = LoadComboFrameSprite(5) ?? closedNormalSp;
            Sprite rowHoverSp = LoadComboFrameSprite(6) ?? rowNormalSp;

            var host = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Button));
            host.transform.SetParent(parent, false);
            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var hostImg = host.GetComponent<Image>();
            hostImg.color = new Color(1f, 1f, 1f, 0.01f);
            hostImg.raycastTarget = true;

            var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(host.transform, false);
            var visualRt = visual.GetComponent<RectTransform>();
            visualRt.anchorMin = new Vector2(0, 1);
            visualRt.anchorMax = new Vector2(0, 1);
            visualRt.pivot = new Vector2(0, 1);
            visualRt.anchoredPosition = Vector2.zero;
            visualRt.sizeDelta = new Vector2(closedNormalSp != null ? closedNormalSp.rect.width : w, closedNormalSp != null ? closedNormalSp.rect.height : h);
            var visualImg = visual.GetComponent<Image>();
            visualImg.sprite = closedNormalSp ?? GetWhiteUiSprite();
            visualImg.type = Image.Type.Simple;
            visualImg.preserveAspect = false;
            visualImg.color = closedNormalSp != null ? Color.white : new Color(1f, 1f, 1f, 0.92f);
            visualImg.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(host.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(25f, 1f);
            textRt.offsetMax = new Vector2(-4f, -1f);
            var label = textGo.GetComponent<TextMeshProUGUI>();
            label.font = LoadFont();
            label.fontSize = 14f;
            int selectedIndex = Mathf.Clamp(getIndex(), 0, items.Length - 1);
            label.text = items[selectedIndex];
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color32(40, 30, 20, 255);
            label.raycastTarget = false;

            var blocker = new GameObject(objectName + "_Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
            blocker.transform.SetParent(parent, false);
            var brt = blocker.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var bimg = blocker.GetComponent<Image>();
            bimg.color = new Color(0f, 0f, 0f, 0f);
            bimg.raycastTarget = true;
            blocker.SetActive(false);

            var popup = new GameObject(objectName + "_Popup", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            popup.transform.SetParent(parent, false);
            var popupRt = popup.GetComponent<RectTransform>();
            popupRt.anchorMin = popupRt.anchorMax = new Vector2(0, 1);
            popupRt.pivot = new Vector2(0, 1);
            popupRt.anchoredPosition = new Vector2(x, -(y + h));
            popupRt.sizeDelta = new Vector2(w, h * items.Length);
            var popupImg = popup.GetComponent<Image>();
            popupImg.color = new Color(1f, 1f, 1f, 0.01f);
            popupImg.raycastTarget = true;
            popup.SetActive(false);

            for (int i = 0; i < items.Length; i++)
            {
                CreateRosterComboOption(popup.transform, rowNormalSp, rowHoverSp, 0f, i * h, w, h, items[i], i,
                    actionName, sink, popup, label, blocker, setIndex, visualImg, closedNormalSp, closedOpenSp);
            }

            void ClosePopup()
            {
                popup.SetActive(false);
                blocker.SetActive(false);
                visualImg.sprite = closedNormalSp ?? GetWhiteUiSprite();
            }

            host.GetComponent<Button>().onClick.AddListener(() =>
            {
                bool next = !popup.activeSelf;
                popup.SetActive(next);
                blocker.SetActive(next);
                visualImg.sprite = next ? (closedOpenSp ?? closedNormalSp ?? GetWhiteUiSprite()) : (closedNormalSp ?? GetWhiteUiSprite());
                if (next)
                {
                    blocker.transform.SetAsLastSibling();
                    popup.transform.SetAsLastSibling();
                    host.transform.SetAsLastSibling();
                }
            });

            blocker.GetComponent<Button>().onClick.AddListener(ClosePopup);
            host.transform.SetAsLastSibling();
        }

        private void CreateRosterComboOption(Transform parent, Sprite rowSp, Sprite rowHoverSp,
            float x, float y, float w, float h, string title, int optionIndex, string actionName,
            IUiActionSink sink, GameObject popup, TextMeshProUGUI hostLabel, GameObject blocker, Action<int> setIndex,
            Image closedImage, Sprite closedNormalSp, Sprite closedOpenSp)
        {
            var optionHost = new GameObject("OptionHost_" + title, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Button));
            optionHost.transform.SetParent(parent, false);
            var rt = optionHost.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var hostImg = optionHost.GetComponent<Image>();
            hostImg.color = new Color(1f, 1f, 1f, 0.01f);
            hostImg.raycastTarget = true;

            var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(optionHost.transform, false);
            var vrt = visual.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0, 1);
            vrt.anchorMax = new Vector2(0, 1);
            vrt.pivot = new Vector2(0, 1);
            vrt.anchoredPosition = Vector2.zero;
            vrt.sizeDelta = new Vector2(rowSp != null ? rowSp.rect.width : w, rowSp != null ? rowSp.rect.height : h);

            var vimg = visual.GetComponent<Image>();
            vimg.sprite = rowSp ?? GetWhiteUiSprite();
            vimg.type = Image.Type.Simple;
            vimg.preserveAspect = false;
            vimg.color = rowSp != null ? Color.white : new Color(1f, 1f, 1f, 0.92f);
            vimg.raycastTarget = false;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(optionHost.transform, false);
            var trt2 = txtGo.GetComponent<RectTransform>();
            trt2.anchorMin = Vector2.zero;
            trt2.anchorMax = Vector2.one;
            trt2.offsetMin = new Vector2(25f, 1f);
            trt2.offsetMax = new Vector2(-4f, -1f);
            var tmp2 = txtGo.GetComponent<TextMeshProUGUI>();
            tmp2.font = LoadFont();
            tmp2.fontSize = 14f;
            tmp2.text = title;
            tmp2.alignment = TextAlignmentOptions.MidlineLeft;
            tmp2.color = new Color32(40, 30, 20, 255);
            tmp2.raycastTarget = false;

            var hover = optionHost.AddComponent<RowHoverSwapExact>();
            hover.Image = vimg;
            hover.Normal = rowSp;
            hover.Hover = rowHoverSp ?? rowSp;

            optionHost.GetComponent<Button>().onClick.AddListener(() =>
            {
                setIndex(optionIndex);
                hostLabel.text = title;
                popup.SetActive(false);
                blocker.SetActive(false);
                if (closedImage != null)
                    closedImage.sprite = closedNormalSp ?? closedOpenSp ?? GetWhiteUiSprite();
                sink.OnAction(title, new UiAction { Name = actionName, Payload = optionIndex.ToString() });
            });
        }

        private void RenderTeamValue(RectTransform parent, float x, float y, float w, float h, int slotIndex, IUiActionSink sink)
        {
            var host = new GameObject("RosterTeam_" + slotIndex, typeof(RectTransform), typeof(Image), typeof(PointerClickRelay));
            host.transform.SetParent(parent, false);
            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(14f, w), h);

            var img = host.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.01f);
            img.raycastTarget = true;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(host.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var label = textGo.GetComponent<TextMeshProUGUI>();
            label.font = LoadFont();
            label.fontSize = 14f;
            label.text = TeamValueToText(_playerTeamBySlot.TryGetValue(slotIndex, out var current) ? current : 0);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(70, 60, 48, 255);
            label.raycastTarget = false;

            var relay = host.GetComponent<PointerClickRelay>();
            relay.Clicked = button =>
            {
                int currentValue = _playerTeamBySlot.TryGetValue(slotIndex, out var stored) ? stored : 0;
                if (button == PointerEventData.InputButton.Right)
                    currentValue = currentValue == 0 ? 4 : currentValue - 1;
                else
                    currentValue = currentValue < 4 ? currentValue + 1 : 0;

                _playerTeamBySlot[slotIndex] = currentValue;
                label.text = TeamValueToText(currentValue);
                sink.OnAction(label.text, new UiAction { Name = "cva_BR_PlTeam", Payload = currentValue.ToString() });
            };
        }

        private static string TeamValueToText(int value)
        {
            return value <= 0 ? "-" : value.ToString();
        }

        private void RenderStaticCombo(RectTransform parent, string cashDir,
            float x, float y, float w, float h, string displayText, string actionName, IUiActionSink sink)
        {
            Sprite boxSp = LoadComboFrameSprite(0) ?? LoadG16Sprite(cashDir, "Interf3_elements_combo", 0);

            var host = new GameObject("StaticCombo_" + actionName, typeof(RectTransform), typeof(Image), typeof(Button));
            host.transform.SetParent(parent, false);
            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var hostImg = host.GetComponent<Image>();
            hostImg.color = new Color(1f, 1f, 1f, 0.01f);
            hostImg.raycastTarget = true;

            if (boxSp != null)
            {
                var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
                visual.transform.SetParent(host.transform, false);
                var vrt = visual.GetComponent<RectTransform>();
                vrt.anchorMin = new Vector2(0, 1);
                vrt.anchorMax = new Vector2(0, 1);
                vrt.pivot = new Vector2(0, 1);
                vrt.anchoredPosition = Vector2.zero;
                vrt.sizeDelta = new Vector2(w, h);
                var vimg = visual.GetComponent<Image>();
                vimg.sprite = boxSp;
                vimg.type = Image.Type.Simple;
                vimg.preserveAspect = false;
                vimg.raycastTarget = false;
            }

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(host.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(29f, 1f);
            textRt.offsetMax = new Vector2(-4f, -1f);

            var label = textGo.GetComponent<TextMeshProUGUI>();
            label.font = LoadFont();
            label.fontSize = 14f;
            label.text = displayText;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color32(40, 30, 20, 255);
            label.raycastTarget = false;

            host.GetComponent<Button>().onClick.AddListener(() =>
                sink.OnAction(displayText, new UiAction { Name = actionName, Payload = displayText }));
        }


        private void RenderMissionListFromScene(RectTransform parent,
            MbScene scene,
            string cashDir,
            List<BattleEntrySimple> entries,
            string selectedId,
            IUiActionSink sink,
            bool showBattles)
        {
            var listNode = scene.Nodes.OfType<MbListNode>()
                .Where(n => n.Visible && n.Name.Equals("S", StringComparison.OrdinalIgnoreCase))
                .Where(n => n.Actions.Any(a => a.Name.Equals(showBattles ? "va_LD_Battle" : "va_LD_Skirmish", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(n => n.Y)
                .ThenBy(n => n.X)
                .FirstOrDefault();

            if (listNode == null)
            {
                PlaceMissionList(parent, cashDir, entries, selectedId, sink);
                return;
            }

            DrawPaperFillExact(parent, cashDir, listNode.X, listNode.Y, listNode.Width, listNode.Height, "MissionListPaper");
            DrawBDLines(parent, cashDir, listNode.X, listNode.Y, listNode.Width, listNode.Height, "MissionListFrame");

            const float marginX = 3f;
            const float marginY = 3f;
            const float rowH = 20f;

            var panel = new GameObject("MissionList", typeof(RectTransform), typeof(RectMask2D));
            panel.transform.SetParent(parent, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 1);
            prt.anchoredPosition = new Vector2(listNode.X + marginX, -(listNode.Y + marginY));
            prt.sizeDelta = new Vector2(listNode.Width - marginX * 2f, listNode.Height - marginY * 2f);

            float y = 0f;
            int maxRows = Mathf.Max(1, Mathf.FloorToInt((prt.sizeDelta.y + marginY) / (rowH + marginY)));
            int shown = 0;
            float rowW = Mathf.Max(1f, prt.sizeDelta.x);

            foreach (var e in entries)
            {
                if (shown >= maxRows)
                    break;

                CreateMissionListRow(
                    panel.transform as RectTransform,
                    cashDir,
                    e,
                    selectedId,
                    0f,
                    y,
                    rowW,
                    rowH,
                    sink);

                y += rowH + marginY;
                shown++;
            }
        }

        private string ResolveLocOrFallback(LocDb loc, string key, string fallback)
        {
            string text = key ?? "";
            if (loc != null && !string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    string resolved = loc.Resolve(key);
                    if (!string.IsNullOrWhiteSpace(resolved))
                        text = resolved;
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(text) || text == key)
                text = fallback;

            return text ?? "";
        }

        private void PlaceMissionList(RectTransform parent,
            string cashDir,
            List<BattleEntrySimple> entries,
            string selectedId, IUiActionSink sink)
        {
            var panel = new GameObject("MissionList", typeof(RectTransform), typeof(Image), typeof(Mask));
            panel.transform.SetParent(parent, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 1);
            prt.anchoredPosition = new Vector2(73, -415);
            prt.sizeDelta = new Vector2(365, 265);
            var pimg = panel.GetComponent<Image>();
            pimg.color = new Color(1, 1, 1, 0.01f);
            panel.GetComponent<Mask>().showMaskGraphic = false;

            float y = 3f;
            int maxRows = 10;
            int shown = 0;
            float rowW = 360f;

            foreach (var e in entries)
            {
                if (shown >= maxRows)
                    break;

                CreateMissionListRow(
                    panel.transform as RectTransform,
                    cashDir,
                    e,
                    selectedId,
                    0f,
                    y,
                    rowW,
                    20f,
                    sink);

                y += 23f;
                shown++;
            }
        }

        private void CreateMissionListRow(
            RectTransform parent,
            string cashDir,
            BattleEntrySimple entry,
            string selectedId,
            float x,
            float y,
            float width,
            float height,
            IUiActionSink sink)
        {
            bool isSel = string.Equals(entry.Id, selectedId, StringComparison.OrdinalIgnoreCase);

            var row = new GameObject("Row_" + entry.Id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PointerClickRelay), typeof(MissionListRowHover));
            row.transform.SetParent(parent, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0, 1);
            rrt.pivot = new Vector2(0, 1);
            rrt.anchoredPosition = new Vector2(x, -y);
            rrt.sizeDelta = new Vector2(width, height);

            var rowImg = row.GetComponent<Image>();
            rowImg.color = new Color(1f, 1f, 1f, 0.01f);
            rowImg.raycastTarget = true;

            GameObject hoverVisual = null;
            RectTransform hoverStrip = CreateTiledStrip(row.transform as RectTransform,
                "HoverVisual", cashDir, "interf3_elements_vbuttons", 0, 0f, 0f, width, height);
            if (hoverStrip != null)
            {
                hoverVisual = hoverStrip.gameObject;
                hoverVisual.SetActive(false);
            }

            GameObject selectedVisual = null;
            RectTransform selectedStrip = CreateTiledStrip(row.transform as RectTransform,
                "SelectedVisual", cashDir, "interf3_elements_vbuttons", 5, 0f, 0f, width, height);
            if (selectedStrip != null)
            {
                selectedVisual = selectedStrip.gameObject;
                selectedVisual.SetActive(isSel);
            }

            if (hoverVisual == null && selectedVisual == null)
            {
                var fallbackGo = new GameObject("FallbackHighlight", typeof(RectTransform), typeof(Image));
                fallbackGo.transform.SetParent(row.transform, false);
                var hrt = fallbackGo.GetComponent<RectTransform>();
                hrt.anchorMin = Vector2.zero;
                hrt.anchorMax = Vector2.one;
                hrt.offsetMin = Vector2.zero;
                hrt.offsetMax = Vector2.zero;
                var fallbackImg = fallbackGo.GetComponent<Image>();
                fallbackImg.color = new Color32(197, 54, 45, 96);
                fallbackImg.raycastTarget = false;
                fallbackGo.SetActive(isSel);
                selectedVisual = fallbackGo;
            }

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(row.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 0f);
            trt.offsetMax = new Vector2(-4f, 0f);

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = 15f;
            tmp.text = entry.DisplayName;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = isSel ? new Color32(170, 40, 40, 255) : new Color32(40, 30, 25, 255);
            tmp.raycastTarget = false;
#pragma warning disable CS0618
            tmp.enableWordWrapping = false;
#pragma warning restore CS0618
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            string id = entry.Id;
            Action selectRow = () => sink.OnAction(id, new UiAction { Name = "cva_Battles_Select", Payload = id });
            var rowButton = row.GetComponent<Button>();
            rowButton.transition = Selectable.Transition.None;
            rowButton.onClick.AddListener(() => selectRow());
            row.GetComponent<PointerClickRelay>().Clicked = _ => selectRow();

            var hover = row.GetComponent<MissionListRowHover>();
            hover.HoverVisual = hoverVisual;
            hover.SelectedVisual = selectedVisual;
            hover.Label = tmp;
            hover.PassiveColor = new Color32(40, 30, 25, 255);
            hover.HoverColor = new Color32(170, 40, 40, 255);
            hover.SelectedColor = new Color32(170, 40, 40, 255);
            hover.Selected = isSel;
            hover.Refresh();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAP PREVIEW
        // ─────────────────────────────────────────────────────────────────────
        private sealed class PreviewMeta
        {
            public string RelativePath = "";
            public int CenterX;
            public int CenterY;
            public readonly List<Vector2> ScreenSaver = new List<Vector2>();
        }


        private int GetMissionPlayerCount(CoreFileSystem fs, string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return 2;

            string missionsAbs = "";
            try { missionsAbs = fs.ResolvePath(@"Missions\missions.txt"); } catch { missionsAbs = ""; }
            if (string.IsNullOrWhiteSpace(missionsAbs) || !File.Exists(missionsAbs))
                return 2;

            string text;
            try
            {
                text = ReadTextSmart(missionsAbs);
            }
            catch
            {
                return 2;
            }

            if (string.IsNullOrWhiteSpace(text))
                return 2;

            string wanted = "#" + missionId.Trim();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = (lines[i] ?? "").Trim();
                if (!line.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                    continue;

                for (int j = i + 1; j < Math.Min(lines.Length, i + 12); j++)
                {
                    string s = (lines[j] ?? "").Trim();
                    if (int.TryParse(s, out var parsed))
                        return Mathf.Clamp(parsed, 1, 7);
                }

                break;
            }

            return 2;
        }

        private PreviewMeta LoadPreviewMeta(CoreFileSystem fs, string missionId, bool showBattles)
        {
            var meta = new PreviewMeta();
            if (string.IsNullOrWhiteSpace(missionId))
                return meta;

            string missionsAbs;
            try
            {
                missionsAbs = fs.ResolvePath(@"Missions\missions.txt");
            }
            catch
            {
                return meta;
            }

            if (string.IsNullOrWhiteSpace(missionsAbs) || !File.Exists(missionsAbs))
                return meta;

            string text;
            try
            {
                text = ReadTextSmart(missionsAbs);
            }
            catch
            {
                return meta;
            }

            if (string.IsNullOrWhiteSpace(text))
                return meta;

            string wanted = "#" + missionId.Trim();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = (lines[i] ?? "").Trim();
                if (!line.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                    continue;

                for (int j = i + 1; j < Math.Min(lines.Length, i + 12); j++)
                {
                    if (!TryParsePreviewSpec(lines[j], out string relPath, out int cx, out int cy))
                        continue;

                    meta.RelativePath = relPath;
                    meta.CenterX = cx;
                    meta.CenterY = cy;

                    if (j + 1 < lines.Length)
                        TryParseScreenSaver(lines[j + 1], meta.ScreenSaver);

                    return meta;
                }

                break;
            }

            return meta;
        }

        private static bool TryParsePreviewSpec(string line, out string relPath, out int cx, out int cy)
        {
            relPath = "";
            cx = 0;
            cy = 0;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var m = Regex.Match(
                line.Trim(),
                @"^(?<path>\S+\.(?:jpg|jpeg|png|bmp|tga))\s+(?<cx>-?\d+)\s+(?<cy>-?\d+)\s*$",
                RegexOptions.IgnoreCase);

            if (!m.Success)
                return false;

            relPath = m.Groups["path"].Value.Trim();
            return int.TryParse(m.Groups["cx"].Value, out cx)
                && int.TryParse(m.Groups["cy"].Value, out cy);
        }

        private static void TryParseScreenSaver(string line, List<Vector2> dst)
        {
            if (dst == null || string.IsNullOrWhiteSpace(line))
                return;

            var matches = Regex.Matches(line, @"-?\d+");
            if (matches.Count < 4)
                return;

            if (int.TryParse(matches[0].Value, out int x1) &&
                int.TryParse(matches[1].Value, out int y1) &&
                int.TryParse(matches[2].Value, out int x2) &&
                int.TryParse(matches[3].Value, out int y2))
            {
                dst.Add(new Vector2(x1, y1));
                dst.Add(new Vector2(x2, y2));
            }
        }

        private static string ResolvePreviewAbsolutePath(CoreFileSystem fs, PreviewMeta meta, string fallbackAbsPath)
        {
            if (meta != null && !string.IsNullOrWhiteSpace(meta.RelativePath))
            {
                try
                {
                    string abs = fs.ResolvePath(meta.RelativePath);
                    if (!string.IsNullOrWhiteSpace(abs) && File.Exists(abs))
                        return abs;
                }
                catch
                {
                }

                try
                {
                    string resAbs = Path.Combine(
                        Application.dataPath,
                        "Resources",
                        meta.RelativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(resAbs))
                        return resAbs;
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(fallbackAbsPath) && File.Exists(fallbackAbsPath))
                return fallbackAbsPath;

            return "";
        }

        private static int ClampPreviewCenter(int center, float hostSize, float imageSize)
        {
            if (imageSize <= hostSize)
                return Mathf.RoundToInt(imageSize * 0.5f);

            int min = Mathf.RoundToInt(hostSize * 0.5f);
            int max = Mathf.RoundToInt(imageSize - hostSize * 0.5f);
            if (center < min) center = min;
            if (center > max) center = max;
            return center;
        }

private static Vector2 ClampPreviewOffset(Vector2 offset, float hostW, float hostH, float imageW, float imageH)
{
    float minX = Mathf.Min(0f, hostW - imageW);
    float maxX = 0f;

    if (imageW <= hostW)
        offset.x = (hostW - imageW) * 0.5f;
    else
        offset.x = Mathf.Clamp(offset.x, minX, maxX);

    // Top-left anchored image:
    // y = 0                   -> top edge aligned with viewport
    // y = imageH - hostH      -> bottom edge aligned with viewport
    // negative y would sink the image down out of the viewport, which must never happen.
    if (imageH <= hostH)
        offset.y = -(hostH - imageH) * 0.5f;
    else
        offset.y = Mathf.Clamp(offset.y, 0f, imageH - hostH);

    return offset;
}

        private sealed class MapPreviewJpgController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private RectTransform _host;
            private RectTransform _image;
            private float _hostW;
            private float _hostH;
            private float _imageW;
            private float _imageH;
            private int _centerX;
            private int _centerY;
            private readonly List<Vector2> _screenSaver = new List<Vector2>();

            private bool _initialized;
            private bool _dragging;
            private float _lastInteractionTime;
            private bool _autoActive;
            private float _segmentStartTime;
            private int _state;
            private int _state2;
            private Vector2 _segmentFrom;
            private Vector2 _segmentTo;
            private Vector2 _dragOffsetAtStart;

            public void Initialize(RectTransform host, RectTransform image, int centerX, int centerY, IEnumerable<Vector2> screenSaver)
            {
                _host = host;
                _image = image;
                RefreshSizes();
                _centerX = centerX;
                _centerY = centerY;
                _screenSaver.Clear();
                if (screenSaver != null)
                    _screenSaver.AddRange(screenSaver.Take(2));
            }

            private void Start()
            {
                EnsureInitialized();
            }

            private void OnEnable()
            {
                EnsureInitialized();
            }

            private void Update()
            {
                EnsureInitialized();

                if (!_initialized || _dragging || _screenSaver.Count < 2)
                    return;

                float now = Time.unscaledTime;
                if (!_autoActive)
                {
                    if (now - _lastInteractionTime > 2f)
                    {
                        _autoActive = true;
                        _segmentStartTime = now;
                        _state = -1;
                        _segmentFrom = GetCurrentCenter();
                        PrepareNextAutoTarget();
                    }
                    return;
                }

                if (now - _segmentStartTime > 15f)
                {
                    _segmentStartTime = now;
                    _state = _state2;
                    _segmentFrom = ClampCenter(_screenSaver[_state]);
                    PrepareNextAutoTarget();
                }

                float t = Mathf.Clamp01((now - _segmentStartTime) / 10f);
                Vector2 center = Vector2.Lerp(_segmentFrom, _segmentTo, t);
                SetOffsetFromCenter(center);
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                EnsureInitialized();
                if (!_initialized)
                    return;

                _dragging = true;
                _autoActive = false;
                _lastInteractionTime = Time.unscaledTime;
                _dragOffsetAtStart = GetCurrentOffset();
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (_host == null || _image == null)
                    return;

                // Top-left anchored image: negative Y moves the picture down inside the viewport.
                Vector2 next = _dragOffsetAtStart + new Vector2(eventData.position.x - eventData.pressPosition.x, eventData.position.y - eventData.pressPosition.y);
                SetOffset(next);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                _dragging = false;
                _lastInteractionTime = Time.unscaledTime;
            }

            private void EnsureInitialized()
            {
                if (_initialized || _host == null || _image == null)
                    return;

                RefreshSizes();

                Vector2 center;
                if (_screenSaver.Count > 0)
                {
                    _state = 0;
                    center = ClampCenter(_screenSaver[0]);
                }
                else
                {
                    int cx = ClampPreviewCenter(_centerX > 0 ? _centerX : Mathf.RoundToInt(_imageW * 0.5f), _hostW, _imageW);
                    int cy = ClampPreviewCenter(_centerY > 0 ? _centerY : Mathf.RoundToInt(_imageH * 0.5f), _hostH, _imageH);
                    center = new Vector2(cx, cy);
                }

                SetOffsetFromCenter(center);
                _lastInteractionTime = Time.unscaledTime;
                _initialized = true;
            }

            private void RefreshSizes()
            {
                if (_host == null || _image == null)
                    return;

                _hostW = Mathf.Max(1f, _host.rect.width);
                _hostH = Mathf.Max(1f, _host.rect.height);
                _imageW = Mathf.Max(1f, _image.sizeDelta.x);
                _imageH = Mathf.Max(1f, _image.sizeDelta.y);
            }

            private void PrepareNextAutoTarget()
            {
                if (_screenSaver.Count < 2)
                    return;

                if (_state == -1)
                {
                    float best = -1f;
                    int bestIdx = 0;
                    Vector2 cur = _segmentFrom;
                    for (int i = 0; i < 2 && i < _screenSaver.Count; i++)
                    {
                        Vector2 candidate = ClampCenter(_screenSaver[i]);
                        float dist = (cur - candidate).sqrMagnitude;
                        if (dist > best)
                        {
                            best = dist;
                            bestIdx = i;
                        }
                    }
                    _state2 = bestIdx;
                }
                else
                {
                    _state2 = _state == 0 ? 1 : 0;
                }

                _segmentTo = ClampCenter(_screenSaver[_state2]);
            }

            private Vector2 ClampCenter(Vector2 center)
            {
                int cx = ClampPreviewCenter(Mathf.RoundToInt(center.x), _hostW, _imageW);
                int cy = ClampPreviewCenter(Mathf.RoundToInt(center.y), _hostH, _imageH);
                return new Vector2(cx, cy);
            }

            private Vector2 GetCurrentOffset()
            {
                return _image.anchoredPosition;
            }

            private Vector2 GetCurrentCenter()
            {
                Vector2 pos = GetCurrentOffset();
                return new Vector2(_hostW * 0.5f - pos.x, _hostH * 0.5f + pos.y);
            }

            private void SetOffsetFromCenter(Vector2 center)
            {
                Vector2 pos = new Vector2(_hostW * 0.5f - center.x, center.y - _hostH * 0.5f);
                SetOffset(pos);
            }

            private void SetOffset(Vector2 pos)
            {
                _image.anchoredPosition = ClampPreviewOffset(pos, _hostW, _hostH, _imageW, _imageH);
            }
        }

        private Sprite GetPreviewHintTriangleSprite()
        {
            if (_previewHintTriangleSprite != null)
                return _previewHintTriangleSprite;

            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var clear = new Color32(0, 0, 0, 0);
            var fill = new Color32(255, 255, 255, 170);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);
            }

            float cx = (size - 1) * 0.5f;
            float apexY = 3f;
            float baseY = size - 4f;
            for (int y = 0; y < size; y++)
            {
                float t = Mathf.InverseLerp(apexY, baseY, y);
                if (t < 0f || t > 1f)
                    continue;

                float halfWidth = Mathf.Lerp(0f, size * 0.34f, t);
                int x0 = Mathf.Max(0, Mathf.RoundToInt(cx - halfWidth));
                int x1 = Mathf.Min(size - 1, Mathf.RoundToInt(cx + halfWidth));
                for (int x = x0; x <= x1; x++)
                    tex.SetPixel(x, y, fill);
            }

            tex.Apply(false, false);
            _previewHintTriangleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
            return _previewHintTriangleSprite;
        }

        private void AddPreviewHintGroup(RectTransform parent, string name, Vector2 center, float rotationZ, int count, float spacing)
        {
            var sprite = GetPreviewHintTriangleSprite();
            if (sprite == null)
                return;

            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            var grt = group.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0, 1);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = center;
            grt.sizeDelta = new Vector2(1f, 1f);

            bool horizontal = Mathf.Abs(Mathf.DeltaAngle(rotationZ, 90f)) < 0.1f || Mathf.Abs(Mathf.DeltaAngle(rotationZ, 270f)) < 0.1f;
            for (int i = 0; i < count; i++)
            {
                float shift = (i - (count - 1) * 0.5f) * spacing;
                var go = new GameObject(name + "_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(group.transform, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(12f, 12f);
                rt.anchoredPosition = horizontal ? new Vector2(shift, 0f) : new Vector2(0f, shift);
                rt.localEulerAngles = new Vector3(0f, 0f, rotationZ);

                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.color = new Color32(255, 255, 255, 150);
                img.raycastTarget = false;
            }
        }

private void CreatePreviewHintArrows(RectTransform hostRt)
{
    if (hostRt == null)
        return;

    float w = hostRt.rect.width;
    float h = hostRt.rect.height;

    // Original preview hint layout:
    // top    = 1 arrow
    // bottom = 1 arrow
    // left   = 3 arrows
    // right  = 3 arrows
    AddPreviewHintGroup(hostRt, "PreviewHintTop",    new Vector2(w * 0.5f, -14f),        180f, 1, 10f);
    AddPreviewHintGroup(hostRt, "PreviewHintBottom", new Vector2(w * 0.5f, -(h - 14f)),    0f, 1, 10f);
    AddPreviewHintGroup(hostRt, "PreviewHintLeft",   new Vector2(16f,       -(h * 0.5f)),  90f, 3, 10f);
    AddPreviewHintGroup(hostRt, "PreviewHintRight",  new Vector2(w - 16f,   -(h * 0.5f)), 270f, 3, 10f);
}

        private static bool IsPreviewHostNode(MbDeskNode node)
        {
            return node != null &&
                   string.Equals(node.Role, "PreviewHost", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPreviewFrameNode(MbGpPictureNode node)
        {
            return node != null &&
                   string.Equals(node.Role, "PreviewFrame", StringComparison.OrdinalIgnoreCase);
        }

        private static bool BelongsToPreviewHost(MbGpPictureNode node, MbDeskNode previewHostNode, float hostX, float hostY)
        {
            if (node == null)
                return false;

            if (previewHostNode != null &&
                node.HostX == previewHostNode.X &&
                node.HostY == previewHostNode.Y)
                return true;

            return Mathf.Approximately(node.HostX, hostX) &&
                   Mathf.Approximately(node.HostY, hostY);
        }

        private void PlacePreviewFrameOverlays(
            RectTransform hostRt,
            MbScene scene,
            MbDeskNode previewHostNode,
            string cashDir,
            float hostX,
            float hostY)
        {
            if (hostRt == null || scene == null)
                return;

            foreach (var gp in scene.Nodes.OfType<MbGpPictureNode>())
            {
                if (!IsPreviewFrameNode(gp))
                    continue;

                if (!BelongsToPreviewHost(gp, previewHostNode, hostX, hostY))
                    continue;

                if (string.IsNullOrWhiteSpace(gp.FileID))
                    continue;

                Sprite sp = LoadG16Sprite(cashDir, gp.FileID, gp.SpriteID);
                if (sp == null)
                {
                    Debug.LogWarning($"[MBattles] Preview overlay sprite missing: file='{gp.FileID}' sprite={gp.SpriteID}");
                    continue;
                }

                float localX = gp.X - hostX;
                float localY = gp.Y - hostY;
                float overlayW = gp.Width > 0 ? gp.Width : sp.rect.width;
                float overlayH = gp.Height > 0 ? gp.Height : sp.rect.height;

                bool isMapar = gp.FileID.IndexOf("mapar", StringComparison.OrdinalIgnoreCase) >= 0;
                float overlayRotation = (isMapar && (gp.SpriteID == 0 || gp.SpriteID == 3)) ? 180f : 0f;

                if (overlayRotation != 0f)
                    PlaceSprCenteredRotated(hostRt, sp, localX, localY, overlayW, overlayH, $"PreviewFrame_{gp.SpriteID}", overlayRotation);
                else
                    PlaceSpr(hostRt, sp, localX, localY, overlayW, overlayH, $"PreviewFrame_{gp.SpriteID}");

                Debug.Log($"[MBattles] Preview overlay file='{gp.FileID}' sprite={gp.SpriteID} local=({localX},{localY}) size={overlayW}x{overlayH} rot={overlayRotation}");
            }
        }

        private void PlaceMapPreview(
            RectTransform parent,
            MbScene scene,
            string cashDir,
            string absPath,
            PreviewMeta meta,
            float fallbackX,
            float fallbackY,
            float fallbackW,
            float fallbackH)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(absPath); } catch { return; }

            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!tex.LoadImage(bytes))
            {
                Debug.LogWarning("[MBattles] LoadImage failed: " + absPath);
                return;
            }

            MbDeskNode previewHostNode = scene != null
                ? scene.Nodes.OfType<MbDeskNode>().FirstOrDefault(IsPreviewHostNode)
                : null;

            float hostX = previewHostNode != null ? previewHostNode.X : fallbackX;
            float hostY = previewHostNode != null ? previewHostNode.Y : fallbackY;
            float hostW = previewHostNode != null && previewHostNode.Width > 0 ? previewHostNode.Width : fallbackW;
            float hostH = previewHostNode != null && previewHostNode.Height > 0 ? previewHostNode.Height : fallbackH;

            var host = new GameObject("PreviewHost", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            host.transform.SetParent(parent, false);
            var hostRt = host.GetComponent<RectTransform>();
            hostRt.anchorMin = hostRt.anchorMax = new Vector2(0, 1);
            hostRt.pivot = new Vector2(0, 1);
            hostRt.anchoredPosition = new Vector2(hostX, -hostY);
            hostRt.sizeDelta = new Vector2(hostW, hostH);

            var hostImg = host.GetComponent<Image>();
            hostImg.color = new Color(0f, 0f, 0f, 0f);
            hostImg.raycastTarget = true;

            var preview = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
            preview.transform.SetParent(host.transform, false);
            var imageRt = preview.GetComponent<RectTransform>();
            imageRt.anchorMin = imageRt.anchorMax = new Vector2(0, 1);
            imageRt.pivot = new Vector2(0, 1);
            imageRt.anchoredPosition = Vector2.zero;
            imageRt.sizeDelta = new Vector2(tex.width, tex.height);

            var image = preview.GetComponent<Image>();
            image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 1), 1f);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            var controller = host.AddComponent<MapPreviewJpgController>();
            controller.Initialize(
                hostRt,
                imageRt,
                meta != null ? meta.CenterX : 0,
                meta != null ? meta.CenterY : 0,
                meta != null ? meta.ScreenSaver : null);

            // The original preview arrows are XML GPPicture children from Interf3\mapar.
            // Do not recreate the old procedural white triangle hints here.
            // CreatePreviewHintArrows(hostRt);
            PlacePreviewFrameOverlays(hostRt, scene, previewHostNode, cashDir, hostX, hostY);

            Debug.Log($"[MBattles] Preview placed at ({hostX},{hostY}) size={hostW}x{hostH} tex={tex.width}x{tex.height}");
        }


        private string ResolveMissionDescriptionText(CoreFileSystem fs, bool showBattles, string selectedId, BattleEntrySimple entry)
        {
            if (!string.IsNullOrWhiteSpace(entry != null ? entry.Description : null))
            {
                Debug.Log($"[MBattles] desc source=entry id='{selectedId}'");
                return entry.Description;
            }

            string catalogText = TryReadMissionDescriptionFromCatalog(fs, selectedId);
            if (!string.IsNullOrWhiteSpace(catalogText))
                return catalogText;

            foreach (string rel in BuildFallbackMissionDescriptionCandidates(selectedId, showBattles))
            {
                string abs = ResolveMissionPathWithFallbackRoots(fs, rel);
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                    continue;

                string loaded = ReadTextSmart(abs);
                if (!string.IsNullOrWhiteSpace(loaded))
                {
                    Debug.Log($"[MBattles] desc source=fallback rel='{rel}' abs='{abs}' len={loaded.Length}");
                    return loaded;
                }
            }

            Debug.LogWarning($"[MBattles] desc source=missing id='{selectedId}'");
            return string.Empty;
        }

        private string TryReadMissionDescriptionFromCatalog(CoreFileSystem fs, string selectedId)
        {
            if (string.IsNullOrWhiteSpace(selectedId))
                return string.Empty;

            var catalog = LoadMissionCatalog(fs);
            if (!catalog.TryGetValue(selectedId, out var meta) || string.IsNullOrWhiteSpace(meta.DescriptionRel))
                return string.Empty;

            string abs = ResolveMissionPathWithFallbackRoots(fs, meta.DescriptionRel);
            if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
            {
                Debug.LogWarning($"[MBattles] desc catalog miss id='{selectedId}' rel='{meta.DescriptionRel}'");
                return string.Empty;
            }

            string loaded = ReadTextSmart(abs);
            if (!string.IsNullOrWhiteSpace(loaded))
            {
                Debug.Log($"[MBattles] desc source=catalog rel='{meta.DescriptionRel}' abs='{abs}' len={loaded.Length}");
                return loaded;
            }

            return string.Empty;
        }

        private IEnumerable<string> BuildFallbackMissionDescriptionCandidates(string selectedId, bool showBattles)
        {
            if (string.IsNullOrWhiteSpace(selectedId))
                yield break;

            if (!showBattles)
            {
                yield return $@"Missions\Skirmish\{selectedId}.txt";
                yield break;
            }

            yield return $@"Missions\Battles\{selectedId}.txt";

            if (selectedId.StartsWith("HBattle", StringComparison.OrdinalIgnoreCase))
                yield return $@"Missions\Battles\{selectedId.Replace("HBattle", "Hbattle")}.txt";
        }

        private string ResolveMissionPathWithFallbackRoots(CoreFileSystem fs, string relPath)
        {
            foreach (string candidate in EnumerateMissionPathCandidates(fs, relPath))
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return string.Empty;
        }

        private IEnumerable<string> EnumerateMissionPathCandidates(CoreFileSystem fs, string relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath))
                yield break;

            string normalized = relPath.Replace('/', Path.DirectorySeparatorChar)
                                       .Replace('\\', Path.DirectorySeparatorChar)
                                       .TrimStart(Path.DirectorySeparatorChar);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool TryAdd(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                string full = path;
                try { full = Path.GetFullPath(path); } catch { }

                if (!seen.Add(full))
                    return false;

                return true;
            }

            string direct = "";
            try { direct = fs.ResolvePath(relPath); } catch { direct = ""; }
            if (TryAdd(direct))
                yield return direct;

            string dataRoot = fs != null ? (fs.DataRoot ?? "") : "";
            string gameRoot = "";
            try { gameRoot = Directory.GetParent(dataRoot)?.FullName ?? ""; } catch { gameRoot = ""; }

            if (!string.IsNullOrWhiteSpace(gameRoot))
            {
                string data1 = Path.Combine(gameRoot, "Data1", normalized);
                if (TryAdd(data1))
                    yield return data1;

                string data = Path.Combine(gameRoot, "Data", normalized);
                if (TryAdd(data))
                    yield return data;
            }

            foreach (string exact in seen.ToArray())
            {
                string dir = "";
                string file = "";
                try
                {
                    dir = Path.GetDirectoryName(exact) ?? "";
                    file = Path.GetFileName(exact) ?? "";
                }
                catch { }

                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(file) || !Directory.Exists(dir))
                    continue;

                string found = "";
                try
                {
                    found = Directory.GetFiles(dir)
                        .FirstOrDefault(f => string.Equals(Path.GetFileName(f), file, StringComparison.OrdinalIgnoreCase)) ?? "";
                }
                catch { found = ""; }

                if (TryAdd(found))
                    yield return found;
            }
        }


        // ─────────────────────────────────────────────────────────────────────
        // DESCRIPTION SCROLL AREA
        // Original: TextViewer x=605,y=445 w=342 h=226
        //           VScrollBar  x=957,y=434 h=238
        // ─────────────────────────────────────────────────────────────────────



private void PlaceDescriptionScroll(RectTransform parent, string cashDir,
            LocDb loc, float deskX, float deskY, float deskW, float deskH, string text)
        {
            const float viewInsetL = 11f;
            const float viewInsetT = 6f;
            const float viewW = 340f;
            const float viewH = 167f;

            const float sbWidth = 15f;
            const float sbX = 351f;
            const float sbY = 0f;
            const float sbH = 214f;

            string raw = text ?? string.Empty;
            string rich = FormatDescriptionText(raw);
            string plain = StripEngineTagsToPlainText(raw);
            if (string.IsNullOrWhiteSpace(plain))
                plain = "Описание отсутствует.";

            var host = new GameObject("DescScroll", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var hrt = host.GetComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = new Vector2(0, 1);
            hrt.pivot = new Vector2(0, 1);
            hrt.anchoredPosition = new Vector2(deskX, -deskY);
            hrt.sizeDelta = new Vector2(deskW, deskH);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(host.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = vpRt.anchorMax = new Vector2(0, 1);
            vpRt.pivot = new Vector2(0, 1);
            vpRt.anchoredPosition = new Vector2(viewInsetL, -viewInsetT);
            vpRt.sizeDelta = new Vector2(viewW, viewH);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
            crt.pivot = new Vector2(0, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(viewW, viewH);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(content.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, 1);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(viewW, viewH);

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = 14f;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color32(0x2E, 0x23, 0x17, 0xFF);
            tmp.lineSpacing = -2f;
            tmp.margin = Vector4.zero;
            tmp.raycastTarget = false;
            tmp.text = string.IsNullOrWhiteSpace(rich) ? plain : rich;
            tmp.ForceMeshUpdate();

            if (tmp.textInfo == null || tmp.textInfo.characterCount == 0)
            {
                tmp.richText = false;
                tmp.text = plain;
                tmp.ForceMeshUpdate();
            }

            Canvas.ForceUpdateCanvases();
            float preferred = Mathf.Max(viewH, tmp.preferredHeight + 6f);
            trt.sizeDelta = new Vector2(viewW, preferred);
            crt.sizeDelta = new Vector2(viewW, preferred);

            var scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGo.transform.SetParent(host.transform, false);
            var sbRt = scrollbarGo.GetComponent<RectTransform>();
            sbRt.anchorMin = sbRt.anchorMax = new Vector2(0, 1);
            sbRt.pivot = new Vector2(0, 1);
            sbRt.anchoredPosition = new Vector2(sbX + DescScrollbarOffsetX, -(sbY + DescScrollbarOffsetY));
            sbRt.sizeDelta = new Vector2(sbWidth, sbH);

            Sprite trackSp = LoadG16Sprite(cashDir, "Interf3_elements_scroll3", 5)
                             ?? LoadG16Sprite(cashDir, "Interf3_elements_scroll3", 6)
                             ?? LoadG16Sprite(cashDir, "Interf3_elements_scroll3", 7);
            var sbImg = scrollbarGo.GetComponent<Image>();
            if (trackSp != null)
            {
                sbImg.sprite = trackSp;
                sbImg.type = Image.Type.Sliced;
            }
            else sbImg.color = new Color32(90, 75, 60, 100);

            Sprite upSp = LoadG16Sprite(cashDir, "Interf3_elements_scroll3", 0);
            Sprite downSp = LoadG16Sprite(cashDir, "Interf3_elements_scroll3", 2);
            Sprite handleSp = LoadG16Sprite(cashDir, "Interf3_elements_scroll3", 4);

            float arrowH = upSp != null ? upSp.rect.height : 18f;
            float thumbH = handleSp != null ? handleSp.rect.height : 45f;
            float slideH = Mathf.Max(8f, sbH - arrowH * 2f);
            thumbH = Mathf.Clamp(thumbH + DescScrollbarThumbHeightAdjust, 10f, slideH - 2f);

            var upGo = new GameObject("ArrowUp", typeof(RectTransform), typeof(Image), typeof(Button));
            upGo.transform.SetParent(scrollbarGo.transform, false);
            var upRt = upGo.GetComponent<RectTransform>();
            upRt.anchorMin = new Vector2(0, 1);
            upRt.anchorMax = new Vector2(1, 1);
            upRt.pivot = new Vector2(0.5f, 1f);
            upRt.anchoredPosition = Vector2.zero;
            upRt.sizeDelta = new Vector2(0f, arrowH);
            var upImg = upGo.GetComponent<Image>();
            if (upSp != null) { upImg.sprite = upSp; upImg.type = Image.Type.Simple; upImg.preserveAspect = false; }
            else upImg.color = new Color32(140, 90, 70, 255);

            var downGo = new GameObject("ArrowDown", typeof(RectTransform), typeof(Image), typeof(Button));
            downGo.transform.SetParent(scrollbarGo.transform, false);
            var downRt = downGo.GetComponent<RectTransform>();
            downRt.anchorMin = new Vector2(0, 0);
            downRt.anchorMax = new Vector2(1, 0);
            downRt.pivot = new Vector2(0.5f, 0f);
            downRt.anchoredPosition = Vector2.zero;
            downRt.sizeDelta = new Vector2(0f, arrowH);
            var downImg = downGo.GetComponent<Image>();
            if (downSp != null) { downImg.sprite = downSp; downImg.type = Image.Type.Simple; downImg.preserveAspect = false; }
            else downImg.color = new Color32(140, 90, 70, 255);

            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform), typeof(RectMask2D));
            slidingArea.transform.SetParent(scrollbarGo.transform, false);
            var saRt = slidingArea.GetComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(0f, arrowH);
            saRt.offsetMax = new Vector2(0f, -arrowH);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(slidingArea.transform, false);
            var hRt = handleGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, DescScrollbarThumbOffsetY);
            hRt.sizeDelta = new Vector2(0f, thumbH);
            var hImg = handleGo.GetComponent<Image>();
            if (handleSp != null)
            {
                hImg.sprite = handleSp;
                hImg.type = Image.Type.Sliced;
            }
            else hImg.color = new Color32(150, 120, 90, 220);

            var sb = scrollbarGo.GetComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.targetGraphic = hImg;
            sb.handleRect = hRt;
            sb.value = 1f;

            upGo.GetComponent<Button>().onClick.AddListener(() => sb.value = Mathf.Clamp01(sb.value + 0.12f));
            downGo.GetComponent<Button>().onClick.AddListener(() => sb.value = Mathf.Clamp01(sb.value - 0.12f));

            var scrollRect = host.AddComponent<ScrollRect>();
            scrollRect.viewport = vpRt;
            scrollRect.content = crt;
            scrollRect.horizontal = false;
            scrollRect.vertical = preferred > viewH + 1f;
            scrollRect.scrollSensitivity = 18f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = sb;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = 1f;

            Debug.Log($"[MBattles] desc render rawLen={raw.Length} richLen={rich.Length} plainLen={plain.Length} chars={tmp.textInfo.characterCount} preferred={preferred:0.0} mode={(tmp.richText ? "rich" : "plain")}");
        }

        private static string FormatDescriptionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string s = text
                .Replace("\0", string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            s = s.Replace("\\\n", "\n");
            s = s.Replace("\n\\", "\n");
            s = s.Trim();

            var rx = new Regex(@"\{([^}]*)\}");
            var matches = rx.Matches(s);

            var sb = new System.Text.StringBuilder(s.Length * 2);
            int last = 0;
            int currentSize = 14;
            string currentColor = "2E2317";

            foreach (Match m in matches)
            {
                if (m.Index > last)
                    AppendStyledDescriptionText(sb, s.Substring(last, m.Index - last), currentSize, currentColor);

                string token = (m.Groups[1].Value ?? string.Empty).Trim();
                if (token.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                {
                    string colorHex = ExtractEngineColorHex(token);
                    if (!string.IsNullOrWhiteSpace(colorHex))
                        currentColor = colorHex;
                }
                else if (token.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                {
                    currentSize = ResolveEngineFontSize(token.Substring(1));
                }

                last = m.Index + m.Length;
            }

            if (last < s.Length)
                AppendStyledDescriptionText(sb, s.Substring(last), currentSize, currentColor);

            return sb.ToString();
        }

        private static void AppendStyledDescriptionText(System.Text.StringBuilder sb, string raw, int size, string colorHex)
        {
            if (string.IsNullOrEmpty(raw))
                return;

            string escaped = EscapeTmpText(raw).Replace("\n", "<br>");
            if (string.IsNullOrEmpty(escaped))
                return;

            string resolvedColor = NormalizeDescriptionColor(colorHex);
            bool accent = string.Equals(resolvedColor, "640100", StringComparison.OrdinalIgnoreCase);

            sb.Append("<size=").Append(size).Append("><color=#").Append(resolvedColor).Append(">");
            if (accent)
                sb.Append("<b>");
            sb.Append(escaped);
            if (accent)
                sb.Append("</b>");
            sb.Append("</color></size>");
        }

        private static string NormalizeDescriptionColor(string colorHex)
        {
            if (string.IsNullOrWhiteSpace(colorHex))
                return "2E2317";

            string c = colorHex.Trim().ToUpperInvariant();
            if (c.Length == 8)
                c = c.Substring(2);

            if (c == "640100")
                return "640100";
            if (c == "707070")
                return "707070";
            if (c == "100801")
                return "100801";

            return Regex.IsMatch(c, "^[0-9A-F]{6}$") ? c : "2E2317";
        }

        private static string StripEngineTagsToPlainText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string s = text
                .Replace("\0", string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            s = s.Replace("\\\n", "\n");
            s = s.Replace("\n\\", "\n");
            s = Regex.Replace(s, @"\{[^}]*\}", string.Empty);
            s = s.Trim();

            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            return s;
        }

        private static string ExtractEngineColorHex(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            string payload = token.Length > 1 ? token.Substring(1).Trim() : string.Empty;
            if (payload.Length == 0)
                return null;

            payload = payload.Replace(" ", string.Empty);
            if (payload.Length == 8)
                payload = payload.Substring(2);
            if (payload.Length != 6)
                return null;

            return Regex.IsMatch(payload, "^[0-9A-Fa-f]{6}$") ? payload.ToUpperInvariant() : null;
        }

        private static int ResolveEngineFontSize(string fontToken)
        {
            if (string.IsNullOrWhiteSpace(fontToken))
                return 14;

            string token = fontToken.Trim().ToUpperInvariant();
            if (token == "S")
                return 10;
            if (token == "C12")
                return 12;
            if (token == "C10")
                return 10;
            if (token == "C14" || token == "G14")
                return 14;
            if (token == "G16")
                return 16;

            var m = Regex.Match(token, @"(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int parsed))
                return Mathf.Clamp(parsed, 8, 24);

            return 14;
        }

        private static string EscapeTmpText(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return s
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }


// ─────────────────────────────────────────────────────────────────────
        // COMBO BOX (Arcade Mode)
        // ─────────────────────────────────────────────────────────────────────
        private void PlacePseudoCombo(RectTransform parent, string cashDir,
            float x, float y, float w, float h, string displayText, string actionName, IUiActionSink sink)
        {
            Sprite boxSp = LoadComboFrameSprite(0) ?? LoadG16Sprite(cashDir, "Interf3_elements_combo", 0);
            Sprite rowSp = LoadComboFrameSprite(5) ?? boxSp;
            Sprite rowHoverSp = LoadComboFrameSprite(6) ?? rowSp;

            var host = new GameObject("ArcadeComboHost", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Button));
            host.transform.SetParent(parent, false);
            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var hostImg = host.GetComponent<Image>();
            hostImg.color = new Color(1f, 1f, 1f, 0.01f);
            hostImg.raycastTarget = true;

            if (boxSp != null)
            {
                var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
                visual.transform.SetParent(host.transform, false);
                var vrt = visual.GetComponent<RectTransform>();
                vrt.anchorMin = new Vector2(0, 1);
                vrt.anchorMax = new Vector2(0, 1);
                vrt.pivot = new Vector2(0, 1);
                vrt.anchoredPosition = Vector2.zero;
                vrt.sizeDelta = new Vector2(boxSp.rect.width, boxSp.rect.height);
                var vimg = visual.GetComponent<Image>();
                vimg.sprite = boxSp;
                vimg.type = Image.Type.Simple;
                vimg.preserveAspect = false;
                vimg.raycastTarget = false;
            }

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(host.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(25, 1);
            textRt.offsetMax = new Vector2(-4, -1);
            var label = textGo.GetComponent<TextMeshProUGUI>();
            label.font = LoadFont();
            label.fontSize = 14f;
            label.text = displayText;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color32(40, 30, 20, 255);
            label.raycastTarget = false;

            var blocker = new GameObject("ArcadeComboBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
            blocker.transform.SetParent(parent, false);
            var brt = blocker.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var bimg = blocker.GetComponent<Image>();
            bimg.color = new Color(0f, 0f, 0f, 0f);
            bimg.raycastTarget = true;
            blocker.SetActive(false);

            var popup = new GameObject("ArcadeComboPopup", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            popup.transform.SetParent(parent, false);
            var popupRt = popup.GetComponent<RectTransform>();
            popupRt.anchorMin = popupRt.anchorMax = new Vector2(0, 1);
            popupRt.pivot = new Vector2(0, 1);
            popupRt.anchoredPosition = new Vector2(x, -(y + h));
            popupRt.sizeDelta = new Vector2(w, h * 2f);
            var popupImg = popup.GetComponent<Image>();
            popupImg.color = new Color(1f, 1f, 1f, 0.01f);
            popupImg.raycastTarget = true;
            popup.SetActive(false);

            CreateComboOption(popup.transform, rowSp, rowHoverSp, 0f, 0f, w, h, "Включен", actionName, sink, true, popup, label, blocker);
            CreateComboOption(popup.transform, rowSp, rowHoverSp, 0f, h,  w, h, "Выключен", actionName, sink, false, popup, label, blocker);

            void ClosePopup()
            {
                popup.SetActive(false);
                blocker.SetActive(false);
            }

            host.GetComponent<Button>().onClick.AddListener(() =>
            {
                bool next = !popup.activeSelf;
                popup.SetActive(next);
                blocker.SetActive(next);
                if (next)
                {
                    blocker.transform.SetAsLastSibling();
                    popup.transform.SetAsLastSibling();
                    host.transform.SetAsLastSibling();
                }
            });

            blocker.GetComponent<Button>().onClick.AddListener(ClosePopup);

            Debug.Log($"[MBattles] Exact combo host=({x},{y},{w},{h}) boxSprite={(boxSp != null ? $"{boxSp.rect.width}x{boxSp.rect.height}" : "null")} rowSprite={(rowSp != null ? $"{rowSp.rect.width}x{rowSp.rect.height}" : "null")} text='{displayText}'");
        }

        private void CreateComboOption(Transform parent, Sprite rowSp, Sprite rowHoverSp, float x, float y, float w, float h,
            string title, string actionName, IUiActionSink sink, bool targetEnabled, GameObject popup, TextMeshProUGUI hostLabel, GameObject blocker)
        {
            var optionHost = new GameObject("OptionHost_" + title, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Button));
            optionHost.transform.SetParent(parent, false);
            var rt = optionHost.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var hostImg = optionHost.GetComponent<Image>();
            hostImg.color = new Color(1f, 1f, 1f, 0.01f);
            hostImg.raycastTarget = true;

            var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(optionHost.transform, false);
            var vrt = visual.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0, 1);
            vrt.anchorMax = new Vector2(0, 1);
            vrt.pivot = new Vector2(0, 1);
            vrt.anchoredPosition = Vector2.zero;
            if (rowSp != null)
                vrt.sizeDelta = new Vector2(rowSp.rect.width, rowSp.rect.height);
            else
                vrt.sizeDelta = new Vector2(w, h);

            var vimg = visual.GetComponent<Image>();
            if (rowSp != null)
            {
                vimg.sprite = rowSp;
                vimg.type = Image.Type.Simple;
                vimg.preserveAspect = false;
            }
            else
            {
                vimg.color = new Color32(232, 222, 203, 255);
            }
            vimg.raycastTarget = false;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(optionHost.transform, false);
            var trt2 = txtGo.GetComponent<RectTransform>();
            trt2.anchorMin = Vector2.zero;
            trt2.anchorMax = Vector2.one;
            trt2.offsetMin = new Vector2(25, 1);
            trt2.offsetMax = new Vector2(-4, -1);
            var tmp2 = txtGo.GetComponent<TextMeshProUGUI>();
            tmp2.font = LoadFont();
            tmp2.fontSize = 14f;
            tmp2.text = title;
            tmp2.alignment = TextAlignmentOptions.MidlineLeft;
            tmp2.color = new Color32(40, 30, 20, 255);
            tmp2.raycastTarget = false;

            var colors = optionHost.GetComponent<Button>().colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            optionHost.GetComponent<Button>().colors = colors;

            var hover = optionHost.AddComponent<RowHoverSwapExact>();
            hover.Image = vimg;
            hover.Normal = rowSp;
            hover.Hover = rowHoverSp ?? rowSp;

            optionHost.GetComponent<Button>().onClick.AddListener(() =>
            {
                bool current = MenuActionSink.SingleBattlesArcadeModeEnabled;
                hostLabel.text = title;
                popup.SetActive(false);
                blocker.SetActive(false);
                if (current != targetEnabled)
                    sink.OnAction(title, new UiAction { Name = actionName });
            });
        }

        private Sprite LoadComboFrameSprite(int frameIndex)
        {
            string key = $"Interf3_elements_combo_frames/frame_{frameIndex:0000}";
            var sp = Resources.Load<Sprite>(key);
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>(key);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 1), 1f);
        }

        private sealed class RowHoverSwapExact : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Image;
            public Sprite Normal;
            public Sprite Hover;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Image != null) Image.sprite = Hover ?? Normal;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (Image != null) Image.sprite = Normal;
            }
        }

        private Sprite CropSpriteLeft(Sprite src, float widthPx)
        {
            if (src == null || src.texture == null) return null;

            var rect = src.rect;
            float cropW = Mathf.Clamp(widthPx, 1f, rect.width);
            var newRect = new Rect(rect.x, rect.y, cropW, rect.height);
            return Sprite.Create(
                src.texture,
                newRect,
                new Vector2(0f, 0.5f),
                src.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                src.border
            );
        }

        private Sprite CropSpriteTop(Sprite src, float heightPx)
        {
            if (src == null || src.texture == null) return null;

            var rect = src.rect;
            float cropH = Mathf.Clamp(heightPx, 1f, rect.height);
            float y = rect.y + (rect.height - cropH);
            var newRect = new Rect(rect.x, y, rect.width, cropH);
            return Sprite.Create(
                src.texture,
                newRect,
                new Vector2(0f, 1f),
                src.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                src.border
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // TAB BUTTON
        // ─────────────────────────────────────────────────────────────────────
        private void PlaceTabButton(RectTransform parent, string cashDir,
            string label, float x, float y, float w, float h,
            bool active, string actionName, IUiActionSink sink)
        {
            // Use vbuttons for tabs (0=passive, 1=active)
            int sprIdx = active ? 1 : 0;
            Sprite sp = LoadG16Sprite(cashDir, "interf3_elements_vbuttons", sprIdx);

            var go = new GameObject("Tab_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            else img.color = active ? new Color32(200, 180, 140, 200) : new Color32(160, 145, 115, 180);

            go.GetComponent<Button>().onClick.AddListener(() =>
                sink.OnAction(label, new UiAction { Name = actionName }));

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = 14f;
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = active ? new Color32(255, 230, 150, 255) : new Color32(200, 185, 140, 255);
        }

        // ─────────────────────────────────────────────────────────────────────
        // BOTTOM BUTTONS / ORIGINAL STYLE
        // ─────────────────────────────────────────────────────────────────────
        private RectTransform CreateTiledStrip(RectTransform parent,
            string objectName,
            string cashDir,
            string fileId,
            int baseFrame,
            float x,
            float y,
            float w,
            float h)
        {
            Sprite spL = LoadG16Sprite(cashDir, fileId, baseFrame + 0);
            Sprite spR = LoadG16Sprite(cashDir, fileId, baseFrame + 1);
            Sprite spC1 = LoadG16Sprite(cashDir, fileId, baseFrame + 2);
            Sprite spC2 = LoadG16Sprite(cashDir, fileId, baseFrame + 3);
            Sprite spC3 = LoadG16Sprite(cashDir, fileId, baseFrame + 4);

            if (spL == null && spC1 != null) spL = spC1;
            if (spR == null && spC1 != null) spR = spC1;
            if (spC1 == null) spC1 = spL ?? spR;
            if (spC2 == null) spC2 = spC1;
            if (spC3 == null) spC3 = spC1;
            if (spL == null || spR == null || spC1 == null)
                return null;

            TryForcePointFiltering(spL);
            TryForcePointFiltering(spR);
            TryForcePointFiltering(spC1);
            TryForcePointFiltering(spC2);
            TryForcePointFiltering(spC3);

            float nativeH = Mathf.Max(1f, Mathf.Max(spL.rect.height, Mathf.Max(spR.rect.height, Mathf.Max(spC1.rect.height, Mathf.Max(spC2.rect.height, spC3.rect.height)))));
            float widthL = Mathf.Max(1f, spL.rect.width);
            float widthR = Mathf.Max(1f, spR.rect.width);

            var rootGo = new GameObject(objectName, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(RectMask2D));
            maskGo.transform.SetParent(rt, false);
            var maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;

            float stripY = Mathf.Round((h - nativeH) * 0.5f);

            void AddTile(string name, Sprite sprite, float px)
            {
                if (sprite == null)
                    return;

                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(maskRt, false);

                var srt = go.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
                srt.pivot = new Vector2(0, 1);
                srt.anchoredPosition = new Vector2(px, -stripY);
                srt.sizeDelta = new Vector2(Mathf.Max(1f, sprite.rect.width), Mathf.Max(1f, sprite.rect.height));

                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
                img.color = Color.white;
            }

            Sprite[] centerSprites = { spC1, spC2, spC3 };
            float xPos = 0f;
            int tileIndex = 0;
            while (xPos < w && tileIndex < 512)
            {
                Sprite tileSp = centerSprites[tileIndex % 3] ?? spC1;
                if (tileSp == null)
                    break;

                AddTile("Tile_" + tileIndex, tileSp, xPos);
                xPos += Mathf.Max(1f, tileSp.rect.width);
                tileIndex++;
            }

            AddTile("EdgeL", spL, 0f);
            AddTile("EdgeR", spR, Mathf.Max(0f, w - widthR));
            return rt;
        }

        private RectTransform CreateBottomPassiveOverlay(RectTransform parent, string cashDir, float w, float h)
        {
            Sprite sp = LoadG16Sprite(cashDir, @"Interf3\elements\buttons", 0);
            if (sp == null)
                return null;

            TryForcePointFiltering(sp);

            float spW = Mathf.Max(1f, sp.rect.width);
            float spH = Mathf.Max(1f, sp.rect.height);

            var rootGo = new GameObject("PassiveVisual", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(spW, spH);

            var img = rootGo.AddComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            img.color = Color.white;
            return rt;
        }

        private RectTransform CreateBottomHoverOverlay(RectTransform parent, string cashDir, float w, float h)
        {
            Sprite sp = LoadG16Sprite(cashDir, @"Interf3\elements\buttons", 5);
            if (sp == null)
                return null;

            TryForcePointFiltering(sp);

            float spW = Mathf.Max(1f, sp.rect.width);
            float spH = Mathf.Max(1f, sp.rect.height);

            var rootGo = new GameObject("HoverVisual", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(spW, spH);

            var img = rootGo.AddComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            img.color = Color.white;
            return rt;
        }

        private void PlaceBottomButtonBar(RectTransform parent, string cashDir)
        {
            Sprite sp = LoadG16Sprite(cashDir, @"INTERF3\ELEMENTS\BUTTON_BACK", 1);
            if (sp != null)
            {
                PlaceSprCenteredFlipped(parent, sp, 0f, 680f, 837f, 88f, "BottomButtonsBar", false, true);
                PlaceSprCenteredFlipped(parent, sp, 0f, 680f, 837f, 88f, "BottomButtonsBar_Overlay", false, true);
                return;
            }

            var go = new GameObject("BottomButtonsBarFallback", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0f, -680f);
            rt.sizeDelta = new Vector2(837f, 88f);
            go.GetComponent<Image>().color = new Color32(96, 36, 28, 255);
        }

        private void PlaceBottomButton(RectTransform parent, string cashDir,
            string label, float x, float y, float w, float h,
            bool enabled, string actionName, string payload, IUiActionSink sink)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(OriginalBottomButtonState));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.01f);
            hit.raycastTarget = true;

            RectTransform passiveStrip = CreateBottomPassiveOverlay(rt, cashDir, w, h);
            RectTransform hoverStrip = CreateBottomHoverOverlay(rt, cashDir, w, h);
            if (passiveStrip != null)
                passiveStrip.gameObject.SetActive(true);
            if (hoverStrip != null)
                hoverStrip.gameObject.SetActive(false);

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 1.5f);
            trt.sizeDelta = new Vector2(w, h);

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = 21f;
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color32(242, 225, 164, 255);
            tmp.raycastTarget = false;
#pragma warning disable CS0618
            tmp.enableWordWrapping = false;
#pragma warning restore CS0618
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            if (!string.IsNullOrWhiteSpace(actionName))
            {
                btn.onClick.AddListener(() =>
                    sink.OnAction(label, new UiAction { Name = actionName, Payload = payload }));
            }

            var state = go.GetComponent<OriginalBottomButtonState>();
            state.Button = btn;
            state.PassiveVisual = passiveStrip != null ? passiveStrip.gameObject : null;
            state.HoverVisual = hoverStrip != null ? hoverStrip.gameObject : null;
            state.Label = tmp;
            state.PassiveColor = new Color32(242, 225, 164, 255);
            state.HoverColor = new Color32(255, 255, 255, 255);
            state.DisabledColor = new Color32(148, 148, 148, 255);
            state.Interactable = enabled;
            state.ApplyState();
        }

        private void PlaceSingleLineText(RectTransform parent, string text,
            float x, float y, float w, float h, float fontSize,
            Color color, TextAlignmentOptions align, bool bold = false)
        {
            var go = new GameObject("TxtSingle", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            float anchorX = (align == TextAlignmentOptions.Center) ? x - w * 0.5f : x;
            rt.anchoredPosition = new Vector2(anchorX, -y);
            rt.sizeDelta = new Vector2(w, h);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = fontSize;
            tmp.text = text ?? string.Empty;
            tmp.alignment = align;
            tmp.color = color;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
#pragma warning disable CS0618
            tmp.enableWordWrapping = false;
#pragma warning restore CS0618
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private void PlaceBitmap(RectTransform parent, string absPath,
            float x, float y, float w, float h)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(absPath); } catch { return; }
            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!tex.LoadImage(bytes)) return;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 1), 1f);
            var go = new GameObject("BG", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        private void PlaceText(RectTransform parent, string text,
            float x, float y, float w, float h, float fontSize,
            Color color, TextAlignmentOptions align, bool bold = false)
        {
            var go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            // x is the center point for centered text, left edge for left-aligned
            float anchorX = (align == TextAlignmentOptions.Center) ? x - w * 0.5f : x;
            rt.anchoredPosition = new Vector2(anchorX, -y);
            rt.sizeDelta = new Vector2(w, h);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = fontSize;
            tmp.text = text;
            tmp.alignment = align;
            tmp.color = color;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }

        private void PlaceSpr(RectTransform parent, Sprite sp,
            float x, float y, float w, float h, string name)
        {
            if (sp == null || w <= 0 || h <= 0) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
        }

        private void PlaceSprCenteredFlipped(RectTransform parent, Sprite sp,
            float x, float y, float w, float h, string name, bool flipX, bool flipY)
        {
            if (sp == null || w <= 0 || h <= 0) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rt.sizeDelta = new Vector2(w, h);
            rt.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
        }

        private void PlaceSprCenteredRotated(RectTransform parent, Sprite sp,
            float x, float y, float w, float h, string name, float rotationZ)
        {
            if (sp == null || w <= 0 || h <= 0) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rt.sizeDelta = new Vector2(w, h);
            rt.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
        }

        private void PlaceSprTiled(RectTransform parent, Sprite sp,
            float x, float y, float w, float h, bool horizontal)
        {
            if (sp == null || w <= 0 || h <= 0) return;
            int step = horizontal ? Mathf.Max(1, (int)sp.rect.width) : Mathf.Max(1, (int)sp.rect.height);
            int count = Mathf.CeilToInt(horizontal ? w / step : h / step);
            for (int i = 0; i < count; i++)
            {
                float px = horizontal ? x + i * step : x;
                float py = horizontal ? y : y + i * step;
                float pw = horizontal ? Mathf.Min(step, x + w - px) : w;
                float ph = horizontal ? h : Mathf.Min(step, y + h - py);
                PlaceSpr(parent, sp, px, py, pw, ph, $"T{i}");
            }
        }

        private static TMP_FontAsset LoadFont()
        {
            var f = Resources.Load<TMP_FontAsset>("Fonts/Slovic");
            return f ?? TMP_Settings.defaultFontAsset;
        }


        // ─────────────────────────────────────────────────────────────────────
        // LOAD BATTLE ENTRIES (original order from SingleMiss / SingleBatl)
        // ─────────────────────────────────────────────────────────────────────

        private Dictionary<string, string> LoadMissionDisplayNameCatalog(CoreFileSystem fs)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string[] candidates =
            {
                @"Missions\Text\Skirbatlnames.txt",
                @"Text\Skirbatlnames.txt"
            };

            foreach (string rel in candidates)
            {
                string abs = "";
                try { abs = fs.ResolvePath(rel); } catch { abs = ""; }
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                    continue;

                string catalogText = ReadTextSmart(abs);
                foreach (string rawLine in catalogText.Replace("\r", "").Split('\n'))
                {
                    string line = (rawLine ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    int split = line.IndexOf(' ');
                    if (split <= 1)
                        split = line.IndexOf('\t');
                    if (split <= 1)
                        continue;

                    string key = line.Substring(1, split - 1).Trim();
                    string value = line.Substring(split + 1).Trim();
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                        continue;

                    map[key] = value;

                    if (key.EndsWith("_TXT", StringComparison.OrdinalIgnoreCase))
                        map[key.Substring(0, key.Length - 4)] = value;
                }

                if (map.Count > 0)
                    break;
            }

            return map;
        }

        private string ResolveMissionDisplayName(
            LocDb loc,
            Dictionary<string, string> displayCatalog,
            string displayKey,
            string fallbackKey)
        {
            string[] candidates =
            {
                displayKey ?? "",
                (displayKey ?? "").TrimStart('#'),
                fallbackKey ?? "",
                (fallbackKey ?? "") + "_TXT"
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                if (displayCatalog != null && displayCatalog.TryGetValue(candidate.TrimStart('#'), out string mapped) && !string.IsNullOrWhiteSpace(mapped))
                    return mapped.Trim();

                if (loc != null)
                {
                    string resolved = loc.Resolve(candidate);
                    if (!string.IsNullOrWhiteSpace(resolved) &&
                        !resolved.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
                        !resolved.StartsWith("#", StringComparison.Ordinal))
                    {
                        return resolved.Trim();
                    }
                }
            }

            return fallbackKey;
        }

        private sealed class MissionCatalogEntry
        {
            public string MissionKey = "";
            public string DisplayKey = "";
            public string DescriptionRel = "";
            public string PreviewRel = "";
        }

        private List<BattleEntrySimple> LoadEntries(
            CoreFileSystem fs, LocDb loc, bool showBattles)
        {
            var list = new List<BattleEntrySimple>();
            var catalog = LoadMissionCatalog(fs);
            var displayCatalog = LoadMissionDisplayNameCatalog(fs);

            string orderRel = showBattles ? @"Missions\SingleBatl.txt" : @"Missions\SingleMiss.txt";
            string orderAbs = "";
            try { orderAbs = fs.ResolvePath(orderRel); } catch { orderAbs = ""; }

            var orderedIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(orderAbs) && File.Exists(orderAbs))
            {
                foreach (string raw in File.ReadAllLines(orderAbs))
                {
                    string s = (raw ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    if (Regex.IsMatch(s, @"^\d+$")) continue;
                    if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);
                    orderedIds.Add(s);
                }
            }

            foreach (string key in orderedIds)
            {
                if (!catalog.TryGetValue(key, out var meta))
                    continue;

                string display = ResolveMissionDisplayName(loc, displayCatalog, meta.DisplayKey, key);
                string desc = "";
                if (!string.IsNullOrWhiteSpace(meta.DescriptionRel))
                {
                    try
                    {
                        string abs = fs.ResolvePath(meta.DescriptionRel);
                        if (File.Exists(abs))
                            desc = ReadTextSmart(abs);
                    }
                    catch { }
                }

                string previewAbs = "";
                if (!string.IsNullOrWhiteSpace(meta.PreviewRel))
                {
                    try
                    {
                        string abs = fs.ResolvePath(meta.PreviewRel);
                        if (File.Exists(abs))
                            previewAbs = abs;
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(display) || display.StartsWith("#") || display.Equals(key, StringComparison.OrdinalIgnoreCase))
                    display = ExtractDisplayNameFromMissionText(desc, key);

                list.Add(new BattleEntrySimple
                {
                    Id = key,
                    DisplayName = display,
                    Description = desc,
                    PreviewPath = previewAbs
                });
            }

            if (list.Count == 0)
            {
                string relDir = showBattles ? @"Missions\Battles" : @"Missions\Skirmish";
                string absDir = fs.ResolvePath(relDir);
                if (Directory.Exists(absDir))
                {
                    var txts = Directory.GetFiles(absDir, "*.txt");
                    Array.Sort(txts, NaturalCompare);
                    foreach (var f in txts)
                    {
                        string id = Path.GetFileNameWithoutExtension(f);
                        string display = ResolveMissionDisplayName(loc, displayCatalog, "#" + id + "_TXT", id);
                        list.Add(new BattleEntrySimple
                        {
                            Id = id,
                            DisplayName = display,
                            Description = ReadTextSmart(f),
                            PreviewPath = FindPreview(absDir, id)
                        });
                    }
                }
            }

            return list;
        }


        private static string ExtractDisplayNameFromMissionText(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            string s = raw.Replace("\r", "");
            int slash = s.IndexOf('\\');
            if (slash >= 0)
                s = s.Substring(0, slash);

            s = Regex.Replace(s, @"\{[^}]*\}", "");
            s = s.Trim();
            return string.IsNullOrWhiteSpace(s) ? fallback : s;
        }

        private Dictionary<string, MissionCatalogEntry> LoadMissionCatalog(CoreFileSystem fs)
        {
            var map = new Dictionary<string, MissionCatalogEntry>(StringComparer.OrdinalIgnoreCase);
            string missionsAbs = "";
            try { missionsAbs = fs.ResolvePath(@"Missions\missions.txt"); } catch { missionsAbs = ""; }
            if (string.IsNullOrWhiteSpace(missionsAbs) || !File.Exists(missionsAbs))
                return map;

            string text = ReadTextSmart(missionsAbs);
            if (string.IsNullOrWhiteSpace(text))
                return map;

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length - 6; i++)
            {
                string key = (lines[i] ?? "").Trim();
                if (!key.StartsWith("#", StringComparison.Ordinal))
                    continue;

                string descRel = (lines[i + 4] ?? "").Trim();
                if (descRel.IndexOf("Missions\\Skirmish\\", StringComparison.OrdinalIgnoreCase) < 0 &&
                    descRel.IndexOf("Missions\\Battles\\", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string missionKey = key.Substring(1);
                var meta = new MissionCatalogEntry
                {
                    MissionKey = missionKey,
                    DisplayKey = (lines[i + 1] ?? "").Trim(),
                    DescriptionRel = descRel
                };

                for (int j = i + 1; j < Math.Min(lines.Length, i + 12); j++)
                {
                    if (TryParsePreviewSpec(lines[j], out string relPath, out _, out _))
                    {
                        meta.PreviewRel = relPath;
                        break;
                    }
                }

                map[missionKey] = meta;
            }

            return map;
        }

        private static string FindPreview(string dir, string id)
        {
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp" })
            {
                var p = Path.Combine(dir, id + ext);
                if (File.Exists(p)) return p;
            }
            return "";
        }

        private static string FindPreviewInDir(string dir, string id)
        {
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp" })
            {
                if (!Directory.Exists(dir))
                    continue;

                var files = Directory.GetFiles(dir, id + ext);
                if (files.Length > 0) return files[0];

                foreach (var f in Directory.GetFiles(dir))
                    if (string.Equals(Path.GetFileNameWithoutExtension(f), id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Path.GetExtension(f), ext, StringComparison.OrdinalIgnoreCase))
                        return f;
            }
            return "";
        }

        private static string ReadTextSmart(string absPath)
        {
            try
            {
                var bytes = File.ReadAllBytes(absPath);
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    return System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
                try
                {
                    var s = System.Text.Encoding.GetEncoding(1251).GetString(bytes);
                    if (s.Length > 0) return s;
                }
                catch { }
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch { return ""; }
        }

        private static int NaturalCompare(string a, string b)
        {
            a = Path.GetFileNameWithoutExtension(a) ?? "";
            b = Path.GetFileNameWithoutExtension(b) ?? "";
            int ia = 0, ib = 0;
            while (ia < a.Length && ib < b.Length)
            {
                if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
                {
                    long va = 0, vb = 0;
                    while (ia < a.Length && char.IsDigit(a[ia])) va = va * 10 + (a[ia++] - '0');
                    while (ib < b.Length && char.IsDigit(b[ib])) vb = vb * 10 + (b[ib++] - '0');
                    int c = va.CompareTo(vb);
                    if (c != 0) return c;
                }
                else
                {
                    int c = char.ToUpperInvariant(a[ia]).CompareTo(char.ToUpperInvariant(b[ib]));
                    if (c != 0) return c;
                    ia++; ib++;
                }
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}
