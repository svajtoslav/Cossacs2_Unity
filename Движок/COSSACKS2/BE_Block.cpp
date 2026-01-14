
#include "BE_HEADERS.h"

//////////////////////////////////////////////////////////////////////////
// ssBaseFunction ////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
_str ssBaseFunctionViewCE;	// Info for ssBaseFunction in ClassEditor
ssBaseFunction::ssBaseFunction(){
	ClassTypeID=_ssBaseFunction_;
	RetValue.ClassName=_ssVoid_;
};
bool			ssBaseFunction::CheckIfObjectIsGlobal(){
	return true;
};
const	char*	ssBaseFunction::GetThisElementView(const char* LocalName){
	// Returned value
	ssBaseFunctionViewCE="{C 0xFF0000ff}";
	Enumerator* E=ENUM.Get("BE2_VALUE_TYPES");
	if (E->Get(RetValue.ClassName)!=NULL)	ssBaseFunctionViewCE+=E->Get(RetValue.ClassName);
	else									ssBaseFunctionViewCE+="NoType";
	ssBaseFunctionViewCE+="{C 0xFF000000}  ";
	// Function Name
	ssBaseFunctionViewCE+="{C 0xFF960000}";
	if (Name.str!=NULL)	ssBaseFunctionViewCE+=Name.str;
	else				ssBaseFunctionViewCE+="NoName";
	ssBaseFunctionViewCE+="{C 0xFF000000}";
	// Parametr list
	ssBaseFunctionViewCE+=Parm_List.GetThisElementView(NULL);

	return	ssBaseFunctionViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// csBaseModul ///////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
bool rce_BaseModulProcCallback(ClassEditor* CE,BaseClass* BC,int Options){
	static	DWORD oldFLAG_MODUL;
	if			(Options==1) {
		oldFLAG_MODUL=BE_GLOBAL_STORE.ggMODUL_TVM;
		BE_GLOBAL_STORE.ggMODUL_TVM=0x0000000F;
	}else if	(Options==2) {
	}else if	(Options==3) {
		BE_GLOBAL_STORE.ggMODUL_TVM=oldFLAG_MODUL;
	}else if	(Options==4) {
		BE_GLOBAL_STORE.ggMODUL_TVM=oldFLAG_MODUL;
	};
	return false;
};//Options=1-init, 2-process, 3-ok pressed, 4-cancel pressed
csBaseModul::csBaseModul(){
	ClassTypeID = _csBaseModul_;
};
void	csBaseModul::BM_EDIT::EvaluateFunction(){
	BaseClass* pBM = GetParent();
	if (pBM!=NULL) {
//		ItemChoose=-1;
		ReplaceEditor("PROCERURE",pBM);
		ProcessEditor("PROCERURE");
	};
};
DWORD	csBaseModul::GetClassMask(){
	DWORD	MASK=0xFFFFFFFF;
	MASK = MASK & BE_GLOBAL_STORE.ggMODUL_TVM;
	return MASK;
};

//////////////////////////////////////////////////////////////////////////
// ccFunction_List ///////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ccFunction_List::ccFunction_List(){
	ExpansionRules=1;
};
int		ccFunction_List::GetExpansionRules(){
	return ExpansionRules;
};
void	ccFunction_List::SetExpansionRules(int ExRul){
	ExpansionRules=ExRul;
};

//////////////////////////////////////////////////////////////////////////
// ssCALL ////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
int		ssCALL::CALL(){
	ssBaseFunction* pFUNC=FUNC.Get();
	if (pFUNC->ClassTypeID==_ssBaseFunction_) {
		BE2_CALL_ENGINE_FUNC(pFUNC,&VALUES);
	}else
	if (pFUNC->ClassTypeID==_csBaseModul_) {
		csBaseModul*	pMODL=( csBaseModul* )( pFUNC );
	};

	return 0;
};
void	ssCALL::CreateDefValueList(){
	ssBaseFunction* pFUNC=FUNC.Get();
	if (pFUNC!=NULL) {
		VALUES.Clear();
		scParam_List& PL= pFUNC->Parm_List;
		int NP=PL.GetAmount();
		ssValue* pVal=NULL;
		for (int i=0; i<NP; i++){
			pVal = CreateNewValue(PL[i]->ClassName);
			if (pVal!=NULL) VALUES.Add(pVal);
			pVal=NULL;
		};
	};
};
void	ssCALL::CALL_CREATE_PARAM::EvaluateFunction(){
	ssCALL* pCALL = get_parent<ssCALL>();
	if (pCALL==NULL)	return;
	pCALL->CreateDefValueList();
};

//////////////////////////////////////////////////////////////////////////
// scCALL_List ///////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
int scCALL_List::GetExpansionRules(){
	return 1;//0-no expansion 1-expand with base type only 2-expand with child classes
};

//////////////////////////////////////////////////////////////////////////

void	REG_CLASS_BLOCK_H(){
	REG_CLASS	(	ssBaseFunction	);
	REG_CLASS	(	csBaseModul		);
	REG_CLASS	(	ccFunction_List	);
	REG_CLASS	(	ssCALL			);
	REG_CLASS	(	scCALL_List		);
};

//////////////////////////////////////////////////////////////////////////









































