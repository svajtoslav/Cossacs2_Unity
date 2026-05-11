// C2BuildingPlacementPreviewV27.cs
// V33: construction placement preview with composite MD ghost and runtime construction creation.
// It consumes HUD produce building clicks, parses original MD build zones, draws #STANDLO/#BUILDLO through the
// same composite renderer as saved map buildings, then creates a staged construction site on confirm.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2BuildingPlacementPreviewV27 : MonoBehaviour
    {
        private const string Contract = "V44_BUILDERSNAPSHOT_KEEP_SELECTED_WORKERS";
        private const int MotionCellOriginalPixels = 16;
        private const float PreviewYOffset = 0.12f;
        private const float GhostYOffset = 0.16f;
        private const bool VerboseHoverLogLikeOriginal = false;

        private static C2BuildingPlacementPreviewV27 s_Instance;

        private bool _active;
        private string _unitId = string.Empty;
        private string _mdName = string.Empty;
        private int _nation;
        private string _builderId = string.Empty;
        private string _builderMd = string.Empty;
        private string _source = string.Empty;
        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _builderSnapshotV44 = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);

        private C2BuildMdInfoV27 _buildMd;
        private C2WorkerBuildMdInfoV27 _workerMd;

        private GameObject _root;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private Material _validMat;
        private Material _invalidMat;
        private GameObject _ghostRoot;
        private MeshFilter _ghostMeshFilter;
        private MeshRenderer _ghostMeshRenderer;
        private Mesh _ghostMesh;
        private Material _ghostValidMat;
        private Material _ghostInvalidMat;
        private GameObject _checkOverlayRoot;
        private MeshFilter _checkOverlayMeshFilter;
        private MeshRenderer _checkOverlayMeshRenderer;
        private Mesh _checkOverlayMesh;
        private Material _checkOverlayMat;
        private Texture2D _ghostTexture;
        private C2AnimFrameRefV28 _ghostFrame;
        private string _ghostAudit = "not_loaded";
        private bool _ghostCompositeReady;
        private int _ghostCompositeRealX;
        private int _ghostCompositeRealY;
        private bool _ghostCompositeValid;
        private string _ghostCompositeAudit = "not_started";
        private string _ghostCompositeMdName = string.Empty;
        private int _ghostCompositeNation = int.MinValue;

        private C2BattleTerrainMode _mode;
        private float _nextModeLookup;
        private float _nextHoverLog;
        private string _lastHoverSig = string.Empty;
        private bool _lastValid;
        private int _lastAnchorCellX;
        private int _lastAnchorCellY;
        private int _lastFootprintCellX;
        private int _lastFootprintCellY;
        private int _lastRealX;
        private int _lastRealY;
        private bool _lastSmartSnapped;
        private bool _ignoreLeftUntilRelease;
        private readonly List<Bounds> _runtimeBuildingBounds = new List<Bounds>(256); // legacy fallback, V38 prefers original BUILDBAR cache.
        private readonly List<C2BuildBarAreaV38> _runtimeBuildingBuildBarsV38 = new List<C2BuildBarAreaV38>(256);
        private readonly List<Bounds> _runtimeRoadBounds = new List<Bounds>(256); // legacy; V37 uses original road-cell cache instead of per-triangle renderer AABBs.
        private readonly HashSet<long> _runtimeRoadCellsV37 = new HashSet<long>();
        private object _runtimeRoadCellsMapV37;
        private int _runtimeRoadCellsSegmentsV37;
        private int _runtimeRoadCellsBuildsV37;
        private float _nextRuntimeBlockerCacheRefresh;
        private int _runtimeBlockerCacheVersion;
        private bool _hasPlacementCache;
        private int _placementCacheFootprintCellX;
        private int _placementCacheFootprintCellY;
        private int _placementCacheSnapCellX;
        private int _placementCacheSnapCellY;
        private int _placementCacheBlockerVersion;
        private C2PlacementCheckV27 _placementCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (s_Instance != null) return;
            GameObject go = new GameObject("C2_BuildingPlacementPreview_V27");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<C2BuildingPlacementPreviewV27>();
        }

        public static void RequestBuildPreviewLikeOriginal(
            string unitId,
            string mdName,
            int nation,
            string builderId,
            string builderMd,
            string source)
        {
            if (s_Instance == null) AutoInstall();
            if (s_Instance == null) return;
            s_Instance.BeginPreview(unitId, mdName, nation, builderId, builderMd, source);
        }


        public static bool C2BuildPlacementActiveLikeOriginal
        {
            get { return s_Instance != null && s_Instance._active; }
        }

        public static string C2BuildPlacementAuditLikeOriginal
        {
            get
            {
                if (s_Instance == null || !s_Instance._active) return "inactive";
                return "active unit='" + s_Instance._unitId + "' md='" + s_Instance._mdName + "' valid=" + s_Instance._lastValid +
                       " reason='" + (s_Instance._placementCache.Reason ?? string.Empty) + "'";
            }
        }

        public static void C2BuildPlacementRefreshDebugOverlayLikeOriginal()
        {
            if (s_Instance != null)
                s_Instance.UpdateCheckpointsDebugOverlayV58LikeOriginal();
        }

        private void Awake()
        {
            s_Instance = this;
            EnsurePreviewObjects();
            SetPreviewVisible(false);
            Debug.Log("[C2:BUILD PREVIEW V44] installed contract=" + Contract +
                      " original=va_Unit_P_Box::LeftClick -> ShowBuildingPreview/CheckSmartCreationAbility/CmdCreateBuilding" +
                      " mode=composite_ghost_and_runtime_construction");
        }

        private void Update()
        {
            ConsumeLegacyHudStaticRequestLikeOriginal();

            if (!_active)
                return;

            if (CancelPressedThisFrameLikeOriginal())
            {
                Debug.Log("[C2:BUILD PREVIEW V44 CANCEL] unit='" + _unitId + "' md='" + _mdName + "'");
                StopPreview();
                return;
            }

            UpdatePreviewUnderMouseLikeOriginal();

            if (LeftPressedThisFrameLikeOriginal())
            {
                if (_ignoreLeftUntilRelease)
                    return;
                ConfirmConstructionLikeOriginal();
            }
            else if (_ignoreLeftUntilRelease && !LeftHeldLikeOriginal())
            {
                _ignoreLeftUntilRelease = false;
            }
        }

        private int CaptureSelectedBuildersSnapshotV44LikeOriginal()
        {
            _builderSnapshotV44.Clear();

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled || !u.IsSelected || !u.CanReceiveOrdersLikeOriginal())
                    continue;
                if (!_builderSnapshotV44.Contains(u))
                    _builderSnapshotV44.Add(u);
            }

            return _builderSnapshotV44.Count;
        }

        private void BeginPreview(string unitId, string mdName, int nation, string builderId, string builderMd, string source)
        {
            ClearGhostCompositeLikeOriginal();

            _unitId = unitId ?? string.Empty;
            _mdName = mdName ?? string.Empty;
            _nation = nation;
            _builderId = builderId ?? string.Empty;
            _builderMd = builderMd ?? string.Empty;
            _source = source ?? string.Empty;

            _buildMd = C2BuildMdInfoV27.Parse(_mdName);
            _workerMd = C2WorkerBuildMdInfoV27.Parse(_builderMd);
            int builderSnapshotCountV44 = CaptureSelectedBuildersSnapshotV44LikeOriginal();
            LoadGhostTextureLikeOriginal();

            _active = true;
            _ignoreLeftUntilRelease = LeftHeldLikeOriginal();
            _lastHoverSig = string.Empty;
            _nextHoverLog = 0.0f;
            _hasPlacementCache = false;
            RefreshRuntimeBlockerCacheLikeOriginal(true);

            EnsurePreviewObjects();
            SetPreviewVisible(true);
            C2GameplayHudV1.C2GameplayHudV28InvalidateBuildModeLikeOriginal();

            C2GameplayHudV1.C2GameplayHudV13PlacementRequestedLikeOriginal = false;

            Debug.Log("[C2:BUILD PREVIEW V44 START] unit='" + _unitId +
                      "' md='" + _mdName +
                      "' nation=" + _nation.ToString(CultureInfo.InvariantCulture) +
                      " builder='" + _builderId +
                      "' builderMd='" + _builderMd +
                      "' source='" + _source +
                      "' mdFound=" + _buildMd.Found +
                      " mdPath='" + _buildMd.MdPath +
                      "' package='" + _buildMd.Package +
                      "' buildStages=" + _buildMd.BuildStages.ToString(CultureInfo.InvariantCulture) +
                      " buildBar=" + _buildMd.BuildBarAudit +
                      " lockPoints=" + _buildMd.LockPoints.Count.ToString(CultureInfo.InvariantCulture) +
                      " checkPoints=" + _buildMd.CheckPoints.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildLockPoints=" + _buildMd.BuildLockPoints.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildPoints=" + _buildMd.BuildPoints.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildLo=" + _buildMd.BuildLoAudit +
                      " standLoFrames=" + _buildMd.StandLoFrameCount.ToString(CultureInfo.InvariantCulture) +
                      " rectangle=" + _buildMd.RectangleAudit +
                      " ghost=" + _ghostAudit +
                      " ghostFrame=" + _ghostFrame.Audit +
                      " workerMdFound=" + _workerMd.Found +
                      " workerMdPath='" + _workerMd.MdPath +
                      "' workerWork=" + _workerMd.WorkAudit +
                      " builderSnapshot=" + builderSnapshotCountV44.ToString(CultureInfo.InvariantCulture) +
                      " transfer=V44_buildersnapshot_keeps_selection_for_BuildWithSelected");
        }

        private void ConfirmConstructionLikeOriginal()
        {
            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            int hoverRealX = _lastRealX;
            int hoverRealY = _lastRealY;

            if (mode == null)
            {
                Debug.LogWarning("[C2:BUILD PREVIEW V44 CONFIRM REJECT] unit='" + _unitId +
                                 "' md='" + _mdName +
                                 "' reason=no_C2BattleTerrainMode");
                return;
            }

            // V66:
            // Confirm must use the exact point that the player clicked.
            // Smart-search on confirm could create a building near/through a forbidden area even when hover was red
            // (water/building/road/field). Original user-facing behaviour here: forbidden click creates nothing.
            C2PlacementCheckV27 confirm = CheckPlacementAtRealLikeOriginal(mode, hoverRealX, hoverRealY, false);
            _placementCache = confirm;
            _lastValid = confirm.Valid;
            _lastAnchorCellX = confirm.AnchorCellX;
            _lastAnchorCellY = confirm.AnchorCellY;
            _lastFootprintCellX = confirm.FootprintCellX;
            _lastFootprintCellY = confirm.FootprintCellY;
            _lastRealX = confirm.RealX;
            _lastRealY = confirm.RealY;
            _lastSmartSnapped = confirm.SmartSnapped;

            int realX = confirm.RealX;
            int realY = confirm.RealY;

            if (!confirm.Valid)
            {
                Debug.Log("[C2:BUILD PREVIEW V44 CONFIRM REJECT] unit='" + _unitId +
                          "' md='" + _mdName +
                          "' anchorCell=" + confirm.AnchorCellX.ToString(CultureInfo.InvariantCulture) + "/" + confirm.AnchorCellY.ToString(CultureInfo.InvariantCulture) +
                          " footprintCell=" + confirm.FootprintCellX.ToString(CultureInfo.InvariantCulture) + "/" + confirm.FootprintCellY.ToString(CultureInfo.InvariantCulture) +
                          " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " smartSnapped=" + confirm.SmartSnapped +
                          " reason='" + (confirm.Reason ?? "CheckPlacementLikeOriginal_false") + "'" +
                          " blockedCells=" + confirm.BlockedCells.ToString(CultureInfo.InvariantCulture) +
                          " roadHits=" + confirm.RoadHits.ToString(CultureInfo.InvariantCulture) +
                          " buildingHits=" + confirm.BuildingHits.ToString(CultureInfo.InvariantCulture) +
                          " mapBoundsHits=" + confirm.MapBoundsHits.ToString(CultureInfo.InvariantCulture) +
                          " stonesOrOre=" + confirm.StoneOrOreHits.ToString(CultureInfo.InvariantCulture) +
                          " food=" + confirm.FoodHits.ToString(CultureInfo.InvariantCulture) +
                          " woodEraseOk=" + confirm.WoodHits.ToString(CultureInfo.InvariantCulture));
                return;
            }

            GameObject site;
            string createAudit;
            bool created = mode.C2BuildRuntimeCreateConstructionLikeOriginal(
                _mdName,
                string.IsNullOrEmpty(_unitId) ? _mdName : _unitId,
                _nation,
                realX,
                realY,
                "placement-confirm-v44",
                out site,
                out createAudit);

            string eraseAudit = "not_run";
            if (created)
            {
                IList<Vector2Int> erasePts = _buildMd != null && _buildMd.BuildLockPoints != null && _buildMd.BuildLockPoints.Count > 0
                    ? (IList<Vector2Int>)_buildMd.BuildLockPoints
                    : (_buildMd != null && _buildMd.LockPoints != null ? (IList<Vector2Int>)_buildMd.LockPoints : null);

                int eraseRadius = Mathf.Max(1, Mathf.CeilToInt((_buildMd != null ? _buildMd.BRadius : 8) * 0.10f));
                mode.C2BuildRuntimeErasePlacedFoundationAreaLikeOriginal(
                    confirm.FootprintCellX,
                    confirm.FootprintCellY,
                    erasePts,
                    eraseRadius,
                    "placement-confirm-v44",
                    out eraseAudit);
            }

            int assigned = 0;
            string assignAudit = "not_run";
            if (created)
                assigned = mode.C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(site, realX, realY, _builderSnapshotV44, "placement-confirm-v44", out assignAudit);

            Debug.Log("[C2:BUILD PREVIEW V44 CONFIRM] unit='" + _unitId +
                      "' md='" + _mdName +
                      "' valid=" + confirm.Valid +
                      " anchorCell=" + confirm.AnchorCellX.ToString(CultureInfo.InvariantCulture) + "/" + confirm.AnchorCellY.ToString(CultureInfo.InvariantCulture) +
                      " footprintCell=" + confirm.FootprintCellX.ToString(CultureInfo.InvariantCulture) + "/" + confirm.FootprintCellY.ToString(CultureInfo.InvariantCulture) +
                      " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " smartSnapped=" + confirm.SmartSnapped +
                      " created=" + created +
                      " buildersAssigned=" + assigned.ToString(CultureInfo.InvariantCulture) +
                      " createAudit=[" + createAudit + "]" +
                      " eraseAudit=[" + eraseAudit + "]" +
                      " assignAudit=[" + assignAudit + "]");

            if (created && !ShiftHeldLikeOriginal())
                StopPreview();
        }


        private void ConsumeLegacyHudStaticRequestLikeOriginal()
        {
            if (!C2GameplayHudV1.C2GameplayHudV13PlacementRequestedLikeOriginal)
                return;

            string unitId = C2GameplayHudV1.C2GameplayHudV13SelectedBuildUnitIdLikeOriginal ?? string.Empty;
            string md = C2GameplayHudV1.C2GameplayHudV13SelectedBuildMdLikeOriginal ?? string.Empty;
            int ni = C2GameplayHudV1.C2GameplayHudV13SelectedBuildNationLikeOriginal;

            C2GameplayHudV1.C2GameplayHudV13PlacementRequestedLikeOriginal = false;

            if (string.IsNullOrEmpty(md) && string.IsNullOrEmpty(unitId))
                return;

            BeginPreview(unitId, md, ni, string.Empty, string.Empty, "legacy_static_request");
        }

        private void UpdatePreviewUnderMouseLikeOriginal()
        {
            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            if (mode == null)
            {
                SetPreviewVisible(false);
                return;
            }

            Vector3 world;
            Camera usedCamera;
            if (!TryMouseWorldLikeOriginal(out world, out usedCamera))
            {
                SetPreviewVisible(false);
                return;
            }

            float oxFloat;
            float oyFloat;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(world, out oxFloat, out oyFloat))
            {
                SetPreviewVisible(false);
                return;
            }

            int mouseOx = Mathf.RoundToInt(oxFloat);
            int mouseOy = Mathf.RoundToInt(oyFloat);
            int mouseRealX = mouseOx << 4;
            int mouseRealY = mouseOy << 4;

            int minLocalX;
            int maxLocalX;
            int minLocalY;
            int maxLocalY;
            _buildMd.GetPreviewBounds(out minLocalX, out maxLocalX, out minLocalY, out maxLocalY);

            C2PlacementCheckV27 check = CachedCheckPlacementLikeOriginal(mode, mouseRealX, mouseRealY, false);
            _lastValid = check.Valid;
            _lastAnchorCellX = check.AnchorCellX;
            _lastAnchorCellY = check.AnchorCellY;
            _lastFootprintCellX = check.FootprintCellX;
            _lastFootprintCellY = check.FootprintCellY;
            _lastRealX = check.RealX;
            _lastRealY = check.RealY;
            _lastSmartSnapped = check.SmartSnapped;

            UpdatePreviewMeshLikeOriginal(mode, check.FootprintCellX, check.FootprintCellY, minLocalX, maxLocalX, minLocalY, maxLocalY, check.Valid);
            UpdateGhostMeshLikeOriginal(mode, check.RealX, check.RealY, check.Valid);
            UpdateCheckpointsDebugOverlayV58LikeOriginal();

            string sig = _mdName + "|" + check.RealX.ToString(CultureInfo.InvariantCulture) + "|" + check.RealY.ToString(CultureInfo.InvariantCulture) + "|" + check.Valid + "|" + (check.Reason ?? string.Empty);
            if (VerboseHoverLogLikeOriginal && (Time.realtimeSinceStartup >= _nextHoverLog || sig != _lastHoverSig))
            {
                _nextHoverLog = Time.realtimeSinceStartup + 0.35f;
                _lastHoverSig = sig;

                Debug.Log("[C2:BUILD PREVIEW V44 HOVER] unit='" + _unitId +
                          "' md='" + _mdName +
                          "' mouseOriginal=" + mouseOx.ToString(CultureInfo.InvariantCulture) + "/" + mouseOy.ToString(CultureInfo.InvariantCulture) +
                          " anchorCell=" + check.AnchorCellX.ToString(CultureInfo.InvariantCulture) + "/" + check.AnchorCellY.ToString(CultureInfo.InvariantCulture) +
                          " footprintCell=" + check.FootprintCellX.ToString(CultureInfo.InvariantCulture) + "/" + check.FootprintCellY.ToString(CultureInfo.InvariantCulture) +
                          " real=(" + check.RealX.ToString(CultureInfo.InvariantCulture) + "," + check.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " smartSnapped=" + check.SmartSnapped +
                          " localBounds=" + minLocalX.ToString(CultureInfo.InvariantCulture) + "," + minLocalY.ToString(CultureInfo.InvariantCulture) +
                          ".." + maxLocalX.ToString(CultureInfo.InvariantCulture) + "," + maxLocalY.ToString(CultureInfo.InvariantCulture) +
                          " valid=" + check.Valid +
                          " reason='" + (check.Reason ?? string.Empty) + "'" +
                          " blockedCells=" + check.BlockedCells.ToString(CultureInfo.InvariantCulture) +
                          " roadHits=" + check.RoadHits.ToString(CultureInfo.InvariantCulture) +
                          " buildingHits=" + check.BuildingHits.ToString(CultureInfo.InvariantCulture) +
                          " stonesOrOre=" + check.StoneOrOreHits.ToString(CultureInfo.InvariantCulture) +
                          " foodOrComplex=" + check.FoodHits.ToString(CultureInfo.InvariantCulture) +
                          " treesEraseLikeOriginal=" + check.WoodHits.ToString(CultureInfo.InvariantCulture) +
                          " resourceAudit='" + check.ResourceAudit +
                          "' ghostComposite='" + _ghostCompositeAudit +
                          "' camera='" + (usedCamera != null ? usedCamera.name : "<none>") +
                          "'");
            }
            else if (!VerboseHoverLogLikeOriginal && sig != _lastHoverSig && Time.realtimeSinceStartup >= _nextHoverLog)
            {
                _nextHoverLog = Time.realtimeSinceStartup + 0.25f;
                _lastHoverSig = sig;
                Debug.Log("[C2:BUILD PREVIEW V44 HOVER STATE] unit='" + _unitId +
                          "' md='" + _mdName +
                          "' valid=" + check.Valid +
                          " reason='" + (check.Reason ?? string.Empty) +
                          "' real=(" + check.RealX.ToString(CultureInfo.InvariantCulture) + "," + check.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " smartSnapped=" + check.SmartSnapped +
                          " tint=" + (check.Valid ? "0xFFFFFFFF" : "0x80FF0000"));
            }
        }

        private C2PlacementCheckV27 CachedCheckPlacementLikeOriginal(C2BattleTerrainMode mode, int realX, int realY, bool allowSmartSnap)
        {
            RefreshRuntimeBlockerCacheLikeOriginal(false);

            int footprintCellX = (realX + (_buildMd.PicDx << 4)) >> 8;
            int footprintCellY = (realY + (_buildMd.PicDy << 5)) >> 8;
            int snapCellX = realX >> 8;
            int snapCellY = realY >> 8;

            if (_hasPlacementCache &&
                _placementCacheFootprintCellX == footprintCellX &&
                _placementCacheFootprintCellY == footprintCellY &&
                _placementCacheSnapCellX == snapCellX &&
                _placementCacheSnapCellY == snapCellY &&
                _placementCacheBlockerVersion == _runtimeBlockerCacheVersion)
            {
                C2PlacementCheckV27 cached = _placementCache;
                if (!cached.SmartSnapped)
                {
                    cached.RealX = realX;
                    cached.RealY = realY;
                    cached.AnchorCellX = snapCellX;
                    cached.AnchorCellY = snapCellY;
                    cached.FootprintCellX = footprintCellX;
                    cached.FootprintCellY = footprintCellY;
                }
                return cached;
            }

            C2PlacementCheckV27 check = allowSmartSnap ? CheckPlacementLikeOriginal(mode, realX, realY) : CheckPlacementAtRealLikeOriginal(mode, realX, realY, false);
            _placementCache = check;
            _placementCacheFootprintCellX = footprintCellX;
            _placementCacheFootprintCellY = footprintCellY;
            _placementCacheSnapCellX = snapCellX;
            _placementCacheSnapCellY = snapCellY;
            _placementCacheBlockerVersion = _runtimeBlockerCacheVersion;
            _hasPlacementCache = true;
            return check;
        }

        private C2PlacementCheckV27 CheckPlacementLikeOriginal(C2BattleTerrainMode mode, int realX, int realY)
        {
            C2PlacementCheckV27 initial = CheckPlacementAtRealLikeOriginal(mode, realX, realY, false);
            if (initial.Valid)
                return initial;

            int snapX = (realX >> 8) << 8;
            int snapY = (realY >> 8) << 8;
            for (int radius = 1; radius < 10; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                            continue;

                        C2PlacementCheckV27 candidate = CheckPlacementAtRealLikeOriginal(mode, snapX + (dx << 8), snapY + (dy << 8), true);
                        if (!candidate.Valid)
                            continue;

                        candidate.SmartSnapped = dx != 0 || dy != 0 || candidate.RealX != realX || candidate.RealY != realY;
                        candidate.SmartDx = dx;
                        candidate.SmartDy = dy;
                        return candidate;
                    }
                }
            }

            return initial;
        }

        private C2PlacementCheckV27 CheckPlacementAtRealLikeOriginal(C2BattleTerrainMode mode, int realX, int realY, bool smartProbe)
        {
            C2PlacementCheckV27 r = new C2PlacementCheckV27();
            r.Valid = true;
            r.RealX = realX;
            r.RealY = realY;
            r.AnchorCellX = realX >> 8;
            r.AnchorCellY = realY >> 8;
            r.FootprintCellX = (realX + (_buildMd.PicDx << 4)) >> 8;
            r.FootprintCellY = (realY + (_buildMd.PicDy << 5)) >> 8;
            r.SmartProbe = smartProbe;
            r.HeightMin = int.MaxValue;
            r.HeightMax = int.MinValue;
            r.CheckMinCellX = int.MaxValue;
            r.CheckMinCellY = int.MaxValue;
            r.CheckMaxCellX = int.MinValue;
            r.CheckMaxCellY = int.MinValue;
            r.HeightMinCellX = int.MinValue;
            r.HeightMinCellY = int.MinValue;
            r.HeightMaxCellX = int.MinValue;
            r.HeightMaxCellY = int.MinValue;

            if (mode != null && !mode.C2OriginalResourceMapV1IsReadyLikeOriginal())
                mode.C2OriginalResourceMapV1TryBuildLikeOriginal("build-preview-v38");

            // Original CheckCreationAbility first checks only the anchor coordinate:
            // xs=x2>>9; ys=y2>>9; if(xs<=0||ys<=0||xs>=msx||ys>=msy)return -1;
            // V37 incorrectly rejected by full visual/footprint bounds. Do not do that here.
            ApplyOriginalAnchorMapBoundsLikeOriginal(mode, realX, realY, ref r);

            RefreshOriginalRoadCellCacheLikeOriginal(mode, false);

            StringBuilder resAudit = VerboseHoverLogLikeOriginal ? new StringBuilder(512) : null;

            // Original sprites/resources check uses center+BRadius and Erase=true. We approximate it from
            // CHECKPOINTS/BUILDLOCKPOINTS cells: TREE/wood is erasable, FOOD/field blocks, stone/ore is audit-only.
            List<Vector2Int> spritePts = _buildMd.CheckPoints.Count > 0 ? _buildMd.CheckPoints : _buildMd.BuildLockPoints;
            if (spritePts == null || spritePts.Count == 0) spritePts = _buildMd.LockPoints;
            if (spritePts == null || spritePts.Count == 0) spritePts = _buildMd.FallbackBoxPoints();
            int spriteStep = Mathf.Max(1, (spritePts.Count / 120) + 1);
            for (int i = 0; i < spritePts.Count; i += spriteStep)
            {
                Vector2Int p = spritePts[i];
                int cx = r.FootprintCellX + p.x;
                int cy = r.FootprintCellY + p.y;

                int ox = cx * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                int oy = cy * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;

                byte res;
                string audit;
                if (mode != null && mode.C2OriginalResourceMapV1TryDetermineResourceLikeOriginal(ox, oy, out res, out audit))
                {
                    if (res == C2BattleTerrainMode.C2OriginalResourceWoodV1LikeOriginal)
                    {
                        // Original: EraseTreesInPoint later clears trees on BUILDLOCKPOINTS.
                        r.WoodHits++;
                    }
                    else if (res == C2BattleTerrainMode.C2OriginalResourceFoodV1LikeOriginal)
                    {
                        // Fields/food complex sprites are not erased like trees; block ordinary building placement.
                        r.FoodHits++;
                        r.Valid = false;
                    }
                    else if (res == C2BattleTerrainMode.C2OriginalResourceStoneV1LikeOriginal ||
                             res == C2BattleTerrainMode.C2OriginalResourceGoldV1LikeOriginal ||
                             res == C2BattleTerrainMode.C2OriginalResourceIronV1LikeOriginal ||
                             res == C2BattleTerrainMode.C2OriginalResourceCoalV1LikeOriginal)
                    {
                        // Audit-only for now: original CheckSpritesInCellNew allows some STONES (IntResType>8),
                        // while mines/resources need their special ProdType path.
                        r.StoneOrOreHits++;
                    }

                    if (resAudit != null && resAudit.Length < 420)
                    {
                        if (resAudit.Length > 0) resAudit.Append(" | ");
                        resAudit.Append(audit);
                    }
                }
            }

            // Motion lock and slope checks use CHECKPOINTS, like original CheckVLine/CheckHLine/maxZ-minZ.
            // V60: check ALL points, not every N-th point. Also treat CHECKPOINTS outside height-map as invalid.
            List<Vector2Int> checkPts = _buildMd.CheckPoints.Count > 0 ? _buildMd.CheckPoints : spritePts;
            r.CheckPointCount = checkPts != null ? checkPts.Count : 0;

            int heightMapW;
            int heightMapH;
            bool hasHeightMapBounds = TryGetOriginalHeightMapDimensionsLikeOriginal(mode, out heightMapW, out heightMapH);

            for (int i = 0; checkPts != null && i < checkPts.Count; i++)
            {
                Vector2Int p = checkPts[i];
                int cx = r.FootprintCellX + p.x;
                int cy = r.FootprintCellY + p.y;

                if (cx < r.CheckMinCellX) r.CheckMinCellX = cx;
                if (cy < r.CheckMinCellY) r.CheckMinCellY = cy;
                if (cx > r.CheckMaxCellX) r.CheckMaxCellX = cx;
                if (cy > r.CheckMaxCellY) r.CheckMaxCellY = cy;

                if (hasHeightMapBounds && (cx < 0 || cy < 0 || cx >= heightMapW || cy >= heightMapH))
                {
                    // V64:
                    // Do not reject by full CHECKPOINTS bounds.
                    // The original first hard map check is anchor-based:
                    // xs=x2>>9; ys=y2>>9; if outside -> reject.
                    // A large palace can have far CHECKPOINTS outside the raw height array near map edges,
                    // while the visible/anchor placement is still inside the map.
                    // Treat these points as "no height sample", not as placement blocker.
                    r.CheckMapBoundsHits++;
                    continue;
                }

                if (CheckVLineBlockedLikeOriginal(cx, cy - 4, 8) || CheckHLineBlockedLikeOriginal(cx - 3, cy, 6))
                {
                    r.BlockedCells++;
                    r.Valid = false;
                }

                int h;
                if (TrySampleOriginalHeightAtCellLikeOriginal(mode, cx, cy, out h))
                {
                    r.CheckSamples++;
                    if (h < r.HeightMin)
                    {
                        r.HeightMin = h;
                        r.HeightMinCellX = cx;
                        r.HeightMinCellY = cy;
                    }
                    if (h > r.HeightMax)
                    {
                        r.HeightMax = h;
                        r.HeightMaxCellX = cx;
                        r.HeightMaxCellY = cy;
                    }
                }
            }

            if (r.HeightMin != int.MaxValue && r.HeightMax != int.MinValue)
            {
                // V63:
                // SampleWallHeightOriginalXYV1LikeOriginal returns raw THMap height.
                // Original screen/placement logic uses shifted terrain height (THMap >> ScShift),
                // not the raw short value. Raw delta 100 is only about 6 screen-height units.
                // Using raw delta made visually flat places fail as slopeHeight=100+.
                r.HeightDeltaRaw = Mathf.Abs(r.HeightMax - r.HeightMin);

                int minPlacementH = ShiftOriginalHeightForBuildSlopeV63LikeOriginal(r.HeightMin);
                int maxPlacementH = ShiftOriginalHeightForBuildSlopeV63LikeOriginal(r.HeightMax);
                r.HeightDelta = Mathf.Abs(maxPlacementH - minPlacementH);

                if (r.HeightDelta > 50)
                    r.Valid = false;
            }

            ApplyOriginalBuildBarRoadAndBuildingBlockersLikeOriginal(mode, realX, realY, ref r);

            r.ResourceAudit = resAudit != null ? resAudit.ToString() : string.Empty;
            r.Reason = BuildPlacementReasonLikeOriginal(ref r);
            return r;
        }

        private void ApplyOriginalBuildBarRoadAndBuildingBlockersLikeOriginal(C2BattleTerrainMode mode, int realX, int realY, ref C2PlacementCheckV27 r)
        {
            if (mode == null || _buildMd == null) return;

            C2BuildBarAreaV38 candidate;
            if (!_buildMd.TryGetOriginalBuildBarArea(realX, realY, out candidate))
                return;

            // Original CheckRoadsInArea is driven by BUILDBAR, not by visual sprite bounds.
            int roadHits = CountOriginalRoadCellsInBuildBarLikeOriginal(candidate);
            if (roadHits > 0)
            {
                r.RoadHits += roadHits;
                r.Valid = false;
            }

            // V65: water blocks ordinary building placement.
            // Use the same BUILDBAR polygon as roads/buildings, but sample parsed SEA2 WaterDeep.
            int waterHits = CountOriginalWaterCellsInBuildBarLikeOriginal(mode, candidate);
            if (waterHits > 0)
            {
                r.WaterHits += waterHits;
                r.Valid = false;
            }

            RefreshRuntimeBlockerCacheLikeOriginal(false);

            // V66 hard blocker:
            // Existing placed/building zones are already written into the building motion field.
            // CHECKPOINTS line probes can miss intersections, so scan the candidate BUILDBAR polygon itself.
            int motionBuildingHits = CountOriginalMotionBlockedCellsInBuildBarLikeOriginal(candidate);
            if (motionBuildingHits > 0)
            {
                r.BuildingHits += motionBuildingHits;
                r.Valid = false;
            }

            for (int i = 0; i < _runtimeBuildingBuildBarsV38.Count; i++)
            {
                if (!BuildBarsOverlapLikeOriginal(candidate, _runtimeBuildingBuildBarsV38[i])) continue;
                r.BuildingHits++;
                r.Valid = false;
                break;
            }

            // Fallback for saved map objects whose unit-id -> MD mapping was not resolved.
            // Their renderer bounds are cached in _runtimeBuildingBounds; check them too.
            if (_runtimeBuildingBounds.Count > 0)
            {
                Bounds candidateWorld;
                if (TryBuildWorldBuildBarBoundsLikeOriginal(mode, candidate, out candidateWorld))
                {
                    for (int i = 0; i < _runtimeBuildingBounds.Count; i++)
                    {
                        if (!BoundsOverlapXZLikeOriginal(candidateWorld, _runtimeBuildingBounds[i])) continue;
                        r.BuildingHits++;
                        r.Valid = false;
                        break;
                    }
                }
            }
        }

        private void ApplyOriginalAnchorMapBoundsLikeOriginal(C2BattleTerrainMode mode, int realX, int realY, ref C2PlacementCheckV27 r)
        {
            int xs = realX >> 9;
            int ys = realY >> 9;
            int minMapX;
            int minMapY;
            int maxMapX;
            int maxMapY;
            if (TryGetOriginalMapCellBoundsLikeOriginal(mode, out minMapX, out minMapY, out maxMapX, out maxMapY))
            {
                if (xs <= 0 || ys <= 0 || xs >= maxMapX || ys >= maxMapY)
                {
                    r.MapBoundsHits++;
                    r.Valid = false;
                }
            }
            else if (xs <= 0 || ys <= 0)
            {
                r.MapBoundsHits++;
                r.Valid = false;
            }
        }

        private static bool CheckVLineBlockedLikeOriginal(int x, int y, int len)
        {
            for (int i = 0; i < len; i++)
                if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedLikeOriginal(x, y + i))
                    return true;
            return false;
        }

        private static bool CheckHLineBlockedLikeOriginal(int x, int y, int len)
        {
            for (int i = 0; i < len; i++)
                if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedLikeOriginal(x + i, y))
                    return true;
            return false;
        }

        private static bool TryGetOriginalHeightMapDimensionsLikeOriginal(C2BattleTerrainMode mode, out int vertInLine, out int maxTH)
        {
            vertInLine = 0;
            maxTH = 0;

            object map = TryGetParsedMapObjectLikeOriginal(mode);
            if (map == null) return false;

            try
            {
                vertInLine = GetIntFieldLikeOriginal(map, "VertInLine", 0);
                maxTH = GetIntFieldLikeOriginal(map, "MaxTH", 0);
                Array heights = GetArrayFieldLikeOriginal(map, "Heights");
                return vertInLine > 0 && maxTH > 0 && heights != null && heights.Length > 0;
            }
            catch
            {
                vertInLine = 0;
                maxTH = 0;
                return false;
            }
        }

        private static MethodInfo s_C2VisualSurfaceHeightSamplerV62;
        private static bool s_C2VisualSurfaceHeightSamplerResolvedV62;

        private static bool TrySampleOriginalHeightAtCellLikeOriginal(C2BattleTerrainMode mode, int cx, int cy, out int h)
        {
            // V62:
            // Do NOT read map.Heights[cy * VertInLine + cx] directly for building CHECKPOINTS.
            // That desynced with the visual terrain/units placement surface.
            // First use the same terrain height sampler path used by visual objects:
            // C2BattleTerrainMode.SampleWallHeightOriginalXYV1LikeOriginal(originalX, originalY).
            //
            // Original coords convention already used by our overlay/world placement:
            // cell -> original pixel XY = cell * 16.
            if (TrySampleVisualSurfaceHeightAtCellV62LikeOriginal(mode, cx, cy, out h))
                return true;

            // Safe fallback for older builds where the visual sampler method is absent.
            return TrySampleRawTHMapHeightAtCellV62FallbackLikeOriginal(mode, cx, cy, out h);
        }

        private static bool TrySampleVisualSurfaceHeightAtCellV62LikeOriginal(C2BattleTerrainMode mode, int cx, int cy, out int h)
        {
            h = 0;
            if (mode == null) return false;

            try
            {
                if (!s_C2VisualSurfaceHeightSamplerResolvedV62)
                {
                    s_C2VisualSurfaceHeightSamplerResolvedV62 = true;
                    s_C2VisualSurfaceHeightSamplerV62 = typeof(C2BattleTerrainMode).GetMethod(
                        "SampleWallHeightOriginalXYV1LikeOriginal",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new Type[] { typeof(float), typeof(float) },
                        null);
                }

                if (s_C2VisualSurfaceHeightSamplerV62 == null)
                    return false;

                float originalX = cx * 16.0f;
                float originalY = cy * 16.0f;
                object value = s_C2VisualSurfaceHeightSamplerV62.Invoke(mode, new object[] { originalX, originalY });
                if (value == null) return false;

                float fh = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                if (float.IsNaN(fh) || float.IsInfinity(fh))
                    return false;

                h = Mathf.RoundToInt(fh);
                return true;
            }
            catch
            {
                h = 0;
                return false;
            }
        }

        private static bool TrySampleRawTHMapHeightAtCellV62FallbackLikeOriginal(C2BattleTerrainMode mode, int cx, int cy, out int h)
        {
            h = 0;
            object map = TryGetParsedMapObjectLikeOriginal(mode);
            if (map == null) return false;
            try
            {
                int vertInLine = GetIntFieldLikeOriginal(map, "VertInLine", 0);
                int maxTH = GetIntFieldLikeOriginal(map, "MaxTH", 0);
                Array heights = GetArrayFieldLikeOriginal(map, "Heights");
                if (vertInLine <= 0 || maxTH <= 0 || heights == null || heights.Length == 0)
                    return false;
                if (cx < 0 || cy < 0 || cx >= vertInLine || cy >= maxTH)
                    return false;
                int idx = cy * vertInLine + cx;
                if ((uint)idx >= (uint)heights.Length)
                    return false;
                object v = heights.GetValue(idx);
                h = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private int CountOriginalMotionBlockedCellsInBuildBarLikeOriginal(C2BuildBarAreaV38 area)
        {
            int[] px;
            int[] py;
            if (!TryBuildExpandedBuildBarPolygonPixelsLikeOriginal(area, out px, out py))
                return 0;

            int minX, maxX, minY, maxY;
            GetPolygonBoundsLikeOriginal(px, py, 8, out minX, out maxX, out minY, out maxY);

            int cminX = Mathf.FloorToInt(minX / (float)MotionCellOriginalPixels) - 1;
            int cmaxX = Mathf.CeilToInt(maxX / (float)MotionCellOriginalPixels) + 1;
            int cminY = Mathf.FloorToInt(minY / (float)MotionCellOriginalPixels) - 1;
            int cmaxY = Mathf.CeilToInt(maxY / (float)MotionCellOriginalPixels) + 1;

            int hits = 0;
            int guard = 0;
            for (int cy = cminY; cy <= cmaxY; cy++)
            {
                for (int cx = cminX; cx <= cmaxX; cx++)
                {
                    if (++guard > 24000) return hits;

                    int ox = cx * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                    int oy = cy * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                    if (!PointInPolygonLikeOriginal(ox, oy, px, py, 8))
                        continue;

                    if (!C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedLikeOriginal(cx, cy))
                        continue;

                    hits++;
                    if (hits >= 64) return hits;
                }
            }

            return hits;
        }

        private bool TryBuildWorldBuildBarBoundsLikeOriginal(C2BattleTerrainMode mode, C2BuildBarAreaV38 area, out Bounds b)
        {
            b = default(Bounds);
            if (mode == null) return false;

            int x0 = area.X0 >> 4;
            int y0 = area.Y0 >> 4;
            int x1 = area.X1 >> 4;
            int y1 = area.Y1 >> 4;

            Vector3 w00 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x0, y0);
            Vector3 w10 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y0);
            Vector3 w11 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y1);
            Vector3 w01 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x0, y1);

            Vector3 min = Vector3.Min(Vector3.Min(w00, w10), Vector3.Min(w11, w01));
            Vector3 max = Vector3.Max(Vector3.Max(w00, w10), Vector3.Max(w11, w01));
            min.y -= 64.0f;
            max.y += 64.0f;
            b.SetMinMax(min, max);
            return true;
        }

        private static bool TryBuildExpandedBuildBarPolygonPixelsLikeOriginal(C2BuildBarAreaV38 area, out int[] px, out int[] py)
        {
            px = new int[8];
            py = new int[8];

            int x0 = area.X0 >> 4;
            int y0 = area.Y0 >> 4;
            int x1 = area.X1 >> 4;
            int y1 = area.Y1 >> 4;

            x0 -= 32;
            x1 += 32;

            int ddd = 64;

            int ex0 = x0;
            int ey0 = y0;
            int ex1 = (x0 + x1 + y0 - y1) / 2;
            int ey1 = (y0 + y1 + x0 - x1) / 2;
            int ex2 = x1;
            int ey2 = y1;
            int ex3 = (x0 + x1 + y1 - y0) / 2;
            int ey3 = (y0 + y1 + x1 - x0) / 2;

            px[0] = ex0;       py[0] = ey0 + ddd;
            px[1] = ex0;       py[1] = ey0 - ddd;
            px[2] = ex1 - ddd; py[2] = ey1;
            px[3] = ex1 + ddd; py[3] = ey1;
            px[4] = ex2;       py[4] = ey2 - ddd;
            px[5] = ex2;       py[5] = ey2 + ddd;
            px[6] = ex3 + ddd; py[6] = ey3;
            px[7] = ex3 - ddd; py[7] = ey3;

            return true;
        }

        private static void GetPolygonBoundsLikeOriginal(int[] px, int[] py, int n, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = px != null && px.Length > 0 ? px[0] : 0;
            maxX = minX;
            minY = py != null && py.Length > 0 ? py[0] : 0;
            maxY = minY;

            for (int i = 1; px != null && py != null && i < n && i < px.Length && i < py.Length; i++)
            {
                if (px[i] < minX) minX = px[i];
                if (px[i] > maxX) maxX = px[i];
                if (py[i] < minY) minY = py[i];
                if (py[i] > maxY) maxY = py[i];
            }
        }

        private int CountOriginalWaterCellsInBuildBarLikeOriginal(C2BattleTerrainMode mode, C2BuildBarAreaV38 area)
        {
            object water = TryGetParsedWaterObjectLikeOriginal(mode);
            if (water == null)
                return 0;

            int seaLx = GetIntFieldLikeOriginal(water, "SeaLx", 0);
            int seaLy = GetIntFieldLikeOriginal(water, "SeaLy", 0);
            Array deep = GetArrayFieldLikeOriginal(water, "WaterDeep");

            if (seaLx <= 0 || seaLy <= 0 || deep == null || deep.Length < seaLx * seaLy)
                return 0;

            int[] px;
            int[] py;
            if (!TryBuildExpandedBuildBarPolygonPixelsLikeOriginal(area, out px, out py))
                return 0;

            int minX, maxX, minY, maxY;
            GetPolygonBoundsLikeOriginal(px, py, 8, out minX, out maxX, out minY, out maxY);

            int cminX = Mathf.FloorToInt(minX / (float)MotionCellOriginalPixels) - 1;
            int cmaxX = Mathf.CeilToInt(maxX / (float)MotionCellOriginalPixels) + 1;
            int cminY = Mathf.FloorToInt(minY / (float)MotionCellOriginalPixels) - 1;
            int cmaxY = Mathf.CeilToInt(maxY / (float)MotionCellOriginalPixels) + 1;

            int hits = 0;
            int guard = 0;
            for (int cy = cminY; cy <= cmaxY; cy++)
            {
                if (cy < 0 || cy >= seaLy)
                    continue;

                for (int cx = cminX; cx <= cmaxX; cx++)
                {
                    if (++guard > 24000) return hits;
                    if (cx < 0 || cx >= seaLx)
                        continue;

                    int ox = cx * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                    int oy = cy * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                    if (!PointInPolygonLikeOriginal(ox, oy, px, py, 8))
                        continue;

                    int idx = cy * seaLx + cx;
                    if ((uint)idx >= (uint)deep.Length)
                        continue;

                    object v = deep.GetValue(idx);
                    int d = 0;
                    try { d = Convert.ToInt32(v, CultureInfo.InvariantCulture); }
                    catch { d = 0; }

                    // SEA2 WaterDeep==0 means no water. Any non-zero water coverage blocks building placement.
                    if (d <= 0)
                        continue;

                    hits++;
                    if (hits >= 64) return hits;
                }
            }

            return hits;
        }

        private static object TryGetParsedWaterObjectLikeOriginal(C2BattleTerrainMode mode)
        {
            object map = TryGetParsedMapObjectLikeOriginal(mode);
            if (map == null) return null;

            try
            {
                FieldInfo f = map.GetType().GetField("Water", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return f != null ? f.GetValue(map) : null;
            }
            catch
            {
                return null;
            }
        }

        private int CountOriginalRoadCellsInBuildBarLikeOriginal(C2BuildBarAreaV38 area)
        {
            if (_runtimeRoadCellsV37.Count == 0)
                return 0;

            // Original CheckRoadsInArea shifts from real coords to original pixels, expands the bar polygon,
            // and checks road knot centers inside that polygon.
            int[] px;
            int[] py;
            if (!TryBuildExpandedBuildBarPolygonPixelsLikeOriginal(area, out px, out py))
                return 0;

            int minX, maxX, minY, maxY;
            GetPolygonBoundsLikeOriginal(px, py, 8, out minX, out maxX, out minY, out maxY);

            int cminX = Mathf.FloorToInt(minX / (float)MotionCellOriginalPixels) - 1;
            int cmaxX = Mathf.CeilToInt(maxX / (float)MotionCellOriginalPixels) + 1;
            int cminY = Mathf.FloorToInt(minY / (float)MotionCellOriginalPixels) - 1;
            int cmaxY = Mathf.CeilToInt(maxY / (float)MotionCellOriginalPixels) + 1;

            int hits = 0;
            int guard = 0;
            for (int cy = cminY; cy <= cmaxY; cy++)
            {
                for (int cx = cminX; cx <= cmaxX; cx++)
                {
                    if (++guard > 20000) return hits;
                    if (!_runtimeRoadCellsV37.Contains(PackCellKeyLikeOriginal(cx, cy)))
                        continue;
                    int ox = cx * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                    int oy = cy * MotionCellOriginalPixels + MotionCellOriginalPixels / 2;
                    if (!PointInPolygonLikeOriginal(ox, oy, px, py, 8))
                        continue;
                    hits++;
                    if (hits >= 64) return hits;
                }
            }
            return hits;
        }

        private static bool PointInPolygonLikeOriginal(int x, int y, int[] px, int[] py, int n)
        {
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                bool crosses = ((py[i] > y) != (py[j] > y));
                if (!crosses) continue;
                float atX = px[i] + (px[j] - px[i]) * ((y - py[i]) / (float)(py[j] - py[i]));
                if (x < atX) inside = !inside;
            }
            return inside;
        }

        private static bool BuildBarsOverlapLikeOriginal(C2BuildBarAreaV38 a, C2BuildBarAreaV38 b)
        {
            int nx0 = a.X0 + a.Y0;
            int ny0 = a.Y0 - a.X0;
            int nx1 = a.X1 + a.Y1;
            int ny1 = a.Y1 - a.X1;
            int nxc = (nx0 + nx1) >> 1;
            int nyc = (ny0 + ny1) >> 1;
            int rx = Mathf.Abs(nx1 - nx0) >> 1;
            int ry = Mathf.Abs(ny1 - ny0) >> 1;

            int bnx0 = b.X0 + b.Y0;
            int bny0 = b.Y0 - b.X0;
            int bnx1 = b.X1 + b.Y1;
            int bny1 = b.Y1 - b.X1;
            int bnxc = (bnx0 + bnx1) >> 1;
            int bnyc = (bny0 + bny1) >> 1;
            int brx = Mathf.Abs(bnx1 - bnx0) >> 1;
            int bry = Mathf.Abs(bny1 - bny0) >> 1;

            return Mathf.Abs(bnxc - nxc) < brx + rx && Mathf.Abs(bnyc - nyc) < bry + ry;
        }

        private struct C2BuildBarAreaV38
        {
            public int X0;
            public int Y0;
            public int X1;
            public int Y1;
            public string Audit;
        }

        private static int ShiftOriginalHeightForBuildSlopeV63LikeOriginal(int h)
        {
            // Original render formula uses THMap >> ScShift. In our map pipeline ScShift is 4.
            // Keep arithmetic shift for negative heights.
            return h >> 4;
        }

        private static string BuildPlacementReasonLikeOriginal(ref C2PlacementCheckV27 r)
        {
            string heightAudit = BuildHeightAuditLikeOriginal(ref r);
            string checkAudit = BuildCheckBoxAuditLikeOriginal(ref r);

            if (r.Valid)
            {
                string audit = string.Empty;
                if (r.WoodHits > 0) audit += " woodEraseOk=" + r.WoodHits.ToString(CultureInfo.InvariantCulture);
                if (r.StoneOrOreHits > 0) audit += " stoneOreAuditOnly=" + r.StoneOrOreHits.ToString(CultureInfo.InvariantCulture);
                if (r.HeightDelta > 0) audit += " " + heightAudit;
                if (!string.IsNullOrEmpty(checkAudit)) audit += " " + checkAudit;
                if (r.CheckMapBoundsHits > 0) audit += " checkOutOfMapSkipped=" + r.CheckMapBoundsHits.ToString(CultureInfo.InvariantCulture);
                return "OK" + audit;
            }

            List<string> parts = new List<string>(8);
            if (r.BlockedCells > 0) parts.Add("motionField/lockpoints=" + r.BlockedCells.ToString(CultureInfo.InvariantCulture));
            if (r.BuildingHits > 0) parts.Add("existingBuilding=" + r.BuildingHits.ToString(CultureInfo.InvariantCulture));
            if (r.HeightDelta > 50) parts.Add("slopeHeight=" + r.HeightDelta.ToString(CultureInfo.InvariantCulture));
            if (r.MapBoundsHits > 0) parts.Add("mapBounds=" + r.MapBoundsHits.ToString(CultureInfo.InvariantCulture));
            if (r.RoadHits > 0) parts.Add("road=" + r.RoadHits.ToString(CultureInfo.InvariantCulture));
            if (r.WaterHits > 0) parts.Add("water=" + r.WaterHits.ToString(CultureInfo.InvariantCulture));
            if (r.FoodHits > 0) parts.Add("fieldFood=" + r.FoodHits.ToString(CultureInfo.InvariantCulture));
            if (parts.Count == 0) parts.Add("unknown");
            if (!string.IsNullOrEmpty(heightAudit)) parts.Add(heightAudit);
            if (!string.IsNullOrEmpty(checkAudit)) parts.Add(checkAudit);
            if (r.StoneOrOreHits > 0) parts.Add("stoneOreAuditOnly=" + r.StoneOrOreHits.ToString(CultureInfo.InvariantCulture));
            if (r.WoodHits > 0) parts.Add("woodEraseOk=" + r.WoodHits.ToString(CultureInfo.InvariantCulture));
            return string.Join(",", parts.ToArray());
        }

        private static string BuildHeightAuditLikeOriginal(ref C2PlacementCheckV27 r)
        {
            if (r.HeightMin == int.MaxValue || r.HeightMax == int.MinValue)
                return string.Empty;

            return "heightVisualShifted[minRaw=" + r.HeightMin.ToString(CultureInfo.InvariantCulture) +
                   "@" + r.HeightMinCellX.ToString(CultureInfo.InvariantCulture) +
                   "/" + r.HeightMinCellY.ToString(CultureInfo.InvariantCulture) +
                   " maxRaw=" + r.HeightMax.ToString(CultureInfo.InvariantCulture) +
                   "@" + r.HeightMaxCellX.ToString(CultureInfo.InvariantCulture) +
                   "/" + r.HeightMaxCellY.ToString(CultureInfo.InvariantCulture) +
                   " rawDelta=" + r.HeightDeltaRaw.ToString(CultureInfo.InvariantCulture) +
                   " shiftedDelta=" + r.HeightDelta.ToString(CultureInfo.InvariantCulture) +
                   " samples=" + r.CheckSamples.ToString(CultureInfo.InvariantCulture) + "]";
        }

        private static string BuildCheckBoxAuditLikeOriginal(ref C2PlacementCheckV27 r)
        {
            if (r.CheckPointCount <= 0 || r.CheckMinCellX == int.MaxValue || r.CheckMaxCellX == int.MinValue)
                return string.Empty;

            return "checkBox=" + r.CheckMinCellX.ToString(CultureInfo.InvariantCulture) +
                   "/" + r.CheckMinCellY.ToString(CultureInfo.InvariantCulture) +
                   "-" + r.CheckMaxCellX.ToString(CultureInfo.InvariantCulture) +
                   "/" + r.CheckMaxCellY.ToString(CultureInfo.InvariantCulture) +
                   " checkPts=" + r.CheckPointCount.ToString(CultureInfo.InvariantCulture);
        }

        private void RefreshRuntimeBlockerCacheLikeOriginal(bool force)
        {
            float now = Time.realtimeSinceStartup;
            if (!force && now < _nextRuntimeBlockerCacheRefresh)
                return;

            _nextRuntimeBlockerCacheRefresh = now + 2.50f;
            _runtimeBuildingBounds.Clear();
            _runtimeBuildingBuildBarsV38.Clear();
            _runtimeRoadBounds.Clear();
            _runtimeBlockerCacheVersion++;

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; buildings != null && i < buildings.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                if (b == null) continue;

                // V66: SourceMonsterId is the real unit/building id, KindName can be only a category label.
                // Using KindName first made exact BUILDBAR cache miss for many existing map buildings.
                string mdName = !string.IsNullOrEmpty(b.SourceMonsterId) ? b.SourceMonsterId : b.KindName;
                C2BuildMdInfoV27 md = C2BuildMdInfoV27.ParseCached(mdName);
                C2BuildBarAreaV38 area;
                if (md != null && md.Found && md.TryGetOriginalBuildBarArea(b.RealX, b.RealY, out area))
                {
                    area.Audit = "building idx=" + b.RecordIndex.ToString(CultureInfo.InvariantCulture) + " md='" + mdName + "'";
                    _runtimeBuildingBuildBarsV38.Add(area);
                    continue;
                }

                // Fallback only for objects without MD/BUILDBAR. Normal buildings use exact BUILDBAR above.
                Renderer[] rr = b.GetComponentsInChildren<Renderer>(true);
                for (int k = 0; rr != null && k < rr.Length; k++)
                {
                    Renderer rend = rr[k];
                    if (rend == null || !rend.enabled) continue;
                    _runtimeBuildingBounds.Add(rend.bounds);
                }
            }

            RefreshOriginalRoadCellCacheLikeOriginal(GetBattleTerrainModeCached(), force);
        }

        private void RefreshOriginalRoadCellCacheLikeOriginal(C2BattleTerrainMode mode, bool force)
        {
            if (mode == null) return;

            object map = TryGetParsedMapObjectLikeOriginal(mode);
            if (map == null) return;

            if (!force && ReferenceEquals(_runtimeRoadCellsMapV37, map) && _runtimeRoadCellsV37.Count > 0)
                return;

            _runtimeRoadCellsV37.Clear();
            _runtimeRoadCellsMapV37 = map;
            _runtimeRoadCellsSegmentsV37 = 0;
            _runtimeRoadCellsBuildsV37++;

            try
            {
                Type mapType = map.GetType();
                FieldInfo knotsField = mapType.GetField("RoadKnots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo hasField = mapType.GetField("HasRoadNet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                bool hasRoadNet = true;
                if (hasField != null)
                {
                    object hv = hasField.GetValue(map);
                    if (hv is bool) hasRoadNet = (bool)hv;
                }

                Array knots = knotsField != null ? knotsField.GetValue(map) as Array : null;
                if (!hasRoadNet || knots == null || knots.Length == 0)
                    return;

                for (int i = 0; i < knots.Length; i++)
                {
                    object k0 = knots.GetValue(i);
                    if (k0 == null) continue;
                    if (GetIntFieldLikeOriginal(k0, "Hidden", 0) != 0) continue;

                    int x0 = GetIntFieldLikeOriginal(k0, "X", 0);
                    int y0 = GetIntFieldLikeOriginal(k0, "Y", 0);
                    int nLinks = Mathf.Clamp(GetIntFieldLikeOriginal(k0, "NLinks", 0), 0, 8);

                    Array links = GetArrayFieldLikeOriginal(k0, "Links");
                    Array linkTypes = GetArrayFieldLikeOriginal(k0, "LinkType");
                    if (links == null) continue;

                    for (int j = 0; j < nLinks && j < links.Length; j++)
                    {
                        int f = Convert.ToInt32(links.GetValue(j), CultureInfo.InvariantCulture);
                        if (f < 0 || f >= knots.Length || f <= i)
                            continue;

                        object k1 = knots.GetValue(f);
                        if (k1 == null) continue;
                        if (GetIntFieldLikeOriginal(k1, "Hidden", 0) != 0) continue;

                        int x1 = GetIntFieldLikeOriginal(k1, "X", x0);
                        int y1 = GetIntFieldLikeOriginal(k1, "Y", y0);
                        int type = 0;
                        if (linkTypes != null && j < linkTypes.Length)
                            type = Convert.ToInt32(linkTypes.GetValue(j), CultureInfo.InvariantCulture);

                        int radiusCells = RoadBlockRadiusCellsByTypeLikeOriginal(type);
                        AddOriginalRoadSegmentCellsLikeOriginal(x0, y0, x1, y1, radiusCells);
                        _runtimeRoadCellsSegmentsV37++;

                        if (_runtimeRoadCellsV37.Count > 600000)
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[C2:BUILD PREVIEW V44 ROAD CACHE WARN] " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static object TryGetParsedMapObjectLikeOriginal(C2BattleTerrainMode mode)
        {
            if (mode == null) return null;
            try
            {
                FieldInfo f = typeof(C2BattleTerrainMode).GetField("_map", BindingFlags.Instance | BindingFlags.NonPublic);
                return f != null ? f.GetValue(mode) : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetOriginalMapCellBoundsLikeOriginal(C2BattleTerrainMode mode, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = minY = 0;
            maxX = maxY = 0;
            object map = TryGetParsedMapObjectLikeOriginal(mode);
            if (map == null) return false;

            try
            {
                minX = GetIntFieldLikeOriginal(map, "MinMapX", 0);
                minY = GetIntFieldLikeOriginal(map, "MinMapY", 0);
                maxX = GetIntFieldLikeOriginal(map, "MaxMapX", 0);
                maxY = GetIntFieldLikeOriginal(map, "MaxMapY", 0);
                return maxX > minX + 4 && maxY > minY + 4;
            }
            catch
            {
                return false;
            }
        }

        private static int GetIntFieldLikeOriginal(object obj, string name, int fallback)
        {
            if (obj == null) return fallback;
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return fallback;
            object v = f.GetValue(obj);
            if (v == null) return fallback;
            try
            {
                return Convert.ToInt32(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static Array GetArrayFieldLikeOriginal(object obj, string name)
        {
            if (obj == null) return null;
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return f != null ? f.GetValue(obj) as Array : null;
        }

        private void AddOriginalRoadSegmentCellsLikeOriginal(float x0, float y0, float x1, float y1, int radiusCells)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            int steps = Mathf.Clamp(Mathf.CeilToInt(len / 8.0f), 1, 8192);
            int rr = Mathf.Max(1, radiusCells);
            int rr2 = rr * rr;

            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                float x = x0 + dx * t;
                float y = y0 + dy * t;
                int cx = Mathf.FloorToInt(x / MotionCellOriginalPixels);
                int cy = Mathf.FloorToInt(y / MotionCellOriginalPixels);

                for (int oy = -rr; oy <= rr; oy++)
                {
                    for (int ox = -rr; ox <= rr; ox++)
                    {
                        if (ox * ox + oy * oy > rr2)
                            continue;
                        _runtimeRoadCellsV37.Add(PackCellKeyLikeOriginal(cx + ox, cy + oy));
                    }
                }
            }
        }

        private static int RoadBlockRadiusCellsByTypeLikeOriginal(int type)
        {
            // RoadDesc.RWidth is private in the terrain partial; this safe approximation is in the same
            // motion-field grid used by BUILDLOCKPOINTS/CHECKPOINTS. Narrow trails get less padding, wide
            // map roads/junctions get more. It is intentionally cheaper and closer than renderer AABBs.
            if (type == 11) return 3;
            if (type == 0 || type == 14 || type == 16) return 5;
            return 4;
        }

        private bool IsOriginalRoadCellBlockedLikeOriginal(int cellX, int cellY)
        {
            if (_runtimeRoadCellsV37.Count == 0)
                return false;
            return _runtimeRoadCellsV37.Contains(PackCellKeyLikeOriginal(cellX, cellY));
        }

        private static long PackCellKeyLikeOriginal(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }

        private static void AddRoadRendererLocalTriangleBoundsLikeOriginal(Renderer rend, List<Bounds> outBounds)
        {
            if (rend == null || outBounds == null) return;

            Mesh mesh = null;
            MeshFilter mf = rend.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;

            // Do not use rend.bounds for road meshes. In our Unity pipeline one road renderer can cover the whole map,
            // while original CheckRoadsInArea checks precise road knots/curves. Using the whole renderer AABB makes
            // every point on the map look like road=2. Cache per-triangle AABBs instead.
            if (mesh == null || mesh.vertexCount <= 0 || mesh.triangles == null || mesh.triangles.Length < 3)
            {
                Bounds rb = rend.bounds;
                if (rb.size.x < 256.0f && rb.size.z < 256.0f)
                    outBounds.Add(rb);
                return;
            }

            Vector3[] vv = mesh.vertices;
            int[] tris = mesh.triangles;
            Transform tr = rend.transform;
            int guard = 0;
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int i0 = tris[t];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];
                if ((uint)i0 >= (uint)vv.Length || (uint)i1 >= (uint)vv.Length || (uint)i2 >= (uint)vv.Length)
                    continue;

                Vector3 w0 = tr.TransformPoint(vv[i0]);
                Vector3 w1 = tr.TransformPoint(vv[i1]);
                Vector3 w2 = tr.TransformPoint(vv[i2]);

                Vector3 min = Vector3.Min(w0, Vector3.Min(w1, w2));
                Vector3 max = Vector3.Max(w0, Vector3.Max(w1, w2));

                // Small padding approximates the original road curve distance/expanded polygon test.
                const float pad = 0.75f;
                min.x -= pad; min.z -= pad; min.y -= 1.0f;
                max.x += pad; max.z += pad; max.y += 1.0f;

                Bounds b = default(Bounds);
                b.SetMinMax(min, max);
                if (b.size.x <= 0.0001f || b.size.z <= 0.0001f)
                    continue;

                outBounds.Add(b);

                // Safety against malformed gigantic meshes. Normal road meshes are far below this.
                if (++guard > 200000)
                    break;
            }
        }

        private static bool TryGetTerrainBoundsLikeOriginal(C2BattleTerrainMode mode, out Bounds b)
        {
            b = default(Bounds);
            if (mode == null) return false;

            try
            {
                FieldInfo f = typeof(C2BattleTerrainMode).GetField("_terrainBounds", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f == null) return false;
                object val = f.GetValue(mode);
                if (!(val is Bounds)) return false;
                b = (Bounds)val;
                return b.size.x > 0.01f && b.size.z > 0.01f;
            }
            catch
            {
                return false;
            }
        }

        private static bool TerrainContainsFootprintXZLikeOriginal(Bounds terrain, Bounds footprint)
        {
            // Original CheckCreationAbility rejects coordinates outside the playable map.
            // Here we reject when the preview footprint leaks outside the built Unity terrain bounds.
            const float pad = 0.25f;
            if (footprint.min.x < terrain.min.x - pad) return false;
            if (footprint.max.x > terrain.max.x + pad) return false;
            if (footprint.min.z < terrain.min.z - pad) return false;
            if (footprint.max.z > terrain.max.z + pad) return false;
            return true;
        }

        private bool TryBuildWorldFootprintBoundsLikeOriginal(C2BattleTerrainMode mode, int footprintCellX, int footprintCellY, out Bounds b)
        {
            b = default(Bounds);
            if (mode == null || _buildMd == null) return false;

            int minLocalX;
            int maxLocalX;
            int minLocalY;
            int maxLocalY;
            _buildMd.GetPreviewBounds(out minLocalX, out maxLocalX, out minLocalY, out maxLocalY);

            float x0 = (footprintCellX + minLocalX) * MotionCellOriginalPixels;
            float y0 = (footprintCellY + minLocalY) * MotionCellOriginalPixels;
            float x1 = (footprintCellX + maxLocalX + 1) * MotionCellOriginalPixels;
            float y1 = (footprintCellY + maxLocalY + 1) * MotionCellOriginalPixels;

            Vector3 w00 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x0, y0);
            Vector3 w10 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y0);
            Vector3 w11 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y1);
            Vector3 w01 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x0, y1);

            Vector3 min = Vector3.Min(Vector3.Min(w00, w10), Vector3.Min(w11, w01));
            Vector3 max = Vector3.Max(Vector3.Max(w00, w10), Vector3.Max(w11, w01));
            min.y -= 8.0f;
            max.y += 8.0f;
            b.SetMinMax(min, max);
            return true;
        }

        private static bool BoundsOverlapXZLikeOriginal(Bounds a, Bounds b)
        {
            if (a.max.x < b.min.x || a.min.x > b.max.x) return false;
            if (a.max.z < b.min.z || a.min.z > b.max.z) return false;
            return true;
        }

        private static bool LooksLikeRoadRendererLikeOriginal(Renderer r)
        {
            if (r == null) return false;
            Material mat = r.sharedMaterial;
            string n = FullPathLikeOriginal(r.transform) + "|" + (mat != null ? mat.name : string.Empty);
            Texture tex = null;
            if (mat != null)
            {
                if (mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                else if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
            }
            if (tex != null)
                n += "|" + tex.name;
            return n.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("дорог", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("Roads", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FullPathLikeOriginal(Transform t)
        {
            if (t == null) return string.Empty;
            StringBuilder sb = new StringBuilder(t.name ?? string.Empty);
            Transform p = t.parent;
            int guard = 0;
            while (p != null && guard++ < 32)
            {
                sb.Insert(0, "/");
                sb.Insert(0, p.name ?? string.Empty);
                p = p.parent;
            }
            return sb.ToString();
        }

        private void UpdatePreviewMeshLikeOriginal(
            C2BattleTerrainMode mode,
            int footprintCellX,
            int footprintCellY,
            int minLocalX,
            int maxLocalX,
            int minLocalY,
            int maxLocalY,
            bool valid)
        {
            if (mode == null)
                return;

            EnsurePreviewObjects();

            float x0 = (footprintCellX + minLocalX) * MotionCellOriginalPixels;
            float y0 = (footprintCellY + minLocalY) * MotionCellOriginalPixels;
            float x1 = (footprintCellX + maxLocalX + 1) * MotionCellOriginalPixels;
            float y1 = (footprintCellY + maxLocalY + 1) * MotionCellOriginalPixels;

            Vector3 w00 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x0, y0);
            Vector3 w10 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y0);
            Vector3 w11 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x1, y1);
            Vector3 w01 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(x0, y1);

            w00.y += PreviewYOffset;
            w10.y += PreviewYOffset;
            w11.y += PreviewYOffset;
            w01.y += PreviewYOffset;

            _mesh.Clear();
            _mesh.vertices = new[] { w00, w10, w11, w01 };
            _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            _mesh.RecalculateBounds();

            _meshRenderer.sharedMaterial = valid ? _validMat : _invalidMat;

            // When the real composite ghost is available the original preview is the building sprite itself.
            // If no ghost can be built, never cover valid terrain by the old white fallback rectangle.
            if (_root != null)
                _root.SetActive(!HasGhostLikeOriginal() && !valid);

            SetPreviewVisible(true);
        }


        private void LoadGhostTextureLikeOriginal()
        {
            _ghostTexture = null;
            _ghostAudit = "no_md";
            _ghostFrame = _buildMd != null ? _buildMd.SelectGhostFrameLikeOriginal() : new C2AnimFrameRefV28();

            if (_buildMd == null || !_buildMd.Found)
            {
                _ghostAudit = "md_not_found";
                return;
            }

            if (!_ghostFrame.Valid)
            {
                _ghostAudit = "no_#BUILDLO_0_or_#STANDLO_frame";
                return;
            }

            string package = _buildMd.ResolvePackageForFrame(_ghostFrame.FileRef);
            if (string.IsNullOrEmpty(package))
                package = _buildMd.Package;

            string audit;
            _ghostTexture = TryLoadGpFrameTextureLikeOriginal(package, _buildMd.MdPath, _ghostFrame.SpriteId, out audit);
            _ghostAudit = "package='" + (package ?? string.Empty) + "' " + audit;
            if (_ghostTexture != null)
            {
                ConfigureGhostMaterialLikeOriginal(_ghostValidMat, _ghostTexture, new Color(1.0f, 1.0f, 1.0f, 0.72f));
                ConfigureGhostMaterialLikeOriginal(_ghostInvalidMat, _ghostTexture, new Color(1.0f, 0.0f, 0.0f, 0.72f));
                SetPreviewVisible(_active);
            }
        }

        private void UpdateGhostMeshLikeOriginal(C2BattleTerrainMode mode, int realX, int realY, bool valid)
        {
            if (TryUpdateCompositeGhostLikeOriginal(mode, realX, realY, valid))
                return;

            if (mode == null || _ghostTexture == null || _ghostMesh == null)
                return;

            EnsurePreviewObjects();
            ClearGhostCompositeLikeOriginal();
            if (_ghostMeshRenderer != null) _ghostMeshRenderer.enabled = true;

            // Same family as C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal:
            // parent.position = base world point; mesh vertices are local sprite rect.
            float originalX = realX / 16.0f;
            float originalY = realY / 16.0f;
            Vector3 basePos = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(originalX, originalY);
            basePos.y += GhostYOffset;
            _ghostRoot.transform.position = basePos;

            float s = 1.0f / 16.0f;
            int dx = _buildMd.GetPivotDx(_ghostFrame);
            int dy = _buildMd.GetPivotDy(_ghostFrame);

            float lx = dx * s;
            float rx = (dx + _ghostTexture.width) * s;
            float ty = -dy * s;
            float by = -(dy + _ghostTexture.height) * s;

            _ghostMesh.Clear();
            _ghostMesh.vertices = new[]
            {
                new Vector3(lx, by, 0f),
                new Vector3(rx, by, 0f),
                new Vector3(rx, ty, 0f),
                new Vector3(lx, ty, 0f)
            };

            _ghostMesh.uv = new[]
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f)
            };
            _ghostMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _ghostMesh.RecalculateBounds();

            Material m = valid ? _ghostValidMat : _ghostInvalidMat;
            ConfigureGhostMaterialLikeOriginal(m, _ghostTexture, valid ? new Color(1.0f, 1.0f, 1.0f, 0.72f) : new Color(1.0f, 0.0f, 0.0f, 0.72f));
            _ghostMeshRenderer.sharedMaterial = m;
            _ghostMeshRenderer.sortingOrder = 32760;
            if (!_ghostRoot.activeSelf) _ghostRoot.SetActive(true);
        }

        private bool TryUpdateCompositeGhostLikeOriginal(C2BattleTerrainMode mode, int realX, int realY, bool valid)
        {
            if (mode == null || _ghostRoot == null || string.IsNullOrEmpty(_mdName))
                return false;

            if (_ghostCompositeReady &&
                string.Equals(_ghostCompositeMdName, _mdName, StringComparison.OrdinalIgnoreCase) &&
                _ghostCompositeNation == _nation)
            {
                if (_ghostCompositeRealX != realX || _ghostCompositeRealY != realY)
                    ShiftGhostCompositeLikeOriginal(mode, _ghostCompositeRealX, _ghostCompositeRealY, realX, realY);
                if (_ghostCompositeValid != valid)
                    ApplyGhostCompositeTintLikeOriginal(_ghostRoot.transform, valid);

                _ghostCompositeRealX = realX;
                _ghostCompositeRealY = realY;
                _ghostCompositeValid = valid;
                if (_ghostMeshRenderer != null) _ghostMeshRenderer.enabled = false;
                if (!_ghostRoot.activeSelf) _ghostRoot.SetActive(true);
                return true;
            }

            string audit;
            bool ok = mode.C2BuildRuntimeDrawGhostCompositeLikeOriginal(
                _ghostRoot.transform,
                _mdName,
                _nation,
                realX,
                realY,
                valid,
                "placement-preview-v39",
                out audit);

            _ghostCompositeAudit = audit;
            _ghostCompositeReady = ok;
            _ghostCompositeRealX = realX;
            _ghostCompositeRealY = realY;
            _ghostCompositeValid = valid;
            _ghostCompositeMdName = _mdName;
            _ghostCompositeNation = _nation;

            if (!ok)
                return false;

            if (_ghostMeshRenderer != null) _ghostMeshRenderer.enabled = false;
            if (!_ghostRoot.activeSelf) _ghostRoot.SetActive(true);
            if (_root != null) _root.SetActive(false);
            return true;
        }

        private void ShiftGhostCompositeLikeOriginal(C2BattleTerrainMode mode, int oldRealX, int oldRealY, int newRealX, int newRealY)
        {
            if (mode == null || _ghostRoot == null)
                return;

            Vector3 oldWorld = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(oldRealX / 16.0f, oldRealY / 16.0f);
            Vector3 newWorld = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(newRealX / 16.0f, newRealY / 16.0f);
            Vector3 delta = newWorld - oldWorld;
            if (delta.sqrMagnitude <= 0.0000001f)
                return;

            Transform root = _ghostRoot.transform;
            for (int i = 0; i < root.childCount; i++)
                root.GetChild(i).position += delta;
        }

        private static void ApplyGhostCompositeTintLikeOriginal(Transform root, bool valid)
        {
            if (root == null) return;
            Color c = valid ? new Color(1.0f, 1.0f, 1.0f, 0.78f) : new Color(1.0f, 0.0f, 0.0f, 0.62f);
            Renderer[] rr = root.GetComponentsInChildren<Renderer>(true);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; rr != null && i < rr.Length; i++)
            {
                Renderer rend = rr[i];
                if (rend == null) continue;
                rend.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_BaseColor", c);
                rend.SetPropertyBlock(block);
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        private static Texture2D TryLoadGpFrameTextureLikeOriginal(string package, string mdPath, int spriteId, out string audit)
        {
            audit = "not_started";
            if (string.IsNullOrWhiteSpace(package))
            {
                audit = "empty_package";
                return null;
            }

            try
            {
                Type bridgeType = ResolveMelinojaBridgeTypeLikeOriginal();
                if (bridgeType == null)
                {
                    audit = "Melinoja bridge not found";
                    return null;
                }

                string[] exts = { ".g17", ".G17", ".g16", ".G16", ".g2d", ".G2D" };
                List<string> candidates = BuildVisualCandidatesLikeOriginal(package, mdPath, exts);
                string logicalPkg = CleanPackageNameLikeOriginal(package);
                List<string> tried = new List<string>(16);

                for (int c = 0; c < candidates.Count; c++)
                {
                    string key = candidates[c];
                    bool isFile = File.Exists(key);
                    if (!isFile && c > 0) continue;

                    Texture2D tex;
                    string keyAudit;
                    if (TryLoadBridgeKeyFrameLikeOriginal(bridgeType, key, spriteId, out tex, out keyAudit))
                    {
                        audit = "ok key='" + key + "' logical='" + logicalPkg + "' sprite=" + spriteId.ToString(CultureInfo.InvariantCulture) + " " + keyAudit;
                        return tex;
                    }

                    if (tried.Count < 14 && !string.IsNullOrEmpty(keyAudit))
                        tried.Add((isFile ? "EXISTS:" : "LOGICAL:") + key + " => " + keyAudit);
                }

                audit = "frame_decode_failed sprite=" + spriteId.ToString(CultureInfo.InvariantCulture) +
                        " logical='" + logicalPkg + "' mdPath='" + (mdPath ?? string.Empty) + "'" +
                        " candidates=" + candidates.Count.ToString(CultureInfo.InvariantCulture) +
                        " tried=" + string.Join(" || ", tried.ToArray());
                return null;
            }
            catch (Exception ex)
            {
                audit = "exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static bool TryLoadBridgeKeyFrameLikeOriginal(Type bridgeType, string key, int spriteId, out Texture2D tex, out string audit)
        {
            tex = null;
            audit = string.Empty;
            if (bridgeType == null || string.IsNullOrWhiteSpace(key))
            {
                audit = "empty_bridge_or_key";
                return false;
            }

            string loadAudit = string.Empty;
            string[] loadNames = { "LoadG17ToMemory", "LoadGPToMemory", "LoadPackageToMemory", "LoadG16ToMemory" };
            for (int i = 0; i < loadNames.Length; i++)
            {
                MethodInfo load = bridgeType.GetMethod(loadNames[i], BindingFlags.Public | BindingFlags.Static);
                if (load == null) continue;
                ParameterInfo[] ps = load.GetParameters();
                object[] args = null;
                if (ps.Length == 3) args = new object[] { key, null, false };
                else if (ps.Length == 2) args = new object[] { key, null };
                else if (ps.Length == 1) args = new object[] { key };
                if (args == null) continue;

                try
                {
                    object result = load.Invoke(null, args);
                    bool okLoad = result is bool ? (bool)result : true;
                    string err = args.Length > 1 ? args[1] as string : string.Empty;
                    loadAudit += loadNames[i] + "=" + (okLoad ? "true" : "false") + (string.IsNullOrEmpty(err) ? "" : ":" + err) + ";";
                    if (okLoad) break;
                }
                catch (Exception ex)
                {
                    loadAudit += loadNames[i] + "=EX:" + ex.GetType().Name + ";";
                }
            }

            string[] frameNames =
            {
                "TryGetG17FrameRGBAExact",
                "TryGetGPFrameRGBAExact",
                "TryGetPackageFrameRGBAExact",
                "TryGetFrameRGBAExact",
                "TryGetG16FrameRGBAExact",
                "TryGetG17FrameRGBA",
                "TryGetGPFrameRGBA",
                "TryGetPackageFrameRGBA",
                "TryGetFrameRGBA",
                "TryGetG16FrameRGBA"
            };

            string frameAudit = string.Empty;
            for (int i = 0; i < frameNames.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(frameNames[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null) continue;
                ParameterInfo[] ps = mi.GetParameters();
                if (ps.Length != 6)
                {
                    frameAudit += frameNames[i] + ":bad_sig" + ps.Length.ToString(CultureInfo.InvariantCulture) + ";";
                    continue;
                }

                try
                {
                    object[] args = { key, spriteId, 0, 0, null, null };
                    object result = mi.Invoke(null, args);
                    if (!(result is bool) || !(bool)result)
                    {
                        string err = args[5] as string;
                        frameAudit += frameNames[i] + "=false" + (string.IsNullOrEmpty(err) ? "" : ":" + err) + ";";
                        continue;
                    }

                    int w = args[2] is int ? (int)args[2] : 0;
                    int h = args[3] is int ? (int)args[3] : 0;
                    byte[] rgba = args[4] as byte[];
                    if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                    {
                        frameAudit += frameNames[i] + ":bad_rgba;";
                        continue;
                    }

                    tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                    tex.name = "C2_BuildPreviewGhost_" + SafeName(key) + "_" + spriteId.ToString(CultureInfo.InvariantCulture);
                    tex.LoadRawTextureData(rgba);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply(false, false);

                    string alphaRepairAudit;
                    ApplyGhostBorderWhiteAlphaRepairLikeOriginal(tex, out alphaRepairAudit);

                    audit = "via=" + frameNames[i] + " size=" + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture) +
                            " alphaRepair=[" + alphaRepairAudit + "] load=" + loadAudit;
                    return true;
                }
                catch (Exception ex)
                {
                    frameAudit += frameNames[i] + "=EX:" + ex.GetType().Name + ";";
                }
            }

            audit = "load=" + loadAudit + " frames=" + frameAudit;
            return false;
        }

        private static void ApplyGhostBorderWhiteAlphaRepairLikeOriginal(Texture2D tex, out string audit)
        {
            audit = "none";
            if (tex == null)
            {
                audit = "no_texture";
                return;
            }

            try
            {
                int w = tex.width;
                int h = tex.height;
                if (w <= 2 || h <= 2)
                {
                    audit = "small";
                    return;
                }

                Color32[] px = tex.GetPixels32();
                if (px == null || px.Length != w * h)
                {
                    audit = "bad_pixels";
                    return;
                }

                Color32 c00 = px[0];
                Color32 c10 = px[w - 1];
                Color32 c01 = px[(h - 1) * w];
                Color32 c11 = px[h * w - 1];

                bool[] seen = new bool[px.Length];
                int[] queue = new int[px.Length];
                int head = 0;
                int tail = 0;

                Action<int> enqueue = idx =>
                {
                    if ((uint)idx >= (uint)px.Length) return;
                    if (seen[idx]) return;
                    if (!IsGhostBorderMatteLikeOriginal(px[idx], c00, c10, c01, c11)) return;
                    seen[idx] = true;
                    queue[tail++] = idx;
                };

                for (int x = 0; x < w; x++)
                {
                    enqueue(x);
                    enqueue((h - 1) * w + x);
                }
                for (int y = 0; y < h; y++)
                {
                    enqueue(y * w);
                    enqueue(y * w + (w - 1));
                }

                while (head < tail)
                {
                    int idx = queue[head++];
                    int x = idx % w;
                    int y = idx / w;
                    if (x > 0) enqueue(idx - 1);
                    if (x + 1 < w) enqueue(idx + 1);
                    if (y > 0) enqueue(idx - w);
                    if (y + 1 < h) enqueue(idx + w);
                }

                int cleared = 0;
                for (int i = 0; i < tail; i++)
                {
                    int idx = queue[i];
                    Color32 c = px[idx];
                    if (c.a == 0) continue;
                    c.a = 0;
                    px[idx] = c;
                    cleared++;
                }

                if (cleared > 0)
                {
                    tex.SetPixels32(px);
                    tex.Apply(false, false);
                }

                float pct = (cleared * 100.0f) / (w * h);
                audit = "borderMatteFlood cleared=" + cleared.ToString(CultureInfo.InvariantCulture) +
                        " total=" + (w * h).ToString(CultureInfo.InvariantCulture) +
                        " pct=" + pct.ToString("0.00", CultureInfo.InvariantCulture) +
                        " corners=(" + ColorAuditLikeOriginal(c00) + "|" + ColorAuditLikeOriginal(c10) + "|" + ColorAuditLikeOriginal(c01) + "|" + ColorAuditLikeOriginal(c11) + ")";
            }
            catch (Exception ex)
            {
                audit = "exception=" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private static bool IsGhostBorderMatteLikeOriginal(Color32 c, Color32 c00, Color32 c10, Color32 c01, Color32 c11)
        {
            if (c.a <= 8) return false;

            int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            int min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            int diff = max - min;

            // G16/G17 cached building frames can carry a pale matte that is not pure 255 white.
            // Only connected border matte is cleared, so inner white details remain.
            if (max >= 168 && min >= 148 && diff <= 90)
                return true;

            if (ColorDistanceLikeOriginal(c, c00) <= 70) return true;
            if (ColorDistanceLikeOriginal(c, c10) <= 70) return true;
            if (ColorDistanceLikeOriginal(c, c01) <= 70) return true;
            if (ColorDistanceLikeOriginal(c, c11) <= 70) return true;

            return false;
        }

        private static int ColorDistanceLikeOriginal(Color32 a, Color32 b)
        {
            int dr = a.r - b.r;
            int dg = a.g - b.g;
            int db = a.b - b.b;
            return Mathf.Abs(dr) + Mathf.Abs(dg) + Mathf.Abs(db);
        }

        private static string ColorAuditLikeOriginal(Color32 c)
        {
            return c.r.ToString(CultureInfo.InvariantCulture) + "," +
                   c.g.ToString(CultureInfo.InvariantCulture) + "," +
                   c.b.ToString(CultureInfo.InvariantCulture) + "," +
                   c.a.ToString(CultureInfo.InvariantCulture);
        }

        private static List<string> BuildVisualCandidatesLikeOriginal(string package, string mdPath, string[] exts)
        {
            var res = new List<string>();
            Action<string> add = p =>
            {
                if (string.IsNullOrEmpty(p)) return;
                for (int i = 0; i < res.Count; i++)
                    if (string.Equals(res[i], p, StringComparison.OrdinalIgnoreCase))
                        return;
                res.Add(p);
            };

            string pkg = CleanPackageNameLikeOriginal(package);
            string pkgNoExt = Path.ChangeExtension(pkg, null);
            string flatPkg = (pkgNoExt ?? string.Empty).Replace('\\', '_').Replace('/', '_');
            string flatPkgUpper = flatPkg.ToUpperInvariant();
            string barePkg = Path.GetFileName(pkgNoExt ?? string.Empty);
            string barePkgUpper = barePkg.ToUpperInvariant();

            add(pkg); // logical key first, for bridge builds that know package names.

            List<string> roots = DataRootsLikeOriginal();
            if (!string.IsNullOrEmpty(mdPath))
            {
                string mdDir = Path.GetDirectoryName(mdPath);
                if (!string.IsNullOrEmpty(mdDir))
                    roots.Insert(0, mdDir);
            }

            for (int e = 0; e < exts.Length; e++)
            {
                string ext = exts[e];
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", flatPkgUpper + ext));
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", flatPkg + ext));
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", barePkgUpper + ext));
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", barePkg + ext));
            }

            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root)) continue;
                for (int e = 0; e < exts.Length; e++)
                {
                    string ext = exts[e];
                    add(Path.Combine(root, flatPkgUpper + ext));
                    add(Path.Combine(root, flatPkg + ext));
                    add(Path.Combine(root, barePkgUpper + ext));
                    add(Path.Combine(root, barePkg + ext));
                    add(Path.Combine(root, "Cash", flatPkgUpper + ext));
                    add(Path.Combine(root, "Cash", flatPkg + ext));
                    add(Path.Combine(root, "UnitsG17", barePkg + ext));
                    add(Path.Combine(root, "UnitsG17", barePkgUpper + ext));
                    add(Path.Combine(root, "Data", "UnitsG17", barePkg + ext));
                    add(Path.Combine(root, "Data", "UnitsG17", barePkgUpper + ext));
                    add(Path.Combine(root, "Data", "Cash", flatPkgUpper + ext));
                    add(Path.Combine(root, "Data1", "Cash", flatPkgUpper + ext));
                    add(Path.Combine(root, pkgNoExt + ext));
                    add(Path.Combine(root, pkg + ext));
                    add(Path.Combine(root, "Units", pkgNoExt + ext));
                    add(Path.Combine(root, "UnitsMD", pkgNoExt + ext));
                    add(Path.Combine(root, "UnitsMD", "Units", pkgNoExt + ext));
                    add(Path.Combine(root, "Sprites", pkgNoExt + ext));
                    add(Path.Combine(root, "G16", pkgNoExt + ext));
                    add(Path.Combine(root, "G2D", pkgNoExt + ext));
                    add(Path.Combine(root, "Cash", pkgNoExt + ext));
                    add(Path.Combine(root, "Data", pkgNoExt + ext));
                    add(Path.Combine(root, "Data", "Cash", pkgNoExt + ext));
                    add(Path.Combine(root, "Data1", pkgNoExt + ext));
                    add(Path.Combine(root, "Data1", "Cash", pkgNoExt + ext));
                    add(Path.Combine(root, "Resources", pkgNoExt + ext));
                    add(Path.Combine(root, "Resources", "Cash", pkgNoExt + ext));
                }
            }

            return res;
        }

        private static List<string> DataRootsLikeOriginal()
        {
            var roots = new List<string>();
            Action<string> add = p =>
            {
                if (string.IsNullOrEmpty(p)) return;
                for (int i = 0; i < roots.Count; i++)
                    if (string.Equals(roots[i], p, StringComparison.OrdinalIgnoreCase))
                        return;
                roots.Add(p);
            };

            add(@"C:\GSC Game World\Cossacks II\Data\Cash");
            add(@"C:\GSC Game World\Cossacks II\Data\UnitsG17");
            add(@"C:\GSC Game World\Cossacks II\Data\UnitsMD");
            add(@"C:\GSC Game World\Cossacks II\Data");
            add(Application.streamingAssetsPath);
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "Cash"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data1"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data1", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources"));
            add(Path.Combine(Application.dataPath, "Resources", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources", "Data"));
            add(Path.Combine(Application.dataPath, "Resources", "Data", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources", "Data1"));
            add(Path.Combine(Application.dataPath, "Resources", "Data1", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources", "UnitsMD"));
            add(Path.Combine(Application.dataPath, "Resources", "UnitsMD", "Units"));
            add(Path.Combine(Application.dataPath, "Resources", "Units"));
            add(Path.Combine(Application.dataPath, "Resources", "UnitsG17"));
            add(Path.Combine(Application.dataPath, "Resources", "G2D"));
            add(Path.Combine(Application.dataPath, "Resources", "Data", "UnitsG17"));
            add(Path.Combine(Application.dataPath, "Resources", "Data1", "UnitsG17"));
            add(Path.Combine(Application.dataPath, "Resources", "Models"));
            add(Path.Combine(Application.dataPath, "..", "Data"));
            add(Path.Combine(Application.dataPath, "..", "Cossacks2", "Data"));
            add(@"C:\GSC Game World\Cossacks II\Data");
            add(@"C:\GSC Game World\Cossacks II\Data\Cash");
            add(@"C:\GSC Game World\Cossacks II\Data\UnitsG17");
            add(@"C:\GSC Game World\Cossacks II\Data1");
            add(@"C:\GSC Game World\Cossacks II\Data1\Cash");
            add(@"C:\Program Files (x86)\GSC Game World\Cossacks II\Data");
            add(@"C:\Games\Cossacks II\Data");
            return roots;
        }

        private static string CleanPackageNameLikeOriginal(string package)
        {
            if (string.IsNullOrWhiteSpace(package)) return string.Empty;
            string p = package.Trim().Trim('"', '\'');
            p = p.Replace('/', '\\');
            string ext = Path.GetExtension(p);
            if (!string.IsNullOrEmpty(ext)) p = p.Substring(0, p.Length - ext.Length);
            return p;
        }

        private static Type ResolveMelinojaBridgeTypeLikeOriginal()
        {
            string[] names =
            {
                "TemnyLessCodec.CodecFacade, Melinoja",
                "TemnyLessCodec.CodecFacade",
                "Melinoja.CodecFacade, Melinoja",
                "Melinoja.CodecFacade"
            };

            for (int i = 0; i < names.Length; i++)
            {
                Type t = Type.GetType(names[i], false);
                if (t != null) return t;
            }

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Assembly asm = assemblies[i];
                    if (asm == null) continue;
                    Type t = asm.GetType("TemnyLessCodec.CodecFacade", false);
                    if (t != null) return t;
                    t = asm.GetType("Melinoja.CodecFacade", false);
                    if (t != null) return t;
                }
            }
            catch { }

            return null;
        }

        private static string SafeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "empty";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }
            return sb.ToString();
        }

        private bool TryMouseWorldLikeOriginal(out Vector3 world, out Camera usedCamera)
        {
            world = Vector3.zero;
            usedCamera = null;

            Vector2 mouse = MousePositionBottomLeftLikeOriginal();
            Camera[] cams = BestPickCamerasLikeOriginal();
            if (cams == null || cams.Length == 0) return false;

            float planeY = SelectedPlaneYLikeOriginal();

            Plane plane = new Plane(Vector3.up, new Vector3(0.0f, planeY, 0.0f));
            for (int i = 0; i < cams.Length; i++)
            {
                Camera cam = cams[i];
                if (cam == null || !cam.isActiveAndEnabled) continue;
                Ray ray = cam.ScreenPointToRay(mouse);
                float enter;
                if (!plane.Raycast(ray, out enter) || enter < 0.0f) continue;
                world = ray.GetPoint(enter);
                world.y = planeY;
                usedCamera = cam;
                return true;
            }

            return false;
        }

        private static Vector2 MousePositionBottomLeftLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private static bool LeftPressedThisFrameLikeOriginal()
        {
            // In build mode original clicks are world-placement clicks, not UI clicks.
            // V28 filtered EventSystem.IsPointerOverGameObject() and could suppress CONFIRM.
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool LeftHeldLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButton(0);
#else
            return false;
#endif
        }

        private static bool CancelPressedThisFrameLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) return true;
            if (UnityEngine.Input.GetMouseButtonDown(1)) return true;
#endif
            return false;
        }

        private static bool ShiftHeldLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        private static Camera[] BestPickCamerasLikeOriginal()
        {
            List<Camera> result = new List<Camera>(4);
            Camera[] all = Camera.allCameras;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; all != null && i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled || result.Contains(c)) continue;
                    string n = c.name ?? string.Empty;
                    if (pass == 0 && n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0) result.Add(c);
                    if (pass == 1 && n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0) result.Add(c);
                    if (pass == 2 && c == Camera.main) result.Add(c);
                }
            }
            if (result.Count == 0 && Camera.main != null) result.Add(Camera.main);
            return result.ToArray();
        }

        private static float SelectedPlaneYLikeOriginal()
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            float sum = 0.0f;
            int count = 0;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.IsSelected) continue;
                sum += u.transform.position.y;
                count++;
            }
            return count > 0 ? sum / count : 0.0f;
        }

        private C2BattleTerrainMode GetBattleTerrainModeCached()
        {
            float now = Time.realtimeSinceStartup;
            if (_mode != null && now < _nextModeLookup)
                return _mode;

            _nextModeLookup = now + 1.0f;
            _mode = FindObjectOfType<C2BattleTerrainMode>();
            return _mode;
        }

        private void EnsurePreviewObjects()
        {
            if (_root != null && _meshFilter != null && _meshRenderer != null && _mesh != null &&
                _checkOverlayRoot != null && _checkOverlayMeshFilter != null && _checkOverlayMeshRenderer != null && _checkOverlayMesh != null)
                return;

            _root = new GameObject("C2_BuildingPlacementPreview_V28_Mesh");
            _root.transform.SetParent(transform, false);
            _meshFilter = _root.AddComponent<MeshFilter>();
            _meshRenderer = _root.AddComponent<MeshRenderer>();
            _mesh = new Mesh();
            _mesh.name = "C2_BuildingPlacementPreview_V28_Quad";
            _meshFilter.sharedMesh = _mesh;

            _validMat = CreatePreviewMaterialLikeOriginal("C2_BuildPreview_Valid_V32", new Color(1.0f, 1.0f, 1.0f, 0.08f));
            _invalidMat = CreatePreviewMaterialLikeOriginal("C2_BuildPreview_Invalid_V32", new Color(1.0f, 0.0f, 0.0f, 0.18f));
            _meshRenderer.sharedMaterial = _validMat;

            _ghostRoot = new GameObject("C2_BuildingPlacementPreview_V32_Ghost");
            _ghostRoot.transform.SetParent(transform, false);
            _ghostMeshFilter = _ghostRoot.AddComponent<MeshFilter>();
            _ghostMeshRenderer = _ghostRoot.AddComponent<MeshRenderer>();
            _ghostMesh = new Mesh();
            _ghostMesh.name = "C2_BuildingPlacementPreview_V32_GhostMesh";
            _ghostMeshFilter.sharedMesh = _ghostMesh;
            _ghostValidMat = CreatePreviewMaterialLikeOriginal("C2_BuildGhost_Valid_V32", new Color(1.0f, 1.0f, 1.0f, 0.72f));
            _ghostInvalidMat = CreatePreviewMaterialLikeOriginal("C2_BuildGhost_Invalid_V32", new Color(1.0f, 0.0f, 0.0f, 0.72f));
            _ghostMeshRenderer.sharedMaterial = _ghostValidMat;
            _ghostMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ghostMeshRenderer.receiveShadows = false;
            _ghostMeshRenderer.sortingOrder = 32760;

            _checkOverlayRoot = new GameObject("C2_BuildingPlacementPreview_CHECKPOINTS_Q_Overlay_V58");
            _checkOverlayRoot.transform.SetParent(transform, false);
            _checkOverlayMeshFilter = _checkOverlayRoot.AddComponent<MeshFilter>();
            _checkOverlayMeshRenderer = _checkOverlayRoot.AddComponent<MeshRenderer>();
            _checkOverlayMesh = new Mesh();
            _checkOverlayMesh.name = "C2_BuildingPlacementPreview_CHECKPOINTS_V58_Mesh";
            _checkOverlayMeshFilter.sharedMesh = _checkOverlayMesh;
            _checkOverlayMat = CreatePreviewMaterialLikeOriginal("C2_BuildPreview_Checkpoints_Yellow_V58", new Color(1.0f, 0.86f, 0.0f, 0.36f));
            _checkOverlayMeshRenderer.sharedMaterial = _checkOverlayMat;
            _checkOverlayMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _checkOverlayMeshRenderer.receiveShadows = false;
            _checkOverlayMeshRenderer.sortingOrder = 32762;
            _checkOverlayRoot.SetActive(false);
        }

        private static Material CreatePreviewMaterialLikeOriginal(string name, Color color)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (sh == null) sh = Shader.Find("Cossacks2Bridge/SettlementBuildingSpriteV23LikeOriginal");
            if (sh == null) sh = Shader.Find("Cossacks2Bridge/WallObjectSpriteV31ExactCutout");
            if (sh == null) sh = Shader.Find("Standard");

            Material m = new Material(sh);
            m.name = name;
            m.color = color;
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            if (m.HasProperty("_Cull")) m.SetInt("_Cull", 0);
            m.renderQueue = 5000;
            return m;
        }

        private static void ConfigureGhostMaterialLikeOriginal(Material mat, Texture2D tex, Color color)
        {
            if (mat == null) return;
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            mat.color = color;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", 0.05f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.05f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0.0f);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 5000;
        }

        private void UpdateCheckpointsDebugOverlayV58LikeOriginal()
        {
            EnsurePreviewObjects();

            bool visible = _active &&
                           C2BuildingPassabilityOverlayHotkeyLikeOriginal.CurrentModeLikeOriginal == 2 &&
                           _buildMd != null &&
                           _buildMd.CheckPoints != null &&
                           _buildMd.CheckPoints.Count > 0;

            if (!visible)
            {
                if (_checkOverlayRoot != null && _checkOverlayRoot.activeSelf)
                    _checkOverlayRoot.SetActive(false);
                return;
            }

            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            if (mode == null)
            {
                if (_checkOverlayRoot != null && _checkOverlayRoot.activeSelf)
                    _checkOverlayRoot.SetActive(false);
                return;
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            var verts = new List<Vector3>((_buildMd.CheckPoints.Count + 512) * 4);
            var colors = new List<Color32>((_buildMd.CheckPoints.Count + 512) * 4);
            var tris = new List<int>((_buildMd.CheckPoints.Count + 512) * 6);
            Color32 fillYellow = new Color32(255, 220, 0, 82);
            Color32 borderYellow = new Color32(255, 246, 0, 235);

            int minH = int.MaxValue;
            int maxH = int.MinValue;
            int minHX = int.MinValue;
            int minHY = int.MinValue;
            int maxHX = int.MinValue;
            int maxHY = int.MinValue;
            int samples = 0;

            for (int i = 0; i < _buildMd.CheckPoints.Count; i++)
            {
                Vector2Int p = _buildMd.CheckPoints[i];
                int gx = _lastFootprintCellX + p.x;
                int gy = _lastFootprintCellY + p.y;

                if (gx < minX) minX = gx;
                if (gy < minY) minY = gy;
                if (gx > maxX) maxX = gx;
                if (gy > maxY) maxY = gy;

                int h;
                if (TrySampleOriginalHeightAtCellLikeOriginal(mode, gx, gy, out h))
                {
                    samples++;
                    if (h < minH)
                    {
                        minH = h;
                        minHX = gx;
                        minHY = gy;
                    }
                    if (h > maxH)
                    {
                        maxH = h;
                        maxHX = gx;
                        maxHY = gy;
                    }
                }

                AddGhostCheckCellQuadV58LikeOriginal(mode, verts, colors, tris, gx, gy, fillYellow);
            }

            if (minX <= maxX && minY <= maxY)
                AddGhostCheckRectBorderV60LikeOriginal(mode, verts, colors, tris, minX, minY, maxX, maxY, borderYellow);

            // V61 debug: show the exact min/max height cells that decide slopeHeight.
            // Blue = lowest sampled CHECKPOINTS height, Red = highest sampled CHECKPOINTS height.
            if (samples > 0 && minHX != int.MinValue && maxHX != int.MinValue)
            {
                AddGhostCheckMarkerV61LikeOriginal(mode, verts, colors, tris, minHX, minHY, new Color32(0, 150, 255, 245));
                AddGhostCheckMarkerV61LikeOriginal(mode, verts, colors, tris, maxHX, maxHY, new Color32(255, 0, 0, 245));
            }

            _checkOverlayMesh.Clear();
            if (verts.Count > 65000) _checkOverlayMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _checkOverlayMesh.SetVertices(verts);
            _checkOverlayMesh.SetColors(colors);
            _checkOverlayMesh.SetTriangles(tris, 0);
            _checkOverlayMesh.RecalculateBounds();

            if (_checkOverlayRoot != null && !_checkOverlayRoot.activeSelf)
                _checkOverlayRoot.SetActive(true);
        }

        private static void AddGhostCheckRectBorderV60LikeOriginal(
            C2BattleTerrainMode mode,
            List<Vector3> verts,
            List<Color32> colors,
            List<int> tris,
            int minX,
            int minY,
            int maxX,
            int maxY,
            Color32 color)
        {
            if (mode == null || verts == null || colors == null || tris == null) return;

            int bx0 = minX - 1;
            int by0 = minY - 1;
            int bx1 = maxX + 1;
            int by1 = maxY + 1;

            for (int x = bx0; x <= bx1; x++)
            {
                AddGhostCheckCellQuadV58LikeOriginal(mode, verts, colors, tris, x, by0, color);
                AddGhostCheckCellQuadV58LikeOriginal(mode, verts, colors, tris, x, by1, color);
            }

            for (int y = by0 + 1; y <= by1 - 1; y++)
            {
                AddGhostCheckCellQuadV58LikeOriginal(mode, verts, colors, tris, bx0, y, color);
                AddGhostCheckCellQuadV58LikeOriginal(mode, verts, colors, tris, bx1, y, color);
            }
        }

        private static void AddGhostCheckMarkerV61LikeOriginal(
            C2BattleTerrainMode mode,
            List<Vector3> verts,
            List<Color32> colors,
            List<int> tris,
            int cx,
            int cy,
            Color32 color)
        {
            // 5x5 marker so the decisive height sample is visible under/around the ghost building.
            for (int y = cy - 2; y <= cy + 2; y++)
            {
                for (int x = cx - 2; x <= cx + 2; x++)
                    AddGhostCheckCellQuadV58LikeOriginal(mode, verts, colors, tris, x, y, color);
            }
        }

        private static void AddGhostCheckCellQuadV58LikeOriginal(
            C2BattleTerrainMode mode,
            List<Vector3> verts,
            List<Color32> colors,
            List<int> tris,
            int gx,
            int gy,
            Color32 color)
        {
            if (mode == null || verts == null || colors == null || tris == null) return;

            Vector3 a = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(gx * 16.0f, gy * 16.0f);
            Vector3 b = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal((gx + 1) * 16.0f, gy * 16.0f);
            Vector3 c = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal((gx + 1) * 16.0f, (gy + 1) * 16.0f);
            Vector3 d = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(gx * 16.0f, (gy + 1) * 16.0f);

            a.y += GhostYOffset + 0.14f;
            b.y += GhostYOffset + 0.14f;
            c.y += GhostYOffset + 0.14f;
            d.y += GhostYOffset + 0.14f;

            int baseIndex = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 3);
        }

        private void SetPreviewVisible(bool visible)
        {
            bool hasGhost = HasGhostLikeOriginal();
            bool footprintVisible = visible && !hasGhost && !_lastValid;
            if (_root != null && _root.activeSelf != footprintVisible)
                _root.SetActive(footprintVisible);
            bool ghostVisible = visible && hasGhost;
            if (_ghostRoot != null && _ghostRoot.activeSelf != ghostVisible)
                _ghostRoot.SetActive(ghostVisible);
        }

        private void StopPreview()
        {
            _active = false;
            C2GameplayHudV1.C2GameplayHudV28InvalidateBuildModeLikeOriginal();
            SetPreviewVisible(false);
            if (_checkOverlayRoot != null) _checkOverlayRoot.SetActive(false);
            ClearGhostCompositeLikeOriginal();
            _runtimeBuildingBounds.Clear();
            _runtimeRoadBounds.Clear();
            _builderSnapshotV44.Clear();
        }

        private bool HasGhostLikeOriginal()
        {
            return _ghostCompositeReady || _ghostTexture != null;
        }

        private void ClearGhostCompositeLikeOriginal()
        {
            _ghostCompositeReady = false;
            _ghostCompositeAudit = "cleared";
            _ghostCompositeRealX = int.MinValue;
            _ghostCompositeRealY = int.MinValue;
            _ghostCompositeValid = false;
            _ghostCompositeMdName = string.Empty;
            _ghostCompositeNation = int.MinValue;

            if (_ghostRoot == null) return;
            for (int i = _ghostRoot.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = _ghostRoot.transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private struct C2PlacementCheckV27
        {
            public bool Valid;
            public int BlockedCells;
            public int RoadHits;
            public int WaterHits;
            public int BuildingHits;
            public int MapBoundsHits;
            public int WoodHits;
            public int StoneOrOreHits;
            public int FoodHits;
            public int HeightDelta;
            public int HeightDeltaRaw;
            public int HeightMin;
            public int HeightMax;
            public int CheckPointCount;
            public int CheckSamples;
            public int CheckMapBoundsHits;
            public int CheckMinCellX;
            public int CheckMinCellY;
            public int CheckMaxCellX;
            public int CheckMaxCellY;
            public int HeightMinCellX;
            public int HeightMinCellY;
            public int HeightMaxCellX;
            public int HeightMaxCellY;
            public string ResourceAudit;
            public string Reason;
            public int RealX;
            public int RealY;
            public int AnchorCellX;
            public int AnchorCellY;
            public int FootprintCellX;
            public int FootprintCellY;
            public bool SmartProbe;
            public bool SmartSnapped;
            public int SmartDx;
            public int SmartDy;
        }

        private sealed class C2WorkerBuildMdInfoV27
        {
            public bool Found;
            public string MdPath = string.Empty;
            public int WorkFrames;
            public int WorkRotations;
            public string WorkName = string.Empty;

            public string WorkAudit
            {
                get
                {
                    if (!Found) return "md_not_found";
                    if (WorkFrames <= 0) return "no_#WORK_or_@WORK";
                    return WorkName + " rotations=" + WorkRotations.ToString(CultureInfo.InvariantCulture) +
                           " frames=" + WorkFrames.ToString(CultureInfo.InvariantCulture);
                }
            }

            public static C2WorkerBuildMdInfoV27 Parse(string mdName)
            {
                C2WorkerBuildMdInfoV27 info = new C2WorkerBuildMdInfoV27();
                string path = C2BuildMdInfoV27.FindMdPath(mdName);
                info.MdPath = path ?? string.Empty;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return info;
                info.Found = true;

                string[] lines = C2BuildMdInfoV27.ReadMdLines(path);
                for (int i = 0; lines != null && i < lines.Length; i++)
                {
                    string line = C2BuildMdInfoV27.CleanLine(lines[i]);
                    string[] t = C2BuildMdInfoV27.SplitTokens(line);
                    if (t.Length < 3) continue;
                    string cmd = t[0].ToUpperInvariant();
                    if (cmd == "#WORK" || cmd == "@WORK" || cmd.StartsWith("#WORK", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("@WORK", StringComparison.OrdinalIgnoreCase))
                    {
                        int rot;
                        int frames;
                        int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out rot);
                        int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out frames);
                        if (frames > info.WorkFrames)
                        {
                            info.WorkName = t[0];
                            info.WorkRotations = rot;
                            info.WorkFrames = frames;
                        }
                    }
                }

                return info;
            }
        }

        private struct C2AnimFrameRefV28
        {
            public bool Valid;
            public string AnimName;
            public int FileRef;
            public int SpriteId;

            public string Audit
            {
                get
                {
                    return Valid
                        ? ((AnimName ?? string.Empty) + " fileRef=" + FileRef.ToString(CultureInfo.InvariantCulture) + " sprite=" + SpriteId.ToString(CultureInfo.InvariantCulture))
                        : "none";
                }
            }

            public C2AnimFrameRefV28(string animName, int fileRef, int spriteId)
            {
                Valid = true;
                AnimName = animName ?? string.Empty;
                FileRef = fileRef;
                SpriteId = spriteId;
            }
        }

        private sealed class C2BuildMdInfoV27
        {
            public bool Found;
            public string MdName = string.Empty;
            public string MdPath = string.Empty;
            public string Package = string.Empty;
            public int BuildStages;
            public int StandLoFrameCount;
            public readonly List<Vector2Int> LockPoints = new List<Vector2Int>();
            public readonly List<Vector2Int> CheckPoints = new List<Vector2Int>();
            public readonly List<Vector2Int> BuildLockPoints = new List<Vector2Int>();
            public readonly List<Vector2Int> BuildPoints = new List<Vector2Int>();
            public readonly int[] BuildLoFrames = new int[4];
            public readonly C2AnimFrameRefV28[] BuildLoFirstFrames = new C2AnimFrameRefV28[4];
            public C2AnimFrameRefV28 StandLoFirstFrame;
            public readonly Dictionary<int, string> RlcPackages = new Dictionary<int, string>();
            public readonly Dictionary<int, int> RlcDx = new Dictionary<int, int>();
            public readonly Dictionary<int, int> RlcDy = new Dictionary<int, int>();
            public int Dx;
            public int Dy;
            public int PicDx;
            public int PicDy;
            public int PicLx;
            public int PicLy;
            public bool HasBuildBar;
            public int BuildBarX;
            public int BuildBarY;
            public int BuildBarW;
            public int BuildBarH;
            public bool HasRectangle;
            public int RectX0;
            public int RectY0;
            public int RectX1;
            public int RectY1;
            public int CenterMX;
            public int CenterMY;
            public int BRadius;

            private static readonly Dictionary<string, C2BuildMdInfoV27> s_Cache = new Dictionary<string, C2BuildMdInfoV27>(StringComparer.OrdinalIgnoreCase);

            public string BuildBarAudit
            {
                get
                {
                    return HasBuildBar ? (BuildBarX + "," + BuildBarY + "," + BuildBarW + "," + BuildBarH) : "none";
                }
            }

            public string RectangleAudit
            {
                get
                {
                    return HasRectangle ? (RectX0 + "," + RectY0 + "," + RectX1 + "," + RectY1) : "none";
                }
            }

            public string BuildLoAudit
            {
                get
                {
                    return "#BUILDLO_0=" + BuildLoFrames[0].ToString(CultureInfo.InvariantCulture) +
                           " #BUILDLO_1=" + BuildLoFrames[1].ToString(CultureInfo.InvariantCulture) +
                           " #BUILDLO_2=" + BuildLoFrames[2].ToString(CultureInfo.InvariantCulture) +
                           " #BUILDLO_3=" + BuildLoFrames[3].ToString(CultureInfo.InvariantCulture);
                }
            }

            public static C2BuildMdInfoV27 ParseCached(string mdName)
            {
                string key = StripNationSuffix(mdName ?? string.Empty);
                if (string.IsNullOrEmpty(key)) key = mdName ?? string.Empty;
                C2BuildMdInfoV27 cached;
                if (s_Cache.TryGetValue(key, out cached)) return cached;
                cached = Parse(mdName);
                s_Cache[key] = cached;
                return cached;
            }

            public static C2BuildMdInfoV27 Parse(string mdName)
            {
                C2BuildMdInfoV27 info = new C2BuildMdInfoV27();
                info.MdName = mdName ?? string.Empty;
                string path = FindMdPath(mdName);
                info.MdPath = path ?? string.Empty;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return info;

                info.Found = true;
                string[] lines = ReadMdLines(path);
                for (int i = 0; lines != null && i < lines.Length; i++)
                {
                    string line = CleanLine(lines[i]);
                    if (line.Length == 0) continue;
                    string[] t = SplitTokens(line);
                    if (t.Length == 0) continue;

                    string cmd = t[0].ToUpperInvariant();

                    if ((cmd == "USERLC" || cmd == "USERLCEXT") && t.Length >= 3)
                    {
                        int fileRef;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out fileRef))
                        {
                            info.RlcPackages[fileRef] = t[2];
                            int shadowPos = -1;
                            for (int q = 3; q < t.Length; q++)
                            {
                                if (string.Equals(t[q], "SHADOW", StringComparison.OrdinalIgnoreCase))
                                {
                                    shadowPos = q;
                                    break;
                                }
                            }
                            int dx;
                            int dy;
                            if (shadowPos >= 0 && shadowPos + 2 < t.Length &&
                                int.TryParse(t[shadowPos + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dx) &&
                                int.TryParse(t[shadowPos + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out dy))
                            {
                                info.RlcDx[fileRef] = dx;
                                info.RlcDy[fileRef] = dy;
                            }
                        }

                        if (string.IsNullOrEmpty(info.Package))
                            info.Package = t[2];
                    }
                    else if (cmd == "SETANMPARAM" && t.Length >= 5)
                    {
                        int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.PicDx);
                        int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.PicDy);
                        int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.PicLx);
                        int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.PicLy);
                        info.Dx = info.PicDx;
                        info.Dy = info.PicDy;
                    }
                    else if (cmd == "BUILDSTAGES" && t.Length >= 2)
                    {
                        int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.BuildStages);
                    }
                    else if (cmd == "BUILDBAR" && t.Length >= 5)
                    {
                        info.HasBuildBar =
                            int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.BuildBarX) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.BuildBarY) &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.BuildBarW) &&
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.BuildBarH);
                    }
                    else if (cmd == "RECTANGLE" && t.Length >= 5)
                    {
                        info.HasRectangle =
                            int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.RectX0) &&
                            int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.RectY0) &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.RectX1) &&
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.RectY1);
                    }
                    else if (cmd == "LOCKPOINTS")
                    {
                        ParsePointList(t, info.LockPoints);
                    }
                    else if (cmd == "CHECKPOINTS")
                    {
                        ParsePointList(t, info.CheckPoints);
                    }
                    else if (cmd == "BUILDLOCKPOINTS")
                    {
                        ParsePointList(t, info.BuildLockPoints);
                    }
                    else if (cmd == "BUILDPOINTS")
                    {
                        ParsePointList(t, info.BuildPoints);
                    }
                    else if (cmd == "#STANDLO" && t.Length >= 3)
                    {
                        int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out info.StandLoFrameCount);
                        int fileRef;
                        int sprite;
                        if (t.Length >= 5 &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out fileRef) &&
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out sprite))
                            info.StandLoFirstFrame = new C2AnimFrameRefV28(cmd, fileRef, sprite);
                    }
                    else if (cmd.StartsWith("#BUILDLO_", StringComparison.OrdinalIgnoreCase) && t.Length >= 3)
                    {
                        int suffix = ParseSuffix(cmd);
                        if (suffix >= 0 && suffix < 4)
                        {
                            int frames;
                            if (int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out frames))
                                info.BuildLoFrames[suffix] = frames;

                            int fileRef;
                            int sprite;
                            if (t.Length >= 5 &&
                                int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out fileRef) &&
                                int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out sprite))
                                info.BuildLoFirstFrames[suffix] = new C2AnimFrameRefV28(cmd, fileRef, sprite);
                        }
                    }
                }

                info.ComputeCheckCenterAndRadiusLikeOriginal();
                return info;
            }

            private void ComputeCheckCenterAndRadiusLikeOriginal()
            {
                List<Vector2Int> pts = CheckPoints.Count > 0 ? CheckPoints : BuildLockPoints;
                if (pts == null || pts.Count == 0) pts = LockPoints;
                if (pts == null || pts.Count == 0) pts = FallbackBoxPoints();
                if (pts == null || pts.Count == 0) return;

                int sx = 0;
                int sy = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    sx += pts[i].x;
                    sy += pts[i].y;
                }

                CenterMX = sx / pts.Count;
                CenterMY = sy / pts.Count;
                int maxd = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    int dx = pts[i].x - CenterMX;
                    int dy = pts[i].y - CenterMY;
                    int d = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy));
                    if (d > maxd) maxd = d;
                }
                BRadius = maxd;
            }

            public bool TryGetOriginalBuildBarArea(int realX, int realY, out C2BuildBarAreaV38 area)
            {
                area = default(C2BuildBarAreaV38);
                if (!HasBuildBar) return false;

                int bx0 = PicDx + (BuildBarX << 4);
                int by0 = (PicDy + (BuildBarY << 3)) << 1;
                int bx1 = PicDx + (BuildBarW << 4);
                int by1 = (PicDy + (BuildBarH << 3)) << 1;

                area.X0 = realX + (bx0 << 4);
                area.Y0 = realY + (by0 << 4);
                area.X1 = realX + (bx1 << 4);
                area.Y1 = realY + (by1 << 4);
                area.Audit = "raw=" + BuildBarAudit + " off=(" + bx0.ToString(CultureInfo.InvariantCulture) + "," + by0.ToString(CultureInfo.InvariantCulture) + ")->(" + bx1.ToString(CultureInfo.InvariantCulture) + "," + by1.ToString(CultureInfo.InvariantCulture) + ")";
                return true;
            }

            public C2AnimFrameRefV28 SelectGhostFrameLikeOriginal()
            {
                // Placement preview must show the whole building silhouette, not the first construction-stage blob.
                // Use #STANDLO first. If absent, fall back from the latest #BUILDLO stage down to #BUILDLO_0.
                if (StandLoFirstFrame.Valid) return StandLoFirstFrame;
                for (int i = BuildLoFirstFrames.Length - 1; i >= 0; i--)
                    if (BuildLoFirstFrames[i].Valid) return BuildLoFirstFrames[i];
                return new C2AnimFrameRefV28();
            }

            public string ResolvePackageForFrame(int fileRef)
            {
                string p;
                if (RlcPackages != null && RlcPackages.TryGetValue(fileRef, out p)) return p;
                return Package ?? string.Empty;
            }

            public int GetPivotDx(C2AnimFrameRefV28 frame)
            {
                if (PicDx != 0 || PicDy != 0 || PicLx > 0 || PicLy > 0) return PicDx;
                int v;
                if (RlcDx != null && RlcDx.TryGetValue(frame.FileRef, out v)) return v;
                return Dx;
            }

            public int GetPivotDy(C2AnimFrameRefV28 frame)
            {
                if (PicDx != 0 || PicDy != 0 || PicLx > 0 || PicLy > 0) return PicDy;
                int v;
                if (RlcDy != null && RlcDy.TryGetValue(frame.FileRef, out v)) return v;
                return Dy;
            }

            public void GetPreviewBounds(out int minX, out int maxX, out int minY, out int maxY)
            {
                List<Vector2Int> pts = CheckPoints.Count > 0 ? CheckPoints : BuildLockPoints;
                if (pts == null || pts.Count == 0) pts = LockPoints;
                if (pts == null || pts.Count == 0) pts = FallbackBoxPoints();

                minX = 0;
                maxX = 0;
                minY = 0;
                maxY = 0;
                if (pts.Count > 0)
                {
                    minX = maxX = pts[0].x;
                    minY = maxY = pts[0].y;
                }

                for (int i = 1; i < pts.Count; i++)
                {
                    Vector2Int p = pts[i];
                    if (p.x < minX) minX = p.x;
                    if (p.x > maxX) maxX = p.x;
                    if (p.y < minY) minY = p.y;
                    if (p.y > maxY) maxY = p.y;
                }
            }

            public List<Vector2Int> FallbackBoxPoints()
            {
                var pts = new List<Vector2Int>();
                int minX = -4;
                int maxX = 4;
                int minY = -4;
                int maxY = 4;

                if (HasRectangle)
                {
                    minX = Mathf.FloorToInt(Mathf.Min(RectX0, RectX1) / 16.0f);
                    maxX = Mathf.CeilToInt(Mathf.Max(RectX0, RectX1) / 16.0f);
                    minY = Mathf.FloorToInt(Mathf.Min(RectY0, RectY1) / 16.0f);
                    maxY = Mathf.CeilToInt(Mathf.Max(RectY0, RectY1) / 16.0f);
                }

                for (int y = minY; y <= maxY; y++)
                    for (int x = minX; x <= maxX; x++)
                        pts.Add(new Vector2Int(x, y));
                return pts;
            }

            private static void ParsePointList(string[] t, List<Vector2Int> dst)
            {
                if (t == null || t.Length < 3 || dst == null) return;
                int start = 1;
                int declared;
                if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out declared))
                {
                    int remaining = t.Length - 2;
                    if (declared > 0 && remaining >= declared * 2)
                        start = 2;
                }

                for (int i = start; i + 1 < t.Length; i += 2)
                {
                    int x;
                    int y;
                    if (int.TryParse(t[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) &&
                        int.TryParse(t[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y))
                        dst.Add(new Vector2Int(x, y));
                }
            }

            private static int ParseSuffix(string cmd)
            {
                int p = cmd.LastIndexOf('_');
                if (p < 0 || p + 1 >= cmd.Length) return -1;
                int v;
                return int.TryParse(cmd.Substring(p + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : -1;
            }

            public static string[] ReadMdLines(string path)
            {
                try { return File.ReadAllLines(path, Encoding.GetEncoding(866)); }
                catch
                {
                    try { return File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
                    catch
                    {
                        try { return File.ReadAllLines(path); }
                        catch { return null; }
                    }
                }
            }

            public static string CleanLine(string line)
            {
                if (line == null) return string.Empty;
                int c = line.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) line = line.Substring(0, c);
                return line.Trim();
            }

            public static string[] SplitTokens(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return new string[0];
                return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            }

            public static string FindMdPath(string mdName)
            {
                string name = StripNationSuffix(mdName);
                if (string.IsNullOrEmpty(name)) return null;

                var roots = new List<string>();
                AddRoot(roots, @"C:\GSC Game World\Cossacks II\Data\UnitsMD");
                AddRoot(roots, @"C:\GSC Game World\Cossacks II\Data");
                AddRoot(roots, @"C:\GSC Game World\Cossacks II\Data1");
                AddRoot(roots, @"C:\Program Files (x86)\GSC Game World\Cossacks II\Data\UnitsMD");
                AddRoot(roots, @"C:\Program Files (x86)\GSC Game World\Cossacks II\Data");
                AddRoot(roots, Path.Combine(Application.dataPath, "..", "Data", "UnitsMD"));
                AddRoot(roots, Path.Combine(Application.dataPath, "..", "Data"));
                AddRoot(roots, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "UnitsMD"));
                AddRoot(roots, Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));
                AddRoot(roots, Path.Combine(Application.dataPath, "Resources", "UnitsMD"));
                AddRoot(roots, Path.Combine(Application.dataPath, "Resources"));

                for (int i = 0; i < roots.Count; i++)
                {
                    string root = roots[i];
                    if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;

                    string[] candidates =
                    {
                        Path.Combine(root, name + ".MD"),
                        Path.Combine(root, name + ".md"),
                        Path.Combine(root, "UnitsMD", name + ".MD"),
                        Path.Combine(root, "UnitsMD", name + ".md")
                    };

                    for (int c = 0; c < candidates.Length; c++)
                        if (File.Exists(candidates[c]))
                            return candidates[c];
                }

                return null;
            }

            private static void AddRoot(List<string> roots, string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                string full;
                try { full = Path.GetFullPath(path); }
                catch { full = path; }

                for (int i = 0; i < roots.Count; i++)
                    if (string.Equals(roots[i], full, StringComparison.OrdinalIgnoreCase))
                        return;

                roots.Add(full);
            }

            private static string StripNationSuffix(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                int p = s.IndexOf('(');
                if (p > 0) return s.Substring(0, p).Trim();
                return s.Trim();
            }
        }
    }
}
