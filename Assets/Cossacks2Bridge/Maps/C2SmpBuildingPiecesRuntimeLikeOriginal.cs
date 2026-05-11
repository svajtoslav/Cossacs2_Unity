// C2SmpBuildingPiecesRuntimeLikeOriginal.cs
// V107: V105 async cached bake + original NRG1/1GRN weight-mask crop; no dirty-rect garbage.
// Original chain:
//   NewMonster::PieceName -> RM_GetObjVector(piece) -> RM_LoadNotObj(piece, rx>>4, ry>>4)
// For Unity we apply the SMP vertex texture/facture records into ParsedMap runtime arrays,
// then rebake only affected software-terrain chunks. No preview/ghost terrain writes.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const string C2SmpRuntimeContractV82LikeOriginal = "V108_ASYNC_CACHED_NRG_MASK_ASYNC_OVERLAY_BUFFER";
        private const bool C2SmpRuntimeEnabledV82LikeOriginal = true;
        private const bool C2SmpRuntimeVerboseV82LikeOriginal = false;

        private readonly HashSet<string> _c2SmpRuntimeAppliedKeysV82LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, C2SmpPieceLikeOriginal> _c2SmpRuntimePieceCacheV82LikeOriginal = new Dictionary<string, C2SmpPieceLikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private sealed class C2SmpChunkShadowV87LikeOriginal
        {
            public string Key = string.Empty;
            public int ChunkX;
            public int ChunkY;
            public int Width;
            public int Height;
            public Color32[] Pixels;
            public int Version;
        }

        private readonly Dictionary<string, C2SmpChunkShadowV87LikeOriginal> _c2SmpChunkShadowV87LikeOriginal =
            new Dictionary<string, C2SmpChunkShadowV87LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private ParsedMap _c2SmpChunkShadowMapRefV87LikeOriginal;
        private ParsedMap _c2SmpRuntimeAppliedMapRefV82LikeOriginal;

        private const float C2SmpProgressiveRevealSecondsV84LikeOriginal = 45.0f;
        private const int C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal = 1;
        private const int C2SmpProgressivePixelPaddingV84LikeOriginal = 8;
        private const int C2SmpProgressiveMaxNewPixelsPerFrameV84LikeOriginal = 1024;
        private const float C2SmpProgressiveUploadMinIntervalV84LikeOriginal = 0.08f;
        private const int C2SmpProgressiveTileSizeV87LikeOriginal = 8;
        private const float C2SmpProgressiveFrameWorkBudgetMsV88LikeOriginal = 0.45f;
        private const float C2SmpFrontBottomGateRatioV91LikeOriginal = 0.55f;
        private const float C2SmpFrontBottomGateProgressV91LikeOriginal = 0.70f;
        private const float C2SmpSoftEdgeAlphaSideV91LikeOriginal = 0.88f;
        private const float C2SmpSoftEdgeAlphaCornerV91LikeOriginal = 0.76f;
        private const int C2SmpProgressiveMaxTileUploadsPerFrameV88LikeOriginal = 6;
        private const bool C2SmpWaitAllChunksReadyBeforeRevealV92LikeOriginal = true;
        private readonly Queue<C2SmpPaintJobV84LikeOriginal> _c2SmpPaintJobsV84LikeOriginal = new Queue<C2SmpPaintJobV84LikeOriginal>();
        private Coroutine _c2SmpPaintWorkerV84LikeOriginal;
        private ParsedMap _c2SmpPaintWorkerMapRefV84LikeOriginal;
        private bool _c2SmpFacturePrewarmedForMapV84LikeOriginal;
        private readonly Queue<C2SmpOverlayJobV93LikeOriginal> _c2SmpOverlayJobsV93LikeOriginal = new Queue<C2SmpOverlayJobV93LikeOriginal>();
        private readonly Dictionary<string, C2SmpOverlayInstanceV93LikeOriginal> _c2SmpOverlayInstancesV93LikeOriginal = new Dictionary<string, C2SmpOverlayInstanceV93LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private Coroutine _c2SmpOverlayWorkerV93LikeOriginal;
        private ParsedMap _c2SmpOverlayWorkerMapRefV93LikeOriginal;
        private const float C2SmpOverlayFadeSecondsV93LikeOriginal = 15.0f;
        private const float C2SmpOverlayMaxVisibleAlphaV97LikeOriginal = 1.00f;
        private const float C2SmpOverlayMaskRadiusScaleV98LikeOriginal = 0.60f;
        private const int C2SmpOverlayDeltaThresholdV98LikeOriginal = 18;
        private const int C2SmpOverlayFarWeakDeltaThresholdV98LikeOriginal = 42;
        private const float C2SmpOverlayFarWeakDeltaStartV98LikeOriginal = 0.55f;
        private const bool C2SmpOverlayUseTimeFallbackV93LikeOriginal = true;
        private const float C2SmpOverlayWorldYOffsetV93LikeOriginal = 0.06f;
        private const float C2SmpOverlaySoftMaskPlateauV96LikeOriginal = 0.70f;
        private const float C2SmpOverlaySoftMaskOuterV96LikeOriginal = 1.00f;
        private const int C2SmpOverlaySoftMaskMinAlphaV96LikeOriginal = 0;
        private const float C2SmpOverlayAlphaLogIntervalV102LikeOriginal = 1.50f;
        private const int C2SmpOverlayCropPaddingPixelsV102LikeOriginal = 32;
        private const int C2SmpOverlayWeakChunkSkipMaxAlphaV102LikeOriginal = 64;
        private const int C2SmpOverlayWeakChunkSkipMaxPixelsV102LikeOriginal = 12000;
        private const bool C2SmpOverlayUseNrgWeightMaskV107LikeOriginal = true;
        private const int C2SmpOverlayNrgWeightMinV107LikeOriginal = 1;
        private const int C2SmpOverlayNrgMaskCropPaddingPixelsV107LikeOriginal = 12;
        private const int C2SmpOverlayProbeGridV103LikeOriginal = 3;
        private const int C2SmpOverlayDenseProbeGridV103LikeOriginal = 5;
        private const bool C2SmpOverlayDisableFullMainThreadFallbackV103LikeOriginal = true;


        private const bool C2SmpProfilerEnabledV87LikeOriginal = true;
        private const float C2SmpProfilerLogIntervalV87LikeOriginal = 0.50f;
        private const float C2SmpProfilerSpikeMsV87LikeOriginal = 33.0f;

        private sealed class C2SmpProfilerV87LikeOriginal
        {
            public string PieceName = string.Empty;
            public string MdName = string.Empty;
            public float StartRealtime;
            public float LastLogRealtime;
            public int FrameCount;
            public int Spike33;
            public int Spike50;
            public int Spike100;
            public float MinFps = 999999.0f;
            public float SumFps;
            public float MaxFrameMs;
            public long PrepareInputsMs;
            public long PrewarmMs;
            public long RegionMs;
            public long DirtyRectMs;
            public long GetPixelsMs;
            public long ShadowMs;
            public long AsyncWaitMs;
            public long BakeMs;
            public long FallbackBakeMs;
            public long CopyMs;
            public long RevealMs;
            public long SetPixelsMs;
            public long ApplyMs;
            public long FinalUploadMs;
            public int UploadCalls;
            public int FinalUploadCalls;
            public int RevealedPixels;
            public int DirtyPixelsTotal;
            public int Entries;
            public int TargetFailCount;
        }

        private sealed class C2SmpChunkBakeResultV88LikeOriginal
        {
            public Color32[] TargetPixels;
            public long BakeMs;
            public string Error = string.Empty;
            public int Width;
            public int Height;
        }

        private struct C2SmpDirtyTileV88LikeOriginal
        {
            public int X;
            public int Y;
            public int W;
            public int H;
        }

        private static string C2SmpFpsNowV87LikeOriginal()
        {
            float dt = Time.unscaledDeltaTime > 0.000001f ? Time.unscaledDeltaTime : Time.deltaTime;
            float fps = dt > 0.000001f ? 1.0f / dt : 0.0f;
            float smooth = Time.smoothDeltaTime > 0.000001f ? 1.0f / Time.smoothDeltaTime : fps;
            float frameMs = dt * 1000.0f;
            return " fps=" + fps.ToString("0.0", CultureInfo.InvariantCulture) +
                   " smoothFps=" + smooth.ToString("0.0", CultureInfo.InvariantCulture) +
                   " frameMs=" + frameMs.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static void C2SmpProfilerSampleFrameV87LikeOriginal(C2SmpProfilerV87LikeOriginal prof)
        {
            if (prof == null)
                return;

            float dt = Time.unscaledDeltaTime > 0.000001f ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0.000001f)
                return;

            float fps = 1.0f / dt;
            float frameMs = dt * 1000.0f;
            prof.FrameCount++;
            prof.SumFps += fps;
            prof.MinFps = Mathf.Min(prof.MinFps, fps);
            prof.MaxFrameMs = Mathf.Max(prof.MaxFrameMs, frameMs);
            if (frameMs >= 33.0f) prof.Spike33++;
            if (frameMs >= 50.0f) prof.Spike50++;
            if (frameMs >= 100.0f) prof.Spike100++;
        }

        private static string C2SmpProfilerSummaryV87LikeOriginal(C2SmpProfilerV87LikeOriginal prof)
        {
            if (prof == null)
                return string.Empty;

            float avgFps = prof.FrameCount > 0 ? prof.SumFps / prof.FrameCount : 0.0f;
            float minFps = prof.MinFps < 999998.0f ? prof.MinFps : 0.0f;
            return " fpsAvg=" + avgFps.ToString("0.0", CultureInfo.InvariantCulture) +
                   " fpsMin=" + minFps.ToString("0.0", CultureInfo.InvariantCulture) +
                   " maxFrameMs=" + prof.MaxFrameMs.ToString("0.0", CultureInfo.InvariantCulture) +
                   " spikes33=" + prof.Spike33.ToString(CultureInfo.InvariantCulture) +
                   " spikes50=" + prof.Spike50.ToString(CultureInfo.InvariantCulture) +
                   " spikes100=" + prof.Spike100.ToString(CultureInfo.InvariantCulture) +
                   " frames=" + prof.FrameCount.ToString(CultureInfo.InvariantCulture);
        }


        private sealed class C2SmpPieceLikeOriginal
        {
            public string SourcePath = string.Empty;
            public string PieceName = string.Empty;
            public readonly List<C2SmpVertexRecordLikeOriginal> Vertices = new List<C2SmpVertexRecordLikeOriginal>();
            public readonly List<C2SmpObjAnchorLikeOriginal> Objects = new List<C2SmpObjAnchorLikeOriginal>();
            public bool HasNrg1;
            public int NrgNx;
            public int NrgNy;
            public short NrgDx;
            public short NrgDy;
            public short NrgDx0;
            public short NrgDy0;
            public int NrgPointCount;
            public int NrgActivePointCount;
            public C2SmpGroundPointLikeOriginal[] NrgPoints;
            public bool HasPix1;
            public int PixSquareCount;
            public short MinVertexX;
            public short MaxVertexX;
            public short MinVertexY;
            public short MaxVertexY;
        }

        private struct C2SmpVertexRecordLikeOriginal
        {
            public short X;
            public short Y;
            public byte Tex;
            public byte Facture;
            public byte FactureWeight;
            public byte ExtraTex;
            public byte ExtraWeight;
            public byte S1;
            public byte S2;
            public byte S3;
            public short Height;
        }

        private struct C2SmpGroundPointLikeOriginal
        {
            public byte Weight;
            public ushort TexIndex;
            public short Z;
        }

        private struct C2SmpObjAnchorLikeOriginal
        {
            public int X;
            public int Y;
            public byte Nation;
            public string Name;
        }

        private sealed class C2SmpPaintJobV84LikeOriginal
        {
            public string PieceName = string.Empty;
            public string MdName = string.Empty;
            public string Source = string.Empty;
            public int DirtyMinCellX;
            public int DirtyMinCellY;
            public int DirtyMaxCellXExclusive;
            public int DirtyMaxCellYExclusive;
            public int ChangedVertices;
            public float CreatedRealtime;
        }

        private sealed class C2SmpOverlayJobV93LikeOriginal
        {
            public string PieceName = string.Empty;
            public string MdName = string.Empty;
            public string Source = string.Empty;
            public int DirtyMinCellX;
            public int DirtyMinCellY;
            public int DirtyMaxCellXExclusive;
            public int DirtyMaxCellYExclusive;
            public int ChangedVertices;
            public string ApplyKey = string.Empty;
            public C2SmpPieceLikeOriginal Piece;
            public float CreatedRealtime;
        }

        private sealed class C2SmpOverlayChunkBuildV93LikeOriginal
        {
            public string ChunkName = string.Empty;
            public int ChunkX;
            public int ChunkY;
            public Bounds WorldBounds;
            public Bounds FullWorldBounds;
            public Bounds JobWorldBounds;
            public int Width;
            public int Height;
            public int FullWidth;
            public int FullHeight;
            public int OffsetPixelsX;
            public int OffsetPixelsY;
            public Mesh SourceMesh;
            public Vector3 SourceLocalPosition;
            public Quaternion SourceLocalRotation;
            public Vector3 SourceLocalScale;
            public Color32[] BasePixels;
            public Color32[] TargetPixels;
            public C2SmpPieceLikeOriginal Piece;
            public long BakeMs;
            public string Error = string.Empty;
        }


        private sealed class C2SmpOverlayTextureBuildResultV108LikeOriginal
        {
            public bool HasTexture;
            public string SkipReason = string.Empty;
            public Color32[] Pixels;
            public int CropW;
            public int CropH;
            public int CropX;
            public int CropY;
            public float CropMinU;
            public float CropMaxU;
            public float CropMinV;
            public float CropMaxV;
            public Bounds CropWorldBounds;
            public int FullW;
            public int FullH;
            public int ActivePixels;
            public int MaxAlpha;
            public int FullAlphaPixels;
            public int EdgeFadePixels;
            public int ZeroByMask;
            public int ZeroByDelta;
            public int ZeroByFarWeak;
            public int ZeroByNrgMask;
            public bool UseNrgMask;
            public float Plateau;
            public float RadiusScale;
            public int DeltaThreshold;
            public int FarWeakThreshold;
            public float CenterX;
            public float CenterZ;
            public float RadiusX;
            public float RadiusZ;
            public long BuildMs;
            public string Error = string.Empty;
        }

        private sealed class C2SmpOverlayChunkInstanceV93LikeOriginal
        {
            public string ChunkName = string.Empty;
            public GameObject GameObject;
            public MeshRenderer Renderer;
            public Material Material;
            public Texture2D Texture;
            public float CurrentAlpha;
        }

        private sealed class C2SmpOverlayInstanceV93LikeOriginal
        {
            public string Key = string.Empty;
            public string PieceName = string.Empty;
            public string MdName = string.Empty;
            public string Source = string.Empty;
            public GameObject Root;
            public readonly List<C2SmpOverlayChunkInstanceV93LikeOriginal> Chunks = new List<C2SmpOverlayChunkInstanceV93LikeOriginal>();
            public float CurrentAlpha;
            public float FadeSeconds;
            public float StartRealtime;
            public Coroutine FadeCoroutine;
        }

        private sealed class C2SmpPaintChunkEntryV84LikeOriginal
        {
            public string ChunkName = string.Empty;
            public int ChunkX;
            public int ChunkY;
            public Texture2D Texture;
            public C2SmpChunkShadowV87LikeOriginal Shadow;
            public int OffsetPixelsX;
            public int OffsetPixelsY;
            public int WidthPixels;
            public int HeightPixels;
            public Color32[] BasePixels;
            public Color32[] TargetPixels;
            public Color32[] WorkingPixels;
            public bool[] RevealedTiles;
            public int TileCols;
            public int TileRows;
            public float CenterX;
            public float CenterY;
            public float MaxDistance;
            public float NoiseSeed;
            public float LastUploadTime;
            public int RevealedCount;
            public int DirtyUploadMinX;
            public int DirtyUploadMinY;
            public int DirtyUploadMaxXExclusive;
            public int DirtyUploadMaxYExclusive;
            public List<C2SmpDirtyTileV88LikeOriginal> DirtyTiles = new List<C2SmpDirtyTileV88LikeOriginal>();
            public List<int> FrontierTiles = new List<int>();
            public bool[] FrontierMask;
            public bool FrontierInitialized;
            public bool[] ActiveTiles;
            public int ActiveTileCount;
            public int ActivePixelEstimate;
            public byte[] TileRevealAlpha;
            public int ActiveMinTileX = int.MaxValue;
            public int ActiveMinTileY = int.MaxValue;
            public int ActiveMaxTileXExclusive = int.MinValue;
            public int ActiveMaxTileYExclusive = int.MinValue;
            public int RawDirtyPixelCount;
            public Task<C2SmpChunkBakeResultV88LikeOriginal> PendingBakeTask;
            public bool Ready;
            public bool Failed;
            public string PendingBakeError = string.Empty;
        }


        private static string C2SmpChunkKeyV87LikeOriginal(int chunkX, int chunkY)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:00}_{1:00}", chunkX, chunkY);
        }

        private void C2SmpRememberTerrainChunkShadowV87LikeOriginal(int chunkX, int chunkY, Color32[] pixels, int width, int height)
        {
            if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0 || pixels.Length != width * height)
                return;

            if (!object.ReferenceEquals(_c2SmpChunkShadowMapRefV87LikeOriginal, _map))
            {
                _c2SmpChunkShadowV87LikeOriginal.Clear();
                _c2SmpChunkShadowMapRefV87LikeOriginal = _map;
            }

            string key = C2SmpChunkKeyV87LikeOriginal(chunkX, chunkY);
            C2SmpChunkShadowV87LikeOriginal shadow = new C2SmpChunkShadowV87LikeOriginal();
            shadow.Key = key;
            shadow.ChunkX = chunkX;
            shadow.ChunkY = chunkY;
            shadow.Width = width;
            shadow.Height = height;
            shadow.Pixels = (Color32[])pixels.Clone();
            shadow.Version = 1;
            _c2SmpChunkShadowV87LikeOriginal[key] = shadow;
        }

        private bool C2SmpTryGetTerrainChunkShadowV87LikeOriginal(int chunkX, int chunkY, int width, int height, out C2SmpChunkShadowV87LikeOriginal shadow)
        {
            shadow = null;
            if (!object.ReferenceEquals(_c2SmpChunkShadowMapRefV87LikeOriginal, _map))
            {
                _c2SmpChunkShadowV87LikeOriginal.Clear();
                _c2SmpChunkShadowMapRefV87LikeOriginal = _map;
                return false;
            }

            string key = C2SmpChunkKeyV87LikeOriginal(chunkX, chunkY);
            if (!_c2SmpChunkShadowV87LikeOriginal.TryGetValue(key, out shadow))
                return false;
            if (shadow == null || shadow.Pixels == null || shadow.Width != width || shadow.Height != height || shadow.Pixels.Length != width * height)
            {
                _c2SmpChunkShadowV87LikeOriginal.Remove(key);
                shadow = null;
                return false;
            }
            return true;
        }

        private static void C2SmpCopySubRectV87LikeOriginal(Color32[] src, int srcWidth, int srcHeight, int sx, int sy, int w, int h, Color32[] dst)
        {
            if (src == null || dst == null || srcWidth <= 0 || srcHeight <= 0 || w <= 0 || h <= 0)
                return;
            for (int y = 0; y < h; y++)
            {
                int srcRow = (sy + y) * srcWidth + sx;
                int dstRow = y * w;
                if (srcRow < 0 || srcRow + w > src.Length || dstRow < 0 || dstRow + w > dst.Length)
                    continue;
                Array.Copy(src, srcRow, dst, dstRow, w);
            }
        }

        private static void C2SmpWriteSubRectToShadowV87LikeOriginal(C2SmpChunkShadowV87LikeOriginal shadow, int sx, int sy, int w, int h, Color32[] src)
        {
            if (shadow == null || shadow.Pixels == null || src == null || w <= 0 || h <= 0)
                return;
            for (int y = 0; y < h; y++)
            {
                int dstRow = (sy + y) * shadow.Width + sx;
                int srcRow = y * w;
                if (dstRow < 0 || dstRow + w > shadow.Pixels.Length || srcRow < 0 || srcRow + w > src.Length)
                    continue;
                Array.Copy(src, srcRow, shadow.Pixels, dstRow, w);
            }
            shadow.Version++;
        }

        private static Color32[] C2SmpExtractSubRectV87LikeOriginal(Color32[] src, int srcWidth, int srcHeight, int sx, int sy, int w, int h)
        {
            Color32[] dst = new Color32[Mathf.Max(1, w * h)];
            C2SmpCopySubRectV87LikeOriginal(src, srcWidth, srcHeight, sx, sy, w, h, dst);
            return dst;
        }

        private static Color32[] C2SmpBuildWorkingUploadRectV87LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, int x0, int y0, int x1, int y1)
        {
            int w = Mathf.Max(0, x1 - x0);
            int h = Mathf.Max(0, y1 - y0);
            if (entry == null || entry.WorkingPixels == null || w <= 0 || h <= 0)
                return null;
            Color32[] dst = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int srcRow = (y0 + y) * entry.WidthPixels + x0;
                int dstRow = y * w;
                if (srcRow < 0 || srcRow + w > entry.WorkingPixels.Length)
                    continue;
                Array.Copy(entry.WorkingPixels, srcRow, dst, dstRow, w);
            }
            return dst;
        }

        private static void C2SmpClearDirtyUploadV87LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry)
        {
            if (entry == null)
                return;
            entry.DirtyUploadMinX = int.MaxValue;
            entry.DirtyUploadMinY = int.MaxValue;
            entry.DirtyUploadMaxXExclusive = int.MinValue;
            entry.DirtyUploadMaxYExclusive = int.MinValue;
            if (entry.DirtyTiles != null)
                entry.DirtyTiles.Clear();
        }

        private static void C2SmpMarkDirtyUploadV87LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, int x0, int y0, int x1, int y1)
        {
            if (entry == null)
                return;
            x0 = Mathf.Clamp(x0, 0, entry.WidthPixels);
            y0 = Mathf.Clamp(y0, 0, entry.HeightPixels);
            x1 = Mathf.Clamp(x1, 0, entry.WidthPixels);
            y1 = Mathf.Clamp(y1, 0, entry.HeightPixels);
            if (x1 <= x0 || y1 <= y0)
                return;
            entry.DirtyUploadMinX = Mathf.Min(entry.DirtyUploadMinX, x0);
            entry.DirtyUploadMinY = Mathf.Min(entry.DirtyUploadMinY, y0);
            entry.DirtyUploadMaxXExclusive = Mathf.Max(entry.DirtyUploadMaxXExclusive, x1);
            entry.DirtyUploadMaxYExclusive = Mathf.Max(entry.DirtyUploadMaxYExclusive, y1);
            if (entry.DirtyTiles != null)
            {
                C2SmpDirtyTileV88LikeOriginal r;
                r.X = x0; r.Y = y0; r.W = x1 - x0; r.H = y1 - y0;
                entry.DirtyTiles.Add(r);
            }
        }

        private static bool C2SmpHasDirtyUploadV87LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry)
        {
            return entry != null && entry.DirtyTiles != null && entry.DirtyTiles.Count > 0;
        }

        private static Color32[] C2SmpBuildWorkingUploadTileV88LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, C2SmpDirtyTileV88LikeOriginal tile)
        {
            return C2SmpBuildWorkingUploadRectV87LikeOriginal(entry, tile.X, tile.Y, tile.X + tile.W, tile.Y + tile.H);
        }


        private void C2SmpRuntimeApplyBuildingPieceFromMdLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record record,
            int realX,
            int realY,
            string source)
        {
            if (!C2SmpRuntimeEnabledV82LikeOriginal)
                return;
            if (md == null || string.IsNullOrWhiteSpace(md.PieceName))
                return;
            if (_map == null || !_terrainBuilt || _terrainRoot == null)
            {
                if (C2SmpRuntimeVerboseV82LikeOriginal)
                    Debug.LogWarning("[C2:SMP V93 SKIP] piece='" + md.PieceName + "' reason=no_runtime_terrain source='" + (source ?? string.Empty) + "'");
                return;
            }

            if (!object.ReferenceEquals(_c2SmpRuntimeAppliedMapRefV82LikeOriginal, _map))
            {
                _c2SmpRuntimeAppliedMapRefV82LikeOriginal = _map;
                _c2SmpRuntimeAppliedKeysV82LikeOriginal.Clear();
            }

            string pieceName = md.PieceName.Trim();
            string applyKey = (_mapRelativePath ?? string.Empty) + "|" + pieceName + "|" +
                              (realX >> 10).ToString(CultureInfo.InvariantCulture) + "|" +
                              (realY >> 10).ToString(CultureInfo.InvariantCulture);

            // Re-applying the same SMP at the same 1024-real bucket is visually idempotent.
            // Keep it one-shot to avoid repeated chunk rebakes when multiple builders touch the same site.
            if (_c2SmpRuntimeAppliedKeysV82LikeOriginal.Contains(applyKey))
                return;
            _c2SmpRuntimeAppliedKeysV82LikeOriginal.Add(applyKey);

            C2SmpPieceLikeOriginal piece;
            string loadAudit;
            if (!C2SmpTryLoadPieceLikeOriginal(pieceName, out piece, out loadAudit))
            {
                Debug.LogWarning("[C2:SMP V93 MISS] piece='" + pieceName + "' md='" + (md.MdName ?? string.Empty) +
                                 "' audit='" + loadAudit + "' source='" + (source ?? string.Empty) + "'");
                return;
            }

            int changedVertices;
            int dirtyMinCellX;
            int dirtyMinCellY;
            int dirtyMaxCellXExclusive;
            int dirtyMaxCellYExclusive;

            bool changed = C2SmpApplyVertexRecordsToRuntimeMapLikeOriginal(
                piece,
                realX,
                realY,
                out changedVertices,
                out dirtyMinCellX,
                out dirtyMinCellY,
                out dirtyMaxCellXExclusive,
                out dirtyMaxCellYExclusive);

            if (!changed)
            {
                Debug.LogWarning("[C2:SMP V93 SKIP] piece='" + pieceName + "' md='" + (md.MdName ?? string.Empty) +
                                 "' reason=no_changed_vertices audit='" + loadAudit + "' source='" + (source ?? string.Empty) + "'");
                return;
            }

            int rebakedChunks;
            string rebakeAudit;
            C2SmpQueueOverlayJobV93LikeOriginal(
                applyKey,
                piece,
                pieceName,
                md.MdName,
                source,
                changedVertices,
                dirtyMinCellX,
                dirtyMinCellY,
                dirtyMaxCellXExclusive,
                dirtyMaxCellYExclusive,
                out rebakedChunks,
                out rebakeAudit);

            if (C2SmpRuntimeVerboseV82LikeOriginal)
            {
                string anchor = piece.Objects.Count > 0
                    ? (piece.Objects[0].Name + "@" + piece.Objects[0].X.ToString(CultureInfo.InvariantCulture) + "/" + piece.Objects[0].Y.ToString(CultureInfo.InvariantCulture))
                    : "none";

                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V107 APPLY] contract=" + C2SmpRuntimeContractV82LikeOriginal +
                          " md='" + (md.MdName ?? string.Empty) + "'" +
                          " piece='" + pieceName + "'" +
                          " path='" + (piece.SourcePath ?? string.Empty) + "'" +
                          " anchor='" + anchor + "'" +
                          " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " baseVertex=(" + (((realX >> 10) << 1)).ToString(CultureInfo.InvariantCulture) + "," + (((realY >> 10) << 1)).ToString(CultureInfo.InvariantCulture) + ")" +
                          " vertices=" + piece.Vertices.Count.ToString(CultureInfo.InvariantCulture) +
                          " changedVertices=" + changedVertices.ToString(CultureInfo.InvariantCulture) +
                          " dirtyCells=" + dirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + dirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                          "-" + dirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + dirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                          " queuedChunks=" + rebakedChunks.ToString(CultureInfo.InvariantCulture) +
                          " nrg=" + (piece.HasNrg1 ? (piece.NrgNx.ToString(CultureInfo.InvariantCulture) + "x" + piece.NrgNy.ToString(CultureInfo.InvariantCulture)) : "none") +
                          " pixSquares=" + piece.PixSquareCount.ToString(CultureInfo.InvariantCulture) +
                          " loadAudit='" + loadAudit + "'" +
                          " rebakeAudit='" + rebakeAudit + "'" +
                          " source='" + (source ?? string.Empty) + "'");
            }
        }

        private bool C2SmpApplyVertexRecordsToRuntimeMapLikeOriginal(
            C2SmpPieceLikeOriginal piece,
            int realX,
            int realY,
            out int changedVertices,
            out int dirtyMinCellX,
            out int dirtyMinCellY,
            out int dirtyMaxCellXExclusive,
            out int dirtyMaxCellYExclusive)
        {
            changedVertices = 0;
            dirtyMinCellX = int.MaxValue;
            dirtyMinCellY = int.MaxValue;
            dirtyMaxCellXExclusive = int.MinValue;
            dirtyMaxCellYExclusive = int.MinValue;

            if (piece == null || piece.Vertices.Count == 0 || _map == null)
                return false;

            EnsureTerrainTileArraysForSmpLikeOriginal(_map);
            EnsureFactureMapsLikeOriginal(_map);

            int maxPointIndex = GetMaxPointIndexLikeOriginal(_map);
            if (maxPointIndex <= 0)
                return false;

            // Original RM_LoadNotObj receives (rx>>4, ry>>4), then RM_LoadVertices uses:
            //     RM_LoadVertices(F, (x>>6)<<1, (y>>6)<<1)
            // where x=rx>>4. Therefore base = ((real>>4)>>6)<<1 == (real>>10)<<1.
            int baseVertexX = (realX >> 10) << 1;
            int baseVertexY = (realY >> 10) << 1;

            for (int i = 0; i < piece.Vertices.Count; i++)
            {
                C2SmpVertexRecordLikeOriginal v = piece.Vertices[i];
                int vx = baseVertexX + v.X;
                int vy = baseVertexY + v.Y;

                // Original ignores border vertices: vx>0 && vy>0 && vx<VertInLine-1 && vy<MaxTH-1.
                if (vx <= 0 || vy <= 0 || vx >= _map.VertInLine - 1 || vy >= _map.MaxTH - 1)
                    continue;

                int idx = vx + vy * _map.VertInLine;
                if (idx < 0 || idx >= maxPointIndex)
                    continue;

                // Height in AusKuz.smp is 0. Do not force height if later pieces contain nonzero values:
                // the original adds Height to THMap, but our runtime terrain collision/objects are already live.
                // This V82 pass is terrain-texture underlay only.

                bool textureMeaningful = (v.Tex != 0) || (v.ExtraTex != 0 && v.ExtraWeight != 0);
                if (!textureMeaningful)
                    continue;

                // Original ImmVis path calls SetTexture(v, tex), not TexMap[v]=tex+128.
                if (v.Tex != 0 && _map.TexMap != null && idx < _map.TexMap.Length)
                    _map.TexMap[idx] = v.Tex;

                // Original 3D branch:
                //   if(tex || (et && wt>40)) { FactureMap[v]=fc; FactureWeight[v]=fw; }
                if (_map.FactureMap != null && _map.FactureWeight != null &&
                    idx < _map.FactureMap.Length && idx < _map.FactureWeight.Length &&
                    (v.Tex != 0 || (v.ExtraTex != 0 && v.ExtraWeight > 40)))
                {
                    _map.FactureMap[idx] = v.Facture;
                    _map.FactureWeight[idx] = v.FactureWeight;
                    _map.HasFactureMapChunk = true;
                }

                // Original 3D branch:
                //   if(int(wt)>int(WTexMapEx[v])*3) { if(et) TexMapEx[v]=et; WTexMapEx[v]=wt; }
                if (_map.TexMapEx != null && _map.WTexMapEx != null &&
                    idx < _map.TexMapEx.Length && idx < _map.WTexMapEx.Length &&
                    (int)v.ExtraWeight > (int)_map.WTexMapEx[idx] * 3)
                {
                    if (v.ExtraTex != 0)
                        _map.TexMapEx[idx] = v.ExtraTex;
                    _map.WTexMapEx[idx] = v.ExtraWeight;
                    _map.HasTilesExChunk = true;
                }

                changedVertices++;

                // Dirty cells around this vertex. A terrain cell uses 4 neighbor vertices.
                dirtyMinCellX = Mathf.Min(dirtyMinCellX, vx - 1);
                dirtyMinCellY = Mathf.Min(dirtyMinCellY, vy - 1);
                dirtyMaxCellXExclusive = Mathf.Max(dirtyMaxCellXExclusive, vx + 2);
                dirtyMaxCellYExclusive = Mathf.Max(dirtyMaxCellYExclusive, vy + 2);
            }

            if (changedVertices <= 0)
                return false;

            dirtyMinCellX = Mathf.Clamp(dirtyMinCellX, 0, Mathf.Max(0, _map.VertInLine - 2));
            dirtyMinCellY = Mathf.Clamp(dirtyMinCellY, 0, Mathf.Max(0, _map.MaxTH - 2));
            dirtyMaxCellXExclusive = Mathf.Clamp(dirtyMaxCellXExclusive, dirtyMinCellX + 1, Mathf.Max(1, _map.VertInLine - 1));
            dirtyMaxCellYExclusive = Mathf.Clamp(dirtyMaxCellYExclusive, dirtyMinCellY + 1, Mathf.Max(1, _map.MaxTH - 1));
            return true;
        }

        private static void EnsureTerrainTileArraysForSmpLikeOriginal(ParsedMap map)
        {
            if (map == null) return;
            int expected = Mathf.Max(0, map.VertInLine * map.MaxTH);
            if (map.TexMap == null || map.TexMap.Length < expected)
                map.TexMap = new byte[expected];
            if (map.TexMapEx == null || map.TexMapEx.Length < expected)
                map.TexMapEx = new byte[expected];
            if (map.WTexMapEx == null || map.WTexMapEx.Length < expected)
                map.WTexMapEx = new byte[expected];
        }


        private void C2SmpQueueProgressiveTerrainPaintJobV84LikeOriginal(
            string pieceName,
            string mdName,
            string source,
            int changedVertices,
            int dirtyMinCellX,
            int dirtyMinCellY,
            int dirtyMaxCellXExclusive,
            int dirtyMaxCellYExclusive,
            out int queuedChunks,
            out string audit)
        {
            queuedChunks = 0;
            audit = "not_started";

            if (_map == null || _terrainRoot == null || !_terrainBuilt)
            {
                audit = "no_terrain";
                return;
            }

            if (!object.ReferenceEquals(_c2SmpPaintWorkerMapRefV84LikeOriginal, _map))
            {
                _c2SmpPaintJobsV84LikeOriginal.Clear();
                _c2SmpPaintWorkerMapRefV84LikeOriginal = _map;
                _c2SmpFacturePrewarmedForMapV84LikeOriginal = false;
            }

            int paddedMinX = dirtyMinCellX - C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            int paddedMinY = dirtyMinCellY - C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            int paddedMaxX = dirtyMaxCellXExclusive + C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            int paddedMaxY = dirtyMaxCellYExclusive + C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;

            if (_map != null)
            {
                paddedMinX = Mathf.Clamp(paddedMinX, 0, Mathf.Max(0, _map.VertInLine - 2));
                paddedMinY = Mathf.Clamp(paddedMinY, 0, Mathf.Max(0, _map.MaxTH - 2));
                paddedMaxX = Mathf.Clamp(paddedMaxX, paddedMinX + 1, Mathf.Max(1, _map.VertInLine - 1));
                paddedMaxY = Mathf.Clamp(paddedMaxY, paddedMinY + 1, Mathf.Max(1, _map.MaxTH - 1));
            }

            C2SmpPaintJobV84LikeOriginal job = new C2SmpPaintJobV84LikeOriginal();
            job.PieceName = pieceName ?? string.Empty;
            job.MdName = mdName ?? string.Empty;
            job.Source = source ?? string.Empty;
            job.DirtyMinCellX = paddedMinX;
            job.DirtyMinCellY = paddedMinY;
            job.DirtyMaxCellXExclusive = paddedMaxX;
            job.DirtyMaxCellYExclusive = paddedMaxY;
            job.ChangedVertices = changedVertices;
            job.CreatedRealtime = Time.realtimeSinceStartup;

            _c2SmpPaintJobsV84LikeOriginal.Enqueue(job);

            queuedChunks = C2SmpEstimateDirtyChunkCountV84LikeOriginal(job);
            audit = "queued dirtyCells=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                    "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                    " estimatedChunks=" + queuedChunks.ToString(CultureInfo.InvariantCulture) +
                    " revealSeconds=" + C2SmpProgressiveRevealSecondsV84LikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) +
                    " mode=subregion_target_slow_pixel_reveal_profiled";

            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 JOB] piece='" + (pieceName ?? string.Empty) + "'" +
                      " md='" + (mdName ?? string.Empty) + "'" +
                      " dirtyRect=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                      "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                      " chunks=" + queuedChunks.ToString(CultureInfo.InvariantCulture) +
                      " targetBakeMs=pending shadow=1 uploadPerFrameMs=pending progress=0 source='" + (source ?? string.Empty) + "'");

            if (_c2SmpPaintWorkerV84LikeOriginal == null)
                _c2SmpPaintWorkerV84LikeOriginal = StartCoroutine(C2SmpProgressivePaintWorkerV84LikeOriginal());
        }

        private int C2SmpEstimateDirtyChunkCountV84LikeOriginal(C2SmpPaintJobV84LikeOriginal job)
        {
            if (job == null || _map == null)
                return 0;

            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);

            int totalCellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int totalCellsY = Mathf.Max(0, kernel.MaxCellYExclusive - kernel.MinCellY);
            int chunkCountX = Mathf.Max(1, Mathf.CeilToInt(totalCellsX / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int chunkCountY = Mathf.Max(1, Mathf.CeilToInt(totalCellsY / (float)TerrainSoftwareChunkCellsLikeOriginal));

            int minChunkX = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMinCellX - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int maxChunkX = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMaxCellXExclusive - 1 - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int minChunkY = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMinCellY - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);
            int maxChunkY = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMaxCellYExclusive - 1 - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);
            return Mathf.Max(0, (maxChunkX - minChunkX + 1) * (maxChunkY - minChunkY + 1));
        }

        private IEnumerator C2SmpProgressivePaintWorkerV84LikeOriginal()
        {
            // V85: do not block the whole SMP queue behind the first 15-second reveal.
            // Each building gets its own paint coroutine, so several foundations can reveal terrain in parallel.
            while (_c2SmpPaintJobsV84LikeOriginal.Count > 0)
            {
                C2SmpPaintJobV84LikeOriginal job = _c2SmpPaintJobsV84LikeOriginal.Dequeue();
                if (job != null)
                    StartCoroutine(C2SmpRunProgressivePaintJobV84LikeOriginal(job));
                yield return null;
            }

            _c2SmpPaintWorkerV84LikeOriginal = null;
        }

        private IEnumerator C2SmpRunProgressivePaintJobV84LikeOriginal(C2SmpPaintJobV84LikeOriginal job)
        {
            if (job == null || _map == null || _terrainRoot == null || !_terrainBuilt)
                yield break;

            var totalSw = global::System.Diagnostics.Stopwatch.StartNew();
            C2SmpProfilerV87LikeOriginal prof = null;
            if (C2SmpProfilerEnabledV87LikeOriginal)
            {
                prof = new C2SmpProfilerV87LikeOriginal();
                prof.PieceName = job.PieceName;
                prof.MdName = job.MdName;
                prof.StartRealtime = Time.realtimeSinceStartup;
                prof.LastLogRealtime = prof.StartRealtime;
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 PROF START] piece='" + job.PieceName + "'" +
                          " md='" + job.MdName + "'" +
                          " source='" + job.Source + "'" +
                          " dirtyRect=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                          "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                          " queueAgeMs=" + Mathf.RoundToInt((Time.realtimeSinceStartup - job.CreatedRealtime) * 1000.0f).ToString(CultureInfo.InvariantCulture) +
                          C2SmpFpsNowV87LikeOriginal());
            }

            var prepareSwV86 = global::System.Diagnostics.Stopwatch.StartNew();
            TerrainSoftwareBakeInputsLikeOriginal inputs = PrepareTerrainSoftwareBakeInputsLikeOriginal();
            prepareSwV86.Stop();
            if (prof != null) prof.PrepareInputsMs += prepareSwV86.ElapsedMilliseconds;
            if (prepareSwV86.ElapsedMilliseconds >= 5)
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 STEP] piece='" + job.PieceName + "' stage=prepare_inputs ms=" + prepareSwV86.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
            if (inputs == null || inputs.GroundAtlas == null || inputs.GroundPixels == null || inputs.GroundPixels.Length == 0)
            {
                Debug.LogWarning("[C2:SMP V92 JOB SKIP] piece='" + job.PieceName + "' reason=no_bake_inputs");
                yield break;
            }

            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);

            _lastBuiltTerrainKernel = kernel;
            _hasLastBuiltTerrainKernel = true;

            // V92: prewarm every SMP job after its runtime terrain arrays were changed.
            // V88 prewarmed only once per map, so later buildings could hit Texture2D.GetPixels32 inside Task.Run.
            if (!TerrainQualityFactureLayerDisabledLikeAdapted && HasFactureLayerDataLikeOriginal(_map))
            {
                var prewarmSw = global::System.Diagnostics.Stopwatch.StartNew();
                PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal(inputs);
                prewarmSw.Stop();
                if (prof != null) prof.PrewarmMs += prewarmSw.ElapsedMilliseconds;
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 PREWARM] piece='" + job.PieceName + "' ms=" + prewarmSw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
                yield return null;
            }

            List<C2SmpPaintChunkEntryV84LikeOriginal> entries = new List<C2SmpPaintChunkEntryV84LikeOriginal>();
            int failed = 0;
            long targetBakeMs = 0;

            int totalCellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int totalCellsY = Mathf.Max(0, kernel.MaxCellYExclusive - kernel.MinCellY);
            int chunkCountX = Mathf.Max(1, Mathf.CeilToInt(totalCellsX / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int chunkCountY = Mathf.Max(1, Mathf.CeilToInt(totalCellsY / (float)TerrainSoftwareChunkCellsLikeOriginal));

            int minChunkX = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMinCellX - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int maxChunkX = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMaxCellXExclusive - 1 - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int minChunkY = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMinCellY - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);
            int maxChunkY = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMaxCellYExclusive - 1 - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);

            for (int cy = minChunkY; cy <= maxChunkY; cy++)
            {
                int fullMinCellY = kernel.MinCellY + cy * TerrainSoftwareChunkCellsLikeOriginal;
                int fullMaxCellYExclusive = Mathf.Min(kernel.MaxCellYExclusive, fullMinCellY + TerrainSoftwareChunkCellsLikeOriginal);

                for (int cx = minChunkX; cx <= maxChunkX; cx++)
                {
                    int fullMinCellX = kernel.MinCellX + cx * TerrainSoftwareChunkCellsLikeOriginal;
                    int fullMaxCellXExclusive = Mathf.Min(kernel.MaxCellXExclusive, fullMinCellX + TerrainSoftwareChunkCellsLikeOriginal);
                    if (fullMaxCellXExclusive <= fullMinCellX || fullMaxCellYExclusive <= fullMinCellY)
                        continue;

                    string chunkName = string.Format(CultureInfo.InvariantCulture, "TerrainChunkSoftware_{0:00}_{1:00}", cx, cy);
                    Transform chunkTr = _terrainRoot.transform.Find(chunkName);
                    MeshRenderer mr = chunkTr != null ? chunkTr.GetComponent<MeshRenderer>() : null;
                    Texture2D tex = null;
                    if (mr != null && mr.sharedMaterial != null)
                        tex = mr.sharedMaterial.mainTexture as Texture2D;
                    if (tex == null)
                    {
                        failed++;
                        continue;
                    }

                    var regionSwV86 = global::System.Diagnostics.Stopwatch.StartNew();
                    TerrainSoftwareChunkRegionLikeOriginal fullRegion = CreateTerrainSoftwareChunkRegionLikeOriginal(_map, kernel, fullMinCellX, fullMaxCellXExclusive, fullMinCellY, fullMaxCellYExclusive);
                    regionSwV86.Stop();
                    if (prof != null) prof.RegionMs += regionSwV86.ElapsedMilliseconds;
                    int fullWidth = fullRegion.WidthPixels;
                    int fullHeight = fullRegion.HeightPixels;
                    if (tex.width != fullWidth || tex.height != fullHeight)
                    {
                        Debug.LogWarning("[C2:SMP V92 CHUNK SKIP] piece='" + job.PieceName + "' chunk=" + chunkName +
                                         " reason=texture_size_mismatch tex=" + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture) +
                                         " expected=" + fullWidth.ToString(CultureInfo.InvariantCulture) + "x" + fullHeight.ToString(CultureInfo.InvariantCulture));
                        failed++;
                        continue;
                    }

                    int subMinCellX = Mathf.Max(job.DirtyMinCellX, fullMinCellX);
                    int subMinCellY = Mathf.Max(job.DirtyMinCellY, fullMinCellY);
                    int subMaxCellX = Mathf.Min(job.DirtyMaxCellXExclusive, fullMaxCellXExclusive);
                    int subMaxCellY = Mathf.Min(job.DirtyMaxCellYExclusive, fullMaxCellYExclusive);
                    if (subMaxCellX <= subMinCellX || subMaxCellY <= subMinCellY)
                        continue;

                    var subRegionSwV86 = global::System.Diagnostics.Stopwatch.StartNew();
                    TerrainSoftwareChunkRegionLikeOriginal subRegion = CreateTerrainSoftwareChunkRegionLikeOriginal(
                        _map, kernel, subMinCellX, subMaxCellX, subMinCellY, subMaxCellY);
                    subRegionSwV86.Stop();
                    if (prof != null) prof.RegionMs += subRegionSwV86.ElapsedMilliseconds;

                    int targetRawWidth = subRegion.WidthPixels;
                    int targetRawHeight = subRegion.HeightPixels;
                    if (targetRawWidth <= 0 || targetRawHeight <= 0)
                    {
                        failed++;
                        continue;
                    }

                    // V85 offset fix: do NOT use cell*pixels. Project the sub-region footprint into the real full chunk.
                    Bounds sb = subRegion.FootprintBounds;
                    Vector2 sp0 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.min.x, 0.0f, sb.min.z));
                    Vector2 sp1 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.max.x, 0.0f, sb.min.z));
                    Vector2 sp2 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.min.x, 0.0f, sb.max.z));
                    Vector2 sp3 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.max.x, 0.0f, sb.max.z));
                    int px = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(Mathf.Min(sp0.x, sp1.x), Mathf.Min(sp2.x, sp3.x))), 0, Mathf.Max(0, fullWidth - 1));
                    int py = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(Mathf.Min(sp0.y, sp1.y), Mathf.Min(sp2.y, sp3.y))), 0, Mathf.Max(0, fullHeight - 1));
                    int pw = Mathf.Min(targetRawWidth, fullWidth - px);
                    int ph = Mathf.Min(targetRawHeight, fullHeight - py);
                    if (pw <= 0 || ph <= 0)
                    {
                        failed++;
                        continue;
                    }

                    var shadowSwV87 = global::System.Diagnostics.Stopwatch.StartNew();
                    C2SmpChunkShadowV87LikeOriginal shadow;
                    if (!C2SmpTryGetTerrainChunkShadowV87LikeOriginal(cx, cy, fullWidth, fullHeight, out shadow))
                    {
                        shadowSwV87.Stop();
                        if (prof != null) prof.ShadowMs += shadowSwV87.ElapsedMilliseconds;
                        Debug.LogWarning("[C2:SMP V92 CHUNK SKIP] piece='" + job.PieceName + "' chunk=" + chunkName +
                                         " reason=no_cpu_shadow full=" + fullWidth.ToString(CultureInfo.InvariantCulture) + "x" + fullHeight.ToString(CultureInfo.InvariantCulture) +
                                         " note='reload map after V87 so terrain bake registers chunk shadows'");
                        failed++;
                        continue;
                    }
                    shadowSwV87.Stop();
                    if (prof != null) prof.ShadowMs += shadowSwV87.ElapsedMilliseconds;

                    var copySwV87 = global::System.Diagnostics.Stopwatch.StartNew();
                    Color32[] baseSub = C2SmpExtractSubRectV87LikeOriginal(shadow.Pixels, shadow.Width, shadow.Height, px, py, pw, ph);
                    copySwV87.Stop();
                    if (prof != null)
                    {
                        prof.CopyMs += copySwV87.ElapsedMilliseconds;
                        prof.DirtyPixelsTotal += pw * ph;
                        prof.Entries++;
                    }

                    C2SmpPaintChunkEntryV84LikeOriginal entry = new C2SmpPaintChunkEntryV84LikeOriginal();
                    entry.ChunkName = chunkName;
                    entry.ChunkX = cx;
                    entry.ChunkY = cy;
                    entry.Texture = tex;
                    entry.Shadow = shadow;
                    entry.OffsetPixelsX = px;
                    entry.OffsetPixelsY = py;
                    entry.WidthPixels = pw;
                    entry.HeightPixels = ph;
                    entry.RawDirtyPixelCount = pw * ph;
                    entry.BasePixels = baseSub;
                    entry.WorkingPixels = (Color32[])baseSub.Clone();
                    entry.TileCols = Mathf.Max(1, Mathf.CeilToInt(pw / (float)C2SmpProgressiveTileSizeV87LikeOriginal));
                    entry.TileRows = Mathf.Max(1, Mathf.CeilToInt(ph / (float)C2SmpProgressiveTileSizeV87LikeOriginal));
                    entry.RevealedTiles = new bool[entry.TileCols * entry.TileRows];
                    entry.FrontierMask = new bool[entry.TileCols * entry.TileRows];
                    entry.CenterX = pw * 0.5f;
                    entry.CenterY = ph * 0.5f;
                    entry.MaxDistance = Mathf.Max(1.0f, Mathf.Sqrt(pw * pw + ph * ph) * 0.5f);
                    entry.NoiseSeed = (cx * 928371 + cy * 364479 + job.PieceName.GetHashCode()) * 0.0001f;
                    entry.LastUploadTime = -1000.0f;
                    C2SmpClearDirtyUploadV87LikeOriginal(entry);
                    var subRegionCapture = subRegion;
                    var inputsCapture = inputs;
                    var mapCapture = _map;
                    var kernelCapture = kernel;
                    entry.PendingBakeTask = Task.Run(() =>
                    {
                        var res = new C2SmpChunkBakeResultV88LikeOriginal();
                        var bakeSwLocal = global::System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            Color32[] targetRaw = BakeTerrainChunkPixelsSoftwareLikeOriginal(mapCapture, kernelCapture, subRegionCapture, inputsCapture);
                            if (targetRaw != null && targetRaw.Length == targetRawWidth * targetRawHeight)
                            {
                                Color32[] targetSubLocal = new Color32[pw * ph];
                                for (int yy = 0; yy < ph; yy++)
                                {
                                    int srcRow = yy * targetRawWidth;
                                    int dstRow = yy * pw;
                                    if (srcRow >= 0 && srcRow + pw <= targetRaw.Length && dstRow >= 0 && dstRow + pw <= targetSubLocal.Length)
                                        Array.Copy(targetRaw, srcRow, targetSubLocal, dstRow, pw);
                                }
                                res.TargetPixels = targetSubLocal;
                                res.Width = pw;
                                res.Height = ph;
                            }
                            else
                            {
                                res.Error = "invalid_target_size";
                            }
                        }
                        catch (Exception ex)
                        {
                            res.Error = ex.GetType().Name + ": " + ex.Message;
                        }
                        bakeSwLocal.Stop();
                        res.BakeMs = bakeSwLocal.ElapsedMilliseconds;
                        return res;
                    });
                    entries.Add(entry);

                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 CHUNK QUEUED] piece='" + job.PieceName + "' chunk=" + chunkName +
                              " full=" + fullWidth.ToString(CultureInfo.InvariantCulture) + "x" + fullHeight.ToString(CultureInfo.InvariantCulture) +
                              " dirtyPixels=" + px.ToString(CultureInfo.InvariantCulture) + "/" + py.ToString(CultureInfo.InvariantCulture) +
                              "+" + pw.ToString(CultureInfo.InvariantCulture) + "x" + ph.ToString(CultureInfo.InvariantCulture) +
                              " shadow=hit" +
                              " copyMs=" + copySwV87.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                              C2SmpFpsNowV87LikeOriginal());

                    yield return null;
                }
            }

            if (entries.Count == 0)
            {
                Debug.LogWarning("[C2:SMP V92 JOB SKIP] piece='" + job.PieceName + "' reason=no_chunk_entries failed=" + failed.ToString(CultureInfo.InvariantCulture));
                yield break;
            }

            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 JOB] piece='" + job.PieceName + "'" +
                      " md='" + job.MdName + "'" +
                      " dirtyRect=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                      "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                      " chunks=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                      " targetBakeMs=" + targetBakeMs.ToString(CultureInfo.InvariantCulture) +
                      " shadow=1 uploadPerFrameMs=0 progress=0");

            float start = Time.realtimeSinceStartup;
            int lastLoggedPercent = -1;
            float nextProfileLogV86 = start + C2SmpProfilerLogIntervalV87LikeOriginal;

            while (true)
            {
                if (prof != null)
                    C2SmpProfilerSampleFrameV87LikeOriginal(prof);
                float now = Time.realtimeSinceStartup;
                float progress = Mathf.Clamp01((now - start) / Mathf.Max(0.1f, C2SmpProgressiveRevealSecondsV84LikeOriginal));
                int budget = Mathf.Max(1, C2SmpProgressiveMaxNewPixelsPerFrameV84LikeOriginal);
                long uploadMsThisFrame = 0;
                long revealMsThisFrame = 0;
                int revealedThisFrame = 0;
                int readyEntries = 0;
                bool anyPendingBake = false;
                bool anyUnrevealedReady = false;
                bool holdRevealThisFrameV92 = false;
                if (C2SmpWaitAllChunksReadyBeforeRevealV92LikeOriginal)
                {
                    for (int pe = 0; pe < entries.Count; pe++)
                    {
                        var pendingEntry = entries[pe];
                        if (pendingEntry != null && !pendingEntry.Ready && !pendingEntry.Failed)
                        {
                            holdRevealThisFrameV92 = true;
                            break;
                        }
                    }
                }
                var frameBudgetSwV88 = global::System.Diagnostics.Stopwatch.StartNew();

                for (int e = 0; e < entries.Count; e++)
                {
                    C2SmpPaintChunkEntryV84LikeOriginal entry = entries[e];
                    if (entry == null || entry.Texture == null)
                        continue;

                    if (!entry.Ready && !entry.Failed)
                    {
                        if (entry.PendingBakeTask != null && entry.PendingBakeTask.IsCompleted)
                        {
                            C2SmpChunkBakeResultV88LikeOriginal bakeRes = null;
                            try { bakeRes = entry.PendingBakeTask.Result; } catch (Exception ex) { bakeRes = new C2SmpChunkBakeResultV88LikeOriginal(); bakeRes.Error = ex.GetType().Name + ": " + ex.Message; }
                            entry.PendingBakeTask = null;
                            if (bakeRes == null || bakeRes.TargetPixels == null || bakeRes.TargetPixels.Length != entry.WidthPixels * entry.HeightPixels)
                            {
                                entry.Failed = true;
                                entry.PendingBakeError = bakeRes != null ? bakeRes.Error : "null_bake_result";
                                if (prof != null) { prof.TargetFailCount++; prof.BakeMs += bakeRes != null ? bakeRes.BakeMs : 0; }
                                Debug.LogWarning("[C2:SMP V92 TARGET FAIL] piece='" + job.PieceName + "' chunk=" + entry.ChunkName +
                                                 " error='" + entry.PendingBakeError + "'");
                            }
                            else
                            {
                                entry.TargetPixels = bakeRes.TargetPixels;
                                entry.ActiveTiles = C2SmpBuildActiveTileMaskV90LikeOriginal(entry, out entry.ActiveTileCount, out entry.ActivePixelEstimate);
                                entry.Ready = true;
                                targetBakeMs += bakeRes.BakeMs;
                                if (prof != null)
                                {
                                    prof.BakeMs += bakeRes.BakeMs;
                                    prof.DirtyPixelsTotal -= entry.RawDirtyPixelCount;
                                    prof.DirtyPixelsTotal += entry.ActivePixelEstimate;
                                }
                                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 CHUNK READY] piece='" + job.PieceName + "' chunk=" + entry.ChunkName +
                                          " dirtyPixels=" + entry.OffsetPixelsX.ToString(CultureInfo.InvariantCulture) + "/" + entry.OffsetPixelsY.ToString(CultureInfo.InvariantCulture) +
                                          "+" + entry.WidthPixels.ToString(CultureInfo.InvariantCulture) + "x" + entry.HeightPixels.ToString(CultureInfo.InvariantCulture) +
                                          " activeTiles=" + entry.ActiveTileCount.ToString(CultureInfo.InvariantCulture) + "/" + (entry.TileCols * entry.TileRows).ToString(CultureInfo.InvariantCulture) +
                                          " activePixels~=" + entry.ActivePixelEstimate.ToString(CultureInfo.InvariantCulture) +
                                          " bakeMs=" + bakeRes.BakeMs.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
                            }
                        }
                        if (!entry.Ready && !entry.Failed)
                            anyPendingBake = true;
                    }

                    if (!entry.Ready || entry.Failed || entry.TargetPixels == null)
                        continue;

                    readyEntries++;
                    int revealableTilesForEntryV92 = entry.ActiveTileCount > 0 ? entry.ActiveTileCount : entry.RevealedTiles.Length;
                    if (entry.RevealedCount >= revealableTilesForEntryV92)
                        continue;

                    anyUnrevealedReady = true;
                    if (holdRevealThisFrameV92 || budget <= 0 || frameBudgetSwV88.Elapsed.TotalMilliseconds >= C2SmpProgressiveFrameWorkBudgetMsV88LikeOriginal)
                        continue;

                    var revealSwV86 = global::System.Diagnostics.Stopwatch.StartNew();
                    bool changed = C2SmpRevealPixelsForEntryV84LikeOriginal(entry, progress, ref budget, out int revealedEntry);
                    revealSwV86.Stop();
                    revealMsThisFrame += revealSwV86.ElapsedMilliseconds;
                    if (prof != null)
                    {
                        prof.RevealMs += revealSwV86.ElapsedMilliseconds;
                        prof.RevealedPixels += revealedEntry;
                    }
                    revealedThisFrame += revealedEntry;

                    if (changed && C2SmpHasDirtyUploadV87LikeOriginal(entry) && now - entry.LastUploadTime >= C2SmpProgressiveUploadMinIntervalV84LikeOriginal)
                    {
                        try
                        {
                            var setSwV87 = global::System.Diagnostics.Stopwatch.StartNew();
                            int tileUploads = 0;
                            if (entry.DirtyTiles != null)
                            {
                                while (entry.DirtyTiles.Count > 0 && tileUploads < C2SmpProgressiveMaxTileUploadsPerFrameV88LikeOriginal)
                                {
                                    var tile = entry.DirtyTiles[0];
                                    entry.DirtyTiles.RemoveAt(0);
                                    Color32[] uploadPixels = C2SmpBuildWorkingUploadTileV88LikeOriginal(entry, tile);
                                    if (uploadPixels == null || uploadPixels.Length != tile.W * tile.H)
                                        continue;
                                    entry.Texture.SetPixels32(entry.OffsetPixelsX + tile.X, entry.OffsetPixelsY + tile.Y, tile.W, tile.H, uploadPixels);
                                    C2SmpWriteSubRectToShadowV87LikeOriginal(entry.Shadow, entry.OffsetPixelsX + tile.X, entry.OffsetPixelsY + tile.Y, tile.W, tile.H, uploadPixels);
                                    tileUploads++;
                                }
                            }
                            setSwV87.Stop();
                            if (tileUploads > 0)
                            {
                                var applySwV87 = global::System.Diagnostics.Stopwatch.StartNew();
                                entry.Texture.Apply(false, false);
                                applySwV87.Stop();
                                if (entry.DirtyTiles == null || entry.DirtyTiles.Count == 0)
                                    C2SmpClearDirtyUploadV87LikeOriginal(entry);
                                entry.LastUploadTime = now;
                                uploadMsThisFrame += setSwV87.ElapsedMilliseconds + applySwV87.ElapsedMilliseconds;
                                if (prof != null)
                                {
                                    prof.SetPixelsMs += setSwV87.ElapsedMilliseconds;
                                    prof.ApplyMs += applySwV87.ElapsedMilliseconds;
                                    prof.UploadCalls++;
                                }
                                if (setSwV87.ElapsedMilliseconds + applySwV87.ElapsedMilliseconds >= C2SmpProfilerSpikeMsV87LikeOriginal)
                                {
                                    Debug.LogWarning("[C2:SMP V92 SPIKE] piece='" + job.PieceName + "' chunk=" + entry.ChunkName +
                                                     " stage=upload tiles=" + tileUploads.ToString(CultureInfo.InvariantCulture) +
                                                     " setPixelsMs=" + setSwV87.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                                                     " applyMs=" + applySwV87.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                                                     C2SmpFpsNowV87LikeOriginal());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[C2:SMP V92 UPLOAD FAIL] piece='" + job.PieceName + "' chunk=" + entry.ChunkName +
                                             " error='" + ex.GetType().Name + ": " + ex.Message + "'");
                        }
                    }
                }

                int percent = Mathf.FloorToInt(progress * 100.0f);
                if (percent >= lastLoggedPercent + 25)
                {
                    lastLoggedPercent = percent;
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 JOB] piece='" + job.PieceName + "'" +
                              " dirtyRect=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                              "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                              " chunks=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                              " ready=" + readyEntries.ToString(CultureInfo.InvariantCulture) +
                              " targetBakeMs=" + targetBakeMs.ToString(CultureInfo.InvariantCulture) +
                              " uploadPerFrameMs=" + uploadMsThisFrame.ToString(CultureInfo.InvariantCulture) +
                              " revealMsThisFrame=" + revealMsThisFrame.ToString(CultureInfo.InvariantCulture) +
                              " progress=" + percent.ToString(CultureInfo.InvariantCulture) +
                              " revealedPixelsThisFrame=" + revealedThisFrame.ToString(CultureInfo.InvariantCulture) +
                              C2SmpProfilerSummaryV87LikeOriginal(prof) +
                              C2SmpFpsNowV87LikeOriginal());
                }

                if (prof != null && now >= nextProfileLogV86)
                {
                    nextProfileLogV86 = now + C2SmpProfilerLogIntervalV87LikeOriginal;
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 FPS] piece='" + job.PieceName + "'" +
                              " stage=reveal progress=" + Mathf.FloorToInt(progress * 100.0f).ToString(CultureInfo.InvariantCulture) +
                              " entries=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                              " ready=" + readyEntries.ToString(CultureInfo.InvariantCulture) +
                              " revealedThisFrame=" + revealedThisFrame.ToString(CultureInfo.InvariantCulture) +
                              " revealMsThisFrame=" + revealMsThisFrame.ToString(CultureInfo.InvariantCulture) +
                              " uploadMsThisFrame=" + uploadMsThisFrame.ToString(CultureInfo.InvariantCulture) +
                              " uploadCalls=" + (prof != null ? prof.UploadCalls.ToString(CultureInfo.InvariantCulture) : "0") +
                              " revealedTotal=" + (prof != null ? prof.RevealedPixels.ToString(CultureInfo.InvariantCulture) : "0") + "/" + (prof != null ? prof.DirtyPixelsTotal.ToString(CultureInfo.InvariantCulture) : "0") +
                              C2SmpProfilerSummaryV87LikeOriginal(prof) +
                              C2SmpFpsNowV87LikeOriginal());
                }

                bool allFinished = !anyPendingBake;
                if (allFinished)
                {
                    for (int e = 0; e < entries.Count; e++)
                    {
                        var entry = entries[e];
                        if (entry == null || entry.Failed)
                            continue;
                        if (!entry.Ready || entry.RevealedTiles == null || entry.RevealedCount < (entry.ActiveTileCount > 0 ? entry.ActiveTileCount : entry.RevealedTiles.Length) || (entry.DirtyTiles != null && entry.DirtyTiles.Count > 0))
                        {
                            allFinished = false;
                            break;
                        }
                    }
                }

                if (allFinished)
                    break;

                yield return null;
            }

            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V92 DONE] piece='" + job.PieceName + "'" +
                      " md='" + job.MdName + "'" +
                      " chunks=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                      " targetBakeMs=" + targetBakeMs.ToString(CultureInfo.InvariantCulture) +
                      " shadow=1 totalMs=" + totalSw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                      " revealSeconds=" + C2SmpProgressiveRevealSecondsV84LikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) +
                      " failed=" + failed.ToString(CultureInfo.InvariantCulture) +
                      " prepareInputsMs=" + (prof != null ? prof.PrepareInputsMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " prewarmMs=" + (prof != null ? prof.PrewarmMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " regionMs=" + (prof != null ? prof.RegionMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " getPixelsMs=" + (prof != null ? prof.GetPixelsMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " shadowMs=" + (prof != null ? prof.ShadowMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " asyncWaitMs=" + (prof != null ? prof.AsyncWaitMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " bakeMs=" + (prof != null ? prof.BakeMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " fallbackBakeMs=" + (prof != null ? prof.FallbackBakeMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " copyMs=" + (prof != null ? prof.CopyMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " revealMs=" + (prof != null ? prof.RevealMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " setPixelsMs=" + (prof != null ? prof.SetPixelsMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " applyMs=" + (prof != null ? prof.ApplyMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " finalUploadMs=" + (prof != null ? prof.FinalUploadMs.ToString(CultureInfo.InvariantCulture) : "0") +
                      " uploadCalls=" + (prof != null ? prof.UploadCalls.ToString(CultureInfo.InvariantCulture) : "0") +
                      " finalUploadCalls=" + (prof != null ? prof.FinalUploadCalls.ToString(CultureInfo.InvariantCulture) : "0") +
                      " dirtyPixels=" + (prof != null ? prof.DirtyPixelsTotal.ToString(CultureInfo.InvariantCulture) : "0") +
                      C2SmpProfilerSummaryV87LikeOriginal(prof));
        }

        private bool C2SmpComputeDirtyPixelRectInFullChunkV84LikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareChunkRegionLikeOriginal fullRegion,
            C2SmpPaintJobV84LikeOriginal job,
            out int x,
            out int y,
            out int w,
            out int h)
        {
            x = 0;
            y = 0;
            w = 0;
            h = 0;
            if (map == null || job == null || fullRegion.WidthPixels <= 0 || fullRegion.HeightPixels <= 0)
                return false;

            int minCellX = Mathf.Clamp(job.DirtyMinCellX, fullRegion.MinCellX, fullRegion.MaxCellXExclusive - 1);
            int maxCellX = Mathf.Clamp(job.DirtyMaxCellXExclusive, minCellX + 1, fullRegion.MaxCellXExclusive);
            int minCellY = Mathf.Clamp(job.DirtyMinCellY, fullRegion.MinCellY, fullRegion.MaxCellYExclusive - 1);
            int maxCellY = Mathf.Clamp(job.DirtyMaxCellYExclusive, minCellY + 1, fullRegion.MaxCellYExclusive);

            TerrainSoftwareChunkRegionLikeOriginal dirtyRegion = CreateTerrainSoftwareChunkRegionLikeOriginal(map, kernel, minCellX, maxCellX, minCellY, maxCellY);
            Bounds b = dirtyRegion.FootprintBounds;

            Vector2 p0 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(b.min.x, 0.0f, b.min.z));
            Vector2 p1 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(b.max.x, 0.0f, b.min.z));
            Vector2 p2 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(b.min.x, 0.0f, b.max.z));
            Vector2 p3 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(b.max.x, 0.0f, b.max.z));

            float minPx = Mathf.Min(Mathf.Min(p0.x, p1.x), Mathf.Min(p2.x, p3.x));
            float maxPx = Mathf.Max(Mathf.Max(p0.x, p1.x), Mathf.Max(p2.x, p3.x));
            float minPy = Mathf.Min(Mathf.Min(p0.y, p1.y), Mathf.Min(p2.y, p3.y));
            float maxPy = Mathf.Max(Mathf.Max(p0.y, p1.y), Mathf.Max(p2.y, p3.y));

            int pad = Mathf.Max(2, C2SmpProgressivePixelPaddingV84LikeOriginal);
            int ix0 = Mathf.Clamp(Mathf.FloorToInt(minPx) - pad, 0, fullRegion.WidthPixels - 1);
            int iy0 = Mathf.Clamp(Mathf.FloorToInt(minPy) - pad, 0, fullRegion.HeightPixels - 1);
            int ix1 = Mathf.Clamp(Mathf.CeilToInt(maxPx) + pad + 1, ix0 + 1, fullRegion.WidthPixels);
            int iy1 = Mathf.Clamp(Mathf.CeilToInt(maxPy) + pad + 1, iy0 + 1, fullRegion.HeightPixels);

            x = ix0;
            y = iy0;
            w = Mathf.Max(1, ix1 - ix0);
            h = Mathf.Max(1, iy1 - iy0);
            return w > 0 && h > 0;
        }


        private static bool C2SmpIsActiveTileV90LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, int idx)
        {
            if (entry == null)
                return false;
            if (entry.ActiveTiles == null || entry.ActiveTiles.Length == 0)
                return true;
            return idx >= 0 && idx < entry.ActiveTiles.Length && entry.ActiveTiles[idx];
        }

        private static bool[] C2SmpBuildActiveTileMaskV90LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, out int activeTiles, out int activePixelEstimate)
        {
            activeTiles = 0;
            activePixelEstimate = 0;
            if (entry == null || entry.BasePixels == null || entry.TargetPixels == null ||
                entry.WidthPixels <= 0 || entry.HeightPixels <= 0 || entry.TileCols <= 0 || entry.TileRows <= 0)
                return null;

            int tileSize = Mathf.Max(1, C2SmpProgressiveTileSizeV87LikeOriginal);
            int totalTiles = entry.TileCols * entry.TileRows;
            bool[] mask = new bool[totalTiles];
            byte[] alpha = new byte[totalTiles];
            entry.ActiveMinTileX = int.MaxValue;
            entry.ActiveMinTileY = int.MaxValue;
            entry.ActiveMaxTileXExclusive = int.MinValue;
            entry.ActiveMaxTileYExclusive = int.MinValue;

            for (int ty = 0; ty < entry.TileRows; ty++)
            {
                for (int tx = 0; tx < entry.TileCols; tx++)
                {
                    int tileIndex = ty * entry.TileCols + tx;
                    int x0 = tx * tileSize;
                    int y0 = ty * tileSize;
                    int x1 = Mathf.Min(entry.WidthPixels, x0 + tileSize);
                    int y1 = Mathf.Min(entry.HeightPixels, y0 + tileSize);
                    int changed = 0;
                    int strong = 0;
                    int pixels = Mathf.Max(0, (x1 - x0) * (y1 - y0));

                    for (int y = y0; y < y1; y++)
                    {
                        int row = y * entry.WidthPixels;
                        for (int x = x0; x < x1; x++)
                        {
                            int pi = row + x;
                            if (pi < 0 || pi >= entry.BasePixels.Length || pi >= entry.TargetPixels.Length)
                                continue;
                            Color32 a = entry.BasePixels[pi];
                            Color32 b = entry.TargetPixels[pi];
                            int dr = Mathf.Abs((int)a.r - (int)b.r);
                            int dg = Mathf.Abs((int)a.g - (int)b.g);
                            int db = Mathf.Abs((int)a.b - (int)b.b);
                            int da = Mathf.Abs((int)a.a - (int)b.a);
                            int d = dr + dg + db + da;
                            if (d >= 18)
                                changed++;
                            if (d >= 42 || dr >= 16 || dg >= 16 || db >= 16 || da >= 16)
                                strong++;
                        }
                    }

                    bool active = strong > 0 || changed >= Mathf.Max(2, pixels / 16);
                    mask[tileIndex] = active;
                    if (active)
                    {
                        activeTiles++;
                        activePixelEstimate += pixels;
                        entry.ActiveMinTileX = Mathf.Min(entry.ActiveMinTileX, tx);
                        entry.ActiveMinTileY = Mathf.Min(entry.ActiveMinTileY, ty);
                        entry.ActiveMaxTileXExclusive = Mathf.Max(entry.ActiveMaxTileXExclusive, tx + 1);
                        entry.ActiveMaxTileYExclusive = Mathf.Max(entry.ActiveMaxTileYExclusive, ty + 1);
                    }
                }
            }

            if (activeTiles <= 0)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    mask[i] = true;
                    alpha[i] = 255;
                }
                activeTiles = mask.Length;
                activePixelEstimate = entry.WidthPixels * entry.HeightPixels;
                entry.ActiveMinTileX = 0;
                entry.ActiveMinTileY = 0;
                entry.ActiveMaxTileXExclusive = entry.TileCols;
                entry.ActiveMaxTileYExclusive = entry.TileRows;
            }
            else
            {
                for (int ty = 0; ty < entry.TileRows; ty++)
                {
                    for (int tx = 0; tx < entry.TileCols; tx++)
                    {
                        int idx = ty * entry.TileCols + tx;
                        if (!mask[idx])
                            continue;
                        int inactive4 = 0;
                        // V92: never treat the local chunk/subrect border itself as a soft edge.
                        // If an SMP piece crosses a chunk boundary, the neighbor tile lives in the next chunk;
                        // counting the border as inactive creates the visible rectangular seam.
                        if (tx > 0 && !mask[idx - 1]) inactive4++;
                        if (tx < entry.TileCols - 1 && !mask[idx + 1]) inactive4++;
                        if (ty > 0 && !mask[idx - entry.TileCols]) inactive4++;
                        if (ty < entry.TileRows - 1 && !mask[idx + entry.TileCols]) inactive4++;
                        if (inactive4 >= 2)
                            alpha[idx] = (byte)Mathf.Clamp(Mathf.RoundToInt(C2SmpSoftEdgeAlphaCornerV91LikeOriginal * 255.0f), 1, 255);
                        else if (inactive4 == 1)
                            alpha[idx] = (byte)Mathf.Clamp(Mathf.RoundToInt(C2SmpSoftEdgeAlphaSideV91LikeOriginal * 255.0f), 1, 255);
                        else
                            alpha[idx] = 255;
                    }
                }
            }

            entry.TileRevealAlpha = alpha;
            if (entry.ActiveMaxTileXExclusive > entry.ActiveMinTileX && entry.ActiveMaxTileYExclusive > entry.ActiveMinTileY)
            {
                float minPx = entry.ActiveMinTileX * tileSize;
                float minPy = entry.ActiveMinTileY * tileSize;
                float maxPx = Mathf.Min(entry.WidthPixels, entry.ActiveMaxTileXExclusive * tileSize);
                float maxPy = Mathf.Min(entry.HeightPixels, entry.ActiveMaxTileYExclusive * tileSize);
                entry.CenterX = (minPx + maxPx) * 0.5f;
                entry.CenterY = (minPy + maxPy) * 0.5f;
                entry.MaxDistance = Mathf.Max(1.0f, Mathf.Sqrt((maxPx - minPx) * (maxPx - minPx) + (maxPy - minPy) * (maxPy - minPy)) * 0.5f);
            }

            return mask;
        }

        private static Color32 C2SmpLerpColor32V91LikeOriginal(Color32 a, Color32 b, byte alpha)
        {
            if (alpha <= 0)
                return a;
            if (alpha >= 255)
                return b;
            int ia = 255 - alpha;
            Color32 c = new Color32();
            c.r = (byte)((a.r * ia + b.r * alpha + 127) / 255);
            c.g = (byte)((a.g * ia + b.g * alpha + 127) / 255);
            c.b = (byte)((a.b * ia + b.b * alpha + 127) / 255);
            c.a = (byte)((a.a * ia + b.a * alpha + 127) / 255);
            return c;
        }

        private static int C2SmpFindNearestActiveTileToCenterV90LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry)
        {
            if (entry == null || entry.TileCols <= 0 || entry.TileRows <= 0)
                return -1;
            int tileSize = Mathf.Max(1, C2SmpProgressiveTileSizeV87LikeOriginal);
            float seedX = entry.CenterX;
            float seedY = Mathf.Lerp(entry.HeightPixels - 1.0f, entry.CenterY, 0.35f);
            float best = float.MaxValue;
            int bestIdx = -1;
            for (int ty = 0; ty < entry.TileRows; ty++)
            {
                for (int tx = 0; tx < entry.TileCols; tx++)
                {
                    int idx = ty * entry.TileCols + tx;
                    if (!C2SmpIsActiveTileV90LikeOriginal(entry, idx) || (entry.RevealedTiles != null && entry.RevealedTiles[idx]))
                        continue;
                    float x0 = tx * tileSize;
                    float y0 = ty * tileSize;
                    float x1 = Mathf.Min(entry.WidthPixels, x0 + tileSize);
                    float y1 = Mathf.Min(entry.HeightPixels, y0 + tileSize);
                    float cx = (x0 + x1) * 0.5f;
                    float cy = (y0 + y1) * 0.5f;
                    float dx = cx - seedX;
                    float dy = cy - seedY;
                    float d = dx * dx + dy * dy;
                    float topPenalty = Mathf.Clamp01((entry.CenterY - cy) / Mathf.Max(1.0f, entry.HeightPixels));
                    d += topPenalty * topPenalty * (entry.WidthPixels * entry.HeightPixels * 0.2f);
                    if (d < best)
                    {
                        best = d;
                        bestIdx = idx;
                    }
                }
            }
            return bestIdx;
        }

        private static void C2SmpAddFrontierTileV89LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, int tx, int ty)
        {
            if (entry == null || entry.RevealedTiles == null || entry.FrontierMask == null || entry.FrontierTiles == null)
                return;
            if (tx < 0 || ty < 0 || tx >= entry.TileCols || ty >= entry.TileRows)
                return;
            int idx = ty * entry.TileCols + tx;
            if (idx < 0 || idx >= entry.RevealedTiles.Length)
                return;
            if (entry.RevealedTiles[idx] || entry.FrontierMask[idx] || !C2SmpIsActiveTileV90LikeOriginal(entry, idx))
                return;
            entry.FrontierMask[idx] = true;
            entry.FrontierTiles.Add(idx);
        }

        private static void C2SmpRevealOneTileV89LikeOriginal(C2SmpPaintChunkEntryV84LikeOriginal entry, int tileIndex, int tileSize, ref int budget, out int revealedPixels)
        {
            revealedPixels = 0;
            if (entry == null || entry.RevealedTiles == null || entry.TargetPixels == null || entry.WorkingPixels == null)
                return;
            if (tileIndex < 0 || tileIndex >= entry.RevealedTiles.Length || entry.RevealedTiles[tileIndex] || !C2SmpIsActiveTileV90LikeOriginal(entry, tileIndex))
                return;

            int tx = tileIndex % entry.TileCols;
            int ty = tileIndex / entry.TileCols;
            int x0 = tx * tileSize;
            int y0 = ty * tileSize;
            int x1 = Mathf.Min(entry.WidthPixels, x0 + tileSize);
            int y1 = Mathf.Min(entry.HeightPixels, y0 + tileSize);
            if (x1 <= x0 || y1 <= y0)
                return;

            int tilePixels = (x1 - x0) * (y1 - y0);
            if (tilePixels > budget && budget > 0)
                budget = tilePixels;

            byte revealAlpha = 255;
            if (entry.TileRevealAlpha != null && tileIndex >= 0 && tileIndex < entry.TileRevealAlpha.Length && entry.TileRevealAlpha[tileIndex] > 0)
                revealAlpha = entry.TileRevealAlpha[tileIndex];

            for (int y = y0; y < y1; y++)
            {
                int row = y * entry.WidthPixels;
                for (int x = x0; x < x1; x++)
                {
                    int pi = row + x;
                    if (pi >= 0 && pi < entry.WorkingPixels.Length && pi < entry.TargetPixels.Length)
                    {
                        Color32 target = entry.TargetPixels[pi];
                        if (revealAlpha >= 255 || entry.BasePixels == null || pi >= entry.BasePixels.Length)
                            entry.WorkingPixels[pi] = target;
                        else
                            entry.WorkingPixels[pi] = C2SmpLerpColor32V91LikeOriginal(entry.BasePixels[pi], target, revealAlpha);
                    }
                }
            }

            entry.RevealedTiles[tileIndex] = true;
            if (entry.FrontierMask != null && tileIndex < entry.FrontierMask.Length)
                entry.FrontierMask[tileIndex] = false;
            entry.RevealedCount++;
            budget -= tilePixels;
            if (budget < 0) budget = 0;
            revealedPixels = tilePixels;
            C2SmpMarkDirtyUploadV87LikeOriginal(entry, x0, y0, x1, y1);

            C2SmpAddFrontierTileV89LikeOriginal(entry, tx + 1, ty);
            C2SmpAddFrontierTileV89LikeOriginal(entry, tx - 1, ty);
            C2SmpAddFrontierTileV89LikeOriginal(entry, tx, ty + 1);
            C2SmpAddFrontierTileV89LikeOriginal(entry, tx, ty - 1);
        }

        private static bool C2SmpRevealPixelsForEntryV84LikeOriginal(
            C2SmpPaintChunkEntryV84LikeOriginal entry,
            float progress,
            ref int budget,
            out int revealedThisEntry)
        {
            revealedThisEntry = 0;
            if (entry == null || entry.RevealedTiles == null || entry.WorkingPixels == null ||
                entry.TargetPixels == null || entry.WidthPixels <= 0 || entry.HeightPixels <= 0 ||
                entry.TileCols <= 0 || entry.TileRows <= 0 || budget <= 0)
                return false;

            bool changed = false;
            float threshold = Mathf.Clamp01(progress);
            float centerX = entry.CenterX;
            float centerY = entry.CenterY;
            float maxDist = Mathf.Max(1.0f, entry.MaxDistance);
            int tileSize = Mathf.Max(1, C2SmpProgressiveTileSizeV87LikeOriginal);
            int totalTiles = entry.TileCols * entry.TileRows;
            int revealableTiles = entry.ActiveTileCount > 0 ? entry.ActiveTileCount : totalTiles;
            int targetRevealCount = Mathf.Clamp(Mathf.CeilToInt(revealableTiles * threshold), 1, revealableTiles);
            if (threshold >= 0.999f)
                targetRevealCount = revealableTiles;

            if (!entry.FrontierInitialized)
            {
                entry.FrontierInitialized = true;
                int seed = C2SmpFindNearestActiveTileToCenterV90LikeOriginal(entry);
                int revealed;
                C2SmpRevealOneTileV89LikeOriginal(entry, seed, tileSize, ref budget, out revealed);
                if (revealed > 0)
                {
                    revealedThisEntry += revealed;
                    changed = true;
                }
            }

            while (budget > 0 && entry.RevealedCount < targetRevealCount)
            {
                if (entry.FrontierTiles == null || entry.FrontierTiles.Count == 0)
                {
                    if (entry.ActiveTileCount > 0 && entry.RevealedCount < targetRevealCount)
                    {
                        int seed2 = C2SmpFindNearestActiveTileToCenterV90LikeOriginal(entry);
                        int revealed2;
                        C2SmpRevealOneTileV89LikeOriginal(entry, seed2, tileSize, ref budget, out revealed2);
                        if (revealed2 > 0)
                        {
                            revealedThisEntry += revealed2;
                            changed = true;
                            continue;
                        }
                    }
                    break;
                }

                float revealedRatio = revealableTiles > 0 ? (entry.RevealedCount / (float)revealableTiles) : 1.0f;
                int bestListIndex = -1;
                float bestScore = float.MaxValue;
                for (int i = 0; i < entry.FrontierTiles.Count; i++)
                {
                    int t = entry.FrontierTiles[i];
                    if (t < 0 || t >= totalTiles || entry.RevealedTiles[t] || !C2SmpIsActiveTileV90LikeOriginal(entry, t))
                        continue;
                    int tx = t % entry.TileCols;
                    int ty = t / entry.TileCols;
                    int x0 = tx * tileSize;
                    int y0 = ty * tileSize;
                    int x1 = Mathf.Min(entry.WidthPixels, x0 + tileSize);
                    int y1 = Mathf.Min(entry.HeightPixels, y0 + tileSize);
                    float tileCx = (x0 + x1) * 0.5f;
                    float tileCy = (y0 + y1) * 0.5f;

                    bool topBackHalf = tileCy < centerY - tileSize * 0.15f;
                    if (topBackHalf && revealedRatio < C2SmpFrontBottomGateRatioV91LikeOriginal && progress < C2SmpFrontBottomGateProgressV91LikeOriginal)
                        continue;

                    float d = Vector2.Distance(new Vector2(tileCx, tileCy), new Vector2(centerX, centerY)) / maxDist;
                    float noise = Mathf.PerlinNoise((tileCx + entry.NoiseSeed) * 0.03125f, (tileCy - entry.NoiseSeed) * 0.03125f) * 0.16f - 0.08f;
                    float bottomBias = Mathf.Clamp01((centerY - tileCy) / Mathf.Max(1.0f, entry.HeightPixels)) * 0.33f;
                    float earlyBottomBonus = (revealedRatio < 0.45f && tileCy >= centerY) ? -0.10f : 0.0f;
                    float score = d + noise + bottomBias + earlyBottomBonus;
                    if (score > threshold + 0.14f && progress < 0.999f)
                        continue;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestListIndex = i;
                    }
                }

                if (bestListIndex < 0)
                {
                    if (entry.FrontierTiles != null)
                        entry.FrontierTiles.Clear();
                    continue;
                }

                int tileIndex = entry.FrontierTiles[bestListIndex];
                entry.FrontierTiles.RemoveAt(bestListIndex);
                int revealed;
                C2SmpRevealOneTileV89LikeOriginal(entry, tileIndex, tileSize, ref budget, out revealed);
                if (revealed <= 0)
                    continue;
                revealedThisEntry += revealed;
                changed = true;
            }

            return changed;
        }

        private void C2SmpRebakeDirtySoftwareTerrainChunksLikeOriginal(
            int dirtyMinCellX,
            int dirtyMinCellY,
            int dirtyMaxCellXExclusive,
            int dirtyMaxCellYExclusive,
            out int rebakedChunks,
            out string audit)
        {
            rebakedChunks = 0;
            audit = "not_started";

            if (_map == null || _terrainRoot == null || !_terrainBuilt)
            {
                audit = "no_terrain";
                return;
            }

            TerrainSoftwareBakeInputsLikeOriginal inputs = PrepareTerrainSoftwareBakeInputsLikeOriginal();
            if (inputs == null || inputs.GroundAtlas == null || inputs.GroundPixels == null || inputs.GroundPixels.Length == 0)
            {
                audit = "no_bake_inputs";
                return;
            }

            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);

            _lastBuiltTerrainKernel = kernel;
            _hasLastBuiltTerrainKernel = true;

            if (!TerrainQualityFactureLayerDisabledLikeAdapted && HasFactureLayerDataLikeOriginal(_map))
                PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal(inputs);

            int totalCellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int totalCellsY = Mathf.Max(0, kernel.MaxCellYExclusive - kernel.MinCellY);
            int chunkCountX = Mathf.Max(1, Mathf.CeilToInt(totalCellsX / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int chunkCountY = Mathf.Max(1, Mathf.CeilToInt(totalCellsY / (float)TerrainSoftwareChunkCellsLikeOriginal));

            int minChunkX = Mathf.Clamp(Mathf.FloorToInt((dirtyMinCellX - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int maxChunkX = Mathf.Clamp(Mathf.FloorToInt((dirtyMaxCellXExclusive - 1 - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int minChunkY = Mathf.Clamp(Mathf.FloorToInt((dirtyMinCellY - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);
            int maxChunkY = Mathf.Clamp(Mathf.FloorToInt((dirtyMaxCellYExclusive - 1 - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);

            int missing = 0;
            int failed = 0;

            for (int cy = minChunkY; cy <= maxChunkY; cy++)
            {
                int minCellY = kernel.MinCellY + cy * TerrainSoftwareChunkCellsLikeOriginal;
                int maxCellYExclusive = Mathf.Min(kernel.MaxCellYExclusive, minCellY + TerrainSoftwareChunkCellsLikeOriginal);

                for (int cx = minChunkX; cx <= maxChunkX; cx++)
                {
                    int minCellX = kernel.MinCellX + cx * TerrainSoftwareChunkCellsLikeOriginal;
                    int maxCellXExclusive = Mathf.Min(kernel.MaxCellXExclusive, minCellX + TerrainSoftwareChunkCellsLikeOriginal);
                    if (maxCellXExclusive <= minCellX || maxCellYExclusive <= minCellY)
                        continue;

                    TerrainSoftwareChunkRegionLikeOriginal region = CreateTerrainSoftwareChunkRegionLikeOriginal(
                        _map,
                        kernel,
                        minCellX,
                        maxCellXExclusive,
                        minCellY,
                        maxCellYExclusive);

                    Color32[] pixels = null;
                    try
                    {
                        pixels = BakeTerrainChunkPixelsSoftwareLikeOriginal(_map, kernel, region, inputs);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Debug.LogWarning("[C2:SMP V85 REBAKE FAIL] chunk=" + cx.ToString(CultureInfo.InvariantCulture) + "/" + cy.ToString(CultureInfo.InvariantCulture) +
                                         " error=" + ex.GetType().Name + ": " + ex.Message);
                        continue;
                    }

                    Texture2D tex = CreateTerrainSoftwareChunkTextureFromPixelsLikeOriginal(region, pixels, cx, cy);
                    if (tex == null)
                    {
                        failed++;
                        continue;
                    }

                    string chunkName = string.Format(CultureInfo.InvariantCulture, "TerrainChunkSoftware_{0:00}_{1:00}", cx, cy);
                    Transform chunkTr = _terrainRoot.transform.Find(chunkName);
                    MeshRenderer mr = chunkTr != null ? chunkTr.GetComponent<MeshRenderer>() : null;
                    if (mr == null)
                    {
                        SafeDestroy(tex);
                        missing++;
                        continue;
                    }

                    Material newMat = CreateSoftwareBakedTerrainChunkMaterialLikeOriginal(tex, cx, cy);
                    if (newMat == null)
                    {
                        SafeDestroy(tex);
                        failed++;
                        continue;
                    }

                    Material oldMat = mr.sharedMaterial;
                    Texture oldTex = oldMat != null ? oldMat.mainTexture : null;
                    mr.sharedMaterial = newMat;
                    rebakedChunks++;

                    // Do not destroy built-in/project assets. Runtime chunk materials/textures are safe to release.
                    if (oldMat != null && oldMat.name.StartsWith("C2_TerrainSoftwareChunk_", StringComparison.OrdinalIgnoreCase))
                        SafeDestroy(oldMat);
                    if (oldTex != null && oldTex.name.StartsWith("TerrainChunkSoftware_", StringComparison.OrdinalIgnoreCase))
                        SafeDestroy(oldTex);
                }
            }

            audit = "chunks=" + minChunkX.ToString(CultureInfo.InvariantCulture) + "/" + minChunkY.ToString(CultureInfo.InvariantCulture) +
                    "-" + maxChunkX.ToString(CultureInfo.InvariantCulture) + "/" + maxChunkY.ToString(CultureInfo.InvariantCulture) +
                    " rebaked=" + rebakedChunks.ToString(CultureInfo.InvariantCulture) +
                    " missing=" + missing.ToString(CultureInfo.InvariantCulture) +
                    " failed=" + failed.ToString(CultureInfo.InvariantCulture);
        }

        private bool C2SmpTryLoadPieceLikeOriginal(string pieceNameRaw, out C2SmpPieceLikeOriginal piece, out string audit)
        {
            piece = null;
            audit = "not_started";

            string cacheKey = C2SmpNormalizePieceNameLikeOriginal(pieceNameRaw);
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                audit = "empty_piece";
                return false;
            }

            C2SmpPieceLikeOriginal cached;
            if (_c2SmpRuntimePieceCacheV82LikeOriginal.TryGetValue(cacheKey, out cached))
            {
                piece = cached;
                audit = "cache_hit path='" + cached.SourcePath + "'";
                return true;
            }

            byte[] data;
            string path;
            if (!C2SmpTryReadPieceBytesLikeOriginal(cacheKey, out data, out path, out audit))
                return false;

            if (!C2SmpTryParsePieceBytesLikeOriginal(cacheKey, path, data, out piece, out audit))
                return false;

            _c2SmpRuntimePieceCacheV82LikeOriginal[cacheKey] = piece;
            return true;
        }

        private bool C2SmpTryReadPieceBytesLikeOriginal(string normalizedPieceName, out byte[] data, out string path, out string audit)
        {
            data = null;
            path = string.Empty;
            audit = "not_found";

            string slash = normalizedPieceName.Replace('\\', '/');
            string back = normalizedPieceName.Replace('/', '\\');

            List<string> rels = new List<string>();
            rels.Add(slash);
            rels.Add(back);
            if (!slash.EndsWith(".smp", StringComparison.OrdinalIgnoreCase))
            {
                rels.Add(slash + ".smp");
                rels.Add(back + ".smp");
            }

            if (_bootstrap != null && _bootstrap.Fs != null)
            {
                for (int i = 0; i < rels.Count; i++)
                {
                    string rel = rels[i];
                    try
                    {
                        if (_bootstrap.Fs.Exists(rel))
                        {
                            data = _bootstrap.Fs.ReadAllBytes(rel);
                            path = rel;
                            audit = "fs_hit rel='" + rel + "'";
                            return data != null && data.Length > 0;
                        }
                    }
                    catch { }
                }
            }

            List<string> roots = C2Settlement3InuMdV2DataRootsLikeOriginal();
            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    continue;

                for (int i = 0; i < rels.Count; i++)
                {
                    string p = Path.Combine(root, rels[i]);
                    if (!File.Exists(p))
                        continue;
                    data = File.ReadAllBytes(p);
                    path = p;
                    audit = "file_hit path='" + p + "'";
                    return data != null && data.Length > 0;
                }
            }

            audit = "not_found piece='" + normalizedPieceName + "'";
            return false;
        }

        private static string C2SmpNormalizePieceNameLikeOriginal(string pieceNameRaw)
        {
            if (string.IsNullOrWhiteSpace(pieceNameRaw))
                return string.Empty;
            string s = pieceNameRaw.Trim().Trim('"').Replace('/', '\\');
            while (s.StartsWith("\\", StringComparison.Ordinal))
                s = s.Substring(1);
            return s;
        }

        private static bool C2SmpTryParsePieceBytesLikeOriginal(
            string pieceName,
            string sourcePath,
            byte[] data,
            out C2SmpPieceLikeOriginal piece,
            out string audit)
        {
            piece = null;
            audit = "not_started";

            if (data == null || data.Length < 16)
            {
                audit = "too_small";
                return false;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    piece = new C2SmpPieceLikeOriginal();
                    piece.PieceName = pieceName ?? string.Empty;
                    piece.SourcePath = sourcePath ?? string.Empty;

                    bool parsedVertex = false;

                    string magic8 = Encoding.ASCII.GetString(br.ReadBytes(Mathf.Min(8, data.Length)));
                    if (magic8 == "SAMPVER3")
                    {
                        // SAMPVER3 stores the vertex block directly after the 8-byte magic:
                        // int blockSize, int nVert, then nVert records.
                        if (!C2SmpParseVertexBlockLikeOriginal(br, piece, true))
                        {
                            audit = "bad_sampver3_vertex";
                            return false;
                        }
                        parsedVertex = true;
                    }
                    else
                    {
                        br.BaseStream.Position = 0;
                        string tag = C2SmpReadTagLikeOriginal(br);
                        if (!string.Equals(tag, "PMAS", StringComparison.OrdinalIgnoreCase))
                        {
                            audit = "bad_magic '" + tag + "'/'" + magic8 + "'";
                            return false;
                        }
                    }

                    while (br.BaseStream.Position + 8 <= br.BaseStream.Length)
                    {
                        string tag = C2SmpReadTagLikeOriginal(br);

                        if (tag == "\u00FF\u00FF\u00FF\u00FF" || tag == "EOF\u0000")
                            break;

                        int blockSize = br.ReadInt32();
                        if (blockSize < 8)
                            break;

                        long payloadStart = br.BaseStream.Position;
                        long payloadEnd = payloadStart + (blockSize - 8);
                        if (payloadEnd > br.BaseStream.Length)
                            payloadEnd = br.BaseStream.Length;

                        if (tag == "TREV" || tag == "3REV")
                        {
                            br.BaseStream.Position = payloadStart;
                            C2SmpParseVertexPayloadLikeOriginal(br, piece, payloadEnd);
                            parsedVertex = true;
                        }
                        else if (tag == "OBJS")
                        {
                            br.BaseStream.Position = payloadStart;
                            C2SmpParseObjsPayloadLikeOriginal(br, piece, payloadEnd);
                        }
                        else if (tag == "PIX1")
                        {
                            br.BaseStream.Position = payloadStart;
                            C2SmpParsePix1HeaderLikeOriginal(br, piece, payloadEnd);
                        }
                        else if (tag == "NRG1")
                        {
                            br.BaseStream.Position = payloadStart;
                            C2SmpParseNrg1HeaderLikeOriginal(br, piece, payloadEnd);
                        }
                        else if (tag == "SPRT" || tag == "GSPR" || tag == "KCOL" || tag == "ZONЕ" || tag == "ZONE")
                        {
                            // Not needed for terrain underlay V82.
                        }

                        br.BaseStream.Position = payloadEnd;
                    }

                    if (!parsedVertex || piece.Vertices.Count == 0)
                    {
                        audit = "no_vertices";
                        return false;
                    }

                    audit = "ok vertices=" + piece.Vertices.Count.ToString(CultureInfo.InvariantCulture) +
                            " objs=" + piece.Objects.Count.ToString(CultureInfo.InvariantCulture) +
                            " nrg=" + (piece.HasNrg1 ? (piece.NrgNx.ToString(CultureInfo.InvariantCulture) + "x" + piece.NrgNy.ToString(CultureInfo.InvariantCulture) +
                            " active=" + piece.NrgActivePointCount.ToString(CultureInfo.InvariantCulture) + "/" + piece.NrgPointCount.ToString(CultureInfo.InvariantCulture)) : "none") +
                            " pixSquares=" + piece.PixSquareCount.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch (Exception ex)
            {
                piece = null;
                audit = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static string C2SmpReadTagLikeOriginal(BinaryReader br)
        {
            byte[] b = br.ReadBytes(4);
            if (b.Length < 4) return string.Empty;
            return Encoding.ASCII.GetString(b);
        }

        private static bool C2SmpParseVertexBlockLikeOriginal(BinaryReader br, C2SmpPieceLikeOriginal piece, bool hasBlockSize)
        {
            if (hasBlockSize)
            {
                int blockSize = br.ReadInt32();
                long payloadEnd = br.BaseStream.Position + Mathf.Max(0, blockSize - 4);
                return C2SmpParseVertexPayloadLikeOriginal(br, piece, payloadEnd);
            }

            return C2SmpParseVertexPayloadLikeOriginal(br, piece, br.BaseStream.Length);
        }

        private static bool C2SmpParseVertexPayloadLikeOriginal(BinaryReader br, C2SmpPieceLikeOriginal piece, long payloadEnd)
        {
            if (br.BaseStream.Position + 4 > payloadEnd)
                return false;

            int n = br.ReadInt32();
            if (n < 0 || n > 100000)
                return false;

            piece.MinVertexX = short.MaxValue;
            piece.MinVertexY = short.MaxValue;
            piece.MaxVertexX = short.MinValue;
            piece.MaxVertexY = short.MinValue;

            for (int i = 0; i < n && br.BaseStream.Position + 14 <= payloadEnd; i++)
            {
                C2SmpVertexRecordLikeOriginal v = new C2SmpVertexRecordLikeOriginal();
                v.X = br.ReadInt16();
                v.Y = br.ReadInt16();
                v.Tex = br.ReadByte();
                v.Facture = br.ReadByte();
                v.FactureWeight = br.ReadByte();
                v.ExtraTex = br.ReadByte();
                v.ExtraWeight = br.ReadByte();
                v.S1 = br.ReadByte();
                v.S2 = br.ReadByte();
                v.S3 = br.ReadByte();
                v.Height = br.ReadInt16();

                piece.Vertices.Add(v);
                if (v.X < piece.MinVertexX) piece.MinVertexX = v.X;
                if (v.X > piece.MaxVertexX) piece.MaxVertexX = v.X;
                if (v.Y < piece.MinVertexY) piece.MinVertexY = v.Y;
                if (v.Y > piece.MaxVertexY) piece.MaxVertexY = v.Y;
            }

            return piece.Vertices.Count > 0;
        }

        private static void C2SmpParseObjsPayloadLikeOriginal(BinaryReader br, C2SmpPieceLikeOriginal piece, long payloadEnd)
        {
            if (br.BaseStream.Position + 4 > payloadEnd)
                return;

            int n = br.ReadInt32();
            if (n < 0 || n > 4096)
                return;

            for (int i = 0; i < n && br.BaseStream.Position + 9 <= payloadEnd; i++)
            {
                C2SmpObjAnchorLikeOriginal obj = new C2SmpObjAnchorLikeOriginal();
                obj.X = br.ReadInt32();
                obj.Y = br.ReadInt32();
                obj.Nation = br.ReadByte();

                List<byte> nameBytes = new List<byte>(64);
                while (br.BaseStream.Position < payloadEnd)
                {
                    byte c = br.ReadByte();
                    if (c == 0)
                        break;
                    nameBytes.Add(c);
                }

                try { obj.Name = Encoding.GetEncoding(1251).GetString(nameBytes.ToArray()); }
                catch { obj.Name = Encoding.ASCII.GetString(nameBytes.ToArray()); }

                piece.Objects.Add(obj);

                // SMP object records are variable in old samples; for V82 anchor we only need the first.
                // If there are more records, stop safely rather than guessing a wrong stride.
                if (i == 0)
                    break;
            }
        }

        private static void C2SmpParsePix1HeaderLikeOriginal(BinaryReader br, C2SmpPieceLikeOriginal piece, long payloadEnd)
        {
            piece.HasPix1 = true;
            if (br.BaseStream.Position + 8 > payloadEnd)
                return;

            int rawSize = br.ReadInt32();
            int nSquares = br.ReadInt32();
            piece.PixSquareCount = Mathf.Max(0, nSquares);

            // Full packed pixel payload is not used in V82 because AusKuz.smp has zero payload bytes.
            // Keep count only for audit; future pieces can implement SetPixelsFromBuf here.
            _ = rawSize;
        }

        private static void C2SmpParseNrg1HeaderLikeOriginal(BinaryReader br, C2SmpPieceLikeOriginal piece, long payloadEnd)
        {
            piece.HasNrg1 = true;
            if (br.BaseStream.Position + 16 > payloadEnd)
                return;

            // Original block is file tag '1GRN' (read as NRG1 here):
            //   DWORD sz; then GetGroundIntoBuf payload:
            //   WORD nx, WORD ny, WORD dx, WORD dy, WORD dx0, WORD dy0,
            //   then nx*ny records: BYTE W, WORD PointTexIndex, WORD z0.
            int rawSize = br.ReadInt32();
            piece.NrgNx = br.ReadInt16();
            piece.NrgNy = br.ReadInt16();
            piece.NrgDx = br.ReadInt16();
            piece.NrgDy = br.ReadInt16();
            piece.NrgDx0 = br.ReadInt16();
            piece.NrgDy0 = br.ReadInt16();

            int possible = Mathf.Max(0, piece.NrgNx * piece.NrgNy);
            int availableByBlock = Mathf.Max(0, (int)((payloadEnd - br.BaseStream.Position) / 5));
            int availableByRaw = rawSize > 12 ? Mathf.Max(0, (rawSize - 12) / 5) : availableByBlock;
            piece.NrgPointCount = Mathf.Min(possible, Mathf.Min(availableByBlock, availableByRaw));
            piece.NrgActivePointCount = 0;

            if (piece.NrgPointCount <= 0)
            {
                _ = rawSize;
                return;
            }

            piece.NrgPoints = new C2SmpGroundPointLikeOriginal[piece.NrgPointCount];
            for (int i = 0; i < piece.NrgPointCount && br.BaseStream.Position + 5 <= payloadEnd; i++)
            {
                C2SmpGroundPointLikeOriginal gp = new C2SmpGroundPointLikeOriginal();
                gp.Weight = br.ReadByte();
                gp.TexIndex = br.ReadUInt16();
                gp.Z = br.ReadInt16();
                piece.NrgPoints[i] = gp;

                // Original uses signed char W and applies only W > 0.
                if (gp.Weight > 0 && gp.Weight < 128)
                    piece.NrgActivePointCount++;
            }

            _ = rawSize;
        }
        private void C2SmpQueueOverlayJobV93LikeOriginal(
            string applyKey,
            C2SmpPieceLikeOriginal piece,
            string pieceName,
            string mdName,
            string source,
            int changedVertices,
            int dirtyMinCellX,
            int dirtyMinCellY,
            int dirtyMaxCellXExclusive,
            int dirtyMaxCellYExclusive,
            out int queuedChunks,
            out string audit)
        {
            queuedChunks = 0;
            audit = "not_started";

            if (_map == null || _terrainRoot == null || !_terrainBuilt)
            {
                audit = "no_terrain";
                return;
            }

            if (!object.ReferenceEquals(_c2SmpOverlayWorkerMapRefV93LikeOriginal, _map))
            {
                _c2SmpOverlayJobsV93LikeOriginal.Clear();
                _c2SmpOverlayInstancesV93LikeOriginal.Clear();
                _c2SmpOverlayWorkerMapRefV93LikeOriginal = _map;
            }

            int paddedMinX = dirtyMinCellX - C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            int paddedMinY = dirtyMinCellY - C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            int paddedMaxX = dirtyMaxCellXExclusive + C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            int paddedMaxY = dirtyMaxCellYExclusive + C2SmpProgressiveDirtyPaddingCellsV84LikeOriginal;
            if (_map != null)
            {
                paddedMinX = Mathf.Clamp(paddedMinX, 0, Mathf.Max(0, _map.VertInLine - 2));
                paddedMinY = Mathf.Clamp(paddedMinY, 0, Mathf.Max(0, _map.MaxTH - 2));
                paddedMaxX = Mathf.Clamp(paddedMaxX, paddedMinX + 1, Mathf.Max(1, _map.VertInLine - 1));
                paddedMaxY = Mathf.Clamp(paddedMaxY, paddedMinY + 1, Mathf.Max(1, _map.MaxTH - 1));
            }

            C2SmpOverlayJobV93LikeOriginal job = new C2SmpOverlayJobV93LikeOriginal();
            job.ApplyKey = applyKey ?? string.Empty;
            job.Piece = piece;
            job.PieceName = pieceName ?? string.Empty;
            job.MdName = mdName ?? string.Empty;
            job.Source = source ?? string.Empty;
            job.DirtyMinCellX = paddedMinX;
            job.DirtyMinCellY = paddedMinY;
            job.DirtyMaxCellXExclusive = paddedMaxX;
            job.DirtyMaxCellYExclusive = paddedMaxY;
            job.ChangedVertices = changedVertices;
            job.CreatedRealtime = Time.realtimeSinceStartup;
            _c2SmpOverlayJobsV93LikeOriginal.Enqueue(job);

            queuedChunks = C2SmpEstimateDirtyChunkCountV84LikeOriginal(new C2SmpPaintJobV84LikeOriginal
            {
                PieceName = job.PieceName, MdName = job.MdName, Source = job.Source,
                DirtyMinCellX = job.DirtyMinCellX, DirtyMinCellY = job.DirtyMinCellY,
                DirtyMaxCellXExclusive = job.DirtyMaxCellXExclusive, DirtyMaxCellYExclusive = job.DirtyMaxCellYExclusive,
                ChangedVertices = job.ChangedVertices, CreatedRealtime = job.CreatedRealtime
            });

            audit = "overlay queued dirtyCells=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                    "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                    " estimatedChunks=" + queuedChunks.ToString(CultureInfo.InvariantCulture) +
                    " alphaMode=" + (C2SmpOverlayUseTimeFallbackV93LikeOriginal ? "time_fallback" : "build_stage");

            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 JOB] piece='" + job.PieceName + "'" +
                      " md='" + job.MdName + "'" +
                      " dirtyRect=" + job.DirtyMinCellX.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMinCellY.ToString(CultureInfo.InvariantCulture) +
                      "-" + job.DirtyMaxCellXExclusive.ToString(CultureInfo.InvariantCulture) + "/" + job.DirtyMaxCellYExclusive.ToString(CultureInfo.InvariantCulture) +
                      " chunks=" + queuedChunks.ToString(CultureInfo.InvariantCulture) +
                      " mode=overlay_alpha source='" + job.Source + "'");

            if (_c2SmpOverlayWorkerV93LikeOriginal == null)
                _c2SmpOverlayWorkerV93LikeOriginal = StartCoroutine(C2SmpOverlayWorkerV93LikeOriginal());
        }

        private IEnumerator C2SmpOverlayWorkerV93LikeOriginal()
        {
            while (_c2SmpOverlayJobsV93LikeOriginal.Count > 0)
            {
                C2SmpOverlayJobV93LikeOriginal job = _c2SmpOverlayJobsV93LikeOriginal.Dequeue();
                if (job != null)
                    yield return StartCoroutine(C2SmpRunOverlayJobV93LikeOriginal(job));
                yield return null;
            }
            _c2SmpOverlayWorkerV93LikeOriginal = null;
        }

        private long C2SmpPrewarmOverlayBakeProbeV103LikeOriginal(
            ParsedMap map,
            OriginalTerrainKernelConfig kernel,
            TerrainSoftwareBakeInputsLikeOriginal inputs,
            int minCellX,
            int maxCellXExclusive,
            int minCellY,
            int maxCellYExclusive,
            bool dense,
            string pieceName,
            string chunkName)
        {
            if (map == null || inputs == null || maxCellXExclusive <= minCellX || maxCellYExclusive <= minCellY)
                return 0;

            int grid = dense ? C2SmpOverlayDenseProbeGridV103LikeOriginal : C2SmpOverlayProbeGridV103LikeOriginal;
            grid = Mathf.Clamp(grid, 1, 9);
            var sw = global::System.Diagnostics.Stopwatch.StartNew();
            int probes = 0;
            int fails = 0;

            for (int gy = 0; gy < grid; gy++)
            {
                float fy = grid <= 1 ? 0.5f : gy / (float)(grid - 1);
                int cy = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minCellY, Mathf.Max(minCellY, maxCellYExclusive - 2), fy)), minCellY, Mathf.Max(minCellY, maxCellYExclusive - 2));
                for (int gx = 0; gx < grid; gx++)
                {
                    float fx = grid <= 1 ? 0.5f : gx / (float)(grid - 1);
                    int cx = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minCellX, Mathf.Max(minCellX, maxCellXExclusive - 2), fx)), minCellX, Mathf.Max(minCellX, maxCellXExclusive - 2));
                    try
                    {
                        TerrainSoftwareChunkRegionLikeOriginal probeRegion = CreateTerrainSoftwareChunkRegionLikeOriginal(
                            map, kernel, cx, Mathf.Min(cx + 1, maxCellXExclusive), cy, Mathf.Min(cy + 1, maxCellYExclusive));
                        Color32[] probe = BakeTerrainChunkPixelsSoftwareLikeOriginal(map, kernel, probeRegion, inputs);
                        if (probe == null || probe.Length == 0)
                            fails++;
                    }
                    catch
                    {
                        fails++;
                    }
                    probes++;
                }
            }

            sw.Stop();
            if (fails > 0 || sw.ElapsedMilliseconds >= 16)
            {
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 TARGET PROBE AUDIT] piece='" + (pieceName ?? string.Empty) + "' chunk=" + (chunkName ?? string.Empty) +
                          " dense=" + dense.ToString() +
                          " probes=" + probes.ToString(CultureInfo.InvariantCulture) +
                          " fails=" + fails.ToString(CultureInfo.InvariantCulture) +
                          " ms=" + sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
            }
            return sw.ElapsedMilliseconds;
        }

        private IEnumerator C2SmpRunOverlayJobV93LikeOriginal(C2SmpOverlayJobV93LikeOriginal job)
        {
            if (job == null || _map == null || _terrainRoot == null || !_terrainBuilt)
                yield break;

            var totalSw = global::System.Diagnostics.Stopwatch.StartNew();
            List<C2SmpOverlayChunkBuildV93LikeOriginal> chunks = new List<C2SmpOverlayChunkBuildV93LikeOriginal>();
            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel ? _lastBuiltTerrainKernel : CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            int totalCellsX = Mathf.Max(0, kernel.MaxCellXExclusive - kernel.MinCellX);
            int totalCellsY = Mathf.Max(0, kernel.MaxCellYExclusive - kernel.MinCellY);
            int chunkCountX = Mathf.Max(1, Mathf.CeilToInt(totalCellsX / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int chunkCountY = Mathf.Max(1, Mathf.CeilToInt(totalCellsY / (float)TerrainSoftwareChunkCellsLikeOriginal));
            int minChunkX = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMinCellX - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int maxChunkX = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMaxCellXExclusive - 1 - kernel.MinCellX) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountX - 1);
            int minChunkY = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMinCellY - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);
            int maxChunkY = Mathf.Clamp(Mathf.FloorToInt((job.DirtyMaxCellYExclusive - 1 - kernel.MinCellY) / (float)TerrainSoftwareChunkCellsLikeOriginal), 0, chunkCountY - 1);

            TerrainSoftwareBakeInputsLikeOriginal inputs = PrepareTerrainSoftwareBakeInputsLikeOriginal();
            if (inputs == null)
            {
                Debug.LogWarning("[C2:SMP V107 JOB SKIP] piece='" + job.PieceName + "' reason=no_bake_inputs");
                yield break;
            }

            if (!TerrainQualityFactureLayerDisabledLikeAdapted && HasFactureLayerDataLikeOriginal(_map))
            {
                if (!_c2SmpFacturePrewarmedForMapV84LikeOriginal)
                {
                    var prewarmSwV94 = global::System.Diagnostics.Stopwatch.StartNew();
                    PrewarmTerrainSoftwareFactureBakeCacheLikeOriginal(inputs);
                    _c2SmpFacturePrewarmedForMapV84LikeOriginal = true;
                    prewarmSwV94.Stop();
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 PREWARM] piece='" + job.PieceName + "' ms=" + prewarmSwV94.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
                    yield return null;
                }
                else
                {
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 PREWARM SKIP] piece='" + job.PieceName + "' reason=already_warmed");
                }
            }

            TerrainSoftwareChunkRegionLikeOriginal jobRegionV95 = CreateTerrainSoftwareChunkRegionLikeOriginal(_map, kernel, job.DirtyMinCellX, job.DirtyMaxCellXExclusive, job.DirtyMinCellY, job.DirtyMaxCellYExclusive);
            Bounds jobWorldBoundsV95 = jobRegionV95.FootprintBounds;
            if (jobWorldBoundsV95.size.x <= 0.01f || jobWorldBoundsV95.size.z <= 0.01f)
                jobWorldBoundsV95 = new Bounds(jobWorldBoundsV95.center, new Vector3(1.0f, 1.0f, 1.0f));

            int failures = 0;
            for (int cy = minChunkY; cy <= maxChunkY; cy++)
            {
                int fullMinCellY = kernel.MinCellY + cy * TerrainSoftwareChunkCellsLikeOriginal;
                int fullMaxCellYExclusive = Mathf.Min(kernel.MaxCellYExclusive, fullMinCellY + TerrainSoftwareChunkCellsLikeOriginal);
                for (int cx = minChunkX; cx <= maxChunkX; cx++)
                {
                    int fullMinCellX = kernel.MinCellX + cx * TerrainSoftwareChunkCellsLikeOriginal;
                    int fullMaxCellXExclusive = Mathf.Min(kernel.MaxCellXExclusive, fullMinCellX + TerrainSoftwareChunkCellsLikeOriginal);
                    if (fullMaxCellXExclusive <= fullMinCellX || fullMaxCellYExclusive <= fullMinCellY)
                        continue;

                    int subMinCellX = Mathf.Max(job.DirtyMinCellX, fullMinCellX);
                    int subMinCellY = Mathf.Max(job.DirtyMinCellY, fullMinCellY);
                    int subMaxCellX = Mathf.Min(job.DirtyMaxCellXExclusive, fullMaxCellXExclusive);
                    int subMaxCellY = Mathf.Min(job.DirtyMaxCellYExclusive, fullMaxCellYExclusive);
                    if (subMaxCellX <= subMinCellX || subMaxCellY <= subMinCellY)
                        continue;

                    string chunkName = string.Format(CultureInfo.InvariantCulture, "TerrainChunkSoftware_{0:00}_{1:00}", cx, cy);
                    Transform chunkTr = _terrainRoot.transform.Find(chunkName);
                    MeshRenderer mr = chunkTr != null ? chunkTr.GetComponent<MeshRenderer>() : null;
                    MeshFilter sourceMf = chunkTr != null ? chunkTr.GetComponent<MeshFilter>() : null;
                    Texture2D tex = null;
                    if (mr != null && mr.sharedMaterial != null)
                        tex = mr.sharedMaterial.mainTexture as Texture2D;
                    if (tex == null || sourceMf == null || sourceMf.sharedMesh == null)
                    {
                        failures++;
                        Debug.LogWarning("[C2:SMP V103 CHUNK SKIP] piece='" + job.PieceName + "' chunk=" + chunkName + " reason=no_source_texture_or_mesh");
                        continue;
                    }

                    TerrainSoftwareChunkRegionLikeOriginal fullRegion = CreateTerrainSoftwareChunkRegionLikeOriginal(_map, kernel, fullMinCellX, fullMaxCellXExclusive, fullMinCellY, fullMaxCellYExclusive);
                    TerrainSoftwareChunkRegionLikeOriginal subRegion = CreateTerrainSoftwareChunkRegionLikeOriginal(_map, kernel, subMinCellX, subMaxCellX, subMinCellY, subMaxCellY);
                    int targetRawWidth = subRegion.WidthPixels;
                    int targetRawHeight = subRegion.HeightPixels;
                    Bounds sb = subRegion.FootprintBounds;
                    Vector2 sp0 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.min.x, 0.0f, sb.min.z));
                    Vector2 sp1 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.max.x, 0.0f, sb.min.z));
                    Vector2 sp2 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.min.x, 0.0f, sb.max.z));
                    Vector2 sp3 = ProjectWorldToChunkPixelLikeOriginal(fullRegion, new Vector3(sb.max.x, 0.0f, sb.max.z));
                    int px = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(Mathf.Min(sp0.x, sp1.x), Mathf.Min(sp2.x, sp3.x))), 0, Mathf.Max(0, fullRegion.WidthPixels - 1));
                    int py = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(Mathf.Min(sp0.y, sp1.y), Mathf.Min(sp2.y, sp3.y))), 0, Mathf.Max(0, fullRegion.HeightPixels - 1));
                    int pw = Mathf.Min(targetRawWidth, fullRegion.WidthPixels - px);
                    int ph = Mathf.Min(targetRawHeight, fullRegion.HeightPixels - py);
                    if (pw <= 0 || ph <= 0)
                    {
                        failures++;
                        continue;
                    }

                    C2SmpChunkShadowV87LikeOriginal shadow;
                    if (!C2SmpTryGetTerrainChunkShadowV87LikeOriginal(cx, cy, fullRegion.WidthPixels, fullRegion.HeightPixels, out shadow))
                    {
                        failures++;
                        Debug.LogWarning("[C2:SMP V103 CHUNK SKIP] piece='" + job.PieceName + "' chunk=" + chunkName + " reason=no_cpu_shadow");
                        continue;
                    }

                    Color32[] baseSub = C2SmpExtractSubRectV87LikeOriginal(shadow.Pixels, shadow.Width, shadow.Height, px, py, pw, ph);
                    var chunkData = new C2SmpOverlayChunkBuildV93LikeOriginal();
                    chunkData.ChunkName = chunkName;
                    chunkData.ChunkX = cx;
                    chunkData.ChunkY = cy;
                    chunkData.WorldBounds = sb;
                    chunkData.FullWorldBounds = fullRegion.FootprintBounds;
                    chunkData.JobWorldBounds = jobWorldBoundsV95;
                    chunkData.Width = pw;
                    chunkData.Height = ph;
                    chunkData.FullWidth = fullRegion.WidthPixels;
                    chunkData.FullHeight = fullRegion.HeightPixels;
                    chunkData.OffsetPixelsX = px;
                    chunkData.OffsetPixelsY = py;
                    chunkData.SourceMesh = sourceMf.sharedMesh;
                    chunkData.SourceLocalPosition = chunkTr.localPosition;
                    chunkData.SourceLocalRotation = chunkTr.localRotation;
                    chunkData.SourceLocalScale = chunkTr.localScale;
                    chunkData.BasePixels = baseSub;
                    chunkData.Piece = job.Piece;

                    C2SmpChunkBakeResultV88LikeOriginal bake = null;
                    string bakeModeV105 = "async_cached_nrg_mask_no_mainthread_stall";
                    var mapCaptureV105 = _map;
                    var kernelCaptureV105 = kernel;
                    var subRegionCaptureV105 = subRegion;
                    var inputsCaptureV105 = inputs;
                    int targetRawWidthCaptureV105 = targetRawWidth;
                    int targetRawHeightCaptureV105 = targetRawHeight;
                    int pwCaptureV105 = pw;
                    int phCaptureV105 = ph;

                    Task<C2SmpChunkBakeResultV88LikeOriginal> targetTaskV105 = Task.Run(() =>
                    {
                        C2SmpChunkBakeResultV88LikeOriginal res = new C2SmpChunkBakeResultV88LikeOriginal();
                        var bakeSwLocalV105 = global::System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            // V105: run cropped target bake off the Unity main thread again.
                            // V104 fixed the GetPixels32 crash by guarding missed facture cache reads, so the
                            // old async path is safe enough and avoids 500-1600 ms main-thread stalls on big SMP.
                            Color32[] targetRaw = BakeTerrainChunkPixelsSoftwareLikeOriginal(mapCaptureV105, kernelCaptureV105, subRegionCaptureV105, inputsCaptureV105);
                            if (targetRaw != null && targetRaw.Length == targetRawWidthCaptureV105 * targetRawHeightCaptureV105)
                            {
                                Color32[] targetSub = new Color32[pwCaptureV105 * phCaptureV105];
                                for (int yy = 0; yy < phCaptureV105; yy++)
                                {
                                    int srcRow = yy * targetRawWidthCaptureV105;
                                    int dstRow = yy * pwCaptureV105;
                                    if (srcRow >= 0 && srcRow + pwCaptureV105 <= targetRaw.Length &&
                                        dstRow >= 0 && dstRow + pwCaptureV105 <= targetSub.Length)
                                    {
                                        Array.Copy(targetRaw, srcRow, targetSub, dstRow, pwCaptureV105);
                                    }
                                }
                                res.TargetPixels = targetSub;
                                res.Width = pwCaptureV105;
                                res.Height = phCaptureV105;
                            }
                            else
                            {
                                res.Error = "invalid_target_size_async_cached";
                            }
                        }
                        catch (Exception ex)
                        {
                            res.Error = ex.GetType().Name + ": " + ex.Message;
                        }
                        bakeSwLocalV105.Stop();
                        res.BakeMs = bakeSwLocalV105.ElapsedMilliseconds;
                        return res;
                    });

                    while (!targetTaskV105.IsCompleted)
                        yield return null;

                    try
                    {
                        bake = targetTaskV105.Result;
                    }
                    catch (Exception ex)
                    {
                        bake = new C2SmpChunkBakeResultV88LikeOriginal();
                        bake.Error = ex.GetType().Name + ": " + ex.Message;
                    }

                    if (bake != null && bake.BakeMs >= 8)
                    {
                        if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V107 TARGET ASYNC CACHED] piece='" + job.PieceName + "' chunk=" + chunkName +
                                  " ms=" + bake.BakeMs.ToString(CultureInfo.InvariantCulture) +
                                  " pixels=" + pw.ToString(CultureInfo.InvariantCulture) + "x" + ph.ToString(CultureInfo.InvariantCulture) +
                                  " mode=" + bakeModeV105 + C2SmpFpsNowV87LikeOriginal());
                    }
                    yield return null;

                    chunkData.BakeMs = bake != null ? bake.BakeMs : 0;
                    chunkData.TargetPixels = bake != null ? bake.TargetPixels : null;
                    chunkData.Error = bake != null ? bake.Error : "null_bake";
                    if (chunkData.TargetPixels == null || chunkData.TargetPixels.Length != pw * ph)
                    {
                        failures++;
                        Debug.LogWarning("[C2:SMP V107 TARGET FAIL] piece='" + job.PieceName + "' chunk=" + chunkName + " error='" + chunkData.Error + "'");
                        continue;
                    }
                    chunks.Add(chunkData);
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V107 CHUNK READY] piece='" + job.PieceName + "' chunk=" + chunkName +
                              " worldBounds=(" + sb.min.x.ToString("0.##", CultureInfo.InvariantCulture) + "," + sb.min.z.ToString("0.##", CultureInfo.InvariantCulture) + ")-(" + sb.max.x.ToString("0.##", CultureInfo.InvariantCulture) + "," + sb.max.z.ToString("0.##", CultureInfo.InvariantCulture) + ")" +
                              " pixels=" + pw.ToString(CultureInfo.InvariantCulture) + "x" + ph.ToString(CultureInfo.InvariantCulture) +
                              " bakeMs=" + chunkData.BakeMs.ToString(CultureInfo.InvariantCulture) + " bakeMode=" + bakeModeV105 + C2SmpFpsNowV87LikeOriginal());
                    yield return null;
                }
            }

            if (chunks.Count <= 0)
            {
                Debug.LogWarning("[C2:SMP V107 JOB SKIP] piece='" + job.PieceName + "' reason=no_overlay_chunks failed=" + failures.ToString(CultureInfo.InvariantCulture));
                yield break;
            }

            C2SmpOverlayInstanceV93LikeOriginal existing;
            if (_c2SmpOverlayInstancesV93LikeOriginal.TryGetValue(job.ApplyKey, out existing) && existing != null)
            {
                if (existing.Root != null) Destroy(existing.Root);
                _c2SmpOverlayInstancesV93LikeOriginal.Remove(job.ApplyKey);
            }

            C2SmpOverlayInstanceV93LikeOriginal instance = new C2SmpOverlayInstanceV93LikeOriginal();
            instance.Key = job.ApplyKey;
            instance.PieceName = job.PieceName;
            instance.MdName = job.MdName;
            instance.Source = job.Source;
            instance.FadeSeconds = C2SmpOverlayFadeSecondsV93LikeOriginal;
            instance.StartRealtime = Time.realtimeSinceStartup;
            instance.Root = new GameObject("SMPOverlay_" + job.PieceName.Replace('/', '_').Replace('\\', '_'));
            instance.Root.transform.SetParent(_terrainRoot.transform, false);

            for (int i = 0; i < chunks.Count; i++)
            {
                var built = chunks[i];
                Task<C2SmpOverlayTextureBuildResultV108LikeOriginal> overlayBuildTaskV108 = Task.Run(() => C2SmpBuildOverlayTexturePixelsV108LikeOriginal(built));
                while (!overlayBuildTaskV108.IsCompleted)
                    yield return null;

                C2SmpOverlayTextureBuildResultV108LikeOriginal overlayBuildV108 = null;
                try
                {
                    overlayBuildV108 = overlayBuildTaskV108.Result;
                }
                catch (Exception ex)
                {
                    overlayBuildV108 = new C2SmpOverlayTextureBuildResultV108LikeOriginal();
                    overlayBuildV108.Error = ex.GetType().Name + ": " + ex.Message;
                }

                if (overlayBuildV108 == null || !overlayBuildV108.HasTexture || overlayBuildV108.Pixels == null)
                {
                    string skipReasonV108 = overlayBuildV108 != null ? (overlayBuildV108.SkipReason + (string.IsNullOrEmpty(overlayBuildV108.Error) ? string.Empty : " error='" + overlayBuildV108.Error + "'")) : "null_overlay_build";
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V108 OVERLAY TEX SKIP] chunk=" + built.ChunkName + " reason='" + skipReasonV108 + "'");
                    continue;
                }

                if (overlayBuildV108.BuildMs >= 8)
                {
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V108 OVERLAY BUFFER ASYNC] chunk=" + built.ChunkName +
                              " ms=" + overlayBuildV108.BuildMs.ToString(CultureInfo.InvariantCulture) +
                              " cropPixels=" + overlayBuildV108.CropW.ToString(CultureInfo.InvariantCulture) + "x" + overlayBuildV108.CropH.ToString(CultureInfo.InvariantCulture) +
                              " activePixels=" + overlayBuildV108.ActivePixels.ToString(CultureInfo.InvariantCulture) +
                              " useNrgMask=" + (overlayBuildV108.UseNrgMask ? "1" : "0") +
                              C2SmpFpsNowV87LikeOriginal());
                }

                Texture2D overlayTex = C2SmpCreateOverlayTextureFromBuildV108LikeOriginal(built, overlayBuildV108);
                if (overlayTex == null)
                    continue;
                GameObject quad = new GameObject("Overlay_" + built.ChunkName);
                quad.transform.SetParent(instance.Root.transform, false);
                MeshFilter mf = quad.AddComponent<MeshFilter>();
                MeshRenderer mr = quad.AddComponent<MeshRenderer>();
                Material mat = C2SmpCreateOverlayMaterialV93LikeOriginal(overlayTex);
                mr.sharedMaterial = mat;
                if (built.SourceMesh != null)
                {
                    mf.sharedMesh = built.SourceMesh;
                    quad.transform.localPosition = built.SourceLocalPosition;
                    quad.transform.localRotation = built.SourceLocalRotation;
                    quad.transform.localScale = built.SourceLocalScale;
                }
                else
                {
                    mf.sharedMesh = C2SmpCreateOverlayMeshV93LikeOriginal(built.WorldBounds);
                }
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                C2SmpSetOverlayAlphaV93LikeOriginal(mat, 0.0f);
                instance.Chunks.Add(new C2SmpOverlayChunkInstanceV93LikeOriginal { ChunkName = built.ChunkName, GameObject = quad, Renderer = mr, Material = mat, Texture = overlayTex, CurrentAlpha = 0.0f });
                built.BasePixels = null;
                built.TargetPixels = null;
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 OVERLAY CHUNK ADD] piece='" + job.PieceName + "' chunk=" + built.ChunkName + " overlayChunks=" + instance.Chunks.Count.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
                yield return null;
            }
            chunks.Clear();

            if (instance.Chunks.Count <= 0)
            {
                Debug.LogWarning("[C2:SMP V107 JOB SKIP] piece='" + job.PieceName + "' reason=no_active_overlay_chunks_after_mask");
                if (instance.Root != null) Destroy(instance.Root);
                yield break;
            }

            _c2SmpOverlayInstancesV93LikeOriginal[job.ApplyKey] = instance;

            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 OVERLAY CREATE] piece='" + job.PieceName + "' md='" + job.MdName + "' chunks=" + instance.Chunks.Count.ToString(CultureInfo.InvariantCulture) +
                      " alphaMode=" + (C2SmpOverlayUseTimeFallbackV93LikeOriginal ? "time_fallback" : "build_stage") +
                      " fadeSeconds=" + instance.FadeSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                      " source='" + job.Source + "'");

            instance.FadeCoroutine = StartCoroutine(C2SmpRunOverlayFadeV93LikeOriginal(instance));
            totalSw.Stop();
            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V107 DONE] piece='" + job.PieceName + "' md='" + job.MdName + "' chunks=" + instance.Chunks.Count.ToString(CultureInfo.InvariantCulture) +
                      " failures=" + failures.ToString(CultureInfo.InvariantCulture) +
                      " totalMs=" + totalSw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + C2SmpFpsNowV87LikeOriginal());
        }

        private IEnumerator C2SmpRunOverlayFadeV93LikeOriginal(C2SmpOverlayInstanceV93LikeOriginal instance)
        {
            if (instance == null)
                yield break;
            float start = Time.realtimeSinceStartup;
            float lastLog = start - 999f;
            while (instance != null && instance.Root != null)
            {
                float rawProgress = C2SmpOverlayUseTimeFallbackV93LikeOriginal
                    ? Mathf.Clamp01((Time.realtimeSinceStartup - start) / Mathf.Max(0.01f, instance.FadeSeconds))
                    : 1.0f;
                float progress = Mathf.Min(rawProgress, C2SmpOverlayMaxVisibleAlphaV97LikeOriginal);
                instance.CurrentAlpha = progress;
                for (int i = 0; i < instance.Chunks.Count; i++)
                {
                    var ch = instance.Chunks[i];
                    if (ch != null && ch.Material != null)
                        C2SmpSetOverlayAlphaV93LikeOriginal(ch.Material, progress);
                }
                if (Time.realtimeSinceStartup - lastLog >= C2SmpOverlayAlphaLogIntervalV102LikeOriginal)
                {
                    lastLog = Time.realtimeSinceStartup;
                    if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 ALPHA] piece='" + instance.PieceName + "' md='" + instance.MdName + "' alpha=" + progress.ToString("0.000", CultureInfo.InvariantCulture) +
                              " raw=" + rawProgress.ToString("0.000", CultureInfo.InvariantCulture) +
                              " max=" + C2SmpOverlayMaxVisibleAlphaV97LikeOriginal.ToString("0.000", CultureInfo.InvariantCulture) +
                              " mode=" + (C2SmpOverlayUseTimeFallbackV93LikeOriginal ? "time_fallback" : "build_stage") + C2SmpFpsNowV87LikeOriginal());
                }
                if (rawProgress >= C2SmpOverlayMaxVisibleAlphaV97LikeOriginal)
                    break;
                yield return null;
            }
            if (instance != null)
            {
                instance.CurrentAlpha = C2SmpOverlayMaxVisibleAlphaV97LikeOriginal;
                for (int i = 0; i < instance.Chunks.Count; i++)
                {
                    var ch = instance.Chunks[i];
                    if (ch != null && ch.Material != null)
                        C2SmpSetOverlayAlphaV93LikeOriginal(ch.Material, C2SmpOverlayMaxVisibleAlphaV97LikeOriginal);
                }
            }
        }


        private static bool C2SmpHasUsableNrgMaskV107LikeOriginal(C2SmpPieceLikeOriginal piece)
        {
            return C2SmpOverlayUseNrgWeightMaskV107LikeOriginal &&
                   piece != null &&
                   piece.HasNrg1 &&
                   piece.NrgNx > 1 &&
                   piece.NrgNy > 1 &&
                   piece.NrgPoints != null &&
                   piece.NrgPoints.Length > 0 &&
                   piece.NrgActivePointCount > 0;
        }

        private static int C2SmpGetNrgWeightAtV107LikeOriginal(C2SmpPieceLikeOriginal piece, int ix, int iy)
        {
            if (piece == null || piece.NrgPoints == null || piece.NrgNx <= 0 || piece.NrgNy <= 0)
                return 0;
            ix = Mathf.Clamp(ix, 0, piece.NrgNx - 1);
            iy = Mathf.Clamp(iy, 0, piece.NrgNy - 1);

            // Original save loop is ix outer, iy inner.
            int idx = ix * piece.NrgNy + iy;
            if (idx < 0 || idx >= piece.NrgPoints.Length)
                return 0;

            int w = piece.NrgPoints[idx].Weight;
            // Original W is signed char and condition is W > 0.
            if (w <= 0 || w >= 128)
                return 0;
            return w;
        }

        private static int C2SmpSampleNrgMaskAlphaV107LikeOriginal(C2SmpOverlayChunkBuildV93LikeOriginal built, float worldX, float worldZ)
        {
            if (built == null || !C2SmpHasUsableNrgMaskV107LikeOriginal(built.Piece))
                return 255;

            Bounds b = built.JobWorldBounds;
            if (b.size.x <= 0.001f || b.size.z <= 0.001f)
                b = built.WorldBounds;
            if (b.size.x <= 0.001f || b.size.z <= 0.001f)
                return 255;

            float u = Mathf.InverseLerp(b.min.x, b.max.x, worldX);
            float v = Mathf.InverseLerp(b.min.z, b.max.z, worldZ);

            // Outside the original 1GRN footprint we must not show dirty-rect bake differences.
            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
                return 0;

            C2SmpPieceLikeOriginal piece = built.Piece;
            float gx = u * Mathf.Max(1, piece.NrgNx - 1);
            float gy = v * Mathf.Max(1, piece.NrgNy - 1);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, piece.NrgNx - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(gy), 0, piece.NrgNy - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, piece.NrgNx - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, piece.NrgNy - 1);
            float fx = Mathf.Clamp01(gx - x0);
            float fy = Mathf.Clamp01(gy - y0);

            int w00 = C2SmpGetNrgWeightAtV107LikeOriginal(piece, x0, y0);
            int w10 = C2SmpGetNrgWeightAtV107LikeOriginal(piece, x1, y0);
            int w01 = C2SmpGetNrgWeightAtV107LikeOriginal(piece, x0, y1);
            int w11 = C2SmpGetNrgWeightAtV107LikeOriginal(piece, x1, y1);

            float wa = Mathf.Lerp(w00, w10, fx);
            float wb = Mathf.Lerp(w01, w11, fx);
            float w = Mathf.Lerp(wa, wb, fy);

            if (w < C2SmpOverlayNrgWeightMinV107LikeOriginal)
                return 0;

            // NRG weight in original is not an overlay alpha directly; it is a point texture strength.
            // Scale 1..127 into alpha while preserving soft edges from low weights.
            return Mathf.Clamp(Mathf.RoundToInt((w / 127.0f) * 255.0f), 0, 255);
        }


        private static C2SmpOverlayTextureBuildResultV108LikeOriginal C2SmpBuildOverlayTexturePixelsV108LikeOriginal(C2SmpOverlayChunkBuildV93LikeOriginal built)
        {
            C2SmpOverlayTextureBuildResultV108LikeOriginal result = new C2SmpOverlayTextureBuildResultV108LikeOriginal();
            var swV108 = global::System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (built == null || built.Width <= 0 || built.Height <= 0 || built.TargetPixels == null || built.BasePixels == null)
                {
                    result.SkipReason = "invalid_input";
                    return result;
                }

                int fullW = built.FullWidth > 0 ? built.FullWidth : built.Width;
                int fullH = built.FullHeight > 0 ? built.FullHeight : built.Height;
                int ox = Mathf.Clamp(built.OffsetPixelsX, 0, Mathf.Max(0, fullW - 1));
                int oy = Mathf.Clamp(built.OffsetPixelsY, 0, Mathf.Max(0, fullH - 1));

                Bounds fullBounds = built.FullWorldBounds;
                if (fullBounds.size.x <= 0.01f || fullBounds.size.z <= 0.01f)
                    fullBounds = built.WorldBounds;
                Bounds jobBounds = built.JobWorldBounds;
                if (jobBounds.size.x <= 0.01f || jobBounds.size.z <= 0.01f)
                    jobBounds = built.WorldBounds;

                float centerX = jobBounds.center.x;
                float centerZ = jobBounds.center.z;
                float radiusScale = Mathf.Clamp(C2SmpOverlayMaskRadiusScaleV98LikeOriginal, 0.10f, 1.00f);
                float radiusX = Mathf.Max(1.0f, jobBounds.size.x * 0.5f * radiusScale);
                float radiusZ = Mathf.Max(1.0f, jobBounds.size.z * 0.5f * radiusScale);
                float plateau = Mathf.Clamp01(C2SmpOverlaySoftMaskPlateauV96LikeOriginal);
                float outer = Mathf.Clamp(C2SmpOverlaySoftMaskOuterV96LikeOriginal, plateau + 0.001f, 4.0f);
                int minVisibleAlpha = Mathf.Clamp(C2SmpOverlaySoftMaskMinAlphaV96LikeOriginal, 0, 254);
                int deltaThreshold = Mathf.Max(0, C2SmpOverlayDeltaThresholdV98LikeOriginal);
                int farWeakThreshold = Mathf.Max(deltaThreshold + 1, C2SmpOverlayFarWeakDeltaThresholdV98LikeOriginal);
                float farWeakStart = Mathf.Clamp01(C2SmpOverlayFarWeakDeltaStartV98LikeOriginal);

                int subLen = built.Width * built.Height;
                Color32[] subPix = new Color32[subLen];
                byte[] subAlpha = new byte[subLen];
                int activePixels = 0;
                int maxAlpha = 0;
                int fullAlphaPixels = 0;
                int edgeFadePixels = 0;
                int zeroByMask = 0;
                int zeroByDelta = 0;
                int zeroByFarWeak = 0;
                int zeroByNrgMaskV107 = 0;
                bool useNrgMaskV107 = C2SmpHasUsableNrgMaskV107LikeOriginal(built.Piece);
                int activeMinX = built.Width;
                int activeMinY = built.Height;
                int activeMaxX = -1;
                int activeMaxY = -1;

                for (int y = 0; y < built.Height; y++)
                {
                    int srcRow = y * built.Width;
                    int dstY = oy + y;
                    if (dstY < 0 || dstY >= fullH) continue;
                    for (int x = 0; x < built.Width; x++)
                    {
                        int src = srcRow + x;
                        int dstX = ox + x;
                        if (src < 0 || src >= built.TargetPixels.Length || src >= built.BasePixels.Length || dstX < 0 || dstX >= fullW) continue;
                        Color32 b = built.BasePixels[src];
                        Color32 t = built.TargetPixels[src];
                        int d = Mathf.Abs((int)b.r - (int)t.r) + Mathf.Abs((int)b.g - (int)t.g) + Mathf.Abs((int)b.b - (int)t.b) + Mathf.Abs((int)b.a - (int)t.a);
                        if (d <= deltaThreshold)
                        {
                            zeroByDelta++;
                            continue;
                        }

                        float u = (dstX + 0.5f) / Mathf.Max(1.0f, (float)fullW);
                        float v = (dstY + 0.5f) / Mathf.Max(1.0f, (float)fullH);
                        float wx = Mathf.Lerp(fullBounds.min.x, fullBounds.max.x, u);
                        float wz = Mathf.Lerp(fullBounds.min.z, fullBounds.max.z, v);
                        int maskA;
                        if (useNrgMaskV107)
                        {
                            maskA = C2SmpSampleNrgMaskAlphaV107LikeOriginal(built, wx, wz);
                            if (maskA <= 0)
                            {
                                zeroByNrgMaskV107++;
                                continue;
                            }
                        }
                        else
                        {
                            float nx = (wx - centerX) / radiusX;
                            float nz = (wz - centerZ) / radiusZ;
                            float r = Mathf.Sqrt(nx * nx + nz * nz);

                            if (r >= outer)
                            {
                                zeroByMask++;
                                continue;
                            }

                            if (r >= farWeakStart && d < farWeakThreshold)
                            {
                                zeroByFarWeak++;
                                continue;
                            }

                            if (r <= plateau)
                            {
                                maskA = 255;
                            }
                            else
                            {
                                float tEdge = 1.0f - Mathf.InverseLerp(plateau, outer, r);
                                tEdge = tEdge * tEdge * (3.0f - 2.0f * tEdge);
                                maskA = Mathf.RoundToInt(Mathf.Lerp((float)minVisibleAlpha, 255.0f, tEdge));
                                if (maskA < minVisibleAlpha) maskA = minVisibleAlpha;
                                if (maskA > 255) maskA = 255;
                            }
                            if (maskA <= 0)
                            {
                                zeroByMask++;
                                continue;
                            }
                        }

                        int deltaA = Mathf.Clamp((d - deltaThreshold) * 5, 0, 255);
                        int a = Mathf.Min(maskA, deltaA);
                        if (a <= 0)
                        {
                            zeroByDelta++;
                            continue;
                        }

                        subPix[src] = new Color32(t.r, t.g, t.b, (byte)a);
                        subAlpha[src] = (byte)a;
                        activePixels++;
                        if (a > maxAlpha) maxAlpha = a;
                        if (a >= 255) fullAlphaPixels++;
                        else edgeFadePixels++;
                        if (x < activeMinX) activeMinX = x;
                        if (y < activeMinY) activeMinY = y;
                        if (x > activeMaxX) activeMaxX = x;
                        if (y > activeMaxY) activeMaxY = y;
                    }
                }

                result.FullW = fullW;
                result.FullH = fullH;
                result.ActivePixels = activePixels;
                result.MaxAlpha = maxAlpha;
                result.FullAlphaPixels = fullAlphaPixels;
                result.EdgeFadePixels = edgeFadePixels;
                result.ZeroByMask = zeroByMask;
                result.ZeroByDelta = zeroByDelta;
                result.ZeroByFarWeak = zeroByFarWeak;
                result.ZeroByNrgMask = zeroByNrgMaskV107;
                result.UseNrgMask = useNrgMaskV107;
                result.Plateau = plateau;
                result.RadiusScale = radiusScale;
                result.DeltaThreshold = deltaThreshold;
                result.FarWeakThreshold = farWeakThreshold;
                result.CenterX = centerX;
                result.CenterZ = centerZ;
                result.RadiusX = radiusX;
                result.RadiusZ = radiusZ;

                if (activePixels <= 0 || maxAlpha <= 0 || activeMaxX < activeMinX || activeMaxY < activeMinY)
                {
                    result.SkipReason = "no_active_pixels fullPixels=" + fullW.ToString(CultureInfo.InvariantCulture) + "x" + fullH.ToString(CultureInfo.InvariantCulture) +
                                        " subPixels=" + built.Width.ToString(CultureInfo.InvariantCulture) + "x" + built.Height.ToString(CultureInfo.InvariantCulture) +
                                        " activePixels=" + activePixels.ToString(CultureInfo.InvariantCulture) + " maxAlpha=" + maxAlpha.ToString(CultureInfo.InvariantCulture) +
                                        " zeroByMask=" + zeroByMask.ToString(CultureInfo.InvariantCulture) + " zeroByDelta=" + zeroByDelta.ToString(CultureInfo.InvariantCulture) +
                                        " zeroByFarWeak=" + zeroByFarWeak.ToString(CultureInfo.InvariantCulture) + " zeroByNrgMask=" + zeroByNrgMaskV107.ToString(CultureInfo.InvariantCulture) +
                                        " useNrgMask=" + (useNrgMaskV107 ? "1" : "0");
                    return result;
                }

                if (maxAlpha < C2SmpOverlayWeakChunkSkipMaxAlphaV102LikeOriginal && activePixels < C2SmpOverlayWeakChunkSkipMaxPixelsV102LikeOriginal)
                {
                    result.SkipReason = "weak_chunk subPixels=" + built.Width.ToString(CultureInfo.InvariantCulture) + "x" + built.Height.ToString(CultureInfo.InvariantCulture) +
                                        " activePixels=" + activePixels.ToString(CultureInfo.InvariantCulture) + " maxAlpha=" + maxAlpha.ToString(CultureInfo.InvariantCulture);
                    return result;
                }

                int pad = Mathf.Max(0, useNrgMaskV107 ? C2SmpOverlayNrgMaskCropPaddingPixelsV107LikeOriginal : C2SmpOverlayCropPaddingPixelsV102LikeOriginal);
                int cropSubMinX = Mathf.Clamp(activeMinX - pad, 0, built.Width - 1);
                int cropSubMinY = Mathf.Clamp(activeMinY - pad, 0, built.Height - 1);
                int cropSubMaxXExclusive = Mathf.Clamp(activeMaxX + pad + 1, cropSubMinX + 1, built.Width);
                int cropSubMaxYExclusive = Mathf.Clamp(activeMaxY + pad + 1, cropSubMinY + 1, built.Height);
                int cropW = cropSubMaxXExclusive - cropSubMinX;
                int cropH = cropSubMaxYExclusive - cropSubMinY;
                int cropX = ox + cropSubMinX;
                int cropY = oy + cropSubMinY;
                if (cropW <= 0 || cropH <= 0 || cropX < 0 || cropY < 0 || cropX + cropW > fullW || cropY + cropH > fullH)
                {
                    result.SkipReason = "invalid_crop";
                    return result;
                }

                Color32[] pix = new Color32[cropW * cropH];
                for (int y = 0; y < cropH; y++)
                {
                    int srcY = cropSubMinY + y;
                    if (srcY < 0 || srcY >= built.Height) continue;
                    int srcRow = srcY * built.Width;
                    int dstRow = y * cropW;
                    for (int x = 0; x < cropW; x++)
                    {
                        int srcX = cropSubMinX + x;
                        if (srcX < 0 || srcX >= built.Width) continue;
                        int src = srcRow + srcX;
                        if (src < 0 || src >= subAlpha.Length || subAlpha[src] == 0) continue;
                        pix[dstRow + x] = subPix[src];
                    }
                }

                float cropMinU = cropX / Mathf.Max(1.0f, (float)fullW);
                float cropMaxU = (cropX + cropW) / Mathf.Max(1.0f, (float)fullW);
                float cropMinV = cropY / Mathf.Max(1.0f, (float)fullH);
                float cropMaxV = (cropY + cropH) / Mathf.Max(1.0f, (float)fullH);
                Bounds cropWorldBounds = C2SmpComputeCropWorldBoundsV102LikeOriginal(fullBounds, cropMinU, cropMaxU, cropMinV, cropMaxV);

                result.HasTexture = true;
                result.Pixels = pix;
                result.CropW = cropW;
                result.CropH = cropH;
                result.CropX = cropX;
                result.CropY = cropY;
                result.CropMinU = cropMinU;
                result.CropMaxU = cropMaxU;
                result.CropMinV = cropMinV;
                result.CropMaxV = cropMaxV;
                result.CropWorldBounds = cropWorldBounds;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.GetType().Name + ": " + ex.Message;
                result.SkipReason = "exception";
                return result;
            }
            finally
            {
                swV108.Stop();
                result.BuildMs = swV108.ElapsedMilliseconds;
            }
        }

        private static Texture2D C2SmpCreateOverlayTextureFromBuildV108LikeOriginal(C2SmpOverlayChunkBuildV93LikeOriginal built, C2SmpOverlayTextureBuildResultV108LikeOriginal result)
        {
            if (built == null || result == null || !result.HasTexture || result.Pixels == null || result.CropW <= 0 || result.CropH <= 0)
                return null;

            built.WorldBounds = result.CropWorldBounds;
            built.SourceMesh = C2SmpCreateCroppedOverlayMeshV102LikeOriginal(built.SourceMesh, result.CropMinU, result.CropMaxU, result.CropMinV, result.CropMaxV, result.CropWorldBounds);

            var uploadSwV108 = global::System.Diagnostics.Stopwatch.StartNew();
            Texture2D tex = new Texture2D(result.CropW, result.CropH, TextureFormat.RGBA32, false, false);
            tex.name = "SMPOverlayCrop_" + (built.ChunkName ?? "chunk");
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(result.Pixels);
            tex.Apply(false, true);
            uploadSwV108.Stop();

            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V108 OVERLAY TEX] chunk=" + built.ChunkName +
                      " fullPixels=" + result.FullW.ToString(CultureInfo.InvariantCulture) + "x" + result.FullH.ToString(CultureInfo.InvariantCulture) +
                      " subPixels=" + built.Width.ToString(CultureInfo.InvariantCulture) + "x" + built.Height.ToString(CultureInfo.InvariantCulture) +
                      " cropPixels=" + result.CropW.ToString(CultureInfo.InvariantCulture) + "x" + result.CropH.ToString(CultureInfo.InvariantCulture) +
                      " cropOffset=" + result.CropX.ToString(CultureInfo.InvariantCulture) + "/" + result.CropY.ToString(CultureInfo.InvariantCulture) +
                      " activePixels=" + result.ActivePixels.ToString(CultureInfo.InvariantCulture) +
                      " fullAlphaPixels=" + result.FullAlphaPixels.ToString(CultureInfo.InvariantCulture) +
                      " edgeFadePixels=" + result.EdgeFadePixels.ToString(CultureInfo.InvariantCulture) +
                      " zeroByMask=" + result.ZeroByMask.ToString(CultureInfo.InvariantCulture) +
                      " zeroByDelta=" + result.ZeroByDelta.ToString(CultureInfo.InvariantCulture) +
                      " zeroByFarWeak=" + result.ZeroByFarWeak.ToString(CultureInfo.InvariantCulture) +
                      " zeroByNrgMask=" + result.ZeroByNrgMask.ToString(CultureInfo.InvariantCulture) +
                      " useNrgMask=" + (result.UseNrgMask ? "1" : "0") +
                      " nrg=" + (built.Piece != null && built.Piece.HasNrg1 ? (built.Piece.NrgNx.ToString(CultureInfo.InvariantCulture) + "x" + built.Piece.NrgNy.ToString(CultureInfo.InvariantCulture) + " active=" + built.Piece.NrgActivePointCount.ToString(CultureInfo.InvariantCulture) + "/" + built.Piece.NrgPointCount.ToString(CultureInfo.InvariantCulture)) : "none") +
                      " maxAlpha=" + result.MaxAlpha.ToString(CultureInfo.InvariantCulture) +
                      " plateau=" + result.Plateau.ToString("0.###", CultureInfo.InvariantCulture) +
                      " radiusScale=" + result.RadiusScale.ToString("0.###", CultureInfo.InvariantCulture) +
                      " deltaThreshold=" + result.DeltaThreshold.ToString(CultureInfo.InvariantCulture) +
                      " farWeakThreshold=" + result.FarWeakThreshold.ToString(CultureInfo.InvariantCulture) +
                      " center=" + result.CenterX.ToString("0.##", CultureInfo.InvariantCulture) + "/" + result.CenterZ.ToString("0.##", CultureInfo.InvariantCulture) +
                      " radius=" + result.RadiusX.ToString("0.##", CultureInfo.InvariantCulture) + "/" + result.RadiusZ.ToString("0.##", CultureInfo.InvariantCulture) +
                      " bufferMs=" + result.BuildMs.ToString(CultureInfo.InvariantCulture) +
                      " uploadMs=" + uploadSwV108.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                      C2SmpFpsNowV87LikeOriginal());

            result.Pixels = null;
            return tex;
        }

        private static Texture2D C2SmpBuildOverlayTextureV93LikeOriginal(C2SmpOverlayChunkBuildV93LikeOriginal built)
        {
            if (built == null || built.Width <= 0 || built.Height <= 0 || built.TargetPixels == null || built.BasePixels == null)
                return null;

            int fullW = built.FullWidth > 0 ? built.FullWidth : built.Width;
            int fullH = built.FullHeight > 0 ? built.FullHeight : built.Height;
            int ox = Mathf.Clamp(built.OffsetPixelsX, 0, Mathf.Max(0, fullW - 1));
            int oy = Mathf.Clamp(built.OffsetPixelsY, 0, Mathf.Max(0, fullH - 1));

            Bounds fullBounds = built.FullWorldBounds;
            if (fullBounds.size.x <= 0.01f || fullBounds.size.z <= 0.01f)
                fullBounds = built.WorldBounds;
            Bounds jobBounds = built.JobWorldBounds;
            if (jobBounds.size.x <= 0.01f || jobBounds.size.z <= 0.01f)
                jobBounds = built.WorldBounds;

            float centerX = jobBounds.center.x;
            float centerZ = jobBounds.center.z;
            float radiusScale = Mathf.Clamp(C2SmpOverlayMaskRadiusScaleV98LikeOriginal, 0.10f, 1.00f);
            float radiusX = Mathf.Max(1.0f, jobBounds.size.x * 0.5f * radiusScale);
            float radiusZ = Mathf.Max(1.0f, jobBounds.size.z * 0.5f * radiusScale);
            float plateau = Mathf.Clamp01(C2SmpOverlaySoftMaskPlateauV96LikeOriginal);
            float outer = Mathf.Clamp(C2SmpOverlaySoftMaskOuterV96LikeOriginal, plateau + 0.001f, 4.0f);
            int minVisibleAlpha = Mathf.Clamp(C2SmpOverlaySoftMaskMinAlphaV96LikeOriginal, 0, 254);
            int deltaThreshold = Mathf.Max(0, C2SmpOverlayDeltaThresholdV98LikeOriginal);
            int farWeakThreshold = Mathf.Max(deltaThreshold + 1, C2SmpOverlayFarWeakDeltaThresholdV98LikeOriginal);
            float farWeakStart = Mathf.Clamp01(C2SmpOverlayFarWeakDeltaStartV98LikeOriginal);

            int subLen = built.Width * built.Height;
            Color32[] subPix = new Color32[subLen];
            byte[] subAlpha = new byte[subLen];
            int activePixels = 0;
            int maxAlpha = 0;
            int fullAlphaPixels = 0;
            int edgeFadePixels = 0;
            int zeroByMask = 0;
            int zeroByDelta = 0;
            int zeroByFarWeak = 0;
            int zeroByNrgMaskV107 = 0;
            bool useNrgMaskV107 = C2SmpHasUsableNrgMaskV107LikeOriginal(built.Piece);
            int activeMinX = built.Width;
            int activeMinY = built.Height;
            int activeMaxX = -1;
            int activeMaxY = -1;

            for (int y = 0; y < built.Height; y++)
            {
                int srcRow = y * built.Width;
                int dstY = oy + y;
                if (dstY < 0 || dstY >= fullH) continue;
                for (int x = 0; x < built.Width; x++)
                {
                    int src = srcRow + x;
                    int dstX = ox + x;
                    if (src < 0 || src >= built.TargetPixels.Length || src >= built.BasePixels.Length || dstX < 0 || dstX >= fullW) continue;
                    Color32 b = built.BasePixels[src];
                    Color32 t = built.TargetPixels[src];
                    int d = Mathf.Abs((int)b.r - (int)t.r) + Mathf.Abs((int)b.g - (int)t.g) + Mathf.Abs((int)b.b - (int)t.b) + Mathf.Abs((int)b.a - (int)t.a);
                    if (d <= deltaThreshold)
                    {
                        zeroByDelta++;
                        continue;
                    }

                    float u = (dstX + 0.5f) / Mathf.Max(1.0f, (float)fullW);
                    float v = (dstY + 0.5f) / Mathf.Max(1.0f, (float)fullH);
                    float wx = Mathf.Lerp(fullBounds.min.x, fullBounds.max.x, u);
                    float wz = Mathf.Lerp(fullBounds.min.z, fullBounds.max.z, v);
                    int maskA;
                    if (useNrgMaskV107)
                    {
                        maskA = C2SmpSampleNrgMaskAlphaV107LikeOriginal(built, wx, wz);
                        if (maskA <= 0)
                        {
                            zeroByNrgMaskV107++;
                            continue;
                        }
                    }
                    else
                    {
                        float nx = (wx - centerX) / radiusX;
                        float nz = (wz - centerZ) / radiusZ;
                        float r = Mathf.Sqrt(nx * nx + nz * nz);

                        if (r >= outer)
                        {
                            zeroByMask++;
                            continue;
                        }

                        if (r >= farWeakStart && d < farWeakThreshold)
                        {
                            zeroByFarWeak++;
                            continue;
                        }

                        if (r <= plateau)
                        {
                            maskA = 255;
                        }
                        else
                        {
                            float tEdge = 1.0f - Mathf.InverseLerp(plateau, outer, r);
                            tEdge = tEdge * tEdge * (3.0f - 2.0f * tEdge);
                            maskA = Mathf.RoundToInt(Mathf.Lerp((float)minVisibleAlpha, 255.0f, tEdge));
                            if (maskA < minVisibleAlpha) maskA = minVisibleAlpha;
                            if (maskA > 255) maskA = 255;
                        }
                        if (maskA <= 0)
                        {
                            zeroByMask++;
                            continue;
                        }
                    }

                    int deltaA = Mathf.Clamp((d - deltaThreshold) * 5, 0, 255);
                    int a = Mathf.Min(maskA, deltaA);
                    if (a <= 0)
                    {
                        zeroByDelta++;
                        continue;
                    }

                    subPix[src] = new Color32(t.r, t.g, t.b, (byte)a);
                    subAlpha[src] = (byte)a;
                    activePixels++;
                    if (a > maxAlpha) maxAlpha = a;
                    if (a >= 255) fullAlphaPixels++;
                    else edgeFadePixels++;
                    if (x < activeMinX) activeMinX = x;
                    if (y < activeMinY) activeMinY = y;
                    if (x > activeMaxX) activeMaxX = x;
                    if (y > activeMaxY) activeMaxY = y;
                }
            }

            if (activePixels <= 0 || maxAlpha <= 0 || activeMaxX < activeMinX || activeMaxY < activeMinY)
            {
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 OVERLAY TEX SKIP] chunk=" + built.ChunkName + " fullPixels=" + fullW.ToString(CultureInfo.InvariantCulture) + "x" + fullH.ToString(CultureInfo.InvariantCulture) +
                          " subPixels=" + built.Width.ToString(CultureInfo.InvariantCulture) + "x" + built.Height.ToString(CultureInfo.InvariantCulture) +
                          " activePixels=" + activePixels.ToString(CultureInfo.InvariantCulture) + " maxAlpha=" + maxAlpha.ToString(CultureInfo.InvariantCulture) +
                          " zeroByMask=" + zeroByMask.ToString(CultureInfo.InvariantCulture) + " zeroByDelta=" + zeroByDelta.ToString(CultureInfo.InvariantCulture) +
                          " zeroByFarWeak=" + zeroByFarWeak.ToString(CultureInfo.InvariantCulture) +
                          " zeroByNrgMask=" + zeroByNrgMaskV107.ToString(CultureInfo.InvariantCulture) +
                          " useNrgMask=" + (useNrgMaskV107 ? "1" : "0"));
                return null;
            }

            if (maxAlpha < C2SmpOverlayWeakChunkSkipMaxAlphaV102LikeOriginal && activePixels < C2SmpOverlayWeakChunkSkipMaxPixelsV102LikeOriginal)
            {
                if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 OVERLAY TEX WEAK SKIP] chunk=" + built.ChunkName +
                          " subPixels=" + built.Width.ToString(CultureInfo.InvariantCulture) + "x" + built.Height.ToString(CultureInfo.InvariantCulture) +
                          " activePixels=" + activePixels.ToString(CultureInfo.InvariantCulture) + " maxAlpha=" + maxAlpha.ToString(CultureInfo.InvariantCulture));
                return null;
            }

            int pad = Mathf.Max(0, useNrgMaskV107 ? C2SmpOverlayNrgMaskCropPaddingPixelsV107LikeOriginal : C2SmpOverlayCropPaddingPixelsV102LikeOriginal);
            int cropSubMinX = Mathf.Clamp(activeMinX - pad, 0, built.Width - 1);
            int cropSubMinY = Mathf.Clamp(activeMinY - pad, 0, built.Height - 1);
            int cropSubMaxXExclusive = Mathf.Clamp(activeMaxX + pad + 1, cropSubMinX + 1, built.Width);
            int cropSubMaxYExclusive = Mathf.Clamp(activeMaxY + pad + 1, cropSubMinY + 1, built.Height);
            int cropW = cropSubMaxXExclusive - cropSubMinX;
            int cropH = cropSubMaxYExclusive - cropSubMinY;
            int cropX = ox + cropSubMinX;
            int cropY = oy + cropSubMinY;
            if (cropW <= 0 || cropH <= 0 || cropX < 0 || cropY < 0 || cropX + cropW > fullW || cropY + cropH > fullH)
                return null;

            Color32[] pix = new Color32[cropW * cropH];
            for (int y = 0; y < cropH; y++)
            {
                int srcY = cropSubMinY + y;
                if (srcY < 0 || srcY >= built.Height) continue;
                int srcRow = srcY * built.Width;
                int dstRow = y * cropW;
                for (int x = 0; x < cropW; x++)
                {
                    int srcX = cropSubMinX + x;
                    if (srcX < 0 || srcX >= built.Width) continue;
                    int src = srcRow + srcX;
                    if (src < 0 || src >= subAlpha.Length || subAlpha[src] == 0) continue;
                    pix[dstRow + x] = subPix[src];
                }
            }

            float cropMinU = cropX / Mathf.Max(1.0f, (float)fullW);
            float cropMaxU = (cropX + cropW) / Mathf.Max(1.0f, (float)fullW);
            float cropMinV = cropY / Mathf.Max(1.0f, (float)fullH);
            float cropMaxV = (cropY + cropH) / Mathf.Max(1.0f, (float)fullH);
            Bounds cropWorldBounds = C2SmpComputeCropWorldBoundsV102LikeOriginal(fullBounds, cropMinU, cropMaxU, cropMinV, cropMaxV);
            built.WorldBounds = cropWorldBounds;
            built.SourceMesh = C2SmpCreateCroppedOverlayMeshV102LikeOriginal(built.SourceMesh, cropMinU, cropMaxU, cropMinV, cropMaxV, cropWorldBounds);

            Texture2D tex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false, false);
            tex.name = "SMPOverlayCrop_" + (built.ChunkName ?? "chunk");
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(pix);
            tex.Apply(false, true);
            if (C2SmpRuntimeVerboseV82LikeOriginal) Debug.Log("[C2:SMP V103 OVERLAY TEX] chunk=" + built.ChunkName + " fullPixels=" + fullW.ToString(CultureInfo.InvariantCulture) + "x" + fullH.ToString(CultureInfo.InvariantCulture) +
                      " subPixels=" + built.Width.ToString(CultureInfo.InvariantCulture) + "x" + built.Height.ToString(CultureInfo.InvariantCulture) +
                      " cropPixels=" + cropW.ToString(CultureInfo.InvariantCulture) + "x" + cropH.ToString(CultureInfo.InvariantCulture) +
                      " cropOffset=" + cropX.ToString(CultureInfo.InvariantCulture) + "/" + cropY.ToString(CultureInfo.InvariantCulture) +
                      " activePixels=" + activePixels.ToString(CultureInfo.InvariantCulture) + " fullAlphaPixels=" + fullAlphaPixels.ToString(CultureInfo.InvariantCulture) +
                      " edgeFadePixels=" + edgeFadePixels.ToString(CultureInfo.InvariantCulture) + " zeroByMask=" + zeroByMask.ToString(CultureInfo.InvariantCulture) +
                      " zeroByDelta=" + zeroByDelta.ToString(CultureInfo.InvariantCulture) + " zeroByFarWeak=" + zeroByFarWeak.ToString(CultureInfo.InvariantCulture) +
                      " zeroByNrgMask=" + zeroByNrgMaskV107.ToString(CultureInfo.InvariantCulture) +
                      " useNrgMask=" + (useNrgMaskV107 ? "1" : "0") +
                      " nrg=" + (built.Piece != null && built.Piece.HasNrg1 ? (built.Piece.NrgNx.ToString(CultureInfo.InvariantCulture) + "x" + built.Piece.NrgNy.ToString(CultureInfo.InvariantCulture) + " active=" + built.Piece.NrgActivePointCount.ToString(CultureInfo.InvariantCulture) + "/" + built.Piece.NrgPointCount.ToString(CultureInfo.InvariantCulture)) : "none") +
                      " maxAlpha=" + maxAlpha.ToString(CultureInfo.InvariantCulture) +
                      " plateau=" + plateau.ToString("0.###", CultureInfo.InvariantCulture) + " radiusScale=" + radiusScale.ToString("0.###", CultureInfo.InvariantCulture) +
                      " deltaThreshold=" + deltaThreshold.ToString(CultureInfo.InvariantCulture) + " farWeakThreshold=" + farWeakThreshold.ToString(CultureInfo.InvariantCulture) +
                      " center=" + centerX.ToString("0.##", CultureInfo.InvariantCulture) + "/" + centerZ.ToString("0.##", CultureInfo.InvariantCulture) +
                      " radius=" + radiusX.ToString("0.##", CultureInfo.InvariantCulture) + "/" + radiusZ.ToString("0.##", CultureInfo.InvariantCulture));
            return tex;
        }

        private static Bounds C2SmpComputeCropWorldBoundsV102LikeOriginal(Bounds fullBounds, float minU, float maxU, float minV, float maxV)
        {
            float minX = Mathf.Lerp(fullBounds.min.x, fullBounds.max.x, Mathf.Clamp01(minU));
            float maxX = Mathf.Lerp(fullBounds.min.x, fullBounds.max.x, Mathf.Clamp01(maxU));
            float minZ = Mathf.Lerp(fullBounds.min.z, fullBounds.max.z, Mathf.Clamp01(minV));
            float maxZ = Mathf.Lerp(fullBounds.min.z, fullBounds.max.z, Mathf.Clamp01(maxV));
            Vector3 mn = new Vector3(Mathf.Min(minX, maxX), 0.0f, Mathf.Min(minZ, maxZ));
            Vector3 mx = new Vector3(Mathf.Max(minX, maxX), 0.0f, Mathf.Max(minZ, maxZ));
            Bounds b = new Bounds((mn + mx) * 0.5f, mx - mn);
            if (b.size.x <= 0.01f || b.size.z <= 0.01f)
                b = new Bounds(b.center, new Vector3(1.0f, 1.0f, 1.0f));
            return b;
        }

        private static Mesh C2SmpCreateCroppedOverlayMeshV102LikeOriginal(Mesh source, float minU, float maxU, float minV, float maxV, Bounds fallbackBounds)
        {
            if (source == null || source.vertexCount <= 0 || source.triangles == null || source.triangles.Length < 3 || source.uv == null || source.uv.Length < source.vertexCount)
                return C2SmpCreateOverlayMeshV93LikeOriginal(fallbackBounds);

            Vector3[] srcV = source.vertices;
            Vector2[] srcUv = source.uv;
            int[] srcTri = source.triangles;
            float du = Mathf.Max(0.00001f, maxU - minU);
            float dv = Mathf.Max(0.00001f, maxV - minV);
            float eps = 2.0f / Mathf.Max(16.0f, Mathf.Max(1.0f / du, 1.0f / dv));
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();
            Dictionary<int, int> map = new Dictionary<int, int>();

            for (int i = 0; i + 2 < srcTri.Length; i += 3)
            {
                int i0 = srcTri[i];
                int i1 = srcTri[i + 1];
                int i2 = srcTri[i + 2];
                if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= srcUv.Length || i1 >= srcUv.Length || i2 >= srcUv.Length)
                    continue;
                Vector2 u0 = srcUv[i0];
                Vector2 u1 = srcUv[i1];
                Vector2 u2 = srcUv[i2];
                float triMinU = Mathf.Min(u0.x, Mathf.Min(u1.x, u2.x));
                float triMaxU = Mathf.Max(u0.x, Mathf.Max(u1.x, u2.x));
                float triMinV = Mathf.Min(u0.y, Mathf.Min(u1.y, u2.y));
                float triMaxV = Mathf.Max(u0.y, Mathf.Max(u1.y, u2.y));
                if (triMaxU < minU - eps || triMinU > maxU + eps || triMaxV < minV - eps || triMinV > maxV + eps)
                    continue;

                int n0 = C2SmpAddCroppedMeshVertexV102LikeOriginal(i0, srcV, srcUv, map, vertices, uvs, minU, minV, du, dv);
                int n1 = C2SmpAddCroppedMeshVertexV102LikeOriginal(i1, srcV, srcUv, map, vertices, uvs, minU, minV, du, dv);
                int n2 = C2SmpAddCroppedMeshVertexV102LikeOriginal(i2, srcV, srcUv, map, vertices, uvs, minU, minV, du, dv);
                if (n0 < 0 || n1 < 0 || n2 < 0 || n0 == n1 || n1 == n2 || n2 == n0)
                    continue;
                tris.Add(n0);
                tris.Add(n1);
                tris.Add(n2);
            }

            if (vertices.Count < 3 || tris.Count < 3)
                return C2SmpCreateOverlayMeshV93LikeOriginal(fallbackBounds);

            Mesh m = new Mesh();
            if (vertices.Count > 65000)
                m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.name = "SMPOverlayCropMesh";
            m.SetVertices(vertices);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0, true);
            m.RecalculateBounds();
            m.RecalculateNormals();
            return m;
        }

        private static int C2SmpAddCroppedMeshVertexV102LikeOriginal(
            int srcIndex,
            Vector3[] srcV,
            Vector2[] srcUv,
            Dictionary<int, int> map,
            List<Vector3> vertices,
            List<Vector2> uvs,
            float minU,
            float minV,
            float du,
            float dv)
        {
            int mapped;
            if (map.TryGetValue(srcIndex, out mapped))
                return mapped;
            if (srcIndex < 0 || srcIndex >= srcV.Length || srcIndex >= srcUv.Length)
                return -1;
            mapped = vertices.Count;
            map[srcIndex] = mapped;
            vertices.Add(srcV[srcIndex]);
            Vector2 uv = srcUv[srcIndex];
            float u = (uv.x - minU) / du;
            float v = (uv.y - minV) / dv;
            uvs.Add(new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v)));
            return mapped;
        }

        private static Material C2SmpCreateOverlayMaterialV93LikeOriginal(Texture2D tex)
        {
            Shader shader = Shader.Find("C2/SMPOverlay");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            Material mat = new Material(shader);
            mat.name = "C2_SMPOverlay_Mat";
            mat.mainTexture = tex;
            mat.color = new Color(1f, 1f, 1f, 0f);
            mat.renderQueue = 3000;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }

        private static void C2SmpSetOverlayAlphaV93LikeOriginal(Material mat, float alpha)
        {
            if (mat == null) return;
            Color c = mat.color;
            c.a = Mathf.Clamp01(alpha);
            mat.color = c;
        }

        private static Mesh C2SmpCreateOverlayMeshV93LikeOriginal(Bounds b)
        {
            Mesh m = new Mesh();
            float y = C2SmpOverlayWorldYOffsetV93LikeOriginal;
            Vector3[] v = new Vector3[4];
            v[0] = new Vector3(b.min.x, y, b.min.z);
            v[1] = new Vector3(b.max.x, y, b.min.z);
            v[2] = new Vector3(b.min.x, y, b.max.z);
            v[3] = new Vector3(b.max.x, y, b.max.z);
            m.vertices = v;
            m.uv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
            m.triangles = new int[] { 0,2,1, 2,3,1 };
            m.RecalculateBounds();
            m.RecalculateNormals();
            return m;
        }

    }
}
