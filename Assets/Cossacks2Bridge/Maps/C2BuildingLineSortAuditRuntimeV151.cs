using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V152: safe LINESORT audit + dy-facing bucket audit.
    //
    // What this does:
    //   - keeps the already assigned C2Settlement3InuMdV2SortOrderLikeOriginal sortingOrder; in V152 this includes dy-positive LINE back bucket;
    //   - parses and stores MD LINESORT part descriptors;
    //   - logs sprite id / part / GROUND-LINE-TOP / current sortingOrder;
    //   - when a unit is available, logs who is actually above whom by the current Unity sortingOrder.
    //
    // What this does NOT do:
    //   - does not change MeshRenderer.sortingOrder at runtime;
    //   - does not use one "nearest unit" to push all LINE parts in front/back; V152 bucket is static by LINE dy;
    //   - does not deform, slice, project, or scale any building sprite.
    internal class C2BuildingLineSortAuditSorterV151LikeOriginal : MonoBehaviour
    {
        public const int SortKindFallback = 0;
        public const int SortKindGround = 1;
        public const int SortKindLine = 2;
        public const int SortKindTop = 3;

        private const bool EnabledV151 = true;
        private const bool AuditInitialV151 = true;
        private const bool AuditStateChangesV151 = true;
        private const bool AuditProductionForceV151 = true;

        private const float RelevantUnitRadiusRealV151 = 768.0f * 16.0f;
        private const int UnitCacheRefreshFramesV151 = 6;
        private const int MaxTickLogsPerBuildingV151 = 48;

        private sealed class Part
        {
            public MeshRenderer Renderer;
            public int PartIndex;
            public int SpriteId;
            public string AnimationName;
            public int SortKind;
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;
            public int PivotDx;
            public int PivotDy;
            public int StaticOrder;
            public string LastRelation = string.Empty;
            public int LastUnitRecord = int.MinValue;
            public int LastRendererOrder = int.MinValue;
        }

        private readonly List<Part> _parts = new List<Part>();
        private int _recordIndex;
        private string _monsterId = string.Empty;
        private string _mdName = string.Empty;
        private string _kindName = string.Empty;
        private int _buildingRealX;
        private int _buildingRealY;
        private int _tickLogsLeft = MaxTickLogsPerBuildingV151;
        private int _lastUpdateFrame = -1;

        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] s_unitsV151;
        private static int s_unitsFrameV151 = -999999;

        public void Configure(int recordIndex, string monsterId, string mdName, string kindName, int realX, int realY)
        {
            _recordIndex = recordIndex;
            _monsterId = monsterId ?? string.Empty;
            _mdName = mdName ?? string.Empty;
            _kindName = kindName ?? string.Empty;
            _buildingRealX = realX;
            _buildingRealY = realY;
        }

        public void AddPart(
            MeshRenderer renderer,
            int partIndex,
            int spriteId,
            string animationName,
            int sortKind,
            int x1,
            int y1,
            int x2,
            int y2,
            int pivotDx,
            int pivotDy,
            int staticOrder)
        {
            if (renderer == null)
                return;

            var p = new Part();
            p.Renderer = renderer;
            p.PartIndex = partIndex;
            p.SpriteId = spriteId;
            p.AnimationName = animationName ?? string.Empty;
            p.SortKind = sortKind;
            p.X1 = x1;
            p.Y1 = y1;
            p.X2 = x2;
            p.Y2 = y2;
            p.PivotDx = pivotDx;
            p.PivotDy = pivotDy;
            p.StaticOrder = staticOrder;
            _parts.Add(p);
        }

        public void CommitAndLogInitialState()
        {
            if (!EnabledV151 || _parts.Count == 0)
                return;

            // V151 critical: do not write renderer.sortingOrder here.
            if (!AuditInitialV151)
                return;

            var sb = new StringBuilder(512);
            sb.Append("[C2:BUILD LINESORT V152 INIT] building=").Append(_recordIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(" name='").Append(_monsterId).Append("' md='").Append(_mdName).Append("' kind=").Append(_kindName);
            sb.Append(" real=").Append(_buildingRealX.ToString(CultureInfo.InvariantCulture)).Append(',').Append(_buildingRealY.ToString(CultureInfo.InvariantCulture));
            sb.Append(" parts=").Append(_parts.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(" rule=dy_bucket_sorting_audit no_dynamic_reorder_runtime no_mesh_deform");
            for (int i = 0; i < _parts.Count; i++)
            {
                Part p = _parts[i];
                sb.Append(" | part=").Append(p.PartIndex.ToString(CultureInfo.InvariantCulture));
                sb.Append(" sprite=").Append(p.SpriteId.ToString(CultureInfo.InvariantCulture));
                sb.Append(" anim='").Append(p.AnimationName).Append("'");
                sb.Append(" sort=").Append(SortKindNameV151(p.SortKind));
                if (p.SortKind == SortKindLine)
                {
                    sb.Append(" line=").Append(p.X1.ToString(CultureInfo.InvariantCulture)).Append(',').Append(p.Y1.ToString(CultureInfo.InvariantCulture));
                    sb.Append("->").Append(p.X2.ToString(CultureInfo.InvariantCulture)).Append(',').Append(p.Y2.ToString(CultureInfo.InvariantCulture));
                    sb.Append(" dy=").Append(LineDyV152(p).ToString(CultureInfo.InvariantCulture));
                    sb.Append(" face=").Append(LineFacingNameV152(p));
                    sb.Append(" pivot=").Append(p.PivotDx.ToString(CultureInfo.InvariantCulture)).Append(',').Append(p.PivotDy.ToString(CultureInfo.InvariantCulture));
                }
                sb.Append(" staticOrder=").Append(p.StaticOrder.ToString(CultureInfo.InvariantCulture));
                sb.Append(" currentOrder=").Append((p.Renderer != null ? p.Renderer.sortingOrder : p.StaticOrder).ToString(CultureInfo.InvariantCulture));
            }
            Debug.Log(sb.ToString());

            // Also print per-part init lines so filtering by sprite id is easy.
            for (int i = 0; i < _parts.Count; i++)
            {
                Part p = _parts[i];
                if (p == null || p.Renderer == null) continue;
                Debug.Log(BuildPartAuditLineV151(p, null, "init_building"));
            }
        }

        private void LateUpdate()
        {
            if (!EnabledV151 || !AuditStateChangesV151 || _parts.Count == 0)
                return;

            if (_lastUpdateFrame == Time.frameCount)
                return;

            _lastUpdateFrame = Time.frameCount;

            // Keep tick logs limited. Runtime/preview buildings are the only important noisy case.
            if (_recordIndex < 900000 || _tickLogsLeft <= 0)
                return;

            C2NeutralPeasantUnitInfoV2LikeOriginal unit = FindNearestRelevantUnitV151();
            if (unit == null)
                return;

            for (int i = 0; i < _parts.Count; i++)
            {
                Part p = _parts[i];
                if (p == null || p.Renderer == null || p.SortKind != SortKindLine)
                    continue;

                string relation = ResolveActualRelationV151(p, unit, ComputeLineSideForAuditV151(p, unit));
                int order = p.Renderer.sortingOrder;
                int unitRecord = unit.RecordIndex;
                if (p.LastUnitRecord == unitRecord &&
                    p.LastRendererOrder == order &&
                    string.Equals(p.LastRelation, relation, StringComparison.Ordinal))
                    continue;

                p.LastUnitRecord = unitRecord;
                p.LastRendererOrder = order;
                p.LastRelation = relation;

                _tickLogsLeft--;
                Debug.Log(BuildPartAuditLineV151(p, unit, "tick"));
                if (_tickLogsLeft <= 0)
                    break;
            }
        }

        public void ForceAuditAgainstUnitV151(C2NeutralPeasantUnitInfoV2LikeOriginal unit, string reason)
        {
            if (!EnabledV151 || _parts.Count == 0)
                return;

            for (int i = 0; i < _parts.Count; i++)
            {
                Part p = _parts[i];
                if (p == null || p.Renderer == null)
                    continue;

                Debug.Log(BuildPartAuditLineV151(p, unit, reason ?? "force_audit"));
            }
        }

        public static void LogBuildingVsUnitForAllParts(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string reason)
        {
            if (!AuditProductionForceV151 || building == null || unit == null)
                return;

            C2BuildingLineSortAuditSorterV151LikeOriginal sorter = building.GetComponent<C2BuildingLineSortAuditSorterV151LikeOriginal>();
            if (sorter == null)
            {
                Debug.Log("[C2:BUILD LINESORT V152 PRODUCE] building=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                          " name='" + (building.SourceMonsterId ?? string.Empty) + "'" +
                          " unit=" + unit.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                          " unitName='" + (unit.SourceMonsterId ?? string.Empty) + "'" +
                          " reason='" + (reason ?? string.Empty) + "' no_LINESORT_audit_on_building");
                return;
            }

            sorter.ForceAuditAgainstUnitV151(unit, "production_complete " + (reason ?? string.Empty));
        }

        private C2NeutralPeasantUnitInfoV2LikeOriginal FindNearestRelevantUnitV151()
        {
            RefreshUnitCacheV151();
            if (s_unitsV151 == null || s_unitsV151.Length == 0)
                return null;

            float bestD2 = RelevantUnitRadiusRealV151 * RelevantUnitRadiusRealV151;
            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            for (int i = 0; i < s_unitsV151.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = s_unitsV151[i];
                if (u == null || !u.isActiveAndEnabled)
                    continue;

                float dx = u.RealXFloat - _buildingRealX;
                float dy = u.RealYFloat - _buildingRealY;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = u;
                }
            }
            return best;
        }

        private static void RefreshUnitCacheV151()
        {
            int frame = Time.frameCount;
            if (s_unitsV151 != null && frame - s_unitsFrameV151 < UnitCacheRefreshFramesV151)
                return;

            s_unitsFrameV151 = frame;
            s_unitsV151 = UnityEngine.Object.FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        }

        private string BuildPartAuditLineV151(Part p, C2NeutralPeasantUnitInfoV2LikeOriginal unit, string reason)
        {
            int currentOrder = p.Renderer != null ? p.Renderer.sortingOrder : p.StaticOrder;
            float side = unit != null && p.SortKind == SortKindLine ? ComputeLineSideForAuditV151(p, unit) : 0.0f;
            string relation = unit != null
                ? ResolveActualRelationV151(p, unit, side)
                : "NO_UNIT currentOrder=" + currentOrder.ToString(CultureInfo.InvariantCulture);

            string unitText = unit != null
                ? (" unit=" + unit.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                   " unitName='" + (unit.SourceMonsterId ?? string.Empty) + "'" +
                   " unitReal=" + Mathf.RoundToInt(unit.RealXFloat).ToString(CultureInfo.InvariantCulture) + "," + Mathf.RoundToInt(unit.RealYFloat).ToString(CultureInfo.InvariantCulture) +
                   " unitOrder=" + unit.SortKey.ToString(CultureInfo.InvariantCulture))
                : " unit=<none>";

            return "[C2:BUILD LINESORT V152 PART] reason='" + (reason ?? string.Empty) + "'" +
                   " building=" + _recordIndex.ToString(CultureInfo.InvariantCulture) +
                   " name='" + _monsterId + "' md='" + _mdName + "'" +
                   " buildingReal=" + _buildingRealX.ToString(CultureInfo.InvariantCulture) + "," + _buildingRealY.ToString(CultureInfo.InvariantCulture) +
                   unitText +
                   " part=" + p.PartIndex.ToString(CultureInfo.InvariantCulture) +
                   " sprite=" + p.SpriteId.ToString(CultureInfo.InvariantCulture) +
                   " anim='" + p.AnimationName + "'" +
                   " sort=" + SortKindNameV151(p.SortKind) +
                   (p.SortKind == SortKindLine
                       ? (" line=" + p.X1.ToString(CultureInfo.InvariantCulture) + "," + p.Y1.ToString(CultureInfo.InvariantCulture) + "->" + p.X2.ToString(CultureInfo.InvariantCulture) + "," + p.Y2.ToString(CultureInfo.InvariantCulture) +
                          " dy=" + LineDyV152(p).ToString(CultureInfo.InvariantCulture) +
                          " face=" + LineFacingNameV152(p) +
                          " pivot=" + p.PivotDx.ToString(CultureInfo.InvariantCulture) + "," + p.PivotDy.ToString(CultureInfo.InvariantCulture) +
                          " sideAudit=" + side.ToString("0.###", CultureInfo.InvariantCulture))
                       : string.Empty) +
                   " staticOrder=" + p.StaticOrder.ToString(CultureInfo.InvariantCulture) +
                   " currentOrder=" + currentOrder.ToString(CultureInfo.InvariantCulture) +
                   " relation=" + relation +
                   " note=no_runtime_sortingOrder_write";
        }

        private string ResolveActualRelationV151(Part p, C2NeutralPeasantUnitInfoV2LikeOriginal unit, float side)
        {
            int partOrder = p.Renderer != null ? p.Renderer.sortingOrder : p.StaticOrder;
            int unitOrder = unit != null ? unit.SortKey : 0;

            if (unit == null)
                return "NO_UNIT";

            if (partOrder > unitOrder)
                return "SPRITE_OVER_UNIT by_sortingOrder partOrder=" + partOrder.ToString(CultureInfo.InvariantCulture) +
                       " unitOrder=" + unitOrder.ToString(CultureInfo.InvariantCulture) +
                       " sideAudit=" + side.ToString("0.###", CultureInfo.InvariantCulture);

            if (partOrder < unitOrder)
                return "UNIT_OVER_SPRITE by_sortingOrder partOrder=" + partOrder.ToString(CultureInfo.InvariantCulture) +
                       " unitOrder=" + unitOrder.ToString(CultureInfo.InvariantCulture) +
                       " sideAudit=" + side.ToString("0.###", CultureInfo.InvariantCulture);

            return "TIE same_sortingOrder partOrder=" + partOrder.ToString(CultureInfo.InvariantCulture) +
                   " unitOrder=" + unitOrder.ToString(CultureInfo.InvariantCulture) +
                   " sideAudit=" + side.ToString("0.###", CultureInfo.InvariantCulture);
        }

        // Audit-only side value. It is printed for calibration; V152 sorting uses only LINE dy in C2Settlement3InuMdV2SortOrderLikeOriginal.
        private float ComputeLineSideForAuditV151(Part p, C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (p == null || unit == null)
                return 0.0f;

            float ux = (unit.RealXFloat - _buildingRealX) / 16.0f;
            float uy = ((unit.RealYFloat - _buildingRealY) / 16.0f) * 2.0f;

            float ax = p.X1 - p.PivotDx;
            float ay = (p.Y1 - p.PivotDy) * 2.0f;
            float bx = p.X2 - p.PivotDx;
            float by = (p.Y2 - p.PivotDy) * 2.0f;

            float abx = bx - ax;
            float aby = by - ay;
            float aux = ux - ax;
            float auy = uy - ay;
            return abx * auy - aby * aux;
        }

        private static int LineDyV152(Part p)
        {
            return p != null ? (p.Y2 - p.Y1) : 0;
        }

        private static string LineFacingNameV152(Part p)
        {
            int dy = LineDyV152(p);
            if (dy > 0) return "BACK_DY_POSITIVE";
            if (dy < 0) return "FRONT_DY_NEGATIVE";
            return "FRONT_DY_ZERO";
        }

        private static string SortKindNameV151(int sortKind)
        {
            if (sortKind == SortKindGround) return "GROUND";
            if (sortKind == SortKindLine) return "LINE";
            if (sortKind == SortKindTop) return "TOP";
            return "FALLBACK";
        }
    }
}
