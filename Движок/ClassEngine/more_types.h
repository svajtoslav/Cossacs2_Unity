#ifndef __MORE_TYPES_H__
#define __MORE_TYPES_H__
#pragma pack(push)
#pragma pack(1)
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
//some useful macro for properties definition//
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
//this macro can be used to access private members and bit fields
//DEF_VARPROP must be defined in public section
//classname - owner class name
//type      - type of variable
//x         - name of variable
#define DEF_VPROP(classname,type,x)\
	type _Get##x(){return x;}\
	void _Set##x(type v){x=v;}\
	static void* __Get##x(void* C){static type V=((classname*)C)->_Get##x();return &V;}\
	static void  __Set##x(void* C,void* V){((classname*)C)->_Set##x(*((type*)V))
//if you have defined DEF_VPROP int public section, you must
//define REG_VPROP in SAVE..ENDSAVE section
//type      - type of variable
//x         - name of variable
#define REG_VPROP(type,x) REG_PROP(type,x,Get##x,Set##x)
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
//        additional types definition        //
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
class DIALOGS_API _color:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();  
	virtual void reset_class(void* ptr){
		*((DWORD*)ptr)=0;
	}
	AUTONEW(_color);
};

//_textid
class DIALOGS_API _variant:public _str{	
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return !strcmp(TypeName,"class _str");
	}
	virtual void GetVariants(ClonesArray<_str> &List){};
	virtual const char* GetClassName(){
		return "_variant";
	}
	AUTONEW(_variant);
};
class DIALOGS_API _ClassIndex:public _str{
public:
	_ClassIndex() : _str() { INDEX=-1; NUMBER=-1; };
	int INDEX;	// Index
	int NUMBER;	// Last element number in list

	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL){
		bool res = _str::Load(xml,ClassPtr,Error,Extra);
		INDEX=-1; 
		NUMBER=-1;
		return res;
	};
	virtual void reset_class(void* ptr){ 
		_str::reset_class(ptr); 
		INDEX=-1; 
		NUMBER=-1; 
	};

	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	};
	virtual const char* GetClassName(){
		return "_ClassIndex";
	};
	virtual bool CheckDirectCasting(){
		return true;
	}
	BaseClass*	getARRAY(char* _ExtraPtr);
	BaseClass*	getELEMENT(char* _ExtraPtr);
	AUTONEW(_ClassIndex);
};
//
class DIALOGS_API _strindex:public _str{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return !strcmp(TypeName,"class _str");
	}
	virtual const char* GetClassName(){
		return "_strindex";
	}
	AUTONEW(_strindex);
};
class DIALOGS_API _textid:public BaseClass,public DString{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return !strcmp(TypeName,"char *");
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();   
	virtual void reset_class(void* ptr){
		char** pc=(char**)ptr;
		if(*pc){
			free(*pc);
			*pc=NULL;
		}		
	}
	AUTONEW(_textid);    
};//_textid
class DIALOGS_API _WORD:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==2;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName(); 
	virtual void reset_class(void* ptr){
		*((WORD*)ptr)=0;
	}
	AUTONEW(_WORD);
};//_WORD
class DIALOGS_API _short:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==2;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();  
	virtual void reset_class(void* ptr){
		*((short*)ptr)=0;
	}
	AUTONEW(_short);
};//_short
class DIALOGS_API _char:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==1;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName(); 
	virtual void reset_class(void* ptr){
		*((char*)ptr)=0;
	}
	AUTONEW(_char);
};//_char
class DIALOGS_API _BYTE:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==1;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName(); 
	virtual void reset_class(void* ptr){
		*((BYTE*)ptr)=0;
	}
	AUTONEW(_BYTE);
};//_byte
//_font
class DIALOGS_API _font:public BaseClass,public DString{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return !strcmp(TypeName,"class RLCFont *");
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName(); 
	virtual void reset_class(void* ptr){
		*((DWORD*)ptr)=NULL;
	}
	AUTONEW(_font);    
};//_font
class DIALOGS_API _gframe:public BaseClass{
public:
	WORD FileID;
	WORD SpriteID;
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	
	AUTONEW(_gframe);    
};//_gframe
class DIALOGS_API _gpfile:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==2;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	
	AUTONEW(_gpfile);
};//_gpfile
//
class DIALOGS_API _picfile:public _str{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return !strcmp(TypeName,"class _str");
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	
	AUTONEW(_picfile);
};
class DIALOGS_API _ModelID:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	
	AUTONEW(_ModelID);
};//_ModelID
class DIALOGS_API _TextureID:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	
	AUTONEW(_TextureID);
};//_TextureID
class _UnitType:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();
	virtual void reset_class(void* ptr);
	AUTONEW(_UnitType);
};
#ifdef IMPLEMENT_CLASS_FACTORY
//_color
void _color::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	DWORD* V=(DWORD*)ClassPtr;
	char c[16];
	sprintf(c,"%08X",*V);
	xml.Assign_string(c);
}
bool _color::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	DWORD* V=(DWORD*)ClassPtr;
	const char* c=xml.Get_string();
	*V=0;
	int z=0;
	if(c)z=sscanf(c,"%X",V);
	if(z!=1){
		Error->xml_print(xml,"unable to read DWORD from XML: %s\n",xml.Get_string());
		return false;
	}
	return true;
};
const char* _color::GetClassName(){
	return "_color";
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
//~~~~~~~~~ClassPointersGarbage~~~~~~//
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
void TestCGARB();
DWORD ClassPointersGarbage::AddClass(BaseClass* Ptr){
	if(Ptr->GetObjectNamePointer()){
		if(FreePtrs.GetAmount()){
			OneClassPointer* CP=FreePtrs[FreePtrs.GetAmount()-1];
			FreePtrs.Del(FreePtrs.GetAmount()-1,1);
			CP->NamePtr=Ptr->GetObjectNamePointer();	
			CP->ClassPtr=Ptr;			
			CP->ROOT=Ptr->GetRoot();
			CP->RefCount=0;
			Ptr->SetObjectGlobalID(CP->Index);
			return CP->Index;
		}
		OneClassPointer* CP;
		SAFENEW;
			CP=new OneClassPointer;
		ENDNEW;
		CP->NamePtr=Ptr->GetObjectNamePointer();	
		CP->ClassPtr=Ptr;
		CP->Index=CPointer.GetAmount();
		CP->ROOT=Ptr->GetRoot();
		CP->RefCount=0;
		SAFENEW;
			CPointer.Add(CP);
		ENDNEW;
		Ptr->SetObjectGlobalID(CP->Index);
		return CP->Index;
	}
	Ptr->SetObjectGlobalID(0xFFFFFFFF);
	return (DWORD)-1;
}
void  ClassPointersGarbage::DeleteClass(BaseClass* Ptr){
	if(Ptr->GetObjectNamePointer()){
		DWORD ID=Ptr->GetObjectGlobalID();
		if(ID!=0xFFFFFFFF){
			OneClassPointer* CP=CPointer[ID];
			if(CP->ClassPtr){
				CP->ClassPtr=NULL;
				CP->NamePtr=NULL;
				if(CP->RefCount==0){
					CP->ROOT=NULL;
					CP->RefName.Free();
					SAFENEW;
						//for(int i=0;i<FreePtrs.GetAmount();i++)assert(FreePtrs[i]->Index!=CP->Index);
						FreePtrs.Add(CP);
					ENDNEW;
				}
			}
		}
	}
}
int testv2=0;
DWORD ClassPointersGarbage::AddRef(char* ClassID,char* MemberName,BaseClass* Root){
	BaseClass* ROOT=Root->GetRoot();
	if(!strcmp(MemberName,"GivePess")){
		testv2=65;	
	}
	for(int i=0;i<CPointer.GetAmount();i++){
		try{
			OneClassPointer* CP=CPointer[i];
			if(CP->ClassPtr&&!strcmp(CP->ClassPtr->GetClassName(),ClassID)
				&&CP->NamePtr&&CP->NamePtr->str&&!strcmp(CP->NamePtr->str,MemberName)){
					if(CP->ClassPtr->CheckIfObjectIsGlobal()||ROOT==CP->ClassPtr->GetRoot()){
						CPointer[i]->RefCount++;
						return i;
					}
				}
		}catch(...){};
	}
	for(int i=0;i<CPointer.GetAmount();i++){
		try{
			OneClassPointer* CP=CPointer[i];
			if(CP->ClassPtr==NULL&&CP->NamePtr==NULL
				&&CP->RefName.str&&!strcmp(CP->RefName.str,MemberName)
				&&CP->ClassName.str&&!strcmp(CP->ClassName.str,ClassID)){
					CP->RefCount++;
					return CP->Index; 
				}  
		}catch(...){};
	}
	OneClassPointer* CP;
	if(FreePtrs.GetAmount()){
		CP=FreePtrs[FreePtrs.GetAmount()-1];
		FreePtrs.Del(FreePtrs.GetAmount()-1,1);
	}else{
		SAFENEW;
			CP=new OneClassPointer;		
			CP->Index=CPointer.GetAmount();
			CPointer.Add(CP);
		ENDNEW;
	}
	CP->FirstRef=Root;
	CP->NamePtr=NULL;
	CP->ClassPtr=NULL;
	CP->RefName=MemberName;
	CP->ClassName=ClassID;	
	CP->ROOT=ROOT;
	CP->RefCount=1;	
	return CP->Index;
}
void ClassPointersGarbage::DelRef(DWORD ID){  
	if(ID!=0xFFFFFFFF){
		int N=(--CPointer[ID]->RefCount);
		assert(N>=0);
		OneClassPointer* CP=CPointer[ID];
		if(N==0&&CP->ClassPtr==NULL){
			CP->ClassPtr=NULL;
			CP->NamePtr=NULL;			
			CP->ROOT=NULL;
			CP->RefName="";
			SAFENEW;
				//for(int i=0;i<FreePtrs.GetAmount();i++)assert(FreePtrs[i]->Index!=CP->Index);
				FreePtrs.Add(CP);			
			ENDNEW;
		}
	}
}
void ClassPointersGarbage::AddRef(DWORD ID){    
	if(ID!=0xFFFFFFFF){
		CPointer[ID]->RefCount++;
	}
}
int testv=0;
DWORD ClassPointersGarbage::TryToLinkClass(BaseClass* ClassPtr){
	if(ClassPtr->GetObjectNamePointer()){
		BaseClass* ROOT=ClassPtr->GetRoot();
		char* s=ClassPtr->GetObjectNamePointer()->str;
		if(s&&s[0]){
			DWORD FRef=0xFFFFFFFF;
			for(int i=0;i<CPointer.GetAmount();i++){
				try{
					OneClassPointer* CP=CPointer[i];
					if(!CP->ClassPtr){
						if(CP->ClassName.str&&CP->RefName.str
							&&(!strcmp(CP->ClassName.str,ClassPtr->GetClassName()))
							&&(!strcmp(CP->RefName.str,s))&&(ClassPtr->CheckIfObjectIsGlobal()||CP->ROOT==ROOT)){
								if(FRef!=0xFFFFFFFF){
									ClassRef<BaseClass>* CR=(ClassRef<BaseClass>*)CP->FirstRef;
									if(CR){
										DelRef(CR->CPG_Index);
										CR->CPG_Index=FRef;
										CP->FirstRef=NULL;
										AddRef(FRef);
									}
								}else{
									CP->ClassPtr=ClassPtr;
									CP->NamePtr=ClassPtr->GetObjectNamePointer();							
									CPGARB.DeleteClass(ClassPtr);
									ClassPtr->SetObjectGlobalID(i);
									CP->FirstRef=NULL;
									FRef=i;
								}
							}
					}
				}catch(...){};
			}
		}		
	}
	return 0xFFFFFFFF;
}
ClassPointersGarbage::~ClassPointersGarbage(){
	for(int i=0;i<CPointer.GetAmount();i++){
		try{
			delete(CPointer[i]);
		}catch(...){};
	}
	CPointer.Clear();
}
DWORD ClassPointersGarbage::FindClass(BaseClass* ClassPtr){
	for(int i=0;i<CPointer.GetAmount();i++)if(CPointer[i]->ClassPtr==ClassPtr)return i;
	return (DWORD)-1;
}
#define IMPLEMENT_SIMPLETYPE(x)\
void _##x::Save(xmlQuote& xml,void* ClassPtr,void* Extra){\
	x* w=(x*)ClassPtr;\
	xml.Assign_int(*w);\
}\
bool _##x::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){\
	x* w=(x*)ClassPtr;\
	int i;\
	int z=sscanf(xml.Get_string(),"%d",&i);\
	if(z==1){\
		*w=(x)i;\
		return true;\
	}else{\
		Error->xml_print(xml,"unable to read "#x);\
		return false;\
	}\
}\
const char* _##x::GetClassName(){\
	return "_"#x;\
}
IMPLEMENT_SIMPLETYPE(WORD);
IMPLEMENT_SIMPLETYPE(short);
IMPLEMENT_SIMPLETYPE(char);
IMPLEMENT_SIMPLETYPE(BYTE);
//end simple types
#ifdef _USE3D
#ifndef NO_COSS_EXT
extern GP_System GPS;
void _gframe::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	char s[256];
	char s1[128];
	strcpy(s1,GPS.GetGPPath(FileID));
	char* c=strchr(s1,'.');
	if(c)*c=0;
	c=strstr(s1,"Cash\\");
    if(c)strcpy(s1,s1+5);	
	sprintf(s,"%s %d",s1,SpriteID);
	xml.Assign_string(s);
}
bool _gframe::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	char fname[128];
	int frame;
	int z=sscanf(xml.Get_string(),"%s%d",fname,&frame);
	if(z==2){
		FileID=(WORD)GPS.PreLoadGPImage(fname);
		SpriteID=(WORD)frame;
        return true;
	}else{
		Error->xml_print(xml,"unable to read frame");
		return false;
	}
}
const char* _gframe::GetClassName(){
	return "_gframe";
}
//
void _gpfile::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	char s[256];
	char s1[128];
	short v=*((short*)ClassPtr);
	if(v!=-1){
		strcpy(s1,GPS.GetGPName(v));
		/*
		char* c=strchr(s1,'.');
		if(c)*c=0;
		c=strstr(s1,"Cash\\");
		if(c)strcpy(s1,s1+5);
		*/
		xml.Assign_string(s1);
	}
}
bool _gpfile::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	char* s=(char*)xml.Get_string();
	*((WORD*)ClassPtr)=s?(WORD)GPS.PreLoadGPImage(s):0;
	return true;
}
const char* _gpfile::GetClassName(){
	return "_gpfile";
}
//
void _picfile::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	//char s[256];
	//char s1[128];
	char* v=((_str*)ClassPtr)->str;
	if(v&&v[0]){
		//strcpy(s1,GPS.GetGPName(v));
		/*
		char* c=strchr(s1,'.');
		if(c)*c=0;
		c=strstr(s1,"Cash\\");
		if(c)strcpy(s1,s1+5);
		*/
		xml.Assign_string(v);
	}
}
bool _picfile::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	*((_str*)ClassPtr)=xml.Get_string(); //(WORD)GPS.PreLoadGPImage((char*)xml.Get_string());
	return true;
}
const char* _picfile::GetClassName(){
	return "_picfile";
}
//
void _ModelID::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	char s[256];
	char s1[128];
	DWORD v=*((DWORD*)ClassPtr);
	if(v!=-1){
		strcpy(s1,IMM->GetModelFileName(v));
		xml.Assign_string(s1);
	}
}
bool _ModelID::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	char* ID=(char*)xml.Get_string();
	*((DWORD*)ClassPtr)=(DWORD)IMM->GetModelID(ID?ID:"");
	return true;
}
const char* _ModelID::GetClassName(){
	return "_ModelID";
}

void _TextureID::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	char s[256];
	char s1[128];
	DWORD v=*((DWORD*)ClassPtr);
	if(v!=-1){
		xml.Assign_string((char*)IRS->GetTextureName(v));
	}
}
bool _TextureID::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	extern IRenderSystem* IRS;
	*((DWORD*)ClassPtr)=(DWORD)IRS->GetTextureID((char*)xml.Get_string());
	return true;
}
const char* _TextureID::GetClassName(){
	return "_TextureID";
}

void _textid::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	char* s=*((char**)ClassPtr);
	if(s){
		char* GetIDByText(char*);
		char* ID=GetIDByText(s);
        xml.Assign_string(ID);
	}
}
bool _textid::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	char** lpS=(char**)ClassPtr;
	char* s;
	char* GetTextByID(char*);
	//
	extern bool v_DialogsMissed;
	v_DialogsMissed=true;
	if(xml.Get_string()) s=GetTextByID((char*)xml.Get_string());
		else s="";
	v_DialogsMissed=false;
	//	
	if(*lpS)free(*lpS);
	*lpS=(char*)malloc(strlen(s)+1);
	strcpy(*lpS,s);
	return true;
}
const char* _textid::GetClassName(){
	return "_textid";
}
void _font::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	DWORD* fp=(DWORD*)ClassPtr;
	Enumerator* EF=ENUM.Get("FONTS");
	char* s=EF->Get(*fp);
	if(!s)s=EF->GetStr(0);
	xml.Assign_string(s);
}
bool _font::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	Enumerator* EF=ENUM.Get("FONTS");
	if(xml.Get_string()){
		DWORD V=EF->Get((char*)xml.Get_string());
		if(V==0xFFFFFFFF)V=EF->GetVal(0);
		*((DWORD*)ClassPtr)=V;
	}else{
		*((DWORD*)ClassPtr)=EF->GetVal(0);
	}
	return true;
}
const char* _font::GetClassName(){
	return "_font";
}

// _ClassIndex ///////////////////////////////////////////////////////////
bool	g_SeparateStr(char* a_str,char* a_frs,char* a_sep,bool revers=false){
	a_frs[0]=0;
	if (a_str==NULL||strlen(a_str)==0||a_frs==NULL)	return false;
	if (a_sep==NULL||strlen(a_sep)==0)	{ sprintf(a_frs,"%s",a_str); a_str[0]=0;	return true; };
	char* ppp=strstr(a_str,a_sep);
	if (ppp==NULL)						{ sprintf(a_frs,"%s",a_str); a_str[0]=0;	return true; };
	int nc=(int)(ppp-a_str+1);
	strncpy(a_frs,a_str,nc);
	a_frs[nc-1]=0;
	if (a_str[nc]==0)	{	a_str[0]=0;				return true; };
	char ttt[512];
	sprintf(ttt,"%s",&(a_str[nc]));
	sprintf(a_str,"%s",ttt);
	if (revers) {
		sprintf(ttt,"%s",a_str);
		sprintf(a_str,"%s",a_frs);
		sprintf(a_frs,"%s",ttt);
	};
	return true;
};
BaseClass*	_ClassIndex::getARRAY(char* _ExtraPtr){
	if (_ExtraPtr==NULL) return NULL;
	BaseClass* OWNER = (BaseClass*)this;
	if (OWNER==NULL) return NULL;
	OWNER = (BaseClass*)(OWNER->GetParent());
	if (OWNER==NULL) return NULL;
	char	FILD_NAME[512];	// Path to fild that contain data list object
	char	ELEM_NAME[128];	// fild than show in list in editor 
	sscanf(_ExtraPtr,"%s%s",FILD_NAME,ELEM_NAME);
	if (strlen(FILD_NAME)==0||strlen(ELEM_NAME)==0)	return NULL;
	char	FRST_NAME[512];
	char	MODE_NAME[64];
	BaseClass*	ClassArrayPtr	= NULL;
	void*		ClassArrayData	= NULL;
	void*		ClassArrayExtra	= NULL;
	while (FILD_NAME[0]!=0&&OWNER!=NULL) {
		FRST_NAME[0]=0;
		MODE_NAME[0]=0;
		if (strstr(FILD_NAME,"/")!=NULL){
			g_SeparateStr(FILD_NAME,FRST_NAME,"/");	
		}else{
			sprintf(FRST_NAME,"%s",FILD_NAME);
			FILD_NAME[0]=0;
		};

		if (strstr(FRST_NAME,"^")!=NULL) {
			g_SeparateStr(FRST_NAME,MODE_NAME,"^",true);	
		};
		
		if (FRST_NAME[0]!=0) {	
			if (strcmp(FRST_NAME,"..")==0) {	// go to parent
				if (OWNER->CheckDirectCasting()){
					OWNER=(BaseClass*)OWNER->GetParent();
				};
			}else{								// have directly fild
				OWNER->GetElementByName(FRST_NAME,&ClassArrayPtr,&ClassArrayData,&ClassArrayExtra,OWNER);
				if(ClassArrayPtr&&ClassArrayPtr->CheckDirectCasting()) ClassArrayPtr=(BaseClass*)ClassArrayData;
				else ClassArrayPtr=NULL;
				OWNER=ClassArrayPtr;
			};
		};
		if (MODE_NAME[0]!=0){	// Have some modificator for OWNER
			if (strcmp(MODE_NAME,"REF")==0) {
				ClassRef<ReferableBaseClass>* pRefClass = (ClassRef<ReferableBaseClass>*)OWNER;
				if (OWNER!=NULL) {
					OWNER = (BaseClass*)(pRefClass->Get());
				};
			};
		};
	};
	return OWNER;
};
BaseClass*	_ClassIndex::getELEMENT(char* _ExtraPtr){
    BaseClass* pElement = NULL;
	BaseClass* pArray = getARRAY(_ExtraPtr);
	char	FILD_NAME[512];	// Path to fild that contain data list object
	char	ELEM_NAME[128];	// fild than show in list in editor 
	sscanf(_ExtraPtr,"%s%s",FILD_NAME,ELEM_NAME);
	int N=pArray->GetAmountOfElements();
	if (pArray!=NULL) {
		if (INDEX!=-1&&NUMBER==N) {
			BaseClass* ClassType=pArray->GetElementClass(INDEX);
			void* ElmData=pArray->GetElementPtr(INDEX,pArray);
			void* Extra=pArray->GetElementExtraPtr(INDEX);

			BaseClass* E_Class=NULL;
			void* E_Data;
			void* E_Extra;
			if(ClassType->GetElementByName(ELEM_NAME,&E_Class,&E_Data,&E_Extra,ClassType)){
				xmlQuote xml;
				E_Class->Save(xml,E_Data,E_Extra);
				const char* s=xml.Get_string();

				if (s!=NULL && !strcmp( s, this->str )){
					pElement=(BaseClass*)(ElmData);
				};
			};
		};
		if (pElement==NULL) {
			for(int i=0;(i<N)&&(pElement==NULL);i++){
				BaseClass* ClassType=pArray->GetElementClass(i);
				void* ElmData=pArray->GetElementPtr(i,pArray);
				void* Extra=pArray->GetElementExtraPtr(i);

				BaseClass* E_Class=NULL;
				void* E_Data;
				void* E_Extra;
				if(ClassType->GetElementByName(ELEM_NAME,&E_Class,&E_Data,&E_Extra,ClassType)){
					xmlQuote xml;
					E_Class->Save(xml,E_Data,E_Extra);
					const char* s=xml.Get_string();

					if (s!=NULL && !strcmp( s, this->str )){
						pElement=(BaseClass*)(ElmData);
						NUMBER=N;
						INDEX=i;
					};
				};
			};
		};
	};
	if (pElement==NULL) {
		NUMBER=-1;
		INDEX=-1;
	}
	return pElement;
};
//////////////////////////////////////////////////////////////////////////
#endif //NO_COSS_EXT
#endif //_USE3D
void BaseClass::LoadPostProcess(void* DataPtr,void* ExtraData){
	BaseClass* SF_ClassPtr=NULL;;
	void* SF_ElmPtr;
	void* SF_ExtraPtr;
	if(GetElementByName("SourceFile",&SF_ClassPtr,&SF_ElmPtr,&SF_ExtraPtr,DataPtr)){
		if(!strcmp(SF_ClassPtr->GetClassName(),"_str")){
			BaseClass* LR_ClassPtr=NULL;;
			void* LR_ElmPtr;
			void* LR_ExtraPtr;
			if(GetElementByName("LoadingRules",&LR_ClassPtr,&LR_ElmPtr,&LR_ExtraPtr,DataPtr)){
				if(*((byte*)LR_ElmPtr)){
					xmlQuote xml;
					if(xml.ReadFromFile(((_str*)SF_ElmPtr)->str)){
						reset_class(DataPtr);
						ErrorPager Error;
						Load(xml,DataPtr,&Error,ExtraData);
					}
				}
			}
		}
	}
	int N=GetAmountOfElements();
	for(int i=0;i<N;i++){
		LoadPostProcess(GetElementPtr(i,DataPtr),GetElementExtraPtr(i));
	}
}
#endif //IMPLEMENT_CLASS_FACTORY
#pragma pack(pop)
#endif //__MORE_TYPES_H__