#ifndef __HEROABILITY_H__
#define __HEROABILITY_H__

#include "WeaponSystem.h"
//==================================================================================================================//
class Probability : public BaseClass
{
public:
	int Level;
	int	Weight;
	SAVE(Probability)
		REG_MEMBER(_int,Level);
		REG_MEMBER(_int,Weight);
	ENDSAVE;
};
class UpHeroParam : public BaseClass
{
public:
	UpHeroParam();
	int Index;
	short FileID;
	int SpriteID;
	_str Hint;
	bool IsInPass;
	bool Special;
	ClonesArray<Probability> ProbabilityList;
	SAVE(UpHeroParam)
		REG_MEMBER(_int,Index);
		REG_MEMBER(_gpfile,FileID);
		REG_MEMBER(_int,SpriteID);
		REG_MEMBER(_str,Hint);
		REG_MEMBER(_bool,IsInPass);
		REG_AUTO(ProbabilityList);
		//REG_MEMBER(_Bool,Special);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpLife : public UpHeroParam
{
public:
	UpLife();
	int AddMaxLife;
	SAVE(UpLife)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,AddMaxLife);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpAttack : public UpHeroParam
{
public:
	UpAttack();
	int AttackAdd;
	SAVE(UpAttack)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,AttackAdd);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpVision : public UpHeroParam
{
public:
	UpVision();
	int VisionAdd;
	SAVE(UpVision)
		REG_PARENT(UpHeroParam);
	REG_MEMBER(_int,VisionAdd);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpRange : public UpHeroParam
{
public:
	UpRange();
	int RageAdd;
	int AttackType;
	SAVE(UpRange)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,RageAdd);
		REG_MEMBER(_int,AttackType);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpAttackSpeed : public UpHeroParam
{
public:
	UpAttackSpeed();
	int SpeedAdd;
	SAVE(UpAttackSpeed)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,SpeedAdd);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpMotionSpeed : public UpHeroParam
{
public:
	UpMotionSpeed();
	int MotionSpeedAdd;
	SAVE(UpMotionSpeed)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,MotionSpeedAdd);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpShield : public UpHeroParam
{
public:
	UpShield();
	int ShieldAdd;
	int AttackType;
	SAVE(UpShield)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,ShieldAdd);
		REG_MEMBER(_int,AttackType);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpLifeRegeneration : public UpHeroParam
{
public:
	UpLifeRegeneration();
	int RegenerationAdd;
	SAVE(UpLifeRegeneration)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,RegenerationAdd);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpSearchEnemyRadius : public UpHeroParam
{
public:
	UpSearchEnemyRadius();
	int RadiusAdd;
	SAVE(UpSearchEnemyRadius)
		REG_PARENT(UpHeroParam);
		REG_MEMBER(_int,RadiusAdd);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class UpVariable : public UpHeroParam
{
public:
	UpVariable();
	_str VarName;
	int AddValue;
	SAVE(UpVariable)
		REG_PARENT(UpHeroParam);
		REG_AUTO(VarName);
		REG_MEMBER(_int,AddValue);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class LetPass : public UpHeroParam
{
public:
	LetPass();
	SAVE(LetPass)
		REG_PARENT(UpHeroParam);
	ENDSAVE;
	virtual bool Realize(OneObject* OB);
};
//==================================================================================================================//
class ChooseUpHeroParam : public ActiveUnitAbility
{
public:
	ChooseUpHeroParam();
	//int HeroAbilityIndex;
	ClassRef<UnitAbility> HeroAbilityRef;
	int UpIndex;
	SAVE(ChooseUpHeroParam)
		REG_PARENT(ActiveUnitAbility);
		//REG_MEMBER(_int,HeroAbilityIndex);
		REG_AUTO(HeroAbilityRef);
		REG_MEMBER(_int,UpIndex);
	ENDSAVE;
	virtual bool OnClick();
	virtual bool Process();
};
//==================================================================================================================//
class UpLevelParam : public ChooseUpHeroParam
{
public:
	SAVE(UpLevelParam)
		REG_PARENT(ActiveUnitAbility);
		REG_PARENT(ChooseUpHeroParam);
	ENDSAVE;
	virtual bool OnClick();
	virtual bool Process();
};
//==================================================================================================================//
// от этой штуки наследовать все геройские абилки/карточки
//==================================================================================================================//
class MagicCardActive : public ActiveUnitAbility
{
public:
	MagicCardActive();

	bool fst;
	int timer1;
	int timer2;
	int Damage;
	int Radius; 
	int Tx,Ty;

	SAVE(MagicCardActive){
		REG_PARENT(ActiveUnitAbility);
	}ENDSAVE;

	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRightClick();
	virtual bool isTarget();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z); //
	virtual bool Execute(word TargetUnit, int x, int y, int z);
	virtual bool DrawCursor(int x,int y);
	virtual int GetCoolDownProc();
};
class CardRef;
//class CardList;
class MagicCard : public UnitAbility
{
public:
	MagicCard();

/*	int CursorTexture;
	bool EnemyTarget;
	bool FriendlyTarget;
	UserFriendlyNumericalReturner LongTime;
	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 
	UserFriendlyNumericalReturner Damage; // можно задать формулой типа - (Hero:Level+1)*100 
//	ClonesArray<ClassRef<MagicCard>> Blocking;
//	ClonesArray<ClassRef<MagicCard>> UnCast;*/

	SubSection Magic;
	UserFriendlyNumericalReturner ManaCost; // можно задать формулой типа - (Hero:Level+1)*100 
//	int AttType;
//	ClassRef<WeaponModificator>* EffectName;

	_str EffectName;
	
	int ProcTime;
	
	SAVE(MagicCard){
		REG_PARENT(UnitAbility);
		REG_AUTO(Magic);
/*		REG_ENUM(_index,CursorTexture,TEXTURE_CURSOR_TYPES);
		REG_MEMBER(_bool,EnemyTarget);
		REG_MEMBER(_bool,FriendlyTarget);
		REG_AUTO(LongTime);
		REG_AUTO(Radius);
		REG_AUTO(Damage);*/
		REG_AUTO(ManaCost);
//		REG_AUTO(Blocking);
//		REG_AUTO(UnCast);
//		REG_MEMBER(_int,AttType);
		REG_MEMBER(_str,EffectName);
//		REG_AUTO(EffectName);
	}ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);
	virtual bool CanApply(OneObject* her, HeroVariableStorage* storage);// для интерфейса те. иконка нажемаемая
	virtual int GetCoolDownProc(OneObject* her, HeroVariableStorage* storage);// 0 - 100%
	virtual int GetRadius(OneObject* her, HeroVariableStorage* storage);
	virtual bool OnClick(OneObject* her, HeroVariableStorage* storage);
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
	virtual bool isTarget(OneObject* her, HeroVariableStorage* storage); // если надо указать место применения
	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool isActive(OneObject* her, HeroVariableStorage* storage); // 
	virtual MagicCardActive* GetMCActiveAbility();
};
class CardList:public ClassArray<MagicCard>
{
public:
	//int GetExpansionRules();
};
class CardRef:public BaseClass
{
public:
	ClassRef<MagicCard> cardplace;
	SAVE(CardRef)
		REG_AUTO(cardplace);
	ENDSAVE;
};
//=======================================    Hero Card    ==========================================================//
class MCThickskin : public MagicCard
{
public:
	MCThickskin();

	SAVE(MCThickskin)
		REG_PARENT(MagicCard);
		//REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);
	virtual MagicCardActive* GetMCActiveAbility();
};
//==================================================================================================================//
class MCThickskinActive : public MagicCardActive
{
public:
	MCThickskinActive();

	SAVE(MCThickskinActive)
		REG_PARENT(MagicCardActive);
	ENDSAVE;

	virtual bool Process();
/*	virtual bool OnClick();
	virtual bool OnRightClick();
	virtual bool isTarget();
	//virtual bool DrawCursor(int x,int y);
	virtual bool CanTarget(word TargetUnit, int x, int y, int z); //
	virtual bool Execute(word TargetUnit, int x, int y, int z);*/
};
//==================================================================================================================//
/*class MCAntimagic : public MagicCard
{
public:
	MCAntimagic();

	SAVE(MCAntimagic)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();
};
//==================================================================================================================//
class MCAntimagicActive : public MagicCardActive
{
public:
	MCAntimagicActive();

	SAVE(MCAntimagicActive)
		REG_PARENT(MagicCardActive);
	ENDSAVE;

	virtual bool Process();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z); //
	virtual bool Execute(word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCFreezing : public MagicCard
{
public:
	MCFreezing();

	SAVE(MCFreezing)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();
};
//==================================================================================================================//
class MCFreezingActive : public MagicCardActive
{
public:
	MCFreezingActive();

	SAVE(MCFreezingActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z); //
	virtual bool Execute(word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCBerserk : public MagicCard
{
public:
	MCBerserk();

	SAVE(MCBerserk)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();
};
//==================================================================================================================//
class MCBerserkActive : public MagicCardActive
{
public:
	MCBerserkActive();

	SAVE(MCBerserkActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z); //
	virtual bool Execute(word TargetUnit, int x, int y, int z);
};*/
//==================================================================================================================//
/*class MCEarthquake : public MagicCard
{
public:
	MCEarthquake();

	SAVE(MCEarthquake)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCEarthquakeActive : public MagicCardActive
{
public:
	MCEarthquakeActive();

	SAVE(MCEarthquakeActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCPolymorph : public MagicCard
{
public:
	MCPolymorph();

	SAVE(MCPolymorph)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCPolymorphActive : public MagicCardActive
{
public:
	MCPolymorphActive();

	SAVE(MCPolymorphActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCInvisibility : public MagicCard
{
public:
	MCInvisibility();

	SAVE(MCInvisibility)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCInvisibilityActive : public MagicCardActive
{
public:
	MCInvisibilityActive();

	SAVE(MCInvisibilityActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCEyelightnings : public MagicCard
{
public:
	MCEyelightnings();

	SAVE(MCEyelightnings)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCEyelightningsActive : public MagicCardActive
{
public:
	MCEyelightningsActive();

	SAVE(MCEyelightningsActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCBoneGolem : public MagicCard
{
public:
	MCBoneGolem();

	SAVE(MCBoneGolem)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCBoneGolemActive : public MagicCardActive
{
public:
	MCBoneGolemActive();

	SAVE(MCBoneGolemActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCAbsorption : public MagicCard
{
public:
	MCAbsorption();

	SAVE(MCAbsorption)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCAbsorptionActive : public MagicCardActive
{
public:
	MCAbsorptionActive();

	SAVE(MCAbsorptionActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCTheft : public MagicCard
{
public:
	MCTheft();

	SAVE(MCTheft)
		REG_PARENT(MagicCard);
		REG_PARENT(UnitAbility);
	ENDSAVE;

	virtual MagicCardActive* GetMCActiveAbility();

};
//==================================================================================================================//
class MCTheftActive : public MagicCardActive
{
public:
	MCTheftActive();

	SAVE(MCTheftActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process();
};
//==================================================================================================================//
class MCDeceiveDeath : public MagicCard
{
public:
	MCDeceiveDeath();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCDeceiveDeath)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCDeceiveDeathActive : public MagicCardActive
{
public:
	MCDeceiveDeathActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCDeceiveDeathActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCStatueInsult : public MagicCard
{
public:
	MCStatueInsult();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCStatueInsult)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCStatueInsultActive : public MagicCardActive
{
public:
	MCStatueInsultActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCStatueInsultActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCWhiteMagic : public MagicCard
{
public:
	MCWhiteMagic();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCWhiteMagic)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCWhiteMagicActive : public MagicCardActive
{
public:
	MCWhiteMagicActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCWhiteMagicActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCCallillusions : public MagicCard
{
public:
	MCCallillusions();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCCallillusions)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCCallillusionsActive : public ActiveUnitAbility
{
public:
	MCCallillusionsActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCCallillusionsActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCWithdrawal : public MagicCard
{
public:
	MCWithdrawal();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCWithdrawal)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCWithdrawalActive : public ActiveUnitAbility
{
public:
	MCWithdrawalActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCWithdrawalActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCFieryaura : public MagicCard
{
public:
	MCFieryaura();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCFieryaura)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCFieryauraActive : public ActiveUnitAbility
{
public:
	MCFieryauraActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCFieryauraActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCClairvoyance : public MagicCard
{
public:
	MCClairvoyance();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCClairvoyance)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCClairvoyanceActive : public ActiveUnitAbility
{
public:
	MCClairvoyanceActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCClairvoyanceActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCLastsigh : public MagicCard
{
public:
	MCLastsigh();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCLastsigh)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCLastsighActive : public ActiveUnitAbility
{
public:
	MCLastsighActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCLastsighActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCPetrifaction : public MagicCard
{
public:
	MCPetrifaction();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCPetrifaction)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCPetrifactionActive : public ActiveUnitAbility
{
public:
	MCPetrifactionActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCPetrifactionActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCSpheremagma : public MagicCard
{
public:
	MCSpheremagma();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCSpheremagma)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCSpheremagmaActive : public ActiveUnitAbility
{
public:
	MCSpheremagmaActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCSpheremagmaActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCShieldarrows : public MagicCard
{
public:
	MCShieldarrows();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldarrows)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCShieldarrowsActive : public ActiveUnitAbility
{
public:
	MCShieldarrowsActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldarrowsActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCShieldaxe : public MagicCard
{
public:
	MCShieldaxe();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldaxe)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCShieldaxeActive : public ActiveUnitAbility
{
public:
	MCShieldaxeActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldaxeActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCShieldmace : public MagicCard
{
public:
	MCShieldmace();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldmace)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCShieldmaceActive : public ActiveUnitAbility
{
public:
	MCShieldmaceActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldmaceActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCShieldmagic : public MagicCard
{
public:
	MCShieldmagic();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldmagic)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCShieldmagicActive : public ActiveUnitAbility
{
public:
	MCShieldmagicActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCShieldmagicActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};
//==================================================================================================================//
class MCTrap : public MagicCard
{
public:
	MCTrap();
	UserFriendlyNumericalReturner LongTime;
	//	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCTrap)
		REG_PARENT(MagicCard);
	REG_PARENT(UnitAbility);
	REG_AUTO(LongTime);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* Newbie);

	virtual bool CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z); //
	virtual bool Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class MCTrapActive : public ActiveUnitAbility
{
public:
	MCTrapActive();
	bool fst;
	int timer1;
	int timer2;
	DString oldhint;

	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 

	SAVE(MCTrapActive)
		REG_PARENT(ActiveUnitAbility);
	REG_MEMBER(_int,timer1);
	REG_MEMBER(_int,timer2);
	REG_AUTO(Radius);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
	virtual bool OnRClick();
};*/
//==================================================================================================================//
class UpHeroParamList:public ClassArray<UpHeroParam>
{
public:
	int GetExpansionRules();
};
//==================================================================================================================//
class HeroAbility : public UnitAbility
{
public:
	HeroAbility(void);
	LinearArray<int,_int> LevelUp;
	UpHeroParamList UpHeroParams;
	UpHeroParamList UpParamForLevel;
	int ExperienceRadius;
	int GetExperienceProc;
	int LifeRegeneration;
	int ReBornTime;
	int Mana;
	int ManaRegeneration;
	int SpeedDownIfTired;
	int PlaceCard;
	UserFriendlyNumericalReturner CardRegeneration;
//	CardList baseColoda;
	SAVE(HeroAbility)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,ExperienceRadius);
		REG_MEMBER(_int,GetExperienceProc);
		REG_MEMBER(_int,LifeRegeneration);
		REG_AUTO(LevelUp);
		REG_AUTO(UpHeroParams);
		REG_AUTO(UpParamForLevel);
		REG_MEMBER(_int,ReBornTime);
		REG_MEMBER(_int,Mana);
		REG_MEMBER(_int,ManaRegeneration);
		REG_MEMBER(_int,SpeedDownIfTired);
		REG_MEMBER(_int,PlaceCard);
		REG_AUTO(CardRegeneration);
//		REG_AUTO(baseColoda);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
class HeroVariable : public BaseClass
{
public:
	HeroVariable();
	_str Name;
	int Value;
	SAVE(HeroVariable)
		REG_AUTO(Name);
		REG_MEMBER(_int, Value);
	ENDSAVE;
};

//==================================================================================================================//
class HeroVariableStorage : public ActiveUnitAbility
{
public:
	HeroVariableStorage();
	int HeroAbilityIndex;
	HeroAbility* Hero;
	int Level;
	int Experience;
	int AddExperienceRadius;
	//int GetExperienceProc;
	int Knowledge;
	bool PassSelectHeroParametr;
	bool SelectHeroParamState;
	LinearArray<int,_int> Param;
	int UpLifeRegeneration;
	int LastUpLifeRegenerationTime;
	int UpManaRegeneration;
	int LastUpManaRegenerationTime;
	bool IsTired;
	int SpeedMinus;
	int freePlaceCard;
	ClonesArray<HeroVariable> Variables;
	int* GetVarRef(const char* Name);
	SAVE(HeroVariableStorage)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	int GatherExperience(OneObject* Victim,word Killer);
	virtual bool Process();
	int ExperienceToNextLevel;
	int DieTime;
	int ReBornTime;
	CardList coloda;
	int indexx;
	int colodaPointer;
	int CardRegeneration;
//	int PlaceCard;
//	int FreeCardPlace;

private:
	int GetExperienceToNextLevel();
	void UpLevel();
	void CreateOptionUpHeroParams();
	void AddChooseUpHeroParam(int UpHeroParamIndex);
	void CreateLevelUpParam();
};
//==================================================================================================================//
class CHeroesCollector :public BaseClass
{
public:
	CHeroesCollector();
	DynArray<HeroVariableStorage*> Herosima;
	void OnDieProcess(OneObject* Victim,word Killer);
};
//==================================================================================================================//
class CUnitExperienceParm : public BaseClass
{
public:
	CUnitExperienceParm();
	int LifeK;
	int DamageK;
	int RangeK;
	int SpeedK;
	SAVE(CUnitExperienceParm)
		REG_MEMBER(_int,LifeK);
		REG_MEMBER(_int,DamageK);
		REG_MEMBER(_int,RangeK);
		REG_MEMBER(_int,SpeedK);
	ENDSAVE;
};
//==================================================================================================================//
//==================================================================================================================//
class CardPlace : public UnitAbility
{
public:
	CardPlace();
	SAVE(CardPlace)
		REG_PARENT(UnitAbility);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveCardPlace : public ActiveUnitAbility
{
public:
	ActiveCardPlace();

	HeroVariableStorage* heroStorage;
	int newCardSetTime;
	int cardIndex;
	MagicCard* p_card;
	bool OnClk;
	bool Restart;
	short oldFileID;
	int oldSpriteID;

	SAVE(ActiveCardPlace)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;

	virtual bool Process(OneObject* OB);
	virtual bool CanApply();
	virtual int GetCoolDownProc();
	virtual int GetRadius();
	virtual bool OnClick();
	virtual bool OnRightClick();
	virtual bool Execute(word TargetUnit, int x, int y, int z);
	virtual bool isTarget();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z);
	virtual bool isActive();
	virtual bool DrawCursor(int x,int y);
};

//==================================================================================================================//
#endif