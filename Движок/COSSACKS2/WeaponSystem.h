#ifndef __WEAPONSYSTEM_H__
#define __WEAPONSYSTEM_H__

#pragma once
#pragma pack ( push )
#pragma pack ( 1 ) 

#include "UnitAbility.h"
#include "SuperEditor.h"
#include "QParser.h"
//==================================================================================================================//
class TargetDesignation : public BaseClass
{
public:
	TargetDesignation();
	int UnitIndex;
	int x;
	int y;
	int z;
	SAVE(TargetDesignation);
		REG_MEMBER(_int,UnitIndex);
		REG_MEMBER(_int,x);
		REG_MEMBER(_int,y);
		REG_MEMBER(_int,z);
	ENDSAVE;
};
//==================================================================================================================//
class AdditionalWeaponParams : public BaseClass
{
public:
	AdditionalWeaponParams();
	int Damage;
	int AttType;
	int Radius;
	int NI;
	int N;
	SAVE(AdditionalWeaponParams);
		REG_MEMBER(_int,Damage);
		REG_MEMBER(_int,AttType);
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,NI);
		REG_MEMBER(_int,N);
	ENDSAVE
};
//==================================================================================================================//
class WeaponModificator;
class HeroVariableStorage;
class WeaponParams : public BaseClass
{
public:
	WeaponParams();
	_str WeaponModificatorName;
	WeaponModificator* WeaponModificatorP;

	TargetDesignation From;
	int OwnerWeaponIndex;
	//int Damage;
	//int AttType;
	TargetDesignation To;
	int BirthTime;
	int LastMoveTime;
	int TraveledDistance;
	int x;
	int y;
	int z;
	bool NeedDelete;
	int V;
	int Dir;
	int DirZ;
	ClassPtr<AdditionalWeaponParams> AdditionalParams;
	
    //  Silver, 3.08.2004
    //  Current direction of the missile, used for rendering purposes only
    float fiDir;    //  yaw
    float fiOrt;    //  pitch

	int Serial;
	bool OnceProcesed;
	SAVE(WeaponParams);
		REG_ENUM(_strindex,WeaponModificatorName,WeaponModificatorEnum);
		//REG_MEMBER(_str,WeaponModificatorName);
		REG_AUTO(From);
		REG_MEMBER(_int,OwnerWeaponIndex);
		//REG_MEMBER(_int,Damage);
		//REG_MEMBER(_int,AttType);
		REG_AUTO(To);
		REG_MEMBER(_int,BirthTime);
		REG_MEMBER(_int,LastMoveTime);
		REG_MEMBER(_int,TraveledDistance);
		REG_MEMBER(_int,x);
		REG_MEMBER(_int,y);
		REG_MEMBER(_int,z);
		REG_MEMBER(_int,V);
		REG_MEMBER(_int,Dir);
		REG_MEMBER(_int,DirZ);
		REG_AUTO(AdditionalParams);
		REG_MEMBER(_int,Serial);
		REG_MEMBER(_bool,OnceProcesed);
		//REG_MEMBER(_int,Vx);
		//REG_MEMBER(_int,Vy);
		//REG_MEMBER(_int,Vz);
	ENDSAVE;

	bool Process();
	bool Draw();
	bool IsOnScreen();
	HeroVariableStorage* GetHeroStorage();
private:
	HeroVariableStorage* HVS;
	bool CheckHero;
};
//==================================================================================================================//
class PointModificator : public BaseClass
{
public:
	SAVE(PointModificator);
	ENDSAVE;

	virtual bool MakeOneStep(WeaponParams* WP);
	virtual bool CanDraw(WeaponParams* WP);
	virtual bool Draw(WeaponParams* WP);
};
//==================================================================================================================//
class WeaponEvent : public BaseClass
{
public:
	SAVE(WeaponEvent);
	ENDSAVE;

	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//
class WeaponModificatorList:public ClassArray<PointModificator>
{
public:
	int GetExpansionRules();
};

class WeaponProcess : public BaseClass
{
public:
	ClassPtr<WeaponEvent> Event;
	WeaponModificatorList WeaponModificators;
	SAVE(WeaponProcess);
		REG_AUTO(Event);
		REG_AUTO(WeaponModificators);
	ENDSAVE;
	bool Check(WeaponParams* WP);
	bool Process(WeaponParams* WP);
	bool CanDraw(WeaponParams* WP);
	bool Draw(WeaponParams* WP);
};
//==================================================================================================================//
class WP_list:public ClonesArray<WeaponProcess>{
public:
	virtual bool ForceSimplification(){
		return true;
	}
};
class WeaponModificator : public ReferableBaseClass
{
public:
	//_str Name;
	Weapon* WOwner;
	WeaponModificator();
	WP_list WPL;
	SAVE(WeaponModificator);
		REG_AUTO(Name);
		REG_AUTO(WPL);
	ENDSAVE;

	bool Process(WeaponParams* WP);
	bool Draw(WeaponParams* WP);
	virtual bool CheckIfObjectIsGlobal(){return true;}
	const char* GetThisElementView(const char* LocalName);
};
//==================================================================================================================//
class WeaponSystem : public BaseClass
{
public:
	WeaponSystem(void);
	ClonesArray<WeaponModificator> AllWeaponModificators;
	ClonesArray<WeaponParams> ActiveWeapons;
	DynArray<Weapon*> Weapons;
	int LastSerial;
	SAVE(WeaponSystem);
		REG_AUTO(AllWeaponModificators);
		REG_AUTO(ActiveWeapons);
		REG_MEMBER(_int,LastSerial);
	ENDSAVE;
	void RefreshEnumerator();
	void Process();
	void Draw();

	bool LoadAllWeaponModificators(char* FileName);
	void ClearAllActiveWeapons();
	void AddActiveWeapon(WeaponParams* W);

};
//==================================================================================================================//
//==================================================================================================================//
class DrawOne : public PointModificator
{
public:
	DrawOne();
	NewAnimation Anim;
	int Frame;
	int ScaleByRadius;
	int AddDirZ;
	SAVE(DrawOne);
		REG_PARENT(PointModificator);
		REG_AUTO(Anim);
		REG_MEMBER(_int,Frame);
		REG_MEMBER(_int,ScaleByRadius);
		REG_MEMBER(_int,AddDirZ);
	ENDSAVE;

	virtual bool CanDraw(WeaponParams* WP);
	virtual bool Draw(WeaponParams* WP);
};
//==================================================================================================================//
class SelfMurder : public PointModificator
{
public:
	SAVE(SelfMurder);
		REG_PARENT(PointModificator);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class StaticMotion : public PointModificator
{
public: 
	StaticMotion();
	int Vx;
	int Ax;
	int Vy;
	int Ay;
	int Vz;
	int Az;
	SAVE(StaticMotion);
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,Vx);
		REG_MEMBER(_int,Ax);
		REG_MEMBER(_int,Vy);
		REG_MEMBER(_int,Ay);
		REG_MEMBER(_int,Vz);
		REG_MEMBER(_int,Az);
	ENDSAVE;
	bool FirstStep;
	int Vmx;
	int Amx;
	int Vmy;
	int Amy;	
	word Dir;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class BalisticMotion : public PointModificator
{
public:
	BalisticMotion();
	int ConstSpeed;
	int ConstHieght;
	int g;
	bool SetTargetHieghtOnGround;
	bool StopInDestPoint;
	SAVE(BalisticMotion)	
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,ConstSpeed);
		REG_MEMBER(_int,ConstHieght);
		REG_MEMBER(_int,g);
		REG_MEMBER(_bool,SetTargetHieghtOnGround);
		REG_MEMBER(_bool,StopInDestPoint);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class Jump : public PointModificator
{
public:
	Jump();
	int JumpDist;
	bool JumpToEnd;
	SAVE(Jump);
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,JumpDist);
		REG_MEMBER(_bool,JumpToEnd);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class Motion :  public PointModificator
{
public:
	SAVE(Motion);
		REG_PARENT(PointModificator);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class HarmonicMotion : public PointModificator
{
public:
	HarmonicMotion();
	int Hx;
	int Tx;
	int Dx;
	int Hy;
	int Ty;
	int Dy;
	int Hz;
	int Tz;
	int Dz;
	SAVE(HarmonicMotion);
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,Hx);
		REG_MEMBER(_int,Tx);
		REG_MEMBER(_int,Dx);
		REG_MEMBER(_int,Hy);
		REG_MEMBER(_int,Ty);
		REG_MEMBER(_int,Dy);
		REG_MEMBER(_int,Hz);
		REG_MEMBER(_int,Tz);
		REG_MEMBER(_int,Dz);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class FollowUnit : public PointModificator 
{
public:
	FollowUnit();
	int F;
	SAVE(FollowUnit);
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,F);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class BirthNew : public PointModificator
{
public:
	BirthNew();
	_str NewWeaponModificator;
	int DamageChange;
	int BirthPause;
	bool LeaveFromPoint;
	int Fr_AddX;
	int Fr_AddY;
	int Fr_AddZ;
	int To_RandomUnitInRadius;
	int To_RandomFrendlyUnitInRadius;
	int To_RandomEnemyUnitInRadius;
	int To_RandomPosInRadius;
	int To_AddX;
	int To_AddY;
	int To_AddZ;
	SAVE(BirthNew);
		REG_PARENT(PointModificator);
		//REG_ENUM(_index,NewWeaponModificator,WeaponModificatorEnum);
		REG_ENUM(_strindex,NewWeaponModificator,WeaponModificatorEnum);
		REG_MEMBER(_int,DamageChange);
		REG_MEMBER(_int,BirthPause);
		REG_MEMBER(_bool,LeaveFromPoint);
		REG_MEMBER(_int,Fr_AddX);
		REG_MEMBER(_int,Fr_AddY);
		REG_MEMBER(_int,Fr_AddZ);
		REG_MEMBER(_int,To_RandomUnitInRadius);
		REG_MEMBER(_int,To_RandomFrendlyUnitInRadius);
		REG_MEMBER(_int,To_RandomEnemyUnitInRadius);
		REG_MEMBER(_int,To_RandomPosInRadius);
		REG_MEMBER(_int,To_AddX);
		REG_MEMBER(_int,To_AddY);
		REG_MEMBER(_int,To_AddZ);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class TargetFinder : public BaseClass
{
public:
	SAVE(TargetFinder);
	ENDSAVE;
	virtual bool GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD);
};

//==================================================================================================================//
class UnitsInRadius : public TargetFinder
{
public:
	UnitsInRadius();
	//int Radius;
	UserFriendlyNumericalReturner Radius;
	UserFriendlyNumericalReturner MinRadius;
	//int MaxUnits;
	UserFriendlyNumericalReturner MaxUnits;
	
	bool Frendly;
	bool Enemy;
	int  UnitsSelectionRule;
	
	SAVE(UnitsInRadius);
		REG_PARENT(TargetFinder);
		//REG_MEMBER(_int,Radius);
		//REG_MEMBER(_int,MaxUnits);
		REG_AUTO(Radius);
		REG_AUTO(MinRadius);
		REG_AUTO(MaxUnits);		
		REG_MEMBER(_bool,Frendly);
		REG_MEMBER(_bool,Enemy);
		REG_ENUM(_index,UnitsSelectionRule,UNITSSELRULE);//RandomUnits,NearestUnits,Unsorted
	ENDSAVE;
	DynArray<int> FindedUnits;
	bool FillList;
	word Owner;
	virtual bool GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD);
	static bool CheckUnitsInRadius(OneObject* OB,void* param);
};
//==================================================================================================================//
class RandomPosInRadius : public TargetFinder
{
public:
	UserFriendlyNumericalReturner MinRadius;
	UserFriendlyNumericalReturner Radius;
	UserFriendlyNumericalReturner N;
	SAVE(RandomPosInRadius);
		REG_PARENT(TargetFinder);
		REG_AUTO(MinRadius);
		REG_AUTO(Radius);
		REG_AUTO(N);
	ENDSAVE;
	virtual bool GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD);
};
class UserDefinedPointCoor:public BaseClass
{
public:
	float x;
	float y;
	float z;
	SAVE(UserDefinedPointCoor);
		REG_MEMBER(_float,x);
		REG_MEMBER(_float,y);
		REG_MEMBER(_float,z);
	ENDSAVE;
};
class UserDefinedPoints : public TargetFinder
{
public:
	UserFriendlyNumericalReturner Radius;
	class _fnAddPointsInRadius:public BaseFunction{
	public:
		float Radius;
		float z;
		int NPoints;
		void EvaluateFunction(){
			UserDefinedPoints* u=get_parent<UserDefinedPoints>();
			if(u){
				for(int i=0;i<NPoints;i++){
					UserDefinedPointCoor* p=new UserDefinedPointCoor;
					float ang=2*c_PI*i/NPoints;
					p->x=Radius*cos(ang);
					p->y=Radius*sin(ang);
					p->z=z;
					u->Points.Add(p);
				}
			}
		}
		SAVE(_fnAddPointsInRadius);
			REG_PARENT(BaseFunction);
			REG_MEMBER(_float,Radius);
			REG_MEMBER(_float,z);
			REG_MEMBER(_int,NPoints);
		ENDSAVE;
	};
	_fnAddPointsInRadius AddPointsInRadius;
	class _fnClearAllPoints:public BaseFunction{
	public:
		void EvaluateFunction(){
			UserDefinedPoints* u=get_parent<UserDefinedPoints>();
			if(u){
				u->Points.Clear();				
			}
		}
		SAVE(_fnClearAllPoints);
		REG_PARENT(BaseFunction);
		ENDSAVE;
	};
	_fnClearAllPoints ClearAllPoints;
	ClonesArray<UserDefinedPointCoor> Points;
	SAVE(UserDefinedPoints)
		REG_PARENT(TargetFinder);
		REG_AUTO(Radius);
		REG_AUTO(AddPointsInRadius);
		REG_AUTO(ClearAllPoints);
		REG_AUTO(Points);
	ENDSAVE;   
	virtual bool GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD);
};
//==================================================================================================================//
class MassBirthNew : public PointModificator
{
public:
	MassBirthNew();
	_str NewWeaponModificator;
	int DamageChange;
	int BirthPause;
	bool LeaveFromPoint;
	ClassPtr<TargetFinder> NewTargetList;
	SAVE(MassBirthNew);
		REG_ENUM(_strindex,NewWeaponModificator,WeaponModificatorEnum);
		REG_MEMBER(_int,DamageChange);
		REG_MEMBER(_int,BirthPause);
		REG_MEMBER(_bool,LeaveFromPoint);
		REG_AUTO(NewTargetList);
		REG_PARENT(PointModificator);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class ChangeModificator : public PointModificator
{
public:
	ChangeModificator();
	_str NewWeaponModificator;
	bool LeaveFromPoint;
	SAVE(ChangeModificator);
		REG_PARENT(PointModificator);
		REG_ENUM(_strindex,NewWeaponModificator,WeaponModificatorEnum);
		REG_MEMBER(_bool,LeaveFromPoint);
	ENDSAVE;
	_str CheckedName;
	WeaponModificator* Wm;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class MakeDamage : public PointModificator
{
public:
	MakeDamage();
	bool OnlyTargetUnits;
	int InRadius;
	bool OnlyEnemyUnits;
	int MaxUnits;
	bool DamageBuilding;
	int PushUnitsForce;
	SAVE(MakeDamage);
		REG_PARENT(PointModificator);
		REG_MEMBER(_bool,OnlyTargetUnits);
		REG_MEMBER(_int,InRadius);
		REG_MEMBER(_bool,OnlyEnemyUnits);
		REG_MEMBER(_int,MaxUnits);
		REG_MEMBER(_bool,DamageBuilding);
		REG_MEMBER(_int,PushUnitsForce);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
	static bool MakeDam(OneObject* OB,void* param);
};
//==================================================================================================================//
class Wave : public PointModificator
{
public:
	Wave();
	int H;
	int MaxR;
	int MinR;
	int PushUnitsForce;
	int LinearWidth;
	bool OnlyEnemyUnits;
	bool Damage;
	SAVE(Wave);
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,H);
		REG_MEMBER(_int,MaxR);
		REG_MEMBER(_int,MinR);
		REG_MEMBER(_int,PushUnitsForce);
		REG_MEMBER(_int,LinearWidth);
		REG_MEMBER(_bool,OnlyEnemyUnits);
		REG_MEMBER(_bool,Damage);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
	static bool MakeWave(OneObject* OB,void* param);
};
//==================================================================================================================//
class HangUpActiveAbility : public PointModificator
{
public:
	HangUpActiveAbility();
	SAVE(MakeDamage);
		REG_PARENT(PointModificator);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//
class UnitAbility;
//class BaseClassUnitType;
//
//==================================================================================================================//
class BirthNewUnit : public PointModificator
{
public:
	BaseClassUnitType UT;
	UserFriendlyNumericalReturner UnitLife;
	ClonesArray< ClassRef<UnitAbility> > AdditionalAbilites;
	SAVE(BirthNewUnit);
		REG_PARENT(PointModificator);
		REG_AUTO(UT);
		REG_AUTO(UnitLife);
		REG_AUTO(AdditionalAbilites);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
};
//==================================================================================================================//
class BirthNewUnitsFromSprites : public PointModificator
{
public:
	BirthNewUnitsFromSprites();
	BaseClassUnitType UT;
	UserFriendlyNumericalReturner MaxUnits;
	UserFriendlyNumericalReturner Radius;
	bool DeleteSprites; 
	UserFriendlyNumericalReturner UnitLife;
	ClonesArray< ClassRef<UnitAbility> > AdditionalAbilites;
	SAVE(BirthNewUnitsFromSprites);
		REG_PARENT(PointModificator);
		REG_AUTO(UT);
		REG_AUTO(MaxUnits);
		REG_AUTO(Radius);
		REG_MEMBER(_bool,DeleteSprites);
		REG_AUTO(UnitLife);
		REG_AUTO(AdditionalAbilites);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);

	static bool CheckSprite(OneSprite* OS,void* Param);

};
//==================================================================================================================//
class ChangeNation : public PointModificator
{
public:
	ChangeNation();
	int FromNI;
	bool AnyEnemyNation;
	bool AnyFriendlyNation;
	int ToNI;
	int Radius;
	int NUnits;	
	bool OnlyTargetUnit;
	ClonesArray<BaseClassUnitType> TypeList;
	SAVE(ChangeNation);
		REG_PARENT(PointModificator);
		REG_MEMBER(_int,FromNI);
		REG_MEMBER(_bool,AnyEnemyNation);
		REG_MEMBER(_bool,AnyFriendlyNation);
		REG_MEMBER(_int,ToNI);
		REG_MEMBER(_int,Radius); 
		REG_MEMBER(_int,NUnits);
		REG_MEMBER(_bool,OnlyTargetUnit);
		REG_AUTO(TypeList);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);
	static bool ChangeObjectNation(OneObject* OB,void* param);
};
//==================================================================================================================//
class PlaySomeSound : public PointModificator
{
public:
	PlaySomeSound(){SoundID=-1;};
	int SoundID;
	SAVE(PlaySomeSound);
	REG_PARENT(PointModificator);
	REG_ENUM(_index,SoundID,ALL_SOUNDS);
	ENDSAVE;
	virtual bool MakeOneStep(WeaponParams* WP);	
};
class True : public WeaponEvent
{
public:
	SAVE(True); 
		REG_PARENT(WeaponEvent);
	ENDSAVE;

	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//
class IsTargetDie : public WeaponEvent
{
public:
	SAVE(IsTargetDie); 
		REG_PARENT(WeaponEvent);
	ENDSAVE;

	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//
class IsTargetInvisible : public WeaponEvent
{
public:
	IsTargetInvisible();
	bool Not;
	SAVE(IsTargetInvisible); 
		REG_PARENT(WeaponEvent);
		REG_MEMBER(_bool,Not);
	ENDSAVE;

	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//
class Conditions : public WeaponEvent
{
public:
	Conditions();
	int LifeTimeMore;
	int LifeTimeLess;
	int TraveledDistanceMore;
	int TraveledDistanceLess;
	int RemainderDistanceMore;
	int RemainderDistanceLess;
	int HeightMore;
	int HeightLess;
	bool IsInBuilding;
	int AbsHeightMore;
	int AbsHeightLess;
	SAVE(Conditions);
		REG_PARENT(WeaponEvent);
		REG_MEMBER(_int,LifeTimeMore);
		REG_MEMBER(_int,LifeTimeLess);
		REG_MEMBER(_int,TraveledDistanceMore);
		REG_MEMBER(_int,TraveledDistanceLess);
		REG_MEMBER(_int,RemainderDistanceMore);
		REG_MEMBER(_int,RemainderDistanceLess);
		REG_MEMBER(_int,HeightMore);
		REG_MEMBER(_int,HeightLess);
		REG_MEMBER(_bool,IsInBuilding);
		REG_MEMBER(_int,AbsHeightMore);
		REG_MEMBER(_int,AbsHeightLess);
	ENDSAVE;

	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//
class TargetReached : public WeaponEvent
{
public:
	TargetReached(){
		EarthOrWaterReached=true;
		TargetPointReached=true;
		TargetPointDistance=30;
		IsInsideBuilding=true;
	}
	bool EarthOrWaterReached;
	bool TargetPointReached;
	int  TargetPointDistance;
	bool IsInsideBuilding;
	SAVE(TargetReached);
		REG_PARENT(WeaponEvent);
		REG_MEMBER(_bool,EarthOrWaterReached);
		REG_MEMBER(_bool,TargetPointReached);
		SAVE_SECTION(2);
		REG_MEMBER(_int,TargetPointDistance);
        SAVE_SECTION(1);
		REG_MEMBER(_bool,IsInsideBuilding);
	ENDSAVE;
	DWORD GetClassMask(){
		return TargetPointReached?3:1;
	}
	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//
class OR_several_events : public WeaponEvent
{
public:
	ClassArray<WeaponEvent> OR_events;
	
	SAVE(OR_several_events);
		REG_PARENT(WeaponEvent);
		REG_AUTO(OR_events);
	ENDSAVE;
	virtual bool Check(WeaponParams* WP){
		for(int i=0;i<OR_events.GetAmount();i++){
			if(OR_events[i]->Check(WP))return true;
		}
		return false;
	}
};
//==================================================================================================================//
class AND_several_events : public WeaponEvent
{
public:
	ClassArray<WeaponEvent> AND_events;

	SAVE(AND_several_events);
		REG_PARENT(WeaponEvent);
		REG_AUTO(AND_events);
	ENDSAVE;
	virtual bool Check(WeaponParams* WP){
		for(int i=0;i<AND_events.GetAmount();i++){
			if(!(AND_events[i]->Check(WP)))return false;
		}
		return true;
	}
};
//==================================================================================================================//
class IsFirstStep : public WeaponEvent
{
public:
	IsFirstStep();
	bool Not;
	SAVE(IsFirstStep); 
		REG_PARENT(WeaponEvent);
		REG_MEMBER(_bool,Not);
	ENDSAVE;

	virtual bool Check(WeaponParams* WP);
};
//==================================================================================================================//

//==================================================================================================================//
extern WeaponSystem GameWeaponSystem;
#pragma pack ( pop )


#endif