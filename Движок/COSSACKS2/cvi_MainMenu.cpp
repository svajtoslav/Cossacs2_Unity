#include "stdheader.h"
#include ".\cvi_MainMenu.h"
//////////////////////////////////////////////////////////////////////////
cvi_MainMenu v_MainMenu;
cvi_InterfaceSystem v_ISys;
char* v_ISysXML="dialogs\\interface\\InterfaceSystem.xml";
cvi_FontStyle v_FontStyle;
char* v_FontStyleXML="dialogs\\interface\\FontStyles.xml";
//
int GetCross(){
    return v_MainMenu.Cross;
}
void SetCross(int Cross){
	v_MainMenu.Cross=Cross;
}
extern int RealLx;
extern int RealLy;
void ProcessScreen();
extern bool GameInProgress;
void UnPress();
void KeyTestMem();
CIMPORT void SetCDVolume(int Vol);
CIMPORT int GetCDVolume();
CIMPORT void StopPlayCD();
void PlayRandomTrack();
bool isFinishedMP3();
CEXPORT void SetCurPtr(int v);
//
void InitMenu(){
	v_MainMenu.Init();
}
cvi_MainMenu::cvi_MainMenu(){ 
	Enable=false;
	GameMode=false;
	ModalDesk="Main";
	Cross=0;
}
//
bool EnableRestoreMusic=true;
//
void cvi_MainMenu::ProcessDSinGame(){
	if(GameMode&&vDS&&!Exit){
		vDS->SetWidth(RealLx);
		vDS->SetHeight(RealLy);
		if(Cross==1&&CrossTime==0){
			Cross=0;						
		}
		if(Cross==3){
			if(CrossSD)CrossSD->v_Actions.LeftClick(CrossSD);
			Cross=1;
			CrossTime=0;
			CrossSD=NULL;
		}
		if(Lpressed) LastLPress=GetTickCount();
		//
		vDS->ProcessDialogs();
		//GSets.CGame.ViewMask=251;
		//
		if(EnableRestoreMusic){
			if(GSets.SVOpt.MusicVolume!=GetCDVolume()){
				SetCDVolume(GSets.SVOpt.MusicVolume);
			}						
			extern bool RandomMode;
			if(RandomMode!=GSets.SVOpt.MusicPlayMode){
				void SetMusicMode(bool State);
				SetMusicMode(GSets.SVOpt.MusicPlayMode);
			}		
		}else{
			EnableRestoreMusic=true;
		}					
	}else{
		//Exit=true;
		GameMode=false;
		vDS=NULL;
		//GSets.CGame.ViewMask=255;
	}
};
void cvi_MainMenu::RestoreDSinGame(){
	Exit=false;
	GameMode=false;
	ModalDesk=vMD;	
	vDS=NULL;
}
bool cvi_MainMenu::StartDS(char* ID, bool gMode){
	void PerformCheckCD();
	PerformCheckCD();
	if(ID==NULL||gMode&&GameMode){
		return false;
	}
	SetCurPtr(0);
	KeyPressed=0;
	LastKey=0;
	UnPress();	
	//
	vDS=NULL;
	extern bool GameExit;
	if( gMode && !GameMode && vGameMode==gmMul && !GameExit ){
		vMD=ModalDesk;
		ModalDesk="Main";
		GameMode=gMode;
		Exit=false;
		DWORD DID=Dialogs->Get(ID);
		if(DID!=0xFFFFFFFF){
			vDS=dynamic_cast<DialogsSystem*>((DialogsSystem*)DID);
			extern bool NoPress;
			NoPress=false;
		}
		return false;
	}
	//
	Enable=true;
	if(Enable){		
		DWORD DID=Dialogs->Get(ID);
		if(DID!=0xFFFFFFFF){
			DialogsSystem* DS=NULL;
			if(Dialogs) DS=dynamic_cast<DialogsSystem*>((DialogsSystem*)DID);
			if(DS){
				vMD=ModalDesk;
				ModalDesk="Main";
				bool gM=GameMode;
				GameMode=gMode;
				//
				static int StopLoop=0;
				StopLoop=GetTickCount()+2500;
				//
				Exit=false;
				LastLPress=GetTickCount();
				extern bool NoPress;
				NoPress=false;
				//
				if(GameMode&&StopLoop<GetTickCount()){
					void StopLoopSounds();
					StopLoopSounds();
				}
				//
				while (!Exit) {
					if(GameMode&&StopLoop<GetTickCount()){
						//void StopLoopSounds();
						//StopLoopSounds();
					}
					DS->SetWidth(RealLx);
					DS->SetHeight(RealLy);
					if(GameMode){
						GSets.CGame.ViewMask=251;
						ProcessScreen();
					}
					ProcessMessages();
					if(Cross==1&&CrossTime==0){
						Cross=0;						
					}
					if(Cross==3){
						if(CrossSD)CrossSD->v_Actions.LeftClick(CrossSD);
						Cross=1;
						CrossTime=0;
						CrossSD=NULL;
					}
					if(Lpressed) LastLPress=GetTickCount();
					DS->ProcessDialogs();
					DS->RefreshView();
					//
					if(EnableRestoreMusic){
						if(GSets.SVOpt.MusicVolume!=GetCDVolume()){
							SetCDVolume(GSets.SVOpt.MusicVolume);
						}						
						extern bool RandomMode;
						if(RandomMode!=GSets.SVOpt.MusicPlayMode){
							void SetMusicMode(bool State);
							SetMusicMode(GSets.SVOpt.MusicPlayMode);
						}		
					}else{
						EnableRestoreMusic=true;
					}					
				}
				Exit=false;
				GameMode=gM;
				ModalDesk=vMD;
				GSets.CGame.ViewMask=255;
				return true;
			}
		}
	}
	return false;
};
void cvi_MainMenu::SetModalDesk(char* str){
	if(str){
		ModalDeskBack=ModalDesk;
		ModalDesk=str;
	}	
};
void cvi_MainMenu::BackModalDesk(){
	if(ModalDeskBack.isClear()){
		ModalDesk="Main";
	}else{
		_str tmp;
		tmp=ModalDesk;
		ModalDesk=ModalDeskBack;
		ModalDeskBack=tmp;
	}
};
//
bool cva_M_CrossActivator::LeftClick(SimpleDialog* SD){
	if(v_MainMenu.Cross!=3){
		v_MainMenu.Cross=2;
		v_MainMenu.CrossTime=0;
		v_MainMenu.CrossSD=SD;
	}
	return true;
}
void cva_M_CrossFade::SetFrameState(SimpleDialog* SD){
	if(FadeTime==0) FadeTime=100;
	SD->Diffuse=0;
	if(v_MainMenu.Cross==1||v_MainMenu.Cross==2){		
		int Alpha;
		int Com;
		if(v_MainMenu.Cross==1){
			Alpha=0xFF*(FadeTime-v_MainMenu.CrossTime)/FadeTime;
			Com=0;
		}else{
			Alpha=0xFF*v_MainMenu.CrossTime/FadeTime;
			Com=3;
		}
		SD->Diffuse=byte(Alpha)*0x01000000;//+0x00FFFFFF;
		v_MainMenu.CrossTime++;
		if(v_MainMenu.CrossTime>FadeTime){
			v_MainMenu.Cross=Com;
		}
	}

}
//
void cvi_MainMenu::CloseDS(){
	Exit=true;
};
void addDStoEnum(Enumerator* E, const char* ID, const char* xmlFileName){
	DialogsSystem* DS=new DialogsSystem;
	if(DS->SafeReadFromFile(xmlFileName)){
		E->Add((char*)ID,(DWORD)DS);
	}else{
		delete DS;
	}
};
bool cvi_MainMenu::Init(){	
	dsLoading.SafeReadFromFile("dialogs\\mm\\M_Loading.DialogsSystem.xml");
	ShowLoadProgress("LoadMenu",0,1);
	xmlQuote cfg;
	char FileName[]="dialogs\\MainMenu.xml";
	if(!cfg.ReadFromFile(FileName)){
		//cfg.QuoteName.Assign("MainMenu");
		cfg.AddSubQuote("Main","dialogs\\v\\M_Main.DialogsSystem.xml");
		cfg.AddSubQuote("Single","dialogs\\v\\M_Single.DialogsSystem.xml");
		cfg.AddSubQuote("Options","dialogs\\v\\M_Options.DialogsSystem.xml");
		cfg.WriteToFile(FileName);
	};	
    //
	Enumerator* E=Dialogs=ENUM.Get("ve_MainMenuDialogs");
	for(int i=0;i<cfg.GetNSubQuotes();i++){
		ShowLoadProgress("LoadMenu",i,cfg.GetNSubQuotes());
		xmlQuote* sub=cfg.GetSubQuote(i);
		addDStoEnum(E,sub->GetQuoteName(),sub->Get_string());
	}
	//
	
	return true;
};
//
int cva_MM_LoadingProgress::Value=0;
void cvi_MainMenu::ClearLoadMark(){
	lMark.Clear();
	lMarkVal.Clear();
	lMarkValMax=0;
	cva_MM_LoadingProgress::Value=0;
};
void cvi_MainMenu::AddLoadMark(char* Mark, int Value){
	if(lMark.GetAmount()==0){
		lMarkValMax=0;
	}
	_str* M=new _str;
	M->Assign(Mark);
	lMark.Add(M);
	lMarkVal.Add(Value);
    lMarkValMax+=Value;
};
void cvi_MainMenu::ShowLoadProgress(char* Mark, int v, int vMax){
	static int time=0;
	if(GetTickCount()<time||!Mark||vMax==0) return;
	
    IRS->ClearDeviceTarget();
    IRS->ClearDeviceZBuffer();

	time=GetTickCount()+150;
	int val=0;
	bool find=false;
	for(int i=0;i<lMark.GetAmount();i++){
		if(*(lMark[i])==Mark){
			val+=lMarkVal[i]*v/vMax;
			find=true;
			break;
		}else{
			val+=lMarkVal[i];
		}
	}
	cva_MM_LoadingProgress::Value=0;
	if(!find){
		//if(Mark[0]) Log.Warning("Loading progress not find: (%s)", Mark);
		return;
	}else
	if(lMarkValMax){
		cva_MM_LoadingProgress::Value=val*100/lMarkValMax;
	}
	dsLoading.ProcessDialogs();
	//ShowString(0,0,Mark,&BigRedFont);
	dsLoading.RefreshView();
};
//
void ClearLoadMark(){
	v_MainMenu.ClearLoadMark();
}
bool CheckIfProgressActive(){
	return v_MainMenu.lMark.GetAmount();
}
void AddLoadMark(char* Mark, int Value){
	v_MainMenu.AddLoadMark(Mark,Value);
}
void ShowLoadProgress(char* Mark, int v, int vMax){
	v_MainMenu.ShowLoadProgress(Mark,v,vMax);
}
//
void cva_MM_LoadingProgress::SetFrameState(SimpleDialog* SD){
	ProgressBar* PB=dynamic_cast<ProgressBar*>(SD);
	if(PB){
		PB->Value=Value;
		if(ShowPercent){
			PB->Message.Clear();
			if(SD->DSS.GetAmount()==0){
				PB->Message.print("%d%%",Value);
			}
		}else{
			PB->Message=Mess;
		}
	}
}
// cva_GM_CloseButtons
void cva_GM_CloseButtons::SetFrameState(SimpleDialog* SD){
	SD->Visible = SD->Name=="Close";
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	if(vGameMode!=gmMul&&NATIONS[NI].VictState==1){
		SD->Visible = SD->Name=="Close&Restart";
	}
	if(vGameMode==gmCamp&&NATIONS[NI].VictState==2){
		SD->Visible = SD->Name=="Statistic";
	}
}
//
void cvi_InterfaceSystem::Init(){
	ChatTitleDy=1;
	ChatStringDy=10;
	NKillsForGuardian=300;
	GlobalBrigX=13;
	GlobalBrigY=51;
	GlobalBrigDx=67;
	bbMoraleDelta=4;
	bbMoraleSpeed=1;
	bbHideTime=3000;
	bbFade=250;
	CampaignNDone=2;
	CreditsLeft=0;
	CreditsRight=RealLx-1;
	CreditsTop=0;
	CreditsBottom=0;
	CreditsSpeed=70;
	CreditsLDis=25;
	CreditsFont=&BlackFont;
	LoadingTipsPrefix="#Tips";
	LoadingTipsAmount=5;
};
// cvi_FontStyle
void cvi_FontStyle::reset_class(void* DataPtr){
	BaseClass* B=(BaseClass*)DataPtr;
	B->BaseClass::reset_class(B);
	cvi_FontStyle* FS=dynamic_cast<cvi_FontStyle*>(B);
	if(FS){
	}
}