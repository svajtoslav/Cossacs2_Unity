using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1 : MonoBehaviour
    {
        private const string Contract = "V239_UNIT_HUD_PORT_NO_BUILD_LOGIC";
        private const int OriginalUnitProduceBaseX = 182;
        private const int OriginalUnitProduceBaseY = 613; // V16: lifted 20 px; V15 row/card bottom was partly below 768 reference height
        private const int OriginalUnitProduceStepX = 67;
        private const int OriginalUnitProduceStepY = 126;
        private const int OriginalUnitProduceWidth = 64;
        private const int OriginalUnitProduceHeight = 123;
        private const int OriginalUnitProduceIconX = 0;
        private const int OriginalUnitProduceIconY = 0;
        private const int OriginalUnitProduceIconW = 56;
        private const int OriginalUnitProduceIconH = 118;
        private const int OriginalSelPointSideWidthV137LikeOriginal = 35;
        private const int OriginalSelPointSideYV137LikeOriginal = 479;
        private const int OriginalSelPointSideUnitAlphaV149LikeOriginal = 155;
        private const int OriginalWeaponPanelBaseXV154LikeOriginal = 182;
        private const int OriginalWeaponPanelBaseYV154LikeOriginal = 543;

        // Original VUI_Info.cpp uses Upgrade.VitButton.xml with x=uX+I->x*67, y=uY+I->y*67.
        // This first pass places upgrades as small square buttons to the right of the left portrait card,
        // matching the original screenshots' compact upgrade grid.
        private const int OriginalBuildingUpgradeBaseX = 248;
        private const int OriginalBuildingUpgradeBaseY = 500;
        private const int OriginalBuildingUpgradeStep = 67;
        private const int OriginalBuildingUpgradeBox = 58;
        private const int OriginalBuildingUpgradeIcon = 48;
        private const int GameplayHudLayer = 31; // isolated runtime layer: prevents main-menu/debug UI from being rendered by the HUD overlay camera

        private static C2GameplayHudV1 _active;

        private Canvas _canvas;
        private RectTransform _root;
        private Camera _boundBattleCamera;
        private Camera _hudOverlayCamera;
        private string _lastCanvasBindingLog = string.Empty;
        private RectTransform _tooltipRoot;
        private Text _tooltipText;
        private readonly List<GameObject> _spawned = new List<GameObject>(128);
        private bool _visible;
        private int _lastSelectedCount = -1;
        private float _nextRefresh;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _lastUnit;
        private C2SettlementBuildingSelectableV1LikeOriginal _lastBuilding;
        private C2RuntimeConstructionSiteProxyLikeOriginal _lastBuildingProxy;
        private int _lastBuildingSelectedCount = -1;
        private string _lastBuildingStateKey = string.Empty;
        private string _activeUnitSelPointKeyV137LikeOriginal = string.Empty;
        private string _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
        private string _lastProduceAudit = string.Empty;
        private string _activeWeaponUiKeyV154LikeOriginal = string.Empty;
        private int _activeWeaponUiTypeV154LikeOriginal = -1;
        private readonly HashSet<string> _activeWeaponUiStatesV157LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static GameObject _weaponRangeRootV154LikeOriginal;
        private static GameObject _enemyBrigadeHoverArrowRootV395LikeOriginal;
        private static int _enemyBrigadeHoverArrowSourceGroupV395LikeOriginal = -1;
        private static int _enemyBrigadeHoverArrowTargetGroupV395LikeOriginal = -1;
        private static float _enemyBrigadeHoverArrowNextRefreshV395LikeOriginal;
        private static LineRenderer _weaponRangeLineV154LikeOriginal;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _hoverWeaponRangeUnitV159LikeOriginal;
        private string _hoverWeaponRangeKeyV159LikeOriginal = string.Empty;
        private int _hoverWeaponRangeTypeV159LikeOriginal = -1;
        private int _hoverWeaponRangeRadiusV159LikeOriginal;
        private GameObject _weaponRangeScreenRootV160LikeOriginal;
        private C2WeaponRangeScreenGraphicV160LikeOriginal _weaponRangeScreenGraphicV160LikeOriginal;
        private bool _weaponRangeGuiVisibleV161LikeOriginal;
        private Vector2[][] _weaponRangeGuiPolysV161LikeOriginal = new Vector2[0][];
        private Vector2[] _weaponRangeGuiCentersV161LikeOriginal = new Vector2[0];
        private Color[] _weaponRangeGuiColorsV161LikeOriginal = new Color[0];
        private static Material _weaponRangeGuiMaterialV161LikeOriginal;
        private string _lastWeaponRangeAuditKeyV159LikeOriginal = string.Empty;
        private float _nextWeaponRangeAuditV159LikeOriginal;
        private float _weaponRangeHideAtV390LikeOriginal = -1.0f;
        private float _nextWeaponReloadUiRefreshV390LikeOriginal;
        private float _nextWeaponRangeDynamicRefreshV392LikeOriginal;
        private readonly List<C2WeaponReloadUiBindingV390LikeOriginal> _weaponReloadUiV390LikeOriginal =
            new List<C2WeaponReloadUiBindingV390LikeOriginal>(4);
        private readonly List<C2TiredUiBindingV398LikeOriginal> _tiredUiV398LikeOriginal =
            new List<C2TiredUiBindingV398LikeOriginal>(4);
        private readonly List<C2WeaponHoverBindingV391LikeOriginal> _weaponHoverBindingsV391LikeOriginal =
            new List<C2WeaponHoverBindingV391LikeOriginal>(4);
        private bool _lastBuildPlacementActive;
        private StringBuilder _spriteAudit = new StringBuilder(8192);
        private int _spriteAuditOrder;
        internal static C2SettlementBuildingSelectableV1LikeOriginal C2GameplayHudV133SelectedBuildingLikeOriginal;
        private static readonly Dictionary<string, Sprite> s_topSliceSpriteCacheV133LikeOriginal = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static Font _cachedRuntimeFont;


        private sealed class C2WeaponReloadUiBindingV390LikeOriginal
        {
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public string Key = string.Empty;
            public int WeaponType;
            public Text ReadyText;
            public Image ReloadLine;
            public int X;
            public int Y;
            public int MaxHeight;
        }

        private sealed class C2TiredUiBindingV398LikeOriginal
        {
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public Image Fill;
            public int X;
            public int Y;
            public int Width;
            public int MaxHeight;
        }

        private sealed class C2WeaponHoverBindingV391LikeOriginal
        {
            public RectTransform Rect;
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public int WeaponType;
            public int Radius;
        }

        private void OnGUI()
        {
            // V162: the attack-range preview is no longer drawn through OnGUI.
            // Original C2 DrawWRect/DrawWorldLine is part of the world/unit draw pass,
            // so units must stay visually above the range fill.
        }

        private static Material EnsureWeaponRangeGuiMaterialV161LikeOriginal()
        {
            if (_weaponRangeGuiMaterialV161LikeOriginal != null)
                return _weaponRangeGuiMaterialV161LikeOriginal;

            Shader sh = Shader.Find("Hidden/Internal-Colored");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) return null;

            _weaponRangeGuiMaterialV161LikeOriginal = new Material(sh);
            _weaponRangeGuiMaterialV161LikeOriginal.hideFlags = HideFlags.HideAndDontSave;
            _weaponRangeGuiMaterialV161LikeOriginal.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _weaponRangeGuiMaterialV161LikeOriginal.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _weaponRangeGuiMaterialV161LikeOriginal.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _weaponRangeGuiMaterialV161LikeOriginal.SetInt("_ZWrite", 0);
            _weaponRangeGuiMaterialV161LikeOriginal.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            return _weaponRangeGuiMaterialV161LikeOriginal;
        }

        private static void DrawWeaponRangeGuiPolygonV161LikeOriginal(Vector2 center, Vector2[] points, Color color)
        {
            if (points == null || points.Length < 3)
                return;

            Color fill = new Color(color.r, color.g, color.b, Mathf.Clamp01(Mathf.Max(color.a, 0.22f)));
            Color line = new Color(Mathf.Clamp01(color.r + 0.18f), Mathf.Clamp01(color.g + 0.18f), Mathf.Clamp01(color.b + 0.18f), 0.96f);

            GL.Begin(GL.TRIANGLES);
            GL.Color(fill);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Length];
                GL.Vertex3(center.x, center.y, 0);
                GL.Vertex3(a.x, a.y, 0);
                GL.Vertex3(b.x, b.y, 0);
            }
            GL.End();

            DrawWeaponRangeGuiLineLoopV161LikeOriginal(points, line, 0.0f, 0.0f);
            DrawWeaponRangeGuiLineLoopV161LikeOriginal(points, line, 1.0f, 0.0f);
            DrawWeaponRangeGuiLineLoopV161LikeOriginal(points, line, -1.0f, 0.0f);
            DrawWeaponRangeGuiLineLoopV161LikeOriginal(points, line, 0.0f, 1.0f);
            DrawWeaponRangeGuiLineLoopV161LikeOriginal(points, line, 0.0f, -1.0f);
        }

        private static void DrawWeaponRangeGuiLineLoopV161LikeOriginal(Vector2[] points, Color color, float ox, float oy)
        {
            if (points == null || points.Length < 2)
                return;

            GL.Begin(GL.LINES);
            GL.Color(color);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Length];
                GL.Vertex3(a.x + ox, a.y + oy, 0);
                GL.Vertex3(b.x + ox, b.y + oy, 0);
            }
            GL.End();
        }

        private sealed class C2SelectedUnitSelPointV137LikeOriginal
        {
            public string Key = string.Empty;
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public int Count;
            public int NIndex;
            public int RealX;
            public int RealY;
            public bool Peasant;
            public string Title = string.Empty;
            public C2OriginalProduceCatalogV13.C2MdIconInfoV13 Icon;
        }

        public static bool C2GameplayHudV13PlacementRequestedLikeOriginal;
        public static string C2GameplayHudV13SelectedBuildUnitIdLikeOriginal = string.Empty;
        public static string C2GameplayHudV13SelectedBuildMdLikeOriginal = string.Empty;
        public static int C2GameplayHudV13SelectedBuildNationLikeOriginal;

        public static void C2GameplayHudV28InvalidateBuildModeLikeOriginal()
        {
            if (_active == null) return;
            _active._nextRefresh = 0.0f;
            _active._lastSelectedCount = -999999;
            _active._lastBuildingStateKey = string.Empty;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (_active != null) return;
            GameObject go = new GameObject("GameplayHud_SelectedPanel_V13_Manager");
            DontDestroyOnLoad(go);
            _active = go.AddComponent<C2GameplayHudV1>();
        }

        private void Awake()
        {
            _active = this;
            EnsureCanvas();
            Debug.Log("[C2:GAMEPLAY HUD V172] installed contract=" + Contract +
                      " original=SelPoint.DialogsDesk + va_SP_Bld_BigPortret + OISelection::SetProduce/SetUpgrade" +
                      " fix=building_ready_filter_bigicon_only");
        }

        private static readonly Unity.Profiling.ProfilerMarker HudUpdateMarker =
            new Unity.Profiling.ProfilerMarker("C2.Hud.Update");

        private void Update()
        {
            using (HudUpdateMarker.Auto()) UpdateHudLikeOriginal();
        }

        private void UpdateHudLikeOriginal()
        {
            if (Time.realtimeSinceStartup < _nextRefresh) return;
            _nextRefresh = Time.realtimeSinceStartup + 0.20f;

            List<C2SelectedUnitSelPointV137LikeOriginal> unitSelPoints = BuildSelectedUnitSelPointsV137LikeOriginal();
            int activeUnitSelPointIndex = ResolveActiveUnitSelPointIndexV137LikeOriginal(unitSelPoints);
            C2SelectedUnitSelPointV137LikeOriginal activeUnitSelPoint =
                activeUnitSelPointIndex >= 0 && activeUnitSelPointIndex < unitSelPoints.Count ? unitSelPoints[activeUnitSelPointIndex] : null;
            C2NeutralPeasantUnitInfoV2LikeOriginal unit = activeUnitSelPoint != null ? activeUnitSelPoint.Unit : null;
            int count = activeUnitSelPoint != null ? activeUnitSelPoint.Count : 0;
            string unitSelPointStateKey = BuildUnitSelPointStateKeyV137LikeOriginal(unitSelPoints, activeUnitSelPointIndex);
            C2SettlementBuildingSelectableV1LikeOriginal building = unit == null ? FirstSelectedBuildingLikeOriginal() : null;
            int buildingCount = unit == null ? CountSelectedBuildingsLikeOriginal() : 0;
            Camera battleCamera = FindBattleCamera();
            bool buildPlacementActive = C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal;

            // Original BuildMode is an overlay on the current selected builder.
            // If the same mouse click that pressed a build icon is also seen by the map picker,
            // keep the last selected builder HUD instead of hiding the portrait/panel.
            if (unit == null && building == null && buildPlacementActive && _lastUnit != null && _lastUnit.isActiveAndEnabled)
            {
                unit = _lastUnit;
                count = Mathf.Max(1, _lastSelectedCount);
                unitSelPoints = null;
                activeUnitSelPointIndex = 0;
                unitSelPointStateKey = "buildmode_last_unit:" + (_lastUnit.SourceMonsterId ?? string.Empty);
            }

            if (battleCamera != null)
            {
                // V13N: entering/being in battle view must kill old main-menu/debug UI immediately,
                // even before a peasant is selected. V13J suppressed it only after shouldShow=true.
                KillForeignBattleUiRoots();
                EnsureCanvas();
                ConfigureScreenOverlay(battleCamera);
                SuppressForeignCanvasesWhileBattleHudActive();
            }

            RefreshResourcePanelLikeOriginal(battleCamera);

            bool hasGlobalBrigDialogV172 = battleCamera != null && HasGlobalBrigDialogProposalV172LikeOriginal();
            string globalBrigDialogStateKeyV172 = hasGlobalBrigDialogV172 ? GlobalBrigDialogStateKeyV172LikeOriginal() : string.Empty;
            bool shouldShow = (unit != null || building != null || hasGlobalBrigDialogV172) && battleCamera != null;

            if (!shouldShow)
            {
                if (_visible) SetVisible(false);
                _lastSelectedCount = -1;
                _lastBuildingSelectedCount = -1;
                _lastBuildingStateKey = string.Empty;
                _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
                _lastGlobalBrigDialogStateKeyV172LikeOriginal = string.Empty;
                _lastUnit = null;
                _lastBuilding = null;
                _lastBuildingProxy = null;
                C2GameplayHudV133SelectedBuildingLikeOriginal = null;
                if (battleCamera == null)
                    ConfigureScreenOverlay(null);
                return;
            }

            // C2 GlobalBrigDialog is a battle overlay, not a selected-building panel.
            // If nothing is selected but a valid own barracks/center exists, keep the HUD canvas alive
            // and draw only the top-left global brigade frame.
            if (unit == null && building == null && hasGlobalBrigDialogV172)
            {
                if (_visible && _lastUnit == null && _lastBuilding == null &&
                    _lastGlobalBrigDialogStateKeyV172LikeOriginal == globalBrigDialogStateKeyV172 &&
                    _lastBuildPlacementActive == buildPlacementActive)
                    return;
                EnsureCanvas();
                ReloadOriginalDataForModLikeOriginal();
                ClearSpawned();
                BuildGlobalBrigDialogV172LikeOriginal(null, null);
                _lastSelectedCount = -1;
                _lastBuildingSelectedCount = -1;
                _lastBuildingStateKey = string.Empty;
                _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
                _lastGlobalBrigDialogStateKeyV172LikeOriginal = globalBrigDialogStateKeyV172;
                _lastBuildPlacementActive = buildPlacementActive;
                _lastUnit = null;
                _lastBuilding = null;
                _lastBuildingProxy = null;
                C2GameplayHudV133SelectedBuildingLikeOriginal = null;
                ConfigureScreenOverlay(battleCamera);
                SetVisible(true);
                return;
            }

            if (unit != null)
            {
                if (!_visible || _lastSelectedCount != count || _lastUnit != unit || _lastBuilding != null ||
                    _lastUnitSelPointStateKeyV137LikeOriginal != unitSelPointStateKey ||
                    _lastGlobalBrigDialogStateKeyV172LikeOriginal != globalBrigDialogStateKeyV172 ||
                    _boundBattleCamera != battleCamera || _lastBuildPlacementActive != buildPlacementActive)
                {
                    C2GameplayHudV133SelectedBuildingLikeOriginal = null;
                    Rebuild(unit, count, unitSelPoints, activeUnitSelPointIndex);
                    BuildGlobalBrigDialogV172LikeOriginal(unit, null);
                    _lastSelectedCount = count;
                    _lastBuildingSelectedCount = -1;
                    _lastBuildingStateKey = string.Empty;
                    _lastUnitSelPointStateKeyV137LikeOriginal = unitSelPointStateKey;
                    _lastGlobalBrigDialogStateKeyV172LikeOriginal = globalBrigDialogStateKeyV172;
                    _lastUnit = unit;
                    _lastBuilding = null;
                    _lastBuildingProxy = null;
                    _lastBuildPlacementActive = buildPlacementActive;
                    ConfigureScreenOverlay(battleCamera);
                    SetVisible(true);
                }
            }
            else
            {
                string buildingStateKey = BuildBuildingHudStateKeyV114LikeOriginal(building);
                if (!_visible || _lastBuildingSelectedCount != buildingCount || _lastBuilding != building || _lastBuildingStateKey != buildingStateKey || _lastGlobalBrigDialogStateKeyV172LikeOriginal != globalBrigDialogStateKeyV172 || _lastUnit != null || _boundBattleCamera != battleCamera || _lastBuildPlacementActive != buildPlacementActive)
                {
                    C2GameplayHudV133SelectedBuildingLikeOriginal = building;
                    RebuildBuildingLikeOriginal(building, buildingCount);
                    BuildGlobalBrigDialogV172LikeOriginal(null, building);
                    _lastSelectedCount = -1;
                    _lastBuildingSelectedCount = buildingCount;
                    _lastBuildingStateKey = buildingStateKey;
                    _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
                    _lastGlobalBrigDialogStateKeyV172LikeOriginal = globalBrigDialogStateKeyV172;
                    _lastUnit = null;
                    _lastBuilding = building;
                    _lastBuildingProxy = building != null ? building.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>() : null;
                    _lastBuildPlacementActive = buildPlacementActive;
                    ConfigureScreenOverlay(battleCamera);
                    SetVisible(true);
                }
            }
        }

        private void EnsureCanvas()
        {
            // V13N:
            // Use a dedicated UI camera, but register it as a URP Overlay camera in the battle camera stack.
            // V13E/F created a normal extra camera and it cleared the GameView black/yellow.
            // V13G/H used the battle camera and could disappear/clip. Camera-stack overlay gives real screen UI
            // without terrain/building occlusion and without clearing the map.
            if (_canvas != null && _root != null)
            {
                ConfigureScreenOverlay(FindBattleCamera());
                return;
            }

            string[] oldNames =
            {
                "C2_GameplayHud_Canvas_V1",
                "GameplayHud_Canvas_V13_Overlay",
                "GameplayHud_Canvas_V13C_BattleCamera",
                "GameplayHud_Canvas_V13D_ScreenOverlay",
                "GameplayHud_Canvas_V13E_UiCameraOverlay",
                "GameplayHud_OverlayCamera_V13E",
                "GameplayHud_Canvas_V13F_UiCameraNoClearOverlay",
                "GameplayHud_OverlayCamera_V13F",
                "GameplayHud_Canvas_V13G_BattleCameraZAlways",
                "GameplayHud_Canvas_V13H_BattleCameraHiddenFallback",
                "GameplayHud_Canvas_V13I_URPStackedOverlay",
                "GameplayHud_OverlayCamera_V13I",
                "GameplayHud_Canvas_V13J_URPStackedIsolatedOverlay",
                "GameplayHud_OverlayCamera_V13J",
                "GameplayHud_Canvas_V13K_URPStackedIsolatedOverlay",
                "GameplayHud_OverlayCamera_V13K",
                "GameplayHud_Canvas_V13M_BigIconFrameAllBattleCameras",
                "GameplayHud_OverlayCamera_V13M",
                "GameplayHud_Canvas_V13O_BigIconFullSelPointFrame",
                "GameplayHud_OverlayCamera_V13O",
                "GameplayHud_Canvas_V13P_BigIconFullSelPointFrame",
                "GameplayHud_OverlayCamera_V13P",
                "GameplayHud_Canvas_V13Q_ModRootTitleProduceCache",
                "GameplayHud_OverlayCamera_V13Q",
                "GameplayHud_Canvas_V13R_StableSpritesTitleSource",
                "GameplayHud_OverlayCamera_V13R",
                "GameplayHud_Canvas_V13S_NameBarGeometryFix",
                "GameplayHud_OverlayCamera_V13S",
                "GameplayHud_Canvas_V14_AiDatFlagsMdPorts",
                "GameplayHud_OverlayCamera_V14",
                "C2_MainMenuCanvas",
                "C2_OptionsCanvas",
                "C2_AddProfileCanvas",
                "C2_MBattlesCanvas"
            };
            for (int i = 0; i < oldNames.Length; i++)
            {
                GameObject old = GameObject.Find(oldNames[i]);
                if (old != null)
                    Destroy(old);
            }

            GameObject cgo = GameObject.Find("GameplayHud_Canvas_V14_AiDatFlagsMdPorts");
            if (cgo == null)
            {
                cgo = new GameObject("GameplayHud_Canvas_V14_AiDatFlagsMdPorts");
                DontDestroyOnLoad(cgo);
            }

            cgo.transform.SetParent(null, false);
            SetLayerRecursive(cgo, GameplayHudLayer);
            _canvas = cgo.GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = cgo.AddComponent<Canvas>();

            CanvasScaler scaler = cgo.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = cgo.AddComponent<CanvasScaler>();

            GraphicRaycaster raycaster = cgo.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = cgo.AddComponent<GraphicRaycaster>();

            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            Transform oldRoot = cgo.transform.Find("GameplayHud_Root_V13N");
            if (oldRoot != null)
                Destroy(oldRoot.gameObject);

            GameObject rootGo = new GameObject("GameplayHud_Root_V13N");
            rootGo.transform.SetParent(cgo.transform, false);
            _root = rootGo.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0, 1);
            _root.anchorMax = new Vector2(0, 1);
            _root.pivot = new Vector2(0, 1);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(1024, 768);

            ConfigureScreenOverlay(FindBattleCamera(), true);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            if (go == null) return;

            go.layer = layer;
            Transform tr = go.transform;
            for (int i = 0; i < tr.childCount; i++)
            {
                Transform child = tr.GetChild(i);
                if (child != null)
                    SetLayerRecursive(child.gameObject, layer);
            }
        }

        private void ConfigureScreenOverlay(Camera cam, bool forceLog = false)
        {
            if (_canvas == null) return;

            if (cam == null)
            {
                _boundBattleCamera = null;
                _canvas.enabled = false;
                GraphicRaycaster disabledRaycaster = _canvas.GetComponent<GraphicRaycaster>();
                if (disabledRaycaster != null) disabledRaycaster.enabled = false;
                if (_hudOverlayCamera != null) _hudOverlayCamera.enabled = false;
                return;
            }

            _boundBattleCamera = cam;
            Camera uiCam = EnsureHudOverlayCamera(cam);

            _canvas.enabled = true; // ResPan remains visible without a selection.
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = uiCam != null ? uiCam : cam;
            _canvas.planeDistance = 1.0f;
            _canvas.targetDisplay = cam.targetDisplay;
            _canvas.pixelPerfect = false;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 32767;

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1024, 768);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1.0f;
            }

            GraphicRaycaster raycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = true;

            Transform tr = _canvas.transform;
            if (tr != null)
            {
                tr.SetParent(null, false);
                tr.SetAsLastSibling();
            }

            string log = "canvas='" + _canvas.gameObject.name +
                         "' renderMode=" + _canvas.renderMode +
                         " primaryBattleCamera='" + cam.name +
                         "' activeBattleCameras=" + CountBattleCamerasForLog().ToString(CultureInfo.InvariantCulture) +
                         " uiCamera='" + (uiCam != null ? uiCam.name : "<fallback-battle-camera>") +
                         "' targetDisplay=" + cam.targetDisplay.ToString(CultureInfo.InvariantCulture) +
                         " sortingOrder=" + _canvas.sortingOrder.ToString(CultureInfo.InvariantCulture) +
                         " uiCameraDepth=" + (uiCam != null ? uiCam.depth.ToString(CultureInfo.InvariantCulture) : "<none>") +
                         " hudLayer=" + GameplayHudLayer.ToString(CultureInfo.InvariantCulture) +
                         " pixelRect=" + cam.pixelRect.ToString();
            if (forceLog || !string.Equals(log, _lastCanvasBindingLog, StringComparison.Ordinal))
            {
                _lastCanvasBindingLog = log;
                C2RuntimeDiagnosticsV1.MarkPerfEvent(
                    "HUD_CAMERA_BIND",
                    "canvas='" + _canvas.gameObject.name +
                    "' pixelRect=" + cam.pixelRect.ToString() +
                    " uiCamera='" + (uiCam != null ? uiCam.name : "<fallback>") +
                    "' visible=" + _visible +
                    " activeBattleCameras=" + CountBattleCamerasForLog().ToString(CultureInfo.InvariantCulture));
                Debug.Log("[C2:GAMEPLAY HUD V15 URP STACKED ALL-CAMERA OVERLAY] " + log);
            }
        }


        private static int _lastKilledForeignBattleUiFrame = -1;

        public static void KillForeignBattleUiRoots()
        {
            // V13N hard cleanup for F12/map load:
            // BaseUiRenderer creates C2_MainMenuCanvas as ScreenSpaceOverlay, so URP layer isolation alone cannot hide it.
            // Kill all non-HUD C2/UI canvases once per frame while a battle camera exists.
            if (_lastKilledForeignBattleUiFrame == Time.frameCount)
                return;
            _lastKilledForeignBattleUiFrame = Time.frameCount;

            int disabled = 0;
            int destroyed = 0;

            try
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas c = canvases[i];
                    if (c == null) continue;

                    GameObject go = c.gameObject;
                    if (go == null) continue;

                    string n = go.name ?? string.Empty;

                    bool isOurHud =
                        n.StartsWith("GameplayHud_", StringComparison.Ordinal) ||
                        n.StartsWith("C2_HUD_", StringComparison.Ordinal) ||
                        n.IndexOf("GameplayHud", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isOurHud)
                        continue;

                    bool isForeignBattleUi =
                        n.StartsWith("C2_", StringComparison.Ordinal) ||
                        n.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Options", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("AddProfile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Mbattles", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Damba", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("WALS", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!isForeignBattleUi)
                        continue;

                    if (c.enabled)
                    {
                        c.enabled = false;
                        disabled++;
                    }

                    GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
                    if (gr != null && gr.enabled)
                        gr.enabled = false;

                    // Destroy only runtime scene objects, not assets/prefabs.
                    if (go.scene.IsValid())
                    {
                        Destroy(go);
                        destroyed++;
                    }
                }

                GameObject[] named =
                {
                    GameObject.Find("C2_MainMenuCanvas"),
                    GameObject.Find("C2_OptionsCanvas"),
                    GameObject.Find("C2_AddProfileCanvas"),
                    GameObject.Find("C2_MBattlesCanvas"),
                    GameObject.Find("C2_DAMBA_WALS2D_V178"),
                    GameObject.Find("C2_DAMBA_WALS2D"),
                    GameObject.Find("DONT DESTROY ON LOAD")
                };

                for (int i = 0; i < named.Length; i++)
                {
                    GameObject go = named[i];
                    if (go == null) continue;

                    string n = go.name ?? string.Empty;
                    if (n == "DONT DESTROY ON LOAD")
                        continue;

                    if (n.StartsWith("GameplayHud_", StringComparison.Ordinal))
                        continue;

                    Canvas c = go.GetComponent<Canvas>();
                    if (c != null && c.enabled)
                    {
                        c.enabled = false;
                        disabled++;
                    }

                    GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
                    if (gr != null && gr.enabled)
                        gr.enabled = false;

                    if (go.scene.IsValid())
                    {
                        Destroy(go);
                        destroyed++;
                    }
                }

                if (disabled > 0 || destroyed > 0)
                {
                    Debug.Log("[C2:GAMEPLAY HUD V15 KILL FOREIGN BATTLE UI] disabledCanvases=" +
                              disabled.ToString(CultureInfo.InvariantCulture) +
                              " destroyedRoots=" + destroyed.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:GAMEPLAY HUD V14 KILL FOREIGN BATTLE UI WARN] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void SuppressForeignCanvasesWhileBattleHudActive()
        {
            // V13N safety: the previous URP overlay camera rendered every object on the shared UI layer,
            // so the main menu and old debug panels could leak into the battle view.
            // We isolate our HUD on layer 31 and also disable non-HUD canvases while a battle camera is active.
            try
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas c = canvases[i];
                    if (c == null || c == _canvas) continue;

                    GameObject go = c.gameObject;
                    if (go == null) continue;

                    string n = go.name ?? string.Empty;
                    if (n.StartsWith("GameplayHud_", StringComparison.Ordinal) ||
                        n.StartsWith("C2_HUD_", StringComparison.Ordinal))
                        continue;

                    // Do not destroy anything; only hide visual/raycast canvas while the battle HUD owns the game view.
                    if (c.enabled)
                        c.enabled = false;

                    GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
                    if (gr != null && gr.enabled)
                        gr.enabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:GAMEPLAY HUD V14 FOREIGN CANVAS SUPPRESS WARN] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private Camera EnsureHudOverlayCamera(Camera battleCamera)
        {
            if (battleCamera == null) return null;

            if (_hudOverlayCamera == null)
            {
                GameObject old = GameObject.Find("GameplayHud_OverlayCamera_V14");
                if (old != null)
                    _hudOverlayCamera = old.GetComponent<Camera>();

                if (_hudOverlayCamera == null)
                {
                    GameObject go = new GameObject("GameplayHud_OverlayCamera_V14");
                    DontDestroyOnLoad(go);
                    _hudOverlayCamera = go.AddComponent<Camera>();
                }
            }

            _hudOverlayCamera.enabled = true;
            _hudOverlayCamera.transform.SetParent(null, false);
            _hudOverlayCamera.transform.position = Vector3.zero;
            _hudOverlayCamera.transform.rotation = Quaternion.identity;
            _hudOverlayCamera.transform.localScale = Vector3.one;

            _hudOverlayCamera.clearFlags = CameraClearFlags.Depth;
            _hudOverlayCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _hudOverlayCamera.cullingMask = 1 << GameplayHudLayer;
            _hudOverlayCamera.depth = battleCamera.depth + 1000.0f;
            _hudOverlayCamera.orthographic = true;
            _hudOverlayCamera.orthographicSize = 5.0f;
            _hudOverlayCamera.fieldOfView = 60.0f;
            _hudOverlayCamera.nearClipPlane = 0.01f;
            _hudOverlayCamera.farClipPlane = 100.0f;
            _hudOverlayCamera.rect = new Rect(0f, 0f, 1f, 1f);
            _hudOverlayCamera.targetDisplay = battleCamera.targetDisplay;
            _hudOverlayCamera.allowHDR = false;
            _hudOverlayCamera.allowMSAA = false;
            _hudOverlayCamera.useOcclusionCulling = false;

            // V13N: bind the HUD overlay camera to every active battle camera stack.
            // This makes the same menu visible when switching between the strict iso camera and the free/debug camera.
            BindHudOverlayCameraToAllBattleCameras(_hudOverlayCamera);
            return _hudOverlayCamera;
        }

        private static int CountBattleCamerasForLog()
        {
            List<Camera> cams = FindBattleCameras();
            return cams != null ? cams.Count : 0;
        }

        private static void BindHudOverlayCameraToAllBattleCameras(Camera overlayCamera)
        {
            if (overlayCamera == null) return;

            List<Camera> bases = FindBattleCameras();
            if (bases == null || bases.Count == 0) return;

            for (int i = 0; i < bases.Count; i++)
            {
                Camera baseCam = bases[i];
                if (baseCam == null || object.ReferenceEquals(baseCam, overlayCamera)) continue;
                TryBindUrpOverlayCamera(baseCam, overlayCamera);
            }
        }

        private static void TryBindUrpOverlayCamera(Camera baseCamera, Camera overlayCamera)
        {
            if (baseCamera == null || overlayCamera == null) return;

            try
            {
                Type urpDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpDataType == null) return;

                Component baseData = baseCamera.GetComponent(urpDataType);
                if (baseData == null) baseData = baseCamera.gameObject.AddComponent(urpDataType);

                Component overlayData = overlayCamera.GetComponent(urpDataType);
                if (overlayData == null) overlayData = overlayCamera.gameObject.AddComponent(urpDataType);

                // overlayData.renderType = CameraRenderType.Overlay
                System.Reflection.PropertyInfo renderTypeProp = urpDataType.GetProperty("renderType");
                if (renderTypeProp != null && renderTypeProp.CanWrite)
                {
                    Type enumType = renderTypeProp.PropertyType;
                    object overlayValue = Enum.Parse(enumType, "Overlay");
                    object baseValue = Enum.Parse(enumType, "Base");
                    renderTypeProp.SetValue(overlayData, overlayValue, null);
                    renderTypeProp.SetValue(baseData, baseValue, null);
                }

                // baseData.cameraStack.Add(overlayCamera)
                System.Reflection.PropertyInfo stackProp = urpDataType.GetProperty("cameraStack");
                if (stackProp != null)
                {
                    object stackObj = stackProp.GetValue(baseData, null);
                    System.Collections.IList stack = stackObj as System.Collections.IList;
                    if (stack != null)
                    {
                        bool exists = false;
                        for (int i = 0; i < stack.Count; i++)
                        {
                            if (object.ReferenceEquals(stack[i], overlayCamera))
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists) stack.Add(overlayCamera);
                    }
                }

                // Some URP versions expose clearDepth on overlay cameras.
                System.Reflection.PropertyInfo clearDepthProp = urpDataType.GetProperty("clearDepth");
                if (clearDepthProp != null && clearDepthProp.CanWrite)
                    clearDepthProp.SetValue(overlayData, true, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:GAMEPLAY HUD V14 URP STACK WARN] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void LateUpdate()
        {
            PollWeaponCardHoverV391LikeOriginal();

            if (_weaponRangeHideAtV390LikeOriginal >= 0.0f &&
                Time.realtimeSinceStartup >= _weaponRangeHideAtV390LikeOriginal)
            {
                _weaponRangeHideAtV390LikeOriginal = -1.0f;
                HideWeaponRangeV154LikeOriginal();
            }

            if (Time.realtimeSinceStartup >= _nextWeaponReloadUiRefreshV390LikeOriginal)
            {
                _nextWeaponReloadUiRefreshV390LikeOriginal = Time.realtimeSinceStartup + 0.08f;
                RefreshWeaponReloadUiV390LikeOriginal();
            }

            // VUI_Actions.cpp::va_SP_TiredLine::SetFrameState is evaluated every
            // UI frame. The underlying GetTired changes on the 40 ms simulation
            // tick, but the Canvas bar itself is not an 80 ms weapon-HUD poll.
            RefreshTiredUiV398LikeOriginal();

            if (_canvas != null)
            {
                ConfigureScreenOverlay(FindBattleCamera());
                if (_hoverWeaponRangeUnitV159LikeOriginal != null &&
                    _weaponRangeRootV154LikeOriginal != null &&
                    _weaponRangeRootV154LikeOriginal.activeSelf &&
                    Time.realtimeSinceStartup >= _nextWeaponRangeDynamicRefreshV392LikeOriginal)
                {
                    // V392: V390 rebuilt/destroyed the translucent range meshes every
                    // rendered frame. That was the visible rifle-range flicker.
                    _nextWeaponRangeDynamicRefreshV392LikeOriginal =
                        Time.realtimeSinceStartup + 0.12f;
                    RebuildWeaponRangeV159LikeOriginal(false);
                }
                if (_hoverBrigCreateUnitV165LikeOriginal != null &&
                    _weaponRangeRootV154LikeOriginal != null &&
                    _weaponRangeRootV154LikeOriginal.activeSelf)
                {
                    RebuildBrigCreateRangeV165LikeOriginal(false);
                }
                if (_hoverBrigCreateBuildingV166LikeOriginal != null &&
                    _weaponRangeRootV154LikeOriginal != null &&
                    _weaponRangeRootV154LikeOriginal.activeSelf)
                {
                    RebuildBarracksBrigCreateRangeV166LikeOriginal(false);
                }
                LateUpdateGlobalBrigHoverV172LikeOriginal();
                if (_tooltipRoot != null && _tooltipRoot.gameObject.activeSelf)
                    _tooltipRoot.SetAsLastSibling();
            }
        }

        private void Rebuild(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int selectedCount)
        {
            List<C2SelectedUnitSelPointV137LikeOriginal> groups = BuildSelectedUnitSelPointsV137LikeOriginal();
            int activeIndex = ResolveActiveUnitSelPointIndexV137LikeOriginal(groups);
            Rebuild(unit, selectedCount, groups, activeIndex);
        }

        private void Rebuild(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int selectedCount,
            List<C2SelectedUnitSelPointV137LikeOriginal> unitSelPoints,
            int activeUnitSelPointIndex)
        {
            EnsureCanvas();
            ReloadOriginalDataForModLikeOriginal();
            ClearSpawned();
            _spriteAudit.Length = 0;
            _spriteAuditOrder = 0;

            int selPointCount = unitSelPoints != null ? unitSelPoints.Count : 0;
            bool hasMultipleSelPoints = selPointCount > 1;
            int activeOffsetX = hasMultipleSelPoints ? Mathf.Max(0, activeUnitSelPointIndex) * OriginalSelPointSideWidthV137LikeOriginal : 0;

            // GlBuildSel.GPPicture.Dialogs.xml is NOT part of the selected peasant portrait.
            // It contains a placeholder child Interf3\Units_egp_mini sprite 0 at x=11 y=632.
            // Drawing it permanently is exactly the "Bedouin" leak behind the peasant portrait.
            // Original selected-unit card comes from Dialogs/v/SelPoint.DialogsDesk.Dialogs.xml.
            // The frame is the va_SP_PortretBox GPPicture: Interf3\cropped sprite 19.
            // The portrait inside it is NM->BigIconFile/BigIconIndex, not MINICON.
            // Original XML order in Dialogs/v/SelPoint.DialogsDesk.Dialogs.xml:
            // right-side SelPoint desk, left-side SelPoint desk, then center portrait desk.
            // Therefore all side cards are drawn first and the active BIGICON is drawn last on top.
            if (hasMultipleSelPoints)
            {
                BuildSelectedUnitSelPointSideCardsV137LikeOriginal(unitSelPoints, activeUnitSelPointIndex, true);
                BuildSelectedUnitSelPointSideCardsV137LikeOriginal(unitSelPoints, activeUnitSelPointIndex, false);
            }

            BuildSelectedUnitLeftCardLikeOriginal(unit, selectedCount, activeOffsetX);

            if (hasMultipleSelPoints && activeUnitSelPointIndex >= 0 && activeUnitSelPointIndex < unitSelPoints.Count)
            {
                // Unity raycast fix for the original overlap: side cards visually go under the center portrait.
                // This transparent center area is added after side click areas, so clicks inside the active portrait
                // do not accidentally activate the nearest side SelPoint in the overlapped 45-50 px strip.
                AddUnitSelPointCenterClickAreaV148LikeOriginal(
                    "sp_center_active_click_original",
                    -2 + activeOffsetX,
                    459,
                    183,
                    306,
                    unitSelPoints[activeUnitSelPointIndex].Key);
            }

            // Original cvi_BrigCreate is a second, local path: selecting an officer opens
            // the radius-800 collector even when no COMMANDCENTER is nearby.
            BuildBrigCreateDialogV172LikeOriginal(unit, selectedCount, selPointCount);

            // Original gameplay BuildMode keeps the selected builder portrait, but while a building is already
            // on the cursor the produce list is no longer shown. Do not draw the building buttons/click areas
            // until BuildMode is cancelled or the foundation is placed.
            if (C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal)
            {
                _lastProduceAudit = "hidden_during_buildmode_cursor_object_like_original";
                HideTooltip();
            }
            else if (BuildSelectedUnitWeaponPanelV154LikeOriginal(unit, selectedCount, selPointCount))
            {
                // Original Weapons.DialogsDesk.Dialogs.xml is driven from the active/Last SelPoint.
                // This is UI-only for now: click changes the frame state, but does not start battle orders.
                _lastProduceAudit = "weapon_panel_v154_from_md active='" + (unit != null ? unit.ResolvedMd : string.Empty) + "'";
                HideTooltip();
            }
            else if (hasMultipleSelPoints)
            {
                // Original OISelection::SetProduce returns unless exactly one SelPoint exists.
                _lastProduceAudit = "hidden_multiple_selpoints_like_original count=" + selPointCount.ToString(CultureInfo.InvariantCulture);
                HideTooltip();
            }
            else
            {
                BuildOriginalProducePanelLikeOriginal(unit);
            }
            EnsureTooltipLayer();
            DumpSpriteAuditLikeOriginal(unit, selectedCount);
        }



        private static void ReloadOriginalDataForModLikeOriginal()
        {
            // V133: do not clear and reparse NDS/MD/UI catalogs on every HUD rebuild.
            // The catalog loaders already EnsureLoaded() on demand.
        }

        private static List<C2SelectedUnitSelPointV137LikeOriginal> BuildSelectedUnitSelPointsV137LikeOriginal()
        {
            var result = new List<C2SelectedUnitSelPointV137LikeOriginal>();
            var byKey = new Dictionary<string, C2SelectedUnitSelPointV137LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();

            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled || !u.IsSelected) continue;

                string key = UnitSelPointKeyV137LikeOriginal(u);
                if (string.IsNullOrEmpty(key)) continue;

                C2SelectedUnitSelPointV137LikeOriginal sp;
                if (!byKey.TryGetValue(key, out sp))
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal displayUnit = u;
                    C2NeutralPeasantUnitInfoV2LikeOriginal formationSoldier;
                    if (key.StartsWith("Brigade:", StringComparison.OrdinalIgnoreCase) &&
                        C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierRepresentativeV332LikeOriginal(u, out formationSoldier) &&
                        formationSoldier != null)
                        displayUnit = formationSoldier;

                    C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(displayUnit);
                    sp = new C2SelectedUnitSelPointV137LikeOriginal();
                    sp.Key = key;
                    sp.Unit = displayUnit;
                    sp.Count = 0;
                    sp.NIndex = displayUnit.NIndex;
                    sp.Peasant = icon.Peasant;
                    sp.Icon = icon;
                    sp.Title = ResolveUnitTitleLikeOriginal(sp.Icon, displayUnit);
                    byKey.Add(key, sp);
                    result.Add(sp);
                }

                sp.Count++;
                if (sp.Count == 1)
                {
                    sp.RealX = u.RealX;
                    sp.RealY = u.RealY;
                }
                else
                {
                    sp.RealX = (sp.RealX + u.RealX) / 2;
                    sp.RealY = (sp.RealY + u.RealY) / 2;
                }

                if (!key.StartsWith("Brigade:", StringComparison.OrdinalIgnoreCase) &&
                    (sp.Unit == null || u.SortKey < sp.Unit.SortKey))
                    sp.Unit = u;
            }

            result.Sort(CompareUnitSelPointsV137LikeOriginal);
            return result;
        }

        private static int CompareUnitSelPointsV137LikeOriginal(C2SelectedUnitSelPointV137LikeOriginal a, C2SelectedUnitSelPointV137LikeOriginal b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            // Original vui_SelPoint::Cmp places regular units before peasants, then compares NIndex.
            if (a.Peasant != b.Peasant) return a.Peasant ? 1 : -1;
            int n = a.NIndex.CompareTo(b.NIndex);
            if (n != 0) return n;
            return string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
        }

        private static string UnitSelPointKeyV137LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return string.Empty;
            int formationGroupId;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out formationGroupId))
                return "Brigade:" + formationGroupId.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(unit.SourceMonsterId))
                return unit.SourceMonsterId.Trim();
            if (!string.IsNullOrWhiteSpace(unit.ResolvedMd))
                return unit.ResolvedMd.Trim();
            return "NIndex:" + unit.NIndex.ToString(CultureInfo.InvariantCulture);
        }

        private int ResolveActiveUnitSelPointIndexV137LikeOriginal(List<C2SelectedUnitSelPointV137LikeOriginal> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                _activeUnitSelPointKeyV137LikeOriginal = string.Empty;
                return -1;
            }

            if (!string.IsNullOrEmpty(_activeUnitSelPointKeyV137LikeOriginal))
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (string.Equals(groups[i].Key, _activeUnitSelPointKeyV137LikeOriginal, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            _activeUnitSelPointKeyV137LikeOriginal = groups[0].Key;
            return 0;
        }

        private static string BuildUnitSelPointStateKeyV137LikeOriginal(List<C2SelectedUnitSelPointV137LikeOriginal> groups, int activeIndex)
        {
            if (groups == null || groups.Count == 0) return string.Empty;
            var sb = new StringBuilder(128);
            sb.Append("active=").Append(activeIndex.ToString(CultureInfo.InvariantCulture)).Append(';');
            for (int i = 0; i < groups.Count; i++)
            {
                C2SelectedUnitSelPointV137LikeOriginal g = groups[i];
                if (g == null) continue;
                sb.Append(g.Key).Append(':').Append(g.Count.ToString(CultureInfo.InvariantCulture)).Append(':').Append(g.NIndex.ToString(CultureInfo.InvariantCulture)).Append('|');
            }
            if (activeIndex >= 0 && activeIndex < groups.Count && groups[activeIndex] != null)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal active = groups[activeIndex].Unit;
                // Tiring changes continuously while a unit walks/fights. Putting it in
                // the structural HUD key rebuilt/destroyed the weapon buttons every
                // 0.2 s, which is exactly why the rifle range flickered on hover.
                // Morale remains in the state key; weapon hover/reload are refreshed
                // independently in LateUpdate.
                sb.Append("morale=")
                  .Append(ResolveMoraleCurrentLikeOriginal(active).ToString(CultureInfo.InvariantCulture))
                  .Append('/')
                  .Append(ResolveMoraleMaxLikeOriginal(active).ToString(CultureInfo.InvariantCulture));
                int avgLifeV404, maxLifeV404;
                if (C2FormationRuntimeV167LikeOriginal.TryGetFormationAverageLifeV404LikeOriginal(
                        active, out avgLifeV404, out maxLifeV404))
                    sb.Append(";life=").Append(avgLifeV404.ToString(CultureInfo.InvariantCulture))
                      .Append('/').Append(maxLifeV404.ToString(CultureInfo.InvariantCulture));
                int standDelayV403LikeOriginal;
                int standMaxV403LikeOriginal;
                bool inStandV403LikeOriginal;
                int standAddDamageV403LikeOriginal;
                int standAddShieldV403LikeOriginal;
                if (C2FormationRuntimeV167LikeOriginal.TryGetStandGroundSnapshotV403LikeOriginal(
                        active, out standDelayV403LikeOriginal, out standMaxV403LikeOriginal,
                        out inStandV403LikeOriginal, out standAddDamageV403LikeOriginal, out standAddShieldV403LikeOriginal))
                {
                    sb.Append(";sg=")
                      .Append(standDelayV403LikeOriginal.ToString(CultureInfo.InvariantCulture))
                      .Append('/')
                      .Append(standMaxV403LikeOriginal.ToString(CultureInfo.InvariantCulture))
                      .Append('/')
                      .Append(inStandV403LikeOriginal ? '1' : '0');
                }
            }
            return sb.ToString();
        }

        private void BuildSelectedUnitSelPointSideCardsV137LikeOriginal(List<C2SelectedUnitSelPointV137LikeOriginal> groups, int activeIndex, bool beforeActive)
        {
            if (groups == null || groups.Count <= 1) return;

            if (beforeActive)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (i >= activeIndex) break;
                    BuildSelectedUnitSelPointSideCardV137LikeOriginal(groups[i], i, activeIndex, true);
                }
            }
            else
            {
                // Original overlap: cards farther from the active center are painted first,
                // then the nearest right card is painted on top of them.
                // V147 drew right cards left->right, so every next pocket covered the previous one.
                for (int i = groups.Count - 1; i > activeIndex; i--)
                    BuildSelectedUnitSelPointSideCardV137LikeOriginal(groups[i], i, activeIndex, false);
            }
        }

        private void BuildSelectedUnitSelPointSideCardV137LikeOriginal(C2SelectedUnitSelPointV137LikeOriginal group, int index, int activeIndex, bool leftOfActive)
        {
            if (group == null || group.Unit == null) return;

            int slotX = index * OriginalSelPointSideWidthV137LikeOriginal;
            int activeCenterX = -2 + Mathf.Max(0, activeIndex) * OriginalSelPointSideWidthV137LikeOriginal;

            // V152: restore the real XML right-side overlap.
            // SelPoint.DialogsDesk.Dialogs.xml:
            //   left side root  x=-2, frame x=0
            //   center root     x=0,  frame width=183
            //   right side root x=34, frame x=100
            // The original UI then shifts each SelPoint by 35 px. Therefore the right frame is
            // index*35 + 34 + 100, not centerRightEdge + step. V151 placed the nearest right
            // pocket after the center card, leaving the constant top/right "hole".
            int rightFrameX = index * OriginalSelPointSideWidthV137LikeOriginal + 134;
            int y = OriginalSelPointSideYV137LikeOriginal;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon = group.Icon;
            int backSprite = ResolveBranchColorSpriteLikeOriginal(icon);
            int nameCircleSprite = icon.Peasant ? 10 : 9; // va_SP_NameCircleSide: base 8, normal +1, peasant +2.

            if (leftOfActive)
            {
                // Original XML: Dialogs/v/SelPoint.DialogsDesk.Dialogs.xml, LEFT side desk:
                // root x=-2 y=479, SelColorLeft x=0, UnitSprSide global x=-10, SideBox global x=-2.
                // The old Unity path was +2 px too far right and drew a fake top amount plate that
                // does not exist for normal unit side portraits; original side amount/morale plate is
                // brigade-only and stays hidden for units.
                AddG16Image("sp_side_left_color_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\SelColorLeft", backSprite, slotX - 2, y, 49, 263, 255, false, false, false);
                // V150: do NOT draw va_SP_UnitSprSide for ordinary side unit pockets.
                // In SelPoint.DialogsDesk.Dialogs.xml this picture exists only inside a parent
                // DialogsDesk with <Visible>false</Visible>, and va_SP_UnitSprSide itself does not
                // set Visible=true in VUI_Actions.cpp. The visible side pocket is SelColorLeft/Right
                // + cropped frame/name/amount; the previous V149 slice was a Unity-only leak.
                AddG16Image("sp_side_left_frame_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 1, slotX - 2, y, 49, 276, 255, false, false, false);
                AddG16Image("sp_side_left_name_circle_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", nameCircleSprite, slotX + 6, y + 68, 21, 153, 255, false, false, false);

                // Original side amount block: Interf3\cropped sprite 3 at x=9 y=9 inside the left side desk,
                // TextButton action cva_SP_id_Amount prints SP->Inf.Units.Amount for normal units.
                AddG16Image("sp_side_left_amount_plate_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 3, slotX + 7, y + 9, 19, 36, 255, false, false, false);
                AddRotatedLabelXmlVerticalV150LikeOriginal("sp_side_left_amount_text_" + index.ToString(CultureInfo.InvariantCulture), group.Count.ToString(CultureInfo.InvariantCulture), slotX + 10, y + 22, 16, 9, 8, true, Color.white);

                AddSelPointSideNameInBalloonV153LikeOriginal("sp_side_left_name_" + index.ToString(CultureInfo.InvariantCulture), group.Title, slotX + 6, y + 68, 21, 153, 9, true, OriginalHudTitleTextColorV141LikeOriginal());
            }
            else
            {
                // V152: real XML placement. This pocket intentionally starts under the active
                // center portrait and is covered by the center card drawn later; only the visible
                // overlapped strip remains, exactly like the original SelPoint stack.
                AddG16Image("sp_side_right_color_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\SelColorRight", backSprite, rightFrameX, y, 26, 263, 255, false, false, false);
                // V150: right side ordinary unit pocket also does not draw va_SP_UnitSprSide.
                // The XML child portrait is under a hidden DialogsDesk and original SetFrameState only
                // changes FileID/SpriteID; it never makes that hidden parent visible.
                AddG16Image("sp_side_right_frame_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 2, rightFrameX, y, 49, 283, 255, false, false, false);
                AddG16Image("sp_side_right_name_circle_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", nameCircleSprite, rightFrameX + 20, y + 68, 21, 153, 255, false, false, false);

                // Original side amount block: Interf3\cropped sprite 3 at x=121 y=9 inside the right side desk.
                AddG16Image("sp_side_right_amount_plate_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 3, rightFrameX + 21, y + 9, 19, 36, 255, false, false, false);
                AddRotatedLabelXmlVerticalV150LikeOriginal("sp_side_right_amount_text_" + index.ToString(CultureInfo.InvariantCulture), group.Count.ToString(CultureInfo.InvariantCulture), rightFrameX + 24, y + 22, 16, 9, 8, true, Color.white);

                AddSelPointSideNameInBalloonV153LikeOriginal("sp_side_right_name_" + index.ToString(CultureInfo.InvariantCulture), group.Title, rightFrameX + 20, y + 68, 21, 153, 9, true, OriginalHudTitleTextColorV141LikeOriginal());
            }

            AddUnitSelPointSideClickAreaV137LikeOriginal(
                "sp_side_click_" + index.ToString(CultureInfo.InvariantCulture),
                leftOfActive ? slotX : rightFrameX,
                y,
                OriginalSelPointSideWidthV137LikeOriginal,
                leftOfActive ? 276 : 283,
                group.Key);
        }

        private void OnUnitSelPointSideClickedV137LikeOriginal(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            Debug.Log("[C2:SELPOINT V152 CLICK] side activate key='" + key + "'");

            _activeUnitSelPointKeyV137LikeOriginal = key;
            _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
            _lastSelectedCount = -999999;
            _nextRefresh = 0.0f;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
        }


        private static int ResolveNationFlagSpriteLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            // Original va_SP_NatFlag does not use player color order and does not use our menu ColorID order.
            // It uses GlobalAI.Ai[SP->NatID].NWaterAI.  AI/ai.dat lines look like:
            // AUSTRIA Austria UnitKri(AU) 9 1 0 Interf3\TotalWarGraph\lva_ASs #HERO_AS_
            string id = unit != null ? (unit.SourceMonsterId ?? string.Empty) : string.Empty;
            string suffix = ExtractNationSuffixLikeOriginal(id);
            int flag;
            if (C2OriginalAiDatFlagsV14.TryGetFlagBySuffix(suffix, out flag)) return flag;
            if (C2OriginalAiDatFlagsV14.TryGetFlagByMember(id, out flag)) return flag;

            // Verified vanilla AI/ai.dat fallback.  This is only a fallback when the active Data-root is missing.
            if (string.Equals(suffix, "FR", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(suffix, "RU", StringComparison.OrdinalIgnoreCase)) return 8;
            if (string.Equals(suffix, "EN", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(suffix, "PR", StringComparison.OrdinalIgnoreCase)) return 7;
            if (string.Equals(suffix, "AU", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(suffix, "EG", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(suffix, "PO", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(suffix, "SP", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(suffix, "RE", StringComparison.OrdinalIgnoreCase)) return 4;

            // Last-resort fallback only for debug objects without a real nation suffix.
            return Mathf.Clamp(unit != null ? unit.Nation : 0, 0, 31);
        }

        private static string ExtractNationSuffixLikeOriginal(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return string.Empty;
            int a = objectId.LastIndexOf('(');
            int b = objectId.LastIndexOf(')');
            if (a >= 0 && b > a + 1)
                return objectId.Substring(a + 1, b - a - 1).Trim();
            return string.Empty;
        }

        private static string ResolveUnitTitleLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 unitIcon, C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string source;
            return ResolveUnitTitleLikeOriginal(unitIcon, unit, out source);
        }

        private static string ResolveUnitTitleLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 unitIcon, C2NeutralPeasantUnitInfoV2LikeOriginal unit, out string source)
        {
            string mdListName = C2OriginalProduceCatalogV13.ResolveMdDisplayNameV141LikeOriginal(unit != null ? unit.ResolvedMd : string.Empty);
            if (string.IsNullOrEmpty(mdListName) && unitIcon.Path != null)
                mdListName = C2OriginalProduceCatalogV13.ResolveMdDisplayNameV141LikeOriginal(unitIcon.Path);
            if (!string.IsNullOrEmpty(mdListName))
            {
                source = "TEXT_MDLIST_V141";
                return mdListName;
            }

            // Original MD MESSAGE can be a ready CP866 literal ("Крестьянин"/"Крепостной"), not only a LocDb key.
            string rawMessage = (unitIcon.MessageKey ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(rawMessage))
            {
                string localized = C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal(rawMessage);
                if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawMessage, StringComparison.OrdinalIgnoreCase))
                {
                    source = "MD_MESSAGE_LOCDB";
                    return localized;
                }
                if (!rawMessage.StartsWith("#", StringComparison.Ordinal))
                {
                    source = "MD_MESSAGE_LITERAL_CP866";
                    return rawMessage;
                }
            }

            string rawName = (unitIcon.NameKey ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(rawName))
            {
                string localized = C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal(rawName);
                if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawName, StringComparison.OrdinalIgnoreCase))
                {
                    source = "MD_NAME_LOCDB";
                    return localized;
                }
            }

            source = "FALLBACK_UNIT_ID";
            return unit != null ? (unit.SourceMonsterId ?? "Unit") : "Unit";
        }

        private static string ResolveMoraleTextLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return ResolveMoraleCurrentLikeOriginal(unit).ToString(CultureInfo.InvariantCulture) + "/" +
                   ResolveMoraleMaxLikeOriginal(unit).ToString(CultureInfo.InvariantCulture);
        }

        private static int ResolveMoraleCurrentLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            float morale;
            float maxMorale;
            if (C2CombatRuntimeV334LikeOriginal.TryGetMoraleSnapshotLikeOriginal(
                    unit, out morale, out maxMorale))
                return Mathf.Max(0, Mathf.FloorToInt(morale));
            return 50;
        }

        private static int ResolveMoraleMaxLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            float morale;
            float maxMorale;
            if (C2CombatRuntimeV334LikeOriginal.TryGetMoraleSnapshotLikeOriginal(
                    unit, out morale, out maxMorale))
                return Mathf.Max(1, Mathf.FloorToInt(maxMorale));
            return 50;
        }


        private static int ResolveNameColorSpriteLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon)
        {
            // va_SP_NameColor:
            // base 27, brigade -> 28, peasant unit -> 29, normal unit -> 27.
            return icon.Peasant ? 29 : 27;
        }

        private static int ResolveNameCircleSpriteLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon)
        {
            // va_SP_NameCircle:
            // base 23, peasant -> 24, normal unit -> 25, brigade -> 26.
            // The current selected object here is a unit, not a brigade/building panel.
            return icon.Peasant ? 24 : 25;
        }

        private static int ResolveBranchColorSpriteLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon)
        {
            // va_SP_BranchColor:
            // if GO->newMons->PortBackSprite != 0xFFFF -> sprite = 1 + PortBackSprite, else sprite 0.
            return icon.HasPortBackSprite ? Mathf.Max(0, 1 + icon.PortBackSprite) : 0;
        }

        private static int ResolveBranchSpriteLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon)
        {
            // va_SP_BranchSprite:
            // if GO->newMons->PortBranch != 0xFFFF -> sprite = PortBranch. XML fallback is sprite 0.
            return icon.HasPortBranch ? Mathf.Max(0, icon.PortBranch) : 0;
        }

        private void BuildSelectedUnitLeftCardLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int selectedCount, int baseOffsetX = 0)
        {
            if (TryRenderSelectedPointXmlUnitLeftCardV125LikeOriginal(unit, selectedCount, baseOffsetX))
                return;

            int baseX = -2 + baseOffsetX;
            const int baseY = 459; // V16: lifted 20 px so the lower SelPoint frame is not clipped below reference 768

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 unitIcon = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);

            // Unit selected-left-card portrait is strictly NM->BIGICON.
            // Do not fall back to INMENUICON/MINICON here: those are button/card icons and make
            // the left portrait differ from the original selected-unit SelPoint stack.
            // V118: restore V116 isolation. Building HUD fixes must never change this unit path.
            string portraitFile = unitIcon.BigIconFile;
            int portraitSprite = unitIcon.BigIconSprite;
            string portraitSource = string.IsNullOrEmpty(portraitFile) ? "BIGICON_MISSING_NO_FALLBACK" : "BIGICON_UNIT_ONLY";

            int flagSprite = ResolveNationFlagSpriteLikeOriginal(unit);
            string titleSource;
            string title = ResolveUnitTitleLikeOriginal(unitIcon, unit, out titleSource);
            int moraleCurrent = ResolveMoraleCurrentLikeOriginal(unit);
            int moraleMax = ResolveMoraleMaxLikeOriginal(unit);
            string moraleText = ResolveMoraleTextLikeOriginal(unit);
            int branchColorSprite = ResolveBranchColorSpriteLikeOriginal(unitIcon);
            int branchSprite = ResolveBranchSpriteLikeOriginal(unitIcon);
            int nameColorSprite = ResolveNameColorSpriteLikeOriginal(unitIcon);
            int nameCircleSprite = ResolveNameCircleSpriteLikeOriginal(unitIcon);

            // Center selected-point card from Dialogs/v/SelPoint.DialogsDesk.Dialogs.xml.
            // IMPORTANT:
            // - BIGICON is the portrait.
            // - MINICON is only for produce/build buttons.
            // - Awards/experience chevrons are not drawn for the peasant baseline. They belong to real exp/awards state,
            //   and drawing static awards here was the previous bug.
            // va_SP_BranchColor: original action sets SelColorCenter to 1 + PORTBACKSPRITE,
            // or sprite 0 when PORTBACKSPRITE is absent. This is the portrait paper/background layer.
            // The visible Bedouin leak was not this layer; it came from GlBuildSel -> Units_egp_mini sprite 0.
            bool drawBranchColor = true;
            bool drawBranchSprite = unitIcon.HasPortBranch;
            AddG16ImageOverpaintV140LikeOriginal("sp_branch_color_selcolorcenter_original", "Interf3\\SelColorCenter", branchColorSprite, baseX + 21, baseY + 43, 139, 237, 255, false, 92, false, false);

            // Original va_SP_BranchSprite is part of the selected-unit portrait stack even for peasants.
            if (drawBranchSprite)
                AddG16ImageOverpaintV140LikeOriginal("sp_branch_sprite_original", "Interf3\\PortBackBranch", branchSprite, baseX + 30, baseY + 80, 49, 92, 255, false, 96, false, false);

            if (!string.IsNullOrEmpty(portraitFile))
                AddG16ImageOverpaintV140LikeOriginal("sp_unit_bigicon_" + portraitSource, portraitFile, portraitSprite, baseX + 21, baseY + 43, 139, 237, 255, false, 160, false, false);

            // Flag is va_SP_NatFlag: GlobalAI.Ai[SP->NatID].NWaterAI from AI/ai.dat, not menu ColorID.
            AddG16Image("sp_nation_flag", "INTERF3\\FLAG", flagSprite, baseX + 26, baseY + 47, 32, 24, 255, false);

            // Original small stat plates from SelPoint XML.
            AddG16Image("sp_kill_counter_plate", "Interf3\\FormInterface", 24, baseX + 124, baseY + 47, 36, 21, 255, false);
            AddG16Image("sp_defence_shield", "Interf3\\FormInterface", 25, baseX + 138, baseY + 229, 17, 22, 255, false);
            AddG16Image("sp_protect_plate", "Interf3\\FormInterface", 24, baseX + 124, baseY + 254, 36, 21, 255, false);

            // External decorative frame pieces from the original selected-point block.
            AddG16ImageOverpaintV140LikeOriginal("sp_portrait_box_original", "Interf3\\cropped", 19, baseX + 0, baseY + 36, 183, 244, 255, false, 64);
            AddG16ImageOverpaintV140LikeOriginal("sp_portrait_bottom_original", "Interf3\\cropped", 20, baseX + 0, baseY + 280, 183, 26, 255, false, 64);
            AddG16ImageOverpaintV140LikeOriginal("sp_name_color_original", "Interf3\\cropped", nameColorSprite, baseX + 0, baseY + 15, 179, 21, 255, false, 64);

            AddG16ImageOverpaintV140LikeOriginal("sp_name_circle_original", "Interf3\\cropped", nameCircleSprite, baseX + 0, baseY + 13, 181, 23, 255, false, 64);

            // Original VUI_Actions.cpp:
            // va_SP_CenUp_One is visible for one selected unit/building/cannon;
            // va_SP_CenUp_Mul is visible for amount > 1 or brigade.
            // In the real VUI the top ornament is evaluated as a child/action over the name-circle area.
            // V112/V113 drew name-circle after CenUp_One, hiding the single-unit top ornament.
            int formationGroupId;
            int formationLive;
            int formationTotal;
            string formationShape;
            byte formationDirection;
            bool isFormation = C2FormationRuntimeV167LikeOriginal.TryGetFormationSummaryV321LikeOriginal(
                unit,
                out formationGroupId,
                out formationLive,
                out formationTotal,
                out formationShape,
                out formationDirection);

            if (isFormation)
            {
                int avgLifeV404, maxLifeV404;
                if (C2FormationRuntimeV167LikeOriginal.TryGetFormationAverageLifeV404LikeOriginal(
                        unit, out avgLifeV404, out maxLifeV404))
                    AddOriginalLifeLineLikeOriginal(baseX + 9, baseY + 58, 2, 200, avgLifeV404, maxLifeV404);
                AddOriginalTiredLineLikeOriginal(baseX + 169, baseY + 58, 3, 200,
                    C2CombatRuntimeV334LikeOriginal.GetFormationTiringRemainingLikeOriginal(unit), unit);
            }

            if (selectedCount <= 1 && !isFormation)
            {
                AddG16ImageOverpaintV140LikeOriginal("sp_center_top_one_original", "Interf3\\cropped", 32, baseX + 13, baseY + 6, 153, 16, 255, false, 64);
            }
            else
            {
                const int cenUpNativeW = 123;
                const int cenUpNativeH = 21;
                int cenUpX = baseX + 13 + (153 - cenUpNativeW) / 2;
                int cenUpY = baseY + 6 + (16 - cenUpNativeH) / 2;
                AddG16ImageOverpaintV140LikeOriginal("sp_center_top_many_original", "Interf3\\cropped", 31, cenUpX, cenUpY, cenUpNativeW, cenUpNativeH, 255, false, 64);

                string countText = isFormation
                    ? formationLive.ToString(CultureInfo.InvariantCulture) + "/" +
                      formationTotal.ToString(CultureInfo.InvariantCulture)
                    : selectedCount.ToString(CultureInfo.InvariantCulture);
                int countW = Mathf.Clamp(countText.Length * 7 + 8, 17, 36);
                int countX = cenUpX + (cenUpNativeW - countW) / 2;
                AddCrispLabelV140LikeOriginal("sp_selected_count", countText, countX, cenUpY + 5, countW, 10, 9, TextAnchor.MiddleCenter, Color.white);
            }

            // Original va_SP_Morale is GP_TextButton:
            // FileID=Interf3\\cropped, Sprite=22, x=55 y=272 w=71 h=15, text inside.
            AddG16Image("sp_morale_gptext_back_original", "Interf3\\cropped", 22, baseX + 55, baseY + 272, 71, 15, 255, false);

            // Original va_SP_MoraleLine Canvas: x=18 y=291 w=144 h=6.
            AddOriginalMoraleLineLikeOriginal(baseX + 18, baseY + 291, 144, 6, moraleCurrent, moraleMax);

            // Unit title text is the CHILD TextButton inside the green filler picture.
            AddCrispLabelV140LikeOriginal("sp_unit_title", title, baseX + 53, baseY + 21, 75, 11, 11, TextAnchor.MiddleCenter, OriginalHudTitleTextColorV141LikeOriginal());

            // Original SelPoint.DialogsDesk + VUI_Actions.cpp::va_SP_Kills.
            // Brigade NKills is shown as Brigade::GetBrigExp() (fixed-point NKills/100).
            int brigadeExperienceV402LikeOriginal = 0;
            int brigadeRawNKillsV402LikeOriginal = 0;
            int brigadeExpGrowSpeedV402LikeOriginal = 100;
            float brigadeAverageKillsV402LikeOriginal = 0.0f;
            int brigadeSkillStatusV402LikeOriginal = 0;
            if (isFormation)
            {
                C2FormationRuntimeV167LikeOriginal.TryGetBrigadeExperienceV402LikeOriginal(
                    unit,
                    out brigadeExperienceV402LikeOriginal,
                    out brigadeRawNKillsV402LikeOriginal,
                    out brigadeExpGrowSpeedV402LikeOriginal,
                    out brigadeAverageKillsV402LikeOriginal,
                    out brigadeSkillStatusV402LikeOriginal);
            }
            AddCrispLabelV140LikeOriginal(
                "sp_kill_counter_v402",
                brigadeExperienceV402LikeOriginal.ToString(CultureInfo.InvariantCulture),
                baseX + 137, baseY + 49, 18, 12, 10, TextAnchor.MiddleCenter, Color.white);

            // VUI_Actions.cpp::va_SP_KillsAward: Rank increments at >20, >60, >120, >300, >600.
            // The five GPPictures are the original Interf3\awards sprites and XML positions.
            if (isFormation && brigadeExperienceV402LikeOriginal > 0)
            {
                int rankV402LikeOriginal = 0;
                if (brigadeExperienceV402LikeOriginal > 20) rankV402LikeOriginal++;
                if (brigadeExperienceV402LikeOriginal > 60) rankV402LikeOriginal++;
                if (brigadeExperienceV402LikeOriginal > 120) rankV402LikeOriginal++;
                if (brigadeExperienceV402LikeOriginal > 300) rankV402LikeOriginal++;
                if (brigadeExperienceV402LikeOriginal > 600) rankV402LikeOriginal++;
                if (rankV402LikeOriginal >= 1) AddG16Image("sp_kill_award_1_v402", "Interf3\\awards", 1, baseX + 138, baseY + 70, 17, 6, 255, false);
                if (rankV402LikeOriginal >= 2) AddG16Image("sp_kill_award_2_v402", "Interf3\\awards", 1, baseX + 138, baseY + 77, 17, 6, 255, false);
                if (rankV402LikeOriginal >= 3) AddG16Image("sp_kill_award_3_v402", "Interf3\\awards", 0, baseX + 137, baseY + 83, 19, 14, 255, false);
                if (rankV402LikeOriginal >= 4) AddG16Image("sp_kill_award_4_v402", "Interf3\\awards", 0, baseX + 137, baseY + 92, 19, 14, 255, false);
                if (rankV402LikeOriginal >= 5) AddG16Image("sp_kill_award_5_v402", "Interf3\\awards", 0, baseX + 137, baseY + 101, 19, 14, 255, false);
            }

            // Bottom shield is decorative; protect/defence value is on the lower FormInterface 24 plate.
            AddCrispLabelV140LikeOriginal("sp_defence_zero", "0", baseX + 143, baseY + 259, 7, 10, 9, TextAnchor.MiddleCenter, Color.white);
            AddCrispLabelV140LikeOriginal("sp_morale_text", moraleText, baseX + 55, baseY + 272, 71, 15, 9, TextAnchor.MiddleCenter, Color.white);
            // Do not inject a debug-style "КОЛОННА/ЛИНИЯ/КАРЕ 120/120"
            // caption into the original selected-unit portrait.
        }

        private bool BuildSelectedUnitWeaponPanelV154LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int activeGroupCount, int selPointCount)
        {
            if (unit == null) return false;

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            if (info.Peasant) return false;
            if (unit.CanBuildOrRepairLikeOriginal()) return false;

            int fireDamage = info.Damage1 > 0 ? info.Damage1 : info.Damage2;
            int fireRadius = info.AttackRadius1 > 0 ? info.AttackRadius1 : info.AttackRadius2;
            int standGroundDisplayDamageV403LikeOriginal =
                C2FormationRuntimeV167LikeOriginal.GetBrigadeStandGroundDamageBonusV403LikeOriginal(unit, null);
            int meleeDisplayDamageV402LikeOriginal = info.Damage0 +
                C2FormationRuntimeV167LikeOriginal.GetBrigadeExperienceDamageBonusV402LikeOriginal(unit, 0, info.Damage0) +
                standGroundDisplayDamageV403LikeOriginal;
            int rifleDisplayDamageV402LikeOriginal = fireDamage +
                C2FormationRuntimeV167LikeOriginal.GetBrigadeExperienceDamageBonusV402LikeOriginal(unit, 1, fireDamage) +
                standGroundDisplayDamageV403LikeOriginal;
            bool hasMelee = info.Damage0 > 0;
            bool hasRifle = info.Damage0 > 0 && fireDamage > 0;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal formationRecordV165;
            bool hasFormationData = C2FormationCreateCatalogV165LikeOriginal.TryResolveForSelectedUnit(unit, out formationRecordV165);
            // VUI_Actions.cpp MiniCom is only for an existing brigade, but cvi_BrigCreate is shown
            // on an ordinary single SelPoint when the selected unit can create a brigade.
            bool hasRuntimeFormationV168 = C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit);
            if (!hasFormationData && hasRuntimeFormationV168)
                hasFormationData = C2FormationRuntimeV167LikeOriginal.TryGetFormationRecordV320LikeOriginal(
                    unit,
                    out formationRecordV165);
            if (!hasMelee && !hasRifle && !hasRuntimeFormationV168) return false;
            bool hasGrenades = hasRuntimeFormationV168 && info.MaxGrenadesInFormation > 0;

            string key = UnitSelPointKeyV137LikeOriginal(unit);

            // Original VUI_Info.cpp:
            // vdsWeap.Setx((OIS.SelPoint.GetAmount()-1)*OIS.SPSideLx);
            // Weapons panel belongs to the whole SelPoint stack, not to the active portrait X.
            // So in a group it starts after the last side pocket.
            int stackShiftX = Mathf.Max(0, selPointCount - 1) * OriginalSelPointSideWidthV137LikeOriginal;
            int baseX = OriginalWeaponPanelBaseXV154LikeOriginal + stackShiftX;
            int baseY = 0;
            int rendered = 0;
            if (hasMelee)
            {
                BuildSelectedUnitWeaponCardV154LikeOriginal(unit, info, key, 0, activeGroupCount, baseX + 0, baseY + OriginalWeaponPanelBaseYV154LikeOriginal, rendered, meleeDisplayDamageV402LikeOriginal, info.AttackRadius0);
                rendered++;
            }
            if (hasRifle)
            {
                BuildSelectedUnitWeaponCardV154LikeOriginal(unit, info, key, 1, activeGroupCount, baseX + 57, baseY + OriginalWeaponPanelBaseYV154LikeOriginal, rendered, rifleDisplayDamageV402LikeOriginal, fireRadius);
                rendered++;
            }
            if (hasGrenades)
            {
                int memberCount = 0;
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> grenadeUnits;
                int grenadeGroupId;
                string grenadeShape;
                if (C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                        unit, out grenadeUnits, out grenadeGroupId, out grenadeShape) &&
                    grenadeUnits != null)
                    memberCount = grenadeUnits.Count;

                int grenadeCurrent;
                int grenadeMax;
                if (!C2FormationRuntimeV167LikeOriginal.TryGetGrenadeStateV326LikeOriginal(
                        unit,
                        info.MaxGrenadesInFormation,
                        info.GrenadeRechargeTime,
                        out grenadeCurrent,
                        out grenadeMax))
                {
                    grenadeMax = Mathf.Max(0, memberCount * info.MaxGrenadesInFormation / 100);
                    grenadeCurrent = 0;
                }
                BuildSelectedUnitGrenadeCardV325LikeOriginal(
                    unit, info, key, baseX + 127, baseY + OriginalWeaponPanelBaseYV154LikeOriginal,
                    rendered, grenadeCurrent, grenadeMax, info.AttackRadius2);
                rendered++;
            }
            if (hasRuntimeFormationV168 && hasFormationData)
            {
                rendered += BuildFormationCommandButtonsV165LikeOriginal(unit, formationRecordV165, activeGroupCount, selPointCount, baseX, baseY + OriginalWeaponPanelBaseYV154LikeOriginal, rendered);
            }

            // va_BR_StandGroundLine is rendered inside the Bayonet card above,
            // in the same child order as the original Weapons.DialogsDesk.Dialogs.xml.
            return rendered > 0;
        }

        private void BuildSelectedUnitGrenadeCardV325LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info,
            string key,
            int x,
            int y,
            int slotIndex,
            int grenadeCurrent,
            int grenadeMax,
            int attackRadius)
        {
            string suffix = slotIndex.ToString(CultureInfo.InvariantCulture) + "_2";
            AddG16ImageOverpaintV140LikeOriginal(
                "weapon_grenade_frame_v325_" + suffix,
                "Interf3\\FormInterface", 40, x, y, 68, 220, 255, false, 56, false, false);
            AddG16ImageOverpaintV140LikeOriginal(
                "weapon_grenade_icon_v325_" + suffix,
                "Interf3\\BigWeapon", 2, x + 4, y + 21, 54, 176, 255, false, 110, false, false);
            AddCrispLabelV140LikeOriginal(
                "weapon_grenade_charge_v325_" + suffix,
                grenadeCurrent.ToString(CultureInfo.InvariantCulture) + "/" + grenadeMax.ToString(CultureInfo.InvariantCulture),
                x + 28, y + 99, 15, 9, 8, TextAnchor.MiddleCenter, Color.white);
            int lineH = grenadeMax > 0
                ? Mathf.Clamp(Mathf.RoundToInt(97.0f * grenadeCurrent / grenadeMax), 0, 97)
                : 0;
            if (lineH > 0)
                AddSolid(
                    "weapon_grenade_charge_line_v325_" + suffix,
                    new Color(0.0f, 0.95f, 0.15f, 0.95f),
                    x + 62, y + 115 + (97 - lineH), 2, lineH, false);
            AddSelectedUnitWeaponClickAreaV154LikeOriginal(
                "weapon_grenade_click_v325_" + suffix,
                x, y, 68, 220, unit, key, 2, attackRadius);
        }

        private void BuildSelectedUnitWeaponCardV154LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info,
            string key,
            int weaponType,
            int activeGroupCount,
            int x,
            int y,
            int slotIndex,
            int displayDamage,
            int attackRadius)
        {
            bool active = weaponType == 1
                ? C2CombatRuntimeV334LikeOriginal.GetFormationRifleAttackStateV398LikeOriginal(unit)
                : _activeWeaponUiStatesV157LikeOriginal.Contains(WeaponUiStateKeyV157LikeOriginal(key, weaponType));
            string suffix = slotIndex.ToString(CultureInfo.InvariantCulture) + "_" + weaponType.ToString(CultureInfo.InvariantCulture);

            if (weaponType == 0)
            {
                int frameSprite = active ? 6 : 5;
                string weapFile = !string.IsNullOrEmpty(info.BigColdWeaponFile) ? info.BigColdWeaponFile : (!string.IsNullOrEmpty(info.BigWeaponFile) ? info.BigWeaponFile : "Interf3\\BigWeapon");
                int weapSprite = info.BigColdWeaponSprite >= 0 ? info.BigColdWeaponSprite : 0;

                AddG16ImageOverpaintV140LikeOriginal("weapon_melee_frame_v154_" + suffix, "Interf3\\FormInterface", frameSprite, x, y, 55, 219, 255, false, 56, false, false);
                AddG16ImageOverpaintV140LikeOriginal("weapon_melee_icon_v154_" + suffix, weapFile, weapSprite, x + 1, y + 21, 54, 176, 255, false, 110, false, false);

                // COSSACKS2 Data1/Dialogs/v/Weapons.DialogsDesk.Dialogs.xml:
                // Bayonet children are ordered as weapon picture -> va_BR_StandGroundLine -> damage text.
                // Keep that draw order so the red stand-ground stripe sits BEHIND the damage number.
                int standDelayV403LikeOriginal;
                int standMaxV403LikeOriginal;
                bool inStandV403LikeOriginal;
                int standAddDamageV403LikeOriginal;
                int standAddShieldV403LikeOriginal;
                if (C2FormationRuntimeV167LikeOriginal.TryGetStandGroundSnapshotV403LikeOriginal(
                        unit, out standDelayV403LikeOriginal, out standMaxV403LikeOriginal,
                        out inStandV403LikeOriginal, out standAddDamageV403LikeOriginal, out standAddShieldV403LikeOriginal) &&
                    standMaxV403LikeOriginal > 0)
                {
                    int displayDelayV403LikeOriginal = standDelayV403LikeOriginal;
                    // UnitsInterface.cpp: when not in full stand-ground and delay is zero,
                    // the UI substitutes BrigDelayMax so the stripe is empty.
                    if (!inStandV403LikeOriginal && displayDelayV403LikeOriginal == 0)
                        displayDelayV403LikeOriginal = standMaxV403LikeOriginal;
                    int standWidthV403LikeOriginal = Mathf.Clamp(
                        41 * (standMaxV403LikeOriginal - displayDelayV403LikeOriginal) / standMaxV403LikeOriginal,
                        0, 41);
                    if (standWidthV403LikeOriginal > 0)
                    {
                        AddSolid(
                            "weapon_standground_line_v403",
                            new Color32(255, 0, 0, 223),
                            x + 7, y + 199, standWidthV403LikeOriginal, 13, false);
                    }
                }

                AddCrispLabelV140LikeOriginal("weapon_melee_damage_v154_" + suffix, displayDamage.ToString(CultureInfo.InvariantCulture), x + 12, y + 200, 31, 10, 9, TextAnchor.MiddleCenter, Color.white);
                AddSelectedUnitWeaponClickAreaV154LikeOriginal("weapon_melee_click_v154_" + suffix, x, y, 55, 219, unit, key, weaponType, attackRadius);
            }
            else
            {
                int frameSprite = active ? 3 : 2;
                string weapFile = !string.IsNullOrEmpty(info.BigFireWeaponFile) ? info.BigFireWeaponFile : (!string.IsNullOrEmpty(info.BigWeaponFile) ? info.BigWeaponFile : "Interf3\\BigWeapon");
                int weapSprite = info.BigFireWeaponSprite >= 0 ? info.BigFireWeaponSprite : 8;

                AddG16ImageOverpaintV140LikeOriginal("weapon_rifle_frame_v154_" + suffix, "Interf3\\FormInterface", frameSprite, x, y, 68, 220, 255, false, 56, false, false);
                AddG16ImageOverpaintV140LikeOriginal("weapon_rifle_icon_v154_" + suffix, weapFile, weapSprite, x + 6, y + 21, 54, 176, 255, false, 110, false, false);

                Text chargeText = AddCrispLabelV140LikeOriginal(
                    "weapon_rifle_charge_v154_" + suffix,
                    Mathf.Max(1, activeGroupCount).ToString(CultureInfo.InvariantCulture),
                    x + 27, y + 6, 17, 9, 8, TextAnchor.MiddleCenter, Color.white);
                AddCrispLabelV140LikeOriginal("weapon_rifle_damage_v154_" + suffix, displayDamage.ToString(CultureInfo.InvariantCulture), x + 17, y + 200, 31, 10, 9, TextAnchor.MiddleCenter, Color.white);

                // V397: va_W_ChargeLine::SetFrameState draws exactly ONE Canvas bar.
                // Do not use AddSolid() here: AddSolid intentionally creates a second
                // full-size brightness pass. RefreshWeaponReloadUi only resizes the
                // returned first Image, so that unbound duplicate stayed at 190 px and
                // made the reload bar look 100% full even when NShots == 0.
                Image reloadLine = AddSolidSinglePassV140ALikeOriginal(
                    "weapon_rifle_charge_line_v154_" + suffix,
                    new Color(0.0f, 1.0f, 0.0f, 1.0f),
                    x + 62, y + 22, 2, 190, false);
                _weaponReloadUiV390LikeOriginal.Add(new C2WeaponReloadUiBindingV390LikeOriginal
                {
                    Unit = unit,
                    Key = key ?? string.Empty,
                    WeaponType = weaponType,
                    ReadyText = chargeText,
                    ReloadLine = reloadLine,
                    X = x + 62,
                    Y = y + 22,
                    MaxHeight = 190
                });
                AddSelectedUnitWeaponClickAreaV154LikeOriginal("weapon_rifle_click_v154_" + suffix, x, y, 68, 220, unit, key, weaponType, attackRadius);
            }
        }

        private void AddSelectedUnitWeaponClickAreaV154LikeOriginal(string name, int x, int y, int w, int h, C2NeutralPeasantUnitInfoV2LikeOriginal unit, string key, int weaponType, int radius)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            C2HudWeaponRelayV154LikeOriginal relay = go.AddComponent<C2HudWeaponRelayV154LikeOriginal>();
            relay.Owner = this;
            relay.Unit = unit;
            relay.Key = key ?? string.Empty;
            relay.WeaponType = weaponType;
            relay.Radius = radius;
            RectTransform clickRect = go.GetComponent<RectTransform>();
            Place(clickRect, x, y, w, h);
            _weaponHoverBindingsV391LikeOriginal.Add(new C2WeaponHoverBindingV391LikeOriginal
            {
                Rect = clickRect,
                Unit = unit,
                WeaponType = weaponType,
                Radius = radius
            });
        }

        private static string WeaponUiStateKeyV157LikeOriginal(string key, int weaponType)
        {
            return (key ?? string.Empty).Trim() + "#" + weaponType.ToString(CultureInfo.InvariantCulture);
        }

        internal void ShowWeaponRangeV154LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int weaponType, int radius)
        {
            if (unit == null) return;

            _weaponRangeHideAtV390LikeOriginal = -1.0f;
            _hoverWeaponRangeUnitV159LikeOriginal = unit;
            _hoverWeaponRangeKeyV159LikeOriginal = UnitSelPointKeyV137LikeOriginal(unit);
            _hoverWeaponRangeTypeV159LikeOriginal = weaponType;
            _hoverWeaponRangeRadiusV159LikeOriginal = radius;
            _nextWeaponRangeDynamicRefreshV392LikeOriginal =
                Time.realtimeSinceStartup + 0.12f;

            RebuildWeaponRangeV159LikeOriginal(true);
        }

        private void RebuildWeaponRangeV159LikeOriginal(bool forceAudit)
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal unit = _hoverWeaponRangeUnitV159LikeOriginal;
            if (unit == null || !unit.isActiveAndEnabled)
            {
                HideWeaponRangeV154LikeOriginal();
                return;
            }

            int weaponType = _hoverWeaponRangeTypeV159LikeOriginal;
            int radius = _hoverWeaponRangeRadiusV159LikeOriginal;

            if (_weaponRangeRootV154LikeOriginal == null)
                _weaponRangeRootV154LikeOriginal = new GameObject("C2_Weapon_Attack_Range_V164_ORIGINAL_RADIUS_ALPHA");

            // The global-brig radius reuses the same static root but does not create its legacy
            // LineRenderer. Hovering a musket afterwards therefore dereferenced a null line.
            if (_weaponRangeLineV154LikeOriginal == null ||
                _weaponRangeLineV154LikeOriginal.gameObject != _weaponRangeRootV154LikeOriginal)
            {
                _weaponRangeLineV154LikeOriginal = _weaponRangeRootV154LikeOriginal.GetComponent<LineRenderer>();
                if (_weaponRangeLineV154LikeOriginal == null)
                    _weaponRangeLineV154LikeOriginal = _weaponRangeRootV154LikeOriginal.AddComponent<LineRenderer>();
                _weaponRangeLineV154LikeOriginal.useWorldSpace = true;
                _weaponRangeLineV154LikeOriginal.loop = true;
                _weaponRangeLineV154LikeOriginal.positionCount = 192;
                _weaponRangeLineV154LikeOriginal.widthMultiplier = 0.085f;
                _weaponRangeLineV154LikeOriginal.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _weaponRangeLineV154LikeOriginal.receiveShadows = false;
                _weaponRangeLineV154LikeOriginal.enabled = false;
                _weaponRangeLineV154LikeOriginal.sortingOrder = 5000;
                _weaponRangeLineV154LikeOriginal.material = CreateWeaponRangeVisibleMaterialV160LikeOriginal(C2AttackRangeLineOuterV162LikeOriginal, 3656);
            }

            ClearWeaponRangeDisksV157LikeOriginal();

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);

            // V390: the retail HUD only highlighted melee opponents, but the port
            // needs an explicit melee attack-zone preview beside rifle/grenade zones.
            int sourceMaxRadius;
            int sourceMinRadius;
            if (weaponType == 0)
            {
                sourceMaxRadius = info.AttackRadius0;
                sourceMinRadius = info.AttackRadius0Min;
            }
            else if (weaponType == 2)
            {
                sourceMaxRadius = info.AttackRadius2;
                sourceMinRadius = info.AttackRadius2Min;
            }
            else
            {
                sourceMaxRadius = info.AttackRadius1 > 0 ? info.AttackRadius1 : info.AttackRadius2;
                sourceMinRadius = info.AttackRadius1 > 0 ? info.AttackRadius1Min : info.AttackRadius2Min;
            }

            int maxRadius = Mathf.Max(0, radius);
            if (maxRadius <= 0)
                maxRadius = sourceMaxRadius;
            if (maxRadius <= 0)
            {
                _weaponRangeRootV154LikeOriginal.SetActive(false);
                if (_weaponRangeScreenRootV160LikeOriginal != null)
                    _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
                ClearWeaponRangeGuiOverlayV161LikeOriginal();
                return;
            }

            int minRadius = Mathf.Max(0, sourceMinRadius);
            // Only the rifle card uses the original ATTPREVIEW tactical bands.
            // Melee and grenade cards show their own clean attack envelope.
            int previewRed = weaponType == 1 ? Mathf.Max(0, info.VisibleRadius1) : 0;
            int previewYellow = weaponType == 1 ? Mathf.Max(0, info.VisibleRadius2) : 0;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> selectedSameType = GetSelectedUnitsOfSelPointKeyV159LikeOriginal(_hoverWeaponRangeKeyV159LikeOriginal);
            if (selectedSameType == null || selectedSameType.Count == 0)
            {
                selectedSameType = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(1);
                selectedSameType.Add(unit);
            }

            Vector3 center = ComputeSelectedUnitsWorldCenterV159LikeOriginal(selectedSameType, unit);
            float y = center.y + 0.70f;
            center.y = y;

            // MD ATTACK_RADIUS/ATTPREVIEW values are in original map-pixel radius space.
            // Do not divide by 16 here: OB->RealX is shifted down before range math in the original path.
            float scale = Mathf.Max(0.01f, Mathf.Abs(unit.MapPixelToWorld));

            // Original VUI_Actions.cpp does NOT use ATTACK_RADIUS0 for the bayonet
            // hover. va_WeapPortBack calls FList.AddMeleeOpponentForBrigade(), and
            // DrawFeatures.cpp draws a 600px enemy-detection circle plus a smart
            // arrow to the nearest hostile brigade. Port that behavior directly.
            if (weaponType == 0)
            {
                const int meleeDetectionRadiusOriginalPixels = 600;
                float detectionWorld = meleeDetectionRadiusOriginalPixels * scale;
                Vector3[] meleeCircle = BuildCenteredCircleV391LikeOriginal(
                    center, detectionWorld, 192);
                AddWeaponRangeCenterFanV162LikeOriginal(
                    "melee_detection_circle_v391",
                    center,
                    meleeCircle,
                    new Color(1.0f, 0.0f, 0.0f, 0.25f),
                    new Color(1.0f, 1.0f, 1.0f, 0.0f),
                    0);
                AddWeaponRangeLineLoopV162LikeOriginal(
                    "melee_detection_line_v391",
                    meleeCircle,
                    new Color(1.0f, 0.0f, 0.0f, 0.95f),
                    4);

                Vector3 enemyCenter;
                C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative;
                if (TryFindNearestEnemyFormationCenterV391LikeOriginal(
                        unit, center, detectionWorld, out enemyCenter, out enemyRepresentative))
                {
                    enemyCenter.y = center.y;
                    AddMeleeAttackArrowV393LikeOriginal(
                        unit, center, enemyCenter, enemyRepresentative);
                }

                if (_weaponRangeLineV154LikeOriginal != null)
                    _weaponRangeLineV154LikeOriginal.enabled = false;
                DisableWeaponRangeScreenAndGuiV162LikeOriginal();
                _weaponRangeRootV154LikeOriginal.SetActive(true);

                string meleeAuditKey = (_hoverWeaponRangeKeyV159LikeOriginal ?? string.Empty) +
                    "#MELEE#" + selectedSameType.Count.ToString(CultureInfo.InvariantCulture);
                if (forceAudit || _lastWeaponRangeAuditKeyV159LikeOriginal != meleeAuditKey ||
                    Time.realtimeSinceStartup >= _nextWeaponRangeAuditV159LikeOriginal)
                {
                    _lastWeaponRangeAuditKeyV159LikeOriginal = meleeAuditKey;
                    _nextWeaponRangeAuditV159LikeOriginal = Time.realtimeSinceStartup + 1.0f;
                    Debug.Log("[C2:MELEE HOVER V394] source='VUI_Actions.cpp::AddMeleeOpponentForBrigade + DrawFeatures.cpp'" +
                              " radiusOriginal=600 selected=" +
                              selectedSameType.Count.ToString(CultureInfo.InvariantCulture) +
                              " enemyArrow=" + (enemyCenter != Vector3.zero ? "1" : "0"));
                }
                return;
            }

            // Original ShowUnitsRanges uses MaxR_Attack*92/100 for the outer attack range.
            // V163 accidentally clamped it to 512 world units; with ATTACK_RADIUS=900 and
            // ATTPREVIEW1=500 that left almost no green band. V164 restores the original
            // radius proportions and keeps only a very high emergency cap.
            float originalOuterRadius = maxRadius > 150 ? maxRadius * 0.92f : maxRadius;
            float outerWorld = Mathf.Max(1.0f, originalOuterRadius * scale);
            if (outerWorld > 4096.0f) outerWorld = 4096.0f;
            float minWorld = minRadius > 0 ? Mathf.Clamp(minRadius * scale, 0.5f, outerWorld) : 0.0f;
            float redWorld = previewRed > 0 ? Mathf.Clamp(previewRed * scale, 0.5f, outerWorld) : 0.0f;
            float yellowWorld = previewYellow > 0 ? Mathf.Clamp(previewYellow * scale, 0.5f, outerWorld) : 0.0f;

            const int segments = 192;

            // Original mapa.cpp::ShowUnitsRanges colors from EngSettings defaults:
            // outer->R1:  AttackRangeFillColorOuter3/Inner3 (green -> yellow)
            // R1->R2:     AttackRangeFillColorOuter2/Inner2 (yellow -> red)
            // R2->center: AttackRangeFillColorOuter1/Inner1 (red -> pale transparent center)
            // lines: outer semi-red, R1 yellow, R2 red.
            Vector3[] outer = BuildSelectedUnitsRangeEnvelopeV159LikeOriginal(selectedSameType, center, outerWorld, segments);
            Vector3[] r1Outline = redWorld > 0.0f ? BuildSelectedUnitsRangeEnvelopeV159LikeOriginal(selectedSameType, center, redWorld, segments) : null;
            Vector3[] r2Outline = yellowWorld > 0.0f ? BuildSelectedUnitsRangeEnvelopeV159LikeOriginal(selectedSameType, center, yellowWorld, segments) : null;

            bool useOriginalPreviewRings = r1Outline != null || r2Outline != null;
            if (useOriginalPreviewRings)
            {
                if (r1Outline == null) r1Outline = outer;
                if (r2Outline == null) r2Outline = r1Outline;

                AddWeaponRangeBandMeshV162LikeOriginal("outer_to_preview1_green", outer, r1Outline, C2AttackRangeFillOuter3V162LikeOriginal, C2AttackRangeFillInner3V162LikeOriginal, 0);
                AddWeaponRangeBandMeshV162LikeOriginal("preview1_to_preview2_yellow", r1Outline, r2Outline, C2AttackRangeFillOuter2V162LikeOriginal, C2AttackRangeFillInner2V162LikeOriginal, 1);
                AddWeaponRangeCenterFanV162LikeOriginal("preview2_to_center_red", center, r2Outline, C2AttackRangeFillOuter1V162LikeOriginal, C2AttackRangeFillInner1V162LikeOriginal, 2);

                AddWeaponRangeLineLoopV162LikeOriginal("line_outer_original_semired", outer, C2AttackRangeLineOuterV162LikeOriginal, 4);
                AddWeaponRangeLineLoopV162LikeOriginal("line_preview1_yellow", r1Outline, C2AttackRangeLineOuter2V162LikeOriginal, 5);
                AddWeaponRangeLineLoopV162LikeOriginal("line_preview2_red", r2Outline, C2AttackRangeLineOuter1V162LikeOriginal, 6);
            }
            else
            {
                AddWeaponRangeCenterFanV162LikeOriginal("simple_range_fill", center, outer, C2AttackRangeFillOuterV162LikeOriginal, C2AttackRangeFillInnerV162LikeOriginal, 0);
                AddWeaponRangeLineLoopV162LikeOriginal("line_simple_outer", outer, C2AttackRangeLineOuterV162LikeOriginal, 4);
            }

            Color c = useOriginalPreviewRings ? C2AttackRangeLineOuterV162LikeOriginal : C2AttackRangeLineOuter1V162LikeOriginal;
            if (_weaponRangeLineV154LikeOriginal.material != null)
                ConfigureWeaponRangeAlwaysVisibleMaterialV159LikeOriginal(_weaponRangeLineV154LikeOriginal.material, c, 3656);
            _weaponRangeLineV154LikeOriginal.startColor = c;
            _weaponRangeLineV154LikeOriginal.endColor = c;
            _weaponRangeLineV154LikeOriginal.positionCount = outer.Length;
            for (int i = 0; i < outer.Length; i++)
            {
                Vector3 p = outer[i];
                p.y = center.y + 0.035f;
                _weaponRangeLineV154LikeOriginal.SetPosition(i, p);
            }

            // V163: world pass only, but render queue is deliberately just before unit sprites
            // (range queue 3650..3664, units queue 3670). ZTest Always keeps it over terrain/roads,
            // and the later unit pass draws every unit over the range fill.
            DisableWeaponRangeScreenAndGuiV162LikeOriginal();

            string auditKey = (_hoverWeaponRangeKeyV159LikeOriginal ?? string.Empty) + "#" + weaponType.ToString(CultureInfo.InvariantCulture) + "#" + selectedSameType.Count.ToString(CultureInfo.InvariantCulture) + "#" + maxRadius.ToString(CultureInfo.InvariantCulture);
            if (forceAudit || _lastWeaponRangeAuditKeyV159LikeOriginal != auditKey || Time.realtimeSinceStartup >= _nextWeaponRangeAuditV159LikeOriginal)
            {
                _lastWeaponRangeAuditKeyV159LikeOriginal = auditKey;
                _nextWeaponRangeAuditV159LikeOriginal = Time.realtimeSinceStartup + 1.0f;
                Debug.Log("[C2:WEAPON RANGE V164] unit='" + (unit != null ? unit.ResolvedMd : string.Empty) +
                    "' weaponType=" + weaponType.ToString(CultureInfo.InvariantCulture) +
                    " selectedSameType=" + selectedSameType.Count.ToString(CultureInfo.InvariantCulture) +
                    " attackMax=" + maxRadius.ToString(CultureInfo.InvariantCulture) +
                    " attackMin=" + minRadius.ToString(CultureInfo.InvariantCulture) +
                    " attPreview1=" + previewRed.ToString(CultureInfo.InvariantCulture) +
                    " attPreview2=" + previewYellow.ToString(CultureInfo.InvariantCulture) +
                    " scale=" + scale.ToString("0.###", CultureInfo.InvariantCulture) +
                    " outerWorld=" + outerWorld.ToString("0.###", CultureInfo.InvariantCulture) +
                    " mode=original_DrawWRect_unit_layer_queue3650_before_unit_queue3670_original92_alpha150 screenOverlay=False onGUI=False ui_only=True");
            }

            _weaponRangeRootV154LikeOriginal.SetActive(true);
        }


        private void UpdateWeaponRangeScreenOverlayV160LikeOriginal(
            Vector3 centerWorld,
            Vector3[] outerWorld,
            Color outerColor,
            Vector3[] redWorld,
            Color redColor,
            Vector3[] yellowWorld,
            Color yellowColor,
            Vector3[] minWorld,
            Color minColor)
        {
            Camera cam = FindBattleCamera();
            if (cam == null)
            {
                if (_weaponRangeScreenRootV160LikeOriginal != null)
                    _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
                ClearWeaponRangeGuiOverlayV161LikeOriginal();
                return;
            }

            // V161 hard fallback: draw the same range directly in OnGUI/GL after the whole camera stack.
            // This bypasses terrain depth, URP camera stack quirks, Canvas sibling order, panel masks and sorting.
            UpdateWeaponRangeGuiOverlayV161LikeOriginal(cam, centerWorld, outerWorld, outerColor, redWorld, redColor, yellowWorld, yellowColor, minWorld, minColor);

            if (_root == null)
                return;

            EnsureWeaponRangeScreenOverlayV160LikeOriginal();
            if (_weaponRangeScreenGraphicV160LikeOriginal == null)
                return;

            Vector2 centerUi;
            Vector2[] outerUi;
            if (!TryProjectWeaponRangePolygonV160LikeOriginal(cam, centerWorld, outerWorld, out centerUi, out outerUi))
            {
                _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
                return;
            }

            _weaponRangeScreenGraphicV160LikeOriginal.ClearPolygons();
            _weaponRangeScreenGraphicV160LikeOriginal.AddPolygon(centerUi, outerUi, outerColor);

            Vector2[] tmp;
            if (redWorld != null && TryProjectWeaponRangePolygonV160LikeOriginal(cam, centerWorld, redWorld, out centerUi, out tmp))
                _weaponRangeScreenGraphicV160LikeOriginal.AddPolygon(centerUi, tmp, redColor);
            if (yellowWorld != null && TryProjectWeaponRangePolygonV160LikeOriginal(cam, centerWorld, yellowWorld, out centerUi, out tmp))
                _weaponRangeScreenGraphicV160LikeOriginal.AddPolygon(centerUi, tmp, yellowColor);
            if (minWorld != null && TryProjectWeaponRangePolygonV160LikeOriginal(cam, centerWorld, minWorld, out centerUi, out tmp))
                _weaponRangeScreenGraphicV160LikeOriginal.AddPolygon(centerUi, tmp, minColor);

            _weaponRangeScreenGraphicV160LikeOriginal.SetVerticesDirty();
            _weaponRangeScreenRootV160LikeOriginal.SetActive(true);
            _weaponRangeScreenRootV160LikeOriginal.transform.SetAsLastSibling();
        }

        private void EnsureWeaponRangeScreenOverlayV160LikeOriginal()
        {
            if (_root == null)
                return;

            if (_weaponRangeScreenRootV160LikeOriginal != null &&
                _weaponRangeScreenRootV160LikeOriginal.transform.parent == _root &&
                _weaponRangeScreenGraphicV160LikeOriginal != null)
                return;

            if (_weaponRangeScreenRootV160LikeOriginal != null)
            {
                if (Application.isPlaying) Destroy(_weaponRangeScreenRootV160LikeOriginal);
                else DestroyImmediate(_weaponRangeScreenRootV160LikeOriginal);
            }

            _weaponRangeScreenRootV160LikeOriginal = new GameObject("C2_HUD_WeaponRange_ScreenOverlay_V160");
            _weaponRangeScreenRootV160LikeOriginal.transform.SetParent(_root, false);
            SetLayerRecursive(_weaponRangeScreenRootV160LikeOriginal, GameplayHudLayer);
            RectTransform rt = _weaponRangeScreenRootV160LikeOriginal.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1024, 768);
            _weaponRangeScreenGraphicV160LikeOriginal = _weaponRangeScreenRootV160LikeOriginal.AddComponent<C2WeaponRangeScreenGraphicV160LikeOriginal>();
            _weaponRangeScreenGraphicV160LikeOriginal.raycastTarget = false;
            _weaponRangeScreenRootV160LikeOriginal.transform.SetAsLastSibling();
        }


        private void UpdateWeaponRangeGuiOverlayV161LikeOriginal(
            Camera cam,
            Vector3 centerWorld,
            Vector3[] outerWorld,
            Color outerColor,
            Vector3[] redWorld,
            Color redColor,
            Vector3[] yellowWorld,
            Color yellowColor,
            Vector3[] minWorld,
            Color minColor)
        {
            if (cam == null || outerWorld == null || outerWorld.Length < 3)
            {
                ClearWeaponRangeGuiOverlayV161LikeOriginal();
                return;
            }

            List<Vector2[]> polys = new List<Vector2[]>(4);
            List<Vector2> centers = new List<Vector2>(4);
            List<Color> colors = new List<Color>(4);

            Vector2 centerGui;
            Vector2[] polyGui;
            if (!TryProjectWeaponRangePolygonGuiV161LikeOriginal(cam, centerWorld, outerWorld, out centerGui, out polyGui))
            {
                ClearWeaponRangeGuiOverlayV161LikeOriginal();
                return;
            }

            centers.Add(centerGui);
            polys.Add(polyGui);
            colors.Add(outerColor);

            if (redWorld != null && TryProjectWeaponRangePolygonGuiV161LikeOriginal(cam, centerWorld, redWorld, out centerGui, out polyGui))
            {
                centers.Add(centerGui);
                polys.Add(polyGui);
                colors.Add(redColor);
            }

            if (yellowWorld != null && TryProjectWeaponRangePolygonGuiV161LikeOriginal(cam, centerWorld, yellowWorld, out centerGui, out polyGui))
            {
                centers.Add(centerGui);
                polys.Add(polyGui);
                colors.Add(yellowColor);
            }

            if (minWorld != null && TryProjectWeaponRangePolygonGuiV161LikeOriginal(cam, centerWorld, minWorld, out centerGui, out polyGui))
            {
                centers.Add(centerGui);
                polys.Add(polyGui);
                colors.Add(minColor);
            }

            _weaponRangeGuiCentersV161LikeOriginal = centers.ToArray();
            _weaponRangeGuiPolysV161LikeOriginal = polys.ToArray();
            _weaponRangeGuiColorsV161LikeOriginal = colors.ToArray();
            _weaponRangeGuiVisibleV161LikeOriginal = _weaponRangeGuiPolysV161LikeOriginal != null && _weaponRangeGuiPolysV161LikeOriginal.Length > 0;
        }

        private void ClearWeaponRangeGuiOverlayV161LikeOriginal()
        {
            _weaponRangeGuiVisibleV161LikeOriginal = false;
            _weaponRangeGuiCentersV161LikeOriginal = new Vector2[0];
            _weaponRangeGuiPolysV161LikeOriginal = new Vector2[0][];
            _weaponRangeGuiColorsV161LikeOriginal = new Color[0];
        }

        private static bool TryProjectWeaponRangePolygonGuiV161LikeOriginal(Camera cam, Vector3 centerWorld, Vector3[] world, out Vector2 centerUi, out Vector2[] ui)
        {
            centerUi = Vector2.zero;
            ui = null;
            if (cam == null || world == null || world.Length < 3)
                return false;

            Vector3 c = cam.WorldToScreenPoint(centerWorld);
            if (c.z <= 0.001f)
                return false;

            float h = Mathf.Max(1, Screen.height);
            centerUi = new Vector2(c.x, h - c.y);
            ui = new Vector2[world.Length];

            for (int i = 0; i < world.Length; i++)
            {
                Vector3 v = cam.WorldToScreenPoint(world[i]);
                ui[i] = new Vector2(v.x, h - v.y);
            }

            return true;
        }

        private static bool TryProjectWeaponRangePolygonV160LikeOriginal(Camera cam, Vector3 centerWorld, Vector3[] world, out Vector2 centerUi, out Vector2[] ui)
        {
            centerUi = Vector2.zero;
            ui = null;
            if (cam == null || world == null || world.Length < 3)
                return false;

            Vector3 c = cam.WorldToViewportPoint(centerWorld);
            if (c.z <= 0.001f)
                return false;

            centerUi = new Vector2(c.x * 1024.0f, -(1.0f - c.y) * 768.0f);
            ui = new Vector2[world.Length];
            for (int i = 0; i < world.Length; i++)
            {
                Vector3 v = cam.WorldToViewportPoint(world[i]);
                ui[i] = new Vector2(v.x * 1024.0f, -(1.0f - v.y) * 768.0f);
            }
            return true;
        }

        private static List<C2NeutralPeasantUnitInfoV2LikeOriginal> GetSelectedUnitsOfSelPointKeyV159LikeOriginal(string key)
        {
            var result = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(32);
            if (string.IsNullOrEmpty(key)) return result;

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled || !u.IsSelected) continue;
                if (!string.Equals(UnitSelPointKeyV137LikeOriginal(u), key, StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(u);
            }
            return result;
        }

        private static Vector3 ComputeSelectedUnitsWorldCenterV159LikeOriginal(List<C2NeutralPeasantUnitInfoV2LikeOriginal> units, C2NeutralPeasantUnitInfoV2LikeOriginal fallback)
        {
            if (units == null || units.Count == 0)
                return fallback != null ? fallback.transform.position : Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                if (u == null || !u.isActiveAndEnabled) continue;
                sum += u.WorldPositionLikeOriginal;
                count++;
            }
            if (count <= 0)
                return fallback != null ? fallback.transform.position : Vector3.zero;
            return sum / count;
        }

        // C2 EngineSettings.h defaults, ARGB DWORD converted to Unity RGBA.
        private static readonly Color C2AttackRangeFillInnerV162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x1EFF0000);
        private static readonly Color C2AttackRangeFillOuterV162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x28FF0000);
        private static readonly Color C2AttackRangeLineOuterV162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x80FF0000);

        private static readonly Color C2AttackRangeFillInner1V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x0AFFFFFF);
        private static readonly Color C2AttackRangeFillOuter1V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x3CFF0000);
        private static readonly Color C2AttackRangeLineOuter1V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0xFFFF0000);

        private static readonly Color C2AttackRangeFillInner2V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x1EFF0000);
        private static readonly Color C2AttackRangeFillOuter2V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x3CFFFF00);
        private static readonly Color C2AttackRangeLineOuter2V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0xFFFFFF00);

        private static readonly Color C2AttackRangeFillInner3V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x1EFFFF00);
        private static readonly Color C2AttackRangeFillOuter3V162LikeOriginal = C2ColorFromArgbV162LikeOriginal(0x3C00FF00);

        private static Color C2ColorFromArgbV162LikeOriginal(uint argb)
        {
            // V164: user requested the range overlay to be about 50% less transparent.
            // This keeps original EngineSettings RGB values and boosts only alpha.
            float a = ((argb >> 24) & 0xFF) / 255.0f;
            a = Mathf.Clamp01(a * 1.50f);
            float r = ((argb >> 16) & 0xFF) / 255.0f;
            float g = ((argb >> 8) & 0xFF) / 255.0f;
            float b = (argb & 0xFF) / 255.0f;
            return new Color(r, g, b, a);
        }

        private static void DisableWeaponRangeScreenAndGuiV162LikeOriginal()
        {
            C2GameplayHudV1 active = _active;
            if (active != null)
            {
                if (active._weaponRangeScreenRootV160LikeOriginal != null)
                    active._weaponRangeScreenRootV160LikeOriginal.SetActive(false);
                active.ClearWeaponRangeGuiOverlayV161LikeOriginal();
            }
        }

        private static void AddWeaponRangeBandMeshV162LikeOriginal(string name, Vector3[] outer, Vector3[] inner, Color outerColor, Color innerColor, int layer)
        {
            if (_weaponRangeRootV154LikeOriginal == null || outer == null || inner == null) return;
            int n = Mathf.Min(outer.Length, inner.Length);
            if (n < 3) return;

            GameObject go = new GameObject("C2_WeaponRange_V164_" + name);
            go.transform.SetParent(_weaponRangeRootV154LikeOriginal.transform, false);

            Mesh mesh = new Mesh();
            mesh.name = "C2_WeaponRangeBand_V164_" + name;
            Vector3[] vertices = new Vector3[n * 2];
            Color[] colors = new Color[n * 2];
            int[] tris = new int[n * 6];
            float yOff = 0.020f + layer * 0.002f;

            for (int i = 0; i < n; i++)
            {
                Vector3 o = outer[i];
                Vector3 inn = inner[i];
                o.y += yOff;
                inn.y += yOff;
                int vi = i * 2;
                vertices[vi] = o;
                vertices[vi + 1] = inn;
                colors[vi] = outerColor;
                colors[vi + 1] = innerColor;
            }

            for (int i = 0; i < n; i++)
            {
                int ni = (i + 1) % n;
                int t = i * 6;
                int o0 = i * 2;
                int i0 = o0 + 1;
                int o1 = ni * 2;
                int i1 = o1 + 1;
                tris[t + 0] = o0;
                tris[t + 1] = o1;
                tris[t + 2] = i0;
                tris[t + 3] = i0;
                tris[t + 4] = o1;
                tris[t + 5] = i1;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 5000 + layer;
            mr.sharedMaterial = CreateWeaponRangeVisibleMaterialV160LikeOriginal(Color.white, 3650 + layer);
        }

        private static void AddWeaponRangeCenterFanV162LikeOriginal(string name, Vector3 center, Vector3[] outline, Color outerColor, Color innerColor, int layer)
        {
            if (_weaponRangeRootV154LikeOriginal == null || outline == null || outline.Length < 3) return;
            int n = outline.Length;
            GameObject go = new GameObject("C2_WeaponRange_V164_" + name);
            go.transform.SetParent(_weaponRangeRootV154LikeOriginal.transform, false);

            Mesh mesh = new Mesh();
            mesh.name = "C2_WeaponRangeFan_V164_" + name;
            Vector3[] vertices = new Vector3[n + 1];
            Color[] colors = new Color[n + 1];
            int[] tris = new int[n * 3];
            float yOff = 0.020f + layer * 0.002f;

            Vector3 c = center;
            c.y += yOff;
            vertices[0] = c;
            colors[0] = innerColor;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = outline[i];
                p.y += yOff;
                vertices[i + 1] = p;
                colors[i + 1] = outerColor;
            }
            for (int i = 0; i < n; i++)
            {
                int t = i * 3;
                tris[t + 0] = 0;
                tris[t + 1] = i + 1;
                tris[t + 2] = (i + 1) % n + 1;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 5000 + layer;
            mr.sharedMaterial = CreateWeaponRangeVisibleMaterialV160LikeOriginal(Color.white, 3650 + layer);
        }

        private static void AddWeaponRangeLineLoopV162LikeOriginal(string name, Vector3[] outline, Color color, int layer)
        {
            if (_weaponRangeRootV154LikeOriginal == null || outline == null || outline.Length < 2) return;
            GameObject go = new GameObject("C2_WeaponRange_V164_" + name);
            go.transform.SetParent(_weaponRangeRootV154LikeOriginal.transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = outline.Length;
            lr.widthMultiplier = 0.070f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 5010 + layer;
            lr.startColor = color;
            lr.endColor = color;
            lr.material = CreateWeaponRangeVisibleMaterialV160LikeOriginal(color, 3658 + layer);

            for (int i = 0; i < outline.Length; i++)
            {
                Vector3 p = outline[i];
                p.y += 0.040f + layer * 0.002f;
                lr.SetPosition(i, p);
            }
        }

        private static Vector3[] BuildCenteredCircleV391LikeOriginal(
            Vector3 center, float radiusWorld, int segments)
        {
            segments = Mathf.Clamp(segments, 32, 384);
            Vector3[] points = new Vector3[segments];
            float radius = Mathf.Max(0.01f, radiusWorld);
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2.0f * i / segments;
                points[i] = new Vector3(
                    center.x + Mathf.Cos(a) * radius,
                    center.y,
                    center.z + Mathf.Sin(a) * radius);
            }
            return points;
        }

        private static bool TryFindNearestEnemyFormationCenterV391LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal source,
            Vector3 sourceCenter,
            float maximumWorldDistance,
            out Vector3 enemyCenter,
            out C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative)
        {
            enemyCenter = Vector3.zero;
            enemyRepresentative = null;
            if (source == null) return false;

            float scale = Mathf.Max(0.0001f, Mathf.Abs(source.MapPixelToWorld));
            float radiusOriginalPixels = maximumWorldDistance / scale;
            if (radiusOriginalPixels <= 0.0f) radiusOriginalPixels = 600.0f;

            int enemyGroupId;
            float sx, sy, ex, ey, distance;
            string audit;
            bool found = C2FormationRuntimeV167LikeOriginal.TryFindNearestEnemyFormationV395LikeOriginal(
                source, radiusOriginalPixels,
                out enemyRepresentative, out enemyGroupId,
                out sx, out sy, out ex, out ey, out distance, out audit);

            C2BattleTerrainMode mode = source.OwnerMode != null
                ? source.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (found && mode != null)
                enemyCenter = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(ex, ey);

            if (Time.frameCount % 20 == 0)
                Debug.Log("[C2:MELEE FIND V395] " + audit + " found=" + (found ? "1" : "0"));
            return found && enemyRepresentative != null;
        }


        // mapa.cpp::DrawSmartArrow exact geometry path used by DrawFeatures.cpp for
        // brigade melee previews. This arrow is PROCEDURAL in retail Cossacks II;
        // it is not a G16 picture. EngineSettings defaults are 80->32 px and
        // ARGB 0x400000FF -> 0x80FF0000 (transparent blue -> transparent red).
        internal static void AddMeleeAttackArrowV393LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal source,
            Vector3 fromWorld,
            Vector3 toWorld,
            C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative,
            Transform explicitParentV395 = null)
        {
            Transform arrowParentV395 = explicitParentV395 != null
                ? explicitParentV395
                : (_weaponRangeRootV154LikeOriginal != null ? _weaponRangeRootV154LikeOriginal.transform : null);
            if (arrowParentV395 == null || source == null) return;
            C2BattleTerrainMode mode = source.OwnerMode != null
                ? source.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode == null) return;

            float x0, y0, x1, y1;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(fromWorld, out x0, out y0) ||
                !mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(toWorld, out x1, out y1))
                return;

            byte startDirection = source.RealDir;
            int groupId, live, total;
            string shape;
            byte groupDirection;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationSummaryV321LikeOriginal(
                    source, out groupId, out live, out total, out shape, out groupDirection))
                startDirection = groupDirection;

            float ddx = x1 - x0;
            float ddy = y1 - y0;
            float r = Mathf.Sqrt(ddx * ddx + ddy * ddy);
            int np = Mathf.FloorToInt(r / 16.0f);
            if (np < 5) return;
            if (np > 700) np = 700;
            float st = r / Mathf.Max(1, np);

            float dx = C2OriginalMovementMathV352.TCos[startDirection] * r / 256.0f;
            float dy = C2OriginalMovementMathV352.TSin[startDirection] * r / 256.0f;
            if (ddx * dx + ddy * dy < 0.0f)
            {
                dx = -dx;
                dy = -dy;
            }

            int nw = np + 1;
            float[] wx = new float[nw];
            float[] wy = new float[nw];
            float[] wt = new float[nw];
            for (int i = 0; i <= np; i++)
            {
                float t = i / (float)np;
                wx[i] = x0 + dx * t * (1.0f - t) + ddx * t * t;
                wy[i] = y0 + dy * t * (1.0f - t) + ddy * t * t;
                wt[i] = 80.0f + (32.0f - 80.0f) * t;
            }

            float ax = 2.0f * (ddx - dx);
            float ay = 2.0f * (ddy - dy);
            float[] xl = new float[nw];
            float[] yl = new float[nw];
            float[] xr = new float[nw];
            float[] yr = new float[nw];
            Color[] pathColor = new Color[nw];
            Color startColor = new Color(0.0f, 0.0f, 1.0f, 64.0f / 255.0f);
            Color finalColor = new Color(1.0f, 0.0f, 0.0f, 128.0f / 255.0f);

            for (int i = 0; i < nw - 1; i++)
            {
                float t = i / (float)np;
                float vx = dx * (1.0f - 2.0f * t) + 2.0f * ddx;
                float vy = dy * (1.0f - 2.0f * t) + 2.0f * ddy;
                float wl = wt[i];
                float wr = wt[i];
                float v2 = vx * vx + vy * vy;
                if (v2 > 0.0001f)
                {
                    float at = (ax * vy - ay * vx) / Mathf.Sqrt(v2);
                    if (Mathf.Abs(at) > 0.0001f)
                    {
                        float rm = Mathf.Abs(v2 / at / 5.0f);
                        if (at < 0.0f)
                        {
                            if (wl > rm) wl = rm;
                        }
                        else
                        {
                            if (wr > rm) wr = rm;
                        }
                    }
                }

                float sx = wx[i + 1] - wx[i];
                float sy = wy[i + 1] - wy[i];
                float px = -sy;
                float py = sx;
                float plen = Mathf.Sqrt(px * px + py * py);
                if (plen < 0.0001f) plen = 1.0f;
                xl[i] = wx[i] + wl * px / plen / 2.0f;
                yl[i] = wy[i] + wl * py / plen / 2.0f;
                xr[i] = wx[i] - wr * px / plen / 2.0f;
                yr[i] = wy[i] - wr * py / plen / 2.0f;
                pathColor[i] = Color.Lerp(startColor, finalColor, i / (float)Mathf.Max(1, nw - 1));
            }
            pathColor[nw - 1] = finalColor;

            int headSegments = Mathf.Min(nw, Mathf.FloorToInt((32.0f / Mathf.Max(0.01f, st)) * 2.0f + 1.0f));
            int bodyEnd = Mathf.Clamp(nw - headSegments, 1, nw - 2);

            var vertices = new List<Vector3>((bodyEnd + 1) * 2 + 3);
            var colors = new List<Color>((bodyEnd + 1) * 2 + 3);
            var triangles = new List<int>(bodyEnd * 6 + 3);

            for (int i = 0; i <= bodyEnd; i++)
            {
                int si = Mathf.Min(i, nw - 2);
                Vector3 l = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(xl[si], yl[si]);
                Vector3 rr = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(xr[si], yr[si]);
                l.y += 0.11f;
                rr.y += 0.11f;
                vertices.Add(l);
                vertices.Add(rr);
                Color c = pathColor[si];
                colors.Add(c);
                colors.Add(c);
                if (i > 0)
                {
                    int b = i * 2;
                    triangles.Add(b - 2); triangles.Add(b); triangles.Add(b - 1);
                    triangles.Add(b); triangles.Add(b + 1); triangles.Add(b - 1);
                }
            }

            // Original source doubles the last left/right displacement before
            // drawing V1->tip<-V3, producing the broad translucent spearhead.
            int hi = Mathf.Min(bodyEnd, nw - 2);
            float hxl = xl[hi] + (xl[hi] - wx[hi]);
            float hyl = yl[hi] + (yl[hi] - wy[hi]);
            float hxr = xr[hi] + (xr[hi] - wx[hi]);
            float hyr = yr[hi] + (yr[hi] - wy[hi]);
            Vector3 hl = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(hxl, hyl);
            Vector3 hr = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(hxr, hyr);
            Vector3 tip = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y1);
            hl.y += 0.115f; hr.y += 0.115f; tip.y += 0.115f;
            int hbase = vertices.Count;
            vertices.Add(hl); vertices.Add(tip); vertices.Add(hr);
            colors.Add(pathColor[hi]); colors.Add(finalColor); colors.Add(pathColor[hi]);
            triangles.Add(hbase); triangles.Add(hbase + 1); triangles.Add(hbase + 2);

            GameObject go = new GameObject("C2_MeleeSmartArrow_V393");
            go.transform.SetParent(arrowParentV395, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            mesh.name = "C2_MeleeSmartArrow_V393_Mesh";
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 5035;
            mr.sharedMaterial = CreateWeaponRangeVisibleMaterialV160LikeOriginal(Color.white, 3685);
        }

        // mapa.cpp:4431: when the mouse is over an enemy OneObject belonging to
        // a brigade and exactly one friendly brigade is selected, C2 calls
        // FList.AddArrowBetweenBrigades(OnlyOneBrig, BR). This is separate from
        // hovering the bayonet card.
        internal static void ShowEnemyBrigadeHoverArrowV395LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal source,
            C2NeutralPeasantUnitInfoV2LikeOriginal target)
        {
            if (source == null || target == null)
            {
                HideEnemyBrigadeHoverArrowV395LikeOriginal();
                return;
            }

            int sg, sn, tg, tn;
            float sx, sy, tx, ty;
            byte sd, td;
            C2NeutralPeasantUnitInfoV2LikeOriginal sr, tr;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationCenterV395LikeOriginal(
                    source, out sg, out sn, out sx, out sy, out sd, out sr) ||
                !C2FormationRuntimeV167LikeOriginal.TryGetFormationCenterV395LikeOriginal(
                    target, out tg, out tn, out tx, out ty, out td, out tr) ||
                sg == tg || sn == tn)
            {
                HideEnemyBrigadeHoverArrowV395LikeOriginal();
                return;
            }

            if (_enemyBrigadeHoverArrowRootV395LikeOriginal == null)
                _enemyBrigadeHoverArrowRootV395LikeOriginal = new GameObject("C2_EnemyBrigadeHoverSmartArrow_V395");

            bool samePair = sg == _enemyBrigadeHoverArrowSourceGroupV395LikeOriginal &&
                            tg == _enemyBrigadeHoverArrowTargetGroupV395LikeOriginal;
            if (samePair && _enemyBrigadeHoverArrowRootV395LikeOriginal.activeSelf &&
                Time.realtimeSinceStartup < _enemyBrigadeHoverArrowNextRefreshV395LikeOriginal)
                return;

            _enemyBrigadeHoverArrowSourceGroupV395LikeOriginal = sg;
            _enemyBrigadeHoverArrowTargetGroupV395LikeOriginal = tg;
            _enemyBrigadeHoverArrowNextRefreshV395LikeOriginal = Time.realtimeSinceStartup + 0.10f;

            for (int i = _enemyBrigadeHoverArrowRootV395LikeOriginal.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = _enemyBrigadeHoverArrowRootV395LikeOriginal.transform.GetChild(i);
                if (child == null) continue;
                child.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(child.gameObject);
                else UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            C2BattleTerrainMode mode = source.OwnerMode != null
                ? source.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode == null) return;
            Vector3 from = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(sx, sy);
            Vector3 to = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(tx, ty);
            from.y += 0.72f;
            to.y = from.y;
            _enemyBrigadeHoverArrowRootV395LikeOriginal.SetActive(true);
            AddMeleeAttackArrowV393LikeOriginal(
                source, from, to, target, _enemyBrigadeHoverArrowRootV395LikeOriginal.transform);
        }

        internal static void HideEnemyBrigadeHoverArrowV395LikeOriginal()
        {
            if (_enemyBrigadeHoverArrowRootV395LikeOriginal != null)
                _enemyBrigadeHoverArrowRootV395LikeOriginal.SetActive(false);
            _enemyBrigadeHoverArrowSourceGroupV395LikeOriginal = -1;
            _enemyBrigadeHoverArrowTargetGroupV395LikeOriginal = -1;
        }

        private static int IssueMeleeAttackOnNearestEnemyV393LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal source,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> attackers,
            int sourceGroupId,
            string sourceShape)
        {
            if (source == null || attackers == null || attackers.Count == 0) return 0;

            C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative;
            int enemyGroupId;
            float sx, sy, ex, ey, distanceOriginal;
            string findAudit;
            if (!C2FormationRuntimeV167LikeOriginal.TryFindNearestEnemyFormationV395LikeOriginal(
                    source, 600.0f, out enemyRepresentative, out enemyGroupId,
                    out sx, out sy, out ex, out ey, out distanceOriginal, out findAudit) ||
                enemyRepresentative == null)
            {
                Debug.Log("[C2:MELEE BUTTON V399] source='Multi.cpp::SetArmAttackState->MoveBrigadeForwardToAttack' result=no_enemy " + findAudit);
                return 0;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> victims;
            int resolvedEnemyGroup;
            string enemyShape;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    enemyRepresentative, out victims, out resolvedEnemyGroup, out enemyShape) ||
                victims == null || victims.Count == 0)
                return 0;

            float dxOriginal = ex - sx;
            float dyOriginal = ey - sy;
            float dOriginal = Mathf.Max(0.001f, Mathf.Sqrt(dxOriginal * dxOriginal + dyOriginal * dyOriginal));

            byte sourceDirection = source.RealDir;
            int sg, live, total; string resolvedSourceShape; byte resolvedSourceDirection;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationSummaryV321LikeOriginal(
                    source, out sg, out live, out total, out resolvedSourceShape, out resolvedSourceDirection))
            {
                sourceDirection = resolvedSourceDirection;
                if (string.IsNullOrEmpty(sourceShape)) sourceShape = resolvedSourceShape;
            }
            byte enemyDirection = enemyRepresentative.RealDir;
            int eg, elive, etotal; string resolvedEnemyShape; byte resolvedEnemyDirection;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationSummaryV321LikeOriginal(
                    enemyRepresentative, out eg, out elive, out etotal, out resolvedEnemyShape, out resolvedEnemyDirection))
            {
                enemyDirection = resolvedEnemyDirection;
                if (string.IsNullOrEmpty(enemyShape)) enemyShape = resolvedEnemyShape;
            }

            byte attackDirection = C2OriginalMovementMathV352.GetDir(Mathf.RoundToInt(dxOriginal), Mathf.RoundToInt(dyOriginal));
            byte destDirection = sourceDirection;
            if (Mathf.Abs((sbyte)(attackDirection - sourceDirection)) < 36) destDirection = attackDirection;
            bool sourceLine = !string.IsNullOrEmpty(sourceShape) && sourceShape.IndexOf("LINE", StringComparison.OrdinalIgnoreCase) >= 0;
            bool enemyLine = !string.IsNullOrEmpty(enemyShape) && enemyShape.IndexOf("LINE", StringComparison.OrdinalIgnoreCase) >= 0;
            if (sourceLine && enemyLine)
            {
                int parallel = Mathf.Abs((sbyte)(enemyDirection - sourceDirection));
                int opposite = Mathf.Abs((sbyte)(enemyDirection - sourceDirection - 128));
                if (parallel < 38) destDirection = enemyDirection;
                else if (opposite < 38) destDirection = (byte)(enemyDirection + 128);
            }

            // Multi.cpp::ShiftDestPoint(xx,yy,xe,ye,80). Brigade centres are Real>>4.
            float destOriginalX = ex + dxOriginal * 80.0f / dOriginal;
            float destOriginalY = ey + dyOriginal * 80.0f / dOriginal;
            float destRealX = destOriginalX * 16.0f;
            float destRealY = destOriginalY * 16.0f;
            string moveAudit;
            int moved = C2GameplayLooseGroupMoveLikeOriginal.IssueMoveLikeOriginal(
                attackers, destRealX, destRealY, true, destDirection,
                "Multi.cpp_MoveBrigadeForwardToAttack_V399", out moveAudit);

            var liveVictims = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; i < victims.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal v = victims[i];
                if (v != null && v.isActiveAndEnabled && !v.IsDeadLikeOriginal) liveVictims.Add(v);
            }
            if (liveVictims.Count == 0) return 0;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> combatAttackersV396;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                    source, out combatAttackersV396) || combatAttackersV396 == null)
                combatAttackersV396 = attackers;

            int armed = 0;
            for (int i = 0; i < combatAttackersV396.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal attacker = combatAttackersV396[i];
                if (attacker == null || !attacker.CanReceivePlayerOrdersLikeOriginal() || attacker.IsDeadLikeOriginal) continue;
                C2CombatRuntimeV334LikeOriginal.SetCommandWeaponModeLikeOriginal(attacker, 0);
                C2NeutralPeasantUnitInfoV2LikeOriginal victim =
                    FindNearestEnemyForMeleeGroupV399LikeOriginal(attacker, liveVictims);
                if (victim == null) continue;
                C2CombatRuntimeV334LikeOriginal combat = attacker.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                GameObject proxy = combat == null ? attacker.EnsureUnityProxyLikeOriginal() : null;
                if (combat == null && proxy != null) combat = proxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
                if (combat == null) continue;
                combat.BeginAttackForcedModeV395LikeOriginal(attacker, victim, null, victim.WorldPositionLikeOriginal, 0);
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    attacker, C2UnitOrderKindV325LikeOriginal.MeleeAttack,
                    "Multi.cpp_MoveBrigadeForwardToAttack", "attack_slot_0");
                armed++;
            }

            Debug.Log("[C2:MELEE BUTTON V399] source='Multi.cpp:4804 SetArmAttackState -> 4599 MoveBrigadeForwardToAttack'" +
                      " sourceGroup=" + sourceGroupId.ToString(CultureInfo.InvariantCulture) +
                      " enemyGroup=" + enemyGroupId.ToString(CultureInfo.InvariantCulture) +
                      " distOriginal=" + distanceOriginal.ToString("0.0", CultureInfo.InvariantCulture) +
                      " moved=" + moved.ToString(CultureInfo.InvariantCulture) +
                      " armed=" + armed.ToString(CultureInfo.InvariantCulture) +
                      " forcedMode=0 destOriginal=(" + destOriginalX.ToString("0", CultureInfo.InvariantCulture) +
                      "," + destOriginalY.ToString("0", CultureInfo.InvariantCulture) + ") " + moveAudit);
            return armed;
        }


        private static C2NeutralPeasantUnitInfoV2LikeOriginal FindNearestEnemyForMeleeGroupV399LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> victims)
        {
            if (attacker == null || victims == null || victims.Count == 0) return null;
            float ax = attacker.RealXFloat != 0.0f ? attacker.RealXFloat : attacker.RealX;
            float ay = attacker.RealYFloat != 0.0f ? attacker.RealYFloat : attacker.RealY;
            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            float best2 = float.MaxValue;
            for (int i = 0; i < victims.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = victims[i];
                if (candidate == null || candidate.IsDeadLikeOriginal || !candidate.isActiveAndEnabled) continue;
                float cx = candidate.RealXFloat != 0.0f ? candidate.RealXFloat : candidate.RealX;
                float cy = candidate.RealYFloat != 0.0f ? candidate.RealYFloat : candidate.RealY;
                float dx = cx - ax;
                float dy = cy - ay;
                float d2 = dx * dx + dy * dy;
                if (d2 >= best2) continue;
                best2 = d2;
                best = candidate;
            }
            return best;
        }

        private static Vector3[] BuildSelectedUnitsRangeEnvelopeV159LikeOriginal(List<C2NeutralPeasantUnitInfoV2LikeOriginal> units, Vector3 center, float radiusWorld, int segments)
        {
            segments = Mathf.Clamp(segments, 32, 384);
            Vector3[] points = new Vector3[segments];
            float r = Mathf.Max(0.01f, radiusWorld);
            float r2 = r * r;

            for (int i = 0; i < segments; i++)
            {
                float a = (Mathf.PI * 2.0f) * i / segments;
                float dx = Mathf.Cos(a);
                float dz = Mathf.Sin(a);
                float best = 0.0f;

                for (int uIndex = 0; units != null && uIndex < units.Count; uIndex++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = units[uIndex];
                    if (u == null || !u.isActiveAndEnabled) continue;
                    Vector3 up = u.WorldPositionLikeOriginal;
                    float ox = up.x - center.x;
                    float oz = up.z - center.z;
                    float along = ox * dx + oz * dz;
                    float off2 = ox * ox + oz * oz - along * along;
                    if (off2 > r2) continue;
                    float reach = along + Mathf.Sqrt(Mathf.Max(0.0f, r2 - off2));
                    if (reach > best) best = reach;
                }

                if (best <= 0.001f) best = r;
                points[i] = new Vector3(center.x + dx * best, center.y, center.z + dz * best);
            }

            return points;
        }

        private static Material CreateWeaponRangeVisibleMaterialV160LikeOriginal(Color color, int queue)
        {
            // V163: the range meshes rely on per-vertex colors. Sprites/Default can silently
            // lose those colors on some Unity/URP paths, which made V162 calculate ranges but
            // draw nothing. Internal-Colored is the closest Unity equivalent to original
            // DrawWRect: vertex color + alpha blend + no texture dependency.
            Shader sh = Shader.Find("Hidden/Internal-Colored");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");

            Material mat = sh != null ? new Material(sh) : new Material(Shader.Find("Sprites/Default"));
            ConfigureWeaponRangeAlwaysVisibleMaterialV159LikeOriginal(mat, color, queue);
            return mat;
        }

        private static void ConfigureWeaponRangeAlwaysVisibleMaterialV159LikeOriginal(Material mat, Color color, int queue)
        {
            if (mat == null) return;

            mat.color = color;
            mat.renderQueue = queue;

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f); // URP transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0.0f);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_OFF");
            mat.DisableKeyword("_ALPHATEST_ON");
        }

        private static void ClearWeaponRangeDisksV157LikeOriginal()
        {
            s_lastGlobalBrigRangeBuildKeyV172LikeOriginal = string.Empty;
            if (_weaponRangeRootV154LikeOriginal == null) return;
            for (int i = _weaponRangeRootV154LikeOriginal.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = _weaponRangeRootV154LikeOriginal.transform.GetChild(i);
                if (child == null) continue;
                // Destroy() is deferred. Leaving the old transparent mesh enabled
                // for the rest of the frame doubles alpha with the replacement and
                // produces the visible rifle-range pulsing/flicker.
                child.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(child.gameObject);
                else UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void AddWeaponRangeEnvelopeDiskV159LikeOriginal(string name, Vector3 center, Vector3[] outline, Color color, int layer)
        {
            if (_weaponRangeRootV154LikeOriginal == null || outline == null || outline.Length < 3) return;

            GameObject go = new GameObject("C2_WeaponRange_" + name);
            go.transform.SetParent(_weaponRangeRootV154LikeOriginal.transform, false);

            Mesh mesh = new Mesh();
            mesh.name = "C2_WeaponRangeEnvelope_" + name;

            int segments = outline.Length;
            Vector3[] vertices = new Vector3[segments + 1];
            int[] tris = new int[segments * 3];
            vertices[0] = new Vector3(center.x, center.y + 0.015f * layer, center.z);
            for (int i = 0; i < segments; i++)
            {
                Vector3 p = outline[i];
                p.y = center.y + 0.015f * layer;
                vertices[i + 1] = p;
            }
            for (int i = 0; i < segments; i++)
            {
                int t = i * 3;
                tris[t + 0] = 0;
                tris[t + 1] = i + 1;
                tris[t + 2] = (i + 1) % segments + 1;
            }
            Color[] colors = new Color[vertices.Length];
            for (int ci = 0; ci < colors.Length; ci++) colors[ci] = color;
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = -20000 + layer;

            Material mat = CreateWeaponRangeVisibleMaterialV160LikeOriginal(color, 2450 + layer);
            mr.sharedMaterial = mat;
        }

        private void PollWeaponCardHoverV391LikeOriginal()
        {
            if (_weaponHoverBindingsV391LikeOriginal.Count == 0 || _canvas == null)
                return;

            Vector2 pointer;
            if (!TryGetPointerPositionV391LikeOriginal(out pointer))
                return;

            Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : (_canvas.worldCamera != null ? _canvas.worldCamera : FindBattleCamera());
            C2WeaponHoverBindingV391LikeOriginal hovered = null;
            for (int i = _weaponHoverBindingsV391LikeOriginal.Count - 1; i >= 0; i--)
            {
                C2WeaponHoverBindingV391LikeOriginal b = _weaponHoverBindingsV391LikeOriginal[i];
                if (b == null || b.Rect == null || b.Unit == null || !b.Rect.gameObject.activeInHierarchy)
                    continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(b.Rect, pointer, eventCamera))
                {
                    hovered = b;
                    break;
                }
            }

            if (hovered != null)
            {
                _weaponRangeHideAtV390LikeOriginal = -1.0f;
                if (_hoverWeaponRangeUnitV159LikeOriginal != hovered.Unit ||
                    _hoverWeaponRangeTypeV159LikeOriginal != hovered.WeaponType ||
                    _hoverWeaponRangeRadiusV159LikeOriginal != hovered.Radius ||
                    _weaponRangeRootV154LikeOriginal == null ||
                    !_weaponRangeRootV154LikeOriginal.activeSelf)
                {
                    ShowWeaponRangeV154LikeOriginal(
                        hovered.Unit, hovered.WeaponType, hovered.Radius);
                }
                return;
            }

            if (_hoverWeaponRangeUnitV159LikeOriginal != null &&
                _weaponRangeHideAtV390LikeOriginal < 0.0f)
                ScheduleHideWeaponRangeV390LikeOriginal(0.06f);
        }

        private static bool TryGetPointerPositionV391LikeOriginal(out Vector2 pointer)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                pointer = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector3 legacy = Input.mousePosition;
            pointer = new Vector2(legacy.x, legacy.y);
            return true;
#else
            pointer = Vector2.zero;
            return false;
#endif
        }

        internal void ScheduleHideWeaponRangeV390LikeOriginal(float delaySeconds = 0.12f)
        {
            if (_hoverWeaponRangeUnitV159LikeOriginal == null) return;
            _weaponRangeHideAtV390LikeOriginal =
                Time.realtimeSinceStartup + Mathf.Max(0.02f, delaySeconds);
        }

        private void RefreshWeaponReloadUiV390LikeOriginal()
        {
            for (int i = 0; i < _weaponReloadUiV390LikeOriginal.Count; i++)
            {
                C2WeaponReloadUiBindingV390LikeOriginal b = _weaponReloadUiV390LikeOriginal[i];
                if (b == null || b.Unit == null || b.ReadyText == null || b.ReloadLine == null)
                    continue;

                int readyCount;
                int totalCount;
                float averageReady01;
                GetWeaponReadyAggregateV390LikeOriginal(
                    b.Unit, b.Key, b.WeaponType,
                    out readyCount, out totalCount, out averageReady01);

                b.ReadyText.text = Mathf.Max(0, readyCount).ToString(CultureInfo.InvariantCulture);
                int lineH = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Max(1, b.MaxHeight) * averageReady01),
                    0,
                    Mathf.Max(1, b.MaxHeight));
                RectTransform rt = b.ReloadLine.rectTransform;
                if (lineH <= 0)
                {
                    b.ReloadLine.enabled = false;
                }
                else
                {
                    b.ReloadLine.enabled = true;
                    Place(
                        rt,
                        b.X,
                        b.Y + (Mathf.Max(1, b.MaxHeight) - lineH),
                        2,
                        lineH);
                }
            }
        }

        private static void GetWeaponReadyAggregateV390LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            string key,
            int weaponType,
            out int readyCount,
            out int totalCount,
            out float averageReady01)
        {
            readyCount = 0; totalCount = 0;
            long delaySum = 0; long maxDelaySum = 0;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
            bool hasBrigadeSoldierSlotsV396 =
                C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                    representative, out members);
            if (!hasBrigadeSoldierSlotsV396)
            {
                members = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
                for (int i = 0; all != null && i < all.Length; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                    if (u == null || !u.isActiveAndEnabled || !u.IsSelected || u.IsDeadLikeOriginal) continue;
                    if (!string.Equals(UnitSelPointKeyV137LikeOriginal(u), key ?? string.Empty, StringComparison.OrdinalIgnoreCase)) continue;
                    members.Add(u);
                }
            }
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = members[i];
                if (u == null || !u.isActiveAndEnabled || u.IsDeadLikeOriginal) continue;
                int delayTicks, maxDelayTicks;
                C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(u, weaponType, out delayTicks, out maxDelayTicks);
                totalCount++; delaySum += Mathf.Max(0, delayTicks); maxDelaySum += Mathf.Max(0, maxDelayTicks);
                if (delayTicks <= 0) readyCount++;
            }
            // A valid brigade with zero readable soldier slots is NOT fully ready.
            // V395 used 100% here, causing the exact 0 shots + full green bar bug.
            if (totalCount <= 0) { averageReady01 = 0.0f; return; }
            int averageDelay = (int)(delaySum / totalCount);
            int averageMaxDelay = (int)(maxDelaySum / totalCount);
            int readyPercent = averageMaxDelay > 0 ? Mathf.Clamp(100 * (averageMaxDelay - averageDelay) / averageMaxDelay, 0, 100) : 100;
            averageReady01 = readyPercent / 100.0f;
        }


        internal void HideWeaponRangeV154LikeOriginal()
        {
            _weaponRangeHideAtV390LikeOriginal = -1.0f;
            _hoverWeaponRangeUnitV159LikeOriginal = null;
            _hoverWeaponRangeKeyV159LikeOriginal = string.Empty;
            _hoverWeaponRangeTypeV159LikeOriginal = -1;
            _hoverWeaponRangeRadiusV159LikeOriginal = 0;
            if (_weaponRangeRootV154LikeOriginal != null)
                _weaponRangeRootV154LikeOriginal.SetActive(false);
            if (_weaponRangeScreenRootV160LikeOriginal != null)
                _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
            ClearWeaponRangeGuiOverlayV161LikeOriginal();
        }

        internal void OnSelectedUnitWeaponClickedV154LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string key,
            int weaponType)
        {
            if (weaponType == 2)
            {
                C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> grenadeUnits;
                int grenadeGroupId;
                string grenadeShape;
                if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                        unit, out grenadeUnits, out grenadeGroupId, out grenadeShape))
                {
                    grenadeUnits = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                    if (unit != null) grenadeUnits.Add(unit);
                }

                C2OriginalProduceCatalogV13.C2MdIconInfoV13 grenadeInfo =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
                int grenadeCurrent;
                int grenadeMaximum;
                C2FormationRuntimeV167LikeOriginal.TryGetGrenadeStateV326LikeOriginal(
                    unit,
                    grenadeInfo.MaxGrenadesInFormation,
                    grenadeInfo.GrenadeRechargeTime,
                    out grenadeCurrent,
                    out grenadeMaximum);
                int allowed = Mathf.Min(grenadeCurrent, grenadeUnits != null ? grenadeUnits.Count : 0);
                int issued = 0;
                C2NeutralPeasantUnitInfoV2LikeOriginal[] enemies =
                    C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
                for (int i = 0; grenadeUnits != null && i < grenadeUnits.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal grenadeUnit = grenadeUnits[i];
                    if (grenadeUnit == null || !grenadeUnit.CanReceivePlayerOrdersLikeOriginal()) continue;
                    if (issued >= allowed) continue;
                    C2NeutralPeasantUnitInfoV2LikeOriginal enemy =
                        FindNearestEnemyForWeaponV337LikeOriginal(
                            grenadeUnit, enemies,
                            Mathf.Max(1, grenadeInfo.AttackRadius2Min),
                            Mathf.Max(1, grenadeInfo.AttackRadius2));
                    if (enemy == null) continue;
                    // Multi.cpp::ComThrowGrenade searches victims immediately
                    // on the button click and starts anm_Attack+2. It does not
                    // enter a second "choose target" cursor mode.
                    if (C2CombatRuntimeV334LikeOriginal.ArmGrenadeLikeOriginal(grenadeUnit))
                    {
                        C2CombatRuntimeV334LikeOriginal combat = grenadeUnit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                        GameObject unitProxy = combat == null ? grenadeUnit.EnsureUnityProxyLikeOriginal() : null;
                        if (combat == null && unitProxy != null) combat = unitProxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
                        if (combat == null) continue;
                        combat.BeginAttackLikeOriginal(grenadeUnit, enemy, null, enemy.transform.position);
                        C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                            grenadeUnit,
                            C2UnitOrderKindV325LikeOriginal.GrenadeAttack,
                            "grenade_button_auto_target",
                            "attack_slot_2");
                        issued++;
                    }
                }
                _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
                _lastSelectedCount = -999999;
                _nextRefresh = 0.0f;
                Debug.Log("[C2:GRENADE UI V325] group=" + grenadeGroupId.ToString(CultureInfo.InvariantCulture) +
                          " units=" + issued.ToString(CultureInfo.InvariantCulture) +
                          " stock=" + grenadeCurrent.ToString(CultureInfo.InvariantCulture) +
                          "/" + grenadeMaximum.ToString(CultureInfo.InvariantCulture) +
                          " mdAttack=#ATTACK2 state=auto_target_from_ComThrowGrenade stock_consumed_on_active_frame");
                return;
            }

            string stateKey = WeaponUiStateKeyV157LikeOriginal(key, weaponType);
            bool nowActive;
            // VUI_Actions.cpp::va_WeapPortBack:
            //   rifle: I->RifleAttack ? UserParam=128(off) : 129(on)
            //   melee: UserParam=1 (command, not toggle)
            bool rifleWasActiveV398 = weaponType == 1 &&
                C2CombatRuntimeV334LikeOriginal.GetFormationRifleAttackStateV398LikeOriginal(unit);
            if (weaponType == 0)
            {
                string statePrefix = (key ?? string.Empty).Trim() + "#";
                _activeWeaponUiStatesV157LikeOriginal.RemoveWhere(
                    candidate => candidate.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase));
                _activeWeaponUiStatesV157LikeOriginal.Add(stateKey);
                nowActive = true;
            }
            else if (weaponType == 1)
            {
                string statePrefix = (key ?? string.Empty).Trim() + "#";
                _activeWeaponUiStatesV157LikeOriginal.RemoveWhere(
                    candidate => candidate.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase));
                nowActive = !rifleWasActiveV398;
                if (nowActive) _activeWeaponUiStatesV157LikeOriginal.Add(stateKey);
            }
            else if (_activeWeaponUiStatesV157LikeOriginal.Contains(stateKey))
            {
                _activeWeaponUiStatesV157LikeOriginal.Remove(stateKey);
                nowActive = false;
            }
            else
            {
                string statePrefix = (key ?? string.Empty).Trim() + "#";
                _activeWeaponUiStatesV157LikeOriginal.RemoveWhere(
                    candidate =>
                        candidate.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(candidate, stateKey, StringComparison.OrdinalIgnoreCase));
                _activeWeaponUiStatesV157LikeOriginal.Add(stateKey);
                nowActive = true;
            }

            _activeWeaponUiKeyV154LikeOriginal = key ?? string.Empty;
            _activeWeaponUiTypeV154LikeOriginal = weaponType;
            _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
            _lastSelectedCount = -999999;
            _nextRefresh = 0.0f;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            int applied = 0;
            if (weaponType == 0 || weaponType == 1)
            {
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> meleeUnits;
                int meleeGroupId;
                string meleeShape;
                bool brigade = C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    unit, out meleeUnits, out meleeGroupId, out meleeShape);

                // Multi.cpp::MoveBrigadeForwardToAttack writes GroundState/NewState=1
                // to every ArmAttack brigade member.  KARE returns immediately after
                // that write; line/column continue through KeepPositions, but they do
                // not skip the posture change.  The earlier KARE-only condition made
                // the grenadier bayonet button a visual no-op in line and column.
                if (!brigade ||
                    !string.IsNullOrEmpty(meleeShape))
                {
                    if (meleeUnits == null)
                    {
                        meleeUnits = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                        if (unit != null) meleeUnits.Add(unit);
                    }
                    List<C2NeutralPeasantUnitInfoV2LikeOriginal> commandTargetsV396 = meleeUnits;
                    List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldierTargetsV396;
                    if (brigade && C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                            unit, out soldierTargetsV396) && soldierTargetsV396 != null)
                        commandTargetsV396 = soldierTargetsV396;
                    // A replacing brigade order owns the combat state in the retail
                    // engine.  Melee replaces RifleAttack immediately; explicit rifle-off
                    // destroys/cancels the active rifle volley as well.
                    if (weaponType == 0)
                        C2CombatRuntimeV334LikeOriginal.ClearFormationRifleAttackStateV399LikeOriginal(
                            unit, "VUI_Actions_melee_replaces_rifle_v399", true);
                    else if (weaponType == 1 && !nowActive)
                        C2CombatRuntimeV334LikeOriginal.ClearFormationRifleAttackStateV399LikeOriginal(
                            unit, "VUI_Actions_rifle_off_128_v399", true);

                    for (int i = 0; i < commandTargetsV396.Count; i++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal meleeUnit = commandTargetsV396[i];
                        if (meleeUnit == null) continue;
                        bool commandAppliedV398;
                        if (weaponType == 1)
                        {
                            // Retail raw command: 129 enables RifleAttack, 128 disables it.
                            commandAppliedV398 = C2CombatRuntimeV334LikeOriginal
                                .SetArmAttackStateValueV396LikeOriginal(
                                    meleeUnit, rifleWasActiveV398 ? 128 : 129);
                        }
                        else
                        {
                            commandAppliedV398 = C2CombatRuntimeV334LikeOriginal
                                .SetArmAttackStateValueV396LikeOriginal(meleeUnit, 1);
                        }
                        if (commandAppliedV398) applied++;
                    }

                    // Multi.cpp::SetArmAttackState does NOT fire here. It only writes
                    // OB->RifleAttack. Brigade::Bitva / BrigadeOrder_Bitva / KeepPositions
                    // later create BrigadeOrder_RifleAttack when a rifleman has delay==0.
                    // V405 restores that separate brigade-order trigger instead of issuing
                    // 120 private shots from the UI click. If the brigade is still reloading,
                    // RifleAttack remains armed and the order starts when delay reaches zero.
                    if (weaponType == 1 && nowActive)
                        C2BrigadeRifleAttackV405LikeOriginal.OnRifleStateEnabledLikeOriginal(unit);

                    // Retail SIMPLEMANAGE does more than flip GroundState here:
                    // SetArmAttackState(...,1) immediately calls MoveBrigadeForwardToAttack.
                    // If an enemy brigade is inside the same 600px detection radius used by
                    // the sabre hover, clicking the bayonet therefore starts the melee charge.
                    if (weaponType == 0 && nowActive)
                        IssueMeleeAttackOnNearestEnemyV393LikeOriginal(
                            unit, meleeUnits, meleeGroupId, meleeShape);
                }
            }

            Debug.Log("[C2:WEAPON UI V405 MODE] key='" + _activeWeaponUiKeyV154LikeOriginal +
                      "' weaponType=" + weaponType.ToString(CultureInfo.InvariantCulture) +
                      " active=" + nowActive.ToString() +
                      " raw=" + (weaponType == 1 ? (rifleWasActiveV398 ? "128" : "129") : (weaponType == 0 ? "1" : "-")) +
                      " originalCommandModeApplied=" + applied.ToString(CultureInfo.InvariantCulture));
        }

        // V405 intentionally has no UI-side rifle target assignment here.
        // See C2BrigadeRifleAttackV405LikeOriginal, ported from
        // COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_RifleAttack.

        private static C2NeutralPeasantUnitInfoV2LikeOriginal FindNearestEnemyForWeaponV337LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal[] candidates,
            int minimumOriginalPixels,
            int maximumOriginalPixels)
        {
            if (attacker == null || candidates == null || maximumOriginalPixels <= 0) return null;
            float ax = attacker.RealXFloat != 0.0f ? attacker.RealXFloat : attacker.RealX;
            float ay = attacker.RealYFloat != 0.0f ? attacker.RealYFloat : attacker.RealY;
            float min2 = minimumOriginalPixels * 16.0f;
            min2 *= min2;
            float max2 = maximumOriginalPixels * 16.0f;
            max2 *= max2;
            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            float best2 = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = candidates[i];
                if (candidate == null || candidate == attacker || candidate.IsDeadLikeOriginal ||
                    candidate.CombatNationLikeOriginal == attacker.CombatNationLikeOriginal) continue;
                float cx = candidate.RealXFloat != 0.0f ? candidate.RealXFloat : candidate.RealX;
                float cy = candidate.RealYFloat != 0.0f ? candidate.RealYFloat : candidate.RealY;
                float dx = cx - ax, dy = cy - ay;
                float d2 = dx * dx + dy * dy;
                if (d2 < min2 || d2 > max2 || d2 >= best2) continue;
                best = candidate;
                best2 = d2;
            }
            return best;
        }

        internal void ClearSelectedUnitWeaponV156LikeOriginal(string key, int weaponType)
        {
            _activeWeaponUiStatesV157LikeOriginal.Remove(WeaponUiStateKeyV157LikeOriginal(key, weaponType));
            if (string.Equals(_activeWeaponUiKeyV154LikeOriginal, key ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                _activeWeaponUiTypeV154LikeOriginal == weaponType)
            {
                _activeWeaponUiTypeV154LikeOriginal = -1;
            }
            _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
            _lastSelectedCount = -999999;
            _nextRefresh = 0.0f;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            Debug.Log("[C2:WEAPON UI V163 RIGHT CLEAR] key='" + (key ?? string.Empty) + "' weaponType=" + weaponType.ToString(CultureInfo.InvariantCulture) + " clear=True ui_only=True");
        }

        private void BuildOriginalProducePanelLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string audit;
            List<C2OriginalProduceItemV13> items = C2OriginalProduceCatalogV13.BuildForSelectedUnit(unit, out audit);
            _lastProduceAudit = audit;

            if (items == null || items.Count == 0)
            {
                AddLabel("produce_empty", "NO ORIGINAL PRODUCE LIST", 182, 635, 260, 18, 11, TextAnchor.MiddleLeft, new Color(1f, 0.75f, 0.25f, 0.9f));
                return;
            }
            for (int i = 0; i < items.Count; i++)
            {
                C2OriginalProduceItemV13 item = items[i];
                int x = OriginalUnitProduceBaseX + item.GridX * OriginalUnitProduceStepX;
                int y = OriginalUnitProduceBaseY + (item.GridY - 1) * OriginalUnitProduceStepY;

                AddOriginalProduceCardV169LikeOriginal(
                    "produce",
                    i,
                    item,
                    x,
                    y,
                    false,
                    null,
                    false);

            }
        }

        private void AddOriginalProduceCardV169LikeOriginal(
            string prefix,
            int slotIndex,
            C2OriginalProduceItemV13 item,
            int x,
            int y,
            bool buildingMenu,
            C2SettlementBuildingSelectableV1LikeOriginal building,
            bool drawBuildingRuntimeOverlays)
        {
            if (item == null)
                return;

            string suffix = slotIndex.ToString(CultureInfo.InvariantCulture);
            const int ProduceCardFrameHeightV169 = OriginalUnitProduceHeight - 2; // V169: lift the lower frame edge 2 px; click area remains original 64x123.

            // One shared mini-card renderer for selected-unit and selected-building produce panels.
            // Frame/background is always the same original UnitProduce/FormInterface card.
            AddG16ImageOverpaintV140LikeOriginal(
                prefix + "_cell_back_" + suffix,
                "Interf3\\FormInterface",
                item.RootSpriteId,
                x,
                y,
                OriginalUnitProduceWidth,
                ProduceCardFrameHeightV169,
                255,
                false,
                56);

            // Soldier mini-icons in building menus need the old V132 native-fit rule.
            // Peasants must NOT use it: their mini sprite otherwise starts at x-1 and visually covers the left frame by ~1-2 px.
            bool useBuildingSoldierFitV169 =
                buildingMenu
                && !item.Building
                && !item.Peasant
                && !string.IsNullOrEmpty(item.IconFileId)
                && item.IconFileId.IndexOf("Units_", StringComparison.OrdinalIgnoreCase) >= 0;

            int iconX = x + OriginalUnitProduceIconX;
            int iconY = y + OriginalUnitProduceIconY;
            int iconW = OriginalUnitProduceIconW;
            int iconH = OriginalUnitProduceIconH;
            bool preserveIconAspect = true;

            if (useBuildingSoldierFitV169)
            {
                iconX = x - 1;
                iconY = y - 1;
                iconW = 60;
                iconH = 118;
                preserveIconAspect = false;
            }

            AddG16ImageOverpaintV140LikeOriginal(
                prefix + "_icon_" + suffix,
                item.IconFileId,
                item.IconSpriteId,
                iconX,
                iconY,
                iconW,
                iconH,
                item.Enabled ? 255 : 128,
                false,
                item.Enabled ? 120 : 48,
                false, // V377: the sprite cache already supplies an upright texture.
                preserveIconAspect);

            if (!item.Enabled)
                AddSolid(prefix + "_disabled_" + suffix, new Color(0f, 0f, 0f, 0.48f), x, y, 57, ProduceCardFrameHeightV169, false);

            if (drawBuildingRuntimeOverlays)
                DrawBuildingProduceRuntimeOverlaysV124LikeOriginal(building, item, slotIndex, x, y);

            AddClickArea(prefix + "_click_" + suffix, x, y, OriginalUnitProduceWidth, OriginalUnitProduceHeight, item);
        }

        private void OnProduceClicked(C2OriginalProduceItemV13 item)
        {
            if (item == null) return;
            if (!item.Enabled)
            {
                return;
            }

            if (item.Building)
            {
                C2GameplayHudV13PlacementRequestedLikeOriginal = true;
                C2GameplayHudV13SelectedBuildUnitIdLikeOriginal = item.UnitId ?? string.Empty;
                C2GameplayHudV13SelectedBuildMdLikeOriginal = item.MdName ?? string.Empty;
                C2GameplayHudV13SelectedBuildNationLikeOriginal = item.Nation;

                C2BuildingPlacementPreviewV27.RequestBuildPreviewLikeOriginal(
                    item.UnitId ?? string.Empty,
                    item.MdName ?? string.Empty,
                    item.Nation,
                    item.BuilderId ?? string.Empty,
                    item.BuilderMd ?? string.Empty,
                    "hud_produce_click");
            }
            else
            {
                // V124: if a building is selected, the same mini-card becomes a training button.
                // Peasant/unit build-button behavior above is untouched.
                if (C2BuildingProductionCardsRuntimeV114.TryHandleBuildingProduceClickLikeOriginal(item))
                {
                    _lastBuildingStateKey = string.Empty;
                    _nextRefresh = 0.0f;

                    // V126: UI clicks on building produce cards must not leak into the map picker.
                    // Otherwise the produced/under-cursor unit may become selected and the building menu closes.
                    C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
                    return;
                }
            }
        }

        internal void OnProduceRightClickedV143LikeOriginal(C2OriginalProduceItemV13 item)
        {
            if (item == null || !item.Enabled || item.Building)
                return;

            if (C2BuildingProductionCardsRuntimeV114.TryHandleBuildingProduceCancelClickLikeOriginal(item))
            {
                // Force HUD state refresh so the green production animation / amount plate disappears immediately.
                _lastBuildingStateKey = string.Empty;
                _nextRefresh = 0.0f;
                C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
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
                Debug.LogWarning("[C2:GAMEPLAY HUD V15] xml render failed rel='" + relPath + "' err=" + ex.GetType().Name + ": " + ex.Message);
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
                    AddG16Image("xml_" + San(fileId) + "_" + spr.ToString(CultureInfo.InvariantCulture), fileId, spr, x, y, w, h, alpha, false);
            }

            for (int i = 0; i < node.Children.Count; i++)
                RenderNode(node.Children[i], x, y, alpha, depth + 1);
        }

        private Image AddG16ImageClippedV149LikeOriginal(
            string name,
            string fileId,
            int spriteId,
            int clipX,
            int clipY,
            int clipW,
            int clipH,
            int imageX,
            int imageY,
            int imageW,
            int imageH,
            int alpha,
            bool raycast,
            bool preserveAspect)
        {
            // SelPoint side pockets in the original are not a free 139x237 portrait drawn on the canvas.
            // Dialogs/v/SelPoint.DialogsDesk.Dialogs.xml puts va_SP_UnitSprSide inside a narrow parent desk:
            //   right: parent x=100 width=31, child portrait x=-81
            //   left : parent x=18  width=35, child portrait x=-26
            // That parent desk clips the big portrait, so only a vertical slice is visible in the side pocket.
            // V148 missed this mask and let the full portrait spill under neighbouring pockets.
            GameObject clipGo = NewUi(name + "_clip_v149");
            RectTransform clipRt = clipGo.GetComponent<RectTransform>();
            Place(clipRt, clipX, clipY, clipW, clipH);
            RectMask2D mask = clipGo.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            mask.softness = Vector2Int.zero;

            Sprite sp = C2GameplayOriginalSpriteCacheV1.LoadSprite(fileId, spriteId, name);
            Image baseImage = AddG16ImageClippedSinglePassV149LikeOriginal(
                name,
                clipRt,
                sp,
                fileId,
                spriteId,
                imageX - clipX,
                imageY - clipY,
                imageW,
                imageH,
                alpha,
                raycast,
                preserveAspect);

            if (alpha > 0)
            {
                Image secondPass = AddG16ImageClippedSinglePassV149LikeOriginal(
                    name + "_v149_doublepass",
                    clipRt,
                    sp,
                    fileId,
                    spriteId,
                    imageX - clipX,
                    imageY - clipY,
                    imageW,
                    imageH,
                    alpha,
                    false,
                    preserveAspect);

                C2HudImageDoublePassSyncV142LikeOriginal sync = baseImage.gameObject.AddComponent<C2HudImageDoublePassSyncV142LikeOriginal>();
                sync.Configure(secondPass);
            }

            return baseImage;
        }

        private Image AddG16ImageClippedSinglePassV149LikeOriginal(
            string name,
            RectTransform parent,
            Sprite sp,
            string fileId,
            int spriteId,
            int localX,
            int localY,
            int w,
            int h,
            int alpha,
            bool raycast,
            bool preserveAspect)
        {
            GameObject go = new GameObject("C2_HUD_" + name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = raycast;

            Color c = Color.white;
            c.a = Mathf.Clamp01(alpha / 255.0f);
            img.color = c;

            Place(rt, localX, localY, w, h);
            AppendSpriteAuditLikeOriginal(name, fileId, spriteId, localX, localY, w, h, alpha, sp, false, preserveAspect);
            return img;
        }

        private Image AddG16Image(string name, string fileId, int spriteId, int x, int y, int w, int h, int alpha, bool raycast, bool uiFlipY = false, bool preserveAspect = true)
        {
            // V140A: ALL HUD G16/GP pictures are rendered with a strict second identical pass.
            // No per-element "light" overpaint: frame, portrait, plate, icon and XML picture use the same rule.
            Sprite sp = C2GameplayOriginalSpriteCacheV1.LoadSprite(fileId, spriteId, name);
            Image baseImage = AddG16ImageSinglePassV140ALikeOriginal(name, sp, fileId, spriteId, x, y, w, h, alpha, raycast, uiFlipY, preserveAspect);
            if (alpha > 0)
            {
                Image secondPass = AddG16ImageSinglePassV140ALikeOriginal(name + "_v140a_doublepass", sp, fileId, spriteId, x, y, w, h, alpha, false, uiFlipY, preserveAspect);
                C2HudImageDoublePassSyncV142LikeOriginal sync = baseImage.gameObject.AddComponent<C2HudImageDoublePassSyncV142LikeOriginal>();
                sync.Configure(secondPass);
            }
            return baseImage;
        }

        private sealed class C2HudImageDoublePassSyncV142LikeOriginal : MonoBehaviour
        {
            private Image _second;
            private RectTransform _selfRt;
            private RectTransform _secondRt;

            public void Configure(Image second)
            {
                _second = second;
                _selfRt = GetComponent<RectTransform>();
                _secondRt = second != null ? second.GetComponent<RectTransform>() : null;
                LateUpdate();
            }

            private void LateUpdate()
            {
                Image first = GetComponent<Image>();
                if (first == null || _second == null)
                {
                    Destroy(this);
                    return;
                }

                _second.enabled = first.enabled;
                _second.sprite = first.sprite;
                _second.type = first.type;
                _second.fillMethod = first.fillMethod;
                _second.fillOrigin = first.fillOrigin;
                _second.fillAmount = first.fillAmount;
                _second.fillClockwise = first.fillClockwise;
                _second.preserveAspect = first.preserveAspect;
                _second.color = first.color;

                if (_selfRt == null) _selfRt = GetComponent<RectTransform>();
                if (_secondRt == null) _secondRt = _second.GetComponent<RectTransform>();
                if (_selfRt != null && _secondRt != null)
                {
                    _secondRt.anchorMin = _selfRt.anchorMin;
                    _secondRt.anchorMax = _selfRt.anchorMax;
                    _secondRt.pivot = _selfRt.pivot;
                    _secondRt.anchoredPosition = _selfRt.anchoredPosition;
                    _secondRt.sizeDelta = _selfRt.sizeDelta;
                    _secondRt.localScale = _selfRt.localScale;
                }
            }
        }

        private Image AddG16ImageSinglePassV140ALikeOriginal(string name, Sprite sp, string fileId, int spriteId, int x, int y, int w, int h, int alpha, bool raycast, bool uiFlipY, bool preserveAspect)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = raycast;
            Color c = Color.white;
            c.a = Mathf.Clamp01(alpha / 255.0f);
            img.color = c;

            RectTransform rt = go.GetComponent<RectTransform>();
            Place(rt, x, y, w, h);

            if (uiFlipY)
            {
                // Direct UI-level vertical flip for produce portraits.
                // Pivot bottom-left + negative Y scale keeps the same top-left rect and avoids the old upward shift.
                rt.pivot = new Vector2(0, 0);
                rt.anchoredPosition = new Vector2(x, -y);
                rt.localScale = new Vector3(1f, -1f, 1f);
            }

            AppendSpriteAuditLikeOriginal(name, fileId, spriteId, x, y, w, h, alpha, sp, uiFlipY, preserveAspect);
            return img;
        }

        private Image AddG16ImageOverpaintV140LikeOriginal(
            string name,
            string fileId,
            int spriteId,
            int x,
            int y,
            int w,
            int h,
            int alpha,
            bool raycast,
            int overpaintAlpha,
            bool uiFlipY = false,
            bool preserveAspect = true)
        {
            // V140A: kept for compatibility with V140 call sites.
            // The real rule is now inside AddG16Image: every image receives one exact second pass.
            return AddG16Image(name, fileId, spriteId, x, y, w, h, alpha, raycast, uiFlipY, preserveAspect);
        }

        private Text AddCrispLabelV140LikeOriginal(string name, string text, int x, int y, int w, int h, int fontSize, TextAnchor anchor, Color color)
        {
            // V141: text is not image overpaint. Double white text created a glowing title.
            // Keep labels crisp with one glyph pass plus a dark original-like shadow.
            Text t = AddLabelSinglePassV140ALikeOriginal(name, text, x, y, w, h, fontSize, anchor, color);

            Shadow sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.05f, 0.035f, 0.02f, Mathf.Clamp01(color.a * 0.72f));
            sh.effectDistance = new Vector2(0.75f, -0.75f);
            sh.useGraphicAlpha = true;

            return t;
        }

        private Image AddG16ImageTopSliceV117LikeOriginal(string name, string fileId, int spriteId, int x, int y, int w, int h, int alpha, bool raycast)
        {
            Sprite source = C2GameplayOriginalSpriteCacheV1.LoadSprite(fileId, spriteId, name + "_source");
            Sprite topSlice = source;

            if (source != null && source.texture != null)
            {
                string sliceKey = (fileId ?? string.Empty).Trim() + "|" + spriteId.ToString(CultureInfo.InvariantCulture) + "|top|" + h.ToString(CultureInfo.InvariantCulture);
                if (!s_topSliceSpriteCacheV133LikeOriginal.TryGetValue(sliceKey, out topSlice) || topSlice == null)
                {
                    Rect tr = source.textureRect;
                    float sliceH = Mathf.Clamp(h, 1.0f, tr.height);
                    Rect sliceRect = new Rect(tr.x, tr.y + tr.height - sliceH, tr.width, sliceH);
                    topSlice = Sprite.Create(source.texture, sliceRect, new Vector2(0.0f, 1.0f), source.pixelsPerUnit);
                    s_topSliceSpriteCacheV133LikeOriginal[sliceKey] = topSlice;
                }
            }

            Image img = AddG16ImageTopSliceSinglePassV140ALikeOriginal(name, topSlice, fileId, spriteId, x, y, w, h, alpha, raycast);
            if (alpha > 0)
                AddG16ImageTopSliceSinglePassV140ALikeOriginal(name + "_v140a_doublepass", topSlice, fileId, spriteId, x, y, w, h, alpha, false);
            return img;
        }

        private Image AddG16ImageTopSliceSinglePassV140ALikeOriginal(string name, Sprite topSlice, string fileId, int spriteId, int x, int y, int w, int h, int alpha, bool raycast)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.sprite = topSlice;
            img.preserveAspect = false;
            img.raycastTarget = raycast;

            Color c = Color.white;
            c.a = Mathf.Clamp01(alpha / 255.0f);
            img.color = c;

            RectTransform rt = go.GetComponent<RectTransform>();
            Place(rt, x, y, w, h);

            AppendSpriteAuditLikeOriginal(name, fileId, spriteId, x, y, w, h, alpha, topSlice, false, false);
            return img;
        }

        private void AppendSpriteAuditLikeOriginal(string name, string fileId, int spriteId, int x, int y, int w, int h, int alpha, Sprite sp, bool uiFlipY, bool preserveAspect)
        {
            // V133: removed verbose per-sprite audit collection.
        }


        private void DumpSpriteAuditLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int selectedCount)
        {
            // V133: removed verbose per-card sprite audit logging.
        }


        private void AddOriginalMoraleLineLikeOriginal(int x, int y, int w, int h, int morale, int moraleMax)
        {
            // Exact COSSACKS2/VUI_Actions.cpp::SetMorale colour contract.
            // Important V404A fix: retail Canvas has NO opaque custom background here.
            // V404 added one, which visually flattened the 0x8F "max morale" yellow
            // against the current 0xFF yellow and made the two-part bar hard to read.
            if (w <= 0 || h <= 0 || moraleMax <= 0) return;

            int n = Mathf.Max(0, morale / 100);
            int m = Mathf.Clamp(morale % 100, 0, 100);
            int M = Mathf.Clamp(moraleMax - n * 100, 0, 100);
            if (n + M <= 0 || n >= 10) return;

            int lx = Mathf.Clamp(m * w / 100, 0, w);
            int lMax = Mathf.Clamp(M * w / 100, 0, w);
            int lr = 0;
            if (n == 0)
                lr = Mathf.Clamp(Mathf.Min(m, 32) * w / 100, 0, w);

            // SetMorale geometry is kept 1:1.  V404A also kept the retail
            // semi-transparent yellow for the unused part, but on Unity's HUD it
            // visually merged with the current segment.  The user explicitly wants
            // the original shape/logic with stronger readable colours.
            Color red = new Color32(0xFF, 0x25, 0x18, 0xFF);
            Color yellow = new Color32(0xFF, 0xD4, 0x12, 0xFF);
            Color yellowMax = new Color32(0x62, 0x3C, 0x08, 0xFF);
            Color ticks = new Color32(0xAF, 0x00, 0x00, 0xFF);

            if (lr > 0)
                AddSolid("sp_morale_line_red_original_v404c", red, x, y, lr, h, false);
            if (lx > lr)
                AddSolid("sp_morale_line_yellow_original_v404c", yellow, x + lr, y, lx - lr, h, false);
            if (lMax > lx)
                AddSolid("sp_morale_line_yellow_max_original_v404c", yellowMax, x + lx, y, lMax - lx, h, false);

            if (n > 0)
            {
                int tickW = Mathf.Max(1, h - 1);
                int start = (w - (n + n - 1) * tickW) / 2;
                for (int i = 0; i < n; i++)
                {
                    int xx = x + start + i * 2 * tickW;
                    AddSolid("sp_morale_line_tick_v404c_" + i.ToString(CultureInfo.InvariantCulture),
                        ticks, xx, y, tickW, h, false);
                }
            }
        }

        private void AddOriginalLifeLineLikeOriginal(
            int x, int y, int w, int h, int life, int maxLife)
        {
            // COSSACKS2/VUI_Actions.cpp::va_SP_LifeLine::SetFrameState.
            if (maxLife <= 0 || life <= 0) return;
            int fill = Mathf.Clamp(h * life / maxLife, 0, h);
            if (fill <= 0) return;
            AddSolidSinglePassV140ALikeOriginal(
                "sp_life_line_v404_original", new Color32(0x00, 0xFF, 0x00, 0xFF),
                x, y + (h - fill), Mathf.Max(1, w), fill, false);
        }

        private void AddOriginalTiredLineLikeOriginal(
            int x, int y, int w, int h, float tiringRemainingPercent,
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceUnitV398LikeOriginal = null)
        {
            // COSSACKS2/VUI_Actions.cpp::va_SP_TiredLine::SetFrameState:
            // SD->Visible=false; Ly=Height*I->Tiring/100; if(Ly>0) AddBar(...).
            // There is NO full-height background bar.  V397's reload-line fix
            // exposed the same old AddSolid/double-pass trap here, so the tired
            // line is one independent single-pass Image only.
            int fill = Mathf.Clamp(
                Mathf.FloorToInt(h * Mathf.Clamp01(tiringRemainingPercent / 100.0f)),
                0, h);
            Image fillImage = AddSolidSinglePassV140ALikeOriginal(
                "sp_tired_line_original",
                TiredLineColorV398LikeOriginal(tiringRemainingPercent),
                x, y + (h - Mathf.Max(1, fill)), w, Mathf.Max(1, fill), false);
            fillImage.enabled = fill > 0;

            C2NeutralPeasantUnitInfoV2LikeOriginal selected =
                sourceUnitV398LikeOriginal != null ? sourceUnitV398LikeOriginal : FirstSelectedUnit();
            if (selected != null)
            {
                _tiredUiV398LikeOriginal.Add(new C2TiredUiBindingV398LikeOriginal
                {
                    Unit = selected,
                    Fill = fillImage,
                    X = x,
                    Y = y,
                    Width = w,
                    MaxHeight = h
                });
            }
        }

        private static float _tiredPulseStartV398LikeOriginal = -1.0f;

        private static Color TiredLineColorV398LikeOriginal(float tiringRemainingPercent)
        {
            // VUI_Actions.cpp starts from DWORD 0xFFEF1212. Above/equal 45%
            // it is used unchanged. Below 45% the retail code calls MulDWORD
            // with factor 255 + int(sin((tick-t)/200)*60-20).
            if (tiringRemainingPercent >= 45.0f)
                return new Color32(0xEF, 0x12, 0x12, 0xFF);

            if (_tiredPulseStartV398LikeOriginal < 0.0f)
                _tiredPulseStartV398LikeOriginal = Time.realtimeSinceStartup;
            float elapsedMs = (Time.realtimeSinceStartup - _tiredPulseStartV398LikeOriginal) * 1000.0f;
            float a = Mathf.Sin(elapsedMs / 200.0f) * 60.0f - 20.0f;
            int factor = 255 + (int)a; // C++ int(a): truncate toward zero.
            byte r = (byte)Mathf.Clamp((0xEF * factor) >> 8, 0, 255);
            byte g = (byte)Mathf.Clamp((0x12 * factor) >> 8, 0, 255);
            byte b = (byte)Mathf.Clamp((0x12 * factor) >> 8, 0, 255);
            byte alpha = (byte)Mathf.Clamp((0xFF * factor) >> 8, 0, 255);
            return new Color32(r, g, b, alpha);
        }

        private void RefreshTiredUiV398LikeOriginal()
        {
            for (int i = 0; i < _tiredUiV398LikeOriginal.Count; i++)
            {
                C2TiredUiBindingV398LikeOriginal b = _tiredUiV398LikeOriginal[i];
                if (b == null || b.Unit == null || b.Fill == null) continue;
                float tiring = C2CombatRuntimeV334LikeOriginal
                    .GetFormationTiringRemainingLikeOriginal(b.Unit);
                int h = Mathf.Max(1, b.MaxHeight);
                int fill = Mathf.Clamp(
                    Mathf.FloorToInt(h * Mathf.Clamp01(tiring / 100.0f)),
                    0, h);
                if (fill <= 0)
                {
                    b.Fill.enabled = false;
                    continue;
                }
                b.Fill.enabled = true;
                b.Fill.color = TiredLineColorV398LikeOriginal(tiring);
                Place(b.Fill.rectTransform, b.X, b.Y + (h - fill), Mathf.Max(1, b.Width), fill);
            }
        }

        private Image AddSolid(string name, Color color, int x, int y, int w, int h, bool raycast)
        {
            Image img = AddSolidSinglePassV140ALikeOriginal(name, color, x, y, w, h, raycast);
            if (color.a > 0.0f)
                AddSolidSinglePassV140ALikeOriginal(name + "_v140a_doublepass", color, x, y, w, h, false);
            return img;
        }

        private Image AddSolidSinglePassV140ALikeOriginal(string name, Color color, int x, int y, int w, int h, bool raycast)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
            return img;
        }

        private void AddClickArea(string name, int x, int y, int w, int h, C2OriginalProduceItemV13 item)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            Button btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            C2OriginalProduceItemV13 captured = item;
            btn.onClick.AddListener(delegate { OnProduceClicked(captured); });

            C2HudProduceCancelRelayV143LikeOriginal cancelRelay = go.AddComponent<C2HudProduceCancelRelayV143LikeOriginal>();
            cancelRelay.Owner = this;
            cancelRelay.Item = captured;

            C2HudTooltipRelayV13I relay = go.AddComponent<C2HudTooltipRelayV13I>();
            relay.Owner = this;
            relay.Item = captured;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
        }

        private static bool IsShiftPressedV152LikeOriginal()
        {
            // Works with both Player Settings modes:
            // - Input System package only
            // - legacy Input Manager / Both
            // Uses reflection so the file still compiles if the package reference changes.
            try
            {
                Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (keyboardType != null)
                {
                    object keyboard = keyboardType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null, null);
                    if (keyboard != null)
                    {
                        if (IsInputSystemKeyPressedV152LikeOriginal(keyboardType, keyboard, "leftShiftKey")) return true;
                        if (IsInputSystemKeyPressedV152LikeOriginal(keyboardType, keyboard, "rightShiftKey")) return true;
                    }
                }
            }
            catch
            {
                // Fall back to legacy Input below.
            }

            try
            {
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsInputSystemKeyPressedV152LikeOriginal(Type keyboardType, object keyboard, string propertyName)
        {
            try
            {
                object key = keyboardType.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.GetValue(keyboard, null);
                if (key == null) return false;

                Type keyType = key.GetType();
                System.Reflection.PropertyInfo pressedProp = keyType.GetProperty("isPressed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (pressedProp != null)
                {
                    object value = pressedProp.GetValue(key, null);
                    if (value is bool) return (bool)value;
                }

                System.Reflection.MethodInfo isPressedMethod = keyType.GetMethod("IsPressed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (isPressedMethod != null)
                {
                    object value = isPressedMethod.Invoke(key, null);
                    if (value is bool) return (bool)value;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private void AddUnitSelPointSideClickAreaV137LikeOriginal(string name, int x, int y, int w, int h, string key)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            Button btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            string captured = key ?? string.Empty;
            btn.onClick.AddListener(delegate { OnUnitSelPointSideClickedV137LikeOriginal(captured); });
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
        }

        private void AddUnitSelPointCenterClickAreaV148LikeOriginal(string name, int x, int y, int w, int h, string key)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            Button btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            string captured = key ?? string.Empty;
            btn.onClick.AddListener(delegate { OnUnitSelPointCenterClickedV148LikeOriginal(captured); });
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
        }

        private void OnUnitSelPointCenterClickedV148LikeOriginal(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            int beforeSelectedV151 = 0;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] allBeforeV151 = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int bi = 0; allBeforeV151 != null && bi < allBeforeV151.Length; bi++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal bu = allBeforeV151[bi];
                if (bu != null && bu.isActiveAndEnabled && bu.IsSelected) beforeSelectedV151++;
            }

            // Original va_SP_PortretBox2/3 -> SelSP(SP):
            // plain LMB selects only this SelPoint type; SHIFT+LMB removes this type from current selection.
            // V152: do not call UnityEngine.Input.GetKey directly. In Input System-only projects it throws
            // InvalidOperationException and the center click never reaches the selection loop.
            bool shift = IsShiftPressedV152LikeOriginal();
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled) continue;
                bool same = string.Equals(UnitSelPointKeyV137LikeOriginal(u), key, StringComparison.OrdinalIgnoreCase);
                if (shift)
                {
                    if (same) u.SetSelected(false);
                }
                else
                {
                    u.SetSelected(same);
                }
            }

            if (!shift)
            {
                C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
                for (int i = 0; buildings != null && i < buildings.Length; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                    if (b != null && b.isActiveAndEnabled) b.SetSelected(false);
                }
            }

            int afterSelectedV151 = 0;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] allAfterV151 = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int ai = 0; allAfterV151 != null && ai < allAfterV151.Length; ai++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal au = allAfterV151[ai];
                if (au != null && au.isActiveAndEnabled && au.IsSelected) afterSelectedV151++;
            }
            Debug.Log("[C2:SELPOINT V152 CLICK] center key='" + key + "' shift=" + shift.ToString() + " selectedBefore=" + beforeSelectedV151.ToString(CultureInfo.InvariantCulture) + " selectedAfter=" + afterSelectedV151.ToString(CultureInfo.InvariantCulture));

            _activeUnitSelPointKeyV137LikeOriginal = key;
            _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
            _lastSelectedCount = -999999;
            _nextRefresh = 0.0f;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
        }

        internal void ShowTooltip(C2OriginalProduceItemV13 item, Vector2 screenPos)
        {
            EnsureTooltipLayer();
            if (_tooltipRoot == null || _tooltipText == null || item == null) return;

            string title = !string.IsNullOrEmpty(item.DisplayText) ? item.DisplayText : (!string.IsNullOrEmpty(item.DisplayNameKey) ? item.DisplayNameKey : item.MdName);
            if (string.IsNullOrEmpty(title)) title = item.UnitId ?? string.Empty;
            string text = title;
            if (!string.IsNullOrEmpty(item.HotKey)) text += "  [" + item.HotKey + "]";
            if (!string.IsNullOrEmpty(item.MdName)) text += "\n" + item.MdName;
            _tooltipText.text = text;
            _tooltipRoot.gameObject.SetActive(true);
            MoveTooltip(screenPos);
            _tooltipRoot.SetAsLastSibling();
        }

        internal void MoveTooltip(Vector2 screenPos)
        {
            if (_tooltipRoot == null || !_tooltipRoot.gameObject.activeSelf) return;
            Vector2 refSize = _root != null && _root.rect.size.sqrMagnitude > 1 ? _root.rect.size : new Vector2(1024, 768);
            float sx = Screen.width > 1 ? refSize.x / Screen.width : 1f;
            float sy = Screen.height > 1 ? refSize.y / Screen.height : 1f;
            float x = screenPos.x * sx + 16f;
            float y = (Screen.height - screenPos.y) * sy + 18f;
            if (x > refSize.x - 210f) x = refSize.x - 210f;
            if (y > refSize.y - 70f) y = refSize.y - 70f;
            _tooltipRoot.anchoredPosition = new Vector2(Mathf.Max(0f, x), -Mathf.Max(0f, y));
        }

        internal void HideTooltip()
        {
            if (_tooltipRoot != null) _tooltipRoot.gameObject.SetActive(false);
        }

        private void EnsureTooltipLayer()
        {
            if (_root == null) return;
            if (_tooltipRoot != null && _tooltipText != null)
            {
                _tooltipRoot.SetAsLastSibling();
                return;
            }

            GameObject go = new GameObject("C2_HUD_Tooltip_V13D");
            go.transform.SetParent(_root, false);
            _tooltipRoot = go.AddComponent<RectTransform>();
            _tooltipRoot.anchorMin = new Vector2(0, 1);
            _tooltipRoot.anchorMax = new Vector2(0, 1);
            _tooltipRoot.pivot = new Vector2(0, 1);
            _tooltipRoot.sizeDelta = new Vector2(205, 54);

            Image back = go.AddComponent<Image>();
            back.color = new Color(0.03f, 0.025f, 0.015f, 0.88f);
            back.raycastTarget = false;

            GameObject textGo = new GameObject("C2_HUD_Tooltip_Text_V13D");
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = new Vector2(7, 5);
            tr.offsetMax = new Vector2(-7, -5);
            _tooltipText = textGo.AddComponent<Text>();
            _tooltipText.font = RuntimeFont();
            _tooltipText.fontSize = 11;
            _tooltipText.alignment = TextAnchor.MiddleLeft;
            _tooltipText.color = new Color(1f, 0.92f, 0.65f, 1f);
            _tooltipText.raycastTarget = false;
            go.SetActive(false);
        }

        private static Color OriginalHudTitleTextColorV141LikeOriginal()
        {
            // Original title text is not pure glowing white; it is a warm ivory/gold over a dark shadow.
            return new Color(0.88f, 0.82f, 0.62f, 1.0f);
        }

        private static Font RuntimeFont()
        {
            if (_cachedRuntimeFont != null)
                return _cachedRuntimeFont;

            _cachedRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedRuntimeFont == null)
                _cachedRuntimeFont = Font.CreateDynamicFontFromOSFont("Arial", 12);
            if (_cachedRuntimeFont == null)
                _cachedRuntimeFont = Font.CreateDynamicFontFromOSFont("Liberation Sans", 12);
            return _cachedRuntimeFont;
        }

        private Text AddLabel(string name, string text, int x, int y, int w, int h, int fontSize, TextAnchor anchor, Color color)
        {
            // V141: text is drawn once. Images remain double-pass; text double-pass caused white glow.
            return AddLabelSinglePassV140ALikeOriginal(name, text, x, y, w, h, fontSize, anchor, color);
        }

        private Text AddLabelSinglePassV140ALikeOriginal(string name, string text, int x, int y, int w, int h, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject go = NewUi(name);
            Text t = go.AddComponent<Text>();
            t.text = text ?? string.Empty;
            t.font = RuntimeFont();
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
            return t;
        }


        private Text AddSelPointSideNameInBalloonV153LikeOriginal(string name, string text, int plateX, int plateY, int plateW, int plateH, int fontSize, bool clockwise, Color color)
        {
            // V153: side unit names belong to the black vertical "name circle" plate
            // (Interf3\cropped sprite 9/10), not to a hand-shifted text rect.
            // Original XML:
            //   LEFT  NameCircle global x=slot+6       y=479+68 w=21 h=153
            //   RIGHT NameCircle global x=rightFrame+20 y=479+68 w=21 h=153
            //
            // Unity Text is horizontal before rotation. To make the rotated text sit inside
            // the black vertical plate, use a horizontal rect of plateH x plateW, pivot it at
            // its center, place that center at the plate center, and rotate around the center.
            int textW = Mathf.Max(1, plateH);
            int textH = Mathf.Max(1, plateW);
            int textX = Mathf.RoundToInt(plateX + plateW * 0.5f - textW * 0.5f);
            int textY = Mathf.RoundToInt(plateY + plateH * 0.5f - textH * 0.5f);

            Text t = AddLabelSinglePassV140ALikeOriginal(name, text ?? string.Empty, textX, textY, textW, textH, fontSize, TextAnchor.MiddleCenter, color);
            Shadow sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.05f, 0.035f, 0.02f, Mathf.Clamp01(color.a * 0.72f));
            sh.effectDistance = new Vector2(0.75f, -0.75f);
            sh.useGraphicAlpha = true;

            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(plateX + plateW * 0.5f, -(plateY + plateH * 0.5f));
                rt.localEulerAngles = new Vector3(0.0f, 0.0f, clockwise ? -90.0f : 90.0f);
            }

            t.raycastTarget = false;
            return t;
        }

        private Text AddRotatedLabelSideNameV152LikeOriginal(string name, string text, int x, int y, int w, int h, int fontSize, bool clockwise, Color color)
        {
            // V152: Unity rotates the whole Text rect around its top-left pivot, while the original
            // Vertical=true text is visually centered inside Interf3\cropped sprite 8.
            // We pass the corrected top-left already shifted to the vertical plaque center.
            return AddRotatedLabel(name, text ?? string.Empty, x, y, w, h, fontSize, clockwise, color);
        }

        private Text AddRotatedLabelXmlVerticalV150LikeOriginal(string name, string text, int x, int y, int w, int h, int fontSize, bool clockwise, Color color)
        {
            // Original TextButton Vertical=true keeps the XML text rectangle as the logical center
            // of rotation. The old Unity helper rotated around top-left pivot, which moved side
            // names and amount digits away from their cropped pocket plates.
            Text t = AddLabelSinglePassV140ALikeOriginal(name, text ?? string.Empty, x, y, w, h, fontSize, TextAnchor.MiddleCenter, color);
            Shadow sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.05f, 0.035f, 0.02f, Mathf.Clamp01(color.a * 0.72f));
            sh.effectDistance = new Vector2(0.75f, -0.75f);
            sh.useGraphicAlpha = true;

            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
                rt.localEulerAngles = new Vector3(0.0f, 0.0f, clockwise ? -90.0f : 90.0f);
            }
            t.raycastTarget = false;
            return t;
        }

        private Text AddRotatedLabel(string name, string text, int x, int y, int w, int h, int fontSize, bool clockwise)
        {
            return AddRotatedLabel(name, text, x, y, w, h, fontSize, clockwise, Color.white);
        }

        private Text AddRotatedLabel(string name, string text, int x, int y, int w, int h, int fontSize, bool clockwise, Color color)
        {
            Text t = AddCrispLabelV140LikeOriginal(name, text ?? string.Empty, x, y, w, h, fontSize, TextAnchor.MiddleCenter, color);
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.localEulerAngles = new Vector3(0.0f, 0.0f, clockwise ? -90.0f : 90.0f);
            t.raycastTarget = false;
            return t;
        }


        private Image FindHudImageV143ALikeOriginal(string rawName)
        {
            if (_root == null || string.IsNullOrEmpty(rawName))
                return null;

            Transform t = _root.Find("C2_HUD_" + rawName);
            return t != null ? t.GetComponent<Image>() : null;
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

            if (_root != null)
                _root.gameObject.SetActive(visible);

            if (_canvas != null)
            {
                Camera cam = FindBattleCamera();
                ConfigureScreenOverlay(cam);
                _canvas.enabled = cam != null;
            }

            if (!visible)
            {
                HideWeaponRangeV154LikeOriginal();
                GraphicRaycaster raycaster = _canvas != null ? _canvas.GetComponent<GraphicRaycaster>() : null;
                if (raycaster != null) raycaster.enabled = FindBattleCamera() != null;
            }
        }

        private void ClearSpawned()
        {
            // V391: do not hide the persistent world-range root during a structural
            // HUD rebuild. New card rects are registered synchronously and the
            // LateUpdate pointer poll decides whether the cursor is still over them.
            _weaponReloadUiV390LikeOriginal.Clear();
            _tiredUiV398LikeOriginal.Clear();
            _weaponHoverBindingsV391LikeOriginal.Clear();
            for (int i = 0; i < _spawned.Count; i++)
            {
                GameObject go = _spawned[i];
                if (go == null) continue;

                // V119: Destroy() removes UI objects at the end of the frame.
                // When selection switches unit -> building in the same HUD tick, the old unit-only
                // morale plate/line can remain visible for one rendered frame under the building card.
                // Hide first, then destroy. Do not let old unit layers visually bleed into building HUD.
                go.SetActive(false);
                Destroy(go);
            }
            _spawned.Clear();
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal FirstSelectedUnit()
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
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
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u != null && u.isActiveAndEnabled && u.IsSelected) count++;
            }
            return count;
        }


        private static C2SettlementBuildingSelectableV1LikeOriginal FirstSelectedBuildingLikeOriginal()
        {
            C2SettlementBuildingSelectableV1LikeOriginal[] all = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            C2SettlementBuildingSelectableV1LikeOriginal best = null;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null || !b.isActiveAndEnabled || !b.IsSelected || b.NotSelectable) continue;
                if (best == null || b.SortKey < best.SortKey) best = b;
            }
            return best;
        }

        private static int CountSelectedBuildingsLikeOriginal()
        {
            int count = 0;
            C2SettlementBuildingSelectableV1LikeOriginal[] all = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b != null && b.isActiveAndEnabled && b.IsSelected && !b.NotSelectable) count++;
            }
            return count;
        }

        private static Camera FindBattleCamera()
        {
            List<Camera> cams = FindBattleCameras();
            if (cams == null || cams.Count == 0) return null;

            Camera best = null;
            int bestScore = -1000000;

            for (int i = 0; i < cams.Count; i++)
            {
                Camera c = cams[i];
                if (c == null) continue;

                string n = c.name ?? string.Empty;
                int score = 0;

                // V13N: choose the camera that is most likely to be the final rendered GameView camera.
                // Earlier versions hard-preferred Iso, so the HUD could be stacked on Iso while Free/debug
                // camera rendered after it and hid the menu. Depth/pixelRect are more important here.
                score += Mathf.RoundToInt(c.depth * 100.0f);
                score += Mathf.RoundToInt(c.pixelRect.width * c.pixelRect.height / 100000.0f);

                if (n.IndexOf("C2_BattleTerrainCamera_Free", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 60;
                if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 50;
                else if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 40;
                else if (n.IndexOf("C2_Battle", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 30;
                else if (n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 20;

                if (best == null || score > bestScore)
                {
                    best = c;
                    bestScore = score;
                }
            }

            return best;
        }

        private static List<Camera> FindBattleCameras()
        {
            Camera[] cams = Camera.allCameras;
            List<Camera> result = new List<Camera>(4);

            for (int i = 0; cams != null && i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (c == null || !c.isActiveAndEnabled || !c.gameObject.activeInHierarchy) continue;
                if (c.targetTexture != null) continue;

                string n = c.name ?? string.Empty;

                if (n.IndexOf("GameplayHud_OverlayCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    n.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool isBattle =
                    n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("C2_BattleTerrainCamera_Free", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("C2_Battle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isBattle) continue;

                result.Add(c);
            }

            return result;
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

    internal sealed class C2HudProduceCancelRelayV143LikeOriginal : MonoBehaviour, IPointerClickHandler
    {
        public C2GameplayHudV1 Owner;
        public C2OriginalProduceItemV13 Item;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Right)
                return;

            // RMB on produce card is a HUD command in the original:
            // cancel queued/training unit, do not pass the same RMB to terrain rally-point logic.
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            eventData.Use();

            if (Owner != null)
                Owner.OnProduceRightClickedV143LikeOriginal(Item);
        }
    }

    internal sealed class C2HudWeaponRelayV154LikeOriginal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
    {
        public C2GameplayHudV1 Owner;
        public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
        public string Key;
        public int WeaponType;
        public int Radius;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Owner != null) Owner.ShowWeaponRangeV154LikeOriginal(Unit, WeaponType, Radius);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Owner != null) Owner.ScheduleHideWeaponRangeV390LikeOriginal(0.12f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Owner == null || eventData == null ||
                eventData.button != PointerEventData.InputButton.Left)
                return;

            // V394: execute weapon cards on press, not on PointerClick.  The HUD can
            // rebuild its weapon card while the mouse is held (range hover/state refresh),
            // which destroys the pressed GameObject before PointerClick is generated.
            // That is exactly why V393 showed MELEE HOVER but no WEAPON UI / MELEE BUTTON log.
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            Owner.OnSelectedUnitWeaponClickedV154LikeOriginal(Unit, Key, WeaponType);
            eventData.Use();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Owner == null || eventData == null) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Owner.ClearSelectedUnitWeaponV156LikeOriginal(Key, WeaponType);
                eventData.Use();
            }
            // Left click is intentionally handled in OnPointerDown above.
        }
    }

    internal sealed class C2HudTooltipRelayV13I : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public C2GameplayHudV1 Owner;
        public C2OriginalProduceItemV13 Item;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Owner != null) Owner.ShowTooltip(Item, eventData != null ? eventData.position : Vector2.zero);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Owner != null) Owner.HideTooltip();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (Owner != null && eventData != null) Owner.MoveTooltip(eventData.position);
        }
    }

    internal static class C2OriginalAiDatFlagsV14
    {
        private static bool _loaded;
        private static string _audit = "not_loaded";
        private static readonly Dictionary<string, int> _suffixToFlag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _memberToFlag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static void ForceReloadLikeOriginal()
        {
            _loaded = false;
            _audit = "not_loaded";
            _suffixToFlag.Clear();
            _memberToFlag.Clear();
        }

        public static string Audit
        {
            get
            {
                EnsureLoaded();
                return _audit;
            }
        }

        public static bool TryGetFlagBySuffix(string suffix, out int flag)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(suffix)) { flag = 0; return false; }
            return _suffixToFlag.TryGetValue(suffix.Trim(), out flag);
        }

        public static bool TryGetFlagByMember(string memberId, out int flag)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(memberId)) { flag = 0; return false; }
            return _memberToFlag.TryGetValue(memberId.Trim(), out flag);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            List<string> files = FindAiDatFiles();
            for (int i = 0; i < files.Count; i++)
            {
                if (ParseAiDat(files[i]))
                {
                    _audit = "aiDat='" + files[i] + "' suffixes=" + _suffixToFlag.Count.ToString(CultureInfo.InvariantCulture) +
                             " members=" + _memberToFlag.Count.ToString(CultureInfo.InvariantCulture);
                    return;
                }
            }
            _audit = "aiDat_missing_using_vanilla_fallback";
        }

        private static List<string> FindAiDatFiles()
        {
            var result = new List<string>();
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; i < roots.Length; i++)
            {
                AddFile(result, Path.Combine(roots[i], "AI", "ai.dat"));
                AddFile(result, Path.Combine(roots[i], "Ai", "ai.dat"));
                AddFile(result, Path.Combine(roots[i], "ai.dat"));
            }
            return result;
        }

        private static void AddFile(List<string> result, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            for (int i = 0; i < result.Count; i++)
                if (string.Equals(result[i], path, StringComparison.OrdinalIgnoreCase)) return;
            result.Add(path);
        }

        private static bool ParseAiDat(string path)
        {
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch { try { lines = File.ReadAllLines(path); } catch { return false; } }

            int found = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(lines[i]);
                if (line.Length == 0 || line[0] == '@') continue;
                string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
                if (t.Length < 6) continue;
                if (t[2].IndexOf('(') < 0 || t[2].IndexOf(')') < 0) continue;
                int flag;
                if (!int.TryParse(t[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out flag)) continue;
                string member = t[2].Trim();
                string suffix = ExtractNationSuffixStatic(member);
                if (!string.IsNullOrEmpty(suffix) && !_suffixToFlag.ContainsKey(suffix)) _suffixToFlag.Add(suffix, flag);
                if (!_memberToFlag.ContainsKey(member)) _memberToFlag.Add(member, flag);
                found++;
            }
            return found > 0;
        }

        private static string ExtractNationSuffixStatic(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return string.Empty;
            int a = objectId.LastIndexOf('(');
            int b = objectId.LastIndexOf(')');
            if (a >= 0 && b > a + 1) return objectId.Substring(a + 1, b - a - 1).Trim();
            return string.Empty;
        }
    }

    public sealed class C2EditorCountryV333LikeOriginal
    {
        public string Id = string.Empty;
        public string SourcePath = string.Empty;
        public readonly List<string> UnitIds = new List<string>();
    }

    public sealed class C2OriginalProduceItemV13
    {
        public string BuilderId = string.Empty;
        public string BuilderMd = string.Empty;
        public string UnitId = string.Empty;
        public string MdName = string.Empty;
        public int GridX;
        public int GridY;
        public int Nation;
        public bool Enabled = true;
        public bool Building;
        public bool Peasant;
        public string IconFileId = "Interf3\\BldSmallIcons";
        public int IconSpriteId;
        public int RootSpriteId = 21;
        public string Source = string.Empty;
        public string DisplayNameKey = string.Empty;
        public string DisplayText = string.Empty;
        public string HotKey = string.Empty;
    }


    internal sealed class C2OriginalBuildingUpgradeItemV29
    {
        public string BuildingId = string.Empty;
        public string UpgradeId = string.Empty;
        public string IconName = string.Empty;
        public string IconFileId = "Interf3\\BldSmallIcons";
        public int IconSpriteId;
        public int GridX;
        public int GridY;
        public string Source = string.Empty;
    }

    internal static class C2OriginalProduceCatalogV13
    {
        private static bool _loaded;
        private static string _audit = "not_loaded";
        private static readonly Dictionary<string, string> _memberToMd = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdToMember = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdNationToMember = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<C2ProduceRefV13>> _fixedProduce = new Dictionary<string, List<C2ProduceRefV13>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2UpgradeDefV29> _upgradeDefsV29 = new Dictionary<string, C2UpgradeDefV29>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> _upgradePlacesV29 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2MdIconInfoV13> _mdCache = new Dictionary<string, C2MdIconInfoV13>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdPathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdListNamesV141 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdListHintNamesV141 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private struct C2SettlementFarmRuleV384ALikeOriginal
        {
            public int NewNFarm;
            public int NFarmsPerSettlement;
        }
        private static readonly Dictionary<string, C2SettlementFarmRuleV384ALikeOriginal> _settlementFarmRulesV384ALikeOriginal =
            new Dictionary<string, C2SettlementFarmRuleV384ALikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static int _unitsPerFarmV384ALikeOriginal = -1;
        private static int _mdListFilesV141;
        private static int _mdListDirectNamesV141;
        private static int _mdListHintNamesCountV141;


        public static void ForceReloadLikeOriginal()
        {
            _loaded = false;
            _audit = "not_loaded";
            _memberToMd.Clear();
            _mdToMember.Clear();
            _mdNationToMember.Clear();
            _fixedProduce.Clear();
            _upgradeDefsV29.Clear();
            _upgradePlacesV29.Clear();
            _mdCache.Clear();
            _mdPathCache.Clear();
            _mdListNamesV141.Clear();
            _mdListHintNamesV141.Clear();
            _settlementFarmRulesV384ALikeOriginal.Clear();
            _unitsPerFarmV384ALikeOriginal = -1;
            _mdListFilesV141 = 0;
            _mdListDirectNamesV141 = 0;
            _mdListHintNamesCountV141 = 0;
            _iconListLoaded = false;
            _iconListCache.Clear();
        }

        public static List<C2EditorCountryV333LikeOriginal> BuildEditorCountriesV333LikeOriginal(out string audit)
        {
            EnsureLoaded();
            var result = new List<C2EditorCountryV333LikeOriginal>();
            var playableFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Austria", "England", "France", "Egipet", "Osman",
                "Poland", "Prussia", "Russia", "Spain", "Neutral"
            };

            List<string> files = FindNdsFiles();
            for (int f = 0; f < files.Count; f++)
            {
                string path = files[f];
                if (!playableFiles.Contains(Path.GetFileNameWithoutExtension(path))) continue;
                string[] lines;
                try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
                catch { try { lines = File.ReadAllLines(path); } catch { continue; } }

                int countryLine = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.Equals(CleanLine(lines[i]), "[COUNTRY]", StringComparison.OrdinalIgnoreCase))
                    {
                        countryLine = i;
                        break;
                    }
                }
                if (countryLine < 0 || countryLine + 1 >= lines.Length) continue;
                string[] header = SplitTokens(CleanLine(lines[countryLine + 1]));
                int declared;
                if (header.Length < 2 || !int.TryParse(header[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out declared) || declared <= 0)
                    continue;

                var country = new C2EditorCountryV333LikeOriginal();
                country.Id = header[0];
                country.SourcePath = path;
                for (int i = countryLine + 2; i < lines.Length && country.UnitIds.Count < declared; i++)
                {
                    string line = CleanLine(lines[i]);
                    if (line.Length == 0 || line[0] == '#') continue;
                    if (line[0] == '[') break;
                    string[] t = SplitTokens(line);
                    if (t.Length == 0 || !_memberToMd.ContainsKey(t[0])) continue;
                    if (!country.UnitIds.Contains(t[0])) country.UnitIds.Add(t[0]);
                }
                if (country.UnitIds.Count > 0) result.Add(country);
            }

            result.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
            audit = _audit + " countries=" + result.Count.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        public static List<C2OriginalProduceItemV13> BuildEditorCatalogV333LikeOriginal(
            C2EditorCountryV333LikeOriginal country,
            out string audit)
        {
            EnsureLoaded();
            var result = new List<C2OriginalProduceItemV13>();
            IList<string> ids = country != null ? country.UnitIds : null;
            for (int i = 0; ids != null && i < ids.Count; i++)
            {
                C2OriginalProduceItemV13 item;
                if (TryBuildEditorItemForUnitIdV333LikeOriginal(ids[i], out item)) result.Add(item);
            }
            result.Sort((a, b) => string.Compare(a.DisplayText, b.DisplayText, StringComparison.CurrentCultureIgnoreCase));
            audit = _audit + " country='" + (country != null ? country.Id : "<null>") +
                    "' editorItems=" + result.Count.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        public static bool TryBuildEditorItemForUnitIdV333LikeOriginal(string unitId, out C2OriginalProduceItemV13 item)
        {
            EnsureLoaded();
            item = null;
            if (string.IsNullOrWhiteSpace(unitId))
                return false;
            string md;
            if (!_memberToMd.TryGetValue(unitId, out md) || string.IsNullOrWhiteSpace(md))
                md = ResolveMdForMemberOrRaw(unitId);
            if (string.IsNullOrWhiteSpace(md)) return false;

            C2MdIconInfoV13 icon = LoadMdIcon(md);
            if (string.IsNullOrWhiteSpace(icon.Path)) return false;
            item = new C2OriginalProduceItemV13();
            item.UnitId = unitId;
            item.MdName = md;
            item.Building = icon.Building;
            item.Peasant = icon.Peasant;
            item.Enabled = true;
            item.DisplayNameKey = icon.NameKey;
            item.DisplayText = ResolveUiTextLikeOriginal(icon.NameKey);
            if (string.IsNullOrWhiteSpace(item.DisplayText)) item.DisplayText = unitId;
            // Map editor Creator uses the object's MD MINICON. BIGICON is the left portrait,
            // INMENUICON is for other menu layouts and must not override this button.
            if (!string.IsNullOrWhiteSpace(icon.MinIconFile))
            {
                item.IconFileId = icon.MinIconFile;
                item.IconSpriteId = icon.MinIconSprite;
            }
            else
                ResolveProduceIconLikeOriginal(icon, out item.IconFileId, out item.IconSpriteId);
            item.Source = "editor_country_minicon_v333";
            return true;
        }

        public static C2MdIconInfoV13 LoadMdInfoForSelectedUnit(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            EnsureLoaded();
            if (unit == null) return new C2MdIconInfoV13();
            string md = ResolveMdForMemberOrRaw(ResolveMemberIdForSelectedUnit(unit));
            if (string.IsNullOrEmpty(md)) md = unit.ResolvedMd;
            return LoadMdIcon(md);
        }

        public static C2MdIconInfoV13 LoadMdInfoForSelectedBuilding(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            EnsureLoaded();
            string md = ResolveMdForSelectedBuildingLikeOriginal(building);
            return LoadMdIcon(md);
        }

        public static C2MdIconInfoV13 LoadMdInfoForRawMemberV166LikeOriginal(string unitId)
        {
            EnsureLoaded();
            string md = ResolveMdForMemberOrRaw(unitId);
            if (string.IsNullOrEmpty(md)) md = StripNationSuffix(unitId);
            return LoadMdIcon(md);
        }

        public static string ExtractNationSuffixFromIdPublicV166LikeOriginal(string objectId)
        {
            return ExtractNationSuffixFromIdLikeOriginal(objectId);
        }

        public static string ResolveMdForSelectedBuildingLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            EnsureLoaded();
            if (building == null) return string.Empty;
            string member = ResolveMemberIdForSelectedBuilding(building);
            string md = ResolveMdForMemberOrRaw(member);
            if (string.IsNullOrEmpty(md)) md = StripNationSuffix(building.SourceMonsterId);
            return md;
        }

        public static string StripNationSuffixPublicLikeOriginal(string s)
        {
            return StripNationSuffix(s);
        }

        public static List<C2OriginalProduceItemV13> BuildForSelectedUnit(C2NeutralPeasantUnitInfoV2LikeOriginal unit, out string audit)
        {
            EnsureLoaded();
            var result = new List<C2OriginalProduceItemV13>();
            if (unit == null)
            {
                audit = _audit + " selected=<null>";
                return result;
            }

            string builderId = ResolveMemberIdForSelectedUnit(unit);
            string builderMd = ResolveMdForMemberOrRaw(builderId);
            List<C2ProduceRefV13> refs = null;
            if (!string.IsNullOrEmpty(builderId)) _fixedProduce.TryGetValue(builderId, out refs);

            if (refs == null || refs.Count == 0)
            {
                audit = _audit + " selected='" + unit.SourceMonsterId + "' md='" + unit.ResolvedMd + "' builderId='" + builderId + "' produce=0";
                return result;
            }

            for (int i = 0; i < refs.Count; i++)
            {
                C2ProduceRefV13 r = refs[i];
                string md = ResolveMdForMemberOrRaw(r.UnitId);
                C2MdIconInfoV13 icon = LoadMdIcon(md);
                var item = new C2OriginalProduceItemV13();
                item.BuilderId = builderId ?? string.Empty;
                item.BuilderMd = builderMd ?? string.Empty;
                item.UnitId = r.UnitId ?? string.Empty;
                item.MdName = md ?? string.Empty;
                item.GridX = r.X;
                item.GridY = r.Y;
                item.Nation = unit.Nation;
                ResolveProduceIconLikeOriginal(icon, out item.IconFileId, out item.IconSpriteId);
                item.Building = icon.Building;
                item.Peasant = icon.Peasant;
                item.Enabled = true;
                item.RootSpriteId = 21; // va_Unit_P_Box: base XML sprite 22 falls back to 21 when no runtime queue/unlimit is exposed yet.
                item.Source = r.Source;
                item.DisplayNameKey = icon.NameKey;
                item.DisplayText = C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal(icon.NameKey);
                item.HotKey = !string.IsNullOrEmpty(icon.HotKey) ? icon.HotKey : (r.HotKey == '\0' ? string.Empty : r.HotKey.ToString());
                result.Add(item);
            }

            audit = _audit + " selected='" + unit.SourceMonsterId + "' md='" + unit.ResolvedMd + "' builderId='" + builderId + "' builderMd='" + builderMd + "' produce=" + result.Count.ToString(CultureInfo.InvariantCulture);
            return result;
        }


        public static List<C2OriginalProduceItemV13> BuildForSelectedBuilding(C2SettlementBuildingSelectableV1LikeOriginal building, out string audit)
        {
            EnsureLoaded();
            var result = new List<C2OriginalProduceItemV13>();
            if (building == null)
            {
                audit = _audit + " selectedBuilding=<null>";
                return result;
            }

            string builderId = ResolveMemberIdForSelectedBuilding(building);
            string builderMd = ResolveMdForMemberOrRaw(builderId);
            List<C2ProduceRefV13> refs = null;
            if (!string.IsNullOrEmpty(builderId)) _fixedProduce.TryGetValue(builderId, out refs);

            if (refs == null || refs.Count == 0)
            {
                audit = _audit + " selectedBuilding='" + building.SourceMonsterId + "' md='" + builderMd + "' builderId='" + builderId + "' produce=0";
                return result;
            }

            for (int i = 0; i < refs.Count; i++)
            {
                C2ProduceRefV13 r = refs[i];
                string md = ResolveMdForMemberOrRaw(r.UnitId);
                C2MdIconInfoV13 icon = LoadMdIcon(md);
                var item = new C2OriginalProduceItemV13();
                item.BuilderId = builderId ?? string.Empty;
                item.BuilderMd = builderMd ?? string.Empty;
                item.UnitId = r.UnitId ?? string.Empty;
                item.MdName = md ?? string.Empty;
                item.GridX = r.X;
                item.GridY = r.Y;
                item.Nation = ResolveNationForBuildingLikeOriginal(building);
                ResolveProduceIconLikeOriginal(icon, out item.IconFileId, out item.IconSpriteId);
                item.Building = icon.Building && !icon.SelfTransform;
                item.Peasant = icon.Peasant;
                item.Enabled = true;
                item.RootSpriteId = 21;
                item.Source = r.Source;
                item.DisplayNameKey = icon.NameKey;
                item.DisplayText = ResolveUiTextLikeOriginal(icon.NameKey);
                item.HotKey = !string.IsNullOrEmpty(icon.HotKey) ? icon.HotKey : (r.HotKey == '\0' ? string.Empty : r.HotKey.ToString());
                result.Add(item);
            }

            bool hasBuildingProduce = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].Building)
                {
                    hasBuildingProduce = true;
                    break;
                }
            }

            if (hasBuildingProduce)
            {
                for (int i = result.Count - 1; i >= 0; i--)
                {
                    if (!result[i].Building)
                        result.RemoveAt(i);
                }
            }

            audit = _audit + " selectedBuilding='" + building.SourceMonsterId + "' md='" + builderMd + "' builderId='" + builderId + "' produce=" + result.Count.ToString(CultureInfo.InvariantCulture) +
                    " buildingProduceFilter=" + hasBuildingProduce;
            return result;
        }

        public static List<C2OriginalProduceItemV13> BuildAllProducedUnitsForNationPrefixesV139LikeOriginal(
            int nation,
            string[] mdPrefixes,
            out string audit)
        {
            EnsureLoaded();
            var result = new List<C2OriginalProduceItemV13>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int refsSeen = 0;
            int unitRefs = 0;
            int prefixMatched = 0;

            foreach (KeyValuePair<string, List<C2ProduceRefV13>> kv in _fixedProduce)
            {
                string builderId = kv.Key ?? string.Empty;
                string builderMd = ResolveMdForMemberOrRaw(builderId);
                List<C2ProduceRefV13> refs = kv.Value;
                if (refs == null) continue;

                for (int i = 0; i < refs.Count; i++)
                {
                    refsSeen++;
                    C2ProduceRefV13 r = refs[i];
                    string md = ResolveMdForMemberOrRaw(r.UnitId);
                    if (string.IsNullOrEmpty(md)) md = StripNationSuffix(r.UnitId);
                    if (string.IsNullOrEmpty(md)) continue;

                    C2MdIconInfoV13 icon = LoadMdIcon(md);
                    if (icon.Building && !icon.SelfTransform)
                        continue;

                    unitRefs++;
                    if (!C2GameplayHudV139MdMatchesAnyPrefixLikeOriginal(md, mdPrefixes))
                        continue;

                    prefixMatched++;
                    string key = (r.UnitId ?? string.Empty) + "|" + md + "|nation=" + nation.ToString(CultureInfo.InvariantCulture);
                    if (seen.Contains(key)) continue;
                    seen.Add(key);

                    var item = new C2OriginalProduceItemV13();
                    item.BuilderId = builderId;
                    item.BuilderMd = builderMd ?? string.Empty;
                    item.UnitId = r.UnitId ?? string.Empty;
                    item.MdName = md ?? string.Empty;
                    item.GridX = r.X;
                    item.GridY = r.Y;
                    item.Nation = nation;
                    ResolveProduceIconLikeOriginal(icon, out item.IconFileId, out item.IconSpriteId);
                    item.Building = false;
                    item.Peasant = icon.Peasant;
                    item.Enabled = true;
                    item.RootSpriteId = 21;
                    item.Source = r.Source;
                    item.DisplayNameKey = icon.NameKey;
                    item.DisplayText = ResolveUiTextLikeOriginal(icon.NameKey);
                    item.HotKey = !string.IsNullOrEmpty(icon.HotKey) ? icon.HotKey : (r.HotKey == '\0' ? string.Empty : r.HotKey.ToString());
                    result.Add(item);
                }
            }

            audit = _audit +
                    " refsSeen=" + refsSeen.ToString(CultureInfo.InvariantCulture) +
                    " unitRefs=" + unitRefs.ToString(CultureInfo.InvariantCulture) +
                    " prefixMatched=" + prefixMatched.ToString(CultureInfo.InvariantCulture) +
                    " unique=" + result.Count.ToString(CultureInfo.InvariantCulture) +
                    " nation=" + nation.ToString(CultureInfo.InvariantCulture) +
                    " prefixes=" + (mdPrefixes != null ? string.Join(",", mdPrefixes) : "<all>");
            return result;
        }

        private static bool C2GameplayHudV139MdMatchesAnyPrefixLikeOriginal(string md, string[] prefixes)
        {
            if (string.IsNullOrWhiteSpace(md)) return false;
            if (prefixes == null || prefixes.Length == 0) return true;
            string clean = StripNationSuffix(md).Trim();
            for (int i = 0; i < prefixes.Length; i++)
            {
                string p = prefixes[i];
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (clean.StartsWith(p.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static List<C2OriginalBuildingUpgradeItemV29> BuildUpgradesForSelectedBuildingLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, out string audit)
        {
            EnsureLoaded();
            var result = new List<C2OriginalBuildingUpgradeItemV29>();
            if (building == null)
            {
                audit = _audit + " selectedBuilding=<null> upgrades=0";
                return result;
            }

            string builderId = ResolveMemberIdForSelectedBuilding(building);
            List<string> ids = null;
            if (!string.IsNullOrEmpty(builderId)) _upgradePlacesV29.TryGetValue(builderId, out ids);
            if ((ids == null || ids.Count == 0) && !string.IsNullOrEmpty(building.SourceMonsterId))
                _upgradePlacesV29.TryGetValue(StripNationSuffix(building.SourceMonsterId), out ids);

            if (ids == null || ids.Count == 0)
            {
                audit = _audit + " selectedBuilding='" + building.SourceMonsterId + "' builderId='" + builderId + "' upgrades=0";
                return result;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                C2UpgradeDefV29 def;
                if (!_upgradeDefsV29.TryGetValue(ids[i], out def))
                    continue;

                var item = new C2OriginalBuildingUpgradeItemV29();
                item.BuildingId = builderId ?? string.Empty;
                item.UpgradeId = def.UpgradeId ?? ids[i];
                item.IconName = def.IconName ?? string.Empty;
                item.IconFileId = "Interf3\\BldSmallIcons";
                item.IconSpriteId = def.IconSprite >= 0 ? def.IconSprite : 0;
                item.GridX = def.X;
                item.GridY = def.Y;
                item.Source = def.Source;
                result.Add(item);
            }

            audit = _audit + " selectedBuilding='" + building.SourceMonsterId + "' builderId='" + builderId + "' upgrades=" + result.Count.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        private static int ResolveNationForBuildingLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null) return 0;
            string suffix = ExtractNationSuffixFromIdLikeOriginal(building.SourceMonsterId);
            if (string.Equals(suffix, "FR", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(suffix, "RU", StringComparison.OrdinalIgnoreCase)) return 8;
            if (string.Equals(suffix, "EN", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(suffix, "PR", StringComparison.OrdinalIgnoreCase)) return 7;
            if (string.Equals(suffix, "AU", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(suffix, "EG", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(suffix, "PO", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(suffix, "SP", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(suffix, "RE", StringComparison.OrdinalIgnoreCase)) return 4;
            return 0;
        }

        private static string ExtractNationSuffixFromIdLikeOriginal(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return string.Empty;
            int a = objectId.LastIndexOf('(');
            int b = objectId.LastIndexOf(')');
            if (a >= 0 && b > a + 1) return objectId.Substring(a + 1, b - a - 1).Trim();
            return string.Empty;
        }

        private static void ResolveProduceIconLikeOriginal(C2MdIconInfoV13 icon, out string fileId, out int spriteId)
        {
            // va_UnitProdPort original chain:
            // ExIcon.Draw(...) if ExIcon exists; else MINICON; else ICON/IconFileID/IconID.
            // ExIcon is registered below but exact multi-sprite draw needs a separate renderer, so keep first icon as fallback.
            if (!string.IsNullOrEmpty(icon.MinIconFile))
            {
                fileId = icon.MinIconFile;
                spriteId = icon.MinIconSprite;
                return;
            }
            if (icon.HasExIcon && !string.IsNullOrEmpty(icon.ExIconFile))
            {
                fileId = icon.ExIconFile;
                spriteId = icon.ExIconSprite;
                return;
            }
            if (icon.HasIcon)
            {
                // In the original ICON sets IconFileID=0 and IconID=GetIconByName(name).
                // The bridge does not have numeric GP file IDs, so use the visible default atlas path and iconlist index.
                fileId = !string.IsNullOrEmpty(icon.IconFile) ? icon.IconFile : "Interf3\\BldSmallIcons";
                spriteId = icon.IconSprite;
                return;
            }
            if (!string.IsNullOrEmpty(icon.InMenuIconFile))
            {
                fileId = icon.InMenuIconFile;
                spriteId = icon.InMenuIconSprite;
                return;
            }
            if (!string.IsNullOrEmpty(icon.BigIconFile))
            {
                fileId = icon.BigIconFile;
                spriteId = icon.BigIconSprite;
                return;
            }
            fileId = "Interf3\\BldSmallIcons";
            spriteId = 0;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            int files = 0;
            int members = 0;
            int produce = 0;
            List<string> ndsFiles = FindNdsFiles();
            files = ndsFiles.Count;
            for (int i = 0; i < ndsFiles.Count; i++)
            {
                ParseNdsFile(ndsFiles[i], ref members, ref produce);
            }

            LoadMdListNamesV141LikeOriginal();

            _audit = "ndsFiles=" + files.ToString(CultureInfo.InvariantCulture) +
                     " members=" + members.ToString(CultureInfo.InvariantCulture) +
                     " fixedProduceBuilders=" + _fixedProduce.Count.ToString(CultureInfo.InvariantCulture) +
                     " fixedProduceItems=" + produce.ToString(CultureInfo.InvariantCulture) +
                     " mdListFiles=" + _mdListFilesV141.ToString(CultureInfo.InvariantCulture) +
                     " mdListNames=" + _mdListDirectNamesV141.ToString(CultureInfo.InvariantCulture) +
                     " mdListHints=" + _mdListHintNamesCountV141.ToString(CultureInfo.InvariantCulture);
        }

        public static string ResolveMdDisplayNameV141LikeOriginal(string mdName)
        {
            EnsureLoaded();
            string key = NormalizeMdListKeyV141LikeOriginal(mdName);
            if (string.IsNullOrEmpty(key)) return string.Empty;

            string name;
            if (_mdListNamesV141.TryGetValue(key, out name) && IsUsefulMdListNameV141LikeOriginal(name))
                return name;

            if (_mdListHintNamesV141.TryGetValue(key, out name) && IsUsefulMdListNameV141LikeOriginal(name))
                return name;

            // mdlist.txt contains SPNM18 only as a raw internal token ("SpnM18").
            // The selected-point card must not show internal/test identifiers.
            if (string.Equals(key, "SPNM18", StringComparison.OrdinalIgnoreCase))
                return "Мушкетер";

            return string.Empty;
        }

        private static void LoadMdListNamesV141LikeOriginal()
        {
            _mdListNamesV141.Clear();
            _mdListHintNamesV141.Clear();
            _mdListFilesV141 = 0;
            _mdListDirectNamesV141 = 0;
            _mdListHintNamesCountV141 = 0;

            List<string> files = FindMdListFilesV141LikeOriginal();
            for (int i = 0; i < files.Count; i++)
                ParseMdListFileV141LikeOriginal(files[i]);
        }

        private static List<string> FindMdListFilesV141LikeOriginal()
        {
            var result = new List<string>();
            string[] roots = OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; i < roots.Length; i++)
            {
                string root = roots[i];
                if (string.IsNullOrEmpty(root)) continue;

                AddFileV141LikeOriginal(result, Path.Combine(root, "Text", "mdlist.txt"));
                AddFileV141LikeOriginal(result, Path.Combine(root, "Text", "mdlist1.txt"));
                AddFileV141LikeOriginal(result, Path.Combine(root, "Text", "mdlist1_2.txt"));
                AddFileV141LikeOriginal(result, Path.Combine(root, "mdlist.txt"));
                AddFileV141LikeOriginal(result, Path.Combine(root, "mdlist1.txt"));
                AddFileV141LikeOriginal(result, Path.Combine(root, "mdlist1_2.txt"));

                string textDir = Path.Combine(root, "Text");
                if (Directory.Exists(textDir))
                {
                    try
                    {
                        string[] found = Directory.GetFiles(textDir, "mdlist*.txt", SearchOption.TopDirectoryOnly);
                        for (int k = 0; found != null && k < found.Length; k++)
                            AddFileV141LikeOriginal(result, found[k]);
                    }
                    catch { }
                }
            }
            return result;
        }

        private static void AddFileV141LikeOriginal(List<string> result, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            for (int i = 0; i < result.Count; i++)
                if (string.Equals(result[i], path, StringComparison.OrdinalIgnoreCase)) return;
            result.Add(path);
        }

        private static void ParseMdListFileV141LikeOriginal(string path)
        {
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch
            {
                try { lines = File.ReadAllLines(path, Encoding.GetEncoding(866)); }
                catch
                {
                    try { lines = File.ReadAllLines(path); }
                    catch { return; }
                }
            }

            _mdListFilesV141++;

            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                string raw = lines[i] ?? string.Empty;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                    continue;

                string upper = line.ToUpperInvariant();
                if (!upper.StartsWith("UNITSMD\\", StringComparison.Ordinal))
                    continue;

                int mdPos = upper.IndexOf(".MD", StringComparison.Ordinal);
                if (mdPos <= "UNITSMD\\".Length)
                    continue;

                string md = line.Substring("UNITSMD\\".Length, mdPos - "UNITSMD\\".Length);
                string key = NormalizeMdListKeyV141LikeOriginal(md);
                if (string.IsNullOrEmpty(key))
                    continue;

                bool isHint = upper.IndexOf(".MD.HINT", mdPos, StringComparison.Ordinal) >= 0;
                int textStart = isHint ? mdPos + ".MD.HINT".Length : mdPos + ".MD".Length;
                if (textStart > line.Length) continue;

                string rest = line.Substring(textStart).Trim();
                string title = isHint ? ExtractHintTitleV141LikeOriginal(rest) : CleanMdListTitleV141LikeOriginal(rest);
                if (!IsUsefulMdListNameV141LikeOriginal(title))
                    continue;

                if (isHint)
                {
                    if (!_mdListHintNamesV141.ContainsKey(key))
                    {
                        _mdListHintNamesV141.Add(key, title);
                        _mdListHintNamesCountV141++;
                    }
                }
                else
                {
                    if (!_mdListNamesV141.ContainsKey(key))
                    {
                        _mdListNamesV141.Add(key, title);
                        _mdListDirectNamesV141++;
                    }
                }
            }
        }

        private static string NormalizeMdListKeyV141LikeOriginal(string mdName)
        {
            if (string.IsNullOrWhiteSpace(mdName)) return string.Empty;
            string s = mdName.Trim().Replace('/', '\\');
            int slash = s.LastIndexOf('\\');
            if (slash >= 0) s = s.Substring(slash + 1);
            if (s.EndsWith(".HINT", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - ".HINT".Length);
            if (s.EndsWith(".MD", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - ".MD".Length);
            return StripNationSuffix(s).Trim().ToUpperInvariant();
        }

        private static string ExtractHintTitleV141LikeOriginal(string rest)
        {
            if (string.IsNullOrWhiteSpace(rest)) return string.Empty;
            string s = rest.Trim();

            int colorClose = s.IndexOf('}');
            if (colorClose >= 0 && colorClose + 1 < s.Length)
                s = s.Substring(colorClose + 1);

            int fs = s.IndexOf("{FS}", StringComparison.OrdinalIgnoreCase);
            if (fs >= 0) s = s.Substring(0, fs);

            int slash = s.IndexOf('\\');
            if (slash >= 0) s = s.Substring(0, slash);

            return CleanMdListTitleV141LikeOriginal(s);
        }

        private static string CleanMdListTitleV141LikeOriginal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            string r = Regex.Replace(s, "\\{[^}]*\\}", string.Empty);
            r = r.Replace("\\", " ");
            r = Regex.Replace(r, "\\s+", " ").Trim();
            return r;
        }

        private static bool IsUsefulMdListNameV141LikeOriginal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            string t = s.Trim();
            if (t.Length == 0) return false;

            bool hasCyrillic = false;
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if ((c >= '\u0400' && c <= '\u04FF') || c == 'ё' || c == 'Ё')
                {
                    hasCyrillic = true;
                    break;
                }
            }
            if (!hasCyrillic) return false;

            return true;
        }

        private static List<string> FindNdsFiles()
        {
            var result = new List<string>();
            var dirs = new List<string>();
            // Active/modded game data must win over Unity fallback copies.
            AddDataRoot(dirs, @"C:\GSC Game World\Cossacks II\Data");
            AddDataRoot(dirs, @"C:\GSC Game World\Cossacks II\Data1");
            AddDataRoot(dirs, @"C:\Program Files (x86)\GSC Game World\Cossacks II\Data");
            AddDataRoot(dirs, @"C:\Games\Cossacks II\Data");
            AddDataRoot(dirs, Path.Combine(Application.dataPath, "..", "Data"));
            AddDataRoot(dirs, Path.Combine(Application.dataPath, "..", "Cossacks2", "Data"));
            AddDataRoot(dirs, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));
            AddDataRoot(dirs, Path.Combine(Application.streamingAssetsPath, "Cossacks2"));
            AddDataRoot(dirs, Application.streamingAssetsPath);
            AddDataRoot(dirs, Path.Combine(Application.dataPath, "Resources"));
            AddDataRoot(dirs, Path.Combine(Application.dataPath, "Resources", "Data"));

            int baseCount = dirs.Count;
            for (int i = 0; i < baseCount; i++)
            {
                AddDataRoot(dirs, Path.Combine(dirs[i], "Nation"));
                AddDataRoot(dirs, Path.Combine(dirs[i], "Nations"));
                AddDataRoot(dirs, Path.Combine(dirs[i], "Data"));
                AddDataRoot(dirs, Path.Combine(dirs[i], "Data1"));
            }

            for (int i = 0; i < dirs.Count; i++)
            {
                string dir = dirs[i];
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                AddNdsFromDir(result, dir, "*.NDS");
                AddNdsFromDir(result, dir, "*.nds");
            }
            return result;
        }

        private static void AddDataRoot(List<string> dirs, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            string full;
            try { full = Path.GetFullPath(path); }
            catch { full = path; }
            for (int i = 0; i < dirs.Count; i++)
                if (string.Equals(dirs[i], full, StringComparison.OrdinalIgnoreCase)) return;
            dirs.Add(full);
        }

        private static void AddNdsFromDir(List<string> result, string dir, string mask)
        {
            try
            {
                string[] files = Directory.GetFiles(dir, mask, SearchOption.TopDirectoryOnly);
                for (int i = 0; files != null && i < files.Length; i++)
                {
                    string f = files[i];
                    bool exists = false;
                    for (int k = 0; k < result.Count; k++)
                    {
                        if (string.Equals(result[k], f, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                    }
                    if (!exists) result.Add(f);
                }
            }
            catch { }
        }

        private static void ParseNdsFile(string path, ref int members, ref int produce)
        {
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch
            {
                try { lines = File.ReadAllLines(path); }
                catch { return; }
            }

            string section = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = CleanLine(lines[i]);
                if (line.Length == 0) continue;
                if (line[0] == '[')
                {
                    section = line.ToUpperInvariant();
                    continue;
                }

                string[] t = SplitTokens(line);
                if (t.Length == 0) continue;

                if (section == "[MEMBERS]")
                {
                    if (t.Length >= 2)
                    {
                        string unitId = t[0];
                        string md = t[1];
                        if (!_memberToMd.ContainsKey(unitId)) _memberToMd.Add(unitId, md);
                        if (!_mdToMember.ContainsKey(md)) _mdToMember.Add(md, unitId);
                        string nationKey = MdNationKeyLikeOriginal(md, ExtractNationSuffixFromIdLikeOriginal(unitId));
                        if (!string.IsNullOrEmpty(nationKey) && !_mdNationToMember.ContainsKey(nationKey))
                            _mdNationToMember.Add(nationKey, unitId);
                        members++;
                    }
                }
                else if (section == "[SETTLFARMS]")
                {
                    int newNFarm;
                    int farmsPerSettlement;
                    if (t.Length >= 3 &&
                        int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out newNFarm) &&
                        int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out farmsPerSettlement))
                    {
                        string unitId = t[0].Trim();
                        if (!_settlementFarmRulesV384ALikeOriginal.ContainsKey(unitId))
                        {
                            _settlementFarmRulesV384ALikeOriginal.Add(unitId, new C2SettlementFarmRuleV384ALikeOriginal
                            {
                                NewNFarm = Mathf.Max(0, newNFarm),
                                NFarmsPerSettlement = Mathf.Max(0, farmsPerSettlement)
                            });
                        }
                    }
                }
                else if (section == "[FIXED_PRODUCE]")
                {
                    int count;
                    if (t.Length >= 2 && int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                    {
                        string builder = t[0];
                        var list = new List<C2ProduceRefV13>();
                        for (int j = 0; j < count && i + 1 < lines.Length; j++)
                        {
                            i++;
                            string pl = CleanLine(lines[i]);
                            if (pl.Length == 0) { j--; continue; }
                            if (pl[0] == '[') { i--; break; }
                            string[] p = SplitTokens(pl);
                            if (p.Length < 4) continue;
                            int x;
                            int y;
                            if (!int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out x)) x = j % 12;
                            if (!int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out y)) y = 1 + j / 12;
                            string hk = p[3];
                            var pr = new C2ProduceRefV13();
                            pr.UnitId = p[0];
                            pr.X = Mathf.Clamp(x, 0, 11);
                            pr.Y = Mathf.Clamp(y, 0, 8);
                            pr.HotKey = string.IsNullOrEmpty(hk) || hk == "NONE" || hk == "----" ? '\0' : hk[0];
                            pr.Source = Path.GetFileName(path) + ":FIXED_PRODUCE";
                            list.Add(pr);
                            produce++;
                        }
                        if (!_fixedProduce.ContainsKey(builder))
                            _fixedProduce.Add(builder, list);
                    }
                }
                else if (section == "[PRODUCE]")
                {
                    int count;
                    if (t.Length >= 2 && int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) && !_fixedProduce.ContainsKey(t[0]))
                    {
                        string builder = t[0];
                        var list = new List<C2ProduceRefV13>();
                        for (int j = 0; j < count && i + 1 < lines.Length; j++)
                        {
                            i++;
                            string pl = CleanLine(lines[i]);
                            if (pl.Length == 0) { j--; continue; }
                            if (pl[0] == '[') { i--; break; }
                            string[] p = SplitTokens(pl);
                            if (p.Length < 1) continue;
                            var pr = new C2ProduceRefV13();
                            pr.UnitId = p[0];
                            pr.X = j % 12;
                            pr.Y = 1 + j / 12;
                            pr.HotKey = '\0';
                            pr.Source = Path.GetFileName(path) + ":PRODUCE_sequential_fallback";
                            list.Add(pr);
                            produce++;
                        }
                        if (!_fixedProduce.ContainsKey(builder))
                            _fixedProduce.Add(builder, list);
                    }
                }
                else if (section == "[UPGRADE]")
                {
                    if (t.Length >= 2)
                    {
                        C2UpgradeDefV29 def = new C2UpgradeDefV29();
                        def.UpgradeId = t[0];
                        def.IconName = t[1];
                        def.IconSprite = ResolveIconListIndexLikeOriginal(def.IconName);
                        def.X = 0;
                        def.Y = 0;
                        def.Source = Path.GetFileName(path) + ":UPGRADE";
                        for (int k = 2; k + 2 < t.Length; k++)
                        {
                            if (string.Equals(t[k], "#POSITION", StringComparison.OrdinalIgnoreCase))
                            {
                                int x;
                                int y;
                                if (int.TryParse(t[k + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out x)) def.X = Mathf.Clamp(x, 0, 11);
                                if (int.TryParse(t[k + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out y)) def.Y = Mathf.Clamp(y, 0, 8);
                                break;
                            }
                        }
                        if (!_upgradeDefsV29.ContainsKey(def.UpgradeId))
                            _upgradeDefsV29.Add(def.UpgradeId, def);
                    }
                }
                else if (section == "[UPGRADEPLACE]")
                {
                    int count;
                    if (t.Length >= 2 && int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                    {
                        string builder = t[0];
                        var list = new List<string>();
                        for (int k = 2; k < t.Length; k++)
                        {
                            if (!string.IsNullOrEmpty(t[k])) list.Add(t[k]);
                        }

                        for (int j = list.Count; j < count && i + 1 < lines.Length; j++)
                        {
                            i++;
                            string pl = CleanLine(lines[i]);
                            if (pl.Length == 0) { j--; continue; }
                            if (pl[0] == '[') { i--; break; }
                            string[] p = SplitTokens(pl);
                            if (p.Length < 1) continue;
                            list.Add(p[0]);
                        }

                        if (!_upgradePlacesV29.ContainsKey(builder))
                            _upgradePlacesV29.Add(builder, list);
                    }
                }
            }
        }

        private static string ResolveMemberIdForSelectedUnit(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string[] keys = BuildSelectedKeys(unit);
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                if (string.IsNullOrEmpty(k)) continue;
                if (_memberToMd.ContainsKey(k)) return k;
                if (_fixedProduce.ContainsKey(k)) return k;
            }

            string suffix = ExtractNationSuffixFromIdLikeOriginal(unit != null ? unit.SourceMonsterId : string.Empty);
            if (string.IsNullOrEmpty(suffix) && unit != null)
                suffix = NationSuffixFromIndexLikeOriginal(unit.Nation);
            for (int i = 0; i < keys.Length; i++)
            {
                string nationKey = MdNationKeyLikeOriginal(keys[i], suffix);
                string memberByNation;
                if (!string.IsNullOrEmpty(nationKey) && _mdNationToMember.TryGetValue(nationKey, out memberByNation))
                    return memberByNation;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                string member;
                if (!string.IsNullOrEmpty(k) && _mdToMember.TryGetValue(k, out member)) return member;
            }
            return keys.Length > 0 ? keys[0] : string.Empty;
        }

        private static string ResolveMemberIdForSelectedBuilding(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            string[] keys = BuildSelectedKeys(building);
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                if (string.IsNullOrEmpty(k)) continue;
                if (_memberToMd.ContainsKey(k)) return k;
                if (_fixedProduce.ContainsKey(k)) return k;
                if (_upgradePlacesV29.ContainsKey(k)) return k;
            }
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                string member;
                if (!string.IsNullOrEmpty(k) && _mdToMember.TryGetValue(k, out member)) return member;
            }
            return keys.Length > 0 ? keys[0] : string.Empty;
        }

        private static string[] BuildSelectedKeys(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            var list = new List<string>();
            AddKey(list, unit.SourceMonsterId);
            AddKey(list, unit.ResolvedMd);
            AddKey(list, StripNationSuffix(unit.SourceMonsterId));
            AddKey(list, StripNationSuffix(unit.ResolvedMd));
            return list.ToArray();
        }

        private static string[] BuildSelectedKeys(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            var list = new List<string>();
            if (building == null) return list.ToArray();
            AddKey(list, building.SourceMonsterId);
            AddKey(list, StripNationSuffix(building.SourceMonsterId));
            AddKey(list, building.KindName);
            AddKey(list, StripNationSuffix(building.KindName));

            string stripped = StripNationSuffix(building.SourceMonsterId);
            string suffix = ExtractNationSuffixFromIdLikeOriginal(building.SourceMonsterId);
            if (!string.IsNullOrEmpty(stripped) && !string.IsNullOrEmpty(suffix))
                AddKey(list, stripped + "(" + suffix + ")");
            return list.ToArray();
        }

        private static void AddKey(List<string> list, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            string k = key.Trim();
            for (int i = 0; i < list.Count; i++)
                if (string.Equals(list[i], k, StringComparison.OrdinalIgnoreCase)) return;
            list.Add(k);
        }

        private static string StripNationSuffix(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            int p = s.IndexOf('(');
            if (p > 0) return s.Substring(0, p).Trim();
            return s.Trim();
        }

        private static string NationSuffixFromIndexLikeOriginal(int nation)
        {
            if (nation == 6) return "FR";
            if (nation == 8) return "RU";
            if (nation == 2) return "EN";
            if (nation == 7) return "PR";
            if (nation == 0) return "AU";
            if (nation == 1) return "EG";
            if (nation == 3) return "PO";
            if (nation == 5) return "SP";
            if (nation == 4) return "RE";
            return string.Empty;
        }

        private static string MdNationKeyLikeOriginal(string mdName, string nationSuffix)
        {
            string md = StripNationSuffix(mdName);
            string suffix = nationSuffix != null ? nationSuffix.Trim() : string.Empty;
            if (string.IsNullOrEmpty(md) || string.IsNullOrEmpty(suffix)) return string.Empty;
            return md.ToUpperInvariant() + "|" + suffix.ToUpperInvariant();
        }

        private static int GetUnitsPerFarmV384ALikeOriginal()
        {
            if (_unitsPerFarmV384ALikeOriginal > 0) return _unitsPerFarmV384ALikeOriginal;
            _unitsPerFarmV384ALikeOriginal = 15;
            string[] roots = OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int r = 0; r < roots.Length; r++)
            {
                string path = Path.Combine(roots[r], "Nres.dat");
                if (!File.Exists(path)) path = Path.Combine(roots[r], "NRES.DAT");
                if (!File.Exists(path)) continue;
                string[] lines;
                try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
                catch { try { lines = File.ReadAllLines(path); } catch { continue; } }
                for (int i = 0; lines != null && i < lines.Length; i++)
                {
                    string line = CleanLine(lines[i]);
                    if (line.Length == 0) continue;
                    string[] t = SplitTokens(line);
                    int value;
                    if (t.Length >= 2 && string.Equals(t[0], "UNITS/FARM", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0)
                    {
                        _unitsPerFarmV384ALikeOriginal = value;
                        return value;
                    }
                }
            }
            return _unitsPerFarmV384ALikeOriginal;
        }

        internal static bool TryGetSettlementFarmRuleV384ALikeOriginal(
            string unitId, out int newNFarm, out int farmsPerSettlement)
        {
            EnsureLoaded();
            newNFarm = 0;
            farmsPerSettlement = 0;
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            C2SettlementFarmRuleV384ALikeOriginal rule;
            if (!_settlementFarmRulesV384ALikeOriginal.TryGetValue(unitId.Trim(), out rule))
                return false;
            newNFarm = rule.NewNFarm;
            farmsPerSettlement = rule.NFarmsPerSettlement;
            return true;
        }

        private static string ResolveMdForMemberOrRaw(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return string.Empty;
            string md;
            if (_memberToMd.TryGetValue(unitId, out md)) return md;
            return StripNationSuffix(unitId);
        }

        public static C2MdIconInfoV13 LoadMdIcon(string mdName)
        {
            if (string.IsNullOrWhiteSpace(mdName)) return new C2MdIconInfoV13();
            C2MdIconInfoV13 cached;
            if (_mdCache.TryGetValue(mdName, out cached)) return cached;

            var info = new C2MdIconInfoV13();
            info.BigWeaponFile = @"Interf3\BigWeapon";
            info.BigColdWeaponFile = @"Interf3\BigWeapon";
            info.BigFireWeaponFile = @"Interf3\BigWeapon";
            info.BigColdWeaponSprite = 0;
            info.BigFireWeaponSprite = 8;
            string path = FindMdPath(mdName);
            info.Path = path ?? string.Empty;
            if (!string.IsNullOrEmpty(path))
            {
                string[] lines;
                try { lines = File.ReadAllLines(path, Encoding.GetEncoding(866)); }
                catch
                {
                    try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
                    catch
                    {
                        try { lines = File.ReadAllLines(path); }
                        catch { lines = null; }
                    }
                }
                for (int i = 0; lines != null && i < lines.Length; i++)
                {
                    string line = CleanLine(lines[i]);
                    if (line.Length == 0) continue;
                    string[] t = SplitTokens(line);
                    if (t.Length == 0) continue;
                    string cmd = t[0].ToUpperInvariant();
                    if (cmd == "NAME" && t.Length >= 2) info.NameKey = t[1];
                    else if ((cmd == "TEXT" || cmd == "DESCR" || cmd == "DESCRIPTION" || cmd == "HINT") && t.Length >= 2) info.HintKey = t[1];
                    else if (cmd == "BUILDING") info.Building = true;
                    else if (cmd == "SELFTRANSFORM") info.SelfTransform = true;
                    else if (cmd == "PEASANT") info.Peasant = true;
                    else if (cmd == "COMMANDCENTER") info.CommandCenter = true;
                    else if (cmd == "GLOBALCOMMANDCENTER") info.GlobalCommandCenter = true;
                    else if (cmd == "FARM") info.FarmCapacity = GetUnitsPerFarmV384ALikeOriginal();
                    else if (cmd == "MFARM" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.FarmCapacity = Mathf.Max(0, v);
                    }
                    else if (cmd == "NOFARM") info.NoFarm = true;
                    else if (cmd == "GRENADE" && t.Length >= 3)
                    {
                        int maxGrenades;
                        int recharge;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out maxGrenades))
                            info.MaxGrenadesInFormation = Mathf.Max(0, maxGrenades);
                        if (int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out recharge))
                            info.GrenadeRechargeTime = Mathf.Max(0, recharge);
                    }
                    else if (cmd == "LIFE" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.LifeMax = Mathf.Max(0, v);
                    }
                    else if (cmd == "DAMAGE" && t.Length >= 3)
                    {
                        int idx, val;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out idx) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                        {
                            if (idx == 0) info.Damage0 = Mathf.Max(0, val);
                            else if (idx == 1) info.Damage1 = Mathf.Max(0, val);
                            else if (idx == 2) info.Damage2 = Mathf.Max(0, val);
                            else if (idx == 3) info.Damage3 = Mathf.Max(0, val);
                        }
                    }
                    else if (cmd == "SKILLDAMAGEMASK" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.SkillDamageMask = v;
                    }
                    else if (cmd == "SKILLDAMAGEFORMBONUS" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.SkillDamageFormationBonus = v;
                    }
                    else if (cmd == "SKILLDAMAGEFORMBONUSSTEP" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.SkillDamageFormationBonusStep = v;
                    }
                    else if (cmd == "VETERAN" && t.Length >= 4)
                    {
                        int k, d, sh;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out k) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out d) &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out sh))
                        {
                            info.VeteranKills = k;
                            info.VeteranExtraDamage = d;
                            info.VeteranExtraShield = sh;
                        }
                    }
                    else if (cmd == "EXPERT" && t.Length >= 4)
                    {
                        int k, d, sh;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out k) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out d) &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out sh))
                        {
                            info.ExpertKills = k;
                            info.ExpertExtraDamage = d;
                            info.ExpertExtraShield = sh;
                        }
                    }
                    else if (cmd == "WEAPON" && t.Length >= 3)
                    {
                        int idx;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out idx))
                        {
                            if (idx == 0) info.Weapon0 = t[2];
                            else if (idx == 1) info.Weapon1 = t[2];
                            else if (idx == 2) info.Weapon2 = t[2];
                            else if (idx == 3) info.Weapon3 = t[2];
                        }
                    }
                    else if (cmd == "ATTACK_RADIUS" && t.Length >= 4)
                    {
                        int idx, r1, r2;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out idx) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out r1) &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out r2))
                        {
                            if (idx == 0) { info.AttackRadius0Min = Mathf.Max(0, r1); info.AttackRadius0 = Mathf.Max(0, r2); }
                            else if (idx == 1) { info.AttackRadius1Min = Mathf.Max(0, r1); info.AttackRadius1 = Mathf.Max(0, r2); }
                            else if (idx == 2) { info.AttackRadius2Min = Mathf.Max(0, r1); info.AttackRadius2 = Mathf.Max(0, r2); }
                        }
                    }
                    else if (cmd == "ATTACK_PAUSE" && t.Length >= 3)
                    {
                        int idx, pause;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out idx) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out pause))
                        {
                            pause = Mathf.Max(0, pause);
                            if (idx == 0) info.AttackPause0 = pause;
                            else if (idx == 1) info.AttackPause1 = pause;
                            else if (idx == 2) info.AttackPause2 = pause;
                            else if (idx == 3) info.AttackPause3 = pause;
                        }
                    }
                    else if (cmd == "ATTPREVIEW1" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.VisibleRadius1 = Mathf.Max(0, v);
                    }
                    else if (cmd == "ATTPREVIEW2" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.VisibleRadius2 = Mathf.Max(0, v);
                    }
                    else if (cmd == "BIGCOLDWEAP" && t.Length >= 3)
                    {
                        int spr;
                        info.BigWeaponFile = t[1];
                        info.BigColdWeaponFile = t[1];
                        info.BigColdWeaponSprite = int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr) ? spr : 0;
                    }
                    else if (cmd == "BIGFIREWEAP" && t.Length >= 3)
                    {
                        int spr;
                        info.BigWeaponFile = t[1];
                        info.BigFireWeaponFile = t[1];
                        info.BigFireWeaponSprite = int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr) ? spr : 8;
                    }
                    else if (cmd == "UNITABSORBER" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.UnitAbsorber = Mathf.Max(0, v);
                    }
                    else if (cmd == "MINICON" && t.Length >= 3)
                    {
                        int spr;
                        info.MinIconFile = t[1];
                        info.MinIconSprite = int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr) ? spr : 0;
                    }
                    else if (cmd == "BIGICON" && t.Length >= 3)
                    {
                        int spr;
                        info.BigIconFile = t[1];
                        info.BigIconSprite = int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr) ? spr : 0;
                    }
                    else if (cmd == "INMENUICON" && t.Length >= 3)
                    {
                        int spr;
                        info.InMenuIconFile = t[1];
                        info.InMenuIconSprite = int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr) ? spr : 0;
                    }
                    else if (cmd == "PORTBRANCH" && t.Length >= 2)
                    {
                        int spr;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr))
                        {
                            info.HasPortBranch = true;
                            info.PortBranch = spr;
                        }
                    }
                    else if (cmd == "PORTBACKSPRITE" && t.Length >= 2)
                    {
                        int spr;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr))
                        {
                            info.HasPortBackSprite = true;
                            info.PortBackSprite = spr;
                        }
                    }
                    else if (cmd == "ICON" && t.Length >= 2)
                    {
                        info.IconName = t[1];
                        info.IconFile = "Interf3\\BldSmallIcons";
                        info.IconSprite = ResolveIconListIndexLikeOriginal(t[1]);
                        info.HasIcon = info.IconSprite >= 0;
                        if (!info.HasIcon) info.IconSprite = 0;
                    }
                    else if (cmd == "ICONEX" && t.Length >= 5)
                    {
                        int dx, dy, spr;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dx) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out dy) &&
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr))
                        {
                            if (!info.HasExIcon)
                            {
                                info.HasExIcon = true;
                                info.ExIconDx = dx;
                                info.ExIconDy = dy;
                                info.ExIconFile = t[3];
                                info.ExIconSprite = spr;
                                info.ExIconEndSprite = spr;
                                info.ExIconStep = 0;
                            }
                        }
                    }
                    else if (cmd == "ICONANM" && t.Length >= 7)
                    {
                        int dx, dy, spr, endSpr, step;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dx) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out dy) &&
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out spr) &&
                            int.TryParse(t[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out endSpr) &&
                            int.TryParse(t[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out step))
                        {
                            if (!info.HasExIcon)
                            {
                                info.HasExIcon = true;
                                info.ExIconDx = dx;
                                info.ExIconDy = dy;
                                info.ExIconFile = t[3];
                                info.ExIconSprite = spr;
                                info.ExIconEndSprite = endSpr;
                                info.ExIconStep = step;
                            }
                        }
                    }
                    else if (cmd == "MESSAGE" && t.Length >= 2)
                    {
                        info.MessageKey = line.Substring(cmd.Length).Trim();
                    }
                    else if (cmd == "BUILDHOTKEY" && t.Length >= 2)
                    {
                        info.HotKey = t[1];
                    }
                }
            }
            _mdCache[mdName] = info;
            return info;
        }

        private static string FindMdPath(string mdName)
        {
            string name = StripNationSuffix(mdName);
            if (string.IsNullOrEmpty(name)) return null;
            string cachedPath;
            if (_mdPathCache.TryGetValue(name, out cachedPath))
                return string.IsNullOrEmpty(cachedPath) ? null : cachedPath;

            var roots = new List<string>();
            // Active/modded game data first; Unity Resources are fallback copies only.
            AddDataRoot(roots, @"C:\GSC Game World\Cossacks II\Data\UnitsMD");
            AddDataRoot(roots, @"C:\GSC Game World\Cossacks II\Data\UnitsGuardMD");
            AddDataRoot(roots, @"C:\GSC Game World\Cossacks II\Data");
            AddDataRoot(roots, @"C:\GSC Game World\Cossacks II\Data1");
            AddDataRoot(roots, Path.Combine(Application.dataPath, "..", "Data", "UnitsMD"));
            AddDataRoot(roots, Path.Combine(Application.dataPath, "..", "Data", "UnitsGuardMD"));
            AddDataRoot(roots, Path.Combine(Application.dataPath, "..", "Data"));
            AddDataRoot(roots, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "UnitsMD"));
            AddDataRoot(roots, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "UnitsGuardMD"));
            AddDataRoot(roots, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));
            AddDataRoot(roots, Path.Combine(Application.dataPath, "Resources", "UnitsMD"));
            AddDataRoot(roots, Path.Combine(Application.dataPath, "Resources", "UnitsGuardMD"));
            AddDataRoot(roots, Path.Combine(Application.dataPath, "Resources"));
            for (int i = 0; i < roots.Count; i++)
            {
                string root = roots[i];
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                string[] candidates =
                {
                    Path.Combine(root, name + ".MD"),
                    Path.Combine(root, name + ".md"),
                    Path.Combine(root, "UnitsMD", name + ".MD"),
                    Path.Combine(root, "UnitsMD", name + ".md"),
                    Path.Combine(root, "UnitsGuardMD", name + ".MD"),
                    Path.Combine(root, "UnitsGuardMD", name + ".md"),
                    Path.Combine(root, "Units", name + ".MD"),
                    Path.Combine(root, "Units", name + ".md")
                };
                for (int c = 0; c < candidates.Length; c++)
                {
                    if (File.Exists(candidates[c]))
                    {
                        _mdPathCache[name] = candidates[c];
                        return candidates[c];
                    }
                }
                try
                {
                    string[] found = Directory.GetFiles(root, name + ".MD", SearchOption.TopDirectoryOnly);
                    if (found != null && found.Length > 0)
                    {
                        _mdPathCache[name] = found[0];
                        return found[0];
                    }
                    found = Directory.GetFiles(root, name + ".md", SearchOption.TopDirectoryOnly);
                    if (found != null && found.Length > 0)
                    {
                        _mdPathCache[name] = found[0];
                        return found[0];
                    }
                    found = Directory.GetFiles(root, name + ".MD", SearchOption.AllDirectories);
                    if (found != null && found.Length > 0)
                    {
                        _mdPathCache[name] = found[0];
                        return found[0];
                    }
                    found = Directory.GetFiles(root, name + ".md", SearchOption.AllDirectories);
                    if (found != null && found.Length > 0)
                    {
                        _mdPathCache[name] = found[0];
                        return found[0];
                    }
                }
                catch { }
            }
            _mdPathCache[name] = string.Empty;
            return null;
        }

        private static readonly Dictionary<string, int> _iconListCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _iconListLoaded;

        private static int ResolveIconListIndexLikeOriginal(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName)) return -1;
            EnsureIconListLoadedLikeOriginal();
            int id;
            return _iconListCache.TryGetValue(iconName.Trim(), out id) ? id : -1;
        }

        private static void EnsureIconListLoadedLikeOriginal()
        {
            if (_iconListLoaded) return;
            _iconListLoaded = true;
            string[] roots = OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int r = 0; r < roots.Length; r++)
            {
                string path = Path.Combine(roots[r], "IconList.txt");
                if (!File.Exists(path)) continue;
                string[] lines;
                try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
                catch { try { lines = File.ReadAllLines(path); } catch { continue; } }
                int idx = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = CleanLine(lines[i]);
                    if (line.Length == 0) continue;
                    string[] t = SplitTokens(line);
                    if (t.Length == 0) continue;
                    string name = t[0].Trim();
                    if (string.Equals(name, "[END]", StringComparison.OrdinalIgnoreCase)) break;
                    if (!_iconListCache.ContainsKey(name)) _iconListCache.Add(name, idx);
                    idx++;
                }
                if (_iconListCache.Count > 0) return;
            }
        }

        internal static string[] OriginalDataRootsForSiblingLoadersLikeOriginal()
        {
            return new[]
            {
                @"C:\GSC Game World\Cossacks II\Data",
                @"C:\GSC Game World\Cossacks II\Data1",
                @"C:\Program Files (x86)\GSC Game World\Cossacks II\Data",
                @"C:\Games\Cossacks II\Data",
                Path.Combine(Application.dataPath, "..", "Data"),
                Path.Combine(Application.dataPath, "..", "Cossacks2", "Data"),
                Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"),
                Path.Combine(Application.streamingAssetsPath, "Cossacks2"),
                Application.streamingAssetsPath,
                Path.Combine(Application.dataPath, "Resources"),
                Path.Combine(Application.dataPath, "Resources", "Data")
            };
        }

        public static string ResolveUiTextLikeOriginal(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            string k = key.Trim();
            string[] roots =
            {
                Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"),
                Path.Combine(Application.dataPath, "Resources"),
                @"C:\GSC Game World\Cossacks II\Data"
            };
            string[] files =
            {
                @"Text\dialogs.txt",
                @"Text\C2_interf07.txt",
                @"Text\DemoTEXT.txt",
                @"Text\textV0.txt",
                @"Text\textV1.txt",
                @"Text\textV2.txt",
                @"Text\textV3.txt",
                @"Text\text6.txt",
                @"Text\BigMapData.txt"
            };
            for (int r = 0; r < roots.Length; r++)
            {
                for (int f = 0; f < files.Length; f++)
                {
                    string path = Path.Combine(roots[r], files[f]);
                    if (!File.Exists(path)) continue;
                    string[] lines;
                    try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
                    catch { try { lines = File.ReadAllLines(path); } catch { continue; } }
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = (lines[i] ?? string.Empty).Trim();
                        if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal)) continue;
                        int sp = line.IndexOfAny(new[] { ' ', '\t' });
                        if (sp <= 0) continue;
                        string lk = line.Substring(0, sp).Trim();
                        if (string.Equals(lk, k, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(lk.TrimStart('#'), k.TrimStart('#'), StringComparison.OrdinalIgnoreCase))
                            return line.Substring(sp).Trim();
                    }
                }
            }
            return k;
        }

        internal static string CleanLineForSiblingLoadersLikeOriginal(string src)
        {
            return CleanLine(src);
        }

        internal static string[] SplitTokensForSiblingLoadersLikeOriginal(string line)
        {
            return SplitTokens(line);
        }

        private static string CleanLine(string src)
        {
            string s = (src ?? string.Empty).Trim();
            if (s.Length == 0) return string.Empty;
            if (s.StartsWith("//", StringComparison.Ordinal)) return string.Empty;
            int p = s.IndexOf("//", StringComparison.Ordinal);
            if (p >= 0) s = s.Substring(0, p).Trim();
            if (s.StartsWith("/", StringComparison.Ordinal)) return string.Empty;
            return s;
        }

        private static string[] SplitTokens(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return new string[0];
            return Regex.Split(line.Trim(), "\\s+");
        }

        private struct C2UpgradeDefV29
        {
            public string UpgradeId;
            public string IconName;
            public int IconSprite;
            public int X;
            public int Y;
            public string Source;
        }

        private struct C2ProduceRefV13
        {
            public string UnitId;
            public int X;
            public int Y;
            public char HotKey;
            public string Source;
        }

        internal struct C2MdIconInfoV13
        {
            public bool Building;
            public bool SelfTransform;
            public bool Peasant;
            public bool CommandCenter;
            public bool GlobalCommandCenter;
            public bool NoFarm;
            public int FarmCapacity;
            public int LifeMax;
            public int Damage0;
            public int Damage1;
            public int Damage2;
            public int Damage3;
            // COSSACKS2/NewMon.cpp experience/skill fields used by Brigade.cpp.
            public int SkillDamageMask;
            public int SkillDamageFormationBonus;
            public int SkillDamageFormationBonusStep;
            public int VeteranKills;
            public int ExpertKills;
            public int VeteranExtraDamage;
            public int ExpertExtraDamage;
            public int VeteranExtraShield;
            public int ExpertExtraShield;
            public string Weapon0;
            public string Weapon1;
            public string Weapon2;
            public string Weapon3;
            public int AttackRadius0Min;
            public int AttackRadius0;
            public int AttackRadius1Min;
            public int AttackRadius1;
            public int AttackRadius2Min;
            public int AttackRadius2;
            public int AttackPause0;
            public int AttackPause1;
            public int AttackPause2;
            public int AttackPause3;
            public int VisibleRadius1;
            public int VisibleRadius2;
            public int MaxGrenadesInFormation;
            public int GrenadeRechargeTime;
            public string BigWeaponFile;
            public string BigColdWeaponFile;
            public int BigColdWeaponSprite;
            public string BigFireWeaponFile;
            public int BigFireWeaponSprite;
            public int UnitAbsorber;
            public string MinIconFile;
            public int MinIconSprite;
            public string BigIconFile;
            public int BigIconSprite;
            public string InMenuIconFile;
            public int InMenuIconSprite;
            public bool HasPortBranch;
            public int PortBranch;
            public bool HasPortBackSprite;
            public int PortBackSprite;
            public bool HasIcon;
            public string IconName;
            public string IconFile;
            public int IconSprite;
            public bool HasExIcon;
            public string ExIconFile;
            public int ExIconSprite;
            public int ExIconEndSprite;
            public int ExIconStep;
            public int ExIconDx;
            public int ExIconDy;
            public string HotKey;
            public string NameKey;
            public string MessageKey;
            public string HintKey;
            public string Path;
        }
    }

    internal sealed class C2WeaponRangeScreenGraphicV160LikeOriginal : MaskableGraphic
    {
        private struct Poly
        {
            public Vector2 Center;
            public Vector2[] Points;
            public Color32 Color;
        }

        private readonly List<Poly> _polys = new List<Poly>(4);

        public void ClearPolygons()
        {
            _polys.Clear();
        }

        public void AddPolygon(Vector2 center, Vector2[] points, Color color)
        {
            if (points == null || points.Length < 3) return;
            Poly p;
            p.Center = center;
            p.Points = points;
            p.Color = color;
            _polys.Add(p);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            for (int pIndex = 0; pIndex < _polys.Count; pIndex++)
            {
                Poly p = _polys[pIndex];
                if (p.Points == null || p.Points.Length < 3) continue;

                int baseIndex = vh.currentVertCount;
                vh.AddVert(p.Center, p.Color, Vector2.zero);
                for (int i = 0; i < p.Points.Length; i++)
                    vh.AddVert(p.Points[i], p.Color, Vector2.zero);

                for (int i = 0; i < p.Points.Length; i++)
                {
                    int a = baseIndex;
                    int b = baseIndex + 1 + i;
                    int c = baseIndex + 1 + ((i + 1) % p.Points.Length);
                    vh.AddTriangle(a, b, c);
                }

                AddOutlineRing(vh, p.Points, new Color32(255, 96, 0, (byte)(p.Color.a > 100 ? 190 : 145)), 1.25f);
            }
        }

        private static void AddOutlineRing(VertexHelper vh, Vector2[] points, Color32 color, float width)
        {
            if (points == null || points.Length < 3 || width <= 0.0f) return;

            int n = points.Length;
            int baseIndex = vh.currentVertCount;

            for (int i = 0; i < n; i++)
            {
                Vector2 prev = points[(i - 1 + n) % n];
                Vector2 cur = points[i];
                Vector2 next = points[(i + 1) % n];

                Vector2 d1 = (cur - prev);
                Vector2 d2 = (next - cur);
                if (d1.sqrMagnitude > 0.0001f) d1.Normalize();
                if (d2.sqrMagnitude > 0.0001f) d2.Normalize();

                Vector2 n1 = new Vector2(-d1.y, d1.x);
                Vector2 n2 = new Vector2(-d2.y, d2.x);
                Vector2 normal = n1 + n2;
                if (normal.sqrMagnitude < 0.0001f) normal = n2;
                normal.Normalize();

                vh.AddVert(cur - normal * width, color, Vector2.zero);
                vh.AddVert(cur + normal * width, color, Vector2.zero);
            }

            for (int i = 0; i < n; i++)
            {
                int i0 = baseIndex + i * 2;
                int i1 = baseIndex + ((i + 1) % n) * 2;
                vh.AddTriangle(i0, i1, i0 + 1);
                vh.AddTriangle(i0 + 1, i1, i1 + 1);
            }
        }
    }

}
