using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2RoadsLayerV1LikeOriginal = true;
        private const float C2RoadsLayerYOffsetV1LikeOriginal = 0.32f;
        private const int C2RoadMaxLinksLikeOriginal = 8;
        private const int C2RoadNetKnotSizeNew2EnrLikeOriginal = 77;
        private const int C2RoadNetKnotSizeOldTenrLikeOriginal = 56;
        private const float C2RoadInterpolationStepLikeOriginal = 26.0f;
        private const int C2RoadRenderQueueOffsetLikeOriginal = 15;
        private const int C2RoadLateRenderQueueV13LikeOriginal = 3600; // V17: keep late queue, but remove aggressive depth forcing.
        private const float C2RoadScreenDepthPullV17LikeOriginal = 0.0f; // V17: disable clip-space depth pull so roads stop punching through mountains/hills.
        private const float C2RoadSceneDepthToleranceV15LikeOriginal = 6.0f; // Unused in V17; kept only for compatibility with older V15 shader properties.

        private sealed partial class ParsedMap
        {
            public bool HasRoadNet;
            public string RoadNetSectionTag = string.Empty;
            public ParsedRoadNetKnotLikeOriginal[] RoadKnots = Array.Empty<ParsedRoadNetKnotLikeOriginal>();
        }

        private struct ParsedRoadNetKnotLikeOriginal
        {
            public int X;
            public int Y;
            public int NLinks;
            public byte Hidden;
            public ushort[] Links;
            public ushort[] RoadID;
            public byte[] LinkType;
            public ushort[] WayPointToPointIndex;
            public int XOnRoad;
            public int YOnRoad;
        }

        private sealed class RoadDescLikeOriginal
        {
            public int Type;
            public string RoadName = string.Empty;
            public string TexturePath = string.Empty;
            public int TexSizeX = 256;
            public int TexSizeY = 256;
            public int ColorY0;
            public int ColorY1;
            public int ReliefY0;
            public int ReliefY1 = 256;
            public int RWidth = 96;
            public int RScaleX = 100;
            public int AlphaFrequency = 1;
            public int AFactor = 160;
            public int AFactorMin = 96;
            public int RFactor = 128;
            public int GFactor = 128;
            public int BFactor = 128;
            public int MapTextureID = -1;
            public int TexRMin;
            public int TexRMax;
            public int TexSmoothness;
            public int FactureID = -1;
            public int BumpTextureID = -1;

            public Color32 FallbackColor
            {
                get
                {
                    byte a = ToByteRoundClampLikeOriginal(Mathf.Clamp(AFactor, 0, 255));
                    byte r = ToByteRoundClampLikeOriginal(Mathf.Clamp(RFactor, 0, 255));
                    byte g = ToByteRoundClampLikeOriginal(Mathf.Clamp(GFactor, 0, 255));
                    byte b = ToByteRoundClampLikeOriginal(Mathf.Clamp(BFactor, 0, 255));
                    return new Color32(r, g, b, a);
                }
            }

            public RoadDescLikeOriginal Clone(int type)
            {
                return new RoadDescLikeOriginal
                {
                    Type = type,
                    RoadName = RoadName,
                    TexturePath = TexturePath,
                    TexSizeX = TexSizeX,
                    TexSizeY = TexSizeY,
                    ColorY0 = ColorY0,
                    ColorY1 = ColorY1,
                    ReliefY0 = ReliefY0,
                    ReliefY1 = ReliefY1,
                    RWidth = RWidth,
                    RScaleX = RScaleX,
                    AlphaFrequency = AlphaFrequency,
                    AFactor = AFactor,
                    AFactorMin = AFactorMin,
                    RFactor = RFactor,
                    GFactor = GFactor,
                    BFactor = BFactor,
                    MapTextureID = MapTextureID,
                    TexRMin = TexRMin,
                    TexRMax = TexRMax,
                    TexSmoothness = TexSmoothness,
                    FactureID = FactureID,
                    BumpTextureID = BumpTextureID
                };
            }
        }

        private sealed class RoadMeshBucketLikeOriginal
        {
            public readonly int Type;
            public readonly RoadDescLikeOriginal Desc;
            public readonly List<Vector3> Vertices = new List<Vector3>(1024);
            public readonly List<Vector2> Uv0 = new List<Vector2>(1024);
            public readonly List<Color32> Colors = new List<Color32>(1024);
            public readonly List<int> Triangles = new List<int>(2048);
            public bool HasBounds;
            public Bounds Bounds;

            public RoadMeshBucketLikeOriginal(int type, RoadDescLikeOriginal desc)
            {
                Type = type;
                Desc = desc;
            }

            public void AddVertex(Vector3 v, Vector2 uv, Color32 color)
            {
                Vertices.Add(v);
                Uv0.Add(uv);
                Colors.Add(color);
                if (!HasBounds)
                {
                    Bounds = new Bounds(v, Vector3.zero);
                    HasBounds = true;
                }
                else
                {
                    Bounds.Encapsulate(v);
                }
            }
        }

        private static bool TryParseRoadsChunkLikeOriginal(string tag, BinaryReader br, ParsedMap map, int payloadLen)
        {
            if (map == null || br == null || payloadLen < 4)
                return false;

            bool isNew = TagEqualsLikeOriginal(tag, "2ENR", "RNE2");
            bool isOld = TagEqualsLikeOriginal(tag, "TENR", "RNET");
            if (!isNew && !isOld)
                return false;

            long start = br.BaseStream.Position;
            int n = Mathf.Max(0, br.ReadInt32());
            int remaining = Mathf.Max(0, payloadLen - 4);

            int rawKnotSize = n > 0 ? remaining / Mathf.Max(1, n) : 0;
            bool exact = n > 0 && remaining == rawKnotSize * n;

            int knotSize;
            if (exact && rawKnotSize == 77)
                knotSize = 77;      // real C2 2ENR packed layout: 12+1+16+16+8+16+8
            else if (exact && rawKnotSize == 80)
                knotSize = 80;      // padded/native variant
            else if (exact && rawKnotSize == 56)
                knotSize = 56;      // old TENR/native old layout
            else if (exact && rawKnotSize == 53)
                knotSize = 53;      // old packed layout
            else if (isNew && remaining >= n * 77)
                knotSize = 77;
            else if (remaining >= n * 56)
                knotSize = 56;
            else if (remaining >= n * 53)
                knotSize = 53;
            else
                knotSize = Mathf.Max(1, rawKnotSize);

            if (n > 0 && remaining < n * knotSize)
                n = Mathf.Max(0, remaining / Mathf.Max(1, knotSize));

            var knots = new ParsedRoadNetKnotLikeOriginal[n];

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            int totalLinksRaw = 0;
            int visibleKnots = 0;
            int linkIndexValid = 0;
            int linkIndexInvalid = 0;
            int[] typeCounts = new int[64];

            for (int i = 0; i < n; i++)
            {
                long knotStart = br.BaseStream.Position;
                var k = new ParsedRoadNetKnotLikeOriginal
                {
                    Links = new ushort[C2RoadMaxLinksLikeOriginal],
                    RoadID = new ushort[C2RoadMaxLinksLikeOriginal],
                    LinkType = new byte[C2RoadMaxLinksLikeOriginal],
                    WayPointToPointIndex = new ushort[C2RoadMaxLinksLikeOriginal]
                };

                k.X = br.ReadInt32();
                k.Y = br.ReadInt32();
                k.NLinks = Mathf.Clamp(br.ReadInt32(), 0, C2RoadMaxLinksLikeOriginal);
                k.Hidden = br.ReadByte();

                if (knotSize == 80)
                {
                    // MSVC/native padded variant: 1 byte Hidden + 1 byte padding before WORD arrays.
                    if (br.BaseStream.Position < knotStart + 14)
                        br.BaseStream.Position = knotStart + 14;
                }
                // knotSize 77 is packed: no padding after Hidden.

                for (int j = 0; j < C2RoadMaxLinksLikeOriginal; j++)
                    k.Links[j] = br.ReadUInt16();

                for (int j = 0; j < C2RoadMaxLinksLikeOriginal; j++)
                    k.RoadID[j] = br.ReadUInt16();

                for (int j = 0; j < C2RoadMaxLinksLikeOriginal; j++)
                    k.LinkType[j] = br.ReadByte();

                if (knotSize == 77 || knotSize == 80)
                {
                    for (int j = 0; j < C2RoadMaxLinksLikeOriginal; j++)
                        k.WayPointToPointIndex[j] = br.ReadUInt16();

                    if (knotSize == 80 && br.BaseStream.Position < knotStart + 72)
                        br.BaseStream.Position = knotStart + 72;

                    k.XOnRoad = br.ReadInt32();
                    k.YOnRoad = br.ReadInt32();
                }
                else
                {
                    for (int j = 0; j < C2RoadMaxLinksLikeOriginal; j++)
                        k.WayPointToPointIndex[j] = 0xFFFF;

                    k.XOnRoad = k.X;
                    k.YOnRoad = k.Y;
                }

                br.BaseStream.Position = knotStart + knotSize;

                if (k.Hidden == 0)
                    visibleKnots++;

                if (k.X < minX) minX = k.X;
                if (k.X > maxX) maxX = k.X;
                if (k.Y < minY) minY = k.Y;
                if (k.Y > maxY) maxY = k.Y;

                totalLinksRaw += k.NLinks;
                for (int j = 0; j < k.NLinks; j++)
                {
                    int link = k.Links[j];
                    if (link >= 0 && link < n)
                        linkIndexValid++;
                    else
                        linkIndexInvalid++;

                    int type = k.LinkType[j];
                    if (type >= 0 && type < typeCounts.Length)
                        typeCounts[type]++;
                }

                knots[i] = k;
            }

            map.HasRoadNet = knots.Length > 0;
            map.RoadNetSectionTag = tag;
            map.RoadKnots = knots;

            br.BaseStream.Position = start + payloadLen;

            string typeReport = BuildRoadTypeReportV2LikeAdapted(typeCounts);
            UnityEngine.Debug.Log(
                $"[C2:ROADS PARSE V2] section={tag} knots={knots.Length} visibleKnots={visibleKnots} knotSize={knotSize} rawKnotSize={rawKnotSize} exact={exact} payload={payloadLen} " +
                $"xy=({minX},{minY})->({maxX},{maxY}) rawLinks={totalLinksRaw} validLinks={linkIndexValid} invalidLinks={linkIndexInvalid} types={typeReport}");

            return true;
        }

        private static string BuildRoadTypeReportV2LikeAdapted(int[] typeCounts)
        {
            if (typeCounts == null || typeCounts.Length == 0)
                return "none";

            var sb = new System.Text.StringBuilder(128);
            int written = 0;
            for (int i = 0; i < typeCounts.Length; i++)
            {
                int c = typeCounts[i];
                if (c <= 0)
                    continue;

                if (written > 0)
                    sb.Append(' ');
                sb.Append(i).Append('=').Append(c);
                written++;
                if (written >= 12)
                    break;
            }

            return written > 0 ? sb.ToString() : "none";
        }

        private void BuildRoadsLayerLikeOriginal(ParsedMap map, Transform parent, ref Bounds terrainBounds)
        {
            // Heavy road reconstruction lives in C2TerrainRoadsNetTextureLikeOriginal.cs.
            // Keep this original entry point small so terrain/textures/shadows/color pipeline stay untouched.
            BuildRoadsNetTextureLayerLikeOriginal(map, parent, ref terrainBounds);
        }

        private bool AppendRoadLinkMeshLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            RoadMeshBucketLikeOriginal bucket,
            int ax,
            int ay,
            int bx,
            int by,
            RoadDescLikeOriginal desc)
        {
            float dx0 = ax - bx;
            float dy0 = ay - by;
            float nn = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0);
            if (nn < 0.001f)
                return false;

            float halfWidth = Mathf.Max(4.0f, desc.RWidth * 0.5f);
            float ox = dx0 * halfWidth / nn;
            float oy = dy0 * halfWidth / nn;

            Vector2 p0 = new Vector2(ax - ox, ay - oy);
            Vector2 p1 = new Vector2(bx + ox, by + oy);
            float length = Vector2.Distance(p0, p1);
            int steps = Mathf.Clamp(Mathf.CeilToInt(length / C2RoadInterpolationStepLikeOriginal), 1, 4096);
            int firstVertex = bucket.Vertices.Count;
            float uPos = 0.0f;
            Vector2 prevCenter = p0;

            for (int i = 0; i <= steps; i++)
            {
                float t = i / Mathf.Max(1.0f, steps);
                Vector2 center = Vector2.Lerp(p0, p1, t);
                Vector2 prev = i > 0 ? Vector2.Lerp(p0, p1, (i - 1) / Mathf.Max(1.0f, steps)) : center;
                Vector2 next = i < steps ? Vector2.Lerp(p0, p1, (i + 1) / Mathf.Max(1.0f, steps)) : center;

                Vector2 tangent = next - prev;
                if (tangent.sqrMagnitude < 0.001f)
                    tangent = p1 - p0;
                tangent.Normalize();

                Vector2 perp = new Vector2(tangent.y, -tangent.x);
                Vector2 left = center + new Vector2(perp.x * halfWidth, perp.y * halfWidth * 1.2f);
                Vector2 right = center - new Vector2(perp.x * halfWidth, perp.y * halfWidth * 1.2f);

                if (i > 0)
                    uPos += Vector2.Distance(prevCenter, center) * Mathf.Max(1, desc.RScaleX) / 100.0f;
                prevCenter = center;

                float u = uPos / Mathf.Max(1.0f, desc.TexSizeX);
                float v0 = desc.ReliefY0 / Mathf.Max(1.0f, desc.TexSizeY);
                float v1 = desc.ReliefY1 / Mathf.Max(1.0f, desc.TexSizeY);

                Color32 leftColor = BuildRoadVertexColorLikeOriginal(desc, i, steps, side: 0);
                Color32 rightColor = BuildRoadVertexColorLikeOriginal(desc, i, steps, side: 1);

                bucket.AddVertex(RoadMapPointToWorldLikeOriginal(map, kernel, left.x, left.y), new Vector2(u, v0), leftColor);
                bucket.AddVertex(RoadMapPointToWorldLikeOriginal(map, kernel, right.x, right.y), new Vector2(u, v1), rightColor);
            }

            for (int i = 0; i < steps; i++)
            {
                int i2 = firstVertex + i * 2;
                // Same index order as original RoadMesh: 0,3,1 and 0,2,3.
                bucket.Triangles.Add(i2);
                bucket.Triangles.Add(i2 + 3);
                bucket.Triangles.Add(i2 + 1);
                bucket.Triangles.Add(i2);
                bucket.Triangles.Add(i2 + 2);
                bucket.Triangles.Add(i2 + 3);
            }

            return true;
        }

        private Vector3 RoadMapPointToWorldLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, float x, float y)
        {
            float gx = x / 32.0f;
            float gy = y / 32.0f;

            float rawX = gx * kernel.BackingStepXWorld;
            int ix = Mathf.FloorToInt(gx);
            float rawZ = gy * kernel.BackingStepZWorld + (((ix & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f);

            float worldX = rawX - kernel.CenterX;
            float worldZ = (rawZ - kernel.CenterZ) * WorldZSign;
            float worldY = SampleRoadHeightLikeOriginal(map, gx, gy) * kernel.HeightScale + C2RoadsLayerYOffsetV1LikeOriginal;
            return new Vector3(worldX, worldY, worldZ);
        }

        private static float SampleRoadHeightLikeOriginal(ParsedMap map, float gx, float gy)
        {
            if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 1 || map.MaxTH <= 1)
                return 0.0f;

            gx = Mathf.Clamp(gx, 0.0f, map.VertInLine - 1.001f);
            gy = Mathf.Clamp(gy, 0.0f, map.MaxTH - 1.001f);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, map.VertInLine - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(gy), 0, map.MaxTH - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, map.VertInLine - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, map.MaxTH - 1);

            float tx = gx - x0;
            float ty = gy - y0;

            float h00 = map.Heights[y0 * map.VertInLine + x0];
            float h10 = map.Heights[y0 * map.VertInLine + x1];
            float h01 = map.Heights[y1 * map.VertInLine + x0];
            float h11 = map.Heights[y1 * map.VertInLine + x1];

            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, ty);
        }

        private static Color32 BuildRoadVertexColorLikeOriginal(RoadDescLikeOriginal desc, int pointIndex, int lastPointIndex, int side)
        {
            int seed = unchecked(pointIndex * Mathf.Max(1, desc.AlphaFrequency) * 1103515245 + side * 12345 + desc.Type * 97);
            int w = (seed >> 8) & 32767;
            int alpha = desc.AFactorMin + (((desc.AFactor - desc.AFactorMin) * w) >> 15);
            alpha = Mathf.Clamp(alpha, 0, 255);

            int fade = 255;
            if (pointIndex < 8)
                fade = Mathf.Min(fade, pointIndex * 32);
            if (pointIndex > lastPointIndex - 8)
                fade = Mathf.Min(fade, Mathf.Max(0, lastPointIndex - pointIndex) * 32);

            alpha = (alpha * fade) >> 8;

            int r = Mathf.Clamp((desc.RFactor * alpha) >> 7, 0, 255);
            int g = Mathf.Clamp((desc.GFactor * alpha) >> 7, 0, 255);
            int b = Mathf.Clamp((desc.BFactor * alpha) >> 7, 0, 255);

            return new Color32((byte)r, (byte)g, (byte)b, (byte)alpha);
        }

        private Material CreateRoadMaterialLikeOriginal(RoadDescLikeOriginal desc)
        {
            Shader shader = Shader.Find("Cossacks2Bridge/RoadLayerV17")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV16")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV15")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV14")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV13")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV11")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV10")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV9")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV8")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV6")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV4")
                            ?? Shader.Find("Cossacks2Bridge/RoadLayerV1")
                            ?? Shader.Find("Unlit/Transparent")
                            ?? Shader.Find("Sprites/Default");

            var mat = new Material(shader)
            {
                name = $"C2_RoadLayer_V17_{desc.Type:00}_{SanitizeRoadNameLikeAdapted(desc.RoadName)}",
                renderQueue = C2RoadLateRenderQueueV13LikeOriginal
            };

            Texture2D tex = TryLoadRoadTextureLikeOriginal(desc, out string resolvedPath);
            if (tex == null)
                tex = Texture2D.whiteTexture;

            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            if (mat.HasProperty("_ZWrite"))
                mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest"))
                mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            string roadTex = desc != null ? (desc.TexturePath ?? string.Empty) : string.Empty;
            bool road6 = roadTex.IndexOf("road6", StringComparison.OrdinalIgnoreCase) >= 0;

            bool useTextureAlpha = ShouldUseRoadTextureAlphaV8LikeOriginal(desc);
            bool vFlip = ShouldFlipRoadTextureV8LikeOriginal(desc);
            bool wideRoad = IsWideRoadTextureV8LikeOriginal(desc);

            // V18: road6.tga has weak/uneven alpha on some maps. The old generic wide-road
            // D3D V-flip can sample the almost-transparent half, so road6 becomes faded,
            // especially on the left side of Skirmish maps. Keep texture alpha, but disable
            // road6 V-flip and strengthen the RGB fallback path.
            if (road6)
                vFlip = false;

            float roadColorBoost = road6 ? 4.00f : (wideRoad ? 2.65f : 2.00f);
            float roadAlphaBoost = road6 ? 3.50f : 2.00f;
            float roadRgbAlphaFallback = road6 ? 0.65f : (wideRoad ? 0.25f : 0.00f);
            float roadRgbAlphaBoost = road6 ? 2.50f : (wideRoad ? 1.85f : 1.00f);

            if (mat.HasProperty("_RoadColorBoost"))
                mat.SetFloat("_RoadColorBoost", roadColorBoost);
            if (mat.HasProperty("_RoadAlphaBoost"))
                mat.SetFloat("_RoadAlphaBoost", roadAlphaBoost);
            if (mat.HasProperty("_RoadAlphaRef"))
                mat.SetFloat("_RoadAlphaRef", 16.0f / 255.0f);
            if (mat.HasProperty("_UseTextureAlpha"))
                mat.SetFloat("_UseTextureAlpha", useTextureAlpha ? 1.0f : 0.0f);
            if (mat.HasProperty("_RoadVFlip"))
                mat.SetFloat("_RoadVFlip", vFlip ? 1.0f : 0.0f);
            if (mat.HasProperty("_RoadRgbAlphaFallback"))
                mat.SetFloat("_RoadRgbAlphaFallback", roadRgbAlphaFallback);
            if (mat.HasProperty("_RoadRgbAlphaBoost"))
                mat.SetFloat("_RoadRgbAlphaBoost", roadRgbAlphaBoost);
            if (mat.HasProperty("_RoadClipDepthPull"))
                mat.SetFloat("_RoadClipDepthPull", C2RoadScreenDepthPullV17LikeOriginal);
            if (mat.HasProperty("_RoadUseSceneDepth"))
                mat.SetFloat("_RoadUseSceneDepth", 0.0f);
            if (mat.HasProperty("_RoadSceneDepthTolerance"))
                mat.SetFloat("_RoadSceneDepthTolerance", C2RoadSceneDepthToleranceV15LikeOriginal);

            UnityEngine.Debug.Log(
                $"[C2:ROADS MATERIAL V18] type={desc.Type} name='{desc.RoadName}' tex='{resolvedPath}' useTextureAlpha={useTextureAlpha} " +
                $"vFlip={vFlip} road6={road6} colorBoost={roadColorBoost:F2} alphaBoost={roadAlphaBoost:F2} rgbAlphaFallback={roadRgbAlphaFallback:F2} rgbAlphaBoost={roadRgbAlphaBoost:F2} " +
                $"reason={(road6 ? "road6_no_vflip_rgb_alpha_repair" : (wideRoad ? "wide_d3d_v_origin_texture_alpha" : "trail_keep_V5"))} zTest=LEqual sceneDepthTolerance=off zWrite=Off depthBias=Offset(-1,-1) lateQueue=3600 screenDepthPull={C2RoadScreenDepthPullV17LikeOriginal:F4} sceneDepthTolerance=0.00 reason2=exact_terrain_triangle_height_smallBias_noMountainBleed width={desc.RWidth}");

            return mat;
        }


        private static bool ShouldUseRoadTextureAlphaV8LikeOriginal(RoadDescLikeOriginal desc)
        {
            // Original road.xml:
            // AlphaOp=Modulate2x(Texture, Diffuse), AlphaRef=16.
            // V7 violated this for wide roads and produced only a wide dark silhouette.
            return true;
        }

        private static bool IsWideRoadTextureV8LikeOriginal(RoadDescLikeOriginal desc)
        {
            if (desc == null)
                return false;

            string tex = desc.TexturePath ?? string.Empty;
            if (desc.Type == 11 || desc.Type == 12)
                return false;
            if (tex.IndexOf("tropinka", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return desc.Type == 0 || desc.Type == 14 || desc.Type == 16 || desc.RWidth >= 110 ||
                   tex.IndexOf("doroga", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   tex.IndexOf("road6", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   tex.IndexOf("road7", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldFlipRoadTextureV8LikeOriginal(RoadDescLikeOriginal desc)
        {
            // Direct3D road UVs use top-origin V from Roads.dat (ReliefY0/ReliefY1).
            // Unity samples bottom-origin textures. Trails looked correct already, so only
            // wide road textures get the D3D V-origin correction.
            return IsWideRoadTextureV8LikeOriginal(desc);
        }

        private Material CreateRoadBodyUnderlayMaterialV6LikeOriginal(RoadDescLikeOriginal desc, TerrainTextureResourcesLikeOriginal resources)
        {
            Shader shader = Shader.Find("Cossacks2Bridge/RoadBodyUnderlayV6")
                            ?? Shader.Find("Unlit/Transparent")
                            ?? Shader.Find("Sprites/Default");

            var mat = new Material(shader)
            {
                name = $"C2_RoadBodyUnderlay_V6_{desc.Type:00}_{SanitizeRoadNameLikeAdapted(desc.RoadName)}",
                renderQueue = C2RoadLateRenderQueueV13LikeOriginal - 1
            };

            bool useGroundAtlas = resources != null && resources.GroundAtlas != null && desc.MapTextureID >= 0;
            string fallbackPath = string.Empty;
            Texture2D tex = useGroundAtlas ? resources.GroundAtlas : TryLoadRoadTextureLikeOriginal(desc, out fallbackPath);
            if (tex == null)
                tex = Texture2D.whiteTexture;

            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_RoadBodyOpacity"))
                mat.SetFloat("_RoadBodyOpacity", useGroundAtlas ? 0.92f : 0.55f);
            if (mat.HasProperty("_RoadBodyColorBoost"))
                mat.SetFloat("_RoadBodyColorBoost", useGroundAtlas ? 1.06f : 2.35f);
            if (mat.HasProperty("_UseTextureAlpha"))
                mat.SetFloat("_UseTextureAlpha", useGroundAtlas ? 0.0f : 0.35f);

            string source = useGroundAtlas ? (resources.GroundAtlasPath ?? "GroundTex.bmp") : fallbackPath;
            UnityEngine.Debug.Log(
                $"[C2:ROADS BODY TEX V6] type={desc.Type} name='{desc.RoadName}' source='{source}' useGroundAtlas={useGroundAtlas} " +
                $"mapTex={desc.MapTextureID} facture={desc.FactureID} texR={desc.TexRMin}->{desc.TexRMax} width={desc.RWidth}");
            return mat;
        }

        private Texture2D TryLoadRoadTextureLikeOriginal(RoadDescLikeOriginal desc, out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (_bootstrap == null || _bootstrap.Fs == null || desc == null || string.IsNullOrWhiteSpace(desc.TexturePath))
                return null;

            string tex = desc.TexturePath.Trim();
            string fileName = SafeRoadFileNameLikeOriginal(tex);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = tex;

            var candidates = new[]
            {
                tex,
                tex.Replace('/', '\\'),
                @"Textures\" + fileName,
                @"textures\" + fileName,
                @"Roads\" + fileName,
                @"roads\" + fileName,
                @"Road\" + fileName,
                @"road\" + fileName,
                fileName
            };

            C2OriginalImageData image = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                string request = candidates[i];
                if (string.IsNullOrWhiteSpace(request))
                    continue;

                if (C2OriginalImageIO.TryReadImage(_bootstrap.Fs, request, out image, out resolvedPath) && image != null)
                    break;

                if (C2OriginalImageIO.TryResolveImagePath(_bootstrap.Fs, request, out resolvedPath))
                {
                    try
                    {
                        byte[] bytes = _bootstrap.Fs.ReadAllBytes(resolvedPath);
                        image = TryCreateRoadImageFromBytesV6LikeOriginal(bytes, resolvedPath);
                        if (image != null)
                            break;
                    }
                    catch
                    {
                        image = null;
                    }
                }
            }

            if (image == null)
            {
                UnityEngine.Debug.LogWarning($"[C2:ROADS TEX V8] failed type={desc.Type} name='{desc.RoadName}' request='{desc.TexturePath}' file='{fileName}' candidates={candidates.Length}");
                return null;
            }

            var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false, false)
            {
                name = $"RoadV8_{desc.Type:00}_{fileName}",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                anisoLevel = 1
            };
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.SetPixels32(image.Pixels);
            texture.Apply(false, false);

            RoadTextureStatsV6LikeOriginal(image.Pixels, out int minA, out int maxA, out int avgA, out int avgRgb);
            UnityEngine.Debug.Log($"[C2:ROADS TEX V8] loaded type={desc.Type} name='{desc.RoadName}' tex='{resolvedPath}' size={image.Width}x{image.Height} a={minA}/{avgA}/{maxA} rgbAvg={avgRgb} vRelief={desc.ReliefY0}->{desc.ReliefY1} vColor={desc.ColorY0}->{desc.ColorY1}");
            return texture;
        }

        private static string SafeRoadFileNameLikeOriginal(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            try
            {
                string p = path.Trim().Replace('/', '\\');
                int slash = p.LastIndexOf('\\');
                return slash >= 0 ? p.Substring(slash + 1) : p;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static C2OriginalImageData TryCreateRoadImageFromBytesV6LikeOriginal(byte[] bytes, string path)
        {
            if (bytes == null || bytes.Length < 18)
                return null;

            string ext = Path.GetExtension(path) ?? string.Empty;
            if (!ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                return C2OriginalImageIO.CreateImageFromBytes(bytes, path);

            int idLen = bytes[0];
            int colorMapType = bytes[1];
            int imageType = bytes[2];
            int width = bytes[12] | (bytes[13] << 8);
            int height = bytes[14] | (bytes[15] << 8);
            int bpp = bytes[16];
            int desc = bytes[17];
            if (colorMapType != 0 || width <= 0 || height <= 0)
                return null;
            if (bpp != 24 && bpp != 32)
                return null;
            if (imageType != 2 && imageType != 10)
                return null;

            int bytesPerPixel = bpp / 8;
            int offset = 18 + idLen;
            if (offset < 18 || offset >= bytes.Length)
                return null;

            bool originTop = (desc & 0x20) != 0;
            Color32[] pixels = new Color32[width * height];

            if (imageType == 2)
            {
                int expected = width * height * bytesPerPixel;
                if (offset + expected > bytes.Length)
                    return null;

                for (int y = 0; y < height; y++)
                {
                    int srcY = originTop ? y : (height - 1 - y);
                    int dstY = y;
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = offset + (srcY * width + x) * bytesPerPixel;
                        byte b = bytes[srcIndex + 0];
                        byte g = bytes[srcIndex + 1];
                        byte r = bytes[srcIndex + 2];
                        byte a = bytesPerPixel >= 4 ? bytes[srcIndex + 3] : (byte)255;
                        pixels[dstY * width + x] = new Color32(r, g, b, a);
                    }
                }

                return new C2OriginalImageData(width, height, pixels, path);
            }

            int px = 0;
            int src = offset;
            while (px < pixels.Length && src < bytes.Length)
            {
                byte header = bytes[src++];
                int count = (header & 0x7F) + 1;
                bool rle = (header & 0x80) != 0;

                if (rle)
                {
                    if (src + bytesPerPixel > bytes.Length)
                        return null;
                    Color32 c = ReadRoadTgaPixelV4LikeOriginal(bytes, src, bytesPerPixel);
                    src += bytesPerPixel;
                    for (int k = 0; k < count && px < pixels.Length; k++, px++)
                        WriteRoadTgaPixelV4LikeOriginal(pixels, width, height, px, originTop, c);
                }
                else
                {
                    for (int k = 0; k < count && px < pixels.Length; k++, px++)
                    {
                        if (src + bytesPerPixel > bytes.Length)
                            return null;
                        Color32 c = ReadRoadTgaPixelV4LikeOriginal(bytes, src, bytesPerPixel);
                        src += bytesPerPixel;
                        WriteRoadTgaPixelV4LikeOriginal(pixels, width, height, px, originTop, c);
                    }
                }
            }

            return px == pixels.Length ? new C2OriginalImageData(width, height, pixels, path) : null;
        }

        private static Color32 ReadRoadTgaPixelV4LikeOriginal(byte[] bytes, int src, int bytesPerPixel)
        {
            byte b = bytes[src + 0];
            byte g = bytes[src + 1];
            byte r = bytes[src + 2];
            byte a = bytesPerPixel >= 4 ? bytes[src + 3] : (byte)255;
            return new Color32(r, g, b, a);
        }

        private static void WriteRoadTgaPixelV4LikeOriginal(Color32[] pixels, int width, int height, int linearIndex, bool originTop, Color32 c)
        {
            int x = linearIndex % width;
            int sourceY = linearIndex / width;
            int y = originTop ? sourceY : (height - 1 - sourceY);
            pixels[y * width + x] = c;
        }

        private static void RoadTextureStatsV6LikeOriginal(Color32[] pixels, out int minA, out int maxA, out int avgA, out int avgRgb)
        {
            minA = 255;
            maxA = 0;
            long sumA = 0;
            long sumRgb = 0;
            int n = pixels != null ? pixels.Length : 0;
            if (n <= 0)
            {
                minA = 0;
                maxA = 0;
                avgA = 0;
                avgRgb = 0;
                return;
            }

            for (int i = 0; i < n; i++)
            {
                Color32 p = pixels[i];
                int a = p.a;
                if (a < minA) minA = a;
                if (a > maxA) maxA = a;
                sumA += a;
                sumRgb += (p.r + p.g + p.b) / 3;
            }

            avgA = (int)(sumA / n);
            avgRgb = (int)(sumRgb / n);
        }

        private List<RoadDescLikeOriginal> LoadRoadDescsLikeOriginal()
        {
            var result = new List<RoadDescLikeOriginal>();
            if (_bootstrap == null || _bootstrap.Fs == null)
                return result;

            string[] candidates =
            {
                "Roads.dat",
                @"Settings\Roads.dat",
                @"Roads\Roads.dat",
                @"Terrain\Roads.dat"
            };

            string source = string.Empty;
            string text = string.Empty;
            for (int i = 0; i < candidates.Length; i++)
            {
                string rel = candidates[i];
                if (!_bootstrap.Fs.Exists(rel))
                    continue;

                try
                {
                    byte[] bytes = _bootstrap.Fs.ReadAllBytes(rel);
                    text = System.Text.Encoding.Default.GetString(bytes);
                    source = rel;
                    break;
                }
                catch
                {
                }
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                UnityEngine.Debug.LogWarning("[C2:ROADS DAT V2] Roads.dat not found. Using fallback descriptors.");
                return result;
            }

            RoadDescLikeOriginal previous = null;
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = StripRoadCommentLikeOriginal(lines[lineIndex]).Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] t = SplitRoadTokensLikeOriginal(line);
                if (t.Length == 0)
                    continue;

                if (string.Equals(t[0], "$EX", StringComparison.OrdinalIgnoreCase))
                {
                    if (previous != null && t.Length >= 6)
                    {
                        RoadDescLikeOriginal clone = previous.Clone(result.Count);
                        clone.MapTextureID = ParseRoadIntLikeOriginal(t[1], clone.MapTextureID);
                        clone.TexRMin = ParseRoadIntLikeOriginal(t[2], clone.TexRMin);
                        clone.TexRMax = ParseRoadIntLikeOriginal(t[3], clone.TexRMax);
                        clone.TexSmoothness = ParseRoadIntLikeOriginal(t[4], clone.TexSmoothness);
                        clone.FactureID = ParseRoadIntLikeOriginal(t[5], clone.FactureID);
                        result.Add(clone);
                        previous = clone;
                    }
                    continue;
                }

                if (string.Equals(t[0], "$BUMP", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(t[0], "$PREVIEW", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(t[0], "$PHYS", StringComparison.OrdinalIgnoreCase))
                    continue;

                // name texture texSizeX texSizeY ColorY0 ColorY1 ReliefY0 ReliefY1 RWidth RScaleX AlphaFrequency AFactor AFactorMin RFactor GFactor BFactor
                if (t.Length < 16)
                    continue;

                var d = new RoadDescLikeOriginal
                {
                    Type = result.Count,
                    RoadName = t[0],
                    TexturePath = t[1],
                    TexSizeX = Mathf.Max(1, ParseRoadIntLikeOriginal(t[2], 256)),
                    TexSizeY = Mathf.Max(1, ParseRoadIntLikeOriginal(t[3], 256)),
                    ColorY0 = ParseRoadIntLikeOriginal(t[4], 0),
                    ColorY1 = ParseRoadIntLikeOriginal(t[5], 0),
                    ReliefY0 = ParseRoadIntLikeOriginal(t[6], 0),
                    ReliefY1 = ParseRoadIntLikeOriginal(t[7], 256),
                    RWidth = Mathf.Max(4, ParseRoadIntLikeOriginal(t[8], 96)),
                    RScaleX = Mathf.Max(1, ParseRoadIntLikeOriginal(t[9], 100)),
                    AlphaFrequency = Mathf.Max(1, ParseRoadIntLikeOriginal(t[10], 1)),
                    AFactor = Mathf.Clamp(ParseRoadIntLikeOriginal(t[11], 160), 0, 255),
                    AFactorMin = Mathf.Clamp(ParseRoadIntLikeOriginal(t[12], 96), 0, 255),
                    RFactor = Mathf.Clamp(ParseRoadIntLikeOriginal(t[13], 128), 0, 255),
                    GFactor = Mathf.Clamp(ParseRoadIntLikeOriginal(t[14], 128), 0, 255),
                    BFactor = Mathf.Clamp(ParseRoadIntLikeOriginal(t[15], 128), 0, 255),
                    MapTextureID = -1,
                    FactureID = -1,
                    BumpTextureID = -1
                };

                result.Add(d);
                previous = d;
            }

            UnityEngine.Debug.Log($"[C2:ROADS DAT V2] source='{source}' descs={result.Count}");
            for (int i = 0; i < result.Count; i++)
            {
                RoadDescLikeOriginal d = result[i];
                if (d != null && (d.Type == 0 || d.Type == 11 || d.Type == 14 || d.Type == 16 || d.RWidth >= 80 || d.MapTextureID >= 0))
                {
                    UnityEngine.Debug.Log($"[C2:ROADS DESC V6] type={d.Type} name='{d.RoadName}' tex='{d.TexturePath}' width={d.RWidth} mapTex={d.MapTextureID} facture={d.FactureID} texR={d.TexRMin}->{d.TexRMax} relief={d.ReliefY0}->{d.ReliefY1} alpha={d.AFactorMin}->{d.AFactor} rgb={d.RFactor},{d.GFactor},{d.BFactor}");
                }
            }
            return result;
        }

        private static List<RoadDescLikeOriginal> BuildFallbackRoadDescsLikeOriginal()
        {
            var list = new List<RoadDescLikeOriginal>(64);
            for (int i = 0; i < 64; i++)
            {
                list.Add(new RoadDescLikeOriginal
                {
                    Type = i,
                    RoadName = "fallback",
                    TexturePath = string.Empty,
                    TexSizeX = 256,
                    TexSizeY = 256,
                    ReliefY0 = 0,
                    ReliefY1 = 256,
                    RWidth = 96,
                    RScaleX = 100,
                    AlphaFrequency = 1,
                    AFactor = 150,
                    AFactorMin = 90,
                    RFactor = 116,
                    GFactor = 100,
                    BFactor = 78
                });
            }
            return list;
        }

        private static RoadDescLikeOriginal GetRoadDescLikeOriginal(List<RoadDescLikeOriginal> descs, int type)
        {
            if (descs == null || descs.Count == 0)
                return null;
            type = Mathf.Clamp(type, 0, descs.Count - 1);
            return descs[type];
        }

        private static string StripRoadCommentLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            int semi = s.IndexOf(';');
            int slash = s.IndexOf("//", StringComparison.Ordinal);
            int cut = -1;
            if (semi >= 0)
                cut = semi;
            if (slash >= 0)
                cut = cut >= 0 ? Mathf.Min(cut, slash) : slash;
            return cut >= 0 ? s.Substring(0, cut) : s;
        }

        private static string[] SplitRoadTokensLikeOriginal(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Array.Empty<string>();
            return s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int ParseRoadIntLikeOriginal(string token, int fallback)
        {
            if (string.IsNullOrWhiteSpace(token))
                return fallback;
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return fallback;
        }

        private static string SanitizeRoadNameLikeAdapted(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "road";
            char[] arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char c = arr[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    arr[i] = '_';
            }
            return new string(arr);
        }
    }
}
