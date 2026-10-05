using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed partial class C2EconomicAiBrainV451
    {
        private sealed class BuildSearch
        {
            internal XElement Action;
            internal C2OriginalProduceItemV13 Item;
            internal readonly List<Vector2Int> Candidates=new List<Vector2Int>();
            internal int Index;
            internal float BestScore=float.NegativeInfinity;
            internal Vector2Int Best;
        }
        private BuildSearch search;
        private readonly Dictionary<string,float> retryBuild=new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int,float> retryWorker=new Dictionary<int,float>();
        private readonly Dictionary<string,int[]> prices=new Dictionary<string,int[]>(StringComparer.OrdinalIgnoreCase);
        private bool Price(C2OriginalProduceItemV13 item,int percent,out int[] price)
        {
            price=null;
            if(!prices.TryGetValue(item.MdName,out var raw)) {
                if(!C2NationResourceEconomyV348LikeOriginal.TryGetUnitPriceLikeOriginal(item.MdName,out raw,out _))return false;
                prices[item.MdName]=raw;
            }
            price=C2UpgradeEconomyV445.UnitPrice(Nation,item.UnitId,raw);return WithinBudget(price,percent);
        }
        private void Produce(XElement action)
        {
            string id=Script.Resolve((string)action.Element("UnitType"));
            int max=Int(action,"MaxAmount"),current=Count(Alive,id),pack=Math.Max(1,Int(action,"PackSize",8));
            foreach(var b in Buildings)current+=C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(b,id).Count;
            int room=Math.Min(pack,max-current);if(room<=0)return;
            foreach(var b in Buildings) {
                if(room<=0)break;if(!C2ResourceStorageV445.Ready(b))continue;
                var item=C2OriginalProduceCatalogV13.BuildForSelectedBuilding(b,out _).Find(i=>string.Equals(i.UnitId,id,StringComparison.OrdinalIgnoreCase));
                if(item==null||item.Building||!C2UpgradeEconomyV445.CanProduce(Driver.Map,Nation,id))continue;
                var queue=b.GetComponent<C2BuildingProductionCardsRuntimeV114>()??b.gameObject.AddComponent<C2BuildingProductionCardsRuntimeV114>();
                for(int n=0;n<room;) {
                    // Recheck the stock fraction after each actual payment.
                    if(!Price(item,Int(action,"ResourcePercent",100),out _))break;
                    n++;if(!Chance(Int(action,"Probability",100)))continue;
                    int before=C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(b,id).Count;
                    queue.EnqueueForNationV449(item,1,false,b,Nation);
                    int after=C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(b,id).Count;
                    if(after<=before)break;Orders++;room--;n--;
                }
            }
        }
        private bool Recruitable(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            if(u==null||u.Nation!=Nation||u.OwnerMode!=Driver.Map||u.IsDeadLikeOriginal||!u.CanBuildOrRepairLikeOriginal()||u.SettlementAiControlledLikeOriginal)return false;
            var task=u.GetComponent<C2GameplayUnitTaskV1>();var build=u.GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
            if(build!=null&&build.isActiveAndEnabled||task!=null&&task.CarriedAmountV447>0)return false;
            if(C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(u))return false;
            var runtime=u.RuntimeLinkCachedLikeOriginal?.Runtime;
            if(runtime!=null&&(runtime.HiddenInsideBuildingLikeOriginal||runtime.PreciseBornPathLikeOriginal))return false;
            return task!=null&&task.ResourceTaskActiveV445||C2PeasantAutoWorkV447.CanAutoAssignForNationV451(u,Nation);
        }
        private void PlanBuilding(XElement action)
        {
            if(search!=null)return;
            string id=Script.Resolve((string)action.Element("UnitType"));
            if(retryBuild.TryGetValue(id,out float until)&&Time.time<until||Count(Alive,id)>=Int(action,"MaxAmount")||!Chance(Int(action,"Probability",100)))return;
            var req=action.Element("Requirements");
            if(Buildings.Count(b=>!C2ResourceStorageV445.Ready(b))>(Bool(req,"MultipleConstruction")?1:0))return;
            var workers=Workers.Where(Recruitable).ToList();
            if(workers.Count<Math.Max(1,Int(req,"MinPeasants",10)))return;
            var item=C2OriginalProduceCatalogV13.BuildForSelectedUnit(workers[0],out _).Find(i=>i.Building&&string.Equals(i.UnitId,id,StringComparison.OrdinalIgnoreCase));
            if(item==null||!C2UpgradeEconomyV445.CanProduce(Driver.Map,Nation,id)||!Price(item,Int(action,"ResourcePercent",100),out _))return;
            search=new BuildSearch {Item=item,Action=action};
            int minimum=Math.Max(256,Int(req,"MinDistFromCenter"));
            // Search concentric diamonds in native map pixels. Spread validation
            // over frames: CheckSite includes footprints, terrain and live blockers.
            for(int radius=minimum;radius<=Math.Max(2048,minimum+1024);radius+=256)
                for(int side=0;side<4;side++)for(int k=0;k<4;k++) {
                    int dx=radius-radius*k/4,dy=radius*k/4;
                    for(int turn=0;turn<side;turn++){int old=dx;dx=-dy;dy=old;}
                    search.Candidates.Add(new Vector2Int(Mathf.RoundToInt(Centre.x)+dx,Mathf.RoundToInt(Centre.y)+dy));
                }
        }
        internal void AdvanceBuildingSearch()
        {
            if(search==null)return;
            var s=search;var req=s.Action.Element("Requirements");
            // One placement validation per frame for this owner. No unbounded
            // terrain/pathfinding burst when a new script rule becomes available.
            if(s.Index<s.Candidates.Count) {
                Vector2Int p=s.Candidates[s.Index++];float road=Driver.Map.AiRoadDistanceV451(p);
                if(road<250)return;
                int a=C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal((int)Centre.x,(int)Centre.y);
                int b=C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(p.x,p.y);
                int areas=C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal();
                if(a>=0xFFFE||b>=0xFFFE||a<0||b<0||areas<=0||a!=b&&C2TopologyCoreV401LikeOriginal.GetLinksDistV401LikeOriginal(a*areas+b,0,(byte)Nation)>=0xFFFE)return;
                if(!C2BuildingPlacementPreviewV27.CheckSiteV450(Driver.Map,s.Item.MdName,p.x*16,p.y*16,out _))return;
                float score=100000f/(Vector2.Distance(Centre,p)+150);
                if(Bool(req,"NearRoad")&&road<700)score+=1000;
                if(Bool(req,"NearTrees")&&Driver.Map.AiResourceDistanceV451(p,0)<600)score+=500;
                if(Bool(req,"NearStones")) {float distance=Driver.Map.AiResourceDistanceV451(p,2);if(distance<500)score+=100;if(distance<700)score+=100;if(distance<900)score+=100;}
                // Source FarFromSameType really adds inverse distance despite its name.
                if(Bool(req,"FarFromSameType"))foreach(var building in Buildings)
                    if(string.Equals(C2OriginalProduceCatalogV13.ResolveMemberIdForSelectedBuilding(building),s.Item.UnitId,StringComparison.OrdinalIgnoreCase))
                        score+=200000f/(Vector2.Distance(p,new Vector2(building.RealX/16f,building.RealY/16f))+200);
                if(Bool(req,"NearToEnemy")||Bool(req,"FarFromEnemy")) {
                    var enemy=Driver.Buildings.Where(x=>x!=null&&x.OwnerMode==Driver.Map&&x.Nation!=Nation&&x.Nation<7&&C2ResourceStorageV445.Ready(x))
                        .OrderBy(x=>(new Vector2(x.RealX/16f,x.RealY/16f)-Centre).sqrMagnitude).FirstOrDefault();
                    if(enemy!=null){float angle=Vector2.Angle(p-Centre,new Vector2(enemy.RealX/16f,enemy.RealY/16f)-Centre)*128/180;
                        score+=(Bool(req,"FarFromEnemy")?angle:-angle)*10;}
                }
                if(score>s.BestScore){s.BestScore=score;s.Best=p;}return;
            }
            search=null;retryBuild[s.Item.UnitId]=Time.time+15;
            if(float.IsNegativeInfinity(s.BestScore))return;
            // Revalidate availability, ownership and funds at commit time. A
            // battlefield can change during the amortized search.
            Snapshot();var workers=Workers.Where(Recruitable).OrderBy(u=>(new Vector2(u.RealX/16f,u.RealY/16f)-s.Best).sqrMagnitude)
                .Take(Math.Max(1,Int(req,"MaxPeasants",15))).ToList();
            if(workers.Count<Math.Max(1,Int(req,"MinPeasants",10))||Count(Alive,s.Item.UnitId)>=Int(s.Action,"MaxAmount")||
                Buildings.Count(b=>!C2ResourceStorageV445.Ready(b))>(Bool(req,"MultipleConstruction")?1:0)||!Price(s.Item,Int(s.Action,"ResourcePercent",100),out _))return;
            if(C2BuildingPlacementPreviewV27.TryBuildForNationV450(Driver.Map,Nation,s.Item.UnitId,s.Best.x*16,s.Best.y*16,workers,out var site,out _)) {
                Builds++;Driver.Statistics.Observe(site.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>());
                Debug.Log("[C2:AI V451] owner="+Nation+" construction="+s.Item.UnitId+" workers="+workers.Count);
            }
        }
        internal void DistributeWorkers()
        {
            int[] assigned=new int[6],target=new int[6];int pool=0;
            foreach(var u in Workers){var t=u.GetComponent<C2GameplayUnitTaskV1>();
                if(t!=null&&t.ResourceTaskActiveV445&&t.ResourceIdV447<6){assigned[t.ResourceIdV447]++;pool++;}
                else if(C2PeasantAutoWorkV447.CanAutoAssignForNationV451(u,Nation))pool++;
            }
            // City::HandleBrigades: these are WEIGHTS, not worker counts. Building,
            // combat and explicitly moving workers are outside the resource pool.
            int food=float.IsPositiveInfinity(Driver.Map.AiResourceDistanceV451(Centre,3))?0:DesiredWorkers[3];
            long total=(long)DesiredWorkers[0]+DesiredWorkers[2]+food;if(total<=0)return;
            target[3]=(int)(pool*(long)food/total);target[2]=(int)(pool*(long)DesiredWorkers[2]/total);target[0]=pool-target[3]-target[2];
            int budget=2;
            foreach(var u in Workers) {
                if(budget<=0)break;
                if(retryWorker.TryGetValue(u.GetInstanceID(),out float retry)&&Time.time<retry)continue;
                var task=u.GetComponent<C2GameplayUnitTaskV1>();int previous=task!=null&&task.ResourceTaskActiveV445?task.ResourceIdV447:-1;
                if(previous>=0) {
                    if(previous>=6||assigned[previous]<=target[previous]||task.CarriedAmountV447>0||!Recruitable(u))continue;
                } else if(!C2PeasantAutoWorkV447.CanAutoAssignForNationV451(u,Nation))continue;
                int best=-1;foreach(int r in new[]{3,2,0})if(target[r]>assigned[r]){best=r;break;}
                if(best<0)break;budget--;retryWorker[u.GetInstanceID()]=Time.time+8;
                if(C2GameplayUnitTaskV1.AssignResourceV447(u,(byte)best)){Assignments++;assigned[best]++;if(previous>=0)assigned[previous]--;}
            }
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        internal float AiResourceDistanceV451(Vector2 p,int resource)
        {
            float best=float.PositiveInfinity;
            foreach(var s in _c2OriginalResourcesV1)if(s.Enabled&&s.Def!=null&&s.Def.Resource==resource)
                best=Mathf.Min(best,(new Vector2(s.X,s.Y)-p).sqrMagnitude);
            return Mathf.Sqrt(best);
        }
        internal float AiRoadDistanceV451(Vector2 p)
        {
            float best=float.PositiveInfinity;
            for(int i=0;i<C2MovementGetRoadCountV425LikeOriginal();i++)if(C2MovementTryGetRoadKnotV425LikeOriginal(i,out int x,out int y,out int links))
                for(int k=0;k<links;k++)if(C2MovementTryGetRoadLinkV425LikeOriginal(i,k,out int next,out _,out _,out _)&&
                    C2MovementTryGetRoadKnotV425LikeOriginal(next,out int bx,out int by,out _)) {
                    Vector2 a=new Vector2(x,y),delta=new Vector2(bx-x,by-y);
                    Vector2 closest=a+delta*(delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude):0);
                    best=Mathf.Min(best,(closest-p).sqrMagnitude);
                }
            return Mathf.Sqrt(best);
        }
    }
}
