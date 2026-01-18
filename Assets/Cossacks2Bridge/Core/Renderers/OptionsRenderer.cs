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
        private static object parent;

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

            // ДИАГНОСТИКА: какие InputBox есть в desk
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("[OptionsRenderer] InputBox elements in desk:");
            foreach (var node in desk.Children)
            {
                if (node is UiInputBox ib)
                    Debug.Log($"  InputBox: name='{ib.Name}', pos=({ib.X},{ib.Y}), size=({ib.Width}x{ib.Height}), visible={ib.Visible}");
            }
            Debug.Log("═══════════════════════════════════════════════════════════");

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
            bool inputBoxCreated = false;

            // Координаты целевой позиции InputBox (где VitButton служит фоном)
            const float TARGET_INPUT_X = 573f;
            const float TARGET_INPUT_Y = 286f;

            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;

                if (node is UiVitButton vb)
                {
                    // ═══════════════════════════════════════════════════════════
                    // Пропускаем ТОЛЬКО ВЕРХНИЙ VitButton (Y < 100) - он не нужен
                    // Нижний VitButton (Y > 200) ОСТАВЛЯЕМ - это фон для InputBox
                    // ═══════════════════════════════════════════════════════════
                    bool isDecorative = (vb.Actions == null || vb.Actions.Count == 0);
                    bool isInputBackground = (vb.Width >= 300 && vb.Height <= 25);
                    bool isUpperPosition = (vb.Y < 100);

                    if (isDecorative && isInputBackground && isUpperPosition)
                    {
                        Debug.Log($"[OptionsRenderer] SKIP VitButton (orphan upper bg): pos=({vb.X},{vb.Y})");
                        continue;
                    }

                    // Остальные VitButton создаём
                    if (isDecorative)
                        CreateVitButtonTiled(vb, root);
                    else
                        CreateVitButton(vb, root, opt, sink);
                }
                else if (node is UiInputBox ib)
                {
                    if (inputBoxCreated)
                    {
                        // ═══════════════════════════════════════════════════════════
                        // ФИКС: Второй InputBox ПЕРЕМЕЩАЕМ на место первого (поверх фона)
                        // ═══════════════════════════════════════════════════════════
                        Debug.Log($"[OptionsRenderer] RELOCATE InputBox from ({ib.X},{ib.Y}) to ({TARGET_INPUT_X},{TARGET_INPUT_Y})");

                        // Меняем координаты напрямую
                        ib.X = (int)TARGET_INPUT_X;
                        ib.Y = (int)TARGET_INPUT_Y;
                        CreateInputBox(ib, root, opt);
                        continue;
                    }

                    // ═══════════════════════════════════════════════════════════
                    // Первый InputBox (573,286) - ПРОПУСКАЕМ, но помечаем что был
                    // ═══════════════════════════════════════════════════════════
                    Debug.Log($"[OptionsRenderer] SKIP first InputBox (will use second): pos=({ib.X},{ib.Y})");
                    inputBoxCreated = true;  // Помечаем что "первый обработан"
                }
            }



            // 4) TextButton
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiTextButton btn)
                {
                    string resolvedText = loc?.Resolve(btn.MessageKey) ?? btn.MessageKey;

                    // ═══════════════════════════════════════════════════════════
                    // ФИКС: Принудительный сдвиг "Имя игрока" влево
                    // ═══════════════════════════════════════════════════════════
                    if (resolvedText != null && resolvedText.Contains("Имя игрока"))
                    {
                        btn.X -= 15;  // Попробуйте -50, -80, -100 пока не выровняется
                        Debug.Log($"[OptionsRenderer] FORCE SHIFT 'Имя игрока' to X={btn.X}");
                    }

                    CreateTextButton(root, btn, opt, sink, loc, MenuOverrideDb.Resolve);
                }
            }


            // 5) GP_TextButton
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiGPTextButton gpBtn) CreateGPTextButton(root, gpBtn, opt, sink, loc);
            }
            // 5) ListDesk (список подключений)
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiListDesk ld)
                {
                    CreateListDeskVisual(ld, root, opt);
                }
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
                    // CreateSlider(sl, desk, root, opt, sink, loc);  // <-- ВРЕМЕННО
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
                else if (canvas.gameObject.name == "C2_MenuCanvas")
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

            // ═══════════════════════════════════════════════════════════
            // ВАРИАНТ A: Целочисленное масштабирование
            // ═══════════════════════════════════════════════════════════
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // Вычисляем целочисленный множитель
            int scaleFactorX = Mathf.Max(1, Screen.width / 1024);
            int scaleFactorY = Mathf.Max(1, Screen.height / 768);
            int scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY); // Берём меньший

            scaler.scaleFactor = scaleFactor; // 1x, 2x, 3x - ЦЕЛОЕ число!

            // ═══════════════════════════════════════════════════════════
            // ИЛИ ВАРИАНТ B: ScaleWithScreenSize но с Expand
            // ═══════════════════════════════════════════════════════════
            // scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // scaler.referenceResolution = new Vector2(1024, 768);
            // scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // scaler.matchWidthOrHeight = 0f; // Игнорируется при Expand

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
            // БЫЛО:
            // var existing = UnityEngine.Object.FindObjectOfType<EventSystem>();

            // СТАЛО:
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
        /// <summary>
        /// ListDesk по логике DrawFilledRect + DrawRect4 из оригинала
        /// Маппинг BD фреймов:
        /// 0 - левый нижний угол, 1 - правый нижний угол
        /// 2 - левый верхний угол, 3 - правый верхний угол
        /// 4 - верхняя горизонтальная линия (текстура горизонтальная)
        /// 5 - нижняя горизонтальная линия (текстура горизонтальная)
        /// 7 - левая вертикальная линия (текстура вертикальная)
        /// 8 - правая вертикальная линия (текстура вертикальная)
        /// 6, 9, 10, 11 - наполнитель
        /// </summary>
        /// <summary>
        /// ListDesk по логике DrawFilledRect + DrawRect4 из оригинала
        /// ИСПРАВЛЕНО: позиции углов используют реальные размеры спрайтов
        /// </summary>
        private static void CreateListDeskVisual(UiListDesk ld, RectTransform parent, RenderOptions opt)
        {
            if (ld == null || parent == null) return;

            // Диагностика Canvas
            var canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[CANVAS DIAG] scaleFactor={canvas.scaleFactor}, " +
                    $"referencePixelsPerUnit={canvas.referencePixelsPerUnit}, " +
                    $"pixelPerfect={canvas.pixelPerfect}");
            }

            const string folder = "interf3_elements_border_BD_frames";

            float w = ld.Width;
            float h = ld.Height;

            // ═══════════════════════════════════════════════════════════
            // 1. ЗАГРУЗКА СПРАЙТОВ
            // ═══════════════════════════════════════════════════════════

            // Углы (по документации: 0=LB, 1=RB, 2=LT, 3=RT)
            Sprite spCornerLB = LoadSpriteFromResources(folder, "frame_0000");
            Sprite spCornerRB = LoadSpriteFromResources(folder, "frame_0001");
            Sprite spCornerLT = LoadSpriteFromResources(folder, "frame_0002");
            Sprite spCornerRT = LoadSpriteFromResources(folder, "frame_0003");

            // Линии (4=Top, 5=Bottom, 7=Left, 8=Right)
            Sprite spLineTop = LoadSpriteFromResources(folder, "frame_0004");
            Sprite spLineBottom = LoadSpriteFromResources(folder, "frame_0005");
            Sprite spLineLeft = LoadSpriteFromResources(folder, "frame_0007");
            Sprite spLineRight = LoadSpriteFromResources(folder, "frame_0008");

            // Наполнитель
            Sprite[] fill = {
        LoadSpriteFromResources(folder, "frame_0006"),
        LoadSpriteFromResources(folder, "frame_0009"),
        LoadSpriteFromResources(folder, "frame_0010"),
        LoadSpriteFromResources(folder, "frame_0011")
    };

            // ═══════════════════════════════════════════════════════════
            // 2. РЕАЛЬНЫЕ РАЗМЕРЫ из спрайтов (не константа S!)
            // ═══════════════════════════════════════════════════════════
            float cornerW = spCornerLT != null ? spCornerLT.rect.width : 32f;
            float cornerH = spCornerLT != null ? spCornerLT.rect.height : 32f;

            // Для безопасности берём максимум из всех углов
            if (spCornerLB != null) { cornerW = Mathf.Max(cornerW, spCornerLB.rect.width); cornerH = Mathf.Max(cornerH, spCornerLB.rect.height); }
            if (spCornerRB != null) { cornerW = Mathf.Max(cornerW, spCornerRB.rect.width); cornerH = Mathf.Max(cornerH, spCornerRB.rect.height); }
            if (spCornerRT != null) { cornerW = Mathf.Max(cornerW, spCornerRT.rect.width); cornerH = Mathf.Max(cornerH, spCornerRT.rect.height); }

            float lineThickness = spLineTop != null ? spLineTop.rect.height : cornerH;

            Debug.Log($"[ListDesk] Real sizes: cornerW={cornerW}, cornerH={cornerH}, lineThickness={lineThickness}");

            // ═══════════════════════════════════════════════════════════
            // 3. КОНТЕЙНЕР
            // ═══════════════════════════════════════════════════════════
            var container = new GameObject($"ListDesk_{SafeName(ld.Name)}", typeof(RectTransform));
            container.transform.SetParent(parent, false);

            var rt = (RectTransform)container.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(ld.X, -ld.Y);
            rt.sizeDelta = new Vector2(w, h);

            // ═══════════════════════════════════════════════════════════
            // 4. НАПОЛНИТЕЛЬ (первый слой - самый нижний)
            // ═══════════════════════════════════════════════════════════
            float fillStartX = cornerW / 2f;
            float fillStartY = cornerH / 2f;
            float fillW = w - cornerW;
            float fillH = h - cornerH;

            var fillContainer = new GameObject("Fill", typeof(RectTransform), typeof(RectMask2D));
            fillContainer.transform.SetParent(container.transform, false);
            fillContainer.transform.SetAsFirstSibling(); // ← В САМЫЙ НИЗ!

            var fillRt = (RectTransform)fillContainer.transform;
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 1);
            fillRt.anchoredPosition = new Vector2(fillStartX, -fillStartY);
            fillRt.sizeDelta = new Vector2(fillW, fillH);

            float tileSize = fill[0] != null ? fill[0].rect.width : 32f;
            int nx = Mathf.CeilToInt(fillW / tileSize);
            int ny = Mathf.CeilToInt(fillH / tileSize);

            for (int iy = 0; iy < ny; iy++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    int idx = (ix + iy) % fill.Length;
                    Sprite sp = fill[idx] ?? fill[0];
                    if (sp == null) continue;

                    var tile = new GameObject($"F_{ix}_{iy}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(fillContainer.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(ix * tileSize, -iy * tileSize);
                    tileRt.sizeDelta = new Vector2(tileSize, tileSize);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                }
            }
            // ═══════════════════════════════════════════════════════════
            // 5. ЛИНИИ (второй слой - над наполнителем)
            // ═══════════════════════════════════════════════════════════
            float innerW = w - cornerW * 2;
            float innerH = h - cornerH * 2;

            Debug.Log($"[ListDesk LINES] innerW={innerW}, innerH={innerH}, cornerW={cornerW}, cornerH={cornerH}");
            Debug.Log($"[ListDesk LINES] spLineTop={spLineTop != null}, spLineBottom={spLineBottom != null}");
            Debug.Log($"[ListDesk LINES] spLineLeft={spLineLeft != null}, spLineRight={spLineRight != null}");

            // Верхняя горизонтальная линия — ПОДНЯТА на 1px
            if (spLineTop != null && innerW > 0)
            {
                float topY = 1f;
                Debug.Log($"[ListDesk] Creating Line_Top at X={cornerW}, Y={topY}, size={innerW}x{spLineTop.rect.height}");
                CreateTiledLineWithLog(container.transform, "Line_Top", spLineTop,
                    cornerW, topY, innerW, spLineTop.rect.height, isHorizontal: true);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Top: sprite={spLineTop != null}, innerW={innerW}");
            }

            // Нижняя горизонтальная линия — ОПУЩЕНА на 1px
            if (spLineBottom != null && innerW > 0)
            {
                float bottomY = -(h - spLineBottom.rect.height) - 1f;
                Debug.Log($"[ListDesk] Creating Line_Bottom at X={cornerW}, Y={bottomY}, size={innerW}x{spLineBottom.rect.height}");
                CreateTiledLineWithLog(container.transform, "Line_Bottom", spLineBottom,
                    cornerW, bottomY, innerW, spLineBottom.rect.height, isHorizontal: true);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Bottom: sprite={spLineBottom != null}, innerW={innerW}");
            }

            // Левая вертикальная линия (без изменений)
            if (spLineLeft != null && innerH > 0)
            {
                Debug.Log($"[ListDesk] Creating Line_Left at X=0, Y={-cornerH}, size={spLineLeft.rect.width}x{innerH}");
                CreateTiledLineWithLog(container.transform, "Line_Left", spLineLeft,
                    0, -cornerH, spLineLeft.rect.width, innerH, isHorizontal: false);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Left: sprite={spLineLeft != null}, innerH={innerH}");
            }

            // Правая вертикальная линия (без изменений)
            if (spLineRight != null && innerH > 0)
            {
                float rightX = w - spLineRight.rect.width;
                Debug.Log($"[ListDesk] Creating Line_Right at X={rightX}, Y={-cornerH}, size={spLineRight.rect.width}x{innerH}");
                CreateTiledLineWithLog(container.transform, "Line_Right", spLineRight,
                    rightX, -cornerH, spLineRight.rect.width, innerH, isHorizontal: false);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Right: sprite={spLineRight != null}, innerH={innerH}");
            }

            // ═══════════════════════════════════════════════════════════
            // 6. УГЛЫ (третий слой - ПОВЕРХ ВСЕГО!)
            // ═══════════════════════════════════════════════════════════

            // Левый верхний (LT) - позиция (0, 0)
            if (spCornerLT != null)
            {
                CreateCornerSprite(container.transform, "Corner_LT", spCornerLT, 0, 0);
            }

            // Правый верхний (RT) - позиция (w - spriteWidth, 0)
            if (spCornerRT != null)
            {
                float rtX = w - spCornerRT.rect.width;
                CreateCornerSprite(container.transform, "Corner_RT", spCornerRT, rtX, 0);
            }

            // Левый нижний (LB) - позиция (0, -(h - spriteHeight))
            if (spCornerLB != null)
            {
                float lbY = -(h - spCornerLB.rect.height);
                Debug.Log($"[ListDesk] Corner_LB: h={h}, spriteH={spCornerLB.rect.height}, Y={lbY}");
                CreateCornerSprite(container.transform, "Corner_LB", spCornerLB, 0, lbY);
            }

            // Правый нижний (RB) - позиция (w - spriteWidth, -(h - spriteHeight))
            if (spCornerRB != null)
            {
                float rbX = w - spCornerRB.rect.width;
                float rbY = -(h - spCornerRB.rect.height);
                Debug.Log($"[ListDesk] Corner_RB: X={rbX}, Y={rbY}");
                CreateCornerSprite(container.transform, "Corner_RB", spCornerRB, rbX, rbY);
            }

            Debug.Log($"[ListDesk] Created at ({ld.X},{ld.Y}) size {w}x{h}, fill area {fillW}x{fillH}");
        }

        /// <summary>
        /// Создаёт угловой спрайт с правильным размером и позиционированием
        /// </summary>
        private static void CreateCornerSprite(Transform parent, string name, Sprite sp, float x, float y)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);

            // ИСПОЛЬЗУЕМ РЕАЛЬНЫЙ РАЗМЕР СПРАЙТА!
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.preserveAspect = false;

            // УГЛЫ ПОВЕРХ ВСЕГО!
            go.transform.SetAsLastSibling();

            Debug.Log($"[ListDesk] {name} placed at ({x},{y}), size={sp.rect.width}x{sp.rect.height}");
        }

        /// <summary>
        /// Тайлируемая линия с логированием и перекрытием
        /// </summary>
        private static void CreateTiledLineWithLog(Transform parent, string name, Sprite sp,
            float x, float y, float areaWidth, float areaHeight, bool isHorizontal)
        {
            if (sp == null)
            {
                Debug.LogWarning($"[TiledLine] {name}: sprite is NULL!");
                return;
            }

            Debug.Log($"[TiledLine] {name}: creating at ({x},{y}), area={areaWidth}x{areaHeight}");

            var container = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            container.transform.SetParent(parent, false);

            var containerRt = (RectTransform)container.transform;
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(x, y);
            containerRt.sizeDelta = new Vector2(areaWidth, areaHeight);

            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Перекрытие для устранения субпиксельных щелей
            const float OVERLAP = 0.5f;

            int tilesCreated = 0;

            if (isHorizontal)
            {
                float stepX = Mathf.Max(1f, tileW - OVERLAP);
                int tilesNeeded = Mathf.CeilToInt(areaWidth / stepX) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(i * stepX, 0);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;

                    tilesCreated++;
                }
            }
            else
            {
                float stepY = Mathf.Max(1f, tileH - OVERLAP);
                int tilesNeeded = Mathf.CeilToInt(areaHeight / stepY) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(0, -i * stepY);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;

                    tilesCreated++;
                }
            }

            Debug.Log($"[TiledLine] {name}: created {tilesCreated} tiles");
        }

        /// <summary>
        /// Создаёт тайлируемую линию С ПЕРЕКРЫТИЕМ для устранения щелей
        /// </summary>
        private static void CreateTiledLineManual(Transform parent, string name, Sprite sp,
            float x, float y, float areaWidth, float areaHeight, bool isHorizontal)
        {
            if (sp == null)
            {
                Debug.LogWarning($"[TiledLine] {name}: sprite is NULL!");
                return;
            }

            var container = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            container.transform.SetParent(parent, false);

            var containerRt = (RectTransform)container.transform;
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(x, y);
            containerRt.sizeDelta = new Vector2(areaWidth, areaHeight);

            // Размер тайла
            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // ═══════════════════════════════════════════════════════════
            // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Добавляем перекрытие 1-2 пикселя
            // чтобы компенсировать субпиксельные щели при масштабировании
            // ═══════════════════════════════════════════════════════════
            const float OVERLAP = 1f;

            if (isHorizontal)
            {
                // Шаг меньше чем размер тайла = перекрытие
                float stepX = tileW - OVERLAP;
                int tilesNeeded = Mathf.CeilToInt(areaWidth / stepX) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(i * stepX, 0);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;
                }
            }
            else
            {
                // Вертикальная линия
                float stepY = tileH - OVERLAP;
                int tilesNeeded = Mathf.CeilToInt(areaHeight / stepY) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(0, -i * stepY);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;
                }
            }
        }
        /// <summary>
        /// Использует ВСТРОЕННЫЙ тайлинг Unity — надёжнее при масштабировании Canvas
        /// </summary>
        private static void CreateTiledLineNative(Transform parent, string name, Sprite sp,
            float x, float y, float areaWidth, float areaHeight)
        {
            if (sp == null) return;

            // Контейнер с маской
            var container = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            container.transform.SetParent(parent, false);

            var containerRt = (RectTransform)container.transform;
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(x, y);
            containerRt.sizeDelta = new Vector2(areaWidth, areaHeight);

            // Один Image с типом Tiled
            var imageGO = new GameObject("TiledImage", typeof(RectTransform), typeof(Image));
            imageGO.transform.SetParent(container.transform, false);

            var imageRt = (RectTransform)imageGO.transform;
            imageRt.anchorMin = Vector2.zero;
            imageRt.anchorMax = Vector2.one;
            imageRt.offsetMin = Vector2.zero;
            imageRt.offsetMax = Vector2.zero;

            var img = imageGO.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Tiled;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = 1f;  // ← ВАЖНО!
            img.raycastTarget = false;
        }
        /// <summary>
        /// Создаёт тайлируемую линию используя встроенный Image.Type.Tiled
        /// </summary>
        private static void CreateTiledEdgeNative(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            img.type = Image.Type.Tiled;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = 1f;
        }
         
         

        /// <summary>
        /// Создаёт ГОРИЗОНТАЛЬНУЮ тайлируемую линию
        /// ВАЖНО: используем ТОЧНЫЙ размер спрайта, без растяжения
        /// </summary>
        private static void CreateTiledEdgeHorizontal(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            // ═══════════════════════════════════════════════════════════
            // ТОЧНЫЙ размер спрайта (не параметр height!)
            // ═══════════════════════════════════════════════════════════
            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Сколько тайлов нужно чтобы заполнить ширину + запас
            int tilesNeeded = Mathf.CeilToInt(width / tileW) + 2;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(go.transform, false);

                var trt = (RectTransform)tile.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                // Позиция: каждый тайл вплотную к предыдущему
                trt.anchoredPosition = new Vector2(i * tileW, 0);

                // ТОЧНЫЙ размер спрайта — без растяжения!
                trt.sizeDelta = new Vector2(tileW, tileH);

                var img = tile.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;  // Размер уже точный
                img.raycastTarget = false;
            }
        }

        /// <summary>
        /// Создаёт ВЕРТИКАЛЬНУЮ тайлируемую линию
        /// ВАЖНО: используем ТОЧНЫЙ размер спрайта, без растяжения
        /// </summary>
        private static void CreateTiledEdgeVertical(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            // ═══════════════════════════════════════════════════════════
            // ТОЧНЫЙ размер спрайта
            // ═══════════════════════════════════════════════════════════
            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Сколько тайлов нужно чтобы заполнить высоту + запас
            int tilesNeeded = Mathf.CeilToInt(height / tileH) + 2;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(go.transform, false);

                var trt = (RectTransform)tile.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                // Позиция: каждый тайл вплотную к предыдущему (вниз)
                trt.anchoredPosition = new Vector2(0, -i * tileH);

                // ТОЧНЫЙ размер спрайта
                trt.sizeDelta = new Vector2(tileW, tileH);

                var img = tile.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
            }
        }


        /// <summary>
        /// Диагностика размеров спрайтов границы
        /// </summary>
        private static void DebugBorderSprites(string folder)
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("[BORDER DEBUG] Checking sprite sizes:");

            for (int i = 0; i <= 11; i++)
            {
                var sp = LoadSpriteFromResources(folder, $"frame_{i:D4}");
                if (sp != null)
                {
                    var tex = sp.texture;
                    Debug.Log($"  frame_{i:D4}: " +
                        $"sprite.rect = {sp.rect.width}x{sp.rect.height}, " +
                        $"texture = {tex.width}x{tex.height}, " +
                        $"PPU = {sp.pixelsPerUnit}");
                }
                else
                {
                    Debug.Log($"  frame_{i:D4}: NOT FOUND");
                }
            }

            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Размещает один спрайт (угол)
        /// </summary>
        private static void PlaceSpriteInternal(Transform parent, string name, Sprite sp, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            // Углы должны быть поверх всего
            go.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Создаёт тайлируемую линию (горизонтальную или вертикальную)
        /// </summary>
        private static void CreateTiledEdgeInternal(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Определяем направление тайлинга
            bool horizontal = width > height;

            int tilesNeeded;
            if (horizontal)
                tilesNeeded = Mathf.CeilToInt(width / tileW) + 1;
            else
                tilesNeeded = Mathf.CeilToInt(height / tileH) + 1;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(go.transform, false);

                var trt = (RectTransform)tile.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                if (horizontal)
                    trt.anchoredPosition = new Vector2(i * tileW, 0);
                else
                    trt.anchoredPosition = new Vector2(0, -i * tileH);

                trt.sizeDelta = new Vector2(tileW, tileH);

                var img = tile.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
            }
        }

        private static void PlaceSprite(Transform parent, string name, Sprite sp, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            go.transform.SetAsLastSibling();
        }

        private static void CreateTiledEdge(Transform parent, string name, Sprite sp, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
        }

        private static void CreateSprite(Transform parent, string name, Sprite sp, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            go.transform.SetAsLastSibling();
        }

        private static void CreateTiledLine(Transform parent, string name, Sprite sp, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт бордер BD из спрайтов (12 фреймов)
        /// Структура BD: 
        /// 0-3: углы (TL, TR, BL, BR)
        /// 4-7: стороны для тайлинга (Top, Right, Bottom, Left)
        /// 8-11: дополнительные элементы
        /// </summary>
        private static void CreateBorderBD(Transform parent, float width, float height)
        {
            const string folder = "interf3_elements_border_BD_frames";

            // Загружаем спрайты углов
            Sprite spTL = LoadSpriteFromResources(folder, "frame_0000"); // Top-Left
            Sprite spTR = LoadSpriteFromResources(folder, "frame_0001"); // Top-Right
            Sprite spBL = LoadSpriteFromResources(folder, "frame_0002"); // Bottom-Left
            Sprite spBR = LoadSpriteFromResources(folder, "frame_0003"); // Bottom-Right

            // Загружаем спрайты сторон для тайлинга
            Sprite spTop = LoadSpriteFromResources(folder, "frame_0004");    // Top edge
            Sprite spRight = LoadSpriteFromResources(folder, "frame_0005");  // Right edge
            Sprite spBottom = LoadSpriteFromResources(folder, "frame_0006"); // Bottom edge
            Sprite spLeft = LoadSpriteFromResources(folder, "frame_0007");   // Left edge

            // Проверяем загрузку
            bool hasSprites = spTL != null || spTop != null;

            if (!hasSprites)
            {
                Debug.LogWarning($"[BorderBD] No sprites found in {folder}, using fallback");
                CreateBorderFallback(parent, width, height);
                return;
            }

            // Размеры углов
            float cornerW = spTL != null ? spTL.rect.width : 8f;
            float cornerH = spTL != null ? spTL.rect.height : 8f;

            // ═══════════════════════════════════════════════════════════
            // Верхняя сторона (тайлится между углами)
            // ═══════════════════════════════════════════════════════════
            if (spTop != null)
            {
                var topGO = new GameObject("Border_Top", typeof(RectTransform), typeof(Image));
                topGO.transform.SetParent(parent, false);

                var topRt = (RectTransform)topGO.transform;
                topRt.anchorMin = topRt.anchorMax = new Vector2(0, 1);
                topRt.pivot = new Vector2(0, 1);
                topRt.anchoredPosition = new Vector2(cornerW, 0);
                topRt.sizeDelta = new Vector2(width - cornerW * 2, spTop.rect.height);

                var topImg = topGO.GetComponent<Image>();
                topImg.sprite = spTop;
                topImg.type = Image.Type.Tiled;
                topImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Нижняя сторона
            // ═══════════════════════════════════════════════════════════
            if (spBottom != null)
            {
                var bottomGO = new GameObject("Border_Bottom", typeof(RectTransform), typeof(Image));
                bottomGO.transform.SetParent(parent, false);

                var bottomRt = (RectTransform)bottomGO.transform;
                bottomRt.anchorMin = bottomRt.anchorMax = new Vector2(0, 1);
                bottomRt.pivot = new Vector2(0, 1);
                bottomRt.anchoredPosition = new Vector2(cornerW, -height + spBottom.rect.height);
                bottomRt.sizeDelta = new Vector2(width - cornerW * 2, spBottom.rect.height);

                var bottomImg = bottomGO.GetComponent<Image>();
                bottomImg.sprite = spBottom;
                bottomImg.type = Image.Type.Tiled;
                bottomImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Левая сторона
            // ═══════════════════════════════════════════════════════════
            if (spLeft != null)
            {
                var leftGO = new GameObject("Border_Left", typeof(RectTransform), typeof(Image));
                leftGO.transform.SetParent(parent, false);

                var leftRt = (RectTransform)leftGO.transform;
                leftRt.anchorMin = leftRt.anchorMax = new Vector2(0, 1);
                leftRt.pivot = new Vector2(0, 1);
                leftRt.anchoredPosition = new Vector2(0, -cornerH);
                leftRt.sizeDelta = new Vector2(spLeft.rect.width, height - cornerH * 2);

                var leftImg = leftGO.GetComponent<Image>();
                leftImg.sprite = spLeft;
                leftImg.type = Image.Type.Tiled;
                leftImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Правая сторона
            // ═══════════════════════════════════════════════════════════
            if (spRight != null)
            {
                var rightGO = new GameObject("Border_Right", typeof(RectTransform), typeof(Image));
                rightGO.transform.SetParent(parent, false);

                var rightRt = (RectTransform)rightGO.transform;
                rightRt.anchorMin = rightRt.anchorMax = new Vector2(0, 1);
                rightRt.pivot = new Vector2(0, 1);
                rightRt.anchoredPosition = new Vector2(width - spRight.rect.width, -cornerH);
                rightRt.sizeDelta = new Vector2(spRight.rect.width, height - cornerH * 2);

                var rightImg = rightGO.GetComponent<Image>();
                rightImg.sprite = spRight;
                rightImg.type = Image.Type.Tiled;
                rightImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Углы (поверх сторон)
            // ═══════════════════════════════════════════════════════════

            // Top-Left
            if (spTL != null)
            {
                CreateCorner(parent, "Corner_TL", spTL, 0, 0);
            }

            // Top-Right
            if (spTR != null)
            {
                CreateCorner(parent, "Corner_TR", spTR, width - spTR.rect.width, 0);
            }

            // Bottom-Left
            if (spBL != null)
            {
                CreateCorner(parent, "Corner_BL", spBL, 0, -height + spBL.rect.height);
            }

            // Bottom-Right
            if (spBR != null)
            {
                CreateCorner(parent, "Corner_BR", spBR, width - spBR.rect.width, -height + spBR.rect.height);
            }
        }

        private static void CreateCorner(Transform parent, string name, Sprite sprite, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.preserveAspect = false;

            // Углы должны быть поверх сторон
            go.transform.SetAsLastSibling();
        }

        private static void CreateBorderFallback(Transform parent, float width, float height)
        {
            Color borderColor = new Color(0.45f, 0.38f, 0.28f, 0.95f);
            float thickness = 3f;

            // Top
            CreateSimpleLine(parent, "Border_Top", 0, 0, width, thickness, borderColor);
            // Bottom
            CreateSimpleLine(parent, "Border_Bottom", 0, height - thickness, width, thickness, borderColor);
            // Left
            CreateSimpleLine(parent, "Border_Left", 0, 0, thickness, height, borderColor);
            // Right
            CreateSimpleLine(parent, "Border_Right", width - thickness, 0, thickness, height, borderColor);
        }

        private static void CreateSimpleLine(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт вертикальный скроллер из спрайтов Scroll3
        /// Структура: 0=track, 1=thumb, 2=arrowUp, 3=arrowDown, 4-7=hover states
        /// </summary>
        private static Scrollbar CreateScroll3(Transform parent, float containerWidth, float containerHeight, float scrollerWidth, float padding)
        {
            const string folder = "Interf3_elements_scroll3_frames";

            // Загружаем спрайты
            Sprite spTrack = LoadSpriteFromResources(folder, "frame_0000");
            Sprite spThumb = LoadSpriteFromResources(folder, "frame_0001");
            Sprite spArrowUp = LoadSpriteFromResources(folder, "frame_0002");
            Sprite spArrowDown = LoadSpriteFromResources(folder, "frame_0003");
            Sprite spArrowUpHover = LoadSpriteFromResources(folder, "frame_0004");
            Sprite spArrowDownHover = LoadSpriteFromResources(folder, "frame_0005");
            Sprite spThumbHover = LoadSpriteFromResources(folder, "frame_0006");

            bool hasSprites = spTrack != null || spThumb != null;

            // ═══════════════════════════════════════════════════════════
            // Контейнер скроллера
            // ═══════════════════════════════════════════════════════════
            var scrollbarGO = new GameObject("VScrollbar", typeof(RectTransform), typeof(Image));
            scrollbarGO.transform.SetParent(parent, false);

            var scrollbarRt = (RectTransform)scrollbarGO.transform;
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 0.5f);
            scrollbarRt.anchoredPosition = new Vector2(-padding, 0);
            scrollbarRt.sizeDelta = new Vector2(scrollerWidth, -(padding * 2));

            var scrollbarImg = scrollbarGO.GetComponent<Image>();
            if (spTrack != null)
            {
                scrollbarImg.sprite = spTrack;
                scrollbarImg.type = Image.Type.Sliced;
            }
            else
            {
                scrollbarImg.color = new Color(0.12f, 0.10f, 0.08f, 0.95f);
            }
            scrollbarImg.raycastTarget = true;

            // ═══════════════════════════════════════════════════════════
            // Размер стрелок
            // ═══════════════════════════════════════════════════════════
            float arrowHeight = spArrowUp != null ? spArrowUp.rect.height : scrollerWidth;
            float arrowWidth = spArrowUp != null ? spArrowUp.rect.width : scrollerWidth;

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вверх"
            // ═══════════════════════════════════════════════════════════
            var arrowUpGO = new GameObject("ArrowUp", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowUpGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowUpRt = (RectTransform)arrowUpGO.transform;
            arrowUpRt.anchorMin = new Vector2(0.5f, 1);
            arrowUpRt.anchorMax = new Vector2(0.5f, 1);
            arrowUpRt.pivot = new Vector2(0.5f, 1);
            arrowUpRt.anchoredPosition = new Vector2(0, 0);
            arrowUpRt.sizeDelta = new Vector2(arrowWidth, arrowHeight);

            var arrowUpImg = arrowUpGO.GetComponent<Image>();
            if (spArrowUp != null)
            {
                arrowUpImg.sprite = spArrowUp;
                arrowUpImg.type = Image.Type.Simple;
                arrowUpImg.preserveAspect = false;

                // Добавляем hover эффект
                if (spArrowUpHover != null)
                {
                    var hoverUp = arrowUpGO.AddComponent<ScrollArrowHover>();
                    hoverUp.Normal = spArrowUp;
                    hoverUp.Hover = spArrowUpHover;
                    hoverUp.Img = arrowUpImg;
                }
            }
            else
            {
                arrowUpImg.color = new Color(0.35f, 0.30f, 0.25f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вниз"
            // ═══════════════════════════════════════════════════════════
            var arrowDownGO = new GameObject("ArrowDown", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowDownGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowDownRt = (RectTransform)arrowDownGO.transform;
            arrowDownRt.anchorMin = new Vector2(0.5f, 0);
            arrowDownRt.anchorMax = new Vector2(0.5f, 0);
            arrowDownRt.pivot = new Vector2(0.5f, 0);
            arrowDownRt.anchoredPosition = new Vector2(0, 0);
            arrowDownRt.sizeDelta = new Vector2(arrowWidth, arrowHeight);

            var arrowDownImg = arrowDownGO.GetComponent<Image>();
            if (spArrowDown != null)
            {
                arrowDownImg.sprite = spArrowDown;
                arrowDownImg.type = Image.Type.Simple;
                arrowDownImg.preserveAspect = false;

                if (spArrowDownHover != null)
                {
                    var hoverDown = arrowDownGO.AddComponent<ScrollArrowHover>();
                    hoverDown.Normal = spArrowDown;
                    hoverDown.Hover = spArrowDownHover;
                    hoverDown.Img = arrowDownImg;
                }
            }
            else
            {
                arrowDownImg.color = new Color(0.35f, 0.30f, 0.25f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Sliding Area (между стрелками)
            // ═══════════════════════════════════════════════════════════
            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarGO.transform, false);

            var slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(1, arrowHeight + 2);
            slidingRt.offsetMax = new Vector2(-1, -arrowHeight - 2);

            // ═══════════════════════════════════════════════════════════
            // Handle (ползунок)
            // ═══════════════════════════════════════════════════════════
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = new Vector2(0, 0);
            handleRt.anchorMax = new Vector2(1, 1);
            handleRt.offsetMin = new Vector2(1, 0);
            handleRt.offsetMax = new Vector2(-1, 0);

            var handleImg = handle.GetComponent<Image>();
            if (spThumb != null)
            {
                handleImg.sprite = spThumb;
                handleImg.type = Image.Type.Sliced;

                if (spThumbHover != null)
                {
                    var hoverThumb = handle.AddComponent<ScrollArrowHover>();
                    hoverThumb.Normal = spThumb;
                    hoverThumb.Hover = spThumbHover;
                    hoverThumb.Img = handleImg;
                }
            }
            else
            {
                handleImg.color = new Color(0.55f, 0.48f, 0.38f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Scrollbar компонент
            // ═══════════════════════════════════════════════════════════
            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            scrollbar.value = 1f;

            // ═══════════════════════════════════════════════════════════
            // Подключаем кнопки стрелок к скроллу
            // ═══════════════════════════════════════════════════════════
            var arrowController = scrollbarGO.AddComponent<ScrollArrowController>();
            arrowController.Scrollbar = scrollbar;
            arrowController.ArrowUp = arrowUpGO.GetComponent<Button>();
            arrowController.ArrowDown = arrowDownGO.GetComponent<Button>();
            arrowController.ScrollStep = 0.1f;

            return scrollbar;
        }

        /// <summary>
        /// Hover эффект для элементов скроллера
        /// </summary>
        private sealed class ScrollArrowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Sprite Normal;
            public Sprite Hover;
            public Image Img;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Img != null && Hover != null) Img.sprite = Hover;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (Img != null && Normal != null) Img.sprite = Normal;
            }
        }

        /// <summary>
        /// Контроллер кнопок-стрелок скроллера
        /// </summary>
        private sealed class ScrollArrowController : MonoBehaviour
        {
            public Scrollbar Scrollbar;
            public Button ArrowUp;
            public Button ArrowDown;
            public float ScrollStep = 0.1f;

            private void Awake()
            {
                if (ArrowUp != null)
                {
                    ArrowUp.onClick.AddListener(() =>
                    {
                        if (Scrollbar != null)
                            Scrollbar.value = Mathf.Clamp01(Scrollbar.value + ScrollStep);
                    });
                }

                if (ArrowDown != null)
                {
                    ArrowDown.onClick.AddListener(() =>
                    {
                        if (Scrollbar != null)
                            Scrollbar.value = Mathf.Clamp01(Scrollbar.value - ScrollStep);
                    });
                }
            }
        }

        /// <summary>
        /// Создаёт бордер (рамку) вокруг ListDesk
        /// </summary>
        private static void CreateListDeskBorder(Transform parent, float width, float height)
        {
            // Пробуем загрузить спрайты бордера
            const string borderFolder = "interf3_elements_borders_frames";

            // Попробуем разные варианты
            Sprite spTop = LoadSpriteFromResources(borderFolder, "frame_0000");
            Sprite spBottom = LoadSpriteFromResources(borderFolder, "frame_0001");
            Sprite spLeft = LoadSpriteFromResources(borderFolder, "frame_0002");
            Sprite spRight = LoadSpriteFromResources(borderFolder, "frame_0003");

            float borderThickness = 2f;

            // Если спрайтов нет - рисуем простую рамку цветом
            if (spTop == null)
            {
                // Верхняя линия
                CreateBorderLine(parent, "BorderTop", 0, 0, width, borderThickness);
                // Нижняя линия
                CreateBorderLine(parent, "BorderBottom", 0, height - borderThickness, width, borderThickness);
                // Левая линия
                CreateBorderLine(parent, "BorderLeft", 0, 0, borderThickness, height);
                // Правая линия
                CreateBorderLine(parent, "BorderRight", width - borderThickness, 0, borderThickness, height);
            }
            else
            {
                // Если есть спрайты - используем их (TODO: реализовать когда будут спрайты)
            }
        }

        private static void CreateBorderLine(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.4f, 0.35f, 0.25f, 0.9f); // Коричневатый цвет рамки
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт вертикальный скроллер для ListDesk
        /// </summary>
        private static Scrollbar CreateListDeskScrollbar(Transform parent, float containerWidth, float containerHeight, float scrollerWidth, float padding)
        {
            const string scrollFolder = "interf3_elements_scroll3_frames";

            // Загружаем спрайты скроллера
            Sprite spTrack = LoadSpriteFromResources(scrollFolder, "frame_0000");     // Фон трека
            Sprite spThumb = LoadSpriteFromResources(scrollFolder, "frame_0001");     // Ползунок
            Sprite spArrowUp = LoadSpriteFromResources(scrollFolder, "frame_0002");   // Стрелка вверх
            Sprite spArrowDown = LoadSpriteFromResources(scrollFolder, "frame_0003"); // Стрелка вниз

            // ═══════════════════════════════════════════════════════════
            // Основной контейнер скроллера
            // ═══════════════════════════════════════════════════════════
            var scrollbarGO = new GameObject("VScrollbar", typeof(RectTransform), typeof(Image));
            scrollbarGO.transform.SetParent(parent, false);

            var scrollbarRt = (RectTransform)scrollbarGO.transform;
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 1);
            scrollbarRt.anchoredPosition = new Vector2(-padding, -padding);
            scrollbarRt.sizeDelta = new Vector2(scrollerWidth, -(padding * 2));

            var scrollbarImg = scrollbarGO.GetComponent<Image>();
            if (spTrack != null)
            {
                scrollbarImg.sprite = spTrack;
                scrollbarImg.type = Image.Type.Sliced;
            }
            else
            {
                scrollbarImg.color = new Color(0.15f, 0.12f, 0.1f, 0.9f); // Тёмный фон
            }
            scrollbarImg.raycastTarget = true;

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вверх"
            // ═══════════════════════════════════════════════════════════
            float arrowHeight = scrollerWidth; // Квадратные кнопки

            var arrowUpGO = new GameObject("ArrowUp", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowUpGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowUpRt = (RectTransform)arrowUpGO.transform;
            arrowUpRt.anchorMin = new Vector2(0, 1);
            arrowUpRt.anchorMax = new Vector2(1, 1);
            arrowUpRt.pivot = new Vector2(0.5f, 1);
            arrowUpRt.anchoredPosition = new Vector2(0, 0);
            arrowUpRt.sizeDelta = new Vector2(0, arrowHeight);

            var arrowUpImg = arrowUpGO.GetComponent<Image>();
            if (spArrowUp != null)
            {
                arrowUpImg.sprite = spArrowUp;
                arrowUpImg.type = Image.Type.Simple;
            }
            else
            {
                arrowUpImg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вниз"
            // ═══════════════════════════════════════════════════════════
            var arrowDownGO = new GameObject("ArrowDown", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowDownGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowDownRt = (RectTransform)arrowDownGO.transform;
            arrowDownRt.anchorMin = new Vector2(0, 0);
            arrowDownRt.anchorMax = new Vector2(1, 0);
            arrowDownRt.pivot = new Vector2(0.5f, 0);
            arrowDownRt.anchoredPosition = new Vector2(0, 0);
            arrowDownRt.sizeDelta = new Vector2(0, arrowHeight);

            var arrowDownImg = arrowDownGO.GetComponent<Image>();
            if (spArrowDown != null)
            {
                arrowDownImg.sprite = spArrowDown;
                arrowDownImg.type = Image.Type.Simple;
            }
            else
            {
                arrowDownImg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Sliding Area (между стрелками)
            // ═══════════════════════════════════════════════════════════
            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarGO.transform, false);

            var slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(0, arrowHeight + 2);
            slidingRt.offsetMax = new Vector2(0, -arrowHeight - 2);

            // ═══════════════════════════════════════════════════════════
            // Handle (ползунок)
            // ═══════════════════════════════════════════════════════════
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = new Vector2(0, 0);
            handleRt.anchorMax = new Vector2(1, 1);
            handleRt.offsetMin = new Vector2(2, 0);
            handleRt.offsetMax = new Vector2(-2, 0);

            var handleImg = handle.GetComponent<Image>();
            if (spThumb != null)
            {
                handleImg.sprite = spThumb;
                handleImg.type = Image.Type.Sliced;
            }
            else
            {
                handleImg.color = new Color(0.5f, 0.45f, 0.35f, 1f); // Светлее фона
            }

            // ═══════════════════════════════════════════════════════════
            // Scrollbar компонент
            // ═══════════════════════════════════════════════════════════
            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            scrollbar.value = 1f; // Начинаем сверху

            return scrollbar;
        }

        /// <summary>
        /// Создаёт вертикальный скроллер
        /// </summary>
        private static Scrollbar CreateVerticalScrollbar(Transform parent, float containerWidth, float containerHeight, float scrollerWidth)
        {
            const string folder = "interf3_elements_scroll_frames";

            var scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGO.transform.SetParent(parent, false);

            var scrollbarRt = (RectTransform)scrollbarGO.transform;
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 1);
            scrollbarRt.anchoredPosition = Vector2.zero;
            scrollbarRt.sizeDelta = new Vector2(scrollerWidth, 0);

            var scrollbarImg = scrollbarGO.GetComponent<Image>();
            scrollbarImg.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);

            // Sliding Area
            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarGO.transform, false);

            var slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(0, 2);
            slidingRt.offsetMax = new Vector2(0, -2);

            // Handle
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            var handleImg = handle.GetComponent<Image>();

            // Пробуем загрузить спрайт скроллера
            var scrollSprite = LoadSpriteFromResources(folder, "frame_0004");
            if (scrollSprite != null)
            {
                handleImg.sprite = scrollSprite;
                handleImg.type = Image.Type.Sliced;
            }
            else
            {
                handleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;

            return scrollbar;
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

        /// <summary>
        /// Рисует VitButton как тайловую линию по логике DrawHeaderEx2 из оригинала.
        /// Спрайты: CSpr (L), CSpr+1 (R), CSpr+2/+3/+4 (Center 1/2/3)
        /// </summary>
        private static void CreateVitButtonTiled(UiVitButton vb, RectTransform parent)
        {
            if (vb == null || parent == null) return;

            // ═══════════════════════════════════════════════════════════
            // 1. Путь к ресурсам (lowercase для совместимости)
            // ═══════════════════════════════════════════════════════════
            string resPath = (vb.GP_File ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToLowerInvariant() + "_frames";  // ← LOWERCASE!

            int baseSpr = vb.SpritePassive;

            Debug.Log($"[VitButtonTiled] GP={vb.GP_File}, resPath={resPath}, baseSpr={baseSpr}, " +
                      $"pos=({vb.X},{vb.Y}), size=({vb.Width}x{vb.Height}), OneSprited={vb.OneSprited}");

            // ═══════════════════════════════════════════════════════════
            // 2. Загружаем спрайты согласно логике DrawHeaderEx2
            // ═══════════════════════════════════════════════════════════
            Sprite spL, spR, spC1, spC2, spC3;

            if (vb.OneSprited)
            {
                // OneSprited: все центры = baseSpr, краёв нет
                spL = null;
                spR = null;
                spC1 = spC2 = spC3 = LoadSpriteFromResources(resPath, $"frame_{baseSpr:0000}");
            }
            else
            {
                // Стандартная логика: L=baseSpr, R=baseSpr+1, C1/C2/C3=baseSpr+2/3/4
                spL = LoadSpriteFromResources(resPath, $"frame_{baseSpr:0000}");
                spR = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 1:0000}");
                spC1 = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 2:0000}");
                spC2 = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 3:0000}");
                spC3 = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 4:0000}");
            }

            Debug.Log($"[VitButtonTiled] Sprites loaded: L={spL != null}, R={spR != null}, " +
                      $"C1={spC1 != null}, C2={spC2 != null}, C3={spC3 != null}");

            // Fallback
            if (spC1 == null) spC1 = spL;
            if (spC2 == null) spC2 = spC1;
            if (spC3 == null) spC3 = spC1;

            if (spC1 == null)
            {
                Debug.LogError($"[VitButtonTiled] No sprites found for {resPath}!");
                return;
            }

            Sprite[] centerSprites = { spC1, spC2, spC3 };

            // Размеры
            float widthL = spL != null ? spL.rect.width : 0f;
            float widthR = spR != null ? spR.rect.width : 0f;
            float centerTileW = spC1.rect.width;
            float height = vb.Height > 0 ? vb.Height : spC1.rect.height;
            float totalWidth = vb.Width > 0 ? vb.Width : 200f;

            // ═══════════════════════════════════════════════════════════
            // 3. Корневой контейнер
            // ═══════════════════════════════════════════════════════════
            var root = new GameObject($"VitLine_{SafeName(vb.Name)}", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0, 1);
            rootRt.pivot = new Vector2(0, 1);
            rootRt.anchoredPosition = new Vector2(vb.X, -vb.Y);
            rootRt.sizeDelta = new Vector2(totalWidth, height);

            // ═══════════════════════════════════════════════════════════
            // 4. Центр с МАСКОЙ (аналог IntersectWindows)
            // ═══════════════════════════════════════════════════════════
            float centerStartX = widthL;
            float centerEndX = totalWidth - widthR;
            float centerWidth = Mathf.Max(0f, centerEndX - centerStartX);

            var centerContainer = new GameObject("CenterMask", typeof(RectTransform), typeof(RectMask2D));
            centerContainer.transform.SetParent(root.transform, false);

            var centerRt = (RectTransform)centerContainer.transform;
            centerRt.anchorMin = centerRt.anchorMax = new Vector2(0, 1);
            centerRt.pivot = new Vector2(0, 1);
            centerRt.anchoredPosition = new Vector2(centerStartX, 0);
            centerRt.sizeDelta = new Vector2(centerWidth, height);

            // ═══════════════════════════════════════════════════════════
            // 5. Тайлим центр с ЧЕРЕДОВАНИЕМ (i % 3)
            // ═══════════════════════════════════════════════════════════
            float xPos = 0f;
            int tileIndex = 0;
            int maxTiles = 300;

            while (xPos < centerWidth && tileIndex < maxTiles)
            {
                Sprite tileSp = centerSprites[tileIndex % 3];
                float tileW = tileSp != null ? tileSp.rect.width : centerTileW;

                var tileGO = new GameObject($"Tile_{tileIndex}", typeof(RectTransform), typeof(Image));
                tileGO.transform.SetParent(centerContainer.transform, false);

                var tileRt = (RectTransform)tileGO.transform;
                tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                tileRt.pivot = new Vector2(0, 1);
                tileRt.anchoredPosition = new Vector2(xPos, 0);
                tileRt.sizeDelta = new Vector2(tileW, height);

                var tileImg = tileGO.GetComponent<Image>();
                tileImg.sprite = tileSp;
                tileImg.type = Image.Type.Simple;
                tileImg.raycastTarget = false;
                tileImg.preserveAspect = false;

                xPos += tileW;
                tileIndex++;
            }

            Debug.Log($"[VitButtonTiled] Created {tileIndex} center tiles");

            // ═══════════════════════════════════════════════════════════
            // 6. Левый край ПОВЕРХ
            // ═══════════════════════════════════════════════════════════
            if (spL != null && widthL > 0)
            {
                var leftGO = new GameObject("EdgeL", typeof(RectTransform), typeof(Image));
                leftGO.transform.SetParent(root.transform, false);

                var leftRt = (RectTransform)leftGO.transform;
                leftRt.anchorMin = leftRt.anchorMax = new Vector2(0, 1);
                leftRt.pivot = new Vector2(0, 1);
                leftRt.anchoredPosition = new Vector2(0, 0);
                leftRt.sizeDelta = new Vector2(widthL, spL.rect.height);

                var leftImg = leftGO.GetComponent<Image>();
                leftImg.sprite = spL;
                leftImg.type = Image.Type.Simple;
                leftImg.raycastTarget = false;

                leftGO.transform.SetAsLastSibling();
            }

            // ═══════════════════════════════════════════════════════════
            // 7. Правый край ПОВЕРХ
            // ═══════════════════════════════════════════════════════════
            if (spR != null && widthR > 0)
            {
                var rightGO = new GameObject("EdgeR", typeof(RectTransform), typeof(Image));
                rightGO.transform.SetParent(root.transform, false);

                var rightRt = (RectTransform)rightGO.transform;
                rightRt.anchorMin = rightRt.anchorMax = new Vector2(0, 1);
                rightRt.pivot = new Vector2(0, 1);
                rightRt.anchoredPosition = new Vector2(totalWidth - widthR, 0);
                rightRt.sizeDelta = new Vector2(widthR, spR.rect.height);

                var rightImg = rightGO.GetComponent<Image>();
                rightImg.sprite = spR;
                rightImg.type = Image.Type.Simple;
                rightImg.raycastTarget = false;

                rightGO.transform.SetAsLastSibling();
            }

            Debug.Log($"[VitButtonTiled] Complete: L={widthL > 0}, R={widthR > 0}, tiles={tileIndex}");
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
                bool hasNavBack =
                    btn.Actions != null && btn.Actions.Exists(x =>
                        x != null && (x.Name == "cva_MM_MultiBack" || x.Name == "cva_MM_Back"));

                foreach (var a in btn.Actions)
                {
                    if (a == null) continue;

                    // ВАЖНО: на кнопке Back в Multi есть и MultiBack и Close — Close надо скипнуть
                    if (hasNavBack && a.Name == "cva_MM_Close")
                        continue;

                    try { sink?.OnAction(btn.MessageKey, a); }
                    catch (Exception e) { Debug.LogError($"[BaseRenderer] Action error: {e}"); }
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
