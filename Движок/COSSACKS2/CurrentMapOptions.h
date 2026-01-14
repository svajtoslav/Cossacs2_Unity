#pragma once
//////////////////////////////////////////////////////////////////////////
#include <more_types.h>
#include "VictoryConditions.h"
//
class VictoryType:public ClassPtr<VictoryConditionRoot>{ //ClassArray<VictoryConditionRoot>{
public:
	/*
	virtual int GetExpansionRules(){
		if(!GetAmount())return 2;
		else return 0;
	}
	*/
};
//==================================================================================================================//
class PlayGameProcess : public BaseClass
{
public:
	SAVE(PlayGameProcess);
	ENDSAVE;
	virtual bool Process();
};
//------------------------------------------------------------------------------------------------------------------//
class PlayGameProcessList : public ClassArray<PlayGameProcess>
{
public:
	SAVE(PlayGameProcessList);
	ENDSAVE;
	virtual int GetExpansionRules();
	bool Process();
};
//------------------------------------------------------------------------------------------------------------------//
class StartTacticalAI : public PlayGameProcess
{
public:
	StartTacticalAI();
	int NI;
	SAVE(StartTacticalAI);
	REG_PARENT(PlayGameProcess);
	REG_MEMBER(_int,NI);
	ENDSAVE;
	virtual bool Process();
};
class PreviewBinkVideo : public PlayGameProcess
{
public:
	_str BinkFile;
	SAVE(PreviewBinkVideo){
		REG_PARENT(PlayGameProcess);
		REG_FILEPATH(BinkFile,".bik");		
	}ENDSAVE;
	virtual bool Process();
};
//==================================================================================================================//

class StartResources:public BaseClass{
public:
	int Wood;
	int Gold;
	int Stone;
	int Food;
	int Iron;
	int Coal;
	SAVE(StartResources);
	REG_MEMBER(_int,Wood);
	REG_MEMBER(_int,Gold);
	REG_MEMBER(_int,Stone);
	REG_MEMBER(_int,Food);
	REG_MEMBER(_int,Iron);
	REG_MEMBER(_int,Coal);
	ENDSAVE;
};
class MapPlayerInfo:public BaseClass{
public:
	StartResources StartRes;	
	bool AutoSelectBuildings;	
	_str CustomAI_Script;
	bool DontSelectPanicers;
	bool DontAffectFogOfWar;
	// vital
	bool DisableInSingle;
	bool DisableInMultiplayer;
	bool DisableNationSelect;
	byte Nation;				// in NatList
	bool DisableStrategicAI;	// razvitie, postroyka unitov
	bool LockTeam;
	int  Team;
	bool AutoTransferUnitsToNearestTeam;
	// vitya (for mission)	//  [3/3/2005]
	bool UseStartPos;		// Set start camera position in (cX,cY)
	int	 cX,cY;
	// vitay end

	SAVE(MapPlayerInfo);
	SAVE_SECTION(1);
		REG_MEMBER(_bool,DisableInSingle);
		REG_MEMBER(_bool,DisableInMultiplayer);
		REG_MEMBER(_bool,DisableStrategicAI);
		REG_AUTO(StartRes);
		REG_MEMBER(_bool,AutoSelectBuildings);
		REG_MEMBER(_bool,DontSelectPanicers);
		REG_MEMBER(_bool,DontAffectFogOfWar);
		REG_AUTO(CustomAI_Script);
		// vital		
		REG_MEMBER(_bool,DisableNationSelect);
	SAVE_SECTION(2);
		REG_ENUM(_byte_index,Nation,NationsName);
	SAVE_SECTION(1);
		REG_MEMBER(_bool,AutoTransferUnitsToNearestTeam);
		REG_MEMBER(_bool,LockTeam);
	SAVE_SECTION(4);
		REG_ENUM(_index,Team,TeamList);
		// vitya (for mission)	//  [3/3/2005]
	SAVE_SECTION(1);
		REG_MEMBER(_bool,UseStartPos);
	SAVE_SECTION(0x10);
		REG_MEMBER(_int,cX);
		REG_MEMBER(_int,cY);
		// vitya end
	ENDSAVE;
	
	DWORD GetClassMask(){
		DWORD M=1;
		if(DisableNationSelect) M|=2;
		if(LockTeam) M|=4;
		if(UseStartPos) M|=0x10;
		return M;
	}

};
class MapScriptReplace:public BaseClass{
public:
	_str OriginalNDS;
	_str CustomNDS;
	SAVE(MapScriptReplace);
	REG_AUTO(OriginalNDS);
	REG_AUTO(CustomNDS);
	ENDSAVE;
};
class ScriptRepList:public ClassArray<MapScriptReplace>{
public:
	virtual int GetExpansionRules(){
		return 1;
	}
};
class StartResScope:public BaseClass{
public:
	bool DisableTeams;
	MapPlayerInfo Player[MaxNatColors+1];
	int GetN_Single();
	int GetN_Multi();
	SAVE(StartResScope)
		REG_MEMBER(_bool,DisableTeams);
		for(int i=0;i<=MaxNatColors;i++){
			char cc[32];
			sprintf(cc,"Player%d",i);
			REG_AUTO_EX2(Player[i],cc);
		}
	ENDSAVE;
};
class cmo_Preview: public BaseClass{
public:
	cmo_Preview(){ Temp="Temp\\temp.bmp"; }
	_str Temp;
	bool Player[MaxNatColors+1];
	_picfile UserPicture;
	SAVE(cmo_Preview)
		for(int i=0;i<=MaxNatColors;i++){
			char cc[32];
			sprintf(cc,"Player%d",i);
			REG_MEMBER_EX2(_bool,Player[i],cc);
		}
		REG_AUTO(UserPicture);
	ENDSAVE;
};
//===============test===================
class Test1:public BaseClass{
public:
	AUTONEW(Test1);
};
class Test2:public BaseClass{
public:
	AUTONEW(Test2);
};
class Test12:public Test1,public Test2{
public:
	int x12;
	SAVE_EX(Test12,Test1);
	REG_PARENT(Test1);
	REG_PARENT(Test2);
	REG_MEMBER(_int,x12);
	ENDSAVE;
};
class Test11:public Test1{
public:
	int x11;
	SAVE(Test11);
	REG_PARENT(Test1);	
	REG_MEMBER(_int,x11);
	ENDSAVE;
};
class Test22:public Test2{
public:
	int x22;
	SAVE(Test22);
	REG_PARENT(Test2);	
	REG_MEMBER(_int,x22);
	ENDSAVE;
};
//======================================
class MapOptions:public BaseClass{
public:
	MapOptions(){
		for(int i=0;i<=MaxNatColors;i++){
			Players.Player[i].StartRes.Wood=5000;	Players.Player[i].UseStartPos=false;
			Players.Player[i].StartRes.Gold=5000;	Players.Player[i].UseStartPos=false;
			Players.Player[i].StartRes.Stone=5000;	Players.Player[i].UseStartPos=false;
			Players.Player[i].StartRes.Food=5000;	Players.Player[i].UseStartPos=false;
			Players.Player[i].StartRes.Iron=5000;	Players.Player[i].UseStartPos=false;
			Players.Player[i].StartRes.Coal=5000;	Players.Player[i].UseStartPos=false;
		}
	}
	//bool Opt;
	VictoryType VictoryRule;
	bool RandomizePlayersPositions;
	int  MaxPlayers;
	bool DontCheckForAlonePlayer;
	bool DontTellAboutLivingPlaces;
	bool DontAllowCityLife;
	StartResScope Players;
    ScriptRepList CustomNDS;
	PlayGameProcessList OnMissionStart;
	cmo_Preview Preview;
	//
	int GetDiff(){ 
		return CITY[MyNation].Difficulty; 
	}
	void SetDiff(int v){
		CITY[MyNation].Difficulty=v;
	}	
	INT_PROPERTY(MapOptions,GetDiff,SetDiff);
	//ClassPtr<Test1> CPT1;
	//ClassPtr<Test2> CPT2;
	SAVE(MapOptions);
		//-------------------
		REG_CLASS(Test1);
		REG_CLASS(Test2);
		REG_CLASS_AMBIGUOUS(Test12,Test1);
		REG_CLASS(Test11);
		REG_CLASS(Test22);
		//-------------------
		REG_CLASS(MapScriptReplace);
		//REG_MEMBER(_bool,Opt);
		REG_MEMBER(_bool,RandomizePlayersPositions);
		REG_MEMBER(_int,MaxPlayers);
		REG_MEMBER(_bool,DontCheckForAlonePlayer);
		REG_MEMBER(_bool,DontTellAboutLivingPlaces);
		REG_MEMBER(_bool,DontAllowCityLife);
		REG_AUTO(VictoryRule);
		REG_AUTO(Players);
		//SAVE_SECTION(2);
		REG_AUTO(CustomNDS);
		REG_AUTO(OnMissionStart);
		//REG_AUTO(CPT1);
		//REG_AUTO(CPT2);
		REG_AUTO(Preview);
		//
		REG_INTPROP(_int,Difficulty,GetDiff,SetDiff);
	ENDSAVE;
	/*DWORD GetClassMask(){
        if(Opt)return 0xFFFFFFFF;
		else return 1;
	}*/
};
extern MapOptions MOptions;

