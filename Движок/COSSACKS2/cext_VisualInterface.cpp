#include "stdheader.h"
#include "GameExtension.h"
#include ".\cext_VisualInterface.h"
#include ".\cvi_HeroButtons.h"
#include ".\vui_Effects.h"
#include ".\cvi_campaign.h"
#include ".\cvi_singleplayerdata.h"
#include ".\vui_GlobalHotKey.h"
#include ".\cvi_market.h"
#include ".\cvi_Missions.h"
//////////////////////////////////////////////////////////////////////////
CIMPORT void GetDipSimpleBuildings(int &NDips, DIP_SimpleBuilding** &Dips);
bool GetObjectVisibilityInFog(int x,int y,int z,OneObject* OB);
CEXPORT UnitsGroup* GetUnitsGroup(GAMEOBJ* Group);
DLLEXPORT OneObject* GetOBJ(int Index);
DLLEXPORT bool GetZoneR(GAMEOBJ* Zone,int* R);
CEXPORT void DrawColoredCircle(int xc,int yc,int R0,int R1,DWORD LineColor1,DWORD LineColor2,DWORD FillColor1,DWORD FillColor2);
DIALOGS_API void PlaySound(char* Name,int x, int y);
DIALOGS_API void PlaySound(char* Name);
extern word NPlayers;
extern byte PlayGameMode;
//
bool SetlRadiusVisible=false;
int SetlRadiusID;
//
bool ShowSetlRadius(){
	if(SetlRadiusVisible){
		SetlRadiusVisible=false;
		DIP_SimpleBuilding** DSB;
		int NDips;
		GetDipSimpleBuildings(NDips,DSB);
		DIP_SimpleBuilding* CDSB=DSB[SetlRadiusID];
		UnitsGroup* UG=GetUnitsGroup(&CDSB->CentralGroup);
		OneObject* OB=GetOBJ(UG->IDS[0]);
		if(OB){	//&&(OB->Selected||(GetKeyState(VK_MENU)&0x8000))
			int r0,r1;
			GetZoneR(&CDSB->BigZone,&r0);
			GetZoneR(&CDSB->VeryBigZone,&r1);
			DrawColoredCircle(OB->RealX>>4,OB->RealY>>4,r0,0,0xFFFF2020,0xFFFFFFFF,0x2FFF2020,0x2FFFFFFF);
		}
	}	
	return false;
}
//
bool cva_VI_Setl::LeftClick(SimpleDialog* SD){
	DIP_SimpleBuilding** SBs;
	int NDips;
	GetDipSimpleBuildings(NDips,SBs);
	if(NDips&&SD->ID<NDips){
		DIP_SimpleBuilding* SB=SBs[SD->ID];
		if(SB){
			OneObject* OB=SB->GetMainObj();
			if(OB){
				void CmdSelObject(OneObject* OB);
				CmdSelObject(OB);
			}
		}
	}
	return true;
}
void cva_VI_Setl::SetFrameState(SimpleDialog* SD){
	if(!SD->Visible){
		return;
	}
	if(SD->MouseOver){
		SetlRadiusVisible=true;
		SetlRadiusID=SD->ID;
	}
	GPPicture* GP=dynamic_cast<GPPicture*>(SD);
	if(GP){
		if(EngSettings.vInterf.sprSettlementAni){
			int N=GPS.GPNFrames(GP->FileID);
			if(N){
				static int t=GetTickCount();
				int T=GetTickCount();
				int spr=((T-t)/80)%N;
				GP->SetSpriteID(spr);
			}
			GPPicture* gP=NULL;
			if(GP->DSS.GetAmount()==0){
				DialogsDesk* DD=new DialogsDesk;
				GP->AddDialog(DD);
				DD->SetWidth(1000);
				DD->SetHeight(1000);
				gP=DD->addGPPicture(NULL,0,0,EngSettings.Resource.File,0);				
			}else{
				gP=dynamic_cast<GPPicture*>(GP->DSS[0]->DSS[0]);
			}
			if(gP){
				gP->Setx(EngSettings.vInterf.sprSettlementAniX);
				gP->Sety(EngSettings.vInterf.sprSettlementAniY);
				DIP_SimpleBuilding** SBs;
				int NDips;
				GetDipSimpleBuildings(NDips,SBs);
				if(NDips&&GP->ID<NDips){
					DIP_SimpleBuilding* SB=SBs[GP->ID];
					if(SB){
						GP->Nation=SB->Owner;
						byte res=0xFF;
						for(int i=0;i<6;i++){
							if(SB->Produce[i]){
								res=i;
								break;
							}
						}
						if(res!=0xFF){
							gP->SetSpriteID(EngSettings.Resource.Sprite[res]);						
						}
					}
				}
			}
		}
	}
}
//
void cext_VisualInterface::OnDrawOnMapAfterTransparentEffects(){
	// settlement picture with hint
	if(EngSettings.vInterf.fSettlement==-1){
		EngSettings.vInterf.fSettlement=GPS.PreLoadGPImage("interf3\\f_icons");
		EngSettings.vInterf.sprSettlement=19;
	}
	DIP_SimpleBuilding** SBs;
	int NDips;
	GetDipSimpleBuildings(NDips,SBs);
	if(NDips){
		static DialogsSystem* dsSettl=new DialogsSystem;	
		for(int i=0;i<NDips;i++){
			GPPicture* GP=NULL;
			if(i>=dsSettl->DSS.GetAmount()){
				GP=new GPPicture;
				GP->FileID=EngSettings.vInterf.fSettlement;
				GP->SetSpriteID(EngSettings.vInterf.sprSettlement);
				dsSettl->DSS.Add(GP);
				vui_Action* A=new cva_VI_Setl;
				GP->v_Actions.Add(A);
			}else{
				GP=(GPPicture*)dsSettl->DSS[i];
			}
			GP->Visible=false;
			DIP_SimpleBuilding* SB=SBs[i];
			if(SB){
				int x,y;
				SB->GetCenter(x,y);
				int z=GetTotalHeight(x,y);			
				if(x>0&&GetObjectVisibilityInFog(x,y,z,NULL)){
					int dH=EngSettings.vInterf.SettlementSpriteHeight;
					OneObject* MainOB=SB->GetMainObj();
					if(MainOB){
						short N=MainOB->newMons->NBars;
						short* B=MainOB->newMons->Bars3D;
						dH=0;
						for(int i=0;i<N;i++){
							int H=B[i*5+4];
							if(H>dH){
								dH=H;
							}
						}
						//dH-=GPS.GetGPHeight(EngSettings.vInterf.fSettlement,EngSettings.vInterf.sprSettlement);					
					}
					Vector3D v = SkewPt(x,y,z+dH);
					WorldToScreenSpace(v);
					//!IMM->IsPointVisible(v)
					if(v.x>-200&&v.y>-200&&v.x<RealLx+200&&v.y<RealLy+200){
						//GPS.ShowGP(v.x,v.y,EngSettings.vInterf.fSettlement,EngSettings.vInterf.sprSettlement,SB->Owner);
						GP->Visible=true;
						GP->Setx(v.x);
						GP->Sety(v.y);
						//
						GP->ID=i;
						//GP->UserParam=y;
						//
						GP->Nation=SB->Owner;
						_str hint;
						static char* tidSettl=GetTextByID("#SettlHint");
						for(int i=0;i<6;i++){
							if(SB->Produce[i]){
								int r=SB->Resource[i];
								/*
								if(r>SB->CaravanCapacity[i]){
								r=SB->CaravanCapacity[i];
								}
								*/
								hint.print(tidSettl,RDS[i].Name,r,SB->CaravanCapacity[i]);
								break;
							}						
						}					
						GP->SetHint(hint.str);
					}
				}			
			}
		}
		for(;i<dsSettl->DSS.GetAmount();i++){
			dsSettl->DSS[i]->Visible=false;
		}
		dsSettl->ProcessDialogs();
	}
};
DialogsSystem cext_VisualInterface::Credits;
DialogsSystem* GetShowAboutDS(){
	return &cext_VisualInterface::Credits;
}
void cext_VisualInterface::OnDrawOnMapOverAll(){
	// Credits
	extern bool vCreditsMode;
	if(vCreditsMode){
		if(Credits.DSS.GetAmount()==0){
			//Credits.ReadFromFile("dialogs\\mm\\Credits.DialogsSystem.xml");
		}
		Credits.ProcessDialogs();
		void ShowAbout();
		ShowAbout();
		SetCurPtr(15);
	}
};
//
DLLEXPORT void ShowPage(char* Name);
CEXPORT bool isInCampaign();
void ACT(int x);
//
bool EndGameMessActive=false;
bool cext_VisualInterface::OnEndGameMessage(int NI,int VictStatus){
	//
	extern _str vm_F1_VictoryCond;
	vm_F1_VictoryCond.Clear();
	//
	GameExit=true;
	if(EndGameMessActive) return true;
	//
	VictoryConditionRoot* VCond=MOptions.VictoryRule.Get();		
	if(/*VCond&&*/NI==MyNation){ //&&cva_VI_EndGame::isStarted
		static DWORD ShowTime=0;
		if(cva_VI_EndGame::isStarted){		
			cva_VI_EndGame::isStarted=false;		
			if(VictStatus==2){			
				ACT(75);
			}else{
				ACT(76);
			}			
			GSets.CGame.ViewMask=2+4;
			ShowTime=GetTickCount()+15000;
		}
		GameExit=false;
		if(ShowTime<GetTickCount()||(GetKeyState(VK_ESCAPE)&0x8000)||!isInCampaign()){
			GameExit=true;
			cva_VI_EndGame::isStarted=true;	
			//
			cva_VI_EndGame::VictStatus=VictStatus;			
			//
			EndGameMessActive=true;
			//
			if(cva_VI_EndGame::UserMessage){
				for(int i=0;i<SCENINF.NPages;i++){
					if(!strcmp(cva_VI_EndGame::UserMessage,SCENINF.PageID[i])){				
						ShowPage(cva_VI_EndGame::UserMessage);
						cva_VI_EndGame::UserMessage=NULL;
						return true;
					}
				}
			}			
			if(v_MainMenu.StartDS("EndGameMessage",true)){
				cva_VI_EndGame::UserMessage=NULL;
			}
		}
		return true;
	}	
	return false;
}
//
DLLEXPORT int GetGlobalTime();
CEXPORT void AddPulseSquare(int x, int y);
//
void cext_VisualInterface::OnUnitBirth(OneObject* NewUnit){
	/*if(EngSettings.AllyNetralBuildings&&NewUnit->NNUM==7){		
		if(NewUnit->NewBuilding){
			NewUnit->NMask=0xFF;
		}else{
			NewUnit->NMask=0x80;
		}		
	}*/
	if(vHeroButtons.Add(NewUnit)){
		byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
		if(NewUnit->NNUM==NI&&GetGlobalTime()>15000){
			char ccc[200];
			sprintf(ccc,BLDBLD,NewUnit->Ref.General->Message);
			AssignHint1(ccc,100);
			LastActionX=NewUnit->RealX>>4;
			LastActionY=NewUnit->RealY>>4;
		}
	};
};
//
bool cext_VisualInterface::OnUnitCapture(OneObject* CapturedUnit,OneObject* Capturer){
	if(CapturedUnit){
		byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
		byte NMask=NATIONS[NI].NMask;
		if(CapturedUnit->newMons->PortBranch==2){
			if(CapturedUnit->NNUM==NI){
				ACT(96);
			}else
			//if(!(CapturedUnit->NMask&NMask)){
			if(Capturer&&Capturer->NNUM==NI){
				ACT(95);
			}
		}else
		if(CapturedUnit->NewBuilding){
			if(CapturedUnit->NNUM==NI){
				static char* txt=GetTextByID("#MyBldCaptured");
				_str t;
				t.print(txt,CapturedUnit->newMons->Message);
				AssignHint1(t.str,100);
			}else
			if(Capturer&&Capturer->NNUM==NI){
				static char* txt=GetTextByID("#EnemyBldCaptured");
				_str t;
				t.print(txt,CapturedUnit->newMons->Message);
				AssignHint1(t.str,100);
			}
		}
	}
	return true;
};
//
bool cext_VisualInterface::OnUnitDie(OneObject* Dead,OneObject* Killer){
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	if(Dead&&Killer){
		if(Dead->newMons->PortBranch==2){			
			byte NMask=NATIONS[NI].NMask;
			if(Dead->NNUM==NI){
				ACT(94);
			}else
			if(!(Dead->NMask&NMask)){
				ACT(93);
			}
		}
	}
	return true;
};
//
int NextDamageAlert=0;
//
bool cext_VisualInterface::OnUnitDamage(OneObject* DamagedUnit,OneObject* Damager,int& Damage){
	if(Damager&&GSets.DisableFriendlyFire&&NPlayers==1&&(DamagedUnit->NMask&Damager->NMask)&&CITY[Damager->NNUM].Difficulty<2){
		return false; //&&Damage>0
	}
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	if(!PlayGameMode&&DamagedUnit->NNUM==NI&&Damager){
		byte myNMask=NATIONS[NI].NMask;
		byte enNMask=NATIONS[Damager->NNUM].NMask;
		// enemy make damage
		if(!(myNMask&enNMask)){			
			vHeroButtons.checkDamage(DamagedUnit);
			if(NextDamageAlert<TrueTime&&DamagedUnit->BrigadeID!=0xFFFF){
				int myPortBranch=DamagedUnit->newMons->PortBranch;
				int enPortBranch=Damager->newMons->PortBranch;
				if(Damager->BrigadeID!=0xFFFF||enPortBranch==2){
					int x=DamagedUnit->RealX>>4;
					int y=DamagedUnit->RealY>>4;
					Vector3D point(x,y,GetHeight(x,y));
					if(!IMM->IsPointVisible(point)){
						NextDamageAlert=TrueTime+20000;
						AddPulseSquare(x,y);
						//
						int dist=Norma(DamagedUnit->RealX-Damager->RealX,DamagedUnit->RealY-Damager->RealY)>>4;						
						bool melee = (dist<80); // AttType
						//
						int act=61;						
						if(enPortBranch==2){
							// cannon
							act=63;
						}else{
							if(myPortBranch==0&&enPortBranch==1){
								// cavalery
								if(melee){
									act=64;
								}
							}else{
								if(melee){
									act=62;
								}
							}
						}
						ACT(act);
					}
				}
			}
		}
	}
	if(DamagedUnit->NNUM==NI&&DamagedUnit->Usage==CenterID&&Damager&&Damager->NNUM!=NI/*&&Damager->BrigadeID!=0xFFFF*/){
		VC_DestroyTownHalls* VC=dynamic_cast<VC_DestroyTownHalls*>(MOptions.VictoryRule.Get());
		if(VC){
			void fes_neTownHall(OneObject* TH);
			fes_neTownHall(DamagedUnit);
		}
	}
	return true;
}
//////////////////////////////////////////////////////////////////////////
extern bool vIGPanelMode;
extern int LastActionX;
extern int LastActionY;
extern int LastActionTime;
//
bool vGameLoaing=false;
bool vFirstHelp=false;
//
void cext_VisualInterface::OnUnloading(){
	EndGameMessActive=false;
	//
	extern bool vTaskCII;
	vTaskCII=false;
	//
	extern int NextDamageAlert;
	NextDamageAlert=0;
	//
	void AlertOnUnloading();
	AlertOnUnloading();
	//
	GSets.CGame.SilenceMessageEvents=false;
	//
	void fes_Init();
	fes_Init();
	//
	vHeroButtons.SetVisible(true);
	void BrigPanelShowAll();
	BrigPanelShowAll();
	//	
	v_MainMenu.Cross=0;
	vGameLoaing=false;
	vIGPanelMode=false;
	vHeroButtons.Clear();
	//
	extern byte LockGrid;
	extern bool TransMode;
	extern bool HealthMode;
	//
	LockGrid=false;
	TransMode=false;
	HealthMode=true;
	// ai
	aiI.Unload();
	// market
	//cvi_Market* vMarket;
	///if(vMarket) vMarket->Load();
	vMarket=&vMarketInGame;
	//cvi_Market vMarketInGame;
	vMarketInGame.Load();
	//
	cva_VI_EndGame::isStarted=true;
	cva_VI_EndGame::VictStatus=0;
	cva_VI_EndGame::UserMessage=NULL;
	//
	LastActionX=0;
	LastActionY=0;
	LastActionTime=0;
	//
	cva_VI_EndGame::isStarted=true;	
	//
	vFirstHelp=false;
	//
	DialogsSystem* DS=&VI_Zone;
	int n=DS->DSS.GetAmount();
	for(int i=0;i<n;i++){
		GPPicture* GP=dynamic_cast<GPPicture*>(DS->DSS[i]);
		if(GP){
			GP->Visible=false;
		}
	}
};
//
void cext_VisualInterface::OnInitAfterMapLoading(){
};
//
DialogsSystem cext_VisualInterface::PulseSquare;
CEXPORT void AddPulseSquare(int x, int y){
	static bool NoXmlFile=false;
	if(NoXmlFile) return;
	GPPicture* Pulse=NULL;
	DialogsSystem* DS=&cext_VisualInterface::PulseSquare;
	int n=DS->DSS.GetAmount();
	for(int i=0;i<n;i++){
		GPPicture* GP=dynamic_cast<GPPicture*>(DS->DSS[i]);
		if(GP){ //!GP->Visible
			int na=GP->v_Actions.GetAmount();
			for(int a=0;a<na;a++){
				vui_BasicEffect* BE=dynamic_cast<vui_BasicEffect*>(GP->v_Actions[a]);
				if(BE){
					if(BE->isFinished()){
						if(!Pulse){
							Pulse=GP;
							//break;
							//BE->StartEffect();			
						}
					}else{
						bool exist=Norma(GP->ID-x,GP->UserParam-y)<512;
						if(exist){
							return;
						}
					}
				}
			}
		}
	}
	if(!Pulse){
		Pulse=new GPPicture;
		if(Pulse->ReadFromFile("dialogs\\Alert.GPPicture.Dialogs.xml")){
			DS->DSS.Add(Pulse);
		}else{
			delete Pulse;
			NoXmlFile=true;
			Log.Warning("Not found file: dialogs\\Alert.GPPicture.Dialogs.xml");
			return;
		}
	}
	//LastActionX=x;
	//LastActionY=y;
	//LastActionTime=TrueTime;
	Pulse->ID=x;
	Pulse->UserParam=y;
	n=Pulse->v_Actions.GetAmount();
	for(i=0;i<n;i++){
		vui_BasicEffect* BE=dynamic_cast<vui_BasicEffect*>(Pulse->v_Actions[i]);
		if(BE){
            BE->StartEffect();			
		}
	}	
}
extern int AnimTime;
class CPulseSquarePoint
{
public:
	CPulseSquarePoint(int xx, int yy)
	{
		x=xx;
		y=yy;
		birthTime=AnimTime;
	};
	int x;
	int y;
	int birthTime;
};
class CPulseSquarePointStorage
{
public:
	CPulseSquarePointStorage() {};
	~CPulseSquarePointStorage() 
	{
		int n=Points.GetAmount();
		for(int i=0;i<n;i++)
		{
			if(Points[i])
			{
				delete (Points[i]);
				Points[i]=NULL;
			}
		}
		Points.Clear();
	};
	DynArray<CPulseSquarePoint*> Points;
	void AddPoint(int x, int y)
	{
		Points.Add( new CPulseSquarePoint(x, y));
	}
	bool CanAlarm(int x, int y, int DeadRadius, int actulAnimTime)
	{
		int n=Points.GetAmount();
		for(int i=0;i<n;i++)
		{
			CPulseSquarePoint* p=Points[n-1-i];
			if(p&&(AnimTime-p->birthTime)<actulAnimTime)
			{
				if(Norma(p->x-x,p->y-y)<DeadRadius)
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}
		return true;
	}
};
CPulseSquarePointStorage PulseSquarePointStorage;
int ActCommand=0;
bool AddPulseSquare(int x, int y, int deadRadius, int actulAnimTime)
{
	if(PulseSquarePointStorage.CanAlarm(x, y, deadRadius, actulAnimTime))
	{
		PulseSquarePointStorage.AddPoint(x, y);
		AddPulseSquare(x, y);
		void ACT(int x);
		//ACT(ActCommand);
		return true;
	}
	return false;
}
//
DialogsSystem cext_VisualInterface::VI_Zone;
CEXPORT void Add_VI_Zone(int x, int y, DWORD color){
	static bool NoXmlFile=false;
	if(NoXmlFile) return;
	GPPicture* Pulse=NULL;
	DialogsSystem* DS=&cext_VisualInterface::VI_Zone;
	int n=DS->DSS.GetAmount();
	for(int i=0;i<n&&!Pulse;i++){
		GPPicture* GP=dynamic_cast<GPPicture*>(DS->DSS[i]);
		if(GP){ //!GP->Visible
			bool find=Norma(GP->ID-x,GP->UserParam-y)<256;
			int na=GP->v_Actions.GetAmount();
			for(int a=0;a<na;a++){
				vui_BasicEffect* BE=dynamic_cast<vui_BasicEffect*>(GP->v_Actions[a]);
				if(BE){
					if(BE->isFinished()){
						if(!Pulse){
							Pulse=GP;
							find=false;
							break;
							//BE->StartEffect();			
						}
					}
				}
			}
			if(find){
				GP->Diffuse=color;
				GP->Visible=true;
				return;
			}
		}
	}
	if(!Pulse){
		Pulse=new GPPicture;
		if(Pulse->ReadFromFile("dialogs\\VI_Zone.GPPicture.Dialogs.xml")){
			DS->DSS.Add(Pulse);
		}else{
			delete Pulse;
			NoXmlFile=true;
			Log.Warning("Not found file: dialogs\\VI_Zone.GPPicture.Dialogs.xml");
			return;
		}
	}
	Pulse->ID=x;
	Pulse->UserParam=y;
	Pulse->Diffuse=color;
	Pulse->Visible=true;
	n=Pulse->v_Actions.GetAmount();
	for(i=0;i<n;i++){
		vui_BasicEffect* BE=dynamic_cast<vui_BasicEffect*>(Pulse->v_Actions[i]);
		if(BE){
            BE->StartEffect();			
		}
	}	
}
//
int BrigMiniBoxX=-1;
int BrigMiniBoxY;
void ShowBrigMiniBox(int x, int y){
	BrigMiniBoxX=x;
	BrigMiniBoxY=y;
};
//
CEXPORT int GetXOnMiniMap(int x,int y);
CEXPORT int GetYOnMiniMap(int x,int y);
void cext_VisualInterface::OnDrawOnMiniMap(int x,int y,int Lx,int Ly){
	vui_SelPoint* SP=OIS.GetLastSelPoint();
	if(SP||BrigMiniBoxX!=-1){
		if(SP&&BrigMiniBoxX==-1){
			BrigMiniBoxX=SP->rX>>4;
			BrigMiniBoxY=SP->rY>>4;
		}
		static int f=GPS.PreLoadGPImage("interf3\\minimap");
		int spr=27;
		int px=GetXOnMiniMap(BrigMiniBoxX,BrigMiniBoxY)-GPS.GetGPWidth(f,spr)/2;
		int py=GetYOnMiniMap(BrigMiniBoxX,BrigMiniBoxY)-GPS.GetGPHeight(f,spr)/2;
		GPS.ShowGP(px,py,f,spr,MyNation);
		BrigMiniBoxX=-1;
	}
	//
	DialogsSystem* DS=&cext_VisualInterface::PulseSquare;
	int n=DS->DSS.GetAmount();
	for(int i=0;i<n;i++){
		GPPicture* GP=dynamic_cast<GPPicture*>(DS->DSS[i]);
		if(GP){ //!GP->Visible
			int px=GetXOnMiniMap(GP->ID,GP->UserParam);
			int py=GetYOnMiniMap(GP->ID,GP->UserParam);
			int w=GP->GetWidth();
			int h=GP->GetHeight();
			//GP->Setx((px>>6)-w/2+x);
			//GP->Sety((py>>6)-h/2+y);
			GP->Setx(px-w/2);
			GP->Sety(py-h/2);
		}
	}
	DS->ProcessDialogs();
	//
	DS=&cext_VisualInterface::VI_Zone;
	n=DS->DSS.GetAmount();
	for(i=0;i<n;i++){
		GPPicture* GP=dynamic_cast<GPPicture*>(DS->DSS[i]);
		if(GP){ //!GP->Visible
			int px=GetXOnMiniMap(GP->ID,GP->UserParam);
			int py=GetYOnMiniMap(GP->ID,GP->UserParam);
			int w=GP->GetWidth();
			int h=GP->GetHeight();
			//GP->Setx((px>>6)-w/2+x);
			//GP->Sety((py>>6)-h/2+y);
			GP->Setx(px-w/2);
			GP->Sety(py-h/2);
		}
	}
	DS->ProcessDialogs();
}
void cext_VisualInterface::OnClassRegistration(){
	//
	REG_CLASS(cvi_GPFile);
	REG_CLASS(cvi_NatPics);	
	//
	void vNewProfInit();
	vNewProfInit();
	//
	REG_CLASS(cvi_InterfaceSystem);
	REG_CLASS(cvi_FontStyle);	
	REG_CLASS(cvi_Point);
	REG_CLASS(cvi_Missions);	
	REG_CLASS(ClonesArray<cvi_Missions>);
	//
	REG_CLASS(cvi_MissionFrame);
	REG_CLASS(cvi_mfGame);
	//REG_CLASS(cvi_mfStatistic);
	REG_CLASS(cvi_mfMovie);
	REG_CLASS(cvi_Fresco);
	REG_CLASS(cvi_mfFrescos);
	REG_CLASS(cvi_ChangeDifficulty);
	REG_CLASS(cvi_Mission);
	REG_CLASS(cvi_Campaign);
	REG_CLASS(ClassRef<cvi_mfMovie>);
	// Init
	vmCampaigns.ReadFromFile(vmCampaignXML);
	vmPlayerData.Read();	
	extern _str PlayerName;
	PlayerName=vmSinglePlayerData.PlayerName;
	// GameSettings
	GSets.gpRally=GPS.PreLoadGPImage("Interf3\\exitpoint");
	GSets.gpMove=GPS.PreLoadGPImage("Interf3\\moveon");
	//
	REG_CLASS(cvi_HeroButtons);
	//
	v_GlobalHotKeys.ReadFromFile(v_GlobalHotKeysXML);
	v_ISys.ReadFromFile(v_ISysXML);
	v_FontStyle.ReadFromFile(v_FontStyleXML);
	if(v_FontStyle.Font!=0){
		v_FontStyle.SetFonts.EvaluateFunction();
	}
	//
	EW2_Missions.ReadFromFile(EW2_MissionsXML);
	//
	void ArcadeModeDDLoad();
	ArcadeModeDDLoad();
	// Credits
	Credits.ReadFromFile("dialogs\\mm\\Credits.DialogsSystem.xml");
	//
	int vgf_UI_Process();
	vgf_UI_Process();
}
void cext_VisualInterface::ProcessingGame(){
	//
	void AlertBrigProcess();
	AlertBrigProcess();
	//
	extern int tmtmt;
	if(tmtmt<100)v_MainMenu.Cross=0;
	//
	if(EngSettings.vInterf.StartHelp&&!vFirstHelp){
		vFirstHelp=true;
		v_MainMenu.StartDS("F1game",true);
	}
	// ai
	aiI.Process();
	return;
	// check ImSellected accordance
	//if(GetRND(20)!=16) return;
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB&&!OB->Sdoxlo){
			for(byte ni=0;ni<8;ni++){
				if(OB->ImSelected&(1<<ni)){
					int n=ImNSL[ni];
					word* id=ImSelm[ni];
					word* sn=ImSerN[ni];
					bool exist=false;
					for(int j=0;j<n;j++){
						if(id[j]==OB->Index&&sn[j]==OB->Serial){
							exist=true;
							break;
						}
					}
					if(n&&!exist){
						Log.Error("Wrong ImSelected=%d in object id=%d, ni=%d",OB->ImSelected,OB->Index,OB->NNUM);
					}
				}
			}

		}
	}
	
};
//
void cext_VisualInterface::OnBrigadeCreated(Brigade* BR){
	Nation* NT=BR->CT->Nat;
	NT->NBrProduced[BR->MembID]++;
	//
	BR->M=0;
	BR->HideTime=0;
	BR->mmActive=0;
	//
	BR->Alert.Init=false;
};
void cext_VisualInterface::OnBrigadeKilled(Brigade* BR,byte KillerNation){
	if(BR->NMemb>40){
		byte NI=BR->CT->NI;
		//byte NMask=NATIONS[NI].NMask;
		byte MyNI=GSets.CGame.cgi_NatRefTBL[MyNation];
		//byte MyNMask=NATIONS[MyNI].NMask;
		if(NI!=7){
			if((NI==MyNI||KillerNation==MyNI)){
				bool CheckLastDeffenderEvent(Brigade* BR);
				int x,y;
				if(CheckLastDeffenderEvent(BR)&&BR->GetCenter(&x,&y)){
					if(NI==MyNI){
						ACT(121);
						AddPulseSquare(x,y);
					}else{
						ACT(122);
						AddPulseSquare(x,y);
					}
				}else{
					void fes_neErasedBrigade(byte BrigadeNI);
					fes_neErasedBrigade(NI);
				}
			}
		}
	}
}
//
bool cext_VisualInterface::OnCheatEntering(const char* Cheat){
	if(!strcmp(Cheat,"ErasePeasants")){
        void EraseAllNetralPeasantsAndPolicemen();
		EraseAllNetralPeasantsAndPolicemen();
		AssignHint1("Erasing peasants and policemen complete",100);
		return true;;
	}
	return false;
};
//
extern int CurEW2miss;
extern _str vm_F1_VictoryCond;
CEXPORT char* GetIDByText(char* Text);
//
bool cext_VisualInterface::OnGameSaving(xmlQuote& xml){	
	//
	xmlQuote* xmlHB = new xmlQuote("vHeroButtons");
	vHeroButtons.Save(*xmlHB,&vHeroButtons);
	xml.AddSubQuote(xmlHB);
	//
	xmlQuote* xmlCI = new xmlQuote("Campaign Info");
	xmlCI->AddSubQuote("GameMode",vGameMode);
	if(vGameMode==gmCamp){
		xmlCI->AddSubQuote("CampaignID",vmCampID);
		xmlCI->AddSubQuote("MissionID",cva_Camp_StartMission::MissionID);
		if(vmCamp&&vmCamp->curMission>=0&&vmCamp->curMission<vmCamp->Missions.GetAmount()){
			xmlCI->AddSubQuote("curFrame",vmCamp->Missions[vmCamp->curMission]->curFrame);
		}
	}
	xml.AddSubQuote(xmlCI);
	//
	xmlQuote* xmlHI = new xmlQuote("spdHeroInfo");
	if(vGameMode==gmCamp){		
		if(vmCamp&&vmCamp->curMission>=0&&vmCamp->curMission<vmCamp->Missions.GetAmount()){
			SinglePlayerData_HeroesInfoList* HI=vmSinglePlayerData.Heri[vmCampID]->CampaignMissions[vmCamp->curMission];
			vHeroButtons.Save(*xmlHI,HI);
		}		
	}
	xml.AddSubQuote(xmlHI);
	//	
	xmlQuote* xmlEW2M = new xmlQuote("curEW2mission");
	xmlEW2M->AddSubQuote("CurEW2miss",CurEW2miss);
	xmlEW2M->AddSubQuote("SilenceMessageEvents",GSets.CGame.SilenceMessageEvents);
	xml.AddSubQuote(xmlEW2M);
	//
	xmlQuote* xmlEW2F1 = new xmlQuote("curEW2help");	
	if(!vm_F1_VictoryCond.isClear()){
		xmlEW2F1->AddSubQuote("vm_F1_VictoryCond",GetIDByText(vm_F1_VictoryCond.str));
	}
	xml.AddSubQuote(xmlEW2F1);
	return true;
};
bool cext_VisualInterface::OnGameLoading(xmlQuote& xml){
	if(vmCamp && vmCamp->curMission>=0 && vmCamp->curMission<vmCamp->Missions.GetAmount()){
		cvi_Mission* cM=vmCamp->Missions[vmCamp->curMission];
		cM->curFrame=cM->Scene.GetAmount()-1;
	}
	vGameLoaing=true;
	vmCamp=NULL;
	ErrorPager Error(1);
	//	
	if (xml.GetSubQuote(0)!=NULL){
		vHeroButtons.reset_class(&vHeroButtons);
		vHeroButtons.Load(*(xml.GetSubQuote(0)),&vHeroButtons,&Error);	
	}
	if (xml.GetSubQuote(1)!=NULL){
		//vValuesMap()->Load(*(xml.GetSubQuote(0)),vValuesMap(),&Error);
		xmlQuote* xmlCI = xml.GetSubQuote(1);
		if(xmlCI->GetNSubQuotes()>0){
			vGameMode=(veGameMode)xmlCI->GetSubQuote(0)->Get_int();
			if(vGameMode==gmCamp&&xmlCI->GetNSubQuotes()>2){
				v_MainMenu.ModalDesk="Campaign";
				vmCampID=xmlCI->GetSubQuote(1)->Get_int();
				vmCamp=vmCampaigns[vmCampID];
				vmCamp->curMission=xmlCI->GetSubQuote(2)->Get_int();
				cva_Camp_StartMission::MissionID=vmCamp->curMission;
				if(vmCamp && vmCamp->curMission>=0 && vmCamp->curMission<vmCamp->Missions.GetAmount()){
					vmCamp->Missions[vmCamp->curMission]->curFrame=xmlCI->GetSubQuote(3)->Get_int();
				}
			}
		}
	}
	if (xml.GetSubQuote(2)!=NULL && vmCamp){
		if( vmCampID < vmSinglePlayerData.Heri.GetAmount() &&
			vmCamp->curMission < vmSinglePlayerData.Heri[vmCampID]->CampaignMissions.GetAmount() ){
			SinglePlayerData_HeroesInfoList* HI=vmSinglePlayerData.Heri[vmCampID]->CampaignMissions[vmCamp->curMission];
			if(HI){
				HI->reset_class(HI);
				HI->Load(*(xml.GetSubQuote(2)),HI,&Error);
			}
		}
	}
	if (xml.GetSubQuote(3)!=NULL){
		xmlQuote* xmlEW2M=xml.GetSubQuote(3);
		if(xmlEW2M->GetNSubQuotes()>0){
			CurEW2miss=xmlEW2M->GetSubQuote(0)->Get_int();
		}
		if(xmlEW2M->GetNSubQuotes()>1){
			GSets.CGame.SilenceMessageEvents=xmlEW2M->GetSubQuote(1)->Get_int();
		}
	}
	vm_F1_VictoryCond.Clear();
	if (xml.GetSubQuote(4)!=NULL){
		xmlQuote* xmlEW2F1=xml.GetSubQuote(4);
		if(xmlEW2F1->GetNSubQuotes()>0){
			char* id=(char*)xmlEW2F1->GetSubQuote(0)->Get_string();
			vm_F1_VictoryCond=GetTextByID(id);
		}
	}
	return true;
};
/////////////////////////////////////////////////////////////////////////////////////////////////
// actions
/////////////////////////////////////////////////////////////////////////////////////////////////
bool cva_VI_EndGame::isStarted=true;
int cva_VI_EndGame::VictStatus=0;
char* cva_VI_EndGame::UserMessage=NULL;
//
void cva_VI_EndGame::SetFrameState(SimpleDialog* SD){	
	if(isStarted){
		EndGameMessActive=true;
		isStarted=false;
		TextButton* dT=dTitle.Get();		
		TextButton* dM=dMessage.Get();
		VitButton* dB=dButton.Get();
		if(VictStatus==2){
			// victory
			if(dT) dT->SetMessage(VicTitle);
			if(dM){
				if(UserMessage) dM->SetMessage(UserMessage);
					else dM->SetMessage(VicMessage);
			}
			if(dB) dB->SetMessage(VicButton);
		}else{
			// defeat
			if(dT) dT->SetMessage(DefTitle);
			if(dM){
				if(UserMessage) dM->SetMessage(UserMessage);
					else dM->SetMessage(DefMessage);
			}
			if(dB) dB->Message=DefButton;
		}		
		GPPicture* dP=dPicture.Get();
		if(dP){
			dP->FileID=gpFile;
			if(VictStatus==2){
				dP->SetSpriteID(VicSprite);
			}else{
				dP->SetSpriteID(DefSprite);
			}
		}
	}
}
/// ai Base ///
aiInfo aiI;
CEXPORT bool CorrectPosition(byte NI, int& x, int& y){
	return aiI.Base[NI].CorrectPosition(x,y);
}
//
void aiBase::Unload(){
	Enable=false;
	cX=-1;
	cUnitX=-1;
	cR=3000;
	rDefenders=2000;
}
bool aiBase::CorrectPosition(int& x, int& y){	
	addrand(655);
	if(eR<cR&&cX!=-1&&eX!=-1){
		// enemy on the base
		if(Norma(cX-x,cY-y)<cR||Norma(eX-x,eY-y)<rDefenders){
			// correct position
			/*
			if(cUnitX!=-1){
				x=cX;
				y=cY;
			}
			*/
			x=eX;
			y=eY;
			addrand(432);
			return true;
		}
	}
	addrand(652);
	return false;
};
void aiBase::Process(){
	int cn=0;
	int cx=0;
	int cy=0;
	int cr=10000;
	eX=-1;
	eY=0;
	eR=10000;
	//
	addrand(325);
	for(int i=0;i<MAXOBJECT;i++){
		OneObject*OB=Group[i];
		if(OB&&!OB->Sdoxlo){
			if(OB->NNUM==NI){
				// my
				void TakeResLink(OneObject* OBJ);
				if(OB->LocalOrder&&OB->LocalOrder->DoLink==&TakeResLink){
					cn++;
					int x=OB->RealX>>4;
					int y=OB->RealY>>4;
					cx+=x;
					cy+=y;
					if(cX!=-1){
						int r=Norma(x-cX,y-cY);
						if(cr>r){
							cr=r;
							cUnitX=x;
							cUnitY=y;
						}
					}
				}
			}else
			if(OB->NMask&NATIONS[NI].NMask){
				// ally			
			}else{
				// enemy
				int x=OB->RealX>>4;
				int y=OB->RealY>>4;
				if(cX!=-1){
					int r=Norma(x-cX,y-cY);
					if(eR>r){
						eR=r;
						eX=x;
						eY=y;
					}
				}
			}
		}
	}
	//
	if(cn){
		cX=cx/cn;
		cY=cy/cn;
	}	
};
void aiInfo::Process(){
	int T=GetGlobalTime();
	if(T>RefreshTime){
		addrand(765);
		RefreshTime=T+MAXOBJECT+100;		
		Base[CurID].Process();
		CurID=(CurID+1)%8;
	}
};
void aiInfo::Unload(){
	RefreshTime=8000;
	CurID=0;
	for(int i=0;i<8;i++){
		Base[i].Unload();
		Base[i].NI=i;
	}
};
/// ai Base ///