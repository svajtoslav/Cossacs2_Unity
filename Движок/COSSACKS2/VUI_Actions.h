#pragma once
#include <more_types.h>
#include "MapTemplates.h"
#include "vui_Action.h"
//
//////////////////////////////////////////////////////////////////////////
template <class T> inline void reg_v_Action(char* Name,char* Section=NULL){
	T* v=new T;
	if(Name&&Name[0]){
		v->Name=Name;
		if(Section)vui_Action::E->Add(v->GetName(),(DWORD)v,Section);		
		else vui_Action::E->Add(v->GetName(),(DWORD)v);
	}
	if(Section){
		int i=0;
		while(Section[i]!='_')i++;
		Section+=i+1;
		i=0;
		while(Section[i]!='_')i++;
		_str s; s=Section;
		s.str[i]=0;		
		REG_CLASS_EX(T,s.str);
	}else REG_CLASS_EX(T,"Old style");
};
#define reg_cva(T,Name) reg_v_Action<T>(Name,#T);
//
/*
class cva_DialogsDesk: public vui_Action{
public:
	DialogsDesk* DD;
};
*/
//
#pragma warning ( push )
#pragma warning ( disable : 4003 )
//
regAc(cva_ClearClick, vfLt vfRt , );
//
regAc(cva_OpenWebLink, vfS vfL _str Link, REG_AUTO(Link) );
regAc(cva_ExitToWin, vfL, );
regAc(cva_Exit2MainMenu, vfL, );
//
class _strArray: public BaseClass {
public:
	ClonesArray<_str> Str;
	_strArray() {};
	SAVE(_strArray){
		REG_AUTO(Str);
	}ENDSAVE;
};
regAc(cva_DemoScreen, vfS
	int SwitchTime;
	ClonesArray< _strArray > Pic;
	,
	REG_MEMBER(_int,SwitchTime);
	REG_AUTO(Pic);
);
regAc(cva_DemoNext, vfS vfL, );
regAc(cva_DemoExit, vfS vfL, );
//
regAc(cva_PlayFullscreenVideo, vfL
	bool Credits;
	float px;
	float py;
	,
	REG_MEMBER(_bool, Credits);
	REG_MEMBER(_float, px);
	REG_MEMBER(_float, py);
);
regAc(cva_TutButtonVisibleAdd, vfS);
regAc(cva_LoadingTips, vfS);
regAc(cva_Loading_Back, vfS
	int ID;
	int Amount;
	,
	REG_MEMBER(_int, ID);
	REG_MEMBER(_int, Amount);
);
regAc(cva_NatPics, vfS);
//
regAc(cva_MM_UnloadBeforeMission, vfS , );
regAc(cva_BR_Title, vfS
	char* SinTitle;
	char* MulTitle;
	,
	REG_MEMBER(_textid, SinTitle);
	REG_MEMBER(_textid, MulTitle);
);
regAc(cva_vGameMode_Set, vfL
	int Mode;
	,
	REG_ENUM(_index,Mode,MM_GameMode);
);
regAc(cva_vGameMode_SetVisible, vfS
	int Mode;
	int Condition;	
	int Operator;
	,		
	REG_ENUM(_index,Mode,MM_GameMode);
	REG_ENUM(_index,Condition,MM_LogicCon);	
	REG_ENUM(_index,Operator,MM_LogicOp);	
);
//
regAc(cva_MR_Desk, vfS
	ClassRef<DialogsDesk> Chat;
	ClassRef<TabButton> Load;
	ClassRef<DialogsDesk> MulButtons;
	ClassRef<DialogsDesk> SinButtons;
	,
	REG_AUTO(Chat);
	REG_AUTO(Load);
	REG_AUTO(MulButtons);
	REG_AUTO(SinButtons);
);
regAc(cva_MR_isServer, vfS bool Server, REG_MEMBER(_bool,Server) );
// SelPoint
regAc(cva_SP_Stop, vfS vfL vfRt);
regAc(cva_SP_Stop_Cannon, vfS vfL vfRt);
regAc(cva_SP_Kinetic,vfS
	ClassRef<TextButton> Title;
	,
	REG_AUTO(Title);
);
//
regAc(cva_RND_MapParam,vfS
	  int Index,
	  REG_MEMBER(_int,Index);
);
//
regAc(cva_BrigDestination,vfS
	int dx;
	int dy;
	bool MiniMap;
	,	  
	REG_MEMBER(_int,dx);
	REG_MEMBER(_int,dy);
	REG_MEMBER(_bool,MiniMap);	
);
// Geometry - manipulation whith size and position of dialogs and children
regA(cva_Geometry_CompactHorisontList,vfS);
//
regA(cva_Geometry_Align_Top,vfS);
regA(cva_Geometry_List_Horisont,vfS);
regA(cva_Geometry_WhiteSpace_Right,vfS);
//
regA(cva_MM_SinPrepare,vfL);
// L ability
regAc(cva_DrawBrig_LAbility,vfS	vfL
	ClassRef<ProgressBar> Main;
	ClassRef<DialogsDesk> Ability;
	ClassRef<DialogsDesk> Morale;
	ClassRef<DialogsDesk> Experience;
	short MainGP;
	ClassRef<GPPicture> Branch;	
	int BranchSpr_0;
	int BranchSpr_1;
	int BranchSpr_2;
	,
	REG_AUTO(Main);
	REG_AUTO(Ability);
	REG_AUTO(Morale);
	REG_AUTO(Experience);
	REG_MEMBER(_gpfile,MainGP);
	REG_AUTO(Branch);
	REG_SPRITE(BranchSpr_0,MainGP);
	REG_SPRITE(BranchSpr_1,MainGP);
	REG_SPRITE(BranchSpr_2,MainGP);
);
regAc(cva_BB_Morale, vfS);
//
regAc(cva_CreateRandomMapName,vfS	
	  ,//int ChooseValue,
	  //REG_MEMBER(_int,ChooseValue);
);
//
regAc(cva_ItemChoose_Set,vfL	
	int ChooseValue,
	REG_MEMBER(_int,ChooseValue);
);
//
regAc(cva_PlayerInfoTab,vfS, ; );
regAc(cva_InGameMenu_ExitConfirm, vfS
	ClassRef<TextButton> Window;
	ClassRef<TextButton> Text;
	ClassRef<VitButton> Exit;
	char* GMapWindow;
	char* GMapText;
	char* GMapExit;
	char* SkirmishWindow;
	char* SkirmishText;
	char* SkirmishExit;
	,
	REG_AUTO(Window);
	REG_AUTO(Text);
	REG_AUTO(Exit);
	REG_MEMBER(_textid,GMapWindow);
	REG_MEMBER(_textid,GMapText);
	REG_MEMBER(_textid,GMapExit);
	REG_MEMBER(_textid,SkirmishWindow);
	REG_MEMBER(_textid,SkirmishText);
	REG_MEMBER(_textid,SkirmishExit);
);
regAc(cva_HideIfUInfoActive,vfS);
regAc(cva_InGameMenu_MainDesk,vfS
	static int CurrentID;
	int ID,
	REG_ENUM(_index,ID,InGameMainMenu);
);
regAc(cva_InGameMenu_MainDesk_Set,vfL
	int ID,
	REG_ENUM(_index,ID,InGameMainMenu);	
);
//regA(cva_Map_Preview,vfS);
void DrawTexturedBar(float xL,float yL,float W,float H,
					 float txL,float tyL,float tW,float tH,
					 DWORD CLU,DWORD CRU,DWORD CLD,DWORD CRD,
					 int TextureID,int ShaderID);
class cva_Map_Preview:public vui_Action{
public:
	virtual void OnDraw(SimpleDialog* SD){
		if(SD->Visible){
			int GetMinimapTexture();
			int t=GetMinimapTexture();
			static int s=IRS->GetShaderID("hud_smooth");
			DrawTexturedBar(SD->x,SD->y,SD->GetWidth(),SD->GetHeight(),0,0,240.0f/256.0f,240.0f/256.0f,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF,t,s);
		}
	};
	virtual void SetFrameState(SimpleDialog* SD);
	SAVE(cva_Map_Preview);
		REG_PARENT(vui_Action);
	ENDSAVE;
};
regAc(cva_Map_PreviewJpg, vfS vfD bool FirstDraw);
regAc(cva_CircleNormaTest, vfD);
//
class cva_BR_WhyNotStart:public vui_Action{
public:
	virtual void OnDraw(SimpleDialog* SD);
	virtual void SetFrameState(SimpleDialog* SD);
	SAVE(cva_BR_WhyNotStart);
	REG_PARENT(vui_Action);
	ENDSAVE;
};
//
regAc(va_LD_Skirmish, vfI vfS vfA
	ClassRef<BitPicture> Preview,
	REG_AUTO(Preview);
);
regAc(va_LD_Battle, vfI vfS
	ClassRef<BitPicture> Preview,
	REG_AUTO(Preview);
);
regAc(cva_MM_AcceptInDesk,vfL
	ClassRef<DialogsDesk> Desk,
	REG_AUTO(Desk);
);
regAc(cva_MM_CancelInDesk,vfL
	  ClassRef<DialogsDesk> Desk,
	  REG_AUTO(Desk);
);
regAc(cva_MM_FilesList, vfS
	_str Folder;	
	_str Ext;
	_str FileName;	
	ClassRef<InputBox> ibName,
	REG_AUTO(Folder);
	REG_AUTO(Ext);
	//REG_AUTO(FileName);
	REG_AUTO(ibName);
);
regA(cva_AB_Button,vfS,vfL,vfR);
regAc(cva_AB_Act_Button, vfS vfL vfR;
	ClassRef<GPPicture> Pic;
	ClassRef<ProgressBar> CoolDown,
	REG_AUTO(Pic);
	REG_AUTO(CoolDown);
);
regAc(cva_ActAblDD, vfS
	int Type;
	,
	REG_MEMBER(_int,Type);
);
regAcx(cva_ActAbl, cva_AB_Act_Button, vfD vfS vfL vfR
	//static int Type;
	//,
	//REG_MEMBER(_int,Type);
);

regAcx(cva_MM_Run_MissionM3D,cva_MM_FilesList, vfA,
	//ClassRef<InputBox> ibName,
	REG_AUTO(Folder);
	REG_AUTO(Ext);
	//REG_AUTO(FileName);
	REG_AUTO(ibName);
);
//
regAc(cva_MM_GoldenTextXY,vfS);
regA(va_SP_TiredLine,vfS);
regA(va_SP_LifeLine,vfS);
regA(va_SP_MoraleLine,vfS);
regA(va_BR_StandGroundLine,vfS);
regAc(va_MC_Discard,vfS vfL vfRt , );
regAc(va_MC_Fill,vfS vfL vfRt , );
regAc(va_MC_LineShot,vfS vfD vfL , );
regAc(va_MC_Reform,vfS vfL vfRt , );
regAc(va_CannonFillUnits, vfS vfD vfL vfRt);
regAc(va_CannonReload,vfS vfL vfRt
	//char* Hint;
	//int HotKey;
	,
	//REG_MEMBER(_textid,Hint);
	//REG_ENUM(_index,HotKey,ve_HotKey);
);
regA(va_CannonUnLink,vfS,vfL,vfRt);
regA(va_CannonReloadStage,vfS);
regA(va_CannonFire,vfS,vfL,vfR);
regA(va_W_CannonSet,vfS);
regA(va_UnitBigPortret,vfS);
regA(va_SP_Bld_BigPortret,vfS);
regA(va_UnitProdPort,vfS);
regA(va_UnitProdStage,vfS);
regA(va_Unit_P_Box,vfS,vfL,vfR);
regAc(cva_Guardian, vfS);
regA(va_WeapPort,vfS);
regAc(va_WeapPortBack, vfS vfL vfRt
	char* Hint_Enabled;
	char* Hint_Disabled;
	,
	REG_MEMBER(_textid, Hint_Enabled);
	REG_MEMBER(_textid, Hint_Disabled);
);
regA(va_MiniComDesk,vfS);
regA(va_SP_BranchSprite,vfS);
regA(va_SP_Kills,vfS);
regA(va_SP_KillsAward,vfS);
regAc(cva_SP_KillsGuardian,vfS);
regA(va_SP_Morale,vfS);
regA(va_SP_NatFlag,vfS);
regA(va_SP_Protect,vfS);
regA(va_W_Damage,vfS);
regA(va_W_Charge,vfS);
regA(va_W_ChargeLine,vfS);
regAc(cva_BrigBinkVideoTry, vfS
	int PlayerID,
	REG_MEMBER(_int,PlayerID);
);
regAc(cva_BrigBinkVideoShow, vfD
	int PlayerID,
	REG_MEMBER(_int,PlayerID);
);
regAc(cva_BrigBinkVisibility, vfS);
regAc(cva_BrigBinkSetVision, vfL);
//
regA(va_SP_PortretBox, vfD vfL vfR);
regA(va_SP_PortretBox2, vfS vfD vfL vfR);
regA(va_SP_PortretBox3, vfD vfL vfR);
regA(va_SP_SideBox, vfD vfS vfL vfR);

regAx(va_SP_BranchColor,va_SP_PortretBox,vfS);
regAx(va_SP_CenUp_One,va_SP_PortretBox,vfS);
regAx(va_SP_CenUp_Mul,va_SP_PortretBox,vfS);
regAx(va_SP_CenDown,va_SP_PortretBox,vfS);
regAx(va_SP_BuildingOnly,va_SP_PortretBox,vfS);
regA(va_SP_B_Population,vfS);
regA(va_SP_ConstructMode,vfS);
regAx(va_SP_ConstructedMode,va_SP_ConstructMode,vfS);
regAx(va_SP_B_Stage,va_SP_ConstructMode,vfS);
regAx(va_SP_B_StageLine,va_SP_ConstructMode,vfS);
regAx(va_SP_B_Life,va_SP_ConstructedMode,vfS);
regAx(va_SP_B_Places,va_SP_ConstructedMode,vfS);

regAx(va_SP_BldOnly_Side,va_SP_SideBox,vfS);
regAx(va_SP_BColorSide,va_SP_SideBox,vfS);
regA(va_SP_UnitSprSide,vfS);
regA(va_SP_UnitNameSide,vfS);
regA(va_Unit_P_BuildMode,vfS);
regA(va_Unit_P_BuildMode2,vfS);
regA(va_Unit_P_Amount,vfS);
regA(va_Unit_P_Unlimit,vfS);
regA(va_Unit_P_Unlimit2,vfS);
regAx(va_SP_NameCircle,va_SP_PortretBox,vfS);
regAx(va_SP_NameCircleSide,va_SP_SideBox,vfS);
regAx(va_SP_NameColor,va_SP_PortretBox,vfS);
regAx(va_SP_MoraleSide,va_SP_SideBox,vfS);
regA(va_SP_MoraleSideText,vfS);
regA(va_SP_MoraleSideBlink,vfS);
regA(va_GB_SelBuildings,vfS,vfL,vfRt);
regA(va_DB_SetVarReturn,vfL);
regA(va_ListDesk,vfS);
regA(va_ListItem,vfL);
regA(va_vi_ReturnID,vfL);
regAc(va_MissDescription, vfS
	ClassRef<GPPicture> gpPicture;
	,
	REG_AUTO(gpPicture);
);
regAc(cva_MissName, vfS , );
regA(va_SP_UpgPicture,vfS);
regA(va_SP_UpgBut,vfS,vfL,vfR);
regA(va_SP_UpgStageLine,vfS);
regA(va_AR_MenuScroll,vfS);
regA(cva_MM_Start,vfL);
regA(cva_MM_StartInGame,vfL);
regAc(cva_MM_StartInDemo, vfL, );
regA(cva_MM_Close,vfL);
regAx(cva_MM_MultiCreate,cva_MM_Start,vfL,vfS);
regAx(cva_MM_MultiJoin,cva_MM_Start,vfL,vfS);
regAx(cva_MM_MultiEnter,cva_MM_Start,vfL);
regA(cva_MM_MultiBack,vfL,vfS); //cva_MM_Close
regA(cva_MM_MultiStart,vfS,vfL);
regAc(cva_MM_SinglStart,vfS vfL
	bool NationConvertUnits,
	REG_MEMBER(_bool,NationConvertUnits);
);
regAc(cva_MM_DialogsEditor, vfL 
	bool Ctrl,
	REG_MEMBER(_bool,Ctrl);
);
//
regAc(cva_Multi_ManualServer,vfS,);
regAc(cva_Multi_ManualIPServer,vfS,);
regAx(cva_LD_MultiGame,va_ListDesk,vfS);
//
regA(cva_MM_RunMapEditor,vfL);
regAc(cva_MM_RunRecordGame,vfL
	bool Credits;
	,
	REG_MEMBER(_bool, Credits);
);
regAc(cva_NullHint, vfS);
regAc(cva_ShowAboutText, vfD);
regAc(cva_CreditsDesk, vfS);
regAc(cva_MM_RunCredits, vfL);
regAc(cva_MM_RunShopingDemo,vfL
	_str Rolik;
	_str Demka;
	 ,
	 REG_AUTO(Rolik);
	 REG_AUTO(Demka);
);
//
regAc(cva_MM_LobbyVersion,vfS,);
//
regA(cva_MM_RunSingleMiss,vfL);
regAx(cva_MM_BackToMain,cva_MM_Close,vfL);
regAx(cva_MM_ReplayCurMiss,cva_MM_Close,vfL);
regA(cva_ResPanel_Money,vfS);
regA(cva_SP_EnableAttack,vfS,vfL,vfRt);
regA(va_UnitPortret_Small,vfS);
regA(cva_WeapMiniDamPort,vfS);
regA(cva_WeapMiniDam,vfS);
regA(cva_WeapMiniDefPort,vfS);
regA(cva_WeapMiniDef,vfS);
regA(cva_SP_id_Amount,vfS);
regA(va_SP_Amount,vfS);
regAx(va_SP_AmountMini,cva_SP_id_Amount,vfS);
regA(cva_SP_id_Life,vfS);
regA(cva_SP_id_LifeLineHor,vfS);
regA(cva_SP_Patrol,vfS,vfL,vfRt);
regA(cva_SP_Guard,vfS,vfL,vfRt);
regA(cva_SP_Activity0,vfS,vfL,vfRt);
regA(cva_SP_Activity12,vfS,vfL,vfRt);
regA(cva_MM_Profile,vfL);
regA(cva_MM_Campaign,vfL);
regA(cva_GM_MessPic,vfS);
regA(cva_GM_MessTxt,vfS);
regAx(cva_F1_Start,cva_MM_StartInGame,vfL);
regA(cva_MM_StartInGameModal,vfL);
regA(cva_F1_Object,vfS);
regA(cva_F1_History);
regA(cva_F1_VictoryCond,vfS);
regA(cva_Opt_VMode,vfSi,vfI,vfA);
regAx(cva_MM_Accept,cva_MM_Close,vfL);
regAx(cva_MM_Cancel,cva_MM_Close,vfL);
//
regAc(cva_Opt_Options,vfS vfI vfA
	ClassRef<CheckBox> FriendlyFire;
	ClassRef<CheckBox> ShowHint;
	ClassRef<CheckBox> ShowVideo;
	ClassRef<ComboBox> ShadowQuality;
	ClassRef<ComboBox> AnimationQuality,
	REG_AUTO(FriendlyFire);
	REG_AUTO(ShowHint);
	REG_AUTO(ShowVideo);
	REG_AUTO(ShadowQuality);
	REG_AUTO(AnimationQuality);
);
regAc(cva_Opt_Subtitres, vfI vfA, );
regAc(cva_Opt_ArcadeMode, vfI vfA vfS, );
regA(cva_Opt_AnimQuality,vfSi,vfI,vfA);
regA(cva_Opt_SoundVolume,vfSi,vfI,vfA);
regA(cva_Opt_MusicVolume,vfS,vfI,vfA);
regA(cva_Opt_RequiredMsPerFrame_Scr,vfSi,vfI,vfA);
regA(cva_Opt_RequiredMsPerFrame_Dig,vfS);
regA(cva_Opt_ScrollSpeed,vfSi,vfI,vfA);
regA(cva_Opt_MusicPlayMode,vfS,vfI,vfA);
regA(cva_Opt_WaterQuality,vfSi,vfI,vfA);
regAc(cva_Opt_SelectionType, vfI vfA , );
//
regAc(cva_SP_AllowShoot, vfS vfL vfRt , );
//
regAc(cva_BR_PlPing,vfS vfI,;);
regAc(cva_BR_PlName,vfS vfI);
regAc(cva_BR_PlRace, vfS vfI);
regAc(cva_BR_Ready, vfS vfI);

regAc(cva_BR_PlColor,vfS vfI vfL vfR
	bool OneSprited;
	,
	REG_MEMBER(_bool,OneSprited);
);
regA(cva_BR_PlTeam,vfS,vfL,vfR);//,vfI
regA(cva_BR_PlMapLoading,vfS);
//regA(cva_BR_PlVersion);
regA(cva_BR_MapPict);
regAx(cva_BR_MapList,va_ListDesk,vfS,vfI);
regAx(cva_BR_SavList,va_ListDesk,vfS,vfI);
regA(cva_BR_SetSkirBatl,vfL,vfS);
regA(cva_BR_TabMap,vfS,vfL);
regA(cva_BR_ChatDesk,vfSi,vfI);
regAc(cva_BR_ChatInput,vfS
	ClassRef<ChatDesk> Desk;
	,
	REG_AUTO(Desk);
);
regA(cva_BR_ChatEnter,vfL);
regAc(cva_MU_NameInput,vfS vfI vfA
	ClassRef<SimpleDialog> sdAccept;
	,
	REG_AUTO(sdAccept);
);
regAc(cva_C2_HintMessage, vfS, );
regA(cva_OIS_ArtDepo, vfS);
regA(cva_MU_NickInput,vfS);
regAc(cva_DemoDisable, vfS , );
regAx(cva_LS_FileList,va_ListDesk,vfS int prev; ,vfI);
regA(cva_LS_LoadName,vfSi,vfI);
regA(cva_LS_SaveName,vfS,vfI);
//
regAc(cva_DeleteSaveFile, vfS vfL
	  _str ModalDesk,
	  REG_AUTO(ModalDesk);
);
regAc(cva_DeleteButton, vfL
	  bool Delete,
	  REG_MEMBER(_bool, Delete);
);
//
regAx(cva_LoadInGame_Accept,cva_MM_Accept,vfS vfL);
regAx(cva_SaveInGame_Accept,cva_MM_Accept,vfS vfL);
regAc(cva_ConfirmSaveFileName, vfL);
regA(cva_H_TaskList,vfSi,vfI);
regA(cva_H_TalkList,vfSi,vfI);
regA(cva_H_HintList,vfSi,vfI);
//
regA(cva_SP_in_Box,vfL vfRt);
regA(cva_SP_LeaveAll_Box,vfL vfRt);
regA(cva_SP_in_Pic,vfS);
regA(cva_SP_in_Amount,vfS);
//for fantasy
regA(cva_Inf_ShowExperience,vfS);
regA(cva_Inf_ShowLevel,vfS);
regA(cva_Inf_ShowMana,vfS);
regA(cva_Inf_ShowSpeed,vfS);
regA(cva_Inf_ShowStamina,vfS);
regA(cva_Inf_ShowAttSpeed,vfS);
regAc(cva_Inf_ShowRomeMorale,vfS);
regAc(cva_Inf_BrigExp_Rome,vfS
	  int Level,
	  REG_MEMBER(_int,Level);
);
//
regAc(cva_Peasant_Idle, vfS vfL vfRt);
regAc(cva_Peasant_AutoWork, vfS vfL vfRt);
//
regAc(cva_HotKeyButton,vfS vfL vfRt);
//
regA(cva_Sin_ProfName,vfI,vfS);
regA(cva_ProfAdd_Race,vfI,vfS);
regA(cva_ProfAdd_RaceFlg,vfS);
regA(cva_ProfAdd_Diff,vfS,vfI);
regA(cva_ProfAdd_Name,vfS,vfI);
regA(cva_ProfAdd_Port,vfS);
regA(cva_ProfAdd_Desc,vfS);
//
regAc(cva_ProfAdd_PortScr, vfI vfS vfL);
regAc(cva_ProfAdd_PortScrBut, vfL);
//
regAx(cva_ProfAdd_Accept,cva_MM_Close,vfL,vfS);
regAx(cva_ProfAdd_Cancel,cva_MM_Cancel,vfS);
//
regAc(cva_SPD_CampMessageCheck, vfL
	ClassRef<SimpleDialog> Button,
	REG_AUTO(Button);
);
regAc(cva_ProfSave, vfL, );
regAc(cva_ProfLoad, vfL, );
//
regAc(cva_MixWithWhite, vfS
	DWORD Color,
	REG_MEMBER(_color,Color);
);
//
regAx(cva_ProfList,va_ListDesk,vfS,vfI,vfA);
regAx(cva_ProfSel_Accept,cva_MM_Close,vfL,vfS);
regAx(cva_ProfSel_Add,cva_MM_Start,vfS);
regAx(cva_ProfSel_Cancel,cva_MM_Cancel,vfS,vfL);
regAc(cva_ProfCur_Desc,	vfS;	
	bool Init;
	int NatID;
	int HeroID; , ;	
);
regA(cva_ProfCur_Name,vfS);
regA(cva_ProfCur_Race,vfS);
//regA(cva_ProfCur_RaceFlg);//,vfS);
regA(cva_ProfCur_Rank,vfS);
regA(cva_ProfCur_Diff,vfS);
regA(cva_ProfCur_Port,vfS);
regA(cva_ProfCur_Ocup,vfS);
//regA(cva_ProfCur_Desc);//,vfS);
regA(cva_ProfDelete,vfL);
regA(cva_ProfStats,vfL);
regA(cva_ProfDel_Accept,vfL);
regA(cva_ProfDel_Cancel,vfL);
regA(cva_ProfDel_Desk,vfS);
regAx(cva_MM_SinStart,cva_MM_Start,vfL);
//
regA(cva_Mess_CenterQuest,vfS);
regA(cva_Mess_Quests,vfS);
regA(cva_Mess_QuestsButton,vfS,vfL,vfRt);
regA(cva_Mess_Dialogs,vfS);
//
regA(cva_M_ModalDesk,vfS);
regA(cva_M_ModalDeskSet,vfL);
regA(cva_M_ModalDeskBack,vfL);
//
//regA(cva_M_RunCurMiss,vfL);
regA(cva_M_Accept,vfL);	// not close
regAx(cva_M_ModDesSetRetChang,cva_M_ModalDeskSet,vfL);
regA(cva_M_AcceptSettings,vfL);
regA(cva_M_Coss2_Relax,vfS);
regA(cva_GameTime,vfS);
regA(cva_MM_StartInternet,vfL);
regA(cva_M_MultRoom_Back,vfS,vfL); //cva_MM_Cancel
regA(cva_SP_in_AmountMax,vfS);
regA(va_MissMapBMP,vfS);
regAc(cva_U_Info_Switch, vfS vfL vfRt
	,
);
regAc(cva_U_InfoDesk, vfS vfLt vfRt
	ClassRef<VitButton> Life;
	ClassRef<VitButton> BuildStage;
	ClassRef<VitButton> RastrataNaVistrel;
	ClassRef<VitButton> StandGroundTime;
	ClassRef<VitButton> SkillDamageBonus,
	REG_AUTO(Life);
	REG_AUTO(BuildStage);
	REG_AUTO(RastrataNaVistrel);
	REG_AUTO(StandGroundTime);
	REG_AUTO(SkillDamageBonus);
);
regAc(cva_U_HistoryMess, vfS);
regAc(cva_U_HistoryTitle, vfS);
regAc(cva_U_HistoryPicture, vfS);
//
//
regA(cva_BR_VirginPlayers, vfL);

regAc(cva_BR_InetGo2Chat, vfS vfL
	,
);
regAc(cva_BR_SetVisibleIfInet, vfS,);
regAc(cva_BR_SetVisibleIfLAN, vfS,);

regAc(cva_U_Info, vfS vfLt vfRt
	ClassRef<SimpleDialog>	Ability_Upgrade;
	ClassRef<SimpleDialog>	SDInfo;
	ClassRef<GPPicture>		EncBackGround;
	ClassRef<VitButton>		SeparatorA;
	ClassRef<VitButton>		SeparatorB;
	ClassRef<VitButton>		SeparatorC;
	ClassRef<VitButton>		SeparatorD;
	ClassRef<VitButton>		SeparatorE;
	ClassRef<VitButton>		SeparatorF;
	ClassRef<GPPicture>		UASeparator;
	ClassRef<GPPicture>		ISeparator;
	ClassRef<TextButton>	LDescription;
	ClassRef<TextButton>	Title;
	ClassRef<TextButton>	Info;
	ClassRef<GPPicture>		Icon;
	ClassRef<GPPicture>		IconBack;
	ClassRef<GPPicture>		Icon2Back;
	ClassRef<TextButton>	Wood;
	ClassRef<TextButton>	Stone;
	ClassRef<TextButton>	Gold;
	ClassRef<TextButton>	Food;
	ClassRef<TextButton>	Iron;
	ClassRef<TextButton>	LLife;
	ClassRef<TextButton>	Life;
	ClassRef<TextButton>	LBuildingTime;
	ClassRef<TextButton>	BuildingTime;
	ClassRef<TextButton>	LCost;
	ClassRef<TextButton>	Cost;
	ClassRef<TextButton>	LSpeed;
	ClassRef<TextButton>	Speed;
	ClassRef<TextButton>	LVision;
	ClassRef<TextButton>	Vision;
	ClassRef<TextButton>	StrikeProbability;
	ClassRef<TextButton>	VeteranExpert;
	ClassRef<TextButton>	AttackSpeed;
	ClassRef<TextButton>	AttackRadius;
	ClassRef<GPPicture>		AttackType;
	ClassRef<TextButton>	AttackDamage;
	ClassRef<TextButton>	AUII;
	ClassRef<TextButton>	AUIII;
	ClassRef<TextButton>	AUIV;
	ClassRef<TextButton>	Chopping;
	ClassRef<TextButton>	Piercing;
	ClassRef<TextButton>	Crushing;
	ClassRef<TextButton>	DUIICh;
	ClassRef<TextButton>	DUIIICh;
	ClassRef<TextButton>	DUIVCh;
	ClassRef<TextButton>	DUIIP;
	ClassRef<TextButton>	DUIIIP;
	ClassRef<TextButton>	DUIVP;
	ClassRef<TextButton>	DUIICr;
	ClassRef<TextButton>	DUIIICr;
	ClassRef<TextButton>	DUIVCr;
	ClassRef<TextButton>	Spread;
	ClassRef<TextButton>	LSpread;
	ClassRef<TextButton>	LStrikeProbability;
	ClassRef<TextButton>	LVeteranExpert;
	ClassRef<TextButton>	LDefenceUpgrades;
	ClassRef<TextButton>	LAttackUpgrades;
	ClassRef<TextButton>	LAttackIIIIIIV;
	ClassRef<TextButton>	LDefenceIIIIIIV;
	ClassRef<GPPicture>		AbilityA;
	ClassRef<GPPicture>		AbilityB;
	ClassRef<GPPicture>		AbilityC;
	ClassRef<GPPicture>		AbilityD;
	ClassRef<GPPicture>		AbilityE;
	ClassRef<GPPicture>		AbilityF;
	ClassRef<GPPicture>		AbilityG;
	ClassRef<GPPicture>		AbilityH;
	ClassRef<GPPicture>		AbilityI;
	ClassRef<GPPicture>		AbilityJ;
	ClassRef<TextButton>	LSpecific;
	ClassRef<TextButton>	LAbilities;
	ClassRef<TextButton>	LAbilitiesSeparator;
	ClassRef<TextButton>	LUpgrades,
	REG_AUTO(Ability_Upgrade);
	REG_AUTO(SDInfo);
	REG_AUTO(EncBackGround);
	REG_AUTO(SeparatorA);
	REG_AUTO(SeparatorB);
	REG_AUTO(SeparatorC);
	REG_AUTO(SeparatorD);
	REG_AUTO(SeparatorE);
	REG_AUTO(SeparatorF);
	REG_AUTO(UASeparator);
	REG_AUTO(ISeparator);
	REG_AUTO(LDescription);
	REG_AUTO(Title);
	REG_AUTO(Info);
	REG_AUTO(Icon);
	REG_AUTO(IconBack);
	REG_AUTO(Icon2Back);
	REG_AUTO(Wood);
	REG_AUTO(Stone);
	REG_AUTO(Gold);
	REG_AUTO(Food);
	REG_AUTO(Iron);
	REG_AUTO(LLife);
	REG_AUTO(Life);
	REG_AUTO(LBuildingTime);
	REG_AUTO(BuildingTime);
	REG_AUTO(LCost);
	REG_AUTO(Cost);
	REG_AUTO(LSpeed);
	REG_AUTO(Speed);
	REG_AUTO(LVision);
	REG_AUTO(Vision);
	REG_AUTO(StrikeProbability);
	REG_AUTO(VeteranExpert);
	REG_AUTO(AttackSpeed);
	REG_AUTO(AttackRadius);
	REG_AUTO(AttackType);
	REG_AUTO(AttackDamage);
	REG_AUTO(AUII);
	REG_AUTO(AUIII);
	REG_AUTO(AUIV);
	REG_AUTO(Chopping);
	REG_AUTO(Piercing);
	REG_AUTO(Crushing);
	REG_AUTO(DUIICh);
	REG_AUTO(DUIIICh);
	REG_AUTO(DUIVCh);
	REG_AUTO(DUIIP);
	REG_AUTO(DUIIIP);
	REG_AUTO(DUIVP);
	REG_AUTO(DUIICr);
	REG_AUTO(DUIIICr);
	REG_AUTO(DUIVCr);
	REG_AUTO(Spread);
	REG_AUTO(LSpread);
	REG_AUTO(LStrikeProbability);
	REG_AUTO(LVeteranExpert);
	REG_AUTO(LDefenceUpgrades);
	REG_AUTO(LAttackUpgrades);
	REG_AUTO(LAttackIIIIIIV);
	REG_AUTO(LDefenceIIIIIIV);
	REG_AUTO(AbilityA);
	REG_AUTO(AbilityB);
	REG_AUTO(AbilityC);
	REG_AUTO(AbilityD);
	REG_AUTO(AbilityE);
	REG_AUTO(AbilityF);
	REG_AUTO(AbilityG);
	REG_AUTO(AbilityH);
	REG_AUTO(AbilityI);
	REG_AUTO(AbilityJ);
	REG_AUTO(LSpecific);
	REG_AUTO(LAbilities);
	REG_AUTO(LAbilitiesSeparator);
	REG_AUTO(LUpgrades);
);

//////////////////////////////////////////////////////////////////////////
struct MultStartErr{
	bool Color[8];
	bool Team[8];
};
extern MultStartErr v_MSErr;
//////////////////////////////////////////////////////////////////////////
DLLEXPORT int GetRND(int Max);
extern AI_Description GlobalAI;
extern DWORD vWaitServerInfo;
extern char LobbyVersion[32];
extern int sfVersion;
void CreateNationalMaskForMap(char* Name);
extern word COMPSTART[8];
CEXPORT	void StartAIEx(byte Nat,char* Name,int Land,int Money,int ResOnMap,int Difficulty);
bool TestFillingAbility(OneObject* OB);
int GetMonsterAccessControl(byte NI, word NIndex, char** acMessage);
int GetLastAnswerT(DWORD ID);
extern bool v_MM_Host;
int GetAmountOfExtraOptions();
const char* GetExtraOptionName(int idx);
int GetExtraOptionAmountOfElements(int idx);
bool GetExtraOptionsCompartability(int idx,int& eidx,int* OptionsValuesArray);
const char* GetExtraOptionElementName(int idx,int eidx);
CIMPORT void StopPlayCD();
void PlayRandomTrack();
bool isFinishedMP3();
CEXPORT void PlayEffect(int n,int pan,int vol);
extern bool InEdit;
extern bool mMapPreview;
//////////////////////////////////////////////////////////////////////////
#pragma warning ( pop )