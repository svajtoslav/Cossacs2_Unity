using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // A command boundary, not an AI shortcut. Neither selection nor ControlledNation
    // is changed. UI and simulation share placement, payment and builder orders.
    public sealed partial class C2BuildingPlacementPreviewV27
    {
        private static C2BuildingPlacementPreviewV27 commandCheckerV450;
        private C2BattleTerrainMode commandMapV450;
        private readonly Dictionary<string,C2BuildMdInfoV27> commandMdsV450 = new Dictionary<string,C2BuildMdInfoV27>(StringComparer.OrdinalIgnoreCase);

        private static C2BuildingPlacementPreviewV27 CommandCheckerV450(C2BattleTerrainMode map)
        {
            if(commandCheckerV450!=null && commandCheckerV450.commandMapV450==map)return commandCheckerV450;
            if(commandCheckerV450!=null)Destroy(commandCheckerV450.gameObject);
            // Remains inactive: Awake/Update of the interactive preview must never
            // run, install a new singleton, create a ghost or consume mouse input.
            var host=new GameObject("C2_ConstructionRules_V450");host.SetActive(false);
            host.transform.SetParent(map.transform,false);
            commandCheckerV450=host.AddComponent<C2BuildingPlacementPreviewV27>();
            commandCheckerV450.commandMapV450=map;
            return commandCheckerV450;
        }

        internal static bool CheckSiteV450(C2BattleTerrainMode map,string md,int x,int y,out string reason)
        {
            reason="Нет карты или описания здания";if(map==null||string.IsNullOrWhiteSpace(md))return false;
            var checker=CommandCheckerV450(map);
            if(!checker.commandMdsV450.TryGetValue(md,out var definition))
                checker.commandMdsV450[md]=definition=C2BuildMdInfoV27.Parse(md);
            if(definition==null||!definition.Found){reason="Не найдено описание здания";return false;}
            checker._buildMd=definition;
            checker.RefreshRuntimeBlockerCacheLikeOriginal(true);
            var check=checker.CheckPlacementAtRealLikeOriginal(map,x,y,false);
            reason=check.Reason;return check.Valid;
        }

        internal static bool TryBuildForNationV450(C2BattleTerrainMode map,int nation,string id,int x,int y,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> builders,out GameObject site,out string reason)
        {
            site=null;reason="Нет своих строителей";
            if(map==null||(uint)nation>=7||builders==null)return false;
            var own=new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            C2OriginalProduceItemV13 item=null;
            foreach(var u in builders)
            {
                if(u==null||u.OwnerMode!=map||u.Nation!=nation||!u.isActiveAndEnabled||u.IsDeadLikeOriginal||!u.CanBuildOrRepairLikeOriginal())continue;
                foreach(var option in C2OriginalProduceCatalogV13.BuildForSelectedUnit(u,out _))
                    if(option.Building && string.Equals(option.UnitId,id,StringComparison.OrdinalIgnoreCase))
                    {item=option;if(!own.Contains(u))own.Add(u);break;}
            }
            if(item==null||own.Count==0)return false;
            C2NationCityRuntimeV384ALikeOriginal.EnsureFreshPopulationV449();
            if(!C2ProductionLimitsV450.Allows(nation,id,out reason))return false;
            if(!C2UpgradeEconomyV445.CanProduce(map,nation,id)){reason="Требуется здание или улучшение";return false;}
            if(!CheckSiteV450(map,item.MdName,x,y,out reason))return false;
            if(!C2NationResourceEconomyV348LikeOriginal.TryGetUnitPriceLikeOriginal(item.MdName,out var rawPrice,out reason))return false;
            var price=C2UpgradeEconomyV445.UnitPrice(nation,id,rawPrice);
            if(!C2NationResourceEconomyV348LikeOriginal.ApplyCostLikeOriginal(nation,price,"CmdCreateBuilding:"+id,out reason))return false;
            bool created=false;
            try {created=map.C2BuildRuntimeCreateConstructionLikeOriginal(item.MdName,id,nation,x,y,"CmdCreateBuilding V450",out site,out reason);}
            finally {if(!created)C2NationResourceEconomyV348LikeOriginal.RestoreCostAfterRejectedCreationLikeOriginal(nation,price,"CmdCreateBuilding rejected:"+id);}
            if(!created)return false;
            C2NationCityRuntimeV384ALikeOriginal.RefreshNowLikeOriginal("construction_placed");
            var checker=CommandCheckerV450(map);var md=checker._buildMd;
            int cellX=(x+(md.PicDx<<4))>>8,cellY=(y+(md.PicDy<<5))>>8;
            IList<Vector2Int> cells=md.BuildLockPoints!=null&&md.BuildLockPoints.Count>0?md.BuildLockPoints:md.LockPoints;
            cells=ConvertFootprintCellsToMapLikeOriginal(map,x,y,cellX,cellY,cells);
            map.C2BuildRuntimeErasePlacedFoundationAreaLikeOriginal(cellX,cellY,cells,0,"CmdCreateBuilding V450",out _);
            int assigned=map.C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(site,x,y,own,"CmdCreateBuilding V450",out var assignment);
            reason="Строителей назначено: "+assigned+"; "+assignment;
            return true;
        }
    }
}
