using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2RoadsNetTextureV3LikeOriginal = true;
        private const bool C2RoadsBodyUnderlayV6LikeOriginal = false; // V8: disabled for loaded road-net; original LoadRoadsNet sets InLoadRoadsNet=true, so SurroundWithTexture() does not run.
        private const float C2RoadsNetTextureYOffsetV3LikeOriginal = 2.25f; // V17: keep stable lift; occlusion fix is reduced depth bias, not extra Y.
        private const int C2RoadsNetTextureCrossSlicesV14LikeOriginal = 8; // V17: 9 vertices across road width; exact per-vertex terrain-triangle height remains.
        private const float C2RoadsNetTextureLongStepV14LikeOriginal = 8.0f; // V17: dense along-road sampling; no camera-depth hack.
        private const int C2RoadsNetTextureMaxStationsPerRoadV14LikeOriginal = 4096;
        private const int C2RoadsNetTextureMinPointsV3LikeOriginal = 3;
        private const int C2RoadsNetTextureMaxGeneratedRoadsV3LikeOriginal = 65536;
        private const int C2RoadsNetTextureMaxCurvePointsV3LikeOriginal = 2046;

        private sealed class GeneratedRoadLikeOriginal
        {
            public int Type;
            public int Width;
            public readonly List<Vector2> Points = new List<Vector2>(128);
            public readonly List<Color32> Weights = new List<Color32>(256);
            public int XMin = int.MaxValue;
            public int YMin = int.MaxValue;
            public int XMax = int.MinValue;
            public int YMax = int.MinValue;

            public void AddPoint(Vector2 p, Color32 left, Color32 right)
            {
                Points.Add(p);
                Weights.Add(left);
                Weights.Add(right);

                int x = Mathf.RoundToInt(p.x);
                int y = Mathf.RoundToInt(p.y);
                if (x < XMin) XMin = x;
                if (x > XMax) XMax = x;
                if (y < YMin) YMin = y;
                if (y > YMax) YMax = y;
            }
        }

        private struct RoadAngleSortLikeOriginal
        {
            public int Idx;
            public int Angle;
            public int RType;
        }

        private void BuildRoadsNetTextureLayerLikeOriginal(ParsedMap map, Transform parent, ref Bounds terrainBounds)
        {
            if (!C2RoadsLayerV1LikeOriginal || !C2RoadsNetTextureV3LikeOriginal || map == null || !map.HasRoadNet || map.RoadKnots == null || map.RoadKnots.Length == 0 || parent == null)
                return;

            List<RoadDescLikeOriginal> descs = LoadRoadDescsLikeOriginal();
            bool usedFallbackDescs = false;
            if (descs == null || descs.Count == 0)
            {
                descs = BuildFallbackRoadDescsLikeOriginal();
                usedFallbackDescs = true;
            }

            var generated = GenerateRoadsFromNetLikeOriginal(map, descs, out int straightRoads, out int junctionRoads, out int skippedLinks, out int skippedJunctions);
            if (generated == null || generated.Count == 0)
            {
                return;
            }

            OriginalTerrainKernelConfig kernel = GetBoundsKernelLikeOriginal(map);
            var buckets = new Dictionary<int, RoadMeshBucketLikeOriginal>(64);
            var bodyBuckets = new Dictionary<int, RoadMeshBucketLikeOriginal>(64);
            TerrainTextureResourcesLikeOriginal bodyResources = TryLoadTerrainSurfaceResourcesLikeOriginal();
            int emittedRoads = 0;
            int emittedVertices = 0;
            int emittedTriangles = 0;
            int emittedBodyRoads = 0;
            int emittedBodyVertices = 0;
            int emittedBodyTriangles = 0;

            for (int i = 0; i < generated.Count; i++)
            {
                GeneratedRoadLikeOriginal road = generated[i];
                if (road == null || road.Points.Count < C2RoadsNetTextureMinPointsV3LikeOriginal)
                    continue;

                RoadDescLikeOriginal desc = GetRoadDescLikeOriginal(descs, road.Type);
                if (desc == null)
                    continue;

                if (!buckets.TryGetValue(desc.Type, out RoadMeshBucketLikeOriginal bucket))
                {
                    bucket = new RoadMeshBucketLikeOriginal(desc.Type, desc);
                    buckets.Add(desc.Type, bucket);
                }

                int beforeV = bucket.Vertices.Count;
                int beforeI = bucket.Triangles.Count;
                if (AppendGeneratedRoadMeshLikeOriginal(map, kernel, bucket, road, desc))
                {
                    emittedRoads++;
                    emittedVertices += bucket.Vertices.Count - beforeV;
                    emittedTriangles += (bucket.Triangles.Count - beforeI) / 3;
                }

                if (C2RoadsBodyUnderlayV6LikeOriginal && ShouldEmitRoadBodyUnderlayV6LikeOriginal(desc))
                {
                    if (!bodyBuckets.TryGetValue(desc.Type, out RoadMeshBucketLikeOriginal bodyBucket))
                    {
                        bodyBucket = new RoadMeshBucketLikeOriginal(desc.Type, desc);
                        bodyBuckets.Add(desc.Type, bodyBucket);
                    }

                    int bodyBeforeV = bodyBucket.Vertices.Count;
                    int bodyBeforeI = bodyBucket.Triangles.Count;
                    bool useGroundAtlasBody = bodyResources != null && bodyResources.GroundAtlas != null && desc.MapTextureID >= 0;
                    if (AppendGeneratedRoadBodyUnderlayMeshV6LikeOriginal(map, kernel, bodyBucket, road, desc, useGroundAtlasBody))
                    {
                        emittedBodyRoads++;
                        emittedBodyVertices += bodyBucket.Vertices.Count - bodyBeforeV;
                        emittedBodyTriangles += (bodyBucket.Triangles.Count - bodyBeforeI) / 3;
                    }
                }
            }

            if (buckets.Count == 0 || emittedVertices <= 0)
            {
                return;
            }

            var roadsRoot = new GameObject("C2_RoadsNetTexture_V17_separate_file");
            roadsRoot.transform.SetParent(parent, false);

            int builtBodyBuckets = 0;
            foreach (RoadMeshBucketLikeOriginal bucket in bodyBuckets.Values)
            {
                if (bucket.Vertices.Count < 4 || bucket.Triangles.Count < 6)
                    continue;

                var mesh = new Mesh { name = $"C2_RoadBodyUnderlayMesh_V6_type_{bucket.Type:00}" };
                if (bucket.Vertices.Count > 65535)
                    mesh.indexFormat = IndexFormat.UInt32;

                mesh.SetVertices(bucket.Vertices);
                mesh.SetUVs(0, bucket.Uv0);
                mesh.SetColors(bucket.Colors);
                mesh.SetTriangles(bucket.Triangles, 0, true);
                mesh.RecalculateBounds();

                var go = new GameObject($"RoadBodyUnderlayV6_type_{bucket.Type:00}_{SanitizeRoadNameLikeAdapted(bucket.Desc.RoadName)}");
                go.transform.SetParent(roadsRoot.transform, false);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = CreateRoadBodyUnderlayMaterialV6LikeOriginal(bucket.Desc, bodyResources);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (bucket.HasBounds)
                    terrainBounds.Encapsulate(bucket.Bounds);

                builtBodyBuckets++;
            }

            int builtBuckets = 0;
            foreach (RoadMeshBucketLikeOriginal bucket in buckets.Values)
            {
                if (bucket.Vertices.Count < 4 || bucket.Triangles.Count < 6)
                    continue;

                var mesh = new Mesh { name = $"C2_RoadNetTextureMesh_V17_type_{bucket.Type:00}" };
                if (bucket.Vertices.Count > 65535)
                    mesh.indexFormat = IndexFormat.UInt32;

                mesh.SetVertices(bucket.Vertices);
                mesh.SetUVs(0, bucket.Uv0);
                mesh.SetColors(bucket.Colors);
                mesh.SetTriangles(bucket.Triangles, 0, true);
                mesh.RecalculateBounds();

                var go = new GameObject($"RoadNetTextureV17_type_{bucket.Type:00}_{SanitizeRoadNameLikeAdapted(bucket.Desc.RoadName)}");
                go.transform.SetParent(roadsRoot.transform, false);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = CreateRoadMaterialLikeOriginal(bucket.Desc);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                // V13: terrain/facture transparent chunks were drawn over the road mesh.
                // Draw roads late in transparent order; ZTest in shader still prevents true through-hill glow.
                mr.sortingOrder = 32000;

                if (bucket.HasBounds)
                    terrainBounds.Encapsulate(bucket.Bounds);

                builtBuckets++;
            }

        }

        private static void EnsureRoadSceneDepthTextureV15LikeOriginal()
        {
            // V15 uses camera depth in the road shader as a tolerant occlusion test:
            // draw road on the terrain surface even when Z-buffer equality jitters,
            // but still discard it when a real hill/mountain is in front of it.
            Camera main = Camera.main;
            if (main != null)
                main.depthTextureMode |= DepthTextureMode.Depth;

            Camera[] cams = Camera.allCameras;
            if (cams != null)
            {
                for (int i = 0; i < cams.Length; i++)
                {
                    if (cams[i] != null)
                        cams[i].depthTextureMode |= DepthTextureMode.Depth;
                }
            }
        }

        private List<GeneratedRoadLikeOriginal> GenerateRoadsFromNetLikeOriginal(
            ParsedMap map,
            List<RoadDescLikeOriginal> descs,
            out int straightRoads,
            out int junctionRoads,
            out int skippedLinks,
            out int skippedJunctions)
        {
            straightRoads = 0;
            junctionRoads = 0;
            skippedLinks = 0;
            skippedJunctions = 0;

            var result = new List<GeneratedRoadLikeOriginal>(Mathf.Min(4096, map.RoadKnots.Length * 2));
            ParsedRoadNetKnotLikeOriginal[] net = map.RoadKnots;

            for (int i = 0; i < net.Length; i++)
            {
                ParsedRoadNetKnotLikeOriginal nk = net[i];
                if (nk.Hidden != 0)
                    continue;

                int nl = Mathf.Clamp(nk.NLinks, 0, C2RoadMaxLinksLikeOriginal);
                var qs = new RoadAngleSortLikeOriginal[C2RoadMaxLinksLikeOriginal];

                int x0 = nk.X;
                int y0 = nk.Y;

                for (int j = 0; j < nl; j++)
                {
                    int f = nk.Links[j];
                    int type = nk.LinkType[j];

                    if (f < 0 || f >= net.Length)
                    {
                        skippedLinks++;
                        continue;
                    }

                    ParsedRoadNetKnotLikeOriginal fn = net[f];

                    if (fn.Hidden == 0 && f > i)
                    {
                        RoadDescLikeOriginal desc = GetRoadDescLikeOriginal(descs, type);
                        if (desc == null)
                        {
                            skippedLinks++;
                        }
                        else
                        {
                            float dx = nk.X - fn.X;
                            float dy = nk.Y - fn.Y;
                            float nn = RoadNormLikeOriginal(dx, dy);
                            if (nn < 1.0f)
                            {
                                skippedLinks++;
                            }
                            else
                            {
                                float w = Mathf.Max(1, desc.RWidth) * 0.5f;
                                dx = dx * w / nn;
                                dy = dy * w / nn;

                                var curve = new List<Vector2>(2)
                                {
                                    new Vector2(nk.X - dx, nk.Y - dy),
                                    new Vector2(fn.X + dx, fn.Y + dy)
                                };

                                GeneratedRoadLikeOriginal road = CreateGeneratedRoadFromCurveLikeOriginal(map, descs, type, curve, lrMode: 0);
                                if (road != null)
                                {
                                    result.Add(road);
                                    straightRoads++;
                                    if (result.Count >= C2RoadsNetTextureMaxGeneratedRoadsV3LikeOriginal)
                                        return result;
                                }
                            }
                        }
                    }

                    qs[j].Idx = f;
                    qs[j].Angle = GetRoadDir256LikeOriginal(net[f].X - x0, (net[f].Y - y0) / 2.0f);
                    qs[j].RType = type;
                }

                if (nl <= 1)
                    continue;

                for (int p = 0; p < nl; p++)
                {
                    int rMinDa = 1024;
                    int lMinDa = -1024;

                    for (int j = 0; j < nl; j++)
                    {
                        if (p == j)
                            continue;

                        int da = ToSignedByteIntLikeOriginal(qs[j].Angle - qs[p].Angle);
                        int dal = da < 0 ? da : -256 + da;
                        int dar = da > 0 ? da : 256 + da;

                        if (dar < rMinDa) rMinDa = dar;
                        if (dal > lMinDa) lMinDa = dal;
                    }

                    rMinDa = ToSignedByteIntLikeOriginal(rMinDa);
                    lMinDa = ToSignedByteIntLikeOriginal(lMinDa);

                    for (int j = 0; j < nl; j++)
                    {
                        if (p == j)
                            continue;

                        RoadDescLikeOriginal descJ = GetRoadDescLikeOriginal(descs, qs[j].RType);
                        RoadDescLikeOriginal descP = GetRoadDescLikeOriginal(descs, qs[p].RType);
                        if (descJ == null || descP == null)
                        {
                            skippedJunctions++;
                            continue;
                        }

                        int w1 = descJ.RWidth;
                        int w2 = descP.RWidth;
                        if (!((w1 == w2 && p > j) || w1 < w2))
                            continue;

                        int idxJ = qs[j].Idx;
                        int idxP = qs[p].Idx;
                        if (idxJ < 0 || idxJ >= net.Length || idxP < 0 || idxP >= net.Length || net[idxJ].Hidden != 0 || net[idxP].Hidden != 0)
                        {
                            skippedJunctions++;
                            continue;
                        }

                        int x = x0;
                        int y = y0;
                        float x1 = net[idxJ].X;
                        float y1 = net[idxJ].Y;
                        float x2 = net[idxP].X;
                        float y2 = net[idxP].Y;

                        int daPair = ToSignedByteIntLikeOriginal(qs[j].Angle - qs[p].Angle);
                        int w = descJ.RWidth * 2;
                        int dd = 20;
                        if (Mathf.Abs(daPair) < 64)
                            dd = 20 + 64 - Mathf.Abs(daPair);
                        if (dd > 40)
                            dd = 40;
                        if (Mathf.Abs(daPair) < 64)
                            w = (w * dd) / 20;

                        float n1 = RoadNormLikeOriginal(x1 - x, y1 - y);
                        float n2 = RoadNormLikeOriginal(x2 - x, y2 - y);
                        if (n1 < 1.0f) n1 = 1.0f;
                        if (n2 < 1.0f) n2 = 1.0f;

                        if (w > n1) w = Mathf.RoundToInt(n1);
                        if (w > n2) w = Mathf.RoundToInt(n2);

                        x1 = x + ((x1 - x) * w) / n1;
                        y1 = y + ((y1 - y) * w) / n1;
                        x2 = x + ((x2 - x) * w) / n2;
                        y2 = y + ((y2 - y) * w) / n2;

                        var curve = new List<Vector2>(5)
                        {
                            new Vector2(x1, y1),
                            new Vector2((x1 + x) * 0.5f, (y1 + y) * 0.5f),
                            new Vector2((x1 + x2 + x + x + x + x) / 6.0f, (y1 + y2 + y + y + y + y) / 6.0f),
                            new Vector2((x2 + x) * 0.5f, (y2 + y) * 0.5f),
                            new Vector2(x2, y2)
                        };

                        int nrd = 2;
                        for (int t = 0; t < nl; t++)
                        {
                            if (t == p || t == j)
                                continue;

                            RoadDescLikeOriginal descT = GetRoadDescLikeOriginal(descs, qs[t].RType);
                            if (descT == null)
                                continue;

                            int w3 = descT.RWidth;
                            if (Mathf.Abs(w1 - w3) < w1 / 3 || Mathf.Abs(w2 - w3) < w2 / 3)
                                nrd++;
                        }

                        int lrMode = 0;
                        if (nrd > 2)
                        {
                            if (Mathf.Abs(daPair) <= 32)
                                continue;

                            if (daPair == rMinDa)
                                lrMode = 1;
                            else if (daPair == lMinDa)
                                lrMode = 2;
                            else
                                lrMode = 3;
                        }

                        GeneratedRoadLikeOriginal road = CreateGeneratedRoadFromCurveLikeOriginal(map, descs, qs[j].RType, curve, lrMode);
                        if (road != null)
                        {
                            result.Add(road);
                            junctionRoads++;
                            if (result.Count >= C2RoadsNetTextureMaxGeneratedRoadsV3LikeOriginal)
                                return result;
                        }
                        else
                        {
                            skippedJunctions++;
                        }
                    }
                }
            }

            return result;
        }

        private GeneratedRoadLikeOriginal CreateGeneratedRoadFromCurveLikeOriginal(
            ParsedMap map,
            List<RoadDescLikeOriginal> descs,
            int type,
            List<Vector2> controlPoints,
            int lrMode)
        {
            RoadDescLikeOriginal desc = GetRoadDescLikeOriginal(descs, type);
            if (desc == null || controlPoints == null || controlPoints.Count < 2)
                return null;

            List<Vector2> points = InterpolateRoadCurveLikeOriginal(controlPoints);
            if (points == null || points.Count < C2RoadsNetTextureMinPointsV3LikeOriginal)
                return null;

            var road = new GeneratedRoadLikeOriginal
            {
                Type = Mathf.Clamp(type, 0, Mathf.Max(0, descs.Count - 1)),
                Width = Mathf.Max(1, desc.RWidth)
            };

            // Original CreateRoad() adds points with: for(i=0;i<NCurves-1;i++) AddPointToRoad(...)
            int countToAdd = Mathf.Max(0, points.Count - 1);
            for (int i = 0; i < countToAdd; i++)
            {
                Vector2 p = points[i];
                BuildRoadPointWeightsLikeOriginal(map, desc, i, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), out Color32 left, out Color32 right);
                ApplyRoadLRWeightsLikeOriginal(desc, i, lrMode, ref left, ref right);
                road.AddPoint(p, left, right);
            }

            return road.Points.Count >= C2RoadsNetTextureMinPointsV3LikeOriginal ? road : null;
        }

        private List<Vector2> InterpolateRoadCurveLikeOriginal(List<Vector2> input)
        {
            if (input == null || input.Count < 2)
                return null;

            var output = new List<Vector2>(Mathf.Min(C2RoadsNetTextureMaxCurvePointsV3LikeOriginal, input.Count * 32));
            int minDst = 80;

            for (int i = 1; i < input.Count; i++)
            {
                Vector2 p0 = input[i - 1];
                Vector2 p3 = input[i];

                int nr = Mathf.RoundToInt(2.0f * RoadNormLikeOriginal(p3.x - p0.x, p3.y - p0.y) / 5.0f);

                Vector2 d1 = GetBezierVectorLikeOriginal(input, i - 1);
                d1 = NormalizeRoadVectorLikeOriginal(d1, nr);
                Vector2 p1 = p0 + d1;

                Vector2 d2 = GetBezierVectorLikeOriginal(input, i);
                d2 = NormalizeRoadVectorLikeOriginal(d2, nr);
                Vector2 p2 = p3 - d2;

                int np = Mathf.FloorToInt(RoadNormLikeOriginal(p1.x - p0.x, p1.y - p0.y) / 5.0f);
                if (np > 1)
                {
                    np += 2;
                    for (int j = 0; j < np; j++)
                    {
                        float u = j / Mathf.Max(1.0f, (float)np);
                        float iu = 1.0f - u;
                        float b0 = iu * iu * iu;
                        float b1 = 3.0f * u * iu * iu;
                        float b2 = 3.0f * u * u * iu;
                        float b3 = u * u * u;
                        int x = Mathf.FloorToInt(b0 * p0.x + b1 * p1.x + b2 * p2.x + b3 * p3.x);
                        int y = Mathf.FloorToInt(b0 * p0.y + b1 * p1.y + b2 * p2.y + b3 * p3.y);

                        minDst = 8;
                        AddInterpolatedRoadPointLikeOriginal(output, new Vector2(x, y), minDst);
                        minDst = 80;

                        if (output.Count >= C2RoadsNetTextureMaxCurvePointsV3LikeOriginal)
                            return output;
                    }
                }
                else
                {
                    AddInterpolatedRoadPointLikeOriginal(output, p0, minDst);
                }

                if (i == input.Count - 1)
                    AddInterpolatedRoadPointLikeOriginal(output, p3, minDst);
            }

            return output;
        }

        private static Vector2 GetBezierVectorLikeOriginal(List<Vector2> points, int i)
        {
            int np = points.Count;
            if (i <= 0)
                return 2.0f * (points[1] - points[0]);
            if (i >= np - 1)
                return 2.0f * (points[np - 1] - points[np - 2]);
            return points[i + 1] - points[i - 1];
        }

        private static Vector2 NormalizeRoadVectorLikeOriginal(Vector2 v, float n)
        {
            float len = RoadNormLikeOriginal(v.x, v.y);
            if (len > 0.001f)
                return v * (n / len);
            return Vector2.zero;
        }

        private static void AddInterpolatedRoadPointLikeOriginal(List<Vector2> points, Vector2 p, int minDst)
        {
            if (points == null)
                return;

            if (points.Count > 0)
            {
                Vector2 last = points[points.Count - 1];
                if (RoadNormLikeOriginal(p.x - last.x, p.y - last.y) < minDst)
                    return;
            }

            if (points.Count < C2RoadsNetTextureMaxCurvePointsV3LikeOriginal)
                points.Add(p);
        }

        private void BuildRoadPointWeightsLikeOriginal(ParsedMap map, RoadDescLikeOriginal desc, int pointIndex, int x, int y, out Color32 left, out Color32 right)
        {
            int w = GetRoadRandomInterpolatedLikeOriginal(pointIndex * Mathf.Max(1, desc.AlphaFrequency));
            int a = desc.AFactorMin + (((desc.AFactor - desc.AFactorMin) * w) >> 15);

            int lt = GetRoadLightingInPointLikeOriginal(map, x, y);
            int r = RoadLim255LikeOriginal((desc.RFactor * lt) / 128);
            int g = RoadLim255LikeOriginal((desc.GFactor * lt) / 128);
            int b = RoadLim255LikeOriginal((desc.BFactor * lt) / 128);

            left = BuildRoadDiffuseColorLikeOriginal(a, r, g, b);

            w = 32767 - w;
            a = desc.AFactorMin + (((desc.AFactor - desc.AFactorMin) * w) >> 15);
            right = BuildRoadDiffuseColorLikeOriginal(a, r, g, b);
        }

        private static void ApplyRoadLRWeightsLikeOriginal(RoadDescLikeOriginal desc, int pointIndex, int lrMode, ref Color32 left, ref Color32 right)
        {
            if (lrMode <= 0)
                return;

            int rm = (desc.AFactorMin + desc.AFactor) / 2;
            int rv = GetRoadRandomInterpolatedStaticLikeOriginal((pointIndex + 1103) * Mathf.Max(1, desc.AlphaFrequency));
            int a = desc.AFactor + (((rm - desc.AFactor) * rv) >> 15);

            Color32 c = BuildRoadDiffuseColorLikeOriginal(
                a,
                desc.RFactor,
                desc.GFactor,
                desc.BFactor);

            Color32 c1 = BuildRoadDiffuseColorLikeOriginal(
                a >> 2,
                desc.RFactor,
                desc.GFactor,
                desc.BFactor);

            if (lrMode == 1)
            {
                left = new Color32(0, 0, 0, 0);
                right = c;
            }
            else if (lrMode == 2)
            {
                right = new Color32(0, 0, 0, 0);
                left = c;
            }
            else if (lrMode == 3)
            {
                left = c1;
                right = c1;
            }
        }

        private static bool ShouldEmitRoadBodyUnderlayV6LikeOriginal(RoadDescLikeOriginal desc)
        {
            if (desc == null)
                return false;

            // Original OneRoad::SurroundWithTexture() writes the broad road texture into TexMap/FactureMap.
            // The visible road overlay mesh alone is only the narrow detail line. Keep trails untouched.
            bool hasSurroundDesc = desc.MapTextureID >= 0 || desc.TexRMin > 0 || desc.TexRMax > 0 || desc.FactureID >= 0;
            bool looksLikeWideRoad = desc.RWidth >= 80 || desc.Type == 16 || desc.Type == 14 || desc.Type == 0;
            bool looksLikeTrail = desc.Type == 11 || (desc.RWidth > 0 && desc.RWidth < 72 && (desc.RoadName ?? string.Empty).IndexOf("ROAD1", StringComparison.OrdinalIgnoreCase) >= 0);
            return looksLikeWideRoad && !looksLikeTrail && (hasSurroundDesc || desc.RWidth >= 96);
        }

        private bool AppendGeneratedRoadBodyUnderlayMeshV6LikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            RoadMeshBucketLikeOriginal bucket,
            GeneratedRoadLikeOriginal road,
            RoadDescLikeOriginal desc,
            bool useGroundAtlasBody)
        {
            int n = road != null ? road.Points.Count : 0;
            if (n < C2RoadsNetTextureMinPointsV3LikeOriginal || bucket == null || desc == null)
                return false;

            int emitPairs = Mathf.Max(0, n - 1);
            if (emitPairs < 2)
                return false;

            int firstVertex = bucket.Vertices.Count;
            const int crossCount = 5;
            float uPos = 0.0f;
            float bodyRadius = GetRoadBodyRadiusV6LikeOriginal(desc, road.Width);
            float hardRadius = Mathf.Max(1.0f, GetRoadBodyHardRadiusV6LikeOriginal(desc, road.Width, bodyRadius));

            for (int i = 0; i < emitPairs; i++)
            {
                Vector2 cur = road.Points[i];
                Vector2 prev = i > 0 ? road.Points[i - 1] : road.Points[i];
                Vector2 next = i < n - 1 ? road.Points[i + 1] : road.Points[i];

                float dx = next.x - prev.x;
                float dy = next.y - prev.y;
                float np = RoadNormLikeOriginal(dy, -dx);
                if (np < 0.001f)
                    np = 1.0f;

                if (i < n - 1)
                    uPos += RoadNormLikeOriginal(next.x - cur.x, next.y - cur.y) * Mathf.Max(1, desc.RScaleX) / 100.0f;

                for (int c = 0; c < crossCount; c++)
                {
                    float t = c / (float)(crossCount - 1);
                    float offset = Mathf.Lerp(-bodyRadius, bodyRadius, t);
                    float px = dy * offset / np;
                    float py = -dx * offset / np * 1.2f;
                    Vector2 p = new Vector2(cur.x + px, cur.y + py);

                    int alpha = GetRoadBodyAlphaV6LikeOriginal(desc, Mathf.Abs(offset), hardRadius, bodyRadius);
                    if (i < 8)
                        alpha = (alpha * Mathf.Clamp(i * 32, 0, 255)) >> 8;
                    if (i >= n - 8)
                        alpha = (alpha * Mathf.Clamp((n - i - 1) * 32, 0, 255)) >> 8;

                    Color32 color = new Color32(255, 255, 255, (byte)Mathf.Clamp(alpha, 0, 255));
                    Vector2 uv = useGroundAtlasBody
                        ? BuildRoadGroundAtlasUvV6LikeOriginal(desc.MapTextureID, uPos, t)
                        : BuildRoadTextureBodyUvV6LikeOriginal(desc, uPos, t);

                    bucket.AddVertex(RoadOriginalPointToWorldV3LikeOriginal(map, kernel, p.x, p.y), uv, color);
                }
            }

            for (int i = 0; i < emitPairs - 1; i++)
            {
                int row0 = firstVertex + i * crossCount;
                int row1 = row0 + crossCount;
                for (int c = 0; c < crossCount - 1; c++)
                {
                    int a = row0 + c;
                    int b = row0 + c + 1;
                    int c0 = row1 + c;
                    int d = row1 + c + 1;
                    bucket.Triangles.Add(a);
                    bucket.Triangles.Add(d);
                    bucket.Triangles.Add(b);
                    bucket.Triangles.Add(a);
                    bucket.Triangles.Add(c0);
                    bucket.Triangles.Add(d);
                }
            }

            return true;
        }

        private static float GetRoadBodyRadiusV6LikeOriginal(RoadDescLikeOriginal desc, int roadWidth)
        {
            float visualHalfWidth = Mathf.Max(1, roadWidth) * 0.5f;
            float r0 = desc != null && desc.TexRMin > 0 ? desc.TexRMin : visualHalfWidth;
            float drMax = Mathf.Max(8.0f, (r0 * Mathf.Max(1, roadWidth)) / 200.0f);
            float surroundRadius = r0 + drMax;
            return Mathf.Clamp(Mathf.Max(visualHalfWidth, surroundRadius), visualHalfWidth, Mathf.Max(visualHalfWidth, roadWidth * 1.15f));
        }

        private static float GetRoadBodyHardRadiusV6LikeOriginal(RoadDescLikeOriginal desc, int roadWidth, float bodyRadius)
        {
            if (desc != null && desc.TexRMin > 0)
                return Mathf.Clamp(desc.TexRMin, 1.0f, bodyRadius);
            return Mathf.Clamp(Mathf.Max(1, roadWidth) * 0.43f, 1.0f, bodyRadius);
        }

        private static int GetRoadBodyAlphaV6LikeOriginal(RoadDescLikeOriginal desc, float dist, float hardRadius, float bodyRadius)
        {
            float weight;
            if (dist <= hardRadius)
                weight = 1.0f;
            else
                weight = 1.0f - ((dist - hardRadius) / Mathf.Max(0.001f, bodyRadius - hardRadius));

            weight = Mathf.Clamp01(weight);
            int maxAlpha = desc != null && desc.Type == 16 ? 168 : 132;
            return Mathf.RoundToInt(maxAlpha * weight * weight);
        }

        private static Vector2 BuildRoadGroundAtlasUvV6LikeOriginal(int mapTextureId, float uPos, float crossT)
        {
            int tex = mapTextureId & 63;
            float tileSizeU = 1.0f / GroundAtlasTileCountXLikeOriginal;
            float tileSizeV = 1.0f / GroundAtlasTileCountYLikeOriginal;
            float tileU = (tex & (GroundAtlasTileCountXLikeOriginal - 1)) * tileSizeU;
            float tileV = (tex / GroundAtlasTileCountXLikeOriginal) * tileSizeV;
            float insetU = tileSizeU / 64.0f;
            float insetV = tileSizeV / 64.0f;
            float localU = Repeat01V6LikeOriginal(uPos / 256.0f);
            float localV = Mathf.Clamp01(crossT);
            return new Vector2(
                tileU + insetU + localU * (tileSizeU - insetU * 2.0f),
                tileV + insetV + localV * (tileSizeV - insetV * 2.0f));
        }

        private static Vector2 BuildRoadTextureBodyUvV6LikeOriginal(RoadDescLikeOriginal desc, float uPos, float crossT)
        {
            float u = uPos / Mathf.Max(1.0f, desc != null ? desc.TexSizeX : 256.0f);
            float v0 = desc != null ? desc.ReliefY0 : 0.0f;
            float v1 = desc != null ? desc.ReliefY1 : 256.0f;
            float v = Mathf.Lerp(v0, v1, Mathf.Clamp01(crossT)) / Mathf.Max(1.0f, desc != null ? desc.TexSizeY : 256.0f);
            return new Vector2(u, v);
        }

        private static float Repeat01V6LikeOriginal(float v)
        {
            return v - Mathf.Floor(v);
        }

        private struct RoadMeshStationV14LikeOriginal
        {
            public Vector2 Center;
            public Vector2 Perp;
            public float U;
            public Color32 Left;
            public Color32 Right;
        }

        private bool AppendGeneratedRoadMeshLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            RoadMeshBucketLikeOriginal bucket,
            GeneratedRoadLikeOriginal road,
            RoadDescLikeOriginal desc)
        {
            int n = road.Points.Count;
            if (n < C2RoadsNetTextureMinPointsV3LikeOriginal || road.Weights.Count < n * 2)
                return false;

            // Original RoadMesh was a 2-vertex strip in the old screen-space pipeline.
            // Unity terrain writes real depth. V16 keeps original road path/UV/alpha, resamples
            // the ribbon densely and samples the exact same terrain triangle plane for every
            // vertex. No camera-depth hack, no dependency on camera distance.
            int emitPairs = Mathf.Max(0, n - 1);
            if (emitPairs < 2)
                return false;

            float v0 = desc.ReliefY0 / Mathf.Max(1.0f, desc.TexSizeY);
            float v1 = desc.ReliefY1 / Mathf.Max(1.0f, desc.TexSizeY);

            var baseStations = new List<RoadMeshStationV14LikeOriginal>(emitPairs);
            float uPos = 0.0f;

            for (int i = 0; i < emitPairs; i++)
            {
                Vector2 cur = road.Points[i];
                Vector2 prev = i > 0 ? road.Points[i - 1] : road.Points[i];
                Vector2 next = i < n - 1 ? road.Points[i + 1] : road.Points[i];

                float dx = next.x - prev.x;
                float dy = next.y - prev.y;
                float px = dy;
                float py = -dx;
                float np = RoadNormLikeOriginal(px, py);
                if (np < 0.001f)
                    np = 1.0f;

                float w = Mathf.Max(1, road.Width) * 0.5f;
                px = px * w / np;
                py = py * w / np * 1.2f;

                if (i < n - 1)
                    uPos += RoadNormLikeOriginal(next.x - cur.x, next.y - cur.y) * Mathf.Max(1, desc.RScaleX) / 100.0f;

                Color32 left = road.Weights[i * 2];
                Color32 right = road.Weights[i * 2 + 1];

                if (i < 8)
                {
                    TransRoadDiffuseLikeOriginal(ref left, i * 32);
                    TransRoadDiffuseLikeOriginal(ref right, i * 32);
                }

                if (i >= n - 8)
                {
                    int fade = (n - i - 1) * 32;
                    TransRoadDiffuseLikeOriginal(ref left, fade);
                    TransRoadDiffuseLikeOriginal(ref right, fade);
                }

                baseStations.Add(new RoadMeshStationV14LikeOriginal
                {
                    Center = cur,
                    Perp = new Vector2(px, py),
                    U = uPos / Mathf.Max(1.0f, desc.TexSizeX),
                    Left = left,
                    Right = right
                });
            }

            if (baseStations.Count < 2)
                return false;

            int firstVertex = bucket.Vertices.Count;
            int cols = C2RoadsNetTextureCrossSlicesV14LikeOriginal + 1;
            int stationCount = 0;

            void EmitStation(RoadMeshStationV14LikeOriginal st)
            {
                for (int s = 0; s < cols; s++)
                {
                    float t = s / (float)C2RoadsNetTextureCrossSlicesV14LikeOriginal;
                    Vector2 p = st.Center + Vector2.Lerp(st.Perp, -st.Perp, t);
                    Vector2 uv = new Vector2(st.U, Mathf.Lerp(v0, v1, t));
                    Color32 c = RoadColorLerpV14LikeOriginal(st.Left, st.Right, t);
                    bucket.AddVertex(RoadOriginalPointToWorldV3LikeOriginal(map, kernel, p.x, p.y), uv, c);
                }
                stationCount++;
            }

            EmitStation(baseStations[0]);

            for (int i = 0; i < baseStations.Count - 1; i++)
            {
                RoadMeshStationV14LikeOriginal a = baseStations[i];
                RoadMeshStationV14LikeOriginal b = baseStations[i + 1];
                float dist = RoadNormLikeOriginal(b.Center.x - a.Center.x, b.Center.y - a.Center.y);
                int steps = Mathf.Clamp(Mathf.CeilToInt(dist / Mathf.Max(1.0f, C2RoadsNetTextureLongStepV14LikeOriginal)), 1, 64);

                for (int k = 1; k <= steps; k++)
                {
                    if (stationCount >= C2RoadsNetTextureMaxStationsPerRoadV14LikeOriginal)
                        break;

                    float t = k / (float)steps;
                    RoadMeshStationV14LikeOriginal st = new RoadMeshStationV14LikeOriginal
                    {
                        Center = Vector2.Lerp(a.Center, b.Center, t),
                        Perp = Vector2.Lerp(a.Perp, b.Perp, t),
                        U = Mathf.Lerp(a.U, b.U, t),
                        Left = RoadColorLerpV14LikeOriginal(a.Left, b.Left, t),
                        Right = RoadColorLerpV14LikeOriginal(a.Right, b.Right, t)
                    };
                    EmitStation(st);
                }
            }

            if (stationCount < 2)
                return false;

            for (int i = 0; i < stationCount - 1; i++)
            {
                int row0 = firstVertex + i * cols;
                int row1 = row0 + cols;

                for (int s = 0; s < cols - 1; s++)
                {
                    int a = row0 + s;
                    int c = row0 + s + 1;
                    int b = row1 + s;
                    int d = row1 + s + 1;

                    bucket.Triangles.Add(a);
                    bucket.Triangles.Add(d);
                    bucket.Triangles.Add(c);
                    bucket.Triangles.Add(a);
                    bucket.Triangles.Add(b);
                    bucket.Triangles.Add(d);
                }
            }

            return true;
        }

        private static Color32 RoadColorLerpV14LikeOriginal(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)), 0, 255));
        }

        private Vector3 RoadOriginalPointToWorldV3LikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, float x, float y)
        {
            float gridX = x / 32.0f;
            int ix = Mathf.Clamp(Mathf.FloorToInt(gridX), 0, Mathf.Max(0, map.VertInLine - 1));

            // Original terrain point -> road XY relation:
            // y = row*32 for even columns, y = row*32-16 for odd columns.
            float gridY = ((ix & 1) != 0) ? ((y + 16.0f) / 32.0f) : (y / 32.0f);

            float rawX = gridX * kernel.BackingStepXWorld;
            float rawZ = gridY * kernel.BackingStepZWorld + (((ix & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f);

            float worldX = rawX - kernel.CenterX;
            float worldZ = (rawZ - kernel.CenterZ) * WorldZSign;
            float worldY = SampleRoadHeightByOriginalXYV3LikeOriginal(map, x, y) * kernel.HeightScale + C2RoadsNetTextureYOffsetV3LikeOriginal;

            return new Vector3(worldX, worldY, worldZ);
        }
        private static float SampleRoadHeightByOriginalXYV3LikeOriginal(ParsedMap map, float x, float y)
        {
            if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 1 || map.MaxTH <= 1)
                return 0.0f;

            // V16 root fix:
            // Previous road height sampling used bilinear interpolation over the height grid.
            // The terrain mesh itself is NOT bilinear: every cell is split by the original odd/even
            // triangle diagonal in BuildCellTriangulationLikeOriginal().
            // On steep hills the bilinear road ribbon can fall a little behind the real triangle
            // surface; then hardware ZTest cuts it when the camera comes close. That is why yOffset,
            // renderQueue and screen-depth hacks did not really solve the problem.
            //
            // Sample the exact same triangle plane as the terrain mesh. This keeps roads locked to
            // the surface for every camera position while preserving ZTest LEqual so real hills can
            // still occlude roads.
            float rawX = x;
            float rawZ = y + 16.0f;

            float cellXf = Mathf.Clamp(rawX / 32.0f, 0.0f, map.VertInLine - 1.001f);
            int cellX = Mathf.Clamp(Mathf.FloorToInt(cellXf), 0, map.VertInLine - 2);
            float tx = Mathf.Clamp01((rawX - cellX * 32.0f) / 32.0f);

            float offLeft = ((cellX & 1) == 0) ? 16.0f : 0.0f;
            float offRight = (((cellX + 1) & 1) == 0) ? 16.0f : 0.0f;
            float zBaseAtTx = Mathf.Lerp(offLeft, offRight, tx);

            float cellYf = (rawZ - zBaseAtTx) / 32.0f;
            int cellY = Mathf.Clamp(Mathf.FloorToInt(cellYf), 0, map.MaxTH - 2);

            float zTopAtTx = cellY * 32.0f + zBaseAtTx;
            float ty = Mathf.Clamp01((rawZ - zTopAtTx) / 32.0f);

            int i00 = cellY * map.VertInLine + cellX;
            int i10 = i00 + 1;
            int i01 = i00 + map.VertInLine;
            int i11 = i01 + 1;

            float h00 = (i00 >= 0 && i00 < map.Heights.Length) ? map.Heights[i00] : 0.0f;
            float h10 = (i10 >= 0 && i10 < map.Heights.Length) ? map.Heights[i10] : h00;
            float h01 = (i01 >= 0 && i01 < map.Heights.Length) ? map.Heights[i01] : h00;
            float h11 = (i11 >= 0 && i11 < map.Heights.Length) ? map.Heights[i11] : h01;

            if ((cellX & 1) != 0)
            {
                // Odd cell: first triangle V0,V1,V2; second triangle V2,V1,V3.
                if (tx + ty <= 1.0f)
                    return h00 + tx * (h10 - h00) + ty * (h01 - h00);

                float rx = 1.0f - tx;
                float ry = 1.0f - ty;
                return h11 + rx * (h01 - h11) + ry * (h10 - h11);
            }

            // Even cell: first triangle V0,V1,V3; second triangle V0,V3,V2.
            if (ty <= tx)
                return h00 + tx * (h10 - h00) + ty * (h11 - h10);

            return h00 + tx * (h11 - h01) + ty * (h01 - h00);
        }

        private int GetRoadLightingInPointLikeOriginal(ParsedMap map, int x, int y)
        {
            if (map == null || map.VertInLine <= 0 || map.MaxTH <= 0)
                return 128;

            int vx = Mathf.Clamp(Mathf.RoundToInt(x / 32.0f), 0, map.VertInLine - 1);
            int vy = ((vx & 1) != 0) ? Mathf.RoundToInt((y + 16.0f) / 32.0f) : Mathf.RoundToInt(y / 32.0f);
            vy = Mathf.Clamp(vy, 0, map.MaxTH - 1);

            int idx = vy * map.VertInLine + vx;
            if (idx < 0 || map.Heights == null || idx >= map.Heights.Length)
                return 128;

            return GetLighting3DLikeOriginal(idx);
        }

        private int GetRoadRandomInterpolatedLikeOriginal(int x)
        {
            return GetRoadRandomInterpolatedStaticLikeOriginal(x);
        }

        private static int GetRoadRandomInterpolatedStaticLikeOriginal(int x)
        {
            short[] randoma = GetRandomTableLikeOriginal();
            if (randoma == null || randoma.Length != 8192)
                return 16384;

            int d = x >> 6;
            int r = x & 63;
            int v1 = randoma[d & 8191] & 0x7FFF;
            int v2 = randoma[(d + 1) & 8191] & 0x7FFF;
            return v1 + (((v2 - v1) * r) >> 6);
        }

        private static Color32 BuildRoadDiffuseColorLikeOriginal(int a, int r, int g, int b)
        {
            a = RoadLim255LikeOriginal(a);
            r = RoadLim255LikeOriginal((r * a) >> 7);
            g = RoadLim255LikeOriginal((g * a) >> 7);
            b = RoadLim255LikeOriginal((b * a) >> 7);
            return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        }

        private static void TransRoadDiffuseLikeOriginal(ref Color32 c, int v)
        {
            v = Mathf.Clamp(v, 0, 255);
            c.a = (byte)((c.a * v) >> 8);
            c.r = (byte)((c.r * v) >> 8);
            c.g = (byte)((c.g * v) >> 8);
            c.b = (byte)((c.b * v) >> 8);
        }

        private static int RoadLim255LikeOriginal(int x)
        {
            return x < 0 ? 0 : (x > 255 ? 255 : x);
        }

        private static float RoadNormLikeOriginal(float dx, float dy)
        {
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static int GetRoadDir256LikeOriginal(float dx, float dy)
        {
            if (Mathf.Abs(dx) < 0.001f && Mathf.Abs(dy) < 0.001f)
                return 0;

            double a = Math.Atan2(dy, dx);
            int v = (int)Math.Round(a * 128.0 / Math.PI);
            return v & 255;
        }

        private static int ToSignedByteIntLikeOriginal(int v)
        {
            unchecked
            {
                return (sbyte)(byte)v;
            }
        }
    }
}
