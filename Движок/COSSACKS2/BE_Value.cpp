
#include "BE_HEADERS.h"

//////////////////////////////////////////////////////////////////////////
// ssValue ///////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ssValue* CreateNewValue(DWORD type){
	ssValue* retVAl=NULL;
	switch(type) {
	case _ssVoid_:
		retVAl=new ssVoid;
		break;
	case _ssBool_:
		retVAl=new ssBool;
		break;
	case _ssInt_:
		retVAl=new ssInt;
		break;
	case _ssFloat_:
		retVAl=new ssFloat;
		break;
	case _ssStr_:
		retVAl=new ssStr;
		break;
	};
	return	retVAl;
};
_str ssValueViewCE;	// Info for ssValue in ClassEditor
ssValue::ssValue(){
	ClassTypeID = _ssValue_;
};
DWORD	ssValue::GetClassMask(){
	return	BE_GLOBAL_STORE.ggVALUE_TVM;
};
char*	ssValue::GetClassTypeSTR(){
	Enumerator* E=ENUM.Get("BE2_VALUE_TYPES");
	if (E->Get(ClassTypeID)!=NULL)	ssValueViewCE =E->Get(ClassTypeID);
	else							ssValueViewCE ="NoType";
	return ssValueViewCE.str;
};
DWORD	ssValue::GetClassTypeID(){
	return ClassTypeID;
};
char*	ssValue::GetValueAsSTR(){
	ssValueViewCE = "ssValue";
	return ssValueViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// ssVoid ////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ssVoid::ssVoid(){
	ClassTypeID = _ssVoid_;
};
char* ssVoid::GetValueAsSTR(){
	ssValueViewCE="void";
	return ssValueViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// ssBool ////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ssBool::ssBool(){
	ClassTypeID = _ssBool_;
};
char* ssBool::GetValueAsSTR(){
	if (DATA)	ssValueViewCE = "true";
	else		ssValueViewCE = "false";
	return ssValueViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// ssInt /////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ssInt::ssInt(){
	ClassTypeID = _ssInt_;
};
char* ssInt::GetValueAsSTR(){
	ssValueViewCE = DATA;
	return ssValueViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// ssFloat ///////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ssFloat::ssFloat(){
	ClassTypeID = _ssFloat_;
};
char* ssFloat::GetValueAsSTR(){
	ssValueViewCE = DATA;
	return ssValueViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// ssStr /////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
ssStr::ssStr(){
	ClassTypeID = _ssStr_;
};
char* ssStr::GetValueAsSTR(){
	ssValueViewCE = DATA.str;
	return ssValueViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// ssParametr ////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
_str ssParamViewCE;	// Info for ssParametr in ClassEditor
const	char*	ssParametr::GetThisElementView(const char* LocalName){
	// Param Type
	ssParamViewCE = "";
	Enumerator* E=ENUM.Get("BE2_VALUE_TYPES");
	ssParamViewCE = "{C 0xFF0000ff}";
	if (E->Get(ClassName)!=NULL)	ssParamViewCE+=E->Get(ClassName);
	else							ssParamViewCE+="NoType";
	ssParamViewCE += "{C 0xFF000000}";

	if (ClassName!=_ssVoid_) {
	
		// Param Name
		ssParamViewCE += "{C 0xFF960000}";
		if (ssParamViewCE.str!=NULL)	ssParamViewCE+="  ";
		if (ParamName.str!=NULL)		ssParamViewCE+=ParamName.str;
		else							ssParamViewCE+="NoName";
		ssParamViewCE += "{C 0xFF000000}";

		// Param defoult value
		if (DeffValue.Get()!=NULL)
		{	
										ssParamViewCE+=" {C 0xFF0000ff}={C 0xFF000000} ";
			if (DeffValue.Get()->GetClassTypeID()==ClassName){
										ssParamViewCE+=DeffValue.Get()->GetValueAsSTR();
			}else{
										ssParamViewCE+="{C 0xFF0000ff}BadValue{C 0xFF000000}";
			};
		};

	};
	return ssParamViewCE.str;
};
DWORD	ssParametr::GetClassMask(){
	DWORD	MASK=0x00000000;
	if (ClassName==_ssVoid_) {
		MASK=0x00000001;
	}else{
		MASK=0xFFFFFFFF;
	}
	return MASK;
};
//////////////////////////////////////////////////////////////////////////
// scParam_List //////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
_str ssParamListViewCE;	// Info for scParam_List in ClassEditor
int scParam_List::GetExpansionRules(){
	return 1;//0-no expansion 1-expand with base type only 2-expand with child classes
};
bool scParam_List::IsMyValueList(scValue_List* pVL){
	if (pVL==NULL)	return false;
	// NP=NV
	int NP = GetAmount();
	int NV = pVL->GetAmount();
	if (NP!=NV)	return false;
	bool Error=false;
	for (int i=0; i<NP; i++){
		if ( (*pVL)[i]->GetClassTypeID()!=(*this)[i]->ClassName ){
			Error=true;
		};
	};
	return !Error;
};// Check values according to param list
const	char*	scParam_List::GetThisElementView(const char* LocalName){
	int N=GetAmount();
	ssParamListViewCE = "{C 0xFF0000ff}( ";
	Enumerator* E=ENUM.Get("BE2_VALUE_TYPES");
	for (int i=0; i<N; i++){
		if (E->Get((*this)[i]->ClassName)!=NULL)	ssParamListViewCE+=E->Get((*this)[i]->ClassName);
		else										ssParamListViewCE+="NoType";
		if (i==N-1) ssParamListViewCE += " ";
		else		ssParamListViewCE += ", ";
	};
	ssParamListViewCE += "){C 0xFF000000}";
	return ssParamListViewCE.str;
};

//////////////////////////////////////////////////////////////////////////
// Class Registration ////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	REG_CLASS_VALUE_H(){
	REG_CLASS	(	ssValue			);
	REG_CLASS	(	scValue_List	);
	REG_CLASS	(	ssBool			);
	REG_CLASS	(	ssInt			);
	REG_CLASS	(	ssFloat			);
	REG_CLASS	(	ssStr			);
	REG_CLASS	(	ssParametr		);
	REG_CLASS	(	scParam_List	);
};

//////////////////////////////////////////////////////////////////////////
// Value Type Enumerator /////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	BE_CREATE_VALUE_ENUM(){
	Enumerator* E=ENUM.Get("BE2_VALUE_TYPES");
	E->Clear();
	E->Add(	"void"  ,	_ssVoid_	);
	E->Add(	"bool"	,	_ssBool_	);
	E->Add(	"int"	,	_ssInt_		);
	E->Add(	"float"	,	_ssFloat_	);
	E->Add(	"_str"	,	_ssStr_		);
};


//////////////////////////////////////////////////////////////////////////
































