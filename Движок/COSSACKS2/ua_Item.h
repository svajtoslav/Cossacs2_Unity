#pragma once
//#include "UnitAbility.h"
#include "HeroAbility.h"
//==================================================================================================================//
class NewMonster;
//==================================================================================================================//
class NewItem: public MagicCard{
public:
	virtual bool OnUnitBirth(OneObject* Newbie);
	int Amount;
	SAVE(NewItem){
		REG_BASE(UnitAbility);
		REG_MEMBER(_int,Amount);
		REG_PARENT(MagicCard);
	}ENDSAVE;
};
class OneItem: public MagicCardActive{
private:
	bool Droped;
public:
	bool Deleted;
public:
	OneItem(){
		TypeAbil=4;
		Droped=false;
	}
	virtual bool Process();
	virtual bool OnRightClick();
	//
	NewMonster* NM;
	word NIndex; // newmonster of item OneObject
	//
	SAVE(OneItem){
		REG_PARENT(MagicCardActive);
	}ENDSAVE;
};
//
class NewMagazine: public UnitAbility{
public:
	virtual bool OnUnitBirth(OneObject* Newbie);
	SAVE(NewMagazine){
		REG_PARENT(UnitAbility);
	}ENDSAVE;
};
class OneMagazine: public ActiveUnitAbility{
public:
	OneMagazine(){
		TypeAbil=2;
	}
	virtual bool Process(OneObject* OB);
	//
	word HeroID;
	word HeroSN;
	//
	SAVE(OneMagazine){
		REG_PARENT(ActiveUnitAbility);
		REG_MEMBER(_short,HeroID);
		REG_MEMBER(_short,HeroSN);
	}ENDSAVE;
};