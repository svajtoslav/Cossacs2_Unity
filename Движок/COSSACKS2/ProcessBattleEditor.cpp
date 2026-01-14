#include "stdheader.h"	
#include "BE_HEADERS.h"

void	PreDrawGameProcess();
void	GFieldShow();
void	PostDrawGameProcess();
void	GlobalHandleMouse();
void	UnPress();

DialogsSystem	DLG00,DLG01,DLG02;
ClassEditor		EDT00,EDT01,EDT02;

extern	lvCStorage* ActivObject;
extern	lvCEdge*	pActivEdge;

bool	addNewNodeInAct = false;

lvCSquardsOnMap* pSquardsOnMap = NULL;
lvCZonesOnMap*   pZonesOnMap   = NULL;
lvCGroupsOnMap*	 pGroupsOnMap  = NULL;
lvCNodesOnMap*	 pNodesOnMap   = NULL;
lvCVValuesOnMap* pVValuesOnMap = NULL;

SingletonCBEDriveMode*		SingletonCBEDriveMode::m_pObj		=NULL;
SingletonlvCNodesMAP*		SingletonlvCNodesMAP::m_pObj		=NULL;
SingletonlvCGroupsMAP_ST*	SingletonlvCGroupsMAP_ST::m_pObj	=NULL;
Singleton_vvMAP_ST*			Singleton_vvMAP_ST::m_pObj			=NULL;
SingletonlvCBattleShema*	SingletonlvCBattleShema::m_pObj		=NULL;
SingletonlvCScriptHandler*	SingletonlvCScriptHandler::m_pObj	=NULL;

SingletonlvCShapes*			SingletonlvCShapes::m_pObj			=NULL;
lvCRotateCamera*			lvCRotateCamera::m_pObj				=NULL;
CameraPoss*					CameraPoss::m_pObj					=NULL;
// EXPERIENCE ////////////////////////////////////////////////////////////
ExperienceManager*			ExperienceManager::m_pObj			=NULL;
// TEST //////////////////////////////////////////////////////////////////

extern	BaseMesMgrST		gMessagesMap;

BE_EDIT_PANEL	MAIN_EDIT_PANEL;


#ifdef	__LUA_DEBUGGER__
	CScriptDebugger	g_LUA_DBG;
#endif//__LUA_DEBUGGER__
// PRESENTATION //////////////////////////////////////////////////////////
WCP_Editor	g_WCP_EDITOR;
void	WCP_Editor::ShowEditor(){
    SimpleEditClass("MASK PRESENTATION EDITOR",this);
};
void	WCP_Editor::LoadMASK(){
	int N=MASK_LIST.GetAmount();
	WCP.ClassList.Clear();
	WCP.SetStdPresentation();
	for (int i=0; i<N; i++){
		WCP.AddFromList(MASK_LIST[i]->MASK_FILE.str);
	};
};
void	WCP_Editor::LOAD_ALL_MASK::EvaluateFunction(){
	WCP_Editor* pPR = dynamic_cast<WCP_Editor*>(GetParent());
	if (pPR){
		pPR->LoadMASK();
	};
};
void	CPRESENTATION::EvaluateFunction(){
	BaseClass* pPR = GetParent();
	if (pPR!=NULL) {
		_str pres;
		if (dynamic_cast<lvCStorage*>(pPR))	(dynamic_cast<lvCStorage*>(pPR))->Prepare_lua();
		g_WCP_EDITOR.WCP.GetPresentation(pres,pPR);
		FILE* file=fopen(FileName.str,"w");
		if (file) 	fprintf(file,"%s",pres.str);
		fclose(file);
	};
};
//////////////////////////////////////////////////////////////////////////

// MESSAGES //////////////////////////////////////////////////////////////
void	SetCurrentHelp(tpMessageArray** ppTASKS,tpMessageArray** ppTALKS,tpMessageArray** ppHINTS){
	*ppTASKS=NULL;
	*ppTALKS=NULL;
	*ppHINTS=NULL;
	vvMESSGES* pMESSAGES = NULL;
	int N = vValuesMap()->V_VALUES.GetAmount();
	while (pMESSAGES==NULL&&N--) {
		if (vValuesMap()->V_VALUES[N]->InfID==_vvMESSGES_)
			pMESSAGES = dynamic_cast<vvMESSGES*>(vValuesMap()->V_VALUES[N]);
	};
	if (pMESSAGES!=NULL){
		*ppTASKS = &(pMESSAGES->TASKS);	
//		*ppTALKS = &(pMESSAGES->TALKS);
//		*ppHINTS = &(pMESSAGES->HINTS);
	};
};

// BE_EDIT_PANEL /////////////////////////////////////////////////////////
BE_EDIT_PANEL::BE_EDIT_PANEL(){
	Error=true;
	Visible=false;
	X0=25;
	Y0=60;
	dY=20;
	tX=10;
	mW=50;
};
BE_EDIT_PANEL::~BE_EDIT_PANEL(){
};
bool			BE_EDIT_PANEL::EP_LOAD(){
	if (Error==false)	return Error;
	xmlQuote	xmlData( "EDIT_PANEL" ); 
	if (xmlData.ReadFromFile( "Dialogs\\BE_EDIT_PANEL.DialogsSystem.xml" )){
		ErrorPager	Err;
		DS_PANEL.Load( xmlData,&DS_PANEL,&Err );
		EP_PANEL_VISIBLE(false);
		Error=false;
	};
	return Error;
};
bool			BE_EDIT_PANEL::EP_DRAW(){
	if (Error)	return false;
	DS_PANEL.ProcessDialogs();
	return true;
};
bool			BE_EDIT_PANEL::EP_UPDATE(){
	if (Error)	return Error;
	EP_PANEL_VISIBLE(Visible);
	EP_TEXT_VISIBLE(Visible);
	EP_PROPER_SIZE_POS();
	return Error;
};
DialogsDesk*	BE_EDIT_PANEL::EP_PANEL(){
    if (Error)	return NULL;
	DialogsDesk* pPANEL=NULL;
	int NDSS = DS_PANEL.DSS.GetAmount();
	if (NDSS>0){
		while (pPANEL==NULL && NDSS--) {
			pPANEL = dynamic_cast<DialogsDesk*>(DS_PANEL.DSS[NDSS]);
			if (pPANEL!=NULL && strcmp(pPANEL->Name.str,"PANEL")!=0) pPANEL=NULL;
		};
	};
	return pPANEL;
};
TextButton*		BE_EDIT_PANEL::EP_TEXT(){
	if (Error)	return NULL;
	TextButton* pTEXT = NULL;
	DialogsDesk* pPANEL = EP_PANEL();
	if (pPANEL!=NULL) {
		pTEXT = dynamic_cast<TextButton*>(pPANEL->Find("TEXT"));
	};
	return pTEXT;
};
void			BE_EDIT_PANEL::EP_VISIBLE(bool state){
	Visible=state;
};
void			BE_EDIT_PANEL::EP_SET_TEXT(char* text){
	if (text==NULL) return;
	TextButton* pTB=EP_TEXT();
	if (pTB==NULL)	return;
	pTB->SetMessage(text);
};
void			BE_EDIT_PANEL::EP_PANEL_VISIBLE(bool state){
	if (Error)	return;
	EP_PANEL()->Visible=state;
};
void			BE_EDIT_PANEL::EP_TEXT_VISIBLE(bool state){
	if (Error)	return;
	EP_TEXT()->Visible=state;
};
void			BE_EDIT_PANEL::EP_PROPER_SIZE_POS(){
	if (Error)	return;

	int PW=RealLx-X0*2;
	int PH=RealLy/2-Y0-dY;

	EP_TEXT_POSITION(tX,tX);
	EP_TEXT_MAXWIDTH(PW-tX*2-mW);
	EP_PANEL_SIZE(PW,PH);
	EP_PANEL_POSITION(X0,Y0);
};
void			BE_EDIT_PANEL::EP_PANEL_POSITION(int x,int y){
	if (Error)	return;
	EP_PANEL()->Setx(x);
	EP_PANEL()->Sety(y);
};
void			BE_EDIT_PANEL::EP_TEXT_POSITION(int x,int y){
	if (Error)	return;
	EP_TEXT()->Setx(x);
	EP_TEXT()->Sety(y);
};
void			BE_EDIT_PANEL::EP_PANEL_SIZE(int w,int h){
	if (Error)	return;
	EP_PANEL()->SetWidth(w);
	EP_PANEL()->SetHeight(h);
};
void			BE_EDIT_PANEL::EP_TEXT_MAXWIDTH(int maxw){
	if (Error)	return;
	EP_TEXT()->MaxWidth=maxw;
};
// CBEDriveMode_ST ///////////////////////////////////////////////////////
CBEDriveMode_ST::CBEDriveMode_ST()	{ 
	visible=false; 
	OBJECT=2; 
	USE_vGRP=true;		
	NodeAct=3; 
	NodeView=1; 
	ND_RECT=true; ND_XY=true; ND_NAME=false; ND_DESCR=false;

	EdgeAct=2;
	EdgeView=1;
	EG_RECT=true; EG_DIRECT=true; EG_PROC=false; EG_START=false; EG_MODE=false;

	PR_CREATE=true;
	PR_MAIN=true;
	PR_SQUAD=true;
	PROCESS=false;

	BE_SAVE = false;

	BE_vGRP_VISIBLE=true;
	BE_vGRP_TEST=false;

	MainEditType=0;
	vGroupNameColor=0xFF000000;
	vGroupLineColor=0xFF0000FF;
	EdgeNameColor=0xFF00FF00;
	EdgeLineColor=0xFF00FF00;
	NodeNameColor=0xFF00FF00;
	NodeLineColor=0xFF00FF00;
	NodeIDColor=0xFFFF0000;
	NodeStyle=1; // "Rectangle"

	EditOperation=4;
	SelectedEdge=NULL;
	SelectedNodeBeg=NULL;
	SelectedNodeEnd=NULL;
	SelectedSquadShema=NULL;
	SelectedValue=NULL;			CorectionType=0;	
};

CBEDriveMode_ST::~CBEDriveMode_ST(){ 
	TimerData.Clear(); 
};

void CBEDriveMode_ST::Access(){
};

void	CBEDriveMode_ST::CBE_MainEdit::EvaluateFunction(){
	int lastItemChoose = ItemChoose;
	ItemChoose=-1;
//	vValuesMap()->SetViewType(0x00000002);
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM->MainEditType==0){
		ProcessEditor("MainBEScript");
	}else if (pDM->MainEditType==1) {
		MAIN_EDIT_PANEL.EP_VISIBLE(true);
		ProcessEditor("MainBEScriptEX");
	};				
	ItemChoose = lastItemChoose;
//	vValuesMap()->SetViewType(0x00000001);
	MAIN_EDIT_PANEL.EP_VISIBLE(false);
};

CBEDriveMode_ST::CBE_AddFilm::CBE_AddFilm()		{scrName="New Film"; scrDescr="New Film Descr";};
CBEDriveMode_ST::CBE_AddFilm::~CBE_AddFilm()	{scrName.Clear(); scrDescr.Clear();};
void	CBEDriveMode_ST::CBE_AddFilm::EvaluateFunction(){
	BattleHandler()->AddNewFilm(scrName.str,scrDescr.str);
};

void	CBEDriveMode_ST::CBE_EditFilm::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		BaseClass* pOBJ = dynamic_cast<BaseClass*>(pDM->scrFILM.Get());
		if (pOBJ!=NULL) {
			ReplaceEditor("ScriptFilm",pOBJ);
			int lastItemChoose = ItemChoose;
			ItemChoose=-1;
			ProcessEditor("ScriptFilm");
			ItemChoose = lastItemChoose;
		};
	};
};

void	CBEDriveMode_ST::CBE_EditGraph::EvaluateFunction(){
	BaseClass* pEditClass = &(BattleHandler()->SCRIPT_GRAPH);
	if (pEditClass!=NULL){
		ReplaceEditor("ScriptGraph",dynamic_cast<BaseClass*>(pEditClass));
		int lastItemChoose = ItemChoose;
		ItemChoose=-1;
		ProcessEditor("ScriptGraph");
		ItemChoose = lastItemChoose;
	};
};

CBEDriveMode_ST::CBE_Process::CBE_Process(){
	SQUARD_PR=MAIN_PR=true;
};

void	CBEDriveMode_ST::CBE_Process::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		pDM->PR_MAIN = MAIN_PR;
		pDM->PR_SQUAD= SQUARD_PR;
		pDM->PROCESS = !pDM->PROCESS;
	};
};

void	CBEDriveMode_ST::CBE_vGRP_VISIBLE_ALL::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		pDM->BE_vGRP_VISIBLE = !pDM->BE_vGRP_VISIBLE;
		GroupsMap()->SetVisible(pDM->BE_vGRP_VISIBLE);
	};
};

void	CBEDriveMode_ST::CBE_vGRP_VISIBLE::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->visible = !pvGRP->visible;
		};
	};
};

void	CBEDriveMode_ST::CBE_ADD_vGRP::EvaluateFunction(){
	GroupsMap()->AddGroup(Name.str);
};

void	CBEDriveMode_ST::CBE_ADD_vGRP_SMART::EvaluateFunction(){
	GroupsMap()->AddGroupSmart(Name.str);
};

void	CBEDriveMode_ST::CBE_EDIT_vGRP::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL){
		BaseClass* pOBJ = dynamic_cast<BaseClass*>(GroupsMap()->GetGroupID(pDM->BE_vGROUPS));
		if (pOBJ!=NULL) {
			ReplaceEditor("BEvGroupEDIT",pOBJ);
			int lastItemChoose = ItemChoose;
			ItemChoose=-1;
			ProcessEditor("BEvGroupEDIT");
			ItemChoose = lastItemChoose;
		};
	};
};

void	CBEDriveMode_ST::CBE_ADD_SelUnits::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->AddSelectedUnits();
		};
	};
};

void	CBEDriveMode_ST::CBE_ON_SCREEN::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->ScreenOnGroup();
		};
	};
};

CBEDriveMode_ST::CBE_REM_N_UNT::CBE_REM_N_UNT(){
	number=0xFFFF;
};
void	CBEDriveMode_ST::CBE_REM_N_UNT::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		lvCGroup* pDestGRP	=  GroupsMap()->GetGroupID(destGRP_ID);
		if (pvGRP!=NULL&&pDestGRP!=NULL) {
			pvGRP->RemoveNUnitsToCGroup(pDestGRP,number);
		};
	};
};

CBEDriveMode_ST::CBE_KILL_N_UNT::CBE_KILL_N_UNT(){ 
	number=0xFFFF; 
};

void CBEDriveMode_ST::CBE_KILL_N_UNT::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->KillUnits(number);
		};
	};
};

CBEDriveMode_ST::CBE_ERASE_N_UNT::CBE_ERASE_N_UNT(){ 
	number=0xFFFF; 
};

void CBEDriveMode_ST::CBE_ERASE_N_UNT::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->EraseUnits(number);
		};
	};
};

void CBEDriveMode_ST::CBE_UN_SELECT::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->UnSelect();
		};
	};
};

CBEDriveMode_ST::CBE_SELECT::CBE_SELECT(){ 
	add=false; 
};

void CBEDriveMode_ST::CBE_SELECT::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->SelectUnits(add);
		};
	};
};

void CBEDriveMode_ST::CBE_SELECT_IN_XYR::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->SelectUnitsInZone(x,y,r,add);
		};
	};
};

void CBEDriveMode_ST::CBE_SET_NATION::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->SetNation(NI);
		};
	};
};

void CBEDriveMode_ST::CBE_SET_AGRESIVITY::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->SetAgresiveST(state);
		};
	};
};

void CBEDriveMode_ST::CBE_SEND_TO::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->SendTo(x,y,dir,type);
		};
	};
};

void CBEDriveMode_ST::CBE_SET_DIR::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->ChangeDirection(dir,type);
		};
	};
};

void CBEDriveMode_ST::CBE_SET_FORM::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP		= GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->ChengeFormation(formType);
		};
	};
};

void CBEDriveMode_ST::CBE_SET_FLAGBR::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->SetBrFlagbearers();
		};
	};
};

void CBEDriveMode_ST::CBE_CLEAR_FLAGBR::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL&&pDM->BE_vGROUPS!=0xFFFF) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) {
			pvGRP->ClearBrFlagbearers();
		};
	};
};

void CBEDriveMode_ST::CBE_vValuesEdit::EvaluateFunction(){
	int lastItemChoose = ItemChoose;
	ItemChoose=-1;
	ProcessEditor("BEvValueEDIT");
	ItemChoose = lastItemChoose;
};

void CBEDriveMode_ST::CBE_CameraPossEdit::EvaluateFunction(){
	int lastItemChoose = ItemChoose;
	ItemChoose=-1;
	ReplaceEditor("CameraPoss",dynamic_cast<BaseClass*>(CameraPositons()));
	ProcessEditor("CameraPoss");
	ItemChoose = lastItemChoose;	
};

void CBEDriveMode_ST::CBE_BattleHandler::EvaluateFunction(){
	int lastItemChoose = ItemChoose;
	ItemChoose=-1;
	ReplaceEditor("BattleHendler",dynamic_cast<BaseClass*>(BattleHandler()));
	ProcessEditor("BattleHendler");
	ItemChoose = lastItemChoose;	
};

void CBEDriveMode_ST::CBE_MISS_SET::EvaluateFunction(){
	int lastItemChoose = ItemChoose;
	ItemChoose=-1;
	ReplaceEditor("MissionSettings",dynamic_cast<BaseClass*>(&gMISS_SET));
	ProcessEditor("MissionSettings");
	ItemChoose = lastItemChoose;
};
void CBEDriveMode_ST::CBE_WCP_EDIT::EvaluateFunction(){
	g_WCP_EDITOR.ShowEditor();
};
void CBEDriveMode_ST::CBE_MESSGES_MGR::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL){
		int lastItemChoose = ItemChoose;
		ItemChoose=-1;
		ProcessEditor("MessageMgr");
		ItemChoose = lastItemChoose;
	};
};

int		CBEDriveMode_ST::GetSquardID(){
	if (USE_vGRP) return BE_vGROUPS;
	return SQUARD;
};

DWORD	CBEDriveMode_ST::GetClassMask(){
	switch(OBJECT) {
		case 0:		// "NODE"
			return	0x00000001;
			break;
		case 1:		// "EDGE"
			return	0x00000002;
			break;
		case 2:		// "SCRIPT"
			return	0x00000004;
			break;
		case 3:		// "SAVE"
			return	0x00000008;
			break;
		case 4:		// "vGROUPS"
			if (BE_vGRP_TEST)	return 0x00000020;
			return	0x00000010;
			break;
		case 5:		// "vVALUES"
			return	0x00000040;
		case 6:		// "Settings"
			return	0x00000080;
			break;
		case 7:		// "EDIT"
			return	0x00000200;
			break;
		case 8:		// "TIMER"
			return	0x00000400;
			break;
		default:	// "NOTHING"
			return	0x80000000;
	};
};

DWORD	CBEDriveMode_ST::NodeMode(){
	DWORD	mode = 0;
	if (ND_RECT)	mode |= _RECT_;
	if (ND_XY)		mode |= _XY_;
	if (ND_NAME)	mode |= _NAME_;
	if (ND_DESCR)	mode |= _DESCR_;
	return	mode;
};

DWORD	CBEDriveMode_ST::EdgeMode(){
	DWORD	mode = 0;
	if (EG_RECT)	mode |= _RECT_;
	if (EG_DIRECT)	mode |= _DIRECTION_;
	if (EG_PROC)	mode |= _PROC_TIME_;
	if (EG_START)	mode |= _START_TIME_;
	if (EG_MODE)	mode |= _MODE_;
	return	mode;
};

void	CBEDriveMode_ST::StartMission(){
	NodeView=false;

	EdgeView=false;

	PR_CREATE=true;
	PR_MAIN=true;
	PR_SQUAD=true;

	BE_vGRP_VISIBLE = false;
	GroupsMap()->SetVisible(BE_vGRP_VISIBLE);	

	PROCESS=true;
};

void	CBEDriveMode_ST::StartMissionAfterLoad(){
	NodeView=false;

	EdgeView=false;

	PR_CREATE=false;
	PR_MAIN=true;
	PR_SQUAD=true;

	BE_vGRP_VISIBLE = false;
	GroupsMap()->SetVisible(BE_vGRP_VISIBLE);	

	PROCESS=true;
};



void	CBEDriveMode_ST::CBE_CREATE_DEFAULT_VG::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM==NULL)	return;
	
	LinearArray<int,_int>	natBrigList;
	LinearArray<int,_int>	vgBrigList;
    int curNAT=Nat;
	
	if (0<=curNAT&&curNAT<7){
		// Find all Brigads on map for this nation
		natBrigList.Clear();	
		for (int b=0; b<MaxBrig-11; b++){
			if ( CITY[curNAT].Brigs[b].Enabled==true ){
				natBrigList.Add(CITY[curNAT].Brigs[b].ID);
			};
		};
		// Find all Brigads on map in vGROUP for this nation 
		vgBrigList.Clear();
		GroupsMap()->GetBrigList(vgBrigList,curNAT);
		// Delete all meet elements from natBrigList
		int natN=natBrigList.GetAmount();
		bool	del = false;
		while (natN--) {
			int vgN=vgBrigList.GetAmount();
			while (!del&&vgN--) {
				if (natBrigList[natN]==vgBrigList[vgN])	del=true;
			};
			if (del) {
				natBrigList.Del(natN,1);
			};
		};
		// Create vGROUP and set name for them from natBrigList
        natN = natBrigList.GetAmount();
		if (natN!=0){
			_str	nameSQ;
			while (natN--) {
				nameSQ = "vgSQ_N";
				nameSQ += curNAT;
				nameSQ += "_";
				nameSQ += (int)(natBrigList[natN]);
				lvCGroup* pGrp = GroupsMap()->GetGroupNM(nameSQ.str);
				if (pGrp==NULL) {
					GroupsMap()->AddGroup(nameSQ.str);
					lvCGroup* pGrp = GroupsMap()->GetGroupNM(nameSQ.str);
					if (pGrp!=NULL) {
						pGrp->AddBrigad(natBrigList[natN],curNAT);
					};	
				};
			};
		};
	};
};

void	CBEDriveMode_ST::CBE_COPY_EDGE::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		lvCEdge* pNewEdge = NULL;
		pDM->SelectedEdge->GetCopy( (lvCStorage**)(&pNewEdge) );
		if (pNewEdge!=NULL) {
			pNewEdge->begID=pDM->SelectedNodeBeg->id;
			pNewEdge->endID=pDM->SelectedNodeEnd->id;
			BattleShema()->vAddEdge(pDM->BE_vGROUPS,pNewEdge);
		};
	};
};

void	CBEDriveMode_ST::CBE_SELECT_SQUAD::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		lvCSquardShema* pNewSS=BattleShema()->vGetSqShemaID(DriveMode()->GetSquardID());
		if (pNewSS!=NULL) {
			if (pDM->SelectedSquadShema!=NULL) {
				pDM->SelectedSquadShema->Select(false);
				if (pDM->SelectedSquadShema!=pNewSS) {
					pNewSS->Select(true);
					pDM->SelectedSquadShema=pNewSS;
				}else{
					pDM->SelectedSquadShema=NULL;
				};
			}else{
				pNewSS->Select(true);
				pDM->SelectedSquadShema=pNewSS;
			};
		};
	};
};

class lvCPara : public BaseClass{
public:
	int OldID;
	int NewID;
};
class lvCParaArr : public BaseClass
{
public:
	ClonesArray<lvCPara>	ParaArr;

	void	Clear()				{ ParaArr.Clear(); };
	void	Add(lvCPara* pPara)	{ ParaArr.Add(pPara); };
	int		GetNewID(int OldID)	{ 
		int N = ParaArr.GetAmount();
		int newID = 0;
		while (N--) {
			if (ParaArr[N]->OldID==OldID){
				N==0;
				newID=ParaArr[N]->NewID;
			};
		};
		return newID;
	};
};
void	CBEDriveMode_ST::CBE_COPY_SQUAD::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		lvCGroup* pOldGrp = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		lvCGroup* pNewGrp = GroupsMap()->GetGroupID(GrpID);
		if (pOldGrp!=NULL&&pNewGrp!=NULL) {
			int ox=0;
			int oy=0;
			pOldGrp->GetGroupCenter(ox,oy);
			int nx=0;
			int ny=0;
			pNewGrp->GetGroupCenter(nx,ny);
			int dx=nx-ox;
			int dy=ny-oy;

			// Create new Node for Squad Shema
			lvCParaArr	PARA;
			PARA.Clear();
			int N = NodesMap()->NodesArray.GetAmount();
			for (int i=0; i<N; i++){
				if (NodesMap()->NodesArray[i]->selected==true) {
					ox=NodesMap()->NodesArray[i]->x;
					oy=NodesMap()->NodesArray[i]->y;
					lvCNode* pNewNode =	NodesMap()->vAddNode(ox+dx,oy+dy);
					lvCPara* pNewPara = new lvCPara();
					pNewPara->OldID=NodesMap()->NodesArray[i]->id;
					pNewPara->NewID=pNewNode->id;
					PARA.Add(pNewPara);
					pNewPara=NULL;
				};
			};

			// Create new Edge for new Squad Shema
			lvCEdge* pEdge=NULL;
			lvCSquardShema* pOldShema = BattleShema()->vGetSqShemaID(pDM->BE_vGROUPS);
			lvCSquardShema* pNewShema = BattleShema()->vGetSqShemaID(GrpID);
			if (pOldShema!=NULL&&pNewShema!=NULL) {
				for (int i=0; i<pOldShema->SquardEdges.GetAmount(); i++){
					if (pOldShema->SquardEdges[i]->selected==true) {
						pOldShema->SquardEdges[i]->GetCopy( (lvCStorage**) (&pEdge) );
						if (pEdge!=NULL) {
							pEdge->id		= pNewShema->vGetFreeEdgeID();
							pEdge->begID	= PARA.GetNewID( pOldShema->SquardEdges[i]->begID );
							pEdge->endID	= PARA.GetNewID( pOldShema->SquardEdges[i]->endID );
							pNewShema->vAddEdge(pEdge);
						};
						pEdge=NULL;
					};
				};
			};
		};	
	};
};
void	CBEDriveMode_ST::CBE_ADD_MESSGES::EvaluateFunction(){
	if (Message.Get()!=NULL){
		AddMessageMM(Message.Get(),ParentName.str);
		Message.Set(NULL);
	};
};
void	CBEDriveMode_ST::CBE_FORM_SQUARD::EvaluateFunction(){
	lvCCreateBrigade* pCurForm = new lvCCreateBrigade();
	if (pCurForm==NULL)	return;
	int N = SquadForm.GetAmount();
	for (int i=0; i<N; i++){
		pCurForm->vGrp = SquadForm[i]->first_element;
		pCurForm->Use_VV = false;
		pCurForm->iSize = SquadForm[i]->second_element;
		pCurForm->Process(0);
	};
};
void	CBEDriveMode_ST::CBE_WRITE_LOG::EvaluateFunction(){
	if (vvLOG.Get()!=NULL)	vvLOG.Get()->WriteToLogClass();
};
void	CBEDriveMode_ST::UpdateTimerData(){
	TempTimerClass* pCurTimer = NULL;
	for (int i=0; i<TimerData.GetAmount(); i++){
		pCurTimer = TimerData[i];
		if (0<=pCurTimer->TimerID&&pCurTimer->TimerID<32){
			GTimer* TM=SCENINF.TIME+pCurTimer->TimerID;
			pCurTimer->Time					= TM->Time;
			pCurTimer->Used					= TM->Used;
			pCurTimer->First				= TM->First;
			pCurTimer->LastCheckAnimTime	= TM->LastCheckAnimTime;
			pCurTimer->trueTime				= TM->trueTime;
		};
	};
};
char*	CBEDriveMode_ST::GetSaveSFileName(){
	if (BE_SAVE&&FileForScript.str!=NULL) return FileForScript.str;
	return NULL;
};
void CBEDriveMode_ST::CBE_DelFilm::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL){
		if (pDM->scrFILM.Get()!=NULL){
			_str MessText;
			MessText = "Delete [";
			MessText += pDM->scrFILM.Get()->Name.str;
			MessText += "] Movie ???";
			if (MessageBox(hwnd,MessText.str,"Delete",MB_ICONWARNING|MB_OK|MB_OKCANCEL)==IDOK) 
				BattleHandler()->DeleteFilm(pDM->scrFILM.Get()->Name.str);
		};
	};
};
void CBEDriveMode_ST::CBE_DEL_vGRP::EvaluateFunction(){
	CBEDriveMode_ST* pDM = get_parent<CBEDriveMode_ST>();
	if (pDM!=NULL) {
		_str MessText;
		MessText = "Delete [";
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(pDM->BE_vGROUPS);
		if (pvGRP!=NULL) MessText += pvGRP->NAME.str;
		MessText += "] Group ???";
		if (MessageBox(hwnd,MessText.str,"Delete",MB_ICONWARNING|MB_OK|MB_OKCANCEL)==IDOK) 
			GroupsMap()->DelGroupID(pDM->BE_vGROUPS);
	};
};
void CBEDriveMode_ST::CBE_SAVE_GROUP_STRUCT::EvaluateFunction(){
	if (FileName.str==NULL)	return;
	LinearArray<int,_int> TYPES;
	GroupsMap()->GetStructNameList(TYPES);
	if (TYPES.GetAmount()>0) {
		FILE* pFile = fopen(FileName.str,"w");
		if (pFile!=NULL) {
			for (int i=0; i<TYPES.GetAmount(); i++){
				fprintf(pFile,"%s%s",NATIONS[0].Mon[TYPES[i]]->MonsterID,"\r\n");
			};
			fclose(pFile);
		};
	};
};
void CBEDriveMode_ST::CBE_SAVE_GROUP_LUA::EvaluateFunction(){
	SimpleEditClass("Group list", (BaseClass*)(GroupsMap()) );
};
void CBEDriveMode_ST::CBE_SAVE_NODE_LUA::EvaluateFunction(){
	SimpleEditClass("Node list", (BaseClass*)(NodesMap()) );
};
void CBEDriveMode_ST::CBE_SAVE_GRAPH_LUA::EvaluateFunction(){
	SimpleEditClass("Graph list", (BaseClass*)(BattleHandler()) );
};
void CBEDriveMode_ST::CSK_TASK_ED::EvaluateFunction(){
	SimpleEditClass("Skirmish task editor", (BaseClass*)(&g_SkTASK) );
};
void CBEDriveMode_ST::CSK_TASK_ADD::EvaluateFunction(){
	g_addTask(NI,x,y,name.str); // position on map in pixel
};
void CBEDriveMode_ST::CSK_TASK_DEL::EvaluateFunction(){
	g_delTask(name.str);
};
void CBEDriveMode_ST::CBE_SHOW_PROMF::EvaluateFunction(){	
	SimpleEditClass("MISSION PRO CALL", (BaseClass*)(&g_PMF) );
};
void CBEDriveMode_ST::CBE_MAS_EDITOR::EvaluateFunction(){
//	void	TEST_EDIT_ASS();
//	TEST_EDIT_ASS();
//	SimpleEditClass("Animete Picture store editor", (BaseClass*)(&g_TEST_ASS) );
};
// CBE_HANDLER ///////////////////////////////////////////////////////////
bool CBE_HANDLER::OnGameSaving(xmlQuote& xml){

	GroupsMap()->BeforeSave();
	vValuesMap()->DeleteAllSeparators();

	xmlQuote*	pxmlValueMap	= new xmlQuote( vValuesMap()->ClassName.str );
	xmlQuote*	pxmlNodeMap		= new xmlQuote( NodesMap()->ClassName.str	);	
	xmlQuote*	pxmlGroupsMap	= new xmlQuote( GroupsMap()->ClassName.str );
	xmlQuote*	pxmlBattleShema	= new xmlQuote( BattleShema()->ClassName.str );
	xmlQuote*	pxmlMainScript	= new xmlQuote( BattleHandler()->MAIN_SCRIPT.ClassName.str );
	xmlQuote*	pxmlScriptFilms	= new xmlQuote( "MissionScriptFilms" );
	xmlQuote*	pxmlScriptGraph	= new xmlQuote( "MissionScriptGraph" );
	xmlQuote*	pxmlCameraPoss  = new xmlQuote( "CameraPossitions"   );
	xmlQuote*	pxml_gMessagesMap = new xmlQuote( "NewMessagesMap" );
	xmlQuote*	pxml_MISS_SET	= new xmlQuote( "MissionSettings" );
	xmlQuote*	pxml_LUA		= new xmlQuote( "LUA" );
	// Save all additional information for mission.
	xmlQuote*	pxmlAGroupCurSCR	= new xmlQuote( "AGroupCurScreept" );
	xmlQuote*	pxmlVGroupCurSCR	= new xmlQuote( "VGroupCurScreept" );


	vValuesMap()->Save(*pxmlValueMap,vValuesMap());
	NodesMap()->Save(*pxmlNodeMap,NodesMap());
	GroupsMap()->Save(*pxmlGroupsMap,GroupsMap());
	BattleShema()->Save(*pxmlBattleShema,BattleShema());
	BattleHandler()->MAIN_SCRIPT.Save(*pxmlMainScript,&(BattleHandler()->MAIN_SCRIPT));
	BattleHandler()->SCRIPT_FILMS.Save(*pxmlScriptFilms,&(BattleHandler()->SCRIPT_FILMS));
	BattleHandler()->SCRIPT_GRAPH.Save(*pxmlScriptGraph,&(BattleHandler()->SCRIPT_GRAPH));
	CameraPositons()->Save(*pxmlCameraPoss,CameraPositons());
	gMessagesMap.Save(*pxml_gMessagesMap,&gMessagesMap);
	gMISS_SET.Save(*pxml_MISS_SET,&gMISS_SET);
	BattleHandler()->LUAC.Save(*pxml_LUA,&(BattleHandler()->LUAC));
	// Save all additional information for mission.
	BattleHandler()->SQUARDS_SCRIPTS.Save(*pxmlAGroupCurSCR,&(BattleHandler()->SQUARDS_SCRIPTS));
	BattleHandler()->vGROUPS_SCRIPTS.Save(*pxmlVGroupCurSCR,&(BattleHandler()->vGROUPS_SCRIPTS));


	xml.AddSubQuote(pxmlValueMap);
	xml.AddSubQuote(pxmlNodeMap);
	xml.AddSubQuote(pxmlGroupsMap);
	xml.AddSubQuote(pxmlBattleShema);
	xml.AddSubQuote(pxmlMainScript);
	xml.AddSubQuote(pxmlScriptFilms);
	xml.AddSubQuote(pxmlScriptGraph);
	xml.AddSubQuote(pxmlCameraPoss);
	xml.AddSubQuote(pxml_gMessagesMap);
	xml.AddSubQuote(pxml_MISS_SET);
	xml.AddSubQuote(pxml_LUA);
	// Save all additional information for mission.
	xml.AddSubQuote(pxmlAGroupCurSCR);
	xml.AddSubQuote(pxmlVGroupCurSCR);


	GroupsMap()->AfterSave();

	return true;
};
bool CBE_HANDLER::OnGameLoading(xmlQuote& xml){
	
	// Delete Alert after LOAD
	g_DeleteAllAlert();

	REG_BE_FUNCTIONS_class();
	REG_BE_CONDITION_class();
	REG_BE_DataStorageXML_class();

	vValuesMap()->reset_class(vValuesMap());
	NodesMap()->reset_class(NodesMap());
	GroupsMap()->reset_class(GroupsMap());
	BattleShema()->reset_class(BattleShema());
	BattleHandler()->MAIN_SCRIPT.reset_class(&(BattleHandler()->MAIN_SCRIPT));
	BattleHandler()->SCRIPT_FILMS.reset_class(&(BattleHandler()->SCRIPT_FILMS));
	BattleHandler()->SCRIPT_GRAPH.reset_class(&(BattleHandler()->SCRIPT_GRAPH));
	CameraPositons()->reset_class(CameraPositons());
	gMessagesMap.reset_class(&gMessagesMap);
	gMISS_SET.reset_class(&gMISS_SET);
	BattleHandler()->LUAC.reset_class(&(BattleHandler()->LUAC));
	// Load all additional information for mission.
	BattleHandler()->SQUARDS_SCRIPTS.reset_class(&(BattleHandler()->SQUARDS_SCRIPTS));
	BattleHandler()->vGROUPS_SCRIPTS.reset_class(&(BattleHandler()->vGROUPS_SCRIPTS));


	GroupsMap()->BeforeLoad();

	ErrorPager	Err(0);
	if (xml.GetSubQuote(0)!=NULL)	vValuesMap()->Load(*(xml.GetSubQuote(0)),vValuesMap(),&Err);
	if (xml.GetSubQuote(1)!=NULL)	NodesMap()->Load(*(xml.GetSubQuote(1)),NodesMap(),&Err);										
	if (xml.GetSubQuote(2)!=NULL)	GroupsMap()->Load(*(xml.GetSubQuote(2)),GroupsMap(),&Err);									
	if (xml.GetSubQuote(3)!=NULL)	BattleShema()->Load(*(xml.GetSubQuote(3)),BattleShema(),&Err);
	if (xml.GetSubQuote(4)!=NULL)	BattleHandler()->MAIN_SCRIPT.Load(*(xml.GetSubQuote(4)),&(BattleHandler()->MAIN_SCRIPT),&Err);
	if (xml.GetSubQuote(5)!=NULL)	BattleHandler()->SCRIPT_FILMS.Load(*(xml.GetSubQuote(5)),&(BattleHandler()->SCRIPT_FILMS),&Err);
	if (xml.GetSubQuote(6)!=NULL)	BattleHandler()->SCRIPT_GRAPH.Load(*(xml.GetSubQuote(6)),&(BattleHandler()->SCRIPT_GRAPH),&Err);
	if (xml.GetSubQuote(7)!=NULL)	CameraPositons()->Load(*(xml.GetSubQuote(7)),CameraPositons(),&Err);
	if (xml.GetSubQuote(8)!=NULL)	gMessagesMap.Load(*(xml.GetSubQuote(8)),&gMessagesMap,&Err);
	if (xml.GetSubQuote(9)!=NULL)	gMISS_SET.Load(*(xml.GetSubQuote(9)),&gMISS_SET,&Err);
	if (xml.GetSubQuote(10)!=NULL)	BattleHandler()->LUAC.Load(*(xml.GetSubQuote(10)),&(BattleHandler()->LUAC),&Err);
	// Load all additional information for mission.
	if (xml.GetSubQuote(11)!=NULL)	
		BattleHandler()->SQUARDS_SCRIPTS.Load(*(xml.GetSubQuote(11)),&(BattleHandler()->SQUARDS_SCRIPTS),&Err);
	if (xml.GetSubQuote(12)!=NULL)	
		BattleHandler()->vGROUPS_SCRIPTS.Load(*(xml.GetSubQuote(12)),&(BattleHandler()->vGROUPS_SCRIPTS),&Err);

	GroupsMap()->AfterLoad();

	BattleHandler()->LastTime=BattleHandler()->Time=0;
	DriveMode()->StartMissionAfterLoad();

	CSingleMessagereCreate=true;

	return true;
};
bool CBE_HANDLER::OnCheckingBuildPossibility(byte NI,int Type,int& x,int& y){
	int N = gMISS_SET.DZone.GetAmount();
	bool	ret = true;
	int xxx=-1,yyy=-1,RRR=-1;
	int Nat=0;
	while (ret&&N--) {
		Nat = gMISS_SET.DZone[N]->Nat;
			
		if (gMISS_SET.DZone[N]->UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(gMISS_SET.DZone[N]->Node);
			if (pNode!=NULL) {
				xxx=pNode->vGetX();
				yyy=pNode->vGetY();
				RRR=pNode->vGetR();
			};
		}else{
			int ZoneID = gMISS_SET.DZone[N]->Zone;
			if (0<AZones.GetAmount()&&ZoneID<AZones.GetAmount()&&AZones[ZoneID]!=NULL&&Nat==NI){
				ActiveZone* AZ=AZones[ZoneID];
				if (AZ!=NULL) {
					xxx=AZ->x;
					yyy=AZ->y;
					RRR=AZ->R;
				};
			};
		};
		
		if ( (xxx!=-1&&yyy!=-1&&RRR!=-1) && Norma((xxx)-(x>>4),(yyy)-(y>>4))<RRR )	ret=false;
		xxx=yyy=RRR=-1;
	};
	return ret;
};
void CBE_HANDLER::OnClassRegistration(){
	REG_CLASS(TempTimerClass);

	REG_BE_FUNCTIONS_class();
	REG_BE_CONDITION_class();
	REG_BE_DataStorageXML_class();
	
	pSquardsOnMap = new lvCSquardsOnMap;
	Enumerator* E=ENUM.Get("ALL_GROUPS_ON_MAP");
	E->SetProcEnum(pSquardsOnMap);

	pZonesOnMap = new lvCZonesOnMap;
	E=ENUM.Get("ALL_ZONES_ON_MAP");
	E->SetProcEnum(pZonesOnMap);

	pGroupsOnMap = new lvCGroupsOnMap;
	E=ENUM.Get("ALL_vGROUPS_ON_MAP");
	E->SetProcEnum(pGroupsOnMap);

	pNodesOnMap = new lvCNodesOnMap;
	E=ENUM.Get("ALL_vNODES_ON_MAP");
	E->SetProcEnum(pNodesOnMap);

	REG_CLASS(WCP_MASK_FILE);
	REG_CLASS(WCP_Editor);
	REG_CLASS(CPRESENTATION);

//	void	RegAnimateClassForBigMap();
//	RegAnimateClassForBigMap();
};

void CBE_HANDLER::OnInitAfterMapLoading(){

	DriveMode()->PR_MODE	= 0;
	DriveMode()->PR_CREATE	= true; 
	DriveMode()->PR_MAIN	= true;
	DriveMode()->PR_SQUAD	= true;

	//if (BattleHandler()->MAIN_SCRIPT.MAIN_INIT.GetAmount()>0	|| 
	//	BattleHandler()->MAIN_SCRIPT.MAIN_SCRIPTS.GetAmount()>0 ||
	//	BattleHandler()->SQUARDS_SCRIPTS.GetAmount()>0			||
	//	BattleHandler()->vGROUPS_SCRIPTS.GetAmount()>0				){
	//		DriveMode()->PR_MODE	= 0;
	//		DriveMode()->PR_CREATE	= true; 
	//		DriveMode()->PR_MAIN	= true;
	//		DriveMode()->PR_SQUAD	= true;
	//		DriveMode()->PROCESS	= true;
	//	};
};

void CBE_HANDLER::OnUnloading(){
	CBEDriveMode_ST::Delete();
	lvCNodesMAP_ST::Delete();
	lvCGroupsMAP_ST::Delete();
	vvMAP_ST::Delete();
	lvCBattleShema_ST::Delete();
	lvCScriptHandler_ST::Delete();
	lvCShapes::Delete();
	lvCRotateCamera::Delete();
	CameraPoss::Delete();

	ExperienceManager::Delete();

//	SetHelpMessagesArrays(SetCurrentHelp);
};

bool CBE_HANDLER::OnMapUnLoading(){
	
	g_DeleteAllAlert();
	BattleHandler()->LUA_SAFE_CLOSE_MISS();
	return true;
};
void CBE_HANDLER::OnEditorStart(){
	
	// TEST /////
//	AR_FL.INIT();
};	

void CBE_HANDLER::ProcessingGame(){
	BE_PROCESS_SCREPT();

	// TEST /////
//	AR_GR.PROCESS();	
};

bool CBE_HANDLER::OnAttemptToMove(OneObject* Unit,int x,int y){
	return GroupsMap()->__CheckMove(Unit);
};
void CBE_HANDLER::OnDrawOnMapAfterFogOfWar(){
	BattlePainter();

	GroupsMap()->DrawGroup();

	CameraPositons()->Draw();

	vValuesMap()->Draw();

	BattleHandler()->DRAW();

	gMessagesMap.DRAW();
	// TEST /////
//	AR_GR.ShowState();

};

void CBE_HANDLER::OnDrawOnMapOverAll(){
	if (DriveMode()->PROCESS){
		BattleHandler()->DRAW();
	};
	CameraDriver()->Draw();
};

void CBE_HANDLER::OnDrawOnMiniMap(int x,int y,int Lx,int Ly){
//	if (DriveMode()->PROCESS){
		vValuesMap()->DrawOnMiniMap(x,y,Lx,Ly);
//	};
	// for skirmish
	g_SkTASK.DrawOnMiniMap();
};
bool CBE_HANDLER::OnCheatEntering(const char* Cheat){
	return false;
};

bool CBE_HANDLER::OnMouseHandling(	int mx,int my,
									bool& LeftPressed,
									bool& RightPressed,
									int MapCoordX,int MapCoordY,
									bool OverMiniMap){
	if ( BE_HandlerMouse() || gMessagesMap.HANDLE(mx,my,LeftPressed,RightPressed,MapCoordX,MapCoordY,OverMiniMap) ){	
		UnPress();
		return true;
	};

	if (vValuesMap()->OnMouseHandling(mx,my,LeftPressed,RightPressed,MapCoordX,MapCoordY,OverMiniMap)) {
		UnPress();
		return true;
	};

	return	false;
};



void BE_InstallExtension(){
	InstallExtension(new CBE_HANDLER, "Start BE Editor Handler");
};
//////////////////////////////////////////////////////////////////////////

// ProcessBattleEditor ///////////////////////////////////////////////////
void	ProcessBattleEditor(){
	// FIRST CALL ////////////////////////////////////////////////////////////
	SET_BE_DrawOnMapCallback();

	ActivObject = NULL;

	static bool	first = true;
	if (first) {
		first=false;
		
		pSquardsOnMap = new lvCSquardsOnMap;
		Enumerator* E=ENUM.Get("ALL_GROUPS_ON_MAP");
		E->SetProcEnum(pSquardsOnMap);

		pZonesOnMap = new lvCZonesOnMap;
		E=ENUM.Get("ALL_ZONES_ON_MAP");
		E->SetProcEnum(pZonesOnMap);

		pGroupsOnMap = new lvCGroupsOnMap;
		E=ENUM.Get("ALL_vGROUPS_ON_MAP");
		E->SetProcEnum(pGroupsOnMap);

		pVValuesOnMap = new lvCVValuesOnMap;
		E=ENUM.Get("ALL_vVALUES_ON_MAP");
		E->SetProcEnum(pVValuesOnMap);

		pNodesOnMap = new lvCNodesOnMap;
		E=ENUM.Get("ALL_vNODES_ON_MAP");
		E->SetProcEnum(pNodesOnMap);
	};
	//////////////////////////////////////////////////////////////////////////
	
	DriveMode()->visible = !DriveMode()->visible;
	BattleShema()->vUpdateGroups();

	return;
};
//////////////////////////////////////////////////////////////////////////

// BE_PROCESS_SCREPT /////////////////////////////////////////////////////
void	BE_PROCESS_SCREPT(){
//	if (DriveMode()->OBJECT==2){	// "SCRIPT" selected
		if (DriveMode()->PROCESS==true) {
			if (DriveMode()->PR_CREATE==true) {
				BattleHandler()->Create();
				DriveMode()->PR_CREATE = false;
			};
			switch(DriveMode()->PR_MODE) {
			case 0:		// "All"
				if (DriveMode()->PR_SQUAD) BattleHandler()->PROCESS();
				BattleHandler()->PROCESS_MAIN(DriveMode()->PR_MAIN);
					
				BattleHandler()->LUA_SAFE_OPEN_MISS();
				BattleHandler()->LUA_CallFList();
                break;	
			case 1:		// "SQUAD"
				BattleHandler()->PROCESS(DriveMode()->GetSquardID());
				break;
			case 2:		// "SWITCHED"
				BattleHandler()->PROCESS(true);
				break;
			default:
				break;
			};
			BattleHandler()->TIMER(true);
		}else{
			BattleHandler()->TIMER(false);
			// Hide task list
			vvMESSGES* pMess = dynamic_cast<vvMESSGES*>(vValuesMap()->GetVValueTypeID(_vvMESSGES_));
			if (pMess!=NULL) {
				pMess->SetMessDS_Visbility(false);
			};
		};
		DriveMode()->PR_TIME = (int)(BattleHandler()->Time);
//	}else 
	
	if (DriveMode()->OBJECT==8) {
		DriveMode()->UpdateTimerData();
	};
	return;
}
//////////////////////////////////////////////////////////////////////////

// BE_HandlerMouse ///////////////////////////////////////////////////////
extern	int	DLG_X0;	extern	int DLG01_X0;		
extern	int	DLG_Y0;	extern	int DLG01_Y0;
extern	int	DLG_W;	extern	int DLG01_W;
extern	int	DLG_H;	extern	int DLG01_H;
extern	ClassArray<ActiveGroup> AGroups;
extern	int		mouseX;
extern	int		mouseY;
extern	int		LastMx;
extern	int		LastMy;
extern	bool	Lpressed;
extern	bool	Rpressed;
bool	BE_MouseInEditor(){
	bool	inEditor = (DLG_X0<=mouseX&&mouseX<=DLG_X0+DLG_W && DLG_Y0<=mouseY&&mouseY<=DLG_Y0+DLG_H);
	//if (DriveMode.NodeView==1/* VIEW ALL */ && DriveMode.NodeAct==3/* EDIT */ && ActivObject!=NULL) {
	//	inEditor = inEditor || (lvDLG01_X0<=mouseX&&mouseX<=lvDLG01_X0+lvDLG01_W && lvDLG01_Y0<=mouseY&&mouseY<=lvDLG01_Y0+lvDLG01_H);
	//};
	return inEditor;
};

lvCNodeActionsVI*	pObjectHendler = NULL;
bool	NODE_ACTIONS_HENDLER(lvCNodeActionsVI* pActInterf){
	if (pActInterf==NULL)	return false;

	switch(DriveMode()->NodeAct) {
			case 0:		// "ADD"
				if (Lpressed&&DriveMode()->NodeView==1) {
					pActInterf->vAddNode(LastMx,LastMy);
					Lpressed = false;
					return	true;
				};
				break;
			case 1:		// "DELETE"
				if (Lpressed) {
					if (DriveMode()->NodeView==1) {
						BattleShema()->vDelNode(LastMx,LastMy,0xFFFFFFFF);
						NodesMap()->vDelNode(LastMx,LastMy);
					};
					if (DriveMode()->NodeView==2) {
						if ( BattleShema()->vDelNode(LastMx,LastMy,DriveMode()->GetSquardID()) ){
							NodesMap()->vDelNode(LastMx,LastMy);
						};	
					};
					Lpressed = false;
					return true;
				};
				break;
			case 2:		// "MOVE"
				if (ActivObject==NULL && Lpressed) {
					ActivObject = pActInterf->vGetNode(LastMx,LastMy,DriveMode()->GetSquardID());
					if (ActivObject!=NULL) {
						Lpressed = false;
						return true;
					};
				};
				if (ActivObject!=NULL && Lpressed) {
					ActivObject = NULL;
					Lpressed = false;
					return true;
				};
				break;
			case 3:		// "EDIT"
				if (ActivObject==NULL && Lpressed) {
					ActivObject = pActInterf->vGetNode(LastMx,LastMy,DriveMode()->GetSquardID());
					if (ActivObject!=NULL) {
					//	SET_BE_NODE_CLASS_EDITOR(reinterpret_cast<lvCNode*>(ActivObject),true);
						ReplaceEditor("BENodeEDIT",dynamic_cast<BaseClass*>(ActivObject));
						int lastItemChoose = ItemChoose;
						ItemChoose=-1;
						ProcessEditor("BENodeEDIT");
						ItemChoose = lastItemChoose;
						pActivEdge=NULL;
						Lpressed = false;
//						ItemChoose=-1;
						return	true;
					};
				};
				if (ActivObject!=NULL && Lpressed) {
					ActivObject = NULL;
				//	SET_BE_NODE_CLASS_EDITOR(NULL,true);
					Lpressed = false;
					return	true;
				};
				break;
			default:	// "NOTHING"
				return false;
		};
		return	false;
};

bool	BE_HandlerMouse(){
	// NOT ACTIVE
//	if (DriveMode()->visible==false)	return false;

	static int NGrpOnMap = AGroups.GetAmount();
	if (NGrpOnMap!=AGroups.GetAmount()) {
		NGrpOnMap = AGroups.GetAmount();
		EDT00.ExpInfChanged=true;
		BattleShema()->vUpdateGroups();
	};
	
	// MOUSE IN DIALOG MENU
//	if (BE_MouseInEditor())			return false;

	switch(DriveMode()->OBJECT) {
	case 0:		// "NODE"
		if (DriveMode()->NodeView==1) {	// "VIEW ALL"
			pObjectHendler = dynamic_cast<lvCNodeActionsVI*>(NodesMap());
		};
		if (DriveMode()->NodeView==2) {	// "FOR GROUP"
			pObjectHendler = dynamic_cast<lvCNodeActionsVI*>(BattleShema());
		};
		return	NODE_ACTIONS_HENDLER(pObjectHendler);
		break;
	case 1:		// "EDGE"
		if (DriveMode()->EdgeView==1) {	// "VIEW"
			switch(DriveMode()->EdgeAct) {
			case 0:			// "ADD"
				if (GetKeyState(VK_ESCAPE)&0x8000){
					if (pActivEdge!=NULL){
						delete pActivEdge;
					};
					pActivEdge=NULL;
					return true;
				}
				if (addNewNodeInAct==false&&Lpressed) {
					if (pActivEdge!=NULL){
						delete pActivEdge;
					};
					pActivEdge=NULL;
                    lvCNode* pSelND = NodesMap()->vGetNode(LastMx,LastMy);
					if (pSelND!=NULL) {
						pActivEdge = new lvCEdge();
						pActivEdge->vINIT();
						pActivEdge->vSetBeg(pSelND->id);
						pActivEdge->endID = 0;
						addNewNodeInAct=true;
						Lpressed=false;
						return true;
					};
				};
				if (addNewNodeInAct==true&&Lpressed) {
					lvCNode* pSelND = NodesMap()->vGetNode(LastMx,LastMy);
					if (pSelND!=NULL&&pActivEdge!=NULL) {
						pActivEdge->vSetEnd(pSelND->id);
                        BattleShema()->vAddEdge(DriveMode()->GetSquardID(),pActivEdge);
						pActivEdge = NULL;
						addNewNodeInAct=false;
						Lpressed=false;
						return true;
					};
				};
				break;
			case 1:			// "DELETE"
				if (Lpressed) {
					if ( BattleShema()->vDelEdge(DriveMode()->GetSquardID(),LastMx,LastMy) ){
						Lpressed=false;
						return true;
					};
				};
				break;
			case 2:			// "EDIT"
				if (Lpressed) {
					if (pActivEdge!=NULL) {
						delete pActivEdge;
						pActivEdge=NULL;
					};
					pActivEdge=BattleShema()->vGetEdge(DriveMode()->GetSquardID(),LastMx,LastMy);
					if (pActivEdge!=NULL) {
					//	SET_BE_EDGE_CLASS_EDITOR(pActivEdge,true);
					//	vValuesMap()->SetViewType(0x00000002);
						ReplaceEditor("BEEdgeEDIT",dynamic_cast<BaseClass*>(pActivEdge));
						ReplaceEditor("BEEdgeEDITEX",dynamic_cast<BaseClass*>(pActivEdge));
						int lastItemChoose = ItemChoose;
						ItemChoose=-1;
						if (DriveMode()->MainEditType==0){
							ProcessEditor("BEEdgeEDIT");
						}else if (DriveMode()->MainEditType==1) {
							MAIN_EDIT_PANEL.EP_VISIBLE(true);
							ProcessEditor("BEEdgeEDITEX");
						};				
						ItemChoose = lastItemChoose;
					//	vValuesMap()->SetViewType(0x00000001);
						pActivEdge=NULL;
						Lpressed=false;
//						ItemChoose=-1;
						return true;
					};
				};
				break;
			default:		// "NOTHING"
				return false;
			};
		}
		break;
	case 7:					// "EDIT"
		switch(DriveMode()->EditOperation) {
		case 0:	// "Select Edge"
			if (Lpressed) {
				lvCEdge* pNewSelect = BattleShema()->vGetEdge(DriveMode()->GetSquardID(),LastMx,LastMy);
				if (pNewSelect!=NULL) {
					if (DriveMode()->SelectedEdge!=NULL) {
						DriveMode()->SelectedEdge->selected=false;
					};
					if (pNewSelect!=DriveMode()->SelectedEdge) {
						DriveMode()->SelectedEdge=pNewSelect;
						DriveMode()->SelectedEdge->selected=true;
					}else{
						DriveMode()->SelectedEdge=NULL;
					};
					
					Lpressed=false;
				};
			};
			break;
		case 1:	// "Select Node Beg"
			if (Lpressed) {
				lvCNode* pNewSelect = NodesMap()->vGetNode(LastMx,LastMy,DriveMode()->GetSquardID());
				if (pNewSelect!=NULL) {
					if (DriveMode()->SelectedNodeBeg!=NULL) {
						DriveMode()->SelectedNodeBeg->selected=false;
					};
					if (pNewSelect!=DriveMode()->SelectedNodeBeg) {
						DriveMode()->SelectedNodeBeg=pNewSelect;
						DriveMode()->SelectedNodeBeg->selected=true;
						DriveMode()->SelectedNodeBeg->type=0;
					}else{
						DriveMode()->SelectedNodeBeg=NULL;
					};
					
					Lpressed=false;
				};
			};
			break;
		case 2:	// "Select Node End"
			if (Lpressed) {
				lvCNode* pNewSelect = NodesMap()->vGetNode(LastMx,LastMy,DriveMode()->GetSquardID());
				if (pNewSelect!=NULL) {
					if (DriveMode()->SelectedNodeEnd!=NULL) {
						DriveMode()->SelectedNodeEnd->selected=false;
					};
					if (pNewSelect!=DriveMode()->SelectedNodeEnd) {
						DriveMode()->SelectedNodeEnd=pNewSelect;
						DriveMode()->SelectedNodeEnd->selected=true;
						DriveMode()->SelectedNodeEnd->type=1;
					}else{
						DriveMode()->SelectedNodeEnd=NULL;
					};
					
					Lpressed=false;
				};
			};
			break;
		case 3:	// "Select Squad"
			// Anable Select and UnSlect Edge, Node.
			if (Lpressed) {
				lvCNode* pNewSelectN = NodesMap()->vGetNode(LastMx,LastMy,DriveMode()->GetSquardID());
				lvCEdge* pNewSelectE = BattleShema()->vGetEdge(DriveMode()->GetSquardID(),LastMx,LastMy);
				if (pNewSelectN!=NULL){
					pNewSelectN->selected = !(pNewSelectN->selected);
					Lpressed=false;
				}else if (pNewSelectE!=NULL) {
					pNewSelectE->selected = !(pNewSelectE->selected);
					Lpressed=false;
				};
			};
			break;
		case 4:	// "Select Value"
			static int LastLx;
			static int LastLy;
			if (Lpressed) {
				if (DriveMode()->SelectedValue==NULL) {
					DriveMode()->SelectedValue = vValuesMap()->GetNearestValue(LastMx,LastMy);
					if (DriveMode()->SelectedValue!=NULL) {
						LastLx = LastMx;
						LastLy = LastMy;
						Lpressed=false;
					};
				}else{
					DriveMode()->SelectedValue=NULL;
					Lpressed=false;
				};
			};
			if (DriveMode()->SelectedValue!=NULL) {
				if (Rpressed) {
					DriveMode()->CorectionType = (DriveMode()->CorectionType+1)%5;
				};
				char s[5];
				int deltaX = LastMx-LastLx;
				int deltaY = LastMy-LastLy;
				LastLx=LastMx;LastLy=LastMy;
				switch(DriveMode()->CorectionType) {
				case 0:
					strcpy(s,"XY");
					DriveMode()->SelectedValue->SetPoss(LastMx,LastMy,0);
					break;
				case 1:
					if (DriveMode()->SelectedValue->InfID==_lvCTeraforming_) {
                        ((lvCTeraforming*)(DriveMode()->SelectedValue))->Set_r(deltaX);
					};
					strcpy(s,"r");
					break;
				case 2:
					if (DriveMode()->SelectedValue->InfID==_lvCTeraforming_) {
						((lvCTeraforming*)(DriveMode()->SelectedValue))->Set_R(deltaX);
					};
					strcpy(s,"R");
					break;
				case 3:
					if (DriveMode()->SelectedValue->InfID==_lvCTeraforming_) {
						((lvCTeraforming*)(DriveMode()->SelectedValue))->Set_h(-deltaY);
					};
					strcpy(s,"h");
					break;
				case 4:
					if (DriveMode()->SelectedValue->InfID==_lvCTeraforming_) {
						((lvCTeraforming*)(DriveMode()->SelectedValue))->Set_H(-deltaY);
					};
					strcpy(s,"H");
					break;
				};
				void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
				void WorldToScreenSpace ( Vector4D& vec );
				Vector4D	p((float)LastMx-10,(float)LastMy-10,(float)GetHeight(LastMx+15,LastMy+15),1);
				WorldToScreenSpace(p);
				ShowStringEx(p.x,p.y,s,&SmallWhiteFont);
			};
		default:	// "NOTHING"
			break;
		};
		break;
	default:	// "NOTHING"
		return false;
	};
	return	false;
};
//////////////////////////////////////////////////////////////////////////
bool	ggGetIndexedString(char* source,int pos,char* dest){
	int curPos=0;
	int curLen=-1;
	for (int i=0; i<strlen(source)+1; i++){
		if (source[i]==' '||source[i]=='\0') {
			curPos++;
		};
		if (curPos==pos){
			curLen++;
		};

		if (curPos==pos+1) {
			char sss[256];
			strcpy(sss,source+i-curLen-1);
			sss[curLen+1]='\0';
			strcpy(dest,sss);
			i=strlen(source);
		};
	};
	return (curLen!=0);
};
bool PrepareToShow_BE_MAIN(ClassEditor* CE,BaseClass* BC,int Options){
	//GlobalTextMouseOverCommand
	int		adress=0;
	char	s[256];
	bool	Error=false;
	Error = !(ggGetIndexedString(GlobalTextMouseOverCommand,1,s));
	if (!Error) adress = atoi(s);
	Error = Error || !(ggGetIndexedString(GlobalTextMouseOverCommand,0,s)); 

	if (Options==1) {
		strcpy(GlobalTextMouseOverCommand,"");
		MAIN_EDIT_PANEL.EP_SET_TEXT(" ");
		MAIN_EDIT_PANEL.EP_LOAD();
		return false;
	};
	if (Options==2) {
		if (!Error&&adress!=0) {
			if (strcmp(s,"BE_CFO")==0) {	// lvCCondForOper
				lvCCondForOper* pCFO = (lvCCondForOper*)(adress);
				MAIN_EDIT_PANEL.EP_SET_TEXT(pCFO->GetSourceCode());
			};
		};
		MAIN_EDIT_PANEL.EP_UPDATE();
		MAIN_EDIT_PANEL.EP_DRAW();
		return false;
	};
	if (Options==3) {
		return false;
	};
	if (Options==4) {
		return false;
	};
	return false;
};
// BE_EDIT_CLASS /////////////////////////////////////////////////////////
void	Add_Class_To_Main_Editor(DWORD _rce_, DWORD _DILOG_EDITOR_){
	AddStdEditor("BattleEditor",dynamic_cast<BaseClass*>(DriveMode()),"",_DILOG_EDITOR_,NULL,NULL,'B');

	AddStdEditor(	"MainBEScript",
					dynamic_cast<BaseClass*>(&(BattleHandler()->MAIN_SCRIPT)),
					"",
					_rce_|RCE_INVISIBLE														);

	AddStdEditor(	"MainBEScriptEX",
					dynamic_cast<BaseClass*>(&(BattleHandler()->MAIN_SCRIPT)),
					"",
					RCE_BOTTOM|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER,
					PrepareToShow_BE_MAIN										);

	AddStdEditor("ScriptFilm",NULL,"",_rce_|RCE_INVISIBLE);
	AddStdEditor("BEvGroupEDIT",dynamic_cast<BaseClass*>(GroupsMap()),"",_rce_|RCE_INVISIBLE);
	AddStdEditor("BEvValueEDIT",dynamic_cast<BaseClass*>(vValuesMap()),"",_rce_|RCE_INVISIBLE);

	AddStdEditor("ScriptGraph",NULL,"",_rce_|RCE_INVISIBLE);
	
	AddStdEditor("BENodeEDIT",dynamic_cast<BaseClass*>(NodesMap()),"",_rce_|RCE_INVISIBLE);

	AddStdEditor("BEEdgeEDIT",NULL,"",_rce_|RCE_INVISIBLE);
	AddStdEditor("BEEdgeEDITEX",NULL,"",RCE_BOTTOM|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER|RCE_INVISIBLE,PrepareToShow_BE_MAIN);

	AddStdEditor("CameraPoss",NULL,"",_rce_|RCE_INVISIBLE);

	// TEST //
	AddStdEditor("MessageMgr",dynamic_cast<BaseClass*>(&gMessagesMap),"",_rce_|RCE_INVISIBLE);
	AddStdEditor("MissionSettings",dynamic_cast<BaseClass*>(&gMISS_SET),"",_rce_|RCE_INVISIBLE);

	// SAVE // 
	AddStdEditor("BattleHendler",dynamic_cast<BaseClass*>(BattleHandler()),"",_rce_|RCE_INVISIBLE);
};
//////////////////////////////////////////////////////////////////////////


















