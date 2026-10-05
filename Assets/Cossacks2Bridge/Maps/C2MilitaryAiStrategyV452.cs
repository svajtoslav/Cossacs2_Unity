using System;
using System.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed partial class C2MilitaryAiV452
    {
        private void BuildPoints()
        {
            Points.Clear();
            foreach(var village in Brain.Driver.Villages)if(village!=null&&village.isActiveAndEnabled&&village.MapV451==Map) {
                int resource=village.ResourceV445;
                Points.Add(new Point{Identity=village,Village=village,Owner=village.OwnerV445,Position=village.CentreV451,
                    Value=(uint)resource<6?Math.Max(0,Brain.SettlementPriority[resource]):100});
            }
            foreach(var building in Brain.Driver.Buildings)if(building!=null&&building.OwnerMode==Map&&building.Nation<7&&C2ResourceStorageV445.Ready(building)) {
                var md=C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(building);
                string id=C2OriginalProduceCatalogV13.ResolveMemberIdForSelectedBuilding(building);
                int value=md.CommandCenter?200:id.StartsWith("BldCen(",StringComparison.OrdinalIgnoreCase)?300:0;
                if(value>0)Points.Add(new Point{Identity=building,Building=building,Owner=building.Nation,Value=value,Position=new Vector2(building.RealX/16f,building.RealY/16f)});
            }
            float friendly=0,enemy=0;
            foreach(var u in Brain.Driver.Units)if(Alive(u)&&u.OwnerMode==Map){if(Friendly(u))friendly+=Force(u);else enemy+=Force(u);}
            foreach(var point in Points) {
                foreach(var u in Brain.Driver.Units) {
                    if(!Alive(u)||u.OwnerMode!=Map)continue;
                    float distance=Vector2.Distance(Position(u),point.Position);if(distance>=4000)continue;
                    // Native main-point influence: 4000 reach, full local force
                    // inside 1200, normalized by the nation's total field force.
                    float force=Force(u);
                    if(Friendly(u)){point.FriendlyPressure+=(force*10000/(friendly+1))*(4000-distance)/400000;if(distance<1200)point.FriendlyForce+=force;}
                    else {point.EnemyPressure+=(force*10000/(enemy+1))*(4000-distance)/400000;if(distance<1200)point.EnemyForce+=force;}
                }
                bool quiet=point.EnemyPressure*100/Mathf.Max(1,point.FriendlyPressure)<20&&point.EnemyForce==0;
                point.Hold=quiet?point.Value:0;point.Take=quiet?0:point.Value;
            }
            foreach(var point in Points) {
                float hold=point.Hold,take=point.Take;
                foreach(var other in Points)if(other!=point){float distance=Vector2.Distance(point.Position,other.Position);if(distance>0&&distance<10000){hold+=other.Hold*(10000-distance)/50000;take+=other.Take*(10000-distance)/50000;}}
                point.Weight=(hold+take)*point.EnemyPressure/100;
                if(point.Owner!=Brain.Nation)point.Weight+=take;
                // The supplied C++ MainPoint::GetOwner calculates rez but always
                // returns -1. Preserve actual ownership here, including peaceful
                // neutral villages, so they remain legitimate capture objectives.
            }
        }
        internal Point ChoosePoint(Squad squad)
        {
            Point best=null;float bestScore=0;
            float available=Squads.Where(s=>!s.Cannon).Sum(s=>s.Force);
            foreach(var point in Points) {
                if(point.Value<=0||point.Owner==Brain.Nation&&point.EnemyForce<=0)continue;
                if(squad.Cannon&&!point.HasInfantry)continue; // native cannon assignment requires infantry support
                if(point.Owner!=Brain.Nation&&point.EnemyForce>available&&available>0)continue;
                if(!squad.Cannon&&point.Assigned>0&&point.Assigned>=Mathf.Max(1,point.EnemyForce))continue;
                float distance=RouteDistance(squad.Position,point.Position,Brain.Nation);if(float.IsInfinity(distance))continue;
                float score=(point.Value+point.Weight)*1000/Mathf.Max(500,distance);
                if(point.Owner==Brain.Nation&&point.EnemyForce>0)score*=4;
                if(ReferenceEquals(squad.Objective,point.Identity))score*=1.15f;
                if(score>bestScore){best=point;bestScore=score;}
            }
            return best;
        }
        private void AssignPoints()
        {
            foreach(var squad in Squads) {
                var point=ChoosePoint(squad);
                if(point==null){squad.Objective=null;continue;}
                point.Assigned+=squad.Force;if(!squad.Cannon)point.HasInfantry=true;
                if(!ReferenceEquals(squad.Objective,point.Identity)||squad.ObjectiveOwner!=point.Owner) {
                    squad.Objective=point.Identity;squad.ObjectiveOwner=point.Owner;squad.NextOrder=0;
                    if(point.Village!=null&&point.Owner!=Brain.Nation)CaptureAssignments++;
                    if(point.Owner==Brain.Nation)DefenseAssignments++;
                    Debug.Log("[C2:MILITARY V452] owner="+Brain.Nation+" group="+squad.Group+" objective="+(point.Village!=null?"village":"base")+" targetOwner="+point.Owner+" target="+point.Position);
                }
            }
        }
        internal C2NeutralPeasantUnitInfoV2LikeOriginal NearestEnemy(Squad squad,float radius)
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal best=null;float bestDistance=radius*radius;
            foreach(var u in Brain.Driver.Units) {
                if(!Alive(u)||u.OwnerMode!=Map||Friendly(u)||u==squad.Leader)continue;
                var runtime=u.RuntimeLinkCachedLikeOriginal?.Runtime;
                if(runtime!=null&&(runtime.HiddenInsideBuildingLikeOriginal||runtime.PreciseBornPathLikeOriginal))continue;
                if(!C2CombatCoreV408LikeOriginal.CanAttackRelationV408LikeOriginal(squad.Leader,u)||!C2CombatCoreV408LikeOriginal.CanDamageMaterialV408LikeOriginal(squad.Leader,u))continue;
                float distance=(Position(u)-squad.Position).sqrMagnitude;
                if(distance<bestDistance&&!float.IsInfinity(RouteDistance(squad.Position,Position(u),Brain.Nation))) {best=u;bestDistance=distance;}
            }
            return best;
        }
    }
}
