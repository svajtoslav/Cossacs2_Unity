// C2BuildingSmpSurfaceV1LikeOriginal.cs
// Cossacks II 1.1 building PIECE/SMP terrain stamp.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private struct C2SmpVertexV1LikeOriginal
        {
            public short Dx;
            public short Dy;
            public byte Texture;
            public byte Facture;
            public byte FactureWeight;
            public byte TextureEx;
            public byte TextureExWeight;
            public byte Sector1;
            public byte Sector2;
            public byte Sector3;
            public short Height;
        }

        private struct C2SmpSpriteV1LikeOriginal
        {
            public string Sign;
            public int X;
            public int Y;
            public ushort Index;
        }

        private sealed class C2SmpPieceV1LikeOriginal
        {
            public string Path = string.Empty;
            public int ObjectX;
            public int ObjectY;
            public bool HasObjectVector;
            public readonly List<C2SmpVertexV1LikeOriginal> Vertices = new List<C2SmpVertexV1LikeOriginal>();
            public readonly List<C2SmpSpriteV1LikeOriginal> Sprites = new List<C2SmpSpriteV1LikeOriginal>();
            public int GroundPayloadBytes;
            public int LockPayloadBytes;
            public string BlocksAudit = string.Empty;
        }

        private sealed class C2SmpSurfacePatchV1LikeOriginal
        {
            public int MinCellX;
            public int MaxCellXExclusive;
            public int MinCellY;
            public int MaxCellYExclusive;
            public GameObject Root;
            public Mesh Mesh;
            public Texture2D Texture;
        }

        private readonly Dictionary<string, C2SmpPieceV1LikeOriginal> _c2SmpPieceCacheV1LikeOriginal =
            new Dictionary<string, C2SmpPieceV1LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private readonly List<C2SmpSurfacePatchV1LikeOriginal> _c2SmpSurfacePatchesV1LikeOriginal =
            new List<C2SmpSurfacePatchV1LikeOriginal>();
        private bool C2SmpApplyBuildingPieceV1LikeOriginal(
            C2BuildingMdInfoLikeOriginal md,
            ref int realX,
            ref int realY,
            string source,
            out string audit)
        {
            audit = "not_started";
            if (md == null)
            {
                audit = "md=<null>";
                return false;
            }
            if (_map == null || _map.Heights == null || _map.Heights.Length == 0)
            {
                audit = "piece='" + md.PieceName + "' skipped:no_old_surface";
                return false;
            }

            C2SmpPieceV1LikeOriginal piece = null;
            string parseAudit = "piece=none";
            bool pieceOk = true;
            int vertexOriginX = 0;
            int vertexOriginY = 0;
            int minVertexX = int.MaxValue;
            int minVertexY = int.MaxValue;
            int maxVertexX = int.MinValue;
            int maxVertexY = int.MinValue;
            int pieceChanged = 0;
            int textureChanged = 0;

            if (!string.IsNullOrWhiteSpace(md.PieceName))
            {
                if (!C2SmpTryGetPieceV1LikeOriginal(
                        md.PieceName, out piece, out parseAudit))
                {
                    pieceOk = false;
                }
                else
                {
                    EnsureTileMapsLikeOriginal(_map);
                    EnsureFactureMapsLikeOriginal(_map);

                    // NewMon.cpp::CreateNewMonsterAt:
                    // RM_LoadNotObj(PieceName, rx>>4, ry>>4) ->
                    // RM_LoadVertices((x>>6)<<1,(y>>6)<<1,1).
                    vertexOriginX = ((realX >> 4) >> 6) << 1;
                    vertexOriginY = ((realY >> 4) >> 6) << 1;
                    for (int i = 0; i < piece.Vertices.Count; i++)
                    {
                        C2SmpVertexV1LikeOriginal sv = piece.Vertices[i];
                        int vx = vertexOriginX + sv.Dx;
                        int vy = vertexOriginY + sv.Dy;
                        if (vx <= 0 || vy <= 0 || vx >= _map.VertInLine - 1 || vy >= _map.MaxTH - 1)
                            continue;

                        int v = vx + vy * _map.VertInLine;
                        if (v < 0 || v >= _map.Heights.Length)
                            continue;

                        _map.Heights[v] = unchecked((short)(_map.Heights[v] + sv.Height));

                        // RM_LoadVertices(Vnew=1, CheckHI=0, ImmVis=1).
                        bool hasTexturePayload = sv.Texture != 0 ||
                            (sv.TextureEx != 0 && sv.TextureExWeight != 0);
                        if (hasTexturePayload && sv.Texture < 64)
                        {
                            byte oldTexture = _map.TexMap[v], oldEx = _map.TexMapEx[v], oldWeight = _map.WTexMapEx[v];
                            byte oldFacture = _map.FactureMap[v], oldFactureWeight = _map.FactureWeight[v];
                            if (sv.Texture != 0)
                            {
                                _map.TexMap[v] = sv.Texture;
                                _map.TexMapEx[v] = sv.Texture;
                                _map.WTexMapEx[v] = 0;
                            }

                            if (sv.Texture != 0 || (sv.TextureEx != 0 && sv.TextureExWeight > 40))
                            {
                                _map.FactureMap[v] = sv.Facture;
                                _map.FactureWeight[v] = sv.FactureWeight;
                            }

                            if (sv.TextureExWeight > _map.WTexMapEx[v] * 3)
                            {
                                if (sv.TextureEx != 0)
                                    _map.TexMapEx[v] = sv.TextureEx;
                                _map.WTexMapEx[v] = sv.TextureExWeight;
                            }
                            if (oldTexture != _map.TexMap[v] || oldEx != _map.TexMapEx[v] ||
                                oldWeight != _map.WTexMapEx[v] || oldFacture != _map.FactureMap[v] ||
                                oldFactureWeight != _map.FactureWeight[v])
                            {
                                C2SmpMarkTextureVertexChanged(vx, vy);
                                textureChanged++;
                            }
                        }

                        minVertexX = Math.Min(minVertexX, vx);
                        minVertexY = Math.Min(minVertexY, vy);
                        maxVertexX = Math.Max(maxVertexX, vx);
                        maxVertexY = Math.Max(maxVertexY, vy);
                        pieceChanged++;
                    }

                    if (pieceChanged > 0)
                    {
                        _map.HasTilesChunk = true;
                        _map.HasTilesExChunk = true;
                        _map.HasFactureMapChunk = true;
                    }

                    // RM_GetObjVector is applied immediately after RM_LoadNotObj.
                    if (piece.HasObjectVector)
                    {
                        realX = ((realX >> 10) << 10) + (piece.ObjectX << 4);
                        realY = ((realY >> 10) << 10) + (piece.ObjectY << 4);
                    }
                }
            }

            // NewMon.cpp skips CreatePlaneUnderBuilding only for Anyway/editor
            // placement. Normal peasant construction uses BUILDBAR for all buildings.
            bool editorInstant = C2BuildingPlacementPreviewV27
                .C2IsEditorInstantPlacementSourceV378LikeOriginal(
                    EditorTestModeLikeOriginal, source);
            int planeChanged = 0;
            int planeMinX = int.MaxValue;
            int planeMinY = int.MaxValue;
            int planeMaxX = int.MinValue;
            int planeMaxY = int.MinValue;
            string planeAudit = editorInstant ? "skipped_editor_anyway" : "buildbar=none";
            if (!editorInstant && md.HasBuildBar)
            {
                planeChanged = C2ApplyBuildingPlaneV379LikeOriginal(
                    md, realX, realY,
                    out planeMinX, out planeMinY, out planeMaxX, out planeMaxY,
                    out planeAudit);
            }

            string piecePatchAudit = "no_piece_vertices";
            bool piecePatchOk = pieceChanged <= 0 || C2SmpRebuildSurfacePatchV1LikeOriginal(
                minVertexX,
                minVertexY,
                maxVertexX,
                maxVertexY,
                textureChanged > 0,
                out piecePatchAudit);
            string planePatchAudit = "no_plane_vertices";
            bool planePatchOk = planeChanged <= 0 || C2SmpRebuildSurfacePatchV1LikeOriginal(
                planeMinX,
                planeMinY,
                planeMaxX,
                planeMaxY,
                false,
                out planePatchAudit);
            bool patchOk = piecePatchOk && planePatchOk;

            audit = "piece='" + (piece != null ? piece.Path : (md.PieceName ?? string.Empty)) + "'" +
                    " parse=[" + parseAudit + "]" +
                    " vertices=" + (piece != null ? piece.Vertices.Count.ToString(CultureInfo.InvariantCulture) : "0") +
                    " pieceChanged=" + pieceChanged.ToString(CultureInfo.InvariantCulture) +
                    " textureChanged=" + textureChanged.ToString(CultureInfo.InvariantCulture) +
                    " planeChanged=" + planeChanged.ToString(CultureInfo.InvariantCulture) +
                    " plane=[" + planeAudit + "]" +
                    " originV=" + vertexOriginX.ToString(CultureInfo.InvariantCulture) + "/" + vertexOriginY.ToString(CultureInfo.InvariantCulture) +
                    " objectVector=" + (piece != null && piece.HasObjectVector ? piece.ObjectX.ToString(CultureInfo.InvariantCulture) + "/" + piece.ObjectY.ToString(CultureInfo.InvariantCulture) : "none") +
                    " real=" + realX.ToString(CultureInfo.InvariantCulture) + "/" + realY.ToString(CultureInfo.InvariantCulture) +
                    " sprites=" + (piece != null ? piece.Sprites.Count.ToString(CultureInfo.InvariantCulture) : "0") +
                    " legacyNRG1Bytes=" + (piece != null ? piece.GroundPayloadBytes.ToString(CultureInfo.InvariantCulture) : "0") +
                    " lockBytes=" + (piece != null ? piece.LockPayloadBytes.ToString(CultureInfo.InvariantCulture) : "0") +
                    " patchOk=" + patchOk.ToString() +
                    " piecePatch=[" + piecePatchAudit + "]" +
                    " planePatch=[" + planePatchAudit + "]" +
                    " blocks=[" + (piece != null ? piece.BlocksAudit : string.Empty) + "]" +
                    " source='" + (source ?? string.Empty) + "'";
            Debug.Log("[C2:SMP SURFACE V1] " + audit);
            return pieceOk && patchOk;
        }

        private int C2ApplyBuildingPlaneV379LikeOriginal(
            C2BuildingMdInfoLikeOriginal md,
            int realX,
            int realY,
            out int minChangedX,
            out int minChangedY,
            out int maxChangedX,
            out int maxChangedY,
            out string audit)
        {
            minChangedX = int.MaxValue;
            minChangedY = int.MaxValue;
            maxChangedX = int.MinValue;
            maxChangedY = int.MinValue;
            audit = "not_started";
            if (md == null || !md.HasBuildBar || _map == null || _map.Heights == null)
            {
                audit = "missing_buildbar_or_map";
                return 0;
            }

            int[] curve = C2BuildPlaneCurveV379LikeOriginal(
                realX >> 4, realY >> 4,
                md.PicDx, md.PicDy,
                md.BuildBarX0, md.BuildBarY0,
                md.BuildBarX1, md.BuildBarY1);
            int minX = Math.Min(Math.Min(curve[0], curve[2]), Math.Min(curve[4], curve[6]));
            int minY = Math.Min(Math.Min(curve[1], curve[3]), Math.Min(curve[5], curve[7]));
            int maxX = Math.Max(Math.Max(curve[0], curve[2]), Math.Max(curve[4], curve[6]));
            int maxY = Math.Max(Math.Max(curve[1], curve[3]), Math.Max(curve[5], curve[7]));

            // TriUnit=16 in the Cossacks II branch. GetTriX/GetTriY therefore
            // operate on a 32-pixel staggered vertex grid.
            int minVertexX = Math.Max(0, minX / 32 - 5);
            int minVertexY = Math.Max(0, minY / 32 - 5);
            int maxVertexX = Math.Min(_map.VertInLine - 1, maxX / 32 + 5);
            int maxVertexY = Math.Min(_map.MaxTH - 1, maxY / 32 + 5);
            if (maxVertexX < minVertexX || maxVertexY < minVertexY)
            {
                audit = "empty_bounds";
                return 0;
            }

            int heightSum = 0;
            int insideCount = 0;
            for (int vy = minVertexY; vy <= maxVertexY; vy++)
            for (int vx = minVertexX; vx <= maxVertexX; vx++)
            {
                int index = vx + vy * _map.VertInLine;
                int x = vx * 32;
                int y = vy * 32 - ((vx & 1) != 0 ? 16 : 0);
                if (C2BuildPlaneCurveDistanceV379LikeOriginal(x, y, curve) < 0)
                {
                    heightSum += _map.Heights[index];
                    insideCount++;
                }
            }

            if (insideCount == 0)
            {
                audit = "inside=0";
                return 0;
            }

            int averageHeight = heightSum / insideCount;
            int changed = 0;
            for (int vy = minVertexY; vy <= maxVertexY; vy++)
            for (int vx = minVertexX; vx <= maxVertexX; vx++)
            {
                int index = vx + vy * _map.VertInLine;
                int current = _map.Heights[index];
                if (current <= 50) continue;
                int x = vx * 32;
                int y = vy * 32 - ((vx & 1) != 0 ? 16 : 0);
                int distance = C2BuildPlaneCurveDistanceV379LikeOriginal(x, y, curve);
                int next = current;
                if (distance < 0)
                    next = averageHeight;
                else if (distance < 64)
                    next = (current * distance + averageHeight * (64 - distance)) / 64;
                if (next == current) continue;

                _map.Heights[index] = unchecked((short)next);
                minChangedX = Math.Min(minChangedX, vx);
                minChangedY = Math.Min(minChangedY, vy);
                maxChangedX = Math.Max(maxChangedX, vx);
                maxChangedY = Math.Max(maxChangedY, vy);
                changed++;
            }

            audit = "curve=(" + curve[0].ToString(CultureInfo.InvariantCulture) + "," + curve[1].ToString(CultureInfo.InvariantCulture) + ")" +
                    "-(" + curve[2].ToString(CultureInfo.InvariantCulture) + "," + curve[3].ToString(CultureInfo.InvariantCulture) + ")" +
                    "-(" + curve[4].ToString(CultureInfo.InvariantCulture) + "," + curve[5].ToString(CultureInfo.InvariantCulture) + ")" +
                    "-(" + curve[6].ToString(CultureInfo.InvariantCulture) + "," + curve[7].ToString(CultureInfo.InvariantCulture) + ")" +
                    " inside=" + insideCount.ToString(CultureInfo.InvariantCulture) +
                    " avg=" + averageHeight.ToString(CultureInfo.InvariantCulture) +
                    " changed=" + changed.ToString(CultureInfo.InvariantCulture);
            return changed;
        }

        internal static int[] C2BuildPlaneCurveV379LikeOriginal(
            int centerX,
            int centerY,
            int picDx,
            int picDy,
            int buildBarX0,
            int buildBarY0,
            int buildBarX1,
            int buildBarY1)
        {
            // NewMon.cpp::BUILDBAR and CreatePlaneUnderBuilding, including the
            // original doubled Y convention.
            int x0 = centerX + picDx + (buildBarX0 << 4);
            int y0 = centerY + ((picDy + (buildBarY0 << 3)) << 1);
            int x1 = centerX + picDx + (buildBarX1 << 4);
            int y1 = centerY + ((picDy + (buildBarY1 << 3)) << 1);
            int d = (y1 - y0 + x1 - x0) >> 1;
            return new[]
            {
                x0 - 40, y0,
                x0 + d, y0 + d + 40,
                x1 + 40, y1,
                x1 - d, y1 - d - 40
            };
        }

        internal static int C2BuildPlaneCurveDistanceV379LikeOriginal(int x, int y, int[] curve)
        {
            if (curve == null || curve.Length < 8) return 100000;
            int crossings = 0;
            int nearest = 100000;
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) & 3;
                int x0 = curve[i * 2];
                int y0 = curve[i * 2 + 1];
                int x1 = curve[next * 2];
                int y1 = curve[next * 2 + 1];
                if (x1 < x0)
                {
                    int swap = x1; x1 = x0; x0 = swap;
                    swap = y1; y1 = y0; y0 = swap;
                }

                int dx = x1 - x0;
                int dy = y1 - y0;
                int lengthSquared = dx * dx + dy * dy;
                if (lengthSquared != 0)
                {
                    int proportion = (((x - x0) * dx + (y - y0) * dy) << 8) / lengthSquared;
                    if (proportion >= 0 && proportion <= 256)
                    {
                        int length = (int)Math.Sqrt(lengthSquared);
                        if (length > 0)
                            nearest = Math.Min(nearest, Math.Abs(dy * (x - x0) - dx * (y - y0)) / length);
                    }
                }

                int ex = x - x0;
                int ey = y - y0;
                nearest = Math.Min(nearest, (int)Math.Sqrt(ex * ex + ey * ey));
                if (x0 <= x && x1 > x && (y0 > y || y1 > y))
                {
                    int crossingY = y0 + (y1 - y0) * (x - x0) / (x1 - x0);
                    if (crossingY > y) crossings++;
                }
            }
            return (crossings & 1) != 0 ? -nearest : nearest;
        }

        private bool C2SmpTryGetPieceV1LikeOriginal(
            string piecePath,
            out C2SmpPieceV1LikeOriginal piece,
            out string audit)
        {
            piece = null;
            audit = string.Empty;
            string path = (piecePath ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrEmpty(path) || _bootstrap == null || _bootstrap.Fs == null)
            {
                audit = "missing_path_or_fs";
                return false;
            }

            if (_c2SmpPieceCacheV1LikeOriginal.TryGetValue(path, out piece) && piece != null)
            {
                audit = "cache_hit";
                return true;
            }
            if (!_bootstrap.Fs.Exists(path))
            {
                audit = "not_found:'" + path + "'";
                return false;
            }

            byte[] data = _bootstrap.Fs.ReadAllBytes(path);
            if (data == null || data.Length < 8 || Encoding.ASCII.GetString(data, 0, 4) != "SAMP")
            {
                audit = "bad_SAMP bytes=" + (data != null ? data.Length.ToString(CultureInfo.InvariantCulture) : "0");
                return false;
            }

            var parsed = new C2SmpPieceV1LikeOriginal { Path = path };
            var blocks = new List<string>();
            try
            {
                using (var ms = new MemoryStream(data, false))
                using (var br = new BinaryReader(ms))
                {
                    br.ReadBytes(4);
                    while (ms.Position + 4 <= ms.Length)
                    {
                        int rawTag = br.ReadInt32();
                        if (rawTag == -1)
                            break;
                        if (ms.Position + 4 > ms.Length)
                            throw new InvalidDataException("truncated block size");

                        string tag = C2SmpTagFromIntV1LikeOriginal(rawTag);
                        int size = br.ReadInt32();
                        int payload = size - 8;
                        long payloadStart = ms.Position;
                        long payloadEnd = payloadStart + payload;
                        if (payload < 0 || payloadEnd > ms.Length)
                            throw new InvalidDataException("bad block '" + tag + "' size=" + size.ToString(CultureInfo.InvariantCulture));
                        blocks.Add(tag + ":" + size.ToString(CultureInfo.InvariantCulture));

                        if (tag == "VER3")
                            C2SmpParseVerticesV1LikeOriginal(br, payloadEnd, parsed);
                        else if (tag == "OBJS")
                            C2SmpParseObjectVectorV1LikeOriginal(br, payloadEnd, parsed);
                        else if (tag == "SPRT")
                            C2SmpParseSpritesV1LikeOriginal(br, payloadEnd, parsed);
                        else if (tag == "NRG1")
                            parsed.GroundPayloadBytes += payload;
                        else if (tag == "LOCK")
                            parsed.LockPayloadBytes += payload;

                        ms.Position = payloadEnd;
                    }
                }
            }
            catch (Exception ex)
            {
                audit = ex.GetType().Name + ":" + ex.Message;
                return false;
            }

            parsed.BlocksAudit = string.Join(",", blocks.ToArray());
            _c2SmpPieceCacheV1LikeOriginal[path] = parsed;
            piece = parsed;
            audit = "parsed bytes=" + data.Length.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static void C2SmpParseVerticesV1LikeOriginal(BinaryReader br, long payloadEnd, C2SmpPieceV1LikeOriginal piece)
        {
            if (br.BaseStream.Position + 4 > payloadEnd)
                return;
            int count = Math.Max(0, br.ReadInt32());
            int possible = (int)Math.Max(0L, (payloadEnd - br.BaseStream.Position) / 14L);
            count = Math.Min(count, possible);
            for (int i = 0; i < count; i++)
            {
                piece.Vertices.Add(new C2SmpVertexV1LikeOriginal
                {
                    Dx = br.ReadInt16(),
                    Dy = br.ReadInt16(),
                    Texture = br.ReadByte(),
                    Facture = br.ReadByte(),
                    FactureWeight = br.ReadByte(),
                    TextureEx = br.ReadByte(),
                    TextureExWeight = br.ReadByte(),
                    Sector1 = br.ReadByte(),
                    Sector2 = br.ReadByte(),
                    Sector3 = br.ReadByte(),
                    Height = br.ReadInt16()
                });
            }
        }

        private static void C2SmpParseObjectVectorV1LikeOriginal(BinaryReader br, long payloadEnd, C2SmpPieceV1LikeOriginal piece)
        {
            if (br.BaseStream.Position + 4 > payloadEnd)
                return;
            int count = Math.Max(0, br.ReadInt32());
            if (count <= 0 || br.BaseStream.Position + 40 > payloadEnd)
                return;
            piece.ObjectX = br.ReadInt32();
            piece.ObjectY = br.ReadInt32();
            piece.HasObjectVector = true;
            br.BaseStream.Position += 32; // nation byte + char Name[31]
        }

        private static void C2SmpParseSpritesV1LikeOriginal(BinaryReader br, long payloadEnd, C2SmpPieceV1LikeOriginal piece)
        {
            if (br.BaseStream.Position + 4 > payloadEnd)
                return;
            int count = Math.Max(0, br.ReadInt32());
            int possible = (int)Math.Max(0L, (payloadEnd - br.BaseStream.Position) / 12L);
            count = Math.Min(count, possible);
            for (int i = 0; i < count; i++)
            {
                ushort sign = br.ReadUInt16();
                piece.Sprites.Add(new C2SmpSpriteV1LikeOriginal
                {
                    Sign = new string(new[] { (char)(sign & 255), (char)((sign >> 8) & 255) }),
                    X = br.ReadInt32(),
                    Y = br.ReadInt32(),
                    Index = br.ReadUInt16()
                });
            }
        }

        private static string C2SmpTagFromIntV1LikeOriginal(int raw)
        {
            byte[] bytes = BitConverter.GetBytes(raw);
            return Encoding.ASCII.GetString(bytes, 0, 4);
        }

        private bool C2SmpRebuildSurfacePatchV1LikeOriginal(
            int minVertexX,
            int minVertexY,
            int maxVertexX,
            int maxVertexY,
            bool rebuildTexture,
            out string audit)
        {
            audit = "not_started";
            if (_map == null || _terrainRoot == null || !_hasLastBuiltTerrainKernel)
            {
                audit = "missing_map_terrain_or_kernel";
                return false;
            }

            OriginalTerrainKernelConfig kernel = _lastBuiltTerrainKernel;
            int minCellX = Mathf.Clamp(minVertexX - 1, kernel.MinCellX, kernel.MaxCellXExclusive);
            int minCellY = Mathf.Clamp(minVertexY - 1, kernel.MinCellY, kernel.MaxCellYExclusive);
            int maxCellXExclusive = Mathf.Clamp(maxVertexX + 1, kernel.MinCellX, kernel.MaxCellXExclusive);
            int maxCellYExclusive = Mathf.Clamp(maxVertexY + 1, kernel.MinCellY, kernel.MaxCellYExclusive);
            if (maxCellXExclusive <= minCellX || maxCellYExclusive <= minCellY)
            {
                audit = "empty_region";
                return false;
            }

            // Scape3D uses 64x64-cell terrain cache entries.  Rebuild those same
            // entries instead of adding one Unity renderer per building stamp.
            int minChunkX = Mathf.Max(0, Mathf.FloorToInt((minCellX - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int minChunkY = Mathf.Max(0, Mathf.FloorToInt((minCellY - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int maxChunkX = Mathf.Max(minChunkX, Mathf.FloorToInt((maxCellXExclusive - 1 - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int maxChunkY = Mathf.Max(minChunkY, Mathf.FloorToInt((maxCellYExclusive - 1 - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int queued = 0;
            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                {
                    _smpDirtyChunks[new Vector2Int(chunkX, chunkY)] = ++_smpRevision;
                    if (rebuildTexture)
                        _smpTextureDirtyChunks.Add(new Vector2Int(chunkX, chunkY));
                    queued++;
                }
            }
            if (_smpBakeCoroutine == null) _smpBakeCoroutine = StartCoroutine(C2SmpProcessDirtyChunks());
            audit = "changedCells=" + minCellX.ToString(CultureInfo.InvariantCulture) + "/" + minCellY.ToString(CultureInfo.InvariantCulture) +
                    ".." + maxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + maxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                    " terrainChunksQueued=" + queued +
                    " mode=" + (rebuildTexture ? "texture_and_height" : "height_only_keep_texture") +
                    " gameplayHeightAppliedImmediately=true extraRenderers=0";
            return queued > 0;
        }

        private static Transform C2SmpFindTerrainChunkTransformV1LikeOriginal(Transform root, string chunkName)
        {
            if (root == null || string.IsNullOrEmpty(chunkName))
                return null;
            if (string.Equals(root.name, chunkName, StringComparison.Ordinal))
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = C2SmpFindTerrainChunkTransformV1LikeOriginal(root.GetChild(i), chunkName);
                if (found != null) return found;
            }
            return null;
        }

        private static void C2SmpDestroySurfacePatchV1LikeOriginal(C2SmpSurfacePatchV1LikeOriginal patch)
        {
            if (patch == null)
                return;
            if (patch.Mesh != null) SafeDestroy(patch.Mesh);
            if (patch.Texture != null) SafeDestroy(patch.Texture);
        }

        private void C2SmpClearSurfacePatchesV1LikeOriginal()
        {
            C2SmpCancelPendingBake();
            for (int i = _c2SmpSurfacePatchesV1LikeOriginal.Count - 1; i >= 0; i--)
                C2SmpDestroySurfacePatchV1LikeOriginal(_c2SmpSurfacePatchesV1LikeOriginal[i]);
            _c2SmpSurfacePatchesV1LikeOriginal.Clear();
        }

        private void OnDestroy()
        {
            // Dynamic terrain chunk meshes/textures are not assets and are not owned by
            // the scene hierarchy. Release them explicitly when the map mode is closed.
            C2SmpClearSurfacePatchesV1LikeOriginal();
        }
    }
}
