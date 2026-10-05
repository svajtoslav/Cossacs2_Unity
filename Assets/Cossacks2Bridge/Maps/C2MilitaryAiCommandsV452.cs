using System;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed partial class C2MilitaryAiV452
    {
        private void Command(Squad squad)
        {
            if (DrainAttacks(squad)) return;
            if (Time.time < squad.NextOrder) return;
            squad.NextOrder = Time.time + 2;
            var traits = C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(squad.Leader);
            float rifleRange = traits.MaxDamage[1] > 0 ? traits.AttackRadius2[1] : 0;
            var enemy = NearestEnemy(squad, Mathf.Max(1200, rifleRange));
            Point point = Points.Find(p => ReferenceEquals(p.Identity, squad.Objective));
            var building = enemy == null && point != null && point.Owner != Brain.Nation ? point.Building : null;
            if (building != null && building.LifeLikeOriginal <= 0) building = null;
            if (enemy != null || building != null)
            {
                Vector2 target = enemy != null ? Position(enemy) : new Vector2(building.RealX / 16f, building.RealY / 16f);
                if (squad.Cannon)
                {
                    // NewAttackPoint owns approach, backing away, loading and the
                    // selected ammunition. Do not replace its order every tick.
                    if (!ReferenceEquals(squad.CombatTarget, (object)enemy ?? building) ||
                        squad.Leader.RuntimeLinkCachedLikeOriginal?.Runtime?.ArtilleryPointOrderV439 == null ||
                        (target - squad.LastDestination).sqrMagnitude > 160 * 160)
                        AttackIndividuals(squad, enemy, building, -1, target);
                    return;
                }
                float distance = Vector2.Distance(squad.Position, target);
                bool ranged = enemy != null && rifleRange > 0 && distance > Mathf.Max(120, traits.AttackRadius1[1]);
                if (ranged && distance > rifleRange * .8f)
                {
                    Move(squad, target + (squad.Position - target).normalized * rifleRange * .65f);
                    return;
                }
                if (ranged)
                {
                    // BrigadeOrder_RifleAttack selects individual victims and
                    // keeps the real cartridge/reload cycle across volleys.
                    if (squad.Order != "fire")
                    {
                        C2FormationRuntimeV167LikeOriginal.TryStopFormationV320LikeOriginal(squad.Leader, out _);
                        foreach (var u in squad.Members) if (Own(u))
                            C2CombatRuntimeV334LikeOriginal.SetCommandWeaponModeLikeOriginal(u, 1);
                        C2BrigadeRifleAttackV405LikeOriginal.StartBrigadeRifleAttackV418LikeOriginal(squad.Leader, "MilitaryAI_V452");
                        squad.Order = "fire"; squad.Mode = 1; squad.CombatTarget = enemy; AttackOrders++;
                    }
                    return;
                }
                if (enemy != null && C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(enemy, out int enemyGroup) && enemyGroup >= 0)
                {
                    // The normal brigade melee command, including its ordered
                    // movement through the enemy formation, owns local combat.
                    bool sameBrigade = squad.CombatTarget is C2NeutralPeasantUnitInfoV2LikeOriginal previous && Alive(previous) &&
                        C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(previous, out int previousGroup) && previousGroup == enemyGroup;
                    if (squad.Order == "melee" && sameBrigade) return;
                    if (C2FormationRuntimeV167LikeOriginal.TryGetFormationCenterRealV406LikeOriginal(enemy, out float x, out float y))
                    {
                        Vector2 direction = (new Vector2(x, y) / 16 - squad.Position).normalized;
                        byte facing = C2FormationRuntimeV167LikeOriginal.GetMeleeAttackDestinationDirectionV406LikeOriginal(squad.Leader, enemy);
                        if (C2FormationRuntimeV167LikeOriginal.PrepareBrigadeMeleeAttackMoveV414LikeOriginal(squad.Leader, enemy, true, "MilitaryAI_V452"))
                        {
                            if (C2FormationRuntimeV167LikeOriginal.IssueBrigadeAttackHumanLocalSendToV414LikeOriginal(squad.Leader, x + direction.x * 1920, y + direction.y * 1920, facing, out _, out _))
                            {
                                C2FormationRuntimeV167LikeOriginal.FinishBrigadeMeleeAttackMoveV414LikeOriginal(squad.Leader, enemy, true, "MilitaryAI_V452");
                                squad.Order = "melee"; squad.CombatTarget = enemy; squad.Mode = 0; AttackOrders++;
                            }
                            else C2FormationRuntimeV167LikeOriginal.AbortBrigadeMeleeAttackMoveV414LikeOriginal(squad.Leader);
                        }
                    }
                    return;
                }
                if (squad.Order != "attack" || !ReferenceEquals(squad.CombatTarget, (object)enemy ?? building))
                    AttackIndividuals(squad, enemy, building, 0, target);
                return;
            }
            if (point != null)
            {
                // Capture and ownership changes remain in settlement diplomacy.
                // Infantry enters the normal capture radius; cannons follow at
                // a supporting distance and never claim a village themselves.
                Vector2 destination = point.Position;
                if (squad.Cannon) destination += (squad.Position - destination).normalized * 750;
                Move(squad, destination);
            }
        }

        private void AttackIndividuals(Squad squad, C2NeutralPeasantUnitInfoV2LikeOriginal enemy,
            C2SettlementBuildingSelectableV1LikeOriginal building, int mode, Vector2 target)
        {
            squad.CombatTarget = (object)enemy ?? building; squad.LastDestination = target;
            squad.PendingUnit = enemy; squad.PendingBuilding = building; squad.PendingIndex = 0;
            squad.Mode = mode; squad.Order = "attack"; AttackOrders++;
            DrainAttacks(squad);
        }
        private bool DrainAttacks(Squad squad)
        {
            if (squad.PendingIndex < 0) return false;
            bool valid = Alive(squad.PendingUnit) && !Friendly(squad.PendingUnit) && squad.PendingUnit.OwnerMode == Map ||
                squad.PendingBuilding != null && squad.PendingBuilding.OwnerMode == Map && squad.PendingBuilding.Nation != Brain.Nation && squad.PendingBuilding.LifeLikeOriginal > 0;
            if (!valid) { squad.PendingIndex = -1; return false; }
            int end = Math.Min(squad.PendingIndex + 8, squad.Members.Count);
            for (; squad.PendingIndex < end; squad.PendingIndex++)
            {
                var u = squad.Members[squad.PendingIndex]; if (!Own(u)) continue;
                u.EnsureUnityProxyLikeOriginal();
                if (squad.Mode >= 0) C2CombatRuntimeV334LikeOriginal.SetCommandWeaponModeLikeOriginal(u, squad.Mode);
                var combat = u.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                if (combat == null) combat = u.gameObject.AddComponent<C2CombatRuntimeV334LikeOriginal>();
                combat.BeginAttackLikeOriginal(u, squad.PendingUnit, squad.PendingBuilding,
                    squad.PendingUnit != null ? squad.PendingUnit.WorldPositionLikeOriginal : squad.PendingBuilding.transform.position);
            }
            if (squad.PendingIndex >= squad.Members.Count) squad.PendingIndex = -1;
            return true;
        }
        private void Move(Squad squad, Vector2 destination)
        {
            if ((destination - squad.Position).sqrMagnitude < 160 * 160) return;
            if ((squad.Position - squad.LastProgressPosition).sqrMagnitude > 80 * 80)
            { squad.LastProgressPosition = squad.Position; squad.LastProgressTime = Time.time; }
            if (squad.Order == "move" && (destination - squad.LastDestination).sqrMagnitude < 128 * 128 && Time.time - squad.LastProgressTime < 12) return;
            if (float.IsInfinity(RouteDistance(squad.Position, destination, Brain.Nation))) return;
            bool accepted;
            if (squad.Cannon)
            {
                squad.Leader.SetMoveDestinationRealLikeOriginal(destination.x * 16, destination.y * 16,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal, false, 0);
                accepted = true;
            }
            else accepted = C2FormationRuntimeV167LikeOriginal.TryIssueMoveV167LikeOriginal(squad.Members,
                destination.x * 16, destination.y * 16, false, 0, "MilitaryAI_V452", out _, out _);
            if (!accepted) return;
            squad.PendingIndex = -1; squad.CombatTarget = null; squad.Mode = -1; squad.Order = "move";
            squad.LastDestination = destination; squad.LastProgressTime = Time.time; MoveOrders++;
        }
    }
}
