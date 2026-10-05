using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // One map owns the brains and their clocks. No global selected-nation swap,
    // no resource reset and no automatic activation in the editor/test plateau.
    internal sealed class C2EconomicAiDriverV451 : MonoBehaviour
    {
        internal C2BattleTerrainMode Map;
        internal C2AiStatisticsV451 Statistics;
        internal C2NeutralPeasantUnitInfoV2LikeOriginal[] Units=Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        internal C2SettlementBuildingSelectableV1LikeOriginal[] Buildings=Array.Empty<C2SettlementBuildingSelectableV1LikeOriginal>();
        internal C2SettlementDipVillageV336LikeOriginal[] Villages=Array.Empty<C2SettlementDipVillageV336LikeOriginal>();
        internal readonly SortedDictionary<int,C2EconomicAiBrainV451> Brains=new SortedDictionary<int,C2EconomicAiBrainV451>();
        internal float Started;
        private float nextTick;
        private int buildTurn;
        internal static bool Owns(C2BattleTerrainMode map,int nation)=>map!=null&&map.GetComponent<C2EconomicAiDriverV451>() is C2EconomicAiDriverV451 driver&&driver.enabled&&driver.Brains.ContainsKey(nation);
        internal static bool TryStartOriginal(C2BattleTerrainMode map,int nation,string country,out string reason)
        {
            reason="Нужны карта, игрок и страна";if(map==null||(uint)nation>=7||string.IsNullOrEmpty(country))return false;
            try {
                var script=C2EconomicAiScriptV451.LoadOriginal(country);
                StartForNation(map,nation,script);
                reason="ИИ экономики и армии включён для игрока "+(nation+1)+" ("+country+"). Ресурсы расходуются по обычным правилам.";
                return true;
            } catch(Exception e){reason="Не удалось загрузить ИИ для "+country;Debug.LogWarning("[C2:AI V451] "+reason+": "+e.Message);return false;}
        }
        internal static void StopForNation(C2BattleTerrainMode map,int nation)
        {
            var driver=map!=null?map.GetComponent<C2EconomicAiDriverV451>():null;
            if(driver==null)return;driver.Brains.Remove(nation);if(driver.Brains.Count==0)driver.enabled=false;
        }
        internal static C2EconomicAiBrainV451 StartForNation(C2BattleTerrainMode map,int nation,C2EconomicAiScriptV451 script,int difficulty=1,bool bonuses=false)
        {
            if(map==null||(uint)nation>=7||script==null)throw new ArgumentException("AI requires a map, player 0..6 and a script");
            var driver=map.GetComponent<C2EconomicAiDriverV451>();
            if(driver==null){driver=map.gameObject.AddComponent<C2EconomicAiDriverV451>();driver.Map=map;driver.Started=Time.time;driver.Statistics=new C2AiStatisticsV451(map);}
            var brain=new C2EconomicAiBrainV451{Driver=driver,Nation=nation,Script=script,Difficulty=difficulty,AllowScriptBonuses=bonuses};
            brain.MilitaryV452=new C2MilitaryAiV452(brain);
            driver.Brains[nation]=brain;driver.enabled=true;driver.Refresh();brain.Snapshot();
            Debug.Log("[C2:AI V451] started owner="+nation+" difficulty="+difficulty+" scriptBonuses="+bonuses+" source="+script.Source);
            return brain;
        }
        internal void Refresh()
        {
            Units=C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            Buildings=FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsSortMode.None);
            Villages=FindObjectsByType<C2SettlementDipVillageV336LikeOriginal>(FindObjectsSortMode.None);
            foreach(var u in Units)Statistics.Observe(u);
            foreach(var b in Buildings)Statistics.Observe(b);
        }
        internal void Tick(float seconds)
        {
            Refresh();C2NationCityRuntimeV384ALikeOriginal.EnsureFreshPopulationV449();
            foreach(var brain in Brains.Values)brain.Tick(seconds);
        }
        private void Update()
        {
            if(Map==null||Statistics==null||Time.timeScale<=0||Brains.Count==0)return;
            if(Time.time>=nextTick){nextTick=Time.time+1;Tick(Time.time-Started);}
            int target=buildTurn++%Brains.Count,index=0;
            foreach(var brain in Brains.Values)if(index++==target){brain.AdvanceBuildingSearch();brain.MilitaryV452?.Step();break;}
        }
    }

    // NProduced/NKilled remain historical after a corpse leaves the registry.
    // Object references, not recycled Group[] indices, prevent double accounting.
    internal sealed class C2AiStatisticsV451
    {
        private sealed class Entry {internal int Nation;internal string Id;internal bool Dead;}
        private readonly C2BattleTerrainMode map;
        private readonly Dictionary<object,Entry> objects=new Dictionary<object,Entry>();
        private readonly Dictionary<string,int> produced=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),lost=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        internal C2AiStatisticsV451(C2BattleTerrainMode owner){map=owner;}
        private static string Key(int n,string id)=>n+":"+id;
        private static void Change(Dictionary<string,int> data,int n,string id,int delta){string key=Key(n,id);data.TryGetValue(key,out int v);data[key]=Math.Max(0,v+delta);}
        internal int Produced(int n,string id)=>produced.TryGetValue(Key(n,id),out int v)?v:0;
        internal int Lost(int n,string id)=>lost.TryGetValue(Key(n,id),out int v)?v:0;
        private void Record(object obj,int nation,string id,bool dead)
        {
            if(obj==null||string.IsNullOrEmpty(id))return;
            if(!objects.TryGetValue(obj,out var old)){old=new Entry{Nation=nation,Id=id};objects[obj]=old;Change(produced,nation,id,1);}
            if(!old.Dead&&(old.Nation!=nation||!string.Equals(old.Id,id,StringComparison.OrdinalIgnoreCase))) {
                Change(produced,old.Nation,old.Id,-1);Change(produced,nation,id,1);old.Nation=nation;old.Id=id;
            }
            if(dead&&!old.Dead){old.Dead=true;Change(lost,old.Nation,old.Id,1);}
        }
        internal void Observe(C2NeutralPeasantUnitInfoV2LikeOriginal u,bool killed=false)
        {
            if(u!=null&&u.OwnerMode==map)Record(u,u.Nation,C2OriginalProduceCatalogV13.ResolveMemberIdForSelectedUnit(u),killed||u.IsDeadLikeOriginal);
        }
        internal void Observe(C2SettlementBuildingSelectableV1LikeOriginal b,bool killed=false)
        {
            if(b!=null&&b.OwnerMode==map)Record(b,b.Nation,C2OriginalProduceCatalogV13.ResolveMemberIdForSelectedBuilding(b),killed||b.ReadyLikeOriginal&&b.LifeLikeOriginal<=0);
        }
        internal static void Notify(C2SettlementBuildingSelectableV1LikeOriginal b,bool killed=false)
        {
            if(b!=null&&b.OwnerMode!=null)b.OwnerMode.GetComponent<C2EconomicAiDriverV451>()?.Statistics?.Observe(b,killed);
        }
        internal static void Notify(C2NeutralPeasantUnitInfoV2LikeOriginal u,bool killed=false)
        {
            if(u?.OwnerMode!=null)u.OwnerMode.GetComponent<C2EconomicAiDriverV451>()?.Statistics?.Observe(u,killed);
        }
    }
}
