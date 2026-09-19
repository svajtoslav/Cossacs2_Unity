#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cossacks2Bridge.UnityAdapters.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-only reproduction through the real construction/SMP entry point.
[InitializeOnLoad]
public static class C2BuildingPlacementRegressionEditor
{
    const string Key = "C2.BuildPlacementRegression";
    static int phase, phaseFrame;
    static C2BattleTerrainMode mode;
    static double started;
    static float originalX, originalY;
    static int publishedBeforeCancel;
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static C2BuildingPlacementRegressionEditor() { EditorApplication.update += Tick; }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        C2RegressionSceneScopeEditor.Begin();
        try
        {
            EditorSceneManager.OpenScene(C2RegressionSceneScopeEditor.MenuScene);
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".started", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); C2RegressionSceneScopeEditor.Finish(1); }
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            started = SessionState.GetFloat(Key + ".started", 0);
            if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Building regression timeout");
            if (phase == 0)
            {
                if (Time.frameCount < 10) return;
                var open = typeof(C2FoundationBenchmarkEditor).GetMethod("TryOpenEditorPlateauLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic);
                if (!(bool)open.Invoke(null, null)) return;
                phase = 1; phaseFrame = Time.frameCount; return;
            }
            if (phase == 1)
            {
                if (Time.frameCount - phaseFrame < 60) return;
                mode = UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>();
                if (mode == null || !mode.C2NoUnitWorldToOriginalPixelLikeOriginal(Vector3.zero, out float x, out float y))
                    throw new InvalidOperationException("Map not ready");
                originalX = x; originalY = y;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                bool ok = mode.C2BuildRuntimeCreateConstructionLikeOriginal("EngKaz", "BldKaz(EN)", 0,
                    Mathf.RoundToInt(x * 16), Mathf.RoundToInt(y * 16), "placement_regression", out GameObject site, out string audit);
                Debug.Log("[C2 BUILD REGRESSION] create=" + ok + " elapsedMs=" + clock.Elapsed.TotalMilliseconds + " " + audit);
                if (!ok || site == null || site.GetComponentsInChildren<MeshRenderer>().Length == 0)
                    throw new InvalidOperationException("Construction missing or invisible");
                if (clock.Elapsed.TotalSeconds > 1.0) throw new InvalidOperationException("Placement still blocks over one second");
                phase = 2; phaseFrame = Time.frameCount; return;
            }
            if (mode.SmpFailedChunksForDiagnostics != 0) throw new InvalidOperationException("SMP bake failed");
            if (phase == 2)
            {
                // Re-stamp the same chunks only after the first worker took its
                // snapshot. This must discard stale output, not erase the new stamp.
                if (Field(mode, "_smpBakeCancellation") == null) return;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                bool ok = mode.C2BuildRuntimeCreateConstructionLikeOriginal("EngKaz", "BldKaz(EN)", 0,
                    Mathf.RoundToInt((originalX + 64) * 16), Mathf.RoundToInt(originalY * 16),
                    "overlapping_stamp_regression", out GameObject site, out string audit);
                if (!ok || site == null || clock.Elapsed.TotalSeconds > 1) throw new InvalidOperationException("Second placement blocked/failed");
                Debug.Log("[C2 BUILD REGRESSION] overlapping createMs=" + clock.Elapsed.TotalMilliseconds + " " + audit);
                phase = 3; return;
            }
            if (phase == 4)
            {
                if (Field(mode, "_smpBakeCancellation") == null) return;
                Call("C2SmpCancelPendingBake");
                publishedBeforeCancel = mode.SmpPublishedChunksForDiagnostics;
                phaseFrame = Time.frameCount; phase = 5; return;
            }
            if (phase == 5)
            {
                if (Time.frameCount - phaseFrame < 120) return;
                if (mode.SmpPendingChunksForDiagnostics != 0 || Field(mode, "_smpBakeCoroutine") != null ||
                    mode.SmpPublishedChunksForDiagnostics != publishedBeforeCancel)
                    throw new InvalidOperationException("Canceled bake published stale terrain");
                Debug.Log("[C2 BUILD REGRESSION] PASS cancel/no-late-publish");
                SessionState.SetBool(Key, false); C2RegressionSceneScopeEditor.Finish(0); return;
            }
            if (mode.SmpPendingChunksForDiagnostics > 0) return;
            if (mode.SmpPublishedChunksForDiagnostics < 1 || Time.frameCount - phaseFrame < 10)
                throw new InvalidOperationException("Expected completed background baking and continued frames");
            if (mode.SmpDiscardedStaleChunksForDiagnostics < 1) throw new InvalidOperationException("No stale-worker test coverage");
            CheckPublishedPixelsAndRequeue();
            Debug.Log("[C2 BUILD REGRESSION] PASS published=" + mode.SmpPublishedChunksForDiagnostics +
                " staleDiscarded=" + mode.SmpDiscardedStaleChunksForDiagnostics + " framesWhileBaking=" + (Time.frameCount - phaseFrame));
            phase = 4;
        }
        catch (Exception e)
        {
            Debug.LogException(e); SessionState.SetBool(Key, false); C2RegressionSceneScopeEditor.Finish(1);
        }
    }

    static object Field(object target, string name) => target.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
    static object Call(string name, params object[] args) => typeof(C2BattleTerrainMode)
        .GetMethod(name, PrivateInstance).Invoke(mode, args);

    static void CheckPublishedPixelsAndRequeue()
    {
        var patches = (IList)Field(mode, "_c2SmpSurfacePatchesV1LikeOriginal");
        object map = Field(mode, "_map"), kernel = Field(mode, "_lastBuiltTerrainKernel"), inputs = Field(mode, "_smpBakeInputs");
        var createRegion = typeof(C2BattleTerrainMode).GetMethod("CreateTerrainSoftwareChunkRegionLikeOriginal", BindingFlags.Static | BindingFlags.NonPublic);
        long checkedPixels = 0;
        foreach (object patch in patches)
        {
            int x0 = (int)Field(patch, "MinCellX"), x1 = (int)Field(patch, "MaxCellXExclusive");
            int y0 = (int)Field(patch, "MinCellY"), y1 = (int)Field(patch, "MaxCellYExclusive");
            object region = createRegion.Invoke(null, new[] { map, kernel, (object)x0, x1, y0, y1 });
            // Synchronous reference is intentional ONLY inside this batch test.
            var expected = (Color32[])Call("BakeTerrainChunkPixelsSoftwareLikeOriginal", map, kernel, region, inputs, CancellationToken.None);
            var actual = ((Texture2D)Field(patch, "Texture")).GetPixels32();
            if (actual.Length != expected.Length) throw new InvalidOperationException("SMP pixel dimensions");
            for (int i = 0; i < actual.Length; ++i)
                if (!actual[i].Equals(expected[i])) throw new InvalidOperationException("Latest-stamp pixel mismatch at " + i);
            checkedPixels += actual.Length;
        }
        Debug.Log("[C2 BUILD REGRESSION] latest-snapshot/synchronous pixels match=" + checkedPixels);
        object first = patches[0];
        object[] stamp = { (int)Field(first, "MinCellX") + 1, (int)Field(first, "MinCellY") + 1,
            (int)Field(first, "MaxCellXExclusive") - 1, (int)Field(first, "MaxCellYExclusive") - 1, null };
        if (!(bool)Call("C2SmpRebuildSurfacePatchV1LikeOriginal", stamp)) throw new InvalidOperationException("Cancel test failed to queue");
    }
}
#endif
