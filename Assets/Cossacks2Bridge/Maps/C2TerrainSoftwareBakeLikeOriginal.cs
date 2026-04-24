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
        private const float TerrainSoftwareRoadLikeCoreAlphaLikeAdapted = 0.92f;
        private const float TerrainSoftwareRoadLikeEdgeBandLikeAdapted = 0.045f;
        private const float TerrainSoftwareOverlayEdgeBandLikeAdapted = 0.060f;
        private const string TerrainSoftwarePersistentCacheVersionLikeOriginal = "MIDPIX_PARALLEL_NO_PNG_V18B_QUALITY40_40px_quality";
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

            var totalSwV11 = global::System.Diagnostics.Stopwatch.StartNew();

            // MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX:
            // Keep the exact middle-project pixels[] raster formula, but remove PNG cache/encode/decode
            // and bake chunk pixel buffers on CPU worker threads. Unity Texture2D/Mesh/GameObject creation
            // still stays on the main thread after the parallel pixel phase.
            inputs.PersistentChunkCacheEnabled = false;
            inputs.PersistentChunkCacheDirectory = string.Empty;
            inputs.PersistentChunkCacheKey = string.Empty;

            // Warm all static map/material/random tables on main thread before worker threads start.
            // Several helper paths use static Dictionaries; they must not initialize during Parallel.For.
            _ = GetTerrainTextureTablesLikeOriginal();
            _ = GetFactureMaterialTablesLikeAdapted();
            _ = GetRandomTableLikeOriginal();

            var prewarmSwV11 = global::System.Diagnostics.Stopwatch.StartNew();
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

            UnityEngine.Debug.Log(
                $"[C2:REN] kernel=BuildStrictOldSurfaceSoftwareBakedChunksLikeOriginal mode=MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX " +
                $"rect=({kernel.MinCellX},{kernel.MinCellY})->({kernel.MaxCellXExclusive},{kernel.MaxCellYExclusive}) " +
                $"chunkCells={TerrainSoftwareChunkCellsLikeOriginal} pxPerCell={TerrainSoftwarePixelsPerCellLikeOriginal} " +
                $"jobs={jobCount} workers={Mathf.Max(1, Environment.ProcessorCount - 1)} " +
                $"rules='same middle pixels[] raster blend; no PNG cache; parallel chunk pixel buffers; main-thread Texture2D only'");

            int workerCount = Mathf.Max(1, Environment.ProcessorCount - 1);
            var options = new ParallelOptions { MaxDegreeOfParallelism = workerCount };

            var parallelSwV11 = global::System.Diagnostics.Stopwatch.StartNew();
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
                UnityEngine.Debug.LogWarning("[C2:REN][MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX] parallel bake failed, continuing with completed jobs where possible: " + ex.Message);
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
                        UnityEngine.Debug.LogWarning($"[C2:REN][MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX] chunk=({job.ChunkX},{job.ChunkY}) failed: {job.Error}");
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

            uploadSwV11.Stop();
            totalSwV11.Stop();

            UnityEngine.Debug.Log(
                $"[C2:REN] software baked chunks built={builtChunkCount}/{jobCount} failed={failedChunkCount} " +
                $"path=MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX cache=disabled png=disabled gapfill=queue raster=scalar upload=SetPixelData textureFilter=trilinear_mip_aniso16_bias-0.75f " +
                $"timingMs prewarm={prewarmSwV11.ElapsedMilliseconds} parallelPixels={parallelSwV11.ElapsedMilliseconds} uploadMeshTexture={uploadSwV11.ElapsedMilliseconds} total={totalSwV11.ElapsedMilliseconds}");
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

            UnityEngine.Debug.Log($"[C2:REN][MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX] facture texture cache prewarmed entries={inputs.FactureCache.Count} arrayReady=256.");
        }

        private Color32[] BakeTerrainChunkPixelsSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            var pixels = new Color32[region.WidthPixels * region.HeightPixels];
            var baseCoverage = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 255);

            List<FactureTriangleCopyDescriptorLikeAdapted> scratchFactureCopies =
                HasFactureLayerDataLikeOriginal(map)
                    ? new List<FactureTriangleCopyDescriptorLikeAdapted>(4)
                    : null;

            for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
            {
                for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                {
                    BakeTerrainCellSoftwareLikeOriginal(map, kernel, region, inputs, pixels, baseCoverage, cellX, cellY);
                }
            }

            CloseBaseCoverageGapsLikeOriginal(pixels, baseCoverage, region.WidthPixels, region.HeightPixels);

            if (scratchFactureCopies != null)
            {
                for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
                {
                    for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                    {
                        BakeTerrainCellFactureSoftwareLikeOriginal(map, kernel, region, inputs, pixels, cellX, cellY, scratchFactureCopies);
                    }
                }
            }

            BleedChunkTextureEdgesLikeAdapted(pixels, region.WidthPixels, region.HeightPixels, 3);
            return pixels;
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

            // V11: SetPixelData avoids the managed Color32[] -> Unity color conversion path used by SetPixels32.
            texture.SetPixelData(pixels, 0);
            texture.Apply(true, false);
            return texture;
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

            return result;
        }


        private static void PrepareTerrainSoftwarePersistentChunkCacheLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareBakeInputsLikeOriginal inputs)
        {
            if (inputs == null)
                return;

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
            // MIDDLE_PIXEL_PARALLEL_NO_PNG_V20_ROAD_PROTECT_COMPILE_FIX: PNG cache writes are disabled.
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
            var baseCoverage = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 255);

            List<FactureTriangleCopyDescriptorLikeAdapted> scratchFactureCopies =
                HasFactureLayerDataLikeOriginal(map)
                    ? new List<FactureTriangleCopyDescriptorLikeAdapted>(4)
                    : null;

            for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
            {
                for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                {
                    BakeTerrainCellSoftwareLikeOriginal(map, kernel, region, inputs, pixels, baseCoverage, cellX, cellY);
                }
            }

            CloseBaseCoverageGapsLikeOriginal(pixels, baseCoverage, region.WidthPixels, region.HeightPixels);

            if (scratchFactureCopies != null)
            {
                for (int cellY = region.MinCellY; cellY < region.MaxCellYExclusive; cellY++)
                {
                    for (int cellX = region.MinCellX; cellX < region.MaxCellXExclusive; cellX++)
                    {
                        BakeTerrainCellFactureSoftwareLikeOriginal(map, kernel, region, inputs, pixels, cellX, cellY, scratchFactureCopies);
                    }
                }
            }

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
            int cellX,
            int cellY)
        {
            OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);
            CellVertexPayloadLikeOriginal v0 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V0);
            CellVertexPayloadLikeOriginal v1 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V1);
            CellVertexPayloadLikeOriginal v2 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V2);
            CellVertexPayloadLikeOriginal v3 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V3);

            if (TryBuildCellStageLikeOriginal(map, cell, true, out CellSurfaceStageLikeOriginal stage1))
                BakeCellStageSoftwareLikeOriginal(map, kernel, region, inputs, targetPixels, baseCoverage, cell, v0, v1, v2, v3, stage1);

            if (TryBuildCellStageLikeOriginal(map, cell, false, out CellSurfaceStageLikeOriginal stage2))
                BakeCellStageSoftwareLikeOriginal(map, kernel, region, inputs, targetPixels, baseCoverage, cell, v0, v1, v2, v3, stage2);
        }

        private void BakeTerrainCellFactureSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            int cellX,
            int cellY,
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies)
        {
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
                    scratchCopies);

                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    cellX, cellY,
                    v2, v1, v3,
                    scratchCopies);
            }
            else
            {
                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    cellX, cellY,
                    v0, v1, v3,
                    scratchCopies);

                BakeFactureTriangleSoftwareLikeOriginal(
                    map, region, inputs, targetPixels,
                    BaseSurfaceTriangleKindLikeOriginal.EvenLower,
                    cellX, cellY,
                    v0, v3, v2,
                    scratchCopies);
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
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies)
        {
            scratchCopies.Clear();
            ExpandFactureTriangleCopiesLikeAdapted(map, kind, cellX, cellY, a.Index, b.Index, c.Index, scratchCopies);

            bool emittedAny = scratchCopies.Count > 0;
            for (int i = 0; i < scratchCopies.Count; i++)
                RasterizeFactureTriangleDescriptorSoftwareLikeOriginal(map, region, inputs, targetPixels, a, b, c, scratchCopies[i]);

            GetFactureTriangleCoverageLikeAdapted(scratchCopies, out int coverageA, out int coverageB, out int coverageC);

            // MIDDLE_PIXEL_PARALLEL_NO_PNG_V17:
            // Keep the hole-closing fallback, but make it smarter.
            // Old logic only checked "any weight on each corner" and then painted a flat 192 alpha triangle.
            // That left some holes when coverage was weak/partial or the nearest facture sat outside the old 3-cell search. Here we measure accumulated coverage
            // per triangle corner and add only the missing part, so small gaps close without bringing back
            // the large blocky fallback look.
            if (NeedsSoftwareFactureFallbackLikeAdapted(emittedAny, coverageA, coverageB, coverageC))
            {
                if (TryBuildSoftwareFactureFallbackDescriptorLikeAdapted(map, kind, cellX, cellY, a, b, c, coverageA, coverageB, coverageC, out FactureTriangleCopyDescriptorLikeAdapted fallback))
                    RasterizeFactureTriangleDescriptorSoftwareLikeOriginal(map, region, inputs, targetPixels, a, b, c, fallback);
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

            int protectedRawFactureId = 0;
            bool protectedRoadLike = TryChooseProtectedRoadLikeRenderFactureIdLikeAdapted(
                map, a, b, c, out int protectedRenderFactureId, out protectedRawFactureId);

            int renderFactureId;
            if (protectedRoadLike)
            {
                renderFactureId = protectedRenderFactureId;
            }
            else if (!TryChooseTriangleWinnerRenderFactureIdLikeAdapted(map, a, b, c, out renderFactureId))
            {
                return false;
            }

            int bucketTextureId = GetFactureBucketTextureIdLikeAdapted(renderFactureId);
            if (bucketTextureId == 0)
                return false;

            int fallbackWeightA = protectedRoadLike
                ? BuildProtectedRoadLikeFallbackWeightLikeAdapted(map, a.Index, protectedRawFactureId, coverageA)
                : BuildSoftwareFactureFallbackWeightLikeAdapted(coverageA);
            int fallbackWeightB = protectedRoadLike
                ? BuildProtectedRoadLikeFallbackWeightLikeAdapted(map, b.Index, protectedRawFactureId, coverageB)
                : BuildSoftwareFactureFallbackWeightLikeAdapted(coverageB);
            int fallbackWeightC = protectedRoadLike
                ? BuildProtectedRoadLikeFallbackWeightLikeAdapted(map, c.Index, protectedRawFactureId, coverageC)
                : BuildSoftwareFactureFallbackWeightLikeAdapted(coverageC);
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

            return Mathf.Clamp(Mathf.Max(missingCoverage, 64), 0, 255);
        }

        private static bool TryChooseProtectedRoadLikeRenderFactureIdLikeAdapted(
            ParsedMap map,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            out int renderFactureId,
            out int rawFactureId)
        {
            renderFactureId = 0;
            rawFactureId = 0;
            if (map == null)
                return false;

            int bestVertex = -1;
            int bestRaw = 0;
            int bestWeight = -1;

            ConsiderProtectedRoadLikeVertexFactureLikeAdapted(map, a.Index, ref bestVertex, ref bestRaw, ref bestWeight);
            ConsiderProtectedRoadLikeVertexFactureLikeAdapted(map, b.Index, ref bestVertex, ref bestRaw, ref bestWeight);
            ConsiderProtectedRoadLikeVertexFactureLikeAdapted(map, c.Index, ref bestVertex, ref bestRaw, ref bestWeight);

            if (bestVertex < 0 || bestRaw == 0)
                return false;

            rawFactureId = bestRaw;
            renderFactureId = ResolveFactureRenderIndexForRawLikeAdapted(map, bestVertex, bestRaw, out _, out _, out _);
            return renderFactureId != 0 && IsRoadLikeProtectedFactureLikeAdapted(GetFactureBucketTextureIdLikeAdapted(renderFactureId));
        }

        private static void ConsiderProtectedRoadLikeVertexFactureLikeAdapted(
            ParsedMap map,
            int vertexIndex,
            ref int bestVertex,
            ref int bestRaw,
            ref int bestWeight)
        {
            int raw = GetFactureIdLikeOriginal(map, vertexIndex) & 255;
            if (raw == 0 || !IsRoadLikeProtectedFactureLikeAdapted(raw))
                return;

            int weight = Mathf.Clamp(GetFactureWeightByIdxLikeOriginal(map, vertexIndex), 0, 255);
            bool better = bestVertex < 0 || weight > bestWeight || (weight == bestWeight && raw < bestRaw);
            if (!better)
                return;

            bestVertex = vertexIndex;
            bestRaw = raw;
            bestWeight = weight;
        }

        private static int BuildProtectedRoadLikeFallbackWeightLikeAdapted(ParsedMap map, int vertexIndex, int protectedRawFactureId, int coverage)
        {
            int raw = GetFactureIdLikeOriginal(map, vertexIndex) & 255;
            int sourceWeight = Mathf.Clamp(GetFactureWeightByIdxLikeOriginal(map, vertexIndex), 0, 255);
            int missingCoverage = Mathf.Clamp(TerrainSoftwareFactureFallbackCoverageTargetLikeAdapted - Mathf.Clamp(coverage, 0, 255), 0, 255);

            if (raw == protectedRawFactureId)
                return Mathf.Clamp(Mathf.Max(Mathf.Max(sourceWeight, missingCoverage), 224), 0, 255);

            if (IsRoadLikeProtectedFactureLikeAdapted(raw))
                return Mathf.Clamp(Mathf.Max(sourceWeight, 192), 0, 255);

            // Non-road vertices are only the feathering band. This keeps the cobble/bridge core on top,
            // but still lets the edge dissolve into the neighbouring terrain.
            return Mathf.Clamp(Mathf.Min(Mathf.Max(missingCoverage, 48), 128), 0, 255);
        }

        private static bool IsRoadLikeProtectedFactureLikeAdapted(int bucketTextureId)
        {
            int id = bucketTextureId & 255;

            // Fallback-default table uses idx=N-1 for Textures\ground\TEXN.bmp.
            // These are the visible cobble/road-like candidates in the current texture pack:
            // TEX30, TEX55, TEX63 plus close road/wood bridge variants.
            if (id == 29 || id == 35 || id == 36 || id == 54 || id == 62)
                return true;

            FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
            if (tables == null || id < 0 || id >= tables.DiffuseTexturePath.Length)
                return false;

            string path = tables.DiffuseTexturePath[id];
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string lower = path.Replace('\\', '/').ToLowerInvariant();
            return lower.Contains("doroga") ||
                   lower.Contains("roadgorod") ||
                   lower.Contains("road_gorod") ||
                   lower.Contains("road/") ||
                   lower.Contains("/road") ||
                   lower.Contains("bridge") ||
                   lower.Contains("brus") ||
                   lower.Contains("cobbl") ||
                   lower.Contains("pavement") ||
                   lower.Contains("tex30") ||
                   lower.Contains("tex55") ||
                   lower.Contains("tex63");
        }

        private void BakeTerrainCellFallbackSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
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
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddLeft,
                    v0, v1, v2,
                    fallbackTile);

                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    v2, v1, v3,
                    fallbackTile);
            }
            else
            {
                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    v0, v1, v3,
                    fallbackTile);

                BakeFallbackTriangleUncoveredOnlyLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
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
            OriginalCellTriangulationLikeOriginal cell,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3,
            CellSurfaceStageLikeOriginal stage)
        {
            if ((cell.V0 % map.VertInLine & 1) != 0)
            {
                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddLeft,
                    v0, v1, v2,
                    stage.T0, stage.T1, stage.T2,
                    stage);

                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    v2, v1, v3,
                    stage.T2, stage.T1, stage.T3,
                    stage);
            }
            else
            {
                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    v0, v1, v3,
                    stage.T0, stage.T1, stage.T3,
                    stage);

                BakeExpandedTriangleStageSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels, baseCoverage, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenLower,
                    v0, v3, v2,
                    stage.T0, stage.T3, stage.T2,
                    stage);
            }
        }

        private void BakeExpandedTriangleStageSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int tA,
            int tB,
            int tC,
            CellSurfaceStageLikeOriginal stage)
        {
            int wA = ResolveCellVertexStageWeightLikeOriginal(cell, a.Index, stage.W0, stage.W1, stage.W2, stage.W3);
            int wB = ResolveCellVertexStageWeightLikeOriginal(cell, b.Index, stage.W0, stage.W1, stage.W2, stage.W3);
            int wC = ResolveCellVertexStageWeightLikeOriginal(cell, c.Index, stage.W0, stage.W1, stage.W2, stage.W3);

            int tMin;
            int tAve;
            int tMax;
            BuildSortedTriangleTilesLikeOriginal(kind, tA, tB, tC, out tMin, out tAve, out tMax);

            ExpandedTriangleCopyLikeOriginal copy0;
            BuildInitialExpandedTriangleCopyLikeOriginal(kind, cell, tMin, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copy0);
            RasterizeTriangleDescriptorSoftwareLikeOriginal(
                map, kernel, region, inputs, targetPixels,
                baseCoverage,
                a, b, c,
                BuildTriangleDescriptorFromCopyLikeAdapted(
                    map, inputs.Tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Primary, stage.IsBaseStage,
                    a.Index, b.Index, c.Index,
                    tA, tB, tC,
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                    wA, wB, wC,
                    copy0));

            if (tAve != tMin)
            {
                ExpandedTriangleCopyLikeOriginal copyAve;
                BuildAverageExpandedTriangleCopyLikeOriginal(kind, cell, tMin, tAve, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyAve);
                RasterizeTriangleDescriptorSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels,
                    baseCoverage,
                    a, b, c,
                    BuildTriangleDescriptorFromCopyLikeAdapted(
                        map, inputs.Tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Average, stage.IsBaseStage,
                        a.Index, b.Index, c.Index,
                        tA, tB, tC,
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                        wA, wB, wC,
                        copyAve));
            }

            if (tMax != tMin && tMax != tAve)
            {
                ExpandedTriangleCopyLikeOriginal copyMax;
                BuildMaximumExpandedTriangleCopyLikeOriginal(kind, cell, tAve, tMax, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyMax);
                RasterizeTriangleDescriptorSoftwareLikeOriginal(
                    map, kernel, region, inputs, targetPixels,
                    baseCoverage,
                    a, b, c,
                    BuildTriangleDescriptorFromCopyLikeAdapted(
                        map, inputs.Tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Maximum, stage.IsBaseStage,
                        a.Index, b.Index, c.Index,
                        tA, tB, tC,
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                        GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                        wA, wB, wC,
                        copyMax));
            }
        }

        private void RasterizeTriangleDescriptorSoftwareLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            byte[] baseCoverage,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            BaseSurfaceTriangleDescriptorLikeAdapted descriptor,
            bool onlyIfUncovered = false)
        {
            if (descriptor.AlphaA <= 0.0f && descriptor.AlphaB <= 0.0f && descriptor.AlphaC <= 0.0f)
                return;
            if (!ShouldEmitOverlayDescriptorLikeAdapted(descriptor))
                return;

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
                pA, pB, pC,
                uvA, uvB, uvC,
                crossA, crossB, crossC,
                colorA, colorB, colorC,
                descriptor.IsBaseStage,
                descriptor.PlainMode,
                onlyIfUncovered);
        }

        private void RasterizeFactureTriangleDescriptorSoftwareLikeOriginal(
            ParsedMap map,
            TerrainSoftwareChunkRegionLikeOriginal region,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            Color32[] targetPixels,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            FactureTriangleCopyDescriptorLikeAdapted descriptor)
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

            bool protectedRoadLike = IsRoadLikeProtectedFactureLikeAdapted(descriptor.BucketTextureId);
            int strongWeightCount = 0;
            if (descriptor.WeightA > FactureAlphaRefByteLikeOriginal)
                strongWeightCount++;
            if (descriptor.WeightB > FactureAlphaRefByteLikeOriginal)
                strongWeightCount++;
            if (descriptor.WeightC > FactureAlphaRefByteLikeOriginal)
                strongWeightCount++;
            bool protectedRoadCore = protectedRoadLike && strongWeightCount >= 2;

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
                descriptor.BucketTextureId,
                protectedRoadLike,
                protectedRoadCore);
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
            bool onlyIfUncovered)
        {
            float area = EdgeFunctionLikeOriginal(pA, pB, pC);
            if (Mathf.Abs(area) < 0.0001f)
                return;

            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.x, Mathf.Min(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.x, Mathf.Max(pB.x, pC.x))), 0, region.WidthPixels - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pA.y, Mathf.Min(pB.y, pC.y))), 0, region.HeightPixels - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pA.y, Mathf.Max(pB.y, pC.y))), 0, region.HeightPixels - 1);

            bool crossEnabled = !((colorA.a > 200) && (colorB.a > 200) && (colorC.a > 200) && !plainMode);
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

                        SampleTextureBilinearRgbaFastLikeOriginal(inputs.GroundPixels, inputs.GroundWidth, inputs.GroundHeight, uvx, uvy, false, out float atlasR, out float atlasG, out float atlasB, out _);
                        float srcR = Clamp01FastLikeOriginal(atlasR * lr * 2.0f);
                        float srcG = Clamp01FastLikeOriginal(atlasG * lg * 2.0f);
                        float srcB = Clamp01FastLikeOriginal(atlasB * lb * 2.0f);

                        int pixelIndex = row + x;
                        if (isBaseStage)
                        {
                            if (!onlyIfUncovered || baseCoverage == null || baseCoverage[pixelIndex] == 0)
                            {
                                if (la >= TerrainSoftwareAlphaClipLikeOriginal)
                                {
                                    targetPixels[pixelIndex] = new Color32(
                                        ToByteRoundClampLikeOriginal(srcR * 255.0f),
                                        ToByteRoundClampLikeOriginal(srcG * 255.0f),
                                        ToByteRoundClampLikeOriginal(srcB * 255.0f),
                                        255);
                                    if (baseCoverage != null)
                                        baseCoverage[pixelIndex] = 255;
                                }
                            }
                        }
                        else
                        {
                            float finalAlpha = la;
                            if (crossEnabled && inputs.CrossPixels != null && inputs.CrossPixels.Length > 0)
                            {
                                float crossUvX = crossA.x * w0 + crossB.x * w1 + crossC.x * w2;
                                float crossUvY = crossA.y * w0 + crossB.y * w1 + crossC.y * w2;
                                SampleTextureBilinearRgbaFastLikeOriginal(inputs.CrossPixels, inputs.CrossWidth, inputs.CrossHeight, crossUvX, crossUvY, true, out _, out _, out _, out float crossAValue);
                                finalAlpha = Clamp01FastLikeOriginal(crossAValue + la - 0.5f);
                            }

                            if (finalAlpha >= TerrainSoftwareAlphaClipLikeOriginal)
                            {
                                Color32 dst = targetPixels[pixelIndex];
                                float invA = 1.0f - finalAlpha;
                                targetPixels[pixelIndex] = new Color32(
                                    ToByteRoundClampLikeOriginal((srcR * finalAlpha + dst.r * inv255 * invA) * 255.0f),
                                    ToByteRoundClampLikeOriginal((srcG * finalAlpha + dst.g * inv255 * invA) * 255.0f),
                                    ToByteRoundClampLikeOriginal((srcB * finalAlpha + dst.b * inv255 * invA) * 255.0f),
                                    255);
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
            int bucketTextureId,
            bool protectedRoadLike,
            bool protectedRoadCore)
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
            float alphaRef = FactureAlphaRefByteLikeOriginal * inv255;

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
                        float alpha = Clamp01FastLikeOriginal((colorA.a * w0 + colorB.a * w1 + colorC.a * w2) * inv255);
                        alpha = ApplyFactureEdgePolicyAlphaLikeAdapted(
                            alpha,
                            w0,
                            w1,
                            w2,
                            x,
                            y,
                            bucketTextureId,
                            protectedRoadLike,
                            protectedRoadCore);
                        if (alpha > alphaRef)
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

                            targetPixels[pixelIndex] = new Color32(
                                ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(2.0f * sourceR * (dst.r * inv255)) * 255.0f),
                                ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(2.0f * sourceG * (dst.g * inv255)) * 255.0f),
                                ToByteRoundClampLikeOriginal(Clamp01FastLikeOriginal(2.0f * sourceB * (dst.b * inv255)) * 255.0f),
                                255);
                        }
                    }

                    w0 += edge0Dx;
                    w1 += edge1Dx;
                }

                rowW0 += edge0Dy;
                rowW1 += edge1Dy;
            }
        }

        private static float ApplyFactureEdgePolicyAlphaLikeAdapted(
            float alpha,
            float w0,
            float w1,
            float w2,
            int x,
            int y,
            int bucketTextureId,
            bool protectedRoadLike,
            bool protectedRoadCore)
        {
            if (alpha <= 0.0f)
                return 0.0f;

            float edge = Mathf.Min(w0, Mathf.Min(w1, w2));
            float edgeBand = protectedRoadLike ? TerrainSoftwareRoadLikeEdgeBandLikeAdapted : TerrainSoftwareOverlayEdgeBandLikeAdapted;

            if (edge < edgeBand)
            {
                float t = Clamp01FastLikeOriginal(edge / Mathf.Max(0.0001f, edgeBand));
                float dither = PixelDither01LikeAdapted(x, y, bucketTextureId);
                if (dither > t)
                    return 0.0f;

                // Preserve a few pixels on the edge, but never make the boundary a hard triangle.
                alpha *= protectedRoadLike ? Mathf.Lerp(0.70f, 1.0f, t) : Mathf.Lerp(0.45f, 1.0f, t);
            }
            else if (protectedRoadCore)
            {
                // Road/cobble core must stay readable. The edge still dissolves above.
                alpha = Mathf.Max(alpha, TerrainSoftwareRoadLikeCoreAlphaLikeAdapted);
            }

            return Clamp01FastLikeOriginal(alpha);
        }

        private static float PixelDither01LikeAdapted(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ ((uint)salt * 2246822519u);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 255u) * (1.0f / 255.0f);
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

        private Material CreateSoftwareBakedTerrainChunkMaterialLikeOriginal(Texture2D bakedTexture, int chunkX, int chunkY)
        {
            if (bakedTexture == null)
                return null;

            Shader shader = Shader.Find("Unlit/Texture")
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

            return mat;
        }
    }
}
