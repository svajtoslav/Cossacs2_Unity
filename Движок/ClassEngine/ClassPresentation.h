#pragma once

class WholeClassPresentation;

class OnePresentationCondition:public BaseClass{
public:
	virtual bool Check(BaseClass* Class,void* DataPtr,void* ExtraPtr){
		return true;
	}	
	SAVE(OnePresentationCondition);
	ENDSAVE;	
};
class pc_IfClassFieldIsEqual:public OnePresentationCondition{
public:
	_str FieldName;
	_str FieldValue;
	virtual bool Check(BaseClass* Class,void* DataPtr,void* ExtraPtr);
	const char* GetThisElementView(const char*){
		char* s=GetGlobalBuffer();
		sprintf(s,"%s==%s",FieldName.str,FieldValue.str);
		return s;
	}

	SAVE(pc_IfClassFieldIsEqual);
		REG_PARENT(OnePresentationCondition);
		REG_AUTO(FieldName);
		REG_AUTO(FieldValue);
	ENDSAVE;
};
class pc_OR:public OnePresentationCondition{
public:
	ClassArray<OnePresentationCondition> OR_List;

	virtual bool Check(BaseClass* Class,void* DataPtr,void* ExtraPtr);

	const char* GetThisElementView(const char*){
		static _str s;
		s="( ";
		for(int i=0;i<OR_List.GetAmount();i++){
			OnePresentationCondition* PC=OR_List[i];
			const char* ss=PC->GetThisElementView(NULL);
			if(ss){
				s+=ss;
				if(i<OR_List.GetAmount()-1)s+=" && ";
			}
		}
		s+=" )";
		return s.str;
	}

	SAVE(pc_OR);
		REG_PARENT(OnePresentationCondition);
		REG_AUTO(OR_List);
	ENDSAVE;
};
class ConditionsList:public ClassArray<OnePresentationCondition>{
public:
	const char* GetThisElementView(const char*){
		static _str s;
		s.Clear();
		for(int i=0;i<GetAmount();i++){
			OnePresentationCondition* PC=(*this)[i];
			s+=PC->GetThisElementView(NULL);
            if(i<GetAmount()-1)s+=" && ";
		}
		return s.str;
	}
};
class OnePresentationItem:public BaseClass{
public:
	ClassArray<OnePresentationCondition> Condition;
	_str Text;
	SAVE(OnePresentationItem);
		REG_AUTO(Condition);
		REG_AUTO(Text);
	ENDSAVE;
};
class ClassPresentation:public BaseClass{
public:
	virtual	bool		haveMask(const char* _ClassName){ 
		return ( _ClassName && GetPresClassName() && strcmp(GetPresClassName(),_ClassName)==0); 
	};
	virtual	const char* GetPresClassName(){ return NULL; };
	virtual	const char* GetThisElementView(const char*){
		if(GetPresClassName()){
			char* s=GetGlobalBuffer();
			sprintf(s,"class: %s",GetPresClassName());
			return s;
		};
		return NULL;
	}
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);

	SAVE(ClassPresentation);
	ENDSAVE;
};
class TextClassPresentation:public ClassPresentation{
public:	
	_str	ClassName;
	virtual	const char* GetPresClassName(){ return ClassName.str; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	
	ClonesArray<OnePresentationItem> Presentation;
	
	SAVE(TextClassPresentation);
		REG_PARENT(ClassPresentation);
		REG_AUTO(ClassName);
		REG_AUTO(Presentation);
	ENDSAVE;
};
class _int_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_int"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_int_Presentation(){};    
	SAVE(_int_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _WORD_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_WORD"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_WORD_Presentation(){};    
	SAVE(_WORD_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _DWORD_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_DWORD"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_DWORD_Presentation(){};    
	SAVE(_DWORD_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _UnitType_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_UnitType"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_UnitType_Presentation(){};    
	SAVE(_UnitType_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _float_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_float"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_float_Presentation(){};    
	SAVE(_float_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _str_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_str"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_str_Presentation(){};    
	SAVE(_str_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _index_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_index"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_index_Presentation(){};    
	SAVE(_index_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _bool_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_bool"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_bool_Presentation(){};    
	SAVE(_bool_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _gpfile_Presentation:public ClassPresentation{
public:
	virtual	const char* GetPresClassName(){ return "_gpfile"; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_gpfile_Presentation(){};    
	SAVE(_gpfile_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _ClassRef_Presentation:public ClassPresentation{
public:
	virtual	bool		haveMask(const char* _ClassName);
	virtual	const char* GetPresClassName(){ return "_ClassRef."; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_ClassRef_Presentation(){};    
	SAVE(_ClassRef_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _ClassPtr_Presentation:public ClassPresentation{
public:
	virtual	bool		haveMask(const char* _ClassName);
	virtual	const char* GetPresClassName(){ return "_ClassPtr."; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_ClassPtr_Presentation(){};    
	SAVE(_ClassPtr_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _ClassArray_Presentation:public ClassPresentation{
public:
	virtual	bool		haveMask(const char* _ClassName);
	virtual	const char* GetPresClassName(){ return "_ClassArray."; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
											char* Opt,WholeClassPresentation* pPresStore);
	_ClassArray_Presentation(){};    
	SAVE(_ClassArray_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class _ClonesArray_Presentation:public ClassPresentation{
public:
	virtual	bool		haveMask(const char* _ClassName);
	virtual	const char* GetPresClassName(){ return "_ClonesArray."; };
	virtual void		GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,
		char* Opt,WholeClassPresentation* pPresStore);
	_ClonesArray_Presentation(){};    
	SAVE(_ClonesArray_Presentation);
		REG_PARENT(ClassPresentation);
	ENDSAVE;
};
class ClassPresentationList:public BaseClass{
public:
	ClonesArray<TextClassPresentation> ClassList;
	SAVE(ClassPresentationList);
		REG_AUTO(ClassList);
		REG_LOADSAVE("ClassEngine\\","*.ClassList.xml");
	ENDSAVE;
};
class WholeClassPresentation:public BaseClass{
public:
	WholeClassPresentation();
	ClassArray<ClassPresentation> ClassList;
	void SetStdPresentation();
	void AddOnePresentation(ClassPresentation* P);
	void AddFromList(char* List);
	void AddFromList(ClassPresentationList* List);
	void GetPresentation(_str& Dest,BaseClass* Class);
	void GetPresentation(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt);
	ClassPresentation* GetPresentation(const char* ClassName);
	void FormatingString(_str& Dest);	// change "/"->'\n'
};
#ifdef IMPLEMENT_CLASS_FACTORY

void RegisterPresentations(){
    REG_CLASS(OnePresentationCondition);
	REG_CLASS(pc_IfClassFieldIsEqual);
	REG_CLASS(pc_OR);
	REG_CLASS(OnePresentationItem);
	REG_CLASS(ClassPresentation);
	REG_CLASS(TextClassPresentation);
	REG_CLASS(_int_Presentation);
	REG_CLASS(_WORD_Presentation);
	REG_CLASS(_DWORD_Presentation);
	REG_CLASS(_UnitType_Presentation);
	REG_CLASS(_float_Presentation);
	REG_CLASS(_str_Presentation);
	REG_CLASS(_index_Presentation);
	REG_CLASS(_bool_Presentation);
	REG_CLASS(_gpfile_Presentation);
	REG_CLASS(_ClassRef_Presentation);
	REG_CLASS(_ClassPtr_Presentation);
	REG_CLASS(_ClassArray_Presentation);
	REG_CLASS(_ClonesArray_Presentation);
	REG_CLASS(ClassPresentationList);
}
void WholeClassPresentation::AddOnePresentation(ClassPresentation* BC){
	const char* C=BC->GetPresClassName();
	if(!(C&&C[0]))return;
    ClassPresentation* CP=(ClassPresentation*)BC->new_element();
	BC->Copy(CP);
	for(int i=0;i<ClassList.GetAmount();i++){
		if(ClassList[i]->GetPresClassName()&&!strcmp(ClassList[i]->GetPresClassName(),C)){
			delete(ClassList[i]);
			ClassList[i]=CP;
			return;
		}
	}
	ClassList.Add(CP);
}
void WholeClassPresentation::AddFromList(char* List){
	if (List&&List[0]){
		ClassPresentationList TCP; 
		if (TCP.SafeReadFromFile(List))	AddFromList(&TCP);
	};
};
void WholeClassPresentation::AddFromList(ClassPresentationList* List){
	if (List!=NULL) {
		int clN=List->ClassList.GetAmount();
		ClassPresentation* pCP=NULL;
		for (int i=0; i<clN; i++){
			pCP = dynamic_cast<ClassPresentation*>(List->ClassList[i]);
			if (pCP!=NULL) AddOnePresentation( pCP );
		};
	};
};
void WholeClassPresentation::SetStdPresentation(){
	OneClassStorage* OCS=CGARB.GetClass("ClassPresentation");
	if(OCS){
		for(int i=0;i<OCS->Children.GetAmount();i++){
			char* C=OCS->Children[i];
			if(C){
				OneClassStorage* OCS2=CGARB.GetClass(C);
				if(OCS2){
					BaseClass* BC=OCS2->OneMemb->new_element();
					if( BC&&strcmp(BC->GetClassName(),"TextClassPresentation") ){
						AddOnePresentation( (ClassPresentation*)BC );
					}
				}
			}
		}
	}
}
WholeClassPresentation::WholeClassPresentation(){
	SetStdPresentation();
};
void WholeClassPresentation::GetPresentation(_str& Dest,BaseClass* Class){
	Dest="";
	if (Class&&Class->CheckDirectCasting()) {
		ClassPresentation* pCP=GetPresentation(Class->GetClassName());
		if (pCP!=NULL){
			pCP->GetPresentationMask(Dest,Class,Class,NULL,NULL,this);
			FormatingString(Dest);
			GetPresentation(Dest,Class,Class,NULL,NULL);
		};
		if (Dest.str[0]==0)	Dest=Class->GetThisElementView(NULL);
	};
};
void WholeClassPresentation::GetPresentation(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt){

	BaseClass* pBC=(BaseClass*)Data;
	BaseClass* pBC_TPR=NULL;
	BaseClass*	a_Class	= NULL;
	void*		a_Data	= NULL;
	void*		a_Extra	= NULL;

	int strN=strlen(Dest.str);
	char* str=(char*)malloc(strN+1);
	strcpy(str,Dest.str);
	str[strN]=0;

	char* aaa=NULL;
	char* bbb=NULL;
	char* ccc=NULL;
	char* sep=strstr(str,"%$");
	if (sep){
		do{
			// create aaa
			int aaaN = (sep-str);
			aaa=(char*)malloc(aaaN+1);
			if (aaaN) strncpy(aaa,str,aaaN);
			aaa[aaaN]=0;
			// create bbb

			int sepOPEN=0;
			int sepCLOS=0;
            
			char* sepEND=sep+2;
			bool stop=true;
			do{
				sepEND = strstr(sepEND,"$%");
				char* sepTTT=sep+2;
				sepOPEN=0;
				while ( (sepTTT=strstr(sepTTT,"%$")) && (sepEND-sepTTT>0) ) {
					sepTTT+=2;
					sepOPEN++;
				};
				stop=true;
				if (sepOPEN-sepCLOS>0){ sepEND+=2;sepCLOS++;stop=false;};
			}while(!stop);

			int bbbN = (sepEND-sep-2);
			bbb=(char*)malloc(bbbN+1);
			if (bbbN) strncpy(bbb,sep+2,bbbN);
			bbb[bbbN]=0;
			// create ccc
			int cccN = strlen(sepEND)-2;
			ccc=(char*)malloc(cccN+1);
			if (cccN) strcpy(ccc,sepEND+2);
			ccc[cccN]=0;

			// fill bbb using function GetPresentation(...);
			int nameN=strlen(bbb);
			char* opte=NULL;
			char* sepc=strstr(bbb,",");
			if (sepc) {
				nameN = (sepc-bbb);
				int opteN = strlen(sepc+1);
				opte=(char*)malloc(opteN+1);
				if (opteN) strcpy(opte,sepc+1);
				opte[opteN]=0;
			};
			char* name=(char*)malloc(nameN+1);
			if (nameN) strncpy(name,bbb,nameN);
			name[nameN]=0;
			if (name[0]>=0x30&&name[0]<=0x39) {
				int index = atoi(name);
				a_Class=pBC->GetElementClass(index);
				a_Data =pBC->GetElementPtr(index,pBC);
				a_Extra=pBC->GetElementExtraPtr(index);
			}else{
				char* pPAR=name;
				pBC_TPR=pBC;
                while ( pPAR=strstr(pPAR,"PAR.") ) {
					pPAR+=4;
					pBC_TPR=(BaseClass*)(pBC_TPR->GetParent());
					// reform name
					int tNN=strlen(pPAR);
					char* tName=(char*)malloc(tNN+1);
					if (tNN) strcpy(tName,pPAR);
					tName[tNN]=0;
					free(name);
					name=(char*)malloc(tNN+1);
					if (tNN) strcpy(name,tName);
					name[tNN]=0;
					pPAR=name;
					free(tName);
				};
				pBC_TPR->GetElementByName(name,&a_Class,&a_Data,&a_Extra,pBC_TPR);
			};
			if (a_Class==NULL){
				_str ErrorMessage;
				if		(pBC_TPR!=NULL) ErrorMessage  = pBC_TPR->GetClassName();
				else if (pBC!=NULL)		ErrorMessage  = pBC->GetClassName();
				else					ErrorMessage  = "class UN_DEFINE";
				ErrorMessage += "::";
				ErrorMessage += name;
			//	MessageBox(hwnd,ErrorMessage.str,"Presentation Error",MB_ICONERROR|MB_OK);
				Log.Error("Invalid class member %s",ErrorMessage.str);
			};
			free(name);
			if (a_Class!=NULL) {
				_str BBB;
				ClassPresentation* pCPA=GetPresentation(a_Class->GetClassName());
				if (pCPA){
					pCPA->GetPresentationMask(BBB,a_Class,a_Data,a_Extra,opte,this);
					FormatingString(BBB);
					// set N of space in front of presentation
					char* opt_spece = aaa;
					char* lsep=(char*)malloc(2);
					lsep[0]='\n'; lsep[1]=0;
					while (strstr(opt_spece,lsep)) {
						opt_spece=strstr(opt_spece,lsep)+1;
					};
					free(lsep);
					aaaN=strlen(opt_spece);
					opt_spece=(char*)malloc(aaaN+1);
					if (aaaN)	for (int i=0;i<aaaN;i++)	opt_spece[i]=' ';
					opt_spece[aaaN]=0;
					//////////////////////////////////////////
					GetPresentation(BBB,a_Class,a_Data,a_Extra,opt_spece);
					free(opt_spece);
				}else{
					GetPresentation(BBB,(BaseClass*)a_Data);
				};
				if (opte) free(opte);
				bbbN = strlen(BBB.str);
				free(bbb);
				bbb=(char*)malloc(bbbN+1);
				if (bbbN) strcpy(bbb,BBB.str);
				bbb[bbbN]=0;
			};
			
			// restore str
			free(str);
			str=(char*)malloc(strlen(aaa)+strlen(bbb)+strlen(ccc)+1);
			sprintf(str,"%s%s%s",aaa,bbb,ccc);
			// free memory
			free(aaa);
			free(bbb);
			free(ccc);
		}while( (sep=strstr(str,"%$")) );
	};
		
	// add spases after \n and \r
	if (Opt&&Opt[0]) {
		int OptN=strlen(Opt);
		sep=str;
		while (*sep) {
			if (*sep=='\n'||*sep=='\r') {
				int aaaN=(sep-str+1);
				aaa=(char*)malloc(aaaN+1);
				if (aaaN) strncpy(aaa,str,aaaN);
				aaa[aaaN]=0;
				int bbbN=strlen(sep+1);
				bbb=(char*)malloc(bbbN+1);
				if (bbbN) strncpy(bbb,sep+1,bbbN);
				bbb[bbbN]=0;
				free(str);
				str=(char*)malloc(strlen(aaa)+strlen(Opt)+strlen(bbb)+1);
				sprintf(str,"%s%s%s",aaa,Opt,bbb);
				sep=&(str[aaaN+OptN]);
			};
			sep++;
		};
	};

	Dest=str;
	free(str);
};
ClassPresentation* WholeClassPresentation::GetPresentation(const char* ClassName){
	if (ClassName==NULL||ClassName[0]==0)	return NULL;
	ClassPresentation* pCP=NULL;
	int N=ClassList.GetAmount();
	while (pCP==NULL&&N--) {
		pCP=ClassList[N];
		if (pCP&&pCP->haveMask(ClassName)==false)	pCP=NULL;
	};
	return pCP;
};
void WholeClassPresentation::FormatingString(_str& Dest){
	if (Dest.str==NULL)	return;
	int i=0;
	while (Dest.str[i]) {
		if ( Dest.str[i]== '/' ) Dest.str[i]='\n';
		i++;
	};
};
bool pc_IfClassFieldIsEqual::Check(BaseClass* Class,void* DataPtr,void* ExtraPtr){
	if(FieldName.str&&FieldValue.str){
		void* Data2;
		BaseClass* Class2;
		void* Extra2;
		if(Class->GetElementByName(FieldName.str,&Class2,&Data2,&Extra2,DataPtr)){
			bool empty=(strcmp(FieldValue.str,"EMPTY")==0);
			bool notempty=(strcmp(FieldValue.str,"NOTEMPTY")==0);
			if (empty||notempty){
				_str cName;
				BaseClass* pBC=(BaseClass*)Data2;
				cName=Class2->GetClassName();
				if       (strstr(cName.str,"ClassArray")||strstr(cName.str,"ClonesArray")) {
					return ( (empty) ? (pBC->GetAmountOfElements()==0) : (pBC->GetAmountOfElements()>0));
				}else if (strstr(cName.str,"ClassRef")||strstr(cName.str,"ClonessPtr")){
					return ( (empty) ? (pBC->GetElementPtr(0,pBC)==NULL) : (pBC->GetElementPtr(0,pBC)!=NULL) );
				};
			};
			xmlQuote xml;
			Class2->Save(xml,Data2,Extra2);
			const char* s=xml.Get_string();
			if(s&&!strcmp(s,FieldValue.str))return true;
		}
	}
	return false;
}
bool pc_OR::Check(BaseClass* Class,void* DataPtr,void* ExtraPtr){
	for(int i=0;i<OR_List.GetAmount();i++){
		bool r=OR_List[i]->Check(Class,DataPtr,ExtraPtr);
		if(r)return true;
	}
	return false;
}
void ClassPresentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){	
}
void TextClassPresentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){	
	for(int i=0;i<Presentation.GetAmount();i++){
		OnePresentationItem* OPI=Presentation[i];
		bool well=true;		
		for(int j=0;j<OPI->Condition.GetAmount();j++){
			bool r=OPI->Condition[j]->Check(Class,Data,Extra);
			if( !(well&=r) )break;
		}
		if(well){
			Dest+=OPI->Text;
		}
	}
}
void _int_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
    int v=*((int*)Data);
	Dest.print("%d",v);
}
void _WORD_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	WORD v=*((WORD*)Data);
	Dest.print("%u",v);
};
void _DWORD_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	DWORD v=*((DWORD*)Data);
	Dest.print("%u",v);
};
void _UnitType_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	int V=*((int*)Data);
	Dest.print("%d",V);	
};
void _float_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	int v=*((int*)Data);
	if(Opt&&Opt[0]){
		Dest.print(Opt,v);
	}else Dest.print("%.02f",v);
}
void _str_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	_str* s=(_str*)Data;	
	if (Opt&&Opt[0]) {
		if (strcmp(Opt,"value")==0) {
			Dest.print("%s",s->str);
		};
	}else{
		Dest.print("\"%s\"",s->str);
	};
}
void _bool_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	bool v=*((bool*)Data);
	Dest.print(v?"true":"false");
}
void _gpfile_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	WORD v=*((WORD*)Data);
	Dest.print("%u",v);
}
void _index_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	Enumerator* E=(Enumerator*)Extra;
	int v=*((int*)Data);
	Dest.print("%s",E->Get(v));
}
bool _ClassRef_Presentation::haveMask(const char* _ClassName){
	return	( _ClassName!=NULL && strstr(_ClassName,"ClassRef.")!=NULL );
};
void _ClassRef_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	Dest="%$";
	Dest+=0;
	Dest+="$%";
};
bool _ClassPtr_Presentation::haveMask(const char* _ClassName){
	return	( _ClassName!=NULL && strstr(_ClassName,"ClassPtr.")!=NULL );
};
void _ClassPtr_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	Dest="%$";
	Dest+=0;
	Dest+="$%";
};
bool _ClassArray_Presentation::haveMask(const char* _ClassName){
	return	( _ClassName!=NULL && strstr(_ClassName,"ClassArray.")!=NULL );
};
void _ClassArray_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	Dest="";
	BaseClass* pArray=(BaseClass*)Data;
	int N=pArray->GetAmountOfElements();
	for (int i=0;i<N;i++){
		Dest+="%$";
		Dest+=i;
		Dest+="$%";
		if (i+1<N&&Opt) Dest+=Opt;
	};
};
bool _ClonesArray_Presentation::haveMask(const char* _ClassName){
	return	( _ClassName!=NULL && strstr(_ClassName,"ClonesArray.")!=NULL );
};
void _ClonesArray_Presentation::GetPresentationMask(_str& Dest,BaseClass* Class,void* Data,void* Extra,char* Opt,WholeClassPresentation* pPresStore){
	Dest="";
	BaseClass* pArray=(BaseClass*)Data;
	int N=pArray->GetAmountOfElements();
	for (int i=0;i<N;i++){
		Dest+="%$";
		Dest+=i;
		Dest+="$%";
		if (i+1<N&&Opt) Dest+=Opt;
	};
};
#endif //IMPLEMENT_CLASS_FECTORY

























