using Cossacks2Bridge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    public sealed class OptionsRenderer : BaseUiRenderer
    {
        private static int s_lastBuildFrame = -1;

        public override void Render(UiDesk desk, CoreFileSystem fs, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            // ═══════════════════════════════════════════════════════════
            // ЗАЩИТА ОТ ПОВТОРНЫХ ВЫЗОВОВ В ТОТ ЖЕ FRAME
            // ═══════════════════════════════════════════════════════════
            int frame = Time.frameCount;
            if (s_lastBuildFrame == frame) return;
            s_lastBuildFrame = frame;

            RenderCounter++;

            // 1) Найти/создать Canvas
            RectTransform root = EnsureOptionsCanvas(opt);

            // 2) Очистить Canvas
            DestroyAllChildrenImmediate(root);

            // 3) Чистка кэша ресурсов
            ResFrames.ClearCache();

            // 4) Фон
            bool hasBackground = desk?.Children != null && desk.Children.Any(n => n is UiBitPicture);
            if (!hasBackground)
            {
                var bgPic = new UiBitPicture
                {
                    FileName = "Interf3/background/options.jpg",
                    X = 0,
                    Y = 0,
                    Width = 1024,
                    Height = 768,
                    Visible = true
                };
                CreateBitPicture(root, bgPic, fs, opt);
            }

            if (desk?.Children == null) return;

            // 1) BitPicture (фоны)
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiBitPicture pic) CreateBitPicture(root, pic, fs, opt);
            }

            // 2) GPPicture (декор)
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiGPPicture gp) CreateGPPicture(root, gp, fs, opt);
            }

            // 3) VitButton / VitLine (фон под ник) + InputBox + ListDesk
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;

                if (node is UiVitButton vb)
                {
                    // Если у VitButton НЕТ Actions — это декоративная линия (фон под ник)
                    if (vb.Actions == null || vb.Actions.Count == 0)
                        CreateVitButtonTiled(vb, root);
                    else
                        CreateVitButton(vb, root, opt, sink);
                }
                else if (node is UiInputBox ib)
                {
                    CreateInputBox(ib, root, opt);
                }
                else if (node is UiListDesk ld)
                {
                    CreateListDesk(ld, root);
                }
            }

            // 4) TextButton
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiTextButton btn) CreateTextButton(root, btn, opt, sink, loc, MenuOverrideDb.Resolve);
            }

            // 5) GP_TextButton
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiGPTextButton gpBtn) CreateGPTextButton(root, gpBtn, opt, sink, loc);
            }

            // 6) CheckBox, Slider, ComboBox
            int cbIndex = 0;
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;

                if (node is UiCheckBox cb)
                {
                    cbIndex++;
                    CreateCheckBox(cb, desk, root, opt, sink, loc, cbIndex);
                }
                else if (node is UiSlider sl)
                {
                    CreateSlider(sl, desk, root, opt, sink, loc);
                }
                else if (node is UiComboBox combo)
                {
                    CreateComboBox(combo, desk, root, opt, sink, loc);
                }
            }
        }

        // ===================== CANVAS / CLEANUP =====================

        private static RectTransform EnsureOptionsCanvas(RenderOptions opt)
        {
            var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            RectTransform reusable = null;

            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;

                if (canvas.gameObject.name == "C2_OptionsCanvas")
                {
                    if (reusable == null)
                    {
                        reusable = canvas.GetComponent<RectTransform>();
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                    }
                }
                else if (canvas.gameObject.name == "C2_MainMenuCanvas")
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }
            }

            if (reusable != null) return reusable;

            return CreateCanvasClean("C2_OptionsCanvas", opt);
        }

        private static RectTransform CreateCanvasClean(string canvasName, RenderOptions opt)
        {
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

            EnsureEventSystem();

            return root;
        }

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (existing != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void DestroyAllChildrenImmediate(RectTransform root)
        {
            if (root == null) return;

            root.gameObject.SetActive(false);

            var toDestroy = new List<GameObject>();
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child != null) toDestroy.Add(child.gameObject);
            }

            foreach (var go in toDestroy)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            root.gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
        }

        // ===================== RES FRAMES =====================

        private static class ResFrames
        {
            private static readonly Dictionary<string, Sprite> _cache = new();
            private static readonly Dictionary<string, Texture2D> _texCache = new();

            public static void ClearCache()
            {
                _cache.Clear();
                _texCache.Clear();
            }

            public static Texture2D GetTexture(string folder, string frameName)
            {
                string key = folder + "/" + frameName;

                if (_texCache.TryGetValue(key, out var cached) && cached != null)
                    return cached;

                var tex = Resources.Load<Texture2D>(key);
                if (tex != null)
                {
                    _texCache[key] = tex;
                    return tex;
                }

                return null;
            }

            public static Sprite GetByName(string folder, string frameName)
            {
                string key = folder + "/" + frameName;

                if (_cache.TryGetValue(key, out var sp) && sp != null)
                    return sp;

                var tex = Resources.Load<Texture2D>(key);
                if (tex == null) return null;

                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 1f);
                sp.name = frameName;
                _cache[key] = sp;

                return sp;
            }
        }

        private static class SpriteCropper
        {
            private static readonly Dictionary<string, Sprite> _cache = new();

            public static Sprite CropLeft(Sprite src, int leftPx)
            {
                if (src == null || leftPx <= 0) return src;

                string key = $"{src.texture.GetInstanceID()}:{src.rect.x}:{src.rect.y}:{src.rect.width}:{src.rect.height}:L{leftPx}";
                if (_cache.TryGetValue(key, out var s) && s != null) return s;

                var r = src.rect;
                if (r.width <= leftPx + 1) return src;

                var newRect = new Rect(r.x + leftPx, r.y, r.width - leftPx, r.height);

                Vector2 pivotPx = new Vector2(src.pivot.x * r.width, src.pivot.y * r.height);
                pivotPx.x = Mathf.Max(0f, pivotPx.x - leftPx);
                var newPivot = new Vector2(pivotPx.x / newRect.width, pivotPx.y / newRect.height);

                var outSp = Sprite.Create(src.texture, newRect, newPivot, 1f);
                outSp.name = src.name + $"_cropL{leftPx}";
                _cache[key] = outSp;
                return outSp;
            }
        }

        private static Sprite LoadSpriteFromResources(string folder, string frameName)
        {
            var sp = Resources.Load<Sprite>($"{folder}/{frameName}");
            if (sp != null) return sp;

            var tex = Resources.Load<Texture2D>($"{folder}/{frameName}");
            if (tex == null) return null;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 1f);
        }

        // ===================== OPTIONS CONTROLS =====================

        private static void CreateCheckBox(
            UiCheckBox cb,
            UiDesk desk,
            RectTransform parent,
            RenderOptions opt,
            IUiActionSink sink,
            LocDb loc,
            int index)
        {
            // CUT мусорные чекбоксы
            if (index == 1 || index == 5) return;

            const string folder = "interf3_elements_checkbox_frames";

            var spOff = ResFrames.GetByName(folder, "frame_0000");
            var spOn = ResFrames.GetByName(folder, "frame_0001");

            var go = new GameObject($"CheckBox_{index:00}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(cb.X, -cb.Y);

            var sizeTex = (spOff != null ? spOff.texture : (spOn != null ? spOn.texture : null));
            rt.sizeDelta = sizeTex != null ? new Vector2(sizeTex.width, sizeTex.height) : new Vector2(16, 16);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            img.sprite = cb.State ? (spOn ?? spOff) : (spOff ?? spOn);
            img.preserveAspect = false;
            img.type = Image.Type.Simple;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;

            var dbg = go.AddComponent<CheckBoxDebugToggle>();
            dbg.Index = index;
            dbg.Image = img;
            dbg.SpriteOff = spOff;
            dbg.SpriteOn = spOn;
            dbg.State = cb.State;
        }

        private static void CreateComboBox(UiComboBox box, UiDesk desk, RectTransform parent, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            const string folder = "Interf3_elements_combo_frames";

            var spClosed = ResFrames.GetByName(folder, "frame_0000");
            var spOpen = ResFrames.GetByName(folder, "frame_0001");
            var spRow = ResFrames.GetByName(folder, "frame_0005");
            var spRowHover = ResFrames.GetByName(folder, "frame_0006");

            const int CROP_L = 3;
            spRow = SpriteCropper.CropLeft(spRow, CROP_L);
            spRowHover = SpriteCropper.CropLeft(spRowHover, CROP_L);

            var go = new GameObject($"ComboBox_{SafeName(box.Name)}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(box.X, -box.Y);
            rt.sizeDelta = new Vector2(box.Width, box.Height);

            var boxImgGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxImgGO.transform.SetParent(go.transform, false);

            var boxRt = (RectTransform)boxImgGO.transform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0, 1);
            boxRt.pivot = new Vector2(0, 1);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(box.Width, box.Height);

            var boxImg = boxImgGO.GetComponent<Image>();
            boxImg.raycastTarget = true;
            boxImg.sprite = spClosed;
            boxImg.preserveAspect = false;
            boxImg.type = Image.Type.Simple;

            var textGO = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(boxImgGO.transform, false);

            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = new Vector2(24, 2);
            trt.offsetMax = new Vector2(-35, -2);

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            ApplyTextStyle(tmp, UiTextStyle.OptionLabel, opt);
            tmp.text = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";

            var list = BuildResolutionList();

            float rowH = 20f;
            int maxVisible = 14;
            int visibleCount = Mathf.Min(maxVisible, list.Count);

            float topPad = 2f;
            float botPad = 4f;
            float panelH = topPad + botPad + visibleCount * rowH;
            float panelW = box.Width;

            var panelGO = new GameObject("ComboDropPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);

            var panelRt = (RectTransform)panelGO.transform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0, 1);
            panelRt.pivot = new Vector2(0, 1);
            panelRt.anchoredPosition = new Vector2(box.X + 10f, -box.Y - box.Height);
            panelRt.sizeDelta = new Vector2(panelW, panelH);

            var panelImg = panelGO.GetComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0);
            panelImg.raycastTarget = true;

            var blockerGO = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
            blockerGO.transform.SetParent(parent, false);

            var blockerRt = (RectTransform)blockerGO.transform;
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = Vector2.zero;
            blockerRt.offsetMax = Vector2.zero;

            var blockerImg = blockerGO.GetComponent<Image>();
            blockerImg.color = new Color(0, 0, 0, 0);
            blockerImg.raycastTarget = true;

            var rowsContainer = new GameObject("Rows", typeof(RectTransform));
            rowsContainer.transform.SetParent(panelGO.transform, false);

            var rowsRt = (RectTransform)rowsContainer.transform;
            rowsRt.anchorMin = new Vector2(0, 1);
            rowsRt.anchorMax = new Vector2(1, 1);
            rowsRt.pivot = new Vector2(0, 1);
            rowsRt.anchoredPosition = new Vector2(0, -topPad);
            rowsRt.sizeDelta = new Vector2(0, visibleCount * rowH);

            var controller = boxImgGO.AddComponent<ComboBoxController>();
            controller.Panel = panelGO;
            controller.Blocker = blockerGO;
            controller.BoxImage = boxImg;
            controller.SpriteClosed = spClosed;
            controller.SpriteOpen = spOpen ?? spClosed;

            for (int i = 0; i < visibleCount; i++)
            {
                string resText = list[i];

                var row = new GameObject($"Row_{i:00}", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(rowsContainer.transform, false);

                var rrt = (RectTransform)row.transform;
                rrt.anchorMin = new Vector2(0, 1);
                rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0, 1);
                rrt.anchoredPosition = new Vector2(0, -i * rowH);
                rrt.sizeDelta = new Vector2(0, rowH);

                var rowImg = row.GetComponent<Image>();
                rowImg.raycastTarget = true;
                rowImg.sprite = spRow;
                rowImg.type = Image.Type.Sliced;
                rowImg.color = Color.white;

                var lab = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                lab.transform.SetParent(row.transform, false);

                var lrt = (RectTransform)lab.transform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(35, 0);
                lrt.offsetMax = new Vector2(-6, 0);

                var ltmp = lab.GetComponent<TextMeshProUGUI>();
                ltmp.raycastTarget = false;
                ltmp.richText = false;
                ltmp.textWrappingMode = TextWrappingModes.NoWrap;
                ltmp.alignment = TextAlignmentOptions.Left;
                ltmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                ltmp.text = resText;
                ApplyTextStyle(ltmp, UiTextStyle.OptionLabel, opt);

                var hover = row.AddComponent<RowHoverSwap>();
                hover.Bg = rowImg;
                hover.NormalSprite = spRow;
                hover.HoverSprite = spRowHover ?? spRow;

                string capturedText = resText;
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    tmp.text = capturedText;
                    controller.ClosePopup();
                });
            }

            var blockerClick = blockerGO.AddComponent<Button>();
            blockerClick.transition = Selectable.Transition.None;
            blockerClick.onClick.AddListener(controller.ClosePopup);

            blockerGO.SetActive(false);
            panelGO.SetActive(false);
        }

        private static List<string> BuildResolutionList()
        {
            var hs = new HashSet<string>();
            var res = Screen.resolutions;
            for (int i = 0; i < res.Length; i++)
                hs.Add($"{res[i].width}x{res[i].height}");

            var list = new List<string>(hs);
            list.Sort((a, b) =>
            {
                Parse(a, out int aw, out int ah);
                Parse(b, out int bw, out int bh);
                int c = aw.CompareTo(bw);
                return c != 0 ? c : ah.CompareTo(bh);
            });
            return list;

            static void Parse(string s, out int w, out int h)
            {
                w = 0; h = 0;
                int x = s.IndexOf('x');
                if (x <= 0) return;
                int.TryParse(s.Substring(0, x), out w);
                int.TryParse(s.Substring(x + 1), out h);
            }
        }

        private static void CreateSlider(UiSlider sl, UiDesk desk, RectTransform parent, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            const string folder = "interf3_elements_slider_frames";
            const int removeLeftLamellas = 1;
            const int gapPx = 1;
            const int REAL_LAM_W = 10, REAL_LAM_H = 18;
            const int REAL_THUMB_W = 15, REAL_THUMB_H = 20;

            Sprite spThumb = ResFrames.GetByName(folder, "frame_0000");
            Sprite spLam0 = ResFrames.GetByName(folder, "frame_0003");
            if (spThumb == null) return;

            int lineW = (sl.LineLx > 0) ? sl.LineLx : Mathf.Max(1, sl.Width);
            int lineH = (sl.LineLy > 0) ? sl.LineLy : Mathf.Max(1, sl.Height);
            int max = Mathf.Max(1, sl.MaxPosition);
            int pos = Mathf.Clamp(sl.Position, 0, max);

            float lamY = -Mathf.Max(0f, (lineH - REAL_LAM_H) * 0.5f);
            float thumbY = -Mathf.Max(0f, (lineH - REAL_THUMB_H) * 0.5f);

            int rawCount = Mathf.Max(1, (lineW + gapPx) / (REAL_LAM_W + gapPx));
            int count = Mathf.Max(1, rawCount - removeLeftLamellas);
            int startX = removeLeftLamellas * (REAL_LAM_W + gapPx);

            var sliderRoot = new GameObject($"Slider_{SafeName(sl.Name)}", typeof(RectTransform));
            sliderRoot.transform.SetParent(parent, false);

            var rrt = (RectTransform)sliderRoot.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(0, 1);
            rrt.pivot = new Vector2(0, 1);
            rrt.anchoredPosition = new Vector2(sl.X, -sl.Y);
            rrt.sizeDelta = new Vector2(sl.Width > 0 ? sl.Width : lineW, sl.Height > 0 ? sl.Height : lineH);

            var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGO.transform.SetParent(sliderRoot.transform, false);

            var trackRT = (RectTransform)trackGO.transform;
            trackRT.anchorMin = trackRT.anchorMax = new Vector2(0, 1);
            trackRT.pivot = new Vector2(0, 1);
            trackRT.anchoredPosition = new Vector2(sl.ScrDx, sl.ScrDy);
            trackRT.sizeDelta = new Vector2(lineW, lineH);

            var trackImg = trackGO.GetComponent<Image>();
            trackImg.color = new Color(0, 0, 0, 0);
            trackImg.raycastTarget = true;

            var lamContainer = new GameObject("Lamellas", typeof(RectTransform));
            lamContainer.transform.SetParent(trackGO.transform, false);

            var lamContainerRT = (RectTransform)lamContainer.transform;
            lamContainerRT.anchorMin = lamContainerRT.anchorMax = new Vector2(0, 1);
            lamContainerRT.pivot = new Vector2(0, 1);
            lamContainerRT.anchoredPosition = Vector2.zero;
            lamContainerRT.sizeDelta = new Vector2(lineW, lineH);

            for (int i = 0; i < count; i++)
            {
                int x = startX + i * (REAL_LAM_W + gapPx);

                var seg = new GameObject($"Lam_{i:00}", typeof(RectTransform), typeof(Image));
                seg.transform.SetParent(lamContainer.transform, false);

                var srt = (RectTransform)seg.transform;
                srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
                srt.pivot = new Vector2(0, 1);
                srt.anchoredPosition = new Vector2(x, lamY);
                srt.sizeDelta = new Vector2(REAL_LAM_W, REAL_LAM_H);

                var img = seg.GetComponent<Image>();
                img.sprite = spLam0;
                img.raycastTarget = false;
                img.type = Image.Type.Simple;
            }

            int thumbW = REAL_THUMB_W;
            float initialT = (max > 0) ? (pos / (float)max) : 0f;

            float minPx = startX;
            float maxPx = Mathf.Max(minPx, lineW - thumbW);
            float initialPx = Mathf.Lerp(minPx, maxPx, initialT);

            var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(sliderRoot.transform, false);

            var thRT = (RectTransform)thumbGO.transform;
            thRT.anchorMin = thRT.anchorMax = new Vector2(0, 1);
            thRT.pivot = new Vector2(0, 1);
            thRT.sizeDelta = new Vector2(REAL_THUMB_W, REAL_THUMB_H);

            float thumbBaseX = sl.ScrDx + initialPx;
            float thumbBaseY = sl.ScrDy + thumbY;
            thRT.anchoredPosition = new Vector2(thumbBaseX, thumbBaseY);

            var thImg = thumbGO.GetComponent<Image>();
            thImg.sprite = spThumb;
            thImg.raycastTarget = false;
            thImg.type = Image.Type.Simple;
            thImg.preserveAspect = false;

            thumbGO.transform.SetAsLastSibling();

            var handler = sliderRoot.AddComponent<SliderController>();
            handler.Initialize(
                trackRT: trackRT,
                thumbRT: thRT,
                thumbImage: thImg,
                max: max,
                initialPos: pos,
                thumbY: thumbBaseY,
                trackOffsetX: sl.ScrDx,
                lineWidth: lineW,
                thumbWidth: thumbW,
                lamMinX: sl.ScrDx + startX
            );
        }

        private static void CreateGPPicture(RectTransform parent, UiGPPicture gp, CoreFileSystem fs, RenderOptions opt)
        {
            string resPath = (gp.FileID ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToUpperInvariant() + "_frames";

            var sprite = LoadSpriteFromResources(resPath, $"frame_{gp.SpriteID:0000}");
            if (sprite == null)
            {
                Debug.LogWarning($"[GPPicture] sprite not found {resPath}/frame_{gp.SpriteID:0000}");
                return;
            }

            var go = new GameObject("GPPicture", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(gp.X, -gp.Y);
            rt.sizeDelta = new Vector2(gp.Width, gp.Height);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
        }

        private static void CreateInputBox(UiInputBox ib, RectTransform root, RenderOptions opt)
        {
            var go = new GameObject($"InputBox_{SafeName(ib.Name)}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(ib.X, -ib.Y);

            float w = ib.Width > 0 ? ib.Width : 320;
            float h = ib.Height > 0 ? ib.Height : 20;
            rt.sizeDelta = new Vector2(w, h);

            var bgImg = go.GetComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.1f);
            bgImg.raycastTarget = true;

            var textAreaGO = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textAreaGO.transform.SetParent(go.transform, false);

            var textAreaRt = (RectTransform)textAreaGO.transform;
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(5, 2);
            textAreaRt.offsetMax = new Vector2(-5, -2);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(textAreaGO.transform, false);

            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.text = "";

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(textAreaGO.transform, false);

            var prt = (RectTransform)placeholderGO.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var ptmp = placeholderGO.GetComponent<TextMeshProUGUI>();
            ptmp.fontSize = 14;
            ptmp.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            ptmp.alignment = TextAlignmentOptions.Left;
            ptmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            ptmp.raycastTarget = false;
            ptmp.fontStyle = FontStyles.Italic;
            ptmp.text = "Введите имя...";

            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textComponent = tmp;
            inputField.placeholder = ptmp;
            inputField.textViewport = textAreaRt;
            inputField.targetGraphic = bgImg;

            inputField.characterLimit = ib.MaxLen > 0 ? ib.MaxLen : 30;
            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.inputType = TMP_InputField.InputType.Standard;
            inputField.keyboardType = TouchScreenKeyboardType.Default;

            inputField.caretColor = Color.black;
            inputField.caretWidth = 2;
            inputField.caretBlinkRate = 0.85f;
            inputField.selectionColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);

            inputField.interactable = true;
            inputField.readOnly = false;
        }

        private static void CreateListDesk(UiListDesk ld, RectTransform root)
        {
            var go = new GameObject("ListDesk", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(ld.X, -ld.Y);
            rt.sizeDelta = new Vector2(ld.Width, ld.Height);

            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
        }

        private static void CreateVitButton(UiVitButton vb, RectTransform parent, RenderOptions opt, IUiActionSink sink)
        {
            if (vb == null || parent == null) return;

            string resPath = (vb.GP_File ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToUpperInvariant() + "_frames";

            var spNormal = LoadSpriteFromResources(resPath, $"frame_{vb.SpritePassive:0000}");
            var spHover = LoadSpriteFromResources(resPath, $"frame_{vb.SpriteActive:0000}") ?? spNormal;

            var go = new GameObject($"VitButton_{SafeName(vb.Name)}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(vb.X, -vb.Y);

            float w = vb.Width > 0 ? vb.Width : (spNormal != null ? spNormal.rect.width : 0f);
            float h = vb.Height > 0 ? vb.Height : (spNormal != null ? spNormal.rect.height : 0f);
            if (w <= 0) w = 16;
            if (h <= 0) h = 16;
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            img.sprite = spNormal;
            img.preserveAspect = false;
            img.type = Image.Type.Simple;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = vb.Enabled;

            var hover = go.AddComponent<VitButtonHoverSwap>();
            hover.Bg = img;
            hover.Normal = spNormal;
            hover.Hover = spHover;

            if (vb.Actions != null && vb.Actions.Count > 0)
            {
                btn.onClick.AddListener(() =>
                {
                    foreach (var a in vb.Actions)
                    {
                        try { sink?.OnAction(vb.Name, a); }
                        catch (Exception e) { Debug.LogError($"VitButton action error: {e}"); }
                    }
                });
            }
        }

        // ЛИНИЯ ИЗ ФРЕЙМОВ VBUTTONS (для VitButton БЕЗ действий, типа фона под ник)
        private static void CreateVitButtonTiled(UiVitButton vb, RectTransform parent)
        {
            if (vb == null || parent == null) return;

            // ДЕЛАЕМ ТОЧНО КАК У GPPicture: UPPERCASE
            string resPath = (vb.GP_File ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToUpperInvariant() + "_frames";

            var tile = LoadSpriteFromResources(resPath, $"frame_{vb.SpritePassive:0000}");
            if (tile == null)
            {
                Debug.LogError($"[VitButtonTiled] sprite NOT FOUND: {resPath}/frame_{vb.SpritePassive:0000}");
                return;
            }

            Debug.Log($"[VitButtonTiled] use {resPath}/frame_{vb.SpritePassive:0000} " +
                      $"size={tile.rect.width}x{tile.rect.height} at ({vb.X},{vb.Y}), W={vb.Width}, H={vb.Height}");

            var go = new GameObject($"VitLine_{SafeName(vb.Name)}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(vb.X, -vb.Y);

            float segW = tile.rect.width;
            float segH = tile.rect.height;

            float width = vb.Width > 0 ? vb.Width : segW;
            float height = vb.Height > 0 ? vb.Height : segH;
            rt.sizeDelta = new Vector2(width, height);

            int count = Mathf.Max(1, Mathf.CeilToInt(width / Mathf.Max(1f, segW)));

            for (int i = 0; i < count; i++)
            {
                var segGO = new GameObject($"Seg_{i}", typeof(RectTransform), typeof(Image));
                segGO.transform.SetParent(go.transform, false);

                var srt = (RectTransform)segGO.transform;
                srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
                srt.pivot = new Vector2(0, 1);
                srt.anchoredPosition = new Vector2(i * segW, 0);
                srt.sizeDelta = new Vector2(segW, height);

                var img = segGO.GetComponent<Image>();
                img.sprite = tile;
                img.color = Color.white;          // на всякий случай
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
            }
        }

        private sealed class VitButtonHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Bg;
            public Sprite Normal;
            public Sprite Hover;

            public void OnPointerEnter(PointerEventData e)
            {
                if (Bg != null && Hover != null) Bg.sprite = Hover;
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (Bg != null && Normal != null) Bg.sprite = Normal;
            }
        }

        private static void CreateGPTextButton(RectTransform parent, UiGPTextButton btn, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            var go = new GameObject($"GPTextButton_{SafeName(btn.MessageKey)}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(btn.X, -btn.Y);
            rt.sizeDelta = new Vector2(btn.Width, btn.Height);

            var bg = go.AddComponent<Image>();
            bg.raycastTarget = true;

            Sprite normalSp = LoadButtonFrame(btn.Sprite1);
            Sprite hoverSp = LoadButtonFrame(btn.Sprite);
            Sprite disabledSp = LoadButtonFrame(btn.Sprite1 + 1);

            bg.sprite = normalSp ?? hoverSp;
            bg.type = Image.Type.Sliced;

            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = btn.Enabled;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);

            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            string textResolved = loc?.Resolve(btn.MessageKey) ?? btn.MessageKey;
            tmp.text = textResolved;
            ApplyTextStyle(tmp, UiTextStyle.Button, opt);

            if (textResolved?.Trim() is "ПРИНЯТЬ" or "ОТМЕНА")
            {
                tmp.fontStyle = FontStyles.Normal;
                tmp.fontWeight = FontWeight.Regular;
                var m = new Material(tmp.fontMaterial);
                m.SetFloat(ShaderUtilities.ID_FaceDilate, -0.15f);
                m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                tmp.fontMaterial = m;
            }

            tmp.alignment = btn.Center ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
            tmp.rectTransform.anchoredPosition += new Vector2(btn.FontDx, -btn.FontDy);

            var swap = go.AddComponent<GpButtonHoverSwap>();
            swap.Bg = bg;
            swap.Label = tmp;
            swap.Button = button;
            swap.NormalBg = normalSp;
            swap.HoverBg = hoverSp;
            swap.DisabledBg = disabledSp;
            swap.NormalText = OptionsTextStyleConfig.Button.NormalColor;
            swap.HoverText = OptionsTextStyleConfig.Button.HoverColor;
            swap.DisabledText = OptionsTextStyleConfig.Button.DisabledColor;

            button.onClick.AddListener(() =>
            {
                if (btn.Actions == null) return;
                foreach (var a in btn.Actions)
                {
                    try { sink?.OnAction(btn.MessageKey, a); }
                    catch (Exception e) { Debug.LogError($"Action error: {e}"); }
                }
            });
        }

        private static Sprite LoadButtonFrame(int id)
        {
            var tex = Resources.Load<Texture2D>($"Buttons/frame_{id:0000}");
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
        }

        // ===================== HELPER CLASSES =====================

        private sealed class ComboBoxController : MonoBehaviour, IPointerClickHandler
        {
            public GameObject Panel;
            public GameObject Blocker;
            public Image BoxImage;
            public Sprite SpriteClosed;
            public Sprite SpriteOpen;

            private bool _isOpen;

            public void OnPointerClick(PointerEventData e)
            {
                if (Panel == null) return;
                _isOpen = !_isOpen;
                if (_isOpen) OpenPopup();
                else ClosePopup();
            }

            private void OpenPopup()
            {
                if (Blocker != null)
                {
                    Blocker.SetActive(true);
                    Blocker.transform.SetAsLastSibling();
                }

                if (Panel != null)
                {
                    Panel.SetActive(true);
                    Panel.transform.SetAsLastSibling();
                }

                if (BoxImage != null) BoxImage.sprite = SpriteOpen;
                _isOpen = true;
            }

            public void ClosePopup()
            {
                _isOpen = false;
                if (Panel != null) Panel.SetActive(false);
                if (Blocker != null) Blocker.SetActive(false);
                if (BoxImage != null) BoxImage.sprite = SpriteClosed;
            }

            private void OnDisable() => ClosePopup();
        }

        private sealed class RowHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Bg;
            public Sprite NormalSprite;
            public Sprite HoverSprite;

            public void OnPointerEnter(PointerEventData e)
            {
                if (Bg == null) return;
                Bg.sprite = HoverSprite != null ? HoverSprite : NormalSprite;
                Bg.SetVerticesDirty();
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (Bg == null) return;
                Bg.sprite = NormalSprite;
                Bg.SetVerticesDirty();
            }
        }

        private sealed class GpButtonHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Bg;
            public TextMeshProUGUI Label;
            public Button Button;
            public Sprite NormalBg, HoverBg, DisabledBg;
            public Color32 NormalText, HoverText, DisabledText;

            void OnEnable() => ApplyCurrent();

            void ApplyCurrent()
            {
                bool ok = Button == null || Button.interactable;
                if (Bg) Bg.sprite = ok ? NormalBg : DisabledBg;
                if (Label) Label.color = ok ? NormalText : DisabledText;
            }

            public void OnPointerEnter(PointerEventData e)
            {
                if (Button != null && !Button.interactable) return;
                if (Bg && HoverBg) Bg.sprite = HoverBg;
                if (Label) Label.color = HoverText;
            }

            public void OnPointerExit(PointerEventData e) => ApplyCurrent();
        }

        private sealed class SliderController : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
        {
            private RectTransform _trackRT;
            private RectTransform _thumbRT;
            private Image _thumbImage;
            private int _max;
            private int _currentPos;
            private float _thumbY;
            private float _trackOffsetX;
            private float _lineWidth;
            private float _thumbWidth;
            private bool _isDragging;
            private Canvas _canvas;
            private CanvasRenderer _thumbCanvasRenderer;
            private float _lamMinX;
            private float _grabOffsetPx;

            public void Initialize(
                RectTransform trackRT,
                RectTransform thumbRT,
                Image thumbImage,
                int max,
                int initialPos,
                float thumbY,
                float trackOffsetX,
                float lineWidth,
                float thumbWidth,
                float lamMinX)
            {
                _trackRT = trackRT;
                _thumbRT = thumbRT;
                _thumbImage = thumbImage;
                _max = max;
                _lamMinX = lamMinX;
                _currentPos = initialPos;
                _thumbY = thumbY;
                _trackOffsetX = trackOffsetX;
                _lineWidth = lineWidth;
                _thumbWidth = thumbWidth;
                _isDragging = false;

                _canvas = GetComponentInParent<Canvas>();
                _thumbCanvasRenderer = thumbRT != null ? thumbRT.GetComponent<CanvasRenderer>() : null;
            }

            private void Start()
            {
                StartCoroutine(ForceRefreshNextFrame());
            }

            private IEnumerator ForceRefreshNextFrame()
            {
                yield return null;
                ForceRefresh();
            }

            public void OnPointerDown(PointerEventData e)
            {
                _isDragging = true;

                if (_trackRT != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _trackRT, e.position, e.pressEventCamera, out var lp))
                {
                    float curLeft = _thumbRT.anchoredPosition.x - _trackOffsetX;

                    if (lp.x >= curLeft && lp.x <= curLeft + _thumbWidth)
                        _grabOffsetPx = lp.x - curLeft;
                    else
                        _grabOffsetPx = _thumbWidth * 0.5f;

                    _grabOffsetPx = Mathf.Clamp(_grabOffsetPx, 0f, _thumbWidth);
                }
                else
                {
                    _grabOffsetPx = _thumbWidth * 0.5f;
                }

                HandleInput(e);
            }

            public void OnBeginDrag(PointerEventData e) => _isDragging = true;

            public void OnDrag(PointerEventData e)
            {
                if (_isDragging) HandleInput(e);
            }

            public void OnEndDrag(PointerEventData e) => _isDragging = false;

            private void HandleInput(PointerEventData e)
            {
                if (_trackRT == null || _max <= 0) return;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _trackRT, e.position, e.pressEventCamera, out var localPoint))
                    return;

                float minPx = _lamMinX - _trackOffsetX;
                float maxPx = Mathf.Max(minPx, _lineWidth - _thumbWidth);

                float leftPx = localPoint.x - _grabOffsetPx;
                leftPx = Mathf.Clamp(leftPx, minPx, maxPx);

                float newX = _trackOffsetX + leftPx;
                _thumbRT.anchoredPosition = new Vector2(newX, _thumbY);

                float denom = Mathf.Max(1e-4f, maxPx - minPx);
                float t = (leftPx - minPx) / denom;
                _currentPos = Mathf.Clamp(Mathf.RoundToInt(t * _max), 0, _max);

                ForceRefresh();
            }

            private void ForceRefresh()
            {
                if (_thumbImage != null)
                {
                    _thumbImage.SetVerticesDirty();
                    _thumbImage.SetMaterialDirty();
                }

                if (_thumbCanvasRenderer != null)
                    _thumbCanvasRenderer.SetAlpha(1f);

                if (_thumbRT != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_thumbRT);

                if (_canvas != null)
                    Canvas.ForceUpdateCanvases();
            }
        }

        private sealed class CheckBoxDebugToggle : MonoBehaviour
        {
            public int Index;
            public Image Image;
            public Sprite SpriteOff;
            public Sprite SpriteOn;
            public bool State;

            public void Toggle()
            {
                State = !State;
                if (Image)
                    Image.sprite = State ? (SpriteOn ?? SpriteOff) : (SpriteOff ?? SpriteOn);
            }

            private void Awake()
            {
                var b = GetComponent<Button>();
                if (b) b.onClick.AddListener(Toggle);
            }
        }
    }
}
