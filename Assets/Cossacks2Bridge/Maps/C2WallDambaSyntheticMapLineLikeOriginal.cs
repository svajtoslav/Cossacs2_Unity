using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2WallDambaSyntheticMapLineV93EnabledLikeOriginal = true;
        private const bool C2WallDambaSyntheticMapLineV93SuppressOriginalPiecesLikeOriginal = true;
        private const int C2WallDambaSyntheticMapLineV93MinRunSectionsLikeOriginal = 6;
        private const bool C2WallDambaSyntheticMapLineV95CullInternalConnectorCapsLikeOriginal = true;
        private const bool C2WallDambaSyntheticMapLineV96AddCameraStableTopSeamCoversLikeOriginal = false;
        private const float C2WallDambaSyntheticMapLineV96TopSeamCoverLiftWorldLikeOriginal = 0.035f;
        private const float C2WallDambaSyntheticMapLineV96TopSeamCoverWidthWorldLikeOriginal = 4.0f;
        private const float C2WallDambaSyntheticMapLineV96TopSeamCoverSideInsetWorldLikeOriginal = 2.0f;
        private const bool C2WallDambaSyntheticMapLineV97HideCameraSeamsByRowAxisEdgeOverlapLikeOriginal = false;
        private const float C2WallDambaSyntheticMapLineV97RowAxisEdgeOverlapWorldLikeOriginal = 0.22f;
        private const float C2WallDambaSyntheticMapLineV97RowAxisEdgeBandFractionLikeOriginal = 0.065f;
        private const bool C2WallDambaSyntheticMapLineV98CullCameraFightingInternalEndFacesLikeOriginal = true;
        private const float C2WallDambaSyntheticMapLineV98CapNormalDotRowMinLikeOriginal = 0.38f;
        private const float C2WallDambaSyntheticMapLineV98EndBandFractionLikeOriginal = 0.085f;
        private const bool C2WallDambaSyntheticMapLineV99UseCameraStableTextureSamplingLikeOriginal = true;
        private const int C2WallDambaSyntheticMapLineV99RgbBleedPassesLikeOriginal = 2;
        private const bool C2WallDambaSyntheticMapLineV100CullInternalTopSeamFacesLikeOriginal = false;
        private const float C2WallDambaSyntheticMapLineV100TopSeamBandFractionLikeOriginal = 0.065f;
        private const float C2WallDambaSyntheticMapLineV100TopFaceNormalDotUpMinLikeOriginal = 0.72f;
        private const bool C2WallDambaSyntheticMapLineV101UseDominantTopOverlapLikeOriginal = false;
        private const float C2WallDambaSyntheticMapLineV101ProjectionBandFractionLikeOriginal = 0.085f;
        private const float C2WallDambaSyntheticMapLineV101TopHeightBandFractionLikeOriginal = 0.42f;
        private const float C2WallDambaSyntheticMapLineV101DominantOverlapWorldLikeOriginal = 2.40f;
        private const float C2WallDambaSyntheticMapLineV101DominantTopLiftWorldLikeOriginal = 0.035f;
        private const bool C2WallDambaSyntheticMapLineV102UseDominantSeamOverlapLikeOriginal = true;
        private const float C2WallDambaSyntheticMapLineV102ProjectionBandFractionLikeOriginal = 0.18f;
        private const float C2WallDambaSyntheticMapLineV102TopHeightBandFractionLikeOriginal = 0.78f;
        private const float C2WallDambaSyntheticMapLineV102DominantOverlapWorldLikeOriginal = 6.00f;
        private const float C2WallDambaSyntheticMapLineV102DominantTopLiftWorldLikeOriginal = 0.18f;
        private const bool C2WallDambaSyntheticMapLineV102CullInternalSideSeamFacesLikeOriginal = true;
        private const float C2WallDambaSyntheticMapLineV102SideSeamBandFractionLikeOriginal = 0.14f;
        private const float C2WallDambaSyntheticMapLineV102SideFaceNormalDotSideMinLikeOriginal = 0.42f;
        private const float C2WallDambaSyntheticMapLineV102SideFaceNormalDotUpMaxLikeOriginal = 0.72f;
        private const bool C2WallDambaSyntheticMapLineV103CullInternalBackSeamFacesLikeOriginal = true;
        private const float C2WallDambaSyntheticMapLineV103ProjectionBandFractionLikeOriginal = 0.16f;
        private const float C2WallDambaSyntheticMapLineV103TopHeightBandFractionLikeOriginal = 0.62f;
        private const float C2WallDambaSyntheticMapLineV103SideEdgeBandFractionLikeOriginal = 0.18f;
        private const float C2WallDambaSyntheticMapLineV103InwardNormalDotSideMinLikeOriginal = 0.28f;
        private const float C2WallDambaSyntheticMapLineV103NormalDotUpMaxLikeOriginal = 0.84f;
        private const bool C2WallDambaSyntheticMapLineV104CullInternalBackStripFacesLikeOriginal = true;
        private const float C2WallDambaSyntheticMapLineV104TopHeightBandFractionLikeOriginal = 0.74f;
        private const float C2WallDambaSyntheticMapLineV104SideEdgeBandFractionLikeOriginal = 0.24f;
        private const float C2WallDambaSyntheticMapLineV104InwardNormalDotSideMinLikeOriginal = 0.20f;
        private const float C2WallDambaSyntheticMapLineV104NormalDotUpMaxLikeOriginal = 0.92f;
        private const bool C2WallDambaSyntheticMapLineV105CullHardBacksideStripFacesLikeOriginal = false;
        private const float C2WallDambaSyntheticMapLineV105TopHeightBandFractionLikeOriginal = 0.86f;
        private const float C2WallDambaSyntheticMapLineV105SideEdgeBandFractionLikeOriginal = 0.18f;
        private const float C2WallDambaSyntheticMapLineV105NormalDotUpMaxLikeOriginal = 0.82f;
        private const float C2WallDambaSyntheticMapLineV105NormalDotSideMinLikeOriginal = 0.05f;
        private const int C2WallDambaSyntheticMapLineV105BacksideModeW60LikeOriginal = 1;
        private const int C2WallDambaSyntheticMapLineV105BacksideModeW63LikeOriginal = -1;
        private const string C2WallDambaSyntheticMapLineV93ContractLikeOriginal = "V114_final_keep_v99_texture_v102_gap_closure_v111_save_disable_v105_hard_cull";

        private sealed class SyntheticDambaSavedPoseV93LikeOriginal
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        private struct SyntheticDambaRowSegmentV93LikeOriginal
        {
            public Vector2 A;
            public Vector2 B;
        }

        private readonly List<GameObject> _c2WallDambaSyntheticRowsV93LikeOriginal = new List<GameObject>();
        private Dictionary<string, SyntheticDambaSavedPoseV93LikeOriginal> _c2WallDambaSyntheticSavedPosesV93LikeOriginal;
        private string _c2WallDambaSyntheticSavedPoseMapKeyV93LikeOriginal;
        private string _c2WallDambaSyntheticPoseStatusV93LikeOriginal = string.Empty;
        private float _c2WallDambaSyntheticPoseStatusUntilV93LikeOriginal;

        private int BuildSyntheticDambaMapRowsV93LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            Transform parent,
            Dictionary<WallSavedMapSpriteV6LikeOriginal, float> modelRunHeightsV59,
            out HashSet<WallSavedMapSpriteV6LikeOriginal> suppressed)
        {
            suppressed = null;
            if (!C2WallDambaSyntheticMapLineV93EnabledLikeOriginal ||
                sprites == null ||
                catalog == null ||
                parent == null ||
                _map == null)
            {
                return 0;
            }

            int built = 0;
            _c2WallDambaSyntheticRowsV93LikeOriginal.Clear();
            EnsureSyntheticDambaSavedPosesLoadedV93LikeOriginal();
            suppressed = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
            var consumed = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
            var candidatesBySprite = new Dictionary<int, List<WallSavedMapSpriteV6LikeOriginal>>();
            var rowSegments = new List<SyntheticDambaRowSegmentV93LikeOriginal>();

            for (int i = 0; i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null ||
                    !catalog.ByIndex.TryGetValue(s.SpriteIndex, out WallSpriteDescV1LikeOriginal desc) ||
                    !IsSyntheticDambaLineDescV93LikeOriginal(desc))
                {
                    continue;
                }

                suppressed.Add(s);
                if (!candidatesBySprite.TryGetValue(s.SpriteIndex, out List<WallSavedMapSpriteV6LikeOriginal> list))
                {
                    list = new List<WallSavedMapSpriteV6LikeOriginal>();
                    candidatesBySprite[s.SpriteIndex] = list;
                }
                list.Add(s);
            }

            foreach (var kv in candidatesBySprite)
            {
                if (!catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal desc) || desc == null)
                    continue;

                List<List<WallSavedMapSpriteV6LikeOriginal>> components = BuildSyntheticDambaSpatialRunsV93LikeOriginal(kv.Value);
                for (int c = 0; c < components.Count; c++)
                {
                    List<WallSavedMapSpriteV6LikeOriginal> component = components[c];
                    if (component == null || component.Count < C2WallDambaSyntheticMapLineV93MinRunSectionsLikeOriginal)
                        continue;

                    SortSyntheticDambaComponentByMajorAxisV93LikeOriginal(component);
                    if (TryBuildSyntheticDambaRunMeshV93LikeOriginal(
                        component,
                        0,
                        component.Count,
                        desc,
                        catalog,
                        parent,
                        modelRunHeightsV59,
                        built,
                        out GameObject go))
                    {
                        built++;
                        rowSegments.Add(BuildSyntheticDambaRowSegmentV93LikeOriginal(component, 0, component.Count));
                        for (int k = 0; k < component.Count; k++)
                            consumed.Add(component[k]);
                    }
                }
            }

            // Fallback for maps where old WALLS export order already contains a useful run,
            // but the spatial cluster was too fragmented.
            for (int i = 0; i < sprites.Count;)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (first == null ||
                    !catalog.ByIndex.TryGetValue(first.SpriteIndex, out WallSpriteDescV1LikeOriginal desc) ||
                    !IsSyntheticDambaLineDescV93LikeOriginal(desc) ||
                    consumed.Contains(first))
                {
                    i++;
                    continue;
                }

                int spriteIndex = first.SpriteIndex;
                int j = i + 1;
                while (j < sprites.Count &&
                       sprites[j] != null &&
                       sprites[j].SpriteIndex == spriteIndex)
                {
                    j++;
                }

                int count = j - i;
                if (count >= C2WallDambaSyntheticMapLineV93MinRunSectionsLikeOriginal &&
                    TryBuildSyntheticDambaRunMeshV93LikeOriginal(
                        sprites,
                        i,
                        count,
                        desc,
                        catalog,
                        parent,
                        modelRunHeightsV59,
                        built,
                        out GameObject go))
                {
                    built++;
                    rowSegments.Add(BuildSyntheticDambaRowSegmentV93LikeOriginal(sprites, i, count));
                    if (C2WallDambaSyntheticMapLineV93SuppressOriginalPiecesLikeOriginal)
                    {
                        for (int k = i; k < j; k++)
                            suppressed.Add(sprites[k]);
                    }
                }

                i = j;
            }

            if (built == 0)
                suppressed.Clear();
            else
                SuppressBridgeSideSpritesNearSyntheticDambaRowsV93LikeOriginal(sprites, catalog, rowSegments, suppressed);

            return built;
        }

        private static SyntheticDambaRowSegmentV93LikeOriginal BuildSyntheticDambaRowSegmentV93LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int startIndex,
            int count)
        {
            int lastIndex = Mathf.Clamp(startIndex + count - 1, 0, sprites.Count - 1);
            WallSavedMapSpriteV6LikeOriginal a = sprites[Mathf.Clamp(startIndex, 0, sprites.Count - 1)];
            WallSavedMapSpriteV6LikeOriginal b = sprites[lastIndex];
            return new SyntheticDambaRowSegmentV93LikeOriginal
            {
                A = a != null ? new Vector2(a.X, a.Y) : Vector2.zero,
                B = b != null ? new Vector2(b.X, b.Y) : Vector2.zero
            };
        }

        private static void SuppressBridgeSideSpritesNearSyntheticDambaRowsV93LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            List<SyntheticDambaRowSegmentV93LikeOriginal> rowSegments,
            HashSet<WallSavedMapSpriteV6LikeOriginal> suppressed)
        {
            if (sprites == null || catalog == null || rowSegments == null || rowSegments.Count == 0 || suppressed == null)
                return;

            float baseStep = C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.magnitude;
            float maxDistance = Mathf.Max(72.0f, baseStep * 1.45f);
            float maxDistanceSq = maxDistance * maxDistance;
            float endMargin = Mathf.Max(80.0f, baseStep * 1.5f);

            for (int i = 0; i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null || suppressed.Contains(s))
                    continue;

                if (!catalog.ByIndex.TryGetValue(s.SpriteIndex, out WallSpriteDescV1LikeOriginal desc) ||
                    !IsSyntheticDambaBridgeSideSpriteV93LikeOriginal(desc))
                {
                    continue;
                }

                Vector2 p = new Vector2(s.X, s.Y);
                for (int r = 0; r < rowSegments.Count; r++)
                {
                    if (IsPointNearSyntheticDambaRowV93LikeOriginal(p, rowSegments[r], maxDistanceSq, endMargin))
                    {
                        suppressed.Add(s);
                        break;
                    }
                }
            }
        }

        private static bool IsPointNearSyntheticDambaRowV93LikeOriginal(
            Vector2 p,
            SyntheticDambaRowSegmentV93LikeOriginal row,
            float maxDistanceSq,
            float endMargin)
        {
            Vector2 ab = row.B - row.A;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1.0f)
                return false;

            float t = Vector2.Dot(p - row.A, ab) / lenSq;
            float len = Mathf.Sqrt(lenSq);
            float marginT = endMargin / Mathf.Max(1.0f, len);
            if (t < -marginT || t > 1.0f + marginT)
                return false;

            t = Mathf.Clamp01(t);
            Vector2 closest = row.A + ab * t;
            return (p - closest).sqrMagnitude <= maxDistanceSq;
        }

        private static bool IsSyntheticDambaBridgeSideSpriteV93LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;

            if (desc.SpriteIndex == 58 || desc.SpriteIndex == 59)
                return true;

            string name = desc.Name ?? string.Empty;
            return name.IndexOf("MOST", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   (name.IndexOf("W58", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("W59", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private List<List<WallSavedMapSpriteV6LikeOriginal>> BuildSyntheticDambaSpatialRunsV93LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> sprites)
        {
            var result = new List<List<WallSavedMapSpriteV6LikeOriginal>>();
            if (sprites == null || sprites.Count == 0)
                return result;

            float maxNeighbor = C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.magnitude * 1.75f;
            float maxNeighborSq = maxNeighbor * maxNeighbor;
            var used = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
            for (int i = 0; i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal seed = sprites[i];
                if (seed == null || used.Contains(seed))
                    continue;

                var component = new List<WallSavedMapSpriteV6LikeOriginal>();
                var queue = new Queue<WallSavedMapSpriteV6LikeOriginal>();
                used.Add(seed);
                queue.Enqueue(seed);

                while (queue.Count > 0)
                {
                    WallSavedMapSpriteV6LikeOriginal cur = queue.Dequeue();
                    component.Add(cur);
                    Vector2 curPos = new Vector2(cur.X, cur.Y);
                    for (int j = 0; j < sprites.Count; j++)
                    {
                        WallSavedMapSpriteV6LikeOriginal other = sprites[j];
                        if (other == null || used.Contains(other))
                            continue;
                        Vector2 otherPos = new Vector2(other.X, other.Y);
                        if ((otherPos - curPos).sqrMagnitude <= maxNeighborSq)
                        {
                            used.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }

                result.Add(component);
            }

            return result;
        }

        private static void SortSyntheticDambaComponentByMajorAxisV93LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> component)
        {
            if (component == null || component.Count < 2)
                return;

            int minX = component[0].X;
            int maxX = component[0].X;
            int minY = component[0].Y;
            int maxY = component[0].Y;
            for (int i = 1; i < component.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = component[i];
                if (s == null)
                    continue;
                minX = Mathf.Min(minX, s.X);
                maxX = Mathf.Max(maxX, s.X);
                minY = Mathf.Min(minY, s.Y);
                maxY = Mathf.Max(maxY, s.Y);
            }

            bool sortByX = Mathf.Abs(maxX - minX) >= Mathf.Abs(maxY - minY);
            component.Sort((a, b) =>
            {
                if (a == null || b == null)
                    return a == b ? 0 : (a == null ? -1 : 1);
                int major = sortByX ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
                if (major != 0)
                    return major;
                return sortByX ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
            });
        }

        private bool TryBuildSyntheticDambaRunMeshV93LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int startIndex,
            int count,
            WallSpriteDescV1LikeOriginal sourceDesc,
            WallSpriteCatalogV1LikeOriginal catalog,
            Transform parent,
            Dictionary<WallSavedMapSpriteV6LikeOriginal, float> modelRunHeightsV59,
            int runOrder,
            out GameObject go)
        {
            go = null;
            if (sprites == null || count < 2 || sourceDesc == null)
                return false;

            WallSpriteDescV1LikeOriginal desc = sourceDesc;
            if (desc == null || string.IsNullOrWhiteSpace(desc.ModelPath))
                return false;

            WallC2MParsedMeshV23LikeOriginal c2m = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out string loadAudit);
            if (c2m == null || c2m.Vertices == null || c2m.Vertices.Length == 0 || c2m.Triangles == null || c2m.Triangles.Length < 3)
            {
                return false;
            }

            int firstRunIndex = startIndex;
            int lastRunIndex = startIndex + count - 1;
            Vector2 first = new Vector2(sprites[firstRunIndex].X, sprites[firstRunIndex].Y);
            Vector2 last = new Vector2(sprites[lastRunIndex].X, sprites[lastRunIndex].Y);
            Vector2 row = last - first;
            if (row.sqrMagnitude < 1.0f)
                return false;

            Vector2 calibratedStep = C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal;
            if (Vector2.Dot(calibratedStep, row) < 0.0f)
                calibratedStep = -calibratedStep;

            float stepLength = calibratedStep.magnitude;
            if (stepLength < 0.001f)
                return false;

            Vector2 direction = row.normalized;
            Vector2 step = direction * stepLength;

            int syntheticCount = Mathf.Max(2, Mathf.RoundToInt(row.magnitude / stepLength) + 1);
            Vector2 center = (first + last) * 0.5f;
            Vector2 syntheticFirst = center - step * ((syntheticCount - 1) * 0.5f);
            float runHeight = ResolveSyntheticDambaRunHeightV93LikeOriginal(sprites, startIndex, count, modelRunHeightsV59);

            Mesh sectionMesh = BuildWallDambaCalibratorMeshV1LikeOriginal(desc, c2m, "C2_DAMBA_SYNTH_V93_SECTION_" + desc.Name);
            if (sectionMesh == null || sectionMesh.vertexCount == 0)
                return false;

            Vector3 worldOrigin = OriginalWallXYZToWorldV6LikeOriginal(
                syntheticFirst.x,
                syntheticFirst.y,
                runHeight + C2WallObjectsV60BridgeVerticalOffsetOriginal + desc.FixHeight);

            Mesh combined = BuildSyntheticDambaCombinedMeshV93LikeOriginal(
                sectionMesh,
                desc,
                step,
                syntheticCount,
                runOrder,
                out int skippedInternalConnectorCapsV95,
                out int seamCoverQuadsV96,
                out int rowAxisEdgeVertsAdjustedV97,
                out int internalEndFacesCulledV98,
                out int internalTopSeamFacesCulledV100,
                out int dominantTopOverlapVertsAdjustedV101,
                out int dominantSeamOverlapVertsAdjustedV102,
                out int internalSideSeamFacesCulledV102,
                out int internalBackSeamFacesCulledV103,
                out int internalBackStripFacesCulledV104,
                out int hardBacksideStripFacesCulledV105);
            if (combined == null)
                return false;

            go = new GameObject("C2_DAMBA_SYNTH_V93_" + runOrder.ToString("00", CultureInfo.InvariantCulture) + "_" + desc.Name + "_single_mesh");
            go.transform.SetParent(parent, false);
            go.transform.position = worldOrigin;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            ApplyWallRendererShadowContractV44LikeOriginal(mr);
            mf.sharedMesh = combined;

            Texture2D tex = TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(c2m, out _, out _);
            tex = PrepareSyntheticDambaTextureSamplingV99LikeOriginal(tex, desc, out string textureSamplingAuditV99);
            Material mat = CreateWallC2MModelMaterialV26LikeOriginal(tex, desc);
            if (mat != null)
            {
                mat.name = "C2_DAMBA_SYNTH_V93_MAT_" + desc.Name;
                ApplySyntheticDambaMaterialStabilityV99LikeOriginal(mat);
            }
            mr.sharedMaterial = mat;
            mr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(center.y), -32768, 32767);
            ApplySyntheticDambaSavedPoseV93LikeOriginal(go);
            _c2WallDambaSyntheticRowsV93LikeOriginal.Add(go);

            return true;
        }

        private static Texture2D PrepareSyntheticDambaTextureSamplingV99LikeOriginal(
            Texture2D source,
            WallSpriteDescV1LikeOriginal desc,
            out string audit)
        {
            audit = "disabled";
            if (!C2WallDambaSyntheticMapLineV99UseCameraStableTextureSamplingLikeOriginal)
                return source;

            if (source == null)
            {
                audit = "null_texture";
                return null;
            }

            try
            {
                Texture2D stable = TryCreateSyntheticDambaNoMipRgbBleedTextureV99LikeOriginal(source, desc, out string copyAudit);
                if (stable != null)
                {
                    audit = copyAudit;
                    return stable;
                }

                ApplySyntheticDambaTextureSamplerStateV99LikeOriginal(source);
                audit = "sampler_only_original_texture_no_copy";
                return source;
            }
            catch (Exception ex)
            {
                try { ApplySyntheticDambaTextureSamplerStateV99LikeOriginal(source); } catch { }
                audit = "sampler_only_after_exception_" + ex.GetType().Name;
                return source;
            }
        }

        private static Texture2D TryCreateSyntheticDambaNoMipRgbBleedTextureV99LikeOriginal(
            Texture2D source,
            WallSpriteDescV1LikeOriginal desc,
            out string audit)
        {
            audit = "copy_failed";
            if (source == null || source.width <= 0 || source.height <= 0)
                return null;

            Color32[] pixels;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (Exception ex)
            {
                audit = "source_not_readable_" + ex.GetType().Name;
                return null;
            }

            int w = source.width;
            int h = source.height;
            if (pixels == null || pixels.Length != w * h)
            {
                audit = "bad_pixels";
                return null;
            }

            Color32[] work = pixels;
            int changedRgb = 0;
            int passes = Mathf.Clamp(C2WallDambaSyntheticMapLineV99RgbBleedPassesLikeOriginal, 0, 8);
            for (int pass = 0; pass < passes; pass++)
            {
                Color32[] next = (Color32[])work.Clone();
                int passChanged = 0;
                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int idx = row + x;
                        Color32 c = work[idx];
                        if (c.a != 0)
                            continue;

                        int r = 0;
                        int g = 0;
                        int b = 0;
                        int n = 0;
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int yy = y + oy;
                            if (yy < 0 || yy >= h)
                                continue;

                            int yyRow = yy * w;
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0)
                                    continue;

                                int xx = x + ox;
                                if (xx < 0 || xx >= w)
                                    continue;

                                Color32 nc = work[yyRow + xx];
                                if (nc.a == 0)
                                    continue;

                                r += nc.r;
                                g += nc.g;
                                b += nc.b;
                                n++;
                            }
                        }

                        if (n <= 0)
                            continue;

                        next[idx] = new Color32(
                            (byte)Mathf.Clamp(r / n, 0, 255),
                            (byte)Mathf.Clamp(g / n, 0, 255),
                            (byte)Mathf.Clamp(b / n, 0, 255),
                            0);
                        passChanged++;
                    }
                }

                work = next;
                changedRgb += passChanged;
                if (passChanged == 0)
                    break;
            }

            Texture2D stable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            stable.name = (source.name ?? "DAMBA") + "_V99_no_mips_point_clamp";
            stable.SetPixels32(work);
            stable.Apply(false, false);
            ApplySyntheticDambaTextureSamplerStateV99LikeOriginal(stable);
            audit = "no_mips_point_clamp_rgb_bleed_passes=" + passes.ToString(CultureInfo.InvariantCulture) +
                    " rgbBleedPixels=" + changedRgb.ToString(CultureInfo.InvariantCulture) +
                    " size=" + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture) +
                    " sourceMipmaps=" + source.mipmapCount.ToString(CultureInfo.InvariantCulture);
            return stable;
        }

        private static void ApplySyntheticDambaTextureSamplerStateV99LikeOriginal(Texture tex)
        {
            if (tex == null)
                return;

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.anisoLevel = 0;
            tex.mipMapBias = -8.0f;
        }

        private static void ApplySyntheticDambaMaterialStabilityV99LikeOriginal(Material mat)
        {
            if (mat == null || !C2WallDambaSyntheticMapLineV99UseCameraStableTextureSamplingLikeOriginal)
                return;

            mat.renderQueue = C2WallObjectsV24ModelRenderQueueLikeOriginal;
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0.0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1.0f);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", 0.015f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.015f);
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }


        private Mesh BuildSyntheticDambaCombinedMeshV93LikeOriginal(
            Mesh sectionMesh,
            WallSpriteDescV1LikeOriginal desc,
            Vector2 step,
            int count,
            int runOrder,
            out int skippedInternalConnectorCapsV95,
            out int seamCoverQuadsV96,
            out int rowAxisEdgeVertsAdjustedV97,
            out int internalEndFacesCulledV98,
            out int internalTopSeamFacesCulledV100,
            out int dominantTopOverlapVertsAdjustedV101,
            out int dominantSeamOverlapVertsAdjustedV102,
            out int internalSideSeamFacesCulledV102,
            out int internalBackSeamFacesCulledV103,
            out int internalBackStripFacesCulledV104,
            out int hardBacksideStripFacesCulledV105)
        {
            skippedInternalConnectorCapsV95 = 0;
            seamCoverQuadsV96 = 0;
            rowAxisEdgeVertsAdjustedV97 = 0;
            internalEndFacesCulledV98 = 0;
            internalTopSeamFacesCulledV100 = 0;
            dominantTopOverlapVertsAdjustedV101 = 0;
            dominantSeamOverlapVertsAdjustedV102 = 0;
            internalSideSeamFacesCulledV102 = 0;
            internalBackSeamFacesCulledV103 = 0;
            internalBackStripFacesCulledV104 = 0;
            hardBacksideStripFacesCulledV105 = 0;

            Vector3[] sectionVerts = sectionMesh.vertices;
            int[] sectionTris = sectionMesh.triangles;
            Vector2[] sectionUv = sectionMesh.uv;
            Color32[] sectionColors = sectionMesh.colors32;
            if (sectionVerts == null || sectionVerts.Length == 0 || sectionTris == null || sectionTris.Length < 3)
                return null;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            Vector3 stepWorld = new Vector3(
                step.x * (kernel.BackingStepXWorld / 32.0f),
                0.0f,
                step.y * (kernel.BackingStepZWorld * WorldZSign / 32.0f));

            int hardBacksideModeV105 = 0;
            if (desc != null)
            {
                string descNameV105 = desc.Name ?? string.Empty;
                bool isW60V105 = desc.SpriteIndex == 60 || descNameV105.IndexOf("W60", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isW63V105 = desc.SpriteIndex == 63 || descNameV105.IndexOf("W63", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isW60V105)
                    hardBacksideModeV105 = C2WallDambaSyntheticMapLineV105BacksideModeW60LikeOriginal;
                else if (isW63V105)
                    hardBacksideModeV105 = C2WallDambaSyntheticMapLineV105BacksideModeW63LikeOriginal;
            }

            int baseVerts = sectionVerts.Length * count;
            bool addTopSeamCoversV96 =
                C2WallDambaSyntheticMapLineV96AddCameraStableTopSeamCoversLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;
            int seamCoverVertexCountV96 = addTopSeamCoversV96 ? (count - 1) * 4 : 0;
            int totalVerts = baseVerts + seamCoverVertexCountV96;
            var verts = new Vector3[totalVerts];
            Vector2[] uv = sectionUv != null && sectionUv.Length == sectionVerts.Length ? new Vector2[totalVerts] : null;
            Color32[] colors = sectionColors != null && sectionColors.Length == sectionVerts.Length ? new Color32[totalVerts] : null;

            bool canCullInternalCaps =
                C2WallDambaSyntheticMapLineV95CullInternalConnectorCapsLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canRowAxisEdgeOverlapV97 =
                C2WallDambaSyntheticMapLineV97HideCameraSeamsByRowAxisEdgeOverlapLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canCullInternalEndFacesV98 =
                C2WallDambaSyntheticMapLineV98CullCameraFightingInternalEndFacesLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canCullInternalTopSeamFacesV100 =
                C2WallDambaSyntheticMapLineV100CullInternalTopSeamFacesLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canDominantTopOverlapV101 =
                C2WallDambaSyntheticMapLineV101UseDominantTopOverlapLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canDominantSeamOverlapV102 =
                C2WallDambaSyntheticMapLineV102UseDominantSeamOverlapLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canCullInternalSideSeamFacesV102 =
                C2WallDambaSyntheticMapLineV102CullInternalSideSeamFacesLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canCullInternalBackSeamFacesV103 =
                C2WallDambaSyntheticMapLineV103CullInternalBackSeamFacesLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canCullInternalBackStripFacesV104 =
                C2WallDambaSyntheticMapLineV104CullInternalBackStripFacesLikeOriginal &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            bool canCullHardBacksideStripFacesV105 =
                C2WallDambaSyntheticMapLineV105CullHardBacksideStripFacesLikeOriginal &&
                hardBacksideModeV105 != 0 &&
                count > 1 &&
                stepWorld.sqrMagnitude > 0.000001f;

            Vector3 stepDir = (canCullInternalCaps || canRowAxisEdgeOverlapV97 || canCullInternalEndFacesV98 || canCullInternalTopSeamFacesV100 || canDominantTopOverlapV101 || canDominantSeamOverlapV102 || canCullInternalSideSeamFacesV102 || canCullInternalBackSeamFacesV103 || canCullInternalBackStripFacesV104 || canCullHardBacksideStripFacesV105) ? stepWorld.normalized : Vector3.right;
            float minProjection = 0.0f;
            float maxProjection = 0.0f;
            float capProjectionEpsilon = 0.0f;
            float robustEndBandV98 = 0.0f;
            float robustMaxSpanV98 = 0.0f;
            float rowAxisEdgeBandV97 = 0.0f;
            float rowAxisEdgeOverlapV97 = 0.0f;
            float topSeamBandV100 = 0.0f;
            float dominantProjectionBandV101 = 0.0f;
            float dominantTopBandYV101 = 0.0f;
            float dominantTopMaxYV101 = 0.0f;
            float dominantProjectionBandV102 = 0.0f;
            float dominantTopBandYV102 = 0.0f;
            float dominantTopMaxYV102 = 0.0f;
            float sideSeamBandV102 = 0.0f;
            float backProjectionBandV103 = 0.0f;
            float backTopBandYV103 = 0.0f;
            float backSideEdgeBandV103 = 0.0f;
            float backStripTopBandYV104 = 0.0f;
            float backStripSideEdgeBandV104 = 0.0f;
            float hardBacksideTopBandYV105 = 0.0f;
            float hardBacksideSideEdgeBandV105 = 0.0f;
            float minSideProjectionV103 = 0.0f;
            float maxSideProjectionV103 = 0.0f;
            Vector3 sideDirV102 = new Vector3(-stepDir.z, 0.0f, stepDir.x);
            if (sideDirV102.sqrMagnitude <= 0.000001f)
                sideDirV102 = Vector3.right;
            else
                sideDirV102.Normalize();

            if (canCullInternalCaps || canRowAxisEdgeOverlapV97 || canCullInternalEndFacesV98 || canCullInternalTopSeamFacesV100 || canDominantTopOverlapV101 || canDominantSeamOverlapV102 || canCullInternalSideSeamFacesV102 || canCullInternalBackSeamFacesV103 || canCullInternalBackStripFacesV104 || canCullHardBacksideStripFacesV105)
            {
                minProjection = Vector3.Dot(sectionVerts[0], stepDir);
                maxProjection = minProjection;
                for (int v = 1; v < sectionVerts.Length; v++)
                {
                    float p = Vector3.Dot(sectionVerts[v], stepDir);
                    if (p < minProjection) minProjection = p;
                    if (p > maxProjection) maxProjection = p;
                }

                float sectionExtent = Mathf.Max(0.001f, maxProjection - minProjection);
                capProjectionEpsilon = Mathf.Clamp(sectionExtent * 0.006f, 0.05f, 2.5f);
                robustEndBandV98 = Mathf.Clamp(
                    sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV98EndBandFractionLikeOriginal),
                    0.75f,
                    Mathf.Max(0.8f, sectionExtent * 0.22f));
                robustMaxSpanV98 = Mathf.Max(robustEndBandV98 * 2.25f, sectionExtent * 0.18f);
                rowAxisEdgeBandV97 = Mathf.Clamp(
                    sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV97RowAxisEdgeBandFractionLikeOriginal),
                    0.05f,
                    Mathf.Max(0.06f, sectionExtent * 0.25f));
                rowAxisEdgeOverlapV97 = Mathf.Clamp(
                    C2WallDambaSyntheticMapLineV97RowAxisEdgeOverlapWorldLikeOriginal,
                    0.0f,
                    Mathf.Max(0.001f, sectionExtent * 0.08f));
                topSeamBandV100 = Mathf.Clamp(
                    sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV100TopSeamBandFractionLikeOriginal),
                    0.4f,
                    Mathf.Max(0.5f, sectionExtent * 0.18f));
                dominantProjectionBandV101 = Mathf.Clamp(
                    sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV101ProjectionBandFractionLikeOriginal),
                    0.5f,
                    Mathf.Max(0.6f, sectionExtent * 0.25f));

                float minYV101 = sectionVerts[0].y;
                dominantTopMaxYV101 = minYV101;
                for (int v = 1; v < sectionVerts.Length; v++)
                {
                    float y = sectionVerts[v].y;
                    if (y < minYV101) minYV101 = y;
                    if (y > dominantTopMaxYV101) dominantTopMaxYV101 = y;
                }
                float yExtentV101 = Mathf.Max(0.001f, dominantTopMaxYV101 - minYV101);
                dominantTopBandYV101 = Mathf.Clamp(
                    yExtentV101 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV101TopHeightBandFractionLikeOriginal),
                    0.12f,
                    Mathf.Max(0.2f, yExtentV101 * 0.75f));

                dominantProjectionBandV102 = Mathf.Clamp(
                    sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV102ProjectionBandFractionLikeOriginal),
                    1.0f,
                    Mathf.Max(1.2f, sectionExtent * 0.40f));
                dominantTopMaxYV102 = dominantTopMaxYV101;
                dominantTopBandYV102 = Mathf.Clamp(
                    yExtentV101 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV102TopHeightBandFractionLikeOriginal),
                    0.20f,
                    Mathf.Max(0.4f, yExtentV101 * 0.95f));
                sideSeamBandV102 = Mathf.Clamp(
                    sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV102SideSeamBandFractionLikeOriginal),
                    0.8f,
                    Mathf.Max(1.0f, sectionExtent * 0.30f));

                minSideProjectionV103 = float.PositiveInfinity;
                maxSideProjectionV103 = float.NegativeInfinity;
                for (int sv = 0; sv < sectionVerts.Length; sv++)
                {
                    float sideProj = Vector3.Dot(sectionVerts[sv], sideDirV102);
                    if (sideProj < minSideProjectionV103) minSideProjectionV103 = sideProj;
                    if (sideProj > maxSideProjectionV103) maxSideProjectionV103 = sideProj;
                }
                if (!float.IsInfinity(minSideProjectionV103) && !float.IsInfinity(maxSideProjectionV103))
                {
                    float sideExtentV103 = Mathf.Max(0.001f, maxSideProjectionV103 - minSideProjectionV103);
                    backProjectionBandV103 = Mathf.Clamp(
                        sectionExtent * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV103ProjectionBandFractionLikeOriginal),
                        0.8f,
                        Mathf.Max(1.0f, sectionExtent * 0.32f));
                    backTopBandYV103 = Mathf.Clamp(
                        yExtentV101 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV103TopHeightBandFractionLikeOriginal),
                        0.18f,
                        Mathf.Max(0.30f, yExtentV101 * 0.90f));
                    backSideEdgeBandV103 = Mathf.Clamp(
                        sideExtentV103 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV103SideEdgeBandFractionLikeOriginal),
                        0.25f,
                        Mathf.Max(0.35f, sideExtentV103 * 0.40f));
                    backStripTopBandYV104 = Mathf.Clamp(
                        yExtentV101 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV104TopHeightBandFractionLikeOriginal),
                        0.22f,
                        Mathf.Max(0.35f, yExtentV101 * 0.98f));
                    backStripSideEdgeBandV104 = Mathf.Clamp(
                        sideExtentV103 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV104SideEdgeBandFractionLikeOriginal),
                        0.35f,
                        Mathf.Max(0.45f, sideExtentV103 * 0.50f));
                    hardBacksideTopBandYV105 = Mathf.Clamp(
                        yExtentV101 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV105TopHeightBandFractionLikeOriginal),
                        0.28f,
                        Mathf.Max(0.42f, yExtentV101));
                    hardBacksideSideEdgeBandV105 = Mathf.Clamp(
                        sideExtentV103 * Mathf.Max(0.001f, C2WallDambaSyntheticMapLineV105SideEdgeBandFractionLikeOriginal),
                        0.20f,
                        Mathf.Max(0.28f, sideExtentV103 * 0.38f));
                }
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 localOrigin = stepWorld * i;

                int vertexOffset = i * sectionVerts.Length;
                for (int v = 0; v < sectionVerts.Length; v++)
                {
                    Vector3 localVertex = sectionVerts[v];
                    float sourceProjectionV101 = Vector3.Dot(sectionVerts[v], stepDir);
                    if (canRowAxisEdgeOverlapV97 && rowAxisEdgeOverlapV97 > 0.0f && rowAxisEdgeBandV97 > 0.0f)
                    {
                        bool adjustedV97 = false;
                        localVertex = AdjustSyntheticDambaRowAxisEdgeOverlapV97LikeOriginal(
                            localVertex,
                            stepDir,
                            sourceProjectionV101,
                            minProjection,
                            maxProjection,
                            rowAxisEdgeBandV97,
                            rowAxisEdgeOverlapV97,
                            i > 0,
                            i < count - 1,
                            ref adjustedV97);
                        if (adjustedV97)
                            rowAxisEdgeVertsAdjustedV97++;
                    }

                    if (canDominantTopOverlapV101 && dominantProjectionBandV101 > 0.0f && dominantTopBandYV101 > 0.0f)
                    {
                        bool adjustedV101 = false;
                        localVertex = AdjustSyntheticDambaDominantTopOverlapV101LikeOriginal(
                            localVertex,
                            stepDir,
                            sourceProjectionV101,
                            maxProjection,
                            dominantProjectionBandV101,
                            dominantTopMaxYV101,
                            dominantTopBandYV101,
                            C2WallDambaSyntheticMapLineV101DominantOverlapWorldLikeOriginal,
                            C2WallDambaSyntheticMapLineV101DominantTopLiftWorldLikeOriginal,
                            i < count - 1,
                            ref adjustedV101);
                        if (adjustedV101)
                            dominantTopOverlapVertsAdjustedV101++;
                    }

                    if (canDominantSeamOverlapV102 && dominantProjectionBandV102 > 0.0f && dominantTopBandYV102 > 0.0f)
                    {
                        bool adjustedV102 = false;
                        localVertex = AdjustSyntheticDambaDominantSeamOverlapV102LikeOriginal(
                            localVertex,
                            stepDir,
                            sourceProjectionV101,
                            minProjection,
                            maxProjection,
                            dominantProjectionBandV102,
                            dominantTopMaxYV102,
                            dominantTopBandYV102,
                            C2WallDambaSyntheticMapLineV102DominantOverlapWorldLikeOriginal,
                            C2WallDambaSyntheticMapLineV102DominantTopLiftWorldLikeOriginal,
                            i > 0,
                            i < count - 1,
                            ref adjustedV102);
                        if (adjustedV102)
                            dominantSeamOverlapVertsAdjustedV102++;
                    }

                    verts[vertexOffset + v] = localOrigin + localVertex;
                    if (uv != null)
                        uv[vertexOffset + v] = sectionUv[v];
                    if (colors != null)
                        colors[vertexOffset + v] = sectionColors[v];
                }
            }

            var triList = new List<int>(sectionTris.Length * count + Mathf.Max(0, count - 1) * 6);
            for (int i = 0; i < count; i++)
            {
                int vertexOffset = i * sectionVerts.Length;
                for (int t = 0; t + 2 < sectionTris.Length; t += 3)
                {
                    int a = sectionTris[t];
                    int b = sectionTris[t + 1];
                    int c = sectionTris[t + 2];

                    if (canCullInternalCaps &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length)
                    {
                        float pa = Vector3.Dot(sectionVerts[a], stepDir);
                        float pb = Vector3.Dot(sectionVerts[b], stepDir);
                        float pc = Vector3.Dot(sectionVerts[c], stepDir);

                        bool minCap =
                            pa <= minProjection + capProjectionEpsilon &&
                            pb <= minProjection + capProjectionEpsilon &&
                            pc <= minProjection + capProjectionEpsilon;

                        bool maxCap =
                            pa >= maxProjection - capProjectionEpsilon &&
                            pb >= maxProjection - capProjectionEpsilon &&
                            pc >= maxProjection - capProjectionEpsilon;

                        // V95: every repeated C2M section contains its own end/cap faces.
                        // Inside a synthetic row those caps sit on the same depth as the neighbour section.
                        // With Unity depth precision they flicker as the camera moves: black/gray stitching lines.
                        // Keep only the two real outer caps and remove all internal connector caps.
                        if ((i > 0 && minCap) || (i < count - 1 && maxCap))
                        {
                            skippedInternalConnectorCapsV95++;
                            continue;
                        }
                    }

                    if (canCullInternalEndFacesV98 &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length &&
                        IsSyntheticDambaInternalEndFaceV98LikeOriginal(
                            sectionVerts[a],
                            sectionVerts[b],
                            sectionVerts[c],
                            stepDir,
                            minProjection,
                            maxProjection,
                            robustEndBandV98,
                            robustMaxSpanV98,
                            C2WallDambaSyntheticMapLineV98CapNormalDotRowMinLikeOriginal,
                            i > 0,
                            i < count - 1))
                    {
                        internalEndFacesCulledV98++;
                        continue;
                    }

                    if (canCullInternalTopSeamFacesV100 &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length &&
                        IsSyntheticDambaInternalTopSeamFaceV100LikeOriginal(
                            sectionVerts[a],
                            sectionVerts[b],
                            sectionVerts[c],
                            stepDir,
                            maxProjection,
                            topSeamBandV100,
                            C2WallDambaSyntheticMapLineV100TopFaceNormalDotUpMinLikeOriginal,
                            i < count - 1))
                    {
                        internalTopSeamFacesCulledV100++;
                        continue;
                    }

                    if (canCullInternalSideSeamFacesV102 &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length &&
                        IsSyntheticDambaInternalSideSeamFaceV102LikeOriginal(
                            sectionVerts[a],
                            sectionVerts[b],
                            sectionVerts[c],
                            stepDir,
                            sideDirV102,
                            minProjection,
                            maxProjection,
                            sideSeamBandV102,
                            C2WallDambaSyntheticMapLineV102SideFaceNormalDotSideMinLikeOriginal,
                            C2WallDambaSyntheticMapLineV102SideFaceNormalDotUpMaxLikeOriginal,
                            i > 0,
                            i < count - 1))
                    {
                        internalSideSeamFacesCulledV102++;
                        continue;
                    }

                    if (canCullInternalBackSeamFacesV103 &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length &&
                        IsSyntheticDambaInternalBackSeamFaceV103LikeOriginal(
                            sectionVerts[a],
                            sectionVerts[b],
                            sectionVerts[c],
                            stepDir,
                            sideDirV102,
                            minProjection,
                            maxProjection,
                            minSideProjectionV103,
                            maxSideProjectionV103,
                            backProjectionBandV103,
                            backSideEdgeBandV103,
                            dominantTopMaxYV102,
                            backTopBandYV103,
                            C2WallDambaSyntheticMapLineV103InwardNormalDotSideMinLikeOriginal,
                            C2WallDambaSyntheticMapLineV103NormalDotUpMaxLikeOriginal,
                            i > 0,
                            i < count - 1))
                    {
                        internalBackSeamFacesCulledV103++;
                        continue;
                    }

                    if (canCullInternalBackStripFacesV104 &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length &&
                        IsSyntheticDambaInternalBackStripFaceV104LikeOriginal(
                            sectionVerts[a],
                            sectionVerts[b],
                            sectionVerts[c],
                            sideDirV102,
                            minSideProjectionV103,
                            maxSideProjectionV103,
                            dominantTopMaxYV102,
                            backStripTopBandYV104,
                            backStripSideEdgeBandV104,
                            C2WallDambaSyntheticMapLineV104InwardNormalDotSideMinLikeOriginal,
                            C2WallDambaSyntheticMapLineV104NormalDotUpMaxLikeOriginal))
                    {
                        internalBackStripFacesCulledV104++;
                        continue;
                    }

                    if (canCullHardBacksideStripFacesV105 &&
                        a >= 0 && a < sectionVerts.Length &&
                        b >= 0 && b < sectionVerts.Length &&
                        c >= 0 && c < sectionVerts.Length &&
                        IsSyntheticDambaHardBacksideStripFaceV105LikeOriginal(
                            sectionVerts[a],
                            sectionVerts[b],
                            sectionVerts[c],
                            sideDirV102,
                            minSideProjectionV103,
                            maxSideProjectionV103,
                            dominantTopMaxYV102,
                            hardBacksideTopBandYV105,
                            hardBacksideSideEdgeBandV105,
                            hardBacksideModeV105,
                            C2WallDambaSyntheticMapLineV105NormalDotUpMaxLikeOriginal,
                            C2WallDambaSyntheticMapLineV105NormalDotSideMinLikeOriginal))
                    {
                        hardBacksideStripFacesCulledV105++;
                        continue;
                    }

                    triList.Add(vertexOffset + a);
                    triList.Add(vertexOffset + b);
                    triList.Add(vertexOffset + c);
                }
            }

            if (addTopSeamCoversV96)
            {
                seamCoverQuadsV96 = AddSyntheticDambaTopSeamCoversV96LikeOriginal(
                    sectionVerts,
                    sectionUv,
                    sectionColors,
                    stepWorld,
                    count,
                    verts,
                    uv,
                    colors,
                    triList,
                    baseVerts);
            }

            Mesh mesh = new Mesh { name = "C2_DAMBA_SYNTH_V105_RUN_" + runOrder.ToString("00", CultureInfo.InvariantCulture) + "_" + desc.Name };
            if (totalVerts > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.triangles = triList.ToArray();
            if (uv != null)
                mesh.uv = uv;
            if (colors != null)
                mesh.colors32 = colors;
            mesh.RecalculateBounds();
            try { mesh.RecalculateNormals(); } catch { }
            return mesh;
        }


        private static bool IsSyntheticDambaInternalEndFaceV98LikeOriginal(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 rowDir,
            float minProjection,
            float maxProjection,
            float endBand,
            float maxSpan,
            float minNormalDotRow,
            bool hasPreviousSection,
            bool hasNextSection)
        {
            if (rowDir.sqrMagnitude <= 0.000001f || endBand <= 0.000001f)
                return false;

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude <= 0.0000001f)
                return false;

            float normalDotRow = Mathf.Abs(Vector3.Dot(n.normalized, rowDir.normalized));
            if (normalDotRow < Mathf.Clamp01(minNormalDotRow))
                return false;

            float p0r = Vector3.Dot(p0, rowDir);
            float p1r = Vector3.Dot(p1, rowDir);
            float p2r = Vector3.Dot(p2, rowDir);
            float triMin = Mathf.Min(p0r, Mathf.Min(p1r, p2r));
            float triMax = Mathf.Max(p0r, Mathf.Max(p1r, p2r));
            float center = (p0r + p1r + p2r) * (1.0f / 3.0f);
            float span = triMax - triMin;
            if (span > Mathf.Max(endBand, maxSpan))
                return false;

            bool nearMinEnd = center <= minProjection + endBand;
            bool nearMaxEnd = center >= maxProjection - endBand;
            return (hasPreviousSection && nearMinEnd) || (hasNextSection && nearMaxEnd);
        }


        private static bool IsSyntheticDambaInternalTopSeamFaceV100LikeOriginal(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 rowDir,
            float maxProjection,
            float seamBand,
            float minNormalDotUp,
            bool hasNextSection)
        {
            if (!hasNextSection || rowDir.sqrMagnitude <= 0.000001f || seamBand <= 0.000001f)
                return false;

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude <= 0.0000001f)
                return false;

            n.Normalize();
            if (Mathf.Abs(Vector3.Dot(n, Vector3.up)) < Mathf.Clamp01(minNormalDotUp))
                return false;

            float p0r = Vector3.Dot(p0, rowDir);
            float p1r = Vector3.Dot(p1, rowDir);
            float p2r = Vector3.Dot(p2, rowDir);
            float center = (p0r + p1r + p2r) * (1.0f / 3.0f);
            if (center < maxProjection - seamBand)
                return false;

            return true;
        }


        private static bool IsSyntheticDambaInternalSideSeamFaceV102LikeOriginal(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 rowDir,
            Vector3 sideDir,
            float minProjection,
            float maxProjection,
            float seamBand,
            float minNormalDotSide,
            float maxNormalDotUp,
            bool hasPreviousSection,
            bool hasNextSection)
        {
            if (rowDir.sqrMagnitude <= 0.000001f || sideDir.sqrMagnitude <= 0.000001f || seamBand <= 0.000001f)
                return false;

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude <= 0.0000001f)
                return false;
            n.Normalize();

            float normalDotUp = Mathf.Abs(Vector3.Dot(n, Vector3.up));
            if (normalDotUp > Mathf.Clamp01(maxNormalDotUp))
                return false;

            float normalDotSide = Mathf.Abs(Vector3.Dot(n, sideDir));
            if (normalDotSide < Mathf.Clamp01(minNormalDotSide))
                return false;

            float p0r = Vector3.Dot(p0, rowDir);
            float p1r = Vector3.Dot(p1, rowDir);
            float p2r = Vector3.Dot(p2, rowDir);
            float center = (p0r + p1r + p2r) * (1.0f / 3.0f);
            bool nearMinEnd = center <= minProjection + seamBand;
            bool nearMaxEnd = center >= maxProjection - seamBand;
            return (hasPreviousSection && nearMinEnd) || (hasNextSection && nearMaxEnd);
        }


        private static bool IsSyntheticDambaHardBacksideStripFaceV105LikeOriginal(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 sideDir,
            float minSideProjection,
            float maxSideProjection,
            float maxY,
            float topBandY,
            float sideEdgeBand,
            int backsideMode,
            float maxNormalDotUp,
            float minNormalDotSide)
        {
            if (backsideMode == 0 || sideDir.sqrMagnitude <= 0.000001f || topBandY <= 0.000001f || sideEdgeBand <= 0.000001f)
                return false;

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude <= 0.0000001f)
                return false;
            n.Normalize();

            float normalDotUp = Mathf.Abs(Vector3.Dot(n, Vector3.up));
            if (normalDotUp > Mathf.Clamp01(maxNormalDotUp))
                return false;

            float normalDotSide = Mathf.Abs(Vector3.Dot(n, sideDir));
            if (normalDotSide < Mathf.Clamp01(minNormalDotSide))
                return false;

            float centerY = (p0.y + p1.y + p2.y) * (1.0f / 3.0f);
            if (maxY - centerY > topBandY)
                return false;

            float p0s = Vector3.Dot(p0, sideDir);
            float p1s = Vector3.Dot(p1, sideDir);
            float p2s = Vector3.Dot(p2, sideDir);
            float centerSide = (p0s + p1s + p2s) * (1.0f / 3.0f);

            if (backsideMode > 0)
                return centerSide >= maxSideProjection - sideEdgeBand;
            return centerSide <= minSideProjection + sideEdgeBand;
        }


        private static bool IsSyntheticDambaInternalBackStripFaceV104LikeOriginal(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 sideDir,
            float minSideProjection,
            float maxSideProjection,
            float maxY,
            float topBandY,
            float sideEdgeBand,
            float minInwardNormalDotSide,
            float maxNormalDotUp)
        {
            if (sideDir.sqrMagnitude <= 0.000001f || topBandY <= 0.000001f || sideEdgeBand <= 0.000001f)
                return false;

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude <= 0.0000001f)
                return false;
            n.Normalize();

            float normalDotUp = Mathf.Abs(Vector3.Dot(n, Vector3.up));
            if (normalDotUp > Mathf.Clamp01(maxNormalDotUp))
                return false;

            float centerY = (p0.y + p1.y + p2.y) * (1.0f / 3.0f);
            if (maxY - centerY > topBandY)
                return false;

            float p0s = Vector3.Dot(p0, sideDir);
            float p1s = Vector3.Dot(p1, sideDir);
            float p2s = Vector3.Dot(p2, sideDir);
            float centerSide = (p0s + p1s + p2s) * (1.0f / 3.0f);
            bool nearMinSide = centerSide <= minSideProjection + sideEdgeBand;
            bool nearMaxSide = centerSide >= maxSideProjection - sideEdgeBand;
            if (!(nearMinSide || nearMaxSide))
                return false;

            float sideMid = (minSideProjection + maxSideProjection) * 0.5f;
            float outwardSign = Mathf.Sign(centerSide - sideMid);
            if (Mathf.Abs(outwardSign) < 0.5f)
                return false;

            float signedOutwardDot = Vector3.Dot(n, sideDir) * outwardSign;
            if (signedOutwardDot > -Mathf.Clamp01(minInwardNormalDotSide))
                return false;

            return true;
        }


        private static bool IsSyntheticDambaInternalBackSeamFaceV103LikeOriginal(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 rowDir,
            Vector3 sideDir,
            float minProjection,
            float maxProjection,
            float minSideProjection,
            float maxSideProjection,
            float projectionBand,
            float sideEdgeBand,
            float maxY,
            float topBandY,
            float minInwardNormalDotSide,
            float maxNormalDotUp,
            bool hasPreviousSection,
            bool hasNextSection)
        {
            if (rowDir.sqrMagnitude <= 0.000001f || sideDir.sqrMagnitude <= 0.000001f)
                return false;
            if (projectionBand <= 0.000001f || sideEdgeBand <= 0.000001f || topBandY <= 0.000001f)
                return false;

            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude <= 0.0000001f)
                return false;
            n.Normalize();

            float normalDotUp = Mathf.Abs(Vector3.Dot(n, Vector3.up));
            if (normalDotUp > Mathf.Clamp01(maxNormalDotUp))
                return false;

            float p0r = Vector3.Dot(p0, rowDir);
            float p1r = Vector3.Dot(p1, rowDir);
            float p2r = Vector3.Dot(p2, rowDir);
            float centerRow = (p0r + p1r + p2r) * (1.0f / 3.0f);
            bool nearMinEnd = centerRow <= minProjection + projectionBand;
            bool nearMaxEnd = centerRow >= maxProjection - projectionBand;
            if (!((hasPreviousSection && nearMinEnd) || (hasNextSection && nearMaxEnd)))
                return false;

            float centerY = (p0.y + p1.y + p2.y) * (1.0f / 3.0f);
            if (maxY - centerY > topBandY)
                return false;

            float p0s = Vector3.Dot(p0, sideDir);
            float p1s = Vector3.Dot(p1, sideDir);
            float p2s = Vector3.Dot(p2, sideDir);
            float centerSide = (p0s + p1s + p2s) * (1.0f / 3.0f);
            bool nearMinSide = centerSide <= minSideProjection + sideEdgeBand;
            bool nearMaxSide = centerSide >= maxSideProjection - sideEdgeBand;
            if (!(nearMinSide || nearMaxSide))
                return false;

            float sideMid = (minSideProjection + maxSideProjection) * 0.5f;
            float outwardSign = Mathf.Sign(centerSide - sideMid);
            if (Mathf.Abs(outwardSign) < 0.5f)
                return false;

            float signedOutwardDot = Vector3.Dot(n, sideDir) * outwardSign;
            if (signedOutwardDot > -Mathf.Clamp01(minInwardNormalDotSide))
                return false;

            return true;
        }


        private static Vector3 AdjustSyntheticDambaDominantSeamOverlapV102LikeOriginal(
            Vector3 localVertex,
            Vector3 rowDir,
            float projection,
            float minProjection,
            float maxProjection,
            float projectionBand,
            float maxY,
            float topBandY,
            float overlap,
            float topLift,
            bool hasPreviousSection,
            bool hasNextSection,
            ref bool adjusted)
        {
            adjusted = false;
            if (projectionBand <= 0.000001f || overlap <= 0.000001f || rowDir.sqrMagnitude <= 0.000001f)
                return localVertex;

            float fromTop = maxY - localVertex.y;
            if (fromTop > topBandY)
                return localVertex;

            Vector3 result = localVertex;
            float topK = 1.0f - Mathf.Clamp01(fromTop / Mathf.Max(0.0001f, topBandY));
            if (hasPreviousSection)
            {
                float fromMin = projection - minProjection;
                if (fromMin <= projectionBand)
                {
                    float kProj = 1.0f - Mathf.Clamp01(fromMin / projectionBand);
                    float k = Mathf.Clamp01(Mathf.Max(kProj, topK));
                    result -= rowDir * (overlap * k);
                    result += Vector3.up * (topLift * k);
                    adjusted = true;
                }
            }

            if (hasNextSection)
            {
                float fromMax = maxProjection - projection;
                if (fromMax <= projectionBand)
                {
                    float kProj = 1.0f - Mathf.Clamp01(fromMax / projectionBand);
                    float k = Mathf.Clamp01(Mathf.Max(kProj, topK));
                    result += rowDir * (overlap * k);
                    result += Vector3.up * (topLift * k);
                    adjusted = true;
                }
            }

            return result;
        }


        private static Vector3 AdjustSyntheticDambaDominantTopOverlapV101LikeOriginal(
            Vector3 localVertex,
            Vector3 rowDir,
            float projection,
            float maxProjection,
            float projectionBand,
            float maxY,
            float topBandY,
            float overlap,
            float topLift,
            bool hasNextSection,
            ref bool adjusted)
        {
            adjusted = false;
            if (!hasNextSection || projectionBand <= 0.000001f || overlap <= 0.000001f || rowDir.sqrMagnitude <= 0.000001f)
                return localVertex;

            float fromMax = maxProjection - projection;
            if (fromMax > projectionBand)
                return localVertex;

            float fromTop = maxY - localVertex.y;
            if (fromTop > topBandY)
                return localVertex;

            float kProj = 1.0f - Mathf.Clamp01(fromMax / projectionBand);
            float kTop = 1.0f - Mathf.Clamp01(fromTop / Mathf.Max(0.0001f, topBandY));
            float k = Mathf.Clamp01(Mathf.Max(kProj, kTop));
            if (k <= 0.0f)
                return localVertex;

            adjusted = true;
            return localVertex + rowDir * (overlap * k) + Vector3.up * (topLift * k);
        }


        private static Vector3 AdjustSyntheticDambaRowAxisEdgeOverlapV97LikeOriginal(
            Vector3 localVertex,
            Vector3 rowDir,
            float projection,
            float minProjection,
            float maxProjection,
            float edgeBand,
            float overlap,
            bool hasPreviousSection,
            bool hasNextSection,
            ref bool adjusted)
        {
            adjusted = false;
            if (edgeBand <= 0.000001f || overlap <= 0.000001f || rowDir.sqrMagnitude <= 0.000001f)
                return localVertex;

            Vector3 result = localVertex;
            if (hasPreviousSection)
            {
                float fromMin = projection - minProjection;
                if (fromMin <= edgeBand)
                {
                    float k = 1.0f - Mathf.Clamp01(fromMin / edgeBand);
                    result -= rowDir * (overlap * k);
                    adjusted = true;
                }
            }

            if (hasNextSection)
            {
                float fromMax = maxProjection - projection;
                if (fromMax <= edgeBand)
                {
                    float k = 1.0f - Mathf.Clamp01(fromMax / edgeBand);
                    result += rowDir * (overlap * k);
                    adjusted = true;
                }
            }

            return result;
        }

        private static int AddSyntheticDambaTopSeamCoversV96LikeOriginal(
            Vector3[] sectionVerts,
            Vector2[] sectionUv,
            Color32[] sectionColors,
            Vector3 stepWorld,
            int count,
            Vector3[] verts,
            Vector2[] uv,
            Color32[] colors,
            List<int> triList,
            int firstCoverVertex)
        {
            if (sectionVerts == null ||
                sectionVerts.Length == 0 ||
                verts == null ||
                triList == null ||
                count < 2 ||
                stepWorld.sqrMagnitude <= 0.000001f)
            {
                return 0;
            }

            Vector3 rowDir = stepWorld.normalized;
            Vector3 sideDir = new Vector3(-rowDir.z, 0.0f, rowDir.x);
            if (sideDir.sqrMagnitude <= 0.000001f)
                sideDir = Vector3.right;
            sideDir.Normalize();

            float minSide = Vector3.Dot(sectionVerts[0], sideDir);
            float maxSide = minSide;
            float maxY = sectionVerts[0].y;
            for (int i = 1; i < sectionVerts.Length; i++)
            {
                Vector3 v = sectionVerts[i];
                float side = Vector3.Dot(v, sideDir);
                if (side < minSide) minSide = side;
                if (side > maxSide) maxSide = side;
                if (v.y > maxY) maxY = v.y;
            }

            float sideSpan = Mathf.Max(0.001f, maxSide - minSide);
            float inset = Mathf.Min(
                Mathf.Max(0.0f, C2WallDambaSyntheticMapLineV96TopSeamCoverSideInsetWorldLikeOriginal),
                sideSpan * 0.20f);
            float sideA = minSide + inset;
            float sideB = maxSide - inset;
            if (sideB <= sideA + 0.001f)
            {
                sideA = minSide;
                sideB = maxSide;
            }

            float stepLen = Mathf.Max(0.001f, stepWorld.magnitude);
            float coverHalfWidth = Mathf.Clamp(
                C2WallDambaSyntheticMapLineV96TopSeamCoverWidthWorldLikeOriginal * 0.5f,
                0.05f,
                Mathf.Max(0.06f, stepLen * 0.15f));
            float coverY = maxY + Mathf.Max(0.0f, C2WallDambaSyntheticMapLineV96TopSeamCoverLiftWorldLikeOriginal);
            Vector2 coverUv = ResolveSyntheticDambaTopSeamCoverUvV96LikeOriginal(sectionVerts, sectionUv, maxY);
            Color32 coverColor = ResolveSyntheticDambaTopSeamCoverColorV96LikeOriginal(sectionVerts, sectionColors, maxY);

            int emitted = 0;
            for (int seam = 1; seam < count; seam++)
            {
                // The C2M section is centered before V93 assembles the row.
                // So the visible crack is between two section centers, not at the next center itself.
                Vector3 seamCenter = stepWorld * (seam - 0.5f);
                Vector3 a0 = seamCenter - rowDir * coverHalfWidth + sideDir * sideA;
                Vector3 a1 = seamCenter + rowDir * coverHalfWidth + sideDir * sideA;
                Vector3 b1 = seamCenter + rowDir * coverHalfWidth + sideDir * sideB;
                Vector3 b0 = seamCenter - rowDir * coverHalfWidth + sideDir * sideB;
                a0.y = coverY;
                a1.y = coverY;
                b1.y = coverY;
                b0.y = coverY;

                int vi = firstCoverVertex + emitted * 4;
                if (vi + 3 >= verts.Length)
                    break;

                verts[vi + 0] = a0;
                verts[vi + 1] = a1;
                verts[vi + 2] = b1;
                verts[vi + 3] = b0;

                if (uv != null)
                {
                    uv[vi + 0] = coverUv;
                    uv[vi + 1] = coverUv;
                    uv[vi + 2] = coverUv;
                    uv[vi + 3] = coverUv;
                }

                if (colors != null)
                {
                    colors[vi + 0] = coverColor;
                    colors[vi + 1] = coverColor;
                    colors[vi + 2] = coverColor;
                    colors[vi + 3] = coverColor;
                }

                triList.Add(vi + 0);
                triList.Add(vi + 1);
                triList.Add(vi + 2);
                triList.Add(vi + 0);
                triList.Add(vi + 2);
                triList.Add(vi + 3);
                emitted++;
            }

            return emitted;
        }

        private static Vector2 ResolveSyntheticDambaTopSeamCoverUvV96LikeOriginal(
            Vector3[] sectionVerts,
            Vector2[] sectionUv,
            float maxY)
        {
            if (sectionVerts == null ||
                sectionUv == null ||
                sectionUv.Length != sectionVerts.Length ||
                sectionVerts.Length == 0)
            {
                return new Vector2(0.5f, 0.5f);
            }

            int best = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < sectionVerts.Length; i++)
            {
                float dy = Mathf.Abs(maxY - sectionVerts[i].y);
                Vector2 u = sectionUv[i];
                float centerPenalty = Mathf.Abs(u.x - 0.5f) + Mathf.Abs(u.y - 0.5f);
                float score = dy * 10.0f + centerPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            return sectionUv[best];
        }

        private static Color32 ResolveSyntheticDambaTopSeamCoverColorV96LikeOriginal(
            Vector3[] sectionVerts,
            Color32[] sectionColors,
            float maxY)
        {
            if (sectionVerts == null ||
                sectionColors == null ||
                sectionColors.Length != sectionVerts.Length ||
                sectionVerts.Length == 0)
            {
                return new Color32(255, 255, 255, 255);
            }

            int best = 0;
            float bestDy = float.MaxValue;
            for (int i = 0; i < sectionVerts.Length; i++)
            {
                float dy = Mathf.Abs(maxY - sectionVerts[i].y);
                if (dy < bestDy)
                {
                    bestDy = dy;
                    best = i;
                }
            }

            Color32 c = sectionColors[best];
            if (c.a == 0)
                c.a = 255;
            return c;
        }

        private float ResolveSyntheticDambaRunHeightV93LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int startIndex,
            int count,
            Dictionary<WallSavedMapSpriteV6LikeOriginal, float> modelRunHeightsV59)
        {
            float sum = 0.0f;
            int samples = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null)
                    continue;

                if (modelRunHeightsV59 != null && modelRunHeightsV59.TryGetValue(s, out float sharedHeight))
                    sum += sharedHeight;
                else
                    sum += SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y);
                samples++;
            }

            return samples > 0 ? sum / samples : 0.0f;
        }

        private static bool IsSyntheticDambaLineDescV93LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null || !IsWallDambaC2MModelV33LikeOriginal(desc))
                return false;

            int id = desc.SpriteIndex;
            if (id >= 60 && id <= 67)
                return true;

            string model = (desc.ModelPath ?? string.Empty).Replace('/', '\\');
            return model.EndsWith("dam_bottom.c2m", StringComparison.OrdinalIgnoreCase) ||
                   model.EndsWith("dam_top.c2m", StringComparison.OrdinalIgnoreCase) ||
                   model.EndsWith("dam1_bottom.c2m", StringComparison.OrdinalIgnoreCase) ||
                   model.EndsWith("dam1_left.c2m", StringComparison.OrdinalIgnoreCase) ||
                   model.EndsWith("dam1_right.c2m", StringComparison.OrdinalIgnoreCase) ||
                   model.IndexOf("\\dam", StringComparison.OrdinalIgnoreCase) >= 0;
        }

#if UNITY_EDITOR
        private void OnSyntheticDambaSceneGuiV93LikeOriginal(SceneView sceneView)
        {
            DrawSyntheticDambaSavePoseButtonV93LikeOriginal(true);
        }

        private void DrawSyntheticDambaSavePoseButtonV93LikeOriginal()
        {
            DrawSyntheticDambaSavePoseButtonV93LikeOriginal(false);
        }

        private void DrawSyntheticDambaSavePoseButtonV93LikeOriginal(bool wrapHandles)
        {
            if (_c2WallDambaSyntheticRowsV93LikeOriginal == null ||
                _c2WallDambaSyntheticRowsV93LikeOriginal.Count == 0)
            {
                return;
            }

            if (wrapHandles)
                Handles.BeginGUI();
            EnsureWals2DHeightInstructionLoadedV178LikeOriginal();
            GUILayout.BeginArea(new Rect(12, 218, 330, 178), "C2 DAMBA / WALS2D V178", GUI.skin.window);

            GUILayout.Label("Высота вертикалей: " + _c2Wals2DVerticalRaisePixelsV178LikeOriginal.ToString("0.#", CultureInfo.InvariantCulture));
            float newVertical = GUILayout.HorizontalSlider(_c2Wals2DVerticalRaisePixelsV178LikeOriginal, C2WallObjectsV178HeightSliderMinLikeOriginal, C2WallObjectsV178HeightSliderMaxLikeOriginal);
            newVertical = Mathf.Round(newVertical * 2.0f) * 0.5f;

            GUILayout.Label("Высота горизонталей: " + _c2Wals2DHorizontalRaisePixelsV178LikeOriginal.ToString("0.#", CultureInfo.InvariantCulture));
            float newHorizontal = GUILayout.HorizontalSlider(_c2Wals2DHorizontalRaisePixelsV178LikeOriginal, C2WallObjectsV178HeightSliderMinLikeOriginal, C2WallObjectsV178HeightSliderMaxLikeOriginal);
            newHorizontal = Mathf.Round(newHorizontal * 2.0f) * 0.5f;

            if (Mathf.Abs(newVertical - _c2Wals2DVerticalRaisePixelsV178LikeOriginal) > 0.0001f ||
                Mathf.Abs(newHorizontal - _c2Wals2DHorizontalRaisePixelsV178LikeOriginal) > 0.0001f)
            {
                _c2Wals2DVerticalRaisePixelsV178LikeOriginal = newVertical;
                _c2Wals2DHorizontalRaisePixelsV178LikeOriginal = newHorizontal;
                ApplyWals2DHeightSlidersToLiveMeshesV178LikeOriginal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Сброс высот", GUILayout.Height(24)))
            {
                _c2Wals2DVerticalRaisePixelsV178LikeOriginal = 0.0f;
                _c2Wals2DHorizontalRaisePixelsV178LikeOriginal = 0.0f;
                ApplyWals2DHeightSlidersToLiveMeshesV178LikeOriginal();
            }

            if (GUILayout.Button("Сохранить позицию", GUILayout.Height(24)))
                SaveSyntheticDambaPosesV93LikeOriginal();
            GUILayout.EndHorizontal();

            string status = Time.realtimeSinceStartup < _c2WallDambaSyntheticPoseStatusUntilV93LikeOriginal
                ? _c2WallDambaSyntheticPoseStatusV93LikeOriginal
                : "Move V93 line objects / tune WALS2D heights, then save";
            GUILayout.Label(status);
            GUILayout.EndArea();
            if (wrapHandles)
                Handles.EndGUI();
        }
#endif

        private void ApplySyntheticDambaSavedPoseV93LikeOriginal(GameObject go)
        {
            if (go == null)
                return;

            EnsureSyntheticDambaSavedPosesLoadedV93LikeOriginal();
            if (_c2WallDambaSyntheticSavedPosesV93LikeOriginal != null &&
                _c2WallDambaSyntheticSavedPosesV93LikeOriginal.TryGetValue(go.name, out SyntheticDambaSavedPoseV93LikeOriginal pose) &&
                pose != null)
            {
                go.transform.position = pose.Position;
                go.transform.rotation = pose.Rotation;
                go.transform.localScale = pose.Scale;
            }
        }

        private void EnsureSyntheticDambaSavedPosesLoadedV93LikeOriginal()
        {
            string key = ResolveSyntheticDambaPoseMapKeyV93LikeOriginal();
            if (_c2WallDambaSyntheticSavedPosesV93LikeOriginal != null &&
                string.Equals(_c2WallDambaSyntheticSavedPoseMapKeyV93LikeOriginal, key, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _c2WallDambaSyntheticSavedPoseMapKeyV93LikeOriginal = key;
            _c2WallDambaSyntheticSavedPosesV93LikeOriginal = new Dictionary<string, SyntheticDambaSavedPoseV93LikeOriginal>(StringComparer.OrdinalIgnoreCase);

            // Legacy fallback first.
            LoadSyntheticDambaPoseFileV93LikeOriginal(ResolveSyntheticDambaPoseProjectPathV93LikeOriginal(), _c2WallDambaSyntheticSavedPosesV93LikeOriginal);
            LoadSyntheticDambaPoseFileV93LikeOriginal(ResolveSyntheticDambaPosePersistentPathV93LikeOriginal(), _c2WallDambaSyntheticSavedPosesV93LikeOriginal);

            // V111: map-local instruction is authoritative and overrides legacy calibration files.
            LoadSyntheticDambaPoseFileV93LikeOriginal(ResolveSyntheticDambaPoseMapSidecarPathV111LikeOriginal(), _c2WallDambaSyntheticSavedPosesV93LikeOriginal);
        }

        private void SaveSyntheticDambaPosesV93LikeOriginal()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# C2 DAMBA synthetic V111 map-local instruction");
                sb.AppendLine("# Saved next to .m3d when possible; legacy calibration paths are fallback only.");
                sb.AppendLine("map=" + ResolveSyntheticDambaPoseMapKeyV93LikeOriginal());
                sb.AppendLine("mapPath=" + (_mapRelativePath ?? string.Empty));
                sb.AppendLine("mapInstructionPath=" + ResolveSyntheticDambaPoseMapSidecarPathV111LikeOriginal());
                sb.AppendLine("contract=" + C2WallDambaSyntheticMapLineV93ContractLikeOriginal);
                sb.AppendLine("wals2dHeightContract=" + C2WallObjectsV175WLSavedSpriteSideShadowLiftContractLikeOriginal);
                sb.AppendLine("wals2dVerticalRaisePx=" + _c2Wals2DVerticalRaisePixelsV178LikeOriginal.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("wals2dHorizontalRaisePx=" + _c2Wals2DHorizontalRaisePixelsV178LikeOriginal.ToString("R", CultureInfo.InvariantCulture));

                int saved = 0;
                for (int i = 0; i < _c2WallDambaSyntheticRowsV93LikeOriginal.Count; i++)
                {
                    GameObject go = _c2WallDambaSyntheticRowsV93LikeOriginal[i];
                    if (go == null)
                        continue;

                    Transform tr = go.transform;
                    sb.AppendLine();
                    sb.AppendLine("row=" + go.name);
                    sb.AppendLine("position=" + FormatSyntheticDambaVector3V93LikeOriginal(tr.position));
                    sb.AppendLine("rotation=" + FormatSyntheticDambaQuaternionV93LikeOriginal(tr.rotation));
                    sb.AppendLine("scale=" + FormatSyntheticDambaVector3V93LikeOriginal(tr.localScale));
                    saved++;
                }

                string text = sb.ToString();
                string mapSidecarPath = ResolveSyntheticDambaPoseMapSidecarPathV111LikeOriginal();
                string persistentPath = ResolveSyntheticDambaPosePersistentPathV93LikeOriginal();
                string projectPath = ResolveSyntheticDambaPoseProjectPathV93LikeOriginal();

                var writtenPaths = new List<string>();
                if (TryWriteSyntheticDambaPoseFileV111LikeOriginal(mapSidecarPath, text, out string mapWriteError))
                {
                    writtenPaths.Add(mapSidecarPath);
                }
                else
                {
                    // If the real map folder is not writable, keep old locations as fallback instead of losing the manual work.
                    if (TryWriteSyntheticDambaPoseFileV111LikeOriginal(projectPath, text, out _))
                        writtenPaths.Add(projectPath);
                    if (TryWriteSyntheticDambaPoseFileV111LikeOriginal(persistentPath, text, out _))
                        writtenPaths.Add(persistentPath);

                }

                _c2WallDambaSyntheticSavedPosesV93LikeOriginal = null;
                _c2Wals2DHeightInstructionLoadedV178LikeOriginal = false;
                EnsureSyntheticDambaSavedPosesLoadedV93LikeOriginal();
                EnsureWals2DHeightInstructionLoadedV178LikeOriginal();
                ApplyWals2DHeightSlidersToLiveMeshesV178LikeOriginal();
                _c2WallDambaSyntheticPoseStatusV93LikeOriginal = writtenPaths.Count > 0
                    ? "Saved rows: " + saved.ToString(CultureInfo.InvariantCulture) + " -> " + writtenPaths[0]
                    : "Save failed: no writable path";
                _c2WallDambaSyntheticPoseStatusUntilV93LikeOriginal = Time.realtimeSinceStartup + 6.0f;
            }
            catch (Exception ex)
            {
                _c2WallDambaSyntheticPoseStatusV93LikeOriginal = "Save failed: " + ex.Message;
                _c2WallDambaSyntheticPoseStatusUntilV93LikeOriginal = Time.realtimeSinceStartup + 6.0f;
            }
        }

        private void EnsureWals2DHeightInstructionLoadedV178LikeOriginal()
        {
            if (_c2Wals2DHeightInstructionLoadedV178LikeOriginal)
                return;

            _c2Wals2DHeightInstructionLoadedV178LikeOriginal = true;
            _c2Wals2DVerticalRaisePixelsV178LikeOriginal = C2WallObjectsV35VerticalFenceRaisePixelsLikeOriginal;
            _c2Wals2DHorizontalRaisePixelsV178LikeOriginal = C2WallObjectsV178DefaultHorizontalFenceRaisePixelsLikeOriginal;

            LoadWals2DHeightInstructionFileV178LikeOriginal(ResolveSyntheticDambaPoseProjectPathV93LikeOriginal());
            LoadWals2DHeightInstructionFileV178LikeOriginal(ResolveSyntheticDambaPosePersistentPathV93LikeOriginal());
            LoadWals2DHeightInstructionFileV178LikeOriginal(ResolveSyntheticDambaPoseMapSidecarPathV111LikeOriginal());
        }

        private void LoadWals2DHeightInstructionFileV178LikeOriginal(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                line = line.Trim();
                if (line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Equals("wals2dVerticalRaisePx", StringComparison.OrdinalIgnoreCase))
                    _c2Wals2DVerticalRaisePixelsV178LikeOriginal = ClampWals2DHeightInstructionV178LikeOriginal(ParseWals2DFloatV178LikeOriginal(value, _c2Wals2DVerticalRaisePixelsV178LikeOriginal));
                else if (key.Equals("wals2dHorizontalRaisePx", StringComparison.OrdinalIgnoreCase))
                    _c2Wals2DHorizontalRaisePixelsV178LikeOriginal = ClampWals2DHeightInstructionV178LikeOriginal(ParseWals2DFloatV178LikeOriginal(value, _c2Wals2DHorizontalRaisePixelsV178LikeOriginal));
            }
        }

        private static float ParseWals2DFloatV178LikeOriginal(string value, float fallback)
        {
            if (!float.TryParse(value ?? string.Empty, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return fallback;
            return parsed;
        }

        private static float ClampWals2DHeightInstructionV178LikeOriginal(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0.0f;
            return Mathf.Clamp(value, C2WallObjectsV178HeightSliderMinLikeOriginal, C2WallObjectsV178HeightSliderMaxLikeOriginal);
        }

        private static void LoadSyntheticDambaPoseFileV93LikeOriginal(
            string path,
            Dictionary<string, SyntheticDambaSavedPoseV93LikeOriginal> poses)
        {
            if (string.IsNullOrWhiteSpace(path) || poses == null || !File.Exists(path))
                return;

            string currentRow = null;
            SyntheticDambaSavedPoseV93LikeOriginal currentPose = null;
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line = line.Trim();
                if (line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Equals("row", StringComparison.OrdinalIgnoreCase))
                {
                    currentRow = value;
                    currentPose = new SyntheticDambaSavedPoseV93LikeOriginal
                    {
                        Position = Vector3.zero,
                        Rotation = Quaternion.identity,
                        Scale = Vector3.one
                    };
                    poses[currentRow] = currentPose;
                }
                else if (currentPose != null && key.Equals("position", StringComparison.OrdinalIgnoreCase))
                {
                    currentPose.Position = ParseSyntheticDambaVector3V93LikeOriginal(value, currentPose.Position);
                }
                else if (currentPose != null && key.Equals("rotation", StringComparison.OrdinalIgnoreCase))
                {
                    currentPose.Rotation = ParseSyntheticDambaQuaternionV93LikeOriginal(value, currentPose.Rotation);
                }
                else if (currentPose != null && key.Equals("scale", StringComparison.OrdinalIgnoreCase))
                {
                    currentPose.Scale = ParseSyntheticDambaVector3V93LikeOriginal(value, currentPose.Scale);
                }
            }
        }

        private static bool TryWriteSyntheticDambaPoseFileV111LikeOriginal(string path, string content, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "empty_path";
                return false;
            }

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, content ?? string.Empty, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private string ResolveSyntheticDambaPoseMapSidecarPathV111LikeOriginal()
        {
            string mapAbs = ResolveSyntheticDambaAbsoluteMapPathV111LikeOriginal();
            if (string.IsNullOrWhiteSpace(mapAbs))
                return string.Empty;

            string dir = Path.GetDirectoryName(mapAbs);
            if (string.IsNullOrWhiteSpace(dir))
                return string.Empty;

            string fileName = Path.GetFileNameWithoutExtension(mapAbs);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = ResolveSyntheticDambaPoseMapKeyV93LikeOriginal();

            return Path.Combine(dir, fileName + ".c2bridge_damba_instruction.txt");
        }

        private string ResolveSyntheticDambaAbsoluteMapPathV111LikeOriginal()
        {
            try
            {
                if (_bootstrap != null &&
                    _bootstrap.Fs != null &&
                    !string.IsNullOrWhiteSpace(_mapRelativePath))
                {
                    string resolved = _bootstrap.Fs.ResolvePath(_mapRelativePath);
                    if (!string.IsNullOrWhiteSpace(resolved))
                        return resolved;
                }
            }
            catch
            {
                // Keep legacy fallback paths alive if CoreFileSystem cannot resolve this map.
            }

            return string.Empty;
        }

        private string ResolveSyntheticDambaPosePersistentPathV93LikeOriginal()
        {
            string dir = Path.Combine(Application.persistentDataPath, "C2WallCalibration");
            return Path.Combine(dir, ResolveSyntheticDambaPoseMapKeyV93LikeOriginal() + "_damba_synthetic_v93_poses.txt");
        }

        private string ResolveSyntheticDambaPoseProjectPathV93LikeOriginal()
        {
            string dir = Path.Combine(Application.dataPath, "Cossacks2Bridge", "Maps", "C2WallCalibration");
            return Path.Combine(dir, ResolveSyntheticDambaPoseMapKeyV93LikeOriginal() + "_damba_synthetic_v93_poses.txt");
        }

        private string ResolveSyntheticDambaPoseMapKeyV93LikeOriginal()
        {
            string fileName = !string.IsNullOrWhiteSpace(_mapRelativePath)
                ? Path.GetFileNameWithoutExtension(_mapRelativePath)
                : "current_map";
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                fileName = fileName.Replace(invalid[i], '_');
            return fileName;
        }

        private static string FormatSyntheticDambaVector3V93LikeOriginal(Vector3 v)
        {
            return v.x.ToString("R", CultureInfo.InvariantCulture) + "," +
                   v.y.ToString("R", CultureInfo.InvariantCulture) + "," +
                   v.z.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string FormatSyntheticDambaQuaternionV93LikeOriginal(Quaternion q)
        {
            return q.x.ToString("R", CultureInfo.InvariantCulture) + "," +
                   q.y.ToString("R", CultureInfo.InvariantCulture) + "," +
                   q.z.ToString("R", CultureInfo.InvariantCulture) + "," +
                   q.w.ToString("R", CultureInfo.InvariantCulture);
        }

        private static Vector3 ParseSyntheticDambaVector3V93LikeOriginal(string value, Vector3 fallback)
        {
            string[] parts = (value ?? string.Empty).Split(',');
            if (parts.Length != 3 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return fallback;
            }
            return new Vector3(x, y, z);
        }

        private static Quaternion ParseSyntheticDambaQuaternionV93LikeOriginal(string value, Quaternion fallback)
        {
            string[] parts = (value ?? string.Empty).Split(',');
            if (parts.Length != 4 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z) ||
                !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
            {
                return fallback;
            }
            return new Quaternion(x, y, z, w);
        }

    }
}
