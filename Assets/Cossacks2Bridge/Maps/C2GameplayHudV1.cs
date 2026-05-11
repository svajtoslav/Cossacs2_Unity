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
        private const string Contract = "V143_BUILDING_PRODUCE_RMB_CANCEL";
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
        private bool _lastBuildPlacementActive;
        private StringBuilder _spriteAudit = new StringBuilder(8192);
        private int _spriteAuditOrder;
        internal static C2SettlementBuildingSelectableV1LikeOriginal C2GameplayHudV133SelectedBuildingLikeOriginal;
        private static readonly Dictionary<string, Sprite> s_topSliceSpriteCacheV133LikeOriginal = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static Font _cachedRuntimeFont;

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
            Debug.Log("[C2:GAMEPLAY HUD V119] installed contract=" + Contract +
                      " original=SelPoint.DialogsDesk + va_SP_Bld_BigPortret + OISelection::SetProduce/SetUpgrade" +
                      " fix=building_ready_filter_bigicon_only");
        }

        private void Update()
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

            bool shouldShow = (unit != null || building != null) && battleCamera != null;

            if (!shouldShow)
            {
                if (_visible) SetVisible(false);
                _lastSelectedCount = -1;
                _lastBuildingSelectedCount = -1;
                _lastBuildingStateKey = string.Empty;
                _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
                _lastUnit = null;
                _lastBuilding = null;
                _lastBuildingProxy = null;
                C2GameplayHudV133SelectedBuildingLikeOriginal = null;
                if (battleCamera == null)
                    ConfigureScreenOverlay(null);
                return;
            }

            if (unit != null)
            {
                if (!_visible || _lastSelectedCount != count || _lastUnit != unit || _lastBuilding != null ||
                    _lastUnitSelPointStateKeyV137LikeOriginal != unitSelPointStateKey ||
                    _boundBattleCamera != battleCamera || _lastBuildPlacementActive != buildPlacementActive)
                {
                    C2GameplayHudV133SelectedBuildingLikeOriginal = null;
                    Rebuild(unit, count, unitSelPoints, activeUnitSelPointIndex);
                    _lastSelectedCount = count;
                    _lastBuildingSelectedCount = -1;
                    _lastBuildingStateKey = string.Empty;
                    _lastUnitSelPointStateKeyV137LikeOriginal = unitSelPointStateKey;
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
                if (!_visible || _lastBuildingSelectedCount != buildingCount || _lastBuilding != building || _lastBuildingStateKey != buildingStateKey || _lastUnit != null || _boundBattleCamera != battleCamera || _lastBuildPlacementActive != buildPlacementActive)
                {
                    C2GameplayHudV133SelectedBuildingLikeOriginal = building;
                    RebuildBuildingLikeOriginal(building, buildingCount);
                    _lastSelectedCount = -1;
                    _lastBuildingSelectedCount = buildingCount;
                    _lastBuildingStateKey = buildingStateKey;
                    _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
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

            _canvas.enabled = _visible || _root == null || _root.childCount > 0;
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
                raycaster.enabled = _visible || (_root != null && _root.childCount > 0);

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
            if (_canvas != null)
            {
                ConfigureScreenOverlay(FindBattleCamera());
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
            if (hasMultipleSelPoints)
                BuildSelectedUnitSelPointSideCardsV137LikeOriginal(unitSelPoints, activeUnitSelPointIndex, true);

            BuildSelectedUnitLeftCardLikeOriginal(unit, selectedCount, activeOffsetX);

            if (hasMultipleSelPoints)
                BuildSelectedUnitSelPointSideCardsV137LikeOriginal(unitSelPoints, activeUnitSelPointIndex, false);

            // Original gameplay BuildMode keeps the selected builder portrait, but while a building is already
            // on the cursor the produce list is no longer shown. Do not draw the building buttons/click areas
            // until BuildMode is cancelled or the foundation is placed.
            if (C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal)
            {
                _lastProduceAudit = "hidden_during_buildmode_cursor_object_like_original";
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
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();

            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled || !u.IsSelected) continue;

                string key = UnitSelPointKeyV137LikeOriginal(u);
                if (string.IsNullOrEmpty(key)) continue;

                C2SelectedUnitSelPointV137LikeOriginal sp;
                if (!byKey.TryGetValue(key, out sp))
                {
                    C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u);
                    sp = new C2SelectedUnitSelPointV137LikeOriginal();
                    sp.Key = key;
                    sp.Unit = u;
                    sp.Count = 0;
                    sp.NIndex = u.NIndex;
                    sp.Peasant = icon.Peasant;
                    sp.Icon = icon;
                    sp.Title = ResolveUnitTitleLikeOriginal(sp.Icon, u);
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

                if (sp.Unit == null || u.SortKey < sp.Unit.SortKey)
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
                    BuildSelectedUnitSelPointSideCardV137LikeOriginal(groups[i], i, true);
                }
            }
            else
            {
                for (int i = activeIndex + 1; i < groups.Count; i++)
                    BuildSelectedUnitSelPointSideCardV137LikeOriginal(groups[i], i, false);
            }
        }

        private void BuildSelectedUnitSelPointSideCardV137LikeOriginal(C2SelectedUnitSelPointV137LikeOriginal group, int index, bool leftOfActive)
        {
            if (group == null || group.Unit == null) return;

            int slotX = index * OriginalSelPointSideWidthV137LikeOriginal;
            int y = OriginalSelPointSideYV137LikeOriginal;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon = group.Icon;
            int backSprite = ResolveBranchColorSpriteLikeOriginal(icon);
            int nameCircleSprite = icon.Peasant ? 10 : 9; // va_SP_NameCircleSide: base 8, normal +1, peasant +2.

            if (leftOfActive)
            {
                AddG16Image("sp_side_left_color_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\SelColorLeft", backSprite, slotX, y, 49, 263, 255, false, false, false);
                if (!string.IsNullOrEmpty(icon.BigIconFile))
                    AddG16Image("sp_side_left_unit_" + index.ToString(CultureInfo.InvariantCulture), icon.BigIconFile, icon.BigIconSprite, slotX - 8, y + 26, 139, 237, 115, false, false, false);
                AddG16Image("sp_side_left_frame_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 1, slotX, y, 49, 276, 255, false, false, false);
                AddG16Image("sp_side_left_name_circle_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", nameCircleSprite, slotX + 8, y + 68, 21, 153, 255, false, false, false);
                AddG16Image("sp_side_left_amount_plate_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 3, slotX + 9, y + 9, 19, 36, 255, false, false, false);
                AddLabel("sp_side_left_amount_" + index.ToString(CultureInfo.InvariantCulture), group.Count.ToString(CultureInfo.InvariantCulture), slotX + 12, y + 22, 14, 9, 8, TextAnchor.MiddleCenter, Color.white);
                AddRotatedLabel("sp_side_left_name_" + index.ToString(CultureInfo.InvariantCulture), group.Title, slotX + 13, y + 207, 116, 11, 9, true);
            }
            else
            {
                AddG16Image("sp_side_right_color_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\SelColorRight", backSprite, slotX + 100, y, 26, 263, 255, false, false, false);
                if (!string.IsNullOrEmpty(icon.BigIconFile))
                    AddG16Image("sp_side_right_unit_" + index.ToString(CultureInfo.InvariantCulture), icon.BigIconFile, icon.BigIconSprite, slotX + 19, y + 26, 139, 237, 115, false, false, false);
                AddG16Image("sp_side_right_frame_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 2, slotX + 100, y, 49, 283, 255, false, false, false);
                AddG16Image("sp_side_right_name_circle_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", nameCircleSprite, slotX + 120, y + 68, 21, 153, 255, false, false, false);
                AddG16Image("sp_side_right_amount_plate_" + index.ToString(CultureInfo.InvariantCulture), "Interf3\\cropped", 3, slotX + 121, y + 9, 19, 36, 255, false, false, false);
                AddLabel("sp_side_right_amount_" + index.ToString(CultureInfo.InvariantCulture), group.Count.ToString(CultureInfo.InvariantCulture), slotX + 124, y + 22, 14, 9, 8, TextAnchor.MiddleCenter, Color.white);
                AddRotatedLabel("sp_side_right_name_" + index.ToString(CultureInfo.InvariantCulture), group.Title, slotX + 126, y + 207, 116, 11, 9, true);
            }

            AddUnitSelPointSideClickAreaV137LikeOriginal(
                "sp_side_click_" + index.ToString(CultureInfo.InvariantCulture),
                leftOfActive ? slotX : slotX + 100,
                y,
                leftOfActive ? OriginalSelPointSideWidthV137LikeOriginal : 49,
                leftOfActive ? 276 : 283,
                group.Key);
        }

        private void OnUnitSelPointSideClickedV137LikeOriginal(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
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
            // Original va_SP_Morale reads SP->Inf.Units.Morale.
            // The current gameplay bridge does not expose that field yet. Baseline peasant morale in the original UI is 50/50.
            return 50;
        }

        private static int ResolveMoraleMaxLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            // Original va_SP_Morale reads SP->Inf.Units.MoraleMax.
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
            if (selectedCount <= 1)
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

                string countText = selectedCount.ToString(CultureInfo.InvariantCulture);
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

            // 0 top-right = kills counter. Bottom shield is decorative; protect/defence value is on the lower FormInterface 24 plate.
            AddCrispLabelV140LikeOriginal("sp_kill_counter_zero", "0", baseX + 137, baseY + 49, 18, 12, 10, TextAnchor.MiddleCenter, Color.white);
            AddCrispLabelV140LikeOriginal("sp_defence_zero", "0", baseX + 143, baseY + 259, 7, 10, 9, TextAnchor.MiddleCenter, Color.white);
            AddCrispLabelV140LikeOriginal("sp_morale_text", moraleText, baseX + 55, baseY + 272, 71, 15, 9, TextAnchor.MiddleCenter, Color.white);
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

                // Same clone source as original: vxProduceGP -> UnitProduce.GPPicture.Dialogs.xml.
                // va_Unit_P_Box changes the root sprite between 22/21; for build buttons the inactive slot usually uses 21.
                AddG16ImageOverpaintV140LikeOriginal("produce_cell_back_" + i.ToString(CultureInfo.InvariantCulture), "Interf3\\FormInterface", item.RootSpriteId, x, y, OriginalUnitProduceWidth, OriginalUnitProduceHeight, 255, false, 56);
                AddG16ImageOverpaintV140LikeOriginal("produce_icon_" + i.ToString(CultureInfo.InvariantCulture), item.IconFileId, item.IconSpriteId,
                            x + OriginalUnitProduceIconX, y + OriginalUnitProduceIconY, OriginalUnitProduceIconW, OriginalUnitProduceIconH,
                            item.Enabled ? 255 : 128, false, item.Enabled ? 120 : 48, true);

                if (!item.Enabled)
                    AddSolid("produce_disabled_" + i.ToString(CultureInfo.InvariantCulture), new Color(0f, 0f, 0f, 0.48f), x, y, 57, 123, false);

                AddClickArea("produce_click_" + i.ToString(CultureInfo.InvariantCulture), x, y, OriginalUnitProduceWidth, OriginalUnitProduceHeight, item);
            }
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
            // Port of VUI_Actions.cpp SetMorale(Canvas*, Morale, MoraleMax):
            // n = Morale / 100, m = Morale % 100, M = clamp(MoraleMax - n*100, 0..100).
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

            if (lr > 0)
                AddSolid("sp_morale_line_red_original", new Color(0.99f, 0.16f, 0.16f, 1.0f), x, y, lr, h, false);
            if (lx > lr)
                AddSolid("sp_morale_line_yellow_original", new Color(0.99f, 0.77f, 0.03f, 1.0f), x + lr, y, lx - lr, h, false);
            if (lMax > lx)
                AddSolid("sp_morale_line_yellow_max_original", new Color(0.99f, 0.77f, 0.03f, 0.56f), x + lx, y, lMax - lx, h, false);

            if (n > 0)
            {
                int tickW = Mathf.Max(1, h - 1);
                int start = (w - (n + n - 1) * tickW) / 2;
                for (int i = 0; i < n; i++)
                {
                    int xx = x + start + i * 2 * tickW;
                    AddSolid("sp_morale_line_tick_" + i.ToString(CultureInfo.InvariantCulture), new Color(0.69f, 0.0f, 0.0f, 1.0f), xx, y, tickW, h, false);
                }
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

        private Text AddRotatedLabel(string name, string text, int x, int y, int w, int h, int fontSize, bool clockwise)
        {
            Text t = AddLabel(name, text ?? string.Empty, x, y, w, h, fontSize, TextAnchor.MiddleCenter, Color.white);
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
                Camera cam = visible ? FindBattleCamera() : null;
                ConfigureScreenOverlay(cam);
                _canvas.enabled = visible && cam != null;
            }

            if (!visible)
            {
                GraphicRaycaster raycaster = _canvas != null ? _canvas.GetComponent<GraphicRaycaster>() : null;
                if (raycaster != null) raycaster.enabled = false;
            }
        }

        private void ClearSpawned()
        {
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

            if (Owner != null)
                Owner.OnProduceRightClickedV143LikeOriginal(Item);
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

    internal sealed class C2OriginalProduceItemV13
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
        private static readonly Dictionary<string, List<C2ProduceRefV13>> _fixedProduce = new Dictionary<string, List<C2ProduceRefV13>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2UpgradeDefV29> _upgradeDefsV29 = new Dictionary<string, C2UpgradeDefV29>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> _upgradePlacesV29 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2MdIconInfoV13> _mdCache = new Dictionary<string, C2MdIconInfoV13>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdPathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdListNamesV141 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdListHintNamesV141 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static int _mdListFilesV141;
        private static int _mdListDirectNamesV141;
        private static int _mdListHintNamesCountV141;


        public static void ForceReloadLikeOriginal()
        {
            _loaded = false;
            _audit = "not_loaded";
            _memberToMd.Clear();
            _mdToMember.Clear();
            _fixedProduce.Clear();
            _upgradeDefsV29.Clear();
            _upgradePlacesV29.Clear();
            _mdCache.Clear();
            _mdPathCache.Clear();
            _mdListNamesV141.Clear();
            _mdListHintNamesV141.Clear();
            _mdListFilesV141 = 0;
            _mdListDirectNamesV141 = 0;
            _mdListHintNamesCountV141 = 0;
            _iconListLoaded = false;
            _iconListCache.Clear();
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
                        members++;
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
                    else if (cmd == "LIFE" && t.Length >= 2)
                    {
                        int v;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                            info.LifeMax = Mathf.Max(0, v);
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
            public int LifeMax;
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
}
