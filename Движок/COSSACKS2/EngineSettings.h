//
#pragma once
//
// MissGlobalSET /////////////////////////////////////////////////////////
class MissGlobalSET : public BaseClass
{
public:
	MissGlobalSET()		{ Play_Task_Sound=false; };
	~MissGlobalSET()	{};

	// TASK SET  //
	SubSection	TASK_MENU;
	bool	DonotShowComleteQuest;
	int		minTASK_Lx;
	int		TsskMarge_x;
	int		TsskMarge_y;
	int		TextMarge_x;
	int		TextMarge_y;
	void	SetDeff_TASK_MENU()	{ 
		DonotShowComleteQuest=true;
		minTASK_Lx=200; 
		TsskMarge_x=10; 
		TsskMarge_y=10; 
		TextMarge_x=5; 
		TextMarge_y=5; 
		NotActiveDiffuse=0x99FFFFFF; 
	};
	DWORD	NotActiveDiffuse;
	class SetDeff_ALEX : public BaseFunction
	{
	public:
		void EvaluateFunction(){
			MissGlobalSET* pPAR = get_parent<MissGlobalSET>();
			if (pPAR!=NULL) {
				pPAR->SetDeff_TASK_MENU();
			};
		};
		SAVE(SetDeff_ALEX);
			REG_PARENT(BaseFunction);
		ENDSAVE;
	}SetDeff_TM_ALEX;

	// For play sound after and front
	bool	Play_Task_Sound;
	_str	PTS_FileName;
	_str	PTS_FileName_Give;

	SAVE(MissGlobalSET);
		REG_AUTO(TASK_MENU);
		REG_MEMBER(_bool,DonotShowComleteQuest);
		REG_MEMBER(_int,minTASK_Lx);
		REG_MEMBER(_int,TsskMarge_x);
		REG_MEMBER(_int,TsskMarge_y);
		REG_MEMBER(_int,TextMarge_x);
		REG_MEMBER(_int,TextMarge_y);
		REG_MEMBER(_color,NotActiveDiffuse);
		REG_AUTO(SetDeff_TM_ALEX);
		REG_MEMBER(_bool,Play_Task_Sound);
		REG_FILEPATH(PTS_FileName,".ogg");
		REG_FILEPATH(PTS_FileName_Give,".ogg");
	ENDSAVE;
};
//////////////////////////////////////////////////////////////////////////

class engInterface:public BaseClass{
public:
	int VolumeFullScreenBik;
	int VolumeSoundEvents;
	int VolumeMissDialogs;
	//
	int selBorder;
	DWORD selColor;
	//
	word fSettlement;
	int sprSettlement;
	bool sprSettlementAni;
	int sprSettlementAniX;
	int sprSettlementAniY;
	int SettlementSpriteHeight;
	bool StartHelp;
	int neTime;
	char* neLivingPlaces; // not enought
	char* neSklad;
	char* neResourceForProduce;
	char* neAmmunition;
	char* neSettlementCaptured;
	char* neSettlementLost;
	char* neBrigadeTerminated;
	char* neBrigadeLost;
	//
	int HintX;
	int HintY;
	//
	int svFrescoMusic;
	int svFrescoVoice;
	//
	bool ShowPeasantDamage;
	bool ShowPreCampMessage;
	//
	SAVE(engInterface){
		REG_MEMBER(_int,VolumeFullScreenBik);
		REG_MEMBER(_int,VolumeSoundEvents);
		REG_MEMBER(_int,VolumeMissDialogs);
		REG_MEMBER(_int,selBorder);
		REG_MEMBER(_color,selColor);
		REG_MEMBER(_gpfile,fSettlement);
		REG_SPRITE(sprSettlement,fSettlement);
		REG_MEMBER(_bool,sprSettlementAni);
		REG_MEMBER(_int,sprSettlementAniX);
		REG_MEMBER(_int,sprSettlementAniY);		
		REG_MEMBER(_int,SettlementSpriteHeight);
		REG_MEMBER(_bool,StartHelp);
		REG_MEMBER(_int,neTime);
		REG_MEMBER(_textid,neLivingPlaces);
		REG_MEMBER(_textid,neSklad);
		REG_MEMBER(_textid,neResourceForProduce);
		REG_MEMBER(_textid,neAmmunition);
		REG_MEMBER(_textid,neSettlementCaptured);
		REG_MEMBER(_textid,neSettlementLost);
		REG_MEMBER(_textid,neBrigadeTerminated);
		REG_MEMBER(_textid,neBrigadeLost);
		REG_MEMBER(_int,HintX);
		REG_MEMBER(_int,HintY);
		REG_MEMBER(_int,svFrescoMusic);
		REG_MEMBER(_int,svFrescoVoice);
		REG_MEMBER(_bool,ShowPeasantDamage);
		REG_MEMBER(_bool,ShowPreCampMessage);
	}ENDSAVE;
	void Init();
	engInterface(){
		Init();
	}
	virtual void reset_class(void* DataPtr){
		BaseClass* B=(BaseClass*)DataPtr;
        engInterface* I=dynamic_cast<engInterface*>(B);
        if(I) I->Init();
	}
};
class EngineSettings;
class ResourceSettings:public BaseClass{
public:
	word File;
	int Sprite[6];
	bool EnableSprHint;
	int dx;
	int dy;
	int Lx;
	int Ly;
	int SprHint[6];
	SAVE(ResourceSettings){
		REG_MEMBER(_gpfile,File);
		//
		REG_SPRITE_EX(Sprite[0],Sprite_0,File);
		REG_SPRITE_EX(Sprite[1],Sprite_1,File);
		REG_SPRITE_EX(Sprite[2],Sprite_2,File);
		REG_SPRITE_EX(Sprite[3],Sprite_3,File);
		REG_SPRITE_EX(Sprite[4],Sprite_4,File);
		REG_SPRITE_EX(Sprite[5],Sprite_5,File);
		//
		REG_MEMBER(_bool,EnableSprHint);
		REG_MEMBER(_int,dx);
		REG_MEMBER(_int,dy);
		REG_MEMBER(_int,Lx);
		REG_MEMBER(_int,Ly);
		REG_SPRITE_EX(SprHint[0],SprHint_0,File);
		REG_SPRITE_EX(SprHint[1],SprHint_1,File);
		REG_SPRITE_EX(SprHint[2],SprHint_2,File);
		REG_SPRITE_EX(SprHint[3],SprHint_3,File);
		REG_SPRITE_EX(SprHint[4],SprHint_4,File);
		REG_SPRITE_EX(SprHint[5],SprHint_5,File);
	}ENDSAVE;

	bool SetHint(byte NI, int* Cost, _str* txt);
};
class cHintTemplate:public BaseClass{
public:
	_str MessageID;
	_str HotKeyID;
	_str AccessControlID;
	_str PriceID;
	//
	_str HotKey;
	_str AccessControl;
	_str Produce;
	//
	SAVE(cHintTemplate){
		REG_AUTO(MessageID);
		REG_AUTO(HotKeyID);
		REG_AUTO(AccessControlID);
		REG_AUTO(PriceID);
		//
		REG_AUTO(HotKey);
		REG_AUTO(AccessControl);
		REG_AUTO(Produce);
	}ENDSAVE;

	//bool SetHint(byte NI, int* Cost, _str* txt);
	bool AddHotKey(_str* txt, int Key);
};
class OneFireInfo:public BaseClass{
public:	
	int ModelID;
	int SoundID;
	float Scale;
	OneFireInfo(){
		ModelID=-1;
		Scale=1;
		SoundID=-1;
	}
	SAVE(OneFireInfo);
		REG_MEMBER(_ModelID,ModelID);		
		REG_MEMBER(_float0_10,Scale);
		REG_ENUM(_index,SoundID,ALL_SOUNDS);
	ENDSAVE;
	void reset_class(void* ptr){
		OneFireInfo* FI=(OneFireInfo*)ptr;
		FI->Scale=1.0f;
		FI->SoundID=-1;
	}
	bool ForceSimplification(){return true;}
};
class OneRandomEffectInfo:public BaseClass{
public:	
	int ModelID;
	float Scale;
	float Probability;
	float MaxBirthPerSecond;
	float EffectErasureTime;
	OneRandomEffectInfo(){
		ModelID=-1;
		Scale=1;
		Probability=1.0f;
		MaxBirthPerSecond=4;
		EffectErasureTime=5;
	}
	SAVE(OneRandomEffectInfo);
	REG_MEMBER(_ModelID,ModelID);
	REG_MEMBER(_float0_10,Scale);
	REG_MEMBER(_float01,Probability);
	REG_MEMBER(_float0_10,MaxBirthPerSecond);
	REG_MEMBER(_float0_10,EffectErasureTime);
	ENDSAVE;
	void reset_class(void* ptr){
		OneRandomEffectInfo* FI=(OneRandomEffectInfo*)ptr;
		FI->Scale=1.0f;
	}
	bool ForceSimplification(){return true;}
};
class NatColorSettings:public BaseClass{
public:
	DWORD CLR[8];
	SAVE(NatColorSettings);		
		REG_MEMBER_EX(_color,CLR[0],C0);
		REG_MEMBER_EX(_color,CLR[1],C1);
		REG_MEMBER_EX(_color,CLR[2],C2);
		REG_MEMBER_EX(_color,CLR[3],C3);
		REG_MEMBER_EX(_color,CLR[4],C4);
		REG_MEMBER_EX(_color,CLR[5],C5);
		REG_MEMBER_EX(_color,CLR[6],C6);
		REG_MEMBER_EX(_color,CLR[7],C7);
	ENDSAVE;
	NatColorSettings(){
		init();
	}
	void init(){
		for(int i=0;i<8;i++){
			DWORD GetNatColor(int);
			CLR[i]=GetNatColor(i);
		}
	}
	void reset_class(void* BC){
		NatColorSettings* N=(NatColorSettings*)BC;
		N->init();
	}
};
class BrigArrowParam:public BaseClass{
public:
	int   StartArrowWidth;
	int   FinalArrowWidth;
	DWORD StartArrowColor;
	DWORD FinalArrowColor;
	int   RadiusOfEnemyDetection;
	DWORD StartCircleColor;
	DWORD FinalCircleColor;
	DWORD CircleLineColor;
	SAVE(BrigArrowParam);
		REG_MEMBER(_int,StartArrowWidth);
		REG_MEMBER(_int,FinalArrowWidth);
		REG_MEMBER(_color,StartArrowColor);
		REG_MEMBER(_color,FinalArrowColor);
		REG_MEMBER(_int,RadiusOfEnemyDetection);
		REG_MEMBER(_color,StartCircleColor);
		REG_MEMBER(_color,FinalCircleColor);
		REG_MEMBER(_color,CircleLineColor);
	ENDSAVE;
	void Init(){
		StartArrowWidth=80;
		FinalArrowWidth=32;
		StartArrowColor=0x400000FF;
		FinalArrowColor=0x80FF0000;
		RadiusOfEnemyDetection=600;		
		StartCircleColor=0x00FFFFFF;
		FinalCircleColor=0x40FF0000;
		CircleLineColor=0xFFFF0000;
	}
	BrigArrowParam(){
		Init();
	}
	void reset_class(void* B){
		BrigArrowParam* I=dynamic_cast<BrigArrowParam*>((BaseClass*)B);
		if(I){
			I->BaseClass::reset_class(I);
			I->Init();
		}
	}
};
class OneMapName:public BaseClass{
public:
	_str MapName;

	SAVE(OneMapName);
		REG_FILEPATH(MapName,"*.M3D");
	ENDSAVE;
	bool ForceSimplification(){return true;}
};
class SpecialSpeedForMaps:public BaseClass{
public:
	_str Description;
	int Speed;
	ClonesArray<OneMapName> MapsList;

	SAVE(SpecialSpeedForMaps);
		REG_CLASS(OneMapName);
		REG_AUTO(Description);
		REG_MEMBER(_int,Speed);
		REG_AUTO(MapsList);
	ENDSAVE;
	SpecialSpeedForMaps(){
        Speed=30;
	}
	const char* GetThisElementView(const char*){
		return Description.str;
	}	
};
class OnePrecashedSprite:public BaseClass{
public:
    word gpFileName;

	SAVE(OnePrecashedSprite);
		REG_MEMBER(_gpfile,gpFileName);
	ENDSAVE;

	bool ForceSimplification(){return true;}
};
class EngineSettings: public BaseClass{
public:
	class RefreshSurface:public BaseFunction{
	public:
		void EvaluateFunction(){
			void RefreshWater();
			RefreshWater();
			void MakeAllDirtyGBUF();
			MakeAllDirtyGBUF();
		}
		SAVE(RefreshSurface){
			REG_PARENT(BaseFunction);
		}ENDSAVE;
	};
	SubSection ColorParams;

	bool  DynamicalReflection;
	RefreshSurface Refresh;
	DWORD MiniRoadColor;
	DWORD MiniWaterColor;
	DWORD WaterColorModulator;
	DWORD MiniFogOfWarColor;
	DWORD FogOfWarColor;
	DWORD LightDiffuse;
	DWORD LightAmbient;
	DWORD AtmosphereColor;
	DWORD ShadowsColor;
	DWORD SunColor;
	DWORD ShadowColor;
	DWORD EnemyHighliting;
	DWORD FriendsHighliting;
	DWORD AllyHighliting;
	NatColorSettings NatColor;

	SubSection GeneralGameplayParams;

	int   AutoProduceFoodLimit;
	bool  AutoChangeFormationType;
	bool  AllowChangeFormationDistance;	
	int   GreaterFormationDistanceScale;
	int   LowerFormationDistanceScale;
	int   MoraleType;
	bool  AllowTiring;
	bool  DontUseAgressiveState;
	int   MarkerDx;
	int   MarkerDy;
	int   DisbandFormLimit;
	int   MaxBrigAddDamage;
	int   DefaultMissOnHeight;
	int   DefaultMissOnHeightMax;
	int   DefaultMissInsideUnitsDamage;
	bool  DontUseRoads;
	int   MinDistanceToEnterRoad;
	int   MinTopDistanceToEnterRoad;
	int   MinDistForLineFormations;
	bool  DebugTopologyMode;
	float TerrainZBias;
    float EffectsZBias;
    float CameraFactor;
	bool  DrawBrigadeDestPositions;
	int   BrigadeWaitingCycles;
	bool  BrigadeVseZaOdnogoInAgresivMode;
	bool  BrigadeVseZaOdnogoInNormalMode;

	int	  BrigadeAlarmDist1;
	int	  BrigadeAlarmDist2;
	int	  BrigadeAlarmDist3;
	int	  BrigadeAlarmRadius;
	int	  BrigadeAlarmRadiusTime;
	int	  BoidsOffLimit;
	int   RestFrequency;
	int   CannonAddShotDistPer100_Height;
	
	SubSection AI_Params;

	bool  EnableTacticalAI;
	bool  EnableVit2Alex;
	bool  EnableAutosavingInMissions;
	bool  EnableCapturing;
	bool  CaptureOnlyWithFormations;
	bool  DefendOnlyWithFormations;
	bool  LightenSelectedUnits;
	bool  DrawShotsFog;

	int   DefaultGameSpeedForCampaign;
	int   DefaultGameSpeedForSkirmish;	
	ClonesArray<SpecialSpeedForMaps> SpecifySpeedForMaps;
	bool  AllowCameraRotation;	
	//
	ClonesArray<OneFireInfo> FiresList;
	ClonesArray<OneFireInfo> SmokeList;
	ClonesArray<OneRandomEffectInfo> WaterBlobsList;
	ClonesArray<OneRandomEffectInfo> RunDustList;
	//
	SubSection VitalInterfaceSettings;

	engInterface vInterf;	// constants for vital interface
	bool AllyNetralBuildings;
	ResourceSettings Resource;
	cHintTemplate HintTemplate;	
	//
	
	ClonesArray<OnePrecashedSprite> PrecashedSprites;

	//
	SubSection      GraphicsParams;

	SubSection		RoadsSettings;
	// VITYA (MISSIONS SETTINGS)  //
	SubSection		GeneralMissionsSettings;
	int				DefMusicVolumeInMissFilms;
	float			RelativDialogSoundVolume;	// [0.f,1.f]
	float			RelativDialogMusicVolume;	// [0.f,1.f]
	MissGlobalSET	MIS_SET;
	////////////////////////////////
	int				GameName;
	
	bool NeuroLearn;
	bool NeuroUse;

	int BuildIconLx;
	int BuildIconLy;
	int PortraitIconLx;
	int PortraitIconLy;
	int MaxSpaceExtraDistance;

	
	SubSection CirclesAndArrowsColors;

	BrigArrowParam BrigadesArrowParam;

	DWORD AttackRangeFillInnerColor;
	DWORD AttackRangeFillOuterColor;
	DWORD AttackRangeLineOuterColor;

	DWORD BuildingAttackRangeInnerLineColor;
	DWORD BuildingAttackRangeOuterLineColor;
	DWORD BuildingAttackRangeInnerFillColor;
	DWORD BuildingAttackRangeOuterFillColor;

	DWORD AttackRangeFillColorInner1;
	DWORD AttackRangeFillColorOuter1;
	DWORD AttackRangeLineColorOuter1;

	DWORD AttackRangeFillColorInner2;
	DWORD AttackRangeFillColorOuter2;
	DWORD AttackRangeLineColorOuter2;
	
	DWORD AttackRangeFillColorInner3;
	DWORD AttackRangeFillColorOuter3;
	DWORD AttackRangeLineColorOuter3;

	DWORD FillCannonFillColor1;
	DWORD FillCannonFillColor2;

	SubSection PredefinedColors;

	DWORD Black;
	DWORD Red;	
	DWORD Yellow;
	DWORD White;	
	DWORD Gray;	
	DWORD Orange; 
	DWORD Disable;

	SAVE(EngineSettings);

		REG_AUTO(ColorParams);

		REG_AUTO(Refresh);
		REG_MEMBER(_bool,DynamicalReflection);
		REG_MEMBER(_color,MiniRoadColor);
		REG_MEMBER(_color,MiniWaterColor);
		REG_MEMBER(_color,WaterColorModulator);
		REG_MEMBER(_color,MiniFogOfWarColor);
		REG_MEMBER(_color,FogOfWarColor);
		REG_MEMBER(_color,LightDiffuse);
		REG_MEMBER(_color,LightAmbient);
		REG_MEMBER(_color,AtmosphereColor);
		REG_MEMBER(_color,ShadowsColor);
		REG_MEMBER(_color,SunColor);
		REG_MEMBER(_color,ShadowColor);
		REG_MEMBER(_color,EnemyHighliting);
		REG_MEMBER(_color,FriendsHighliting);
		REG_MEMBER(_color,AllyHighliting);
        REG_AUTO(NatColor);
		REG_AUTO  (Refresh);

		REG_AUTO(CirclesAndArrowsColors);

		REG_MEMBER(_color,AttackRangeFillInnerColor);
		REG_MEMBER(_color,AttackRangeFillOuterColor);
		REG_MEMBER(_color,AttackRangeLineOuterColor);

		REG_MEMBER(_color,BuildingAttackRangeInnerLineColor);
		REG_MEMBER(_color,BuildingAttackRangeOuterLineColor);
		REG_MEMBER(_color,BuildingAttackRangeInnerFillColor);
		REG_MEMBER(_color,BuildingAttackRangeOuterFillColor);

		REG_MEMBER(_color,AttackRangeFillColorInner1);
		REG_MEMBER(_color,AttackRangeFillColorOuter1);
		REG_MEMBER(_color,AttackRangeLineColorOuter1);

		REG_MEMBER(_color,AttackRangeFillColorInner2);
		REG_MEMBER(_color,AttackRangeFillColorOuter2);
		REG_MEMBER(_color,AttackRangeLineColorOuter2);

		REG_MEMBER(_color,AttackRangeFillColorInner3);
		REG_MEMBER(_color,AttackRangeFillColorOuter3);
		REG_MEMBER(_color,AttackRangeLineColorOuter3);

		REG_MEMBER(_color,FillCannonFillColor1);
		REG_MEMBER(_color,FillCannonFillColor2);

		REG_AUTO(PredefinedColors);

		REG_MEMBER(_color,Black);
		REG_MEMBER(_color,Red);	
		REG_MEMBER(_color,Yellow);
		REG_MEMBER(_color,White);	
		REG_MEMBER(_color,Gray);	
		REG_MEMBER(_color,Orange); 
		REG_MEMBER(_color,Disable);        

		REG_AUTO  (GeneralGameplayParams);

		REG_ENUM(_index,GameName,BE_GAME_NAME);
		REG_MEMBER(_int,DefaultGameSpeedForCampaign);
		REG_MEMBER(_int,DefaultGameSpeedForSkirmish);
		REG_CLASS(SpecialSpeedForMaps);
		REG_AUTO(SpecifySpeedForMaps);
		REG_MEMBER(_int,AutoProduceFoodLimit);
		REG_MEMBER(_bool,AutoChangeFormationType);
		REG_MEMBER(_bool,AllowChangeFormationDistance);
		REG_MEMBER(_int,GreaterFormationDistanceScale);
		REG_MEMBER(_int,LowerFormationDistanceScale);
		REG_ENUM  (_index,MoraleType,MoraleType);
		REG_MEMBER(_bool,AllowTiring);		
		REG_MEMBER(_int,DisbandFormLimit);
		REG_MEMBER(_int,MaxBrigAddDamage);
		REG_MEMBER(_int,DefaultMissOnHeight);
		REG_MEMBER(_int,DefaultMissOnHeightMax);
		REG_MEMBER(_int,DefaultMissInsideUnitsDamage);		
		REG_MEMBER(_bool,DebugTopologyMode);		
		REG_MEMBER(_int,BrigadeWaitingCycles);
		REG_MEMBER(_bool,BrigadeVseZaOdnogoInAgresivMode);
		REG_MEMBER(_bool,BrigadeVseZaOdnogoInNormalMode);

		REG_MEMBER(_int,BrigadeAlarmDist1);
		REG_MEMBER(_int,BrigadeAlarmDist2);
		REG_MEMBER(_int,BrigadeAlarmDist3);
		REG_MEMBER(_int,BrigadeAlarmRadius);
		REG_MEMBER(_int,BrigadeAlarmRadiusTime);
		REG_MEMBER(_int,BoidsOffLimit);
		REG_MEMBER(_int,RestFrequency);
		REG_MEMBER(_int,CannonAddShotDistPer100_Height);
		

		REG_MEMBER(_bool,DontUseAgressiveState);
		REG_MEMBER(_bool,AllowCameraRotation);
		REG_AUTO(BrigadesArrowParam);

		REG_AUTO(AI_Params);

		REG_MEMBER(_bool,EnableTacticalAI);
		REG_MEMBER(_bool,EnableVit2Alex);	
		REG_MEMBER(_bool,EnableCapturing);		
		REG_MEMBER(_bool,CaptureOnlyWithFormations);
		REG_MEMBER(_bool,DefendOnlyWithFormations);
		REG_MEMBER(_bool,NeuroLearn);	
		REG_MEMBER(_bool,NeuroUse);		
		//
		REG_AUTO(VitalInterfaceSettings);
		REG_AUTO(vInterf);
		REG_MEMBER(_bool,AllyNetralBuildings);
		REG_MEMBER(_int,BuildIconLx);
		REG_MEMBER(_int,BuildIconLy);
		REG_MEMBER(_int,PortraitIconLx);
		REG_MEMBER(_int,PortraitIconLy);
		REG_AUTO(Resource);
		REG_AUTO(HintTemplate);
		REG_MEMBER(_int,MaxSpaceExtraDistance);
		//
		REG_AUTO(RoadsSettings);
		REG_MEMBER(_bool,DontUseRoads);
		REG_MEMBER(_int,MinDistanceToEnterRoad);
		REG_MEMBER(_int,MinTopDistanceToEnterRoad);
		REG_MEMBER(_int,MinDistForLineFormations);

		REG_AUTO(GraphicsParams);
		
		REG_CLASS(OnePrecashedSprite);
		REG_AUTO(PrecashedSprites);
		REG_AUTO(Resource);
		REG_AUTO(HintTemplate);		
		REG_CLASS(OneFireInfo);
		REG_CLASS(OneRandomEffectInfo);
		REG_AUTO(FiresList);
		REG_AUTO(SmokeList);
		REG_AUTO(WaterBlobsList);
		REG_AUTO(RunDustList);
		REG_MEMBER(_bool,LightenSelectedUnits);
		REG_MEMBER(_int, MarkerDx);
		REG_MEMBER(_int, MarkerDy);
		REG_MEMBER(_float,TerrainZBias);
		REG_MEMBER(_float,EffectsZBias);
        REG_MEMBER(_float,CameraFactor);
		REG_MEMBER(_bool,DrawBrigadeDestPositions);
		REG_MEMBER(_bool,DrawShotsFog);

		// VITYA (MISSIONS SETTINGS)  //
		REG_AUTO(GeneralMissionsSettings);	
		REG_MEMBER(_int,DefMusicVolumeInMissFilms);
		REG_MEMBER(_float01,RelativDialogSoundVolume);
		REG_MEMBER(_float01,RelativDialogMusicVolume);
		REG_AUTO(MIS_SET);
		////////////////////////////////		

	ENDSAVE;	
	void Init(){
		DrawShotsFog=false;
		DynamicalReflection=false;
		MiniRoadColor=0xFF605040;
		MiniWaterColor=0xFF306080;
		MiniFogOfWarColor=0xB0505050;
		FogOfWarColor=0xFFFFFFFF;
		LightDiffuse=0xFF808080;
		LightAmbient=0xFF222222;
		WaterColorModulator=0xFF808080;
		AtmosphereColor=0;
		ShadowsColor=0xFF808080;
		SunColor=0xFF808080;
		EnemyHighliting   = 0xFFB04040;
		FriendsHighliting = 0xFF4040B0;
		AllyHighliting    = 0xFF40B040;
		AutoProduceFoodLimit=500;
		AutoChangeFormationType=0;
		AllowChangeFormationDistance=0;
		GreaterFormationDistanceScale=200;
		MoraleType=0;
		AllowTiring=1;
		ShadowsColor=0;
		DisbandFormLimit=25;
		MaxBrigAddDamage=10000;
		float GetTerrainZBias();
		TerrainZBias=GetTerrainZBias();
        EffectsZBias=150;
        CameraFactor=3.5f;
		DrawBrigadeDestPositions=false;
		BrigadeWaitingCycles=40;	
		BrigadeVseZaOdnogoInAgresivMode=false;
		BrigadeVseZaOdnogoInNormalMode=false;
		BrigadeAlarmDist1=1500;
		BrigadeAlarmDist2=1000;
		BrigadeAlarmDist3=700;
		BrigadeAlarmRadius=700;
		BrigadeAlarmRadiusTime=6;
		BoidsOffLimit=5000;
		RestFrequency=128*8;
		CannonAddShotDistPer100_Height=300;

		EnableTacticalAI=false;
		EnableVit2Alex=false;
		EnableAutosavingInMissions=true;
		EnableCapturing=false;
		CaptureOnlyWithFormations=false;
		DefendOnlyWithFormations=false;
		NeuroLearn=false;
		NeuroUse=false;
		MinDistanceToEnterRoad=2500;
		MinTopDistanceToEnterRoad=1100;
		DefaultGameSpeedForCampaign=40;
		DefaultGameSpeedForSkirmish=30;
		MinDistForLineFormations=1800;
		BuildIconLx=45;
		BuildIconLy=45;
		PortraitIconLx=45;
		PortraitIconLy=45;
		MaxSpaceExtraDistance=5;


		Black	 = 0xFF2E2317;
		Red	     = 0xFF8A1000;
		Yellow   = 0xFFD4C19C;
		White	 = 0xFFFFF7EF;
		Gray	 = 0xFF6D6862;
		Orange   = 0xFF6A3000;
		Disable  = 0xC0665F57;

		AttackRangeFillInnerColor=0x1EFF0000;
		AttackRangeFillOuterColor=0x28FF0000;
		AttackRangeLineOuterColor=0x80FF0000;

		BuildingAttackRangeInnerLineColor=0x40FF0000;
		BuildingAttackRangeOuterLineColor=0x90FF0000;
		BuildingAttackRangeInnerFillColor=0x40FF0000;
		BuildingAttackRangeOuterFillColor=0x80FF0000;

		AttackRangeFillColorInner1=0x0AFFFFFF;
		AttackRangeFillColorOuter1=0x3CFF0000;
		AttackRangeLineColorOuter1=0xFFFF0000;

		AttackRangeFillColorInner2=0x1EFF0000;
		AttackRangeFillColorOuter2=0x3CFFFF00;
		AttackRangeLineColorOuter2=0xFFFFFF00;

		AttackRangeFillColorInner3=0x1EFFFF00;
		AttackRangeFillColorOuter3=0x3C00FF00;
		AttackRangeLineColorOuter3=0xFF00FF00;

		FillCannonFillColor1=0x1000FF00;
		FillCannonFillColor2=0x6000FF00;

	}
	EngineSettings(){
		Init();
	}
	void reset_class(void* B){
        EngineSettings* I=dynamic_cast<EngineSettings*>((BaseClass*)B);
		if(I){
			I->BaseClass::reset_class(I);
			I->Init();
		}
	}
};
extern EngineSettings EngSettings;//
#include "Localization.h"