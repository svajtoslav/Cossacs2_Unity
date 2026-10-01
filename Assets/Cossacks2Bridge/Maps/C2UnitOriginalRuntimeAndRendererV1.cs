using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // ========================================================================
    // C2UnitOriginalRuntimeAndRendererV1
    // ------------------------------------------------------------------------
    // First real unit runtime step after the audit/visual probe.
    //
    // Scope:
    //   - creates ALL real 3INU Unit records on the loaded map;
    //   - caches MD parse once per MD path;
    //   - uses TemnyLessViewer C2GpSystem cache path for GP/G2D/G16 frames;
    //   - advances CurrentFrameLong like original fixed-point animation time;
    //   - picks CurrentAnim from unit state (#REST/#STAND fallback for now);
    //   - uses RealDir from map record, no test hotkeys and no manual direction mutation;
    //   - adds a simple unit selection picker for these new runtime units.
    //
    // Still NOT complete gameplay:
    //   - no movement/pathing yet;
    //   - no attack/death state transitions yet;
    //   - no unit HUD integration yet;
    //   - no original selection ring art yet.
    // ========================================================================
    [DefaultExecutionOrder(32040)]
    public sealed partial class C2UnitOriginalRuntimeAndRendererV1 : MonoBehaviour
    {
        private const string LogPrefix = "[C2:UNIT ORIGINAL RUNTIME V4M_ORIGINAL_BORN_NO_CLEARANCE]";
        private const string RootName = "C2_UNIT_ORIGINAL_RUNTIME_V3";

        [Header("Startup")]
        public bool AutoRunOnceAfterMapLoad = true;
        public bool AutoFocusCameraOnFirstUnit = false;

        [Header("Original Animation State")]
        public string StandAnimationName = "#STAND";
        public string RestAnimationName = "#REST";
        public string MotionAnimationName = "#MOTION_L";
        public string WorkAnimationName = "#WORK";
        public string DeathAnimationName = "#DEATH";
        // COSSACKS2 advances CurrentFrameLong by GameSpeed (256) once per
        // 40 ms simulation quantum.  At normal speed that is one MD frame at
        // 25 Hz; motion clips are phase-locked to distance separately.
        public float DefaultAnimFps = 25.0f;
        public bool UseOriginalMotionFramesFromPath = true;
        public bool UseContinuousWorldDeltaForOriginalMotion = true;
        public float OriginalMotionDefaultSpeedOriginalPixelsPerSecond = 42.0f;
        public float OriginalMotionMinStopDistanceOriginalPixels = 1.0f;
        [Header("Original Unit Collision")]
        public bool UseOriginalUnitCollisionCheckPositionLikeOriginal = true;
        public bool UseOriginalUnitCollisionHardStepBlockLikeOriginal = true;
        public bool UseOriginalUnitSeparationForcesLikeOriginal = true;
        public bool UseOriginalProducedRallyFindUnitPositionLikeOriginal = true;
        public float OriginalUnitCollisionCellRealLikeOriginal = 512.0f;
        public float OriginalUnitCollisionMinRadiusRealLikeOriginal = 96.0f;
        public float OriginalUnitCollisionMaxRadiusRealLikeOriginal = 320.0f;
        public float OriginalUnitSeparationMaxPushRealPerFrameLikeOriginal = 96.0f;
        public int OriginalUnitSeparationMaxNeighborsLikeOriginal = 12;
        public int OriginalProducedRallyFindUnitPositionRingsLikeOriginal = 50;
        public bool UseOriginalIdleUnitYieldAsideLikeOriginal = true;
        public float OriginalIdleUnitYieldAsideStepRealLikeOriginal = 512.0f;
        public float OriginalIdleUnitYieldAsideCooldownSecondsLikeOriginal = 0.45f;
        public bool UseOriginalMovingUnitMutualYieldLikeOriginal = true;
        public float OriginalMovingUnitMutualYieldStepRealLikeOriginal = 48.0f;
        public float OriginalMovingUnitMutualYieldCooldownSecondsLikeOriginal = 0.08f;
        public int MaxOriginalIdleYieldAsideLogsLikeOriginal = 48;
        public bool UseOriginalGotoFinePositionAfterProductionLikeOriginal = true;
        public int OriginalGotoFinePositionMaxAttemptsLikeOriginal = 3;
        public float OriginalGotoFinePositionMinMoveRealLikeOriginal = 64.0f;
        public bool LogOriginalGotoFinePositionLikeOriginal = true;
        public int MaxOriginalGotoFinePositionLogsLikeOriginal = 32;
        public bool LogOriginalMotionOnce = false; // V46_LOG_CLEAN: no motion path spam by default
        public bool EnableOriginalRestRandomLikeOriginal = true;
        public float RestMinDelaySeconds = 2.5f;
        public float RestMaxDelaySeconds = 7.5f;
        public float RestChancePerCheck = 0.35f;
        public bool LoopCurrentAnimationLikeOriginal = true;

        [Header("Cossacks II SINGLESTEP / Boids")]
        public bool UseMdSingleStepPassThroughLikeOriginal = true;
        public bool UseMdBoidsSteeringLikeOriginal = true;
        public float OriginalBoidsRadiusOriginalPixelsLikeOriginal = 60.0f;
        public float OriginalBoidsMinDistanceOriginalPixelsLikeOriginal = 14.0f;
        public int OriginalBoidsNeighborRefreshTicksLikeOriginal = 17;
        // COSSACKS2/EngineSettings.h sets BoidsOffLimit=5000. Both
        // BoidsExtension and MotionHandlerForSingleStepObjects use that limit.
        public int OriginalBoidsOffLimitLikeOriginal = 5000;
        public float OriginalBoidsMainDirectionNormLikeOriginal = 1000.0f;
        public float OriginalBoidsDensityNormLikeOriginal = 350.0f;
        public float OriginalBoidsChangeSpeedWeightLikeOriginal = 8.0f;

        [Header("Render")]
        public bool BillboardToBattleCamera = false;
        public bool ViewerLikePixelPerfectCameraPlane = true;
        public bool ViewerLikeLockPixelScaleAtFirstFrame = true;
        public bool ViewerLikeTextureSrgb = true;
        [Range(1.0f, 2.0f)] public float ViewerLikeUnitAlphaBoost = 1.35f;
        public bool LogViewerLikePixelFixOnce = false; // V260C_LOG_CLEAN
        public bool DrawOriginalPivotPlacement = true;
        public bool UseOriginalDrawSpriteUnitPivotLikeOriginal = true;
        public bool UseViewerGpCacheLikeOriginal = true;
        public bool DecodeOnlyVisibleUnitFramesLikeOriginal = true;
        public float ViewerFrameVisibilityMarginPixelsLikeOriginal = 192.0f;
        [HideInInspector] public int MaxViewerTextureUploadsPerFrameLikeOriginal = 2; // Legacy serialized field; current animation frames are never deferred.
        [Header("Cossacks II DrawUnits / Fog Of War")]
        public bool UseOriginalDrawUnitsCellVisibilityLikeOriginal = true;
        public bool UseOriginalFogOfWarLikeOriginal = true;
        public int OriginalDrawUnitsSphereRadiusPixelsLikeOriginal = 300;
        public bool LogOriginalDrawUnitsAuditOnceLikeOriginal = true;
        public float VisualScale = 1.0f;
        public float DebugYOffset = 0.35f;
        public int SortingOrderBase = 6000;
        public bool SortUnitsByScreenYLikeOriginal = true;
        public bool SortUnitsAgainstBuildingsByOriginalRealYLikeOriginal = true;
        public float UnitScreenYSortScaleLikeOriginal = 4.0f;
        public int UnitScreenYSortClampLikeOriginal = 9000;
        public float UnitRealYSortScaleFallbackLikeOriginal = 0.25f;
        public int UnitBodyRenderQueueLikeOriginal = 3670;
        public bool UseScreenSpriteOverlayDepth = false;
        public bool UnitBodyThroughTerrainLikeOriginal = true;
        public bool UseUnitBuildingDepthPrepassLikeOriginal = true;
        [Tooltip("MiniMap4X.cpp::DrawUnits -> GPS sprite submission: collect visible unit quads by the shared 256x256 sprite surface instead of keeping two active Unity renderers per unit.")]
        public bool UseOriginalGpsUnitBatchRendererLikeOriginal = true;
        public bool UseOriginalMotionFieldPerStepBlockLikeOriginal = true;
        public int UnitMotionBlockMaxSlideTriesLikeOriginal = 10;
        public float UnitMotionBlockMinSlideStepRealLikeOriginal = 64.0f;
        public int PlayerColorArgb = unchecked((int)0xFFFF2020);

        private const int UnitOriginalZBufferSameLineTieMaxLikeOriginal = 15;
        private const float UnitOriginalZBufferDepthBiasAmplitudePixelsLikeOriginal = 4.0f;
        private const float UnitDepthAlphaCutoffLikeOriginal = 96.0f / 255.0f;
        private const int UnitViewerTextureCacheSoftLimitLikeOriginal = 8192;
        private const int UnitViewerTextureCacheHardLimitLikeOriginal = 12288;

        // V277: unit size is native frame/slide visual scale, not map position scale.
        // RealX/RealY/path/LINESORT stay in map pipeline coordinates; camera is observer/projection only.
        private float _unitNativeVisualPixelToWorldScaleV277LikeOriginal = -1.0f;
        private string _unitNativeVisualPixelToWorldScaleSourceV277LikeOriginal = string.Empty;

        [Header("Selection")]
        public bool EnableSelectionPicker = false;
        public float PickPaddingPixels = 10.0f;
        public float SelectionRoundScale = 1.0f;
        public bool LogSelection = true;

        [Header("Building Rally / Exit Point Like Original")]
        public bool EnableBuildingRallyPointV155LikeOriginal = true;
        public bool LogBuildingRallyPointV155LikeOriginal = true;

        [Header("Selection Brightness Like Original")]
        public bool SelectionBrightnessPulseLikeOriginal = true;
        public bool SelectionBrightnessUseRawOriginalDiffuse = false;
        [Range(1.0f, 2.0f)] public float SelectionBrightnessMaxMultiplier = 1.35f;
        public bool LogSelectionBrightnessOnce = true;

        [Header("Death Like Original")]
        public bool EnableDeleteSelectedDeathLikeOriginal = true;
        public bool DeathStopsMovementLikeOriginal = true;
        public bool DeathClearsSelectionLikeOriginal = true;
        public bool DeathHideSelectionRingLikeOriginal = true;
        public bool DeathRandomRotateLikeOriginal = true;
        public int DeathRandomDirSpreadLikeOriginal = 64;
        public bool DeathFallbackToDeathLie1LikeOriginal = true;
        public bool LogDeathLikeOriginal = true;

        [Header("Work / Build Animation Like Original")]
        public bool EnableWorkAnimationLikeOriginal = true;
        public bool WorkAnimationExternalPhaseLikeOriginal = true;
        // Integration mirrors of GSets.CGame.ArcadeMode/NPlayers/MyNation used only
        // by NewMon.cpp::ApplyTiring. Retail/default behavior keeps ArcadeMode off.
        public bool OriginalArcadeModeV431LikeOriginal = false;
        public int OriginalPlayerCountV431LikeOriginal = 1;
        public int OriginalMyNationV431LikeOriginal = 0;
        public bool WorkStopsMoveLikeOriginal = true;
        public bool WorkFallbackToStandIfMissingLikeOriginal = true;
        public bool LogWorkAnimationLikeOriginal = false; // V46_LOG_CLEAN

        [Header("Building Passability / LINESORT Like Original")]
        public bool RouteUnitMoveThroughBuildingLockPointsLikeOriginal = true;
        public int BuildingPathMaxSearchCellsLikeOriginal = 12000;
        public bool MarkPreciseBornExitPathForLineSortLikeOriginal = true;
        public bool LogBuildingPathOnceLikeOriginal = false; // V46_LOG_CLEAN
        public bool LogBornExitAuditLikeOriginal = true;
        public int MaxBornExitAuditLogsLikeOriginal = 64; // keep the next spawn/rally log useful, cap hard

        [Header("Selection Ring")]
        public string SelectionRingResourcePath = "textures/selection/round3";
        public bool SelectionRingThroughTerrain = true;
        public bool SelectionRingBehindUnit = true;
        public float SelectionRingWidth = 32f;
        public float SelectionRingHeight = 32f;
        public float SelectionRingOffsetX = 0f;
        public float SelectionRingOffsetY = -0.35f;
        public float SelectionRingOffsetZ = 0f;
        public bool SelectionRingUseGroundPlane = true;

        [Header("Logging")]
        public bool LogCreatedUnits = false; // V46_LOG_CLEAN
        public bool LogFirstFramePerUnit = false; // V46_LOG_CLEAN
        public int MaxUnitCreateLogs = 80;
        public int MaxFirstFrameLogs = 80;

        private bool _busy;
        private bool _started;
        private bool _initialized;
        private C2BattleTerrainMode _battle;
        private string _dataRoot;
        private string _mapRelativePath;
        private string _mapAbsPath;

        private readonly List<C2UnitOriginalRuntime> _units = new List<C2UnitOriginalRuntime>();
        private readonly Dictionary<long, List<C2UnitOriginalRuntime>> _unitCollisionBucketsLikeOriginal =
            new Dictionary<long, List<C2UnitOriginalRuntime>>(2048);
        private readonly Stack<List<C2UnitOriginalRuntime>> _unitCollisionBucketPoolLikeOriginal =
            new Stack<List<C2UnitOriginalRuntime>>(2048);
        private struct OriginalBoidsNeighborPairLikeOriginal
        {
            public int A;
            public int B;
        }
        private readonly List<OriginalBoidsNeighborPairLikeOriginal> _originalBoidsNeighborPairsLikeOriginal =
            new List<OriginalBoidsNeighborPairLikeOriginal>(16384);
        // UnitAbility.cpp uses one contiguous int[4*MAXOBJECT] block for
        // RealX, RealY, ForceX, ForceY.  Keeping the same dense layout avoids
        // millions of managed object dereferences in a crowded brigade while
        // preserving CalculatePushForce/BoidsSingleStep2 arithmetic.
        private float[] _originalBoidsCoordAndForceLikeOriginal = Array.Empty<float>();
        private int _originalBoidsSimulationTickLikeOriginal;
        private int _originalBoidsNeighborRefreshesLikeOriginal;
        private readonly Dictionary<long, List<C2UnitOriginalRuntime>> _unitDrawCellsLikeOriginal =
            new Dictionary<long, List<C2UnitOriginalRuntime>>(2048);
        private readonly Stack<List<C2UnitOriginalRuntime>> _unitDrawCellPoolLikeOriginal =
            new Stack<List<C2UnitOriginalRuntime>>(2048);
        // NewMon.cpp::ProcessNewMonsters keeps SetInCellTime in 8.8 game-time
        // units: SetMonstersInCells() resets it to 256*10 and each normal
        // simulation tick subtracts GameSpeed=256.  Consequently MCount/NMSL,
        // which MiniMap4X.cpp::DrawUnits consumes, is rebuilt once per ten
        // original 40 ms ticks rather than once per tick.
        private const int OriginalGameSpeed256LikeOriginal = 256;
        private const int OriginalSetInCellInterval256LikeOriginal = OriginalGameSpeed256LikeOriginal * 10;
        private int _originalSetInCellTime256LikeOriginal;
        private readonly List<C2UnitOriginalRuntime> _drawUnitsCurrentLikeOriginal =
            new List<C2UnitOriginalRuntime>(1024);
        private readonly List<C2UnitOriginalRuntime> _drawUnitsPreviousLikeOriginal =
            new List<C2UnitOriginalRuntime>(1024);
        private int _drawUnitsEpochV379LikeOriginal;
        private int _drawUnitsCameraCellCandidatesV372LikeOriginal;
        private bool _drawUnitsFellBackToAllUnitsV372LikeOriginal;
        private readonly Plane[] _drawUnitFrustumPlanesLikeOriginal = new Plane[6];
        private int _drawOriginalBoundsFrameLikeOriginal = -1;
        private bool _drawOriginalBoundsValidLikeOriginal;
        private float _drawOriginalBoundsMinXLikeOriginal;
        private float _drawOriginalBoundsMinYLikeOriginal;
        private float _drawOriginalBoundsMaxXLikeOriginal;
        private float _drawOriginalBoundsMaxYLikeOriginal;
        private sealed class OriginalDrawBuildingLikeOriginal
        {
            public C2BuildingRuntimeInfoV247LikeOriginal Info;
            public Renderer[] Renderers = Array.Empty<Renderer>();
            public bool DynamicRenderers;
            public int VisionType;
            public int VisibilityRadiusOriginalPixels = 300;
            public bool DontAffectFogOfWar;
            public bool EverSeenInFog;
        }
        private readonly List<OriginalDrawBuildingLikeOriginal> _drawBuildingsLikeOriginal =
            new List<OriginalDrawBuildingLikeOriginal>(512);
        private readonly Dictionary<long, List<OriginalDrawBuildingLikeOriginal>> _buildingDrawCellsLikeOriginal =
            new Dictionary<long, List<OriginalDrawBuildingLikeOriginal>>(512);
        private readonly Stack<List<OriginalDrawBuildingLikeOriginal>> _buildingDrawCellPoolLikeOriginal =
            new Stack<List<OriginalDrawBuildingLikeOriginal>>(512);
        private readonly HashSet<OriginalDrawBuildingLikeOriginal> _drawBuildingsCurrentSetLikeOriginal =
            new HashSet<OriginalDrawBuildingLikeOriginal>();
        private readonly HashSet<OriginalDrawBuildingLikeOriginal> _drawBuildingsPreviousSetLikeOriginal =
            new HashSet<OriginalDrawBuildingLikeOriginal>();
        private readonly HashSet<EntityId> _buildingsEverSeenInFogLikeOriginal = new HashSet<EntityId>();
        private struct FogMdInfoLikeOriginal
        {
            public int VisionType;
            public int VisibilityRadiusOriginalPixels;
            public bool DontAffectFogOfWar;
        }
        private readonly Dictionary<string, FogMdInfoLikeOriginal> _fogMdInfoByPathLikeOriginal =
            new Dictionary<string, FogMdInfoLikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private float _nextBuildingDrawRefreshAtLikeOriginal;
        private bool _originalDrawUnitsAuditLoggedLikeOriginal;
        private ushort[] _fogMapLikeOriginal;
        private ushort[] _fogMapWorkLikeOriginal;
        private Texture2D _fogTextureLikeOriginal;
        private GameObject _fogOverlayRootLikeOriginal;
        private Camera _fogOverlayCameraLikeOriginal;
        private Mesh _fogOverlayMeshLikeOriginal;
        private MeshRenderer _fogOverlayRendererLikeOriginal;
        private Material _fogOverlayMaterialLikeOriginal;
        private Vector3[] _fogOverlayVerticesLikeOriginal;
        private Vector2[] _fogOverlayUvLikeOriginal;
        private Color32[] _fogOverlayColorsLikeOriginal;
        private readonly Vector2[] _fogOverlayCornersLikeOriginal = new Vector2[4];
        private short[] _fogFractalMapLikeOriginal;
        private bool _fogOverlayLoadFailureLoggedLikeOriginal;
        private int _fogMapSideLikeOriginal;
        private int _fogAddshLikeOriginal = 1;
        private bool _fogInitializedLikeOriginal;
        private readonly Dictionary<string, MdModel> _mdCache = new Dictionary<string, MdModel>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _mdAuditCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _viewerGpIdByPackage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _viewerPackageKeyByPackage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private struct ViewerTextureKeyLookupLikeOriginal : IEquatable<ViewerTextureKeyLookupLikeOriginal>
        {
            public int GpId;
            public int DrawSprite;
            public int ColorId;
            public int Alpha1000;

            public bool Equals(ViewerTextureKeyLookupLikeOriginal other)
            {
                return GpId == other.GpId && DrawSprite == other.DrawSprite &&
                       ColorId == other.ColorId && Alpha1000 == other.Alpha1000;
            }

            public override bool Equals(object obj)
            {
                return obj is ViewerTextureKeyLookupLikeOriginal && Equals((ViewerTextureKeyLookupLikeOriginal)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = GpId;
                    hash = (hash * 397) ^ DrawSprite;
                    hash = (hash * 397) ^ ColorId;
                    return (hash * 397) ^ Alpha1000;
                }
            }
        }
        private readonly Dictionary<ViewerTextureKeyLookupLikeOriginal, string> _viewerTextureStringKeyCacheLikeOriginal =
            new Dictionary<ViewerTextureKeyLookupLikeOriginal, string>(4096);
        private readonly Dictionary<ViewerTextureKeyLookupLikeOriginal, global::C2UnitFrameOriginal> _viewerReadyFrameByLookupLikeOriginal =
            new Dictionary<ViewerTextureKeyLookupLikeOriginal, global::C2UnitFrameOriginal>(4096);
        // A bounded front cache for repeated brigade frames. It stores the exact
        // resolved GPS frame, including nation colour and atlas coordinates.
        // No animation phase, direction, or off-screen simulation is changed.
        private struct ViewerHotFrameV374LikeOriginal
        {
            public string Package;
            public int DisplaySprite;
            public int FallbackSprite;
            public int ColorId;
            public int Alpha1000;
            public bool Mirror;
            public global::C2UnitFrameOriginal Frame;
        }
        private readonly ViewerHotFrameV374LikeOriginal[] _viewerHotFramesV374LikeOriginal =
            new ViewerHotFrameV374LikeOriginal[512];
        public int VisualHotFrameHitsV374LikeOriginal { get; private set; }
        private readonly Dictionary<EntityId, Material> _unitBodyMaterialByTextureLikeOriginal = new Dictionary<EntityId, Material>(256);
        private readonly Dictionary<EntityId, Material> _unitDepthMaterialByTextureLikeOriginal = new Dictionary<EntityId, Material>(256);
        private sealed class OriginalGpsUnitBatchLikeOriginal
        {
            public EntityId TextureId;
            public Texture2D Texture;
            public GameObject Root;
            public Mesh Mesh;
            public MeshRenderer BodyRenderer;
            public MeshRenderer DepthRenderer;
            public bool Touched;
            public Matrix4x4 WorldToLocalThisFrameV378LikeOriginal;
            public readonly List<Vector3> Vertices = new List<Vector3>(4096);
            public readonly List<Vector2> Uv = new List<Vector2>(4096);
            public readonly List<Color> Colors = new List<Color>(4096);
            public readonly List<int> Triangles = new List<int>(6144);

            public void BeginFrameLikeOriginal()
            {
                Touched = false;
                Vertices.Clear();
                Uv.Clear();
                Colors.Clear();
                Triangles.Clear();
            }
        }
        private readonly Dictionary<EntityId, OriginalGpsUnitBatchLikeOriginal> _originalGpsUnitBatchByTextureLikeOriginal =
            new Dictionary<EntityId, OriginalGpsUnitBatchLikeOriginal>(32);
        private bool _originalGpsUnitBatchAuditLoggedLikeOriginal;
        private readonly Dictionary<string, Texture2D> _viewerTextureByFrame = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Rect> _viewerTextureUvByFrameLikeOriginal = new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase);
        private struct ViewerFrameMetaLikeOriginal
        {
            public int OriginX;
            public int OriginY;
            public int Width;
            public int Height;
        }
        private readonly Dictionary<string, ViewerFrameMetaLikeOriginal> _viewerFrameMetaByKeyLikeOriginal =
            new Dictionary<string, ViewerFrameMetaLikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _viewerTextureNationByFrameLikeOriginal = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _viewerTextureBytesByFrameLikeOriginal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<EntityId> _viewerTextureMemoryIdsLikeOriginal = new HashSet<EntityId>();
        private readonly Dictionary<string, string> _unitVisualAbsPathCacheLikeOriginal = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private long _viewerTextureApproxBytesLikeOriginal;
        private int _viewerTextureNationFramesLikeOriginal;
        private int _viewerTexturePlainFramesLikeOriginal;
        private int _viewerTextureMemoryLogsLikeOriginal;
        private int _viewerTextureCreateEventsLikeOriginal;
        private float _nextViewerTexturePerfEventAtLikeOriginal;
        private int _viewerTextureUploadFrameLikeOriginal = -1;
        private int _viewerTextureUploadsThisFrameLikeOriginal;
        private float _nextViewerTextureDeferredPerfAtLikeOriginal;
        private int _gotoFinePositionLogsLikeOriginal;
        private Camera _cachedBattleCameraLikeOriginal;
        private double _simulationAccumulatorLikeOriginal;
        private const double OriginalSimulationQuantumSecondsLikeOriginal = 0.040;
        private const int MaxSimulationCatchUpStepsLikeOriginal = 8;
        private static readonly ProfilerMarker SimulationMarkerV370 = new ProfilerMarker("C2.Unit.SimulationV370");
        private static readonly ProfilerMarker FormationKeepMarkerV370 = new ProfilerMarker("C2.Unit.FormationKeepV370");
        private static readonly ProfilerMarker OrdersMarkerV370 = new ProfilerMarker("C2.Unit.OrdersV370");
        private static readonly ProfilerMarker StepAllMarkerV370 = new ProfilerMarker("C2.Unit.StepAllV370");
        private static readonly ProfilerMarker DrawListMarkerV370 = new ProfilerMarker("C2.Unit.DrawListV370");
        private static readonly ProfilerMarker RenderVisibleMarkerV370 = new ProfilerMarker("C2.Unit.RenderVisibleV370");
        private static readonly ProfilerMarker BatchBuildMarkerV370 = new ProfilerMarker("C2.Unit.BatchBuildV370");
        private static readonly ProfilerMarker WholeUpdateMarkerV371 = new ProfilerMarker("C2.Unit.WholeUpdateV371");
        private static readonly ProfilerMarker SpawnMarkerLikeOriginal = new ProfilerMarker("C2.Unit.Spawn");
        private long _profileSpawnTicksLikeOriginal;
        private long _profileSpawnMaxTicksLikeOriginal;
        private int _profileSpawnCallsLikeOriginal;
        private Camera _visibleUnitPickCameraV375LikeOriginal;
        private static readonly int[] MotionFieldSlideAngleStepsLikeOriginal = { 1, -1, 2, -2, 3, -3, 4, -4, 6, -6 };
        private static readonly float[] MotionFieldSlideStepScalesLikeOriginal = { 1.0f, 0.5f, 0.25f };
        private static readonly float[] IdleYieldStepScalesLikeOriginal = { 1.0f, 1.5f, 2.0f, 0.5f };
        // Diagnostics only.  Leaving this enabled in normal play sampled the
        // high-resolution clock six or more times for every live unit on every
        // 40 ms simulation quantum (90k+ timer calls at 15k units).  Cossacks II
        // does not run that instrumentation in its release loop; opt in from the
        // inspector only while taking a phase trace.
        public bool ProfileUnitRuntimePhasesLikeOriginal = false;
        [NonSerialized]
        public bool SkipUnitVisualUpdateForPerformanceDiagnosisV371;
        [NonSerialized]
        public bool SkipGpsBatchMeshCommitForPerformanceDiagnosisV372;
        [NonSerialized]
        public bool SkipUnitFrameMaterializationForPerformanceDiagnosisV373;
        [NonSerialized]
        public bool SkipGpsBatchBuildForPerformanceDiagnosisV373;
        [NonSerialized]
        public bool SkipUnitFrameApplyForPerformanceDiagnosisV373;
        [NonSerialized]
        public bool SkipUnitRenderTransformForPerformanceDiagnosisV373;
        private int _visualRenderRelevantV373LikeOriginal;
        private int _visualFrameApplyCallsV373LikeOriginal;
        private int _visualFrameScalarHitsV373LikeOriginal;
        private int _visualReadyFrameCacheHitsV373LikeOriginal;
        private int _visualTextureCacheHitsV373LikeOriginal;
        private int _visualTextureUploadsV373LikeOriginal;
        private int _visualBatchSubmittedV373LikeOriginal;
        private int _visualActiveBatchesV373LikeOriginal;
        private int _visualTotalBatchesV373LikeOriginal;
        private long _profileBucketTicksLikeOriginal;
        private long _profileStepTicksLikeOriginal;
        private long _profileSeparationTicksLikeOriginal;
        private long _profileRenderTicksLikeOriginal;
        private long _profileStateTicksLikeOriginal;
        private long _profileTiringTicksLikeOriginal;
        private long _profileAnimationTicksLikeOriginal;
        private long _profileFrameApplyTicksLikeOriginal;
        private long _profileBoidsTicksLikeOriginal;
        private long _profileMotionFieldTicksLikeOriginal;
        private long _profileWorldSyncTicksLikeOriginal;
        private long _profileBlockedMoveTicksLikeOriginal;
        private long _profileDrawCellRebuildTicksLikeOriginal;
        private long _profileWholeUpdateTicksLikeOriginal;
        private long _profileInputTicksLikeOriginal;
        private long _profileFormationKeepTicksLikeOriginal;
        private long _profileOrderTicksLikeOriginal;
        private long _profileFogAndTurnsTicksLikeOriginal;
        private long _profileDrawListAndFogOverlayTicksLikeOriginal;
        private int _profileDrawCellRebuildsLikeOriginal;
        private int _profileMovingCallsLikeOriginal;
        private int _profileBlockedMoveCallsLikeOriginal;
        private int _profileSimulationStepsLikeOriginal;
        private int _profileRenderFramesLikeOriginal;
        private int _profileWholeUpdateFramesLikeOriginal;
        private float _nextProfileReportAtLikeOriginal;
        private readonly global::TemnyLessViewer.C2GpSystem _viewerGps = new global::TemnyLessViewer.C2GpSystem();
        private const int ViewerSpriteSurfaceSideLikeOriginal = 256; // sgSpriteManager.h::c_GPTexSide
        private sealed class ViewerSpriteSurfaceLikeOriginal
        {
            public Texture2D Texture;
            public bool Dirty;
            public int CursorX;
            public int CursorY;
            public int RowHeight;

            public bool TryAllocate(int width, int height, out RectInt pixelRect)
            {
                pixelRect = default(RectInt);
                if (width <= 0 || height <= 0 || width > ViewerSpriteSurfaceSideLikeOriginal || height > ViewerSpriteSurfaceSideLikeOriginal)
                    return false;
                if (CursorX + width > ViewerSpriteSurfaceSideLikeOriginal)
                {
                    CursorX = 0;
                    CursorY += RowHeight;
                    RowHeight = 0;
                }
                if (CursorY + height > ViewerSpriteSurfaceSideLikeOriginal)
                    return false;
                pixelRect = new RectInt(CursorX, CursorY, width, height);
                CursorX += width;
                RowHeight = Mathf.Max(RowHeight, height);
                return true;
            }
        }
        private readonly List<ViewerSpriteSurfaceLikeOriginal> _viewerSpriteSurfacesLikeOriginal =
            new List<ViewerSpriteSurfaceLikeOriginal>(64);
        private C2UnitOriginalRuntime _selected;
        private GameObject _runtimeRoot;
        private Texture2D _selectionRoundTexture;
        private float _lastDeleteAt;
        private int _createdLogs;
        private int _firstFrameLogs;
        private int _pixelFixLogs;
        private int _originalMotionLogs;
        private int _deathLogs;
        private int _workLogs;
        private int _buildingPathLogs;
        private int _bornExitAuditLogs;
        private int _idleYieldAsideLogsLikeOriginal;
        private bool _unitCollisionBucketsValidLikeOriginal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLikeOriginal()
        {
            if (GameObject.Find(RootName) != null) return;
            GameObject go = new GameObject(RootName);
            DontDestroyOnLoad(go);
            go.AddComponent<C2UnitOriginalRuntimeAndRendererV1>();
            Debug.Log(LogPrefix + " installed auto=1 mode=TEMNYLESS_VIEWER_GP_CACHE_V5 layers=ObjectRuntime/AnimationState/ViewerDrawSpritePath/SelectionPicker state=StandRestMotionWorkDeath+ORIGINAL_LineInfo_BornAlias_CheckBarRadius_BornExitAudit_RawBornPath_PathPreserve+BuildingRallyDstXY currentFrameLong=1 realDir=1 viewerGpCache=1 oldFrameProvider=0 billboard=0 selection=1 hotkeys=0 no_buildings=1 no_hud=1");
        }

        private void Awake()
        {
            ConfigureViewerGpManagedCacheLikeOriginal();
        }

        private void ConfigureViewerGpManagedCacheLikeOriginal()
        {
            // Original ISM retains compressed packages + atlas surfaces. It does
            // not retain an RGBA bitmap for every uploaded frame.
            _viewerGps.MaxCachedFrames = 192;
            _viewerGps.MaxCachedBytes = 64L * 1024L * 1024L;
        }

        private int AccumulateSimulationStepsLikeOriginal(double renderDt)
        {
            // Ddex1.cpp::PreDrawGameProcess scales GameSpeed with elapsed time.
            // This port uses fixed GameSpeed=256 quanta instead, so it must run
            // every elapsed quantum. One step per render slowed the entire game
            // below 25 FPS. Bound long editor stalls to the existing 250 ms cap.
            _simulationAccumulatorLikeOriginal += Math.Max(0.0, Math.Min(0.25, renderDt));
            int steps = Math.Min(MaxSimulationCatchUpStepsLikeOriginal,
                (int)Math.Floor((_simulationAccumulatorLikeOriginal + 0.0000001) / OriginalSimulationQuantumSecondsLikeOriginal));
            _simulationAccumulatorLikeOriginal -= steps * OriginalSimulationQuantumSecondsLikeOriginal;
            if (_simulationAccumulatorLikeOriginal < 0.0) _simulationAccumulatorLikeOriginal = 0.0;
            if (steps >= MaxSimulationCatchUpStepsLikeOriginal &&
                _simulationAccumulatorLikeOriginal > OriginalSimulationQuantumSecondsLikeOriginal)
                _simulationAccumulatorLikeOriginal = OriginalSimulationQuantumSecondsLikeOriginal;
            return steps;
        }

        private void Update()
        {
            if (C2BattleTerrainMode.EditorTestModeLikeOriginal && UseOriginalFogOfWarLikeOriginal)
            {
                UseOriginalFogOfWarLikeOriginal = false;
                Shader.SetGlobalFloat("_C2FogEnabledLikeOriginal", 0.0f);
                if (_fogOverlayRendererLikeOriginal != null)
                    _fogOverlayRendererLikeOriginal.forceRenderingOff = true;
                if (_fogOverlayCameraLikeOriginal != null)
                    _fogOverlayCameraLikeOriginal.enabled = false;
                Debug.Log(LogPrefix + " FOW_DISABLED reason=editor_test_mode_runtime");
            }

            if (!_started && AutoRunOnceAfterMapLoad && !_busy)
                StartCoroutine(RunWhenBattleReadyLikeOriginal("auto"));

            if (!_initialized) return;

            _visualRenderRelevantV373LikeOriginal = 0;
            _visualFrameApplyCallsV373LikeOriginal = 0;
            _visualFrameScalarHitsV373LikeOriginal = 0;
            VisualHotFrameHitsV374LikeOriginal = 0;
            _visualReadyFrameCacheHitsV373LikeOriginal = 0;
            _visualTextureCacheHitsV373LikeOriginal = 0;
            _visualTextureUploadsV373LikeOriginal = 0;
            _visualBatchSubmittedV373LikeOriginal = 0;
            _visualActiveBatchesV373LikeOriginal = 0;
            _visualTotalBatchesV373LikeOriginal = _originalGpsUnitBatchByTextureLikeOriginal.Count;

            using var wholeUpdateScopeV371 = WholeUpdateMarkerV371.Auto();

            using var wholeUpdateScopeV371Cost = C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Units);

            long wholeUpdateStartedLikeOriginal = ProfileUnitRuntimePhasesLikeOriginal
                ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                : 0L;
            long inputStartedLikeOriginal = wholeUpdateStartedLikeOriginal;

            if (EnableSelectionPicker)
                HandleSelectionInputLikeOriginal();
            if (EnableBuildingRallyPointV155LikeOriginal)
                HandleBuildingRallyPointInputV155LikeOriginal();
            HandleRuntimeHotkeysLikeOriginal();
            HandleDeleteSelectedDeathHotkeyLikeOriginal();
            if (ProfileUnitRuntimePhasesLikeOriginal)
                _profileInputTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - inputStartedLikeOriginal;

            float renderDt = Time.unscaledDeltaTime;
            Camera unitSortCamera = SortUnitsByScreenYLikeOriginal && !SortUnitsAgainstBuildingsByOriginalRealYLikeOriginal
                ? FindBattleCameraLikeOriginal()
                : null;

            // Use the 40 ms Skirmish cadence from the game's EngineSettings.xml.
            // CII varies GameSpeed; this adapter uses equivalent fixed quanta. Running
            // pathfinding, collision buckets and frame counters on every Unity
            // render frame made the port 2.4x heavier at 60 FPS and changed MD
            // animation timing.  Keep rendering independent, but tick the original
            // simulation at its exact 25 Hz cadence.
            int simulationSteps = AccumulateSimulationStepsLikeOriginal(renderDt);
            for (int simulationStep = 0; simulationStep < simulationSteps; simulationStep++)
            {
                using var simulationScopeV370 = SimulationMarkerV370.Auto();
                using var movementFieldsV433 = C2OriginalMovementSystemV425LikeOriginal.PrepareFieldBatchV433();

                long phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                // NewMon.cpp::ProcessNewMonsters refreshes MCount/NMsList through
                // SetMonstersInCells once per 256*10 GameSpeed units, not on every
                // simulation tick.  Collision queries and DrawUnits consume that
                // same periodically rebuilt spatial registration.
                bool rebuildSetInCellsLikeOriginal =
                    _originalSetInCellTime256LikeOriginal <= 0 ||
                    !_unitCollisionBucketsValidLikeOriginal ||
                    (UseOriginalDrawUnitsCellVisibilityLikeOriginal && _unitDrawCellsLikeOriginal.Count == 0);
                if (rebuildSetInCellsLikeOriginal)
                {
                    RebuildRuntimeUnitCollisionBucketsLikeOriginal();
                    if (ProfileUnitRuntimePhasesLikeOriginal)
                        _profileBucketTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;

                    {
                        phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                        RebuildOriginalDrawUnitCellsLikeOriginal();
                        if (ProfileUnitRuntimePhasesLikeOriginal)
                        {
                            _profileDrawCellRebuildTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;
                            _profileDrawCellRebuildsLikeOriginal++;
                        }
                    }
                    _originalSetInCellTime256LikeOriginal = OriginalSetInCellInterval256LikeOriginal;
                }
                _originalSetInCellTime256LikeOriginal -= OriginalGameSpeed256LikeOriginal;

                phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                UpdateOriginalBoidsPairForcesLikeOriginal();
                if (ProfileUnitRuntimePhasesLikeOriginal)
                    _profileBoidsTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;

                // Motion.cpp/path.cpp keep MFIELDS GLock and UnitsField alive across
                // simulation quanta. Rebuild the managed mirrors from current unit state
                // before orders/pathfinding consume them.
                C2OriginalMovementSystemV425LikeOriginal.RebuildDynamicUnitFieldsV425LikeOriginal();

                phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                using (FormationKeepMarkerV370.Auto())
                using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Formation))
                    C2FormationRuntimeV167LikeOriginal.TickFormationKeepPositionsSpeedV358LikeOriginal();
                if (ProfileUnitRuntimePhasesLikeOriginal)
                    _profileFormationKeepTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;

                phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                using (StepAllMarkerV370.Auto())
                using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Movement))
                {
                    // LongProcesses: nation 0..7, then each object's LocalOrder
                    // immediately followed by its MotionStyle. Keeping all orders
                    // in a backwards pass used stale occupancy for every soldier.
                    for (int nationV433 = 0; nationV433 < 8; nationV433++)
                    for (int i = 0; i < _units.Count; i++)
                    {
                        C2UnitOriginalRuntime u = _units[i];
                        if (u == null || !u.ActiveLikeOriginal || u.Md == null || (u.Info != null ? u.Info.Nation : 0) != nationV433) continue;
                        long orderStartV433 = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                        C2OriginalOrderChainV352.TickUnitOrderV433LikeOriginal(u.Info);
                        if (ProfileUnitRuntimePhasesLikeOriginal)
                            _profileOrderTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - orderStartV433;
                        StepUnitRuntimeLikeOriginal(u, (float)OriginalSimulationQuantumSecondsLikeOriginal);
                    }
                }
                if (ProfileUnitRuntimePhasesLikeOriginal)
                    _profileStepTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;

                RemoveExpiredDeathsV435LikeOriginal();

                // NewMon.cpp::LongProcesses calls PerformPathFiding AFTER all
                // object motion. A queued CreatePath prepares next tick's DestX.
                using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Paths))
                    C2OriginalMovementSystemV425LikeOriginal.PerformPathFindingV425LikeOriginal();

                // COSSACKS2/BoidsExtension.cpp::ProcessingGame stops the
                // neighbor-force path completely at MAXOBJECT >= BoidsOffLimit.
                // The compatibility separation below is not part of the active
                // high-count C2 chain, so it must not perform another full bucket
                // rebuild after that exact cutoff either.
                if (UseOriginalUnitSeparationForcesLikeOriginal &&
                    _units.Count < Mathf.Max(1, OriginalBoidsOffLimitLikeOriginal))
                {
                    phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                    RebuildRuntimeUnitCollisionBucketsLikeOriginal();
                    ApplyRuntimeUnitSeparationForcesLikeOriginal((float)OriginalSimulationQuantumSecondsLikeOriginal);
                    if (ProfileUnitRuntimePhasesLikeOriginal)
                        _profileSeparationTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;
                }

                phaseStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                StepOriginalFogOfWarLikeOriginal();
                C2FormationRuntimeV167LikeOriginal.TickFormationTurnsV334LikeOriginal();
                if (ProfileUnitRuntimePhasesLikeOriginal)
                    _profileFogAndTurnsTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - phaseStarted;
                if (ProfileUnitRuntimePhasesLikeOriginal) _profileSimulationStepsLikeOriginal++;
            }
            // Benchmark-only isolation switch. It is never serialized and stays
            // false in normal play. The scale harness uses it to split simulation
            // cost from Unity's dynamic-mesh and render submission cost.
            if (SkipUnitVisualUpdateForPerformanceDiagnosisV371)
                return;

            long drawListStartedLikeOriginal = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
            Camera drawCameraLikeOriginal;
            using (DrawListMarkerV370.Auto())
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.DrawList))
            {
                drawCameraLikeOriginal = FindBattleCameraLikeOriginal();
                if (UseOriginalDrawUnitsCellVisibilityLikeOriginal)
                {
                    if (_unitDrawCellsLikeOriginal.Count == 0 && _units.Count > 0)
                        RebuildOriginalDrawUnitCellsLikeOriginal();
                    BuildOriginalDrawUnitsListLikeOriginal(drawCameraLikeOriginal);
                    _visibleUnitPickCameraV375LikeOriginal = drawCameraLikeOriginal;
                }
                UpdateOriginalFogOverlayLikeOriginal(drawCameraLikeOriginal);
            }
            if (ProfileUnitRuntimePhasesLikeOriginal)
                _profileDrawListAndFogOverlayTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - drawListStartedLikeOriginal;

            // Diagnostic-only split: retain the original cell/frustum/fog list and
            // simulation, but stop before any frame resolution, transform or batch
            // work. This isolates the MiniMap4X.cpp::DrawUnits boundary before
            // AddAnimation/GPS without changing gameplay coordinates.
            if (SkipUnitFrameMaterializationForPerformanceDiagnosisV373)
                return;

            long renderStarted = ProfileUnitRuntimePhasesLikeOriginal ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
            int renderUnitCountLikeOriginal = UseOriginalDrawUnitsCellVisibilityLikeOriginal
                ? _drawUnitsCurrentLikeOriginal.Count
                : _units.Count;
            Camera frameTransformCamera = FindUnitScaleReferenceCameraLikeOriginal();
            if (frameTransformCamera == null) frameTransformCamera = drawCameraLikeOriginal;
            Quaternion frameTransformRotation = frameTransformCamera != null
                ? frameTransformCamera.transform.rotation : Quaternion.identity;
            using (RenderVisibleMarkerV370.Auto())
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Sprites))
            {
                for (int i = 0; i < renderUnitCountLikeOriginal; i++)
                {
                    C2UnitOriginalRuntime u = UseOriginalDrawUnitsCellVisibilityLikeOriginal
                        ? _drawUnitsCurrentLikeOriginal[i]
                        : _units[i];
                    if (u == null || u.Md == null) continue;
                    bool renderRelevant = u.Selected || ShouldMaterializeUnitFrameLikeOriginal(u);
                    if (u.OriginalComplexObjectV430LikeOriginal != null)
                    {
                        RenderComplexRuntimeV437(u, renderRelevant);
                        if (u.Selected || (u.SelectionRingObject != null && u.SelectionRingObject.activeSelf))
                            UpdateSelectionRingLikeOriginal(u);
                        continue;
                    }
                    if (renderRelevant)
                    {
                        _visualRenderRelevantV373LikeOriginal++;
                        // MiniMap4X.cpp::DrawSpriteUnit advances OctantInfo on render
                        // frames, including frames without a simulation/animation tick.
                        bool settlingDirection = u.OctantInfo != 0xFF && (u.OctantInfo >> 4) != 0;
                        if ((u.FrameUploadPendingLikeOriginal || settlingDirection) && !SkipUnitFrameApplyForPerformanceDiagnosisV373)
                            u.FrameUploadPendingLikeOriginal = !ApplyUnitFrameLikeOriginal(u, "draw_units_visible");
                        if (!SkipUnitRenderTransformForPerformanceDiagnosisV373)
                        {
                            ApplyViewerLikePixelPerfectTransformLikeOriginal(u, frameTransformCamera, frameTransformRotation);
                            UpdateUnitSortingOrderLikeOriginal(u, unitSortCamera);
                        }
                    }
                    if (u.Selected ||
                        Mathf.Abs(u.LastSelectionBrightnessMultiplierLikeOriginal - 1.0f) >= 0.001f ||
                        u.LastAppliedFogVisibilityValueLikeOriginal != u.OriginalFogVisibilityValueLikeOriginal)
                        UpdateSelectionBrightnessPulseLikeOriginal(u);
                    if (u.Selected || (u.SelectionRingObject != null && u.SelectionRingObject.activeSelf))
                        UpdateSelectionRingLikeOriginal(u);
                }
            }
            if (!SkipGpsBatchBuildForPerformanceDiagnosisV373)
            {
                using (BatchBuildMarkerV370.Auto())
                using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Batches))
                    RebuildOriginalGpsUnitBatchesLikeOriginal(renderUnitCountLikeOriginal);
            }
            if (ProfileUnitRuntimePhasesLikeOriginal)
            {
                long profileNowTicksLikeOriginal = global::System.Diagnostics.Stopwatch.GetTimestamp();
                _profileRenderTicksLikeOriginal += profileNowTicksLikeOriginal - renderStarted;
                _profileRenderFramesLikeOriginal++;
                _profileWholeUpdateTicksLikeOriginal += profileNowTicksLikeOriginal - wholeUpdateStartedLikeOriginal;
                _profileWholeUpdateFramesLikeOriginal++;
                float now = Time.realtimeSinceStartup;
                if (now >= _nextProfileReportAtLikeOriginal)
                {
                    _nextProfileReportAtLikeOriginal = now + 5.0f;
                    double msPerTick = 1000.0 / global::System.Diagnostics.Stopwatch.Frequency;
                    Debug.Log("[C2:UNIT PHASE PROFILE] units=" + _units.Count.ToString(CultureInfo.InvariantCulture) +
                              " simSteps=" + _profileSimulationStepsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " renderFrames=" + _profileRenderFramesLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " bucketMsPerStep=" + (_profileBucketTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " stepMsPerStep=" + (_profileStepTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " stateMsPerStep=" + (_profileStateTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " tiringMsPerStep=" + (_profileTiringTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " animationMsPerStep=" + (_profileAnimationTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " frameApplyMsPerStep=" + (_profileFrameApplyTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " boidsMsPerStep=" + (_profileBoidsTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " boidPairs=" + _originalBoidsNeighborPairsLikeOriginal.Count.ToString(CultureInfo.InvariantCulture) +
                              " boidNeighborRefreshes=" + _originalBoidsNeighborRefreshesLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " motionFieldMsPerStep=" + (_profileMotionFieldTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " worldSyncMsPerStep=" + (_profileWorldSyncTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " blockedMsPerStep=" + (_profileBlockedMoveTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " movingCallsPerStep=" + ((double)_profileMovingCallsLikeOriginal / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.0", CultureInfo.InvariantCulture) +
                              " blockedCallsPerStep=" + ((double)_profileBlockedMoveCallsLikeOriginal / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.0", CultureInfo.InvariantCulture) +
                              " drawCellRebuilds=" + _profileDrawCellRebuildsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " drawCellMsPerRebuild=" + (_profileDrawCellRebuildTicksLikeOriginal * msPerTick / Math.Max(1, _profileDrawCellRebuildsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " inputMsPerFrame=" + (_profileInputTicksLikeOriginal * msPerTick / Math.Max(1, _profileWholeUpdateFramesLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " formationKeepMsPerStep=" + (_profileFormationKeepTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " orderMsPerStep=" + (_profileOrderTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " fogTurnsMsPerStep=" + (_profileFogAndTurnsTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " drawListFogMsPerFrame=" + (_profileDrawListAndFogOverlayTicksLikeOriginal * msPerTick / Math.Max(1, _profileWholeUpdateFramesLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " separationMsPerStep=" + (_profileSeparationTicksLikeOriginal * msPerTick / Math.Max(1, _profileSimulationStepsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " renderMsPerFrame=" + (_profileRenderTicksLikeOriginal * msPerTick / Math.Max(1, _profileRenderFramesLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " wholeUpdateMsPerFrame=" + (_profileWholeUpdateTicksLikeOriginal * msPerTick / Math.Max(1, _profileWholeUpdateFramesLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                              " spawnCalls=" + _profileSpawnCallsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " spawnTotalMs=" + (_profileSpawnTicksLikeOriginal * msPerTick).ToString("0.000", CultureInfo.InvariantCulture) +
                              " spawnMaxMs=" + (_profileSpawnMaxTicksLikeOriginal * msPerTick).ToString("0.000", CultureInfo.InvariantCulture));
                    _profileSpawnTicksLikeOriginal = 0;
                    _profileSpawnMaxTicksLikeOriginal = 0;
                    _profileSpawnCallsLikeOriginal = 0;
                    _profileBucketTicksLikeOriginal = 0L;
                    _profileStepTicksLikeOriginal = 0L;
                    _profileSeparationTicksLikeOriginal = 0L;
                    _profileRenderTicksLikeOriginal = 0L;
                    _profileStateTicksLikeOriginal = 0L;
                    _profileTiringTicksLikeOriginal = 0L;
                    _profileAnimationTicksLikeOriginal = 0L;
                    _profileFrameApplyTicksLikeOriginal = 0L;
                    _profileBoidsTicksLikeOriginal = 0L;
                    _profileMotionFieldTicksLikeOriginal = 0L;
                    _profileWorldSyncTicksLikeOriginal = 0L;
                    _profileBlockedMoveTicksLikeOriginal = 0L;
                    _profileDrawCellRebuildTicksLikeOriginal = 0L;
                    _profileWholeUpdateTicksLikeOriginal = 0L;
                    _profileInputTicksLikeOriginal = 0L;
                    _profileFormationKeepTicksLikeOriginal = 0L;
                    _profileOrderTicksLikeOriginal = 0L;
                    _profileFogAndTurnsTicksLikeOriginal = 0L;
                    _profileDrawListAndFogOverlayTicksLikeOriginal = 0L;
                    _profileDrawCellRebuildsLikeOriginal = 0;
                    _profileMovingCallsLikeOriginal = 0;
                    _profileBlockedMoveCallsLikeOriginal = 0;
                    _originalBoidsNeighborRefreshesLikeOriginal = 0;
                    _profileSimulationStepsLikeOriginal = 0;
                    _profileRenderFramesLikeOriginal = 0;
                    _profileWholeUpdateFramesLikeOriginal = 0;
                }
            }
        }

        private void HandleRuntimeHotkeysLikeOriginal()
        {
            // Compile hook only. Unit runtime hotkeys are disabled in V4A.
        }

        private void HandleDeleteSelectedDeathHotkeyLikeOriginal()
        {
            if (!EnableDeleteSelectedDeathLikeOriginal) return;
            if (!IsDeletePressedThisFrameLikeOriginal()) return;
            if (Time.unscaledTime - _lastDeleteAt < 0.12f) return;
            _lastDeleteAt = Time.unscaledTime;

            int killed = PlayDeathForSelectedUnitsLikeOriginal("delete_hotkey");
            if (killed > 0)
                C2GameplayHudV1.ForceRefreshLikeOriginal();
        }

        private static bool IsDeletePressedThisFrameLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Delete)) return true;
#endif
            return false;
        }

        private int PlayDeathForSelectedUnitsLikeOriginal(string reason)
        {
            int killed = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (u == null || u.State == C2UnitOriginalState.Death) continue;
                bool selected = u.Selected || _selected == u;
                if (!selected && u.Info != null) selected = u.Info.IsSelected;
                if (!selected) continue;

                if (PlayRuntimeDeathLikeOriginal(u, (byte)(u.RealDirPrecise & 255), reason))
                    killed++;
            }
            return killed;
        }

        internal bool PlayRuntimeDeathLikeOriginal(C2UnitOriginalRuntime u, byte realDir, string reason)
        {
            if (u == null || u.Md == null) return false;
            if (u.State == C2UnitOriginalState.Death) return false;
            // Nation.cpp::OneObject::Die returns here when only the complex crew is lost.
            if (u.OriginalComplexObjectV430LikeOriginal != null && !DieComplexObjectV441(u)) return false;

            int death = ResolveAnimationIndexLikeOriginal(u.Md, DeathAnimationName);
            if (death < 0) death = ResolveAnimationIndexLikeOriginal(u.Md, "#DEATH");
            if (death < 0 && DeathFallbackToDeathLie1LikeOriginal) death = ResolveAnimationIndexLikeOriginal(u.Md, "#DEATHLIE1");
            if (death < 0 && DeathFallbackToDeathLie1LikeOriginal) death = ResolveAnimationIndexLikeOriginal(u.Md, "#DEATHLIE2");
            if (death < 0 || death >= u.Md.Animations.Count)
            {
                // LongProcesses removes a dead non-building with no DEATH clip.
                // A missing visual must never leave a live selectable zero-HP unit.
                u.State = C2UnitOriginalState.Death;
                ExpireOriginalDeathV435LikeOriginal(u);
                return true;
            }

            if (DeathStopsMovementLikeOriginal)
            {
                EmitBornExitAuditLikeOriginal(u, "move_path_complete", "hasMoveTargetBeforeClear=" + u.HasMoveTargetLikeOriginal);
                u.HasMoveTargetLikeOriginal = false;
            }

            int finalDir = realDir & 255;
            if (DeathRandomRotateLikeOriginal && DeathRandomDirSpreadLikeOriginal > 0)
            {
                float rnd = StableUnitRandom01LikeOriginal(u.Probe, 1009 + u.UnitOrder + Mathf.RoundToInt(Time.unscaledTime * 1000.0f));
                int spread = Mathf.Clamp(DeathRandomDirSpreadLikeOriginal, 0, 128);
                int delta = Mathf.RoundToInt(Mathf.Lerp(-spread, spread - 1, rnd));
                finalDir = (finalDir + delta) & 255;
            }
            SetRuntimeFacingLikeOriginal(u, (byte)finalDir);

            if (DeathClearsSelectionLikeOriginal)
            {
                if (_selected == u) _selected = null;
                u.Selected = false;
                if (u.Info != null) u.Info.SetSelectedFromRuntimeLikeOriginal(false);
            }

            SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Death, death, true, reason ?? "death");
            u.OriginalDeathCounterV435LikeOriginal = 1;
            if (u.AnimState != null) u.AnimState.Reason = reason ?? "death";
            u.FrameFinishedLikeOriginal = false;
            u.LastFrameKey = string.Empty;

            UpdateSelectionBrightnessPulseLikeOriginal(u);
            if (DeathHideSelectionRingLikeOriginal && u.SelectionRingObject != null)
                u.SelectionRingObject.SetActive(false);
            else
                UpdateSelectionRingLikeOriginal(u);

            ApplyUnitFrameLikeOriginal(u, "death_start_" + (reason ?? ""));

            if (LogDeathLikeOriginal && _deathLogs < 32)
            {
                _deathLogs++;
                AnimModel a = CurrentAnim(u);
                Debug.Log(LogPrefix + " DEATH_LIKE_ORIGINAL source=DeleteSelected->DestructBuilding->OneObject.Die->anm_Death" +
                          " reason=" + (reason ?? "") +
                          " unit='" + (u.Probe != null ? u.Probe.MonsterId : "") + "'" +
                          " md='" + (u.Md != null ? u.Md.Name : "") + "'" +
                          " anim='" + (a != null ? a.Name : "") + "'" +
                          " frames=" + (a != null ? a.Frames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                          " realDir=" + finalDir.ToString(CultureInfo.InvariantCulture) +
                          " stopMove=" + DeathStopsMovementLikeOriginal +
                          " clearSelection=" + DeathClearsSelectionLikeOriginal);
            }

            return true;
        }

        private IEnumerator RunWhenBattleReadyLikeOriginal(string source)
        {
            if (_busy) yield break;
            _busy = true;

            int waitFrames = 0;
            while (waitFrames < 600)
            {
                _battle = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                if (_battle != null && TryResolveBattleContextLikeOriginal(_battle, out _dataRoot, out _mapRelativePath, out _mapAbsPath))
                    break;
                waitFrames++;
                yield return null;
            }

            if (_battle == null || string.IsNullOrEmpty(_mapAbsPath) || !File.Exists(_mapAbsPath))
            {
                Debug.LogWarning(LogPrefix + " wait_failed source=" + source + " battle=" + (_battle != null) + " mapAbs='" + (_mapAbsPath ?? "") + "'");
                _busy = false;
                yield break;
            }

            yield return null;
            yield return null;

            bool ok = InitializeAllUnitsLikeOriginal(source);
            _started = ok;
            _busy = false;
        }

        private bool InitializeAllUnitsLikeOriginal(string source)
        {
            // The original map editor is an omniscient authoring mode.  FOW is
            // a gameplay observer filter and must never cover the editor/test
            // palette viewport.
            if (C2BattleTerrainMode.EditorTestModeLikeOriginal && UseOriginalFogOfWarLikeOriginal)
            {
                UseOriginalFogOfWarLikeOriginal = false;
                Shader.SetGlobalFloat("_C2FogEnabledLikeOriginal", 0.0f);
                Debug.Log(LogPrefix + " FOW_DISABLED reason=editor_test_mode");
            }

            ClearPreviousRuntimeLikeOriginal();

            // The shipped Models\MapAutosave.m3d contains leftover 3INU artillery/crew records.
            // The original new-map editor starts from terrain only; its Creator tool adds units later.
            if (C2BattleTerrainMode.EditorTestModeLikeOriginal)
            {
                _runtimeRoot = new GameObject("C2_UnitOriginalRuntimeV3_Units");
                _runtimeRoot.transform.SetParent(transform, false);
                _initialized = true;
                Debug.Log(LogPrefix + " SUMMARY source=" + source +
                          " map='" + _mapRelativePath + "' unitsCreated=0" +
                          " skippedSavedEditor3INU=True contract=V333_clean_editor_terrain");
                return true;
            }

            string parseAudit;
            IList records;
            if (!TryParse3InuRecordsByReflectionLikeOriginal(_mapAbsPath, out records, out parseAudit) || records == null || records.Count == 0)
            {
                Debug.LogWarning(LogPrefix + " no_3inu_records source=" + source + " map='" + _mapAbsPath + "' audit=" + (parseAudit ?? ""));
                return false;
            }

            _runtimeRoot = new GameObject("C2_UnitOriginalRuntimeV3_Units");
            _runtimeRoot.transform.SetParent(transform, false);

            int inspected = 0;
            int units = 0;
            int skippedNonUnit = 0;
            int skippedMd = 0;
            int skippedWorld = 0;
            int skippedAnim = 0;
            int mdCacheHits = 0;
            int mdCacheMisses = 0;

            for (int i = 0; i < records.Count; i++)
            {
                inspected++;
                object rec = records[i];
                string monsterId = GetFieldString(rec, "MonsterId", "");
                if (string.IsNullOrWhiteSpace(monsterId)) { skippedNonUnit++; continue; }

                object mdRef = ResolveMdByReflectionLikeOriginal(monsterId);
                if (mdRef == null || !GetFieldBool(mdRef, "Found", false)) { skippedMd++; continue; }

                string kind = GetFieldObject(mdRef, "Kind") != null ? GetFieldObject(mdRef, "Kind").ToString() : string.Empty;
                bool isUnit = string.Equals(kind, "Unit", StringComparison.OrdinalIgnoreCase);
                bool isAnimal = string.Equals(kind, "Animal", StringComparison.OrdinalIgnoreCase);
                bool isBuilding = kind.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0 || GetFieldBool(mdRef, "Building", false);
                if (!isUnit || isAnimal || isBuilding) { skippedNonUnit++; continue; }

                var probe = new UnitProbe();
                probe.RawRecord = rec;
                probe.RawMd = mdRef;
                probe.MonsterId = monsterId;
                probe.MdName = GetFieldString(mdRef, "MdName", monsterId);
                probe.MdPath = GetFieldString(mdRef, "MdPath", "");
                probe.DefaultPackage = GetFieldString(mdRef, "Package", "");
                probe.Nation = GetFieldInt(rec, "Nation", 0);
                probe.RealX = GetFieldInt(rec, "RealX", 0);
                probe.RealY = GetFieldInt(rec, "RealY", 0);
                probe.RealDir = GetFieldInt(rec, "RealDir", 0) & 255;
                probe.Index = GetFieldInt(rec, "Index", i);

                Vector3 world;
                if (!TryWorldPosByReflectionLikeOriginal(_battle, rec, out world)) { skippedWorld++; continue; }

                string mdPath = ResolveMdFilePathLikeOriginal(probe);
                if (string.IsNullOrWhiteSpace(mdPath) || !File.Exists(mdPath)) { skippedMd++; continue; }

                MdModel md;
                string mdAudit;
                if (_mdCache.TryGetValue(mdPath, out md))
                {
                    mdCacheHits++;
                    mdAudit = _mdAuditCache.ContainsKey(mdPath) ? _mdAuditCache[mdPath] : "cache_hit";
                }
                else
                {
                    mdCacheMisses++;
                    if (!TryParseMdLikeOriginal(mdPath, out md, out mdAudit))
                    {
                        skippedMd++;
                        continue;
                    }
                    _mdCache[mdPath] = md;
                    _mdAuditCache[mdPath] = mdAudit;
                }

                int standAnim = ResolveAnimationIndexLikeOriginal(md, StandAnimationName);
                if (standAnim < 0) standAnim = ResolveAnimationIndexLikeOriginal(md, RestAnimationName);
                if (!HasRuntimeVisualV437(md, mdPath, standAnim)) { skippedAnim++; continue; }

                C2UnitOriginalRuntime u = CreateRuntimeUnitLikeOriginal(probe, md, mdPath, world, standAnim, units);
                if (u == null) { skippedAnim++; continue; }
                _units.Add(u);
                _originalSetInCellTime256LikeOriginal = 0;
                if (UseOriginalDrawUnitsCellVisibilityLikeOriginal)
                    RegisterOriginalDrawUnitInCellLikeOriginal(u);
                units++;

                if (LogCreatedUnits && _createdLogs < MaxUnitCreateLogs)
                {
                    _createdLogs++;
                    Debug.Log(LogPrefix + " CREATE unit='" + probe.MonsterId + "' md='" + md.Name + "' kind='" + kind + "'" +
                              " index=" + probe.Index.ToString(CultureInfo.InvariantCulture) +
                              " nation=" + probe.Nation.ToString(CultureInfo.InvariantCulture) +
                              " real=(" + probe.RealX.ToString(CultureInfo.InvariantCulture) + "," + probe.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                              " world=" + world.ToString("F3") +
                              " realDir=" + probe.RealDir.ToString(CultureInfo.InvariantCulture) +
                              " anim='" + (standAnim >= 0 ? md.Animations[standAnim].Name : md.ComplexObjectIdLikeOriginal) + "' frames=" + (standAnim >= 0 ? md.Animations[standAnim].Frames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                              " rotations=" + (standAnim >= 0 ? md.Animations[standAnim].Rotations : 0).ToString(CultureInfo.InvariantCulture) +
                              " mdCache=" + (mdCacheHits > 0 ? "shared" : "new") +
                              " mdAudit=" + mdAudit);
                }
            }

            _initialized = true;
            if (AutoFocusCameraOnFirstUnit && _units.Count > 0)
            {
                string focusAudit;
                bool focusOk = TryFocusBattleCameraOnProbeLikeOriginal(_units[0].WorldPosition, out focusAudit);
                Debug.Log(LogPrefix + " CAMERA_FOCUS_FIRST_UNIT ok=" + focusOk + " " + focusAudit);
            }

            Debug.Log(LogPrefix + " SUMMARY source=" + source +
                      " map='" + _mapRelativePath + "'" +
                      " inspected=" + inspected.ToString(CultureInfo.InvariantCulture) +
                      " unitsCreated=" + units.ToString(CultureInfo.InvariantCulture) +
                      " skippedNonUnit=" + skippedNonUnit.ToString(CultureInfo.InvariantCulture) +
                      " skippedMd=" + skippedMd.ToString(CultureInfo.InvariantCulture) +
                      " skippedWorld=" + skippedWorld.ToString(CultureInfo.InvariantCulture) +
                      " skippedAnim=" + skippedAnim.ToString(CultureInfo.InvariantCulture) +
                      " mdCacheHits=" + mdCacheHits.ToString(CultureInfo.InvariantCulture) +
                      " mdCacheMisses=" + mdCacheMisses.ToString(CultureInfo.InvariantCulture) +
                      " g2dRule=provider_file_cache_parses_each_package_once frameTextures_shared_by_path_sprite_color" +
                      " parseAudit=" + (parseAudit ?? ""));
            return units > 0;
        }

        private C2UnitOriginalRuntime CreateRuntimeUnitLikeOriginal(UnitProbe probe, MdModel md, string mdPath, Vector3 world, int idleAnim, int unitOrder)
        {
            var u = new C2UnitOriginalRuntime();
            u.Probe = probe;
            u.Md = md;
            u.MdPath = mdPath;
            u.WorldPosition = world + new Vector3(0f, DebugYOffset, -0.35f);
            u.State = C2UnitOriginalState.Stand;
            u.AnimState = new C2UnitOriginalAnimationState();
            u.RealDirPrecise = probe.RealDir & 255;
            u.OriginalRealDirPrecise256LikeOriginal = (probe.RealDir & 255) << 8;
            u.OctantInfo = 0xFF;
            u.CurrentFrameLong = 0;
            u.RuntimeRealXLikeOriginal = probe.RealX;
            u.RuntimeRealYLikeOriginal = probe.RealY;
            u.MoveTargetRealXLikeOriginal = probe.RealX;
            u.MoveTargetRealYLikeOriginal = probe.RealY;
            u.OriginalMotionDistLikeOriginal = md != null && md.MotionDist > 0 ? md.MotionDist : Mathf.RoundToInt(OriginalMotionDefaultSpeedOriginalPixelsPerSecond);
            u.OriginalGroupSpeedV431LikeOriginal = u.OriginalMotionDistLikeOriginal;
            u.OriginalMoreCharacterSpeedPercentLikeOriginal = md != null && md.MoreCharacterSpeedPercent > 0 ? md.MoreCharacterSpeedPercent : 100;
            u.BaseMoveSpeedOriginalPixelsPerSecondLikeOriginal = ResolveMdMoveSpeedOriginalPixelsPerSecondLikeOriginal(md);
            u.MoveSpeedOriginalPixelsPerSecondLikeOriginal = u.BaseMoveSpeedOriginalPixelsPerSecondLikeOriginal;
            u.MoveSpeedWorldLikeOriginal = u.BaseMoveSpeedOriginalPixelsPerSecondLikeOriginal * 0.1f;
            u.CanBuildLikeOriginal = md != null && (md.CanBuild || md.Pioneer);
            u.PioneerLikeOriginal = md != null && md.Pioneer;
            u.TotalPathLikeOriginal = 0f;
            u.AnimFps = DefaultAnimFps;
            u.LastFrameKey = string.Empty;
            // Corpse removal compacts _units, but live OneObject identities must
            // never reuse the index of another still-live unit after production.
            unitOrder = Math.Max(unitOrder, _nextRuntimeUnitOrderV435LikeOriginal);
            _nextRuntimeUnitOrderV435LikeOriginal = checked(unitOrder + 1);
            u.UnitOrder = unitOrder;
            u.NextRestCheckTime = Time.unscaledTime + StableUnitRandomRangeLikeOriginal(probe, 11, RestMinDelaySeconds, RestMaxDelaySeconds);
            u.CurrentAnimIndex = -1;
            SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, idleAnim, true, "create");

            Camera cam = FindBattleCameraLikeOriginal();
            int visibleLayer = C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera
                ? C2SpriteDepthLayerLikeOriginal.LayerIndex
                : ResolveLayerVisibleByBattleCameraLikeOriginal(cam);
            u.UnityProxyNameLikeOriginal = "C2UnitOriginal_" + unitOrder.ToString(CultureInfo.InvariantCulture) + "_" + SanitizeName(probe.MonsterId);
            u.VisibleLayerLikeOriginal = visibleLayer;
            u.WorldRotationLikeOriginal = Quaternion.identity;
            u.WorldScaleLikeOriginal = Vector3.one;
            u.ActiveLikeOriginal = true;
            u.RenderedByOriginalGpsBatchLikeOriginal = UseOriginalGpsUnitBatchRendererLikeOriginal;
            u.BodyQuadVerticesLikeOriginal = new Vector3[4];
            u.BodyQuadUvsLikeOriginal = new Vector2[4];

            EnsureCompatibilityUnitInfoLikeOriginal(u);
            C2UnitOriginalRuntimeLinkLikeOriginal link =
                u.Info != null ? u.Info.RuntimeLinkCachedLikeOriginal : null;
            if (link == null)
            {
                link = new C2UnitOriginalRuntimeLinkLikeOriginal();
                if (u.Info != null) u.Info.BindRuntimeLinkV364LikeOriginal(link);
            }
            link.BindLikeOriginal(this, u);
            if (!string.IsNullOrEmpty(md.ComplexObjectIdLikeOriginal))
            {
                EnsureComplexObjectRuntimeV430LikeOriginal(u);
                UpdateRuntimeWorldAndRealLikeOriginal(u);
            }
            if (!u.RenderedByOriginalGpsBatchLikeOriginal)
                EnsureRuntimeUnityProxyLikeOriginal(u);
            UpdateUnitSortingOrderLikeOriginal(u, cam);

            // Selection visuals are rare.  Creating a GameObject, Mesh and
            // Renderer for every one of several hundred unselected units doubled
            // the renderer count and added needless culling/update work.
            if (u.OriginalComplexObjectV430LikeOriginal != null)
                u.FrameUploadPendingLikeOriginal = false;
            else if (ShouldMaterializeUnitFrameLikeOriginal(u))
                u.FrameUploadPendingLikeOriginal = !ApplyUnitFrameLikeOriginal(u, "initial");
            else
                u.FrameUploadPendingLikeOriginal = true;
            return u;
        }

        internal GameObject EnsureRuntimeUnityProxyLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return null;
            if (u.Root != null) return u.Root;

            GameObject go = new GameObject(string.IsNullOrEmpty(u.UnityProxyNameLikeOriginal)
                ? "C2UnitOriginal_" + u.UnitOrder.ToString(CultureInfo.InvariantCulture)
                : u.UnityProxyNameLikeOriginal);
            go.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : transform, true);
            go.transform.position = u.WorldPosition;
            go.transform.rotation = u.WorldRotationLikeOriginal;
            go.transform.localScale = u.WorldScaleLikeOriginal;
            go.SetActive(u.ActiveLikeOriginal);
            u.Root = go;
            SetLayerRecursiveLikeOriginal(go, u.VisibleLayerLikeOriginal);
            if (u.Info != null)
                u.Info.C2AttachUnityProxyV366LikeOriginal(go);
            if (!u.RenderedByOriginalGpsBatchLikeOriginal)
                EnsureIndividualUnitRenderersLikeOriginal(u, u.VisibleLayerLikeOriginal);
            return go;
        }

        internal void SetRuntimeActiveLikeOriginal(C2UnitOriginalRuntime u, bool active)
        {
            if (u == null) return;
            u.ActiveLikeOriginal = active;
            if (u.Root != null && u.Root.activeSelf != active)
                u.Root.SetActive(active);
        }

        private void EnsureIndividualUnitRenderersLikeOriginal(C2UnitOriginalRuntime u, int visibleLayer)
        {
            if (u == null || u.Root == null) return;

            if (u.Mesh == null)
            {
                u.Mesh = new Mesh();
                u.Mesh.name = u.Root.name + "_Mesh";
                u.Mesh.MarkDynamic();
            }
            if (u.MeshFilter == null)
            {
                u.MeshFilter = u.Root.GetComponent<MeshFilter>();
                // Unity can return a managed missing-component wrapper; ?? does
                // not use Unity's destroyed/missing object equality operator.
                if (u.MeshFilter == null) u.MeshFilter = u.Root.AddComponent<MeshFilter>();
            }
            u.MeshFilter.sharedMesh = u.Mesh;

            if (u.MeshRenderer == null)
            {
                u.MeshRenderer = u.Root.GetComponent<MeshRenderer>();
                if (u.MeshRenderer == null) u.MeshRenderer = u.Root.AddComponent<MeshRenderer>();
                u.MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                u.MeshRenderer.receiveShadows = false;
            }
            u.MeshRenderer.sortingOrder = SortingOrderBase + u.UnitOrder;
            Texture2D texture = u.LastTexture != null ? u.LastTexture : Texture2D.whiteTexture;
            u.Material = GetUnitBodyMaterialForTextureLikeOriginal(texture);
            u.MeshRenderer.sharedMaterial = u.Material;

            Vector3[] vertices = u.BodyQuadVerticesLikeOriginal;
            Vector2[] uvs = u.BodyQuadUvsLikeOriginal;
            if (vertices != null && vertices.Length == 4 && uvs != null && uvs.Length == 4)
            {
                u.Mesh.Clear(false);
                u.Mesh.vertices = vertices;
                u.Mesh.uv = uvs;
                u.Mesh.triangles = C2UnitOriginalRuntime.BodyQuadTrianglesLikeOriginal;
                u.BodyQuadTrianglesAssignedLikeOriginal = true;
                u.Mesh.RecalculateBounds();
            }

            if ((UseUnitBuildingDepthPrepassLikeOriginal || C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera) &&
                u.DepthMeshRenderer == null)
                CreateUnitDepthPrepassRendererLikeOriginal(u, visibleLayer);
            if (u.DepthMeshFilter != null) u.DepthMeshFilter.sharedMesh = u.Mesh;
            if (u.DepthMeshRenderer != null)
            {
                u.DepthMaterial = GetUnitDepthMaterialForTextureLikeOriginal(texture);
                u.DepthMeshRenderer.sharedMaterial = u.DepthMaterial;
                u.DepthMeshRenderer.sortingOrder = u.MeshRenderer.sortingOrder;
            }
            if (u.Info != null) u.Info.SpriteMeshRenderer = u.MeshRenderer;
        }

        private float ResolveMdMoveSpeedOriginalPixelsPerSecondLikeOriginal(MdModel md)
        {
            int motionDist = md != null && md.MotionDist > 0
                ? md.MotionDist
                : Mathf.RoundToInt(Mathf.Max(1.0f, OriginalMotionDefaultSpeedOriginalPixelsPerSecond));
            int speedPercent = md != null && md.MoreCharacterSpeedPercent > 0 ? md.MoreCharacterSpeedPercent : 100;
            // GEOMETRY MotionDist is a RealX/RealY displacement per original
            // 40 ms tick, not pixels/second. Real coordinates have 16 units
            // per map pixel: px/s = MotionDist * 25 / 16.
            const float originalTicksPerSecond = 25.0f;
            const float originalRealUnitsPerPixel = 16.0f;
            return Mathf.Max(1.0f,
                motionDist * (originalTicksPerSecond / originalRealUnitsPerPixel) * speedPercent / 100.0f);
        }

        private float ResolveRuntimeMoveSpeedOriginalPixelsPerSecondLikeOriginal(C2UnitOriginalRuntime u, float requestedSpeedOriginalPixelsPerSecond)
        {
            float baseSpeed = u != null && u.BaseMoveSpeedOriginalPixelsPerSecondLikeOriginal > 0.001f
                ? u.BaseMoveSpeedOriginalPixelsPerSecondLikeOriginal
                : 0.0f;

            bool legacyDefaultRequest =
                requestedSpeedOriginalPixelsPerSecond <= 0.001f ||
                Mathf.Abs(requestedSpeedOriginalPixelsPerSecond - C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal) <= 0.01f ||
                Mathf.Abs(requestedSpeedOriginalPixelsPerSecond - OriginalMotionDefaultSpeedOriginalPixelsPerSecond) <= 0.01f;

            if (baseSpeed > 0.001f && legacyDefaultRequest)
                return Mathf.Max(1.0f, baseSpeed);

            if (requestedSpeedOriginalPixelsPerSecond > 0.001f)
                return Mathf.Max(1.0f, requestedSpeedOriginalPixelsPerSecond);

            if (baseSpeed > 0.001f)
                return Mathf.Max(1.0f, baseSpeed);

            return Mathf.Max(1.0f, OriginalMotionDefaultSpeedOriginalPixelsPerSecond);
        }

        private void CreateUnitDepthPrepassRendererLikeOriginal(C2UnitOriginalRuntime u, int visibleLayer)
        {
            if (u == null || u.Root == null || u.Mesh == null)
                return;

            u.DepthMaterial = GetUnitDepthMaterialForTextureLikeOriginal(Texture2D.whiteTexture);
            u.DepthMeshRenderer = C2SpriteDepthPrepassLikeOriginal.AddDepthRendererLikeOriginal(
                u.Root,
                u.Mesh,
                u.DepthMaterial,
                u.MeshRenderer != null ? u.MeshRenderer.sortingOrder : SortingOrderBase + u.UnitOrder,
                "depth_cutout_prepass");
            if (u.DepthMeshRenderer != null)
            {
                u.DepthMeshRenderer.gameObject.layer = visibleLayer;
                u.DepthMeshFilter = u.DepthMeshRenderer.GetComponent<MeshFilter>();
            }
        }

        private static void ConfigureUnitDepthMaterialLikeOriginal(Material mat)
        {
            if (mat == null)
                return;

            if (mat.HasProperty("_C2IgnoreDarkShadowPixels")) mat.SetFloat("_C2IgnoreDarkShadowPixels", 1.0f);
            if (mat.HasProperty("_C2ShadowMaxAlpha")) mat.SetFloat("_C2ShadowMaxAlpha", 0.985f);
            if (mat.HasProperty("_C2ShadowMaxLuma")) mat.SetFloat("_C2ShadowMaxLuma", 0.52f);
            if (mat.HasProperty("_C2ShadowMaxSaturation")) mat.SetFloat("_C2ShadowMaxSaturation", 0.32f);
        }

        public static bool TrySpawnProducedUnitFromBuildingLikeOriginal(
            C2BattleTerrainMode battle,
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            int nation,
            out C2NeutralPeasantUnitInfoV2LikeOriginal spawned,
            out string audit)
        {
            spawned = null;
            audit = string.Empty;
            if (item == null) { audit = "item=<null>"; return false; }
            if (item.Building) { audit = "item_is_building"; return false; }

            C2UnitOriginalRuntimeAndRendererV1 rt = UnityEngine.Object.FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
            if (rt == null)
            {
                GameObject host = GameObject.Find(RootName);
                if (host == null)
                {
                    host = new GameObject(RootName);
                    DontDestroyOnLoad(host);
                }
                rt = host.GetComponent<C2UnitOriginalRuntimeAndRendererV1>();
                if (rt == null) rt = host.AddComponent<C2UnitOriginalRuntimeAndRendererV1>();
                rt.AutoRunOnceAfterMapLoad = false;
            }

            if (battle == null) battle = building != null && building.OwnerMode != null ? building.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            return rt.TrySpawnProducedUnitInstanceLikeOriginal(battle, building, item, nation, out spawned, out audit);
        }

        public static bool TrySpawnEditorUnitAtRealV332LikeOriginal(
            C2BattleTerrainMode battle,
            C2OriginalProduceItemV13 item,
            int nation,
            int realX,
            int realY,
            out C2NeutralPeasantUnitInfoV2LikeOriginal spawned,
            out string audit)
        {
            spawned = null;
            audit = string.Empty;
            if (item == null || item.Building) { audit = item == null ? "item=<null>" : "item_is_building"; return false; }

            C2UnitOriginalRuntimeAndRendererV1 rt = UnityEngine.Object.FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
            if (rt == null)
            {
                GameObject host = GameObject.Find(RootName);
                if (host == null)
                {
                    host = new GameObject(RootName);
                    DontDestroyOnLoad(host);
                }
                rt = host.GetComponent<C2UnitOriginalRuntimeAndRendererV1>();
                if (rt == null) rt = host.AddComponent<C2UnitOriginalRuntimeAndRendererV1>();
                rt.AutoRunOnceAfterMapLoad = false;
            }
            return rt.TrySpawnProducedUnitInstanceLikeOriginal(
                battle, null, item, nation, out spawned, out audit, realX, realY);
        }

        internal static bool TryGetEditorFormationScaleV375LikeOriginal(
            C2OriginalProduceItemV13 item, out int scale, out string audit)
        {
            scale = 100;
            audit = "editor_md_missing";
            if (item == null) return false;
            string mdPath = C2OriginalProduceCatalogV13.LoadMdIcon(
                string.IsNullOrEmpty(item.MdName) ? item.UnitId : item.MdName).Path;
            MdModel md;
            if (string.IsNullOrEmpty(mdPath) || !TryParseMdLikeOriginal(mdPath, out md, out audit))
                return false;
            scale = Mathf.Max(1, md.FormationDistanceScale);
            return true;
        }

        public static void RefreshNationColorsV332LikeOriginal()
        {
            C2UnitOriginalRuntimeAndRendererV1 rt = UnityEngine.Object.FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
            if (rt == null) return;
            for (int i = 0; i < rt._units.Count; i++)
            {
                C2UnitOriginalRuntime u = rt._units[i];
                if (u == null) continue;
                u.LastFrameKey = string.Empty;
                rt.ApplyUnitFrameLikeOriginal(u, "nation_color_refresh_v332");
            }
        }


        // V259: production exit must use THIS building instance, not a nearest/global runtime record.
        // Original Build.cpp::ProduceObjLink uses OBJ->newMons (the produced building object).
        // The previous bridge fallback could match any runtime info when MdName/SourceMonsterId was empty,
        // so an English queue could take AusKaz BORNPOINTS and spawn at the enemy barracks.
        private static bool TryGetProducedUnitExitPathForExactBuildingInstanceV259LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            string buildingMd,
            out Vector2[] path,
            out string audit)
        {
            path = null;
            audit = "not_started_v259";
            if (building == null)
            {
                audit = "building=<null> v259";
                return false;
            }

            C2BuildingRuntimeInfoV247LikeOriginal info;
            if (TryGetExactRuntimeInfoForSelectableBuildingV259LikeOriginal(building, buildingMd, out info, out audit) &&
                info != null)
            {
                // V377: the building supplies its original physical MD exit route.
                List<Vector2> productionExitPathV309 = C2BuildingRuntimeInfoV247LikeOriginal.C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);
                if (productionExitPathV309 == null || productionExitPathV309.Count == 0)
                    return false;

                path = productionExitPathV309.ToArray();
                audit = "v377_original_md_BORN_exit" +
                        " path=" + path.Length.ToString(CultureInfo.InvariantCulture) +
                        " record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " md='" + (info.MdName ?? string.Empty) + "'" +
                        " selectableRecord=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " selectable='" + (building.SourceMonsterId ?? string.Empty) + "'" +
                        " selectableMd='" + (building.KindName ?? string.Empty) + "'" +
                        " corner=(" + info.CornerCellX.ToString(CultureInfo.InvariantCulture) + "," +
                        info.CornerCellY.ToString(CultureInfo.InvariantCulture) + ")" +
                        " bornV283=" + (info.BornExitPathReal != null ? info.BornExitPathReal.Count : 0).ToString(CultureInfo.InvariantCulture) +
                        " bornRawAuditV291=" + (info.BornExitPathRawAuditV291 != null ? info.BornExitPathRawAuditV291.Count : 0).ToString(CultureInfo.InvariantCulture) +
                        " bornVisualV305=" + (info.BornExitPathVisualRealV305 != null ? info.BornExitPathVisualRealV305.Count : 0).ToString(CultureInfo.InvariantCulture) +
                        " concV283_entry_available=" + (info.ConcentratorPathReal != null ? info.ConcentratorPathReal.Count : 0).ToString(CultureInfo.InvariantCulture) +
                        " rule=original_corner_cell_plus_md_BORN_exit";
                return true;
            }

            return false;
        }

        private static bool TryGetExactRuntimeInfoForSelectableBuildingV259LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            string buildingMd,
            out C2BuildingRuntimeInfoV247LikeOriginal best,
            out string audit)
        {
            best = null;
            audit = "not_found_v259";
            if (building == null)
            {
                audit = "building=<null> v259";
                return false;
            }

            string selectableMember = (building.SourceMonsterId ?? string.Empty).Trim();
            string selectableMd = (building.KindName ?? string.Empty).Trim();
            string wantedMd = (buildingMd ?? string.Empty).Trim();
            int selectableNation = building.Nation;

            // 1) Fast path: component on this exact object/children/parent.
            C2BuildingRuntimeInfoV247LikeOriginal direct = building.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (IsRuntimeInfoCompatibleWithSelectableV259LikeOriginal(direct, building, selectableMember, selectableMd, wantedMd, selectableNation, true))
            {
                best = direct;
                audit = "direct_runtime_info_v259";
                return true;
            }

            C2BuildingRuntimeInfoV247LikeOriginal child = building.GetComponentInChildren<C2BuildingRuntimeInfoV247LikeOriginal>(true);
            if (IsRuntimeInfoCompatibleWithSelectableV259LikeOriginal(child, building, selectableMember, selectableMd, wantedMd, selectableNation, true))
            {
                best = child;
                audit = "child_runtime_info_v259";
                return true;
            }

            C2BuildingRuntimeInfoV247LikeOriginal parent = building.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (IsRuntimeInfoCompatibleWithSelectableV259LikeOriginal(parent, building, selectableMember, selectableMd, wantedMd, selectableNation, true))
            {
                best = parent;
                audit = "parent_runtime_info_v259";
                return true;
            }

            // 2) Slow path V303: exact record only.  Never choose a runtime info by distance.
            // Identical barracks can have several stale/preview/stage runtime children at nearby
            // RealX/RealY; distance matching makes units exit from the wrong visual instance.
            C2BuildingRuntimeInfoV247LikeOriginal[] infos = UnityEngine.Object.FindObjectsOfType<C2BuildingRuntimeInfoV247LikeOriginal>();
            int bestScore = int.MaxValue;
            string reject = string.Empty;

            for (int i = 0; infos != null && i < infos.Length; i++)
            {
                C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];

                if (info == null || info.RecordIndex != building.RecordIndex)
                {
                    if (info != null && reject.Length < 256)
                        reject += " reject#" + i.ToString(CultureInfo.InvariantCulture) +
                                  " rec=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                                  " md='" + (info.MdName ?? string.Empty) + "'" +
                                  " src='" + (info.SourceMonsterId ?? string.Empty) + "'" +
                                  " real=(" + info.RealX.ToString(CultureInfo.InvariantCulture) + "," + info.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                                  " reason=not_same_record_v303";
                    continue;
                }

                if (!IsRuntimeInfoCompatibleWithSelectableV259LikeOriginal(info, building, selectableMember, selectableMd, wantedMd, selectableNation, false))
                {
                    if (reject.Length < 256)
                        reject += " reject#" + i.ToString(CultureInfo.InvariantCulture) +
                                  " rec=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                                  " md='" + (info.MdName ?? string.Empty) + "'" +
                                  " src='" + (info.SourceMonsterId ?? string.Empty) + "'" +
                                  " real=(" + info.RealX.ToString(CultureInfo.InvariantCulture) + "," + info.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                                  " reason=incompatible_v303";
                    continue;
                }

                int dist = Mathf.Abs(info.RealX - building.RealX) + Mathf.Abs(info.RealY - building.RealY);
                int score = dist;
                if (info.RecordIndex == building.RecordIndex) score = -1000000 + dist;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = info;
                }
            }

            if (best != null)
            {
                audit = "scan_exact_record_runtime_info_v303 score=" + bestScore.ToString(CultureInfo.InvariantCulture) +
                        " record=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " md='" + (best.MdName ?? string.Empty) + "'";
                return true;
            }

            audit = "no_exact_runtime_info_v259 selectableRecord=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                    " selectable='" + selectableMember + "' selectableMd='" + selectableMd + "'" +
                    " wantedMd='" + wantedMd + "' real=(" + building.RealX.ToString(CultureInfo.InvariantCulture) + "," +
                    building.RealY.ToString(CultureInfo.InvariantCulture) + ")" + reject;
            return false;
        }

        private static bool IsRuntimeInfoCompatibleWithSelectableV259LikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            C2SettlementBuildingSelectableV1LikeOriginal building,
            string selectableMember,
            string selectableMd,
            string wantedMd,
            int selectableNation,
            bool sameObjectGraph)
        {
            if (info == null || building == null || !info.isActiveAndEnabled)
                return false;
            if (info.BornExitPathReal == null || info.BornExitPathReal.Count == 0)
                return false;

            bool memberMatch = NonEmptyEqualsV259LikeOriginal(info.SourceMonsterId, selectableMember);
            bool mdMatch = NonEmptyEqualsV259LikeOriginal(info.MdName, selectableMd) ||
                           NonEmptyEqualsV259LikeOriginal(info.MdName, wantedMd) ||
                           NonEmptyEqualsV259LikeOriginal(info.SourceMonsterId, wantedMd);
            bool nameOk = memberMatch || mdMatch;
            if (!nameOk)
                return false;

            if (info.Nation != selectableNation)
                return false;

            int dist = Mathf.Abs(info.RealX - building.RealX) + Mathf.Abs(info.RealY - building.RealY);
            if (info.RecordIndex == building.RecordIndex)
                return true;

            if (sameObjectGraph && dist <= 8192)
                return true;

            // V303: no non-graph distance match.  Distance-only matching is the source of
            // identical-barracks route offset: it can bind production to a stale preview/stage
            // runtime info with the same MD but a different transform.
            return false;
        }

        private static bool NonEmptyEqualsV259LikeOriginal(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private bool TrySpawnProducedUnitInstanceLikeOriginal(
            C2BattleTerrainMode battle,
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            int nation,
            out C2NeutralPeasantUnitInfoV2LikeOriginal spawned,
            out string audit,
            int? directRealX = null,
            int? directRealY = null)
        {
            using var profileScope = SpawnMarkerLikeOriginal.Auto();
            using var profileScopeCost = C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Spawn);
            bool profile = ProfileUnitRuntimePhasesLikeOriginal;
            long started = profile ? global::System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            try
            {
                return TrySpawnProducedUnitCoreLikeOriginal(battle, building, item, nation,
                    out spawned, out audit, directRealX, directRealY);
            }
            finally
            {
                if (profile)
                {
                    long elapsed = global::System.Diagnostics.Stopwatch.GetTimestamp() - started;
                    _profileSpawnTicksLikeOriginal += elapsed;
                    _profileSpawnMaxTicksLikeOriginal = Math.Max(_profileSpawnMaxTicksLikeOriginal, elapsed);
                    _profileSpawnCallsLikeOriginal++;
                }
            }
        }

        private bool TrySpawnProducedUnitCoreLikeOriginal(
            C2BattleTerrainMode battle,
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            int nation,
            out C2NeutralPeasantUnitInfoV2LikeOriginal spawned,
            out string audit,
            int? directRealX = null,
            int? directRealY = null)
        {
            spawned = null;
            audit = string.Empty;
            // Map Group[] must be loaded before simulation producers create units.
            // Otherwise InitializeAllUnits releases the first newborns while DIP
            // still retains their identities and waits forever for their exit.
            if (!_started || !_initialized || _busy)
            { audit = "unit_map_initialization_pending"; return false; }
            if (item == null) { audit = "item=<null>"; return false; }
            if (battle == null) battle = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (battle == null) { audit = "battle=<null>"; return false; }

            _battle = battle;
            string dataRoot;
            string mapRel;
            string mapAbs;
            if (TryResolveBattleContextLikeOriginal(battle, out dataRoot, out mapRel, out mapAbs))
            {
                _dataRoot = dataRoot;
                _mapRelativePath = mapRel;
                _mapAbsPath = mapAbs;
            }
            else if (string.IsNullOrWhiteSpace(_dataRoot))
            {
                string guess = @"C:\GSC Game World\Cossacks II\Data";
                if (Directory.Exists(guess)) _dataRoot = guess;
            }

            string unitId = item.UnitId ?? string.Empty;
            string mdName = !string.IsNullOrWhiteSpace(item.MdName) ? item.MdName : unitId;
            if (string.IsNullOrWhiteSpace(mdName)) { audit = "md=<empty> unit='" + unitId + "'"; return false; }

            UnitProbe probe = new UnitProbe();
            probe.MonsterId = !string.IsNullOrWhiteSpace(unitId) ? unitId : mdName;
            probe.MdName = mdName;
            probe.MdPath = string.Empty;
            probe.DefaultPackage = string.Empty;
            probe.Nation = nation;
            probe.RealX = directRealX.HasValue ? directRealX.Value : (building != null ? building.RealX : 0);
            probe.RealY = directRealY.HasValue ? directRealY.Value : (building != null ? building.RealY : 0);
            probe.RealDir = building != null ? building.RealDir & 255 : 0;
            probe.Index = 1000000 + _units.Count;

            int spawnRealX = probe.RealX;
            int spawnRealY = probe.RealY;
            Vector2[] bornExitPathLikeOriginal = null;
            Vector2 bornRawFinalRealLikeOriginal = Vector2.zero;
            Vector2 bornClearancePointRealLikeOriginal = Vector2.zero;
            bool hasBornRawFinalRealLikeOriginal = false;
            bool hasBornClearancePointRealLikeOriginal = false;
            bool hasBornExitPathLikeOriginal = false;
            string bornExitAuditLikeOriginal = "";
            if (building != null)
            {
                Vector2[] bornPath;
                string bornAudit;
                string buildingMdForBornV46 = !string.IsNullOrWhiteSpace(building.KindName) ? building.KindName : building.SourceMonsterId;
                if (TryGetProducedUnitExitPathForExactBuildingInstanceV259LikeOriginal(
                        building,
                        buildingMdForBornV46,
                        out bornPath,
                        out bornAudit) &&
                    bornPath != null && bornPath.Length > 0)
                {
                    // Original Build.cpp:
                    //   PTX=NM->BornPtX, PTY=NM->BornPtY
                    //   XC=((cornerX<<4)+PTX[0])<<4
                    //   CreateNewMonsterAt(XC,YC)
                    //   for j=1..nc-1 NewMonsterPreciseSendTo(((cornerX<<4)+PTX[j])<<4,...)
                    //
                    // FIX5 ORIGINAL BORNPOINTS:
                    // use exact BORNPOINTS[0..N] only, same as Build.cpp.
                    // The ordered exit path is marked precise/unlimited so it can leave LOCKPOINTS.
                    string bornClearanceAuditLikeOriginal;
                    Vector2[] adjustedBornPathLikeOriginal = C2UnitOriginalRuntimeUseRawBornExitPathLikeOriginal(
                        bornPath,
                        out bornClearanceAuditLikeOriginal);

                    hasBornRawFinalRealLikeOriginal = bornPath != null && bornPath.Length > 0;
                    if (hasBornRawFinalRealLikeOriginal)
                        bornRawFinalRealLikeOriginal = bornPath[bornPath.Length - 1];
                    hasBornClearancePointRealLikeOriginal = adjustedBornPathLikeOriginal != null && bornPath != null && adjustedBornPathLikeOriginal.Length > bornPath.Length;
                    if (hasBornClearancePointRealLikeOriginal)
                        bornClearancePointRealLikeOriginal = adjustedBornPathLikeOriginal[adjustedBornPathLikeOriginal.Length - 1];

                    spawnRealX = Mathf.RoundToInt(adjustedBornPathLikeOriginal[0].x);
                    spawnRealY = Mathf.RoundToInt(adjustedBornPathLikeOriginal[0].y);

                    // COSSACKS2/Build.cpp::ProduceObjLink:
                    //   CreateNewMonsterAt(..., nc>1 ? GetDir(PTX[1]-PTX[0], PTY[1]-PTY[0]) : -1);
                    //   OB->RealDir = GetDir(PTX[1]-PTX[0], PTY[1]-PTY[0]);
                    //   OB->GraphDir = OB->RealDir;
                    // The old Unity port created the first visible frame with building.RealDir
                    // and corrected it only when the first precise movement tick started.
                    // That produced the visible sideways-spawn -> snap-turn regression.
                    if (bornPath.Length > 1)
                    {
                        probe.RealDir = C2OriginalMovementMathV352.GetDir(
                            Mathf.RoundToInt(bornPath[1].x - bornPath[0].x),
                            Mathf.RoundToInt(bornPath[1].y - bornPath[0].y));
                    }

                    bornExitPathLikeOriginal = adjustedBornPathLikeOriginal;
                    // FIX19:
                    // Pure original production exit: raw BORNPOINTS[0..N] only.
                    // No StretchBornExitSegments and no clearancePoint.
                    // The animated exit marker is only the rally/DstX-DstY target after exit.
                    hasBornExitPathLikeOriginal = adjustedBornPathLikeOriginal.Length > 1;
                    bornExitAuditLikeOriginal = (bornAudit ?? "") + " " + (bornClearanceAuditLikeOriginal ?? "");
                    EmitBornExitAuditLikeOriginal(null, "spawn_select", "building=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " buildingMd='" + (building.SourceMonsterId ?? string.Empty) + "' unit='" + unitId + "' rawBornCount=" + bornPath.Length.ToString(CultureInfo.InvariantCulture) +
                        " chosenPathCount=" + adjustedBornPathLikeOriginal.Length.ToString(CultureInfo.InvariantCulture) +
                        " spawnReal=(" + spawnRealX.ToString(CultureInfo.InvariantCulture) + "," + spawnRealY.ToString(CultureInfo.InvariantCulture) + ") rawPath=" + FormatRealPathLikeOriginal(bornPath) +
                        " adjustedPath=" + FormatRealPathLikeOriginal(adjustedBornPathLikeOriginal) +
                        " " + bornExitAuditLikeOriginal);
                }
                else
                {
                    // FIX5 ORIGINAL BORNPOINTS:
                    // Build.cpp: if PTX == NULL -> OBJ->DeleteLastOrder(); return;
                    // Do not invent nearest-free/center+384 spawn points because that creates wrong exits.
                    audit = "spawn_cancelled_no_exact_BORNPOINTS building=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                            " unit='" + unitId + "' " + (bornAudit ?? string.Empty);
                    EmitBornExitAuditLikeOriginal(null, "spawn_cancelled", audit);
                    return false;
                }
            }
            probe.RealX = spawnRealX;
            probe.RealY = spawnRealY;

            string mdPath = ResolveMdFilePathLikeOriginal(probe);
            if (string.IsNullOrWhiteSpace(mdPath) || !File.Exists(mdPath))
            {
                audit = "md_missing unit='" + unitId + "' md='" + mdName + "' path='" + (mdPath ?? "") + "'";
                return false;
            }

            MdModel md;
            string mdAudit;
            if (!_mdCache.TryGetValue(mdPath, out md))
            {
                if (!TryParseMdLikeOriginal(mdPath, out md, out mdAudit))
                {
                    audit = "md_parse_failed unit='" + unitId + "' md='" + mdName + "' path='" + mdPath + "' " + (mdAudit ?? "");
                    return false;
                }
                _mdCache[mdPath] = md;
                _mdAuditCache[mdPath] = mdAudit;
            }
            else
            {
                mdAudit = _mdAuditCache.ContainsKey(mdPath) ? _mdAuditCache[mdPath] : "cache_hit";
            }

            int idleAnim = ResolveAnimationIndexLikeOriginal(md, StandAnimationName);
            if (idleAnim < 0) idleAnim = ResolveAnimationIndexLikeOriginal(md, RestAnimationName);
            if (!HasRuntimeVisualV437(md, mdPath, idleAnim))
            {
                audit = "idle_anim_missing unit='" + unitId + "' md='" + mdName + "'";
                return false;
            }

            if (_runtimeRoot == null)
            {
                _runtimeRoot = new GameObject("C2_UnitOriginalRuntimeV3_Units");
                _runtimeRoot.transform.SetParent(transform, false);
            }

            Vector3 world = battle.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(spawnRealX >> 4, spawnRealY >> 4);
            int order = _units.Count;
            C2UnitOriginalRuntime u = CreateRuntimeUnitLikeOriginal(probe, md, mdPath, world, idleAnim, order);
            if (u == null) { audit = "runtime_create_failed unit='" + unitId + "' md='" + mdName + "'"; return false; }
            _units.Add(u);
            _originalSetInCellTime256LikeOriginal = 0;
            if (UseOriginalDrawUnitsCellVisibilityLikeOriginal)
                RegisterOriginalDrawUnitInCellLikeOriginal(u);
            _initialized = true;

            EnsureCompatibilityUnitInfoLikeOriginal(u);
            spawned = u.Info;
            if (spawned != null)
            {
                spawned.OwnerMode = battle;
                spawned.RealX = spawnRealX;
                spawned.RealY = spawnRealY;
                spawned.RealXFloat = spawnRealX;
                spawned.RealYFloat = spawnRealY;
                C2LiveUnitCellIndex.PositionChanged(spawned);
                // V277: MapPixelToWorld is map/pipeline position scale only.
                // It is not the visual sprite/frame scale. Never inherit building visual/legacy scale into unit size.
                float inheritedMapPixelToWorld = building != null ? building.MapPixelToWorld : spawned.MapPixelToWorld;
                spawned.MapPixelToWorld = inheritedMapPixelToWorld > 0.0001f ? inheritedMapPixelToWorld : 1.0f;
                if (spawned.MapPixelToWorld < 0.999f)
                    spawned.MapPixelToWorld = 1.0f;
                spawned.VisualAudit = "viewer_gp_cache_spawn packageCache=" + _viewerGpIdByPackage.Count.ToString(CultureInfo.InvariantCulture);
                spawned.FramesAudit = mdAudit ?? string.Empty;
            }

            if (u != null)
            {
                u.BornExitAuditLikeOriginal = bornExitAuditLikeOriginal ?? string.Empty;
                u.BornExitOriginalPointCountLikeOriginal = bornExitPathLikeOriginal != null ? bornExitPathLikeOriginal.Length : 0;
                u.BornExpectedFinalValidLikeOriginal = bornExitPathLikeOriginal != null && bornExitPathLikeOriginal.Length > 0;
                if (u.BornExpectedFinalValidLikeOriginal)
                    u.BornExpectedFinalRealLikeOriginal = bornExitPathLikeOriginal[bornExitPathLikeOriginal.Length - 1];
                u.BornRawFinalValidLikeOriginal = hasBornRawFinalRealLikeOriginal;
                if (u.BornRawFinalValidLikeOriginal)
                    u.BornRawFinalRealLikeOriginal = bornRawFinalRealLikeOriginal;
                u.BornClearancePointValidLikeOriginal = hasBornClearancePointRealLikeOriginal;
                if (u.BornClearancePointValidLikeOriginal)
                    u.BornClearancePointRealLikeOriginal = bornClearancePointRealLikeOriginal;
                u.NeedsGotoFinePositionLikeOriginal = building != null && UseOriginalGotoFinePositionAfterProductionLikeOriginal;
                u.GotoFinePositionAttemptsLikeOriginal = 0;
            }

            if (building != null && u != null)
            {
                var path = new List<Vector2>(8);
                if (bornExitPathLikeOriginal != null && bornExitPathLikeOriginal.Length > 1)
                {
                    for (int i = 1; i < bornExitPathLikeOriginal.Length; i++)
                        path.Add(bornExitPathLikeOriginal[i]);
                }

                int rallyRealX;
                int rallyRealY;
                bool hasRallyPointLikeOriginal = building.TryGetRallyPointRealV155LikeOriginal(out rallyRealX, out rallyRealY);
                if (hasRallyPointLikeOriginal)
                {
                    string rallyAuditLikeOriginal;
                    Vector2 rallyFinalLikeOriginal = ResolveProducedRallyDestinationRealLikeOriginal(
                        u,
                        rallyRealX,
                        rallyRealY,
                        out rallyAuditLikeOriginal);
                    path.Add(rallyFinalLikeOriginal);
                    bornExitAuditLikeOriginal = (bornExitAuditLikeOriginal ?? string.Empty) + " " + (rallyAuditLikeOriginal ?? string.Empty);
                }
                else if (hasBornExitPathLikeOriginal && bornExitPathLikeOriginal != null && bornExitPathLikeOriginal.Length > 1)
                {
                    // FIX9:
                    // Original Build.cpp does NOT stop the produced unit exactly on the last
                    // BORNPOINT when the building has no destination/rally:
                    //
                    //   if(OBJ->DstX<0&&!Immediate)
                    //       OB->NewMonsterSendTo(lastBornX + (rando()%4096)-2048,
                    //                            lastBornY + (rando()%4096)-2048,16,2+128);
                    //
                    // This extra normal order pulls the unit away from the door after
                    // ClearOrderedUnlimitedMotion.  Without it EngKaz visually stops at the
                    // last BORNPOINT and looks like it did not exit to the end.
                    Vector2 lastBorn = bornExitPathLikeOriginal[bornExitPathLikeOriginal.Length - 1];
                    float scatterX = StableUnitRandomRangeLikeOriginal(u.Probe, 5001 + u.UnitOrder, -2048.0f, 2048.0f);
                    float scatterY = StableUnitRandomRangeLikeOriginal(u.Probe, 5002 + u.UnitOrder, -2048.0f, 2048.0f);
                    path.Add(new Vector2(lastBorn.x + scatterX, lastBorn.y + scatterY));
                }
                // FIX5/FIX9: bornExitPathLikeOriginal is exact BORNPOINTS[0..N].
                // After ClearOrderedUnlimitedMotion original either follows building rally/DstX
                // or, when DstX<0, sends the unit to lastBorn +/- random 2048 real units.

                if (path.Count > 0)
                {
                    Vector2[] runtimePathLikeOriginal = path.ToArray();
                    EmitBornExitAuditLikeOriginal(u, "path_set", "building=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " buildingMd='" + (building.SourceMonsterId ?? string.Empty) + "' originalBornPoints=" + (bornExitPathLikeOriginal != null ? bornExitPathLikeOriginal.Length : 0).ToString(CultureInfo.InvariantCulture) +
                        " runtimePathCount=" + runtimePathLikeOriginal.Length.ToString(CultureInfo.InvariantCulture) + " precise=" + hasBornExitPathLikeOriginal +
                        " preciseLastIndexWillBe=" + (hasBornExitPathLikeOriginal && bornExitPathLikeOriginal != null ? Mathf.Max(0, bornExitPathLikeOriginal.Length - 2) : -1).ToString(CultureInfo.InvariantCulture) +
                        " postBornOrder=" + (hasRallyPointLikeOriginal ? "rally" : (hasBornExitPathLikeOriginal ? "original_lastBorn_random_4096" : "none")) +
                        " runtimePath=" + FormatRealPathLikeOriginal(runtimePathLikeOriginal));
                    C2OriginalOrderChainV352.SubmitBornExitV433LikeOriginal(u.Info, runtimePathLikeOriginal,
                        hasBornExitPathLikeOriginal ? bornExitPathLikeOriginal.Length - 1 : 0);
                    if (hasBornExitPathLikeOriginal && u.Info != null)
                    {
                        // V202: Gate LINESORT is only for the unit exiting THIS exact completed building.
                        // Store the source building record so nearby buildings/gates cannot catch ordinary units.
                        u.Info.C2NeutralPeasantUnitsV15SetPreciseBornSourceBuildingRecordLikeOriginal(building.RecordIndex);
                    }
                    if (hasBornExitPathLikeOriginal)
                    {
                        // path[] starts from BORNPOINTS[1], so the last precise/unlimited
                        // waypoint index is bornPath.Length-2.  Any appended rally point after
                        // that must go back through normal CheckBar/pathing.
                        u.PreciseBornPathLastWaypointIndexLikeOriginal = Mathf.Max(0, bornExitPathLikeOriginal.Length - 2);
                    }
                }
            }

            audit = "spawned_viewer_gp_cache unit='" + unitId + "' md='" + md.Name + "' real=(" +
                    spawnRealX.ToString(CultureInfo.InvariantCulture) + "," + spawnRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " world=" + world.ToString("F3") +
                    " anim='" + (idleAnim >= 0 ? md.Animations[idleAnim].Name : md.ComplexObjectIdLikeOriginal) + "'" +
                    " frames=" + (idleAnim >= 0 ? md.Animations[idleAnim].Frames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                    " mdCache=" + (_mdCache.ContainsKey(mdPath) ? "1" : "0") +
                    " exit='" + (bornExitAuditLikeOriginal ?? "") + "'" +
                    " preciseExit=" + hasBornExitPathLikeOriginal;
            return true;
        }

        private void EnsureCompatibilityUnitInfoLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal info = u.Info;
            if (info == null)
            {
                info = new C2NeutralPeasantUnitInfoV2LikeOriginal();
                // OneObject ids are stable table slots, independent of any
                // temporary visual/behaviour proxy.  Negative values cannot be
                // confused with map record ids exposed by the compatibility API.
                info.C2BindRuntimeAndRegisterV366LikeOriginal(u.Root, -(u.UnitOrder + 1));
            }
            else if (u.Root != null)
                info.C2AttachUnityProxyV366LikeOriginal(u.Root);
            C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal anim = info.SpriteAnimator;
            if (anim == null) anim = new C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal(info);

            u.Info = info;
            info.OwnerMode = _battle;
            info.SpriteAnimator = anim;
            info.SpriteMeshRenderer = u.MeshRenderer;
            info.SourceMonsterId = u.Probe != null ? (u.Probe.MonsterId ?? string.Empty) : string.Empty;
            info.ResolvedMd = u.Md != null ? (u.Md.Name ?? string.Empty) : string.Empty;
            info.RecordIndex = u.Probe != null ? u.Probe.Index : u.UnitOrder;
            info.Nation = (byte)Mathf.Clamp(u.Probe != null ? u.Probe.Nation : 0, 0, 255);
            info.RealX = u.Probe != null ? u.Probe.RealX : info.RealX;
            info.RealY = u.Probe != null ? u.Probe.RealY : info.RealY;
            info.RealDir = (byte)(u.RealDirPrecise & 255);
            info.GraphDir = info.RealDir;
            info.OctantInfo = (byte)(u.OctantInfo & 255);
            info.RealXFloat = info.RealX;
            info.RealYFloat = info.RealY;
            C2LiveUnitCellIndex.PositionChanged(info);
            info.RealDirPrecise = u.RealDirPrecise & 255;
            info.SortKey = SortingOrderBase + u.UnitOrder;
            info.FrameCount = CountTotalFrames(u.Md);
            info.MotionDist = u.OriginalMotionDistLikeOriginal > 0 ? u.OriginalMotionDistLikeOriginal : info.MotionDist;
            info.GeometryRadius2Real = u.Md != null && u.Md.GeometryRadius2 > 0 ? u.Md.GeometryRadius2 * 16 : info.GeometryRadius2Real;
            info.CanBuildLikeOriginal = u.CanBuildLikeOriginal;
            info.PioneerLikeOriginal = u.PioneerLikeOriginal;
            info.UsageLikeOriginal = u.Md != null ? (u.Md.Usage ?? string.Empty) : string.Empty;
            C2MoraleRuntimeV404LikeOriginal.EnsureSoloInitializedV404LikeOriginal(info);
            if (info.MaxLifeLikeOriginal <= 0)
            {
                C2OriginalProduceCatalogV13.C2MdIconInfoV13 combatInfo =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(info);
                info.MaxLifeLikeOriginal = Mathf.Max(1, combatInfo.LifeMax);
                info.LifeLikeOriginal = info.MaxLifeLikeOriginal;
            }
            // NewMon.cpp::CreateNewMonsterAt under GETTIRED: G->GetTired=100*1000.
            info.GetTiredLikeOriginal = 100000;
            info.TiringRemainingPercentLikeOriginal = 100.0f;

            // V396: initialize OneObject::ArmAttack/RifleAttack/GroundState/NewState
            // at unit birth/compatibility creation instead of first attack click.
            C2CombatRuntimeV334LikeOriginal.InitializeUnitCombatStateFromMdV396LikeOriginal(
                info, C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(info));
            info.ControllableByPlayer =
                C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(info.Nation);
            info.NotSelectable = false;
            if (info.MapPixelToWorld <= 0.1001f)
                info.MapPixelToWorld = 1.0f;
            info.VisualAudit = "unit_original_runtime_viewer_gp_cache";
        }

        // V427: StepUnitRuntimeLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        private bool ShouldMaterializeUnitFrameLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (!DecodeOnlyVisibleUnitFramesLikeOriginal) return true;
            if (u == null || !u.ActiveLikeOriginal) return false;

            if (UseOriginalDrawUnitsCellVisibilityLikeOriginal)
                return u.VisibleInOriginalDrawUnitsLikeOriginal;

            int currentFrame = Time.frameCount;
            if (u.LastVisibilityCheckFrameLikeOriginal >= 0 &&
                currentFrame - u.LastVisibilityCheckFrameLikeOriginal < 4)
                return u.VisibleWithinViewerMarginLikeOriginal;

            Camera cam = FindBattleCameraLikeOriginal();
            if (cam == null)
            {
                u.LastVisibilityCheckFrameLikeOriginal = currentFrame;
                u.VisibleWithinViewerMarginLikeOriginal = true;
                return true;
            }

            Vector3 screen = cam.WorldToScreenPoint(u.WorldPosition);
            float margin = Mathf.Max(0.0f, ViewerFrameVisibilityMarginPixelsLikeOriginal);
            bool visible = screen.z > 0.0f &&
                           screen.x >= -margin && screen.x <= cam.pixelWidth + margin &&
                           screen.y >= -margin && screen.y <= cam.pixelHeight + margin;
            u.LastVisibilityCheckFrameLikeOriginal = currentFrame;
            u.VisibleWithinViewerMarginLikeOriginal = visible;
            return visible;
        }

        private void RebuildOriginalDrawUnitCellsLikeOriginal()
        {
            foreach (KeyValuePair<long, List<C2UnitOriginalRuntime>> pair in _unitDrawCellsLikeOriginal)
            {
                List<C2UnitOriginalRuntime> old = pair.Value;
                if (old == null) continue;
                old.Clear();
                _unitDrawCellPoolLikeOriginal.Push(old);
            }
            _unitDrawCellsLikeOriginal.Clear();

            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                RegisterOriginalDrawUnitInCellLikeOriginal(u);
            }
        }

        private void RegisterOriginalDrawUnitInCellLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || !u.ActiveLikeOriginal) return;

            // NewMon.cpp::CreateNewMonster immediately appends a newborn unit
            // to MCount/NMSL; the periodic SetMonstersInCells pass later repairs
            // moved/dead entries for the complete Group[] array.
            const float cellReal = 128.0f * 16.0f;
            int cx = Mathf.FloorToInt(u.RuntimeRealXLikeOriginal / cellReal);
            int cy = Mathf.FloorToInt(u.RuntimeRealYLikeOriginal / cellReal);
            long key = UnitCollisionBucketKeyLikeOriginal(cx, cy);
            List<C2UnitOriginalRuntime> bucket;
            if (!_unitDrawCellsLikeOriginal.TryGetValue(key, out bucket) || bucket == null)
            {
                bucket = _unitDrawCellPoolLikeOriginal.Count > 0
                    ? _unitDrawCellPoolLikeOriginal.Pop()
                    : new List<C2UnitOriginalRuntime>(16);
                _unitDrawCellsLikeOriginal[key] = bucket;
            }
            bucket.Add(u);
        }

        private void BuildOriginalDrawUnitsListLikeOriginal(Camera cam)
        {
            _drawUnitsCurrentLikeOriginal.Clear();
            if (_drawUnitsEpochV379LikeOriginal == int.MaxValue)
            {
                foreach (var unit in _units) unit.DrawVisibleEpochV379LikeOriginal = 0;
                foreach (var unit in _drawUnitsPreviousLikeOriginal) unit.DrawVisibleEpochV379LikeOriginal = 0;
                _drawUnitsEpochV379LikeOriginal = 0;
            }
            _drawUnitsEpochV379LikeOriginal++;
            _drawUnitsCameraCellCandidatesV372LikeOriginal = 0;
            _drawUnitsFellBackToAllUnitsV372LikeOriginal = false;
            if (cam != null)
                GeometryUtility.CalculateFrustumPlanes(cam, _drawUnitFrustumPlanesLikeOriginal);
            _unitFrustumRadiusWorldV378LikeOriginal = _battle != null
                ? _battle.C2OriginalSphereRadiusToWorldV1LikeOriginal(
                    Mathf.Max(1.0f, OriginalDrawUnitsSphereRadiusPixelsLikeOriginal)) : 0f;

            if (cam == null || _battle == null)
            {
                _drawUnitsFellBackToAllUnitsV372LikeOriginal = true;
                for (int i = 0; i < _units.Count; i++)
                    AddOriginalDrawUnitCandidateLikeOriginal(_units[i], cam);
            }
            else
            {
                float minX;
                float minY;
                float maxX;
                float maxY;
                if (!TryGetOriginalCameraBoundsLikeOriginal(cam, out minX, out minY, out maxX, out maxY))
                {
                    _drawUnitsFellBackToAllUnitsV372LikeOriginal = true;
                    for (int i = 0; i < _units.Count; i++)
                        AddOriginalDrawUnitCandidateLikeOriginal(_units[i], cam);
                }
                else
                {
                    int cx0 = Mathf.FloorToInt(minX / 128.0f) - 1;
                    int cy0 = Mathf.FloorToInt(minY / 128.0f) - 1;
                    int cx1 = Mathf.FloorToInt(maxX / 128.0f) + 1;
                    int cy1 = Mathf.FloorToInt(maxY / 128.0f) + 1;
                    cx0 = Mathf.Max(0, cx0);
                    cy0 = Mathf.Max(0, cy0);
                    for (int cx = cx0; cx < cx1; cx++)
                    {
                        for (int cy = cy0; cy < cy1; cy++)
                        {
                            List<C2UnitOriginalRuntime> bucket;
                            if (!_unitDrawCellsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(cx, cy), out bucket) || bucket == null)
                                continue;
                            for (int i = 0; i < bucket.Count; i++)
                                AddOriginalDrawUnitCandidateLikeOriginal(bucket[i], cam);
                        }
                    }
                }
            }

            foreach (C2UnitOriginalRuntime old in _drawUnitsPreviousLikeOriginal)
            {
                if (old != null && old.DrawVisibleEpochV379LikeOriginal != _drawUnitsEpochV379LikeOriginal)
                {
                    old.VisibleInOriginalDrawUnitsLikeOriginal = false;
                    SetUnitForceRenderingOffLikeOriginal(old, true);
                }
            }
            _drawUnitsPreviousLikeOriginal.Clear();
            _drawUnitsPreviousLikeOriginal.AddRange(_drawUnitsCurrentLikeOriginal);

            BuildOriginalDrawBuildingsLikeOriginal(cam);
            if (LogOriginalDrawUnitsAuditOnceLikeOriginal && !_originalDrawUnitsAuditLoggedLikeOriginal)
            {
                _originalDrawUnitsAuditLoggedLikeOriginal = true;
                Debug.Log(LogPrefix + " DRAW_UNITS_LIKE_ORIGINAL source=MiniMap4X.cpp::DrawUnits" +
                          " totalUnits=" + _units.Count.ToString(CultureInfo.InvariantCulture) +
                          " visibleUnits=" + _drawUnitsCurrentLikeOriginal.Count.ToString(CultureInfo.InvariantCulture) +
                          " unitCells=" + _unitDrawCellsLikeOriginal.Count.ToString(CultureInfo.InvariantCulture) +
                          " buildings=" + _drawBuildingsLikeOriginal.Count.ToString(CultureInfo.InvariantCulture) +
                          " visibleBuildings=" + _drawBuildingsCurrentSetLikeOriginal.Count.ToString(CultureInfo.InvariantCulture) +
                          " unitPadding=1 buildingPadding=5 sphereRadius=300" +
                          " fog=" + UseOriginalFogOfWarLikeOriginal +
                          " fogSide=" + _fogMapSideLikeOriginal.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void RefreshOriginalDrawBuildingsLikeOriginal(bool force)
        {
            if (!force && Time.unscaledTime < _nextBuildingDrawRefreshAtLikeOriginal) return;
            _nextBuildingDrawRefreshAtLikeOriginal = Time.unscaledTime + 0.5f;

            foreach (OriginalDrawBuildingLikeOriginal old in _drawBuildingsLikeOriginal)
                SetBuildingForceRenderingOffLikeOriginal(old, false);
            _drawBuildingsLikeOriginal.Clear();
            foreach (KeyValuePair<long, List<OriginalDrawBuildingLikeOriginal>> pair in _buildingDrawCellsLikeOriginal)
            {
                List<OriginalDrawBuildingLikeOriginal> old = pair.Value;
                if (old == null) continue;
                old.Clear();
                _buildingDrawCellPoolLikeOriginal.Push(old);
            }
            _buildingDrawCellsLikeOriginal.Clear();
            _drawBuildingsCurrentSetLikeOriginal.Clear();
            _drawBuildingsPreviousSetLikeOriginal.Clear();

            C2BuildingRuntimeInfoV247LikeOriginal[] infos =
                FindObjectsOfType<C2BuildingRuntimeInfoV247LikeOriginal>();
            const float cellReal = 128.0f * 16.0f;
            for (int i = 0; i < infos.Length; i++)
            {
                C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                if (info == null || info.RuntimeConstructionVisualChildV303) continue;
                OriginalDrawBuildingLikeOriginal building = new OriginalDrawBuildingLikeOriginal();
                building.Info = info;
                building.Renderers = info.GetComponentsInChildren<Renderer>(true);
                building.DynamicRenderers = info.RuntimeConstructionSiteV303;
                FogMdInfoLikeOriginal mdFog = ReadFogMdInfoLikeOriginal(info.MdPath);
                building.VisionType = mdFog.VisionType;
                building.VisibilityRadiusOriginalPixels = mdFog.VisibilityRadiusOriginalPixels > 0
                    ? mdFog.VisibilityRadiusOriginalPixels
                    : OriginalDrawUnitsSphereRadiusPixelsLikeOriginal;
                building.DontAffectFogOfWar = mdFog.DontAffectFogOfWar;
                building.EverSeenInFog = _buildingsEverSeenInFogLikeOriginal.Contains(info.GetEntityId());
                _drawBuildingsLikeOriginal.Add(building);
                SetBuildingForceRenderingOffLikeOriginal(building, true);

                int cx = Mathf.FloorToInt(info.RealX / cellReal);
                int cy = Mathf.FloorToInt(info.RealY / cellReal);
                long key = UnitCollisionBucketKeyLikeOriginal(cx, cy);
                List<OriginalDrawBuildingLikeOriginal> bucket;
                if (!_buildingDrawCellsLikeOriginal.TryGetValue(key, out bucket) || bucket == null)
                {
                    bucket = _buildingDrawCellPoolLikeOriginal.Count > 0
                        ? _buildingDrawCellPoolLikeOriginal.Pop()
                        : new List<OriginalDrawBuildingLikeOriginal>(8);
                    _buildingDrawCellsLikeOriginal[key] = bucket;
                }
                bucket.Add(building);
            }
        }

        private void BuildOriginalDrawBuildingsLikeOriginal(Camera cam)
        {
            RefreshOriginalDrawBuildingsLikeOriginal(false);
            _drawBuildingsCurrentSetLikeOriginal.Clear();
            if (cam == null || _battle == null)
            {
                for (int i = 0; i < _drawBuildingsLikeOriginal.Count; i++)
                    AddOriginalDrawBuildingCandidateLikeOriginal(_drawBuildingsLikeOriginal[i], cam);
            }
            else
            {
                float minX;
                float minY;
                float maxX;
                float maxY;
                if (!TryGetOriginalCameraBoundsLikeOriginal(cam, out minX, out minY, out maxX, out maxY))
                {
                    for (int i = 0; i < _drawBuildingsLikeOriginal.Count; i++)
                        AddOriginalDrawBuildingCandidateLikeOriginal(_drawBuildingsLikeOriginal[i], cam);
                }
                else
                {
                    // MiniMap4X.cpp widens the already padded unit cell range by five.
                    int cx0 = Mathf.FloorToInt(minX / 128.0f) - 1 - 5;
                    int cy0 = Mathf.FloorToInt(minY / 128.0f) - 1 - 5;
                    int cx1 = Mathf.FloorToInt(maxX / 128.0f) + 1 + 5;
                    int cy1 = Mathf.FloorToInt(maxY / 128.0f) + 1 + 5;
                    cx0 = Mathf.Max(0, cx0);
                    cy0 = Mathf.Max(0, cy0);
                    for (int cx = cx0; cx < cx1; cx++)
                    {
                        for (int cy = cy0; cy < cy1; cy++)
                        {
                            List<OriginalDrawBuildingLikeOriginal> bucket;
                            if (!_buildingDrawCellsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(cx, cy), out bucket) || bucket == null)
                                continue;
                            for (int i = 0; i < bucket.Count; i++)
                                AddOriginalDrawBuildingCandidateLikeOriginal(bucket[i], cam);
                        }
                    }
                }
            }

            foreach (OriginalDrawBuildingLikeOriginal old in _drawBuildingsPreviousSetLikeOriginal)
            {
                if (old != null && !_drawBuildingsCurrentSetLikeOriginal.Contains(old))
                    SetBuildingForceRenderingOffLikeOriginal(old, true);
            }
            _drawBuildingsPreviousSetLikeOriginal.Clear();
            foreach (OriginalDrawBuildingLikeOriginal current in _drawBuildingsCurrentSetLikeOriginal)
                _drawBuildingsPreviousSetLikeOriginal.Add(current);
        }

        private void AddOriginalDrawBuildingCandidateLikeOriginal(OriginalDrawBuildingLikeOriginal building, Camera cam)
        {
            if (building == null || building.Info == null || _drawBuildingsCurrentSetLikeOriginal.Contains(building)) return;
            bool frustumVisible = cam == null || CheckOriginalBuildingSphereInFrustumLikeOriginal(building);
            if (!frustumVisible)
            {
                SetBuildingForceRenderingOffLikeOriginal(building, true);
                return;
            }
            // The local player never loses knowledge of an owned/allied object.
            // This is especially important for a construction site: the unfinished
            // building does not yet cast vision, but its owner must still see every
            // construction stage while the fog map catches up around it.
            bool fogVisible = !UseOriginalFogOfWarLikeOriginal || building.Info.Nation == 7 ||
                              AreNationsAlliedForFogLikeOriginal(building.Info.Nation) ||
                              GetOriginalBuildingVisibilityInFogLikeOriginal(building);
            if (fogVisible)
            {
                building.EverSeenInFog = true;
                _buildingsEverSeenInFogLikeOriginal.Add(building.Info.GetEntityId());
            }
            bool visible = fogVisible || building.EverSeenInFog;
            SetBuildingForceRenderingOffLikeOriginal(building, !visible);
            if (visible) _drawBuildingsCurrentSetLikeOriginal.Add(building);
        }

        private bool CheckOriginalBuildingSphereInFrustumLikeOriginal(OriginalDrawBuildingLikeOriginal building)
        {
            if (building == null || building.Info == null) return false;
            // Runtime construction sites deliberately keep their container at the
            // scene origin and place pseudo-3D parts directly in map/world space.
            // Culling by the container transform therefore hid every player-built
            // building as soon as DrawBuildings was introduced. Use the gameplay
            // RealX/RealY position, which is also what original DrawUnits uses.
            C2BattleTerrainMode mode = building.Info.OwnerMode != null
                ? building.Info.OwnerMode
                : _battle;
            Vector3 center = mode != null
                ? mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                    building.Info.RealX / 16.0f,
                    building.Info.RealY / 16.0f)
                : building.Info.transform.position;
            float originalRadius = Mathf.Max(1.0f, building.VisibilityRadiusOriginalPixels);
            float radius = _battle != null
                ? _battle.C2OriginalSphereRadiusToWorldV1LikeOriginal(originalRadius)
                : originalRadius;
            for (int i = 0; i < _drawUnitFrustumPlanesLikeOriginal.Length; i++)
            {
                if (_drawUnitFrustumPlanesLikeOriginal[i].GetDistanceToPoint(center) < -radius)
                    return false;
            }
            return true;
        }

        private bool GetOriginalBuildingVisibilityInFogLikeOriginal(OriginalDrawBuildingLikeOriginal building)
        {
            if (building == null || building.Info == null) return false;
            int px = building.Info.RealX >> 4;
            int py = building.Info.RealY >> 4;
            int z = _battle != null ? _battle.C2OriginalFogTerrainHeightV1LikeOriginal(px, py) : 0;
            return GetInterpFowLikeOriginal(px, (py >> 1) - z) >= 850;
        }

        private static void SetBuildingForceRenderingOffLikeOriginal(OriginalDrawBuildingLikeOriginal building, bool off)
        {
            if (building == null) return;
            // Runtime construction replaces/adds visual children as the stage
            // advances. Keep the culling entry attached to the current renderers,
            // not to the renderer array captured before the stage rebuild.
            if (building.DynamicRenderers && building.Info != null)
                building.Renderers = building.Info.GetComponentsInChildren<Renderer>(true);
            if (building.Renderers == null) return;
            for (int i = 0; i < building.Renderers.Length; i++)
            {
                Renderer renderer = building.Renderers[i];
                if (renderer != null) renderer.forceRenderingOff = off;
            }
        }

        private FogMdInfoLikeOriginal ReadFogMdInfoLikeOriginal(string mdPath)
        {
            FogMdInfoLikeOriginal info;
            string key = mdPath ?? string.Empty;
            if (_fogMdInfoByPathLikeOriginal.TryGetValue(key, out info)) return info;
            info = new FogMdInfoLikeOriginal { VisibilityRadiusOriginalPixels = 300 };
            if (!string.IsNullOrEmpty(mdPath) && File.Exists(mdPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(mdPath, System.Text.Encoding.Default);
                    int picDx = 0;
                    int picDy = 0;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string raw = (lines[i] ?? string.Empty).Trim();
                        if (raw.Length == 0 || raw.StartsWith("/", StringComparison.Ordinal)) continue;
                        if (raw.StartsWith("[", StringComparison.Ordinal)) break;
                        int comment = raw.IndexOf("//", StringComparison.Ordinal);
                        if (comment >= 0) raw = raw.Substring(0, comment).Trim();
                        string[] tokens = SplitTokens(raw);
                        if (tokens.Length == 0) continue;
                        if (string.Equals(tokens[0], "VISION", StringComparison.OrdinalIgnoreCase) && tokens.Length >= 2)
                        {
                            int vision;
                            if (TryParseInt(tokens[1], out vision)) info.VisionType = Mathf.Max(0, vision);
                        }
                        else if (string.Equals(tokens[0], "DONTAFFECTFOGOFWAR", StringComparison.OrdinalIgnoreCase))
                        {
                            info.DontAffectFogOfWar = true;
                        }
                        else if (string.Equals(tokens[0], "SETANMPARAM", StringComparison.OrdinalIgnoreCase) && tokens.Length >= 3)
                        {
                            TryParseInt(tokens[1], out picDx);
                            TryParseInt(tokens[2], out picDy);
                        }
                        else if (string.Equals(tokens[0], "BUILDBAR", StringComparison.OrdinalIgnoreCase) && tokens.Length >= 5)
                        {
                            int dx0;
                            int dy0;
                            int dx1;
                            int dy1;
                            if (TryParseInt(tokens[1], out dx0) && TryParseInt(tokens[2], out dy0) &&
                                TryParseInt(tokens[3], out dx1) && TryParseInt(tokens[4], out dy1))
                            {
                                int buildX0 = picDx + (dx0 << 4);
                                int buildY0 = (picDy + (dy0 << 3)) << 1;
                                int buildX1 = picDx + (dx1 << 4);
                                int buildY1 = (picDy + (dy1 << 3)) << 1;
                                info.VisibilityRadiusOriginalPixels = Mathf.Max(
                                    Mathf.Abs(buildX1 - buildX0),
                                    Mathf.Abs(buildY1 - buildY0)) * 2;
                            }
                        }
                    }
                }
                catch { }
            }
            _fogMdInfoByPathLikeOriginal[key] = info;
            return info;
        }

        private void AddOriginalDrawUnitCandidateLikeOriginal(C2UnitOriginalRuntime u, Camera cam)
        {
            if (u == null || !u.ActiveLikeOriginal || u.Md == null) return;
            if (u.DrawVisibleEpochV379LikeOriginal == _drawUnitsEpochV379LikeOriginal) return;
            _drawUnitsCameraCellCandidatesV372LikeOriginal++;

            bool frustumVisible = !u.HiddenInsideBuildingLikeOriginal &&
                                  (cam == null || CheckOriginalUnitSphereInFrustumLikeOriginal(u));
            int fogVisibility = frustumVisible ? GetOriginalObjectVisibilityValueInFogLikeOriginal(u) : 0;
            bool visible = frustumVisible && fogVisibility != 0;
            u.VisibleInOriginalDrawUnitsLikeOriginal = visible;
            u.OriginalFogVisibilityValueLikeOriginal = fogVisibility;
            SetUnitForceRenderingOffLikeOriginal(u, !visible);
            if (!visible) return;

            u.DrawVisibleEpochV379LikeOriginal = _drawUnitsEpochV379LikeOriginal;
            _drawUnitsCurrentLikeOriginal.Add(u);
        }

        // GP_Draw.cpp::RegisterVisibleGP/CheckCoorInGP consumes the render
        // registration, not Group[] for the entire map through several cameras.
        internal bool C2TryGetVisibleUnitsForPickingV375LikeOriginal(
            out IReadOnlyList<C2UnitOriginalRuntime> units, out Camera camera)
        {
            units = _drawUnitsCurrentLikeOriginal;
            camera = _visibleUnitPickCameraV375LikeOriginal;
            return _initialized && UseOriginalDrawUnitsCellVisibilityLikeOriginal && camera != null;
        }

        public void C2GetOriginalDrawUnitsAuditV372LikeOriginal(
            out int totalUnits,
            out int cameraCellCandidates,
            out int visibleUnits,
            out bool cameraBoundsValid,
            out bool fellBackToAllUnits,
            out Vector4 cameraBoundsOriginalPixels)
        {
            totalUnits = _units.Count;
            cameraCellCandidates = _drawUnitsCameraCellCandidatesV372LikeOriginal;
            visibleUnits = _drawUnitsCurrentLikeOriginal.Count;
            cameraBoundsValid = _drawOriginalBoundsValidLikeOriginal;
            fellBackToAllUnits = _drawUnitsFellBackToAllUnitsV372LikeOriginal;
            cameraBoundsOriginalPixels = new Vector4(
                _drawOriginalBoundsMinXLikeOriginal,
                _drawOriginalBoundsMinYLikeOriginal,
                _drawOriginalBoundsMaxXLikeOriginal,
                _drawOriginalBoundsMaxYLikeOriginal);
        }

        public void C2GetVisualPipelineAuditV373LikeOriginal(
            out int renderRelevant,
            out int frameApplyCalls,
            out int frameScalarHits,
            out int readyFrameCacheHits,
            out int textureCacheHits,
            out int textureUploads,
            out int batchSubmitted,
            out int activeBatches,
            out int totalBatches)
        {
            renderRelevant = _visualRenderRelevantV373LikeOriginal;
            frameApplyCalls = _visualFrameApplyCallsV373LikeOriginal;
            frameScalarHits = _visualFrameScalarHitsV373LikeOriginal;
            readyFrameCacheHits = _visualReadyFrameCacheHitsV373LikeOriginal;
            textureCacheHits = _visualTextureCacheHitsV373LikeOriginal;
            textureUploads = _visualTextureUploadsV373LikeOriginal;
            batchSubmitted = _visualBatchSubmittedV373LikeOriginal;
            activeBatches = _visualActiveBatchesV373LikeOriginal;
            totalBatches = _visualTotalBatchesV373LikeOriginal;
        }

        private bool TryGetOriginalCameraBoundsLikeOriginal(Camera cam, out float minX, out float minY, out float maxX, out float maxY)
        {
            if (_drawOriginalBoundsFrameLikeOriginal == Time.frameCount)
            {
                minX = _drawOriginalBoundsMinXLikeOriginal;
                minY = _drawOriginalBoundsMinYLikeOriginal;
                maxX = _drawOriginalBoundsMaxXLikeOriginal;
                maxY = _drawOriginalBoundsMaxYLikeOriginal;
                return _drawOriginalBoundsValidLikeOriginal;
            }

            minX = float.PositiveInfinity;
            minY = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            maxY = float.NegativeInfinity;
            if (cam == null || _battle == null) return false;

            Rect r = cam.pixelRect;
            int valid = 0;
            IncludeOriginalCameraCornerLikeOriginal(new Vector2(r.xMin + 0.5f, r.yMin + 0.5f), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            IncludeOriginalCameraCornerLikeOriginal(new Vector2(r.xMax - 0.5f, r.yMin + 0.5f), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            IncludeOriginalCameraCornerLikeOriginal(new Vector2(r.xMax - 0.5f, r.yMax - 0.5f), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            IncludeOriginalCameraCornerLikeOriginal(new Vector2(r.xMin + 0.5f, r.yMax - 0.5f), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            int groundValid = valid;
            IncludeOriginalCameraWorldPointLikeOriginal(cam.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, cam.nearClipPlane)), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            IncludeOriginalCameraWorldPointLikeOriginal(cam.ViewportToWorldPoint(new Vector3(1.0f, 0.0f, cam.nearClipPlane)), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            IncludeOriginalCameraWorldPointLikeOriginal(cam.ViewportToWorldPoint(new Vector3(1.0f, 1.0f, cam.nearClipPlane)), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            IncludeOriginalCameraWorldPointLikeOriginal(cam.ViewportToWorldPoint(new Vector3(0.0f, 1.0f, cam.nearClipPlane)), ref valid, ref minX, ref minY, ref maxX, ref maxY);
            bool result = groundValid == 4 && maxX > minX && maxY > minY;
            _drawOriginalBoundsFrameLikeOriginal = Time.frameCount;
            _drawOriginalBoundsValidLikeOriginal = result;
            _drawOriginalBoundsMinXLikeOriginal = minX;
            _drawOriginalBoundsMinYLikeOriginal = minY;
            _drawOriginalBoundsMaxXLikeOriginal = maxX;
            _drawOriginalBoundsMaxYLikeOriginal = maxY;
            return result;
        }

        private void IncludeOriginalCameraCornerLikeOriginal(
            Vector2 screen,
            ref int valid,
            ref float minX,
            ref float minY,
            ref float maxX,
            ref float maxY)
        {
            float ox;
            float oy;
            Camera cam = FindBattleCameraLikeOriginal();
            if (!_battle.C2OriginalCameraPlaneScreenToPixelV1LikeOriginal(cam, screen, out ox, out oy)) return;
            minX = Mathf.Min(minX, ox);
            minY = Mathf.Min(minY, oy);
            maxX = Mathf.Max(maxX, ox);
            maxY = Mathf.Max(maxY, oy);
            valid++;
        }

        private void IncludeOriginalCameraWorldPointLikeOriginal(
            Vector3 world,
            ref int valid,
            ref float minX,
            ref float minY,
            ref float maxX,
            ref float maxY)
        {
            float ox;
            float oy;
            if (!_battle.C2NoUnitWorldToOriginalPixelLikeOriginal(world, out ox, out oy)) return;
            minX = Mathf.Min(minX, ox);
            minY = Mathf.Min(minY, oy);
            maxX = Mathf.Max(maxX, ox);
            maxY = Mathf.Max(maxY, oy);
            valid++;
        }

        private float _unitFrustumRadiusWorldV378LikeOriginal;

        private bool CheckOriginalUnitSphereInFrustumLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || !u.ActiveLikeOriginal || _battle == null) return false;
            Vector3 center = u.WorldPosition;
            float radiusWorld = _unitFrustumRadiusWorldV378LikeOriginal;
            for (int i = 0; i < _drawUnitFrustumPlanesLikeOriginal.Length; i++)
            {
                if (_drawUnitFrustumPlanesLikeOriginal[i].GetDistanceToPoint(center) < -radiusWorld)
                    return false;
            }
            return true;
        }

        private static void SetUnitForceRenderingOffLikeOriginal(C2UnitOriginalRuntime u, bool off)
        {
            if (u == null) return;
            off = off || u.HiddenInsideBuildingLikeOriginal;
            if (u.ComplexVisualV437 != null && u.ComplexVisualV437.Root != null)
                u.ComplexVisualV437.Root.SetActive(!off && u.ActiveLikeOriginal);
            bool individualOff = off || u.RenderedByOriginalGpsBatchLikeOriginal;
            if (u.MeshRenderer != null) u.MeshRenderer.forceRenderingOff = individualOff;
            if (u.DepthMeshRenderer != null) u.DepthMeshRenderer.forceRenderingOff = individualOff;
            if (u.SelectionRingRenderer != null) u.SelectionRingRenderer.forceRenderingOff = off;
        }

        private void EnsureOriginalFogMapLikeOriginal()
        {
            if (_fogInitializedLikeOriginal && _fogMapLikeOriginal != null && _fogMapWorkLikeOriginal != null)
                return;
            int addsh;
            int vertInLine;
            int maxTH;
            if (_battle == null || !_battle.C2OriginalFogMapConfigV1LikeOriginal(out addsh, out vertInLine, out maxTH))
                addsh = 1;
            _fogAddshLikeOriginal = Mathf.Clamp(addsh, 1, 3);
            _fogMapSideLikeOriginal = 134 << (_fogAddshLikeOriginal - 1);
            _fogMapLikeOriginal = new ushort[_fogMapSideLikeOriginal * _fogMapSideLikeOriginal];
            _fogMapWorkLikeOriginal = new ushort[_fogMapLikeOriginal.Length];
            if (SystemInfo.SupportsTextureFormat(TextureFormat.R16))
            {
                _fogTextureLikeOriginal = new Texture2D(
                    _fogMapSideLikeOriginal,
                    _fogMapSideLikeOriginal,
                    TextureFormat.R16,
                    false,
                    true)
                {
                    name = "C2_FogMap_LikeOriginal",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                Shader.SetGlobalTexture("_C2FogMapLikeOriginal", _fogTextureLikeOriginal);
            }
            _fogInitializedLikeOriginal = true;
        }

        private void StepOriginalFogOfWarLikeOriginal()
        {
            if (!UseOriginalFogOfWarLikeOriginal) return;
            EnsureOriginalFogMapLikeOriginal();
            if (_fogMapLikeOriginal == null || _fogMapWorkLikeOriginal == null) return;

            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (!CanUnitLightOriginalFogLikeOriginal(u)) continue;
                int px = (int)(u.RuntimeRealXLikeOriginal / 16.0f);
                int py = (int)(u.RuntimeRealYLikeOriginal / 16.0f);
                int h = _battle != null ? _battle.C2OriginalFogTerrainHeightV1LikeOriginal(px, py) : 0;
                MarkOriginalVisionTypeLikeOriginal(px, py - (h << 1), u.Md.VisionType);
            }

            RefreshOriginalDrawBuildingsLikeOriginal(false);
            for (int i = 0; i < _drawBuildingsLikeOriginal.Count; i++)
            {
                OriginalDrawBuildingLikeOriginal building = _drawBuildingsLikeOriginal[i];
                if (!CanBuildingLightOriginalFogLikeOriginal(building)) continue;
                int px = building.Info.RealX >> 4;
                int py = building.Info.RealY >> 4;
                int h = _battle != null ? _battle.C2OriginalFogTerrainHeightV1LikeOriginal(px, py) : 0;
                MarkOriginalVisionTypeLikeOriginal(px, py - (h << 1), building.VisionType);
            }

            Array.Copy(_fogMapLikeOriginal, _fogMapWorkLikeOriginal, _fogMapLikeOriginal.Length);
            int shift = _fogAddshLikeOriginal == 1 ? 8 : 10;
            for (int y = 1; y < _fogMapSideLikeOriginal - 1; y++)
            {
                int row = y * _fogMapSideLikeOriginal;
                for (int x = 1; x < _fogMapSideLikeOriginal - 1; x++)
                {
                    int index = row + x;
                    int sum = _fogMapWorkLikeOriginal[index - _fogMapSideLikeOriginal] +
                              _fogMapWorkLikeOriginal[index + _fogMapSideLikeOriginal] +
                              _fogMapWorkLikeOriginal[index - 1] +
                              _fogMapWorkLikeOriginal[index + 1];
                    int next = (sum >> 2) - (sum >> shift);
                    next = Mathf.Clamp(next, 0, ushort.MaxValue);
                    if (_fogAddshLikeOriginal != 1 || next > 780 || next >= _fogMapLikeOriginal[index])
                        _fogMapLikeOriginal[index] = (ushort)next;
                }
            }
            UploadOriginalFogToTerrainShadersLikeOriginal();
        }

        private void UploadOriginalFogToTerrainShadersLikeOriginal()
        {
            if (_fogTextureLikeOriginal == null || _fogMapLikeOriginal == null || _battle == null)
            {
                Shader.SetGlobalFloat("_C2FogEnabledLikeOriginal", 0.0f);
                return;
            }
            Vector4 map;
            Vector4 map2;
            if (!_battle.C2OriginalFogShaderMappingV1LikeOriginal(out map, out map2))
            {
                Shader.SetGlobalFloat("_C2FogEnabledLikeOriginal", 0.0f);
                return;
            }
            _fogTextureLikeOriginal.SetPixelData(_fogMapLikeOriginal, 0);
            _fogTextureLikeOriginal.Apply(false, false);
            Shader.SetGlobalTexture("_C2FogMapLikeOriginal", _fogTextureLikeOriginal);
            Shader.SetGlobalVector("_C2FogWorldMapLikeOriginal", map);
            Shader.SetGlobalVector("_C2FogWorldMap2LikeOriginal", map2);
            Shader.SetGlobalVector("_C2FogTextureInfoLikeOriginal", new Vector4(
                _fogMapSideLikeOriginal,
                1.0f / Mathf.Max(1, _fogMapSideLikeOriginal),
                0.0f,
                0.0f));
            Shader.SetGlobalColor("_C2FogColorLikeOriginal", new Color(0.025f, 0.028f, 0.025f, 1.0f));
            Shader.SetGlobalFloat("_C2FogEnabledLikeOriginal", UseOriginalFogOfWarLikeOriginal ? 1.0f : 0.0f);
        }

        private void UpdateOriginalFogOverlayLikeOriginal(Camera cam)
        {
            bool enabled = UseOriginalFogOfWarLikeOriginal && cam != null && _battle != null;
            if (!enabled)
            {
                if (_fogOverlayRendererLikeOriginal != null)
                    _fogOverlayRendererLikeOriginal.forceRenderingOff = true;
                if (_fogOverlayCameraLikeOriginal != null)
                    _fogOverlayCameraLikeOriginal.enabled = false;
                return;
            }

            EnsureOriginalFogMapLikeOriginal();
            if (_fogMapLikeOriginal == null || !EnsureOriginalFogOverlayResourcesLikeOriginal())
            {
                if (_fogOverlayRendererLikeOriginal != null)
                    _fogOverlayRendererLikeOriginal.forceRenderingOff = true;
                if (_fogOverlayCameraLikeOriginal != null)
                    _fogOverlayCameraLikeOriginal.enabled = false;
                return;
            }

            SyncOriginalFogOverlayCameraLikeOriginal(cam);

            Rect pixelRect = cam.pixelRect;
            if (!TryOriginalFogOverlayCornerLikeOriginal(new Vector2(pixelRect.xMin + 0.5f, pixelRect.yMin + 0.5f), out _fogOverlayCornersLikeOriginal[0]) ||
                !TryOriginalFogOverlayCornerLikeOriginal(new Vector2(pixelRect.xMax - 0.5f, pixelRect.yMin + 0.5f), out _fogOverlayCornersLikeOriginal[1]) ||
                !TryOriginalFogOverlayCornerLikeOriginal(new Vector2(pixelRect.xMax - 0.5f, pixelRect.yMax - 0.5f), out _fogOverlayCornersLikeOriginal[2]) ||
                !TryOriginalFogOverlayCornerLikeOriginal(new Vector2(pixelRect.xMin + 0.5f, pixelRect.yMax - 0.5f), out _fogOverlayCornersLikeOriginal[3]))
            {
                _fogOverlayRendererLikeOriginal.forceRenderingOff = true;
                if (_fogOverlayCameraLikeOriginal != null)
                    _fogOverlayCameraLikeOriginal.enabled = false;
                return;
            }

            const int cells = 40;
            const int side = cells + 1;
            float depth = cam.nearClipPlane + Mathf.Max(0.01f, cam.nearClipPlane * 0.01f);
            float timeLikeOriginal = unchecked((uint)Environment.TickCount) / 14000.0f;
            Transform overlayTransform = _fogOverlayRootLikeOriginal.transform;
            int index = 0;
            for (int iy = 0; iy <= cells; iy++)
            {
                float fy = (float)iy / cells;
                for (int ix = 0; ix <= cells; ix++, index++)
                {
                    float fx = (float)ix / cells;
                    Vector2 original = BilinearOriginalFogCornerLikeOriginal(_fogOverlayCornersLikeOriginal, fx, fy);
                    float px = original.x / 1200.0f;
                    float py = original.y / 1200.0f;
                    _fogOverlayUvLikeOriginal[index] = new Vector2(
                        px + px + Mathf.Sin(px + timeLikeOriginal),
                        py + py + Mathf.Sin(py * 1.1f + timeLikeOriginal * 1.213f) / 1.5f);

                    int alpha = GetOriginalFogOverlayAlphaLikeOriginal((int)original.x, (int)(original.y * 0.5f));
                    int diffuse = Mathf.Min(255, 383 - alpha);
                    _fogOverlayColorsLikeOriginal[index] = new Color32(
                        (byte)diffuse,
                        (byte)diffuse,
                        (byte)diffuse,
                        (byte)alpha);
                    Vector3 world = cam.ViewportToWorldPoint(new Vector3(fx, fy, depth));
                    _fogOverlayVerticesLikeOriginal[index] = overlayTransform.InverseTransformPoint(world);
                }
            }

            _fogOverlayMeshLikeOriginal.vertices = _fogOverlayVerticesLikeOriginal;
            _fogOverlayMeshLikeOriginal.uv = _fogOverlayUvLikeOriginal;
            _fogOverlayMeshLikeOriginal.colors32 = _fogOverlayColorsLikeOriginal;
            _fogOverlayMeshLikeOriginal.RecalculateBounds();
            _fogOverlayRendererLikeOriginal.forceRenderingOff = false;
            if (_fogOverlayCameraLikeOriginal != null)
                _fogOverlayCameraLikeOriginal.enabled = true;
        }

        private void SyncOriginalFogOverlayCameraLikeOriginal(Camera baseCamera)
        {
            if (baseCamera == null) return;

            // The world camera must never see this mesh.  A separate pass is
            // required because Unity's transparent sorting can otherwise draw a
            // building or unit after the fog even with a higher render queue.
            baseCamera.cullingMask &= ~C2FogOverlayLayerLikeOriginal.Mask;
            if (_fogOverlayCameraLikeOriginal == null)
            {
                GameObject go = new GameObject("C2_FogOfWar_Camera_AfterWorld_LikeOriginal");
                go.transform.SetParent(transform, false);
                _fogOverlayCameraLikeOriginal = go.AddComponent<Camera>();
            }

            _fogOverlayCameraLikeOriginal.CopyFrom(baseCamera);
            Transform source = baseCamera.transform;
            Transform target = _fogOverlayCameraLikeOriginal.transform;
            target.position = source.position;
            target.rotation = source.rotation;
            target.localScale = Vector3.one;
            _fogOverlayCameraLikeOriginal.cullingMask = C2FogOverlayLayerLikeOriginal.Mask;
            _fogOverlayCameraLikeOriginal.clearFlags = CameraClearFlags.Depth;
            _fogOverlayCameraLikeOriginal.depth = baseCamera.depth + 0.50f;
            _fogOverlayCameraLikeOriginal.useOcclusionCulling = false;
            _fogOverlayCameraLikeOriginal.allowHDR = false;
            _fogOverlayCameraLikeOriginal.allowMSAA = false;
            _fogOverlayCameraLikeOriginal.depthTextureMode = DepthTextureMode.None;
            TryBindOriginalFogOverlayCameraToUrpStackLikeOriginal(baseCamera, _fogOverlayCameraLikeOriginal);
            _fogOverlayCameraLikeOriginal.enabled = true;
        }

        private static void TryBindOriginalFogOverlayCameraToUrpStackLikeOriginal(
            Camera baseCamera,
            Camera overlayCamera)
        {
            if (baseCamera == null || overlayCamera == null) return;

            try
            {
                Type urpDataType = Type.GetType(
                    "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpDataType == null) return;

                Component baseData = baseCamera.GetComponent(urpDataType);
                if (baseData == null) baseData = baseCamera.gameObject.AddComponent(urpDataType);
                Component overlayData = overlayCamera.GetComponent(urpDataType);
                if (overlayData == null) overlayData = overlayCamera.gameObject.AddComponent(urpDataType);

                System.Reflection.PropertyInfo renderTypeProp = urpDataType.GetProperty("renderType");
                if (renderTypeProp != null && renderTypeProp.CanWrite)
                {
                    Type enumType = renderTypeProp.PropertyType;
                    renderTypeProp.SetValue(baseData, Enum.Parse(enumType, "Base"), null);
                    renderTypeProp.SetValue(overlayData, Enum.Parse(enumType, "Overlay"), null);
                }

                // The overlay only blends the fog texture. Clearing color here is what
                // turned the revealed part of the URP frame black.
                System.Reflection.PropertyInfo clearDepthProp = urpDataType.GetProperty("clearDepth");
                if (clearDepthProp != null && clearDepthProp.CanWrite)
                    clearDepthProp.SetValue(overlayData, false, null);

                System.Reflection.PropertyInfo stackProp = urpDataType.GetProperty("cameraStack");
                System.Collections.IList stack = stackProp != null
                    ? stackProp.GetValue(baseData, null) as System.Collections.IList
                    : null;
                if (stack == null) return;

                for (int i = stack.Count - 1; i >= 0; i--)
                    if (object.ReferenceEquals(stack[i], overlayCamera))
                        stack.RemoveAt(i);

                // Original order: terrain -> sprite-depth buildings/units -> fog -> HUD.
                int insertAt = stack.Count;
                for (int i = 0; i < stack.Count; i++)
                {
                    Camera stacked = stack[i] as Camera;
                    string n = stacked != null ? (stacked.name ?? string.Empty) : string.Empty;
                    if (n.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("GameplayOverlay", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        insertAt = i;
                        break;
                    }
                }
                stack.Insert(insertAt, overlayCamera);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + " FOG_URP_STACK_WARN " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private bool EnsureOriginalFogOverlayResourcesLikeOriginal()
        {
            if (_fogOverlayRendererLikeOriginal != null && _fogOverlayMeshLikeOriginal != null &&
                _fogOverlayMaterialLikeOriginal != null && _fogFractalMapLikeOriginal != null)
                return true;

            Shader overlayShader = Shader.Find("Cossacks2/C2FogOfWarOverlayLikeOriginal");
            if (overlayShader == null)
            {
                LogOriginalFogOverlayLoadFailureOnceLikeOriginal("shader_missing");
                return false;
            }
            if (string.IsNullOrWhiteSpace(_dataRoot) || !Directory.Exists(_dataRoot))
            {
                LogOriginalFogOverlayLoadFailureOnceLikeOriginal("data_root_missing path='" + (_dataRoot ?? string.Empty) + "'");
                return false;
            }

            string resolvedTexture;
            Texture2D fogTexture = C2OriginalTextureService.TryLoadTexture(
                new Cossacks2Bridge.Core.CoreFileSystem(_dataRoot),
                @"textures\FogOfWar.tga",
                "C2_FogOfWar_tga_LikeOriginal",
                C2OriginalTexturePolicy.WorldTextureLikeOriginal,
                out resolvedTexture);
            if (fogTexture == null)
            {
                LogOriginalFogOverlayLoadFailureOnceLikeOriginal("texture_missing request='textures\\FogOfWar.tga'");
                return false;
            }
            fogTexture.wrapMode = TextureWrapMode.Repeat;
            fogTexture.filterMode = FilterMode.Bilinear;

            if (!TryBuildOriginalFogFractalLikeOriginal()) return false;

            const int cells = 40;
            const int side = cells + 1;
            _fogOverlayVerticesLikeOriginal = new Vector3[side * side];
            _fogOverlayUvLikeOriginal = new Vector2[side * side];
            _fogOverlayColorsLikeOriginal = new Color32[side * side];
            int[] triangles = new int[cells * cells * 6];
            int ti = 0;
            for (int y = 0; y < cells; y++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int v0 = y * side + x;
                    int v1 = v0 + 1;
                    int v2 = v0 + side;
                    int v3 = v2 + 1;
                    triangles[ti++] = v0;
                    triangles[ti++] = v1;
                    triangles[ti++] = v2;
                    triangles[ti++] = v1;
                    triangles[ti++] = v3;
                    triangles[ti++] = v2;
                }
            }

            _fogOverlayRootLikeOriginal = new GameObject("C2_FogOfWar_40x40_LikeOriginal");
            _fogOverlayRootLikeOriginal.transform.SetParent(transform, false);
            C2FogOverlayLayerLikeOriginal.SetLayerRecursive(_fogOverlayRootLikeOriginal);
            MeshFilter filter = _fogOverlayRootLikeOriginal.AddComponent<MeshFilter>();
            _fogOverlayRendererLikeOriginal = _fogOverlayRootLikeOriginal.AddComponent<MeshRenderer>();
            _fogOverlayRendererLikeOriginal.shadowCastingMode = ShadowCastingMode.Off;
            _fogOverlayRendererLikeOriginal.receiveShadows = false;

            _fogOverlayMeshLikeOriginal = new Mesh { name = "C2_FogOfWar_40x40_Mesh_LikeOriginal" };
            _fogOverlayMeshLikeOriginal.MarkDynamic();
            _fogOverlayMeshLikeOriginal.vertices = _fogOverlayVerticesLikeOriginal;
            _fogOverlayMeshLikeOriginal.uv = _fogOverlayUvLikeOriginal;
            _fogOverlayMeshLikeOriginal.colors32 = _fogOverlayColorsLikeOriginal;
            _fogOverlayMeshLikeOriginal.triangles = triangles;
            filter.sharedMesh = _fogOverlayMeshLikeOriginal;

            _fogOverlayMaterialLikeOriginal = new Material(overlayShader)
            {
                name = "C2_FogOfWar_Overlay_Material_LikeOriginal",
                renderQueue = 3990
            };
            _fogOverlayMaterialLikeOriginal.SetTexture("_MainTex", fogTexture);
            _fogOverlayRendererLikeOriginal.sharedMaterial = _fogOverlayMaterialLikeOriginal;
            return true;
        }

        private bool TryOriginalFogOverlayCornerLikeOriginal(Vector2 screen, out Vector2 original)
        {
            original = Vector2.zero;
            float x;
            float y;
            Camera cam = FindBattleCameraLikeOriginal();
            if (_battle == null || !_battle.C2OriginalCameraPlaneScreenToPixelV1LikeOriginal(cam, screen, out x, out y))
                return false;
            original = new Vector2(x, y);
            return true;
        }

        private static Vector2 BilinearOriginalFogCornerLikeOriginal(Vector2[] corners, float fx, float fy)
        {
            return corners[0] + (corners[1] - corners[0]) * fx + (corners[3] - corners[0]) * fy +
                   (corners[2] + corners[0] - corners[1] - corners[3]) * (fx * fy);
        }

        private int GetOriginalFogOverlayAlphaLikeOriginal(int x, int y)
        {
            int scale = _battle != null ? _battle.C2OriginalFogViewScaleV1LikeOriginal() : 1;
            int fog = GetInterpFow2LikeOriginal(x, y);
            int value = 255 - (fog - 700) / scale;
            value = value * (GetOriginalFogFractalValueLikeOriginal(x * 4 + y, y * 4 + x) + 256) / 1024;
            return Mathf.Clamp(value, 0, 255);
        }

        private int GetInterpFow2LikeOriginal(int x, int y)
        {
            if (_fogMapLikeOriginal == null || _fogMapSideLikeOriginal <= 0) return 100;
            int x0 = Mathf.Max(0, x);
            int y0 = Mathf.Max(0, y);
            int fx = x0 >> 7;
            int fy = y0 >> 6;
            int max = _fogMapSideLikeOriginal - 4;
            if (fx >= max || fy >= max) return 100;
            int dx = x0 & 127;
            int dy = y0 & 63;
            int ofs = (fy + 3) * _fogMapSideLikeOriginal + fx + 3;
            int f1 = _fogMapLikeOriginal[ofs];
            int f2 = _fogMapLikeOriginal[ofs + 1];
            int f3 = _fogMapLikeOriginal[ofs + _fogMapSideLikeOriginal];
            int f4 = _fogMapLikeOriginal[ofs + _fogMapSideLikeOriginal + 1];
            int interpolated = f1 + (((f2 - f1) * dx) >> 7) + (((f3 - f1) * dy) >> 6) +
                               (((f4 + f1 - f3 - f2) * dx * dy) >> 13);
            int boundary = 100 + Math.Min(x + 50, (y + 80) * 2) * 3;
            return Math.Min(boundary, interpolated);
        }

        private bool TryBuildOriginalFogFractalLikeOriginal()
        {
            if (_fogFractalMapLikeOriginal != null) return true;
            string randomPath = Path.Combine(_dataRoot ?? string.Empty, "random.lst");
            if (!File.Exists(randomPath))
            {
                LogOriginalFogOverlayLoadFailureOnceLikeOriginal("random_missing path='" + randomPath + "'");
                return false;
            }
            byte[] bytes;
            try { bytes = File.ReadAllBytes(randomPath); }
            catch (Exception ex)
            {
                LogOriginalFogOverlayLoadFailureOnceLikeOriginal("random_read_failed type=" + ex.GetType().Name);
                return false;
            }
            if (bytes.Length < 16384)
            {
                LogOriginalFogOverlayLoadFailureOnceLikeOriginal("random_short bytes=" + bytes.Length.ToString(CultureInfo.InvariantCulture));
                return false;
            }

            short[] random = new short[8192];
            Buffer.BlockCopy(bytes, 0, random, 0, 16384);
            short[] map = new short[1024 * 1024];
            int randomPosition = 20;
            int randomXor = 0;
            Func<int> nextRandom = () =>
            {
                randomPosition++;
                if (randomPosition > 8191) randomXor += 0x3571;
                randomPosition &= 8191;
                return (random[randomPosition] ^ randomXor) & 32767;
            };
            Func<int, int> randomAmplitude = amp => ((nextRandom() * amp) >> 14) - amp;
            Func<int, int, int> get = (x, y) => map[(x & 1023) + ((y & 1023) << 10)];
            Action<int, int, int> set = (x, y, value) =>
            {
                if (x < 0 || y < 0 || x >= 1024 || y >= 1024) return;
                map[x + (y << 10)] = (short)value;
            };

            int length = 16;
            int amplitude = 2048;
            for (int x = 0; x < 1024; x += length)
                for (int y = 0; y < 1024; y += length)
                    set(x, y, randomAmplitude(amplitude));
            while (length > 1)
            {
                amplitude = amplitude * 9 / 20;
                int half = length >> 1;
                for (int x = 0; x < 1024; x += length)
                {
                    for (int y = 0; y < 1024; y += length)
                    {
                        set(x + half, y, randomAmplitude(amplitude) + ((get(x, y) + get(x + length, y)) >> 1));
                        set(x + half, y + length, randomAmplitude(amplitude) + ((get(x, y + length) + get(x + length, y + length)) >> 1));
                        set(x, y + half, randomAmplitude(amplitude) + ((get(x, y) + get(x, y + length)) >> 1));
                        set(x + length, y + half, randomAmplitude(amplitude) + ((get(x + length, y) + get(x + length, y + length)) >> 1));
                        set(x + half, y + half, randomAmplitude(amplitude) +
                            ((get(x + half, y) + get(x + half, y + length) + get(x, y + half) + get(x + length, y + half)) >> 2));
                    }
                }
                length >>= 1;
            }

            int min = int.MaxValue;
            int max = int.MinValue;
            for (int x = 0; x < 1024; x++)
            {
                for (int y = 0; y < 1024; y++)
                {
                    int value = get(x, y);
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }
            int delta = Mathf.Max(1, max - min);
            for (int x = 0; x < 1024; x++)
                for (int y = 0; y < 1024; y++)
                    set(x, y, ((get(x, y) - min) << 9) / delta);
            for (int x = 0; x < 1024; x++)
                for (int y = 0; y < 1024; y++)
                    set(x, y, (get(x + 1, y) + get(x - 1, y) + get(x, y + 1) + get(x, y - 1) + get(x, y)) / 5);

            _fogFractalMapLikeOriginal = map;
            return true;
        }

        private int GetOriginalFogFractalValueLikeOriginal(int x, int y)
        {
            if (_fogFractalMapLikeOriginal == null) return 256;
            int x0 = (x / 32) & 1023;
            int y0 = (y / 32) & 1023;
            int x1 = (x0 + 1) & 1023;
            int y1 = (y0 + 1) & 1023;
            int dx = x % 32;
            int dy = y % 32;
            int v0 = _fogFractalMapLikeOriginal[x0 + (y0 << 10)];
            int v1 = _fogFractalMapLikeOriginal[x1 + (y0 << 10)];
            int v2 = _fogFractalMapLikeOriginal[x0 + (y1 << 10)];
            int v3 = _fogFractalMapLikeOriginal[x1 + (y1 << 10)];
            return v0 + (v1 - v0) * dx / 32 + (v2 - v0) * dy / 32 +
                   ((v3 + v0 - v1 - v2) * dx / 32) * dy / 32;
        }

        private void LogOriginalFogOverlayLoadFailureOnceLikeOriginal(string reason)
        {
            if (_fogOverlayLoadFailureLoggedLikeOriginal) return;
            _fogOverlayLoadFailureLoggedLikeOriginal = true;
            Debug.LogError(LogPrefix + " FOW_OVERLAY_LOAD_FAILED " + reason);
        }

        private bool CanUnitLightOriginalFogLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null || !u.ActiveLikeOriginal) return false;
            if (u.State == C2UnitOriginalState.Death || u.Md.DontAffectFogOfWar) return false;
            if (u.PreciseBornPathLikeOriginal) return false;
            if (u.Info != null && u.Info.NotSelectable) return false;
            int fogNation = u.Info != null && u.Info.SettlementAllegianceNationLikeOriginal >= 0
                ? u.Info.SettlementAllegianceNationLikeOriginal
                : (u.Probe != null ? u.Probe.Nation : 0);
            return AreNationsAlliedForFogLikeOriginal(fogNation);
        }

        private bool CanBuildingLightOriginalFogLikeOriginal(OriginalDrawBuildingLikeOriginal building)
        {
            if (building == null || building.Info == null || building.DontAffectFogOfWar) return false;
            if (building.Info.NotSelectable) return false;
            C2SettlementBuildingSelectableV1LikeOriginal selectable =
                building.Info.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (selectable != null && !selectable.ReadyLikeOriginal) return false;
            return AreNationsAlliedForFogLikeOriginal(building.Info.Nation);
        }

        private static bool AreNationsAlliedForFogLikeOriginal(int nation)
        {
            int controlled = Mathf.Clamp(C2EditorRuntimeStateV333LikeOriginal.ControlledNation, 0, 7);
            if (nation == controlled) return true;
            int localTeam = global::MenuActionSink.GetSingleBattlesPlayerTeam(controlled);
            return localTeam > 0 && nation >= 0 && nation < 8 &&
                   global::MenuActionSink.GetSingleBattlesPlayerTeam(nation) == localTeam;
        }

        private void MarkOriginalVisionTypeLikeOriginal(int x, int y, int visionType)
        {
            int step = 128;
            switch (visionType)
            {
                case 0: FogSpotLikeOriginal(x, y); break;
                case 1:
                    FogSpotLikeOriginal(x + step, y); FogSpotLikeOriginal(x - step, y);
                    FogSpotLikeOriginal(x, y + step); FogSpotLikeOriginal(x, y - step); break;
                case 2:
                    FogSpotLikeOriginal(x + step, y + step); FogSpotLikeOriginal(x - step, y + step);
                    FogSpotLikeOriginal(x + step, y - step); FogSpotLikeOriginal(x - step, y - step); break;
                case 3: MarkOriginalVisionEightLikeOriginal(x, y, 2, 1); break;
                case 4: MarkOriginalVisionEightLikeOriginal(x, y, 3, 2); break;
                case 5: MarkOriginalVisionEightLikeOriginal(x, y, 4, 3); break;
                case 6: MarkOriginalVisionEightLikeOriginal(x, y, 5, 4); break;
                case 7:
                    FogSpotLikeOriginal(x + 6 * step, y); FogSpotLikeOriginal(x - 6 * step, y);
                    FogSpotLikeOriginal(x, y + 6 * step); FogSpotLikeOriginal(x, y - 6 * step);
                    // Literal Cossacks II fog.cpp behavior, including its first +4 Y offset.
                    FogSpotLikeOriginal(x + 4 * step, y + 4); FogSpotLikeOriginal(x - 4 * step, y + 4 * step);
                    FogSpotLikeOriginal(x + 4 * step, y - 4 * step); FogSpotLikeOriginal(x - 4 * step, y - 4 * step); break;
                case 8: MarkOriginalVisionEightLikeOriginal(x, y, 7, 5); break;
                default:
                    MarkOriginalVisionCircleLikeOriginal(x, y, Mathf.Max(0, visionType));
                    break;
            }
        }

        private void MarkOriginalVisionEightLikeOriginal(int x, int y, int axisSteps, int diagonalSteps)
        {
            const int step = 128;
            FogSpotLikeOriginal(x + axisSteps * step, y); FogSpotLikeOriginal(x - axisSteps * step, y);
            FogSpotLikeOriginal(x, y + axisSteps * step); FogSpotLikeOriginal(x, y - axisSteps * step);
            FogSpotLikeOriginal(x + diagonalSteps * step, y + diagonalSteps * step);
            FogSpotLikeOriginal(x - diagonalSteps * step, y + diagonalSteps * step);
            FogSpotLikeOriginal(x + diagonalSteps * step, y - diagonalSteps * step);
            FogSpotLikeOriginal(x - diagonalSteps * step, y - diagonalSteps * step);
        }

        private void MarkOriginalVisionCircleLikeOriginal(int x, int y, int visionType)
        {
            int radius = Mathf.Max(0, visionType - 1) * 128;
            int n = Mathf.Max(4, 6 * radius / 250);
            FogSpotLikeOriginal(x, y);
            for (int i = 0; i < n; i++)
            {
                float a = (Mathf.PI * 2.0f * i) / n;
                FogSpotLikeOriginal(x + Mathf.RoundToInt(radius * Mathf.Cos(a)), y + Mathf.RoundToInt(radius * Mathf.Sin(a)));
            }
            if (visionType <= 20) return;
            radius >>= 1;
            n = Mathf.Max(4, 6 * radius / 250);
            FogSpotLikeOriginal(x, y);
            for (int i = 0; i < n; i++)
            {
                float a = (Mathf.PI * 2.0f * i) / n;
                FogSpotLikeOriginal(x + Mathf.RoundToInt(radius * Mathf.Cos(a)), y + Mathf.RoundToInt(radius * Mathf.Sin(a)));
            }
        }

        private void FogSpotLikeOriginal(int x, int y)
        {
            if (_fogMapLikeOriginal == null || _fogMapSideLikeOriginal <= 0) return;
            int fx = ((x + 64) >> 7) + 3;
            int fy = ((y + 64) >> 7) + 3;
            fx = Mathf.Clamp(fx, 1, _fogMapSideLikeOriginal - 1);
            fy = Mathf.Clamp(fy, 1, _fogMapSideLikeOriginal - 1);
            _fogMapLikeOriginal[fy * _fogMapSideLikeOriginal + fx] = 8000;
        }

        private int GetOriginalObjectVisibilityValueInFogLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return 0;
            int nation = u.Probe != null ? u.Probe.Nation : 0;
            if (nation == 7 || !UseOriginalFogOfWarLikeOriginal) return 255;
            EnsureOriginalFogMapLikeOriginal();
            int px = (int)(u.RuntimeRealXLikeOriginal / 16.0f);
            int py = (int)(u.RuntimeRealYLikeOriginal / 16.0f);
            int z = _battle != null ? _battle.C2OriginalFogTerrainHeightV1LikeOriginal(px, py) : 0;
            int dp = GetInterpFowLikeOriginal(px, (py >> 1) - z);
            if (dp < 850) return 0;
            if (dp > 1100) return 255;
            if (dp < 950)
                return Mathf.Clamp(((dp - 850) * 255) / (940 - 850), 0, 255);
            return 255;
        }

        private int GetInterpFowLikeOriginal(int x, int y)
        {
            if (_fogMapLikeOriginal == null || _fogMapSideLikeOriginal <= 0) return 100;
            int fx = x >> 7;
            int fy = y >> 6;
            int max = _fogMapSideLikeOriginal - 4;
            if (fx < 0 || fy < 0 || fx >= max || fy >= max) return 100;
            int dx = x & 127;
            int dy = y & 63;
            int ofs = (fy + 3) * _fogMapSideLikeOriginal + fx + 3;
            int f1 = _fogMapLikeOriginal[ofs];
            int f2 = _fogMapLikeOriginal[ofs + 1];
            int f3 = _fogMapLikeOriginal[ofs + _fogMapSideLikeOriginal];
            int f4 = _fogMapLikeOriginal[ofs + _fogMapSideLikeOriginal + 1];
            return f1 + (((f2 - f1) * dx) >> 7) + (((f3 - f1) * dy) >> 6) +
                   (((f4 + f1 - f3 - f2) * dx * dy) >> 13);
        }

        internal bool IsOriginalMapPointVisibleInFogLikeOriginal(int originalPixelX, int originalPixelY)
        {
            if (!UseOriginalFogOfWarLikeOriginal) return true;
            EnsureOriginalFogMapLikeOriginal();
            int z = _battle != null
                ? _battle.C2OriginalFogTerrainHeightV1LikeOriginal(originalPixelX, originalPixelY)
                : 0;
            return GetInterpFowLikeOriginal(originalPixelX, (originalPixelY >> 1) - z) >= 850;
        }

        // V427: ApplyTiringLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: UpdateOriginalObjectRuntimeStateLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeMoveDestinationWorldLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeMoveDestinationRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeValidatedDirectMoveDestinationRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeMovePathRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeMoveDestinationRealInternalLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeMoveTargetOnlyLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: StartRuntimeMoveTargetNowLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: AdvanceRuntimeMoveWaypointLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: C2UnitOriginalRuntimeUseRawBornExitPathLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: C2UnitOriginalRuntimeAppendBornExitClearancePointV10LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: C2UnitOriginalRuntimeBornRealPointNearLockV10LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: FormatRealPathLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: EmitBornExitAuditLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: EmitBornStopAuditLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeFacingLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: UpdateRuntimeRealFromWorldLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: EnsureRuntimeRealFromInfoLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: UpdateRuntimeWorldAndRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: UpdateRuntimeWorldAndRealContinuousLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        private void UpdateUnitSortingOrderLikeOriginal(C2UnitOriginalRuntime u, Camera cam)
        {
            if (u == null) return;
            if (u.Info != null && u.Info.C2NeutralPeasantUnitsV15HasLineSortOverrideLikeOriginal())
                return;

            int tie = ComputeUnitOriginalZBufferSameLineTieLikeOriginal(u, cam);
            int sortOrder = SortingOrderBase + u.UnitOrder;

            if (SortUnitsAgainstBuildingsByOriginalRealYLikeOriginal)
            {
                // Original ZBuffer buckets sprites by screen line and sorts entries on that line by X.
                // Keep the RealY band shared with buildings, but use screen-X as the same-line
                // tiebreaker instead of spawn order; dense infantry streams otherwise collide in
                // Unity's transparent renderer and flicker/flip.
                float ry = u.RuntimeRealYLikeOriginal;
                if (u.Info != null && Mathf.Abs(u.Info.RealYFloat) > 0.001f)
                    ry = u.Info.RealYFloat;

                int mapY = Mathf.RoundToInt(ry / 16.0f);
                sortOrder = Mathf.Clamp(SortingOrderBase + mapY + tie, -30000, 30000);
            }
            else if (SortUnitsByScreenYLikeOriginal)
            {
                bool sortedByScreen = false;

                if (cam != null)
                {
                    Vector3 anchor = u.WorldPosition;
                    Vector3 screen = cam.WorldToScreenPoint(anchor);
                    float yFromTop = (float)cam.pixelHeight - screen.y;
                    int yRank = Mathf.RoundToInt(yFromTop * UnitScreenYSortScaleLikeOriginal);
                    yRank = Mathf.Clamp(yRank, -UnitScreenYSortClampLikeOriginal, UnitScreenYSortClampLikeOriginal);
                    sortOrder = SortingOrderBase + yRank + tie;
                    sortedByScreen = true;
                }

                if (!sortedByScreen && u.Info != null)
                {
                    int yRank = Mathf.RoundToInt((u.Info.RealYFloat / 16.0f) * UnitRealYSortScaleFallbackLikeOriginal);
                    yRank = Mathf.Clamp(yRank, -UnitScreenYSortClampLikeOriginal, UnitScreenYSortClampLikeOriginal);
                    sortOrder = SortingOrderBase + yRank + tie;
                }
            }

            if (u.MeshRenderer != null && u.MeshRenderer.sortingOrder != sortOrder)
                u.MeshRenderer.sortingOrder = sortOrder;
            if (u.DepthMeshRenderer != null && u.DepthMeshRenderer.sortingOrder != sortOrder)
                u.DepthMeshRenderer.sortingOrder = sortOrder;

            if (u.Info != null)
                u.Info.SortKey = sortOrder;
        }

        private int ComputeUnitOriginalZBufferSameLineTieLikeOriginal(C2UnitOriginalRuntime u, Camera cam)
        {
            int fallback = u != null ? (u.UnitOrder & UnitOriginalZBufferSameLineTieMaxLikeOriginal) : 0;
            if (u == null)
                return fallback;

            float originalX = Mathf.Abs(u.RuntimeRealXLikeOriginal) > 0.001f
                ? u.RuntimeRealXLikeOriginal
                : (u.Info != null ? u.Info.RealXFloat : 0.0f);
            float t = Mathf.Repeat(originalX / 16.0f, UnitOriginalZBufferSameLineTieMaxLikeOriginal + 1.0f) /
                      Mathf.Max(1.0f, UnitOriginalZBufferSameLineTieMaxLikeOriginal);
            return Mathf.Clamp(Mathf.RoundToInt(t * UnitOriginalZBufferSameLineTieMaxLikeOriginal), 0, UnitOriginalZBufferSameLineTieMaxLikeOriginal);
        }

        private static float ComputeUnitOriginalZBufferDepthBiasPixelsLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null)
                return 0.0f;

            // MiniMap4X.cpp::DrawSpriteUnit adds a small camera-depth variation for crowded
            // brigade sprites. Unity uses one transparent quad per unit, so keep the same idea
            // as a deterministic local Z bias for equal sortingOrder cases.
            float xPx = u.RuntimeRealXLikeOriginal / 16.0f;
            float wave = Mathf.Sin(xPx * (Mathf.PI * 2.0f / 100.0f)) * UnitOriginalZBufferDepthBiasAmplitudePixelsLikeOriginal;
            float stable = ((u.UnitOrder & 31) - 15.5f) * 0.03125f;
            return wave + stable;
        }

        // V427: TryAdvanceRuntimeRealWithOriginalMotionFieldLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: C2BuildingMotionFieldV1BlockedForTurnLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: IsMdSingleStepPassThroughLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: AdvanceSingleStepMotionV352LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SyncMotionPhaseAfterRotateV357LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: BeginSingleStepRotUnitV352LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: AdvanceSingleStepRotUnitLinkV352LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // Returns true when Brigade.cpp::RotUnitLink has completed the appended facing order.
        // V427: AdvanceFinalRotUnitV352LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V411 / COSSACKS2/NewMon.cpp::AttackObjLink rotation gate.
        // needState is the original zero-based attack slot.  This keeps the
        // retail MotionStyle numeric mapping (2=SHEEPS, 3=COMPLEXROTATE,
        // 7=SINGLESTEP); V410 incorrectly applied the MotionStyle==2 branch to
        // SINGLESTEP infantry.
        // V427: AdvanceRuntimeAttackFacingV411LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // Compatibility entry retained for callers outside the combat runtime.
        // V427: AdvanceRuntimeAttackFacingV410LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: MotionStyleCodeV411LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ApplyRotateAtPlaceAnimationFrameV352LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // UnitAbility.cpp::BoidsSingleStep2 copied with the same coordinate units.
        // V427: ApplyOriginalBoidsSingleStep2V352LikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ApplyOriginalSingleStepBoidsSteeringLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: UpdateOriginalBoidsPairForcesLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: RebuildOriginalBoidsNeighborPairsLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: OriginalNormaLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryRetargetBlockedFinishToFreePositionLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitOccupyRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitAdvanceRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitAdvanceSegmentRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitFinishTargetRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ResolveProducedRallyDestinationRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryStartGotoFinePositionAfterProductionLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: LogGotoFinePositionLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryFindProducedRallyFreePositionRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanProducedRallyCandidateRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitAvoidOtherUnitsAndReservedTargetsLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: IsPointInsideRuntimeUnitRadiusLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryGetRuntimeUnitReservedDestinationRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SquareRingOffsetLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitOccupyTerrainLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanPreciseBornUnitAdvanceWithDoorSpacingLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitOccupyOtherUnitsLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryRequestIdleBlockerYieldAsideLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryResolveMovingUnitBlockLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: CanRuntimeUnitOccupyOtherUnitsExceptLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryFindRuntimeUnitBlockingPositionLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: IsRuntimeUnitFreeToYieldAsideLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: TryPickIdleYieldAsideTargetLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: RebuildRuntimeUnitCollisionBucketsLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ApplyRuntimeUnitSeparationForcesLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: IsRuntimeUnitCollisionCandidateLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeHiddenInsideBuildingLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ResolveRuntimeUnitCollisionRadiusRealLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: UnitCollisionBucketKeyLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: StableUnitCollisionAngleLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ResolveRuntimeUnitRadiusCellsLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: DirectionFromRealDeltaLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: GetMotionRInFrameLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: ApplyMotionFrameFromPathLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        internal int GetRuntimeWorkFrameCountLikeOriginal(C2UnitOriginalRuntime u, byte realDir)
        {
            if (!EnableWorkAnimationLikeOriginal || u == null || u.Md == null) return 0;
            int work = ResolveAnimationIndexLikeOriginal(u.Md, WorkAnimationName);
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "#WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "@WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "$WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "%WORK");
            if (work < 0 || work >= u.Md.Animations.Count) return 0;
            AnimModel anim = u.Md.Animations[work];
            return anim != null ? anim.Frames.Count : 0;
        }

        internal bool SetRuntimeWorkFramePhaseLikeOriginal(C2UnitOriginalRuntime u, byte realDir, float phase, bool force)
        {
            if (!EnableWorkAnimationLikeOriginal || u == null || u.Md == null) return false;
            if (u.State == C2UnitOriginalState.Death) return false;

            int work = ResolveAnimationIndexLikeOriginal(u.Md, WorkAnimationName);
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "#WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "@WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "$WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(u.Md, "%WORK");
            if (work < 0 || work >= u.Md.Animations.Count)
            {
                if (WorkFallbackToStandIfMissingLikeOriginal && u.State != C2UnitOriginalState.Stand)
                {
                    int stand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                    if (stand >= 0) SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, stand, true, "work_missing_fallback_stand");
                }
                return false;
            }

            AnimModel anim = u.Md.Animations[work];
            int nf = anim != null ? anim.Frames.Count : 0;
            if (nf <= 0) return false;

            int frameIndex = Mathf.FloorToInt(phase) % nf;
            if (frameIndex < 0) frameIndex += nf;

            if (WorkStopsMoveLikeOriginal)
            {
                C2OriginalOrderChainV352.StopTaskMoveV433LikeOriginal(u.Info);
                u.HasMoveTargetLikeOriginal = false;
            }

            SetRuntimeFacingLikeOriginal(u, realDir);

            bool changedState = u.State != C2UnitOriginalState.Work || u.CurrentAnimIndex != work;
            if (changedState)
                SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Work, work, true, "build_work_start");

            int newLong = Mathf.Clamp(frameIndex, 0, nf - 1) << 8;
            bool changedFrame = u.CurrentFrameLong != newLong || force || changedState || string.IsNullOrEmpty(u.LastFrameKey);
            u.CurrentFrameLong = newLong;
            u.FrameFinishedLikeOriginal = false;
            if (u.AnimState != null)
            {
                u.AnimState.CurrentFrameLong = u.CurrentFrameLong;
                u.AnimState.FrameFinished = false;
                u.AnimState.Reason = "build_work_phase";
            }

            if (changedFrame)
                ApplyUnitFrameLikeOriginal(u, "build_work_phase");

            if (LogWorkAnimationLikeOriginal && _workLogs < 48 && (changedFrame || changedState))
            {
                _workLogs++;
                Debug.Log(LogPrefix + " WORK_LIKE_ORIGINAL source=BuildObjLink" +
                          " unit='" + (u.Probe != null ? (u.Probe.MonsterId ?? string.Empty) : string.Empty) + "'" +
                          " md='" + (u.Md != null ? (u.Md.Name ?? string.Empty) : string.Empty) + "'" +
                          " anim='" + (anim != null ? (anim.Name ?? string.Empty) : string.Empty) + "'" +
                          " frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + "/" + nf.ToString(CultureInfo.InvariantCulture) +
                          " phase=" + phase.ToString("0.###", CultureInfo.InvariantCulture) +
                          " realDir=" + (realDir & 255).ToString(CultureInfo.InvariantCulture) +
                          " stopMove=" + WorkStopsMoveLikeOriginal +
                          " externalPhase=" + WorkAnimationExternalPhaseLikeOriginal);
            }
            return true;
        }

        internal bool SetRuntimeTakeResourceFramePhaseV222LikeOriginal(C2UnitOriginalRuntime u, byte resourceId, byte realDir, float phase, bool force)
        {
            if (!EnableWorkAnimationLikeOriginal || u == null || u.Md == null) return false;
            if (u.State == C2UnitOriginalState.Death) return false;

            int animIndex = ResolveTakeResourceAnimationIndexV222LikeOriginal(u.Md, resourceId);
            if (animIndex < 0 || animIndex >= u.Md.Animations.Count)
            {
                return SetRuntimeWorkFramePhaseLikeOriginal(u, realDir, phase, force);
            }

            AnimModel anim = u.Md.Animations[animIndex];
            int nf = anim != null ? anim.Frames.Count : 0;
            if (nf <= 0) return false;

            int frameIndex = Mathf.FloorToInt(phase) % nf;
            if (frameIndex < 0) frameIndex += nf;

            if (WorkStopsMoveLikeOriginal)
            {
                C2OriginalOrderChainV352.StopTaskMoveV433LikeOriginal(u.Info);
                u.HasMoveTargetLikeOriginal = false;
            }

            SetRuntimeFacingLikeOriginal(u, realDir);

            bool changedState = u.State != C2UnitOriginalState.Work || u.CurrentAnimIndex != animIndex;
            if (changedState)
                SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Work, animIndex, true, "take_resource_work_v222");

            int newLong = Mathf.Clamp(frameIndex, 0, nf - 1) << 8;
            bool changedFrame = u.CurrentFrameLong != newLong || force || changedState || string.IsNullOrEmpty(u.LastFrameKey);
            u.CurrentFrameLong = newLong;
            u.FrameFinishedLikeOriginal = false;
            if (u.AnimState != null)
            {
                u.AnimState.CurrentFrameLong = u.CurrentFrameLong;
                u.AnimState.FrameFinished = false;
                u.AnimState.Reason = "take_resource_work_v222";
            }

            if (changedFrame)
                ApplyUnitFrameLikeOriginal(u, "take_resource_work_v222");

            return true;
        }

        internal int GetRuntimeTakeResourceFrameCountV348LikeOriginal(C2UnitOriginalRuntime u, byte resourceId)
        {
            if (!EnableWorkAnimationLikeOriginal || u == null || u.Md == null) return 0;
            int animIndex = ResolveTakeResourceAnimationIndexV222LikeOriginal(u.Md, resourceId);
            if (animIndex < 0 || animIndex >= u.Md.Animations.Count) return 0;
            AnimModel anim = u.Md.Animations[animIndex];
            return anim != null ? anim.Frames.Count : 0;
        }

        internal bool SetRuntimeTakeResourceDepositFramePhaseV227LikeOriginal(C2UnitOriginalRuntime u, byte resourceId, byte realDir, float phase, bool force)
        {
            if (!EnableWorkAnimationLikeOriginal || u == null || u.Md == null) return false;
            if (u.State == C2UnitOriginalState.Death) return false;

            int animIndex = ResolveTakeResourceDepositAnimationIndexV227LikeOriginal(u.Md, resourceId);
            if (animIndex < 0 || animIndex >= u.Md.Animations.Count)
                return SetRuntimeTakeResourceFramePhaseV222LikeOriginal(u, resourceId, realDir, phase, force);

            AnimModel anim = u.Md.Animations[animIndex];
            int nf = anim != null ? anim.Frames.Count : 0;
            if (nf <= 0) return false;

            int frameIndex = Mathf.FloorToInt(phase) % nf;
            if (frameIndex < 0) frameIndex += nf;

            if (WorkStopsMoveLikeOriginal)
            {
                C2OriginalOrderChainV352.StopTaskMoveV433LikeOriginal(u.Info);
                u.HasMoveTargetLikeOriginal = false;
            }

            SetRuntimeFacingLikeOriginal(u, realDir);

            bool changedState = u.State != C2UnitOriginalState.Work || u.CurrentAnimIndex != animIndex;
            if (changedState)
                SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Work, animIndex, true, "take_resource_deposit_v227");

            int newLong = Mathf.Clamp(frameIndex, 0, nf - 1) << 8;
            bool changedFrame = u.CurrentFrameLong != newLong || force || changedState || string.IsNullOrEmpty(u.LastFrameKey);
            u.CurrentFrameLong = newLong;
            u.FrameFinishedLikeOriginal = false;
            if (u.AnimState != null)
            {
                u.AnimState.CurrentFrameLong = u.CurrentFrameLong;
                u.AnimState.FrameFinished = false;
                u.AnimState.Reason = "take_resource_deposit_v227";
            }

            if (changedFrame)
                ApplyUnitFrameLikeOriginal(u, "take_resource_deposit_v227");

            return true;
        }

        internal void SetRuntimeCarryResourceMotionV223LikeOriginal(C2UnitOriginalRuntime u, byte resourceId, bool carrying)
        {
            if (u == null) return;
            u.CarryResourceMotionV223LikeOriginal = carrying;
            u.CarryResourceIdV223LikeOriginal = carrying ? resourceId : (byte)0xFF;
        }

        internal void StopRuntimeTakeResourceWorkV222LikeOriginal(C2UnitOriginalRuntime u)
        {
            StopRuntimeWorkAnimationLikeOriginal(u);
        }

        private static int ResolveCarryResourceGoWithStageV223LikeOriginal(byte resourceId)
        {
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceWoodV1LikeOriginal) return 5;  // TAKERESSTAGES WOOD ... GoWithStage=5 -> MOTION_L4
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceFoodV1LikeOriginal) return 3;  // FOOD ... GoWithStage=3 -> MOTION_L2
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceStoneV1LikeOriginal) return 8; // STONE ... GoWithStage=8 -> MOTION_L7
            return 0;
        }

        private int ResolveRuntimeMotionAnimationIndexV223LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null) return -1;

            if (u.CarryResourceMotionV223LikeOriginal)
            {
                int goWithStage = ResolveCarryResourceGoWithStageV223LikeOriginal(u.CarryResourceIdV223LikeOriginal);
                int suffix = goWithStage - 1;
                if (suffix > 0)
                {
                    string s = suffix.ToString(CultureInfo.InvariantCulture);
                    int idx = ResolveAnimationIndexLikeOriginal(u.Md, "@MOTION_L" + s);
                    if (idx < 0) idx = ResolveAnimationIndexLikeOriginal(u.Md, "#MOTION_L" + s);
                    if (idx < 0) idx = ResolveAnimationIndexLikeOriginal(u.Md, "MOTION_L" + s);
                    if (idx >= 0) return idx;
                }
            }

            // NewMonster::GetAnimation(anm_MotionL) is a direct table lookup.
            // Preserve the public override, but use the finalised MD table for
            // the normal original #MOTION_L path used by walking units.
            int motion = string.Equals(MotionAnimationName, "#MOTION_L", StringComparison.OrdinalIgnoreCase)
                ? ResolveOriginalMotionGaitV376LikeOriginal(u.Md,
                    u.PostureWeaponTypeLikeOriginal >= 0 ? u.PostureWeaponTypeLikeOriginal + 1 : 0,
                    u.OriginalUnitSpeedLikeOriginal)
                : ResolveAnimationIndexLikeOriginal(u.Md, MotionAnimationName);
            if (motion < 0) motion = ResolveAnimationIndexLikeOriginal(u.Md, "@MOTION_L");
            if (motion < 0) motion = ResolveAnimationIndexLikeOriginal(u.Md, "#MOTION_L");
            if (motion < 0) motion = ResolveAnimationIndexLikeOriginal(u.Md, "MOTION_L");
            if (motion < 0) motion = ResolveAnimationIndexLikeOriginal(u.Md, "#MOTION");
            if (motion < 0) motion = ResolveAnimationIndexLikeOriginal(u.Md, "@MOTION");
            if (motion < 0) motion = ResolveAnimationIndexLikeOriginal(u.Md, "MOTION");
            return motion;
        }

        private static int ResolveOriginalMotionGaitV376LikeOriginal(MdModel md, int state, int unitSpeed)
        {
            if (md == null) return -1;
            int normal = md.MotionLAnimationIndexLikeOriginal;
            int first = md.PostureMotionLAnimationIndicesV376LikeOriginal[0];
            int speed = unitSpeed <= 0 ? 64 : unitSpeed;
            int rate = state > 0 && state <= md.Rate.Length ? md.Rate[state - 1] : 16;
            // C2 1.4 PerformMotion: AutoSpeedAnm > 16 selects MotionL/PMotionL
            // at SPD > 80 (decompiled engine: 776310-776337). PMotionL is MOTION_L0.
            if (md.Rate[0] > 16)
            {
                int effectiveSpeed = state > 0 ? (speed * rate) >> 4 : speed;
                return effectiveSpeed > 80 && first >= 0 ? first : normal;
            }
            if (state > 0)
            {
                int posture = state <= md.PostureMotionLAnimationIndicesV376LikeOriginal.Length
                    ? md.PostureMotionLAnimationIndicesV376LikeOriginal[state - 1] : -1;
                if (posture >= 0) return posture;
                if (first >= 0) return first;
            }
            return normal;
        }

        private static int ResolveTakeResourceDepositAnimationIndexV227LikeOriginal(MdModel md, byte resourceId)
        {
            if (md == null) return -1;

            int goWithStage = ResolveCarryResourceGoWithStageV223LikeOriginal(resourceId);
            int suffix = goWithStage - 1;

            if (suffix > 0)
            {
                string s = suffix.ToString(CultureInfo.InvariantCulture);
                string[] carryCandidates = new[]
                {
                    "@UATTACK" + s, "#UATTACK" + s, "UATTACK" + s,
                    "@PATTACK" + s, "#PATTACK" + s, "PATTACK" + s
                };

                for (int i = 0; i < carryCandidates.Length; i++)
                {
                    int idx = ResolveAnimationIndexLikeOriginal(md, carryCandidates[i]);
                    if (idx >= 0) return idx;
                }
            }

            int takeStage = ResolveTakeResourceStageV222LikeOriginal(resourceId);
            int attackIndex = takeStage - 1;
            if (attackIndex > 0)
            {
                string s = attackIndex.ToString(CultureInfo.InvariantCulture);
                string[] takeCandidates = new[]
                {
                    "@UATTACK" + s, "#UATTACK" + s, "UATTACK" + s,
                    "@PATTACK" + s, "#PATTACK" + s, "PATTACK" + s
                };

                for (int i = 0; i < takeCandidates.Length; i++)
                {
                    int idx = ResolveAnimationIndexLikeOriginal(md, takeCandidates[i]);
                    if (idx >= 0) return idx;
                }
            }

            int generic = ResolveAnimationIndexLikeOriginal(md, "@UATTACK");
            if (generic < 0) generic = ResolveAnimationIndexLikeOriginal(md, "#UATTACK");
            if (generic < 0) generic = ResolveAnimationIndexLikeOriginal(md, "@PATTACK");
            if (generic < 0) generic = ResolveAnimationIndexLikeOriginal(md, "#PATTACK");
            return generic;
        }

        private static int ResolveTakeResourceAnimationIndexV222LikeOriginal(MdModel md, byte resourceId)
        {
            int takeStage = ResolveTakeResourceStageV222LikeOriginal(resourceId);
            int attackIndex = takeStage - 1;

            string[] candidates;
            if (attackIndex <= 0)
            {
                candidates = new[] { "@ATTACK", "#ATTACK", "ATTACK" };
            }
            else
            {
                string s = attackIndex.ToString(CultureInfo.InvariantCulture);
                candidates = new[] { "@ATTACK" + s, "#ATTACK" + s, "ATTACK" + s };
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                int idx = ResolveAnimationIndexLikeOriginal(md, candidates[i]);
                if (idx >= 0) return idx;
            }

            int work = ResolveAnimationIndexLikeOriginal(md, "#WORK");
            if (work < 0) work = ResolveAnimationIndexLikeOriginal(md, "@WORK");
            return work;
        }

        private static int ResolveTakeResourceStageV222LikeOriginal(byte resourceId)
        {
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceWoodV1LikeOriginal) return 2;  // TAKERESSTAGES WOOD ... TakeResStage=2 -> ATTACK1
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceFoodV1LikeOriginal) return 4;  // FOOD ... TakeResStage=4 -> ATTACK3
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceStoneV1LikeOriginal) return 7; // STONE ... TakeResStage=7 -> ATTACK6
            return 1;
        }

        internal void StopRuntimeWorkAnimationLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null) return;
            if (u.State == C2UnitOriginalState.Death) return;
            if (u.State != C2UnitOriginalState.Work) return;
            int stand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
            if (stand < 0) stand = ResolveAnimationIndexLikeOriginal(u.Md, RestAnimationName);
            if (stand >= 0)
                SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, stand, true, "stop_build_work");
            u.LastFrameKey = string.Empty;
            ApplyUnitFrameLikeOriginal(u, "stop_build_work");
        }

        // V427: SetRuntimeMovingFlagLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        internal void SetRuntimeCombatPostureV322LikeOriginal(
            C2UnitOriginalRuntime u,
            int weaponType,
            bool active)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death) return;

            // NewMon.cpp SETATTSTATE writes NewState immediately. LocalNewState is
            // separate and is changed only by TryToStand's actual transition path.
            if (u.OriginalComplexObjectV430LikeOriginal != null) return;
            int requestedPosture = active ? Mathf.Max(0, weaponType) : -1;
            int oldDesiredV411 = u.PostureWeaponTypeLikeOriginal;
            u.PostureWeaponTypeLikeOriginal = requestedPosture;

            // Reissuing SETATTSTATE with the same NewState does not restart or
            // truncate ATTACK/PATTACK/UATTACK in retail.
            if (oldDesiredV411 == requestedPosture &&
                (u.State == C2UnitOriginalState.Attack ||
                 u.State == C2UnitOriginalState.Recharge ||
                 u.State == C2UnitOriginalState.Transition))
                return;

            if (u.HasMoveTargetLikeOriginal || u.MoveDeferredUntilNeutralStandLikeOriginal)
            {
                u.PostureAfterMoveLikeOriginal = requestedPosture;
                return;
            }

            TryToStandRuntimeV411LikeOriginal(
                u,
                false,
                active ? "weapon_posture_on_v411" : "weapon_posture_off_v411");
            ApplyUnitFrameLikeOriginal(
                u,
                active ? "weapon_posture_on_v411" : "weapon_posture_off_v411");
        }

        // BrigadeOrders.cpp::BrigadeOrder_KeepPositions::Process drives NewState/
        // GroundState through TryToStand while the brigade finishes assembling.
        // Keep that entry point on the runtime owner so the brigade adapter does not
        // reimplement posture-transition selection.
        internal void ApplyRuntimeKeepPositionsGroundStateV415LikeOriginal(
            C2UnitOriginalRuntime u,
            int state,
            string reason)
        {
            if (u == null || u.Info == null) return;
            state = Mathf.Clamp(state, 0, 16);
            if (u.Info.NewStateV396LikeOriginal != state)
            {
                u.Info.NewStateV396LikeOriginal = state;
                u.Info.GroundStateV396LikeOriginal = state;
                u.PostureWeaponTypeLikeOriginal = state > 0 ? state - 1 : -1;
                TryToStandRuntimeV411LikeOriginal(u, false, reason ?? "KeepPositions");
            }
        }

        internal bool IsRuntimeKeepPositionsPostureReadyV415LikeOriginal(
            C2UnitOriginalRuntime u,
            int state)
        {
            if (u == null || u.Md == null) return true;
            int desiredLocal = state > 0 ? state - 1 : -1;
            if (u.LocalPostureWeaponTypeV411LikeOriginal != desiredLocal) return false;
            AnimModel anim = CurrentAnim(u);
            return anim == null || u.FrameFinishedLikeOriginal || anim.CanBeBroken;
        }

        // V412 - managed port of the represented combat-posture portion of
        // COSSACKS2/NewMon.cpp::TryToStand.  The full-function inventory also
        // contains engine-side blocking/rest/Brigade/CObjIndex behavior that is
        // not represented by this adapter; do not label the whole C++ function 1=1.
        // For the represented transition path, decision order follows retail:
        // TRANSxy -> DIRECTTRANS -> TRANS01/10 -> TRANSX3/3X -> UATTACK -> neutral
        // -> PATTACK -> PSTAND -> STAND.
        private void TryToStandRuntimeV411LikeOriginal(
            C2UnitOriginalRuntime u,
            bool rest,
            string reason)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death) return;

            int desired = u.PostureWeaponTypeLikeOriginal;          // NewState-1, -1 == state 0
            int local = u.LocalPostureWeaponTypeV411LikeOriginal;   // LocalNewState-1
            if (desired >= 16) desired = -1;
            if (local >= 16) local = -1;
            u.PostureWeaponTypeLikeOriginal = desired;
            u.LocalPostureWeaponTypeV411LikeOriginal = local;

            // NewState != LocalNewState.
            if (desired != local)
            {
                if (local >= 0)
                {
                    // 1) GetAnimation(anm_Trans+n1-1+(n2-1)*10).
                    if (desired >= 0)
                    {
                        // Retail condition is literally if(n1&n2), not merely
                        // "both non-zero". n1/n2 are 1-based NewState values.
                        // This matters for 1<->2 where DIRECTTRANS must win over
                        // the later special TRANS01/TRANS10 branch.
                        int n1V411 = local + 1;
                        int n2V411 = desired + 1;
                        int trans = (n1V411 & n2V411) != 0
                            ? ResolveTransitionXYV411LikeOriginal(u.Md, local, desired)
                            : -1;
                        if (trans >= 0)
                        {
                            u.LocalPostureWeaponTypeV411LikeOriginal = desired;
                            u.PendingPostureAfterNeutralLikeOriginal = -1;
                            u.PendingStandAnimIndexLikeOriginal = ResolveStandForPostureV411LikeOriginal(u, desired);
                            u.TransitionTargetLocalPostureV411LikeOriginal = desired;
                            SelectAnimationStateLikeOriginal(
                                u, C2UnitOriginalState.Transition, trans, true,
                                (reason ?? "TryToStand") + "_TRANS" + local + desired);
                            return;
                        }

                        // 2) NewMonster::TransMask / DIRECTTRANS. No clip is played.
                        if (local < u.Md.DirectTransitionMaskLikeOriginal.Length &&
                            desired < u.Md.DirectTransitionMaskLikeOriginal.Length &&
                            (u.Md.DirectTransitionMaskLikeOriginal[local] & (1 << desired)) != 0)
                        {
                            // NewMon.cpp::TryToStand DIRECTTRANS is deliberately
                            // state-only: LocalNewState=n2; return.  It does NOT
                            // install PSTAND in this pass.  The next TryToStand pass
                            // handles the now-equal state.
                            u.LocalPostureWeaponTypeV411LikeOriginal = desired;
                            u.PendingPostureAfterNeutralLikeOriginal = -1;
                            u.PendingStandAnimIndexLikeOriginal = -1;
                            u.TransitionTargetLocalPostureV411LikeOriginal = int.MinValue;
                            return;
                        }

                        // 3) Retail special TRANS01 / TRANS10. These normally map
                        // to the same named MD clips, but retain the explicit branch
                        // because some data sets omit the generic transition table.
                        if ((local == 0 && desired == 1) || (local == 1 && desired == 0))
                        {
                            string tname = local == 0 ? "#TRANS01" : "#TRANS10";
                            int tr = ResolveAnimationIndexLikeOriginal(u.Md, tname);
                            if (tr < 0)
                                tr = ResolveAnimationIndexLikeOriginal(
                                    u.Md, local == 0 ? "@TRANS01" : "@TRANS10");
                            if (tr >= 0)
                            {
                                u.LocalPostureWeaponTypeV411LikeOriginal = desired;
                                u.PendingStandAnimIndexLikeOriginal = ResolveStandForPostureV411LikeOriginal(u, desired);
                                u.TransitionTargetLocalPostureV411LikeOriginal = desired;
                                SelectAnimationStateLikeOriginal(
                                    u, C2UnitOriginalState.Transition, tr, true,
                                    (reason ?? "TryToStand") + "_TRANS01_10");
                                return;
                            }
                        }

                        // 4) n2==4 / n1==4 branches: #TRANSX3 and #TRANS3X.
                        if (desired == 3 &&
                            (u.Md.TransXMaskLikeOriginal & (1 << local)) != 0)
                        {
                            int tr = ResolveAnimationIndexLikeOriginal(u.Md, "#TRANSX3");
                            if (tr < 0) tr = ResolveAnimationIndexLikeOriginal(u.Md, "@TRANSX3");
                            if (tr >= 0)
                            {
                                u.LocalPostureWeaponTypeV411LikeOriginal = desired;
                                u.PendingStandAnimIndexLikeOriginal = ResolveStandForPostureV411LikeOriginal(u, desired);
                                u.TransitionTargetLocalPostureV411LikeOriginal = desired;
                                SelectAnimationStateLikeOriginal(
                                    u, C2UnitOriginalState.Transition, tr, true,
                                    (reason ?? "TryToStand") + "_TRANSX3");
                                return;
                            }
                        }
                        if (local == 3 &&
                            (u.Md.TransXMaskLikeOriginal & (1 << desired)) != 0)
                        {
                            int tr = ResolveAnimationIndexLikeOriginal(u.Md, "#TRANS3X");
                            if (tr < 0) tr = ResolveAnimationIndexLikeOriginal(u.Md, "@TRANS3X");
                            if (tr >= 0)
                            {
                                u.LocalPostureWeaponTypeV411LikeOriginal = desired;
                                u.PendingStandAnimIndexLikeOriginal = ResolveStandForPostureV411LikeOriginal(u, desired);
                                u.TransitionTargetLocalPostureV411LikeOriginal = desired;
                                SelectAnimationStateLikeOriginal(
                                    u, C2UnitOriginalState.Transition, tr, true,
                                    (reason ?? "TryToStand") + "_TRANS3X");
                                return;
                            }
                        }
                    }

                    // 5) Transform old LocalNewState to neutral through UATTACK.
                    int uattack = ResolveUAttackForPostureV411LikeOriginal(u.Md, local);
                    u.LocalPostureWeaponTypeV411LikeOriginal = -1;
                    u.TransitionTargetLocalPostureV411LikeOriginal = -1;
                    u.PendingPostureAfterNeutralLikeOriginal = desired;
                    u.PendingStandAnimIndexLikeOriginal = ResolveStandForPostureV411LikeOriginal(u, desired);
                    if (uattack >= 0)
                    {
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Transition, uattack, true,
                            (reason ?? "TryToStand") + "_UATTACK_to_neutral");
                        return;
                    }

                    // Exact NewMon.cpp fallback: when UATTACK is absent, set
                    // STAND + LocalNewState=0 and RETURN.  The original never starts
                    // PATTACK for the requested state in this same pass.
                    u.PendingPostureAfterNeutralLikeOriginal = -1;
                    u.PendingStandAnimIndexLikeOriginal = -1;
                    int neutralStand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                    if (neutralStand >= 0)
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Stand, neutralStand, true,
                            (reason ?? "TryToStand") + "_neutral_fallback_return");
                    return;
                }

                // 6) LocalNewState==0 -> requested state through PATTACK.
                if (desired >= 0)
                {
                    int pattack = ResolvePAttackForPostureV411LikeOriginal(u.Md, desired);
                    u.LocalPostureWeaponTypeV411LikeOriginal = desired;
                    u.PendingPostureAfterNeutralLikeOriginal = -1;
                    u.TransitionTargetLocalPostureV411LikeOriginal = desired;
                    int targetStand = ResolveStandForPostureV411LikeOriginal(u, desired);
                    u.PendingStandAnimIndexLikeOriginal = targetStand;
                    if (pattack >= 0)
                    {
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Transition, pattack, true,
                            (reason ?? "TryToStand") + "_PATTACK_from_neutral");
                        return;
                    }

                    // PATTACK missing -> PSTAND if enabled, otherwise STAND.
                    if (targetStand >= 0)
                    {
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Stand, targetStand, true,
                            (reason ?? "TryToStand") + "_PSTAND_fallback");
                        return;
                    }
                }

                int stand0 = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                if (stand0 >= 0)
                    SelectAnimationStateLikeOriginal(
                        u, C2UnitOriginalState.Stand, stand0, true,
                        (reason ?? "TryToStand") + "_STAND_fallback");
                return;
            }

            // NewState == LocalNewState. TryToStand is reached after the current
            // unbreakable attack/transition has finished; never truncate one if a
            // managed caller happens to ask for the same state mid-animation.
            if (u.State == C2UnitOriginalState.Attack ||
                u.State == C2UnitOriginalState.Recharge ||
                (u.State == C2UnitOriginalState.Transition && !u.FrameFinishedLikeOriginal))
                return;

            if (desired >= 0)
            {
                // NewState==LocalNewState!=0: first try the exact PSTAND slot.
                // In this equal-state branch retail deliberately does NOT fall
                // back to STAND for MotionStyle==8 (FLY).  V411 used the generic
                // resolver, which always fell back to STAND and changed this rule.
                // RestA1 remains a separately inventoried adapter gap below.
                int exactPostureStandV412 = ResolveExactPostureStandV412LikeOriginal(u.Md, desired);
                if (exactPostureStandV412 >= 0)
                {
                    SelectAnimationStateLikeOriginal(
                        u, C2UnitOriginalState.Stand, exactPostureStandV412, true,
                        (reason ?? "TryToStand_equal") + "_PSTAND");
                }
                else if (MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle) != 8)
                {
                    int equalFallbackStandV412 = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                    if (equalFallbackStandV412 >= 0)
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Stand, equalFallbackStandV412, true,
                            (reason ?? "TryToStand_equal") + "_STAND_fallback");
                }
                return;
            }

            // Exact neutral gate from NewMon.cpp: ordinary neutral units are reset
            // to STAND only for SINGLESTEP, or after the current animation finished.
            // V411 reset STAND unconditionally and could truncate a neutral one-shot.
            if (MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle) == 7 ||
                u.FrameFinishedLikeOriginal)
            {
                int neutral = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                if (neutral < 0) neutral = ResolveAnimationIndexLikeOriginal(u.Md, RestAnimationName);
                if (neutral >= 0 && u.State != C2UnitOriginalState.Attack &&
                    u.State != C2UnitOriginalState.Recharge)
                    SelectAnimationStateLikeOriginal(
                        u, C2UnitOriginalState.Stand, neutral, true,
                        (reason ?? "TryToStand") + (rest ? "_rest_allowed" : "_neutral"));
            }
        }

        private int ResolveTransitionXYV411LikeOriginal(MdModel md, int fromPosture, int toPosture)
        {
            if (md == null || fromPosture < 0 || toPosture < 0) return -1;
            string n = "#TRANS" +
                       fromPosture.ToString(CultureInfo.InvariantCulture) +
                       toPosture.ToString(CultureInfo.InvariantCulture);
            int idx = ResolveAnimationIndexLikeOriginal(md, n);
            if (idx < 0)
                idx = ResolveAnimationIndexLikeOriginal(md, "@" + n.Substring(1));
            return idx;
        }

        private int ResolvePAttackForPostureV411LikeOriginal(MdModel md, int posture)
        {
            if (md == null || posture < 0) return -1;
            string suffix = posture > 0 ? posture.ToString(CultureInfo.InvariantCulture) : string.Empty;
            int idx = ResolveAnimationIndexLikeOriginal(md, "#PATTACK" + suffix);
            if (idx < 0) idx = ResolveAnimationIndexLikeOriginal(md, "@PATTACK" + suffix);
            return idx;
        }

        private int ResolveUAttackForPostureV411LikeOriginal(MdModel md, int posture)
        {
            if (md == null || posture < 0) return -1;
            string suffix = posture > 0 ? posture.ToString(CultureInfo.InvariantCulture) : string.Empty;
            int idx = ResolveAnimationIndexLikeOriginal(md, "#UATTACK" + suffix);
            if (idx < 0) idx = ResolveAnimationIndexLikeOriginal(md, "@UATTACK" + suffix);
            return idx;
        }

        private int ResolveStandForPostureV411LikeOriginal(C2UnitOriginalRuntime u, int posture)
        {
            if (u == null || u.Md == null) return -1;
            if (posture >= 0)
            {
                string suffix = posture > 0 ? posture.ToString(CultureInfo.InvariantCulture) : string.Empty;
                int idx = ResolveAnimationIndexLikeOriginal(u.Md, "#PSTAND" + suffix);
                if (idx < 0) idx = ResolveAnimationIndexLikeOriginal(u.Md, "@PSTAND" + suffix);
                if (idx >= 0) return idx;
            }
            return ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
        }

        private int ResolveExactPostureStandV412LikeOriginal(MdModel md, int posture)
        {
            if (md == null || posture < 0) return -1;
            string suffix = posture > 0 ? posture.ToString(CultureInfo.InvariantCulture) : string.Empty;
            int idx = ResolveAnimationIndexLikeOriginal(md, "#PSTAND" + suffix);
            if (idx < 0) idx = ResolveAnimationIndexLikeOriginal(md, "@PSTAND" + suffix);
            return idx;
        }

        private void SelectSteadyPostureStandV411LikeOriginal(
            C2UnitOriginalRuntime u,
            int posture,
            string reason)
        {
            if (u == null || u.Md == null) return;
            int stand = ResolveStandForPostureV411LikeOriginal(u, posture);
            if (stand >= 0)
                SelectAnimationStateLikeOriginal(
                    u, C2UnitOriginalState.Stand, stand, true,
                    (reason ?? "TryToStand") + "_PSTAND");
        }

        internal bool PlayRuntimeAttackOneShotV325LikeOriginal(C2UnitOriginalRuntime u, int attackState)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death) return false;
            int state = Mathf.Max(0, attackState);
            string suffix = state > 0 ? state.ToString(CultureInfo.InvariantCulture) : string.Empty;
            int attack = ResolveAnimationIndexLikeOriginal(u.Md, "#ATTACK" + suffix);
            if (attack < 0)
                attack = ResolveAnimationIndexLikeOriginal(u.Md, "@ATTACK" + suffix);
            if (attack < 0) return false;
            u.HasMoveTargetLikeOriginal = false;
            SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Attack, attack, true, "order_attack_" + state);
            ApplyUnitFrameLikeOriginal(u, "order_attack_" + state);
            return true;
        }

        internal void QueueRuntimeSlowRechargeLikeOriginal(
            C2UnitOriginalRuntime u, int attackMode, int pauseTicks)
        {
            if (u == null || pauseTicks <= 0) return;
            u.RechargeAttackModeLikeOriginal = attackMode;
            u.RechargeTicksMaximumLikeOriginal = Math.Max(1, pauseTicks);
            u.RechargeTicksRemainingLikeOriginal = Math.Max(1, pauseTicks);
        }

        internal bool BeginRuntimeSlowRechargeLikeOriginal(C2UnitOriginalRuntime u, int pauseTicks)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death)
                return false;

            if (u.RechargeTicksRemainingLikeOriginal <= 0)
            {
                if (pauseTicks <= 0) return false;
                u.RechargeTicksMaximumLikeOriginal = Math.Max(1, pauseTicks);
                u.RechargeTicksRemainingLikeOriginal = Math.Max(1, pauseTicks);
            }
            if (u.RechargeTicksMaximumLikeOriginal <= 0)
                u.RechargeTicksMaximumLikeOriginal = Math.Max(1, u.RechargeTicksRemainingLikeOriginal);

            int reload = ResolveAnimationIndexLikeOriginal(u.Md, "#ATTACK3");
            if (reload < 0)
                reload = ResolveAnimationIndexLikeOriginal(u.Md, "@ATTACK3");
            if (reload < 0)
                return false;

            AnimModel anim = u.Md.Animations[reload];
            int reloadTicks = Math.Max(1, anim != null ? anim.Frames.Count : 1);
            // NewMon.cpp: delay -= anm_Attack[3].NFrames at the moment the
            // reload animation is installed.  The visual cycle then plays.
            u.RechargeTicksRemainingLikeOriginal = Math.Max(
                0, u.RechargeTicksRemainingLikeOriginal - reloadTicks);
            u.HasMoveTargetLikeOriginal = false;
            SelectAnimationStateLikeOriginal(
                u, C2UnitOriginalState.Recharge, reload, true,
                "slow_recharge_begin");
            ApplyUnitFrameLikeOriginal(u, "slow_recharge_begin");
            return true;
        }

        internal bool TryGetRuntimeSlowRechargeProgressLikeOriginal(
            C2UnitOriginalRuntime u,
            int attackMode,
            out int remainingTicks,
            out int maximumTicks,
            out bool animationPlaying)
        {
            remainingTicks = 0;
            maximumTicks = 0;
            animationPlaying = false;
            if (u == null) return false;
            if (u.RechargeAttackModeLikeOriginal != attackMode) return false;

            remainingTicks = Math.Max(0, u.RechargeTicksRemainingLikeOriginal);
            maximumTicks = Math.Max(0, u.RechargeTicksMaximumLikeOriginal);
            animationPlaying = u.State == C2UnitOriginalState.Recharge;
            return maximumTicks > 0 && (remainingTicks > 0 || animationPlaying);
        }

        internal void ClearRuntimeSlowRechargeDelayLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return;
            u.RechargeTicksRemainingLikeOriginal = 0;
            u.RechargeTicksMaximumLikeOriginal = 0;
            u.RechargeAttackModeLikeOriginal = -1;
        }

        internal void InterruptRuntimeSlowRechargeAnimationForExternalOrderV399LikeOriginal(
            C2UnitOriginalRuntime u,
            string reason)
        {
            if (u == null || u.State != C2UnitOriginalState.Recharge) return;
            // NewMon.cpp: SLOWRECHARGE is only installed while the object is idle
            // (!LocalOrder / StandTime gate). A movement order interrupts ATTACK3,
            // but OB->delay/MaxDelay survive and recharge resumes when idle again.
            int stand = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
            if (stand < 0) stand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
            if (stand >= 0)
            {
                SelectAnimationStateLikeOriginal(
                    u, C2UnitOriginalState.Stand, stand, true,
                    "slow_recharge_interrupted_" + (reason ?? "external_order_v399"));
                ApplyUnitFrameLikeOriginal(u, "slow_recharge_interrupted_external_order_v399");
            }
        }

        internal bool TryGetRuntimeWeaponStartWorldLikeOriginal(
            C2UnitOriginalRuntime u, int muzzleIndex, out Vector3 world)
        {
            Vector2 originalPoint;
            return TryGetRuntimeWeaponStartWorldV435LikeOriginal(u, muzzleIndex, out world, out originalPoint);
        }

        internal bool TryGetRuntimeWeaponStartWorldV435LikeOriginal(
            C2UnitOriginalRuntime u, int muzzleIndex, out Vector3 world, out Vector2 originalPoint)
        {
            world = u != null ? u.WorldPosition : Vector3.zero;
            originalPoint = u != null ? new Vector2(u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal) / 16.0f : Vector2.zero;
            AnimModel anim = CurrentAnim(u);
            if (u == null || !u.ActiveLikeOriginal || anim == null || anim.Frames.Count == 0)
                return false;

            int frameIndex = FixedFrameIndexLikeOriginal(u, anim);
            FrameModel frame = anim.Frames[frameIndex];
            DrawSpriteAudit draw = ComputeDrawSpriteUnitLikeOriginal(anim, frame, u);
            int rotations = Math.Max(1, anim.Rotations);
            int shot = Mathf.Clamp(muzzleIndex, 0, Math.Max(0, anim.DoubleShot));
            int point = shot * rotations + Mathf.Clamp(draw.Dir, 0, rotations - 1);
            int pointX = anim.ActivePtX != null && point < anim.ActivePtX.Length
                ? anim.ActivePtX[point]
                : 0;
            int pointY = anim.ActivePtY != null && point < anim.ActivePtY.Length
                ? anim.ActivePtY[point]
                : 0;

            // NewMon.cpp 12674..12705: the left half mirrors both the active
            // point and NewFrame.dx; vertical muzzle height is -(ptY+dy).
            float localX = draw.MirrorGeometry
                ? -(pointX + frame.Dx) * VisualScale
                : (pointX + frame.Dx) * VisualScale;
            float localY = -(pointY + frame.Dy) * VisualScale;
            originalPoint.x += draw.MirrorGeometry ? -(pointX + frame.Dx) : pointX + frame.Dx;
            world = GetUnitLocalToWorldMatrixLikeOriginal(u).MultiplyPoint3x4(new Vector3(localX, localY, 0.0f));
            return true;
        }

        private int ResolveRuntimeStandAnimationIndexV322LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null) return -1;
            string[] candidates = null;
            int localPostureV411 = u.LocalPostureWeaponTypeV411LikeOriginal;
            if (localPostureV411 >= 0)
            {
                string suffix = localPostureV411 > 0
                    ? localPostureV411.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                candidates = new[] { "#PSTAND" + suffix, "@PSTAND" + suffix };
            }

            for (int i = 0; candidates != null && i < candidates.Length; i++)
            {
                int idx = ResolveAnimationIndexLikeOriginal(u.Md, candidates[i]);
                if (idx >= 0) return idx;
            }
            return ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
        }

        // V427: SetRuntimeMotionStateLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: SetRuntimeWalkPathFrameLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        // V427: DirectionFromWorldDeltaLikeOriginal physically moved to C2MovementSystemV425LikeOriginal.cs.
        private bool ShouldLoopAnimationLikeOriginal(C2UnitOriginalRuntime u, AnimModel anim)
        {
            if (u == null || anim == null) return false;
            if (u.State == C2UnitOriginalState.Stand) return true;
            if (u.State == C2UnitOriginalState.Rest) return false;   // BREAKANIMATION #REST behaviour for idle.
            if (u.State == C2UnitOriginalState.Transition) return false;
            if (u.State == C2UnitOriginalState.Death) return false;
            if (u.State == C2UnitOriginalState.Work) return true;
            if (u.State == C2UnitOriginalState.Attack) return false;
            if (u.State == C2UnitOriginalState.Recharge) return false;
            // Motion is distance-driven above. Unknown/special one-shots must
            // retain SetNextFrame semantics and stop on their final frame.
            return false;
        }

        private void SelectAnimationStateLikeOriginal(C2UnitOriginalRuntime u, C2UnitOriginalState state, int animIndex, bool resetFrame, string reason)
        {
            if (u == null || u.Md == null || animIndex < 0 || animIndex >= u.Md.Animations.Count) return;
            bool animationChangedLikeOriginal = u.State != state || u.CurrentAnimIndex != animIndex;
            u.State = state;
            u.CurrentAnimIndex = animIndex;
            // FrameFinished belongs to the currently selected NewAnm.  Even paths that
            // intentionally preserve CurrentFrameLong (SINGLESTEP motion phase) must not
            // carry the completion latch from the previous Stand/Work animation.
            if (animationChangedLikeOriginal)
                u.FrameFinishedLatchedForOrdersLikeOriginal = false;
            if (animationChangedLikeOriginal || resetFrame)
                u.FrameUploadPendingLikeOriginal = true;
            if (resetFrame)
            {
                u.CurrentFrameLong = 0;
                u.AnimFrameLongRemainderLikeOriginal = 0.0;
                u.FrameFinishedLikeOriginal = false;
                u.FrameFinishedLatchedForOrdersLikeOriginal = false;
                u.LastFrameKey = string.Empty;
            }
            if (u.AnimState == null) u.AnimState = new C2UnitOriginalAnimationState();
            u.AnimState.State = state;
            u.AnimState.CurrentAnimIndex = animIndex;
            u.AnimState.CurrentAnimationName = u.Md.Animations[animIndex].Name;
            u.AnimState.Reason = reason ?? string.Empty;
        }

        private bool ApplyUnitFrameLikeOriginal(C2UnitOriginalRuntime u, string reason)
        {
            _visualFrameApplyCallsV373LikeOriginal++;
            AnimModel anim = CurrentAnim(u);
            if (anim == null || anim.Frames.Count == 0) return false;
            int frameIndex = FixedFrameIndexLikeOriginal(u, anim);
            FrameModel fr = anim.Frames[frameIndex];
            DrawSpriteAudit draw = ComputeDrawSpriteUnitLikeOriginal(anim, fr, u);

            int ownerPlayerIndexLikeOriginal = (u != null && u.Probe != null) ? u.Probe.Nation : 0;
            int ownerColorIdLikeOriginal = C2PlayerColorsLikeOriginal.GetPlayerColorId(ownerPlayerIndexLikeOriginal);
            if (!string.IsNullOrEmpty(u.LastFrameKey) && u.LastTexture != null &&
                string.Equals(u.LastRenderedPackageLikeOriginal, fr.Package, StringComparison.OrdinalIgnoreCase) &&
                u.LastRenderedSpriteLikeOriginal == draw.DisplaySprite &&
                u.LastRenderedMirrorLikeOriginal == draw.MirrorGeometry &&
                u.LastRenderedColorIdLikeOriginal == ownerColorIdLikeOriginal &&
                u.LastRenderedDxLikeOriginal == fr.Dx && u.LastRenderedDyLikeOriginal == fr.Dy)
            {
                _visualFrameScalarHitsV373LikeOriginal++;
                return true;
            }

            string audit = string.Empty;
            global::C2UnitFrameOriginal viewerFrame;
            if (UseViewerGpCacheLikeOriginal &&
                TryGetSharedViewerFrameV374LikeOriginal(u, fr, draw, ownerColorIdLikeOriginal, out viewerFrame, out audit))
            {
                // LastFrameKey remains the existing invalidation sentinel, while
                // the actual comparison uses allocation-free scalar fields.
                // Avoid dirtying the managed object graph for unchanged references
                // on every animation tick (incremental GC write barriers).
                if (!ReferenceEquals(u.LastFrameKey, "ready")) u.LastFrameKey = "ready";
                if (!ReferenceEquals(u.LastRenderedPackageLikeOriginal, fr.Package))
                    u.LastRenderedPackageLikeOriginal = fr.Package;
                u.LastRenderedSpriteLikeOriginal = draw.DisplaySprite;
                u.LastRenderedMirrorLikeOriginal = draw.MirrorGeometry;
                u.LastRenderedColorIdLikeOriginal = ownerColorIdLikeOriginal;
                u.LastRenderedDxLikeOriginal = fr.Dx;
                u.LastRenderedDyLikeOriginal = fr.Dy;
                if (!ReferenceEquals(u.LastTexture, viewerFrame.Texture))
                    u.LastTexture = viewerFrame.Texture;
                u.LastTextureUvRectLikeOriginal = viewerFrame.UvRect.width > 0.0f && viewerFrame.UvRect.height > 0.0f
                    ? viewerFrame.UvRect
                    : new Rect(0.0f, 0.0f, 1.0f, 1.0f);
                u.LastDecodedWidth = viewerFrame.Width;
                u.LastDecodedHeight = viewerFrame.Height;
                ApplyViewerGpFrameToMeshLikeOriginal(u, viewerFrame, fr, draw);

                if (LogFirstFramePerUnit && _firstFrameLogs < MaxFirstFrameLogs)
                {
                    _firstFrameLogs++;
                    Debug.Log(LogPrefix + " FRAME ok=True reason=" + reason +
                              " unit='" + u.Probe.MonsterId + "'" +
                              " anim='" + anim.Name + "'" +
                              " frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + "/" + anim.Frames.Count.ToString(CultureInfo.InvariantCulture) +
                              " frameLong=" + u.CurrentFrameLong.ToString(CultureInfo.InvariantCulture) +
                              " fileRef=" + fr.FileRef.ToString(CultureInfo.InvariantCulture) +
                              " package='" + fr.Package + "'" +
                              " NewFrame.SpriteID=" + fr.SpriteId.ToString(CultureInfo.InvariantCulture) +
                              " NewFrame.dxdy=" + fr.Dx.ToString(CultureInfo.InvariantCulture) + "/" + fr.Dy.ToString(CultureInfo.InvariantCulture) +
                              " realDir=" + u.RealDirPrecise.ToString(CultureInfo.InvariantCulture) +
                              " drawDir=" + draw.Dir.ToString(CultureInfo.InvariantCulture) +
                              " dirOffset=" + draw.DirOffset.ToString(CultureInfo.InvariantCulture) +
                              " displaySprite=" + draw.DisplaySprite.ToString(CultureInfo.InvariantCulture) +
                              " mirrored=" + draw.MirroredByTransform +
                              " viewerGpFrame=" + viewerFrame.Width.ToString(CultureInfo.InvariantCulture) + "x" + viewerFrame.Height.ToString(CultureInfo.InvariantCulture) +
                              " rectDxDy=" + viewerFrame.Dx.ToString(CultureInfo.InvariantCulture) + "/" + viewerFrame.Dy.ToString(CultureInfo.InvariantCulture) +
                              " drawPath=TemnyLessViewer::C2GpSystem->C2DirectSpriteBank->C2RenderedFrame" +
                              " alphaBoost=" + ViewerLikeUnitAlphaBoost.ToString("0.###", CultureInfo.InvariantCulture) +
                              " localRectRule=viewer_anchor:signedDx=" + (draw.MirrorGeometry ? -fr.Dx : fr.Dx).ToString(CultureInfo.InvariantCulture) +
                              " audit=" + (audit ?? ""));
                }
                return true;
            }

            if (LogFirstFramePerUnit && _firstFrameLogs < MaxFirstFrameLogs)
            {
                _firstFrameLogs++;
                Debug.LogWarning(LogPrefix + " FRAME_MISS_VIEWER_ONLY unit='" + u.Probe.MonsterId + "' anim='" + anim.Name + "' frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + " package='" + fr.Package + "' displaySprite=" + draw.DisplaySprite.ToString(CultureInfo.InvariantCulture) + " oldFrameProvider=0 audit=" + (audit ?? ""));
            }
            return false;
        }

        private bool TryGetSharedViewerFrameV374LikeOriginal(
            C2UnitOriginalRuntime u, FrameModel fr, DrawSpriteAudit draw, int colorId,
            out global::C2UnitFrameOriginal frame, out string audit)
        {
            int alpha1000 = Mathf.RoundToInt(ViewerLikeUnitAlphaBoost * 1000.0f);
            int hash = string.IsNullOrEmpty(fr.Package) ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(fr.Package);
            unchecked
            {
                hash = (hash * 397) ^ draw.DisplaySprite;
                hash = (hash * 397) ^ fr.SpriteId;
                hash = (hash * 397) ^ colorId;
                hash = (hash * 397) ^ alpha1000;
                hash = (hash * 397) ^ (draw.MirrorGeometry ? 1 : 0);
            }
            int slot = hash & (_viewerHotFramesV374LikeOriginal.Length - 1);
            ref ViewerHotFrameV374LikeOriginal cached = ref _viewerHotFramesV374LikeOriginal[slot];
            if (cached.Package != null && cached.DisplaySprite == draw.DisplaySprite &&
                cached.FallbackSprite == fr.SpriteId && cached.ColorId == colorId &&
                cached.Alpha1000 == alpha1000 && cached.Mirror == draw.MirrorGeometry &&
                string.Equals(cached.Package, fr.Package, StringComparison.OrdinalIgnoreCase) &&
                cached.Frame.Texture != null)
            {
                frame = cached.Frame;
                frame.FileRef = fr.FileRef;
                audit = "viewer_shared_frame_v374";
                VisualHotFrameHitsV374LikeOriginal++;
                return true;
            }
            if (!TryGetViewerGpFrameLikeOriginal(u, fr, draw, out frame, out audit))
                return false;
            cached.Package = fr.Package;
            cached.DisplaySprite = draw.DisplaySprite;
            cached.FallbackSprite = fr.SpriteId;
            cached.ColorId = colorId;
            cached.Alpha1000 = alpha1000;
            cached.Mirror = draw.MirrorGeometry;
            cached.Frame = frame;
            return true;
        }

        private bool TryGetViewerGpFrameLikeOriginal(C2UnitOriginalRuntime u, FrameModel fr, DrawSpriteAudit draw, out global::C2UnitFrameOriginal frame, out string audit)
        {
            frame = new global::C2UnitFrameOriginal();
            audit = string.Empty;
            if (u == null)
            {
                audit = "viewer_gp_missing_unit";
                return false;
            }
            if (string.IsNullOrWhiteSpace(fr.Package))
            {
                audit = "viewer_gp_empty_package";
                return false;
            }

            string packageKey;
            if (!_viewerPackageKeyByPackage.TryGetValue(fr.Package, out packageKey))
            {
                packageKey = (_dataRoot ?? string.Empty) + "|" + fr.Package;
                _viewerPackageKeyByPackage[fr.Package] = packageKey;
            }
            int gpId;
            if (!_viewerGpIdByPackage.TryGetValue(packageKey, out gpId))
            {
                string preloadError;
                gpId = _viewerGps.PreLoadGPImage(fr.Package, _dataRoot, out preloadError);
                if (gpId < 0)
                {
                    audit = "viewer_gp_preload_failed " + (preloadError ?? string.Empty);
                    return false;
                }
                _viewerGpIdByPackage[packageKey] = gpId;
            }

            int displaySprite = draw.DisplaySprite;
            int frameCount = _viewerGps.GetFrameCount(gpId);
            if (frameCount > 0 && (displaySprite < 0 || displaySprite >= frameCount))
                displaySprite = fr.SpriteId;

            int drawSprite = draw.MirrorGeometry ? (displaySprite + 4096) : displaySprite;
            int ownerPlayerIndexLikeOriginal = (u.Probe != null) ? u.Probe.Nation : 0;
            int ownerColorIdLikeOriginal = C2PlayerColorsLikeOriginal.GetPlayerColorId(ownerPlayerIndexLikeOriginal);
            ViewerTextureKeyLookupLikeOriginal lookup = new ViewerTextureKeyLookupLikeOriginal
            {
                GpId = gpId,
                DrawSprite = drawSprite,
                ColorId = ownerColorIdLikeOriginal,
                Alpha1000 = Mathf.RoundToInt(ViewerLikeUnitAlphaBoost * 1000.0f)
            };
            global::C2UnitFrameOriginal readyFrame;
            if (_viewerReadyFrameByLookupLikeOriginal.TryGetValue(lookup, out readyFrame))
            {
                if (readyFrame.Texture != null)
                {
                    _visualReadyFrameCacheHitsV373LikeOriginal++;
                    frame = readyFrame;
                    audit = "viewer_ready_frame_cache";
                    return true;
                }
                _viewerReadyFrameByLookupLikeOriginal.Remove(lookup);
            }
            string texKey;
            if (!_viewerTextureStringKeyCacheLikeOriginal.TryGetValue(lookup, out texKey))
            {
                string natSuffixLikeOriginal = C2PlayerColorsLikeOriginal.CacheSuffixForPlayer(ownerPlayerIndexLikeOriginal);
                texKey = packageKey + "|spr=" + drawSprite.ToString(CultureInfo.InvariantCulture) + "|" +
                         natSuffixLikeOriginal + "|ab=" + ViewerLikeUnitAlphaBoost.ToString("0.###", CultureInfo.InvariantCulture);
                _viewerTextureStringKeyCacheLikeOriginal[lookup] = texKey;
            }
            Texture2D tex;
            bool cacheHasTexture = _viewerTextureByFrame.TryGetValue(texKey, out tex) && tex != null;
            if (cacheHasTexture)
                _visualTextureCacheHitsV373LikeOriginal++;
            Rect texUv = new Rect(0f, 0f, 1f, 1f);
            if (cacheHasTexture)
            {
                Rect cachedUv;
                if (_viewerTextureUvByFrameLikeOriginal.TryGetValue(texKey, out cachedUv))
                    texUv = cachedUv;
            }

            ViewerFrameMetaLikeOriginal frameMeta;
            bool haveFrameMeta = _viewerFrameMetaByKeyLikeOriginal.TryGetValue(texKey, out frameMeta);
            global::TemnyLessViewer.C2RenderedFrame rendered = null;
            string err = string.Empty;
            bool directNationColor = false;
            if (!cacheHasTexture)
            {

                Color32 nationColor = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(ownerPlayerIndexLikeOriginal);
                if (_viewerGps.GetRenderedFrameNationColor(
                        gpId, drawSprite, nationColor.r, nationColor.g, nationColor.b,
                        out rendered, out err) && rendered != null && rendered.Rgba != null)
                {
                    directNationColor = true;
                }
            }
            if (rendered == null && (!cacheHasTexture || !haveFrameMeta) &&
                (!_viewerGps.GetRenderedFrame(gpId, drawSprite, out rendered, out err) || rendered == null || rendered.Rgba == null))
            {
                audit = "viewer_gp_get_frame_failed gpID=" + gpId.ToString(CultureInfo.InvariantCulture) +
                        " sprite=" + drawSprite.ToString(CultureInfo.InvariantCulture) +
                        " err=" + (err ?? string.Empty);
                return false;
            }
            if (rendered != null)
            {
                frameMeta.OriginX = rendered.OriginX;
                frameMeta.OriginY = rendered.OriginY;
                frameMeta.Width = rendered.Width;
                frameMeta.Height = rendered.Height;
                _viewerFrameMetaByKeyLikeOriginal[texKey] = frameMeta;
            }

            bool texFromNationColor = false;
            string texSourceAudit = string.Empty;

            if (!cacheHasTexture)
            {
                global::TemnyLessViewer.C2RenderedFrame renderedForTexture = rendered;
                Texture2D plainTex = directNationColor
                    ? PackRenderedFrameOnSpriteSurfaceLikeOriginal(renderedForTexture, ViewerLikeUnitAlphaBoost, out texUv)
                    : CreateUnityTextureFromViewerTopLeftRgbaLikeOriginal(fr.Package, drawSprite, renderedForTexture, ViewerLikeUnitAlphaBoost);
                if (plainTex == null)
                {
                    audit = "viewer_gp_texture_create_failed";
                    return false;
                }

                tex = plainTex;
                texFromNationColor = directNationColor;
                texSourceAudit = directNationColor
                    ? "viewer_direct_argb4444_nation_bit_exact"
                    : "viewer_rgba_plain";

                Texture2D nationActualTex = null;
                Texture2D nationMaskBlackTex = null;
                Texture2D nationMaskWhiteTex = null;
                string nationOverlayAudit = string.Empty;
                bool haveNationOverlay = !directNationColor &&
                    TryGetNationColorOverlaySetLikeOriginal(u, fr, displaySprite, out nationActualTex, out nationMaskBlackTex, out nationMaskWhiteTex, out nationOverlayAudit);
                if (haveNationOverlay)
                {
                    Texture2D composedTex;
                    string composeAudit;
                    if (TryComposeNationColorMaskOnlyBottomAlignedLikeOriginal(plainTex, nationActualTex, nationMaskBlackTex, nationMaskWhiteTex, fr.Package, drawSprite, draw.MirrorGeometry, out composedTex, out composeAudit) && composedTex != null)
                    {
                        tex = composedTex;
                        texFromNationColor = true;
                        texSourceAudit = "viewer_nat_mask_only_overlay [" + nationOverlayAudit + "] [" + composeAudit + "]";
                        DestroyTransientUnitTextureLikeOriginal(plainTex, tex);
                    }
                    else
                    {
                        tex = plainTex;
                        texFromNationColor = false;
                        texSourceAudit = "viewer_rgba_plain nat_mask_rejected [" + nationOverlayAudit + "] [" + composeAudit + "]";
                    }
                }

                DestroyTransientUnitTextureLikeOriginal(nationActualTex, tex);
                DestroyTransientUnitTextureLikeOriginal(nationMaskBlackTex, tex);
                DestroyTransientUnitTextureLikeOriginal(nationMaskWhiteTex, tex);
                Texture2D frameTexture = tex;
                Texture2D surfaceTexture = directNationColor
                    ? frameTexture
                    : PackViewerFrameOnSpriteSurfaceLikeOriginal(frameTexture, out texUv);
                if (surfaceTexture != null)
                {
                    tex = surfaceTexture;
                    if (!ReferenceEquals(frameTexture, surfaceTexture))
                        DestroyTransientUnitTextureLikeOriginal(frameTexture, surfaceTexture);
                }
                PruneViewerTextureCacheIfNeededLikeOriginal("before_add");
                AddViewerTextureCacheEntryLikeOriginal(texKey, tex, texFromNationColor);
                _visualTextureUploadsV373LikeOriginal++;
                _viewerTextureUvByFrameLikeOriginal[texKey] = texUv;
                PruneViewerTextureCacheIfNeededLikeOriginal("after_add");
            }
            else
            {
                bool cachedNation;
                texFromNationColor = _viewerTextureNationByFrameLikeOriginal.TryGetValue(texKey, out cachedNation) && cachedNation;
                texSourceAudit = texFromNationColor ? "cache_hit_nat_overlay" : "cache_hit_plain";
            }

            frame.Texture = tex;
            frame.UvRect = texUv;
            frame.FileRef = fr.FileRef;
            frame.SpriteId = drawSprite;
            frame.Dx = -frameMeta.OriginX;
            frame.Dy = -frameMeta.OriginY;
            frame.Width = frameMeta.Width;
            frame.Height = frameMeta.Height;
            frame.Audit = "viewer_gp ok gpID=" + gpId.ToString(CultureInfo.InvariantCulture) +
                          " sprite=" + drawSprite.ToString(CultureInfo.InvariantCulture) +
                          " origin=" + frameMeta.OriginX.ToString(CultureInfo.InvariantCulture) + "/" + frameMeta.OriginY.ToString(CultureInfo.InvariantCulture) +
                          " resolvedSprite=" + displaySprite.ToString(CultureInfo.InvariantCulture) +
                          " frameCount=" + frameCount.ToString(CultureInfo.InvariantCulture) +
                          " cacheFrames=" + _viewerGps.CachedFrameCount.ToString(CultureInfo.InvariantCulture) +
                          " hitsMisses=" + _viewerGps.Hits.ToString(CultureInfo.InvariantCulture) + "/" + _viewerGps.Misses.ToString(CultureInfo.InvariantCulture) +
                          " texSource=" + texSourceAudit;
            _viewerReadyFrameByLookupLikeOriginal[lookup] = frame;
            audit = frame.Audit;
            return true;
        }

        private void LateUpdate()
        {
            // Resolve every requested animation frame synchronously, then upload
            // each changed shared surface once before rendering. A per-frame
            // quota on individual sprites made whole brigades show old poses.
            for (int i = 0; i < _viewerSpriteSurfacesLikeOriginal.Count; i++)
            {
                var surface = _viewerSpriteSurfacesLikeOriginal[i];
                if (!surface.Dirty || surface.Texture == null) continue;
                surface.Texture.Apply(false, false);
                surface.Dirty = false;
            }
        }

        private Texture2D PackViewerFrameOnSpriteSurfaceLikeOriginal(Texture2D frameTexture, out Rect uv)
        {
            uv = new Rect(0f, 0f, 1f, 1f);
            if (frameTexture == null || frameTexture.width <= 0 || frameTexture.height <= 0)
                return frameTexture;
            if (frameTexture.width > ViewerSpriteSurfaceSideLikeOriginal || frameTexture.height > ViewerSpriteSurfaceSideLikeOriginal)
                return frameTexture;

            ViewerSpriteSurfaceLikeOriginal surface = null;
            RectInt dst = default(RectInt);
            for (int i = 0; i < _viewerSpriteSurfacesLikeOriginal.Count; i++)
            {
                if (_viewerSpriteSurfacesLikeOriginal[i].TryAllocate(frameTexture.width, frameTexture.height, out dst))
                {
                    surface = _viewerSpriteSurfacesLikeOriginal[i];
                    break;
                }
            }
            if (surface == null)
            {
                surface = CreateViewerSpriteSurfaceLikeOriginal();
                if (!surface.TryAllocate(frameTexture.width, frameTexture.height, out dst))
                    return frameTexture;
            }

            surface.Texture.SetPixels32(dst.x, dst.y, dst.width, dst.height, frameTexture.GetPixels32());
            surface.Dirty = true;
            float inv = 1.0f / ViewerSpriteSurfaceSideLikeOriginal;
            uv = new Rect(dst.x * inv, dst.y * inv, dst.width * inv, dst.height * inv);
            return surface.Texture;
        }

        private Texture2D PackRenderedFrameOnSpriteSurfaceLikeOriginal(
            global::TemnyLessViewer.C2RenderedFrame rendered, float alphaBoost, out Rect uv)
        {
            uv = new Rect(0f, 0f, 1f, 1f);
            if (rendered == null || rendered.Rgba == null || rendered.Width <= 0 || rendered.Height <= 0 ||
                rendered.Width > ViewerSpriteSurfaceSideLikeOriginal || rendered.Height > ViewerSpriteSurfaceSideLikeOriginal)
                return CreateUnityTextureFromViewerTopLeftRgbaLikeOriginal("oversize", 0, rendered, alphaBoost);

            ViewerSpriteSurfaceLikeOriginal surface = null;
            RectInt dst = default(RectInt);
            for (int i = 0; i < _viewerSpriteSurfacesLikeOriginal.Count; i++)
            {
                if (_viewerSpriteSurfacesLikeOriginal[i].TryAllocate(rendered.Width, rendered.Height, out dst))
                {
                    surface = _viewerSpriteSurfacesLikeOriginal[i];
                    break;
                }
            }
            if (surface == null)
            {
                surface = CreateViewerSpriteSurfaceLikeOriginal();
                if (!surface.TryAllocate(rendered.Width, rendered.Height, out dst))
                    return null;
            }

            var pixels = surface.Texture.GetRawTextureData<Color32>();
            float boost = Mathf.Clamp(alphaBoost, 1.0f, 2.0f);
            for (int y = 0; y < rendered.Height; y++)
            {
                int srcY = rendered.Height - 1 - y;
                for (int x = 0; x < rendered.Width; x++)
                {
                    int si = (srcY * rendered.Width + x) * 4;
                    byte a = rendered.Rgba[si + 3];
                    if (a != 0 && a != 255 && boost > 1.0001f)
                        a = (byte)Mathf.Min(255, Mathf.RoundToInt(a * boost));
                    pixels[(dst.y + y) * ViewerSpriteSurfaceSideLikeOriginal + dst.x + x] = new Color32(
                        rendered.Rgba[si], rendered.Rgba[si + 1], rendered.Rgba[si + 2], a);
                }
            }
            surface.Dirty = true;
            float inv = 1.0f / ViewerSpriteSurfaceSideLikeOriginal;
            uv = new Rect(dst.x * inv, dst.y * inv, dst.width * inv, dst.height * inv);
            return surface.Texture;
        }

        private ViewerSpriteSurfaceLikeOriginal CreateViewerSpriteSurfaceLikeOriginal()
        {
            ViewerSpriteSurfaceLikeOriginal surface = new ViewerSpriteSurfaceLikeOriginal();
            surface.Texture = new Texture2D(ViewerSpriteSurfaceSideLikeOriginal, ViewerSpriteSurfaceSideLikeOriginal,
                TextureFormat.RGBA32, false, false);
            surface.Texture.name = "C2ViewerSpriteSurface_" + _viewerSpriteSurfacesLikeOriginal.Count.ToString(CultureInfo.InvariantCulture);
            surface.Texture.filterMode = FilterMode.Point;
            surface.Texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = surface.Texture.GetRawTextureData<Color32>();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = default(Color32);
            surface.Dirty = true;
            _viewerSpriteSurfacesLikeOriginal.Add(surface);
            return surface;
        }

        private void AddViewerTextureCacheEntryLikeOriginal(string key, Texture2D tex, bool nationColor)
        {
            if (string.IsNullOrEmpty(key) || tex == null) return;

            Texture2D oldTex;
            if (_viewerTextureByFrame.TryGetValue(key, out oldTex) && oldTex != null && !ReferenceEquals(oldTex, tex))
                DestroyTransientUnitTextureLikeOriginal(oldTex, tex);

            _viewerTextureByFrame[key] = tex;
            _viewerTextureNationByFrameLikeOriginal[key] = nationColor;
            int bytes = Mathf.Max(0, tex.width) * Mathf.Max(0, tex.height) * 4;
            bool firstTextureReference = _viewerTextureMemoryIdsLikeOriginal.Add(tex.GetEntityId());
            _viewerTextureBytesByFrameLikeOriginal[key] = firstTextureReference ? bytes : 0;
            if (firstTextureReference)
                _viewerTextureApproxBytesLikeOriginal += bytes;
            if (nationColor) _viewerTextureNationFramesLikeOriginal++;
            else _viewerTexturePlainFramesLikeOriginal++;
            _viewerTextureCreateEventsLikeOriginal++;

            if (ShouldMarkViewerTextureCreateEventLikeOriginal())
            {
                C2RuntimeDiagnosticsV1.MarkPerfEvent(
                    "UNIT_TEXTURE_CREATE",
                    "cache=" + _viewerTextureByFrame.Count.ToString(CultureInfo.InvariantCulture) +
                    " creates=" + _viewerTextureCreateEventsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                    " nat=" + nationColor.ToString() +
                    " approxMB=" + (_viewerTextureApproxBytesLikeOriginal / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) +
                    " gpFrames=" + _viewerGps.CachedFrameCount.ToString(CultureInfo.InvariantCulture) +
                    " gpHitsMissesEvict=" + _viewerGps.Hits.ToString(CultureInfo.InvariantCulture) + "/" +
                    _viewerGps.Misses.ToString(CultureInfo.InvariantCulture) + "/" +
                    _viewerGps.Evictions.ToString(CultureInfo.InvariantCulture) +
                    " tex='" + ShortPerfTextLikeOriginal(tex.name, 64) + "'" +
                    " size=" + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture));
            }

            // V260C_LOG_CLEAN: this used to log every 32 cached frames and could print hundreds
            // of lines while workers were building.  Keep counters, do not spam the Editor console.
            if (false)
            {
                _viewerTextureMemoryLogsLikeOriginal++;
                Debug.Log(LogPrefix + " UNIT_MEMORY_NATCOLOR_CACHE frames=" + _viewerTextureByFrame.Count.ToString(CultureInfo.InvariantCulture) +
                          " nationFrames=" + _viewerTextureNationFramesLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " plainFrames=" + _viewerTexturePlainFramesLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " approxMB=" + (_viewerTextureApproxBytesLikeOriginal / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) +
                          " last=" + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture) +
                          " nat=" + nationColor.ToString());
            }
        }

        private bool ShouldMarkViewerTextureCreateEventLikeOriginal()
        {
            int count = _viewerTextureByFrame.Count;
            if (count <= 16 || count % 64 == 0)
                return true;

            float now = Time.realtimeSinceStartup;
            if (now >= _nextViewerTexturePerfEventAtLikeOriginal)
            {
                _nextViewerTexturePerfEventAtLikeOriginal = now + 0.5f;
                return true;
            }

            return false;
        }

        private void PruneViewerTextureCacheIfNeededLikeOriginal(string reason)
        {
            if (_viewerTextureByFrame.Count <= UnitViewerTextureCacheHardLimitLikeOriginal)
                return;

            HashSet<EntityId> inUse = BuildViewerTextureInUseSetLikeOriginal();
            int needRemove = Mathf.Max(0, _viewerTextureByFrame.Count - UnitViewerTextureCacheSoftLimitLikeOriginal);
            if (needRemove <= 0)
                return;

            List<string> remove = new List<string>(Mathf.Min(needRemove, _viewerTextureByFrame.Count));
            int keptInUse = 0;
            foreach (KeyValuePair<string, Texture2D> kv in _viewerTextureByFrame)
            {
                Texture2D t = kv.Value;
                if (t != null && inUse.Contains(t.GetEntityId()))
                {
                    keptInUse++;
                    continue;
                }

                remove.Add(kv.Key);
                if (remove.Count >= needRemove)
                    break;
            }

            int destroyed = 0;
            if (remove.Count > 0)
                Array.Clear(_viewerHotFramesV374LikeOriginal, 0, _viewerHotFramesV374LikeOriginal.Length);
            for (int i = 0; i < remove.Count; i++)
            {
                string key = remove[i];
                Texture2D t;
                if (_viewerTextureByFrame.TryGetValue(key, out t))
                {
                    if (DestroyTransientUnitTextureLikeOriginal(t, null))
                        destroyed++;
                }

                _viewerTextureByFrame.Remove(key);
                _viewerTextureNationByFrameLikeOriginal.Remove(key);
                _viewerTextureBytesByFrameLikeOriginal.Remove(key);
                _viewerTextureUvByFrameLikeOriginal.Remove(key);
                _viewerFrameMetaByKeyLikeOriginal.Remove(key);
            }

            RebuildViewerTextureStatsLikeOriginal();
            C2RuntimeDiagnosticsV1.MarkPerfEvent(
                "UNIT_TEXTURE_CACHE_PRUNE",
                "reason=" + (reason ?? string.Empty) +
                " removed=" + remove.Count.ToString(CultureInfo.InvariantCulture) +
                " destroyed=" + destroyed.ToString(CultureInfo.InvariantCulture) +
                " keptInUse=" + keptInUse.ToString(CultureInfo.InvariantCulture) +
                " cache=" + _viewerTextureByFrame.Count.ToString(CultureInfo.InvariantCulture) +
                " approxMB=" + (_viewerTextureApproxBytesLikeOriginal / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture));
        }

        private HashSet<EntityId> BuildViewerTextureInUseSetLikeOriginal()
        {
            HashSet<EntityId> ids = new HashSet<EntityId>();
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (u == null || u.LastTexture == null)
                    continue;

                ids.Add(u.LastTexture.GetEntityId());
            }
            return ids;
        }

        private void ClearViewerTextureCacheLikeOriginal(string reason, bool destroyTextures)
        {
            Array.Clear(_viewerHotFramesV374LikeOriginal, 0, _viewerHotFramesV374LikeOriginal.Length);
            int entries = _viewerTextureByFrame.Count;
            int destroyed = 0;
            if (destroyTextures && entries > 0)
            {
                HashSet<EntityId> destroyedIds = new HashSet<EntityId>();
                foreach (KeyValuePair<string, Texture2D> kv in _viewerTextureByFrame)
                {
                    Texture2D tex = kv.Value;
                    if (tex == null)
                        continue;

                    EntityId id = tex.GetEntityId();
                    if (!destroyedIds.Add(id))
                        continue;

                    if (DestroyTransientUnitTextureLikeOriginal(tex, null))
                        destroyed++;
                }
            }

            _viewerTextureByFrame.Clear();
            _viewerTextureNationByFrameLikeOriginal.Clear();
            _viewerTextureBytesByFrameLikeOriginal.Clear();
            _viewerTextureMemoryIdsLikeOriginal.Clear();
            _viewerTextureUvByFrameLikeOriginal.Clear();
            _viewerFrameMetaByKeyLikeOriginal.Clear();
            _viewerReadyFrameByLookupLikeOriginal.Clear();
            if (destroyTextures)
            {
                for (int i = 0; i < _viewerSpriteSurfacesLikeOriginal.Count; i++)
                {
                    Texture2D surface = _viewerSpriteSurfacesLikeOriginal[i] != null
                        ? _viewerSpriteSurfacesLikeOriginal[i].Texture
                        : null;
                    if (surface == null) continue;
                    if (Application.isPlaying) Destroy(surface);
                    else DestroyImmediate(surface);
                }
            }
            _viewerSpriteSurfacesLikeOriginal.Clear();
            _viewerTextureApproxBytesLikeOriginal = 0;
            _viewerTextureNationFramesLikeOriginal = 0;
            _viewerTexturePlainFramesLikeOriginal = 0;

            if (entries > 0)
            {
                C2RuntimeDiagnosticsV1.MarkPerfEvent(
                    "UNIT_TEXTURE_CACHE_CLEAR",
                    "reason=" + (reason ?? string.Empty) +
                    " entries=" + entries.ToString(CultureInfo.InvariantCulture) +
                    " destroyed=" + destroyed.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void RebuildViewerTextureStatsLikeOriginal()
        {
            _viewerTextureApproxBytesLikeOriginal = 0;
            _viewerTextureNationFramesLikeOriginal = 0;
            _viewerTexturePlainFramesLikeOriginal = 0;
            _viewerTextureMemoryIdsLikeOriginal.Clear();
            HashSet<EntityId> countedTextures = new HashSet<EntityId>();
            foreach (KeyValuePair<string, Texture2D> kv in _viewerTextureByFrame)
            {
                Texture2D tex = kv.Value;
                if (tex == null)
                    continue;

                int bytes = Mathf.Max(0, tex.width) * Mathf.Max(0, tex.height) * 4;
                bool firstReference = countedTextures.Add(tex.GetEntityId());
                if (firstReference)
                    _viewerTextureMemoryIdsLikeOriginal.Add(tex.GetEntityId());
                _viewerTextureBytesByFrameLikeOriginal[kv.Key] = firstReference ? bytes : 0;
                if (firstReference)
                    _viewerTextureApproxBytesLikeOriginal += bytes;

                bool nation;
                if (_viewerTextureNationByFrameLikeOriginal.TryGetValue(kv.Key, out nation) && nation)
                    _viewerTextureNationFramesLikeOriginal++;
                else
                    _viewerTexturePlainFramesLikeOriginal++;
            }
        }

        private bool DestroyTransientUnitTextureLikeOriginal(Texture2D tex, Texture2D keep)
        {
            if (tex == null || ReferenceEquals(tex, keep) || ReferenceEquals(tex, Texture2D.whiteTexture))
                return false;

            string n = tex.name ?? string.Empty;
            bool owned =
                n.StartsWith("C2ViewerGpFrame", StringComparison.Ordinal) ||
                n.StartsWith("C2_UNIT_NATCOLOR_EXACT", StringComparison.Ordinal);
            if (!owned)
                return false;

            if (Application.isPlaying)
                Destroy(tex);
            else
                DestroyImmediate(tex);
            return true;
        }

        private static string ShortPerfTextLikeOriginal(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            value = value.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            if (value.Length <= maxLen) return value;
            return value.Substring(0, Math.Max(0, maxLen - 3)) + "...";
        }

        private bool TryGetNationColorOverlaySetLikeOriginal(C2UnitOriginalRuntime u, FrameModel fr, int spriteId, out Texture2D actualTex, out Texture2D maskBlackTex, out Texture2D maskWhiteTex, out string audit)
        {
            actualTex = null;
            maskBlackTex = null;
            maskWhiteTex = null;
            audit = "nation_color_set_not_attempted";
            if (u == null || u.Probe == null || string.IsNullOrWhiteSpace(fr.Package))
            {
                audit = "nation_color_set_missing_unit_or_package";
                return false;
            }

            Color32 actualColor = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(u.Probe.Nation);
            string a1;
            string a2;
            string a3;
            bool okActual = TryGetNationColoredViewerTextureLikeOriginal(u, fr, spriteId, actualColor, out actualTex, out a1);
            bool okBlack = TryGetNationColoredViewerTextureLikeOriginal(u, fr, spriteId, new Color32(0, 0, 0, 255), out maskBlackTex, out a2);
            bool okWhite = TryGetNationColoredViewerTextureLikeOriginal(u, fr, spriteId, new Color32(255, 255, 255, 255), out maskWhiteTex, out a3);

            audit = "actual=[" + (a1 ?? string.Empty) + "] black=[" + (a2 ?? string.Empty) + "] white=[" + (a3 ?? string.Empty) + "]";
            return okActual && okBlack && okWhite && actualTex != null && maskBlackTex != null && maskWhiteTex != null;
        }

        private bool TryGetNationColoredViewerTextureLikeOriginal(C2UnitOriginalRuntime u, FrameModel fr, int spriteId, Color32 nationColor, out Texture2D tex, out string audit)
        {
            tex = null;
            audit = "nation_color_not_attempted";
            if (u == null || string.IsNullOrWhiteSpace(fr.Package))
            {
                audit = "nation_color_missing_unit_or_package";
                return false;
            }

            string absPath;
            string pathAudit;
            if (!TryResolveUnitVisualAbsPathLikeOriginal(fr.Package, u.Probe != null ? u.Probe.MdPath : string.Empty, out absPath, out pathAudit))
            {
                audit = "nation_color_path_miss " + pathAudit;
                return false;
            }

            string ext = Path.GetExtension(absPath).ToLowerInvariant();

            try
            {
                Texture2D loaded = null;
                string source = string.Empty;

                if (string.Equals(ext, ".g2d", StringComparison.OrdinalIgnoreCase))
                {
                    MethodInfo mi = ResolveBattleStaticMethodLikeOriginal("TryLoadG2DFrameViaMelinojaNationColorV1LikeOriginal");
                    if (mi != null)
                    {
                        object[] args = new object[] { absPath, spriteId, nationColor, null };
                        loaded = mi.Invoke(null, args) as Texture2D;
                        source = args.Length > 3 ? (args[3] as string ?? string.Empty) : string.Empty;
                    }
                }
                else
                {
                    MethodInfo mi = ResolveBattleStaticMethodLikeOriginal("TryLoadBuildingGpFrameViaMelinojaNationColorV1LikeOriginal");
                    if (mi != null)
                    {
                        object[] args = new object[] { absPath, spriteId, nationColor, null, fr.Package };
                        loaded = mi.Invoke(null, args) as Texture2D;
                        source = args.Length > 3 ? (args[3] as string ?? string.Empty) : string.Empty;
                    }
                }

                if (loaded == null)
                {
                    audit = "nation_color_decode_failed ext=" + ext + " abs=" + absPath + " source=[" + source + "]";
                    return false;
                }

                tex = loaded;
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                audit = "nation_color_ok ext=" + ext + " abs=" + absPath + " source=[" + source + "]";
                return true;
            }
            catch (Exception ex)
            {
                audit = "nation_color_exception " + ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private bool TryResolveUnitVisualAbsPathLikeOriginal(string logicalPackage, string mdPath, out string absPath, out string audit)
        {
            absPath = null;
            audit = string.Empty;

            string cacheKey = (_dataRoot ?? string.Empty) + "|" + (logicalPackage ?? string.Empty) + "|" + (mdPath ?? string.Empty);
            string cached;
            if (_unitVisualAbsPathCacheLikeOriginal.TryGetValue(cacheKey, out cached))
            {
                if (!string.IsNullOrEmpty(cached) && File.Exists(cached))
                {
                    absPath = cached;
                    audit = "cache_hit";
                    return true;
                }
                audit = "cache_miss";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(logicalPackage) && Path.IsPathRooted(logicalPackage) && File.Exists(logicalPackage))
            {
                absPath = logicalPackage;
                _unitVisualAbsPathCacheLikeOriginal[cacheKey] = absPath;
                audit = "direct_abs";
                return true;
            }

            string[] exts = new[] { ".g2d", ".G2D", ".g16", ".G16", ".g17", ".G17" };
            MethodInfo visualCandidatesMethod = ResolveBattleStaticMethodLikeOriginal("C2Settlement3InuMdV2VisualCandidatesLikeOriginal");
            MethodInfo addIndexedMethod = ResolveBattleStaticMethodLikeOriginal("C2Settlement3InuMdV2AddIndexedVisualCandidatesLikeOriginal");
            List<string> candidates = null;
            try
            {
                if (visualCandidatesMethod != null)
                {
                    object result = visualCandidatesMethod.Invoke(null, new object[] { logicalPackage, mdPath, exts });
                    if (result is List<string>) candidates = (List<string>)result;
                    else if (result is IEnumerable<string>) candidates = new List<string>((IEnumerable<string>)result);
                    else if (result is IList)
                    {
                        candidates = new List<string>();
                        IList list = (IList)result;
                        for (int i = 0; i < list.Count; i++)
                        {
                            string s = list[i] as string;
                            if (!string.IsNullOrEmpty(s)) candidates.Add(s);
                        }
                    }
                    if (candidates != null && addIndexedMethod != null)
                        addIndexedMethod.Invoke(null, new object[] { candidates, logicalPackage, exts });
                }
            }
            catch { candidates = null; }
            if (candidates == null) candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(logicalPackage))
            {
                if (Path.HasExtension(logicalPackage))
                {
                    string p = !string.IsNullOrWhiteSpace(_dataRoot) ? Path.Combine(_dataRoot, logicalPackage) : logicalPackage;
                    candidates.Add(p);
                }
                else if (!string.IsNullOrWhiteSpace(_dataRoot))
                {
                    for (int i = 0; i < exts.Length; i++) candidates.Add(Path.Combine(_dataRoot, logicalPackage + exts[i]));
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                string p = candidates[i];
                if (string.IsNullOrWhiteSpace(p)) continue;
                try
                {
                    if (File.Exists(p))
                    {
                        absPath = p;
                        _unitVisualAbsPathCacheLikeOriginal[cacheKey] = absPath;
                        audit = "resolved:" + Path.GetFileName(p);
                        return true;
                    }
                }
                catch { }
            }

            _unitVisualAbsPathCacheLikeOriginal[cacheKey] = string.Empty;
            audit = "not_found logical=" + (logicalPackage ?? string.Empty);
            return false;
        }

        private static bool TryComposeNationColorMaskOnlyBottomAlignedLikeOriginal(Texture2D baseViewerTex, Texture2D nationActualTex, Texture2D nationMaskBlackTex, Texture2D nationMaskWhiteTex, string package, int spriteId, bool mirrorOverlay, out Texture2D composedTex, out string audit)
        {
            composedTex = null;
            audit = "compose_mask_not_attempted";
            if (baseViewerTex == null || nationActualTex == null || nationMaskBlackTex == null || nationMaskWhiteTex == null)
            {
                audit = "compose_mask_missing_input";
                return false;
            }

            try
            {
                int baseW = baseViewerTex.width;
                int baseH = baseViewerTex.height;
                int overlayW = nationActualTex.width;
                int overlayH = nationActualTex.height;
                if (baseW <= 0 || baseH <= 0 || overlayW <= 0 || overlayH <= 0 ||
                    nationMaskBlackTex.width != overlayW || nationMaskBlackTex.height != overlayH ||
                    nationMaskWhiteTex.width != overlayW || nationMaskWhiteTex.height != overlayH)
                {
                    audit = "compose_mask_bad_dims base=" + baseW.ToString(CultureInfo.InvariantCulture) + "x" + baseH.ToString(CultureInfo.InvariantCulture) +
                            " actual=" + overlayW.ToString(CultureInfo.InvariantCulture) + "x" + overlayH.ToString(CultureInfo.InvariantCulture) +
                            " black=" + nationMaskBlackTex.width.ToString(CultureInfo.InvariantCulture) + "x" + nationMaskBlackTex.height.ToString(CultureInfo.InvariantCulture) +
                            " white=" + nationMaskWhiteTex.width.ToString(CultureInfo.InvariantCulture) + "x" + nationMaskWhiteTex.height.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                Color32[] basePixels = baseViewerTex.GetPixels32();
                Color32[] actualPixels = nationActualTex.GetPixels32();
                Color32[] blackPixels = nationMaskBlackTex.GetPixels32();
                Color32[] whitePixels = nationMaskWhiteTex.GetPixels32();
                if (basePixels == null || actualPixels == null || blackPixels == null || whitePixels == null ||
                    basePixels.Length != baseW * baseH ||
                    actualPixels.Length != overlayW * overlayH ||
                    blackPixels.Length != overlayW * overlayH ||
                    whitePixels.Length != overlayW * overlayH)
                {
                    audit = "compose_mask_pixels_unreadable";
                    return false;
                }

                if (mirrorOverlay)
                {
                    actualPixels = MirrorPixelsXLikeOriginal(actualPixels, overlayW, overlayH);
                    blackPixels = MirrorPixelsXLikeOriginal(blackPixels, overlayW, overlayH);
                    whitePixels = MirrorPixelsXLikeOriginal(whitePixels, overlayW, overlayH);
                }

                RectInt baseBounds;
                RectInt overlayBounds;
                int baseAlphaCount;
                int overlayAlphaCount;
                if (!TryGetAlphaBoundsLikeOriginal(basePixels, baseW, baseH, out baseBounds, out baseAlphaCount))
                {
                    audit = "compose_mask_no_base_alpha";
                    return false;
                }
                if (!TryGetAlphaBoundsLikeOriginal(blackPixels, overlayW, overlayH, out overlayBounds, out overlayAlphaCount))
                {
                    audit = "compose_mask_no_overlay_alpha";
                    return false;
                }

                int baseCenterX = baseBounds.xMin + (baseBounds.width - 1) / 2;
                int overlayCenterX = overlayBounds.xMin + (overlayBounds.width - 1) / 2;
                int baseBottomY = baseBounds.yMin + baseBounds.height - 1;
                int overlayBottomY = overlayBounds.yMin + overlayBounds.height - 1;
                int offsetX = baseCenterX - overlayCenterX;
                int offsetY = baseBottomY - overlayBottomY;

                Color32[] outPixels = new Color32[basePixels.Length];
                Array.Copy(basePixels, outPixels, basePixels.Length);
                int natMaskCount = 0;
                int mappedCount = 0;
                int changedCount = 0;

                for (int oy = 0; oy < overlayH; oy++)
                {
                    int by = oy + offsetY;
                    if (by < 0 || by >= baseH) continue;
                    int overlayRow = oy * overlayW;
                    int baseRow = by * baseW;
                    for (int ox = 0; ox < overlayW; ox++)
                    {
                        int oi = overlayRow + ox;
                        Color32 b0 = blackPixels[oi];
                        Color32 w0 = whitePixels[oi];
                        if (b0.a <= 0 && w0.a <= 0) continue;

                        int maskDiff = Math.Abs((int)b0.r - (int)w0.r) + Math.Abs((int)b0.g - (int)w0.g) + Math.Abs((int)b0.b - (int)w0.b);
                        if (maskDiff < 12) continue; // only pixels whose RGB changes when nation color changes

                        natMaskCount++;
                        int bx = ox + offsetX;
                        if (bx < 0 || bx >= baseW) continue;
                        int bi = baseRow + bx;
                        Color32 bp = basePixels[bi];
                        if (bp.a <= 0) continue;

                        Color32 ap = actualPixels[oi];
                        if (ap.a <= 0) continue;

                        mappedCount++;
                        if (bp.r != ap.r || bp.g != ap.g || bp.b != ap.b)
                        {
                            outPixels[bi] = new Color32(ap.r, ap.g, ap.b, bp.a);
                            changedCount++;
                        }
                    }
                }

                if (changedCount <= 0)
                {
                    audit = "compose_mask_no_effect changed=0 mask=" + natMaskCount.ToString(CultureInfo.InvariantCulture) +
                            " mapped=" + mappedCount.ToString(CultureInfo.InvariantCulture) +
                            " base=" + baseW.ToString(CultureInfo.InvariantCulture) + "x" + baseH.ToString(CultureInfo.InvariantCulture) +
                            " overlay=" + overlayW.ToString(CultureInfo.InvariantCulture) + "x" + overlayH.ToString(CultureInfo.InvariantCulture) +
                            " mirror=" + mirrorOverlay.ToString();
                    return false;
                }

                Texture2D tex = new Texture2D(baseW, baseH, TextureFormat.RGBA32, false, false);
                tex.name = "C2ViewerGpFrameNatMask_" + SanitizeName(Path.GetFileNameWithoutExtension(package ?? string.Empty)) + "_" + spriteId.ToString(CultureInfo.InvariantCulture);
                tex.SetPixels32(outPixels);
                tex.Apply(false, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                composedTex = tex;
                audit = "compose_mask_ok changed=" + changedCount.ToString(CultureInfo.InvariantCulture) +
                        " mask=" + natMaskCount.ToString(CultureInfo.InvariantCulture) +
                        " mapped=" + mappedCount.ToString(CultureInfo.InvariantCulture) +
                        " base=" + baseW.ToString(CultureInfo.InvariantCulture) + "x" + baseH.ToString(CultureInfo.InvariantCulture) +
                        " overlay=" + overlayW.ToString(CultureInfo.InvariantCulture) + "x" + overlayH.ToString(CultureInfo.InvariantCulture) +
                        " mirror=" + mirrorOverlay.ToString() +
                        " offset=" + offsetX.ToString(CultureInfo.InvariantCulture) + "," + offsetY.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                audit = "compose_mask_exception " + ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private static Color32[] MirrorPixelsXLikeOriginal(Color32[] src, int width, int height)
        {
            if (src == null || width <= 0 || height <= 0 || src.Length != width * height) return src;
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                    dst[row + x] = src[row + (width - 1 - x)];
            }
            return dst;
        }

        private static bool TryGetAlphaBoundsLikeOriginal(Color32[] pixels, int width, int height, out RectInt bounds, out int alphaCount)
        {
            bounds = new RectInt(0, 0, 0, 0);
            alphaCount = 0;
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length != width * height)
                return false;

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= 0) continue;
                    alphaCount++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (alphaCount <= 0 || maxX < minX || maxY < minY)
                return false;

            bounds = new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
            return true;
        }

        private static MethodInfo ResolveBattleStaticMethodLikeOriginal(string name)
        {
            return typeof(C2BattleTerrainMode).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static Texture2D CreateUnityTextureFromViewerTopLeftRgbaLikeOriginal(string package, int spriteId, global::TemnyLessViewer.C2RenderedFrame rendered, float alphaBoost)
        {
            if (rendered == null || rendered.Width <= 0 || rendered.Height <= 0 || rendered.Rgba == null) return null;
            int w = rendered.Width;
            int h = rendered.Height;
            if (rendered.Rgba.Length < w * h * 4) return null;

            byte[] unityRgba = new byte[w * h * 4];
            float ab = Mathf.Clamp(alphaBoost, 1.0f, 2.0f);
            for (int y = 0; y < h; y++)
            {
                int srcRow = y * w * 4;
                int dstRow = (h - 1 - y) * w * 4;
                Buffer.BlockCopy(rendered.Rgba, srcRow, unityRgba, dstRow, w * 4);

                // Viewer-like sprite pixels are mostly semi-transparent. Unity blending on terrain makes them too pale,
                // so boost only alpha, not RGB. This keeps colors intact and also restores soft shadows.
                if (ab > 1.0001f)
                {
                    int rowEnd = dstRow + w * 4;
                    for (int i = dstRow + 3; i < rowEnd; i += 4)
                    {
                        int a = unityRgba[i];
                        if (a != 0 && a != 255)
                            unityRgba[i] = (byte)Mathf.Min(255, Mathf.RoundToInt(a * ab));
                    }
                }
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false); // sRGB color texture like normal sprites; not linear data
            tex.name = "C2ViewerGpFrame_" + SanitizeName(Path.GetFileNameWithoutExtension(package ?? string.Empty)) + "_" + spriteId.ToString(CultureInfo.InvariantCulture);
            tex.LoadRawTextureData(unityRgba);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private void ApplyViewerGpFrameToMeshLikeOriginal(C2UnitOriginalRuntime u, global::C2UnitFrameOriginal f, FrameModel fr, DrawSpriteAudit draw)
        {
            if (u == null || f.Texture == null) return;

            float s = Mathf.Max(0.0001f, VisualScale);
            float signedFrameDx = draw.MirrorGeometry ? -fr.Dx : fr.Dx;
            float x0 = (signedFrameDx + f.Dx) * s;
            float x1 = (signedFrameDx + f.Dx + Mathf.Max(1, f.Width)) * s;
            float yTop = -(fr.Dy + f.Dy) * s;
            float yBottom = -(fr.Dy + f.Dy + Mathf.Max(1, f.Height)) * s;
            float zBias = ComputeUnitOriginalZBufferDepthBiasPixelsLikeOriginal(u) * s;

            Vector3[] vertices = u.BodyQuadVerticesLikeOriginal;
            if (vertices == null || vertices.Length != 4)
                vertices = u.BodyQuadVerticesLikeOriginal = new Vector3[4];
            vertices[0] = new Vector3(x0, yBottom, zBias);
            vertices[1] = new Vector3(x1, yBottom, zBias);
            vertices[2] = new Vector3(x1, yTop, zBias);
            vertices[3] = new Vector3(x0, yTop, zBias);

            Rect uv = f.UvRect.width > 0.0f && f.UvRect.height > 0.0f
                ? f.UvRect
                : new Rect(0f, 0f, 1f, 1f);
            Vector2[] uvs = u.BodyQuadUvsLikeOriginal;
            if (uvs == null || uvs.Length != 4)
                uvs = u.BodyQuadUvsLikeOriginal = new Vector2[4];
            uvs[0] = new Vector2(uv.xMin, uv.yMin);
            uvs[1] = new Vector2(uv.xMax, uv.yMin);
            uvs[2] = new Vector2(uv.xMax, uv.yMax);
            uvs[3] = new Vector2(uv.xMin, uv.yMax);

            // MiniMap4X.cpp::DrawUnits submits the current GPS frame directly to
            // shared sprite surfaces.  Once the equivalent batched path is active,
            // these four cached vertices/UVs are all the batch builder needs.
            // Rebuilding a disabled per-unit Unity Mesh here made every animation
            // frame pay Mesh.Clear/vertices/triangles/RecalculateBounds thousands
            // of times, even though those individual renderers are never drawn.
            if (UseOriginalGpsUnitBatchRendererLikeOriginal)
            {
                u.RenderedByOriginalGpsBatchLikeOriginal = true;
                return;
            }

            if (u.Mesh != null)
            {
                u.Mesh.Clear(false);
                u.Mesh.vertices = vertices;
                u.Mesh.uv = uvs;
                u.Mesh.triangles = C2UnitOriginalRuntime.BodyQuadTrianglesLikeOriginal;
                u.BodyQuadTrianglesAssignedLikeOriginal = true;
                u.Mesh.RecalculateBounds();
            }

            if (u.MeshRenderer != null)
            {
                u.Material = GetUnitBodyMaterialForTextureLikeOriginal(f.Texture);
                u.MeshRenderer.sharedMaterial = u.Material;
            }
            UpdateSelectionBrightnessPulseLikeOriginal(u);

            if (u.DepthMeshFilter != null)
                u.DepthMeshFilter.sharedMesh = u.Mesh;
            if (u.DepthMeshRenderer != null)
            {
                u.DepthMeshRenderer.sortingOrder = u.MeshRenderer != null ? u.MeshRenderer.sortingOrder : u.DepthMeshRenderer.sortingOrder;
                u.DepthMaterial = GetUnitDepthMaterialForTextureLikeOriginal(f.Texture);
                u.DepthMeshRenderer.sharedMaterial = u.DepthMaterial;
            }

            ApplyViewerLikePixelPerfectTransformLikeOriginal(u);
        }

        private Vector3 CalculateSelectionRingWorldPositionLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return Vector3.zero;

            float px = Mathf.Max(0.0001f, GetFixedPixelWorldScaleForUnitLikeOriginal(u));
            Vector3 p = u.WorldPosition;
            p.x += SelectionRingOffsetX * px;
            p.y += SelectionRingOffsetY;
            p.z += SelectionRingOffsetZ * px;
            return p;
        }

        private float GetFixedPixelWorldScaleForUnitLikeOriginal(C2UnitOriginalRuntime u)
        {
            float px = ResolveUnitNativeVisualPixelWorldScaleV286LikeOriginal(u);
            if (u != null)
                u.FixedPixelWorldScaleLikeOriginal = px;
            return px;
        }

        private float ResolveUnitMapPixelWorldScaleV286LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u != null && u.Info != null && u.Info.MapPixelToWorld > 0.0001f &&
                !float.IsNaN(u.Info.MapPixelToWorld) && !float.IsInfinity(u.Info.MapPixelToWorld))
                return Mathf.Max(0.0001f, u.Info.MapPixelToWorld);

            return 1.0f;
        }

        private float ResolveUnitNativeVisualPixelWorldScaleV286LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (_unitNativeVisualPixelToWorldScaleV277LikeOriginal > 0.0001f &&
                !float.IsNaN(_unitNativeVisualPixelToWorldScaleV277LikeOriginal) &&
                !float.IsInfinity(_unitNativeVisualPixelToWorldScaleV277LikeOriginal))
                return _unitNativeVisualPixelToWorldScaleV277LikeOriginal;

            float mapScale = ResolveUnitMapPixelWorldScaleV286LikeOriginal(u);
            C2BattleTerrainMode mode = _battle;
            if (mode == null)
                mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();

            if (mode != null)
            {
                _unitNativeVisualPixelToWorldScaleV277LikeOriginal =
                    Mathf.Max(0.0001f, mode.C2OriginalNativeVisualPixelToWorldScaleV277LikeOriginal(mapScale));
                _unitNativeVisualPixelToWorldScaleSourceV277LikeOriginal = "battle_native_visual_locked_once_v286_units_observer_only";
                return _unitNativeVisualPixelToWorldScaleV277LikeOriginal;
            }

            _unitNativeVisualPixelToWorldScaleV277LikeOriginal = Mathf.Max(0.0001f, mapScale);
            _unitNativeVisualPixelToWorldScaleSourceV277LikeOriginal = "fallback_map_pixel_no_battle_v286";
            return _unitNativeVisualPixelToWorldScaleV277LikeOriginal;
        }

        private Camera FindUnitScaleReferenceCameraLikeOriginal()
        {
            try
            {
                if (_battle == null)
                    _battle = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();

                if (_battle != null)
                {
                    Camera strict = _battle.GetStrictIsoCameraLikeOriginal();
                    if (strict != null)
                        return strict;
                }

                Camera[] cams = Resources.FindObjectsOfTypeAll<Camera>();
                for (int i = 0; i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (c == null) continue;
                    if (c.gameObject == null || !c.gameObject.scene.IsValid()) continue;
                    string n = c.name ?? string.Empty;
                    if (string.Equals(n, "C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase))
                        return c;
                }

                for (int i = 0; i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (c == null) continue;
                    if (c.gameObject == null || !c.gameObject.scene.IsValid()) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("SpriteDepth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Free", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("BattleTerrainCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                }
            }
            catch { }

            return FindBattleCameraLikeOriginal();
        }

        private void ApplyViewerLikePixelPerfectTransformLikeOriginal(C2UnitOriginalRuntime u)
        {
            Camera scaleCam = FindUnitScaleReferenceCameraLikeOriginal();
            Camera transformCam = scaleCam != null ? scaleCam : FindBattleCameraLikeOriginal();
            ApplyViewerLikePixelPerfectTransformLikeOriginal(u, transformCam,
                transformCam != null ? transformCam.transform.rotation : Quaternion.identity);
        }

        private void ApplyViewerLikePixelPerfectTransformLikeOriginal(
            C2UnitOriginalRuntime u, Camera transformCam, Quaternion cameraRotation)
        {
            if (!ViewerLikePixelPerfectCameraPlane || u == null) return;

            float worldUnitsPerPixel = ResolveUnitNativeVisualPixelWorldScaleV286LikeOriginal(u);
            float mapUnitsPerPixel = ResolveUnitMapPixelWorldScaleV286LikeOriginal(u);
            u.FixedPixelWorldScaleLikeOriginal = worldUnitsPerPixel;
            u.WorldScaleLikeOriginal = Vector3.one * worldUnitsPerPixel;
            if (transformCam != null)
                u.WorldRotationLikeOriginal = cameraRotation;

            Transform tr = u.Root != null ? u.Root.transform : null;
            if (tr != null && transformCam != null)
            {
                Quaternion wantedRotation = cameraRotation;
                if (Quaternion.Angle(tr.rotation, wantedRotation) > 0.001f)
                    tr.rotation = wantedRotation;
            }

            Transform parent = tr != null ? tr.parent : null;
            if (tr != null && parent != null)
            {
                Vector3 ps = parent.lossyScale;
                Vector3 wantedScale = new Vector3(
                    worldUnitsPerPixel / Mathf.Max(0.0001f, Mathf.Abs(ps.x)),
                    worldUnitsPerPixel / Mathf.Max(0.0001f, Mathf.Abs(ps.y)),
                    worldUnitsPerPixel / Mathf.Max(0.0001f, Mathf.Abs(ps.z)));
                if ((tr.localScale - wantedScale).sqrMagnitude > 0.00000001f)
                    tr.localScale = wantedScale;
            }
            else if (tr != null)
            {
                Vector3 wantedScale = Vector3.one * worldUnitsPerPixel;
                if ((tr.localScale - wantedScale).sqrMagnitude > 0.00000001f)
                    tr.localScale = wantedScale;
            }

            if (LogViewerLikePixelFixOnce && _pixelFixLogs < 12 &&
                u.BodyQuadVerticesLikeOriginal != null && u.BodyQuadVerticesLikeOriginal.Length == 4)
            {
                _pixelFixLogs++;
                Rect sr = default(Rect);
                Vector2 anchor = Vector2.zero;
                bool screenOk = transformCam != null && TryGetRuntimeScreenRectLikeOriginal(u, transformCam, out sr, out anchor);
                Debug.Log(LogPrefix + " PIXEL_FIX_V286_UNIT_LOCKED unit='" + (u.Probe != null ? u.Probe.MonsterId : "") + "'" +
                          " nominalCam='" + (transformCam != null ? transformCam.name : "<none>") + "'" +
                          " nativeVisualWorldUnitsPerPixel=" + worldUnitsPerPixel.ToString("0.######", CultureInfo.InvariantCulture) +
                          " mapWorldUnitsPerPixel=" + mapUnitsPerPixel.ToString("0.######", CultureInfo.InvariantCulture) +
                          " fixedWorldUnitsPerPixel=" + u.FixedPixelWorldScaleLikeOriginal.ToString("0.######", CultureInfo.InvariantCulture) +
                          " source=" + _unitNativeVisualPixelToWorldScaleSourceV277LikeOriginal +
                          " cameraScale=observer_only_unit_size_locked" +
                          " rootScale=" + (tr != null ? tr.localScale : u.WorldScaleLikeOriginal).ToString("F6") +
                          " screenOk=" + screenOk +
                          (screenOk ? " screenRect=" + sr.width.ToString("0.###", CultureInfo.InvariantCulture) + "x" + sr.height.ToString("0.###", CultureInfo.InvariantCulture) : ""));
            }
        }


        private void HandleBuildingRallyPointInputV155LikeOriginal()
        {
            if (!IsRightMousePressedThisFrameLikeOriginal()) return;

            // Original UI command priority:
            // RMB on a HUD produce card cancels production and must not leak into the map as DstX/DstY rally.
            // Otherwise the animated Interf3\exitpoint marker jumps under the card.
            if (C2BuildingProductionCardsRuntimeV114.ShouldSuppressMapSelectionFromHudClickV126LikeOriginal())
                return;
            if (IsPointerOverHudUiV155LikeOriginal())
                return;

            C2SettlementBuildingSelectableV1LikeOriginal[] selected = FindSelectedBuildingsForRallyV155LikeOriginal();
            if (selected == null || selected.Length == 0) return;

            Camera cam = FindBattleCameraLikeOriginal();
            if (cam == null) return;

            Vector2 mouse = GetMousePositionLikeOriginal();
            float planeY = 0.0f;
            int planeCount = 0;
            C2BattleTerrainMode mode = _battle;

            for (int i = 0; i < selected.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = selected[i];
                if (b == null || !b.isActiveAndEnabled || !b.IsSelected) continue;
                planeCount++;
                if (mode == null && b.OwnerMode != null) mode = b.OwnerMode;
            }

            if (planeCount <= 0) return;

            if (mode == null) mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode == null) return;

            Ray ray = cam.ScreenPointToRay(new Vector3(mouse.x, mouse.y, 0.0f));
            Plane plane = new Plane(Vector3.up, new Vector3(0.0f, planeY, 0.0f));
            float enter;
            if (!plane.Raycast(ray, out enter)) return;

            Vector3 world = ray.GetPoint(enter);
            float originalX;
            float originalY;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(world, out originalX, out originalY))
                return;

            // V255:
            // The click ray initially intersects a flat helper plane. On terrain with height, projecting the
            // resulting OriginalXY back to the main ISO camera can land a few pixels away from the cursor.
            // Refine OriginalXY so the animated Interf3\exitpoint marker center is exactly under the RMB tip.
            RefineRallyOriginalUnderMouseV255LikeOriginal(mode, cam, mouse, ref originalX, ref originalY);

            int realX = Mathf.RoundToInt(originalX * 16.0f);
            int realY = Mathf.RoundToInt(originalY * 16.0f);

            int applied = 0;
            for (int i = 0; i < selected.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = selected[i];
                if (b == null || !b.isActiveAndEnabled || !b.IsSelected) continue;
                b.SetRallyPointV155LikeOriginal(realX, realY, "unit_original_runtime_rmb_terrain_v255_screen_refined");
                applied++;
            }

            if (applied > 0 && LogBuildingRallyPointV155LikeOriginal)
            {
                Debug.Log(LogPrefix + " RALLY_SET source=selected_building_rmb rule=original_OB_DstX_DstY marker=Interf3\\\\exitpoint applied=" +
                          applied.ToString(CultureInfo.InvariantCulture) +
                          " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " originalPix=(" + originalX.ToString("0.0", CultureInfo.InvariantCulture) + "," + originalY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                          " screen=(" + mouse.x.ToString("0", CultureInfo.InvariantCulture) + "," + mouse.y.ToString("0", CultureInfo.InvariantCulture) + ")");
            }
        }


        private static void RefineRallyOriginalUnderMouseV255LikeOriginal(
            C2BattleTerrainMode mode,
            Camera cam,
            Vector2 mouse,
            ref float originalX,
            ref float originalY)
        {
            if (mode == null || cam == null)
                return;

            const float step = 16.0f;
            for (int iter = 0; iter < 4; iter++)
            {
                Vector3 w0 = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(originalX, originalY);
                Vector3 s0 = cam.WorldToScreenPoint(w0);
                if (s0.z <= 0.0f)
                    return;

                float ex = mouse.x - s0.x;
                float ey = mouse.y - s0.y;
                if (Mathf.Abs(ex) + Mathf.Abs(ey) < 0.35f)
                    return;

                Vector3 wx = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(originalX + step, originalY);
                Vector3 wy = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(originalX, originalY + step);
                Vector3 sx = cam.WorldToScreenPoint(wx);
                Vector3 sy = cam.WorldToScreenPoint(wy);

                float ax = (sx.x - s0.x) / step;
                float ay = (sx.y - s0.y) / step;
                float bx = (sy.x - s0.x) / step;
                float by = (sy.y - s0.y) / step;
                float det = ax * by - bx * ay;
                if (Mathf.Abs(det) < 0.000001f)
                    return;

                float dx = (ex * by - bx * ey) / det;
                float dy = (ax * ey - ex * ay) / det;
                if (float.IsNaN(dx) || float.IsInfinity(dx) || float.IsNaN(dy) || float.IsInfinity(dy))
                    return;

                originalX += Mathf.Clamp(dx, -256.0f, 256.0f);
                originalY += Mathf.Clamp(dy, -256.0f, 256.0f);
            }
        }

        private static C2SettlementBuildingSelectableV1LikeOriginal[] FindSelectedBuildingsForRallyV155LikeOriginal()
        {
            List<C2SettlementBuildingSelectableV1LikeOriginal> selected = new List<C2SettlementBuildingSelectableV1LikeOriginal>(8);

            C2SettlementBuildingSelectableV1LikeOriginal direct = C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal;
            if (direct != null && direct.isActiveAndEnabled && direct.IsSelected)
                selected.Add(direct);

            C2SettlementBuildingSelectableV1LikeOriginal[] all = UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null || !b.isActiveAndEnabled || !b.IsSelected) continue;
                if (!selected.Contains(b)) selected.Add(b);
            }

            return selected.ToArray();
        }

        private static bool IsPointerOverHudUiV155LikeOriginal()
        {
            try
            {
                return UnityEngine.EventSystems.EventSystem.current != null &&
                       UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRightMousePressedThisFrameLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetMouseButtonDown(1)) return true;
            }
            catch { }
#endif
            return false;
        }

        private static Vector2 GetMousePositionLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                Vector3 p = Input.mousePosition;
                return new Vector2(p.x, p.y);
            }
            catch { }
#endif
            return Vector2.zero;
        }

        private void HandleSelectionInputLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            Vector2 mouse = Mouse.current.position.ReadValue();
#else
            return;
#endif
            Camera cam = FindBattleCameraLikeOriginal();
            if (cam == null) return;

            C2UnitOriginalRuntime best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (u == null || !u.ActiveLikeOriginal || u.BodyQuadVerticesLikeOriginal == null ||
                    u.BodyQuadVerticesLikeOriginal.Length != 4) continue;
                if (u.State == C2UnitOriginalState.Death) continue;
                float score;
                if (MouseHitsUnitMeshAabbLikeOriginal(cam, u, mouse, out score))
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = u;
                    }
                }
            }

            if (best != null)
                SelectUnitLikeOriginal(best, "mouse");
        }

        private bool MouseHitsUnitMeshAabbLikeOriginal(Camera cam, C2UnitOriginalRuntime u, Vector2 mouse, out float score)
        {
            score = float.MaxValue;
            Rect rect;
            Vector2 anchor;
            if (!TryGetRuntimeScreenRectLikeOriginal(u, cam, out rect, out anchor)) return false;

            float pad = Mathf.Max(0f, PickPaddingPixels);
            rect.xMin -= pad;
            rect.xMax += pad;
            rect.yMin -= pad;
            rect.yMax += pad;
            if (!rect.Contains(mouse, true)) return false;

            score = (mouse - rect.center).sqrMagnitude;
            return true;
        }

        internal readonly struct UnitScreenProjectionV376LikeOriginal
        {
            private readonly Matrix4x4 _view;
            private readonly Matrix4x4 _viewProjection;
            private readonly float _pixelX, _pixelY, _halfWidth, _halfHeight;

            internal UnitScreenProjectionV376LikeOriginal(Camera camera)
            {
                _view = camera.worldToCameraMatrix;
                _viewProjection = camera.projectionMatrix * _view;
                Rect pixels = camera.pixelRect;
                _pixelX = pixels.x;
                _pixelY = pixels.y;
                _halfWidth = 0.5f * pixels.width;
                _halfHeight = 0.5f * pixels.height;
            }

            internal Vector3 Project(Vector3 world)
            {
                float clipX = _viewProjection.m00 * world.x + _viewProjection.m01 * world.y + _viewProjection.m02 * world.z + _viewProjection.m03;
                float clipY = _viewProjection.m10 * world.x + _viewProjection.m11 * world.y + _viewProjection.m12 * world.z + _viewProjection.m13;
                float clipW = _viewProjection.m30 * world.x + _viewProjection.m31 * world.y + _viewProjection.m32 * world.z + _viewProjection.m33;
                float inverseW = clipW > 0.0000001f || clipW < -0.0000001f ? 1f / clipW : 0f;
                return new Vector3(
                    _pixelX + (clipX * inverseW + 1f) * _halfWidth,
                    _pixelY + (clipY * inverseW + 1f) * _halfHeight,
                    -(_view.m20 * world.x + _view.m21 * world.y + _view.m22 * world.z + _view.m23));
            }

            internal bool ProjectQuad(in Matrix4x4 localToWorld, Vector3[] vertices, out Rect rect, out Vector2 anchor)
            {
                // Combine transforms once; do not project each corner through a
                // separate local->world->clip call chain. No screen-Y inversion.
                Matrix4x4 clip = _viewProjection * localToWorld;
                float depthX = -(_view.m20 * localToWorld.m00 + _view.m21 * localToWorld.m10 + _view.m22 * localToWorld.m20);
                float depthY = -(_view.m20 * localToWorld.m01 + _view.m21 * localToWorld.m11 + _view.m22 * localToWorld.m21);
                float depthZ = -(_view.m20 * localToWorld.m02 + _view.m21 * localToWorld.m12 + _view.m22 * localToWorld.m22);
                float depthOffset = -(_view.m20 * localToWorld.m03 + _view.m21 * localToWorld.m13 + _view.m22 * localToWorld.m23 + _view.m23);
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 vertex = vertices[i];
                    if (depthX * vertex.x + depthY * vertex.y + depthZ * vertex.z + depthOffset < 0f) continue;
                    float w = clip.m30 * vertex.x + clip.m31 * vertex.y + clip.m32 * vertex.z + clip.m33;
                    float iw = w > 0.0000001f || w < -0.0000001f ? 1f / w : 0f;
                    float x = _pixelX + ((clip.m00 * vertex.x + clip.m01 * vertex.y + clip.m02 * vertex.z + clip.m03) * iw + 1f) * _halfWidth;
                    float y = _pixelY + ((clip.m10 * vertex.x + clip.m11 * vertex.y + clip.m12 * vertex.z + clip.m13) * iw + 1f) * _halfHeight;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
                rect = default;
                anchor = default;
                if (minX == float.MaxValue) return false;
                rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
                float anchorIw = clip.m33 > 0.0000001f || clip.m33 < -0.0000001f ? 1f / clip.m33 : 0f;
                anchor = new Vector2(_pixelX + (clip.m03 * anchorIw + 1f) * _halfWidth,
                    _pixelY + (clip.m13 * anchorIw + 1f) * _halfHeight);
                return maxX - minX > 0.5f && maxY - minY > 0.5f;
            }
        }

        // Broad phase only: exact native projection + alpha picking still decides
        // actual cursor hits. The camera matrices are read once per query.
        internal bool TryGetRuntimeScreenRectProjectedV376LikeOriginal(
            C2UnitOriginalRuntime u, in UnitScreenProjectionV376LikeOriginal projection,
            out Rect rect, out Vector2 anchor)
        {
            rect = default(Rect);
            anchor = Vector2.zero;
            if (u == null || !u.ActiveLikeOriginal) return false;
            if (u.OriginalComplexObjectV430LikeOriginal != null) return TryComplexScreenRectV437(u,in projection,out rect,out anchor);
            Vector3[] verts = u.BodyQuadVerticesLikeOriginal;
            if (verts == null || verts.Length == 0) return false;
            Matrix4x4 unitToWorld = GetUnitLocalToWorldMatrixLikeOriginal(u);
            return projection.ProjectQuad(in unitToWorld, verts, out rect, out anchor);
        }

        internal bool TryGetRuntimeScreenRectLikeOriginal(C2UnitOriginalRuntime u, Camera cam, out Rect rect, out Vector2 anchor)
        {
            rect = default(Rect);
            anchor = Vector2.zero;
            if (cam == null || u == null || !u.ActiveLikeOriginal) return false;
            if (u.OriginalComplexObjectV430LikeOriginal != null)
            {
                var projection = new UnitScreenProjectionV376LikeOriginal(cam);
                return TryComplexScreenRectV437(u,in projection,out rect,out anchor);
            }
            Vector3[] verts = u.BodyQuadVerticesLikeOriginal;
            if (verts == null || verts.Length == 0) return false;
            Matrix4x4 unitToWorld = GetUnitLocalToWorldMatrixLikeOriginal(u);

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = unitToWorld.MultiplyPoint3x4(verts[i]);
                Vector3 sp = cam.WorldToScreenPoint(wp);
                if (sp.z < 0f) continue;
                if (sp.x < minX) minX = sp.x;
                if (sp.y < minY) minY = sp.y;
                if (sp.x > maxX) maxX = sp.x;
                if (sp.y > maxY) maxY = sp.y;
            }
            if (minX == float.MaxValue) return false;

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            Vector3 foot = cam.WorldToScreenPoint(u.WorldPosition);
            anchor = new Vector2(foot.x, foot.y);
            return rect.width > 0.5f && rect.height > 0.5f;
        }

        private void SelectUnitLikeOriginal(C2UnitOriginalRuntime u, string source)
        {
            if (_selected == u) return;
            if (_selected != null)
            {
                _selected.Selected = false;
                if (_selected.Info != null) _selected.Info.SetSelectedFromRuntimeLikeOriginal(false);
            }
            _selected = u;
            if (_selected != null)
            {
                _selected.Selected = true;
                if (_selected.Info != null) _selected.Info.SetSelectedFromRuntimeLikeOriginal(true);
            }

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; buildings != null && i < buildings.Length; i++)
                if (buildings[i] != null && buildings[i].isActiveAndEnabled) buildings[i].SetSelected(false);
            C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal = null;
            C2GameplayHudV1.ForceRefreshLikeOriginal();

            for (int i = 0; i < _units.Count; i++) UpdateSelectionRingLikeOriginal(_units[i]);

            if (LogSelection && u != null)
            {
                AnimModel a = CurrentAnim(u);
                Debug.Log(LogPrefix + " SELECT source=" + source +
                          " unit='" + u.Probe.MonsterId + "'" +
                          " md='" + (u.Md != null ? u.Md.Name : "") + "'" +
                          " index=" + u.Probe.Index.ToString(CultureInfo.InvariantCulture) +
                          " nation=" + u.Probe.Nation.ToString(CultureInfo.InvariantCulture) +
                          " real=(" + u.Probe.RealX.ToString(CultureInfo.InvariantCulture) + "," + u.Probe.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " realDir=" + u.RealDirPrecise.ToString(CultureInfo.InvariantCulture) +
                          " state=" + u.State +
                          " anim='" + (a != null ? a.Name : "") + "'" +
                          " frameLong=" + u.CurrentFrameLong.ToString(CultureInfo.InvariantCulture) +
                          " frameFinished=" + u.FrameFinishedLikeOriginal);
            }
        }

        internal void SetRuntimeSelectionFromInfoLikeOriginal(C2UnitOriginalRuntime u, bool selected)
        {
            if (u == null) return;
            if (selected && u.State == C2UnitOriginalState.Death) return;
            if (selected)
            {
                u.Selected = true;
                _selected = u;
                if (u.Info != null) u.Info.SetSelectedFromRuntimeLikeOriginal(true);
                UpdateSelectionBrightnessPulseLikeOriginal(u);
                UpdateSelectionRingLikeOriginal(u);
                C2GameplayHudV1.ForceRefreshLikeOriginal();
                return;
            }

            if (_selected == u) _selected = null;
            u.Selected = false;
            if (u.Info != null) u.Info.SetSelectedFromRuntimeLikeOriginal(false);
            UpdateSelectionBrightnessPulseLikeOriginal(u);
            UpdateSelectionRingLikeOriginal(u);
            C2GameplayHudV1.ForceRefreshLikeOriginal();
        }

        private void CreateSelectionRingLikeOriginal(C2UnitOriginalRuntime u, int visibleLayer)
        {
            if (u == null) return;

            GameObject ringGo = new GameObject("C2UnitOriginal_SelectionRing_round3");
            ringGo.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : transform, true);
            ringGo.transform.position = CalculateSelectionRingWorldPositionLikeOriginal(u);

            Mesh mesh = new Mesh();
            mesh.name = ringGo.name + "_Mesh";
            MeshFilter mf = ringGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = ringGo.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sharedMaterial = GetSelectionMaterialLikeOriginal();
            ringGo.layer = visibleLayer;

            u.SelectionRingObject = ringGo;
            u.SelectionRingFilter = mf;
            u.SelectionRingRenderer = mr;
            u.SelectionRingMesh = mesh;

            RebuildSelectionRingMeshLikeOriginal(u);
            UpdateSelectionRingLikeOriginal(u);
        }

        private void RebuildSelectionRingMeshLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.SelectionRingMesh == null) return;

            float px = Mathf.Max(0.0001f, GetFixedPixelWorldScaleForUnitLikeOriginal(u));
            float w = Mathf.Max(px, SelectionRingWidth * Mathf.Max(0.01f, SelectionRoundScale) * px);
            float h = Mathf.Max(px, SelectionRingHeight * Mathf.Max(0.01f, SelectionRoundScale) * px);
            if (u.SelectionRingMesh.vertexCount == 4 &&
                Mathf.Abs(u.SelectionRingMeshWidthLikeOriginal - w) < 0.0001f &&
                Mathf.Abs(u.SelectionRingMeshHeightLikeOriginal - h) < 0.0001f)
                return;

            u.SelectionRingMeshWidthLikeOriginal = w;
            u.SelectionRingMeshHeightLikeOriginal = h;

            float x0 = -w * 0.5f;
            float x1 =  w * 0.5f;
            float y0 = -h * 0.5f;
            float y1 =  h * 0.5f;

            u.SelectionRingMesh.Clear(false);
            u.SelectionRingMesh.vertices = new[]
            {
                new Vector3(x0, y0, 0f),
                new Vector3(x1, y0, 0f),
                new Vector3(x1, y1, 0f),
                new Vector3(x0, y1, 0f)
            };
            u.SelectionRingMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            u.SelectionRingMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            u.SelectionRingMesh.RecalculateBounds();
        }

        private bool _selectionBrightnessLogged;

        private void RebuildOriginalGpsUnitBatchesLikeOriginal(int renderUnitCountLikeOriginal)
        {
            RebuildSelectionMarkBatchesV420(renderUnitCountLikeOriginal);
            if (!UseOriginalGpsUnitBatchRendererLikeOriginal)
            {
                RestoreIndividualUnitRenderersLikeOriginal();
                return;
            }

            foreach (OriginalGpsUnitBatchLikeOriginal batch in _originalGpsUnitBatchByTextureLikeOriginal.Values)
                if (batch != null) batch.BeginFrameLikeOriginal();

            int submitted = 0;
            for (int i = 0; i < renderUnitCountLikeOriginal; i++)
            {
                C2UnitOriginalRuntime u = UseOriginalDrawUnitsCellVisibilityLikeOriginal
                    ? _drawUnitsCurrentLikeOriginal[i]
                    : _units[i];
                if (u == null || !u.ActiveLikeOriginal || u.HiddenInsideBuildingLikeOriginal ||
                    u.LastTexture == null || u.BodyQuadVerticesLikeOriginal == null ||
                    u.BodyQuadVerticesLikeOriginal.Length != 4 || u.BodyQuadUvsLikeOriginal == null ||
                    u.BodyQuadUvsLikeOriginal.Length != 4)
                    continue;

                if (!u.RenderedByOriginalGpsBatchLikeOriginal)
                {
                    u.RenderedByOriginalGpsBatchLikeOriginal = true;
                    if (u.MeshRenderer != null) u.MeshRenderer.enabled = false;
                    if (u.DepthMeshRenderer != null) u.DepthMeshRenderer.enabled = false;
                }

                OriginalGpsUnitBatchLikeOriginal batch = GetOrCreateOriginalGpsUnitBatchLikeOriginal(u.LastTexture);
                if (batch == null || batch.Root == null) continue;

                if (!batch.Touched)
                {
                    _visualActiveBatchesV373LikeOriginal++;
                    // Batch transform is invariant throughout this synchronous build.
                    // Preserve transformed parent roots, but read the native matrix
                    // once per surface instead of once per sprite.
                    batch.WorldToLocalThisFrameV378LikeOriginal = batch.Root.transform.worldToLocalMatrix;
                }

                int first = batch.Vertices.Count;
                Matrix4x4 unitToBatch = batch.WorldToLocalThisFrameV378LikeOriginal * GetUnitLocalToWorldMatrixLikeOriginal(u);
                float mul = SelectionBrightnessPulseLikeOriginal && u.Selected
                    ? GetOriginalSelectionBrightnessMultiplierLikeOriginal()
                    : 1.0f;
                float fogAlpha = Mathf.Clamp01(u.OriginalFogVisibilityValueLikeOriginal / 255.0f);
                Color diffuse = new Color(mul, mul, mul, fogAlpha);

                for (int v = 0; v < 4; v++)
                {
                    batch.Vertices.Add(unitToBatch.MultiplyPoint3x4(u.BodyQuadVerticesLikeOriginal[v]));
                    batch.Uv.Add(u.BodyQuadUvsLikeOriginal[v]);
                    batch.Colors.Add(diffuse);
                }
                batch.Triangles.Add(first);
                batch.Triangles.Add(first + 2);
                batch.Triangles.Add(first + 1);
                batch.Triangles.Add(first);
                batch.Triangles.Add(first + 3);
                batch.Triangles.Add(first + 2);
                batch.Touched = true;
                submitted++;

                u.LastSelectionBrightnessMultiplierLikeOriginal = mul;
                u.LastAppliedFogVisibilityValueLikeOriginal = u.OriginalFogVisibilityValueLikeOriginal;
            }

            _visualBatchSubmittedV373LikeOriginal = submitted;
            _visualTotalBatchesV373LikeOriginal = _originalGpsUnitBatchByTextureLikeOriginal.Count;

            int activeBatches = 0;
            if (SkipGpsBatchMeshCommitForPerformanceDiagnosisV372)
                return;
            foreach (OriginalGpsUnitBatchLikeOriginal batch in _originalGpsUnitBatchByTextureLikeOriginal.Values)
            {
                if (batch == null || batch.Mesh == null || batch.BodyRenderer == null) continue;
                bool visible = batch.Touched && batch.Vertices.Count > 0;
                batch.BodyRenderer.forceRenderingOff = !visible;
                if (batch.DepthRenderer != null) batch.DepthRenderer.forceRenderingOff = !visible;
                if (!visible) continue;

                batch.Mesh.Clear(false);
                batch.Mesh.SetVertices(batch.Vertices);
                batch.Mesh.SetUVs(0, batch.Uv);
                batch.Mesh.SetColors(batch.Colors);
                batch.Mesh.SetTriangles(batch.Triangles, 0, false);
                batch.Mesh.RecalculateBounds();
                activeBatches++;
            }

            if (!_originalGpsUnitBatchAuditLoggedLikeOriginal && submitted > 0)
            {
                _originalGpsUnitBatchAuditLoggedLikeOriginal = true;
                Debug.Log(LogPrefix + " GPS_UNIT_BATCH_LIKE_ORIGINAL source=MiniMap4X.cpp::DrawUnits->AddAnimation/GPS" +
                          " visibleQuads=" + submitted.ToString(CultureInfo.InvariantCulture) +
                          " sharedSurfaceBatches=" + activeBatches.ToString(CultureInfo.InvariantCulture) +
                          " individualBodyDepthRenderers=not_allocated movementCoordinates=untouched");
            }
        }

        private OriginalGpsUnitBatchLikeOriginal GetOrCreateOriginalGpsUnitBatchLikeOriginal(Texture2D texture)
        {
            if (texture == null) return null;
            EntityId textureId = texture.GetEntityId();
            OriginalGpsUnitBatchLikeOriginal batch;
            if (_originalGpsUnitBatchByTextureLikeOriginal.TryGetValue(textureId, out batch) &&
                batch != null && batch.Root != null && batch.Mesh != null)
                return batch;

            batch = new OriginalGpsUnitBatchLikeOriginal();
            batch.TextureId = textureId;
            batch.Texture = texture;
            batch.Root = new GameObject("C2_GPS_UnitSurfaceBatch_" + textureId.ToString());
            batch.Root.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : transform, false);
            batch.Root.transform.localPosition = Vector3.zero;
            batch.Root.transform.localRotation = Quaternion.identity;
            batch.Root.transform.localScale = Vector3.one;

            Camera camera = FindBattleCameraLikeOriginal();
            int visibleLayer = C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera
                ? C2SpriteDepthLayerLikeOriginal.LayerIndex
                : ResolveLayerVisibleByBattleCameraLikeOriginal(camera);
            batch.Root.layer = visibleLayer;

            batch.Mesh = new Mesh();
            batch.Mesh.name = batch.Root.name + "_Mesh";
            batch.Mesh.indexFormat = IndexFormat.UInt32;
            batch.Mesh.MarkDynamic();
            MeshFilter filter = batch.Root.AddComponent<MeshFilter>();
            filter.sharedMesh = batch.Mesh;

            batch.BodyRenderer = batch.Root.AddComponent<MeshRenderer>();
            batch.BodyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            batch.BodyRenderer.receiveShadows = false;
            batch.BodyRenderer.lightProbeUsage = LightProbeUsage.Off;
            batch.BodyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            batch.BodyRenderer.sortingOrder = SortingOrderBase;
            batch.BodyRenderer.sharedMaterial = GetUnitBodyMaterialForTextureLikeOriginal(texture);

            if (UseUnitBuildingDepthPrepassLikeOriginal || C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera)
            {
                batch.DepthRenderer = C2SpriteDepthPrepassLikeOriginal.AddDepthRendererLikeOriginal(
                    batch.Root,
                    batch.Mesh,
                    GetUnitDepthMaterialForTextureLikeOriginal(texture),
                    SortingOrderBase,
                    "depth_cutout_prepass_batch");
                if (batch.DepthRenderer != null)
                    batch.DepthRenderer.gameObject.layer = visibleLayer;
            }

            _originalGpsUnitBatchByTextureLikeOriginal[textureId] = batch;
            return batch;
        }

        private void RestoreIndividualUnitRenderersLikeOriginal()
        {
            foreach (OriginalGpsUnitBatchLikeOriginal batch in _originalGpsUnitBatchByTextureLikeOriginal.Values)
            {
                if (batch == null) continue;
                if (batch.BodyRenderer != null) batch.BodyRenderer.forceRenderingOff = true;
                if (batch.DepthRenderer != null) batch.DepthRenderer.forceRenderingOff = true;
            }

            Camera camera = FindBattleCameraLikeOriginal();
            int visibleLayer = C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera
                ? C2SpriteDepthLayerLikeOriginal.LayerIndex
                : ResolveLayerVisibleByBattleCameraLikeOriginal(camera);
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (u == null || !u.RenderedByOriginalGpsBatchLikeOriginal) continue;
                u.RenderedByOriginalGpsBatchLikeOriginal = false;
                EnsureIndividualUnitRenderersLikeOriginal(u, visibleLayer);
                if (u.MeshRenderer != null) u.MeshRenderer.enabled = true;
                if (u.DepthMeshRenderer != null) u.DepthMeshRenderer.enabled = true;
                SetUnitForceRenderingOffLikeOriginal(
                    u,
                    u.HiddenInsideBuildingLikeOriginal ||
                    (UseOriginalDrawUnitsCellVisibilityLikeOriginal && !u.VisibleInOriginalDrawUnitsLikeOriginal));
            }
        }

        private void ClearOriginalGpsUnitBatchesLikeOriginal()
        {
            ClearSelectionMarkBatchesV420();
            foreach (OriginalGpsUnitBatchLikeOriginal batch in _originalGpsUnitBatchByTextureLikeOriginal.Values)
            {
                if (batch == null) continue;
                if (batch.Mesh != null) Destroy(batch.Mesh);
            }
            _originalGpsUnitBatchByTextureLikeOriginal.Clear();
            _originalGpsUnitBatchAuditLoggedLikeOriginal = false;
        }

        private float GetOriginalSelectionBrightnessMultiplierLikeOriginal()
        {
            // Original Cossacks II path:
            // ZBuffer.cpp: SelColor = 200 + 50 * sin((GetTickCount() - T0) / 120)
            // MiniMap4X.cpp: selected RGB = RGB * SelColor >> 7.
            float selColor = 200.0f + 50.0f * Mathf.Sin((Time.realtimeSinceStartup * 1000.0f) / 120.0f);

            if (SelectionBrightnessUseRawOriginalDiffuse)
                return Mathf.Clamp(selColor / 128.0f, 1.0f, 2.0f);

            float t = Mathf.Clamp01((selColor - 150.0f) / 100.0f);
            return Mathf.Lerp(1.0f, Mathf.Max(1.0f, SelectionBrightnessMaxMultiplier), t);
        }

        private void UpdateSelectionBrightnessPulseLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return;

            float mul = 1.0f;
            if (SelectionBrightnessPulseLikeOriginal && u.Selected)
                mul = GetOriginalSelectionBrightnessMultiplierLikeOriginal();

            if (Mathf.Abs(u.LastSelectionBrightnessMultiplierLikeOriginal - mul) < 0.001f &&
                u.LastAppliedFogVisibilityValueLikeOriginal == u.OriginalFogVisibilityValueLikeOriginal)
                return;

            float fogAlpha = Mathf.Clamp01(u.OriginalFogVisibilityValueLikeOriginal / 255.0f);
            Color diffuse = new Color(mul, mul, mul, fogAlpha);
            if (u.RenderedByOriginalGpsBatchLikeOriginal)
            {
                // The shared batch carries this value in vertex color.  Do not issue
                // a MaterialPropertyBlock update to a disabled per-unit renderer.
                u.LastSelectionBrightnessMultiplierLikeOriginal = mul;
                u.LastAppliedFogVisibilityValueLikeOriginal = u.OriginalFogVisibilityValueLikeOriginal;
                return;
            }
            if (u.Material == null || u.MeshRenderer == null) return;
            if (u.BodyPropertiesLikeOriginal == null)
                u.BodyPropertiesLikeOriginal = new MaterialPropertyBlock();
            u.MeshRenderer.GetPropertyBlock(u.BodyPropertiesLikeOriginal);
            if (u.Material.HasProperty("_C2Diffuse"))
            {
                if (u.Material.HasProperty("_Color")) u.BodyPropertiesLikeOriginal.SetColor("_Color", Color.white);
                if (u.Material.HasProperty("_BaseColor")) u.BodyPropertiesLikeOriginal.SetColor("_BaseColor", Color.white);
                u.BodyPropertiesLikeOriginal.SetColor("_C2Diffuse", diffuse);
            }
            else
            {
                if (u.Material.HasProperty("_Color")) u.BodyPropertiesLikeOriginal.SetColor("_Color", diffuse);
                if (u.Material.HasProperty("_BaseColor")) u.BodyPropertiesLikeOriginal.SetColor("_BaseColor", diffuse);
            }
            u.MeshRenderer.SetPropertyBlock(u.BodyPropertiesLikeOriginal);

            u.LastSelectionBrightnessMultiplierLikeOriginal = mul;
            u.LastAppliedFogVisibilityValueLikeOriginal = u.OriginalFogVisibilityValueLikeOriginal;

            if (LogSelectionBrightnessOnce && !_selectionBrightnessLogged)
            {
                _selectionBrightnessLogged = true;
                Debug.Log(LogPrefix + " SELECTION_BRIGHTNESS_LIKE_ORIGINAL source=MiniMap4X.cpp/VariateUnitColor selectedUses=ImSelected blinkingFormula='SelColor=200+50*sin(GetTickCount/120); rgb=rgb*SelColor>>7' normalizedToCurrentUnitBaseline=1 maxMultiplier=" +
                          SelectionBrightnessMaxMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                          " rawOriginalDiffuse=" + SelectionBrightnessUseRawOriginalDiffuse);
            }
        }

        private void UpdateSelectionRingLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return;
            if (u.OriginalComplexObjectV430LikeOriginal != null)
            {
                if (u.SelectionRingObject != null) u.SelectionRingObject.SetActive(false);
                if (u.ComplexVisualV437 != null) UpdateComplexSelectionV437(u);
                return;
            }
            if (UseOriginalGpsUnitBatchRendererLikeOriginal && UseBatchedSelectionMarksV420)
            {
                if (u.SelectionRingObject != null && u.SelectionRingObject.activeSelf)
                    u.SelectionRingObject.SetActive(false);
                return; // collected once in RebuildSelectionMarkBatchesV420
            }
            if (u.Selected && u.SelectionRingObject == null)
                CreateSelectionRingLikeOriginal(u, u.VisibleLayerLikeOriginal);
            if (u.SelectionRingObject == null) return;

            if (u.SelectionRingObject.activeSelf != u.Selected)
                u.SelectionRingObject.SetActive(u.Selected);
            if (!u.Selected)
            {
                if (u.SelectionRingRenderer != null) u.SelectionRingRenderer.enabled = false;
                return;
            }

            u.SelectionRingObject.transform.position = CalculateSelectionRingWorldPositionLikeOriginal(u);
            u.SelectionRingObject.transform.rotation = SelectionRingUseGroundPlane
                ? Quaternion.Euler(90f, 0f, 0f)
                : u.WorldRotationLikeOriginal;

            RebuildSelectionRingMeshLikeOriginal(u);

            if (u.SelectionRingRenderer != null)
            {
                bool brigadeSelection =
                    u.Info != null &&
                    C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u.Info);
                u.SelectionRingRenderer.sharedMaterial = brigadeSelection
                    ? GetSelectionBrigadeMaterialLikeOriginal()
                    : GetSelectionMaterialLikeOriginal();
                u.SelectionRingRenderer.enabled = u.Selected;
                // sortingOrder precedes renderQueue for transparent draws.
                // Batched units have no per-unit MeshRenderer: the old fallback
                // put every ring at 6001, AFTER the unit surface batch at 6000.
                // Ground marks must precede every body, not just their owner.
                u.SelectionRingRenderer.sortingOrder = SelectionRingBehindUnit
                    ? short.MinValue
                    : SortingOrderBase + 1;

                Material mat = u.SelectionRingRenderer.sharedMaterial;
                if (mat != null)
                {
                    // Dialogs/SelType.xml -> BrigRound/round4 is a selection mark,
                    // not an overlay over the soldier.  Cossacks II draws the mark
                    // before the unit image.  Queue 4999 placed it after the batched
                    // unit body (3670), producing the white diamond lattice visible
                    // across selected formations.
                    mat.renderQueue = SelectionRingBehindUnit
                        ? UnitBodyRenderQueueLikeOriginal - 1
                        : UnitBodyRenderQueueLikeOriginal + 1;
                    if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
                    if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", SelectionRingThroughTerrain ? 8 : 4);
                    Texture2D tex = brigadeSelection
                        ? GetSelectionBrigadeTextureLikeOriginal()
                        : GetSelectionRoundTextureLikeOriginal();
                    if (tex != null)
                    {
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                        mat.mainTexture = tex;
                    }
                }
            }
        }

        private Material _selectionMaterial;
        private Material _selectionBrigadeMaterial;
        private Texture2D _selectionBrigadeTexture;

        private Material GetSelectionBrigadeMaterialLikeOriginal()
        {
            if (_selectionBrigadeMaterial != null) return _selectionBrigadeMaterial;
            Material baseMaterial = GetSelectionMaterialLikeOriginal();
            _selectionBrigadeMaterial = new Material(baseMaterial);
            _selectionBrigadeMaterial.name = "C2UnitOriginal_SelectionRing_BrigRound_round4_Mat";
            Texture2D tex = GetSelectionBrigadeTextureLikeOriginal();
            if (tex != null)
            {
                if (_selectionBrigadeMaterial.HasProperty("_MainTex")) _selectionBrigadeMaterial.SetTexture("_MainTex", tex);
                if (_selectionBrigadeMaterial.HasProperty("_BaseMap")) _selectionBrigadeMaterial.SetTexture("_BaseMap", tex);
                _selectionBrigadeMaterial.mainTexture = tex;
            }
            return _selectionBrigadeMaterial;
        }

        private Texture2D GetSelectionBrigadeTextureLikeOriginal()
        {
            if (_selectionBrigadeTexture != null) return _selectionBrigadeTexture;
            // Dialogs/SelType.xml: SELTYPE_BRIG BrigRound -> round4.tga.
            _selectionBrigadeTexture = Resources.Load<Texture2D>("textures/selection/round4");
            if (_selectionBrigadeTexture == null) _selectionBrigadeTexture = Resources.Load<Texture2D>("selection/round4");
            if (_selectionBrigadeTexture == null) _selectionBrigadeTexture = Resources.Load<Texture2D>("round4");
            if (_selectionBrigadeTexture != null)
            {
                _selectionBrigadeTexture.filterMode = FilterMode.Point;
                _selectionBrigadeTexture.wrapMode = TextureWrapMode.Clamp;
            }
            return _selectionBrigadeTexture;
        }

        private Material GetSelectionMaterialLikeOriginal()
        {
            if (_selectionMaterial != null) return _selectionMaterial;

            Shader shader = Shader.Find("C2/UnitSelectionRingAlways");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

            _selectionMaterial = new Material(shader);
            _selectionMaterial.name = "C2UnitOriginal_SelectionRing_round3_Mat";
            _selectionMaterial.renderQueue = SelectionRingBehindUnit
                ? UnitBodyRenderQueueLikeOriginal - 1
                : UnitBodyRenderQueueLikeOriginal + 1;

            Texture2D tex = GetSelectionRoundTextureLikeOriginal();
            if (tex != null)
            {
                if (_selectionMaterial.HasProperty("_MainTex")) _selectionMaterial.SetTexture("_MainTex", tex);
                if (_selectionMaterial.HasProperty("_BaseMap")) _selectionMaterial.SetTexture("_BaseMap", tex);
                _selectionMaterial.mainTexture = tex;
            }

            if (_selectionMaterial.HasProperty("_Color")) _selectionMaterial.SetColor("_Color", Color.white);
            if (_selectionMaterial.HasProperty("_BaseColor")) _selectionMaterial.SetColor("_BaseColor", Color.white);
            if (_selectionMaterial.HasProperty("_Cull")) _selectionMaterial.SetInt("_Cull", 0);
            if (_selectionMaterial.HasProperty("_ZWrite")) _selectionMaterial.SetInt("_ZWrite", 0);
            if (_selectionMaterial.HasProperty("_ZTest")) _selectionMaterial.SetInt("_ZTest", SelectionRingThroughTerrain ? 8 : 4);
            return _selectionMaterial;
        }

        private Texture2D GetSelectionRoundTextureLikeOriginal()
        {
            if (_selectionRoundTexture != null) return _selectionRoundTexture;

            string path = string.IsNullOrWhiteSpace(SelectionRingResourcePath) ? "textures/selection/round3" : SelectionRingResourcePath;
            _selectionRoundTexture = Resources.Load<Texture2D>(path);

            if (_selectionRoundTexture == null)
            {
                // Fallbacks only for broken local Resources paths. round3 is the original single-unit ring.
                _selectionRoundTexture = Resources.Load<Texture2D>("textures/selection/round3");
                if (_selectionRoundTexture == null) _selectionRoundTexture = Resources.Load<Texture2D>("selection/round3");
                if (_selectionRoundTexture == null) _selectionRoundTexture = Resources.Load<Texture2D>("round3");
            }

            if (_selectionRoundTexture != null)
            {
                _selectionRoundTexture.filterMode = FilterMode.Point;
                _selectionRoundTexture.wrapMode = TextureWrapMode.Clamp;
                Debug.Log(LogPrefix + " SELECTION_RING_TEXTURE_OK path='" + path + "' size=" + _selectionRoundTexture.width.ToString(CultureInfo.InvariantCulture) + "x" + _selectionRoundTexture.height.ToString(CultureInfo.InvariantCulture) + " defaultQuad=" + SelectionRingWidth.ToString("0.###", CultureInfo.InvariantCulture) + "x" + SelectionRingHeight.ToString("0.###", CultureInfo.InvariantCulture));
            }
            else
            {
                Debug.LogWarning(LogPrefix + " SELECTION_RING_TEXTURE_MISSING path='" + path + "' expected=Resources/textures/selection/round3");
            }

            return _selectionRoundTexture;
        }

        private Material CreateUnitMaterialLikeOriginal(string name)
        {
            Shader shader = Shader.Find("Cossacks2Bridge/C2UnitSpriteV56SelectionDiffuseLikeOriginal");
            if (shader == null && (UnitBodyThroughTerrainLikeOriginal || UseScreenSpriteOverlayDepth))
                shader = Shader.Find("Cossacks2Bridge/C2SelectionPatchV50ThroughTerrainUnderUnits");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material m = new Material(shader);
            m.name = name;
            m.renderQueue = UnitBodyRenderQueueLikeOriginal;
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_C2Diffuse")) m.SetColor("_C2Diffuse", Color.white);
            if (m.HasProperty("_Cull")) m.SetInt("_Cull", 0);
            bool spriteDepthOverlay = C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera;
            bool spriteDepthPrepass = spriteDepthOverlay || UseUnitBuildingDepthPrepassLikeOriginal;
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            int zTest = spriteDepthPrepass
                ? (int)CompareFunction.LessEqual
                : ((UnitBodyThroughTerrainLikeOriginal || UseScreenSpriteOverlayDepth) ? (int)CompareFunction.Always : (int)CompareFunction.LessEqual);
            if (m.HasProperty("_ZTest")) m.SetInt("_ZTest", zTest);
            return m;
        }

        private Material GetUnitBodyMaterialForTextureLikeOriginal(Texture2D texture)
        {
            if (texture == null) texture = Texture2D.whiteTexture;
            EntityId id = texture.GetEntityId();
            Material material;
            if (_unitBodyMaterialByTextureLikeOriginal.TryGetValue(id, out material) && material != null)
                return material;

            material = CreateUnitMaterialLikeOriginal("C2UnitOriginal_SharedBody_" + id.ToString());
            material.mainTexture = texture;
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            _unitBodyMaterialByTextureLikeOriginal[id] = material;
            return material;
        }

        private Material GetUnitDepthMaterialForTextureLikeOriginal(Texture2D texture)
        {
            if (texture == null) texture = Texture2D.whiteTexture;
            EntityId id = texture.GetEntityId();
            Material material;
            if (_unitDepthMaterialByTextureLikeOriginal.TryGetValue(id, out material) && material != null)
                return material;

            material = C2SpriteDepthPrepassLikeOriginal.CreateDepthMaterialLikeOriginal(
                "C2UnitOriginal_SharedDepth_" + id.ToString(),
                texture,
                UnitDepthAlphaCutoffLikeOriginal);
            ConfigureUnitDepthMaterialLikeOriginal(material);
            _unitDepthMaterialByTextureLikeOriginal[id] = material;
            return material;
        }

        private void ClearSharedUnitMaterialsLikeOriginal()
        {
            HashSet<EntityId> destroyed = new HashSet<EntityId>();
            foreach (Material material in _unitBodyMaterialByTextureLikeOriginal.Values)
                if (material != null && destroyed.Add(material.GetEntityId())) Destroy(material);
            foreach (Material material in _unitDepthMaterialByTextureLikeOriginal.Values)
                if (material != null && destroyed.Add(material.GetEntityId())) Destroy(material);
            _unitBodyMaterialByTextureLikeOriginal.Clear();
            _unitDepthMaterialByTextureLikeOriginal.Clear();
        }

        private void UpdateBillboardLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (!BillboardToBattleCamera || u == null) return;
            Camera cam = FindUnitScaleReferenceCameraLikeOriginal();
            if (cam == null) return;
            u.WorldRotationLikeOriginal = cam.transform.rotation;
            if (u.Root != null)
                u.Root.transform.rotation = u.WorldRotationLikeOriginal;
        }

        internal static Matrix4x4 GetUnitLocalToWorldMatrixLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return Matrix4x4.identity;
            Quaternion rotation = u.WorldRotationLikeOriginal;
            Vector3 scale = u.WorldScaleLikeOriginal;
            // TRS's rotation/scale part does not depend on position. Movement
            // only changes its translation column. Use exact comparisons: tiny
            // camera changes must not be swallowed by Unity's approximate ==.
            if (!u.LocalToWorldBasisReadyV379LikeOriginal ||
                !rotation.Equals(u.LocalToWorldRotationV379LikeOriginal) ||
                !scale.Equals(u.LocalToWorldScaleV379LikeOriginal))
            {
                u.LocalToWorldBasisV379LikeOriginal = Matrix4x4.TRS(Vector3.zero, rotation, scale);
                u.LocalToWorldRotationV379LikeOriginal = rotation;
                u.LocalToWorldScaleV379LikeOriginal = scale;
                u.LocalToWorldBasisReadyV379LikeOriginal = true;
            }
            Matrix4x4 matrix = u.LocalToWorldBasisV379LikeOriginal;
            matrix.m03 = u.WorldPosition.x;
            matrix.m13 = u.WorldPosition.y;
            matrix.m23 = u.WorldPosition.z;
            return matrix;
        }

        private AnimModel CurrentAnim(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null || u.CurrentAnimIndex < 0 || u.CurrentAnimIndex >= u.Md.Animations.Count) return null;
            return u.Md.Animations[u.CurrentAnimIndex];
        }

        private static int FixedFrameIndexLikeOriginal(C2UnitOriginalRuntime u, AnimModel anim)
        {
            if (u == null || anim == null || anim.Frames.Count <= 0) return 0;
            int idx = u.CurrentFrameLong >> 8;
            if (idx < 0) idx = 0;
            if (idx >= anim.Frames.Count) idx = anim.Frames.Count - 1;
            return idx;
        }

        private static int ResolveAnimationIndexLikeOriginal(MdModel md, string name)
        {
            if (md == null || string.IsNullOrWhiteSpace(name)) return -1;
            int cached;
            if (md.AnimationIndexCacheLikeOriginal.TryGetValue(name, out cached) &&
                cached >= 0 && cached < md.Animations.Count &&
                string.Equals(md.Animations[cached].Name, name, StringComparison.OrdinalIgnoreCase))
                return cached;
            for (int i = 0; i < md.Animations.Count; i++)
            {
                if (!string.Equals(md.Animations[i].Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                // Only positive hits are cached: MD parsing may ask for a name
                // before a later line appends that animation.
                md.AnimationIndexCacheLikeOriginal[name] = i;
                return i;
            }
            return -1;
        }

        private static void FinalizeOriginalAnimationTableLikeOriginal(MdModel md)
        {
            if (md == null) return;

            // NewMonster::Load stores parsed animations in Animations once and
            // simulation code then uses GetAnimation(anm_*). Build the equivalent
            // direct table only after the complete MD has been parsed.
            md.AnimationIndexCacheLikeOriginal.Clear();
            for (int i = 0; i < md.Animations.Count; i++)
            {
                AnimModel animation = md.Animations[i];
                if (animation == null || string.IsNullOrWhiteSpace(animation.Name)) continue;
                if (!md.AnimationIndexCacheLikeOriginal.ContainsKey(animation.Name))
                    md.AnimationIndexCacheLikeOriginal.Add(animation.Name, i);
            }

            md.MotionLAnimationIndexLikeOriginal = ResolveFinalAnimationIndexLikeOriginal(
                md, "#MOTION_L", "@MOTION_L", "MOTION_L", "#MOTION", "@MOTION", "MOTION");
            for (int state = 0; state < md.PostureMotionLAnimationIndicesV376LikeOriginal.Length; state++)
            {
                string suffix = state.ToString(CultureInfo.InvariantCulture);
                md.PostureMotionLAnimationIndicesV376LikeOriginal[state] = ResolveFinalAnimationIndexLikeOriginal(
                    md, "#MOTION_L" + suffix, "@MOTION_L" + suffix, "MOTION_L" + suffix);
                md.PostureMotionRAnimationIndicesV411LikeOriginal[state] = ResolveFinalAnimationIndexLikeOriginal(
                    md, "#MOTION_R" + suffix, "@MOTION_R" + suffix, "MOTION_R" + suffix);
            }
            md.RotateLAnimationIndexLikeOriginal = ResolveFinalAnimationIndexLikeOriginal(
                md, "#ROTATEL", "@ROTATEL", "ROTATEL");
            md.RotateRAnimationIndexLikeOriginal = ResolveFinalAnimationIndexLikeOriginal(
                md, "#ROTATER", "@ROTATER", "ROTATER");
            md.HaveRotateAnimationsLikeOriginal =
                md.RotateLAnimationIndexLikeOriginal >= 0 &&
                md.RotateLAnimationIndexLikeOriginal < md.Animations.Count &&
                md.Animations[md.RotateLAnimationIndexLikeOriginal] != null &&
                md.Animations[md.RotateLAnimationIndexLikeOriginal].Frames.Count > 0;
        }

        private static int ResolveFinalAnimationIndexLikeOriginal(MdModel md, params string[] names)
        {
            if (md == null || names == null) return -1;
            for (int i = 0; i < names.Length; i++)
            {
                int index;
                if (md.AnimationIndexCacheLikeOriginal.TryGetValue(names[i], out index))
                    return index;
            }
            return -1;
        }

        private void ClearPreviousRuntimeLikeOriginal()
        {
            _initialized = false;
            foreach (OriginalDrawBuildingLikeOriginal building in _drawBuildingsLikeOriginal)
                SetBuildingForceRenderingOffLikeOriginal(building, false);
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime unit = _units[i];
                if (unit != null) { unit.ComplexVisualV437?.Dispose(); unit.ComplexVisualV437 = null; }
                if (unit != null && unit.Info != null)
                    unit.Info.C2ReleaseV365LikeOriginal();
            }
            if (_battle != null) _battle.ClearArtilleryGraphicsV437();
            _units.Clear();
            _unitDrawCellsLikeOriginal.Clear();
            _drawUnitsCurrentLikeOriginal.Clear();
            _drawUnitsEpochV379LikeOriginal = 0;
            _drawUnitsPreviousLikeOriginal.Clear();
            _drawBuildingsLikeOriginal.Clear();
            _buildingDrawCellsLikeOriginal.Clear();
            _drawBuildingsCurrentSetLikeOriginal.Clear();
            _drawBuildingsPreviousSetLikeOriginal.Clear();
            _buildingsEverSeenInFogLikeOriginal.Clear();
            _fogMapLikeOriginal = null;
            _fogMapWorkLikeOriginal = null;
            if (_fogTextureLikeOriginal != null) Destroy(_fogTextureLikeOriginal);
            _fogTextureLikeOriginal = null;
            if (_fogOverlayMaterialLikeOriginal != null) Destroy(_fogOverlayMaterialLikeOriginal);
            if (_fogOverlayMeshLikeOriginal != null) Destroy(_fogOverlayMeshLikeOriginal);
            if (_fogOverlayRootLikeOriginal != null) Destroy(_fogOverlayRootLikeOriginal);
            if (_fogOverlayCameraLikeOriginal != null) Destroy(_fogOverlayCameraLikeOriginal.gameObject);
            _fogOverlayMaterialLikeOriginal = null;
            _fogOverlayMeshLikeOriginal = null;
            _fogOverlayRendererLikeOriginal = null;
            _fogOverlayRootLikeOriginal = null;
            _fogOverlayCameraLikeOriginal = null;
            _fogOverlayVerticesLikeOriginal = null;
            _fogOverlayUvLikeOriginal = null;
            _fogOverlayColorsLikeOriginal = null;
            _fogOverlayLoadFailureLoggedLikeOriginal = false;
            Shader.SetGlobalFloat("_C2FogEnabledLikeOriginal", 0.0f);
            _fogMapSideLikeOriginal = 0;
            _fogInitializedLikeOriginal = false;
            _nextBuildingDrawRefreshAtLikeOriginal = 0.0f;
            _originalDrawUnitsAuditLoggedLikeOriginal = false;
            _selected = null;
            ClearOriginalGpsUnitBatchesLikeOriginal();
            if (_runtimeRoot != null) Destroy(_runtimeRoot);
            _runtimeRoot = null;
            _createdLogs = 0;
            _firstFrameLogs = 0;
            _deathLogs = 0;
            _workLogs = 0;
            _viewerGpIdByPackage.Clear();
            _viewerPackageKeyByPackage.Clear();
            _viewerTextureStringKeyCacheLikeOriginal.Clear();
            ClearSharedUnitMaterialsLikeOriginal();
            ClearViewerTextureCacheLikeOriginal("runtime_clear", true);
            _unitVisualAbsPathCacheLikeOriginal.Clear();
            _viewerTextureMemoryLogsLikeOriginal = 0;
            _viewerTextureCreateEventsLikeOriginal = 0;
            _nextViewerTexturePerfEventAtLikeOriginal = 0.0f;
            _viewerGps.Reset();
            _simulationAccumulatorLikeOriginal = 0.0;
        }

        private int ResolveLayerVisibleByBattleCameraLikeOriginal(Camera cam)
        {
            if (cam != null)
            {
                int mask = cam.cullingMask;
                if ((mask & 1) != 0) return 0;
                for (int layer = 0; layer < 32; layer++) if ((mask & (1 << layer)) != 0) return layer;
            }
            return 0;
        }

        private static void SetLayerRecursiveLikeOriginal(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++) SetLayerRecursiveLikeOriginal(go.transform.GetChild(i).gameObject, layer);
        }

        private Camera FindBattleCameraLikeOriginal()
        {
            if (_cachedBattleCameraLikeOriginal != null &&
                _cachedBattleCameraLikeOriginal.isActiveAndEnabled)
                return _cachedBattleCameraLikeOriginal;

            Camera fallback = null;
            try
            {
                Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>();

                // Observer/free debug cameras must not drive unit pixel scale or sorting.
                // Prefer the strict gameplay camera 1:1.
                for (int i = 0; i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        n.IndexOf("Free", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        _cachedBattleCameraLikeOriginal = c;
                        return c;
                    }
                }

                for (int i = 0; i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("BattleTerrainCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (n.IndexOf("Free", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (fallback == null) fallback = c;
                            continue;
                        }
                        _cachedBattleCameraLikeOriginal = c;
                        return c;
                    }
                }
            }
            catch { }
            if (fallback != null)
            {
                _cachedBattleCameraLikeOriginal = fallback;
                return fallback;
            }
            _cachedBattleCameraLikeOriginal = Camera.main;
            return _cachedBattleCameraLikeOriginal;
        }

        private bool TryFocusBattleCameraOnProbeLikeOriginal(Vector3 target, out string audit)
        {
            audit = string.Empty;
            Camera cam = FindBattleCameraLikeOriginal();
            if (cam == null) { audit = "cam=<null>"; return false; }
            Vector3 beforeCam = cam.transform.position;
            Vector3 beforeViewport = cam.WorldToViewportPoint(target);
            try
            {
                Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                Plane plane = new Plane(Vector3.up, new Vector3(0f, target.y, 0f));
                float enter;
                if (!plane.Raycast(centerRay, out enter)) { audit = "cam=" + cam.name + " planeRaycast=fail"; return false; }
                Vector3 centerHit = centerRay.GetPoint(enter);
                Vector3 delta = target - centerHit;
                cam.transform.position += delta;
                Vector3 afterViewport = cam.WorldToViewportPoint(target);
                audit = "cam=" + cam.name + " beforeCam=" + beforeCam.ToString("F3") + " target=" + target.ToString("F3") + " delta=" + delta.ToString("F3") + " beforeViewport=" + beforeViewport.ToString("F3") + " afterViewport=" + afterViewport.ToString("F3");
                return true;
            }
            catch (Exception ex) { audit = "focusException=" + ex.GetType().Name + ":" + ex.Message; return false; }
        }

        // Direct COSSACKS2/MiniMap4X.cpp::NewAnimation::DrawSpriteUnit port.
        private static DrawSpriteAudit ComputeDrawSpriteUnitLikeOriginal(AnimModel anim, FrameModel fr, C2UnitOriginalRuntime u)
        {
            int rotations = anim != null ? anim.Rotations : 1;
            if (rotations <= 0) rotations = 1;

            int realDir = u != null ? (u.RealDirPrecise & 255) : 0;
            if (u != null && (rotations == 16 || rotations == 9))
            {
                if (u.OctantInfo == 0xFF)
                {
                    u.OctantInfo = (realDir + 8) >> 4;
                }
                else
                {
                    int cd = (u.OctantInfo & 15) << 4;
                    int dd = (sbyte)((byte)cd - (byte)realDir);
                    int ad = Math.Abs(dd);
                    if (ad <= 8)
                    {
                        u.OctantInfo &= 0x0F;
                        realDir = (u.OctantInfo & 15) << 4;
                    }
                    else if (ad < 16)
                    {
                        int ot = u.OctantInfo >> 4;
                        if (ot < 12)
                        {
                            realDir = (u.OctantInfo & 15) << 4;
                            ot += ad >> 3;
                            u.OctantInfo = (u.OctantInfo & 15) + (ot << 4);
                        }
                        else
                        {
                            u.OctantInfo = (realDir + 8) >> 4;
                            realDir = (u.OctantInfo & 15) << 4;
                        }
                    }
                    else
                    {
                        u.OctantInfo = (realDir + 8) >> 4;
                        realDir = (u.OctantInfo & 15) << 4;
                    }
                }
            }

            if (anim != null && anim.Inverse)
                realDir = (128 - realDir) & 255;

            int octs;
            int oc2;
            int oc1;
            int ocM;
            int rawDir;
            if (rotations == 1)
            {
                octs = 1;
                oc2 = 1;
                oc1 = 1;
                ocM = 0;
                rawDir = 0;
            }
            else if ((rotations & 1) != 0)
            {
                octs = (rotations - 1) * 2;
                oc2 = rotations - 1;
                if (octs <= 0) octs = 1;
                int sesize = 255 / (octs * 2);
                oc1 = octs;
                ocM = oc2;
                rawDir = (((realDir + 64 + sesize) & 255) * octs) >> 8;
            }
            else
            {
                octs = rotations;
                oc2 = rotations;
                if (octs <= 0) octs = 1;
                int sesize = 128 / octs;
                oc1 = octs;
                ocM = 0;
                rawDir = (((realDir + 64 + sesize + 128) & 255) * octs) >> 8;
            }

            bool reverseBranch = rawDir < ocM;
            int directionIndex;
            bool mirrorX;
            if (reverseBranch)
            {
                directionIndex = oc2 - rawDir;
                mirrorX = anim == null || !anim.Inverse;
            }
            else
            {
                int dir = oc1 - rawDir;
                directionIndex = oc2 - dir;
                mirrorX = anim != null && anim.Inverse;
            }
            directionIndex = Mathf.Clamp(directionIndex, 0, rotations - 1);
            int displaySprite = rotations * fr.SpriteId + directionIndex;
            int signedDx = mirrorX ? -fr.Dx : fr.Dx;

            DrawSpriteAudit a;
            a.RawDir = rawDir;
            a.Dir = directionIndex;
            a.DirOffset = displaySprite - fr.SpriteId;
            a.DisplaySprite = displaySprite;
            a.ReverseBranch = reverseBranch;
            a.MirrorGeometry = mirrorX;
            a.MirroredByTransform = mirrorX;
            a.PivotX = signedDx;
            a.PivotY = fr.Dy;
            a.RealDirAfterOctant = realDir;
            a.Branch = reverseBranch ? "cossacks2_reverse" : "cossacks2_non_reverse";
            return a;
        }

        // --------------------------------------------------------------------
        // MD parse: NewAnimation table.
        // --------------------------------------------------------------------
        private static bool TryParseMdLikeOriginal(string mdPath, out MdModel md, out string audit)
        {
            md = new MdModel(); md.Path = mdPath; audit = string.Empty;
            try
            {
                string[] lines = File.ReadAllLines(mdPath, System.Text.Encoding.Default);
                md.Name = Path.GetFileNameWithoutExtension(mdPath);
                LoadTiringDefaultsFromNresLikeOriginal(mdPath, md);
                int malformed = 0;
                for (int li = 0; li < lines.Length; li++)
                {
                    string rawOriginal = lines[li] ?? string.Empty;
                    string raw = rawOriginal.Trim();
                    if (raw.Length == 0) continue;
                    if (raw.StartsWith("[", StringComparison.Ordinal)) break;
                    if (raw.StartsWith("//", StringComparison.Ordinal)) continue;
                    if (raw.StartsWith("/", StringComparison.Ordinal)) continue;
                    int comment = raw.IndexOf("//", StringComparison.Ordinal);
                    if (comment >= 0) raw = raw.Substring(0, comment).Trim();
                    if (raw.Length == 0) continue;
                    string[] t = SplitTokens(raw);
                    if (t.Length == 0) continue;
                    if (string.Equals(t[0], "NAME", StringComparison.OrdinalIgnoreCase) && t.Length >= 2) { md.Name = t[1]; continue; }
                    if (string.Equals(t[0], "GEOMETRY", StringComparison.OrdinalIgnoreCase) && t.Length >= 4)
                    {
                        int radius1;
                        int radius2;
                        int motionDist;
                        if (TryParseInt(t[1], out radius1)) md.GeometryRadius1 = radius1;
                        if (TryParseInt(t[2], out radius2)) md.GeometryRadius2 = radius2;
                        if (TryParseInt(t[3], out motionDist) && motionDist > 0) md.MotionDist = motionDist;
                        continue;
                    }
                    if (string.Equals(t[0], "USAGE", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        md.Usage = t[1] ?? string.Empty;
                        continue;
                    }
                    if (string.Equals(t[0], "VISION", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int vision;
                        if (TryParseInt(t[1], out vision)) md.VisionType = Mathf.Max(0, vision);
                        continue;
                    }
                    if (string.Equals(t[0], "DONTAFFECTFOGOFWAR", StringComparison.OrdinalIgnoreCase))
                    {
                        md.DontAffectFogOfWar = true;
                        continue;
                    }
                    if (string.Equals(t[0], "CANBUILD", StringComparison.OrdinalIgnoreCase))
                    {
                        md.CanBuild = true;
                        continue;
                    }
                    if (string.Equals(t[0], "PIONEER", StringComparison.OrdinalIgnoreCase))
                    {
                        md.Pioneer = true;
                        continue;
                    }
                    if (string.Equals(t[0], "MOTIONSTYLE", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        md.MotionStyle = t[1].ToUpperInvariant();
                        continue;
                    }
                    if (string.Equals(t[0], "COMPLEXOBJECT", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        md.ComplexObjectIdLikeOriginal = t[1];
                        continue;
                    }
                    if (string.Equals(t[0], "SELTYPE", StringComparison.OrdinalIgnoreCase) && t.Length >= 4)
                    {
                        md.SelectionTypeV437=t[1];
                        md.SelectionScaleXV437=float.Parse(t[2],CultureInfo.InvariantCulture);
                        md.SelectionScaleYV437=float.Parse(t[3],CultureInfo.InvariantCulture);
                        continue;
                    }
                    if (string.Equals(t[0], "SELSHIFT", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {md.SelectionShiftV437=int.Parse(t[1],CultureInfo.InvariantCulture);continue;}
                    if (string.Equals(t[0], "ARTPODGOTOVKA", StringComparison.OrdinalIgnoreCase))
                    {
                        md.Artpodgotovka = true;
                        continue;
                    }
                    if (string.Equals(t[0], "RPLACESPEED", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int rotationAtPlaceSpeed;
                        if (TryParseInt(t[1], out rotationAtPlaceSpeed))
                            md.RotationAtPlaceSpeed = Math.Max(0, rotationAtPlaceSpeed);
                        continue;
                    }
                    if (string.Equals(t[0], "BRANDOMPOS", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int randomPos;
                        if (TryParseInt(t[1], out randomPos))
                            md.BRandomPosLikeOriginal = Math.Max(0, randomPos);
                        continue;
                    }
                    if (string.Equals(t[0], "BRANDOMSPEED", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int randomSpeed;
                        if (TryParseInt(t[1], out randomSpeed))
                            md.BRandomSpeedLikeOriginal = Math.Max(0, randomSpeed);
                        continue;
                    }
                    if (string.Equals(t[0], "DONTSTUCKINENEMY", StringComparison.OrdinalIgnoreCase))
                    {
                        md.DontStuckInEnemyLikeOriginal = true;
                        continue;
                    }
                    if (string.Equals(t[0], "LOWCOLLISION", StringComparison.OrdinalIgnoreCase))
                    {
                        md.LowCollisionLikeOriginal = true;
                        continue;
                    }
                    if (string.Equals(t[0], "BOIDSMOVING", StringComparison.OrdinalIgnoreCase))
                    {
                        md.BoidsMoving = true;
                        if (t.Length >= 3)
                        {
                            int minDist;
                            int weight;
                            if (TryParseInt(t[1], out minDist)) md.BoidsMovingMinDist = minDist;
                            if (TryParseInt(t[2], out weight)) md.BoidsMovingWeight = weight;
                        }
                        continue;
                    }
                    if (string.Equals(t[0], "ROTATE", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int rotate;
                        if (TryParseInt(t[1], out rotate))
                        {
                            md.RotateStep = rotate;
                            md.MinRotator = rotate;
                        }
                        continue;
                    }
                    if (string.Equals(t[0], "RATE", StringComparison.OrdinalIgnoreCase) && t.Length >= 3)
                    {
                        int rateIndex;
                        int rateValue;
                        if (TryParseInt(t[1], out rateIndex) && TryParseInt(t[2], out rateValue) &&
                            rateIndex >= 0 && rateIndex < md.Rate.Length)
                            md.Rate[rateIndex] = rateValue;
                        continue;
                    }
                    if (string.Equals(t[0], "STOPDISTANCE", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int distance;
                        if (TryParseInt(t[1], out distance)) md.StopDistance = Math.Min(distance,300);
                        continue;
                    }
                    if (string.Equals(t[0], "SPEEDSCALE", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int percent;
                        if (TryParseInt(t[1], out percent)) md.SpeedScale = (percent * 256) / 100;
                        continue;
                    }
                    if (string.Equals(t[0], "SPEEDSCALEONTREES", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int percent;
                        if (TryParseInt(t[1], out percent)) md.SpeedScaleOnTrees = (percent * 256) / 100;
                        continue;
                    }
                    if (string.Equals(t[0], "FORMDISTSCALE", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int value;
                        if (TryParseInt(t[1], out value)) md.FormationDistanceScale = value;
                        continue;
                    }
                    if (string.Equals(t[0], "FLYPARAM", StringComparison.OrdinalIgnoreCase) && t.Length >= 3)
                    {
                        int h0;
                        int h;
                        if (TryParseInt(t[1], out h0) && TryParseInt(t[2], out h))
                        {
                            md.StartFlyHeightLikeOriginal = h0;
                            md.FlyHeightLikeOriginal = h;
                        }
                        else malformed++;
                        continue;
                    }
                    if (string.Equals(t[0], "WATERROUND", StringComparison.OrdinalIgnoreCase))
                    {
                        md.WaterActiveLikeOriginal = true;
                        continue;
                    }
                    if (string.Equals(t[0], "KINETIC", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int value;
                        if (TryParseInt(t[1], out value)) md.KineticLimitLikeOriginal = Math.Max(0, value);
                        else malformed++;
                        continue;
                    }
                    if (string.Equals(t[0], "MINDISTANCETOENTERROAD", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int value;
                        if (TryParseInt(t[1], out value)) md.MinDistanceToEnterRoad = value;
                        continue;
                    }
                    if (string.Equals(t[0], "MINDISTFORLINEFORMATIONS", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int value;
                        if (TryParseInt(t[1], out value)) md.MinDistForLineFormations = value;
                        continue;
                    }
                    if (string.Equals(t[0], "MINTOPDISTANCETOENTERROAD", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int value;
                        if (TryParseInt(t[1], out value)) md.MinTopDistanceToEnterRoad = value;
                        continue;
                    }
                    if (string.Equals(t[0], "RETREATRADIUS", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int value;
                        if (TryParseInt(t[1], out value)) md.RetreatRadius = Math.Max(0, value);
                        continue;
                    }
                    if (string.Equals(t[0], "DIRECTTRANS", StringComparison.OrdinalIgnoreCase) && t.Length >= 3)
                    {
                        int fromState;
                        int toState;
                        if (TryParseInt(t[1], out fromState) && TryParseInt(t[2], out toState) &&
                            fromState >= 0 && fromState < md.DirectTransitionMaskLikeOriginal.Length &&
                            toState >= 0 && toState < md.DirectTransitionMaskLikeOriginal.Length)
                        {
                            md.DirectTransitionMaskLikeOriginal[fromState] |= 1 << toState;
                            md.DirectTransitionMaskLikeOriginal[toState] |= 1 << fromState;
                        }
                        else malformed++;
                        continue;
                    }
                    if (string.Equals(t[0], "USETRANSX", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int state;
                        if (TryParseInt(t[1], out state) && state >= 0 && state < 16)
                            md.TransXMaskLikeOriginal |= 1 << state;
                        else
                            malformed++;
                        continue;
                    }
                    if (string.Equals(t[0], "TIREDCHANGE", StringComparison.OrdinalIgnoreCase) && t.Length >= 3)
                    {
                        int tiringChange;
                        if (TryParseInt(t[2], out tiringChange))
                            md.TiringOverridesLikeOriginal[NormalizeAnimationNameLikeOriginal(t[1])] = tiringChange;
                        else
                            malformed++;
                        continue;
                    }
                    if (string.Equals(t[0], "SLOWFRAME", StringComparison.OrdinalIgnoreCase) && t.Length >= 5)
                    {
                        int slowAnim = ResolveAnimationIndexLikeOriginal(md, NormalizeAnimationNameLikeOriginal(t[1]));
                        int start;
                        int end;
                        int speed;
                        if (slowAnim >= 0 &&
                            TryParseInt(t[2], out start) && TryParseInt(t[3], out end) && TryParseInt(t[4], out speed))
                        {
                            AnimModel slow = md.Animations[slowAnim];
                            slow.SlowFrameStartLikeOriginal = start;
                            slow.SlowFrameEndLikeOriginal = end;
                            slow.SlowFrameSpeedLikeOriginal = speed;
                        }
                        // NewMon.cpp ignores a null animation lookup here.
                        continue;
                    }
                    if ((string.Equals(t[0], "BREAKANIMATION", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t[0], "MOVEBREAK", StringComparison.OrdinalIgnoreCase)) && t.Length >= 2)
                    {
                        int marked = ResolveAnimationIndexLikeOriginal(md, NormalizeAnimationNameLikeOriginal(t[1]));
                        if (marked >= 0)
                        {
                            if (string.Equals(t[0], "BREAKANIMATION", StringComparison.OrdinalIgnoreCase))
                                md.Animations[marked].CanBeBroken = true;
                            else
                                md.Animations[marked].MoveBreak = true;
                        }
                        // The shipped MD set deliberately contains optional
                        // MOVEBREAK references to clips disabled with a leading
                        // '/'. NewMon.cpp simply ignores a null lookup here.
                        continue;
                    }
                    if (string.Equals(t[0], "USERLC", StringComparison.OrdinalIgnoreCase))
                    {
                        UserLcRef r;
                        if (TryParseUserLc(t, li + 1, rawOriginal, out r)) md.UserLc[r.FileRef] = r; else malformed++;
                        continue;
                    }
                    if (string.Equals(t[0], "ANMSUMM", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TryApplyAnimationSummationLikeOriginal(md, t))
                            malformed++;
                        continue;
                    }
                    if ((string.Equals(t[0], "SETACTIVEPOINT", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t[0], "SETACTIVEPOINT0", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t[0], "SETACTIVEPOINT2", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t[0], "SETACTIVEPOINT3", StringComparison.OrdinalIgnoreCase)) &&
                        t.Length >= 3)
                    {
                        int activeFrame;
                        int activeAnim = ResolveAnimationIndexLikeOriginal(
                            md, NormalizeAnimationNameLikeOriginal(t[1]));
                        if (activeAnim >= 0 && TryParseInt(t[2], out activeFrame))
                        {
                            AnimModel active = md.Animations[activeAnim];
                            active.ActiveFrame = Mathf.Max(0, activeFrame);
                            int muzzleGroups =
                                string.Equals(t[0], "SETACTIVEPOINT3", StringComparison.OrdinalIgnoreCase) ? 3 :
                                string.Equals(t[0], "SETACTIVEPOINT2", StringComparison.OrdinalIgnoreCase) ? 2 :
                                string.Equals(t[0], "SETACTIVEPOINT0", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                            active.DoubleShot = Mathf.Max(0, muzzleGroups - 1);
                            int pointCount = active.Rotations * muzzleGroups;
                            if (pointCount > 0)
                            {
                                active.ActivePtX = new int[pointCount];
                                active.ActivePtY = new int[pointCount];
                                int token = 3;
                                for (int point = 0; point < pointCount; point++)
                                {
                                    int x;
                                    int y;
                                    if (token + 1 >= t.Length ||
                                        !TryParseInt(t[token], out x) ||
                                        !TryParseInt(t[token + 1], out y))
                                    {
                                        malformed++;
                                        break;
                                    }
                                    active.ActivePtX[point] = x;
                                    active.ActivePtY[point] = y;
                                    token += 2;
                                }
                            }
                        }
                        continue;
                    }
                    char c = t[0][0];
                    if (c == '#' || c == '@' || c == '$' || c == '%')
                    {
                        AnimModel anim;
                        if (TryParseAnimationLikeOriginal(t, li + 1, rawOriginal, md.UserLc, out anim))
                        {
                            if (anim != null && anim.Frames.Count > 0) md.Animations.Add(anim);
                        }
                        else malformed++;
                    }
                }
                FinalizeOriginalAnimationTableLikeOriginal(md);
                ApplyAnimationTiringTableLikeOriginal(md);
                audit = "ok lines=" + lines.Length.ToString(CultureInfo.InvariantCulture) + " userlc=" + md.UserLc.Count.ToString(CultureInfo.InvariantCulture) + " animations=" + md.Animations.Count.ToString(CultureInfo.InvariantCulture) + " frames=" + CountTotalFrames(md).ToString(CultureInfo.InvariantCulture) + " motionDist=" + md.MotionDist.ToString(CultureInfo.InvariantCulture) + " motionStyle=" + md.MotionStyle + " boids=" + md.BoidsMoving + " canBuild=" + md.CanBuild + " pioneer=" + md.Pioneer + " tiring=" + md.TiringAuditLikeOriginal + " malformed=" + malformed.ToString(CultureInfo.InvariantCulture);
                return !string.IsNullOrEmpty(md.ComplexObjectIdLikeOriginal) || (md.UserLc.Count > 0 && md.Animations.Count > 0);
            }
            catch (Exception ex) { audit = ex.GetType().Name + ":" + ex.Message; return false; }
        }

        private static bool TryApplyAnimationSummationLikeOriginal(MdModel md, string[] tokens)
        {
            if (md == null || tokens == null || tokens.Length < 4)
                return false;
            int amount;
            if (!TryParseInt(tokens[2], out amount) || amount < 1 || tokens.Length < 3 + amount)
                return false;
            int targetIndex = ResolveAnimationIndexLikeOriginal(md, NormalizeAnimationNameLikeOriginal(tokens[1]));
            if (targetIndex < 0)
                return false;
            AnimModel target = md.Animations[targetIndex];
            bool appended = false;
            for (int i = 0; i < amount; i++)
            {
                int sourceIndex = ResolveAnimationIndexLikeOriginal(
                    md,
                    NormalizeAnimationNameLikeOriginal(tokens[3 + i]));
                if (sourceIndex < 0)
                    continue;
                AnimModel source = md.Animations[sourceIndex];
                for (int frame = 0; frame < source.Frames.Count; frame++)
                    target.Frames.Add(source.Frames[frame]);
                appended = true;
            }
            return appended;
        }

        private static void LoadTiringDefaultsFromNresLikeOriginal(string mdPath, MdModel md)
        {
            if (md == null) return;
            string mdDir = string.IsNullOrEmpty(mdPath) ? string.Empty : Path.GetDirectoryName(mdPath);
            string dataRoot = !string.IsNullOrEmpty(mdDir) ? Directory.GetParent(mdDir)?.FullName : string.Empty;
            string[] candidates = new string[]
            {
                !string.IsNullOrEmpty(dataRoot) ? Path.Combine(dataRoot, "Nres.dat") : string.Empty,
                !string.IsNullOrEmpty(dataRoot) ? Path.Combine(dataRoot, "NRes.dat") : string.Empty,
                !string.IsNullOrEmpty(mdDir) ? Path.Combine(mdDir, "Nres.dat") : string.Empty
            };
            string path = string.Empty;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrEmpty(candidates[i]) && File.Exists(candidates[i]))
                {
                    path = candidates[i];
                    break;
                }
            }
            if (string.IsNullOrEmpty(path))
            {
                md.TiringAuditLikeOriginal = "NewMon.cpp defaults (Nres.dat missing)";
                return;
            }
            try
            {
                string[] lines = File.ReadAllLines(path, System.Text.Encoding.Default);
                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = (lines[i] ?? string.Empty).Trim();
                    if (raw.Length == 0 || raw.StartsWith("/", StringComparison.Ordinal)) continue;
                    string[] t = SplitTokens(raw);
                    if (t.Length < 2) continue;
                    int value;
                    if (!TryParseInt(t[1], out value)) continue;
                    if (string.Equals(t[0], "Default_StandTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultStandTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_RestTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultRestTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_MotionTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultMotionTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_AttackTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultAttackTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_PAttackTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultPAttackTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_UAttackTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultUAttackTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_AttMotionTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultAttMotionTiringLikeOriginal = value;
                    else if (string.Equals(t[0], "Default_PStandTiring", StringComparison.OrdinalIgnoreCase)) md.DefaultPStandTiringLikeOriginal = value;
                }
                md.TiringAuditLikeOriginal = "Nres.dat:" + Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                md.TiringAuditLikeOriginal = "Nres.dat error:" + ex.GetType().Name;
            }
        }

        private static void ApplyAnimationTiringTableLikeOriginal(MdModel md)
        {
            if (md == null) return;
            for (int i = 0; i < md.Animations.Count; i++)
            {
                AnimModel anim = md.Animations[i];
                if (anim == null) continue;
                string name = anim.Name ?? string.Empty;
                int change = md.DefaultStandTiringLikeOriginal;
                // NewMon.cpp::SetDefaultAnmTiring order, intentionally preserved.
                if (name.IndexOf("#ATTACK", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultAttackTiringLikeOriginal;
                if (name.IndexOf("#MOTION", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultMotionTiringLikeOriginal;
                if (name.IndexOf("#REST", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultRestTiringLikeOriginal;
                if (name.IndexOf("#ATTACK", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultAttackTiringLikeOriginal;
                if (name.IndexOf("#UATTACK", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultUAttackTiringLikeOriginal;
                if (name.IndexOf("#PATTACK", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultPAttackTiringLikeOriginal;
                if (name.IndexOf("#MOTION_L0", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultAttMotionTiringLikeOriginal;
                if (name.IndexOf("#PSTAND", StringComparison.OrdinalIgnoreCase) >= 0) change = md.DefaultPStandTiringLikeOriginal;
                int overridden;
                if (md.TiringOverridesLikeOriginal.TryGetValue(name, out overridden)) change = overridden;
                anim.TiringChange = change;
            }
        }

        private static string NormalizeAnimationNameLikeOriginal(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            char kind = name[0];
            if (kind == '@' || kind == '$' || kind == '%')
                return "#" + name.Substring(1);
            return name;
        }

        private static bool TryParseUserLc(string[] t, int line, string raw, out UserLcRef r)
        {
            r = new UserLcRef();
            if (t.Length < 3) return false;
            int fileRef;
            if (!TryParseInt(t[1], out fileRef)) return false;
            r.FileRef = fileRef; r.Package = t[2]; r.Line = line; r.RawLine = raw; r.Shadow = false; r.Dx = 0; r.Dy = 0;
            for (int i = 3; i < t.Length; i++)
            {
                if (string.Equals(t[i], "SHADOW", StringComparison.OrdinalIgnoreCase))
                {
                    r.Shadow = true;
                    if (i + 2 < t.Length)
                    {
                        int dx, dy;
                        if (TryParseInt(t[i + 1], out dx)) r.Dx = dx;
                        if (TryParseInt(t[i + 2], out dy)) r.Dy = dy;
                    }
                    break;
                }
            }
            return true;
        }

        private static bool TryParseAnimationLikeOriginal(string[] t, int line, string raw, Dictionary<int, UserLcRef> userLc, out AnimModel anim)
        {
            anim = null;
            if (t.Length < 2) return false;
            char kind = t[0][0];
            string name = t[0];
            if (kind == '@' || kind == '$' || kind == '%') name = "#" + t[0].Substring(1);
            if (kind == '#') return TryParseExplicitFrames(name, "#", t, line, raw, userLc, out anim);
            if (kind == '@') return TryParseRangeFrames(name, "@", t, line, raw, userLc, out anim);
            if (kind == '$') return TryParseMultiRangeFrames(name, "$", t, line, raw, userLc, out anim);
            if (kind == '%') return TryParseExplicitFixedPivotFrames(name, "%", t, line, raw, userLc, out anim);
            return false;
        }

        private static bool TryParseExplicitFrames(string name, string sourceKind, string[] t, int line, string raw, Dictionary<int, UserLcRef> userLc, out AnimModel anim)
        {
            anim = null;
            if (t.Length < 3) return false;
            int rotations, nFrames;
            if (!TryParseInt(t[1], out rotations)) return false;
            if (!TryParseInt(t[2], out nFrames)) return false;
            AnimModel a = NewAnim(name, sourceKind, rotations, line, raw);
            int pos = 3;
            for (int i = 0; i < nFrames && pos + 1 < t.Length; i++, pos += 2)
            {
                int fileRef, sprite;
                if (!TryParseInt(t[pos], out fileRef)) return false;
                if (!TryParseInt(t[pos + 1], out sprite)) return false;
                a.Frames.Add(MakeFrame(userLc, fileRef, sprite, line));
            }
            anim = a;
            return a.Frames.Count == nFrames;
        }

        private static bool TryParseRangeFrames(string name, string sourceKind, string[] t, int line, string raw, Dictionary<int, UserLcRef> userLc, out AnimModel anim)
        {
            anim = null;
            if (t.Length < 5) return false;
            int rotations, fileRef, start, end;
            if (!TryParseInt(t[1], out rotations)) return false;
            if (!TryParseInt(t[2], out fileRef)) return false;
            if (!TryParseInt(t[3], out start)) return false;
            if (!TryParseInt(t[4], out end)) return false;
            AnimModel a = NewAnim(name, sourceKind, rotations, line, raw);
            int step = start > end ? -1 : 1;
            int stop = end + step;
            for (int sp = start; sp != stop; sp += step) a.Frames.Add(MakeFrame(userLc, fileRef, sp, line));
            anim = a;
            return true;
        }

        private static bool TryParseMultiRangeFrames(string name, string sourceKind, string[] t, int line, string raw, Dictionary<int, UserLcRef> userLc, out AnimModel anim)
        {
            // Original NewMon.cpp '$' syntax:
            //   $ANIM dx dy rotations fileRef startSprite endSprite
            // It is NOT a multipart list. It is a fixed-pivot range animation.
            anim = null;
            if (t.Length < 7) return false;
            int dx, dy, rotations, fileRef, start, end;
            if (!TryParseInt(t[1], out dx)) return false;
            if (!TryParseInt(t[2], out dy)) return false;
            if (!TryParseInt(t[3], out rotations)) return false;
            if (!TryParseInt(t[4], out fileRef)) return false;
            if (!TryParseInt(t[5], out start)) return false;
            if (!TryParseInt(t[6], out end)) return false;
            AnimModel a = NewAnim(name, sourceKind, rotations, line, raw);
            int step = start > end ? -1 : 1;
            int stop = end + step;
            for (int sp = start; sp != stop; sp += step)
            {
                FrameModel fr = MakeFrame(userLc, fileRef, sp, line);
                fr.Dx = dx;
                fr.Dy = dy;
                fr.PivotSource = "explicit_$";
                a.Frames.Add(fr);
            }
            anim = a;
            return a.Frames.Count > 0;
        }

        private static bool TryParseExplicitFixedPivotFrames(string name, string sourceKind, string[] t, int line, string raw, Dictionary<int, UserLcRef> userLc, out AnimModel anim)
        {
            anim = null;
            if (t.Length < 5) return false;
            int dx, dy, rotations, nFrames;
            if (!TryParseInt(t[1], out dx)) return false;
            if (!TryParseInt(t[2], out dy)) return false;
            if (!TryParseInt(t[3], out rotations)) return false;
            if (!TryParseInt(t[4], out nFrames)) return false;
            AnimModel a = NewAnim(name, sourceKind, rotations, line, raw);
            int pos = 5;
            for (int i = 0; i < nFrames && pos + 1 < t.Length; i++, pos += 2)
            {
                int fileRef, sprite;
                if (!TryParseInt(t[pos], out fileRef)) return false;
                if (!TryParseInt(t[pos + 1], out sprite)) return false;
                FrameModel fr = MakeFrame(userLc, fileRef, sprite, line);
                fr.Dx = dx; fr.Dy = dy; fr.PivotSource = "explicit_%";
                a.Frames.Add(fr);
            }
            anim = a;
            return a.Frames.Count == nFrames;
        }

        private static AnimModel NewAnim(string name, string sourceKind, int rotations, int line, string raw)
        {
            var a = new AnimModel();
            a.Name = name; a.SourceKind = sourceKind; a.Inverse = rotations < 0; a.Rotations = rotations == 0 ? 1 : Math.Abs(rotations); a.SourceLine = line; a.RawLine = raw.Trim();
            // NewAnimation parser: explicit #/% clips start at 0xFF, range
            // animations (@/$) use zero and attack code replaces zero with N/2.
            a.ActiveFrame = (sourceKind == "#" || sourceKind == "%") ? 0xFF : 0;
            return a;
        }

        private static FrameModel MakeFrame(Dictionary<int, UserLcRef> userLc, int fileRef, int sprite, int line)
        {
            var fr = new FrameModel();
            fr.FileRef = fileRef; fr.SpriteId = sprite; fr.SourceLine = line; fr.Package = string.Empty; fr.Dx = 0; fr.Dy = 0; fr.PivotSource = "missing_userlc";
            UserLcRef r;
            if (userLc.TryGetValue(fileRef, out r)) { fr.Package = r.Package; fr.Dx = r.Dx; fr.Dy = r.Dy; fr.PivotSource = "USERLC_RLCdxdy"; }
            return fr;
        }

        private string ResolveMdFilePathLikeOriginal(UnitProbe unit)
        {
            string p = unit.MdPath;
            if (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) return p;
            string mdName = unit.MdName;
            if (string.IsNullOrWhiteSpace(mdName)) mdName = unit.MonsterId;
            mdName = mdName.Trim();
            if (mdName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) mdName = Path.GetFileNameWithoutExtension(mdName);
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(_dataRoot))
            {
                candidates.Add(Path.Combine(_dataRoot, "UnitsMD", mdName + ".md"));
                candidates.Add(Path.Combine(_dataRoot, "UnitsMD", mdName + ".MD"));
                candidates.Add(Path.Combine(_dataRoot, "UnitsGuardMD", mdName + ".md"));
                candidates.Add(Path.Combine(_dataRoot, "UnitsGuardMD", mdName + ".MD"));
            }
            candidates.Add(Path.Combine(Application.dataPath, "Resources", "UnitsMD", mdName + ".md"));
            candidates.Add(Path.Combine(Application.dataPath, "Resources", "UnitsMD", mdName + ".MD"));
            candidates.Add(Path.Combine(Application.dataPath, "Resources", "UnitsGuardMD", mdName + ".md"));
            candidates.Add(Path.Combine(Application.dataPath, "Resources", "UnitsGuardMD", mdName + ".MD"));
            for (int i = 0; i < candidates.Count; i++) if (File.Exists(candidates[i])) return candidates[i];
            return !string.IsNullOrWhiteSpace(unit.MdPath) ? unit.MdPath : (mdName + ".md");
        }

        private static int CountTotalFrames(MdModel md)
        {
            int n = 0;
            if (md == null) return 0;
            for (int i = 0; i < md.Animations.Count; i++) n += md.Animations[i].Frames.Count;
            return n;
        }

        private static string[] SplitTokens(string s) { return s.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); }
        private static bool TryParseInt(string s, out int v) { return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v); }

        private bool TryResolveBattleContextLikeOriginal(C2BattleTerrainMode battle, out string dataRoot, out string mapRel, out string mapAbs)
        {
            dataRoot = string.Empty; mapRel = string.Empty; mapAbs = string.Empty;
            if (battle == null) return false;
            object bootstrap = GetFieldObject(battle, "_bootstrap");
            if (bootstrap != null)
            {
                object fs = GetPropertyOrFieldObject(bootstrap, "Fs");
                if (fs != null)
                {
                    object rootObj = GetPropertyOrFieldObject(fs, "DataRoot");
                    if (rootObj != null) dataRoot = rootObj.ToString();
                }
            }
            object relObj = GetFieldObject(battle, "_mapRelativePath");
            if (relObj != null) mapRel = relObj.ToString();
            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                string guess = @"C:\GSC Game World\Cossacks II\Data";
                if (Directory.Exists(guess)) dataRoot = guess;
            }
            if (string.IsNullOrWhiteSpace(mapRel)) mapRel = @"Missions\Skirmish\Skirmish2.m3d";
            if (!string.IsNullOrWhiteSpace(dataRoot) && !string.IsNullOrWhiteSpace(mapRel))
            {
                string relNorm = mapRel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                mapAbs = Path.Combine(dataRoot, relNorm);
            }
            return !string.IsNullOrWhiteSpace(dataRoot) && File.Exists(mapAbs);
        }

        private static bool TryParse3InuRecordsByReflectionLikeOriginal(string absMap, out IList records, out string audit)
        {
            records = null; audit = string.Empty;
            try
            {
                MethodInfo m = typeof(C2BattleTerrainMode).GetMethod("C2Settlement3InuMdV2TryParseRecordsLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic);
                if (m == null) { audit = "missing_private_parser_method"; return false; }
                object[] args = new object[] { absMap, null, null };
                bool ok = (bool)m.Invoke(null, args);
                records = args[1] as IList;
                audit = args[2] != null ? args[2].ToString() : string.Empty;
                return ok && records != null && records.Count > 0;
            }
            catch (Exception ex) { audit = "reflection_parse_failed " + ex.GetType().Name + ":" + UnwrapReflectionMessage(ex); return false; }
        }

        private static object ResolveMdByReflectionLikeOriginal(string monsterId)
        {
            try
            {
                MethodInfo m = typeof(C2BattleTerrainMode).GetMethod("C2Settlement3InuMdV2ResolveMdLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic);
                if (m == null) return null;
                return m.Invoke(null, new object[] { monsterId });
            }
            catch { return null; }
        }

        private static bool TryWorldPosByReflectionLikeOriginal(C2BattleTerrainMode battle, object record, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (battle == null || record == null) return false;
            try
            {
                MethodInfo m = typeof(C2BattleTerrainMode).GetMethod("C2Settlement3InuMdV2WorldLikeOriginal", BindingFlags.Instance | BindingFlags.NonPublic);
                if (m == null) return false;
                object v = m.Invoke(battle, new object[] { record });
                if (v is Vector3) { pos = (Vector3)v; return true; }
            }
            catch { }
            return false;
        }

        private static object GetPropertyOrFieldObject(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            Type t = obj.GetType();
            PropertyInfo p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) { try { return p.GetValue(obj, null); } catch { } }
            FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) { try { return f.GetValue(obj); } catch { } }
            return null;
        }

        private static object GetFieldObject(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return null;
            try { return f.GetValue(obj); } catch { return null; }
        }
        private static string GetFieldString(object obj, string name, string def) { object v = GetFieldObject(obj, name); return v != null ? v.ToString() : def; }
        private static int GetFieldInt(object obj, string name, int def) { object v = GetFieldObject(obj, name); if (v == null) return def; try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); } catch { return def; } }
        private static bool GetFieldBool(object obj, string name, bool def) { object v = GetFieldObject(obj, name); if (v == null) return def; try { return Convert.ToBoolean(v, CultureInfo.InvariantCulture); } catch { return def; } }
        private static string UnwrapReflectionMessage(Exception ex)
        {
            if (ex == null) return string.Empty;
            TargetInvocationException tie = ex as TargetInvocationException;
            if (tie != null && tie.InnerException != null) return tie.InnerException.GetType().Name + ":" + tie.InnerException.Message;
            return ex.Message;
        }

        private static float StableUnitRandom01LikeOriginal(UnitProbe p, int salt)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (p != null ? p.Index : 0);
                h = h * 31 + (p != null ? p.RealX : 0);
                h = h * 31 + (p != null ? p.RealY : 0);
                h = h * 31 + salt;
                uint x = (uint)h;
                x ^= x << 13; x ^= x >> 17; x ^= x << 5;
                return (x & 0x00FFFFFFu) / 16777215.0f;
            }
        }

        private static float StableUnitRandomRangeLikeOriginal(UnitProbe p, int salt, float min, float max)
        {
            if (max < min) { float t = min; min = max; max = t; }
            return Mathf.Lerp(min, max, StableUnitRandom01LikeOriginal(p, salt));
        }

        private static string SanitizeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unit";
            char[] arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++) if (!char.IsLetterOrDigit(arr[i]) && arr[i] != '_' && arr[i] != '-') arr[i] = '_';
            return new string(arr);
        }

        internal sealed class UnitProbe
        {
            public object RawRecord;
            public object RawMd;
            public string MonsterId;
            public string MdName;
            public string MdPath;
            public string DefaultPackage;
            public int Nation;
            public int RealX;
            public int RealY;
            public int RealDir;
            public int Index;
        }

        internal sealed class MdModel
        {
            internal string SelectionTypeV437;
            internal float SelectionScaleXV437=1, SelectionScaleYV437=1;
            internal int SelectionShiftV437;
            public string Name;
            public string Path;
            public int GeometryRadius1 = 1;
            public int GeometryRadius2 = 10;
            public int MotionDist = 42;
            public int StopDistance = 256; // NewMonster constructor, native real units
            // COSSACKS2/NewMon.cpp NewMonster defaults / MD parser.
            public int SpeedScale = 256;
            public int SpeedScaleOnTrees;
            // NewMonster constructor default is 100; MD token FORMDISTSCALE overrides it.
            public int FormationDistanceScale = 100;
            public int MinRotator = 16;
            public int RotationAtPlaceSpeed;
            public bool Artpodgotovka;
            public int MinDistanceToEnterRoad;
            public int MinDistForLineFormations;
            public int MinTopDistanceToEnterRoad;
            // COSSACKS2/NewMon.cpp: RETREATRADIUS. Zero means the Multi.cpp
            // player-order default of 500 original map pixels.
            public int RetreatRadius;
            // NewMonster::TransMask populated by DIRECTTRANS a b. Indices are
            // zero-based attack states, exactly as in the MD command.
            public readonly int[] DirectTransitionMaskLikeOriginal = new int[16];
            // NewMonster::TransXMask populated by USETRANSX n. TryToStand uses
            // it for state<->charge-state (#TRANSX3/#TRANS3X) transitions.
            public int TransXMaskLikeOriginal;
            public readonly int[] Rate = CreateDefaultRatesLikeOriginal();
            public int VisionType;
            public bool DontAffectFogOfWar;
            public int MoreCharacterSpeedPercent = 100;
            public string MotionStyle = string.Empty;
            public string ComplexObjectIdLikeOriginal = string.Empty;
            // NewMon.cpp NewMonster defaults; FLYPARAM overrides them.
            public int StartFlyHeightLikeOriginal = 60;
            public int FlyHeightLikeOriginal = 100;
            public int KineticLimitLikeOriginal;
            public bool WaterActiveLikeOriginal;
            public int RotateStep = 16;
            public bool BoidsMoving;
            // NewMonster::BRandomPos, used by Brigade::KeepPositions immediately
            // after CreateOrderedPositions to jitter each member's BR->posX/posY.
            public int BRandomPosLikeOriginal;
            // NewMonster::BRandomSpeed, applied by BrigadeOrder_KeepPositions after
            // proportional speed equalisation. AusGrn.MD uses BRANDOMSPEED 14.
            public int BRandomSpeedLikeOriginal;
            public bool DontStuckInEnemyLikeOriginal;
            public bool LowCollisionLikeOriginal;
            public int BoidsMovingMinDist = -1;
            public int BoidsMovingWeight = -1;
            public bool CanBuild;
            public bool Pioneer;
            public string Usage = string.Empty;
            // Nature.cpp compile defaults; Nres.dat overrides them before MD
            // animation defaults are assigned, then MDTIREDCHANGE overrides
            // individual clips exactly as mdParser.cpp does.
            public int DefaultStandTiringLikeOriginal = 50;
            public int DefaultRestTiringLikeOriginal = 50;
            public int DefaultMotionTiringLikeOriginal = -100;
            public int DefaultAttackTiringLikeOriginal = -100;
            public int DefaultPAttackTiringLikeOriginal = -100;
            public int DefaultUAttackTiringLikeOriginal = -100;
            public int DefaultAttMotionTiringLikeOriginal = -100;
            public int DefaultPStandTiringLikeOriginal = 100;
            public string TiringAuditLikeOriginal = "NewMon.cpp defaults";
            public readonly Dictionary<string, int> TiringOverridesLikeOriginal =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<int, UserLcRef> UserLc = new Dictionary<int, UserLcRef>();
            public readonly List<AnimModel> Animations = new List<AnimModel>();
            public readonly Dictionary<string, int> AnimationIndexCacheLikeOriginal =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Direct equivalents of NewMonster::GetAnimation(anm_*) hot lookups.
            public int MotionLAnimationIndexLikeOriginal = -1;
            public readonly int[] PostureMotionLAnimationIndicesV376LikeOriginal = new int[16];
            public readonly int[] PostureMotionRAnimationIndicesV411LikeOriginal = new int[16];
            public int RotateLAnimationIndexLikeOriginal = -1;
            public int RotateRAnimationIndexLikeOriginal = -1;
            public bool HaveRotateAnimationsLikeOriginal;

            private static int[] CreateDefaultRatesLikeOriginal()
            {
                // NewMonster constructor: for(i=0;i<NAttTypes;i++) Rate[i]=16.
                int[] rate = new int[16];
                for (int i = 0; i < rate.Length; i++) rate[i] = 16;
                return rate;
            }
        }

        internal struct UserLcRef
        {
            public int FileRef;
            public string Package;
            public int Dx;
            public int Dy;
            public bool Shadow;
            public int Line;
            public string RawLine;
        }

        internal sealed class AnimModel
        {
            public string Name;
            public string SourceKind;
            public int Rotations;
            public bool Inverse;
            public bool DoubleAnm;
            public bool CanBeBroken;
            public bool MoveBreak;
            public int ActiveFrame;
            public int[] ActivePtX;
            public int[] ActivePtY;
            public int DoubleShot;
            public int TiringChange;
            // NewMon.cpp::SLOWFRAME: used by MotionHandlerForFlyingObjects while turning.
            public int SlowFrameStartLikeOriginal;
            public int SlowFrameEndLikeOriginal;
            public int SlowFrameSpeedLikeOriginal = 100;
            public int SourceLine;
            public string RawLine;
            public readonly List<FrameModel> Frames = new List<FrameModel>();
        }

        internal struct FrameModel
        {
            public int FileRef;
            public string Package;
            public int SpriteId;
            public int Dx;
            public int Dy;
            public int SourceLine;
            public string PivotSource;
        }

        internal struct DrawSpriteAudit
        {
            public int RawDir;
            public int Dir;
            public int DirOffset;
            public int DisplaySprite;
            public bool ReverseBranch;
            public bool MirrorGeometry;
            public bool MirroredByTransform;
            public float PivotX;
            public float PivotY;
            public int RealDirAfterOctant;
            public string Branch;
        }
    }

    public sealed class C2UnitOriginalObjectRuntime
    {
        public Vector3 Position;
        public int RealDirPrecise;
        public int Serial;
        public int Nation;
    }

    public sealed class C2UnitOriginalAnimationState
    {
        public C2UnitOriginalState State;
        public int CurrentAnimIndex;
        public string CurrentAnimationName;
        public int CurrentFrameLong;
        public bool FrameFinished;
        public string Reason;
    }

    public sealed class C2UnitOriginalDrawSpritePath
    {
        public int FileID;
        public int SpriteID;
        public int DisplaySprite;
        public int NewFrameDx;
        public int NewFrameDy;
        public bool Mirrored;
    }

    public sealed class C2UnitOriginalSelectionPicker
    {
        public C2UnitOriginalRuntime Selected;
    }

    public enum C2UnitOriginalState
    {
        Stand,
        Rest,
        Motion,
        Work,
        Attack,
        Recharge,
        Transition,
        Death
    }

    // Compatibility API over C2UnitOriginalRuntime.  In COSSACKS2 these are
    // direct OneObject operations, not a Unity Component attached per unit.
    internal sealed class C2UnitOriginalRuntimeLinkLikeOriginal
    {
        internal C2UnitOriginalRuntimeAndRendererV1 Owner;
        internal C2UnitOriginalRuntime Runtime;

        internal void BindLikeOriginal(C2UnitOriginalRuntimeAndRendererV1 owner, C2UnitOriginalRuntime runtime)
        {
            Owner = owner;
            Runtime = runtime;
        }

        internal bool IsSelectedLikeOriginal
        {
            get { return Runtime != null && Runtime.Selected; }
        }

        internal bool IsReadyLikeOriginal
        {
            get { return Owner != null && Runtime != null && Runtime.ActiveLikeOriginal; }
        }

        internal Vector3 WorldPositionLikeOriginal
        {
            get { return Runtime != null ? Runtime.WorldPosition : Vector3.zero; }
        }

        internal GameObject EnsureUnityProxyLikeOriginal()
        {
            return Owner != null && Runtime != null
                ? Owner.EnsureRuntimeUnityProxyLikeOriginal(Runtime)
                : null;
        }

        internal void SetActiveLikeOriginal(bool active)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeActiveLikeOriginal(Runtime, active);
        }

        internal bool IsFrameFinishedLikeOriginal
        {
            get { return Runtime != null && Runtime.FrameFinishedLikeOriginal; }
        }

        // Stable SetNextFrame completion state for external order adapters.
        // FrameFinishedLikeOriginal is still used by the current renderer/state adapter;
        // this latch survives visual looping until SelectAnimationState/SetZeroFrame.
        internal bool IsFrameFinishedLatchedForOrdersLikeOriginal
        {
            get { return Runtime != null && Runtime.FrameFinishedLatchedForOrdersLikeOriginal; }
        }

        internal bool CanReceiveOrdersLikeOriginal()
        {
            return Owner != null &&
                   Runtime != null &&
                   Runtime.ActiveLikeOriginal &&
                   Runtime.State != C2UnitOriginalState.Death &&
                   !Runtime.PreciseBornPathLikeOriginal;
        }

        internal bool PlayDeathOneShotLikeOriginal(byte realDir)
        {
            return Owner != null && Runtime != null && Owner.PlayRuntimeDeathLikeOriginal(Runtime, realDir, "external_play_death");
        }

        internal int GetWorkFrameCountLikeOriginal(byte realDir)
        {
            return Owner != null && Runtime != null ? Owner.GetRuntimeWorkFrameCountLikeOriginal(Runtime, realDir) : 0;
        }

        internal bool SetWorkFramePhaseLikeOriginal(byte realDir, float phase, bool force)
        {
            return Owner != null && Runtime != null && Owner.SetRuntimeWorkFramePhaseLikeOriginal(Runtime, realDir, phase, force);
        }

        internal bool SetTakeResourceFramePhaseV222LikeOriginal(byte resourceId, byte realDir, float phase, bool force)
        {
            return Owner != null && Runtime != null && Owner.SetRuntimeTakeResourceFramePhaseV222LikeOriginal(Runtime, resourceId, realDir, phase, force);
        }

        internal int GetTakeResourceFrameCountV348LikeOriginal(byte resourceId)
        {
            return Owner != null && Runtime != null
                ? Owner.GetRuntimeTakeResourceFrameCountV348LikeOriginal(Runtime, resourceId)
                : 0;
        }

        internal bool SetTakeResourceDepositFramePhaseV227LikeOriginal(byte resourceId, byte realDir, float phase, bool force)
        {
            return Owner != null && Runtime != null && Owner.SetRuntimeTakeResourceDepositFramePhaseV227LikeOriginal(Runtime, resourceId, realDir, phase, force);
        }

        internal void StopTakeResourceWorkV222LikeOriginal()
        {
            if (Owner != null && Runtime != null) Owner.StopRuntimeTakeResourceWorkV222LikeOriginal(Runtime);
        }

        internal void SetCarryResourceMotionV223LikeOriginal(byte resourceId, bool carrying)
        {
            if (Owner != null && Runtime != null) Owner.SetRuntimeCarryResourceMotionV223LikeOriginal(Runtime, resourceId, carrying);
        }

        internal void SetHiddenInsideBuildingLikeOriginal(bool hidden)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeHiddenInsideBuildingLikeOriginal(Runtime, hidden);
        }

        internal void StopWorkAnimationLikeOriginal()
        {
            if (Owner != null && Runtime != null) Owner.StopRuntimeWorkAnimationLikeOriginal(Runtime);
        }

        internal void SetSelectedLikeOriginal(bool selected)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeSelectionFromInfoLikeOriginal(Runtime, selected);
        }

        internal bool TryGetScreenRectLikeOriginal(Camera cam, out Rect rect, out Vector2 anchor)
        {
            rect = default(Rect);
            anchor = Vector2.zero;
            return Owner != null && Runtime != null && Owner.TryGetRuntimeScreenRectLikeOriginal(Runtime, cam, out rect, out anchor);
        }

        internal bool TryPixelHitLikeOriginal(
            Camera cam,
            Vector3 screenPosition,
            out float alpha,
            out Vector2 uv)
        {
            alpha = 0.0f;
            uv = Vector2.zero;
            if (Owner == null || Runtime == null) return false;
            if (Runtime.OriginalComplexObjectV430LikeOriginal != null)
                return Owner.TryComplexPixelHitV437(Runtime,cam,screenPosition,out alpha,out uv);
            if (Runtime.LastTexture == null) return false;

            Rect rect;
            Vector2 anchor;
            if (!Owner.TryGetRuntimeScreenRectLikeOriginal(Runtime, cam, out rect, out anchor))
                return false;

            Vector2 p = new Vector2(screenPosition.x, screenPosition.y);
            if (!rect.Contains(p, true) || rect.width <= 0.5f || rect.height <= 0.5f)
                return false;

            uv = new Vector2(
                Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, screenPosition.x)),
                Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, screenPosition.y)));

            // V356: the old compatibility picker treated the entire sprite quad as
            // opaque (alpha=1).  Transparent GP padding therefore turned empty ground
            // into an Enemy hover and showed attack.cur.  Sample the actual rendered
            // C2 frame alpha instead, so only visible sprite pixels are hittable.
            try
            {
                Rect texUv = Runtime.LastTextureUvRectLikeOriginal.width > 0.0f && Runtime.LastTextureUvRectLikeOriginal.height > 0.0f
                    ? Runtime.LastTextureUvRectLikeOriginal
                    : new Rect(0.0f, 0.0f, 1.0f, 1.0f);
                float texU = Mathf.Lerp(texUv.xMin, texUv.xMax, uv.x);
                float texV = Mathf.Lerp(texUv.yMin, texUv.yMax, uv.y);
                alpha = Runtime.LastTexture.GetPixelBilinear(texU, texV).a;
                return alpha > 0.01f;
            }
            catch
            {
                alpha = 0.0f;
                return false;
            }
        }

        internal void SetMoveDestinationWorldLikeOriginal(Vector3 targetWorld, float speedOriginalPixelsPerSecond)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeMoveDestinationWorldLikeOriginal(Runtime, targetWorld, speedOriginalPixelsPerSecond);
        }

        internal void SetMoveDestinationRealLikeOriginal(float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeMoveDestinationRealLikeOriginal(Runtime, destRealX, destRealY, speedOriginalPixelsPerSecond, hasFinalFacingDir, finalFacingDir);
        }

        internal void SetValidatedDirectMoveDestinationRealLikeOriginal(
            float destRealX,
            float destRealY,
            float speedOriginalPixelsPerSecond,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            bool preciseBornPath,
            string source)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeValidatedDirectMoveDestinationRealLikeOriginal(
                    Runtime,
                    destRealX,
                    destRealY,
                    speedOriginalPixelsPerSecond,
                    hasFinalFacingDir,
                    finalFacingDir,
                    preciseBornPath,
                    source);
        }

        internal void SetMovePathRealLikeOriginal(Vector2[] pathReal, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir, bool preciseBornPath, string source)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeMovePathRealLikeOriginal(Runtime, pathReal, speedOriginalPixelsPerSecond, hasFinalFacingDir, finalFacingDir, preciseBornPath, source);
        }

        internal void SetFacingDirectionLikeOriginal(byte realDir)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeFacingLikeOriginal(Runtime, realDir);
        }

        internal void SetMovingLikeOriginal(bool moving)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeMovingFlagLikeOriginal(Runtime, moving);
        }

        internal void SetAttackStateMovementV405BLikeOriginal(bool preserveAttackPosture)
        {
            if (Runtime != null)
                Runtime.MovePreservesCombatPostureV405BLikeOriginal = preserveAttackPosture;
        }

        internal int GetRetreatRadiusV405BLikeOriginal()
        {
            if (Runtime == null || Runtime.Md == null) return 0;
            return Runtime.Md.RetreatRadius;
        }

        internal int GetPostureWeaponTypeV405BLikeOriginal()
        {
            return Runtime != null ? Runtime.PostureWeaponTypeLikeOriginal : -1;
        }

        internal void SetCombatPostureV322LikeOriginal(int weaponType, bool active)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeCombatPostureV322LikeOriginal(Runtime, weaponType, active);
        }

        internal void ApplyKeepPositionsGroundStateV415LikeOriginal(int state, string reason)
        {
            if (Owner != null && Runtime != null)
                Owner.ApplyRuntimeKeepPositionsGroundStateV415LikeOriginal(Runtime, state, reason);
        }

        internal bool IsKeepPositionsPostureReadyV415LikeOriginal(int state)
        {
            return Owner == null || Runtime == null ||
                   Owner.IsRuntimeKeepPositionsPostureReadyV415LikeOriginal(Runtime, state);
        }

        internal bool PlayAttackOneShotV325LikeOriginal(int attackState)
        {
            return Owner != null && Runtime != null &&
                   Owner.PlayRuntimeAttackOneShotV325LikeOriginal(Runtime, attackState);
        }

        internal void QueueSlowRechargeLikeOriginal(int attackMode, int pauseTicks)
        {
            if (Owner != null && Runtime != null)
                Owner.QueueRuntimeSlowRechargeLikeOriginal(Runtime, attackMode, pauseTicks);
        }

        internal bool BeginSlowRechargeLikeOriginal(int pauseTicks)
        {
            return Owner != null && Runtime != null &&
                   Owner.BeginRuntimeSlowRechargeLikeOriginal(Runtime, pauseTicks);
        }

        internal bool IsSlowRechargingLikeOriginal()
        {
            return Runtime != null && Runtime.State == C2UnitOriginalState.Recharge;
        }

        internal bool HasMoveTargetLikeOriginal()
        {
            return Runtime != null &&
                   (Runtime.HasMoveTargetLikeOriginal ||
                    Runtime.MoveDeferredUntilNeutralStandLikeOriginal);
        }

        internal bool TryGetSlowRechargeProgressLikeOriginal(
            int attackMode,
            out int remainingTicks,
            out int maximumTicks,
            out bool animationPlaying)
        {
            remainingTicks = 0;
            maximumTicks = 0;
            animationPlaying = false;
            return Owner != null && Runtime != null &&
                   Owner.TryGetRuntimeSlowRechargeProgressLikeOriginal(
                       Runtime, attackMode, out remainingTicks, out maximumTicks, out animationPlaying);
        }

        internal void ClearSlowRechargeDelayLikeOriginal()
        {
            if (Owner != null && Runtime != null)
                Owner.ClearRuntimeSlowRechargeDelayLikeOriginal(Runtime);
        }

        internal void InterruptSlowRechargeAnimationForExternalOrderV399LikeOriginal(string reason)
        {
            if (Owner != null && Runtime != null)
                Owner.InterruptRuntimeSlowRechargeAnimationForExternalOrderV399LikeOriginal(
                    Runtime, reason);
        }

        internal bool TryGetWeaponStartWorldLikeOriginal(int muzzleIndex, out Vector3 world)
        {
            world = Runtime != null ? Runtime.WorldPosition : Vector3.zero;
            return Owner != null && Runtime != null &&
                   Owner.TryGetRuntimeWeaponStartWorldLikeOriginal(
                       Runtime, muzzleIndex, out world);
        }

        internal bool AdvanceAttackFacingV410LikeOriginal(byte enemyDir)
        {
            return Owner != null && Runtime != null &&
                   Owner.AdvanceRuntimeAttackFacingV410LikeOriginal(Runtime, enemyDir);
        }

        internal bool AdvanceAttackFacingV411LikeOriginal(byte enemyDir, int needState)
        {
            return Owner != null && Runtime != null &&
                   Owner.AdvanceRuntimeAttackFacingV411LikeOriginal(Runtime, enemyDir, needState);
        }

        internal bool IsCombatPostureReadyV335LikeOriginal(int weaponType)
        {
            return Runtime != null &&
                   Runtime.State == C2UnitOriginalState.Stand &&
                   !Runtime.HasMoveTargetLikeOriginal &&
                   Runtime.PostureWeaponTypeLikeOriginal == weaponType &&
                   Runtime.LocalPostureWeaponTypeV411LikeOriginal == weaponType;
        }

        internal bool TryGetAttackTimingV335LikeOriginal(
            out int frame, out int frameCount, out int activeFrame, out bool attacking)
        {
            int previousFrame;
            return TryGetAttackTimingV415LikeOriginal(
                out previousFrame, out frame, out frameCount, out activeFrame, out attacking);
        }

        internal bool TryGetAttackTimingV415LikeOriginal(
            out int previousFrame, out int frame, out int frameCount, out int activeFrame, out bool attacking)
        {
            previousFrame = 0;
            frame = 0;
            frameCount = 0;
            activeFrame = 0;
            attacking = false;
            if (Runtime == null || Runtime.Md == null ||
                Runtime.CurrentAnimIndex < 0 ||
                Runtime.CurrentAnimIndex >= Runtime.Md.Animations.Count)
                return false;
            C2UnitOriginalRuntimeAndRendererV1.AnimModel anim =
                Runtime.Md.Animations[Runtime.CurrentAnimIndex];
            if (anim == null || anim.Frames.Count == 0) return false;
            frame = Mathf.Clamp(Runtime.CurrentFrameLong >> 8, 0, anim.Frames.Count - 1);
            previousFrame = Runtime.PreviousFrameAnimIndexV415LikeOriginal == Runtime.CurrentAnimIndex
                ? Mathf.Clamp(Runtime.PreviousFrameIndexV415LikeOriginal, 0, anim.Frames.Count - 1)
                : frame;
            frameCount = anim.Frames.Count;
            activeFrame = anim.ActiveFrame;
            // NewMon.cpp::AttackObjLink:
            //   if(!af) af=OBJ->NewAnm->NFrames/2; if(af<4) af=4;
            // 0xFF remains 0xFF; manufacturing a clipped active point would be wrong.
            if (activeFrame == 0)
                activeFrame = frameCount / 2;
            if (activeFrame < 4)
                activeFrame = 4;
            attacking = Runtime.State == C2UnitOriginalState.Attack;
            return true;
        }

        internal bool IsUnlimitedMotionV415LikeOriginal
        {
            get { return Runtime != null && Runtime.PreciseBornPathLikeOriginal; }
        }

        internal void SetMotionStateLikeOriginal(byte realDir, bool backMotion)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeMotionStateLikeOriginal(Runtime, realDir, backMotion);
        }

        internal void SetWalkPathFrameLikeOriginal(float totalPathReal, float rInFrameReal)
        {
            if (Owner != null && Runtime != null)
                Owner.SetRuntimeWalkPathFrameLikeOriginal(Runtime, totalPathReal, rInFrameReal);
        }
    }

    public sealed partial class C2UnitOriginalRuntime
    {
        public object ProbeRaw;
        internal object RawRecord;
        internal object RawMd;
        internal object ProbeObject;
        internal object MdObject;
        internal GameObject Root;
        internal string UnityProxyNameLikeOriginal;
        internal int VisibleLayerLikeOriginal;
        internal bool ActiveLikeOriginal = true;
        internal Quaternion WorldRotationLikeOriginal = Quaternion.identity;
        internal Vector3 WorldScaleLikeOriginal = Vector3.one;
        internal int DrawVisibleEpochV379LikeOriginal;
        internal bool LocalToWorldBasisReadyV379LikeOriginal;
        internal Quaternion LocalToWorldRotationV379LikeOriginal;
        internal Vector3 LocalToWorldScaleV379LikeOriginal;
        internal Matrix4x4 LocalToWorldBasisV379LikeOriginal;
        internal MeshFilter MeshFilter;
        internal MeshRenderer MeshRenderer;
        internal Mesh Mesh;
        internal static readonly int[] BodyQuadTrianglesLikeOriginal = { 0, 2, 1, 0, 3, 2 };
        internal Vector3[] BodyQuadVerticesLikeOriginal;
        internal Vector2[] BodyQuadUvsLikeOriginal;
        internal bool BodyQuadTrianglesAssignedLikeOriginal;
        internal Material Material;
        internal MeshFilter DepthMeshFilter;
        internal MeshRenderer DepthMeshRenderer;
        internal Material DepthMaterial;
        internal LineRenderer SelectionRing;
        internal GameObject SelectionRingObject;
        internal MeshFilter SelectionRingFilter;
        internal MeshRenderer SelectionRingRenderer;
        internal Mesh SelectionRingMesh;
        internal bool Selected;
        internal Vector3 WorldPosition;
        internal int CurrentAnimIndex;
        // Exact NewMon.cpp split: PostureWeaponType == NewState-1 (desired),
        // LocalPostureWeaponType == LocalNewState-1 (actually transformed state).
        // V410 conflated the two, which could skip PATTACK/UATTACK/TRANS stages.
        internal int PostureWeaponTypeLikeOriginal = -1;
        internal int LocalPostureWeaponTypeV411LikeOriginal = -1;
        internal int TransitionTargetLocalPostureV411LikeOriginal = int.MinValue;
        internal int PostureAfterMoveLikeOriginal = -1;
        internal bool MoveDeferredUntilNeutralStandLikeOriginal;
        // Multi.cpp::SendSelectedToXY -> GroupSendSelectedTo(...,Prio=128):
        // short movement while in attack state keeps the lowered/bayonet posture.
        internal bool MovePreservesCombatPostureV405BLikeOriginal;
        internal int PendingStandAnimIndexLikeOriginal = -1;
        internal int PendingPostureAfterNeutralLikeOriginal = -1;
        internal int CurrentFrameLong;
        // NewCurSpritePrev equivalent used by C2 AttackObjLink active-frame crossing.
        internal int PreviousFrameIndexV415LikeOriginal;
        internal int PreviousFrameAnimIndexV415LikeOriginal = -1;
        internal double AnimFrameLongRemainderLikeOriginal;
        internal bool FrameFinishedLikeOriginal;
        internal bool FrameFinishedLatchedForOrdersLikeOriginal;
        internal int RechargeTicksRemainingLikeOriginal;
        internal int RechargeTicksMaximumLikeOriginal;
        internal int RechargeAttackModeLikeOriginal = -1;
        internal int RealDirPrecise;
        internal int OctantInfo = 0xFF;
        internal float AnimFps;
        internal string LastFrameKey;
        internal string LastRenderedPackageLikeOriginal;
        internal int LastRenderedSpriteLikeOriginal = int.MinValue;
        internal int LastRenderedColorIdLikeOriginal = -1;
        internal int LastRenderedDxLikeOriginal;
        internal int LastRenderedDyLikeOriginal;
        internal bool LastRenderedMirrorLikeOriginal;
        internal int LastVisibilityCheckFrameLikeOriginal = -1;
        // MiniMap4X.cpp::DrawUnits builds the visible object list from the
        // camera cells.  A newly created unit is not drawable until that pass
        // admits it.  Defaulting these flags to true made every off-screen
        // unit decode/apply an animation frame on every simulation tick.
        internal bool VisibleWithinViewerMarginLikeOriginal;
        internal bool VisibleInOriginalDrawUnitsLikeOriginal;
        internal bool HiddenInsideBuildingLikeOriginal;
        internal int OriginalFogVisibilityValueLikeOriginal = 255;
        internal int LastAppliedFogVisibilityValueLikeOriginal = -1;
        internal MaterialPropertyBlock BodyPropertiesLikeOriginal;
        internal bool FrameUploadPendingLikeOriginal;
        internal bool RenderedByOriginalGpsBatchLikeOriginal;
        internal Texture2D LastTexture;
        internal Rect LastTextureUvRectLikeOriginal = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        internal int LastDecodedWidth;
        internal int LastDecodedHeight;
        internal int UnitOrder;
        internal float LastSelectionBrightnessMultiplierLikeOriginal = 1.0f;
        internal float SelectionRingMeshWidthLikeOriginal;
        internal float SelectionRingMeshHeightLikeOriginal;
        internal C2UnitOriginalState State;
        internal C2UnitOriginalAnimationState AnimState;
        internal float NextRestCheckTime;
        internal int RestRollCounter;
        internal bool HasMoveTargetLikeOriginal;
        internal bool CarryResourceMotionV223LikeOriginal;
        internal byte CarryResourceIdV223LikeOriginal;
        internal float FixedPixelWorldScaleLikeOriginal;
        internal float RuntimeRealXLikeOriginal;
        internal float RuntimeRealYLikeOriginal;
        internal float MoveTargetRealXLikeOriginal;
        internal float MoveTargetRealYLikeOriginal;
        internal float MoveSpeedOriginalPixelsPerSecondLikeOriginal;
        internal float BaseMoveSpeedOriginalPixelsPerSecondLikeOriginal;
        internal int OriginalMotionDistLikeOriginal;
        // OneObject::GroupSpeed is distinct from UnitSpeed and MotionDist.
        internal int OriginalGroupSpeedV431LikeOriginal;
        internal int OriginalUnitSpeedLikeOriginal = 64;
        // Cached equivalent of BR->NewBOrder == BRIGADEORDER_GOONROAD.
        internal bool OriginalGoOnRoadLikeOriginal;
        internal int OriginalRealDirPrecise256LikeOriginal;
        internal int OriginalMoreCharacterSpeedPercentLikeOriginal = 100;
        internal float TotalPathLikeOriginal;
        internal int MoveRInFrameLikeOriginal;
        internal byte FinalFacingDirLikeOriginal;
        internal bool HasFinalFacingDirLikeOriginal;
        internal bool SingleStepRotateAtPlaceActiveV352LikeOriginal;
        internal byte SingleStepRotateAtPlaceTargetV352LikeOriginal;
        // NewMon.cpp::AttackObjLink -> RotUnit per-object state. These two
        // fields were present in V410/V411 but were accidentally dropped while
        // merging V412 onto the newer 19.09 runtime.
        internal bool AttackRotateAtPlaceActiveV410LikeOriginal;
        internal byte AttackRotateAtPlaceTargetV410LikeOriginal;
        internal bool FinalRotUnitActiveV352LikeOriginal;
        internal byte FinalRotUnitTargetV352LikeOriginal;
        internal Vector2[] MovePathRealWaypointsLikeOriginal;
        internal int MovePathIndexLikeOriginal;
        internal float NextBlockedRouteRefreshLikeOriginal;
        internal bool PreciseBornPathLikeOriginal;
        internal int PreciseBornPathLastWaypointIndexLikeOriginal = -1;
        internal bool MovePathFinalAllowsUnitOverlapFinishLikeOriginal;
        internal bool MoveTargetAllowsUnitOverlapFinishLikeOriginal;
        internal int BlockedFinishRetargetAttemptLikeOriginal;
        internal bool NeedsGotoFinePositionLikeOriginal;
        internal int GotoFinePositionAttemptsLikeOriginal;
        internal float NextYieldAsideAllowedAtLikeOriginal;
        internal float OriginalBoidsPushForceXLikeOriginal;
        internal float OriginalBoidsPushForceYLikeOriginal;
        internal Vector3 MoveTargetWorldLikeOriginal;
        internal float MoveSpeedWorldLikeOriginal = 4.2f;
        internal bool CanBuildLikeOriginal;
        internal bool PioneerLikeOriginal;
        internal C2NeutralPeasantUnitInfoV2LikeOriginal Info;
        internal string BornExitAuditLikeOriginal;
        internal int BornExitOriginalPointCountLikeOriginal;
        internal bool BornExpectedFinalValidLikeOriginal;
        internal Vector2 BornExpectedFinalRealLikeOriginal;
        internal bool BornRawFinalValidLikeOriginal;
        internal Vector2 BornRawFinalRealLikeOriginal;
        internal bool BornClearancePointValidLikeOriginal;
        internal Vector2 BornClearancePointRealLikeOriginal;
        internal C2UnitOriginalRuntimeAndRendererV1.UnitProbe Probe;
        internal C2UnitOriginalRuntimeAndRendererV1.MdModel Md;
        internal string MdPath;
    }
}
