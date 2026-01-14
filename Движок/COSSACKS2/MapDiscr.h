#ifndef __MapDiscr__
#define __MapDiscr__
//#define FANTASY
//you should specify this define to protect using this exe for russian localisation
//#define NORUSSTEXT
/*                    Map discription
 *    
 * This file describes map cells, animations, monsters, buildings,
 * flying monsters, on-water monsters...
 */
//#define CDVERSION
//#define CHECK_SYNC_FAILURE
#define NO_CHEATS_IN_MULTIPLAYER
#define CONQUEST
#pragma pack(1)

#ifndef DIALOGS_API
#ifndef DIALOGS_USER
#define DIALOGS_API __declspec(dllexport)
#else//DIALOGS_USER
#define DIALOGS_API __declspec(dllimport)
#define BaseMesh void
#endif//DIALOGS_USER
#endif//DIALOGS_API

typedef unsigned short word;

#include <stdio.h>
#include <stdlib.h>
#include <algorithm>
#include "smart_assert.h"
#include <DString.h>
#include <xmlQuote.h>
#include <ClassEngine.h>
#include <more_types.h>
#include <IMediaManager.h>
//#include "UnitAbility.h"
#include "AntiBug.h"
#include "Icons.h"

#define NBRANCH 4
#define ULIMIT 65535
#define LULIMIT 65000
extern byte MYNATION;
#define SetMyNation(x) {MYNATION=x^133;}
#define MyNation (MYNATION^133)
#define CEXPORT __declspec(dllexport)
#define NFEARSUBJ 16
#define NCOMM 2
extern int GameSpeed;
CEXPORT
char* GetTextByID(char* ID);
#define BRIGDELAY 50
//#define ADDSH 1
extern int ADDSH;
#include "gFile.h"
//Maximum size of cells map
extern int MAXCX;
#define MAXCY MAXCX
#include "MapTemplates.h"
#include "StructuresExtensions.h"
//#define MAXCX (64<<ADDSH)
//#define MAXCY (64<<ADDSH)
//#define MAXCIOFS (MAXCX*MAXCY)
//maximal amount of units in cell
#define MAXINCELL 256
//A*MAXCX=A<<SHFCX
//#define SHFCX (6+ADDSH)
//A*MAXINCELL=SHFCELL
#define SHFCELL 8
//Size of cell
#define CELLSIZE 4
//A*CELLSIZE=A<<CELL2
#define CELL2 2
extern int URESRC[8][8];
#define XRESRC(i,j) (URESRC[i][j]^134525)
#define SetXRESRC(i,j,k) URESRC[i][j]=(k^134525)
#define AddXRESRC(i,j,k) URESRC[i][j]=(((URESRC[i][j]^134525)+k)^134525);
extern int VAL_SHFCX;
extern int VAL_MAXCX;
extern int VAL_MAXCIOFS;
extern int VAL_SPRNX;
extern int VAL_SPRSIZE;
extern int VAL_MAPSX;
extern int MapShift;
extern int WLX;
extern int WMPSIZE;
extern int MaxWX;
extern int MaxWY;
extern int MAPSX;
extern int MAPSY;
extern int MAPSHF;
extern int BMSX;
extern int TopLx;
extern int TopLy;
extern int MaxTop;
extern int TopSH;
extern int B3SX;
extern int B3SY;
extern int B3SZ;
extern int MaxSector;
extern int MaxTH;
extern int MTHShift;
extern int VertInLine;
extern int SectInLine;
extern int MaxPointIndex;
extern int MaxLineIndex;
extern int StratLx;
extern short* THMap;// Map of heights in vertices
//extern byte* AddTHMap;
extern byte* TexMap;//Map of textures in vertices
extern byte* SectMap;//Map of sections on lines
extern int MAXCIOFS;
extern int TSX;

#define SINGLE_ORDER 0
#define HEAD_ORDER   1
#define TILE_ORDER   2
struct Coor3D{
	int x,y,z;
};
typedef void HandlePro(int);
class Weapon;
class SelGroup;
class NewMonster;
#ifdef _USE3D
class OneObjectEx;
#endif // _USE3D

//Описание одной клетки на карте(без картинки)
struct MapCell{
	//word Flags;		//Описывают состояние клетки
//	bool LandLock:1;	//Клетка недоступна для наземных
	bool WaterLock:1;	//Клетка недоступна для водных
	bool AirLock:1;		//Клетка недоступна для летающих
	bool TempLock:1;    //Признак временной недоступности
	byte LayerID;		//Нижний слой на карте-объект типа МЕСТОРОЖДЕНИЕ
    word MonsterID;		//Номер монстра в таблице объектов  
	word BuildingID;	//Номер здания в таблице объектов
	word FlyID;			//Летающий объект
//	word TileID;		//Номер картинки, описывающей поверхность
};//8 bytes

//Элемент в таблице объектов
struct ObjectRef{
	unsigned ObjID:8;	//Тип объекта
	unsigned Location:2;//Наземный/Водный/Летающий/Всех типов
	void* lpObj;		//Ссылка на объект
};
//Максимальное количество объектов
#define MaxObject ULIMIT
//Массив объектов
typedef ObjectRef ObjArray[MaxObject];
class Brigade;
//Описание анимации
struct OneSlide{
	word  FileID;   //Номер файла анимации
	small dx;		//Смещение спрайта по горизонтали
	small dy;		//-----/----- по вертикали
	word  spr;		//Номер спрайта в данном файле
	word SoundID;   //Номер звука
};
typedef OneSlide MovieType[256];
typedef MovieType* lpOneMovie ;
struct  Octant{
	byte count;
	byte ticks;
	small gdx;
	small gdy;
	OneSlide* Movie;
};
typedef Octant Animation[8];
//Информация о анимации и ее применении
struct MoreAnimation{
	Octant* Anm;
	word WhatFor;		//Как будет использоваться данная анимация
	word Kind;          // 
	//word Reserved;
};
typedef MoreAnimation AnimArray[32]; 
typedef word WordArray[32768];
typedef WordArray* lpWordArray;
struct ObjIcon{
	//word FileID;
	word spr;
};
#include "Upgrade.h"
//Описание типов объектов(идентификаторы)
#define EmptyID 0
//#define MonsterID 1

//---------------------SYCRO------------------
DLLEXPORT
int RandNew(char*,int);
DLLEXPORT
void AddRandNew(char*,int,int);
DLLEXPORT
void AddUN(char* File,int Line,int Param,int Type);

#ifdef CHECK_SYNC_FAILURE

#define rando() RandNew(__FILE__,__LINE__)
#define addrand(v) AddRandNew(__FILE__,__LINE__,v)
#define addname(v) AddUN(__FILE__,__LINE__,v,0)

#else

#define rando() RandNew(__FILE__,__LINE__)
#define addrand(v)
#define addname(v)

#endif

//New monster format
//<ANMNAME> ShiftX ShiftY Rotations FileID NFrames frame1 ... frameN
//SETACTIVE <ANMNAME> ActiveFrame ActivePtX ActivePtY 
class NewFrame : public BaseClass {
public:
	short FileID;
	int SpriteID;
	int dx;
	int dy;
	SAVE(NewFrame);
		REG_MEMBER(_gpfile,FileID);
		REG_MEMBER(_int,SpriteID);
		REG_MEMBER(_int,dx);
		REG_MEMBER(_int,dy);
	ENDSAVE;
};
class ComplexFireSource:public BaseClass{
public:
	int  x,y,z;
	byte Dir;
	byte DirDiff;
    word AttTypeMask;
    word CoolDown;
};
class NewAnimation;
class AnimationExtension:public BaseClass{
public:
	NewAnimation* NA;
	float dx,dy,dz;
	float Scale,dDir,dFi;
	int   Period;
	SAVE(AnimationExtension);
	//AnimationExtension
	ENDSAVE;
};
class AnmParticlesSource:public BaseClass{	
public:	
	IParticleSystem* Particle;
	_str NodeName;
	float Phase;
	float AverageDensity;
	int LinkedTo;
	SAVE(AnmParticlesSource);
	REG_AUTO(NodeName);
	REG_MEMBER(_float01,Phase);
	REG_MEMBER(_float01,AverageDensity);
	REG_ENUM(_index,LinkedTo,AnmLinkedTo);
	ENDSAVE;

	IParticleSystem* GetParticle(int ModelID){
        if(Particle)return Particle;
		if(NodeName.str&&NodeName.str[0]){
			int NodeID=IMM->GetNodeID(ModelID,NodeName.str);
			Particle=IMM->GetParticleSystem(NodeID);
		}
		return Particle;
	}
	AnmParticlesSource(){
		Particle=NULL;
	}
};
class AnimFrame3D:public BaseClass{
public:
	int Model;
	int Animation;
	int NFrames;
	float StartAnmTime;
	float FinalAnmTime;
	float Scale;
	float AddDir;
	SAVE(AnimFrame3D);
		REG_MEMBER(_ModelID,Model);
		REG_MEMBER(_ModelID,Animation);
		REG_MEMBER(_int,NFrames);
		REG_MEMBER(_float,StartAnmTime);
		REG_MEMBER(_float,FinalAnmTime);
		REG_MEMBER(_float,Scale);
		REG_MEMBER(_float,AddDir);
	ENDSAVE;
};
class OneObject;
class OneBoundEffect:public BaseClass{
public:
	_str EffectFile;
	_str EffectName;
	int EffectFileID;
	int EffectNameID;
	int SoundID;
	SAVE(OneBoundEffect);
		REG_FILEPATH(EffectFile,"*.eff");
		REG_AUTO(EffectName);
	ENDSAVE;
	virtual DWORD GetAlpha(OneObject* OBJ){return 0;};
	virtual float GetIntensity(OneObject* OBJ){return 1.0f;};
	virtual bool Parse(char* s,_str& ErrLog);
};
class LifeDependentEffect:public OneBoundEffect{
public:
    int LifePercent;
	SAVE(LifeDependentEffect);
		REG_PARENT(OneBoundEffect);
		REG_MEMBER(_int,LifePercent);
	ENDSAVE;
	virtual DWORD GetAlpha(OneObject* OBJ);	
	virtual bool Parse(char* s,_str& ErrLog);
};
class MoveDependentEffect:public OneBoundEffect{
public:	
	SAVE(MoveDependentEffect);
	REG_PARENT(OneBoundEffect);
	ENDSAVE;
	virtual DWORD GetAlpha(OneObject* OBJ);	
	virtual float GetIntensity(OneObject* OBJ);
	virtual bool Parse(char* s,_str& ErrLog);
};
class NewAnimation:public BaseClass{
public:
	union{
		struct{
			word Code1;//used for indexsation
			word Code2;
		};
		DWORD Code;
	};
	NewAnimation* Next;//for hash
	word NSuchAnimations;

	bool Enabled;
	byte AnimationType;//0-2d, 1-3d 2-patch
	bool Reflected;
	bool CanBeBroken;
	bool MoveBreak;
	bool Inverse;
	byte DoubleShot;
	bool DoubleAnm;
	int  NFrames;
	byte Parts;
	byte PartSize;
	byte ActiveFrame;

	short SoundID;
	short SoundProbability;
	int  HotFrame;
	
	short StartDx;
	short StartDy;
	short* ActivePtX;
	short* ActivePtY;
	short* LineInfo;
	char* Name;
	int Rotations;
	ClonesArray<NewFrame> Frames;
	//for 3D models!
	int ModelID;
	int AnimationID;
	int ReflectionID;
	int TimeAnimationID;
	int TimeAnimationFrames;
	int TimeAnimationVariation;
    ClassArray<AnimFrame3D> AnimSet3D;

	int SecondAnimationID;
	float DirFactor;
	float Scale;
	int AddDirection;
	int  AddHeight;

	int HotRadius;
	int HotHeight;
	int HotShift;

	NewAnimation();
	~NewAnimation();
	int firstFrame;
	int lastFrame;
	int TiringChange;
	int SlowFrameStart;
	int SlowFrameEnd;
	int SlowFrameSpeed;
	//for patch
	int PatchTextureID;
	int TexXL;
	int TexYL;
	int TexXR;
	int TexYR;
	bool VerticalPatch;
	ClonesArray<AnimationExtension> AnmExt;
	ClonesArray<AnmParticlesSource> Particles;
	ClassArray<OneBoundEffect> Effects;
	//ClassArray<ComplexFireSource> ComplexFire;
	void AllocFrames(int n){
		NewFrame* F=new NewFrame[n];
		for(int i=0;i<n;i++)Frames.Add(F+i);
	}
	void DrawAt(int frame,byte NI,
		float x,float y,float z,
		float Dir,float Scale,DWORD Color,
		float fiDir,float fiOrt,
		OneObject* OB);//frame - multiplied on 256

    void DrawSpriteUnit( OneObject* OB, const Vector3D& pos, int frame, float Dir, byte NI );
    void DrawSpriteBuilding( OneObject* OB, const Vector3D& pos, int frame, byte NI );

	//bool GetHotSpot(int& x,int& y,int& z);
	SAVE(NewAnimation);
	//REG_CLASS(AnimationExtension);
	REG_CLASS(AnmParticlesSource);
	REG_ENUM_EX(_index,Code,MON_ANIMATIONS,AnimationCode);
	SAVE_SECTION(1);//common section
	REG_MEMBER(_bool,Enabled);	
	REG_MEMBER(_bool,CanBeBroken);
	REG_MEMBER(_bool,MoveBreak);
	REG_MEMBER(_int,TiringChange);
	REG_ENUM(_byte_index,AnimationType,AnimationType);
	SAVE_SECTION(2);//for 2d animation
	REG_MEMBER(_bool,Inverse);
	REG_MEMBER(_bool,DoubleShot);
	REG_MEMBER(_bool,DoubleAnm);
#ifdef FANTASY
	REG_AUTO(Frames);
#endif
	REG_MEMBER(_int,NFrames);
	REG_MEMBER(_int,HotFrame);
	REG_MEMBER(_int,Rotations);
	SAVE_SECTION(4);//for 3d animation
	REG_AUTO(AnimSet3D);
	REG_MEMBER(_ModelID,ModelID);
	REG_MEMBER(_ModelID,AnimationID);
	REG_MEMBER(_ModelID,SecondAnimationID);
	REG_MEMBER(_ModelID,ReflectionID);
	REG_MEMBER(_ModelID,TimeAnimationID);
	REG_MEMBER(_int,NFrames);	
	REG_MEMBER(_int,TimeAnimationFrames);
	REG_MEMBER(_int,TimeAnimationVariation)
	REG_MEMBER(_float,DirFactor);
	REG_MEMBER(_float,Scale);
	REG_MEMBER(_int,AddDirection);
	REG_MEMBER(_int,AddHeight);	
	REG_MEMBER(_int,SlowFrameStart);
	REG_MEMBER(_int,SlowFrameEnd);
	REG_MEMBER(_int,SlowFrameSpeed);
	REG_AUTO(Particles);
	SAVE_SECTION(8);
	REG_MEMBER(_TextureID,PatchTextureID);
	REG_MEMBER(_int,TexXL);
	REG_MEMBER(_int,TexYL);
	REG_MEMBER(_int,TexXR);
	REG_MEMBER(_int,TexYR);
	REG_MEMBER(_bool,VerticalPatch);
	REG_MEMBER(_float,DirFactor);
	REG_MEMBER(_float,Scale);
	SAVE_SECTION(1);
#ifdef FANTASY
	REG_AUTO(AnmExt);	
#endif
	ENDSAVE;
	DWORD GetClassMask(){
		if(AnimationType==1)return 1+4;
		else if(AnimationType==2)return 1+8;
		else return 1+2;
	}
	const char* GetThisElementView(const char*){
		Enumerator* E=ENUM.Get("MON_ANIMATIONS");
		if(E)return E->Get(Code);
		else return NULL;
	}
};
#define AnmHashSize 67
class AnimationsScope:public BaseClass{
public:
	ClassArray<NewAnimation> ANM;
    NewAnimation* AHASH[AnmHashSize];
	void Add(NewAnimation* NA,DWORD Code);
	NewAnimation* Get(DWORD Code);
	NewAnimation* Get(DWORD Code,int Index);
	void CreateHashTable();
	SAVE(AnimationsScope);
	REG_AUTO(ANM);
	ENDSAVE;
};
#ifdef CONQUEST
#define NAttTypes 12 
#else
#define NAttTypes 4 
#endif
struct FogRec{
	word  NWeap;
	int   WProb;
	word* Weap;
};
class AdvCharacter;
class OrderClassDescription{
public:
	char* ID;
	char* Message;
	int IconPos;
	int IconID;
	OrderClassDescription();
	~OrderClassDescription();
};
struct SingleGroup{
	int ClassIndex;
	int NCommon;
	byte* IDX;
	int NForms;
	word* Forms;
};
class FormGroupDescription{
public:
	int NGrp;
	SingleGroup* Grp;
	FormGroupDescription();
	~FormGroupDescription();
	void Load(GFILE* f);
};
extern FormGroupDescription FormGrp;
class OrderDescription{
public:
	char* ID;
	int NLines;
	short** Lines;
	word*   LineNU;
	byte*   Opt;
	int NCom;
	short* ComX;
	short* ComY;
	short YShift;
	int NUnits;
	short BarX0;
	short BarY0;
	short BarX1;
	short BarY1;
	//symmetry groups
	word* Sym4f;
	word* Sym4i;
	word* SymInv;
	//additional parameters
	char AddDamage1;
	char AddShield1;
	char AddDamage2;
	char AddShield2;
	char FAddDamage;
	char FAddShield;
	word StandGroundBonus;
	byte GroupID;
	byte OrdUsage;
	bool DirectionalBonus;
	//------special for COSSACKS2-----
	int FirstActualLine;
	int NActualLines;
	int Width;//V pikselah
	int Hight;
	//---------------
	OrderDescription();
	~OrderDescription();
};
class StroiDescription{
public:
	byte ID;
	word NAmount;
	word* Amount;
	word* LocalID;
	word NUnits;
	word* Units;
};
class OfficerRecord{
public:
	word BarabanID;
	word FlagID;
	word NStroi;
	StroiDescription SDES[5];
};
struct Flags3D{
	int N;
	short Xr;
	short Points[48];
};
struct OneAddSprite{
	word SpriteID;
	short SortX;
	short SortY;
};
struct OneAddStage{
	word GPID;

	OneAddSprite Empty;
	OneAddSprite Stage1;
	OneAddSprite Ready;
	OneAddSprite Dead;

	word AddPoints;

	int NExplPoints;
	short* ExplCoor;

	short* FireX[2];
	short* FireY[2];
	short  NFires[2];

	int Cost[8];
};
#define MaxAStages 5
struct ComplexBuilding{
	byte Mask;
	OneAddStage Stages[MaxAStages];
};
struct ComplexUnitRecord{
	bool CanTakeExRes:1;
	byte GoWithStage [16];
	byte TakeResStage[16];
	byte TransformTo[16];
};
struct ExRect{
	int NRects;
	int Coor[4];
};
struct WeaponInSector{
	int AttIndex;
	int RMin,RMax,Angle;
	int MaxDamage,MinDamage,AnglFactor;
};

#define anm_MotionL			1
#define anm_MotionR			2
#define anm_MotionLB		3
#define anm_MotionRB		4
#define anm_MiniStepL		5
#define anm_MiniStepR		6
#define anm_MiniStepLB		7
#define anm_MiniStepRB		8
#define anm_Fist			9
#define anm_Death			10
#define anm_DeathLie1		11
#define anm_DeathLie2		12
#define anm_DeathLie3		13
#define anm_Stand			14
#define anm_Work			15
#define anm_Trans01			16
#define anm_Trans10			17
#define anm_FallDown        18
#define anm_StandUp         19
#define anm_StandHi			18
#define anm_StandLo			19
#define anm_Build			20
#define anm_BuildHi			21
#define anm_Damage			22

#define anm_Rest			23
#define anm_Rest1			24
#define anm_Rest2			25
#define anm_Rest3			26

#define anm_RotateR			27
#define anm_RotateL			28

#define anm_WorkTree		29
#define anm_WorkStone		30
#define anm_WorkField		31

#define anm_TransX3			32
#define anm_Trans3X			33

#define anm_RotateAtPlace   34
#define anm_Greeting        35
#define anm_Scare			36

#define anm_RestA1          37


#define anm_Attack			100
#define anm_PAttack			200
#define anm_UAttack			300
#define anm_PStand			400
#define anm_PMotionL		500
#define anm_PMotionR		600
#define anm_PMotionLB		700
#define anm_PMotionRB		800
#define anm_PMiniStepL		900
#define anm_PMiniStepR		1000
#define anm_PMiniStepLB		1100
#define anm_PMiniStepRB		1200
#define anm_BuildLo         1300
#define anm_Trans           1400
//#TRANSXY   x=0..9 y=0..9
#define anm_Temp			1500


class MonsterAbility;
class OneExIcon:public BaseClass{
public:
	virtual void Draw(int x,int y,int Lx,int Ly,byte NI){};
};
class OneSpriteExIcon:public OneExIcon{
public:
	int FileID;
	int StartSpriteID;
	int EndSpriteID;
	int Step;
	int dx;
	int dy;
	virtual void Draw(int x,int y,int Lx,int Ly,byte NI);
};
class ExtendedIcon:public BaseClass{
public:
	ClassArray<OneExIcon> Icons;
	void Draw(int x,int y,int Lx,int Ly,byte NI);
};
class NewMonster:public BaseClass{
public:
	AnimationsScope Animations;
	__forceinline NewAnimation* GetAnimation(DWORD ID){
		return Animations.Get(ID);
	}
	__forceinline NewAnimation* GetFirstAnimation(DWORD ID){
		return Animations.Get(ID,0);
	}
	NewAnimation* CreateAnimation(char* Name);
	byte TransXMask;
	word AttackRadius1[NAttTypes];   //начальный радиус атаки в зависимости от типа
	word AttackRadius2[NAttTypes];   //конечный  радиус атаки в зависимости от типа
    word DetRadius1[NAttTypes];      //Радиус, по которому определяется какой тип атаки использовать
    word DetRadius2[NAttTypes];
	word AttackRadiusAdd[NAttTypes]; //добавка к радиусу атаки при условии, 
									 //что юнит в сострянии немедленно атаковать
	word VisibleRadius1;  //additional red radius, shown near unit, it is only visible feature
	word VisibleRadius2;  //additional yellow radius, shown near unit, it is only visible feature

	Weapon* DamWeap[NAttTypes];   //оружие для поражения(пуля,ядро,...)

	byte Rate[NAttTypes];            //16=x1 rate
	word AttackPause[NAttTypes];
	short AngleUp[NAttTypes];         //64=45degrees,32=arctan(1/2)degrees
	short AngleDn[NAttTypes];
	short MinDamage[NAttTypes];
	short MaxDamage[NAttTypes];
	short DamageRadius[NAttTypes];
	word  DamageDecr[NAttTypes];
	byte  WeaponKind[NAttTypes];
	byte  AttackMask[NAttTypes];
#ifdef NEWMORALE
	short       MoraleDecSpeed;
	int         StartMorale;
#else //NEWMORALE
	word FearFactor[NFEARSUBJ];
#endif //NEWMORALE

	byte FearType  [NAttTypes];
	byte FearRadius[NFEARSUBJ];

	word  MyIndex;
	short SrcZPoint;				 //additional height of the weapon
	short DstZPoint;
	word NLockPt;
	byte* LockX;
	byte* LockY;

	word NSLockPt[MaxAStages];
	byte* SLockX[MaxAStages];
	byte* SLockY[MaxAStages];

	word NBLockPt;
	byte* BLockX;
	byte* BLockY;

	word NCheckPt;
	byte* CheckX;
	byte* CheckY;

	ComplexBuilding* CompxCraft;
	ComplexUnitRecord* CompxUnit;
	ExRect* Doors;

	char* Message;
	char* LongMessage;
	char* PieceName;
	bool Officer:1;
	bool Baraban:1;
	bool Building:1;
	bool Peasant:1;
	bool UnitAbsorber:1;
	bool PeasantAbsorber:1;
	bool Transport:1;
	bool Producer:1;
    bool SpriteObject:1;
    bool Wall:1;
	bool RiseUp:1;
	bool SelfProduce:1;
	bool WaterActive:1;
	bool TwoParts:1;
	bool Port:1;
	bool Farm:1;
	bool ShowDelay:1;
	bool Capture:1;
	bool CantCapture:1;
	bool NotHungry:1;
	bool ShotForward:1;
	bool Artilery:1;
	bool Rinok:1;
	bool SlowDeath:1;
	bool AutoNoAttack:1;
	bool AutoStandGround:1;
	bool AttBuild:1;
	bool CanStandGr:1;
	bool Priest:1;
	bool Shaman:1;
	bool ResSubst:1;
	bool Archer:1;
	bool ArtDepo:1;
	bool Artpodgotovka:1;
	bool CanBeKilledInside:1;
	bool CanBeCapturedWhenFree:1;
	bool CanShoot:1;
	bool CanStorm:1;
	bool NoDestruct:1;
	bool SlowRecharge:1;
	bool PeasantConvertor:1;
	bool ExField:1;
	bool BReflection:1;
	bool FullRotation:1;
	bool LikeCannon:1;
	bool FriendlyFire:1;
	bool CommandCenter:1;
	bool ArmAttack:1;
	bool CanFire:1;
	bool Nedorazvitoe:1;
	bool HighUnit:1;
	bool HighUnitCantEnter:1;
	bool StandAnywhere:1;
	bool NikakixMam:1;
	bool No25:1;
	bool CanStopBuild:1;
	bool FormLikeShooters:1;
	bool ShowIdlePeasants:1;
	bool ShowIdleMines:1;
	bool Animal:1;
	bool AI_use_against_buildings:1;
	bool NoFarm:1;
	bool NoMorale:1;
	bool HaveRotateAnimations:1;
	bool SelfTransform:1;
	bool UseLikeGate:1;
	bool OldStyleSelection:1;
	bool LongDeath:1;
	bool NotSelectable:1;	
	bool CanBeInFocusOfFormation:1;
	bool UnbeatableWhenFree:1;
	bool DontTransformToChargeState:1;
	bool ShowInsideUnits:1;
	bool DontStuckInEnemy:1;
	bool NeverCapture:1;
	bool InvisibleOnMinimap:1;
	bool Immortal:1;
	bool LowCollision:1;
	bool DontFillCannon:1;
	bool CanBuild:1;
	bool DontAffectFogOfWar:1;
	bool DontAnswerOnAttack:1;
	bool DontRotateOnDeath:1;
	bool NoInfinity:1;
	bool BornBehindBuilding:1;
	bool GlobalCommandCenter:1;

	byte DirectFight;

	short AI_PreferredAttR_Min;
	short AI_PreferredAttR_Max;
	
	byte WaterCheckDist1;
	byte WaterCheckDist2;
	word SingleShot;

	byte Category;
	byte NInArtDepot;
	byte MeatTransformIndex;
	byte NInFarm;
	byte ArtSet;
	byte TransMask[MaxAStages];
	NewAnimation* UpperPart;
	short UpperPartShift;
	NewAnimation* BuiAnm;
	byte ArtCap[5];
	short BuiDist;
	short MaxPortDist;
	byte NRiseFrames;
	byte RiseStep;
	byte ProdType;
	byte SelfProduceStep;
	word FreeAdd;
	word PeasantAdd;
	word MaxInside;
	word ResConcentrator;
	byte MaxZalp;
	byte NoWaitMask;
	byte UnitRadius;
	byte TempDelay;
	byte FireLimit;
	byte SkillDamageBonus;
	byte SkillDamageMask;
	short SkillDamageFormationBonus;
	short SkillDamageFormationBonusStep;
	short Psixoz;
	int Ves;
	short LowCostRadius;
	short StopDistance;
	short SpeedScaleOnTrees;

	short AddShotRadius;
	byte  PromaxPercent[8];
	byte  PromaxCategory;

	DynArray<short> BuildPtX;
	DynArray<short> BuildPtY;

	DynArray<short> ConcPtX;
	DynArray<short> ConcPtY;

	DynArray<short> PosPtX;
	DynArray<short> PosPtY;

	DynArray<short> CraftPtX;
	DynArray<short> CraftPtY;

	DynArray<short> DamPtX;
	DynArray<short> DamPtY;

	DynArray<short>BornPtX;
	DynArray<short>BornPtY;

    word NShotPt;
    short* ShotPtX;
    short* ShotPtY;
	short* ShotPtYG;
	short* ShotPtZ;
	short* ShotDelay;
	byte* ShotAttType;
	
	byte* ShotDir;
	byte* ShotDiff;
	word* ShotMinR;
	word* ShotMaxR;

	short* FireX[2];
	short* FireY[2];
	short  NFires[2];
	byte MaxResPortion[8];
    int  NeedRes[8];
	int  UMS_Res[8];
    int CenterMX;
    int CenterMY;
    int BRadius;
	word ProduceStages;
	word IconFileID;
	word IconID;

	word Page1Icon;
	word Page2Icon;
	word Page3Icon;
	word Page1IconON;
	word Page2IconON;
	word Page3IconON;

	char* Page1Hint;
	char* Page2Hint;
	char* Page3Hint;
	byte CurrPage;

	int Radius1;
	int Radius2;
	int MotionDist;
	int OneStepDX[256];
	int OneStepDY[256];
	int POneStepDX[256];
	int POneStepDY[256];
	int PicDx,PicDy,PicLx,PicLy;
	short BuildX0,BuildY0,BuildX1,BuildY1;
    byte KillMask;
    byte MathMask;
	byte CO_MathMask;//for complex object - default mathmask
	byte ExitPause;
	//rectangle around the monster
	int RectLx,RectLy,RectDx,RectDy;
	//Monster characteristics
	word Res1cost,Res2cost,Res3cost;
	word Life,Shield;
	word AttRange,VisRange;
	word VisRangeShift;
	word MinAttRange,MaxAttRange,MaxNearRange;
	word AttType;
	word Time,Kind;
	short AnmUpShift;
	short* Bars3D;
	short  NBars;
	char* Name;
	//fogging&fire
	FogRec Fogging;
	FogRec Destruct;
	FogRec Fire;
	short Protection[16];
    word Sprite;
    word SpriteVisual;
	byte ExplosionMedia;
	byte EMediaRadius;
	byte LockType;//0-Land,1-Water
	byte MotionStyle;//0-Soldiers,1-Cavalery,2-Sheeps,3-Fly
	NewAnimation* Veslo;
	NewAnimation* Reflection;
	byte VisionType;
	short VesloRotX;
	short VesloRotY;
	byte NVesel;
	short* VesloX;
	short* VesloY;
	short LinearLength;


	ClassArray<ComplexFireSource> MultiWp;
	/*short* MultiWpX;
	short* MultiWpY;
	short* MultiWpZ;
	short NMultiWp;	*/


	short ResEff;
	short SelectSoundID;
	short AttackSoundID;
	short BornSoundID;
	short OrderSoundID;
	short DeathSoundID;
	short StrikeSoundID;
	short MotionOnRoadSound;

	short* HideTriX;
	short* HideTriY;
	short NHideTri;

	word  Razbros;
	word  ExplRadius;

	short ResConsumer;
	byte ResConsID;

	byte MaxAIndex;
	byte Behavior;
	byte ResAttType;
	byte ResAttType1;
	byte NShotRes;
	word* ShotRes;
	word CostPercent;
	byte VesStart;
	byte VesFin;
	byte MinRotator;
	byte FishSpeed;
	word FishAmount;
	byte InfType;
	word PictureID;
	byte Force;//for AI
	//for strongholds siege
	byte MinOposit;
	byte MaxOposit;
	byte StormForce;

	byte IdlePPos;
	byte IdleMPos;

	byte EuropeanMask;

	WeaponInSector* WSECT;
	short* WavePoints;
	byte NWaves;
	char WaveDZ;
	byte Usage;
	byte BattleForce;
	Flags3D* FLAGS;
	char* MD_File;
	short ArmRadius;
	NewMonster();
	bool CreateFromFile(char* Name);
	void InitNM(char* name);
	NewAnimation* LoadNewAnimationByName(char* Name,int Index=0);	
	//AdvCharacter* AdvChar;
	~NewMonster();
#ifdef COSSACKS2
	word ComplexObjIndex;
	byte GrenadeRechargeTime;
	byte MaxGrenadesInFormation;
	word PortionLimit;
	word BigIconFile;
	word BigIconIndex;
	word MinIconFile;
	word MinIconIndex;	
	word InMenuIconFile;
	word InMenuIconIndex;	
#endif //COSSACKS2
#ifdef _USE3D
	byte BRandomPos; //Построение в бригаде случайно (points)
	byte BRandomSpeed; //Рандомная скорость в бригаде (%)
	word FreeShotDist; //Не поподает по своим (points)
#endif // _USE3D
	word VUI;	// Vital Unit Interface	
	byte BigColdWeapSprite;
	byte BigFireWeapSprite;
	word BigWeapFile;
	word BuildHotKey;
	int PortBackColor;
	word PortBranch;
	word PortBackSprite;
	word StandGroundTime;
	short FlagIndex;
	short FlagX;
	short FlagY;
	short StartFlyHeight;
	short FlyHeight;
	//-----mine----
	short MineRadius;
	short MineDamage;
	short BuildNearBuildingRadius;
	//-------------
	short Directional3DBarsEffect;
	//-
	bool SpecialSel;	
	int DetonationForce;
	int MaxAttackersAmount;
	int ColorVariation;
	byte StrikeProbability;
	MonsterAbility* Ability;
	bool BoidsMoving;
	int BoidsMovingMinDist;
	int BoidsMovingWeight;
	bool SitInFormations;
	int FormationDistanceScale;//100=usual size
	int StrikeFlySpeed;//Speed of unit that flies after strike
	int StrikeFlyMaxSpeed;//MaxSpeed of unit that flies after strike
	int StrikeForce;//modulator of striken unit speed 100
	int StrikeRotate;
	int Expa;
	word RotationAtPlaceSpeed;
	bool DiscreteRotationDirections;
	word MaxMana;
	int SpeedScale;
	NewMonsterExtension Ext;
	//new transport/mine params
	byte InviteMask;
	byte VisitorMask;
	short NPlacesInTransport;
	//morale for Rome-style game
	short VeteranKills;
	short ExpertKills;
	short VeteranExtraDamage;
	short ExpertExtraDamage;
	short VeteranExtraShield;
	short ExpertExtraShield;
	short MoraleRegenerationSpeed;
	short KillDelay;
	word  BrigadeWaitingCycles;
	word  RetreatRadius;
	word  MinTopDistanceToEnterRoad;
	word  MinDistForLineFormations;
	word  MinDistanceToEnterRoad;

	bool ShotAlwaysOn;
	float PlaneFactor;
	word KineticLimit;
	byte MissProbabilityForInsideUnits;
	byte MissProbabilityOnHeightDiff100;
	byte MaxMissProbabilityOnHeightDiff;

	float selScaleX;
	float selScaleY;
	short selShift;
	char  selIndex;
	char  selIndexBR;

	bool Use3pAlign;

	int AlignPt1x;
	int AlignPt1y;
	int AlignPt1z;

	int AlignPt2x;
	int AlignPt2y;
	int AlignPt2z;

	int AlignPt3x;
	int AlignPt3y;
	int AlignPt3z;

	ExtendedIcon ExIcon;
};
class AdvCharacter{
public:
	word		AttackRadius1[NAttTypes];
	word		AttackRadius2[NAttTypes];
	word		DetRadius1[NAttTypes];
	word		DetRadius2[NAttTypes];
	//Weapon*		DamWeap[NAttTypes];
	word		AttackPause[NAttTypes];
	short		MaxDamage[NAttTypes];
	byte		WeaponKind[NAttTypes];
	byte		Rate[NAttTypes];
	byte		Protection[16];
	int			NeedRes[8];
	word		MaxInside;
	word		ProduceStages;
	word		Life;
	word		BirthLife;
	word		Shield;
	byte		FishSpeed;
	word		FishAmount;
	word        Razbros;

	word        MinR_Attack;
	word		MaxR_Attack;
	word        MaxDam;
	word		NInFarm;
	short       ResEff;

#ifdef NEWMORALE
	short       MoraleDecSpeed;
	int         StartMorale;
#else //NEWMORALE
	word		FearFactor[NFEARSUBJ];	
#endif //NEWMORALE
	
	bool		Changed;
	int Speed;	
	int AttackSpeed;
	byte VisionType;
	int VisRange;
	word MaxMana;
	NewMonsterExtension Ext;
};
class NewUpgrade{
public:
	//information
	char* Name;
	char* Message;
	byte Level;
	byte Branch;
	word IconFileID;
	word IconSpriteID;
	char IconPosition;
	byte NatID;
	word  Cost[8];
	byte CtgUpgrade;
	byte CtgType;
	union{
		int  NCtg;
		int  CtgValue;
	};
	word*    CtgGroup;
	byte UnitType;
	union{
		int NUnits;
		int UnitValue;
	};
	word* UnitGroup;
	byte ValueType;
	union{
		int  Value;
		int  NValues;
	};
	int* ValGroup;
	//mechanics
	bool Done:1;
	bool Enabled:1;
	bool PermanentEnabled:1;
	bool IsDoing:1;
	bool Gray:1;
	bool Individual:1;
	bool ManualDisable:1;
	bool ManualEnable:1;
	bool StageUp:1;
	word NStages;
	word CurStage;
	byte NAutoPerform;
	byte NAutoEnable;
	word* AutoPerform;
	word* AutoEnable;
	byte Options;
	byte StageMask;
};
//Описание базового типа, общего для всех объектов
class GeneralObject{
public:
	char* Message;

	bool NoSearchVictim:1;
	bool Enabled:1;
	bool CondEnabled:1;
	bool Transport:1;	
	bool WATT:1;
	bool AATT:1;
	bool P100:1;
	bool T3X3:1;
	bool FWEAP:1;//огненное оружие
	bool AGold:1;
	bool AWood:1;
	bool Submarine:1;
	bool RefreshLife:1;
	bool CanFly:1;
	bool CanAttWall:1;
	bool CanFear:1;
	//bool UseMagic:1;
	bool canNucAttack:1;
	bool AntiNuc:1;
	bool UFO:1;
	bool UFOTrans:1;
	bool CanRepair:1;
	bool ExtMenu:1;
	bool CanDest:1;
	bool ManualDisable:1;
	bool ManualEnable:1;
	byte Options;
	byte NatID;
	byte Country;
	OfficerRecord* OFCR;
	NewMonster* newMons;
	char* MonsterID;
	word MagAgainst;
	word MagWith;
	word IDforUFO;
	//word capMagic;
	byte Kind;
	word MaxAutoAmount;
	word Useful[NBRANCH];//полезность объекта для каждой из отраслей
	word SpUsef[NBRANCH];//удельная полезность
	word AbsNeedPrio;
	word AbsNeedCount;
	word LifeShotLost;
	int	 cost;
	word delay;
	//short wepX;
	//short wepY;
	byte WepSpeed;
	byte WepDelay;
	//byte  VisRadius;
	//byte  VisSpots;
	//byte  SpotType;
	//byte  SpotSize;
	//byte  DangerZone;
	word NUpgrades;
	word* Upg;
	char Wdx[8];
	char Wdy[8];
	byte NIcons;
	word* IRefs;
	word NWeap;
	short NStages;
	Weapon* MWeap[12];
	byte SWPIndex[12];
	short HitSound;
	short ClickSound;
	short OrderSound;
	short DeathSound;
	short BornSound;
	short AttackSound;
	short TreeSound;
	short GoldSound;
	word LockID;
	byte NLockUnits;
	byte Branch;
	
	byte SizeX;
	byte SizeY;
	byte StageMask;

	int NeedRes[8];//cost
	int CostPercent;//cost growing
	bool CostGrowsOnlyForReadyBuildings;//cost grows only when building is ready
	int NFarmsPerSettlement;
	int NewNFarm;

	word FlagBearerID;//used for formations in Rome, sometimes usual unit becomes flagbearer
	word FlagBearerID2;//used for formations in Rome, sometimes usual unit becomes flagbearer
	word UsualFormID;//back transformation
	
	byte ResourceID[4];     //Индекс необходимого ресурса
	word ResAmount[4];      //Сколько необходимо для постройки 
	AdvCharacter* MoreCharacter;


	void GetMonsterCostString(char* st);
	void CloseGO();
};
//Класс видимых объектов-монстры,здания
class Visuals : public GeneralObject
{
	/*
public:
//Базовые показатели
	union{
		struct{
			word MaxLife;
			word MaxShield;
			word Strength;
			word PsychoForce;
			word Dextrity;
			word MinDamage;
			word MaxDamage;
			word Productivity;
			word AttackRange;
			word Reserved1;
			word Reserved2;
			word Reserved3;
			word Reserved4;
			word Reserved5;
			word Reserved6;
			word Reserved7;
		} Basic;
		word Index[16];
	} info;
	//Информация об оружии дальнего поражения
	*/
};
//Upgrade-замена одного типа объектов на другой
class ObjectUpgrade : public GeneralObject
{
public:
	word SourceTypeIndex[8];
	word FinalTypeIndex[8];
};
//Upgrade - для Visuals-улучшение параметров для группы объектов;
class ParameterUpgrade : public GeneralObject
{
	word ObjectIndex[8];
	byte ChangedParameter[8];
	byte AdditionalValue[8];
};

class OneObject;
typedef void ReportFn(OneObject* Sender);
#define MaxFillObj 4
//Описание командного блока
//Приказы 1-го уровня
struct Order1{
	//Указатель на отложенный приказ, если NULL то нет 
	//отложенного приказа 
	Order1* NextOrder;
	byte Used;
	//Уровень приоритета выполнения команды
	//0-нижайший уровень приоритета
	//Данное задание может быть отложено только в том случае,
	//если приоритет прерывания выше приоритета выполнения
	byte PrioryLevel;
	byte OrderType;
	byte OrderTime;
	//Информация по каждому типу приказов
	ReportFn* DoLink;
	union{
		struct{
			byte VisibilityRadius;
		} Stand;
		struct{
			int x;
			int y;
			word PrevDist;
			byte Times;
			word EnemyID;
			word EnemySN;
			word Steps;
		} MoveToXY;
		struct{
			byte xd;
			byte yd;
			byte time;
			word BuildID;
			word BSN;
		} UFO;
		struct{
			word ox;
			word oy;
			word x,y,z;
			byte wep;
		} AttackXY;
		struct{
			word ObjIndex;
			word SN;
			word PrevDist;
			byte wep;
		} MoveToObj;
		struct{
			int ObjIndex;
			word SN;
			short ObjX;
			short ObjY;
			byte AttMethod;
		}BuildObj;
		struct{
			short x;
			short y;
			short x1;
			short y1;
			byte dir;
		} Patrol;
		struct{
			word ObjIndex;
			int Progress;
			int NStages;
			word ID;
			byte PStep;
			word Power;
		}Produce;
		struct{
			word OldUpgrade;
			word NewUpgrade;
			int Stage;
			int NStages;
		}PUpgrade;
		struct{
			byte dir;
		}MoveFrom;
		struct{
			int x;
			int y;
			int SprObj;
			byte ResID;
		}TakeRes;
		struct{
			short LockX;
			short LockY;
			short EndX;
			short EndY;
		}DelBlock;
		struct{
			word x,y;
			short dx,dy;
			word NextX,NextY,NextTop;
		}SmartSend;
		struct{
			word IDS[MaxFillObj];
			word SNS[MaxFillObj];
		}FillObject;
	}info;
};
class GOrder;
struct GlobalIconInfo{
	HandlePro* HPLeft;
	HandlePro* HPRight;
	int LParam;
	int RParam;
	int IconSpriteID;
	char* Hint;
};
typedef bool GOrderFn(OneObject* OB,GOrder* GOR,int LParam,int RParam);
typedef int IconInfo(GOrder* GOR,int IcoIndex,OneObject* OB,GlobalIconInfo* GIN);
class GOrder{
public:
	GOrderFn* CheckDisconnectionAbility;
	GOrderFn* Disconnect;
	GOrderFn* KillOrder;
	IconInfo* GetIcon;
	void*     Data;
	GOrder();
	~GOrder();
};
class Legion;
class Nation;
class FireObjectInfo{
public:
	byte* FireSprite;
	byte* FireStage;
	byte delay;
	short NeedFires;
	short RealFires;
	void  Erase();
};
class FireInfo{
public:
	byte BStage;
	FireObjectInfo* Objs[2];
	void Erase();
};
class UnitActiveAbilityList;
//Информация для каждого индивидуума
#include "OneObject.h"
#pragma warning( default : 4035  )
//Описание оружия
class Nation;

//Описание элемента анимации(оружие в действии)
class AnmObject{
public:
	NewAnimation* NewAnm;
	int x,y,z;//координаты
	int vx,vy,vz;//скорости
	int az;//ускорение
	int xd,yd,zd;//точка назначения
	short GraphDZ;
	short Frame;
	int   LongFrame;
	int   PrevLongFrame;
	short NTimes;
	Weapon* Weap;
	word    Damage;
	OneObject* Sender;
	word ASerial;
	word DestObj;
	word DestSN;
	byte AttType;
	char WeaponKind;
};
class City;
class Needness{
public:
	byte NeedType;//==0-monster,==1-Upgrade
	word MonID;
	byte GroupSize;
	byte Amount;
	word Probability;
	word MoneyPercent;
};
struct SWPAR{
	word Range;
	//byte MinMagic;
	bool Enabled:1;
	bool Fly:1;
};
struct sAI_Req{
	byte Kind;//0-unit,1-upgrade,2-group
	word ObjID;
	word Amount;//if upgrade:1-Done 2-Enabled
};
struct sAI_Devlp{
	byte Kind;//0-unit,1-upgrade
	byte Source;//0-general,1-army,2-selo,3-science
	byte ConKind;//0-unit,2-group
	word ObjID;
	word ConID;
	word Amount;
	word GoldPercent;
	word AtnPercent;
};
struct sAI_Cmd{
	byte Kind;//1-army,2-selo,3-science
	word Info[8];
};
class Branch{
public:
	int  RESAM[8];
	word RESP[8];
	int  RESRM[8];
	void AddTo(byte ResID,int Amount);
	void AddPerc(byte ResID,int Amount,int perc);
	void AddEntire(byte ResID,int Amount);
	void Check(byte NI);
	void Init();
	int GetMonsterCostPercent(byte NI,word NIndex);
	int GetUpgradeCostPercent(byte NI,word NIndex);
};
//Описание нации в целом
struct U_Grp{
	word N;
	word* UIDS;
	word* UVAL;
};
struct vsInfoArmy{
	int Active;
	int Killed;
	int Lost;
};
/*
struct vsInfoRes{
	int Peasa
};
*/
typedef void VoidProc();
class Nation{
public:
	char SCRIPT[16];
	int NMon;
	bool GoldBunt:1;
	GeneralObject* Mon[2048];
	byte SoundMask[2048];
	byte VictState;//0-? 1-defeat 2-victory
	//--- academy ---
	bool vsInfoInit;
	word NErased[2048];		// killed enemy
	word NKilled[2048];		// lost
	word NProduced[2048];
	//
	word NBrErased[2048];	// brigade killed enemy
	word NBrLost[2048];		// brigade lost
	word NBrProduced[2048];
	//
	vsInfoArmy NBrInfantry;	// infantry brigades PortBranch=0
	vsInfoArmy NBrCavalery;	// cavalery brigades PortBranch=1
	vsInfoArmy NCannons;	// cannons PortBranch=2
	//----Resource control-------
	int ResTotal[8];
	int ResOnUpgrade[8];
	int ResOnMines[8];
	int ResOnUnits[8];
	int ResOnBuildings[8];
	int ResOnLife[8];
	int ResBuy[8];
	int ResSell[8];	
	//-------COMMAND CENTERS----------	
	DynArray<word> CentIDS;
	DynArray<word> CentSNS;
    int LastCheckTime;
	OneObject* GetNearestCenter(int UType,int x,int y);//real coords
	//--------------------------------
	City* CITY;
	int NGidot;
	int NFarms;
	int AddFarms;
	word NArtdep;
	word NArtUnits[6];
	word* PAble[2048];
	word PACount[2048];
	char* AIndex[2048];
	char* AHotKey[2048];
	int BranchPercent[NBRANCH];
	//Upgrade UPG;
	int NUpgrades;
	NewUpgrade* UPGRADE[4096];
	int NOct;
	int NSlides;
	//AI Statements
	int CasheSize;
	int TAX_PERCENT;
	int CASH_PUSH_PROBABILITY;
	int NationalAI;//0..32768-determines speed of development
	int AI_Level_MIN;
	int AI_Level_MAX;
	int AI_Forward;
	short DangerSound;
	short VictorySound;
	short ConstructSound;
	short BuildDieSound;
	short UnitDieSound;
	word  LastAttackTime;
	//byte MagicDelay;
	word Harch;
	word NLmenus;
	word* Lmenus;
	word NAmenus;
	word* Amenus;
	word NWmenus;
	word* Wmenus;
	word NCmenus;
	word* Cmenus;
	word NNeed;
	Needness NEED[1024];
	int ResRem[8];
	int ResSpeed[8];
	//ENDAI
	byte NNUM;
	int  NFinf;
	byte palette[256];
	byte NMask;
	word NIcons;
	WIcon* wIcons[1024];
	word NCOND;
	word CLSize[4096];//Access controlling
	word* CLRef[4096];
	//Strange weapon prameters
	word SWRange[256];
	SWPAR SWP[256];
	//-------------NEW AI--------------
	word NGrp;        //Groups of types definition
	word GRSize[32];
	word* GRRef[32];
	word  GAmount[32];//Result of calculation
	word N_AI_Levels;
	word N_AI_Req[256];
	word N_AI_Devlp[256];
	word N_AI_Cmd[256];
	sAI_Req* AI_Req[256];
	sAI_Devlp* AI_Devlp[256];
	sAI_Cmd*   AI_Cmd[256];
	word AI_Level;
	word NPBal;
	word* PBalance;
	word NMineBL;
	word* PBL;

	int POnFood;
	int POnWood;
	int POnStone;

	char* DLLName;
	VoidProc* ProcessAIinDLL;
	HINSTANCE hLibAI;
	//byte GoldMatrix[40];
	//byte IronMatrix[40];
	//byte CoalMatrix[40];
	//------------------SHAR----------------//StartSave
	byte SharStage;
	int SearchRadius;
	int SharX;
	int SharY;
	int SharZ;
	int SharVx;
	int SharVy;
	int SharVz;
	int SharAx;
	int SharAy;
	int SharAz;
	bool Vision:1;
	bool SharAllowed:1;
	bool SharPlaceFound:1;
	bool AI_Enabled:1;
	//---------Upgradable properties--------//
	word FoodEff;
	word WoodEff;
	word StoneEff;
	bool Geology;
	//---------------Constants--------------//
	word UID_PEASANT;//EndSave
	//word UID_TOWER;
	word UID_WALL;
	//word UID_WALL2;
	//word UID_MORTIRA;
	//word UID_PUSHKA;
	word UID_MINE;
	word UID_HOUSE;

	//U_Grp UGRP_TOWUP;
	U_Grp UGRP_MINEUP;
	//U_Grp UGRP_STRELKI;
	//U_Grp UGRP_LIGHTINF;
	//U_Grp UGRP_HARDINF;

	word  MINE_CAPTURE_RADIUS;
	word  MINE_UPGRADE1_RADIUS;
	word  MINE_UPGRADE2_RADIUS;
	word  DEFAULT_MAX_WORKERS;
	word  MIN_PBRIG;

	word  MU1G_PERCENT[3];
	word  MU1I_PERCENT[3];
	word  MU1C_PERCENT[3];

	word  MU2G_PERCENT[3];
	word  MU2I_PERCENT[3];
	word  MU2C_PERCENT[3];

	word  MU3G_PERCENT[3];
	word  MU3I_PERCENT[3];
	word  MU3C_PERCENT[3];
	//--------------------------------------
	char** History;
	int NHistory;
	//-----------------XRONIKA--------------
	byte ThereWasUnit;
	int NPopul;
	int MaxPopul;
	word* Popul;

	int NAccount;
	int MaxAccount;
	word* Account;

	int NUpgMade;
	int MaxUpgMade;
	word* UpgIDS;

	int*  UpgTime;
	void AddUpgrade(word ID,int time);
	void AddPopul(word N);
	//---------------NEW resource-----------
	Branch SELO;
	Branch ARMY;
	Branch SCIENCE;
	Branch GENERAL;
	//----------------------choose unit menu
	char***UnitNames;
	int*   NUnits;
	word** UnitsIDS;
	word FormUnitID;
	//---------------------------------
	void CreateNation(byte NMask,byte NIndex);
	int  CreateNewMonsterAt(int x,int y,int n,bool Anyway,short Dir=-1);
	void AssignWeapon(Weapon* Wpn,int i);
	int CreateBuilding(word ID,byte x,byte y);
	bool CheckBuilding(word ID,byte x,byte y);
	void GetUpgradeCostString(char* st,word UI);
	void CloseNation();
	void AddResource(byte Rid,int Amount);
	void ControlProduce(byte Branch,byte ResID,int Amount);
	int GetNationlKillingExpirience();
};
#define MaxChildWeap 8
extern OneObject* Group[ULIMIT];

typedef char** lplpCHAR;
typedef char*  lpCHAR;
typedef int*   lpINT;
class SelGroup{
public:
	word* Member;
	word* SerialN;
	word NMemb;
	bool CanMove:1;
	bool CanSearchVictim:1;
	bool CanHelpToFriend:1;
	bool Egoizm:1;
	SelGroup();
	void CreateFromSelection(byte NI);
	void SelectMembers(byte NI,bool Shift);
	void DeleteMembers(byte NI);
	void ImSelectMembers(byte NI,bool Shift);
};


//Массив все монстров на карте
#define MaxObj ULIMIT
#define maximage 2048

extern RLCTable MImage[maximage];
extern RLCTable miniMImage[maximage];
void LoadMonsters();
#define maxmap (128<<1)//ADDSH)  //Don't change it!
void LoadLock();
#define MaxAsmCount 16384
#define OneAsmSize 256
#define OneAShift 8;
#define MaxOrdCount 32768
#define OneOrdSize 32;
#define OneOShift 5;
char* GetAsmBlock();
void FreeAsmBlock(char* p );
void InitAsmBuf();
Order1* GetOrdBlock();
//void FreeOrdBlock(Order1* p );
void InitOrdBuf();
extern Order1  OrdBuf[MaxOrdCount];
extern bool	AsmUsage[MaxAsmCount];
extern int	msx;
extern int msy;
extern void Except();
//Размер очереди на выполнение(2^n only !)
#define StSize 8192
#define StMask StSize-1;
extern word ComStc[StSize];
extern word StHead;
extern word StTile;
#define FreeTime 600;
void CarryOutOrder();
void InitStack();
void doooo();
extern word Creator;
extern Nation NAT;
extern int	smapx;
extern int	smapy;
extern int	smaplx;
extern int	smaply;
extern int minix;
extern int	miniy;
//extern HWND hwnd;
void MakePostProcess();
void MakeWPostProcess();
void PrepareProcessing();
extern int Flips;
//extern void FreeOrdBlock(Order1* p );
extern int	mapx;
extern int	mapy;
//byte CreateExObj(Weapon* Wep,short x,short y,
//				 short dx,short dy,short v,byte Mask,OneObject* Send);
//byte CreateExObjDPoint(Weapon* Wep,short x,short y,
//				 short dx,short dy,short v,byte Mask,OneObject* Send,byte dsx,byte dsy);
void InitExplosions();
void ProcessExpl();
extern Weapon FlyFire1;
extern Weapon Vibux1;
void CloseExplosions();
extern byte PlayerMask;
extern bool EgoFlag;
void AddAsk(word ReqID,byte x,byte y,char zdx,char zdy);
extern SelGroup SelSet[80];
extern Weapon* WPLIST[1024];
int CreateZone(int x,int y,int lx,int ly,HandlePro* HPro,int Index,char* Hint);
void DeleteZone(int i);
void ShowProp();
void InitPrpBar();
void ShowAbility();
extern word* Selm[8];
extern word* SerN[8];
extern word* ImSelm[8];
extern word* ImSerN[8];
extern word ImNSL[8];
extern word NSL[8];
void CmdCreateSelection(byte NI,byte x,byte y,byte x1,byte y1);
void CmdSendToXY(byte NI,int x,int y,short Dir);
void CmdAttackObj(byte NI,word ObjID,short DIR);
void CmdCreateTerrain(byte NI,byte x,byte y,word Type);
void CmdCreateBuilding(byte NI,int x,int y,word Type);
void CmdProduceObj(byte NI,word Type);
void CmdMemSelection(byte NI,byte Index);
void CmdRememSelection(byte NI,byte Index);
void CmdBuildObj(byte NI,word ObjID);
void CmdBuildWall(byte NI,short xx,short yy);
void CmdRepairWall(byte NI,short xx,short yy);
void CmdDamageWall(byte NI,word LIN);
void CmdTakeRes(byte NI,int x,int y,byte ResID);
void CmdPerformUpgrade(byte NI,word UI);
void CmdCreateKindSelection(byte NI,byte x,byte y,byte x1,byte y1,byte Kind);
void CmdCreateTypeSelection(byte NI,byte x,byte y,byte x1,byte y1,word Type);
void CmdCreateGoodSelection(byte NI,int x,int y,int x1,int y1);
void CmdCreateGoodKindSelection(byte NI,int x,int y,int x1,int y1,byte Kind);
void CmdCreateGoodTypeSelection(byte NI,int x,int y,int x1,int y1,word Type);
void CmdSetDst(byte NI,int x,int y);
void CmdSendToPoint(byte NI,byte x,byte y);
void CmdAttackToXY(byte NI,byte x,byte y);
void CmdStop(byte NI);
void CmdStandGround(byte NI);
void CmdPatrol(byte NI,int x,int y);
void CmdRepair(byte NI,byte x,byte y);
void CmdGetResource(byte NI,byte x,byte y);
void CmdSendToTransport(byte NI,word ID);
void CmdUnload(byte NI,byte x,byte y);
void CmdDie(byte NI);
void CmdContinueAttackPoint(byte NI,byte x,byte y);
void CmdContinueAttackWall(byte NI,byte x,byte y);
void CmdSitDown(byte NI);
void CmdNucAtt(byte NI,byte x,byte y);
void CmdChooseSelType(byte NI,word ID);
void CmdChooseUnSelType(byte NI,word ID);
void CmdGoToMine(byte NI,word ID);
void CmdLeaveMine(byte NI,word Type);
void CmdErseBrigs(byte NI);
void CmdChooseSelBrig(byte NI,word ID);
void CmdChooseUnSelBrig(byte NI,word ID);
void CmdMakeStandGround(byte NI);
void CmdCancelStandGround(byte NI);
void CmdCrBig(byte NI,int i);
void CmdSelBrig(byte NI,byte Type,word ID);

extern Nation NATIONS[8];

void InitEBuf();
void ExecuteBuffer();
extern char Prompt[80];
extern int PromptTime;
void CreateWaterMap();
extern CEXPORT int SCRSizeX;
extern CEXPORT int SCRSizeY;
extern CEXPORT int RSCRSizeX;
extern CEXPORT int RSCRSizeY;
extern CEXPORT int COPYSizeX;
void CmdGetOil(byte NI,word UI);

extern byte NLocks[64][64];
void SetLock(int x,int y,char val);
inline void IncLock(byte x,byte y){
	NLocks[y>>2][x>>2]++;
	SetLock(x,y,1);
};
inline void DecLock(byte x,byte y){
	NLocks[y>>2][x>>2]--;
	SetLock(x,y,1);
};
extern bool FASTMODE;
extern word MAXOBJECT;
void SetupHint();
CEXPORT void AssignHint(char* s,int time);
void GetChar(GeneralObject* GO,char* s);
void ProcessHint();
extern OneObject OBJECTS[ULIMIT];
extern short TSin[257];
extern short TCos[257];
extern short TAtg[257];
void SetFlyMarkers();
void ClearFlyMarkers();
typedef void UniqMethood(int n,int x,int y);
void HandleSW();
void CreateStrangeObject(int i,byte NI,int x,int y,word ID);
void ShowRLCItemMutno(int x,int y,lpRLCTable lprt,int n);
void ShowRLCItemFired(int x,int y,lpRLCTable lprt,int n);
/*byte CreateUniExObj(Weapon* Wep,int x,int y,
				 short v,byte Mask,
				 OneObject* Send,
				 byte dsx,byte dsy,
				 word DOBJ);*/
/*byte CreateLeadingObject(Weapon* Wep,int x,int y,
				 short v,byte Mask,
				 OneObject* Send,
				 word DestAnm);*/
bool CheckAttAbility(OneObject* OB,word Patient);
void PrepareToEdit();
void PrepareToGame();
extern int MaxSizeX;
extern int MaxSizeY;
extern bool MiniMode;
void SetMiniMode();
void ClearMiniMode();
extern int Shifter;
extern int Multip;
void InitAllGame();
//x,y-coordinates of point on the 2D plane (unit:pix)
//returnfs index of building,otherwise 0xFFFF
word DetermineBuilding(int x,int y,byte NMask);
bool Create3DAnmObject(Weapon* Weap,int xs,int ys,int zs1,
					                int xd,int yd,int zd,
									OneObject* OB,byte AttType,word DestObj);
bool Create3DAnmObject(Weapon* Weap,int xs,int ys,int zs1,
					                int xd,int yd,int zd,
									OneObject* OB,byte AttType,word DestObj,char dz);
int div24(int y);
int Prop43(int y);

int GETV(char* Name);
char* GETS(char* Name);
void LoadAllNations(byte msk,byte NIndex);
int GetExMedia(char* Name);
extern NewAnimation** FiresAnm[2];
extern NewAnimation** PreFires[2];
extern NewAnimation** PostFires[2];
extern int            NFiresAnm[2];
typedef NewAnimation* lpNewAnimation;
extern int UnitsPerFarm;
extern int ResPerUnit;
extern int EatenRes;
#include "UnSyncro.h"
void AFile(char* str);
void AText(char* str);

//extern word fmap[FMSX][FMSX];
extern word* fmap;
//extern byte MCount[MAXCY*MAXCX];
extern byte* MCount;
//extern word NMsList[MAXCX*MAXCY*MAXINCELL];
//extern word* NMsList;

//extern word BLDList[MAXCX*MAXCY];
extern word** BLDList;
//extern byte NPresence[MAXCX*MAXCY];
extern byte* NPresence;
//------------sorting by nations-------------
extern word* NatList[8];
extern int   NtNUnits[8];
extern int   NtMaxUnits[8];
void SetupNatList();
void InitNatList();
void AddObject(OneObject* OB);
void DelObject(OneObject* OB);
void PlayAnimation(NewAnimation* NA,int Frame,int x,int y);
void MakeOrderSound(OneObject* OB,byte SMask);

extern int GoldID;
extern int FoodID;
extern int StoneID;
extern int TreeID;
extern int CoalID;
extern int IronID;
void UpdateAttackR(AdvCharacter* ADC);
//------------------IDS-----------------//
#define MelnicaID	0x01
#define MelnicaIDS  "MELNICA"

#define FarmID		0x02
#define FarmIDS		"FARM"

#define CenterID	0x03
#define CenterIDS	"CENTER"

#define SkladID		0x04
#define SkladIDS	"SKLAD"

#define TowerID		0x05
#define TowerIDS	"TOWER"

#define FieldID		0x06
#define FieldIDS	"FIELD"

#define MineID		0x07
#define MineIDS		"MINE"

#define FastHorseID	0x08
#define FastHorseIDS "FASTHORSE"

#define MortiraID	0x09
#define MortiraIDS  "MORTIRA"

#define PushkaID	0x0A
#define PushkaIDS   "PUSHKA"

#define GrenaderID  0x0B
#define GrenaderIDS "GRENADER"

#define HardWallID  0x0C
#define HardWallIDS "HARDWALL"

#define WeakWallID  0x0D
#define WeakWallIDS "WEAKWALL"

#define LinkorID	0x0E
#define LinkorIDS	"LINKOR"

#define WeakID		0x0F
#define WeakIDS	    "WEAK"

#define FisherID	0x10
#define FisherIDS	"FISHER"

#define ArtDepoID	0x11
#define ArtDepoIDS  "ARTDEPO"

#define SupMortID	0x12
#define SupMortIDS	"SUPERMORTIRA"

#define PortID		0x13
#define PortIDS	    "PORT"

#define LightInfID	0x14
#define LightInfIDS	"LIGHTINFANTRY"

#define StrelokID	0x15
#define StrelokIDS	"STRELOK"

#define HardHorceID	0x16
#define HardHorceIDS "HARDHORCE"

#define PeasantID	0x17
#define PeasantIDS	"PEASANT"

#define HorseStrelokID	0x18
#define HorseStrelokIDS "HORSE-STRELOK"

#define FregatID	0x19
#define FregatIDS   "FREGAT"

#define GaleraID	0x1B
#define GaleraIDS   "GALERA"

#define IaxtaID	    0x1C
#define IaxtaIDS    "IAXTA"

#define ShebekaID	 0x1E
#define ShebekaIDS   "SHEBEKA"

#define ParomID      0x1F
#define ParomIDS     "PAROM"

#define ArcherID    0x20
#define ArcherIDS   "ARCHER"

#define MultiCannonID 0x21
#define MultiCannonIDS "MCANNON"

#define DiplomatID 0x22
#define DiplomatIDS "DIPLOMAT"

#define MentID 0x23
#define MentIDS "MENT"

#define EgerID 0x24
#define EgerIDS "EGER"

#define CostlyID 0x25
#define CostlyIDS "COSTLY"

//-----------------------------------------//
extern OrderClassDescription OrderDesc[16];
extern int NOClasses;
extern OrderDescription ElementaryOrders[256];
extern int NEOrders;
//-----------------------------------------//
void Susp(char* str);
#define SUSPCHECK
#ifdef SUSPCHECK
#define SUSP(x) Susp(x)
#else
#define SUSP(x) ;
#endif 
int OScale(int x);
extern short LastDirection;
#define MobilR 1024
void MemReport(char* str);

extern DWORD LOADNATMASK;
extern char NatCharLo[32][8];
extern char NatCharHi[32][8];
CEXPORT void RunPF(int i,const char* Desc);
CEXPORT void StopPF(int i);
void ShowPF();
extern bool GoAndAttackMode;
extern int FrmDec;
extern int SpeedSh;
extern int REALTIME;
typedef DWORD DPID1, FAR *LPDPID;
class PlayerInfo:public BaseClass{
public:
	DPID1 PlayerID;
	//
	char name[32];	
	byte NationID;
	byte ColorID;
	byte GroupID;
	char MapName[36+4];//36+8
	//
	DWORD MapHashValue;//new
	int ProfileID;
	DWORD Game_GUID;
	byte UserParam[7];
	byte Rank;
	word COMPINFO[8];//=(Nation:5<<11)+(Difficulty:3<<8)+(Team:3<<4)+(Color:4)
	int  CHKSUMM;
	byte MapStyle;	// 0 - no AI, >0 - diff level of AI
	byte HillType;	// new game or saved game
	byte StartRes;
	byte ResOnMap;
	byte Ready;
	byte Host;
	byte Page;
	byte CD;
	word Version;
	byte VictCond;
	word GameTime;
};
//extern PlayerInfo GSets.CGame.PL_INFO[8];
CEXPORT void AssignHint1(char* s,int time);
CEXPORT void AssignHint1(char* s,int time,byte opt);
//-----------------New text files------------------//

extern int LX_fmap;
word GetV_fmap(int x,int y);
extern int VAL_SHFCX;
extern int VAL_MAXCX;
extern int VAL_MAXCIOFS;
extern short randoma[8192];
extern word TexFlags[256];
int AddTHMap(int);
#define SECTMAP(i) (SectMap?SectMap[i]:(word(randoma[word(i&8191)])%3))

word GetNMSL(int i);
void SetNMSL(int i,word W);
void CleanNMSL();
extern int LastActionX;
extern int LastActionY;

#define GM(x) (1<<x)
#define INITBEST 0x0FFFFFFF

//#define INETTESTVERSION
//#define DeleteLastOrder() DeleteLastOrder();addrand(77)
#define CreatePath(x1,y1) CreatePath(x1,y1);addrand(99);
#define NewMonsterSendTo(x,y,Prio,OrdType) NewMonsterSendTo(x,y,Prio,OrdType);addrand(33)
#define NewMonsterPreciseSendTo(x,y,Prio,OrdType) NewMonsterPreciseSendTo(x,y,Prio,OrdType);addrand(44)
#define NewMonsterSmartSendTo(x,y,dx,dy,Prio,OrdType) NewMonsterSmartSendTo(x,y,dx,dy,Prio,OrdType);addrand(55)

#ifndef _USE3D
#define memset(a,b,c) try{memset(a,b,c);}catch(...){}
#endif 

extern bool LMode;
extern byte* XYShift;//map of horisontal shifting
#define SETXSHIFT(v,x) {if(x<-8)x=-8;if(x>7)x=7;if(v>=0&&v<MaxPointIndex)XYShift[v]=(XYShift[v]&0xF0)|((x>>1)+8);};
#define SETYSHIFT(v,x) {if(x<-8)x=-8;if(x>7)x=7;if(v>=0&&v<MaxPointIndex)XYShift[v]=(XYShift[v]&0x0F)|(((x>>1)+8)<<4)};
#define GETXSHIFT(v,x) (int(XYShift[v]&0x0F)-8)
#define GETYSHIFT(v,x) (int(XYShift[v]>>4)-8)
_inline void FreeXYShift(){
	if(XYShift)free(XYShift);
	XYShift=NULL;
}
_inline void SetupXYShift(){
	XYShift=znew(byte,(MaxTH+1)*MaxTH);
	int szz=(MaxTH+1)*MaxTH*2;
	memset(XYShift,0x88,(MaxTH+1)*MaxTH);
}
_inline void ClearXYShift(){
	memset(XYShift,0x88,(MaxTH+1)*MaxTH);
}
void _dbgLogVar(const char* var,const char* val);
void _dbgPrintState(const char* var,char* mask,...);
#define GraphStackSize 16
#define MaxGraphColumns 8
class PerfGraph{
    int History[GraphStackSize][MaxGraphColumns];
	LARGE_INTEGER HStart[MaxGraphColumns];
public:
	PerfGraph();
    void Start(int Kind);
	void End(int Kind);
	void New();
	void Draw(int x,int y,int Lx);
};
extern PerfGraph PGR1;
CEXPORT int GetXOnMiniMap(int x,int y);
CEXPORT int GetYOnMiniMap(int x,int y);
void DrawFillRectOnMiniMap(int x,int y,int x1,int y1,DWORD Color);
void DrawFillRectOnMiniMap(int x,int y,int x1,int y1,DWORD Color1,DWORD Color2,DWORD Color3,DWORD Color4);
void DrawRectOnMiniMap(int x,int y,int x1,int y1,DWORD Color);
void DrawLineOnMiniMap(int x,int y,int x1,int y1,DWORD Color);
void DrawCircleOnMiniMap(int x,int y,int R,DWORD Color);
void ClearLoadMark();
void AddLoadMark(char* Mark, int Value);
void ShowLoadProgress(char* Mark, int v, int vMax);
void LogBattle(char* mask,...);
void LogBattle(int NI,char* mask,...);
char* GetSt_UName(int NIndex);
#endif __MapDiscr__