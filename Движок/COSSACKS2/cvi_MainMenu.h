#pragma once
#include "MapTemplates.h"
#include "vui_Action.h"
//
//#define _DEMO_
//
//////////////////////////////////////////////////////////////////////////
class SimpleDialog;
class DialogsSystem;
//////////////////////////////////////////////////////////////////////////
class cvi_MainMenu: public BaseClass{
public:
	//DialogsSystem* DS;	
	Enumerator* Dialogs;
	//ClassArray<DialogsSystem*> Dialogs;
	//
	DialogsSystem dsLoading;
	ClonesArray<_str> lMark;
	DynArray<int> lMarkVal;
	int lMarkValMax;
	void AddLoadMark(char* Mark, int Value);
	void ClearLoadMark();
	void ShowLoadProgress(char* Mark, int v, int vMax);
	//
	//bool LoadXmlFile(char* FileName);
	//void SaveXmlFile(char* FileName);
	cvi_MainMenu();
	bool Exit;	
	bool GameMode;
	bool StartDS(char* ID, bool gMode=false);	
	void CloseDS();
	bool Enable;
	bool Init();
	//
	_str ModalDesk;
	_str ModalDeskBack;
	void SetModalDesk(char* str);
	void BackModalDesk();
	//
	int LastLPress;	// Time of last LPressed in GetTickCount
	//
	int Cross; // 0 - without, 1 - begin, 2 - end, 3 - start leftclick
	int CrossTime;
	SimpleDialog* CrossSD;
	//	
	void ProcessDSinGame();
	void RestoreDSinGame();
	_str vMD;
	DialogsSystem* vDS;
};
extern cvi_MainMenu v_MainMenu;
//////////////////////////////////////////////////////////////////////////
class cvi_GPFile: public BaseClass{
public:
	short FileID;
	SAVE(cvi_GPFile){
		REG_MEMBER(_gpfile,FileID);
	}ENDSAVE;
};
class cvi_NatPics: public BaseClass{
public:
	ClonesArray<cvi_GPFile> Pics;
	SAVE(cvi_NatPics){
		REG_AUTO(Pics);
	}ENDSAVE;
};
class cvi_InterfaceSystem: public BaseClass{
public:
	cvi_InterfaceSystem(){ Init(); };
	virtual void reset_class(void* DataPtr){
		BaseClass* B=(BaseClass*)DataPtr;
		B->BaseClass::reset_class(B);
		cvi_InterfaceSystem* IS=dynamic_cast<cvi_InterfaceSystem*>(B);
		if(IS)IS->Init();
	};
	void Init();
	_str EW2_CampaignStats;
	int ChatTitleDy;
	int ChatStringDy;
	short GlobalUnitFileID;
	int GlobalUnitSpriteID;
	int NKillsForGuardian;
	int GlobalBrigX;
	int GlobalBrigY;
	int GlobalBrigDx;
	int bbMoraleDelta; // delta - visible condition
	int bbMoraleSpeed; // morales per frame
	int bbHideTime; // time before not visible
	int bbFade; // fading time
	int CampaignNDone;
	int CreditsLeft;
	int CreditsRight;
	int CreditsTop;
	int CreditsBottom;
	int CreditsSpeed;
	int CreditsLDis;
	_str LoadingTipsPrefix;
	int LoadingTipsAmount;
	RLCFont* CreditsFont;
	ClonesArray<cvi_GPFile> NationalPic;
	SAVE(cvi_InterfaceSystem){
		REG_FILEPATH(EW2_CampaignStats,".DialogsSystem.xml");
		REG_MEMBER(_int,ChatTitleDy);
		REG_MEMBER(_int,ChatStringDy);
		REG_MEMBER(_gpfile,GlobalUnitFileID);
		REG_SPRITE(GlobalUnitSpriteID,GlobalUnitFileID);
		REG_MEMBER(_int,NKillsForGuardian);
		REG_MEMBER(_int,GlobalBrigX);
		REG_MEMBER(_int,GlobalBrigY);
		REG_MEMBER(_int,GlobalBrigDx);
		REG_MEMBER(_int,bbMoraleDelta);
		REG_MEMBER(_int,bbMoraleSpeed);
		REG_MEMBER(_int,bbHideTime);
		REG_MEMBER(_int,bbFade);
		REG_MEMBER(_int,CampaignNDone);
		REG_MEMBER(_int,CreditsLeft);
		REG_MEMBER(_int,CreditsRight);
		REG_MEMBER(_int,CreditsTop);
		REG_MEMBER(_int,CreditsBottom);
		REG_MEMBER(_int,CreditsSpeed);
		REG_MEMBER(_int,CreditsLDis);
		REG_MEMBER(_font,CreditsFont);
		REG_AUTO(LoadingTipsPrefix);
		REG_MEMBER(_int,LoadingTipsAmount);
		REG_AUTO(NationalPic);
	}ENDSAVE;
};
extern cvi_InterfaceSystem v_ISys;
extern char* v_ISysXML;
//////////////////////////////////////////////////////////////////////////
class cvi_FontStyle: public BaseClass{
public:
	//cvi_FontStyle(){ Init(); };
	virtual void reset_class(void* DataPtr);
	short SmallFont;
	short SmallFont1;
	short SpecialFont;
	short Font;
	short BigFont;
	class cSetFonts: public BaseFunction{
	public:
		void EvaluateFunction(){
			cvi_FontStyle* FS=get_parent<cvi_FontStyle>();
			if(FS){
				SmallWhiteFont.SetGPIndex(FS->SmallFont);
				SmallRedFont.SetGPIndex(FS->SmallFont);
				SmallYellowFont.SetGPIndex(FS->SmallFont);
				SmallBlackFont.SetGPIndex(FS->SmallFont);
				SmallOrangeFont.SetGPIndex(FS->SmallFont);
				SmallGrayFont.SetGPIndex(FS->SmallFont);
				//
				SmallWhiteFont1.SetGPIndex(FS->SmallFont1);
				SmallRedFont1.SetGPIndex(FS->SmallFont1);
				SmallBlackFont1.SetGPIndex(FS->SmallFont1);
				SmallYellowFont1.SetGPIndex(FS->SmallFont1);
				//
				SpecialWhiteFont.SetGPIndex(FS->SpecialFont);
				SpecialYellowFont.SetGPIndex(FS->SpecialFont);
				SpecialRedFont.SetGPIndex(FS->SpecialFont);
				SpecialBlackFont.SetGPIndex(FS->SpecialFont);
				SpecialGrayFont.SetGPIndex(FS->SpecialFont);
				//
				WhiteFont.SetGPIndex(FS->Font);	
				YellowFont.SetGPIndex(FS->Font);
				RedFont.SetGPIndex(FS->Font);	
				BlackFont.SetGPIndex(FS->Font);	
				OrangeFont.SetGPIndex(FS->Font);
				GrayFont.SetGPIndex(FS->Font);
				//
				BigWhiteFont.SetGPIndex(FS->BigFont);
				BigYellowFont.SetGPIndex(FS->BigFont);
				BigRedFont.SetGPIndex(FS->BigFont);
				BigBlackFont.SetGPIndex(FS->BigFont);
			}
		}
	} SetFonts;
	SAVE(cvi_FontStyle){
		REG_AUTO(SetFonts);
		REG_MEMBER(_gpfile,SmallFont);
		REG_MEMBER(_gpfile,SmallFont1);
		REG_MEMBER(_gpfile,SpecialFont);
		REG_MEMBER(_gpfile,Font);
		REG_MEMBER(_gpfile,BigFont);
	}ENDSAVE;
};
extern cvi_FontStyle v_FontStyle;
extern char* v_FontStyleXML;
//////////////////////////////////////////////////////////////////////////
regAc(cva_MM_LoadingProgress, vfS
	static int Value;
	bool ShowPercent;
	char* Mess;
	,	
	REG_MEMBER(_bool,ShowPercent);
	REG_MEMBER(_textid,Mess);
);
regAc(cva_GM_CloseButtons, vfS
	//cvi_GM_CloseButtons Mode;
	,
	//REG_AUTO(Mode);
);
regAc(cva_M_CrossActivator, vfL
);
regAc(cva_M_CrossFade, vfS
	int FadeTime;
	,
	REG_MEMBER(_int,FadeTime);
);