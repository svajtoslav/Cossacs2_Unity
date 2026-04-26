using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool GpuBaseBakeExperimentEnabledLikeOriginal = true;
        private const bool GpuBaseBakeKeepFacturesLikeOriginal = true;
        private const int GpuBaseBakePixelsPerCellLikeOriginal = 16;

        private bool TryBuildGpuBaseBakedOldSurfaceLikeOriginal(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            terrainBounds = new Bounds(Vector3.zero, Vector3.one);

            if (map == null || map.Heights == null || map.Heights.Length == 0)
                return false;
            if (!SystemInfo.supportsRenderTextures)
            {
                Debug.LogWarning("[C2:GPU-BAKE BASE V1] RenderTexture is not supported; fallback to fast triangle renderer.");
                return false;
            }

            Shader bakedShader = Shader.Find("Cossacks2Bridge/TerrainGpuBakedChunk");
            if (bakedShader == null)
            {
                Debug.LogWarning("[C2:GPU-BAKE BASE V1] Missing shader Cossacks2Bridge/TerrainGpuBakedChunk; fallback to fast triangle renderer.");
                return false;
            }

            _terrainMaterial = CreateTerrainMaterialLikeOriginal(map);
            _terrainBaseMaterial = CreateSurfacePassMaterialLikeAdapted(_terrainMaterial, false);
            _terrainOverlayMaterial = CreateSurfacePassMaterialLikeAdapted(_terrainMaterial, true);

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(map);
            _lastBuiltTerrainKernel = kernel;
            _hasLastBuiltTerrainKernel = true;

            int cellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int stripeWidth = Mathf.Clamp(StripeColumnWidth, 4, 256);
            int stripeCount = Mathf.Max(1, Mathf.CeilToInt(cellsX / (float)stripeWidth));
            bool hasBounds = false;
            int built = 0;

            Debug.Log($"[C2:GPU-BAKE BASE V1] enabled. mode=gpu-rendertexture-base-cross-only stripes={stripeCount} pxPerCell={GpuBaseBakePixelsPerCellLikeOriginal} keepFactures={GpuBaseBakeKeepFacturesLikeOriginal}. This is experiment 1: base+BoundNew128 only, no HQ/facture bake yet.");

            BeginFactureCoverageAuditLikeAdapted(map, kernel, stripeCount);

            for (int stripe = 0; stripe < stripeCount; stripe++)
            {
                int startX = kernel.MinCellX + stripe * stripeWidth;
                int endX = Mathf.Min(kernel.MaxCellXExclusive, startX + stripeWidth);
                if (endX <= startX)
                    continue;

                KernelStripeData data = BuildTerrainWholeMapLikeOriginalKernel(map, kernel, startX, endX);
                if (data == null || data.Vertices.Count == 0 || !data.HasBounds)
                    continue;

                Bounds stripeBounds = data.Bounds;
                int cellCountX = Mathf.Max(1, endX - startX);
                int cellCountY = Mathf.Max(1, kernel.MaxCellYExclusive - kernel.MinCellY);
                int width = Mathf.Clamp(cellCountX * GpuBaseBakePixelsPerCellLikeOriginal + 1, 16, 4096);
                int height = Mathf.Clamp(cellCountY * GpuBaseBakePixelsPerCellLikeOriginal + 1, 16, 4096);

                RenderTexture baked = BakeStripeSurfaceToRenderTextureLikeOriginal(data, stripeBounds, width, height, stripe);
                if (baked == null)
                    continue;

                Mesh displayMesh = BuildGpuBakedStripeDisplayMeshLikeOriginal(data, stripeBounds);
                if (displayMesh == null || displayMesh.vertexCount == 0)
                {
                    SafeDestroy(baked);
                    continue;
                }

                Material displayMaterial = new Material(bakedShader)
                {
                    name = $"TerrainGpuBakedBase_{stripe:000}"
                };
                displayMaterial.SetTexture("_MainTex", baked);
                displayMaterial.mainTexture = baked;

                var go = new GameObject($"GpuBakedBaseStripe_{stripe:000}");
                go.transform.SetParent(parent, false);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = displayMesh;
                mr.sharedMaterial = displayMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (GpuBaseBakeKeepFacturesLikeOriginal)
                {
                    try
                    {
                        BuildFactureStripeLayerLikeAdapted(map, kernel, startX, endX, go.transform, stripe);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[C2:GPU-BAKE BASE V1] facture stripe={stripe} hookup failed: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                if (!hasBounds)
                {
                    terrainBounds = stripeBounds;
                    hasBounds = true;
                }
                else
                {
                    terrainBounds.Encapsulate(stripeBounds.min);
                    terrainBounds.Encapsulate(stripeBounds.max);
                }

                built++;
            }

            EndFactureCoverageAuditLikeAdapted();

            if (!hasBounds || built <= 0)
            {
                Debug.LogWarning("[C2:GPU-BAKE BASE V1] no baked stripes were built; fallback to fast triangle renderer.");
                return false;
            }

            Debug.Log($"[C2:GPU-BAKE BASE V1] built={built}/{stripeCount}. Result uses GPU RenderTexture bake for base+cross; HQ/factures stay on current fast no-hole path.");
            return true;
        }

        private RenderTexture BakeStripeSurfaceToRenderTextureLikeOriginal(KernelStripeData data, Bounds stripeBounds, int width, int height, int stripeIndex)
        {
            Mesh bakeMesh = BuildGpuBakePixelMeshLikeOriginal(data, stripeBounds, width, height);
            if (bakeMesh == null || bakeMesh.vertexCount == 0)
                return null;

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"GpuBaseBake_{stripeIndex:000}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false,
                anisoLevel = 1
            };
            rt.Create();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, width, 0, height);
            GL.Clear(false, true, Color.black);

            if (_terrainBaseMaterial != null && _terrainBaseMaterial.SetPass(0))
                Graphics.DrawMeshNow(bakeMesh, Matrix4x4.identity, 0);
            if (bakeMesh.subMeshCount > 1 && _terrainOverlayMaterial != null && _terrainOverlayMaterial.SetPass(1))
                Graphics.DrawMeshNow(bakeMesh, Matrix4x4.identity, 1);

            GL.PopMatrix();
            RenderTexture.active = prev;
            SafeDestroy(bakeMesh);
            return rt;
        }

        private static Mesh BuildGpuBakePixelMeshLikeOriginal(KernelStripeData data, Bounds stripeBounds, int width, int height)
        {
            if (data == null || data.Vertices == null || data.Vertices.Count == 0)
                return null;

            float sizeX = Mathf.Max(0.001f, stripeBounds.size.x);
            float sizeZ = Mathf.Max(0.001f, stripeBounds.size.z);
            var verts = new List<Vector3>(data.Vertices.Count);
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                Vector3 w = data.Vertices[i];
                float px = ((w.x - stripeBounds.min.x) / sizeX) * Mathf.Max(1, width - 1);
                float py = ((w.z - stripeBounds.min.z) / sizeZ) * Mathf.Max(1, height - 1);
                verts.Add(new Vector3(px, py, 0.0f));
            }

            var mesh = new Mesh { name = "GpuBaseBakePixelMesh" };
            if (verts.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            if (data.OverlayTriangles != null && data.OverlayTriangles.Count > 0)
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(data.Triangles, 0, true);
                mesh.SetTriangles(data.OverlayTriangles, 1, true);
            }
            else
            {
                mesh.SetTriangles(data.Triangles, 0, true);
            }
            if (data.Colors != null && data.Colors.Count == verts.Count)
                mesh.SetColors(data.Colors);
            if (data.Uv0 != null && data.Uv0.Count == verts.Count)
                mesh.SetUVs(0, data.Uv0);
            if (data.Uv1 != null && data.Uv1.Count == verts.Count)
                mesh.SetUVs(1, data.Uv1);
            if (data.Uv2 != null && data.Uv2.Count == verts.Count)
                mesh.SetUVs(2, data.Uv2);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildGpuBakedStripeDisplayMeshLikeOriginal(KernelStripeData data, Bounds stripeBounds)
        {
            if (data == null || data.Vertices == null || data.Vertices.Count == 0)
                return null;

            float sizeX = Mathf.Max(0.001f, stripeBounds.size.x);
            float sizeZ = Mathf.Max(0.001f, stripeBounds.size.z);
            var uv = new List<Vector2>(data.Vertices.Count);
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                Vector3 w = data.Vertices[i];
                uv.Add(new Vector2(
                    Mathf.Clamp01((w.x - stripeBounds.min.x) / sizeX),
                    Mathf.Clamp01((w.z - stripeBounds.min.z) / sizeZ)));
            }

            var tris = new List<int>(data.Triangles.Count + (data.OverlayTriangles != null ? data.OverlayTriangles.Count : 0));
            tris.AddRange(data.Triangles);
            if (data.OverlayTriangles != null && data.OverlayTriangles.Count > 0)
                tris.AddRange(data.OverlayTriangles);

            var mesh = new Mesh { name = "GpuBakedBaseDisplayMesh" };
            if (data.Vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(data.Vertices);
            mesh.SetTriangles(tris, 0, true);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
