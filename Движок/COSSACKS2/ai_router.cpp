#include "stdheader.h"
#include "Extensions.h"
#include "CurrentMapOptions.h"
class OneScriptString:public BaseClass{
public:
	_str ScriptName;
	SAVE(OneScriptString);
		REG_FILEPATH(ScriptName,"*.ai.xml");
	ENDSAVE;
	bool ForceSimplification(){return true;}
};
class OneAI_route:public BaseClass{
public:
	OneAI_route(){
		Nation=0;
		CheckStartResources=false;
		StartResources=1000;
		LandType=0;
	}
	int Nation;
	bool CheckStartResources;
	int StartResources;
	int LandType;
	int VictoryCondition;
	ClonesArray<OneScriptString> AI_scripts;
	SAVE(OneAI_route);
		REG_ENUM(_index,Nation,NATIONS);
		REG_MEMBER(_bool,CheckStartResources);
		SAVE_SECTION(1);
		REG_MEMBER(_int,StartResources);
		SAVE_SECTION(0xFFFFFFF);
		REG_ENUM(_index,LandType,LANDTYPE);
		REG_ENUM(_index,VictoryCondition,AI_CheckVictoryCondition);
		REG_AUTO(AI_scripts);
	ENDSAVE;
	DWORD GetClassMask(){
		return CheckStartResources?0xFFFFFFFF:0xFFFFFFFE;
	}
	const char* GetThisElementView(const char* LocalName){
		char* s=GetGlobalBuffer();
		s[0]=0;
		Enumerator* EN=ENUM.Get("NATIONS");
		Enumerator* EL=ENUM.Get("LANDTYPE");
		if(EN&&EL){
			if(CheckStartResources)sprintf(s,"{CR}%s{CB} res=%d{CY} Land=%s",EN->Get(Nation),StartResources,EL->Get(LandType));
			else sprintf(s,"{CR}%s{CB} res=ANY{CY} Land=%s",EN->Get(Nation),EL->Get(LandType));
			return s;
		}
		return NULL;
	}
};
ClonesArray<OneAI_route> AI_Router;
void RegAI_router(){
	REG_CLASS(OneScriptString);
    REG_CLASS(OneAI_route);
	AI_Router.SafeReadFromFile("ai\\router.xml");
	AddStdEditor("AI router",&AI_Router,"ai\\router.xml",RCE_DEFAULT);
}
char* GetBestAI(int NI,char* mapname){
	bool IsIsl=false;
	for(int i=0;i<7;i++)if(i!=NI&&CITY[i].MyIsland!=0xFF){
		if(CITY[i].MyIsland!=CITY[NI].MyIsland)IsIsl=true;
	}
	int Nat=-1;
	for(int j=0;j<MAXOBJECT;j++){
		OneObject* OB=Group[j];
		if(OB&&OB->NNUM==NI&&!OB->Sdoxlo&&(OB->newMons->Peasant||OB->NewBuilding)){
			Nat=OB->Ref.General->NatID;
			break;
		};
	};
	int stres=XRESRC(NI,0);
	char* ais[512];
	int nai=0;
	for(int i=0;i<AI_Router.GetAmount();i++){
		OneAI_route* AR=AI_Router[i];
		if(AR){
			bool yes=AR->Nation==Nat;
			if(AR->CheckStartResources){
				yes&=stres==AR->StartResources;
			}
			if(AR->LandType){
				yes&=bool(AR->LandType-1)==IsIsl;
			}
			if(AR->VictoryCondition){
				VictoryConditionRoot* VT=MOptions.VictoryRule.Get();
				if(VT){
					const char* CC=VT->GetClassName();
					if(AR->VictoryCondition==1&&strcmp(CC,"VC_DestroyTownHalls"))yes=false;
					if(AR->VictoryCondition==2&&strcmp(CC,"VC_ScoreGame"))yes=false;
					if(AR->VictoryCondition==3&&strcmp(CC,"VC_CaptureGame"))yes=false;
					if(AR->VictoryCondition==4&&strcmp(CC,"VC_AnnihilateHero"))yes=false;
				}else yes=false;
			}
			if(yes){
				for(int i=0;i<AR->AI_scripts.GetAmount();i++){
					if(nai<512){
						ais[nai]=AR->AI_scripts[i]->ScriptName.str;
						nai++;
					}
				}
			}
		}
	}
	if(nai){
		int s=0;
		int L=strlen(mapname);
		for(int i=0;i<L;i++){
			s+=mapname[i];
		}
		s%=nai;
		return ais[s];
	}
	return NULL;
}