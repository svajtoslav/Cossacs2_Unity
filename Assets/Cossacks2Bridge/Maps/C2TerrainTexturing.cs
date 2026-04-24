using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const int GroundAtlasTileCountXLikeOriginal = 8;
        private const int GroundAtlasTileCountYLikeOriginal = 8;
        private const int TriScaleLikeOriginal = 512; // #define TRISCALE 512
        private const int VvvLikeOriginal = 28; // #define VVV 28
        private const int SurfaceBaseRenderQueueLikeAdapted = 2000;
        private const int SurfaceOverlayRenderQueueLikeAdapted = 2001;
        private const int FactureOverlayRenderQueueLikeAdapted = 3000;
        private const int FactureOverlayStripeSortStepLikeAdapted = 512;
        private const int FactureAlphaRefByteLikeOriginal = 4;
        private const int FactureWeakCoverageDeadZoneLikeAdapted = FactureAlphaRefByteLikeOriginal;
        private const int FactureSingleVertexCoverageFloorLikeAdapted = 16;
        private const float FactureCoverageSoftStartLikeAdapted = 8.0f / 255.0f;
        private const float GroundAtlasTriScaleLikeOriginal = TriScaleLikeOriginal;
        private const float GroundAtlasTileSpanLikeOriginal = 32.0f / GroundAtlasTriScaleLikeOriginal;
        private const float GroundAtlasHalfSpanLikeOriginal = 16.0f / GroundAtlasTriScaleLikeOriginal;
        private const float CrossingUvScaleLikeOriginal = 1.0f / 256.0f;

        private static readonly Dictionary<string, TerrainTextureResourcesLikeOriginal> s_surfaceTextureCacheLikeOriginal =
            new Dictionary<string, TerrainTextureResourcesLikeOriginal>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, TerrainTextureTablesLikeOriginal> s_surfaceTablesCacheLikeOriginal =
            new Dictionary<string, TerrainTextureTablesLikeOriginal>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, short[]> s_randomTableCacheLikeOriginal =
            new Dictionary<string, short[]>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, FactureMaterialTablesLikeAdapted> s_factureMaterialTablesCacheLikeAdapted =
            new Dictionary<string, FactureMaterialTablesLikeAdapted>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Texture2D> s_factureTextureCacheLikeAdapted =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Material> s_factureMaterialCacheLikeAdapted =
            new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Texture2D> s_factureGeneratedNormalCacheLikeAdapted =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> s_factureMetadataWarningsLikeAdapted =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> s_factureMetadataLoadReportsLikeAdapted =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<int, string> s_factureObservedFallbackPathsLikeAdapted =
            new Dictionary<int, string>
            {
                { 0,  @"Textures\ground\tex1.bmp" },
                { 1,  @"Textures\ground\grass3.bmp" },
                { 2,  @"Textures\ground\tex33.bmp" },
                { 3,  @"Textures\ground\tex4.bmp" },
                { 4,  @"Textures\ground\tex54.bmp" },
                { 5,  @"Textures\ground\tex53.bmp" },
                { 7,  @"Textures\ground\tex8.bmp" },
                { 10, @"Textures\ground\tex11.bmp" },
                { 12, @"Textures\ground\tex13.bmp" },
                { 13, @"Textures\ground\tex14.bmp" },
                { 15, @"Textures\ground\tex16.bmp" },
                { 16, @"Textures\ground\tex17.bmp" },
                { 20, @"Textures\ground\tex21.bmp" },
                { 21, @"Textures\ground\tex22.bmp" },
                { 24, @"Textures\ground\tex25.bmp" },
                { 25, @"Textures\ground\tex26.bmp" },
                { 28, @"Textures\ground\tex29.bmp" },
                { 31, @"Textures\ground\tex32.bmp" },
                { 35, @"Textures\ground\tex37.bmp" },
                { 36, @"Textures\ground\tex38.bmp" },
                { 39, @"Textures\ground\tex43.bmp" },
                { 42, @"Textures\ground\tex46.bmp" },
                { 46, @"Textures\ground\tex50.bmp" },
                { 54, @"Textures\ground\tex69.bmp" },
                { 55, @"Textures\ground\tex68.bmp" },
                { 57, @"Textures\ground\tex66.bmp" },
                { 59, @"Textures\ground\tex63.bmp" },
                { 60, @"Textures\ground\tex62.bmp" },
                { 64, @"Textures\ground\tex58.bmp" },
                { 65, @"Textures\ground\tex65.bmp" },
                { 66, @"Textures\ground\tex42.bmp" },
                { 70, @"Textures\ground\tex79.bmp" },
                { 71, @"Textures\ground\grass1.bmp" },
                { 72, @"Textures\ground\grass2.bmp" },
                { 73, @"Textures\ground\tex2.bmp" },
            };

        private enum FactureTextureVariantLikeAdapted
        {
            PlainDiffuse,
            Dot3Diffuse,
            BumpSource
        }

        // Compatibility no-op stubs: older audit-enabled C2BattleTerrainMode.cs files may still call these.
        private static void BeginFactureCoverageAuditLikeAdapted(ParsedMap map, OriginalTerrainKernelConfig kernel, int stripeCount) { }
        private static void EndFactureCoverageAuditLikeAdapted() { }

        private static bool s_loggedTexturingBootstrapLikeOriginal;
        private static C2BattleTerrainMode s_activeTexturingContextLikeOriginal;
        private static string s_randomTableDataRootLikeOriginal = string.Empty;

        private sealed class TerrainTextureResourcesLikeOriginal
        {
            public Texture2D GroundAtlas;
            public Texture2D CrossTex;
            public string GroundAtlasPath = string.Empty;
            public string CrossTexPath = string.Empty;
        }

        private sealed class TerrainTextureTablesLikeOriginal
        {
            public readonly byte[] TexCrossing = new byte[64 * 64];
            public readonly Color32[] TexDiffuse = new Color32[64];
            public readonly ushort[] TexFlags = new ushort[256];
            public readonly byte[] RoadTex = new byte[256];
            public readonly byte[,] ExtTex = new byte[256, 4];
            public readonly byte[] TexMedia = new byte[256];

            public TerrainTextureTablesLikeOriginal()
            {
                for (int i = 0; i < 256; i++)
                {
                    RoadTex[i] = (byte)i;
                    ExtTex[i, 0] = (byte)i;
                    ExtTex[i, 1] = (byte)i;
                    ExtTex[i, 2] = (byte)i;
                    ExtTex[i, 3] = (byte)i;
                }
            }
        }


private sealed class FactureMaterialTablesLikeAdapted
{
    public readonly byte[] Usage = new byte[256];
    public readonly bool[] UseBump = new bool[256];
    public readonly float[] UScale = new float[256];
    public readonly float[] VScale = new float[256];
    public readonly float[] UShift = new float[256];
    public readonly float[] VShift = new float[256];
    public readonly string[] DiffuseTexturePath = new string[256];
    public readonly string[] BumpTexturePath = new string[256];
    public readonly float[] BumpDegree = new float[256];
    public readonly float[] BumpContrast = new float[256];
    public readonly float[] BumpBrightness = new float[256];
    public bool LoadedFromXml;
    public string SourceKind = "uninitialized";
    public string SourceXmlPath = string.Empty;
    public string SourceTexturesXmlPath = string.Empty;
    public string SourceDatPath = string.Empty;
    public int ActiveEntryCount;

    public FactureMaterialTablesLikeAdapted()
    {
        for (int i = 0; i < 256; i++)
        {
            Usage[i] = 0;
            UseBump[i] = false;
            UScale[i] = 1.0f;
            VScale[i] = 1.0f;
            UShift[i] = 0.0f;
            VShift[i] = 0.0f;
            DiffuseTexturePath[i] = string.Empty;
            BumpTexturePath[i] = string.Empty;
            BumpDegree[i] = 1.0f;
            BumpContrast[i] = 0.6f;
            BumpBrightness[i] = 1.0f;
        }
    }
}

        private const ushort TexAlwaysLandLockLikeOriginal = 1;
        private const ushort TexAlwaysLandUnlockLikeOriginal = 2;
        private const ushort TexAlwaysWaterUnlockLikeOriginal = 4;
        private const ushort TexPlainLikeOriginal = 8;
        private const ushort TexHardLikeOriginal = 16;
        private const ushort TexHardLightLikeOriginal = 32;
        private const ushort TexNoLightLikeOriginal = 64;
        private const ushort TexNormalPutLikeOriginal = 128;
        private const ushort TexGrassLikeOriginal = 256;

        private enum BaseSurfaceTriangleKindLikeOriginal
        {
            Unknown = 0,
            OddLeft = 1,
            OddRight = 2,
            EvenUpper = 3,
            EvenLower = 4,
        }

        private enum BaseSurfaceTriangleCopyRoleLikeAdapted
        {
            Primary = 0,
            Average = 1,
            Maximum = 2,
        }


private enum FactureUsageLikeOriginal
{
    Unknown = -1,
    Planar = 0,
    Vertical = 1,
    Edges = 2,
}

private enum FactureOrientationLikeAdapted
{
    None = 0,
    DominantY = 1,
    DominantX = 2,
    NegativeX = 3,
    PositiveX = 4,
}

private struct FactureVertexInfluenceLikeAdapted
{
    public int VertexIndex;
    public int RawFactureId;
    public int RenderFactureId;
    public int Weight;
    public FactureUsageLikeOriginal Usage;
    public FactureOrientationLikeAdapted Orientation;
    public int VariantIndex;
    public bool HasBump;
}

private struct FactureTriangleSourceDescriptorLikeAdapted
{
    public BaseSurfaceTriangleKindLikeOriginal SourceKind;
    public int SourceCellX;
    public int SourceCellY;
    public int VertexA;
    public int VertexB;
    public int VertexC;
    public FactureVertexInfluenceLikeAdapted InfluenceA;
    public FactureVertexInfluenceLikeAdapted InfluenceB;
    public FactureVertexInfluenceLikeAdapted InfluenceC;
}

private struct FactureTriangleCopyDescriptorLikeAdapted
{
    public BaseSurfaceTriangleKindLikeOriginal SourceKind;
    public int SourceCellX;
    public int SourceCellY;
    public int VertexA;
    public int VertexB;
    public int VertexC;
    public int SourceFactureA;
    public int SourceFactureB;
    public int SourceFactureC;
    public int CopyFactureId;
    public FactureUsageLikeOriginal Usage;
    public FactureOrientationLikeAdapted Orientation;
    public int VariantIndex;
    public int WeightA;
    public int WeightB;
    public int WeightC;
    public Vector2 UvA;
    public Vector2 UvB;
    public Vector2 UvC;
    public int BucketTextureId;
    public bool HasBump;
}

private sealed class FactureBucketMeshDataLikeAdapted
{
    public readonly List<Vector3> Vertices;
    public readonly List<Color32> Colors;
    public readonly List<int> Triangles;
    public readonly List<Vector2> Uv0;
    public Bounds Bounds;
    public bool HasBounds;
    public bool HasContent;
    public bool HasBumpContent;

    public FactureBucketMeshDataLikeAdapted(int estimatedTriangles)
    {
        int vertexCapacity = Mathf.Max(estimatedTriangles * 3, 6);
        Vertices = new List<Vector3>(vertexCapacity);
        Colors = new List<Color32>(vertexCapacity);
        Triangles = new List<int>(vertexCapacity);
        Uv0 = new List<Vector2>(vertexCapacity);
        Bounds = new Bounds(Vector3.zero, Vector3.zero);
        HasBounds = false;
        HasContent = false;
    }
}

private struct TriangleWinnerRecordLikeAdapted
{
    public int CellX;
    public int CellY;
    public bool EmitBase;
    public int WinnerRawFactureId;
    public int RenderFactureId;
    public int BucketTextureId;
    public bool HasBump;
    public CellVertexPayloadLikeOriginal A;
    public CellVertexPayloadLikeOriginal B;
    public CellVertexPayloadLikeOriginal C;
    public Vector2 UvA;
    public Vector2 UvB;
    public Vector2 UvC;
}

private struct EdgeKeyLikeAdapted : IEquatable<EdgeKeyLikeAdapted>
{
    public int A;
    public int B;

    public EdgeKeyLikeAdapted(int v0, int v1)
    {
        if (v0 <= v1)
        {
            A = v0;
            B = v1;
        }
        else
        {
            A = v1;
            B = v0;
        }
    }

    public bool Equals(EdgeKeyLikeAdapted other)
    {
        return A == other.A && B == other.B;
    }

    public override bool Equals(object obj)
    {
        return obj is EdgeKeyLikeAdapted other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (A * 397) ^ B;
        }
    }
}


        private struct ExpandedTriangleCopyLikeOriginal
        {
            public byte Tile;
            public int Vr;
            public bool IsLeft;
            public bool OpaqueBase;
            public bool PlainMode;
            public int SeedVertexU;
            public int SeedSetU;
            public int SeedVertexV;
            public int SeedSetV;
            public float AlphaA;
            public float AlphaB;
            public float AlphaC;
        }

        private struct BaseSurfaceTriangleDescriptorLikeAdapted
        {
            public bool IsBaseStage;
            public BaseSurfaceTriangleKindLikeOriginal Kind;
            public BaseSurfaceTriangleCopyRoleLikeAdapted Role;
            public int VertexA;
            public int VertexB;
            public int VertexC;
            public int BaseTileA;
            public int BaseTileB;
            public int BaseTileC;
            public int ExTileA;
            public int ExTileB;
            public int ExTileC;
            public int WeightA;
            public int WeightB;
            public int WeightC;
            public byte Tile;
            public int ResolvedTile;
            public int Vr;
            public bool IsLeft;
            public bool OpaqueBase;
            public bool PlainMode;
            public int SeedVertexU;
            public int SeedSetU;
            public int SeedVertexV;
            public int SeedSetV;
            public float AlphaA;
            public float AlphaB;
            public float AlphaC;
        }

        private struct CellSurfaceStageLikeOriginal
        {
            public bool IsBaseStage;
            public bool PlainMode;
            public int T0;
            public int T1;
            public int T2;
            public int T3;
            public int W0;
            public int W1;
            public int W2;
            public int W3;
        }


        private sealed partial class ParsedMap
        {
            public byte[] TexMap = Array.Empty<byte>();
            public byte[] TexMapEx = Array.Empty<byte>();
            public byte[] WTexMapEx = Array.Empty<byte>();
            public byte[] FactureMap = Array.Empty<byte>();
            public byte[] FactureWeight = Array.Empty<byte>();
            public bool HasTilesChunk;
            public bool HasTilesExChunk;
            public bool HasFactureMapChunk;
        }


        private void LogTerrainTexturingBootstrapLikeOriginal(ParsedMap map)
        {
            s_activeTexturingContextLikeOriginal = this;
            s_randomTableDataRootLikeOriginal = (_bootstrap != null && _bootstrap.Fs != null && !string.IsNullOrWhiteSpace(_bootstrap.Fs.DataRoot))
                ? _bootstrap.Fs.DataRoot
                : string.Empty;

            if (s_loggedTexturingBootstrapLikeOriginal)
                return;

            s_loggedTexturingBootstrapLikeOriginal = true;
        }

        private static void LogSurfaceTexturingChunkLikeOriginal(string tag, int payloadLen, int count)
        {
        }


        private static bool TagEqualsTexturingLikeOriginal(string actualTag, params string[] expectedVariants)
        {
            if (string.IsNullOrEmpty(actualTag) || expectedVariants == null)
                return false;

            for (int i = 0; i < expectedVariants.Length; i++)
            {
                string expected = expectedVariants[i];
                if (!string.IsNullOrEmpty(expected) && string.Equals(actualTag, expected, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool TryParseSurfaceTexturingChunkLikeOriginal(string tag, BinaryReader br, ParsedMap map, int payloadLen)
        {
            if (TagEqualsTexturingLikeOriginal(tag, "ELIT", "TILE"))
            {
                LoadTilesLikeOriginal(br, map, payloadLen);
                return true;
            }

            if (TagEqualsTexturingLikeOriginal(tag, "2LIT", "TIL2"))
            {
                LoadTilesExLikeOriginal(br, map, payloadLen);
                return true;
            }

            if (TagEqualsTexturingLikeOriginal(tag, "FMAP", "PAMF"))
            {
                LoadFacturesLikeOriginal(br, map, payloadLen);
                return true;
            }

            return false;
        }

        private static void LoadTilesLikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            EnsureTileMapsLikeOriginal(map);
            int count = Mathf.Min(GetMaxPointIndexLikeOriginal(map), payloadLen);
            if (count <= 0)
                return;

            br.Read(map.TexMap, 0, count);
            map.HasTilesChunk = true;
            LogSurfaceTexturingChunkLikeOriginal("ELIT/TILE", payloadLen, count);
        }

        private static void LoadTilesExLikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            EnsureTileMapsLikeOriginal(map);
            int count = Mathf.Min(GetMaxPointIndexLikeOriginal(map), payloadLen / 2);
            if (count <= 0)
                return;

            br.Read(map.TexMapEx, 0, count);
            br.Read(map.WTexMapEx, 0, count);
            map.HasTilesExChunk = true;
            LogSurfaceTexturingChunkLikeOriginal("2LIT/TIL2", payloadLen, count);
        }

        private static void LoadFacturesLikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            EnsureFactureMapsLikeOriginal(map);
            int count = Mathf.Min(GetMaxPointIndexLikeOriginal(map), payloadLen / 2);
            if (count <= 0)
                return;

            br.Read(map.FactureMap, 0, count);
            br.Read(map.FactureWeight, 0, count);
            map.HasFactureMapChunk = true;
            LogSurfaceTexturingChunkLikeOriginal("FMAP/PAMF", payloadLen, count);
        }

        private static void EnsureTileMapsLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return;

            int expected = GetMaxPointIndexLikeOriginal(map);
            if (expected <= 0)
                return;

            if (map.TexMap == null || map.TexMap.Length < expected)
                map.TexMap = new byte[expected];
            if (map.TexMapEx == null || map.TexMapEx.Length < expected)
                map.TexMapEx = new byte[expected];
            if (map.WTexMapEx == null || map.WTexMapEx.Length < expected)
                map.WTexMapEx = new byte[expected];
        }

        private static void EnsureFactureMapsLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return;

            int expected = GetMaxPointIndexLikeOriginal(map);
            if (expected <= 0)
                return;

            if (map.FactureMap == null || map.FactureMap.Length < expected)
                map.FactureMap = new byte[expected];
            if (map.FactureWeight == null || map.FactureWeight.Length < expected)
                map.FactureWeight = new byte[expected];
        }

        private static int GetFactureIdLikeOriginal(ParsedMap map, int vertexIndex)
        {
            if (map == null || !map.HasFactureMapChunk || map.FactureMap == null || vertexIndex < 0 || vertexIndex >= map.FactureMap.Length)
                return 0;
            return map.FactureMap[vertexIndex];
        }

        private static int GetFactureWeightLikeOriginal(ParsedMap map, int vertexIndex)
        {
            if (map == null || !map.HasFactureMapChunk || map.FactureWeight == null || vertexIndex < 0 || vertexIndex >= map.FactureWeight.Length)
                return 0;
            return map.FactureWeight[vertexIndex];
        }


private static bool HasFactureLayerDataLikeOriginal(ParsedMap map)
{
    return map != null
        && map.HasFactureMapChunk
        && map.FactureMap != null
        && map.FactureWeight != null
        && map.FactureMap.Length > 0
        && map.FactureWeight.Length > 0;
}

private static int GetFactureWeightByIdxLikeOriginal(ParsedMap map, int vertexIndex)
{
    if (!HasFactureLayerDataLikeOriginal(map))
        return 0;
    if (vertexIndex <= map.VertInLine || vertexIndex < 0 || vertexIndex >= map.FactureWeight.Length)
        return 0;
    if (map.Heights == null || map.Heights.Length == 0 || vertexIndex >= map.Heights.Length || (vertexIndex - map.VertInLine) < 0 || (vertexIndex - map.VertInLine) >= map.Heights.Length)
        return 0;

    if ((map.Heights[vertexIndex] - map.Heights[vertexIndex - map.VertInLine]) < 16)
        return map.FactureWeight[vertexIndex];
    return 0;
}

private static Vector3 GetFactureVertexNormalLikeOriginal(ParsedMap map, int vertexIndex)
{
    if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 0 || map.MaxTH <= 0 || vertexIndex < 0 || vertexIndex >= map.Heights.Length)
        return Vector3.up;

    int vx = vertexIndex % map.VertInLine;
    int vy = vertexIndex / map.VertInLine;
    int h = map.Heights[vertexIndex];

    int SampleHeight(int x, int y)
    {
        x = Mathf.Clamp(x, 0, map.VertInLine - 1);
        y = Mathf.Clamp(y, 0, map.MaxTH - 1);
        int idx = y * map.VertInLine + x;
        if (idx < 0 || idx >= map.Heights.Length)
            return h;
        return map.Heights[idx];
    }

    int hlu, hld, hru, hrd, hu, hd;
    if ((vx & 1) != 0)
    {
        if (vy > 0)
        {
            hu = SampleHeight(vx, vy - 1);
            hlu = SampleHeight(vx - 1, vy - 1);
            hru = SampleHeight(vx + 1, vy - 1);
        }
        else
        {
            hu = h;
            hlu = h;
            hru = h;
        }

        hld = SampleHeight(vx - 1, vy);
        hrd = SampleHeight(vx + 1, vy);
        hd = SampleHeight(vx, vy + 1);
    }
    else
    {
        hlu = vx > 0 ? SampleHeight(vx - 1, vy) : h;
        hld = vx > 0 ? SampleHeight(vx - 1, vy + 1) : h;
        hu = vy > 0 ? SampleHeight(vx, vy - 1) : h;
        hru = SampleHeight(vx + 1, vy);
        hrd = SampleHeight(vx + 1, vy + 1);
        hd = SampleHeight(vx, vy + 1);
    }

    float nx = (hlu + hld - hru - hrd) * 0.5f;
    float ny = (hd - hu);
    float nz = 64.0f;
    Vector3 n = new Vector3(nx, ny, nz);
    if (n.sqrMagnitude <= 1e-6f)
        return new Vector3(0.0f, 0.0f, 1.0f);
    return n.normalized;
}

private static FactureMaterialTablesLikeAdapted GetFactureMaterialTablesLikeAdapted()
{
    string dataRoot = (s_activeTexturingContextLikeOriginal != null
            && s_activeTexturingContextLikeOriginal._bootstrap != null
            && s_activeTexturingContextLikeOriginal._bootstrap.Fs != null
            && !string.IsNullOrWhiteSpace(s_activeTexturingContextLikeOriginal._bootstrap.Fs.DataRoot))
        ? s_activeTexturingContextLikeOriginal._bootstrap.Fs.DataRoot
        : string.Empty;

    if (s_factureMaterialTablesCacheLikeAdapted.TryGetValue(dataRoot, out FactureMaterialTablesLikeAdapted cached) && cached != null)
        return cached;

    FactureMaterialTablesLikeAdapted tables = new FactureMaterialTablesLikeAdapted();
    TryLoadFactureMaterialTablesLikeAdapted(dataRoot, tables);
    s_factureMaterialTablesCacheLikeAdapted[dataRoot] = tables;
    return tables;
}

private static void TryLoadFactureMaterialTablesLikeAdapted(string dataRoot, FactureMaterialTablesLikeAdapted tables)
{
    if (tables == null)
        return;

    bool xmlLoaded = false;

    foreach (string candidate in EnumerateFactureMetadataCandidatesLikeAdapted(dataRoot, "FacturesList.xml"))
    {
        if (!File.Exists(candidate))
            continue;

        if (TryLoadFacturesListXmlLikeAdapted(candidate, tables))
        {
            tables.LoadedFromXml = true;
            tables.SourceKind = "xml";
            tables.SourceXmlPath = candidate;
            xmlLoaded = true;

            foreach (string texCandidate in EnumerateFactureMetadataCandidatesLikeAdapted(dataRoot, "Textures.xml"))
            {
                if (!File.Exists(texCandidate))
                    continue;

                tables.SourceTexturesXmlPath = texCandidate;
                break;
            }

            break;
        }
    }

    if (!xmlLoaded)
    {
        InitializeFactureFallbackLikeOriginal(tables);

        foreach (string candidate in EnumerateFactureMetadataCandidatesLikeAdapted(dataRoot, "Factures.dat"))
        {
            if (!File.Exists(candidate))
                continue;

            if (TryLoadFacturesDatLikeAdapted(candidate, tables))
            {
                tables.SourceDatPath = candidate;
                tables.SourceKind = "fallback-dat";
                break;
            }
        }

        int observedOverrides = ApplyObservedFactureFallbackOverridesLikeAdapted(tables);
        if (observedOverrides > 0)
        {
            tables.SourceKind = string.Equals(tables.SourceKind, "fallback-dat", StringComparison.OrdinalIgnoreCase)
                ? "fallback-dat+observed"
                : "fallback-default40+observed";
        }

        if (string.IsNullOrEmpty(tables.SourceKind) || string.Equals(tables.SourceKind, "uninitialized", StringComparison.OrdinalIgnoreCase))
            tables.SourceKind = "fallback-default40";
    }

    LogFactureMaterialTablesSummaryLikeAdapted(dataRoot, tables);
}

private static IEnumerable<string> EnumerateFactureMetadataCandidatesLikeAdapted(string dataRoot, string fileName)
{
    var results = new List<string>(32);
    var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var searchRoots = new List<string>(16);
    var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, dataRoot);

    if (!string.IsNullOrWhiteSpace(dataRoot))
    {
        string current = dataRoot;
        for (int i = 0; i < 6 && !string.IsNullOrWhiteSpace(current); i++)
        {
            AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, current);
            string parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                break;
            current = parent;
        }
    }

    try
    {
        string currentDir = Environment.CurrentDirectory;
        AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, currentDir);

        string parent = currentDir;
        for (int i = 0; i < 4 && !string.IsNullOrWhiteSpace(parent); i++)
        {
            parent = Path.GetDirectoryName(parent);
            if (string.IsNullOrWhiteSpace(parent))
                break;
            AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, parent);
        }
    }
    catch
    {
    }

    try
    {
        string dataPath = Application.dataPath;
        AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, dataPath);
        if (!string.IsNullOrWhiteSpace(dataPath))
        {
            string assetsParent = Path.GetDirectoryName(dataPath);
            AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, assetsParent);
            AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, Path.Combine(dataPath, "Resources"));
            if (!string.IsNullOrWhiteSpace(assetsParent))
                AppendFactureMetadataSearchRootLikeAdapted(searchRoots, seenRoots, Path.Combine(assetsParent, "Assets", "Resources"));
        }
    }
    catch
    {
    }

    for (int i = 0; i < searchRoots.Count; i++)
    {
        string root = searchRoots[i];
        if (string.IsNullOrWhiteSpace(root))
            continue;

        AppendFactureMetadataCandidatePathLikeAdapted(results, seenPaths, Path.Combine(root, fileName));
        AppendFactureMetadataCandidatePathLikeAdapted(results, seenPaths, Path.Combine(root, "Resources", fileName));
        AppendFactureMetadataCandidatePathLikeAdapted(results, seenPaths, Path.Combine(root, "Assets", "Resources", fileName));
        AppendFactureMetadataCandidatePathLikeAdapted(results, seenPaths, Path.Combine(root, "Assets", fileName));
    }

    for (int i = 0; i < searchRoots.Count; i++)
    {
        string root = searchRoots[i];
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            continue;

        try
        {
            foreach (string absolutePath in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                AppendFactureMetadataCandidatePathLikeAdapted(results, seenPaths, absolutePath);
        }
        catch
        {
        }
    }

    return results;
}

private static void AppendFactureMetadataSearchRootLikeAdapted(List<string> roots, HashSet<string> seenRoots, string root)
{
    if (roots == null || seenRoots == null || string.IsNullOrWhiteSpace(root))
        return;

    try
    {
        string full = Path.GetFullPath(root);
        if (seenRoots.Add(full))
            roots.Add(full);
    }
    catch
    {
    }
}

private static void AppendFactureMetadataCandidatePathLikeAdapted(List<string> results, HashSet<string> seenPaths, string candidate)
{
    if (results == null || seenPaths == null || string.IsNullOrWhiteSpace(candidate))
        return;

    try
    {
        string full = Path.GetFullPath(candidate);
        if (seenPaths.Add(full))
            results.Add(full);
    }
    catch
    {
    }
}

private static int ApplyObservedFactureFallbackOverridesLikeAdapted(FactureMaterialTablesLikeAdapted tables)
{
    if (tables == null)
        return 0;

    int applied = 0;
    foreach (KeyValuePair<int, string> kv in s_factureObservedFallbackPathsLikeAdapted)
    {
        int idx = kv.Key;
        if (idx < 0 || idx >= tables.DiffuseTexturePath.Length)
            continue;

        string normalizedPath = NormalizeFactureTexturePathLikeAdapted(kv.Value);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            continue;

        tables.DiffuseTexturePath[idx] = normalizedPath;
        tables.Usage[idx] = (byte)Mathf.Clamp(tables.Usage[idx], 0, 2);
        tables.UScale[idx] = Mathf.Approximately(tables.UScale[idx], 0.0f) ? 1.0f : tables.UScale[idx];
        tables.VScale[idx] = Mathf.Approximately(tables.VScale[idx], 0.0f) ? 1.0f : tables.VScale[idx];
        tables.ActiveEntryCount = Mathf.Max(tables.ActiveEntryCount, idx + 1);
        applied++;
    }

    return applied;
}

private static void LogFactureMaterialTablesSummaryLikeAdapted(string dataRoot, FactureMaterialTablesLikeAdapted tables)
{
    if (tables == null)
        return;

    string reportKey = (dataRoot ?? string.Empty) + "|" + (tables.SourceKind ?? string.Empty) + "|" + (tables.SourceXmlPath ?? string.Empty) + "|" + (tables.SourceDatPath ?? string.Empty);
    if (!s_factureMetadataLoadReportsLikeAdapted.Add(reportKey))
        return;

    var sb = new StringBuilder(2048);
    sb.Append("[C2:FACT] tables source='").Append(tables.SourceKind)
      .Append("' active=").Append(tables.ActiveEntryCount)
      .Append(" xml='").Append(tables.SourceXmlPath)
      .Append("' texturesXml='").Append(tables.SourceTexturesXmlPath)
      .Append("' dat='").Append(tables.SourceDatPath)
      .Append("' dataRoot='").Append(dataRoot ?? string.Empty).Append("'");

    int listed = 0;
    for (int i = 0; i < tables.DiffuseTexturePath.Length; i++)
    {
        string diffuse = tables.DiffuseTexturePath[i];
        if (string.IsNullOrWhiteSpace(diffuse))
            continue;

        if (listed == 0)
            sb.Append("\n");
        sb.Append("  [").Append(i).Append("] usage=").Append(tables.Usage[i]).Append(" diffuse='").Append(diffuse).Append("'");
        if (!string.IsNullOrWhiteSpace(tables.BumpTexturePath[i]))
            sb.Append(" bump='").Append(tables.BumpTexturePath[i]).Append("'");
        sb.Append("\n");
        listed++;
    }

    if (listed == 0)
        sb.Append("\n  <no diffuse entries>");

    UnityEngine.Debug.Log(sb.ToString());
}

private static void InitializeFactureFallbackLikeOriginal(FactureMaterialTablesLikeAdapted tables)
{
    if (tables == null)
        return;

    int count = Mathf.Min(40, tables.DiffuseTexturePath.Length);
    for (int i = 0; i < count; i++)
    {
        tables.Usage[i] = 0;
        tables.UseBump[i] = false;
        tables.UScale[i] = 1.0f;
        tables.VScale[i] = 1.0f;
        tables.UShift[i] = 0.0f;
        tables.VShift[i] = 0.0f;
        tables.DiffuseTexturePath[i] = $@"Textures\ground\tex{i + 1}.bmp";
        tables.BumpTexturePath[i] = string.Empty;
        tables.BumpDegree[i] = 1.0f;
        tables.BumpContrast[i] = 0.6f;
        tables.BumpBrightness[i] = 1.0f;
    }

    tables.ActiveEntryCount = count;
    tables.LoadedFromXml = false;
    tables.SourceXmlPath = string.Empty;
    tables.SourceTexturesXmlPath = string.Empty;
    tables.SourceDatPath = string.Empty;
    tables.SourceKind = "fallback-default40";
}


private static bool TryLoadFacturesDatLikeAdapted(string path, FactureMaterialTablesLikeAdapted tables)
{
    try
    {
        string[] lines = File.ReadAllLines(path);
        int idx = 0;
        int maxEntries = Mathf.Min(40, tables.Usage.Length);
        for (int i = 0; i < lines.Length && idx < maxEntries; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < parts.Length && idx < maxEntries; j++)
            {
                if (int.TryParse(parts[j], NumberStyles.Integer, CultureInfo.InvariantCulture, out int usage))
                    tables.Usage[idx++] = (byte)Mathf.Clamp(usage, 0, 2);
            }
        }

        tables.ActiveEntryCount = Mathf.Max(tables.ActiveEntryCount, idx);
        return idx > 0;
    }
    catch
    {
        return false;
    }
}

private static bool TryLoadFacturesListXmlLikeAdapted(string path, FactureMaterialTablesLikeAdapted tables)
{
    try
    {
        string xml = File.ReadAllText(path);
        MatchCollection matches = Regex.Matches(xml, @"<OneFactureInfo\b[\s\S]*?</OneFactureInfo>", RegexOptions.IgnoreCase);
        if (matches.Count <= 0)
            return false;

        int idx = 0;
        foreach (Match match in matches)
        {
            if (idx >= tables.Usage.Length)
                break;

            string block = match.Value;
            string diffusePath = NormalizeFactureTexturePathLikeAdapted(ExtractXmlStringValueLikeAdapted(block, "FactureID", string.Empty));
            string bumpPath = NormalizeFactureTexturePathLikeAdapted(ExtractXmlStringValueLikeAdapted(block, "BumpTextureID", string.Empty));
            string normalePath = NormalizeFactureTexturePathLikeAdapted(ExtractXmlStringValueLikeAdapted(block, "NormaleTextureID", string.Empty));

            tables.Usage[idx] = (byte)Mathf.Clamp(ExtractXmlIntValueLikeAdapted(block, "Mapping", 0), 0, 2);
            tables.UseBump[idx] = ExtractXmlBoolValueLikeAdapted(block, "UseBumpMap", false)
                || !string.IsNullOrWhiteSpace(bumpPath)
                || !string.IsNullOrWhiteSpace(normalePath);

            tables.UScale[idx] = ExtractXmlFloatValueLikeAdapted(block, "UScale", 1.0f);
            tables.VScale[idx] = ExtractXmlFloatValueLikeAdapted(block, "VScale", 1.0f);
            tables.UShift[idx] = ExtractXmlFloatValueLikeAdapted(block, "UShift", 0.0f);
            tables.VShift[idx] = ExtractXmlFloatValueLikeAdapted(block, "VShift", 0.0f);
            tables.BumpDegree[idx] = ExtractXmlFloatValueLikeAdapted(block, "BumpDegree", 1.0f);
            tables.BumpContrast[idx] = ExtractXmlFloatValueLikeAdapted(block, "BumpContrast", 0.6f);
            tables.BumpBrightness[idx] = ExtractXmlFloatValueLikeAdapted(block, "BumpBrightness", 1.0f);
            tables.DiffuseTexturePath[idx] = diffusePath;
            tables.BumpTexturePath[idx] = !string.IsNullOrWhiteSpace(bumpPath) ? bumpPath : normalePath;
            idx++;
        }

        tables.ActiveEntryCount = idx;
        return idx > 0;
    }
    catch
    {
        return false;
    }
}

private static string ExtractXmlStringValueLikeAdapted(string xmlBlock, string tagName, string fallback)
{
    if (string.IsNullOrEmpty(xmlBlock) || string.IsNullOrEmpty(tagName))
        return fallback;

    Match m = Regex.Match(xmlBlock, $@"<{Regex.Escape(tagName)}>\s*([^<]+?)\s*</{Regex.Escape(tagName)}>", RegexOptions.IgnoreCase);
    if (!m.Success)
        return fallback;

    string value = m.Groups[1].Value.Trim();
    if (string.IsNullOrEmpty(value))
        return fallback;

    return value;
}


private static string NormalizeFactureTexturePathLikeAdapted(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

    string normalized = value.Trim().Trim('"', '\'', ' ');
    if (string.IsNullOrWhiteSpace(normalized))
        return string.Empty;

    normalized = normalized.Replace('/', '\\');

    bool numericOnly = true;
    for (int i = 0; i < normalized.Length; i++)
    {
        char ch = normalized[i];
        if (!(char.IsDigit(ch) || ch == '+' || ch == '-'))
        {
            numericOnly = false;
            break;
        }
    }

    if (numericOnly)
        return string.Empty;

    while (normalized.StartsWith(@".\", StringComparison.Ordinal))
        normalized = normalized.Substring(2);

    return normalized;
}

private static int ExtractXmlIntValueLikeAdapted(string xmlBlock, string tagName, int fallback)
{
    if (string.IsNullOrEmpty(xmlBlock) || string.IsNullOrEmpty(tagName))
        return fallback;

    Match m = Regex.Match(xmlBlock, $@"<{Regex.Escape(tagName)}>\s*([-+]?\d+)\s*</{Regex.Escape(tagName)}>", RegexOptions.IgnoreCase);
    if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        return value;
    return fallback;
}

private static float ExtractXmlFloatValueLikeAdapted(string xmlBlock, string tagName, float fallback)
{
    if (string.IsNullOrEmpty(xmlBlock) || string.IsNullOrEmpty(tagName))
        return fallback;

    Match m = Regex.Match(xmlBlock, $@"<{Regex.Escape(tagName)}>\s*([-+]?\d+(?:[\.,]\d+)?)\s*</{Regex.Escape(tagName)}>", RegexOptions.IgnoreCase);
    if (m.Success)
    {
        string valueText = m.Groups[1].Value.Replace(',', '.');
        if (float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            return value;
    }

    return fallback;
}

private static bool ExtractXmlBoolValueLikeAdapted(string xmlBlock, string tagName, bool fallback)
{
    if (string.IsNullOrEmpty(xmlBlock) || string.IsNullOrEmpty(tagName))
        return fallback;

    Match m = Regex.Match(xmlBlock, $@"<{Regex.Escape(tagName)}>\s*([^<]+?)\s*</{Regex.Escape(tagName)}>", RegexOptions.IgnoreCase);
    if (!m.Success)
        return fallback;

    string value = m.Groups[1].Value.Trim();
    if (bool.TryParse(value, out bool boolValue))
        return boolValue;
    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
        return intValue != 0;
    return fallback;
}

private static FactureUsageLikeOriginal ResolveFactureUsageLikeAdapted(int rawFactureId)
{
    FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
    int idx = Mathf.Clamp(rawFactureId & 255, 0, tables.Usage.Length - 1);
    return (FactureUsageLikeOriginal)tables.Usage[idx];
}

private static bool ResolveFactureBumpFlagLikeAdapted(int rawFactureId)
{
    FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
    int idx = Mathf.Clamp(rawFactureId & 255, 0, tables.UseBump.Length - 1);
    return tables.UseBump[idx];
}

private static void GetFactureBumpParamsLikeAdapted(int rawFactureId, out float degree, out float contrast, out float brightness)
{
    FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
    int idx = Mathf.Clamp(rawFactureId & 255, 0, tables.UseBump.Length - 1);
    degree = tables.BumpDegree[idx];
    contrast = tables.BumpContrast[idx];
    brightness = tables.BumpBrightness[idx];
}

private static int ResolveFactureRenderIndexLikeAdapted(ParsedMap map, int vertexIndex, out FactureUsageLikeOriginal usage, out FactureOrientationLikeAdapted orientation, out int variantIndex)
{
    int rawFactureId = GetFactureIdLikeOriginal(map, vertexIndex) & 255;
    return ResolveFactureRenderIndexForRawLikeAdapted(map, vertexIndex, rawFactureId, out usage, out orientation, out variantIndex);
}

private static int ResolveFactureRenderIndexForRawLikeAdapted(ParsedMap map, int vertexIndex, int rawFactureId, out FactureUsageLikeOriginal usage, out FactureOrientationLikeAdapted orientation, out int variantIndex)
{
    usage = FactureUsageLikeOriginal.Unknown;
    orientation = FactureOrientationLikeAdapted.None;
    variantIndex = 0;

    rawFactureId &= 255;
    usage = ResolveFactureUsageLikeAdapted(rawFactureId);
    if (usage == FactureUsageLikeOriginal.Unknown)
        usage = FactureUsageLikeOriginal.Planar;

    if (usage == FactureUsageLikeOriginal.Planar)
    {
        int vx = map != null && map.VertInLine > 0 ? (vertexIndex % map.VertInLine) : 0;
        int vy = map != null && map.VertInLine > 0 ? (vertexIndex / map.VertInLine) : 0;
        int uu = (vx + vy) / 12;
        int uv = (vx - vy) / 12;
        short[] randoma = GetRandomTableLikeOriginal();
        int rnd = randoma != null && randoma.Length > 0 ? randoma[(uu + uv * 17) & 8191] : 0;
        variantIndex = rnd & 63;
        orientation = FactureOrientationLikeAdapted.None;
        return rawFactureId + (variantIndex * 256);
    }

    Vector3 n = GetFactureVertexNormalLikeOriginal(map, vertexIndex);
    float anx = Mathf.Abs(n.x);
    float any = Mathf.Abs(n.y);
    variantIndex = 0;

    if (any > anx * 2.0f)
    {
        orientation = FactureOrientationLikeAdapted.DominantY;
        variantIndex = 1;
        return rawFactureId + 256;
    }

    if (anx > any * 2.0f)
    {
        orientation = FactureOrientationLikeAdapted.DominantX;
        variantIndex = 2;
        return rawFactureId + 512;
    }

    if (n.x < 0.0f)
    {
        orientation = FactureOrientationLikeAdapted.NegativeX;
        variantIndex = 3;
        return rawFactureId + 768;
    }

    orientation = FactureOrientationLikeAdapted.PositiveX;
    variantIndex = 4;
    return rawFactureId + 1024;
}

private static int GetFactureBucketTextureIdLikeAdapted(int renderFactureId)
{
    return renderFactureId & 255;
}

private static FactureVertexInfluenceLikeAdapted BuildFactureVertexInfluenceLikeAdapted(ParsedMap map, int vertexIndex)
{
    FactureVertexInfluenceLikeAdapted influence = new FactureVertexInfluenceLikeAdapted
    {
        VertexIndex = vertexIndex,
        RawFactureId = GetFactureIdLikeOriginal(map, vertexIndex) & 255,
        Weight = GetFactureWeightByIdxLikeOriginal(map, vertexIndex),
        Usage = FactureUsageLikeOriginal.Unknown,
        Orientation = FactureOrientationLikeAdapted.None,
        VariantIndex = 0,
        HasBump = false,
        RenderFactureId = 0
    };

    influence.RenderFactureId = ResolveFactureRenderIndexLikeAdapted(map, vertexIndex, out influence.Usage, out influence.Orientation, out influence.VariantIndex);
    influence.HasBump = ResolveFactureBumpFlagLikeAdapted(influence.RawFactureId);
    return influence;
}

private static FactureTriangleSourceDescriptorLikeAdapted BuildFactureTriangleSourceDescriptorLikeAdapted(ParsedMap map, BaseSurfaceTriangleKindLikeOriginal sourceKind, int sourceCellX, int sourceCellY, int vertexA, int vertexB, int vertexC)
{
    return new FactureTriangleSourceDescriptorLikeAdapted
    {
        SourceKind = sourceKind,
        SourceCellX = sourceCellX,
        SourceCellY = sourceCellY,
        VertexA = vertexA,
        VertexB = vertexB,
        VertexC = vertexC,
        InfluenceA = BuildFactureVertexInfluenceLikeAdapted(map, vertexA),
        InfluenceB = BuildFactureVertexInfluenceLikeAdapted(map, vertexB),
        InfluenceC = BuildFactureVertexInfluenceLikeAdapted(map, vertexC),
    };
}

private static FactureTriangleCopyDescriptorLikeAdapted BuildFactureTriangleCopyDescriptorSkeletonLikeAdapted(FactureTriangleSourceDescriptorLikeAdapted source, int copyFactureId)
{
    FactureTriangleCopyDescriptorLikeAdapted descriptor = new FactureTriangleCopyDescriptorLikeAdapted
    {
        SourceKind = source.SourceKind,
        SourceCellX = source.SourceCellX,
        SourceCellY = source.SourceCellY,
        VertexA = source.VertexA,
        VertexB = source.VertexB,
        VertexC = source.VertexC,
        SourceFactureA = source.InfluenceA.RenderFactureId,
        SourceFactureB = source.InfluenceB.RenderFactureId,
        SourceFactureC = source.InfluenceC.RenderFactureId,
        CopyFactureId = copyFactureId,
        Usage = FactureUsageLikeOriginal.Unknown,
        Orientation = FactureOrientationLikeAdapted.None,
        VariantIndex = 0,
        WeightA = source.InfluenceA.RawFactureId == (copyFactureId & 255) ? source.InfluenceA.Weight : 0,
        WeightB = source.InfluenceB.RawFactureId == (copyFactureId & 255) ? source.InfluenceB.Weight : 0,
        WeightC = source.InfluenceC.RawFactureId == (copyFactureId & 255) ? source.InfluenceC.Weight : 0,
        UvA = Vector2.zero,
        UvB = Vector2.zero,
        UvC = Vector2.zero,
        BucketTextureId = GetFactureBucketTextureIdLikeAdapted(copyFactureId),
        HasBump = false
    };

    if (source.InfluenceA.RenderFactureId == copyFactureId)
    {
        descriptor.Usage = source.InfluenceA.Usage;
        descriptor.Orientation = source.InfluenceA.Orientation;
        descriptor.VariantIndex = source.InfluenceA.VariantIndex;
        descriptor.HasBump = source.InfluenceA.HasBump;
    }
    else if (source.InfluenceB.RenderFactureId == copyFactureId)
    {
        descriptor.Usage = source.InfluenceB.Usage;
        descriptor.Orientation = source.InfluenceB.Orientation;
        descriptor.VariantIndex = source.InfluenceB.VariantIndex;
        descriptor.HasBump = source.InfluenceB.HasBump;
    }
    else if (source.InfluenceC.RenderFactureId == copyFactureId)
    {
        descriptor.Usage = source.InfluenceC.Usage;
        descriptor.Orientation = source.InfluenceC.Orientation;
        descriptor.VariantIndex = source.InfluenceC.VariantIndex;
        descriptor.HasBump = source.InfluenceC.HasBump;
    }

    return descriptor;
}


private static bool HasFactureContributionLikeAdapted(FactureTriangleSourceDescriptorLikeAdapted source)
{
    return source.InfluenceA.Weight > 0 || source.InfluenceB.Weight > 0 || source.InfluenceC.Weight > 0;
}

private static void GetFactureNeighbourHeightsLikeAdapted(ParsedMap map, int vertexIndex, out int h, out int hlu, out int hld, out int hru, out int hrd, out int hu, out int hd)
{
    h = 0;
    hlu = 0;
    hld = 0;
    hru = 0;
    hrd = 0;
    hu = 0;
    hd = 0;

    if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 0 || map.MaxTH <= 0 || vertexIndex < 0 || vertexIndex >= map.Heights.Length)
        return;

    int vx = vertexIndex % map.VertInLine;
    int vy = vertexIndex / map.VertInLine;
    h = map.Heights[vertexIndex];
    int baseHeight = h;

    int SampleHeight(int x, int y)
    {
        x = Mathf.Clamp(x, 0, map.VertInLine - 1);
        y = Mathf.Clamp(y, 0, map.MaxTH - 1);
        int idx = y * map.VertInLine + x;
        if (idx < 0 || idx >= map.Heights.Length)
            return baseHeight;
        return map.Heights[idx];
    }

    if ((vx & 1) != 0)
    {
        if (vy > 0)
        {
            hu = SampleHeight(vx, vy - 1);
            hlu = SampleHeight(vx - 1, vy - 1);
            hru = SampleHeight(vx + 1, vy - 1);
        }
        else
        {
            hu = h;
            hlu = h;
            hru = h;
        }

        hld = SampleHeight(vx - 1, vy);
        hrd = SampleHeight(vx + 1, vy);
        hd = SampleHeight(vx, vy + 1);
    }
    else
    {
        if (vx > 0)
        {
            hlu = SampleHeight(vx - 1, vy);
            hld = SampleHeight(vx - 1, vy + 1);
        }
        else
        {
            hlu = h;
            hld = h;
        }

        hu = vy > 0 ? SampleHeight(vx, vy - 1) : h;
        hru = SampleHeight(vx + 1, vy);
        hrd = SampleHeight(vx + 1, vy + 1);
        hd = SampleHeight(vx, vy + 1);
    }
}

private static int SampleThMapVertexLikeOriginal(ParsedMap map, int vx, int vy)
{
    if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 0 || map.MaxTH <= 0)
        return 0;

    vx = Mathf.Clamp(vx, 0, map.VertInLine - 1);
    vy = Mathf.Clamp(vy, 0, map.MaxTH - 1);
    int idx = vy * map.VertInLine + vx;
    if (idx < 0 || idx >= map.Heights.Length)
        return 0;
    return map.Heights[idx];
}

private static int GetHeightLikeOriginal(ParsedMap map, int x, int y)
{
    if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 0 || map.MaxTH <= 0)
        return 0;

    int maxX = Mathf.Max(0, (map.VertInLine - 1) << 5);
    int maxY = Mathf.Max(32, (map.MaxTH - 1) << 5);

    if (x < 0) x = 0;
    if (y < 32) y = 32;
    if (x > maxX) x = maxX;
    if (y > maxY) y = maxY;

    int nx = x >> 5;
    if ((nx & 1) != 0)
    {
        int dd = x & 31;
        int dy = dd >> 1;
        int oy = 15 - dy;
        int y1 = (y + oy) >> 5;
        int dy1 = (y + oy) & 31;

        if (dy1 > 32 - dd)
        {
            int h2 = SampleThMapVertexLikeOriginal(map, nx + 1, y1);
            int h3 = SampleThMapVertexLikeOriginal(map, nx + 1, y1 + 1);
            int h1 = SampleThMapVertexLikeOriginal(map, nx, y1 + 1);
            int x0 = nx << 5;
            int y0 = (y1 << 5) + 16;
            return h1 + (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
        }
        else
        {
            int h2 = SampleThMapVertexLikeOriginal(map, nx, y1);
            int h3 = SampleThMapVertexLikeOriginal(map, nx, y1 + 1);
            int h1 = SampleThMapVertexLikeOriginal(map, nx + 1, y1);
            int x0 = (nx << 5) + 32;
            int y0 = (y1 << 5);
            return h1 - (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
        }
    }
    else
    {
        int dd = x & 31;
        int dy = dd >> 1;
        int y1 = (y + dy) >> 5;
        int dy1 = (y + dy) & 31;

        if (dy1 < dd)
        {
            int h1 = SampleThMapVertexLikeOriginal(map, nx, y1);
            int h2 = SampleThMapVertexLikeOriginal(map, nx + 1, y1);
            int h3 = SampleThMapVertexLikeOriginal(map, nx + 1, y1 + 1);
            int x0 = nx << 5;
            int y0 = y1 << 5;
            return h1 + (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
        }
        else
        {
            int h2 = SampleThMapVertexLikeOriginal(map, nx, y1);
            int h3 = SampleThMapVertexLikeOriginal(map, nx, y1 + 1);
            int h1 = SampleThMapVertexLikeOriginal(map, nx + 1, y1 + 1);
            int x0 = (nx << 5) + 32;
            int y0 = (y1 << 5) + 16;
            return h1 - (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
        }
    }
}

private static Vector3 GetSurfaceNLikeOriginal(ParsedMap map, int x, int y)
{
    int hx0 = GetHeightLikeOriginal(map, x - 64, y);
    int hx1 = GetHeightLikeOriginal(map, x + 64, y);
    int hy0 = GetHeightLikeOriginal(map, x, y - 64);
    int hy1 = GetHeightLikeOriginal(map, x, y + 64);

    Vector3 n = new Vector3(hx0 - hx1, hy0 - hy1, 128.0f);
    if (n.sqrMagnitude <= 1e-6f)
        return new Vector3(0.0f, 0.0f, 1.0f);
    return n.normalized;
}

private static int MaxD3LikeAdapted(int a, int b, int c)
{
    return Mathf.Max(a, Mathf.Max(b, c));
}

private static int Min3LikeAdapted(int a, int b, int c)
{
    return Mathf.Min(a, Mathf.Min(b, c));
}

private static int ComputeFactureSlopeMaxWeightLikeAdapted(int hlu, int hld, int hru, int hrd, int hu, int hd, int h)
{
    int a = MaxD3LikeAdapted(hlu, h, hld);
    int b = MaxD3LikeAdapted(hlu, hu, h);
    int c = MaxD3LikeAdapted(hu, h, hru);
    int d = MaxD3LikeAdapted(hru, hrd, h);
    int e = MaxD3LikeAdapted(hrd, hd, h);
    int f = MaxD3LikeAdapted(hd, h, hld);
    int minRing = Min3LikeAdapted(Min3LikeAdapted(a, b, c), Min3LikeAdapted(d, e, f), 255);
    int maxW = 180 * minRing;
    return Mathf.Clamp(maxW, 0, 255);
}

private static void GetFactureUvInfoLikeAdapted(int rawFactureId, out float du, out float dv, out float su, out float sv)
{
    FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
    int idx = Mathf.Clamp(rawFactureId & 255, 0, tables.UScale.Length - 1);
    du = tables.UShift[idx];
    dv = tables.VShift[idx];
    su = tables.UScale[idx];
    sv = tables.VScale[idx];
}

private static void GetFactureUvwLikeAdapted(ParsedMap map, int vertexIndex, int renderFactureId, out Vector2 uv, out int maxWeight)
{
    uv = Vector2.zero;
    maxWeight = 0;

    if (map == null || map.VertInLine <= 0 || map.MaxTH <= 0 || vertexIndex < 0)
        return;

    int idx = renderFactureId & 255;
    int opt = renderFactureId >> 8;
    FactureUsageLikeOriginal usage = ResolveFactureUsageLikeAdapted(idx);
    if (usage == FactureUsageLikeOriginal.Unknown)
        usage = FactureUsageLikeOriginal.Planar;

    int vx = vertexIndex % map.VertInLine;
    int vy = vertexIndex / map.VertInLine;

    float tx = (vx << 5) + (usage == FactureUsageLikeOriginal.Planar ? GetVertexXShiftLikeOriginal(map, vertexIndex) : 0.0f);
    float ty = (vy << 5) - (((vx & 1) != 0) ? 16.0f : 0.0f) + (usage == FactureUsageLikeOriginal.Planar ? GetVertexYShiftLikeOriginal(map, vertexIndex) : 0.0f);

    if (usage == FactureUsageLikeOriginal.Planar)
    {
        int adop = (opt >> 6) + 1;
        float u = (tx + ((opt & 7) * 32.0f)) / 256.0f;
        float v = (ty + (((opt / 8) & 7) * 32.0f)) / 512.0f * adop;
        maxWeight = idx == 0 ? 0xA0 : 255;

        GetFactureUvInfoLikeAdapted(idx, out float du, out float dv, out float su, out float sv);
        uv = new Vector2((u + du) * su, (v + dv) * sv);
        return;
    }

    GetFactureNeighbourHeightsLikeAdapted(map, vertexIndex, out int h, out int hlu, out int hld, out int hru, out int hrd, out int hu, out int hd);
    if ((vx & 1) != 0)
        ty -= 16.0f;

    float uu = 0.0f;
    float vv = 0.0f;
    maxWeight = ComputeFactureSlopeMaxWeightLikeAdapted(hlu, hld, hru, hrd, hu, hd, h);

    if (usage == FactureUsageLikeOriginal.Vertical)
    {
        vv = -h / 180.0f;
    }
    else
    {
        int x = vx << 5;
        int y = (vy << 5) - (((vx & 1) != 0) ? 16 : 0);
        Vector3 n = GetSurfaceNLikeOriginal(map, x, y);
        float slope = (1.0f - n.z) * 2.0f;
        if (slope > 1.0f)
            slope = 1.0f;
        if (slope < 0.0f)
            slope = 0.0f;
        vv = slope;
    }

    if (opt == 1)
        uu = tx / 256.0f;
    else if (opt == 2)
        uu = ty / 256.0f;
    else if (opt == 3)
        uu = (tx + ty) / 256.0f / 1.4142f;
    else
        uu = (tx - ty) / 256.0f / 1.4142f;

    uu *= 1.5f;
    vv *= 1.5f;

    GetFactureUvInfoLikeAdapted(idx, out float du2, out float dv2, out float su2, out float sv2);
    uv = new Vector2((uu + du2) * su2, (vv + dv2) * sv2);
}

private static int ClampFactureBatchWeightLikeOriginal(int weight, int sampledMaxWeight)
{
    if (weight <= 0 || sampledMaxWeight <= 0)
        return 0;
    return Mathf.Clamp(Mathf.Min(weight, sampledMaxWeight), 0, 255);
}

private static int ApplyFactureCoverageDeadZoneLikeAdapted(int weight)
{
    // Keep literal engine semantics: emitted facture batches are driven by W > 0 and max-weight clamping.
    return Mathf.Clamp(weight, 0, 255);
}

private static bool RejectWeakFactureTriangleCopyLikeAdapted(ref FactureTriangleCopyDescriptorLikeAdapted descriptor)
{
    descriptor.WeightA = ApplyFactureCoverageDeadZoneLikeAdapted(descriptor.WeightA);
    descriptor.WeightB = ApplyFactureCoverageDeadZoneLikeAdapted(descriptor.WeightB);
    descriptor.WeightC = ApplyFactureCoverageDeadZoneLikeAdapted(descriptor.WeightC);

    int maxWeight = Mathf.Max(descriptor.WeightA, Mathf.Max(descriptor.WeightB, descriptor.WeightC));
    return maxWeight <= 0;
}

private static bool TryBuildFactureTriangleCopyBatchLikeOriginal(
    ParsedMap map,
    FactureTriangleSourceDescriptorLikeAdapted source,
    int batchIndex,
    int copyFactureId,
    int uvSourceFactureId,
    out FactureTriangleCopyDescriptorLikeAdapted descriptor)
{
    descriptor = BuildFactureTriangleCopyDescriptorSkeletonLikeAdapted(source, copyFactureId);

    GetFactureUvwLikeAdapted(map, source.VertexA, uvSourceFactureId, out descriptor.UvA, out int maxWeightA);
    GetFactureUvwLikeAdapted(map, source.VertexB, uvSourceFactureId, out descriptor.UvB, out int maxWeightB);
    GetFactureUvwLikeAdapted(map, source.VertexC, uvSourceFactureId, out descriptor.UvC, out int maxWeightC);

    int f1 = source.InfluenceA.RenderFactureId;
    int f2 = source.InfluenceB.RenderFactureId;
    int f3 = source.InfluenceC.RenderFactureId;

    int w1 = Mathf.Clamp(source.InfluenceA.Weight, 0, 255);
    int w2 = Mathf.Clamp(source.InfluenceB.Weight, 0, 255);
    int w3 = Mathf.Clamp(source.InfluenceC.Weight, 0, 255);

    switch (batchIndex)
    {
        case 1:
            descriptor.WeightA = ClampFactureBatchWeightLikeOriginal(w1, maxWeightA);
            descriptor.WeightB = f2 == f1 ? ClampFactureBatchWeightLikeOriginal(w2, maxWeightB) : 0;
            descriptor.WeightC = f3 == f1 ? ClampFactureBatchWeightLikeOriginal(w3, maxWeightC) : 0;
            break;

        case 2:
            descriptor.WeightA = 0;
            descriptor.WeightB = ClampFactureBatchWeightLikeOriginal(w2, maxWeightB);
            descriptor.WeightC = f3 == f2 ? ClampFactureBatchWeightLikeOriginal(w3, maxWeightC) : 0;
            break;

        case 3:
            descriptor.WeightA = 0;
            descriptor.WeightB = 0;
            // Retail quirk: batch3 fetches UV/maxWeight from F2 path, but alpha comes only from W3.
            descriptor.WeightC = ClampFactureBatchWeightLikeOriginal(w3, maxWeightC);
            break;

        default:
            descriptor.WeightA = 0;
            descriptor.WeightB = 0;
            descriptor.WeightC = 0;
            break;
    }

    if (RejectWeakFactureTriangleCopyLikeAdapted(ref descriptor))
        return false;

    return true;
}

private static void ExpandFactureTriangleCopiesLikeAdapted(ParsedMap map, FactureTriangleSourceDescriptorLikeAdapted source, List<FactureTriangleCopyDescriptorLikeAdapted> output)
{
    if (output == null || !HasFactureContributionLikeAdapted(source))
        return;

    int f1 = source.InfluenceA.RenderFactureId;
    int f2 = source.InfluenceB.RenderFactureId;
    int f3 = source.InfluenceC.RenderFactureId;

    if (source.InfluenceA.Weight > 0 &&
        TryBuildFactureTriangleCopyBatchLikeOriginal(map, source, 1, f1, f1, out FactureTriangleCopyDescriptorLikeAdapted copyA))
    {
        output.Add(copyA);
    }

    if (source.InfluenceB.Weight > 0 &&
        f2 != f1 &&
        TryBuildFactureTriangleCopyBatchLikeOriginal(map, source, 2, f2, f2, out FactureTriangleCopyDescriptorLikeAdapted copyB))
    {
        output.Add(copyB);
    }

    if (source.InfluenceC.Weight > 0 &&
        f3 != f1 &&
        f3 != f2 &&
        TryBuildFactureTriangleCopyBatchLikeOriginal(map, source, 3, f3, f2, out FactureTriangleCopyDescriptorLikeAdapted copyC))
    {
        output.Add(copyC);
    }
}

private static int ExpandFactureTriangleCopiesLikeAdapted(ParsedMap map, BaseSurfaceTriangleKindLikeOriginal sourceKind, int sourceCellX, int sourceCellY, int vertexA, int vertexB, int vertexC, List<FactureTriangleCopyDescriptorLikeAdapted> output)
{
    if (output == null || !HasFactureLayerDataLikeOriginal(map))
        return 0;

    int startCount = output.Count;
    FactureTriangleSourceDescriptorLikeAdapted source = BuildFactureTriangleSourceDescriptorLikeAdapted(map, sourceKind, sourceCellX, sourceCellY, vertexA, vertexB, vertexC);
    ExpandFactureTriangleCopiesLikeAdapted(map, source, output);
    return output.Count - startCount;
}

        

        private static Color32 BuildFactureVertexColorLikeAdapted(int alpha)
        {
            byte a = (byte)Mathf.Clamp(alpha, 0, 255);
            return new Color32(255, 255, 255, a);
        }

        private static Vector3 GetFactureLightDirLikeAdapted()
        {
            Vector3 l = new Vector3(C2GlobalLighting.LightDX, C2GlobalLighting.LightDY, C2GlobalLighting.LightDZ);
            if (l.sqrMagnitude <= 1e-6f)
                l = new Vector3(0.0f, 0.0f, 255.0f);
            l.Normalize();
            return -l;
        }

        private static Color32 BuildFactureBumpVertexColorLikeAdapted(ParsedMap map, int vertexIndex, int alpha, int bucketTextureId)
        {
            byte a = (byte)Mathf.Clamp(alpha, 0, 255);
            int vx = vertexIndex % map.VertInLine;
            int vy = vertexIndex / map.VertInLine;
            int x = vx << 5;
            int y = (vy << 5) - ((vx & 1) << 4);
            Vector3 normal = GetSurfaceNLikeOriginal(map, x, y);
            Vector3 ldir = GetFactureLightDirLikeAdapted();
            float dotp = Vector3.Dot(ldir, normal);
            if (dotp < 0.0f)
                ldir = Vector3.zero;

            GetFactureBumpParamsLikeAdapted(bucketTextureId, out float degree, out float contrast, out float brightness);
            float pr1 = dotp < 0.0f ? contrast : contrast * (1.0f - 2.0f * dotp);
            if (pr1 < 0.0f)
                pr1 = 0.0f;

            Vector3 vn = normal * pr1;
            Vector3 tangentLight = ldir + vn;
            float n = tangentLight.magnitude;
            if (n > brightness && n > 1e-6f)
                tangentLight *= brightness / n;

            Vector3 tangent = Vector3.right - normal * Vector3.Dot(Vector3.right, normal);
            if (tangent.sqrMagnitude <= 1e-6f)
                tangent = Vector3.forward - normal * Vector3.Dot(Vector3.forward, normal);
            tangent.Normalize();
            Vector3 binormal = Vector3.Cross(normal, tangent).normalized;
            Vector3 ts = new Vector3(Vector3.Dot(tangentLight, tangent), Vector3.Dot(tangentLight, binormal), Vector3.Dot(tangentLight, normal));
            ts = Vector3.ClampMagnitude(ts, 1.0f);
            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt((ts.x * 0.5f + 0.5f) * 255.0f), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt((ts.y * 0.5f + 0.5f) * 255.0f), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt((ts.z * 0.5f + 0.5f) * 255.0f), 0, 255);
            return new Color32(r, g, b, a);
        }

        private static Vector3 ResolveFactureVertexWorldLikeAdapted(
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int vertexIndex)
        {
            if (a.Index == vertexIndex) return a.World;
            if (b.Index == vertexIndex) return b.World;
            if (c.Index == vertexIndex) return c.World;
            return a.World;
        }

        private static void AppendFactureVertexToBucketLikeAdapted(FactureBucketMeshDataLikeAdapted bucket, Vector3 position, Color32 color, Vector2 uv)
        {
            int index = bucket.Vertices.Count;
            bucket.Vertices.Add(position);
            bucket.Colors.Add(color);
            bucket.Uv0.Add(uv);
            bucket.Triangles.Add(index);

            if (!bucket.HasBounds)
            {
                bucket.Bounds = new Bounds(position, Vector3.zero);
                bucket.HasBounds = true;
            }
            else
            {
                bucket.Bounds.Encapsulate(position);
            }

            bucket.HasContent = true;
        }

        private static void AppendFactureCopyToBucketLikeAdapted(
            ParsedMap map,
            Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            FactureTriangleCopyDescriptorLikeAdapted descriptor)
        {
            int maxAlpha = Mathf.Max(descriptor.WeightA, Mathf.Max(descriptor.WeightB, descriptor.WeightC));
            if (maxAlpha <= 0)
                return;

            if (!buckets.TryGetValue(descriptor.BucketTextureId, out FactureBucketMeshDataLikeAdapted bucket))
            {
                bucket = new FactureBucketMeshDataLikeAdapted(128);
                buckets[descriptor.BucketTextureId] = bucket;
            }

            bucket.HasBumpContent |= descriptor.HasBump;

            AppendFactureVertexToBucketLikeAdapted(
                bucket,
                ResolveFactureVertexWorldLikeAdapted(a, b, c, descriptor.VertexA),
                descriptor.HasBump ? BuildFactureBumpVertexColorLikeAdapted(map, descriptor.VertexA, descriptor.WeightA, descriptor.BucketTextureId) : BuildFactureVertexColorLikeAdapted(descriptor.WeightA),
                descriptor.UvA);

            AppendFactureVertexToBucketLikeAdapted(
                bucket,
                ResolveFactureVertexWorldLikeAdapted(a, b, c, descriptor.VertexB),
                descriptor.HasBump ? BuildFactureBumpVertexColorLikeAdapted(map, descriptor.VertexB, descriptor.WeightB, descriptor.BucketTextureId) : BuildFactureVertexColorLikeAdapted(descriptor.WeightB),
                descriptor.UvB);

            AppendFactureVertexToBucketLikeAdapted(
                bucket,
                ResolveFactureVertexWorldLikeAdapted(a, b, c, descriptor.VertexC),
                descriptor.HasBump ? BuildFactureBumpVertexColorLikeAdapted(map, descriptor.VertexC, descriptor.WeightC, descriptor.BucketTextureId) : BuildFactureVertexColorLikeAdapted(descriptor.WeightC),
                descriptor.UvC);
        }

        private static int GetCellDominantRawFactureIdLikeAdapted(ParsedMap map, CellVertexPayloadLikeOriginal v0, CellVertexPayloadLikeOriginal v1, CellVertexPayloadLikeOriginal v2, CellVertexPayloadLikeOriginal v3, out int representativeVertexIndex)
        {
            representativeVertexIndex = -1;
            if (map == null)
                return 0;

            int[] ids =
            {
                GetFactureIdLikeOriginal(map, v0.Index) & 255,
                GetFactureIdLikeOriginal(map, v1.Index) & 255,
                GetFactureIdLikeOriginal(map, v2.Index) & 255,
                GetFactureIdLikeOriginal(map, v3.Index) & 255,
            };
            int[] vids = { v0.Index, v1.Index, v2.Index, v3.Index };
            int bestId = 0;
            int bestCount = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (id == 0)
                    continue;
                int count = 0;
                for (int j = 0; j < ids.Length; j++)
                    if (ids[j] == id) count++;
                if (count > bestCount)
                {
                    bestCount = count;
                    bestId = id;
                    representativeVertexIndex = vids[i];
                }
            }
            if (bestId == 0)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] != 0)
                    {
                        bestId = ids[i];
                        representativeVertexIndex = vids[i];
                        break;
                    }
                }
            }
            return bestId;
        }

        private static bool TryAppendCellDominantFallbackTriangleLikeAdapted(
            ParsedMap map,
            BaseSurfaceTriangleKindLikeOriginal kind,
            int cellX,
            int cellY,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int fallbackRenderFactureId,
            Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets)
        {
            if (fallbackRenderFactureId == 0 || buckets == null)
                return false;

            FactureTriangleCopyDescriptorLikeAdapted descriptor = new FactureTriangleCopyDescriptorLikeAdapted
            {
                SourceKind = kind,
                SourceCellX = cellX,
                SourceCellY = cellY,
                VertexA = a.Index,
                VertexB = b.Index,
                VertexC = c.Index,
                SourceFactureA = fallbackRenderFactureId,
                SourceFactureB = fallbackRenderFactureId,
                SourceFactureC = fallbackRenderFactureId,
                CopyFactureId = fallbackRenderFactureId,
                Usage = FactureUsageLikeOriginal.Unknown,
                Orientation = FactureOrientationLikeAdapted.None,
                VariantIndex = 0,
                WeightA = 255,
                WeightB = 255,
                WeightC = 255,
                BucketTextureId = GetFactureBucketTextureIdLikeAdapted(fallbackRenderFactureId),
                HasBump = ResolveFactureBumpFlagLikeAdapted(fallbackRenderFactureId & 255),
                UvA = Vector2.zero,
                UvB = Vector2.zero,
                UvC = Vector2.zero,
            };

            GetFactureUvwLikeAdapted(map, a.Index, fallbackRenderFactureId, out descriptor.UvA, out _);
            GetFactureUvwLikeAdapted(map, b.Index, fallbackRenderFactureId, out descriptor.UvB, out _);
            GetFactureUvwLikeAdapted(map, c.Index, fallbackRenderFactureId, out descriptor.UvC, out _);
            AppendFactureCopyToBucketLikeAdapted(map, buckets, a, b, c, descriptor);
            return true;
        }

        private static bool HasCompleteTriangleCoverageLikeAdapted(List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies)
        {
            if (scratchCopies == null || scratchCopies.Count == 0)
                return false;

            bool coveredA = false;
            bool coveredB = false;
            bool coveredC = false;

            for (int i = 0; i < scratchCopies.Count; i++)
            {
                FactureTriangleCopyDescriptorLikeAdapted copy = scratchCopies[i];
                coveredA |= copy.WeightA > 0;
                coveredB |= copy.WeightB > 0;
                coveredC |= copy.WeightC > 0;
                if (coveredA && coveredB && coveredC)
                    return true;
            }

            return false;
        }

        private static void ExpandFactureTriangleIntoBucketsLikeAdapted(
            ParsedMap map,
            BaseSurfaceTriangleKindLikeOriginal kind,
            int cellX,
            int cellY,
            CellVertexPayloadLikeOriginal a,
            CellVertexPayloadLikeOriginal b,
            CellVertexPayloadLikeOriginal c,
            int fallbackRenderFactureId,
            Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets,
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies)
        {
            if (scratchCopies == null)
                return;

            scratchCopies.Clear();
            ExpandFactureTriangleCopiesLikeAdapted(map, kind, cellX, cellY, a.Index, b.Index, c.Index, scratchCopies);

            bool emittedAny = scratchCopies.Count > 0;
            for (int i = 0; i < scratchCopies.Count; i++)
            {
                AppendFactureCopyToBucketLikeAdapted(map, buckets, a, b, c, scratchCopies[i]);
            }

            if (!emittedAny)
            {
                TryAppendCellDominantFallbackTriangleLikeAdapted(map, kind, cellX, cellY, a, b, c, fallbackRenderFactureId, buckets);
                return;
            }

            if (!HasCompleteTriangleCoverageLikeAdapted(scratchCopies))
            {
                TryAppendCellDominantFallbackTriangleLikeAdapted(map, kind, cellX, cellY, a, b, c, fallbackRenderFactureId, buckets);
            }
        }

        private static void ExpandFactureCellIntoBucketsLikeAdapted(
            ParsedMap map,
            OriginalCellTriangulationLikeOriginal cell,
            int cellX,
            int cellY,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3,
            Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets,
            List<FactureTriangleCopyDescriptorLikeAdapted> scratchCopies)
        {
            bool isOddCell = cell.FirstA == cell.V0 && cell.FirstB == cell.V1 && cell.FirstC == cell.V2;
            int representativeVertexIndex;
            int fallbackRawFactureId = GetCellDominantRawFactureIdLikeAdapted(map, v0, v1, v2, v3, out representativeVertexIndex);
            int fallbackRenderFactureId = 0;
            if (fallbackRawFactureId != 0 && representativeVertexIndex >= 0)
                fallbackRenderFactureId = ResolveFactureRenderIndexLikeAdapted(map, representativeVertexIndex, out _, out _, out _);

            if (isOddCell)
            {
                ExpandFactureTriangleIntoBucketsLikeAdapted(map, BaseSurfaceTriangleKindLikeOriginal.OddLeft, cellX, cellY, v0, v1, v2, fallbackRenderFactureId, buckets, scratchCopies);
                ExpandFactureTriangleIntoBucketsLikeAdapted(map, BaseSurfaceTriangleKindLikeOriginal.OddRight, cellX, cellY, v2, v1, v3, fallbackRenderFactureId, buckets, scratchCopies);
            }
            else
            {
                ExpandFactureTriangleIntoBucketsLikeAdapted(map, BaseSurfaceTriangleKindLikeOriginal.EvenUpper, cellX, cellY, v0, v1, v3, fallbackRenderFactureId, buckets, scratchCopies);
                ExpandFactureTriangleIntoBucketsLikeAdapted(map, BaseSurfaceTriangleKindLikeOriginal.EvenLower, cellX, cellY, v0, v3, v2, fallbackRenderFactureId, buckets, scratchCopies);
            }
        }



private static bool TryChooseTriangleNeighbourWinnerRawFactureLikeAdapted(
    ParsedMap map,
    CellVertexPayloadLikeOriginal a,
    CellVertexPayloadLikeOriginal b,
    CellVertexPayloadLikeOriginal c,
    out int rawFactureId,
    out int representativeVertex)
{
    rawFactureId = 0;
    representativeVertex = -1;
    if (map == null || map.VertInLine <= 0 || map.MaxTH <= 0)
        return false;

    int ax = a.Index % map.VertInLine;
    int ay = a.Index / map.VertInLine;
    int bx = b.Index % map.VertInLine;
    int by = b.Index / map.VertInLine;
    int cx = c.Index % map.VertInLine;
    int cy = c.Index / map.VertInLine;

    int minX = Mathf.Min(ax, Mathf.Min(bx, cx));
    int maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
    int minY = Mathf.Min(ay, Mathf.Min(by, cy));
    int maxY = Mathf.Max(ay, Mathf.Max(by, cy));

    int bestTotal = -1;
    int bestCount = -1;
    int bestWeight = -1;
    int bestDistance = int.MaxValue;
    int bestId = 0;
    int bestRep = -1;

    for (int radius = 1; radius <= 12; radius++)
    {
        var totalById = new Dictionary<int, int>();
        var countById = new Dictionary<int, int>();
        var bestWeightById = new Dictionary<int, int>();
        var bestDistanceById = new Dictionary<int, int>();
        var repById = new Dictionary<int, int>();

        int x0 = Mathf.Max(0, minX - radius);
        int x1 = Mathf.Min(map.VertInLine - 1, maxX + radius);
        int y0 = Mathf.Max(0, minY - radius);
        int y1 = Mathf.Min(map.MaxTH - 1, maxY + radius);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int idx = y * map.VertInLine + x;
                int raw = GetFactureIdLikeOriginal(map, idx) & 255;
                if (raw == 0)
                    continue;

                int weight = GetFactureWeightByIdxLikeOriginal(map, idx);
                int distA = Mathf.Abs(x - ax) + Mathf.Abs(y - ay);
                int distB = Mathf.Abs(x - bx) + Mathf.Abs(y - by);
                int distC = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                int dist = Mathf.Min(distA, Mathf.Min(distB, distC));

                if (totalById.TryGetValue(raw, out int total))
                    totalById[raw] = total + Mathf.Max(weight, 1);
                else
                    totalById[raw] = Mathf.Max(weight, 1);

                if (countById.TryGetValue(raw, out int count))
                    countById[raw] = count + 1;
                else
                    countById[raw] = 1;

                if (!bestWeightById.TryGetValue(raw, out int currentBestWeight) || weight > currentBestWeight)
                {
                    bestWeightById[raw] = weight;
                    bestDistanceById[raw] = dist;
                    repById[raw] = idx;
                }
                else if (weight == currentBestWeight && (!bestDistanceById.TryGetValue(raw, out int currentBestDistance) || dist < currentBestDistance))
                {
                    bestDistanceById[raw] = dist;
                    repById[raw] = idx;
                }
            }
        }

        foreach (KeyValuePair<int, int> pair in totalById)
        {
            int id = pair.Key;
            int total = pair.Value;
            int count = countById.TryGetValue(id, out int cCount) ? cCount : 0;
            int peak = bestWeightById.TryGetValue(id, out int cPeak) ? cPeak : 0;
            int dist = bestDistanceById.TryGetValue(id, out int cDist) ? cDist : int.MaxValue;
            int rep = repById.TryGetValue(id, out int cRep) ? cRep : -1;

            bool better = false;
            if (bestId == 0)
            {
                better = true;
            }
            else if (total != bestTotal)
            {
                better = total > bestTotal;
            }
            else if (count != bestCount)
            {
                better = count > bestCount;
            }
            else if (peak != bestWeight)
            {
                better = peak > bestWeight;
            }
            else if (dist != bestDistance)
            {
                better = dist < bestDistance;
            }
            else
            {
                better = id < bestId;
            }

            if (better)
            {
                bestTotal = total;
                bestCount = count;
                bestWeight = peak;
                bestDistance = dist;
                bestId = id;
                bestRep = rep;
            }
        }

        if (bestId != 0 && bestRep >= 0)
        {
            rawFactureId = bestId;
            representativeVertex = bestRep;
            return true;
        }
    }

    return false;
}

private static bool TryChooseTriangleWinnerRenderFactureIdLikeAdapted(
    ParsedMap map,
    CellVertexPayloadLikeOriginal a,
    CellVertexPayloadLikeOriginal b,
    CellVertexPayloadLikeOriginal c,
    out int renderFactureId)
{
    renderFactureId = 0;
    if (map == null)
        return false;

    int w1 = GetFactureWeightByIdxLikeOriginal(map, a.Index);
    int w2 = GetFactureWeightByIdxLikeOriginal(map, b.Index);
    int w3 = GetFactureWeightByIdxLikeOriginal(map, c.Index);

    int representativeVertex = -1;
    int rawFactureId = 0;

    if (w1 > 0 || w2 > 0 || w3 > 0)
    {
        representativeVertex = a.Index;
        int bestWeight = w1;
        if (w2 > bestWeight)
        {
            bestWeight = w2;
            representativeVertex = b.Index;
        }

        if (w3 > bestWeight)
        {
            bestWeight = w3;
            representativeVertex = c.Index;
        }

        rawFactureId = GetFactureIdLikeOriginal(map, representativeVertex) & 255;
    }
    else
    {
        int f1 = GetFactureIdLikeOriginal(map, a.Index) & 255;
        int f2 = GetFactureIdLikeOriginal(map, b.Index) & 255;
        int f3 = GetFactureIdLikeOriginal(map, c.Index) & 255;

        if (f1 != 0 && (f1 == f2 || f1 == f3))
        {
            representativeVertex = a.Index;
            rawFactureId = f1;
        }
        else if (f2 != 0 && f2 == f3)
        {
            representativeVertex = b.Index;
            rawFactureId = f2;
        }
        else if (f1 != 0)
        {
            representativeVertex = a.Index;
            rawFactureId = f1;
        }
        else if (f2 != 0)
        {
            representativeVertex = b.Index;
            rawFactureId = f2;
        }
        else if (f3 != 0)
        {
            representativeVertex = c.Index;
            rawFactureId = f3;
        }
    }

    if ((rawFactureId == 0 || representativeVertex < 0) &&
        TryChooseTriangleNeighbourWinnerRawFactureLikeAdapted(map, a, b, c, out int neighbourRawFactureId, out int neighbourRepresentativeVertex))
    {
        rawFactureId = neighbourRawFactureId;
        representativeVertex = neighbourRepresentativeVertex;
    }

    if (representativeVertex < 0)
        representativeVertex = a.Index;

    renderFactureId = ResolveFactureRenderIndexForRawLikeAdapted(map, representativeVertex, rawFactureId, out _, out _, out _);
    return representativeVertex >= 0;
}

private static void AppendTriangleWinnerToBucketsLikeAdapted(
    ParsedMap map,
    CellVertexPayloadLikeOriginal a,
    CellVertexPayloadLikeOriginal b,
    CellVertexPayloadLikeOriginal c,
    Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets)
{
    if (buckets == null || map == null)
        return;

    if (!TryChooseTriangleWinnerRenderFactureIdLikeAdapted(map, a, b, c, out int renderFactureId))
        return;

    int bucketTextureId = renderFactureId & 255;
    bool hasBump = ResolveFactureBumpFlagLikeAdapted(bucketTextureId);

    GetFactureUvwLikeAdapted(map, a.Index, renderFactureId, out Vector2 uvA, out _);
    GetFactureUvwLikeAdapted(map, b.Index, renderFactureId, out Vector2 uvB, out _);
    GetFactureUvwLikeAdapted(map, c.Index, renderFactureId, out Vector2 uvC, out _);

    if (!buckets.TryGetValue(bucketTextureId, out FactureBucketMeshDataLikeAdapted bucket))
    {
        bucket = new FactureBucketMeshDataLikeAdapted(128);
        buckets[bucketTextureId] = bucket;
    }

    bucket.HasBumpContent |= hasBump;

    AppendFactureVertexToBucketLikeAdapted(
        bucket,
        a.World,
        hasBump ? BuildFactureBumpVertexColorLikeAdapted(map, a.Index, 255, bucketTextureId) : BuildFactureVertexColorLikeAdapted(255),
        uvA);

    AppendFactureVertexToBucketLikeAdapted(
        bucket,
        b.World,
        hasBump ? BuildFactureBumpVertexColorLikeAdapted(map, b.Index, 255, bucketTextureId) : BuildFactureVertexColorLikeAdapted(255),
        uvB);

    AppendFactureVertexToBucketLikeAdapted(
        bucket,
        c.World,
        hasBump ? BuildFactureBumpVertexColorLikeAdapted(map, c.Index, 255, bucketTextureId) : BuildFactureVertexColorLikeAdapted(255),
        uvC);
}

private static bool TryBuildTriangleWinnerRecordLikeAdapted(
    ParsedMap map,
    int cellX,
    int cellY,
    bool emitBase,
    CellVertexPayloadLikeOriginal a,
    CellVertexPayloadLikeOriginal b,
    CellVertexPayloadLikeOriginal c,
    out TriangleWinnerRecordLikeAdapted record)
{
    record = default;
    if (map == null)
        return false;

    if (!TryChooseTriangleWinnerRenderFactureIdLikeAdapted(map, a, b, c, out int renderFactureId))
        return false;

    int bucketTextureId = renderFactureId & 255;
    GetFactureUvwLikeAdapted(map, a.Index, renderFactureId, out Vector2 uvA, out _);
    GetFactureUvwLikeAdapted(map, b.Index, renderFactureId, out Vector2 uvB, out _);
    GetFactureUvwLikeAdapted(map, c.Index, renderFactureId, out Vector2 uvC, out _);

    record = new TriangleWinnerRecordLikeAdapted
    {
        CellX = cellX,
        CellY = cellY,
        EmitBase = emitBase,
        WinnerRawFactureId = bucketTextureId,
        RenderFactureId = renderFactureId,
        BucketTextureId = bucketTextureId,
        HasBump = ResolveFactureBumpFlagLikeAdapted(bucketTextureId),
        A = a,
        B = b,
        C = c,
        UvA = uvA,
        UvB = uvB,
        UvC = uvC,
    };
    return true;
}

private static void AppendTriangleWinnerRecordToBucketsLikeAdapted(
    ParsedMap map,
    TriangleWinnerRecordLikeAdapted record,
    Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets)
{
    if (map == null || buckets == null)
        return;

    if (!buckets.TryGetValue(record.BucketTextureId, out FactureBucketMeshDataLikeAdapted bucket))
    {
        bucket = new FactureBucketMeshDataLikeAdapted(128);
        buckets[record.BucketTextureId] = bucket;
    }

    bucket.HasBumpContent |= record.HasBump;

    AppendFactureVertexToBucketLikeAdapted(
        bucket,
        record.A.World,
        record.HasBump ? BuildFactureBumpVertexColorLikeAdapted(map, record.A.Index, 255, record.BucketTextureId) : BuildFactureVertexColorLikeAdapted(255),
        record.UvA);

    AppendFactureVertexToBucketLikeAdapted(
        bucket,
        record.B.World,
        record.HasBump ? BuildFactureBumpVertexColorLikeAdapted(map, record.B.Index, 255, record.BucketTextureId) : BuildFactureVertexColorLikeAdapted(255),
        record.UvB);

    AppendFactureVertexToBucketLikeAdapted(
        bucket,
        record.C.World,
        record.HasBump ? BuildFactureBumpVertexColorLikeAdapted(map, record.C.Index, 255, record.BucketTextureId) : BuildFactureVertexColorLikeAdapted(255),
        record.UvC);
}

private static CellVertexPayloadLikeOriginal GetTriangleWinnerPayloadByIndexLikeAdapted(TriangleWinnerRecordLikeAdapted record, int vertexIndex)
{
    if (record.A.Index == vertexIndex) return record.A;
    if (record.B.Index == vertexIndex) return record.B;
    if (record.C.Index == vertexIndex) return record.C;
    return record.A;
}

private static bool TryGetSharedEdgeBetweenWinnerTrianglesLikeAdapted(
    TriangleWinnerRecordLikeAdapted first,
    TriangleWinnerRecordLikeAdapted second,
    out CellVertexPayloadLikeOriginal shared0,
    out CellVertexPayloadLikeOriginal shared1,
    out CellVertexPayloadLikeOriginal oppositeFirst,
    out CellVertexPayloadLikeOriginal oppositeSecond)
{
    shared0 = default;
    shared1 = default;
    oppositeFirst = default;
    oppositeSecond = default;

    int[] firstIndices = { first.A.Index, first.B.Index, first.C.Index };
    int[] secondIndices = { second.A.Index, second.B.Index, second.C.Index };
    int matchCount = 0;
    int s0 = -1;
    int s1 = -1;

    for (int i = 0; i < firstIndices.Length; i++)
    {
        for (int j = 0; j < secondIndices.Length; j++)
        {
            if (firstIndices[i] == secondIndices[j])
            {
                if (matchCount == 0) s0 = firstIndices[i];
                else if (matchCount == 1) s1 = firstIndices[i];
                matchCount++;
                break;
            }
        }
    }

    if (matchCount != 2 || s0 < 0 || s1 < 0)
        return false;

    shared0 = GetTriangleWinnerPayloadByIndexLikeAdapted(first, s0);
    shared1 = GetTriangleWinnerPayloadByIndexLikeAdapted(first, s1);

    oppositeFirst = first.A.Index != s0 && first.A.Index != s1 ? first.A
        : first.B.Index != s0 && first.B.Index != s1 ? first.B
        : first.C;

    oppositeSecond = second.A.Index != s0 && second.A.Index != s1 ? second.A
        : second.B.Index != s0 && second.B.Index != s1 ? second.B
        : second.C;

    return true;
}

private static Vector2 InterpolateTriangleWinnerUvLikeAdapted(Vector3 position, TriangleWinnerRecordLikeAdapted record)
{
    Vector2 p = new Vector2(position.x, position.z);
    Vector2 a = new Vector2(record.A.World.x, record.A.World.z);
    Vector2 b = new Vector2(record.B.World.x, record.B.World.z);
    Vector2 c = new Vector2(record.C.World.x, record.C.World.z);

    float denom = ((b.y - c.y) * (a.x - c.x)) + ((c.x - b.x) * (a.y - c.y));
    if (Mathf.Abs(denom) < 1e-6f)
        return record.UvA;

    float wA = (((b.y - c.y) * (p.x - c.x)) + ((c.x - b.x) * (p.y - c.y))) / denom;
    float wB = (((c.y - a.y) * (p.x - c.x)) + ((a.x - c.x) * (p.y - c.y))) / denom;
    float wC = 1.0f - wA - wB;

    return record.UvA * wA + record.UvB * wB + record.UvC * wC;
}

private static Color32 BuildSeamTriangleVertexColorLikeAdapted(
    ParsedMap map,
    TriangleWinnerRecordLikeAdapted record,
    Vector3 position,
    int alpha = 255)
{
    if (!record.HasBump || map == null)
        return BuildFactureVertexColorLikeAdapted(alpha);

    float da = (record.A.World - position).sqrMagnitude;
    float db = (record.B.World - position).sqrMagnitude;
    float dc = (record.C.World - position).sqrMagnitude;
    int fallbackIndex = da <= db && da <= dc ? record.A.Index : (db <= dc ? record.B.Index : record.C.Index);
    return BuildFactureBumpVertexColorLikeAdapted(map, fallbackIndex, alpha, record.BucketTextureId);
}

private static void AppendOpaqueSeamTriangleLikeAdapted(
    ParsedMap map,
    Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets,
    TriangleWinnerRecordLikeAdapted record,
    Vector3 p0,
    Vector3 p1,
    Vector3 p2)
{
    Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
    if (cross.sqrMagnitude <= 1e-10f)
        return;

    if (!buckets.TryGetValue(record.BucketTextureId, out FactureBucketMeshDataLikeAdapted bucket))
    {
        bucket = new FactureBucketMeshDataLikeAdapted(64);
        buckets[record.BucketTextureId] = bucket;
    }

    bucket.HasBumpContent |= record.HasBump;

    AppendFactureVertexToBucketLikeAdapted(bucket, p0, BuildSeamTriangleVertexColorLikeAdapted(map, record, p0), InterpolateTriangleWinnerUvLikeAdapted(p0, record));
    AppendFactureVertexToBucketLikeAdapted(bucket, p1, BuildSeamTriangleVertexColorLikeAdapted(map, record, p1), InterpolateTriangleWinnerUvLikeAdapted(p1, record));
    AppendFactureVertexToBucketLikeAdapted(bucket, p2, BuildSeamTriangleVertexColorLikeAdapted(map, record, p2), InterpolateTriangleWinnerUvLikeAdapted(p2, record));
}

private static void AppendSeamQuadLikeAdapted(
    ParsedMap map,
    Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets,
    TriangleWinnerRecordLikeAdapted record,
    Vector3 edge0,
    Vector3 edge1,
    Vector3 outer0,
    Vector3 outer1,
    int alphaAtEdge,
    int alphaAtOuter)
{
    if (map == null || buckets == null)
        return;

    Vector3 crossA = Vector3.Cross(edge1 - edge0, outer1 - edge0);
    Vector3 crossB = Vector3.Cross(outer1 - edge0, outer0 - edge0);
    if (crossA.sqrMagnitude <= 1e-10f || crossB.sqrMagnitude <= 1e-10f)
        return;

    if (!buckets.TryGetValue(record.BucketTextureId, out FactureBucketMeshDataLikeAdapted bucket))
    {
        bucket = new FactureBucketMeshDataLikeAdapted(64);
        buckets[record.BucketTextureId] = bucket;
    }

    bucket.HasBumpContent |= record.HasBump;

    AppendFactureVertexToBucketLikeAdapted(bucket, edge0, BuildSeamTriangleVertexColorLikeAdapted(map, record, edge0, alphaAtEdge), InterpolateTriangleWinnerUvLikeAdapted(edge0, record));
    AppendFactureVertexToBucketLikeAdapted(bucket, edge1, BuildSeamTriangleVertexColorLikeAdapted(map, record, edge1, alphaAtEdge), InterpolateTriangleWinnerUvLikeAdapted(edge1, record));
    AppendFactureVertexToBucketLikeAdapted(bucket, outer1, BuildSeamTriangleVertexColorLikeAdapted(map, record, outer1, alphaAtOuter), InterpolateTriangleWinnerUvLikeAdapted(outer1, record));

    AppendFactureVertexToBucketLikeAdapted(bucket, edge0, BuildSeamTriangleVertexColorLikeAdapted(map, record, edge0, alphaAtEdge), InterpolateTriangleWinnerUvLikeAdapted(edge0, record));
    AppendFactureVertexToBucketLikeAdapted(bucket, outer1, BuildSeamTriangleVertexColorLikeAdapted(map, record, outer1, alphaAtOuter), InterpolateTriangleWinnerUvLikeAdapted(outer1, record));
    AppendFactureVertexToBucketLikeAdapted(bucket, outer0, BuildSeamTriangleVertexColorLikeAdapted(map, record, outer0, alphaAtOuter), InterpolateTriangleWinnerUvLikeAdapted(outer0, record));
}

private static void AppendWinnerEdgePixelExchangeStripLikeAdapted(
    ParsedMap map,
    TriangleWinnerRecordLikeAdapted first,
    TriangleWinnerRecordLikeAdapted second,
    Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets)
{
    if (map == null || buckets == null)
        return;
    if (first.BucketTextureId == second.BucketTextureId && first.RenderFactureId == second.RenderFactureId)
        return;
    if (!TryGetSharedEdgeBetweenWinnerTrianglesLikeAdapted(first, second, out CellVertexPayloadLikeOriginal shared0, out CellVertexPayloadLikeOriginal shared1, out CellVertexPayloadLikeOriginal oppositeFirst, out CellVertexPayloadLikeOriginal oppositeSecond))
        return;

    Vector3 p0 = shared0.World;
    Vector3 p1 = shared1.World;
    float edgeLength = Vector3.Distance(p0, p1);
    if (edgeLength <= 1e-5f)
        return;

    Vector3 bias = new Vector3(0.0f, 0.0035f, 0.0f);
    Vector3 edgeDir = (p1 - p0).normalized;
    Vector3 mid = (p0 + p1) * 0.5f;

    Vector3 rawFirstDir = oppositeFirst.World - mid;
    Vector3 rawSecondDir = oppositeSecond.World - mid;
    rawFirstDir -= edgeDir * Vector3.Dot(rawFirstDir, edgeDir);
    rawSecondDir -= edgeDir * Vector3.Dot(rawSecondDir, edgeDir);
    rawFirstDir.y = 0.0f;
    rawSecondDir.y = 0.0f;

    if (rawFirstDir.sqrMagnitude <= 1e-8f || rawSecondDir.sqrMagnitude <= 1e-8f)
        return;

    Vector3 intoFirst = rawFirstDir.normalized;
    Vector3 intoSecond = rawSecondDir.normalized;
    float avgDepth = 0.5f * (rawFirstDir.magnitude + rawSecondDir.magnitude);
    float stripDepth = Mathf.Clamp(avgDepth * 0.33f, 0.08f, 0.22f);
    int segmentCount = Mathf.Clamp(Mathf.RoundToInt(edgeLength * 6.0f), 10, 28);
    const int alphaAtEdge = 160;
    const int alphaAtOuter = 0;

    for (int segment = 0; segment < segmentCount; segment++)
    {
        float t0 = segment / (float)segmentCount;
        float t1 = (segment + 1) / (float)segmentCount;

        Vector3 edge0 = Vector3.Lerp(p0, p1, t0) + bias;
        Vector3 edge1 = Vector3.Lerp(p0, p1, t1) + bias;

        Vector3 firstOuter0 = edge0 + intoFirst * stripDepth;
        Vector3 firstOuter1 = edge1 + intoFirst * stripDepth;
        Vector3 secondOuter0 = edge0 + intoSecond * stripDepth;
        Vector3 secondOuter1 = edge1 + intoSecond * stripDepth;

        AppendSeamQuadLikeAdapted(map, buckets, first, edge0, edge1, secondOuter0, secondOuter1, alphaAtEdge, alphaAtOuter);
        AppendSeamQuadLikeAdapted(map, buckets, second, edge0, edge1, firstOuter0, firstOuter1, alphaAtEdge, alphaAtOuter);
    }
}


        private static Mesh BuildFactureBucketMeshLikeAdapted(FactureBucketMeshDataLikeAdapted bucket, int stripeIndex, int bucketTextureId)
        {
            if (bucket == null || !bucket.HasContent || bucket.Vertices.Count < 3)
                return null;

            var mesh = new Mesh { name = $"FactureStripe_{stripeIndex:000}_{bucketTextureId:000}" };
            if (bucket.Vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(bucket.Vertices);
            mesh.SetTriangles(bucket.Triangles, 0, true);
            mesh.SetColors(bucket.Colors);
            mesh.SetUVs(0, bucket.Uv0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildFactureStripeMeshLikeAdapted(
            Dictionary<int, FactureBucketMeshDataLikeAdapted> buckets,
            List<int> orderedBucketIds,
            int stripeIndex)
        {
            if (buckets == null || orderedBucketIds == null || orderedBucketIds.Count == 0)
                return null;

            var vertices = new List<Vector3>(1024);
            var colors = new List<Color32>(1024);
            var uv0 = new List<Vector2>(1024);
            var submeshTriangles = new List<List<int>>(orderedBucketIds.Count);

            for (int bucketOrder = 0; bucketOrder < orderedBucketIds.Count; bucketOrder++)
            {
                int bucketId = orderedBucketIds[bucketOrder];
                if (!buckets.TryGetValue(bucketId, out FactureBucketMeshDataLikeAdapted bucket) || bucket == null || !bucket.HasContent || bucket.Vertices.Count < 3)
                {
                    submeshTriangles.Add(new List<int>(0));
                    continue;
                }

                int vertexBase = vertices.Count;
                vertices.AddRange(bucket.Vertices);
                colors.AddRange(bucket.Colors);
                uv0.AddRange(bucket.Uv0);

                var tris = new List<int>(bucket.Triangles.Count);
                for (int i = 0; i < bucket.Triangles.Count; i++)
                    tris.Add(vertexBase + bucket.Triangles[i]);
                submeshTriangles.Add(tris);
            }

            if (vertices.Count < 3)
                return null;

            var mesh = new Mesh { name = $"FactureStripe_{stripeIndex:000}" };
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv0);
            mesh.subMeshCount = submeshTriangles.Count;
            for (int sub = 0; sub < submeshTriangles.Count; sub++)
                mesh.SetTriangles(submeshTriangles[sub], sub, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

private static void AppendFactureTextureCandidateLikeAdapted(List<string> candidates, string candidate)
{
    if (candidates == null || string.IsNullOrWhiteSpace(candidate))
        return;

    string normalized = candidate.Trim().Trim('"', '\'', ' ');
    if (string.IsNullOrWhiteSpace(normalized))
        return;

    normalized = normalized.Replace('/', '\\');
    if (!candidates.Contains(normalized))
        candidates.Add(normalized);
}

private static void AppendFactureMetadataPathCandidatesLikeAdapted(List<string> candidates, string metaPath)
{
    if (candidates == null || string.IsNullOrWhiteSpace(metaPath))
        return;

    string normalized = NormalizeFactureTexturePathLikeAdapted(metaPath);
    if (string.IsNullOrWhiteSpace(normalized))
        return;

    string ext = Path.GetExtension(normalized);
    string baseName = string.Empty;
    try { baseName = Path.GetFileName(normalized); } catch { baseName = string.Empty; }

    AppendFactureTextureCandidateLikeAdapted(candidates, normalized);
    if (!string.IsNullOrWhiteSpace(baseName))
    {
        AppendFactureTextureCandidateLikeAdapted(candidates, baseName);
        AppendFactureTextureCandidateLikeAdapted(candidates, @"Textures\" + baseName);
        AppendFactureTextureCandidateLikeAdapted(candidates, @"textures\" + baseName);
        AppendFactureTextureCandidateLikeAdapted(candidates, @"Textures\ground\" + baseName);
        AppendFactureTextureCandidateLikeAdapted(candidates, @"textures\ground\" + baseName);
    }

    if (string.IsNullOrWhiteSpace(ext))
    {
        AppendFactureTextureCandidateLikeAdapted(candidates, normalized + ".bmp");
        AppendFactureTextureCandidateLikeAdapted(candidates, normalized + ".tga");

        if (!string.IsNullOrWhiteSpace(baseName))
        {
            AppendFactureTextureCandidateLikeAdapted(candidates, baseName + ".bmp");
            AppendFactureTextureCandidateLikeAdapted(candidates, baseName + ".tga");
            AppendFactureTextureCandidateLikeAdapted(candidates, @"Textures\" + baseName + ".bmp");
            AppendFactureTextureCandidateLikeAdapted(candidates, @"Textures\" + baseName + ".tga");
            AppendFactureTextureCandidateLikeAdapted(candidates, @"textures\ground\" + baseName + ".bmp");
            AppendFactureTextureCandidateLikeAdapted(candidates, @"textures\ground\" + baseName + ".tga");
            AppendFactureTextureCandidateLikeAdapted(candidates, @"Textures\ground\" + baseName + ".bmp");
            AppendFactureTextureCandidateLikeAdapted(candidates, @"Textures\ground\" + baseName + ".tga");
        }
    }
}

private string[] BuildFactureTextureCandidatesLikeAdapted(int bucketTextureId, FactureTextureVariantLikeAdapted variant)
{
    int idx = Mathf.Clamp(bucketTextureId, 0, 255);
    FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
    bool bump = variant == FactureTextureVariantLikeAdapted.BumpSource;
    string metaPath = bump ? tables.BumpTexturePath[idx] : tables.DiffuseTexturePath[idx];

    var candidates = new List<string>(4);
    AppendFactureMetadataPathCandidatesLikeAdapted(candidates, metaPath);
    return candidates.ToArray();
}

        private Texture2D TryLoadFactureTextureLikeAdapted(int bucketTextureId, FactureTextureVariantLikeAdapted variant, out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (_bootstrap == null || _bootstrap.Fs == null)
                return null;

            bool bump = variant == FactureTextureVariantLikeAdapted.BumpSource;
            bool plainDiffuse = variant == FactureTextureVariantLikeAdapted.PlainDiffuse;
            bool dot3Diffuse = variant == FactureTextureVariantLikeAdapted.Dot3Diffuse;
            string variantTag = plainDiffuse ? "plain" : (dot3Diffuse ? "dot3" : "bumpSource");
            string cacheKey = $"{_bootstrap.Fs.DataRoot}|{bucketTextureId}|{variantTag}";
            if (s_factureTextureCacheLikeAdapted.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                resolvedPath = cacheKey;
                return cached;
            }

            FactureMaterialTablesLikeAdapted tables = GetFactureMaterialTablesLikeAdapted();
            int idx = Mathf.Clamp(bucketTextureId, 0, 255);
            string metaPath = bump ? tables.BumpTexturePath[idx] : tables.DiffuseTexturePath[idx];
            string[] candidates = BuildFactureTextureCandidatesLikeAdapted(bucketTextureId, variant);
            string logName = bump ? $"FactureBump[{bucketTextureId}]" : (dot3Diffuse ? $"FactureDot3Diffuse[{bucketTextureId}]" : $"FactureDiffuse[{bucketTextureId}]");
            FilterMode filterMode = dot3Diffuse ? FilterMode.Bilinear : FilterMode.Point;
            bool generateMipmaps = plainDiffuse;
            Texture2D tex = TryLoadFactureTextureByCandidatesLikeAdapted(
                _bootstrap.Fs,
                candidates,
                logName,
                TextureWrapMode.Repeat,
                filterMode,
                generateMipmaps,
                out resolvedPath);

            if (tex == null)
            {
                string warningKey = $"{cacheKey}|{metaPath}";
                if (s_factureMetadataWarningsLikeAdapted.Add(warningKey))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[C2:FACT] unresolved {(bump ? "bump" : (dot3Diffuse ? "dot3-diffuse" : "diffuse"))} texture idx={bucketTextureId} source='{tables.SourceKind}' " +
                        $"metaPath='{metaPath}' candidates='{string.Join(" | ", candidates)}'");
                }

                return null;
            }

            s_factureTextureCacheLikeAdapted[cacheKey] = tex;
            return tex;
        }


private static Texture2D TryLoadFactureTextureByCandidatesLikeAdapted(
    Cossacks2Bridge.Core.CoreFileSystem fs,
    string[] candidates,
    string debugName,
    TextureWrapMode wrapMode,
    FilterMode filterMode,
    bool generateMipmaps,
    out string resolvedPath)
{
    resolvedPath = string.Empty;
    if (fs == null || candidates == null || candidates.Length == 0)
        return null;

    if (!C2OriginalImageIO.TryReadImageByCandidates(fs, candidates, out C2OriginalImageData image, out resolvedPath) || image == null)
        return null;

    var tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, generateMipmaps, false);
    tex.name = debugName;
    tex.wrapMode = wrapMode;
    tex.filterMode = generateMipmaps ? FilterMode.Trilinear : filterMode;
    tex.anisoLevel = generateMipmaps ? 8 : 1;
    tex.mipMapBias = generateMipmaps ? -0.25f : 0.0f;
    tex.SetPixels32(image.Pixels);
    tex.Apply(generateMipmaps, false);
    return tex;
}

private Texture2D TryBuildFactureNormalMapLikeAdapted(int bucketTextureId, out string resolvedPath)
{
    resolvedPath = string.Empty;
    if (_bootstrap == null || _bootstrap.Fs == null)
        return null;

    Texture2D bumpSource = TryLoadFactureTextureLikeAdapted(bucketTextureId, FactureTextureVariantLikeAdapted.BumpSource, out string sourcePath);
    if (bumpSource == null)
        return null;

    GetFactureBumpParamsLikeAdapted(bucketTextureId, out float degree, out _, out _);
    string cacheKey = $"{_bootstrap.Fs.DataRoot}|normal|{bucketTextureId}|{sourcePath}|{degree.ToString("0.###", CultureInfo.InvariantCulture)}";
    if (s_factureGeneratedNormalCacheLikeAdapted.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
    {
        resolvedPath = cacheKey;
        return cached;
    }

    Color32[] src = bumpSource.GetPixels32();
    int w = bumpSource.width;
    int h = bumpSource.height;
    if (src == null || src.Length != w * h || w <= 0 || h <= 0)
        return null;

    float scale = Mathf.Max(0.01f, degree * 2.0f);
    Color32[] dst = new Color32[src.Length];

    float SampleHeight(int x, int y)
    {
        x = ((x % w) + w) % w;
        y = ((y % h) + h) % h;
        Color32 c = src[y * w + x];
        return (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255.0f;
    }

    for (int y = 0; y < h; y++)
    {
        for (int x = 0; x < w; x++)
        {
            float hl = SampleHeight(x - 1, y);
            float hr = SampleHeight(x + 1, y);
            float hd = SampleHeight(x, y - 1);
            float hu = SampleHeight(x, y + 1);
            Vector3 n = new Vector3((hl - hr) * scale, (hd - hu) * scale, 1.0f).normalized;
            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255.0f), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255.0f), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255.0f), 0, 255);
            dst[y * w + x] = new Color32(r, g, b, 255);
        }
    }

    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
    tex.SetPixels32(dst);
    tex.Apply(false, false);
    tex.wrapMode = TextureWrapMode.Repeat;
    tex.filterMode = FilterMode.Trilinear;
    tex.anisoLevel = 8;
    tex.mipMapBias = -0.25f;
    tex.name = $"FactureNormal[{bucketTextureId}]";
    s_factureGeneratedNormalCacheLikeAdapted[cacheKey] = tex;
    resolvedPath = cacheKey;
    return tex;
}

private Material GetFactureMaterialLikeAdapted(int bucketTextureId, bool hasBump)
{
    Texture2D bump = null;
    string bumpPath = string.Empty;
    bool useBumpMaterial = false;
    if (hasBump)
    {
        bump = TryBuildFactureNormalMapLikeAdapted(bucketTextureId, out bumpPath);
        useBumpMaterial = bump != null;
    }

    Texture2D diffuse = TryLoadFactureTextureLikeAdapted(bucketTextureId, useBumpMaterial ? FactureTextureVariantLikeAdapted.Dot3Diffuse : FactureTextureVariantLikeAdapted.PlainDiffuse, out string diffusePath);
    if (diffuse == null && bucketTextureId != 0)
        diffuse = TryLoadFactureTextureLikeAdapted(0, useBumpMaterial ? FactureTextureVariantLikeAdapted.Dot3Diffuse : FactureTextureVariantLikeAdapted.PlainDiffuse, out diffusePath);
    if (diffuse == null)
        return null;

    string key = $"{bucketTextureId}|{diffusePath}|{(useBumpMaterial ? bumpPath : "plain")}";
    if (s_factureMaterialCacheLikeAdapted.TryGetValue(key, out Material cached) && cached != null)
        return cached;

    Shader shader = null;
    if (useBumpMaterial)
        shader = Shader.Find("Cossacks2Bridge/TerrainRuntimeFactureDot3");

    if (shader == null)
            shader = Shader.Find("Cossacks2Bridge/TerrainRuntimeFacture3")
                    ?? Shader.Find("Unlit/Texture")
                    ?? Shader.Find("Standard");

    var mat = new Material(shader);
    mat.name = useBumpMaterial ? $"C2_FactureDot3_{bucketTextureId:000}" : $"C2_Facture_{bucketTextureId:000}";
    if (mat.HasProperty("_MainTex"))
        mat.SetTexture("_MainTex", diffuse);
    if (useBumpMaterial && mat.HasProperty("_NormalTex"))
        mat.SetTexture("_NormalTex", bump);
    if (mat.HasProperty("_Color"))
        mat.SetColor("_Color", Color.white);
    if (mat.HasProperty("_FactureTFactor"))
        mat.SetColor("_FactureTFactor", new Color(128.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f));
    if (mat.HasProperty("_UseDitherLikeOriginal"))
        mat.SetFloat("_UseDitherLikeOriginal", 0.0f);
    if (mat.HasProperty("_DitherStrengthLikeOriginal"))
        mat.SetFloat("_DitherStrengthLikeOriginal", 0.0f);
    if (mat.HasProperty("_FactureAlphaRefLikeOriginal"))
        mat.SetFloat("_FactureAlphaRefLikeOriginal", FactureAlphaRefByteLikeOriginal / 255.0f);
    if (mat.HasProperty("_FactureCoverageSoftStartLikeAdapted"))
        mat.SetFloat("_FactureCoverageSoftStartLikeAdapted", FactureCoverageSoftStartLikeAdapted);
    // keep facture layer after the base surface and below future roads / stones / object overrides
    mat.renderQueue = FactureOverlayRenderQueueLikeAdapted;
    s_factureMaterialCacheLikeAdapted[key] = mat;
    return mat;
}

        private static int ComputeFactureOverlaySortingOrderLikeAdapted(int stripeIndex, int bucketOrder)
        {
            return stripeIndex * FactureOverlayStripeSortStepLikeAdapted + bucketOrder;
        }

        private static void ConfigureFactureOverlayRendererLikeAdapted(MeshRenderer renderer, int stripeIndex)
        {
            if (renderer == null)
                return;

            renderer.sortingLayerID = 0;
            renderer.sortingOrder = ComputeFactureOverlaySortingOrderLikeAdapted(stripeIndex, 0);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
        }


private void BuildFactureStripeLayerLikeAdapted(
    ParsedMap map,
    OriginalTerrainKernelConfig kernel,
    int startCellX,
    int endCellX,
    Transform parent,
    int stripeIndex)
{
    if (map == null || parent == null || !HasFactureLayerDataLikeOriginal(map))
        return;

    var buckets = new Dictionary<int, FactureBucketMeshDataLikeAdapted>();
    var winnerTriangles = new List<TriangleWinnerRecordLikeAdapted>(Mathf.Max(64, (endCellX - startCellX + 2) * Mathf.Max(1, kernel.MaxCellYExclusive - kernel.MinCellY) * 2));

    int collectStartX = Mathf.Max(kernel.MinCellX, startCellX - 1);
    int collectEndX = Mathf.Min(kernel.MaxCellXExclusive, endCellX + 1);

    for (int cellY = kernel.MinCellY; cellY < kernel.MaxCellYExclusive; cellY++)
    {
        for (int cellX = collectStartX; cellX < collectEndX; cellX++)
        {
            OriginalCellTriangulationLikeOriginal cell = BuildCellTriangulationLikeOriginal(map, kernel, cellX, cellY);

            CellVertexPayloadLikeOriginal v0 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V0);
            CellVertexPayloadLikeOriginal v1 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V1);
            CellVertexPayloadLikeOriginal v2 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V2);
            CellVertexPayloadLikeOriginal v3 = BuildCellVertexPayloadLikeOriginal(map, kernel, cell, cell.V3);

            bool emitBase = cellX >= startCellX && cellX < endCellX;
            bool isOddCell = cell.FirstA == cell.V0 && cell.FirstB == cell.V1 && cell.FirstC == cell.V2;

            if (isOddCell)
            {
                if (TryBuildTriangleWinnerRecordLikeAdapted(map, cellX, cellY, emitBase, v0, v1, v2, out TriangleWinnerRecordLikeAdapted first))
                    winnerTriangles.Add(first);
                if (TryBuildTriangleWinnerRecordLikeAdapted(map, cellX, cellY, emitBase, v2, v1, v3, out TriangleWinnerRecordLikeAdapted second))
                    winnerTriangles.Add(second);
            }
            else
            {
                if (TryBuildTriangleWinnerRecordLikeAdapted(map, cellX, cellY, emitBase, v0, v1, v3, out TriangleWinnerRecordLikeAdapted first))
                    winnerTriangles.Add(first);
                if (TryBuildTriangleWinnerRecordLikeAdapted(map, cellX, cellY, emitBase, v0, v3, v2, out TriangleWinnerRecordLikeAdapted second))
                    winnerTriangles.Add(second);
            }
        }
    }

    for (int i = 0; i < winnerTriangles.Count; i++)
    {
        if (winnerTriangles[i].EmitBase)
            AppendTriangleWinnerRecordToBucketsLikeAdapted(map, winnerTriangles[i], buckets);
    }

    var edgeMap = new Dictionary<EdgeKeyLikeAdapted, List<int>>(winnerTriangles.Count * 2);
    void AddEdge(int a, int b, int triIndex)
    {
        EdgeKeyLikeAdapted key = new EdgeKeyLikeAdapted(a, b);
        if (!edgeMap.TryGetValue(key, out List<int> list))
        {
            list = new List<int>(2);
            edgeMap[key] = list;
        }

        if (list.Count == 0 || list[list.Count - 1] != triIndex)
            list.Add(triIndex);
    }

    for (int i = 0; i < winnerTriangles.Count; i++)
    {
        TriangleWinnerRecordLikeAdapted tri = winnerTriangles[i];
        AddEdge(tri.A.Index, tri.B.Index, i);
        AddEdge(tri.B.Index, tri.C.Index, i);
        AddEdge(tri.C.Index, tri.A.Index, i);
    }

    foreach (KeyValuePair<EdgeKeyLikeAdapted, List<int>> pair in edgeMap)
    {
        List<int> linked = pair.Value;
        if (linked == null || linked.Count != 2)
            continue;

        TriangleWinnerRecordLikeAdapted first = winnerTriangles[linked[0]];
        TriangleWinnerRecordLikeAdapted second = winnerTriangles[linked[1]];

        int ownerCellX = Mathf.Min(first.CellX, second.CellX);
        if (ownerCellX < startCellX || ownerCellX >= endCellX)
            continue;

        if (first.BucketTextureId == second.BucketTextureId && first.RenderFactureId == second.RenderFactureId)
            continue;

        AppendWinnerEdgePixelExchangeStripLikeAdapted(map, first, second, buckets);
    }

    List<int> orderedBucketIds = new List<int>(buckets.Keys);
    orderedBucketIds.Sort();

    var renderableBucketIds = new List<int>(orderedBucketIds.Count);
    var materials = new List<Material>(orderedBucketIds.Count);
    var skipped = new List<string>(8);

    for (int bucketOrder = 0; bucketOrder < orderedBucketIds.Count; bucketOrder++)
    {
        int bucketId = orderedBucketIds[bucketOrder];
        if (!buckets.TryGetValue(bucketId, out FactureBucketMeshDataLikeAdapted bucket) || bucket == null)
        {
            skipped.Add(bucketId.ToString(CultureInfo.InvariantCulture) + ":missing-bucket");
            continue;
        }

        if (!bucket.HasContent || bucket.Vertices == null || bucket.Vertices.Count < 3 || bucket.Triangles == null || bucket.Triangles.Count < 3)
        {
            skipped.Add(bucketId.ToString(CultureInfo.InvariantCulture) + ":empty");
            continue;
        }

        Material mat = GetFactureMaterialLikeAdapted(bucketId, bucket.HasBumpContent);
        if (mat == null)
        {
            skipped.Add(bucketId.ToString(CultureInfo.InvariantCulture) + ":material-null");
            continue;
        }

        renderableBucketIds.Add(bucketId);
        materials.Add(mat);
    }

    if (renderableBucketIds.Count <= 0 || materials.Count <= 0)
        return;

    Mesh stripeMesh = BuildFactureStripeMeshLikeAdapted(buckets, renderableBucketIds, stripeIndex);
    if (stripeMesh == null || stripeMesh.vertexCount <= 0)
        return;

    if (skipped.Count > 0)
    {
        UnityEngine.Debug.Log(
            $"[C2:FACT] stripe={stripeIndex} fix='triangle-winner-smooth-crossfade-strip-v2' rendered={renderableBucketIds.Count} skipped='{string.Join(",", skipped)}'");
    }

    var go = new GameObject($"FactureStripe_{stripeIndex:000}");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(parent.childCount - 1);
    var mf = go.AddComponent<MeshFilter>();
    var mr = go.AddComponent<MeshRenderer>();
    mf.sharedMesh = stripeMesh;
    mr.sharedMaterials = materials.ToArray();
    ConfigureFactureOverlayRendererLikeAdapted(mr, stripeIndex);
}

private Material CreateTerrainMaterialCoreLikeOriginal(ParsedMap map)
        {
            LogTerrainTexturingBootstrapLikeOriginal(map);

            if (map != null && GetSurfaceModeLikeOriginal(map) == ParsedSurfaceMode.OldSurface && map.HasTilesChunk)
            {
                TerrainTextureTablesLikeOriginal tables = GetTerrainTextureTablesLikeOriginal();
                TerrainTextureResourcesLikeOriginal resources = TryLoadTerrainSurfaceResourcesLikeOriginal();
                if (resources != null && resources.GroundAtlas != null)
                {
                    Shader shader = Shader.Find("Cossacks2Bridge/TerrainRuntimeSurfaceBlendLikeOriginal")
                                    ?? Shader.Find("Cossacks2Bridge/TerrainRuntimeBaseSurfaceAtlas")
                                    ?? Shader.Find("Standard")
                                    ?? Shader.Find("Sprites/Default")
                                    ?? Shader.Find("Unlit/Texture");

                    var mat = new Material(shader);
                    mat.name = "C2_StrictTerrainSurfaceMaterial";

                    if (mat.HasProperty("_GroundAtlas"))
                        mat.SetTexture("_GroundAtlas", resources.GroundAtlas);
                    if (mat.HasProperty("_CrossTex") && resources.CrossTex != null)
                        mat.SetTexture("_CrossTex", resources.CrossTex);
                    if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", resources.GroundAtlas);
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", resources.GroundAtlas);
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", Color.white);
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", Color.white);
                    if (mat.HasProperty("_UseCrossLikeOriginal"))
                        mat.SetFloat("_UseCrossLikeOriginal", resources.CrossTex != null ? 1.0f : 0.0f);
                    if (mat.HasProperty("_UseOverlayLikeOriginal"))
                        mat.SetFloat("_UseOverlayLikeOriginal", map.HasTilesExChunk ? 1.0f : 0.0f);
                    if (mat.HasProperty("_UseDitherLikeOriginal"))
                        mat.SetFloat("_UseDitherLikeOriginal", 0.0f);
                    if (mat.HasProperty("_DitherStrengthLikeOriginal"))
                        mat.SetFloat("_DitherStrengthLikeOriginal", 0.0f);

                    mat.renderQueue = SurfaceBaseRenderQueueLikeAdapted;
                    return mat;
                }

                UnityEngine.Debug.LogWarning("[C2:TEX] GroundTex.bmp was not loaded. Terrain stays on fallback material until atlas decode succeeds.");
            }

            return CreateFallbackTerrainMaterialLikeOriginal();
        }

        private Material CreateFallbackTerrainMaterialLikeOriginal()
        {
            Shader shader = Shader.Find("Unlit/Color")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = "C2_StrictTerrainMaterial";
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            mat.renderQueue = SurfaceBaseRenderQueueLikeAdapted;
            return mat;
        }

        private TerrainTextureResourcesLikeOriginal TryLoadTerrainSurfaceResourcesLikeOriginal()
        {
            if (_bootstrap == null || _bootstrap.Fs == null || string.IsNullOrWhiteSpace(_bootstrap.Fs.DataRoot))
                return null;

            string cacheKey = _bootstrap.Fs.DataRoot;
            if (s_surfaceTextureCacheLikeOriginal.TryGetValue(cacheKey, out TerrainTextureResourcesLikeOriginal cached))
            {
                return cached;
            }

            var resources = new TerrainTextureResourcesLikeOriginal();
            resources.GroundAtlas = TryLoadTextureByCandidatesLikeOriginal(
                _bootstrap.Fs,
                new[]
                {
                    @"Textures\GroundTex.bmp",
                    @"Textures/groundtex.bmp",
                    @"Textures\groundtex.bmp",
                    @"GroundTex.bmp",
                    @"groundtex.bmp"
                },
                "GroundTex.bmp",
                TextureWrapMode.Clamp,
                FilterMode.Point,
                false,
                out resources.GroundAtlasPath);

            resources.CrossTex = TryLoadTextureByCandidatesLikeOriginal(
                _bootstrap.Fs,
                new[]
                {
                    @"BoundNew128.tga",
                    @"boundnew128.tga",
                    @"Textures\BoundNew128.tga",
                    @"Textures/BoundNew128.tga",
                    @"Textures\boundnew128.tga",
                    @"Textures/boundnew128.tga"
                },
                "BoundNew128.tga",
                TextureWrapMode.Repeat,
                FilterMode.Point,
                false,
                out resources.CrossTexPath);

            if (resources.GroundAtlas == null)
                UnityEngine.Debug.LogWarning("[C2:TEX] GroundTex.bmp candidates not found or decode failed.");
            if (resources.CrossTex == null)
                UnityEngine.Debug.LogWarning("[C2:TEX] BoundNew128.tga was not loaded. Overlay will fall back to weight-only blend.");

            s_surfaceTextureCacheLikeOriginal[cacheKey] = resources;
            return resources;
        }

        private static Texture2D TryLoadTextureByCandidatesLikeOriginal(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string[] candidates,
            string debugName,
            TextureWrapMode wrapMode,
            FilterMode filterMode,
            bool generateMipmaps,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (fs == null || candidates == null)
                return null;

            for (int i = 0; i < candidates.Length; i++)
            {
                string rel = candidates[i];
                if (string.IsNullOrWhiteSpace(rel))
                    continue;

                bool exists = fs.Exists(rel);
                if (!exists)
                    continue;

                try
                {
                    byte[] bytes = fs.ReadAllBytes(rel);
                    Texture2D tex = CreateTextureFromBytesLikeOriginal(bytes, rel, generateMipmaps);
                    if (tex != null)
                    {
                        tex.name = debugName;
                        tex.wrapMode = wrapMode;
                        tex.filterMode = generateMipmaps ? FilterMode.Trilinear : filterMode;
                        tex.anisoLevel = generateMipmaps ? 8 : 1;
                        tex.mipMapBias = generateMipmaps ? -0.25f : 0.0f;
                        resolvedPath = rel;
                        return tex;
                    }

                    UnityEngine.Debug.LogWarning($"[C2:TEX] decode returned null path='{rel}'");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[C2:TEX] texture load failed path='{rel}': {ex.GetType().Name}: {ex.Message}");
                }
            }

            return null;
        }

        private static Texture2D CreateTextureFromBytesLikeOriginal(byte[] bytes, string path, bool generateMipmaps)
        {
            if (bytes == null || bytes.Length < 4)
                return null;

            string ext = Path.GetExtension(path) ?? string.Empty;
            if (ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                return TryLoadBmpTextureLikeOriginal(bytes, path, generateMipmaps);
            if (ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                return TryLoadTgaTextureLikeOriginal(bytes, path, generateMipmaps);
            return null;
        }

        private static Texture2D TryLoadBmpTextureLikeOriginal(byte[] bytes, string path, bool generateMipmaps)
        {
            if (bytes.Length < 54 || bytes[0] != (byte)'B' || bytes[1] != (byte)'M')
                return null;

            int pixelOffset = BitConverter.ToInt32(bytes, 10);
            int dibSize = BitConverter.ToInt32(bytes, 14);
            int width = BitConverter.ToInt32(bytes, 18);
            int height = BitConverter.ToInt32(bytes, 22);
            short planes = BitConverter.ToInt16(bytes, 26);
            short bpp = BitConverter.ToInt16(bytes, 28);
            int compression = BitConverter.ToInt32(bytes, 30);
            int colorsUsed = dibSize >= 40 ? BitConverter.ToInt32(bytes, 46) : 0;
            if (dibSize < 40 || planes != 1 || width <= 0 || height == 0 || compression != 0)
                return null;

            int absHeight = Math.Abs(height);
            bool topDown = height < 0;

            if (bpp == 8)
            {
                int paletteEntries = colorsUsed > 0 ? colorsUsed : 256;
                int paletteOffset = 14 + dibSize;
                int paletteSize = paletteEntries * 4;
                if (paletteOffset + paletteSize > bytes.Length || pixelOffset <= 0 || pixelOffset >= bytes.Length)
                    return null;

                var palette = new Color32[paletteEntries];
                for (int i = 0; i < paletteEntries; i++)
                {
                    int p = paletteOffset + i * 4;
                    palette[i] = new Color32(bytes[p + 2], bytes[p + 1], bytes[p + 0], 255);
                }

                int rowStride = (width + 3) & ~3;
                if (pixelOffset + rowStride * absHeight > bytes.Length)
                    return null;

                var pixels = new Color32[width * absHeight];
                for (int y = 0; y < absHeight; y++)
                {
                    int srcY = topDown ? y : (absHeight - 1 - y);
                    int rowOff = pixelOffset + srcY * rowStride;
                    for (int x = 0; x < width; x++)
                    {
                        byte idx = bytes[rowOff + x];
                        pixels[y * width + x] = idx < palette.Length ? palette[idx] : new Color32(255, 0, 255, 255);
                    }
                }

                var tex8 = new Texture2D(width, absHeight, TextureFormat.RGBA32, generateMipmaps, false);
                tex8.SetPixels32(pixels);
                tex8.Apply(generateMipmaps, false);
                return tex8;
            }

            if (bpp != 24 && bpp != 32)
                return null;

            int bytesPerPixel = bpp / 8;
            int rowStride24 = ((width * bytesPerPixel) + 3) & ~3;
            if (pixelOffset <= 0 || pixelOffset + rowStride24 * absHeight > bytes.Length)
                return null;

            var pixels24 = new Color32[width * absHeight];
            for (int y = 0; y < absHeight; y++)
            {
                int srcY = topDown ? y : (absHeight - 1 - y);
                int rowOff = pixelOffset + srcY * rowStride24;
                for (int x = 0; x < width; x++)
                {
                    int i = rowOff + x * bytesPerPixel;
                    byte b = bytes[i + 0];
                    byte g = bytes[i + 1];
                    byte r = bytes[i + 2];
                    byte a = bytesPerPixel >= 4 ? bytes[i + 3] : (byte)255;
                    pixels24[y * width + x] = new Color32(r, g, b, a);
                }
            }

            var tex = new Texture2D(width, absHeight, TextureFormat.RGBA32, generateMipmaps, false);
            tex.SetPixels32(pixels24);
            tex.Apply(generateMipmaps, false);
            return tex;
        }

        private static Texture2D TryLoadTgaTextureLikeOriginal(byte[] bytes, string path, bool generateMipmaps)
        {
            if (bytes.Length < 18)
                return null;

            int idLen = bytes[0];
            int colorMapType = bytes[1];
            int imageType = bytes[2];
            int width = bytes[12] | (bytes[13] << 8);
            int height = bytes[14] | (bytes[15] << 8);
            int bpp = bytes[16];
            int desc = bytes[17];
            if (colorMapType != 0 || imageType != 2 || width <= 0 || height <= 0)
                return null;
            if (bpp != 24 && bpp != 32)
                return null;

            int bytesPerPixel = bpp / 8;
            int header = 18 + idLen;
            int expected = width * height * bytesPerPixel;
            if (header + expected > bytes.Length)
                return null;

            bool originTop = (desc & 0x20) != 0;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int srcY = originTop ? y : (height - 1 - y);
                int dstY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    int srcIndex = header + (srcY * width + x) * bytesPerPixel;
                    byte b = bytes[srcIndex + 0];
                    byte g = bytes[srcIndex + 1];
                    byte r = bytes[srcIndex + 2];
                    byte a = bytesPerPixel >= 4 ? bytes[srcIndex + 3] : (byte)255;
                    pixels[dstY * width + x] = new Color32(r, g, b, a);
                }
            }

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, generateMipmaps, false);
            tex.SetPixels32(pixels);
            tex.Apply(generateMipmaps, false);
            return tex;
        }

        private static bool AppendSurfaceTexturingPayloadForCellLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            KernelStripeData stripe,
            OriginalCellTriangulationLikeOriginal cell,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3)
        {
            if (s_activeTexturingContextLikeOriginal == null)
                return false;

            return s_activeTexturingContextLikeOriginal.AppendSurfaceTexturingPayloadForCellInstanceLikeOriginal(
                map, kernel, stripe, cell, v0, v1, v2, v3);
        }

        private bool AppendSurfaceTexturingPayloadForCellInstanceLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            KernelStripeData stripe,
            OriginalCellTriangulationLikeOriginal cell,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3)
        {
            if (!HasBaseSurfaceTexturingLikeOriginal(map))
                return false;

            TerrainTextureTablesLikeOriginal tables = GetTerrainTextureTablesStaticLikeOriginal();
            bool emittedAny = false;

            CellSurfaceStageLikeOriginal stage;
            if (TryBuildCellStageLikeOriginal(map, cell, true, out stage))
                emittedAny |= EmitExpandedStageForCellLikeOriginal(map, kernel, stripe, tables, cell, v0, v1, v2, v3, stage);

            if (TryBuildCellStageLikeOriginal(map, cell, false, out stage))
                emittedAny |= EmitExpandedStageForCellLikeOriginal(map, kernel, stripe, tables, cell, v0, v1, v2, v3, stage);

            return emittedAny;
        }

        private bool EmitExpandedStageForCellLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            KernelStripeData stripe,
            TerrainTextureTablesLikeOriginal tables,
            OriginalCellTriangulationLikeOriginal cell,
            CellVertexPayloadLikeOriginal v0,
            CellVertexPayloadLikeOriginal v1,
            CellVertexPayloadLikeOriginal v2,
            CellVertexPayloadLikeOriginal v3,
            CellSurfaceStageLikeOriginal stage)
        {
            bool isOddCell = cell.FirstA == cell.V0 && cell.FirstB == cell.V1 && cell.FirstC == cell.V2;
            bool emittedAny = false;

            if (isOddCell)
            {
                emittedAny |= EmitExpandedTriangleForCellStageLikeOriginal(
                    map, kernel, stripe, tables, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddLeft,
                    v0, v1, v2,
                    stage.T0, stage.T1, stage.T2,
                    stage);

                emittedAny |= EmitExpandedTriangleForCellStageLikeOriginal(
                    map, kernel, stripe, tables, cell,
                    BaseSurfaceTriangleKindLikeOriginal.OddRight,
                    v2, v1, v3,
                    stage.T2, stage.T1, stage.T3,
                    stage);
            }
            else
            {
                emittedAny |= EmitExpandedTriangleForCellStageLikeOriginal(
                    map, kernel, stripe, tables, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenUpper,
                    v0, v1, v3,
                    stage.T0, stage.T1, stage.T3,
                    stage);

                emittedAny |= EmitExpandedTriangleForCellStageLikeOriginal(
                    map, kernel, stripe, tables, cell,
                    BaseSurfaceTriangleKindLikeOriginal.EvenLower,
                    v0, v3, v2,
                    stage.T0, stage.T3, stage.T2,
                    stage);
            }

            return emittedAny;
        }

        private bool EmitExpandedTriangleForCellStageLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            KernelStripeData stripe,
            TerrainTextureTablesLikeOriginal tables,
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

            bool emittedAny = false;

            ExpandedTriangleCopyLikeOriginal copy0;
            BuildInitialExpandedTriangleCopyLikeOriginal(kind, cell, tMin, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copy0);
            BaseSurfaceTriangleDescriptorLikeAdapted desc0 = BuildTriangleDescriptorFromCopyLikeAdapted(
                map, tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Primary,
                stage.IsBaseStage,
                a.Index, b.Index, c.Index,
                tA, tB, tC,
                GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                wA, wB, wC,
                copy0);
            emittedAny |= EmitTriangleDescriptorLikeAdapted(
                map, kernel, stripe, tables,
                a.RawX, a.RawZ, a.World,
                b.RawX, b.RawZ, b.World,
                c.RawX, c.RawZ, c.World,
                desc0);

            if (tAve != tMin)
            {
                ExpandedTriangleCopyLikeOriginal copyAve;
                BuildAverageExpandedTriangleCopyLikeOriginal(kind, cell, tMin, tAve, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyAve);
                BaseSurfaceTriangleDescriptorLikeAdapted descAve = BuildTriangleDescriptorFromCopyLikeAdapted(
                    map, tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Average,
                    stage.IsBaseStage,
                    a.Index, b.Index, c.Index,
                    tA, tB, tC,
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                    wA, wB, wC,
                    copyAve);
                emittedAny |= EmitTriangleDescriptorLikeAdapted(
                    map, kernel, stripe, tables,
                    a.RawX, a.RawZ, a.World,
                    b.RawX, b.RawZ, b.World,
                    c.RawX, c.RawZ, c.World,
                    descAve);
            }

            if (tMax != tMin && tMax != tAve)
            {
                ExpandedTriangleCopyLikeOriginal copyMax;
                BuildMaximumExpandedTriangleCopyLikeOriginal(kind, cell, tAve, tMax, stage.IsBaseStage, stage.PlainMode, tA, tB, tC, wA, wB, wC, out copyMax);
                BaseSurfaceTriangleDescriptorLikeAdapted descMax = BuildTriangleDescriptorFromCopyLikeAdapted(
                    map, tables, kind, BaseSurfaceTriangleCopyRoleLikeAdapted.Maximum,
                    stage.IsBaseStage,
                    a.Index, b.Index, c.Index,
                    tA, tB, tC,
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, a.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, b.Index),
                    GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, c.Index),
                    wA, wB, wC,
                    copyMax);
                emittedAny |= EmitTriangleDescriptorLikeAdapted(
                    map, kernel, stripe, tables,
                    a.RawX, a.RawZ, a.World,
                    b.RawX, b.RawZ, b.World,
                    c.RawX, c.RawZ, c.World,
                    descMax);
            }

            return emittedAny;
        }

        private static void BuildSortedTriangleTilesLikeOriginal(BaseSurfaceTriangleKindLikeOriginal kind, int tA, int tB, int tC, out int tMin, out int tAve, out int tMax)
        {
            tMin = tA;
            tAve = tB;
            tMax = tC;
            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    Sort3LLikeOriginal(ref tMin, ref tAve, ref tMax);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    Sort3RLikeOriginal(ref tMin, ref tAve, ref tMax);
                    return;
                default:
                    Sort3LikeOriginal(ref tMin, ref tAve, ref tMax);
                    return;
            }
        }

        private static int GetOverlayRawWeightByteLikeOriginal(ParsedMap map, int vertexIndex)
        {
            if (map == null || !map.HasTilesExChunk || map.WTexMapEx == null || vertexIndex < 0 || vertexIndex >= map.WTexMapEx.Length)
                return 0;
            return map.WTexMapEx[vertexIndex];
        }

        private static bool TryBuildCellStageLikeOriginal(
            ParsedMap map,
            OriginalCellTriangulationLikeOriginal cell,
            bool isBaseStage,
            out CellSurfaceStageLikeOriginal stage)
        {
            stage = new CellSurfaceStageLikeOriginal
            {
                IsBaseStage = isBaseStage,
                PlainMode = false,
                T0 = 0,
                T1 = 0,
                T2 = 0,
                T3 = 0,
                W0 = 0,
                W1 = 0,
                W2 = 0,
                W3 = 0,
            };

            if (map == null)
                return false;

            if (isBaseStage)
            {
                stage.T0 = GetVertexTileLikeOriginal(map.TexMap, cell.V0);
                stage.T1 = GetVertexTileLikeOriginal(map.TexMap, cell.V1);
                stage.T2 = GetVertexTileLikeOriginal(map.TexMap, cell.V2);
                stage.T3 = GetVertexTileLikeOriginal(map.TexMap, cell.V3);

                int rawW0 = GetOverlayRawWeightByteLikeOriginal(map, cell.V0);
                int rawW1 = GetOverlayRawWeightByteLikeOriginal(map, cell.V1);
                int rawW2 = GetOverlayRawWeightByteLikeOriginal(map, cell.V2);
                int rawW3 = GetOverlayRawWeightByteLikeOriginal(map, cell.V3);
                stage.PlainMode = rawW0 > 80 || rawW1 > 80 || rawW2 > 80 || rawW3 > 80;
                // Original Stage 1 contract from Factures3D.cpp:
                // base layer uses TexMap, PLAINMODE is read from WTexMapEx,
                // but coverage itself is forced to full 0xFF.
                stage.W0 = 255;
                stage.W1 = 255;
                stage.W2 = 255;
                stage.W3 = 255;
                return true;
            }

            if (!map.HasTilesExChunk || map.TexMapEx == null || map.WTexMapEx == null)
                return false;

            stage.T0 = GetVertexTileLikeOriginal(map.TexMapEx, cell.V0);
            stage.T1 = GetVertexTileLikeOriginal(map.TexMapEx, cell.V1);
            stage.T2 = GetVertexTileLikeOriginal(map.TexMapEx, cell.V2);
            stage.T3 = GetVertexTileLikeOriginal(map.TexMapEx, cell.V3);
            stage.W0 = ScaleByteLikeOriginal(GetOverlayRawWeightByteLikeOriginal(map, cell.V0));
            stage.W1 = ScaleByteLikeOriginal(GetOverlayRawWeightByteLikeOriginal(map, cell.V1));
            stage.W2 = ScaleByteLikeOriginal(GetOverlayRawWeightByteLikeOriginal(map, cell.V2));
            stage.W3 = ScaleByteLikeOriginal(GetOverlayRawWeightByteLikeOriginal(map, cell.V3));
            if ((stage.W0 | stage.W1 | stage.W2 | stage.W3) == 0)
                return false;

            if (stage.W0 < 1) stage.W0 = 1;
            if (stage.W1 < 1) stage.W1 = 1;
            if (stage.W2 < 1) stage.W2 = 1;
            if (stage.W3 < 1) stage.W3 = 1;
            stage.PlainMode = true;
            return true;
        }

        private static int ResolveCellVertexStageWeightLikeOriginal(
            OriginalCellTriangulationLikeOriginal cell,
            int vertexIndex,
            int w0,
            int w1,
            int w2,
            int w3)
        {
            if (vertexIndex == cell.V0)
                return w0;
            if (vertexIndex == cell.V1)
                return w1;
            if (vertexIndex == cell.V2)
                return w2;
            if (vertexIndex == cell.V3)
                return w3;
            return 1;
        }

        private static float BuildCopyAlphaLikeOriginal(int vertexTile, int copyTile, int baseWeight)
        {
            if (vertexTile != copyTile || baseWeight <= 0)
                return 0.0f;
            return Mathf.Clamp01(baseWeight / 255.0f);
        }

        private static void BuildInitialExpandedTriangleCopyLikeOriginal(
            BaseSurfaceTriangleKindLikeOriginal kind,
            OriginalCellTriangulationLikeOriginal cell,
            int tile,
            bool isBaseStage,
            bool plainMode,
            int tA, int tB, int tC,
            int wA, int wB, int wC,
            out ExpandedTriangleCopyLikeOriginal copy)
        {
            copy = new ExpandedTriangleCopyLikeOriginal
            {
                Tile = (byte)Mathf.Clamp(tile, 0, 63),
                OpaqueBase = isBaseStage,
                PlainMode = plainMode,
            };

            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                    copy.IsLeft = true;
                    copy.Vr = cell.V0;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 71;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 77;
                    copy.AlphaA = BuildCopyAlphaLikeOriginal(tA, tile, wA);
                    copy.AlphaB = BuildCopyAlphaLikeOriginal(tB, tile, wB);
                    copy.AlphaC = BuildCopyAlphaLikeOriginal(tC, tile, wC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                    copy.IsLeft = false;
                    copy.Vr = cell.V1;
                    copy.SeedVertexU = cell.V1;
                    copy.SeedSetU = 73;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 79;
                    copy.AlphaA = BuildCopyAlphaLikeOriginal(tA, tile, wA);
                    copy.AlphaB = BuildCopyAlphaLikeOriginal(tB, tile, wB);
                    copy.AlphaC = BuildCopyAlphaLikeOriginal(tC, tile, wC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    copy.IsLeft = false;
                    copy.Vr = cell.V1;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 47;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 49;
                    copy.AlphaA = BuildCopyAlphaLikeOriginal(tA, tile, wA);
                    copy.AlphaB = BuildCopyAlphaLikeOriginal(tB, tile, wB);
                    copy.AlphaC = BuildCopyAlphaLikeOriginal(tC, tile, wC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    copy.IsLeft = true;
                    copy.Vr = cell.V3;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 111;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 113;
                    copy.AlphaA = BuildCopyAlphaLikeOriginal(tA, tile, wA);
                    copy.AlphaB = BuildCopyAlphaLikeOriginal(tB, tile, wB);
                    copy.AlphaC = BuildCopyAlphaLikeOriginal(tC, tile, wC);
                    return;
                default:
                    copy.AlphaA = copy.AlphaB = copy.AlphaC = 0.0f;
                    return;
            }
        }

        private static void BuildAverageExpandedTriangleCopyLikeOriginal(
            BaseSurfaceTriangleKindLikeOriginal kind,
            OriginalCellTriangulationLikeOriginal cell,
            int prevTile,
            int tile,
            bool isBaseStage,
            bool plainMode,
            int tA, int tB, int tC,
            int wA, int wB, int wC,
            out ExpandedTriangleCopyLikeOriginal copy)
        {
            copy = new ExpandedTriangleCopyLikeOriginal
            {
                Tile = (byte)Mathf.Clamp(tile, 0, 63),
                OpaqueBase = false,
                PlainMode = plainMode,
            };

            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                    copy.IsLeft = true;
                    copy.Vr = cell.V2;
                    copy.SeedVertexU = cell.V3;
                    copy.SeedSetU = 93;
                    copy.SeedVertexV = cell.V3;
                    copy.SeedSetV = 97;
                    break;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                    copy.IsLeft = false;
                    copy.Vr = cell.V1;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 63;
                    copy.SeedVertexV = cell.V1;
                    copy.SeedSetV = 97;
                    break;
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    copy.IsLeft = false;
                    copy.Vr = cell.V3;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 171;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 371;
                    break;
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    copy.IsLeft = true;
                    copy.Vr = cell.V1;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 79;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 39;
                    break;
            }

            copy.AlphaA = BuildCopyAlphaLikeOriginal(tA, tile, wA);
            copy.AlphaB = BuildCopyAlphaLikeOriginal(tB, tile, wB);
            copy.AlphaC = BuildCopyAlphaLikeOriginal(tC, tile, wC);
        }

        private static void BuildMaximumExpandedTriangleCopyLikeOriginal(
            BaseSurfaceTriangleKindLikeOriginal kind,
            OriginalCellTriangulationLikeOriginal cell,
            int prevTile,
            int tile,
            bool isBaseStage,
            bool plainMode,
            int tA, int tB, int tC,
            int wA, int wB, int wC,
            out ExpandedTriangleCopyLikeOriginal copy)
        {
            copy = new ExpandedTriangleCopyLikeOriginal
            {
                Tile = (byte)Mathf.Clamp(tile, 0, 63),
                OpaqueBase = false,
                PlainMode = plainMode,
            };

            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                    copy.IsLeft = true;
                    copy.Vr = cell.V3;
                    copy.SeedVertexU = cell.V1;
                    copy.SeedSetU = 77;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 67;
                    break;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                    copy.IsLeft = false;
                    copy.Vr = cell.V2;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 93;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 61;
                    break;
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    copy.IsLeft = false;
                    copy.Vr = cell.V1;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 711;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 211;
                    break;
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    copy.IsLeft = true;
                    copy.Vr = cell.V2;
                    copy.SeedVertexU = cell.V0;
                    copy.SeedSetU = 29;
                    copy.SeedVertexV = cell.V0;
                    copy.SeedSetV = 37;
                    break;
            }

            copy.AlphaA = BuildCopyAlphaLikeOriginal(tA, tile, wA);
            copy.AlphaB = BuildCopyAlphaLikeOriginal(tB, tile, wB);
            copy.AlphaC = BuildCopyAlphaLikeOriginal(tC, tile, wC);
        }


        private static int ResolveBaseSurfaceRenderTileLikeAdapted(ExpandedTriangleCopyLikeOriginal copy)
        {
            return copy.Tile & 63;
        }

        private static float FilterOverlayAlphaByRoleLikeAdapted(BaseSurfaceTriangleCopyRoleLikeAdapted role, float alpha)
        {
            if (alpha <= 0.0f)
                return 0.0f;

            return Mathf.Clamp01(alpha);
        }

        private static int CountOverlaySupportVerticesLikeAdapted(BaseSurfaceTriangleDescriptorLikeAdapted descriptor, float threshold)
        {
            int count = 0;
            if (descriptor.AlphaA > threshold) count++;
            if (descriptor.AlphaB > threshold) count++;
            if (descriptor.AlphaC > threshold) count++;
            return count;
        }

        private static bool ShouldEmitOverlayDescriptorLikeAdapted(BaseSurfaceTriangleDescriptorLikeAdapted descriptor)
        {
            if (descriptor.IsBaseStage)
                return true;

            // Original-like Stage 2 contract:
            // do not pre-reject transition copies by role/support heuristics.
            // Emit every copy that still carries any non-zero vertex coverage and
            // let the shader/device alpha test decide final visibility.
            return descriptor.AlphaA > 0.0f || descriptor.AlphaB > 0.0f || descriptor.AlphaC > 0.0f;
        }

        private static BaseSurfaceTriangleDescriptorLikeAdapted BuildTriangleDescriptorFromCopyLikeAdapted(
            ParsedMap map,
            TerrainTextureTablesLikeOriginal tables,
            BaseSurfaceTriangleKindLikeOriginal kind,
            BaseSurfaceTriangleCopyRoleLikeAdapted role,
            bool isBaseStage,
            int aIndex,
            int bIndex,
            int cIndex,
            int baseTileA,
            int baseTileB,
            int baseTileC,
            int exTileA,
            int exTileB,
            int exTileC,
            int weightA,
            int weightB,
            int weightC,
            ExpandedTriangleCopyLikeOriginal copy)
        {
            var descriptor = new BaseSurfaceTriangleDescriptorLikeAdapted
            {
                IsBaseStage = isBaseStage,
                Kind = kind,
                Role = role,
                VertexA = aIndex,
                VertexB = bIndex,
                VertexC = cIndex,
                BaseTileA = baseTileA,
                BaseTileB = baseTileB,
                BaseTileC = baseTileC,
                ExTileA = exTileA,
                ExTileB = exTileB,
                ExTileC = exTileC,
                WeightA = weightA,
                WeightB = weightB,
                WeightC = weightC,
                Tile = copy.Tile,
                Vr = copy.Vr,
                IsLeft = copy.IsLeft,
                OpaqueBase = copy.OpaqueBase,
                PlainMode = copy.PlainMode,
                SeedVertexU = copy.SeedVertexU,
                SeedSetU = copy.SeedSetU,
                SeedVertexV = copy.SeedVertexV,
                SeedSetV = copy.SeedSetV,
                AlphaA = isBaseStage ? copy.AlphaA : FilterOverlayAlphaByRoleLikeAdapted(role, copy.AlphaA),
                AlphaB = isBaseStage ? copy.AlphaB : FilterOverlayAlphaByRoleLikeAdapted(role, copy.AlphaB),
                AlphaC = isBaseStage ? copy.AlphaC : FilterOverlayAlphaByRoleLikeAdapted(role, copy.AlphaC),
                ResolvedTile = ResolveBaseSurfaceRenderTileLikeAdapted(copy),
            };
            return descriptor;
        }

        private bool EmitTriangleDescriptorLikeAdapted(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            KernelStripeData stripe,
            TerrainTextureTablesLikeOriginal tables,
            float ax, float az, Vector3 va,
            float bx, float bz, Vector3 vb,
            float cx, float cz, Vector3 vc,
            BaseSurfaceTriangleDescriptorLikeAdapted descriptor)
        {
            if (descriptor.AlphaA <= 0.0f && descriptor.AlphaB <= 0.0f && descriptor.AlphaC <= 0.0f)
                return false;
            if (!ShouldEmitOverlayDescriptorLikeAdapted(descriptor))
                return false;

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

            ApplyGroundAtlasSafetyInsetLikeAdapted(descriptor.ResolvedTile, ref uvA, ref uvB, ref uvC);

            int alphaA = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaA * 255.0f), 0, 255);
            int alphaB = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaB * 255.0f), 0, 255);
            int alphaC = Mathf.Clamp(Mathf.RoundToInt(descriptor.AlphaC * 255.0f), 0, 255);

            Vector2 crossA;
            Vector2 crossB;
            Vector2 crossC;
            BuildCrossTriangleUvForPairLikeOriginal(
                map, kernel, tables,
                descriptor.VertexA, ax, az,
                descriptor.VertexB, bx, bz,
                descriptor.VertexC, cx, cz,
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

            int triBase = stripe.Vertices.Count;
            stripe.Vertices.Add(va);
            stripe.Vertices.Add(vb);
            stripe.Vertices.Add(vc);

            stripe.Colors.Add(ConvertColor32LikeOriginal(colorA));
            stripe.Colors.Add(ConvertColor32LikeOriginal(colorB));
            stripe.Colors.Add(ConvertColor32LikeOriginal(colorC));
            stripe.Uv0.Add(uvA);
            stripe.Uv0.Add(uvB);
            stripe.Uv0.Add(uvC);

            float overlayStageFlag = descriptor.IsBaseStage ? 0.0f : 1.0f;
            bool crossEnabled = !(alphaA > 200 && alphaB > 200 && alphaC > 200 && !descriptor.PlainMode);
            float overlayCrossFlag = crossEnabled ? 1.0f : 0.0f;
            Vector2 stageDescriptor = new Vector2(overlayStageFlag, overlayCrossFlag);
            stripe.Uv1.Add(stageDescriptor);
            stripe.Uv1.Add(stageDescriptor);
            stripe.Uv1.Add(stageDescriptor);

            stripe.Uv2.Add(crossA);
            stripe.Uv2.Add(crossB);
            stripe.Uv2.Add(crossC);
            List<int> targetTriangles = descriptor.IsBaseStage ? stripe.Triangles : stripe.OverlayTriangles;
            targetTriangles.Add(triBase + 0);
            targetTriangles.Add(triBase + 1);
            targetTriangles.Add(triBase + 2);
            return true;
        }

        private void ApplyGroundAtlasSafetyInsetLikeAdapted(int tex, ref Vector2 uvA, ref Vector2 uvB, ref Vector2 uvC)
        {
            TerrainTextureResourcesLikeOriginal resources = TryLoadTerrainSurfaceResourcesLikeOriginal();
            Texture2D atlas = resources != null ? resources.GroundAtlas : null;
            if (atlas == null || atlas.width <= 0 || atlas.height <= 0)
                return;

            float tileMinU = (float)(tex & (GroundAtlasTileCountXLikeOriginal - 1)) / GroundAtlasTileCountXLikeOriginal;
            float tileMinV = (float)(tex / GroundAtlasTileCountXLikeOriginal) / GroundAtlasTileCountYLikeOriginal;
            float tileSizeU = 1.0f / GroundAtlasTileCountXLikeOriginal;
            float tileSizeV = 1.0f / GroundAtlasTileCountYLikeOriginal;

            float tilePixelWidth = Mathf.Max(1.0f, atlas.width / (float)GroundAtlasTileCountXLikeOriginal);
            float tilePixelHeight = Mathf.Max(1.0f, atlas.height / (float)GroundAtlasTileCountYLikeOriginal);
            float localInsetU = 0.5f / tilePixelWidth;
            float localInsetV = 0.5f / tilePixelHeight;

            uvA = RemapGroundAtlasUvToTileCentersLikeAdapted(uvA, tileMinU, tileMinV, tileSizeU, tileSizeV, localInsetU, localInsetV);
            uvB = RemapGroundAtlasUvToTileCentersLikeAdapted(uvB, tileMinU, tileMinV, tileSizeU, tileSizeV, localInsetU, localInsetV);
            uvC = RemapGroundAtlasUvToTileCentersLikeAdapted(uvC, tileMinU, tileMinV, tileSizeU, tileSizeV, localInsetU, localInsetV);
        }

        private static Vector2 RemapGroundAtlasUvToTileCentersLikeAdapted(
            Vector2 uv,
            float tileMinU,
            float tileMinV,
            float tileSizeU,
            float tileSizeV,
            float localInsetU,
            float localInsetV)
        {
            float localU = tileSizeU > 0.0f ? Mathf.Clamp01((uv.x - tileMinU) / tileSizeU) : 0.0f;
            float localV = tileSizeV > 0.0f ? Mathf.Clamp01((uv.y - tileMinV) / tileSizeV) : 0.0f;

            localU = Mathf.Lerp(localInsetU, 1.0f - localInsetU, localU);
            localV = Mathf.Lerp(localInsetV, 1.0f - localInsetV, localV);

            return new Vector2(tileMinU + localU * tileSizeU, tileMinV + localV * tileSizeV);
        }

        private static Color ConvertColor32LikeOriginal(Color32 c)
        {
            return new Color(c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, c.a / 255.0f);
        }

        private static bool HasBaseSurfaceTexturingLikeOriginal(ParsedMap map)
        {
            return map != null
                && GetSurfaceModeLikeOriginal(map) == ParsedSurfaceMode.OldSurface
                && map.HasTilesChunk
                && map.TexMap != null
                && map.TexMap.Length > 0;
        }

        private static int GetVertexTileLikeOriginal(byte[] table, int vertexIndex)
        {
            if (table == null || vertexIndex < 0 || vertexIndex >= table.Length)
                return 0;
            return table[vertexIndex];
        }

        // Factures3D.cpp helpers:
        // void Sort3(int&v1,int&v2,int&v3)
        // void Sort3L(int&v1,int&v2,int&v3)
        // void Sort3R(int&v1,int&v2,int&v3)
        private static void Sort3LikeOriginal(ref int v1, ref int v2, ref int v3)
        {
            int[] v = { -v1, -v2, -v3 };
            bool changed;
            do
            {
                changed = false;
                if (v[0] > v[1])
                {
                    Swap2LikeOriginal(ref v[0], ref v[1]);
                    changed = true;
                }

                if (v[1] > v[2])
                {
                    Swap2LikeOriginal(ref v[1], ref v[2]);
                    changed = true;
                }
            } while (changed);

            v1 = -v[0];
            v2 = -v[1];
            v3 = -v[2];
        }

        private static void Sort3LLikeOriginal(ref int v1, ref int v2, ref int v3)
        {
            Sort3LikeOriginal(ref v1, ref v2, ref v3);
        }

        private static void Sort3RLikeOriginal(ref int v1, ref int v2, ref int v3)
        {
            Sort3LikeOriginal(ref v1, ref v2, ref v3);
        }

        private static void Swap2LikeOriginal(ref int v1, ref int v2)
        {
            v1 = v1 + v2;
            v2 = v1 - v2;
            v1 = v1 - v2;
        }

        // Factures3D.cpp:
        // int GetVValue(int Vertex,int RSet){ return randoma[(Vertex*RSet)&8191]; }
        private static int GetVValueLikeOriginal(int vertex, int rset)
        {
            short[] table = GetRandomTableLikeOriginal();
            return table[(vertex * rset) & 8191];
        }

        private static short[] GetRandomTableLikeOriginal()
        {
            string cacheKey = !string.IsNullOrWhiteSpace(s_randomTableDataRootLikeOriginal)
                ? s_randomTableDataRootLikeOriginal
                : "__fallback__";

            if (s_randomTableCacheLikeOriginal.TryGetValue(cacheKey, out short[] cached) && cached != null && cached.Length == 8192)
                return cached;

            short[] table = TryLoadRandomTableLikeOriginal(cacheKey);
            if (table == null || table.Length != 8192)
                table = BuildFallbackRandomTableLikeOriginal();

            s_randomTableCacheLikeOriginal[cacheKey] = table;
            return table;
        }

        private static short[] TryLoadRandomTableLikeOriginal(string cacheKey)
        {
            string dataRoot = s_randomTableDataRootLikeOriginal;
            if (string.IsNullOrWhiteSpace(dataRoot))
                return null;

            string rootDir = Path.GetDirectoryName(dataRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string[] absoluteCandidates = new[]
            {
                Path.Combine(dataRoot, "random.lst"),
                !string.IsNullOrWhiteSpace(rootDir) ? Path.Combine(rootDir, "random.lst") : null,
                !string.IsNullOrWhiteSpace(rootDir) ? Path.Combine(rootDir, "Data", "random.lst") : null,
            };

            for (int i = 0; i < absoluteCandidates.Length; i++)
            {
                string candidate = absoluteCandidates[i];
                if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
                    continue;

                short[] table = TryDecodeRandomTableLikeOriginal(File.ReadAllBytes(candidate));
                if (table != null)
                    return table;
            }

            C2BattleTerrainMode ctx = s_activeTexturingContextLikeOriginal;
            if (ctx != null && ctx._bootstrap != null && ctx._bootstrap.Fs != null)
            {
                string[] relativeCandidates = new[]
                {
                    "random.lst",
                    @"Data\random.lst",
                };

                for (int i = 0; i < relativeCandidates.Length; i++)
                {
                    string rel = relativeCandidates[i];
                    try
                    {
                        if (!ctx._bootstrap.Fs.Exists(rel))
                            continue;
                        short[] table = TryDecodeRandomTableLikeOriginal(ctx._bootstrap.Fs.ReadAllBytes(rel));
                        if (table != null)
                            return table;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        private static short[] TryDecodeRandomTableLikeOriginal(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8192 * 2)
                return null;

            var table = new short[8192];
            using (var ms = new MemoryStream(bytes, false))
            using (var br = new BinaryReader(ms))
            {
                for (int i = 0; i < table.Length; i++)
                    table[i] = br.ReadInt16();
            }
            return table;
        }

        private static short[] BuildFallbackRandomTableLikeOriginal()
        {
            var table = new short[8192];
            unchecked
            {
                uint state = 0x13579BDFu;
                for (int i = 0; i < table.Length; i++)
                {
                    state = state * 1664525u + 1013904223u;
                    table[i] = (short)(state & 0x7FFFu);
                }
            }
            return table;
        }

        // Factures3D.cpp:
        // float GetBaseU(byte Tex){ return float(Tex&(TEXNX-1))/TEXNX+1.0/TRISCALE; }
        private static float GetBaseULikeOriginal(int tex)
        {
            tex &= 63;
            return (float)(tex & (GroundAtlasTileCountXLikeOriginal - 1)) / GroundAtlasTileCountXLikeOriginal + (1.0f / TriScaleLikeOriginal);
        }

        // Factures3D.cpp:
        // float GetBaseV(byte Tex){ return float(Tex/TEXNY)/TEXNY+1.0/TRISCALE; }
        private static float GetBaseVLikeOriginal(int tex)
        {
            tex &= 63;
            return (float)(tex / GroundAtlasTileCountYLikeOriginal) / GroundAtlasTileCountYLikeOriginal + (1.0f / TriScaleLikeOriginal);
        }

        // Factures3D.cpp style:
        // int Limit / #define SCAL(x) x=Lim(x*6/5)
        private static int LimitLikeOriginal(int value)
        {
            if (value < 0)
                return 0;
            if (value > 255)
                return 255;
            return value;
        }

        private static int ScaleByteLikeOriginal(int value)
        {
            return LimitLikeOriginal((value * 6) / 5);
        }

        private static float GetGroundAtlasBaseULikeOriginal(int tex)
        {
            tex &= 63;
            return GetBaseULikeOriginal(tex);
        }

        private static float GetGroundAtlasBaseVLikeOriginal(int tex)
        {
            tex &= 63;
            return GetBaseVLikeOriginal(tex);
        }

        private float GetBaseVertexUOffsetLikeOriginal(int vertexIndex, int rset)
        {
            return (GetVValueLikeOriginal(vertexIndex, rset) & 31) / (float)TriScaleLikeOriginal;
        }

        private float GetBaseVertexVOffsetLikeOriginal(int vertexIndex, int rset)
        {
            return (GetVValueLikeOriginal(vertexIndex, rset) % VvvLikeOriginal) / (float)TriScaleLikeOriginal;
        }

        private void BuildBaseTriangleUvExplicitLikeOriginal(
            BaseSurfaceTriangleKindLikeOriginal kind,
            int tex,
            int seedVertexU,
            int seedSetU,
            int seedVertexV,
            int seedSetV,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            float dx0 = GetBaseULikeOriginal(tex) + GetBaseVertexUOffsetLikeOriginal(seedVertexU, seedSetU);
            float dy0 = GetBaseVLikeOriginal(tex) + GetBaseVertexVOffsetLikeOriginal(seedVertexV, seedSetV);
            float fTQuant = GroundAtlasTileSpanLikeOriginal;
            float fHQuant = GroundAtlasHalfSpanLikeOriginal;

            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    uvA = new Vector2(dx0, dy0);
                    uvB = new Vector2(dx0 + fTQuant, dy0 + fHQuant);
                    uvC = new Vector2(dx0, dy0 + fTQuant);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    uvA = new Vector2(dx0, dy0 + fHQuant);
                    uvB = new Vector2(dx0 + fTQuant, dy0);
                    uvC = new Vector2(dx0 + fTQuant, dy0 + fTQuant);
                    return;
                default:
                    uvA = Vector2.zero;
                    uvB = Vector2.zero;
                    uvC = Vector2.zero;
                    return;
            }
        }

        private void BuildBaseTriangleUvLikeOriginal(
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            int tex,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                    BuildBaseTriangleUvExplicitLikeOriginal(kind, tex, cell.V0, 71, cell.V0, 77, out uvA, out uvB, out uvC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                    BuildBaseTriangleUvExplicitLikeOriginal(kind, tex, cell.V1, 73, cell.V0, 79, out uvA, out uvB, out uvC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    BuildBaseTriangleUvExplicitLikeOriginal(kind, tex, cell.V0, 47, cell.V0, 49, out uvA, out uvB, out uvC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    BuildBaseTriangleUvExplicitLikeOriginal(kind, tex, cell.V0, 111, cell.V0, 113, out uvA, out uvB, out uvC);
                    return;
                default:
                    uvA = Vector2.zero;
                    uvB = Vector2.zero;
                    uvC = Vector2.zero;
                    return;
            }
        }

        private void BuildCrossTriangleUvForPairLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainTextureTablesLikeOriginal tables,
            int aIndex, float ax, float az,
            int bIndex, float bx, float bz,
            int cIndex, float cx, float cz,
            int vr,
            bool isLeft,
            bool plainMode,
            int alphaA,
            int alphaB,
            int alphaC,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            NormalizeVertexTexCoreLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, vr, isLeft, plainMode, alphaA, alphaB, alphaC, out uvA, out uvB, out uvC);
        }

        private void BuildCrossTriangleUvLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainTextureTablesLikeOriginal tables,
            OriginalCellTriangulationLikeOriginal cell,
            BaseSurfaceTriangleKindLikeOriginal kind,
            int aIndex, float ax, float az,
            int bIndex, float bx, float bz,
            int cIndex, float cx, float cz,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            switch (kind)
            {
                case BaseSurfaceTriangleKindLikeOriginal.OddLeft:
                    BuildCrossTriangleUvForPairLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, cell.V0, true, false, 255, 255, 255, out uvA, out uvB, out uvC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.OddRight:
                    BuildCrossTriangleUvForPairLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, cell.V1, false, false, 255, 255, 255, out uvA, out uvB, out uvC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.EvenUpper:
                    BuildCrossTriangleUvForPairLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, cell.V1, false, false, 255, 255, 255, out uvA, out uvB, out uvC);
                    return;
                case BaseSurfaceTriangleKindLikeOriginal.EvenLower:
                    BuildCrossTriangleUvForPairLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, cell.V3, true, false, 255, 255, 255, out uvA, out uvB, out uvC);
                    return;
                default:
                    uvA = Vector2.zero;
                    uvB = Vector2.zero;
                    uvC = Vector2.zero;
                    return;
            }
        }

        private void NormalizeVertexTexRLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainTextureTablesLikeOriginal tables,
            int aIndex, float ax, float az,
            int bIndex, float bx, float bz,
            int cIndex, float cx, float cz,
            int vr,
            bool plainMode,
            int alphaA,
            int alphaB,
            int alphaC,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            NormalizeVertexTexCoreLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, vr, false, plainMode, alphaA, alphaB, alphaC, out uvA, out uvB, out uvC);
        }

        private void NormalizeVertexTexLLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainTextureTablesLikeOriginal tables,
            int aIndex, float ax, float az,
            int bIndex, float bx, float bz,
            int cIndex, float cx, float cz,
            int vr,
            bool plainMode,
            int alphaA,
            int alphaB,
            int alphaC,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            NormalizeVertexTexCoreLikeOriginal(map, kernel, tables, aIndex, ax, az, bIndex, bx, bz, cIndex, cx, cz, vr, true, plainMode, alphaA, alphaB, alphaC, out uvA, out uvB, out uvC);
        }

        private void NormalizeVertexTexCoreLikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainTextureTablesLikeOriginal tables,
            int aIndex, float ax, float az,
            int bIndex, float bx, float bz,
            int cIndex, float cx, float cz,
            int vr,
            bool isLeft,
            bool plainMode,
            int alphaA,
            int alphaB,
            int alphaC,
            out Vector2 uvA,
            out Vector2 uvB,
            out Vector2 uvC)
        {
            int t1;
            int t2;
            ResolveCrossTexturePairLikeOriginal(map, vr, out t1, out t2);
            t1 &= 63;
            t2 &= 63;

            int crossing = 1;
            if (tables != null)
                crossing = tables.TexCrossing[t1 + (t2 * 64)];

            bool c1 = alphaA > 200;
            bool c2 = alphaB > 200;
            bool c3 = alphaC > 200;
            if (c1 && c2 && c3 && !plainMode)
            {
                uvA = Vector2.zero;
                uvB = Vector2.zero;
                uvC = Vector2.zero;
                return;
            }

            float x0 = (64.0f + (128.0f * (crossing & 1)) - 32.0f + GetCrossJitterLikeAdapted(vr, isLeft, t1, t2, crossing, 0)) * CrossingUvScaleLikeOriginal;
            float y0 = (64.0f + (128.0f * ((crossing >> 1) & 1)) - 32.0f + GetCrossJitterLikeAdapted(vr, isLeft, t1, t2, crossing, 1)) * CrossingUvScaleLikeOriginal;

            Vector2 sa = GetScreenLikeVertexUvSourceLikeOriginal(map, kernel, aIndex, ax, az);
            Vector2 sb = GetScreenLikeVertexUvSourceLikeOriginal(map, kernel, bIndex, bx, bz);
            Vector2 sc = GetScreenLikeVertexUvSourceLikeOriginal(map, kernel, cIndex, cx, cz);

            uvA = new Vector2(x0, y0);
            uvB = new Vector2(x0 + ((sb.x - sa.x) * CrossingUvScaleLikeOriginal), y0 + ((sb.y - sa.y) * CrossingUvScaleLikeOriginal));
            uvC = new Vector2(x0 + ((sc.x - sa.x) * CrossingUvScaleLikeOriginal), y0 + ((sc.y - sa.y) * CrossingUvScaleLikeOriginal));
        }

        private static Vector2 GetScreenLikeVertexUvSourceLikeOriginal(ParsedMap map, OriginalTerrainKernelConfig kernel, int vertexIndex, float rawX, float rawZ)
        {
            // Keep this helper in post-ScShift screen-like units: rawZ already uses quantized backing space,
            // height uses kernel.HeightScale = VerticalScale / (1<<ScShift), and GETYSHIFT stays unshifted like retail.
            short h = (map != null && map.Heights != null && vertexIndex >= 0 && vertexIndex < map.Heights.Length) ? map.Heights[vertexIndex] : (short)0;
            float xShift = GetVertexXShiftLikeOriginal(map, vertexIndex);
            float yShift = GetVertexYShiftLikeOriginal(map, vertexIndex);
            float sx = rawX + xShift;
            float sy = rawZ - (h * kernel.HeightScale) + yShift;
            return new Vector2(sx, sy);
        }

        private static void ResolveCrossTexturePairLikeOriginal(ParsedMap map, int vertexIndex, out int t1, out int t2)
        {
            t1 = GetVertexTileLikeOriginal(map != null ? map.TexMap : null, vertexIndex);
            t2 = GetVertexTileLikeOriginal(map != null ? map.TexMapEx : null, vertexIndex);
        }

        private static uint HashCrossJitterSeedLikeAdapted(int vertex, bool isLeft, int t1, int t2, int crossing, int axis)
        {
            unchecked
            {
                uint h = (uint)vertex;
                h ^= (uint)(t1 & 63) * 0x9E3779B9u;
                h ^= (uint)(t2 & 63) * 0x85EBCA6Bu;
                h ^= (uint)(crossing & 3) * 0xC2B2AE35u;
                h ^= isLeft ? 0x27D4EB2Du : 0x165667B1u;
                h ^= (uint)(axis + 1) * 0x7FEB352Du;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }

        private static float GetCrossJitterLikeAdapted(int vertex, bool isLeft, int t1, int t2, int crossing, int axis)
        {
            return (float)(HashCrossJitterSeedLikeAdapted(vertex, isLeft, t1, t2, crossing, axis) & 63u);
        }

        private TerrainTextureTablesLikeOriginal GetTerrainTextureTablesLikeOriginal()
        {
            if (_bootstrap == null || _bootstrap.Fs == null || string.IsNullOrWhiteSpace(_bootstrap.Fs.DataRoot))
                return null;

            string cacheKey = _bootstrap.Fs.DataRoot;
            if (s_surfaceTablesCacheLikeOriginal.TryGetValue(cacheKey, out TerrainTextureTablesLikeOriginal cached))
                return cached;

            var tables = new TerrainTextureTablesLikeOriginal();
            for (int i = 0; i < tables.TexDiffuse.Length; i++)
                tables.TexDiffuse[i] = new Color32(255, 255, 255, 255);
            for (int i = 0; i < tables.TexCrossing.Length; i++)
                tables.TexCrossing[i] = 1;

            TryLoadTextureTablesFromListLikeOriginal(_bootstrap.Fs, tables);
            s_surfaceTablesCacheLikeOriginal[cacheKey] = tables;
            return tables;
        }

        private static TerrainTextureTablesLikeOriginal GetTerrainTextureTablesStaticLikeOriginal()
        {
            foreach (TerrainTextureTablesLikeOriginal v in s_surfaceTablesCacheLikeOriginal.Values)
                return v;
            return null;
        }

        private static void TryLoadTextureTablesFromListLikeOriginal(Cossacks2Bridge.Core.CoreFileSystem fs, TerrainTextureTablesLikeOriginal tables)
        {
            if (fs == null || tables == null)
                return;
            if (!fs.Exists("textures.lst"))
                return;

            string text;
            try
            {
                text = fs.ReadAllText("textures.lst", Encoding.ASCII);
            }
            catch
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
                return;

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                    continue;

                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                if (string.Equals(parts[0], "#CROSS", StringComparison.OrdinalIgnoreCase) && parts.Length >= 4)
                {
                    if (TryParseIntLikeOriginal(parts[1], out int t1) && TryParseIntLikeOriginal(parts[2], out int t2) && TryParseIntLikeOriginal(parts[3], out int t3))
                    {
                        if (t1 >= 0 && t1 < 64 && t2 >= 0 && t2 < 64 && t3 >= 0 && t3 < 4)
                        {
                            tables.TexCrossing[t1 + t2 * 64] = (byte)t3;
                            tables.TexCrossing[t2 + t1 * 64] = (byte)t3;
                        }
                    }
                    continue;
                }

                if (string.Equals(parts[0], "#CROSSX", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
                {
                    if (TryParseIntLikeOriginal(parts[1], out int t1) && TryParseIntLikeOriginal(parts[2], out int t2))
                    {
                        if (t1 >= 0 && t1 < 64 && t2 >= 0 && t2 < 4)
                        {
                            for (int t = 0; t < 64; t++)
                            {
                                tables.TexCrossing[t + t1 * 64] = (byte)t2;
                                tables.TexCrossing[t1 + t * 64] = (byte)t2;
                            }
                        }
                    }
                    continue;
                }

                if (string.Equals(parts[0], "#COLOR", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
                {
                    if (TryParseIntLikeOriginal(parts[1], out int t) && TryParseHexColorLikeOriginal(parts[2], out Color32 c))
                    {
                        if (t >= 0 && t < 64)
                            tables.TexDiffuse[t] = c;
                    }
                    continue;
                }

                if (string.Equals(parts[0], "#MULTI", StringComparison.OrdinalIgnoreCase) && parts.Length >= 5)
                {
                    if (TryParseIntLikeOriginal(parts[1], out int t1) &&
                        TryParseIntLikeOriginal(parts[2], out int t2) &&
                        TryParseIntLikeOriginal(parts[3], out int t3) &&
                        TryParseIntLikeOriginal(parts[4], out int t4))
                    {
                        if (t1 >= 0 && t1 < 256 && t2 >= 0 && t2 < 256 && t3 >= 0 && t3 < 256 && t4 >= 0 && t4 < 256)
                        {
                            tables.ExtTex[t1, 0] = (byte)t1;
                            tables.ExtTex[t1, 1] = (byte)t2;
                            tables.ExtTex[t1, 2] = (byte)t3;
                            tables.ExtTex[t1, 3] = (byte)t4;
                        }
                    }
                    continue;
                }

                if (string.Equals(parts[0], "#ROAD", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
                {
                    if (TryParseIntLikeOriginal(parts[1], out int t1) && TryParseIntLikeOriginal(parts[2], out int t2))
                    {
                        if (t1 >= 0 && t1 < 256 && t2 >= 0 && t2 < 256)
                            tables.RoadTex[t1] = (byte)t2;
                    }
                    continue;
                }

                if (parts[0].StartsWith("/", StringComparison.Ordinal))
                    continue;

                if (parts.Length >= 2 && TryParseIntLikeOriginal(parts[1], out int nte) && nte >= 0 && nte < 256)
                {
                    string flagsToken = parts.Length >= 3 ? parts[2] : string.Empty;
                    tables.TexFlags[nte] = ParseTextureFlagsLikeOriginal(flagsToken);

                    int mediaMarker = Array.IndexOf(parts, "#");
                    if (mediaMarker >= 0 && mediaMarker + 1 < parts.Length)
                    {
                        int media = ParseTextureMediaLikeAdapted(parts[mediaMarker + 1]);
                        if (media >= 0)
                            tables.TexMedia[nte] = (byte)Mathf.Clamp(media, 0, 255);
                    }
                }
            }
        }

        private static ushort ParseTextureFlagsLikeOriginal(string flagsToken)
        {
            ushort flags = 0;
            if (string.IsNullOrEmpty(flagsToken))
                return flags;

            if (flagsToken.IndexOf('W') >= 0) flags |= TexAlwaysWaterUnlockLikeOriginal;
            if (flagsToken.IndexOf('L') >= 0) flags |= TexAlwaysLandLockLikeOriginal;
            if (flagsToken.IndexOf('U') >= 0) flags |= TexAlwaysLandUnlockLikeOriginal;
            if (flagsToken.IndexOf('P') >= 0) flags |= TexPlainLikeOriginal;
            if (flagsToken.IndexOf('N') >= 0) flags |= TexNormalPutLikeOriginal;
            if (flagsToken.IndexOf('H') >= 0) flags |= TexHardLikeOriginal;
            if (flagsToken.IndexOf('R') >= 0) flags |= TexHardLightLikeOriginal;

            if (flagsToken.IndexOf("G1", StringComparison.OrdinalIgnoreCase) >= 0) flags |= (ushort)(TexGrassLikeOriginal * 1);
            if (flagsToken.IndexOf("G2", StringComparison.OrdinalIgnoreCase) >= 0) flags |= (ushort)(TexGrassLikeOriginal * 2);
            if (flagsToken.IndexOf("G3", StringComparison.OrdinalIgnoreCase) >= 0) flags |= (ushort)(TexGrassLikeOriginal * 4);
            if (flagsToken.IndexOf("G4", StringComparison.OrdinalIgnoreCase) >= 0) flags |= (ushort)(TexGrassLikeOriginal * 8);
            if (flagsToken.IndexOf('S') >= 0) flags |= (ushort)(TexGrassLikeOriginal * 16);
            if (flagsToken.IndexOf('T') >= 0) flags |= (ushort)(TexGrassLikeOriginal * 32);
            if (flagsToken.IndexOf('B') >= 0) flags |= (ushort)(TexGrassLikeOriginal * 64);

            return flags;
        }

        private static int ParseTextureMediaLikeAdapted(string mediaToken)
        {
            if (string.IsNullOrWhiteSpace(mediaToken))
                return -1;

            switch (mediaToken.Trim().ToUpperInvariant())
            {
                case "LAND":
                    return 1;
                case "WATER":
                    return 2;
                case "ROAD":
                    return 3;
                case "MOUNTAIN":
                    return 4;
                default:
                    return -1;
            }
        }

        private static ushort GetTextureFlagsLikeOriginal(TerrainTextureTablesLikeOriginal tables, int tile)
        {
            if (tables == null)
                return 0;
            tile &= 255;
            return tile >= 0 && tile < tables.TexFlags.Length ? tables.TexFlags[tile] : (ushort)0;
        }

        private static bool HasTextureFlagLikeOriginal(TerrainTextureTablesLikeOriginal tables, int tile, ushort flag)
        {
            return (GetTextureFlagsLikeOriginal(tables, tile) & flag) != 0;
        }

        private static int ResolveMultiTextureVariantLikeAdapted(TerrainTextureTablesLikeOriginal tables, int tile, int seedVertexU, int seedSetU, int seedVertexV, int seedSetV)
        {
            int baseTile = tile & 255;
            if (tables == null || baseTile < 0 || baseTile >= 256)
                return tile & 63;

            int v0 = tables.ExtTex[baseTile, 0];
            int v1 = tables.ExtTex[baseTile, 1];
            int v2 = tables.ExtTex[baseTile, 2];
            int v3 = tables.ExtTex[baseTile, 3];
            if (v0 == baseTile && v1 == baseTile && v2 == baseTile && v3 == baseTile)
                return baseTile & 63;

            int seed = (GetVValueLikeOriginal(seedVertexU, seedSetU) ^ (GetVValueLikeOriginal(seedVertexV, seedSetV) << 3) ^ (baseTile << 1)) & 3;
            int selected = tables.ExtTex[baseTile, seed];
            if (selected < 0 || selected >= 64)
                selected = baseTile;
            return selected & 63;
        }

        private static bool TryParseIntLikeOriginal(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseHexColorLikeOriginal(string text, out Color32 color)
        {
            color = new Color32(255, 255, 255, 255);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string hex = text.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            if (hex.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(1);
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint raw))
                return false;

            byte a = (byte)((raw >> 24) & 0xFF);
            byte r = (byte)((raw >> 16) & 0xFF);
            byte g = (byte)((raw >> 8) & 0xFF);
            byte b = (byte)(raw & 0xFF);
            if (hex.Length <= 6)
                a = 255;
            color = new Color32(r, g, b, a);
            return true;
        }
    }
}