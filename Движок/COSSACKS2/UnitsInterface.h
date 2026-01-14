#ifndef __UnitsInterface__
#define __UnitsInterface__

#include "MapTemplates.h"
//#include "unitability.h"

// Object Interface

enum ve_UnitType {ve_UT_hero=0, ve_UT_cannon=1, ve_UT_brigade=2, ve_UT_units=3, ve_UT_building=4};

struct vui_WeaponInfo{
	int Damage;
	int DamageBonus;
	int Charged;
	int WeaponID;
	void Clear(){memset(this,0,sizeof(*this));}
};
struct vui_ProduceInfo{
	bool Enabled;	
	word NProduce;
	word NUnlimit;	//  по бесконечности
	word NIndex;	
	int Stage;
	int MaxStage;
	bool Building;
	//
	char* AccessControl[8];
	int NAccessControl;
	//
	int x,y;		// координаты кнопки
	byte HotKey;	
};
struct vui_UpgradeInfo{
	NewUpgrade* Upg;
	word Index;
	int FileID;
	int SpriteID;
	char* Message;
	//
	int x,y;
	byte HotKey;
};
struct vui_CannInfo{
	OneObject* OB;
	int ChargeType, ChargeStage;
	int Shield;
	int NKills;
	short Damage[3];
	int NShots;
};
struct BrigParam{
	int Life;
	int MaxLife;
	int Morale;
	int MaxMorale;
	int NMembers;
	int NLiveMembers;
	int NShots;
	int NGrenades;
	int NKills;
	int Tiring;
	bool RifleAttack;
	int ReadyPercent;
};

struct vui_UnitInfo{
	bool GroundState;
	byte ActiveState;
	word Amount;
	int Life;
	word LifeMax;
	bool Peasant;
	int WeapType[3];
	int Damage[3];
	int Defence[4];
	int WeapFileID;
	int WeapColdSprite;
	int WeapFireSprite;
	int Shield;
	int NKills;
	bool RifleAttack;
	int NShots;
	int Delay;
	int DelayMax;
	int Morale;
	int MoraleMax;
	int		Speed;
	word	Spread;
	word	BuildingTime;
	int		Price[8];
	int		Cost;
	int		Vision;
	float	AttackSpeed;
	int		AttackRadius;
	short	MoraleRegeneration;
	byte	StrikeProbability;
	short	VeteranKills;
	short	ExpertKills;
	int		AttackUpgrades[3];	// [Level: II, III, IV]
	// [Defence from: chopping, piercing, crushing][Level: II, III, IV]
	int		DefenceUpgrades[3][3];
	MonsterAbility	*pAbilities;
	// RemainUpgrades
	int		RUNumber;
#define UI_RUMAXNUMBER	10
	char*	RUHints[UI_RUMAXNUMBER];
	word	RUIconFileIDs[UI_RUMAXNUMBER], RUIconSpriteIDs[UI_RUMAXNUMBER];
	//
	int Kinetic;
	int KineticMax;
	//
	bool Patrol;
};
class vui_SelPoint;
struct vui_BrigInfo{
	bool SetFromBrig(vui_SelPoint* SP);
	Brigade* BR;
	word BrigID;
	byte ActiveState;
	int Life;
	int MaxLife;
	int Morale;
	int MaxMorale;
	int NMembers;
	int NLiveMembers;
	int NShots;
	int ReadyPercent;
	int Grenades;
	int GrenadesMax;
	int NKills;
	int Tiring;
	bool RifleAttack;
	bool isGrenaders;
	int WeapFileID;
	int WeapColdSprite;
	int WeapFireSprite;
	int Shield;
	int ShieldAdd;
	int WeapType[3];
	int Damage[3];
	int DamageAdd[3];
	int Defence[4];
	int DefenceAdd[4];
	bool isFillable;
	byte FormID[3];
	word CurForm;
	word ShotLine[3];	// 0xFFFF - not allowed, + BrigID*8
	int  BrigDelay;		// stand ground
	int  BrigDelayMax;
	bool AttEnm;		// опущены штыки
	bool NoOrder;
	vui_UnitInfo	UI;
	int	MaxDamageAdd[3];
	int	MaxDefenceAdd[4];
	int ScaleFactor;
	int x,y;	
};

struct vui_HeroInfo: public vui_UnitInfo{
};

struct vui_BuildInfo{
	word Amount;
	bool Ready;
	int Life;
	word LifeMax;
	word Stage;
	word StageMax;
	word Places;
	word Population;
	word PopulationMax;
	byte SingleUpgLevel;
	OneObject* OB[10];
	vui_UnitInfo	UI;
	bool AllowShoot;
};
union vu_SelPointInfo{
	vui_CannInfo Cannon;
	vui_BrigInfo Brigade;
	vui_UnitInfo Units;
	vui_BuildInfo Buildings;
};

struct vu_UpgInfo{
	word ID;
	int Stage;
	int StageMax;
	byte SingleUpgLevel;
};
//
class ActiveUnitAbility;
//
class vui_SelPoint{
public:	
	byte NI;
	word NIndex;
	ve_UnitType Type;
	ve_UnitType Sort;
	vu_SelPointInfo Inf;
	//
	DynArray<vu_UpgInfo> Upg;
	OneObject* CanUpg;
	//
	MonsterAbility* Abl;
	UnitActiveAbilityList* ActAbl;
	//
	DynArray<ActiveUnitAbility*> Effects;
	DynArray<ActiveUnitAbility*> Passive;
	DynArray<ActiveUnitAbility*> Buttons;
	DynArray<ActiveUnitAbility*> LevelUps;
	DynArray<ActiveUnitAbility*> Items;
	ActiveUnitAbility* GetActiveUnitAbility(int Index, int Type);
	//

	OneObject* OB;

	byte NatID;
	bool SearchVictim;
	//DynArray<OneObject*> OB;
	int rX,rY;
	//int GetX();
	//int GetY();

	vui_SelPoint() { NIndex=0xFFFF; Abl=NULL; };
	//vui_SelPoint(OneObject* Obj);
	Init(OneObject* Obj);
	int Cmp(vui_SelPoint* SP);	// 1 - SP<this, 0 - equal, -1 - SP>this
	bool Add(vui_SelPoint* SP);

	//static Enumerator* E;
	//virtual void Init(OneObject* Obj) {};
	//virtual int Cmp(vui_SelPoint* SP) { return 0; };	// compare object with sel_point
	//virtual bool Add(vui_SelPoint* SP) { return false; };
	//virtual void Clear() {};
};
/*
class vui_SP_Units: public vui_SelPoint{ 
public: 
	virtual int Cmp(OneObject* obj); 
};
class vui_SP_Brigade: public vui_SelPoint{ 
public:
	word BrigID;
	virtual int Cmp(OneObject* obj); 
};
class vui_SP_Buildings: public vui_SelPoint{ 
public: 
	virtual int Cmp(OneObject* obj); 
};
*/

//////////////////////////////////////////////////////////////////////////
struct OISelection{
	OISelection() {memset(this,0,sizeof(*this));}
	~OISelection() {
		if(SelBr)free(SelBr); if(SelObj)free(SelObj); if(SelObjA)free(SelObjA);
		Clear();
	}		
	
	void AddObj(OneObject* OB); // добавить обьект по индексу
	void Clear();
	void Process(byte NI);
	void CreateFromSelection(byte NI);

	byte SelNation;
	word MaxSelBr;
	word NSelBr;
	word* SelBr;		// индексы бригад

	word MaxSelObj;
	word NSelObj;
	word* SelObj;		// типы юнитов
	word* SelObjA;		// к-во юнитов

	struct OIS_Bld{
		word NIndex;
		DynArray<word> ID;
	};
	DynArray<OIS_Bld*> Bld;

	word Settlement;
	word Oboz;
	bool RifleAttEnabled;
	bool RifleAttackAllowed;

	int ActiveState;

	int LastSP;
	DynArray<vui_SelPoint> SelPoint;
	DynArray<vui_ProduceInfo> Produce;	
	DynArray<vui_UpgradeInfo> Upgrade;
	void SetProduce();
	void SetUpgrade();
	
	int SPSideLx;

	bool SetLastSP(word LSP);
	word GetNIndex(word SelPointID);	
	GeneralObject* GetGeneralObject(word SelPointID);
	GeneralObject* GetGeneralObject(ParentFrame* PF);
	vui_SelPoint* GetSelPoint(ParentFrame* PF);
	vui_SelPoint* GetLastSelPoint();

	vui_ProduceInfo* GetProduceInfo(word ID);
	vui_UpgradeInfo* GetUpgradeInfo(word ID);
	int		GetUpgradeAmount(void);
};
extern OISelection OIS;
//////////////////////////////////////////////////////////////////////////
regAc(cva_OIS_Rome, vfS	
	ClassRef<DialogsDesk> ddSingle;
	ClassRef<DialogsDesk> ddMulti;	
	,
	REG_AUTO(ddSingle);
	REG_AUTO(ddMulti);	
);

//////////////////////////////////////////////////////////////////////////

struct cvs_BrigPanel{	
	bool Weapon[3];	
	bool Formation;
	bool Disband;
	bool Fill;
	bool Stop;
	cvs_BrigPanel(){
		Restore();
	}
	void Restore(){
		Weapon[0]=true;
		Weapon[1]=true;
		Weapon[2]=true;
		Formation=true;
		Disband=true;
		Fill=true;
		Stop=true;
	}
};
extern cvs_BrigPanel vBrigPanel;
void BrigPanelSet(cvs_BrigPanel& BP);
void BrigPanelShowAll();

//
extern int FI_File;
class OneTrigger;
extern OneTrigger VideoSeq;
extern OneTrigger VidOfSeq;
extern int LastNI,LastBID;
void ProcessVideoForBrigade(OneTrigger* OT,byte NI,word BrigID,int Action,int BackGP,int BackSprite,int VideoX,int VideoY, int vdx, int vdy, int PlayerID);
void ACT(int x);
//
//////////////////////////////////////////////////////////////////////////////////
regAc(cva_OIS_Scroll, vfS, );
regAc(cva_OIS_ScrollLeft, vfL, );
regAc(cva_OIS_ScrollRight, vfL, );
//////////////////////////////////////////////////////////////////////////////////
#endif __UnitsInterface__