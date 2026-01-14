#include "stdheader.h"
#include "WeaponSystem.h"
#include ".\vui_GlobalHotKey.h"
#include ".\BE_HEADERS.h"
//////////////////////////////////////////////////////////////////////////
vui_GlobalHotKeys v_GlobalHotKeys;
char* v_GlobalHotKeysXML="dialogs\\interface\\HotKeys.xml";
//////////////////////////////////////////////////////////////////////////

#define reg_HotKey(x,keyID,ctrl,shift,alt) { x* v=new x; v->KeyID=keyID; v->Ctrl=ctrl; v->Shift=shift; v->Alt=alt; v_GlobalHotKeys.HotKeys.Add(v); }
//
char* GetKeyName(word Key);
const char* vui_GlobalHotKey::GetThisElementView(const char* LocalName){
	static _str str;
	str=LocalName;
	str+=": ";
	if(Ctrl) str+="Ctrl+";
	if(Shift) str+="Shift+";
	if(Alt) str+="Alt+";
	str.print("%s",GetKeyName(KeyID));
	return str.str;
};
void vui_GlobalHotKeys::Process(){
	if(KeyPressed&&LastKey){
		int n=HotKeys.GetAmount();
		for(int i=0;i<n;i++){
			vui_GlobalHotKey* key=HotKeys[i];
			if(LastKey==key->KeyID&&
				(key->Ctrl==((GetKeyState(VK_CONTROL)&0x8000)!=0))&&
				(key->Shift==((GetKeyState(VK_SHIFT)&0x8000)!=0))&&
				(key->Alt==((GetKeyState(VK_MENU)&0x8000))!=0)){
					key->Action();
					LastKey=0;
					KeyPressed=0;
					return;
				}
		}
	}
};

void vui_GlobalHotKeys::DrawHelp(){
	if(ShowHelp){
		char str[128];
		RLCFont* f=&SmallWhiteFont;
		int x=350,y=50,dy=16;
		ShowString(x,y,"List of HotKeys",f); y+=dy; x+=50;
		f=&SmallYellowFont;
		int n=HotKeys.GetAmount();
		for(int i=0;i<n;i++){
			vui_GlobalHotKey* key=HotKeys[i];
			DString str;
			str+=key->GetName();
			str+=": ";
			if(key->Ctrl) str+="Ctrl+";
			if(key->Shift) str+="Shift+";
			if(key->Alt) str+="Alt+";
			int l=strlen(str.str);
			str.Allocate(l+2);
			str.str[l]=key->KeyID;			
			str.str[l+1]=0;
			ShowString(x,y,str.str,f); y+=dy;
		}
	}
};
/*
void vui_GHK_Help::Action(){
	v_GlobalHotKeys.ShowHelp=!v_GlobalHotKeys.ShowHelp;
}

void vui_GHK_ReloadUnitInterface::Action(){
	vgf_UI_Clear();
}
void vui_GHK_VScripts::Action(){
	extern bool MS_LV_VIS;
	MS_LV_VIS = true;
}
void vui_GHK_VBattle::Action(){
	void	ProcessBattleEditor();
	ProcessBattleEditor();
}
void vui_GHK_AIEditor::Action(){
	void ProcessAIEditor();
	ProcessAIEditor();
}
void vui_GHK_DialogEditor::Action(){
	void ProcessDialogsEditor();
	ProcessDialogsEditor();
}
*/
//////////////////////////////////////////////////////////////////////////
//
extern byte LockGrid;
extern bool TransMode;
extern byte TopTypeMode;
extern bool MiniActive;
extern bool Recreate;
extern byte PlayGameMode;
bool CheckFlagsNeed();
extern bool OptHidden;
extern int InfDX;
extern int Inform;
extern int InfDX0;
extern bool HealthMode;
extern byte SpecCmd;
extern int tmtmt;
extern bool LockPause;
//
void vui_GHK_ImpassableZones::Action(){
	if(EngSettings.DebugTopologyMode){
		if(!(GetKeyState(VK_CONTROL)&0x8000)) LockGrid++;
		if(LockGrid>NMFIELDS) LockGrid=0;
		if(GetKeyState(VK_CONTROL)&0x8000) TopTypeMode++;
		if(TopTypeMode>NMFIELDS) TopTypeMode=0;
	}else{
		LockGrid=!LockGrid;
	}
	MiniActive=0;
	Recreate=1;
};
void vui_GHK_Transparency::Action(){
	//if(MsPerFrame)MsPerFrame--;
	if(GetKeyState(VK_CONTROL)&0x8000){
		if(PlayGameMode==2||CheckFlagsNeed()){
			OptHidden=!OptHidden;
			if(!OptHidden){
				Inform=0;
				InfDX=InfDX0;
			};
		};
	}else{
		TransMode=!TransMode;
		MiniActive=0;
		Recreate=1;
	};
};
//
int vui_GHK_HealthMode::GetState(){
	return HealthMode;
}
void vui_GHK_HealthMode::Action(){
	HealthMode=!HealthMode;
}
void vui_GHK_GoToCurSelPosition::Action(){
	SpecCmd=111;
}
void vui_GHK_GoToLastActions::Action(){ 
	SpecCmd=112;
}
void vui_GHK_Pause::Action(){ 
	if(tmtmt>32&&!LockPause)SpecCmd=137;
}
void vui_GHK_GoAndAttackMode::Action(){ 
	if(NSL[MyNation])GoAndAttackMode=1;
}
//
extern bool MiniMapVisible;
extern bool FullMini;
//
int vui_GHK_MiniMapMode::GetState(){
	if(!MiniMapVisible){
		return 2;
	}
	if(!FullMini){
		return 1;
	}else{
		return 0;
	}	
};
void vui_GHK_MiniMapMode::Action(){ 
	if(!MiniMapVisible){
		FullMini=1;
		MiniMapVisible=1;
	}else{
		if(FullMini)FullMini=0;
		else MiniMapVisible=0;
	}
	//
	MiniActive=0;
	Recreate=1;
}
// vui_GHK_AddActiveWeapon
void vui_GHK_AddActiveWeapon::Action(){
	AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
	AWP->Damage=Damage;
	AWP->AttType=AttType;
	AWP->Radius=Radius;
	//
	extern int LastMx;
	extern int LastMy;
	extern void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, AdditionalWeaponParams* AddParams);
	//
	CreateNewActiveWeapon(WeaponName.str,0xFFFF,LastMx,LastMy,0,0xFFFF,LastMx+300,LastMy,0,AWP);
}
// vui_GHK_TaskHint
int vui_GHK_TaskHint::GetState(){
	vvMESSGES* pMM = g_TASK_HINT_OBJ();
	return pMM?pMM->GetMessDS_Visbility():-1;
}
void vui_GHK_TaskHint::Action(){ 
	vvMESSGES* pMM = g_TASK_HINT_OBJ();
	if(pMM)pMM->SetMessDS_Visbility(!pMM->GetMessDS_Visbility());
}
//
extern bool LMode;
int vui_GHK_LMode::GetState(){
	return LMode;
}
extern int MinMapX;
extern int MaxMapX;
extern int MinMapY;
extern int MaxMapY;
void vui_GHK_LMode::Action(){ 
	if( MaxMapX-MinMapX>180 && MaxMapY-MinMapY>180 ){
		void ReverseLMode();
		ReverseLMode();
	}
}
//
void vgf_CreateHotKeysArray(){

	REG_CLASS(vui_GlobalHotKey);
	//
	REG_CLASS(vui_GHK_ImpassableZones);
	REG_CLASS(vui_GHK_Transparency);
	REG_CLASS(vui_GHK_HealthMode);
	REG_CLASS(vui_GHK_GoToCurSelPosition);
	REG_CLASS(vui_GHK_GoToLastActions);
	REG_CLASS(vui_GHK_Pause);
	REG_CLASS(vui_GHK_GoAndAttackMode);	
	REG_CLASS(vui_GHK_MiniMapMode);
	REG_CLASS(vui_GHK_LMode);
	REG_CLASS(vui_GHK_TaskHint);
	REG_CLASS(vui_GHK_AddActiveWeapon);
	//	
	/*
	reg_HotKey(vui_GHK_Help,VK_F1,true,true,true);
	reg_HotKey(vui_GHK_ReloadUnitInterface,'R',true,true,true);
	///reg_HotKey(vui_GHK_VScripts,'E',1,0,0);
	reg_HotKey(vui_GHK_VBattle,'B',1,0,1);
	reg_HotKey(vui_GHK_AIEditor,'U',1,1,1);
	reg_HotKey(vui_GHK_DialogEditor,'U',1,1,0);
	*/
}
//////////////////////////////////////////////////////////////////////////
void cva_HotKeyAction::SetFrameState(SimpleDialog* SD){
	vui_GlobalHotKey* A=Action.Get();
	if(A){
		SD->HotKey=A->KeyID;
		VitButton* VB=dynamic_cast<VitButton*>(SD);
		if(VB){
			int state=A->GetState();
			if(state==-1){
				VB->Visible=false;
			}else{
				VB->Visible=true;
				VB->State=state;
			}			
		}
	}
}
bool cva_HotKeyAction::LeftClick(SimpleDialog* SD){
	vui_GlobalHotKey* A=Action.Get();
	if(A){
		A->Action();
		LastKey=0;
		KeyPressed=0;
	}
	return true;
}
//////////////////////////////////////////////////////////////////////////