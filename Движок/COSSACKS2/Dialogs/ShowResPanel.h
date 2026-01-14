#include "cvi_MainMenu.h"
//
int PanelGP=-1;
extern DialogsSystem GLOBHINT;
extern bool HideBorderMode;
//
extern DialogsSystem ResPan;
//father 4451763
bool ResPanInit=false;
extern bool SHOWSLIDE;
bool ShowResPanel(){
	if(!SHOWSLIDE)return true;
	if(ResPan.DSS.GetAmount()==0){
		if(ResPanInit) return false;
		bool me=v_MainMenu.Enable;
		v_MainMenu.Enable=false;
		ResPan.SafeReadFromFile("dialogs\\respanel.DialogsSystem.xml");
		v_MainMenu.Enable=me;
		ResPanInit=true;
	}
	ResPan.ProcessDialogs();
	return true;
}

void ShowRMap(){
	if(!(GSets.CGame.ViewMask&1))return;
	if(PlayGameMode==1||HideBorderMode)return;
	if(ShowResPanel()) return;

	int x=smapx+smaplx*32-140;
	if(MiniMode)x=smapx+(smaplx<<4)-140;
	int y=smapy;
	
	int ResBackW = 12;
	int ResPanelW = 1024;

	int x0=RealLx-ResPanelW;
	int RPMButX=x0-65;
	int RPLButX=x0-65*2;

	int x1 = x0+150;
	int dx0 = 140;

#ifdef COSSACKS2
	if(PanelGP==-1) PanelGP=GPS.PreLoadGPImage("Interf3\\ResPanel");
#else
	if(PanelGP==-1) PanelGP=GPS.PreLoadGPImage("Interf2\\ResPanel");
#endif

	int N=(RealLx-ResPanelW)/ResBackW+1;
	for(int i=0;i<N;i++){
		GPS.ShowGP(i*ResBackW,0,PanelGP,1,MyNation);
	};
#ifdef _USE3D
	GPS.FlushBatches();
#endif
	GPS.ShowGP(x0,0,PanelGP,0,MyNation);
	///GPS.ShowGP(RPMButX,0,PanelGP,2,MyNation);
#ifdef _USE3D
	GPS.FlushBatches();
#endif
	int MON[8];	
	GetCorrectMoney(GSets.CGame.cgi_NatRefTBL[MyNation],MON);
	for(i=0;i<6;i++){
		char gg[100];
		sprintf(gg,"%d",MON[i]);
		int x;
		int ii=i;
		if(i==FoodID)//GetResID("FOOD"))
			ii-=2;
		else
		if(i==GoldID)//GetResID("GOLD"))
			ii+=2;			
		if(MON[i]<10000000){
			ShowString(x1+ii*dx0/*-GetRLCStrWidth(gg,&YellowFont)*/,3,gg,&YellowFont);
		}else
			ShowString(x1+ii*dx0/*-GetRLCStrWidth(RES_LOT_MONEY,&YellowFont)*/,3, RES_LOT_MONEY, &YellowFont);
	};
#ifdef _USE3D
	GPS.FlushBatches();
#endif
	GLOBHINT.ProcessDialogs();
	return;
};