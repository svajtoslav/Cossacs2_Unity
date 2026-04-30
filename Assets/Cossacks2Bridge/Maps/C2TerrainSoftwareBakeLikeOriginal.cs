using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const int TerrainSoftwareChunkCellsLikeOriginal = 64;
        private const int TerrainSoftwarePixelsPerCellLikeOriginal = 40;
        private const float TerrainSoftwareAlphaClipLikeOriginal = 39.0f / 255.0f;
        private const float TerrainSoftwareRasterToleranceLikeOriginal = -0.0015f;
        private const int TerrainSoftwareFactureFallbackAlphaLikeAdapted = 192;
        private const int TerrainSoftwareFactureFallbackCoverageTargetLikeAdapted = 255;
        private const int TerrainSoftwareTex44RevealTileIdLikeAdapted = 44;
        private const float TerrainSoftwareTex44RevealAlphaFloorLikeAdapted = 0.85f;
        private const float TerrainSoftwareTex44ProtectionOverlayAttenuationLikeAdapted = 0.25f;
        private const bool TerrainSoftwareBaseSoftBlendEnabledLikeAdapted = false;
        private const int TerrainSoftwareBaseTileSoftBlendRadiusLikeAdapted = 10;
        private const int TerrainSoftwareBaseTileSoftBlendPassesLikeAdapted = 6;
        private const float TerrainSoftwareBaseTileSoftBlendStrengthLikeAdapted = 0.92f;
        private const float TerrainSoftwareBaseOverlayAlphaClipLikeAdapted = 1.0f / 255.0f;
        private const float TerrainSoftwareBaseOverlayTileIdAlphaLikeAdapted = 8.0f / 255.0f;
        private const float TerrainSoftwareBaseSoftBlendThresholdLikeAdapted = 10.0f;
        private const float TerrainSoftwareBaseSoftBlendRangeLikeAdapted = 56.0f;
        private const bool TerrainSoftwareBaseWeightedCompositeV3LikeAdapted = true;
        private const float TerrainSoftwareBaseWeightedCompositeMinAlphaV3LikeAdapted = 1.0f / 255.0f;
        private const float TerrainSoftwareBaseWeightedCompositeOverlayStrengthV3LikeAdapted = 1.0f;
        private const bool TerrainSoftwareFactureAllSoftEdgesV4LikeAdapted = true;
        private const float TerrainSoftwareFactureAlphaRefV4LikeAdapted = 0.0f;
        private const float TerrainSoftwareFactureMinVisibleAlphaV4LikeAdapted = 1.0f / 255.0f;
        private const float TerrainSoftwareFactureEdgeFeatherPixelsV4LikeAdapted = 10.0f;
        private const bool TerrainSoftwareFactureNoTriangleEdgeFadeV5LikeAdapted = true;
        private const float TerrainSoftwareFactureCoverageSoftStartV5LikeAdapted = 30.6f / 255.0f;
        private const bool TerrainSoftwareDisableFactureFallbackV7LikeAdapted = false;
        private const bool TerrainSoftwareSafeHoleOnlyFactureFallbackV8LikeAdapted = true;
        private const int TerrainSoftwareSafeHoleCoverageThresholdV8LikeAdapted = 24;
        private const string TerrainSoftwarePersistentCacheVersionLikeOriginal = "FINAL_COLOR_POLISH_V4_GPU_COMPILEFIX_FORCE_REBAKE_TEXTURE_MAPSIDE_CACHE_V1";
        private const bool TerrainSoftwareMapSideChunkCacheV1LikeOriginal = true;
        private const int TerrainSoftwareMapSideChunkCacheVersionV1LikeOriginal = 1;
        private const uint TerrainSoftwareMapSideChunkCacheMagicV1LikeOriginal = 0x314B3243; // C2K1
        private const bool TerrainSoftwareFallbackStructureFeatherV1LikeAdapted = true;
        private const int TerrainSoftwareFallbackStructureFeatherRadiusV1LikeAdapted = 18;
        private const bool TerrainSoftwareFallbackStructureSprayV2LikeAdapted = true;
        private const float TerrainSoftwareFallbackStructureSprayWarpPixelsV3LikeAdapted = 6.5f;
        private const float TerrainSoftwareFallbackStructureSprayErodePixelsV3LikeAdapted = 7.5f;
        private const float TerrainSoftwareFallbackStructureSprayThresholdEdgeV3LikeAdapted = 0.90f;
        private const float TerrainSoftwareFallbackStructureSprayThresholdCenterV3LikeAdapted = 0.18f;
        private const float TerrainSoftwareFallbackStructureSprayMinAlphaV3LikeAdapted = 0.15f;
        private const bool TerrainSoftwareFallbackStructureFeatherDisableCacheV1LikeAdapted = true;
        private const bool TerrainSoftwareTerrainShadowOverlayV5LikeAdapted = true;
        private const float TerrainSoftwareTerrainShadowOverlayMaxAlphaV5LikeAdapted = 0.46f;
        private const float TerrainSoftwareTerrainShadowOverlayHeightScaleV5LikeAdapted = 1.00f;
        private const int TerrainSoftwareTerrainShadowOverlayDecayV5LikeAdapted = 10;
        private const int TerrainSoftwareTerrainShadowOverlaySpreadRadiusV5LikeAdapted = 1;
        private const float TerrainSoftwareTerrainShadowOverlayYOffsetV5LikeAdapted = 0.18f;
        private const float TerrainSoftwareCastShadowStartDepthV4LikeAdapted = 58.0f;
        private const float TerrainSoftwareCastShadowFullDepthV4LikeAdapted = 100.0f;
        private const int TerrainSoftwareCastShadowPostBlurRadiusV4LikeAdapted = 2;
        private const bool TerrainSoftwareFinalColorPolishV1LikeAdapted = true;
        private const float TerrainSoftwareFinalColorPolishWarmR_V1LikeAdapted = 1.055f;
        private const float TerrainSoftwareFinalColorPolishWarmG_V1LikeAdapted = 1.002f;
        private const float TerrainSoftwareFinalColorPolishWarmB_V1LikeAdapted = 0.962f;
        private const float TerrainSoftwareFinalColorPolishSaturationV1LikeAdapted = 1.030f;
        private const float TerrainSoftwareFinalColorPolishContrastV1LikeAdapted = 1.030f;
        private const float TerrainSoftwareFinalColorPolishGammaV1LikeAdapted = 1.000f;
        private const float TerrainSoftwareFinalColorPolishShadowWarmR_V1LikeAdapted = 0.040f;
        private const float TerrainSoftwareFinalColorPolishShadowWarmG_V1LikeAdapted = 0.006f;
        private const float TerrainSoftwareFinalColorPolishShadowCoolB_V1LikeAdapted = 0.055f;
        private static bool s_terrainSoftwareFinalColorPolishLoggedV1LikeAdapted;
        private static bool s_terrainSoftwareTerrainShadowOverlayLoggedV5LikeAdapted;
        private static bool s_terrainSoftwareFallbackStructureFeatherCacheLoggedV1LikeAdapted;
        private static bool s_terrainSoftwareFallbackStructureFeatherPathLoggedV1LikeAdapted;
        private static bool s_terrainSoftwarePersistentCacheWarningLoggedLikeOriginal;
        private static readonly object s_terrainSoftwareFactureCacheBuildLockLikeOriginal = new object();

        private struct TerrainSoftwareChunkRegionLikeOriginal
        {
            public int MinCellX;
            public int MaxCellXExclusive;
            public int MinCellY;
            public int MaxCellYExclusive;
            public int WidthPixels;
            public int HeightPixels;
            public Bounds FootprintBounds;
        }

        private struct TerrainSoftwareChunkJobLikeOriginal
        {
            public int ChunkX;
            public int ChunkY;
            public TerrainSoftwareChunkRegionLikeOriginal Region;
            public Color32[] Pixels;
            public bool Success;
            public string Error;
        }

        private sealed class TerrainSoftwareBakeInputsLikeOriginal
        {
            public Texture2D GroundAtlas;
            public Color32[] GroundPixels;
            public int GroundWidth;
            public int GroundHeight;
            public Texture2D CrossTex;
            public Color32[] CrossPixels;
            public int CrossWidth;
            public int CrossHeight;
            public Texture2D StandaloneTex44;
            public Color32[] StandaloneTex44Pixels;
            public int StandaloneTex44Width;
            public int StandaloneTex44Height;
            public TerrainTextureTablesLikeOriginal Tables;
            public readonly Dictionary<int, TerrainSoftwareFactureBakeCacheEntryLikeOriginal> FactureCache = new Dictionary<int, TerrainSoftwareFactureBakeCacheEntryLikeOriginal>();
            public readonly TerrainSoftwareFactureBakeCacheEntryLikeOriginal[] FactureCacheArray = new TerrainSoftwareFactureBakeCacheEntryLikeOriginal[256];
            public readonly bool[] FactureCacheInitialized = new bool[256];
            public bool PersistentChunkCacheEnabled;
            public string PersistentChunkCacheKey = string.Empty;
            public string PersistentChunkCacheDirectory = string.Empty;
            public int PersistentChunkCacheHits;
            public int PersistentChunkCacheMisses;
            public int PersistentChunkCacheWrites;
        }

        private sealed class TerrainSoftwareFactureBakeCacheEntryLikeOriginal
        {
            public int BucketTextureId;
            public Color32[] PlainDiffusePixels;
            public int PlainDiffuseWidth;
            public int PlainDiffuseHeight;
            public Color32[] Dot3DiffusePixels;
            public int Dot3DiffuseWidth;
            public int Dot3DiffuseHeight;
            public Color32[] NormalPixels;
            public int NormalWidth;
            public int NormalHeight;
        }

        private void BuildStrictOldSurfaceSoftwareBakedChunksLikeOriginal(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (map.Heights == null || map.Heights.Length == 0)
                throw new InvalidOperationException("Map has no SURF heights.");

            TerrainSoftwareBakeInputsLikeOriginal inputs = PrepareTerrainSoftwareBakeInputsLikeOriginal();
            if (inputs == null || inputs.GroundAtlas == null || inputs.GroundPixels == null || inputs.GroundPixels.Length == 0)
                throw new InvalidOperationException("Ground atlas is not available for software terrain bake.");

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(map);
            _lastBuiltTerrainKernel = kernel;
            _hasLastBuiltTerrainKernel = true;

            LogProtectedGroundIdAuditLikeAdapted(map);
            WriteTextureSourceAuditFilesLikeAdapted(map, inputs);

            var totalSwV11 = global::System.Diagnostics.Stopwatch.StartNew();

            // MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES:
            // Keep the exact middle-project pixels[] raster formula, but remove PNG cache/encode/decode
            // and bake chunk pixel buffers on CPU worker threads. Unity Texture2D/Mesh/GameObject creation
            // still stays on the main thread after the parallel pixel phase.
            inputs.PersistentChunkCacheEnabled = false;
            inputs.PersistentChunkCacheDirectory = string.Empty;
            inputs.PersistentChunkCacheKey = string.Empty;

            // Warm all static map/material/random tables on main thread before worker threads start.
            // Several helper paths use static Dictionaries; they must not initialize during Parallel.For.
            _ = GetTerrainTextureTablesLikeOriginal();
            _ = GetRandomTableLikeOriginal();

            var prewarmSwV11 = global::System.Diagnostics.Stopwatch.StartNew();
            if (!TerrainQualityFactureLayerDisabledLikeAdapted)
                PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal(inputs);
            prewarmSwV11.Stop();

            int totalCellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int totalCellsY = Mathf.Max(0, kernel.MaxCellYExclusive - kernel.MinCellY);
            int chunkCountX = Mathf.Max(1, Mathf.CeilToInt(totalCellsX / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int chunkCountY = Mathf.Max(1, Mathf.CeilToInt(totalCellsY / (float)TerrainSoftwareChunkCellsLikeOriginal));

            var jobs = new TerrainSoftwareChunkJobLikeOriginal[chunkCountX * chunkCountY];
            int jobCount = 0;

            for (int chunkY = 0; chunkY < chunkCountY; chunkY++)
            {
                int minCellY = kernel.MinCellY + chunkY * TerrainSoftwareChunkCellsLikeOriginal;
                int maxCellYExclusive = Mathf.Min(kernel.MaxCellYExclusive, minCellY + TerrainSoftwareChunkCellsLikeOriginal);

                for (int chunkX = 0; chunkX < chunkCountX; chunkX++)
                {
                    int minCellX = kernel.MinCellX + chunkX * TerrainSoftwareChunkCellsLikeOriginal;
                    int maxCellXExclusive = Mathf.Min(kernel.MaxCellXExclusive, minCellX + TerrainSoftwareChunkCellsLikeOriginal);
                    if (maxCellXExclusive <= minCellX || maxCellYExclusive <= minCellY)
                        continue;

                    jobs[jobCount++] = new TerrainSoftwareChunkJobLikeOriginal
                    {
                        ChunkX = chunkX,
                        ChunkY = chunkY,
                        Region = CreateTerrainSoftwareChunkRegionLikeOriginal(
                            map,
                            kernel,
                            minCellX,
                            maxCellXExclusive,
                            minCellY,
                            maxCellYExclusive)
                    };
                }
            }

            string mapSideCachePathV1 = GetTerrainSoftwareMapSideChunkCachePathV1LikeOriginal(map);
            string mapSideCacheKeyV1 = BuildTerrainSoftwarePersistentChunkCacheKeyLikeOriginal(map, kernel);
            bool loadedAllFromMapSideCacheV1 = TryLoadTerrainSoftwareMapSideChunkCacheV1LikeOriginal(
                mapSideCachePathV1,
                mapSideCacheKeyV1,
                jobs,
                jobCount,
                out string mapSideCacheAuditV1);

            UnityEngine.Debug.Log(
                $"[C2:REN][BASE WEIGHTED COMPOSITE V3] enabled={TerrainSoftwareBaseWeightedCompositeV3LikeAdapted} postBlur={TerrainSoftwareBaseSoftBlendEnabledLikeAdapted} " +
                $"mode=per-pixel-weighted-source-composite overlayStrength={TerrainSoftwareBaseWeightedCompositeOverlayStrengthV3LikeAdapted} minAlpha={TerrainSoftwareBaseWeightedCompositeMinAlphaV3LikeAdapted}. " +
                $"Old post-blur approach is disabled; base/overlay texture candidates are mixed before write.");

            UnityEngine.Debug.Log(
                $"[C2:REN] kernel=BuildStrictOldSurfaceSoftwareBakedChunksLikeOriginal mode=MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES " +
                $"rect=({kernel.MinCellX},{kernel.MinCellY})->({kernel.MaxCellXExclusive},{kernel.MaxCellYExclusive}) " +
                $"chunkCells={TerrainSoftwareChunkCellsLikeOriginal} pxPerCell={TerrainSoftwarePixelsPerCellLikeOriginal} " +
                $"jobs={jobCount} workers={Mathf.Max(1, Environment.ProcessorCount - 1)} " +
                $"rules='same middle pixels[] raster blend; no PNG cache; parallel chunk pixel buffers; main-thread Texture2D only'");

            UnityEngine.Debug.Log(
                $"[C2:REN][BASE TILE SOFT BLEND V2] enabled={TerrainSoftwareBaseSoftBlendEnabledLikeAdapted} " +
                $"radius={TerrainSoftwareBaseTileSoftBlendRadiusLikeAdapted} passes={TerrainSoftwareBaseTileSoftBlendPassesLikeAdapted} " +
                $"strength={TerrainSoftwareBaseTileSoftBlendStrengthLikeAdapted:0.00} overlayAlphaClip={TerrainSoftwareBaseOverlayAlphaClipLikeAdapted:0.000}. " +
                "This pass is for BASE/UNDERLAY texture boundaries only; HQ/facture layer remains disabled.");

            int workerCount = Mathf.Max(1, Environment.ProcessorCount - 1);
            var options = new ParallelOptions { MaxDegreeOfParallelism = workerCount };

            var parallelSwV11 = global::System.Diagnostics.Stopwatch.StartNew();
            if (!loadedAllFromMapSideCacheV1)
            {
                try
                {
                    Parallel.For(0, jobCount, options, i =>
                    {
                        TerrainSoftwareChunkJobLikeOriginal job = jobs[i];
                        try
                        {
                            job.Pixels = BakeTerrainChunkPixelsSoftwareLikeOriginal(map, kernel, job.Region, inputs);
                            job.Success = job.Pixels != null && job.Pixels.Length == job.Region.WidthPixels * job.Region.HeightPixels;
                        }
                        catch (Exception ex)
                        {
                            job.Success = false;
                            job.Error = ex.GetType().Name + ": " + ex.Message;
                        }

                        jobs[i] = job;
                    });
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[C2:REN][MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES] parallel bake failed, continuing with completed jobs where possible: " + ex.Message);
                }

                if (TrySaveTerrainSoftwareMapSideChunkCacheV1LikeOriginal(mapSideCachePathV1, mapSideCacheKeyV1, jobs, jobCount, out string saveAuditV1))
                    mapSideCacheAuditV1 = string.IsNullOrEmpty(mapSideCacheAuditV1) ? saveAuditV1 : (mapSideCacheAuditV1 + "; " + saveAuditV1);
                else if (!string.IsNullOrEmpty(saveAuditV1))
                    mapSideCacheAuditV1 = string.IsNullOrEmpty(mapSideCacheAuditV1) ? saveAuditV1 : (mapSideCacheAuditV1 + "; " + saveAuditV1);
            }
            parallelSwV11.Stop();

            var uploadSwV11 = global::System.Diagnostics.Stopwatch.StartNew();

            terrainBounds = new Bounds(Vector3.zero, Vector3.one);
            bool hasBounds = false;
            int builtChunkCount = 0;
            int failedChunkCount = 0;

            for (int i = 0; i < jobCount; i++)
            {
                TerrainSoftwareChunkJobLikeOriginal job = jobs[i];
                if (!job.Success || job.Pixels == null)
                {
                    failedChunkCount++;
                    if (!string.IsNullOrEmpty(job.Error))
                        UnityEngine.Debug.LogWarning($"[C2:REN][MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES] chunk=({job.ChunkX},{job.ChunkY}) failed: {job.Error}");
                    continue;
                }

                Texture2D chunkTexture = CreateTerrainSoftwareChunkTextureFromPixelsLikeOriginal(job.Region, job.Pixels, job.ChunkX, job.ChunkY);
                if (chunkTexture == null)
                {
                    failedChunkCount++;
                    continue;
                }

                Mesh chunkMesh = BuildProjectedChunkMeshSoftwareLikeOriginal(map, kernel, job.Region, out Bounds chunkBounds);
                if (chunkMesh == null || chunkMesh.vertexCount == 0)
                {
                    SafeDestroy(chunkTexture);
                    failedChunkCount++;
                    continue;
                }

                Material chunkMaterial = CreateSoftwareBakedTerrainChunkMaterialLikeOriginal(chunkTexture, job.ChunkX, job.ChunkY);
                if (chunkMaterial == null)
                {
                    SafeDestroy(chunkMesh);
                    SafeDestroy(chunkTexture);
                    failedChunkCount++;
                    continue;
                }

                var chunkGo = new GameObject($"TerrainChunkSoftware_{job.ChunkX:00}_{job.ChunkY:00}");
                chunkGo.transform.SetParent(parent, false);
                var mf = chunkGo.AddComponent<MeshFilter>();
                var mr = chunkGo.AddComponent<MeshRenderer>();
                mf.sharedMesh = chunkMesh;
                mr.sharedMaterial = chunkMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (!hasBounds)
                {
                    terrainBounds = chunkBounds;
                    hasBounds = true;
                }
                else
                {
                    terrainBounds.Encapsulate(chunkBounds.min);
                    terrainBounds.Encapsulate(chunkBounds.max);
                }

                builtChunkCount++;
            }

            if (!hasBounds)
                terrainBounds = new Bounds(Vector3.zero, Vector3.one);

            if (TerrainSoftwareTerrainShadowOverlayV5LikeAdapted)
                TryBuildTerrainShadowOverlayV5LikeAdapted(map, kernel, parent, ref terrainBounds, ref hasBounds);

            uploadSwV11.Stop();
            totalSwV11.Stop();

            UnityEngine.Debug.Log(
                $"[C2:REN] software baked chunks built={builtChunkCount}/{jobCount} failed={failedChunkCount} " +
                $"path=MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES cache={(loadedAllFromMapSideCacheV1 ? "map-side-hit" : "map-side-bake-write")} cacheAudit='{mapSideCacheAuditV1}' png=disabled gapfill=queue raster=scalar upload=SetPixelData textureFilter=trilinear_mip_aniso16_bias-0.75f terrainShadowOverlay=V4_original_cast_only_global finalColorPolish=V4_GPU_SHADER textureSourceAudit=files_and_functions " +
                $"timingMs prewarm={prewarmSwV11.ElapsedMilliseconds} parallelPixels={parallelSwV11.ElapsedMilliseconds} uploadMeshTexture={uploadSwV11.ElapsedMilliseconds} total={totalSwV11.ElapsedMilliseconds}");
        }

        private static void TryBuildTerrainShadowOverlayV5LikeAdapted(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            Transform parent,
            ref Bounds terrainBounds,
            ref bool hasBounds)
        {
            if (!TerrainSoftwareTerrainShadowOverlayV5LikeAdapted || map == null || parent == null)
                return;
            if (map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 1 || map.MaxTH <= 1)
                return;

            byte[] shadowAlpha = BuildTerrainShadowOverlayAlphaV5LikeAdapted(map, out int shadowWidth, out int shadowHeight);
            if (shadowAlpha == null || shadowAlpha.Length == 0 || shadowWidth <= 1 || shadowHeight <= 1)
                return;

            Texture2D shadowTexture = CreateTerrainShadowOverlayTextureV5LikeAdapted(shadowAlpha, shadowWidth, shadowHeight);
            if (shadowTexture == null)
                return;

            Mesh shadowMesh = BuildTerrainShadowOverlayMeshV5LikeAdapted(map, kernel, out Bounds shadowBounds);
            if (shadowMesh == null || shadowMesh.vertexCount == 0)
            {
                SafeDestroy(shadowTexture);
                return;
            }

            Material shadowMaterial = CreateTerrainShadowOverlayMaterialV5LikeAdapted(shadowTexture);
            if (shadowMaterial == null)
            {
                SafeDestroy(shadowMesh);
                SafeDestroy(shadowTexture);
                return;
            }

            var go = new GameObject("TerrainShadowOverlay_OriginalCastOnly_V4");
            go.transform.SetParent(parent, false);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = shadowMesh;
            mr.sharedMaterial = shadowMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            if (!hasBounds)
            {
                terrainBounds = shadowBounds;
                hasBounds = true;
            }
            else
            {
                terrainBounds.Encapsulate(shadowBounds.min);
                terrainBounds.Encapsulate(shadowBounds.max);
            }

            if (!s_terrainSoftwareTerrainShadowOverlayLoggedV5LikeAdapted)
            {
                s_terrainSoftwareTerrainShadowOverlayLoggedV5LikeAdapted = true;
                UnityEngine.Debug.Log(
                    $"[C2:ORIGINAL CAST SHADOW V4] built one global cast-only shadow layer from original ScanLightOffset/CreateLightMap path; no per-chunk baked shadow, no facture vertex lighting. " +
                    $"tex={shadowWidth}x{shadowHeight} maxAlpha={TerrainSoftwareTerrainShadowOverlayMaxAlphaV5LikeAdapted} " +
                    $"heightScale={TerrainSoftwareTerrainShadowOverlayHeightScaleV5LikeAdapted} decay={TerrainSoftwareTerrainShadowOverlayDecayV5LikeAdapted} " +
                    $"spreadR={TerrainSoftwareTerrainShadowOverlaySpreadRadiusV5LikeAdapted} startDepth={TerrainSoftwareCastShadowStartDepthV4LikeAdapted} fullDepth={TerrainSoftwareCastShadowFullDepthV4LikeAdapted}");
            }
        }

        private static byte[] BuildTerrainShadowOverlayAlphaV5LikeAdapted(ParsedMap map, out int width, out int height)
        {
            width = map != null ? map.VertInLine : 0;
            height = map != null ? map.MaxTH : 0;
            if (map == null || map.Heights == null || width <= 1 || height <= 1 || map.Heights.Length < width * height)
                return null;

            var lightMap = new byte[width * height];
            for (int i = 0; i < lightMap.Length; i++)
                lightMap[i] = 255;

            for (int y = height - 1; y > 0; y--)
                ScanTerrainShadowOverlayLightOffsetV5LikeAdapted(map, lightMap, width, height, width - 1, y);

            for (int x = 0; x < width - 1; x++)
                ScanTerrainShadowOverlayLightOffsetV5LikeAdapted(map, lightMap, width, height, x, height - 1);

            var smooth = new byte[lightMap.Length];
            for (int i = 0; i < smooth.Length; i++)
                smooth[i] = 255;

            // Original CreateLightMap-style 7-neighbour blur on staggered terrain grid.
            for (int iy = 0; iy < height; iy++)
            {
                int row = iy * width;
                for (int ix = 0; ix < width; ix++)
                {
                    int ofs = row + ix;
                    if (ix > 0 && iy > 0 && ix < width - 3 && iy < height - 3)
                    {
                        if ((ix & 1) != 0)
                        {
                            smooth[ofs] = (byte)(
                                ((int)lightMap[ofs + width] +
                                 (int)lightMap[ofs] +
                                 (int)lightMap[ofs - 1] +
                                 (int)lightMap[ofs + 1] +
                                 (int)lightMap[ofs - width - 1] +
                                 (int)lightMap[ofs - width] +
                                 (int)lightMap[ofs - width + 1]) / 7);
                        }
                        else
                        {
                            smooth[ofs] = (byte)(
                                ((int)lightMap[ofs - width] +
                                 (int)lightMap[ofs] +
                                 (int)lightMap[ofs - 1] +
                                 (int)lightMap[ofs + 1] +
                                 (int)lightMap[ofs + width - 1] +
                                 (int)lightMap[ofs + width] +
                                 (int)lightMap[ofs + width + 1]) / 7);
                        }
                    }
                }
            }

            byte[] spread = SpreadTerrainShadowAlphaV5LikeAdapted(smooth, width, height, TerrainSoftwareTerrainShadowOverlaySpreadRadiusV5LikeAdapted);
            var alpha = new byte[spread.Length];

            float startDepth = Mathf.Max(0.0f, TerrainSoftwareCastShadowStartDepthV4LikeAdapted);
            float fullDepth = Mathf.Max(startDepth + 1.0f, TerrainSoftwareCastShadowFullDepthV4LikeAdapted);
            float invDepthRange = 1.0f / Mathf.Max(1.0f, fullDepth - startDepth);

            for (int i = 0; i < alpha.Length; i++)
            {
                // V4: do not display raw LightMap. Extract only strong cast-shadow depth.
                // This removes most valley/pit darkening that made V3/V5 shadow appear "everywhere".
                float depth = 255.0f - spread[i];
                float shadowDepth = Clamp01FastLikeOriginal((depth - startDepth) * invDepthRange);
                shadowDepth = SmoothStep01LikeAdapted(shadowDepth);
                alpha[i] = ToByteRoundClampLikeOriginal(shadowDepth * TerrainSoftwareTerrainShadowOverlayMaxAlphaV5LikeAdapted * 255.0f);
            }

            if (TerrainSoftwareCastShadowPostBlurRadiusV4LikeAdapted > 0)
                alpha = BlurShadowAlphaV4LikeAdapted(alpha, width, height, TerrainSoftwareCastShadowPostBlurRadiusV4LikeAdapted);

            return alpha;
        }

        private static void ScanTerrainShadowOverlayLightOffsetV5LikeAdapted(
            ParsedMap map,
            byte[] lightMap,
            int width,
            int height,
            int x0,
            int y0)
        {
            int dd = Mathf.Max(1, TerrainSoftwareTerrainShadowOverlayDecayV5LikeAdapted);
            int hMax = 0;
            int ofs = x0 + y0 * width;
            int hp = 0;
            int h = 0;

            while (x0 >= 0 && y0 >= 0 && ofs >= 0 && ofs < lightMap.Length)
            {
                hp = h;
                h = Mathf.RoundToInt(GetTerrainShadowOverlayHeightV5LikeAdapted(map, ofs) * TerrainSoftwareTerrainShadowOverlayHeightScaleV5LikeAdapted);

                if (h > hMax)
                    hMax = h;

                int dh = hMax - h;
                if (dh > 0)
                {
                    dh *= 2 + Mathf.Abs(hp - h) / 2;
                    if (dh > 100)
                        dh = 100;

                    lightMap[ofs] = (byte)Mathf.Clamp(255 - dh, 0, 255);
                }
                else
                {
                    lightMap[ofs] = 255;
                }

                if ((x0 & 1) != 0)
                {
                    ofs -= width + 1;
                    y0--;
                }
                else
                {
                    ofs--;
                }

                x0--;
                hMax -= dd;
            }
        }

        private static int GetTerrainShadowOverlayHeightV5LikeAdapted(ParsedMap map, int vertexIndex)
        {
            if (map == null || map.Heights == null || vertexIndex < 0 || vertexIndex >= map.Heights.Length)
                return 0;

            return map.Heights[vertexIndex];
        }

        private static byte[] SpreadTerrainShadowAlphaV5LikeAdapted(byte[] lightMap, int width, int height, int radius)
        {
            if (lightMap == null || lightMap.Length == 0 || width <= 1 || height <= 1)
                return lightMap;

            int total = Mathf.Min(lightMap.Length, width * height);
            var result = new byte[total];

            radius = Mathf.Clamp(radius, 0, 8);
            if (radius <= 0)
            {
                Array.Copy(lightMap, result, total);
                return result;
            }

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int idx = row + x;
                    int minLight = lightMap[idx];

                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        int sy = y + oy;
                        if (sy < 0 || sy >= height)
                            continue;

                        int srow = sy * width;
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            int sx = x + ox;
                            if (sx < 0 || sx >= width)
                                continue;

                            int d2 = ox * ox + oy * oy;
                            if (d2 > radius * radius)
                                continue;

                            int sample = lightMap[srow + sx];
                            if (sample < minLight)
                                minLight = sample;
                        }
                    }

                    result[idx] = (byte)Mathf.Clamp(minLight, 0, 255);
                }
            }

            return result;
        }

        private static byte[] BlurShadowAlphaV4LikeAdapted(byte[] alpha, int width, int height, int radius)
        {
            if (alpha == null || alpha.Length == 0 || width <= 1 || height <= 1)
                return alpha;

            radius = Mathf.Clamp(radius, 0, 5);
            if (radius <= 0)
                return alpha;

            int total = Mathf.Min(alpha.Length, width * height);
            var temp = new byte[total];
            var result = new byte[total];

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int sum = 0;
                    int count = 0;

                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int sx = x + ox;
                        if (sx < 0 || sx >= width)
                            continue;

                        sum += alpha[row + sx];
                        count++;
                    }

                    temp[row + x] = (byte)Mathf.Clamp(count > 0 ? (sum / count) : alpha[row + x], 0, 255);
                }
            }

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int sum = 0;
                    int count = 0;

                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        int sy = y + oy;
                        if (sy < 0 || sy >= height)
                            continue;

                        sum += temp[sy * width + x];
                        count++;
                    }

                    result[row + x] = (byte)Mathf.Clamp(count > 0 ? (sum / count) : temp[row + x], 0, 255);
                }
            }

            return result;
        }

        private static Texture2D CreateTerrainShadowOverlayTextureV5LikeAdapted(byte[] alpha, int width, int height)
        {
            if (alpha == null || alpha.Length == 0 || width <= 1 || height <= 1)
                return null;

            var pixels = new Color32[width * height];
            int total = Mathf.Min(pixels.Length, alpha.Length);
            for (int i = 0; i < total; i++)
                pixels[i] = new Color32(0, 0, 0, alpha[i]);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "TerrainShadowOverlay_OriginalCastOnly_V4",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1
            };

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static Mesh BuildTerrainShadowOverlayMeshV5LikeAdapted(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            out Bounds shadowBounds)
        {
            shadowBounds = new Bounds(Vector3.zero, Vector3.one);
            if (map == null || map.VertInLine <= 1 || map.MaxTH <= 1)
                return null;

            int minCellX = Mathf.Clamp(kernel.MinCellX, 0, map.VertInLine - 2);
            int maxCellXExclusive = Mathf.Clamp(kernel.MaxCellXExclusive, minCellX + 1, map.VertInLine - 1);
            int minCellY = Mathf.Clamp(kernel.MinCellY, 0, map.MaxTH - 2);
            int maxCellYExclusive = Mathf.Clamp(kernel.MaxCellYExclusive, minCellY + 1, map.MaxTH - 1);

            int vertexCountX = maxCellXExclusive - minCellX + 1;
            int vertexCountY = maxCellYExclusive - minCellY + 1;
            if (vertexCountX <= 1 || vertexCountY <= 1)
                return null;

            var vertices = new List<Vector3>(vertexCountX * vertexCountY);
            var uvs = new List<Vector2>(vertexCountX * vertexCountY);
            var triangles = new List<int>((vertexCountX - 1) * (vertexCountY - 1) * 6);

            bool hasBounds = false;
            for (int y = minCellY; y <= maxCellYExclusive; y++)
            {
                for (int x = minCellX; x <= maxCellXExclusive; x++)
                {
                    int vertexIndex = y * map.VertInLine + x;
                    float rawX = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, x);
                    float rawZ = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, x, y);
                    Vector3 world = CreateKernelWorldVertexLikeOriginal(map, kernel, vertexIndex, rawX, rawZ);
                    world.y += TerrainSoftwareTerrainShadowOverlayYOffsetV5LikeAdapted;

                    vertices.Add(world);
                    uvs.Add(new Vector2(
                        Mathf.Clamp01(x / Mathf.Max(1.0f, map.VertInLine - 1.0f)),
                        Mathf.Clamp01(y / Mathf.Max(1.0f, map.MaxTH - 1.0f))));

                    if (!hasBounds)
                    {
                        shadowBounds = new Bounds(world, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        shadowBounds.Encapsulate(world);
                    }
                }
            }

            int RowLocal(int y) => (y - minCellY) * vertexCountX;

            for (int cellY = minCellY; cellY < maxCellYExclusive; cellY++)
            {
                for (int cellX = minCellX; cellX < maxCellXExclusive; cellX++)
                {
                    OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);
                    int row0 = RowLocal(cellY);
                    int row1 = RowLocal(cellY + 1);
                    int lx = cellX - minCellX;

                    int i0 = row0 + lx;
                    int i1 = row0 + lx + 1;
                    int i2 = row1 + lx;
                    int i3 = row1 + lx + 1;

                    if (cell.FirstC == cell.V2)
                    {
                        triangles.Add(i0);
                        triangles.Add(i1);
                        triangles.Add(i2);
                        triangles.Add(i2);
                        triangles.Add(i1);
                        triangles.Add(i3);
                    }
                    else
                    {
                        triangles.Add(i0);
                        triangles.Add(i1);
                        triangles.Add(i3);
                        triangles.Add(i0);
                        triangles.Add(i3);
                        triangles.Add(i2);
                    }
                }
            }

            if (vertices.Count == 0 || triangles.Count == 0)
                return null;

            var mesh = new Mesh { name = "TerrainShadowOverlayMesh_OriginalCastOnly_V4" };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            shadowBounds = mesh.bounds;
            return mesh;
        }

        private static Material CreateTerrainShadowOverlayMaterialV5LikeAdapted(Texture2D shadowTexture)
        {
            if (shadowTexture == null)
                return null;

            Shader shader = Shader.Find("Unlit/Transparent")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (shader == null)
                return null;

            var mat = new Material(shader)
            {
                name = "C2_TerrainShadowOverlay_OriginalCastOnly_V4",
                renderQueue = SurfaceBaseRenderQueueLikeAdapted + 20
            };

            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", shadowTexture);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", shadowTexture);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            if (mat.HasProperty("_ZWrite"))
                mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest"))
                mat.SetInt("_ZTest", (int)CompareFunction.Always);

            return mat;
        }

        private static void LogProtectedGroundIdAuditLikeAdapted(ParsedMap map)
        {
            if (map == null)
                return;

            int[] ids = { 3, 7, 9, 10, 20, 44, 55 };
            string texMapCounts = BuildGroundIdCountReportLikeAdapted(map.TexMap, ids);
            string texMapExCounts = BuildGroundIdCountReportLikeAdapted(map.TexMapEx, ids);

            UnityEngine.Debug.Log(
                "[C2:GROUND-ID AUDIT] protectedIds=3,7,9,10,20,44,55 " +
                "TexMap{" + texMapCounts + "} " +
                "TexMapEx{" + texMapExCounts + "} " +
                $"hasTiles={map.HasTilesChunk} hasTilesEx={map.HasTilesExChunk}");
        }

        private static string BuildGroundIdCountReportLikeAdapted(byte[] table, int[] ids)
        {
            if (table == null || table.Length == 0 || ids == null || ids.Length == 0)
                return "missing";

            int[] counts = new int[ids.Length];
            int other = 0;
            for (int i = 0; i < table.Length; i++)
            {
                int value = table[i] & 63;
                bool matched = false;
                for (int j = 0; j < ids.Length; j++)
                {
                    if (value == ids[j])
                    {
                        counts[j]++;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    other++;
            }

            var sb = new System.Text.StringBuilder(128);
            for (int i = 0; i < ids.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                sb.Append(ids[i]).Append('=').Append(counts[i]);
            }
            sb.Append(" other=").Append(other);
            return sb.ToString();
        }

        private void WriteTextureSourceAuditFilesLikeAdapted(ParsedMap map, TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            if (map == null)
                return;

            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string dir = Path.Combine(root, "C2TextureSourceAudit");
                Directory.CreateDirectory(dir);

                string modeA = Path.Combine(dir, "C2_TextureSource_ModeA_BMP_List.txt");
                string modeB = Path.Combine(dir, "C2_TextureSource_ModeB_Functions.txt");

                TerrainTextureResourcesLikeOriginal resources = TryLoadTerrainSurfaceResourcesLikeOriginal();
                TerrainTextureTablesLikeOriginal tables = GetTerrainTextureTablesLikeOriginal();
                FactureMaterialTablesLikeAdapted factureTables = GetFactureMaterialTablesLikeAdapted();

                WriteTextureSourceBmpListLikeAdapted(modeA, map, inputs, resources, tables, factureTables);
                WriteTextureSourceFunctionListLikeAdapted(modeB);

                UnityEngine.Debug.Log("[C2:TEXTURE SOURCE AUDIT] files written: " + dir);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[C2:TEXTURE SOURCE AUDIT] failed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static string GetTerrainSoftwareMapSideChunkCachePathV1LikeOriginal(ParsedMap map)
        {
            if (!TerrainSoftwareMapSideChunkCacheV1LikeOriginal || map == null || string.IsNullOrWhiteSpace(map.SourcePath))
                return string.Empty;

            try
            {
                string sourcePath = map.SourcePath;
                if (!Path.IsPathRooted(sourcePath))
                {
                    string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    sourcePath = Path.GetFullPath(Path.Combine(root, sourcePath));
                }
                else
                {
                    sourcePath = Path.GetFullPath(sourcePath);
                }

                string directory = Path.GetDirectoryName(sourcePath);
                string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                    return string.Empty;

                return Path.Combine(directory, fileName + ".кеш");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryLoadTerrainSoftwareMapSideChunkCacheV1LikeOriginal(
            string path,
            string key,
            TerrainSoftwareChunkJobLikeOriginal[] jobs,
            int jobCount,
            out string audit)
        {
            audit = "disabled";
            if (!TerrainSoftwareMapSideChunkCacheV1LikeOriginal)
                return false;
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(key) || jobs == null || jobCount <= 0)
            {
                audit = "missing_path_or_key";
                return false;
            }
            if (!File.Exists(path))
            {
                audit = "miss";
                return false;
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    uint magic = br.ReadUInt32();
                    int version = br.ReadInt32();
                    string storedKey = br.ReadString();
                    int storedJobCount = br.ReadInt32();
                    if (magic != TerrainSoftwareMapSideChunkCacheMagicV1LikeOriginal ||
                        version != TerrainSoftwareMapSideChunkCacheVersionV1LikeOriginal ||
                        !string.Equals(storedKey, key, StringComparison.Ordinal) ||
                        storedJobCount != jobCount)
                    {
                        audit = $"stale magic=0x{magic:X8} version={version} count={storedJobCount}";
                        return false;
                    }

                    for (int i = 0; i < jobCount; i++)
                    {
                        TerrainSoftwareChunkJobLikeOriginal job = jobs[i];
                        int chunkX = br.ReadInt32();
                        int chunkY = br.ReadInt32();
                        int minX = br.ReadInt32();
                        int maxX = br.ReadInt32();
                        int minY = br.ReadInt32();
                        int maxY = br.ReadInt32();
                        int width = br.ReadInt32();
                        int height = br.ReadInt32();
                        int byteCount = br.ReadInt32();

                        int expectedBytes = width * height * 4;
                        if (chunkX != job.ChunkX ||
                            chunkY != job.ChunkY ||
                            minX != job.Region.MinCellX ||
                            maxX != job.Region.MaxCellXExclusive ||
                            minY != job.Region.MinCellY ||
                            maxY != job.Region.MaxCellYExclusive ||
                            width != job.Region.WidthPixels ||
                            height != job.Region.HeightPixels ||
                            byteCount != expectedBytes ||
                            byteCount <= 0)
                        {
                            audit = $"stale_chunk index={i}";
                            return false;
                        }

                        byte[] raw = br.ReadBytes(byteCount);
                        if (raw.Length != byteCount)
                        {
                            audit = $"truncated index={i}";
                            return false;
                        }

                        job.Pixels = BytesToColor32ArrayV1LikeOriginal(raw);
                        job.Success = job.Pixels != null && job.Pixels.Length == width * height;
                        job.Error = null;
                        if (!job.Success)
                        {
                            audit = $"decode_failed index={i}";
                            return false;
                        }

                        jobs[i] = job;
                    }
                }

                audit = "hit " + path;
                return true;
            }
            catch (Exception ex)
            {
                audit = "load_failed " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static bool TrySaveTerrainSoftwareMapSideChunkCacheV1LikeOriginal(
            string path,
            string key,
            TerrainSoftwareChunkJobLikeOriginal[] jobs,
            int jobCount,
            out string audit)
        {
            audit = "disabled";
            if (!TerrainSoftwareMapSideChunkCacheV1LikeOriginal)
                return false;
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(key) || jobs == null || jobCount <= 0)
            {
                audit = "save_skipped_missing_path_or_key";
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string tmpPath = path + ".tmp";
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(TerrainSoftwareMapSideChunkCacheMagicV1LikeOriginal);
                    bw.Write(TerrainSoftwareMapSideChunkCacheVersionV1LikeOriginal);
                    bw.Write(key);
                    bw.Write(jobCount);

                    for (int i = 0; i < jobCount; i++)
                    {
                        TerrainSoftwareChunkJobLikeOriginal job = jobs[i];
                        if (!job.Success || job.Pixels == null || job.Pixels.Length != job.Region.WidthPixels * job.Region.HeightPixels)
                        {
                            audit = $"save_skipped_bad_chunk index={i}";
                            return false;
                        }

                        byte[] raw = Color32ArrayToBytesV1LikeOriginal(job.Pixels);
                        bw.Write(job.ChunkX);
                        bw.Write(job.ChunkY);
                        bw.Write(job.Region.MinCellX);
                        bw.Write(job.Region.MaxCellXExclusive);
                        bw.Write(job.Region.MinCellY);
                        bw.Write(job.Region.MaxCellYExclusive);
                        bw.Write(job.Region.WidthPixels);
                        bw.Write(job.Region.HeightPixels);
                        bw.Write(raw.Length);
                        bw.Write(raw);
                    }
                }

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmpPath, path);
                audit = "saved " + path;
                return true;
            }
            catch (Exception ex)
            {
                audit = "save_failed " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static byte[] Color32ArrayToBytesV1LikeOriginal(Color32[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return Array.Empty<byte>();

            var raw = new byte[pixels.Length * 4];
            int o = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                raw[o++] = c.r;
                raw[o++] = c.g;
                raw[o++] = c.b;
                raw[o++] = c.a;
            }

            return raw;
        }

        private static Color32[] BytesToColor32ArrayV1LikeOriginal(byte[] raw)
        {
            if (raw == null || raw.Length == 0 || (raw.Length & 3) != 0)
                return null;

            var pixels = new Color32[raw.Length / 4];
            int o = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(raw[o], raw[o + 1], raw[o + 2], raw[o + 3]);
                o += 4;
            }

            return pixels;
        }

        private void WriteTextureSourceBmpListLikeAdapted(
            string path,
            ParsedMap map,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            TerrainTextureResourcesLikeOriginal resources,
            TerrainTextureTablesLikeOriginal tables,
            FactureMaterialTablesLikeAdapted factureTables)
        {
            var sb = new global::System.Text.StringBuilder(64 * 1024);
            sb.AppendLine("C2 Texture Source Audit Mode A - BMP/TGA list");
            sb.AppendLine("version=V41_TEXTURE_SOURCE_AUDIT");
            sb.AppendLine("goal=list exact texture ids used by current M3D and map them to files/functions");
            sb.AppendLine("hasTiles=" + map.HasTilesChunk + " hasTilesEx=" + map.HasTilesExChunk + " hasFactures=" + map.HasFactureMapChunk);
            sb.AppendLine("GroundAtlasPath=" + (resources != null ? resources.GroundAtlasPath : "<missing>"));
            sb.AppendLine("CrossTexPath=" + (resources != null ? resources.CrossTexPath : "<missing>"));
            sb.AppendLine("GroundAtlasSize=" + (inputs != null ? inputs.GroundWidth + "x" + inputs.GroundHeight : "<missing>"));
            sb.AppendLine("FactureSourceKind=" + (factureTables != null ? factureTables.SourceKind : "<missing>"));
            sb.AppendLine("FactureSourceXmlPath=" + (factureTables != null ? factureTables.SourceXmlPath : "<missing>"));
            sb.AppendLine("FactureTexturesXmlPath=" + (factureTables != null ? factureTables.SourceTexturesXmlPath : "<missing>"));
            sb.AppendLine();

            int[] texMap = CountTileIdsLikeAdapted(map.TexMap, false, null);
            int[] texMapEx = CountTileIdsLikeAdapted(map.TexMapEx, false, null);
            int[] texMapExWeighted = CountTileIdsLikeAdapted(map.TexMapEx, true, map.WTexMapEx);
            int[] factureAll = CountTileIdsLikeAdapted(map.FactureMap, false, null);
            int[] factureWeighted = CountTileIdsLikeAdapted(map.FactureMap, true, map.FactureWeight);

            sb.AppendLine("GROUND IDS 0..63");
            sb.AppendLine("Format: id fileCandidate exists atlasCell texMap texMapEx texMapExWeighted roadTex extTex[0..3] flags media diffuseColor");
            for (int id = 0; id < 64; id++)
            {
                string candidate = BuildGroundTextureCandidateListLikeAdapted(id, out bool existsAny);
                byte road = tables != null ? tables.RoadTex[id] : (byte)id;
                byte e0 = tables != null ? tables.ExtTex[id, 0] : (byte)id;
                byte e1 = tables != null ? tables.ExtTex[id, 1] : (byte)id;
                byte e2 = tables != null ? tables.ExtTex[id, 2] : (byte)id;
                byte e3 = tables != null ? tables.ExtTex[id, 3] : (byte)id;
                ushort flags = tables != null ? tables.TexFlags[id] : (ushort)0;
                byte media = tables != null ? tables.TexMedia[id] : (byte)0;
                Color32 diffuse = tables != null ? tables.TexDiffuse[id] : new Color32(0, 0, 0, 255);
                int atlasX = id & 7;
                int atlasY = id / 8;
                sb.Append("id=").Append(id)
                    .Append(" candidate=").Append(candidate)
                    .Append(" exists=").Append(existsAny ? 1 : 0)
                    .Append(" atlasCell=").Append(atlasX).Append(',').Append(atlasY)
                    .Append(" TexMap=").Append(texMap[id])
                    .Append(" TexMapEx=").Append(texMapEx[id])
                    .Append(" TexMapExWeighted=").Append(texMapExWeighted[id])
                    .Append(" RoadTex=").Append(road)
                    .Append(" ExtTex=").Append(e0).Append(',').Append(e1).Append(',').Append(e2).Append(',').Append(e3)
                    .Append(" flags=0x").Append(flags.ToString("X4"))
                    .Append(" media=").Append(media)
                    .Append(" diffuse=").Append(diffuse.r).Append(',').Append(diffuse.g).Append(',').Append(diffuse.b)
                    .AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("FACTURE IDS / BUCKETS 0..255");
            sb.AppendLine("Format: id FactureMap FactureMapWeighted diffuse bump usage useBump scale shift");
            for (int id = 0; id < 256; id++)
            {
                int count = factureAll[id];
                int weighted = factureWeighted[id];
                string diffuse = factureTables != null ? factureTables.DiffuseTexturePath[id] : string.Empty;
                string bump = factureTables != null ? factureTables.BumpTexturePath[id] : string.Empty;
                bool hasAny = count > 0 || weighted > 0 || !string.IsNullOrEmpty(diffuse) || !string.IsNullOrEmpty(bump);
                if (!hasAny)
                    continue;

                sb.Append("id=").Append(id)
                    .Append(" FactureMap=").Append(count)
                    .Append(" FactureMapWeighted=").Append(weighted)
                    .Append(" diffuse=").Append(string.IsNullOrEmpty(diffuse) ? "<empty>" : diffuse)
                    .Append(" bump=").Append(string.IsNullOrEmpty(bump) ? "<empty>" : bump)
                    .Append(" usage=").Append(factureTables != null ? factureTables.Usage[id].ToString() : "?")
                    .Append(" useBump=").Append(factureTables != null && factureTables.UseBump[id] ? 1 : 0)
                    .Append(" scale=").Append(factureTables != null ? factureTables.UScale[id].ToString("0.###") : "?")
                    .Append(',').Append(factureTables != null ? factureTables.VScale[id].ToString("0.###") : "?")
                    .Append(" shift=").Append(factureTables != null ? factureTables.UShift[id].ToString("0.###") : "?")
                    .Append(',').Append(factureTables != null ? factureTables.VShift[id].ToString("0.###") : "?")
                    .AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("INTERPRETATION NOTES");
            sb.AppendLine("Ground tile ids are not separate BMP loads during runtime software bake: the actual sampled texture is GroundTex.bmp atlas.");
            sb.AppendLine("Separate Assets/Resources/textures/Ground/texNN.bmp files are listed only as candidates/helpers; the live path samples atlas cell id=(x=id&7,y=id/8).");
            sb.AppendLine("If a bridge-looking pattern is missing while TexMap/TexMapEx/write counts exist, compare GroundTex.bmp atlas cell pixels and the polygon shader path, not only texNN.bmp files.");

            File.WriteAllText(path, sb.ToString());
        }

        private void WriteTextureSourceFunctionListLikeAdapted(string path)
        {
            var sb = new global::System.Text.StringBuilder(32 * 1024);
            sb.AppendLine("C2 Texture Source Audit Mode B - functions affecting BMP/TGA path");
            sb.AppendLine("version=V41_TEXTURE_SOURCE_AUDIT");
            sb.AppendLine();
            sb.AppendLine("GROUND / BMP / ATLAS CHAIN");
            sb.AppendLine("1. TryLoadTerrainSurfaceResourcesLikeOriginal -> loads Textures/GroundTex.bmp and BoundNew128.tga.");
            sb.AppendLine("2. TryLoadTextureTablesFromListLikeOriginal -> parses textures.lst: #CROSS, #CROSSX, #COLOR, #MULTI, #ROAD, flags/media.");
            sb.AppendLine("3. ParsedMap.TexMap -> base Ground tile IDs from M3D.");
            sb.AppendLine("4. ParsedMap.TexMapEx + WTexMapEx -> overlay Ground tile IDs and weights from M3D.");
            sb.AppendLine("5. TryBuildCellStageLikeOriginal(BASE/OVERLAY) -> builds T0/T1/T2/T3/W0/W1/W2/W3.");
            sb.AppendLine("6. BakeCellStageSoftwareLikeOriginal -> emits 2 triangles per cell.");
            sb.AppendLine("7. BakeExpandedTriangleStageSoftwareLikeOriginal -> sorts tMin/tAve/tMax and creates Primary/Average/Maximum copies.");
            sb.AppendLine("8. BuildInitialExpandedTriangleCopyLikeOriginal / BuildAverageExpandedTriangleCopyLikeOriginal / BuildMaximumExpandedTriangleCopyLikeOriginal -> chooses seed, role, alpha.");
            sb.AppendLine("9. BuildTriangleDescriptorFromCopyLikeAdapted -> descriptor.ResolvedTile.");
            sb.AppendLine("10. RasterizeTriangleDescriptorSoftwareLikeOriginal -> builds GroundAtlas UV + cross UV.");
            sb.AppendLine("11. BuildBaseTriangleUvExplicitLikeOriginal -> exact atlas cell UV.");
            sb.AppendLine("12. BuildCrossTriangleUvForPairLikeOriginal -> BoundNew128.tga edge/cross mask.");
            sb.AppendLine("13. RasterizeTriangleSoftwareLikeOriginal -> samples GroundTex.bmp + BoundNew128.tga and blends into chunk pixels.");
            sb.AppendLine("14. CloseBaseCoverageGapsLikeOriginal -> fills uncovered pixels from neighbors only after base.");
            sb.AppendLine("15. RasterizeFactureTriangleDescriptorSoftwareLikeOriginal -> later can modify already baked ground pixels.");
            sb.AppendLine("16. CreateTerrainSoftwareChunkTextureFromPixelsLikeOriginal -> chunk Texture2D upload/filter/mip settings.");
            sb.AppendLine();
            sb.AppendLine("FACTURE / TGA/BMP CHAIN");
            sb.AppendLine("1. ParsedMap.FactureMap + FactureWeight -> facture ids/weights from M3D.");
            sb.AppendLine("2. GetFactureMaterialTablesLikeAdapted -> loads facture material table/xml/dat.");
            sb.AppendLine("3. GetFactureBucketTextureIdLikeAdapted -> bucket texture id = renderFactureId & 255.");
            sb.AppendLine("4. TryLoadFactureTextureLikeAdapted / GetOrCreateFactureBakeCacheEntryLikeOriginal -> loads diffuse/dot3/normal textures.");
            sb.AppendLine("5. BuildFactureTriangleCopiesLikeAdapted / TryBuildSoftwareFactureFallbackDescriptorLikeAdapted -> chooses real/fallback facture copies.");
            sb.AppendLine("6. RasterizeFactureTriangleSoftwareLikeOriginal -> blends facture texture over Ground result.");
            sb.AppendLine();
            sb.AppendLine("WHAT TO COMPARE NEXT");
            sb.AppendLine("A. Compare this file from Polygon and Base: GroundAtlasPath, facture paths, TexMap/TexMapEx counts, RoadTex/ExtTex, flags.");
            sb.AppendLine("B. If counts and paths match, the bridge difference is not 'which BMP is loaded'; it is formula/shader/raster/sampling/output filtering.");
            sb.AppendLine("C. If paths differ, replace only the relevant resource load/table path, not the drawing logic.");

            File.WriteAllText(path, sb.ToString());
        }

        private static int[] CountTileIdsLikeAdapted(byte[] table, bool requireWeight, byte[] weights)
        {
            int[] counts = new int[256];
            if (table == null)
                return counts;

            int n = table.Length;
            for (int i = 0; i < n; i++)
            {
                if (requireWeight)
                {
                    if (weights == null || i >= weights.Length || weights[i] == 0)
                        continue;
                }
                counts[table[i] & 255]++;
            }
            return counts;
        }

        private static string BuildGroundTextureCandidateListLikeAdapted(int id, out bool existsAny)
        {
            existsAny = false;
            string assets = Application.dataPath;
            string[] names =
            {
                "tex" + id + ".bmp",
                "TEX" + id + ".BMP",
                "tex" + id + ".BMP",
                "TEX" + id + ".bmp"
            };
            string baseDir = Path.Combine(assets, "Resources", "textures", "Ground");
            var sb = new global::System.Text.StringBuilder(128);
            for (int i = 0; i < names.Length; i++)
            {
                string full = Path.Combine(baseDir, names[i]);
                bool exists = File.Exists(full);
                if (exists)
                    existsAny = true;
                if (i > 0)
                    sb.Append('|');
                sb.Append("Assets/Resources/textures/Ground/").Append(names[i]).Append(exists ? "[exists]" : "[missing]");
            }
            return sb.ToString();
        }

        private void PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal(TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            if (inputs == null)
                return;

            lock (s_terrainSoftwareFactureCacheBuildLockLikeOriginal)
            {
                for (int bucketTextureId = 0; bucketTextureId < 256; bucketTextureId++)
                {
                    TerrainSoftwareFactureBakeCacheEntryLikeOriginal entry = GetOrCreateFactureBakeCacheEntryLikeOriginal(inputs, bucketTextureId);
                    inputs.FactureCacheArray[bucketTextureId] = entry;
                    inputs.FactureCacheInitialized[bucketTextureId] = true;
                }
            }

            UnityEngine.Debug.Log($"[C2:REN][MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES] facture texture cache prewarmed entries={inputs.FactureCache.Count} arrayReady=256.");
        }

        private Color32[] BakeTerrainChunkPixelsSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            var pixels = new Color32[region.WidthPixels * region.HeightPixels];
            if (!s_terrainSoftwareFallbackStructureFeatherPathLoggedV1LikeAdapted)
            {
                s_terrainSoftwareFallbackStructureFeatherPathLoggedV1LikeAdapted = true;
                UnityEngine.Debug.Log("[C2:HOLECLOSER ERODE V3] active: fallback structures use inward erosion + clustered dust alpha; no straight edge ribbons, no triangle-edge fade.");
            }
            var baseCoverage = new byte[pixels.Length];
            var tex44Protection = new byte[pixels.Length];
            var baseTileIds = new byte[pixels.Length];
            var fallbackStructurePixels = new Color32[pixels.Length];
            var fallbackStructureMask = new byte[pixels.Length];
            var fallbackStructureAlpha = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 255);
                baseTileIds[i] = 255;
                fallbackStructurePixels[i] = new Color32(0, 0, 0, 255);
                fallbackStructureMask[i] = 0;
                fallbackStructureAlpha[i] = 0;
            }

            List<FactureTriangleCopyDescriptorLikeAdapted> scratchFactureCopies =
                (!TerrainQualityFactureLayerDisabledLikeAdapted && HasFactureLayerDataLikeOriginal(map))
                    ? new List<FactureTriangleCopyDescriptorLikeAdapted>(4)
                    : null;

            for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
            {
                for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                {
                    BakeTerrainCellSoftwareLikeOriginal(map, kernel, region, inputs, pixels, baseCoverage, tex44Protection, cellX, cellY, baseTileIds);
                }
            }

            CloseBaseCoverageGapsLikeOriginal(pixels, baseCoverage, region.WidthPixels, region.HeightPixels);

            if (TerrainSoftwareBaseSoftBlendEnabledLikeAdapted)
                SoftenBaseTileTransitionsLikeAdapted(pixels, baseTileIds, region.WidthPixels, region.HeightPixels, TerrainSoftwareBaseTileSoftBlendRadiusLikeAdapted, TerrainSoftwareBaseTileSoftBlendPassesLikeAdapted, TerrainSoftwareBaseTileSoftBlendStrengthLikeAdapted);

            if (scratchFactureCopies != null)
            {
                for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
                {
                    for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                    {
                        BakeTerrainCellFactureSoftwareLikeOriginal(map, kernel, region, inputs, pixels, cellX, cellY, scratchFactureCopies, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);
                    }
                }
            }

            if (TerrainSoftwareFallbackStructureFeatherV1LikeAdapted)
                CompositeFallbackStructuresWithFeatherV1LikeAdapted(
                    pixels,
                    fallbackStructurePixels,
                    fallbackStructureMask,
                    fallbackStructureAlpha,
                    region.WidthPixels,
                    region.HeightPixels,
                    TerrainSoftwareFallbackStructureFeatherRadiusV1LikeAdapted);

            BleedChunkTextureEdgesLikeAdapted(pixels, region.WidthPixels, region.HeightPixels, 3);
            return pixels;
        }
        private static void ApplyFinalTerrainColorPolishBufferV1LikeAdapted(Color32[] pixels)
        {
            if (!TerrainSoftwareFinalColorPolishV1LikeAdapted || pixels == null || pixels.Length == 0)
                return;

            if (!s_terrainSoftwareFinalColorPolishLoggedV1LikeAdapted)
            {
                s_terrainSoftwareFinalColorPolishLoggedV1LikeAdapted = true;
                UnityEngine.Debug.Log(
                    $"[C2:FINAL COLOR POLISH V4 GPU] enabled. CPU-fast no-Mathf.Pow. warm=({TerrainSoftwareFinalColorPolishWarmR_V1LikeAdapted:F3},{TerrainSoftwareFinalColorPolishWarmG_V1LikeAdapted:F3},{TerrainSoftwareFinalColorPolishWarmB_V1LikeAdapted:F3}) " +
                    $"sat={TerrainSoftwareFinalColorPolishSaturationV1LikeAdapted:F3} contrast={TerrainSoftwareFinalColorPolishContrastV1LikeAdapted:F3}");
            }

            float warmR = TerrainSoftwareFinalColorPolishWarmR_V1LikeAdapted;
            float warmG = TerrainSoftwareFinalColorPolishWarmG_V1LikeAdapted;
            float warmB = TerrainSoftwareFinalColorPolishWarmB_V1LikeAdapted;
            float saturation = TerrainSoftwareFinalColorPolishSaturationV1LikeAdapted;
            float contrast = TerrainSoftwareFinalColorPolishContrastV1LikeAdapted;
            float shadowWarmR = TerrainSoftwareFinalColorPolishShadowWarmR_V1LikeAdapted;
            float shadowWarmG = TerrainSoftwareFinalColorPolishShadowWarmG_V1LikeAdapted;
            float shadowCoolB = TerrainSoftwareFinalColorPolishShadowCoolB_V1LikeAdapted;
            const float inv255 = 1.0f / 255.0f;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 src = pixels[i];

                float r = src.r * inv255;
                float g = src.g * inv255;
                float b = src.b * inv255;

                r *= warmR;
                g *= warmG;
                b *= warmB;

                float gray = (r + g + b) * 0.33333334f;
                r = gray + (r - gray) * saturation;
                g = gray + (g - gray) * saturation;
                b = gray + (b - gray) * saturation;

                r = (r - 0.5f) * contrast + 0.5f;
                g = (g - 0.5f) * contrast + 0.5f;
                b = (b - 0.5f) * contrast + 0.5f;

                r = Clamp01FastLikeOriginal(r);
                g = Clamp01FastLikeOriginal(g);
                b = Clamp01FastLikeOriginal(b);

                float luma = r * 0.299f + g * 0.587f + b * 0.114f;
                float darkness = Clamp01FastLikeOriginal((0.60f - luma) / 0.60f);

                r *= 1.0f + darkness * shadowWarmR;
                g *= 1.0f + darkness * shadowWarmG;
                b *= 1.0f - darkness * shadowCoolB;

                pixels[i] = new Color32(
                    ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(r) * 255.0f),
                    ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(g) * 255.0f),
                    ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(b) * 255.0f),
                    src.a);
            }
        }

        private static Texture2D CreateTerrainSoftwareChunkTextureFromPixelsLikeOriginal(
            TerrainSoftwareChunkRegionLikeOriginal region,
            Color32[] pixels,
            int chunkX,
            int chunkY)
        {
            if (pixels == null || pixels.Length != region.WidthPixels * region.HeightPixels)
                return null;

            var texture = new Texture2D(region.WidthPixels, region.HeightPixels, TextureFormat.RGBA32, true)
            {
                name = $"TerrainChunkSoftware_{chunkX:00}_{chunkY:00}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16,
                mipMapBias = -0.75f
            };

            // V4 GPU: final color polish is done by the chunk material shader, not by a CPU per-pixel loop.
            // This keeps V3 tuned look while removing a full extra pass over every baked pixel during loading.
            texture.SetPixelData(pixels, 0);
            texture.Apply(true, false);
            return texture;
        }


        private static Color32[] ReadTexturePixels32SafeLikeAdapted(Texture2D texture, out int width, out int height, string label)
        {
            width = texture != null ? texture.width : 0;
            height = texture != null ? texture.height : 0;
            if (texture == null || width <= 0 || height <= 0)
                return null;

            try
            {
                return texture.GetPixels32();
            }
            catch (Exception directEx)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture rt = null;
                Texture2D readable = null;
                try
                {
                    rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                    Graphics.Blit(texture, rt);
                    RenderTexture.active = rt;
                    readable = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                    {
                        name = (texture.name ?? label ?? "texture") + "_readable_copy"
                    };
                    readable.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    readable.Apply(false, false);
                    Color32[] pixels = readable.GetPixels32();
                    UnityEngine.Debug.Log($"[C2:V51B TEX44 READABLE FIX] texture '{label}' was not readable; copied through RenderTexture. reason={directEx.GetType().Name}: {directEx.Message}");
                    return pixels;
                }
                catch (Exception copyEx)
                {
                    UnityEngine.Debug.LogWarning($"[C2:V51B TEX44 READABLE FIX] readable-copy failed for texture '{label}': {copyEx.GetType().Name}: {copyEx.Message}");
                    return null;
                }
                finally
                {
                    RenderTexture.active = previous;
                    if (rt != null)
                        RenderTexture.ReleaseTemporary(rt);
                    if (readable != null)
                    {
#if UNITY_EDITOR
                        UnityEngine.Object.DestroyImmediate(readable);
#else
                        UnityEngine.Object.Destroy(readable);
#endif
                    }
                }
            }
        }


                private TerrainSoftwareBakeInputsLikeOriginal PrepareTerrainSoftwareBakeInputsLikeOriginal()
        {
            TerrainTextureResourcesLikeOriginal resources = TryLoadTerrainSurfaceResourcesLikeOriginal();
            if (resources == null || resources.GroundAtlas == null)
                return null;

            var result = new TerrainSoftwareBakeInputsLikeOriginal
            {
                GroundAtlas = resources.GroundAtlas,
                GroundPixels = resources.GroundAtlas.GetPixels32(),
                GroundWidth = resources.GroundAtlas.width,
                GroundHeight = resources.GroundAtlas.height,
                Tables = GetTerrainTextureTablesLikeOriginal()
            };

            if (resources.CrossTex != null)
            {
                result.CrossTex = resources.CrossTex;
                result.CrossPixels = resources.CrossTex.GetPixels32();
                result.CrossWidth = resources.CrossTex.width;
                result.CrossHeight = resources.CrossTex.height;
            }

            Texture2D standaloneTex44 = Resources.Load<Texture2D>("textures/Ground/tex44");
            if (standaloneTex44 != null)
            {
                result.StandaloneTex44 = standaloneTex44;
                result.StandaloneTex44Pixels = ReadTexturePixels32SafeLikeAdapted(standaloneTex44, out result.StandaloneTex44Width, out result.StandaloneTex44Height, "tex44");
                if (result.StandaloneTex44Pixels != null && result.StandaloneTex44Pixels.Length > 0)
                {
                    UnityEngine.Debug.Log($"[C2:V51B TEX44 READABLE FIX] standalone tex44 prepared via Resources path='textures/Ground/tex44' size={result.StandaloneTex44Width}x{result.StandaloneTex44Height} readableCopy={(standaloneTex44.isReadable ? 0 : 1)}");
                }
                else
                {
                    result.StandaloneTex44 = null;
                    result.StandaloneTex44Width = 0;
                    result.StandaloneTex44Height = 0;
                    UnityEngine.Debug.LogWarning("[C2:V51B TEX44 READABLE FIX] standalone tex44 pixels unavailable after readable-copy fallback; bake keeps atlas-only sampling.");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[C2:V51B TEX44 READABLE FIX] standalone tex44 not found at Resources/textures/Ground/tex44; bake keeps atlas-only sampling.");
            }

            return result;
        }


        private static void PrepareTerrainSoftwarePersistentChunkCacheLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            if (inputs == null)
                return;

            if (TerrainSoftwareFallbackStructureFeatherDisableCacheV1LikeAdapted)
            {
                inputs.PersistentChunkCacheEnabled = false;
                inputs.PersistentChunkCacheDirectory = string.Empty;
                inputs.PersistentChunkCacheKey = TerrainSoftwarePersistentCacheVersionLikeOriginal;
                if (!s_terrainSoftwareFallbackStructureFeatherCacheLoggedV1LikeAdapted)
                {
                    s_terrainSoftwareFallbackStructureFeatherCacheLoggedV1LikeAdapted = true;
                    UnityEngine.Debug.Log("[C2:HOLECLOSER ERODE V3] persistent chunk cache disabled; fallback structures are freshly baked.");
                }
                return;
            }

            string root = GetTerrainSoftwarePersistentChunkCacheRootLikeOriginal();
            string key = BuildTerrainSoftwarePersistentChunkCacheKeyLikeOriginal(map, kernel);
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(key))
            {
                inputs.PersistentChunkCacheEnabled = false;
                return;
            }

            inputs.PersistentChunkCacheKey = key;
            inputs.PersistentChunkCacheDirectory = Path.Combine(root, key);
            try
            {
                Directory.CreateDirectory(inputs.PersistentChunkCacheDirectory);
                inputs.PersistentChunkCacheEnabled = true;
                UnityEngine.Debug.Log($"[C2:REN][AR-1] software baked chunk cache enabled key={key} dir='{inputs.PersistentChunkCacheDirectory}'. First run bakes and writes PNG chunks; next run reuses them without changing terrain pixels.");
            }
            catch (Exception ex)
            {
                inputs.PersistentChunkCacheEnabled = false;
                LogTerrainSoftwarePersistentCacheWarningLikeOriginal("create cache dir failed: " + ex.Message);
            }
        }

        private static string GetTerrainSoftwarePersistentChunkCacheRootLikeOriginal()
        {
            try
            {
                string dataPath = Application.dataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    DirectoryInfo assetsDir = Directory.GetParent(dataPath);
                    if (assetsDir != null)
                        return Path.Combine(assetsDir.FullName, "Library", "C2TerrainSoftwareBakeCache");
                }

                if (!string.IsNullOrEmpty(Application.persistentDataPath))
                    return Path.Combine(Application.persistentDataPath, "C2TerrainSoftwareBakeCache");
                if (!string.IsNullOrEmpty(Application.temporaryCachePath))
                    return Path.Combine(Application.temporaryCachePath, "C2TerrainSoftwareBakeCache");
            }
            catch (Exception ex)
            {
                LogTerrainSoftwarePersistentCacheWarningLikeOriginal("resolve cache root failed: " + ex.Message);
            }

            return string.Empty;
        }

        private static string BuildTerrainSoftwarePersistentChunkCacheKeyLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel)
        {
            if (map == null)
                return string.Empty;

            ulong hash = 14695981039346656037UL;
            HashString64LikeOriginal(ref hash, TerrainSoftwarePersistentCacheVersionLikeOriginal);
            HashString64LikeOriginal(ref hash, map.SourcePath);
            HashInt64LikeOriginal(ref hash, map.VertInLine);
            HashInt64LikeOriginal(ref hash, map.MaxTH);
            HashInt64LikeOriginal(ref hash, map.MinMapX);
            HashInt64LikeOriginal(ref hash, map.MinMapY);
            HashInt64LikeOriginal(ref hash, map.MaxMapX);
            HashInt64LikeOriginal(ref hash, map.MaxMapY);
            HashInt64LikeOriginal(ref hash, kernel.MinCellX);
            HashInt64LikeOriginal(ref hash, kernel.MaxCellXExclusive);
            HashInt64LikeOriginal(ref hash, kernel.MinCellY);
            HashInt64LikeOriginal(ref hash, kernel.MaxCellYExclusive);
            HashFloat64LikeOriginal(ref hash, kernel.TQuantWorld);
            HashFloat64LikeOriginal(ref hash, kernel.HQuantWorld);
            HashFloat64LikeOriginal(ref hash, kernel.SQuantWorld);
            HashFloat64LikeOriginal(ref hash, kernel.BackingStepXWorld);
            HashFloat64LikeOriginal(ref hash, kernel.BackingStepZWorld);
            HashFloat64LikeOriginal(ref hash, kernel.BackingOddColumnOffsetZWorld);
            HashFloat64LikeOriginal(ref hash, kernel.HeightScale);
            HashFloat64LikeOriginal(ref hash, kernel.YShiftWorldScale);
            HashInt64LikeOriginal(ref hash, kernel.ScShift);
            HashInt64LikeOriginal(ref hash, TerrainSoftwareChunkCellsLikeOriginal);
            HashInt64LikeOriginal(ref hash, TerrainSoftwarePixelsPerCellLikeOriginal);
            HashFloat64LikeOriginal(ref hash, TerrainSoftwareAlphaClipLikeOriginal);
            HashFloat64LikeOriginal(ref hash, TerrainSoftwareRasterToleranceLikeOriginal);

            HashShortArray64LikeOriginal(ref hash, map.Heights);
            HashByteArray64LikeOriginal(ref hash, map.XYShift);
            HashByteArray64LikeOriginal(ref hash, map.TexMap);
            HashByteArray64LikeOriginal(ref hash, map.TexMapEx);
            HashByteArray64LikeOriginal(ref hash, map.WTexMapEx);
            HashByteArray64LikeOriginal(ref hash, map.FactureMap);
            HashByteArray64LikeOriginal(ref hash, map.FactureWeight);

            if (!string.IsNullOrEmpty(map.SourcePath) && File.Exists(map.SourcePath))
            {
                try
                {
                    FileInfo fi = new FileInfo(map.SourcePath);
                    HashLong64LikeOriginal(ref hash, fi.Length);
                    HashLong64LikeOriginal(ref hash, fi.LastWriteTimeUtc.Ticks);
                }
                catch
                {
                    // Parsed map data above is still enough to keep the cache tied to content.
                }
            }

            return hash.ToString("X16");
        }

        private static string GetTerrainSoftwarePersistentChunkCacheFilePathLikeOriginal(
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            TerrainSoftwareChunkRegionLikeOriginal region,
            int chunkX,
            int chunkY)
        {
            if (inputs == null || !inputs.PersistentChunkCacheEnabled || string.IsNullOrEmpty(inputs.PersistentChunkCacheDirectory))
                return string.Empty;

            string fileName =
                $"chunk_{chunkX:00}_{chunkY:00}_{region.MinCellX}_{region.MinCellY}_{region.MaxCellXExclusive}_{region.MaxCellYExclusive}_{region.WidthPixels}x{region.HeightPixels}.png";
            return Path.Combine(inputs.PersistentChunkCacheDirectory, fileName);
        }

        private static bool TryLoadTerrainSoftwareChunkTextureFromPersistentCacheLikeOriginal(
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            TerrainSoftwareChunkRegionLikeOriginal region,
            int chunkX,
            int chunkY,
            out Texture2D texture)
        {
            texture = null;
            string path = GetTerrainSoftwarePersistentChunkCacheFilePathLikeOriginal(inputs, region, chunkX, chunkY);
            if (string.IsNullOrEmpty(path))
                return false;

            if (!File.Exists(path))
            {
                inputs.PersistentChunkCacheMisses++;
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = $"TerrainChunkSoftware_{chunkX:00}_{chunkY:00}_Cached",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 16,
                    mipMapBias = -0.75f
                };

                if (!ImageConversion.LoadImage(loaded, bytes, false) ||
                    loaded.width != region.WidthPixels ||
                    loaded.height != region.HeightPixels)
                {
                    SafeDestroy(loaded);
                    inputs.PersistentChunkCacheMisses++;
                    return false;
                }

                loaded.wrapMode = TextureWrapMode.Clamp;
                loaded.filterMode = FilterMode.Trilinear;
                loaded.anisoLevel = 16;
                loaded.mipMapBias = -0.75f;
                loaded.Apply(false, false);
                texture = loaded;
                inputs.PersistentChunkCacheHits++;
                return true;
            }
            catch (Exception ex)
            {
                inputs.PersistentChunkCacheMisses++;
                LogTerrainSoftwarePersistentCacheWarningLikeOriginal("load chunk failed: " + ex.Message);
                return false;
            }
        }

        private static void TrySaveTerrainSoftwareChunkTextureToPersistentCacheLikeOriginal(
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            TerrainSoftwareChunkRegionLikeOriginal region,
            int chunkX,
            int chunkY,
            Texture2D texture)
        {
            // MIDDLE_PIXEL_PARALLEL_NO_PNG_V42_BASE_ONLY_NO_QUALITY_FACTURES: PNG cache writes are disabled.
        }

        private static void LogTerrainSoftwarePersistentCacheWarningLikeOriginal(string message)
        {
            if (s_terrainSoftwarePersistentCacheWarningLoggedLikeOriginal)
                return;

            s_terrainSoftwarePersistentCacheWarningLoggedLikeOriginal = true;
            UnityEngine.Debug.LogWarning("[C2:REN][AR-1] software baked chunk cache warning: " + message);
        }

        private static void HashByte64LikeOriginal(ref ulong hash, byte value)
        {
            hash ^= value;
            hash *= 1099511628211UL;
        }

        private static void HashInt64LikeOriginal(ref ulong hash, int value)
        {
            unchecked
            {
                HashByte64LikeOriginal(ref hash, (byte)value);
                HashByte64LikeOriginal(ref hash, (byte)(value >> 8));
                HashByte64LikeOriginal(ref hash, (byte)(value >> 16));
                HashByte64LikeOriginal(ref hash, (byte)(value >> 24));
            }
        }

        private static void HashLong64LikeOriginal(ref ulong hash, long value)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                    HashByte64LikeOriginal(ref hash, (byte)(value >> (i * 8)));
            }
        }

        private static void HashFloat64LikeOriginal(ref ulong hash, float value)
        {
            HashInt64LikeOriginal(ref hash, BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        private static void HashString64LikeOriginal(ref ulong hash, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                HashInt64LikeOriginal(ref hash, 0);
                return;
            }

            HashInt64LikeOriginal(ref hash, value.Length);
            for (int i = 0; i < value.Length; i++)
                HashInt64LikeOriginal(ref hash, value[i]);
        }

        private static void HashByteArray64LikeOriginal(ref ulong hash, byte[] data)
        {
            if (data == null)
            {
                HashInt64LikeOriginal(ref hash, -1);
                return;
            }

            HashInt64LikeOriginal(ref hash, data.Length);
            for (int i = 0; i < data.Length; i++)
                HashByte64LikeOriginal(ref hash, data[i]);
        }

        private static void HashShortArray64LikeOriginal(ref ulong hash, short[] data)
        {
            if (data == null)
            {
                HashInt64LikeOriginal(ref hash, -1);
                return;
            }

            HashInt64LikeOriginal(ref hash, data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                int value = data[i];
                HashByte64LikeOriginal(ref hash, (byte)value);
                HashByte64LikeOriginal(ref hash, (byte)(value >> 8));
            }
        }

        private static TerrainSoftwareChunkRegionLikeOriginal CreateTerrainSoftwareChunkRegionLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            int minCellX,
            int maxCellXExclusive,
            int minCellY,
            int maxCellYExclusive)
        {
            return new TerrainSoftwareChunkRegionLikeOriginal
            {
                MinCellX = minCellX,
                MaxCellXExclusive = maxCellXExclusive,
                MinCellY = minCellY,
                MaxCellYExclusive = maxCellYExclusive,
                WidthPixels = Mathf.Max(2, (maxCellXExclusive - minCellX) * TerrainSoftwarePixelsPerCellLikeOriginal + 1),
                HeightPixels = Mathf.Max(2, (maxCellYExclusive - minCellY) * TerrainSoftwarePixelsPerCellLikeOriginal + 1),
                FootprintBounds = ComputeTerrainSoftwareFootprintBoundsLikeOriginal(map, kernel, minCellX, maxCellXExclusive, minCellY, maxCellYExclusive)
            };
        }

        private static Bounds ComputeTerrainSoftwareFootprintBoundsLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            int minCellX,
            int maxCellXExclusive,
            int minCellY,
            int maxCellYExclusive)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            int minVertexX = minCellX;
            int maxVertexXExclusive = Mathf.Min(map.VertInLine, maxCellXExclusive + 1);
            int minVertexY = minCellY;
            int maxVertexYExclusive = Mathf.Min(map.MaxTH, maxCellYExclusive + 1);

            for (int vertexY = minVertexY; vertexY < maxVertexYExclusive; vertexY++)
            {
                for (int vertexX = minVertexX; vertexX < maxVertexXExclusive; vertexX++)
                {
                    int vertexIndex = vertexY * map.VertInLine + vertexX;
                    float rawX = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, vertexX);
                    float rawZ = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, vertexX, vertexY);
                    Vector3 world = CreateKernelWorldVertexLikeOriginal(map, kernel, vertexIndex, rawX, rawZ);

                    if (!hasBounds)
                    {
                        bounds = new Bounds(world, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(world);
                    }
                }
            }

            if (!hasBounds)
                bounds = new Bounds(Vector3.zero, Vector3.one);

            if (bounds.size.x < 0.001f)
                bounds.Encapsulate(bounds.center + new Vector3(0.001f, 0.0f, 0.0f));
            if (bounds.size.z < 0.001f)
                bounds.Encapsulate(bounds.center + new Vector3(0.0f, 0.0f, 0.001f));
            return bounds;
        }

        private void BuildSoftwareBakedFactureOverlaysLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            Transform parent)
        {
            if (map == null || parent == null || !HasFactureLayerDataLikeOriginal(map))
                return;

            int cellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            if (cellsX <= 0)
                return;

            int stripeWidth = Mathf.Clamp(StripeColumnWidth, 1, Mathf.Max(1, cellsX));
            int stripeCount = Mathf.Max(1, Mathf.CeilToInt(cellsX / (float)stripeWidth));

            BeginFactureCoverageAuditLikeAdapted(map, kernel, stripeCount);
            try
            {
                for (int stripe = 0; stripe < stripeCount; stripe++)
                {
                    int startX = kernel.MinCellX + stripe * stripeWidth;
                    int endX = Mathf.Min(kernel.MaxCellXExclusive, startX + stripeWidth);
                    if (endX <= startX)
                        continue;

                    BuildFactureStripeLayerLikeAdapted(map, kernel, startX, endX, parent, stripe);
                }
            }
            finally
            {
                EndFactureCoverageAuditLikeAdapted();
            }
        }

        private Texture2D BakeTerrainChunkSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            int chunkX,
            int chunkY)
        {
            if (TryLoadTerrainSoftwareChunkTextureFromPersistentCacheLikeOriginal(inputs, region, chunkX, chunkY, out Texture2D cachedTexture))
                return cachedTexture;

            var pixels = new Color32[region.WidthPixels * region.HeightPixels];
            if (!s_terrainSoftwareFallbackStructureFeatherPathLoggedV1LikeAdapted)
            {
                s_terrainSoftwareFallbackStructureFeatherPathLoggedV1LikeAdapted = true;
                UnityEngine.Debug.Log("[C2:HOLECLOSER ERODE V3] active: fallback structures use inward erosion + clustered dust alpha; no straight edge ribbons, no triangle-edge fade.");
            }
            var baseCoverage = new byte[pixels.Length];
            var tex44Protection = new byte[pixels.Length];
            var baseTileIds = new byte[pixels.Length];
            var fallbackStructurePixels = new Color32[pixels.Length];
            var fallbackStructureMask = new byte[pixels.Length];
            var fallbackStructureAlpha = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 255);
                baseTileIds[i] = 255;
                fallbackStructurePixels[i] = new Color32(0, 0, 0, 255);
                fallbackStructureMask[i] = 0;
                fallbackStructureAlpha[i] = 0;
            }

            List<FactureTriangleCopyDescriptorLikeAdapted> scratchFactureCopies =
                (!TerrainQualityFactureLayerDisabledLikeAdapted && HasFactureLayerDataLikeOriginal(map))
                    ? new List<FactureTriangleCopyDescriptorLikeAdapted>(4)
                    : null;

            for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
            {
                for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                {
                    BakeTerrainCellSoftwareLikeOriginal(map, kernel, region, inputs, pixels, baseCoverage, tex44Protection, cellX, cellY, baseTileIds);
                }
            }

            CloseBaseCoverageGapsLikeOriginal(pixels, baseCoverage, region.WidthPixels, region.HeightPixels);

            if (TerrainSoftwareBaseSoftBlendEnabledLikeAdapted)
                SoftenBaseTileTransitionsLikeAdapted(pixels, baseTileIds, region.WidthPixels, region.HeightPixels, TerrainSoftwareBaseTileSoftBlendRadiusLikeAdapted, TerrainSoftwareBaseTileSoftBlendPassesLikeAdapted, TerrainSoftwareBaseTileSoftBlendStrengthLikeAdapted);

            if (scratchFactureCopies != null)
            {
                for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
                {
                    for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                    {
                        BakeTerrainCellFactureSoftwareLikeOriginal(map, kernel, region, inputs, pixels, cellX, cellY, scratchFactureCopies, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);
                    }
                }
            }

            if (TerrainSoftwareFallbackStructureFeatherV1LikeAdapted)
                CompositeFallbackStructuresWithFeatherV1LikeAdapted(
                    pixels,
                    fallbackStructurePixels,
                    fallbackStructureMask,
                    fallbackStructureAlpha,
                    region.WidthPixels,
                    region.HeightPixels,
                    TerrainSoftwareFallbackStructureFeatherRadiusV1LikeAdapted);

            BleedChunkTextureEdgesLikeAdapted(pixels, region.WidthPixels, region.HeightPixels, 3);

            var texture = new Texture2D(region.WidthPixels, region.HeightPixels, TextureFormat.RGBA32, true)
            {
                name = $"TerrainChunkSoftware_{chunkX:00}_{chunkY:00}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16,
                mipMapBias = -0.75f
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            TrySaveTerrainSoftwareChunkTextureToPersistentCacheLikeOriginal(inputs, region, chunkX, chunkY, texture);
            return texture;
        }

        private void BakeTerrainCellSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            int cellX,
            int cellY,
            byte[] baseTileIds = null)
        {
            OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);
            CellVertexPayloadLikeOriginal v0 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V0);
            CellVertexPayloadLikeOriginal v1 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V1);
            CellVertexPayloadLikeOriginal v2 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V2);
            CellVertexPayloadLikeOriginal v3 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V3);

            if (TryBuildCellStageLikeOriginal(map, cell, true, out CellSurfaceStageLikeOriginal stage1))
                BakeCellStageSoftwareLikeOriginal(map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell, v0, v1, v2, v3, stage1, baseTileIds);

            if (TryBuildCellStageLikeOriginal(map, cell, false, out CellSurfaceStageLikeOriginal stage2))
                BakeCellStageSoftwareLikeOriginal(map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell, v0, v1, v2, v3, stage2, baseTileIds);
        }

        private void BakeTerrainCellFactureSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            int cellX,
            int cellY,
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies,
            Color32[] fallbackStructurePixels,
            byte[] fallbackStructureMask,
            byte[] fallbackStructureAlpha)
        {
            if (TerrainQualityFactureLayerDisabledLikeAdapted)
                return;

            if (scratchCopies == null || !HasFactureLayerDataLikeOriginal(map))
                return;

            OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);
            CellVertexPayloadLikeOriginal v0 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V0);
            CellVertexPayloadLikeOriginal v1 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V1);
            CellVertexPayloadLikeOriginal v2 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V2);
            CellVertexPayloadLikeOriginal v3 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V3);

            if ((cell.V0 % map.VertInLine & 1) != 0)
            {
                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.OddLeft,
                    cellX, cellY,
                    v0, v1, v2,
                    scratchCopies, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);

                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    cellX, cellY,
                    v2, v1, v3,
                    scratchCopies, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);
            }
            else
            {
                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    cellX, cellY,
                    v0, v1, v3,
                    scratchCopies, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);

                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.EvenLower,
                    cellX, cellY,
                    v0, v3, v2,
                    scratchCopies, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);
            }
        }

        private void BakeFactureTriangleSoftwareLikeOriginal(
            ParsedMap map,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            BaseSurfaceTriangleKindLikeOriginal kind,
            int cellX,
            int cellY,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies,
            Color32[] fallbackStructurePixels,
            byte[] fallbackStructureMask,
            byte[] fallbackStructureAlpha)
        {
            scratchCopies.Clear();
            ExpandFactureTriangleCopiesLikeAdapted(map, kind, cellX, cellY, a.Index, b.Index, c.Index, scratchCopies);

            bool emittedAny = scratchCopies.Count > 0;
            for (int i = 0; i < scratchCopies.Count; i++)
                RasterizeFactureTriangleDescriptorSoftwareLikeOriginal(map, region, inputs, targetPixels, a, b, c, scratchCopies[i], false, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);

            GetFactureTriangleCoverageLikeAdapted(scratchCopies, out int coverageA, out int coverageB, out int coverageC);

            // Hole-closer logic is not changed. Only its final drawing target is changed:
            // fallback draws into an internal structure buffer, then the structure gets a 0->100 alpha feather.
            if (NeedsSoftwareFactureFallbackLikeAdapted(emittedAny, coverageA, coverageB, coverageC))
            {
                if (TryBuildSoftwareFactureFallbackDescriptorLikeAdapted(map, kind, cellX, cellY, a, b, c, coverageA, coverageB, coverageC, out FactureTriangleCopyDescriptorLikeAdapted fallback))
                    RasterizeFactureTriangleDescriptorSoftwareLikeOriginal(map, region, inputs, targetPixels, a, b, c, fallback, true, fallbackStructurePixels, fallbackStructureMask, fallbackStructureAlpha);
            }
        }

        private static bool TryBuildSoftwareFactureFallbackDescriptorLikeAdapted(
            ParsedMap map,
            BaseSurfaceTriangleKindLikeOriginal kind,
            int cellX,
            int cellY,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int coverageA,
            int coverageB,
            int coverageC,
            out FactureTriangleCopyDescriptorLikeAdapted descriptor)
        {
            descriptor = default;
            if (map == null)
                return false;

            if (!TryChooseTriangleWinnerRenderFactureIdLikeAdapted(map, a, b, c, out int renderFactureId))
                return false;

            int bucketTextureId = GetFactureBucketTextureIdLikeAdapted(renderFactureId);
            if (bucketTextureId == 0)
                return false;

            int fallbackWeightA = BuildSoftwareFactureFallbackWeightLikeAdapted(coverageA);
            int fallbackWeightB = BuildSoftwareFactureFallbackWeightLikeAdapted(coverageB);
            int fallbackWeightC = BuildSoftwareFactureFallbackWeightLikeAdapted(coverageC);
            if (fallbackWeightA <= 0 && fallbackWeightB <= 0 && fallbackWeightC <= 0)
                return false;

            descriptor = new FactureTriangleCopyDescriptorLikeAdapted
            {
                SourceKind = kind,
                SourceCellX = cellX,
                SourceCellY = cellY,
                VertexA = a.Index,
                VertexB = b.Index,
                VertexC = c.Index,
                SourceFactureA = renderFactureId,
                SourceFactureB = renderFactureId,
                SourceFactureC = renderFactureId,
                CopyFactureId = renderFactureId,
                Usage = FactureUsageLikeOriginal.Unknown,
                Orientation = FactureOrientationLikeAdapted.None,
                VariantIndex = 0,
                WeightA = fallbackWeightA,
                WeightB = fallbackWeightB,
                WeightC = fallbackWeightC,
                BucketTextureId = bucketTextureId,
                HasBump = ResolveFactureBumpFlagLikeAdapted(bucketTextureId)
            };

            GetFactureUvwLikeAdapted(map, a.Index, renderFactureId, out descriptor.UvA, out _);
            GetFactureUvwLikeAdapted(map, b.Index, renderFactureId, out descriptor.UvB, out _);
            GetFactureUvwLikeAdapted(map, c.Index, renderFactureId, out descriptor.UvC, out _);
            return true;
        }

        private static void GetFactureTriangleCoverageLikeAdapted(
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies,
            out int coverageA,
            out int coverageB,
            out int coverageC)
        {
            coverageA = 0;
            coverageB = 0;
            coverageC = 0;
            if (scratchCopies == null)
                return;

            for (int i = 0; i < scratchCopies.Count; i++)
            {
                FactureTriangleCopyDescriptorLikeAdapted copy = scratchCopies[i];
                coverageA = Mathf.Clamp(coverageA + Mathf.Max(0, copy.WeightA), 0, 255);
                coverageB = Mathf.Clamp(coverageB + Mathf.Max(0, copy.WeightB), 0, 255);
                coverageC = Mathf.Clamp(coverageC + Mathf.Max(0, copy.WeightC), 0, 255);
            }
        }

        private static bool NeedsSoftwareFactureFallbackLikeAdapted(bool emittedAny, int coverageA, int coverageB, int coverageC)
        {
            // V9: return the old hole-closing behavior.
            // Fill any missing/weak facture coverage so the base underlayer does not leak through.
            if (TerrainSoftwareDisableFactureFallbackV7LikeAdapted)
                return false;

            if (!emittedAny)
                return true;

            return coverageA < TerrainSoftwareFactureFallbackCoverageTargetLikeAdapted ||
                   coverageB < TerrainSoftwareFactureFallbackCoverageTargetLikeAdapted ||
                   coverageC < TerrainSoftwareFactureFallbackCoverageTargetLikeAdapted;
        }

        private static int BuildSoftwareFactureFallbackWeightLikeAdapted(int coverage)
        {
            int missingCoverage = TerrainSoftwareFactureFallbackCoverageTargetLikeAdapted - Mathf.Clamp(coverage, 0, 255);
            if (missingCoverage <= 0)
                return 0;

            // V9: old aggressive fill weight restored.
            return Mathf.Clamp(Mathf.Max(missingCoverage, 64), 0, 255);
        }

        private void BakeTerrainCellFallbackSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            OriginalCellTriangulationLikeOriginal cell,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3)
        {
            int fallbackTile = ResolveCellFallbackBaseTileLikeOriginal(map, cell);
            if (fallbackTile < 0)
                return;

            if ((cell.V0 % map.VertInLine & 1) != 0)
            {
                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddLeft,
                    v0, v1, v2,
                    fallbackTile);

                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    v2, v1, v3,
                    fallbackTile);
            }
            else
            {
                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    v0, v1, v3,
                    fallbackTile);

                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenLower,
                    v0, v3, v2,
                    fallbackTile);
            }
        }

        private void BakeFallbackTriangleUncoveredOnlyLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int fallbackTile)
        {
            ExpandedTriangleCopyLikeOriginal copy;
            BuildInitialExpandedTriangleCopyLikeOriginal(
                kind,
                cell,
                fallbackTile,
                true,
                true,
                fallbackTile, fallbackTile, fallbackTile,
                255, 255, 255,
                out copy);

            BaseSurfaceTriangleDescriptorLikeAdapted descriptor = BuildTriangleDescriptorFromCopyLikeAdapted(
                map,
                inputs.Tables,
                kind,
                BaseSurfaceTriangleCopyRoleLikeAdapted.Primary,
                true,
                a.Index, b.Index, c.Index,
                fallbackTile, fallbackTile, fallbackTile,
                fallbackTile, fallbackTile, fallbackTile,
                255, 255, 255,
                copy);

            RasterizeTriangleDescriptorSoftwareLikeOriginal(
                map,
                kernel,
                region,
                inputs,
                targetPixels,
                baseCoverage,
                tex44Protection,
                a,
                b,
                c,
                descriptor,
                true);
        }

        private static int ResolveCellFallbackBaseTileLikeOriginal(ParsedMap map, OriginalCellTriangulationLikeOriginal cell)
        {
            if (map == null || map.TexMap == null)
                return -1;

            int[] tiles =
            {
                GetVertexTileLikeOriginal(map.TexMap, cell.V0),
                GetVertexTileLikeOriginal(map.TexMap, cell.V1),
                GetVertexTileLikeOriginal(map.TexMap, cell.V2),
                GetVertexTileLikeOriginal(map.TexMap, cell.V3),
            };

            int bestTile = -1;
            int bestCount = -1;
            for (int i = 0; i < tiles.Length; i++)
            {
                int tile = tiles[i];
                if (tile < 0)
                    continue;

                int count = 0;
                for (int j = 0; j < tiles.Length; j++)
                {
                    if (tiles[j] == tile)
                        count++;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestTile = tile;
                }
            }

            return bestTile >= 0 ? bestTile : 0;
        }

        private void BakeCellStageSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            OriginalCellTriangulationLikeOriginal cell,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3,
            CellSurfaceStageLikeOriginal stage,
            byte[] baseTileIds = null)
        {
            if ((cell.V0 % map.VertInLine & 1) != 0)
            {
                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddLeft,
                    v0, v1, v2,
                    stage.T0, stage.T1, stage.T2,
                    stage,
                    baseTileIds);

                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    v2, v1, v3,
                    stage.T2, stage.T1, stage.T3,
                    stage,
                    baseTileIds);
            }
            else
            {
                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    v0, v1, v3,
                    stage.T0, stage.T1, stage.T3,
                    stage,
                    baseTileIds);

                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, tex44Protection, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenLower,
                    v0, v3, v2,
                    stage.T0, stage.T3, stage.T2,
                    stage,
                    baseTileIds);
            }
        }

        private void BakeExpandedTriangleStageSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int tA,
            int tB,
            int tC,
            CellSurfaceStageLikeOriginal stage,
            byte[] baseTileIds = null)
        {
            int wA = ResolveCellVertexStageWeightLikeOriginal(cell, a.Index, stage.W0, stage.W1, stage.W2, stage.W3);
            int wB = ResolveCellVertexStageWeightLikeOriginal(cell, b.Index, stage.W0, stage.W1, stage.W2, stage.W3);
            int wC = ResolveCellVertexStageWeightLikeOriginal(cell, c.Index, stage.W0, stage.W1, stage.W2, stage.W3);

            if (TerrainSoftwareBaseWeightedCompositeV3LikeAdapted)
            {
                RasterizeWeightedCompositeTriangleStageSoftwareV3LikeAdapted(
                    map,
                    kernel,
                    region,
                    inputs,
                    targetPixels,
                    baseCoverage,
                    tex44Protection,
                    baseTileIds,
                    cell,
                    kind,
                    a,
                    b,
                    c,
                    tA,
                    tB,
                    tC,
                    wA,
                    wB,
                    wC,
                    stage);
                return;
            }

            int tMin;
            int tAve;
            int tMax;
            BuildSortedTriangleTilesLikeOriginal(kind, tA, tB, tC, out tMin, out tAve, out tMax);

            ExpandedTriangleCopyLikeOriginal copy0;
            BuildInitialExpandedTriangleCopyLikeOriginal(kind, cell, tMin, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copy0);
            RasterizeTriangleDescriptorSoftwareLikeOriginal(
                map, kernel, region, inputs, targetPixels,
                baseCoverage,
                tex44Protection,
                a, b, c,
                BuildTriangleDescriptorFromCopyLikeAdapted(
                    map, inputs.Tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Primary, stage.IsBaseStage,
                    a.Index, b.Index, c.Index,
                    tA, tB, tC,
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                    wA, wB, wC,
                    copy0),
                baseTileIds: baseTileIds);

            if (tAve != tMin)
            {
                ExpandedTriangleCopyLikeOriginal copyAve;
                BuildAverageExpandedTriangleCopyLikeOriginal(kind, cell, tMin, tAve, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyAve);
                RasterizeTriangleDescriptorSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels,
                    baseCoverage,
                    tex44Protection,
                    a, b, c,
                    BuildTriangleDescriptorFromCopyLikeAdapted(
                        map, inputs.Tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Average, stage.IsBaseStage,
                        a.Index, b.Index, c.Index,
                        tA, tB, tC,
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                        wA, wB, wC,
                        copyAve),
                    baseTileIds: baseTileIds);
            }

            if (tMax != tMin && tMax != tAve)
            {
                ExpandedTriangleCopyLikeOriginal copyMax;
                BuildMaximumExpandedTriangleCopyLikeOriginal(kind, cell, tAve, tMax, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyMax);
                RasterizeTriangleDescriptorSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels,
                    baseCoverage,
                    tex44Protection,
                    a, b, c,
                    BuildTriangleDescriptorFromCopyLikeAdapted(
                        map, inputs.Tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Maximum, stage.IsBaseStage,
                        a.Index, b.Index, c.Index,
                        tA, tB, tC,
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                        wA, wB, wC,
                        copyMax),
                    baseTileIds: baseTileIds);
            }
        }

        private void RasterizeWeightedCompositeTriangleStageSoftwareV3LikeAdapted(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            byte[] baseTileIds,
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int tA,
            int tB,
            int tC,
            int wA,
            int wB,
            int wC,
            CellSurfaceStageLikeOriginal stage)
        {
            if (inputs == null || inputs.GroundPixels == null || inputs.GroundPixels.Length == 0 || targetPixels == null || targetPixels.Length == 0)
                return;

            int tMin;
            int tAve;
            int tMax;
            BuildSortedTriangleTilesLikeOriginal(kind, tA, tB, tC, out tMin, out tAve, out tMax);

            var descriptors = new BaseSurfaceTriangleDescriptorLikeAdapted[3];
            int descriptorCount = 0;

            ExpandedTriangleCopyLikeOriginal copy0;
            BuildInitialExpandedTriangleCopyLikeOriginal(kind, cell, tMin, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copy0);
            TryAddWeightedCompositeDescriptorSoftwareV3LikeAdapted(
                map, inputs, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Primary, stage.IsBaseStage,
                a, b, c, tA, tB, tC, wA, wB, wC, copy0, descriptors, ref descriptorCount);

            if (tAve != tMin)
            {
                ExpandedTriangleCopyLikeOriginal copyAve;
                BuildAverageExpandedTriangleCopyLikeOriginal(kind, cell, tMin, tAve, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyAve);
                TryAddWeightedCompositeDescriptorSoftwareV3LikeAdapted(
                    map, inputs, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Average, stage.IsBaseStage,
                    a, b, c, tA, tB, tC, wA, wB, wC, copyAve, descriptors, ref descriptorCount);
            }

            if (tMax != tMin && tMax != tAve)
            {
                ExpandedTriangleCopyLikeOriginal copyMax;
                BuildMaximumExpandedTriangleCopyLikeOriginal(kind, cell, tAve, tMax, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyMax);
                TryAddWeightedCompositeDescriptorSoftwareV3LikeAdapted(
                    map, inputs, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Maximum, stage.IsBaseStage,
                    a, b, c, tA, tB, tC, wA, wB, wC, copyMax, descriptors, ref descriptorCount);
            }

            if (descriptorCount <= 0)
                return;

            Vector2 pA = ProjectWorldToChunkPixelLikeOriginal(region, a.World);
            Vector2 pB = ProjectWorldToChunkPixelLikeOriginal(region, b.World);
            Vector2 pC = ProjectWorldToChunkPixelLikeOriginal(region, c.World);

            float area = EdgeFunctionLikeOriginal(pA, pB, pC);
            if (Mathf.Abs(area) < 0.0001f)
                return;

            var uvA = new Vector2[descriptorCount];
            var uvB = new Vector2[descriptorCount];
            var uvC = new Vector2[descriptorCount];
            var colorA = new Color32[descriptorCount];
            var colorB = new Color32[descriptorCount];
            var colorC = new Color32[descriptorCount];
            var preferStandaloneTex44 = new bool[descriptorCount];

            for (int i = 0; i < descriptorCount; i++)
            {
                BaseSurfaceTriangleDescriptorLikeAdapted descriptor = descriptors[i];

                BuildBaseTriangleUvExplicitLikeOriginal(
                    descriptor.Kind,
                    descriptor.ResolvedTile,
                    descriptor.SeedVertexU,
                    descriptor.SeedSetU,
                    descriptor.SeedVertexV,
                    descriptor.SeedSetV,
                    out uvA[i],
                    out uvB[i],
                    out uvC[i]);

                int alphaA = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaA * 255.0f), 0, 255);
                int alphaB = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaB * 255.0f), 0, 255);
                int alphaC = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaC * 255.0f), 0, 255);

                colorA[i] = BuildStrictSurfaceVertexColorLikeOriginal(descriptor.VertexA, alphaA);
                colorB[i] = BuildStrictSurfaceVertexColorLikeOriginal(descriptor.VertexB, alphaB);
                colorC[i] = BuildStrictSurfaceVertexColorLikeOriginal(descriptor.VertexC, alphaC);
                preferStandaloneTex44[i] = ShouldRevealStandaloneTex44InBakeLikeAdapted(descriptor, inputs);
            }

            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.x, Mathf.Min(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.x, Mathf.Max(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.y, Mathf.Min(pB.y, pC.y))), 0, region.HeightPixels - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.y, Mathf.Max(pB.y, pC.y))), 0, region.HeightPixels - 1);

            float invArea = 1.0f / area;
            const float inv255 = 1.0f / 255.0f;

            float edge0Dx = (pC.y - pB.y) * invArea;
            float edge0Dy = -(pC.x - pB.x) * invArea;
            float edge1Dx = (pA.y - pC.y) * invArea;
            float edge1Dy = -(pA.x - pC.x) * invArea;

            float startX = minX + 0.5f;
            float startY = minY + 0.5f;
            float rowW0 = ((startX - pB.x) * (pC.y - pB.y) - (startY - pB.y) * (pC.x - pB.x)) * invArea;
            float rowW1 = ((startX - pC.x) * (pA.y - pC.y) - (startY - pC.y) * (pA.x - pC.x)) * invArea;

            for (int y = minY; y <= maxY; y++)
            {
                float bw0 = rowW0;
                float bw1 = rowW1;
                int row = y * region.WidthPixels;

                for (int x = minX; x <= maxX; x++)
                {
                    float bw2 = 1.0f - bw0 - bw1;
                    if (bw0 >= TerrainSoftwareRasterToleranceLikeOriginal &&
                        bw1 >= TerrainSoftwareRasterToleranceLikeOriginal &&
                        bw2 >= TerrainSoftwareRasterToleranceLikeOriginal)
                    {
                        float sumR = 0.0f;
                        float sumG = 0.0f;
                        float sumB = 0.0f;
                        float sumWeight = 0.0f;
                        float dominantWeight = -1.0f;
                        int dominantTile = 255;

                        for (int i = 0; i < descriptorCount; i++)
                        {
                            BaseSurfaceTriangleDescriptorLikeAdapted descriptor = descriptors[i];
                            float localWeight = descriptor.AlphaA * bw0 + descriptor.AlphaB * bw1 + descriptor.AlphaC * bw2;
                            if (localWeight <= TerrainSoftwareBaseWeightedCompositeMinAlphaV3LikeAdapted)
                                continue;

                            float uvx = uvA[i].x * bw0 + uvB[i].x * bw1 + uvC[i].x * bw2;
                            float uvy = uvA[i].y * bw0 + uvB[i].y * bw1 + uvC[i].y * bw2;
                            float lr = (colorA[i].r * bw0 + colorB[i].r * bw1 + colorC[i].r * bw2) * inv255;
                            float lg = (colorA[i].g * bw0 + colorB[i].g * bw1 + colorC[i].g * bw2) * inv255;
                            float lb = (colorA[i].b * bw0 + colorB[i].b * bw1 + colorC[i].b * bw2) * inv255;

                            float atlasR;
                            float atlasG;
                            float atlasB;
                            if (preferStandaloneTex44[i] && inputs.StandaloneTex44Pixels != null && inputs.StandaloneTex44Pixels.Length > 0)
                            {
                                float localUvX = WrapAtlasUvToSingleTileLikeAdapted(uvx);
                                float localUvY = WrapAtlasUvToSingleTileLikeAdapted(uvy);
                                SampleTextureBilinearRgbaFastLikeOriginal(inputs.StandaloneTex44Pixels, inputs.StandaloneTex44Width, inputs.StandaloneTex44Height, localUvX, localUvY, true, out atlasR, out atlasG, out atlasB, out _);
                            }
                            else
                            {
                                SampleTextureBilinearRgbaFastLikeOriginal(inputs.GroundPixels, inputs.GroundWidth, inputs.GroundHeight, uvx, uvy, false, out atlasR, out atlasG, out atlasB, out _);
                            }

                            float srcR = Clamp01FastLikeOriginal(atlasR * lr * 2.0f);
                            float srcG = Clamp01FastLikeOriginal(atlasG * lg * 2.0f);
                            float srcB = Clamp01FastLikeOriginal(atlasB * lb * 2.0f);

                            sumR += srcR * localWeight;
                            sumG += srcG * localWeight;
                            sumB += srcB * localWeight;
                            sumWeight += localWeight;

                            if (localWeight > dominantWeight)
                            {
                                dominantWeight = localWeight;
                                dominantTile = descriptor.ResolvedTile & 63;
                            }
                        }

                        if (sumWeight > TerrainSoftwareBaseWeightedCompositeMinAlphaV3LikeAdapted)
                        {
                            int pixelIndex = row + x;
                            float outR = sumR / sumWeight;
                            float outG = sumG / sumWeight;
                            float outB = sumB / sumWeight;

                            if (stage.IsBaseStage)
                            {
                                targetPixels[pixelIndex] = new Color32(
                                    ToByteRoundClampLikeOriginal(outR * 255.0f),
                                    ToByteRoundClampLikeOriginal(outG * 255.0f),
                                    ToByteRoundClampLikeOriginal(outB * 255.0f),
                                    255);

                                if (baseCoverage != null)
                                    baseCoverage[pixelIndex] = 255;
                                if (baseTileIds != null && pixelIndex >= 0 && pixelIndex < baseTileIds.Length)
                                    baseTileIds[pixelIndex] = (byte)Mathf.Clamp(dominantTile, 0, 255);
                                if (dominantTile == TerrainSoftwareTex44RevealTileIdLikeAdapted && tex44Protection != null)
                                    tex44Protection[pixelIndex] = 255;
                            }
                            else
                            {
                                float finalAlpha = Clamp01FastLikeOriginal(sumWeight * TerrainSoftwareBaseWeightedCompositeOverlayStrengthV3LikeAdapted);
                                if (finalAlpha >= TerrainSoftwareBaseOverlayAlphaClipLikeAdapted)
                                {
                                    Color32 dst = targetPixels[pixelIndex];
                                    float invA = 1.0f - finalAlpha;
                                    targetPixels[pixelIndex] = new Color32(
                                        ToByteRoundClampLikeOriginal((outR * finalAlpha + dst.r * inv255 * invA) * 255.0f),
                                        ToByteRoundClampLikeOriginal((outG * finalAlpha + dst.g * inv255 * invA) * 255.0f),
                                        ToByteRoundClampLikeOriginal((outB * finalAlpha + dst.b * inv255 * invA) * 255.0f),
                                        255);

                                    if (baseTileIds != null &&
                                        dominantTile >= 0 &&
                                        dominantTile <= 255 &&
                                        finalAlpha >= TerrainSoftwareBaseOverlayTileIdAlphaLikeAdapted)
                                        baseTileIds[pixelIndex] = (byte)dominantTile;

                                    if (dominantTile == TerrainSoftwareTex44RevealTileIdLikeAdapted && tex44Protection != null)
                                        tex44Protection[pixelIndex] = 255;
                                }
                            }
                        }
                    }

                    bw0 += edge0Dx;
                    bw1 += edge1Dx;
                }

                rowW0 += edge0Dy;
                rowW1 += edge1Dy;
            }
        }

        private static void TryAddWeightedCompositeDescriptorSoftwareV3LikeAdapted(
            ParsedMap map,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            BaseSurfaceTriangleKindLikeOriginal kind,
            BaseSurfaceTriangleCopyRoleLikeAdapted role,
            bool isBaseStage,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int tA,
            int tB,
            int tC,
            int wA,
            int wB,
            int wC,
            ExpandedTriangleCopyLikeOriginal copy,
            BaseSurfaceTriangleDescriptorLikeAdapted[] descriptors,
            ref int descriptorCount)
        {
            if (descriptors == null || descriptorCount < 0 || descriptorCount >= descriptors.Length || inputs == null)
                return;

            BaseSurfaceTriangleDescriptorLikeAdapted descriptor = BuildTriangleDescriptorFromCopyLikeAdapted(
                map,
                inputs.Tables,
                kind,
                role,
                isBaseStage,
                a.Index,
                b.Index,
                c.Index,
                tA,
                tB,
                tC,
                GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                wA,
                wB,
                wC,
                copy);

            if (descriptor.AlphaA <= 0.0f && descriptor.AlphaB <= 0.0f && descriptor.AlphaC <= 0.0f)
                return;
            if (!ShouldEmitOverlayDescriptorLikeAdapted(descriptor))
                return;

            descriptors[descriptorCount++] = descriptor;
        }

        private void RasterizeTriangleDescriptorSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            BaseSurfaceTriangleDescriptorLikeAdapted descriptor,
            bool onlyIfUncovered = false,
            byte[] baseTileIds = null)
        {
            if (descriptor.AlphaA <= 0.0f && descriptor.AlphaB <= 0.0f && descriptor.AlphaC <= 0.0f)
                return;
            if (!ShouldEmitOverlayDescriptorLikeAdapted(descriptor))
                return;

            bool preferStandaloneTex44 = ShouldRevealStandaloneTex44InBakeLikeAdapted(descriptor, inputs);

            Vector2 uvA;
            Vector2 uvB;
            Vector2 uvC;

            BuildBaseTriangleUvExplicitLikeOriginal(
                descriptor.Kind,
                descriptor.ResolvedTile,
                descriptor.SeedVertexU,
                descriptor.SeedSetU,
                descriptor.SeedVertexV,
                descriptor.SeedSetV,
                out uvA,
                out uvB,
                out uvC);

            int alphaA = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaA * 255.0f), 0, 255);
            int alphaB = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaB * 255.0f), 0, 255);
            int alphaC = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaC * 255.0f), 0, 255);

            Vector2 crossA;
            Vector2 crossB;
            Vector2 crossC;
            BuildCrossTriangleUvForPairLikeOriginal(
                map, kernel, inputs.Tables,
                descriptor.VertexA, a.RawX, a.RawZ,
                descriptor.VertexB, b.RawX, b.RawZ,
                descriptor.VertexC, c.RawX, c.RawZ,
                descriptor.Vr,
                descriptor.IsLeft,
                descriptor.PlainMode,
                alphaA,
                alphaB,
                alphaC,
                out crossA,
                out crossB,
                out crossC);

            Color32 colorA = BuildStrictSurfaceVertexColorLikeOriginal(descriptor.VertexA, alphaA);
            Color32 colorB = BuildStrictSurfaceVertexColorLikeOriginal(descriptor.VertexB, alphaB);
            Color32 colorC = BuildStrictSurfaceVertexColorLikeOriginal(descriptor.VertexC, alphaC);

            Vector2 pA = ProjectWorldToChunkPixelLikeOriginal(region, a.World);
            Vector2 pB = ProjectWorldToChunkPixelLikeOriginal(region, b.World);
            Vector2 pC = ProjectWorldToChunkPixelLikeOriginal(region, c.World);

            RasterizeTriangleSoftwareLikeOriginal(
                region,
                inputs,
                targetPixels,
                baseCoverage,
                tex44Protection,
                pA, pB, pC,
                uvA, uvB, uvC,
                crossA, crossB, crossC,
                colorA, colorB, colorC,
                descriptor.IsBaseStage,
                descriptor.PlainMode,
                descriptor.ResolvedTile,
                preferStandaloneTex44,
                onlyIfUncovered,
                baseTileIds);
        }

        private void RasterizeFactureTriangleDescriptorSoftwareLikeOriginal(
            ParsedMap map,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            FactureTriangleCopyDescriptorLikeAdapted descriptor,
            bool isFallbackHoleClose,
            Color32[] fallbackStructurePixels,
            byte[] fallbackStructureMask,
            byte[] fallbackStructureAlpha)
        {
            int maxAlpha = Mathf.Max(descriptor.WeightA, Mathf.Max(descriptor.WeightB, descriptor.WeightC));
            if (maxAlpha <= FactureAlphaRefByteLikeOriginal)
                return;

            TerrainSoftwareFactureBakeCacheEntryLikeOriginal facture = GetOrCreateFactureBakeCacheEntryLikeOriginal(inputs, descriptor.BucketTextureId);
            if (facture == null)
                return;

            bool useDot3 =
                descriptor.HasBump &&
                facture.Dot3DiffusePixels != null && facture.Dot3DiffusePixels.Length > 0 &&
                facture.NormalPixels != null && facture.NormalPixels.Length > 0;

            Color32[] diffusePixels = useDot3 ? facture.Dot3DiffusePixels : facture.PlainDiffusePixels;
            int diffuseWidth = useDot3 ? facture.Dot3DiffuseWidth : facture.PlainDiffuseWidth;
            int diffuseHeight = useDot3 ? facture.Dot3DiffuseHeight : facture.PlainDiffuseHeight;
            if (diffusePixels == null || diffusePixels.Length == 0 || diffuseWidth <= 0 || diffuseHeight <= 0)
                return;

            Vector2 pA = ProjectWorldToChunkPixelLikeOriginal(region, ResolveFactureVertexWorldLikeAdapted(a, b, c, descriptor.VertexA));
            Vector2 pB = ProjectWorldToChunkPixelLikeOriginal(region, ResolveFactureVertexWorldLikeAdapted(a, b, c, descriptor.VertexB));
            Vector2 pC = ProjectWorldToChunkPixelLikeOriginal(region, ResolveFactureVertexWorldLikeAdapted(a, b, c, descriptor.VertexC));

            Color32 colorA = useDot3
                ? BuildFactureBumpVertexColorLikeAdapted(map, descriptor.VertexA, descriptor.WeightA, descriptor.BucketTextureId)
                : BuildFactureVertexColorLikeAdapted(descriptor.WeightA);
            Color32 colorB = useDot3
                ? BuildFactureBumpVertexColorLikeAdapted(map, descriptor.VertexB, descriptor.WeightB, descriptor.BucketTextureId)
                : BuildFactureVertexColorLikeAdapted(descriptor.WeightB);
            Color32 colorC = useDot3
                ? BuildFactureBumpVertexColorLikeAdapted(map, descriptor.VertexC, descriptor.WeightC, descriptor.BucketTextureId)
                : BuildFactureVertexColorLikeAdapted(descriptor.WeightC);

            RasterizeFactureTriangleSoftwareLikeOriginal(
                region,
                targetPixels,
                pA, pB, pC,
                descriptor.UvA, descriptor.UvB, descriptor.UvC,
                colorA, colorB, colorC,
                diffusePixels, diffuseWidth, diffuseHeight,
                useDot3 ? facture.NormalPixels : null,
                useDot3 ? facture.NormalWidth : 0,
                useDot3 ? facture.NormalHeight : 0,
                isFallbackHoleClose,
                fallbackStructurePixels,
                fallbackStructureMask,
                fallbackStructureAlpha);
        }

        private static Vector2 ProjectWorldToChunkPixelLikeOriginal(TerrainSoftwareChunkRegionLikeOriginal region, Vector3 world)
        {
            float sizeX = Mathf.Max(0.001f, region.FootprintBounds.size.x);
            float sizeZ = Mathf.Max(0.001f, region.FootprintBounds.size.z);
            float px = ((world.x - region.FootprintBounds.min.x) / sizeX) * Mathf.Max(1, region.WidthPixels - 1);
            float py = ((world.z - region.FootprintBounds.min.z) / sizeZ) * Mathf.Max(1, region.HeightPixels - 1);
            return new Vector2(px, py);
        }

        private static bool HasUncoveredPixelsInCellRectLikeOriginal(
            TerrainSoftwareChunkRegionLikeOriginal region,
            byte[] baseCoverage,
            int cellX,
            int cellY)
        {
            if (baseCoverage == null || baseCoverage.Length == 0)
                return false;

            int localCellX = cellX - region.MinCellX;
            int localCellY = cellY - region.MinCellY;
            if (localCellX < 0 || localCellY < 0)
                return false;

            int startX = Mathf.Clamp(localCellX * TerrainSoftwarePixelsPerCellLikeOriginal, 0, region.WidthPixels - 1);
            int endX = Mathf.Clamp(startX + TerrainSoftwarePixelsPerCellLikeOriginal, 0, region.WidthPixels - 1);
            int startY = Mathf.Clamp(localCellY * TerrainSoftwarePixelsPerCellLikeOriginal, 0, region.HeightPixels - 1);
            int endY = Mathf.Clamp(startY + TerrainSoftwarePixelsPerCellLikeOriginal, 0, region.HeightPixels - 1);

            for (int y = startY; y <= endY; y++)
            {
                int row = y * region.WidthPixels;
                for (int x = startX; x <= endX; x++)
                {
                    if (baseCoverage[row + x] == 0)
                        return true;
                }
            }

            return false;
        }

        private static float Hash01FallbackSprayV3LikeAdapted(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFF) * (1.0f / 16777215.0f);
            }
        }

        private static float HashFractalFallbackSprayV3LikeAdapted(int x, int y, int salt)
        {
            float coarse = Hash01FallbackSprayV3LikeAdapted(x >> 2, y >> 2, salt + 11);
            float mid = Hash01FallbackSprayV3LikeAdapted(x >> 1, y >> 1, salt + 37);
            float fine = Hash01FallbackSprayV3LikeAdapted(x, y, salt + 71);
            return coarse * 0.55f + mid * 0.30f + fine * 0.15f;
        }

        private static void CompositeFallbackStructuresWithFeatherV1LikeAdapted(
            Color32[] targetPixels,
            Color32[] fallbackPixels,
            byte[] fallbackMask,
            byte[] fallbackAlpha,
            int width,
            int height,
            int radius)
        {
            if (targetPixels == null || fallbackPixels == null || fallbackMask == null || fallbackAlpha == null)
                return;
            if (targetPixels.Length == 0 || fallbackPixels.Length != targetPixels.Length || fallbackMask.Length != targetPixels.Length || fallbackAlpha.Length != targetPixels.Length)
                return;
            if (width <= 2 || height <= 2)
                return;

            int total = Mathf.Min(targetPixels.Length, width * height);
            if (total <= 0)
                return;

            radius = Mathf.Clamp(radius, 1, 32);

            var queued = new byte[total];
            var dist = new byte[total];
            var queue = new int[total];
            int head = 0;
            int tail = 0;

            void Enqueue(int idx, int d)
            {
                if (idx < 0 || idx >= total || queued[idx] != 0 || fallbackMask[idx] == 0)
                    return;

                queued[idx] = 1;
                dist[idx] = (byte)Mathf.Clamp(d, 0, 255);
                queue[tail++] = idx;
            }

            for (int y = 1; y < height - 1; y++)
            {
                int row = y * width;
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = row + x;
                    if (fallbackMask[idx] == 0)
                        continue;

                    bool boundary = false;
                    for (int oy = -1; oy <= 1 && !boundary; oy++)
                    {
                        int nrow = (y + oy) * width;
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0)
                                continue;

                            int nx = x + ox;
                            int ny = y + oy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            {
                                boundary = true;
                                break;
                            }

                            if (fallbackMask[nrow + nx] == 0)
                            {
                                boundary = true;
                                break;
                            }
                        }
                    }

                    if (boundary)
                        Enqueue(idx, 0);
                }
            }

            while (head < tail)
            {
                int idx = queue[head++];
                int d = dist[idx];
                if (d >= radius)
                    continue;

                int x = idx % width;
                int y = idx / width;
                for (int oy = -1; oy <= 1; oy++)
                {
                    int ny = y + oy;
                    if (ny <= 0 || ny >= height - 1)
                        continue;

                    int nrow = ny * width;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox;
                        if (nx <= 0 || nx >= width - 1)
                            continue;

                        Enqueue(nrow + nx, d + 1);
                    }
                }
            }

            for (int i = 0; i < total; i++)
            {
                if (fallbackMask[i] == 0)
                    continue;

                int x = i % width;
                int y = i / width;
                int d = queued[i] != 0 ? dist[i] : radius;

                float baseT = Mathf.Clamp01(d / Mathf.Max(1.0f, radius));

                // Warp + inward erosion. This actually eats the edge instead of just skipping some pixels.
                float warpNoise = HashFractalFallbackSprayV3LikeAdapted(x, y, 19);
                float erodeNoise = HashFractalFallbackSprayV3LikeAdapted(x, y, 53);
                float dustNoise = HashFractalFallbackSprayV3LikeAdapted(x, y, 97);
                float detailNoise = Hash01FallbackSprayV3LikeAdapted(x, y, 131);
                float alphaNoise = Hash01FallbackSprayV3LikeAdapted(x, y, 173);

                float warpPixels = (warpNoise - 0.5f) * 2.0f * TerrainSoftwareFallbackStructureSprayWarpPixelsV3LikeAdapted;
                float erodePixels = (1.0f - erodeNoise) * TerrainSoftwareFallbackStructureSprayErodePixelsV3LikeAdapted * (1.0f - baseT);
                float localDistance = d + warpPixels - erodePixels;
                float t = Mathf.Clamp01(localDistance / Mathf.Max(1.0f, radius));

                float feather = SmoothStep01LikeAdapted(t);

                // Clustered dust threshold: many holes near the edge, almost none inside.
                float clusterField = dustNoise * 0.75f + detailNoise * 0.25f;
                float clusterThreshold = Mathf.Lerp(
                    TerrainSoftwareFallbackStructureSprayThresholdEdgeV3LikeAdapted,
                    TerrainSoftwareFallbackStructureSprayThresholdCenterV3LikeAdapted,
                    feather);

                if (clusterField < clusterThreshold)
                    continue;

                float keep = Mathf.Clamp01((clusterField - clusterThreshold) / Mathf.Max(0.0001f, 1.0f - clusterThreshold));
                keep = SmoothStep01LikeAdapted(keep);

                float noisyAlpha = Mathf.Lerp(0.78f, 1.18f, alphaNoise);
                float oldAlpha = fallbackAlpha[i] * (1.0f / 255.0f);
                float a = Mathf.Clamp01(oldAlpha * feather * Mathf.Lerp(TerrainSoftwareFallbackStructureSprayMinAlphaV3LikeAdapted, 1.0f, keep) * noisyAlpha);

                if (a <= 0.0f)
                    continue;

                Color32 dst = targetPixels[i];
                Color32 src = fallbackPixels[i];

                targetPixels[i] = new Color32(
                    ToByteRoundClampLikeOriginal(dst.r + (src.r - dst.r) * a),
                    ToByteRoundClampLikeOriginal(dst.g + (src.g - dst.g) * a),
                    ToByteRoundClampLikeOriginal(dst.b + (src.b - dst.b) * a),
                    255);
            }
        }

        private static void BleedChunkTextureEdgesLikeAdapted(Color32[] pixels, int width, int height, int borderPixels)
        {
            if (pixels == null || pixels.Length == 0 || width <= 1 || height <= 1)
                return;

            int padX = Mathf.Clamp(borderPixels, 1, Mathf.Max(1, (width - 1) / 2));
            int padY = Mathf.Clamp(borderPixels, 1, Mathf.Max(1, (height - 1) / 2));

            for (int y = 0; y < height; y++)
            {
                Color32 leftSource = pixels[y * width + padX];
                for (int x = 0; x < padX; x++)
                    pixels[y * width + x] = leftSource;

                Color32 rightSource = pixels[y * width + (width - 1 - padX)];
                for (int x = 0; x < padX; x++)
                    pixels[y * width + (width - 1 - x)] = rightSource;
            }

            for (int x = 0; x < width; x++)
            {
                Color32 topSource = pixels[padY * width + x];
                for (int y = 0; y < padY; y++)
                    pixels[y * width + x] = topSource;

                Color32 bottomSource = pixels[(height - 1 - padY) * width + x];
                for (int y = 0; y < padY; y++)
                    pixels[(height - 1 - y) * width + x] = bottomSource;
            }
        }
        private static void CloseBaseCoverageGapsLikeOriginal(Color32[] pixels, byte[] coverage, int width, int height)
        {
            if (pixels == null || coverage == null || pixels.Length == 0 || coverage.Length != pixels.Length || width <= 1 || height <= 1)
                return;

            int total = width * height;
            if (total <= 0 || total > pixels.Length)
                return;

            // MIDDLE_PIXEL_PARALLEL_NO_PNG_V11:
            // The old version scanned the whole chunk again and again until holes closed.
            // This keeps the same purpose (fill only pixels not written by the base stage), but expands
            // from already-covered pixels with a queue. Cost becomes O(width*height), not O(width*height*passes).
            var queued = new byte[total];
            var queue = new int[total];
            int head = 0;
            int tail = 0;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int idx = row + x;
                    if (coverage[idx] != 0)
                        continue;

                    if (!HasCoveredNeighbor8LikeOriginal(coverage, width, height, x, y))
                        continue;

                    queued[idx] = 1;
                    queue[tail++] = idx;
                }
            }

            while (head < tail)
            {
                int idx = queue[head++];
                if (idx < 0 || idx >= total || coverage[idx] != 0)
                    continue;

                int x = idx % width;
                int y = idx / width;

                int sumR = 0;
                int sumG = 0;
                int sumB = 0;
                int count = 0;

                for (int oy = -1; oy <= 1; oy++)
                {
                    int ny = y + oy;
                    if (ny < 0 || ny >= height)
                        continue;

                    int nrow = ny * width;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (ox == 0 && oy == 0)
                            continue;

                        int nx = x + ox;
                        if (nx < 0 || nx >= width)
                            continue;

                        int nidx = nrow + nx;
                        if (coverage[nidx] == 0)
                            continue;

                        Color32 c = pixels[nidx];
                        sumR += c.r;
                        sumG += c.g;
                        sumB += c.b;
                        count++;
                    }
                }

                if (count <= 0)
                    continue;

                pixels[idx] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(sumR / (float)count), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(sumG / (float)count), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(sumB / (float)count), 0, 255),
                    255);
                coverage[idx] = 255;

                for (int oy = -1; oy <= 1; oy++)
                {
                    int ny = y + oy;
                    if (ny < 0 || ny >= height)
                        continue;

                    int nrow = ny * width;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (ox == 0 && oy == 0)
                            continue;

                        int nx = x + ox;
                        if (nx < 0 || nx >= width)
                            continue;

                        int nidx = nrow + nx;
                        if (coverage[nidx] != 0 || queued[nidx] != 0)
                            continue;

                        queued[nidx] = 1;
                        queue[tail++] = nidx;
                    }
                }
            }
        }

        private static void SoftenBaseTileTransitionsLikeAdapted(Color32[] pixels, byte[] tileIds, int width, int height, int radius, int passes, float strength)
        {
            if (pixels == null || tileIds == null || pixels.Length == 0 || tileIds.Length != pixels.Length || width <= 2 || height <= 2 || radius <= 0 || passes <= 0 || strength <= 0.0f)
                return;

            int total = width * height;
            if (total <= 0 || total > pixels.Length)
                return;

            strength = Mathf.Clamp01(strength);
            radius = Mathf.Clamp(radius, 1, 32);
            passes = Mathf.Clamp(passes, 1, 16);

            var activeMask = new byte[total];
            var active = new int[total];
            int activeCount = 0;

            for (int y = 1; y < height - 1; y++)
            {
                int row = y * width;
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = row + x;
                    byte tile = tileIds[idx];
                    if (tile == 255)
                        continue;

                    if ((tileIds[idx - 1] != 255 && tileIds[idx - 1] != tile) ||
                        (tileIds[idx + 1] != 255 && tileIds[idx + 1] != tile) ||
                        (tileIds[idx - width] != 255 && tileIds[idx - width] != tile) ||
                        (tileIds[idx + width] != 255 && tileIds[idx + width] != tile))
                    {
                        AddBaseTileSoftBlendPixelLikeAdapted(activeMask, active, ref activeCount, idx);
                    }
                }
            }

            int frontierStart = 0;
            int frontierEnd = activeCount;
            for (int r = 0; r < radius && frontierStart < frontierEnd; r++)
            {
                int oldEnd = frontierEnd;
                for (int i = frontierStart; i < oldEnd; i++)
                {
                    int idx = active[i];
                    int x = idx % width;
                    int y = idx / width;

                    if (x > 1) AddBaseTileSoftBlendPixelLikeAdapted(activeMask, active, ref activeCount, idx - 1);
                    if (x < width - 2) AddBaseTileSoftBlendPixelLikeAdapted(activeMask, active, ref activeCount, idx + 1);
                    if (y > 1) AddBaseTileSoftBlendPixelLikeAdapted(activeMask, active, ref activeCount, idx - width);
                    if (y < height - 2) AddBaseTileSoftBlendPixelLikeAdapted(activeMask, active, ref activeCount, idx + width);
                }

                frontierStart = oldEnd;
                frontierEnd = activeCount;
            }

            if (activeCount <= 0)
                return;

            var source = new Color32[pixels.Length];
            var destination = new Color32[pixels.Length];
            Array.Copy(pixels, source, pixels.Length);
            Array.Copy(pixels, destination, pixels.Length);

            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < activeCount; i++)
                {
                    int idx = active[i];
                    int x = idx % width;
                    int y = idx / width;
                    if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
                        continue;

                    Color32 center = source[idx];

                    int sumR = center.r * 8;
                    int sumG = center.g * 8;
                    int sumB = center.b * 8;
                    int weight = 8;

                    int rowUp = idx - width;
                    int rowDn = idx + width;

                    AccumulateSoftBlendSampleLikeAdapted(source[idx - 1], 4, ref sumR, ref sumG, ref sumB, ref weight);
                    AccumulateSoftBlendSampleLikeAdapted(source[idx + 1], 4, ref sumR, ref sumG, ref sumB, ref weight);
                    AccumulateSoftBlendSampleLikeAdapted(source[rowUp], 4, ref sumR, ref sumG, ref sumB, ref weight);
                    AccumulateSoftBlendSampleLikeAdapted(source[rowDn], 4, ref sumR, ref sumG, ref sumB, ref weight);

                    AccumulateSoftBlendSampleLikeAdapted(source[rowUp - 1], 2, ref sumR, ref sumG, ref sumB, ref weight);
                    AccumulateSoftBlendSampleLikeAdapted(source[rowUp + 1], 2, ref sumR, ref sumG, ref sumB, ref weight);
                    AccumulateSoftBlendSampleLikeAdapted(source[rowDn - 1], 2, ref sumR, ref sumG, ref sumB, ref weight);
                    AccumulateSoftBlendSampleLikeAdapted(source[rowDn + 1], 2, ref sumR, ref sumG, ref sumB, ref weight);

                    float blurR = sumR / (float)weight;
                    float blurG = sumG / (float)weight;
                    float blurB = sumB / (float)weight;

                    destination[idx] = new Color32(
                        ToByteRoundClampLikeOriginal(center.r + (blurR - center.r) * strength),
                        ToByteRoundClampLikeOriginal(center.g + (blurG - center.g) * strength),
                        ToByteRoundClampLikeOriginal(center.b + (blurB - center.b) * strength),
                        255);
                }

                var temp = source;
                source = destination;
                destination = temp;
            }

            for (int i = 0; i < activeCount; i++)
            {
                int idx = active[i];
                if (idx >= 0 && idx < pixels.Length)
                    pixels[idx] = source[idx];
            }
        }

        private static void AddBaseTileSoftBlendPixelLikeAdapted(byte[] mask, int[] active, ref int activeCount, int idx)
        {
            if (idx < 0 || idx >= mask.Length || mask[idx] != 0 || activeCount >= active.Length)
                return;

            mask[idx] = 1;
            active[activeCount++] = idx;
        }

        private static void AccumulateSoftBlendSampleLikeAdapted(Color32 c, int sampleWeight, ref int sumR, ref int sumG, ref int sumB, ref int weight)
        {
            sumR += c.r * sampleWeight;
            sumG += c.g * sampleWeight;
            sumB += c.b * sampleWeight;
            weight += sampleWeight;
        }

        private static void SoftenBaseLayerTransitionsLikeAdapted(Color32[] pixels, int width, int height, int passes, float strength)
        {
            if (pixels == null || pixels.Length == 0 || width <= 2 || height <= 2 || passes <= 0 || strength <= 0.0f)
                return;

            strength = Mathf.Clamp01(strength);
            var source = new Color32[pixels.Length];
            var destination = new Color32[pixels.Length];
            Array.Copy(pixels, source, pixels.Length);

            for (int pass = 0; pass < passes; pass++)
            {
                Array.Copy(source, destination, source.Length);

                for (int y = 1; y < height - 1; y++)
                {
                    int row = y * width;
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = row + x;
                        Color32 center = source[idx];

                        int sumR = center.r * 4;
                        int sumG = center.g * 4;
                        int sumB = center.b * 4;
                        int weight = 4;
                        int maxDiff = 0;

                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int nrow = (y + oy) * width;
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0)
                                    continue;

                                int nidx = nrow + (x + ox);
                                Color32 sample = source[nidx];
                                int kernelWeight = (ox == 0 || oy == 0) ? 2 : 1;
                                sumR += sample.r * kernelWeight;
                                sumG += sample.g * kernelWeight;
                                sumB += sample.b * kernelWeight;
                                weight += kernelWeight;

                                int diff = Mathf.Max(Mathf.Abs(center.r - sample.r), Mathf.Max(Mathf.Abs(center.g - sample.g), Mathf.Abs(center.b - sample.b)));
                                if (diff > maxDiff)
                                    maxDiff = diff;
                            }
                        }

                        float blurR = sumR / (float)weight;
                        float blurG = sumG / (float)weight;
                        float blurB = sumB / (float)weight;

                        float adaptive = Mathf.Clamp01((maxDiff - TerrainSoftwareBaseSoftBlendThresholdLikeAdapted) / TerrainSoftwareBaseSoftBlendRangeLikeAdapted);
                        if (adaptive <= 0.0f)
                            continue;

                        float mix = adaptive * strength;
                        destination[idx] = new Color32(
                            ToByteRoundClampLikeOriginal(center.r + (blurR - center.r) * mix),
                            ToByteRoundClampLikeOriginal(center.g + (blurG - center.g) * mix),
                            ToByteRoundClampLikeOriginal(center.b + (blurB - center.b) * mix),
                            255);
                    }
                }

                var temp = source;
                source = destination;
                destination = temp;
            }

            Array.Copy(source, pixels, pixels.Length);
        }

        private static bool HasCoveredNeighbor8LikeOriginal(byte[] coverage, int width, int height, int x, int y)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                int ny = y + oy;
                if (ny < 0 || ny >= height)
                    continue;

                int row = ny * width;
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                        continue;

                    int nx = x + ox;
                    if (nx < 0 || nx >= width)
                        continue;

                    if (coverage[row + nx] != 0)
                        return true;
                }
            }

            return false;
        }
        private static void RasterizeTriangleSoftwareLikeOriginal(
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            byte[] tex44Protection,
            Vector2 pA,
            Vector2 pB,
            Vector2 pC,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            Vector2 crossA,
            Vector2 crossB,
            Vector2 crossC,
            Color32 colorA,
            Color32 colorB,
            Color32 colorC,
            bool isBaseStage,
            bool plainMode,
            int resolvedTile,
            bool preferStandaloneTex44,
            bool onlyIfUncovered,
            byte[] baseTileIds = null)
        {
            float area = EdgeFunctionLikeOriginal(pA, pB, pC);
            if (Mathf.Abs(area) < 0.0001f)
                return;

            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.x, Mathf.Min(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.x, Mathf.Max(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.y, Mathf.Min(pB.y, pC.y))), 0, region.HeightPixels - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.y, Mathf.Max(pB.y, pC.y))), 0, region.HeightPixels - 1);

            bool crossEnabled = !preferStandaloneTex44 && !((colorA.a > 200) && (colorB.a > 200) && (colorC.a > 200) && !plainMode);
            float invArea = 1.0f / area;
            const float inv255 = 1.0f / 255.0f;

            float edge0Dx = (pC.y - pB.y) * invArea;
            float edge0Dy = -(pC.x - pB.x) * invArea;
            float edge1Dx = (pA.y - pC.y) * invArea;
            float edge1Dy = -(pA.x - pC.x) * invArea;

            float startX = minX + 0.5f;
            float startY = minY + 0.5f;
            float rowW0 = ((startX - pB.x) * (pC.y - pB.y) - (startY - pB.y) * (pC.x - pB.x)) * invArea;
            float rowW1 = ((startX - pC.x) * (pA.y - pC.y) - (startY - pC.y) * (pA.x - pC.x)) * invArea;

            for (int y = minY; y <= maxY; y++)
            {
                float w0 = rowW0;
                float w1 = rowW1;

                int row = y * region.WidthPixels;
                for (int x = minX; x <= maxX; x++)
                {
                    float w2 = 1.0f - w0 - w1;
                    if (w0 >= TerrainSoftwareRasterToleranceLikeOriginal &&
                        w1 >= TerrainSoftwareRasterToleranceLikeOriginal &&
                        w2 >= TerrainSoftwareRasterToleranceLikeOriginal)
                    {
                        float uvx = uvA.x * w0 + uvB.x * w1 + uvC.x * w2;
                        float uvy = uvA.y * w0 + uvB.y * w1 + uvC.y * w2;
                        float lr = (colorA.r * w0 + colorB.r * w1 + colorC.r * w2) * inv255;
                        float lg = (colorA.g * w0 + colorB.g * w1 + colorC.g * w2) * inv255;
                        float lb = (colorA.b * w0 + colorB.b * w1 + colorC.b * w2) * inv255;
                        float la = (colorA.a * w0 + colorB.a * w1 + colorC.a * w2) * inv255;

                        float atlasR;
                        float atlasG;
                        float atlasB;
                        if (preferStandaloneTex44 && inputs != null && inputs.StandaloneTex44Pixels != null && inputs.StandaloneTex44Pixels.Length > 0)
                        {
                            float localUvX = WrapAtlasUvToSingleTileLikeAdapted(uvx);
                            float localUvY = WrapAtlasUvToSingleTileLikeAdapted(uvy);
                            SampleTextureBilinearRgbaFastLikeOriginal(inputs.StandaloneTex44Pixels, inputs.StandaloneTex44Width, inputs.StandaloneTex44Height, localUvX, localUvY, true, out atlasR, out atlasG, out atlasB, out _);
                        }
                        else
                        {
                            SampleTextureBilinearRgbaFastLikeOriginal(inputs.GroundPixels, inputs.GroundWidth, inputs.GroundHeight, uvx, uvy, false, out atlasR, out atlasG, out atlasB, out _);
                        }

                        float srcR = Clamp01FastLikeOriginal(atlasR * lr * 2.0f);
                        float srcG = Clamp01FastLikeOriginal(atlasG * lg * 2.0f);
                        float srcB = Clamp01FastLikeOriginal(atlasB * lb * 2.0f);

                        int pixelIndex = row + x;
                        if (isBaseStage)
                        {
                            if (!onlyIfUncovered || baseCoverage == null || baseCoverage[pixelIndex] == 0)
                            {
                                float baseAlpha = preferStandaloneTex44 ? Mathf.Max(la, TerrainSoftwareTex44RevealAlphaFloorLikeAdapted) : la;
                                if (baseAlpha >= TerrainSoftwareAlphaClipLikeOriginal)
                                {
                                    targetPixels[pixelIndex] = new Color32(
                                        ToByteRoundClampLikeOriginal(srcR * 255.0f),
                                        ToByteRoundClampLikeOriginal(srcG * 255.0f),
                                        ToByteRoundClampLikeOriginal(srcB * 255.0f),
                                        255);
                                    if (baseCoverage != null)
                                        baseCoverage[pixelIndex] = 255;
                                    if (baseTileIds != null && pixelIndex >= 0 && pixelIndex < baseTileIds.Length)
                                        baseTileIds[pixelIndex] = (byte)(resolvedTile & 63);
                                    if (preferStandaloneTex44 && tex44Protection != null)
                                        tex44Protection[pixelIndex] = 255;
                                }
                            }
                        }
                        else
                        {
                            float finalAlpha = preferStandaloneTex44 ? Mathf.Max(la, TerrainSoftwareTex44RevealAlphaFloorLikeAdapted) : la;
                            if (crossEnabled && inputs.CrossPixels != null && inputs.CrossPixels.Length > 0)
                            {
                                float crossUvX = crossA.x * w0 + crossB.x * w1 + crossC.x * w2;
                                float crossUvY = crossA.y * w0 + crossB.y * w1 + crossC.y * w2;
                                SampleTextureBilinearRgbaFastLikeOriginal(inputs.CrossPixels, inputs.CrossWidth, inputs.CrossHeight, crossUvX, crossUvY, true, out _, out _, out _, out float crossAValue);
                                finalAlpha = Clamp01FastLikeOriginal(crossAValue + finalAlpha - 0.5f);
                            }

                            if (!preferStandaloneTex44 && tex44Protection != null && tex44Protection[pixelIndex] != 0)
                                finalAlpha *= TerrainSoftwareTex44ProtectionOverlayAttenuationLikeAdapted;

                            if (finalAlpha >= TerrainSoftwareBaseOverlayAlphaClipLikeAdapted)
                            {
                                Color32 dst = targetPixels[pixelIndex];
                                float invA = 1.0f - finalAlpha;
                                targetPixels[pixelIndex] = new Color32(
                                    ToByteRoundClampLikeOriginal((srcR * finalAlpha + dst.r * inv255 * invA) * 255.0f),
                                    ToByteRoundClampLikeOriginal((srcG * finalAlpha + dst.g * inv255 * invA) * 255.0f),
                                    ToByteRoundClampLikeOriginal((srcB * finalAlpha + dst.b * inv255 * invA) * 255.0f),
                                    255);
                                if (baseTileIds != null && pixelIndex >= 0 && pixelIndex < baseTileIds.Length && finalAlpha >= TerrainSoftwareBaseOverlayTileIdAlphaLikeAdapted)
                                    baseTileIds[pixelIndex] = (byte)(resolvedTile & 63);
                                if (preferStandaloneTex44 && tex44Protection != null)
                                    tex44Protection[pixelIndex] = 255;
                            }
                        }
                    }

                    w0 += edge0Dx;
                    w1 += edge1Dx;
                }

                rowW0 += edge0Dy;
                rowW1 += edge1Dy;
            }
        }
        private static bool ShouldRevealStandaloneTex44InBakeLikeAdapted(BaseSurfaceTriangleDescriptorLikeAdapted descriptor, TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            if (inputs == null || inputs.StandaloneTex44Pixels == null || inputs.StandaloneTex44Pixels.Length == 0)
                return false;

            return descriptor.ResolvedTile == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.Tile == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.BaseTileA == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.BaseTileB == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.BaseTileC == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.ExTileA == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.ExTileB == TerrainSoftwareTex44RevealTileIdLikeAdapted ||
                   descriptor.ExTileC == TerrainSoftwareTex44RevealTileIdLikeAdapted;
        }

        private static float WrapAtlasUvToSingleTileLikeAdapted(float uv)
        {
            float v = uv * 8.0f;
            v -= Mathf.Floor(v);
            if (v < 0.0f)
                v += 1.0f;
            return v;
        }

        private static void RasterizeFactureTriangleSoftwareLikeOriginal(
            TerrainSoftwareChunkRegionLikeOriginal region,
            Color32[] targetPixels,
            Vector2 pA,
            Vector2 pB,
            Vector2 pC,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            Color32 colorA,
            Color32 colorB,
            Color32 colorC,
            Color32[] diffusePixels,
            int diffuseWidth,
            int diffuseHeight,
            Color32[] normalPixels,
            int normalWidth,
            int normalHeight,
            bool isFallbackHoleClose,
            Color32[] fallbackStructurePixels,
            byte[] fallbackStructureMask,
            byte[] fallbackStructureAlpha)
        {
            float area = EdgeFunctionLikeOriginal(pA, pB, pC);
            if (Mathf.Abs(area) < 0.0001f)
                return;

            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.x, Mathf.Min(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.x, Mathf.Max(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.y, Mathf.Min(pB.y, pC.y))), 0, region.HeightPixels - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.y, Mathf.Max(pB.y, pC.y))), 0, region.HeightPixels - 1);
            bool useDot3 = normalPixels != null && normalPixels.Length > 0 && normalWidth > 0 && normalHeight > 0;

            float invArea = 1.0f / area;
            const float inv255 = 1.0f / 255.0f;
            float alphaRef = TerrainSoftwareFactureAllSoftEdgesV4LikeAdapted
                ? TerrainSoftwareFactureAlphaRefV4LikeAdapted
                : FactureAlphaRefByteLikeOriginal * inv255;

            // V5: do not fade by geometric triangle edges. That created a visible triangle grid.
            // Quality/facture softness must come only from interpolated coverage alpha, shared by neighbouring triangles.

            float edge0Dx = (pC.y - pB.y) * invArea;
            float edge0Dy = -(pC.x - pB.x) * invArea;
            float edge1Dx = (pA.y - pC.y) * invArea;
            float edge1Dy = -(pA.x - pC.x) * invArea;

            float startX = minX + 0.5f;
            float startY = minY + 0.5f;
            float rowW0 = ((startX - pB.x) * (pC.y - pB.y) - (startY - pB.y) * (pC.x - pB.x)) * invArea;
            float rowW1 = ((startX - pC.x) * (pA.y - pC.y) - (startY - pC.y) * (pA.x - pC.x)) * invArea;

            for (int y = minY; y <= maxY; y++)
            {
                float w0 = rowW0;
                float w1 = rowW1;
                int row = y * region.WidthPixels;

                for (int x = minX; x <= maxX; x++)
                {
                    float w2 = 1.0f - w0 - w1;
                    if (w0 >= TerrainSoftwareRasterToleranceLikeOriginal &&
                        w1 >= TerrainSoftwareRasterToleranceLikeOriginal &&
                        w2 >= TerrainSoftwareRasterToleranceLikeOriginal)
                    {
                        float rawAlpha = Clamp01FastLikeOriginal((colorA.a * w0 + colorB.a * w1 + colorC.a * w2) * inv255);
                        float alpha = TerrainSoftwareFactureNoTriangleEdgeFadeV5LikeAdapted
                            ? ComputeFactureCoverageAlphaV5LikeAdapted(rawAlpha)
                            : rawAlpha;

                        if (alpha > Mathf.Max(alphaRef, TerrainSoftwareFactureMinVisibleAlphaV4LikeAdapted))
                        {
                            float uvx = uvA.x * w0 + uvB.x * w1 + uvC.x * w2;
                            float uvy = uvA.y * w0 + uvB.y * w1 + uvC.y * w2;
                            SampleTextureBilinearRgbaFastLikeOriginal(diffusePixels, diffuseWidth, diffuseHeight, uvx, uvy, true, out float diffuseR, out float diffuseG, out float diffuseB, out _);

                            float sourceR;
                            float sourceG;
                            float sourceB;

                            if (useDot3)
                            {
                                SampleTextureBilinearRgbaFastLikeOriginal(normalPixels, normalWidth, normalHeight, uvx, uvy, true, out float normalR, out float normalG, out float normalB, out _);

                                float nmx = normalR * 2.0f - 1.0f;
                                float nmy = normalG * 2.0f - 1.0f;
                                float nmz = normalB * 2.0f - 1.0f;
                                NormalizeVector3FastLikeOriginal(ref nmx, ref nmy, ref nmz);

                                float diffX = (colorA.r * w0 + colorB.r * w1 + colorC.r * w2) * inv255;
                                float diffY = (colorA.g * w0 + colorB.g * w1 + colorC.g * w2) * inv255;
                                float diffZ = (colorA.b * w0 + colorB.b * w1 + colorC.b * w2) * inv255;
                                diffX = diffX * 2.0f - 1.0f;
                                diffY = diffY * 2.0f - 1.0f;
                                diffZ = diffZ * 2.0f - 1.0f;
                                NormalizeVector3FastLikeOriginal(ref diffX, ref diffY, ref diffZ);

                                float dot3 = Clamp01FastLikeOriginal(nmx * diffX + nmy * diffY + nmz * diffZ);
                                sourceR = 0.5f + (diffuseR * dot3 - 0.5f) * alpha;
                                sourceG = 0.5f + (diffuseG * dot3 - 0.5f) * alpha;
                                sourceB = 0.5f + (diffuseB * dot3 - 0.5f) * alpha;
                            }
                            else
                            {
                                sourceR = 0.5f + (diffuseR - 0.5f) * alpha;
                                sourceG = 0.5f + (diffuseG - 0.5f) * alpha;
                                sourceB = 0.5f + (diffuseB - 0.5f) * alpha;
                            }

                            int pixelIndex = row + x;
                            Color32 dst = targetPixels[pixelIndex];

                            Color32 composed = new Color32(
                                ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(2.0f * sourceR * (dst.r * inv255)) * 255.0f),
                                ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(2.0f * sourceG * (dst.g * inv255)) * 255.0f),
                                ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(2.0f * sourceB * (dst.b * inv255)) * 255.0f),
                                255);

                            if (isFallbackHoleClose && TerrainSoftwareFallbackStructureFeatherV1LikeAdapted &&
                                fallbackStructurePixels != null &&
                                fallbackStructureMask != null &&
                                fallbackStructureAlpha != null &&
                                pixelIndex >= 0 &&
                                pixelIndex < fallbackStructurePixels.Length &&
                                pixelIndex < fallbackStructureMask.Length &&
                                pixelIndex < fallbackStructureAlpha.Length)
                            {
                                byte alphaByte = ToByteRoundClampLikeOriginal(alpha * 255.0f);
                                if (alphaByte >= fallbackStructureAlpha[pixelIndex])
                                {
                                    fallbackStructurePixels[pixelIndex] = composed;
                                    fallbackStructureMask[pixelIndex] = 255;
                                    fallbackStructureAlpha[pixelIndex] = alphaByte;
                                }
                            }
                            else
                            {
                                targetPixels[pixelIndex] = composed;
                            }
                        }
                    }

                    w0 += edge0Dx;
                    w1 += edge1Dx;
                }

                rowW0 += edge0Dy;
                rowW1 += edge1Dy;
            }
        }

        private static void NormalizeVector3FastLikeOriginal(ref float x, ref float y, ref float z)
        {
            float sqr = x * x + y * y + z * z;
            if (sqr <= 1e-6f)
            {
                x = 0.0f;
                y = 0.0f;
                z = 1.0f;
                return;
            }

            float invLen = 1.0f / Mathf.Sqrt(sqr);
            x *= invLen;
            y *= invLen;
            z *= invLen;
        }


        private static float EdgeFunctionLikeOriginal(Vector2 a, Vector2 b, Vector2 p)
        {
            return (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);
        }

        private TerrainSoftwareFactureBakeCacheEntryLikeOriginal GetOrCreateFactureBakeCacheEntryLikeOriginal(TerrainSoftwareBakeInputsLikeOriginal inputs, int bucketTextureId)
        {
            if (inputs == null)
                return null;

            bucketTextureId = Mathf.Clamp(bucketTextureId, 0, 255);

            // Worker-thread safe fast path: after PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal
            // this reads only immutable arrays, no Dictionary access and no Unity texture loading.
            if (inputs.FactureCacheInitialized != null &&
                inputs.FactureCacheArray != null &&
                bucketTextureId >= 0 && bucketTextureId < inputs.FactureCacheArray.Length &&
                inputs.FactureCacheInitialized[bucketTextureId])
            {
                return inputs.FactureCacheArray[bucketTextureId];
            }

            lock (s_terrainSoftwareFactureCacheBuildLockLikeOriginal)
            {
                if (inputs.FactureCacheInitialized != null &&
                    inputs.FactureCacheArray != null &&
                    bucketTextureId >= 0 && bucketTextureId < inputs.FactureCacheArray.Length &&
                    inputs.FactureCacheInitialized[bucketTextureId])
                {
                    return inputs.FactureCacheArray[bucketTextureId];
                }

                if (inputs.FactureCache.TryGetValue(bucketTextureId, out TerrainSoftwareFactureBakeCacheEntryLikeOriginal cached))
                {
                    if (inputs.FactureCacheArray != null && bucketTextureId < inputs.FactureCacheArray.Length)
                        inputs.FactureCacheArray[bucketTextureId] = cached;
                    if (inputs.FactureCacheInitialized != null && bucketTextureId < inputs.FactureCacheInitialized.Length)
                        inputs.FactureCacheInitialized[bucketTextureId] = true;
                    return cached;
                }

                var entry = new TerrainSoftwareFactureBakeCacheEntryLikeOriginal
                {
                    BucketTextureId = bucketTextureId
                };

                Texture2D plainDiffuse = TryLoadFactureTextureLikeAdapted(bucketTextureId, FactureTextureVariantLikeAdapted.PlainDiffuse, out _);
                if (plainDiffuse == null && bucketTextureId != 0)
                    plainDiffuse = TryLoadFactureTextureLikeAdapted(0, FactureTextureVariantLikeAdapted.PlainDiffuse, out _);
                FillFactureBakeTexturePixelsLikeOriginal(plainDiffuse, out entry.PlainDiffusePixels, out entry.PlainDiffuseWidth, out entry.PlainDiffuseHeight);

                Texture2D dot3Diffuse = TryLoadFactureTextureLikeAdapted(bucketTextureId, FactureTextureVariantLikeAdapted.Dot3Diffuse, out _);
                if (dot3Diffuse == null && bucketTextureId != 0)
                    dot3Diffuse = TryLoadFactureTextureLikeAdapted(0, FactureTextureVariantLikeAdapted.Dot3Diffuse, out _);
                FillFactureBakeTexturePixelsLikeOriginal(dot3Diffuse, out entry.Dot3DiffusePixels, out entry.Dot3DiffuseWidth, out entry.Dot3DiffuseHeight);

                Texture2D normal = TryBuildFactureNormalMapLikeAdapted(bucketTextureId, out _);
                FillFactureBakeTexturePixelsLikeOriginal(normal, out entry.NormalPixels, out entry.NormalWidth, out entry.NormalHeight);

                if ((entry.PlainDiffusePixels == null || entry.PlainDiffusePixels.Length == 0) &&
                    (entry.Dot3DiffusePixels == null || entry.Dot3DiffusePixels.Length == 0))
                {
                    entry = null;
                }

                inputs.FactureCache[bucketTextureId] = entry;
                if (inputs.FactureCacheArray != null && bucketTextureId < inputs.FactureCacheArray.Length)
                    inputs.FactureCacheArray[bucketTextureId] = entry;
                if (inputs.FactureCacheInitialized != null && bucketTextureId < inputs.FactureCacheInitialized.Length)
                    inputs.FactureCacheInitialized[bucketTextureId] = true;
                return entry;
            }
        }

        private static void FillFactureBakeTexturePixelsLikeOriginal(
            Texture2D texture,
            out Color32[] pixels,
            out int width,
            out int height)
        {
            pixels = null;
            width = 0;
            height = 0;
            if (texture == null)
                return;

            width = texture.width;
            height = texture.height;
            if (width <= 0 || height <= 0)
                return;

            pixels = texture.GetPixels32();
        }

        private static Color32 SampleTexturePointLikeOriginal(Color32[] pixels, int width, int height, Vector2 uv, bool repeat)
        {
            if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
                return new Color32(255, 255, 255, 255);

            float u = repeat ? Mathf.Repeat(uv.x, 1.0f) : Mathf.Clamp01(uv.x);
            float v = repeat ? Mathf.Repeat(uv.y, 1.0f) : Mathf.Clamp01(uv.y);
            int x = Mathf.Clamp(Mathf.FloorToInt(u * width), 0, width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(v * height), 0, height - 1);
            return pixels[y * width + x];
        }
        private static Color SampleTextureBilinearLikeOriginal(Color32[] pixels, int width, int height, Vector2 uv, bool repeat)
        {
            SampleTextureBilinearRgbaFastLikeOriginal(pixels, width, height, uv.x, uv.y, repeat, out float r, out float g, out float b, out float a);
            return new Color(r, g, b, a);
        }

        private static void SampleTextureBilinearRgbaFastLikeOriginal(
            Color32[] pixels,
            int width,
            int height,
            float u,
            float v,
            bool repeat,
            out float r,
            out float g,
            out float b,
            out float a)
        {
            if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
            {
                r = 1.0f;
                g = 1.0f;
                b = 1.0f;
                a = 1.0f;
                return;
            }

            if (repeat)
            {
                u -= Mathf.Floor(u);
                v -= Mathf.Floor(v);
            }
            else
            {
                u = Clamp01FastLikeOriginal(u);
                v = Clamp01FastLikeOriginal(v);
            }

            float fx = u * width - 0.5f;
            float fy = v * height - 0.5f;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0;
            float ty = fy - y0;

            int x1 = x0 + 1;
            int y1 = y0 + 1;

            if (repeat)
            {
                x0 = WrapIntFastLikeOriginal(x0, width);
                x1 = WrapIntFastLikeOriginal(x1, width);
                y0 = WrapIntFastLikeOriginal(y0, height);
                y1 = WrapIntFastLikeOriginal(y1, height);
            }
            else
            {
                x0 = ClampIntFastLikeOriginal(x0, 0, width - 1);
                x1 = ClampIntFastLikeOriginal(x1, 0, width - 1);
                y0 = ClampIntFastLikeOriginal(y0, 0, height - 1);
                y1 = ClampIntFastLikeOriginal(y1, 0, height - 1);
            }

            Color32 c00 = pixels[y0 * width + x0];
            Color32 c10 = pixels[y0 * width + x1];
            Color32 c01 = pixels[y1 * width + x0];
            Color32 c11 = pixels[y1 * width + x1];

            float ix = 1.0f - tx;
            float iy = 1.0f - ty;
            float w00 = ix * iy;
            float w10 = tx * iy;
            float w01 = ix * ty;
            float w11 = tx * ty;
            const float inv255 = 1.0f / 255.0f;

            r = (c00.r * w00 + c10.r * w10 + c01.r * w01 + c11.r * w11) * inv255;
            g = (c00.g * w00 + c10.g * w10 + c01.g * w01 + c11.g * w11) * inv255;
            b = (c00.b * w00 + c10.b * w10 + c01.b * w01 + c11.b * w11) * inv255;
            a = (c00.a * w00 + c10.a * w10 + c01.a * w01 + c11.a * w11) * inv255;
        }

        private static int WrapIntFastLikeOriginal(int value, int size)
        {
            if (size <= 0)
                return 0;

            int result = value % size;
            return result < 0 ? result + size : result;
        }

        private static int ClampIntFastLikeOriginal(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float ComputeFactureCoverageAlphaV5LikeAdapted(float rawAlpha)
        {
            rawAlpha = Clamp01FastLikeOriginal(rawAlpha);
            if (!TerrainSoftwareFactureAllSoftEdgesV4LikeAdapted)
                return rawAlpha;

            float softStart = Mathf.Max(TerrainSoftwareFactureCoverageSoftStartV5LikeAdapted, 1.0f / 255.0f);
            float edgeFade = SmoothStep01LikeAdapted(rawAlpha / softStart);
            return Clamp01FastLikeOriginal(rawAlpha * edgeFade);
        }

        private static float SmoothStep01LikeAdapted(float value)
        {
            value = Clamp01FastLikeOriginal(value);
            return value * value * (3.0f - 2.0f * value);
        }

        private static float Clamp01FastLikeOriginal(float value)
        {
            if (value <= 0.0f)
                return 0.0f;
            if (value >= 1.0f)
                return 1.0f;
            return value;
        }

        private static byte ToByteRoundClampLikeOriginal(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
        }


        private static Color SampleTexturePixelAsColorLikeOriginal(Color32[] pixels, int width, int height, int x, int y, bool repeat)
        {
            if (repeat)
            {
                x = ((x % width) + width) % width;
                y = ((y % height) + height) % height;
            }
            else
            {
                x = Mathf.Clamp(x, 0, width - 1);
                y = Mathf.Clamp(y, 0, height - 1);
            }

            return pixels[y * width + x];
        }

        private static Mesh BuildProjectedChunkMeshSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            out Bounds chunkBounds)
        {
            chunkBounds = new Bounds(Vector3.zero, Vector3.zero);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var indexByKey = new Dictionary<long, int>();
            bool hasBounds = false;

            for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
            {
                for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                {
                    OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);

                    int i0 = GetProjectedChunkVertexIndexSoftwareLikeOriginal(indexByKey, vertices, uvs, map, kernel, region, cellX, cellY, ref chunkBounds, ref hasBounds);
                    int i1 = GetProjectedChunkVertexIndexSoftwareLikeOriginal(indexByKey, vertices, uvs, map, kernel, region, cellX + 1, cellY, ref chunkBounds, ref hasBounds);
                    int i2 = GetProjectedChunkVertexIndexSoftwareLikeOriginal(indexByKey, vertices, uvs, map, kernel, region, cellX, cellY + 1, ref chunkBounds, ref hasBounds);
                    int i3 = GetProjectedChunkVertexIndexSoftwareLikeOriginal(indexByKey, vertices, uvs, map, kernel, region, cellX + 1, cellY + 1, ref chunkBounds, ref hasBounds);

                    if (cell.FirstC == cell.V2)
                    {
                        triangles.Add(i0);
                        triangles.Add(i1);
                        triangles.Add(i2);
                        triangles.Add(i2);
                        triangles.Add(i1);
                        triangles.Add(i3);
                    }
                    else
                    {
                        triangles.Add(i0);
                        triangles.Add(i1);
                        triangles.Add(i3);
                        triangles.Add(i0);
                        triangles.Add(i3);
                        triangles.Add(i2);
                    }
                }
            }

            if (vertices.Count == 0 || triangles.Count == 0)
                return null;

            var mesh = new Mesh { name = $"TerrainSoftwareChunkMesh_{region.MinCellX}_{region.MinCellY}" };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            chunkBounds = mesh.bounds;
            return mesh;
        }

        private static int GetProjectedChunkVertexIndexSoftwareLikeOriginal(
            Dictionary<long, int> indexByKey,
            List<Vector3> vertices,
            List<Vector2> uvs,
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            int vertexX,
            int vertexY,
            ref Bounds bounds,
            ref bool hasBounds)
        {
            long key = ((long)vertexY << 32) | (uint)vertexX;
            if (indexByKey.TryGetValue(key, out int existing))
                return existing;

            int vertexIndex = vertexY * map.VertInLine + vertexX;
            float rawX = GetVertexRawXLikeOriginal(kernel.BackingStepXWorld, vertexX);
            float rawZ = GetVertexRawZLikeOriginal(kernel.BackingStepZWorld, kernel.BackingOddColumnOffsetZWorld, vertexX, vertexY);
            Vector3 world = CreateKernelWorldVertexLikeOriginal(map, kernel, vertexIndex, rawX, rawZ);

            int index = vertices.Count;
            vertices.Add(world);

            float u = Mathf.Clamp01((world.x - region.FootprintBounds.min.x) / Mathf.Max(0.001f, region.FootprintBounds.size.x));
            float v = Mathf.Clamp01((world.z - region.FootprintBounds.min.z) / Mathf.Max(0.001f, region.FootprintBounds.size.z));
            uvs.Add(new Vector2(u, v));

            indexByKey[key] = index;

            if (!hasBounds)
            {
                bounds = new Bounds(world, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(world);
            }

            return index;
        }

        private static void ApplyFinalTerrainColorPolishMaterialV4LikeAdapted(Material mat)
        {
            if (mat == null || !TerrainSoftwareFinalColorPolishV1LikeAdapted)
                return;

            if (mat.HasProperty("_C2Warm"))
            {
                mat.SetVector("_C2Warm", new Vector4(
                    TerrainSoftwareFinalColorPolishWarmR_V1LikeAdapted,
                    TerrainSoftwareFinalColorPolishWarmG_V1LikeAdapted,
                    TerrainSoftwareFinalColorPolishWarmB_V1LikeAdapted,
                    1.0f));
            }

            if (mat.HasProperty("_C2Saturation"))
                mat.SetFloat("_C2Saturation", TerrainSoftwareFinalColorPolishSaturationV1LikeAdapted);

            if (mat.HasProperty("_C2Contrast"))
                mat.SetFloat("_C2Contrast", TerrainSoftwareFinalColorPolishContrastV1LikeAdapted);

            if (mat.HasProperty("_C2ShadowWarm"))
            {
                mat.SetVector("_C2ShadowWarm", new Vector4(
                    TerrainSoftwareFinalColorPolishShadowWarmR_V1LikeAdapted,
                    TerrainSoftwareFinalColorPolishShadowWarmG_V1LikeAdapted,
                    TerrainSoftwareFinalColorPolishShadowCoolB_V1LikeAdapted,
                    0.60f));
            }
        }

        private Material CreateSoftwareBakedTerrainChunkMaterialLikeOriginal(Texture2D bakedTexture, int chunkX, int chunkY)
        {
            if (bakedTexture == null)
                return null;

            Shader shader = Shader.Find("Cossacks2Bridge/TerrainFinalColorPolishV4")
                            ?? Shader.Find("Unlit/Texture")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("Standard");
            if (shader == null)
                return null;

            var mat = new Material(shader)
            {
                name = $"C2_TerrainSoftwareChunk_{chunkX:00}_{chunkY:00}",
                renderQueue = SurfaceBaseRenderQueueLikeAdapted
            };

            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", bakedTexture);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", bakedTexture);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            ApplyFinalTerrainColorPolishMaterialV4LikeAdapted(mat);

            return mat;
        }
    }
}
