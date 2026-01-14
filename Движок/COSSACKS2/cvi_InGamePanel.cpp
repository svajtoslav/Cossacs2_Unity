#include "stdheader.h"
#include ".\cvi_MainMenu.h"
#include ".\cvi_campaign.h"
#include ".\cvi_singleplayerdata.h"
#include ".\cvi_InGamePanel.h"
#include ".\cvi_HeroButtons.h"
#include "BE_BaseClasses.h"
//////////////////////////////////////////////////////////////////////////
cvi_InGamePanel vmIGP;
//const char* vmSinglePlayerDataXML="dialogs\\SinglePlayerData.xml";
//////////////////////////////////////////////////////////////////////////
cvi_InGamePanel::cvi_InGamePanel(){
	Start=0;
	Open=0;
	Show=0;
	Close=0;
}
//
bool igpStart=false;
bool vIGPanelMode=false;
//
void cvi_InGamePanel::StartFrame(char* Message, int OpenTime, int ShowTime, int CloseTime){	
	if(vGameMode == gmCamp&&vmCamp){
		SinglePlayerData_HeroesInfoList* HI=vmSinglePlayerData.Heri[vmCampID]->CampaignMissions[vmCamp->curMission];
		if(HI){
			if(vmCamp->curMission>0){
				SinglePlayerData_HeroesInfoList* mHI=vmSinglePlayerData.Heri[vmCampID]->CampaignMissions[vmCamp->curMission-1];
				if(mHI){
					HI->FreeExp=mHI->FreeExp;
				}
			}else{
				HI->FreeExp=0;
			}
			//if(HI->FreeExp>0) v_MainMenu.StartDS("HeroesUpgrades", true);
		}
		Start=GetTickCount()+2000;
	}else{
		Start=GetTickCount();
	}
	Mess=Message;	
	Open=OpenTime;
	Show=ShowTime;
	Close=CloseTime;
	igpStart=true;
	vIGPanelMode=true;;
};
//////////////////////////////////////////////////////////////////////////
void cva_IGP_Frame::SetFrameState(SimpleDialog* SD){
	//vIGPanelMode=false;
	int t=GetTickCount()-vmIGP.Start;
	SD->Visible=(t<vmIGP.Open+vmIGP.Show+vmIGP.Close);
	if(SD->Visible){
		TextButton* tbMess=Mess.Get();
		if(tbMess){
			tbMess->SetMessage(vmIGP.Mess);
			int left,top,right,bottom;
			SD->GetMargin(left,top,right,bottom);
			int w=tbMess->GetWidth();
			//int h=tbMess->GetHeight();
			SD->SetWidth(w+left+right+60);
			//SD->SetHeight(h+top+bottom+15);
		}
		SD->DeepColor=true;
		byte Alpha=0xFF;
		if(t<vmIGP.Open){
			Alpha=0xFF*t/vmIGP.Open;
		}else
		if(t>vmIGP.Open+vmIGP.Show){
			t=vmIGP.Open+vmIGP.Show+vmIGP.Close-t;
			Alpha=0xFF*t/vmIGP.Close;
		}
		if(t<0){
			Alpha=0x00;			
		}else{
			vIGPanelMode=true;
		}
		SD->Diffuse=0x00FFFFFF+0x1000000*Alpha;
	}else{
		vIGPanelMode=false;
		if(t>vmIGP.Open+vmIGP.Show+vmIGP.Close){
			byte NI = GSets.CGame.cgi_NatRefTBL[MyNation]; // Nation Index
			extern cvi_HeroButtons vHeroButtons;
			OneObject *oo = vHeroButtons.GetObject(NI, 0);
			if(oo)
			{
				vIGPanelMode=false;
				if(igpStart&&vGameMode==gmCamp&&vmCamp){
					igpStart=false;
					if( vmCamp->curMission < vmSinglePlayerData.Heri[vmCampID]->CampaignMissions.GetAmount() &&
						vmCampID < vmSinglePlayerData.Heri.GetAmount() ){
						SinglePlayerData_HeroesInfoList* HI=vmSinglePlayerData.Heri[vmCampID]->CampaignMissions[vmCamp->curMission];
						if(HI&&HI->FreeExp>0) v_MainMenu.StartDS("HeroesUpgrades", true);

					}
				}
			}
		}		
	}
}
// vCreditsMode
extern bool vCreditsMode;
void cva_IGP_Credits::SetFrameState(SimpleDialog* SD){
	SD->Visible=vCreditsMode;
}