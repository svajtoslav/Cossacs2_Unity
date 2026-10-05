using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // NewMonster::ResConcentrator / MapSprites.cpp::TakeResLink.
    internal static class C2ResourceStorageV445
    {
        private static readonly Dictionary<string,int> Masks = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        internal static C2BuildingRuntimeInfoV247LikeOriginal Info(C2SettlementBuildingSelectableV1LikeOriginal b) =>
            b == null ? null : b.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>() ?? b.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();

        internal static int ResourceMask(C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            if(b==null)return 0;
            var info=Info(b);
            string path=info!=null?info.MdPath:null;
            if(string.IsNullOrEmpty(path))path=C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(b).Path;
            if(string.IsNullOrEmpty(path))return 0;
            if(Masks.TryGetValue(path,out int mask))return mask;
            if(File.Exists(path))foreach(string raw in File.ReadLines(path))
            {
                string line=raw.Split(new[]{"//"},StringSplitOptions.None)[0];
                var t=line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(t.Length<2||t[0]!="RESOURCEBASE")continue;
                for(int i=2;i<t.Length;i++){int id=ResourceId(t[i]);if(id>=0)mask|=1<<id;}
            }
            return Masks[path]=mask;
        }
        internal static int ResourceId(string name)
        {
            switch((name ?? "").ToUpperInvariant()){case "WOOD":return 0;case "GOLD":return 1;case "STONE":return 2;case "FOOD":return 3;case "IRON":return 4;case "COAL":return 5;default:return -1;}
        }
        internal static bool Ready(C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            if(b==null||!b.isActiveAndEnabled||!b.ReadyLikeOriginal||b.LifeLikeOriginal<=0)return false;
            var site=b.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            return site==null || (site.Ready&&!site.Dead);
        }
        internal static bool Accepts(C2SettlementBuildingSelectableV1LikeOriginal b,C2BattleTerrainMode map,int nation,int resource) =>
            Ready(b)&&b.OwnerMode==map&&b.Nation==nation&&resource>=0&&resource<6&&(ResourceMask(b)&(1<<resource))!=0;

        internal static bool Paths(C2SettlementBuildingSelectableV1LikeOriginal b,out Vector2[] entry,out Vector2[] exit)
        {
            entry=exit=null;var info=Info(b);if(info==null)return false;
            // TakeResLink uses BORNPOINTS in reverse to unload, then in forward
            // order to leave. CONCENTRATOR is the separate enter-mine order.
            if(info.BornExitPathReal.Count>0){exit=info.BornExitPathReal.ToArray();entry=(Vector2[])exit.Clone();Array.Reverse(entry);return true;}
            if(info.ConcentratorPathReal.Count>0){entry=info.ConcentratorPathReal.ToArray();exit=(Vector2[])entry.Clone();Array.Reverse(exit);return true;}
            return false;
        }
        internal static C2SettlementBuildingSelectableV1LikeOriginal Nearest(C2BattleTerrainMode map,int nation,int resource,float x,float y, IDictionary<C2SettlementBuildingSelectableV1LikeOriginal,float> unavailable = null)
        {
            C2SettlementBuildingSelectableV1LikeOriginal best=null;float distance=float.MaxValue;
            foreach(var b in UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsSortMode.None))
            {
                if(!Accepts(b,map,nation,resource)||!Paths(b,out var entry,out _))continue;
                if(unavailable!=null&&unavailable.TryGetValue(b,out float until)&&Time.time<until)continue;
                float d=(entry[0]-new Vector2(x,y)).sqrMagnitude;
                if(d<distance){distance=d;best=b;}
            }
            return best;
        }
    }
}
