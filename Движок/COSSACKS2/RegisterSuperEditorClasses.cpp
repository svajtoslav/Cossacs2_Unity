//==================================================================================================================//
#include "stdheader.h"
#include "SuperEditor.h"
#include "WeaponSystem.h"
#include "unitability.h"
#include "unitability2.h"
#include "UnitTypeGroup.h"
#include "cvi_singleplayerdata.h"
#include "BrigadeAINeuro.h"
//==================================================================================================================//
void RegisterSuperEditorClasses(){
	REG_CLASS(Operand);
	REG_CLASS_EX(NumericalConst,"Ariphmetical");
	REG_CLASS_EX(Op_Add,"Ariphmetical");
	REG_CLASS_EX(UnarMinus,"Ariphmetical");
	REG_CLASS_EX(Brackets,"Ariphmetical");
	REG_CLASS_AMBIGUOUS_EX(NumericalVariableReference,NumericalAcceptor,"Ariphmetical");
	REG_CLASS_AMBIGUOUS_EX(NumericalArrayVariableReference,NumericalArrayAcceptor,"Ariphmetical");
	REG_CLASS_AMBIGUOUS_EX(NumericalArrayElement,NumericalAcceptor,"Ariphmetical");
	REG_CLASS_EX(Assignment,"Ariphmetical");
	REG_CLASS_EX(Logical,"Ariphmetical");
	REG_CLASS_EX(CompareFun,"Ariphmetical");
	REG_CLASS_EX(If_Else,"Ariphmetical");
	REG_CLASS_EX(ForEachNumerical,"Ariphmetical");
	REG_CLASS_EX(ForFromTo,"Ariphmetical");
	REG_CLASS(BaseType);
	REG_CLASS_EX(NumericalType,"Ariphmetical");
	REG_CLASS_EX(BoolType,"Ariphmetical");
	REG_CLASS_EX(NumericalArray,"Ariphmetical");
	REG_CLASS_EX(qUnitType,"MissionsScript");
	REG_CLASS_EX(qBrigadeFormationType,"MissionsScript");
	//REG_CLASS_EX(BoolArray,"Ariphmetical");
	REG_CLASS(Variable);
	REG_CLASS(Returner); 
	REG_CLASS(Acceptor);
	REG_CLASS(NumericalReturner);
	REG_CLASS(NumericalAcceptor);
	REG_CLASS(NumericalAcceptor);
	REG_CLASS(BoolReturner);
	REG_CLASS(BoolAcceptor);
	REG_CLASS(StringReturner);
	REG_CLASS(StringAcceptor);

	// types
	REG_CLASS_EX(StringType,"Ariphmetical");
	REG_CLASS_EX(StringConst,"Ariphmetical");	
	REG_CLASS_AMBIGUOUS_EX(StringVariableReference,StringAcceptor,"Ariphmetical");
	// functions
	REG_CLASS_EX(qfCondRandom,"Ariphmetical");
	// procedures
	REG_CLASS_EX(qpChatHint,"Ariphmetical");
	//
	REG_CLASS_EX(CGetGameTime,"MissionsScript"); 
	REG_CLASS_AMBIGUOUS_EX(BVariables,NumericalAcceptor,"MissionsScript");
	REG_CLASS_EX(GetPlayerNation,"MissionsScript"); 
	REG_CLASS_EX(GetPlayerSelectedBrigadeID,"MissionsScript"); 
	REG_CLASS_EX(SetCurrentNationColor,"MissionsScript"); 
	REG_CLASS_EX(SetCurrentBrigadeID,"MissionsScript"); 

	REG_CLASS_EX(PlayVideo,"Video"); 
	REG_CLASS_EX(CPlaySound,"Video"); 
	REG_CLASS_EX(OneStringExpressions,"MissionsScript");
	REG_CLASS_EX(GetHeroVariable,"MissionsScript");
	//=================     WeaponSystem      ======================//
	REG_CLASS(TargetDesignation);
	REG_CLASS(WeaponParams);
	REG_CLASS(AdditionalWeaponParams);
	
	REG_CLASS(PointModificator);
	REG_CLASS(WeaponEvent);
	REG_CLASS(WeaponProcess);
	REG_CLASS(WeaponModificator);
	REG_CLASS(WeaponSystem);

	REG_CLASS(DrawOne);
	REG_CLASS(SelfMurder);
	REG_CLASS(StaticMotion);
	REG_CLASS(Jump);
	REG_CLASS(Motion);
	REG_CLASS(HarmonicMotion);
	REG_CLASS(BalisticMotion);
	REG_CLASS(FollowUnit);
	REG_CLASS(BirthNew);
	REG_CLASS(ChangeModificator);
	REG_CLASS(MakeDamage);
	REG_CLASS(Wave);
	
	REG_CLASS(TargetFinder);
	REG_CLASS(UnitsInRadius);
	REG_CLASS(RandomPosInRadius);
	REG_CLASS(UserDefinedPoints);
	REG_CLASS(UserDefinedPointCoor);
	REG_CLASS(MassBirthNew);
	REG_CLASS(BirthNewUnit);
	REG_CLASS(BirthNewUnitsFromSprites);
	REG_CLASS(ChangeNation);
	REG_CLASS(PlaySomeSound);
	
	REG_CLASS(True);
	REG_CLASS(IsTargetDie);
	REG_CLASS(IsTargetInvisible);	
	REG_CLASS(TargetReached);
	REG_CLASS(IsFirstStep);
	REG_CLASS(AND_several_events);
	REG_CLASS(OR_several_events);
	REG_CLASS(Conditions);
	//==============================================================//
	//=================                        =====================//
	REG_CLASS(NewFrame);
	//==============================================================//
	//=================     UnitAbility        =====================//
	//==============================================================//
	REG_CLASS(UnitAbility);
	REG_CLASS(UnitAbility2);
	REG_CLASS(BaseClassUnitType);
	REG_CLASS(UnitTypeList);
	REG_CLASS(UnitAbilityAura);
	REG_CLASS(UnitAbilityMagicEffect);
	REG_CLASS(VampiricAbility);
	REG_CLASS(ChangeMDAbiliti);
	REG_CLASS(AbilityList);
	REG_CLASS(MonsterAbility);
	REG_CLASS(ActiveUnitAbility);
	REG_CLASS(UnitActiveAbilityList);
	REG_CLASS(CoolDown);
	REG_CLASS(ChangeMDAbilitiActive);
	REG_CLASS(UnitsComposition);
	REG_CLASS(OneUnitInComposition);
	REG_CLASS(LeadSeveralUnitsAbility);
	REG_CLASS(BeSlaveOfUnit);
	REG_CLASS(CannonAutoShot);
	REG_CLASS(CannonAutoShotActive);
	REG_CLASS(PushUnitsAbility);
	REG_CLASS(PushAllOnTheWay);
	REG_CLASS(BlizardAbility);
	REG_CLASS(ActiveBlizardAbility);
	REG_CLASS(LeveledActiveUnitAbility);
	REG_CLASS(LifeLimitation);
	REG_CLASS(ActiveLifeLimitation);
	REG_CLASS(GreetingMe);
	REG_CLASS(ActiveGreetingMe);
	REG_CLASS(ActiveGreeting);
	REG_CLASS(NationBonus);
	REG_CLASS(ActiveNationBonus);
	REG_CLASS(BrigadeChangeMoral);
	REG_CLASS(ActiveBrigadeChangeMoral);
	REG_CLASS(ActiveChangedMoralForBrigade);
	REG_CLASS(BrigadeTimeChangeMoral);
	REG_CLASS(ActiveBrigadeTimeChangeMoral);
	REG_CLASS(ActiveTimeChangedMoralForBrigade);
	REG_CLASS(BrigadeIconInfo);
	REG_CLASS(HeroLight);
	REG_CLASS(ActiveHeroLight);
	REG_CLASS(LifeRegeneration);
	REG_CLASS(ActiveLifeRegeneration);
	REG_CLASS(AddBrigadeBonus);
	REG_CLASS(ActiveAddBrigadeBonus);
	REG_CLASS(ActiveBrigadeBonus);
	REG_CLASS(SetMineBonus);
	REG_CLASS(ActiveSetMineBonus);
	REG_CLASS(ActiveMineBonus);
	REG_CLASS(IncreaseMaxLife);
	REG_CLASS(ActiveAdditionalLife);
	REG_CLASS(ActiveIncreaseMaxLife);
	REG_CLASS(BuildingShield);
	REG_CLASS(ActiveAddBuildingShield);
	REG_CLASS(ActiveBuildingShield);
	REG_CLASS(BeSlowNearUnits);
	REG_CLASS(aa_BeSlowNearUnits);
	REG_CLASS(FollowBrigade);
	REG_CLASS(ActiveFollowBrigade);
	REG_CLASS(Behaviour);
	REG_CLASS(ActiveBehaviour);
	REG_CLASS(RomeHero);
	REG_CLASS(ActiveRomeHero);
	REG_CLASS(LeveledActiveUnitAbility);
	REG_CLASS(BeSlowNearUnits);
	REG_CLASS(MakeDamageOnDeath);
	REG_CLASS(aa_MakeDamageOnDeath);

	REG_CLASS(BigUnit);
	REG_CLASS(ActiveBigUnit);
	REG_CLASS(AddUnitBonus);
	REG_CLASS(ActiveAddUnitBonus);
	REG_CLASS(PassiveAddUnitBonus);
	REG_CLASS(AggressiveMove);
	REG_CLASS(ActiveAggressiveMove);
	REG_CLASS(AggressiveSplashFire);
	REG_CLASS(ActiveAggressiveSplashFire);
	//==============================================================//
	//=================     HeroAbility        =====================//
	//==============================================================//
	REG_CLASS(Probability);
	REG_CLASS(UpHeroParam);
	REG_CLASS(ChooseUpHeroParam);
	REG_CLASS(UpHeroParamList);
	REG_CLASS(HeroAbility);
	REG_CLASS(UpLife);
	REG_CLASS(LetPass);
	REG_CLASS(HeroVariableStorage);
	REG_CLASS(UpAttack);
	REG_CLASS(UpVision);
	REG_CLASS(UpRange);
	REG_CLASS(UpAttackSpeed);
	REG_CLASS(UpMotionSpeed);
	REG_CLASS(UpShield);
	REG_CLASS(UpLifeRegeneration);
	REG_CLASS(UpLevelParam);
	REG_CLASS(UpSearchEnemyRadius);
	REG_CLASS(UpVariable);
	REG_CLASS(HeroVariable);
//	REG_CLASS(CardPlace);
//	REG_CLASS(ActiveCardPlace);
	//==============================================================//
	//=================     HeroAbility        =====================//
	//==============================================================//
	REG_CLASS(CBrigadeOrder);
	REG_CLASS(BrigadeOrder_RifleAttack);
	REG_CLASS(BrigadeOrder_GoOnRoad);
	REG_CLASS(BrigadeOrder_Bitva);
	REG_CLASS(BrigadeOrder_HumanEscape);
	REG_CLASS(BrigadeOrder_KeepPositions);
	REG_CLASS(BrigadeOrder_HumanGlobalSendTo);
	//==============================================================//
	//=================         Other          =====================//
	//==============================================================//
	REG_CLASS(TypeGroup);
	REG_CLASS(TypeGroupList);
	REG_CLASS(UnitTypeGroup);
	REG_CLASS(SinglePlayerData_HeroAbilityInfo);
	REG_CLASS(SinglePlayerData_HeroInfo);
	REG_CLASS(SinglePlayerData_HeroesInfoList);
	REG_CLASS(SinglePlayerData_CampaignHeroInfo);
	//
	REG_CLASS(cvi_SinglePlayerData);
	//==============================================================//
	//=================         Neuro          =====================//
	//==============================================================//
	REG_CLASS(Neuro);
	REG_CLASS(Neuro2AutoForm);
	REG_CLASS(CNeuroStorage);
	//==============================================================//
	//=================       MagicCard        =====================//
	//==============================================================//
//	REG_CLASS(CardRef);
//	REG_CLASS(CardList);
//	REG_CLASS(MCThickskin);
//	REG_CLASS(MCThickskinActive);
	REG_CLASS(MagicCard);
	REG_CLASS(MagicSpell);
	REG_CLASS(LifeCast);
	REG_CLASS(AttSpeedCast);
	REG_CLASS(MoveSpeedCast);
	REG_CLASS(ShieldCast);
	/*REG_CLASS(MCAntimagic);
	REG_CLASS(MCAntimagicActive);
	REG_CLASS(MCFreezing);
	REG_CLASS(MCFreezingActive);
	REG_CLASS(MCBerserk);
	REG_CLASS(MCBerserkActive);
	REG_CLASS(Earthquake);
	REG_CLASS(EarthquakeActive);
	REG_CLASS(Polymorph);
	REG_CLASS(PolymorphActive);
	REG_CLASS(Invisibility);
	REG_CLASS(InvisibilityActive);
	REG_CLASS(Eyelightnings);
	REG_CLASS(EyelightningsActive);
	REG_CLASS(BoneGolem);
	REG_CLASS(BoneGolemActive);
	REG_CLASS(Absorption);
	REG_CLASS(AbsorptionActive);
	REG_CLASS(Theft);
	REG_CLASS(TheftActive);
	REG_CLASS(DeceiveDeath);
	REG_CLASS(DeceiveDeathActive);
	REG_CLASS(StatueInsult);
	REG_CLASS(StatueInsultActive);
	REG_CLASS(WhiteMagic);
	REG_CLASS(WhiteMagicActive);
	REG_CLASS(Callillusions);
	REG_CLASS(CallillusionsActive);
	REG_CLASS(Withdrawal);
	REG_CLASS(WithdrawalActive);
	REG_CLASS(Fieryaura);
	REG_CLASS(FieryauraActive);
	REG_CLASS(Clairvoyance);
	REG_CLASS(ClairvoyanceActive);
	REG_CLASS(Lastsigh);
	REG_CLASS(LastsighActive);
	REG_CLASS(Petrifaction);
	REG_CLASS(PetrifactionActive);
	REG_CLASS(Spheremagma);
	REG_CLASS(SpheremagmaActive);
	REG_CLASS(Shieldarrows);
	REG_CLASS(ShieldarrowsActive);
	REG_CLASS(Shieldaxe);
	REG_CLASS(ShieldaxeActive);
	REG_CLASS(Shieldmace);
	REG_CLASS(ShieldmaceActive);
	REG_CLASS(Shieldmagic);
	REG_CLASS(ShieldmagicActive);
	REG_CLASS(Trap);
	REG_CLASS(TrapActive);*/

	//==============================================================//

}
//==================================================================================================================//
