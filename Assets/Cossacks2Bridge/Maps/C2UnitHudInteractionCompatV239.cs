// C2UnitHudInteractionCompatV239.cs
// V245: compatibility bridge for unit HUD/interaction + runtime construction.
// IMPORTANT: the old C2BuildingPlacementPreviewV27 stub is removed here;
// the real preview lives in C2BuildingPlacementPreviewV27.cs.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        // Original map resources live in C2OriginalResourceMapV1LikeOriginal.cs.
        // They used to be hard-disabled compatibility stubs here, which made all
        // saved TRE2 trees/stones disappear and made resource orders impossible.
    }

    public sealed class C2RuntimeConstructionSiteProxyLikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public C2SettlementBuildingSelectableV1LikeOriginal Building;
        public bool IsSelected;

        public string MdName = string.Empty;
        public string UnitId = string.Empty;
        public int Nation;
        public int RealX;
        public int RealY;
        public int BuildStages;
        public int Stage;
        public int LifeMax = 1;
        public int Life = 1;
        public bool Ready;
        public bool Dead;
        public bool NotSelectable;

        public bool CanAcceptBuildersLikeOriginal
        {
            get { return !Ready && !Dead && !NotSelectable; }
        }

        public void Configure(
            C2BattleTerrainMode owner,
            string mdName,
            string unitId,
            int nation,
            int realX,
            int realY,
            int buildStages,
            int stage,
            bool ready,
            bool dead,
            bool notSelectable,
            int lifeMax = 1,
            int life = 1)
        {
            OwnerMode = owner;
            MdName = mdName ?? string.Empty;
            UnitId = unitId ?? string.Empty;
            Nation = nation;
            RealX = realX;
            RealY = realY;
            BuildStages = buildStages;
            Stage = stage;
            LifeMax = Mathf.Max(1, lifeMax);
            Life = Mathf.Clamp(life, 0, LifeMax);
            Ready = ready;
            Dead = dead;
            NotSelectable = notSelectable;

            if (Building == null)
                Building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (Building != null)
            {
                Building.OwnerMode = owner;
                Building.SourceMonsterId = string.IsNullOrEmpty(UnitId) ? MdName : UnitId;
                Building.KindName = "RuntimeConstruction";
                Building.Nation = nation;
                Building.RealX = realX;
                Building.RealY = realY;
                Building.LifeMaxLikeOriginal = LifeMax;
                Building.LifeLikeOriginal = Life;
                Building.StageMaxLikeOriginal = Mathf.Max(1, buildStages);
                Building.StageLikeOriginal = Mathf.Clamp(stage, 0, Mathf.Max(1, buildStages));
                Building.ReadyLikeOriginal = ready;
                Building.NotSelectable = notSelectable;
                Building.RecordIndex = GetEntityId().GetHashCode();
                Building.SortKey = 6000 + (realY >> 4);
            }
        }

        public int AssignSelectedBuildersLikeOriginal(string source, out string audit)
        {
            audit = "not_started";
            if (OwnerMode == null)
            {
                audit = "no_owner_mode";
                return 0;
            }

            if (!CanAcceptBuildersLikeOriginal)
            {
                audit = "not_accepting_builders ready=" + Ready + " dead=" + Dead + " notSelectable=" + NotSelectable;
                return 0;
            }

            return OwnerMode.C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(
                gameObject,
                RealX,
                RealY,
                null,
                source ?? "right_click_mend",
                out audit);
        }
    }

    public static class C2FormationCreateCatalogV165LikeOriginal
    {
        public sealed class C2FormationPointV165LikeOriginal
        {
            public int RawX;
            public int RawY;
            public int X;
            public int Y;
            public int LineIndex;
            public int Option;
        }

        public sealed class C2FormationOrderTemplateV165LikeOriginal
        {
            public string OrderId = string.Empty;
            public string Symmetry = string.Empty;
            public int[] SymInv;
            public int[] Sym4f;
            public int[] Sym4i;
            public string SymmetryAudit = "not_loaded";
            public int AddDamage1;
            public int AddDamage2;
            public int AddShield1;
            public int AddShield2;
            public int FlagAddDamage;
            public int FlagAddShield;
            public int StandGroundBonus = 100;
            public int UnitCount;
            public int CommandCount;
            public int LineCount;
            public int FirstActualLine;
            public int ActualLineCount;
            public int YShift;
            public int BarX0;
            public int BarX1;
            public int BarY0;
            public int BarY1;
            public int Usage;
            public string GroupKey = string.Empty;
            public readonly List<C2FormationPointV165LikeOriginal> SoldierPoints = new List<C2FormationPointV165LikeOriginal>();
            public readonly List<C2FormationPointV165LikeOriginal> CommandPoints = new List<C2FormationPointV165LikeOriginal>();
            public string Source = string.Empty;
        }

        public sealed class C2FormationOrderGroupV165LikeOriginal
        {
            public string Shape = string.Empty;
            public readonly List<int> CommonButtonIds = new List<int>();
            public readonly List<string> OrderIds = new List<string>();
            public string Source = string.Empty;
        }

        public sealed class C2FormationRecordV165LikeOriginal
        {
            public string Key = string.Empty;
            public string UnitId = string.Empty;
            public string MdName = string.Empty;
            public string NationSuffix = string.Empty;
            public string OfficerId = string.Empty;
            public string DrummerId = string.Empty;
            public string FlagId = string.Empty;
            public string Source = string.Empty;
            public readonly List<C2FormationOptionV165LikeOriginal> Options = new List<C2FormationOptionV165LikeOriginal>();
        }

        public sealed class C2FormationOptionV165LikeOriginal
        {
            public string Key = string.Empty;
            public string Shape = string.Empty;
            public string Title = string.Empty;
            public int UnitCount;
            public int IconSprite;
            public string OrderId = string.Empty;
            public int AmountIndex;
            public C2FormationOrderTemplateV165LikeOriginal OrderTemplate;
            public string Source = string.Empty;
        }

        private sealed class C2FormationOfficerBlockV165LikeOriginal
        {
            public string OfficerId = string.Empty;
            public string DrummerId = string.Empty;
            public string FlagId = string.Empty;
            public int ShapeCount;
            public string Source = string.Empty;
        }

        private static bool _loaded;
        private static string _audit = "not_loaded";
        private static readonly Dictionary<string, string> _memberToMd = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdToMember = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _mdNationToMember = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2FormationRecordV165LikeOriginal> _recordsByUnitId = new Dictionary<string, C2FormationRecordV165LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2FormationOrderTemplateV165LikeOriginal> _ordersById = new Dictionary<string, C2FormationOrderTemplateV165LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2FormationOrderGroupV165LikeOriginal> _groupsByShape = new Dictionary<string, C2FormationOrderGroupV165LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static int _resolveAuditLogs;

        public static string AuditLikeOriginal
        {
            get
            {
                EnsureLoaded();
                return _audit;
            }
        }

        public static void ForceReloadLikeOriginal()
        {
            _loaded = false;
            _audit = "not_loaded";
            _memberToMd.Clear();
            _mdToMember.Clear();
            _mdNationToMember.Clear();
            _recordsByUnitId.Clear();
            _ordersById.Clear();
            _groupsByShape.Clear();
            _resolveAuditLogs = 0;
        }

        public static bool TryResolveForSelectedUnit(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out C2FormationRecordV165LikeOriginal record)
        {
            record = null;
            EnsureLoaded();
            if (unit == null) return false;

            string memberId = ResolveMemberIdForSelectedUnit(unit);
            bool ok = !string.IsNullOrEmpty(memberId) &&
                      _recordsByUnitId.TryGetValue(memberId, out record) &&
                      record != null &&
                      record.Options.Count > 0;
            LogResolveAuditLikeOriginal(unit, memberId, ok, record);
            return ok;
        }

        public static string ResolveMemberIdForSelectedUnitLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            EnsureLoaded();
            return ResolveMemberIdForSelectedUnit(unit);
        }

        public static bool TryResolveForUnitMemberIdLikeOriginal(
            string unitId,
            int nation,
            out C2FormationRecordV165LikeOriginal record)
        {
            record = null;
            EnsureLoaded();

            string memberId = ResolveMemberIdForRawUnitLikeOriginal(unitId, nation);
            if (string.IsNullOrEmpty(memberId)) return false;
            return _recordsByUnitId.TryGetValue(memberId, out record) && record != null && record.Options.Count > 0;
        }

        public static bool TryGetOrderTemplateLikeOriginal(string orderId, out C2FormationOrderTemplateV165LikeOriginal template)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(orderId))
            {
                template = null;
                return false;
            }
            return _ordersById.TryGetValue(orderId.Trim(), out template);
        }

        public static bool TryBuildSiblingFormationOptionLikeOriginal(
            C2FormationOptionV165LikeOriginal source,
            string targetShape,
            out C2FormationOptionV165LikeOriginal option)
        {
            option = null;
            EnsureLoaded();
            if (source == null || string.IsNullOrWhiteSpace(targetShape))
                return false;

            C2FormationOrderGroupV165LikeOriginal group;
            if (!_groupsByShape.TryGetValue(targetShape.Trim(), out group) ||
                group == null ||
                group.OrderIds.Count == 0)
                return false;

            int amountIndex = Mathf.Clamp(source.AmountIndex, 0, group.OrderIds.Count - 1);
            string orderId = group.OrderIds[amountIndex];
            C2FormationOrderTemplateV165LikeOriginal order;
            if (!_ordersById.TryGetValue(orderId, out order) || order == null)
                return false;

            option = new C2FormationOptionV165LikeOriginal();
            option.Shape = group.Shape;
            option.OrderId = orderId;
            option.AmountIndex = amountIndex;
            option.OrderTemplate = order;
            option.UnitCount = order.UnitCount;
            option.IconSprite = IconSpriteForShapeLikeOriginal(group.Shape);
            option.Title = TitleForShapeLikeOriginal(group.Shape);
            option.Key = (source.Key ?? string.Empty) + "|sibling|" + group.Shape;
            option.Source = group.Source + "+original_common_buttons";
            return true;
        }

        public static List<C2FormationRecordV165LikeOriginal> BuildAllRecordsSnapshotLikeOriginal()
        {
            EnsureLoaded();
            return new List<C2FormationRecordV165LikeOriginal>(_recordsByUnitId.Values);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            int ndsFiles = 0;
            int members = 0;
            int officerBlocks = 0;
            int options = 0;

            string ordersPath = FindFirstDataFileLikeOriginal("orders.lst");
            int orders = ParseOrdersFileLikeOriginal(ordersPath);

            string groupsPath = FindFirstDataFileLikeOriginal("ord_groups.lst");
            int groups = ParseOrderGroupsFileLikeOriginal(groupsPath);
            ApplyGroupsToOrdersLikeOriginal();

            List<string> files = FindNdsFilesLikeOriginal();
            ndsFiles = files.Count;
            for (int i = 0; i < files.Count; i++)
                ParseNdsFileLikeOriginal(files[i], ref members, ref officerBlocks, ref options);

            _audit = "formationCatalog ndsFiles=" + ndsFiles.ToString(CultureInfo.InvariantCulture) +
                     " members=" + members.ToString(CultureInfo.InvariantCulture) +
                     " officerBlocks=" + officerBlocks.ToString(CultureInfo.InvariantCulture) +
                     " records=" + _recordsByUnitId.Count.ToString(CultureInfo.InvariantCulture) +
                     " options=" + options.ToString(CultureInfo.InvariantCulture) +
                     " orders=" + orders.ToString(CultureInfo.InvariantCulture) +
                     " groups=" + groups.ToString(CultureInfo.InvariantCulture) +
                     " ordersPath='" + (ordersPath ?? string.Empty) + "'" +
                     " groupsPath='" + (groupsPath ?? string.Empty) + "'";
            Debug.Log("[C2:FORMATION CATALOG V165] " + _audit);
        }

        private static void LogResolveAuditLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string memberId,
            bool ok,
            C2FormationRecordV165LikeOriginal record)
        {
            if (_resolveAuditLogs >= 16) return;
            _resolveAuditLogs++;
            Debug.Log("[C2:FORMATION RESOLVE V165] unit='" + (unit != null ? unit.SourceMonsterId : string.Empty) +
                      "' md='" + (unit != null ? unit.ResolvedMd : string.Empty) +
                      "' member='" + (memberId ?? string.Empty) +
                      "' ok=" + ok +
                      " options=" + (record != null ? record.Options.Count.ToString(CultureInfo.InvariantCulture) : "0") +
                      " audit='" + _audit + "'");
        }

        private static int ParseOrdersFileLikeOriginal(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            string[] lines = ReadAllLines1251LikeOriginal(path);
            if (lines == null) return 0;

            C2FormationOrderTemplateV165LikeOriginal current = null;
            int parsed = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i] ?? string.Empty;
                string trim = raw.Trim();
                if (trim.StartsWith("//", StringComparison.Ordinal)) continue;
                if (trim.StartsWith("/", StringComparison.Ordinal)) continue;

                if (trim.StartsWith("#", StringComparison.Ordinal))
                {
                    if (string.Equals(trim, "#EXIT", StringComparison.OrdinalIgnoreCase))
                        break;
                    if (string.Equals(trim, "#END", StringComparison.OrdinalIgnoreCase))
                    {
                        if (current != null)
                        {
                            FinalizeOrderTemplateLikeOriginal(current);
                            if (!_ordersById.ContainsKey(current.OrderId))
                            {
                                _ordersById.Add(current.OrderId, current);
                                parsed++;
                            }
                            current = null;
                        }
                        continue;
                    }

                    current = ParseOrderHeaderLikeOriginal(trim, path);
                    continue;
                }

                if (current == null) continue;
                ParseOrderPatternLineLikeOriginal(current, raw);
            }

            return parsed;
        }

        private static C2FormationOrderTemplateV165LikeOriginal ParseOrderHeaderLikeOriginal(string line, string path)
        {
            string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
            var result = new C2FormationOrderTemplateV165LikeOriginal();
            result.Source = Path.GetFileName(path) + ":orders.lst";
            if (t.Length > 0) result.OrderId = t[0];
            if (t.Length > 1) result.Symmetry = t[1];
            if (t.Length > 2) int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out result.AddDamage1);
            if (t.Length > 3) int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out result.AddDamage2);
            if (t.Length > 4) int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out result.AddShield1);
            if (t.Length > 5) int.TryParse(t[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out result.AddShield2);
            if (t.Length > 6) int.TryParse(t[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out result.FlagAddDamage);
            if (t.Length > 7) int.TryParse(t[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out result.FlagAddShield);
            // Nature.cpp initializes SG_bonus=100, then overwrites it only when
            // the ninth headline value is actually present. An explicit 0 is valid
            // and must not be silently converted back to 100.
            result.StandGroundBonus = 100;
            if (t.Length > 8)
            {
                int standGroundBonusV403LikeOriginal;
                if (int.TryParse(t[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out standGroundBonusV403LikeOriginal))
                    result.StandGroundBonus = standGroundBonusV403LikeOriginal;
            }
            if (result.OrderId.IndexOf("#SQUARE", StringComparison.OrdinalIgnoreCase) >= 0) result.Usage = 1;
            else if (result.OrderId.IndexOf("#KARE", StringComparison.OrdinalIgnoreCase) >= 0) result.Usage = 2;
            else if (result.OrderId.IndexOf("#MOV", StringComparison.OrdinalIgnoreCase) >= 0) result.Usage = 1;
            return result;
        }

        private static void ParseOrderPatternLineLikeOriginal(C2FormationOrderTemplateV165LikeOriginal order, string rawLine)
        {
            int lineIndex = order.LineCount;
            order.LineCount++;
            if (rawLine == null) rawLine = string.Empty;
            string trim = rawLine.TrimStart();
            if (trim.StartsWith("/", StringComparison.Ordinal)) return;

            for (int p = 0; p < rawLine.Length; p++)
            {
                char c = rawLine[p];
                if (c == '*' || c == '@' || c == 'F')
                {
                    var pt = new C2FormationPointV165LikeOriginal();
                    pt.RawX = p * 2;
                    pt.RawY = lineIndex * 2;
                    pt.X = pt.RawX;
                    pt.Y = pt.RawY;
                    pt.LineIndex = lineIndex;
                    pt.Option = c == '@' ? 1 : (c == 'F' ? 2 : 0);
                    order.SoldierPoints.Add(pt);
                }
                else if (c == 'C')
                {
                    var pt = new C2FormationPointV165LikeOriginal();
                    pt.RawX = p * 2;
                    pt.RawY = lineIndex * 2;
                    pt.X = pt.RawX;
                    pt.Y = pt.RawY;
                    pt.LineIndex = lineIndex;
                    order.CommandPoints.Add(pt);
                }
            }
        }

        private static void FinalizeOrderTemplateLikeOriginal(C2FormationOrderTemplateV165LikeOriginal order)
        {
            int count = order.SoldierPoints.Count;
            order.UnitCount = count;
            order.CommandCount = order.CommandPoints.Count;
            if (count <= 0) return;

            int xs = 0;
            int ys = 0;
            int xmin = int.MaxValue;
            int xmax = int.MinValue;
            int ymin = int.MaxValue;
            int ymax = int.MinValue;
            for (int i = 0; i < order.SoldierPoints.Count; i++)
            {
                C2FormationPointV165LikeOriginal pt = order.SoldierPoints[i];
                xs += pt.RawX;
                ys += pt.RawY;
                if (pt.RawX < xmin) xmin = pt.RawX;
                if (pt.RawX > xmax) xmax = pt.RawX;
                if (pt.RawY < ymin) ymin = pt.RawY;
                if (pt.RawY > ymax) ymax = pt.RawY;
            }

            xs /= count;
            ys /= count;
            order.YShift = ys;
            order.BarX0 = xmin - xs - 1;
            order.BarX1 = xmax - xs + 1;
            order.BarY0 = ymin - ys - 1;
            order.BarY1 = ymax - ys + 1;

            for (int i = 0; i < order.SoldierPoints.Count; i++)
            {
                C2FormationPointV165LikeOriginal pt = order.SoldierPoints[i];
                pt.X = pt.RawX - xs;
                pt.Y = pt.RawY - ys;
            }

            for (int i = 0; i < order.CommandPoints.Count; i++)
            {
                C2FormationPointV165LikeOriginal pt = order.CommandPoints[i];
                pt.X = pt.RawX - xs;
                pt.Y = pt.RawY - ys;
            }

            int first = -1;
            int last = -1;
            for (int i = 0; i < order.SoldierPoints.Count; i++)
            {
                int line = order.SoldierPoints[i].LineIndex;
                if (first < 0 || line < first) first = line;
                if (line > last) last = line;
            }
            order.FirstActualLine = first < 0 ? 0 : first;
            order.ActualLineCount = first < 0 ? 0 : (last - first + 1);
            C2FormationSymmetryLikeOriginal.Build(order);
        }

        private static int ParseOrderGroupsFileLikeOriginal(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            string[] lines = ReadAllLines1251LikeOriginal(path);
            if (lines == null) return 0;

            int parsed = 0;
            bool skippedCount = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(lines[i]);
                if (line.Length == 0) continue;
                string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
                if (t.Length == 0) continue;

                if (!skippedCount)
                {
                    int dummy;
                    if (t.Length == 1 && int.TryParse(t[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out dummy))
                    {
                        skippedCount = true;
                        continue;
                    }
                    skippedCount = true;
                }

                int nCommon;
                if (t.Length < 3 || !int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out nCommon))
                    continue;

                var group = new C2FormationOrderGroupV165LikeOriginal();
                group.Shape = t[0];
                group.Source = Path.GetFileName(path) + ":ord_groups.lst";
                int idx = 2;
                for (int k = 0; k < nCommon && idx < t.Length; k++, idx++)
                {
                    int button;
                    if (int.TryParse(t[idx], NumberStyles.Integer, CultureInfo.InvariantCulture, out button))
                        group.CommonButtonIds.Add(button);
                }

                int nForms;
                if (idx >= t.Length || !int.TryParse(t[idx], NumberStyles.Integer, CultureInfo.InvariantCulture, out nForms))
                    continue;
                idx++;

                for (int k = 0; k < nForms && idx < t.Length; k++, idx++)
                    group.OrderIds.Add(t[idx]);

                if (!_groupsByShape.ContainsKey(group.Shape))
                {
                    _groupsByShape.Add(group.Shape, group);
                    parsed++;
                }
            }

            return parsed;
        }

        private static void ApplyGroupsToOrdersLikeOriginal()
        {
            foreach (KeyValuePair<string, C2FormationOrderGroupV165LikeOriginal> kv in _groupsByShape)
            {
                C2FormationOrderGroupV165LikeOriginal group = kv.Value;
                for (int i = 0; i < group.OrderIds.Count; i++)
                {
                    C2FormationOrderTemplateV165LikeOriginal order;
                    if (_ordersById.TryGetValue(group.OrderIds[i], out order))
                        order.GroupKey = group.Shape;
                }
            }
        }

        private static void ParseNdsFileLikeOriginal(string path, ref int members, ref int officerBlocks, ref int options)
        {
            string[] lines = ReadAllLines1251LikeOriginal(path);
            if (lines == null) return;

            string section = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(lines[i]);
                if (line.Length == 0) continue;
                if (line[0] == '[')
                {
                    section = line.ToUpperInvariant();
                    continue;
                }

                string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
                if (t.Length == 0) continue;

                if (section == "[MEMBERS]")
                {
                    if (t.Length >= 2)
                    {
                        RegisterMemberLikeOriginal(t[0], t[1]);
                        members++;
                    }
                }
                else if (section == "[OFFICERS]")
                {
                    int consumed;
                    C2FormationOfficerBlockV165LikeOriginal block;
                    if (TryParseOfficerBlockHeaderLikeOriginal(t, path, i + 1, out block))
                    {
                        officerBlocks++;
                        consumed = ParseOfficerShapeLinesLikeOriginal(lines, i + 1, block, ref options);
                        i += consumed;
                    }
                }
            }
        }

        private static bool TryParseOfficerBlockHeaderLikeOriginal(
            string[] t,
            string path,
            int lineNumber,
            out C2FormationOfficerBlockV165LikeOriginal block)
        {
            block = null;
            if (t == null || t.Length < 3) return false;
            int shapeCount;
            string flag = t.Length >= 3 ? t[2] : string.Empty;
            string drummer = t.Length >= 2 ? t[1] : string.Empty;
            string flagId = string.Empty;

            if (int.TryParse(flag, NumberStyles.Integer, CultureInfo.InvariantCulture, out shapeCount))
            {
                flagId = string.Empty;
            }
            else
            {
                if (t.Length < 4 || !int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out shapeCount))
                    return false;
                flagId = flag;
            }

            block = new C2FormationOfficerBlockV165LikeOriginal();
            block.OfficerId = t[0];
            block.DrummerId = string.Equals(drummer, "NONE", StringComparison.OrdinalIgnoreCase) ? string.Empty : drummer;
            block.FlagId = string.Equals(flagId, "NONE", StringComparison.OrdinalIgnoreCase) ? string.Empty : flagId;
            block.ShapeCount = Mathf.Max(0, shapeCount);
            block.Source = Path.GetFileName(path) + ":OFFICERS:" + lineNumber.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static int ParseOfficerShapeLinesLikeOriginal(
            string[] lines,
            int startIndex,
            C2FormationOfficerBlockV165LikeOriginal block,
            ref int options)
        {
            int consumed = 0;
            for (int shapeLine = 0; shapeLine < block.ShapeCount && startIndex + consumed < lines.Length; )
            {
                string line = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(lines[startIndex + consumed]);
                consumed++;
                if (line.Length == 0) continue;
                if (line[0] == '[') break;

                string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
                if (t.Length < 4) continue;

                int nAmounts;
                if (!int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out nAmounts))
                    continue;

                int idx = 2;
                var orderIds = new List<string>();
                for (int k = 0; k < nAmounts && idx < t.Length; k++, idx++)
                    orderIds.Add(t[idx]);

                int unitTypeCount;
                if (idx >= t.Length || !int.TryParse(t[idx], NumberStyles.Integer, CultureInfo.InvariantCulture, out unitTypeCount))
                    continue;
                idx++;

                var unitIds = new List<string>();
                for (int k = 0; k < unitTypeCount && idx < t.Length; k++, idx++)
                    unitIds.Add(t[idx]);

                for (int u = 0; u < unitIds.Count; u++)
                {
                    C2FormationRecordV165LikeOriginal record = GetOrCreateRecordLikeOriginal(unitIds[u], block);
                    for (int amount = 0; amount < orderIds.Count; amount++)
                    {
                        C2FormationOrderTemplateV165LikeOriginal order;
                        _ordersById.TryGetValue(orderIds[amount], out order);
                        var option = new C2FormationOptionV165LikeOriginal();
                        option.Shape = t[0];
                        option.OrderId = orderIds[amount];
                        option.AmountIndex = amount;
                        option.OrderTemplate = order;
                        option.UnitCount = order != null ? order.UnitCount : 0;
                        option.IconSprite = IconSpriteForShapeLikeOriginal(option.Shape);
                        option.Title = TitleForShapeLikeOriginal(option.Shape);
                        option.Key = record.UnitId + "|" + option.Shape + "|" + option.OrderId + "|" + amount.ToString(CultureInfo.InvariantCulture);
                        option.Source = block.Source;
                        if (!RecordHasOptionLikeOriginal(record, option.Key))
                        {
                            record.Options.Add(option);
                            options++;
                        }
                    }
                }

                shapeLine++;
            }

            return consumed;
        }

        private static bool RecordHasOptionLikeOriginal(C2FormationRecordV165LikeOriginal record, string key)
        {
            if (record == null || string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < record.Options.Count; i++)
            {
                C2FormationOptionV165LikeOriginal option = record.Options[i];
                if (option != null && string.Equals(option.Key, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static C2FormationRecordV165LikeOriginal GetOrCreateRecordLikeOriginal(
            string unitId,
            C2FormationOfficerBlockV165LikeOriginal block)
        {
            C2FormationRecordV165LikeOriginal record;
            if (_recordsByUnitId.TryGetValue(unitId, out record))
                return record;

            record = new C2FormationRecordV165LikeOriginal();
            record.Key = unitId;
            record.UnitId = unitId;
            record.MdName = ResolveMdForMemberOrRawLikeOriginal(unitId);
            record.NationSuffix = ExtractNationSuffixFromIdLikeOriginal(unitId);
            record.OfficerId = block != null ? block.OfficerId : string.Empty;
            record.DrummerId = block != null ? block.DrummerId : string.Empty;
            record.FlagId = block != null ? block.FlagId : string.Empty;
            record.Source = block != null ? block.Source : string.Empty;
            _recordsByUnitId.Add(unitId, record);
            return record;
        }

        private static int IconSpriteForShapeLikeOriginal(string shape)
        {
            if (string.Equals(shape, "LINE", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(shape, "SQUARE", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(shape, "KARE", StringComparison.OrdinalIgnoreCase)) return 23;
            if (string.Equals(shape, "PRUS2", StringComparison.OrdinalIgnoreCase)) return 20;
            if (string.Equals(shape, "SHER2", StringComparison.OrdinalIgnoreCase)) return 21;
            if (string.Equals(shape, "TRI2", StringComparison.OrdinalIgnoreCase)) return 22;
            return 0;
        }

        private static string TitleForShapeLikeOriginal(string shape)
        {
            if (string.Equals(shape, "LINE", StringComparison.OrdinalIgnoreCase)) return "LINE";
            if (string.Equals(shape, "SQUARE", StringComparison.OrdinalIgnoreCase)) return "SQUARE";
            if (string.Equals(shape, "KARE", StringComparison.OrdinalIgnoreCase)) return "KARE";
            if (string.Equals(shape, "PRUS", StringComparison.OrdinalIgnoreCase)) return "PRUS";
            if (string.Equals(shape, "SHER", StringComparison.OrdinalIgnoreCase)) return "SHER";
            if (string.Equals(shape, "TRI", StringComparison.OrdinalIgnoreCase)) return "TRI";
            return shape ?? string.Empty;
        }

        private static void RegisterMemberLikeOriginal(string unitId, string md)
        {
            if (string.IsNullOrWhiteSpace(unitId) || string.IsNullOrWhiteSpace(md)) return;
            if (!_memberToMd.ContainsKey(unitId)) _memberToMd.Add(unitId, md);
            if (!_mdToMember.ContainsKey(md)) _mdToMember.Add(md, unitId);
            string suffix = ExtractNationSuffixFromIdLikeOriginal(unitId);
            string nationKey = MdNationKeyLikeOriginal(md, suffix);
            if (!string.IsNullOrEmpty(nationKey) && !_mdNationToMember.ContainsKey(nationKey))
                _mdNationToMember.Add(nationKey, unitId);
        }

        private static string ResolveMemberIdForSelectedUnit(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return string.Empty;
            string suffix = ExtractNationSuffixFromIdLikeOriginal(unit.SourceMonsterId);
            if (string.IsNullOrEmpty(suffix))
                suffix = NationSuffixFromIndexLikeOriginal(unit.Nation);

            string[] keys =
            {
                unit.SourceMonsterId,
                unit.ResolvedMd,
                C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(unit.SourceMonsterId),
                C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(unit.ResolvedMd)
            };

            for (int i = 0; i < keys.Length; i++)
            {
                string k = CleanKeyLikeOriginal(keys[i]);
                if (k.Length > 0 && _recordsByUnitId.ContainsKey(k)) return k;
                if (k.Length > 0 && _memberToMd.ContainsKey(k) && _recordsByUnitId.ContainsKey(k)) return k;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string k = CleanKeyLikeOriginal(keys[i]);
                string nationKey = MdNationKeyLikeOriginal(k, suffix);
                string member;
                if (!string.IsNullOrEmpty(nationKey) && _mdNationToMember.TryGetValue(nationKey, out member) && _recordsByUnitId.ContainsKey(member))
                    return member;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string k = CleanKeyLikeOriginal(keys[i]);
                string member;
                if (k.Length > 0 && _mdToMember.TryGetValue(k, out member) && _recordsByUnitId.ContainsKey(member))
                    return member;
            }

            return string.Empty;
        }

        private static string ResolveMemberIdForRawUnitLikeOriginal(string unitId, int nation)
        {
            string k = CleanKeyLikeOriginal(unitId);
            if (k.Length == 0) return string.Empty;
            if (_recordsByUnitId.ContainsKey(k)) return k;

            string suffix = ExtractNationSuffixFromIdLikeOriginal(k);
            if (string.IsNullOrEmpty(suffix))
                suffix = NationSuffixFromIndexLikeOriginal(nation);

            string nationKey = MdNationKeyLikeOriginal(k, suffix);
            string member;
            if (!string.IsNullOrEmpty(nationKey) && _mdNationToMember.TryGetValue(nationKey, out member) && _recordsByUnitId.ContainsKey(member))
                return member;
            if (_mdToMember.TryGetValue(k, out member) && _recordsByUnitId.ContainsKey(member))
                return member;
            return string.Empty;
        }

        private static string ResolveMdForMemberOrRawLikeOriginal(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return string.Empty;
            string md;
            if (_memberToMd.TryGetValue(unitId.Trim(), out md)) return md;
            return C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(unitId);
        }

        private static string CleanKeyLikeOriginal(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }

        private static string ExtractNationSuffixFromIdLikeOriginal(string objectId)
        {
            return C2OriginalProduceCatalogV13.ExtractNationSuffixFromIdPublicV166LikeOriginal(objectId);
        }

        private static string NationSuffixFromIndexLikeOriginal(int nation)
        {
            if (nation == 6) return "FR";
            if (nation == 8) return "RU";
            if (nation == 2) return "EN";
            if (nation == 7) return "PR";
            if (nation == 0) return "AU";
            if (nation == 1) return "EG";
            if (nation == 3) return "PO";
            if (nation == 5) return "SP";
            if (nation == 4) return "RE";
            return string.Empty;
        }

        private static string MdNationKeyLikeOriginal(string mdName, string nationSuffix)
        {
            string md = C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(mdName);
            string suffix = nationSuffix != null ? nationSuffix.Trim() : string.Empty;
            if (string.IsNullOrEmpty(md) || string.IsNullOrEmpty(suffix)) return string.Empty;
            return md.ToUpperInvariant() + "|" + suffix.ToUpperInvariant();
        }

        private static string[] ReadAllLines1251LikeOriginal(string path)
        {
            try { return File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch
            {
                try { return File.ReadAllLines(path, Encoding.GetEncoding(866)); }
                catch
                {
                    try { return File.ReadAllLines(path); }
                    catch { return null; }
                }
            }
        }

        private static List<string> FindNdsFilesLikeOriginal()
        {
            var result = new List<string>();
            var dirs = BuildDataDirsLikeOriginal();
            for (int i = 0; i < dirs.Count; i++)
            {
                string dir = dirs[i];
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                AddFilesFromDirLikeOriginal(result, dir, "*.NDS");
                AddFilesFromDirLikeOriginal(result, dir, "*.nds");
            }
            return result;
        }

        private static string FindFirstDataFileLikeOriginal(string fileName)
        {
            var dirs = BuildDataDirsLikeOriginal();
            for (int i = 0; i < dirs.Count; i++)
            {
                string p = Path.Combine(dirs[i], fileName);
                if (File.Exists(p)) return p;
            }
            return string.Empty;
        }

        private static List<string> BuildDataDirsLikeOriginal()
        {
            var dirs = new List<string>();
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; roots != null && i < roots.Length; i++)
                AddDataRootLikeOriginal(dirs, roots[i]);

            int baseCount = dirs.Count;
            for (int i = 0; i < baseCount; i++)
            {
                AddDataRootLikeOriginal(dirs, Path.Combine(dirs[i], "Nation"));
                AddDataRootLikeOriginal(dirs, Path.Combine(dirs[i], "Nations"));
                AddDataRootLikeOriginal(dirs, Path.Combine(dirs[i], "Data"));
                AddDataRootLikeOriginal(dirs, Path.Combine(dirs[i], "Data1"));
            }
            return dirs;
        }

        private static void AddDataRootLikeOriginal(List<string> dirs, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            string full;
            try { full = Path.GetFullPath(path); }
            catch { full = path; }
            for (int i = 0; i < dirs.Count; i++)
                if (string.Equals(dirs[i], full, StringComparison.OrdinalIgnoreCase)) return;
            dirs.Add(full);
        }

        private static void AddFilesFromDirLikeOriginal(List<string> result, string dir, string mask)
        {
            try
            {
                string[] files = Directory.GetFiles(dir, mask, SearchOption.TopDirectoryOnly);
                for (int i = 0; files != null && i < files.Length; i++)
                {
                    string f = files[i];
                    bool exists = false;
                    for (int k = 0; k < result.Count; k++)
                    {
                        if (string.Equals(result[k], f, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) result.Add(f);
                }
            }
            catch { }
        }
    }

    // V266A: C2GameplayHudV1 partial HUD overrides removed.
    // Current project already defines BuildBuildingHudStateKeyV114LikeOriginal,
    // RebuildBuildingLikeOriginal and DrawBuildingProduceRuntimeOverlaysV124LikeOriginal.
    // Keeping them here caused CS0111 duplicate member compile errors.


    public sealed partial class C2GameplayHudV1
    {
        // V266B: restore only the missing formation/global-brig compatibility methods.
        // Do NOT redefine building HUD methods already present in the current project.

        private const int OriginalGlobalBrigRadiusV172LikeOriginal = 1500;
        private const int OriginalGlobalBrigUiXLikeOriginal = 13;
        private const int OriginalGlobalBrigUiYLikeOriginal = 51;
        private const int OriginalGlobalBrigUiDxLikeOriginal = 67;
        private const int OriginalGlobalBrigExpandedWidthV260LikeOriginal = OriginalGlobalBrigUiDxLikeOriginal * 2 + OriginalUnitProduceWidth;

        private static bool s_globalBrigPrioLoadedV172LikeOriginal;
        private static readonly Dictionary<string, int> s_globalBrigPrioByUnitV172LikeOriginal =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private string _lastGlobalBrigAuditKeyV172LikeOriginal = string.Empty;
        private float _nextGlobalBrigAuditV172LikeOriginal;
        private bool _globalBrigHoverV172LikeOriginal;
        private bool _globalBrigExpandedShownV172LikeOriginal;
        private bool _globalBrigRangeActiveV172LikeOriginal;
        private float _globalBrigExpandedUntilV172LikeOriginal;
        private C2SettlementBuildingSelectableV1LikeOriginal _globalBrigHoverCenterV172LikeOriginal;
        private bool _lastGlobalBrigMouseExpandedV172LikeOriginal;
        private string _lastGlobalBrigRangeAuditKeyV172LikeOriginal = string.Empty;
        private float _nextGlobalBrigRangeAuditV172LikeOriginal;
        private static string s_lastGlobalBrigRangeBuildKeyV172LikeOriginal = string.Empty;
        private static float s_globalBrigTextureThrottleUntilV172LikeOriginal;
        private string _cachedGlobalBrigProposalKeyV172LikeOriginal = string.Empty;
        private bool _cachedGlobalBrigProposalFoundV172LikeOriginal;
        private C2GlobalBrigProposalV172LikeOriginal _cachedGlobalBrigProposalV172LikeOriginal;
        private string _cachedGlobalBrigProposalAuditV172LikeOriginal = string.Empty;
        private float _cachedGlobalBrigProposalUntilV172LikeOriginal;
        private float _nextGlobalBrigRangeRefreshV172LikeOriginal;

        private static readonly Color C2GlobalBrigRangeFillOuterV172LikeOriginal = new Color(0.125f, 1.0f, 0.125f, 0.24f);
        private static readonly Color C2GlobalBrigRangeFillInnerV172LikeOriginal = new Color(0.125f, 1.0f, 0.125f, 0.08f);
        private static readonly Color C2GlobalBrigRangeLineV172LikeOriginal = new Color(0.125f, 1.0f, 0.125f, 0.95f);

        internal static bool IsGlobalBrigHoverTextureThrottleActiveV172LikeOriginal()
        {
            return Time.realtimeSinceStartup <= s_globalBrigTextureThrottleUntilV172LikeOriginal;
        }

        private sealed class C2GlobalBrigProposalV172LikeOriginal
        {
            public string Key = string.Empty;
            public int Nation;
            public C2SettlementBuildingSelectableV1LikeOriginal Center;
            public C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal Record;
            public C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal Option;
            public int Required;
            public int SoldierCount;
            public int OfficerCount;
            public int DrummerCount;
            public int FlagCount;
            public int BrigCount;
            public int Priority;
            public bool CenterCanProduceOfficer;
            public int CenterCount;
            public int ScannedRecords;
            public bool CanCreate;
            public string Audit = string.Empty;
        }

        private bool HasGlobalBrigDialogProposalV172LikeOriginal()
        {
            C2GlobalBrigProposalV172LikeOriginal proposal;
            string audit;
            return TryFindBestGlobalBrigProposalCachedV172LikeOriginal(null, null, out proposal, out audit);
        }

        private string GlobalBrigDialogStateKeyV172LikeOriginal()
        {
            C2GlobalBrigProposalV172LikeOriginal proposal;
            string audit;
            if (!TryFindBestGlobalBrigProposalCachedV172LikeOriginal(null, null, out proposal, out audit) || proposal == null)
                return string.Empty;
            return proposal.Key;
        }

        private void BuildGlobalBrigDialogV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selectedUnit,
            C2SettlementBuildingSelectableV1LikeOriginal selectedBuilding)
        {
            C2GlobalBrigProposalV172LikeOriginal proposal;
            string audit;
            if (!TryFindBestGlobalBrigProposalCachedV172LikeOriginal(selectedUnit, selectedBuilding, out proposal, out audit) || proposal == null)
                return;

            DrawGlobalBrigMainCardV172LikeOriginal(proposal, OriginalGlobalBrigUiXLikeOriginal, OriginalGlobalBrigUiYLikeOriginal);

            string auditKey = proposal.Key + "|" + proposal.Audit;
            if (_lastGlobalBrigAuditKeyV172LikeOriginal != auditKey || Time.realtimeSinceStartup >= _nextGlobalBrigAuditV172LikeOriginal)
            {
                _lastGlobalBrigAuditKeyV172LikeOriginal = auditKey;
                _nextGlobalBrigAuditV172LikeOriginal = Time.realtimeSinceStartup + 1.0f;
                Debug.Log("[C2:GLOBAL BRIG V172] " + proposal.Audit);
            }
        }

        private bool TryFindBestGlobalBrigProposalCachedV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selectedUnit,
            C2SettlementBuildingSelectableV1LikeOriginal selectedBuilding,
            out C2GlobalBrigProposalV172LikeOriginal proposal,
            out string audit)
        {
            int wantedNation = -1;
            if (selectedUnit != null) wantedNation = selectedUnit.Nation;
            else if (selectedBuilding != null) wantedNation = selectedBuilding.Nation;

            string key =
                (selectedUnit != null ? selectedUnit.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "0") + "|" +
                (selectedBuilding != null ? selectedBuilding.GetEntityId().ToString() : "0") + "|" +
                wantedNation.ToString(CultureInfo.InvariantCulture);

            float now = Time.realtimeSinceStartup;
            if (now < _cachedGlobalBrigProposalUntilV172LikeOriginal &&
                string.Equals(_cachedGlobalBrigProposalKeyV172LikeOriginal, key, StringComparison.Ordinal))
            {
                proposal = _cachedGlobalBrigProposalV172LikeOriginal;
                audit = _cachedGlobalBrigProposalAuditV172LikeOriginal;
                return _cachedGlobalBrigProposalFoundV172LikeOriginal;
            }

            bool found = TryFindBestGlobalBrigProposalV172LikeOriginal(selectedUnit, selectedBuilding, out proposal, out audit);
            _cachedGlobalBrigProposalKeyV172LikeOriginal = key;
            _cachedGlobalBrigProposalFoundV172LikeOriginal = found && proposal != null;
            _cachedGlobalBrigProposalV172LikeOriginal = _cachedGlobalBrigProposalFoundV172LikeOriginal ? proposal : null;
            _cachedGlobalBrigProposalAuditV172LikeOriginal = audit ?? string.Empty;
            // The proposal scans formation records against every live unit. Repeating that several
            // times per second caused periodic main-thread stalls while the barracks zone was visible.
            _cachedGlobalBrigProposalUntilV172LikeOriginal = now + 3.0f;
            return found;
        }

        private bool TryFindBestGlobalBrigProposalV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selectedUnit,
            C2SettlementBuildingSelectableV1LikeOriginal selectedBuilding,
            out C2GlobalBrigProposalV172LikeOriginal best,
            out string audit)
        {
            using var profileScope = GlobalBrigProposalMarkerLikeOriginal.Auto();
            best = null;
            audit = "not_started";
            EnsureGlobalBrigPrioLoadedV172LikeOriginal();

            int wantedNation = -1;
            if (selectedUnit != null) wantedNation = selectedUnit.Nation;
            else if (selectedBuilding != null) wantedNation = selectedBuilding.Nation;

            List<C2SettlementBuildingSelectableV1LikeOriginal> centers =
                BuildGlobalCommandCentersV172LikeOriginal(wantedNation);
            if (centers.Count == 0 && wantedNation >= 0)
                centers = BuildGlobalCommandCentersV172LikeOriginal(-1);
            if (centers.Count == 0)
            {
                audit = "no_COMMANDCENTER_ready";
                return false;
            }

            List<C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal> records =
                C2FormationCreateCatalogV165LikeOriginal.BuildAllRecordsSnapshotLikeOriginal();
            if (records == null || records.Count == 0)
            {
                best = BuildDisabledGlobalBrigProposalV172LikeOriginal(
                    centers,
                    "no_formation_records",
                    0,
                    0);
                audit = best.Audit;
                return true;
            }

            C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            int scannedRecords = 0;
            int usableCenters = 0;
            for (int c = 0; c < centers.Count; c++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal center = centers[c];
                if (center == null) continue;
                usableCenters++;
                Dictionary<string, int> memberCounts = CountGlobalBrigMembersNearCenterLikeOriginal(center, units);
                for (int r = 0; r < records.Count; r++)
                {
                    C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record = records[r];
                    if (record == null || record.Options == null || record.Options.Count == 0) continue;
                    if (!GlobalBrigRecordMatchesCommandCenterNationV172LikeOriginal(center, record)) continue;
                    scannedRecords++;

                    C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                        SelectGlobalBrigDefaultOptionV172LikeOriginal(record);
                    if (option == null || option.UnitCount <= 0) continue;

                    int soldiers = GlobalBrigMemberCountLikeOriginal(memberCounts, record.UnitId);
                    int officers = GlobalBrigMemberCountLikeOriginal(memberCounts, record.OfficerId);
                    int drummers = GlobalBrigMemberCountLikeOriginal(memberCounts, record.DrummerId);
                    int flags = GlobalBrigMemberCountLikeOriginal(memberCounts, record.FlagId);

                    int brigCount = soldiers / Mathf.Max(1, option.UnitCount);
                    if (brigCount <= 0) continue;

                    int priority = GlobalBrigPriorityV172LikeOriginal(record.UnitId);
                    bool centerCanProduceOfficer = CenterCanProduceMemberV172LikeOriginal(center, record.OfficerId);

                    C2GlobalBrigProposalV172LikeOriginal proposal = new C2GlobalBrigProposalV172LikeOriginal();
                    proposal.Center = center;
                    proposal.Record = record;
                    proposal.Option = option;
                    proposal.Nation = center.Nation;
                    proposal.Required = Mathf.Max(1, option.UnitCount);
                    proposal.SoldierCount = soldiers;
                    proposal.OfficerCount = officers;
                    proposal.DrummerCount = drummers;
                    proposal.FlagCount = flags;
                    proposal.BrigCount = brigCount;
                    proposal.Priority = priority;
                    proposal.CenterCanProduceOfficer = centerCanProduceOfficer;
                    proposal.CenterCount = centers.Count;
                    proposal.ScannedRecords = scannedRecords;
                    proposal.CanCreate = brigCount > 0;
                    proposal.Key =
                        "COMMANDCENTER|" + center.GetEntityId().ToString() +
                        "|" + (record.UnitId ?? string.Empty) +
                        "|" + (option.Shape ?? string.Empty) +
                        "|" + proposal.Required.ToString(CultureInfo.InvariantCulture) +
                        "|" + soldiers.ToString(CultureInfo.InvariantCulture) +
                        "|" + officers.ToString(CultureInfo.InvariantCulture) +
                        "|" + drummers.ToString(CultureInfo.InvariantCulture) +
                        "|" + flags.ToString(CultureInfo.InvariantCulture);
                    proposal.Audit =
                        "mode=GetGlobalCreateBrigList_COMMANDCENTER center='" + (center.SourceMonsterId ?? string.Empty) +
                        "' nation=" + center.Nation.ToString(CultureInfo.InvariantCulture) +
                        " radius=" + OriginalGlobalBrigRadiusV172LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                        " unit='" + (record.UnitId ?? string.Empty) +
                        "' option='" + (option.Shape ?? string.Empty) +
                        "' required=" + proposal.Required.ToString(CultureInfo.InvariantCulture) +
                        " soldiers=" + soldiers.ToString(CultureInfo.InvariantCulture) +
                        " brigCount=" + brigCount.ToString(CultureInfo.InvariantCulture) +
                        " officer='" + (record.OfficerId ?? string.Empty) + "' officers=" + officers.ToString(CultureInfo.InvariantCulture) +
                        " drummer='" + (record.DrummerId ?? string.Empty) + "' drummers=" + drummers.ToString(CultureInfo.InvariantCulture) +
                        " flag='" + (record.FlagId ?? string.Empty) + "' flags=" + flags.ToString(CultureInfo.InvariantCulture) +
                        " centerCanProduceOfficer=" + centerCanProduceOfficer.ToString() +
                        " priority=" + priority.ToString(CultureInfo.InvariantCulture);

                    if (IsBetterGlobalBrigProposalV172LikeOriginal(proposal, best))
                        best = proposal;
                }
            }

            if (best == null)
            {
                best = BuildDisabledGlobalBrigProposalV172LikeOriginal(
                    centers,
                    "no_complete_brigade",
                    usableCenters,
                    scannedRecords);
                audit = best.Audit;
                return true;
            }

            audit = best.Audit;
            return true;
        }

        private static C2GlobalBrigProposalV172LikeOriginal BuildDisabledGlobalBrigProposalV172LikeOriginal(
            List<C2SettlementBuildingSelectableV1LikeOriginal> centers,
            string reason,
            int usableCenters,
            int scannedRecords)
        {
            C2SettlementBuildingSelectableV1LikeOriginal center =
                centers != null && centers.Count > 0 ? centers[0] : null;
            int centerCount = centers != null ? centers.Count : 0;
            var proposal = new C2GlobalBrigProposalV172LikeOriginal();
            proposal.Center = center;
            proposal.Nation = center != null ? center.Nation : -1;
            proposal.CenterCount = centerCount;
            proposal.ScannedRecords = scannedRecords;
            proposal.CanCreate = false;
            proposal.Key =
                "COMMANDCENTER_DISABLED|" +
                centerCount.ToString(CultureInfo.InvariantCulture) +
                "|" + (center != null ? center.GetEntityId().ToString() : "none") +
                "|" + (reason ?? string.Empty) +
                "|" + scannedRecords.ToString(CultureInfo.InvariantCulture);
            proposal.Audit =
                "mode=GetGlobalCreateBrigList_COMMANDCENTER_DISABLED reason='" + (reason ?? string.Empty) +
                "' centers=" + centerCount.ToString(CultureInfo.InvariantCulture) +
                " usableCenters=" + usableCenters.ToString(CultureInfo.InvariantCulture) +
                " records=" + scannedRecords.ToString(CultureInfo.InvariantCulture) +
                " radius=" + OriginalGlobalBrigRadiusV172LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                " center='" + (center != null ? (center.SourceMonsterId ?? string.Empty) : string.Empty) +
                "'";
            return proposal;
        }

        private static List<C2SettlementBuildingSelectableV1LikeOriginal> BuildGlobalCommandCentersV172LikeOriginal(int wantedNation)
        {
            List<C2SettlementBuildingSelectableV1LikeOriginal> centers = new List<C2SettlementBuildingSelectableV1LikeOriginal>(16);
            C2SettlementBuildingSelectableV1LikeOriginal[] all =
                FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null || !b.isActiveAndEnabled || !b.gameObject.activeInHierarchy) continue;
                if (b.NotSelectable || !b.ReadyLikeOriginal) continue;
                if (wantedNation >= 0 && b.Nation != wantedNation) continue;

                string commandCenterAudit;
                if (!IsGlobalCommandCenterBuildingV172LikeOriginal(b, out commandCenterAudit)) continue;
                centers.Add(b);
            }
            return centers;
        }

        private static bool IsGlobalCommandCenterBuildingV172LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            out string audit)
        {
            audit = string.Empty;
            if (building == null) return false;

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 selectedInfo =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(building);
            if (selectedInfo.CommandCenter)
            {
                audit = "selected";
                return true;
            }

            string[] probes =
            {
                C2OriginalProduceCatalogV13.ResolveMdForSelectedBuildingLikeOriginal(building),
                building.SourceMonsterId,
                C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(building.SourceMonsterId),
                building.KindName,
                C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(building.KindName)
            };

            for (int i = 0; i < probes.Length; i++)
            {
                string probe = probes[i];
                if (string.IsNullOrWhiteSpace(probe)) continue;
                bool duplicate = false;
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(probes[j], probe, StringComparison.OrdinalIgnoreCase))
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (duplicate) continue;

                C2OriginalProduceCatalogV13.C2MdIconInfoV13 info =
                    C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(probe);
                if (info.CommandCenter)
                {
                    audit = "probe='" + probe + "'";
                    return true;
                }
            }

            audit = "not_COMMANDCENTER source='" + (building.SourceMonsterId ?? string.Empty) +
                    "' kind='" + (building.KindName ?? string.Empty) + "'";
            return false;
        }

        private static bool IsBetterGlobalBrigProposalV172LikeOriginal(
            C2GlobalBrigProposalV172LikeOriginal candidate,
            C2GlobalBrigProposalV172LikeOriginal current)
        {
            if (candidate == null) return false;
            if (current == null) return true;
            if (candidate.BrigCount != current.BrigCount) return candidate.BrigCount > current.BrigCount;
            if (candidate.Priority != current.Priority) return candidate.Priority < current.Priority;
            if (candidate.CenterCanProduceOfficer != current.CenterCanProduceOfficer) return candidate.CenterCanProduceOfficer;
            if (candidate.SoldierCount != current.SoldierCount) return candidate.SoldierCount > current.SoldierCount;
            return string.Compare(candidate.Record != null ? candidate.Record.UnitId : string.Empty,
                current.Record != null ? current.Record.UnitId : string.Empty,
                StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal SelectGlobalBrigDefaultOptionV172LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record)
        {
            if (record == null || record.Options == null) return null;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal best = null;
            for (int i = 0; i < record.Options.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = record.Options[i];
                if (option == null || option.UnitCount <= 0) continue;
                if (best == null || option.UnitCount < best.UnitCount)
                    best = option;
            }
            return best;
        }

        private static readonly Unity.Profiling.ProfilerMarker GlobalBrigProposalMarkerLikeOriginal =
            new Unity.Profiling.ProfilerMarker("C2.Hud.GlobalBrigProposal");

        private static Dictionary<string, int> CountGlobalBrigMembersNearCenterLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal center,
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (center == null || units == null) return counts;

            float centerX = center.RealX;
            float centerY = center.RealY;
            float radiusReal = OriginalGlobalBrigRadiusV172LikeOriginal * 16.0f;
            float radius2 = radiusReal * radiusReal;

            for (int i = 0; i < units.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                if (!IsUsableGlobalBrigUnitV172LikeOriginal(u)) continue;
                if (u.Nation != center.Nation) continue;

                float ux = u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                float uy = u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                float dx = ux - centerX;
                float dy = uy - centerY;
                if (dx * dx + dy * dy > radius2) continue;

                string resolved = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(u);
                AddGlobalBrigMemberCountLikeOriginal(counts, resolved);
                if (!string.Equals(resolved, u.SourceMonsterId, StringComparison.OrdinalIgnoreCase))
                    AddGlobalBrigMemberCountLikeOriginal(counts, u.SourceMonsterId);
            }
            return counts;
        }

        private static bool IsUsableGlobalBrigUnitV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || !unit.isActiveAndEnabled) return false;
            if (unit.NotSelectable || unit.IsDeadLikeOriginal) return false;
            if (!unit.CanReceivePlayerOrdersLikeOriginal()) return false;
            if (C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit)) return false;
            return true;
        }

        private static void AddGlobalBrigMemberCountLikeOriginal(Dictionary<string, int> counts, string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId)) return;
            int count;
            counts.TryGetValue(memberId, out count);
            counts[memberId] = count + 1;
        }

        private static int GlobalBrigMemberCountLikeOriginal(Dictionary<string, int> counts, string memberId)
        {
            int count;
            return !string.IsNullOrWhiteSpace(memberId) && counts.TryGetValue(memberId, out count) ? count : 0;
        }

        private static bool GlobalBrigRecordMatchesCommandCenterNationV172LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal center,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record)
        {
            if (center == null || record == null) return false;
            string recordSuffix = C2OriginalProduceCatalogV13.ExtractNationSuffixFromIdPublicV166LikeOriginal(record.UnitId);
            if (string.IsNullOrEmpty(recordSuffix)) recordSuffix = record.NationSuffix ?? string.Empty;
            recordSuffix = recordSuffix.Trim();
            if (recordSuffix.Length == 0) return true;

            string[] probes =
            {
                center.SourceMonsterId,
                center.KindName,
                C2OriginalProduceCatalogV13.ResolveMdForSelectedBuildingLikeOriginal(center)
            };
            for (int i = 0; i < probes.Length; i++)
            {
                string centerSuffix = C2OriginalProduceCatalogV13.ExtractNationSuffixFromIdPublicV166LikeOriginal(probes[i]);
                if (string.IsNullOrEmpty(centerSuffix)) continue;
                if (string.Equals(centerSuffix.Trim(), recordSuffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool CenterCanProduceMemberV172LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal center, string memberId)
        {
            if (center == null || string.IsNullOrWhiteSpace(memberId)) return false;
            string audit;
            List<C2OriginalProduceItemV13> items = C2OriginalProduceCatalogV13.BuildForSelectedBuilding(center, out audit);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                C2OriginalProduceItemV13 item = items[i];
                if (item == null) continue;
                if (string.Equals(item.UnitId ?? string.Empty, memberId, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void DrawGlobalBrigMainCardV172LikeOriginal(C2GlobalBrigProposalV172LikeOriginal proposal, int x, int y)
        {
            if (proposal == null) return;

            string fileId;
            int spriteId;
            bool active = proposal.Record != null && proposal.CanCreate;
            if (proposal.Record != null)
                ResolveGlobalBrigUnitIconV172LikeOriginal(proposal.Record.UnitId, out fileId, out spriteId);
            else
            {
                fileId = "Interf3\\Units_can_mini";
                spriteId = 5;
            }
            AddG16ImageOverpaintV140LikeOriginal(
                "global_brig_cell_back_v172",
                "Interf3\\FormInterface",
                21,
                x,
                y,
                OriginalUnitProduceWidth,
                OriginalUnitProduceHeight - 2,
                active ? 255 : 128,
                false,
                56);
            AddG16ImageOverpaintV140LikeOriginal(
                "global_brig_unit_icon_v172",
                fileId,
                spriteId,
                x + OriginalUnitProduceIconX,
                y + OriginalUnitProduceIconY,
                OriginalUnitProduceIconW,
                OriginalUnitProduceIconH,
                active ? 255 : 105,
                false,
                active ? 120 : 32,
                false,
                true);

            bool expanded = proposal.Record != null && ShouldShowGlobalBrigExpandedV172LikeOriginal();
            _globalBrigExpandedShownV172LikeOriginal = expanded;

            if (proposal.Record != null)
            {
                AddCrispLabelV140LikeOriginal(
                    "global_brig_count_v172",
                    proposal.BrigCount.ToString(CultureInfo.InvariantCulture),
                    x + 43,
                    y + 4,
                    16,
                    13,
                    9,
                    TextAnchor.MiddleCenter,
                    Color.white);

                if (expanded)
                {
                    int nextX = x + OriginalGlobalBrigUiDxLikeOriginal;
                    AddGlobalBrigHoverAreaV172LikeOriginal("global_brig_expanded_hover_v172", nextX, y, OriginalGlobalBrigExpandedWidthV260LikeOriginal, OriginalUnitProduceHeight, proposal.Center);
                    DrawGlobalBrigCommandMiniV172LikeOriginal("officer", proposal.Center, proposal.Record.OfficerId, proposal.OfficerCount, nextX, y);
                    DrawGlobalBrigCommandMiniV172LikeOriginal("drummer", proposal.Center, proposal.Record.DrummerId, proposal.DrummerCount, nextX + OriginalGlobalBrigUiDxLikeOriginal, y);
                    DrawGlobalBrigCommandMiniV172LikeOriginal("flag", proposal.Center, proposal.Record.FlagId, proposal.FlagCount, nextX + OriginalGlobalBrigUiDxLikeOriginal * 2, y);
                }
            }
            AddGlobalBrigClickAreaV172LikeOriginal("global_brig_click_v172", x, y, OriginalUnitProduceWidth, OriginalUnitProduceHeight, proposal);
        }

        private bool ShouldShowGlobalBrigExpandedV172LikeOriginal()
        {
            return IsMouseOverGlobalBrigUiRectV172LikeOriginal(true);
        }

        private void DrawGlobalBrigCommandMiniV172LikeOriginal(string suffix, C2SettlementBuildingSelectableV1LikeOriginal center, string memberId, int count, int x, int y)
        {
            if (string.IsNullOrWhiteSpace(memberId)) return;
            string fileId;
            int spriteId;
            ResolveGlobalBrigUnitIconV172LikeOriginal(memberId, out fileId, out spriteId);
            bool canProduceHere = CenterCanProduceMemberV172LikeOriginal(center, memberId);
            int iconAlpha = count > 0 ? 220 : (canProduceHere ? 150 : 70);
            int iconBoost = count > 0 ? 90 : (canProduceHere ? 48 : 18);
            AddG16ImageOverpaintV140LikeOriginal(
                "global_brig_" + suffix + "_back_v172",
                "Interf3\\FormInterface",
                21,
                x,
                y,
                OriginalUnitProduceWidth,
                OriginalUnitProduceHeight - 2,
                180,
                false,
                56);
            AddG16ImageOverpaintV140LikeOriginal(
                "global_brig_" + suffix + "_icon_v172",
                fileId,
                spriteId,
                x + OriginalUnitProduceIconX,
                y + OriginalUnitProduceIconY,
                OriginalUnitProduceIconW,
                OriginalUnitProduceIconH,
                iconAlpha,
                false,
                iconBoost,
                false,
                true);
            AddG16ImageTopSliceV117LikeOriginal(
                "global_brig_" + suffix + "_count_plate_v260",
                "Interf3\\FormInterface",
                23,
                x,
                y + 103,
                57,
                20,
                220,
                false);
            AddCrispLabelV140LikeOriginal(
                "global_brig_" + suffix + "_count_v172",
                count.ToString(CultureInfo.InvariantCulture),
                x + 20,
                y + 108,
                21,
                11,
                9,
                TextAnchor.MiddleCenter,
                Color.white);
            DrawGlobalBrigMemberProgressLineV260LikeOriginal(suffix, center, memberId, x, y);
            AddGlobalBrigMemberClickAreaV172LikeOriginal(
                "global_brig_" + suffix + "_member_click_v172",
                x,
                y,
                OriginalUnitProduceWidth,
                OriginalUnitProduceHeight,
                center,
                memberId,
                canProduceHere);
        }

        private void DrawGlobalBrigMemberProgressLineV260LikeOriginal(string suffix, C2SettlementBuildingSelectableV1LikeOriginal center, string memberId, int x, int y)
        {
            C2BuildingProduceCardStateV114LikeOriginal cardState =
                C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(center, memberId);
            if (cardState.Count <= 0 && !cardState.Infinite)
                return;

            const int fullH = 109;
            Image progressImg = AddSolid(
                "global_brig_" + suffix + "_produce_progress_v260",
                new Color(0.0f, 1.0f, 0.0f, 1.0f),
                x + 60,
                y + 7 + fullH,
                2,
                1,
                false);
            Image progressSecondPass = FindHudImageV143ALikeOriginal(
                "global_brig_" + suffix + "_produce_progress_v260_v140a_doublepass");
            C2BuildingProduceProgressBarUiV125LikeOriginal bar =
                progressImg.gameObject.AddComponent<C2BuildingProduceProgressBarUiV125LikeOriginal>();
            bar.InitLikeOriginal(center, memberId, x + 60, y + 7, 2, fullH, progressSecondPass);
        }

        private void AddGlobalBrigClickAreaV172LikeOriginal(
            string name,
            int x,
            int y,
            int w,
            int h,
            C2GlobalBrigProposalV172LikeOriginal proposal)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            C2HudGlobalBrigRelayV172LikeOriginal relay = go.AddComponent<C2HudGlobalBrigRelayV172LikeOriginal>();
            relay.Owner = this;
            relay.Center = proposal.Center;
            relay.Record = proposal.Record;
            relay.Option = proposal.Option;
            relay.Enabled = proposal.CanCreate && proposal.BrigCount > 0 && proposal.Record != null && proposal.Option != null;
            relay.Clickable = true;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
        }

        private void AddGlobalBrigHoverAreaV172LikeOriginal(
            string name,
            int x,
            int y,
            int w,
            int h,
            C2SettlementBuildingSelectableV1LikeOriginal center)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            C2HudGlobalBrigRelayV172LikeOriginal relay = go.AddComponent<C2HudGlobalBrigRelayV172LikeOriginal>();
            relay.Owner = this;
            relay.Center = center;
            relay.Clickable = false;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
        }

        private void AddGlobalBrigMemberClickAreaV172LikeOriginal(
            string name,
            int x,
            int y,
            int w,
            int h,
            C2SettlementBuildingSelectableV1LikeOriginal center,
            string memberId,
            bool enabled)
        {
            GameObject go = NewUi(name);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            C2HudGlobalBrigRelayV172LikeOriginal relay = go.AddComponent<C2HudGlobalBrigRelayV172LikeOriginal>();
            relay.Owner = this;
            relay.Center = center;
            relay.MemberId = memberId ?? string.Empty;
            relay.Enabled = enabled;
            relay.Clickable = true;
            Place(go.GetComponent<RectTransform>(), x, y, w, h);
        }

        private static void ResolveGlobalBrigUnitIconV172LikeOriginal(string memberId, out string fileId, out int spriteId)
        {
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon = C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(memberId);
            if (!string.IsNullOrEmpty(icon.MinIconFile))
            {
                fileId = icon.MinIconFile;
                spriteId = icon.MinIconSprite;
                return;
            }
            if (icon.HasExIcon && !string.IsNullOrEmpty(icon.ExIconFile))
            {
                fileId = icon.ExIconFile;
                spriteId = icon.ExIconSprite;
                return;
            }
            if (icon.HasIcon)
            {
                fileId = !string.IsNullOrEmpty(icon.IconFile) ? icon.IconFile : "Interf3\\BldSmallIcons";
                spriteId = icon.IconSprite;
                return;
            }
            if (!string.IsNullOrEmpty(icon.BigIconFile))
            {
                fileId = icon.BigIconFile;
                spriteId = icon.BigIconSprite;
                return;
            }
            fileId = "Interf3\\BldSmallIcons";
            spriteId = 0;
        }

        internal void OnGlobalBrigClickedV172LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal center,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            bool enabled)
        {
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            string result;
            if (!enabled)
            {
                result = "disabled";
            }
            else
            {
                int groupId;
                string audit;
                bool ok = C2FormationRuntimeV167LikeOriginal.TryCreateGlobalBrigFromCommandCenterV172LikeOriginal(
                    center,
                    record,
                    option,
                    out groupId,
                    out audit);
                result = audit;
                if (ok)
                {
                    _lastGlobalBrigDialogStateKeyV172LikeOriginal = string.Empty;
                    _cachedGlobalBrigProposalUntilV172LikeOriginal = 0.0f;
                    _lastSelectedCount = -999999;
                    _lastBuildingSelectedCount = -999999;
                    _nextRefresh = 0.0f;
                }
            }

            Debug.Log("[C2:GLOBAL BRIG V172 CLICK] " + result);
        }

        internal void OnGlobalBrigMemberClickedV172LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal center,
            string memberId,
            bool enabled)
        {
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            string result;
            if (!enabled)
            {
                result = "disabled member='" + (memberId ?? string.Empty) + "'";
            }
            else
            {
                C2OriginalProduceItemV13 item;
                string audit;
                if (!TryFindGlobalBrigMemberProduceItemV172LikeOriginal(center, memberId, out item, out audit))
                {
                    result = audit;
                }
                else
                {
                    bool ok = C2BuildingProductionCardsRuntimeV114.TryHandleBuildingProduceClickForBuildingLikeOriginal(
                        center,
                        item,
                        1,
                        false,
                        "global_brig_mini");
                    result = (ok ? "produce_ok " : "produce_failed ") + audit;
                    if (ok)
                    {
                        _lastGlobalBrigDialogStateKeyV172LikeOriginal = string.Empty;
                        _lastBuildingStateKey = string.Empty;
                        _cachedGlobalBrigProposalUntilV172LikeOriginal = 0.0f;
                        _nextRefresh = 0.0f;
                    }
                }
            }

            Debug.Log("[C2:GLOBAL BRIG V172 MEMBER CLICK] " + result);
        }

        private static bool TryFindGlobalBrigMemberProduceItemV172LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal center,
            string memberId,
            out C2OriginalProduceItemV13 item,
            out string audit)
        {
            item = null;
            if (center == null)
            {
                audit = "no_center member='" + (memberId ?? string.Empty) + "'";
                return false;
            }
            if (string.IsNullOrWhiteSpace(memberId))
            {
                audit = "empty_member center='" + (center.SourceMonsterId ?? string.Empty) + "'";
                return false;
            }

            string buildAudit;
            List<C2OriginalProduceItemV13> items = C2OriginalProduceCatalogV13.BuildForSelectedBuilding(center, out buildAudit);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                C2OriginalProduceItemV13 candidate = items[i];
                if (candidate == null) continue;
                if (!string.Equals(candidate.UnitId ?? string.Empty, memberId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!candidate.Enabled)
                {
                    audit = "member_disabled member='" + memberId + "' " + (buildAudit ?? string.Empty);
                    return false;
                }

                item = candidate;
                audit = "member='" + memberId + "' center='" + (center.SourceMonsterId ?? string.Empty) + "' " + (buildAudit ?? string.Empty);
                return true;
            }

            audit = "member_not_in_center_produce member='" + memberId + "' center='" + (center.SourceMonsterId ?? string.Empty) +
                    "' " + (buildAudit ?? string.Empty);
            return false;
        }

        internal void OnGlobalBrigHoverV172LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal center, bool hover)
        {
            bool nextHover = hover && IsMouseOverGlobalBrigUiRectV172LikeOriginal(false);
            C2SettlementBuildingSelectableV1LikeOriginal nextCenter = nextHover ? center : null;

            // Do not invalidate/rebuild the HUD from its own pointer-enter event. The previous code
            // destroyed and recreated the hovered button, which generated another pointer-enter and
            // formed a continuous rebuild/GC loop that also made clicks unreliable.
            if (nextHover == _globalBrigHoverV172LikeOriginal && nextCenter == _globalBrigHoverCenterV172LikeOriginal)
                return;

            _globalBrigHoverV172LikeOriginal = nextHover;
            _globalBrigHoverCenterV172LikeOriginal = nextCenter;
            _globalBrigExpandedUntilV172LikeOriginal = nextHover ? Time.realtimeSinceStartup + 0.15f : 0.0f;
            s_globalBrigTextureThrottleUntilV172LikeOriginal = nextHover ? Time.realtimeSinceStartup + 0.75f : 0.0f;
            _nextGlobalBrigRangeRefreshV172LikeOriginal = 0.0f;

            if (nextHover)
                RebuildGlobalBrigCommandCenterRangeV172LikeOriginal(true);
            else
                HideGlobalBrigCommandCenterRangeV172LikeOriginal();
        }

        private void LateUpdateGlobalBrigHoverV172LikeOriginal()
        {
            bool mouseOverMain = IsMouseOverGlobalBrigUiRectV172LikeOriginal(false);
            bool mouseOverExpanded = IsMouseOverGlobalBrigUiRectV172LikeOriginal(true);

            if (mouseOverExpanded != _lastGlobalBrigMouseExpandedV172LikeOriginal)
            {
                _lastGlobalBrigMouseExpandedV172LikeOriginal = mouseOverExpanded;
                _lastGlobalBrigDialogStateKeyV172LikeOriginal = string.Empty;
                _nextRefresh = 0.0f;
            }

            if (mouseOverMain)
            {
                float now = Time.realtimeSinceStartup;
                s_globalBrigTextureThrottleUntilV172LikeOriginal = now + 0.75f;
                if (_globalBrigHoverCenterV172LikeOriginal == null)
                    _globalBrigHoverCenterV172LikeOriginal = ResolveCurrentGlobalBrigCenterV172LikeOriginal();
                if (_globalBrigHoverCenterV172LikeOriginal != null && !_globalBrigRangeActiveV172LikeOriginal)
                {
                    // The center and radius are static while this hover is active. Reusing the
                    // already-built mesh avoids rebuilding 192-point geometry every 0.35 seconds.
                    _nextGlobalBrigRangeRefreshV172LikeOriginal = now + 3.0f;
                    RebuildGlobalBrigCommandCenterRangeV172LikeOriginal(false);
                }
                return;
            }

            HideGlobalBrigCommandCenterRangeV172LikeOriginal();
        }

        private bool IsMouseOverGlobalBrigUiRectV172LikeOriginal(bool includeExpanded)
        {
            if (_root == null || _canvas == null) return false;
            Vector2 mouseScreen;
            if (!TryGetGlobalBrigMouseScreenPositionV172LikeOriginal(out mouseScreen))
                return false;

            Vector2 refSize = _root.rect.size.sqrMagnitude > 1.0f ? _root.rect.size : new Vector2(1024.0f, 768.0f);
            float sx = Screen.width > 1 ? refSize.x / Screen.width : 1.0f;
            float sy = Screen.height > 1 ? refSize.y / Screen.height : 1.0f;
            Vector2 topLeftPoint = new Vector2(mouseScreen.x * sx, (Screen.height - mouseScreen.y) * sy);
            Rect main = new Rect(
                OriginalGlobalBrigUiXLikeOriginal,
                OriginalGlobalBrigUiYLikeOriginal,
                OriginalUnitProduceWidth,
                OriginalUnitProduceHeight);
            if (main.Contains(topLeftPoint)) return true;

            if (!includeExpanded) return false;
            Rect expanded = new Rect(
                OriginalGlobalBrigUiXLikeOriginal + OriginalGlobalBrigUiDxLikeOriginal,
                OriginalGlobalBrigUiYLikeOriginal,
                OriginalGlobalBrigExpandedWidthV260LikeOriginal,
                OriginalUnitProduceHeight);
            return expanded.Contains(topLeftPoint);
        }

        private static bool TryGetGlobalBrigMouseScreenPositionV172LikeOriginal(out Vector2 mouseScreen)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mouseScreen = Mouse.current.position.ReadValue();
                return true;
            }
#endif
            try
            {
                Vector3 p = UnityEngine.Input.mousePosition;
                mouseScreen = new Vector2(p.x, p.y);
                return true;
            }
            catch
            {
                mouseScreen = Vector2.zero;
                return false;
            }
        }

        private C2SettlementBuildingSelectableV1LikeOriginal ResolveCurrentGlobalBrigCenterV172LikeOriginal()
        {
            C2GlobalBrigProposalV172LikeOriginal proposal;
            string audit;
            return TryFindBestGlobalBrigProposalCachedV172LikeOriginal(null, null, out proposal, out audit) && proposal != null
                ? proposal.Center
                : null;
        }

        private void RebuildGlobalBrigCommandCenterRangeV172LikeOriginal(bool forceAudit)
        {
            C2SettlementBuildingSelectableV1LikeOriginal hoverCenter = _globalBrigHoverCenterV172LikeOriginal;
            int wantedNation = hoverCenter != null ? hoverCenter.Nation : -1;
            List<C2SettlementBuildingSelectableV1LikeOriginal> centers = BuildGlobalCommandCentersV172LikeOriginal(wantedNation);
            if (centers.Count == 0 && hoverCenter != null)
                centers.Add(hoverCenter);
            if (centers.Count == 0)
            {
                HideGlobalBrigCommandCenterRangeV172LikeOriginal();
                return;
            }

            float scale = ResolveGlobalBrigMapPixelToWorldScaleV172LikeOriginal(hoverCenter != null ? hoverCenter : centers[0]);
            float radiusWorld = Mathf.Clamp(OriginalGlobalBrigRadiusV172LikeOriginal * scale, 1.0f, 4096.0f);
            string buildKey = BuildGlobalBrigRangeBuildKeyV172LikeOriginal(centers, wantedNation, radiusWorld);
            string auditKey = wantedNation.ToString(CultureInfo.InvariantCulture) + "#" + centers.Count.ToString(CultureInfo.InvariantCulture) + "#" + radiusWorld.ToString("0.###", CultureInfo.InvariantCulture);

            if (_weaponRangeRootV154LikeOriginal != null &&
                _weaponRangeRootV154LikeOriginal.transform.childCount > 0 &&
                string.Equals(s_lastGlobalBrigRangeBuildKeyV172LikeOriginal, buildKey, StringComparison.Ordinal))
            {
                DisableWeaponRangeScreenAndGuiV162LikeOriginal();
                _weaponRangeRootV154LikeOriginal.SetActive(true);
                _globalBrigRangeActiveV172LikeOriginal = true;
                if (forceAudit)
                {
                    _lastGlobalBrigRangeAuditKeyV172LikeOriginal = auditKey;
                    _nextGlobalBrigRangeAuditV172LikeOriginal = Time.realtimeSinceStartup + 1.0f;
                    Debug.Log("[C2:GLOBAL BRIG RANGE V172] centers=" + centers.Count.ToString(CultureInfo.InvariantCulture) +
                              " nation=" + wantedNation.ToString(CultureInfo.InvariantCulture) +
                              " radius=" + OriginalGlobalBrigRadiusV172LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " scale=" + scale.ToString("0.###", CultureInfo.InvariantCulture) +
                              " radiusWorld=" + radiusWorld.ToString("0.###", CultureInfo.InvariantCulture) +
                              " cached=1 mode=COMMANDCENTER_hover_like_GlobalBrigDialog");
                }
                return;
            }

            if (_weaponRangeRootV154LikeOriginal == null)
                _weaponRangeRootV154LikeOriginal = new GameObject("C2_GlobalBrig_COMMANDCENTER_Range_V172_ORIGINAL_RADIUS_1500");

            ClearWeaponRangeDisksV157LikeOriginal();

            for (int i = 0; i < centers.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal center = centers[i];
                if (center == null) continue;
                Vector3 world = ResolveGlobalBrigBuildingWorldV172LikeOriginal(center);
                world.y += 0.70f;
                Vector3[] outer = BuildGlobalBrigCircleWorldV172LikeOriginal(world, radiusWorld, 192);
                AddWeaponRangeCenterFanV162LikeOriginal(
                    "global_brig_commandcenter_radius1500_" + i.ToString(CultureInfo.InvariantCulture),
                    world,
                    outer,
                    C2GlobalBrigRangeFillOuterV172LikeOriginal,
                    C2GlobalBrigRangeFillInnerV172LikeOriginal,
                    i);
                AddWeaponRangeLineLoopV162LikeOriginal(
                    "global_brig_commandcenter_radius1500_line_" + i.ToString(CultureInfo.InvariantCulture),
                    outer,
                    C2GlobalBrigRangeLineV172LikeOriginal,
                    4 + i);
            }

            DisableWeaponRangeScreenAndGuiV162LikeOriginal();
            _weaponRangeRootV154LikeOriginal.SetActive(true);
            _globalBrigRangeActiveV172LikeOriginal = true;
            s_lastGlobalBrigRangeBuildKeyV172LikeOriginal = buildKey;

            if (forceAudit || _lastGlobalBrigRangeAuditKeyV172LikeOriginal != auditKey || Time.realtimeSinceStartup >= _nextGlobalBrigRangeAuditV172LikeOriginal)
            {
                _lastGlobalBrigRangeAuditKeyV172LikeOriginal = auditKey;
                _nextGlobalBrigRangeAuditV172LikeOriginal = Time.realtimeSinceStartup + 1.0f;
                Debug.Log("[C2:GLOBAL BRIG RANGE V172] centers=" + centers.Count.ToString(CultureInfo.InvariantCulture) +
                          " nation=" + wantedNation.ToString(CultureInfo.InvariantCulture) +
                          " radius=" + OriginalGlobalBrigRadiusV172LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " scale=" + scale.ToString("0.###", CultureInfo.InvariantCulture) +
                          " radiusWorld=" + radiusWorld.ToString("0.###", CultureInfo.InvariantCulture) +
                          " mode=COMMANDCENTER_hover_like_GlobalBrigDialog");
            }
        }

        private void HideGlobalBrigCommandCenterRangeV172LikeOriginal()
        {
            if (!_globalBrigRangeActiveV172LikeOriginal)
                return;
            _globalBrigRangeActiveV172LikeOriginal = false;
            _globalBrigHoverCenterV172LikeOriginal = null;
            if (_weaponRangeRootV154LikeOriginal != null)
            {
                _weaponRangeRootV154LikeOriginal.SetActive(false);
            }
            if (_weaponRangeScreenRootV160LikeOriginal != null)
                _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
            ClearWeaponRangeGuiOverlayV161LikeOriginal();
        }

        private static string BuildGlobalBrigRangeBuildKeyV172LikeOriginal(
            List<C2SettlementBuildingSelectableV1LikeOriginal> centers,
            int wantedNation,
            float radiusWorld)
        {
            StringBuilder sb = new StringBuilder(128);
            sb.Append(wantedNation.ToString(CultureInfo.InvariantCulture));
            sb.Append('|');
            sb.Append(radiusWorld.ToString("0.###", CultureInfo.InvariantCulture));
            if (centers != null)
            {
                for (int i = 0; i < centers.Count; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal center = centers[i];
                    if (center == null) continue;
                    sb.Append('|');
                    sb.Append(center.GetEntityId().ToString());
                    sb.Append(':');
                    sb.Append(center.RealX.ToString(CultureInfo.InvariantCulture));
                    sb.Append(',');
                    sb.Append(center.RealY.ToString(CultureInfo.InvariantCulture));
                    sb.Append(',');
                    sb.Append(center.Nation.ToString(CultureInfo.InvariantCulture));
                    sb.Append(',');
                    sb.Append(center.ReadyLikeOriginal ? '1' : '0');
                }
            }
            return sb.ToString();
        }

        private static float ResolveGlobalBrigMapPixelToWorldScaleV172LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal center)
        {
            float scale = center != null ? center.MapPixelToWorld : 0.0f;
            if (center != null && scale <= 0.0001f && center.OwnerMode != null)
                scale = center.OwnerMode.C2MapPixelToWorldScaleV277LikeOriginal();
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.0001f)
                scale = 1.0f;
            return scale;
        }

        private static Vector3 ResolveGlobalBrigBuildingWorldV172LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal center)
        {
            if (center == null) return Vector3.zero;
            C2BattleTerrainMode mode = center.OwnerMode != null ? center.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode != null)
                return mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(center.RealX / 16.0f, center.RealY / 16.0f);
            return center.transform.position;
        }

        private static Vector3[] BuildGlobalBrigCircleWorldV172LikeOriginal(Vector3 center, float radiusWorld, int segments)
        {
            segments = Mathf.Clamp(segments, 32, 384);
            Vector3[] result = new Vector3[segments];
            float radius = Mathf.Max(0.01f, radiusWorld);
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2.0f * i / segments;
                result[i] = new Vector3(center.x + Mathf.Cos(a) * radius, center.y, center.z + Mathf.Sin(a) * radius);
            }
            return result;
        }

        private static void EnsureGlobalBrigPrioLoadedV172LikeOriginal()
        {
            if (s_globalBrigPrioLoadedV172LikeOriginal) return;
            s_globalBrigPrioLoadedV172LikeOriginal = true;
            s_globalBrigPrioByUnitV172LikeOriginal.Clear();

            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            int index = 0;
            for (int r = 0; roots != null && r < roots.Length; r++)
            {
                string[] paths =
                {
                    Path.Combine(roots[r], "Dip", "GBrigPrio.txt"),
                    Path.Combine(roots[r], "GBrigPrio.txt")
                };
                for (int p = 0; p < paths.Length; p++)
                {
                    if (string.IsNullOrWhiteSpace(paths[p]) || !File.Exists(paths[p])) continue;
                    string[] lines = ReadGlobalBrigPrioLines1251V172LikeOriginal(paths[p]);
                    if (lines == null) continue;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(lines[i]);
                        if (line.Length == 0) continue;
                        string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
                        if (t.Length == 0 || string.IsNullOrWhiteSpace(t[0])) continue;
                        if (!s_globalBrigPrioByUnitV172LikeOriginal.ContainsKey(t[0]))
                            s_globalBrigPrioByUnitV172LikeOriginal.Add(t[0], index);
                        index++;
                    }
                    if (s_globalBrigPrioByUnitV172LikeOriginal.Count > 0)
                        return;
                }
            }
        }

        private static int GlobalBrigPriorityV172LikeOriginal(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return int.MaxValue;
            EnsureGlobalBrigPrioLoadedV172LikeOriginal();
            int prio;
            if (s_globalBrigPrioByUnitV172LikeOriginal.TryGetValue(unitId, out prio)) return prio;
            string stripped = C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(unitId);
            if (s_globalBrigPrioByUnitV172LikeOriginal.TryGetValue(stripped, out prio)) return prio;
            return int.MaxValue;
        }

        private static string[] ReadGlobalBrigPrioLines1251V172LikeOriginal(string path)
        {
            try { return File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch
            {
                try { return File.ReadAllLines(path, Encoding.GetEncoding(866)); }
                catch
                {
                    try { return File.ReadAllLines(path); }
                    catch { return null; }
                }
            }
        }

        private const int OriginalBrigCreateRadiusV165LikeOriginal = 800;
        private const int OriginalBrigCreateButtonSizeV165LikeOriginal = 32;
        private const int OriginalBrigCreateButtonGapV165LikeOriginal = 4;
        private const int OriginalBrigCreateMaxVisibleAmountsV165LikeOriginal = 4;

        private string _activeBrigCreateUiKeyV165LikeOriginal = string.Empty;
        private string _hoverBrigCreateKeyV165LikeOriginal = string.Empty;
        private string _lastBrigCreateRangeAuditKeyV165LikeOriginal = string.Empty;
        private float _nextBrigCreateRangeAuditV165LikeOriginal;

        private static readonly Color C2BrigCreateRangeFillOuterV165LikeOriginal = new Color(0.0f, 1.0f, 0.16f, 0.30f);
        private static readonly Color C2BrigCreateRangeFillInnerV165LikeOriginal = new Color(0.0f, 1.0f, 0.16f, 0.08f);
        private static readonly Color C2BrigCreateRangeLineV165LikeOriginal = new Color(0.18f, 1.0f, 0.18f, 0.86f);

        internal void RebuildBrigCreateRangeV165LikeOriginal(bool forceAudit)
        {
#pragma warning disable CS0162
            C2NeutralPeasantUnitInfoV2LikeOriginal unit = _hoverBrigCreateUnitV165LikeOriginal;
            if (unit == null || !unit.isActiveAndEnabled)
            {
                HideBrigCreateRangeV165LikeOriginal();
                return;
            }

            if (_weaponRangeRootV154LikeOriginal == null)
            {
                _weaponRangeRootV154LikeOriginal = new GameObject("C2_BrigCreate_Range_V165_ORIGINAL_RADIUS_800");
                _weaponRangeLineV154LikeOriginal = _weaponRangeRootV154LikeOriginal.AddComponent<LineRenderer>();
                _weaponRangeLineV154LikeOriginal.useWorldSpace = true;
                _weaponRangeLineV154LikeOriginal.loop = true;
                _weaponRangeLineV154LikeOriginal.positionCount = 192;
                _weaponRangeLineV154LikeOriginal.widthMultiplier = 0.070f;
                _weaponRangeLineV154LikeOriginal.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _weaponRangeLineV154LikeOriginal.receiveShadows = false;
                _weaponRangeLineV154LikeOriginal.enabled = false;
                _weaponRangeLineV154LikeOriginal.sortingOrder = 5000;
                _weaponRangeLineV154LikeOriginal.material = CreateWeaponRangeVisibleMaterialV160LikeOriginal(C2BrigCreateRangeLineV165LikeOriginal, 3656);
            }

            ClearWeaponRangeDisksV157LikeOriginal();

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> selectedSameType = GetSelectedUnitsOfSelPointKeyV159LikeOriginal(UnitSelPointKeyV137LikeOriginal(unit));
            if (selectedSameType == null || selectedSameType.Count == 0)
            {
                selectedSameType = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(1);
                selectedSameType.Add(unit);
            }

            Vector3 center = ComputeSelectedUnitsWorldCenterV159LikeOriginal(selectedSameType, unit);
            center.y += 0.70f;

            float scale = Mathf.Max(0.01f, Mathf.Abs(unit.MapPixelToWorld));
            float radiusWorld = Mathf.Clamp(OriginalBrigCreateRadiusV165LikeOriginal * scale, 1.0f, 4096.0f);
            Vector3[] outer = BuildSelectedUnitsRangeEnvelopeV159LikeOriginal(selectedSameType, center, radiusWorld, 192);

            AddWeaponRangeCenterFanV162LikeOriginal(
                "brig_create_radius800_green",
                center,
                outer,
                C2BrigCreateRangeFillOuterV165LikeOriginal,
                C2BrigCreateRangeFillInnerV165LikeOriginal,
                0);
            AddWeaponRangeLineLoopV162LikeOriginal("brig_create_radius800_line", outer, C2BrigCreateRangeLineV165LikeOriginal, 4);
            DisableWeaponRangeScreenAndGuiV162LikeOriginal();

            int nearCount = CountBrigCreateCandidateUnitsV165LikeOriginal(unit);
            string auditKey = UnitSelPointKeyV137LikeOriginal(unit) + "#" + nearCount.ToString(CultureInfo.InvariantCulture) + "#" + selectedSameType.Count.ToString(CultureInfo.InvariantCulture);
            if (forceAudit || _lastBrigCreateRangeAuditKeyV165LikeOriginal != auditKey || Time.realtimeSinceStartup >= _nextBrigCreateRangeAuditV165LikeOriginal)
            {
                _lastBrigCreateRangeAuditKeyV165LikeOriginal = auditKey;
                _nextBrigCreateRangeAuditV165LikeOriginal = Time.realtimeSinceStartup + 1.0f;
                Debug.Log("[C2:BRIG CREATE RANGE V165] unit='" + (unit.SourceMonsterId ?? unit.ResolvedMd ?? string.Empty) +
                          "' key='" + UnitSelPointKeyV137LikeOriginal(unit) +
                          "' radius=" + OriginalBrigCreateRadiusV165LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " scale=" + scale.ToString("0.###", CultureInfo.InvariantCulture) +
                          " radiusWorld=" + radiusWorld.ToString("0.###", CultureInfo.InvariantCulture) +
                          " selectedSameType=" + selectedSameType.Count.ToString(CultureInfo.InvariantCulture) +
                          " nearbySameType=" + nearCount.ToString(CultureInfo.InvariantCulture) +
                          " mode=cvi_BrigCreate_ui_only");
            }

            _weaponRangeRootV154LikeOriginal.SetActive(true);
#pragma warning restore CS0162
        }

        internal void ShowBrigCreateRangeV165LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, string key)
        {
            if (unit == null || !unit.isActiveAndEnabled)
            {
                HideBrigCreateRangeV165LikeOriginal();
                return;
            }

            _hoverBrigCreateUnitV165LikeOriginal = unit;
            _hoverBrigCreateKeyV165LikeOriginal = key ?? string.Empty;
            RebuildBrigCreateRangeV165LikeOriginal(true);
        }

        internal void HideBrigCreateRangeV165LikeOriginal()
        {
            _hoverBrigCreateUnitV165LikeOriginal = null;
            _hoverBrigCreateKeyV165LikeOriginal = string.Empty;
            if (_weaponRangeRootV154LikeOriginal != null)
                _weaponRangeRootV154LikeOriginal.SetActive(false);
            if (_weaponRangeScreenRootV160LikeOriginal != null)
                _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
            ClearWeaponRangeGuiOverlayV161LikeOriginal();
        }

        internal void RebuildBarracksBrigCreateRangeV166LikeOriginal(bool forceAudit)
        {
            _hoverBrigCreateBuildingV166LikeOriginal = null;
            if (_weaponRangeRootV154LikeOriginal != null)
                _weaponRangeRootV154LikeOriginal.SetActive(false);
            if (_weaponRangeScreenRootV160LikeOriginal != null)
                _weaponRangeScreenRootV160LikeOriginal.SetActive(false);
            ClearWeaponRangeGuiOverlayV161LikeOriginal();
        }

        private bool TryResolveBrigCreateRecordForSelectedUnitV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record)
        {
            record = null;
            if (unit == null) return false;

            // Малые строи (15 егерей/сапёров) создаются самим выбранным солдатом.
            string selectedMember = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(unit);
            if (C2FormationCreateCatalogV165LikeOriginal.TryResolveForSelectedUnit(unit, out record) && record != null)
            {
                bool selfCommand =
                    string.Equals(record.OfficerId ?? string.Empty, selectedMember ?? string.Empty, StringComparison.OrdinalIgnoreCase) ||
                    (string.IsNullOrEmpty(record.OfficerId) &&
                     string.IsNullOrEmpty(record.DrummerId) &&
                     string.IsNullOrEmpty(record.FlagId));
                if (selfCommand) return true;
                record = null;
            }

            // Обычные строи на 120 пехотинцев и 45 кавалеристов открываются
            // через офицера/командный юнит. Находим его NDS-запись по OfficerId.
            if (string.IsNullOrEmpty(selectedMember)) return false;

            List<C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal> all =
                C2FormationCreateCatalogV165LikeOriginal.BuildAllRecordsSnapshotLikeOriginal();
            int bestNearby = -1;
            for (int i = 0; all != null && i < all.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal candidate = all[i];
                if (candidate == null || candidate.Options == null || candidate.Options.Count == 0) continue;
                if (!string.Equals(candidate.OfficerId ?? string.Empty, selectedMember, StringComparison.OrdinalIgnoreCase)) continue;

                int nearby = CountBrigCreateCandidateUnitsV165LikeOriginal(unit, candidate);
                if (nearby > bestNearby)
                {
                    bestNearby = nearby;
                    record = candidate;
                }
            }
            return record != null;
        }

        private void BuildBrigCreateDialogV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int activeGroupCount, int selPointCount)
        {
            if (unit == null || selPointCount != 1) return;
            if (C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit)) return;

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            if (info.Peasant || unit.CanBuildOrRepairLikeOriginal()) return;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record;
            if (!TryResolveBrigCreateRecordForSelectedUnitV172LikeOriginal(unit, out record) || record == null)
                return;

            // Original cvi_BrigCreate::Process works from the selected officer and nearby units only.
            // It does not require a command center. Keep a nearby center only as optional UI audit context.
            C2SettlementBuildingSelectableV1LikeOriginal commandCenter;
            string centerAudit;
            bool hasCommandCenter = TryFindBrigCreateCommandCenterV172LikeOriginal(unit, out commandCenter, out centerAudit);
            if (!hasCommandCenter)
                centerAudit = "original_cvi_BrigCreate_no_commandcenter_required; " + (centerAudit ?? string.Empty);

            List<C2BrigCreateAmountOptionV165LikeOriginal> amountOptions = BuildBrigCreateAmountOptionsV165LikeOriginal(record);
            if (amountOptions.Count == 0) return;

            int panelX = 14;
            int panelY = 92;
            string key = BrigCreateUiKeyV165LikeOriginal(unit, record);
            bool active = string.Equals(_activeBrigCreateUiKeyV165LikeOriginal, key, StringComparison.OrdinalIgnoreCase);
            int nearby = CountBrigCreateCandidateUnitsV165LikeOriginal(unit, record);

            AddBrigCreateButtonV165LikeOriginal(
                "brig_create_active_selpoint_v172",
                panelX,
                panelY,
                active ? 4 : 2,
                active ? "Создать строй: включено" : "Создать строй",
                (commandCenter != null ? "COMMANDCENTER: " + (commandCenter.SourceMonsterId ?? string.Empty) + "; " : string.Empty) +
                    "radius 800; nearby " + nearby.ToString(CultureInfo.InvariantCulture) + "; " + centerAudit,
                unit,
                key,
                -1,
                0,
                true,
                true);

            if (!active) return;

            int visible = Mathf.Min(OriginalBrigCreateMaxVisibleAmountsV165LikeOriginal, amountOptions.Count);
            for (int i = 0; i < visible; i++)
            {
                C2BrigCreateAmountOptionV165LikeOriginal option = amountOptions[i];
                int required = Mathf.Max(1, option.UnitCount);
                bool enabled = nearby >= required;
                int x = panelX + (i + 1) * (OriginalBrigCreateButtonSizeV165LikeOriginal + OriginalBrigCreateButtonGapV165LikeOriginal);
                AddBrigCreateButtonV165LikeOriginal(
                    "brig_create_amount_selpoint_v172_" + i.ToString(CultureInfo.InvariantCulture),
                    x,
                    panelY,
                    option.IconSprite,
                    "Создать строй: " + required.ToString(CultureInfo.InvariantCulture),
                    option.Shape + " amountIndex=" + option.AmountIndex.ToString(CultureInfo.InvariantCulture) +
                        " nearby " + nearby.ToString(CultureInfo.InvariantCulture) + "/" + required.ToString(CultureInfo.InvariantCulture) +
                        "; " + centerAudit,
                    unit,
                    key,
                    option.AmountIndex,
                    required,
                    enabled,
                    false);
            }
        }

        private int BuildFormationCommandButtonsV165LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
            int activeGroupCount,
            int selPointCount,
            int baseX,
            int baseY,
            int renderedWeaponCards)
        {
            return BuildOriginalFormationMiniComV320LikeOriginal(
                unit,
                record,
                activeGroupCount,
                selPointCount,
                baseX,
                baseY,
                renderedWeaponCards);
        }

        private static int FormationCommandPanelOffsetXV165LikeOriginal(int renderedWeaponCards)
        {
            if (renderedWeaponCards >= 2) return 138;
            if (renderedWeaponCards == 1) return 70;
            return 0;
        }

        private sealed class C2BrigCreateAmountOptionV165LikeOriginal
        {
            public int AmountIndex;
            public int UnitCount;
            public int IconSprite;
            public string Shape = string.Empty;
        }

        private static List<C2BrigCreateAmountOptionV165LikeOriginal> BuildBrigCreateAmountOptionsV165LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record)
        {
            var result = new List<C2BrigCreateAmountOptionV165LikeOriginal>();
            if (record == null) return result;

            for (int i = 0; i < record.Options.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = record.Options[i];
                if (option == null) continue;
                int amountIndex = Mathf.Max(0, option.AmountIndex);
                int unitCount = option.UnitCount;
                bool exists = false;
                for (int r = 0; r < result.Count; r++)
                {
                    if (result[r].AmountIndex == amountIndex && result[r].UnitCount == unitCount)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                var dst = new C2BrigCreateAmountOptionV165LikeOriginal();
                dst.AmountIndex = amountIndex;
                dst.UnitCount = unitCount;
                dst.IconSprite = Mathf.Max(0, option.IconSprite);
                dst.Shape = option.Shape ?? string.Empty;
                result.Add(dst);
            }

            result.Sort(delegate (C2BrigCreateAmountOptionV165LikeOriginal a, C2BrigCreateAmountOptionV165LikeOriginal b)
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                int n = a.AmountIndex.CompareTo(b.AmountIndex);
                if (n != 0) return n;
                n = a.UnitCount.CompareTo(b.UnitCount);
                if (n != 0) return n;
                return string.Compare(a.Shape, b.Shape, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        private void AddBrigCreateButtonV165LikeOriginal(
            string name,
            int x,
            int y,
            int iconSprite,
            string title,
            string detail,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string key,
            int amountIndex,
            int requiredAmount,
            bool enabled,
            bool activeToggle)
        {
            int alpha = enabled ? 255 : 150;
            AddG16ImageOverpaintV140LikeOriginal(
                name + "_back",
                "Interf3\\FormInterface",
                18,
                x,
                y,
                OriginalBrigCreateButtonSizeV165LikeOriginal,
                OriginalBrigCreateButtonSizeV165LikeOriginal,
                alpha,
                false,
                56,
                false,
                false);

            AddG16ImageOverpaintV140LikeOriginal(
                name + "_icon",
                "Interf3\\f_icons",
                Mathf.Max(0, iconSprite),
                x + 3,
                y + 3,
                OriginalBrigCreateButtonSizeV165LikeOriginal - 6,
                OriginalBrigCreateButtonSizeV165LikeOriginal - 6,
                alpha,
                false,
                80,
                false,
                true);

            if (!activeToggle && requiredAmount > 0)
            {
                AddCrispLabelV140LikeOriginal(
                    name + "_amount",
                    requiredAmount.ToString(CultureInfo.InvariantCulture),
                    x + 1,
                    y + OriginalBrigCreateButtonSizeV165LikeOriginal - 11,
                    OriginalBrigCreateButtonSizeV165LikeOriginal - 2,
                    10,
                    8,
                    TextAnchor.MiddleCenter,
                    enabled ? Color.white : new Color(1.0f, 1.0f, 1.0f, 0.62f));
            }

            GameObject go = NewUi(name + "_click");
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            C2HudBrigCreateRelayV165LikeOriginal relay = go.AddComponent<C2HudBrigCreateRelayV165LikeOriginal>();
            relay.Owner = this;
            relay.Unit = unit;
            relay.Key = key ?? string.Empty;
            relay.AmountIndex = amountIndex;
            relay.RequiredAmount = requiredAmount;
            relay.Enabled = enabled;
            relay.ActiveToggle = activeToggle;

            C2HudTooltipRelayV13I tooltip = go.AddComponent<C2HudTooltipRelayV13I>();
            tooltip.Owner = this;
            tooltip.Item = BuildBrigCreateTooltipV165LikeOriginal(title, detail, unit);

            Place(go.GetComponent<RectTransform>(), x, y, OriginalBrigCreateButtonSizeV165LikeOriginal, OriginalBrigCreateButtonSizeV165LikeOriginal);
        }

        private static C2OriginalProduceItemV13 BuildBrigCreateTooltipV165LikeOriginal(string title, string detail, C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            var item = new C2OriginalProduceItemV13();
            item.DisplayText = title ?? string.Empty;
            item.MdName = detail ?? string.Empty;
            item.UnitId = unit != null ? (unit.SourceMonsterId ?? string.Empty) : string.Empty;
            return item;
        }

        private string BrigCreateUiKeyV165LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record)
        {
            string unitKey = UnitSelPointKeyV137LikeOriginal(unit);
            string recordKey = record != null ? (record.UnitId ?? string.Empty) : string.Empty;
            return unitKey + "|" + recordKey + "|" + (unit != null ? unit.Nation.ToString(CultureInfo.InvariantCulture) : "0");
        }

        private bool TryFindBrigCreateCommandCenterV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out C2SettlementBuildingSelectableV1LikeOriginal commandCenter,
            out string audit)
        {
            commandCenter = null;
            audit = "no_unit";
            if (unit == null) return false;

            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            float radiusReal = OriginalBrigCreateRadiusV165LikeOriginal * 16.0f;
            float radius2 = radiusReal * radiusReal;
            float best2 = float.MaxValue;
            int scanned = 0;
            int commandCenters = 0;

            C2SettlementBuildingSelectableV1LikeOriginal[] all = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null || !b.isActiveAndEnabled || !b.gameObject.activeInHierarchy) continue;
                scanned++;
                if (b.NotSelectable || !b.ReadyLikeOriginal) continue;
                if (b.Nation != unit.Nation) continue;

                C2OriginalProduceCatalogV13.C2MdIconInfoV13 bi = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(b);
                if (!bi.CommandCenter && !bi.GlobalCommandCenter) continue;
                commandCenters++;

                float dx = b.RealX - ux;
                float dy = b.RealY - uy;
                float d2 = dx * dx + dy * dy;
                if (d2 > radius2) continue;
                if (d2 < best2)
                {
                    best2 = d2;
                    commandCenter = b;
                }
            }

            if (commandCenter == null)
            {
                audit = "no_commandcenter_in_radius scanned=" + scanned.ToString(CultureInfo.InvariantCulture) +
                        " commandCentersSameNation=" + commandCenters.ToString(CultureInfo.InvariantCulture) +
                        " radius=" + OriginalBrigCreateRadiusV165LikeOriginal.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            audit = "commandcenter='" + (commandCenter.SourceMonsterId ?? string.Empty) +
                    "' dist=" + Mathf.RoundToInt(Mathf.Sqrt(best2) / 16.0f).ToString(CultureInfo.InvariantCulture) +
                    " radius=" + OriginalBrigCreateRadiusV165LikeOriginal.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private int CountBrigCreateCandidateUnitsV165LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal centerUnit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record = null)
        {
            if (centerUnit == null) return 0;
            float centerX = centerUnit.RealXFloat != 0.0f ? centerUnit.RealXFloat : centerUnit.RealX;
            float centerY = centerUnit.RealYFloat != 0.0f ? centerUnit.RealYFloat : centerUnit.RealY;
            float radiusReal = OriginalBrigCreateRadiusV165LikeOriginal * 16.0f;
            float radius2 = radiusReal * radiusReal;
            string memberId = record != null && !string.IsNullOrEmpty(record.UnitId)
                ? record.UnitId
                : C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(centerUnit);
            int count = 0;

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled) continue;
                if (u.NotSelectable || u.IsDeadLikeOriginal) continue;
                if (u.Nation != centerUnit.Nation) continue;
                if (!string.Equals(
                        C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(u),
                        memberId,
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                if (C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u)) continue;

                float ux = u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                float uy = u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                float dx = ux - centerX;
                float dy = uy - centerY;
                if (dx * dx + dy * dy <= radius2)
                    count++;
            }

            return count;
        }

        internal void OnBrigCreateActiveClickedV165LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, string key)
        {
            if (unit == null || string.IsNullOrEmpty(key)) return;

            bool nowActive;
            if (string.Equals(_activeBrigCreateUiKeyV165LikeOriginal, key, StringComparison.OrdinalIgnoreCase))
            {
                _activeBrigCreateUiKeyV165LikeOriginal = string.Empty;
                nowActive = false;
            }
            else
            {
                _activeBrigCreateUiKeyV165LikeOriginal = key;
                nowActive = true;
            }

            _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
            _lastSelectedCount = -999999;
            _nextRefresh = 0.0f;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();

            Debug.Log("[C2:BRIG CREATE V165 ACTIVE] unit='" + (unit.SourceMonsterId ?? unit.ResolvedMd ?? string.Empty) +
                      "' key='" + key +
                      "' active=" + nowActive.ToString() +
                      " radius=" + OriginalBrigCreateRadiusV165LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " nearbySameType=" + CountBrigCreateCandidateUnitsV165LikeOriginal(unit).ToString(CultureInfo.InvariantCulture) +
                      " mode=cvi_BrigCreate_ui_only");
        }

        internal void OnBrigCreateAmountClickedV165LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string key,
            int amountIndex,
            int requiredAmount,
            bool enabled)
        {
            if (unit == null) return;

            int nearby = CountBrigCreateCandidateUnitsV165LikeOriginal(unit);
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            string result;
            if (!enabled)
            {
                result = "disabled_not_enough_nearby";
            }
            else
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record;
                C2SettlementBuildingSelectableV1LikeOriginal commandCenter;
                string centerAudit;
                if (!TryResolveBrigCreateRecordForSelectedUnitV172LikeOriginal(unit, out record) || record == null)
                {
                    result = "no_formation_record";
                }
                else
                {
                    // Original cvi_BrigCreate does not require a command center.
                    // Resolve one only as optional context for compatibility/audit.
                    TryFindBrigCreateCommandCenterV172LikeOriginal(unit, out commandCenter, out centerAudit);
                    int groupId;
                    string createAudit;
                    bool ok = C2FormationRuntimeV167LikeOriginal.TryCreateBrigInZoneV172LikeOriginal(
                        unit,
                        record,
                        amountIndex,
                        requiredAmount,
                        commandCenter,
                        out groupId,
                        out createAudit);
                    result = createAudit;
                    if (ok)
                    {
                        _activeBrigCreateUiKeyV165LikeOriginal = string.Empty;
                        _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
                        _lastSelectedCount = -999999;
                        _nextRefresh = 0.0f;
                    }
                }
            }

            Debug.Log("[C2:BRIG CREATE V165 AMOUNT] unit='" + (unit.SourceMonsterId ?? unit.ResolvedMd ?? string.Empty) +
                      "' key='" + (key ?? string.Empty) +
                      "' amountIndex=" + amountIndex.ToString(CultureInfo.InvariantCulture) +
                      " required=" + requiredAmount.ToString(CultureInfo.InvariantCulture) +
                      " nearbySameType=" + nearby.ToString(CultureInfo.InvariantCulture) +
                      " enabled=" + enabled.ToString() +
                      " result=" + result);
        }
    }

    internal sealed class C2HudGlobalBrigRelayV172LikeOriginal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public C2GameplayHudV1 Owner;
        public C2SettlementBuildingSelectableV1LikeOriginal Center;
        public C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal Record;
        public C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal Option;
        public string MemberId = string.Empty;
        public bool Enabled;
        public bool Clickable = true;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Owner == null) return;
            Owner.OnGlobalBrigHoverV172LikeOriginal(Center, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Owner == null) return;
            Owner.OnGlobalBrigHoverV172LikeOriginal(Center, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Owner == null || eventData == null) return;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            eventData.Use();
            if (!Clickable)
                return;
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!string.IsNullOrEmpty(MemberId))
                Owner.OnGlobalBrigMemberClickedV172LikeOriginal(Center, MemberId, Enabled);
            else
                Owner.OnGlobalBrigClickedV172LikeOriginal(Center, Record, Option, Enabled);
        }
    }

    internal sealed class C2HudBrigCreateRelayV165LikeOriginal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public C2GameplayHudV1 Owner;
        public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
        public string Key;
        public int AmountIndex;
        public int RequiredAmount;
        public bool Enabled;
        public bool ActiveToggle;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Owner != null) Owner.ShowBrigCreateRangeV165LikeOriginal(Unit, Key);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Owner != null) Owner.HideBrigCreateRangeV165LikeOriginal();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Owner == null || eventData == null) return;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            eventData.Use();

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (ActiveToggle)
                Owner.OnBrigCreateActiveClickedV165LikeOriginal(Unit, Key);
            else
                Owner.OnBrigCreateAmountClickedV165LikeOriginal(Unit, Key, AmountIndex, RequiredAmount, Enabled);
        }
    }


}
