#include "stdheader.h"
#include "EngineSettings.h"
//
LocalizationSettings LocSettings;
DWORD GetObjectsShadowColor()
{
    return EngSettings.ShadowsColor;
}

float GetCameraFactor()
{
    return EngSettings.CameraFactor;
}

bool ResourceSettings::SetHint(byte NI, int* Cost, _str* txt){
	if(EnableSprHint){
		txt->Clear();
		for(int i=0;i<6;i++){
			if(Cost[i]){
				txt->print("{G %d %d %d %d %d %d} ", File, SprHint[i], dx, dy, Lx, Ly);
				if(Cost[i]>XRESRC(NI,i)){
					txt->Add("{CR}");
				}else{
					txt->Add("{CB}");
				}
				txt->print("%d ",Cost[i]);				
			}
		}
		return true;
	}
	return false;
};

bool cHintTemplate::AddHotKey(_str* txt, int Key){
	if(!HotKey.isClear()){
		txt->print(HotKey.str,Key);
		return true;
	}
	return false;
};
//
void ACT(int x);
extern int TrueTime;
//
bool fes_bLivingPlaces=true;
int fes_tLivingPlaces=0;
int fes_aLivingPlaces=0;
void fes_neLivingPlaces(){
	if(EngSettings.vInterf.neLivingPlaces&&fes_bLivingPlaces){
		byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
		Nation* NT=NATIONS+NI;
		if(fes_tLivingPlaces<TrueTime||fes_aLivingPlaces<NT->NFarms){
			fes_aLivingPlaces=NT->NFarms;
			fes_tLivingPlaces=TrueTime+300000; //EngSettings.vInterf.neTime
			ACT(42);
			AssignHint1(EngSettings.vInterf.neLivingPlaces,160);
			fes_bLivingPlaces=false;
		}
	}
}
//
CEXPORT void AddPulseSquare(int x, int y);
//
int fes_tTownHall=0;
void fes_neTownHall(OneObject* TH){
	if(fes_tTownHall<TrueTime){
		fes_tTownHall=TrueTime+45000;
		ACT(113);
		static char* h=GetTextByID("#TownHallUnderAttack");
		AssignHint1(h,160);
		AddPulseSquare(TH->RealX>>4,TH->RealY>>4);
	}
}
//
int fes_tSklad=0;
void fes_neSklad(){
	if(EngSettings.vInterf.neSklad){
		if(fes_tSklad<TrueTime){
			fes_tSklad=TrueTime+EngSettings.vInterf.neTime;
			AssignHint1(EngSettings.vInterf.neSklad,160);
		}
	}
}
int fes_tResourceForProduce=0;
void fes_neResourceForProduce(char* UnitMess){
	if(EngSettings.vInterf.neResourceForProduce){
		if(fes_tResourceForProduce<TrueTime){
			fes_tResourceForProduce=TrueTime+EngSettings.vInterf.neTime;
			char txt[150];
			sprintf(txt,EngSettings.vInterf.neResourceForProduce,UnitMess);
			AssignHint1(txt,60);
		}
	}
}
bool fes_bAmunition[8192]; // alredy alarmed
int fes_tAmmunition=0;
void fes_neAmmunition(word NIndex, char* Message=NULL){
	if(EngSettings.vInterf.neAmmunition&&!fes_bAmunition[NIndex]){
		if(fes_tAmmunition<TrueTime){
			fes_tAmmunition=TrueTime+EngSettings.vInterf.neTime;
			ACT(40);
			if(!Message){
				Message=EngSettings.vInterf.neAmmunition;
			}
			AssignHint1(Message,160);			
			fes_bAmunition[NIndex]=true;
		}
	}
}
int fes_tSettlementCapture=0;
CIMPORT word GetNTribes();
CEXPORT word GetNTribes(byte Owner);
CEXPORT void fes_neSettlementCapture(byte AgressorNI){	
	if(fes_tSettlementCapture<TrueTime){
		fes_tSettlementCapture=TrueTime+EngSettings.vInterf.neTime;
		byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
		if(AgressorNI==NI){
			ACT(50);
			if(EngSettings.vInterf.neSettlementCaptured) AssignHint1(EngSettings.vInterf.neSettlementCaptured,100);
			LogBattle(NI,"^ST_CAPV^ %d.",GetNTribes(NI),GetNTribes());
		}else{
			ACT(51);
			if(EngSettings.vInterf.neSettlementLost) AssignHint1(EngSettings.vInterf.neSettlementLost,160);
			LogBattle(NI,"^ST_LOSV^ %d.",GetNTribes(NI),GetNTribes());
		}
	}
}
int fes_tErasedBrigade=0;
void fes_neErasedBrigade(byte BrigadeNI){	
	if(fes_tErasedBrigade<TrueTime){
		fes_tErasedBrigade=TrueTime+EngSettings.vInterf.neTime;
		byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
		if(BrigadeNI==NI){
			ACT(73);
			if(EngSettings.vInterf.neBrigadeLost) AssignHint1(EngSettings.vInterf.neBrigadeLost,160);
		}else{
			ACT(74);
			if(EngSettings.vInterf.neBrigadeTerminated) AssignHint1(EngSettings.vInterf.neBrigadeTerminated,160);
		}
	}
}
void fes_Init(){
	fes_tLivingPlaces=0;
	fes_aLivingPlaces=0;
	fes_tSklad=0;
	fes_tResourceForProduce=0;
	fes_tAmmunition=0;
	fes_tSettlementCapture=0;
	fes_tErasedBrigade=0;
	fes_tTownHall=0;
	//
	memset(fes_bAmunition,0,sizeof fes_bAmunition);
}
//
#define alstr(s,x) if(s==NULL){s=(char*)malloc(strlen(x)+1); strcpy(s,x);}
void engInterface::Init(){
	VolumeFullScreenBik=100;
	VolumeSoundEvents=100;
	VolumeMissDialogs=100;
	//
	fSettlement=-1;
	sprSettlementAni=true;
	SettlementSpriteHeight=120;
	neTime=8000;
	selBorder=0;
	selColor=0x801010EE; //0x80EBEBEB
	HintX=80;
	HintY=-225;
	svFrescoMusic=40;
	svFrescoVoice=70;
	//
	alstr(neLivingPlaces,"#neLivingPlaces");
	alstr(neSklad,"#neSklad");
	alstr(neResourceForProduce,"#neResourceForProduce");
	alstr(neAmmunition,"#neAmmunition");
	alstr(neSettlementCaptured,"#neSettlementCaptured");
	alstr(neSettlementLost,"#neSettlementLost");
	alstr(neBrigadeTerminated,"#neBrigadeTerminated");
	alstr(neBrigadeLost,"#neBrigadeLost");
}