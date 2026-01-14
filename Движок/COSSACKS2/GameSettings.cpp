#include "stdheader.h"
#include "BE_HEADERS.h"
#include "Pinger.h"

#define MinPing0 90

extern City CITY[8];
extern CCommCore IPCORE;
extern char PlName[64];
void CreateNationalMaskForMap(char* Name);
void PlayGame();
void PrepareGameMedia(byte myid,bool SaveNR);
extern bool GameExit;
byte vmDifficulty[8];
//
void ClearLoadMark();
void AddLoadMark(char* Mark, int Value);
void ShowLoadProgress(char* Mark, int v, int vMax);
//
void SetRoomOptions(char* s);
DWORD GetObjectsShadowQuality()
{
    return GSets.ShadowQuality;
}
extern word NPlayers;
void CurrentGameInfo::RunSingleplayerGame(byte Color){	
	NPlayers=1;	
	SetMyNation(Color);
	CreateNationalMaskForMap(cgi_CurrentMap);	
	PrepareGameMedia(0,0);
	GameExit=false;
	strcpy(PL_INFO[0].name,PlName);
	DriveMode()->StartMission();	
	for(int i=0;i<8;i++){		
		CITY[i].Difficulty=vmDifficulty[i];
	}	
	PlayGame();
	memset(vmDifficulty,0,sizeof vmDifficulty);
}
extern word NPlayers;
void RunInetStart(){
	extern bool UseGSC_Login;
	if(!UseGSC_Login)return;
	char* PLAYERS[8];
	int Profiles[8];
	char NAT[8][32];
	char* Nations[8];
	int Teams[8];
	int Colors[8];
	for(int i=0;i<NPlayers;i++){
		PLAYERS[i]=GSets.CGame.PL_INFO[i].name;
		sprintf(NAT[i],"%d",GSets.CGame.PL_INFO[i].NationID);//+48
		Nations[i]=NAT[i];
		Profiles[i]=GSets.CGame.PL_INFO[i].ProfileID;
		Teams[i]=GSets.CGame.PL_INFO[i].GroupID;
		Colors[i]=GSets.CGame.PL_INFO[i].ColorID;
	};
	StartGSCGame("",GSets.CGame.PL_INFO[0].MapName,NPlayers,Profiles,Nations,Teams,Colors);
	extern bool NeedToReportInGameStats;
	NeedToReportInGameStats=1;
	extern int LastTimeReport_tmtmt;
	LastTimeReport_tmtmt=0;
}
void CurrentGameInfo::RunHostGame(){
	IPCORE.DisableServer();
	IPCORE.CloseSession();	
	bool StartIGame(bool SINGLE);
	StartIGame(0);
	GameExit=false;
	CurState=3;
	RunInetStart();
	SetRoomOptions("");
	PlayGame();	
}
void CurrentGameInfo::RunClientGame(){	
	GameExit=false;
	CurState=3;
	RunInetStart();
	SetRoomOptions("");
	PlayGame();
}
extern int CurrentMaxPing[8];
extern int MaxPingTime;
extern DWORD MyDPID;
extern word NPlayers;
int GetMaxRealPing();
int GetRealTime();
bool PIEnumeratePlayers(PlayerInfo* PIN,bool DoMsg);
bool SendToAllPlayers(DWORD Size,LPVOID lpData);
void SETPLAYERDATA(DWORD ID,void* Data,int size,bool change);
CEXPORT
void SendPings();
void CurrentGameInfo::ProcessRoom(){
	if(CurState!=1&&CurState!=2)return;
	extern bool GameInProgress;
	if(GameInProgress)return;
	GSets.CGame.UnitTransform=true;
	bool Host=CurState==1;
	static PlayerInfo MYPINF;
	BEGIN_ONCE
	memset(&MYPINF,0,sizeof MYPINF);
	END_ONCE
	//analysing ping
	SendPings();
	PlayerInfo* PI=GSets.CGame.GetCurrentPlayerInfo();
	int GetMyProfile();
	if(PI)PI->ProfileID=GetMyProfile();
	if(Host){
		PlayerInfo* PI=GetCurrentPlayerInfo();
		if(PI){
			PI->Host=1;
			strcpy(cgi_CurrentMap,PI->MapName);
			DWORD GetMapHash(char* Name);
			PI->MapHashValue=GetMapHash(PI->MapName);
			PI->ProfileID=GetMyProfile();
			extern word COMPSTART[8];
			memcpy(PI->COMPINFO,COMPSTART,16);
		}
	}else{
		if(PI){
			PlayerInfo* SPI=GSets.CGame.GetHostPlayerInfo();
			if(SPI){
				extern word COMPSTART[8];
				memcpy(COMPSTART,SPI->COMPINFO,16);
			}
		}
	}
	for(int p=NPlayers;p<8;p++)CurrentMaxPing[p]=-1;
	if(PSUMM.NPL&&PSUMM.PSET[0].NPings){
		int png=GetMaxRealPing();
		static int PPTIME=GetTickCount()-1000;
		if(GetRealTime()-PPTIME>1000){
			DWORD lpp[3];
			lpp[0]='PNTF';
			lpp[1]=png;
			lpp[2]=lpp[0]+lpp[1];
			SendToAllPlayers(12,lpp);
			PPTIME=GetRealTime();
			for(p=0;p<NPlayers;p++)if(GSets.CGame.PL_INFO[p].PlayerID==MyDPID)CurrentMaxPing[p]=png;
		};
		for(p=0;p<NPlayers;p++)if(CurrentMaxPing[p]>png)png=CurrentMaxPing[p];
		MaxPingTime=png;
		if(png<MinPing0)MaxPingTime=0;
		if(png>=MinPing0&&png<200)MaxPingTime=200;
		if(MaxPingTime)MaxPingTime+=150;
		//_str cc2;
		//CreateDiffStr(cc2);
		//sprintf(ccc,"max ping: %d  (%s)",png,cc2.str);		
	}
	//analysing change of player data
	for(int i=0;i<8;i++){
		if(i<NPlayers){
			if(GSets.CGame.PL_INFO[i].PlayerID==MyDPID){
				bool ch=memcmp(&MYPINF,GSets.CGame.PL_INFO+i,sizeof MYPINF)!=0;
				static int PREVSD=GetTickCount()-3000;
				if(ch||GetTickCount()-PREVSD>3000){					
					SETPLAYERDATA(MyDPID,&PL_INFO[i].NationID,sizeof(PlayerInfo)-36,ch);					
					memcpy(&MYPINF,PL_INFO+i,sizeof MYPINF);
					PREVSD=GetTickCount();
				};
				if(ch&&Host)PSUMM.ClearPingInfo();				
			}
		}
	}
	PIEnumeratePlayers(PL_INFO,true);
	if(NPlayers>7)NPlayers=7;
	void AnalyseMessages();
	AnalyseMessages();
}
bool CreateMultiplaterInterface();
extern int CurStatus;
extern bool GameInProgress;
extern int GLOBALTIME;
extern int PGLOBALTIME;
extern int PitchTicks;
extern word PlayerMenuMode;
extern int tmtmt;
void ClearCTime();
void ReceiveAll();
extern bool DoNewInet;
bool CurrentGameInfo::CreateRoom(char* RoomName,int MaxPlayers){
	void PerformCheckCD();
	PerformCheckCD();
	CreateMultiplaterInterface();
	bool CreateSession(char* SessName,char* Name,DWORD User2,bool Style,int MaxPlayers);
	if(CreateSession(RoomName,PlName,0,1,MaxPlayers)){
		memset(CurrentMaxPing,0xFF,4*8);
		void ClearLPACK();
		ClearLPACK();
		ReceiveAll();
		CurStatus=0;
		PitchTicks=8;
		MaxPingTime=0;
		ClearCTime();
		PlayerMenuMode=1;
		GameInProgress=0;
		tmtmt=0;
		REALTIME=0;
		GLOBALTIME=0;
		PGLOBALTIME=0;
		PIEnumeratePlayers(PL_INFO,false);
		PL_INFO[0].Host=1;		
		IPCORE.EnableServer();
		CurState=1;
		PSUMM.ClearPingInfo();
		return true;
	}else return false;
}
bool FindSessionAndJoin(char* Name,char* Nick,bool Style);
bool CurrentGameInfo::JoinRoom(char* ip,char* RoomName){
	DoNewInet=true;
	CreateMultiplaterInterface();
	extern char IPADDR[128];
	strcpy(IPADDR,ip);
	if(FindSessionAndJoin(RoomName,PlName,1)){
		memset(CurrentMaxPing,0xFF,4*8);
		ReceiveAll();
		void ClearLPACK();
		ClearLPACK();
		CurStatus=0;
		PitchTicks=8;
		MaxPingTime=0;
		ClearCTime();
		PlayerMenuMode=1;
		GameInProgress=0;
		tmtmt=0;
		REALTIME=0;
		GLOBALTIME=0;
		PGLOBALTIME=0;
		PIEnumeratePlayers(PL_INFO,false);		
		CurState=2;
		PSUMM.ClearPingInfo();
		void PerformCheckCD();
		PerformCheckCD();
		return true;
	}return false;
}
void CloseMPL();
CIMPORT
void LeaveGSCRoom();
void CurrentGameInfo::LeaveRoom(){
	CloseMPL();
	CurState=0;
	MyDPID=0;
	SetRoomOptions("");
	extern bool UseGSC_Login;
	if(UseGSC_Login)LeaveGSCRoom();
}
PlayerInfo* CurrentGameInfo::GetCurrentPlayerInfo(){
	for(int i=0;i<NPlayers;i++)if(PL_INFO[i].PlayerID==MyDPID)return PL_INFO+i;
	return NULL;
}
PlayerInfo* CurrentGameInfo::GetHostPlayerInfo(){
	for(int i=0;i<NPlayers;i++)if(PL_INFO[i].Host)return PL_INFO+i;
	return NULL;
}
int GetReadyPercent();
int CurrentGameInfo::GetReadyPercent(){
	return ::GetReadyPercent();
};
int GetPing(DPID pid);
int CurrentGameInfo::GetPing(DWORD PlayerID){
	return ::GetPing(PlayerID);
};
bool CurrentGameInfo::Kick(DWORD PlayerID){
	PlayerInfo* I=GetCurrentPlayerInfo();
	if(I&&I->Host){
		return IPCORE.DeletePeer(PlayerID);
	}
	return false;
};
//
EngineSettings EngSettings;
//
void GameSettings::reset_class(void* DataPtr){
	GameClass::reset_class(DataPtr);
	SelBarGP=GPS.PreLoadGPImage("interf3\\selbar");
}
//
bool CurrentGameInfo::isHumanPlayer(byte Color){
	if(NPlayers==1){
		return Color==MyNation;
	}else
	for(int i=0;i<NPlayers;i++){
		PlayerInfo* PI=PL_INFO+i;
		if(PI->ColorID==Color){
			return true;
		}
	}
	return false;
}
bool CLeibzigDemo::StartRecord(){
	int nr=RecordsList.GetAmount();
	if(nr){
		int p=rand()%nr;
		char* rname=GetStr(&RecordsList,p);
		//HideBorderMode=1;
		//NoMoveMode=1;
		void PlayRecfile(char*);
		//IRS->ClearDeviceTarget();
		PlayRecfile(rname);
		//if(!GSets.LeibzigDemo.RecBroken)return 0;
		//HideBorderMode=0;
		//NoMoveMode=0;
		return true;
	}
	return false;
};
//
CEXPORT int GetCDVolume();
void SetCDVolumeEx(int Vol);
//
bool CLeibzigDemo::StartVideo(){
	Lpressed=false;
	void PlayFullscreenVideo(char* name,float px,float py);
	int nr=GSets.LeibzigDemo.VideoList.GetAmount();
/*
	if(nr){
		int p=rand()%nr;
		char* rname=GSets.LeibzigDemo.GetStr(&GSets.LeibzigDemo.VideoList,p);
		PlayFullscreenVideo(rname,float(100-GSets.LeibzigDemo.VideoScaleX)/100.0f,float(100-GSets.LeibzigDemo.VideoScaleY)/100.0f);
		return true;
	}
*/
	for(int p=0;p<nr;p++){		
		char* rname=GSets.LeibzigDemo.GetStr(&GSets.LeibzigDemo.VideoList,p);
		PlayFullscreenVideo(rname,float(100-GSets.LeibzigDemo.VideoScaleX)/100.0f,float(100-GSets.LeibzigDemo.VideoScaleY)/100.0f);
		//return true;
	}	
	return false;
};
bool IsCameraLocked(){
	return !(GSets.CGame.ViewMask& 4); 
}