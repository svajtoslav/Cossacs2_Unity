#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Cossacks2Bridge.UnityAdapters;
using Cossacks2Bridge.UnityAdapters.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Headless/editor-driven foundation benchmark.  It deliberately uses the
/// same editor placement and formation-order entry points as the interactive
/// battle polygon, so the test does not introduce a second movement path.
/// Run with:
///   Unity -batchmode -executeMethod C2FoundationBenchmarkEditor.Run
/// The runner exits Unity by itself after both 5- and 12-formation samples.
/// </summary>
[InitializeOnLoad]
public static class C2FoundationBenchmarkEditor
{
    private const string ActiveKey = "C2.FoundationBenchmark.Active";
    private const string ExitPendingKey = "C2.FoundationBenchmark.ExitPending";
    private const string ExitCodeKey = "C2.FoundationBenchmark.ExitCode";
    private const string ScaleModeKey = "C2.FoundationBenchmark.ScaleMode";
    private const string CoreOnlyModeKey = "C2.FoundationBenchmark.CoreOnlyMode";
    private const string SimulationOnlyModeKey = "C2.FoundationBenchmark.SimulationOnlyMode";
    private const string SkipBatchCommitModeKey = "C2.FoundationBenchmark.SkipBatchCommitMode";
    private const string SkipFrameMaterializationModeKey = "C2.FoundationBenchmark.SkipFrameMaterializationMode";
    private const string SkipBatchBuildModeKey = "C2.FoundationBenchmark.SkipBatchBuildMode";
    private const string SkipFrameApplyModeKey = "C2.FoundationBenchmark.SkipFrameApplyMode";
    private const string SkipRenderTransformModeKey = "C2.FoundationBenchmark.SkipRenderTransformMode";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    private enum Phase
    {
        None,
        WaitForPlay,
        OpenMap,
        WaitForMap,
        SpawnFive,
        WarmFiveStationary,
        MeasureFiveStationary,
        WarmFiveMoving,
        MeasureFiveMoving,
        StopFive,
        SpawnTwelve,
        WarmTwelveStationary,
        MeasureTwelveStationary,
        WarmTwelveMoving,
        MeasureTwelveMoving,
        SpawnFifty,
        WarmFiftyStationary,
        MeasureFiftyStationary,
        WarmFiftyMoving,
        MeasureFiftyMoving,
        StopFifty,
        SpawnOneHundredTwentyTwo,
        WarmOneHundredTwentyTwoStationary,
        MeasureOneHundredTwentyTwoStationary,
        WarmOneHundredTwentyTwoMoving,
        MeasureOneHundredTwentyTwoMoving,
        WarmCameraNear,
        MeasureCameraNear,
        WarmCameraFar,
        MeasureCameraFar,
        Complete
    }

    private static Phase _phase;
    private static int _phaseStartFrame;
    private static int _lastSpawnedFormationCount;
    private static readonly List<float> FrameMilliseconds = new List<float>(360);
    private static readonly string[] FrameMarkerNames =
    {
        "PlayerLoop",
        "BehaviourUpdate",
        "Update.ScriptRunBehaviourUpdate",
        "C2UnitOriginalRuntimeAndRendererV1.Update()",
        "C2BattleTerrainMode.Update()",
        "C2GameplayHudV1.Update()",
        "C2GameplayInteractionControllerV1.Update()",
        "C2MinimapRuntimeLikeOriginal.Update()",
        "C2FormationRuntimeV167LikeOriginal.Update()",
        "C2RoadUnitSpeedControllerV352.Update()",
        "C2SettlementDipRuntimeV336LikeOriginal.Update()",
        "C2EditorTestPaletteV332LikeOriginal.Update()",
        "C2UnitSelectionBoxV239.Update()",
        "Camera.Render",
        "RenderPipelineManager.DoRenderLoop_Internal",
        "UpdateRendererBoundingVolumes",
        "Mesh.UploadMeshData",
        "GUI.Repaint",
        "C2.Unit.SimulationV370",
        "C2.Unit.FormationKeepV370",
        "C2.Unit.OrdersV370",
        "C2.Unit.StepAllV370",
        "C2.Unit.DrawListV370",
        "C2.Unit.RenderVisibleV370",
        "C2.Unit.BatchBuildV370",
        "C2.Unit.WholeUpdateV371"
        ,"GC.Collect"
        ,"GC.CollectIncremental"
        ,"GarbageCollectAssetsProfile"
        ,"C2.Interaction.UnitPickV377"
    };
    private static readonly Recorder[] FrameMarkerRecorders = new Recorder[FrameMarkerNames.Length];
    private static readonly long[] FrameMarkerNanoseconds = new long[FrameMarkerNames.Length];
    private static readonly long[] FrameMarkerBlocks = new long[FrameMarkerNames.Length];
    private static C2BattleTerrainMode _mode;
    private static C2EditorTestPaletteV332LikeOriginal _palette;
    private static MethodInfo _spawnFormation;
    private static float _centerRealX;
    private static float _centerRealY;
    private static int _bootstrapCreatedFrame = -1;
    private static bool _scaleMode;
    private static bool _coreOnlyMode;
    private static bool _simulationOnlyMode;
    private static bool CpuTraceRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-c2CpuTrace") >= 0;
    private static bool RenderRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-c2Render") >= 0;
    private static bool CameraSweepRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-c2CameraSweep") >= 0;
    private struct UnitMeasurementStart
    {
        internal C2UnitOriginalRuntime Unit;
        internal Vector3 Position;
        internal int AnimationFrame;
        internal bool OutsideDrawList;
    }
    private static readonly List<UnitMeasurementStart> UnitMeasurementStarts = new List<UnitMeasurementStart>(16384);
    private static RenderTexture _renderTarget;
    private static Camera _renderCamera;
    private static int _renderedFrames;
    private static int _renderedAtMeasureStart;
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCounters
    {
        internal uint Size, PageFaultCount;
        internal UIntPtr PeakWorkingSet, WorkingSet, PeakPagedPool, PagedPool,
            PeakNonPagedPool, NonPagedPool, PagefileUsage, PeakPagefileUsage, PrivateUsage;
    }
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(IntPtr process, out ProcessMemoryCounters counters, uint size);

    static C2FoundationBenchmarkEditor()
    {
        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    public static void Run()
    {
        SessionState.SetBool(CoreOnlyModeKey, false);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, false);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(false);
    }

    /// <summary>
    /// Scale proof using the same runtime path: 50 formations (~6150 units),
    /// then 122 formations (~15006 units). The 5000-unit boundary is deliberate:
    /// it verifies the exact COSSACKS2 BoidsOffLimit branch.
    /// </summary>
    public static void RunScale()
    {
        SessionState.SetBool(CoreOnlyModeKey, false);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, false);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    /// <summary>
    /// Diagnostic isolation run.  Placement stays identical, then every scene
    /// MonoBehaviour except the central C2 unit core is disabled.  This proves
    /// whether remaining frame time belongs to the unit data/visual proxies or
    /// to another transitional Unity subsystem that scans the army each frame.
    /// </summary>
    public static void RunScaleCoreOnly()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, false);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    public static void RunScaleSimulationOnly()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, true);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, false);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    public static void RunScaleNoBatchCommit()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, true);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, false);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    public static void RunScaleDrawListOnly()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, true);
        SessionState.SetBool(SkipBatchBuildModeKey, false);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    public static void RunScaleFrameApplyOnly()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, true);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    public static void RunScaleTransformOnly()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, true);
        SessionState.SetBool(SkipFrameApplyModeKey, true);
        SessionState.SetBool(SkipRenderTransformModeKey, false);
        StartBenchmark(true);
    }

    public static void RunScaleFrameResolveOnly()
    {
        SessionState.SetBool(CoreOnlyModeKey, true);
        SessionState.SetBool(SimulationOnlyModeKey, false);
        SessionState.SetBool(SkipBatchCommitModeKey, false);
        SessionState.SetBool(SkipFrameMaterializationModeKey, false);
        SessionState.SetBool(SkipBatchBuildModeKey, true);
        SessionState.SetBool(SkipFrameApplyModeKey, false);
        SessionState.SetBool(SkipRenderTransformModeKey, true);
        StartBenchmark(true);
    }

    private static void StartBenchmark(bool scaleMode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Foundation benchmark must start in edit mode.");

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(ExitPendingKey, false);
        SessionState.SetInt(ExitCodeKey, 0);
        SessionState.SetBool(ScaleModeKey, scaleMode);
        _scaleMode = scaleMode;
        _coreOnlyMode = SessionState.GetBool(CoreOnlyModeKey, false);
        _phase = Phase.WaitForPlay;
        _phaseStartFrame = 0;
        _lastSpawnedFormationCount = 0;
        FrameMilliseconds.Clear();

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[C2:FOUNDATION BENCH] start scene=" + ScenePath +
                  " contract=same_editor_placement_same_formation_order_chain" +
                  " scaleMode=" + scaleMode);
        EditorApplication.isPlaying = true;
    }

    private static void EditorUpdate()
    {
        if (SessionState.GetBool(ExitPendingKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            int code = SessionState.GetInt(ExitCodeKey, 1);
            SessionState.SetBool(ExitPendingKey, false);
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.Exit(code);
            return;
        }

        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying)
            return;

        try
        {
            TickPlayMode();
        }
        catch (Exception ex)
        {
            Debug.LogError("[C2:FOUNDATION BENCH] FAILED\n" + ex);
            Finish(3);
        }
    }

    private static void TickPlayMode()
    {
        if (_phase == Phase.None || _phase == Phase.WaitForPlay)
            EnterPhase(Phase.OpenMap);

        switch (_phase)
        {
            case Phase.OpenMap:
                if (FramesInPhase < 8) return;
                if (!TryOpenEditorPlateauLikeOriginal()) return;
                EnterPhase(Phase.WaitForMap);
                return;

            case Phase.WaitForMap:
                if (FramesInPhase < 30) return;
                if (!ResolveMapAndPalette())
                {
                    if (FramesInPhase > 600)
                        throw new InvalidOperationException("Editor plateau/palette did not become ready.");
                    return;
                }
                if (RenderRequested) PrepareRenderTarget();
                _scaleMode = SessionState.GetBool(ScaleModeKey, false);
                _coreOnlyMode = SessionState.GetBool(CoreOnlyModeKey, false);
                _simulationOnlyMode = SessionState.GetBool(SimulationOnlyModeKey, false);
                C2UnitOriginalRuntimeAndRendererV1 diagnosticCore =
                    UnityEngine.Object.FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
                if (diagnosticCore != null)
                {
                    diagnosticCore.SkipUnitVisualUpdateForPerformanceDiagnosisV371 = _simulationOnlyMode;
                    diagnosticCore.SkipGpsBatchMeshCommitForPerformanceDiagnosisV372 =
                        SessionState.GetBool(SkipBatchCommitModeKey, false);
                    diagnosticCore.SkipUnitFrameMaterializationForPerformanceDiagnosisV373 =
                        SessionState.GetBool(SkipFrameMaterializationModeKey, false);
                    diagnosticCore.SkipGpsBatchBuildForPerformanceDiagnosisV373 =
                        SessionState.GetBool(SkipBatchBuildModeKey, false);
                    diagnosticCore.SkipUnitFrameApplyForPerformanceDiagnosisV373 =
                        SessionState.GetBool(SkipFrameApplyModeKey, false);
                    diagnosticCore.SkipUnitRenderTransformForPerformanceDiagnosisV373 =
                        SessionState.GetBool(SkipRenderTransformModeKey, false);
                }
                EnterPhase(_scaleMode ? Phase.SpawnFifty : Phase.SpawnFive);
                return;

            case Phase.SpawnFive:
                SpawnUntilFormationCount(5);
                EnterPhase(Phase.WarmFiveStationary);
                return;

            case Phase.WarmFiveStationary:
                if (FramesInPhase >= 120) BeginMeasurement(Phase.MeasureFiveStationary);
                return;

            case Phase.MeasureFiveStationary:
                SampleFrame();
                if (FrameMilliseconds.Count >= 300)
                {
                    Report("5_stationary");
                    IssueMoveOrder(+15000.0f, 0.0f);
                    EnterPhase(Phase.WarmFiveMoving);
                }
                return;

            case Phase.WarmFiveMoving:
                if (FramesInPhase >= 90) BeginMeasurement(Phase.MeasureFiveMoving);
                return;

            case Phase.MeasureFiveMoving:
                SampleFrame();
                if (FrameMilliseconds.Count >= 300)
                {
                    Report("5_moving");
                    EnterPhase(Phase.StopFive);
                }
                return;

            case Phase.StopFive:
                StopAllUnits();
                EnterPhase(Phase.SpawnTwelve);
                return;

            case Phase.SpawnTwelve:
                SpawnUntilFormationCount(12);
                EnterPhase(Phase.WarmTwelveStationary);
                return;

            case Phase.WarmTwelveStationary:
                if (FramesInPhase >= 120) BeginMeasurement(Phase.MeasureTwelveStationary);
                return;

            case Phase.MeasureTwelveStationary:
                SampleFrame();
                if (FrameMilliseconds.Count >= 300)
                {
                    Report("12_stationary");
                    IssueMoveOrder(-15000.0f, 0.0f);
                    EnterPhase(Phase.WarmTwelveMoving);
                }
                return;

            case Phase.WarmTwelveMoving:
                if (FramesInPhase >= 90) BeginMeasurement(Phase.MeasureTwelveMoving);
                return;

            case Phase.MeasureTwelveMoving:
                SampleFrame();
                if (FrameMilliseconds.Count >= 300)
                {
                    Report("12_moving");
                    EnterPhase(Phase.Complete);
                }
                return;

            case Phase.SpawnFifty:
                SpawnUntilFormationCount(50);
                EnterPhase(Phase.WarmFiftyStationary);
                return;

            case Phase.WarmFiftyStationary:
                if (FramesInPhase >= 120) BeginMeasurement(Phase.MeasureFiftyStationary);
                return;

            case Phase.MeasureFiftyStationary:
                SampleFrame();
                if (FrameMilliseconds.Count >= 180)
                {
                    Report("50_stationary");
                    IssueMoveOrder(+30000.0f, 0.0f);
                    EnterPhase(Phase.WarmFiftyMoving);
                }
                return;

            case Phase.WarmFiftyMoving:
                if (FramesInPhase >= 90) BeginMeasurement(Phase.MeasureFiftyMoving);
                return;

            case Phase.MeasureFiftyMoving:
                SampleFrame();
                if (FrameMilliseconds.Count >= 180)
                {
                    Report("50_moving");
                    EnterPhase(Phase.StopFifty);
                }
                return;

            case Phase.StopFifty:
                StopAllUnits();
                EnterPhase(Phase.SpawnOneHundredTwentyTwo);
                return;

            case Phase.SpawnOneHundredTwentyTwo:
                SpawnUntilFormationCount(122);
                EnterPhase(Phase.WarmOneHundredTwentyTwoStationary);
                return;

            case Phase.WarmOneHundredTwentyTwoStationary:
                if (FramesInPhase >= 120) BeginMeasurement(Phase.MeasureOneHundredTwentyTwoStationary);
                return;

            case Phase.MeasureOneHundredTwentyTwoStationary:
                SampleFrame();
                if (FrameMilliseconds.Count >= 120)
                {
                    Report("122_stationary");
                    IssueMoveOrder(-30000.0f, 0.0f);
                    EnterPhase(Phase.WarmOneHundredTwentyTwoMoving);
                }
                return;

            case Phase.WarmOneHundredTwentyTwoMoving:
                if (FramesInPhase >= 90) BeginMeasurement(Phase.MeasureOneHundredTwentyTwoMoving);
                return;

            case Phase.MeasureOneHundredTwentyTwoMoving:
                SampleFrame();
                if (FrameMilliseconds.Count >= 120)
                {
                    Report("122_moving");
                    if (CameraSweepRequested)
                    {
                        SetCameraLiftTarget(-1100f);
                        EnterPhase(Phase.WarmCameraNear);
                    }
                    else EnterPhase(Phase.Complete);
                }
                return;

            case Phase.WarmCameraNear:
                if (FramesInPhase >= 120) BeginMeasurement(Phase.MeasureCameraNear);
                return;
            case Phase.MeasureCameraNear:
                SampleFrame();
                if (FrameMilliseconds.Count >= 180)
                {
                    Report("122_camera_near");
                    SetCameraLiftTarget(0f);
                    EnterPhase(Phase.WarmCameraFar);
                }
                return;
            case Phase.WarmCameraFar:
                if (FramesInPhase >= 120) BeginMeasurement(Phase.MeasureCameraFar);
                return;
            case Phase.MeasureCameraFar:
                SampleFrame();
                if (FrameMilliseconds.Count >= 180)
                {
                    Report("122_camera_far");
                    EnterPhase(Phase.Complete);
                }
                return;

            case Phase.Complete:
                Debug.Log("[C2:FOUNDATION BENCH] COMPLETE formations=" +
                          (_scaleMode ? "50,122" : "5,12") +
                          " movementSource=C2FormationRuntimeV167LikeOriginal.TryIssueMoveV167LikeOriginal");
                Finish(0);
                return;
        }
    }

    private static int FramesInPhase => Time.frameCount - _phaseStartFrame;

    private static void EnterPhase(Phase next)
    {
        _phase = next;
        _phaseStartFrame = Time.frameCount;
    }

    private static void BeginMeasurement(Phase phase)
    {
        FrameMilliseconds.Clear();
        UnitMeasurementStarts.Clear();
        foreach (var info in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
        {
            var unit = info != null ? info.RuntimeLinkCachedLikeOriginal?.Runtime : null;
            if (unit == null) continue;
            UnitMeasurementStarts.Add(new UnitMeasurementStart
            {
                Unit = unit, Position = unit.WorldPosition, AnimationFrame = unit.CurrentFrameLong,
                OutsideDrawList = !unit.VisibleInOriginalDrawUnitsLikeOriginal
            });
        }
        _renderedAtMeasureStart = _renderedFrames;
        if (CpuTraceRequested && phase == Phase.MeasureOneHundredTwentyTwoMoving)
        {
            UnityEditorInternal.ProfilerDriver.profileEditor = true;
            Profiler.logFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "C2DiagnosticLogs", "unit_cpu_trace.raw");
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
        }
        BeginFrameMarkerMeasurement();
        EnterPhase(phase);
    }

    private static void SampleFrame()
    {
        FrameMilliseconds.Add(Time.unscaledDeltaTime * 1000.0f);
        for (int i = 0; i < FrameMarkerRecorders.Length; i++)
        {
            Recorder recorder = FrameMarkerRecorders[i];
            if (recorder == null || !recorder.isValid) continue;
            FrameMarkerNanoseconds[i] += recorder.elapsedNanoseconds;
            FrameMarkerBlocks[i] += recorder.sampleBlockCount;
        }
    }

    private static void BeginFrameMarkerMeasurement()
    {
        for (int i = 0; i < FrameMarkerRecorders.Length; i++)
        {
            if (FrameMarkerRecorders[i] != null)
                FrameMarkerRecorders[i].enabled = false;
            FrameMarkerNanoseconds[i] = 0L;
            FrameMarkerBlocks[i] = 0L;
            Recorder recorder = Recorder.Get(FrameMarkerNames[i]);
            FrameMarkerRecorders[i] = recorder;
            if (recorder != null && recorder.isValid)
                recorder.enabled = true;
        }
    }

    private static bool TryOpenEditorPlateauLikeOriginal()
    {
        MenuBootstrap bootstrap = UnityEngine.Object.FindObjectOfType<MenuBootstrap>();
        if (bootstrap == null)
        {
            GameObject host = new GameObject("C2_FoundationBenchmark_MenuBootstrap");
            bootstrap = host.AddComponent<MenuBootstrap>();
            _bootstrapCreatedFrame = Time.frameCount;
            Debug.Log("[C2:FOUNDATION BENCH] created isolated MenuBootstrap because batch play mode supplied a temporary scene");
            return false;
        }
        if (_bootstrapCreatedFrame >= 0 && Time.frameCount - _bootstrapCreatedFrame < 8)
            return false;

        MenuActionSink.SingleBattlesShowBattles = false;
        MenuActionSink.SingleBattlesShowLoad = false;
        MenuActionSink.SingleBattlesArcadeModeEnabled = false;
        MenuActionSink.SingleBattlesSelectedId = "EditorPlateau";
        C2MapLoadLighting.ApplyMapLoadDefaultsLikeOriginal();
        C2BattleTerrainMode.OpenFromBattles(bootstrap, true);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        return true;
    }

    private static bool ResolveMapAndPalette()
    {
        _mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
        _palette = UnityEngine.Object.FindObjectOfType<C2EditorTestPaletteV332LikeOriginal>();
        if (_mode == null || _palette == null) return false;

        _spawnFormation = typeof(C2EditorTestPaletteV332LikeOriginal).GetMethod(
            "SpawnFormationLikeOriginal", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo allField = typeof(C2EditorTestPaletteV332LikeOriginal).GetField(
            "_all", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo selectedField = typeof(C2EditorTestPaletteV332LikeOriginal).GetField(
            "_selected", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_spawnFormation == null || allField == null || selectedField == null)
            throw new MissingMemberException("Editor formation placement members were not found.");

        var all = allField.GetValue(_palette) as List<C2OriginalProduceItemV13>;
        C2OriginalProduceItemV13 selected = SelectLargestFormationMember(all);
        if (selected == null)
            throw new InvalidOperationException("No formation-capable editor unit was found.");
        selectedField.SetValue(_palette, selected);

        float centerX;
        float centerY;
        if (!_mode.C2NoUnitWorldToOriginalPixelLikeOriginal(Vector3.zero, out centerX, out centerY))
            throw new InvalidOperationException("Could not resolve the plateau center to original coordinates.");
        _centerRealX = centerX * 16.0f;
        _centerRealY = centerY * 16.0f;
        Debug.Log("[C2:FOUNDATION BENCH] map_ready selected=" + selected.UnitId +
                  " centerReal=" + F(_centerRealX) + "/" + F(_centerRealY));
        return true;
    }

    private static C2OriginalProduceItemV13 SelectLargestFormationMember(List<C2OriginalProduceItemV13> all)
    {
        C2OriginalProduceItemV13 best = null;
        int bestCount = 0;
        for (int i = 0; all != null && i < all.Count; i++)
        {
            C2OriginalProduceItemV13 item = all[i];
            if (item == null || item.Building || string.IsNullOrEmpty(item.UnitId)) continue;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record;
            if (!C2FormationCreateCatalogV165LikeOriginal.TryResolveForUnitMemberIdLikeOriginal(
                    item.UnitId, 0, out record) || record == null) continue;
            for (int n = 0; n < record.Options.Count; n++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = record.Options[n];
                if (option == null || option.UnitCount <= 0 || option.UnitCount > 120) continue;
                if (option.UnitCount <= bestCount) continue;
                best = item;
                bestCount = option.UnitCount;
            }
        }
        return best;
    }

    private static void SpawnUntilFormationCount(int target)
    {
        if (_palette == null || _spawnFormation == null)
            throw new InvalidOperationException("Formation palette is not ready.");

        for (int i = _lastSpawnedFormationCount; i < target; i++)
        {
            int columns = _scaleMode ? 12 : 4;
            float centerColumn = _scaleMode ? 5.5f : 1.5f;
            float centerRow = _scaleMode ? 5.0f : 1.0f;
            int col = i % columns;
            int row = i / columns;
            int realX = Mathf.RoundToInt(_centerRealX + (col - centerColumn) * 4800.0f);
            int realY = Mathf.RoundToInt(_centerRealY + (row - centerRow) * 4800.0f);
            _spawnFormation.Invoke(_palette, new object[] { realX, realY });
        }
        _lastSpawnedFormationCount = target;

        if (_coreOnlyMode)
            DisableNonCoreBehavioursForDiagnosis();

        C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
            C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
        Debug.Log("[C2:FOUNDATION BENCH] spawnedFormations=" + target.ToString(CultureInfo.InvariantCulture) +
                  " runtimeUnits=" + (units != null ? units.Length : 0).ToString(CultureInfo.InvariantCulture));
    }

    private static void DisableNonCoreBehavioursForDiagnosis()
    {
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        var disabled = new List<string>();
        for (int i = 0; behaviours != null && i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour is C2UnitOriginalRuntimeAndRendererV1) continue;
            if (!behaviour.gameObject.scene.IsValid()) continue;
            if (!behaviour.enabled) continue;
            behaviour.enabled = false;
            disabled.Add(behaviour.GetType().Name);
        }
        Debug.Log("[C2:FOUNDATION BENCH CORE ONLY] disabled=" + string.Join(",", disabled));
    }

    private static void IssueMoveOrder(float deltaX, float deltaY)
    {
        C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
            C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
        int issued;
        string audit;
        bool ok = C2FormationRuntimeV167LikeOriginal.TryIssueMoveV167LikeOriginal(
            units, _centerRealX + deltaX, _centerRealY + deltaY, false, 0,
            "foundation_benchmark", out issued, out audit);
        Debug.Log("[C2:FOUNDATION BENCH] move ok=" + ok +
                  " issued=" + issued.ToString(CultureInfo.InvariantCulture) +
                  " audit=" + audit);
        if (!ok || issued <= 0)
            throw new InvalidOperationException("Formation move order failed: " + audit);
    }

    private static void StopAllUnits()
    {
        C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
            C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
        for (int i = 0; units != null && i < units.Length; i++)
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[i];
            if (unit != null) unit.StopMoveAndFaceDirectionLikeOriginal(unit.RealDir);
        }
    }

    private static void Report(string label)
    {
        Debug.Log("[C2:TEST CONFIG] label=" + label + " codeOptimization=" +
                  UnityEditor.Compilation.CompilationPipeline.codeOptimization +
                  " render=" + RenderRequested + " coreOnly=" + _coreOnlyMode + " simulationOnly=" + _simulationOnlyMode);
        float[] sorted = FrameMilliseconds.ToArray();
        Array.Sort(sorted);
        double sum = 0.0;
        for (int i = 0; i < sorted.Length; i++) sum += sorted[i];
        double averageMs = sorted.Length > 0 ? sum / sorted.Length : 0.0;
        double averageFps = averageMs > 0.00001 ? 1000.0 / averageMs : 0.0;
        int moved = 0, offscreenBoth = 0, offscreenMoved = 0, offscreenAnimationChanged = 0;
        foreach (var start in UnitMeasurementStarts)
        {
            bool positionChanged = !start.Position.Equals(start.Unit.WorldPosition);
            if (positionChanged) moved++;
            if (start.OutsideDrawList && !start.Unit.VisibleInOriginalDrawUnitsLikeOriginal)
            {
                offscreenBoth++;
                if (positionChanged) offscreenMoved++;
                if (start.AnimationFrame != start.Unit.CurrentFrameLong) offscreenAnimationChanged++;
            }
        }
        int simMarker = Array.IndexOf(FrameMarkerNames, "C2.Unit.SimulationV370");
        double simHz = sum > 0 && simMarker >= 0 ? FrameMarkerBlocks[simMarker] * 1000.0 / sum : 0;
        Debug.Log("[C2:SIMULATION PROOF] label=" + label + " simulationHz=" + simHz.ToString("0.00", CultureInfo.InvariantCulture) +
                  " unitsPositionChanged=" + moved + " outsideDrawAtBothEnds=" + offscreenBoth +
                  " outsideMoved=" + offscreenMoved + " outsideAnimationChanged=" + offscreenAnimationChanged);
        float p95 = Percentile(sorted, 0.95f);
        float p99 = Percentile(sorted, 0.99f);
        C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
            C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
        MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        int unitProxyCount = 0;
        for (int i = 0; transforms != null && i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t != null && t.name.StartsWith("C2UnitOriginal_", StringComparison.Ordinal))
                unitProxyCount++;
        }
        long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
        long totalReserved = Profiler.GetTotalReservedMemoryLong();
        long graphics = Profiler.GetAllocatedMemoryForGraphicsDriver();
        // Unity's bundled Mono can report zero for Process.WorkingSet64.
        // Query the OS counters for this process, or explicitly report unavailable.
        ProcessMemoryCounters memory;
        if (Application.platform == RuntimePlatform.WindowsEditor &&
            GetProcessMemoryInfo(GetCurrentProcess(), out memory, (uint)Marshal.SizeOf<ProcessMemoryCounters>()))
            Debug.Log("[C2:PROCESS MEMORY] label=" + label + " editorWorkingSetMiB=" + ToMiB((long)memory.WorkingSet.ToUInt64()) +
                      " editorPrivateMiB=" + ToMiB((long)memory.PrivateUsage.ToUInt64()) +
                      " note=whole_editor_process_not_standalone_player source=Windows_GetProcessMemoryInfo");
        else Debug.Log("[C2:PROCESS MEMORY] label=" + label + " unavailable=true");
        int drawTotalUnits = 0;
        int drawCandidates = 0;
        int drawVisibleUnits = 0;
        bool drawBoundsValid = false;
        bool drawFellBackToAll = false;
        Vector4 drawBounds = Vector4.zero;
        C2UnitOriginalRuntimeAndRendererV1 drawCore =
            UnityEngine.Object.FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
        if (drawCore != null)
        {
            drawCore.C2GetOriginalDrawUnitsAuditV372LikeOriginal(
                out drawTotalUnits,
                out drawCandidates,
                out drawVisibleUnits,
                out drawBoundsValid,
                out drawFellBackToAll,
                out drawBounds);
            ValidateScreenProjection(drawCore);
        }
        float drawWidthOriginalPixels = drawBoundsValid ? Mathf.Max(0.0f, drawBounds.z - drawBounds.x) : 0.0f;
        float drawHeightOriginalPixels = drawBoundsValid ? Mathf.Max(0.0f, drawBounds.w - drawBounds.y) : 0.0f;
        int visualRenderRelevant = 0;
        int visualFrameApplyCalls = 0;
        int visualFrameScalarHits = 0;
        int visualReadyFrameCacheHits = 0;
        int visualTextureCacheHits = 0;
        int visualTextureUploads = 0;
        int visualBatchSubmitted = 0;
        int visualActiveBatches = 0;
        int visualTotalBatches = 0;
        if (drawCore != null)
        {
            drawCore.C2GetVisualPipelineAuditV373LikeOriginal(
                out visualRenderRelevant,
                out visualFrameApplyCalls,
                out visualFrameScalarHits,
                out visualReadyFrameCacheHits,
                out visualTextureCacheHits,
                out visualTextureUploads,
                out visualBatchSubmitted,
                out visualActiveBatches,
                out visualTotalBatches);
        }

        Debug.Log("[C2:FOUNDATION BENCH RESULT] label=" + label +
                  " frames=" + sorted.Length.ToString(CultureInfo.InvariantCulture) +
                  " units=" + (units != null ? units.Length : 0).ToString(CultureInfo.InvariantCulture) +
                  " meshRenderers=" + (renderers != null ? renderers.Length : 0).ToString(CultureInfo.InvariantCulture) +
                  " transforms=" + (transforms != null ? transforms.Length : 0).ToString(CultureInfo.InvariantCulture) +
                  " unitProxies=" + unitProxyCount.ToString(CultureInfo.InvariantCulture) +
                  " avgMs=" + averageMs.ToString("0.000", CultureInfo.InvariantCulture) +
                  " avgFps=" + averageFps.ToString("0.0", CultureInfo.InvariantCulture) +
                  " p95Ms=" + p95.ToString("0.000", CultureInfo.InvariantCulture) +
                  " p99Ms=" + p99.ToString("0.000", CultureInfo.InvariantCulture) +
                  " totalAllocatedMiB=" + ToMiB(totalAllocated) +
                  " totalReservedMiB=" + ToMiB(totalReserved) +
                  " graphicsMiB=" + ToMiB(graphics) +
                  " drawTotalUnits=" + drawTotalUnits.ToString(CultureInfo.InvariantCulture) +
                  " drawCandidates=" + drawCandidates.ToString(CultureInfo.InvariantCulture) +
                  " drawVisibleUnits=" + drawVisibleUnits.ToString(CultureInfo.InvariantCulture) +
                  " drawBoundsValid=" + drawBoundsValid +
                  " drawFallbackAll=" + drawFellBackToAll +
                  " drawWidthOriginalPx=" + drawWidthOriginalPixels.ToString("0.0", CultureInfo.InvariantCulture) +
                  " drawHeightOriginalPx=" + drawHeightOriginalPixels.ToString("0.0", CultureInfo.InvariantCulture) +
                  " visualRenderRelevant=" + visualRenderRelevant.ToString(CultureInfo.InvariantCulture) +
                  " visualFrameApplyCalls=" + visualFrameApplyCalls.ToString(CultureInfo.InvariantCulture) +
                  " visualFrameScalarHits=" + visualFrameScalarHits.ToString(CultureInfo.InvariantCulture) +
                  " visualHotFrameHits=" + (drawCore != null ? drawCore.VisualHotFrameHitsV374LikeOriginal : 0).ToString(CultureInfo.InvariantCulture) +
                  " visualReadyFrameCacheHits=" + visualReadyFrameCacheHits.ToString(CultureInfo.InvariantCulture) +
                  " visualTextureCacheHits=" + visualTextureCacheHits.ToString(CultureInfo.InvariantCulture) +
                  " visualTextureUploads=" + visualTextureUploads.ToString(CultureInfo.InvariantCulture) +
                  " visualBatchSubmitted=" + visualBatchSubmitted.ToString(CultureInfo.InvariantCulture) +
                  " visualActiveBatches=" + visualActiveBatches.ToString(CultureInfo.InvariantCulture) +
                  " visualTotalBatches=" + visualTotalBatches.ToString(CultureInfo.InvariantCulture));
        if (RenderRequested)
        {
            int rendered = _renderedFrames - _renderedAtMeasureStart;
            Debug.Log("[C2:RENDER PROOF] label=" + label + " renderedCameraFrames=" + rendered +
                      " output=" + _renderTarget.width + "x" + _renderTarget.height);
            if (rendered == 0) throw new InvalidOperationException("No camera render occurred; FPS is not a rendered-scene measurement.");
            SaveRenderedFrame(label);
        }

        string markerAudit = string.Empty;
        for (int i = 0; i < FrameMarkerRecorders.Length; i++)
        {
            Recorder recorder = FrameMarkerRecorders[i];
            if (recorder == null || !recorder.isValid) continue;
            double markerMs = FrameMarkerNanoseconds[i] / 1000000.0 / Math.Max(1, sorted.Length);
            markerAudit += " " + FrameMarkerNames[i] + "MsPerFrame=" +
                           markerMs.ToString("0.000", CultureInfo.InvariantCulture) +
                           " blocks=" + FrameMarkerBlocks[i].ToString(CultureInfo.InvariantCulture);
            recorder.enabled = false;
        }
        Debug.Log("[C2:FOUNDATION BENCH MARKERS] label=" + label + markerAudit);
        if (CpuTraceRequested && label == "122_moving")
            ReportCpuTrace();

        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        var behaviourCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; behaviours != null && i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null) continue;
            string typeName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
            int count;
            behaviourCounts.TryGetValue(typeName, out count);
            behaviourCounts[typeName] = count + 1;
        }
        string[] topBehaviours = behaviourCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(20)
            .Select(pair => pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture))
            .ToArray();
        Debug.Log("[C2:FOUNDATION BENCH BEHAVIOURS] label=" + label +
                  " total=" + (behaviours != null ? behaviours.Length : 0).ToString(CultureInfo.InvariantCulture) +
                  " top=" + string.Join(",", topBehaviours));
    }

    private static float Percentile(float[] sorted, float percentile)
    {
        if (sorted == null || sorted.Length == 0) return 0.0f;
        int index = Mathf.Clamp(Mathf.CeilToInt(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
        return sorted[index];
    }

    private static void ReportCpuTrace()
    {
        Profiler.enabled = false;
        Profiler.enableBinaryLog = false;
        UnityEditorInternal.ProfilerDriver.LoadProfile(Profiler.logFile, false);
        Debug.Log("[C2:CPU TRACE] first=" + UnityEditorInternal.ProfilerDriver.firstFrameIndex +
                  " last=" + UnityEditorInternal.ProfilerDriver.lastFrameIndex);
        var totals = new Dictionary<string, double>(StringComparer.Ordinal);
        int frames = 0;
        int last = UnityEditorInternal.ProfilerDriver.lastFrameIndex - 1;
        for (int f = Math.Max(UnityEditorInternal.ProfilerDriver.firstFrameIndex, last - 29); f <= last; f++)
        {
            using (var data = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(f, 0))
            {
                if (!data.valid) continue;
                frames++;
                for (int s = 0; s < data.sampleCount; s++)
                {
                    string name = data.GetSampleName(s);
                    double time = data.GetSampleTimeMs(s);
                    double sum;
                    totals.TryGetValue(name, out sum);
                    totals[name] = sum + time;
                }
            }
        }
        foreach (var pair in totals.OrderByDescending(p => p.Value).Take(50))
            Debug.Log("[C2:CPU TRACE] frames=" + frames + " sample=" + pair.Key +
                      " inclusiveMs=" + (pair.Value / Math.Max(1, frames)).ToString("0.000", CultureInfo.InvariantCulture));
        Profiler.enabled = false;
    }

    private static void PrepareRenderTarget()
    {
        if (_renderTarget != null) return;
        _renderCamera = _mode.GetActiveBattleCameraLikeOriginal();
        if (_renderCamera == null) throw new InvalidOperationException("No active battle camera for render proof.");
        _renderTarget = new RenderTexture(1904, 1040, 24, RenderTextureFormat.ARGB32);
        _renderTarget.name = "C2_Benchmark_RenderProof";
        _renderTarget.Create();
        _renderCamera.targetTexture = _renderTarget;
        _renderedFrames = 0;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += CountRenderedCamera;
        Debug.Log("[C2:RENDER PROOF] camera=" + _renderCamera.name +
                  " orthographic=" + _renderCamera.orthographic + " size=" + _renderCamera.orthographicSize +
                  " aspect=" + _renderCamera.aspect + " pixelRect=" + _renderCamera.pixelRect);
    }

    private static void ValidateScreenProjection(C2UnitOriginalRuntimeAndRendererV1 core)
    {
        IReadOnlyList<C2UnitOriginalRuntime> units;
        Camera camera;
        if (!core.C2TryGetVisibleUnitsForPickingV375LikeOriginal(out units, out camera)) return;
        var projection = new C2UnitOriginalRuntimeAndRendererV1.UnitScreenProjectionV376LikeOriginal(camera);
        float error = 0f;
        float matrixError = 0f;
        int checkedUnits = 0;
        int onScreenQuads = 0;
        var distinctUnits = new HashSet<C2UnitOriginalRuntime>();
        Rect viewport = camera.pixelRect;
        for (int i = 0; i < units.Count; i++)
        {
            if (!distinctUnits.Add(units[i]) || !units[i].VisibleInOriginalDrawUnitsLikeOriginal)
                throw new InvalidOperationException("Draw list duplicate or visibility state mismatch.");
            Rect screenRect;
            Vector2 screenAnchor;
            if (core.TryGetRuntimeScreenRectProjectedV376LikeOriginal(units[i], in projection, out screenRect, out screenAnchor) &&
                viewport.Overlaps(screenRect)) onScreenQuads++;
        }
        for (int i = 0; i < units.Count; i += Math.Max(1, units.Count / 128))
        {
            var unit = units[i];
            Matrix4x4 referenceMatrix = Matrix4x4.TRS(unit.WorldPosition, unit.WorldRotationLikeOriginal, unit.WorldScaleLikeOriginal);
            Matrix4x4 cachedMatrix = C2UnitOriginalRuntimeAndRendererV1.GetUnitLocalToWorldMatrixLikeOriginal(unit);
            for (int element = 0; element < 16; element++)
                matrixError = Mathf.Max(matrixError, Mathf.Abs(referenceMatrix[element] - cachedMatrix[element]));
            Rect nativeRect, projectedRect;
            Vector2 nativeAnchor, projectedAnchor;
            bool nativeOk = core.TryGetRuntimeScreenRectLikeOriginal(units[i], camera, out nativeRect, out nativeAnchor);
            bool projectedOk = core.TryGetRuntimeScreenRectProjectedV376LikeOriginal(units[i], in projection, out projectedRect, out projectedAnchor);
            if (nativeOk != projectedOk) throw new InvalidOperationException("Projection visibility parity mismatch.");
            if (!nativeOk) continue;
            error = Mathf.Max(error, (nativeAnchor - projectedAnchor).magnitude,
                Mathf.Abs(nativeRect.xMin - projectedRect.xMin), Mathf.Abs(nativeRect.yMin - projectedRect.yMin),
                Mathf.Abs(nativeRect.xMax - projectedRect.xMax), Mathf.Abs(nativeRect.yMax - projectedRect.yMax));
            checkedUnits++;
        }
        Debug.Log("[C2:PICK PROJECTION PARITY] checked=" + checkedUnits + " maxErrorPx=" + error.ToString("0.00000", CultureInfo.InvariantCulture));
        Debug.Log("[C2:DRAW VALIDATION] matrixMaxError=" + matrixError.ToString("0.000000", CultureInfo.InvariantCulture) +
                  " uniqueSubmitted=" + distinctUnits.Count + " screenRectIntersections=" + onScreenQuads +
                  " note=rect_intersection_not_unoccluded_pixel_count");
        if (error > 0.1f) throw new InvalidOperationException("Projection parity error exceeds broad-phase tolerance.");
        if (matrixError > 0.000001f) throw new InvalidOperationException("Cached TRS differs from native TRS.");
    }

    private static void SetCameraLiftTarget(float target)
    {
        var field = typeof(C2BattleTerrainMode).GetField("_strictZoomTargetLikeOriginal", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new InvalidOperationException("Original camera lift target unavailable.");
        // Use the exact state written by the wheel. Do not alter the camera
        // Transform, map coordinates, FOV or the separate free-camera mode.
        field.SetValue(_mode, target);
        Debug.Log("[C2:CAMERA SWEEP] target=" + target + " transition=existing_smooth_camera_update");
    }

    private static void CountRenderedCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
    {
        if (camera == _renderCamera) _renderedFrames++;
    }

    private static void SaveRenderedFrame(string label)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = _renderTarget;
        var image = new Texture2D(_renderTarget.width, _renderTarget.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine("C2DiagnosticLogs", "render_proof_" + label + ".png"), image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        RenderTexture.active = previous;
    }

    private static string ToMiB(long bytes)
    {
        return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string F(float value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static void Finish(int exitCode)
    {
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= CountRenderedCamera;
        if (_renderCamera != null && _renderCamera.targetTexture == _renderTarget) _renderCamera.targetTexture = null;
        if (_renderTarget != null) UnityEngine.Object.DestroyImmediate(_renderTarget);
        _renderTarget = null;
        _renderCamera = null;
        SessionState.SetInt(ExitCodeKey, exitCode);
        SessionState.SetBool(ExitPendingKey, true);
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        else
            EditorApplication.Exit(exitCode);
    }
}
#endif
