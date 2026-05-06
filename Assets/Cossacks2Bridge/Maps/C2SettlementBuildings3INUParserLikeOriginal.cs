
// C2SettlementBuildings3INUParserLikeOriginal.cs
// V64: kill C2_Nature_TS_V2_batch_* roots directly; V63 missed because renderers live under child paths.
 //      Keeps V57 layer-composite visuals, V55 cache, V53 windmill work sort, V52 part sorting, V50 NDS aliases.
 //      Does not touch building MD/G16 frames, forests, trees, stones, roads, water or terrain chunks.
// Buildings: MonsterID -> .md -> USERLC/USERLCEXT -> .g16 sprite.
// Units:     MonsterID -> .md -> USERLC/USERLCEXT -> .g2d sprite.
// Does NOT use OC/COMPLEX as buildings. V64 disables C2_Nature_TS_V2_batch shadow batch roots directly.

using System;
using System.Collections;
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
        private const bool Settlement3InuMdV2Enabled = true;
        private const bool Settlement3InuMdV2DrawBuildings = true;
        private const bool Settlement3InuMdV2DrawUnitsWhenMdResolved = false;
        private const bool Settlement3InuMdV2DrawAnimals = false;
        private const bool Settlement3InuMdV2DrawMissingMdMarkers = false;
        private const bool Settlement3InuMdV2DrawLabels = false;
        private const int Settlement3InuMdV2RecordSize = 54;
        private const int C2Settlement3InuMdV2AlignGround = -10000;
        private const int C2Settlement3InuMdV2AlignTopmost = 10000;
        private const string Settlement3InuMdV2RootPrefix = "C2_SettlementBuildings_3INU_MD_V32_";
        private const string Settlement3InuMdV2MaterialName = "C2_SettlementBuildings_3INU_MD_V32_Mat";
        // V32: stop compensating by eye. Building frames now use the original NewMonster
        // pivot rule (building NF.dx/dy = PicDx/PicDy) and each GP frame's own dimensions.
        private const float Settlement3InuMdV2SpriteScaleCompensator = 1.0f;
        // V21: restore V19 behavior: draw the first WORK frame so mines/mills keep their visible moving/extra element.
        // This is the user-confirmed better visual baseline.
        private const bool Settlement3InuMdV2DrawWorkStaticPreview = false;
        private const float Settlement3InuMdV2WorkAnimationFps = 12.0f;
        // V22 / V49 audit: shader sprite_buildings uses alpha-test with AlphaRef=4,
        // ZEnable=1, ZWriteEnable=1, ZFunc=4 (LEqual), alpha blend SrcAlpha/InvSrcAlpha,
        // point sampling (Mag/Min=1) and no Unity lighting/fog.
        private const float Settlement3InuMdV2AlphaRefV49LikeOriginal = 4.0f / 255.0f;
        private const int Settlement3InuMdV2RenderQueueV49LikeOriginal = (int)RenderQueue.Overlay - 10; // V23 temporary Unity adapter: screen-sprite pass, before full 3-point depth is ported
        private const bool Settlement3InuMdV2UseVisibleBottomLiftHack = false;
        // V54: production-fast path. Heavy audit (full lists, per-part sorting spam and RGBA hashes)
        // is kept available but disabled by default because it repeatedly reads texture pixels and
        // builds multi-kilobyte log strings during map load. Visual logic is unchanged.
        private const bool Settlement3InuMdV2VerboseAuditV54 = false;
        private const bool Settlement3InuMdV2DiskFrameCacheV55 = true;
        // V57 test mode: Paint.NET-style duplicated layer compositing.
        // This matches the user's manual test: original sprite + the same sprite over it.
        // RGB is kept exact; only final straight-alpha is recomputed by normal SourceOver.
        private const bool Settlement3InuMdV2LayerCompositeV57 = true;
        private const float Settlement3InuMdV2LayerCompositeTopOpacityV57 = 1.00f; // 1.00 = exact duplicate layer, 0.50 = half-opacity duplicate layer
        // V63: the user selected the real offender in Unity hierarchy: C2_Nature_TS_V2_batch_*.
        // These are tree-shadow/nature-shadow batches, not 3INU buildings. V61 was too broad and killed forests;
        // V62 targeted TerrainShadowOverlay and missed this object. V63 targets only TS batch objects near parsed mines.
        private const bool Settlement3InuMdV2MineNearbyAuditV58 = true;
        private const bool Settlement3InuMdV2CullNatureNearMinesV61 = false;
        private const bool Settlement3InuMdV2CullTerrainShadowOverlayNearMinesV62 = false;
        private const bool Settlement3InuMdV2CullNatureTreeShadowBatchNearMinesV63 = true;
        private const int Settlement3InuMdV2MineNearbyAuditDelayFramesV58 = 2;
        private const float Settlement3InuMdV2MineNearbyAuditRadiusWorldV58 = 55.0f;
        private const float Settlement3InuMdV2MineNatureCullRadiusWorldV61 = 55.0f;
        private const float Settlement3InuMdV2MineTerrainShadowCullRadiusWorldV62 = 55.0f;
        private const float Settlement3InuMdV2MineNatureTreeShadowBatchCullRadiusWorldV63 = 55.0f;
        private const int Settlement3InuMdV2MineNearbyAuditMaxPerMineV58 = 96;
        private const int Settlement3InuMdV2MineNearbyAuditTopSmallPerMineV59 = 20;
        private const int Settlement3InuMdV2MineNearbyAuditTopExcludedPerMineV59 = 30;
        private const float Settlement3InuMdV2MineNearbyAuditMaxSmallHorizontalV59 = 110.0f;
        private const float Settlement3InuMdV2MineNearbyAuditMaxSmallAreaV59 = 7200.0f;

        private enum C2Settlement3InuMdV2Kind
        {
            Unknown,
            SettlementBuilding,
            Building,
            ResourceBuilding,
            Unit,
            Animal,
            SpriteObject
        }

        private struct C2Settlement3InuMdV2Record
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

        private struct C2Settlement3InuMdV2AnimFrame
        {
            public int FileRef;
            public int SpriteId;

            public C2Settlement3InuMdV2AnimFrame(int fileRef, int spriteId)
            {
                FileRef = fileRef;
                SpriteId = spriteId;
            }
        }

        private struct C2Settlement3InuMdV2LineSortInfo
        {
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;

            public bool IsGround { get { return X1 == C2Settlement3InuMdV2AlignGround; } }
            public bool IsTop { get { return X1 == C2Settlement3InuMdV2AlignTopmost; } }
            public bool IsLine { get { return !IsGround && !IsTop; } }

            public C2Settlement3InuMdV2LineSortInfo(int x1, int y1, int x2, int y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
        }

        private sealed class C2Settlement3InuMdV2Animation
        {
            public string Name;
            public int Rotations = 1;
            public readonly List<C2Settlement3InuMdV2AnimFrame> Frames = new List<C2Settlement3InuMdV2AnimFrame>();
            public readonly List<C2Settlement3InuMdV2LineSortInfo> LineSort = new List<C2Settlement3InuMdV2LineSortInfo>();
        }

        private sealed class C2Settlement3InuMdV2LoadedFrame
        {
            public Texture2D Texture;
            public C2Settlement3InuMdV2AnimFrame Frame;
            public bool HasLineSort;
            public C2Settlement3InuMdV2LineSortInfo LineSort;
            public string AnimationName;
            public bool IsWork;

            public C2Settlement3InuMdV2LoadedFrame(Texture2D texture, C2Settlement3InuMdV2AnimFrame frame, string animationName, bool isWork)
            {
                Texture = texture;
                Frame = frame;
                AnimationName = animationName;
                IsWork = isWork;
            }
        }

        private sealed class C2Settlement3InuMdV2Info
        {
            public bool Found;
            public string MdPath;
            public string MdName;
            public string Package;
            public string PreferredExt;
            public int SpriteId;
            public readonly List<C2Settlement3InuMdV2AnimFrame> StandLoFrames = new List<C2Settlement3InuMdV2AnimFrame>();
            public readonly List<C2Settlement3InuMdV2LineSortInfo> StandLoLineSort = new List<C2Settlement3InuMdV2LineSortInfo>();
            // #WORK/@WORK frames are not part of the base house body. Original switches these over time.
            // V23: do not draw them as permanent static base parts; they are animated overlays.
            public readonly List<C2Settlement3InuMdV2AnimFrame> WorkFrames = new List<C2Settlement3InuMdV2AnimFrame>();
            // USERLC/USERLCEXT maps MD fileRef -> real GP package. Original stores FileID per animation frame.
            // V22 ignored FileRef and always used md.Package, which can bind wrong sprites for multi-package MDs.
            public readonly Dictionary<int, string> RlcPackages = new Dictionary<int, string>();
            public readonly Dictionary<int, int> RlcDx = new Dictionary<int, int>();
            public readonly Dictionary<int, int> RlcDy = new Dictionary<int, int>();
            public readonly Dictionary<string, C2Settlement3InuMdV2Animation> Animations = new Dictionary<string, C2Settlement3InuMdV2Animation>(StringComparer.OrdinalIgnoreCase);
            public int Rotations = 1;
            public int Dx;
            public int Dy;
            public int PicDx;
            public int PicDy;
            public int PicLx;
            public int PicLy;
            public int SetAnmParamDx;
            public int SetAnmParamDy;
            public int SetAnmParamParts = 1;
            public int SetAnmParamPartSize = 96;
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
            public int BuildStages;
            public string DestructRaw;
            public bool Building;
            public bool SpriteObject;
            public bool Peasant;
            public bool UnitAbsorber;
            public bool PeasantAbsorber;
            public bool Producer;
            public bool NotSelectable;
            public int UnitRadius = 16;
            public int FreeAdd;
            public int PeasantAdd;
            public int MaxInside;
            public bool ParsedAnimation;
            public bool HasUserLc;
            public string Usage;
            public C2Settlement3InuMdV2Kind Kind;
            public string Audit;
        }

        private static readonly Dictionary<string, C2Settlement3InuMdV2Info> Settlement3InuMdV2MdCache = new Dictionary<string, C2Settlement3InuMdV2Info>(StringComparer.OrdinalIgnoreCase);
        private sealed class C2Settlement3InuMdV2TextureCacheEntryV54
        {
            public Texture2D Texture;
            public string Source;
            public string Size;
        }
        private static readonly Dictionary<string, C2Settlement3InuMdV2TextureCacheEntryV54> s_C2Settlement3InuMdV2TextureCacheV54 = new Dictionary<string, C2Settlement3InuMdV2TextureCacheEntryV54>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, Material> s_C2Settlement3InuMdV2MaterialCacheV54 = new Dictionary<int, Material>();
        private static readonly Dictionary<string, string> s_C2Settlement3InuMdV2VisualPathCacheV55 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static int s_C2Settlement3InuMdV2TextureCacheHitsV54;
        private static int s_C2Settlement3InuMdV2TextureCacheMissesV54;
        private static int s_C2Settlement3InuMdV2MaterialCacheHitsV54;
        private static int s_C2Settlement3InuMdV2MaterialCacheMissesV54;
        private static int s_C2Settlement3InuMdV2DiskCacheHitsV55;
        private static int s_C2Settlement3InuMdV2DiskCacheMissesV55;
        private static int s_C2Settlement3InuMdV2DiskCacheWritesV55;
        private static int s_C2Settlement3InuMdV2DiskCacheWriteFailsV55;
        private static int s_C2Settlement3InuMdV2VisualPathCacheHitsV55;
        private static int s_C2Settlement3InuMdV2VisualPathCacheMissesV55;
        private static int s_C2Settlement3InuMdV2LayerBlendFramesV57;
        private static long s_C2Settlement3InuMdV2LayerBlendPixelsV57;
        private static long s_C2Settlement3InuMdV2LayerBlendOpaquePixelsV57;
        // V50: original nations define logical map IDs -> real MD names in *.NDS.
        // Example from France.NDS: BldRudCoal(FR) -> BldRudSel, BldRudGold(FR) -> BldRudGln.
        // This must win before hand-written English/Russian aliases.
        private static Dictionary<string, string> s_C2Settlement3InuMdV2NdsUnitToMdV50;
        private static string s_C2Settlement3InuMdV2NdsAuditV50 = "not_built";
        // V51: Unity sortingOrder must stay inside a compact range and must follow the original
        // ZBuffer building bucket order. Original AddAnimation uses:
        // YL = (mapY >> 1) - (mapy << 4) + DYZBuf, then ArrangeZBuffer iterates YL ascending.
        // Camera mapy is a constant offset for all objects, so whole-map relative order is mapY>>1.
        // Same-line order follows original UNI3 insertion order; do not use X as a tie-breaker.
        private static Dictionary<int, int> s_C2Settlement3InuMdV2SortRankV51 = new Dictionary<int, int>();
        private static string s_C2Settlement3InuMdV2SortRankAuditV51 = "not_built";
        private static Material Settlement3InuMdV2Material;

        // Optional manual fallback. Normally auto-detected from C2BattleTerrainMode._mapRelativePath.
        public string Settlement3InuMdV2MapPathOverride = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void C2Settlement3InuMdV6AutoInstallLikeOriginal()
        {
            if (!Settlement3InuMdV2Enabled) return;

            var existing = UnityEngine.Object.FindObjectOfType<C2Settlement3InuMdV6AutoRunner>();
            if (existing != null) return;

            var go = new GameObject("C2_SettlementBuildings_3INU_MD_V32_AutoRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<C2Settlement3InuMdV6AutoRunner>();
        }

        private sealed class C2Settlement3InuMdV6AutoRunner : MonoBehaviour
        {
            private C2BattleTerrainMode _lastMode;
            private string _lastMap;
            private float _nextTick;
            private int _waitLogs;

            private void Update()
            {
                if (Time.realtimeSinceStartup < _nextTick) return;
                _nextTick = Time.realtimeSinceStartup + 0.5f;

                var mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                if (mode == null) return;

                mode.C2Settlement3InuMdV2DisableWrongOcComplexAdapterLikeOriginal();

                string map = mode.TryGetCurrentMapPathForSettlement3InuMdV2LikeOriginal();
                bool mapObjectReady = mode._map != null;

                if (string.IsNullOrWhiteSpace(map) || !mapObjectReady)
                {
                    if (_waitLogs < 12)
                    {
                        _waitLogs++;
                        Debug.Log("[C2:SETTLEMENT 3INU V32 WAIT] mode found mapPath='" +
                                  (map ?? "<null>") + "' mapObjectReady=" + mapObjectReady +
                                  " hint=waiting for [C2:MAP] Parsed clean map / _mapRelativePath");
                    }
                    return;
                }

                if (_lastMode == mode && string.Equals(_lastMap, map, StringComparison.OrdinalIgnoreCase))
                    return;

                _lastMode = mode;
                _lastMap = map;
                mode.BuildSettlementBuildingsFrom3InuMdV2LikeOriginal(map, "auto-runner-v32");
            }
        }

        private string TryGetCurrentMapPathForSettlement3InuMdV2LikeOriginal()
        {
            if (!string.IsNullOrWhiteSpace(Settlement3InuMdV2MapPathOverride))
                return Settlement3InuMdV2MapPathOverride.Trim();

            // Current clean terrain mode stores the selected map here:
            // private string _mapRelativePath = "Missions\Skirmish\Skirmish2.m3d";
            string[] names =
            {
                "_mapRelativePath", "mapRelativePath", "MapRelativePath",
                "_selectedMapPath", "selectedMapPath", "SelectedMapPath",
                "lastLoadedMapPath", "_lastLoadedMapPath",
                "currentMapPath", "_currentMapPath",
                "mapPath", "_mapPath",
                "lastMapPath", "_lastMapPath"
            };

            Type t = GetType();
            BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (int i = 0; i < names.Length; i++)
            {
                var fi = t.GetField(names[i], f);
                if (fi != null && fi.FieldType == typeof(string))
                {
                    string v = fi.GetValue(this) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }

                var pi = t.GetProperty(names[i], f);
                if (pi != null && pi.PropertyType == typeof(string) && pi.GetIndexParameters().Length == 0)
                {
                    string v = null;
                    try { v = pi.GetValue(this, null) as string; } catch { }
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            }

            // Fallback: read private ParsedMap _map.SourcePath when available.
            object parsed = null;
            var mapField = t.GetField("_map", f) ?? t.GetField("map", f) ?? t.GetField("ParsedMap", f);
            if (mapField != null) { try { parsed = mapField.GetValue(this); } catch { parsed = null; } }

            if (parsed != null)
            {
                Type mt = parsed.GetType();
                var spField = mt.GetField("SourcePath", f) ?? mt.GetField("sourcePath", f);
                if (spField != null && spField.FieldType == typeof(string))
                {
                    string v = spField.GetValue(parsed) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }

                var spProp = mt.GetProperty("SourcePath", f) ?? mt.GetProperty("sourcePath", f);
                if (spProp != null && spProp.PropertyType == typeof(string) && spProp.GetIndexParameters().Length == 0)
                {
                    string v = null;
                    try { v = spProp.GetValue(parsed, null) as string; } catch { }
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            }

            return null;
        }

        public void BuildSettlementBuildingsFrom3InuMdV2LikeOriginal(string mapPath, string source = "manual")
        {
            if (!Settlement3InuMdV2Enabled) return;
            var swTotalV54 = System.Diagnostics.Stopwatch.StartNew();
            s_C2Settlement3InuMdV2TextureCacheHitsV54 = 0;
            s_C2Settlement3InuMdV2TextureCacheMissesV54 = 0;
            s_C2Settlement3InuMdV2MaterialCacheHitsV54 = 0;
            s_C2Settlement3InuMdV2MaterialCacheMissesV54 = 0;
            s_C2Settlement3InuMdV2DiskCacheHitsV55 = 0;
            s_C2Settlement3InuMdV2DiskCacheMissesV55 = 0;
            s_C2Settlement3InuMdV2DiskCacheWritesV55 = 0;
            s_C2Settlement3InuMdV2DiskCacheWriteFailsV55 = 0;
            s_C2Settlement3InuMdV2VisualPathCacheHitsV55 = 0;
            s_C2Settlement3InuMdV2VisualPathCacheMissesV55 = 0;
            s_C2Settlement3InuMdV2LayerBlendFramesV57 = 0;
            s_C2Settlement3InuMdV2LayerBlendPixelsV57 = 0;
            s_C2Settlement3InuMdV2LayerBlendOpaquePixelsV57 = 0;
            C2Settlement3InuMdV2DisableWrongOcComplexAdapterLikeOriginal();
            C2Settlement3InuMdV2ClearOldRootsLikeOriginal();

            if (_map == null)
            {
                Debug.LogWarning("[C2:SETTLEMENT 3INU V32] parsed terrain map object is not ready yet; skip build source=" + source + " map='" + (mapPath ?? "<null>") + "'");
                return;
            }

            string abs = C2Settlement3InuMdV2ResolveMapPathLikeOriginal(mapPath);
            if (string.IsNullOrEmpty(abs) || !File.Exists(abs))
            {
                Debug.LogWarning("[C2:SETTLEMENT 3INU V32] map not found: " + (mapPath ?? "<null>"));
                return;
            }

            long parseStartMsV54 = swTotalV54.ElapsedMilliseconds;
            List<C2Settlement3InuMdV2Record> records;
            string chunkAudit;
            if (!C2Settlement3InuMdV2TryParseRecordsLikeOriginal(abs, out records, out chunkAudit))
            {
                Debug.LogWarning("[C2:SETTLEMENT 3INU V32] no 3INU/UNI3 records map='" + mapPath + "' audit=" + chunkAudit);
                return;
            }

            long parseMsV54 = swTotalV54.ElapsedMilliseconds - parseStartMsV54;
            C2Settlement3InuMdV2BuildSortRanksV51LikeOriginal(records);

            long buildStartMsV54 = swTotalV54.ElapsedMilliseconds;
            var root = new GameObject(Settlement3InuMdV2RootPrefix + Path.GetFileNameWithoutExtension(abs));
            root.transform.SetParent(transform, true);

            int mdFound = 0, mdMissing = 0, visualFound = 0, visualMissing = 0;
            int buildings = 0, resources = 0, settlements = 0, units = 0, animals = 0, unknown = 0;
            int drawn = 0, skipped = 0;
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mdMiss = new List<string>();
            var visMiss = new List<string>();
            var sample = new List<string>();
            var mineAudit = new List<string>();
            var fullMdAudit = new List<string>();
            var buildingMdAudit = new List<string>();
            var mineVisualAudit = new List<string>();
            var mineFamilyCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mineRecordsForNearbyAuditV58 = new List<C2Settlement3InuMdV2Record>();

            string rawMineStringScan = Settlement3InuMdV2VerboseAuditV54 ? C2Settlement3InuMdV2RawMineStringScanInMapV49LikeOriginal(abs) : "skipped_fast_v55";
            string ndsAliasAudit = Settlement3InuMdV2VerboseAuditV54 ? C2Settlement3InuMdV2NdsResourceAliasAuditV50LikeOriginal(records) : "skipped_fast_v55";

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                C2Settlement3InuMdV2Count(nameCounts, r.MonsterId);
                var md = C2Settlement3InuMdV2ResolveMdLikeOriginal(r.MonsterId);
                if (md.Found) mdFound++; else { mdMissing++; C2Settlement3InuMdV2AddLimited(mdMiss, r.MonsterId, 24); }

                var kind = md.Found ? md.Kind : C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(r.MonsterId);
                switch (kind)
                {
                    case C2Settlement3InuMdV2Kind.SettlementBuilding: settlements++; break;
                    case C2Settlement3InuMdV2Kind.ResourceBuilding: resources++; break;
                    case C2Settlement3InuMdV2Kind.Building:
                    case C2Settlement3InuMdV2Kind.SpriteObject: buildings++; break;
                    case C2Settlement3InuMdV2Kind.Unit: units++; break;
                    case C2Settlement3InuMdV2Kind.Animal: animals++; break;
                    default: unknown++; break;
                }

                string strictMineAlias = C2Settlement3InuMdV2MineMdAliasForAuditV50LikeOriginal(r.MonsterId);
                if (!string.IsNullOrEmpty(strictMineAlias))
                {
                    mineRecordsForNearbyAuditV58.Add(r);
                    C2Settlement3InuMdV2Count(mineFamilyCounts, strictMineAlias);
                    if (Settlement3InuMdV2VerboseAuditV54)
                    {
                        C2Settlement3InuMdV2AddLimited(
                            mineAudit,
                            "#" + r.Index.ToString(CultureInfo.InvariantCulture) +
                            " " + r.MonsterId +
                            " NI=" + r.Nation.ToString(CultureInfo.InvariantCulture) +
                            " NIndex=" + r.NIndex.ToString(CultureInfo.InvariantCulture) +
                            " -> strict=" + strictMineAlias +
                            " md=" + (md.Found ? Path.GetFileName(md.MdPath) : "<missing>") +
                            " pkg=" + (md.Package ?? "<none>") +
                            " real=(" + r.RealX.ToString(CultureInfo.InvariantCulture) + "," + r.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                            " map=(" + (r.RealX >> 4).ToString(CultureInfo.InvariantCulture) + "," + (r.RealY >> 4).ToString(CultureInfo.InvariantCulture) + ")" +
                            " dir=" + r.RealDir.ToString(CultureInfo.InvariantCulture),
                            256);
                    }
                }

                if (Settlement3InuMdV2VerboseAuditV54)
                {
                    string fullEntry = "#" + r.Index.ToString(CultureInfo.InvariantCulture) +
                        " kind=" + kind +
                        " name='" + r.MonsterId + "'" +
                        " ndsAlias=" + (C2Settlement3InuMdV2ResolveNdsMdAliasV50LikeOriginal(r.MonsterId) ?? "") +
                        " NI=" + r.Nation.ToString(CultureInfo.InvariantCulture) +
                        " NIndex=" + r.NIndex.ToString(CultureInfo.InvariantCulture) +
                        " md=" + (md.Found ? Path.GetFileName(md.MdPath) : "<missing>") +
                        " pkg='" + (md.Package ?? "") + "'" +
                        " sprite=" + md.SpriteId.ToString(CultureInfo.InvariantCulture) +
                        " stand=" + (md.StandLoFrames != null ? md.StandLoFrames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                        " work=" + (md.WorkFrames != null ? md.WorkFrames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                        " real=(" + r.RealX.ToString(CultureInfo.InvariantCulture) + "," + r.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                        " map=(" + (r.RealX >> 4).ToString(CultureInfo.InvariantCulture) + "," + (r.RealY >> 4).ToString(CultureInfo.InvariantCulture) + ")" +
                        " dir=" + r.RealDir.ToString(CultureInfo.InvariantCulture) +
                        " life=" + r.Life.ToString(CultureInfo.InvariantCulture) +
                        " stage=" + r.Stage.ToString(CultureInfo.InvariantCulture) +
                        " flags=" + r.Flags.ToString(CultureInfo.InvariantCulture);
                    fullMdAudit.Add(fullEntry);
                    if (kind == C2Settlement3InuMdV2Kind.SettlementBuilding ||
                        kind == C2Settlement3InuMdV2Kind.Building ||
                        kind == C2Settlement3InuMdV2Kind.ResourceBuilding ||
                        kind == C2Settlement3InuMdV2Kind.SpriteObject)
                    {
                        buildingMdAudit.Add(fullEntry);
                    }
                }

                bool shouldDraw = false;
                if (Settlement3InuMdV2DrawBuildings && (kind == C2Settlement3InuMdV2Kind.SettlementBuilding || kind == C2Settlement3InuMdV2Kind.Building || kind == C2Settlement3InuMdV2Kind.ResourceBuilding || kind == C2Settlement3InuMdV2Kind.SpriteObject)) shouldDraw = true;
                if (Settlement3InuMdV2DrawUnitsWhenMdResolved && md.Found && kind == C2Settlement3InuMdV2Kind.Unit) shouldDraw = true;
                if (Settlement3InuMdV2DrawAnimals && md.Found && kind == C2Settlement3InuMdV2Kind.Animal) shouldDraw = true;
                if (!shouldDraw) { skipped++; continue; }

                List<C2Settlement3InuMdV2LoadedFrame> loadedFrames = null;
                string visualAudit = string.Empty;
                bool ok = md.Found && C2Settlement3InuMdV2TryLoadVisualFramesLikeOriginal(md, r, kind, out loadedFrames, out visualAudit);
                if (!ok || loadedFrames == null || loadedFrames.Count == 0)
                {
                    visualMissing++;
                    if (Settlement3InuMdV2VerboseAuditV54 && !string.IsNullOrEmpty(strictMineAlias))
                    {
                        mineVisualAudit.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) + " " + r.MonsterId +
                            " -> " + strictMineAlias + " VISUAL_MISSING md=" + (md.Found ? Path.GetFileName(md.MdPath) : "<missing>") +
                            " pkg=" + (md.Package ?? "<none>") + " audit=" + visualAudit);
                    }
                    C2Settlement3InuMdV2AddLimited(visMiss, r.MonsterId + " md=" + (md.Found ? md.MdPath : "<missing>") + " pkg=" + (md.Package ?? "<none>") + " audit=" + visualAudit, 18);
                    if (!Settlement3InuMdV2DrawMissingMdMarkers) { skipped++; continue; }
                    C2Settlement3InuMdV2CreateMdBoundsFallbackLikeOriginal(root.transform, r, md, kind, md.Found ? "MD_NO_VISUAL" : "NO_MD");
                    drawn++;
                }
                else
                {
                    visualFound++;
                    if (Settlement3InuMdV2VerboseAuditV54 && !string.IsNullOrEmpty(strictMineAlias))
                    {
                        string loadedHashAudit = C2Settlement3InuMdV2LoadedFramesHashAuditV49LikeOriginal(loadedFrames);
                        mineVisualAudit.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) + " " + r.MonsterId +
                            " -> " + strictMineAlias + " VISUAL_OK md=" + Path.GetFileName(md.MdPath) +
                            " pkg=" + (md.Package ?? "<none>") + " frames=" + loadedFrames.Count.ToString(CultureInfo.InvariantCulture) +
                            " visual=" + visualAudit + " hashes=" + loadedHashAudit);
                    }
                    C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal(root.transform, r, md, kind, loadedFrames, visualAudit);
                    drawn++;
                }

                if (Settlement3InuMdV2VerboseAuditV54 && sample.Count < 48)
                {
                    sample.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) + " kind=" + kind + " name='" + r.MonsterId + "' md=" + (md.Found ? Path.GetFileName(md.MdPath) : "<missing>") + " pkg='" + (md.Package ?? "") + "' frame=" + md.SpriteId + "/parts=" + (md.StandLoFrames != null ? md.StandLoFrames.Count : 0) + "/work=" + (md.WorkFrames != null ? md.WorkFrames.Count : 0) + " real=(" + r.RealX + "," + r.RealY + ") map=(" + (r.RealX >> 4) + "," + (r.RealY >> 4) + ") dir=" + r.RealDir);
                }
            }

            long buildMsV54 = swTotalV54.ElapsedMilliseconds - buildStartMsV54;
            Debug.Log("[C2:SETTLEMENT 3INU V59 FAST] contract=V57_LAYER_COMPOSITE_DUPLICATE100_OVER_V55_CACHE source=" + source + " map='" + mapPath + "' records=" + records.Count + " mdFound=" + mdFound + " mdMissing=" + mdMissing + " visualFound=" + visualFound + " visualMissing=" + visualMissing + " settlements=" + settlements + " buildings=" + buildings + " resourceBuildings=" + resources + " units=" + units + " animals=" + animals + " unknown=" + unknown + " drawn=" + drawn + " skipped=" + skipped + " parseMs=" + parseMsV54.ToString(CultureInfo.InvariantCulture) + " buildMs=" + buildMsV54.ToString(CultureInfo.InvariantCulture) + " totalMs=" + swTotalV54.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + " texCacheHits=" + s_C2Settlement3InuMdV2TextureCacheHitsV54.ToString(CultureInfo.InvariantCulture) + " texCacheMisses=" + s_C2Settlement3InuMdV2TextureCacheMissesV54.ToString(CultureInfo.InvariantCulture) + " diskHits=" + s_C2Settlement3InuMdV2DiskCacheHitsV55.ToString(CultureInfo.InvariantCulture) + " diskMisses=" + s_C2Settlement3InuMdV2DiskCacheMissesV55.ToString(CultureInfo.InvariantCulture) + " diskWrites=" + s_C2Settlement3InuMdV2DiskCacheWritesV55.ToString(CultureInfo.InvariantCulture) + " diskWriteFails=" + s_C2Settlement3InuMdV2DiskCacheWriteFailsV55.ToString(CultureInfo.InvariantCulture) + " pathHits=" + s_C2Settlement3InuMdV2VisualPathCacheHitsV55.ToString(CultureInfo.InvariantCulture) + " pathMisses=" + s_C2Settlement3InuMdV2VisualPathCacheMissesV55.ToString(CultureInfo.InvariantCulture) + " matCacheHits=" + s_C2Settlement3InuMdV2MaterialCacheHitsV54.ToString(CultureInfo.InvariantCulture) + " matCacheMisses=" + s_C2Settlement3InuMdV2MaterialCacheMissesV54.ToString(CultureInfo.InvariantCulture) + " layerBlendFrames=" + s_C2Settlement3InuMdV2LayerBlendFramesV57.ToString(CultureInfo.InvariantCulture) + " layerBlendPixels=" + s_C2Settlement3InuMdV2LayerBlendPixelsV57.ToString(CultureInfo.InvariantCulture) + " layerBlendOpaquePixels=" + s_C2Settlement3InuMdV2LayerBlendOpaquePixelsV57.ToString(CultureInfo.InvariantCulture) + " chunkAudit=" + chunkAudit + " names=" + C2Settlement3InuMdV2TopNamesLikeOriginal(nameCounts, 96));
            Debug.Log("[C2:SETTLEMENT 3INU V59 MINE FAMILY COUNTS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(mineFamilyCounts, 32) + " note=family_is_real_MD_after_NDS_alias_no_synthetic_mines_created");
            if (Settlement3InuMdV2MineNearbyAuditV58 && mineRecordsForNearbyAuditV58.Count > 0)
            {
                StartCoroutine(C2Settlement3InuMdV2MineNearbyAuditDelayedV58(abs, root, mineRecordsForNearbyAuditV58.ToArray()));
            }
            if (Settlement3InuMdV2VerboseAuditV54)
            {
                Debug.Log("[C2:SETTLEMENT 3INU V58 RAW MINE STRING SCAN] " + rawMineStringScan);
                Debug.Log("[C2:SETTLEMENT 3INU V58 NDS RESOURCE ALIAS] " + ndsAliasAudit);
                Debug.Log("[C2:SETTLEMENT 3INU V58 ZSORT RANK] " + s_C2Settlement3InuMdV2SortRankAuditV51);
                if (sample.Count > 0) Debug.Log("[C2:SETTLEMENT 3INU V58 SAMPLE] " + string.Join(" | ", sample.ToArray()));
                C2Settlement3InuMdV2LogListChunksV49LikeOriginal("[C2:SETTLEMENT 3INU V58 MINE MAP FULL]", mineAudit, 24);
                C2Settlement3InuMdV2LogListChunksV49LikeOriginal("[C2:SETTLEMENT 3INU V58 MINE VISUAL FULL]", mineVisualAudit, 12);
                C2Settlement3InuMdV2LogListChunksV49LikeOriginal("[C2:SETTLEMENT 3INU V58 BUILDING MD LIST FULL]", buildingMdAudit, 24);
                C2Settlement3InuMdV2LogListChunksV49LikeOriginal("[C2:SETTLEMENT 3INU V58 ALL UNI3 MD LIST FULL]", fullMdAudit, 24);
            }
            if (mdMiss.Count > 0) Debug.LogWarning("[C2:SETTLEMENT 3INU V58 MD MISS] " + string.Join(" | ", mdMiss.ToArray()));
            if (visMiss.Count > 0) Debug.LogWarning("[C2:SETTLEMENT 3INU V54 VISUAL MISS] " + string.Join(" | ", visMiss.ToArray()));
        }

        private void C2Settlement3InuMdV2DisableWrongOcComplexAdapterLikeOriginal()
        {
            var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if (n.IndexOf("ComplexBuildings", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("OC", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    mb.enabled = false;
                    Debug.Log("[C2:SETTLEMENT 3INU V32] disabled wrong OC/COMPLEX building adapter: " + n);
                }
            }
        }

        private void C2Settlement3InuMdV2ClearOldRootsLikeOriginal()
        {
            var gos = UnityEngine.Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < gos.Length; i++)
            {
                var go = gos[i];
                if (go != null && go.name.StartsWith(Settlement3InuMdV2RootPrefix, StringComparison.OrdinalIgnoreCase)) SafeDestroy(go);
                if (go != null && go.name.StartsWith("C2_SettlementBuildings_3INU_", StringComparison.OrdinalIgnoreCase)) SafeDestroy(go);
            }
        }

        private static bool C2Settlement3InuMdV2TryParseRecordsLikeOriginal(string absMap, out List<C2Settlement3InuMdV2Record> records, out string audit)
        {
            records = new List<C2Settlement3InuMdV2Record>();
            audit = "";
            byte[] raw;
            try { raw = File.ReadAllBytes(absMap); } catch (Exception e) { audit = "read_error=" + e.Message; return false; }
            string err;
            byte[] data = MaybeDecompressM3d(raw, out err);
            if (data == null || data.Length < 16) { audit = "bad_data err=" + err; return false; }

            using (var ms = new MemoryStream(data, false))
            using (var br = new BinaryReader(ms))
            {
                string magic = ReadTag(br);

                int storedVertInLine = 0;
                int storedMaxTH = 0;
                if (ms.Position + 8 <= ms.Length)
                {
                    storedVertInLine = br.ReadInt32();
                    storedMaxTH = br.ReadInt32();
                }

                int chunks = 0;
                int unitChunks = 0;
                List<string> seen = new List<string>(64);
                List<string> unitChunkAudits = new List<string>(8);

                while (ms.Position + 8 <= ms.Length)
                {
                    long chunkStart = ms.Position;
                    string tag = ReadTag(br);
                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal) ||
                        string.Equals(tag, "MDNE", StringComparison.Ordinal))
                    {
                        audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                                " chunksSeen=" + chunks + " endTag=" + tag + " unitChunks=" + unitChunks +
                                " parsed=" + records.Count + " seen=" + string.Join(",", seen.ToArray()) +
                                " units=" + string.Join(" || ", unitChunkAudits.ToArray());
                        return records.Count > 0;
                    }

                    int sizeField = br.ReadInt32();
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;
                    long payloadEnd = payloadStart + payloadLen;
                    if (payloadEnd > ms.Length)
                    {
                        audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                                " chunksSeen=" + chunks + " brokenChunk tag=" + tag +
                                " chunkStart=" + chunkStart + " sizeField=" + sizeField +
                                " payloadLen=" + payloadLen + " fileLen=" + ms.Length +
                                " unitChunks=" + unitChunks + " parsed=" + records.Count +
                                " seen=" + string.Join(",", seen.ToArray()) +
                                " units=" + string.Join(" || ", unitChunkAudits.ToArray());
                        return records.Count > 0;
                    }

                    chunks++;
                    if (seen.Count < 64) seen.Add(tag + ":" + sizeField.ToString(CultureInfo.InvariantCulture));

                    if (TagEqualsLikeOriginal(tag, "3INU", "UNI3"))
                    {
                        if (payloadLen >= 4)
                        {
                            int declared = br.ReadInt32();
                            int possible = Mathf.Max(0, (payloadLen - 4) / Settlement3InuMdV2RecordSize);
                            int count = Mathf.Clamp(declared, 0, possible);
                            long recordsEnd = Math.Min(payloadEnd, ms.Position + (long)count * Settlement3InuMdV2RecordSize);
                            int before = records.Count;

                            for (int i = 0; i < count; i++)
                            {
                                if (ms.Position + Settlement3InuMdV2RecordSize > recordsEnd)
                                    break;

                                var r = new C2Settlement3InuMdV2Record();
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
                                byte[] nameBytes = br.ReadBytes(33);
                                r.MonsterId = C2Settlement3InuMdV2DecodeCStringLikeOriginal(nameBytes);
                                records.Add(r);
                            }

                            unitChunks++;
                            unitChunkAudits.Add("tag=" + tag + " chunkStart=" + chunkStart.ToString(CultureInfo.InvariantCulture) +
                                " sizeField=" + sizeField.ToString(CultureInfo.InvariantCulture) +
                                " payload=" + payloadLen.ToString(CultureInfo.InvariantCulture) +
                                " declared=" + declared.ToString(CultureInfo.InvariantCulture) +
                                " possible=" + possible.ToString(CultureInfo.InvariantCulture) +
                                " parsedHere=" + (records.Count - before).ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            unitChunkAudits.Add("tag=" + tag + " payload_too_small=" + payloadLen.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    ms.Position = payloadEnd;
                }

                audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                        " chunksSeen=" + chunks + " unitChunks=" + unitChunks +
                        " parsed=" + records.Count + " seen=" + string.Join(",", seen.ToArray()) +
                        " units=" + string.Join(" || ", unitChunkAudits.ToArray());
                return records.Count > 0;
            }
        }

        private static string C2Settlement3InuMdV2DecodeCStringLikeOriginal(byte[] b)
        {
            int n = 0;
            while (n < b.Length && b[n] != 0) n++;
            try { return Encoding.GetEncoding(1251).GetString(b, 0, n).Trim(); }
            catch { return Encoding.ASCII.GetString(b, 0, n).Trim(); }
        }

        private static void C2Settlement3InuMdV2LogListChunksV49LikeOriginal(string tag, List<string> entries, int perLine)
        {
            if (entries == null || entries.Count == 0) return;
            if (perLine <= 0) perLine = 24;
            int total = entries.Count;
            int part = 0;
            for (int i = 0; i < total; i += perLine)
            {
                int take = Math.Min(perLine, total - i);
                var chunk = new List<string>(take);
                for (int j = 0; j < take; j++) chunk.Add(entries[i + j]);
                Debug.Log(tag + " part=" + part.ToString(CultureInfo.InvariantCulture) +
                    " offset=" + i.ToString(CultureInfo.InvariantCulture) +
                    " count=" + take.ToString(CultureInfo.InvariantCulture) +
                    "/" + total.ToString(CultureInfo.InvariantCulture) + " " + string.Join(" | ", chunk.ToArray()));
                part++;
            }
        }

        private static string C2Settlement3InuMdV2RawMineStringScanInMapV49LikeOriginal(string absMap)
        {
            try
            {
                byte[] raw = File.ReadAllBytes(absMap);
                string err;
                byte[] data = MaybeDecompressM3d(raw, out err);
                if (data == null) return "bad_data err=" + err;
                var names = new List<string>();
                byte[] needle = Encoding.ASCII.GetBytes("BldRud");
                for (int i = 0; i <= data.Length - needle.Length; i++)
                {
                    bool ok = true;
                    for (int j = 0; j < needle.Length; j++)
                    {
                        if (data[i + j] != needle[j]) { ok = false; break; }
                    }
                    if (!ok) continue;
                    int end = i;
                    while (end < data.Length && data[end] != 0 && end - i < 64) end++;
                    string name;
                    try { name = Encoding.GetEncoding(1251).GetString(data, i, end - i).Trim(); }
                    catch { name = Encoding.ASCII.GetString(data, i, end - i).Trim(); }
                    if (!string.IsNullOrEmpty(name)) names.Add(name);
                }
                var c = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < names.Count; i++) C2Settlement3InuMdV2Count(c, names[i]);
                return "rawBldRudStrings=" + names.Count.ToString(CultureInfo.InvariantCulture) +
                    " counts=" + C2Settlement3InuMdV2TopNamesLikeOriginal(c, 64) +
                    " names=" + string.Join(" | ", names.ToArray());
            }
            catch (Exception e)
            {
                return "scan_error=" + e.GetType().Name + ":" + e.Message;
            }
        }

        private static string C2Settlement3InuMdV2LoadedFramesHashAuditV49LikeOriginal(List<C2Settlement3InuMdV2LoadedFrame> loadedFrames)
        {
            if (!Settlement3InuMdV2VerboseAuditV54) return "skipped_fast_v55";
            if (loadedFrames == null || loadedFrames.Count == 0) return "<none>";
            var parts = new List<string>();
            int lim = Math.Min(loadedFrames.Count, 16);
            for (int i = 0; i < lim; i++)
            {
                var lf = loadedFrames[i];
                Texture2D t = lf != null ? lf.Texture : null;
                string hash = t != null ? C2Settlement3InuMdV2TextureHash32V49LikeOriginal(t) : "<null>";
                parts.Add("#" + i.ToString(CultureInfo.InvariantCulture) +
                    " anim=" + (lf != null ? (lf.AnimationName ?? "") : "") +
                    " fileRef=" + (lf != null ? lf.Frame.FileRef.ToString(CultureInfo.InvariantCulture) : "-") +
                    " sprite=" + (lf != null ? lf.Frame.SpriteId.ToString(CultureInfo.InvariantCulture) : "-") +
                    " tex=" + (t != null ? (t.name ?? "") : "<null>") +
                    " size=" + (t != null ? (t.width.ToString(CultureInfo.InvariantCulture) + "x" + t.height.ToString(CultureInfo.InvariantCulture)) : "0x0") +
                    " rgbaHash=0x" + hash);
            }
            if (loadedFrames.Count > lim) parts.Add("...+" + (loadedFrames.Count - lim).ToString(CultureInfo.InvariantCulture));
            return string.Join(" ; ", parts.ToArray());
        }

        private static string C2Settlement3InuMdV2TextureHash32V49LikeOriginal(Texture2D tex)
        {
            if (tex == null) return "00000000";
            try
            {
                Color32[] px = tex.GetPixels32();
                unchecked
                {
                    uint h = 2166136261u;
                    h = (h ^ (uint)tex.width) * 16777619u;
                    h = (h ^ (uint)tex.height) * 16777619u;
                    for (int i = 0; i < px.Length; i++)
                    {
                        Color32 c = px[i];
                        h = (h ^ c.r) * 16777619u;
                        h = (h ^ c.g) * 16777619u;
                        h = (h ^ c.b) * 16777619u;
                        h = (h ^ c.a) * 16777619u;
                    }
                    return h.ToString("X8", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception e)
            {
                return "ERR_" + e.GetType().Name;
            }
        }

        private static C2Settlement3InuMdV2Info C2Settlement3InuMdV2ResolveMdLikeOriginal(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) monsterId = "<empty>";
            C2Settlement3InuMdV2Info cached;
            if (Settlement3InuMdV2MdCache.TryGetValue(monsterId, out cached)) return cached;

            var info = new C2Settlement3InuMdV2Info();
            info.MdName = monsterId;
            info.Kind = C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(monsterId);
            string path = C2Settlement3InuMdV2FindMdLikeOriginal(monsterId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                info.Found = false;
                info.Audit = "md_not_found";
                Settlement3InuMdV2MdCache[monsterId] = info;
                return info;
            }

            info.Found = true;
            info.MdPath = path;
            C2Settlement3InuMdV2ParseMdLikeOriginal(info, path);
            if (info.Kind == C2Settlement3InuMdV2Kind.Unknown) info.Kind = C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(monsterId);
            Settlement3InuMdV2MdCache[monsterId] = info;
            return info;
        }

        private static string C2Settlement3InuMdV2FindMdLikeOriginal(string monsterId)
        {
            var roots = C2Settlement3InuMdV2DataRootsLikeOriginal();
            var names = C2Settlement3InuMdV2NameCandidatesLikeOriginal(monsterId);
            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                for (int n = 0; n < names.Count; n++)
                {
                    string name = names[n];
                    string[] rels = {
                        name + ".md",
                        Path.Combine("UnitsMD", name + ".md"),
                        Path.Combine("UnitsMD", "Units", name + ".md"),
                        Path.Combine("Units", name + ".md"),
                        Path.Combine("Missions", name + ".md"),
                        Path.Combine("Nation", name + ".md"),
                        Path.Combine("Data", name + ".md")
                    };
                    for (int k = 0; k < rels.Length; k++)
                    {
                        string p = Path.Combine(root, rels[k]);
                        if (File.Exists(p)) return p;
                    }

                    // Unity project layout support:
                    // Assets/Resources/UnitsMD may be a flat or nested dump of original *.md files.
                    try
                    {
                        string unitsMd = Path.Combine(root, "UnitsMD");
                        if (Directory.Exists(unitsMd))
                        {
                            string[] found = Directory.GetFiles(unitsMd, name + ".md", SearchOption.AllDirectories);
                            if (found != null && found.Length > 0) return found[0];
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        private static List<string> C2Settlement3InuMdV2NameCandidatesLikeOriginal(string monsterId)
        {
            var list = new List<string>();
            Action<string> add = s => { if (!string.IsNullOrEmpty(s) && !list.Exists(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase))) list.Add(s); };
            string raw = (monsterId ?? "").Trim();
            int p = raw.IndexOf('(');
            string baseName = p > 0 ? raw.Substring(0, p).Trim() : raw;
            string suffix = "";
            int p2 = raw.IndexOf(')');
            if (p >= 0 && p2 > p) suffix = raw.Substring(p + 1, p2 - p - 1).Trim();

            // V50: first trust original Nation *.NDS UnitID -> MD mapping.
            // France.NDS says, for example: BldRudCoal(FR) -> BldRudSel.
            // This is the same logical indirection the original engine has after LoadAllNations.
            string ndsAlias = C2Settlement3InuMdV2ResolveNdsMdAliasV50LikeOriginal(raw);
            if (!string.IsNullOrEmpty(ndsAlias)) add(ndsAlias);

            // Fallback only when no NDS mapping is available.
            string strictMine = C2Settlement3InuMdV2StrictMineAliasMdNameLikeOriginal(baseName);
            if (!string.IsNullOrEmpty(strictMine)) add(strictMine);

            add(raw);
            add(baseName);

            if (!string.IsNullOrEmpty(suffix))
            {
                add(baseName + suffix);
                add(baseName + "_" + suffix);
                add(suffix + baseName);
                add(suffix + "_" + baseName);

                // SaveNewMap stores logical IDs like BldMel(FR), but MD dump may contain
                // nation-specific files like FrnMel.md / RusMel.md.
                string nat = C2Settlement3InuMdV2NationPrefixLikeOriginal(suffix);
                if (!string.IsNullOrEmpty(nat) && string.Equals(baseName, "BldMel", StringComparison.OrdinalIgnoreCase))
                {
                    add(nat + "Mel");
                    add(nat + "MelN");
                    add("N" + nat + "Mel");
                }
            }

            // Keep explicit aliases too, after the strict first candidate.
            if (string.Equals(baseName, "BldRudCoal", StringComparison.OrdinalIgnoreCase)) { add("BldRudSel"); add("BldRudCoal"); }
            if (string.Equals(baseName, "BldRudIron", StringComparison.OrdinalIgnoreCase)) { add("BldRudRud"); }
            if (string.Equals(baseName, "BldRudGold", StringComparison.OrdinalIgnoreCase)) { add("BldRudGln"); }
            if (string.Equals(baseName, "BldRudStone", StringComparison.OrdinalIgnoreCase) || string.Equals(baseName, "BldRudSton", StringComparison.OrdinalIgnoreCase)) { add("BldRudKam"); }
            if (string.Equals(baseName, "BldRudSel", StringComparison.OrdinalIgnoreCase)) add("BldRudSel");
            if (string.Equals(baseName, "BldRudGln", StringComparison.OrdinalIgnoreCase)) add("BldRudGln");
            if (string.Equals(baseName, "BldRudUgl", StringComparison.OrdinalIgnoreCase)) add("BldRudUgl");
            if (string.Equals(baseName, "BldRudRud", StringComparison.OrdinalIgnoreCase)) add("BldRudRud");
            if (string.Equals(baseName, "BldRudKam", StringComparison.OrdinalIgnoreCase)) add("BldRudKam");

            add(C2Settlement3InuMdV2SanitizeNameLikeOriginal(raw));
            add(C2Settlement3InuMdV2SanitizeNameLikeOriginal(baseName));
            return list;
        }

        private static string C2Settlement3InuMdV2MineMdAliasForAuditV50LikeOriginal(string monsterIdOrBaseName)
        {
            string nds = C2Settlement3InuMdV2ResolveNdsMdAliasV50LikeOriginal(monsterIdOrBaseName);
            if (!string.IsNullOrEmpty(nds) && C2Settlement3InuMdV2LooksLikeMineMdNameV50LikeOriginal(nds)) return nds;
            return C2Settlement3InuMdV2StrictMineAliasMdNameLikeOriginal(monsterIdOrBaseName);
        }

        private static bool C2Settlement3InuMdV2LooksLikeMineMdNameV50LikeOriginal(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string s = name.Trim();
            int p = s.IndexOf('(');
            if (p > 0) s = s.Substring(0, p).Trim();
            return s.IndexOf("BldRud", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string C2Settlement3InuMdV2ResolveNdsMdAliasV50LikeOriginal(string monsterId)
        {
            if (string.IsNullOrWhiteSpace(monsterId)) return "";
            C2Settlement3InuMdV2EnsureNdsAliasMapV50LikeOriginal();
            if (s_C2Settlement3InuMdV2NdsUnitToMdV50 == null || s_C2Settlement3InuMdV2NdsUnitToMdV50.Count == 0) return "";

            string raw = monsterId.Trim();
            string md;
            if (s_C2Settlement3InuMdV2NdsUnitToMdV50.TryGetValue(raw, out md)) return md;

            // Some saved maps use spacing/case variations. Try sanitized exact key as last resort.
            string clean = C2Settlement3InuMdV2SanitizeNameLikeOriginal(raw);
            if (!string.IsNullOrEmpty(clean) && !string.Equals(clean, raw, StringComparison.OrdinalIgnoreCase) &&
                s_C2Settlement3InuMdV2NdsUnitToMdV50.TryGetValue(clean, out md)) return md;

            return "";
        }

        private static string C2Settlement3InuMdV2NdsResourceAliasAuditV50LikeOriginal(List<C2Settlement3InuMdV2Record> records)
        {
            C2Settlement3InuMdV2EnsureNdsAliasMapV50LikeOriginal();
            var entries = new List<string>();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    string id = records[i].MonsterId ?? "";
                    if (id.IndexOf("BldRud", StringComparison.OrdinalIgnoreCase) < 0 &&
                        id.IndexOf("BldMel", StringComparison.OrdinalIgnoreCase) < 0 &&
                        id.IndexOf("BldLes", StringComparison.OrdinalIgnoreCase) < 0) continue;

                    string alias = C2Settlement3InuMdV2ResolveNdsMdAliasV50LikeOriginal(id);
                    if (string.IsNullOrEmpty(alias)) alias = C2Settlement3InuMdV2StrictMineAliasMdNameLikeOriginal(id);
                    if (!string.IsNullOrEmpty(alias)) C2Settlement3InuMdV2Count(counts, alias);
                    if (entries.Count < 64)
                    {
                        entries.Add("#" + records[i].Index.ToString(CultureInfo.InvariantCulture) + " " + id +
                            " -> " + (string.IsNullOrEmpty(alias) ? "<no_nds_alias>" : alias) +
                            " map=(" + (records[i].RealX >> 4).ToString(CultureInfo.InvariantCulture) + "," + (records[i].RealY >> 4).ToString(CultureInfo.InvariantCulture) + ")");
                    }
                }
            }
            return "nds=" + s_C2Settlement3InuMdV2NdsAuditV50 +
                " resolvedResourceRecords=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                " mdCounts=" + C2Settlement3InuMdV2TopNamesLikeOriginal(counts, 64) +
                " entries=" + string.Join(" | ", entries.ToArray());
        }

        private static void C2Settlement3InuMdV2EnsureNdsAliasMapV50LikeOriginal()
        {
            if (s_C2Settlement3InuMdV2NdsUnitToMdV50 != null) return;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = C2Settlement3InuMdV2DataRootsLikeOriginal();
            for (int i = 0; i < roots.Count; i++)
            {
                C2Settlement3InuMdV2AddNdsSearchDirV50LikeOriginal(dirs, roots[i]);
                try
                {
                    var di = new DirectoryInfo(roots[i]);
                    if (di.Exists && di.Parent != null)
                        C2Settlement3InuMdV2AddNdsSearchDirV50LikeOriginal(dirs, di.Parent.FullName);
                }
                catch { }
            }

            foreach (string dir in dirs)
            {
                try
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                    string[] a = Directory.GetFiles(dir, "*.NDS", SearchOption.TopDirectoryOnly);
                    for (int i = 0; a != null && i < a.Length; i++) files.Add(a[i]);
                    string[] b = Directory.GetFiles(dir, "*.nds", SearchOption.TopDirectoryOnly);
                    for (int i = 0; b != null && i < b.Length; i++) files.Add(b[i]);
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

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = C2Settlement3InuMdV2StripCommentLikeOriginal(lines[i]).Trim();
                    if (line.Length == 0 || line[0] == '/') continue;
                    string[] t = C2Settlement3InuMdV2SplitTokensLikeOriginal(line);
                    if (t == null || t.Length < 2) continue;

                    string unitId = (t[0] ?? "").Trim();
                    string mdName = (t[1] ?? "").Trim();
                    if (unitId.Length == 0 || mdName.Length == 0) continue;
                    if (unitId.IndexOf('(') < 0 || unitId.IndexOf(')') < 0) continue;
                    if (mdName.IndexOf('(') >= 0 || mdName.IndexOf(')') >= 0) continue;
                    if (mdName.IndexOf('%') >= 0 || mdName.IndexOf('=') >= 0) continue;
                    int dummy;
                    if (int.TryParse(mdName, NumberStyles.Integer, CultureInfo.InvariantCulture, out dummy)) continue;
                    if (string.Equals(mdName, "GRP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(mdName, "LIFE", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(mdName, "BUILD", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!map.ContainsKey(unitId)) map.Add(unitId, mdName);
                    linesParsed++;
                }
            }

            s_C2Settlement3InuMdV2NdsUnitToMdV50 = map;
            s_C2Settlement3InuMdV2NdsAuditV50 = "files=" + files.Count.ToString(CultureInfo.InvariantCulture) +
                " dirs=" + dirs.Count.ToString(CultureInfo.InvariantCulture) +
                " aliases=" + map.Count.ToString(CultureInfo.InvariantCulture) +
                " parsedLines=" + linesParsed.ToString(CultureInfo.InvariantCulture);
        }

        private static void C2Settlement3InuMdV2AddNdsSearchDirV50LikeOriginal(HashSet<string> dirs, string path)
        {
            if (dirs == null || string.IsNullOrWhiteSpace(path)) return;
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
                    if (di.Parent != null) dirs.Add(di.Parent.FullName);
                }
            }
            catch { }
        }

        private static string C2Settlement3InuMdV2StrictMineAliasMdNameLikeOriginal(string monsterIdOrBaseName)
        {
            if (string.IsNullOrWhiteSpace(monsterIdOrBaseName)) return "";
            string s = monsterIdOrBaseName.Trim();
            int p = s.IndexOf('(');
            if (p > 0) s = s.Substring(0, p).Trim();

            // Fallback names follow original *.NDS conventions:
            // BldRudCoal -> BldRudSel, while BldRudUgl is the real coal/ugol MD.
            if (string.Equals(s, "BldRudCoal", StringComparison.OrdinalIgnoreCase))
                return "BldRudSel";

            if (string.Equals(s, "BldRudUgl", StringComparison.OrdinalIgnoreCase))
                return "BldRudUgl";

            if (string.Equals(s, "BldRudIron", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudRud", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudOre", StringComparison.OrdinalIgnoreCase))
                return "BldRudRud";

            if (string.Equals(s, "BldRudGold", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudGln", StringComparison.OrdinalIgnoreCase))
                return "BldRudGln";

            if (string.Equals(s, "BldRudSel", StringComparison.OrdinalIgnoreCase))
                return "BldRudSel";

            if (string.Equals(s, "BldRudStone", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudSton", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "BldRudKam", StringComparison.OrdinalIgnoreCase))
                return "BldRudKam";

            return "";
        }

        private static string C2Settlement3InuMdV2NationPrefixLikeOriginal(string suffix)
        {
            string s = (suffix ?? "").Trim().ToUpperInvariant();
            if (s == "FR" || s == "FRA" || s == "SFR") return "Frn";
            if (s == "RU" || s == "RUS" || s == "DR") return "Rus";
            if (s == "EN" || s == "ENG") return "Eng";
            if (s == "AU" || s == "AUS") return "Aus";
            if (s == "EG" || s == "EGP") return "Egp";
            if (s == "PR" || s == "PRU") return "Pru";
            return "";
        }

        private static void C2Settlement3InuMdV2ParseMdLikeOriginal(C2Settlement3InuMdV2Info info, string path)
        {
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch { lines = File.ReadAllLines(path); }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = C2Settlement3InuMdV2StripCommentLikeOriginal(lines[i]).Trim();
                if (line.Length == 0) continue;

                // In .md files a leading '/' disables old/alternate animation directives:
                // /#WORK, /@PSTAND2, /TAKERESSTAGES, etc. V12 accidentally treated them as active.
                if (line[0] == '/') continue;

                string[] t = C2Settlement3InuMdV2SplitTokensLikeOriginal(line);
                if (t.Length == 0) continue;
                string cmdRaw = t[0].ToUpperInvariant();
                string cmd = cmdRaw.StartsWith("/", StringComparison.Ordinal) ? cmdRaw.Substring(1) : cmdRaw;

                if (cmd == "BUILDING")
                {
                    info.Building = true;
                    if (info.Kind == C2Settlement3InuMdV2Kind.Unknown) info.Kind = C2Settlement3InuMdV2Kind.Building;
                }
                else if (cmd == "SPRITEOBJECT")
                {
                    info.SpriteObject = true;
                    info.Building = true;
                    info.Kind = C2Settlement3InuMdV2Kind.SpriteObject;
                }
                else if (cmd == "PEASANT")
                {
                    info.Peasant = true;
                    if (info.Kind == C2Settlement3InuMdV2Kind.Unknown) info.Kind = C2Settlement3InuMdV2Kind.Unit;
                }
                else if (cmd == "NOTSELECTABLE")
                {
                    info.NotSelectable = true;
                }
                else if (cmd == "UNITRADIUS" && t.Length >= 2)
                {
                    info.UnitRadius = Mathf.Max(0, C2Settlement3InuMdV2ToInt(t[1]));
                    if (info.Kind == C2Settlement3InuMdV2Kind.Unknown) info.Kind = C2Settlement3InuMdV2Kind.Unit;
                }
                else if (cmd == "UNITABSORBER" && t.Length >= 2)
                {
                    info.UnitAbsorber = true;
                    info.MaxInside = Mathf.Max(0, C2Settlement3InuMdV2ToInt(t[1]));
                }
                else if (cmd == "PEASANTABSORBER" && t.Length >= 2)
                {
                    info.PeasantAbsorber = true;
                    info.MaxInside = Mathf.Max(0, C2Settlement3InuMdV2ToInt(t[1]));
                }
                else if (cmd == "PRODUCER")
                {
                    info.Producer = true;
                    if (t.Length >= 5)
                    {
                        info.FreeAdd = C2Settlement3InuMdV2ToInt(t[t.Length - 2]);
                        info.PeasantAdd = C2Settlement3InuMdV2ToInt(t[t.Length - 1]);
                    }
                }
                else if (cmd == "USAGE" && t.Length >= 2)
                {
                    info.Usage = t[1];
                    string u = t[1].ToUpperInvariant();
                    if (u.IndexOf("MELN") >= 0 || u.IndexOf("MINE") >= 0 || u.IndexOf("RUD") >= 0 || u.IndexOf("SKLAD") >= 0 || u.IndexOf("WOOD") >= 0 || u.IndexOf("LES") >= 0)
                        info.Kind = C2Settlement3InuMdV2Kind.ResourceBuilding;
                    else if (u.IndexOf("PEASANT") >= 0 || u.IndexOf("PUSHKA") >= 0 || u.IndexOf("GRENADER") >= 0 || u.IndexOf("LIGHTINF") >= 0 || u.IndexOf("STRELOK") >= 0 || u.IndexOf("HORSE") >= 0)
                        info.Kind = C2Settlement3InuMdV2Kind.Unit;
                }
                else if (cmd == "SETANMPARAM" && t.Length >= 5)
                {
                    info.SetAnmParamDx = C2Settlement3InuMdV2ToInt(t[1]);
                    info.SetAnmParamDy = C2Settlement3InuMdV2ToInt(t[2]);
                    info.SetAnmParamParts = C2Settlement3InuMdV2ToInt(t[3]);
                    info.SetAnmParamPartSize = C2Settlement3InuMdV2ToInt(t[4]);
                }
                else if (cmd == "BUILDSTAGES" && t.Length >= 2)
                {
                    info.BuildStages = C2Settlement3InuMdV2ToInt(t[1]);
                }
                else if (cmd == "DESTRUCT" && t.Length >= 2)
                {
                    info.DestructRaw = line;
                }
                else if (cmd == "ALIGN_WITH_3POINTS" && t.Length >= 10)
                {
                    info.AlignPt1x = C2Settlement3InuMdV2ToInt(t[1]);
                    info.AlignPt1y = C2Settlement3InuMdV2ToInt(t[2]);
                    info.AlignPt1z = C2Settlement3InuMdV2ToInt(t[3]);
                    info.AlignPt2x = C2Settlement3InuMdV2ToInt(t[4]);
                    info.AlignPt2y = C2Settlement3InuMdV2ToInt(t[5]);
                    info.AlignPt2z = C2Settlement3InuMdV2ToInt(t[6]);
                    info.AlignPt3x = C2Settlement3InuMdV2ToInt(t[7]);
                    info.AlignPt3y = C2Settlement3InuMdV2ToInt(t[8]);
                    info.AlignPt3z = C2Settlement3InuMdV2ToInt(t[9]);
                    info.Use3pAlign = true;
                }
                else if (cmd == "LINESORT" && t.Length >= 2)
                {
                    string animName = C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal(t[1]);
                    C2Settlement3InuMdV2Animation anim = C2Settlement3InuMdV2GetOrCreateAnimationLikeOriginal(info, animName, 1);
                    int expected = anim.Frames != null && anim.Frames.Count > 0 ? anim.Frames.Count : 256;
                    anim.LineSort.Clear();

                    // Original Gscanf reads exactly NANM->NFrames sort descriptors from this token stream.
                    C2Settlement3InuMdV2ParseLineSortTokensLikeOriginal(anim.LineSort, t, 2, expected);

                    int j = i + 1;
                    for (; j < lines.Length && anim.LineSort.Count < expected; j++)
                    {
                        string rawSort = C2Settlement3InuMdV2StripCommentLikeOriginal(lines[j]).Trim();
                        if (rawSort.Length == 0)
                        {
                            if (anim.LineSort.Count > 0) break;
                            continue;
                        }

                        if (rawSort[0] == '/') continue;

                        string[] st = C2Settlement3InuMdV2SplitTokensLikeOriginal(rawSort);
                        if (st.Length == 0) continue;

                        int before = anim.LineSort.Count;
                        C2Settlement3InuMdV2ParseLineSortTokensLikeOriginal(anim.LineSort, st, 0, expected);
                        if (anim.LineSort.Count == before) break;
                    }

                    C2Settlement3InuMdV2PostProcessLineSortLikeOriginal(anim.LineSort);
                    if (string.Equals(animName, "#STANDLO", StringComparison.OrdinalIgnoreCase))
                    {
                        info.StandLoLineSort.Clear();
                        info.StandLoLineSort.AddRange(anim.LineSort);
                    }
                    i = Math.Max(i, j - 1);
                }
                else if (cmd == "LOCATION" && t.Length >= 5)
                {
                    info.PicDx = C2Settlement3InuMdV2ToInt(t[1]);
                    info.PicDy = C2Settlement3InuMdV2ToInt(t[2]);
                    info.PicLx = C2Settlement3InuMdV2ToInt(t[3]);
                    info.PicLy = C2Settlement3InuMdV2ToInt(t[4]);
                }
                else if ((cmd == "USERLC" || cmd == "USERLCEXT") && t.Length >= 6)
                {
                    int shift = cmd == "USERLCEXT" ? 2 : 0;
                    // USERLC    fileRef package stage dx dy
                    // USERLCEXT fileRef extra0 extra1 package stage dx dy
                    if (t.Length >= 6 + shift)
                    {
                        int fileRef = C2Settlement3InuMdV2ToInt(t[1]);
                        string pkg = C2Settlement3InuMdV2CleanPackageNameLikeOriginal(t[2 + shift]);
                        int dx = C2Settlement3InuMdV2ToInt(t[4 + shift]);
                        int dy = C2Settlement3InuMdV2ToInt(t[5 + shift]);
                        if (string.IsNullOrEmpty(info.Package)) info.Package = pkg;
                        info.RlcPackages[fileRef] = pkg;
                        info.RlcDx[fileRef] = dx;
                        info.RlcDy[fileRef] = dy;
                        info.Dx = dx;
                        info.Dy = dy;
                        info.HasUserLc = true;
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '#')
                {
                    if (t.Length >= 3)
                    {
                        int rotations = Math.Max(1, C2Settlement3InuMdV2ToInt(t[1]));
                        int frames = Math.Max(0, C2Settlement3InuMdV2ToInt(t[2]));
                        string animName = C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal(cmdRaw);
                        C2Settlement3InuMdV2Animation anim = C2Settlement3InuMdV2GetOrCreateAnimationLikeOriginal(info, animName, rotations);
                        anim.Rotations = rotations;
                        anim.Frames.Clear();
                        for (int q = 0; q < frames; q++)
                        {
                            int a = 3 + q * 2;
                            if (a + 1 >= t.Length) break;
                            anim.Frames.Add(new C2Settlement3InuMdV2AnimFrame(C2Settlement3InuMdV2ToInt(t[a]), C2Settlement3InuMdV2ToInt(t[a + 1])));
                        }

                        if (string.Equals(animName, "#WORK", StringComparison.OrdinalIgnoreCase))
                        {
                            info.WorkFrames.Clear();
                            info.WorkFrames.AddRange(anim.Frames);
                        }
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '@')
                {
                    if (t.Length >= 5)
                    {
                        int rotations = Math.Max(1, C2Settlement3InuMdV2ToInt(t[1]));
                        int fileRef = C2Settlement3InuMdV2ToInt(t[2]);
                        int startFrame = C2Settlement3InuMdV2ToInt(t[3]);
                        int endFrame = C2Settlement3InuMdV2ToInt(t[4]);
                        string animName = C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal("#" + cmdRaw.Substring(1));
                        C2Settlement3InuMdV2Animation anim = C2Settlement3InuMdV2GetOrCreateAnimationLikeOriginal(info, animName, rotations);
                        anim.Rotations = rotations;
                        anim.Frames.Clear();
                        int step = startFrame > endFrame ? -1 : 1;
                        for (int fr = startFrame; ; fr += step)
                        {
                            anim.Frames.Add(new C2Settlement3InuMdV2AnimFrame(fileRef, fr));
                            if (fr == endFrame || anim.Frames.Count >= 2048) break;
                        }

                        if (string.Equals(animName, "#WORK", StringComparison.OrdinalIgnoreCase))
                        {
                            info.WorkFrames.Clear();
                            info.WorkFrames.AddRange(anim.Frames);
                        }
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '$')
                {
                    if (t.Length >= 3)
                    {
                        int rotations = Math.Max(1, C2Settlement3InuMdV2ToInt(t[1]));
                        int parts = Math.Max(0, C2Settlement3InuMdV2ToInt(t[2]));
                        var allTokens = new List<string>(t);
                        int consumedLines = 0;
                        int required = 3 + parts * 3;
                        while (allTokens.Count < required && i + 1 + consumedLines < lines.Length)
                        {
                            string raw = C2Settlement3InuMdV2StripCommentLikeOriginal(lines[i + 1 + consumedLines]).Trim();
                            if (raw.Length == 0) { consumedLines++; continue; }
                            if (raw[0] == '/') { consumedLines++; continue; }
                            string[] more = C2Settlement3InuMdV2SplitTokensLikeOriginal(raw);
                            if (more.Length == 0) { consumedLines++; continue; }
                            if (!C2Settlement3InuMdV2LooksLikeIntLikeOriginal(more[0])) break;
                            allTokens.AddRange(more);
                            consumedLines++;
                        }

                        string animName = C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal("#" + cmdRaw.Substring(1));
                        C2Settlement3InuMdV2Animation anim = C2Settlement3InuMdV2GetOrCreateAnimationLikeOriginal(info, animName, rotations);
                        anim.Rotations = rotations;
                        anim.Frames.Clear();
                        int p = 3;
                        for (int q = 0; q < parts && p + 2 < allTokens.Count; q++, p += 3)
                        {
                            int fileRef = C2Settlement3InuMdV2ToInt(allTokens[p]);
                            int startFrame = C2Settlement3InuMdV2ToInt(allTokens[p + 1]);
                            int endFrame = C2Settlement3InuMdV2ToInt(allTokens[p + 2]);
                            int step = startFrame > endFrame ? -1 : 1;
                            for (int fr = startFrame; ; fr += step)
                            {
                                anim.Frames.Add(new C2Settlement3InuMdV2AnimFrame(fileRef, fr));
                                if (fr == endFrame || anim.Frames.Count >= 4096) break;
                            }
                        }
                        i += consumedLines;
                    }
                }
            }

            C2Settlement3InuMdV2Animation stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(info, "#STANDLO");
            if (stand == null) stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(info, "#STAND");
            if (stand == null) stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(info, "#STAND1");
            if (stand == null) stand = C2Settlement3InuMdV2FindFirstSafeBaseAnimationLikeOriginal(info);
            C2Settlement3InuMdV2ApplyBaseAnimationLikeOriginal(info, stand);

            if (info.Kind == C2Settlement3InuMdV2Kind.Unknown)
            {
                if (info.Building) info.Kind = C2Settlement3InuMdV2Kind.Building;
                else info.Kind = C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(info.MdName);
            }
            if (info.Kind == C2Settlement3InuMdV2Kind.Unit || info.Kind == C2Settlement3InuMdV2Kind.Animal) info.PreferredExt = ".g2d";
            else info.PreferredExt = ".g16";
            info.Audit = "pkg=" + (info.Package ?? "<none>") + " frame=" + info.SpriteId + " kind=" + info.Kind + " building=" + info.Building +
                         " peasant=" + info.Peasant + " notSelectable=" + info.NotSelectable +
                         " unitRadius=" + info.UnitRadius.ToString(CultureInfo.InvariantCulture) +
                         " usage=" + (info.Usage ?? "") + " loc=" + info.PicDx + "," + info.PicDy + "," + info.PicLx + "," + info.PicLy + " standLoParts=" + (info.StandLoFrames != null ? info.StandLoFrames.Count : 0) +
                         " workParts=" + (info.WorkFrames != null ? info.WorkFrames.Count : 0) +
                         " animations=" + (info.Animations != null ? info.Animations.Count : 0) +
                         " lineSort=" + (info.StandLoLineSort != null ? info.StandLoLineSort.Count : 0) +
                         " setAnm=" + info.SetAnmParamDx + "," + info.SetAnmParamDy + "," + info.SetAnmParamParts + "," + info.SetAnmParamPartSize +
                         " align3p=" + info.Use3pAlign;
        }

        private static string C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal(string name)
        {
            string s = (name ?? string.Empty).Trim();
            if (s.Length == 0) return "#";
            if (s[0] == '@' || s[0] == '$') s = "#" + s.Substring(1);
            if (s[0] != '#') s = "#" + s;
            return s.ToUpperInvariant();
        }

        private static C2Settlement3InuMdV2Animation C2Settlement3InuMdV2GetOrCreateAnimationLikeOriginal(C2Settlement3InuMdV2Info info, string name, int rotations)
        {
            if (info == null) return null;
            string key = C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal(name);
            C2Settlement3InuMdV2Animation anim;
            if (!info.Animations.TryGetValue(key, out anim) || anim == null)
            {
                anim = new C2Settlement3InuMdV2Animation();
                anim.Name = key;
                info.Animations[key] = anim;
            }
            if (rotations > 0) anim.Rotations = rotations;
            return anim;
        }

        private static C2Settlement3InuMdV2Animation C2Settlement3InuMdV2FindAnimationLikeOriginal(C2Settlement3InuMdV2Info info, string name)
        {
            if (info == null || info.Animations == null) return null;
            C2Settlement3InuMdV2Animation anim;
            return info.Animations.TryGetValue(C2Settlement3InuMdV2NormalizeAnimationNameLikeOriginal(name), out anim) && anim != null && anim.Frames.Count > 0 ? anim : null;
        }

        private static C2Settlement3InuMdV2Animation C2Settlement3InuMdV2FindFirstSafeBaseAnimationLikeOriginal(C2Settlement3InuMdV2Info info)
        {
            if (info == null || info.Animations == null) return null;
            foreach (var kv in info.Animations)
            {
                C2Settlement3InuMdV2Animation anim = kv.Value;
                if (anim == null || anim.Frames.Count == 0) continue;
                string n = anim.Name ?? string.Empty;
                if (n.IndexOf("DEATH", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.IndexOf("DIE", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.IndexOf("BUILD", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.IndexOf("WORK", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                return anim;
            }
            return null;
        }

        private static void C2Settlement3InuMdV2ApplyBaseAnimationLikeOriginal(C2Settlement3InuMdV2Info info, C2Settlement3InuMdV2Animation anim)
        {
            if (info == null || anim == null || anim.Frames.Count == 0) return;
            info.Rotations = Math.Max(1, anim.Rotations);
            info.StandLoFrames.Clear();
            info.StandLoFrames.AddRange(anim.Frames);
            info.StandLoLineSort.Clear();
            if (anim.LineSort != null) info.StandLoLineSort.AddRange(anim.LineSort);
            info.SpriteId = anim.Frames[0].SpriteId;
            info.ParsedAnimation = true;
        }

        private static C2Settlement3InuMdV2Animation C2Settlement3InuMdV2SelectBuildingAnimationForRecordLikeOriginal(C2Settlement3InuMdV2Info info, C2Settlement3InuMdV2Record r)
        {
            if (info == null) return null;

            // SaveUnits3 never writes Sdoxlo objects, and original CreateNewUnitAt3 does not
            // use saved Life to select death animations for NewBuilding. Life==0 in 3INU must
            // therefore not switch a completed house to #DEATHLIE/#DEATH sprites.
            if (r.Stage > 0x8000)
            {
                C2Settlement3InuMdV2Animation build = C2Settlement3InuMdV2FindBuildAnimationLikeOriginal(info, 0xFFFF - r.Stage);
                if (build != null) return build;
            }

            C2Settlement3InuMdV2Animation stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(info, "#STANDLO");
            if (stand == null) stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(info, "#STAND");
            if (stand == null) stand = C2Settlement3InuMdV2FindFirstSafeBaseAnimationLikeOriginal(info);
            return stand;
        }

        private static C2Settlement3InuMdV2Animation C2Settlement3InuMdV2FindBuildAnimationLikeOriginal(C2Settlement3InuMdV2Info info, int stage)
        {
            if (info == null || info.Animations == null) return null;
            C2Settlement3InuMdV2Animation first = null;
            C2Settlement3InuMdV2Animation best = null;
            int bestDistance = int.MaxValue;
            int denom = info.BuildStages > 0 ? info.BuildStages : 64;
            int wanted = Mathf.Clamp((stage * 4) / Math.Max(1, denom), 0, 3);

            foreach (var kv in info.Animations)
            {
                C2Settlement3InuMdV2Animation anim = kv.Value;
                if (anim == null || anim.Frames.Count == 0) continue;
                string n = anim.Name ?? string.Empty;
                if (n.IndexOf("#BUILDLO", StringComparison.OrdinalIgnoreCase) != 0) continue;
                if (first == null) first = anim;
                int suffix = C2Settlement3InuMdV2AnimationNumericSuffixLikeOriginal(n);
                if (suffix < 0) suffix = 0;
                int d = Math.Abs(suffix - wanted);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = anim;
                }
            }

            return best ?? first;
        }

        private static int C2Settlement3InuMdV2AnimationNumericSuffixLikeOriginal(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            int p = name.LastIndexOf('_');
            if (p < 0 || p + 1 >= name.Length) return -1;
            return C2Settlement3InuMdV2ToInt(name.Substring(p + 1));
        }

        private bool C2Settlement3InuMdV2TryLoadVisualFramesLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind, out List<C2Settlement3InuMdV2LoadedFrame> loadedFrames, out string audit)
        {
            loadedFrames = new List<C2Settlement3InuMdV2LoadedFrame>();
            audit = string.Empty;
            if (md == null || !md.Found || string.IsNullOrEmpty(md.Package)) { audit = "no_md_or_package"; return false; }

            bool compositeBuilding = kind == C2Settlement3InuMdV2Kind.SettlementBuilding || kind == C2Settlement3InuMdV2Kind.Building || kind == C2Settlement3InuMdV2Kind.ResourceBuilding || kind == C2Settlement3InuMdV2Kind.SpriteObject;
            C2Settlement3InuMdV2Animation sourceAnim = compositeBuilding ? C2Settlement3InuMdV2SelectBuildingAnimationForRecordLikeOriginal(md, r) : null;
            List<C2Settlement3InuMdV2AnimFrame> sourceFrames = sourceAnim != null ? sourceAnim.Frames : md.StandLoFrames;
            if (compositeBuilding && sourceFrames != null && sourceFrames.Count > 0)
            {
                var partsAudit = new List<string>();
                bool frnMelLineSortV31 = C2Settlement3InuMdV2IsFrnMelLikeOriginal(md, r);
                int standLimitV28 = sourceFrames.Count;
                if (frnMelLineSortV31)
                    partsAudit.Add("FRNMEL_V32_LINESORT_SORT_ONLY_NO_DEFORM sourceFrames=" + sourceFrames.Count.ToString(CultureInfo.InvariantCulture) + " lineSort=" + (sourceAnim != null && sourceAnim.LineSort != null ? sourceAnim.LineSort.Count : 0).ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < standLimitV28; i++)
                {
                    Texture2D partTex;
                    string partAudit;
                    C2Settlement3InuMdV2AnimFrame frameRef = sourceFrames[i];
                    if (C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frameRef, kind, out partTex, out partAudit) && partTex != null)
                    {
                        var loaded = new C2Settlement3InuMdV2LoadedFrame(partTex, frameRef, sourceAnim != null ? sourceAnim.Name : "#STANDLO", false);
                        if (sourceAnim != null && sourceAnim.LineSort != null && i < sourceAnim.LineSort.Count)
                        {
                            loaded.HasLineSort = true;
                            loaded.LineSort = sourceAnim.LineSort[i];
                        }
                        loadedFrames.Add(loaded);
                        if (partsAudit.Count < 12) partsAudit.Add("OK#" + frameRef.SpriteId + ":" + partAudit);
                    }
                    else
                    {
                        if (partsAudit.Count < 12) partsAudit.Add("MISS#" + frameRef.SpriteId + ":" + partAudit);
                    }
                }
                int workLoaded = 0;
                if (Settlement3InuMdV2DrawWorkStaticPreview && md.WorkFrames != null && md.WorkFrames.Count > 0)
                {
                    // Original changes these frames over time (#WORK/@WORK). Do not bake frame 0 permanently unless explicitly enabled.
                    Texture2D workTex;
                    string workAudit;
                    int wf = md.WorkFrames[0].SpriteId;
                    if (C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, md.WorkFrames[0], kind, out workTex, out workAudit) && workTex != null)
                    {
                        loadedFrames.Add(new C2Settlement3InuMdV2LoadedFrame(workTex, md.WorkFrames[0], "#WORK", true));
                        workLoaded = 1;
                        if (partsAudit.Count < 12) partsAudit.Add("WORK#" + wf + ":" + workAudit);
                    }
                    else
                    {
                        if (partsAudit.Count < 12) partsAudit.Add("WORK_MISS#" + wf + ":" + workAudit);
                    }
                }
                else if (md.WorkFrames != null && md.WorkFrames.Count > 0)
                {
                    if (partsAudit.Count < 12) partsAudit.Add("WORK_SKIP_STATIC_PREVIEW_OFF count=" + md.WorkFrames.Count.ToString(CultureInfo.InvariantCulture));
                }

                audit = "anim=" + (sourceAnim != null ? sourceAnim.Name : "#STANDLO") + " parts=" + loadedFrames.Count.ToString(CultureInfo.InvariantCulture) + "/" + sourceFrames.Count.ToString(CultureInfo.InvariantCulture) + " workFirst=" + workLoaded.ToString(CultureInfo.InvariantCulture) + "/" + (md.WorkFrames != null ? md.WorkFrames.Count : 0).ToString(CultureInfo.InvariantCulture) + " " + string.Join(" || ", partsAudit.ToArray());
                return loadedFrames.Count > 0;
            }

            Texture2D tex;
            string oneAudit;
            int frame = C2Settlement3InuMdV2SpriteFrameLikeOriginal(md, r, kind);
            bool ok = C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frame, kind, out tex, out oneAudit);
            audit = oneAudit;
            if (ok && tex != null) loadedFrames.Add(new C2Settlement3InuMdV2LoadedFrame(tex, new C2Settlement3InuMdV2AnimFrame(0, frame), "#FRAME", false));
            return loadedFrames.Count > 0;
        }

        private bool C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2AnimFrame frameRef, C2Settlement3InuMdV2Kind kind, out Texture2D tex, out string audit)
        {
            string packageOverride = C2Settlement3InuMdV2PackageForFileRefLikeOriginal(md, frameRef.FileRef);
            return C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frameRef.SpriteId, kind, out tex, out audit, packageOverride, frameRef.FileRef);
        }

        private static string C2Settlement3InuMdV2PackageForFileRefLikeOriginal(C2Settlement3InuMdV2Info md, int fileRef)
        {
            if (md != null && md.RlcPackages != null)
            {
                string p;
                if (md.RlcPackages.TryGetValue(fileRef, out p) && !string.IsNullOrEmpty(p)) return p;
            }
            return md != null ? md.Package : null;
        }

        private static string C2Settlement3InuMdV2TextureCacheKeyV54(string abs, string logicalPackage, int fileRef, int frame)
        {
            string a = string.IsNullOrEmpty(abs) ? "" : Path.GetFullPath(abs);
            string p = logicalPackage ?? "";
            // V54 key intentionally does not include UNI3 object index or MD part index:
            // same original file + same logical package + same exact sprite id = same pixels.
            return a + "|pkg=" + p + "|frame=" + frame.ToString(CultureInfo.InvariantCulture) + "|layerCompositeV57_top100";
        }

        private static string C2Settlement3InuMdV2TextureAuditV54(string fileRef, string file, int frame, Texture2D tex, string source, bool cacheHit)
        {
            return "fileRef=" + fileRef + " file=" + (file ?? "") + " frame=" + frame.ToString(CultureInfo.InvariantCulture) +
                   " tex=" + (tex != null ? (tex.name ?? "") : "<null>") +
                   " size=" + (tex != null ? (tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture)) : "0x0") +
                   (Settlement3InuMdV2VerboseAuditV54 ? (" rgbaHash=0x" + C2Settlement3InuMdV2TextureHash32V49LikeOriginal(tex)) : "") +
                   " cache=" + (cacheHit ? "hit" : "miss") + " source=" + (source ?? "");
        }

        private static string C2Settlement3InuMdV2VisualPathCacheKeyV55(string pkg, string mdPath, string[] exts)
        {
            return (pkg ?? "") + "|md=" + (mdPath ?? "") + "|ext=" + string.Join(",", exts ?? new string[0]);
        }

        private static List<string> C2Settlement3InuMdV2VisualCandidatesCachedV55(string pkg, string mdPath, string[] exts, string visualPathKey)
        {
            string cachedPath;
            if (!string.IsNullOrEmpty(visualPathKey) && s_C2Settlement3InuMdV2VisualPathCacheV55.TryGetValue(visualPathKey, out cachedPath) && !string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
            {
                s_C2Settlement3InuMdV2VisualPathCacheHitsV55++;
                return new List<string> { cachedPath };
            }

            s_C2Settlement3InuMdV2VisualPathCacheMissesV55++;
            var files = C2Settlement3InuMdV2VisualCandidatesLikeOriginal(pkg, mdPath, exts);
            C2Settlement3InuMdV2AddIndexedVisualCandidatesLikeOriginal(files, pkg, exts);
            return files;
        }

        private static string C2Settlement3InuMdV2DiskCacheRootV55()
        {
            try
            {
                string projectRoot = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(projectRoot))
                    return Path.Combine(projectRoot, "Library", "C2BridgeCache", "G16Frames");
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(Application.temporaryCachePath))
                    return Path.Combine(Application.temporaryCachePath, "C2BridgeCache", "G16Frames");
            }
            catch { }
            return Path.Combine(Path.GetTempPath(), "C2BridgeCache", "G16Frames");
        }

        private static string C2Settlement3InuMdV2FrameDiskCachePathV55(string abs, string logicalPackage, int fileRef, int frame)
        {
            if (!Settlement3InuMdV2DiskFrameCacheV55 || string.IsNullOrEmpty(abs) || !File.Exists(abs)) return null;
            FileInfo fi = new FileInfo(abs);
            string key = Path.GetFullPath(abs) + "|len=" + fi.Length.ToString(CultureInfo.InvariantCulture) + "|ticks=" + fi.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture) + "|pkg=" + (logicalPackage ?? "") + "|fileRef=" + fileRef.ToString(CultureInfo.InvariantCulture) + "|frame=" + frame.ToString(CultureInfo.InvariantCulture) + "|rgba32_srgb_layerCompositeV57_top100";
            ulong h = C2Settlement3InuMdV2Fnv1a64V55(key);
            string name = h.ToString("X16", CultureInfo.InvariantCulture) + ".rgba55";
            return Path.Combine(C2Settlement3InuMdV2DiskCacheRootV55(), name);
        }

        private static ulong C2Settlement3InuMdV2Fnv1a64V55(string s)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                string v = s ?? string.Empty;
                for (int i = 0; i < v.Length; i++)
                {
                    hash ^= v[i];
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }

        private static Texture2D C2Settlement3InuMdV2TryReadFrameDiskCacheV55(string abs, string logicalPackage, int fileRef, int frame, out string source)
        {
            source = "disk_cache_disabled";
            string path = C2Settlement3InuMdV2FrameDiskCachePathV55(abs, logicalPackage, fileRef, frame);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                s_C2Settlement3InuMdV2DiskCacheMissesV55++;
                source = "disk_cache_miss";
                return null;
            }

            try
            {
                using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    int magic = br.ReadInt32();
                    int version = br.ReadInt32();
                    int w = br.ReadInt32();
                    int h = br.ReadInt32();
                    int len = br.ReadInt32();
                    if (magic != unchecked((int)0xC2F55A01) || version != 1 || w <= 0 || h <= 0 || len < w * h * 4 || len > 268435456)
                    {
                        s_C2Settlement3InuMdV2DiskCacheMissesV55++;
                        source = "disk_cache_bad_header";
                        return null;
                    }
                    byte[] rgba = br.ReadBytes(len);
                    if (rgba == null || rgba.Length < w * h * 4)
                    {
                        s_C2Settlement3InuMdV2DiskCacheMissesV55++;
                        source = "disk_cache_bad_payload";
                        return null;
                    }
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                    tex.name = "C2_BLD_DISKCACHE_" + Path.GetFileNameWithoutExtension(abs) + "_frame_" + frame.ToString(CultureInfo.InvariantCulture);
                    tex.LoadRawTextureData(rgba);
                    tex.Apply(false, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Point;
                    s_C2Settlement3InuMdV2DiskCacheHitsV55++;
                    source = "disk_cache_hit:" + path;
                    return tex;
                }
            }
            catch (Exception ex)
            {
                s_C2Settlement3InuMdV2DiskCacheMissesV55++;
                source = "disk_cache_read_error:" + ex.GetType().Name;
                return null;
            }
        }

        private static void C2Settlement3InuMdV2TryWriteFrameDiskCacheV55(string abs, string logicalPackage, int fileRef, int frame, Texture2D tex)
        {
            if (!Settlement3InuMdV2DiskFrameCacheV55 || tex == null) return;
            string path = C2Settlement3InuMdV2FrameDiskCachePathV55(abs, logicalPackage, fileRef, frame);
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                byte[] raw = tex.GetRawTextureData();
                if (raw == null || raw.Length < tex.width * tex.height * 4) return;
                string tmp = path + ".tmp";
                using (BinaryWriter bw = new BinaryWriter(File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    bw.Write(unchecked((int)0xC2F55A01));
                    bw.Write(1);
                    bw.Write(tex.width);
                    bw.Write(tex.height);
                    bw.Write(raw.Length);
                    bw.Write(raw);
                }
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                s_C2Settlement3InuMdV2DiskCacheWritesV55++;
            }
            catch
            {
                s_C2Settlement3InuMdV2DiskCacheWriteFailsV55++;
            }
        }

        private bool C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(C2Settlement3InuMdV2Info md, int frame, C2Settlement3InuMdV2Kind kind, out Texture2D tex, out string audit)
        {
            return C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frame, kind, out tex, out audit, null, -1);
        }

        private bool C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(C2Settlement3InuMdV2Info md, int frame, C2Settlement3InuMdV2Kind kind, out Texture2D tex, out string audit, string packageOverride, int fileRef)
        {
            tex = null;
            audit = string.Empty;
            string pkg = !string.IsNullOrEmpty(packageOverride) ? packageOverride : (md != null ? md.Package : null);
            if (md == null || string.IsNullOrEmpty(pkg)) { audit = "no_package"; return false; }
            string[] exts = kind == C2Settlement3InuMdV2Kind.Unit || kind == C2Settlement3InuMdV2Kind.Animal
                ? new[] { ".g2d", ".G2D", ".g17", ".G17", ".g16", ".G16" }
                : new[] { ".g17", ".G17", ".g16", ".G16", ".g2d", ".G2D" };
            string visualPathKey = C2Settlement3InuMdV2VisualPathCacheKeyV55(pkg, md.MdPath, exts);
            var files = C2Settlement3InuMdV2VisualCandidatesCachedV55(pkg, md.MdPath, exts, visualPathKey);
            List<string> tried = Settlement3InuMdV2VerboseAuditV54 ? new List<string>() : null;
            for (int i = 0; i < files.Count; i++)
            {
                string p = files[i];
                bool exists = File.Exists(p);
                if (tried != null && i < 12) tried.Add((exists ? "EXISTS:" : "MISS:") + p);
                if (!exists) continue;
                if (!s_C2Settlement3InuMdV2VisualPathCacheV55.ContainsKey(visualPathKey))
                    s_C2Settlement3InuMdV2VisualPathCacheV55[visualPathKey] = p;
                string source;
                try
                {
                    string cacheKey = C2Settlement3InuMdV2TextureCacheKeyV54(p, pkg, fileRef, frame);
                    C2Settlement3InuMdV2TextureCacheEntryV54 cached;
                    if (s_C2Settlement3InuMdV2TextureCacheV54.TryGetValue(cacheKey, out cached) && cached != null && cached.Texture != null)
                    {
                        tex = cached.Texture;
                        s_C2Settlement3InuMdV2TextureCacheHitsV54++;
                        audit = C2Settlement3InuMdV2TextureAuditV54(fileRef.ToString(CultureInfo.InvariantCulture), p, frame, tex, cached.Source, true);
                        return true;
                    }

                    s_C2Settlement3InuMdV2TextureCacheMissesV54++;
                    string diskSource;
                    tex = C2Settlement3InuMdV2TryReadFrameDiskCacheV55(p, pkg, fileRef, frame, out diskSource);
                    if (tex != null)
                    {
                        source = diskSource;
                        var ceDisk = new C2Settlement3InuMdV2TextureCacheEntryV54();
                        ceDisk.Texture = tex;
                        ceDisk.Source = source;
                        ceDisk.Size = tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture);
                        s_C2Settlement3InuMdV2TextureCacheV54[cacheKey] = ceDisk;
                        audit = C2Settlement3InuMdV2TextureAuditV54(fileRef.ToString(CultureInfo.InvariantCulture), p, frame, tex, source, false);
                        return true;
                    }

                    string e = Path.GetExtension(p).ToLowerInvariant();
                    if (e == ".g2d") tex = TryLoadG2DFrameViaMelinojaV3LikeOriginal(p, frame, out source);
                    else tex = TryLoadBuildingGpFrameViaMelinojaV23LikeOriginal(p, frame, out source, pkg);
                    if (tex != null)
                    {
                        tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                        C2Settlement3InuMdV2TryWriteFrameDiskCacheV55(p, pkg, fileRef, frame, tex);
                        var ce = new C2Settlement3InuMdV2TextureCacheEntryV54();
                        ce.Texture = tex;
                        ce.Source = source;
                        ce.Size = tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture);
                        s_C2Settlement3InuMdV2TextureCacheV54[cacheKey] = ce;
                        audit = C2Settlement3InuMdV2TextureAuditV54(fileRef.ToString(CultureInfo.InvariantCulture), p, frame, tex, source, false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    audit = "decode_error fileRef=" + fileRef + " file=" + p + " frame=" + frame + " err=" + ex.Message;
                }
            }
            audit = "visual_not_found fileRef=" + fileRef + " pkg=" + pkg + " frame=" + frame + " tried=" + (tried != null ? string.Join(";", tried.ToArray()) : "audit_disabled");
            return false;
        }

        private bool C2Settlement3InuMdV2TryLoadVisualLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind, out Texture2D tex, out string audit)
        {
            tex = null;
            audit = "";
            if (md == null || !md.Found || string.IsNullOrEmpty(md.Package)) { audit = "no_md_or_package"; return false; }
            int frame = C2Settlement3InuMdV2SpriteFrameLikeOriginal(md, r, kind);
            string[] exts = kind == C2Settlement3InuMdV2Kind.Unit || kind == C2Settlement3InuMdV2Kind.Animal ? new[] { ".g2d", ".G2D", ".g17", ".G17", ".g16", ".G16" } : new[] { ".g17", ".G17", ".g16", ".G16", ".g2d", ".G2D" };
            var files = C2Settlement3InuMdV2VisualCandidatesLikeOriginal(md.Package, md.MdPath, exts);
            C2Settlement3InuMdV2AddIndexedVisualCandidatesLikeOriginal(files, md.Package, exts);
            var tried = new List<string>();
            for (int i = 0; i < files.Count; i++)
            {
                string p = files[i];
                if (i < 12) tried.Add((File.Exists(p) ? "EXISTS:" : "MISS:") + p);
                if (!File.Exists(p)) continue;
                string source;
                try
                {
                    string e = Path.GetExtension(p).ToLowerInvariant();
                    if (e == ".g2d") tex = TryLoadG2DFrameViaMelinojaV3LikeOriginal(p, frame, out source);
                    else tex = TryLoadBuildingGpFrameViaMelinojaV23LikeOriginal(p, frame, out source, md.Package);
                    if (tex != null)
                    {
                        tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                        audit = "file=" + p + " frame=" + frame + " tex=" + (tex != null ? (tex.name ?? "") : "<null>") + " size=" + (tex != null ? (tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture)) : "0x0") + (Settlement3InuMdV2VerboseAuditV54 ? (" rgbaHash=0x" + C2Settlement3InuMdV2TextureHash32V49LikeOriginal(tex)) : "") + " source=" + source;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    audit = "decode_error file=" + p + " frame=" + frame + " err=" + ex.Message;
                }
            }
            audit = "visual_not_found pkg=" + md.Package + " frame=" + frame + " tried=" + string.Join(";", tried.ToArray());
            return false;
        }

        private static int C2Settlement3InuMdV2SpriteFrameLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind)
        {
            int baseSprite = Math.Max(0, md.SpriteId);
            if (kind == C2Settlement3InuMdV2Kind.Unit || kind == C2Settlement3InuMdV2Kind.Animal)
            {
                int rot = Math.Max(1, md.Rotations);
                int dir = (int)Math.Round((r.RealDir & 255) * (rot / 256.0));
                if (dir >= rot) dir = rot - 1;
                // Original units use rotation bank: Rotations * SpriteID + DirectionIndex.
                return Math.Max(0, baseSprite * rot + dir);
            }
            // Original buildings draw stored frame/sprite id directly.
            return baseSprite;
        }

        private static List<string> C2Settlement3InuMdV2VisualCandidatesLikeOriginal(string package, string mdPath, string[] exts)
        {
            var res = new List<string>();
            Action<string> add = p => { if (!string.IsNullOrEmpty(p) && !res.Exists(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) res.Add(p); };
            string pkg = C2Settlement3InuMdV2CleanPackageNameLikeOriginal(package);
            string pkgNoExt = Path.ChangeExtension(pkg, null);
            string flatPkg = (pkgNoExt ?? "").Replace('\\', '_').Replace('/', '_');
            string flatPkgUpper = flatPkg.ToUpperInvariant();
            string barePkg = Path.GetFileName(pkgNoExt ?? "");
            string barePkgUpper = barePkg.ToUpperInvariant();
            var roots = C2Settlement3InuMdV2DataRootsLikeOriginal();

            // V21 hard cache first: original cache uses flat package names in Data\Cash, e.g.
            // UnitsG17\FrnMel -> C:\GSC Game World\Cossacks II\Data\Cash\UNITSG17_FRNMEL.g16.
            for (int e0 = 0; e0 < exts.Length; e0++)
            {
                string ext0 = exts[e0];
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", flatPkgUpper + ext0));
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", flatPkg + ext0));
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", barePkgUpper + ext0));
                add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data\Cash", barePkg + ext0));
            }

            if (!string.IsNullOrEmpty(mdPath)) roots.Insert(0, Path.GetDirectoryName(mdPath));
            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root)) continue;
                for (int e = 0; e < exts.Length; e++)
                {
                    string ext = exts[e];
                    // Original cache flattens package paths: UnitsG17\BldLes -> UNITSG17_BLDLES.g16.
                    add(Path.Combine(root, flatPkgUpper + ext));
                    add(Path.Combine(root, flatPkg + ext));
                    add(Path.Combine(root, barePkgUpper + ext));
                    add(Path.Combine(root, barePkg + ext));
                    add(Path.Combine(root, "Cash", flatPkgUpper + ext));
                    add(Path.Combine(root, "Cash", flatPkg + ext));
                    add(Path.Combine(root, "UnitsG17", barePkg + ext));
                    add(Path.Combine(root, "UnitsG17", barePkgUpper + ext));
                    add(Path.Combine(root, "Data", "UnitsG17", barePkg + ext));
                    add(Path.Combine(root, "Data", "UnitsG17", barePkgUpper + ext));
                    add(Path.Combine(root, "Data", "Cash", flatPkgUpper + ext));
                    add(Path.Combine(root, "Data1", "Cash", flatPkgUpper + ext));

                    add(Path.Combine(root, pkgNoExt + ext));
                    add(Path.Combine(root, pkg + ext));
                    add(Path.Combine(root, "Units", pkgNoExt + ext));
                    add(Path.Combine(root, "UnitsMD", pkgNoExt + ext));
                    add(Path.Combine(root, "UnitsMD", "Units", pkgNoExt + ext));
                    add(Path.Combine(root, "Sprites", pkgNoExt + ext));
                    add(Path.Combine(root, "G16", pkgNoExt + ext));
                    add(Path.Combine(root, "G2D", pkgNoExt + ext));
                    add(Path.Combine(root, "Cash", pkgNoExt + ext));
                    add(Path.Combine(root, "Data", pkgNoExt + ext));
                    add(Path.Combine(root, "Data", "Cash", pkgNoExt + ext));
                    add(Path.Combine(root, "Data1", pkgNoExt + ext));
                    add(Path.Combine(root, "Data1", "Cash", pkgNoExt + ext));
                    add(Path.Combine(root, "Resources", pkgNoExt + ext));
                    add(Path.Combine(root, "Resources", "Cash", pkgNoExt + ext));
                }
            }
            return res;
        }


        private static Dictionary<string, string> s_C2Settlement3InuMdV2VisualIndex;

        private static void C2Settlement3InuMdV2AddIndexedVisualCandidatesLikeOriginal(List<string> files, string package, string[] exts)
        {
            if (files == null) return;
            string pkg = C2Settlement3InuMdV2CleanPackageNameLikeOriginal(package);
            string pkgNoExt = Path.ChangeExtension(pkg, null) ?? "";
            string flat = pkgNoExt.Replace('\\', '_').Replace('/', '_');
            string bare = Path.GetFileName(pkgNoExt);
            var keys = new List<string>();
            Action<string> addKey = k =>
            {
                if (!string.IsNullOrEmpty(k))
                {
                    string kk = k.ToLowerInvariant();
                    if (!keys.Contains(kk)) keys.Add(kk);
                }
            };
            for (int i = 0; i < exts.Length; i++)
            {
                addKey(flat + exts[i]);
                addKey(flat.ToUpperInvariant() + exts[i]);
                addKey(bare + exts[i]);
                addKey(bare.ToUpperInvariant() + exts[i]);
            }

            var index = C2Settlement3InuMdV2VisualIndexLikeOriginal();
            for (int i = 0; i < keys.Count; i++)
            {
                string found;
                if (index.TryGetValue(keys[i], out found) && !string.IsNullOrEmpty(found) && !files.Exists(x => string.Equals(x, found, StringComparison.OrdinalIgnoreCase)))
                    files.Insert(0, found);
            }
        }

        private static Dictionary<string, string> C2Settlement3InuMdV2VisualIndexLikeOriginal()
        {
            if (s_C2Settlement3InuMdV2VisualIndex != null) return s_C2Settlement3InuMdV2VisualIndex;
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var roots = C2Settlement3InuMdV2DataRootsLikeOriginal();
            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    string[] files = Directory.GetFiles(root, "*.*", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string ext = Path.GetExtension(files[i]);
                        if (!string.Equals(ext, ".g16", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(ext, ".g2d", StringComparison.OrdinalIgnoreCase)) continue;
                        string key = Path.GetFileName(files[i]).ToLowerInvariant();
                        if (!d.ContainsKey(key)) d[key] = files[i];
                    }
                }
                catch { }
            }
            s_C2Settlement3InuMdV2VisualIndex = d;
            return d;
        }

        private static List<string> C2Settlement3InuMdV2DataRootsLikeOriginal()
        {
            var roots = new List<string>();
            Action<string> add = p => { if (!string.IsNullOrEmpty(p) && !roots.Exists(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) roots.Add(p); };
            // V21: user-confirmed original cache roots first.
            add(@"C:\GSC Game World\Cossacks II\Data\Cash");
            add(@"C:\GSC Game World\Cossacks II\Data\UnitsG17");
            add(@"C:\GSC Game World\Cossacks II\Data\UnitsMD");
            add(@"C:\GSC Game World\Cossacks II\Data");
            add(Application.streamingAssetsPath);
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "Cash"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data1"));
            add(Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data1", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources"));
            add(Path.Combine(Application.dataPath, "Resources", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources", "Data"));
            add(Path.Combine(Application.dataPath, "Resources", "Data", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources", "Data1"));
            add(Path.Combine(Application.dataPath, "Resources", "Data1", "Cash"));
            add(Path.Combine(Application.dataPath, "Resources", "UnitsMD"));
            add(Path.Combine(Application.dataPath, "Resources", "UnitsMD", "Units"));
            add(Path.Combine(Application.dataPath, "Resources", "Units"));
            add(Path.Combine(Application.dataPath, "Resources", "UnitsG17"));
            add(Path.Combine(Application.dataPath, "Resources", "G2D"));
            add(Path.Combine(Application.dataPath, "Resources", "Data", "UnitsG17"));
            add(Path.Combine(Application.dataPath, "Resources", "Data1", "UnitsG17"));
            add(Path.Combine(Application.dataPath, "Resources", "Models"));
            add(Path.Combine(Application.dataPath, "..", "Data"));
            add(Path.Combine(Application.dataPath, "..", "Cossacks2", "Data"));
            add(@"C:\GSC Game World\Cossacks II\Data");
            add(@"C:\GSC Game World\Cossacks II\Data\Cash");
            add(@"C:\GSC Game World\Cossacks II\Data\UnitsG17");
            add(@"C:\GSC Game World\Cossacks II\Data1");
            add(@"C:\GSC Game World\Cossacks II\Data1\Cash");
            add(@"C:\Program Files (x86)\GSC Game World\Cossacks II\Data");
            add(@"C:\Games\Cossacks II\Data");
            return roots;
        }

        private void C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal(Transform root, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Kind kind, List<C2Settlement3InuMdV2LoadedFrame> loadedFrames, string audit)
        {
            if (loadedFrames == null || loadedFrames.Count == 0) return;

            Vector3 basePos = C2Settlement3InuMdV2WorldLikeOriginal(r);
            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;

            bool hasLineSortV32 = false;
            for (int i = 0; i < loadedFrames.Count; i++)
            {
                if (loadedFrames[i] != null && loadedFrames[i].HasLineSort)
                {
                    hasLineSortV32 = true;
                    break;
                }
            }
            bool flipVForG16Building = !C2Settlement3InuMdV2NeedsNoVerticalFlipLikeOriginal(md, r);

            if (Settlement3InuMdV2UseVisibleBottomLiftHack)
            {
                float visibleBottom = float.PositiveInfinity;
                for (int i = 0; i < loadedFrames.Count; i++)
                {
                    if (loadedFrames[i] == null || loadedFrames[i].Texture == null) continue;
                    float plx, prx, pby, pty;
                    C2Settlement3InuMdV2FrameRectLikeOriginal(md, loadedFrames[i].Texture, loadedFrames[i].Frame, s, out plx, out prx, out pby, out pty);
                    float vb = C2Settlement3InuMdV2VisibleBottomLocalYLikeOriginal(loadedFrames[i].Texture, pby, pty, flipVForG16Building);
                    if (vb < visibleBottom) visibleBottom = vb;
                }
                if (float.IsInfinity(visibleBottom)) visibleBottom = 0f;
                basePos.y -= visibleBottom;
            }

            var parent = new GameObject("C2_3INU_MD_COMPOSITE_" + kind + "_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(r.MonsterId) + "_" + r.Index.ToString(CultureInfo.InvariantCulture));
            parent.transform.SetParent(root, true);
            parent.transform.position = basePos;

            var selectable = parent.AddComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            float selectionHalfX;
            float selectionHalfY;
            C2Settlement3InuMdV2SelectionHalfPixelsLikeOriginal(kind, out selectionHalfX, out selectionHalfY);
            selectable.Configure(
                this,
                r.Index,
                r.MonsterId,
                kind.ToString(),
                r.RealX,
                r.RealY,
                r.RealDir,
                md != null && md.NotSelectable,
                WallOriginalXYUnitToWorldScaleV8LikeOriginal(),
                selectionHalfX,
                selectionHalfY);

            if (Settlement3InuMdV2VerboseAuditV54 && hasLineSortV32)
            {
                Debug.Log("[C2:SETTLEMENT 3INU V54 LINESORT] obj=" + r.Index.ToString(CultureInfo.InvariantCulture) +
                          " name='" + (r.MonsterId ?? "") + "' parts=" + loadedFrames.Count.ToString(CultureInfo.InvariantCulture) +
                          " lineSort=" + (md != null && md.StandLoLineSort != null ? md.StandLoLineSort.Count : 0).ToString(CultureInfo.InvariantCulture) +
                          " transform=sort_only_no_geometry_deform" +
                          " modes=" + C2Settlement3InuMdV2LineSortAuditLikeOriginal(md));
            }

            int compositeMaxSortingOrderV53 = int.MinValue;

            for (int i = 0; i < loadedFrames.Count; i++)
            {
                C2Settlement3InuMdV2LoadedFrame loaded = loadedFrames[i];
                if (loaded == null) continue;
                Texture2D tex = loaded.Texture;
                if (tex == null) continue;

                var go = new GameObject("part_" + i.ToString(CultureInfo.InvariantCulture) + "_" + tex.name);
                go.transform.SetParent(parent.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, -0.0005f * i);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                bool isShadowPart = false;
                C2Settlement3InuMdV2PreparePartTextureLikeOriginal(tex, isShadowPart);
                mr.sharedMaterial = C2Settlement3InuMdV2GetMaterialLikeOriginal(tex, isShadowPart);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mr.sortingOrder = C2Settlement3InuMdV2SortOrderLikeOriginal(r, loaded, i, tex);
                if (mr.sortingOrder > compositeMaxSortingOrderV53) compositeMaxSortingOrderV53 = mr.sortingOrder;
                if (Settlement3InuMdV2VerboseAuditV54 && C2Settlement3InuMdV2LooksLikeMillOrMineLikeOriginal(md, r))
                {
                    Debug.Log("[C2:SETTLEMENT 3INU V54 PART SORT] obj=" + r.Index.ToString(CultureInfo.InvariantCulture) +
                              " name='" + (r.MonsterId ?? "") + "'" +
                              " md=" + (md != null ? (md.MdName ?? "") : "") +
                              " part=" + i.ToString(CultureInfo.InvariantCulture) +
                              " order=" + mr.sortingOrder.ToString(CultureInfo.InvariantCulture) +
                              " mapY=" + ((r.RealY >> 4).ToString(CultureInfo.InvariantCulture)) +
                              " localY=" + C2Settlement3InuMdV2LineSortLocalYV52(loaded, tex, i).ToString(CultureInfo.InvariantCulture) +
                              " lineSort=" + C2Settlement3InuMdV2LineSortOneAuditV52(loaded));
                }

                float lx, rx, by, ty;
                C2Settlement3InuMdV2FrameRectLikeOriginal(md, tex, loaded.Frame, s, out lx, out rx, out by, out ty);

                // V32: keep LINESORT parsed and post-processed exactly, but do not reapply the failed V30
                // deformation until the full DrawSpriteBuilding matrix path is ported.
                Vector3[] vertices = new[]
                {
                    new Vector3(lx, by, 0f),
                    new Vector3(rx, by, 0f),
                    new Vector3(rx, ty, 0f),
                    new Vector3(lx, ty, 0f)
                };

                var mesh = new Mesh();
                mesh.name = go.name + "_Mesh";
                mesh.vertices = vertices;
                mesh.uv = flipVForG16Building
                    ? new[]
                    {
                        new Vector2(0f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 0f),
                        new Vector2(0f, 0f)
                    }
                    : new[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f)
                    };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.RecalculateBounds();
                mf.sharedMesh = mesh;
            }

            C2Settlement3InuMdV2CreateWorkAnimationOverlayLikeOriginal(parent.transform, r, md, kind, loadedFrames.Count, s, flipVForG16Building, compositeMaxSortingOrderV53);
            selectable.SortKey = compositeMaxSortingOrderV53 != int.MinValue
                ? compositeMaxSortingOrderV53
                : C2Settlement3InuMdV2SortOrderLikeOriginal(r, loadedFrames[0], 0, loadedFrames[0].Texture);
        }

        private static void C2Settlement3InuMdV2SelectionHalfPixelsLikeOriginal(C2Settlement3InuMdV2Kind kind, out float halfX, out float halfY)
        {
            if (kind == C2Settlement3InuMdV2Kind.SettlementBuilding)
            {
                halfX = 36.0f;
                halfY = 28.0f;
                return;
            }

            if (kind == C2Settlement3InuMdV2Kind.ResourceBuilding)
            {
                halfX = 64.0f;
                halfY = 52.0f;
                return;
            }

            halfX = 52.0f;
            halfY = 40.0f;
        }

        private void C2Settlement3InuMdV2CreateWorkAnimationOverlayLikeOriginal(Transform parent, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Kind kind, int basePartCount, float s, bool flipVForG16Building, int baseMaxSortingOrderV53)
        {
            if (!C2Settlement3InuMdV2ShouldDrawWorkAnimationLikeOriginal(md, r, kind)) return;

            var textures = new List<Texture2D>();
            var vertices = new List<Vector3[]>();
            int totalWorkFrames = md.WorkFrames.Count;
            int frameCount = totalWorkFrames; // V46: no artificial 24-frame cap. MD frame list is the only limit.

            for (int i = 0; i < frameCount; i++)
            {
                C2Settlement3InuMdV2AnimFrame frameRef = md.WorkFrames[i];
                Texture2D tex;
                string workAudit;
                if (!C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frameRef, kind, out tex, out workAudit) || tex == null) continue;

                C2Settlement3InuMdV2PreparePartTextureLikeOriginal(tex, false);
                float lx, rx, by, ty;
                C2Settlement3InuMdV2FrameRectLikeOriginal(md, tex, frameRef, s, out lx, out rx, out by, out ty);
                textures.Add(tex);
                vertices.Add(new[]
                {
                    new Vector3(lx, by, 0f),
                    new Vector3(rx, by, 0f),
                    new Vector3(rx, ty, 0f),
                    new Vector3(lx, ty, 0f)
                });
            }

            if (textures.Count == 0) return;

            var go = new GameObject("work_anim_#WORK_" + textures.Count.ToString(CultureInfo.InvariantCulture));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.0005f * (basePartCount + 1));

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = C2Settlement3InuMdV2GetMaterialLikeOriginal(textures[0], false);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            int workFallbackOrderV53 = C2Settlement3InuMdV2SortOrderLikeOriginal(r, null, basePartCount + 1, textures.Count > 0 ? textures[0] : null);
            bool windmillWorkFrontV53 = C2Settlement3InuMdV2IsFrnMelLikeOriginal(md, r) && baseMaxSortingOrderV53 > int.MinValue / 2;
            mr.sortingOrder = windmillWorkFrontV53 ? Mathf.Clamp(baseMaxSortingOrderV53 + 8, -30000, 30000) : workFallbackOrderV53;
            if (Settlement3InuMdV2VerboseAuditV54 && windmillWorkFrontV53)
            {
                Debug.Log("[C2:SETTLEMENT 3INU V54 WINDMILL WORK SORT] obj=" + r.Index.ToString(CultureInfo.InvariantCulture) +
                          " name='" + (r.MonsterId ?? "") + "'" +
                          " md=" + (md != null ? (md.MdName ?? "") : "") +
                          " workOrder=" + mr.sortingOrder.ToString(CultureInfo.InvariantCulture) +
                          " baseMaxOrder=" + baseMaxSortingOrderV53.ToString(CultureInfo.InvariantCulture) +
                          " fallbackOrder=" + workFallbackOrderV53.ToString(CultureInfo.InvariantCulture) +
                          " rule=work_animation_front_of_mill_body");
            }

            var mesh = new Mesh();
            mesh.name = go.name + "_Mesh";
            mesh.vertices = vertices[0];
            mesh.uv = flipVForG16Building
                ? new[]
                {
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 0f)
                }
                : new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            var animator = go.AddComponent<C2Settlement3InuMdV2FrameAnimator>();
            animator.Textures = textures.ToArray();
            animator.Vertices = vertices.ToArray();
            animator.Mesh = mesh;
            animator.Renderer = mr;
            animator.FrameRate = Settlement3InuMdV2WorkAnimationFps;
        }

        private static bool C2Settlement3InuMdV2ShouldDrawWorkAnimationLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind)
        {
            if (md == null || md.WorkFrames == null || md.WorkFrames.Count == 0) return false;
            return C2Settlement3InuMdV2LooksLikeMillOrMineLikeOriginal(md, r);
        }

        private static bool C2Settlement3InuMdV2LooksLikeMillOrMineLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r)
        {
            string s =
                ((r.MonsterId ?? string.Empty) + " " +
                 (md != null ? (md.MdName ?? string.Empty) : string.Empty) + " " +
                 (md != null ? (md.Usage ?? string.Empty) : string.Empty) + " " +
                 (md != null ? (md.MdPath ?? string.Empty) : string.Empty)).ToUpperInvariant();

            return s.IndexOf("MEL", StringComparison.Ordinal) >= 0
                || s.IndexOf("MELN", StringComparison.Ordinal) >= 0
                || s.IndexOf("MILL", StringComparison.Ordinal) >= 0
                || s.IndexOf("RUD", StringComparison.Ordinal) >= 0
                || s.IndexOf("MINE", StringComparison.Ordinal) >= 0;
        }

        private static void C2Settlement3InuMdV2FrameRectLikeOriginal(C2Settlement3InuMdV2Info md, Texture2D tex, C2Settlement3InuMdV2AnimFrame frame, float s, out float lx, out float rx, out float by, out float ty)
        {
            int w = tex != null ? tex.width : 64;
            int h = tex != null ? tex.height : 64;
            int dx;
            int dy;
            C2Settlement3InuMdV2FramePivotLikeOriginal(md, frame, out dx, out dy);

            lx = dx * s;
            rx = (dx + w) * s;
            ty = -dy * s;
            by = -(dy + h) * s;
        }

        private static void C2Settlement3InuMdV2FramePivotLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2AnimFrame frame, out int dx, out int dy)
        {
            dx = md != null ? md.Dx : 0;
            dy = md != null ? md.Dy : 0;
            if (md != null)
            {
                if (md.Building && (md.PicDx != 0 || md.PicDy != 0 || md.PicLx > 0 || md.PicLy > 0))
                {
                    dx = md.PicDx;
                    dy = md.PicDy;
                    return;
                }

                int v;
                if (md.RlcDx != null && md.RlcDx.TryGetValue(frame.FileRef, out v)) dx = v;
                if (md.RlcDy != null && md.RlcDy.TryGetValue(frame.FileRef, out v)) dy = v;
            }
        }

        private static string C2Settlement3InuMdV2LineSortAuditLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            if (md == null || md.StandLoLineSort == null || md.StandLoLineSort.Count == 0) return "none";
            var sb = new StringBuilder();
            int n = Math.Min(md.StandLoLineSort.Count, 12);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(',');
                C2Settlement3InuMdV2LineSortInfo li = md.StandLoLineSort[i];
                if (li.IsGround) sb.Append("GROUND");
                else if (li.IsTop) sb.Append("TOP");
                else sb.Append("LINE(").Append(li.X1).Append(',').Append(li.Y1).Append("->").Append(li.X2).Append(',').Append(li.Y2).Append(')');
            }
            if (md.StandLoLineSort.Count > n) sb.Append("...");
            return sb.ToString();
        }

        private static Vector3[] C2Settlement3InuMdV2BuildLineSortVerticesLikeOriginal(Texture2D tex, int dx, int dy, C2Settlement3InuMdV2LineSortInfo li, float s, float fallbackLx, float fallbackRx, float fallbackBy, float fallbackTy)
        {
            int w = tex != null ? tex.width : 64;
            int h = tex != null ? tex.height : 64;

            if (li.IsGround)
            {
                return new[]
                {
                    C2Settlement3InuMdV2GroundPointLikeOriginal(0f, h, dx, dy, s),
                    C2Settlement3InuMdV2GroundPointLikeOriginal(w, h, dx, dy, s),
                    C2Settlement3InuMdV2GroundPointLikeOriginal(w, 0f, dx, dy, s),
                    C2Settlement3InuMdV2GroundPointLikeOriginal(0f, 0f, dx, dy, s)
                };
            }

            if (li.IsTop)
            {
                return new[]
                {
                    new Vector3(fallbackLx, fallbackBy, 0f),
                    new Vector3(fallbackRx, fallbackBy, 0f),
                    new Vector3(fallbackRx, fallbackTy, 0f),
                    new Vector3(fallbackLx, fallbackTy, 0f)
                };
            }

            return new[]
            {
                C2Settlement3InuMdV2LinePointLikeOriginal(0f, h, dx, dy, li, s),
                C2Settlement3InuMdV2LinePointLikeOriginal(w, h, dx, dy, li, s),
                C2Settlement3InuMdV2LinePointLikeOriginal(w, 0f, dx, dy, li, s),
                C2Settlement3InuMdV2LinePointLikeOriginal(0f, 0f, dx, dy, li, s)
            };
        }

        private static Vector3 C2Settlement3InuMdV2GroundPointLikeOriginal(float x, float y, int dx, int dy, float s)
        {
            // Original GetAlignGroundTransform doubles sprite Y into the map plane.
            return new Vector3((x - dx) * s, 0f, -(y - dy) * 2.0f * s);
        }

        private static Vector3 C2Settlement3InuMdV2LinePointLikeOriginal(float x, float y, int dx, int dy, C2Settlement3InuMdV2LineSortInfo li, float s)
        {
            // V30: closer GetAlignLineTransform approximation.
            // The MD line is the exact ground contact segment. In V29 the projection used dir*(pixelDistance*s),
            // so the second endpoint did not land on g2 when the ground projection had the original 2Y skew.
            // Here the projected point is interpolated between the two real ground endpoints; endpoints are exact.
            float vx = li.X2 - li.X1;
            float vy = li.Y2 - li.Y1;
            float lenSq = vx * vx + vy * vy;
            if (lenSq < 0.000001f)
            {
                return C2Settlement3InuMdV2GroundPointLikeOriginal(x, y, dx, dy, s);
            }

            float px = x - li.X1;
            float py = y - li.Y1;

            float t01 = (px * vx + py * vy) / lenSq;
            float len = Mathf.Sqrt(lenSq);
            float cross = vx * py - vy * px;
            float height = -cross / len;

            Vector3 g1 = C2Settlement3InuMdV2GroundPointLikeOriginal(li.X1, li.Y1, dx, dy, s);
            Vector3 g2 = C2Settlement3InuMdV2GroundPointLikeOriginal(li.X2, li.Y2, dx, dy, s);
            Vector3 onGround = g1 + (g2 - g1) * t01;

            return onGround + Vector3.up * (height * s);
        }

        private void C2Settlement3InuMdV2CreateSpriteObjectLikeOriginal(Transform root, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Kind kind, Texture2D tex, string audit)
        {
            Vector3 basePos = C2Settlement3InuMdV2WorldLikeOriginal(r);
            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;

            C2Settlement3InuMdV2AnimFrame frameRef = md != null && md.StandLoFrames != null && md.StandLoFrames.Count > 0
                ? md.StandLoFrames[0]
                : new C2Settlement3InuMdV2AnimFrame(0, md != null ? C2Settlement3InuMdV2SpriteFrameLikeOriginal(md, r, kind) : 0);
            float lx, rx, by, ty;
            C2Settlement3InuMdV2FrameRectLikeOriginal(md, tex, frameRef, s, out lx, out rx, out by, out ty);

            // G16 RGBA returned by Melinoja is top-left ordered for these GP frames.
            // Unity quad bottom must sample V=1 and top must sample V=0, otherwise buildings appear upside down.
            const bool flipVForG16Building = true;
            if (Settlement3InuMdV2UseVisibleBottomLiftHack)
            {
                float visibleBottom = C2Settlement3InuMdV2VisibleBottomLocalYLikeOriginal(tex, by, ty, flipVForG16Building);
                basePos.y -= visibleBottom;
            }

            var go = new GameObject("C2_3INU_MD_" + kind + "_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(r.MonsterId) + "_" + r.Index.ToString(CultureInfo.InvariantCulture));
            go.transform.SetParent(root, true);
            go.transform.position = basePos;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            bool isShadowPart = false;
                C2Settlement3InuMdV2PreparePartTextureLikeOriginal(tex, isShadowPart);
                mr.sharedMaterial = C2Settlement3InuMdV2GetMaterialLikeOriginal(tex, isShadowPart);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.sortingOrder = C2Settlement3InuMdV2SortOrderLikeOriginal(r, 0);

            var mesh = new Mesh();
            mesh.name = go.name + "_Mesh";
            mesh.vertices = new[]
            {
                new Vector3(lx, by, 0f),
                new Vector3(rx, by, 0f),
                new Vector3(rx, ty, 0f),
                new Vector3(lx, ty, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            if (Settlement3InuMdV2DrawLabels)
            {
                var label = new GameObject("label");
                label.transform.SetParent(go.transform, false);
                label.transform.localPosition = new Vector3(0f, ty + 0.5f, 0f);
                var tm = label.AddComponent<TextMesh>();
                tm.text = r.MonsterId + "\n" + Path.GetFileName(md.MdPath);
                tm.characterSize = 0.35f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.yellow;
            }
        }

        private static bool C2Settlement3InuMdV2IsFrnMelLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r)
        {
            string a = r.MonsterId ?? string.Empty;
            string b = md != null ? (md.MdName ?? string.Empty) : string.Empty;
            string c = md != null ? (md.Package ?? string.Empty) : string.Empty;
            string d = md != null ? (md.MdPath ?? string.Empty) : string.Empty;
            return a.IndexOf("BldMel", StringComparison.OrdinalIgnoreCase) >= 0
                || b.IndexOf("FrnMel", StringComparison.OrdinalIgnoreCase) >= 0
                || c.IndexOf("FrnMel", StringComparison.OrdinalIgnoreCase) >= 0
                || d.IndexOf("FrnMel", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool C2Settlement3InuMdV2NeedsNoVerticalFlipLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r)
        {
            // UNITSG17_FRNMEL.g16 comes through the Melinoja GP alias path with the opposite row convention
            // compared with the ordinary settlement-house frames. Keeping the generic G16 v-flip makes the
            // mill body/cap appear upside down. Limit the exception to the French mill only.
            return C2Settlement3InuMdV2IsFrnMelLikeOriginal(md, r);
        }

        private static float C2Settlement3InuMdV2VisibleBottomLocalYLikeOriginal(Texture2D tex, float bottom, float top, bool bottomSamplesV1)
        {
            if (tex == null) return bottom;
            int w = Mathf.Max(1, tex.width);
            int h = Mathf.Max(1, tex.height);
            try
            {
                Color32[] px = tex.GetPixels32();
                int minY = h - 1;
                int maxY = 0;
                bool any = false;
                int minPixelsInRow = Mathf.Max(2, w / 160);
                for (int y = 0; y < h; y++)
                {
                    int count = 0;
                    int ofs = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        if (px[ofs + x].a > 24)
                        {
                            count++;
                            if (count >= minPixelsInRow) break;
                        }
                    }
                    if (count >= minPixelsInRow)
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        any = true;
                    }
                }
                if (!any) return bottom;
                float row = bottomSamplesV1 ? maxY : minY;
                float t = 1.0f - Mathf.Clamp01(row / Mathf.Max(1.0f, h - 1.0f));
                if (!bottomSamplesV1) t = Mathf.Clamp01(row / Mathf.Max(1.0f, h - 1.0f));
                return Mathf.Lerp(bottom, top, t);
            }
            catch { return bottom; }
        }

        private void C2Settlement3InuMdV2CreateMdBoundsFallbackLikeOriginal(Transform root, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Kind kind, string reason)
        {
            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            float w = 96.0f;
            float h = 96.0f;
            float offX = -w * 0.5f;
            float offY = -h;
            if (md != null)
            {
                if (md.PicLx > 0) w = md.PicLx;
                if (md.PicLy > 0) h = md.PicLy;
                if (md.PicDx != 0) offX = md.PicDx;
                if (md.PicDy != 0) offY = md.PicDy;
            }

            var go = new GameObject("C2_3INU_MD_BOUNDS_" + reason + "_" + kind + "_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(r.MonsterId) + "_" + r.Index.ToString(CultureInfo.InvariantCulture));
            go.transform.SetParent(root, true);
            go.transform.position = C2Settlement3InuMdV2WorldLikeOriginal(r);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            Shader sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            mat.name = "C2_SettlementBuildings_3INU_MD_V22_BoundsFallback";
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1.0f, 0.75f, 0.10f, 0.45f));
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 1900 + r.Index;

            float left = offX * s;
            float right = (offX + w) * s;
            float bottom = 0.0f;
            float top = Mathf.Max(8.0f, h) * s;

            var mesh = new Mesh();
            mesh.name = go.name + "_Mesh";
            mesh.vertices = new[]
            {
                new Vector3(left, bottom, 0f),
                new Vector3(right, bottom, 0f),
                new Vector3(right, top, 0f),
                new Vector3(left, top, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            if (Settlement3InuMdV2DrawLabels)
            {
                var label = new GameObject("label");
                label.transform.SetParent(go.transform, false);
                label.transform.localPosition = new Vector3((left + right) * 0.5f, top + 0.35f, 0f);
                var tm = label.AddComponent<TextMesh>();
                tm.text = r.MonsterId + "\n" + reason + "\nneed G16";
                tm.characterSize = 0.28f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.yellow;
            }
        }

        private void C2Settlement3InuMdV2CreateMarkerLikeOriginal(Transform root, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind, string reason)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "C2_3INU_MD_MARKER_" + reason + "_" + kind + "_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(r.MonsterId) + "_" + r.Index.ToString(CultureInfo.InvariantCulture);
            go.transform.SetParent(root, true);
            go.transform.position = C2Settlement3InuMdV2WorldLikeOriginal(r) + Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(1.5f, 1.0f, 1.5f);
            var col = go.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);
        }

        private static void C2Settlement3InuMdV2BuildSortRanksV51LikeOriginal(List<C2Settlement3InuMdV2Record> records)
        {
            s_C2Settlement3InuMdV2SortRankV51 = new Dictionary<int, int>();
            var entries = new List<C2Settlement3InuMdV2SortEntryV51>();
            var auditMines = new List<string>();
            var auditFirst = new List<string>();

            if (records == null)
            {
                s_C2Settlement3InuMdV2SortRankAuditV51 = "records=null";
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                C2Settlement3InuMdV2Record r = records[i];
                C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(r.MonsterId);
                C2Settlement3InuMdV2Kind kind = md != null && md.Found ? md.Kind : C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(r.MonsterId);

                bool shouldDraw =
                    Settlement3InuMdV2DrawBuildings &&
                    (kind == C2Settlement3InuMdV2Kind.SettlementBuilding ||
                     kind == C2Settlement3InuMdV2Kind.Building ||
                     kind == C2Settlement3InuMdV2Kind.ResourceBuilding ||
                     kind == C2Settlement3InuMdV2Kind.SpriteObject);

                if (!shouldDraw) continue;

                int mapX = r.RealX >> 4;
                int mapY = r.RealY >> 4;
                int yLine = mapY >> 1; // original relative YL without camera constant: (y>>(1+zoomsh)), zoomsh=0.
                entries.Add(new C2Settlement3InuMdV2SortEntryV51(r.Index, yLine, mapX, mapY, r.MonsterId ?? "", md != null && md.Found ? Path.GetFileName(md.MdPath) : "<missing>"));
            }

            entries.Sort(delegate (C2Settlement3InuMdV2SortEntryV51 a, C2Settlement3InuMdV2SortEntryV51 b)
            {
                int c = a.YLine.CompareTo(b.YLine);
                if (c != 0) return c;
                // Original ShowZBuffer does not call SortZBuffer in the active path.
                // Same YL therefore keeps AddAnimation/UNI3 insertion order, not XL order.
                return a.RecordIndex.CompareTo(b.RecordIndex);
            });

            for (int rank = 0; rank < entries.Count; rank++)
            {
                s_C2Settlement3InuMdV2SortRankV51[entries[rank].RecordIndex] = rank;
                string one = "#" + entries[rank].RecordIndex.ToString(CultureInfo.InvariantCulture) +
                             " rank=" + rank.ToString(CultureInfo.InvariantCulture) +
                             " yLine=" + entries[rank].YLine.ToString(CultureInfo.InvariantCulture) +
                             " map=(" + entries[rank].MapX.ToString(CultureInfo.InvariantCulture) + "," + entries[rank].MapY.ToString(CultureInfo.InvariantCulture) + ")" +
                             " name='" + entries[rank].Name + "' md=" + entries[rank].Md;
                if (auditFirst.Count < 24) auditFirst.Add(one);
                if ((entries[rank].Name ?? "").IndexOf("BldRud", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (entries[rank].Md ?? "").IndexOf("BldRud", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (entries[rank].Name ?? "").IndexOf("BldMel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (entries[rank].Md ?? "").IndexOf("FrnMel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    auditMines.Add(one);
                }
            }

            s_C2Settlement3InuMdV2SortRankAuditV51 =
                "contract=original_AddAnimation_YL_rank_V53_part_linesort_windmill_work_front formula='partOrder=6000+mapY+(partLocalLineSortY/4)+partTie; windmillWorkOrder=max(millBodyPartOrder)+8; V51 whole-object rank kept only for audit' " +
                "drawnBuildingRanks=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                " first=" + string.Join(" | ", auditFirst.ToArray()) +
                " minesAndMills=" + string.Join(" | ", auditMines.ToArray());
        }

        private struct C2Settlement3InuMdV2SortEntryV51
        {
            public int RecordIndex;
            public int YLine;
            public int MapX;
            public int MapY;
            public string Name;
            public string Md;

            public C2Settlement3InuMdV2SortEntryV51(int recordIndex, int yLine, int mapX, int mapY, string name, string md)
            {
                RecordIndex = recordIndex;
                YLine = yLine;
                MapX = mapX;
                MapY = mapY;
                Name = name;
                Md = md;
            }
        }

        private static int C2Settlement3InuMdV2SortOrderLikeOriginal(C2Settlement3InuMdV2Record r, int partIndex)
        {
            return C2Settlement3InuMdV2SortOrderLikeOriginal(r, null, partIndex, null);
        }

        private static int C2Settlement3InuMdV2SortOrderLikeOriginal(C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2LoadedFrame loaded, int partIndex, Texture2D tex)
        {
            // V52: V51 ranked the whole building by root mapY. That is not enough for mines/houses
            // standing nearly on the same camera line: original MD uses LINESORT for separate body
            // strips. Keep order compact, but let every part carry its own effective local contact Y.
            int mapY = r.RealY >> 4;
            int localY = C2Settlement3InuMdV2LineSortLocalYV52(loaded, tex, partIndex);
            int tie = Mathf.Clamp(partIndex, 0, 3);
            int order = 6000 + mapY + (localY >> 2) + tie;
            return Mathf.Clamp(order, -30000, 30000);
        }

        private static int C2Settlement3InuMdV2LineSortLocalYV52(C2Settlement3InuMdV2LoadedFrame loaded, Texture2D tex, int partIndex)
        {
            int h = tex != null ? tex.height : 512;
            if (loaded != null && loaded.HasLineSort)
            {
                C2Settlement3InuMdV2LineSortInfo li = loaded.LineSort;
                if (li.IsGround) return 0;
                if (li.IsTop) return h + 256;
                return Mathf.Clamp((li.Y1 + li.Y2) / 2, 0, h + 256);
            }

            // Fallback for one-piece objects without explicit LINESORT: use the lower visual half,
            // not only root mapY, so tall sprites do not get buried by a slightly lower small object.
            return Mathf.Clamp((h * 3) / 4 + partIndex * 4, 0, h + 256);
        }

        private static string C2Settlement3InuMdV2LineSortOneAuditV52(C2Settlement3InuMdV2LoadedFrame loaded)
        {
            if (loaded == null || !loaded.HasLineSort) return "fallback";
            C2Settlement3InuMdV2LineSortInfo li = loaded.LineSort;
            if (li.IsGround) return "GROUND";
            if (li.IsTop) return "TOP";
            return "LINE(" + li.X1.ToString(CultureInfo.InvariantCulture) + "," + li.Y1.ToString(CultureInfo.InvariantCulture) + "->" + li.X2.ToString(CultureInfo.InvariantCulture) + "," + li.Y2.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private IEnumerator C2Settlement3InuMdV2MineNearbyAuditDelayedV58(string mapAbs, GameObject settlementRoot, C2Settlement3InuMdV2Record[] mineRecords)
        {
            int delay = Mathf.Max(0, Settlement3InuMdV2MineNearbyAuditDelayFramesV58);
            for (int frameDelay = 0; frameDelay < delay; frameDelay++) yield return null;
            if (Settlement3InuMdV2CullTerrainShadowOverlayNearMinesV62)
            {
                C2Settlement3InuMdV2CullTerrainShadowOverlayNearMinesV62(mapAbs, settlementRoot, mineRecords);
            }
            if (Settlement3InuMdV2CullNatureTreeShadowBatchNearMinesV63)
            {
                C2Settlement3InuMdV2CullNatureTreeShadowBatchesNearMinesV63(mapAbs, settlementRoot, mineRecords);
            }
            if (Settlement3InuMdV2CullNatureNearMinesV61)
            {
                C2Settlement3InuMdV2CullNatureBillboardsNearMinesV61(mapAbs, settlementRoot, mineRecords);
            }
            C2Settlement3InuMdV2RunMineNearbyAuditV58(mapAbs, settlementRoot, mineRecords);
        }


        private void C2Settlement3InuMdV2CullTerrainShadowOverlayNearMinesV62(string mapAbs, GameObject settlementRoot, C2Settlement3InuMdV2Record[] mineRecords)
        {
            try
            {
                if (mineRecords == null || mineRecords.Length == 0) return;

                Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
                StringBuilder sb = new StringBuilder(32768);
                Dictionary<string, int> materialCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> textureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> pathCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                string mapName = !string.IsNullOrEmpty(mapAbs) ? Path.GetFileNameWithoutExtension(mapAbs) : "map";

                int scanned = 0;
                int candidates = 0;
                int disabled = 0;
                float radius = Mathf.Max(1.0f, Settlement3InuMdV2MineTerrainShadowCullRadiusWorldV62);

                sb.AppendLine("# C2 V62 near-mine terrain shadow overlay cull");
                sb.AppendLine("# Reason: V61 broad Nature cull removed real forests and still left the mine shadow pile.");
                sb.AppendLine("# Scope: disables only renderers whose material looks like C2_TerrainShadowOverlay_OriginalCastOnly near parsed mines.");
                sb.AppendLine("# Does NOT disable C2_Nature tree renderers, stones, roads, water, walls, fences, buildings or terrain chunks.");
                sb.AppendLine("map=" + (mapAbs ?? "") +
                              " mines=" + mineRecords.Length.ToString(CultureInfo.InvariantCulture) +
                              " radiusWorld=" + radius.ToString(CultureInfo.InvariantCulture));

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer rr = renderers[i];
                    if (rr == null) continue;
                    GameObject go = rr.gameObject;
                    if (go == null) continue;
                    if (!rr.enabled || !go.activeInHierarchy) continue;

                    scanned++;

                    Transform tr = rr.transform;
                    if (settlementRoot != null && tr != null && tr.IsChildOf(settlementRoot.transform)) continue;

                    Bounds bounds = rr.bounds;
                    if (!C2Settlement3InuMdV2BoundsValidV58(bounds)) continue;

                    string path = C2Settlement3InuMdV2TransformPathV58(tr, 10);
                    string materialName = C2Settlement3InuMdV2RendererMaterialAuditV59(rr);
                    string shaderName = C2Settlement3InuMdV2RendererShaderAuditV58(rr);
                    string texName = C2Settlement3InuMdV2RendererTextureAuditV58(rr);

                    if (!C2Settlement3InuMdV2IsMineCullTerrainShadowOverlayV62(materialName, shaderName, texName, path)) continue;
                    candidates++;

                    int nearestMineIndex = -1;
                    float nearestDist = float.MaxValue;
                    C2Settlement3InuMdV2Record nearestMine = default(C2Settlement3InuMdV2Record);

                    for (int mineIndex = 0; mineIndex < mineRecords.Length; mineIndex++)
                    {
                        C2Settlement3InuMdV2Record mine = mineRecords[mineIndex];
                        Vector3 mineWorld = C2Settlement3InuMdV2WorldLikeOriginal(mine);
                        float dist = C2Settlement3InuMdV2DistanceXZToBoundsV58(mineWorld, bounds);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestMineIndex = mineIndex;
                            nearestMine = mine;
                        }
                    }

                    if (nearestMineIndex < 0 || nearestDist > radius) continue;

                    rr.enabled = false;
                    disabled++;

                    C2Settlement3InuMdV2Count(materialCounts, materialName);
                    C2Settlement3InuMdV2Count(textureCounts, texName);
                    C2Settlement3InuMdV2Count(pathCounts, path);

                    sb.AppendLine("DISABLED_SHADOW_OVERLAY distBounds=" + nearestDist.ToString("0.###", CultureInfo.InvariantCulture) +
                                  " mine=#" + nearestMine.Index.ToString(CultureInfo.InvariantCulture) +
                                  " mineName='" + (nearestMine.MonsterId ?? "") + "'" +
                                  " mineMap=(" + (nearestMine.RealX >> 4).ToString(CultureInfo.InvariantCulture) + "," + (nearestMine.RealY >> 4).ToString(CultureInfo.InvariantCulture) + ")" +
                                  " path='" + path + "'" +
                                  " material='" + materialName + "'" +
                                  " shader='" + shaderName + "'" +
                                  " tex='" + texName + "'" +
                                  " pos=" + C2Settlement3InuMdV2Vec3AuditV58(tr != null ? tr.position : Vector3.zero) +
                                  " boundsCenter=" + C2Settlement3InuMdV2Vec3AuditV58(bounds.center) +
                                  " boundsSize=" + C2Settlement3InuMdV2Vec3AuditV58(bounds.size));
                }

                sb.AppendLine();
                sb.AppendLine("[SUMMARY] scanned=" + scanned.ToString(CultureInfo.InvariantCulture) +
                              " candidates=" + candidates.ToString(CultureInfo.InvariantCulture) +
                              " disabled=" + disabled.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("[SUMMARY MATERIALS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 64));
                sb.AppendLine("[SUMMARY TEXTURES] " + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 64));
                sb.AppendLine("[SUMMARY PATHS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(pathCounts, 64));

                string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "C2MineNearbyAudit"));
                Directory.CreateDirectory(outDir);
                string outPath = Path.Combine(outDir, "C2MineNearbyShadowCull_V62_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(mapName) + ".txt");
                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

                Debug.Log("[C2:MINE NEARBY TERRAIN SHADOW CULL V62] mines=" + mineRecords.Length.ToString(CultureInfo.InvariantCulture) +
                          " renderers=" + renderers.Length.ToString(CultureInfo.InvariantCulture) +
                          " candidates=" + candidates.ToString(CultureInfo.InvariantCulture) +
                          " disabled=" + disabled.ToString(CultureInfo.InvariantCulture) +
                          " radiusWorld=" + radius.ToString(CultureInfo.InvariantCulture) +
                          " file='" + outPath + "'" +
                          " materials=" + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 12) +
                          " textures=" + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 12));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:MINE NEARBY TERRAIN SHADOW CULL V62] failed: " + ex.GetType().Name + ":" + ex.Message);
            }
        }

        private static bool C2Settlement3InuMdV2IsMineCullTerrainShadowOverlayV62(string materialName, string shaderName, string texName, string path)
        {
            string m = materialName ?? "";
            string s = shaderName ?? "";
            string t = texName ?? "";
            string p = path ?? "";

            bool looksTarget =
                m.IndexOf("C2_TerrainShadowOverlay_OriginalCastOnly", StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.IndexOf("TerrainShadowOverlay_OriginalCastOnly", StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.IndexOf("TerrainShadowOverlay", StringComparison.OrdinalIgnoreCase) >= 0 && s.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!looksTarget) return false;

            // Hard excludes: do not remove forests/nature billboards, stones, roads, water, wall/fence or settlement buildings.
            if (m.IndexOf("C2_Nature", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("C2_Nature", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("C2_Nature", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("STONE", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("STONE", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("Fence", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (p.IndexOf("C2_SettlementBuildings_3INU", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            return true;
        }

private void C2Settlement3InuMdV2CullNatureTreeShadowBatchesNearMinesV63(string mapAbs, GameObject settlementRoot, C2Settlement3InuMdV2Record[] mineRecords)
        {
            try
            {
                Transform[] allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                StringBuilder sb = new StringBuilder(32768);
                Dictionary<string, int> materialCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> textureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> pathCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                string mapName = !string.IsNullOrEmpty(mapAbs) ? Path.GetFileNameWithoutExtension(mapAbs) : "map";
                int transformScanned = 0;
                int batchRoots = 0;
                int rootDisabled = 0;
                int rendererDisabled = 0;
                int renderersInside = 0;

                sb.AppendLine("# C2 V64 direct C2_Nature_TS_V2_batch killer");
                sb.AppendLine("# Reason: V63 scanned Renderer.gameObject/path and got candidates=0, while the real offender is the selected hierarchy root C2_Nature_TS_V2_batch_*.");
                sb.AppendLine("# Scope: disables only GameObject roots whose name contains C2_Nature_TS_V2_batch. Does not touch C2_Nature_GA_V2_batch forests, buildings, stones, roads, walls, water or terrain chunks.");
                sb.AppendLine("# This is intentionally NOT radius-based: these TS batch roots are the unwanted garbage sprites/shadows found in Scene hierarchy.");
                sb.AppendLine("map=" + (mapAbs ?? "") + " mines=" + (mineRecords != null ? mineRecords.Length.ToString(CultureInfo.InvariantCulture) : "0"));
                sb.AppendLine();

                if (allTransforms != null)
                {
                    for (int i = 0; i < allTransforms.Length; i++)
                    {
                        Transform tr = allTransforms[i];
                        if (tr == null) continue;
                        transformScanned++;

                        GameObject go = tr.gameObject;
                        if (go == null) continue;
                        if (settlementRoot != null && tr.IsChildOf(settlementRoot.transform)) continue;

                        string objectName = go.name ?? "";
                        if (!C2Settlement3InuMdV2IsTsBatchRootNameV64(objectName)) continue;
                        if (C2Settlement3InuMdV2HasTsBatchParentV64(tr)) continue;

                        batchRoots++;

                        string rootPath = C2Settlement3InuMdV2TransformPathV58(tr, 32);
                        Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>(true);
                        int childCount = childRenderers != null ? childRenderers.Length : 0;
                        renderersInside += childCount;

                        if (childRenderers != null)
                        {
                            for (int r = 0; r < childRenderers.Length; r++)
                            {
                                Renderer rr = childRenderers[r];
                                if (rr == null) continue;

                                string materialName = C2Settlement3InuMdV2RendererMaterialAuditV59(rr);
                                string texName = C2Settlement3InuMdV2RendererTextureAuditV58(rr);
                                string childPath = C2Settlement3InuMdV2TransformPathV58(rr.transform, 32);

                                C2Settlement3InuMdV2Count(materialCounts, materialName);
                                C2Settlement3InuMdV2Count(textureCounts, texName);
                                C2Settlement3InuMdV2Count(pathCounts, childPath);

                                if (rr.enabled)
                                {
                                    rr.enabled = false;
                                    rendererDisabled++;
                                }
                            }
                        }

                        bool wasActiveSelf = go.activeSelf;
                        if (wasActiveSelf)
                        {
                            go.SetActive(false);
                            rootDisabled++;
                        }

                        sb.AppendLine("KILLED_TS_BATCH_ROOT object='" + objectName + "'" +
                                      " path='" + rootPath + "'" +
                                      " childRenderers=" + childCount.ToString(CultureInfo.InvariantCulture) +
                                      " activeSelfWas=" + (wasActiveSelf ? "true" : "false"));
                    }
                }

                sb.AppendLine();
                sb.AppendLine("[SUMMARY] transformScanned=" + transformScanned.ToString(CultureInfo.InvariantCulture) +
                              " batchRoots=" + batchRoots.ToString(CultureInfo.InvariantCulture) +
                              " rootDisabled=" + rootDisabled.ToString(CultureInfo.InvariantCulture) +
                              " renderersInside=" + renderersInside.ToString(CultureInfo.InvariantCulture) +
                              " rendererDisabled=" + rendererDisabled.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("[SUMMARY MATERIALS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 64));
                sb.AppendLine("[SUMMARY TEXTURES] " + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 64));
                sb.AppendLine("[SUMMARY PATHS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(pathCounts, 64));

                string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "C2MineNearbyAudit"));
                Directory.CreateDirectory(outDir);
                string outPath = Path.Combine(outDir, "C2MineNearbyTSBatchKill_V64_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(mapName) + ".txt");
                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

                Debug.Log("[C2:MINE NEARBY TS BATCH KILL V64] mines=" +
                          (mineRecords != null ? mineRecords.Length.ToString(CultureInfo.InvariantCulture) : "0") +
                          " transforms=" + (allTransforms != null ? allTransforms.Length.ToString(CultureInfo.InvariantCulture) : "0") +
                          " batchRoots=" + batchRoots.ToString(CultureInfo.InvariantCulture) +
                          " rootDisabled=" + rootDisabled.ToString(CultureInfo.InvariantCulture) +
                          " renderersInside=" + renderersInside.ToString(CultureInfo.InvariantCulture) +
                          " rendererDisabled=" + rendererDisabled.ToString(CultureInfo.InvariantCulture) +
                          " file='" + outPath + "'" +
                          " materials=" + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 12) +
                          " textures=" + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 12));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:MINE NEARBY TS BATCH KILL V64] failed: " + ex.GetType().Name + ":" + ex.Message);
            }
        }

        private static bool C2Settlement3InuMdV2IsTsBatchRootNameV64(string objectName)
        {
            string o = objectName ?? "";
            if (o.IndexOf("C2_Nature_TS_V2_batch", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (o.IndexOf("C2_Nature_GA_V2_batch", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        private static bool C2Settlement3InuMdV2HasTsBatchParentV64(Transform tr)
        {
            if (tr == null) return false;
            Transform p = tr.parent;
            while (p != null)
            {
                if (C2Settlement3InuMdV2IsTsBatchRootNameV64(p.name)) return true;
                p = p.parent;
            }
            return false;
        }

        private static bool C2Settlement3InuMdV2IsMineCullNatureTreeShadowBatchV63(string objectName, string materialName, string shaderName, string texName, string path)
        {
            return C2Settlement3InuMdV2IsTsBatchRootNameV64(objectName) ||
                   (!string.IsNullOrEmpty(path) && path.IndexOf("C2_Nature_TS_V2_batch", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void C2Settlement3InuMdV2CullNatureBillboardsNearMinesV61(string mapAbs, GameObject settlementRoot, C2Settlement3InuMdV2Record[] mineRecords)
        {
            try
            {
                if (mineRecords == null || mineRecords.Length == 0) return;

                Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
                if (renderers == null || renderers.Length == 0) return;

                string mapName = !string.IsNullOrEmpty(mapAbs) ? Path.GetFileNameWithoutExtension(mapAbs) : "map";
                var sb = new StringBuilder(32768);
                var materialCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var textureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var pathCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                int scanned = 0;
                int candidates = 0;
                int disabled = 0;
                float radius = Mathf.Max(1.0f, Settlement3InuMdV2MineNatureCullRadiusWorldV61);

                sb.AppendLine("# C2 V61 near-mine Nature billboard cull");
                sb.AppendLine("# Reason: V60 audit showed the black piles at mines are Nature tree/shadow billboard renderers under C2_BattleTerrainMode, not 3INU buildings.");
                sb.AppendLine("# Scope: disables only renderers matching Nature tree/shadow textures/materials inside mine radius. Does not touch buildings, stones, roads, walls, terrain chunks or water.");
                sb.AppendLine("map=" + (mapAbs ?? "") + " mines=" + mineRecords.Length.ToString(CultureInfo.InvariantCulture) + " radiusWorld=" + radius.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine();

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer rr = renderers[rendererIndex];
                    if (rr == null) continue;
                    GameObject go = rr.gameObject;
                    if (go == null) continue;
                    if (!rr.enabled || !go.activeInHierarchy) continue;

                    scanned++;

                    Transform tr = rr.transform;
                    if (settlementRoot != null && tr != null && tr.IsChildOf(settlementRoot.transform)) continue;

                    Bounds bounds = rr.bounds;
                    if (!C2Settlement3InuMdV2BoundsValidV58(bounds)) continue;

                    string path = C2Settlement3InuMdV2TransformPathV58(tr, 10);
                    string materialName = C2Settlement3InuMdV2RendererMaterialAuditV59(rr);
                    string shaderName = C2Settlement3InuMdV2RendererShaderAuditV58(rr);
                    string texName = C2Settlement3InuMdV2RendererTextureAuditV58(rr);

                    if (!C2Settlement3InuMdV2IsMineCullNatureBillboardV61(materialName, shaderName, texName, path)) continue;
                    candidates++;

                    int nearestMineIndex = -1;
                    float nearestDist = float.MaxValue;
                    C2Settlement3InuMdV2Record nearestMine = default(C2Settlement3InuMdV2Record);

                    for (int mineIndex = 0; mineIndex < mineRecords.Length; mineIndex++)
                    {
                        C2Settlement3InuMdV2Record mine = mineRecords[mineIndex];
                        Vector3 mineWorld = C2Settlement3InuMdV2WorldLikeOriginal(mine);
                        float dist = C2Settlement3InuMdV2DistanceXZToBoundsV58(mineWorld, bounds);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestMineIndex = mineIndex;
                            nearestMine = mine;
                        }
                    }

                    if (nearestMineIndex < 0 || nearestDist > radius) continue;

                    rr.enabled = false;
                    disabled++;

                    C2Settlement3InuMdV2Count(materialCounts, materialName);
                    C2Settlement3InuMdV2Count(textureCounts, texName);
                    C2Settlement3InuMdV2Count(pathCounts, path);

                    sb.AppendLine("DISABLED distBounds=" + nearestDist.ToString("0.###", CultureInfo.InvariantCulture) +
                                  " mine=#" + nearestMine.Index.ToString(CultureInfo.InvariantCulture) +
                                  " mineName='" + (nearestMine.MonsterId ?? "") + "'" +
                                  " mineMap=(" + (nearestMine.RealX >> 4).ToString(CultureInfo.InvariantCulture) + "," + (nearestMine.RealY >> 4).ToString(CultureInfo.InvariantCulture) + ")" +
                                  " path='" + path + "'" +
                                  " material='" + materialName + "'" +
                                  " shader='" + shaderName + "'" +
                                  " tex='" + texName + "'" +
                                  " pos=" + C2Settlement3InuMdV2Vec3AuditV58(tr != null ? tr.position : Vector3.zero) +
                                  " boundsCenter=" + C2Settlement3InuMdV2Vec3AuditV58(bounds.center) +
                                  " boundsSize=" + C2Settlement3InuMdV2Vec3AuditV58(bounds.size));
                }

                sb.AppendLine();
                sb.AppendLine("[SUMMARY] scanned=" + scanned.ToString(CultureInfo.InvariantCulture) +
                              " candidates=" + candidates.ToString(CultureInfo.InvariantCulture) +
                              " disabled=" + disabled.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("[SUMMARY MATERIALS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 64));
                sb.AppendLine("[SUMMARY TEXTURES] " + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 64));
                sb.AppendLine("[SUMMARY PATHS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(pathCounts, 64));

                string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "C2MineNearbyAudit"));
                Directory.CreateDirectory(outDir);
                string outPath = Path.Combine(outDir, "C2MineNearbyCull_V61_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(mapName) + ".txt");
                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

                Debug.Log("[C2:MINE NEARBY NATURE CULL V61] mines=" + mineRecords.Length.ToString(CultureInfo.InvariantCulture) +
                          " renderers=" + renderers.Length.ToString(CultureInfo.InvariantCulture) +
                          " candidates=" + candidates.ToString(CultureInfo.InvariantCulture) +
                          " disabled=" + disabled.ToString(CultureInfo.InvariantCulture) +
                          " radiusWorld=" + radius.ToString(CultureInfo.InvariantCulture) +
                          " file='" + outPath + "'" +
                          " materials=" + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 12) +
                          " textures=" + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 12));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:MINE NEARBY NATURE CULL V61] failed: " + ex.GetType().Name + ":" + ex.Message);
            }
        }

        private static bool C2Settlement3InuMdV2IsMineCullNatureBillboardV61(string materialName, string shaderName, string texName, string path)
        {
            string m = materialName ?? "";
            string s = shaderName ?? "";
            string t = texName ?? "";
            string p = path ?? "";

            bool looksNatureTree =
                m.IndexOf("C2_Nature_TREES_Tree", StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.IndexOf("C2_Nature_TreesAll_ShadowBillboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("C2_Nature_G2D_TreesAll", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("C2_Nature_G2D_TREES", StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.IndexOf("C2_Nature", StringComparison.OrdinalIgnoreCase) >= 0 && p.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0 ||
                s.IndexOf("Nature", StringComparison.OrdinalIgnoreCase) >= 0 && s.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!looksNatureTree) return false;

            // Hard excludes: V61 must not remove actual terrain, water, roads, stones, walls/fences or settlement buildings.
            if (m.IndexOf("STONE", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("STONE", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("TerrainSoftwareChunk", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (m.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("Fence", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            return true;
        }

        private sealed class C2Settlement3InuMdV2NearbyAuditEntryV58
        {
            public float DistBounds;
            public float DistCenter;
            public bool IsSettlement;
            public bool Excluded;
            public string ExcludeReason;
            public string Line;
        }

        private void C2Settlement3InuMdV2RunMineNearbyAuditV58(string mapAbs, GameObject settlementRoot, C2Settlement3InuMdV2Record[] mineRecords)
        {
            try
            {
                if (mineRecords == null || mineRecords.Length == 0) return;

                Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
                var sb = new StringBuilder(65536);
                string mapName = !string.IsNullOrEmpty(mapAbs) ? Path.GetFileNameWithoutExtension(mapAbs) : "map";
                sb.AppendLine("# C2 mine-nearby focused terrain/ground renderer audit V61");
                sb.AppendLine("# Purpose: find black/coal-like piles near mines. This logger changes no visuals.");
                sb.AppendLine("# V61 runs after Nature billboard cull and confirms what remains near mines.");
                sb.AppendLine("map=" + (mapAbs ?? "") +
                              " mines=" + mineRecords.Length.ToString(CultureInfo.InvariantCulture) +
                              " renderers=" + (renderers != null ? renderers.Length : 0).ToString(CultureInfo.InvariantCulture) +
                              " radiusWorld=" + Settlement3InuMdV2MineNearbyAuditRadiusWorldV58.ToString(CultureInfo.InvariantCulture) +
                              " maxSmallHorizontal=" + Settlement3InuMdV2MineNearbyAuditMaxSmallHorizontalV59.ToString(CultureInfo.InvariantCulture) +
                              " maxSmallArea=" + Settlement3InuMdV2MineNearbyAuditMaxSmallAreaV59.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine();

                int totalNearRaw = 0;
                int totalSettlementNear = 0;
                int totalSmallNonBuilding = 0;
                int totalExcludedNonBuilding = 0;
                var rootCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var textureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var materialCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var excludedReasonCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var terrainGroundRootCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var terrainGroundTextureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var terrainGroundMaterialCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var terrainGroundPathCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int mineIndex = 0; mineIndex < mineRecords.Length; mineIndex++)
                {
                    C2Settlement3InuMdV2Record mine = mineRecords[mineIndex];
                    Vector3 mineWorld = C2Settlement3InuMdV2WorldLikeOriginal(mine);
                    int mineMapX = mine.RealX >> 4;
                    int mineMapY = mine.RealY >> 4;
                    var small = new List<C2Settlement3InuMdV2NearbyAuditEntryV58>();
                    var excluded = new List<C2Settlement3InuMdV2NearbyAuditEntryV58>();
                    var terrainGround = new List<C2Settlement3InuMdV2NearbyAuditEntryV58>();
                    int settlementNear = 0;

                    if (renderers != null)
                    {
                        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                        {
                            Renderer rr = renderers[rendererIndex];
                            if (rr == null) continue;
                            GameObject go = rr.gameObject;
                            if (go == null) continue;
                            if (!rr.enabled || !go.activeInHierarchy) continue;

                            Bounds bounds = rr.bounds;
                            if (!C2Settlement3InuMdV2BoundsValidV58(bounds)) continue;

                            float distBounds = C2Settlement3InuMdV2DistanceXZToBoundsV58(mineWorld, bounds);
                            if (distBounds > Settlement3InuMdV2MineNearbyAuditRadiusWorldV58) continue;

                            totalNearRaw++;
                            float distCenter = C2Settlement3InuMdV2DistanceXZV58(mineWorld, bounds.center);
                            Transform tr = rr.transform;
                            bool isSettlement = settlementRoot != null && tr != null && tr.IsChildOf(settlementRoot.transform);
                            if (isSettlement)
                            {
                                settlementNear++;
                                totalSettlementNear++;
                                continue;
                            }

                            string rootName = C2Settlement3InuMdV2RootNameV58(tr);
                            string path = C2Settlement3InuMdV2TransformPathV58(tr, 10);
                            string materialName = C2Settlement3InuMdV2RendererMaterialAuditV59(rr);
                            string shaderName = C2Settlement3InuMdV2RendererShaderAuditV58(rr);
                            string texName = C2Settlement3InuMdV2RendererTextureAuditV58(rr);
                            string queue = C2Settlement3InuMdV2RendererQueueAuditV58(rr);
                            string excludeReason = C2Settlement3InuMdV2ExcludeReasonV59(rr, bounds, rootName, path, materialName, shaderName, texName);

                            var entry = new C2Settlement3InuMdV2NearbyAuditEntryV58();
                            entry.DistBounds = distBounds;
                            entry.DistCenter = distCenter;
                            entry.IsSettlement = false;
                            entry.Excluded = !string.IsNullOrEmpty(excludeReason);
                            entry.ExcludeReason = entry.Excluded ? excludeReason : "";
                            string auditLinePrefix = !entry.Excluded ? "TOP_NONBUILDING" : (string.Equals(entry.ExcludeReason, "terrain_ground", StringComparison.OrdinalIgnoreCase) ? "SUSPECT_TERRAIN_GROUND" : "EXCLUDED_NONBUILDING");
                            entry.Line =
                                auditLinePrefix +
                                " distBounds=" + distBounds.ToString("0.###", CultureInfo.InvariantCulture) +
                                " distCenter=" + distCenter.ToString("0.###", CultureInfo.InvariantCulture) +
                                (entry.Excluded ? " reason='" + entry.ExcludeReason + "'" : "") +
                                " root='" + rootName + "'" +
                                " path='" + path + "'" +
                                " renderer='" + rr.GetType().Name + "'" +
                                " sortingLayer=" + rr.sortingLayerID.ToString(CultureInfo.InvariantCulture) +
                                " sortingOrder=" + rr.sortingOrder.ToString(CultureInfo.InvariantCulture) +
                                " queue=" + queue +
                                " material='" + materialName + "'" +
                                " shader='" + shaderName + "'" +
                                " tex='" + texName + "'" +
                                " pos=" + C2Settlement3InuMdV2Vec3AuditV58(tr != null ? tr.position : Vector3.zero) +
                                " boundsCenter=" + C2Settlement3InuMdV2Vec3AuditV58(bounds.center) +
                                " boundsSize=" + C2Settlement3InuMdV2Vec3AuditV58(bounds.size);

                            if (entry.Excluded)
                            {
                                excluded.Add(entry);
                                totalExcludedNonBuilding++;
                                C2Settlement3InuMdV2Count(excludedReasonCounts, entry.ExcludeReason);
                                if (string.Equals(entry.ExcludeReason, "terrain_ground", StringComparison.OrdinalIgnoreCase))
                                {
                                    terrainGround.Add(entry);
                                    C2Settlement3InuMdV2Count(terrainGroundRootCounts, rootName);
                                    C2Settlement3InuMdV2Count(terrainGroundTextureCounts, texName);
                                    C2Settlement3InuMdV2Count(terrainGroundMaterialCounts, materialName);
                                    C2Settlement3InuMdV2Count(terrainGroundPathCounts, path);
                                }
                            }
                            else
                            {
                                small.Add(entry);
                                totalSmallNonBuilding++;
                                C2Settlement3InuMdV2Count(rootCounts, rootName);
                                C2Settlement3InuMdV2Count(textureCounts, texName);
                                C2Settlement3InuMdV2Count(materialCounts, materialName);
                            }
                        }
                    }

                    small.Sort((a, b) =>
                    {
                        int c = a.DistBounds.CompareTo(b.DistBounds);
                        if (c != 0) return c;
                        return a.DistCenter.CompareTo(b.DistCenter);
                    });
                    excluded.Sort((a, b) =>
                    {
                        int c = a.DistBounds.CompareTo(b.DistBounds);
                        if (c != 0) return c;
                        return a.DistCenter.CompareTo(b.DistCenter);
                    });
                    terrainGround.Sort((a, b) =>
                    {
                        int c = a.DistBounds.CompareTo(b.DistBounds);
                        if (c != 0) return c;
                        return a.DistCenter.CompareTo(b.DistCenter);
                    });

                    sb.AppendLine("[MINE] #" + mine.Index.ToString(CultureInfo.InvariantCulture) +
                                  " name='" + (mine.MonsterId ?? "") + "'" +
                                  " map=(" + mineMapX.ToString(CultureInfo.InvariantCulture) + "," + mineMapY.ToString(CultureInfo.InvariantCulture) + ")" +
                                  " real=(" + mine.RealX.ToString(CultureInfo.InvariantCulture) + "," + mine.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                                  " world=" + C2Settlement3InuMdV2Vec3AuditV58(mineWorld) +
                                  " topSmallNonBuilding=" + small.Count.ToString(CultureInfo.InvariantCulture) +
                                  " settlementNearby=" + settlementNear.ToString(CultureInfo.InvariantCulture) +
                                  " excludedNonBuilding=" + excluded.Count.ToString(CultureInfo.InvariantCulture) +
                                  " terrainGroundNearby=" + terrainGround.Count.ToString(CultureInfo.InvariantCulture));

                    int smallLimit = Mathf.Min(small.Count, Settlement3InuMdV2MineNearbyAuditTopSmallPerMineV59);
                    for (int i = 0; i < smallLimit; i++) sb.AppendLine("  " + small[i].Line);
                    if (small.Count > smallLimit) sb.AppendLine("  ... small truncated " + (small.Count - smallLimit).ToString(CultureInfo.InvariantCulture) + " more");

                    if (terrainGround.Count > 0)
                    {
                        sb.AppendLine("  [terrain-ground-nearest-suspects]");
                        int terrainLimit = Mathf.Min(terrainGround.Count, Settlement3InuMdV2MineNearbyAuditTopExcludedPerMineV59);
                        for (int i = 0; i < terrainLimit; i++) sb.AppendLine("  " + terrainGround[i].Line);
                        if (terrainGround.Count > terrainLimit) sb.AppendLine("  ... terrain_ground truncated " + (terrainGround.Count - terrainLimit).ToString(CultureInfo.InvariantCulture) + " more");
                    }

                    if (excluded.Count > 0)
                    {
                        sb.AppendLine("  [excluded-nearest-sample]");
                        int excludedLimit = Mathf.Min(excluded.Count, Settlement3InuMdV2MineNearbyAuditTopExcludedPerMineV59);
                        for (int i = 0; i < excludedLimit; i++) sb.AppendLine("  " + excluded[i].Line);
                        if (excluded.Count > excludedLimit) sb.AppendLine("  ... excluded truncated " + (excluded.Count - excludedLimit).ToString(CultureInfo.InvariantCulture) + " more");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("[SUMMARY] totalNearRaw=" + totalNearRaw.ToString(CultureInfo.InvariantCulture) +
                              " settlementNear=" + totalSettlementNear.ToString(CultureInfo.InvariantCulture) +
                              " smallNonBuilding=" + totalSmallNonBuilding.ToString(CultureInfo.InvariantCulture) +
                              " excludedNonBuilding=" + totalExcludedNonBuilding.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("[SUMMARY SMALL NONBUILDING ROOTS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(rootCounts, 128));
                sb.AppendLine("[SUMMARY SMALL NONBUILDING TEXTURES] " + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 128));
                sb.AppendLine("[SUMMARY SMALL NONBUILDING MATERIALS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(materialCounts, 128));
                sb.AppendLine("[SUMMARY EXCLUDED REASONS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(excludedReasonCounts, 128));
                sb.AppendLine("[SUMMARY TERRAIN_GROUND ROOTS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundRootCounts, 128));
                sb.AppendLine("[SUMMARY TERRAIN_GROUND TEXTURES] " + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundTextureCounts, 128));
                sb.AppendLine("[SUMMARY TERRAIN_GROUND MATERIALS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundMaterialCounts, 128));
                sb.AppendLine("[SUMMARY TERRAIN_GROUND PATHS] " + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundPathCounts, 128));

                string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "C2MineNearbyAudit"));
                Directory.CreateDirectory(outDir);
                string outPath = Path.Combine(outDir, "C2MineNearbyAudit_V61_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(mapName) + ".txt");
                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

                Debug.Log("[C2:MINE NEARBY TERRAIN AUDIT V61] mines=" + mineRecords.Length.ToString(CultureInfo.InvariantCulture) +
                          " renderers=" + (renderers != null ? renderers.Length : 0).ToString(CultureInfo.InvariantCulture) +
                          " totalNearRaw=" + totalNearRaw.ToString(CultureInfo.InvariantCulture) +
                          " settlementNear=" + totalSettlementNear.ToString(CultureInfo.InvariantCulture) +
                          " smallNonBuilding=" + totalSmallNonBuilding.ToString(CultureInfo.InvariantCulture) +
                          " excludedNonBuilding=" + totalExcludedNonBuilding.ToString(CultureInfo.InvariantCulture) +
                          " radiusWorld=" + Settlement3InuMdV2MineNearbyAuditRadiusWorldV58.ToString(CultureInfo.InvariantCulture) +
                          " file='" + outPath + "'" +
                          " roots=" + C2Settlement3InuMdV2TopNamesLikeOriginal(rootCounts, 24) +
                          " textures=" + C2Settlement3InuMdV2TopNamesLikeOriginal(textureCounts, 24) +
                          " terrainRoots=" + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundRootCounts, 16) +
                          " terrainTextures=" + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundTextureCounts, 16) +
                          " terrainMaterials=" + C2Settlement3InuMdV2TopNamesLikeOriginal(terrainGroundMaterialCounts, 16) +
                          " excluded=" + C2Settlement3InuMdV2TopNamesLikeOriginal(excludedReasonCounts, 12));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:MINE NEARBY TERRAIN AUDIT V61] failed: " + ex.GetType().Name + ":" + ex.Message);
            }
        }

        private static bool C2Settlement3InuMdV2BoundsValidV58(Bounds b)
        {
            Vector3 s = b.size;
            Vector3 c = b.center;
            return !(float.IsNaN(s.x) || float.IsNaN(s.y) || float.IsNaN(s.z) || float.IsNaN(c.x) || float.IsNaN(c.y) || float.IsNaN(c.z) || float.IsInfinity(s.x) || float.IsInfinity(s.y) || float.IsInfinity(s.z));
        }

        private static float C2Settlement3InuMdV2DistanceXZToBoundsV58(Vector3 p, Bounds b)
        {
            float dx = 0f;
            if (p.x < b.min.x) dx = b.min.x - p.x;
            else if (p.x > b.max.x) dx = p.x - b.max.x;
            float dz = 0f;
            if (p.z < b.min.z) dz = b.min.z - p.z;
            else if (p.z > b.max.z) dz = p.z - b.max.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float C2Settlement3InuMdV2DistanceXZV58(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string C2Settlement3InuMdV2Vec3AuditV58(Vector3 v)
        {
            return "(" + v.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + v.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + v.z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }

        private static string C2Settlement3InuMdV2RootNameV58(Transform tr)
        {
            if (tr == null) return "<null>";
            Transform root = tr;
            while (root.parent != null) root = root.parent;
            return root.name;
        }

        private static string C2Settlement3InuMdV2TransformPathV58(Transform tr, int maxDepth)
        {
            if (tr == null) return "<null>";
            var parts = new List<string>();
            Transform cur = tr;
            int depth = 0;
            while (cur != null && depth < maxDepth)
            {
                parts.Add(cur.name);
                cur = cur.parent;
                depth++;
            }
            parts.Reverse();
            string path = string.Join("/", parts.ToArray());
            if (cur != null) path = ".../" + path;
            return path;
        }

        private static string C2Settlement3InuMdV2RendererShaderAuditV58(Renderer rr)
        {
            Material mat = rr != null ? rr.sharedMaterial : null;
            Shader sh = mat != null ? mat.shader : null;
            return sh != null ? sh.name : "<none>";
        }

        private static string C2Settlement3InuMdV2RendererQueueAuditV58(Renderer rr)
        {
            Material mat = rr != null ? rr.sharedMaterial : null;
            return mat != null ? mat.renderQueue.ToString(CultureInfo.InvariantCulture) : "<none>";
        }

        private static string C2Settlement3InuMdV2RendererMaterialAuditV59(Renderer rr)
        {
            if (rr == null) return "<none>";
            Material[] mats = rr.sharedMaterials;
            if (mats == null || mats.Length == 0) return "<none>";
            var names = new List<string>();
            int limit = Mathf.Min(mats.Length, 4);
            for (int i = 0; i < limit; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;
                names.Add(mat.name);
            }
            if (mats.Length > limit) names.Add("+" + (mats.Length - limit).ToString(CultureInfo.InvariantCulture));
            return names.Count > 0 ? string.Join(",", names.ToArray()) : "<none>";
        }

        private static string C2Settlement3InuMdV2RendererTextureAuditV58(Renderer rr)
        {
            if (rr == null) return "<none>";
            Material[] mats = rr.sharedMaterials;
            if (mats == null || mats.Length == 0) return "<none>";

            string[] propertyNames = new string[]
            {
                "_MainTex", "_BaseMap", "_BaseColorMap", "_DiffuseTex", "_Albedo", "_Texture",
                "_Tex", "_MainTexA", "_MainTexB", "_MainTex1", "_MainTex2", "_MaskTex", "_DetailTex",
                "_BumpMap", "_NormalMap"
            };

            for (int matIndex = 0; matIndex < mats.Length; matIndex++)
            {
                Material mat = mats[matIndex];
                if (mat == null) continue;
                for (int propIndex = 0; propIndex < propertyNames.Length; propIndex++)
                {
                    string prop = propertyNames[propIndex];
                    if (!mat.HasProperty(prop)) continue;
                    Texture tex = null;
                    try { tex = mat.GetTexture(prop); }
                    catch { tex = null; }
                    if (tex == null) continue;
                    return prop + ":" + tex.name + "(" + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture) + ")";
                }
            }

            return "<none>";
        }

        private static string C2Settlement3InuMdV2ExcludeReasonV59(Renderer rr, Bounds b, string rootName, string path, string matName, string shaderName, string texName)
        {
            string combined = ((rootName ?? "") + " " + (path ?? "") + " " + (matName ?? "") + " " + (shaderName ?? "") + " " + (texName ?? "")).ToLowerInvariant();
            if (combined.Contains("water") || combined.Contains("sea") || combined.Contains("river") || combined.Contains("ripple")) return "water";
            if (combined.Contains("terrain") || combined.Contains("ground") || combined.Contains("facture") || combined.Contains("surface") || combined.Contains("heightmap")) return "terrain_ground";
            if (combined.Contains("road") || combined.Contains("rne2") || combined.Contains("rnm") || combined.Contains("damba")) return "road_damba";
            if (combined.Contains("wall") || combined.Contains("wals") || combined.Contains("fence")) return "wall_fence";

            Vector3 s = b.size;
            float horizontalMax = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
            float area = Mathf.Abs(s.x * s.z);
            if (horizontalMax > Settlement3InuMdV2MineNearbyAuditMaxSmallHorizontalV59) return "large_bounds";
            if (area > Settlement3InuMdV2MineNearbyAuditMaxSmallAreaV59) return "large_area";

            return "";
        }

        private Vector3 C2Settlement3InuMdV2WorldLikeOriginal(C2Settlement3InuMdV2Record r)
        {
            int mx = r.RealX >> 4;
            int my = r.RealY >> 4;

            return WallOriginalXYToWorldV1LikeOriginal(mx, my, 0.0f);
        }

        private static Material C2Settlement3InuMdV2GetMaterialLikeOriginal(Texture2D tex, bool shadowLike)
        {
            Texture2D mainTex = tex != null ? tex : Texture2D.whiteTexture;
            int key = mainTex.GetInstanceID() * 2 + (shadowLike ? 1 : 0);
            Material cached;
            if (s_C2Settlement3InuMdV2MaterialCacheV54.TryGetValue(key, out cached) && cached != null)
            {
                s_C2Settlement3InuMdV2MaterialCacheHitsV54++;
                return cached;
            }
            s_C2Settlement3InuMdV2MaterialCacheMissesV54++;

            // V23: use a dedicated shader but keep Unity-side ZTest Always until the original
            // DrawSpriteBuilding pseudo-projection/depth matrix is ported. Hardware LEqual on a flat
            // billboard cuts houses into the terrain and looks like destroyed/sunk buildings.
            Shader sh = Shader.Find("Cossacks2Bridge/SettlementBuildingSpriteV23LikeOriginal");
            if (sh == null) sh = Shader.Find("Cossacks2Bridge/WallObjectSpriteV31ExactCutout");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Transparent/Cutout/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Transparent Cutout");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");

            var mat = new Material(sh);
            mat.name = Settlement3InuMdV2MaterialName + "_UNITY_SAFE_DEPTH_" + (tex != null ? tex.name : "null");
            mat.mainTexture = mainTex;
            mat.renderQueue = Settlement3InuMdV2RenderQueueV49LikeOriginal;
            mat.SetOverrideTag("RenderType", "TransparentCutout");

            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", Settlement3InuMdV2AlphaRefV49LikeOriginal);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", Settlement3InuMdV2AlphaRefV49LikeOriginal);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1.0f);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

            mat.enableInstancing = true;
            s_C2Settlement3InuMdV2MaterialCacheV54[key] = mat;
            return mat;
        }

        private static Texture2D C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(Texture2D tex)
        {
            // V57: Paint.NET-style duplicate layer compositing.
            // Keep RGB exact and recompute alpha as if the same decoded sprite was drawn
            // over itself once before Texture2D/disk cache.
            if (tex == null) return null;
            try
            {
                byte[] raw = tex.GetRawTextureData();
                if (raw != null && raw.Length >= tex.width * tex.height * 4)
                {
                    byte[] rgba = new byte[raw.Length];
                    Buffer.BlockCopy(raw, 0, rgba, 0, raw.Length);
                    C2Settlement3InuMdV2ApplyLayerCompositeV57(rgba, tex.width, tex.height);

                    var srgb = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
                    srgb.name = tex.name + "_srgb_layerV57";
                    srgb.LoadRawTextureData(rgba);
                    srgb.Apply(false, false);
                    srgb.wrapMode = TextureWrapMode.Clamp;
                    srgb.filterMode = FilterMode.Point; // V49: D3D Mag/Min=1/1 = point sampling
                    return srgb;
                }
            }
            catch { }
            try
            {
                Color32[] px = tex.GetPixels32();
                C2Settlement3InuMdV2ApplyLayerCompositeV57(px);
                var srgb = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
                srgb.name = tex.name + "_srgb_layerV57";
                srgb.SetPixels32(px);
                srgb.Apply(false, false);
                srgb.wrapMode = TextureWrapMode.Clamp;
                srgb.filterMode = FilterMode.Point;
                return srgb;
            }
            catch
            {
                return tex;
            }
        }

        private static byte C2Settlement3InuMdV2CompositeAlphaByteV57(byte a)
        {
            if (!Settlement3InuMdV2LayerCompositeV57 || a == 0 || a == 255) return a;

            float bottomA = a / 255.0f;
            float topOpacity = Mathf.Clamp01(Settlement3InuMdV2LayerCompositeTopOpacityV57);
            float topA = bottomA * topOpacity;

            // Normal SourceOver compositing of the same sprite over itself:
            // outA = topA + bottomA * (1 - topA)
            float outA = topA + bottomA * (1.0f - topA);
            int ia = Mathf.Clamp(Mathf.RoundToInt(outA * 255.0f), 0, 255);
            return (byte)ia;
        }

        private static void C2Settlement3InuMdV2ApplyLayerCompositeV57(byte[] rgba, int width, int height)
        {
            if (!Settlement3InuMdV2LayerCompositeV57 || rgba == null || rgba.Length < 4) return;
            int pixelCount = Mathf.Min(width * height, rgba.Length / 4);
            if (pixelCount <= 0) return;

            bool changed = false;
            long changedPixels = 0;
            long opaquePixels = 0;
            for (int p = 0, i = 0; p < pixelCount; p++, i += 4)
            {
                byte oldA = rgba[i + 3];
                byte newA = C2Settlement3InuMdV2CompositeAlphaByteV57(oldA);
                if (newA != oldA)
                {
                    rgba[i + 3] = newA;
                    changed = true;
                    changedPixels++;
                    if (newA == 255) opaquePixels++;
                }
            }

            if (changed)
            {
                s_C2Settlement3InuMdV2LayerBlendFramesV57++;
                s_C2Settlement3InuMdV2LayerBlendPixelsV57 += changedPixels;
                s_C2Settlement3InuMdV2LayerBlendOpaquePixelsV57 += opaquePixels;
            }
        }

        private static void C2Settlement3InuMdV2ApplyLayerCompositeV57(Color32[] px)
        {
            if (!Settlement3InuMdV2LayerCompositeV57 || px == null || px.Length == 0) return;

            bool changed = false;
            long changedPixels = 0;
            long opaquePixels = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                byte oldA = c.a;
                byte newA = C2Settlement3InuMdV2CompositeAlphaByteV57(oldA);
                if (newA != oldA)
                {
                    c.a = newA;
                    px[i] = c;
                    changed = true;
                    changedPixels++;
                    if (newA == 255) opaquePixels++;
                }
            }

            if (changed)
            {
                s_C2Settlement3InuMdV2LayerBlendFramesV57++;
                s_C2Settlement3InuMdV2LayerBlendPixelsV57 += changedPixels;
                s_C2Settlement3InuMdV2LayerBlendOpaquePixelsV57 += opaquePixels;
            }
        }

        private static void C2Settlement3InuMdV2PreparePartTextureLikeOriginal(Texture2D tex, bool shadowLike)
        {
            // V21: explicitly no-op. Do not edit alpha, RGB, filtering, or shadow frames.
        }

        private static bool C2Settlement3InuMdV2LooksLikeShadowTextureOriginal(Texture2D tex)
        {
            if (tex == null) return false;
            try
            {
                Color32[] px = tex.GetPixels32();
                if (px == null || px.Length == 0) return false;
                long rgb = 0;
                long alpha = 0;
                int count = 0;
                int step = Mathf.Max(1, px.Length / 4096);
                for (int i = 0; i < px.Length; i += step)
                {
                    byte a = px[i].a;
                    if (a <= 8) continue;
                    rgb += px[i].r + px[i].g + px[i].b;
                    alpha += a;
                    count++;
                }
                if (count < 8) return false;
                float avgRgb = rgb / (float)(count * 3);
                float avgAlpha = alpha / (float)count;

                // Black/grey low-brightness layers are usually SHADOW/ground darkening frames.
                return avgRgb < 55.0f && avgAlpha < 210.0f;
            }
            catch { return false; }
        }

        private static C2Settlement3InuMdV2Kind C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            if (n.IndexOf("anm") == 0 || n.IndexOf("ovc") >= 0 || n.IndexOf("swi") >= 0 || n.IndexOf("kor") >= 0 || n.IndexOf("bar") >= 0) return C2Settlement3InuMdV2Kind.Animal;
            if (n.IndexOf("unit") == 0 || n.IndexOf("sold") >= 0 || n.IndexOf("kri") >= 0 || n.IndexOf("gren") >= 0 || n.IndexOf("horse") >= 0 || n.IndexOf("cannon") >= 0) return C2Settlement3InuMdV2Kind.Unit;
            if (n.IndexOf("seldom") >= 0 || (n.IndexOf("sel") == 0 && n.IndexOf("dom") >= 0)) return C2Settlement3InuMdV2Kind.SettlementBuilding;
            if (n.IndexOf("bldrud") >= 0 || n.IndexOf("bldmel") >= 0 || n.IndexOf("bldles") >= 0 || n.IndexOf("rud") >= 0 || n.IndexOf("mine") >= 0 || n.IndexOf("meln") >= 0 || n.IndexOf("sklad") >= 0 || n.IndexOf("coal") >= 0 || n.IndexOf("iron") >= 0 || n.IndexOf("gold") >= 0) return C2Settlement3InuMdV2Kind.ResourceBuilding;
            if (n.IndexOf("bld") == 0 || n.IndexOf("build") >= 0 || n.IndexOf("house") >= 0 || n.IndexOf("town") >= 0 || n.IndexOf("port") >= 0 || n.IndexOf("tower") >= 0) return C2Settlement3InuMdV2Kind.Building;
            return C2Settlement3InuMdV2Kind.Unknown;
        }

        private static string C2Settlement3InuMdV2ResolveMapPathLikeOriginal(string mapPath)
        {
            if (string.IsNullOrEmpty(mapPath)) return null;
            if (File.Exists(mapPath)) return Path.GetFullPath(mapPath);
            string p = mapPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string[] roots = {
                Application.dataPath,
                Path.Combine(Application.dataPath, "Resources"),
                Path.Combine(Application.dataPath, "Resources", "Maps"),
                Application.streamingAssetsPath,
                Path.Combine(Application.streamingAssetsPath, "Cossacks2"),
                Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"),
                Path.Combine(Application.dataPath, "..", "Data"),
                @"C:\GSC Game World\Cossacks II\Data"
            };
            for (int i = 0; i < roots.Length; i++)
            {
                string c = Path.Combine(roots[i], p);
                if (File.Exists(c)) return Path.GetFullPath(c);
            }
            return null;
        }

        private static readonly HashSet<string> C2SettlementBuildingsLoadedGpV23LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static Texture2D TryLoadBuildingGpFrameViaMelinojaV23LikeOriginal(string abs, int frameIndex, out string source, string logicalPackage = null)
        {
            source = string.Empty;

            // V46: MD sprite id is the exact original GP/G16/G17 sprite number.
            // The current Melinoja bridge can expose compact non-empty frame ordinals on some packages
            // (SelFraDom4_1: missing real sprite 3 makes request 4 return visual 5).
            // Route through the exact adapter before the normal G16 bridge call.
            Texture2D tex = TryLoadG16FrameViaMelinojaExactV46LikeOriginal(abs, frameIndex, out string g16Source, logicalPackage);
            if (tex != null)
            {
                source = "G16ExactV46 " + g16Source;
                return tex;
            }
            if (g16Source.IndexOf("exact_missing", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                source = "G16ExactV46 " + g16Source;
                return null;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "building_gp_path_not_found:" + (abs ?? string.Empty) + " logical=" + (logicalPackage ?? "") + " base=" + g16Source;
                    return null;
                }

                Type bridgeType = ResolveMelinojaBridgeTypeV2LikeOriginal();
                if (bridgeType == null)
                {
                    source = "building GP bridge type not found logical=" + (logicalPackage ?? "") + " base=" + g16Source;
                    return null;
                }

                List<string> keys = C2Settlement3InuMdV2BuildGpAliasKeysV24LikeOriginal(abs, logicalPackage);
                List<string> audits = new List<string>();
                for (int k = 0; k < keys.Count; k++)
                {
                    Texture2D viaKey;
                    string keyAudit;
                    if (C2Settlement3InuMdV2TryLoadBridgeKeyFrameV24LikeOriginal(bridgeType, keys[k], frameIndex, out viaKey, out keyAudit) && viaKey != null)
                    {
                        source = "BuildingGPV24_ALIAS " + keyAudit + " abs=" + abs + " logical=" + (logicalPackage ?? "") + " base=" + g16Source;
                        return viaKey;
                    }
                    if (audits.Count < 10 && !string.IsNullOrEmpty(keyAudit)) audits.Add(keyAudit);
                }

                // Last safety: some bridge builds expose package readers only via the G2D path.
                // Try it on the same file even if the extension is .g16; if unsupported it just returns null.
                string g2dSource;
                tex = TryLoadG2DFrameViaMelinojaV3LikeOriginal(abs, frameIndex, out g2dSource);
                if (tex != null)
                {
                    source = "BuildingGPV24_G2D_FALLBACK " + g2dSource + " abs=" + abs + " logical=" + (logicalPackage ?? "") + " base=" + g16Source;
                    return tex;
                }

                source = "BuildingGPV24 no_alias_frame abs=" + abs + " logical=" + (logicalPackage ?? "") + " frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + " aliasAudit=" + string.Join(" || ", audits.ToArray()) + " g2d=" + g2dSource + " base=" + g16Source;
                return null;
            }
            catch (Exception ex)
            {
                source = "BuildingGPV24 failed: " + ex.GetType().Name + ":" + ex.Message + " abs=" + (abs ?? "") + " logical=" + (logicalPackage ?? "") + " base=" + g16Source;
                return null;
            }
        }

        private static Texture2D TryLoadG16FrameViaMelinojaExactV46LikeOriginal(string abs, int exactFrameIndex, out string source, string logicalPackage = null)
        {
            source = string.Empty;

            int melinojaFrameIndex;
            string exactAudit;
            if (!C2Settlement3InuMdV2ResolveMelinojaCompactFrameV46LikeOriginal(!string.IsNullOrEmpty(logicalPackage) ? logicalPackage : abs, exactFrameIndex, out melinojaFrameIndex, out exactAudit))
            {
                source = "exact_missing frame=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) + " " + exactAudit + " abs=" + (abs ?? "") + " logical=" + (logicalPackage ?? "");
                return null;
            }

            Texture2D tex = TryLoadG16FrameViaMelinojaV42LikeOriginal(abs, melinojaFrameIndex, out string baseSource);
            if (tex != null)
            {
                source = "exactFrame=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                         " melinojaFrame=" + melinojaFrameIndex.ToString(CultureInfo.InvariantCulture) +
                         " " + exactAudit + " base=" + baseSource;
                return tex;
            }

            source = "exactFrame=" + exactFrameIndex.ToString(CultureInfo.InvariantCulture) +
                     " melinojaFrame=" + melinojaFrameIndex.ToString(CultureInfo.InvariantCulture) +
                     " " + exactAudit + " base=" + baseSource;
            return null;
        }

        private static bool C2Settlement3InuMdV2ResolveMelinojaCompactFrameV46LikeOriginal(string packageOrPath, int exactFrameIndex, out int melinojaFrameIndex, out string audit)
        {
            melinojaFrameIndex = exactFrameIndex;
            audit = string.Empty;
            if (exactFrameIndex < 0)
            {
                audit = "negative_frame";
                return false;
            }

            List<int> missing = C2Settlement3InuMdV2MissingExactFramesV46LikeOriginal(packageOrPath);
            if (missing == null || missing.Count == 0)
            {
                audit = "exactMap=no_missing_known";
                return true;
            }

            int before = 0;
            for (int i = 0; i < missing.Count; i++)
            {
                int m = missing[i];
                if (m == exactFrameIndex)
                {
                    audit = "exactMap=missing_exact package='" + (packageOrPath ?? "") + "' missing=" + C2Settlement3InuMdV2JoinIntsV46LikeOriginal(missing) + "";
                    return false;
                }
                if (m < exactFrameIndex) before++;
            }

            melinojaFrameIndex = Math.Max(0, exactFrameIndex - before);
            audit = "exactMap=compact_repair package='" + (packageOrPath ?? "") + "' missingBefore=" + before.ToString(CultureInfo.InvariantCulture) +
                    " missing=" + C2Settlement3InuMdV2JoinIntsV46LikeOriginal(missing);
            return true;
        }

        private static List<int> C2Settlement3InuMdV2MissingExactFramesV46LikeOriginal(string packageOrPath)
        {
            Dictionary<string, List<int>> table = C2Settlement3InuMdV2MissingExactFrameTableV46LikeOriginal();
            if (table == null || table.Count == 0) return null;

            List<string> keys = C2Settlement3InuMdV2ExactFrameLookupKeysV46LikeOriginal(packageOrPath);
            for (int i = 0; i < keys.Count; i++)
            {
                List<int> found;
                if (table.TryGetValue(keys[i], out found) && found != null && found.Count > 0) return found;
            }
            return null;
        }

        private static Dictionary<string, List<int>> s_C2Settlement3InuMdV2MissingExactFrameTableV46;

        private static Dictionary<string, List<int>> C2Settlement3InuMdV2MissingExactFrameTableV46LikeOriginal()
        {
            if (s_C2Settlement3InuMdV2MissingExactFrameTableV46 != null) return s_C2Settlement3InuMdV2MissingExactFrameTableV46;

            var table = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            // V46 built-in confirmed case from audit/hash proof:
            // SelFraDom4_1 MD asks #STANDLO 0,1,2,3,4, but real GP/G16 sprite 3 is empty.
            // Melinoja compacted non-empty frames, so exact 4 was coming back as visual 5.
            C2Settlement3InuMdV2AddMissingExactFramesV46LikeOriginal(table, "UnitsG17\\SelFraDom4_1", new[] { 3 });
            C2Settlement3InuMdV2AddMissingExactFramesV46LikeOriginal(table, "UNITSG17_SELFRADOM4_1", new[] { 3 });
            C2Settlement3InuMdV2AddMissingExactFramesV46LikeOriginal(table, "SelFraDom4_1", new[] { 3 });

            // Optional fast manifest, no directory probing and no *_frames sidecar dependency.
            // Format examples:
            //   UnitsG17\SelFraDom4_1 = 3
            //   UNITSG17_SELFRADOM4_1: 3, 7, 55
            List<string> roots = C2Settlement3InuMdV2DataRootsLikeOriginal();
            string[] names = { "C2ExactMissingG16Frames.txt", "C2ExactMissingG17Frames.txt", "C2ExactMissingGpFrames.txt" };
            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrWhiteSpace(root)) continue;
                for (int n = 0; n < names.Length; n++)
                {
                    string path = Path.Combine(root, names[n]);
                    if (File.Exists(path)) C2Settlement3InuMdV2ReadMissingExactFrameManifestV46LikeOriginal(table, path);
                }
            }

            s_C2Settlement3InuMdV2MissingExactFrameTableV46 = table;
            return table;
        }

        private static void C2Settlement3InuMdV2ReadMissingExactFrameManifestV46LikeOriginal(Dictionary<string, List<int>> table, string path)
        {
            if (table == null || string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = C2Settlement3InuMdV2StripCommentLikeOriginal(lines[i]).Trim();
                    if (line.Length == 0) continue;
                    int sep = line.IndexOf('=');
                    if (sep < 0) sep = line.IndexOf(':');
                    if (sep <= 0 || sep + 1 >= line.Length) continue;

                    string key = line.Substring(0, sep).Trim();
                    string values = line.Substring(sep + 1).Trim();
                    string[] tokens = C2Settlement3InuMdV2SplitTokensLikeOriginal(values);
                    var missing = new List<int>();
                    for (int t = 0; t < tokens.Length; t++)
                    {
                        if (!C2Settlement3InuMdV2LooksLikeIntLikeOriginal(tokens[t])) continue;
                        int v = C2Settlement3InuMdV2ToInt(tokens[t]);
                        if (v >= 0 && !missing.Contains(v)) missing.Add(v);
                    }
                    if (missing.Count > 0) C2Settlement3InuMdV2AddMissingExactFramesV46LikeOriginal(table, key, missing.ToArray());
                }
            }
            catch { }
        }

        private static void C2Settlement3InuMdV2AddMissingExactFramesV46LikeOriginal(Dictionary<string, List<int>> table, string key, int[] missing)
        {
            if (table == null || string.IsNullOrWhiteSpace(key) || missing == null || missing.Length == 0) return;
            List<string> keys = C2Settlement3InuMdV2ExactFrameLookupKeysV46LikeOriginal(key);
            for (int k = 0; k < keys.Count; k++)
            {
                string kk = keys[k];
                if (string.IsNullOrWhiteSpace(kk)) continue;
                List<int> list;
                if (!table.TryGetValue(kk, out list) || list == null)
                {
                    list = new List<int>();
                    table[kk] = list;
                }
                for (int i = 0; i < missing.Length; i++)
                {
                    int v = missing[i];
                    if (v >= 0 && !list.Contains(v)) list.Add(v);
                }
                list.Sort();
            }
        }

        private static List<string> C2Settlement3InuMdV2ExactFrameLookupKeysV46LikeOriginal(string packageOrPath)
        {
            var keys = new List<string>();
            Action<string> add = k =>
            {
                if (string.IsNullOrWhiteSpace(k)) return;
                k = k.Trim().Trim('"', '\'');
                if (k.Length == 0) return;
                for (int i = 0; i < keys.Count; i++)
                    if (string.Equals(keys[i], k, StringComparison.OrdinalIgnoreCase)) return;
                keys.Add(k);
            };

            string raw = packageOrPath ?? string.Empty;
            raw = raw.Replace('/', '\\');
            add(raw);
            add(raw.ToUpperInvariant());

            string noExt = Path.ChangeExtension(raw, null) ?? raw;
            add(noExt);
            add(noExt.ToUpperInvariant());

            string stem = Path.GetFileNameWithoutExtension(raw);
            add(stem);
            add(stem.ToUpperInvariant());

            string flat = noExt.Replace('\\', '_').Replace('/', '_');
            add(flat);
            add(flat.ToUpperInvariant());

            const string unitsPrefix = "UNITSG17_";
            if (flat.StartsWith(unitsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string rest = flat.Substring(unitsPrefix.Length);
                add(rest);
                add(rest.ToUpperInvariant());
                add("UnitsG17\\" + rest);
                add(("UnitsG17\\" + rest).ToUpperInvariant());
            }
            else if (!string.IsNullOrEmpty(stem))
            {
                add("UnitsG17\\" + stem);
                add(("UnitsG17\\" + stem).ToUpperInvariant());
                add("UNITSG17_" + stem);
                add(("UNITSG17_" + stem).ToUpperInvariant());
            }

            return keys;
        }

        private static string C2Settlement3InuMdV2JoinIntsV46LikeOriginal(List<int> values)
        {
            if (values == null || values.Count == 0) return "";
            string[] parts = new string[values.Count];
            for (int i = 0; i < values.Count; i++) parts[i] = values[i].ToString(CultureInfo.InvariantCulture);
            return string.Join(",", parts);
        }

        private static List<string> C2Settlement3InuMdV2BuildGpAliasKeysV24LikeOriginal(string abs, string logicalPackage)
        {
            List<string> keys = new List<string>();
            C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, logicalPackage);
            if (!string.IsNullOrEmpty(logicalPackage))
            {
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, logicalPackage.Replace('/', '\\'));
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, logicalPackage.Replace('\\', '/'));
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, logicalPackage.ToUpperInvariant());
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, Path.GetFileName(logicalPackage));
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, Path.GetFileName(logicalPackage).ToUpperInvariant());
            }

            C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, abs);
            if (!string.IsNullOrEmpty(abs))
            {
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, Path.ChangeExtension(abs, null));
                string stem = Path.GetFileNameWithoutExtension(abs);
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, stem);
                C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, stem.ToUpperInvariant());

                const string unitsG17Prefix = "UNITSG17_";
                if (stem.StartsWith(unitsG17Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string rest = stem.Substring(unitsG17Prefix.Length);
                    C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, rest);
                    C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, rest.ToUpperInvariant());
                    C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, "UnitsG17\\" + rest);
                    C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(keys, "UnitsG17/" + rest);
                }
            }

            return keys;
        }

        private static void C2Settlement3InuMdV2AddAliasKeyV24LikeOriginal(List<string> keys, string key)
        {
            if (keys == null || string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim().Trim('"', '\'');
            if (key.Length == 0) return;
            for (int i = 0; i < keys.Count; i++)
            {
                if (string.Equals(keys[i], key, StringComparison.OrdinalIgnoreCase)) return;
            }
            keys.Add(key);
        }

        private static bool C2Settlement3InuMdV2TryLoadBridgeKeyFrameV24LikeOriginal(Type bridgeType, string key, int frameIndex, out Texture2D tex, out string audit)
        {
            tex = null;
            audit = string.Empty;
            if (bridgeType == null || string.IsNullOrWhiteSpace(key))
            {
                audit = "empty_bridge_or_key";
                return false;
            }

            string loadAudit = string.Empty;
            if (!C2SettlementBuildingsLoadedGpV23LikeOriginal.Contains(key))
            {
                string[] loadNames = { "LoadG17ToMemory", "LoadGPToMemory", "LoadPackageToMemory", "LoadG16ToMemory" };
                for (int i = 0; i < loadNames.Length; i++)
                {
                    MethodInfo load = bridgeType.GetMethod(loadNames[i], BindingFlags.Public | BindingFlags.Static);
                    if (load == null) continue;
                    ParameterInfo[] ps = load.GetParameters();
                    object[] args = null;
                    if (ps.Length == 3) args = new object[] { key, null, false };
                    else if (ps.Length == 2) args = new object[] { key, null };
                    else if (ps.Length == 1) args = new object[] { key };
                    if (args == null) continue;

                    try
                    {
                        object result = load.Invoke(null, args);
                        bool ok = result is bool b ? b : true;
                        string err = args.Length > 1 ? args[1] as string : string.Empty;
                        loadAudit += loadNames[i] + "=" + ok + (string.IsNullOrEmpty(err) ? "" : ":" + err) + ";";
                        if (ok) break;
                    }
                    catch (Exception ex)
                    {
                        loadAudit += loadNames[i] + "=EX:" + ex.GetType().Name + ";";
                    }
                }
                C2SettlementBuildingsLoadedGpV23LikeOriginal.Add(key);
            }

            int melinojaFrameIndex;
            string exactAudit;
            if (!C2Settlement3InuMdV2ResolveMelinojaCompactFrameV46LikeOriginal(key, frameIndex, out melinojaFrameIndex, out exactAudit))
            {
                audit = "key='" + key + "' exact_missing frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + " " + exactAudit + " load=" + loadAudit;
                return false;
            }

            string[] frameNamesExact = { "TryGetG17FrameRGBAExact", "TryGetGPFrameRGBAExact", "TryGetPackageFrameRGBAExact", "TryGetFrameRGBAExact", "TryGetG16FrameRGBAExact" };
            string[] frameNames = { "TryGetG17FrameRGBA", "TryGetGPFrameRGBA", "TryGetPackageFrameRGBA", "TryGetFrameRGBA", "TryGetG16FrameRGBA" };
            string frameAudit = string.Empty;
            for (int i = 0; i < frameNamesExact.Length + frameNames.Length; i++)
            {
                bool exactApi = i < frameNamesExact.Length;
                string frameMethodName = exactApi ? frameNamesExact[i] : frameNames[i - frameNamesExact.Length];
                MethodInfo mi = bridgeType.GetMethod(frameMethodName, BindingFlags.Public | BindingFlags.Static);
                if (mi == null) continue;
                ParameterInfo[] ps = mi.GetParameters();
                if (ps.Length != 6)
                {
                    frameAudit += frameMethodName + ":bad_sig" + ps.Length.ToString(CultureInfo.InvariantCulture) + ";";
                    continue;
                }

                try
                {
                    object[] args = { key, exactApi ? frameIndex : melinojaFrameIndex, 0, 0, null, null };
                    object result = mi.Invoke(null, args);
                    if (!(result is bool ok) || !ok)
                    {
                        string err = args.Length > 5 ? args[5] as string : string.Empty;
                        frameAudit += frameMethodName + "=false" + (string.IsNullOrEmpty(err) ? "" : ":" + err) + ";";
                        continue;
                    }

                    int w = args[2] is int iw ? iw : 0;
                    int h = args[3] is int ih ? ih : 0;
                    byte[] rgba = args[4] as byte[];
                    if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                    {
                        frameAudit += frameMethodName + ":invalid_size " + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture) + ";";
                        continue;
                    }

                    tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                    tex.name = "C2_BLD_GP_ALIAS_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(key) + "_frame_" + frameIndex.ToString(CultureInfo.InvariantCulture);
                    tex.LoadRawTextureData(rgba);
                    tex.Apply(false, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    audit = "key='" + key + "' exactFrame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + " melinojaFrame=" + (exactApi ? frameIndex : melinojaFrameIndex).ToString(CultureInfo.InvariantCulture) + " via=" + frameMethodName + " " + exactAudit + " load=" + loadAudit;
                    return true;
                }
                catch (Exception ex)
                {
                    frameAudit += frameMethodName + "=EX:" + ex.GetType().Name + ";";
                }
            }

            audit = "key='" + key + "' exactFrame=" + frameIndex.ToString(CultureInfo.InvariantCulture) + " melinojaFrame=" + melinojaFrameIndex.ToString(CultureInfo.InvariantCulture) + " " + exactAudit + " load=" + loadAudit + " frames=" + frameAudit;
            return false;
        }

        private static string C2Settlement3InuMdV2StripCommentLikeOriginal(string s)
        {
            if (s == null) return "";
            int p = s.IndexOf("//", StringComparison.Ordinal);
            if (p >= 0) s = s.Substring(0, p);
            return s;
        }

        private static string[] C2Settlement3InuMdV2SplitTokensLikeOriginal(string s)
        {
            return (s ?? "").Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int C2Settlement3InuMdV2ParseLineSortTokensLikeOriginal(List<C2Settlement3InuMdV2LineSortInfo> target, string[] st, int start, int expected)
        {
            if (target == null || st == null) return 0;
            int added = 0;
            int p = Math.Max(0, start);
            while (p < st.Length && target.Count < expected)
            {
                string scmd = (st[p] ?? string.Empty).Trim().ToUpperInvariant();
                if (scmd.Length == 0) { p++; continue; }

                if (scmd == "GROUND")
                {
                    target.Add(new C2Settlement3InuMdV2LineSortInfo(
                        C2Settlement3InuMdV2AlignGround,
                        C2Settlement3InuMdV2AlignGround,
                        C2Settlement3InuMdV2AlignGround,
                        C2Settlement3InuMdV2AlignGround));
                    added++;
                    p++;
                    continue;
                }

                if (scmd == "TOP" || scmd == "TOPMOST")
                {
                    target.Add(new C2Settlement3InuMdV2LineSortInfo(
                        C2Settlement3InuMdV2AlignTopmost,
                        C2Settlement3InuMdV2AlignTopmost,
                        C2Settlement3InuMdV2AlignTopmost,
                        C2Settlement3InuMdV2AlignTopmost));
                    added++;
                    p++;
                    continue;
                }

                if (scmd == "POINT" && p + 2 < st.Length)
                {
                    int px = C2Settlement3InuMdV2ToInt(st[p + 1]);
                    int py = C2Settlement3InuMdV2ToInt(st[p + 2]);
                    target.Add(new C2Settlement3InuMdV2LineSortInfo(px, py, px, py));
                    added++;
                    p += 3;
                    continue;
                }

                if (scmd == "LINE" && p + 4 < st.Length)
                {
                    target.Add(new C2Settlement3InuMdV2LineSortInfo(
                        C2Settlement3InuMdV2ToInt(st[p + 1]),
                        C2Settlement3InuMdV2ToInt(st[p + 2]),
                        C2Settlement3InuMdV2ToInt(st[p + 3]),
                        C2Settlement3InuMdV2ToInt(st[p + 4])));
                    added++;
                    p += 5;
                    continue;
                }

                if (p + 3 < st.Length &&
                    C2Settlement3InuMdV2LooksLikeIntLikeOriginal(st[p]) &&
                    C2Settlement3InuMdV2LooksLikeIntLikeOriginal(st[p + 1]) &&
                    C2Settlement3InuMdV2LooksLikeIntLikeOriginal(st[p + 2]) &&
                    C2Settlement3InuMdV2LooksLikeIntLikeOriginal(st[p + 3]))
                {
                    target.Add(new C2Settlement3InuMdV2LineSortInfo(
                        C2Settlement3InuMdV2ToInt(st[p]),
                        C2Settlement3InuMdV2ToInt(st[p + 1]),
                        C2Settlement3InuMdV2ToInt(st[p + 2]),
                        C2Settlement3InuMdV2ToInt(st[p + 3])));
                    added++;
                    p += 4;
                    continue;
                }

                break;
            }
            return added;
        }

        private static void C2Settlement3InuMdV2PostProcessLineSortLikeOriginal(List<C2Settlement3InuMdV2LineSortInfo> lineSort)
        {
            if (lineSort == null || lineSort.Count == 0) return;
            bool hasGround = false;
            int minX = 10000;
            int maxX = -10000;
            for (int i = 0; i < lineSort.Count; i++)
            {
                C2Settlement3InuMdV2LineSortInfo li = lineSort[i];
                if (li.IsGround)
                {
                    hasGround = true;
                    continue;
                }
                if (li.IsTop) continue;
                if (li.X1 < minX) minX = li.X1;
                if (li.X2 < minX) minX = li.X2;
                if (li.X1 > maxX) maxX = li.X1;
                if (li.X2 > maxX) maxX = li.X2;
            }

            if (!hasGround || minX > maxX) return;
            int avx = (minX + maxX) >> 1;
            for (int i = 0; i < lineSort.Count; i++)
            {
                if (!lineSort[i].IsGround) continue;
                lineSort[i] = new C2Settlement3InuMdV2LineSortInfo(C2Settlement3InuMdV2AlignGround, -10, avx, -10);
            }
        }

        private static bool C2Settlement3InuMdV2LooksLikeIntLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            if (s[0] == '-' || s[0] == '+') i = 1;
            if (i >= s.Length) return false;
            for (; i < s.Length; i++)
            {
                if (!char.IsDigit(s[i])) return false;
            }
            return true;
        }

        private static int C2Settlement3InuMdV2ToInt(string s)
        {
            int v;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return v;
            if (!string.IsNullOrEmpty(s) && s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return v;
            return 0;
        }

        private static string C2Settlement3InuMdV2CleanPackageNameLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim().Trim('"', '\'');
            s = s.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            if (s.EndsWith(".g16", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".g2d", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".gp", StringComparison.OrdinalIgnoreCase))
                s = Path.ChangeExtension(s, null);
            return s;
        }

        private static string C2Settlement3InuMdV2SanitizeNameLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else if (c == '(' || c == ')' || c == ' ' || c == '.') { }
                else sb.Append('_');
            }
            return sb.Length == 0 ? "_" : sb.ToString();
        }

        private static void C2Settlement3InuMdV2Count(Dictionary<string, int> d, string k)
        {
            if (string.IsNullOrEmpty(k)) k = "<empty>";
            int v; d.TryGetValue(k, out v); d[k] = v + 1;
        }

        private static void C2Settlement3InuMdV2AddLimited(List<string> list, string item, int max)
        {
            if (list.Count >= max) return;
            if (!list.Contains(item)) list.Add(item);
        }

        private static string C2Settlement3InuMdV2TopNamesLikeOriginal(Dictionary<string, int> counts, int max)
        {
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            int n = Math.Min(max, list.Count);
            var parts = new List<string>();
            for (int i = 0; i < n; i++) parts.Add(list[i].Key + ":" + list[i].Value);
            if (list.Count > n) parts.Add("...+" + (list.Count - n));
            return string.Join(",", parts.ToArray());
        }
    }

    public sealed class C2Settlement3InuMdV2FrameAnimator : MonoBehaviour
    {
        public Texture2D[] Textures;
        public Vector3[][] Vertices;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public float FrameRate = 12.0f;

        private int _lastFrame = -1;

        private void Update()
        {
            if (Textures == null || Textures.Length == 0 || Mesh == null || Renderer == null) return;
            float fps = FrameRate > 0.01f ? FrameRate : 12.0f;
            int frame = ((int)(Time.time * fps)) % Textures.Length;
            if (frame == _lastFrame) return;

            _lastFrame = frame;
            Texture2D tex = Textures[frame];
            if (Renderer.sharedMaterial != null && tex != null) Renderer.sharedMaterial.mainTexture = tex;
            if (Vertices != null && frame < Vertices.Length && Vertices[frame] != null)
            {
                Mesh.vertices = Vertices[frame];
                Mesh.RecalculateBounds();
            }
        }
    }

    internal sealed class C2Settlement3InuMdV2ScreenBillboard : MonoBehaviour
    {
        private Camera _cam;

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // Original Cossacks buildings are screen-space GP/G16 sprites anchored to map coordinates.
            // Keep the quad facing the camera to avoid perspective stretching while panning/zooming.
            transform.rotation = _cam.transform.rotation;
        }
    }

}
