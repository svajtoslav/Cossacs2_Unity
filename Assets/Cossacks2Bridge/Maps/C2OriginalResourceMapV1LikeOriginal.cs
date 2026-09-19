// C2OriginalResourceMapV1LikeOriginal.cs
// Saved-map nature/resources ported from Cossacks II MapSprites.cpp/NewMon.cpp.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using TemnyLessViewer;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private sealed class C2OriginalResourceDefV1
        {
            public string Name = string.Empty;
            public int CenterX;
            public int CenterY;
            public int Radius;
            public int Frame;
            public byte Resource = C2OriginalResourceEmptyV1LikeOriginal;
            public int ResourcePerWork;
            public int WorkRadius;
            public int WorkAmount;
            public string NextWorkName = string.Empty;
            public C2OriginalResourceDefV1 NextWorkDef;
        }

        private sealed class C2OriginalResourceSpriteV1
        {
            public string Sign = string.Empty;
            public int X;
            public int Y;
            public int SpriteIndex;
            public int NIndex;
            public int Locking;
            public bool Enabled = true;
            public int WorkOver;
            public C2OriginalResourceDefV1 Def;
        }

        private sealed class C2OriginalNatureFrameV1
        {
            public Texture2D Texture;
            public Material ColorMaterial;
            public Material DepthMaterial;
            public int Width;
            public int Height;
            public int PivotX;
            public int PivotY;
        }

        private sealed class C2OriginalNatureMeshGroupV1
        {
            public readonly List<C2OriginalResourceSpriteV1> Sprites = new List<C2OriginalResourceSpriteV1>();
            public string Sign = string.Empty;
            public int Frame;
            public int BucketX;
            public int BucketY;
        }

        private readonly List<C2OriginalResourceSpriteV1> _c2OriginalResourcesV1 =
            new List<C2OriginalResourceSpriteV1>();
        private readonly Dictionary<long, List<int>> _c2OriginalResourceBucketsV1 =
            new Dictionary<long, List<int>>();
        private readonly Dictionary<string, C2OriginalNatureFrameV1> _c2OriginalNatureFramesV1 =
            new Dictionary<string, C2OriginalNatureFrameV1>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<long, C2NeutralPeasantUnitInfoV2LikeOriginal> _c2OriginalResourceWorkOwnersV1 =
            new Dictionary<long, C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private readonly Dictionary<int, long> _c2OriginalResourceWorkSlotByUnitV1 =
            new Dictionary<int, long>();
        private static readonly HashSet<long> s_c2OriginalNatureBlockedCellsV1 = new HashSet<long>();
        private GameObject _c2OriginalNatureRootV1;
        private bool _c2OriginalResourceReadyV1;
        private string _c2OriginalResourceMapPathV1 = string.Empty;

        public bool C2OriginalResourceMapV1IsReadyLikeOriginal()
        {
            return _c2OriginalResourceReadyV1 &&
                   string.Equals(_c2OriginalResourceMapPathV1, _mapRelativePath ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public bool C2OriginalResourceMapV1TryBuildLikeOriginal(string source = "manual")
        {
            if (C2OriginalResourceMapV1IsReadyLikeOriginal())
                return true;
            if (_bootstrap == null || _bootstrap.Fs == null || _map == null || _terrainRoot == null ||
                string.IsNullOrWhiteSpace(_mapRelativePath) || !_bootstrap.Fs.Exists(_mapRelativePath))
                return false;

            ClearOriginalResourceMapV1LikeOriginal(false);
            try
            {
                List<C2OriginalResourceDefV1> trees = LoadOriginalResourceCatalogV1LikeOriginal("treelist.lst", "treelist.rsr");
                List<C2OriginalResourceDefV1> stones = LoadOriginalResourceCatalogV1LikeOriginal("stonlist.lst", "stonlist.rsr");
                List<C2OriginalResourceDefV1> complex = LoadOriginalResourceCatalogV1LikeOriginal("complex.lst", "complex.rsr");

                ParseOriginalResourceSpritesFromMapV1LikeOriginal(trees, stones, complex);
                RebuildOriginalResourceBucketsV1LikeOriginal();
                BuildOriginalNatureVisualsV1LikeOriginal();

                _c2OriginalResourceMapPathV1 = _mapRelativePath ?? string.Empty;
                _c2OriginalResourceReadyV1 = true;
                Debug.Log("[C2:ORIGINAL RESOURCES V1] ready map='" + _c2OriginalResourceMapPathV1 +
                          "' savedSprites=" + _c2OriginalResourcesV1.Count.ToString(CultureInfo.InvariantCulture) +
                          " buckets=" + _c2OriginalResourceBucketsV1.Count.ToString(CultureInfo.InvariantCulture) +
                          " natureFrames=" + _c2OriginalNatureFramesV1.Count.ToString(CultureInfo.InvariantCulture) +
                          " source='" + (source ?? string.Empty) + "'");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:ORIGINAL RESOURCES V1] build failed source='" + (source ?? string.Empty) + "' " + ex);
                ClearOriginalResourceMapV1LikeOriginal(false);
                return false;
            }
        }

        public bool C2OriginalResourceMapV1TryDetermineResourceLikeOriginal(
            int originalX,
            int originalY,
            out byte resourceId,
            out string audit)
        {
            int rx, ry, radius;
            string name;
            return C2OriginalResourceMapV1TryDetermineResourceTargetV222LikeOriginal(
                originalX, originalY, out resourceId, out rx, out ry, out radius, out name, out audit);
        }

        public bool C2OriginalResourceMapV1TryDetermineResourceTargetV222LikeOriginal(
            int originalX,
            int originalY,
            out byte resourceId,
            out int resourceOriginalX,
            out int resourceOriginalY,
            out int workRadius,
            out string resourceName,
            out string audit)
        {
            resourceId = C2OriginalResourceEmptyV1LikeOriginal;
            resourceOriginalX = 0;
            resourceOriginalY = 0;
            workRadius = 0;
            resourceName = string.Empty;
            audit = "not_ready";
            if (!C2OriginalResourceMapV1IsReadyLikeOriginal() && !C2OriginalResourceMapV1TryBuildLikeOriginal("determine-resource"))
                return false;

            int bx = originalX >> 7;
            int by = originalY >> 7;
            long bestD2 = long.MaxValue;
            C2OriginalResourceSpriteV1 best = null;
            for (int yy = by - 2; yy <= by + 2; yy++)
            {
                for (int xx = bx - 2; xx <= bx + 2; xx++)
                {
                    if (!_c2OriginalResourceBucketsV1.TryGetValue(PackCellV1LikeOriginal(xx, yy), out List<int> indices))
                        continue;
                    for (int i = 0; i < indices.Count; i++)
                    {
                        int index = indices[i];
                        if (index < 0 || index >= _c2OriginalResourcesV1.Count) continue;
                        C2OriginalResourceSpriteV1 s = _c2OriginalResourcesV1[index];
                        if (s == null || !s.Enabled || s.Def == null || s.Def.Resource >= 0xFE) continue;
                        long dx = (long)s.X - originalX;
                        long dy = (long)s.Y - originalY;
                        long d2 = dx * dx + dy * dy;
                        if (d2 < bestD2)
                        {
                            bestD2 = d2;
                            best = s;
                        }
                    }
                }
            }

            // Original DetermineResource accepts the closest source only while Norma < 160.
            if (best == null || bestD2 >= 160L * 160L)
            {
                audit = "empty original=" + originalX.ToString(CultureInfo.InvariantCulture) + "/" +
                        originalY.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            resourceId = best.Def.Resource;
            resourceOriginalX = best.X;
            resourceOriginalY = best.Y;
            workRadius = Mathf.Max(1, best.Def.WorkRadius);
            resourceName = best.Sign + ":" + best.Def.Name;
            audit = "ok name='" + resourceName + "' resource=" + resourceId.ToString(CultureInfo.InvariantCulture) +
                    " target=" + resourceOriginalX.ToString(CultureInfo.InvariantCulture) + "/" +
                    resourceOriginalY.ToString(CultureInfo.InvariantCulture) +
                    " workRadius=" + workRadius.ToString(CultureInfo.InvariantCulture) +
                    " d2=" + bestD2.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public static bool C2BuildingMotionFieldV1IsBlockedLikeOriginal(int cellX, int cellY)
        {
            return s_c2OriginalNatureBlockedCellsV1.Contains(PackCellV1LikeOriginal(cellX, cellY));
        }

        public bool C2OriginalResourceMapV1TryFindResourceWorkTargetLikeOriginal(
            byte resourceId,
            int searchCenterOriginalX,
            int searchCenterOriginalY,
            int searchRadiusOriginalPixels,
            C2NeutralPeasantUnitInfoV2LikeOriginal worker,
            out int resourceOriginalX,
            out int resourceOriginalY,
            out int workOriginalX,
            out int workOriginalY,
            out int workRadius,
            out string resourceName,
            out string audit)
        {
            resourceOriginalX = resourceOriginalY = workOriginalX = workOriginalY = workRadius = 0;
            resourceName = string.Empty;
            audit = "not_ready";
            if (worker == null || (!C2OriginalResourceMapV1IsReadyLikeOriginal() &&
                !C2OriginalResourceMapV1TryBuildLikeOriginal("find-resource-work"))) return false;

            ReleaseOriginalResourceWorkSlotV1LikeOriginal(worker);
            long limit2 = searchRadiusOriginalPixels > 0
                ? (long)searchRadiusOriginalPixels * searchRadiusOriginalPixels
                : long.MaxValue;
            var candidates = new List<int>();
            for (int i = 0; i < _c2OriginalResourcesV1.Count; i++)
            {
                C2OriginalResourceSpriteV1 s = _c2OriginalResourcesV1[i];
                if (s == null || !s.Enabled || s.Def == null || s.Def.Resource != resourceId) continue;
                long dx = (long)s.X - searchCenterOriginalX;
                long dy = (long)s.Y - searchCenterOriginalY;
                if (dx * dx + dy * dy <= limit2) candidates.Add(i);
            }
            candidates.Sort((a, b) =>
            {
                C2OriginalResourceSpriteV1 sa = _c2OriginalResourcesV1[a];
                C2OriginalResourceSpriteV1 sb = _c2OriginalResourcesV1[b];
                long adx = (long)sa.X - searchCenterOriginalX, ady = (long)sa.Y - searchCenterOriginalY;
                long bdx = (long)sb.X - searchCenterOriginalX, bdy = (long)sb.Y - searchCenterOriginalY;
                return (adx * adx + ady * ady).CompareTo(bdx * bdx + bdy * bdy);
            });

            for (int n = 0; n < candidates.Count; n++)
            {
                int spriteIndex = candidates[n];
                C2OriginalResourceSpriteV1 s = _c2OriginalResourcesV1[spriteIndex];
                if (!TryReserveOriginalResourceWorkPositionV1LikeOriginal(
                        spriteIndex, s, worker, out workOriginalX, out workOriginalY, out int slot)) continue;
                resourceOriginalX = s.X;
                resourceOriginalY = s.Y;
                workRadius = Mathf.Max(1, s.Def.WorkRadius);
                resourceName = s.Sign + ":" + s.Def.Name;
                audit = "FindResourceObject+FindPlaceForPeasant resource='" + resourceName +
                        "' sprite=" + spriteIndex.ToString(CultureInfo.InvariantCulture) +
                        " slot=" + slot.ToString(CultureInfo.InvariantCulture) +
                        " resourcePix=" + resourceOriginalX.ToString(CultureInfo.InvariantCulture) + "/" +
                        resourceOriginalY.ToString(CultureInfo.InvariantCulture) +
                        " workPix=" + workOriginalX.ToString(CultureInfo.InvariantCulture) + "/" +
                        workOriginalY.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            audit = "no_free_resource_work_slot type=" + resourceId.ToString(CultureInfo.InvariantCulture) +
                    " candidates=" + candidates.Count.ToString(CultureInfo.InvariantCulture);
            return false;
        }

        public bool C2OriginalResourceMapV1TryReserveWorkPointForTargetLikeOriginal(
            int resourceOriginalX,
            int resourceOriginalY,
            C2NeutralPeasantUnitInfoV2LikeOriginal worker,
            out int workOriginalX,
            out int workOriginalY,
            out string audit)
        {
            workOriginalX = resourceOriginalX;
            workOriginalY = resourceOriginalY;
            audit = "resource_not_found";
            if (worker == null) return false;
            for (int i = 0; i < _c2OriginalResourcesV1.Count; i++)
            {
                C2OriginalResourceSpriteV1 s = _c2OriginalResourcesV1[i];
                if (s == null || !s.Enabled || s.X != resourceOriginalX || s.Y != resourceOriginalY) continue;
                ReleaseOriginalResourceWorkSlotV1LikeOriginal(worker);
                if (!TryReserveOriginalResourceWorkPositionV1LikeOriginal(
                        i, s, worker, out workOriginalX, out workOriginalY, out int slot))
                {
                    audit = "resource_surrounded";
                    return false;
                }
                audit = "FindPlaceForPeasant slot=" + slot.ToString(CultureInfo.InvariantCulture) +
                        " workPix=" + workOriginalX.ToString(CultureInfo.InvariantCulture) + "/" +
                        workOriginalY.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public void C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal worker)
        {
            ReleaseOriginalResourceWorkSlotV1LikeOriginal(worker);
        }

        public bool C2OriginalResourceMapV1PerformWorkLikeOriginal(
            int resourceOriginalX,
            int resourceOriginalY,
            byte resourceId,
            int effectPercent,
            out int gathered,
            out string audit)
        {
            gathered = 0;
            audit = "resource_not_found";
            if (!C2OriginalResourceMapV1IsReadyLikeOriginal() &&
                !C2OriginalResourceMapV1TryBuildLikeOriginal("perform-work"))
                return false;

            for (int i = 0; i < _c2OriginalResourcesV1.Count; i++)
            {
                C2OriginalResourceSpriteV1 sprite = _c2OriginalResourcesV1[i];
                if (sprite == null || !sprite.Enabled || sprite.Def == null ||
                    sprite.X != resourceOriginalX || sprite.Y != resourceOriginalY ||
                    sprite.Def.Resource != resourceId)
                    continue;

                // MapSprites.cpp::OneSprite::PerformWork(): one completed work
                // animation increments WorkOver once and always returns ResPerWork.
                C2OriginalResourceDefV1 workedDef = sprite.Def;
                sprite.WorkOver++;
                int threshold = (workedDef.WorkAmount * Mathf.Max(0, effectPercent)) / 100;
                bool transformed = threshold <= sprite.WorkOver;
                if (transformed && workedDef.NextWorkDef != null)
                {
                    sprite.Def = workedDef.NextWorkDef;
                    sprite.SpriteIndex = sprite.Def.Frame;
                    sprite.WorkOver = 0;
                    RebuildOriginalResourceBucketsV1LikeOriginal();
                    if (string.Equals(sprite.Sign, "GA", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sprite.Sign, "TS", StringComparison.OrdinalIgnoreCase))
                        BuildOriginalNatureVisualsV1LikeOriginal();
                }

                gathered = Mathf.Max(0, workedDef.ResourcePerWork);
                audit = "OneSprite::PerformWork name='" + workedDef.Name +
                        "' gathered=" + gathered.ToString(CultureInfo.InvariantCulture) +
                        " workOver=" + sprite.WorkOver.ToString(CultureInfo.InvariantCulture) +
                        " threshold=" + threshold.ToString(CultureInfo.InvariantCulture) +
                        " transformed=" + (transformed && workedDef.NextWorkDef != null).ToString() +
                        " next='" + (sprite.Def != null ? sprite.Def.Name : string.Empty) + "'";
                return true;
            }
            return false;
        }

        private bool TryReserveOriginalResourceWorkPositionV1LikeOriginal(
            int spriteIndex,
            C2OriginalResourceSpriteV1 sprite,
            C2NeutralPeasantUnitInfoV2LikeOriginal worker,
            out int workOriginalX,
            out int workOriginalY,
            out int selectedSlot)
        {
            workOriginalX = workOriginalY = 0;
            selectedSlot = -1;
            int radiusCells = Mathf.Max(1, Mathf.Max(1, sprite.Def.WorkRadius) >> 4);
            int treeX = (sprite.X >> 4) - 1;
            int treeY = (sprite.Y >> 4) - 1;
            Vector2Int[] cells =
            {
                new Vector2Int(treeX + radiusCells, treeY),
                new Vector2Int(treeX + radiusCells, treeY + radiusCells),
                new Vector2Int(treeX + radiusCells, treeY - radiusCells),
                new Vector2Int(treeX, treeY - radiusCells),
                new Vector2Int(treeX, treeY + radiusCells),
                new Vector2Int(treeX - radiusCells, treeY + radiusCells),
                new Vector2Int(treeX - radiusCells, treeY),
                new Vector2Int(treeX - radiusCells, treeY - radiusCells)
            };
            float workerPixX = (worker.RealXFloat != 0f ? worker.RealXFloat : worker.RealX) / 16f;
            float workerPixY = (worker.RealYFloat != 0f ? worker.RealYFloat : worker.RealY) / 16f;
            var order = new List<int>(8);
            for (int i = 0; i < 8; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                float adx = (cells[a].x + 1) * 16f - workerPixX, ady = (cells[a].y + 1) * 16f - workerPixY;
                float bdx = (cells[b].x + 1) * 16f - workerPixX, bdy = (cells[b].y + 1) * 16f - workerPixY;
                return (adx * adx + ady * ady).CompareTo(bdx * bdx + bdy * bdy);
            });
            for (int oi = 0; oi < order.Count; oi++)
            {
                int slot = order[oi];
                long key = ((long)spriteIndex << 8) | (uint)slot;
                if (_c2OriginalResourceWorkOwnersV1.TryGetValue(key, out C2NeutralPeasantUnitInfoV2LikeOriginal owner))
                {
                    if (owner != null && !owner.IsDeadLikeOriginal) continue;
                    _c2OriginalResourceWorkOwnersV1.Remove(key);
                }
                int bx = cells[slot].x + 1;
                int by = cells[slot].y + 1;
                float candidateRealX = (bx << 4) * 16f;
                float candidateRealY = (by << 4) * 16f;
                if (C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(candidateRealX, candidateRealY, 1)) continue;
                _c2OriginalResourceWorkOwnersV1[key] = worker;
                _c2OriginalResourceWorkSlotByUnitV1[worker.GetInstanceID()] = key;
                workOriginalX = bx << 4;
                workOriginalY = by << 4;
                selectedSlot = slot;
                return true;
            }
            return false;
        }

        private void ReleaseOriginalResourceWorkSlotV1LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal worker)
        {
            if (worker == null) return;
            int id = worker.GetInstanceID();
            if (!_c2OriginalResourceWorkSlotByUnitV1.TryGetValue(id, out long key)) return;
            _c2OriginalResourceWorkSlotByUnitV1.Remove(id);
            if (_c2OriginalResourceWorkOwnersV1.TryGetValue(key, out C2NeutralPeasantUnitInfoV2LikeOriginal owner) && owner == worker)
                _c2OriginalResourceWorkOwnersV1.Remove(key);
        }

        public void C2BuildRuntimeErasePlacedFoundationAreaLikeOriginal(
            int footprintCellX,
            int footprintCellY,
            IList<Vector2Int> erasePts,
            int eraseRadius,
            string source,
            out string audit)
        {
            int removed = 0;
            if (C2OriginalResourceMapV1IsReadyLikeOriginal() && erasePts != null)
            {
                removed = EraseFoundationResourceCellsV377LikeOriginal(_c2OriginalResourcesV1, footprintCellX, footprintCellY, erasePts);
            }

            if (removed > 0)
            {
                RebuildOriginalResourceBucketsV1LikeOriginal();
                BuildOriginalNatureVisualsV1LikeOriginal();
            }
            audit = "original_EraseTreesInPoint exactCells=true removed=" + removed.ToString(CultureInfo.InvariantCulture) +
                    " cell=" + footprintCellX.ToString(CultureInfo.InvariantCulture) + "/" +
                    footprintCellY.ToString(CultureInfo.InvariantCulture) +
                    " source='" + (source ?? string.Empty) + "'";
        }

        private static int EraseFoundationResourceCellsV377LikeOriginal(
            IList<C2OriginalResourceSpriteV1> resources, int footprintCellX, int footprintCellY, IList<Vector2Int> erasePts)
        {
            if (resources == null || erasePts == null || erasePts.Count == 0) return 0;
            int removed = 0;
            // COSSACKS2/MapSprites.cpp::EraseTreesInPoint compares one 16px cell.
            // BRadius belongs to GetAwayMonstersInArea; it is not a tree-clearing radius.
            var cells = new HashSet<long>();
            for (int i = 0; i < erasePts.Count; i++)
            {
                int x = footprintCellX + erasePts[i].x;
                int y = footprintCellY + erasePts[i].y;
                cells.Add(((long)x << 32) ^ (uint)y);
            }
            for (int n = 0; n < resources.Count; n++)
            {
                C2OriginalResourceSpriteV1 sprite = resources[n];
                if (sprite == null || !sprite.Enabled) continue;
                if (!string.Equals(sprite.Sign, "GA", StringComparison.OrdinalIgnoreCase) &&
                    (sprite.Def == null || sprite.Def.Resource != 0xFE)) continue;
                long cell = ((long)(sprite.X >> 4) << 32) ^ (uint)(sprite.Y >> 4);
                if (!cells.Contains(cell)) continue;
                sprite.Enabled = false;
                removed++;
            }
            return removed;
        }

        private List<C2OriginalResourceDefV1> LoadOriginalResourceCatalogV1LikeOriginal(string listPath, string rsrPath)
        {
            var result = new List<C2OriginalResourceDefV1>();
            if (!_bootstrap.Fs.Exists(listPath)) return result;
            string[] lines = _bootstrap.Fs.ReadAllText(listPath, Encoding.ASCII).Replace("\r", string.Empty).Split('\n');
            int expected = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripOriginalLineCommentV1LikeOriginal(lines[i]);
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (expected < 0)
                {
                    if (p.Length >= 2) int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out expected);
                    continue;
                }
                if (p.Length < 4 || p[0].StartsWith("[", StringComparison.Ordinal)) continue;
                if (!int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cx) ||
                    !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cy) ||
                    !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius)) continue;
                result.Add(new C2OriginalResourceDefV1
                {
                    Name = p[0], CenterX = cx, CenterY = cy, Radius = radius, Frame = result.Count
                });
                if (expected > 0 && result.Count >= expected) break;
            }

            if (_bootstrap.Fs.Exists(rsrPath))
            {
                var byName = new Dictionary<string, C2OriginalResourceDefV1>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < result.Count; i++) byName[result[i].Name] = result[i];
                string section = string.Empty;
                lines = _bootstrap.Fs.ReadAllText(rsrPath, Encoding.ASCII).Replace("\r", string.Empty).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = StripOriginalLineCommentV1LikeOriginal(lines[i]);
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("[", StringComparison.Ordinal))
                    {
                        section = line.Trim();
                        continue;
                    }
                    string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (string.Equals(section, "[SOURCES]", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Length < 4 || !byName.TryGetValue(p[0], out C2OriginalResourceDefV1 def)) continue;
                        def.Resource = ParseOriginalResourceIdV1LikeOriginal(p[1]);
                        int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out def.ResourcePerWork);
                        int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out def.WorkRadius);
                    }
                    else if (string.Equals(section, "[WORKTRANSFORM]", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Length < 3 || !byName.TryGetValue(p[0], out C2OriginalResourceDefV1 def)) continue;
                        def.NextWorkName = p[1];
                        int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out def.WorkAmount);
                    }
                }

                for (int i = 0; i < result.Count; i++)
                {
                    C2OriginalResourceDefV1 def = result[i];
                    if (!string.IsNullOrEmpty(def.NextWorkName))
                        byName.TryGetValue(def.NextWorkName, out def.NextWorkDef);
                }
            }
            return result;
        }

        private static string StripOriginalLineCommentV1LikeOriginal(string raw)
        {
            string s = (raw ?? string.Empty).Trim();
            if (s.StartsWith("//", StringComparison.Ordinal) || s.StartsWith("/", StringComparison.Ordinal)) return string.Empty;
            int comment = s.IndexOf("//", StringComparison.Ordinal);
            return (comment >= 0 ? s.Substring(0, comment) : s).Trim();
        }

        private static byte ParseOriginalResourceIdV1LikeOriginal(string name)
        {
            switch ((name ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "WOOD": return 0;
                case "GOLD": return 1;
                case "STONE": return 2;
                case "FOOD": return 3;
                case "IRON": return 4;
                case "COAL": return 5;
                case "NONE": return 0xFE;
                case "REMOVE": return 0xFE;
                default: return C2OriginalResourceEmptyV1LikeOriginal;
            }
        }

        private void ParseOriginalResourceSpritesFromMapV1LikeOriginal(
            List<C2OriginalResourceDefV1> trees,
            List<C2OriginalResourceDefV1> stones,
            List<C2OriginalResourceDefV1> complex)
        {
            byte[] raw = _bootstrap.Fs.ReadAllBytes(_mapRelativePath);
            byte[] data = MaybeDecompressM3d(raw, out string error);
            if (data == null || data.Length < 20) throw new InvalidDataException("map decompress failed: " + error);
            using (var ms = new MemoryStream(data, false))
            using (var br = new BinaryReader(ms))
            {
                string magic = ReadTag(br);
                if (!TryGetAddshFromMapMagic(magic, out _)) throw new InvalidDataException("unsupported map magic '" + magic + "'");
                br.ReadInt32();
                br.ReadInt32();
                while (ms.Position + 8 <= ms.Length)
                {
                    string tag = ReadTag(br);
                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal)) break;
                    int sizeField = br.ReadInt32();
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;
                    long payloadEnd = payloadStart + payloadLen;
                    if (payloadEnd > ms.Length) break;
                    bool tre2 = string.Equals(tag, "2ERT", StringComparison.Ordinal) || string.Equals(tag, "TRE2", StringComparison.Ordinal);
                    bool tre1 = string.Equals(tag, "1ERT", StringComparison.Ordinal) || string.Equals(tag, "TRE1", StringComparison.Ordinal);
                    bool tree = string.Equals(tag, "EERT", StringComparison.Ordinal) || string.Equals(tag, "TREE", StringComparison.Ordinal);
                    if ((tre2 || tre1 || tree) && payloadLen >= 4)
                    {
                        int count = br.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            int minimum = tre2 ? 15 : (tre1 ? 14 : 12);
                            if (br.BaseStream.Position + minimum > payloadEnd) break;
                            string sign = NormalizeSpriteGroupSignV6LikeOriginal(br.ReadUInt16());
                            int x = br.ReadInt32();
                            int y = br.ReadInt32();
                            int spriteIndex = br.ReadUInt16();
                            int nindPacked = (tre2 || tre1) ? br.ReadUInt16() : 0;
                            bool hasMatrix = tre2 && br.ReadByte() != 0;
                            if (hasMatrix && br.BaseStream.Position + 64 <= payloadEnd) br.BaseStream.Position += 64;
                            List<C2OriginalResourceDefV1> defs =
                                string.Equals(sign, "GA", StringComparison.OrdinalIgnoreCase) ? trees :
                                string.Equals(sign, "TS", StringComparison.OrdinalIgnoreCase) ? stones :
                                string.Equals(sign, "OC", StringComparison.OrdinalIgnoreCase) ? complex : null;
                            if (defs == null || spriteIndex < 0 || spriteIndex >= defs.Count) continue;
                            _c2OriginalResourcesV1.Add(new C2OriginalResourceSpriteV1
                            {
                                Sign = sign, X = x, Y = y, SpriteIndex = spriteIndex,
                                NIndex = nindPacked & 4095, Locking = nindPacked >> 12, Def = defs[spriteIndex]
                            });
                        }
                    }
                    ms.Position = payloadEnd;
                }
            }
        }

        private void RebuildOriginalResourceBucketsV1LikeOriginal()
        {
            _c2OriginalResourceBucketsV1.Clear();
            _c2OriginalResourceWorkOwnersV1.Clear();
            _c2OriginalResourceWorkSlotByUnitV1.Clear();
            for (int i = 0; i < _c2OriginalResourcesV1.Count; i++)
            {
                C2OriginalResourceSpriteV1 s = _c2OriginalResourcesV1[i];
                if (s == null || !s.Enabled || s.Def == null || s.Def.Resource >= 0xFE) continue;
                long key = PackCellV1LikeOriginal(s.X >> 7, s.Y >> 7);
                if (!_c2OriginalResourceBucketsV1.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>();
                    _c2OriginalResourceBucketsV1[key] = list;
                }
                list.Add(i);
            }
        }

        private void BuildOriginalNatureVisualsV1LikeOriginal()
        {
            if (_c2OriginalNatureRootV1 != null) SafeDestroy(_c2OriginalNatureRootV1);
            _c2OriginalNatureRootV1 = new GameObject("C2_OriginalNature_TRE2");
            _c2OriginalNatureRootV1.transform.SetParent(_terrainRoot.transform, false);
            _c2OriginalNatureRootV1.transform.localPosition = Vector3.zero;

            var groups = new Dictionary<string, C2OriginalNatureMeshGroupV1>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _c2OriginalResourcesV1.Count; i++)
            {
                C2OriginalResourceSpriteV1 s = _c2OriginalResourcesV1[i];
                if (s == null || !s.Enabled || s.Def == null ||
                    (!string.Equals(s.Sign, "GA", StringComparison.OrdinalIgnoreCase) && !string.Equals(s.Sign, "TS", StringComparison.OrdinalIgnoreCase))) continue;
                int bx = s.X >> 9;
                int by = s.Y >> 9;
                string key = s.Sign + ":" + s.Def.Frame.ToString(CultureInfo.InvariantCulture) + ":" + bx.ToString(CultureInfo.InvariantCulture) + ":" + by.ToString(CultureInfo.InvariantCulture);
                if (!groups.TryGetValue(key, out C2OriginalNatureMeshGroupV1 g))
                {
                    g = new C2OriginalNatureMeshGroupV1 { Sign = s.Sign, Frame = s.Def.Frame, BucketX = bx, BucketY = by };
                    groups[key] = g;
                }
                g.Sprites.Add(s);
            }

            foreach (C2OriginalNatureMeshGroupV1 group in groups.Values)
            {
                C2OriginalNatureFrameV1 frame = GetOriginalNatureFrameV1LikeOriginal(group.Sign, group.Frame, group.Sprites[0].Def);
                if (frame == null || frame.Texture == null) continue;
                BuildOriginalNatureMeshGroupV1LikeOriginal(group, frame);
            }
            C2SpriteDepthLayerLikeOriginal.SetLayerRecursive(_c2OriginalNatureRootV1);
        }

        private C2OriginalNatureFrameV1 GetOriginalNatureFrameV1LikeOriginal(string sign, int frameIndex, C2OriginalResourceDefV1 def)
        {
            string key = sign + ":" + frameIndex.ToString(CultureInfo.InvariantCulture);
            if (_c2OriginalNatureFramesV1.TryGetValue(key, out C2OriginalNatureFrameV1 cached) && cached != null) return cached;
            string bankPath = string.Equals(sign, "GA", StringComparison.OrdinalIgnoreCase) ? "Trees.g2d" : "Cash\\STONES.g16";
            if (!_bootstrap.Fs.Exists(bankPath)) return null;
            var bank = new C2DirectSpriteBank();
            if (!bank.Load(_bootstrap.Fs.ResolvePath(bankPath), out string loadError) ||
                !bank.RenderFrame(frameIndex, out C2RenderedFrame rendered, out string renderError) || rendered == null ||
                rendered.Width <= 0 || rendered.Height <= 0 || rendered.Rgba == null) return null;

            byte[] flipped = new byte[rendered.Rgba.Length];
            int row = rendered.Width * 4;
            for (int y = 0; y < rendered.Height; y++)
                Buffer.BlockCopy(rendered.Rgba, y * row, flipped, (rendered.Height - 1 - y) * row, row);
            Texture2D tex = new Texture2D(rendered.Width, rendered.Height, TextureFormat.RGBA32, false, false);
            tex.name = "C2Nature_" + sign + "_" + frameIndex.ToString(CultureInfo.InvariantCulture);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixelData(flipped, 0);
            tex.Apply(false, true);

            Shader shader = Shader.Find("Cossacks2Bridge/C2UnitSpriteV56SelectionDiffuseLikeOriginal");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            Material color = new Material(shader);
            color.name = tex.name + "_Color";
            color.mainTexture = tex;
            color.SetFloat("_C2CameraFacing", 1f);
            color.renderQueue = 3660;
            if (color.HasProperty("_MainTex")) color.SetTexture("_MainTex", tex);
            if (color.HasProperty("_BaseMap")) color.SetTexture("_BaseMap", tex);
            if (color.HasProperty("_Color")) color.SetColor("_Color", Color.white);
            if (color.HasProperty("_BaseColor")) color.SetColor("_BaseColor", Color.white);
            if (color.HasProperty("_Cull")) color.SetInt("_Cull", 0);
            if (color.HasProperty("_ZWrite")) color.SetInt("_ZWrite", 0);
            if (color.HasProperty("_ZTest")) color.SetInt("_ZTest", (int)CompareFunction.LessEqual);

            var value = new C2OriginalNatureFrameV1
            {
                Texture = tex,
                ColorMaterial = color,
                DepthMaterial = C2SpriteDepthPrepassLikeOriginal.CreateDepthMaterialLikeOriginal(tex.name + "_Depth", tex, 0.12f),
                Width = rendered.Width,
                Height = rendered.Height,
                // DrawWorldPoint subtracts the catalog center; rasterization also
                // translated the raw G2D vertices by Origin. These offsets add,
                // including negative crop origins, rather than replacing each other.
                PivotX = rendered.OriginX + def.CenterX,
                PivotY = rendered.OriginY + def.CenterY
            };
            if (value.DepthMaterial != null) value.DepthMaterial.SetFloat("_C2CameraFacing", 1f);
            _c2OriginalNatureFramesV1[key] = value;
            return value;
        }

        private void BuildOriginalNatureMeshGroupV1LikeOriginal(C2OriginalNatureMeshGroupV1 group, C2OriginalNatureFrameV1 frame)
        {
            int count = group.Sprites.Count;
            var vertices = new Vector3[count * 4];
            var uv = new Vector2[count * 4];
            var spriteOffsets = new Vector2[count * 4];
            var triangles = new int[count * 6];
            float mapScale = C2MapPixelToWorldScaleV277LikeOriginal();
            float scale = C2OriginalNativeVisualPixelToWorldScaleV277LikeOriginal(mapScale);
            for (int i = 0; i < count; i++)
            {
                C2OriginalResourceSpriteV1 s = group.Sprites[i];
                Vector3 anchor = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(s.X, s.Y);
                float x0 = -frame.PivotX * scale;
                float x1 = (frame.Width - frame.PivotX) * scale;
                float yTop = frame.PivotY * scale;
                float yBottom = (frame.PivotY - frame.Height) * scale;
                int v = i * 4;
                vertices[v + 0] = anchor + new Vector3(x0, yBottom, 0f);
                vertices[v + 1] = anchor + new Vector3(x1, yBottom, 0f);
                vertices[v + 2] = anchor + new Vector3(x1, yTop, 0f);
                vertices[v + 3] = anchor + new Vector3(x0, yTop, 0f);
                spriteOffsets[v + 0] = new Vector2(x0, yBottom);
                spriteOffsets[v + 1] = new Vector2(x1, yBottom);
                spriteOffsets[v + 2] = new Vector2(x1, yTop);
                spriteOffsets[v + 3] = new Vector2(x0, yTop);
                uv[v + 0] = new Vector2(0f, 0f);
                uv[v + 1] = new Vector2(1f, 0f);
                uv[v + 2] = new Vector2(1f, 1f);
                uv[v + 3] = new Vector2(0f, 1f);
                int t = i * 6;
                triangles[t + 0] = v + 0; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 0; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }

            Mesh mesh = new Mesh();
            mesh.name = "C2NatureMesh_" + group.Sign + "_" + group.Frame.ToString(CultureInfo.InvariantCulture) + "_" + group.BucketX + "_" + group.BucketY;
            if (vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.uv2 = spriteOffsets;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            // Shader rotation can move corners out of the original vertical plane.
            // Conservative bounds cover every camera orientation without CPU rebuilds.
            float radius = new Vector2(Mathf.Max(Mathf.Abs(frame.PivotX), Mathf.Abs(frame.Width - frame.PivotX)),
                Mathf.Max(Mathf.Abs(frame.PivotY), Mathf.Abs(frame.Height - frame.PivotY))).magnitude * scale;
            Bounds billboardBounds = mesh.bounds;
            billboardBounds.Expand(radius * 2f);
            mesh.bounds = billboardBounds;
            mesh.UploadMeshData(true);

            GameObject go = new GameObject(mesh.name);
            go.transform.SetParent(_c2OriginalNatureRootV1.transform, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = frame.ColorMaterial;
            mr.sortingOrder = 5900 + Mathf.Clamp(group.BucketY, -500, 500);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            C2SpriteDepthPrepassLikeOriginal.AddDepthRendererLikeOriginal(go, mesh, frame.DepthMaterial, mr.sortingOrder, "depth_cutout");
        }

        private void ClearOriginalResourceMapV1LikeOriginal(bool destroyFrames)
        {
            if (_c2OriginalNatureRootV1 != null) SafeDestroy(_c2OriginalNatureRootV1);
            _c2OriginalNatureRootV1 = null;
            _c2OriginalResourcesV1.Clear();
            _c2OriginalResourceBucketsV1.Clear();
            s_c2OriginalNatureBlockedCellsV1.Clear();
            _c2OriginalResourceReadyV1 = false;
            _c2OriginalResourceMapPathV1 = string.Empty;
            if (!destroyFrames) return;
            foreach (C2OriginalNatureFrameV1 f in _c2OriginalNatureFramesV1.Values)
            {
                if (f == null) continue;
                if (f.ColorMaterial != null) Destroy(f.ColorMaterial);
                if (f.DepthMaterial != null) Destroy(f.DepthMaterial);
                if (f.Texture != null) Destroy(f.Texture);
            }
            _c2OriginalNatureFramesV1.Clear();
        }

        private static long PackCellV1LikeOriginal(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
    }
}
