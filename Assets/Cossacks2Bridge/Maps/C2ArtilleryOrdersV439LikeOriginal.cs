using System;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntime
    {
        // Mechanics.cpp::SetOrderedStateForComplexObjectLink local order.
        internal int ArtilleryChargeOrderV439 = -1;
        // Complex LocalNewState is an ammunition slot, not an infantry posture.
        internal int ArtilleryAmmoV442;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal static bool IsCannonChargeStateV439(int state) => state == 12 || state == 16 || state == 20;

        private static int ComplexRechargeStartV439(C2ComplexObjectRuntimeV430LikeOriginal cob,int nativeStart,int ready)
        {
            // Deliberate repair of the native post-FillObjectByUnits rocket bug:
            // Mechanics.cpp unconditionally starts 16 -> 12, but RAKETA has no
            // state 16 or that transition. Its real reload is ATTACK -> RATTACK
            // (8 -> 12, 66 ticks). Keep all existing cannon transitions intact.
            bool nativeValid=true,fireReloadValid=true;
            foreach(var quant in cob.Desc.Chain)
            {
                var native=ComplexTransitionV430LikeOriginal(quant,nativeStart,ready);
                var fire=ComplexTransitionV430LikeOriginal(quant,8,ready);
                nativeValid &= native!=null && native.Exists;
                fireReloadValid &= fire!=null && fire.Exists && !fire.Direct;
            }
            return !nativeValid && fireReloadValid ? 8 : nativeStart;
        }

        internal bool SetArtilleryChargeV439(C2UnitOriginalRuntime u, int state)
        {
            var cob=u?.OriginalComplexObjectV430LikeOriginal;
            if(cob==null || CannonNeedsCrewV439(u) || state<0 || state>2 || state==ComplexLocalNewStateV430LikeOriginal(u)) return false;
            var traits=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(u.Info);
            if(string.IsNullOrEmpty(traits.WeaponName[state]))return false;
            ComplexCancelRechargementV430LikeOriginal(cob);
            cob.Charged=false;
            C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(u.Info);
            var combat=u.Info.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if(combat!=null)combat.CancelForExternalOrderLikeOriginal("SetOrderedStateForComplexObject");
            cob.DestDir=-1;
            if(cob.StartState!=cob.FinalState && IsCannonChargeStateV439(cob.FinalState) &&
                (IsCannonChargeStateV439(cob.StartState)||cob.StartState==8))
            {
                if(cob.FinalState==state*4+12)return true;
                cob.StartState=cob.FinalState;cob.FinalState=state*4+12;
                cob.GroundStandState=cob.FinalState;
                u.ArtilleryAmmoV442=state;
                return true;
            }
            u.ArtilleryChargeOrderV439=state;
            return true;
        }

        private static void StepArtilleryChargeOrderV439(C2UnitOriginalRuntime u)
        {
            int order=u.ArtilleryChargeOrderV439;
            if(order<0)return;
            var cob=u.OriginalComplexObjectV430LikeOriginal;
            if(cob==null){u.ArtilleryChargeOrderV439=-1;return;}
            int state=order*4+12;
            if(cob.StartState!=cob.FinalState)
            {
                if(cob.FinalState==state)
                {
                    u.ArtilleryAmmoV442=order;
                    cob.GroundStandState=state;
                }
                return;
            }
            if(cob.FinalState==state)
            {
                u.ArtilleryAmmoV442=order;
                u.ArtilleryChargeOrderV439=-1;
                return;
            }
            ComplexTryTransformV430LikeOriginal(cob,state);
            if(cob.FinalState==state)cob.GroundStandState=state;
        }

        internal static bool GetArtilleryChargeV439(C2UnitOriginalRuntime u,out int type,out int percent)
        {
            type=0;percent=0;
            var cob=u?.OriginalComplexObjectV430LikeOriginal;
            if(cob==null||u.Info==null||u.Info.IsDeadLikeOriginal)return false;
            type=ComplexLocalNewStateV430LikeOriginal(u);
            percent=cob.Charged?100:0;
            if(!cob.Charged)return true;
            if(cob.StartState==cob.FinalState && cob.StartState!=8)return true;
            if(IsCannonChargeStateV439(cob.FinalState) && (IsCannonChargeStateV439(cob.StartState)||cob.StartState==8))
            {
                var transition=ComplexTransitionV430LikeOriginal(cob.Desc.Chain[0],cob.StartState,cob.FinalState);
                percent=transition!=null&&transition.Exists&&!transition.Direct
                    ? 100*cob.TransTime/256/Math.Max(1,transition.MaxTransfTime):0;
            }
            return true;
        }
    }
}
