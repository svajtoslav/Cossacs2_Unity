using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Spatial data-access adapter for the current port's LIVE-coordinate queries.
    // CII Brigade.cpp::GetEnemyDensity reads MCount/NMSL for one >>11 cell;
    // sorting/scanning the entire army per query is not part of that algorithm.
    // This index preserves the port's current cell membership and ascending Index
    // order. It deliberately does not change combat filters or emulate the native
    // ten-tick SetMonstersInCells snapshot as a gameplay change. The presence
    // mask uses the same live membership and is invalidated on every allegiance change.
    internal static class C2LiveUnitCellIndex
    {
        private sealed class Cell
        {
            internal readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> Units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(16);
            internal bool PresenceDirty = true;
            internal byte Presence;
        }
        private static readonly Dictionary<long, Cell> Cells = new Dictionary<long, Cell>(256);
        private static readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, long> Membership =
            new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, long>(2048);
        private static readonly Stack<Cell> SpareCells = new Stack<Cell>();

        private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

        internal static List<C2NeutralPeasantUnitInfoV2LikeOriginal> GetCell(int x, int y)
        {
            return Cells.TryGetValue(Key(x, y), out var cell) ? cell.Units : null;
        }

        internal static List<C2NeutralPeasantUnitInfoV2LikeOriginal> GetCellWithPotentialEnemies(int x, int y, byte ownMask)
        {
            if (!Cells.TryGetValue(Key(x, y), out var cell)) return null;
            if (cell.PresenceDirty)
            {
                byte presence = 0;
                for (int i = 0; i < cell.Units.Count; i++)
                    presence |= C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(cell.Units[i]);
                cell.Presence = presence;
                cell.PresenceDirty = false;
            }
            // Including dead/inactive members is conservative: it can cause an
            // unnecessary scan, never hide a live enemy after reactivation.
            return (cell.Presence & ~ownMask) != 0 ? cell.Units : null;
        }

        internal static void AllegianceChanged(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (Membership.TryGetValue(unit, out long key) && Cells.TryGetValue(key, out var cell))
                cell.PresenceDirty = true;
        }

        // Call after both coordinates have been committed, including within a
        // simulation tick (movement, separation, recoil and spawn relocation).
        internal static void PositionChanged(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.C2ObjectIndexV408LikeOriginal < 0) return;
            int x = Mathf.RoundToInt(unit.RealXFloat != 0f ? unit.RealXFloat : unit.RealX) >> 11;
            int y = Mathf.RoundToInt(unit.RealYFloat != 0f ? unit.RealYFloat : unit.RealY) >> 11;
            long key = Key(x, y);
            if (Membership.TryGetValue(unit, out long previous))
            {
                if (previous == key) return;
                Remove(unit);
            }
            if (!Cells.TryGetValue(key, out var cell))
            {
                cell = SpareCells.Count > 0 ? SpareCells.Pop() : new Cell();
                Cells.Add(key, cell);
            }
            cell.PresenceDirty = true;
            var units = cell.Units;
            int lo = 0, hi = units.Count;
            int index = unit.C2ObjectIndexV408LikeOriginal;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (units[mid].C2ObjectIndexV408LikeOriginal < index) lo = mid + 1;
                else hi = mid;
            }
            units.Insert(lo, unit);
            Membership.Add(unit, key);
        }

        // Remove before releasing/reusing the Group[] identity slot.
        internal static void Remove(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || !Membership.TryGetValue(unit, out long key)) return;
            Membership.Remove(unit);
            if (!Cells.TryGetValue(key, out var cell)) return;
            cell.Units.Remove(unit);
            cell.PresenceDirty = true;
            if (cell.Units.Count != 0) return;
            Cells.Remove(key);
            if (SpareCells.Count < 256) SpareCells.Push(cell);
        }
    }
}
