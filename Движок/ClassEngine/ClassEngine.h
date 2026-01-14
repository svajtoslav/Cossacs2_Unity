#ifndef __CLASSENGINE_H__
#define __CLASSENGINE_H__
#pragma pack(push)
#pragma pack(1)
#include <DString.h>
#include <xmlQuote.h>
#include <StrHash.h>
#include <DynArray.h>
#include <typeinfo.h>
#define IMMEDIATE_ERROR
DIALOGS_API void PushSmartLeak(bool& v);
DIALOGS_API void PopSmartLeak(bool& v);
#define SAFENEW {bool sl;PushSmartLeak(sl);
#define ENDNEW PopSmartLeak(sl);}
char* GetGlobalBuffer();
class DIALOGS_API ErrorPager{
	DString Msg;
public:	
	bool BreakExecution;
	ErrorPager(){
		BreakExecution=0;
	}
	ErrorPager(int bx){
		BreakExecution=bx!=0;
	}
	_inline void print(char* mask,...){
		va_list args;
		va_start(args,mask);
		char temp[512];
		vsprintf(temp,mask,args);
		va_end(args);
		Msg.Add(temp);
#ifdef IMMEDIATE_ERROR
		if(BreakExecution){
			if(MessageBox(NULL,temp,"Class engine error",MB_ICONERROR|MB_OKCANCEL)==IDCANCEL){
				exit(1);
			}
		}		
#endif //IMMEDIATE_ERROR
	}
	_inline void xml_print(xmlQuote& xml,char* mask,...){
		if(BreakExecution){
			va_list args;
			va_start(args,mask);
			char temp[1024];
			vsprintf(temp,mask,args);
			va_end(args);
			DString D;
			xml.GetXMLSource(&D);
			if(strlen(D.str)>500){
				D.str[500]=0;
				D.Add("...");
			}
			sprintf(temp+strlen(temp),"XML source:\n\n%s\n",D.str);
			Msg.Add(temp);
#ifdef IMMEDIATE_ERROR
			if(MessageBox(NULL,temp,"Class engine error",MB_ICONERROR|MB_OKCANCEL)==IDCANCEL){
				exit(1);			
			}
#endif //IMMEDIATE_ERROR
		}
	}
	void Clear();
};
_inline void PrintError(char* mask,...){
	va_list args;
	va_start(args,mask);
	char temp[512];
	vsprintf(temp,mask,args);
	va_end(args);
#ifdef IMMEDIATE_ERROR
	if(MessageBox(NULL,temp,"Class engine error",MB_ICONERROR|MB_OKCANCEL)==IDCANCEL){
		assert(1);
	}
#endif //IMMEDIATE_ERROR
}


class BaseClass;
class Enumerators;
class Enumerator;

typedef int   tpIntPropertyReader(void*);
typedef void* tpPropertyReader(void*);
typedef void  tpIntPropertyWriter(void*,int data);
typedef void  tpPropertyWriter(void*,void*);

class DIALOGS_API OneClassMemberStorage{
public:
	OneClassMemberStorage();
	~OneClassMemberStorage();
	BaseClass* Member;
	char* xmlID;
	int OffsetFromClassRoot;	
	bool UseReference;
	bool ReadOnly;
	bool Invisible;
	bool StaticMode;
	bool NoSaveMode;
	void* ExtraData;
	tpIntPropertyReader* IntPropertyR;
	tpPropertyReader*    GeneralPropertyR;
	tpIntPropertyWriter* IntPropertyW;
	tpPropertyWriter*    GeneralPropertyW;
	const char* HostClass;
	bool DirectCasting;
    DWORD Mask;
	void* GetElmPtr(void* Base){
		return StaticMode?(void*)OffsetFromClassRoot:(void*)(int(Base)+OffsetFromClassRoot);
	}
	void CheckValidRegistration(void* Base,const char* MemName);
};
class DIALOGS_API OneClassStorage{
public:
	OneClassStorage(){
		OneMemb=NULL;
		CurrentSet=-1;
		ClassTopic=NULL;
	}
	~OneClassStorage();
	int CurrentSet;
	BaseClass* OneMemb;
	DynArray<OneClassMemberStorage*> Members;
	DynArray<char*>Children;
	DynArray<char*>Parents;
	StringsHash MembIDS;
	char* ClassTopic;
	_inline OneClassMemberStorage* CreateMember(const char* ID){
		OneClassMemberStorage* M=new OneClassMemberStorage;
		MembIDS.add(ID);
        Members.Add(M);
		M->xmlID=NEW(char,strlen(ID)+1);
		strcpy(M->xmlID,ID);
		return M;
	}
};
class ProcEnumerator{
public:
	virtual DWORD GetValue(const char* ID){return 0;}	
	virtual char* GetValue(DWORD ID){return "";}	

	virtual int   GetAmount(){return 0;}
	virtual char* GetIndexedString(int idx){return "";}
	virtual DWORD GetIndexedValue (int idx){return 0;}

	virtual char* GetCategory(int idx){return NULL;}	
};
class DIALOGS_API Enumerator{
	StringsHash Strings;
	DynArray<DWORD> Values;
	ProcEnumerator* ProcEnum;
public:
	Enumerator(){
		ProcEnum=NULL;
	}	
	~Enumerator(){
        if(ProcEnum)delete(ProcEnum);
		ProcEnum=NULL;
	}
	StringsHash Topics;	
	DynArray<DWORD> TopicRefs;
	char* EnumeratorID;
	void SetProcEnum(ProcEnumerator* Proc){
        if(ProcEnum)delete(ProcEnum);
		ProcEnum=Proc;
	}
	void  Add(char* s){
		Add(s,Values.GetAmount());
	}
	void  Add(char* s,DWORD V){
		int id=Strings.find(s);
		if(id!=-1){			
			Values[id]=V;            
		}else{
			Strings.add(s);
			Values.Add(V);
			TopicRefs.Add(0xFFFFFFFF);
		}
	}
	void  Add(char* s,DWORD V,char* Topic){
		int id=Strings.find(s);
		int tid=Topics.find(Topic);
		if(tid==-1){
            Topics.add(Topic);
			tid=Topics.find(Topic);
		}
		if(id!=-1){			
			Values[id]=V;            
		}else{
			Strings.add(s);
			Values.Add(V);
			TopicRefs.Add(tid);
		}
	}
	void  Add(char* s,char* Topic){
		Add(s,Values.GetAmount(),Topic);
	}
	DWORD Get(char* s){
		if(ProcEnum){
			DWORD V=ProcEnum->GetValue(s);
			if(V!=0xFFFFFFFF)return V;
		}
		int idx=Strings.find(s);
		if(idx!=-1){
			return Values[idx];
		}else return -1;
	}
	char* Get(DWORD V){
		if(ProcEnum){
			char* s=ProcEnum->GetValue(V);
			if(s)return s;
		}
		int idx=Values.find(V);
		if(idx!=-1){
            return Strings.get(idx);
		}else return "";
	}
	char* GetStr(int idx){
		if(idx<Values.GetAmount())return Strings.get(idx);
		else if(ProcEnum)return ProcEnum->GetIndexedString(idx-Values.GetAmount());
		return "";
	}
	DWORD GetVal(int idx){
		if(idx<Values.GetAmount())return Values[idx];
		else if(ProcEnum) return ProcEnum->GetIndexedValue(idx-Values.GetAmount());
		return 0;
	}
	int GetAmount(){
		return Values.GetAmount()+(ProcEnum?ProcEnum->GetAmount():0);
	}
	int FindStr(char* s){
		int idx=Strings.find(s);
		if(idx!=-1)return idx;
		if(ProcEnum){
			int N=ProcEnum->GetAmount()-Values.GetAmount();
			for(int i=0;i<N;i++){
				char * si=ProcEnum->GetIndexedString(i);
				if(si&&!strcmp(si,s))return Values.GetAmount()+i;
			}
		}
		return -1;
	}
	int FindVal(DWORD v){
		int idx=Values.find(v);
		if(idx!=-1)return idx;
		if(ProcEnum){
			int N=ProcEnum->GetAmount()-Values.GetAmount();
			for(int i=0;i<N;i++){
				DWORD V=ProcEnum->GetIndexedValue(i);
				if(V==v)return Values.GetAmount()+i;
			}
		}
		return -1;
	}
	void  Clear(){
		Strings.clear();
        Values.Clear();
		Topics.clear();	
		TopicRefs.Clear();
	}
	virtual char* GetCategory(int idx){
		if(idx<Values.GetAmount()){
            DWORD id=TopicRefs[idx];
			if(idx!=0xFFFFFFFF){
                return Topics.get(id);
			}
			return NULL;
		}else{
            if(ProcEnum)return ProcEnum->GetCategory(idx-Values.GetAmount());
		}
		return NULL;
	}	
};
class DIALOGS_API Enumerators{
	DynArray<Enumerator*> Enums;
	StringsHash EnumNames;
public:
	_inline Enumerator* Create(const char* Name){
		Enumerator* E;
		SAFENEW
			E=new Enumerator;		
			Enums.Add(E);
			EnumNames.add(Name);
			E->EnumeratorID=EnumNames.get(EnumNames.find(Name));
		ENDNEW;
		return E;
	}
	_inline Enumerator* Get(const char* Name){
		int idx=EnumNames.find(Name);
		if(idx!=-1){
            return Enums[idx];
		}else{
			return Create(Name);
		}
	}
	_inline ClearAll(){
		Enums.Clear();
		EnumNames.clear();
	}    
};

extern DIALOGS_API Enumerators ENUM;

class DIALOGS_API ClassGarbage{
	DynArray<OneClassStorage*> Storage;
	StringsHash NamesHash;

public:
	//ClassGarbage();
	//~ClassGarbage();
	_inline OneClassStorage* GetClass(const char* ClassName){
		int idx=NamesHash.find(ClassName);
		if(idx!=-1)return Storage[idx];
		return CreateClassStorage(ClassName);
	}
	_inline OneClassStorage* CreateClassStorage(const char* ClassName){
		OneClassStorage* ST;
		SAFENEW;
			NamesHash.add(ClassName);
			ST=new OneClassStorage;
			Storage.Add(ST);
		ENDNEW;
		return ST;
	}
};
#define AUTONEW(x)	virtual BaseClass* new_element(){return new x;}\
	virtual OneClassStorage* GetClassStorage(){\
		static OneClassStorage* OCS=NULL;\
		if(!OCS)OCS=CGARB.GetClass(GetClassName());\
		return OCS;\
	}
//	virtual x* new_element(){return new x;}
extern DIALOGS_API ClassGarbage CGARB;
struct ClassExpParams{
	DynArray<char*> ExpList;
	DynArray<char*> TopicsList;
	DynArray<int>   TopicsIdxs;
};
class BaseClass;
class _str;
struct OneClassPointer{
	DWORD Index;
	int RefCount;
	BaseClass* ClassPtr;
	BaseClass* ROOT;
	BaseClass* FirstRef;
	_str* NamePtr;
	DString RefName;
	DString ClassName;
	OneClassPointer(){
		Index=0;
		RefCount=0;
		ClassPtr=0;
		ROOT=NULL;
		FirstRef=NULL;
	}
};
class DIALOGS_API ClassPointersGarbage{
public:
	~ClassPointersGarbage();
	DynArray<OneClassPointer*> CPointer;
	DynArray<OneClassPointer*> FreePtrs;
    DWORD AddClass(BaseClass* Ptr);
	void  DeleteClass(BaseClass* Ptr);
	DWORD AddRef(char* ClassID,char* MemberName,BaseClass* ROOT);
	void DelRef(DWORD ID);
	void AddRef(DWORD ID);
	DWORD TryToLinkClass(BaseClass* ClassPtr);
	DWORD FindClass(BaseClass* ClassPtr);
};
extern DIALOGS_API ClassPointersGarbage CPGARB;
class _str;
class DIALOGS_API BaseClass{
protected:
	//there the pointer to the parent should be stored. BaseClass is like a tree, and you always
	//can get the root of the tree using ParentBC
	BaseClass* ParentBC;
	static const char* CurrentSaveFile;
	static bool ReadOnlyMode;
	static bool InvisibleMode;
	static bool SaveInShortForm;
	static bool StaticMode;
	static bool NoSaveMode;
public:
	BaseClass& operator = (BaseClass& bc){
        assert(1);
		return *this;
	}
	static const char* GetCurrentSaveFile(){
		return CurrentSaveFile;
	};
	//BaseClass is like a tree, and you always
	//can get the root of the tree using this function
	BaseClass* GetRoot();
	BaseClass();
	virtual ~BaseClass();
	//clearing all registered content of the class
	virtual void reset_class(void* DataPtr);
	//registration the member of the class. Used if section SAVE..ENDSAVE
	void SetReadOnlyMode(){ReadOnlyMode=true;};
	void SetInvisibleMode(){InvisibleMode=true;};
	void SetStaticMode(){StaticMode=true;};
	void SetNoSaveMode(){NoSaveMode=true;};
	void RegisterOneMember(BaseClass* Member,void* ptr,const char* id,void* Extra=NULL,const char* Host=NULL,bool DirectCast=false,DWORD Mask=0xFFFFFFFF);
	void RegisterOneMemberIntProperty(BaseClass* Member,tpIntPropertyReader* IntPropR,tpIntPropertyWriter* IntPropW,const char* id,void* Extra=NULL,const char* Host=NULL,DWORD Mask=0xFFFFFFFF);
	void RegisterOneMemberProperty(BaseClass* Member,tpPropertyReader* PropR,tpPropertyWriter* PropW,const char* id,void* Extra=NULL,const char* Host=NULL,DWORD Mask=0xFFFFFFFF);	
	//getting class member by its name. This function  returns:
	//ClassPtr - the pointer to the object, that has type of the member, 
	//for example fot int it will return pointer to _int object
	//ElmPtr - the pointer to the member data. You shoul remember, that ElmPtr and ClassPtr are
	//essentialy different pointers. ClassPtr is a type of object, ElmPtr is a pointer to the 
	//physical storage of the object
	//ExtraPtr - Some additional data, that was stored during RegisterOneMember call. It can be simply used 
	//for enumerators, also for for dependent objects 
	bool GetElementByName(const char* ElmName,BaseClass** ClassPtr,void** ElmPtr,void** ExtraPtr,void* base);
	//returns information block about given member
	OneClassMemberStorage* GetElementStorage(const char* ElmName);
	const char* GetElementHostClass(const char* ElmName);
	const char* GetElementHostClass(int Index);
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();
	virtual void RegisterMembers();
	virtual int GetAmountOfElements();
	virtual const char* GetElementID(int Index);
	virtual const char* GetElementView(int Index,const char* LocalName);
	virtual const char* GetThisElementView(const char* LocalName){return NULL;};
	virtual BaseClass* GetElementClass(int Index);
	OneClassMemberStorage* GetElementCMS(int Index);
	virtual void* GetElementPtr(int Index,void* base);
	virtual void* GetElementExtraPtr(int Index);
	virtual bool DelElement(int Index){return false;}
	virtual void LoadPostProcess(void* DataPtr,void* ExtraData);
	bool	LoadMember(xmlQuote& xml,void* ClassPtr,int MemIndex,ErrorPager* Error,void* Extra);
	bool	LoadMemberDirect(void* ElementPtr,void* ClassPtr,int MemIndex);	
	virtual bool CheckDirectCasting(){
		return false;
	}
	virtual int ExpandWith(const char* ElmName,void* base){
		return -1;
	}
	virtual int GetExpansionRules(){
		return 0;//0-no expansion 1-expand with base type only 2-expand with child classes
	}	
	virtual const char* GetExpansionBaseClass(){
		return NULL;
	}
	virtual bool ForceProperty();//if returns true, the class will always be at bottom position in ComplexClassEditor 		
	virtual bool ForceSimplification(){return false;}//if returns true, this node will be hidden in class editor
	//optype:
	//-1 - up
	// 0 - check
	// 1 - down
	virtual bool MoveElement(int index,char opType){return false;}
	//this function is to limit expansion rules of some member of this class,
	//for example class A has members B and C, member B has expansion rules, that allows to 
	//expand class B with classes D and E. But we want that sometimes (in dependence on class C)
	//that class B should not be expanded with class E sometimes. Then 
	//A->AskParentForUsingExpansionClass("A","E") should return false
	virtual bool AskParentForUsingExpansionClass(char* MemberName,char* ClassName){return true;}
	//  added by Silver, 21.08.2003
	bool		HasParentClass(const char* ParentClassName);	
	virtual     bool Copy(void* SrcData,void* SrcDataExtra,BaseClass* Dest,void* DestData,void* DstDataExtra);
	//-by Drew-
	//only for directly casting classes:
	virtual     bool ReadFromFile    (const char* Name);
	virtual     bool SafeReadFromFile(const char* Name);
	virtual     bool WriteToFile     (const char* Name);
	virtual     bool Copy            (BaseClass* Dest, bool Add=true);
	virtual		const char* GetSource(){return NULL;};
	//- special function for integrating in editor -
	virtual int        GetAmountOfEditableElements    (void* DataPtr,DWORD Options=7);//1-enum leafs 2-enum nodes 4-enum adds, so 7-enum everything
	virtual int        GetIndexOfEditableElement      (int Index,void* DataPtr,DWORD Option=7);//return index that is in range 0..GetAmountOfElements()-1
	virtual BaseClass* GetEditableElementClass        (int Index,void* DataPtr,DWORD Option=7);
	virtual void*      GetEditableElementData         (int Index,void* DataPtr,DWORD Option=7);
	virtual void*      GetEditableElementExtra        (int Index,void* DataPtr,DWORD Option=7);
	virtual bool       GetEditableElementExpansionList(ClassExpParams& EXP,int Index,void* DataPtr,DWORD Option=7,BaseClass* Parent=NULL,const char* ElmName=NULL);
	virtual bool       CheckIfElementIsNode           (int Index,void* DataPtr);//Index: 0..GetAmountOfElements()-1
    virtual bool       CheckIfEditableElementIsNode   (int Index,void* DataPtr,DWORD Option=7);
	virtual const char* GetEditableElementName        (int Index,void* DataPtr,DWORD Option=7);
	virtual const char* GetEditableElementView        (int Index,void* DataPtr,const char* LocalName,DWORD Option=7);
	virtual void       GetElementMessageAndHintInEditor(DString& Message,DString& Hint,BaseClass* Parent,char* ElmID);	
	virtual void	   EvaluateFunction()             {}
	virtual bool       CheckIfFunction()			  {return false;}
	virtual int        GetSectionStatus()			  {return 0;}
	virtual void       SetSectionStatus				  (int s){}
	virtual bool       CheckIfElementReadOnly		  (int Index,void* DataPtr,DWORD Option=7);
	virtual bool       CheckIfElementInvisible		  (int Index,void* DataPtr,DWORD Option=7);
	//-----------masking elements of class---------------//
	virtual DWORD GetClassMask(){return 0xFFFFFFFF;}
	virtual int GetRealMaskedPosition(int Index);//in - masked position out - true position in list of all members
	//------Global Class Member Indentification------//
	virtual _str* GetObjectNamePointer(){return NULL;}	//returns pointer to the _str, that contains name 
														//of this class member,it is used for ClassRef	
	virtual bool CheckIfObjectIsGlobal(){return false;}
	virtual DWORD GetObjectGlobalID(){return 0xFFFFFFFF;}
	virtual void SetObjectGlobalID(DWORD ID){}
	virtual BaseClass* GetParent(){return ParentBC;}
	virtual void SetParent(BaseClass* Parent);
	//--------------checking for errors of registration-------------//
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){return true;}
	//-----accessing by field name------//
	const char* GetStringField(const char* FieldName);
	int   GetIntField  (const char* FieldName,int DefaultVal=0);
	float GetFloatField(const char* FieldName,float DefaultVal=0.0f);
	//-------------short form saving----------------//
	void DeleteDefaultSubFields(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool ShouldSaveInShortForm(){return false;}

	AUTONEW(BaseClass);
};
//class BaseNode:public BaseClass{
//protected:
//	BaseClass* ParentBC;
//public:
//	BaseNode(){ParentBC=NULL;}
//	virtual BaseClass* GetParent(){return ParentBC;}
//	virtual void SetParent(BaseClass* Parent){ParentBC=Parent;}
//	AUTONEW(BaseNode);
//};
//_str
class DIALOGS_API _str:public DString,public BaseClass{
public:
	~_str();

	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	

	/* VITYA */
	_str& operator = (		char*		s  )	{ DString::operator = (s);			return *this;	};
	_str& operator = (const char*		s  )	{ DString::operator = (s);			return *this;	};
	_str& operator = (const int&		a  )	{ DString::operator = (a);			return *this;	};
	_str& operator = (		DString&	ds1)	{ DString::operator = (ds1.str);		return *this;	};
	_str& operator = (const DString&	ds1)	{ DString::operator = (ds1.str);		return *this;	};
	_str& operator = (		_str&		st1)	{ DString::operator = (st1.str);	return *this;	};
	_str& operator = (const _str&		st1)	{ DString::operator = (st1.str);	return *this;	};

	_str& operator + (		char*		s  )	{ DString::operator + (s);			return *this;	};
	_str& operator + (const char*		s  )	{ DString::operator + (s);			return *this;	};
	_str& operator + (const int&		a  )	{ DString::operator + (a);			return *this;	};
	_str& operator + (		DString&	ds1)	{ DString::operator + (ds1.str);	return *this;	};
	_str& operator + (const DString&	ds1)	{ DString::operator + (ds1.str);	return *this;	};
	_str& operator + (		_str&		st1)	{ DString::operator + (st1.str);	return *this;	};
	_str& operator + (const _str&		st1)	{ DString::operator + (st1.str);	return *this;	};

	_str& operator += (		 char*		s  )	{ DString::operator +=(s);			return *this;	};
	_str& operator += (const char*		s  )	{ DString::operator +=(s);			return *this;	};
	_str& operator += (const int&		a  )	{ DString::operator +=(a);			return *this;	}
	_str& operator += (		 DString&	ds1)	{ DString::operator +=(ds1.str);	return *this;	};
	_str& operator += (const DString&	ds1)	{ DString::operator +=(ds1.str);	return *this;	};
	_str& operator += (		 _str&		st1)	{ DString::operator +=(st1.str);	return *this;	};
	_str& operator += (const _str&		st1)	{ DString::operator +=(st1.str);	return *this;	};

	bool operator == (		char*		s  )	{ return	DString::operator ==(s);		};
	bool operator == (const char*		s  )	{ return	DString::operator ==(s);		};
	bool operator == (		DString&	ds1)	{ return	DString::operator ==(ds1.str);	};
	bool operator == (const DString&	ds1)	{ return	DString::operator ==(ds1.str);	};
	bool operator == (		_str&		st1)	{ return	DString::operator ==(st1.str);	};
	bool operator == (const _str&		st1)	{ return	DString::operator ==(st1.str);	};

	bool operator != (		char*		s  )	{ return	DString::operator !=(s);		};
	bool operator != (const char*		s  )	{ return	DString::operator !=(s);		};
	bool operator != (		DString&	ds1)	{ return	DString::operator !=(ds1.str);	};
	bool operator != (const DString&	ds1)	{ return	DString::operator !=(ds1.str);		};
	bool operator != (		_str&		st1)	{ return	DString::operator !=(st1.str);	};
	bool operator != (const _str&		st1)	{ return	DString::operator !=(st1.str);	};
	virtual void reset_class(void* ptr){
		_str* s=dynamic_cast<_str*>((BaseClass*)ptr);
		if(s)s->Clear();
	}
	AUTONEW(_str);    
};//_str
class ReferableBaseClass:public BaseClass{
	DWORD ID;
public:
	_str Name;
	ReferableBaseClass(){ID=0xFFFFFFFF;CPGARB.AddClass(this);}
	~ReferableBaseClass(){CPGARB.DeleteClass(this);}
	virtual DWORD GetObjectGlobalID(){return ID;}
	virtual void SetObjectGlobalID(DWORD id){ID=id;}
	virtual BaseClass* GetParent(){return ParentBC;}
	virtual void SetParent(BaseClass* Parent){ParentBC=Parent;}
	virtual _str* GetObjectNamePointer(){return &Name;}
	AUTONEW(ReferableBaseClass);
};
class ReferableBaseClassWithSource:public ReferableBaseClass{
public:
	_str Source;
	bool AlwaysLoadFromSource;
	bool AlwaysSaveToSource;
	virtual const char* GetSource(){
		return Source.str;
	};
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL){		
		ReferableBaseClass::Save(xml,ClassPtr,Extra);
		if(GetCurrentSaveFile()){
			if(AlwaysSaveToSource&&Source.str&&Source.str[0]){				
				xml.WriteToFile(Source.str);
			}
		}
	};
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL){
		if(GetCurrentSaveFile()){
			if(AlwaysLoadFromSource&&Source.str&&Source.str[0]){
				xml.ClearAll();
				xml.ReadFromFile(Source.str);
			}
		}
		return ReferableBaseClass::Load(xml,ClassPtr,Error,Extra);
	};
	virtual bool ReadFromFile(const char* Name){
		bool R=ReferableBaseClass::ReadFromFile(Name);
		if(Name[0]!='$'){
			Source=Name;
		}
		return R;
	};
	virtual bool SafeReadFromFile(const char* Name){
		bool R=ReferableBaseClass::SafeReadFromFile(Name);
		if(Name[0]!='$'){
			Source=Name;
		}
		return R;
	};
	virtual bool WriteToFile(const char* Name){
		if(Name[0]!='$'){
			Source=Name;
		}
		return ReferableBaseClass::WriteToFile(Name);
	};
};
//--implementation of some classes--//
class DIALOGS_API _int:public BaseClass{
public:
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();
	virtual void reset_class(void* ptr){
		*((int*)ptr)=0;
	}
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	AUTONEW(_int);
};

class DIALOGS_API _DWORD:public BaseClass{
public:
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();    	
	virtual void reset_class(void* ptr){
		*((DWORD*)ptr)=0;
	}
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	AUTONEW(_DWORD);
};

class DIALOGS_API _float:public BaseClass{
public:
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName();  
	virtual void reset_class(void* ptr){
		*((float*)ptr)=0.0f;
	}
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return !strcmp(TypeName,"float");
	}
	AUTONEW(_float);
}; // class _float
class _float01:public _float{
public:
	virtual const char* GetClassName(){
		return "_float01";
	}    	
	AUTONEW(_float01);
};
class _float0_10:public _float{
public:
	virtual const char* GetClassName(){
		return "_float0_10";
	}    	
	AUTONEW(_float0_10);
};
class DIALOGS_API _bool:public BaseClass{
public:
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL);
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL);
	virtual const char* GetClassName(); 
	virtual void reset_class(void* ptr){
		*((bool*)ptr)=false;
	}
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==1;
	}
	AUTONEW(_bool);
}; // class _bool

class DIALOGS_API _index:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			xml.Assign_string(E->Get(*((int*)ClassPtr)));
		}
	}
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL){
		void KeyTestMem();
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			const char* s=xml.Get_string();
			if(s){
				int p=E->Get((char*)xml.Get_string());
				if(p!=-1){
					*((int*)ClassPtr)=E->Get((char*)xml.Get_string());					
				}else{
					*((int*)ClassPtr)=E->GetVal(0);
					//Error->xml_print(xml,"identifier \"%s\" not found in list \"%s\" for variable \"%s\"\n",xml.Get_string(),E->EnumeratorID,xml.GetQuoteName());
				}			
			}else{
				*((int*)ClassPtr)=E->GetVal(0);
				//Error->xml_print(xml,"xml node is empty for variable %s\n",xml.GetQuoteName());
			}
			return true;
		}else Error->xml_print(xml,"Enumerator not assigned for type \"_index\" for variable %s\n",xml.GetQuoteName());
		return false;
	}
	virtual const char* GetClassName(){
		return "_index";
	}
	virtual void reset_class(void* ptr){
		*((int*)ptr)=0;
	}
	AUTONEW(_index);
};
class DIALOGS_API _byte_index:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==1;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			xml.Assign_string(E->Get(*((BYTE*)ClassPtr)));
		}
	}
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			const char* s=xml.Get_string();
			if(s){
				int p=E->Get((char*)xml.Get_string());
				if(p!=-1){
					*((BYTE*)ClassPtr)=E->Get((char*)xml.Get_string());
					return true;
				}else Error->xml_print(xml,"identifier \"%s\" not found in list \"%s\" for variable \"%s\"\n",xml.Get_string(),E->EnumeratorID,xml.GetQuoteName());
			}else Error->xml_print(xml,"xml node is empty for variable %s\n",xml.GetQuoteName());
		}else Error->xml_print(xml,"Enumerator not assigned for type \"_index\" for variable %s\n",xml.GetQuoteName());
		return false;
	}
	virtual const char* GetClassName(){
		return "_byte_index";
	}
	virtual void reset_class(void* ptr){
		*((byte*)ptr)=0;
	}
	AUTONEW(_byte_index);
};
class DIALOGS_API _word_index:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==2;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			xml.Assign_string(E->Get(*((WORD*)ClassPtr)));
		}
	}
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			const char* s=xml.Get_string();
			if(s){
				int p=E->Get((char*)xml.Get_string());
				if(p!=-1){
					*((WORD*)ClassPtr)=E->Get((char*)xml.Get_string());
					return true;
				}else Error->xml_print(xml,"identifier \"%s\" not found in list \"%s\" for variable \"%s\"\n",xml.Get_string(),E->EnumeratorID,xml.GetQuoteName());
			}else Error->xml_print(xml,"xml node is empty for variable %s\n",xml.GetQuoteName());
		}else Error->xml_print(xml,"Enumerator not assigned for type \"_index\" for variable %s\n",xml.GetQuoteName());
		return false;
	}
	virtual const char* GetClassName(){
		return "_word_index";
	}
	virtual void reset_class(void* ptr){
		*((word*)ptr)=0;
	}
	AUTONEW(_word_index);
};
class DIALOGS_API _accamulator:public BaseClass{
public:
	virtual bool CheckCompartabilityWith(const char* TypeName,int TypeSize){
		return TypeSize==4;
	}
	virtual void Save(xmlQuote& xml,void* ClassPtr,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			xml.Assign_string(E->Get(*((int*)ClassPtr)));
		}
	}
	virtual bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra=NULL){
		if(Extra){
			Enumerator* E=(Enumerator*)Extra;
			char* S=(char*)xml.Get_string();
			if(S[0]){
				int p=E->Get(S);
				if(p==-1){
					E->Add(S);
					p=E->Get(S);
				}
				*((int*)ClassPtr)=p;
				return true;
			}else Error->xml_print(xml,"xml node is empty for variable %s\n",xml.GetQuoteName());
		}else Error->xml_print(xml,"Enumerator not assigned for type <_accamulator> for variable %s\n",xml.GetQuoteName());
		return false;
	}
	virtual const char* GetClassName(){
		return "_accamulator";
	}
	virtual void reset_class(void* ptr){
		*((int*)ptr)=0;
	}
	AUTONEW(_index);
};
#define TEMPL(c,elm) #c"."#elm
template<class X>class ClassArray:public BaseClass,public DynArray<X*>{
public:
	virtual ~ClassArray(){
		reset_class(this);
	};
	virtual void reset_class(void* ptr){
		ClassArray<X>* CA=(ClassArray<X>*)ptr;
		for(int i=0;i<CA->GetAmount();i++)if((*CA)[i]){
			try{
				delete(CA->GetValues()[i]);
			}catch(...){
			}
			CA->GetValues()[i]=NULL;
		}
		if(CA->Values)delete[](CA->Values);
		CA->Values=NULL;
		CA->NValues=0;
		CA->MaxValues=0;
	}
	void Clear(){
        reset_class(this);
	}
	const char* GetClassName(){
		static char N[64]="";
		if(N[0]!='C'){
			X* x=new X;
			sprintf(N,"ClassArray.%s",x->GetClassName());
			delete(x);
		}
		return N;
	}
	int GetAmountOfElements(){
		return GetAmount();
	}
	const char* GetElementID(int Index){
		return ((*this)[Index])!=NULL?(*this)[Index]->GetClassName():"NULL";
		
	}
	BaseClass* GetElementClass(int Index){
		return (*this)[Index];
	}
	void* GetElementPtr(int Index,void* base){
		return (*((ClassArray<X>*)base))[Index];
	}
	__forceinline int Add(X* V){
		if(V)V->SetParent(this);
		return DynArray<X*>::Add(V);
	}
	__forceinline int Add(X* V,int N){
		if(V)V->SetParent(this);
		return DynArray<X*>::Add(V,N);
	}
	__forceinline bool Insert(int pos, X* V){
		if(V)V->SetParent(this);
		return DynArray<X*>::Insert(pos,V);
	}
	virtual int ExpandWith(const char* ElmName,void* base){
        ClassArray<X>* BC=(ClassArray<X>*)base;
		OneClassStorage* OCS=CGARB.GetClass(ElmName);
		if(OCS){
			BaseClass* NewBase=OCS->OneMemb;
			if(NewBase){
	            BaseClass* B1=NewBase->new_element();
				B1->SetParent(this);
				BC->Add((X*)B1);
				return BC->GetAmount()-1;
			}else return -1;
		}else return -1;
	}
	virtual int GetExpansionRules(){
		return 2;//0-no expansion 1-expand with base type only 2-expand with child classes
	}
	virtual bool MoveElement(int index,char opType){
		if(opType==0)return true;
		/*
		if(opType>0&&index<GetAmount()-1){			
			int dest=(GetKeyState(VK_CONTROL)&0x8000)?GetAmount()-1:index+1;
			std::swap((*this)[index],(*this)[dest]);
			return true;
		}
		if(opType<0&&index>0){
			int dest=(GetKeyState(VK_CONTROL)&0x8000)?0:index-1;
			std::swap((*this)[index],(*this)[dest]);
			return true;
		}
		*/
		return Move(index,index+opType);
	}
	virtual const char* GetExpansionBaseClass(){
		static X x;
        return x.GetClassName();
	}

	virtual bool DelElement(int index){		
		if(index>=0&&index<GetAmount()){
			if((*this)[index])delete((*this)[index]);
			Del(index,1);
			return true;
		}else return false;
	}
	virtual bool CheckDirectCasting(){
		return true;
	}
	AUTONEW(ClassArray);
};
template<class X>class ClonesArray:public ClassArray<X>{
public:
	virtual int GetExpansionRules(){
		return 1;//0-no expansion 1-expand with base type only 2-expand with child classes
	}
};
template <class X> class ClassPtr:public BaseClass{
	X* ptr;
public:

	//ClassPtr& operator = (X* x){ptr=x;}
	X* Get(){
		return ptr;
	}
	void Set(X* x){
		ptr=x;
		if(ptr)ptr->SetParent(this);
	}


	ClassPtr(){
		ptr=NULL;
	}
	virtual ~ClassPtr(){
		reset_class(this);
	};
	virtual void reset_class(void* p){
		ClassPtr<X>* cp=(ClassPtr<X>*)p;
		if(cp->ptr)delete(cp->ptr);
		cp->ptr=NULL;		
	}
	void Clear(){
		reset_class(this);
	}
	const char* GetClassName(){
		static char N[64]="";
		if(!N[0]){
			X* x=new X;
			sprintf(N,"ClassPtr.%s",x->GetClassName());
			delete(x);
		}
		return N;
	}
	int GetAmountOfElements(){
		return ptr?1:0;
	}
	const char* GetElementID(int Index){		
		if(ptr)return ptr->GetClassName();
		else return "NULL";
	}
	BaseClass* GetElementClass(int Index){
		return ptr;
	}
	void* GetElementPtr(int Index,void* base){
		return ptr;
	}
	virtual int ExpandWith(const char* ElmName,void* base){
		reset_class(base);
		ClassPtr<X>* BC=(ClassPtr<X>*)base;
		OneClassStorage* OCS=CGARB.GetClass(ElmName);
		if(OCS){
			BaseClass* NewBase=OCS->OneMemb;
			if(NewBase){
				BC->ptr=(X*)NewBase->new_element();
				BC->ptr->SetParent(this);
				return 0;
			}else return -1;
		}else return -1;
	}
	virtual int GetExpansionRules(){
		return 2+4+8;//0-no expansion 1-expand with base type only 2-expand with child classes
		//4-allow manual type change
		//8-allow short form of presentation
	}
	virtual bool MoveElement(int index,char opType){
		return false;
	}
	virtual const char* GetExpansionBaseClass(){
		static X x;
		return x.GetClassName();
	}

	virtual bool DelElement(int index){		
		reset_class(this);		
		return true;		
	}
	virtual bool CheckDirectCasting(){
		return true;
	}
	AUTONEW(ClassPtr);
};
template <class X> class ClonePtr:public ClassPtr<X>{
	virtual int GetExpansionRules(){
		return 1+4+8;//0-no expansion 1-expand with base type only 2-expand with child classes
		//4-allow manual type change
		//8-allow short form of presentation
	}
};
template <class X> class DIALOGS_API ClassRef:public BaseClass{
public:
	DWORD CPG_Index;	
	X* Get(){
		if(CPG_Index==0xFFFFFFFF)return NULL;
        return (X*)CPGARB.CPointer[CPG_Index]->ClassPtr;
	}
	void Set(X* x){		
		CPGARB.DelRef(CPG_Index);
		if(x&&x->GetObjectNamePointer()){
			CPG_Index=x->GetObjectGlobalID();
			CPGARB.AddRef(CPG_Index);
		}else CPG_Index=0xFFFFFFFF;
	}
	void SetObjectName(char* ObjName){
		CPGARB.DelRef(CPG_Index);
		static char* cname=NULL;
		if(!cname){
			X* x=new X;
			cname=(char*)x->GetClassName();
			delete(x);
		}
		CPG_Index=CPGARB.AddRef(cname,ObjName,this);
	}
	const char* GetObjectName(){
		if(CPG_Index!=0xFFFFFFFF){
            OneClassPointer* CP=CPGARB.CPointer[CPG_Index];
			if(CP){
				if(CP->NamePtr)return CP->NamePtr->str;
				else if(CP->RefName.str)return CP->RefName.str;
			}
		}
		return NULL;
	}
	ClassRef(){
		CPG_Index=0xFFFFFFFF;
	}
	virtual ~ClassRef(){
		CPGARB.DelRef(CPG_Index);
		reset_class(this);
	};
	virtual void reset_class(void* p){
        CPG_Index=0xFFFFFFFF;		
	}
	void Clear(){
		CPGARB.DelRef(CPG_Index);
		CPG_Index=0xFFFFFFFF;
		reset_class(this);
	}
	const char* GetClassName(){
		static char N[64]="";
		if(!N[0]){
			DString D;
			D.Assign((char*)typeid(X).name());
			ConvTypeName(D);			
			sprintf(N,"ClassRef.%s",D.str);//???
			D.Free();
		}
		return N;
	}
	const char* GetElementID(int Index){		
		X* ptr=Get();
		if(ptr)return ptr->GetClassName();
		else return BaseClass::GetElementID(Index);
	}
	BaseClass* GetElementClass(int Index){
		return Get();
	}
	void* GetElementPtr(int Index,void* base){
		return Get();
	}
	int GetAmountOfElements(){
        X* x=Get();
		return x?1:0;
	}
	virtual bool CheckDirectCasting(){
		return true;
	}
	bool Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
		if(xml.GetNSubQuotes()==2){
			xmlQuote* xml1=xml.GetSubQuote("ClassName");
			xmlQuote* xml2=xml.GetSubQuote("RefName");
			if(xml1&&xml2&&xml1->Get_string()&&xml2->Get_string()){
				ClassRef* CR=((ClassRef*)ClassPtr);
				CPGARB.DelRef(CR->CPG_Index);
                CR->CPG_Index=CPGARB.AddRef(
					(char*)xml1->Get_string(),
					(char*)xml2->Get_string(),
					CR);
			}
		}
		return true;
	}
	void Save(xmlQuote& xml,void* ClassPtr,void* Extra){
		X* ptr=((ClassRef*)ClassPtr)->Get();
		if(ptr){
			xmlQuote* xml1=new xmlQuote("ClassName");
			xml1->Assign_string((char*)ptr->GetClassName());
			xmlQuote* xml2=new xmlQuote("RefName");
			if(ptr->GetObjectNamePointer()&&ptr->GetObjectNamePointer()->str){
				xml2->Assign_string(ptr->GetObjectNamePointer()->str);
			}
			xml.AddSubQuote(xml1);
			xml.AddSubQuote(xml2);
		}
	}
	AUTONEW(ClassRef);
};
template<class X,class BaseForX>class DIALOGS_API LinearArray:public BaseClass,public DynArray<X>{
public:
	BaseForX B;
	X B1;
	virtual ~LinearArray(){
		reset_class(this);
	};
	virtual void reset_class(void* DataPtr){	
		LinearArray* LA=(LinearArray*)DataPtr;
		LA->DynArray<X>::~DynArray();
	}
	const char* GetClassName(){
		static char N[64]="";
		if(!N[0]){
			BaseForX* x=new BaseForX;
			sprintf(N,"LinearArray.%s.%s",typeid(X).name(),x->GetClassName());
			delete(x);
		}
		return N;
	}
	int GetAmountOfElements(){
		return GetAmount();
	}
	const char* GetElementID(int Index){
		static char temp[16];
		sprintf(temp,"e%d",Index);
		return temp;
	}
	BaseClass* GetElementClass(int Index){
		return &B;
	}
	void* GetElementPtr(int Index,void* base){
		LinearArray<X,BaseForX>* AR=(LinearArray<X,BaseForX>*)base;
		return AR->GetValues()+Index;
	}
	virtual int ExpandWith(const char* ElmName,void* base){
		LinearArray<X,BaseForX>* BC=(LinearArray<X,BaseForX>*)base;
		BC->Add(B1);
		return BC->GetAmount()-1;
	}
	virtual bool DelElement(int index){
		if(index>=0&&index<GetAmount()){
			Del(index,1);
			return true;
		}else return false;
	}
	virtual int GetExpansionRules(){
		return 1;//0-no expansion 1-expand with base type only 2-expand with child classes
	}
	virtual bool MoveElement(int index,char opType){
		if(opType==0)return true;
		if(opType>0&&index<GetAmount()-1){
			std::swap((*this)[index],(*this)[index+1]);
			return true;
		}
		if(opType<0&&index>0){
			std::swap((*this)[index],(*this)[index-1]);
			return true;
		}
		return false;
	}
	virtual const char* GetExpansionBaseClass(){
        return "";
	}
	typedef LinearArray<X,BaseForX> LA;
	virtual bool CheckDirectCasting(){
		return true;
	}
	AUTONEW(LA);
};
#define INVISIBLE SetInvisibleMode();
#define READONLY SetReadOnlyMode();
#define STATIC SetStaticMode();
#define NOSAVE SetNoSaveMode();


#define INT_PROPERTY(classname,rf,wf)\
	static int __##rf(void* c){return ((classname*)c)->rf();}\
	static void __##wf(void* c,int v){((classname*)c)->wf(v);}

#define BOOL_PROPERTY(classname,rf,wf)\
	static void* __##rf(void* c){static bool temp;temp=((classname*)c)->rf();return &temp;}\
	static void __##wf(void* c,void* v){bool temp=*((bool*)v);((classname*)c)->wf(temp);}

#define FLOAT_PROPERTY(classname,rf,wf)\
	static float __##rf(void* c){return ((classname*)c)->rf();}\
	static void __##wf(void* c,float v){((classname*)c)->wf(v);}

#define PROPERTY(classname,rf,wf)\
	static void* __##rf(void* c){return ((classname*)c)->rf();}\
	static void __##wf(void* c,void* v){((classname*)c)->wf(v);}

#define REG_INTPROP(typ,fieldID,rf,wf)\
	{typ* m=new typ;\
	RegisterOneMemberIntProperty(m,&__##rf,&__##wf,#fieldID,NULL,this->GetClassName(),CurrMask);}

#define REG_INDEXPROP(type,fieldID,rf,wf,EnumID){type* m=new type;Enumerator* E=ENUM.Get(#EnumID);if(!E)E=ENUM.Create(#EnumID);RegisterOneMemberIntProperty(m,&__##rf,&__##wf,#fieldID,E,this->GetClassName(),CurrMask);}

#define REG_PROP(typ,fieldID,rf,wf)\
{typ* m=new typ;\
	RegisterOneMemberProperty(m,&__##rf,&__##wf,#fieldID,NULL,this->GetClassName(),CurrMask);}

#define REG_LOADSAVE(StartDir,mask){char cc[256];sprintf(cc,"LSENUM_%s",this->GetClassName());Enumerator* E=ENUM.Get(cc);if(!E->GetAmount()){E->Add(StartDir);E->Add(mask);}}

#define REG_LOADSAVE_FOR_CLASS(Class,StartDir,mask){Class* c=new Class;char cc[256];sprintf(cc,"LSENUM_%s",c->GetClassName());Enumerator* E=ENUM.Get(cc);if(!E->GetAmount()){E->Add(StartDir);E->Add(mask);delete(c);}}
#define typerr(field)\
	if(!m->CheckCompartabilityWith(typeid(field).name(),sizeof field)){\
	PrintError("Incompartible type used for %s %s::%s : %s",typeid(field).name(),GetClassName(),#field,m->GetClassName());\
	};
#define REG_MEMBER(type,fieldID)\
	{\
		type* m=new type;\
		BaseClass::RegisterOneMember((BaseClass*)(void*)m,&fieldID,#fieldID,NULL,this->GetClassName(),0,CurrMask);\
		typerr(fieldID);\
	}
#define REG_MEMBER_EX(type,fieldID,Name)\
{\
	type* m=new type;\
	BaseClass::RegisterOneMember((BaseClass*)(void*)m,&fieldID,#Name,NULL,this->GetClassName(),0,CurrMask);\
	typerr(fieldID);\
}
#define REG_MEMBER_EX2(type,fieldID,Name)\
{\
	type* m=new type;\
	BaseClass::RegisterOneMember((BaseClass*)(void*)m,&fieldID,Name,NULL,this->GetClassName(),0,CurrMask);\
	typerr(fieldID);\
}
#define REG_ENUM(type,fieldID,EnumID)\
{\
	type* m=new type;\
	Enumerator* E=ENUM.Get(#EnumID);\
	if(!E)E=ENUM.Create(#EnumID);\
	BaseClass::RegisterOneMember((BaseClass*)(void*)m,&fieldID,#fieldID,E,this->GetClassName(),0,CurrMask);\
	typerr(fieldID);\
}
#define REG_ENUM_EX(type,fieldID,EnumID,Name)\
{\
	type* m=new type;\
	Enumerator* E=ENUM.Get(#EnumID);\
	if(!E)E=ENUM.Create(#EnumID);\
	BaseClass::RegisterOneMember((BaseClass*)(void*)m,&fieldID,#Name,E,this->GetClassName(),0,CurrMask);\
	typerr(fieldID);\
}
#define REG_SPRITE(spr,fid)\
	{\
		OneClassStorage* sp=CGARB.GetClass("_sprite");\
		if(sp){\
			BaseClass* m=sp->OneMemb->new_element();\
			BaseClass::RegisterOneMember((BaseClass*)(void*)m,&spr,#spr,(void*)(int(&fid)-int(&spr)),this->GetClassName(),0,CurrMask);\
			typerr(spr);\
		}\
	}
#define REG_FILEPATH(str,mask)\
	{\
	OneClassStorage* sp=CGARB.GetClass("_str");\
	if(sp){\
	BaseClass* m=sp->OneMemb->new_element();\
	BaseClass::RegisterOneMember((BaseClass*)(void*)m,&str,#str,mask,this->GetClassName(),0,CurrMask);\
	typerr(str);\
	}\
	}
#define REG_SPRITE_EX(spr,sprName,fid)\
	{\
		OneClassStorage* sp=CGARB.GetClass("_sprite");\
		if(sp){\
			BaseClass* m=sp->OneMemb->new_element();\
			BaseClass::RegisterOneMember((BaseClass*)(void*)m,&spr,#sprName,(void*)(int(&fid)-int(&spr)),this->GetClassName(),0,CurrMask);\
			typerr(spr);\
		}\
	}
#define REG_BITFILE(file,ext){OneClassStorage* sp=CGARB.GetClass("_picfile"); if(sp){\
			BaseClass* m=sp->OneMemb->new_element();\
			BaseClass::RegisterOneMember((BaseClass*)(void*)m,&file,#file,(void*)(int(&ext)-int(&file)),this->GetClassName(),0,CurrMask);\
			typerr(file);}}

#define REG_EXTRA_MEMBER(type,fieldID,ExtraString) {type* m=new type;char* ex=(char*)ExtraString;BaseClass::RegisterOneMember((BaseClass*)(void*)m,&fieldID,#fieldID,ex,this->GetClassName(),0,CurrMask);typerr(fieldID);}

//simplified bool bit field definition
#define BOOL_PROP(classname,Field)\
	static int __Get##Field(void* ptr){return ((classname*)ptr)->Field;}\
	static void __Set##Field(void* ptr,int v){((classname*)ptr)->Field=(v&255)!=0;}
#define REG_BOOL_PROP(Field) REG_INTPROP(_bool,Field,Get##Field,Set##Field)

#define SAVE(x) \
	typedef x BaseConvertor;\
	virtual const char* GetClassName(){return #x;};\
	virtual bool CheckDirectCasting(){\
		return true;\
	}\
	virtual BaseClass* new_element(){x* X=new x;if(X->CheckDirectCasting()){OneClassStorage* OCS=GetClassStorage();for(int i=0;i<OCS->Members.GetAmount();i++){OneClassMemberStorage* OCMS=OCS->Members[i];if(OCMS&&OCMS->Member&&OCMS->Member->CheckDirectCasting()){((BaseClass*)(OCMS->GetElmPtr(X)))->SetParent(X);}}}return X;}\
	virtual OneClassStorage* GetClassStorage(){\
	static OneClassStorage* OCS=NULL;\
	if(!OCS)OCS=CGARB.GetClass(GetClassName());\
	return OCS;\
	}\
	virtual void RegisterMembers(){SAFENEW;OneClassStorage* OCS=GetClassStorage();if(CheckDirectCasting()){ for(int i=0;i<OCS->Members.GetAmount();i++){OneClassMemberStorage* OCMS=OCS->Members[i];if(OCMS&&OCMS->Member&&OCMS->Member->CheckDirectCasting()){BaseClass* B=((BaseClass*)(OCMS->GetElmPtr(this)));if(B->GetParent())break;B->SetParent(this);}}}\
	if(!(OCS&&OCS->Members.GetAmount())){DWORD CurrMask=0xFFFFFFFF;

//const char* name=typeid(*this).name()+6;char* s2=strstr(name,"::");if(s2)name=s2;if(name[0] && strcmp(name,#x)){PrintError("Incorrect class registration detected for class: %s - legistered like "#x,typeid(*this).name()+6);}

#define SAVE_EX(x,y) \
	typedef y BaseConvertor;\
	virtual const char* GetClassName(){return #x;};\
	virtual bool CheckDirectCasting(){\
	return true;\
	}\
	virtual BaseClass* new_element(){return (y*)new x;};\
	virtual OneClassStorage* GetClassStorage(){\
	static OneClassStorage* OCS=NULL;\
	if(!OCS)OCS=CGARB.GetClass(GetClassName());\
	return OCS;\
	}\
	virtual void RegisterMembers(){SAFENEW;OneClassStorage* OCS=GetClassStorage();\
	if(!(OCS&&OCS->Members.GetAmount())){DWORD CurrMask=0xFFFFFFFF;


#define ENDSAVE }\
	ENDNEW;\
	}
#define SAVE_SECTION(x) CurrMask=x;
_inline char* ConvTypeName(DString& D){
	D.Replace("class ","");
	D.Replace("<",".");
	D.Replace(">","");
	D.Replace(",",".");
	return D.str;
}

template <class X>
void reg_auto(BaseClass* S,X& x,const char* name,void* EnumID=NULL,DWORD Mask=0xFFFFFFFF){
	DString D;
	D.Assign((char*)typeid(X).name());
	ConvTypeName(D);
	X* m=new X;
	S->RegisterOneMember(m,&x,name,EnumID,S->GetClassName(),1,Mask);
	D.Free();
}
template <class X,class Y>
void reg_auto_a(BaseClass* S,X& x,Y* y,const char* name,void* EnumID=NULL,DWORD Mask=0xFFFFFFFF){
	DString D;
	D.Assign((char*)typeid(X).name());
	ConvTypeName(D);
	X* m=new X;
	S->RegisterOneMember((Y*)m,&x,name,EnumID,S->GetClassName(),1,Mask);
	D.Free();
}
template <class X>
void reg_class(X* x,char* Topic=NULL){
	X* elm=(X*)x->new_element();
	DString D;
	D.Assign((char*)typeid(X).name());
	ConvTypeName(D);
	OneClassStorage*OCS=CGARB.GetClass(D.str);
	if(!OCS)OCS=CGARB.CreateClassStorage(D.str);
	if(OCS->OneMemb){
		delete(OCS->OneMemb);
		OCS->OneMemb=elm;        
		//elm=(X*)OCS->OneMemb;
	}else OCS->OneMemb=elm;
	if(!OCS->Members.GetAmount()){
		OCS->OneMemb->RegisterMembers();
	}
	if(Topic&&!OCS->ClassTopic){
		OCS->ClassTopic=new char[strlen(Topic)+1];
		strcpy(OCS->ClassTopic,Topic);
	}
	D.Free();
}
_inline void UNREG_CLASS(char* Name){
	OneClassStorage*OCS=CGARB.GetClass(Name);	
	if(OCS&&OCS->OneMemb){
		OCS->Members.Clear();
		delete(OCS->OneMemb);
		OCS->OneMemb=NULL;
	}	
}
template <class X,class Y>
void reg_class_ex(X* x,Y* y,char* Topic=NULL){
	X* elm=dynamic_cast<X*>(x->new_element());
	if(!elm)return;
	DString D;
	D.Assign((char*)typeid(X).name());
	ConvTypeName(D);
	OneClassStorage*OCS=CGARB.GetClass(D.str);
	//if(OCS){
	//	D.Free();
	//	delete(elm);
	//	return;
	//}
	if(!OCS)OCS=CGARB.CreateClassStorage(D.str);
	if(OCS->OneMemb){
		delete(elm);
	}else OCS->OneMemb=(Y*)elm;
	if(!OCS->Members.GetAmount()){
		elm->RegisterMembers();
	}
	if(Topic&&!OCS->ClassTopic){
		OCS->ClassTopic=new char[strlen(Topic)+1];
		strcpy(OCS->ClassTopic,Topic);
	}
	D.Free();
}

//registering member that is class, derived from BaseClass
#define REG_AUTO(x) reg_auto(this,x,#x,NULL,CurrMask);reg_class(&x);x.SetParent(this);
#define REG_AUTO2(x,y) reg_auto(this,x,#x,y,CurrMask);reg_class(&x);x.SetParent(this);
#define REG_AUTO_A(x,y) reg_auto_a((y*)this,x,(y*)NULL,#x,NULL,CurrMask);
#define REG_AUTO_EX(x,Name) reg_auto(this,x,#Name,NULL,CurrMask);reg_class(&x);x.SetParent(this);
#define REG_AUTO_EX_A(x,y,Name) reg_auto((y*)this,x,#Name,NULL,CurrMask);reg_class(&x);
//registering member-class with enumerator
#define REG_AUTO_ENUM(x,y) {Enumerator* E=ENUM.Get(#y);reg_auto(this,x,#x,E,CurrMask);reg_class(&x);x.SetParent(this);}
//registering class type that can be created by new
//for example, if you are using ClassArray<SomeClass>, then you
//must register classes, that can be stored in this array
#define REG_CLASS(x) {x*m=new(x);reg_class(m);delete(m);}
#define REG_CLASS_EX(x,Topic) {x*m=new(x);reg_class(m,Topic);delete(m);}
#define REG_CLASS_AMBIGUOUS(x,y) {x*m=new(x);reg_class_ex(m,(y*)NULL);delete(m);}
#define REG_CLASS_AMBIGUOUS_EX(x,y,Topic) {x*m=new(x);reg_class_ex(m,(y*)NULL,Topic);delete(m);}
//registering parent class. Deriving from several classses is
//not supported, but you can derive class from class, that is already derived from
//BaseClass
template <class x>
_inline void reg_parent(BaseClass* bas,x*m){
	OneClassStorage* OCS_parent=m->GetClassStorage();
	char* s=new char[strlen(bas->GetClassName())+1];
	strcpy(s,bas->GetClassName());
	bool noadd=0;
	for(int i=0;i<OCS_parent->Children.GetAmount();i++)if(!strcmp(OCS_parent->Children[i],s)){
        noadd=1;
	}
	if(!noadd)OCS_parent->Children.Add(s);
	else delete(s);
	OneClassStorage* OCS_my=bas->GetClassStorage();
	int Np=OCS_parent->Members.GetAmount();

	noadd=0;
	s=new char[strlen(m->GetClassName())+1];
	strcpy(s,m->GetClassName());	
	for(int i=0;i<OCS_my->Parents.GetAmount();i++)if(!strcmp(OCS_my->Parents[i],s)){
		noadd=1;
	}
	if(!noadd)OCS_my->Parents.Add(s);
	else delete(s);

	for(int i=0;i<Np;i++){
		OneClassMemberStorage* OCMS_parent=OCS_parent->Members[i];
		OneClassMemberStorage* OCMS_my=OCS_my->CreateMember(OCMS_parent->xmlID);
		OCMS_my->Member=OCMS_parent->Member->new_element();
		OCMS_my->OffsetFromClassRoot=OCMS_parent->OffsetFromClassRoot;
		OCMS_my->UseReference=OCMS_parent->UseReference;
		OCMS_my->ExtraData=OCMS_parent->ExtraData;
		OCMS_my->IntPropertyR=OCMS_parent->IntPropertyR;
		OCMS_my->GeneralPropertyR=OCMS_parent->GeneralPropertyR;
		OCMS_my->IntPropertyW=OCMS_parent->IntPropertyW;
		OCMS_my->GeneralPropertyW=OCMS_parent->GeneralPropertyW;
		OCMS_my->HostClass=OCMS_parent->HostClass;
		OCMS_my->Mask=OCMS_parent->Mask;
		OCMS_my->StaticMode=OCMS_parent->StaticMode;
		OCMS_my->ReadOnly=OCMS_parent->ReadOnly;
		OCMS_my->NoSaveMode=OCMS_parent->NoSaveMode;
		OCMS_my->Invisible=OCMS_parent->Invisible;
	}
}
#define REG_PARENT(x) {\
	REG_CLASS(x);\
	x* m=new x;\
	reg_parent((BaseConvertor*)this,m);\
	delete(m);\
}
template <class x>
_inline void reg_base(BaseClass* bas,x*m){
	OneClassStorage* OCS_parent=m->GetClassStorage();
	char* s=new char[strlen(bas->GetClassName())+1];
	strcpy(s,bas->GetClassName());
	bool noadd=0;
	for(int i=0;i<OCS_parent->Children.GetAmount();i++)if(!strcmp(OCS_parent->Children[i],s)){
		noadd=1;
	}
	if(!noadd)OCS_parent->Children.Add(s);
	else delete(s);
	OneClassStorage* OCS_my=bas->GetClassStorage();
	int Np=OCS_parent->Members.GetAmount();

	noadd=0;
	s=new char[strlen(m->GetClassName())+1];
	strcpy(s,m->GetClassName());	
	for(int i=0;i<OCS_my->Parents.GetAmount();i++)if(!strcmp(OCS_my->Parents[i],s)){
		noadd=1;
	}
	if(!noadd)OCS_my->Parents.Add(s);
	else delete(s);	
}
#define REG_BASE(x) {\
	REG_CLASS(x);\
	x* m=new x;\
	reg_base((BaseConvertor*)this,m);\
	delete(m);\
}
class BaseFunction: public BaseClass{
public:
	const char* GetClassName(){
		return "BaseFunction";
	}
	virtual bool CheckIfFunction(){
		return true;
	}
	template< class Fn > Fn* get_parent(){
		BaseClass* B=GetParent();
		if(B){
			return dynamic_cast<Fn*>(B);
		}
		return NULL;
	}
	virtual bool CheckDirectCasting(){
		return true;
	}
	AUTONEW(BaseFunction);
};
class SubSection: public BaseClass{
	byte State;
public:
	SubSection(){
		State=1;
	}
	virtual int GetSectionStatus(){
		return State;
	}
	virtual void SetSectionStatus(int s){
		State=s;
	}
	const char* GetClassName(){
		return "SubSection";
	}
	virtual bool CheckDirectCasting(){
		return true;
	}
	virtual bool ForceProperty(){
		return true;
	}
	AUTONEW(SubSection);
};
#ifdef IMPLEMENT_CLASS_FACTORY

Enumerators ENUM;

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
//~~~~~~~~~~~OneClassStorage~~~~~~~~~~~/
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
OneClassStorage::~OneClassStorage()
{
	if(OneMemb)delete OneMemb;
	OneMemb=NULL;
	for(int i=0;i<Children.GetAmount();i++)if(Children[i])delete[](Children[i]);
	if(ClassTopic)delete[](ClassTopic);
	ClassTopic=NULL;
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
//~~~~~~~~~~~BaseClass~~~~~~~~~~~/
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
void BaseClass::SetParent(BaseClass* Parent){
	//if(CheckDirectCasting()){
		ParentBC=Parent;
		OneClassStorage* OCS=GetClassStorage();
		for(int i=0;i<OCS->Members.GetAmount();i++){
			OneClassMemberStorage* OCMS=OCS->Members[i];
			if(OCMS&&OCMS->Member&&OCMS->Member->CheckDirectCasting()){
				((BaseClass*)(OCMS->GetElmPtr(this)))->SetParent(this);
			}
		}
	//}
}
const char* BaseClass::GetClassName(){
	return "BaseClass";
}
const char* BaseClass::GetStringField(const char* FieldName){
	static xmlQuote xml;
	xml.ClearAll();
	if(CheckDirectCasting()){
		BaseClass* BC;
		void* ptr;
		void* extra;
		if(GetElementByName(FieldName,&BC,&ptr,&extra,this)){
			BC->Save(xml,ptr,extra);            
		}
	}
	const char* s=xml.Get_string();
	return s?s:"";
}
int   BaseClass::GetIntField  (const char* FieldName,int DefaultVal){
	int v=DefaultVal;	
	sscanf(GetStringField(FieldName),"%d",&v);
	return v;
}
float BaseClass::GetFloatField(const char* FieldName,float DefaultVal){
	float v=DefaultVal;
	sscanf(GetStringField(FieldName),"%f",&v);
	return v;
}
BaseClass* BaseClass::GetRoot(){
	BaseClass* pp=this;
	BaseClass* p=pp->GetParent();
	if(!p)return NULL;
	while(pp){
		p=pp->GetParent();
		if(!p)return pp;
		pp=p;
	}
	return NULL;
}
BaseClass::BaseClass(){	
	ParentBC=NULL;
}
void BaseClass::GetElementMessageAndHintInEditor(DString& Message,DString& Hint,BaseClass* Parent,char* ElmID){
	Hint.Clear();
	Message.Clear();
	if(Parent->CheckDirectCasting()){
		OneClassStorage* OCS=Parent->GetClassStorage();
		int N=Parent->GetAmountOfElements();
		char ID[512];
		sprintf(ID,"@ID::%s::%s",Parent->GetClassName(),ElmID);
		extern char* GetTextByID(char*);
		char* T=GetTextByID(ID);
		bool smt=false;
		//Message
		if(T[0]!='@'){
            Message=T;
			smt=true;
		}else{
			sprintf(ID,"@ID::%s",ElmID);
			T=GetTextByID(ID);
			if(T[0]!='@'){
				Message=T;
				smt=true;
			}
		}
		//Hint
		sprintf(ID,"@HINT::%s::%s",Parent->GetClassName(),ElmID);
		T=GetTextByID(ID);                
		if(T[0]!='@'){
			Hint=T;
			smt=true;
		}else{
			sprintf(ID,"@HINT::%s",ElmID);
			T=GetTextByID(ID);
			if(T[0]!='@'){
				Hint=T;
				smt=true;
			}
		}
		if(!smt){
			bool GetHintDebugMode();
			if(GetHintDebugMode()){
				sprintf(ID,"Message:\\@ID::%s::%s\\@ID::%s\\Hint:\\@HINT::%s::%s\\@HINT::%s\\",Parent->GetClassName(),ElmID,ElmID,Parent->GetClassName(),ElmID,ElmID);
				Hint=ID;
			}
		}
	}
}
void BaseClass::reset_class(void* DataPtr){
	BaseClass* BC=(BaseClass*)DataPtr;
	if(CheckDirectCasting()){
		for(int i=0;i<BC->GetAmountOfElements();i++){
			BC->GetElementClass(i)->reset_class(BC->GetElementPtr(i,DataPtr));
		}
	}
}
void BaseClass::RegisterOneMember(BaseClass* Member,void* ptr,const char* id,void* Extra,const char* Host,bool DirectCast,DWORD Mask){
	OneClassStorage* CST=GetClassStorage();
	if(!CST){
		CST=CGARB.CreateClassStorage(GetClassName());
	}
	OneClassMemberStorage* MS=CST->CreateMember(id);
	MS->Member=Member;
	MS->UseReference=0;
	if(StaticMode){
		MS->OffsetFromClassRoot= (int)ptr;
	}else{
		MS->OffsetFromClassRoot= (BYTE*)ptr - (BYTE*)this;
	}
	MS->ExtraData=Extra;
	MS->IntPropertyR     =NULL;
	MS->GeneralPropertyR =NULL;
	MS->IntPropertyW     =NULL;
	MS->GeneralPropertyW =NULL;
	MS->HostClass=Host;
	MS->DirectCasting=DirectCast;
	MS->Mask=Mask;
	MS->ReadOnly=ReadOnlyMode;
	MS->Invisible=InvisibleMode;
	MS->StaticMode=StaticMode;
	MS->NoSaveMode=NoSaveMode;

	MS->CheckValidRegistration(this,id);

	ReadOnlyMode=false;
	InvisibleMode=false;
	StaticMode=false;
	NoSaveMode=false;
}
void BaseClass::RegisterOneMemberIntProperty(BaseClass* Member,tpIntPropertyReader* IntPropR,tpIntPropertyWriter* IntPropW,const char* id,void* Extra,const char* Host,DWORD Mask){
	OneClassStorage* CST=GetClassStorage();
	if(!CST){
		CST=CGARB.CreateClassStorage(GetClassName());
	}
	OneClassMemberStorage* MS=CST->CreateMember(id);
	MS->Member=Member;
	MS->UseReference=0;
	MS->OffsetFromClassRoot=0;
	MS->ExtraData=Extra;
	MS->IntPropertyR     =IntPropR;
	MS->GeneralPropertyR =NULL;
	MS->IntPropertyW     =IntPropW;
	MS->GeneralPropertyW =NULL;
	MS->HostClass=Host;
	MS->DirectCasting=false;
	MS->Mask=Mask;

	if(StaticMode)PrintError("STATIC Not allowed for properties: <%s %s>",Member->GetClassName(),id);

	MS->ReadOnly=ReadOnlyMode;
	MS->Invisible=InvisibleMode;
	MS->StaticMode=false;
	MS->NoSaveMode=NoSaveMode;
	
	ReadOnlyMode=false;
	InvisibleMode=false;
	StaticMode=false;
	NoSaveMode=false;
}
void BaseClass::RegisterOneMemberProperty(BaseClass* Member,tpPropertyReader* PropR,tpPropertyWriter* PropW,const char* id,void* Extra,const char* Host,DWORD Mask){
	OneClassStorage* CST=GetClassStorage();
	if(!CST){
		CST=CGARB.CreateClassStorage(GetClassName());
	}
	OneClassMemberStorage* MS=CST->CreateMember(id);
	MS->Member=Member;
	MS->UseReference=0;
	MS->OffsetFromClassRoot=0;
	MS->ExtraData=Extra;
	MS->IntPropertyR     =NULL;
	MS->GeneralPropertyR =PropR;
	MS->IntPropertyW     =NULL;
	MS->GeneralPropertyW =PropW;
	MS->HostClass=Host;
	MS->DirectCasting=false;
	MS->Mask=Mask;

	if(StaticMode)PrintError("STATIC Not allowed for properties: <%s %s>",Member->GetClassName(),id);

	MS->ReadOnly=ReadOnlyMode;
	MS->Invisible=InvisibleMode;
	MS->StaticMode=false;
	MS->NoSaveMode=NoSaveMode;

	ReadOnlyMode=false;
	InvisibleMode=false;
	StaticMode=false;
	NoSaveMode=false;
}
bool BaseClass::GetElementByName(const char* ElmName,BaseClass** ClassPtr,void** ElmPtr,void** ExtraPtr,void* base){
	int idx=ExpandWith(ElmName,base);
	if(idx!=-1){
		*ClassPtr=GetElementClass(idx);
		*ElmPtr=GetElementPtr(idx,base);
		return true;
	}
	int N=GetAmountOfElements();
	for(int i=0;i<N;i++){
		if(!strcmp(ElmName,GetElementID(i))){
			*ClassPtr=GetElementClass(i);
			*ElmPtr=GetElementPtr(i,base);
			*ExtraPtr=GetElementExtraPtr(i);
			return true;
		}
	}
	return false;
}
OneClassMemberStorage* BaseClass::GetElementStorage(const char* ElmName){
	int N=GetAmountOfElements();
	for(int i=0;i<N;i++){
		if(!strcmp(ElmName,GetElementID(i))){
			OneClassStorage* OCS=GetClassStorage();
			if(!OCS)PrintError("class not registered: \"%s\"",GetClassName());
			else{ 
				int idx=i;
				if(CheckDirectCasting()){
					idx=GetRealMaskedPosition(i);
				}
				if(idx<OCS->Members.GetAmount())return OCS->Members[idx];
				else return NULL;
			}
		}
	}
	return NULL;
}
const char* BaseClass::GetElementHostClass(const char* ElmName){
	int N=GetAmountOfElements();
	for(int i=0;i<N;i++){
		if(!strcmp(ElmName,GetElementID(i))){
			OneClassStorage* OCS=GetClassStorage();
			if(!OCS)PrintError("class not registered: \"%s\"",GetClassName());
			else return OCS->Members[i]->HostClass;
		}
	}
	return NULL;
}
const char* BaseClass::GetElementHostClass(int Index){
	OneClassStorage* OCS=GetClassStorage();
	assert(Index>=0&&Index<OCS->Members.GetAmount());
	if(!OCS)PrintError("class not registered: \"%s\"",GetClassName());
	else return OCS->Members[Index]->HostClass;
	return NULL;
}
BaseClass::~BaseClass(){	
	if(CheckDirectCasting())reset_class(this);
};
void BaseClass::RegisterMembers(){
}
int BaseClass::GetAmountOfElements(){
	RegisterMembers();
	OneClassStorage* CST=GetClassStorage();
	if(CST){
		DWORD M=GetClassMask();
		if(M==0xFFFFFFFF){
			return CST->Members.GetAmount();
		}
		int N=CST->Members.GetAmount();
		int NR=0;
		for(int i=0;i<N;i++){
            if(CST->Members[i]->Mask&M)NR++;
		}
		return NR;
	}else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return 0;
	}
}
bool BaseClass::ForceProperty(){
	if(CheckDirectCasting()){
		BaseClass* P=GetParent();
		if(!P)return false;
		bool f=P->ForceProperty();
		return f;
	}
	return false;
}	
int BaseClass::GetRealMaskedPosition(int Index){
	DWORD M=GetClassMask();
	if(M==0xFFFFFFFF)return Index;
	OneClassStorage* CST=GetClassStorage();
	assert(Index>=0&&Index<CST->Members.GetAmount());
	if(CST){
		int N=CST->Members.GetAmount();
		for(int i=0;i<N;i++){
			if(CST->Members[i]->Mask&M){
				if(!Index)return i;
				Index--;
			}
		}
		assert(Index==0);
		return 0;
	}else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return 0;
	}
}
const char* BaseClass::GetElementID(int Index){
	OneClassStorage* CST=GetClassStorage();
	Index=GetRealMaskedPosition(Index);
	assert(Index>=0&&Index<CST->Members.GetAmount());
	if(CST){
		return CST->Members[Index]->xmlID;
	}else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return 0;
	}
}
const char* BaseClass::GetElementView(int Index,const char* LocalName){
	//Index=GetRealMaskedPosition(Index);
	if(CheckDirectCasting()&&Index<GetAmountOfElements()){
		BaseClass* BC=GetElementClass(Index);
        void* eptr=GetElementPtr(Index,this);
		if(BC->CheckDirectCasting()){
			BC=(BaseClass*)eptr;
			const char* s=BC->GetThisElementView(LocalName);
			if(s)return s;
		}
	}
	return GetElementID(Index);
}
BaseClass* BaseClass::GetElementClass(int Index){
	Index=GetRealMaskedPosition(Index);
	OneClassStorage* CST=GetClassStorage();
	assert(Index>=0&&Index<CST->Members.GetAmount());
	if(CST)return CST->Members[Index]->Member;
	else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return 0;
	}
}
OneClassMemberStorage* BaseClass::GetElementCMS(int Index){
	Index=GetRealMaskedPosition(Index);	
	OneClassStorage* CST=GetClassStorage();
	if(!CST->Members.GetAmount())return NULL;
	assert(Index>=0&&Index<CST->Members.GetAmount());
	if(CST)return CST->Members[Index];
	else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return NULL;
	}
}
void* BaseClass::GetElementPtr(int Index,void* base){
	Index=GetRealMaskedPosition(Index);
	OneClassStorage* CST=GetClassStorage();
	assert(Index>=0&&Index<CST->Members.GetAmount());
	if(CST){
		if(CST->Members[Index]->IntPropertyR){
            static int V=0;
			V=CST->Members[Index]->IntPropertyR((BaseClass*)base);
			return &V;
		}else
		if(CST->Members[Index]->GeneralPropertyR){
            return CST->Members[Index]->GeneralPropertyR(base);
		}else
		return CST->Members[Index]->GetElmPtr(base);
	}else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return NULL;
	}
}

bool BaseClass::LoadMember(xmlQuote& xml,void* ClassPtr,int MemIndex,ErrorPager* Error,void* Extra){
	RegisterMembers();
	MemIndex=GetRealMaskedPosition(MemIndex);
	assert(MemIndex>=0&&MemIndex<GetAmountOfElements());
	OneClassStorage* OCS=GetClassStorage();
	OneClassMemberStorage* OCMS=OCS->Members[MemIndex];
	BaseClass* BASE=OCMS->Member;
	void* BasePtr=GetElementPtr(MemIndex,ClassPtr);
	void* Extra2=OCMS->ExtraData;
	if(!Extra2)Extra2=Extra;
	BASE->Load(xml,BasePtr,Error,Extra2);

	if(OCMS->IntPropertyW)
	{
		int v=*((int*)BasePtr);
		OCMS->IntPropertyW((BaseClass*)ClassPtr,v);
	}
	else
	{
		if(OCMS->GeneralPropertyW)
		{
			OCMS->GeneralPropertyW(ClassPtr,BasePtr);
		}
	}
	return true;
}

//  added by Silver, 21.08.2003
//  Returns true when given object is of type derived from parent class
//		with the given name
bool BaseClass::HasParentClass(const char* ParentClassName){
	RegisterMembers();
	for(int i=0;i<GetAmountOfElements();i++){
		if (!strcmp(ParentClassName,GetElementHostClass(i))) return true;	
	}
	return false;
} // BaseClass::HasParentClass

bool BaseClass::LoadMemberDirect(void* ElementPtr,void* ClassPtr,int MemIndex)
{
	MemIndex=GetRealMaskedPosition(MemIndex);
	RegisterMembers();
	assert(MemIndex>=0&&MemIndex<GetAmountOfElements());
	OneClassStorage* OCS=GetClassStorage();
	OneClassMemberStorage* OCMS=OCS->Members[MemIndex];
	if(OCMS->IntPropertyW)
	{
		int v=*((int*)ElementPtr);
		OCMS->IntPropertyW(this,v);
	}else
	{
		if(OCMS->GeneralPropertyW) OCMS->GeneralPropertyW(this,ElementPtr);
	}
	return true;
}

void* BaseClass::GetElementExtraPtr(int Index){
	Index=GetRealMaskedPosition(Index);
	OneClassStorage* CST=GetClassStorage();
	if(CST){
		if(Index>=0&&Index<CST->Members.GetAmount()){
		    return CST->Members[Index]->ExtraData;
		}else return NULL;
	}else{
		PrintError("Undefined class found: \"%s\"\n",GetClassName());
		return 0;
	}
}
void BaseClass::DeleteDefaultSubFields(xmlQuote& xml,void* ClassPtr,void* Extra){
	if(CheckDirectCasting()&&!Extra){
		BaseClass* BC=(BaseClass*)ClassPtr;
		int N=BC->GetAmountOfElements();
		if(!N)return;
		bool s=BaseClass::SaveInShortForm;
		BaseClass::SaveInShortForm=false;
		BaseClass* temp=new_element();		
		xmlQuote XML;
		temp->Save(XML,temp,Extra);
		BaseClass::SaveInShortForm=s;
		for(int i=0;i<xml.GetNSubQuotes();i++){
			xmlQuote* sub=xml.GetSubQuote(i);
			xmlQuote* SUB1=XML.GetSubQuote( (char*)sub->GetQuoteName() );
			bool del=false;
			if(SUB1){
				if(!sub->GetNSubQuotes()){
					char* S=(char*)SUB1->Get_string();
					char* s=(char*)sub->Get_string();
					if( (s==NULL&&S==NULL) || (s&&S&&!strcmp(s,S)) ){
						del=true;
					}
				}
			}
			if(del){
				xml.DelSubQuote(i);
				i--;
			}
		}
		delete(temp);
	}
}
void BaseClass::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	RegisterMembers();
	BaseClass* BC=(BaseClass*)ClassPtr;
	int N=BC->GetAmountOfElements();
	for(int i=0;i<N;i++){
		OneClassMemberStorage* OCMS=BC->GetElementCMS(i);
		if(OCMS==NULL||!OCMS->NoSaveMode){
			xmlQuote* xi=new xmlQuote((char*)BC->GetElementID(i));
			void* mptr=BC->GetElementPtr(i,ClassPtr);
			void* eptr=BC->GetElementExtraPtr(i);
			if(!eptr)eptr=Extra;
			if(mptr){
				BaseClass* BC1=BC->GetElementClass(i);
				BC1->Save(*xi,mptr,eptr);
			}else{
				xi->Assign_string("NULL");
			}
			xml.AddSubQuote(xi);
		}
	}
	if(SaveInShortForm){
		DeleteDefaultSubFields(xml,ClassPtr,Extra);
	}
}
void TestCGARB();
bool BaseClass::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	//TestCGARB();
	RegisterMembers();
	BaseClass* BC=(BaseClass*)ClassPtr;
	int N=xml.GetNSubQuotes();
	OneClassStorage* OCS=BC->GetClassStorage();
	bool WasLink=false;	
	for(int i=0;i<N;i++){
		//TestCGARB();
		xmlQuote* sub=xml.GetSubQuote(i);
		BaseClass* BASE;
		void* BasePtr;
		void* Extra2=NULL;
		//TestCGARB();
		if(BC->GetElementByName(sub->GetQuoteName(),&BASE,&BasePtr,&Extra2,ClassPtr)){
			//TestCGARB();
			if(!Extra2)Extra2=Extra;
			//TestCGARB();
			if(CheckDirectCasting()&&BASE->CheckDirectCasting())((BaseClass*)BasePtr)->SetParent((BaseClass*)ClassPtr);
			//TestCGARB();
			BASE->Load(*sub,BasePtr,Error,Extra2);			
			//TestCGARB();
			OneClassMemberStorage* OCMS=GetElementStorage(sub->GetQuoteName());
			if(OCMS){
				if(OCMS->IntPropertyW){
					int v=*((int*)BasePtr);
					OCMS->IntPropertyW((BaseClass*)ClassPtr,v);
				}else
				if(OCMS->GeneralPropertyW){
					OCMS->GeneralPropertyW(ClassPtr,BasePtr);
				}
			}
			_str* s=BC->GetObjectNamePointer();
			if(s&&s->str&&s->str[0]){
				if(!WasLink)CPGARB.TryToLinkClass(this);
				WasLink=true;

			}
			//TestCGARB();
		}else{
			Error->xml_print(xml,"Unknown identifier %s of class %s.\n",sub->GetQuoteName(),BC->GetClassName());            
		}
		//TestCGARB();
	}	
	_str* s=BC->GetObjectNamePointer();
	if(s&&s->str&&s->str[0]){
		if(!WasLink)CPGARB.TryToLinkClass(this);
	}	
	return true;
}
bool BaseClass::ReadFromFile(const char* Name){
	if(!CheckDirectCasting())return false;
	xmlQuote xml;
	if(xml.ReadFromFile((char*)Name)){
		ErrorPager err;
		CurrentSaveFile=Name;
        Load(xml,this,&err,NULL);
		CurrentSaveFile=NULL;
		return true;
	}
	return false;
}
bool BaseClass::SafeReadFromFile(const char* Name){
	if(!CheckDirectCasting())return false;
	xmlQuote xml;
	if(xml.ReadFromFile((char*)Name)){
		ErrorPager err(0);
		CurrentSaveFile=Name;
		Load(xml,this,&err,NULL);
		CurrentSaveFile=NULL;
		return true;
	}
	return false;
}
bool BaseClass::WriteToFile(const char* Name){
	if(!CheckDirectCasting())return false;
	CurrentSaveFile=Name;
	xmlQuote xml;
	SaveInShortForm=ShouldSaveInShortForm();
	Save(xml,this);
	SaveInShortForm=false;
	xml.WriteToFile((char*)Name);
	CurrentSaveFile=NULL;
	return true;
}
bool BaseClass::Copy(void* SrcData,void* SrcDataExtra,BaseClass* Dest,void* DestData,void* DestExtraPtr){
	xmlQuote xml;
	Save(xml,SrcData,SrcDataExtra);
	ErrorPager EP(1);
    return Dest->Load(xml,DestData,&EP,DestExtraPtr);	
}
bool BaseClass::Copy(BaseClass* Dest, bool Add){
	if(!CheckDirectCasting())return false;
	if(strcmp(GetClassName(),Dest->GetClassName()))return false;
	if(!Add) Dest->reset_class(Dest);
    return Copy(this,NULL,Dest,Dest,NULL);	
}
//- special function for integrating in editor -
//Options: 1-enum leafs 2-enum nodes 4-enum adds, so 7-enum everything
int BaseClass::GetIndexOfEditableElement(int Index,void* DataPtr,DWORD Options){
	int NR=0;
	int N=GetAmountOfElements();
	if((Options&3)!=3){
		for(int i=0;i<N;i++){
			BaseClass* BC=GetElementClass(i);
			void* Data=GetElementPtr(i,DataPtr);
			bool Node=CheckIfElementIsNode(i,DataPtr);
			if(BC->CheckDirectCasting()){
				BC=(BaseClass*)Data;
				if(BC->ForceProperty())Node=false;
			}
			if(CheckDirectCasting()){
                BaseClass* B0=(BaseClass*)DataPtr;
				if(B0&&B0->ForceProperty())Node=false;
			}
			if((Node&&(Options&2))||((!Node)&&(Options&1))){
				if(NR==Index)return i;
				NR++;
			}
		}
	}else{
		if(Index<N)return Index;
		else return -1;
	}
	return -1;
}
int BaseClass::GetAmountOfEditableElements(void* DataPtr,DWORD Options){
	int NR=0;
	BaseClass* BCPTR=CheckDirectCasting()?((BaseClass*)DataPtr):this;
	int N=BCPTR->GetAmountOfElements();
	if(CheckDirectCasting()){
		BaseClass* B0=(BaseClass*)DataPtr;
		if(B0&&B0->ForceProperty()&&Options&1)Options|=4;
	}
	if((Options&3)!=3){
		for(int i=0;i<N;i++){
			BaseClass* BC=BCPTR->GetElementClass(i);
			void* Data=BCPTR->GetElementPtr(i,DataPtr);
			bool Node=BCPTR->CheckIfElementIsNode(i,DataPtr);
			if(BC->CheckDirectCasting()){
				BC=(BaseClass*)Data;
				if(BC->ForceProperty())Node=false;
			}
			if(CheckDirectCasting()){
				BaseClass* B0=(BaseClass*)DataPtr;
				if(B0&&B0->ForceProperty())Node=false;
			}
			if((Node&&(Options&2))||((!Node)&&(Options&1)))NR++;
		}
	}else NR=N;
	int exr=BCPTR->GetExpansionRules();
	if((exr&8)&&NR)return NR;
	return NR+int(exr!=0&&(Options&4)!=0);	
}
BaseClass* BaseClass::GetEditableElementClass(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return NULL;
	return GetElementClass(P);	
}
void* BaseClass::GetEditableElementData(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return NULL;
	return GetElementPtr(P,DataPtr);		
}
bool BaseClass::CheckIfElementReadOnly(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return false;
	Index=GetRealMaskedPosition(P);
	OneClassStorage* CST=GetClassStorage();	
	if(CST&&Index>=0&&Index<CST->Members.GetAmount()){		
		return CST->Members[Index]->ReadOnly;		
	}
	return false;
}
bool BaseClass::CheckIfElementInvisible(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return false;
	Index=GetRealMaskedPosition(P);
	OneClassStorage* CST=GetClassStorage();	
	if(CST&&Index>=0&&Index<CST->Members.GetAmount()){		
		return CST->Members[Index]->Invisible;		
	}
	return false;
}
void* BaseClass::GetEditableElementExtra(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return NULL;
	return GetElementExtraPtr(P);		
}
bool BaseClass::GetEditableElementExpansionList(ClassExpParams& EXP,int Index,void* DataPtr,DWORD Option,BaseClass* Parent,const char* ElmName){	
	int exr=GetExpansionRules();
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if((exr&8)||(P==-1&&exr&3)){
		const char* bclass=GetExpansionBaseClass();
		if(bclass){
			OneClassStorage* OCS=CGARB.GetClass(bclass);
			if(OCS){
				if(exr&1){
					bool ADD=1;
					if(Parent&&ElmName){
						ADD=Parent->AskParentForUsingExpansionClass((char*)ElmName,(char*)bclass);
					}
					if(ADD){
						EXP.ExpList.Add((char*)bclass);
						OneClassStorage* OCS1=CGARB.GetClass((char*)bclass);
						int tidx=-1;
						if(OCS1){
							if(OCS1->ClassTopic){
								tidx=EXP.TopicsList.GetAmount();
								EXP.TopicsList.Add(OCS1->ClassTopic);
							}
						}
						EXP.TopicsIdxs.Add(tidx);
					}
				}
				if(exr&2){
					int N=OCS->Children.GetAmount();
					for(int j=0;j<N;j++){
						char* CLASS=OCS->Children[j];
						if(strcmp(CLASS,bclass)){
							bool ADD=1;
							if(Parent&&ElmName){
								ADD=Parent->AskParentForUsingExpansionClass((char*)ElmName,(char*)CLASS);
							}
							if(ADD){
								EXP.ExpList.Add(CLASS);
								OneClassStorage* OCS1=CGARB.GetClass(CLASS);
								int tidx=-1;
								if(OCS1){
									if(OCS1->ClassTopic){
										for(int q=0;q<EXP.TopicsList.GetAmount();q++)if(!strcmp(EXP.TopicsList[q],OCS1->ClassTopic))tidx=q;
										if(tidx==-1){
											tidx=EXP.TopicsList.GetAmount();
											EXP.TopicsList.Add(OCS1->ClassTopic);
										}
									}
								}
								EXP.TopicsIdxs.Add(tidx);
							}
						}
					}
				}
				if(EXP.TopicsList.GetAmount()<=1){
					EXP.TopicsList.Clear();
				}
				if(EXP.ExpList.GetAmount())return true;
			}
		}
	}
	return false;
}
bool BaseClass::CheckIfElementIsNode(int Index,void* DataPtr){
	if(Index>=GetAmountOfElements())return false;
	BaseClass* BC=GetElementClass(Index);
	void* ptr=GetElementPtr(Index,DataPtr);
	if(BC&&ptr){
        if(BC->GetAmountOfEditableElements(DataPtr,7))return true;
	}
	return false;
}
bool BaseClass::CheckIfEditableElementIsNode(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return false;
	return CheckIfElementIsNode(P,DataPtr);
}
const char* BaseClass::GetEditableElementView(int Index,void* DataPtr,const char* LocalName,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return "add...";
	return GetElementView(P,LocalName);
}
const char* BaseClass::GetEditableElementName(int Index,void* DataPtr,DWORD Option){
	int P=GetIndexOfEditableElement(Index,DataPtr,Option);
	if(P==-1)return "add...";
	return GetElementID(P);
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
//~~~~~~~~~~~_int~~~~~~~~~~~~/
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
void _int::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	int* V=(int*)ClassPtr;
	xml.Assign_int(*V);
}
bool _int::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	int* V=(int*)ClassPtr;
	const char* c=xml.Get_string();
	*V=0;
	int z=0;
	if(c)z=sscanf(c,"%d",V);
	if(z!=1){
		Error->xml_print(xml,"unable to read DWORD from XML: %s\n",xml.Get_string());
		return false;
	}
	return true;
};
const char* _int::GetClassName(){
	return "_int";
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
//~~~~~~~~~~~_DWORD~~~~~~~~~~~~/
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
void _DWORD::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	DWORD* V=(DWORD*)ClassPtr;
	char c[16];
	sprintf(c,"%08X",*V);
	xml.Assign_string(c);
}
bool _DWORD::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
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
const char* _DWORD::GetClassName(){
	return "_DWORD";
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
//~~~~~~~~~~~_float~~~~~~~~~~~~/
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
void _float::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	float* V=(float*)ClassPtr;
	char c[256];
	sprintf(c,"%f",*V);
	xml.Assign_string(c);
}
bool _float::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	float* V=(float*)ClassPtr;
	const char* c=xml.Get_string();
	*V=0;
	int z=0;
	if(c)z=sscanf(c,"%f",V);
	if(z!=1){
		Error->xml_print(xml,"unable to read float from XML: %s\n",xml.Get_string());
		return false;
	}
	return true;
};
const char* _float::GetClassName(){
	return "_float";
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
//~~~~~~~~~~~_bool~~~~~~~~~~~~/
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~/
void _bool::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	bool* V=(bool*)ClassPtr;
	char c[16];
	if (*V) strcpy( c, "true" ); else strcpy( c, "false" );
	xml.Assign_string(c);
}

bool _bool::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra)
{
	bool* V=(bool*)ClassPtr;
	const char* c=xml.Get_string();
	*V=false;
	while (*c == ' ') c++;
	if (!_strnicmp( c, "true", 4 )) *V = true;
	return true;
};
const char* _bool::GetClassName(){
	return "_bool";
}
//_str
_str::~_str(){
	DString::~DString();
}
void _str::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	_str* s=(_str*)ClassPtr;
	if(s->str)xml.Assign_string(s->str);
}
bool _str::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	_str* s=(_str*)ClassPtr;
	s->Clear();
	s->Add((char*)xml.Get_string());
	return true;
}
const char* _str::GetClassName(){
	return "_str";
}
//end _str

void OneClassMemberStorage::CheckValidRegistration(void* Base,const char* MemName){
	if(StaticMode){
		if(OffsetFromClassRoot<1000000){
			PrintError("CRITICAL: Non-static member <%s %s> registered like static",Member->GetClassName(),MemName);
		}
	}else{
		if(abs(OffsetFromClassRoot)>=1000000){
			PrintError("CRITICAL: Static member <%s %s> registered like non-static",Member->GetClassName(),MemName);
		}
	}
}
OneClassMemberStorage::OneClassMemberStorage(){
	Member=NULL;
	xmlID=NULL;
	UseReference=0;
}
OneClassMemberStorage::~OneClassMemberStorage(){
	if(Member)delete(Member);
	if(xmlID)free(xmlID);
	Member=NULL;
	xmlID=NULL;
}
char* GetGlobalBuffer(){
	static char cc[1024];
	return cc;
}
DIALOGS_API ClassGarbage CGARB;
DIALOGS_API ClassPointersGarbage CPGARB;
const char* BaseClass::CurrentSaveFile=NULL;
bool BaseClass::ReadOnlyMode=false;
bool BaseClass::InvisibleMode=false;
bool BaseClass::SaveInShortForm=false;
bool BaseClass::NoSaveMode=false;
bool BaseClass::StaticMode=false;

#include <xmlQuote.hpp>

#ifdef __STDAPPLICATION__

char* GetTextByID(char* x){
	return x;
}
bool GetHintDebugMode(){
	return false;
}
void PushSmartLeak(bool& v){
}
void PopSmartLeak(bool& v){
}
#endif //__STDAPPLICATION__

#endif //IMPLEMENT_CLASS_FACTORY

#pragma pack(pop)
#endif __CLASSENGINE_H__