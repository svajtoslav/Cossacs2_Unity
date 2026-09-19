using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private readonly Dictionary<Vector2Int, long> _smpDirtyChunks = new Dictionary<Vector2Int, long>();
        private readonly HashSet<Vector2Int> _smpTextureDirtyChunks = new HashSet<Vector2Int>();
        // Persistent per-map mask: later stamps retain all earlier local edits.
        private readonly HashSet<Vector2Int> _smpChangedTextureCells = new HashSet<Vector2Int>();
        private void C2SmpMarkTextureVertexChanged(int vx, int vy)
        {
            for (int y = vy - 1; y <= vy; y++)
                for (int x = vx - 1; x <= vx; x++)
                    _smpChangedTextureCells.Add(new Vector2Int(x, y));
        }
        private long _smpRevision;
        private Coroutine _smpBakeCoroutine;
        private CancellationTokenSource _smpBakeCancellation;
        private TerrainSoftwareBakeInputsLikeOriginal _smpBakeInputs;
        internal int SmpPendingChunksForDiagnostics => _smpDirtyChunks.Count;
        internal int SmpPublishedChunksForDiagnostics { get; private set; }
        internal int SmpFailedChunksForDiagnostics { get; private set; }
        internal int SmpDiscardedStaleChunksForDiagnostics { get; private set; }

        private sealed partial class ParsedMap
        {
            internal ParsedMap SnapshotSmpSurface()
            {
                var snapshot = (ParsedMap)MemberwiseClone();
                // Only these buffers can be modified by terrain stamps. All map
                // dimensions, catalogs and immutable topology remain shared.
                snapshot.Heights = (short[])Heights.Clone();
                snapshot.XYShift = (byte[])XYShift.Clone();
                snapshot.TexMap = (byte[])TexMap.Clone();
                snapshot.TexMapEx = (byte[])TexMapEx.Clone();
                snapshot.WTexMapEx = (byte[])WTexMapEx.Clone();
                snapshot.FactureMap = (byte[])FactureMap.Clone();
                snapshot.FactureWeight = (byte[])FactureWeight.Clone();
                return snapshot;
            }
        }

        private IEnumerator C2SmpProcessDirtyChunks()
        {
            // StartCoroutine returns before we initialize: construction itself must
            // never wait for a 2561x2561 CPU texture rasterization.
            yield return null;
            _smpBakeCancellation = new CancellationTokenSource();
            CancellationToken token = _smpBakeCancellation.Token;
            while (_smpDirtyChunks.Count > 0)
            {
                KeyValuePair<Vector2Int, long> entry;
                using (var it = _smpDirtyChunks.GetEnumerator()) { it.MoveNext(); entry = it.Current; }
                Vector2Int key = entry.Key;
                bool rebuildTexture = _smpTextureDirtyChunks.Contains(key);
                ParsedMap source = _map;
                if (source == null || _terrainRoot == null) break;
                if (rebuildTexture && _smpBakeInputs == null)
                {
                    _smpBakeInputs = PrepareTerrainSoftwareBakeInputsLikeOriginal();
                    if (_smpBakeInputs == null)
                    {
                        Debug.LogError("[C2 SMP ASYNC] missing terrain inputs");
                        SmpFailedChunksForDiagnostics++; break;
                    }
                    _ = GetTerrainTextureTablesLikeOriginal();
                    _ = GetRandomTableLikeOriginal();
                    if (!TerrainQualityFactureLayerDisabledLikeAdapted)
                        PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal(_smpBakeInputs);
                }
                var snapshot = rebuildTexture ? source.SnapshotSmpSurface() : source;
                var kernel = _lastBuiltTerrainKernel;
                int x = kernel.MinCellX + key.x * TerrainSoftwareChunkCellsLikeOriginal;
                int y = kernel.MinCellY + key.y * TerrainSoftwareChunkCellsLikeOriginal;
                var region = CreateTerrainSoftwareChunkRegionLikeOriginal(snapshot, kernel, x,
                    Mathf.Min(kernel.MaxCellXExclusive, x + TerrainSoftwareChunkCellsLikeOriginal), y,
                    Mathf.Min(kernel.MaxCellYExclusive, y + TerrainSoftwareChunkCellsLikeOriginal));
                var inputs = _smpBakeInputs;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                if (!rebuildTexture)
                {
                    try
                    {
                        C2SmpPublishHeightOnlyChunk(snapshot, kernel, region, key);
                        SmpPublishedChunksForDiagnostics++;
                        Debug.Log("[C2 SMP ASYNC] published height-only chunk=" + key +
                                  " keepTexture=1 mainMs=" + clock.Elapsed.TotalMilliseconds.ToString("0.0"));
                    }
                    catch (Exception ex)
                    {
                        SmpFailedChunksForDiagnostics++;
                        Debug.LogException(ex);
                    }
                    if (_smpDirtyChunks.TryGetValue(key, out long heightRevision) && heightRevision == entry.Value)
                    {
                        _smpDirtyChunks.Remove(key);
                        _smpTextureDirtyChunks.Remove(key);
                    }
                    yield return null;
                    continue;
                }
                // One worker/one chunk, not one job per building: bounded pixel
                // scratch memory, and overlapping new stamps replace stale jobs.
                var changedCells = new List<Vector2Int>();
                foreach (Vector2Int cell in _smpChangedTextureCells)
                    if (cell.x >= region.MinCellX && cell.x < region.MaxCellXExclusive &&
                        cell.y >= region.MinCellY && cell.y < region.MaxCellYExclusive) changedCells.Add(cell);
                Task<Color32[]> work = Task.Run(() =>
                {
                    Color32[] pixels = BakeTerrainChunkPixelsSoftwareLikeOriginal(snapshot, kernel, region, inputs, token);
                    C2SmpMaskUnchangedTexturePixels(pixels, kernel, region, changedCells, token);
                    return pixels;
                }, token);
                while (!work.IsCompleted) yield return null;
                if (work.IsFaulted || work.IsCanceled)
                {
                    Debug.LogError("[C2 SMP ASYNC] bake failed: " + work.Exception);
                    SmpFailedChunksForDiagnostics++;
                    if (_smpDirtyChunks.TryGetValue(key, out long failedRevision) && failedRevision == entry.Value)
                    {
                        _smpDirtyChunks.Remove(key);
                        _smpTextureDirtyChunks.Remove(key);
                    }
                    continue;
                }
                if (!ReferenceEquals(source, _map)) break;
                if (!_smpDirtyChunks.TryGetValue(key, out long revision) || revision != entry.Value)
                {
                    SmpDiscardedStaleChunksForDiagnostics++;
                    continue; // A newer stamp touched this same chunk while baking.
                }
                try
                {
                    C2SmpPublishChunk(snapshot, kernel, region, key, work.Result);
                    SmpPublishedChunksForDiagnostics++;
                    Debug.Log("[C2 SMP ASYNC] published chunk=" + key + " backgroundMs=" + clock.Elapsed.TotalMilliseconds.ToString("0.0"));
                }
                catch (Exception ex)
                {
                    SmpFailedChunksForDiagnostics++; Debug.LogException(ex);
                }
                _smpDirtyChunks.Remove(key);
                _smpTextureDirtyChunks.Remove(key);
                yield return null; // At most one Texture2D/Mesh publication per frame.
            }
            _smpDirtyChunks.Clear();
            _smpTextureDirtyChunks.Clear();
            _smpBakeCancellation.Dispose(); _smpBakeCancellation = null;
            _smpBakeCoroutine = null;
        }

        private void C2SmpPublishChunk(ParsedMap snapshot, OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region, Vector2Int key, Color32[] pixels)
        {
            string name = "TerrainChunkSoftware_" + key.x.ToString("00") + "_" + key.y.ToString("00");
            Transform target = C2SmpFindTerrainChunkTransformV1LikeOriginal(_terrainRoot.transform, name);
            MeshFilter mf = target != null ? target.GetComponent<MeshFilter>() : null;
            MeshRenderer mr = target != null ? target.GetComponent<MeshRenderer>() : null;
            Material material = GetSharedSoftwareBakedTerrainMaterialV13LikeAdapted();
            if (mf == null || mr == null || material == null) throw new InvalidOperationException("Missing terrain chunk " + name);
            Texture2D texture = null;
            Mesh mesh = null;
            try
            {
                texture = new Texture2D(region.WidthPixels, region.HeightPixels, TextureFormat.RGBA32, false)
                { name = name + "_SmpLocalPatch", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixelData(pixels, 0);
                texture.Apply(false, true);
                mesh = BuildProjectedChunkMeshSoftwareLikeOriginal(snapshot, kernel, region, out Bounds _);
                Mesh previousMesh = mf.sharedMesh;
                if (previousMesh == null || mesh == null || previousMesh.vertexCount != mesh.vertexCount)
                    throw new InvalidOperationException("Terrain patch topology changed: " + name);
                // UV0 continues sampling the original atlas; UV1 samples the local patch.
                mesh.uv2 = mesh.uv;
                mesh.uv = previousMesh.uv;
                var block = new MaterialPropertyBlock();
                mr.GetPropertyBlock(block); // Preserve _MainTex/_BaseMap and their existing atlas.
                block.SetTexture("_C2SmpPatchTex", texture);
                block.SetFloat("_C2SmpPatchEnabled", 1.0f);
                mf.sharedMesh = mesh;
                mr.SetPropertyBlock(block);
                for (int i = _c2SmpSurfacePatchesV1LikeOriginal.Count - 1; i >= 0; --i)
                {
                    var old = _c2SmpSurfacePatchesV1LikeOriginal[i];
                    if (old.MinCellX != region.MinCellX || old.MinCellY != region.MinCellY) continue;
                    C2SmpDestroySurfacePatchV1LikeOriginal(old); _c2SmpSurfacePatchesV1LikeOriginal.RemoveAt(i);
                }
                _c2SmpSurfacePatchesV1LikeOriginal.Add(new C2SmpSurfacePatchV1LikeOriginal {
                    MinCellX = region.MinCellX, MinCellY = region.MinCellY,
                    MaxCellXExclusive = region.MaxCellXExclusive, MaxCellYExclusive = region.MaxCellYExclusive,
                    Root = target.gameObject, Mesh = mesh, Texture = texture });
            }
            catch
            {
                if (texture != null) SafeDestroy(texture);
                if (mesh != null) SafeDestroy(mesh);
                throw;
            }
        }

        private void C2SmpPublishHeightOnlyChunk(ParsedMap snapshot, OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region, Vector2Int key)
        {
            string name = "TerrainChunkSoftware_" + key.x.ToString("00") + "_" + key.y.ToString("00");
            Transform target = C2SmpFindTerrainChunkTransformV1LikeOriginal(_terrainRoot.transform, name);
            MeshFilter mf = target != null ? target.GetComponent<MeshFilter>() : null;
            if (mf == null) throw new InvalidOperationException("Missing terrain chunk " + name);

            Mesh mesh = BuildProjectedChunkMeshSoftwareLikeOriginal(snapshot, kernel, region, out Bounds _);
            if (mesh == null) throw new InvalidOperationException("Empty terrain mesh " + name);

            C2SmpSurfacePatchV1LikeOriginal owned = null;
            for (int i = _c2SmpSurfacePatchesV1LikeOriginal.Count - 1; i >= 0; --i)
            {
                C2SmpSurfacePatchV1LikeOriginal candidate = _c2SmpSurfacePatchesV1LikeOriginal[i];
                if (candidate.MinCellX == region.MinCellX && candidate.MinCellY == region.MinCellY)
                {
                    owned = candidate;
                    break;
                }
            }

            Mesh previousOwnedMesh = owned != null ? owned.Mesh : null;
            // Initial chunks can sample a sub-rectangle of a shared texture atlas.
            // Height-only regeneration creates 0..1 UVs; keeping the atlas with those
            // UVs replaces the ENTIRE chunk's appearance. Preserve the existing UVs.
            Mesh previousMesh = mf.sharedMesh;
            if (previousMesh == null || previousMesh.vertexCount != mesh.vertexCount)
            {
                SafeDestroy(mesh);
                throw new InvalidOperationException("Terrain height patch topology changed: " + name);
            }
            mesh.uv = previousMesh.uv;
            mesh.uv2 = previousMesh.uv2;
            mf.sharedMesh = mesh;
            if (owned == null)
            {
                _c2SmpSurfacePatchesV1LikeOriginal.Add(new C2SmpSurfacePatchV1LikeOriginal
                {
                    MinCellX = region.MinCellX,
                    MinCellY = region.MinCellY,
                    MaxCellXExclusive = region.MaxCellXExclusive,
                    MaxCellYExclusive = region.MaxCellYExclusive,
                    Root = target.gameObject,
                    Mesh = mesh,
                    Texture = null
                });
            }
            else
            {
                owned.Mesh = mesh;
                owned.MaxCellXExclusive = region.MaxCellXExclusive;
                owned.MaxCellYExclusive = region.MaxCellYExclusive;
            }
            if (previousOwnedMesh != null && previousOwnedMesh != mesh)
                SafeDestroy(previousOwnedMesh);
        }

        private static void C2SmpMaskUnchangedTexturePixels(Color32[] pixels, OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal region, List<Vector2Int> cells, CancellationToken cancellation)
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i].a = 0;
            float sx = (region.WidthPixels - 1) / region.FootprintBounds.size.x;
            float sy = (region.HeightPixels - 1) / region.FootprintBounds.size.z;
            const int margin = 8; // Covers local triangle blending and edge filtering.
            foreach (Vector2Int cell in cells)
            {
                cancellation.ThrowIfCancellationRequested();
                float x0 = cell.x * kernel.BackingStepXWorld - kernel.CenterX;
                float x1 = (cell.x + 1) * kernel.BackingStepXWorld - kernel.CenterX;
                float z0 = (cell.y * kernel.BackingStepZWorld - kernel.CenterZ) * WorldZSign;
                float z1 = ((cell.y + 1) * kernel.BackingStepZWorld + kernel.BackingOddColumnOffsetZWorld - kernel.CenterZ) * WorldZSign;
                int left = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(x0,x1) - region.FootprintBounds.min.x)*sx) - margin, 0, region.WidthPixels-1);
                int right = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(x0,x1) - region.FootprintBounds.min.x)*sx) + margin, 0, region.WidthPixels-1);
                int bottom = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(z0,z1) - region.FootprintBounds.min.z)*sy) - margin, 0, region.HeightPixels-1);
                int top = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(z0,z1) - region.FootprintBounds.min.z)*sy) + margin, 0, region.HeightPixels-1);
                for (int y = bottom; y <= top; y++)
                    for (int x = left; x <= right; x++) pixels[y*region.WidthPixels+x].a = 255;
            }
        }

        private void C2SmpCancelPendingBake()
        {
            if (_smpBakeCancellation != null) { _smpBakeCancellation.Cancel(); _smpBakeCancellation.Dispose(); _smpBakeCancellation = null; }
            if (_smpBakeCoroutine != null) StopCoroutine(_smpBakeCoroutine);
            _smpBakeCoroutine = null; _smpBakeInputs = null; _smpDirtyChunks.Clear(); _smpTextureDirtyChunks.Clear();
            _smpChangedTextureCells.Clear();
        }
    }
}
