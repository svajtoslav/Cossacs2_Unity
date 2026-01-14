#ifndef __UNITABILITY_H__
#define __UNITABILITY_H__

//#pragma once
class OneObject;
//==================================================================================================================//
#include "SuperEditor.h"
//class UserFriendlyNumericalReturner;
class HeroVariable;
class HeroVariableStorage;
class ActiveUnitAbility;
//==================================================================================================================//
class MagicSpell:public BaseClass
{
public:
	MagicSpell();
//	int operand; //0-Life, 1-AttSpeed, 2-MoveSpeed, 3-Shild
	int sign; //0-"+", 1-"-", 2-"*", 3-"/"
	int num;
	char* name;

	SAVE(MagicSpell)
//		REG_MEMBER(_int,operand);
		REG_ENUM(_index,sign,ARIPHMETICALOPS);
		REG_MEMBER(_int,num);
	ENDSAVE;

	virtual int CastSpell(OneObject* OB);
	virtual int CancelSpell(OneObject* OB);

	int realchng;
};
class LifeCast:public MagicSpell
{
public:
	LifeCast();

	SAVE(LifeCast)
		REG_PARENT(MagicSpell);
	ENDSAVE;

	virtual int CastSpell(OneObject* OB);
	virtual int CancelSpell(OneObject* OB);
};
class AttSpeedCast:public MagicSpell
{
public:
	AttSpeedCast();

	SAVE(AttSpeedCast)
		REG_PARENT(MagicSpell);
	ENDSAVE;

	virtual int CastSpell(OneObject* OB);
	virtual int CancelSpell(OneObject* OB);
};
class MoveSpeedCast:public MagicSpell
{
public:
	MoveSpeedCast();

	SAVE(MoveSpeedCast)
		REG_PARENT(MagicSpell);
	ENDSAVE;

	virtual int CastSpell(OneObject* OB);
	virtual int CancelSpell(OneObject* OB);
};
class ShieldCast:public MagicSpell
{
public:
	ShieldCast();

	SAVE(ShieldCast)
		REG_PARENT(MagicSpell);
	ENDSAVE;

	virtual int CastSpell(OneObject* OB);
	virtual int CancelSpell(OneObject* OB);
};
//==================================================================================================================//
class UnitAbility : public ReferableBaseClass 
{
public:
	UnitAbility(void);
	int Index;
	bool Visible;
	short FileID;
	int SpriteID;
	short EffectFileID;
	int EffectSpriteID;
	//bool Targety; 
	//int MaxCoolDown;
	//bool AutoCast;
	//bool Aura;
	//bool OnMakeDamage;
	char* Hint;
	//
	int CursType;
	float CursRadius;
	DWORD CursColor;
	//
	int CursorTexture;
	bool EnemyTarget;
	bool FriendlyTarget;
	UserFriendlyNumericalReturner LongTime;
	UserFriendlyNumericalReturner Radius; // можно задать формулой типа - (Hero:Level+1)*100 
	UserFriendlyNumericalReturner Damage; // можно задать формулой типа - (Hero:Level+1)*100 
	//
	SubSection MS_Cast;
	ClassArray <MagicSpell> MSCast;
	//
	SubSection PhysicalChar;
	ClonePtr<NewAnimation> eAn;
	ClassRef<WeaponModificator> eWeapon;
	//
	ClonePtr<MoveSpeedCast> eMoveSpeed;
	ClonePtr<MagicSpell> eDamage[4];
	ClonePtr<ShieldCast> eShield;
	ClonesArray<ShieldCast> eProtection;	
	ClonesArray<AttSpeedCast> eAttSpeed;
	ClonePtr<MagicSpell> eLifeRegen;
	//
	ClonesArray< ClassRef<UnitAbility> > Blocking;
	ClonesArray< ClassRef<UnitAbility> > UnCast;

	//modifyMagicImmunity
	//
	SAVE(UnitAbility){
		REG_MEMBER(_str,Name);
		REG_MEMBER(_bool,Visible);
		REG_MEMBER(_gpfile,FileID);
		REG_SPRITE(SpriteID,FileID);
		REG_MEMBER(_gpfile,EffectFileID);
		REG_SPRITE(EffectSpriteID,EffectFileID);
		//REG_MEMBER(_bool,Targety);
		//REG_MEMBER(_str,WeaponModificatorName);
		//REG_MEMBER(_int,MinDist);
		//REG_MEMBER(_int,MaxDist);
		//REG_MEMBER(_int,MaxCoolDown);
		//REG_MEMBER(_bool,AutoCast);
		//REG_MEMBER(_bool,Aura);
		//REG_MEMBER(_bool,OnMakeDamage);
		REG_MEMBER(_textid,Hint);
		//
		REG_ENUM(_index,CursType,TEXTURE_CURSOR_TYPES);
		REG_MEMBER(_float,CursRadius);
		REG_MEMBER(_color,CursColor);
		//
		REG_ENUM(_index,CursorTexture,TEXTURE_CURSOR_TYPES);
		REG_MEMBER(_bool,EnemyTarget);
		REG_MEMBER(_bool,FriendlyTarget);
		REG_AUTO(LongTime);
		REG_AUTO(Radius);
		REG_AUTO(Damage);
		//
		REG_AUTO(MS_Cast);
		REG_AUTO(MSCast);
		REG_AUTO(Blocking);
		REG_AUTO(UnCast);
		//
		REG_AUTO(PhysicalChar);
		REG_AUTO(eAn);
		REG_AUTO(eWeapon);
		REG_AUTO(eMoveSpeed);
		REG_AUTO(eDamage[0]);
		REG_AUTO(eDamage[1]);
		REG_AUTO(eDamage[2]);
		REG_AUTO(eDamage[3]);
		REG_AUTO(eShield);
		REG_AUTO(eProtection);
		REG_AUTO(eAttSpeed);
		REG_AUTO(eLifeRegen);
	}ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
	virtual bool CheckIfObjectIsGlobal(){return true;}
	virtual ActiveUnitAbility* GetActiveAbility();
	virtual const char* GetThisElementView(const char* LocalName);
	// by vital
	virtual void CopyToActive(ActiveUnitAbility* ab);
}; 
//==================================================================================================================//
class UnitAbility2 : public UnitAbility
{
public:
	UnitAbility2();
	short FileID2;
	int SpriteID2;
	SAVE(UnitAbility2)
		REG_PARENT(UnitAbility)
		REG_MEMBER(_gpfile,FileID2);
		REG_SPRITE(SpriteID2,FileID2);
	ENDSAVE;
};
//==================================================================================================================//
//==================================================================================================================//
class UnitTypeList:public ClassArray<BaseClassUnitType>
{
public:
	int GetExpansionRules();
};
class UnitAbilityAura : public UnitAbility
{
public:
	UnitAbilityAura();
	int AddDamage;
	int AddShield;
	int Cure;
	int Radius;
	bool SumAuraEffect;
	UnitTypeList ChoiceUnitType;
	bool EnemyUnitEffect;
	bool FriendlyUnitEffect;
	SAVE(UnitAbilityAura)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,AddDamage);
		REG_MEMBER(_int,AddShield);
		REG_MEMBER(_int,Cure);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_bool,SumAuraEffect);
		REG_AUTO(ChoiceUnitType);
		REG_MEMBER(_bool,EnemyUnitEffect);
		REG_MEMBER(_bool,FriendlyUnitEffect);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
/*	Type: 
5 - CoolDown
6 - ChangeMDAbilitiActive 
7 - HeroVariableStorage
8 - ChooseUpHeroParam
9 - ActiveGreeting
10 - ActiveChangedMoralForBrigade
11 - ActiveTimeChangedMoralForBrigade
12 - ActiveBrigadeBonus
13 - ActiveMineBonus
14 - ActiveAdditionalLife
15 - ActiveBuildingShield
16 - PassiveAddUnitBonus
*/
//==================================================================================================================//
#define ABL_MotionSpeed      	1
#define ABL_Protection       	2
#define ABL_Shield           	4
#define ABL_Damage           	8
#define ABL_AttackRate       	16
#define ABL_MagicImmunity    	32
#define ABL_LifeRegeneration 	64
#define ABL_Weapon			 	128
   
class UnitAbilityIcon;
class ActiveUnitAbility : public BaseClass
{
public:
	ActiveUnitAbility(void);
	~ActiveUnitAbility(void);
	_str Name;
	int UnitIndex;
	//
	DWORD Serial;
	//
	int Type;
	int TypeAbil; // -1 - None, 0 - Effect, 1 - Button, 2 - Passive, 3 - UpLevels, 4 - Item
	int UpdatesType; // 0 - netral, 1 - positive, 2 - negative, 3 - ...
	//
	bool Visible; // true - use in interface, false - not use in interface
	short FileID;
	int SpriteID;
	_str Hint;
	OneObject* OB;
	//-----Influence of unit ability on different unit parameters-----//
	DWORD InfluenceMask;//contains combination of ABL_... flags. 
	                    //If flag is set, then this ability can ifluence on this type of action	
	                    //constructor should FILL this field.
	//WARNING! every of the next functions can change properties of OneObject-s
	//only if it accepts OneObject* p and p!=NULL. if p==NULL it mostly means
	//that interface system requires some information, so sync can fail if
	//OneObject-s properties will change
	DWORD EfAnimationMask;
	virtual void modifyMotionSpeed       (int BasicSpeed,int& CurrentSpeed);
	virtual void modifyProtection        (int AttType,int BasicProtection,int& CurrentProtection,OneObject* Damager);
	virtual void modifyShield            (int BasicShield,int& CurrentShield,OneObject* Damager);
	virtual void modifyDamage            (int AttType,int BasicDamage,int& CurrentDamage,OneObject* Victim);
	virtual void modifyAttackRate        (int AttackType,int BasicRate,int& ChangedRate,OneObject* Victim);
	virtual void modifyMagicImmunity     (bool Basic,bool& Current,OneObject* Caster);
	virtual void modifyLifeRegeneration  (int Basic,int& Current);//HP per 100 sec
	virtual void modifyWeapon			 (Weapon* Basic,Weapon** Current);
	//----------------------------------------------------------------//
	virtual bool Process();
	virtual bool Process(OneObject* OB);
	virtual bool OnClick();
	virtual bool OnRightClick();
	virtual bool Execute(word TargetUnit, int x, int y, int z);
	virtual bool CanApply();
	virtual bool isTarget();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z);
	virtual int GetCoolDownProc();
	virtual bool isActive();
	virtual bool OnMakeDamage(OneObject* Take,int& Damage);
	//
	virtual bool DrawCursor(int x,int y);
	//
	virtual bool IsHero();
	virtual int GetRadius();
	virtual bool ShowRadius();

	virtual bool CanYouAddToObject(OneObject* OB,void* Param);
	virtual bool AddToObject(OneObject* OB,void* Param);
	virtual bool AddToObject(OneObject* OB);
	virtual bool FillParam(OneObject* OB, void* Param);
	virtual ActiveUnitAbility* GetActionAbilityExample();
	virtual int GetUsePause();

	virtual int AddActionAbilityOverUnitsInRadius();
	virtual const char* GetHint();

	UnitAbility* GetA();
	void SetA(UnitAbility* A);
	UnitAbility* GetAW();
	UnitAbilityIcon* IconInfo;
	UnitAbilityIcon* GetUnitAbilityIconInfo();
private:
	static bool ApplyActionAbility(OneObject* OB,void* Param);
	int UseTime;
	ClassPtr<UnitAbility> UnitAbilityPtr;
	ClassRef<UnitAbility> UnitAbilityRef; 
public:
	SAVE(ActiveUnitAbility){
		//REG_MEMBER(_str,Name);
		REG_MEMBER(_int,Type);
		REG_MEMBER(_int,TypeAbil);
		REG_MEMBER(_int,UnitIndex);
		REG_MEMBER(_bool,Visible);
		REG_MEMBER(_gpfile,FileID);
		REG_MEMBER(_int,SpriteID);
		REG_MEMBER(_str,Hint);
		REG_AUTO(UnitAbilityRef);
		REG_AUTO(UnitAbilityPtr);
	}ENDSAVE;
};
//==================================================================================================================//
class ActiveUnitAbilityAura : public ActiveUnitAbility
{
public:
	ActiveUnitAbilityAura();
	SAVE(ActiveUnitAbilityAura)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
	virtual int GetRadius();
	static bool CheckAbil(OneObject* OB,void* param);
};
//==================================================================================================================//
class OneUnitInComposition:public BaseClass{
public:
	OneUnitInComposition(){
		ZAccelerationWhenDie=7;
		XSpeedWhenDie=0;
		YSpeedWhenDie=-50;   
	}
	int UnitType;
    int dx,dy,dz,LeadDist;

	int ZAccelerationWhenDie;
	int XSpeedWhenDie;
	int YSpeedWhenDie;

	int ModelID;
	_str NodeName;
	int LeadingType;
	SAVE(OneUnitInComposition);
		REG_MEMBER(_UnitType,UnitType);
		REG_MEMBER(_int,dx);
		REG_MEMBER(_int,dy);
		REG_MEMBER(_int,dz);

		REG_MEMBER(_int,ZAccelerationWhenDie);
		REG_MEMBER(_int,XSpeedWhenDie);
		REG_MEMBER(_int,YSpeedWhenDie);

		REG_MEMBER(_int,LeadDist);    
		REG_MEMBER(_ModelID,ModelID);
		REG_AUTO(NodeName);
		REG_ENUM(_index,LeadingType,LEADING_TYPE);
	ENDSAVE;	
};
class UnitsComposition:public UnitAbility{
public:
	//UnitsComposition();
	ClonesArray<OneUnitInComposition> Units;	
	SAVE(UnitsComposition);
		REG_PARENT(UnitAbility);	
		REG_AUTO(Units);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* OBJ);
};
class PushAllOnTheWay:public UnitAbility{
public:
	PushAllOnTheWay(){
		PushForwardRadius=100;		
		PushLeftRightRadius=60;
		PushAngle=80;
		MaxDamage=30;
		MinDamage=50;
		PushForce=100;
		PushEnemyUnits=true;
		PushFriendlyUnits=true;
		DamageFriendlyUnits=true;
		DamageEnemyUnits=true;
		KillNonPushableUnits=true;
		AskGoAwayDistance=200;
		AskFriendlyUnitsGoAway=true;
		UseAttack1NearEnemy=0;
		CanScare=false;
		MaxBoldness=0;
		BoldnessRegeneration=0;
		PushCenterShift=0;
	}
	int PushForwardRadius;	
	int PushLeftRightRadius;
	int PushAngle;
	int MaxDamage;
	int MinDamage;
	int PushForce;
	int AskGoAwayDistance;	
	bool PushEnemyUnits;
	bool PushFriendlyUnits;
	bool DamageFriendlyUnits;
	bool DamageEnemyUnits;	
	bool AskFriendlyUnitsGoAway;
	bool KillNonPushableUnits;
	bool UseAttack1NearEnemy;
	bool CanScare;
	int MaxBoldness;
	int BoldnessRegeneration;
	int PushCenterShift;
	SAVE(PushAllOnTheWay);
		REG_PARENT(UnitAbility);	
		REG_MEMBER(_int,PushForwardRadius);
		REG_MEMBER(_int,PushLeftRightRadius);
		REG_MEMBER(_int,PushAngle);
		REG_MEMBER(_int,MaxDamage);
		REG_MEMBER(_int,MinDamage);
		REG_MEMBER(_int,PushForce);
		REG_MEMBER(_int,AskGoAwayDistance);
		REG_MEMBER(_bool,PushEnemyUnits);
		REG_MEMBER(_bool,PushFriendlyUnits);	
		REG_MEMBER(_bool,DamageEnemyUnits);
		REG_MEMBER(_bool,DamageFriendlyUnits);	
		REG_MEMBER(_bool,AskFriendlyUnitsGoAway);
		REG_MEMBER(_bool,KillNonPushableUnits);
		REG_MEMBER(_bool,UseAttack1NearEnemy);
		REG_MEMBER(_bool,CanScare);
		REG_MEMBER(_int,MaxBoldness);
		REG_MEMBER(_int,BoldnessRegeneration);
		REG_MEMBER(_int,PushCenterShift);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* OBJ);
};
//==================================================================================================================//
#define NSlowDegrees 10
class BeSlowNearUnits:public UnitAbility{
public:
	int Radius;
	int ShiftFromCenter;
	int MaxUnitsAmount;
	bool BeSlowOnlyInEnemyGroup;
	float SlowDegree[NSlowDegrees];
	BeSlowNearUnits(){
		Radius=64;
		ShiftFromCenter=0;
		MaxUnitsAmount=30;
		BeSlowOnlyInEnemyGroup=true;
		for(int i=0;i<NSlowDegrees;i++)SlowDegree[i]=1.0f*(NSlowDegrees-i-1)/(NSlowDegrees-1);
	}
	SAVE(BeSlowNearUnits);
		REG_PARENT(UnitAbility);		
        REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,ShiftFromCenter);
		REG_MEMBER(_bool,BeSlowOnlyInEnemyGroup);
		REG_MEMBER(_int,MaxUnitsAmount);
		for(int i=0;i<NSlowDegrees;i++){
			char cc[16];
			sprintf(cc,"SlowDegree%d",i);
			REG_MEMBER_EX2(_float01,SlowDegree[i],cc);
		}
	ENDSAVE;	
	virtual ActiveUnitAbility* GetActiveAbility();
	virtual void CopyToActive(ActiveUnitAbility* ab);
};
class aa_BeSlowNearUnits:public ActiveUnitAbility{
public:
	BeSlowNearUnits ABL;
	SAVE(aa_BeSlowNearUnits);
	REG_PARENT(ActiveUnitAbility);
	REG_AUTO(ABL);
	ENDSAVE;
	virtual bool Process(OneObject* OB);
};
//==================================================================================================================//
class MakeDamageOnDeath:public UnitAbility{
public:
	bool DamageEnemy;
	bool DamageFriens;
	int xc;
	int yc;
	int R;
	int DamageValue;
	SAVE(MakeDamageOnDeath);
		REG_PARENT(UnitAbility);
		REG_MEMBER(_bool,DamageEnemy);
		REG_MEMBER(_bool,DamageFriens);
		REG_MEMBER(_int,xc);
		REG_MEMBER(_int,yc);
		REG_MEMBER(_int,R);
		REG_MEMBER(_int,DamageValue);
	ENDSAVE;
	MakeDamageOnDeath(){
		DamageEnemy=true;
		DamageFriens=true;
		xc=0;
		yc=0;
		R=50;
		DamageValue=1000;
	}
	virtual ActiveUnitAbility* GetActiveAbility();
	virtual void CopyToActive(ActiveUnitAbility* ab);
};
class aa_MakeDamageOnDeath:public ActiveUnitAbility{
public:
	MakeDamageOnDeath MD;
	bool Done;
	SAVE(aa_MakeDamageOnDeath);
	REG_AUTO(MD);
	REG_MEMBER(_bool,Done);
	ENDSAVE;
	virtual bool Process(OneObject* OB);
	aa_MakeDamageOnDeath(){
        Done=false;
	}
};
//==================================================================================================================//
class UnitAbilityMagicEffect : public UnitAbility
{
public:
	UnitAbilityMagicEffect();
	_str WeaponModificatorName;
	UserFriendlyNumericalReturner MinDist;
	UserFriendlyNumericalReturner MaxDist;
	UserFriendlyNumericalReturner Damage;
	UserFriendlyNumericalReturner Radius;
	UserFriendlyNumericalReturner ManaCost;
	UserFriendlyNumericalReturner CoolDownTime;
	bool ContinueUse;
	UserFriendlyNumericalReturner UsePause;
	UserFriendlyNumericalReturner N;
	int AttType;
	bool EnemyUnitTarget;
	bool FriendlyUnitTarget;
	bool SetPointTargetPoint;
	bool NeedTarget;
	bool TipaAura;
	bool ManualOnOff;
	ClonesArray<HeroVariable> Variables;
	SAVE(UnitAbilityMagicEffect)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_str,WeaponModificatorName);
		REG_AUTO(MinDist);
		REG_AUTO(MaxDist);
		REG_AUTO(Damage);
		REG_AUTO(Radius);
		REG_AUTO(ManaCost);
		REG_AUTO(CoolDownTime);
		REG_MEMBER(_bool,ContinueUse);
		REG_AUTO(UsePause);
		REG_AUTO(N);
		REG_MEMBER(_int,AttType);
		REG_MEMBER(_bool,EnemyUnitTarget);
		REG_MEMBER(_bool,FriendlyUnitTarget);
		REG_MEMBER(_bool,SetPointTargetPoint);
		REG_MEMBER(_bool,NeedTarget);
		REG_MEMBER(_bool,TipaAura);
		REG_MEMBER(_bool,ManualOnOff);
		REG_AUTO(Variables);
	ENDSAVE;

	virtual bool OnUnitBirth(OneObject* OBJ);
};
//==================================================================================================================//
class ActiveUnitAbilityMagicEffect : public ActiveUnitAbility
{
public:
	ActiveUnitAbilityMagicEffect();
	SAVE(ActiveUnitAbilityMagicEffect)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
	virtual bool isTarget();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z);
	virtual bool Execute(word TargetUnit, int x, int y, int z);
	virtual bool OnClick();
	int TempRadius;
	virtual bool DrawCursor(int x,int y);

	int Tx;
	int Ty;
	int Tz;
	int UseTime;
	word Target;
	int CoolDownTime;
	bool OnOff;

	HeroVariableStorage* HVS;
	bool CheckHero;
private:
	bool ApplayWeapon();
};
//==================================================================================================================//
class IntelligentAura : public UnitAbility 
{
public:
	IntelligentAura();
	int Radius;
	SAVE(IntelligentAura)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,Radius);
	ENDSAVE;
};
//==================================================================================================================//
class VampiricAbility : public UnitAbility
{
public:
	VampiricAbility();
	//int DamageProc;
	int LifeProc;
	SAVE(VampiricAbility)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,LifeProc);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* OBJ);
	//virtual bool Execute(word Unit, word TargetUnit, int x, int y, int z);
};
//==================================================================================================================//
class ActiveVampiricAbility : public  ActiveUnitAbility
{
public:
	SAVE(ActiveVampiricAbility)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool OnMakeDamage(OneObject* Take,int& Damage);
	virtual bool Process();
};
//==================================================================================================================//
class ChangeMDAbiliti : public UnitAbility
{
public:
	ChangeMDAbiliti();
	BaseClassUnitType NewType;
	int ThroughState;
	SAVE(ChangeMDAbiliti)
		REG_PARENT(UnitAbility);
		REG_AUTO(NewType);
		REG_MEMBER(_int,ThroughState);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* OBJ);
};
//==================================================================================================================//
class BlizardAbility : public UnitAbility
{
public:
	int MaxDist;
	UserFriendlyNumericalReturner Damage;
	UserFriendlyNumericalReturner Radius;
	int AttType;
	int DamagePause;
	_str EffectName;
	UserFriendlyNumericalReturner ManaCost;
	UserFriendlyNumericalReturner CoolDownTime;
	ClonesArray<HeroVariable> Variables;
	SAVE(BlizardAbility)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,MaxDist);
		REG_AUTO(Damage);
		REG_AUTO(Radius);
		REG_MEMBER(_int,AttType);
		REG_MEMBER(_int,DamagePause);
		REG_MEMBER(_str,EffectName);
		REG_AUTO(ManaCost);
		REG_AUTO(CoolDownTime);
		REG_AUTO(Variables);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* OBJ);
};
//==================================================================================================================//
class ActiveBlizardAbility : public ActiveUnitAbility
{
public:
	ActiveBlizardAbility();

	int Tx;
	int Ty;
	int Tz;
	int InUseTime;
	int CoolDownTime;

	SAVE(ActiveBlizardAbility)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,Tx);
		REG_MEMBER(_int,Ty);
		REG_MEMBER(_int,InUseTime);
		REG_MEMBER(_int,CoolDownTime);
	ENDSAVE;

	HeroVariableStorage* HVS;
	bool CheckHero;

	virtual bool Process();
	virtual bool isTarget();
	virtual bool CanTarget(word TargetUnit, int x, int y, int z);
	virtual bool Execute(word TargetUnit, int x, int y, int z);
	int TempRadius;
	virtual bool DrawCursor(int x,int y);
};
//==================================================================================================================//

class ArrayAbilities:public ClassArray<UnitAbility>
{
public:
	int GetExpansionRules();
};
class AbilityList : public BaseClass
{
public:
	//AbilityList(void);
	ArrayAbilities Abilities;
	SAVE(AbilityList)
		REG_AUTO(Abilities);
	ENDSAVE;

	bool LoadAbilities(char* FileName);
};
//==================================================================================================================//
class MonsterAbility : public BaseClass
{
public:
	MonsterAbility();
	bool Process(int UnitIndex);
	void Init(NewMonster* NM);
	//
	ClassArray<_str> AbilityNames;
	DynArray<UnitAbility*> AbilitiesList;
	bool Feeled;
	SAVE(MonsterAbility)
		REG_AUTO(AbilityNames);
	ENDSAVE;	
};
//==================================================================================================================//
#define ListWalk(x) if(InfluenceMask&ABL_##x)for(int i=0;i<GetAmount();i++)if(Values[i]->InfluenceMask&ABL_##x)Values[i]->modify##x

class ActiveAbilityListArray : public ClassArray<ActiveUnitAbility>
{
public:
	static DWORD CurSerial;
	DWORD InfluenceMask;//contains combination of ABL_... flags. 
	//If flag is set, then this ability can ifluence on this type of action	
	//constructor should FILL this field.
	DWORD EfAnimationMask;
	virtual void modifyMotionSpeed       (int BasicSpeed,int& CurrentSpeed){
        ListWalk(MotionSpeed)(BasicSpeed,CurrentSpeed);
	}
	virtual void modifyProtection        (int AttType,int BasicProtection,int& CurrentProtection,OneObject* Damager){
        ListWalk(Protection)(AttType,BasicProtection,CurrentProtection,Damager);
	}
	virtual void modifyShield            (int BasicShield,int& CurrentShield,OneObject* Damager){
		ListWalk(Shield)(BasicShield,CurrentShield,Damager);
	}
	virtual void modifyDamage            (int AttType,int BasicDamage,int& CurrentDamage,OneObject* Victim){
		ListWalk(Damage)(AttType,BasicDamage,CurrentDamage,Victim);
	}
	virtual void modifyAttackRate        (int AttType,int BasicRate,int& ChangedRate,OneObject* Victim){
		ListWalk(AttackRate)(AttType,BasicRate,ChangedRate,Victim);
	}
	virtual void modifyMagicImmunity     (bool Basic,bool& Current,OneObject* Caster){
		ListWalk(MagicImmunity)(Basic,Current,Caster);
	}
	virtual void modifyLifeRegeneration  (int Basic,int& Current){//HP per 100 sec
		ListWalk(LifeRegeneration)(Basic,Current);
	}	
	virtual void modifyWeapon			 (Weapon* Basic,Weapon** Current){
		ListWalk(Weapon)(Basic,Current);
	}
	//
	int Add(ActiveUnitAbility* V);
	inline void Del(int pos,int N);
};
class UnitActiveAbilityList : public BaseClass
{
public:
	UnitActiveAbilityList(void);
	ActiveAbilityListArray ActiveAbilities;
	bool Process();
	bool AddActiveUnitAbility(ActiveUnitAbility* AUA);
	ActiveUnitAbility* GetActiveUnitAbility(const char* name);
	SAVE(UnitActiveAbilityList){
		REG_AUTO(ActiveAbilities);
	}ENDSAVE;
};
//==================================================================================================================//
class LeadSeveralUnitsAbility:public ActiveUnitAbility{//Drew
public:
	//LeadSeveralUnitsAbility();
	word ObjID;
	word ObjSN;
	int dx,dy,dz;
	int LeadDistance;
	int ActionType;//0-set position 1-lead using DestX
	DWORD ModelID;
	DWORD NodeID;
	SAVE(LeadSeveralUnitsAbility);
	REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
};
class PushUnitsAbility:public ActiveUnitAbility{//Drew
public:
	PushUnitsAbility();
	PushAllOnTheWay Params;
	word OwnerID;
	word OwnerSN;
	int Boldness;
	int LastTimeCheck;
	SAVE(PushUnitsAbility);
		REG_PARENT(ActiveUnitAbility);
		REG_AUTO(Params);
		REG_MEMBER(_short,OwnerID);
		REG_MEMBER(_short,OwnerSN);
		REG_MEMBER(_int,Boldness);
		REG_MEMBER(_int,LastTimeCheck);
	ENDSAVE;
	virtual bool Process();
};
//==================================================================================================================//
class BeSlaveOfUnit:public ActiveUnitAbility{//Drew
public:
	LeadSeveralUnitsAbility();
	word ObjID;
	word ObjSN;

	int DeathAccelerationZ;
	int DeathSpeedX;
	int DeathSpeedY;
	int LastX;
	int LastY;
	int LastZ;

	SAVE(BeSlaveOfUnit);
	REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
};
//==================================================================================================================//
class CoolDown : public ActiveUnitAbility
{
public:
	CoolDown(void);
	CoolDown(int UnitAbilityIndex, int Value);
	int UnitAbilityIndex;
	int Value;
	int LastProcesedTime;
	SAVE(CoolDown)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,UnitAbilityIndex);
		REG_MEMBER(_int,Value);
		REG_MEMBER(_int,LastProcesedTime);
	ENDSAVE;
	virtual bool Process();
};
//==================================================================================================================//
class AuraEffect : public ActiveUnitAbility
{
public:
	AuraEffect(void);
	int Creator;
	int AbilityIndex;
	int LastProcesedTime;
	//int AddDamage;
	//int AddShield;
	//int Cure;
	//int Dist;
	SAVE(AuraEffect)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,Creator);
		REG_MEMBER(_int,AbilityIndex);
		REG_MEMBER(_int,LastProcesedTime);
		//REG_MEMBER(_int,AddDamage);
		//REG_MEMBER(_int,AddShield);
		//REG_MEMBER(_int,Cure);
		//REG_MEMBER(_int,Dist);
	ENDSAVE;
	virtual bool Process();
};
//==================================================================================================================//
class IntelligentAuraEffect : public ActiveUnitAbility
{
public:
	int Creator;
	int AbilityIndex;
	IntelligentAuraEffect();
	SAVE(IntelligentAuraEffect)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
};
//==================================================================================================================//
class ChangeMDAbilitiActive : public ActiveUnitAbility
{
public:
	ChangeMDAbilitiActive();
	//BaseClassUnitType NewType;
	//int ThroughState;
	SAVE(ChangeMDAbilitiActive)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
};
//==================================================================================================================//
class CannonAutoShot : public UnitAbility
{
public:
	CannonAutoShot();
	SAVE(CannonAutoShot)
		REG_PARENT(UnitAbility);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
class CannonAutoShotActive : public ActiveUnitAbility
{
public:
	CannonAutoShotActive();
	bool On;
	SAVE(CannonAutoShotActive)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_bool,On);
	ENDSAVE;
	virtual bool Process();
	virtual bool OnClick();
};
//==================================================================================================================//
class LeveledActiveUnitAbility : public ActiveUnitAbility
{
public:
	LeveledActiveUnitAbility();
	int Level;
	bool VirtualUp;

	int* Le;
	bool* Vi;
    int HintParam;

	SAVE(LeveledActiveUnitAbility)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,Level);
		REG_MEMBER(_bool,VirtualUp);
	ENDSAVE;
	virtual bool UpLevel();
	virtual bool OnClick();
	virtual bool CanApply();
	virtual const char* GetHint();
	virtual const char* GetHintOnLevel(int Lev);
	virtual int GetHintParamOnLevel(int Lev);
private:
	DString TempHint;
	DString TempHintLv;
};
//==================================================================================================================//
class RomeHero : public UnitAbility
{
public:
	RomeHero();
	LinearArray<int,_int> LevelUp;
	int ExpProcIfNotKiller;
	bool IsHero;
	bool GatherExperienceInCampaign;
	int UpOrder[3];
	bool StayBack;
	SAVE(RomeHero)
		REG_PARENT(UnitAbility);
		REG_AUTO(LevelUp);
		REG_MEMBER(_int,ExpProcIfNotKiller);
		REG_MEMBER(_bool,IsHero);
		REG_MEMBER(_bool,GatherExperienceInCampaign);
		REG_MEMBER(_int,UpOrder[0]);
		REG_MEMBER(_int,UpOrder[1]);
		REG_MEMBER(_int,UpOrder[2]);
		REG_MEMBER(_bool,StayBack);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveRomeHero : public ActiveUnitAbility
{
public:
	ActiveRomeHero();
	~ActiveRomeHero();
	int Level;
	int Expa;
	int ExpaToNextLevel;
	bool WaitForAbilUp;
	bool LoadFromProfile;
	
	int* Le;
	int* Ex;
	bool* Wa;
	
	SAVE(ActiveRomeHero)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,Level);
		REG_MEMBER(_int,Expa);
		REG_MEMBER(_int,ExpaToNextLevel);
		REG_MEMBER(_bool,WaitForAbilUp);
	ENDSAVE;

	virtual bool Process(OneObject* OB);
	virtual bool IsHero();
	void GatherExperience(OneObject* Victim,OneObject* Killer, byte Part);

	int GetExperienceToNextLevel();
	int UpLevel();
	int AddExperience(int Expa);
	int GetLevel();
	int GetExperience();
	int GetAmountFreeLevels();
};
//==================================================================================================================//
class RomeHeroCollector : public BaseClass
{
public:
	RomeHeroCollector();
	DynArray<ActiveRomeHero*> Herosima;
	DynArray<int> NHeroInMap;
	bool Calk;

	void ClearAll();
	bool AddExp(OneObject* Victim,OneObject* Killer);
	void AddRomeHero(ActiveRomeHero* Her);
	void DelRomeHero(ActiveRomeHero* Her);
};
//==================================================================================================================//
class LifeLimitation : public UnitAbility
{
public:
	LifeLimitation();
	UserFriendlyNumericalReturner LifeLength;
	_str DieWeaponEffect;
	SAVE(LifeLimitation)
		REG_PARENT(UnitAbility);
		REG_AUTO(LifeLength);
		REG_AUTO(DieWeaponEffect);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
class ActiveLifeLimitation : public ActiveUnitAbility
{
public:
	ActiveLifeLimitation();
	int DieTime;
	SAVE(ActiveLifeLimitation)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,DieTime);
	ENDSAVE;
	virtual bool Process();
};
//==================================================================================================================//
class GreetingMe : public UnitAbility
{
public:
	GreetingMe();
	int Radius;
	SAVE(GreetingMe)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,Radius);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
class ActiveGreetingMe : public ActiveUnitAbility
{
public:
	ActiveGreetingMe();
	SAVE(ActiveGreetingMe)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	int UseTime;
	virtual bool Process();
private:
	static bool AddActiveGreeting(OneObject* OB,void* Param);
};
//==================================================================================================================//
class ActiveGreeting : public ActiveUnitAbility
{
public:
	ActiveGreeting();
	SAVE(ActiveGreeting)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	int PrevDir;
	int State;
	OneObject* GreetingObject;
	virtual bool Process();
};
//==================================================================================================================//
class NationBonus : public UnitAbility2
{
public:
	NationBonus();
	int AddDamage;
	int AddShield;
	UnitTypeList UnitsType;
	bool UseUpgrade;
	int UpgradeBonus;
	int IfDieDowngrade;
	int UpgradeBonusL2;
	int IfDieDowngradeL2;
	int UpgradeBonusL3;
	int IfDieDowngradeL3;
	int UpgradeBonusL4;
	int IfDieDowngradeL4;
	int UpgradeBonusL5;
	int IfDieDowngradeL5;
	int HintParam[5];
	SAVE(NationBonus)
		REG_BASE(UnitAbility);
		REG_PARENT(UnitAbility2);
		REG_MEMBER(_int,AddDamage);
		REG_MEMBER(_int,AddShield);
		REG_AUTO(UnitsType);
		REG_MEMBER(_bool,UseUpgrade);
		REG_ENUM(_index,UpgradeBonus,ALL_UPGRADES);
		REG_ENUM(_index,IfDieDowngrade,ALL_UPGRADES);
		REG_ENUM(_index,UpgradeBonusL2,ALL_UPGRADES);
		REG_ENUM(_index,IfDieDowngradeL2,ALL_UPGRADES);
		REG_ENUM(_index,UpgradeBonusL3,ALL_UPGRADES);
		REG_ENUM(_index,IfDieDowngradeL3,ALL_UPGRADES);
		REG_ENUM(_index,UpgradeBonusL4,ALL_UPGRADES);
		REG_ENUM(_index,IfDieDowngradeL4,ALL_UPGRADES);
		REG_ENUM(_index,UpgradeBonusL5,ALL_UPGRADES);
		REG_ENUM(_index,IfDieDowngradeL5,ALL_UPGRADES);
		REG_MEMBER(_int,HintParam[0]);
		REG_MEMBER(_int,HintParam[1]);
		REG_MEMBER(_int,HintParam[2]);
		REG_MEMBER(_int,HintParam[3]);
		REG_MEMBER(_int,HintParam[4]);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
class ActiveNationBonus : public LeveledActiveUnitAbility
{
public:
	ActiveNationBonus();
	bool m_die;
	SAVE(ActiveNationBonus)
		REG_BASE(ActiveUnitAbility);
		REG_PARENT(LeveledActiveUnitAbility);
		REG_MEMBER(_bool,m_die); 
	ENDSAVE;
	virtual bool Process();
	virtual bool UpLevel();
	virtual int GetHintParamOnLevel(int Lev);
};
//==================================================================================================================//
class HeroLight : public UnitAbility
{
public:
	HeroLight();
	_str EffectName; 
	_str DieEffect;
	bool IsHero;
	SAVE(HeroLight)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_str,EffectName);
		REG_MEMBER(_str,DieEffect);
		REG_MEMBER(_bool,IsHero);
	ENDSAVE;
	virtual bool OnUnitBirth(OneObject* Newbie);
};
//==================================================================================================================//
class ActiveHeroLight : public ActiveUnitAbility
{
public:
	ActiveHeroLight();
	bool IsInit;
	SAVE(ActiveHeroLight)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_bool,IsInit);
	ENDSAVE;
	virtual bool IsHero();
	virtual bool Process();
};
//==================================================================================================================//
class LifeRegeneration : public UnitAbility
{
public:
	LifeRegeneration();
	int Regeneration;
	int Radius;
	int UsePause;
	int CoolDownTime;
	SAVE(LifeRegeneration)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,Regeneration);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,UsePause);
		REG_MEMBER(_int,CoolDownTime);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveLifeRegeneration : public ActiveUnitAbility
{
public:
	ActiveLifeRegeneration();
	SAVE(ActiveLifeRegeneration)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
	virtual bool CanApply();
	virtual int GetCoolDownProc();
	virtual bool ShowRadius();
	virtual bool OnClick();
private:
	int LastUseTime;
	bool Exec();
	static bool AddLife(OneObject* OB, void* Param);
};
//==================================================================================================================//
class SetMineBonus : public UnitAbility2
{
public:
	SetMineBonus();
	int Bonus;
	int Radius;
	int BonusAddForLevel;
	//UnitTypeList MineType;
	SAVE(SetMineBonus)
		REG_BASE(UnitAbility);
		REG_PARENT(UnitAbility2);
		REG_MEMBER(_int,Bonus);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,BonusAddForLevel);
		//REG_AUTO(MineType);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveSetMineBonus : public LeveledActiveUnitAbility
{
public:
	ActiveSetMineBonus();
	SAVE(ActiveSetMineBonus)
		REG_BASE(ActiveUnitAbility);
		REG_PARENT(LeveledActiveUnitAbility);
	ENDSAVE;
	virtual bool Process();
	virtual int GetRadius();
	virtual int GetHintParamOnLevel(int Lev);
private:
	static bool AddMineBonus(OneObject* OB, void* Param);
	int LastUseTime;
};
//==================================================================================================================//
class ActiveMineBonus : public ActiveUnitAbility
{
public:
	ActiveMineBonus();
	SAVE(ActiveMineBonus)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	int Radius;
	int Bonus;
	int HeroIndex;
	virtual bool Process();
};
//==================================================================================================================//
class IncreaseMaxLife : public UnitAbility2
{
public:
	IncreaseMaxLife();
	int Points;
	int Radius;
	UnitTypeList UnitType;
	int AddForLevel;
	SAVE(IncreaseMaxLife)
		REG_BASE(UnitAbility);
		REG_PARENT(UnitAbility2);
		REG_MEMBER(_int,Points);
		REG_MEMBER(_int,Radius);
		REG_AUTO(UnitType);
		REG_MEMBER(_int,AddForLevel);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();	
};
//==================================================================================================================//
class ActiveAdditionalLife : public ActiveUnitAbility
{
public:
	ActiveAdditionalLife();
	int HeroIndex;
	int LifeAdded;
	SAVE(ActiveAdditionalLife) 
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,HeroIndex);
		REG_MEMBER(_int,LifeAdded);
	ENDSAVE;

	virtual bool Process(OneObject* OB);
	virtual bool CanYouAddToObject(OneObject* OB,void* Param);
	virtual bool FillParam(OneObject* OB, void* Param);
};
//==================================================================================================================//
class ActiveIncreaseMaxLife : public LeveledActiveUnitAbility
{
public:
	ActiveIncreaseMaxLife();
	SAVE(ActiveIncreaseMaxLife)
		REG_BASE(ActiveUnitAbility);
		REG_PARENT(LeveledActiveUnitAbility);
	ENDSAVE;
	ActiveAdditionalLife Example;

	virtual bool Process(OneObject* OB);
	virtual int GetRadius();
	virtual ActiveUnitAbility* GetActionAbilityExample();
	virtual int GetUsePause();
	virtual int GetHintParamOnLevel(int Lev);
};
//==================================================================================================================//
class AddUnitBonus : public UnitAbility2
{
public:
	AddUnitBonus();
	int Radius;
	int AddDamage;
	int AddShield;
	bool EnemyTarget;
	int CoolDownTime;
	int EffectTime;
	int UsePause;
	UnitTypeList UnitType;
	int AddForLevel;
	SAVE(AddUnitBonus)
		REG_BASE(UnitAbility);
		REG_PARENT(UnitAbility2);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,AddDamage);
		REG_MEMBER(_int,AddShield);
		REG_MEMBER(_bool,EnemyTarget);
		REG_MEMBER(_int,CoolDownTime);
		REG_MEMBER(_int,EffectTime);
		REG_MEMBER(_int,UsePause);
		REG_AUTO(UnitType);
		REG_MEMBER(_int,AddForLevel);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveAddUnitBonus : public LeveledActiveUnitAbility
{
public:
	ActiveAddUnitBonus();
	SAVE(ActiveAddUnitBonus)
		REG_BASE(ActiveUnitAbility);
		REG_PARENT(LeveledActiveUnitAbility);
	ENDSAVE;

	virtual bool Process(OneObject* OB);
	virtual bool CanApply();
	virtual int GetCoolDownProc();
	virtual int GetRadius();
	virtual bool OnClick();
	virtual int GetHintParamOnLevel(int Lev);
private:
	int LastUseTime;
	bool Exec();
	static bool SetBonus(OneObject* OB, void* Param);
};
//==================================================================================================================//
class PassiveAddUnitBonus : public ActiveUnitAbility
{
public:
	PassiveAddUnitBonus();
	int Radius;
	int AddDamage;
	int AddShield;

	int HeroIndex;
	int SetTime;
	SAVE(PassiveAddUnitBonus)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,AddDamage);
		REG_MEMBER(_int,AddShield);
		REG_MEMBER(_int,HeroIndex);
		REG_MEMBER(_int,SetTime);
	ENDSAVE;
	virtual bool Process(OneObject* OB);
};
//==================================================================================================================//
class BuildingShield : public UnitAbility2
{
public:
	BuildingShield();
	int Radius;
	int AddShield;
	bool EnemyTarget;
	int ShieldAddForLevel;
	SAVE(BuildingShield)
		REG_BASE(UnitAbility);
		REG_PARENT(UnitAbility2);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,AddShield);
		REG_MEMBER(_bool,EnemyTarget);
		REG_MEMBER(_int,ShieldAddForLevel);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveAddBuildingShield : public LeveledActiveUnitAbility
{
public:
	ActiveAddBuildingShield();
	SAVE(ActiveAddBuildingShield)
		REG_BASE(ActiveUnitAbility);
		REG_PARENT(LeveledActiveUnitAbility);
	ENDSAVE;
	int LastUseTime;
	virtual bool Process(OneObject* OB);
	virtual int GetRadius();

	static bool AddBuildingShield(OneObject* Ob, void* param);
	virtual int GetHintParamOnLevel(int Lev);
};
//==================================================================================================================//
class ActiveBuildingShield : public ActiveUnitAbility
{
public:
	ActiveBuildingShield();
	SAVE(ActiveBuildingShield)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,HeroIndex);
		REG_MEMBER(_int,AddShield);
	ENDSAVE;
	int HeroIndex;
	int LastUseTime;
	int AddShield;
	virtual bool Process(OneObject* OB);
};
//==================================================================================================================//
class FollowBrigade : public UnitAbility
{
public:
	FollowBrigade();
	bool MoveOutEnemy;
	SAVE(FollowBrigade)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_bool,MoveOutEnemy);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveFollowBrigade : public ActiveUnitAbility
{
public:
	ActiveFollowBrigade();
	SAVE(ActiveFollowBrigade)
		REG_PARENT(ActiveUnitAbility);
	ENDSAVE;
	virtual bool Process(OneObject* OB);
	static bool CheckEnemy(OneObject* OB,void* param);
private:
	int lastUseTime;
	int lastMoveBack;
};
//==================================================================================================================//
class Behaviour : public UnitAbility
{
public:
	Behaviour();
	int MoveDist;
	int MoveTime;
	int ChangeDir;
	int MaxRestTime;
	int Radius;
	SAVE(Behaviour)
		REG_PARENT(UnitAbility);
		REG_MEMBER(_int,MoveDist);
		REG_MEMBER(_int,MoveTime);
		REG_MEMBER(_int,ChangeDir);
		REG_MEMBER(_int,MaxRestTime);
		REG_MEMBER(_int,Radius);
	ENDSAVE;
	virtual ActiveUnitAbility* GetActiveAbility();
};
//==================================================================================================================//
class ActiveBehaviour : public ActiveUnitAbility
{
public:
	ActiveBehaviour();
	int EndMoveTime;
	int RestTime;
	int BaseX;
	int BaseY;
	SAVE(ActiveBehaviour)
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_int,EndMoveTime);
		REG_MEMBER(_int,RestTime);
		REG_MEMBER(_int,BaseX);
		REG_MEMBER(_int,BaseY);
	ENDSAVE;
	virtual bool Process(OneObject* OB);
private:
	void Move(OneObject* OB, Behaviour* B);
	static bool CheckSprite(OneSprite* OS,void* Param);

};
//==================================================================================================================//
#include "heroability.h"
#include "BrigadeAbility.h"
//==================================================================================================================//

#endif