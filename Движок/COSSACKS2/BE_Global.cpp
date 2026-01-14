
#include "BE_HEADERS.h"

//////////////////////////////////////////////////////////////////////////
// ENGINE FUNCTIONS //////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
DLLEXPORT	void RunTimer(byte ID, int Long, bool trueTime);

//////////////////////////////////////////////////////////////////////////
// Global Enumerators ////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////
// Global calling ////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void ggHANDLER::OnClassRegistration(){
	BE_CREATE_VALUE_ENUM();
	REG_CLASS_BLOCK_H();		// Class from "Block.h"
	REG_CLASS_VALUE_H();		// Class from "Value.h"
	REG_CLASS_GLOBAL_H();		// Class from "Global.h"
};
bool ggHANDLER::OnCheatEntering(const char* Cheat){
	if (BE_GLOBAL_STORE.ggBE2_UseChits==false)	return false;

	return false;
};
void ggHANDLER::ProcessingGame(){
};
//////////////////////////////////////////////////////////////////////////
// Global Class //////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	BE2_InstallExtension(){
	InstallExtension(new ggHANDLER, "Start BE2 Editor Handler");
};
void	BE2_ReggClassEditors(){
	AddStdEditor(	"GLOBAL STORAGE",
					( BaseClass* )( &(BE_GLOBAL_STORE) ),
					"",
					RCE_SHOW_GAME_BACKGROUND|RCE_ALLOW_GAME_PROCESSING|RCE_RIGHT_POSITION|RCE_EXITONESCAPE
				);

	AddStdEditor(	"Engine Functions Header",
					( BaseClass* )( &(BE_GLOBAL_STORE.ggFUNC_LIST) ),
					"",
					RCE_CENTRAL_POSITION|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER|RCE_INVISIBLE
				);
	AddStdEditor(	"PROCERURE",
					NULL,
					"",
					RCE_SHOW_GAME_BACKGROUND|RCE_ALLOW_GAME_PROCESSING|RCE_RIGHT_POSITION|RCE_EXITONESCAPE|RCE_INVISIBLE,
					rce_BaseModulProcCallback
				);
};
void	BE2_CALL_ENGINE_FUNC(ssBaseFunction* pFUNC,scValue_List* pVAL_LIST){
	if (pFUNC==NULL||pFUNC->Name.str==NULL||pFUNC->Parm_List.IsMyValueList(pVAL_LIST)==false)	return;
	// Find function by Name
	_str FName;
	FName = pFUNC->Name.str;
	if			(!strcmp(FName.str,"RunTimer")) {
		int		ID			= ((ssInt* )((*pVAL_LIST)[0]))->DATA;
		int		Long		= ((ssInt* )((*pVAL_LIST)[1]))->DATA;
		bool	trueTime	= ((ssBool*)((*pVAL_LIST)[2]))->DATA;
		RunTimer(ID,Long,trueTime);
	}else if	(false) {
	}else if	(false) {
	}else if	(false) {
	}else if	(false) {
	};
};
//////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////
// Global Objects ////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ggGLOBAL_STORE::ggGLOBAL_STORE(){
	ggBE2_UseChits=false;
	ggVALUE_TVM=0xFFFFFFFF;
	ggFUNC_LIST.SetExpansionRules(1);
	ggMODUL_TVM=0x00000010;
	ggPROC_LIST.SetExpansionRules(2);
};
bool	ggGLOBAL_STORE::Load_FUNC_LIST(char* FileName){
	bool	Error = (FileName==NULL);
	if (!Error){
		xmlQuote	xml( "ENGINE FUNC" );
		xml.ReadFromFile( FileName );
		ErrorPager	Err;
		DWORD oldFLAG_VAL=ggVALUE_TVM;
		ggVALUE_TVM=0x00000002;
        ggFUNC_LIST.Load(xml,&(ggFUNC_LIST),&Err);
		ggVALUE_TVM=oldFLAG_VAL;
	};
	return false;
};
bool	ggGLOBAL_STORE::Save_FUNC_LIST(char* FileName){
	bool	Error = (FileName==NULL);
	if (!Error) {
		xmlQuote	xml( "ENGINE FUNC" );
		DWORD oldFLAG_VAL=ggVALUE_TVM;
		ggVALUE_TVM=0x00000002;
		ggFUNC_LIST.Save(xml,&(ggFUNC_LIST));
		ggVALUE_TVM=oldFLAG_VAL;
		xml.WriteToFile( FileName );
	}
	return !Error;
};
void	ggGLOBAL_STORE::GS_SHOW_FUNC_LIST::EvaluateFunction(){
	int lastItemChoose = ItemChoose;
	ItemChoose=-1;
	DWORD oldFLAG_VAL=BE_GLOBAL_STORE.ggVALUE_TVM;
	BE_GLOBAL_STORE.ggVALUE_TVM=0x00000002;
	ProcessEditor("Engine Functions Header");
	BE_GLOBAL_STORE.ggVALUE_TVM=oldFLAG_VAL;
	ItemChoose = lastItemChoose;
};
void	ggGLOBAL_STORE::GS_SAVE_FUNC_LIST::EvaluateFunction(){
	if (FileName.str==NULL)	return;
	_str Message;
	Message="Save data in << ";
	Message+=FileName.str;
	Message+=" >> file?";
	if (MessageBox(NULL,Message.str,"SAVE",MB_ICONWARNING|MB_OKCANCEL)==IDOK){
		BE_GLOBAL_STORE.Save_FUNC_LIST(FileName.str);
	};
};
void	ggGLOBAL_STORE::GS_LOAD_FUNC_LIST::EvaluateFunction(){
	if (FileName.str==NULL)	return;
	_str Message;
	Message="Load data from << ";
	Message+=FileName.str;
	Message+=" >> file?";
	if (MessageBox(NULL,Message.str,"LOAD",MB_ICONWARNING|MB_OKCANCEL)==IDOK){
		BE_GLOBAL_STORE.Load_FUNC_LIST(FileName.str);
	};
};
ggGLOBAL_STORE	BE_GLOBAL_STORE;

//////////////////////////////////////////////////////////////////////////
// Class Registration ////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	REG_CLASS_GLOBAL_H(){
	REG_CLASS(ggGLOBAL_STORE);
};

//////////////////////////////////////////////////////////////////////////







































