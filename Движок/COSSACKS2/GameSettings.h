#ifndef __GAME_SETTINGS_H__
#define __GAME_SETTINGS_H__
#include <ClassEngine.h>
#include <more_types.h>
#pragma pack( push )
#pragma pack( 1 )
#include "EngineSettings.h"
class GameClass:public BaseClass{
public:
	void SaveToFile(char* fName);
	bool LoadFromFile(char* fname);
	SAVE(GameClass)		
	ENDSAVE
};
typedef LinearArray<int,_accamulator> StrAccamulator;
class CLeibzigDemo:public BaseClass{
public:
	bool Enabled;
	bool RecBroken;
    StrAccamulator RecordsList;
	StrAccamulator MissionsList;
	StrAccamulator VideoList;
	int VideoScaleX;
    int VideoScaleY;
	int StayTime;
	char* GetStr(StrAccamulator* str,int index){
		static Enumerator* E=NULL;
		if(!E)E=ENUM.Get("STDENUM");
		return E->Get((*str)[index]);
	}
	bool StartRecord();
	bool StartVideo();
	CLeibzigDemo(){
		StayTime=60000;
		Enabled=0;
		RecBroken=0;
	};
	SAVE(CLeibzigDemo)
		REG_MEMBER(_bool,Enabled);
		REG_AUTO_ENUM(RecordsList,STDENUM);
		REG_AUTO_ENUM(MissionsList,STDENUM);
		REG_AUTO_ENUM(VideoList,STDENUM);
		REG_MEMBER(_int,StayTime);
		REG_MEMBER(_int,VideoScaleX);
		REG_MEMBER(_int,VideoScaleY);
	ENDSAVE
};
class SoundAndVideoOptions:public BaseClass{
public:
	SoundAndVideoOptions(){
		ScreenSizeX=1024;
		ScreenSizeY=768;
		SoundVolume=50;
		MusicVolume=50;
		RequiredMsPerFrame=40;
		ScrollSpeed=4;
		MusicPlayMode=1;
		WaterQuality=true;
		RazduplingTimeForDemo=20000;
		SleepTimeForDemo=100;
		SelectionType=0;
	}
	int ScreenSizeX;
	int ScreenSizeY;
	int SoundVolume;//0..100
	int MusicVolume;//0..100
	int RequiredMsPerFrame;
	int ScrollSpeed;
	int MusicPlayMode;//0-no music 1-random music
	bool WaterQuality;//true-good,false-bad
	int SleepTimeForDemo;
	int RazduplingTimeForDemo;
	int SelectionType;
	SAVE(SoundAndVideoOptions);
	REG_MEMBER(_int,ScreenSizeX);
	REG_MEMBER(_int,ScreenSizeY);
	REG_MEMBER(_int,SoundVolume);
	REG_MEMBER(_int,MusicVolume);
	REG_MEMBER(_int,RequiredMsPerFrame);
	REG_MEMBER(_int,ScrollSpeed);
	REG_MEMBER(_int,MusicPlayMode);
	REG_MEMBER(_bool,WaterQuality);
	REG_MEMBER(_int,SleepTimeForDemo);
	REG_MEMBER(_int,RazduplingTimeForDemo);
	REG_ENUM(_index,SelectionType,UNITS_SEL_TYPE);
	ENDSAVE;
};
class CurrentGameInfo:public BaseClass{
public:
	byte CurState;//0-outside room, 1 - host & in room, 2 - client & in room, 3 - in game  
	DWORD ViewMask;//1-res panel 2-minimap 4-units interface
	bool SilenceMessageEvents;
	//
	PlayerInfo PL_INFO[8];
	byte cgi_NatRefTBL [8];
	char cgi_CurrentMap[64];	
	int UnitLimit;
	void RunSingleplayerGame(byte Color);
	void RunHostGame();
	void RunClientGame();
	void ProcessRoom();
	bool CreateRoom(char* RoomName,int MaxPlayers);
	bool JoinRoom(char* ip,char* RoomName);
	void LeaveRoom();
	PlayerInfo* GetCurrentPlayerInfo();
	PlayerInfo* GetHostPlayerInfo();
	CurrentGameInfo(){
		CurState=0;
		for(int i=0;i<8;i++)cgi_NatRefTBL[i]=i;
		ViewMask=255;
	}
	int GetReadyPercent();
	int GetPing(DWORD PlayerID);
	bool Kick(DWORD PlayerID);
	bool isHumanPlayer(byte Color);
	bool UnitTransform;
	bool ArcadeMode;
    int  SpecialGameSpeed;
};
class VideoFile:public BaseClass{
public:
	_str File;
	float ScaleX;
	float ScaleY;
	SAVE(VideoFile);
		REG_FILEPATH(File,".bik");
		REG_MEMBER(_float01,ScaleX);
		REG_MEMBER(_float01,ScaleY);
	ENDSAVE;
	VideoFile(){
        ScaleX=1;        
		ScaleY=1;
	}
};
class GameSettings:public GameClass{
public:
	virtual void reset_class(void* DataPtr);
	//insert there different constants and settings
	//and then register them below
	CLeibzigDemo LeibzigDemo;
	ClonesArray<VideoFile> VideoList;
	SoundAndVideoOptions SVOpt;
	bool CheckG17_dates;
	bool AllowFormationsStatesProcessing;
	bool ShowWayOnRoad;
	bool ShowFPS;
	bool HintDebugMode;
	byte StartMode;
	int AnimQuality;	// 0 - super , 1 - 1/2 frame drop, 2 - 2/3 frame drop
	CurrentGameInfo CGame;
	bool ShowChat;
	int ShowRecListTime;
	int GameDayLength;	// in AnimTime
	//
	bool DisableHint;
	bool DisableVideo;
	bool DisableSelectionBar;
	bool DisableMinimap;
	//
	bool ReleaseInterface;
	//
	bool ShowAurasMarker;
	word SelBarGP;
	word gpRally;
	word gpMove;
	bool DisableFriendlyFire;
	bool PeasantAutoWork;

    //
    int   ShadowQuality;
    bool EnableSubtitres;

	//-----------registration section--------------
	SAVE(GameSettings){
		//register it there!
		REG_CLASS(VideoFile);
		REG_AUTO(LeibzigDemo);
		REG_AUTO(SVOpt);
		REG_AUTO(VideoList);
		REG_MEMBER(_bool,CheckG17_dates);
		REG_MEMBER(_bool,AllowFormationsStatesProcessing);
		REG_MEMBER(_bool,ShowWayOnRoad);
		REG_MEMBER(_bool,ShowFPS);
		REG_MEMBER(_bool,HintDebugMode);
		REG_ENUM  (_byte_index,StartMode,PerspMode);
        REG_ENUM  (_index,ShadowQuality,ShadowQuality);
		REG_MEMBER(_int,AnimQuality);
		REG_MEMBER(_bool,ShowChat);
		REG_MEMBER(_int,ShowRecListTime);
		REG_MEMBER(_int,GameDayLength);
		//
		REG_MEMBER(_bool,DisableHint);
		REG_MEMBER(_bool,DisableVideo);
		REG_MEMBER(_bool,DisableSelectionBar);
		REG_MEMBER(_bool,DisableMinimap);
		//
		REG_MEMBER(_bool, ReleaseInterface);
		REG_MEMBER(_bool, ShowAurasMarker);
		REG_MEMBER(_gpfile,SelBarGP);
		REG_MEMBER(_gpfile,gpRally);
		REG_MEMBER(_gpfile,gpMove);
		REG_MEMBER(_bool,DisableFriendlyFire);
		REG_MEMBER(_bool,PeasantAutoWork);
		//
		REG_MEMBER(_bool,EnableSubtitres);		
	}ENDSAVE;
	void Save(){
        SaveToFile("settings.xml");
	}
	void Load(){
		LoadFromFile("settings.xml");
	}
	GameSettings(){		
		CheckG17_dates=false;
		AnimQuality=1;
		AllowFormationsStatesProcessing=0;
		ShowFPS=true;
		ShowWayOnRoad=true;
		GameDayLength=600;
		HintDebugMode=false;
		PeasantAutoWork=true;
        ShadowQuality=0; // high shadow quality by default
		EnableSubtitres=true;
	}
};

#ifdef IMPLEMENT_CLASS_FACTORY
//
GameSettings GSets;
bool GetHintDebugMode(){
	return GSets.HintDebugMode;
}
//
void GameClass::SaveToFile(char* fName){
	xmlQuote xml("GameSettings");
	BaseClass::Save(xml,this);
	xml.WriteToFile(fName);
}
bool GameClass::LoadFromFile(char* fname){
	xmlQuote xml;
	if(xml.ReadFromFile(fname)){
		ErrorPager EP;
		BaseClass::Load(xml,this,&EP);
		return true;
	}else return false;
}
#else //IMPLEMENT_CLASS_FACTORY
extern GameSettings GSets;
#endif //IMPLEMENT_CLASS_FACTORY
#pragma pack( pop )
#endif //__GAME_SETTINGS_H__