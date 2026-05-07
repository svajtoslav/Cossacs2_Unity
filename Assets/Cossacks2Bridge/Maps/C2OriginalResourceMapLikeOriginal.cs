// C2OriginalResourceMapLikeOriginal.cs
// V1G: original-style resource layer + OneSprite runtime identity layer.
// Builds a Sprites/SpRefs-like resource index from real TRE2/2ERT map objects + treelist/stonlist/complex LST/RSR.
// Does not change cursor or unit orders yet. First step: detect WOOD/STONE/FOOD like DetermineResource().

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        public const byte C2OriginalResourceWoodV1LikeOriginal = 0;
        public const byte C2OriginalResourceGoldV1LikeOriginal = 1;
        public const byte C2OriginalResourceStoneV1LikeOriginal = 2;
        public const byte C2OriginalResourceFoodV1LikeOriginal = 3;
        public const byte C2OriginalResourceIronV1LikeOriginal = 4;
        public const byte C2OriginalResourceCoalV1LikeOriginal = 5;
        public const byte C2OriginalResourceNoneV1LikeOriginal = 0xFE;
        public const byte C2OriginalResourceEmptyV1LikeOriginal = 0xFF;

        public bool C2OriginalResourceMapV1AutoBuildLikeOriginal = true;
        // V1C: off by default. Press F4 in Play Mode to toggle interval hover logging, F5 for one-shot.
        public bool C2OriginalResourceMapV1DebugMouseLogLikeOriginal = false;
        public float C2OriginalResourceMapV1DebugLogIntervalSecondsLikeOriginal = 0.35f;
        public bool C2OriginalResourceMapV1DebugLogMouseClicksLikeOriginal = true;

        // V1G: runtime identity layer. Rendering may stay batched, but gameplay reads OneSprite runtime objects.
        // This mirrors original engine logic: OneSprite -> ObjCharacter -> ResType.
        public bool C2OriginalResourceMapV1AutoLinkAuditLikeOriginal = true;
        public bool C2OriginalResourceMapV1CreateMarkerObjectsLikeOriginal = true; // legacy V1F marker, kept for debug compatibility
        public bool C2OriginalResourceMapV1CreateOneSpriteRuntimeLikeOriginal = true;
        public int C2OriginalResourceMapV1StoneLinkAuditLimitLikeOriginal = 50;

        private const int C2OriginalResourceMapV1CellShiftLikeOriginal = 7;
        private const int C2OriginalResourceMapV1SearchRadiusCellsLikeOriginal = 2;
        private const int C2OriginalResourceMapV1AcceptDistanceLikeOriginal = 160;
        private const string C2OriginalResourceMapV1ContractLikeOriginal = "V1G_Sprites_SpRefs_DetermineResource_OneSprite_Runtime";

        private bool _c2OriginalResourceMapV1BuiltLikeOriginal;
        private C2OriginalResourceMapStateV1LikeOriginal _c2OriginalResourceMapV1StateLikeOriginal;
        private float _c2OriginalResourceMapV1NextDebugLogTimeLikeOriginal;
        private bool _c2OriginalResourceMapV1LinkAuditDoneLikeOriginal;
        private GameObject _c2OriginalResourceMapV1MarkerRootLikeOriginal;
        private GameObject _c2OriginalResourceMapV1OneSpriteRootLikeOriginal;

        public bool C2OriginalResourceMapV1TryBuildLikeOriginal(string source = "manual")
        {
            if (_c2OriginalResourceMapV1BuiltLikeOriginal && _c2OriginalResourceMapV1StateLikeOriginal != null)
                return true;

            if (!C2OriginalResourceMapV1AutoBuildLikeOriginal && !string.Equals(source, "manual", StringComparison.OrdinalIgnoreCase))
                return false;

            if (_map == null || _bootstrap == null || _bootstrap.Fs == null || string.IsNullOrWhiteSpace(_mapRelativePath))
                return false;

            if (!_bootstrap.Fs.Exists(_mapRelativePath))
                return false;

            try
            {
                C2OriginalResourceMapStateV1LikeOriginal resState = new C2OriginalResourceMapStateV1LikeOriginal();
                resState.MapPath = _mapRelativePath ?? string.Empty;

                C2OriginalResourceCatalogV1LikeOriginal trees = C2OriginalResourceMapV1LoadCatalogLikeOriginal(
                    "GA", "TREES", new[] { "Treelist.lst", "treelist.lst", "Data1/Treelist.lst", "Data1/treelist.lst" }, new[] { "treelist.rsr", "Treelist.rsr", "Data1/treelist.rsr", "Data1/Treelist.rsr" });
                C2OriginalResourceCatalogV1LikeOriginal stones = C2OriginalResourceMapV1LoadCatalogLikeOriginal(
                    "TS", "STONES", new[] { "stonlist.LST", "stonlist.lst", "Data1/stonlist.LST", "Data1/stonlist.lst" }, new[] { "stonlist.rsr", "stonlist.RSR", "Data1/stonlist.rsr", "Data1/stonlist.RSR" });
                C2OriginalResourceCatalogV1LikeOriginal complex = C2OriginalResourceMapV1LoadCatalogLikeOriginal(
                    "OC", "COMPLEX", new[] { "complex.lst", "Data1/complex.lst" }, new[] { "complex.rsr", "Data1/complex.rsr" });

                resState.CatalogsBySign["GA"] = trees;
                resState.CatalogsBySign["TS"] = stones;
                resState.CatalogsBySign["OC"] = complex;

                WallMapStateV1LikeOriginal wallState = TryLoadWallMapStateFromCurrentMapV1LikeOriginal();
                if (wallState == null || wallState.Tre2Objects == null || wallState.Tre2Objects.Count == 0)
                {
                    Debug.LogWarning("[C2:RESOURCE MAP V1] no TRE2/2ERT objects. contract=" + C2OriginalResourceMapV1ContractLikeOriginal + " map='" + resState.MapPath + "'");
                    _c2OriginalResourceMapV1StateLikeOriginal = resState;
                    _c2OriginalResourceMapV1BuiltLikeOriginal = true;
                    return true;
                }

                for (int i = 0; i < wallState.Tre2Objects.Count; i++)
                {
                    Tre2MapObjectV28LikeOriginal o = wallState.Tre2Objects[i];
                    if (o == null) continue;
                    C2OriginalResourceMapV1RegisterTre2ObjectLikeOriginal(resState, o, i);
                }

                _c2OriginalResourceMapV1StateLikeOriginal = resState;
                _c2OriginalResourceMapV1BuiltLikeOriginal = true;

                Debug.Log("[C2:RESOURCE MAP V1] built contract=" + C2OriginalResourceMapV1ContractLikeOriginal +
                          " source=" + source +
                          " map='" + resState.MapPath + "'" +
                          " definitions TREES=" + trees.DefinitionsByIndex.Count.ToString(CultureInfo.InvariantCulture) + "/" + trees.DeclaredCount.ToString(CultureInfo.InvariantCulture) +
                          " STONES=" + stones.DefinitionsByIndex.Count.ToString(CultureInfo.InvariantCulture) + "/" + stones.DeclaredCount.ToString(CultureInfo.InvariantCulture) +
                          " COMPLEX=" + complex.DefinitionsByIndex.Count.ToString(CultureInfo.InvariantCulture) + "/" + complex.DeclaredCount.ToString(CultureInfo.InvariantCulture) +
                          " sources WOOD=" + resState.WoodCount.ToString(CultureInfo.InvariantCulture) +
                          " STONE=" + resState.StoneCount.ToString(CultureInfo.InvariantCulture) +
                          " FOOD=" + resState.FoodCount.ToString(CultureInfo.InvariantCulture) +
                          " otherRes=" + resState.OtherResourceCount.ToString(CultureInfo.InvariantCulture) +
                          " entries=" + resState.Entries.Count.ToString(CultureInfo.InvariantCulture) +
                          " buckets=" + resState.Buckets.Count.ToString(CultureInfo.InvariantCulture) +
                          " tre2Total=" + wallState.Tre2Objects.Count.ToString(CultureInfo.InvariantCulture) +
                          " skippedNoCatalog=" + resState.SkippedNoCatalog.ToString(CultureInfo.InvariantCulture) +
                          " skippedNoDefinition=" + resState.SkippedNoDefinition.ToString(CultureInfo.InvariantCulture) +
                          " skippedNoResource=" + resState.SkippedNoResource.ToString(CultureInfo.InvariantCulture));

                if (C2OriginalResourceMapV1CreateOneSpriteRuntimeLikeOriginal)
                {
                    int oneSpriteCreated;
                    int oneSpriteReused;
                    C2OriginalResourceMapV1EnsureOneSpriteRuntimeObjectsLikeOriginal(resState, out oneSpriteCreated, out oneSpriteReused);
                    C2OriginalResourceMapV1LogOneSpriteRuntimeSummaryLikeOriginal(resState, "build_" + source, oneSpriteCreated, oneSpriteReused, 0, 0);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:RESOURCE MAP V1] failed:\n" + ex);
                return false;
            }
        }

        public bool C2OriginalResourceMapV1TryDetermineOneSpriteRuntimeLikeOriginal(
            int originalX,
            int originalY,
            out C2OriginalOneSpriteRuntimeLikeOriginal oneSprite,
            out byte resourceId,
            out int bestDist)
        {
            oneSprite = null;
            resourceId = C2OriginalResourceEmptyV1LikeOriginal;
            bestDist = 10000;

            if (!C2OriginalResourceMapV1TryBuildLikeOriginal("determine-runtime"))
                return false;

            C2OriginalResourceMapStateV1LikeOriginal state = _c2OriginalResourceMapV1StateLikeOriginal;
            if (state == null)
                return false;

            if (C2OriginalResourceMapV1CreateOneSpriteRuntimeLikeOriginal)
            {
                int created;
                int reused;
                C2OriginalResourceMapV1EnsureOneSpriteRuntimeObjectsLikeOriginal(state, out created, out reused);
            }

            C2OriginalResourceEntryV1LikeOriginal entry;
            resourceId = C2OriginalResourceMapV1DetermineResourceLikeOriginal(originalX, originalY, out entry, out bestDist);
            if (entry == null || entry.Definition == null || resourceId >= C2OriginalResourceNoneV1LikeOriginal)
                return false;

            oneSprite = entry.OneSpriteRuntime;
            return oneSprite != null;
        }

        public bool C2OriginalResourceMapV1IsReadyLikeOriginal()
        {
            return _c2OriginalResourceMapV1BuiltLikeOriginal && _c2OriginalResourceMapV1StateLikeOriginal != null;
        }

        public byte C2OriginalResourceMapV1DetermineResourceLikeOriginal(int originalX, int originalY)
        {
            C2OriginalResourceEntryV1LikeOriginal best;
            int bestDist;
            return C2OriginalResourceMapV1DetermineResourceLikeOriginal(originalX, originalY, out best, out bestDist);
        }

        public bool C2OriginalResourceMapV1TryDetermineResourceLikeOriginal(int originalX, int originalY, out byte resourceId, out string audit)
        {
            C2OriginalResourceEntryV1LikeOriginal best;
            int bestDist;
            resourceId = C2OriginalResourceMapV1DetermineResourceLikeOriginal(originalX, originalY, out best, out bestDist);
            if (resourceId == C2OriginalResourceEmptyV1LikeOriginal)
            {
                audit = "rk=EMPTY dist=" + bestDist.ToString(CultureInfo.InvariantCulture) + " x=" + originalX.ToString(CultureInfo.InvariantCulture) + " y=" + originalY.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            audit = "rk=" + C2OriginalResourceMapV1ResourceNameLikeOriginal(resourceId) +
                    " entry=" + (best != null && best.Definition != null ? best.Definition.ObjectId : "<null>") +
                    " sign=" + (best != null ? best.Sign : "?") +
                    " sgIndex=" + (best != null ? best.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "?") +
                    " dist=" + bestDist.ToString(CultureInfo.InvariantCulture) +
                    " mouse=(" + originalX.ToString(CultureInfo.InvariantCulture) + "," + originalY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " obj=(" + (best != null ? best.OriginalX.ToString(CultureInfo.InvariantCulture) : "?") + "," + (best != null ? best.OriginalY.ToString(CultureInfo.InvariantCulture) : "?") + ")";
            return true;
        }

        private byte C2OriginalResourceMapV1DetermineResourceLikeOriginal(int originalX, int originalY, out C2OriginalResourceEntryV1LikeOriginal best, out int bestDist)
        {
            best = null;
            bestDist = 10000;

            C2OriginalResourceMapStateV1LikeOriginal state = _c2OriginalResourceMapV1StateLikeOriginal;
            if (state == null || state.Buckets == null || state.Buckets.Count == 0)
                return C2OriginalResourceEmptyV1LikeOriginal;

            int cellX = originalX >> C2OriginalResourceMapV1CellShiftLikeOriginal;
            int cellY = originalY >> C2OriginalResourceMapV1CellShiftLikeOriginal;
            byte bestRes = C2OriginalResourceEmptyV1LikeOriginal;

            for (int dx = -C2OriginalResourceMapV1SearchRadiusCellsLikeOriginal; dx <= C2OriginalResourceMapV1SearchRadiusCellsLikeOriginal; dx++)
            {
                for (int dy = -C2OriginalResourceMapV1SearchRadiusCellsLikeOriginal; dy <= C2OriginalResourceMapV1SearchRadiusCellsLikeOriginal; dy++)
                {
                    long key = C2OriginalResourceMapV1BucketKeyLikeOriginal(cellX + dx, cellY + dy);
                    List<C2OriginalResourceEntryV1LikeOriginal> list;
                    if (!state.Buckets.TryGetValue(key, out list) || list == null || list.Count == 0)
                        continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        C2OriginalResourceEntryV1LikeOriginal r = list[i];
                        if (r == null || r.Definition == null || r.Definition.ResType >= C2OriginalResourceNoneV1LikeOriginal)
                            continue;

                        int dist = C2OriginalResourceMapV1NormaLikeOriginal(originalX - r.OriginalX, originalY - r.OriginalY);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestRes = r.Definition.ResType;
                            best = r;
                        }
                    }
                }
            }

            if (bestDist < C2OriginalResourceMapV1AcceptDistanceLikeOriginal)
                return bestRes;

            best = null;
            return C2OriginalResourceEmptyV1LikeOriginal;
        }

        private void C2OriginalResourceMapV1RegisterTre2ObjectLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, Tre2MapObjectV28LikeOriginal o, int order)
        {
            if (state == null || o == null)
                return;

            string sign = string.IsNullOrWhiteSpace(o.Sign) ? "?" : o.Sign.Trim().ToUpperInvariant();
            C2OriginalResourceCatalogV1LikeOriginal catalog;
            if (!state.CatalogsBySign.TryGetValue(sign, out catalog) || catalog == null)
            {
                state.SkippedNoCatalog++;
                return;
            }

            C2OriginalResourceDefinitionV1LikeOriginal def;
            if (!catalog.DefinitionsByIndex.TryGetValue(o.SpriteIndex, out def) || def == null)
            {
                state.SkippedNoDefinition++;
                return;
            }

            if (def.ResType >= C2OriginalResourceNoneV1LikeOriginal)
            {
                state.SkippedNoResource++;
                return;
            }

            C2OriginalResourceEntryV1LikeOriginal entry = new C2OriginalResourceEntryV1LikeOriginal
            {
                Order = order,
                Section = o.Section ?? string.Empty,
                Sign = sign,
                OriginalX = o.X,
                OriginalY = o.Y,
                OriginalZ = 0,
                SpriteIndex = o.SpriteIndex,
                NIndex = o.NIndex,
                Locking = o.Locking,
                HasMatrix = o.HasMatrix,
                Matrix = o.Matrix,
                Definition = def
            };

            entry.ExactKey = C2OriginalResourceMapV1ExactKeyLikeOriginal(entry.Sign, entry.SpriteIndex, entry.OriginalX, entry.OriginalY);
            entry.SignSpriteKey = C2OriginalResourceMapV1SignSpriteKeyLikeOriginal(entry.Sign, entry.SpriteIndex);

            state.Entries.Add(entry);
            state.EntriesByExactKey[entry.ExactKey] = entry;
            List<C2OriginalResourceEntryV1LikeOriginal> bySignSprite;
            if (!state.EntriesBySignSprite.TryGetValue(entry.SignSpriteKey, out bySignSprite) || bySignSprite == null)
            {
                bySignSprite = new List<C2OriginalResourceEntryV1LikeOriginal>();
                state.EntriesBySignSprite[entry.SignSpriteKey] = bySignSprite;
            }
            bySignSprite.Add(entry);
            C2OriginalResourceMapV1AddToBucketLikeOriginal(state, entry);

            if (def.ResType == C2OriginalResourceWoodV1LikeOriginal) state.WoodCount++;
            else if (def.ResType == C2OriginalResourceStoneV1LikeOriginal) state.StoneCount++;
            else if (def.ResType == C2OriginalResourceFoodV1LikeOriginal) state.FoodCount++;
            else state.OtherResourceCount++;
        }

        private static void C2OriginalResourceMapV1AddToBucketLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, C2OriginalResourceEntryV1LikeOriginal entry)
        {
            if (state == null || entry == null)
                return;
            int cx = entry.OriginalX >> C2OriginalResourceMapV1CellShiftLikeOriginal;
            int cy = entry.OriginalY >> C2OriginalResourceMapV1CellShiftLikeOriginal;
            long key = C2OriginalResourceMapV1BucketKeyLikeOriginal(cx, cy);
            List<C2OriginalResourceEntryV1LikeOriginal> list;
            if (!state.Buckets.TryGetValue(key, out list) || list == null)
            {
                list = new List<C2OriginalResourceEntryV1LikeOriginal>();
                state.Buckets[key] = list;
            }
            list.Add(entry);
        }

        private C2OriginalResourceCatalogV1LikeOriginal C2OriginalResourceMapV1LoadCatalogLikeOriginal(string sign, string label, string[] listCandidates, string[] rsrCandidates)
        {
            C2OriginalResourceCatalogV1LikeOriginal catalog = new C2OriginalResourceCatalogV1LikeOriginal
            {
                Sign = sign ?? string.Empty,
                Label = label ?? string.Empty
            };

            string path;
            string text;
            if (C2OriginalResourceMapV1TryReadGameTextLikeOriginal(listCandidates, out path, out text))
            {
                catalog.SourceListPath = path;
                C2OriginalResourceMapV1ParseLstLikeOriginal(catalog, text);
            }
            else
            {
                Debug.LogWarning("[C2:RESOURCE MAP V1] missing LST label=" + label + " candidates=" + string.Join(",", listCandidates ?? new string[0]));
            }

            if (C2OriginalResourceMapV1TryReadGameTextLikeOriginal(rsrCandidates, out path, out text))
            {
                catalog.SourceRsrPath = path;
                C2OriginalResourceMapV1ParseRsrLikeOriginal(catalog, text);
            }
            else
            {
                Debug.LogWarning("[C2:RESOURCE MAP V1] missing RSR label=" + label + " candidates=" + string.Join(",", rsrCandidates ?? new string[0]));
            }

            return catalog;
        }

        private bool C2OriginalResourceMapV1TryReadGameTextLikeOriginal(string[] candidates, out string path, out string text)
        {
            path = string.Empty;
            text = string.Empty;

            if (TryReadGameTextV1LikeOriginal(candidates, out path, out text))
                return true;

            return C2OriginalResourceMapV1TryReadDiskTextLikeOriginal(candidates, out path, out text);
        }

        private static bool C2OriginalResourceMapV1TryReadDiskTextLikeOriginal(string[] candidates, out string path, out string text)
        {
            path = string.Empty;
            text = string.Empty;
            if (candidates == null || candidates.Length == 0)
                return false;

            List<string> roots = new List<string>();
            try
            {
                string data = Application.dataPath;
                if (!string.IsNullOrEmpty(data))
                {
                    roots.Add(Path.Combine(data, "Resources"));
                    roots.Add(Path.Combine(data, "Resources", "Data1"));
                    roots.Add(Path.Combine(data, "Resources", "Nature"));
                    roots.Add(Path.Combine(data, "Resources", "Cash"));
                    roots.Add(data);
                }
            }
            catch { }

            for (int c = 0; c < candidates.Length; c++)
            {
                string cand = candidates[c];
                if (string.IsNullOrWhiteSpace(cand))
                    continue;

                string normalized = cand.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                for (int r = 0; r < roots.Count; r++)
                {
                    string root = roots[r];
                    if (string.IsNullOrEmpty(root))
                        continue;

                    string direct = Path.Combine(root, normalized);
                    if (File.Exists(direct))
                    {
                        path = direct;
                        text = C2OriginalResourceMapV1ReadTextCp1251LikeOriginal(direct);
                        return true;
                    }

                    string fileName = Path.GetFileName(normalized);
                    if (string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(root))
                        continue;

                    try
                    {
                        string[] found = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
                        if (found != null && found.Length > 0)
                        {
                            path = found[0];
                            text = C2OriginalResourceMapV1ReadTextCp1251LikeOriginal(path);
                            return true;
                        }
                    }
                    catch { }
                }
            }

            return false;
        }

        private static string C2OriginalResourceMapV1ReadTextCp1251LikeOriginal(string file)
        {
            byte[] bytes = File.ReadAllBytes(file);
            Encoding enc;
            try { enc = Encoding.GetEncoding(1251); }
            catch { enc = Encoding.UTF8; }
            return enc.GetString(bytes);
        }

        private static void C2OriginalResourceMapV1ParseLstLikeOriginal(C2OriginalResourceCatalogV1LikeOriginal catalog, string text)
        {
            if (catalog == null || string.IsNullOrEmpty(text))
                return;

            string[] lines = text.Replace("\r", "\n").Split('\n');
            bool headerRead = false;
            int index = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = C2OriginalResourceMapV1StripCommentLikeOriginal(lines[i]);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] t = C2OriginalResourceMapV1SplitTokensLikeOriginal(line);
                if (t.Length == 0)
                    continue;

                if (!headerRead)
                {
                    catalog.GpName = t[0];
                    if (t.Length > 1) C2OriginalResourceMapV1TryParseIntLikeOriginal(t[1], out catalog.DeclaredCount);
                    headerRead = true;
                    continue;
                }

                if (t[0].StartsWith("[", StringComparison.Ordinal))
                    continue;
                if (t.Length < 4)
                    continue;

                int centerX, centerY, radius;
                if (!C2OriginalResourceMapV1TryParseIntLikeOriginal(t[1], out centerX)) continue;
                if (!C2OriginalResourceMapV1TryParseIntLikeOriginal(t[2], out centerY)) continue;
                if (!C2OriginalResourceMapV1TryParseIntLikeOriginal(t[3], out radius)) continue;

                C2OriginalResourceDefinitionV1LikeOriginal def = new C2OriginalResourceDefinitionV1LikeOriginal
                {
                    GroupSign = catalog.Sign,
                    GroupLabel = catalog.Label,
                    Index = index,
                    SpriteIndex = index,
                    ObjectId = t[0],
                    CenterX = centerX,
                    CenterY = centerY,
                    Radius = radius,
                    ResType = C2OriginalResourceEmptyV1LikeOriginal,
                    WorkRadius = 32,
                    ResPerWork = 0,
                    WorkNextIndex = -1,
                    TimeNextIndex = -1,
                    WorkAmount = 0,
                    TimeAmount = 0
                };

                if (t.Length >= 5 && string.Equals(t[4], "#FIELDPATH", StringComparison.OrdinalIgnoreCase))
                {
                    def.IsFieldPath = true;
                    if (t.Length > 5) C2OriginalResourceMapV1TryParseIntLikeOriginal(t[5], out def.FieldWidth);
                    if (t.Length > 6) C2OriginalResourceMapV1TryParseIntLikeOriginal(t[6], out def.FieldHeight);
                    if (t.Length > 7) C2OriginalResourceMapV1TryParseIntLikeOriginal(t[7], out def.FieldGrowStage);
                    if (t.Length > 8) C2OriginalResourceMapV1TryParseIntLikeOriginal(t[8], out def.FieldYScale);
                }

                catalog.DefinitionsByIndex[index] = def;
                catalog.DefinitionsByName[def.ObjectId] = def;
                index++;
            }
        }

        private static void C2OriginalResourceMapV1ParseRsrLikeOriginal(C2OriginalResourceCatalogV1LikeOriginal catalog, string text)
        {
            if (catalog == null || string.IsNullOrEmpty(text))
                return;

            string[] lines = text.Replace("\r", "\n").Split('\n');
            int mode = 0; // 1 work, 2 time, 3 sources
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i] ?? string.Empty;
                string line = C2OriginalResourceMapV1StripCommentLikeOriginal(raw);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    if (line.IndexOf("SOURCES", StringComparison.OrdinalIgnoreCase) >= 0) mode = 3;
                    else if (line.IndexOf("WORKTRANSFORM", StringComparison.OrdinalIgnoreCase) >= 0) mode = 1;
                    else if (line.IndexOf("TIMETRANSFORM", StringComparison.OrdinalIgnoreCase) >= 0) mode = 2;
                    else mode = 0;
                    continue;
                }

                string[] t = C2OriginalResourceMapV1SplitTokensLikeOriginal(line);
                if (t.Length == 0)
                    continue;

                if (mode == 3 && t.Length >= 4)
                {
                    C2OriginalResourceDefinitionV1LikeOriginal def;
                    if (!catalog.DefinitionsByName.TryGetValue(t[0], out def) || def == null)
                        continue;

                    def.ResType = C2OriginalResourceMapV1ResourceIdLikeOriginal(t[1]);
                    C2OriginalResourceMapV1TryParseIntLikeOriginal(t[2], out def.ResPerWork);
                    C2OriginalResourceMapV1TryParseIntLikeOriginal(t[3], out def.WorkRadius);
                    continue;
                }

                if ((mode == 1 || mode == 2) && t.Length >= 3)
                {
                    C2OriginalResourceDefinitionV1LikeOriginal from;
                    C2OriginalResourceDefinitionV1LikeOriginal to;
                    if (!catalog.DefinitionsByName.TryGetValue(t[0], out from) || from == null)
                        continue;
                    if (!catalog.DefinitionsByName.TryGetValue(t[1], out to) || to == null)
                        continue;
                    int amount = 0;
                    C2OriginalResourceMapV1TryParseIntLikeOriginal(t[2], out amount);
                    if (mode == 1)
                    {
                        from.WorkNextIndex = to.Index;
                        from.WorkAmount = amount;
                    }
                    else
                    {
                        from.TimeNextIndex = to.Index;
                        from.TimeAmount = amount;
                    }
                }
            }
        }

        private static string C2OriginalResourceMapV1StripCommentLikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            string s = line.Trim();
            if (s.StartsWith("//", StringComparison.Ordinal) || s.StartsWith("/", StringComparison.Ordinal))
                return string.Empty;
            int comment = s.IndexOf("//", StringComparison.Ordinal);
            if (comment >= 0) s = s.Substring(0, comment).Trim();
            return s;
        }

        private static string[] C2OriginalResourceMapV1SplitTokensLikeOriginal(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return new string[0];
            return line.Split(new[] { ' ', '\t', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool C2OriginalResourceMapV1TryParseIntLikeOriginal(string s, out int v)
        {
            return int.TryParse((s ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
        }

        private static byte C2OriginalResourceMapV1ResourceIdLikeOriginal(string name)
        {
            string n = (name ?? string.Empty).Trim().ToUpperInvariant();
            if (n == "WOOD") return C2OriginalResourceWoodV1LikeOriginal;
            if (n == "GOLD") return C2OriginalResourceGoldV1LikeOriginal;
            if (n == "STONE") return C2OriginalResourceStoneV1LikeOriginal;
            if (n == "FOOD") return C2OriginalResourceFoodV1LikeOriginal;
            if (n == "IRON") return C2OriginalResourceIronV1LikeOriginal;
            if (n == "COAL") return C2OriginalResourceCoalV1LikeOriginal;
            if (n == "NONE") return C2OriginalResourceNoneV1LikeOriginal;
            if (n == "REMOVE") return C2OriginalResourceEmptyV1LikeOriginal;
            return C2OriginalResourceEmptyV1LikeOriginal;
        }

        private static string C2OriginalResourceMapV1ResourceNameLikeOriginal(byte id)
        {
            if (id == C2OriginalResourceWoodV1LikeOriginal) return "WOOD";
            if (id == C2OriginalResourceGoldV1LikeOriginal) return "GOLD";
            if (id == C2OriginalResourceStoneV1LikeOriginal) return "STONE";
            if (id == C2OriginalResourceFoodV1LikeOriginal) return "FOOD";
            if (id == C2OriginalResourceIronV1LikeOriginal) return "IRON";
            if (id == C2OriginalResourceCoalV1LikeOriginal) return "COAL";
            if (id == C2OriginalResourceNoneV1LikeOriginal) return "NONE";
            if (id == C2OriginalResourceEmptyV1LikeOriginal) return "EMPTY";
            return "RES_" + id.ToString(CultureInfo.InvariantCulture);
        }

        private static int C2OriginalResourceMapV1NormaLikeOriginal(int dx, int dy)
        {
            dx = Math.Abs(dx);
            dy = Math.Abs(dy);
            return dx > dy ? dx + (dy >> 1) : dy + (dx >> 1);
        }

        private static long C2OriginalResourceMapV1BucketKeyLikeOriginal(int cx, int cy)
        {
            return (((long)cy) << 32) ^ (uint)cx;
        }

        private static string C2OriginalResourceMapV1ExactKeyLikeOriginal(string sign, int sgIndex, int originalX, int originalY)
        {
            return (sign ?? string.Empty).Trim().ToUpperInvariant() + ":" +
                   sgIndex.ToString(CultureInfo.InvariantCulture) + ":" +
                   originalX.ToString(CultureInfo.InvariantCulture) + ":" +
                   originalY.ToString(CultureInfo.InvariantCulture);
        }

        private static string C2OriginalResourceMapV1SignSpriteKeyLikeOriginal(string sign, int sgIndex)
        {
            return (sign ?? string.Empty).Trim().ToUpperInvariant() + ":" + sgIndex.ToString(CultureInfo.InvariantCulture);
        }


        public bool C2OriginalResourceMapV1TryScreenToOriginalLikeOriginal(Vector3 screenPosition, out int originalX, out int originalY, out Vector3 worldPoint, out string audit)
        {
            originalX = 0;
            originalY = 0;
            worldPoint = Vector3.zero;
            audit = string.Empty;

            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cams = Camera.allCameras;
                if (cams != null && cams.Length > 0) cam = cams[0];
            }

            if (cam == null)
            {
                audit = "no_camera";
                return false;
            }

            Ray ray = cam.ScreenPointToRay(screenPosition);
            bool gotPoint = false;
            RaycastHit best = new RaycastHit();
            float bestDistance = float.PositiveInfinity;

            try
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, 50000.0f, ~0, QueryTriggerInteraction.Ignore);
                if (hits != null && hits.Length > 0)
                {
                    // Prefer terrain/ground hit. If the ray first hits a tree/card, x/z are often close enough,
                    // but original DetermineResource uses map mouse xy, so ground is the cleaner sample point.
                    for (int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit h = hits[i];
                        if (h.collider == null) continue;
                        string n = h.collider.gameObject != null ? h.collider.gameObject.name : string.Empty;
                        bool terrainish = n.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           n.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           n.IndexOf("ShadowOverlay", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (terrainish && h.distance < bestDistance)
                        {
                            best = h;
                            bestDistance = h.distance;
                            gotPoint = true;
                        }
                    }

                    if (!gotPoint)
                    {
                        for (int i = 0; i < hits.Length; i++)
                        {
                            RaycastHit h = hits[i];
                            if (h.collider == null) continue;
                            if (h.distance < bestDistance)
                            {
                                best = h;
                                bestDistance = h.distance;
                                gotPoint = true;
                            }
                        }
                    }
                }
            }
            catch { }

            if (gotPoint)
            {
                worldPoint = best.point;
            }
            else
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                float enter;
                if (!plane.Raycast(ray, out enter) || enter < 0.0f)
                {
                    audit = "no_world_hit";
                    return false;
                }
                worldPoint = ray.GetPoint(enter);
            }

            float ox;
            float oy;
            if (!C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(worldPoint, out ox, out oy))
            {
                audit = "world_to_original_failed world=" + C2OriginalResourceMapV1Vec3LikeOriginal(worldPoint);
                return false;
            }

            originalX = Mathf.RoundToInt(ox);
            originalY = Mathf.RoundToInt(oy);
            audit = "screen=(" + screenPosition.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + screenPosition.y.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                    " world=" + C2OriginalResourceMapV1Vec3LikeOriginal(worldPoint) +
                    " original=(" + originalX.ToString(CultureInfo.InvariantCulture) + "," + originalY.ToString(CultureInfo.InvariantCulture) + ")";
            return true;
        }

        public void C2OriginalResourceMapV1DebugMouseScreenLikeOriginal(Vector3 screenPosition, string reason)
        {
            if (!C2OriginalResourceMapV1TryBuildLikeOriginal("hover-debug"))
                return;

            int ox;
            int oy;
            Vector3 world;
            string posAudit;
            if (!C2OriginalResourceMapV1TryScreenToOriginalLikeOriginal(screenPosition, out ox, out oy, out world, out posAudit))
            {
                Debug.Log("[C2:RESOURCE HOVER V1G] reason=" + reason + " miss " + posAudit);
                return;
            }

            byte rk;
            string resAudit;
            bool found = C2OriginalResourceMapV1TryDetermineResourceLikeOriginal(ox, oy, out rk, out resAudit);
            Debug.Log("[C2:RESOURCE HOVER V1G] reason=" + reason +
                      " found=" + (found ? "1" : "0") +
                      " " + posAudit +
                      " " + resAudit);
        }


        public void C2OriginalResourceMapV1DebugSelectionGuiRectLikeOriginal(Vector2 guiStart, Vector2 guiEnd, int selectionId)
        {
            if (!C2OriginalResourceMapV1TryBuildLikeOriginal("rect-select-debug"))
                return;

            C2OriginalResourceMapStateV1LikeOriginal state = _c2OriginalResourceMapV1StateLikeOriginal;
            if (state == null || state.Entries == null)
                return;

            float x0 = Mathf.Min(guiStart.x, guiEnd.x);
            float y0 = Mathf.Min(guiStart.y, guiEnd.y);
            float x1 = Mathf.Max(guiStart.x, guiEnd.x);
            float y1 = Mathf.Max(guiStart.y, guiEnd.y);

            float w = x1 - x0;
            float h = y1 - y0;

            if (w < 6.0f && h < 6.0f)
            {
                Debug.Log("[C2:RESOURCE SELECT V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                          " ignored small_rect gui=(" +
                          x0.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                          y0.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                          w.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                          h.ToString("0.0", CultureInfo.InvariantCulture) + ")");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cams = Camera.allCameras;
                if (cams != null && cams.Length > 0) cam = cams[0];
            }

            if (cam == null)
            {
                Debug.Log("[C2:RESOURCE SELECT V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                          " failed no_camera");
                return;
            }

            Rect rect = Rect.MinMaxRect(x0, y0, x1, y1);
            List<C2OriginalResourceSelectionHitV1LikeOriginal> hits = new List<C2OriginalResourceSelectionHitV1LikeOriginal>();

            int wood = 0;
            int stone = 0;
            int food = 0;
            int other = 0;

            for (int i = 0; i < state.Entries.Count; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal entry = state.Entries[i];
                if (entry == null || entry.Definition == null)
                    continue;

                Vector3 world = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(entry.OriginalX, entry.OriginalY);
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0.0f)
                    continue;

                Vector2 gui = new Vector2(screen.x, Screen.height - screen.y);
                if (!rect.Contains(gui))
                    continue;

                byte res = entry.Definition.ResType;
                if (res == C2OriginalResourceWoodV1LikeOriginal) wood++;
                else if (res == C2OriginalResourceStoneV1LikeOriginal) stone++;
                else if (res == C2OriginalResourceFoodV1LikeOriginal) food++;
                else other++;

                C2OriginalResourceSelectionHitV1LikeOriginal hit = new C2OriginalResourceSelectionHitV1LikeOriginal();
                hit.Entry = entry;
                hit.ResId = res;
                hit.ScreenX = screen.x;
                hit.ScreenY = screen.y;
                hits.Add(hit);
            }

            hits.Sort(delegate (C2OriginalResourceSelectionHitV1LikeOriginal a, C2OriginalResourceSelectionHitV1LikeOriginal b)
            {
                int c = a.ResId.CompareTo(b.ResId);
                if (c != 0) return c;
                string ao = a.Entry != null && a.Entry.Definition != null ? a.Entry.Definition.ObjectId : string.Empty;
                string bo = b.Entry != null && b.Entry.Definition != null ? b.Entry.Definition.ObjectId : string.Empty;
                c = string.Compare(ao, bo, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                int ax = a.Entry != null ? a.Entry.OriginalX : 0;
                int bx = b.Entry != null ? b.Entry.OriginalX : 0;
                c = ax.CompareTo(bx);
                if (c != 0) return c;
                int ay = a.Entry != null ? a.Entry.OriginalY : 0;
                int by = b.Entry != null ? b.Entry.OriginalY : 0;
                return ay.CompareTo(by);
            });

            Debug.Log("[C2:RESOURCE SELECT V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                      " rectGui=(" +
                      x0.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      y0.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      w.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      h.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " total=" + hits.Count.ToString(CultureInfo.InvariantCulture) +
                      " WOOD=" + wood.ToString(CultureInfo.InvariantCulture) +
                      " STONE=" + stone.ToString(CultureInfo.InvariantCulture) +
                      " FOOD=" + food.ToString(CultureInfo.InvariantCulture) +
                      " OTHER=" + other.ToString(CultureInfo.InvariantCulture) +
                      " mode=project_resource_entries_to_screen");

            C2OriginalResourceMapV1DebugStoneNearestToSelectionLikeOriginal(state, cam, rect, selectionId, x0, y0, w, h);

            if (hits.Count == 0)
                return;

            StringBuilder sb = new StringBuilder(4096);
            int part = 1;
            int totalParts = 1;

            // Total parts are estimated by chunking at runtime. The important stable key is sel + part.
            for (int i = 0; i < hits.Count; i++)
            {
                C2OriginalResourceSelectionHitV1LikeOriginal hit = hits[i];
                C2OriginalResourceEntryV1LikeOriginal e = hit.Entry;
                C2OriginalResourceDefinitionV1LikeOriginal d = e != null ? e.Definition : null;

                string token =
                    i.ToString(CultureInfo.InvariantCulture) + ":" +
                    C2OriginalResourceMapV1ResourceNameLikeOriginal(hit.ResId) + ":" +
                    (d != null ? d.ObjectId : "<null>") +
                    " sign=" + (e != null ? e.Sign : "?") +
                    " sg=" + (e != null ? e.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "?") +
                    " map=(" + (e != null ? e.OriginalX.ToString(CultureInfo.InvariantCulture) : "?") +
                    "," + (e != null ? e.OriginalY.ToString(CultureInfo.InvariantCulture) : "?") + ")" +
                    " scr=(" + hit.ScreenX.ToString("0", CultureInfo.InvariantCulture) +
                    "," + hit.ScreenY.ToString("0", CultureInfo.InvariantCulture) + "); ";

                if (sb.Length + token.Length > 3200)
                {
                    Debug.Log("[C2:RESOURCE SELECT V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                              " part=" + part.ToString(CultureInfo.InvariantCulture) +
                              " entries=" + sb.ToString());
                    sb.Length = 0;
                    part++;
                    totalParts++;
                }

                sb.Append(token);
            }

            if (sb.Length > 0)
            {
                Debug.Log("[C2:RESOURCE SELECT V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                          " part=" + part.ToString(CultureInfo.InvariantCulture) +
                          " entries=" + sb.ToString());
            }
        }

        private void C2OriginalResourceMapV1DebugStoneNearestToSelectionLikeOriginal(
            C2OriginalResourceMapStateV1LikeOriginal state,
            Camera cam,
            Rect rect,
            int selectionId,
            float x0,
            float y0,
            float w,
            float h)
        {
            if (state == null || state.Entries == null || cam == null)
                return;

            float cx = rect.xMin + rect.width * 0.5f;
            float cy = rect.yMin + rect.height * 0.5f;

            int totalStone = 0;
            int projectedStone = 0;
            int insideStone = 0;
            List<C2OriginalResourceStoneProbeV1LikeOriginal> nearest = new List<C2OriginalResourceStoneProbeV1LikeOriginal>(64);

            for (int i = 0; i < state.Entries.Count; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal entry = state.Entries[i];
                if (entry == null || entry.Definition == null)
                    continue;

                if (entry.Definition.ResType != C2OriginalResourceStoneV1LikeOriginal)
                    continue;

                totalStone++;

                Vector3 world = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(entry.OriginalX, entry.OriginalY);
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0.0f)
                    continue;

                projectedStone++;

                float gx = screen.x;
                float gy = Screen.height - screen.y;
                bool inside = rect.Contains(new Vector2(gx, gy));
                if (inside)
                    insideStone++;

                float dxEdge = 0.0f;
                if (gx < rect.xMin) dxEdge = rect.xMin - gx;
                else if (gx > rect.xMax) dxEdge = gx - rect.xMax;

                float dyEdge = 0.0f;
                if (gy < rect.yMin) dyEdge = rect.yMin - gy;
                else if (gy > rect.yMax) dyEdge = gy - rect.yMax;

                float edgeDist = Mathf.Sqrt(dxEdge * dxEdge + dyEdge * dyEdge);
                float centerDx = gx - cx;
                float centerDy = gy - cy;
                float centerDist = Mathf.Sqrt(centerDx * centerDx + centerDy * centerDy);

                C2OriginalResourceStoneProbeV1LikeOriginal probe = new C2OriginalResourceStoneProbeV1LikeOriginal();
                probe.Entry = entry;
                probe.GuiX = gx;
                probe.GuiY = gy;
                probe.ScreenZ = screen.z;
                probe.EdgeDistance = edgeDist;
                probe.CenterDistance = centerDist;
                probe.Inside = inside;
                nearest.Add(probe);
            }

            nearest.Sort(delegate (C2OriginalResourceStoneProbeV1LikeOriginal a, C2OriginalResourceStoneProbeV1LikeOriginal b)
            {
                int c = a.EdgeDistance.CompareTo(b.EdgeDistance);
                if (c != 0) return c;
                c = a.CenterDistance.CompareTo(b.CenterDistance);
                if (c != 0) return c;
                int ax = a.Entry != null ? a.Entry.OriginalX : 0;
                int bx = b.Entry != null ? b.Entry.OriginalX : 0;
                c = ax.CompareTo(bx);
                if (c != 0) return c;
                int ay = a.Entry != null ? a.Entry.OriginalY : 0;
                int by = b.Entry != null ? b.Entry.OriginalY : 0;
                return ay.CompareTo(by);
            });

            Debug.Log("[C2:RESOURCE STONE PROBE V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                      " rectGui=(" +
                      x0.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      y0.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      w.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      h.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " stoneTotal=" + totalStone.ToString(CultureInfo.InvariantCulture) +
                      " stoneProjected=" + projectedStone.ToString(CultureInfo.InvariantCulture) +
                      " stoneInside=" + insideStone.ToString(CultureInfo.InvariantCulture) +
                      " nearestLogged=" + Mathf.Min(16, nearest.Count).ToString(CultureInfo.InvariantCulture) +
                      " mode=nearest_STONE_to_rect_edge_and_center");

            if (nearest.Count == 0)
                return;

            StringBuilder sb = new StringBuilder(4096);
            int count = Mathf.Min(16, nearest.Count);
            for (int i = 0; i < count; i++)
            {
                C2OriginalResourceStoneProbeV1LikeOriginal p = nearest[i];
                C2OriginalResourceEntryV1LikeOriginal e = p.Entry;
                C2OriginalResourceDefinitionV1LikeOriginal d = e != null ? e.Definition : null;

                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                sb.Append(":STONE:");
                sb.Append(d != null ? d.ObjectId : "<null>");
                sb.Append(" sign=");
                sb.Append(e != null ? e.Sign : "?");
                sb.Append(" sg=");
                sb.Append(e != null ? e.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "?");
                sb.Append(" map=(");
                sb.Append(e != null ? e.OriginalX.ToString(CultureInfo.InvariantCulture) : "?");
                sb.Append(",");
                sb.Append(e != null ? e.OriginalY.ToString(CultureInfo.InvariantCulture) : "?");
                sb.Append(") gui=(");
                sb.Append(p.GuiX.ToString("0", CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(p.GuiY.ToString("0", CultureInfo.InvariantCulture));
                sb.Append(") edgeDist=");
                sb.Append(p.EdgeDistance.ToString("0.0", CultureInfo.InvariantCulture));
                sb.Append(" centerDist=");
                sb.Append(p.CenterDistance.ToString("0.0", CultureInfo.InvariantCulture));
                sb.Append(" inside=");
                sb.Append(p.Inside ? "1" : "0");
                sb.Append(" z=");
                sb.Append(p.ScreenZ.ToString("0.00", CultureInfo.InvariantCulture));
                sb.Append("; ");
            }

            Debug.Log("[C2:RESOURCE STONE PROBE V1G] sel=" + selectionId.ToString(CultureInfo.InvariantCulture) +
                      " nearest=" + sb.ToString());
        }


        public void C2OriginalResourceMapV1RunResourceLinkAuditLikeOriginal(string reason)
        {
            if (!C2OriginalResourceMapV1TryBuildLikeOriginal("link-audit"))
                return;

            C2OriginalResourceMapStateV1LikeOriginal state = _c2OriginalResourceMapV1StateLikeOriginal;
            if (state == null || state.Entries == null)
                return;

            int markerCreated = 0;
            int markerReused = 0;
            if (C2OriginalResourceMapV1CreateMarkerObjectsLikeOriginal)
                C2OriginalResourceMapV1EnsureMarkerObjectsLikeOriginal(state, out markerCreated, out markerReused);

            int oneSpriteCreated = 0;
            int oneSpriteReused = 0;
            if (C2OriginalResourceMapV1CreateOneSpriteRuntimeLikeOriginal)
                C2OriginalResourceMapV1EnsureOneSpriteRuntimeObjectsLikeOriginal(state, out oneSpriteCreated, out oneSpriteReused);

            Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            int rendererTotal = 0;
            int natureRendererTotal = 0;
            int natureGA = 0;
            int natureTS = 0;
            int natureOC = 0;
            int natureFIELD = 0;
            int natureShadow = 0;
            int natureOther = 0;
            List<Renderer> natureRenderers = new List<Renderer>();

            if (renderers != null)
            {
                rendererTotal = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null || r.gameObject == null)
                        continue;
                    string n = r.gameObject.name ?? string.Empty;
                    if (n.IndexOf("C2_Nature_", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (_c2OriginalResourceMapV1MarkerRootLikeOriginal != null && r.transform.IsChildOf(_c2OriginalResourceMapV1MarkerRootLikeOriginal.transform))
                        continue;
                    if (_c2OriginalResourceMapV1OneSpriteRootLikeOriginal != null && r.transform.IsChildOf(_c2OriginalResourceMapV1OneSpriteRootLikeOriginal.transform))
                        continue;

                    natureRendererTotal++;
                    natureRenderers.Add(r);
                    if (n.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0) natureShadow++;
                    else if (n.IndexOf("C2_Nature_GA", StringComparison.OrdinalIgnoreCase) >= 0) natureGA++;
                    else if (n.IndexOf("C2_Nature_TS", StringComparison.OrdinalIgnoreCase) >= 0) natureTS++;
                    else if (n.IndexOf("C2_Nature_OC", StringComparison.OrdinalIgnoreCase) >= 0) natureOC++;
                    else if (n.IndexOf("C2_Nature_FIELD", StringComparison.OrdinalIgnoreCase) >= 0) natureFIELD++;
                    else natureOther++;
                }
            }

            int oneSpriteLinked = 0;
            int oneSpriteUnlinked = 0;
            if (C2OriginalResourceMapV1CreateOneSpriteRuntimeLikeOriginal)
            {
                C2OriginalResourceMapV1UpdateOneSpriteRuntimeLinksLikeOriginal(state, natureRenderers, out oneSpriteLinked, out oneSpriteUnlinked);
                C2OriginalResourceMapV1LogOneSpriteRuntimeSummaryLikeOriginal(state, reason, oneSpriteCreated, oneSpriteReused, oneSpriteLinked, oneSpriteUnlinked);
            }

            int stoneTotal = 0;
            int stoneGA = 0;
            int stoneTS = 0;
            int stoneOC = 0;
            int stoneOtherSign = 0;
            int visualMarkersTotal = 0;
            int visualMarkersOutsideDebugRoot = 0;
            int visualMarkersWood = 0;
            int visualMarkersStone = 0;
            int visualMarkersFood = 0;
            C2OriginalResourceMarkerLikeOriginal[] markers = UnityEngine.Object.FindObjectsOfType<C2OriginalResourceMarkerLikeOriginal>();
            if (markers != null)
            {
                visualMarkersTotal = markers.Length;
                for (int i = 0; i < markers.Length; i++)
                {
                    C2OriginalResourceMarkerLikeOriginal m = markers[i];
                    if (m == null) continue;
                    bool insideDebugRoot = _c2OriginalResourceMapV1MarkerRootLikeOriginal != null && m.transform.IsChildOf(_c2OriginalResourceMapV1MarkerRootLikeOriginal.transform);
                    if (!insideDebugRoot)
                    {
                        visualMarkersOutsideDebugRoot++;
                        if (m.ResourceId == C2OriginalResourceWoodV1LikeOriginal) visualMarkersWood++;
                        else if (m.ResourceId == C2OriginalResourceStoneV1LikeOriginal) visualMarkersStone++;
                        else if (m.ResourceId == C2OriginalResourceFoodV1LikeOriginal) visualMarkersFood++;
                    }
                }
            }

            for (int i = 0; i < state.Entries.Count; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal e = state.Entries[i];
                if (e == null || e.Definition == null || e.Definition.ResType != C2OriginalResourceStoneV1LikeOriginal)
                    continue;
                stoneTotal++;
                if (string.Equals(e.Sign, "GA", StringComparison.OrdinalIgnoreCase)) stoneGA++;
                else if (string.Equals(e.Sign, "TS", StringComparison.OrdinalIgnoreCase)) stoneTS++;
                else if (string.Equals(e.Sign, "OC", StringComparison.OrdinalIgnoreCase)) stoneOC++;
                else stoneOtherSign++;
            }

            Debug.Log("[C2:RESOURCE LINK V1G] reason=" + reason +
                      " contract=" + C2OriginalResourceMapV1ContractLikeOriginal +
                      " entries=" + state.Entries.Count.ToString(CultureInfo.InvariantCulture) +
                      " WOOD=" + state.WoodCount.ToString(CultureInfo.InvariantCulture) +
                      " STONE=" + state.StoneCount.ToString(CultureInfo.InvariantCulture) +
                      " FOOD=" + state.FoodCount.ToString(CultureInfo.InvariantCulture) +
                      " buckets=" + state.Buckets.Count.ToString(CultureInfo.InvariantCulture) +
                      " exactLookup=" + state.EntriesByExactKey.Count.ToString(CultureInfo.InvariantCulture) +
                      " signSpriteLookup=" + state.EntriesBySignSprite.Count.ToString(CultureInfo.InvariantCulture) +
                      " markersCreated=" + markerCreated.ToString(CultureInfo.InvariantCulture) +
                      " markersReused=" + markerReused.ToString(CultureInfo.InvariantCulture) +
                      " markerObjects=" + visualMarkersTotal.ToString(CultureInfo.InvariantCulture) +
                      " visualResourceMarkersOutsideDebugRoot=" + visualMarkersOutsideDebugRoot.ToString(CultureInfo.InvariantCulture) +
                      " visualLinked WOOD=" + visualMarkersWood.ToString(CultureInfo.InvariantCulture) +
                      " STONE=" + visualMarkersStone.ToString(CultureInfo.InvariantCulture) +
                      " FOOD=" + visualMarkersFood.ToString(CultureInfo.InvariantCulture));

            Debug.Log("[C2:RESOURCE VISUAL V1G] renderersTotal=" + rendererTotal.ToString(CultureInfo.InvariantCulture) +
                      " natureRenderers=" + natureRendererTotal.ToString(CultureInfo.InvariantCulture) +
                      " natureGA=" + natureGA.ToString(CultureInfo.InvariantCulture) +
                      " natureTS=" + natureTS.ToString(CultureInfo.InvariantCulture) +
                      " natureOC=" + natureOC.ToString(CultureInfo.InvariantCulture) +
                      " natureFIELD=" + natureFIELD.ToString(CultureInfo.InvariantCulture) +
                      " natureShadow=" + natureShadow.ToString(CultureInfo.InvariantCulture) +
                      " natureOther=" + natureOther.ToString(CultureInfo.InvariantCulture) +
                      " note='if visualResourceMarkersOutsideDebugRoot=0, current visible nature meshes are plain batches without original ResType identity' ");

            Debug.Log("[C2:RESOURCE STONE SIGN V1G] stoneTotal=" + stoneTotal.ToString(CultureInfo.InvariantCulture) +
                      " signGA=" + stoneGA.ToString(CultureInfo.InvariantCulture) +
                      " signTS=" + stoneTS.ToString(CultureInfo.InvariantCulture) +
                      " signOC=" + stoneOC.ToString(CultureInfo.InvariantCulture) +
                      " signOther=" + stoneOtherSign.ToString(CultureInfo.InvariantCulture) +
                      " note='original STONE may be GA/D###, not only TS/stonlist visuals' ");

            C2OriginalResourceMapV1LogStoneVisualLinksLikeOriginal(state, natureRenderers, reason);

            _c2OriginalResourceMapV1LinkAuditDoneLikeOriginal = true;
        }


        private void C2OriginalResourceMapV1EnsureOneSpriteRuntimeObjectsLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, out int created, out int reused)
        {
            created = 0;
            reused = 0;
            if (state == null || state.Entries == null)
                return;

            if (_c2OriginalResourceMapV1OneSpriteRootLikeOriginal == null)
            {
                GameObject old = GameObject.Find("C2_OneSprites_Runtime");
                if (old != null) _c2OriginalResourceMapV1OneSpriteRootLikeOriginal = old;
            }

            if (_c2OriginalResourceMapV1OneSpriteRootLikeOriginal == null)
            {
                _c2OriginalResourceMapV1OneSpriteRootLikeOriginal = new GameObject("C2_OneSprites_Runtime");
                _c2OriginalResourceMapV1OneSpriteRootLikeOriginal.hideFlags = HideFlags.DontSave;
            }

            Transform root = _c2OriginalResourceMapV1OneSpriteRootLikeOriginal.transform;

            for (int i = 0; i < state.Entries.Count; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal e = state.Entries[i];
                if (e == null || e.Definition == null)
                    continue;

                if (e.OneSpriteRuntime != null)
                {
                    reused++;
                    continue;
                }

                C2OriginalOneSpriteRuntimeLikeOriginal existing;
                if (state.OneSpritesByExactKey.TryGetValue(e.ExactKey, out existing) && existing != null)
                {
                    e.OneSpriteRuntime = existing;
                    reused++;
                    continue;
                }

                string resName = C2OriginalResourceMapV1ResourceNameLikeOriginal(e.Definition.ResType);
                string objectId = string.IsNullOrWhiteSpace(e.Definition.ObjectId) ? ("sg" + e.SpriteIndex.ToString(CultureInfo.InvariantCulture)) : e.Definition.ObjectId;
                string oneSpriteName = e.Sign + "_" + objectId + "_" +
                                       e.OriginalX.ToString(CultureInfo.InvariantCulture) + "_" +
                                       e.OriginalY.ToString(CultureInfo.InvariantCulture) + "_" + resName;

                GameObject go = new GameObject(oneSpriteName);
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(root, false);

                Vector3 world = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(e.OriginalX, e.OriginalY);
                go.transform.position = world;

                C2OriginalOneSpriteRuntimeLikeOriginal os = go.AddComponent<C2OriginalOneSpriteRuntimeLikeOriginal>();
                os.ConfigureLikeOriginal(
                    e.ExactKey,
                    e.SignSpriteKey,
                    e.Sign,
                    e.SpriteIndex,
                    objectId,
                    e.OriginalX,
                    e.OriginalY,
                    e.OriginalZ,
                    e.Order,
                    e.Section,
                    e.NIndex,
                    e.Locking,
                    e.HasMatrix,
                    e.Definition.ResType,
                    resName,
                    e.Definition.ResPerWork,
                    e.Definition.WorkRadius,
                    e.Definition.WorkAmount,
                    e.Definition.WorkNextIndex,
                    e.Definition.TimeAmount,
                    e.Definition.TimeNextIndex,
                    e.Definition.IsFieldPath,
                    e.Definition.FieldWidth,
                    e.Definition.FieldHeight,
                    e.Definition.FieldGrowStage,
                    e.Definition.FieldYScale,
                    world,
                    null);

                e.OneSpriteRuntime = os;
                state.OneSpritesByExactKey[e.ExactKey] = os;
                long bucketKey = C2OriginalResourceMapV1BucketKeyLikeOriginal(e.OriginalX >> C2OriginalResourceMapV1CellShiftLikeOriginal, e.OriginalY >> C2OriginalResourceMapV1CellShiftLikeOriginal);
                List<C2OriginalOneSpriteRuntimeLikeOriginal> bucket;
                if (!state.OneSpriteBuckets.TryGetValue(bucketKey, out bucket) || bucket == null)
                {
                    bucket = new List<C2OriginalOneSpriteRuntimeLikeOriginal>();
                    state.OneSpriteBuckets[bucketKey] = bucket;
                }
                bucket.Add(os);
                created++;
            }
        }

        private void C2OriginalResourceMapV1UpdateOneSpriteRuntimeLinksLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, List<Renderer> natureRenderers, out int linked, out int unlinked)
        {
            linked = 0;
            unlinked = 0;
            if (state == null || state.Entries == null)
                return;

            for (int i = 0; i < state.Entries.Count; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal e = state.Entries[i];
                if (e == null || e.OneSpriteRuntime == null)
                    continue;

                Renderer nearest;
                float nearestDist;
                bool boundsContains;
                C2OriginalResourceMapV1FindNearestNatureRendererLikeOriginal(e.OneSpriteRuntime.WorldPosition, natureRenderers, out nearest, out nearestDist, out boundsContains);
                e.OneSpriteRuntime.LinkedBatchRenderer = nearest;
                e.OneSpriteRuntime.LinkedBatchRendererName = nearest != null && nearest.gameObject != null ? nearest.gameObject.name : string.Empty;
                e.OneSpriteRuntime.LinkedBatchDistance = nearestDist;
                e.OneSpriteRuntime.LinkedBatchBoundsContains = boundsContains;

                if (nearest != null && boundsContains)
                    linked++;
                else
                    unlinked++;
            }
        }

        private void C2OriginalResourceMapV1LogOneSpriteRuntimeSummaryLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, string reason, int created, int reused, int linked, int unlinked)
        {
            if (state == null)
                return;

            int runtimeTotal = state.OneSpritesByExactKey != null ? state.OneSpritesByExactKey.Count : 0;
            int runtimeBuckets = state.OneSpriteBuckets != null ? state.OneSpriteBuckets.Count : 0;
            int runtimeWood = 0;
            int runtimeStone = 0;
            int runtimeFood = 0;
            int runtimeOther = 0;
            int gaStone = 0;
            int tsStone = 0;
            int ocFood = 0;

            if (state.Entries != null)
            {
                for (int i = 0; i < state.Entries.Count; i++)
                {
                    C2OriginalResourceEntryV1LikeOriginal e = state.Entries[i];
                    if (e == null || e.Definition == null || e.OneSpriteRuntime == null)
                        continue;

                    byte res = e.Definition.ResType;
                    if (res == C2OriginalResourceWoodV1LikeOriginal) runtimeWood++;
                    else if (res == C2OriginalResourceStoneV1LikeOriginal) runtimeStone++;
                    else if (res == C2OriginalResourceFoodV1LikeOriginal) runtimeFood++;
                    else runtimeOther++;

                    if (res == C2OriginalResourceStoneV1LikeOriginal && string.Equals(e.Sign, "GA", StringComparison.OrdinalIgnoreCase)) gaStone++;
                    if (res == C2OriginalResourceStoneV1LikeOriginal && string.Equals(e.Sign, "TS", StringComparison.OrdinalIgnoreCase)) tsStone++;
                    if (res == C2OriginalResourceFoodV1LikeOriginal && string.Equals(e.Sign, "OC", StringComparison.OrdinalIgnoreCase)) ocFood++;
                }
            }

            Debug.Log("[C2:ONESPRITE RUNTIME V1G] reason=" + reason +
                      " contract=" + C2OriginalResourceMapV1ContractLikeOriginal +
                      " root='C2_OneSprites_Runtime'" +
                      " entries=" + (state.Entries != null ? state.Entries.Count.ToString(CultureInfo.InvariantCulture) : "0") +
                      " runtime=" + runtimeTotal.ToString(CultureInfo.InvariantCulture) +
                      " created=" + created.ToString(CultureInfo.InvariantCulture) +
                      " reused=" + reused.ToString(CultureInfo.InvariantCulture) +
                      " buckets=" + runtimeBuckets.ToString(CultureInfo.InvariantCulture) +
                      " WOOD=" + runtimeWood.ToString(CultureInfo.InvariantCulture) +
                      " STONE=" + runtimeStone.ToString(CultureInfo.InvariantCulture) +
                      " FOOD=" + runtimeFood.ToString(CultureInfo.InvariantCulture) +
                      " OTHER=" + runtimeOther.ToString(CultureInfo.InvariantCulture) +
                      " GA_STONE=" + gaStone.ToString(CultureInfo.InvariantCulture) +
                      " TS_STONE=" + tsStone.ToString(CultureInfo.InvariantCulture) +
                      " OC_FOOD=" + ocFood.ToString(CultureInfo.InvariantCulture) +
                      " linkedBatch=" + linked.ToString(CultureInfo.InvariantCulture) +
                      " unlinkedBatch=" + unlinked.ToString(CultureInfo.InvariantCulture) +
                      " note='gameplay must read this OneSprite runtime layer, not the batched renderers'");
        }

        private void C2OriginalResourceMapV1EnsureMarkerObjectsLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, out int created, out int reused)
        {
            created = 0;
            reused = 0;
            if (state == null || state.Entries == null)
                return;

            if (_c2OriginalResourceMapV1MarkerRootLikeOriginal == null)
            {
                GameObject old = GameObject.Find("C2_OriginalResourceMarkers_V1F");
                if (old != null) _c2OriginalResourceMapV1MarkerRootLikeOriginal = old;
            }

            if (_c2OriginalResourceMapV1MarkerRootLikeOriginal == null)
            {
                _c2OriginalResourceMapV1MarkerRootLikeOriginal = new GameObject("C2_OriginalResourceMarkers_V1F");
                _c2OriginalResourceMapV1MarkerRootLikeOriginal.hideFlags = HideFlags.DontSave;
            }

            Transform root = _c2OriginalResourceMapV1MarkerRootLikeOriginal.transform;
            for (int i = 0; i < state.Entries.Count; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal e = state.Entries[i];
                if (e == null || e.Definition == null)
                    continue;

                if (e.Marker != null)
                {
                    reused++;
                    continue;
                }

                string markerName = "RES_" + C2OriginalResourceMapV1ResourceNameLikeOriginal(e.Definition.ResType) + "_" +
                                    e.Sign + "_" + e.Definition.ObjectId + "_sg" + e.SpriteIndex.ToString(CultureInfo.InvariantCulture) + "_" +
                                    e.OriginalX.ToString(CultureInfo.InvariantCulture) + "_" + e.OriginalY.ToString(CultureInfo.InvariantCulture);
                GameObject go = new GameObject(markerName);
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                go.transform.SetParent(root, false);
                go.transform.position = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(e.OriginalX, e.OriginalY);
                C2OriginalResourceMarkerLikeOriginal marker = go.AddComponent<C2OriginalResourceMarkerLikeOriginal>();
                marker.ConfigureLikeOriginal(
                    e.ExactKey,
                    e.SignSpriteKey,
                    e.Sign,
                    e.SpriteIndex,
                    e.Definition != null ? e.Definition.ObjectId : string.Empty,
                    e.OriginalX,
                    e.OriginalY,
                    e.Definition != null ? e.Definition.ResType : C2OriginalResourceEmptyV1LikeOriginal,
                    C2OriginalResourceMapV1ResourceNameLikeOriginal(e.Definition != null ? e.Definition.ResType : C2OriginalResourceEmptyV1LikeOriginal),
                    e.Definition != null ? e.Definition.WorkRadius : 0,
                    e.Definition != null ? e.Definition.ResPerWork : 0,
                    e.Order);
                e.Marker = marker;
                created++;
            }
        }

        private void C2OriginalResourceMapV1LogStoneVisualLinksLikeOriginal(C2OriginalResourceMapStateV1LikeOriginal state, List<Renderer> natureRenderers, string reason)
        {
            if (state == null || state.Entries == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cams = Camera.allCameras;
                if (cams != null && cams.Length > 0) cam = cams[0];
            }

            int limit = Mathf.Clamp(C2OriginalResourceMapV1StoneLinkAuditLimitLikeOriginal, 1, 200);
            StringBuilder sb = new StringBuilder(4096);
            int logged = 0;
            int part = 1;

            for (int i = 0; i < state.Entries.Count && logged < limit; i++)
            {
                C2OriginalResourceEntryV1LikeOriginal e = state.Entries[i];
                if (e == null || e.Definition == null || e.Definition.ResType != C2OriginalResourceStoneV1LikeOriginal)
                    continue;

                Vector3 world = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(e.OriginalX, e.OriginalY);
                Renderer nearest = null;
                float nearestDist;
                bool boundsContains;
                C2OriginalResourceMapV1FindNearestNatureRendererLikeOriginal(world, natureRenderers, out nearest, out nearestDist, out boundsContains);

                string gui = "?";
                if (cam != null)
                {
                    Vector3 screen = cam.WorldToScreenPoint(world);
                    gui = "(" + screen.x.ToString("0", CultureInfo.InvariantCulture) + "," + (Screen.height - screen.y).ToString("0", CultureInfo.InvariantCulture) + ",z=" + screen.z.ToString("0.0", CultureInfo.InvariantCulture) + ")";
                }

                string nearestName = nearest != null && nearest.gameObject != null ? nearest.gameObject.name : "<none>";
                string token = logged.ToString(CultureInfo.InvariantCulture) + ":STONE:" +
                               (e.Definition != null ? e.Definition.ObjectId : "<null>") +
                               " sign=" + e.Sign +
                               " sg=" + e.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                               " key=" + e.ExactKey +
                               " map=(" + e.OriginalX.ToString(CultureInfo.InvariantCulture) + "," + e.OriginalY.ToString(CultureInfo.InvariantCulture) + ")" +
                               " world=" + C2OriginalResourceMapV1Vec3LikeOriginal(world) +
                               " gui=" + gui +
                               " nearestRenderer='" + nearestName + "'" +
                               " nearestDist=" + nearestDist.ToString("0.00", CultureInfo.InvariantCulture) +
                               " boundsContains=" + (boundsContains ? "1" : "0") + "; ";

                if (sb.Length + token.Length > 3200)
                {
                    Debug.Log("[C2:RESOURCE STONE LINK V1G] reason=" + reason + " part=" + part.ToString(CultureInfo.InvariantCulture) + " entries=" + sb.ToString());
                    sb.Length = 0;
                    part++;
                }

                sb.Append(token);
                logged++;
            }

            if (sb.Length > 0)
                Debug.Log("[C2:RESOURCE STONE LINK V1G] reason=" + reason + " part=" + part.ToString(CultureInfo.InvariantCulture) + " entries=" + sb.ToString());
        }

        private static void C2OriginalResourceMapV1FindNearestNatureRendererLikeOriginal(Vector3 world, List<Renderer> renderers, out Renderer nearest, out float nearestDist, out bool boundsContains)
        {
            nearest = null;
            nearestDist = 999999.0f;
            boundsContains = false;
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                    continue;
                Bounds b = r.bounds;
                bool contains = b.Contains(world);
                Vector3 p = b.ClosestPoint(world);
                float dist = Vector3.Distance(world, p);
                if (contains) dist = 0.0f;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = r;
                    boundsContains = contains;
                }
            }
        }

        private static string C2OriginalResourceMapV1Vec3LikeOriginal(Vector3 v)
        {
            return "(" + v.x.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                         v.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                         v.z.ToString("0.00", CultureInfo.InvariantCulture) + ")";
        }

        private sealed class C2OriginalResourceMapStateV1LikeOriginal
        {
            public string MapPath = string.Empty;
            public readonly Dictionary<string, C2OriginalResourceCatalogV1LikeOriginal> CatalogsBySign = new Dictionary<string, C2OriginalResourceCatalogV1LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            public readonly List<C2OriginalResourceEntryV1LikeOriginal> Entries = new List<C2OriginalResourceEntryV1LikeOriginal>();
            public readonly Dictionary<long, List<C2OriginalResourceEntryV1LikeOriginal>> Buckets = new Dictionary<long, List<C2OriginalResourceEntryV1LikeOriginal>>();
            public readonly Dictionary<string, C2OriginalResourceEntryV1LikeOriginal> EntriesByExactKey = new Dictionary<string, C2OriginalResourceEntryV1LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, List<C2OriginalResourceEntryV1LikeOriginal>> EntriesBySignSprite = new Dictionary<string, List<C2OriginalResourceEntryV1LikeOriginal>>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, C2OriginalOneSpriteRuntimeLikeOriginal> OneSpritesByExactKey = new Dictionary<string, C2OriginalOneSpriteRuntimeLikeOriginal>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<long, List<C2OriginalOneSpriteRuntimeLikeOriginal>> OneSpriteBuckets = new Dictionary<long, List<C2OriginalOneSpriteRuntimeLikeOriginal>>();
            public int WoodCount;
            public int StoneCount;
            public int FoodCount;
            public int OtherResourceCount;
            public int SkippedNoCatalog;
            public int SkippedNoDefinition;
            public int SkippedNoResource;
        }

        private sealed class C2OriginalResourceCatalogV1LikeOriginal
        {
            public string Sign = string.Empty;
            public string Label = string.Empty;
            public string GpName = string.Empty;
            public string SourceListPath = string.Empty;
            public string SourceRsrPath = string.Empty;
            public int DeclaredCount;
            public readonly Dictionary<int, C2OriginalResourceDefinitionV1LikeOriginal> DefinitionsByIndex = new Dictionary<int, C2OriginalResourceDefinitionV1LikeOriginal>();
            public readonly Dictionary<string, C2OriginalResourceDefinitionV1LikeOriginal> DefinitionsByName = new Dictionary<string, C2OriginalResourceDefinitionV1LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class C2OriginalResourceDefinitionV1LikeOriginal
        {
            public string GroupSign = string.Empty;
            public string GroupLabel = string.Empty;
            public int Index;
            public int SpriteIndex;
            public string ObjectId = string.Empty;
            public int CenterX;
            public int CenterY;
            public int Radius;
            public byte ResType = C2OriginalResourceEmptyV1LikeOriginal;
            public int ResPerWork;
            public int WorkRadius;
            public int WorkNextIndex = -1;
            public int TimeNextIndex = -1;
            public int WorkAmount;
            public int TimeAmount;
            public bool IsFieldPath;
            public int FieldWidth;
            public int FieldHeight;
            public int FieldGrowStage;
            public int FieldYScale;
        }

        private sealed class C2OriginalResourceSelectionHitV1LikeOriginal
        {
            public C2OriginalResourceEntryV1LikeOriginal Entry;
            public byte ResId;
            public float ScreenX;
            public float ScreenY;
        }

        private sealed class C2OriginalResourceStoneProbeV1LikeOriginal
        {
            public C2OriginalResourceEntryV1LikeOriginal Entry;
            public float GuiX;
            public float GuiY;
            public float ScreenZ;
            public float EdgeDistance;
            public float CenterDistance;
            public bool Inside;
        }

        private sealed class C2OriginalResourceEntryV1LikeOriginal
        {
            public int Order;
            public string Section = string.Empty;
            public string Sign = string.Empty;
            public int OriginalX;
            public int OriginalY;
            public int OriginalZ;
            public int SpriteIndex;
            public string ExactKey = string.Empty;
            public string SignSpriteKey = string.Empty;
            public int NIndex;
            public int Locking;
            public bool HasMatrix;
            public Matrix4x4 Matrix;
            public C2OriginalResourceDefinitionV1LikeOriginal Definition;
            public C2OriginalResourceMarkerLikeOriginal Marker;
            public C2OriginalOneSpriteRuntimeLikeOriginal OneSpriteRuntime;
        }
    }

    public sealed class C2OriginalResourceMapV1AutoRunnerLikeOriginal : MonoBehaviour
    {
        private static bool _installed;
        private C2BattleTerrainMode _mode;
        private float _nextHoverLogTime;
        private bool _resourceSelectDragging;
        private Vector2 _resourceSelectStartGui;
        private Vector2 _resourceSelectLastGui;
        private int _resourceSelectSerial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLikeOriginal()
        {
            if (_installed) return;
            _installed = true;
            GameObject go = new GameObject("C2_OriginalResourceMap_V1_AutoRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<C2OriginalResourceMapV1AutoRunnerLikeOriginal>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 1200; i++)
            {
                _mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                if (_mode != null && _mode.C2OriginalResourceMapV1AutoBuildLikeOriginal)
                {
                    if (_mode.C2OriginalResourceMapV1TryBuildLikeOriginal("auto"))
                    {
                        if (_mode.C2OriginalResourceMapV1AutoLinkAuditLikeOriginal)
                        {
                            for (int wait = 0; wait < 90; wait++) yield return null;
                            _mode.C2OriginalResourceMapV1RunResourceLinkAuditLikeOriginal("auto_after_build_90_frames");
                        }
                        yield break;
                    }
                }
                yield return null;
            }
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e == null) return;

            if (_mode == null)
                _mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (_mode == null) return;

            Vector3 screen = C2OriginalResourceMapV1GuiToScreenLikeOriginal(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _resourceSelectDragging = true;
                _resourceSelectStartGui = e.mousePosition;
                _resourceSelectLastGui = e.mousePosition;
            }
            else if (_resourceSelectDragging && e.type == EventType.MouseDrag && e.button == 0)
            {
                _resourceSelectLastGui = e.mousePosition;
            }
            else if (_resourceSelectDragging && e.type == EventType.MouseUp && e.button == 0)
            {
                _resourceSelectLastGui = e.mousePosition;
                _resourceSelectDragging = false;
                _resourceSelectSerial++;
                _mode.C2OriginalResourceMapV1DebugSelectionGuiRectLikeOriginal(_resourceSelectStartGui, _resourceSelectLastGui, _resourceSelectSerial);
            }


            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F4)
            {
                _mode.C2OriginalResourceMapV1DebugMouseLogLikeOriginal = !_mode.C2OriginalResourceMapV1DebugMouseLogLikeOriginal;
                Debug.Log("[C2:RESOURCE HOVER V1G] toggle interval=" + (_mode.C2OriginalResourceMapV1DebugMouseLogLikeOriginal ? "ON" : "OFF") +
                          " interval=" + _mode.C2OriginalResourceMapV1DebugLogIntervalSecondsLikeOriginal.ToString("0.00", CultureInfo.InvariantCulture) +
                          " hotkeys=F4_toggle_F5_once_F6_link_audit click_logs_when_enabled rect_select=left_drag_release");
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F6)
            {
                _mode.C2OriginalResourceMapV1RunResourceLinkAuditLikeOriginal("F6_manual");
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F5)
            {
                _mode.C2OriginalResourceMapV1DebugMouseScreenLikeOriginal(screen, "F5_once");
                e.Use();
                return;
            }

            if (_mode.C2OriginalResourceMapV1DebugMouseLogLikeOriginal &&
                _mode.C2OriginalResourceMapV1DebugLogMouseClicksLikeOriginal &&
                e.type == EventType.MouseDown)
            {
                _mode.C2OriginalResourceMapV1DebugMouseScreenLikeOriginal(screen, "mouse_down_" + e.button.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (_mode.C2OriginalResourceMapV1DebugMouseLogLikeOriginal && e.type == EventType.Repaint)
            {
                float now = Time.realtimeSinceStartup;
                float interval = Mathf.Max(0.05f, _mode.C2OriginalResourceMapV1DebugLogIntervalSecondsLikeOriginal);
                if (now >= _nextHoverLogTime)
                {
                    _nextHoverLogTime = now + interval;
                    _mode.C2OriginalResourceMapV1DebugMouseScreenLikeOriginal(screen, "interval");
                }
            }
        }

        private static Vector3 C2OriginalResourceMapV1GuiToScreenLikeOriginal(Vector2 guiMouse)
        {
            return new Vector3(guiMouse.x, Screen.height - guiMouse.y, 0.0f);
        }
    }
}
