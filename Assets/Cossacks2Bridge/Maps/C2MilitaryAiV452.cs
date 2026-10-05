using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // TacticalAI.cpp: formations and cannons are separate atoms; villages,
    // command centres (200) and town centres (300) are strategic main points.
    // This adapter uses the port's real movement/combat/settlement processors.
    // It never applies damage, captures villages or creates soldiers itself.
    internal sealed partial class C2MilitaryAiV452
    {
        internal sealed class Squad
        {
            internal long Key;
            internal int Group = -1;
            internal bool Cannon;
            internal readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> Members = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            internal C2NeutralPeasantUnitInfoV2LikeOriginal Leader;
            internal Vector2 Position, LastDestination;
            internal float Force, NextOrder, LastProgressTime;
            internal Vector2 LastProgressPosition;
            internal object Objective, CombatTarget;
            internal C2NeutralPeasantUnitInfoV2LikeOriginal PendingUnit;
            internal C2SettlementBuildingSelectableV1LikeOriginal PendingBuilding;
            internal int PendingIndex = -1;
            internal int Mode = -1;
            internal int ObjectiveOwner = -1;
            internal string Order = "";
        }
        internal sealed class Point
        {
            internal object Identity;
            internal Vector2 Position;
            internal int Owner, Value;
            internal float EnemyForce, FriendlyForce, EnemyPressure, FriendlyPressure, Hold, Take, Weight, Assigned;
            internal bool HasInfantry;
            internal C2SettlementDipVillageV336LikeOriginal Village;
            internal C2SettlementBuildingSelectableV1LikeOriginal Building;
        }
        internal readonly C2EconomicAiBrainV451 Brain;
        internal readonly List<Squad> Squads = new List<Squad>();
        internal readonly List<Point> Points = new List<Point>();
        private readonly Dictionary<long,Squad> squads = new Dictionary<long,Squad>();
        private readonly Dictionary<string,int> forces = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        private float nextAssembly;
        internal int Assemblies, MoveOrders, AttackOrders, CaptureAssignments, DefenseAssignments;
        private C2BattleTerrainMode Map => Brain.Driver.Map;
        internal C2MilitaryAiV452(C2EconomicAiBrainV451 brain) { Brain=brain; }
        internal static Vector2 Position(C2NeutralPeasantUnitInfoV2LikeOriginal u) => new Vector2((u.RealXFloat!=0?u.RealXFloat:u.RealX)/16f,(u.RealYFloat!=0?u.RealYFloat:u.RealY)/16f);
        internal static bool Alive(C2NeutralPeasantUnitInfoV2LikeOriginal u) => u!=null&&!u.IsDeadLikeOriginal&&u.isActiveAndEnabled;
        internal bool Friendly(C2NeutralPeasantUnitInfoV2LikeOriginal u) => (C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(u)&(1<<Brain.Nation))!=0;
        private bool Own(C2NeutralPeasantUnitInfoV2LikeOriginal u) => Alive(u)&&u.OwnerMode==Map&&u.Nation==Brain.Nation&&!u.SettlementAiControlledLikeOriginal&&u.CanReceiveOrdersLikeOriginal();
        internal int Force(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            var md=C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u);
            string path=md.Path??"";
            if(!forces.TryGetValue(path,out int force)) {
                force=1;
                if(File.Exists(path)) {
                    var match=Regex.Match(File.ReadAllText(path),@"(?m)^\s*FORCE\s+(\d+)");
                    if(match.Success&&int.TryParse(match.Groups[1].Value,out int raw))force=Math.Max(1,raw);
                }
                forces[path]=force;
            }
            return force;
        }
        private void CollectSquads()
        {
            Squads.Clear();foreach(var s in squads.Values){s.Members.Clear();s.Force=0;s.Position=Vector2.zero;}
            foreach(var u in Brain.Driver.Units) {
                if(!Own(u))continue;
                var md=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(u);
                bool grouped=C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(u,out int group);
                if(!grouped&&!md.Artilery)continue; // recruits wait for a real formation
                long key=grouped?((long)group<<1):((long)u.GetInstanceID()<<1)|1;
                if(!squads.TryGetValue(key,out var s)){s=new Squad{Key=key,Group=grouped?group:-1,Cannon=!grouped};squads[key]=s;}
                if(s.Members.Count==0)Squads.Add(s);
                s.Members.Add(u);s.Position+=Position(u);s.Force+=Force(u);
            }
            foreach(var key in squads.Where(p=>p.Value.Members.Count==0).Select(p=>p.Key).ToArray())squads.Remove(key);
            foreach(var s in Squads) {
                s.Position/=s.Members.Count;s.Leader=s.Members[0];
                if(s.Group>=0&&C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierRepresentativeV332LikeOriginal(s.Leader,out var soldier))s.Leader=soldier;
            }
            Squads.Sort((a,b)=>a.Cannon!=b.Cannon?(a.Cannon?1:-1):b.Force.CompareTo(a.Force));
        }
        internal void Tick()
        {
            if(Time.time>=nextAssembly){nextAssembly=Time.time+3;TryAssemble();}
            CollectSquads();if(Squads.Count==0)return;
            BuildPoints();AssignPoints();
        }
        // One squad order per frame, interleaved between nations by the driver.
        private int squadTurn;
        internal void Step()
        {
            if(Squads.Count==0)return;
            var squad=Squads[(squadTurn++&int.MaxValue)%Squads.Count];
            if(!Alive(squad.Leader)||squad.Leader.OwnerMode!=Map||squad.Leader.Nation!=Brain.Nation)return;
            Command(squad);
        }
        private void TryAssemble()
        {
            foreach(var centre in Brain.Buildings) {
                if(!C2ResourceStorageV445.Ready(centre)||!C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(centre).CommandCenter)continue;
                Vector2 p=new Vector2(centre.RealX/16f,centre.RealY/16f);
                var counts=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
                foreach(var u in Brain.Driver.Units)if(Own(u)&&!C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u)&&
                    (Position(u)-p).sqrMagnitude<=1500*1500&&!C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u).Peasant) {
                    string id=C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(u);
                    counts.TryGetValue(id,out int n);counts[id]=n+1;
                }
                foreach(var record in C2FormationCreateCatalogV165LikeOriginal.BuildAllRecordsSnapshotLikeOriginal()) {
                    var option=record.Options.FirstOrDefault(o=>o.AmountIndex==0&&o.UnitCount>0);
                    if(option==null||!counts.TryGetValue(record.UnitId,out int count)||count<option.UnitCount)continue;
                    // Same complete-size NDS option as the player's global button.
                    // Command personnel are optional in CreateBrigInZone itself.
                    if(C2FormationRuntimeV167LikeOriginal.TryCreateGlobalBrigFromCommandCenterV172LikeOriginal(centre,record,option,out int group,out _)) {
                        Assemblies++;Debug.Log("[C2:MILITARY V452] assembled owner="+Brain.Nation+" group="+group+" type="+record.UnitId+" amount="+option.UnitCount);return;
                    }
                }
            }
        }
        internal static float RouteDistance(Vector2 from,Vector2 to,int nation)
        {
            int a=C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal((int)from.x,(int)from.y);
            int b=C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal((int)to.x,(int)to.y);
            int count=C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal();
            if(a<0||b<0||a>=0xFFFE||b>=0xFFFE||count<=0)return float.PositiveInfinity;
            if(a==b)return Vector2.Distance(from,to);
            int distance=C2TopologyCoreV401LikeOriginal.GetLinksDistV401LikeOriginal(a*count+b,0,(byte)nation);
            return distance>=0xFFFE?float.PositiveInfinity:Mathf.Max(Vector2.Distance(from,to),distance*64f);
        }
    }
}
