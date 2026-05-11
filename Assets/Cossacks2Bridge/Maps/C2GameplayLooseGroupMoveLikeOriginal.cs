using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2GameplayLooseGroupMoveLikeOriginal
    {
        private const int FormDistLikeOriginal = 270;

        public static int IssueMoveLikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float destRealCenterX,
            float destRealCenterY,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            string cancelSource,
            out string audit)
        {
            audit = "not_started";
            if (sourceUnits == null || sourceUnits.Count == 0)
            {
                audit = "no_units";
                return 0;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(sourceUnits.Count);
            float centroidX = 0.0f;
            float centroidY = 0.0f;
            int maxRadius2Real = 0;
            int firstType = -1;
            bool mixedTypes = false;

            for (int i = 0; i < sourceUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = sourceUnits[i];
                if (u == null || !u.isActiveAndEnabled || !u.CanReceiveOrdersLikeOriginal()) continue;

                units.Add(u);
                centroidX += u.RealXFloat;
                centroidY += u.RealYFloat;

                int r = u.GeometryRadius2Real > 0
                    ? u.GeometryRadius2Real
                    : Mathf.Max(1, u.UnitRadius) << 4;
                if (r > maxRadius2Real) maxRadius2Real = r;

                if (firstType < 0) firstType = u.NIndex;
                else if (firstType != u.NIndex) mixedTypes = true;
            }

            int n = units.Count;
            if (n == 0)
            {
                audit = "no_orderable_units";
                return 0;
            }

            if (n == 1)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[0];
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, cancelSource ?? "loose_group_move_single");
                u.SetMoveDestinationRealLikeOriginal(
                    destRealCenterX,
                    destRealCenterY,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    hasFinalFacingDir,
                    finalFacingDir);
                audit = "single exactDestReal=(" +
                        destRealCenterX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                        destRealCenterY.ToString("0.0", CultureInfo.InvariantCulture) + ")";
                return 1;
            }

            centroidX /= n;
            centroidY /= n;

            int layoutX;
            int layoutY;
            BuildLooseLayoutSizeLikeOriginal(n, out layoutX, out layoutY);

            int dirX = Mathf.RoundToInt(centroidX - destRealCenterX) >> 4;
            int dirY = Mathf.RoundToInt(centroidY - destRealCenterY) >> 4;
            if (dirX == 0 && dirY == 0) dirX = 1;

            int rotatedX = dirY;
            int rotatedY = -dirX;

            SortUnitsForLooseLayoutLikeOriginal(units, layoutX, rotatedX, rotatedY);

            if (mixedTypes && maxRadius2Real > FormDistLikeOriginal * 4)
                maxRadius2Real = FormDistLikeOriginal * 4;
            if (maxRadius2Real <= 0) maxRadius2Real = 160;

            int spacingReal = (maxRadius2Real * 3 / 4) << 2;
            if (spacingReal <= 0) spacingReal = 480;

            Vector2[] slots = BuildLooseSlotsLikeOriginal(
                n,
                layoutX,
                layoutY,
                destRealCenterX,
                destRealCenterY,
                rotatedX,
                rotatedY,
                spacingReal);

            int issued = 0;
            for (int i = 0; i < units.Count && i < slots.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, cancelSource ?? "loose_group_move");
                u.SetMoveDestinationRealLikeOriginal(
                    slots[i].x,
                    slots[i].y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    hasFinalFacingDir,
                    finalFacingDir);
                issued++;
            }

            audit = "loose_grid selected=" + n.ToString(CultureInfo.InvariantCulture) +
                    " issued=" + issued.ToString(CultureInfo.InvariantCulture) +
                    " layout=" + layoutX.ToString(CultureInfo.InvariantCulture) + "x" + layoutY.ToString(CultureInfo.InvariantCulture) +
                    " centerReal=(" + destRealCenterX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    destRealCenterY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                    " centroidReal=(" + centroidX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    centroidY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                    " dir=(" + dirX.ToString(CultureInfo.InvariantCulture) + "," + dirY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " rotated=(" + rotatedX.ToString(CultureInfo.InvariantCulture) + "," + rotatedY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " radius2Real=" + maxRadius2Real.ToString(CultureInfo.InvariantCulture) +
                    " spacingReal=" + spacingReal.ToString(CultureInfo.InvariantCulture) +
                    " finalFacing=" + hasFinalFacingDir +
                    " finalDir=" + finalFacingDir.ToString(CultureInfo.InvariantCulture);
            return issued;
        }

        private static void BuildLooseLayoutSizeLikeOriginal(int count, out int layoutX, out int layoutY)
        {
            int lx = Mathf.FloorToInt(Mathf.Sqrt(Mathf.Max(1, count)));
            int ly = lx * 5 / 3;
            lx = lx * 3 / 5;
            if (count < 4)
            {
                lx = 1;
                ly = count;
            }

            if (lx < 1) lx = 1;
            if (ly < 1) ly = 1;

            int nn = lx * ly;
            if (nn < count)
            {
                if (nn + lx >= count) ly++;
                else if (nn + ly >= count) lx++;
                else
                {
                    ly++;
                    lx++;
                }
            }

            nn = lx * ly;
            if (nn < count)
            {
                if (nn + lx >= count) ly++;
                else if (nn + ly >= count) lx++;
                else
                {
                    ly++;
                    lx++;
                }
            }

            layoutX = Mathf.Max(1, lx);
            layoutY = Mathf.Max(1, ly);
        }

        private static void SortUnitsForLooseLayoutLikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            int rowWidth,
            int lineX,
            int lineY)
        {
            units.Sort((a, b) => CompareByLineLikeOriginal(a, b, lineX, lineY));

            int start = 0;
            while (start < units.Count)
            {
                int len = Mathf.Min(Mathf.Max(1, rowWidth), units.Count - start);
                units.Sort(start, len, Comparer<C2NeutralPeasantUnitInfoV2LikeOriginal>.Create(
                    (a, b) => CompareByLineLikeOriginal(a, b, -lineY, lineX)));
                start += len;
            }
        }

        private static int CompareByLineLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal a,
            C2NeutralPeasantUnitInfoV2LikeOriginal b,
            int lineX,
            int lineY)
        {
            float pa = a.RealXFloat * lineX + a.RealYFloat * lineY;
            float pb = b.RealXFloat * lineX + b.RealYFloat * lineY;
            int c = pa.CompareTo(pb);
            if (c != 0) return c;
            return a.RecordIndex.CompareTo(b.RecordIndex);
        }

        private static Vector2[] BuildLooseSlotsLikeOriginal(
            int count,
            int layoutX,
            int layoutY,
            float centerX,
            float centerY,
            int lineX,
            int lineY,
            int spacingReal)
        {
            Vector2[] slots = new Vector2[count];
            float nr = Mathf.Sqrt(lineX * lineX + lineY * lineY);
            if (nr < 0.001f)
            {
                lineX = 1;
                lineY = 0;
                nr = 1.0f;
            }

            float vx = lineX * spacingReal / nr;
            float vy = lineY * spacingReal / nr;
            float dx = (-(layoutX - 1) * vy + (layoutY - 1) * vx) * 0.5f;
            float dy = ((layoutX - 1) * vx + (layoutY - 1) * vy) * 0.5f;

            int pos = 0;
            for (int iy = 0; iy < layoutY; iy++)
            {
                for (int ix = 0; ix < layoutX; ix++)
                {
                    if (pos < count)
                    {
                        slots[pos] = new Vector2(
                            centerX - ix * vy + iy * vx - dx,
                            centerY + ix * vx + iy * vy - dy);
                    }
                    pos++;
                }
            }

            return slots;
        }
    }
}
