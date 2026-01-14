#pragma once
//#define sprNx  (64<<ADDSH)
#include "MapTemplates.h"
#include <gmDefines.h>
#include <mMath2D.h>
#include <mMath3D.h>
#pragma pack(1)
#define SprShf (6+ADDSH)
#define SprInCell 16
extern int MaxSprt;
class ObjCharacter;
struct BornRef;
class ObjCharRef:public ClassRef<ObjCharacter>{
public:
    SAVE(ObjCharRef);
	REG_PARENT(ClassRef<ObjCharacter>);
	ENDSAVE;
	const char* GetThisElementView(const char*);
};
class BornRef:public BaseClass{
public:
	ObjCharRef ObjectToBorn;
	//int BornID;
	int BDx;
	int BDy;
	SAVE(BornRef);
	REG_AUTO(ObjectToBorn);
	REG_MEMBER(_int,BDx);
	REG_MEMBER(_int,BDy);
	ENDSAVE;
};
class OneConnector:public BaseClass{
public:
	int x,y;
	int ConnType;
	SAVE(OneConnector);
	REG_MEMBER(_int,x);
	REG_MEMBER(_int,y);
	REG_MEMBER(_int,ConnType);
	ENDSAVE;
};
struct AlignInfo{
	float xp,yp;//coordinates on picture
	float wx,wy,wz;//coordinates in world space relative to center of the sprite
};
class LockInfoPoint:public BaseClass{
public:
	int x,y;
	SAVE(LockInfoPoint);
    REG_MEMBER(_int,x);
	REG_MEMBER(_int,y);
	ENDSAVE;
	const char* GetThisElementView(const char*){
		static char cc[32];
		sprintf(cc,"(%d,%d)",x,y);
		return cc;
	}
};

class ObjCharacter:public ReferableBaseClass{
public:
	byte ViewType;
	byte RenderType;
	short FileID;
	int SpriteID;
	short FileID_forBackground;
	int SpriteID_forBackground;
	short FileID_forPreview;
	int SpriteID_forPreview;
	int Radius;
	int CenterX;
	int CenterY;

	int ParentIndex;

	byte ResType;//0-wood,1-gold,2-stone,3-food,0xFE-no resource,0xFF-removable
	int WorkRadius;
	int ResPerWork;
	int Amplitude;
	int WorkAmount;
	ClassRef<ObjCharacter> NextWorkObj;
	word WNextObj;
	word DamageAmount;
	word DNextObj;
    int TimeAmount;
	ClassRef<ObjCharacter> NextTimeObj;
    word TNextObj;
	byte IntResType;
	int IntResPerWork;
	int IntWorkRadius;
    NewAnimation* Stand;
    NewAnimation* Shadow;
	int SoundID;
	int SoundProb;
    short Z0;
    short DZ;
    byte Parts;
    byte Delay;
    byte Frames;
	byte Discret;
	byte LockRadius;
	word OnGround;
	//word NLockPt;
	word NRandom;
	word SpriteIndex;
	short GroupIndex;
	short IndexInGroup;
	//short* LockX;
	//short* LockY;
	ClonesArray<LockInfoPoint> LockInfo;
	ClonesArray<BornRef> BREF;
	//int NBorn;
	//char* Name;
	int ShieldRadius;
	int  ShieldProbability;
	short FixDx;
	short FixDy;
	byte FixDir;
	byte FixR;
	//connectors
	ClonesArray<OneConnector> CONN;
	int NOutConn;
	byte EquGroup;
	//aligning info
	byte Aligning;
	bool HaveAligning;
	//for v-aligning:
    int va_x1;
	int va_y1;
	int va_x2;
	int va_y2;
	AlignInfo P1;//3 points determine plane in space
	AlignInfo P2;
	AlignInfo P3;
	bool UseTexture;
	float uL,vL,uR,vR,uC,vC;
	//IMediaManager
	int ModelManagerID;
	int MShiftX;
	int MShiftY;
	bool EditableModel;

	static float Scale;
	static float RotX;
	static float RotY;
	static float RotFI;
	static float ModelDZ;
	static void SetModelDefaults();


	void GetMatrix4D(Matrix4D& M4,int x,int y,int z) const;
	int FixHeight;
	bool RoadCenter;	
	//auto deep params
	int AutoDeep;
	int AutoDeepR;
	int AutoDeepR1;
	ObjCharacterExtension Ext;
	SAVE(ObjCharacter);
		SAVE_SECTION(0xFFFFFFFF);
		REG_AUTO(Name);
		REG_ENUM(_byte_index,ViewType,SpriteViewType);

		SAVE_SECTION(1);

		REG_ENUM(_byte_index,RenderType,SprRenderType);
		REG_MEMBER(_gpfile,FileID);
		REG_MEMBER(_int,SpriteID);

		SAVE_SECTION(0xFFFFFFFF);

		REG_MEMBER(_gpfile,FileID_forBackground);
		REG_MEMBER(_int,SpriteID_forBackground);
		REG_MEMBER(_gpfile,FileID_forPreview);
		REG_MEMBER(_int,SpriteID_forPreview);

		SAVE_SECTION(1);

		REG_MEMBER(_int,CenterX);
		REG_MEMBER(_int,CenterY);

        SAVE_SECTION(2);

		REG_MEMBER(_float,uL);
		REG_MEMBER(_float,vL);
		REG_MEMBER(_float,uR);
		REG_MEMBER(_float,vR);
		REG_MEMBER(_float,uC);
		REG_MEMBER(_float,vC);

		SAVE_SECTION(16);

		REG_MEMBER(_ModelID,ModelManagerID);
		REG_MEMBER(_int,MShiftX);
		REG_MEMBER(_int,MShiftY);
		REG_MEMBER(_bool,EditableModel);

        SAVE_SECTION(0xFFFFFFFF);

		REG_MEMBER(_int,Radius);		
		REG_ENUM(_byte_index,ResType,ResType);
		REG_MEMBER(_int,WorkRadius);
		REG_MEMBER(_int,Amplitude);
		REG_MEMBER(_int,WorkAmount);
		REG_AUTO(NextWorkObj);
		REG_MEMBER(_int,TimeAmount);
		REG_AUTO(NextTimeObj);
		REG_ENUM(_byte_index,IntResType,ResType);
		REG_MEMBER(_int,IntResPerWork);
		REG_MEMBER(_int,IntWorkRadius);
		REG_AUTO(CONN);
		REG_AUTO(LockInfo);
		REG_AUTO(BREF);

		REG_CLASS(LockInfoPoint);
		REG_CLASS(BornRef);
		REG_CLASS(OneConnector);

		REG_MEMBER(_int,ShieldRadius);
		REG_MEMBER(_int,ShieldProbability);
		REG_MEMBER(_int,AutoDeep);
		REG_MEMBER(_int,AutoDeepR);
		REG_MEMBER(_int,AutoDeepR1);
		REG_MEMBER(_int,FixHeight);

		SAVE_SECTION(4);

		REG_ENUM(_byte_index,Aligning,SpriteAlignType);

		SAVE_SECTION(8);

		REG_MEMBER(_int,va_x1);
		REG_MEMBER(_int,va_y1);
		REG_MEMBER(_int,va_x2);
		REG_MEMBER(_int,va_y2);

	ENDSAVE;
	DWORD GetClassMask(){
		if(ViewType==0){//Sprite
            DWORD M=1+4;
			if(Aligning==2)M|=8;
			return M;
		}else
		if(ViewType==1){//3D-animation
            return 16;
		}else
		if(ViewType==2){//field path
			return 0x80000000;
		}else
		if(ViewType==3){//part of texture
			return 2;
		}else return 0xFFFFFFFF;
	}
	const char* GetThisElementView(const char* LocalName){
		static char cc[128];
		if(!Name.str)return LocalName;
		sprintf(cc,"%s : %s",LocalName,Name.str);
		return cc;
	}
};
class OneSprSection:public BaseClass{
public:
	_str SectionName;
    ClonesArray<ObjCharRef> ObjectsList;
	SAVE(OneSprSection);
	REG_CLASS(ObjCharRef);
	REG_AUTO(SectionName);
	REG_AUTO(ObjectsList);
	ENDSAVE;
};
class ObjList:public ClonesArray<ObjCharacter>{
public:
	void Arrange(){
		for(int i=0;i<GetAmount();i++)(*this)[i]->ParentIndex=i;
	}
	virtual int ExpandWith(const char* ElmName,void* base){
		int N=ClassArray<ObjCharacter>::ExpandWith(ElmName,base);
		Arrange();
		return N;
	}	
	virtual bool MoveElement(int index,char opType){
		bool r=ClassArray<ObjCharacter>::MoveElement(index,opType);
		if(opType)Arrange();
		return r;
	}
	virtual bool DelElement(int index){		
		bool r=ClassArray<ObjCharacter>::DelElement(index);
		Arrange();
		return r;
	}
};
class SprGroup:public BaseClass{
public:
	ObjList Objects;
	ClonesArray<OneSprSection> Sections;

	SAVE(SprGroup);
		REG_AUTO(Objects);
		REG_AUTO(Sections);
	ENDSAVE;
	int GetNSprites(){
		return Objects.GetAmount();
	}	
	SprGroup();
	~SprGroup();
	void LoadSprites(char* fname);
	int GetIndexByName(char* Name);
};
class OneSprite{
public:
	bool Enabled:1;
	bool Surrounded:1;
	byte Locking;
	int x;
	int y;
	short z;
	word Radius;
	SprGroup* SG;
	ObjCharacter* OC;
	int Index;
	word SGIndex;
	byte WorkOver;
    word TimePassed;
	byte Damage;
	Matrix4D* M4;
#ifdef CONQUEST
	word NIndex;
	byte Direction;
	bool Reflected:1;
#endif
	int PerformWork(word);
    int PerformIntWork(int work);
	void PerformDamage(int Dam);
	void CreateMatrix();
};
class TimeReq{
public:
    int NMembers;
    int MaxMembers;
    int* IDS;
    byte* Kinds;
    TimeReq();
    ~TimeReq();
    void Handle();
    void Add(int ID,byte Kind);
    void Del(int ID,byte Kind);
};
extern TimeReq ObjTimer;
//extern byte NSpri[sprNx*sprNx];
extern byte* NSpri;
//extern word* SpRefs[sprNx*sprNx];
extern int** SpRefs
;
extern OneSprite* Sprites;
void InitSprites();
void addSprite(int x,int y,SprGroup* SG,word id);
int GetHeight(int x,int y);
void addScrSprite(int x,int y,SprGroup* SG,word id);
void addTree(int x,int y);
extern OneSprite* Sprites;
extern SprGroup TREES;
extern SprGroup STONES;
extern SprGroup HOLES;
extern SprGroup COMPLEX;
extern SprGroup ANMSPR;
extern SprGroup SPECIAL;
void ProcessSprites();
byte DetermineResource(int x,int y);
byte FindAnyResInCell(int x,int y,int cell,int* Dist,byte Res);
bool CheckDist(int x,int y,word r);
void HideFlags();
//-----------------------L3--------------------------
//#define L3DX (MAPSX>>4)
//#define L3DY (MAPSY>>4)
//#define L3MAX (MAPSX<<2)
//#define L3SH (ADDSH+5)
//extern byte* L3HIMap[L3DX*L3DY];
//void InitL3();
//void ClearL3();
//void SetL3Point(int x,int y,byte H);
//int GetL3Height(int x,int y);
int GetUnitHeight(int x,int y);