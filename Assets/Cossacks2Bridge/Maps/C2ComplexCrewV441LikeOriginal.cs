using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntime
    {
        internal C2NeutralPeasantUnitInfoV2LikeOriginal[] FillCrewV441;
        internal C2UnitOriginalRuntime CrewFillOwnerV441;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal bool FillComplexCrewV441(C2UnitOriginalRuntime u)
        {
            if (u?.Info == null || u.Info.IsDeadLikeOriginal || !CannonNeedsCrewV439(u)) return false;
            StopArtilleryV439(u);
            // MapDiscr.h::MaxFillObj=4. These are reservations, not live helpers.
            u.FillCrewV441 = new C2NeutralPeasantUnitInfoV2LikeOriginal[4];
            return true;
        }

        // Mechanics.cpp::FillObjectByUnitsLink. Runs before the NoMove return:
        // local orders keep executing even though an empty gun cannot move.
        private void StepFillComplexCrewV441(C2UnitOriginalRuntime u)
        {
            var reserved = u.FillCrewV441;
            if (reserved == null) return;
            var cob = u.OriginalComplexObjectV430LikeOriginal;
            if (!CannonNeedsCrewV439(u)) { u.FillCrewV441 = null; return; }
            cob.Charged = cob.ResSubtracted = false;
            bool something = false, missing = false;
            int count = Math.Min(4, cob.HelpersV432LikeOriginal.Count);
            for (int i = 0; i < count; i++)
            {
                var helper = cob.HelpersV432LikeOriginal[i];
                if (!helper.MissingV437) continue;
                missing = true;
                if (!ComplexGetHelperPositionV432LikeOriginal(cob, helper, out int x, out int y, out int h)) continue;
                var recruit = reserved[i];
                if (ReferenceEquals(recruit, null))
                {
                    int best = int.MaxValue;
                    foreach (var soldier in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
                    {
                        if (soldier == null || !soldier.isActiveAndEnabled || soldier.IsDeadLikeOriginal ||
                            soldier.Nation != u.Info.Nation || Array.IndexOf(reserved, soldier) >= 0) continue;
                        var link = soldier.RuntimeLinkCachedLikeOriginal;
                        if (link?.Runtime == null || link.IsUnlimitedMotionV415LikeOriginal || link.Runtime.OriginalComplexObjectV430LikeOriginal != null) continue;
                        // A soldier cannot walk to two guns at once. Native keeps
                        // reservations only within one gun, which lets simultaneous
                        // fill commands steal each other's recruits and stop partly full.
                        var other = link.Runtime.CrewFillOwnerV441;
                        if (other != null && other != u && other.FillCrewV441 != null &&
                            Array.IndexOf(other.FillCrewV441, soldier) >= 0) continue;
                        var traits = C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(soldier);
                        if (traits.DontFillCannonV441 || traits.CostlyV441 || traits.Building) continue;
                        int dist = C2OriginalMovementMathV352.Norma(((int)soldier.RealXFloat >> 4) - x, ((int)soldier.RealYFloat >> 4) - y);
                        if (dist > 512 || dist >= best) continue;
                        best = dist; recruit = soldier;
                    }
                    if (recruit == null) continue;
                    reserved[i] = recruit;
                    C2FormationRuntimeV167LikeOriginal.TryRemoveUnitFromFormationForPanicV404LikeOriginal(recruit, out _, out _, out _);
                    var rt = recruit.RuntimeLinkCachedLikeOriginal.Runtime;
                    rt.CrewFillOwnerV441 = u;
                    recruit.GetComponent<C2CombatRuntimeV334LikeOriginal>()?.CancelForExternalOrderLikeOriginal("FillObjectByUnits");
                    C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(recruit);
                    rt.OriginalUnitSpeedLikeOriginal = 64;
                    rt.PostureWeaponTypeLikeOriginal = rt.LocalPostureWeaponTypeV411LikeOriginal = -1;
                    ClearRuntimeSlowRechargeDelayLikeOriginal(rt);
                    int stand = ResolveRuntimeStandAnimationIndexV322LikeOriginal(rt);
                    if (stand >= 0) SelectAnimationStateLikeOriginal(rt, C2UnitOriginalState.Stand, stand, true, "FillObjectByUnits");
                    C2OriginalOrderChainV352.SubmitMove(recruit, x << 4, y << 4, false, 0, 0, "FillObjectByUnits", true, 145);
                    something = true;
                }
                else
                {
                    something = true;
                    if (recruit == null || !recruit.isActiveAndEnabled || recruit.IsDeadLikeOriginal || recruit.Nation != u.Info.Nation)
                    { reserved[i] = null; continue; }
                    if (C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(recruit) &&
                        C2OriginalOrderChainV352.GetLocalPriorityV434LikeOriginal(recruit) == 17) continue;
                    int dist = C2OriginalMovementMathV352.Norma(((int)recruit.RealXFloat >> 4) - x, ((int)recruit.RealYFloat >> 4) - y);
                    if (dist >= 20) { reserved[i] = null; continue; }
                    helper.MissingV437 = false;
                    recruit.PlayDeathOneShotLikeOriginal(recruit.RealDir);
                    // Native Sdoxlo=30000 consumes the recruit without a corpse.
                    ExpireOriginalDeathV435LikeOriginal(recruit.RuntimeLinkCachedLikeOriginal.Runtime);
                    cob.CrewMaterialActiveV441 = true;
                    missing = false;
                    for (int j = 0; j < count; j++) missing |= cob.HelpersV432LikeOriginal[j].MissingV437;
                    Debug.Log("[C2 CREW V441] filled=" + i + " gun=" + u.Info.SourceMonsterId + " recruit=" + recruit.C2ObjectIndexV408LikeOriginal);
                }
            }
            if (!missing) cob.NoMove = cob.NoAttackV441 = false;
            if (!something) u.FillCrewV441 = null;
        }

        // Mechanics.cpp::CheckObjectForFreeStatus. Embedded crew have no separate
        // OneObject until DieComplexObject releases them; null Runtime is not empty.
        internal static bool IsComplexFreeV441(C2UnitOriginalRuntime u)
        {
            var cob = u?.OriginalComplexObjectV430LikeOriginal;
            if (cob == null) return false;
            foreach (var helper in cob.HelpersV432LikeOriginal)
                if (!helper.MissingV437) return false;
            return true;
        }

        // Mechanics.cpp::DieComplexObject. False means the gun survives without
        // crew; true lets the ordinary OneObject death path continue.
        private bool DieComplexObjectV441(C2UnitOriginalRuntime u)
        {
            var cob = u.OriginalComplexObjectV430LikeOriginal;
            if (cob.DeathProcessedV441) return true;
            C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
            var released = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(8);
            int stage = 1;
            bool totalDeath = u.HiddenInsideBuildingLikeOriginal;
            var trailer = cob.TaleV430LikeOriginal?.OwnerRuntimeV430LikeOriginal;
            if (trailer?.Info != null && !trailer.Info.IsDeadLikeOriginal)
            {
                trailer.Info.PlayDeathOneShotLikeOriginal(trailer.Info.RealDir);
                if (!trailer.Info.IsDeadLikeOriginal) trailer.Info.PlayDeathOneShotLikeOriginal(trailer.Info.RealDir);
            }
            var leader = cob.LeaderV430LikeOriginal?.OwnerRuntimeV430LikeOriginal;
            if (leader?.Info != null && !leader.Info.IsDeadLikeOriginal) totalDeath = true;

            if (!u.HiddenInsideBuildingLikeOriginal)
            {
                foreach (var helper in cob.HelpersV432LikeOriginal)
                {
                    if (helper.MissingV437) continue;
                    if (!ComplexGetHelperPositionV432LikeOriginal(cob, helper, out int x, out int y, out int height))
                    { totalDeath = true; break; }
                    var soldier = helper.Runtime?.Info;
                    bool embedded = helper.Runtime == null;
                    if (embedded)
                    {
                        helper.MissingV437 = true;
                        if (C2OriginalProduceCatalogV13.TryBuildEditorItemForUnitIdV333LikeOriginal(helper.Desc.UnitId, out var item))
                            TrySpawnProducedUnitInstanceLikeOriginal(u.Info.OwnerMode, null, item, u.Info.Nation,
                                out soldier, out string audit, x << 4, y << 4);
                        if (soldier != null)
                        {
                            stage = 0;
                            if (released.Count < 8) released.Add(soldier);
                        }
                    }
                    if (soldier == null || soldier.IsDeadLikeOriginal) continue;
                    if ((helper.Desc.Options & 1) != 0) soldier.PlayDeathOneShotLikeOriginal(soldier.RealDir);
                    else C2MoraleRuntimeV404LikeOriginal.StartPanicV404LikeOriginal(soldier);
                }
                foreach (var quant in cob.Desc.Chain)
                    if (quant.DeathStagesV441.Contains(stage)) totalDeath = true;
                // The $EXPLODE stage controls destruction. The corresponding
                // particle/debris sequence still uses the separate weapon-effect port.
            }
            C2NeutralPeasantUnitInfoV2LikeOriginal lowest = null;
            foreach (var soldier in released)
            {
                if (totalDeath) soldier.PlayDeathOneShotLikeOriginal(soldier.RealDir);
                else if (lowest == null || soldier.RealY < lowest.RealY) lowest = soldier;
            }
            if (!totalDeath && lowest != null) lowest.PlayDeathOneShotLikeOriginal(lowest.RealDir);

            StopArtilleryV439(u);
            u.ArtilleryPointOrderV439 = null;
            u.ArtilleryChargeOrderV439 = -1;
            u.HasMoveTargetLikeOriginal = false;
            cob.NoMove = cob.NoAttackV441 = true;
            cob.LxOverrideV441 = 4;
            cob.DeathProcessedV441 = totalDeath;
            if (!totalDeath)
            {
                u.Info.LifeLikeOriginal = u.Info.MaxLifeLikeOriginal;
                cob.CrewMaterialActiveV441 = false;
                C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
            }
            Debug.Log("[C2 CREW V441] unit=" + u.Info.SourceMonsterId + " released=" + released.Count +
                " stage=" + stage + " totalDeath=" + totalDeath + " life=" + u.Info.LifeLikeOriginal);
            return totalDeath;
        }
    }
}
