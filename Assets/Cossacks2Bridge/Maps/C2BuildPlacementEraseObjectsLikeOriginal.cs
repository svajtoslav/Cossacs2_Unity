// C2BuildPlacementEraseObjectsLikeOriginal.cs
// V39: runtime erase pass for build placement.
// Original chain: CreateBuilding -> BUILDLOCKPOINTS -> BSetPt + EraseTreesInPoint.
// Unity side: nature is batched, so we erase/hide batched tree/stone/fence-like quads whose original pivot falls into BUILDLOCKPOINTS.

using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        public void C2BuildRuntimeErasePlacedFoundationAreaLikeOriginal(
            int footprintCellX,
            int footprintCellY,
            IList<Vector2Int> buildLockPoints,
            int expandCells,
            string source,
            out string audit)
        {
            int cells = 0;
            int hiddenNature = 0;

            HashSet<long> cellSet = new HashSet<long>();
            if (buildLockPoints != null)
            {
                for (int i = 0; i < buildLockPoints.Count; i++)
                {
                    Vector2Int p = buildLockPoints[i];
                    long key = C2NatureBuildEraseMaskV39LikeOriginal.PackCellKeyLikeOriginal(footprintCellX + p.x, footprintCellY + p.y);
                    if (cellSet.Add(key))
                        cells++;
                }
            }

            if (cellSet.Count > 0)
            {
                C2NatureBuildEraseMaskV39LikeOriginal[] masks = Object.FindObjectsOfType<C2NatureBuildEraseMaskV39LikeOriginal>(true);
                for (int i = 0; masks != null && i < masks.Length; i++)
                {
                    C2NatureBuildEraseMaskV39LikeOriginal m = masks[i];
                    if (m == null) continue;
                    hiddenNature += m.EraseCellsLikeOriginal(cellSet, expandCells);
                }
            }

            audit = "V39_BUILDLOCK_ERASE source='" + (source ?? string.Empty) +
                    "' footprintCell=" + footprintCellX.ToString(CultureInfo.InvariantCulture) + "/" + footprintCellY.ToString(CultureInfo.InvariantCulture) +
                    " buildLockCells=" + cells.ToString(CultureInfo.InvariantCulture) +
                    " expand=" + expandCells.ToString(CultureInfo.InvariantCulture) +
                    " hiddenNature=" + hiddenNature.ToString(CultureInfo.InvariantCulture) +
                    " note='trees/stones/fence-like batched nature hidden after foundation; fieldFood remains placement blocker'";
        }
    }
}
