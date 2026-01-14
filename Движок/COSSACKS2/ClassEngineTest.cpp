#define IMPLEMENT_CLASS_FACTORY
#define DIALOGS_API __declspec(dllexport)
//#include "stdafx.h"
//#include "newmemory.h"
//#define NO_COSS_EXT
#include "stdheader.h"
#include "WeaponSystem.h"
#include "unitability.h"
#include "Extensions.h"
#include "TestEditor.h"
#include "AI_Scripts.h"
#include "VisualSoundSystem.h"
#include "BrigadeAINeuro.h"

#include "ClassPresentation.h"

//
void _UnitType::Save(xmlQuote& xml,void* ClassPtr,void* Extra){
	int V=*((int*)ClassPtr);
	if(V<NATIONS->NMon){
		xml.Assign_string(NATIONS->Mon[V]->MonsterID);
	}
}
bool _UnitType::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	const char* s=xml.Get_string();
	if(!s)return false;
	*(int*)ClassPtr=0;	
	for(int i=0;i<NATIONS->NMon;i++){
		if(NATIONS->Mon[i]->MonsterID && !strcmp(s,NATIONS->Mon[i]->MonsterID)){
			*(int*)ClassPtr=i;
			return !!i;
		}
	}
	return false;
}
const char* _UnitType::GetClassName(){
	return "_UnitType";
}  
void _UnitType::reset_class(void* ptr){
	*((int*)ptr)=0;
}
class TestClass:public ReferableBaseClass{
public:	
	ClassRef<TestClass> Ref;
	SubSection xyz;
	int x;
	int y;
	int z;
	SubSection abc;
	int a;
	int b;
	int c;
	SAVE(TestClass)
		REG_AUTO(Name);
		REG_AUTO(Ref);
		REG_AUTO(xyz);
		REG_MEMBER(_int,x);
		REG_MEMBER(_int,y);
		REG_MEMBER(_int,z);
		REG_AUTO(abc);
		REG_MEMBER(_int,a);
		REG_MEMBER(_int,b);
		REG_MEMBER(_int,c);		
	ENDSAVE
};
class TestClass2:public BaseClass{
public:	
	class BFunc2:public BaseFunction{
	public:
		void EvaluateFunction(){
		}
	};
	class BFunc3:public BaseFunction{
	public:
		int x;
		void EvaluateFunction(){			
			TestClass2* TC2=get_parent<TestClass2>();
			if(TC2){
				TC2->x=rand()*x/32768;
				TC2->y=rand()*x/32768;
				TC2->z=rand()*x/32768;
			}			
		}
		SAVE(BFunc3);
		REG_PARENT(BaseFunction);
		REG_MEMBER(_int,x);
		ENDSAVE;
	};
	int Unit;
	ClonesArray<TestClass> Array;	
	BFunc2 BF2;
	BFunc3 BF3;
	SubSection xyz;
	int x;
	int y;
	int z;
	SubSection abc;
	int a;
	int b;
	int c;
	//ClassArray<_str> _StrClass;
	int ClassIndex;
	SAVE(TestClass2)
		REG_ENUM(_index,Unit,ALL_UNITS);		
		REG_AUTO(BF2);
		REG_AUTO(BF3);		
		REG_AUTO(xyz);
		REG_MEMBER(_int,x);
		REG_MEMBER(_int,y);
		REG_MEMBER(_int,z);
		REG_AUTO(abc);
		REG_MEMBER(_int,a);
		REG_MEMBER(_int,b);
		REG_MEMBER(_int,c);		
		REG_AUTO(Array);
		REG_EXTRA_MEMBER(_ClassIndex,ClassIndex,"Array x");
	ENDSAVE
};
TestClass2 TCL;
void RegTestClass(){
	REG_CLASS(TestClass);
	AddStdEditor("TestClass",&TCL,"",RCE_DEFAULT);	
}
//
#define REGCLASS(x)\
{\
	/*OneClassStorage* OCS=CGARB.GetClass();*/\
	x* t=new x;\
	t->RegisterMembers();\
	delete(t);\
}
//
char* _HintAligningX[]={"Left","Center","Right","Auto",""};
char* _HintAligningY[]={"Top","Center","Bottom","Auto",""};
char* _TEXTALIGN[]={"Left","Center","Right",""};
char* _DISTCHECKTYPE[]={"Gradient","Radius","Isoline",""};
char* _PostProcessType[]={"None","BoundSmoothing","Smoothing",""};
char* _WeightPreferring[]={"None","Center","Left","Right","Top","Bottom","LeftTop","RightTop","LeftBottom","RightBottom",""};
char* _DRAWACTION[]={"Set","Smooth",""};
char* _SMOOTHTYPE[]={"InternalArea","OnlyEdges",""};
char* _HEIGHTOP[]={"Add","Min","Max","Mix",""};
char* _FMAPPING[]={"Planar","Vertical",""};
char* _FRACTYPE[]={"Usual","Strips","Combo","Combo2","Circles",""};
char* _SU_EFF_TYPE[]={"PEASANT+",""};
char* _SU_PRICE[]={"PRICE",""};
char* _SDIR_ENUM[]={"East","North-East","North","North-West","West","South-West","South","South-East",""};
char* _RANK_ENUM[]={"#LIEUTENANT","#CAPTAIN","#MAJOR","#COLONEL","#GENERAL","#MARSHAL", ""};
char* _DefLvl_ENUM[]={"#CWT_DefenceLvl0","#CWT_DefenceLvl1","#CWT_DefenceLvl2","#CWT_DefenceLvl3", ""};
char* _MessText_ENUM[]={"#CDM_LetAContract0","#CDM_LetAContract1","#CDM_LetAContract2","#CDM_LetAContract3","#CDM_LetAContract4","#CDM_LetAContract5","#CDM_NoEnoughMoney","#CDM_HavePropose","#CDM_DontWantContract", ""};
char* _InfoLtr_ENUM[]={"#CDB_MessLtr0","#CDB_MessLtr1","#CDB_MessLtr2","#CDB_MessLtr3","#CDB_MessLtr4","#CDB_MessLtr5","#CDB_MessLtr6", ""};
char* _InfoTxt_ENUM[]={"#CDM_ProposeContr0","#CDM_ProposeContr1","#CDM_ProposeContr2","#CDM_ProposeContr3","#CDM_ProposeContr4","#CDM_ProposeContr5","#CDM_EndOfContr2","#CDM_EndOfContr3","#CDM_EndOfContr4","#CDM_EndOfContr5", ""};
char* _BUTTON_ENUM[]={"#CDB_YesButton","#CDB_NoButton","#CDB_OkButton","#CDB_Agree","#CDB_Cancel","#CDB_Close","#CDB_Accept","#CDB_Reject", ""};
char* _SDCT_ENUM[]={"Primary","Control","Reserve","Artillery",""};
char* _LoadingRules[]={"User_Defined_Settings","Load_From_Source_File",""};
char* _OPTYPE[]={"more","less","more equal","less equal","equal","not equal",""};
char* _OPTYPE_LUA[]={">","<",">=","<=","==","~=",""};
char* _DIFFICULTY[]={"Easy","Normal","Hard","VeryHard",""};
char* _RESTYPE[]={"Wood","Gold","Stone","Food","Iron","Coal",""};
char* _PLAYER_STAT_ENUM[]={"Attacker","Defender",""};
char* _LEVELS[]={"Level1","Level2","Level3",""};
char* _VICTORY_COND[]={"Kill","Capture","Disband","Nothing",""};
char* _QUEST_PAGE_COND[]={"UnitInZone","QuestComplit",""};
char* _FORMATION_TYPE[]={"ALONE","SQUARD",""};
char* _ARIPHMETICALOPS[]={"+","-","*","/",""};
char* _LOGICALOPS[]={"And","Or",""};
char* _COMPAREOPS[]={"==","Mr","Ls","MrOrEq","LsOrEq","!=",""};
char* _SoundUsage[]={"UsualSound","UseGroupSounds","UseLikeGroupSound",""};
char* _SoundGroup[]={"None","Group1","Group2","Group3","Group4","Group5","Group6","Group7","Group8","Group9","Group10",""};
char* _AnmLinkedTo[]={"None","Linked_To_First_Animation","Linked_To_Second_Animation",""};
char* _AnimationType[]={"2D Animation","3D Animation","Patch",""};
char* _LEADING_TYPE[]={"SetCoordinates","MotionToDestPoint",""};
char* _ProgressBarType[]={"Horisontal(LeftRight)","Vertical(TopBottom)","Vertical(BottomTop)",""};
char* _HAlignType[]={"AbsoluteLeftAlign","AbsoluteRightAlign","RelativeAlign",""};
char* _VAlignType[]={"AbsoluteTopAlign","AbsoluteBottomAlign","RelativeAlign",""};
char* _WallBlockUsage[]={"LeftWallBlock","CentralWallBlock","RightWallBlock","Tower","Bastion","Gates",""};
char* _WALL_DESTRUCTION[]={"LeftFront","RightFront","LeftBack","RightBack",""};
char* _BE_OBJECT_TYPE_LIST[]={"NODE","EDGE","SCRIPT","SAVE","vGROUPS","vVALUES","Settings","EDIT","TIMER","NOTHING",""};
char* _BE_NODE_ACTION[]={"ADD","DELETE","MOVE","EDIT","NOTHING",""};
char* _BE_NODE_VIEW_MODE[]={"HIDE","VIEW ALL","FOR GROUP",""};
char* _BE_EDGE_ACTION[]={"ADD","DELETE","EDIT","NOTHING",""};
char* _BE_EDGE_VIEW_MODE[]={"HIDE","VIEW",""};
char* _BE_SCREPT_PROCESS_MODE[]={"ALL","SQUAD","SWITCHED",""};
char* _BE_UNIT_MOVE_MODE[]={"Normal","Peacefull","Aggressive",""};
char* _BE_VALUE_TYPE[]={"Triger","Word","Integer","Unit Type","Upgrade",""};
char* _BE_BORDER_TYPE[]={"TOP","LEFT","RIGHT","BOTTOM",""};
char* _BE_FRAME_STATE[]={"Invisible","Left","Right",""};
char* _BE_ASTIMATE_TYPE[]={"Min","Max","Average",""};
char* _BE_ATTACKER_TYPE[]={"Fires","Tomahawks","PsKillers","Storm",""};
char* _BE_EDIT_TYPE_LIST[]={"Select Edge","Select Node Beg","Select Node End","Select Squad","Select Value","Nothing",""};
char* _BE_SETTINGS_NODE_STYLE_LIST[]={"Rectangle","Buoy",""};
char* _BE_MESSAGE_TYPE_EDITE[]={"TASK","TALK LST","HINT LST","TASK_LIST_VISIBILITY",""};
char* _BE_QUEST_COM_TYPE[]={"SetQuestData","AddComplQuest","SaveData",""};
char* _TextureControl[]={"None","ForThisTexture","ForAllTexturesWithoutThis",""};
char* _FactureControl[]={"None","ForThisFacture","ForAllFacturesWithoutThis",""};
char* _SprRenderType[]={"None","RenderShadowOnBackground","RenderObjectOnBackground",""};
char* _MoraleType[]={"Cossacks2","Rome",""};
char* _PerspMode[]={"Perspective","Plane",""};
char* _LANDTYPE[]={"Any","Land","Islands",""};
char* _TeamList[]={"NoTeams","Team1","Team2","Team3","Team4","Team5","Team6","Team7",""};
// vital
char* _InGameMainMenu[]={"SinGlobalMap","SinBattles","MulBattles",""};
//
char* _ResType[]={"Wood","Gold","Stone","Food","Iron","Coal","Nothing",""};
int  _ResTypeV[]={0,1,2,3,4,5,0xFF};
char* _SpriteViewType[]={"Sprite","3D-Animation","FieldPath","PartOfTexture",""};
char* _SpriteAlignType[]={"NoAlign","Horizontal","Vertical",""};

char* _vbeOpType[]  ={"Set","Add","Modulate",""};
char* _vbeEfStart[] ={"OnDrawFirstTime","OnMouseOver","OnMouseNotOver","OnClick","OnProgramer",""};
char* _vbeAmpLow[]  ={"Constant","Linear","Exp","OneWave","SquareLow","QubicLow","Table",""};
char* _vbeCycling[] ={"None","Sin","Cos","Random","SignedSin","SignedCos",""};

char* _Mapping[]  ={"Planar","Vertical","Edges",""};
char* _DispDir[]  ={"Horisontal","Normal",""};
char* _BumpStyle[]={"Positive","Negative","Both",""};

char* _AlertTypeCOM[]={"Scale","Turn","Flip","Appear","Colour",""};
char* _LAYERSLIST[]={"Climat1","Climat2","Climat3","Climat4","Water","Trees","Smooth","Zone1","Zone2","Zone3","Zone4","Zone5",""};
//
char* _MM_GameMode[]={"gmSin","gmMul","gmEdit","gmCamp",""};
char* _MM_LogicCon[]={"==","!=",""};
char* _MM_LogicOp[]={"Set","And","Or",""};
char* _MAI_ACType[]={"Fires","Storm","PsKillers","Tomahowks",""};
char* _ShadowQuality[]={"High","Medium","Low","None",""};

char* _GM_CloseButtons[]={"Single","Campaign","Multi","Victory","Defeat",""};

char* _BE_GAME_NAME[]={"Alexander","CossacsII","Fantasy",""};
char* _UNITS_SEL_TYPE[]={"Round","Blinking","RedColor",""};
char* _AI_CheckVictoryCondition[]={"Any","TownHalls","Score","Mines","Heros",""};

char* _ICONTYPE[]={"Use_Sprite","Use_Texture","Use3D_Object",""};
char* _FIRE_OR_SMOKE[]={"EditFire","EditSmoke",""};
char* _LOCKTYPE[]={"Land","Water",""};
char* _MOTIONSTYLE[]={"StandartMotion","Sheeps","ComplexObject","FlyingObject",""};
int   _MOTIONSTYLEV[]={7,5,6,8,0};
char* _UNITSSELRULE[]={"RandomUnits","NearestUnits","Unsorted",""};
//
#define RegEnum(x)\
{\
	Enumerator* E=ENUM.Get(#x);\
	for(int i=0;_##x[i][0]!=0;i++)E->Add(_##x[i]);\
}
#define RegEnumT(x)\
{\
	Enumerator* E=ENUM.Get(#x);\
	for(int i=0;_##x[i][0]!=0;i++)E->Add(GetTextByID(_##x[i]));\
}
//
#define RegEnumV(x)\
{\
	Enumerator* E=ENUM.Get(#x);\
	for(int i=0;_##x[i][0]!=0;i++)E->Add(_##x[i],_##x##V##[i]);\
}
void InitTypeList();
void vui_CreateActionEnum();
void vgf_CreateHotKeysArray();
//
void test(){
	InitTypeList();
	// vital
	RegEnum(InGameMainMenu);
	//
	RegEnum(HintAligningX);
	RegEnum(HintAligningY);	
	RegEnum(TEXTALIGN);
	RegEnum(DISTCHECKTYPE);
	RegEnum(PostProcessType); 
	RegEnum(WeightPreferring);
	RegEnum(DRAWACTION);
	RegEnum(SMOOTHTYPE);
	RegEnum(HEIGHTOP);
	RegEnum(FMAPPING);
	RegEnum(FRACTYPE);
	RegEnum(SU_EFF_TYPE);
	RegEnum(SU_PRICE);
	RegEnum(SDIR_ENUM);
	RegEnumT(RANK_ENUM);
	RegEnumT(DefLvl_ENUM);
	RegEnumT(MessText_ENUM);
	RegEnumT(InfoLtr_ENUM);
	RegEnumT(InfoTxt_ENUM);
	RegEnumT(BUTTON_ENUM);
	RegEnum(SDCT_ENUM);
	RegEnum(LoadingRules);
	RegEnum(OPTYPE);
	RegEnum(OPTYPE_LUA);
	RegEnum(DIFFICULTY);
	RegEnum(RESTYPE);
	RegEnum(PLAYER_STAT_ENUM);
	RegEnum(LEVELS);
	RegEnum(VICTORY_COND);
	RegEnum(QUEST_PAGE_COND);
	RegEnum(FORMATION_TYPE);
	RegEnum(ARIPHMETICALOPS);
	RegEnum(LOGICALOPS);
	RegEnum(COMPAREOPS);
	RegEnum(SoundUsage);
	RegEnum(SoundGroup);
	RegEnum(AnmLinkedTo);
	RegEnum(AnimationType);
	RegEnum(LEADING_TYPE);
	RegEnum(ProgressBarType);
	RegEnum(HAlignType);
	RegEnum(VAlignType);
	RegEnum(WallBlockUsage);
	RegEnum(WALL_DESTRUCTION);
	RegEnum(BE_OBJECT_TYPE_LIST);
	RegEnum(BE_NODE_ACTION);
	RegEnum(BE_NODE_VIEW_MODE);
	RegEnum(BE_EDGE_ACTION);
	RegEnum(BE_EDGE_VIEW_MODE);
	RegEnum(BE_SCREPT_PROCESS_MODE);
	RegEnum(BE_UNIT_MOVE_MODE);
	RegEnum(BE_VALUE_TYPE);
	RegEnum(BE_BORDER_TYPE);
	RegEnum(BE_FRAME_STATE);
	RegEnum(BE_ASTIMATE_TYPE);
	RegEnum(BE_ATTACKER_TYPE);
	RegEnum(BE_EDIT_TYPE_LIST);
	RegEnum(BE_SETTINGS_NODE_STYLE_LIST);
	RegEnum(BE_MESSAGE_TYPE_EDITE);
	RegEnum(BE_QUEST_COM_TYPE);
	RegEnum(BE_GAME_NAME);
	RegEnum(TextureControl);
	RegEnum(FactureControl);
	RegEnumV(ResType);
	RegEnumV(MOTIONSTYLE);
	RegEnum(UNITSSELRULE);
	RegEnum(SpriteViewType)
	RegEnum(SpriteAlignType)
	RegEnum(SprRenderType);
	RegEnum(MoraleType);
	RegEnum(PerspMode);
	RegEnum(Mapping);
	RegEnum(DispDir);
	RegEnum(BumpStyle);

	RegEnum(vbeOpType);
	RegEnum(vbeEfStart)
	RegEnum(vbeAmpLow);
	RegEnum(vbeCycling);

	RegEnum(LAYERSLIST);

	RegEnum(AlertTypeCOM);
	RegEnum(LANDTYPE);
	RegEnum(TeamList);
	//
	RegEnum(MM_GameMode);
	RegEnum(GM_CloseButtons);
	//
	RegEnum(MM_LogicCon);
	RegEnum(MM_LogicOp);
	RegEnum(MAI_ACType);	
	RegEnum(UNITS_SEL_TYPE);
	RegEnum(AI_CheckVictoryCondition);
	RegEnum(ICONTYPE);
	RegEnum(FIRE_OR_SMOKE);
	RegEnum(LOCKTYPE);
	//	
    RegEnum(ShadowQuality);
	vui_CreateActionEnum();
	vgf_CreateHotKeysArray();
	vgf_CreateInterfSynchroAction();
	// Keys
	Enumerator* E=ENUM.Get("ve_HotKey");
	void CreateKeysEnumerator(Enumerator* E);
	CreateKeysEnumerator(E);
	void CreateAnimationsEnumerator();
	CreateAnimationsEnumerator();
	
	REG_LOADSAVE_FOR_CLASS(ClonesArray<WeaponModificator>,"WeaponSystem\\","*.ws.xml");
	REG_LOADSAVE_FOR_CLASS(ArrayAbilities,"UnitsAbility\\","*.ua.xml");
	REG_LOADSAVE_FOR_CLASS(CNeuroStorage,"AI\\","NeuroStorage.xml");

	//enumerator for sounds
	SoundsEnumerator* SE= new SoundsEnumerator;
	E=ENUM.Get("ALL_SOUNDS");
	E->SetProcEnum(SE);

	//enumerator for upgrades
	UpgradesEnumerator* UE=new UpgradesEnumerator;
	E=ENUM.Get("ALL_UPGRADES");
	E->SetProcEnum(UE);

	//enumerator for formations
	FormationsEnumerator* FE=new FormationsEnumerator;
	E=ENUM.Get("ALL_FORMATIONS");
	E->SetProcEnum(FE);

	//enumerator for unit types
	UnitTypesEnumerator* UNE=new UnitTypesEnumerator;
	E=ENUM.Get("ALL_UNITS");
	E->SetProcEnum(UNE);

	//borders enumerator
	BordersEnumerator* BE=new BordersEnumerator;
	E=ENUM.Get("BORDERS");
	E->SetProcEnum(BE);

	//enumerator for weapons
	void CreateWeaponEnumerator();
	CreateWeaponEnumerator();

	//enumerator for complex objects
	void CreateComplexObjectsEnumerator();
	CreateComplexObjectsEnumerator();

	//enumerator for UnitAbility
	void CreateUnitAbilityEnumeratorEnumerator();
	CreateUnitAbilityEnumeratorEnumerator();

	void RegisterAllSavers();
	RegisterAllSavers();	

	LoadSounds("SoundList.txt");	

	void LoadRDS();
	LoadRDS();
	//resources enumerator
	E=ENUM.Get("RESOURCES");
	for(int i=0;i<8;i++){
		if(RDS[i].Enabled){
			E->Add(RDS[i].ResIDS);			
		};
	};
	void RegisterPresentations();
	RegisterPresentations();
}
class ClassT1:public BaseClass{
public:
	virtual _str* GetObjectNamePointer(){
		return &Name;
	}
	_str Name;
	int x;
	int y;
	SAVE(ClassT1);
	REG_AUTO(Name);
	REG_MEMBER(_int,x);
	REG_MEMBER(_int,y);
	ENDSAVE;	
};
class Class2:public BaseClass{
public:
	ClonesArray<ClassT1> CA1;
	ClassRef<ClassT1> CR;
	ClonesArray<ClassT1> CA2;	
	ClassT1 CT1;
	SAVE(Class2);
	REG_LOADSAVE("Dialogs\\","*.c2.xml")
	REG_AUTO(CA1);
	REG_AUTO(CR);
	REG_AUTO(CA2);
	REG_AUTO(CT1);
	ENDSAVE;
};
class Class3:public BaseClass{
public:
	Class2 C2;
	SAVE(Class3);
	REG_AUTO(C2);
	ENDSAVE;
};
Class3 C2test;
extern int ItemChoose;
bool MMItemChoose(SimpleDialog* SD);
void TestSomeClass(){
	return;
	extern int GameMode;
	GameMode=1;
	REG_CLASS(ClassT1);
	REG_CLASS(Class2);
	xmlQuote xml;
	ItemChoose=-1;
	if(xml.ReadFromFile("Dialogs\\WeaponSystem.DialogsSystem.xml")){
		DialogsSystem DSS;
		ErrorPager EP;
		DSS.Load(xml,&DSS,&EP);
		SimpleDialog* Desk=DSS.Find("OptionsDesk");
		SimpleDialog* OK=DSS.Find("OK");
		SimpleDialog* CANCEL=DSS.Find("CANCEL");
		if(Desk&&OK&&CANCEL){
			int x0,y0,x1,y1;
			DSS.GetDialogsFrame(x0,y0,x1,y1);
			if(x1>x0){
				DSS.x=(RealLx-x1+x0)/2;
				DSS.y=(RealLy-y1+y0)/2;
				OK->OnUserClick=&MMItemChoose;
				OK->UserParam=1;
				CANCEL->OnUserClick=&MMItemChoose;
				CANCEL->UserParam=1;
				ClassEditor CE;
				CE.CreateFromClass(Desk,0,0,Desk->x1-Desk->x,Desk->y1-Desk->y,&C2test,3,"EmptyBorder");
				do{					
					ProcessMessages();					
					DSS.ProcessDialogs();
					CE.Process();
					DSS.RefreshView();
				}while(ItemChoose==-1);
			}
		}
	}	
}
ClassPresentationList PresList;
void RegPresListEditor(){
	AddStdEditor("ClassPresentation",&PresList,"",RCE_CENTRAL_POSITION|RCE_EXITONESCAPE|RCE_EXITONENTER);
}