using System;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2ArtilleryPointOrderV439
    {
        internal int X, Y, Shots;
        internal bool AdjustingRangeV442;
        internal int RangeMoveXV442, RangeMoveYV442;
    }

    public sealed partial class C2UnitOriginalRuntime
    {
        internal C2ArtilleryPointOrderV439 ArtilleryPointOrderV439;
        internal int ArtilleryShotsFiredV439;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal static bool CannonNeedsCrewV439(C2UnitOriginalRuntime u)
        {
            var c=u?.OriginalComplexObjectV430LikeOriginal;
            if(c==null)return false;
            foreach(var helper in c.HelpersV432LikeOriginal)if(helper.MissingV437)return true;
            return false;
        }

        internal bool AttackArtilleryPointV439(C2UnitOriginalRuntime u,int x,int y,int shots)
        {
            if(u?.Info==null || u.Info.IsDeadLikeOriginal || u.OriginalComplexObjectV430LikeOriginal==null || CannonNeedsCrewV439(u))return false;
            var combat=u.Info.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if(combat!=null)combat.CancelForExternalOrderLikeOriginal("NewAttackPoint complex");
            C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(u.Info);
            u.ArtilleryPointOrderV439=new C2ArtilleryPointOrderV439{X=x,Y=y,Shots=shots};
            C2ArtilleryTargetMarkerV442.Ensure(u.Info);
            return true;
        }

        internal void StopArtilleryV439(C2UnitOriginalRuntime u)
        {
            if(u?.Info==null)return;
            C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(u.Info);
            u.Info.GetComponent<C2CombatRuntimeV334LikeOriginal>()?.CancelForExternalOrderLikeOriginal("vui_IS_CancelAttack");
            if(u.OriginalComplexObjectV430LikeOriginal!=null)u.OriginalComplexObjectV430LikeOriginal.DestDir=-1;
        }

        // Mechanics.cpp::AttackPointByComplexObjectLink. Runs once per native
        // simulation tick, before MotionHandlerForComplexObjects advances poses.
        private void StepArtilleryPointOrderV439(C2UnitOriginalRuntime u)
        {
            var order=u.ArtilleryPointOrderV439;
            var cob=u.OriginalComplexObjectV430LikeOriginal;
            if(order==null || cob==null)return;
            int x=order.X,y=order.Y,sx=(int)u.RuntimeRealXLikeOriginal,sy=(int)u.RuntimeRealYLikeOriginal;
            int r=(C2OriginalMovementMathV352.Norma(x-sx,y-sy)>>4)*92/100;
            int type=ComplexLocalNewStateV430LikeOriginal(u),state=type*4+12;
            var traits=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(u.Info);
            int dh=Math.Max(0,cob.Rz-ComplexTerrainHeightV430LikeOriginal(u,x,y));
            dh=dh*_battle.CannonHeightBonusV439/100;
            int min=traits.AttackRadius1[type],max=traits.AttackRadius2[type]+dh;
            // Leave room for the model's pivot displacement during rotation.
            int margin=Math.Min(192,Math.Max(1,(max-min)/3));
            if(CannonNeedsCrewV439(u)){u.ArtilleryPointOrderV439=null;return;}
            if(r>max || r<min || (order.AdjustingRangeV442 && (r<min+margin/2 || r>max-margin/2)))
            {
                // CreatePath keeps the point order as parent; it must not become
                // a new external movement command and cancel the fire mission.
                if(!order.AdjustingRangeV442)
                {
                    int distance=C2OriginalMovementMathV352.Norma(sx-x,sy-y);
                    int dx=sx-x,dy=sy-y;
                    if(distance==0){int d=(u.RealDirPrecise+128)&255;dx=C2OriginalMovementMathV352.TCos[d];dy=C2OriginalMovementMathV352.TSin[d];distance=256;}
                    int desired=(r<min?min+margin:max-margin)*1600/92;
                    order.RangeMoveXV442=x+(int)((long)dx*desired/distance);
                    order.RangeMoveYV442=y+(int)((long)dy*desired/distance);
                    order.AdjustingRangeV442=true;
                }
                C2OriginalOrderChainV352.SubmitTaskMoveV433LikeOriginal(u.Info,order.RangeMoveXV442,order.RangeMoveYV442,0,false,0,true,
                    "AttackPointByComplexObject::CreatePath");
                return;
            }
            if(u.HasMoveTargetLikeOriginal || C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(u.Info))
            {
                bool auto=u.ArtilleryAutoFireV442;
                C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(u.Info);
                u.ArtilleryAutoFireV442=auto;u.ArtilleryPointOrderV439=order;
            }
            order.AdjustingRangeV442=false;
            if(cob.StartState==0 && cob.GroundStandState==8)cob.GroundStandState=state;
            if(cob.StartState!=cob.FinalState || !cob.Charged || !cob.ResSubtracted)return;
            int direction=C2OriginalMovementMathV352.GetDir(x-sx,y-sy);
            int enemyOct=((direction+8)>>4)&15;
            int myDir=(int)cob.Quants[0].Fi, myOct=((myDir+8+512)>>4)&15;
            if(enemyOct!=myOct && ((((direction+1+8)>>4)&15)==myOct || (((direction-1+8+256)>>4)&15)==myOct))enemyOct=myOct;
            cob.DestDir=-1;
            int desiredDir=enemyOct*16;
            int diff=Math.Abs((int)unchecked((sbyte)(desiredDir-myDir)));
            if((cob.FinalState==state || cob.FinalState==8) && diff<6)
            {
                if(enemyOct!=myOct){cob.FinalState=0;return;}
                if(cob.FinalState!=8)
                {cob.GroundStandState=8;ComplexTryTransformV430LikeOriginal(cob,8);return;}
                if(!string.IsNullOrEmpty(traits.WeaponName[type]))
                {
                    var q=cob.Quants[0];var desc=cob.Desc.Chain[0];int angle=((int)q.Fi)&255;
                    int cos=C2OriginalMovementMathV352.TCos[angle],sin=C2OriginalMovementMathV352.TSin[angle];
                    int muzzleX=(int)(q.Xc/16)+((cos*desc.AttackXV437)>>8)-((sin*desc.AttackYV437)>>8);
                    int muzzleY=(int)(q.Yc/16)+cob.Rz*2+((sin*desc.AttackXV437)>>8)+((cos*desc.AttackYV437)>>8);
                    int muzzleZ=cob.Rz+desc.AttackZV437;
                    int targetZ=r<512?muzzleZ:ComplexTerrainHeightV430LikeOriginal(u,x,y);
                    if(traits.Razbros!=0)
                    {
                        int spreadR=C2OriginalMovementMathV352.Norma(sx-x,sy-y)>>9;
                        x+=((C2RetailRandomV407LikeOriginal.Rando(u.Info)>>5)-512)*spreadR*traits.Razbros/5000;
                        y+=((C2RetailRandomV407LikeOriginal.Rando(u.Info)>>5)-512)*spreadR*traits.Razbros/5000;
                    }
                    C2CombatRuntimeV334LikeOriginal.EmitComplexShotV439(u.Info,type,muzzleX,muzzleY,muzzleZ,x,y,targetZ);
                    u.ArtilleryShotsFiredV439++;
                    cob.Charged=true;cob.ResSubtracted=false;
                }
                ComplexTestResSubtractV432LikeOriginal(u,cob);
                cob.GroundStandState=state;ComplexTryTransformV430LikeOriginal(cob,state);
                if(!cob.ResSubtracted){cob.TransTime=1;ComplexCancelRechargementV430LikeOriginal(cob);}
                if(order.Shots>0)order.Shots--;
                // Native repeats after zero only while an enemy building remains
                // in the original target's 128-pixel presence cell.
                if(order.Shots==0 && !EnemyBuildingAtArtilleryPointV439(u.Info,order.X,order.Y))u.ArtilleryPointOrderV439=null;
                return;
            }
            if(myOct==enemyOct && diff<6)
            {cob.GroundStandState=state;ComplexTryTransformV430LikeOriginal(cob,state);return;}
            if(cob.FinalState!=0 && cob.FinalState!=4)
            {ComplexTryTransformV430LikeOriginal(cob,0);cob.GroundStandState=0;cob.GroundMotionState=4;}
            else cob.DestDir=desiredDir;
        }

        private static bool EnemyBuildingAtArtilleryPointV439(C2NeutralPeasantUnitInfoV2LikeOriginal unit,int x,int y)
        {
            foreach(var b in UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsSortMode.None))
                if(b!=null && b.LifeLikeOriginal>0 && ((int)b.RealX>>11)==(x>>11) && ((int)b.RealY>>11)==(y>>11) && b.Nation!=unit.Nation)return true;
            return false;
        }
    }

    public sealed partial class C2CombatRuntimeV334LikeOriginal
    {
        // V442 routes visible cannon/howitzer flight and the cannon ricochet
        // chain through the integer weapon.nds simulation. Canister, rocket
        // flight and the remaining explosion/fragment graphs use the older
        // adapter; this boundary is not a claim of complete weapon parity.
        internal static void EmitComplexShotV439(C2NeutralPeasantUnitInfoV2LikeOriginal unit,int type,
            int xs,int ys,int zs,int targetRealX,int targetRealY,int targetZ)
        {
            var combat=unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if(combat==null)combat=unit.EnsureUnityProxyLikeOriginal().AddComponent<C2CombatRuntimeV334LikeOriginal>();
            combat.enabled=false;combat._unit=unit;combat._targetUnit=null;combat._targetBuilding=null;
            combat._md=C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            combat._artillery=true;combat._slowRecharge=false;combat._attackMode=type;
            combat.EmitArtilleryMuzzleV442(type,xs,ys,zs,targetRealX,targetRealY);
            if(combat.TryEmitArtilleryProjectileV442(type,xs,ys,zs,targetRealX,targetRealY,targetZ))return;
            Vector3 muzzle=unit.OwnerMode.ArtilleryPointV437(xs,ys,zs);
            combat.FireWeaponAtTargetLikeOriginal(type,ResolveWeaponEffectLikeOriginal(combat._md,type),targetRealX,targetRealY,muzzle);
        }
    }
}
