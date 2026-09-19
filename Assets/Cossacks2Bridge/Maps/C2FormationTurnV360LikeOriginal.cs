using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        // Like Brigade::Memb: vacancies keep their indices until an explicit
        // regroup/fill operation. An ordinary move or turn must not compact them.
        private static List<C2NeutralPeasantUnitInfoV2LikeOriginal> GetFormationOrderMembersV360LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            var result = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(group.Units.Count);
            for (int i = 0; i < group.Units.Count; i++)
            {
                var unit = group.Units[i];
                result.Add(IsUsableFormationUnitV172LikeOriginal(unit, true) ? unit : null);
            }
            return result;
        }

        private static int ResolveOrderCommandCountV360LikeOriginal(
            RuntimeFormationV172LikeOriginal group, IList<C2NeutralPeasantUnitInfoV2LikeOriginal> members)
        {
            return group.CommandSlotCount >= 0
                ? Math.Min(group.CommandSlotCount, members.Count)
                : ResolveCommandPrefixCountV320LikeOriginal(group, members);
        }

        private static byte GetFormationPhysicalDirectionV360LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal template,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> members, int commands)
        {
            var positions = new C2FormationSymmetryLikeOriginal.MemberPosition[members.Count];
            for (int i = 0; i < members.Count; i++)
            {
                var unit = members[i];
                if (unit == null) continue;
                positions[i] = new C2FormationSymmetryLikeOriginal.MemberPosition {
                    Present = true, RealX = unit.RealX, RealY = unit.RealY
                };
            }
            return C2FormationSymmetryLikeOriginal.DirectionByPositions(template, positions, commands);
        }

        private static bool ApplyFormationSymmetricMoveV360LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal template,
            byte requestedDirection)
        {
            int commands = ResolveOrderCommandCountV360LikeOriginal(group, members);
            if (template == null || template.SymInv == null ||
                members.Count - commands > template.UnitCount || members.Count <= commands) return false;
            byte physical = GetFormationPhysicalDirectionV360LikeOriginal(template, members, commands);
            byte next;
            int[] swap = C2FormationSymmetryLikeOriginal.SelectTurnSwap(
                template, group.Direction, physical, requestedDirection, out next);
            // Symmetric orders reach the requested direction in this step. Orders
            // without symmetry still need the full GoTo/KeepPositions sequence.
            ApplyFormationTurnSwapV360LikeOriginal(members, commands, swap);
            return swap != null;
        }

        private static void ApplyFormationTurnSwapV360LikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members, int commands, int[] swap)
        {
            if (swap == null) return;
            var soldiers = members.GetRange(commands, members.Count - commands);
            var reordered = C2FormationSymmetryLikeOriginal.ApplySoldierSwap(soldiers, swap);
            members.RemoveRange(commands, members.Count - commands);
            members.AddRange(reordered);
        }
    }
}
