using System;
using System.Collections.Generic;
using Cossacks2Bridge.UnityAdapters.Maps.InternalBZip2;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    /// <summary>
    /// Clean terrain mode rebuilt from scratch.
    /// Only 2 systems live here:
    /// 1) battle cameras
    /// 2) single strict terrain path (build once, whole map, stripes)
    /// No roads, water, fog, helper meshes, texture catalogs, overlays, topology, or legacy fallback paths.
    /// </summary>
    public sealed partial class C2BattleTerrainMode : MonoBehaviour
    {
        internal interface IIRSSubmitMeshLikeOriginal
        {
            Mesh SubmitRuntimeMeshLikeOriginal { get; }
            Material SubmitRuntimeMaterialLikeOriginal { get; }
            int SubmitSubMeshIndexLikeOriginal { get; }
            string SubmitMeshNameLikeOriginal { get; }
            bool SubmitLogThisDrawLikeOriginal { get; }
        }

        public static float HorizontalScale = 16.0f;
        public static float VerticalScale = 1.0f;
        public static int StripeColumnWidth = 64;

        public static float CameraYaw = 45.0f;
        public static float CameraPitch = 55.0f;
        public static float StrictIsoYawDegrees = 30.0f;
        public static float StrictIsoRollDegrees = 0.0f;
        public static float StrictIsoScrollSpeed = 5.0f;
        public static float StrictIsoZoomStepPerWheel = 45.0f;
        public static float StrictIsoStepClamp = 4.0f;
        public static float FreeCameraMoveSpeed = 2200.0f;
        public static float FreeCameraVerticalSpeed = 1800.0f;
        public static float FreeCameraBoostMultiplier = 4.0f;
        public static float FreeCameraLookSensitivity = 0.12f;
        public static float MinCameraDistance = 250.0f;
        public static float MaxCameraDistance = 50000.0f;
        public static float StrictIsoCameraFactor = 3.5f;
        public static float StrictIsoBaseFovXDegrees = 20.0f;
        public static float StrictIsoEdgeAccel = 0.3f;
        public static float CameraDistanceMultiplier = 1.35f;
        public static float ScrollZoomSpeed = 1.15f;
        public static float OrbitSensitivity = 0.18f;
        public static float KeyboardPanSpeed = 2200.0f;
        public static float EdgePanSpeed = 2600.0f;
        public static float DragPanSpeed = 0.85f;
        public static float EdgeScrollMargin = 18.0f;
        public static float ShiftSpeedMultiplier = 10.0f;
        public static float ShiftZoomMultiplier = 10.0f;


        private const string RootName = "C2_BattleTerrainMode";
        private const string CameraName = "C2_BattleTerrainCamera";
        private const string StrictIsoCameraName = "C2_BattleTerrainCamera_Iso";
        private const string FreeCameraName = "C2_BattleTerrainCamera_Free";
        private const string TerrainRootName = "C2_StrictTerrain";
        private const float WorldZSign = -1.0f;

        private Cossacks2Bridge.UnityAdapters.MenuBootstrap _bootstrap;
        private Camera _camera;
        private Camera _strictIsoCamera;
        private Camera _freeCamera;
        private bool _freeCameraMode;

        private Vector3 _freeCameraPosition;
        private float _freeCameraYaw;
        private float _freeCameraPitch;
        private bool _freeCameraStateInitialized;
        private float _yaw;
        private float _pitch;
        private float _strictInitialMapX;
        private float _strictInitialMapY;
        private float _strictInitialZoom;
        private float _strictZoomTargetLikeOriginal;
        private Vector3 _initialPivot;
        private float _initialDistance;
        private float _initialYaw;
        private float _initialPitch;
        private float _nextCameraLogTime = -1.0f;
        private string _lastCameraLogSignature = "";

        private Vector3 _pivot;
        private float _distance;

        private float _strictMapX;
        private float _strictMapY;
        private float _strictZoom = 0.0f;
        private float _strictZoomTarget = 0.0f;
        private float _strictYawLikeOriginal;
        private float _strictRollLikeOriginal;
        private bool _strictCameraStateInitialized;
        private float _strictStepX;
        private float _strictStepY;
        private float _strictLastShiftTime;
        private float _strictDtaLikeOriginal;
        private int _strictZoomModeIndex;

        private ParsedMap _map;
        private string _selectedId = string.Empty;
        private string _mapRelativePath = string.Empty;
        private GameObject _terrainRoot;
        private GameObject _terrainGo;
        private Material _terrainMaterial;
        private Material _terrainBaseMaterial;
        private Material _terrainOverlayMaterial;
        private Bounds _terrainBounds;
        private bool _terrainBuilt;
        private OriginalTerrainKernelConfig _lastBuiltTerrainKernel;
        private bool _hasLastBuiltTerrainKernel;

        internal Camera GetActiveBattleCameraLikeOriginal() => _camera;
        internal Camera GetStrictIsoCameraLikeOriginal() => _strictIsoCamera;
        internal bool IsStrictIsoSurfaceModeLikeOriginal => !_freeCameraMode;

        private sealed partial class ParsedMap
        {
            public string SourcePath = string.Empty;
            public string HeaderMagic = string.Empty;
            public int Addsh;
            public int HeaderStoredVertInLine;
            public int HeaderStoredMaxTH;
            public int VertInLine;
            public int MaxTH;
            public int MaxSector;
            public int MAPSX;
            public int MAPSY;
            public int MinMapX;
            public int MinMapY;
            public int MaxMapX;
            public int MaxMapY;
            public bool HasMapSizeChunk;
            public bool HasSurfaceChunk;
            public short[] Heights = Array.Empty<short>();
            public byte[] XYShift = Array.Empty<byte>();
            public ParsedSurfaceMode SurfaceMode = ParsedSurfaceMode.Unknown;
            public bool IsMeshSurface;
            public ParsedMeshSurfaceVertex[] MeshSurfaceVertices = Array.Empty<ParsedMeshSurfaceVertex>();
            public Vector3[] MeshVertices = Array.Empty<Vector3>();
            public int[] MeshIndices = Array.Empty<int>();
        }

        private enum ParsedSurfaceMode
        {
            Unknown = 0,
            OldSurface = 1,
            NewSurface = 2
        }

        private struct ParsedMeshSurfaceVertex
        {
            public short X;
            public short Y;
            public short Z;
            public uint Color;
            public byte Shadow;
            public byte Facture;
            public sbyte NX;
            public sbyte NY;
            public sbyte NZ;
            public byte Reserved;
        }

        private struct OriginalTerrainKernelConfig
        {
            public int MinCellX;
            public int MaxCellXExclusive;
            public int MinCellY;
            public int MaxCellYExclusive;
            public float TQuantWorld;
            public float HQuantWorld;
            public float SQuantWorld;
            public float BackingStepXWorld;
            public float BackingStepZWorld;
            public float BackingOddColumnOffsetZWorld;
            public float CenterX;
            public float CenterZ;
            public float HeightScale;
            public float YShiftWorldScale;
            public int ScShift;
        }

        private struct OriginalCellTriangulationLikeOriginal
        {
            public int V0;
            public int V1;
            public int V2;
            public int V3;
            public float X0;
            public float X1;
            public float S0;
            public float S1;
            public float S2;
            public float S3;
            public int FirstA;
            public int FirstB;
            public int FirstC;
            public int SecondA;
            public int SecondB;
            public int SecondC;
        }

        private struct CellVertexPayloadLikeOriginal
        {
            public int Index;
            public float RawX;
            public float RawZ;
            public Vector3 World;

            public CellVertexPayloadLikeOriginal(int index, float rawX, float rawZ, Vector3 world)
            {
                Index = index;
                RawX = rawX;
                RawZ = rawZ;
                World = world;
            }
        }

        private sealed class KernelStripeData
        {
            public readonly List<Vector3> Vertices;
            public readonly List<Color> Colors;
            public readonly List<int> Triangles;
            public readonly List<int> OverlayTriangles;
            public readonly List<Vector2> Uv0;
            public readonly List<Vector2> Uv1;
            public readonly List<Vector2> Uv2;
            public Bounds Bounds;
            public bool HasBounds;

            public KernelStripeData(int estimatedTriangles)
            {
                int vertexCapacity = Mathf.Max(estimatedTriangles * 3, 6);
                Vertices = new List<Vector3>(vertexCapacity);
                Colors = new List<Color>(vertexCapacity);
                Triangles = new List<int>(vertexCapacity);
                OverlayTriangles = new List<int>(vertexCapacity);
                Uv0 = new List<Vector2>(vertexCapacity);
                Uv1 = new List<Vector2>(vertexCapacity);
                Uv2 = new List<Vector2>(vertexCapacity);
                Bounds = new Bounds(Vector3.zero, Vector3.zero);
                HasBounds = false;
            }
        }

        public static void OpenFromBattles(Cossacks2Bridge.UnityAdapters.MenuBootstrap bootstrap)
        {
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
                SafeDestroy(oldRoot);

            var go = new GameObject(RootName);
            var mode = go.AddComponent<C2BattleTerrainMode>();
            mode.InitializeSafe(bootstrap);
        }

        private void InitializeSafe(Cossacks2Bridge.UnityAdapters.MenuBootstrap bootstrap)
        {
            try
            {
                Initialize(bootstrap);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[C2] Clean terrain mode failed:\n" + ex);
            }
        }

        private void Initialize(Cossacks2Bridge.UnityAdapters.MenuBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
            _yaw = CameraYaw;
            _pitch = CameraPitch;
            DestroyUiAndOldMode(gameObject);

            if (!TryResolveSelectedMapPath(out _mapRelativePath, out _selectedId, out string resolveError))
                throw new InvalidOperationException("Resolve map failed: " + resolveError);

            if (!TryParseMap(_bootstrap.Fs, _mapRelativePath, out _map, out string parseError))
                throw new InvalidOperationException("Parse map failed: " + parseError);

            UnityEngine.Debug.Log($"[C2:MAP] Selected map id='{_selectedId}' path='{_mapRelativePath}'");
            UnityEngine.Debug.Log($"[C2:MAP] Parsed clean map magic={_map.HeaderMagic} addsh={_map.Addsh} grid={_map.VertInLine}x{_map.MaxTH} stored={_map.HeaderStoredVertInLine}x{_map.HeaderStoredMaxTH} mpsz=({_map.MinMapX},{_map.MinMapY})->({_map.MaxMapX},{_map.MaxMapY})");

            BuildWorld();
        }

        private void BuildWorld()
        {
            CreateCamera();
            CreateTerrainObject(_selectedId);
            InitializeStrictIsoCameraStateLikeOriginal(forceCenter: true);
            InitializeFreeCameraStateFromTerrainBounds();
            _freeCameraMode = false;
            ApplyActiveBattleCameraMode(forceLog: true);
            UpdateCameraTransform();
        }

        private void CreateTerrainObject(string selectedId)
        {
            if (_terrainRoot != null)
                SafeDestroy(_terrainRoot);

            _hasLastBuiltTerrainKernel = false;
            _terrainRoot = new GameObject(TerrainRootName + "_" + selectedId);
            _terrainGo = _terrainRoot;
            _terrainRoot.transform.SetParent(transform, false);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            BuildStrictWholeMapTerrainLikeOriginal(_map, _terrainRoot.transform, out _terrainBounds);
            BuildRoadsLayerLikeOriginal(_map, _terrainRoot.transform, ref _terrainBounds);
            BuildWaterLayerV1LikeOriginal(_map, _terrainRoot.transform, ref _terrainBounds);
            sw.Stop();

            _terrainBuilt = true;
        }

        private void CreateCamera()
        {
            _strictIsoCamera = CreateBattleCameraInstance(StrictIsoCameraName, 1000.0f, true);
            _freeCamera = CreateBattleCameraInstance(FreeCameraName, 1001.0f, false);
            _freeCameraMode = false;
            ApplyActiveBattleCameraMode(forceLog: true);
        }

        private Camera CreateBattleCameraInstance(string name, float depth, bool strictIso)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(24, 28, 34, 255);
            cam.nearClipPlane = strictIso ? 1.0f : 0.3f;
            cam.farClipPlane = 1000000.0f;
            cam.depth = depth;
            cam.fieldOfView = strictIso ? 20.0f : 35.0f;
            cam.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.useOcclusionCulling = false;
            cam.depthTextureMode = DepthTextureMode.None;
            cam.enabled = false;
            return cam;
        }

        private void ApplyActiveBattleCameraMode(bool forceLog)
        {
            bool freeActive = _freeCameraMode && _freeCamera != null;

            if (_strictIsoCamera != null)
                _strictIsoCamera.enabled = !freeActive;
            if (_freeCamera != null)
                _freeCamera.enabled = freeActive;

            _camera = freeActive ? _freeCamera : _strictIsoCamera;

            if (forceLog && !freeActive)
                UnityEngine.Debug.Log($"[C2:CAM] mode=strict-iso active={(_camera != null ? _camera.name : "<null>")}");
        }

        private void ToggleBattleCameraModeLikeOriginal()
        {
            if (_freeCamera == null)
                return;

            if (!_freeCameraStateInitialized)
                InitializeFreeCameraStateFromTerrainBounds();

            _freeCameraMode = !_freeCameraMode;
            ApplyActiveBattleCameraMode(forceLog: true);
            UpdateCameraTransform();
        }

        private void InitializeStrictIsoCameraStateLikeOriginal(bool forceCenter)
        {
            float scale = GetStrictScaleLikeOriginal();
            float viewVol = GetStrictViewVolLikeOriginal(scale);
            float realLy = GetStrictRealLyLikeOriginal(scale);

            if (!_strictCameraStateInitialized || forceCenter)
            {
                float centerMapX = 0.0f;
                float centerMapY = 0.0f;
                ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
                if (runtimeMapLikeOriginal != null)
                {
                    centerMapX = (runtimeMapLikeOriginal.MinMapX + runtimeMapLikeOriginal.MaxMapX) * 0.5f;
                    centerMapY = (runtimeMapLikeOriginal.MinMapY + runtimeMapLikeOriginal.MaxMapY) * 0.5f;
                }

                _strictMapX = centerMapX - viewVol / (2.0f * 32.0f);
                _strictMapY = centerMapY - realLy / 32.0f;
                _strictZoom = 0.0f;
                _strictYawLikeOriginal = Mathf.PI / 6.0f;
                _strictZoomTargetLikeOriginal = 0.0f;
                _strictZoomModeIndex = 0;
                _strictRollLikeOriginal = 0.0f;
                _strictStepX = 0.0f;
                _strictStepY = 0.0f;
                _strictLastShiftTime = Time.realtimeSinceStartup * 1000.0f;
                _strictDtaLikeOriginal = 0.0f;
                _strictCameraStateInitialized = true;
            }

            ClampStrictIsoMapStateLikeOriginal(viewVol, realLy, scale);
            _strictInitialMapX = _strictMapX;
            _strictInitialMapY = _strictMapY;
            _strictInitialZoom = _strictZoom;
        }

        private void InitializeFreeCameraStateFromTerrainBounds()
        {
            if (_strictIsoCamera != null)
                ApplyStrictIsoCameraLikeOriginal();

            if (_strictIsoCamera != null)
            {
                Vector3 e = _strictIsoCamera.transform.eulerAngles;
                _freeCameraPosition = _strictIsoCamera.transform.position;
                _freeCameraYaw = e.y;
                _freeCameraPitch = NormalizeAngleLikeOriginal(e.x);
                _pivot = _terrainBuilt ? _terrainBounds.center : _strictIsoCamera.transform.position + _strictIsoCamera.transform.forward * 2048.0f;
                _distance = Vector3.Distance(_freeCameraPosition, _pivot);
            }
            else
            {
                InitializeFreeCameraFromTerrainBounds();
                Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
                _freeCameraYaw = _yaw;
                _freeCameraPitch = Mathf.Clamp(_pitch, -85.0f, 85.0f);
                _freeCameraPosition = _pivot - rotation * Vector3.forward * _distance;
            }

            _freeCameraStateInitialized = true;

            if (_freeCamera != null)
            {
                _freeCamera.transform.position = _freeCameraPosition;
                _freeCamera.transform.rotation = Quaternion.Euler(_freeCameraPitch, _freeCameraYaw, 0.0f);
            }
        }

        private void Update()
        {
            if (WasFreeCameraTogglePressed())
                ToggleBattleCameraModeLikeOriginal();

            if (WasHomePressed())
                ResetStrictIsoCameraStateLikeOriginal();


            bool speedHeld = IsSpeedHeld();
            if (_freeCameraMode)
                UpdateFreeCameraInput(speedHeld);
            else
                UpdateStrictIsoCameraInputLikeOriginal(speedHeld);

            UpdateCameraTransform();
        }

        private bool UpdateStrictIsoCameraInputLikeOriginal(bool speedHeld)
        {
            if (!_strictCameraStateInitialized)
                InitializeStrictIsoCameraStateLikeOriginal(forceCenter: true);

            bool dirty = false;
            if (WasCameraModeTogglePressed())
            {
                _strictZoomModeIndex = (_strictZoomModeIndex + 1) % 3;
                _strictZoomTargetLikeOriginal = GetStrictZoomHeightByModeLikeOriginal(_strictZoomModeIndex);
                dirty = true;
            }
            float scale = GetStrictScaleLikeOriginal();
            float viewVol = GetStrictViewVolLikeOriginal(scale);
            float realLy = GetStrictRealLyLikeOriginal(scale);

            bool moveX = false;
            bool moveY = false;
            Vector2 pointer = ReadPointerPosition();
            float x = pointer.x;
            float y = pointer.y;
            float yTop = Mathf.Max(0.0f, Screen.height - y);

            Vector2 keyMove = ReadKeyboardMove();
            if (keyMove.x > 0.0f) { _strictStepX = 4.0f; moveX = true; }
            if (keyMove.x < 0.0f) { _strictStepX = -4.0f; moveX = true; }
            if (keyMove.y > 0.0f) { _strictStepY = -4.0f; moveY = true; }
            if (keyMove.y < 0.0f) { _strictStepY = 4.0f; moveY = true; }

            bool pointerInside = Application.isFocused && x >= 0.0f && y >= 0.0f && x < Screen.width && y < Screen.height;
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (pointerInside && !pointerOverUi)
            {
                if (x < 6.0f)
                {
                    if (_strictStepX > 0.0f) _strictStepX = 0.0f;
                    _strictStepX -= StrictIsoEdgeAccel;
                    if (_strictStepX < -StrictIsoStepClamp) _strictStepX = -StrictIsoStepClamp;
                    moveX = true;
                }
                if (yTop < 6.0f)
                {
                    if (_strictStepY > 0.0f) _strictStepY = 0.0f;
                    _strictStepY -= StrictIsoEdgeAccel;
                    if (_strictStepY < -StrictIsoStepClamp) _strictStepY = -StrictIsoStepClamp;
                    moveY = true;
                }
                if (x > Screen.width - 6.0f)
                {
                    if (_strictStepX < 0.0f) _strictStepX = 0.0f;
                    _strictStepX += StrictIsoEdgeAccel;
                    if (_strictStepX > StrictIsoStepClamp) _strictStepX = StrictIsoStepClamp;
                    moveX = true;
                }
                if (yTop > Screen.height - 6.0f)
                {
                    if (_strictStepY < 0.0f) _strictStepY = 0.0f;
                    _strictStepY += StrictIsoEdgeAccel;
                    if (_strictStepY > StrictIsoStepClamp) _strictStepY = StrictIsoStepClamp;
                    moveY = true;
                }
            }

            // literal-like damping from original mapa.cpp
            if (_strictStepX != 0.0f && !moveX)
            {
                if (_strictStepX > 0.0f) _strictStepX -= StrictIsoEdgeAccel * 2.0f;
                if (_strictStepX < 0.0f) _strictStepX += StrictIsoEdgeAccel * 2.0f;
                if (Mathf.Abs(_strictStepX) < StrictIsoEdgeAccel * 2.0f) _strictStepX = 0.0f;
            }
            if (_strictStepY != 0.0f && !moveY)
            {
                if (_strictStepY > 0.0f) _strictStepY -= 1.0f;
                if (_strictStepY < 0.0f) _strictStepY += 1.0f;
                if (Mathf.Abs(_strictStepY) < 1.0f) _strictStepY = 0.0f;
            }

            float nowMs = Time.realtimeSinceStartup * 1000.0f;
            if (_strictLastShiftTime <= 0.0f)
                _strictLastShiftTime = nowMs;
            float dt = (nowMs - _strictLastShiftTime) * (StrictIsoScrollSpeed + 5.0f) / 8.0f;
            if (dt > 300.0f) dt = 300.0f;
            _strictDtaLikeOriginal = (_strictDtaLikeOriginal * 11.0f + dt) / 12.0f;
            _strictLastShiftTime = nowMs;

            int stepx = (int)_strictStepX;
            int stepy = (int)_strictStepY;
            if (stepx != 0 || stepy != 0)
            {
                float dx = stepx * _strictDtaLikeOriginal / 2.0f / 40.0f;
                float dy = stepy * _strictDtaLikeOriginal / 2.0f / 40.0f;
                if (speedHeld)
                {
                    dx *= 10.0f;
                    dy *= 10.0f;
                }

                // Literal-like mapa.cpp path:
                // CD = ICam->GetDir(); CD.z = 0; CD.normalize(); CD.reverse();
                // ShiftCamera(dx*CD.y + dy*CD.x, dy*CD.y - dx*CD.x)
                Vector3 cdUnity = _strictIsoCamera != null ? _strictIsoCamera.transform.forward : MapOriginalDirToUnity(_strictYawLikeOriginal, _strictRollLikeOriginal);
                cdUnity.y = 0.0f;
                if (cdUnity.sqrMagnitude < 0.000001f)
                    cdUnity = MapOriginalDirToUnity(_strictYawLikeOriginal, _strictRollLikeOriginal);
                cdUnity.y = 0.0f;
                cdUnity.Normalize();
                cdUnity = -cdUnity;

                float cdx = cdUnity.x;
                float cdy = cdUnity.z * WorldZSign;
                _strictMapX += dx * cdy + dy * cdx;
                _strictMapY += dy * cdy - dx * cdx;
                dirty = true;
            }

            // Stepped F7 zoom path for retail-like gameplay:
            // - base angle stays fixed at the original default PI/6
            // - F7 cycles camera height levels using the original zoom amplitude (1100) as one lift unit
            // - no wheel/PageUp/PageDown in strict mode
            float zoomStep = Mathf.Max(1.0f, dt * 3.0f);
            if (!Mathf.Approximately(_strictZoom, _strictZoomTargetLikeOriginal))
            {
                _strictZoom = Mathf.MoveTowards(_strictZoom, _strictZoomTargetLikeOriginal, zoomStep);
                _strictZoom = Mathf.Clamp(_strictZoom, 0.0f, GetStrictZoomHeightByModeLikeOriginal(2));
                dirty = true;
            }
            _strictYawLikeOriginal = Mathf.PI / 6.0f;

            if (dirty)
                ClampStrictIsoMapStateLikeOriginal(viewVol, realLy, scale);

            return dirty;
        }

        private void UpdateFreeCameraControlsLikeOriginal(bool speedHeld)
        {
            UpdateFreeCameraInput(speedHeld);
        }

        private void UpdateCameraTransform()
        {
            if (_freeCameraMode)
            {
                ApplyFreeCameraTransform();
                _camera = _freeCamera;
            }
            else
            {
                if (_strictIsoCamera != null)
                    ApplyStrictIsoCameraLikeOriginal();
                _camera = _strictIsoCamera;
            }

            if (_camera == null)
                return;

            UpdateWaterReflectionTarget(force: false);
            ApplyWaterReflectionParams(forceLog: false);

            Vector3 lookTarget = _freeCameraMode ? (_camera.transform.position + _camera.transform.forward * Mathf.Max(_distance, 1.0f)) : _pivot;
            Vector3 toPivot = (lookTarget - _camera.transform.position).normalized;
            float lookDot = Vector3.Dot(_camera.transform.forward.normalized, toPivot);
            MaybeLogCameraState(lookDot, false);
        }

        private void ApplyStrictIsoCameraLikeOriginal()
        {
            if (_strictIsoCamera == null)
                return;

            if (!_strictCameraStateInitialized)
                InitializeStrictIsoCameraStateLikeOriginal(forceCenter: true);

            float scale = GetStrictScaleLikeOriginal();
            float viewVol = GetStrictViewVolLikeOriginal(scale);
            float realLy = GetStrictRealLyLikeOriginal(scale);
            ClampStrictIsoMapStateLikeOriginal(viewVol, realLy, scale);

            float yawRad = _strictYawLikeOriginal;
            float rollRad = _strictRollLikeOriginal;
            Vector3 dir = MapOriginalDirToUnity(yawRad, rollRad);
            float strictDistance = viewVol * StrictIsoCameraFactor + _strictZoom;
            strictDistance = Mathf.Clamp(strictDistance, MinCameraDistance, MaxCameraDistance);

            float smaplx = viewVol / 32.0f;
            float smaply = realLy / 32.0f;
            float centerMapX = 0.0f;
            float centerMapY = 0.0f;
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal != null)
            {
                centerMapX = (runtimeMapLikeOriginal.MinMapX + runtimeMapLikeOriginal.MaxMapX) * 0.5f;
                centerMapY = (runtimeMapLikeOriginal.MinMapY + runtimeMapLikeOriginal.MaxMapY) * 0.5f;
            }

            float kernelBackingX = _hasLastBuiltTerrainKernel ? _lastBuiltTerrainKernel.BackingStepXWorld : HorizontalScale;
            float kernelBackingZ = _hasLastBuiltTerrainKernel ? _lastBuiltTerrainKernel.BackingStepZWorld : HorizontalScale;
            float focusWorldX = ((_strictMapX + smaplx * 0.5f) - centerMapX) * kernelBackingX;
            float focusWorldZ = ((_strictMapY + smaply) - centerMapY) * kernelBackingZ * WorldZSign;
            float focusWorldY = _terrainBuilt ? _terrainBounds.center.y : 0.0f;
            Vector3 focus = new Vector3(focusWorldX, focusWorldY, focusWorldZ);
            Vector3 pos = focus - dir * strictDistance;

            Quaternion rotation = BuildStrictIsoRotationLikeOriginal(dir);
            _strictIsoCamera.transform.SetPositionAndRotation(pos, rotation);
            // V81: strict-isometric gameplay camera uses orthographic projection.
            // Reason: buildings/units are sprite meshes in Unity world. With perspective camera they
            // stretch/compress against each other when the camera height/zoom changes. Original engine
            // compensates this in DrawSpriteBuilding via GetPseudoProjectionTM/ALIGN_WITH_3POINTS in
            // screen space. Until the full screen-space sprite renderer is ported, orthographic strict
            // camera is the safe non-destructive fix: it removes perspective deformation for all sprites
            // without moving building logical roots, passability, cursors, selection or LineSort.
            _strictIsoCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

            float aspect = Mathf.Max(1.0f, _strictIsoCamera.pixelRect.width) / Mathf.Max(1.0f, _strictIsoCamera.pixelRect.height);
            float fovxRad = StrictIsoBaseFovXDegrees * Mathf.Deg2Rad;
            float fovyRad = 2.0f * Mathf.Atan(Mathf.Tan(fovxRad * 0.5f) / Mathf.Max(0.0001f, aspect));
            _strictIsoCamera.fieldOfView = Mathf.Clamp(fovyRad * Mathf.Rad2Deg, 1.0f, 179.0f);
            _strictIsoCamera.orthographic = true;
            _strictIsoCamera.orthographicSize = Mathf.Max(1.0f, Mathf.Tan(fovyRad * 0.5f) * strictDistance);

            Bounds sceneBounds = _terrainBuilt
                ? _terrainBounds
                : new Bounds(focus, new Vector3(4096.0f, 2048.0f, 4096.0f));

            Vector3 closest = sceneBounds.ClosestPoint(pos);
            float distToScene = Vector3.Distance(pos, closest);
            if (distToScene < 0.001f)
                distToScene = Mathf.Max(1.0f, strictDistance * 0.25f);

            float camZn = Mathf.Clamp(distToScene * 0.01f, 0.5f, 5.0f);

            float sceneRadius = sceneBounds.extents.magnitude;
            float camZf = Mathf.Max(camZn + 1000.0f, Vector3.Distance(pos, sceneBounds.center) + sceneRadius + 4096.0f);

            _strictIsoCamera.nearClipPlane = camZn;
            _strictIsoCamera.farClipPlane = camZf;
            _strictIsoCamera.ResetProjectionMatrix();

            _pivot = focus;
            _distance = strictDistance;
        }

        private static void ApplyStrictIsoProjectionLikeOriginal(Camera cam, float zn, float zf)
        {
            if (cam == null)
                return;

            float aspect = Mathf.Max(1.0f, cam.pixelRect.width) / Mathf.Max(1.0f, cam.pixelRect.height);
            float fovx = StrictIsoBaseFovXDegrees * Mathf.Deg2Rad;
            float fovy = 2.0f * Mathf.Atan(Mathf.Tan(fovx * 0.5f) / Mathf.Max(0.0001f, aspect));
            cam.fieldOfView = Mathf.Clamp(fovy * Mathf.Rad2Deg, 1.0f, 179.0f);
            cam.nearClipPlane = Mathf.Max(0.01f, zn);
            cam.farClipPlane = Mathf.Max(cam.nearClipPlane + 1.0f, zf);
            cam.ResetProjectionMatrix();
        }

        private static Quaternion BuildStrictIsoRotationLikeOriginal(Vector3 dir)
        {
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, dir);
            if (right.sqrMagnitude < 0.000001f)
                right = Vector3.right;
            right.Normalize();
            up = Vector3.Cross(dir, right).normalized;

            Matrix4x4 basis = Matrix4x4.identity;
            basis.SetColumn(0, new Vector4(right.x, right.y, right.z, 0.0f));
            basis.SetColumn(1, new Vector4(up.x, up.y, up.z, 0.0f));
            basis.SetColumn(2, new Vector4(dir.x, dir.y, dir.z, 0.0f));
            return basis.rotation;
        }

        private static float NormalizeAngleLikeOriginal(float degrees)
        {
            while (degrees > 180.0f) degrees -= 360.0f;
            while (degrees < -180.0f) degrees += 360.0f;
            return degrees;
        }


        private float GetStrictScaleLikeOriginal()
        {
            return Mathf.Pow(2.0f, Mathf.Clamp(GetStrictScShiftLikeOriginal(), 0, 2));
        }

        private float GetStrictViewVolLikeOriginal(float scale)
        {
            Camera activeCamera = GetActiveBattleCameraLikeOriginal();
            float widthLikeOriginal = activeCamera != null ? Mathf.Max(1.0f, activeCamera.pixelRect.width) : Mathf.Max(1.0f, Screen.width);
            return widthLikeOriginal * scale;
        }

        private float GetStrictRealLyLikeOriginal(float scale)
        {
            Camera activeCamera = GetActiveBattleCameraLikeOriginal();
            float heightLikeOriginal = activeCamera != null ? Mathf.Max(1.0f, activeCamera.pixelRect.height) : Mathf.Max(1.0f, Screen.height);
            return heightLikeOriginal * scale;
        }

        private int GetStrictScShiftLikeOriginal()
        {
            return Mathf.Clamp(_strictZoomModeIndex, 0, 2);
        }

        private static float GetStrictZoomHeightByModeLikeOriginal(int mode)
        {
            // Base angle is fixed. One F7 lift step uses the original zoom amplitude (1100 units).
            switch (mode)
            {
                case 1:
                    return 1100.0f;
                case 2:
                    return 2200.0f;
                default:
                    return 0.0f;
            }
        }

        private void ClampStrictIsoMapStateLikeOriginal(float viewVol, float realLy, float scale)
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null)
                return;

            float smaplx = viewVol / 32.0f;
            float smaply = realLy / 32.0f;

            float minX = runtimeMapLikeOriginal.MinMapX;
            float minY = runtimeMapLikeOriginal.MinMapY;
            float maxX = Mathf.Max(minX + smaplx, runtimeMapLikeOriginal.MaxMapX);
            float maxY = Mathf.Max(minY + smaply, runtimeMapLikeOriginal.MaxMapY);

            if (_strictMapX < minX) _strictMapX = minX;
            if (_strictMapY < minY) _strictMapY = minY;
            if (_strictMapX + smaplx > maxX) _strictMapX = maxX - smaplx;
            if (_strictMapY + smaply > maxY) _strictMapY = maxY - smaply;
        }

        private static Vector3 MapOriginalDirToUnity(float yawRad, float rollRad)
        {
            float ox = 0.0f;
            float oy = -Mathf.Cos(yawRad);
            float oz = -Mathf.Sin(yawRad);

            if (Mathf.Abs(rollRad) > 0.000001f)
            {
                float cr = Mathf.Cos(rollRad);
                float sr = Mathf.Sin(rollRad);
                float rx = ox * cr - oy * sr;
                float ry = ox * sr + oy * cr;
                ox = rx;
                oy = ry;
            }

            return new Vector3(ox, oz, oy * WorldZSign).normalized;
        }

        private bool WasCameraModeTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F7);
#endif
        }

        private bool WasFreeCameraTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.insertKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Insert);
#endif
        }

        private bool WasHomePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Home);
#endif
        }

        private bool IsOrbitHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private bool IsSpeedHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        private Vector2 ReadPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(-1.0f, -1.0f);
#else
            return Input.mousePosition;
#endif
        }

        private Vector2 ReadKeyboardMove()
        {
            Vector2 move = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return move;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1.0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1.0f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1.0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1.0f;
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1.0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1.0f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1.0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1.0f;
#endif
            return move.sqrMagnitude > 1.0f ? move.normalized : move;
        }

        private Vector2 ReadPointerDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        private float ReadScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120.0f : 0.0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        private bool IsPageUpHeldLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.pageUpKey.isPressed;
#else
            return Input.GetKey(KeyCode.PageUp);
#endif
        }

        private bool IsPageDownHeldLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.pageDownKey.isPressed;
#else
            return Input.GetKey(KeyCode.PageDown);
#endif
        }

        private void ApplyFreeCameraTransform()
        {
            if (_freeCamera == null)
                return;

            _freeCameraPitch = Mathf.Clamp(_freeCameraPitch, -85.0f, 85.0f);
            Quaternion rotation = Quaternion.Euler(_freeCameraPitch, _freeCameraYaw, 0.0f);
            _freeCamera.transform.SetPositionAndRotation(_freeCameraPosition, rotation);

            _yaw = _freeCameraYaw;
            _pitch = _freeCameraPitch;
            _pivot = _freeCameraPosition + rotation * Vector3.forward * Mathf.Max(_distance, 1.0f);
        }

        private void InitializeFreeCameraFromTerrainBounds()
        {
            TryGetTerrainWorldBoundsLikeOriginal(out Bounds worldBounds);
            Vector3 scaledExtents = worldBounds.extents;
            _pivot = worldBounds.center;
            _distance = Mathf.Clamp(Mathf.Max(scaledExtents.x, scaledExtents.z, 1.0f) * CameraDistanceMultiplier, MinCameraDistance, MaxCameraDistance);
            _initialPivot = _pivot;
            _initialDistance = _distance;
            _initialYaw = _yaw;
            _initialPitch = _pitch;
        }

        private void ResetStrictIsoCameraStateLikeOriginal()
        {
            if (!_strictCameraStateInitialized)
                InitializeStrictIsoCameraStateLikeOriginal(forceCenter: true);

            _strictMapX = _strictInitialMapX;
            _strictMapY = _strictInitialMapY;
            _strictZoom = _strictInitialZoom;
            _strictYawLikeOriginal = Mathf.PI / 6.0f;
            _strictZoomTargetLikeOriginal = _strictInitialZoom;
            _strictZoomModeIndex = 0;
            _strictRollLikeOriginal = 0.0f;
            _strictStepX = 0.0f;
            _strictStepY = 0.0f;
            _strictLastShiftTime = Time.realtimeSinceStartup * 1000.0f;
            _strictDtaLikeOriginal = 0.0f;
        }

        private bool UpdateFreeCameraInput(bool speedHeld)
        {
            if (!_freeCameraStateInitialized)
                InitializeFreeCameraStateFromTerrainBounds();

            bool dirty = false;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float moveSpeed = FreeCameraMoveSpeed * (speedHeld ? FreeCameraBoostMultiplier : 1.0f);
            float verticalSpeed = FreeCameraVerticalSpeed * (speedHeld ? FreeCameraBoostMultiplier : 1.0f);

            Vector2 move = ReadKeyboardMove();
            float vertical = 0.0f;
            if (IsPageUpHeldLikeOriginal())
                vertical += 1.0f;
            if (IsPageDownHeldLikeOriginal())
                vertical -= 1.0f;

            Quaternion rotation = Quaternion.Euler(_freeCameraPitch, _freeCameraYaw, 0.0f);
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = Vector3.up;

            Vector3 deltaMove = (forward * move.y + right * move.x) * moveSpeed * dt + up * (vertical * verticalSpeed * dt);
            if (deltaMove.sqrMagnitude > 0.0f)
            {
                _freeCameraPosition += deltaMove;
                dirty = true;
            }

            if (IsOrbitHeld())
            {
                Vector2 delta = ReadPointerDelta();
                if (delta.sqrMagnitude > 0.0f)
                {
                    _freeCameraYaw += delta.x * FreeCameraLookSensitivity;
                    _freeCameraPitch -= delta.y * FreeCameraLookSensitivity;
                    dirty = true;
                }
            }

            float scroll = ReadScrollDelta();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _freeCameraPosition += forward * (scroll * moveSpeed * 0.15f);
                dirty = true;
            }

            return dirty;
        }

        private void MaybeLogCameraState(float lookDot, bool force)
        {
            if (_camera == null)
                return;

            string sig = _freeCameraMode
                ? $"{Mathf.RoundToInt(_pivot.x)},{Mathf.RoundToInt(_pivot.y)},{Mathf.RoundToInt(_pivot.z)}|{Mathf.RoundToInt(_distance)}|{Mathf.RoundToInt(_yaw * 10.0f)}|{Mathf.RoundToInt(_pitch * 10.0f)}"
                : $"strict|{_strictMapX:0.###}|{_strictMapY:0.###}|{_strictZoom:0.###}|{Mathf.RoundToInt(_distance)}";
            float now = Time.unscaledTime;
            if (!force)
            {
                if (sig == _lastCameraLogSignature && now < _nextCameraLogTime)
                    return;
                if (now < _nextCameraLogTime)
                    return;
            }

            _lastCameraLogSignature = sig;
            _nextCameraLogTime = now + 1.50f;
            if (_freeCameraMode)
                return;

        }

        private ParsedMap ResolveLiteralITerraRuntimeMapLikeOriginal()
        {
            return _map;
        }

        private static bool IsStrictExactOldSurfaceModeLikeOriginal(ParsedMap map)
        {
            return GetSurfaceModeLikeOriginal(map) == ParsedSurfaceMode.OldSurface;
        }

        private static ParsedSurfaceMode GetSurfaceModeLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return ParsedSurfaceMode.Unknown;
            if (map.SurfaceMode != ParsedSurfaceMode.Unknown)
                return map.SurfaceMode;
            return map.IsMeshSurface ? ParsedSurfaceMode.NewSurface : ParsedSurfaceMode.OldSurface;
        }

        private string GetSurfaceDispatchNameLikeOriginal(ParsedMap map)
        {
            switch (GetSurfaceModeLikeOriginal(map))
            {
                case ParsedSurfaceMode.OldSurface:
                    return "old-surface-frus";
                case ParsedSurfaceMode.NewSurface:
                    return "new-surface-hsem";
                default:
                    return "surface-unknown";
            }
        }

        private bool TryGetTerrainWorldBoundsLikeOriginal(out Bounds worldBounds)
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();

            if (IsStrictExactOldSurfaceModeLikeOriginal(runtimeMapLikeOriginal))
            {
                Bounds localBounds = BuildOldSurfaceBoundsDirectFromMapLikeOriginal(runtimeMapLikeOriginal, GetBoundsKernelLikeOriginal(runtimeMapLikeOriginal));
                Transform reference = _terrainGo != null ? _terrainGo.transform : transform;
                Vector3 lossyScale = reference != null ? reference.lossyScale : Vector3.one;
                Vector3 worldCenter = reference != null ? reference.TransformPoint(localBounds.center) : localBounds.center;
                Vector3 worldSize = Vector3.Scale(localBounds.size, new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
                worldBounds = new Bounds(worldCenter, worldSize);
                return true;
            }

            if (runtimeMapLikeOriginal != null && GetSurfaceModeLikeOriginal(runtimeMapLikeOriginal) == ParsedSurfaceMode.NewSurface)
            {
                Bounds localBounds = BuildMeshSurfaceBoundsDirectFromMapLikeOriginal(runtimeMapLikeOriginal);
                Transform reference = _terrainGo != null ? _terrainGo.transform : transform;
                Vector3 lossyScale = reference != null ? reference.lossyScale : Vector3.one;
                Vector3 worldCenter = reference != null ? reference.TransformPoint(localBounds.center) : localBounds.center;
                Vector3 worldSize = Vector3.Scale(localBounds.size, new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
                worldBounds = new Bounds(worldCenter, worldSize);
                return true;
            }

            if (_terrainGo != null)
            {
                var mf = _terrainGo.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds localBounds = mf.sharedMesh.bounds;
                    Vector3 lossyScale = _terrainGo.transform.lossyScale;
                    Vector3 worldCenter = _terrainGo.transform.TransformPoint(localBounds.center);
                    Vector3 worldSize = Vector3.Scale(localBounds.size, new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
                    worldBounds = new Bounds(worldCenter, worldSize);
                    return true;
                }
            }

            worldBounds = new Bounds(Vector3.zero, new Vector3(1000.0f, 100.0f, 1000.0f));
            return false;
        }

        private OriginalTerrainKernelConfig GetBoundsKernelLikeOriginal(ParsedMap map)
        {
            if (_hasLastBuiltTerrainKernel)
                return _lastBuiltTerrainKernel;
            return CreateOriginalTerrainKernelConfigLikeOriginal(map);
        }

        private static Vector3 GetOldSurfaceVertexWorldPosLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int col, int row)
        {
            int idx = row * map.VertInLine + col;
            float rawX = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, col);
            float rawZ = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, col, row);
            float x = rawX - kernel.CenterX;
            float z = (rawZ - kernel.CenterZ) * WorldZSign;
            float y = (map.Heights != null && idx >= 0 && idx < map.Heights.Length ? map.Heights[idx] : 0) * kernel.HeightScale;
            return new Vector3(x, y, z);
        }

        private static Bounds BuildOldSurfaceBoundsDirectFromMapLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel)
        {
            if (map == null || map.VertInLine <= 0 || map.MaxTH <= 0)
                return new Bounds(Vector3.zero, new Vector3(1000.0f, 100.0f, 1000.0f));

            GetOldSurfaceVertexRectFromMapLikeOriginal(map, out int minVertexX, out int minVertexY, out int maxVertexXExclusive, out int maxVertexYExclusive);
            if (maxVertexXExclusive <= minVertexX || maxVertexYExclusive <= minVertexY)
                return new Bounds(Vector3.zero, new Vector3(1000.0f, 100.0f, 1000.0f));

            Vector3 min = GetOldSurfaceVertexWorldPosLikeOriginal(map, kernel, minVertexX, minVertexY);
            Vector3 max = min;

            for (int row = minVertexY; row < maxVertexYExclusive; row++)
            {
                for (int col = minVertexX; col < maxVertexXExclusive; col++)
                {
                    Vector3 p = GetOldSurfaceVertexWorldPosLikeOriginal(map, kernel, col, row);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
            if (bounds.size.x < 1.0f) bounds.size = new Vector3(1.0f, bounds.size.y, bounds.size.z);
            if (bounds.size.y < 1.0f) bounds.size = new Vector3(bounds.size.x, 1.0f, bounds.size.z);
            if (bounds.size.z < 1.0f) bounds.size = new Vector3(bounds.size.x, bounds.size.y, 1.0f);
            return bounds;
        }

        private static Bounds BuildMeshSurfaceBoundsDirectFromMapLikeOriginal(ParsedMap map)
        {
            if (map == null || map.MeshSurfaceVertices == null || map.MeshSurfaceVertices.Length == 0)
                return new Bounds(Vector3.zero, new Vector3(1000.0f, 100.0f, 1000.0f));

            Vector3 v0 = CreateMeshSurfaceWorldVertexLikeOriginal(map.MeshSurfaceVertices[0]);
            Vector3 min = v0;
            Vector3 max = min;
            for (int i = 1; i < map.MeshSurfaceVertices.Length; i++)
            {
                Vector3 p = CreateMeshSurfaceWorldVertexLikeOriginal(map.MeshSurfaceVertices[i]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
            if (bounds.size.x < 1.0f) bounds.size = new Vector3(1.0f, bounds.size.y, bounds.size.z);
            if (bounds.size.y < 1.0f) bounds.size = new Vector3(bounds.size.x, 1.0f, bounds.size.z);
            if (bounds.size.z < 1.0f) bounds.size = new Vector3(bounds.size.x, bounds.size.y, 1.0f);
            return bounds;
        }

        private void UpdateWaterReflectionTarget(bool force)
        {
            UpdateWaterRuntimeV1LikeOriginal(force);
        }

        private void ApplyWaterReflectionParams(bool forceLog)
        {
            ApplyWaterMaterialParamsV1LikeOriginal(forceLog);
        }

        private void BuildStrictWholeMapTerrainLikeOriginal(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            ParsedSurfaceMode mode = GetSurfaceModeLikeOriginal(map);
            switch (mode)
            {
                case ParsedSurfaceMode.OldSurface:
                    BuildStrictOldSurfaceWholeMapLikeOriginal(map, parent, out terrainBounds);
                    return;
                case ParsedSurfaceMode.NewSurface:
                    BuildStrictNewSurfaceWholeMapLikeOriginal(map, parent, out terrainBounds);
                    return;
                default:
                    throw new InvalidOperationException("Map surface dispatch is unknown.");
            }
        }

        private static readonly int[] V50CandidateTex44MaskTileIdsLikeAdapted = new[] { 3, 7, 9, 10, 20, 21, 44, 55 };

        private void BuildStrictOldSurfaceWholeMapLikeOriginal(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            // V51: no extra reveal overlay.
            // Keep one terrain path only: the fast software-baked old-surface base.
            // Tex44 recovery, if any, must happen inside the bake itself, not as a second mesh/material layer on top.
            BuildStrictOldSurfaceSoftwareBakedChunksLikeOriginal(map, parent, out terrainBounds);
        }

        private void BuildV50CandidateTex44MaskOverlayLikeAdapted(ParsedMap map, Transform parent, ref Bounds terrainBounds)
        {
            if (map == null || map.Heights == null || map.Heights.Length == 0 || parent == null)
                return;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(map);
            int cellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int stripeWidth = Mathf.Clamp(StripeColumnWidth, 4, 256);
            int stripeCount = Mathf.Max(1, Mathf.CeilToInt(cellsX / (float)stripeWidth));

            Material source = CreateTerrainMaterialLikeOriginal(map);
            Material bridgeBaseMaterial = CreateSurfacePassMaterialLikeAdapted(source, false);
            Material bridgeOverlayMaterial = CreateSurfacePassMaterialLikeAdapted(source, true);
            Texture2D tex44 = TryLoadV48BridgeCobbleTex44LikeAdapted();
            ConfigureV48BridgeTex44RevealMaterialLikeAdapted(bridgeBaseMaterial, false, tex44);
            ConfigureV48BridgeTex44RevealMaterialLikeAdapted(bridgeOverlayMaterial, true, tex44);

            var overlayRoot = new GameObject("V50_CandidateTex44MaskReveal_StandaloneBMP44");
            overlayRoot.transform.SetParent(parent, false);

            int builtStripes = 0;
            int skippedStripes = 0;
            int totalVertices = 0;
            Bounds overlayBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasOverlayBounds = false;

            SetV44OldSurfaceTileFilterLikeAdapted(V50CandidateTex44MaskTileIdsLikeAdapted, 0.0f);
            try
            {
                for (int stripe = 0; stripe < stripeCount; stripe++)
                {
                    int startX = kernel.MinCellX + stripe * stripeWidth;
                    int endX = Mathf.Min(kernel.MaxCellXExclusive, startX + stripeWidth);
                    if (endX <= startX)
                        continue;

                    Mesh mesh = null;
                    Bounds stripeBounds = new Bounds(Vector3.zero, Vector3.zero);
                    try
                    {
                        mesh = BuildStripeMeshFromOriginalKernelLikeOriginal(map, kernel, startX, endX, out stripeBounds);
                    }
                    catch (Exception ex)
                    {
                    }

                    if (mesh == null || mesh.vertexCount == 0)
                    {
                        skippedStripes++;
                        continue;
                    }

                    var go = new GameObject($"V50_CandidateTex44MaskOverlay_{stripe:000}");
                    go.transform.SetParent(overlayRoot.transform, false);
                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    mf.sharedMesh = mesh;
                    if (mesh.subMeshCount > 1)
                        mr.sharedMaterials = new[] { bridgeBaseMaterial ?? source, bridgeOverlayMaterial ?? bridgeBaseMaterial ?? source };
                    else
                        mr.sharedMaterial = bridgeBaseMaterial ?? source;

                    totalVertices += mesh.vertexCount;
                    builtStripes++;
                    if (!hasOverlayBounds)
                    {
                        overlayBounds = stripeBounds;
                        hasOverlayBounds = true;
                    }
                    else
                    {
                        overlayBounds.Encapsulate(stripeBounds.min);
                        overlayBounds.Encapsulate(stripeBounds.max);
                    }
                }
            }
            finally
            {
                ClearV44OldSurfaceTileFilterLikeAdapted();
            }

            if (hasOverlayBounds)
            {
                terrainBounds.Encapsulate(overlayBounds.min);
                terrainBounds.Encapsulate(overlayBounds.max);
            }

        }

        private static Texture2D TryLoadV48BridgeCobbleTex44LikeAdapted()
        {
            string[] resourcePaths =
            {
                "textures/Ground/tex44",
                "textures/Ground/TEX44",
                "textures/ground/tex44",
                "Textures/ground/tex44",
                "Textures/Ground/tex44"
            };

            for (int i = 0; i < resourcePaths.Length; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(resourcePaths[i]);
                if (tex != null)
                {
                    return tex;
                }
            }

            return null;
        }

        private static void ConfigureV48BridgeTex44RevealMaterialLikeAdapted(Material mat, bool overlayPass, Texture2D tex44)
        {
            if (mat == null)
                return;

            mat.name = overlayPass ? "V49_FullTex44Reveal_OverlayPass" : "V49_FullTex44Reveal_BasePass";
            mat.renderQueue = overlayPass ? 3601 : 3600;
            if (mat.HasProperty("_SurfacePassModeLikeAdapted"))
                mat.SetFloat("_SurfacePassModeLikeAdapted", overlayPass ? 2.0f : 1.0f);
            if (mat.HasProperty("_V45ForceOpaqueAlphaLikeAdapted"))
                mat.SetFloat("_V45ForceOpaqueAlphaLikeAdapted", 1.0f);
            if (mat.HasProperty("_V45DisableCrossAlphaLikeAdapted"))
                mat.SetFloat("_V45DisableCrossAlphaLikeAdapted", 1.0f);
            if (mat.HasProperty("_V46ForceFullColorLikeAdapted"))
                mat.SetFloat("_V46ForceFullColorLikeAdapted", 1.0f);
            if (mat.HasProperty("_V46OverlayBrightnessLikeAdapted"))
                mat.SetFloat("_V46OverlayBrightnessLikeAdapted", overlayPass ? 1.20f : 1.16f);
            if (mat.HasProperty("_V46DisableAlphaClipLikeAdapted"))
                mat.SetFloat("_V46DisableAlphaClipLikeAdapted", 1.0f);
            if (mat.HasProperty("_V47ForceVisibleOverlayLikeAdapted"))
                mat.SetFloat("_V47ForceVisibleOverlayLikeAdapted", 1.0f);
            if (mat.HasProperty("_V47DisableStageSplitLikeAdapted"))
                mat.SetFloat("_V47DisableStageSplitLikeAdapted", 1.0f);
            if (mat.HasProperty("_V47ZTestLikeAdapted"))
                mat.SetFloat("_V47ZTestLikeAdapted", 8.0f);
            if (mat.HasProperty("_V47ZWriteLikeAdapted"))
                mat.SetFloat("_V47ZWriteLikeAdapted", 0.0f);
            if (tex44 != null && mat.HasProperty("_GroundAtlas"))
                mat.SetTexture("_GroundAtlas", tex44);
            if (tex44 != null)
            {
                tex44.wrapMode = TextureWrapMode.Repeat;
                tex44.filterMode = FilterMode.Trilinear;
                tex44.anisoLevel = Mathf.Max(tex44.anisoLevel, 8);
            }
            if (mat.HasProperty("_V48UseStandaloneTileTextureLikeAdapted"))
                mat.SetFloat("_V48UseStandaloneTileTextureLikeAdapted", tex44 != null ? 1.0f : 0.0f);
            if (mat.HasProperty("_V48StandaloneTileRepeatLikeAdapted"))
                mat.SetFloat("_V48StandaloneTileRepeatLikeAdapted", 1.0f);
            if (mat.HasProperty("_UseCrossLikeOriginal"))
                mat.SetFloat("_UseCrossLikeOriginal", 0.0f);
            if (mat.HasProperty("_UseDitherLikeOriginal"))
                mat.SetFloat("_UseDitherLikeOriginal", 0.0f);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
        }
        private void BuildStrictNewSurfaceWholeMapLikeOriginal(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            if (map.MeshSurfaceVertices == null || map.MeshSurfaceVertices.Length == 0)
                throw new InvalidOperationException("Map has no HSEM vertices.");
            if (map.MeshIndices == null || map.MeshIndices.Length == 0)
                throw new InvalidOperationException("Map has no HSEM indices.");

            _terrainMaterial = CreateTerrainMaterialLikeOriginal(map);
            _hasLastBuiltTerrainKernel = false;

            Mesh mesh = BuildMeshSurfaceMeshLikeOriginal(map, out terrainBounds);
            var go = new GameObject("StrictHsemSurface");
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = _terrainMaterial;
        }

        private static Mesh BuildMeshSurfaceMeshLikeOriginal(ParsedMap map, out Bounds bounds)
        {
            int vertexCount = map.MeshSurfaceVertices != null ? map.MeshSurfaceVertices.Length : 0;
            int indexCount = map.MeshIndices != null ? map.MeshIndices.Length : 0;
            if (vertexCount <= 0 || indexCount <= 0)
            {
                bounds = new Bounds(Vector3.zero, Vector3.one);
                return null;
            }

            var vertices = new List<Vector3>(vertexCount);
            var colors = new List<Color>(vertexCount);
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 p = CreateMeshSurfaceWorldVertexLikeOriginal(map.MeshSurfaceVertices[i]);
                vertices.Add(p);
                colors.Add(DecodeMeshSurfaceColorLikeOriginal(map.MeshSurfaceVertices[i].Color));

                if (!hasBounds)
                {
                    localBounds = new Bounds(p, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(p);
                }
            }

            var triangles = new int[indexCount];
            Array.Copy(map.MeshIndices, triangles, indexCount);

            var mesh = new Mesh { name = "StrictHsemSurfaceMesh" };
            if (vertexCount > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            bounds = hasBounds ? localBounds : mesh.bounds;
            return mesh;
        }

        private static Vector3 CreateMeshSurfaceWorldVertexLikeOriginal(ParsedMeshSurfaceVertex vertex)
        {
            return new Vector3(
                vertex.X * HorizontalScale,
                vertex.Y * VerticalScale,
                vertex.Z * HorizontalScale * WorldZSign);
        }

        private static Color DecodeMeshSurfaceColorLikeOriginal(uint color)
        {
            byte b = (byte)(color & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte r = (byte)((color >> 16) & 0xFF);
            byte a = (byte)((color >> 24) & 0xFF);
            return new Color32(r, g, b, a);
        }

        private static int GetTQuantByScShiftLikeOriginal(int scShift)
        {
            return 32 >> Mathf.Clamp(scShift, 0, 2);
        }

        private static int GetHQuantByScShiftLikeOriginal(int scShift)
        {
            return 16 >> Mathf.Clamp(scShift, 0, 2);
        }

        private static int GetSQuantByScShiftLikeOriginal(int scShift)
        {
            return 8 >> Mathf.Clamp(scShift, 0, 2);
        }

        private static float GetVertexRawXLikeOriginal(float tQuant, int vertexX)
        {
            return vertexX * tQuant;
        }

        private static float GetVertexRawZLikeOriginal(float hQuant, float sQuant, int vertexX, int vertexY)
        {
            return (vertexY * hQuant) + (((vertexX & 1) == 0) ? sQuant : 0.0f);
        }

        private static int PickVertexXWithParityLikeOriginal(int minVertexX, int maxVertexXExclusive, int desiredParity)
        {
            for (int x = minVertexX; x < maxVertexXExclusive; x++)
            {
                if ((x & 1) == desiredParity)
                    return x;
            }

            return Mathf.Clamp(minVertexX, 0, Mathf.Max(0, maxVertexXExclusive - 1));
        }

        private OriginalTerrainKernelConfig CreateOriginalTerrainKernelConfigLikeOriginal(ParsedMap map)
        {
            GetOldSurfaceCellRectFromMapLikeOriginal(map, out int minMapX, out int minMapY, out int maxMapX, out int maxMapY);

            int scShift = GetStrictScShiftLikeOriginal();
            float tQuant = GetTQuantByScShiftLikeOriginal(scShift);
            float hQuant = GetHQuantByScShiftLikeOriginal(scShift);
            float sQuant = GetSQuantByScShiftLikeOriginal(scShift);

            // Adaptation rule under the statute:
            // retail HQuant/SQuant belong to DrawTriStrip screen-space basis, not to the global world footprint.
            // In the single prebuilt Unity world-space mesh we keep the stable world footprint on TQuant / half-cell.
            // Original HQuant/SQuant are still preserved in the kernel for local screen-like math when needed.
            float backingStepX = tQuant;
            float backingStepZ = tQuant;
            float backingOddColumnOffsetZ = tQuant * 0.5f;

            GetOldSurfaceVertexRectFromMapLikeOriginal(map, out int minVertexX, out int minVertexY, out int maxVertexXExclusive, out int maxVertexYExclusive);
            int maxVertexX = Mathf.Max(minVertexX, maxVertexXExclusive - 1);
            int maxVertexY = Mathf.Max(minVertexY, maxVertexYExclusive - 1);
            int minRawZVertexX = PickVertexXWithParityLikeOriginal(minVertexX, maxVertexXExclusive, 1);
            int maxRawZVertexX = PickVertexXWithParityLikeOriginal(minVertexX, maxVertexXExclusive, 0);
            float minRawX = GetVertexRawXLikeOriginal(backingStepX, minVertexX);
            float maxRawX = GetVertexRawXLikeOriginal(backingStepX, maxVertexX);
            float minRawZ = GetVertexRawZLikeOriginal(backingStepZ, backingOddColumnOffsetZ, minRawZVertexX, minVertexY);
            float maxRawZ = GetVertexRawZLikeOriginal(backingStepZ, backingOddColumnOffsetZ, maxRawZVertexX, maxVertexY);
            float centerX = (minRawX + maxRawX) * 0.5f;
            float centerZ = (minRawZ + maxRawZ) * 0.5f;

            float heightScale = VerticalScale / Mathf.Pow(2.0f, Mathf.Clamp(scShift, 0, 2));
            float yShiftWorldScale = 0.0f;

            return new OriginalTerrainKernelConfig
            {
                MinCellX = minMapX,
                MaxCellXExclusive = maxMapX,
                MinCellY = minMapY,
                MaxCellYExclusive = maxMapY,
                TQuantWorld = tQuant,
                HQuantWorld = hQuant,
                SQuantWorld = sQuant,
                BackingStepXWorld = backingStepX,
                BackingStepZWorld = backingStepZ,
                BackingOddColumnOffsetZWorld = backingOddColumnOffsetZ,
                CenterX = centerX,
                CenterZ = centerZ,
                HeightScale = heightScale,
                YShiftWorldScale = yShiftWorldScale,
                ScShift = scShift
            };
        }

        private static Mesh BuildStripeMeshFromOriginalKernelLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int startCellX, int endCellX, out Bounds bounds)
        {
            KernelStripeData data = BuildTerrainWholeMapLikeOriginalKernel(map, kernel, startCellX, endCellX);
            if (data == null || data.Vertices.Count == 0)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                return null;
            }

            var mesh = new Mesh { name = $"StrictKernelStripe_{startCellX}_{endCellX}" };
            if (data.Vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(data.Vertices);
            if (data.OverlayTriangles.Count > 0)
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(data.Triangles, 0, true);
                mesh.SetTriangles(data.OverlayTriangles, 1, true);
            }
            else
            {
                mesh.SetTriangles(data.Triangles, 0, true);
            }
            mesh.SetColors(data.Colors);
            if (data.Uv0.Count == data.Vertices.Count)
                mesh.SetUVs(0, data.Uv0);
            if (data.Uv1.Count == data.Vertices.Count)
                mesh.SetUVs(1, data.Uv1);
            if (data.Uv2.Count == data.Vertices.Count)
                mesh.SetUVs(2, data.Uv2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            bounds = data.HasBounds ? data.Bounds : mesh.bounds;
            return mesh;
        }

        private static KernelStripeData BuildTerrainWholeMapLikeOriginalKernel(ParsedMap map, OriginalTerrainKernelConfig kernel, int startCellX, int endCellX)
        {
            int cellCount = Mathf.Max(0, endCellX - startCellX) * Mathf.Max(0, kernel.MaxCellYExclusive - kernel.MinCellY);
            var stripe = new KernelStripeData(cellCount * 2);
            EmitTerrainStripeLikeOriginal(map, kernel, startCellX, endCellX, stripe);
            return stripe;
        }

        private static void EmitTerrainStripeLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int startCellX, int endCellX, KernelStripeData stripe)
        {
            int minY = kernel.MinCellY;
            int maxY = kernel.MaxCellYExclusive;

            for (int cellY = minY; cellY < maxY; cellY++)
            {
                for (int cellX = startCellX; cellX < endCellX; cellX++)
                    EmitTerrainCellLikeOriginal(map, kernel, cellX, cellY, stripe);
            }
        }

        private static void EmitTerrainCellLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int cellX, int cellY, KernelStripeData stripe)
        {
            OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);

            CellVertexPayloadLikeOriginal v0 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V0);
            CellVertexPayloadLikeOriginal v1 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V1);
            CellVertexPayloadLikeOriginal v2 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V2);
            CellVertexPayloadLikeOriginal v3 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V3);

            bool emitted = AppendSurfaceTexturingPayloadForCellLikeOriginal(map, kernel, stripe, cell, v0, v1, v2, v3);
            if (!emitted)
            {
                if (IsV44OldSurfaceTileFilterActiveLikeAdapted())
                    return;

                AppendFallbackTriangleLikeOriginal(stripe, v0.World, v1.World, v2.World);
                if (cell.FirstC == cell.V2)
                    AppendFallbackTriangleLikeOriginal(stripe, v2.World, v1.World, v3.World);
                else
                    AppendFallbackTriangleLikeOriginal(stripe, v0.World, v3.World, v2.World);
            }

            EncapsulateCellBoundsLikeOriginal(stripe, v0.World, v1.World, v2.World, v3.World);
        }

        private static OriginalCellTriangulationLikeOriginal BuildCellTriangulationLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int cellX, int cellY)
        {
            int p0 = cellY * map.VertInLine + cellX;

            var cell = new OriginalCellTriangulationLikeOriginal
            {
                V0 = p0,
                V1 = p0 + 1,
                V2 = p0 + map.VertInLine,
                V3 = p0 + map.VertInLine + 1,
                X0 = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, cellX),
                X1 = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, cellX + 1)
            };

            cell.S0 = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, cellX, cellY);
            cell.S1 = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, cellX + 1, cellY);
            cell.S2 = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, cellX, cellY + 1);
            cell.S3 = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, cellX + 1, cellY + 1);

            if ((cellX & 1) != 0)
            {
                cell.FirstA = cell.V0;
                cell.FirstB = cell.V1;
                cell.FirstC = cell.V2;
                cell.SecondA = cell.V2;
                cell.SecondB = cell.V1;
                cell.SecondC = cell.V3;
            }
            else
            {
                cell.FirstA = cell.V0;
                cell.FirstB = cell.V1;
                cell.FirstC = cell.V3;
                cell.SecondA = cell.V0;
                cell.SecondB = cell.V3;
                cell.SecondC = cell.V2;
            }

            return cell;
        }

        private static CellVertexPayloadLikeOriginal BuildCellVertexPayloadLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, OriginalCellTriangulationLikeOriginal cell, int vertexIndex)
        {
            float rawX = GetCellVertexRawXLikeOriginal(cell, vertexIndex);
            float rawZ = GetCellVertexRawZLikeOriginal(cell, vertexIndex);
            Vector3 world = CreateKernelWorldVertexLikeOriginal(map, kernel, vertexIndex, rawX, rawZ);
            return new CellVertexPayloadLikeOriginal(vertexIndex, rawX, rawZ, world);
        }

        private static float GetCellVertexRawXLikeOriginal(OriginalCellTriangulationLikeOriginal cell, int vertexIndex)
        {
            return (vertexIndex == cell.V1 || vertexIndex == cell.V3) ? cell.X1 : cell.X0;
        }

        private static float GetCellVertexRawZLikeOriginal(OriginalCellTriangulationLikeOriginal cell, int vertexIndex)
        {
            if (vertexIndex == cell.V0)
                return cell.S0;
            if (vertexIndex == cell.V1)
                return cell.S1;
            if (vertexIndex == cell.V2)
                return cell.S2;
            return cell.S3;
        }

        private static void AppendFallbackTriangleLikeOriginal(KernelStripeData stripe, Vector3 va, Vector3 vb, Vector3 vc)
        {
            int triBase = stripe.Vertices.Count;
            stripe.Vertices.Add(va);
            stripe.Vertices.Add(vb);
            stripe.Vertices.Add(vc);
            stripe.Colors.Add(CreateHeightColorLikeOriginal(va.y));
            stripe.Colors.Add(CreateHeightColorLikeOriginal(vb.y));
            stripe.Colors.Add(CreateHeightColorLikeOriginal(vc.y));
            stripe.Uv0.Add(Vector2.zero);
            stripe.Uv0.Add(Vector2.zero);
            stripe.Uv0.Add(Vector2.zero);
            stripe.Uv1.Add(Vector2.zero);
            stripe.Uv1.Add(Vector2.zero);
            stripe.Uv1.Add(Vector2.zero);
            stripe.Uv2.Add(Vector2.zero);
            stripe.Uv2.Add(Vector2.zero);
            stripe.Uv2.Add(Vector2.zero);
            stripe.Triangles.Add(triBase + 0);
            stripe.Triangles.Add(triBase + 1);
            stripe.Triangles.Add(triBase + 2);
        }

        private static int GetTriangleGroundPriorityRenderFactureIdLikeOriginal(ParsedMap map, int vertA, int vertB, int vertC, out int representativeVertexIndex)
        {
            representativeVertexIndex = GetTriangleGroundPriorityRepresentativeVertexLikeOriginal(map, vertA, vertB, vertC);
            if (representativeVertexIndex < 0)
                return 0;

            int rawFactureId = GetFactureIdLikeOriginal(map, representativeVertexIndex) & 255;
            if (rawFactureId == 0)
                return 0;

            return ResolveFactureRenderIndexLikeAdapted(map, representativeVertexIndex, out _, out _, out _);
        }

        private static int GetTriangleGroundPriorityRepresentativeVertexLikeOriginal(ParsedMap map, int vertA, int vertB, int vertC)
        {
            if (map == null)
                return -1;

            int w1 = GetFactureWeightByIdxLikeOriginal(map, vertA);
            int w2 = GetFactureWeightByIdxLikeOriginal(map, vertB);
            int w3 = GetFactureWeightByIdxLikeOriginal(map, vertC);

            if (w1 > 0 || w2 > 0 || w3 > 0)
            {
                int winner = vertA;
                int bestWeight = w1;
                if (w2 > bestWeight)
                {
                    bestWeight = w2;
                    winner = vertB;
                }

                if (w3 > bestWeight)
                {
                    bestWeight = w3;
                    winner = vertC;
                }

                return winner;
            }

            int f1 = GetFactureIdLikeOriginal(map, vertA) & 255;
            int f2 = GetFactureIdLikeOriginal(map, vertB) & 255;
            int f3 = GetFactureIdLikeOriginal(map, vertC) & 255;

            if (f1 != 0 && (f1 == f2 || f1 == f3))
                return vertA;
            if (f2 != 0 && f2 == f3)
                return vertB;
            if (f1 != 0)
                return vertA;
            if (f2 != 0)
                return vertB;
            if (f3 != 0)
                return vertC;

            return -1;
        }

        private static void EncapsulateCellBoundsLikeOriginal(KernelStripeData stripe, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            if (!stripe.HasBounds)
            {
                stripe.Bounds = new Bounds(v0, Vector3.zero);
                stripe.HasBounds = true;
            }

            stripe.Bounds.Encapsulate(v0);
            stripe.Bounds.Encapsulate(v1);
            stripe.Bounds.Encapsulate(v2);
            stripe.Bounds.Encapsulate(v3);
        }

        private static Vector3 CreateKernelWorldVertexLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int vertexIndex, float rawX, float rawZ)
        {
            short h = (vertexIndex >= 0 && vertexIndex < map.Heights.Length) ? map.Heights[vertexIndex] : (short)0;
            // GETXSHIFT / GETYSHIFT are retail screen-space offsets applied during pretransformed draw.
            // In the one-shot Unity world-space mesh they must not deform the global map footprint.
            float worldX = rawX - kernel.CenterX;
            float worldY = h * kernel.HeightScale;
            float worldZ = (rawZ - kernel.CenterZ) * WorldZSign;
            return new Vector3(worldX, worldY, worldZ);
        }

        private static int GetMaxPointIndexLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return 0;

            if (map.VertInLine > 0 && map.MaxTH > 0)
                return checked(map.VertInLine * map.MaxTH);

            if (map.MaxTH > 0)
                return checked((map.MaxTH + 1) * map.MaxTH);

            if (map.Heights != null && map.Heights.Length > 0)
                return map.Heights.Length;

            return 0;
        }

        private static void EnsureMapXyShiftLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return;

            int expected = GetMaxPointIndexLikeOriginal(map);
            if (expected <= 0)
                return;

            if (map.XYShift != null && map.XYShift.Length >= expected)
                return;

            // Original retail path allocates XYShift through SetupXYShift() and clears it to 0x88.
            // GETXSHIFT / GETYSHIFT then decode that byte to zero shift until some later runtime/editor path mutates it.
            map.XYShift = Enumerable.Repeat((byte)0x88, expected).ToArray();
        }

        private static float GetVertexXShiftLikeOriginal(ParsedMap map, int vertexIndex)
        {
            EnsureMapXyShiftLikeOriginal(map);
            if (map == null || map.XYShift == null || vertexIndex < 0 || vertexIndex >= map.XYShift.Length)
                return 0.0f;
            return (map.XYShift[vertexIndex] & 0x0F) - 8;
        }

        private static float GetVertexYShiftLikeOriginal(ParsedMap map, int vertexIndex)
        {
            EnsureMapXyShiftLikeOriginal(map);
            if (map == null || map.XYShift == null || vertexIndex < 0 || vertexIndex >= map.XYShift.Length)
                return 0.0f;
            return ((map.XYShift[vertexIndex] >> 4) & 0x0F) - 8;
        }

        private static Mesh BuildStripeMeshFallbackLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int startCellX, int endCellX, out Bounds bounds)
        {
            KernelStripeData stripe = new KernelStripeData(Mathf.Max(1, (endCellX - startCellX) * (kernel.MaxCellYExclusive - kernel.MinCellY) * 2));
            EmitTerrainStripeLikeOriginal(map, kernel, startCellX, endCellX, stripe);

            bounds = stripe.HasBounds ? stripe.Bounds : new Bounds(Vector3.zero, Vector3.zero);
            if (stripe.Vertices.Count == 0)
                return null;

            var mesh = new Mesh { name = $"StrictStripeFallback_{startCellX}_{endCellX}" };
            if (stripe.Vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(stripe.Vertices);
            mesh.SetColors(stripe.Colors);
            if (stripe.Uv0.Count == stripe.Vertices.Count)
                mesh.SetUVs(0, stripe.Uv0);
            if (stripe.Uv1.Count == stripe.Vertices.Count)
                mesh.SetUVs(1, stripe.Uv1);
            if (stripe.Uv2.Count == stripe.Vertices.Count)
                mesh.SetUVs(2, stripe.Uv2);
            mesh.SetTriangles(stripe.Triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            bounds = mesh.bounds;
            return mesh;
        }

        private static int GetVertexIndex(
            Dictionary<long, int> indexByKey,
            List<Vector3> vertices,
            List<Color> colors,
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            int x,
            int y,
            ref Bounds bounds,
            ref bool hasBounds)
        {
            long key = ((long)y << 32) | (uint)x;
            if (indexByKey.TryGetValue(key, out int existing))
                return existing;

            Vector3 v = CreateFallbackWorldVertexLikeOriginal(map, kernel, x, y);
            int index = vertices.Count;
            vertices.Add(v);
            colors.Add(CreateHeightColorLikeOriginal(v.y));
            indexByKey[key] = index;

            if (!hasBounds)
            {
                bounds = new Bounds(v, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(v);
            }

            return index;
        }

        private static Vector3 CreateFallbackWorldVertexLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int x, int y)
        {
            int index = y * map.VertInLine + x;
            short h = (index >= 0 && index < map.Heights.Length) ? map.Heights[index] : (short)0;
            float rawX = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, x);
            float rawZ = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, x, y);
            float wx = rawX - kernel.CenterX;
            float wz = (rawZ - kernel.CenterZ) * WorldZSign;
            float wy = h * kernel.HeightScale;
            return new Vector3(wx, wy, wz);
        }

        private static Color CreateHeightColorLikeOriginal(float y)
        {
            float t = Mathf.InverseLerp(-256.0f, 1024.0f, y);
            return Color.Lerp(new Color(0.28f, 0.33f, 0.28f, 1.0f), new Color(0.72f, 0.72f, 0.72f, 1.0f), t);
        }


        private Material CreateSurfacePassMaterialLikeAdapted(Material source, bool overlayPass)
        {
            if (source == null)
                return null;

            Material mat = new Material(source);
            mat.name = overlayPass ? (source.name + "_OverlayOnly") : (source.name + "_BaseOnly");
            if (mat.HasProperty("_SurfacePassModeLikeAdapted"))
                mat.SetFloat("_SurfacePassModeLikeAdapted", overlayPass ? 2.0f : 1.0f);
            mat.renderQueue = overlayPass ? 2001 : 2000;
            return mat;
        }

        private Material CreateTerrainMaterialLikeOriginal(ParsedMap map)
        {
            return CreateTerrainMaterialCoreLikeOriginal(map);
        }

        private bool TryResolveSelectedMapPath(out string relativePath, out string selectedId, out string error)
        {
            relativePath = string.Empty;
            selectedId = MenuActionSink.SingleBattlesSelectedId ?? string.Empty;
            error = string.Empty;

            if (MenuActionSink.SingleBattlesShowLoad)
            {
                error = "Load-mode is not supported in clean terrain mode.";
                return false;
            }

            bool showBattles = MenuActionSink.SingleBattlesShowBattles;
            string relDir = showBattles ? @"Missions\Battles" : @"Missions\Skirmish";

            if (string.IsNullOrWhiteSpace(selectedId))
            {
                string absDir0 = _bootstrap.Fs.ResolvePath(relDir);
                if (Directory.Exists(absDir0))
                {
                    string first = Directory.GetFiles(absDir0, "*.m3d")
                        .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(first))
                        selectedId = Path.GetFileNameWithoutExtension(first);
                }
            }

            if (string.IsNullOrWhiteSpace(selectedId))
                selectedId = showBattles ? "Battle1" : "Skirmish1";

            string direct = relDir + @"\" + selectedId + ".m3d";
            if (_bootstrap.Fs.Exists(direct))
            {
                relativePath = direct;
                return true;
            }

            string absDir = _bootstrap.Fs.ResolvePath(relDir);
            if (!Directory.Exists(absDir))
            {
                error = "Map directory not found: " + absDir;
                return false;
            }

            string selectedIdLocal = selectedId;
            string hit = Directory.GetFiles(absDir, "*.m3d")
                .FirstOrDefault(f => string.Equals(Path.GetFileNameWithoutExtension(f), selectedIdLocal, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(hit))
            {
                error = "Map file not found: " + direct;
                return false;
            }

            relativePath = relDir + @"\" + Path.GetFileName(hit);
            selectedId = Path.GetFileNameWithoutExtension(hit);
            return true;
        }

        private static bool TryParseMap(Cossacks2Bridge.Core.CoreFileSystem fs, string relativePath, out ParsedMap map, out string error)
        {
            map = null;
            error = string.Empty;

            if (fs == null)
            {
                error = "CoreFileSystem == null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(relativePath) || !fs.Exists(relativePath))
            {
                error = "Map file not found: " + relativePath;
                return false;
            }

            byte[] raw = fs.ReadAllBytes(relativePath);
            if (raw == null || raw.Length == 0)
            {
                error = "Map file is empty: " + relativePath;
                return false;
            }

            byte[] data = MaybeDecompressM3d(raw, out error);
            if (data == null || data.Length < 12)
                return false;

            using (var ms = new MemoryStream(data, false))
            using (var br = new BinaryReader(ms))
            {
                string magic = ReadTag(br);
                if (!TryGetAddshFromMapMagic(magic, out int addsh))
                {
                    error = "Unsupported map magic: " + magic;
                    return false;
                }

                map = new ParsedMap();
                map.SourcePath = relativePath;
                map.HeaderMagic = magic;
                map.HeaderStoredVertInLine = br.ReadInt32();
                map.HeaderStoredMaxTH = br.ReadInt32();
                map.Addsh = addsh;
                ApplyOriginalHeaderSetupArraysLikeOriginal(map);

                while (ms.Position + 8 <= ms.Length)
                {
                    string tag = ReadTag(br);
                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal))
                        break;

                    int sizeField = br.ReadInt32();
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;

                    if (TagEqualsLikeOriginal(tag, "MPSZ", "ZSPM"))
                    {
                        LoadMapSizeLikeOriginal(br, map, payloadLen);
                        ms.Position = payloadStart + payloadLen;
                        continue;
                    }

                    if (TagEqualsLikeOriginal(tag, "SURF", "FRUS"))
                    {
                        LoadSurfaceLikeOriginal(br, map, payloadLen);
                        ms.Position = payloadStart + payloadLen;
                        continue;
                    }

                    if (TagEqualsLikeOriginal(tag, "HSEM"))
                    {
                        LoadNewSurfaceLikeOriginal(br, map, payloadLen);
                        ms.Position = payloadStart + payloadLen;
                        continue;
                    }

                    if (TryParseSurfaceTexturingChunkLikeOriginal(tag, br, map, payloadLen))
                    {
                        ms.Position = payloadStart + payloadLen;
                        continue;
                    }

                    if (TryParseRoadsChunkLikeOriginal(tag, br, map, payloadLen))
                    {
                        ms.Position = payloadStart + payloadLen;
                        continue;
                    }

                    if (TryParseWaterChunkLikeOriginal(tag, br, map, payloadLen))
                    {
                        ms.Position = payloadStart + payloadLen;
                        continue;
                    }

                    ms.Position = payloadStart + payloadLen;
                }
            }

            if (map != null && map.HasSurfaceChunk)
            {
                ComputeBoundsLikeOriginal(map);
                return true;
            }

            error = "SURF/HSEM chunk not found.";
            return false;
        }

        private static void SelectSurfaceTypeLikeOriginal(ParsedMap map, bool isNewSurface)
        {
            if (map == null)
                return;

            map.SurfaceMode = isNewSurface ? ParsedSurfaceMode.NewSurface : ParsedSurfaceMode.OldSurface;
            map.IsMeshSurface = isNewSurface;
            map.HasSurfaceChunk = true;
        }

        private static void LoadSurfaceLikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            SelectSurfaceTypeLikeOriginal(map, false);
            int count = Mathf.Min(map.VertInLine * map.MaxTH, payloadLen / 2);
            map.Heights = new short[map.VertInLine * map.MaxTH];
            for (int i = 0; i < count; i++)
                map.Heights[i] = br.ReadInt16();

            map.MeshSurfaceVertices = Array.Empty<ParsedMeshSurfaceVertex>();
            map.MeshVertices = Array.Empty<Vector3>();
            map.MeshIndices = Array.Empty<int>();
            EnsureMapXyShiftLikeOriginal(map);
        }

        private static void LoadNewSurfaceLikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            SelectSurfaceTypeLikeOriginal(map, true);

            if (payloadLen < 8)
            {
                map.MeshSurfaceVertices = Array.Empty<ParsedMeshSurfaceVertex>();
                map.MeshVertices = Array.Empty<Vector3>();
                map.MeshIndices = Array.Empty<int>();
                map.Heights = Array.Empty<short>();
                return;
            }

            int nv = Mathf.Max(0, br.ReadInt32());
            int ni = Mathf.Max(0, br.ReadInt32());

            map.MeshSurfaceVertices = new ParsedMeshSurfaceVertex[nv];
            map.MeshVertices = new Vector3[nv];
            for (int i = 0; i < nv; i++)
            {
                ParsedMeshSurfaceVertex v = new ParsedMeshSurfaceVertex
                {
                    X = br.ReadInt16(),
                    Y = br.ReadInt16(),
                    Z = br.ReadInt16(),
                    Color = br.ReadUInt32(),
                    Shadow = br.ReadByte(),
                    Facture = br.ReadByte(),
                    NX = unchecked((sbyte)br.ReadByte()),
                    NY = unchecked((sbyte)br.ReadByte()),
                    NZ = unchecked((sbyte)br.ReadByte()),
                    Reserved = br.ReadByte()
                };

                map.MeshSurfaceVertices[i] = v;
                map.MeshVertices[i] = new Vector3(v.X, v.Y, v.Z);
            }

            map.MeshIndices = new int[ni];
            for (int i = 0; i < ni; i++)
                map.MeshIndices[i] = br.ReadInt32();

            map.Heights = Array.Empty<short>();
            map.XYShift = Array.Empty<byte>();
        }

        private static void ApplyOriginalHeaderSetupArraysLikeOriginal(ParsedMap map)
        {
            int addsh = Mathf.Clamp(map.Addsh, 1, 3);
            map.Addsh = addsh;
            map.MaxSector = 128 << addsh;
            map.MaxTH = map.MaxSector * 2;
            map.VertInLine = map.MaxSector + map.MaxSector + 1;
            map.MAPSX = 512 << addsh;
            map.MAPSY = map.MAPSX;
        }

        private static void LoadMapSizeLikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            if (map == null)
                return;

            if (payloadLen < 16)
                return;

            map.MinMapX = br.ReadInt32();
            map.MinMapY = br.ReadInt32();
            map.MaxMapX = br.ReadInt32();
            map.MaxMapY = br.ReadInt32();
            map.HasMapSizeChunk = true;
        }

        private static void GetOldSurfaceCellRectFromMapLikeOriginal(ParsedMap map, out int minCellX, out int minCellY, out int maxCellXExclusive, out int maxCellYExclusive)
        {
            int defaultMinX = 0;
            int defaultMinY = 0;
            int defaultMaxX = Mathf.Max(1, map != null ? map.VertInLine - 1 : 1);
            int defaultMaxY = Mathf.Max(1, map != null ? map.MaxTH - 1 : 1);

            if (map == null || !map.HasMapSizeChunk)
            {
                minCellX = defaultMinX;
                minCellY = defaultMinY;
                maxCellXExclusive = defaultMaxX;
                maxCellYExclusive = defaultMaxY;
                return;
            }

            minCellX = Mathf.Clamp(map.MinMapX, defaultMinX, defaultMaxX - 1);
            minCellY = Mathf.Clamp(map.MinMapY, defaultMinY, defaultMaxY - 1);
            maxCellXExclusive = Mathf.Clamp(map.MaxMapX, minCellX + 1, defaultMaxX);
            maxCellYExclusive = Mathf.Clamp(map.MaxMapY, minCellY + 1, defaultMaxY);
        }

        private static void GetOldSurfaceVertexRectFromMapLikeOriginal(ParsedMap map, out int minVertexX, out int minVertexY, out int maxVertexXExclusive, out int maxVertexYExclusive)
        {
            GetOldSurfaceCellRectFromMapLikeOriginal(map, out int minCellX, out int minCellY, out int maxCellXExclusive, out int maxCellYExclusive);
            minVertexX = minCellX;
            minVertexY = minCellY;
            maxVertexXExclusive = Mathf.Min(map != null ? map.VertInLine : 0, maxCellXExclusive + 1);
            maxVertexYExclusive = Mathf.Min(map != null ? map.MaxTH : 0, maxCellYExclusive + 1);
        }

        private static void ComputeBoundsLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return;

            GetOldSurfaceCellRectFromMapLikeOriginal(map, out map.MinMapX, out map.MinMapY, out map.MaxMapX, out map.MaxMapY);
        }

        private static byte[] MaybeDecompressM3d(byte[] raw, out string error)
        {
            error = string.Empty;
            if (raw == null || raw.Length == 0)
            {
                error = "raw map is empty";
                return null;
            }

            string rawMagic = ReadMagic4(raw);
            if (TryGetAddshFromMapMagic(rawMagic, out _))
                return raw;

            if (raw.Length < 9)
            {
                error = "Unknown map container.";
                return null;
            }

            byte compType = raw[0];
            int inLen = BitConverter.ToInt32(raw, 1);
            int outLen = BitConverter.ToInt32(raw, 5);
            if (inLen <= 0 || raw.Length < 9 + inLen)
            {
                error = "Invalid FCOMP header.";
                return null;
            }

            byte[] payload = new byte[inLen];
            Buffer.BlockCopy(raw, 9, payload, 0, inLen);

            try
            {
                switch (compType)
                {
                    case 0:
                        return payload;
                    case 1:
                        return SharpBZip2Compat.Decompress(payload);
                    default:
                        if (StartsWithBzipMagic(payload))
                            return SharpBZip2Compat.Decompress(payload);
                        error = $"Unsupported clean-mode compression type: {compType} (expected raw/store/bzip).";
                        return null;
                }
            }
            catch (Exception ex)
            {
                error = $"Map decompress failed: {ex.GetType().Name}: {ex.Message}; outLen={outLen}";
                return null;
            }
        }

        private static bool StartsWithBzipMagic(byte[] data)
        {
            return data != null && data.Length >= 4 && data[0] == (byte)'B' && data[1] == (byte)'Z' && data[2] == (byte)'h';
        }


        private static bool TagEqualsLikeOriginal(string actualTag, params string[] expectedVariants)
        {
            if (string.IsNullOrEmpty(actualTag) || expectedVariants == null)
                return false;

            for (int i = 0; i < expectedVariants.Length; i++)
            {
                string expected = expectedVariants[i];
                if (!string.IsNullOrEmpty(expected) && string.Equals(actualTag, expected, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string ReadTag(BinaryReader br)
        {
            byte[] tag = br.ReadBytes(4);
            if (tag == null || tag.Length < 4)
                return string.Empty;
            return System.Text.Encoding.ASCII.GetString(tag);
        }

        private static string ReadMagic4(byte[] data)
        {
            if (data == null || data.Length < 4)
                return string.Empty;
            return System.Text.Encoding.ASCII.GetString(data, 0, 4);
        }

        private static bool TryGetAddshFromMapMagic(string magic, out int addsh)
        {
            switch (magic)
            {
                case "3DMP": addsh = 1; return true;
                case "4DMP": addsh = 2; return true;
                case "5DMP": addsh = 3; return true;
                default: addsh = 0; return false;
            }
        }

        private static void DestroyUiAndOldMode(GameObject keepRoot = null)
        {
            foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (c == null)
                    continue;
                if (c.gameObject.name == CameraName)
                    SafeDestroy(c.gameObject);
            }

            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null && oldRoot != keepRoot)
                SafeDestroy(oldRoot);
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null)
                return;
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(obj))
            {
                UnityEngine.Object.DestroyImmediate(obj, true);
                return;
            }
#endif
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
