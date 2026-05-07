using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2GameplayHudV1 : MonoBehaviour
    {
        private const string Contract = "V2_SELECTED_PEASANT_LEFT_BOTTOM_PANEL_XML_G16_LEGACY_RUNTIME_FONT";
        private static C2GameplayHudV1 _active;

        private Canvas _canvas;
        private RectTransform _root;
        private readonly List<GameObject> _spawned = new List<GameObject>(128);
        private bool _visible;
        private int _lastSelectedCount = -1;
        private float _nextRefresh;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _lastUnit;
        private static Font _cachedRuntimeFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (_active != null) return;
            GameObject go = new GameObject("C2_GameplayHud_SelectedPanel_V1");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _active = go.AddComponent<C2GameplayHudV1>();
        }

        private void Awake()
        {
            _active = this;
            EnsureCanvas();
            Debug.Log("[C2:GAMEPLAY HUD V2] installed contract=" + Contract + " xml=Dialogs/v/UnitProduce + GlBuildSel + G16 sprites");
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < _nextRefresh) return;
            _nextRefresh = Time.realtimeSinceStartup + 0.20f;

            C2NeutralPeasantUnitInfoV2LikeOriginal unit = FirstSelectedUnit();
            int count = CountSelectedUnits();
            bool shouldShow = unit != null && HasBattleCamera();

            if (!shouldShow)
            {
                if (_visible) SetVisible(false);
                _lastSelectedCount = -1;
                _lastUnit = null;
                return;
            }

            if (!_visible || _lastSelectedCount != count || _lastUnit != unit)
            {
                Rebuild(unit, count);
                _lastSelectedCount = count;
                _lastUnit = unit;
                SetVisible(true);
            }
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            GameObject cgo = new GameObject("C2_GameplayHud_Canvas_V1");
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32740;
            CanvasScaler scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 768);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1.0f;
            cgo.AddComponent<GraphicRaycaster>();

            GameObject rootGo = new GameObject("C2_GameplayHud_Root_V1");
            rootGo.transform.SetParent(cgo.transform, false);
            _root = rootGo.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0, 1);
            _root.anchorMax = new Vector2(0, 1);
            _root.pivot = new Vector2(0, 1);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(1024, 768);
        }

        private void Rebuild(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int selectedCount)
        {
            EnsureCanvas();
            ClearSpawned();

            // Original panel anchors are stored in Dialogs/v/*.xml in 1024x768 top-left coordinates.
            // UnitProduce has relative art; GlBuildSel gives the left-bottom building selector anchor.
            RenderDialogFile("Dialogs/v/GlBuildSel.GPPicture.Dialogs.xml", 0, 0, 96);
            RenderDialogFile("Dialogs/v/UnitProduce.GPPicture.Dialogs.xml", 8, 632, 96);

            // Practical visible composition from the same original packages.
            AddG16Image("portrait_back", "Interf3\\FormInterface", 22, 7, 634, 72, 126, 255);
            AddG16Image("portrait_unit", "Interf3\\Units_Egp_mini", 0, 13, 642, 56, 118, 255);
            AddG16Image("nation_flag", "INTERF3\\FLAG", Mathf.Clamp(unit != null ? unit.Nation : 0, 0, 31), 9, 637, 22, 15, 255);
            AddSolid("hp_vertical_red", new Color(0.85f, 0.04f, 0.02f, 0.95f), 82, 662, 8, 86);
            AddSolid("hp_vertical_dark_cut", new Color(0.08f, 0.0f, 0.0f, 0.65f), 84, 666, 4, 66);
            AddLabel("unit_name", unit != null ? (unit.SourceMonsterId ?? "Unit") : "Unit", 25, 620, 95, 16, 12, TextAnchor.MiddleCenter, Color.white);
            AddLabel("selected_count", selectedCount.ToString(CultureInfo.InvariantCulture), 48, 610, 28, 12, 10, TextAnchor.MiddleCenter, Color.white);

            BuildBuildingIconGrid();

            Debug.Log("[C2:GAMEPLAY HUD V2] rebuilt selected=" + selectedCount.ToString(CultureInfo.InvariantCulture) +
                      " unit='" + (unit != null ? unit.SourceMonsterId : "<null>") +
                      "' resolvedMd='" + (unit != null ? unit.ResolvedMd : "<null>") +
                      "' contract=" + Contract);
        }

        private void BuildBuildingIconGrid()
        {
            int startX = 98;
            int startY = 628;
            int cellW = 42;
            int cellH = 47;
            int icon = 0;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    int x = startX + col * cellW;
                    int y = startY + row * cellH;
                    AddSolid("build_cell_back_" + icon.ToString(), new Color(0.04f, 0.035f, 0.02f, 0.55f), x, y, 38, 42);
                    AddG16Image("build_icon_" + icon.ToString(), "Interf3\\BldSmallIcons", icon, x + 2, y + 2, 34, 34, 150);
                    AddSolid("build_cell_disabled_" + icon.ToString(), new Color(0.0f, 0.0f, 0.0f, 0.45f), x + 1, y + 1, 36, 40);
                    icon++;
                }
            }
        }

        private void RenderDialogFile(string relPath, int addX, int addY, int alpha)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Cossacks2/Data", relPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return;

            try
            {
                string text = File.ReadAllText(path, Encoding.Default);
                DialogNode root = DialogNode.Parse(text);
                RenderNode(root, addX, addY, alpha, 0);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:GAMEPLAY HUD V2] xml render failed rel='" + relPath + "' err=" + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void RenderNode(DialogNode node, int ox, int oy, int alpha, int depth)
        {
            if (node == null || depth > 8) return;

            int x = ox + node.Int("x", 0);
            int y = oy + node.Int("y", 0);
            int w = node.Int("Width", 0);
            int h = node.Int("Height", 0);
            string visible = node.TextOf("Visible");
            if (string.Equals(visible, "false", StringComparison.OrdinalIgnoreCase)) return;

            if (string.Equals(node.Name, "GPPicture", StringComparison.OrdinalIgnoreCase))
            {
                string fileId = node.TextOf("FileID");
                int spr = node.Int("SpriteID", 0);
                if (!string.IsNullOrWhiteSpace(fileId) && w > 0 && h > 0)
                    AddG16Image("xml_" + San(fileId) + "_" + spr.ToString(), fileId, spr, x, y, w, h, alpha);
            }

            for (int i = 0; i < node.Children.Count; i++)
                RenderNode(node.Children[i], x, y, alpha, depth + 1);
        }

        private Image AddG16Image(string name, string fileId, int spriteId, int x, int y, int w, int h, int alpha)
        {
            Sprite sp = C2GameplayOriginalSpriteCacheV1.LoadSprite(fileId, spriteId, name);
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Color c = Color.white;
            c.a = Mathf.Clamp01(alpha / 255.0f);
            img.color = c;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
            return img;
        }

        private Image AddSolid(string name, Color color, int x, int y, int w, int h)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
            return img;
        }


        private static Font RuntimeFont()
        {
            if (_cachedRuntimeFont != null)
                return _cachedRuntimeFont;

            // Unity 6/newer no longer accepts Arial.ttf here. LegacyRuntime.ttf is the valid builtin fallback.
            _cachedRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedRuntimeFont == null)
                _cachedRuntimeFont = Font.CreateDynamicFontFromOSFont("Arial", 12);
            if (_cachedRuntimeFont == null)
                _cachedRuntimeFont = Font.CreateDynamicFontFromOSFont("Liberation Sans", 12);
            return _cachedRuntimeFont;
        }

        private Text AddLabel(string name, string text, int x, int y, int w, int h, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject go = NewUi(name);
            Text t = go.AddComponent<Text>();
            t.text = text ?? string.Empty;
            t.font = RuntimeFont();
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
            return t;
        }

        private GameObject NewUi(string name)
        {
            GameObject go = new GameObject("C2_HUD_" + name);
            go.transform.SetParent(_root, false);
            go.AddComponent<RectTransform>();
            _spawned.Add(go);
            return go;
        }

        private static void Place(RectTransform rt, int x, int y, int w, int h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1, w), Mathf.Max(1, h));
        }

        private void SetVisible(bool visible)
        {
            EnsureCanvas();
            _visible = visible;
            if (_canvas != null) _canvas.gameObject.SetActive(visible);
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal FirstSelectedUnit()
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u != null && u.isActiveAndEnabled && u.IsSelected) return u;
            }
            return null;
        }

        private static int CountSelectedUnits()
        {
            int count = 0;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u != null && u.isActiveAndEnabled && u.IsSelected) count++;
            }
            return count;
        }

        private static bool HasBattleCamera()
        {
            Camera[] cams = Camera.allCameras;
            for (int i = 0; cams != null && i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (c == null || !c.isActiveAndEnabled) continue;
                string n = c.name ?? string.Empty;
                if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return Camera.main != null;
        }

        private static string San(string s)
        {
            return Regex.Replace(s ?? string.Empty, "[^A-Za-z0-9_]+", "_");
        }

        private sealed class DialogNode
        {
            public string Name;
            public string Text = string.Empty;
            public readonly List<DialogNode> Children = new List<DialogNode>();

            public string TextOf(string tag)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    DialogNode c = Children[i];
                    if (string.Equals(c.Name, tag, StringComparison.OrdinalIgnoreCase))
                        return (c.Text ?? string.Empty).Trim();
                }
                return string.Empty;
            }

            public int Int(string tag, int fallback)
            {
                int v;
                return int.TryParse(TextOf(tag), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback;
            }

            public static DialogNode Parse(string src)
            {
                DialogNode root = new DialogNode { Name = "Root" };
                Stack<DialogNode> stack = new Stack<DialogNode>();
                stack.Push(root);

                Regex rx = new Regex("<(/?)([^>]*)>", RegexOptions.Compiled);
                int last = 0;
                MatchCollection ms = rx.Matches(src ?? string.Empty);
                for (int i = 0; i < ms.Count; i++)
                {
                    Match m = ms[i];
                    if (m.Index > last && stack.Count > 0)
                        stack.Peek().Text += (src ?? string.Empty).Substring(last, m.Index - last);
                    last = m.Index + m.Length;

                    bool close = m.Groups[1].Value == "/";
                    string name = (m.Groups[2].Value ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(name)) name = "RootBlock";
                    int sp = name.IndexOf(' ');
                    if (sp >= 0) name = name.Substring(0, sp).Trim();

                    if (close)
                    {
                        if (stack.Count > 1) stack.Pop();
                    }
                    else
                    {
                        DialogNode n = new DialogNode { Name = name };
                        stack.Peek().Children.Add(n);
                        stack.Push(n);
                    }
                }

                return root;
            }
        }
    }
}
