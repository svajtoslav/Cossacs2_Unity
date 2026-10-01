using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntime
    {
        internal bool ArtilleryAutoFireV442;
    }
    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal void ToggleArtilleryAutoFireV442(C2UnitOriginalRuntime u)
        {
            if(u?.OriginalComplexObjectV430LikeOriginal==null || CannonNeedsCrewV439(u))return;
            bool on=!u.ArtilleryAutoFireV442;
            StopArtilleryV439(u);u.ArtilleryAutoFireV442=on;
        }
        // UnitAbility.cpp::FindCoordForCannon. Search only the current firing
        // sector, with at least three enemies in formation, and protect friends
        // in front of canister. The enabled ability survives its own shot order.
        private void StepArtilleryAutoFireV442(C2UnitOriginalRuntime u)
        {
            if(!u.ArtilleryAutoFireV442)return;
            var cob=u.OriginalComplexObjectV430LikeOriginal;
            if(CannonNeedsCrewV439(u)){u.ArtilleryAutoFireV442=false;return;}
            if(u.ArtilleryPointOrderV439!=null || u.ArtilleryChargeOrderV439>=0 || u.HasMoveTargetLikeOriginal || cob.DestDir>=0)return;
            if(!GetArtilleryChargeV439(u,out int type,out int charge)||charge!=100)return;
            if((C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal&31)!=(u.Info.C2ObjectIndexV408LikeOriginal&31))return;
            if(!FindArtilleryAutoTargetV442(u,out int x,out int y))return;
            if(AttackArtilleryPointV439(u,x*16,y*16,0))u.ArtilleryAutoFireV442=true;
        }
        internal static bool FindArtilleryAutoTargetV442(C2UnitOriginalRuntime u,out int xx,out int yy)
        {
            xx=yy=0;
            var gun=u.Info;var traits=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(gun);
            int type=u.ArtilleryAmmoV442,min=traits.AttackRadius1[type],max=traits.AttackRadius2[type];
            int dir=u.RealDirPrecise&255,oct=((dir+8)>>4)&15;
            int x=(int)gun.RealXFloat>>4,y=(int)gun.RealYFloat>>4,mask=C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(gun);
            var enemies=new List<Vector2Int>();int friends=0;
            int sectorRange=min==0?max*10/6:max;
            foreach(var other in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
            {
                if(other==null||other==gun||other.IsDeadLikeOriginal||!other.isActiveAndEnabled)continue;
                int ox=(int)other.RealXFloat>>4,oy=(int)other.RealYFloat>>4;
                int d=C2OriginalMovementMathV352.Norma(ox-x,oy-y);
                if(d>Math.Max(max+700,sectorRange))continue;
                bool friendly=(C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(other)&mask)!=0;
                if(min<80)
                {
                    int angle=C2OriginalMovementMathV352.GetDir(ox-x,oy-y);
                    int delta=unchecked((sbyte)(angle-oct*16));
                    int cx=x+C2OriginalMovementMathV352.TCos[oct*16]*sectorRange/512;
                    int cy=y+C2OriginalMovementMathV352.TSin[oct*16]*sectorRange/512;
                    if(Math.Abs(delta)>=13 || C2OriginalMovementMathV352.Norma(ox-cx,oy-cy)>sectorRange/2)continue;
                    if(friendly){if(++friends>3)return false;}
                    else if(InFormationForCaptureV441(other))enemies.Add(new Vector2Int(ox,oy));
                }
                else if(!friendly&&InFormationForCaptureV441(other))enemies.Add(new Vector2Int(ox,oy));
            }
            if(enemies.Count<3)return false;
            if(min<80){xx=x+C2OriginalMovementMathV352.TCos[oct*16]/2;yy=y+C2OriginalMovementMathV352.TSin[oct*16]/2;return true;}
            int best=0,step=(max+600-min)/64;
            for(int i=0;i<32;i++)for(int k=0;k<64;k++)
            {
                int a=(((dir>>4)<<4)-16+i+256)&255,d=min+k*step;
                int px=x+((C2OriginalMovementMathV352.TCos[a]*d)>>8),py=y+((C2OriginalMovementMathV352.TSin[a]*d)>>8);
                int angle=C2OriginalMovementMathV352.GetDir(px-x,py-y);
                if((((angle+8)>>4)&15)!=oct)continue;
                int count=0;foreach(var enemy in enemies)if(C2OriginalMovementMathV352.Norma(enemy.x-px,enemy.y-py)<100)count++;
                if(count<3||count<=best)continue;
                int dh=Math.Max(0,u.OriginalComplexObjectV430LikeOriginal.Rz-ComplexTerrainHeightV430LikeOriginal(u,px*16,py*16));
                dh=dh*gun.OwnerMode.CannonHeightBonusV439/100;
                if(C2OriginalMovementMathV352.Norma(px-x,py-y)>=max+dh)continue;
                best=count;xx=px;yy=py;
            }
            return best>0;
        }
    }
}
