using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const int FastV8MinStripeColumnsLikeAdapted = 64;
        private const int FastV8MaxStripeColumnsLikeAdapted = 192;

        private void BuildFastRuntimeOldSurfaceTexturedNoBakeV8LikeAdapted(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (map.Heights == null || map.Heights.Length == 0)
                throw new InvalidOperationException("Map has no SURF heights.");

            // This is the critical part V6 missed: it makes the existing original texturing
            // callbacks live, so BuildTerrainWholeMapLikeOriginalKernel emits GroundTex UVs
            // instead of falling back to height-color triangles.
            LogTerrainTexturingBootstrapLikeOriginal(map);

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(map);
            _lastBuiltTerrainKernel = kernel;
            _hasLastBuiltTerrainKernel = true;

            TerrainTextureResourcesLikeOriginal resources = TryLoadTerrainSurfaceResourcesLikeOriginal();
            Material masterMaterial = CreateTerrainMaterialLikeOriginal(map);
            Material baseMaterial = CreateFastRuntimeSurfaceMaterialV8LikeAdapted(masterMaterial, resources, false);
            Material overlayMaterial = CreateFastRuntimeSurfaceMaterialV8LikeAdapted(masterMaterial, resources, true) ?? baseMaterial;

            _terrainMaterial = masterMaterial != null ? masterMaterial : baseMaterial;
            _terrainBaseMaterial = baseMaterial;
            _terrainOverlayMaterial = overlayMaterial;

            int cellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int stripeWidth = Mathf.Clamp(
                Mathf.Max(StripeColumnWidth, FastV8MinStripeColumnsLikeAdapted),
                FastV8MinStripeColumnsLikeAdapted,
                Mathf.Min(FastV8MaxStripeColumnsLikeAdapted, Mathf.Max(FastV8MinStripeColumnsLikeAdapted, cellsX)));
            int stripeCount = Mathf.Max(1, Mathf.CeilToInt(cellsX / (float)stripeWidth));

            terrainBounds = new Bounds(Vector3.zero, Vector3.one);
            bool hasBounds = false;
            int built = 0;
            int skipped = 0;
            int fallbackStripes = 0;
            int runtimePayloadStripes = 0;
            int totalVertices = 0;
            int totalBaseTriangles = 0;
            int totalOverlayTriangles = 0;

            string groundAtlasText = resources != null && resources.GroundAtlas != null
                ? resources.GroundAtlas.width.ToString() + "x" + resources.GroundAtlas.height.ToString()
                : "NULL";
            string crossAtlasText = resources != null && resources.CrossTex != null
                ? resources.CrossTex.width.ToString() + "x" + resources.CrossTex.height.ToString()
                : "NULL";

            UnityEngine.Debug.Log(
                $"[C2:FAST V9] textured no-bake runtime terrain active. " +
                $"rect=({kernel.MinCellX},{kernel.MinCellY})->({kernel.MaxCellXExclusive},{kernel.MaxCellYExclusive}) " +
                $"stripeWidth={stripeWidth} stripes={stripeCount} " +
                $"groundAtlas={groundAtlasText} cross={crossAtlasText} " +
                $"mode=runtime-mesh+original-uv; noCPUChunkBake noTexturePictures noPNGCache noSoftwareRasterLoop");

            for (int stripe = 0; stripe < stripeCount; stripe++)
            {
                int startX = kernel.MinCellX + stripe * stripeWidth;
                int endX = Mathf.Min(kernel.MaxCellXExclusive, startX + stripeWidth);
                if (endX <= startX)
                    continue;

                bool usedFallback;
                string fallbackReason;
                Mesh stripeMesh = BuildFastRuntimeStripeMeshTexturedNoBakeV8LikeAdapted(
                    map, kernel, startX, endX, out Bounds stripeBounds,
                    out usedFallback, out fallbackReason, out int baseTriCount, out int overlayTriCount);

                if (stripeMesh == null || stripeMesh.vertexCount <= 0)
                {
                    skipped++;
                    continue;
                }

                var go = new GameObject(usedFallback ? $"TerrainFastV8_DirectAtlasFallback_{stripe:000}" : $"TerrainFastV8_OriginalUv_{stripe:000}");
                go.transform.SetParent(parent, false);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = stripeMesh;

                if (stripeMesh.subMeshCount > 1)
                    mr.sharedMaterials = new[] { baseMaterial, overlayMaterial };
                else
                    mr.sharedMaterial = baseMaterial;

                ConfigureFastRuntimeRendererV8LikeAdapted(mr);

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

                if (usedFallback)
                {
                    fallbackStripes++;
                    if (fallbackStripes <= 4)
                        UnityEngine.Debug.LogWarning($"[C2:FAST V9] stripe={stripe} direct atlas fallback reason={fallbackReason}");
                }
                else
                {
                    runtimePayloadStripes++;
                }

                totalVertices += stripeMesh.vertexCount;
                totalBaseTriangles += baseTriCount;
                totalOverlayTriangles += overlayTriCount;
                built++;
            }

            if (!hasBounds)
                terrainBounds = new Bounds(Vector3.zero, Vector3.one);

            UnityEngine.Debug.Log(
                $"[C2:FAST V9] built textured runtime stripes={built}/{stripeCount} skipped={skipped} " +
                $"originalPayloadStripes={runtimePayloadStripes} directAtlasFallbackStripes={fallbackStripes} " +
                $"vertices={totalVertices} baseTris={totalBaseTriangles} overlayTris={totalOverlayTriangles}. " +
                $"Old software chunk bake was NOT called.");
        }

        // Compatibility alias: if an older patched C2BattleTerrainMode.cs still calls V7,
        // it is redirected to the fixed V8 implementation instead of the broken V7 shader path.
        private void BuildFastRuntimeOldSurfaceTexturedNoBakeV7LikeAdapted(ParsedMap map, Transform parent, out Bounds terrainBounds)
        {
            BuildFastRuntimeOldSurfaceTexturedNoBakeV8LikeAdapted(map, parent, out terrainBounds);
        }

        private Material CreateFastRuntimeSurfaceMaterialV8LikeAdapted(Material source, TerrainTextureResourcesLikeOriginal resources, bool overlayPass)
        {
            Material mat = null;

            Shader fastShader = Shader.Find("Cossacks2Bridge/FastRuntimeSurfaceAtlasV8")
                                ?? Shader.Find("Cossacks2Bridge/TerrainRuntimeSurfaceBlendLikeOriginal")
                                ?? Shader.Find("Cossacks2Bridge/TerrainRuntimeBaseSurfaceAtlas")
                                ?? Shader.Find("Unlit/Texture")
                                ?? Shader.Find("Standard");

            if (source != null)
                mat = new Material(source);
            else if (fastShader != null)
                mat = new Material(fastShader);

            if (mat == null)
                return null;

            Texture2D groundAtlas = resources != null ? resources.GroundAtlas : null;
            Texture2D crossTex = resources != null ? resources.CrossTex : null;

            bool materialCanSampleAtlas = mat.HasProperty("_GroundAtlas") || mat.HasProperty("_MainTex") || mat.HasProperty("_BaseMap");
            if (fastShader != null && (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name == "Standard" || (groundAtlas != null && !materialCanSampleAtlas)))
                mat.shader = fastShader;

            mat.name = overlayPass ? "C2_FastV8_RuntimeSurface_OverlayOnly" : "C2_FastV8_RuntimeSurface_BaseOnly";

            if (groundAtlas != null)
            {
                if (mat.HasProperty("_GroundAtlas"))
                    mat.SetTexture("_GroundAtlas", groundAtlas);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", groundAtlas);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", groundAtlas);
                mat.mainTexture = groundAtlas;
            }

            if (crossTex != null && mat.HasProperty("_CrossTex"))
                mat.SetTexture("_CrossTex", crossTex);

            if (mat.HasProperty("_UseCrossLikeOriginal"))
                mat.SetFloat("_UseCrossLikeOriginal", crossTex != null ? 1.0f : 0.0f);
            if (mat.HasProperty("_UseOverlayLikeOriginal"))
                mat.SetFloat("_UseOverlayLikeOriginal", 1.0f);
            if (mat.HasProperty("_UseDitherLikeOriginal"))
                mat.SetFloat("_UseDitherLikeOriginal", 0.0f);
            if (mat.HasProperty("_DitherStrengthLikeOriginal"))
                mat.SetFloat("_DitherStrengthLikeOriginal", 0.0f);
            if (mat.HasProperty("_SurfacePassModeLikeAdapted"))
                mat.SetFloat("_SurfacePassModeLikeAdapted", overlayPass ? 2.0f : 1.0f);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            mat.renderQueue = overlayPass ? 2001 : 2000;
            mat.enableInstancing = false;
            return mat;
        }

        private Mesh BuildFastRuntimeStripeMeshTexturedNoBakeV8LikeAdapted(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            int startCellX,
            int endCellX,
            out Bounds bounds,
            out bool usedFallback,
            out string fallbackReason,
            out int baseTriCount,
            out int overlayTriCount)
        {
            usedFallback = false;
            fallbackReason = string.Empty;
            baseTriCount = 0;
            overlayTriCount = 0;

            KernelStripeData data = BuildTerrainWholeMapLikeOriginalKernel(map, kernel, startCellX, endCellX);
            if (IsFastRuntimeKernelPayloadTexturedV8LikeAdapted(data, out fallbackReason))
            {
                Mesh mesh = BuildFastRuntimeMeshFromKernelDataV8LikeAdapted(data, $"FastV8_OriginalUvStripe_{startCellX}_{endCellX}", out bounds, out baseTriCount, out overlayTriCount);
                if (mesh != null)
                    return mesh;
            }

            usedFallback = true;
            if (string.IsNullOrEmpty(fallbackReason))
                fallbackReason = "kernel mesh build returned null";
            return BuildFastDirectAtlasFallbackStripeMeshV8LikeAdapted(map, kernel, startCellX, endCellX, out bounds, out baseTriCount);
        }

        private static bool IsFastRuntimeKernelPayloadTexturedV8LikeAdapted(KernelStripeData data, out string reason)
        {
            reason = string.Empty;
            if (data == null)
            {
                reason = "kernel data null";
                return false;
            }
            if (data.Vertices == null || data.Vertices.Count == 0)
            {
                reason = "kernel vertices empty";
                return false;
            }
            if (data.Triangles == null || data.Triangles.Count == 0)
            {
                reason = "kernel base triangles empty";
                return false;
            }
            if (data.Uv0 == null || data.Uv0.Count != data.Vertices.Count)
            {
                reason = "uv0 missing or count mismatch";
                return false;
            }
            if (data.Colors == null || data.Colors.Count != data.Vertices.Count)
            {
                reason = "vertex colors missing or count mismatch";
                return false;
            }

            int nonZeroUv = 0;
            int nonWhiteOrAlphaVertex = 0;
            int sampleCount = Mathf.Min(data.Vertices.Count, 4096);
            for (int i = 0; i < sampleCount; i++)
            {
                Vector2 uv = data.Uv0[i];
                if (Mathf.Abs(uv.x) > 0.00001f || Mathf.Abs(uv.y) > 0.00001f)
                    nonZeroUv++;

                Color c = data.Colors[i];
                if (Mathf.Abs(c.r - 1.0f) > 0.001f || Mathf.Abs(c.g - 1.0f) > 0.001f || Mathf.Abs(c.b - 1.0f) > 0.001f || c.a < 0.999f)
                    nonWhiteOrAlphaVertex++;
            }

            if (nonZeroUv <= 0)
            {
                reason = "kernel emitted fallback zero UVs";
                return false;
            }

            // Non-white/alpha is not mandatory for visible texture, but if it is zero the
            // kernel likely fell back to plain triangles. Keep it as a warning only.
            return true;
        }

        private static Mesh BuildFastRuntimeMeshFromKernelDataV8LikeAdapted(
            KernelStripeData data,
            string meshName,
            out Bounds bounds,
            out int baseTriCount,
            out int overlayTriCount)
        {
            baseTriCount = 0;
            overlayTriCount = 0;
            bounds = data != null && data.HasBounds ? data.Bounds : new Bounds(Vector3.zero, Vector3.one);
            if (data == null || data.Vertices == null || data.Vertices.Count == 0)
                return null;

            var mesh = new Mesh { name = meshName };
            if (data.Vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(data.Vertices);

            if (data.OverlayTriangles != null && data.OverlayTriangles.Count > 0)
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(data.Triangles, 0, false);
                mesh.SetTriangles(data.OverlayTriangles, 1, false);
                overlayTriCount = data.OverlayTriangles.Count / 3;
            }
            else
            {
                mesh.subMeshCount = 1;
                mesh.SetTriangles(data.Triangles, 0, false);
            }

            baseTriCount = data.Triangles != null ? data.Triangles.Count / 3 : 0;

            if (data.Colors != null && data.Colors.Count == data.Vertices.Count)
                mesh.SetColors(data.Colors);
            if (data.Uv0 != null && data.Uv0.Count == data.Vertices.Count)
                mesh.SetUVs(0, data.Uv0);
            if (data.Uv1 != null && data.Uv1.Count == data.Vertices.Count)
                mesh.SetUVs(1, data.Uv1);
            if (data.Uv2 != null && data.Uv2.Count == data.Vertices.Count)
                mesh.SetUVs(2, data.Uv2);

            SetFastRuntimeNormalsV8LikeAdapted(mesh, data.Vertices.Count);
            if (data.HasBounds)
                mesh.bounds = data.Bounds;
            else
                mesh.RecalculateBounds();
            bounds = mesh.bounds;
            return mesh;
        }

        private Mesh BuildFastDirectAtlasFallbackStripeMeshV8LikeAdapted(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            int startCellX,
            int endCellX,
            out Bounds bounds,
            out int baseTriCount)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var tris = new List<int>();
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            for (int cellY = kernel.MinCellY; cellY < kernel.MaxCellYExclusive; cellY++)
            {
                for (int cellX = startCellX; cellX < endCellX; cellX++)
                {
                    OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);
                    CellVertexPayloadLikeOriginal v0 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V0);
                    CellVertexPayloadLikeOriginal v1 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V1);
                    CellVertexPayloadLikeOriginal v2 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V2);
                    CellVertexPayloadLikeOriginal v3 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V3);

                    if (cell.FirstC == cell.V2)
                    {
                        AppendFastDirectAtlasTriangleV8LikeAdapted(map, cell, BaseSurfaceTriangleKindLikeOriginal.OddLeft, verts, colors, uv0, uv1, uv2, tris, v0, v1, v2);
                        AppendFastDirectAtlasTriangleV8LikeAdapted(map, cell, BaseSurfaceTriangleKindLikeOriginal.OddRight, verts, colors, uv0, uv1, uv2, tris, v2, v1, v3);
                    }
                    else
                    {
                        AppendFastDirectAtlasTriangleV8LikeAdapted(map, cell, BaseSurfaceTriangleKindLikeOriginal.EvenUpper, verts, colors, uv0, uv1, uv2, tris, v0, v1, v3);
                        AppendFastDirectAtlasTriangleV8LikeAdapted(map, cell, BaseSurfaceTriangleKindLikeOriginal.EvenLower, verts, colors, uv0, uv1, uv2, tris, v0, v3, v2);
                    }

                    EncapsulateFastBoundsV8LikeAdapted(ref bounds, ref hasBounds, v0.World);
                    EncapsulateFastBoundsV8LikeAdapted(ref bounds, ref hasBounds, v1.World);
                    EncapsulateFastBoundsV8LikeAdapted(ref bounds, ref hasBounds, v2.World);
                    EncapsulateFastBoundsV8LikeAdapted(ref bounds, ref hasBounds, v3.World);
                }
            }

            baseTriCount = tris.Count / 3;
            if (verts.Count == 0)
                return null;

            var mesh = new Mesh { name = $"FastV8_DirectAtlasFallback_{startCellX}_{endCellX}" };
            if (verts.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetUVs(2, uv2);
            mesh.SetTriangles(tris, 0, false);
            SetFastRuntimeNormalsV8LikeAdapted(mesh, verts.Count);
            if (hasBounds)
                mesh.bounds = bounds;
            else
                mesh.RecalculateBounds();
            bounds = mesh.bounds;
            return mesh;
        }

        private void AppendFastDirectAtlasTriangleV8LikeAdapted(
            ParsedMap map,
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            List<Vector3> verts,
            List<Color> colors,
            List<Vector2> uv0,
            List<Vector2> uv1,
            List<Vector2> uv2,
            List<int> tris,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c)
        {
            int tile = GetVertexTileLikeOriginal(map != null ? map.TexMap : null, a.Index) & 63;
            Vector2 ua;
            Vector2 ub;
            Vector2 uc;
            BuildFallbackTriangleUvV8LikeAdapted(kind, tile, a.Index, b.Index, c.Index, out ua, out ub, out uc);

            int baseIndex = verts.Count;
            verts.Add(a.World);
            verts.Add(b.World);
            verts.Add(c.World);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
            uv0.Add(ua);
            uv0.Add(ub);
            uv0.Add(uc);
            uv1.Add(Vector2.zero);
            uv1.Add(Vector2.zero);
            uv1.Add(Vector2.zero);
            uv2.Add(Vector2.zero);
            uv2.Add(Vector2.zero);
            uv2.Add(Vector2.zero);
            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
        }

        private void BuildFallbackTriangleUvV8LikeAdapted(
            BaseSurfaceTriangleKindLikeOriginal kind,
            int tile,
            int seedA,
            int seedB,
            int seedC,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            // Use the same atlas tile scale as the original helpers, but make this path
            // independent from the full descriptor builder so it cannot collapse to uv=0.
            tile &= 63;
            float baseU = GetGroundAtlasBaseULikeOriginal(tile);
            float baseV = GetGroundAtlasBaseVLikeOriginal(tile);
            float jitterU = (GetVValueLikeOriginal(seedA, 71) & 31) / (float)TriScaleLikeOriginal;
            float jitterV = (GetVValueLikeOriginal(seedB, 77) % VvvLikeOriginal) / (float)TriScaleLikeOriginal;
            float u0 = baseU + jitterU;
            float v0 = baseV + jitterV;
            float q = GroundAtlasTileSpanLikeOriginal;
            float h = GroundAtlasHalfSpanLikeOriginal;

            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    uvA = new Vector2(u0, v0);
                    uvB = new Vector2(u0 + q, v0 + h);
                    uvC = new Vector2(u0, v0 + q);
                    break;
                default:
                    uvA = new Vector2(u0, v0 + h);
                    uvB = new Vector2(u0 + q, v0);
                    uvC = new Vector2(u0 + q, v0 + q);
                    break;
            }

            ApplyGroundAtlasSafetyInsetLikeAdapted(tile, ref uvA, ref uvB, ref uvC);
        }

        private static void SetFastRuntimeNormalsV8LikeAdapted(Mesh mesh, int vertexCount)
        {
            if (mesh == null)
                return;
            var normals = new List<Vector3>(vertexCount);
            for (int i = 0; i < vertexCount; i++)
                normals.Add(Vector3.up);
            mesh.SetNormals(normals);
        }

        private static void EncapsulateFastBoundsV8LikeAdapted(ref Bounds bounds, ref bool hasBounds, Vector3 point)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(point);
            }
        }

        private static void ConfigureFastRuntimeRendererV8LikeAdapted(MeshRenderer mr)
        {
            if (mr == null)
                return;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            mr.allowOcclusionWhenDynamic = false;
        }
    }
}
