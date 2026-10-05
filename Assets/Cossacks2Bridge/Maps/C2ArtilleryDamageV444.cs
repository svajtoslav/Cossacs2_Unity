using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2ArtilleryExperienceV444
    {
        // VUI_Actions.cpp::va_SP_KillsAward applies to brigades AND cannons.
        internal static int Rank(int kills) => (kills>20?1:0)+(kills>60?1:0)+
            (kills>120?1:0)+(kills>300?1:0)+(kills>600?1:0);
    }

    internal static class C2ArtilleryDamageV444
    {
        private static readonly Dictionary<string,int> Radii=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        private static int _buildingFrame=-1;
        private static C2SettlementBuildingSelectableV1LikeOriginal[] _buildings;

        internal static int VictimRadius(string path)
        {
            if(string.IsNullOrEmpty(path))return 0;
            if(Radii.TryGetValue(path,out int cached))return cached;
            int media=0,explosion=0;
            if(File.Exists(path))foreach(string line in File.ReadLines(path))
            {
                var t=line.Trim().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(t.Length>=3&&t[0]=="EXPLMEDIA")int.TryParse(t[2],out media);
                if(t.Length>=2&&t[0]=="EXPLRADIUS")int.TryParse(t[1],out explosion);
            }
            return Radii[path]=Math.Max(0,media+explosion);
        }

        internal static bool InCircle(int x,int y,int vx,int vy,int radiusReal,int victimRadiusPixels)
        {
            // NewMon.cpp::MakeRoundDamage scans the surrounding 3x3 presence
            // cells, then tests Norma against Radius+(EMediaRadius+ExplRadius)*16.
            return Math.Abs((vx>>11)-(x>>11))<=1 && Math.Abs((vy>>11)-(y>>11))<=1 &&
                C2OriginalMovementMathV352.Norma(vx-x,vy-y)<radiusReal+victimRadiusPixels*16;
        }

        internal static bool Apply(C2NeutralPeasantUnitInfoV2LikeOriginal source,int type,
            C2FlightWeaponV442 weapon,int damage,int x,int y,
            Action<C2SettlementBuildingSelectableV1LikeOriginal,int> damageBuilding)
        {
            if(source==null || damage<=0 || weapon.Radius<=0)return false;
            bool hit=false;int firstCell=int.MaxValue;
            C2NeutralPeasantUnitInfoV2LikeOriginal firstUnit=null;
            C2SettlementBuildingSelectableV1LikeOriginal firstBuilding=null;
            var st=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(source);
            foreach(var victim in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
            {
                if(victim==null || victim==source || victim.IsDeadLikeOriginal || !victim.isActiveAndEnabled || victim.OwnerMode!=source.OwnerMode)continue;
                int vx=(int)victim.RealXFloat,vy=(int)victim.RealYFloat;
                // Reject distant presence cells before touching per-type data. A canister
                // contains 150 pellets; parsing target metadata in this loop stalls shots.
                if(Math.Abs((vx>>11)-(x>>11))>1 || Math.Abs((vy>>11)-(y>>11))>1)continue;
                var vt=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(victim);
                if(!InCircle(x,y,vx,vy,weapon.Radius,Math.Max(0,vt.ExplosionMediaRadiusV445+vt.ExplosionRadiusV445)))continue;
                if(st.FriendlyFireCapable && !C2CombatCoreV408LikeOriginal.CanAttackRelationV408LikeOriginal(source,victim) &&
                    C2OriginalMovementMathV352.Norma((int)source.RealXFloat-vx,(int)source.RealYFloat-vy)<st.FreeShotDist*16)continue;
                if(weapon.Splash)
                {C2CombatCoreV408LikeOriginal.MakeDamageV408LikeOriginal(victim,damage,source,type,true,true);hit=true;}
                else
                {
                    int cell=((vy>>11)-(y>>11)+1)*3+(vx>>11)-(x>>11)+1;
                    if(cell<firstCell){firstCell=cell;firstUnit=victim;}
                }
            }
            if(_buildingFrame!=Time.frameCount)
            {_buildingFrame=Time.frameCount;_buildings=UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsSortMode.None);}
            foreach(var b in _buildings)
            {
                if(b==null || b.OwnerMode!=source.OwnerMode || b.LifeLikeOriginal<=0)continue;
                if(Math.Abs((b.RealX>>11)-(x>>11))>1 || Math.Abs((b.RealY>>11)-(y>>11))>1)continue;
                var bt=C2CombatCoreV408LikeOriginal.GetTraitsForBuildingV413LikeOriginal(b);
                if(!InCircle(x,y,b.RealX,b.RealY,weapon.Radius,Math.Max(0,bt.ExplosionMediaRadiusV445+bt.ExplosionRadiusV445)))continue;
                if(st.FriendlyFireCapable && b.Nation==source.Nation &&
                    C2OriginalMovementMathV352.Norma((int)source.RealXFloat-b.RealX,(int)source.RealYFloat-b.RealY)<st.FreeShotDist*16)continue;
                if(weapon.Splash){damageBuilding(b,damage);hit=true;}
                else
                {
                    int cell=((b.RealY>>11)-(y>>11)+1)*3+(b.RealX>>11)-(x>>11)+1;
                    if(cell<firstCell){firstCell=cell;firstUnit=null;firstBuilding=b;}
                }
            }
            if(firstUnit!=null){C2CombatCoreV408LikeOriginal.MakeDamageV408LikeOriginal(firstUnit,damage,source,type,true,true);hit=true;}
            else if(firstBuilding!=null){damageBuilding(firstBuilding,damage);hit=true;}
            return hit;
        }
    }
}
