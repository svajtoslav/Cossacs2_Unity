using System;
using System.Collections.Generic;
using Template = Cossacks2Bridge.UnityAdapters.Maps.C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // COSSACKS2/Nature.cpp order loading and Brigade.cpp HumanLocalSendTo/ApplySwap.
    // Table entries are destination -> source, not source -> destination.
    internal static class C2FormationSymmetryLikeOriginal
    {
        internal static void Build(Template order)
        {
            order.SymInv = null;
            order.Sym4f = null;
            order.Sym4i = null;
            order.SymmetryAudit = "not_requested";
            bool quarter = string.Equals(order.Symmetry, "SYM4", StringComparison.Ordinal);
            if (!quarter && !string.Equals(order.Symmetry, "SYM2", StringComparison.Ordinal)) return;
            if (order.SoldierPoints.Count == 0) { order.SymmetryAudit = "empty_order"; return; }

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in order.SoldierPoints)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.LineIndex); maxY = Math.Max(maxY, p.LineIndex);
            }
            int height = maxY - minY + 1;
            int width = maxX - minX + 1;
            int[,] grid = EmptyGrid(height, width);
            for (int i = 0; i < order.SoldierPoints.Count; i++)
            {
                var p = order.SoldierPoints[i];
                grid[p.LineIndex - minY, p.X - minX] = i;
            }

            // SYM2 matches the Nth member down a column to the Nth member up
            // the mirrored column. It is not a geometric nearest-point search;
            // the shipped staggered orders rely on this column-rank rule.
            int[] inverse = new int[order.SoldierPoints.Count];
            bool good = true;
            for (int i = 0; i < inverse.Length; i++)
            {
                var p = order.SoldierPoints[i];
                int x = p.X - minX, y = p.LineIndex - minY;
                int ordinal = 1;
                for (int row = 0; row < y; row++) if (grid[row, x] >= 0) ordinal++;
                int mirrorX = width - x - 1;
                int source = -1;
                for (int row = height - 1; row >= 0 && ordinal > 0; row--)
                    if (grid[row, mirrorX] >= 0 && --ordinal == 0) source = grid[row, mirrorX];
                inverse[i] = source;
                if (source < 0) good = false;
            }
            if (!good) { order.SymmetryAudit = "invalid_SYM2"; return; }
            order.SymInv = inverse;
            order.SymmetryAudit = "SYM2";
            if (!quarter) return;

            width = (maxX - minX + 2) >> 1;
            if (width != height) { order.SymmetryAudit = "invalid_SYM4_dimensions"; return; }
            grid = EmptyGrid(height, width);
            for (int i = 0; i < order.SoldierPoints.Count; i++)
            {
                var p = order.SoldierPoints[i];
                grid[p.LineIndex - minY, (p.X - minX) >> 1] = i;
            }
            int[] forward = new int[inverse.Length], backward = new int[inverse.Length];
            for (int i = 0; i < inverse.Length; i++)
            {
                var p = order.SoldierPoints[i];
                int x = (p.X - minX) >> 1, y = p.LineIndex - minY;
                if (grid[y, x] < 0 || grid[x, y] < 0 || grid[height - y - 1, width - x - 1] < 0)
                { order.SymmetryAudit = "invalid_SYM4_pattern"; return; }
                forward[i] = grid[width - x - 1, y];
                backward[i] = grid[x, height - y - 1];
            }
            order.Sym4f = forward;
            order.Sym4i = backward;
            order.SymmetryAudit = "SYM4";
        }

        private static int[,] EmptyGrid(int height, int width)
        {
            var grid = new int[height, width];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) grid[y, x] = -1;
            return grid;
        }

        internal struct MemberPosition
        {
            public bool Present;
            public int RealX, RealY;
        }

        // COSSACKS2/BrigadeAI.cpp::GetBrigadeDirectionByUnitPositions.
        // Member order is significant. The center includes command members, while
        // only soldier slots contribute to the weighted direction vector.
        internal static byte DirectionByPositions(Template order, IList<MemberPosition> members, int commandCount)
        {
            int centerX = 0, centerY = 0, count = 0;
            for (int i = 0; i < members.Count; i++)
            {
                if (!members[i].Present) continue;
                centerX += members[i].RealX >> 4;
                centerY += members[i].RealY >> 4;
                count++;
            }
            if (count > 0) { centerX /= count; centerY /= count; }
            const int interval = 270 * 270 / (256 * 16);
            int baseCenterX = order.ActualLineCount * interval / 2;
            int rx = 0, ry = 0;
            for (int i = 0; i < order.SoldierPoints.Count && i + commandCount < members.Count; i++)
            {
                var member = members[i + commandCount];
                if (!member.Present) continue;
                var point = order.SoldierPoints[i];
                int x = (point.LineIndex - order.FirstActualLine) * interval - baseCenterX;
                int y = point.X * interval;
                int radius = C2OriginalMovementMathV352.Norma(x, y);
                byte baseDirection = C2OriginalMovementMathV352.GetDir(x, y);
                byte direction = C2OriginalMovementMathV352.GetDir(
                    member.RealX / 16 - centerX, member.RealY / 16 - centerY);
                byte combined = unchecked((byte)(direction + baseDirection + 128));
                rx += (radius * C2OriginalMovementMathV352.TCos[combined]) >> 8;
                ry += (radius * C2OriginalMovementMathV352.TSin[combined]) >> 8;
            }
            return C2OriginalMovementMathV352.GetDir(rx, ry);
        }

        internal static int[] SelectTurnSwap(
            Template order, byte storedDirection, byte actualDirection, byte requestedDirection,
            out byte nextDirection)
        {
            int delta = unchecked((sbyte)(requestedDirection - actualDirection));
            nextDirection = requestedDirection;
            if (Math.Abs(delta) < 32) return null;
            if (order.Sym4f != null)
            {
                if (delta >= 32 && delta < 96) return order.Sym4f;
                if (delta <= -32 && delta > -96) return order.Sym4i;
                return order.SymInv;
            }
            if (order.SymInv != null)
                return delta > 64 || delta < -64 ? order.SymInv : null;
            nextDirection = unchecked((byte)(storedDirection + (delta > 0 ? 32 : -32)));
            return null;
        }

        internal static T[] ApplySoldierSwap<T>(IList<T> soldiers, int[] swap)
        {
            if (soldiers == null || swap == null) throw new ArgumentNullException();
            if (soldiers.Count > swap.Length) throw new ArgumentException("Members exceed the order template.");
            var seen = new bool[swap.Length];
            for (int i = 0; i < swap.Length; i++)
            {
                int source = swap[i];
                if (source < 0 || source >= swap.Length || seen[source])
                    throw new ArgumentException("Invalid formation permutation.");
                seen[source] = true;
            }
            // ApplySwap pads a depleted Memb array to the original template size
            // with empty entries. Never compact casualties before applying a map.
            var result = new T[swap.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = swap[i] < soldiers.Count ? soldiers[swap[i]] : default(T);
            return result;
        }
    }
}
