using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode : MonoBehaviour
    {
        private const bool C2WaterV1EnabledLikeOriginal = true;
        private const int C2WaterV1RenderQueueLikeOriginal = 3440;
        private const int C2WaterV1SortingOrderLikeOriginal = 31000;
        private const int C2WaterV1MaxQuadsPerMeshLikeOriginal = 20000;
        private const float C2WaterV1SurfaceYOffsetWorldLikeOriginal = 0.16f;
        private const float C2WaterV1DeepThresholdLikeOriginal = 128.0f;
        private const string C2WaterV1ContractLikeOriginal = "V14_DARKER_REFSKY_SHORE_FADE_RANDOM_RIPPLE_PATCHES";

        private GameObject _c2WaterRootV1LikeOriginal;
        private Material _c2WaterMaterialV1LikeOriginal;
        private Texture2D _c2WaterCloudTextureV1LikeOriginal;
        private float _c2WaterLastParamsLogTimeV1LikeOriginal = -1000.0f;
        private string _c2WaterCloudTexturePathV1LikeOriginal = string.Empty;

        private sealed partial class ParsedMap
        {
            public C2WaterData Water = new C2WaterData();
        }

        private static bool TryParseWaterChunkLikeOriginal(string tag, BinaryReader br, ParsedMap map, int payloadLen)
        {
            if (map == null || br == null || payloadLen <= 0)
                return false;

            bool sea2 = TagEqualsLikeOriginal(tag, "SEA2", "2AES");
            bool riv1 = TagEqualsLikeOriginal(tag, "RIV1", "1VIR");
            bool rgbw = TagEqualsLikeOriginal(tag, "RGBW", "WBGR");
            if (!sea2 && !riv1 && !rgbw)
                return false;

            byte[] payload = br.ReadBytes(payloadLen);
            if (payload == null || payload.Length <= 0)
                return true;

            if (map.Water == null)
                map.Water = new C2WaterData();

            string info;
            bool ok;
            if (sea2)
                ok = TryParseSea2PayloadV1LikeOriginal(payload, map.Water, out info);
            else if (riv1)
                ok = TryParseRiv1PayloadV1LikeOriginal(payload, map.Water, out info);
            else
                ok = TryParseRgbwPayloadV1LikeOriginal(payload, map.Water, out info);

            Debug.Log((ok ? "[C2:WATER PARSE V1] " : "[C2:WATER PARSE V1 WARN] ") + tag + " " + info);
            return true;
        }

        private static bool TryParseSea2PayloadV1LikeOriginal(byte[] payload, C2WaterData water, out string info)
        {
            info = "SEA2 empty";
            if (payload == null || water == null || payload.Length < 8)
                return false;

            using (var ms = new MemoryStream(payload, false))
            using (var br = new BinaryReader(ms))
            {
                int lx = br.ReadInt32();
                int ly = br.ReadInt32();
                long countLong = (long)lx * ly;
                long expected = 8 + countLong * 2;
                if (lx <= 0 || ly <= 0 || lx > 8192 || ly > 8192 || countLong <= 0 || expected > payload.Length)
                {
                    info = $"SEA2 rejected lx={lx} ly={ly} payload={payload.Length} expected={expected}";
                    return false;
                }

                int count = (int)countLong;
                water.SeaLx = lx;
                water.SeaLy = ly;
                water.WaterDeep = new byte[count];
                water.WaterBright = new byte[count];

                int deepNonZero = 0;
                int deepAbove128 = 0;
                int shallow = 0;
                int brightNonZero = 0;

                for (int y = 0; y < ly; y++)
                {
                    int row = y * lx;
                    byte[] deepRow = br.ReadBytes(lx);
                    int copy = Mathf.Min(lx, deepRow != null ? deepRow.Length : 0);
                    if (copy > 0)
                        Buffer.BlockCopy(deepRow, 0, water.WaterDeep, row, copy);

                    for (int x = 0; x < copy; x++)
                    {
                        byte d = water.WaterDeep[row + x];
                        if (d != 0)
                            deepNonZero++;
                        if (d > C2WaterV1DeepThresholdLikeOriginal)
                            deepAbove128++;
                        else if (d != 0)
                            shallow++;
                    }

                    byte[] brightRow = br.ReadBytes(lx);
                    copy = Mathf.Min(lx, brightRow != null ? brightRow.Length : 0);
                    if (copy > 0)
                        Buffer.BlockCopy(brightRow, 0, water.WaterBright, row, copy);

                    for (int x = 0; x < copy; x++)
                    {
                        if (water.WaterBright[row + x] != 0)
                            brightNonZero++;
                    }
                }

                water.SeaDeepNonZeroCount = deepNonZero;
                water.SeaDeepAbove128Count = deepAbove128;
                water.SeaDeepShallowCount = shallow;
                water.SeaBrightNonZeroCount = brightNonZero;
                info = $"SEA2 ok lx={lx} ly={ly} deepNonZero={deepNonZero} deepAbove128={deepAbove128} shallow={shallow} brightNonZero={brightNonZero}";
                return true;
            }
        }

        private static bool TryParseRiv1PayloadV1LikeOriginal(byte[] payload, C2WaterData water, out string info)
        {
            info = "RIV1 empty";
            if (payload == null || water == null || payload.Length < 2)
                return false;

            int half = payload.Length / 2;
            water.RivPayloadBytes = payload.Length;
            water.RivDir = new byte[half];
            water.RivVol = new byte[half];
            Buffer.BlockCopy(payload, 0, water.RivDir, 0, half);
            Buffer.BlockCopy(payload, half, water.RivVol, 0, Mathf.Min(half, payload.Length - half));

            int dirNonZero = 0;
            int volNonZero = 0;
            for (int i = 0; i < half; i++)
            {
                if (water.RivDir[i] != 0)
                    dirNonZero++;
                if (water.RivVol[i] != 0)
                    volNonZero++;
            }

            int side = Mathf.RoundToInt(Mathf.Sqrt(half));
            water.RivSize = side * side == half ? side : 0;
            water.RivDirNonZeroCount = dirNonZero;
            water.RivVolNonZeroCount = volNonZero;
            info = $"RIV1 ok bytes={payload.Length} cells={half} side={water.RivSize} dirNonZero={dirNonZero} volNonZero={volNonZero}";
            return true;
        }

        private static bool TryParseRgbwPayloadV1LikeOriginal(byte[] payload, C2WaterData water, out string info)
        {
            info = "RGBW empty";
            if (payload == null || water == null || payload.Length <= 0)
                return false;

            water.RgbPayloadBytes = payload.Length;
            water.RgbSize = 0;
            water.Red = null;
            water.Green = null;
            water.Blue = null;
            water.Vx = null;
            water.Vy = null;
            water.Level = null;

            if (payload.Length >= 24)
            {
                using (var ms = new MemoryStream(payload, false))
                using (var br = new BinaryReader(ms))
                {
                    int[] sizes = new int[6];
                    long total = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        sizes[i] = br.ReadInt32();
                        if (sizes[i] > 0)
                            total += sizes[i];
                    }

                    if (total > 0 && 24 + total <= payload.Length)
                    {
                        TryCopyRgbwPlainMapsV1LikeOriginal(payload, sizes, water);
                        info = $"RGBW indexed bytes={payload.Length} sizes={sizes[0]},{sizes[1]},{sizes[2]},{sizes[3]},{sizes[4]},{sizes[5]} rgbSize={water.RgbSize}";
                    }
                    else
                    {
                        info = $"RGBW raw bytes={payload.Length}";
                    }
                }
            }
            else
            {
                info = $"RGBW raw bytes={payload.Length}";
            }

            return true;
        }

        private static void TryCopyRgbwPlainMapsV1LikeOriginal(byte[] payload, int[] sizes, C2WaterData water)
        {
            if (payload == null || sizes == null || sizes.Length < 6 || water == null)
                return;

            int offset = 24;
            int redPayload = GetWaterMapPlainPayloadBytesV2LikeOriginal(sizes[0], out int redSkip);
            int greenPayload = GetWaterMapPlainPayloadBytesV2LikeOriginal(sizes[1], out int greenSkip);
            int bluePayload = GetWaterMapPlainPayloadBytesV2LikeOriginal(sizes[2], out int blueSkip);
            int rgbCount = redPayload > 0 && redPayload == greenPayload && greenPayload == bluePayload ? redPayload : 0;
            int rgbSide = rgbCount > 0 ? Mathf.RoundToInt(Mathf.Sqrt(rgbCount)) : 0;
            if (rgbSide > 0 && rgbSide * rgbSide == rgbCount && offset + sizes[0] + sizes[1] + sizes[2] <= payload.Length)
            {
                water.RgbSize = rgbSide;
                water.Red = new byte[rgbCount];
                water.Green = new byte[rgbCount];
                water.Blue = new byte[rgbCount];
                Buffer.BlockCopy(payload, offset + redSkip, water.Red, 0, rgbCount);
                offset += sizes[0];
                Buffer.BlockCopy(payload, offset + greenSkip, water.Green, 0, rgbCount);
                offset += sizes[1];
                Buffer.BlockCopy(payload, offset + blueSkip, water.Blue, 0, rgbCount);
                offset += sizes[2];
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    offset += Mathf.Max(0, sizes[i]);
            }

            int vxPayload = GetWaterMapPlainPayloadBytesV2LikeOriginal(sizes[3], out int vxSkip);
            int vyPayload = GetWaterMapPlainPayloadBytesV2LikeOriginal(sizes[4], out int vySkip);
            int levelPayload = GetWaterMapPlainPayloadBytesV2LikeOriginal(sizes[5], out int levelSkip);
            int vectorCount = vxPayload > 0 && vxPayload == vyPayload && vyPayload == levelPayload && (vxPayload & 1) == 0 ? vxPayload / 2 : 0;
            if (vectorCount <= 0 || offset + sizes[3] + sizes[4] + sizes[5] > payload.Length)
                return;

            water.Vx = new short[vectorCount];
            water.Vy = new short[vectorCount];
            water.Level = new short[vectorCount];
            Buffer.BlockCopy(payload, offset + vxSkip, water.Vx, 0, vxPayload);
            offset += sizes[3];
            Buffer.BlockCopy(payload, offset + vySkip, water.Vy, 0, vyPayload);
            offset += sizes[4];
            Buffer.BlockCopy(payload, offset + levelSkip, water.Level, 0, levelPayload);
        }

        private static int GetWaterMapPlainPayloadBytesV2LikeOriginal(int sectionSize, out int headerSkip)
        {
            headerSkip = 0;
            if (sectionSize <= 0)
                return 0;

            int side = Mathf.RoundToInt(Mathf.Sqrt(sectionSize));
            if (side > 0 && side * side == sectionSize)
                return sectionSize;

            int withoutHeader = sectionSize - 4;
            side = withoutHeader > 0 ? Mathf.RoundToInt(Mathf.Sqrt(withoutHeader)) : 0;
            if (side > 0 && side * side == withoutHeader)
            {
                headerSkip = 4;
                return withoutHeader;
            }

            if (withoutHeader > 0 && (withoutHeader & 1) == 0)
            {
                headerSkip = 4;
                return withoutHeader;
            }

            return sectionSize;
        }

        private void BuildWaterLayerV1LikeOriginal(ParsedMap map, Transform parent, ref Bounds terrainBounds)
        {
            if (_c2WaterRootV1LikeOriginal != null)
                SafeDestroy(_c2WaterRootV1LikeOriginal);
            _c2WaterRootV1LikeOriginal = null;

            if (!C2WaterV1EnabledLikeOriginal || map == null || parent == null)
                return;

            C2WaterData water = map.Water;
            if (water == null || !water.HasRenderableWater)
            {
                Debug.Log("[C2:WATER V1] skipped " + (water != null ? water.BuildSummary() : "water=null") + " contract=" + C2WaterV1ContractLikeOriginal);
                return;
            }

            if (map.IsMeshSurface)
            {
                Debug.LogWarning("[C2:WATER V1] skipped HSEM mesh-surface map for first pass. " + water.BuildSummary());
                return;
            }

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(map);
            int minX = Mathf.Clamp(kernel.MinCellX, 0, Mathf.Max(0, water.SeaLx - 2));
            int minY = Mathf.Clamp(kernel.MinCellY, 0, Mathf.Max(0, water.SeaLy - 2));
            int maxX = Mathf.Clamp(kernel.MaxCellXExclusive, minX + 1, Mathf.Max(minX + 1, water.SeaLx - 1));
            int maxY = Mathf.Clamp(kernel.MaxCellYExclusive, minY + 1, Mathf.Max(minY + 1, water.SeaLy - 1));
            int cellStep = PickWaterCellStepV1LikeOriginal(water, maxX - minX, maxY - minY);

            _c2WaterMaterialV1LikeOriginal = CreateWaterMaterialV1LikeOriginal();
            if (_c2WaterMaterialV1LikeOriginal == null)
            {
                Debug.LogWarning("[C2:WATER V1] no material/shader; skipped water.");
                return;
            }

            _c2WaterRootV1LikeOriginal = new GameObject("C2_WaterLikeOriginal_V1");
            _c2WaterRootV1LikeOriginal.transform.SetParent(parent, false);

            int meshCount = 0;
            int quadCount = 0;
            int candidateBlocks = 0;
            var chunk = new WaterMeshChunkV1LikeOriginal(C2WaterV1MaxQuadsPerMeshLikeOriginal);
            Bounds waterBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasWaterBounds = false;

            for (int y = minY; y < maxY; y += cellStep)
            {
                for (int x = minX; x < maxX; x += cellStep)
                {
                    if (!TrySampleWaterQuadV2LikeOriginal(water, x, y, cellStep, out WaterVertexV2LikeOriginal v0, out WaterVertexV2LikeOriginal v1, out WaterVertexV2LikeOriginal v2, out WaterVertexV2LikeOriginal v3, out byte maxDeep))
                        continue;

                    candidateBlocks++;
                    AddWaterQuadV1LikeOriginal(chunk, kernel, x, y, cellStep, v0, v1, v2, v3, ref waterBounds, ref hasWaterBounds);
                    quadCount++;

                    if (chunk.QuadCount >= C2WaterV1MaxQuadsPerMeshLikeOriginal)
                    {
                        CreateWaterChunkObjectV1LikeOriginal(meshCount++, chunk, _c2WaterRootV1LikeOriginal.transform);
                        chunk.Clear();
                    }
                }
            }

            if (chunk.QuadCount > 0)
                CreateWaterChunkObjectV1LikeOriginal(meshCount++, chunk, _c2WaterRootV1LikeOriginal.transform);

            if (hasWaterBounds)
                terrainBounds.Encapsulate(waterBounds);

            ApplyWaterMaterialParamsV1LikeOriginal(forceLog: true);
            Debug.Log($"[C2:WATER V1] built meshes={meshCount} quads={quadCount} blocks={candidateBlocks} step={cellStep} rect=({minX},{minY})->({maxX},{maxY}) cloud='{_c2WaterCloudTexturePathV1LikeOriginal}' summary={water.BuildSummary()} contract={C2WaterV1ContractLikeOriginal}");
        }

        private static int PickWaterCellStepV1LikeOriginal(C2WaterData water, int width, int height)
        {
            int area = Mathf.Max(0, width) * Mathf.Max(0, height);
            if (area > 1200000 || water.SeaDeepAbove128Count > 350000)
                return 3;
            if (area > 520000 || water.SeaDeepAbove128Count > 180000)
                return 2;
            return 1;
        }

        private static bool TrySampleWaterQuadV2LikeOriginal(
            C2WaterData water,
            int x0,
            int y0,
            int step,
            out WaterVertexV2LikeOriginal v0,
            out WaterVertexV2LikeOriginal v1,
            out WaterVertexV2LikeOriginal v2,
            out WaterVertexV2LikeOriginal v3,
            out byte maxDeep)
        {
            v0 = default;
            v1 = default;
            v2 = default;
            v3 = default;
            maxDeep = 0;
            if (water == null || !water.HasSea2Payload)
                return false;

            int s = Mathf.Max(1, step);
            int x1 = Mathf.Min(water.SeaLx - 1, x0 + s);
            int y1 = Mathf.Min(water.SeaLy - 1, y0 + s);
            byte d0 = water.GetWaterDeep(x0, y0);
            byte d1 = water.GetWaterDeep(x1, y0);
            byte d2 = water.GetWaterDeep(x0, y1);
            byte d3 = water.GetWaterDeep(x1, y1);
            maxDeep = (byte)Mathf.Max(Mathf.Max(d0, d1), Mathf.Max(d2, d3));
            if (maxDeep <= C2WaterV1DeepThresholdLikeOriginal)
                return false;

            v0 = CreateWaterVertexV2LikeOriginal(water, x0, y0, d0, maxDeep);
            v1 = CreateWaterVertexV2LikeOriginal(water, x1, y0, d1, maxDeep);
            v2 = CreateWaterVertexV2LikeOriginal(water, x0, y1, d2, maxDeep);
            v3 = CreateWaterVertexV2LikeOriginal(water, x1, y1, d3, maxDeep);
            return v0.Color.a > 0.001f || v1.Color.a > 0.001f || v2.Color.a > 0.001f || v3.Color.a > 0.001f;
        }

        private static WaterVertexV2LikeOriginal CreateWaterVertexV2LikeOriginal(C2WaterData water, int x, int y, byte deepByte, byte quadMaxDeep)
        {
            float deep01 = Mathf.Clamp01((deepByte - C2WaterV1DeepThresholdLikeOriginal) / 74.0f);
            float shore01 = Mathf.Clamp01((deepByte - 112.0f) / 30.0f);
            float quadWet01 = Mathf.Clamp01((quadMaxDeep - C2WaterV1DeepThresholdLikeOriginal) / 74.0f);
            float alpha = deepByte > C2WaterV1DeepThresholdLikeOriginal
                ? Mathf.Clamp01(0.48f + deep01 * 0.30f)
                : Mathf.Clamp01(shore01 * (0.018f + quadWet01 * 0.055f));

            Color tint = GetWaterTintV2LikeOriginal(water, x, y, deep01, shore01);
            return new WaterVertexV2LikeOriginal
            {
                Color = new Color(tint.r, tint.g, tint.b, alpha),
                Data = new Vector2(deep01, shore01)
            };
        }

        private static Color GetWaterTintV2LikeOriginal(C2WaterData water, int x, int y, float deep01, float shore01)
        {
            Color shallow = new Color(0.31f, 0.65f, 0.61f, 1.0f);
            Color deep = new Color(0.00f, 0.34f, 0.42f, 1.0f);
            Color tint = Color.Lerp(shallow, deep, deep01);

            if (water != null && water.RgbSize > 0)
            {
                float u = water.SeaLx > 1 ? x / (float)(water.SeaLx - 1) : 0.0f;
                float v = water.SeaLy > 1 ? y / (float)(water.SeaLy - 1) : 0.0f;
                Color32 c = water.GetRgbColor01(u, v);
                float lum = (c.r + c.g + c.b) / (255.0f * 3.0f);
                if (lum > 0.08f)
                {
                    Color rgbw = new Color(c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, 1.0f);
                    tint = Color.Lerp(tint, rgbw, 0.035f);
                }
            }

            return Color.Lerp(tint, new Color(0.42f, 0.72f, 0.63f, 1.0f), Mathf.Clamp01((1.0f - deep01) * shore01) * 0.035f);
        }

        private static void AddWaterQuadV1LikeOriginal(
            WaterMeshChunkV1LikeOriginal chunk,
            OriginalTerrainKernelConfig kernel,
            int cellX,
            int cellY,
            int step,
            WaterVertexV2LikeOriginal v0,
            WaterVertexV2LikeOriginal v1,
            WaterVertexV2LikeOriginal v2,
            WaterVertexV2LikeOriginal v3,
            ref Bounds waterBounds,
            ref bool hasWaterBounds)
        {
            int x1 = cellX + Mathf.Max(1, step);
            int y1 = cellY + Mathf.Max(1, step);
            Vector3 p0 = WaterCellToWorldV1LikeOriginal(kernel, cellX, cellY);
            Vector3 p1 = WaterCellToWorldV1LikeOriginal(kernel, x1, cellY);
            Vector3 p2 = WaterCellToWorldV1LikeOriginal(kernel, cellX, y1);
            Vector3 p3 = WaterCellToWorldV1LikeOriginal(kernel, x1, y1);

            int baseIndex = chunk.Vertices.Count;
            chunk.Vertices.Add(p0);
            chunk.Vertices.Add(p1);
            chunk.Vertices.Add(p2);
            chunk.Vertices.Add(p3);
            chunk.Colors.Add(v0.Color);
            chunk.Colors.Add(v1.Color);
            chunk.Colors.Add(v2.Color);
            chunk.Colors.Add(v3.Color);

            float uvScale = 1.0f / 256.0f;
            chunk.Uv.Add(new Vector2(cellX * uvScale, cellY * uvScale));
            chunk.Uv.Add(new Vector2(x1 * uvScale, cellY * uvScale));
            chunk.Uv.Add(new Vector2(cellX * uvScale, y1 * uvScale));
            chunk.Uv.Add(new Vector2(x1 * uvScale, y1 * uvScale));
            chunk.Uv1.Add(v0.Data);
            chunk.Uv1.Add(v1.Data);
            chunk.Uv1.Add(v2.Data);
            chunk.Uv1.Add(v3.Data);

            chunk.Triangles.Add(baseIndex + 0);
            chunk.Triangles.Add(baseIndex + 2);
            chunk.Triangles.Add(baseIndex + 1);
            chunk.Triangles.Add(baseIndex + 1);
            chunk.Triangles.Add(baseIndex + 2);
            chunk.Triangles.Add(baseIndex + 3);
            chunk.QuadCount++;

            EncapsulateWaterPointV1LikeOriginal(p0, ref waterBounds, ref hasWaterBounds);
            EncapsulateWaterPointV1LikeOriginal(p1, ref waterBounds, ref hasWaterBounds);
            EncapsulateWaterPointV1LikeOriginal(p2, ref waterBounds, ref hasWaterBounds);
            EncapsulateWaterPointV1LikeOriginal(p3, ref waterBounds, ref hasWaterBounds);
        }

        private static Vector3 WaterCellToWorldV1LikeOriginal(OriginalTerrainKernelConfig kernel, int cellX, int cellY)
        {
            float rawX = cellX * kernel.BackingStepXWorld;
            float rawZ = cellY * kernel.BackingStepZWorld + (((cellX & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f);
            return new Vector3(rawX - kernel.CenterX, C2WaterV1SurfaceYOffsetWorldLikeOriginal, (rawZ - kernel.CenterZ) * WorldZSign);
        }

        private static void EncapsulateWaterPointV1LikeOriginal(Vector3 p, ref Bounds bounds, ref bool hasBounds)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(p, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(p);
            }
        }

        private void CreateWaterChunkObjectV1LikeOriginal(int meshIndex, WaterMeshChunkV1LikeOriginal chunk, Transform parent)
        {
            if (chunk == null || chunk.Vertices.Count == 0 || parent == null)
                return;

            var mesh = new Mesh { name = $"C2WaterLikeOriginalV1_{meshIndex:000}" };
            if (chunk.Vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(chunk.Vertices);
            mesh.SetColors(chunk.Colors);
            mesh.SetUVs(0, chunk.Uv);
            mesh.SetUVs(1, chunk.Uv1);
            mesh.SetTriangles(chunk.Triangles, 0, true);
            mesh.RecalculateBounds();

            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = _c2WaterMaterialV1LikeOriginal;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.sortingOrder = C2WaterV1SortingOrderLikeOriginal;
        }

        private Material CreateWaterMaterialV1LikeOriginal()
        {
            Shader shader = Shader.Find("Cossacks2Bridge/WaterLikeOriginalV2");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                return null;

            var mat = new Material(shader);
            mat.name = "C2_WaterLikeOriginal_V14_DarkerRefSkyRandomRipples";
            mat.renderQueue = C2WaterV1RenderQueueLikeOriginal;

            _c2WaterCloudTextureV1LikeOriginal = TryLoadWaterCloudTextureV1LikeOriginal(out _c2WaterCloudTexturePathV1LikeOriginal);
            if (_c2WaterCloudTextureV1LikeOriginal == null)
            {
                _c2WaterCloudTextureV1LikeOriginal = CreateFallbackCloudTextureV1LikeOriginal();
                _c2WaterCloudTexturePathV1LikeOriginal = "generated-fallback";
            }

            if (_c2WaterCloudTextureV1LikeOriginal != null)
            {
                _c2WaterCloudTextureV1LikeOriginal.wrapMode = TextureWrapMode.Repeat;
                _c2WaterCloudTextureV1LikeOriginal.filterMode = FilterMode.Bilinear;
                _c2WaterCloudTextureV1LikeOriginal.anisoLevel = 1;
                _c2WaterCloudTextureV1LikeOriginal.mipMapBias = -0.25f;
            }

            SetTextureIfPresentV1LikeOriginal(mat, "_CloudTex", _c2WaterCloudTextureV1LikeOriginal);
            SetColorIfPresentV1LikeOriginal(mat, "_DeepColor", new Color(0.02f, 0.40f, 0.48f, 1.0f));
            SetColorIfPresentV1LikeOriginal(mat, "_ShallowColor", new Color(0.34f, 0.68f, 0.62f, 1.0f));
            SetColorIfPresentV1LikeOriginal(mat, "_FoamColor", new Color(0.90f, 0.96f, 0.93f, 1.0f));
            SetFloatIfPresentV1LikeOriginal(mat, "_CloudScale", 0.00042f);
            SetFloatIfPresentV1LikeOriginal(mat, "_CloudStrength", 3.50f);
            SetFloatIfPresentV1LikeOriginal(mat, "_WaterOpacity", 0.95f);
            SetFloatIfPresentV1LikeOriginal(mat, "_WaveStrength", 0.74f);
            SetFloatIfPresentV1LikeOriginal(mat, "_SkyReflectStrength", 2.60f);
            SetFloatIfPresentV1LikeOriginal(mat, "_SkyDarkStrength", 0.36f);
            SetFloatIfPresentV1LikeOriginal(mat, "_RippleLineStrength", 0.12f);
            SetFloatIfPresentV1LikeOriginal(mat, "_BumpDistortStrength", 0.014f);
            SetFloatIfPresentV1LikeOriginal(mat, "_CameraInfluence", 0.0f);
            SetFloatIfPresentV1LikeOriginal(mat, "_ScreenCloudScale", 0.0f);
            SetFloatIfPresentV1LikeOriginal(mat, "_ScreenCloudStrength", 0.0f);
            SetFloatIfPresentV1LikeOriginal(mat, "_BottomFadeStrength", 0.38f);
            SetFloatIfPresentV1LikeOriginal(mat, "_RefSkyOverlayStrength", 0.78f);
            SetFloatIfPresentV1LikeOriginal(mat, "_RefSkyOverlayScale", 0.62f);
            return mat;
        }

        private Texture2D TryLoadWaterCloudTextureV1LikeOriginal(out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (_bootstrap == null || _bootstrap.Fs == null)
                return null;

            string[] candidates =
            {
                "textures\\oblaka123g1.tga",
                "Textures\\oblaka123g1.tga",
                "textures\\Oblaka123g1.tga",
                "Textures\\Oblaka123g1.tga",
                "Oblaka123g1.tga"
            };

            return C2OriginalTextureService.TryLoadTextureByCandidates(
                _bootstrap.Fs,
                candidates,
                "C2_WaterCloud_Oblaka123g1",
                C2OriginalTexturePolicy.WorldTextureLikeOriginal,
                out resolvedPath);
        }

        private static Texture2D CreateFallbackCloudTextureV1LikeOriginal()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            tex.name = "C2_WaterCloud_Fallback";
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)size;
                    float ny = y / (float)size;
                    float a = Mathf.Sin((nx * 17.0f + ny * 5.0f) * Mathf.PI * 2.0f);
                    float b = Mathf.Sin((nx * 4.0f - ny * 13.0f + 0.31f) * Mathf.PI * 2.0f);
                    float c = Mathf.Sin((nx * 31.0f + ny * 23.0f + 0.17f) * Mathf.PI * 2.0f);
                    float v = Mathf.Clamp01(0.48f + a * 0.19f + b * 0.12f + c * 0.07f);
                    byte p = (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255.0f), 0, 255);
                    pixels[y * size + x] = new Color32(p, p, p, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 1;
            tex.mipMapBias = -0.25f;
            tex.Apply(true, false);
            return tex;
        }

        private void UpdateWaterRuntimeV1LikeOriginal(bool force)
        {
            if (!C2WaterV1EnabledLikeOriginal || _c2WaterMaterialV1LikeOriginal == null)
                return;

            ApplyWaterMaterialParamsV1LikeOriginal(forceLog: force);
        }

        private void ApplyWaterMaterialParamsV1LikeOriginal(bool forceLog)
        {
            Material mat = _c2WaterMaterialV1LikeOriginal;
            if (mat == null)
                return;

            float time = Time.realtimeSinceStartup;
            float originalTime = time / 46.0f;
            float cameraU = _strictMapX * 0.00004f;
            float cameraV = _strictMapY * 0.00004f;
            float offsetU = originalTime;
            float offsetV = -originalTime * 0.86f;

            SetFloatIfPresentV1LikeOriginal(mat, "_C2Time", time);
            if (mat.HasProperty("_CloudOffset"))
                mat.SetVector("_CloudOffset", new Vector4(offsetU, offsetV, cameraU, cameraV));

            if (forceLog || Time.realtimeSinceStartup - _c2WaterLastParamsLogTimeV1LikeOriginal > 8.0f)
            {
                _c2WaterLastParamsLogTimeV1LikeOriginal = Time.realtimeSinceStartup;
                if (forceLog)
                    Debug.Log($"[C2:WATER V1] params cloudOffset=({offsetU:0.0000},{offsetV:0.0000}) driftPerSec=(0.0217,-0.0187) cloudStrength=3.50 skyReflect=2.60 overlay=0.78 gammaRefSky=true rippleStrength=0.12 randomRipplePatches=true shoreFadeDepth=0.20 cameraSoft=({cameraU:0.0000},{cameraV:0.0000}) queue={C2WaterV1RenderQueueLikeOriginal} contract={C2WaterV1ContractLikeOriginal}");
            }
        }

        private static void SetTextureIfPresentV1LikeOriginal(Material mat, string property, Texture texture)
        {
            if (mat != null && texture != null && mat.HasProperty(property))
                mat.SetTexture(property, texture);
        }

        private static void SetColorIfPresentV1LikeOriginal(Material mat, string property, Color color)
        {
            if (mat != null && mat.HasProperty(property))
                mat.SetColor(property, color);
        }

        private static void SetFloatIfPresentV1LikeOriginal(Material mat, string property, float value)
        {
            if (mat != null && mat.HasProperty(property))
                mat.SetFloat(property, value);
        }

        private sealed class WaterMeshChunkV1LikeOriginal
        {
            public readonly List<Vector3> Vertices;
            public readonly List<Color> Colors;
            public readonly List<Vector2> Uv;
            public readonly List<Vector2> Uv1;
            public readonly List<int> Triangles;
            public int QuadCount;

            public WaterMeshChunkV1LikeOriginal(int maxQuads)
            {
                int vertexCapacity = Mathf.Max(4, maxQuads * 4);
                int indexCapacity = Mathf.Max(6, maxQuads * 6);
                Vertices = new List<Vector3>(vertexCapacity);
                Colors = new List<Color>(vertexCapacity);
                Uv = new List<Vector2>(vertexCapacity);
                Uv1 = new List<Vector2>(vertexCapacity);
                Triangles = new List<int>(indexCapacity);
            }

            public void Clear()
            {
                Vertices.Clear();
                Colors.Clear();
                Uv.Clear();
                Uv1.Clear();
                Triangles.Clear();
                QuadCount = 0;
            }
        }

        private struct WaterVertexV2LikeOriginal
        {
            public Color Color;
            public Vector2 Data;
        }
    }
}
