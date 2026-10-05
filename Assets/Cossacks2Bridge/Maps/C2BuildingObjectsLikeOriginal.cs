using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const int C2Buildings3InuRecordSizeLikeOriginal = 54;
        private const int C2BuildingsAlignGroundLikeOriginal = -10000;
        private const int C2BuildingsAlignTopmostLikeOriginal = 10000;
        private const int C2BuildingsRenderQueueLikeOriginal = 3670;
        private const float C2BuildingsAlphaCutoffLikeOriginal = 4.0f / 255.0f;
        internal const float C2BuildingsDepthAlphaCutoffLikeOriginal = 160.0f / 255.0f;
        private const bool C2SpriteDepthPrepassV1LikeOriginal = true;
        private static bool C2SpriteDepthOverlayCameraLikeOriginal => C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera;
        // V264: keep map buildings on the known-good visual path. Unity terrain depth does not
        // match the original ISM/ZBuffer contract, so global building ZTest clips village parts.
        // LINESORT must be solved outside the shared building material.
        private const bool C2BuildingsRespectTerrainDepthV264LikeOriginal = false;
        // V205: old settlement path used Paint.NET-style duplicate-layer compositing:
        // draw the decoded sprite over itself once. RGB stays exact; only straight alpha is recomputed.
        // This restores visible soft shadows / semi-transparent dirt / less grey building parts.
        private const bool C2BuildingsLayerCompositeV205LikeOriginal = true;
        private const float C2BuildingsLayerCompositeTopOpacityV205LikeOriginal = 1.00f;
        // V206: original building #WORK/@WORK animation overlay for mills and mines.
        // The MD body (#STANDLO / #BUILDLO) stays static; #WORK is a separate animated GP/G16 overlay.
        private const bool C2BuildingsDrawWorkAnimationOverlayV206LikeOriginal = true;
        private const float C2BuildingsWorkAnimationFpsV206LikeOriginal = 12.0f;
        private const int C2BuildingsWorkAnimationMaxFramesV206LikeOriginal = 4096;
        private const bool C2BuildingsWorkAnimationAuditV206LikeOriginal = false;
        // V226: diagnose why building textures take too much memory. No visual changes.
        private const bool C2BuildingsTextureMemoryAuditV226LikeOriginal = false; // V260C_LOG_CLEAN
        private const int C2BuildingsTextureMemoryAuditTopV226LikeOriginal = 24;

        // V277: split map/pipeline coordinates from native visual sprite size.
        // Map scale is used for RealX/RealY, BUILDPOINT/BORNPOINT/CONCENTRATOR/LINESORT.
        // Native visual scale is captured once from the original strict-iso presentation scale,
        // then stays constant; camera movement/zoom is observer-only and must not resize sprites.
        private float _c2NativeVisualPixelToWorldScaleV277LikeOriginal = -1.0f;
        private string _c2NativeVisualPixelToWorldScaleSourceV277LikeOriginal = string.Empty;
        private float _c2NativeVisualPixelToWorldScaleOrthoV277LikeOriginal = 0.0f;
        private int _c2NativeVisualPixelToWorldScalePixelHeightV277LikeOriginal = 0;

        // V228: estimate safe lossless alpha-bounds crop for building frames.
        // This only logs diagnostics; it does not change visuals or texture sizes yet.
        private const bool C2BuildingsAlphaBoundsAuditV228LikeOriginal = false; // V260C_LOG_CLEAN
        private const int C2BuildingsAlphaBoundsAuditTopV228LikeOriginal = 24;
        private const byte C2BuildingsAlphaBoundsThresholdV228LikeOriginal = 0;
        // V229: lossless memory cut. Store only the alpha-visible rectangle of every building frame,
        // keep original sprite-space offset in AlphaBounds, and shift mesh vertices back to the same place.
        private const bool C2BuildingsTightCropAlphaV229LikeOriginal = true;
        private const bool C2BuildingsTightCropAuditV229LikeOriginal = false; // V260C_LOG_CLEAN
        // V232: build-sprite disk cache. Store already layer-composited + alpha-cropped RGBA32
        // frames under C2Cache/Buildings, then reuse them on later launches/maps without decoding/cropping again.
        private const bool C2BuildingsPreparedSpriteDiskCacheV232LikeOriginal = true;
        private const int C2BuildingsPreparedSpriteDiskCacheVersionV232LikeOriginal = 232;
        private const string C2BuildingsPreparedSpriteDiskCacheFolderV232LikeOriginal = "Buildings";
        private const int C2BuildingsPreparedSpriteDiskCacheMagicV232LikeOriginal = 0x43324253; // SB2C little-endian marker
        private const float C2BuildingsCosPiOver6LikeOriginal = 0.86602540378443864676f;
        // C2 DrawSpriteBuilding caches original MD vertices and reprojects them when
        // the camera changes. Runtime projection must not change gameplay geometry.
        private const bool C2BuildingsPseudoProjectionVisualV292LikeOriginal = true;

        private GameObject _c2BuildingsRootLikeOriginal;
        private readonly List<C2BuildingPseudoProjectionEntryLikeOriginal> _c2BuildingPseudoProjectionEntriesLikeOriginal =
            new List<C2BuildingPseudoProjectionEntryLikeOriginal>();
        private readonly List<C2BuildingEffectProjectionEntryLikeOriginal> _c2BuildingEffectProjectionEntries =
            new List<C2BuildingEffectProjectionEntryLikeOriginal>();
        private sealed class C2BuildingEffectProjectionEntryLikeOriginal
        {
            public Transform Root, Anchor;
            public C2BuildingMdInfoLikeOriginal Md;
            public float Scale;
            public Vector3 LastRootPosition;
            public readonly Vector3[] Input = new Vector3[1];
            public readonly Vector3[] Output = new Vector3[1];
        }
        private bool _c2BuildingPseudoProjectionCameraValidLikeOriginal;
        private Vector3 _c2BuildingPseudoProjectionLastCameraPosLikeOriginal;
        private Quaternion _c2BuildingPseudoProjectionLastCameraRotLikeOriginal;
        private float _c2BuildingPseudoProjectionLastFovLikeOriginal;
        private int _c2BuildingPseudoProjectionLastPixelWidthLikeOriginal;
        private int _c2BuildingPseudoProjectionLastPixelHeightLikeOriginal;
        private Matrix4x4 _c2BuildingPseudoProjectionLastMatrixLikeOriginal;
        private Rect _c2BuildingPseudoProjectionLastPixelRectLikeOriginal;

        private sealed class C2BuildingPseudoProjectionEntryLikeOriginal
        {
            public Transform Root;
            public Mesh Mesh;
            public Vector3[][] OriginalDrawFrames;
            public Vector3[] Output = new Vector3[4];
            public float Scale;
            public C2BuildingMdInfoLikeOriginal Md;
            public C2BuildingWorkFrameAnimatorV206LikeOriginal Animator;
            public C2BuildingLoadedPartLikeOriginal SourcePart;
            public int LastFrame = -1;
            public Vector3 LastRootPosition;
            public bool RootPositionValid;
            public bool ProjectionCulled;
        }

        private static readonly Dictionary<string, C2BuildingMdInfoLikeOriginal> s_C2BuildingMdCacheLikeOriginal =
            new Dictionary<string, C2BuildingMdInfoLikeOriginal>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> s_C2BuildingVisualPathCacheLikeOriginal =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Texture2D> s_C2BuildingTextureCacheLikeOriginal =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private sealed class C2BuildingTextureDiagV226LikeOriginal
        {
            public string Key = string.Empty;
            public string Package = string.Empty;
            public string Path = string.Empty;
            public int Frame = -1;
            public string Source = string.Empty;
            public Texture2D Texture;
            public int Requests;
            public int StaticUses;
            public int WorkUses;
            public bool CreatedThisBuild;
            public C2BuildingAlphaBoundsDiagV228LikeOriginal AlphaBoundsV228;
        }

        private sealed class C2BuildingAlphaBoundsDiagV228LikeOriginal
        {
            public int OriginalWidth;
            public int OriginalHeight;
            public int MinX;
            public int MinY;
            public int MaxX;
            public int MaxY;
            public int CropWidth;
            public int CropHeight;
            public long OriginalPixels;
            public long CropPixels;
            public long VisiblePixels;
            public bool HasVisiblePixels;
            public bool TightCroppedV229;

            public double CropRatio
            {
                get { return OriginalPixels > 0L ? (double)CropPixels / (double)OriginalPixels : 1.0; }
            }

            public double SavingPercent
            {
                get { return Mathf.Clamp((float)((1.0 - CropRatio) * 100.0), 0.0f, 100.0f); }
            }
        }

        private static int s_C2BuildingTextureRequestsV226LikeOriginal;
        private static int s_C2BuildingTextureCacheHitsV226LikeOriginal;
        private static int s_C2BuildingTextureCacheMissesV226LikeOriginal;
        private static int s_C2BuildingTextureCreatedV226LikeOriginal;
        private static int s_C2BuildingTextureStaticRefsV226LikeOriginal;
        private static int s_C2BuildingTextureWorkRefsV226LikeOriginal;
        private static readonly Dictionary<string, int> s_C2BuildingTextureRequestTopV226LikeOriginal =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<EntityId, C2BuildingTextureDiagV226LikeOriginal> s_C2BuildingTextureDiagByInstanceV226LikeOriginal =
            new Dictionary<EntityId, C2BuildingTextureDiagV226LikeOriginal>();
        private static readonly Dictionary<string, C2BuildingTextureDiagV226LikeOriginal> s_C2BuildingTextureDiagByKeyV226LikeOriginal =
            new Dictionary<string, C2BuildingTextureDiagV226LikeOriginal>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<EntityId, C2BuildingAlphaBoundsDiagV228LikeOriginal> s_C2BuildingAlphaBoundsByTextureIdV228LikeOriginal =
            new Dictionary<EntityId, C2BuildingAlphaBoundsDiagV228LikeOriginal>();

        private static int s_C2BuildingTightCropCreatedV229LikeOriginal;
        private static long s_C2BuildingTightCropOriginalPixelsV229LikeOriginal;
        private static long s_C2BuildingTightCropKeptPixelsV229LikeOriginal;
        private static int s_C2BuildingPreparedSpriteDiskHitsV232LikeOriginal;
        private static int s_C2BuildingPreparedSpriteDiskMissesV232LikeOriginal;
        private static int s_C2BuildingPreparedSpriteDiskWritesV232LikeOriginal;
        private static int s_C2BuildingPreparedSpriteDiskWriteFailsV232LikeOriginal;
        private static int s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal;
        private static bool s_C2DepthV233LoggedLikeOriginal;
        private static bool s_C2BuildingSpriteScaleV276LoggedLikeOriginal;

        private static readonly Dictionary<EntityId, Material> s_C2BuildingMaterialCacheLikeOriginal =
            new Dictionary<EntityId, Material>();
        private static readonly Dictionary<(EntityId TextureId, bool Ground), Material> s_C2BuildingDepthMaterialCacheLikeOriginal =
            new Dictionary<(EntityId TextureId, bool Ground), Material>();

        // V207: same safety path as the old working settlement parser:
        // some GP/G16/G17 packages (notably UnitsG17\FrnMel from Data\Cash\UNITSG17_FRNMEL.g16)
        // fail through absolute LoadG16ToMemory with "decoded 0 frames", but decode correctly through
        // Melinoja package/alias keys such as UnitsG17\FrnMel, FrnMel, UNITSG17_FRNMEL.
        private static readonly HashSet<string> s_C2BuildingLoadedGpKeysV207LikeOriginal =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // V206: original nation NDS indirection: map IDs like BldMel(FR) / BldRudCoal(FR)
        // resolve to real MD visual keys before fallback aliases.
        private static Dictionary<string, string> s_C2BuildingNdsUnitToMdV206LikeOriginal;
        private static string s_C2BuildingNdsAliasAuditV206LikeOriginal = "not_built";

        private struct C2Building3InuRecordLikeOriginal
        {
            public int Index;
            public byte Nation;
            public ushort NIndex;
            public int RealX;
            public int RealY;
            public ushort Life;
            public ushort Stage;
            public short WallX;
            public short WallY;
            public byte RealDir;
            public byte Flags;
            public string MonsterId;
        }

        private struct C2BuildingAnimFrameLikeOriginal
        {
            public int FileRef;
            public int SpriteId;

            public C2BuildingAnimFrameLikeOriginal(int fileRef, int spriteId)
            {
                FileRef = fileRef;
                SpriteId = spriteId;
            }
        }

        private struct C2BuildingLineSortLikeOriginal
        {
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;

            public bool IsGround => X1 == C2BuildingsAlignGroundLikeOriginal;
            public bool IsTop => X1 == C2BuildingsAlignTopmostLikeOriginal;

            public C2BuildingLineSortLikeOriginal(int x1, int y1, int x2, int y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
        }

        private sealed class C2BuildingAnimationLikeOriginal
        {
            public string Name = string.Empty;
            public int Rotations = 1;
            public readonly List<C2BuildingAnimFrameLikeOriginal> Frames = new List<C2BuildingAnimFrameLikeOriginal>();
            public readonly List<C2BuildingLineSortLikeOriginal> LineSort = new List<C2BuildingLineSortLikeOriginal>();
        }

        private sealed class C2BuildingLoadedPartLikeOriginal
        {
            public Texture2D Texture;
            public C2BuildingAnimFrameLikeOriginal Frame;
            public C2BuildingLineSortLikeOriginal LineSort;
            public bool HasLineSort;
            public string AnimationName = string.Empty;
        }

        private struct C2Building3DBarLikeOriginal
        {
            public int X;
            public int Y;
            public int L1;
            public int L2;
            public int Height;

            public C2Building3DBarLikeOriginal(int x, int y, int l1, int l2, int height)
            {
                X = x;
                Y = y;
                L1 = l1;
                L2 = l2;
                Height = height;
            }
        }

        private sealed class C2BuildingMdInfoLikeOriginal
        {
            public bool Found;
            public string MdPath = string.Empty;
            public string MdName = string.Empty;
            public string Package = string.Empty;
            public bool Building;
            public bool SpriteObject;
            public bool NotSelectable;
            public int Dx;
            public int Dy;
            public int PicDx;
            public int PicDy;
            public int PicLx;
            public int PicLy;
            public float PlaneFactor = 1.0f;
            public bool Use3pAlign;
            public int AlignPt1x;
            public int AlignPt1y;
            public int AlignPt1z;
            public int AlignPt2x;
            public int AlignPt2y;
            public int AlignPt2z;
            public int AlignPt3x;
            public int AlignPt3y;
            public int AlignPt3z;
            public int Life;
            public int SetAnmParamDx;
            public int SetAnmParamDy;
            public int SetAnmParamParts = 1;
            public int SetAnmParamPartSize = 96;
            public int BuildStages;
            public bool HasBuildBar;
            public int BuildBarX0;
            public int BuildBarY0;
            public int BuildBarX1;
            public int BuildBarY1;
            public int DestructProbability;
            public string PieceName = string.Empty;
            public string Usage = string.Empty;
            public bool Immortal;
            public bool SlowDeath;
            public bool NoFullDestruct;
            public readonly List<Vector2> FirePoints = new List<Vector2>();
            public readonly List<Vector2> SmokePoints = new List<Vector2>();
            public readonly List<string> DestructWeapons = new List<string>();
            public readonly Dictionary<int, string> RlcPackages = new Dictionary<int, string>();
            public readonly Dictionary<int, int> RlcDx = new Dictionary<int, int>();
            public readonly Dictionary<int, int> RlcDy = new Dictionary<int, int>();
            public readonly Dictionary<string, C2BuildingAnimationLikeOriginal> Animations =
                new Dictionary<string, C2BuildingAnimationLikeOriginal>(StringComparer.OrdinalIgnoreCase);
            public readonly List<C2BuildingAnimFrameLikeOriginal> StandLoFrames = new List<C2BuildingAnimFrameLikeOriginal>();
            public readonly List<C2BuildingLineSortLikeOriginal> StandLoLineSort = new List<C2BuildingLineSortLikeOriginal>();
            // #WORK/@WORK is not baked into the static building body. The original switches it over time
            // as a separate animation overlay (mills, mines, etc.).
            public readonly List<C2BuildingAnimFrameLikeOriginal> WorkFrames = new List<C2BuildingAnimFrameLikeOriginal>();
            public readonly List<Vector2> BornPoints = new List<Vector2>();
            public readonly List<Vector2> Concentrator = new List<Vector2>();
            public readonly List<Vector2> BornPoints2 = new List<Vector2>();
            public readonly List<Vector2> Concentrator2 = new List<Vector2>();
            public readonly List<Vector2> LockPoints = new List<Vector2>();
            public readonly List<Vector2> BuildLockPoints = new List<Vector2>();
            public readonly List<Vector2> CheckPoints = new List<Vector2>();
            public readonly List<Vector2> BuildPoints = new List<Vector2>();
            public readonly List<C2Building3DBarLikeOriginal> Bars3D = new List<C2Building3DBarLikeOriginal>();
        }

        internal struct C2BuildingMatrix4LikeOriginal
        {
            public float e00, e01, e02, e03;
            public float e10, e11, e12, e13;
            public float e20, e21, e22, e23;
            public float e30, e31, e32, e33;

            public static C2BuildingMatrix4LikeOriginal Identity()
            {
                C2BuildingMatrix4LikeOriginal m = new C2BuildingMatrix4LikeOriginal();
                m.e00 = 1.0f;
                m.e11 = 1.0f;
                m.e22 = 1.0f;
                m.e33 = 1.0f;
                return m;
            }

            public static C2BuildingMatrix4LikeOriginal Translation(Vector3 t)
            {
                C2BuildingMatrix4LikeOriginal m = Identity();
                m.e30 = t.x;
                m.e31 = t.y;
                m.e32 = t.z;
                return m;
            }

            public static C2BuildingMatrix4LikeOriginal RotationYZ(float cosPhi, float sinPhi)
            {
                C2BuildingMatrix4LikeOriginal m = Identity();
                m.e11 = cosPhi;
                m.e12 = sinPhi;
                m.e21 = -sinPhi;
                m.e22 = cosPhi;
                return m;
            }

            public static C2BuildingMatrix4LikeOriginal ShearXZ(float cosPhi, float sinPhi)
            {
                C2BuildingMatrix4LikeOriginal m = Identity();
                if (Mathf.Abs(cosPhi) > 0.000001f)
                    m.e02 = sinPhi / cosPhi;
                return m;
            }

            public static C2BuildingMatrix4LikeOriginal operator *(C2BuildingMatrix4LikeOriginal a, C2BuildingMatrix4LikeOriginal b)
            {
                C2BuildingMatrix4LikeOriginal r = new C2BuildingMatrix4LikeOriginal();
                r.e00 = a.e00 * b.e00 + a.e01 * b.e10 + a.e02 * b.e20 + a.e03 * b.e30;
                r.e01 = a.e00 * b.e01 + a.e01 * b.e11 + a.e02 * b.e21 + a.e03 * b.e31;
                r.e02 = a.e00 * b.e02 + a.e01 * b.e12 + a.e02 * b.e22 + a.e03 * b.e32;
                r.e03 = a.e00 * b.e03 + a.e01 * b.e13 + a.e02 * b.e23 + a.e03 * b.e33;
                r.e10 = a.e10 * b.e00 + a.e11 * b.e10 + a.e12 * b.e20 + a.e13 * b.e30;
                r.e11 = a.e10 * b.e01 + a.e11 * b.e11 + a.e12 * b.e21 + a.e13 * b.e31;
                r.e12 = a.e10 * b.e02 + a.e11 * b.e12 + a.e12 * b.e22 + a.e13 * b.e32;
                r.e13 = a.e10 * b.e03 + a.e11 * b.e13 + a.e12 * b.e23 + a.e13 * b.e33;
                r.e20 = a.e20 * b.e00 + a.e21 * b.e10 + a.e22 * b.e20 + a.e23 * b.e30;
                r.e21 = a.e20 * b.e01 + a.e21 * b.e11 + a.e22 * b.e21 + a.e23 * b.e31;
                r.e22 = a.e20 * b.e02 + a.e21 * b.e12 + a.e22 * b.e22 + a.e23 * b.e32;
                r.e23 = a.e20 * b.e03 + a.e21 * b.e13 + a.e22 * b.e23 + a.e23 * b.e33;
                r.e30 = a.e30 * b.e00 + a.e31 * b.e10 + a.e32 * b.e20 + a.e33 * b.e30;
                r.e31 = a.e30 * b.e01 + a.e31 * b.e11 + a.e32 * b.e21 + a.e33 * b.e31;
                r.e32 = a.e30 * b.e02 + a.e31 * b.e12 + a.e32 * b.e22 + a.e33 * b.e32;
                r.e33 = a.e30 * b.e03 + a.e31 * b.e13 + a.e32 * b.e23 + a.e33 * b.e33;
                return r;
            }

            public Vector3 TransformPoint(Vector3 pt)
            {
                float x = pt.x * e00 + pt.y * e10 + pt.z * e20 + e30;
                float y = pt.x * e01 + pt.y * e11 + pt.z * e21 + e31;
                float z = pt.x * e02 + pt.y * e12 + pt.z * e22 + e32;
                return new Vector3(x, y, z);
            }

            public void Translate(Vector3 t)
            {
                e30 += t.x;
                e31 += t.y;
                e32 += t.z;
            }
        }

        private void BuildBuildingObjectsLayerLikeOriginal()
        {
            if (_terrainRoot == null || _map == null || _bootstrap == null || _bootstrap.Fs == null)
                return;

            if (_c2BuildingsRootLikeOriginal != null)
                SafeDestroy(_c2BuildingsRootLikeOriginal);

            _c2BuildingPseudoProjectionEntriesLikeOriginal.Clear();
            _c2BuildingEffectProjectionEntries.Clear();
            _projectedBuildingLineOverlays.Clear();
            s_buildingProjectionFailures.Clear();
            _c2BuildingPseudoProjectionCameraValidLikeOriginal = false;

            ResetBuildingTextureDiagnosticsV226LikeOriginal();

            List<C2Building3InuRecordLikeOriginal> records;
            string parseAudit;
            if (!TryParseBuilding3InuRecordsLikeOriginal(_bootstrap.Fs, _mapRelativePath, out records, out parseAudit))
            {
                Debug.Log("[C2:BUILDINGS 3INU] no building records: " + parseAudit);
                return;
            }

            _c2BuildingsRootLikeOriginal = new GameObject("C2_Buildings_3INU_MD_DrawSpriteBuilding");
            _c2BuildingsRootLikeOriginal.transform.SetParent(_terrainRoot.transform, false);

            int mdFound = 0;
            int mdMissing = 0;
            int buildingRecords = 0;
            int drawn = 0;
            int visualMissing = 0;
            var miss = new List<string>();
            var samples = new List<string>();

            for (int i = 0; i < records.Count; i++)
            {
                C2Building3InuRecordLikeOriginal r = records[i];
                C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(r.MonsterId);
                if (md.Found) mdFound++;
                else
                {
                    mdMissing++;
                    AddLimitedLikeOriginal(miss, r.MonsterId + ":md", 20);
                    continue;
                }

                if (!md.Building)
                    continue;

                buildingRecords++;
                List<C2BuildingLoadedPartLikeOriginal> parts;
                string visualAudit;
                if (!TryLoadBuildingPartsLikeOriginal(md, r, out parts, out visualAudit) || parts.Count == 0)
                {
                    visualMissing++;
                    AddLimitedLikeOriginal(miss, r.MonsterId + ":visual " + visualAudit, 20);
                    continue;
                }

                CreateBuildingCompositeLikeOriginal(_c2BuildingsRootLikeOriginal.transform, r, md, parts);
                drawn++;
                if (samples.Count < 12)
                {
                    samples.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) +
                                " " + r.MonsterId +
                                " md=" + Path.GetFileName(md.MdPath) +
                                " parts=" + parts.Count.ToString(CultureInfo.InvariantCulture) +
                                " work=" + md.WorkFrames.Count.ToString(CultureInfo.InvariantCulture) +
                                " lineSort=" + md.StandLoLineSort.Count.ToString(CultureInfo.InvariantCulture) +
                                " born2=" + md.BornPoints2.Count.ToString(CultureInfo.InvariantCulture) +
                                " conc2=" + md.Concentrator2.Count.ToString(CultureInfo.InvariantCulture));
                }
            }

            Debug.Log("[C2:BUILDINGS 3INU] contract=3INU_MD_BUILDING_DrawSpriteBuilding_LINESORT_DEPTH_V260J_ANCHOR_RESTORE_DEATH_MD records=" +
                      records.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildingRecords=" + buildingRecords.ToString(CultureInfo.InvariantCulture) +
                      " mdFound=" + mdFound.ToString(CultureInfo.InvariantCulture) +
                      " mdMissing=" + mdMissing.ToString(CultureInfo.InvariantCulture) +
                      " visualMissing=" + visualMissing.ToString(CultureInfo.InvariantCulture) +
                      " drawn=" + drawn.ToString(CultureInfo.InvariantCulture) +
                      " parse=[" + parseAudit + "] samples=" + string.Join(" | ", samples.ToArray()));

            if (miss.Count > 0)
                Debug.LogWarning("[C2:BUILDINGS 3INU MISS] " + string.Join(" | ", miss.ToArray()));

            LogBuildingTextureDiagnosticsV226LikeOriginal(buildingRecords, drawn, visualMissing);
        }

        private static bool TryParseBuilding3InuRecordsLikeOriginal(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string relativePath,
            out List<C2Building3InuRecordLikeOriginal> records,
            out string audit)
        {
            records = new List<C2Building3InuRecordLikeOriginal>();
            audit = string.Empty;

            if (fs == null || string.IsNullOrWhiteSpace(relativePath) || !fs.Exists(relativePath))
            {
                audit = "map_not_found:" + (relativePath ?? string.Empty);
                return false;
            }

            byte[] raw = fs.ReadAllBytes(relativePath);
            byte[] data = MaybeDecompressM3d(raw, out string error);
            if (data == null || data.Length < 16)
            {
                audit = "bad_data:" + error;
                return false;
            }

            using (var ms = new MemoryStream(data, false))
            using (var br = new BinaryReader(ms))
            {
                string magic = ReadTag(br);
                if (!TryGetAddshFromMapMagic(magic, out _))
                {
                    audit = "bad_magic:" + magic;
                    return false;
                }

                int storedVertInLine = br.ReadInt32();
                int storedMaxTh = br.ReadInt32();
                int chunks = 0;
                int unitChunks = 0;
                var seen = new List<string>();

                while (ms.Position + 8 <= ms.Length)
                {
                    string tag = ReadTag(br);
                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal) ||
                        string.Equals(tag, "MDNE", StringComparison.Ordinal))
                        break;

                    int sizeField = br.ReadInt32();
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;
                    long payloadEnd = payloadStart + payloadLen;
                    if (payloadEnd > ms.Length)
                        break;

                    chunks++;
                    if (seen.Count < 24)
                        seen.Add(tag + ":" + sizeField.ToString(CultureInfo.InvariantCulture));

                    if (TagEqualsLikeOriginal(tag, "3INU", "UNI3") && payloadLen >= 4)
                    {
                        int declared = br.ReadInt32();
                        int possible = Mathf.Max(0, (payloadLen - 4) / C2Buildings3InuRecordSizeLikeOriginal);
                        int count = Mathf.Clamp(declared, 0, possible);
                        for (int i = 0; i < count && br.BaseStream.Position + C2Buildings3InuRecordSizeLikeOriginal <= payloadEnd; i++)
                        {
                            var r = new C2Building3InuRecordLikeOriginal();
                            r.Index = records.Count;
                            r.Nation = br.ReadByte();
                            r.NIndex = br.ReadUInt16();
                            r.RealX = br.ReadInt32();
                            r.RealY = br.ReadInt32();
                            r.Life = br.ReadUInt16();
                            r.Stage = br.ReadUInt16();
                            r.WallX = br.ReadInt16();
                            r.WallY = br.ReadInt16();
                            r.RealDir = br.ReadByte();
                            r.Flags = br.ReadByte();
                            r.MonsterId = DecodeCString1251LikeOriginal(br.ReadBytes(33));
                            records.Add(r);
                        }
                        unitChunks++;
                    }

                    ms.Position = payloadEnd;
                }

                audit = "magic=" + magic +
                        " stored=" + storedVertInLine.ToString(CultureInfo.InvariantCulture) + "x" + storedMaxTh.ToString(CultureInfo.InvariantCulture) +
                        " chunks=" + chunks.ToString(CultureInfo.InvariantCulture) +
                        " unitChunks=" + unitChunks.ToString(CultureInfo.InvariantCulture) +
                        " records=" + records.Count.ToString(CultureInfo.InvariantCulture) +
                        " seen=" + string.Join(",", seen.ToArray());
                return records.Count > 0;
            }
        }

        private static C2BuildingMdInfoLikeOriginal ResolveBuildingMdLikeOriginal(string monsterId)
        {
            string key = string.IsNullOrWhiteSpace(monsterId) ? "<empty>" : monsterId.Trim();
            if (s_C2BuildingMdCacheLikeOriginal.TryGetValue(key, out C2BuildingMdInfoLikeOriginal cached))
                return cached;

            var info = new C2BuildingMdInfoLikeOriginal();
            info.MdName = key;
            string path = FindBuildingMdPathLikeOriginal(key);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                info.Found = false;
                s_C2BuildingMdCacheLikeOriginal[key] = info;
                return info;
            }

            info.Found = true;
            info.MdPath = path;
            ParseBuildingMdLikeOriginal(info, path);
            s_C2BuildingMdCacheLikeOriginal[key] = info;
            return info;
        }

        private static string FindBuildingMdPathLikeOriginal(string monsterId)
        {
            List<string> names = BuildMdNameCandidatesLikeOriginal(monsterId);
            List<string> roots = DataRootsForBuildingsLikeOriginal();
            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    continue;

                for (int n = 0; n < names.Count; n++)
                {
                    string name = names[n];
                    string[] rels =
                    {
                        name + ".md",
                        name + ".MD",
                        Path.Combine("UnitsMD", name + ".md"),
                        Path.Combine("UnitsMD", name + ".MD"),
                        Path.Combine("UnitsMD", "Units", name + ".md"),
                        Path.Combine("Units", name + ".md")
                    };

                    for (int i = 0; i < rels.Length; i++)
                    {
                        string path = Path.Combine(root, rels[i]);
                        if (File.Exists(path))
                            return path;
                    }
                }
            }

            return string.Empty;
        }

        private static string ResolveBuildingNdsMdAliasV206LikeOriginal(string monsterId)
        {
            if (string.IsNullOrWhiteSpace(monsterId))
                return string.Empty;

            EnsureBuildingNdsAliasMapV206LikeOriginal();
            if (s_C2BuildingNdsUnitToMdV206LikeOriginal == null || s_C2BuildingNdsUnitToMdV206LikeOriginal.Count == 0)
                return string.Empty;

            string raw = monsterId.Trim();
            if (s_C2BuildingNdsUnitToMdV206LikeOriginal.TryGetValue(raw, out string md) && !string.IsNullOrWhiteSpace(md))
                return md;

            string clean = SanitizeNameLikeOriginal(raw);
            if (!string.IsNullOrEmpty(clean) && !string.Equals(clean, raw, StringComparison.OrdinalIgnoreCase) &&
                s_C2BuildingNdsUnitToMdV206LikeOriginal.TryGetValue(clean, out md) && !string.IsNullOrWhiteSpace(md))
                return md;

            return string.Empty;
        }

        private static void EnsureBuildingNdsAliasMapV206LikeOriginal()
        {
            if (s_C2BuildingNdsUnitToMdV206LikeOriginal != null)
                return;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> roots = DataRootsForBuildingsLikeOriginal();
            for (int i = 0; i < roots.Count; i++)
            {
                AddBuildingNdsSearchDirV206LikeOriginal(dirs, roots[i]);
                try
                {
                    var di = new DirectoryInfo(roots[i]);
                    if (di.Exists && di.Parent != null)
                        AddBuildingNdsSearchDirV206LikeOriginal(dirs, di.Parent.FullName);
                }
                catch { }
            }

            foreach (string dir in dirs)
            {
                try
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                        continue;
                    string[] upper = Directory.GetFiles(dir, "*.NDS", SearchOption.TopDirectoryOnly);
                    for (int i = 0; upper != null && i < upper.Length; i++) files.Add(upper[i]);
                    string[] lower = Directory.GetFiles(dir, "*.nds", SearchOption.TopDirectoryOnly);
                    for (int i = 0; lower != null && i < lower.Length; i++) files.Add(lower[i]);
                }
                catch { }
            }

            int linesParsed = 0;
            foreach (string file in files)
            {
                string[] lines;
                try { lines = File.ReadAllLines(file, Encoding.GetEncoding(1251)); }
                catch
                {
                    try { lines = File.ReadAllLines(file); }
                    catch { continue; }
                }

                for (int i = 0; lines != null && i < lines.Length; i++)
                {
                    string line = StripCommentLikeOriginal(lines[i]).Trim();
                    if (line.Length == 0 || line[0] == '/')
                        continue;

                    string[] t = SplitTokensLikeOriginal(line);
                    if (t == null || t.Length < 2)
                        continue;

                    string unitId = (t[0] ?? string.Empty).Trim();
                    string mdName = (t[1] ?? string.Empty).Trim();
                    if (unitId.Length == 0 || mdName.Length == 0)
                        continue;
                    if (unitId.IndexOf('(') < 0 || unitId.IndexOf(')') < 0)
                        continue;
                    if (mdName.IndexOf('(') >= 0 || mdName.IndexOf(')') >= 0)
                        continue;
                    if (mdName.IndexOf('%') >= 0 || mdName.IndexOf('=') >= 0)
                        continue;
                    if (int.TryParse(mdName, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                        continue;
                    if (string.Equals(mdName, "GRP", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(mdName, "LIFE", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(mdName, "BUILD", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!map.ContainsKey(unitId))
                        map.Add(unitId, mdName);
                    linesParsed++;
                }
            }

            s_C2BuildingNdsUnitToMdV206LikeOriginal = map;
            s_C2BuildingNdsAliasAuditV206LikeOriginal = "files=" + files.Count.ToString(CultureInfo.InvariantCulture) +
                " dirs=" + dirs.Count.ToString(CultureInfo.InvariantCulture) +
                " aliases=" + map.Count.ToString(CultureInfo.InvariantCulture) +
                " parsedLines=" + linesParsed.ToString(CultureInfo.InvariantCulture);
        }

        private static void AddBuildingNdsSearchDirV206LikeOriginal(HashSet<string> dirs, string path)
        {
            if (dirs == null || string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string p = Path.GetFullPath(path.Trim());
                dirs.Add(p);

                string name = Path.GetFileName(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.Equals(name, "Cash", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "UnitsMD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "UnitsG17", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "Resources", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "Data1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "Data", StringComparison.OrdinalIgnoreCase))
                {
                    var di = new DirectoryInfo(p);
                    if (di.Parent != null)
                        dirs.Add(di.Parent.FullName);
                }
            }
            catch { }
        }

        private static List<string> BuildMdNameCandidatesLikeOriginal(string monsterId)
        {
            var list = new List<string>();
            void Add(string s)
            {
                if (!string.IsNullOrWhiteSpace(s) && !list.Exists(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                    list.Add(s.Trim());
            }

            string raw = (monsterId ?? string.Empty).Trim();
            int p0 = raw.IndexOf('(');
            int p1 = raw.IndexOf(')');
            string baseName = p0 > 0 ? raw.Substring(0, p0).Trim() : raw;
            string suffix = p0 >= 0 && p1 > p0 ? raw.Substring(p0 + 1, p1 - p0 - 1).Trim() : string.Empty;

            // V206: first trust the original *.NDS mapping. This is how logical map IDs
            // point to the actual MD/visual name in the engine.
            string ndsAlias = ResolveBuildingNdsMdAliasV206LikeOriginal(raw);
            Add(ndsAlias);

            string mineAlias = StrictMineAliasMdNameLikeOriginal(baseName);
            Add(mineAlias);
            Add(raw);
            Add(baseName);

            string nat = NationPrefixLikeOriginal(suffix);
            if (!string.IsNullOrEmpty(nat))
            {
                Add(baseName + nat);
                Add(baseName + "_" + nat);
                Add(nat + baseName);
                Add(nat + "_" + baseName);

                if (string.Equals(baseName, "BldMel", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(nat, "Spn", StringComparison.OrdinalIgnoreCase))
                    {
                        Add("SpnMil");
                        Add("SpnMilN");
                    }
                    else
                    {
                        Add(nat + "Mel");
                        Add(nat + "MelN");
                        Add("N" + nat + "Mel");
                    }
                }
            }

            if (string.Equals(baseName, "BldRudCoal", StringComparison.OrdinalIgnoreCase)) Add("BldRudSel");
            if (string.Equals(baseName, "BldRudIron", StringComparison.OrdinalIgnoreCase)) Add("BldRudRud");
            if (string.Equals(baseName, "BldRudGold", StringComparison.OrdinalIgnoreCase)) Add("BldRudGln");
            if (string.Equals(baseName, "BldRudStone", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "BldRudSton", StringComparison.OrdinalIgnoreCase)) Add("BldRudKam");

            Add(SanitizeNameLikeOriginal(raw));
            Add(SanitizeNameLikeOriginal(baseName));
            return list;
        }

        private static void ParseBuildingMdLikeOriginal(C2BuildingMdInfoLikeOriginal info, string path)
        {
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch { lines = File.ReadAllLines(path); }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripCommentLikeOriginal(lines[i]).Trim();
                if (line.Length == 0)
                    continue;

                // V209: some original/working MDs carry a slash-prefixed explicit #WORK list
                // right after an @WORK range. This list is not a normal comment for us: it is
                // the corrected work-frame sequence used to avoid bad transitional frames
                // (FrnMel/RusMel skip frame 18). Keep slash comments ignored for everything
                // else, but allow /#WORK and /@WORK to override the compact range.
                if (line[0] == '/')
                {
                    if (line.StartsWith("/#WORK", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("/@WORK", StringComparison.OrdinalIgnoreCase))
                    {
                        line = line.Substring(1).Trim();
                        if (line.Length == 0)
                            continue;
                    }
                    else
                    {
                        continue;
                    }
                }

                string[] t = SplitTokensLikeOriginal(line);
                if (t.Length == 0)
                    continue;

                string cmdRaw = t[0];
                string cmd = cmdRaw.ToUpperInvariant();
                if (cmd == "NAME" && t.Length >= 2)
                {
                    info.MdName = t[1];
                }
                else if (cmd == "BUILDING")
                {
                    info.Building = true;
                }
                else if (cmd == "BUILDBAR" && t.Length >= 5)
                {
                    // COSSACKS2/NewMon.cpp keeps these four MD values and combines
                    // them with LOCATION when CreatePlaneUnderBuilding is called.
                    info.BuildBarX0 = ToIntLikeOriginal(t[1]);
                    info.BuildBarY0 = ToIntLikeOriginal(t[2]);
                    info.BuildBarX1 = ToIntLikeOriginal(t[3]);
                    info.BuildBarY1 = ToIntLikeOriginal(t[4]);
                    info.HasBuildBar = true;
                }
                else if (cmd == "SPRITEOBJECT")
                {
                    info.SpriteObject = true;
                }
                else if (cmd == "NOTSELECTABLE")
                {
                    info.NotSelectable = true;
                }
                else if (cmd == "IMMORTAL")
                {
                    info.Immortal = true;
                }
                else if (cmd == "SLOWDEATH")
                {
                    info.SlowDeath = true;
                }
                else if (cmd == "NOFULLDESTRUCT")
                {
                    info.NoFullDestruct = true;
                }
                else if (cmd == "FIRES")
                {
                    // Original NewMon.cpp/mdParser.cpp: FIRES <count> <x0> <y0> ...
                    // Draw-space offsets from the building sprite origin.
                    ParsePointListLikeOriginal(t, info.FirePoints, false);
                }
                else if (cmd == "SMOKE")
                {
                    // Original mdParser.cpp: SMOKE <count> <x0> <y0> ...
                    ParsePointListLikeOriginal(t, info.SmokePoints, false);
                }
                else if (cmd == "USAGE" && t.Length >= 2)
                {
                    info.Usage = t[1];
                }
                else if (cmd == "SETANMPARAM" && t.Length >= 5)
                {
                    info.SetAnmParamDx = ToIntLikeOriginal(t[1]);
                    info.SetAnmParamDy = ToIntLikeOriginal(t[2]);
                    info.SetAnmParamParts = ToIntLikeOriginal(t[3]);
                    info.SetAnmParamPartSize = ToIntLikeOriginal(t[4]);
                }
                else if (cmd == "LOCATION" && t.Length >= 5)
                {
                    info.PicDx = ToIntLikeOriginal(t[1]);
                    info.PicDy = ToIntLikeOriginal(t[2]);
                    info.PicLx = ToIntLikeOriginal(t[3]);
                    info.PicLy = ToIntLikeOriginal(t[4]);
                }
                else if (cmd == "PLANEFACTOR" && t.Length >= 2)
                {
                    info.PlaneFactor = ToFloatLikeOriginal(t[1], info.PlaneFactor);
                }
                else if (cmd == "ALIGN_WITH_3POINTS" && t.Length >= 10)
                {
                    info.AlignPt1x = ToIntLikeOriginal(t[1]);
                    info.AlignPt1y = ToIntLikeOriginal(t[2]);
                    info.AlignPt1z = ToIntLikeOriginal(t[3]);
                    info.AlignPt2x = ToIntLikeOriginal(t[4]);
                    info.AlignPt2y = ToIntLikeOriginal(t[5]);
                    info.AlignPt2z = ToIntLikeOriginal(t[6]);
                    info.AlignPt3x = ToIntLikeOriginal(t[7]);
                    info.AlignPt3y = ToIntLikeOriginal(t[8]);
                    info.AlignPt3z = ToIntLikeOriginal(t[9]);
                    info.Use3pAlign = true;
                }
                else if (cmd == "LIFE" && t.Length >= 2)
                {
                    info.Life = ToIntLikeOriginal(t[1]);
                }
                else if ((cmd == "USERLC" || cmd == "USERLCEXT") && t.Length >= 6)
                {
                    int shift = cmd == "USERLCEXT" ? 2 : 0;
                    if (t.Length >= 6 + shift)
                    {
                        int fileRef = ToIntLikeOriginal(t[1]);
                        string pkg = CleanPackageNameLikeOriginal(t[2 + shift]);
                        int dx = ToIntLikeOriginal(t[4 + shift]);
                        int dy = ToIntLikeOriginal(t[5 + shift]);
                        if (string.IsNullOrEmpty(info.Package))
                            info.Package = pkg;
                        info.RlcPackages[fileRef] = pkg;
                        info.RlcDx[fileRef] = dx;
                        info.RlcDy[fileRef] = dy;
                        info.Dx = dx;
                        info.Dy = dy;
                    }
                }
                else if (cmd == "BORNPOINTS")
                {
                    // Original NewMon.cpp:
                    // BORNPOINTS are cell points converted to exact local pixels: x*16+8, y*16+8.
                    // Build.cpp then spawns at Real=((corner<<4)+BornPt)<<4.
                    info.BornPoints2.Clear();
                    ParseServicePointListLikeOriginal(t, info.BornPoints, false);
                }
                else if (cmd == "CONCENTRATOR")
                {
                    // Same original conversion as BORNPOINTS: x*16+8, y*16+8.
                    info.Concentrator2.Clear();
                    ParseServicePointListLikeOriginal(t, info.Concentrator, false);
                }
                else if (cmd == "BORNPOINTS2")
                {
                    // Original BORNPOINTS2: x raw, y<<1.
                    info.BornPoints.Clear();
                    ParseServicePointListLikeOriginal(t, info.BornPoints2, true);
                }
                else if (cmd == "CONCENTRATOR2")
                {
                    // Original CONCENTRATOR2: x raw, y<<1, and if no BORNPOINTS were defined,
                    // copy concentrator points reversed into born points.
                    bool hadBornPoints = info.BornPoints.Count > 0 || info.BornPoints2.Count > 0;
                    info.Concentrator.Clear();
                    ParseServicePointListLikeOriginal(t, info.Concentrator2, true);
                    if (!hadBornPoints && info.BornPoints.Count == 0 && info.BornPoints2.Count == 0)
                    {
                        for (int q = info.Concentrator2.Count - 1; q >= 0; q--)
                            info.BornPoints2.Add(info.Concentrator2[q]);
                    }
                }
                else if (cmd == "LOCKPOINTS")
                {
                    ParsePointListLikeOriginal(t, info.LockPoints, false);
                }
                else if (cmd == "BUILDLOCKPOINTS")
                {
                    ParsePointListLikeOriginal(t, info.BuildLockPoints, false);
                }
                else if (cmd == "CHECKPOINTS")
                {
                    ParsePointListLikeOriginal(t, info.CheckPoints, false);
                }
                else if (cmd == "BUILDPOINTS")
                {
                    ParsePointListLikeOriginal(t, info.BuildPoints, false);
                }
                else if (cmd == "3DBARS" && t.Length >= 2)
                {
                    // COSSACKS2/NewMon.cpp stores five shorts per bar:
                    // XB, YB, L1, L2, height. Add3DBar later converts these
                    // exact MD values into a projectile collision volume.
                    info.Bars3D.Clear();
                    int declared = Mathf.Max(0, ToIntLikeOriginal(t[1]));
                    int available = Math.Min(declared, Math.Max(0, (t.Length - 2) / 5));
                    for (int bar = 0; bar < available; bar++)
                    {
                        int p = 2 + bar * 5;
                        info.Bars3D.Add(new C2Building3DBarLikeOriginal(
                            ToIntLikeOriginal(t[p]),
                            ToIntLikeOriginal(t[p + 1]),
                            ToIntLikeOriginal(t[p + 2]),
                            ToIntLikeOriginal(t[p + 3]),
                            ToIntLikeOriginal(t[p + 4])));
                    }
                }
                else if (cmd == "EXTRALOCK")
                {
                    // Original NewMon.cpp:
                    // EXTRALOCK expands LOCKPOINTS by one cell on the left/up/down/right-ish edge
                    // and nudges BUILDPOINTS away from the building center. Without this command
                    // the lock field has visible holes and workers/units can slip under sprites.
                    ApplyExtraLockToLockPointsLikeOriginal(info.LockPoints);
                    ApplyExtraLockToBuildPointsLikeOriginal(info.BuildPoints);
                }
                else if (cmd == "BUILDSTAGES" && t.Length >= 2)
                {
                    info.BuildStages = ToIntLikeOriginal(t[1]);
                }
                else if (cmd == "PIECE" && t.Length >= 2)
                {
                    info.PieceName = t[1].Trim().Trim('"');
                }
                else if (cmd == "DESTRUCT" && t.Length >= 2)
                {
                    info.DestructProbability = ToIntLikeOriginal(t[1]);
                    info.DestructWeapons.Clear();
                    int declared = t.Length >= 3 ? ToIntLikeOriginal(t[2]) : 0;
                    int max = declared > 0 ? Math.Min(t.Length, 3 + declared) : t.Length;
                    for (int w = 3; w < max; w++)
                    {
                        string weapon = (t[w] ?? string.Empty).Trim().Trim('"');
                        if (!string.IsNullOrEmpty(weapon))
                            info.DestructWeapons.Add(weapon);
                    }
                }
                else if (cmd == "LINESORT" && t.Length >= 2)
                {
                    string animName = NormalizeAnimationNameLikeOriginal(t[1]);
                    C2BuildingAnimationLikeOriginal anim = GetOrCreateBuildingAnimationLikeOriginal(info, animName, 1);
                    int expected = anim.Frames.Count > 0 ? anim.Frames.Count : 256;
                    anim.LineSort.Clear();
                    ParseLineSortTokensLikeOriginal(anim.LineSort, t, 2, expected);
                    PostProcessLineSortLikeOriginal(anim.LineSort);
                    if (string.Equals(animName, "#STANDLO", StringComparison.OrdinalIgnoreCase))
                    {
                        info.StandLoLineSort.Clear();
                        info.StandLoLineSort.AddRange(anim.LineSort);
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '#')
                {
                    ParseSharpAnimationLikeOriginal(info, cmdRaw, t);
                }
                else if (cmd.Length > 1 && cmd[0] == '@')
                {
                    ParseAtAnimationLikeOriginal(info, cmdRaw, t);
                }
            }

            C2BuildingAnimationLikeOriginal stand = FindBuildingAnimationLikeOriginal(info, "#STANDLO")
                                                    ?? FindBuildingAnimationLikeOriginal(info, "#STAND")
                                                    ?? FindBuildingAnimationLikeOriginal(info, "#STAND1")
                                                    ?? FindFirstBuildingAnimationLikeOriginal(info);
            if (stand != null)
            {
                info.StandLoFrames.Clear();
                info.StandLoFrames.AddRange(stand.Frames);
                info.StandLoLineSort.Clear();
                info.StandLoLineSort.AddRange(stand.LineSort);
            }
        }

        private static void ParseSharpAnimationLikeOriginal(C2BuildingMdInfoLikeOriginal info, string cmdRaw, string[] t)
        {
            if (t.Length < 3)
                return;

            int rotations = Math.Max(1, ToIntLikeOriginal(t[1]));
            int frames = Math.Max(0, ToIntLikeOriginal(t[2]));
            string animName = NormalizeAnimationNameLikeOriginal(cmdRaw);
            C2BuildingAnimationLikeOriginal anim = GetOrCreateBuildingAnimationLikeOriginal(info, animName, rotations);
            anim.Rotations = rotations;
            anim.Frames.Clear();
            for (int i = 0; i < frames; i++)
            {
                int p = 3 + i * 2;
                if (p + 1 >= t.Length)
                    break;
                anim.Frames.Add(new C2BuildingAnimFrameLikeOriginal(ToIntLikeOriginal(t[p]), ToIntLikeOriginal(t[p + 1])));
            }

            if (string.Equals(animName, "#WORK", StringComparison.OrdinalIgnoreCase))
            {
                info.WorkFrames.Clear();
                info.WorkFrames.AddRange(anim.Frames);
            }
        }

        private static void ParseAtAnimationLikeOriginal(C2BuildingMdInfoLikeOriginal info, string cmdRaw, string[] t)
        {
            if (t.Length < 5)
                return;

            int rotations = Math.Max(1, ToIntLikeOriginal(t[1]));
            int fileRef = ToIntLikeOriginal(t[2]);
            int startFrame = ToIntLikeOriginal(t[3]);
            int endFrame = ToIntLikeOriginal(t[4]);
            string animName = NormalizeAnimationNameLikeOriginal("#" + cmdRaw.Substring(1));
            C2BuildingAnimationLikeOriginal anim = GetOrCreateBuildingAnimationLikeOriginal(info, animName, rotations);
            anim.Rotations = rotations;
            anim.Frames.Clear();
            int step = startFrame > endFrame ? -1 : 1;
            for (int frame = startFrame; ; frame += step)
            {
                anim.Frames.Add(new C2BuildingAnimFrameLikeOriginal(fileRef, frame));
                if (frame == endFrame || anim.Frames.Count >= 4096)
                    break;
            }

            if (string.Equals(animName, "#WORK", StringComparison.OrdinalIgnoreCase))
            {
                info.WorkFrames.Clear();
                info.WorkFrames.AddRange(anim.Frames);
            }
        }

        private static C2BuildingAnimationLikeOriginal GetOrCreateBuildingAnimationLikeOriginal(C2BuildingMdInfoLikeOriginal info, string name, int rotations)
        {
            string key = NormalizeAnimationNameLikeOriginal(name);
            if (!info.Animations.TryGetValue(key, out C2BuildingAnimationLikeOriginal anim) || anim == null)
            {
                anim = new C2BuildingAnimationLikeOriginal { Name = key };
                info.Animations[key] = anim;
            }
            if (rotations > 0)
                anim.Rotations = rotations;
            return anim;
        }

        private static C2BuildingAnimationLikeOriginal FindBuildingAnimationLikeOriginal(C2BuildingMdInfoLikeOriginal info, string name)
        {
            if (info == null)
                return null;
            return info.Animations.TryGetValue(NormalizeAnimationNameLikeOriginal(name), out C2BuildingAnimationLikeOriginal anim) &&
                   anim != null &&
                   anim.Frames.Count > 0
                ? anim
                : null;
        }

        private static C2BuildingAnimationLikeOriginal FindFirstBuildingAnimationLikeOriginal(C2BuildingMdInfoLikeOriginal info)
        {
            foreach (KeyValuePair<string, C2BuildingAnimationLikeOriginal> kv in info.Animations)
            {
                C2BuildingAnimationLikeOriginal anim = kv.Value;
                if (anim == null || anim.Frames.Count == 0)
                    continue;
                string n = anim.Name ?? string.Empty;
                if (n.IndexOf("BUILD", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.IndexOf("DEATH", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.IndexOf("WORK", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                return anim;
            }
            return null;
        }

        private static C2BuildingAnimationLikeOriginal SelectBuildingAnimationForRecordLikeOriginal(C2BuildingMdInfoLikeOriginal info, C2Building3InuRecordLikeOriginal record)
        {
            if (record.Stage > 0x8000)
            {
                C2BuildingAnimationLikeOriginal build = FindBuildAnimationLikeOriginal(info, 0xFFFF - record.Stage);
                if (build != null)
                    return build;
            }

            return FindBuildingAnimationLikeOriginal(info, "#STANDLO")
                   ?? FindBuildingAnimationLikeOriginal(info, "#STAND")
                   ?? FindFirstBuildingAnimationLikeOriginal(info);
        }

        private static C2BuildingAnimationLikeOriginal FindBuildAnimationLikeOriginal(C2BuildingMdInfoLikeOriginal info, int stage)
        {
            int denom = info.BuildStages > 0 ? info.BuildStages : 64;
            int wanted = Mathf.Clamp((stage * 4) / Math.Max(1, denom), 0, 3);
            C2BuildingAnimationLikeOriginal first = null;
            C2BuildingAnimationLikeOriginal best = null;
            int bestDistance = int.MaxValue;

            foreach (KeyValuePair<string, C2BuildingAnimationLikeOriginal> kv in info.Animations)
            {
                C2BuildingAnimationLikeOriginal anim = kv.Value;
                if (anim == null || anim.Frames.Count == 0)
                    continue;
                if (!anim.Name.StartsWith("#BUILDLO", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (first == null)
                    first = anim;
                int suffix = AnimationSuffixLikeOriginal(anim.Name);
                if (suffix < 0)
                    suffix = 0;
                int distance = Math.Abs(suffix - wanted);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = anim;
                }
            }

            return best ?? first;
        }

        private bool TryLoadBuildingPartsLikeOriginal(
            C2BuildingMdInfoLikeOriginal md,
            C2Building3InuRecordLikeOriginal record,
            out List<C2BuildingLoadedPartLikeOriginal> parts,
            out string audit)
        {
            parts = new List<C2BuildingLoadedPartLikeOriginal>();
            audit = string.Empty;

            C2BuildingAnimationLikeOriginal anim = SelectBuildingAnimationForRecordLikeOriginal(md, record);
            if (anim == null || anim.Frames.Count == 0)
            {
                audit = "no_animation";
                return false;
            }

            var miss = new List<string>();
            for (int i = 0; i < anim.Frames.Count; i++)
            {
                C2BuildingAnimFrameLikeOriginal frame = anim.Frames[i];
                string pkg = PackageForFileRefLikeOriginal(md, frame.FileRef);
                Texture2D tex = TryLoadBuildingFrameTextureLikeOriginal(pkg, frame.SpriteId, out string source);
                if (tex == null)
                {
                    AddLimitedLikeOriginal(miss, pkg + "#" + frame.SpriteId.ToString(CultureInfo.InvariantCulture) + " " + source, 8);
                    continue;
                }

                RegisterBuildingTextureUseV226LikeOriginal(tex, pkg, frame.SpriteId, "static");

                var part = new C2BuildingLoadedPartLikeOriginal
                {
                    Texture = tex,
                    Frame = frame,
                    AnimationName = anim.Name
                };
                if (i < anim.LineSort.Count)
                {
                    part.HasLineSort = true;
                    part.LineSort = anim.LineSort[i];
                }
                parts.Add(part);
            }

            audit = "anim=" + anim.Name +
                    " loaded=" + parts.Count.ToString(CultureInfo.InvariantCulture) +
                    "/" + anim.Frames.Count.ToString(CultureInfo.InvariantCulture) +
                    " miss=" + string.Join(" || ", miss.ToArray());
            return parts.Count > 0;
        }

        private static void ResetBuildingTextureDiagnosticsV226LikeOriginal()
        {
            if (!C2BuildingsTextureMemoryAuditV226LikeOriginal)
                return;

            s_C2BuildingTextureRequestsV226LikeOriginal = 0;
            s_C2BuildingTextureCacheHitsV226LikeOriginal = 0;
            s_C2BuildingTextureCacheMissesV226LikeOriginal = 0;
            s_C2BuildingTextureCreatedV226LikeOriginal = 0;
            s_C2BuildingTextureStaticRefsV226LikeOriginal = 0;
            s_C2BuildingTextureWorkRefsV226LikeOriginal = 0;
            s_C2BuildingTextureRequestTopV226LikeOriginal.Clear();
            s_C2BuildingTextureDiagByInstanceV226LikeOriginal.Clear();
            s_C2BuildingTextureDiagByKeyV226LikeOriginal.Clear();
            s_C2BuildingAlphaBoundsByTextureIdV228LikeOriginal.Clear();
            s_C2BuildingTightCropCreatedV229LikeOriginal = 0;
            s_C2BuildingTightCropOriginalPixelsV229LikeOriginal = 0L;
            s_C2BuildingTightCropKeptPixelsV229LikeOriginal = 0L;
            s_C2BuildingPreparedSpriteDiskHitsV232LikeOriginal = 0;
            s_C2BuildingPreparedSpriteDiskMissesV232LikeOriginal = 0;
            s_C2BuildingPreparedSpriteDiskWritesV232LikeOriginal = 0;
            s_C2BuildingPreparedSpriteDiskWriteFailsV232LikeOriginal = 0;
            s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal = 0;
        }

        private static void NoteBuildingTextureRequestV226LikeOriginal(
            string package,
            int frame,
            string path,
            string key,
            bool cacheHit,
            Texture2D texture,
            string source)
        {
            if (!C2BuildingsTextureMemoryAuditV226LikeOriginal)
                return;

            s_C2BuildingTextureRequestsV226LikeOriginal++;
            if (cacheHit) s_C2BuildingTextureCacheHitsV226LikeOriginal++;
            else s_C2BuildingTextureCacheMissesV226LikeOriginal++;

            string req = (package ?? string.Empty) + "#" + frame.ToString(CultureInfo.InvariantCulture);
            IncrementStringCounterV226LikeOriginal(s_C2BuildingTextureRequestTopV226LikeOriginal, req, 1);

            if (texture == null)
                return;

            C2BuildingTextureDiagV226LikeOriginal d = GetOrCreateBuildingTextureDiagV226LikeOriginal(texture, package, frame, path, key);
            d.Requests++;
            if (!string.IsNullOrEmpty(source)) d.Source = source;
        }

        private static void RegisterCreatedBuildingTextureV226LikeOriginal(
            string key,
            string package,
            string path,
            int frame,
            Texture2D texture,
            string source)
        {
            if (!C2BuildingsTextureMemoryAuditV226LikeOriginal || texture == null)
                return;

            s_C2BuildingTextureCreatedV226LikeOriginal++;
            C2BuildingTextureDiagV226LikeOriginal d = GetOrCreateBuildingTextureDiagV226LikeOriginal(texture, package, frame, path, key);
            d.CreatedThisBuild = true;
            if (C2BuildingsAlphaBoundsAuditV228LikeOriginal &&
                s_C2BuildingAlphaBoundsByTextureIdV228LikeOriginal.TryGetValue(texture.GetEntityId(), out C2BuildingAlphaBoundsDiagV228LikeOriginal ab) &&
                ab != null)
                d.AlphaBoundsV228 = ab;
            if (!string.IsNullOrEmpty(source)) d.Source = source;
            if (!string.IsNullOrEmpty(key)) s_C2BuildingTextureDiagByKeyV226LikeOriginal[key] = d;
        }

        private static void RegisterBuildingTextureUseV226LikeOriginal(Texture2D texture, string package, int frame, string kind)
        {
            if (!C2BuildingsTextureMemoryAuditV226LikeOriginal || texture == null)
                return;

            C2BuildingTextureDiagV226LikeOriginal d = GetOrCreateBuildingTextureDiagV226LikeOriginal(texture, package, frame, string.Empty, string.Empty);
            if (string.Equals(kind, "work", StringComparison.OrdinalIgnoreCase))
            {
                d.WorkUses++;
                s_C2BuildingTextureWorkRefsV226LikeOriginal++;
            }
            else
            {
                d.StaticUses++;
                s_C2BuildingTextureStaticRefsV226LikeOriginal++;
            }
        }

        private static C2BuildingTextureDiagV226LikeOriginal GetOrCreateBuildingTextureDiagV226LikeOriginal(
            Texture2D texture,
            string package,
            int frame,
            string path,
            string key)
        {
            EntityId id = texture.GetEntityId();
            if (!s_C2BuildingTextureDiagByInstanceV226LikeOriginal.TryGetValue(id, out C2BuildingTextureDiagV226LikeOriginal d) || d == null)
            {
                d = new C2BuildingTextureDiagV226LikeOriginal();
                d.Texture = texture;
                s_C2BuildingTextureDiagByInstanceV226LikeOriginal[id] = d;
            }

            if (!string.IsNullOrEmpty(package)) d.Package = package;
            else if (string.IsNullOrEmpty(d.Package)) d.Package = GuessBuildingTexturePackageV226LikeOriginal(texture != null ? texture.name : string.Empty);
            if (!string.IsNullOrEmpty(path)) d.Path = path;
            if (!string.IsNullOrEmpty(key))
            {
                d.Key = key;
                s_C2BuildingTextureDiagByKeyV226LikeOriginal[key] = d;
            }
            if (frame >= 0) d.Frame = frame;
            return d;
        }

        private static string GuessBuildingTexturePackageV226LikeOriginal(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
                return "<unknown>";
            const string prefix = "C2_BLD_G16_NATIVE_V208_";
            int p = textureName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (p >= 0)
            {
                int start = p + prefix.Length;
                int exact = textureName.IndexOf("_exact_", start, StringComparison.OrdinalIgnoreCase);
                if (exact > start)
                    return textureName.Substring(start, exact - start).Replace('_', '\\');
            }
            return "<name:" + textureName + ">";
        }

        private static void IncrementStringCounterV226LikeOriginal(Dictionary<string, int> dict, string key, int add)
        {
            if (dict == null || string.IsNullOrEmpty(key))
                return;
            if (dict.TryGetValue(key, out int old)) dict[key] = old + add;
            else dict[key] = add;
        }

        private sealed class C2BuildingTextureGroupDiagV226LikeOriginal
        {
            public string Package = string.Empty;
            public int TextureCount;
            public int Requests;
            public int Uses;
            public int StaticUses;
            public int WorkUses;
            public int ReadableCount;
            public long RuntimeBytes;
            public long ReadableRuntimeBytes;
            public readonly List<C2BuildingTextureDiagV226LikeOriginal> Textures = new List<C2BuildingTextureDiagV226LikeOriginal>();
        }

        private static long RuntimeMemoryBytesV226LikeOriginal(UnityEngine.Object obj)
        {
            if (obj == null)
                return 0L;
            try { return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(obj); }
            catch { return 0L; }
        }

        private static bool IsTextureReadableV226LikeOriginal(Texture2D tex)
        {
            if (tex == null)
                return false;
            try { return tex.isReadable; }
            catch { return false; }
        }

        private static string FormatMbV226LikeOriginal(long bytes)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture);
        }

        private static void LogBuildingTextureDiagnosticsV226LikeOriginal(int buildingRecords, int drawn, int visualMissing)
        {
            if (!C2BuildingsTextureMemoryAuditV226LikeOriginal)
                return;

            var uniqueCacheIds = new HashSet<EntityId>();
            long cacheRuntimeBytes = 0L;
            int cacheReadable = 0;
            long cacheReadableBytes = 0L;
            foreach (KeyValuePair<string, Texture2D> kv in s_C2BuildingTextureCacheLikeOriginal)
            {
                Texture2D tex = kv.Value;
                if (tex == null)
                    continue;
                EntityId id = tex.GetEntityId();
                if (!uniqueCacheIds.Add(id))
                    continue;
                long b = RuntimeMemoryBytesV226LikeOriginal(tex);
                cacheRuntimeBytes += b;
                if (IsTextureReadableV226LikeOriginal(tex))
                {
                    cacheReadable++;
                    cacheReadableBytes += b;
                }
            }

            var groups = new Dictionary<string, C2BuildingTextureGroupDiagV226LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            long usedRuntimeBytes = 0L;
            int usedReadable = 0;
            long usedReadableBytes = 0L;
            int usedTextures = 0;
            int createdThisBuild = 0;

            foreach (KeyValuePair<EntityId, C2BuildingTextureDiagV226LikeOriginal> kv in s_C2BuildingTextureDiagByInstanceV226LikeOriginal)
            {
                C2BuildingTextureDiagV226LikeOriginal d = kv.Value;
                if (d == null || d.Texture == null)
                    continue;
                int uses = d.StaticUses + d.WorkUses;
                if (uses <= 0)
                    continue;
                usedTextures++;
                if (d.CreatedThisBuild) createdThisBuild++;
                long bytes = RuntimeMemoryBytesV226LikeOriginal(d.Texture);
                usedRuntimeBytes += bytes;
                bool readable = IsTextureReadableV226LikeOriginal(d.Texture);
                if (readable)
                {
                    usedReadable++;
                    usedReadableBytes += bytes;
                }

                string pkg = string.IsNullOrEmpty(d.Package) ? GuessBuildingTexturePackageV226LikeOriginal(d.Texture.name) : d.Package;
                if (!groups.TryGetValue(pkg, out C2BuildingTextureGroupDiagV226LikeOriginal g) || g == null)
                {
                    g = new C2BuildingTextureGroupDiagV226LikeOriginal();
                    g.Package = pkg;
                    groups[pkg] = g;
                }
                g.TextureCount++;
                g.Requests += d.Requests;
                g.Uses += uses;
                g.StaticUses += d.StaticUses;
                g.WorkUses += d.WorkUses;
                g.RuntimeBytes += bytes;
                if (readable)
                {
                    g.ReadableCount++;
                    g.ReadableRuntimeBytes += bytes;
                }
                g.Textures.Add(d);
            }

            int cacheEntries = s_C2BuildingTextureCacheLikeOriginal.Count;
            int uniqueCacheTextures = uniqueCacheIds.Count;
            float hitRate = s_C2BuildingTextureRequestsV226LikeOriginal > 0
                ? (100.0f * s_C2BuildingTextureCacheHitsV226LikeOriginal / s_C2BuildingTextureRequestsV226LikeOriginal)
                : 0f;
            Debug.Log("[C2:BUILDINGS TEXTURE CACHE V226] buildingRecords=" + buildingRecords.ToString(CultureInfo.InvariantCulture) +
                      " drawn=" + drawn.ToString(CultureInfo.InvariantCulture) +
                      " visualMissing=" + visualMissing.ToString(CultureInfo.InvariantCulture) +
                      " requests=" + s_C2BuildingTextureRequestsV226LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " cacheHits=" + s_C2BuildingTextureCacheHitsV226LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " cacheMisses=" + s_C2BuildingTextureCacheMissesV226LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " hitRate=" + hitRate.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                      " createdThisBuild=" + s_C2BuildingTextureCreatedV226LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " cacheEntries=" + cacheEntries.ToString(CultureInfo.InvariantCulture) +
                      " uniqueCacheTextures=" + uniqueCacheTextures.ToString(CultureInfo.InvariantCulture) +
                      " usedTextures=" + usedTextures.ToString(CultureInfo.InvariantCulture) +
                      " staticRefs=" + s_C2BuildingTextureStaticRefsV226LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " workRefs=" + s_C2BuildingTextureWorkRefsV226LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " cacheRuntimeMB=" + FormatMbV226LikeOriginal(cacheRuntimeBytes) +
                      " usedRuntimeMB=" + FormatMbV226LikeOriginal(usedRuntimeBytes) +
                      " readableUsed=" + usedReadable.ToString(CultureInfo.InvariantCulture) + "/" + usedTextures.ToString(CultureInfo.InvariantCulture) +
                      " readableUsedMB=" + FormatMbV226LikeOriginal(usedReadableBytes) +
                      " readableCache=" + cacheReadable.ToString(CultureInfo.InvariantCulture) + "/" + uniqueCacheTextures.ToString(CultureInfo.InvariantCulture) +
                      " readableCacheMB=" + FormatMbV226LikeOriginal(cacheReadableBytes) +
                      " diskCacheV232=" + (C2BuildingsPreparedSpriteDiskCacheV232LikeOriginal ? "1" : "0") +
                      " diskHits=" + s_C2BuildingPreparedSpriteDiskHitsV232LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " diskMisses=" + s_C2BuildingPreparedSpriteDiskMissesV232LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " diskWrites=" + s_C2BuildingPreparedSpriteDiskWritesV232LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " diskLoadFails=" + s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " diskWriteFails=" + s_C2BuildingPreparedSpriteDiskWriteFailsV232LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " diskFolder='C2Cache/" + C2BuildingsPreparedSpriteDiskCacheFolderV232LikeOriginal + "'");

            var groupList = new List<C2BuildingTextureGroupDiagV226LikeOriginal>(groups.Values);
            groupList.Sort((a, b) => b.RuntimeBytes.CompareTo(a.RuntimeBytes));
            var groupParts = new List<string>();
            int groupLimit = Math.Min(C2BuildingsTextureMemoryAuditTopV226LikeOriginal, groupList.Count);
            for (int i = 0; i < groupLimit; i++)
            {
                C2BuildingTextureGroupDiagV226LikeOriginal g = groupList[i];
                groupParts.Add("#" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                               " pkg='" + g.Package + "'" +
                               " tex=" + g.TextureCount.ToString(CultureInfo.InvariantCulture) +
                               " MB=" + FormatMbV226LikeOriginal(g.RuntimeBytes) +
                               " uses=" + g.Uses.ToString(CultureInfo.InvariantCulture) +
                               " static=" + g.StaticUses.ToString(CultureInfo.InvariantCulture) +
                               " work=" + g.WorkUses.ToString(CultureInfo.InvariantCulture) +
                               " readable=" + g.ReadableCount.ToString(CultureInfo.InvariantCulture) +
                               " readableMB=" + FormatMbV226LikeOriginal(g.ReadableRuntimeBytes));
            }
            Debug.Log("[C2:BUILDINGS TEXTURE GROUPS V226] " + string.Join(" | ", groupParts.ToArray()));

            var texList = new List<C2BuildingTextureDiagV226LikeOriginal>();
            foreach (KeyValuePair<EntityId, C2BuildingTextureDiagV226LikeOriginal> kv in s_C2BuildingTextureDiagByInstanceV226LikeOriginal)
            {
                C2BuildingTextureDiagV226LikeOriginal d = kv.Value;
                if (d != null && d.Texture != null && (d.StaticUses + d.WorkUses) > 0)
                    texList.Add(d);
            }
            texList.Sort((a, b) => RuntimeMemoryBytesV226LikeOriginal(b.Texture).CompareTo(RuntimeMemoryBytesV226LikeOriginal(a.Texture)));
            var topTex = new List<string>();
            int texLimit = Math.Min(C2BuildingsTextureMemoryAuditTopV226LikeOriginal, texList.Count);
            for (int i = 0; i < texLimit; i++)
            {
                C2BuildingTextureDiagV226LikeOriginal d = texList[i];
                Texture2D t = d.Texture;
                topTex.Add("#" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                           " name='" + (t != null ? t.name : "<null>") + "'" +
                           " pkg='" + (string.IsNullOrEmpty(d.Package) ? GuessBuildingTexturePackageV226LikeOriginal(t != null ? t.name : string.Empty) : d.Package) + "'" +
                           " frame=" + d.Frame.ToString(CultureInfo.InvariantCulture) +
                           " size=" + (t != null ? (t.width.ToString(CultureInfo.InvariantCulture) + "x" + t.height.ToString(CultureInfo.InvariantCulture)) : "0x0") +
                           " fmt=" + (t != null ? t.format.ToString() : "<null>") +
                           " readable=" + (t != null && IsTextureReadableV226LikeOriginal(t) ? "1" : "0") +
                           " MB=" + FormatMbV226LikeOriginal(t != null ? RuntimeMemoryBytesV226LikeOriginal(t) : 0L) +
                           " uses=" + (d.StaticUses + d.WorkUses).ToString(CultureInfo.InvariantCulture) +
                           " static=" + d.StaticUses.ToString(CultureInfo.InvariantCulture) +
                           " work=" + d.WorkUses.ToString(CultureInfo.InvariantCulture));
            }
            Debug.Log("[C2:BUILDINGS TEXTURE TOP V226] " + string.Join(" | ", topTex.ToArray()));

            LogBuildingAlphaBoundsDiagnosticsV228LikeOriginal(texList);

            var requestList = new List<KeyValuePair<string, int>>(s_C2BuildingTextureRequestTopV226LikeOriginal);
            requestList.Sort((a, b) => b.Value.CompareTo(a.Value));
            var requestParts = new List<string>();
            int reqLimit = Math.Min(C2BuildingsTextureMemoryAuditTopV226LikeOriginal, requestList.Count);
            for (int i = 0; i < reqLimit; i++)
            {
                requestParts.Add("#" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                                 " '" + requestList[i].Key + "'=" + requestList[i].Value.ToString(CultureInfo.InvariantCulture));
            }
            Debug.Log("[C2:BUILDINGS TEXTURE REQUEST TOP V226] " + string.Join(" | ", requestParts.ToArray()));
        }


        private sealed class C2BuildingAlphaBoundsGroupDiagV228LikeOriginal
        {
            public string Package = string.Empty;
            public int TextureCount;
            public int Uses;
            public long RuntimeBytes;
            public double EstimatedTrimmedBytes;
            public long OriginalPixels;
            public long CropPixels;
            public long VisiblePixels;
            public double MinCropRatio = 1.0;
            public double MaxCropRatio = 0.0;
        }

        private static void LogBuildingAlphaBoundsDiagnosticsV228LikeOriginal(List<C2BuildingTextureDiagV226LikeOriginal> textures)
        {
            if (!C2BuildingsAlphaBoundsAuditV228LikeOriginal || textures == null || textures.Count == 0)
                return;

            long totalRuntimeBytes = 0L;
            double totalTrimmedBytes = 0.0;
            long totalOriginalPixels = 0L;
            long totalCropPixels = 0L;
            long totalVisiblePixels = 0L;
            int withBounds = 0;
            int noVisible = 0;
            int tightCroppedCount = 0;

            var groups = new Dictionary<string, C2BuildingAlphaBoundsGroupDiagV228LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            var top = new List<C2BuildingTextureDiagV226LikeOriginal>();

            for (int i = 0; i < textures.Count; i++)
            {
                C2BuildingTextureDiagV226LikeOriginal d = textures[i];
                if (d == null || d.Texture == null || d.AlphaBoundsV228 == null)
                    continue;

                C2BuildingAlphaBoundsDiagV228LikeOriginal ab = d.AlphaBoundsV228;
                long bytes = RuntimeMemoryBytesV226LikeOriginal(d.Texture);
                bool alreadyTightCropped = ab.TightCroppedV229;
                double cropRatio = alreadyTightCropped ? 1.0 : Math.Max(0.0, Math.Min(1.0, ab.CropRatio));
                double trimmedBytes = bytes * cropRatio;

                withBounds++;
                if (alreadyTightCropped)
                    tightCroppedCount++;
                if (!ab.HasVisiblePixels)
                    noVisible++;

                totalRuntimeBytes += bytes;
                totalTrimmedBytes += trimmedBytes;
                totalOriginalPixels += ab.OriginalPixels;
                totalCropPixels += ab.CropPixels;
                totalVisiblePixels += ab.VisiblePixels;

                string pkg = string.IsNullOrEmpty(d.Package) ? GuessBuildingTexturePackageV226LikeOriginal(d.Texture.name) : d.Package;
                if (!groups.TryGetValue(pkg, out C2BuildingAlphaBoundsGroupDiagV228LikeOriginal g) || g == null)
                {
                    g = new C2BuildingAlphaBoundsGroupDiagV228LikeOriginal();
                    g.Package = pkg;
                    groups[pkg] = g;
                }

                g.TextureCount++;
                g.Uses += d.StaticUses + d.WorkUses;
                g.RuntimeBytes += bytes;
                g.EstimatedTrimmedBytes += trimmedBytes;
                g.OriginalPixels += ab.OriginalPixels;
                g.CropPixels += ab.CropPixels;
                g.VisiblePixels += ab.VisiblePixels;
                g.MinCropRatio = Math.Min(g.MinCropRatio, cropRatio);
                g.MaxCropRatio = Math.Max(g.MaxCropRatio, cropRatio);
                top.Add(d);
            }

            if (withBounds == 0)
            {
                Debug.Log("[C2:BUILDINGS ALPHA BOUNDS V228] no alpha-bound data collected");
                return;
            }

            double savingBytes = Math.Max(0.0, totalRuntimeBytes - totalTrimmedBytes);
            double savingPct = totalRuntimeBytes > 0L ? savingBytes * 100.0 / totalRuntimeBytes : 0.0;
            double cropPixelPct = totalOriginalPixels > 0L ? totalCropPixels * 100.0 / totalOriginalPixels : 100.0;
            double visiblePixelPct = totalOriginalPixels > 0L ? totalVisiblePixels * 100.0 / totalOriginalPixels : 0.0;

            Debug.Log("[C2:BUILDINGS ALPHA BOUNDS V228] textures=" + withBounds.ToString(CultureInfo.InvariantCulture) +
                      " currentMB=" + FormatMbV226LikeOriginal(totalRuntimeBytes) +
                      " estimatedTightCropMB=" + FormatMbDoubleV228LikeOriginal(totalTrimmedBytes) +
                      " estimatedSavingMB=" + FormatMbDoubleV228LikeOriginal(savingBytes) +
                      " estimatedSavingPct=" + savingPct.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                      " originalPixels=" + totalOriginalPixels.ToString(CultureInfo.InvariantCulture) +
                      " cropPixels=" + totalCropPixels.ToString(CultureInfo.InvariantCulture) +
                      " cropPixelsPct=" + cropPixelPct.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                      " visiblePixels=" + totalVisiblePixels.ToString(CultureInfo.InvariantCulture) +
                      " visiblePixelsPct=" + visiblePixelPct.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                      " noVisible=" + noVisible.ToString(CultureInfo.InvariantCulture) +
                      " tightCroppedV229=" + tightCroppedCount.ToString(CultureInfo.InvariantCulture) + "/" + withBounds.ToString(CultureInfo.InvariantCulture) +
                      " tightCropCreatedV229=" + s_C2BuildingTightCropCreatedV229LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " tightCropKeptPixelsPctV229=" + (s_C2BuildingTightCropOriginalPixelsV229LikeOriginal > 0L ? (s_C2BuildingTightCropKeptPixelsV229LikeOriginal * 100.0 / s_C2BuildingTightCropOriginalPixelsV229LikeOriginal).ToString("F1", CultureInfo.InvariantCulture) : "100.0") + "%" +
                      " alphaThreshold=" + C2BuildingsAlphaBoundsThresholdV228LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " rule=tight_crop_enabled_v229_visual_same_offset");

            var groupList = new List<C2BuildingAlphaBoundsGroupDiagV228LikeOriginal>(groups.Values);
            groupList.Sort((a, b) =>
            {
                double asave = a.RuntimeBytes - a.EstimatedTrimmedBytes;
                double bsave = b.RuntimeBytes - b.EstimatedTrimmedBytes;
                return bsave.CompareTo(asave);
            });

            var groupParts = new List<string>();
            int groupLimit = Math.Min(C2BuildingsAlphaBoundsAuditTopV228LikeOriginal, groupList.Count);
            for (int i = 0; i < groupLimit; i++)
            {
                C2BuildingAlphaBoundsGroupDiagV228LikeOriginal g = groupList[i];
                double groupSaving = Math.Max(0.0, g.RuntimeBytes - g.EstimatedTrimmedBytes);
                double groupSavingPct = g.RuntimeBytes > 0L ? groupSaving * 100.0 / g.RuntimeBytes : 0.0;
                double groupCropPct = g.OriginalPixels > 0L ? g.CropPixels * 100.0 / g.OriginalPixels : 100.0;
                double groupVisiblePct = g.OriginalPixels > 0L ? g.VisiblePixels * 100.0 / g.OriginalPixels : 0.0;
                groupParts.Add("#" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                               " pkg='" + g.Package + "'" +
                               " tex=" + g.TextureCount.ToString(CultureInfo.InvariantCulture) +
                               " uses=" + g.Uses.ToString(CultureInfo.InvariantCulture) +
                               " currentMB=" + FormatMbV226LikeOriginal(g.RuntimeBytes) +
                               " cropMB=" + FormatMbDoubleV228LikeOriginal(g.EstimatedTrimmedBytes) +
                               " saveMB=" + FormatMbDoubleV228LikeOriginal(groupSaving) +
                               " savePct=" + groupSavingPct.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                               " cropPixelsPct=" + groupCropPct.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                               " visiblePixelsPct=" + groupVisiblePct.ToString("F1", CultureInfo.InvariantCulture) + "%" +
                               " minCrop=" + (g.MinCropRatio * 100.0).ToString("F1", CultureInfo.InvariantCulture) + "%" +
                               " maxCrop=" + (g.MaxCropRatio * 100.0).ToString("F1", CultureInfo.InvariantCulture) + "%");
            }
            Debug.Log("[C2:BUILDINGS ALPHA GROUPS V228] " + string.Join(" | ", groupParts.ToArray()));

            top.Sort((a, b) =>
            {
                double ar = a.AlphaBoundsV228 != null ? (a.AlphaBoundsV228.TightCroppedV229 ? 1.0 : Math.Max(0.0, Math.Min(1.0, a.AlphaBoundsV228.CropRatio))) : 1.0;
                double br = b.AlphaBoundsV228 != null ? (b.AlphaBoundsV228.TightCroppedV229 ? 1.0 : Math.Max(0.0, Math.Min(1.0, b.AlphaBoundsV228.CropRatio))) : 1.0;
                double asave = RuntimeMemoryBytesV226LikeOriginal(a.Texture) * (1.0 - ar);
                double bsave = RuntimeMemoryBytesV226LikeOriginal(b.Texture) * (1.0 - br);
                return bsave.CompareTo(asave);
            });

            var topParts = new List<string>();
            int topLimit = Math.Min(C2BuildingsAlphaBoundsAuditTopV228LikeOriginal, top.Count);
            for (int i = 0; i < topLimit; i++)
            {
                C2BuildingTextureDiagV226LikeOriginal d = top[i];
                Texture2D t = d.Texture;
                C2BuildingAlphaBoundsDiagV228LikeOriginal ab = d.AlphaBoundsV228;
                long bytes = RuntimeMemoryBytesV226LikeOriginal(t);
                double cropRatio = ab.TightCroppedV229 ? 1.0 : Math.Max(0.0, Math.Min(1.0, ab.CropRatio));
                double cropBytes = bytes * cropRatio;
                double saveBytes = Math.Max(0.0, bytes - cropBytes);
                topParts.Add("#" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                             " name='" + (t != null ? t.name : "<null>") + "'" +
                             " pkg='" + (string.IsNullOrEmpty(d.Package) ? (t != null ? GuessBuildingTexturePackageV226LikeOriginal(t.name) : "<unknown>") : d.Package) + "'" +
                             " frame=" + d.Frame.ToString(CultureInfo.InvariantCulture) +
                             " size=" + ab.OriginalWidth.ToString(CultureInfo.InvariantCulture) + "x" + ab.OriginalHeight.ToString(CultureInfo.InvariantCulture) +
                             " texSize=" + (t != null ? t.width.ToString(CultureInfo.InvariantCulture) + "x" + t.height.ToString(CultureInfo.InvariantCulture) : "<null>") +
                             " crop=" + ab.CropWidth.ToString(CultureInfo.InvariantCulture) + "x" + ab.CropHeight.ToString(CultureInfo.InvariantCulture) +
                             " bbox=(" + ab.MinX.ToString(CultureInfo.InvariantCulture) + "," + ab.MinY.ToString(CultureInfo.InvariantCulture) + ")-(" + ab.MaxX.ToString(CultureInfo.InvariantCulture) + "," + ab.MaxY.ToString(CultureInfo.InvariantCulture) + ")" +
                             " cropPct=" + (cropRatio * 100.0).ToString("F1", CultureInfo.InvariantCulture) + "%" +
                             " currentMB=" + FormatMbV226LikeOriginal(bytes) +
                             " cropMB=" + FormatMbDoubleV228LikeOriginal(cropBytes) +
                             " saveMB=" + FormatMbDoubleV228LikeOriginal(saveBytes) +
                             " uses=" + (d.StaticUses + d.WorkUses).ToString(CultureInfo.InvariantCulture));
            }
            Debug.Log("[C2:BUILDINGS ALPHA TOP V228] " + string.Join(" | ", topParts.ToArray()));
        }

        private static string FormatMbDoubleV228LikeOriginal(double bytes)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture);
        }

        private static C2BuildingAlphaBoundsDiagV228LikeOriginal AnalyzeBuildingAlphaBoundsV228LikeOriginal(byte[] rgba, int width, int height)
        {
            var result = new C2BuildingAlphaBoundsDiagV228LikeOriginal();
            result.OriginalWidth = width;
            result.OriginalHeight = height;
            result.OriginalPixels = Math.Max(0, width) * (long)Math.Max(0, height);

            if (rgba == null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
            {
                result.MinX = 0;
                result.MinY = 0;
                result.MaxX = Math.Max(0, width - 1);
                result.MaxY = Math.Max(0, height - 1);
                result.CropWidth = Math.Max(0, width);
                result.CropHeight = Math.Max(0, height);
                result.CropPixels = result.OriginalPixels;
                return result;
            }

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            long visible = 0L;
            byte threshold = C2BuildingsAlphaBoundsThresholdV228LikeOriginal;

            for (int y = 0; y < height; y++)
            {
                int row = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    byte a = rgba[row + x * 4 + 3];
                    if (a > threshold)
                    {
                        visible++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            result.VisiblePixels = visible;
            result.HasVisiblePixels = visible > 0L;
            if (!result.HasVisiblePixels)
            {
                result.MinX = 0;
                result.MinY = 0;
                result.MaxX = Math.Max(0, width - 1);
                result.MaxY = Math.Max(0, height - 1);
                result.CropWidth = Math.Max(0, width);
                result.CropHeight = Math.Max(0, height);
                result.CropPixels = result.OriginalPixels;
                return result;
            }

            result.MinX = minX;
            result.MinY = minY;
            result.MaxX = maxX;
            result.MaxY = maxY;
            result.CropWidth = Math.Max(1, maxX - minX + 1);
            result.CropHeight = Math.Max(1, maxY - minY + 1);
            result.CropPixels = result.CropWidth * (long)result.CropHeight;
            return result;
        }

        private static bool ShouldTightCropBuildingAlphaV229LikeOriginal(C2BuildingAlphaBoundsDiagV228LikeOriginal ab)
        {
            if (!C2BuildingsTightCropAlphaV229LikeOriginal || ab == null || !ab.HasVisiblePixels)
                return false;

            if (ab.CropWidth <= 0 || ab.CropHeight <= 0 || ab.OriginalWidth <= 0 || ab.OriginalHeight <= 0)
                return false;

            return ab.CropWidth < ab.OriginalWidth || ab.CropHeight < ab.OriginalHeight;
        }

        private static byte[] CropBuildingRgbaToAlphaBoundsV229LikeOriginal(byte[] rgba, int width, int height, C2BuildingAlphaBoundsDiagV228LikeOriginal ab)
        {
            if (rgba == null || ab == null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
                return rgba;

            int cropW = Mathf.Clamp(ab.CropWidth, 1, width);
            int cropH = Mathf.Clamp(ab.CropHeight, 1, height);
            int minX = Mathf.Clamp(ab.MinX, 0, Math.Max(0, width - 1));
            int minY = Mathf.Clamp(ab.MinY, 0, Math.Max(0, height - 1));

            if (cropW == width && cropH == height && minX == 0 && minY == 0)
                return rgba;

            byte[] cropped = new byte[cropW * cropH * 4];
            for (int y = 0; y < cropH; y++)
            {
                int srcY = minY + y;
                if (srcY < 0 || srcY >= height)
                    continue;

                int src = (srcY * width + minX) * 4;
                int dst = y * cropW * 4;
                int count = Math.Min(cropW, Math.Max(0, width - minX)) * 4;
                if (count > 0 && src >= 0 && src + count <= rgba.Length && dst + count <= cropped.Length)
                    Buffer.BlockCopy(rgba, src, cropped, dst, count);
            }

            return cropped;
        }

        private static Texture2D TryLoadBuildingFrameTextureLikeOriginal(string package, int frame, out string source)
        {
            source = string.Empty;
            string path = FindBuildingVisualPathLikeOriginal(package);
            if (string.IsNullOrEmpty(path))
            {
                source = "visual_not_found:" + (package ?? string.Empty);
                NoteBuildingTextureRequestV226LikeOriginal(package, frame, string.Empty, string.Empty, false, null, "visual_not_found");
                return null;
            }

            int decodeFrame = frame;
            if (TryGetBuildingG16NativeFrameDecodeIndexLikeOriginal(path, frame, out int nativeChunks, out int nativeSprites, out int nativeDecodeFrame, out string nativeAudit))
            {
                if (frame < 0 || frame >= nativeSprites || nativeChunks <= 0)
                {
                    source = "empty_g16_native_exact_frame_skipped " + nativeAudit;
                    NoteBuildingTextureRequestV226LikeOriginal(package, frame, path, path + "#exact=" + frame.ToString(CultureInfo.InvariantCulture), false, null, "empty_native_frame");
                    return null;
                }

                decodeFrame = nativeDecodeFrame;
            }

            string key = path + "#exact=" + frame.ToString(CultureInfo.InvariantCulture);
            if (s_C2BuildingTextureCacheLikeOriginal.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                source = "cache:" + key;
                NoteBuildingTextureRequestV226LikeOriginal(package, frame, path, key, true, cached, source);
                return cached;
            }

            if (TryLoadPreparedBuildingSpriteDiskCacheV232LikeOriginal(path, frame, package, key, out Texture2D diskTex, out string diskSource))
            {
                source = diskSource;
                s_C2BuildingTextureCacheLikeOriginal[key] = diskTex;
                NoteBuildingTextureRequestV226LikeOriginal(package, frame, path, key, false, diskTex, source);
                RegisterCreatedBuildingTextureV226LikeOriginal(key, package, path, frame, diskTex, source);
                return diskTex;
            }

            NoteBuildingTextureRequestV226LikeOriginal(package, frame, path, key, false, null, "cache_miss disk_miss");
            Texture2D tex = TryLoadBuildingGpFrameViaMelinojaV207LikeOriginal(path, frame, decodeFrame, package, out string decodeSource);
            if (tex != null)
                tex = PrepareBuildingLoadedTextureV205LikeOriginal(tex, path, frame, package, key);

            source = "exact_frame=" + frame.ToString(CultureInfo.InvariantCulture) +
                     " decode_frame=" + decodeFrame.ToString(CultureInfo.InvariantCulture) +
                     " " + (nativeAudit ?? string.Empty) + " " + (decodeSource ?? string.Empty) +
                     (tex != null ? " layerCompositeV205=1 srgb=1" : string.Empty);
            if (tex != null)
            {
                s_C2BuildingTextureCacheLikeOriginal[key] = tex;
                RegisterCreatedBuildingTextureV226LikeOriginal(key, package, path, frame, tex, source);
            }
            return tex;
        }

        private static Texture2D TryLoadBuildingGpFrameViaMelinojaV207LikeOriginal(
            string abs,
            int exactFrameIndex,
            int compactFrameIndex,
            string logicalPackage,
            out string source)
        {
            source = string.Empty;

            // Fast path: existing wall/G16 bridge by absolute file path.
            Texture2D tex = TryLoadG16FrameViaMelinojaV42LikeOriginal(abs, compactFrameIndex, out string baseSource);
            if (tex != null)
            {
                source = "V207_ABS_OK compactFrame=" + compactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                         " base=[" + (baseSource ?? string.Empty) + "]";
                return tex;
            }

            // V208: If Melinoja sees the GU16 table but LoadG16ToMemory returns decoded 0 frames
            // (FrnMel mills from Data\\Cash do this), fall back to a direct native GU16/GN16 exact-frame decoder.
            // This is not an alias problem: the frame table is valid, chunks are non-zero, but the bridge decode path drops the package.
            Texture2D nativeTex = TryLoadG16FrameNativeExactV208LikeOriginal(abs, exactFrameIndex, out string nativeSource);
            if (nativeTex != null)
            {
                source = "V208_NATIVE_OK exactFrame=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                         " base=[" + (baseSource ?? string.Empty) + "] native=[" + (nativeSource ?? string.Empty) + "]";
                return nativeTex;
            }

            Type bridgeType = ResolveMelinojaBridgeTypeV2LikeOriginal();
            if (bridgeType == null)
            {
                source = "V207 bridge type not found base=[" + (baseSource ?? string.Empty) + "] native=[" + (nativeSource ?? string.Empty) + "]";
                return null;
            }

            List<string> keys = BuildBuildingGpAliasKeysV207LikeOriginal(abs, logicalPackage);
            var audits = new List<string>();
            for (int i = 0; i < keys.Count; i++)
            {
                Texture2D viaKey;
                string keyAudit;
                if (TryLoadBuildingBridgeKeyFrameV207LikeOriginal(
                        bridgeType,
                        keys[i],
                        exactFrameIndex,
                        compactFrameIndex,
                        out viaKey,
                        out keyAudit) && viaKey != null)
                {
                    source = "V207_ALIAS_OK key='" + keys[i] + "' exactFrame=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                             " compactFrame=" + compactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                             " keyAudit=[" + (keyAudit ?? string.Empty) + "] base=[" + (baseSource ?? string.Empty) + "]";
                    return viaKey;
                }

                if (audits.Count < 12 && !string.IsNullOrEmpty(keyAudit))
                    audits.Add(keyAudit);
            }

            source = "V207_ALIAS_MISS abs='" + (abs ?? string.Empty) + "' logical='" + (logicalPackage ?? string.Empty) +
                     "' exactFrame=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                     " compactFrame=" + compactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                     " base=[" + (baseSource ?? string.Empty) + "] aliases=[" + string.Join(" || ", audits.ToArray()) + "]";
            return null;
        }

        private static bool TryLoadBuildingBridgeKeyFrameV207LikeOriginal(
            Type bridgeType,
            string key,
            int exactFrameIndex,
            int compactFrameIndex,
            out Texture2D tex,
            out string audit)
        {
            tex = null;
            audit = string.Empty;
            if (bridgeType == null || string.IsNullOrWhiteSpace(key))
            {
                audit = "empty_bridge_or_key";
                return false;
            }

            string loadAudit = string.Empty;
            if (!s_C2BuildingLoadedGpKeysV207LikeOriginal.Contains(key))
            {
                string[] loadNames =
                {
                    "LoadG17ToMemory",
                    "LoadGPToMemory",
                    "LoadPackageToMemory",
                    "LoadG16ToMemory"
                };

                for (int i = 0; i < loadNames.Length; i++)
                {
                    MethodInfo load = bridgeType.GetMethod(loadNames[i], BindingFlags.Public | BindingFlags.Static);
                    if (load == null)
                        continue;

                    ParameterInfo[] ps = load.GetParameters();
                    object[] args = null;
                    if (ps.Length == 3) args = new object[] { key, null, false };
                    else if (ps.Length == 2) args = new object[] { key, null };
                    else if (ps.Length == 1) args = new object[] { key };
                    if (args == null)
                        continue;

                    try
                    {
                        object result = load.Invoke(null, args);
                        bool ok = !(result is bool) || (bool)result;
                        string err = args.Length > 1 ? args[1] as string : string.Empty;
                        loadAudit += loadNames[i] + "=" + (ok ? "True" : "False") +
                                     (string.IsNullOrEmpty(err) ? "" : ":" + err) + ";";
                        if (ok)
                            break;
                    }
                    catch (Exception ex)
                    {
                        loadAudit += loadNames[i] + "=EX:" + ex.GetType().Name + ";";
                    }
                }

                s_C2BuildingLoadedGpKeysV207LikeOriginal.Add(key);
            }

            string[] exactFrameMethods =
            {
                "TryGetG17FrameRGBAExact",
                "TryGetGPFrameRGBAExact",
                "TryGetPackageFrameRGBAExact",
                "TryGetFrameRGBAExact",
                "TryGetG16FrameRGBAExact"
            };

            string[] compactFrameMethods =
            {
                "TryGetG17FrameRGBA",
                "TryGetGPFrameRGBA",
                "TryGetPackageFrameRGBA",
                "TryGetFrameRGBA",
                "TryGetG16FrameRGBA"
            };

            string frameAudit = string.Empty;
            for (int pass = 0; pass < 2; pass++)
            {
                string[] names = pass == 0 ? exactFrameMethods : compactFrameMethods;
                int frameArg = pass == 0 ? exactFrameIndex : compactFrameIndex;
                string mode = pass == 0 ? "exact" : "compact";

                for (int i = 0; i < names.Length; i++)
                {
                    MethodInfo mi = bridgeType.GetMethod(names[i], BindingFlags.Public | BindingFlags.Static);
                    if (mi == null)
                        continue;

                    ParameterInfo[] ps = mi.GetParameters();
                    if (ps.Length != 6)
                    {
                        frameAudit += names[i] + ":bad_sig" + ps.Length.ToString(CultureInfo.InvariantCulture) + ";";
                        continue;
                    }

                    try
                    {
                        object[] args = { key, frameArg, 0, 0, null, null };
                        object result = mi.Invoke(null, args);
                        if (!(result is bool) || !(bool)result)
                        {
                            string err = args.Length > 5 ? args[5] as string : string.Empty;
                            frameAudit += names[i] + "=" + mode + ":false" +
                                          (string.IsNullOrEmpty(err) ? "" : ":" + err) + ";";
                            continue;
                        }

                        int w = args[2] is int ? (int)args[2] : 0;
                        int h = args[3] is int ? (int)args[3] : 0;
                        byte[] rgba = args[4] as byte[];
                        if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                        {
                            frameAudit += names[i] + "=" + mode + ":invalid_size " +
                                          w.ToString(CultureInfo.InvariantCulture) + "x" +
                                          h.ToString(CultureInfo.InvariantCulture) + ";";
                            continue;
                        }

                        tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                        tex.name = "C2_BLD_GP_ALIAS_V207_" + SanitizeBuildingNameV207LikeOriginal(key) +
                                   "_frame_" + exactFrameIndex.ToString(CultureInfo.InvariantCulture);
                        tex.LoadRawTextureData(rgba);
                        tex.Apply(false, false);
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;

                        audit = "key='" + key + "' via=" + names[i] +
                                " mode=" + mode +
                                " frameArg=" + frameArg.ToString(CultureInfo.InvariantCulture) +
                                " load=[" + loadAudit + "] frames=[" + frameAudit + "]";
                        return true;
                    }
                    catch (Exception ex)
                    {
                        frameAudit += names[i] + "=" + mode + ":EX:" + ex.GetType().Name + ";";
                    }
                }
            }

            audit = "key='" + key + "' load=[" + loadAudit + "] frames=[" + frameAudit + "]";
            return false;
        }

        private static List<string> BuildBuildingGpAliasKeysV207LikeOriginal(string abs, string logicalPackage)
        {
            var keys = new List<string>();

            AddBuildingGpAliasKeyV207LikeOriginal(keys, logicalPackage);
            if (!string.IsNullOrEmpty(logicalPackage))
            {
                string slash = logicalPackage.Replace('\\', '/');
                string backslash = logicalPackage.Replace('/', '\\');
                AddBuildingGpAliasKeyV207LikeOriginal(keys, slash);
                AddBuildingGpAliasKeyV207LikeOriginal(keys, backslash);
                AddBuildingGpAliasKeyV207LikeOriginal(keys, logicalPackage.ToUpperInvariant());

                string logicalName = Path.GetFileName(logicalPackage);
                if (!string.IsNullOrEmpty(logicalName))
                {
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, logicalName);
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, logicalName.ToUpperInvariant());
                }
            }

            AddBuildingGpAliasKeyV207LikeOriginal(keys, abs);
            if (!string.IsNullOrEmpty(abs))
            {
                string fullNoExt = Path.ChangeExtension(abs, null);
                string stem = Path.GetFileNameWithoutExtension(abs);
                AddBuildingGpAliasKeyV207LikeOriginal(keys, fullNoExt);
                AddBuildingGpAliasKeyV207LikeOriginal(keys, stem);
                AddBuildingGpAliasKeyV207LikeOriginal(keys, stem != null ? stem.ToUpperInvariant() : null);

                const string cashPrefix = "UNITSG17_";
                if (!string.IsNullOrEmpty(stem) && stem.StartsWith(cashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string rest = stem.Substring(cashPrefix.Length);
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, rest);
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, rest.ToUpperInvariant());
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, "UnitsG17\\" + rest);
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, "UnitsG17/" + rest);
                    AddBuildingGpAliasKeyV207LikeOriginal(keys, ("UnitsG17\\" + rest).ToUpperInvariant());
                }
            }

            return keys;
        }

        private static void AddBuildingGpAliasKeyV207LikeOriginal(List<string> keys, string key)
        {
            if (keys == null || string.IsNullOrWhiteSpace(key))
                return;

            key = key.Trim();
            for (int i = 0; i < keys.Count; i++)
            {
                if (string.Equals(keys[i], key, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            keys.Add(key);
        }

        private static string SanitizeBuildingNameV207LikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "null";

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_' || c == '-')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            return sb.ToString();
        }




        private static Texture2D TryLoadG16FrameNativeExactV208LikeOriginal(string abs, int exactFrameIndex, out string source)
        {
            source = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "V208_native_path_not_found:" + (abs ?? string.Empty);
                    return null;
                }

                byte[] file = File.ReadAllBytes(abs);
                if (file == null || file.Length < 21)
                {
                    source = "V208_native_too_small";
                    return null;
                }

                int blockOffset;
                int blockSize;
                uint blockMagic;
                string blockAudit;
                if (!FindG16BlockNativeV208LikeOriginal(file, out blockOffset, out blockSize, out blockMagic, out blockAudit))
                {
                    source = "V208_native_find_block_failed:" + (blockAudit ?? string.Empty);
                    return null;
                }

                int w;
                int h;
                byte[] rgba;
                string renderAudit;
                bool ok;
                if (blockMagic == 0x36315547u) // GU16
                    ok = TryRenderGU16FrameNativeV208LikeOriginal(file, blockOffset, blockSize, exactFrameIndex, out w, out h, out rgba, out renderAudit);
                else if (blockMagic == 0x36314E47u) // GN16
                    ok = TryRenderGN16FrameNativeV208LikeOriginal(file, blockOffset, blockSize, exactFrameIndex, out w, out h, out rgba, out renderAudit);
                else
                {
                    source = "V208_native_bad_magic 0x" + blockMagic.ToString("X8", CultureInfo.InvariantCulture);
                    return null;
                }

                if (!ok || w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                {
                    source = "V208_native_render_failed " + (renderAudit ?? string.Empty);
                    return null;
                }

                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                tex.name = "C2_BLD_G16_NATIVE_V208_" + Path.GetFileNameWithoutExtension(abs) +
                           "_exact_" + exactFrameIndex.ToString(CultureInfo.InvariantCulture);
                tex.LoadRawTextureData(rgba);
                tex.Apply(false, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                source = "V208_native_render_ok path='" + abs + "' exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                         " size=" + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture) +
                         " " + (renderAudit ?? string.Empty);
                return tex;
            }
            catch (Exception ex)
            {
                source = "V208_native_exception " + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static bool TryRenderGU16FrameNativeV208LikeOriginal(
            byte[] file,
            int blockOffset,
            int blockSize,
            int exactFrameIndex,
            out int width,
            out int height,
            out byte[] rgba,
            out string audit)
        {
            width = 0;
            height = 0;
            rgba = null;
            audit = string.Empty;

            try
            {
                if (blockOffset < 0 || blockOffset + 21 > file.Length)
                {
                    audit = "GU16 header out of range";
                    return false;
                }

                uint storedBlockSize = ReadU32LENativeV208LikeOriginal(file, blockOffset + 4);
                int framesPerSegment = file[blockOffset + 8];
                if (framesPerSegment <= 0)
                    framesPerSegment = 16;

                int spriteCount = ReadU16LENativeV208LikeOriginal(file, blockOffset + 9);
                width = ReadU16LENativeV208LikeOriginal(file, blockOffset + 11);
                height = ReadU16LENativeV208LikeOriginal(file, blockOffset + 13);
                uint maxWorkbuf = ReadU32LENativeV208LikeOriginal(file, blockOffset + 15);
                int packSegments = ReadU16LENativeV208LikeOriginal(file, blockOffset + 19);

                if (storedBlockSize > 0 && storedBlockSize <= file.Length - blockOffset)
                    blockSize = (int)storedBlockSize;

                if (spriteCount <= 0 || width <= 0 || height <= 0 || packSegments <= 0)
                {
                    audit = "GU16 bad header sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture) +
                            " size=" + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                            " segs=" + packSegments.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                if (exactFrameIndex < 0 || exactFrameIndex >= spriteCount)
                {
                    audit = "GU16 exact out of range exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                            " sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                int segIndex = exactFrameIndex / framesPerSegment;
                int localFrame = exactFrameIndex - segIndex * framesPerSegment;
                if (segIndex < 0 || segIndex >= packSegments)
                {
                    audit = "GU16 seg index out of range seg=" + segIndex.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                int segTable = blockOffset + 21;
                int spriteTable = segTable + packSegments * 4;
                if (spriteTable + spriteCount * 2 > file.Length)
                {
                    audit = "GU16 sprite table out of range";
                    return false;
                }

                int chunks = ReadU16LENativeV208LikeOriginal(file, spriteTable + exactFrameIndex * 2);
                if (chunks <= 0)
                {
                    audit = "GU16 exact empty exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                uint segData = ReadU32LENativeV208LikeOriginal(file, segTable + segIndex * 4);
                uint segStart = segData >> 4;
                uint flags = segData & 0xF;
                uint segNext = (segIndex + 1 < packSegments)
                    ? (ReadU32LENativeV208LikeOriginal(file, segTable + (segIndex + 1) * 4) >> 4)
                    : (uint)blockSize;

                if (segStart >= blockSize || segNext <= segStart || blockOffset + segNext > file.Length)
                {
                    audit = "GU16 bad segment range start=" + segStart.ToString(CultureInfo.InvariantCulture) +
                            " next=" + segNext.ToString(CultureInfo.InvariantCulture) +
                            " block=" + blockSize.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                int segLen = (int)(segNext - segStart);
                byte[] segBytes = new byte[segLen];
                Buffer.BlockCopy(file, blockOffset + (int)segStart, segBytes, 0, segLen);

                uint framesInSeg = (uint)Math.Min(framesPerSegment, spriteCount - segIndex * framesPerSegment);
                uint[] frameOffsets = new uint[framesInSeg];

                uint declaredOutLen = segLen >= 4 ? ReadU32LENativeV208LikeOriginal(segBytes, 0) : 0;
                uint workNeed = Math.Max(Math.Max((uint)blockSize, maxWorkbuf), declaredOutLen);
                if (workNeed < 1024)
                    workNeed = 1024;

                byte[] workbuf = new byte[(int)Math.Min((long)workNeed + 8192L, 256L * 1024L * 1024L)];
                long outCapLong = Math.Max((long)maxWorkbuf * 4L + 8192L, (long)declaredOutLen * 2L + 8192L);
                outCapLong = Math.Max(outCapLong, (long)width * (long)height * 4L + 1024L);
                if (outCapLong > 256L * 1024L * 1024L)
                    outCapLong = 256L * 1024L * 1024L;
                byte[] outbuf = new byte[(int)outCapLong];

                var decoder = new G16AnalyzerLib.G16SegmentDecoder();
                bool ok = decoder.UnpackSegmentSafe(
                    segBytes,
                    (uint)segBytes.Length,
                    outbuf,
                    (uint)outbuf.Length,
                    workbuf,
                    (uint)workbuf.Length,
                    frameOffsets,
                    framesInSeg,
                    flags);

                if (!ok)
                {
                    audit = "GU16 native unpack failed flags=0x" + flags.ToString("X", CultureInfo.InvariantCulture) +
                            " err=" + decoder.GetLastUnpackError();
                    return false;
                }

                if (localFrame < 0 || localFrame >= frameOffsets.Length)
                {
                    audit = "GU16 local frame out of range local=" + localFrame.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                if (!BuildG16FrameRgbaNativeV208LikeOriginal(outbuf, frameOffsets[localFrame], chunks, width, height, out rgba, out string buildAudit))
                {
                    audit = "GU16 native build failed " + buildAudit;
                    return false;
                }

                audit = "GU16 native exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                        " seg=" + segIndex.ToString(CultureInfo.InvariantCulture) +
                        " local=" + localFrame.ToString(CultureInfo.InvariantCulture) +
                        " chunks=" + chunks.ToString(CultureInfo.InvariantCulture) +
                        " flags=0x" + flags.ToString("X", CultureInfo.InvariantCulture) +
                        " segLen=" + segLen.ToString(CultureInfo.InvariantCulture) +
                        " outLen=" + declaredOutLen.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                audit = "GU16 native exception " + ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private static bool TryRenderGN16FrameNativeV208LikeOriginal(
            byte[] file,
            int blockOffset,
            int blockSize,
            int exactFrameIndex,
            out int width,
            out int height,
            out byte[] rgba,
            out string audit)
        {
            width = 0;
            height = 0;
            rgba = null;
            audit = string.Empty;

            try
            {
                if (blockOffset < 0 || blockOffset + 16 > file.Length)
                {
                    audit = "GN16 header out of range";
                    return false;
                }

                uint storedBlockSize = ReadU32LENativeV208LikeOriginal(file, blockOffset + 4);
                int spriteCount = ReadU16LENativeV208LikeOriginal(file, blockOffset + 8);
                uint maxWorkbuf = ReadU32LENativeV208LikeOriginal(file, blockOffset + 10);
                int packSegments = ReadU16LENativeV208LikeOriginal(file, blockOffset + 14);

                if (storedBlockSize > 0 && storedBlockSize <= file.Length - blockOffset)
                    blockSize = (int)storedBlockSize;

                if (spriteCount <= 0 || packSegments <= 0 || exactFrameIndex < 0 || exactFrameIndex >= spriteCount)
                {
                    audit = "GN16 bad header/range sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture) +
                            " segs=" + packSegments.ToString(CultureInfo.InvariantCulture) +
                            " exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                int segTable = blockOffset + 16;
                int segHdrSize = 6;
                int spriteTable = segTable + packSegments * segHdrSize;
                int spriteSize = 8;
                if (spriteTable + spriteCount * spriteSize > file.Length)
                {
                    audit = "GN16 sprite table out of range";
                    return false;
                }

                int chunks = ReadU16LENativeV208LikeOriginal(file, spriteTable + exactFrameIndex * spriteSize + 0);
                width = ReadU16LENativeV208LikeOriginal(file, spriteTable + exactFrameIndex * spriteSize + 2);
                height = ReadU16LENativeV208LikeOriginal(file, spriteTable + exactFrameIndex * spriteSize + 4);
                int segIndex = ReadU16LENativeV208LikeOriginal(file, spriteTable + exactFrameIndex * spriteSize + 6);
                if (chunks <= 0 || width <= 0 || height <= 0 || segIndex < 0 || segIndex >= packSegments)
                {
                    audit = "GN16 empty/bad exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                int baseFrame = 0;
                for (int i = 0; i < segIndex; i++)
                    baseFrame += ReadU16LENativeV208LikeOriginal(file, segTable + i * segHdrSize + 4);
                int localFrame = exactFrameIndex - baseFrame;
                int framesInSegInt = ReadU16LENativeV208LikeOriginal(file, segTable + segIndex * segHdrSize + 4);
                if (localFrame < 0 || localFrame >= framesInSegInt)
                {
                    audit = "GN16 local frame out of range local=" + localFrame.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                uint segData = ReadU32LENativeV208LikeOriginal(file, segTable + segIndex * segHdrSize);
                uint segStart = segData >> 4;
                uint flags = segData & 0xF;
                uint segNext = (segIndex + 1 < packSegments)
                    ? (ReadU32LENativeV208LikeOriginal(file, segTable + (segIndex + 1) * segHdrSize) >> 4)
                    : (uint)blockSize;

                if (segStart >= blockSize || segNext <= segStart || blockOffset + segNext > file.Length)
                {
                    audit = "GN16 bad segment range";
                    return false;
                }

                int segLen = (int)(segNext - segStart);
                byte[] segBytes = new byte[segLen];
                Buffer.BlockCopy(file, blockOffset + (int)segStart, segBytes, 0, segLen);

                uint framesInSeg = (uint)framesInSegInt;
                uint[] frameOffsets = new uint[framesInSeg];

                uint declaredOutLen = segLen >= 4 ? ReadU32LENativeV208LikeOriginal(segBytes, 0) : 0;
                uint workNeed = Math.Max(Math.Max((uint)blockSize, maxWorkbuf), declaredOutLen);
                if (workNeed < 1024)
                    workNeed = 1024;

                byte[] workbuf = new byte[(int)Math.Min((long)workNeed + 8192L, 256L * 1024L * 1024L)];
                long outCapLong = Math.Max((long)maxWorkbuf * 4L + 8192L, (long)declaredOutLen * 2L + 8192L);
                outCapLong = Math.Max(outCapLong, (long)width * (long)height * 4L + 1024L);
                if (outCapLong > 256L * 1024L * 1024L)
                    outCapLong = 256L * 1024L * 1024L;
                byte[] outbuf = new byte[(int)outCapLong];

                var decoder = new G16AnalyzerLib.G16SegmentDecoder();
                bool ok = decoder.UnpackSegmentSafe(
                    segBytes,
                    (uint)segBytes.Length,
                    outbuf,
                    (uint)outbuf.Length,
                    workbuf,
                    (uint)workbuf.Length,
                    frameOffsets,
                    framesInSeg,
                    flags);

                if (!ok)
                {
                    audit = "GN16 native unpack failed flags=0x" + flags.ToString("X", CultureInfo.InvariantCulture) +
                            " err=" + decoder.GetLastUnpackError();
                    return false;
                }

                if (!BuildG16FrameRgbaNativeV208LikeOriginal(outbuf, frameOffsets[localFrame], chunks, width, height, out rgba, out string buildAudit))
                {
                    audit = "GN16 native build failed " + buildAudit;
                    return false;
                }

                audit = "GN16 native exact=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                        " seg=" + segIndex.ToString(CultureInfo.InvariantCulture) +
                        " local=" + localFrame.ToString(CultureInfo.InvariantCulture) +
                        " chunks=" + chunks.ToString(CultureInfo.InvariantCulture) +
                        " flags=0x" + flags.ToString("X", CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                audit = "GN16 native exception " + ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private static bool BuildG16FrameRgbaNativeV208LikeOriginal(
            byte[] outBuf,
            uint frameOffset,
            int numSquares,
            int width,
            int height,
            out byte[] rgba,
            out string audit)
        {
            rgba = null;
            audit = string.Empty;

            if (outBuf == null || frameOffset >= outBuf.Length || numSquares <= 0 || width <= 0 || height <= 0)
            {
                audit = "bad args";
                return false;
            }

            long totalBytes = (long)width * (long)height * 4L;
            if (totalBytes <= 0 || totalBytes > 268435456L)
            {
                audit = "bad output size";
                return false;
            }

            rgba = new byte[(int)totalBytes];
            int srcPos = (int)frameOffset;
            int squaresDone = 0;

            for (int s = 0; s < numSquares; s++)
            {
                if (srcPos + 8 > outBuf.Length)
                {
                    audit = "chunk header OOB s=" + s.ToString(CultureInfo.InvariantCulture);
                    break;
                }

                uint sqHdr = ReadU32LENativeV208LikeOriginal(outBuf, srcPos);
                uint pow = (sqHdr >> 28) & 0xF;
                if (pow > 12)
                {
                    audit = "bad chunk pow=" + pow.ToString(CultureInfo.InvariantCulture);
                    break;
                }

                int side = 1 << (int)pow;
                int pixBytes = side * side * 2;
                if (srcPos + 8 + pixBytes > outBuf.Length)
                {
                    audit = "chunk pixels OOB s=" + s.ToString(CultureInfo.InvariantCulture) +
                            " side=" + side.ToString(CultureInfo.InvariantCulture);
                    break;
                }

                int x = (int)((sqHdr >> 12) & 0xFFF);
                if ((x & 0x800) != 0)
                    x |= unchecked((int)0xFFFFF000);

                int y = (int)(sqHdr & 0xFFF);
                if ((y & 0x800) != 0)
                    y |= unchecked((int)0xFFFFF000);

                int pixelOffset = srcPos + 8;
                int x0 = Math.Max(0, x);
                int y0 = Math.Max(0, y);
                int x1 = Math.Min(width, x + side);
                int y1 = Math.Min(height, y + side);

                for (int yy = y0; yy < y1; yy++)
                {
                    for (int xx = x0; xx < x1; xx++)
                    {
                        int srcIdx = pixelOffset + ((yy - y) * side + (xx - x)) * 2;
                        ushort px = (ushort)(outBuf[srcIdx] | (outBuf[srcIdx + 1] << 8));

                        byte a = (byte)(((px >> 12) & 0xF) * 17);
                        if (a == 0)
                            continue;

                        byte r = (byte)(((px >> 8) & 0xF) * 17);
                        byte g = (byte)(((px >> 4) & 0xF) * 17);
                        byte b = (byte)((px & 0xF) * 17);

                        int di = (yy * width + xx) * 4;
                        rgba[di + 0] = r;
                        rgba[di + 1] = g;
                        rgba[di + 2] = b;
                        rgba[di + 3] = a;
                    }
                }

                srcPos += 8 + pixBytes;
                squaresDone++;
            }

            if (squaresDone <= 0)
            {
                audit = string.IsNullOrEmpty(audit) ? "zero chunks rendered" : audit;
                return false;
            }

            audit = "chunksRendered=" + squaresDone.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static bool FindG16BlockNativeV208LikeOriginal(
            byte[] file,
            out int outOffset,
            out int outSize,
            out uint outMagic,
            out string audit)
        {
            outOffset = 0;
            outSize = 0;
            outMagic = 0;
            audit = string.Empty;

            if (file == null || file.Length < 4)
            {
                audit = "file too small";
                return false;
            }

            uint m0 = ReadU32LENativeV208LikeOriginal(file, 0);
            if (m0 == 0x36315547u || m0 == 0x36314E47u)
            {
                outOffset = 0;
                outMagic = m0;
                uint bs = file.Length >= 8 ? ReadU32LENativeV208LikeOriginal(file, 4) : 0;
                outSize = (bs >= 8 && bs <= file.Length) ? (int)bs : file.Length;
                return true;
            }

            int pos = 0;
            while (pos + 12 <= file.Length)
            {
                uint size = ReadU32LENativeV208LikeOriginal(file, pos + 4);
                if (size == 0 || (long)pos + 8L + size > file.Length)
                    break;

                uint pm = ReadU32LENativeV208LikeOriginal(file, pos + 8);
                if (pm == 0x36315547u || pm == 0x36314E47u)
                {
                    outOffset = pos + 8;
                    outMagic = pm;
                    uint bs = (pos + 12 <= file.Length) ? ReadU32LENativeV208LikeOriginal(file, pos + 12) : 0;
                    outSize = (bs >= 8 && bs <= size) ? (int)bs : (int)size;
                    return true;
                }

                pos += 8 + (int)size;
            }

            audit = "GU16/GN16 magic not found";
            return false;
        }

        private static ushort ReadU16LENativeV208LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 1 >= data.Length)
                return 0;
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        private static uint ReadU32LENativeV208LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 3 >= data.Length)
                return 0;
            return (uint)(data[offset] |
                         (data[offset + 1] << 8) |
                         (data[offset + 2] << 16) |
                         (data[offset + 3] << 24));
        }

        private static bool TryGetBuildingG16NativeFrameDecodeIndexLikeOriginal(
            string abs,
            int exactFrame,
            out int chunkCount,
            out int spriteCount,
            out int decodeFrame,
            out string audit)
        {
            chunkCount = 0;
            spriteCount = 0;
            decodeFrame = exactFrame;
            audit = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    audit = "native_path_not_found:" + (abs ?? string.Empty);
                    return false;
                }

                using (FileStream fs = new FileStream(abs, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length < 25)
                    {
                        audit = "native_too_small len=" + fs.Length.ToString(CultureInfo.InvariantCulture);
                        return false;
                    }

                    byte[] header = new byte[21];
                    int got = fs.Read(header, 0, header.Length);
                    if (got != header.Length)
                    {
                        audit = "native_short_header got=" + got.ToString(CultureInfo.InvariantCulture);
                        return false;
                    }

                    string magic = Encoding.ASCII.GetString(header, 0, 4);
                    if (!string.Equals(magic, "GU16", StringComparison.Ordinal) &&
                        !string.Equals(magic, "GN16", StringComparison.Ordinal))
                    {
                        audit = "native_not_gu16_gn16 magic=" + magic;
                        return false;
                    }

                    int framesPerSegment = header[8];
                    if (framesPerSegment <= 0)
                        framesPerSegment = 16;

                    spriteCount = BitConverter.ToUInt16(header, 9);
                    if (spriteCount <= 0)
                    {
                        audit = "native_zero_sprites magic=" + magic;
                        return true;
                    }

                    int packSegments = (spriteCount + framesPerSegment - 1) / framesPerSegment;
                    long countsOffset = 21L + (long)packSegments * 4L;
                    long countsEnd = countsOffset + (long)spriteCount * 2L;
                    if (countsEnd > fs.Length)
                    {
                        audit = "native_counts_out_of_file magic=" + magic +
                                " sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture) +
                                " fps=" + framesPerSegment.ToString(CultureInfo.InvariantCulture) +
                                " countsOffset=" + countsOffset.ToString(CultureInfo.InvariantCulture) +
                                " len=" + fs.Length.ToString(CultureInfo.InvariantCulture);
                        return false;
                    }

                    if (exactFrame < 0 || exactFrame >= spriteCount)
                    {
                        audit = "native_exact_frame_out_of_range magic=" + magic +
                                " exact=" + exactFrame.ToString(CultureInfo.InvariantCulture) +
                                " sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture);
                        return true;
                    }

                    fs.Seek(countsOffset, SeekOrigin.Begin);
                    int compactIndex = 0;
                    for (int i = 0; i < spriteCount; i++)
                    {
                        int lo = fs.ReadByte();
                        int hi = fs.ReadByte();
                        if (lo < 0 || hi < 0)
                        {
                            audit = "native_short_counts at=" + i.ToString(CultureInfo.InvariantCulture);
                            return false;
                        }

                        int chunks = lo | (hi << 8);
                        if (i == exactFrame)
                        {
                            chunkCount = chunks;
                            decodeFrame = compactIndex;
                        }

                        if (chunks > 0)
                            compactIndex++;
                    }

                    audit = "native_g16_frame_table magic=" + magic +
                            " sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture) +
                            " fps=" + framesPerSegment.ToString(CultureInfo.InvariantCulture) +
                            " exact=" + exactFrame.ToString(CultureInfo.InvariantCulture) +
                            " chunks=" + chunkCount.ToString(CultureInfo.InvariantCulture) +
                            " compact=" + decodeFrame.ToString(CultureInfo.InvariantCulture) +
                            " emptyBefore=" + (exactFrame - decodeFrame).ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch (Exception ex)
            {
                audit = "native_exception " + ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private static string FindBuildingVisualPathLikeOriginal(string package)
        {
            string pkg = CleanPackageNameLikeOriginal(package);
            if (string.IsNullOrWhiteSpace(pkg))
                return string.Empty;

            string cacheKey = pkg.ToLowerInvariant();
            if (s_C2BuildingVisualPathCacheLikeOriginal.TryGetValue(cacheKey, out string cached) && File.Exists(cached))
                return cached;

            List<string> candidates = BuildVisualCandidatesLikeOriginal(pkg);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    s_C2BuildingVisualPathCacheLikeOriginal[cacheKey] = candidates[i];
                    return candidates[i];
                }
            }

            return string.Empty;
        }

        private static List<string> BuildVisualCandidatesLikeOriginal(string package)
        {
            var result = new List<string>();
            void Add(string p)
            {
                if (!string.IsNullOrWhiteSpace(p) && !result.Exists(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase)))
                    result.Add(p);
            }

            string pkg = CleanPackageNameLikeOriginal(package);
            string noExt = Path.ChangeExtension(pkg, null) ?? pkg;
            string flat = noExt.Replace('\\', '_').Replace('/', '_');
            string bare = Path.GetFileName(noExt);
            string[] exts = { ".g16", ".G16", ".g17", ".G17" };
            List<string> roots = DataRootsForBuildingsLikeOriginal();

            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                for (int e = 0; e < exts.Length; e++)
                {
                    string ext = exts[e];
                    Add(Path.Combine(root, noExt + ext));
                    Add(Path.Combine(root, flat + ext));
                    Add(Path.Combine(root, bare + ext));
                    Add(Path.Combine(root, "Cash", flat + ext));
                    Add(Path.Combine(root, "Cash", bare + ext));
                    Add(Path.Combine(root, "Data", "Cash", flat + ext));
                    Add(Path.Combine(root, "Data1", "Cash", flat + ext));
                    Add(Path.Combine(root, "UnitsG17", bare + ext));
                    Add(Path.Combine(root, "Data", "UnitsG17", bare + ext));
                }
            }

            return result;
        }

        private void CreateBuildingCompositeLikeOriginal(
            Transform root,
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md,
            List<C2BuildingLoadedPartLikeOriginal> parts,
            bool attachGameplayRuntimeV303LikeOriginal = true)
        {
            var parent = new GameObject("C2_Building_" + SanitizeNameLikeOriginal(record.MonsterId) + "_" + record.Index.ToString(CultureInfo.InvariantCulture));
            parent.transform.SetParent(root, false);
            parent.transform.position = BuildingWorldPosLikeOriginal(record);
            if (C2SpriteDepthOverlayCameraLikeOriginal)
                parent.layer = C2SpriteDepthLayerLikeOriginal.LayerIndex;

            float mapPixelScale = C2MapPixelToWorldScaleV277LikeOriginal();
            float spriteScale = BuildingSpritePixelToWorldScaleV276LikeOriginal(mapPixelScale);
            float nominalPerspectiveDistanceV292 = BuildingNominalPerspectiveDistanceWorldV292LikeOriginal();
            bool runtimeConstructedBuilding =
                root != null &&
                (root.GetComponentInParent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>() != null ||
                 root.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>() != null);
            int compositeMaxSortingOrderV206 = int.MinValue;
            List<MeshRenderer> lineSortDepthRenderersV251 = new List<MeshRenderer>(parts.Count);
            for (int i = 0; i < parts.Count; i++)
            {
                C2BuildingLoadedPartLikeOriginal part = parts[i];
                if (part == null || part.Texture == null)
                    continue;

                Mesh mesh = BuildBuildingPartMeshLikeOriginal(md, part, spriteScale, nominalPerspectiveDistanceV292);
                var go = new GameObject("part_" + i.ToString(CultureInfo.InvariantCulture) + "_" + part.AnimationName + "_" + part.Frame.SpriteId.ToString(CultureInfo.InvariantCulture));
                go.transform.SetParent(parent.transform, false);
                if (C2SpriteDepthOverlayCameraLikeOriginal)
                    go.layer = C2SpriteDepthLayerLikeOriginal.LayerIndex;
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = GetBuildingMaterialLikeOriginal(part.Texture);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                int sortingOrder = BuildingSortOrderLikeOriginal(record, part, i, runtimeConstructedBuilding);
                mr.sortingOrder = sortingOrder;
                bool groundLineSortPart = part != null && part.HasLineSort && part.LineSort.IsGround;
                // GROUND frames can contain stairs and entire lower storeys (EngKaz).
                // Only their translucent shadow pixels may skip depth.
                if (C2SpriteDepthPrepassV1LikeOriginal)
                {
                    Material depthMat = GetBuildingDepthMaterialLikeOriginal(part.Texture, groundLineSortPart);
                    C2SpriteDepthPrepassLikeOriginal.AddDepthRendererLikeOriginal(
                        go,
                        mesh,
                        depthMat,
                        sortingOrder,
                        "depth_cutout_prepass_" + i.ToString(CultureInfo.InvariantCulture));
                }
                lineSortDepthRenderersV251.Add(mr);
                if (sortingOrder > compositeMaxSortingOrderV206)
                    compositeMaxSortingOrderV206 = sortingOrder;

                RegisterBuildingPseudoProjectionEntryLikeOriginal(
                    parent.transform,
                    mesh,
                    new[] { BuildOriginalDrawSpriteBuildingPointsLikeOriginal(md, part) },
                    spriteScale,
                    md,
                    null, part);
            }

            CreateBuildingWorkAnimationOverlayV206LikeOriginal(parent.transform, record, md, parts.Count, spriteScale, nominalPerspectiveDistanceV292, compositeMaxSortingOrderV206, runtimeConstructedBuilding);
            C2BuildingLineSortDepthV251AttachLikeOriginal(parent, record, md, parts, lineSortDepthRenderersV251, spriteScale);

            // V303: preview ghosts are visual-only.  Only real map/runtime buildings may
            // register gameplay runtime info, selectable/HUD, production queue and service routes.
            // Otherwise a deleted preview/stage child can be picked as the source of BORNPOINTS
            // and the unit exits from the same sprite with an offset.
            if (attachGameplayRuntimeV303LikeOriginal)
            {
                C2BuildingRuntimeV247AttachBuildingLikeOriginal(parent, record, md, spriteScale, mapPixelScale);
            }
        }

        private void CreateBuildingWorkAnimationOverlayV206LikeOriginal(
            Transform parent,
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md,
            int basePartCount,
            float scale,
            float nominalPerspectiveDistanceV292,
            int baseMaxSortingOrder,
            bool runtimeConstructedBuilding)
        {
            if (!ShouldDrawBuildingWorkAnimationV206LikeOriginal(md, record))
                return;

            var textures = new List<Texture2D>();
            var vertices = new List<Vector3[]>();
            var originalDrawVertices = new List<Vector3[]>();
            var loadedFrameIds = new List<int>();
            int totalWorkFrames = md.WorkFrames.Count;
            int frameCount = C2BuildingsWorkAnimationMaxFramesV206LikeOriginal > 0
                ? Math.Min(totalWorkFrames, C2BuildingsWorkAnimationMaxFramesV206LikeOriginal)
                : totalWorkFrames;

            int missed = 0;
            string firstMissAudit = string.Empty;
            for (int i = 0; i < frameCount; i++)
            {
                C2BuildingAnimFrameLikeOriginal frameRef = md.WorkFrames[i];
                string pkg = PackageForFileRefLikeOriginal(md, frameRef.FileRef);
                Texture2D tex = TryLoadBuildingFrameTextureLikeOriginal(pkg, frameRef.SpriteId, out string workAudit);
                if (tex == null)
                {
                    missed++;
                    if (string.IsNullOrEmpty(firstMissAudit))
                        firstMissAudit = "frame=" + frameRef.SpriteId.ToString(CultureInfo.InvariantCulture) + " " + (workAudit ?? string.Empty);
                    continue;
                }

                RegisterBuildingTextureUseV226LikeOriginal(tex, pkg, frameRef.SpriteId, "work");

                var workPart = new C2BuildingLoadedPartLikeOriginal
                {
                    Texture = tex,
                    Frame = frameRef,
                    AnimationName = "#WORK",
                    HasLineSort = false
                };
                textures.Add(tex);
                loadedFrameIds.Add(frameRef.SpriteId);
                vertices.Add(BuildDrawSpriteBuildingVerticesLikeOriginal(md, workPart, scale, nominalPerspectiveDistanceV292));
                originalDrawVertices.Add(BuildOriginalDrawSpriteBuildingPointsLikeOriginal(md, workPart));
            }

            if (textures.Count == 0)
            {
                if (C2BuildingsWorkAnimationAuditV206LikeOriginal)
                {
                    Debug.Log("[C2:BUILDINGS 3INU V206 WORK ANIM MISS] obj=" + record.Index.ToString(CultureInfo.InvariantCulture) +
                              " name='" + (record.MonsterId ?? string.Empty) + "' md=" + (md != null ? (md.MdName ?? string.Empty) : string.Empty) +
                              " pkg=" + (md != null ? (md.Package ?? string.Empty) : string.Empty) +
                              " workFrames=" + totalWorkFrames.ToString(CultureInfo.InvariantCulture) +
                              " missed=" + missed.ToString(CultureInfo.InvariantCulture) +
                              " firstMiss=[" + firstMissAudit + "]");
                }
                return;
            }

            var go = new GameObject("work_anim_#WORK_V206_" + textures.Count.ToString(CultureInfo.InvariantCulture));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.0005f * (basePartCount + 1));
            if (C2SpriteDepthOverlayCameraLikeOriginal)
                go.layer = C2SpriteDepthLayerLikeOriginal.LayerIndex;

            Mesh mesh = new Mesh();
            mesh.name = "work_anim_#WORK_V206_" + textures.Count.ToString(CultureInfo.InvariantCulture) + "_Mesh";
            mesh.vertices = vertices[0];
            mesh.uv = new[]
            {
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(0.0f, 0.0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            Material baseMat = GetBuildingMaterialLikeOriginal(textures[0]);
            mr.sharedMaterial = baseMat != null ? new Material(baseMat) : null;
            if (mr.sharedMaterial != null)
                mr.sharedMaterial.mainTexture = textures[0];
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var firstPart = new C2BuildingLoadedPartLikeOriginal
            {
                Texture = textures[0],
                Frame = md.WorkFrames.Count > 0 ? md.WorkFrames[0] : new C2BuildingAnimFrameLikeOriginal(0, 0),
                AnimationName = "#WORK",
                HasLineSort = false
            };
            int fallbackOrder = BuildingSortOrderLikeOriginal(record, firstPart, basePartCount + 1, runtimeConstructedBuilding);
            bool windmillFront = IsBuildingWindmillV206LikeOriginal(md, record) && baseMaxSortingOrder > int.MinValue / 2;
            mr.sortingOrder = windmillFront
                ? Mathf.Clamp(baseMaxSortingOrder + 8, -30000, 30000)
                : fallbackOrder;

            MeshRenderer depthRenderer = null;
            if (C2SpriteDepthPrepassV1LikeOriginal)
            {
                Material depthMat = C2SpriteDepthPrepassLikeOriginal.CreateDepthMaterialLikeOriginal(
                    "C2_BuildingWorkDepth_" + SanitizeNameLikeOriginal(record.MonsterId),
                    textures[0],
                    C2BuildingsDepthAlphaCutoffLikeOriginal);
                if (depthMat != null) depthMat.SetFloat("_C2ScreenAffine", 1.0f);
                depthRenderer = C2SpriteDepthPrepassLikeOriginal.AddDepthRendererLikeOriginal(
                    go,
                    mesh,
                    depthMat,
                    mr.sortingOrder,
                    "depth_cutout_prepass_work");
            }

            var animator = go.AddComponent<C2BuildingWorkFrameAnimatorV206LikeOriginal>();
            animator.Textures = textures.ToArray();
            animator.Vertices = vertices.ToArray();
            animator.Mesh = mesh;
            animator.Renderer = mr;
            animator.DepthRenderer = depthRenderer;
            animator.FrameRate = BuildingWorkAnimationFpsV206LikeOriginal(md, record);
            animator.RuntimePseudoProjectionOwnsVerticesLikeOriginal = true;

            RegisterBuildingPseudoProjectionEntryLikeOriginal(
                parent,
                mesh,
                originalDrawVertices.ToArray(),
                scale,
                md,
                animator);

            if (C2BuildingsWorkAnimationAuditV206LikeOriginal)
            {
                Debug.Log("[C2:BUILDINGS 3INU V206 WORK ANIM OK] obj=" + record.Index.ToString(CultureInfo.InvariantCulture) +
                          " name='" + (record.MonsterId ?? string.Empty) + "' md=" + (md != null ? (md.MdName ?? string.Empty) : string.Empty) +
                          " pkg=" + (md != null ? (md.Package ?? string.Empty) : string.Empty) +
                          " loaded=" + textures.Count.ToString(CultureInfo.InvariantCulture) +
                          "/" + totalWorkFrames.ToString(CultureInfo.InvariantCulture) +
                          " missed=" + missed.ToString(CultureInfo.InvariantCulture) +
                          " fps=" + animator.FrameRate.ToString(CultureInfo.InvariantCulture) +
                          " sorting=" + mr.sortingOrder.ToString(CultureInfo.InvariantCulture) +
                          " nds=" + s_C2BuildingNdsAliasAuditV206LikeOriginal +
                          " firstFrame=" + (loadedFrameIds.Count > 0 ? loadedFrameIds[0].ToString(CultureInfo.InvariantCulture) : "<none>") +
                          " lastFrame=" + (loadedFrameIds.Count > 0 ? loadedFrameIds[loadedFrameIds.Count - 1].ToString(CultureInfo.InvariantCulture) : "<none>") +
                          " rule=md_#WORK_overlay_drawspritebuilding_v206");
            }
        }

        private static float BuildingWorkAnimationFpsV206LikeOriginal(C2BuildingMdInfoLikeOriginal md, C2Building3InuRecordLikeOriginal record)
        {
            string key = ((md != null ? ((md.MdName ?? string.Empty) + " " + (md.Package ?? string.Empty)) : string.Empty) +
                          " " + (record.MonsterId ?? string.Empty));
            if (key.IndexOf("EgpMel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("BldMel(EG", StringComparison.OrdinalIgnoreCase) >= 0)
                return C2BuildingsWorkAnimationFpsV206LikeOriginal * 5.0f;

            return C2BuildingsWorkAnimationFpsV206LikeOriginal;
        }

        private static bool ShouldDrawBuildingWorkAnimationV206LikeOriginal(C2BuildingMdInfoLikeOriginal md, C2Building3InuRecordLikeOriginal record)
        {
            if (!C2BuildingsDrawWorkAnimationOverlayV206LikeOriginal)
                return false;
            if (md == null || md.WorkFrames == null || md.WorkFrames.Count == 0)
                return false;
            if (!md.Building && !md.SpriteObject)
                return false;
            return LooksLikeBuildingMillOrMineV206LikeOriginal(md, record);
        }

        private static bool LooksLikeBuildingMillOrMineV206LikeOriginal(C2BuildingMdInfoLikeOriginal md, C2Building3InuRecordLikeOriginal record)
        {
            string s = ((record.MonsterId ?? string.Empty) + " " +
                        (md != null ? (md.MdName ?? string.Empty) : string.Empty) + " " +
                        (md != null ? (md.Usage ?? string.Empty) : string.Empty) + " " +
                        (md != null ? (md.MdPath ?? string.Empty) : string.Empty)).ToUpperInvariant();

            return s.IndexOf("MEL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("MELN", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("MILL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("RUD", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("MINE", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("MELNICA", StringComparison.Ordinal) >= 0;
        }

        private static bool IsBuildingWindmillV206LikeOriginal(C2BuildingMdInfoLikeOriginal md, C2Building3InuRecordLikeOriginal record)
        {
            string s = ((record.MonsterId ?? string.Empty) + " " +
                        (md != null ? (md.MdName ?? string.Empty) : string.Empty) + " " +
                        (md != null ? (md.Package ?? string.Empty) : string.Empty) + " " +
                        (md != null ? (md.MdPath ?? string.Empty) : string.Empty)).ToUpperInvariant();

            return s.IndexOf("BLDMEL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("FRNMEL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("RUSMEL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("SPNMIL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("EGPMEL", StringComparison.Ordinal) >= 0 ||
                   s.IndexOf("MELNICA", StringComparison.Ordinal) >= 0;
        }

        private Vector3 BuildingWorldPosLikeOriginal(C2Building3InuRecordLikeOriginal r)
        {
            return WallOriginalXYToWorldV1LikeOriginal(r.RealX >> 4, r.RealY >> 4, 0.0f);
        }

        internal float C2MapPixelToWorldScaleV277LikeOriginal()
        {
            float scale = 1.0f;
            try
            {
                scale = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            }
            catch
            {
                scale = 1.0f;
            }

            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.0001f)
                scale = 1.0f;

            return scale;
        }

        internal float C2OriginalNativeVisualPixelToWorldScaleV277LikeOriginal(float fallbackMapPixelScale)
        {
            // C2 DrawWorldSprite and DrawSpriteBuilding use native pixels as
            // world units, exactly like PicDx/LOCKPOINTS/BORNPOINTS. Perspective
            // magnification is already in the camera matrix. The former
            // 2*tan(FOV/2)*CameraFactor compensation scaled art a SECOND time
            // (1.234289 at the default camera), moving doors away from MD routes.
            float scale = Mathf.Max(0.0001f, fallbackMapPixelScale);
            _c2NativeVisualPixelToWorldScaleV277LikeOriginal = scale;
            _c2NativeVisualPixelToWorldScaleSourceV277LikeOriginal = "native_world_sprite_equals_map_unit";
            return scale;
        }

        private float BuildingSpritePixelToWorldScaleV276LikeOriginal(float fallbackMapPixelScale)
        {
            float fallback = Mathf.Max(0.0001f, fallbackMapPixelScale);
            float scale = C2OriginalNativeVisualPixelToWorldScaleV277LikeOriginal(fallback);
            string source = string.IsNullOrEmpty(_c2NativeVisualPixelToWorldScaleSourceV277LikeOriginal)
                ? "locked_visual_no_camera_v287"
                : _c2NativeVisualPixelToWorldScaleSourceV277LikeOriginal;

            if (!s_C2BuildingSpriteScaleV276LoggedLikeOriginal)
            {
                s_C2BuildingSpriteScaleV276LoggedLikeOriginal = true;
                Camera cam = _strictIsoCamera != null ? _strictIsoCamera : GetActiveBattleCameraLikeOriginal();
                Debug.Log("[C2:BUILDINGS SPRITE SCALE V380] lockedVisualPixelToWorld=" +
                          scale.ToString("0.######", CultureInfo.InvariantCulture) +
                          " mapPixelToWorld=" + fallback.ToString("0.######", CultureInfo.InvariantCulture) +
                          " visualToMap=" + (scale / fallback).ToString("0.######", CultureInfo.InvariantCulture) +
                          " source=" + source +
                          " cameraObserverOnly=1" +
                          " orthoIgnored=" + (cam != null && cam.orthographic) +
                          " contract=sprite_and_MD_share_original_map_unit");
            }

            return scale;
        }

        private float BuildingNominalPerspectiveDistanceWorldV292LikeOriginal()
        {
            float strictScale = 1.0f;
            try
            {
                strictScale = Mathf.Max(0.0001f, GetStrictScaleLikeOriginal());
            }
            catch
            {
                strictScale = 1.0f;
            }

            float widthLikeOriginal = 1920.0f;
            try
            {
                Camera activeCamera = GetActiveBattleCameraLikeOriginal();
                widthLikeOriginal = activeCamera != null
                    ? Mathf.Max(1.0f, activeCamera.pixelRect.width)
                    : Mathf.Max(1.0f, Screen.width);
            }
            catch
            {
                widthLikeOriginal = 1920.0f;
            }

            return Mathf.Max(1.0f, widthLikeOriginal * strictScale * StrictIsoCameraFactor);
        }

        private static Mesh BuildBuildingPartMeshLikeOriginal(C2BuildingMdInfoLikeOriginal md, C2BuildingLoadedPartLikeOriginal part, float scale, float nominalPerspectiveDistanceV292)
        {
            Vector3[] vertices = BuildDrawSpriteBuildingVerticesLikeOriginal(md, part, scale, nominalPerspectiveDistanceV292);
            var mesh = new Mesh();
            mesh.name = "C2_BuildingPart_DrawSpriteBuilding";
            mesh.vertices = vertices;
            mesh.uv = new[]
            {
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(0.0f, 0.0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3[] BuildDrawSpriteBuildingVerticesLikeOriginal(C2BuildingMdInfoLikeOriginal md, C2BuildingLoadedPartLikeOriginal part, float scale, float nominalPerspectiveDistanceV292)
        {
            Vector3[] originalPoints = BuildOriginalDrawSpriteBuildingPointsLikeOriginal(md, part);
            C2BuildingVisualProjectionContextV292LikeOriginal projectionV292 =
                BuildBuildingVisualProjectionContextV292LikeOriginal(md, scale, nominalPerspectiveDistanceV292);
            return new[]
            {
                ProjectOriginalDrawSpaceToUnityLocalV292LikeOriginal(originalPoints[0], scale, projectionV292),
                ProjectOriginalDrawSpaceToUnityLocalV292LikeOriginal(originalPoints[1], scale, projectionV292),
                ProjectOriginalDrawSpaceToUnityLocalV292LikeOriginal(originalPoints[2], scale, projectionV292),
                ProjectOriginalDrawSpaceToUnityLocalV292LikeOriginal(originalPoints[3], scale, projectionV292)
            };
        }

        private static Vector3[] BuildOriginalDrawSpriteBuildingPointsLikeOriginal(C2BuildingMdInfoLikeOriginal md, C2BuildingLoadedPartLikeOriginal part)
        {
            int w = part.Texture != null ? part.Texture.width : 64;
            int h = part.Texture != null ? part.Texture.height : 64;
            int cropOffsetX = 0;
            int cropOffsetY = 0;
            if (part.Texture != null &&
                s_C2BuildingAlphaBoundsByTextureIdV228LikeOriginal.TryGetValue(part.Texture.GetEntityId(), out C2BuildingAlphaBoundsDiagV228LikeOriginal cropV229) &&
                cropV229 != null &&
                cropV229.TightCroppedV229)
            {
                cropOffsetX = cropV229.MinX;
                cropOffsetY = cropV229.MinY;
            }
            FramePivotLikeOriginal(md, part.Frame, out int dx, out int dy);

            Vector3 pivot = SkewPtLikeOriginal(-dx, -dy, 0.0f);
            C2BuildingMatrix4LikeOriginal tm;
            if (part.HasLineSort)
            {
                C2BuildingLineSortLikeOriginal li = part.LineSort;
                if (li.IsGround)
                    tm = GetAlignGroundTransformLikeOriginal(pivot);
                else if (li.IsTop)
                    tm = GetRolledBillboardTransformLikeOriginal(pivot);
                else
                    tm = GetAlignLineTransformLikeOriginal(pivot, li.X1, li.Y1, li.X2, li.Y2);
            }
            else
            {
                tm = GetRolledBillboardTransformLikeOriginal(pivot);
                if (!C2BuildingsPseudoProjectionVisualV292LikeOriginal)
                    tm.Translate(SkewPtLikeOriginal(0.0f, 80.0f, 40.0f));
            }

            float x0 = cropOffsetX;
            float y0 = cropOffsetY;
            float x1 = cropOffsetX + w;
            float y1 = cropOffsetY + h;

            Vector3 p0 = tm.TransformPoint(new Vector3(x0, y1, 0.0f));
            Vector3 p1 = tm.TransformPoint(new Vector3(x1, y1, 0.0f));
            Vector3 p2 = tm.TransformPoint(new Vector3(x1, y0, 0.0f));
            Vector3 p3 = tm.TransformPoint(new Vector3(x0, y0, 0.0f));
            return new[] { p0, p1, p2, p3 };
        }

        private static Vector3 SkewPtLikeOriginal(float x, float y, float z)
        {
            return new Vector3(x, y - 0.5f * z, z * C2BuildingsCosPiOver6LikeOriginal);
        }

        private static C2BuildingMatrix4LikeOriginal GetAlignGroundTransformLikeOriginal(Vector3 pivot)
        {
            C2BuildingMatrix4LikeOriginal tr = C2BuildingMatrix4LikeOriginal.Translation(-pivot);
            tr.e01 *= 2.0f;
            tr.e11 *= 2.0f;
            tr.e21 *= 2.0f;
            tr.e31 *= 2.0f;
            return tr;
        }

        private static C2BuildingMatrix4LikeOriginal GetRolledBillboardTransformLikeOriginal(Vector3 pivot)
        {
            C2BuildingMatrix4LikeOriginal tr = C2BuildingMatrix4LikeOriginal.Translation(-pivot);
            return tr * C2BuildingMatrix4LikeOriginal.RotationYZ(0.5f, -C2BuildingsCosPiOver6LikeOriginal);
        }

        private static C2BuildingMatrix4LikeOriginal GetAlignLineTransformLikeOriginal(Vector3 pivot, int x1, int y1, int x2, int y2)
        {
            float dy = (y2 - y1) / 0.5f;
            float dx = x2 - x1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len <= 0.000001f)
                return GetAlignGroundTransformLikeOriginal(pivot);

            float cosPhi = dx / len;
            float sinPhi = dy / len;
            C2BuildingMatrix4LikeOriginal sh = C2BuildingMatrix4LikeOriginal.ShearXZ(cosPhi, sinPhi);
            Vector3 center = new Vector3(-((float)(x1 + x2)) * 0.5f, -((float)(y1 + y2)) * 0.5f, 0.0f);
            C2BuildingMatrix4LikeOriginal trToC = C2BuildingMatrix4LikeOriginal.Translation(center);
            center = -center;
            center.z += (((y1 + y2) * 0.5f) - pivot.y) / 0.5f;
            C2BuildingMatrix4LikeOriginal trFromC = C2BuildingMatrix4LikeOriginal.Translation(center);
            C2BuildingMatrix4LikeOriginal trAlign = trToC * sh * trFromC;
            C2BuildingMatrix4LikeOriginal spriteToWorld = C2BuildingMatrix4LikeOriginal.Translation(-pivot);
            spriteToWorld = spriteToWorld * C2BuildingMatrix4LikeOriginal.RotationYZ(0.5f, -C2BuildingsCosPiOver6LikeOriginal);
            return trAlign * spriteToWorld;
        }

        private static Vector3 OriginalDrawSpaceToUnityLocalLikeOriginal(Vector3 original, float scale)
        {
            return new Vector3(original.x * scale, original.z * scale, -original.y * scale);
        }

        private struct C2BuildingVisualProjectionContextV292LikeOriginal
        {
            public bool Enabled;
            public Vector3 Dir;
            public Vector3 Right;
            public Vector3 Up;
            public float Distance;
            public float PlaneFactor;
            public bool HasThreePointAffine;
            public float A00;
            public float A01;
            public float A02;
            public float A10;
            public float A11;
            public float A12;
        }

        private static C2BuildingVisualProjectionContextV292LikeOriginal BuildBuildingVisualProjectionContextV292LikeOriginal(
            C2BuildingMdInfoLikeOriginal md,
            float scale,
            float nominalPerspectiveDistanceV292)
        {
            C2BuildingVisualProjectionContextV292LikeOriginal ctx = new C2BuildingVisualProjectionContextV292LikeOriginal();
            if (!C2BuildingsPseudoProjectionVisualV292LikeOriginal ||
                md == null ||
                scale <= 0.000001f ||
                nominalPerspectiveDistanceV292 <= 1.0f)
                return ctx;

            Vector3 dir = MapOriginalDirToUnity(Mathf.PI / 6.0f, 0.0f);
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            if (right.sqrMagnitude < 0.000001f)
                right = Vector3.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(dir, right);
            if (up.sqrMagnitude < 0.000001f)
                up = Vector3.up;
            up.Normalize();

            ctx.Enabled = true;
            ctx.Dir = dir.normalized;
            ctx.Right = right;
            ctx.Up = up;
            ctx.Distance = Mathf.Max(1.0f, nominalPerspectiveDistanceV292);
            ctx.PlaneFactor = Mathf.Clamp(md.PlaneFactor, 0.0f, 3.0f);

            if (md.Use3pAlign)
            {
                ctx.HasThreePointAffine = TryBuildThreePointProjectionAffineV292LikeOriginal(md, scale, ref ctx);
            }
            else
            {
                // COSSACKS2/MiniMap4X.cpp::DrawSpriteBuilding calls the base
                // GetPseudoProjectionTM for every non-ALIGN_WITH_3POINTS building.
                ctx.HasThreePointAffine = false;
            }

            return ctx;
        }

        private static bool TryBuildThreePointProjectionAffineV292LikeOriginal(
            C2BuildingMdInfoLikeOriginal md,
            float scale,
            ref C2BuildingVisualProjectionContextV292LikeOriginal ctx)
        {
            Vector3 a1 = AlignPointDrawSpaceV292LikeOriginal(md, md.AlignPt1x, md.AlignPt1y, md.AlignPt1z);
            Vector3 a2 = AlignPointDrawSpaceV292LikeOriginal(md, md.AlignPt2x, md.AlignPt2y, md.AlignPt2z);
            Vector3 a3 = AlignPointDrawSpaceV292LikeOriginal(md, md.AlignPt3x, md.AlignPt3y, md.AlignPt3z);

            bool ok = TryBuildProjectionAffineFromDrawPointsV292LikeOriginal(a1, a2, a3, scale, ref ctx);
            if (!ok)
                return false;

            // Keep the building floor where the already-correct LineSort/service
            // coordinate system expects it. The linear part still keeps multipart
            // sprites glued, but doors/LineSort near the lower LOCATION edge do not drift.
            Vector3 floorAnchor = SkewPtLikeOriginal(
                md.PicDx + md.PicLx * 0.5f,
                (md.PicDy + md.PicLy) * 2.0f,
                0.0f);
            Vector2 anchor = ProjectionPlaneCoordsV292LikeOriginal(
                OriginalDrawSpaceToUnityLocalLikeOriginal(floorAnchor, scale),
                ctx);
            ctx.A02 = anchor.x - (ctx.A00 * anchor.x + ctx.A01 * anchor.y);
            ctx.A12 = anchor.y - (ctx.A10 * anchor.x + ctx.A11 * anchor.y);
            return true;
        }

        private static bool TryBuildBoundsProjectionAffineV292LikeOriginal(
            C2BuildingMdInfoLikeOriginal md,
            float scale,
            ref C2BuildingVisualProjectionContextV292LikeOriginal ctx)
        {
            if (md == null || md.PicLx <= 0 || md.PicLy <= 0)
                return false;

            Vector3 a1 = SkewPtLikeOriginal(md.PicDx, md.PicDy * 2.0f, 0.0f);
            Vector3 a2 = SkewPtLikeOriginal(md.PicDx + md.PicLx, md.PicDy * 2.0f, 0.0f);
            Vector3 a3 = SkewPtLikeOriginal(md.PicDx, (md.PicDy + md.PicLy) * 2.0f, 0.0f);

            return TryBuildProjectionAffineFromDrawPointsV292LikeOriginal(a1, a2, a3, scale, ref ctx);
        }

        private static bool TryBuildProjectionAffineFromDrawPointsV292LikeOriginal(
            Vector3 a1,
            Vector3 a2,
            Vector3 a3,
            float scale,
            ref C2BuildingVisualProjectionContextV292LikeOriginal ctx)
        {
            Vector2 s1 = ProjectionPlaneCoordsV292LikeOriginal(OriginalDrawSpaceToUnityLocalLikeOriginal(a1, scale), ctx);
            Vector2 s2 = ProjectionPlaneCoordsV292LikeOriginal(OriginalDrawSpaceToUnityLocalLikeOriginal(a2, scale), ctx);
            Vector2 s3 = ProjectionPlaneCoordsV292LikeOriginal(OriginalDrawSpaceToUnityLocalLikeOriginal(a3, scale), ctx);

            Vector2 d1 = ProjectionPlaneCoordsV292LikeOriginal(PerspectiveProjectUnityLocalV292LikeOriginal(OriginalDrawSpaceToUnityLocalLikeOriginal(a1, scale), ctx), ctx);
            Vector2 d2 = ProjectionPlaneCoordsV292LikeOriginal(PerspectiveProjectUnityLocalV292LikeOriginal(OriginalDrawSpaceToUnityLocalLikeOriginal(a2, scale), ctx), ctx);
            Vector2 d3 = ProjectionPlaneCoordsV292LikeOriginal(PerspectiveProjectUnityLocalV292LikeOriginal(OriginalDrawSpaceToUnityLocalLikeOriginal(a3, scale), ctx), ctx);

            float det = s1.x * (s2.y - s3.y) + s2.x * (s3.y - s1.y) + s3.x * (s1.y - s2.y);
            if (Mathf.Abs(det) < 0.000001f)
                return false;

            ctx.A00 = (d1.x * (s2.y - s3.y) + d2.x * (s3.y - s1.y) + d3.x * (s1.y - s2.y)) / det;
            ctx.A01 = (d1.x * (s3.x - s2.x) + d2.x * (s1.x - s3.x) + d3.x * (s2.x - s1.x)) / det;
            ctx.A02 = (d1.x * (s2.x * s3.y - s3.x * s2.y) +
                       d2.x * (s3.x * s1.y - s1.x * s3.y) +
                       d3.x * (s1.x * s2.y - s2.x * s1.y)) / det;
            ctx.A10 = (d1.y * (s2.y - s3.y) + d2.y * (s3.y - s1.y) + d3.y * (s1.y - s2.y)) / det;
            ctx.A11 = (d1.y * (s3.x - s2.x) + d2.y * (s1.x - s3.x) + d3.y * (s2.x - s1.x)) / det;
            ctx.A12 = (d1.y * (s2.x * s3.y - s3.x * s2.y) +
                       d2.y * (s3.x * s1.y - s1.x * s3.y) +
                       d3.y * (s1.x * s2.y - s2.x * s1.y)) / det;

            if (!IsFiniteV292LikeOriginal(ctx.A00) || !IsFiniteV292LikeOriginal(ctx.A01) ||
                !IsFiniteV292LikeOriginal(ctx.A02) || !IsFiniteV292LikeOriginal(ctx.A10) ||
                !IsFiniteV292LikeOriginal(ctx.A11) || !IsFiniteV292LikeOriginal(ctx.A12))
                return false;

            float linearMagnitude = Mathf.Max(Mathf.Abs(ctx.A00), Mathf.Abs(ctx.A01), Mathf.Abs(ctx.A10), Mathf.Abs(ctx.A11));
            return linearMagnitude > 0.0001f && linearMagnitude < 4.0f;
        }

        private static Vector3 AlignPointDrawSpaceV292LikeOriginal(C2BuildingMdInfoLikeOriginal md, int x, int y, int z)
        {
            int dx = md != null ? md.PicDx : 0;
            int dy = md != null ? md.PicDy : 0;
            return SkewPtLikeOriginal(dx + x, (dy + y + z) * 2.0f, z);
        }

        private static Vector3 ProjectOriginalDrawSpaceToUnityLocalV292LikeOriginal(
            Vector3 original,
            float scale,
            C2BuildingVisualProjectionContextV292LikeOriginal ctx)
        {
            Vector3 local = OriginalDrawSpaceToUnityLocalLikeOriginal(original, scale);
            if (!ctx.Enabled)
                return local;

            if (ctx.HasThreePointAffine)
            {
                Vector2 s = ProjectionPlaneCoordsV292LikeOriginal(local, ctx);
                float x = ctx.A00 * s.x + ctx.A01 * s.y + ctx.A02;
                float y = ctx.A10 * s.x + ctx.A11 * s.y + ctx.A12;
                float depth = Vector3.Dot(local, ctx.Dir);
                return ctx.Right * x + ctx.Up * y + ctx.Dir * depth;
            }

            return PerspectiveProjectUnityLocalV292LikeOriginal(local, ctx);
        }

        private static Vector3 PerspectiveProjectUnityLocalV292LikeOriginal(
            Vector3 local,
            C2BuildingVisualProjectionContextV292LikeOriginal ctx)
        {
            float x = Vector3.Dot(local, ctx.Right);
            float y = Vector3.Dot(local, ctx.Up);
            float depth = Vector3.Dot(local, ctx.Dir);
            float projectedDepth = ctx.Distance + depth * ctx.PlaneFactor;
            if (projectedDepth < ctx.Distance * 0.1f)
                projectedDepth = ctx.Distance * 0.1f;

            float k = ctx.Distance / projectedDepth;
            return ctx.Right * (x * k) + ctx.Up * (y * k) + ctx.Dir * depth;
        }

        private static Vector2 ProjectionPlaneCoordsV292LikeOriginal(
            Vector3 local,
            C2BuildingVisualProjectionContextV292LikeOriginal ctx)
        {
            return new Vector2(Vector3.Dot(local, ctx.Right), Vector3.Dot(local, ctx.Up));
        }

        private static bool IsFiniteV292LikeOriginal(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }

        private void RegisterBuildingPseudoProjectionEntryLikeOriginal(
            Transform root,
            Mesh mesh,
            Vector3[][] originalDrawFrames,
            float scale,
            C2BuildingMdInfoLikeOriginal md,
            C2BuildingWorkFrameAnimatorV206LikeOriginal animator, C2BuildingLoadedPartLikeOriginal sourcePart = null)
        {
            if (root == null || mesh == null || originalDrawFrames == null || originalDrawFrames.Length == 0 || md == null)
                return;

            var entry = new C2BuildingPseudoProjectionEntryLikeOriginal
            {
                Root = root,
                Mesh = mesh,
                OriginalDrawFrames = originalDrawFrames,
                Scale = Mathf.Max(0.000001f, scale),
                Md = md,
                Animator = animator, SourcePart = sourcePart
            };
            _c2BuildingPseudoProjectionEntriesLikeOriginal.Add(entry);

            // V381: placement ghosts are rebuilt whenever their snapped map position changes.
            // The former path left every newly-created part on the nominal/approximate matrix
            // until the next Update. A moving ghost was therefore destroyed and recreated before
            // it ever received MiniMap4X::DrawSpriteBuilding's camera-dependent pseudo projection.
            // Apply the exact Cossacks II projection synchronously at registration time.
            Camera cam = _strictIsoCamera;
            if (cam != null)
            {
                int frame = animator != null ? animator.CurrentFrameLikeOriginal : 0;
                if (frame < 0)
                    frame = 0;
                frame %= originalDrawFrames.Length;
                Vector3[] original = originalDrawFrames[frame];
                if (original != null && original.Length == 4 &&
                    TryProjectDrawSpriteBuildingExactLikeOriginal(
                        cam,
                        root,
                        md,
                        entry.Scale,
                        original,
                        entry.Output, frame))
                {
                    mesh.vertices = entry.Output;
                    mesh.RecalculateBounds();
                    entry.LastFrame = frame;
                    entry.LastRootPosition = root.position;
                    entry.RootPositionValid = true;
                }
                else
                {
                    mesh.vertices = new Vector3[4];
                    mesh.RecalculateBounds();
                    entry.ProjectionCulled = true;
                }
            }
        }

        private void UpdateBuildingPseudoProjectionMeshesLikeOriginal()
        {
            Camera cam = _strictIsoCamera;
            if (cam == null || (_c2BuildingPseudoProjectionEntriesLikeOriginal.Count == 0 && _c2BuildingEffectProjectionEntries.Count == 0))
                return;

            bool cameraChanged = !_c2BuildingPseudoProjectionCameraValidLikeOriginal ||
                (_c2BuildingPseudoProjectionLastCameraPosLikeOriginal - cam.transform.position).sqrMagnitude > 0.000001f ||
                Mathf.Abs(Quaternion.Dot(_c2BuildingPseudoProjectionLastCameraRotLikeOriginal, cam.transform.rotation)) < 0.9999999f ||
                Mathf.Abs(_c2BuildingPseudoProjectionLastFovLikeOriginal - cam.fieldOfView) > 0.0001f ||
                _c2BuildingPseudoProjectionLastPixelWidthLikeOriginal != cam.pixelWidth ||
                _c2BuildingPseudoProjectionLastPixelHeightLikeOriginal != cam.pixelHeight ||
                _c2BuildingPseudoProjectionLastPixelRectLikeOriginal != cam.pixelRect ||
                _c2BuildingPseudoProjectionLastMatrixLikeOriginal != cam.projectionMatrix;

            if (cameraChanged)
            {
                _c2BuildingPseudoProjectionCameraValidLikeOriginal = true;
                _c2BuildingPseudoProjectionLastCameraPosLikeOriginal = cam.transform.position;
                _c2BuildingPseudoProjectionLastCameraRotLikeOriginal = cam.transform.rotation;
                _c2BuildingPseudoProjectionLastFovLikeOriginal = cam.fieldOfView;
                _c2BuildingPseudoProjectionLastPixelWidthLikeOriginal = cam.pixelWidth;
                _c2BuildingPseudoProjectionLastPixelHeightLikeOriginal = cam.pixelHeight;
                _c2BuildingPseudoProjectionLastPixelRectLikeOriginal = cam.pixelRect;
                _c2BuildingPseudoProjectionLastMatrixLikeOriginal = cam.projectionMatrix;
            }

            for (int i = _c2BuildingPseudoProjectionEntriesLikeOriginal.Count - 1; i >= 0; i--)
            {
                C2BuildingPseudoProjectionEntryLikeOriginal entry = _c2BuildingPseudoProjectionEntriesLikeOriginal[i];
                if (entry == null || entry.Root == null || entry.Mesh == null || entry.Md == null ||
                    entry.OriginalDrawFrames == null || entry.OriginalDrawFrames.Length == 0)
                {
                    _c2BuildingPseudoProjectionEntriesLikeOriginal.RemoveAt(i);
                    continue;
                }

                int frame = entry.Animator != null ? entry.Animator.CurrentFrameLikeOriginal : 0;
                if (frame < 0)
                    frame = 0;
                frame %= entry.OriginalDrawFrames.Length;

                bool rootMoved = !entry.RootPositionValid ||
                    (entry.LastRootPosition - entry.Root.position).sqrMagnitude > 0.000001f;
                if (!cameraChanged && !rootMoved && entry.LastFrame == frame)
                    continue;

                Vector3[] original = entry.OriginalDrawFrames[frame];
                if (original == null || original.Length != 4)
                    continue;

                if (TryProjectDrawSpriteBuildingExactLikeOriginal(
                    cam,
                    entry.Root,
                    entry.Md,
                    entry.Scale,
                    original,
                    entry.Output, frame))
                {
                    entry.Mesh.vertices = entry.Output;
                    entry.Mesh.RecalculateBounds();
                    entry.LastFrame = frame;
                    entry.LastRootPosition = entry.Root.position;
                    entry.RootPositionValid = true;
                    entry.ProjectionCulled = false;
                }
                else
                {
                    if (!entry.ProjectionCulled)
                    {
                        entry.Mesh.vertices = new Vector3[4];
                        entry.Mesh.RecalculateBounds();
                    }
                    entry.ProjectionCulled = true;
                    entry.LastFrame = frame;
                    entry.LastRootPosition = entry.Root.position;
                    entry.RootPositionValid = true;
                }
            }
            UpdateBuildingEffectAnchorsLikeOriginal(cam, cameraChanged);
            UpdateProjectedBuildingLineOverlaysLikeOriginal(cam);
        }

        // MiniMap4X::ShowFiresNearBuilding sends (PicDx+FireX,0,-PicDy-FireY)
        // through the SAME M4 as DrawSpriteBuilding, followed by screen-to-world.
        // Keep an emitter anchor separate from its animated child so smoke drift
        // cannot overwrite the camera-dependent building projection.
        private Transform CreateBuildingEffectAnchorLikeOriginal(
            Transform root, C2BuildingMdInfoLikeOriginal md, float drawX, float drawY)
        {
            var anchor = new GameObject("C2_BuildingEffectAnchor").transform;
            anchor.SetParent(root, false);
            Transform drawRoot = ResolveBuildingEffectDrawRootLikeOriginal(root, md);
            var entry = new C2BuildingEffectProjectionEntryLikeOriginal
            {
                Root = drawRoot, Anchor = anchor, Md = md,
                Scale = BuildingSpritePixelToWorldScaleV276LikeOriginal(WallOriginalXYUnitToWorldScaleV8LikeOriginal()),
                LastRootPosition = drawRoot.position
            };
            entry.Input[0] = new Vector3(drawX, 0.0f, -drawY);
            anchor.position = drawRoot.TransformPoint(OriginalDrawSpaceToUnityLocalLikeOriginal(entry.Input[0], entry.Scale));
            if (TryProjectDrawSpriteBuildingExactLikeOriginal(_strictIsoCamera, drawRoot, md,
                    entry.Scale, entry.Input, entry.Output))
                anchor.position = drawRoot.TransformPoint(entry.Output[0]);
            _c2BuildingEffectProjectionEntries.Add(entry);
            return anchor;
        }

        private Transform ResolveBuildingEffectDrawRootLikeOriginal(Transform host, C2BuildingMdInfoLikeOriginal md)
        {
            // Constructed buildings store gameplay on a site at the terrain
            // origin and render on a positioned child. M4 belongs to that child,
            // never to the site or a newly-created fire/smoke container.
            for (Transform owner = host; owner != null; owner = owner.parent)
            {
                for (int i = _c2BuildingPseudoProjectionEntriesLikeOriginal.Count - 1; i >= 0; i--)
                {
                    var entry = _c2BuildingPseudoProjectionEntriesLikeOriginal[i];
                    if (entry == null || entry.Root == null || !entry.Root.gameObject.activeInHierarchy || entry.Md != md) continue;
                    if (entry.Root == owner || entry.Root.IsChildOf(owner)) return entry.Root;
                }
                if (owner.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>() != null ||
                    owner.GetComponent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>() != null) break;
            }
            return host;
        }

        private void UpdateBuildingEffectAnchorsLikeOriginal(Camera cam, bool cameraChanged)
        {
            for (int i = _c2BuildingEffectProjectionEntries.Count - 1; i >= 0; i--)
            {
                var entry = _c2BuildingEffectProjectionEntries[i];
                if (entry.Root == null || entry.Anchor == null || entry.Anchor.childCount == 0)
                {
                    if (entry.Anchor != null) Destroy(entry.Anchor.gameObject);
                    _c2BuildingEffectProjectionEntries.RemoveAt(i);
                    continue;
                }
                if (!cameraChanged && (entry.LastRootPosition - entry.Root.position).sqrMagnitude <= 0.000001f)
                    continue;
                if (TryProjectDrawSpriteBuildingExactLikeOriginal(cam, entry.Root, entry.Md,
                        entry.Scale, entry.Input, entry.Output))
                {
                    entry.Anchor.position = entry.Root.TransformPoint(entry.Output[0]);
                    entry.LastRootPosition = entry.Root.position;
                }
            }
        }

        private static bool TryProjectDrawSpriteBuildingExactLikeOriginal(
            Camera cam,
            Transform root,
            C2BuildingMdInfoLikeOriginal md,
            float scale,
            Vector3[] originalDraw,
            Vector3[] output, int frame = -1)
        {
            if (cam == null || root == null || md == null || originalDraw == null || originalDraw.Length == 0 ||
                output == null || output.Length != originalDraw.Length || scale <= 0.000001f)
                return false;

            var cameraSample = BuildingProjectionCameraLikeOriginal(cam, root.position, scale);
            if (!cameraSample.IntersectsFrustum(BuildingProjectionRadiusLikeOriginal(md, scale))) return false;
            float pseudoPlaneFactor = md.Use3pAlign ? 0.0f : md.PlaneFactor;
            // Parallel rays in the optional comparison mode have a common
            // direction. Using camera-to-object there would reintroduce shear.
            Vector3 delta = cam.orthographic ? -cam.transform.forward : cam.transform.position - root.position;
            C2BuildingMatrix4LikeOriginal pseudo = BuildBuildingPseudoProjectionMatrixLikeOriginal(
                cameraSample, new Vector3(delta.x, -delta.z, delta.y), pseudoPlaneFactor, out bool matrixOk);
            if (!matrixOk)
            {
                LogBuildingProjectionFailureLikeOriginal("invalid_camera_basis", cam, root, md, frame, Vector3.zero, cameraSample.Origin);
                return false;
            }

            bool useThreePointAffine = false;
            float a00 = 1.0f, a01 = 0.0f, a02 = 0.0f;
            float a10 = 0.0f, a11 = 1.0f, a12 = 0.0f;
            if (md.Use3pAlign)
            {
                Vector3 v1 = AlignPointDrawSpaceV292LikeOriginal(md, md.AlignPt1x, md.AlignPt1y, md.AlignPt1z);
                Vector3 v2 = AlignPointDrawSpaceV292LikeOriginal(md, md.AlignPt2x, md.AlignPt2y, md.AlignPt2z);
                Vector3 v3 = AlignPointDrawSpaceV292LikeOriginal(md, md.AlignPt3x, md.AlignPt3y, md.AlignPt3z);

                // Active COSSACKS2/MiniMap4X.cpp overload: the rear point is V3.
                if (md.AlignPt1y < md.AlignPt2y && md.AlignPt1y < md.AlignPt3y)
                { Vector3 swap = v3; v3 = v1; v1 = swap; }
                else if (md.AlignPt2y < md.AlignPt1y && md.AlignPt2y < md.AlignPt3y)
                { Vector3 swap = v2; v2 = v3; v3 = swap; }
                if (Mathf.Abs(v2.x - v1.x) > 0.000001f)
                {
                    float fraction = (v3.x - v1.x) / (v2.x - v1.x);
                    Vector3 v4 = v1 * (1.0f - fraction) + v2 * fraction;
                    Vector3 q1 = pseudo.TransformPoint(v1);
                    Vector3 q2 = pseudo.TransformPoint(v2);
                    Vector3 q3 = pseudo.TransformPoint(v3);
                    Vector3 q4 = pseudo.TransformPoint(v4);
                    if (cameraSample.TryProject(v1, out Vector3 s1) &&
                        cameraSample.TryProject(v2, out Vector3 s2) &&
                        cameraSample.TryProject(v3, out Vector3 s3))
                        useThreePointAffine = TrySolveCossacks2ThreePointScreenV377LikeOriginal(
                            q1, q2, q3, q4, s1, s2, s3,
                            out a00, out a01, out a02, out a10, out a11, out a12);
                }
            }

            Rect pixelRect = cam.pixelRect;
            // COSSACKS2 preserves fullTM's projected depth from LINESORT.
            // The shader sets w=1 after projection: affine UVs with per-corner Z.

            for (int i = 0; i < originalDraw.Length; i++)
            {
                Vector3 screenOriginal = pseudo.TransformPoint(originalDraw[i]);
                if (useThreePointAffine)
                {
                    float sx = screenOriginal.x;
                    float sy = screenOriginal.y;
                    screenOriginal.x = a00 * sx + a01 * sy + a02;
                    screenOriginal.y = a10 * sx + a11 * sy + a12;
                }

                float unityScreenY = pixelRect.yMin + pixelRect.yMax - screenOriginal.y;
                // Reciprocal eye depth (perspective) or eye depth (orthographic)
                // is affine to hardware NDC depth. Keep the LINESORT plane.
                if (!IsFiniteV292LikeOriginal(screenOriginal.z) || screenOriginal.z <= 0.0f)
                {
                    LogBuildingProjectionFailureLikeOriginal("invalid_reciprocal_depth", cam, root, md, frame, originalDraw[i], screenOriginal);
                    return false;
                }
                if (!IsFiniteV292LikeOriginal(screenOriginal.x) || !IsFiniteV292LikeOriginal(screenOriginal.y) ||
                    Mathf.Abs(screenOriginal.x - pixelRect.center.x) > pixelRect.width * 64.0f ||
                    Mathf.Abs(screenOriginal.y - pixelRect.center.y) > pixelRect.height * 64.0f)
                {
                    LogBuildingProjectionFailureLikeOriginal("invalid_projected_vertex", cam, root, md, frame, originalDraw[i], screenOriginal);
                    return false;
                }
                float cameraDepth = cameraSample.ToEyeDepth(screenOriginal.z);
                if (!IsFiniteV292LikeOriginal(cameraDepth))
                {
                    LogBuildingProjectionFailureLikeOriginal("invalid_eye_depth", cam, root, md, frame, originalDraw[i], screenOriginal);
                    return false;
                }
                Vector3 projectedWorld = cam.ScreenToWorldPoint(
                    new Vector3(screenOriginal.x, unityScreenY, cameraDepth));
                output[i] = root.InverseTransformPoint(projectedWorld);
            }

            return true;
        }

        internal static C2BuildingMatrix4LikeOriginal BuildBuildingPseudoProjectionMatrixLikeOriginal(
            C2BuildingProjectionCameraLikeOriginal cameraSample, Vector3 objDir, float planeFactor, out bool ok)
        {
            ok = cameraSample.TryScreenBasis(out C2BuildingMatrix4LikeOriginal screen) && Mathf.Abs(screen.e00) > 0.000001f;
            if (!ok) return C2BuildingMatrix4LikeOriginal.Identity();
            float scaleY = 1.0f + (2.0f * screen.e11 / screen.e00 - 1.0f) * planeFactor;
            if (objDir.sqrMagnitude < 0.000001f) { ok = false; return C2BuildingMatrix4LikeOriginal.Identity(); }
            objDir.Normalize();
            C2BuildingMatrix4LikeOriginal m42 = C2BuildingMatrix4LikeOriginal.Identity();
            m42.e00 = 1.0f; m42.e01 = 0.0f; m42.e02 = 0.0f;
            m42.e10 = objDir.x; m42.e11 = objDir.y; m42.e12 = objDir.z;
            m42.e20 = 0.0f; m42.e21 = -0.5f; m42.e22 = C2BuildingsCosPiOver6LikeOriginal * scaleY;
            C2BuildingMatrix4LikeOriginal m41 = C2BuildingMatrix4LikeOriginal.Identity();
            m41.e11 = C2BuildingsCosPiOver6LikeOriginal; m41.e12 = -0.5f;
            m41.e21 = 0.5f; m41.e22 = C2BuildingsCosPiOver6LikeOriginal;
            return (m41 * m42) * screen;
        }

        private static bool TrySolveCossacks2ThreePointScreenV377LikeOriginal(
            Vector3 q1, Vector3 q2, Vector3 q3, Vector3 q4,
            Vector3 s1, Vector3 s2, Vector3 s3,
            out float a00, out float a01, out float a02,
            out float a10, out float a11, out float a12)
        {
            a00 = a11 = 1.0f; a01 = a02 = a10 = a12 = 0.0f;
            float dx = q3.x - q4.x, dy = q3.y - q4.y;
            bool horizontal = Mathf.Abs(dx) > Mathf.Abs(dy);
            float divisor = horizontal ? dx : dy;
            if (Mathf.Abs(divisor) < 0.000001f) return false;
            float slope = horizontal ? -dy / dx : -dx / dy;
            float u1 = horizontal ? q1.x * slope + q1.y : q1.x + q1.y * slope;
            float u2 = horizontal ? q2.x * slope + q2.y : q2.x + q2.y * slope;
            if (Mathf.Abs(u2 - u1) < 0.000001f) return false;
            float k = (s2.x - s1.x) / (u2 - u1);
            float kx = horizontal ? slope * k : k;
            float ky = horizontal ? k : slope * k;
            float origin = s1.x - k * u1;
            float correctedX3 = q3.x * kx + q3.y * ky + origin;
            if (!horizontal)
            {
                if (Mathf.Abs(s2.x - s1.x) < 0.000001f) return false;
                s3.y -= (s2.y - s1.y) / (s2.x - s1.x) * (correctedX3 - s3.x);
            }
            // The original constrains X along V3-V4, rather than fitting all
            // three projected X values. A generic affine fit shears vertical walls.
            s3.x = correctedX3;
            if (!TrySolveScreenAffineLikeOriginal(q1, q2, q3, s1, s2, s3,
                out a00, out a01, out a02, out a10, out a11, out a12)) return false;
            float maxScale = horizontal ? 3.0f : 2.0f;
            return a00 >= 0.0f && a00 <= maxScale && a11 >= 0.0f && a11 <= maxScale;
        }

        private static bool TrySolveScreenAffineLikeOriginal(
            Vector3 q1, Vector3 q2, Vector3 q3, Vector3 s1, Vector3 s2, Vector3 s3,
            out float a00, out float a01, out float a02, out float a10, out float a11, out float a12)
        {
            a00 = a11 = 1; a01 = a02 = a10 = a12 = 0;
            // Same C2 affine equations, translated to the first point. Computing
            // the determinant from absolute pixel coordinates cancels large terms
            // on distant map objects and can invent a nonzero singular determinant.
            double ux = (double)q2.x-q1.x, uy = (double)q2.y-q1.y;
            double vx = (double)q3.x-q1.x, vy = (double)q3.y-q1.y;
            double det = ux*vy-uy*vx;
            double norm = Math.Sqrt((ux*ux+uy*uy)*(vx*vx+vy*vy));
            if (double.IsNaN(det) || norm == 0 || Math.Abs(det) <= norm*1e-8) return false;
            double sx = (double)s2.x-s1.x, tx = (double)s3.x-s1.x;
            double sy = (double)s2.y-s1.y, ty = (double)s3.y-s1.y;
            double xx = (sx*vy-tx*uy)/det, xy = (tx*ux-sx*vx)/det;
            double yx = (sy*vy-ty*uy)/det, yy = (ty*ux-sy*vx)/det;
            a00=(float)xx; a01=(float)xy; a02=(float)(s1.x-xx*q1.x-xy*q1.y);
            a10=(float)yx; a11=(float)yy; a12=(float)(s1.y-yx*q1.x-yy*q1.y);
            return IsFiniteV292LikeOriginal(a00) && IsFiniteV292LikeOriginal(a01) && IsFiniteV292LikeOriginal(a02) &&
                IsFiniteV292LikeOriginal(a10) && IsFiniteV292LikeOriginal(a11) && IsFiniteV292LikeOriginal(a12);
        }

        private static void FramePivotLikeOriginal(C2BuildingMdInfoLikeOriginal md, C2BuildingAnimFrameLikeOriginal frame, out int dx, out int dy)
        {
            // Original mdParser.cpp assigns NewFrame.dx/dy from PicDx/PicDy for BUILDING records.
            // USERLC dx/dy is used for non-building animations only. Keep that split here so
            // DrawSpriteBuilding, preview/check zones and LINESORT share the same LOCATION anchor.
            dx = md != null ? md.Dx : 0;
            dy = md != null ? md.Dy : 0;
            if (md == null)
                return;

            if (md.Building && (md.PicDx != 0 || md.PicDy != 0 || md.PicLx > 0 || md.PicLy > 0))
            {
                dx = md.PicDx;
                dy = md.PicDy;
                return;
            }

            if (md.RlcDx.TryGetValue(frame.FileRef, out int rdx)) dx = rdx;
            if (md.RlcDy.TryGetValue(frame.FileRef, out int rdy)) dy = rdy;
        }

        private static int BuildingSortOrderLikeOriginal(C2Building3InuRecordLikeOriginal r, C2BuildingLoadedPartLikeOriginal part, int partIndex, bool runtimeConstructedBuilding)
        {
            int mapY = r.RealY >> 4;
            int tie = Mathf.Clamp(partIndex, 0, 31);
            if (part != null && part.HasLineSort)
            {
                C2BuildingLineSortLikeOriginal li = part.LineSort;
                if (li.IsGround)
                    return Mathf.Clamp(6000 + mapY - 64 + tie, -30000, 30000);
                if (li.IsTop)
                    return Mathf.Clamp(6000 + mapY + 1024 + tie, -30000, 30000);

                if (runtimeConstructedBuilding)
                {
                    // Static only: do not let LINESORT parts react to units. Runtime-built
                    // buildings stay in the same RealY band as units; foreground/background is
                    // decided by the shared RealY sort key, not by scanning nearby unit feet.
                    return Mathf.Clamp(6000 + mapY + 32 + tie, -30000, 30000);
                }

                int lineY = (li.Y1 + li.Y2) >> 1;
                return Mathf.Clamp(6000 + mapY + (lineY >> 2) + 96 + tie, -30000, 30000);
            }

            return Mathf.Clamp(6000 + mapY + (runtimeConstructedBuilding ? 32 : 256) + tie, -30000, 30000);
        }

        private static bool TryLoadPreparedBuildingSpriteDiskCacheV232LikeOriginal(
            string sourcePath,
            int exactFrame,
            string package,
            string key,
            out Texture2D texture,
            out string source)
        {
            texture = null;
            source = string.Empty;
            if (!C2BuildingsPreparedSpriteDiskCacheV232LikeOriginal)
                return false;

            string cachePath = GetPreparedBuildingSpriteDiskCachePathV232LikeOriginal(sourcePath, exactFrame, package, key);
            if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath))
            {
                s_C2BuildingPreparedSpriteDiskMissesV232LikeOriginal++;
                return false;
            }

            try
            {
                using (FileStream fs = File.Open(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    int magic = br.ReadInt32();
                    int version = br.ReadInt32();
                    if (magic != C2BuildingsPreparedSpriteDiskCacheMagicV232LikeOriginal ||
                        version != C2BuildingsPreparedSpriteDiskCacheVersionV232LikeOriginal)
                    {
                        s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal++;
                        return false;
                    }

                    int originalW = br.ReadInt32();
                    int originalH = br.ReadInt32();
                    int minX = br.ReadInt32();
                    int minY = br.ReadInt32();
                    int cropW = br.ReadInt32();
                    int cropH = br.ReadInt32();
                    long visiblePixels = br.ReadInt64();
                    int payloadLen = br.ReadInt32();
                    if (cropW <= 0 || cropH <= 0 || payloadLen <= 0 || payloadLen != cropW * cropH * 4 || payloadLen > 256 * 1024 * 1024)
                    {
                        s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal++;
                        return false;
                    }

                    byte[] rgba = br.ReadBytes(payloadLen);
                    if (rgba == null || rgba.Length != payloadLen)
                    {
                        s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal++;
                        return false;
                    }

                    Texture2D tex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false, false);
                    string nicePackage = SanitizeBuildingCachePartV232LikeOriginal(package);
                    tex.name = "C2_BLD_CACHE_V232_" + nicePackage + "_frame_" + exactFrame.ToString(CultureInfo.InvariantCulture) + "_TightCropV229";
                    tex.LoadRawTextureData(rgba);

                    var ab = new C2BuildingAlphaBoundsDiagV228LikeOriginal();
                    ab.OriginalWidth = originalW;
                    ab.OriginalHeight = originalH;
                    ab.MinX = minX;
                    ab.MinY = minY;
                    ab.CropWidth = cropW;
                    ab.CropHeight = cropH;
                    ab.MaxX = minX + cropW - 1;
                    ab.MaxY = minY + cropH - 1;
                    ab.OriginalPixels = (long)Math.Max(1, originalW) * (long)Math.Max(1, originalH);
                    ab.CropPixels = (long)cropW * (long)cropH;
                    ab.VisiblePixels = Math.Max(0L, visiblePixels);
                    ab.HasVisiblePixels = true;
                    ab.TightCroppedV229 = true;
                    s_C2BuildingAlphaBoundsByTextureIdV228LikeOriginal[tex.GetEntityId()] = ab;

                    tex.Apply(false, true);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;

                    texture = tex;
                    s_C2BuildingPreparedSpriteDiskHitsV232LikeOriginal++;
                    source = "disk_cache_v232:" + cachePath;
                    return true;
                }
            }
            catch (Exception ex)
            {
                s_C2BuildingPreparedSpriteDiskLoadFailsV232LikeOriginal++;
                source = "disk_cache_v232_load_fail:" + ex.GetType().Name;
                return false;
            }
        }

        private static void SavePreparedBuildingSpriteDiskCacheV232LikeOriginal(
            string sourcePath,
            int exactFrame,
            string package,
            string key,
            byte[] rgba,
            int cropW,
            int cropH,
            int originalW,
            int originalH,
            C2BuildingAlphaBoundsDiagV228LikeOriginal ab)
        {
            if (!C2BuildingsPreparedSpriteDiskCacheV232LikeOriginal || rgba == null || cropW <= 0 || cropH <= 0)
                return;

            string cachePath = GetPreparedBuildingSpriteDiskCachePathV232LikeOriginal(sourcePath, exactFrame, package, key);
            if (string.IsNullOrEmpty(cachePath))
                return;

            try
            {
                string dir = Path.GetDirectoryName(cachePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string tmp = cachePath + ".tmp";
                using (FileStream fs = File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(C2BuildingsPreparedSpriteDiskCacheMagicV232LikeOriginal);
                    bw.Write(C2BuildingsPreparedSpriteDiskCacheVersionV232LikeOriginal);
                    bw.Write(originalW);
                    bw.Write(originalH);
                    bw.Write(ab != null ? ab.MinX : 0);
                    bw.Write(ab != null ? ab.MinY : 0);
                    bw.Write(cropW);
                    bw.Write(cropH);
                    bw.Write(ab != null ? ab.VisiblePixels : 0L);
                    bw.Write(rgba.Length);
                    bw.Write(rgba);
                }

                if (File.Exists(cachePath)) File.Delete(cachePath);
                File.Move(tmp, cachePath);
                s_C2BuildingPreparedSpriteDiskWritesV232LikeOriginal++;
            }
            catch
            {
                s_C2BuildingPreparedSpriteDiskWriteFailsV232LikeOriginal++;
            }
        }

        private static string GetPreparedBuildingSpriteDiskCachePathV232LikeOriginal(string sourcePath, int exactFrame, string package, string key)
        {
            try
            {
                string root = GetC2CacheRootV232LikeOriginal();
                if (string.IsNullOrEmpty(root)) return string.Empty;
                string dir = Path.Combine(root, C2BuildingsPreparedSpriteDiskCacheFolderV232LikeOriginal);

                long len = 0L;
                long ticks = 0L;
                if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
                {
                    FileInfo fi = new FileInfo(sourcePath);
                    len = fi.Length;
                    ticks = fi.LastWriteTimeUtc.Ticks;
                }

                string signature = (sourcePath ?? string.Empty) + "|" + (package ?? string.Empty) + "|" + (key ?? string.Empty) + "|frame=" + exactFrame.ToString(CultureInfo.InvariantCulture) +
                                   "|len=" + len.ToString(CultureInfo.InvariantCulture) + "|ticks=" + ticks.ToString(CultureInfo.InvariantCulture) +
                                   "|decode=V208|layer=V205|crop=V229|cache=V232|alpha=" + C2BuildingsAlphaBoundsThresholdV228LikeOriginal.ToString(CultureInfo.InvariantCulture);
                string hash = HashStringFnv1a64V232LikeOriginal(signature);
                string baseName = SanitizeBuildingCachePartV232LikeOriginal(package);
                if (string.IsNullOrEmpty(baseName)) baseName = SanitizeBuildingCachePartV232LikeOriginal(Path.GetFileNameWithoutExtension(sourcePath));
                if (string.IsNullOrEmpty(baseName)) baseName = "building";
                string file = baseName + "_f" + exactFrame.ToString(CultureInfo.InvariantCulture) + "_" + hash + ".c2bldsprite";
                return Path.Combine(dir, file);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetC2CacheRootV232LikeOriginal()
        {
            try
            {
                string assets = Application.dataPath;
                if (string.IsNullOrEmpty(assets)) return string.Empty;
                return Path.GetFullPath(Path.Combine(assets, "..", "C2Cache"));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SanitizeBuildingCachePartV232LikeOriginal(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        private static string HashStringFnv1a64V232LikeOriginal(string value)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash ^= value[i];
                        hash *= 1099511628211UL;
                    }
                }
                return hash.ToString("X16", CultureInfo.InvariantCulture);
            }
        }

        private static Texture2D PrepareBuildingLoadedTextureV205LikeOriginal(Texture2D tex, string sourcePathV232, int exactFrameV232, string packageV232, string keyV232)
        {
            if (tex == null)
                return null;

            try
            {
                int w = tex.width;
                int h = tex.height;
                if (w <= 0 || h <= 0)
                    return tex;

                byte[] raw = tex.GetRawTextureData();
                if (raw == null || raw.Length < w * h * 4)
                    return tex;

                byte[] rgba = new byte[w * h * 4];
                Buffer.BlockCopy(raw, 0, rgba, 0, rgba.Length);

                ApplyBuildingLayerCompositeV205LikeOriginal(rgba, w, h);

                C2BuildingAlphaBoundsDiagV228LikeOriginal alphaBoundsV228 = AnalyzeBuildingAlphaBoundsV228LikeOriginal(rgba, w, h);

                byte[] uploadRgba = rgba;
                int uploadW = w;
                int uploadH = h;
                bool tightCroppedV229 = ShouldTightCropBuildingAlphaV229LikeOriginal(alphaBoundsV228);
                if (tightCroppedV229)
                {
                    uploadRgba = CropBuildingRgbaToAlphaBoundsV229LikeOriginal(rgba, w, h, alphaBoundsV228);
                    uploadW = Mathf.Max(1, alphaBoundsV228.CropWidth);
                    uploadH = Mathf.Max(1, alphaBoundsV228.CropHeight);
                    alphaBoundsV228.TightCroppedV229 = true;

                    s_C2BuildingTightCropCreatedV229LikeOriginal++;
                    s_C2BuildingTightCropOriginalPixelsV229LikeOriginal += alphaBoundsV228.OriginalPixels;
                    s_C2BuildingTightCropKeptPixelsV229LikeOriginal += alphaBoundsV228.CropPixels;
                }

                // Important: false = sRGB color texture. The shared Melinoja wall loader creates
                // linear textures for wall objects; building sprites must be sampled as color sprites,
                // like the old working settlement path and the viewer/unit cache.
                Texture2D srgb = new Texture2D(uploadW, uploadH, TextureFormat.RGBA32, false, false);
                srgb.name = tex.name + "_BLD_srgb_layerV205" + (tightCroppedV229 ? "_TightCropV229" : string.Empty);
                srgb.LoadRawTextureData(uploadRgba);

                if (alphaBoundsV228 != null)
                    s_C2BuildingAlphaBoundsByTextureIdV228LikeOriginal[srgb.GetEntityId()] = alphaBoundsV228;

                SavePreparedBuildingSpriteDiskCacheV232LikeOriginal(sourcePathV232, exactFrameV232, packageV232, keyV232, uploadRgba, uploadW, uploadH, w, h, alphaBoundsV228);

                // V227: building sprites are final GPU sprites after alpha/color preparation.
                // Keep no CPU-side readable copy. The old code used Apply(false,false), so every
                // cached frame kept a CPU copy; it also left the temporary decoder texture alive.
                srgb.Apply(false, true);
                srgb.filterMode = FilterMode.Point;
                srgb.wrapMode = TextureWrapMode.Clamp;

                // V227: the input texture was only a temporary decode buffer used to read RGBA
                // and build the final sRGB/layer-composited texture. Destroy it to avoid keeping
                // a duplicate Unity native texture for every building frame.
                if (tex != null && tex != srgb)
                {
                    try
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            UnityEngine.Object.DestroyImmediate(tex);
                        else
                            UnityEngine.Object.Destroy(tex);
#else
                        UnityEngine.Object.Destroy(tex);
#endif
                    }
                    catch
                    {
                    }
                }

                return srgb;
            }
            catch
            {
                return tex;
            }
        }

        private static byte CompositeBuildingAlphaByteV205LikeOriginal(byte a)
        {
            if (!C2BuildingsLayerCompositeV205LikeOriginal || a == 0 || a == 255)
                return a;

            float bottomA = a / 255.0f;
            float topOpacity = Mathf.Clamp01(C2BuildingsLayerCompositeTopOpacityV205LikeOriginal);
            float topA = bottomA * topOpacity;

            // Normal SourceOver of the same sprite over itself:
            // outA = topA + bottomA * (1 - topA)
            float outA = topA + bottomA * (1.0f - topA);
            int ia = Mathf.Clamp(Mathf.RoundToInt(outA * 255.0f), 0, 255);
            return (byte)ia;
        }

        private static void ApplyBuildingLayerCompositeV205LikeOriginal(byte[] rgba, int width, int height)
        {
            if (!C2BuildingsLayerCompositeV205LikeOriginal || rgba == null || rgba.Length < 4)
                return;

            int pixelCount = Mathf.Min(width * height, rgba.Length / 4);
            for (int p = 0, i = 0; p < pixelCount; p++, i += 4)
                rgba[i + 3] = CompositeBuildingAlphaByteV205LikeOriginal(rgba[i + 3]);
        }

        private static Material GetBuildingMaterialLikeOriginal(Texture2D tex)
        {
            Texture2D main = tex != null ? tex : Texture2D.whiteTexture;
            EntityId key = main.GetEntityId();
            if (s_C2BuildingMaterialCacheLikeOriginal.TryGetValue(key, out Material cached) && cached != null)
            {
                ApplyBuildingMaterialDepthSettingsV262LikeOriginal(cached, main);
                return cached;
            }

            // V205: do not use WallObjectSpriteV31ExactCutout for buildings here.
            // That shader has Blend Off in the current project, so semi-transparent G16 pixels
            // become hard opaque black/grey blocks. Buildings need alpha-test + alpha-blend.
            Shader shader = Shader.Find("Cossacks2Bridge/SettlementBuildingSpriteV205BlendLikeOriginal");
            if (shader == null) shader = Shader.Find("Cossacks2Bridge/SettlementBuildingSpriteV23LikeOriginal");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Transparent Cutout");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "C2_BuildingSprite_DrawSpriteBuilding_" + main.name;
            ApplyBuildingMaterialDepthSettingsV262LikeOriginal(mat, main);

            if (!s_C2DepthV233LoggedLikeOriginal)
            {
                s_C2DepthV233LoggedLikeOriginal = true;
                Debug.Log("[C2:DEPTH V275] roadsQueue=3600 roadZTest=LEqual roadOffset=-1,-1 walsQueue=" +
                          C2WallObjectsV18RenderQueueLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " walsZTest=LEqual walsZWrite=Off buildingQueue=" +
                          C2BuildingsRenderQueueLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " buildingDepthQueue=" +
                          C2SpriteDepthPrepassLikeOriginal.DepthRenderQueue.ToString(CultureInfo.InvariantCulture) +
                          " buildingZTest=" +
                          (C2SpriteDepthOverlayCameraLikeOriginal ? "LEqual_sprite_overlay_camera_after_global_cutout_prepass" : (C2SpriteDepthPrepassV1LikeOriginal ? "LEqual_after_cutout_prepass" : (C2BuildingsRespectTerrainDepthV264LikeOriginal ? "LEqual" : "Always"))) +
                          " buildingZWrite=" +
                          (C2SpriteDepthOverlayCameraLikeOriginal && !C2SpriteDepthPrepassV1LikeOriginal ? "On" : "Off") +
                          " buildingDepthZWrite=" +
                          (C2SpriteDepthOverlayCameraLikeOriginal || C2SpriteDepthPrepassV1LikeOriginal ? "On" : "Off") +
                          " spriteDepthClearBeforeAlpha=" +
                          (C2SpriteDepthOverlayCameraLikeOriginal ? "separate_overlay_camera_depth_only_prepass_queue" : "disabled_after_urp_depth_clip") +
                          " unitDepthWrites=" +
                          (C2SpriteDepthOverlayCameraLikeOriginal || C2SpriteDepthPrepassV1LikeOriginal ? "On" : "Off") +
                          " buildingGroundDepthWrite=opaque_pixels_only" +
                          " buildingColorAlpha=original_blend buildingDepthAlphaCutoff=" +
                          C2BuildingsDepthAlphaCutoffLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) +
                          " uiSelectionExitpoint=untouched contract=V275_SPRITE_DEPTH_PREPASS_COLOR_SOFT_SHADOWS_LINESORT_NO_UNIT_SEETHROUGH");
            }

            s_C2BuildingMaterialCacheLikeOriginal[key] = mat;
            return mat;
        }

        private static Material GetBuildingDepthMaterialLikeOriginal(Texture2D tex, bool ground = false)
        {
            Texture2D main = tex != null ? tex : Texture2D.whiteTexture;
            var key = (main.GetEntityId(), ground);
            if (s_C2BuildingDepthMaterialCacheLikeOriginal.TryGetValue(key, out Material cached) && cached != null)
            {
                C2SpriteDepthPrepassLikeOriginal.ConfigureDepthMaterialLikeOriginal(cached, main, C2BuildingsDepthAlphaCutoffLikeOriginal);
                if (cached.HasProperty("_C2ScreenAffine")) cached.SetFloat("_C2ScreenAffine", 1.0f);
                if (cached.HasProperty("_C2IgnoreDarkShadowPixels")) cached.SetFloat("_C2IgnoreDarkShadowPixels", ground ? 1.0f : 0.0f);
                return cached;
            }

            Material mat = C2SpriteDepthPrepassLikeOriginal.CreateDepthMaterialLikeOriginal(
                "C2_BuildingSprite_DepthCutout_" + main.name,
                main,
                C2BuildingsDepthAlphaCutoffLikeOriginal);
            if (mat != null && mat.HasProperty("_C2ScreenAffine")) mat.SetFloat("_C2ScreenAffine", 1.0f);
            if (mat != null && mat.HasProperty("_C2IgnoreDarkShadowPixels")) mat.SetFloat("_C2IgnoreDarkShadowPixels", ground ? 1.0f : 0.0f);
            s_C2BuildingDepthMaterialCacheLikeOriginal[key] = mat;
            return mat;
        }

        private static void ApplyBuildingMaterialDepthSettingsV262LikeOriginal(Material mat, Texture2D main)
        {
            if (mat == null)
                return;

            mat.mainTexture = main != null ? main : Texture2D.whiteTexture;
            mat.renderQueue = C2BuildingsRenderQueueLikeOriginal;
            if (mat.HasProperty("_C2ScreenAffine")) mat.SetFloat("_C2ScreenAffine", 1.0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mat.mainTexture);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mat.mainTexture);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", C2BuildingsAlphaCutoffLikeOriginal);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", C2BuildingsAlphaCutoffLikeOriginal);
            if (mat.HasProperty("_ForceSolidAlpha")) mat.SetFloat("_ForceSolidAlpha", 0.0f);
            if (mat.HasProperty("_SolidAlphaThreshold")) mat.SetFloat("_SolidAlphaThreshold", 0.55f);
            int colorZTest = C2SpriteDepthOverlayCameraLikeOriginal || C2SpriteDepthPrepassV1LikeOriginal
                ? (int)CompareFunction.LessEqual
                : (C2BuildingsRespectTerrainDepthV264LikeOriginal ? (int)CompareFunction.LessEqual : (int)CompareFunction.Always);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", C2SpriteDepthOverlayCameraLikeOriginal && !C2SpriteDepthPrepassV1LikeOriginal ? 1 : 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", colorZTest);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        }

        private static int ParseLineSortTokensLikeOriginal(List<C2BuildingLineSortLikeOriginal> target, string[] tokens, int start, int expected)
        {
            if (target == null || tokens == null)
                return 0;

            int added = 0;
            int p = Mathf.Max(0, start);
            while (p < tokens.Length && target.Count < expected)
            {
                string cmd = (tokens[p] ?? string.Empty).Trim().ToUpperInvariant();
                if (cmd == "GROUND")
                {
                    target.Add(new C2BuildingLineSortLikeOriginal(C2BuildingsAlignGroundLikeOriginal, C2BuildingsAlignGroundLikeOriginal, C2BuildingsAlignGroundLikeOriginal, C2BuildingsAlignGroundLikeOriginal));
                    added++;
                    p++;
                }
                else if (cmd == "TOP" || cmd == "TOPMOST")
                {
                    target.Add(new C2BuildingLineSortLikeOriginal(C2BuildingsAlignTopmostLikeOriginal, C2BuildingsAlignTopmostLikeOriginal, C2BuildingsAlignTopmostLikeOriginal, C2BuildingsAlignTopmostLikeOriginal));
                    added++;
                    p++;
                }
                else if (cmd == "POINT" && p + 2 < tokens.Length)
                {
                    int x = ToIntLikeOriginal(tokens[p + 1]);
                    int y = ToIntLikeOriginal(tokens[p + 2]);
                    target.Add(new C2BuildingLineSortLikeOriginal(x, y, x, y));
                    added++;
                    p += 3;
                }
                else if (cmd == "LINE" && p + 4 < tokens.Length)
                {
                    target.Add(new C2BuildingLineSortLikeOriginal(ToIntLikeOriginal(tokens[p + 1]), ToIntLikeOriginal(tokens[p + 2]), ToIntLikeOriginal(tokens[p + 3]), ToIntLikeOriginal(tokens[p + 4])));
                    added++;
                    p += 5;
                }
                else
                {
                    break;
                }
            }
            return added;
        }

        private static void PostProcessLineSortLikeOriginal(List<C2BuildingLineSortLikeOriginal> lineSort)
        {
            if (lineSort == null || lineSort.Count == 0)
                return;

            // Engine copy, see COSSACKS2/mdParser.cpp::MDLINESORT::Initialize and
            // COSSACKS2/NewMon.cpp legacy parser:
            // - GROUND is first stored as (-10000,-10000,-10000,-10000).
            // - All real LINE/POINT descriptors define MinX/MaxX.
            // - If any GROUND exists, MinY is forced to -10 and GROUND entries become
            //   (c_AlignGround,-10,avx,-10) under _USE3D, preserving c_AlignGround so
            //   DrawSpriteBuilding selects GetAlignGroundTransform.
            bool hasGround = false;
            int minX = 10000;
            int maxX = -10000;
            for (int i = 0; i < lineSort.Count; i++)
            {
                C2BuildingLineSortLikeOriginal li = lineSort[i];
                if (li.IsGround)
                {
                    hasGround = true;
                    continue;
                }
                if (li.IsTop)
                    continue;
                minX = Mathf.Min(minX, li.X1, li.X2);
                maxX = Mathf.Max(maxX, li.X1, li.X2);
            }

            if (!hasGround || minX > maxX)
                return;

            int avx = (minX + maxX) >> 1;
            for (int i = 0; i < lineSort.Count; i++)
            {
                if (lineSort[i].IsGround)
                    lineSort[i] = new C2BuildingLineSortLikeOriginal(C2BuildingsAlignGroundLikeOriginal, -10, avx, -10);
            }
        }

        private static void ApplyExtraLockToLockPointsLikeOriginal(List<Vector2> lockPoints)
        {
            if (lockPoints == null || lockPoints.Count == 0)
                return;

            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < lockPoints.Count; i++)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt(lockPoints[i].x), 0, 255);
                int y = Mathf.Clamp(Mathf.RoundToInt(lockPoints[i].y), 0, 255);
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            int width = Mathf.Max(1, maxX + 2);
            int height = Mathf.Max(1, maxY + 2);
            bool[,] ar = new bool[width, height];

            for (int i = 0; i < lockPoints.Count; i++)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt(lockPoints[i].x), 0, width - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(lockPoints[i].y), 0, height - 1);

                ar[x, y] = true;
                if (x + 1 < width) ar[x + 1, y] = true;
                if (y + 1 < height) ar[x, y + 1] = true;
                if (x > 0) ar[x - 1, y] = true;
                if (y > 0) ar[x, y - 1] = true;
            }

            lockPoints.Clear();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (ar[x, y])
                        lockPoints.Add(new Vector2(x, y));
        }

        private static void ApplyExtraLockToBuildPointsLikeOriginal(List<Vector2> buildPoints)
        {
            if (buildPoints == null || buildPoints.Count == 0)
                return;

            int xs = 0;
            int ys = 0;
            for (int i = 0; i < buildPoints.Count; i++)
            {
                xs += Mathf.RoundToInt(buildPoints[i].x);
                ys += Mathf.RoundToInt(buildPoints[i].y);
            }

            xs /= buildPoints.Count;
            ys /= buildPoints.Count;

            for (int i = 0; i < buildPoints.Count; i++)
            {
                int x = Mathf.RoundToInt(buildPoints[i].x);
                int y = Mathf.RoundToInt(buildPoints[i].y);

                if (x < xs) x--;
                if (x > xs) x++;
                // Copy the original NewMon.cpp behavior.  The old source compares BuildPtY to xs,
                // not ys.  Keep it as-is for 1:1 data compatibility.
                if (y < xs) y--;
                if (y > xs) y++;

                buildPoints[i] = new Vector2(x, y);
            }
        }

        private static void ParsePointListLikeOriginal(string[] tokens, List<Vector2> dst, bool doubleY)
        {
            if (dst == null)
                return;

            dst.Clear();
            if (tokens == null || tokens.Length < 2)
                return;

            int count = Mathf.Max(0, ToIntLikeOriginal(tokens[1]));
            int p = 2;
            for (int i = 0; i < count && p + 1 < tokens.Length; i++, p += 2)
            {
                int x = ToIntLikeOriginal(tokens[p]);
                int y = ToIntLikeOriginal(tokens[p + 1]);
                dst.Add(new Vector2(x, doubleY ? (y << 1) : y));
            }
        }

        private static void ParseServicePointListLikeOriginal(string[] tokens, List<Vector2> dst, bool version2)
        {
            if (dst == null)
                return;

            dst.Clear();
            if (tokens == null || tokens.Length < 2)
                return;

            int count = Mathf.Max(0, ToIntLikeOriginal(tokens[1]));
            int p = 2;
            for (int i = 0; i < count && p + 1 < tokens.Length; i++, p += 2)
            {
                int x = ToIntLikeOriginal(tokens[p]);
                int y = ToIntLikeOriginal(tokens[p + 1]);

                if (version2)
                {
                    // BORNPOINTS2 / CONCENTRATOR2 in NewMon.cpp:
                    // BornPtX.Add(p2); BornPtY.Add(p3<<1)
                    dst.Add(new Vector2(x, y << 1));
                }
                else
                {
                    // BORNPOINTS / CONCENTRATOR in NewMon.cpp:
                    // BornPtX.Add(p2*16+8); BornPtY.Add(p3*16+8)
                    dst.Add(new Vector2((x << 4) + 8, (y << 4) + 8));
                }
            }
        }

        private static string PackageForFileRefLikeOriginal(C2BuildingMdInfoLikeOriginal md, int fileRef)
        {
            if (md != null && md.RlcPackages.TryGetValue(fileRef, out string pkg) && !string.IsNullOrEmpty(pkg))
                return pkg;
            return md != null ? md.Package : string.Empty;
        }

        private static string NormalizeAnimationNameLikeOriginal(string name)
        {
            string s = (name ?? string.Empty).Trim();
            if (s.Length == 0)
                return "#";
            if (s[0] == '@' || s[0] == '$')
                s = "#" + s.Substring(1);
            if (s[0] != '#')
                s = "#" + s;
            return s.ToUpperInvariant();
        }

        private static int AnimationSuffixLikeOriginal(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int p = name.LastIndexOf('_');
            if (p < 0 || p + 1 >= name.Length)
                return -1;
            return ToIntLikeOriginal(name.Substring(p + 1));
        }

        private static string CleanPackageNameLikeOriginal(string package)
        {
            string s = (package ?? string.Empty).Trim().Trim('"');
            return s.Replace('/', '\\');
        }

        private static string StrictMineAliasMdNameLikeOriginal(string baseName)
        {
            string s = (baseName ?? string.Empty).Trim();
            if (string.Equals(s, "BldRudCoal", StringComparison.OrdinalIgnoreCase)) return "BldRudSel";
            if (string.Equals(s, "BldRudUgl", StringComparison.OrdinalIgnoreCase)) return "BldRudUgl";
            if (string.Equals(s, "BldRudIron", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudRud", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudOre", StringComparison.OrdinalIgnoreCase)) return "BldRudRud";
            if (string.Equals(s, "BldRudGold", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudGln", StringComparison.OrdinalIgnoreCase)) return "BldRudGln";
            if (string.Equals(s, "BldRudStone", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudSton", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudKam", StringComparison.OrdinalIgnoreCase)) return "BldRudKam";
            if (string.Equals(s, "BldRudSel", StringComparison.OrdinalIgnoreCase)) return "BldRudSel";
            return string.Empty;
        }

        private static string NationPrefixLikeOriginal(string suffix)
        {
            string s = (suffix ?? string.Empty).Trim().ToUpperInvariant();
            if (s == "FR" || s == "FRA" || s == "SFR") return "Frn";
            if (s == "RU" || s == "RUS" || s == "DR") return "Rus";
            if (s == "EN" || s == "ENG") return "Eng";
            if (s == "AU" || s == "AUS") return "Aus";
            if (s == "EG" || s == "EGP") return "Egp";
            if (s == "SP" || s == "SPN" || s == "ES" || s == "ESP") return "Spn";
            if (s == "PR" || s == "PRU") return "Pru";
            return string.Empty;
        }

        private static List<string> DataRootsForBuildingsLikeOriginal()
        {
            var roots = new List<string>();
            void Add(string p)
            {
                if (!string.IsNullOrWhiteSpace(p) && !roots.Exists(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase)))
                    roots.Add(p);
            }

            Add(@"C:\GSC Game World\Cossacks II\Data");
            Add(@"C:\GSC Game World\Cossacks II\Data\UnitsMD");
            Add(@"C:\GSC Game World\Cossacks II\Data\UnitsG17");
            Add(@"C:\GSC Game World\Cossacks II\Data\Cash");
            Add(@"C:\GSC Game World\Cossacks II\Data1");
            Add(@"C:\GSC Game World\Cossacks II\Data1\Cash");
            Add(Application.streamingAssetsPath);
            Add(Path.Combine(Application.streamingAssetsPath, "Cossacks2"));
            Add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));
            Add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "Cash"));
            Add(Path.Combine(Application.dataPath, "Resources"));
            Add(Path.Combine(Application.dataPath, "Resources", "Data"));
            Add(Path.Combine(Application.dataPath, "Resources", "Data", "Cash"));
            Add(Path.Combine(Application.dataPath, "Resources", "UnitsMD"));
            Add(Path.Combine(Application.dataPath, "Resources", "UnitsG17"));
            Add(Path.Combine(Application.dataPath, "..", "Data"));
            return roots;
        }

        private static string StripCommentLikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line))
                return string.Empty;
            int p = line.IndexOf("//", StringComparison.Ordinal);
            return p >= 0 ? line.Substring(0, p) : line;
        }

        private static string[] SplitTokensLikeOriginal(string s)
        {
            return (s ?? string.Empty).Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int ToIntLikeOriginal(string s)
        {
            if (int.TryParse((s ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return 0;
        }

        private static float ToFloatLikeOriginal(string s, float fallback)
        {
            if (float.TryParse((s ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;
            return fallback;
        }

        private static string DecodeCString1251LikeOriginal(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;
            int len = 0;
            while (len < bytes.Length && bytes[len] != 0)
                len++;
            try { return Encoding.GetEncoding(1251).GetString(bytes, 0, len).Trim(); }
            catch { return Encoding.ASCII.GetString(bytes, 0, len).Trim(); }
        }

        private static string SanitizeNameLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "empty";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }
            return sb.ToString();
        }

        private static void AddLimitedLikeOriginal(List<string> list, string value, int max)
        {
            if (list == null || list.Count >= max)
                return;
            list.Add(value ?? string.Empty);
        }
    }

    public sealed class C2BuildingWorkFrameAnimatorV206LikeOriginal : MonoBehaviour
    {
        public Texture2D[] Textures;
        public Vector3[][] Vertices;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public MeshRenderer DepthRenderer;
        public float FrameRate = 12.0f;
        public bool RuntimePseudoProjectionOwnsVerticesLikeOriginal;
        public int CurrentFrameLikeOriginal { get; private set; } = -1;

        private int _lastFrame = -1;
        private MaterialPropertyBlock _block;
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private void Update()
        {
            if (Textures == null || Textures.Length == 0 || Mesh == null || Renderer == null)
                return;

            float fps = FrameRate > 0.01f ? FrameRate : 12.0f;
            int frame = ((int)(Time.time * fps)) % Textures.Length;
            CurrentFrameLikeOriginal = frame;
            if (frame == _lastFrame)
                return;

            _lastFrame = frame;
            Texture2D tex = Textures[frame];
            if (tex != null)
            {
                if (_block == null)
                    _block = new MaterialPropertyBlock();
                Renderer.GetPropertyBlock(_block);
                _block.SetTexture(MainTexId, tex);
                _block.SetTexture(BaseMapId, tex);
                Renderer.SetPropertyBlock(_block);

                if (Renderer.sharedMaterial != null)
                    Renderer.sharedMaterial.mainTexture = tex;

                if (DepthRenderer != null)
                {
                    DepthRenderer.GetPropertyBlock(_block);
                    _block.SetTexture(MainTexId, tex);
                    _block.SetTexture(BaseMapId, tex);
                    DepthRenderer.SetPropertyBlock(_block);

                    if (DepthRenderer.sharedMaterial != null)
                        C2SpriteDepthPrepassLikeOriginal.ConfigureDepthMaterialLikeOriginal(
                            DepthRenderer.sharedMaterial,
                            tex,
                            C2BattleTerrainMode.C2BuildingsDepthAlphaCutoffLikeOriginal);
                }
            }

            if (!RuntimePseudoProjectionOwnsVerticesLikeOriginal &&
                Vertices != null && frame < Vertices.Length && Vertices[frame] != null)
            {
                Mesh.vertices = Vertices[frame];
                Mesh.RecalculateBounds();
            }
        }
    }
}
