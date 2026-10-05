using System;
using System.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        internal static bool VillageMemberMatchesV453(C2NeutralPeasantUnitInfoV2LikeOriginal unit,string id)=>MatchesFormationMemberV172LikeOriginal(unit,id);
        // DIP_SimpleBuilding::GetFreePlacesInBrig / ActiveScenary::FillFormationFromGroup.
        internal static RuntimeFormationV172LikeOriginal VillageTargetV453(C2BattleTerrainMode map,int owner,Vector2 center,float radius)
        {
            if(owner<0||owner>=7)return null;
            foreach(var group in _groupsByIdV172LikeOriginal.Values.OrderBy(g=>g.GroupId))
            {
                if(group.Nation!=owner||string.IsNullOrEmpty(group.SoldierMemberId))continue;
                var icon=C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(group.SoldierMemberId);
                if(!icon.HasPortBranch||icon.PortBranch!=0)continue; // Native villages reinforce infantry only.
                var alive=GetAliveGroupUnitsV320LikeOriginal(group);
                if(alive.Count==0||alive[0].OwnerMode!=map||alive.Any(u=>u.SettlementAiControlledLikeOriginal))continue;
                ComputeActualUnitCenterV320LikeOriginal(alive,out float x,out float y);
                if((new Vector2(x,y)/16f-center).sqrMagnitude>(radius+100)*(radius+100))continue;
                if(!alive.Any(u=>(new Vector2(u.RealX,u.RealY)/16f-center).sqrMagnitude<=radius*radius))continue;
                if(VacanciesV453(group)>0)return group;
            }
            return null;
        }
        internal static int VacanciesV453(RuntimeFormationV172LikeOriginal group)
        {
            var record=ResolveRecordForGroupV320LikeOriginal(group);
            var option=record==null?null:FindFormationOptionV320LikeOriginal(record,group.Shape);
            if(option==null)return 0;
            int alive=0;
            for(int i=group.CommandSlotCount;i<group.Units.Count;i++)
                if(group.Units[i]!=null&&!group.Units[i].IsDeadLikeOriginal)alive++;
            return Mathf.Max(0,option.UnitCount-alive);
        }
        internal static bool FillFromVillageV453(RuntimeFormationV172LikeOriginal group,C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if(unit==null||unit.IsDeadLikeOriginal||unit.IsBusyWithBornExitOrMoveLikeOriginal()||
               IsUnitInRuntimeFormationV168LikeOriginal(unit)||!MatchesFormationMemberV172LikeOriginal(unit,group.SoldierMemberId)||VacanciesV453(group)==0)return false;
            int slot=group.CommandSlotCount;
            while(slot<group.Units.Count&&group.Units[slot]!=null&&!group.Units[slot].IsDeadLikeOriginal)slot++;
            if(slot>=group.Slots.Count)
            {
                var option=FindFormationOptionV320LikeOriginal(ResolveRecordForGroupV320LikeOriginal(group),group.Shape);
                var alive=GetAliveGroupUnitsV320LikeOriginal(group);
                ComputeActualUnitCenterV320LikeOriginal(alive,out float x,out float y);
                var slots=BuildTemplateSlotsDirectedV352LikeOriginal(option,option.UnitCount+group.CommandSlotCount,group.CommandSlotCount,group.Units,x,y,group.SpacingPercent,group.Direction);
                while(group.Slots.Count<slots.Count)group.Slots.Add(slots[group.Slots.Count]);
            }
            if(slot>=group.Slots.Count)return false;
            unit.SetSelected(false);
            unit.C2RenewIdentityAfterCaptureV441();
            unit.SettlementAiControlledLikeOriginal=false;unit.SettlementAllegianceNationLikeOriginal=-1;
            unit.Nation=(byte)group.Nation;unit.ControllableByPlayer=C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(group.Nation);
            var rt=unit.RuntimeLinkCachedLikeOriginal?.Runtime;if(rt?.Probe!=null)rt.Probe.Nation=group.Nation;
            while(group.Units.Count<=slot)group.Units.Add(null);
            group.Units[slot]=unit;_groupIdByUnitInstanceV172LikeOriginal[unit.GetInstanceID()]=group.GroupId;
            var brigadeState=GetStandGroundStateV403LikeOriginal(group,false);
            if(brigadeState!=null)
            {
                var state=GetUnitStandGroundStateV403LikeOriginal(unit,true);
                state.AddDamage=brigadeState.AddDamage;state.AddShield=brigadeState.AddShield;
            }
            C2CombatRuntimeV334LikeOriginal.ResetRifleAttackOnFormationCreateV396LikeOriginal(unit);
            var target=group.Slots[slot];
            C2OriginalOrderChainV352.SubmitMove(unit,target.x,target.y,true,group.Direction,0,"FillFormationFromGroup",true,144);
            C2NationCityRuntimeV384ALikeOriginal.RefreshNowLikeOriginal("village_reinforcement");
            return true;
        }
    }

    public sealed partial class C2SettlementDipVillageV336LikeOriginal
    {
        private float _nextReinforcementV453;
        private C2FormationRuntimeV167LikeOriginal.RuntimeFormationV172LikeOriginal _reinforcementTargetV453;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _convertingCopV453;
        private C2SettlementBuildingSelectableV1LikeOriginal _conversionBuildingV453;
        private int _conversionPhaseV453,_conversionOwnerV453;
        private float _conversionDueV453;
        internal int ReinforcedV453 {get;private set;}

        private C2OriginalProduceItemV13 ReinforcementItemV453()
        {
            if(_reinforcementTargetV453==null||_reinforcementTargetV453.Nation!=_owner||
               C2FormationRuntimeV167LikeOriginal.VacanciesV453(_reinforcementTargetV453)<=0)return _defenderItem;
            return C2OriginalProduceCatalogV13.TryBuildEditorItemForUnitIdV333LikeOriginal(_reinforcementTargetV453.SoldierMemberId,out var item)?item:_defenderItem;
        }
        private void TickReinforcementV453()
        {
            if(_mode==null||_data==null||!_populationInitialized||Time.time<_nextReinforcementV453)return;
            _nextReinforcementV453=Time.time+.25f;
            _reinforcementTargetV453=_attackedV445?null:C2FormationRuntimeV167LikeOriginal.VillageTargetV453(_mode,_owner,CentreV451,BigRadius);
            var desired=ReinforcementItemV453();
            if(_convertingCopV453!=null)
            {
                var cop=_convertingCopV453;
                if(cop.IsDeadLikeOriginal){_convertingCopV453=null;return;}
                if(_conversionOwnerV453!=_owner||_attackedV445||!C2ResourceStorageV445.Ready(_conversionBuildingV453)||desired==null)
                {CancelCopConversionV453();return;}
                if(!C2ResourceStorageV445.Paths(_conversionBuildingV453,out var entry,out var exit))
                {CancelCopConversionV453();return;}
                if(_conversionPhaseV453==0)
                {
                    if(!NearV445(cop,entry[0],50)){MoveV445(cop,entry[0]);return;}
                    C2OriginalOrderChainV352.SubmitBornExitV433LikeOriginal(cop,entry,entry.Length,true,false);_conversionPhaseV453=1;
                }
                else if(_conversionPhaseV453==1)
                {
                    if(cop.IsBusyWithBornExitOrMoveLikeOriginal()||!NearV445(cop,entry[entry.Length-1],40))return;
                    cop.RuntimeLinkCachedLikeOriginal?.SetHiddenInsideBuildingLikeOriginal(true);
                    _conversionDueV453=Time.time+10f/25f;_conversionPhaseV453=2;
                }
                else if(Time.time>=_conversionDueV453)
                {
                    if(!C2UnitOriginalRuntimeAndRendererV1.TrySpawnProducedUnitFromBuildingLikeOriginal(_mode,_conversionBuildingV453,desired,7,out var replacement,out _))return;
                    replacement.SettlementAllegianceNationLikeOriginal=_owner;
                    replacement.SettlementAiControlledLikeOriginal=true;replacement.ControllableByPlayer=false;
                    _defenders.Add(replacement);_defenders.Remove(cop);
                    var link=cop.RuntimeLinkCachedLikeOriginal;
                    link.Owner.RetireConvertedMilitiaV453(link.Runtime);
                    _convertingCopV453=null;
                }
                return;
            }
            if(_attackedV445||_owner==7||desired==null)return;
            // Existing matching defenders leave the pool; casualties and veteran
            // experience stay on the same brigade object, without a reformation.
            if(_reinforcementTargetV453!=null)
                for(int i=_defenders.Count-1;i>=0;i--)
                    if(C2FormationRuntimeV167LikeOriginal.FillFromVillageV453(_reinforcementTargetV453,_defenders[i]))
                    {_defenders.RemoveAt(i);ReinforcedV453++;}
            if(_reinforcementTargetV453!=null&&C2FormationRuntimeV167LikeOriginal.VacanciesV453(_reinforcementTargetV453)==0)return;
            foreach(var cop in _defenders)
            {
                if(cop==null||cop.IsDeadLikeOriginal||cop.IsBusyWithBornExitOrMoveLikeOriginal()||
                   C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(cop)||
                   C2FormationRuntimeV167LikeOriginal.VillageMemberMatchesV453(cop,desired.UnitId))continue;
                foreach(var building in _buildings)
                    if(C2ResourceStorageV445.Ready(building)&&C2ResourceStorageV445.Paths(building,out _,out _))
                    {
                        _convertingCopV453=cop;_conversionBuildingV453=building;_conversionPhaseV453=0;_conversionOwnerV453=_owner;return;
                    }
            }
        }
        private void CancelCopConversionV453()
        {
            var cop=_convertingCopV453;_convertingCopV453=null;if(cop==null||cop.IsDeadLikeOriginal)return;
            cop.RuntimeLinkCachedLikeOriginal?.SetHiddenInsideBuildingLikeOriginal(false);
            C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(cop);
            if(_conversionPhaseV453>0&&C2ResourceStorageV445.Paths(_conversionBuildingV453,out _,out var exit))
                C2OriginalOrderChainV352.SubmitBornExitV433LikeOriginal(cop,exit,exit.Length,true,true);
        }
    }
    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal void RetireConvertedMilitiaV453(C2UnitOriginalRuntime unit)
        {
            // Native DeleteUnit after successful ProduceUnitTicks, without a
            // death animation, kill credit, loot, or a second live defender.
            if(unit?.Info==null||!unit.Info.SettlementAiControlledLikeOriginal)return;
            ExpireOriginalDeathV435LikeOriginal(unit);
        }
    }
}
