
// C2SettlementBuildings3INUParserLikeOriginal.cs
// V21: rollback to V19 confirmed look + cache index; no risky mine alias changes; WORK preview restored.
 //      Keeps V12/V14 MD composite/path fixes, removes V16 screen-billboard and shadow/alpha edits.
 //      Still does not touch trees/shadows.
// Buildings: MonsterID -> .md -> USERLC/USERLCEXT -> .g16 sprite.
// Units:     MonsterID -> .md -> USERLC/USERLCEXT -> .g2d sprite.
// Does NOT use OC/COMPLEX as buildings. Does NOT touch trees/shadows.

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
        private const string Settlement3InuMdV2RootPrefix = "C2_SettlementBuildings_3INU_MD_V21_";
        private const string Settlement3InuMdV2MaterialName = "C2_SettlementBuildings_3INU_MD_V21_Mat";
        // V21: original C2 buildings are screen-space G16 quads mapped into Unity world.
        // V10-V17 used the wall/terrain pixel scale directly, which made houses visibly too large.
        // Keep this as a single compensator so it can be tuned without touching MD/G16 parsing.
        private const float Settlement3InuMdV2SpriteScaleCompensator = 0.82f;
        // V21: restore V19 behavior: draw the first WORK frame so mines/mills keep their visible moving/extra element.
        // This is the user-confirmed better visual baseline.
        private const bool Settlement3InuMdV2DrawWorkStaticPreview = true;

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

        private sealed class C2Settlement3InuMdV2Info
        {
            public bool Found;
            public string MdPath;
            public string MdName;
            public string Package;
            public string PreferredExt;
            public int SpriteId;
            public readonly List<C2Settlement3InuMdV2AnimFrame> StandLoFrames = new List<C2Settlement3InuMdV2AnimFrame>();
            // #WORK/@WORK frames are not part of the base house body. Original switches these over time.
            // V14 draws the first work frame as a separate top layer so mills/mines do not miss their animated element.
            public readonly List<C2Settlement3InuMdV2AnimFrame> WorkFrames = new List<C2Settlement3InuMdV2AnimFrame>();
            public int Rotations = 1;
            public int Dx;
            public int Dy;
            public int PicDx;
            public int PicDy;
            public int PicLx;
            public int PicLy;
            public bool Building;
            public bool SpriteObject;
            public bool ParsedAnimation;
            public bool HasUserLc;
            public string Usage;
            public C2Settlement3InuMdV2Kind Kind;
            public string Audit;
        }

        private static readonly Dictionary<string, C2Settlement3InuMdV2Info> Settlement3InuMdV2MdCache = new Dictionary<string, C2Settlement3InuMdV2Info>(StringComparer.OrdinalIgnoreCase);
        private static Material Settlement3InuMdV2Material;

        // Optional manual fallback. Normally auto-detected from C2BattleTerrainMode._mapRelativePath.
        public string Settlement3InuMdV2MapPathOverride = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void C2Settlement3InuMdV6AutoInstallLikeOriginal()
        {
            if (!Settlement3InuMdV2Enabled) return;

            var existing = UnityEngine.Object.FindObjectOfType<C2Settlement3InuMdV6AutoRunner>();
            if (existing != null) return;

            var go = new GameObject("C2_SettlementBuildings_3INU_MD_V21_AutoRunner");
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
                        Debug.Log("[C2:SETTLEMENT 3INU V21 WAIT] mode found mapPath='" +
                                  (map ?? "<null>") + "' mapObjectReady=" + mapObjectReady +
                                  " hint=waiting for [C2:MAP] Parsed clean map / _mapRelativePath");
                    }
                    return;
                }

                if (_lastMode == mode && string.Equals(_lastMap, map, StringComparison.OrdinalIgnoreCase))
                    return;

                _lastMode = mode;
                _lastMap = map;
                mode.BuildSettlementBuildingsFrom3InuMdV2LikeOriginal(map, "auto-runner-v21");
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
            C2Settlement3InuMdV2DisableWrongOcComplexAdapterLikeOriginal();
            C2Settlement3InuMdV2ClearOldRootsLikeOriginal();

            if (_map == null)
            {
                Debug.LogWarning("[C2:SETTLEMENT 3INU V21] parsed terrain map object is not ready yet; skip build source=" + source + " map='" + (mapPath ?? "<null>") + "'");
                return;
            }

            string abs = C2Settlement3InuMdV2ResolveMapPathLikeOriginal(mapPath);
            if (string.IsNullOrEmpty(abs) || !File.Exists(abs))
            {
                Debug.LogWarning("[C2:SETTLEMENT 3INU V21] map not found: " + (mapPath ?? "<null>"));
                return;
            }

            List<C2Settlement3InuMdV2Record> records;
            string chunkAudit;
            if (!C2Settlement3InuMdV2TryParseRecordsLikeOriginal(abs, out records, out chunkAudit))
            {
                Debug.LogWarning("[C2:SETTLEMENT 3INU V21] no 3INU/UNI3 records map='" + mapPath + "' audit=" + chunkAudit);
                return;
            }

            var root = new GameObject(Settlement3InuMdV2RootPrefix + Path.GetFileNameWithoutExtension(abs));
            root.transform.SetParent(transform, true);

            int mdFound = 0, mdMissing = 0, visualFound = 0, visualMissing = 0;
            int buildings = 0, resources = 0, settlements = 0, units = 0, animals = 0, unknown = 0;
            int drawn = 0, skipped = 0;
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mdMiss = new List<string>();
            var visMiss = new List<string>();
            var sample = new List<string>();

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

                bool shouldDraw = false;
                if (Settlement3InuMdV2DrawBuildings && (kind == C2Settlement3InuMdV2Kind.SettlementBuilding || kind == C2Settlement3InuMdV2Kind.Building || kind == C2Settlement3InuMdV2Kind.ResourceBuilding || kind == C2Settlement3InuMdV2Kind.SpriteObject)) shouldDraw = true;
                if (Settlement3InuMdV2DrawUnitsWhenMdResolved && md.Found && kind == C2Settlement3InuMdV2Kind.Unit) shouldDraw = true;
                if (Settlement3InuMdV2DrawAnimals && md.Found && kind == C2Settlement3InuMdV2Kind.Animal) shouldDraw = true;
                if (!shouldDraw) { skipped++; continue; }

                List<Texture2D> textures = null;
                string visualAudit = string.Empty;
                bool ok = md.Found && C2Settlement3InuMdV2TryLoadVisualFramesLikeOriginal(md, r, kind, out textures, out visualAudit);
                if (!ok || textures == null || textures.Count == 0)
                {
                    visualMissing++;
                    C2Settlement3InuMdV2AddLimited(visMiss, r.MonsterId + " md=" + (md.Found ? md.MdPath : "<missing>") + " pkg=" + (md.Package ?? "<none>") + " audit=" + visualAudit, 18);
                    if (!Settlement3InuMdV2DrawMissingMdMarkers) { skipped++; continue; }
                    C2Settlement3InuMdV2CreateMdBoundsFallbackLikeOriginal(root.transform, r, md, kind, md.Found ? "MD_NO_VISUAL" : "NO_MD");
                    drawn++;
                }
                else
                {
                    visualFound++;
                    C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal(root.transform, r, md, kind, textures, visualAudit);
                    drawn++;
                }

                if (sample.Count < 48)
                {
                    sample.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) + " kind=" + kind + " name='" + r.MonsterId + "' md=" + (md.Found ? Path.GetFileName(md.MdPath) : "<missing>") + " pkg='" + (md.Package ?? "") + "' frame=" + md.SpriteId + "/parts=" + (md.StandLoFrames != null ? md.StandLoFrames.Count : 0) + "/work=" + (md.WorkFrames != null ? md.WorkFrames.Count : 0) + " real=(" + r.RealX + "," + r.RealY + ") map=(" + (r.RealX >> 4) + "," + (r.RealY >> 4) + ") dir=" + r.RealDir);
                }
            }

            Debug.Log("[C2:SETTLEMENT 3INU V21] contract=V21_SaveNewMap_LoadUnits3_3INU_MD_USERLC_G16_buildings_V19_LOOK_CACHE_INDEX_WORK_PREVIEW_SAFE_RESOURCE_ALIAS source=" + source + " map='" + mapPath + "' records=" + records.Count + " mdFound=" + mdFound + " mdMissing=" + mdMissing + " visualFound=" + visualFound + " visualMissing=" + visualMissing + " settlements=" + settlements + " buildings=" + buildings + " resourceBuildings=" + resources + " units=" + units + " animals=" + animals + " unknown=" + unknown + " drawn=" + drawn + " skipped=" + skipped + " chunkAudit=" + chunkAudit + " names=" + C2Settlement3InuMdV2TopNamesLikeOriginal(nameCounts, 40));
            if (sample.Count > 0) Debug.Log("[C2:SETTLEMENT 3INU V21 SAMPLE] " + string.Join(" | ", sample.ToArray()));
            if (mdMiss.Count > 0) Debug.LogWarning("[C2:SETTLEMENT 3INU V21 MD MISS] " + string.Join(" | ", mdMiss.ToArray()));
            if (visMiss.Count > 0) Debug.LogWarning("[C2:SETTLEMENT 3INU V21 VISUAL MISS] " + string.Join(" | ", visMiss.ToArray()));
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
                    Debug.Log("[C2:SETTLEMENT 3INU V21] disabled wrong OC/COMPLEX building adapter: " + n);
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

                // Original SaveNewMap M3D header:
                // [4] magic, [4] stored VertInLine, [4] stored MaxTH, then chunks.
                // V5 forgot these 8 bytes and therefore read stored dimensions as fake chunks.
                int storedVertInLine = 0;
                int storedMaxTH = 0;
                if (ms.Position + 8 <= ms.Length)
                {
                    storedVertInLine = br.ReadInt32();
                    storedMaxTH = br.ReadInt32();
                }

                int chunks = 0;
                List<string> seen = new List<string>(32);

                while (ms.Position + 8 <= ms.Length)
                {
                    long chunkStart = ms.Position;
                    string tag = ReadTag(br);
                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal) ||
                        string.Equals(tag, "MDNE", StringComparison.Ordinal))
                    {
                        audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                                " chunksSeen=" + chunks + " endTag=" + tag + " no_3INU seen=" + string.Join(",", seen.ToArray());
                        return false;
                    }

                    int sizeField = br.ReadInt32();

                    // Original chunk size includes the 4-byte size field itself.
                    // Payload begins after size and is therefore sizeField - 4.
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;
                    long payloadEnd = payloadStart + payloadLen;
                    if (payloadEnd > ms.Length)
                    {
                        audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                                " chunksSeen=" + chunks + " brokenChunk tag=" + tag +
                                " chunkStart=" + chunkStart + " sizeField=" + sizeField +
                                " payloadLen=" + payloadLen + " fileLen=" + ms.Length +
                                " seen=" + string.Join(",", seen.ToArray());
                        return false;
                    }

                    chunks++;
                    if (seen.Count < 32) seen.Add(tag + ":" + sizeField.ToString(CultureInfo.InvariantCulture));

                    if (TagEqualsLikeOriginal(tag, "3INU", "UNI3"))
                    {
                        if (payloadLen < 4)
                        {
                            audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                                    " chunksSeen=" + chunks + " tag=" + tag + " payload_too_small=" + payloadLen;
                            return false;
                        }

                        int declared = br.ReadInt32();
                        int possible = Mathf.Max(0, (payloadLen - 4) / Settlement3InuMdV2RecordSize);
                        int count = Mathf.Clamp(declared, 0, possible);
                        long recordsEnd = Math.Min(payloadEnd, ms.Position + (long)count * Settlement3InuMdV2RecordSize);

                        for (int i = 0; i < count; i++)
                        {
                            if (ms.Position + Settlement3InuMdV2RecordSize > recordsEnd)
                                break;

                            var r = new C2Settlement3InuMdV2Record();
                            r.Index = i;
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

                        audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                                " chunksSeen=" + chunks + " tag=" + tag +
                                " sizeField=" + sizeField + " payload=" + payloadLen +
                                " declared=" + declared + " possible=" + possible +
                                " parsed=" + records.Count + " seen=" + string.Join(",", seen.ToArray());
                        return records.Count > 0;
                    }

                    ms.Position = payloadEnd;
                }

                audit = "magic=" + magic + " stored=" + storedVertInLine + "x" + storedMaxTH +
                        " chunksSeen=" + chunks + " no_3INU seen=" + string.Join(",", seen.ToArray());
                return false;
            }
        }

        private static string C2Settlement3InuMdV2DecodeCStringLikeOriginal(byte[] b)
        {
            int n = 0;
            while (n < b.Length && b[n] != 0) n++;
            try { return Encoding.GetEncoding(1251).GetString(b, 0, n).Trim(); }
            catch { return Encoding.ASCII.GetString(b, 0, n).Trim(); }
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
            add(raw);
            int p = raw.IndexOf('(');
            string baseName = p > 0 ? raw.Substring(0, p).Trim() : raw;
            add(baseName);
            string suffix = "";
            int p2 = raw.IndexOf(')');
            if (p >= 0 && p2 > p) suffix = raw.Substring(p + 1, p2 - p - 1).Trim();
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

            // Resource aliases used by saved 3INU IDs vs actual MD/cache names.
            // V21: rollback to the V19-safe resource aliases.
            // Do NOT guess Gold/Iron as Kam/Gln here: that broke visible mine parts in V20.
            // Until the original MonsterID->MD table is ported, prefer the stable generic ore mine over wrong/broken variants.
            if (string.Equals(baseName, "BldRudCoal", StringComparison.OrdinalIgnoreCase)) { add("BldRudUgl"); add("BldRudCoal"); }
            if (string.Equals(baseName, "BldRudIron", StringComparison.OrdinalIgnoreCase)) { add("BldRudRud"); }
            if (string.Equals(baseName, "BldRudGold", StringComparison.OrdinalIgnoreCase)) { add("BldRudRud"); }
            if (string.Equals(baseName, "BldRudSel", StringComparison.OrdinalIgnoreCase)) add("BldRudSel");
            if (string.Equals(baseName, "BldRudGln", StringComparison.OrdinalIgnoreCase)) add("BldRudGln");

            add(C2Settlement3InuMdV2SanitizeNameLikeOriginal(raw));
            add(C2Settlement3InuMdV2SanitizeNameLikeOriginal(baseName));
            return list;
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
                else if (cmd == "USAGE" && t.Length >= 2)
                {
                    info.Usage = t[1];
                    string u = t[1].ToUpperInvariant();
                    if (u.IndexOf("MELN") >= 0 || u.IndexOf("MINE") >= 0 || u.IndexOf("RUD") >= 0 || u.IndexOf("SKLAD") >= 0 || u.IndexOf("WOOD") >= 0 || u.IndexOf("LES") >= 0)
                        info.Kind = C2Settlement3InuMdV2Kind.ResourceBuilding;
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
                    // USERLC    p1 package gz dx dy
                    // USERLCEXT p1 p4 p5 package gz dx dy
                    if (t.Length >= 6 + shift)
                    {
                        info.Package = C2Settlement3InuMdV2CleanPackageNameLikeOriginal(t[2 + shift]);
                        info.Dx = C2Settlement3InuMdV2ToInt(t[4 + shift]);
                        info.Dy = C2Settlement3InuMdV2ToInt(t[5 + shift]);
                        info.HasUserLc = true;
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '#')
                {
                    // Original MD animation format:
                    // #STANDLO Rotations NFrames FileID_1 SpriteID_1 FileID_2 SpriteID_2 ...
                    // NewMonster.cpp allocates NFrames and fills every frame. ShowBuilding() then draws
                    // ALL StandLo frames at the same x0/y0, so many buildings are composite sprites.
                    // V11 drew only the first frame, therefore houses were missing walls/roof pieces.
                    if (t.Length >= 3)
                    {
                        int rotations = Math.Max(1, C2Settlement3InuMdV2ToInt(t[1]));
                        int frames = Math.Max(0, C2Settlement3InuMdV2ToInt(t[2]));
                        bool isStandLo = string.Equals(cmdRaw, "#STANDLO", StringComparison.OrdinalIgnoreCase);
                        bool isWork = string.Equals(cmdRaw, "#WORK", StringComparison.OrdinalIgnoreCase);

                        var parsedFrames = new List<C2Settlement3InuMdV2AnimFrame>();
                        for (int q = 0; q < frames; q++)
                        {
                            int a = 3 + q * 2;
                            if (a + 1 >= t.Length) break;
                            parsedFrames.Add(new C2Settlement3InuMdV2AnimFrame(C2Settlement3InuMdV2ToInt(t[a]), C2Settlement3InuMdV2ToInt(t[a + 1])));
                        }

                        if (isStandLo)
                        {
                            info.Rotations = rotations;
                            info.StandLoFrames.Clear();
                            info.StandLoFrames.AddRange(parsedFrames);
                            if (parsedFrames.Count > 0) info.SpriteId = parsedFrames[0].SpriteId;
                            info.ParsedAnimation = true;
                        }
                        else if (isWork && parsedFrames.Count > 0)
                        {
                            info.WorkFrames.Clear();
                            info.WorkFrames.AddRange(parsedFrames);
                        }
                        else if (!info.ParsedAnimation && parsedFrames.Count > 0)
                        {
                            info.Rotations = rotations;
                            info.SpriteId = parsedFrames[0].SpriteId;
                            info.ParsedAnimation = true;
                        }
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '@')
                {
                    // @ANIM rotations slot start end
                    if (t.Length >= 5)
                    {
                        info.Rotations = Math.Max(1, C2Settlement3InuMdV2ToInt(t[1]));
                        int fileRef = C2Settlement3InuMdV2ToInt(t[2]);
                        int startFrame = C2Settlement3InuMdV2ToInt(t[3]);
                        int endFrame = C2Settlement3InuMdV2ToInt(t[4]);

                        if (string.Equals(cmdRaw, "@WORK", StringComparison.OrdinalIgnoreCase))
                        {
                            info.WorkFrames.Clear();
                            int a = Math.Min(startFrame, endFrame);
                            int b = Math.Max(startFrame, endFrame);
                            // safety cap: we only need a visible/animated layer, not thousands of frames
                            for (int fr = a; fr <= b && info.WorkFrames.Count < 256; fr++)
                                info.WorkFrames.Add(new C2Settlement3InuMdV2AnimFrame(fileRef, fr));
                        }

                        info.SpriteId = startFrame;
                        info.ParsedAnimation = true;
                    }
                }
                else if (cmd.Length > 1 && cmd[0] == '$')
                {
                    // $ANIM rotations parts; next part lines often contain sprite ids. Use first available token as fallback.
                    if (t.Length >= 3)
                    {
                        info.Rotations = Math.Max(1, C2Settlement3InuMdV2ToInt(t[1]));
                        int parts = Math.Max(0, C2Settlement3InuMdV2ToInt(t[2]));
                        if (parts > 0 && i + 1 < lines.Length)
                        {
                            string[] f = C2Settlement3InuMdV2SplitTokensLikeOriginal(C2Settlement3InuMdV2StripCommentLikeOriginal(lines[i + 1]).Trim());
                            for (int q = f.Length - 1; q >= 0; q--)
                            {
                                int val;
                                if (int.TryParse(f[q], NumberStyles.Integer, CultureInfo.InvariantCulture, out val)) { info.SpriteId = val; break; }
                            }
                            info.ParsedAnimation = true;
                        }
                        i += parts;
                    }
                }
            }

            if (info.Kind == C2Settlement3InuMdV2Kind.Unknown)
            {
                if (info.Building) info.Kind = C2Settlement3InuMdV2Kind.Building;
                else info.Kind = C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(info.MdName);
            }
            if (info.Kind == C2Settlement3InuMdV2Kind.Unit || info.Kind == C2Settlement3InuMdV2Kind.Animal) info.PreferredExt = ".g2d";
            else info.PreferredExt = ".g16";
            info.Audit = "pkg=" + (info.Package ?? "<none>") + " frame=" + info.SpriteId + " kind=" + info.Kind + " building=" + info.Building + " usage=" + (info.Usage ?? "") + " loc=" + info.PicDx + "," + info.PicDy + "," + info.PicLx + "," + info.PicLy + " standLoParts=" + (info.StandLoFrames != null ? info.StandLoFrames.Count : 0) +
                         " workParts=" + (info.WorkFrames != null ? info.WorkFrames.Count : 0);
        }

        private bool C2Settlement3InuMdV2TryLoadVisualFramesLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind, out List<Texture2D> textures, out string audit)
        {
            textures = new List<Texture2D>();
            audit = string.Empty;
            if (md == null || !md.Found || string.IsNullOrEmpty(md.Package)) { audit = "no_md_or_package"; return false; }

            bool compositeBuilding = kind == C2Settlement3InuMdV2Kind.SettlementBuilding || kind == C2Settlement3InuMdV2Kind.Building || kind == C2Settlement3InuMdV2Kind.ResourceBuilding || kind == C2Settlement3InuMdV2Kind.SpriteObject;
            if (compositeBuilding && md.StandLoFrames != null && md.StandLoFrames.Count > 0)
            {
                var partsAudit = new List<string>();
                for (int i = 0; i < md.StandLoFrames.Count; i++)
                {
                    Texture2D partTex;
                    string partAudit;
                    if (C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, md.StandLoFrames[i].SpriteId, kind, out partTex, out partAudit) && partTex != null)
                    {
                        textures.Add(partTex);
                        if (partsAudit.Count < 12) partsAudit.Add("OK#" + md.StandLoFrames[i].SpriteId + ":" + partAudit);
                    }
                    else
                    {
                        if (partsAudit.Count < 12) partsAudit.Add("MISS#" + md.StandLoFrames[i].SpriteId + ":" + partAudit);
                    }
                }
                int workLoaded = 0;
                if (Settlement3InuMdV2DrawWorkStaticPreview && md.WorkFrames != null && md.WorkFrames.Count > 0)
                {
                    // Original changes these frames over time (#WORK/@WORK). Do not bake frame 0 permanently unless explicitly enabled.
                    Texture2D workTex;
                    string workAudit;
                    int wf = md.WorkFrames[0].SpriteId;
                    if (C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, wf, kind, out workTex, out workAudit) && workTex != null)
                    {
                        textures.Add(workTex);
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

                audit = "standlo_parts=" + textures.Count.ToString(CultureInfo.InvariantCulture) + "/" + md.StandLoFrames.Count.ToString(CultureInfo.InvariantCulture) + " workFirst=" + workLoaded.ToString(CultureInfo.InvariantCulture) + "/" + (md.WorkFrames != null ? md.WorkFrames.Count : 0).ToString(CultureInfo.InvariantCulture) + " " + string.Join(" || ", partsAudit.ToArray());
                return textures.Count > 0;
            }

            Texture2D tex;
            string oneAudit;
            int frame = C2Settlement3InuMdV2SpriteFrameLikeOriginal(md, r, kind);
            bool ok = C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frame, kind, out tex, out oneAudit);
            audit = oneAudit;
            if (ok && tex != null) textures.Add(tex);
            return textures.Count > 0;
        }

        private bool C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(C2Settlement3InuMdV2Info md, int frame, C2Settlement3InuMdV2Kind kind, out Texture2D tex, out string audit)
        {
            tex = null;
            audit = string.Empty;
            if (md == null || string.IsNullOrEmpty(md.Package)) { audit = "no_package"; return false; }
            string[] exts = kind == C2Settlement3InuMdV2Kind.Unit || kind == C2Settlement3InuMdV2Kind.Animal ? new[] { ".g2d", ".G2D", ".g16", ".G16" } : new[] { ".g16", ".G16", ".g2d", ".G2D" };
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
                    else tex = TryLoadG16FrameViaMelinojaV42LikeOriginal(p, frame, out source);
                    if (tex != null)
                    {
                        tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                        audit = "file=" + p + " frame=" + frame + " source=" + source;
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

        private bool C2Settlement3InuMdV2TryLoadVisualLikeOriginal(C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Kind kind, out Texture2D tex, out string audit)
        {
            tex = null;
            audit = "";
            if (md == null || !md.Found || string.IsNullOrEmpty(md.Package)) { audit = "no_md_or_package"; return false; }
            int frame = C2Settlement3InuMdV2SpriteFrameLikeOriginal(md, r, kind);
            string[] exts = kind == C2Settlement3InuMdV2Kind.Unit || kind == C2Settlement3InuMdV2Kind.Animal ? new[] { ".g2d", ".G2D", ".g16", ".G16" } : new[] { ".g16", ".G16", ".g2d", ".G2D" };
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
                    else tex = TryLoadG16FrameViaMelinojaV42LikeOriginal(p, frame, out source);
                    if (tex != null)
                    {
                        tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                        audit = "file=" + p + " frame=" + frame + " source=" + source;
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

        private void C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal(Transform root, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Kind kind, List<Texture2D> textures, string audit)
        {
            if (textures == null || textures.Count == 0) return;

            Vector3 basePos = C2Settlement3InuMdV2WorldLikeOriginal(r);
            Texture2D first = textures[0];
            int w = first != null ? first.width : 64;
            int h = first != null ? first.height : 64;
            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;

            bool hasLocation = md != null && md.PicLx > 0 && md.PicLy > 0;
            float lx, rx, by, ty;
            if (hasLocation)
            {
                lx = md.PicDx * s;
                rx = (md.PicDx + md.PicLx) * s;
                ty = -md.PicDy * s;
                by = -(md.PicDy + md.PicLy) * s;
            }
            else
            {
                float pivotX = md != null && md.Dx != 0 ? md.Dx : w * 0.5f;
                lx = -pivotX * s;
                rx = (w - pivotX) * s;
                by = 0f;
                ty = h * s;
            }

            const bool flipVForG16Building = true;
            float visibleBottom = float.PositiveInfinity;
            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] == null) continue;
                float vb = C2Settlement3InuMdV2VisibleBottomLocalYLikeOriginal(textures[i], by, ty, flipVForG16Building);
                if (vb < visibleBottom) visibleBottom = vb;
            }
            if (float.IsInfinity(visibleBottom)) visibleBottom = by;
            basePos.y -= visibleBottom;

            var parent = new GameObject("C2_3INU_MD_COMPOSITE_" + kind + "_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(r.MonsterId) + "_" + r.Index.ToString(CultureInfo.InvariantCulture));
            parent.transform.SetParent(root, true);
            parent.transform.position = basePos;

            for (int i = 0; i < textures.Count; i++)
            {
                Texture2D tex = textures[i];
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
                mr.sortingOrder = 2000 + r.Index * 16 + i;

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
            }
        }

        private void C2Settlement3InuMdV2CreateSpriteObjectLikeOriginal(Transform root, C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md, C2Settlement3InuMdV2Kind kind, Texture2D tex, string audit)
        {
            Vector3 basePos = C2Settlement3InuMdV2WorldLikeOriginal(r);
            int w = tex != null ? tex.width : 64;
            int h = tex != null ? tex.height : 64;
            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;

            // Original MD LOCATION is a screen-space top-left offset + picture size relative to object anchor.
            // V10 treated negative LOCATION.X as a positive pivot, which pushed houses far away and made huge cards.
            bool hasLocation = md != null && md.PicLx > 0 && md.PicLy > 0;
            float lx, rx, by, ty;
            if (hasLocation)
            {
                lx = md.PicDx * s;
                rx = (md.PicDx + md.PicLx) * s;
                ty = -md.PicDy * s;
                by = -(md.PicDy + md.PicLy) * s;
            }
            else
            {
                float pivotX = md != null && md.Dx != 0 ? md.Dx : w * 0.5f;
                lx = -pivotX * s;
                rx = (w - pivotX) * s;
                by = 0f;
                ty = h * s;
            }

            // G16 RGBA returned by Melinoja is top-left ordered for these GP frames.
            // Unity quad bottom must sample V=1 and top must sample V=0, otherwise buildings appear upside down.
            const bool flipVForG16Building = true;
            float visibleBottom = C2Settlement3InuMdV2VisibleBottomLocalYLikeOriginal(tex, by, ty, flipVForG16Building);
            basePos.y -= visibleBottom;

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
            mr.sortingOrder = 2000 + r.Index;

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
            mat.name = "C2_SettlementBuildings_3INU_MD_V21_BoundsFallback";
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

        private Vector3 C2Settlement3InuMdV2WorldLikeOriginal(C2Settlement3InuMdV2Record r)
        {
            int mx = r.RealX >> 4;
            int my = r.RealY >> 4;

            return WallOriginalXYToWorldV1LikeOriginal(mx, my, 0.0f);
        }

        private static Material C2Settlement3InuMdV2GetMaterialLikeOriginal(Texture2D tex, bool shadowLike)
        {
            // V21: НЕ трогаем пиксели/цвет/альфу текстуры.
            // Но оригинальные здания — alpha-test/cutout sprites with depth write.
            // V16 использовал Unlit/Transparent + ZWrite=0 + ZTest Always, поэтому дороги/объекты просвечивали сквозь стены.
            // Prefer fully unlit cutout. Diffuse/Standard changes brightness/contrast under Unity lighting.
            Shader sh = Shader.Find("Legacy Shaders/Transparent/Cutout/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Transparent Cutout");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");

            var mat = new Material(sh);
            mat.name = Settlement3InuMdV2MaterialName + "_RAW_CUTOUT_" + (tex != null ? tex.name : "null");
            mat.mainTexture = tex;

            mat.SetOverrideTag("RenderType", "TransparentCutout");
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 1.0f);

            // AlphaRef in original render states is around 0x40 for these sprite-style objects.
            // This is material alpha-test only: source RGBA pixels are not edited.
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 64.0f / 255.0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1.0f);

            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);

            // Cutout/opaque blend. No soft transparency for wall pixels.
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.Zero);

            mat.enableInstancing = true;
            mat.renderQueue = (int)RenderQueue.Overlay - 10;
            return mat;
        }

        private static Texture2D C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(Texture2D tex)
        {
            // V21: the shared Melinoja G16 loader creates runtime textures with linear=true.
            // Cossacks sprites are authored as gamma/sRGB UI-like GP/G16 pixels.
            // Do not change RGBA bytes; only recreate the texture with Unity sRGB sampling metadata
            // and the sampler closest to old D3D sprite rendering.
            if (tex == null) return null;
            try
            {
                Color32[] px = tex.GetPixels32();
                var srgb = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
                srgb.name = tex.name + "_srgb";
                srgb.SetPixels32(px);
                srgb.Apply(false, false);
                srgb.wrapMode = TextureWrapMode.Clamp;
                srgb.filterMode = FilterMode.Bilinear;
                return srgb;
            }
            catch
            {
                return tex;
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
