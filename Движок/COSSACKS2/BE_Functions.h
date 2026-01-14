#ifndef __BE_FUNCTIONS__
#define __BE_FUNCTIONS__

class lvCSquardsOnMap;
class _UnitType;
// lvCGroup
class lvCNode;
class lvCGroup;
// vVALUE
class vvBASE;
class vvTRIGER;
class vvWORD;
class vvINTEGER;
class vvTEXT;
class vvPOINT2D;
class vvPOINT_SET;
class vvVector3D;
class vvDIALOG;
class vvMissionLOG;
class vvMISSMGR;
class vvTASKS_CII;
// GraphObject
class lvCGraphObject;
class lvCDeffFilmMenu;
// Conditions
class lvCCondition;
// Film
class lvCFilm;

// GLOBAL ////////////////////////////////////////////////////////////////
// 0 - archer
// 1 - short range unit
// 2 - shooter
// 3 - pesants
// 4 - not hidden
// 5 - tomagavk
void	FilterUnitsByCategory(lvCGroup* pvg_Src,lvCGroup* pvg_Dst,int Ctg);	// Ctg = (0,1,2,3,4,5)
void	AddStorm(lvCGroup* Grp, byte Owner, int Diff,bool remove);
void	AddFirers(lvCGroup* Grp,byte Owner,bool remove);
void	AddPsKillers(lvCGroup* Grp,byte Owner,bool remove,bool SeakMine/*=true*/);
void	AddTomahawks(lvCGroup* Grp,byte Owner,bool remove,word Base/*=0*/,int DriftRadius/*=0*/, int CriticalMass/*=0*/);

bool	CheckFilePath(char* _FilePath);
bool	CheckFilePath(_str& _FilePath);
class lvCBaseOperCond : public BaseClass
{
public:
	lvCBaseOperCond();
	lvCBaseOperCond(lvCBaseOperCond* BaseOperCond);
	~lvCBaseOperCond()	{};

	DWORD	InfID;
	_str	Descr;
	int		x0,y0;
	int		x1,y1;
	DWORD	squardID;
	DWORD	timeInProc;
	bool	use_vGroup;
	bool	first;

	// Use node instade zone
	bool	UseNode;
	int		parNode;

	virtual	void	Draw()	{};
	virtual	void	GetCopy(lvCBaseOperCond** pCopy);
	virtual	void	SetEdgeXY(int _x0,int _y0,int _x1,int _y1)	{ 
		x0 = _x0; y0 = _y0;
		x1 = _x1; y1 = _y1;
		first = true;
	};
	virtual	void	SetSquardID(DWORD sqID){
		squardID = sqID;
		first = true;
	};
			DWORD	GetClassMask(){
						return	0x00000002;
					};

	virtual	int		Power()	{ return 0; };

	class CPRESENTATION	fShowPresentation;

	SAVE(lvCBaseOperCond);
		REG_AUTO(fShowPresentation);
		SetReadOnlyMode();
		REG_MEMBER(_int,x0);
		SetReadOnlyMode();
		REG_MEMBER(_int,y0);
		SetReadOnlyMode();
		REG_MEMBER(_int,x1);
		SetReadOnlyMode();
		REG_MEMBER(_int,y1);
		SetReadOnlyMode();
		REG_MEMBER(_DWORD,squardID);
		SetReadOnlyMode();
		REG_MEMBER(_DWORD,timeInProc);
		SetReadOnlyMode();
		REG_MEMBER(_bool,use_vGroup);
		SetReadOnlyMode();
		REG_MEMBER(_bool,first);
		REG_MEMBER(_bool,UseNode);
		REG_ENUM(_index,parNode,ALL_vNODES_ON_MAP);
	ENDSAVE;
};

//======================================================================//
//=================    OPERATION FOR SCRIPT		========================//
//======================================================================//
// lvCOperation //////////////////////////////////////////////////////////
class lvCOperation : public lvCBaseOperCond
{
public:
	lvCOperation()	{ InfID = _lvCOperation_; RepeatInTime=false; myID=-1; };
	lvCOperation(lvCOperation* pOperation) : lvCBaseOperCond(dynamic_cast<lvCBaseOperCond*>(pOperation)) {};
	~lvCOperation()	{};
public:
	virtual	void	Draw()	{};
	virtual int		Process(int time);
	virtual	int		Complite()			{ return 1; };
	virtual	void	GetCopy(lvCOperation** pCopy);

	bool			RepeatInTime;
	int				CurIter;
	void			AddIteration()		{ if (RepeatInTime) CurIter++; };
	void			CheckIteration();

	virtual char*	GetSourceCode(int shift = 0);

	// for pro mission filer
	int myID;				// -1

public:
	SAVE(lvCOperation);
		REG_PARENT(lvCBaseOperCond);
		REG_MEMBER(_bool,RepeatInTime);
	ENDSAVE;
};
class lvCKillNatinZone	: public lvCOperation
{
public:
	lvCKillNatinZone(lvCKillNatinZone* pKillNatinZone);
	lvCKillNatinZone()	{ InfID = _lvCKillNatinZone_; };
	~lvCKillNatinZone()	{};
public:
	int	parNat;
	int parZone;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCKillNatinZone);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parNat);
		REG_ENUM(_index,parZone,ALL_ZONES_ON_MAP);
	ENDSAVE;
};
int KillNatinInPOS_lua(int nat,int x,int y,int R);
class lvCKillNatNear	: public lvCOperation
{
public:
	lvCKillNatNear(lvCKillNatNear* pKillNatNear);
	lvCKillNatNear()	{ InfID = _lvCKillNatNear_; };
	~lvCKillNatNear()	{};
public:
	int		parNat;
	int		vGrp;
	int		parRad;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCKillNatNear);
	REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parNat);
		REG_MEMBER(_int,parRad);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,vGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSelectAll	: public lvCOperation
{
public:
	lvCSelectAll(lvCSelectAll* pSelectAll);
	lvCSelectAll()	{ InfID = _lvCSelectAll_; };
	~lvCSelectAll()	{};
public:
	int	parNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSelectAll);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_int,parNat);
	ENDSAVE;
};
int SelectAll_lua(int nat);
class lvCChangeAS	: public lvCOperation
{
public:
	lvCChangeAS(lvCChangeAS* pChangeAS);
	lvCChangeAS()	{ InfID = _lvCChangeAS_; };
	~lvCChangeAS()	{};
public:
	int	parNat;
	int parState;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCChangeAS);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parNat);
		REG_ENUM(_index,parState,BE_UNIT_MOVE_MODE);
	ENDSAVE;
};
int ChangeAS_lua(int nat,int state);
class lvCSelSendTo	: public lvCOperation
{
public:
	lvCSelSendTo(lvCSelSendTo* pSelSendTo);
	lvCSelSendTo()	{ InfID = _lvCSelSendTo_; parDir=512; };
	~lvCSelSendTo()	{};
public:
	int parNat;
	int parZn;
	int	parDir;
	int	parType;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSelSendTo);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parNat);
		REG_ENUM(_index,parZn,ALL_ZONES_ON_MAP);
		REG_MEMBER(_int,parDir);
		REG_MEMBER(_int,parType);
	ENDSAVE;
};
int SelSendTo_lua(int nat,int x,int y,int dir,int type);
class lvCGroupSendTo	: public lvCOperation
{
public:
	lvCGroupSendTo(lvCGroupSendTo* pGroupSendTo);
	lvCGroupSendTo()	{ InfID = _lvCGroupSendTo_; parDir=512; first=true; };
	~lvCGroupSendTo()	{};
public:
	int	parGrp;
	int parZn;
	int	parDir;
	int	parType;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCGroupSendTo);
			REG_PARENT(lvCOperation);
			REG_ENUM(_index,parZn,ALL_ZONES_ON_MAP);
			REG_MEMBER(_int,parDir);
			REG_MEMBER(_int,parType);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSelSendToNode	: public lvCOperation
{
public:
	lvCSelSendToNode(lvCSelSendToNode* pSelSendToNode);
	lvCSelSendToNode()	{ InfID = _lvCSelSendToNode_; parDir=512; };
	~lvCSelSendToNode()	{};
public:
	int parNat;
	int	parDir;
	int	parType;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSelSendToNode);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parNat);
		REG_MEMBER(_int,parDir);
		REG_MEMBER(_int,parType);
	ENDSAVE;
};
class lvCGroupSendToNode	: public lvCOperation
{
public:
	lvCGroupSendToNode(lvCGroupSendToNode* pGroupSendToNode);
	lvCGroupSendToNode()	{ InfID = _lvCGroupSendToNode_; parDir=512; first=true; };
	~lvCGroupSendToNode()	{};
public:
	int	parGrp;
	int	parDir;
	int	parType;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCGroupSendToNode);
			REG_PARENT(lvCOperation);
			REG_MEMBER(_int,parDir);
			REG_MEMBER(_int,parType);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCRotateGroup	: public lvCOperation
{
public:
	lvCRotateGroup(lvCRotateGroup* pRotateGroup);
	lvCRotateGroup()	{ InfID = _lvCRotateGroup_; parDir=512; first=true; };
	~lvCRotateGroup()	{};
public:
	int	parGrp;
	int	parDir;
	int	prior;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCRotateGroup);
			REG_PARENT(lvCOperation);
			REG_MEMBER(_int,parDir);
			REG_MEMBER(_int,prior);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCRotate	: public lvCOperation
{
public:
	lvCRotate(lvCRotate* pRotate);
	lvCRotate()	{ InfID = _lvCRotate_; parDir=512; first=true; };
	~lvCRotate()	{};
public:
	int	parDir;
	int	prior;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	int		Complite();
	virtual	void	GetCopy(lvCOperation** pCopy);

public:
	SAVE(lvCRotate);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parDir);
		REG_MEMBER(_int,prior);
	ENDSAVE;
};
class lvCSendToNode	: public lvCOperation
{
public:
	lvCSendToNode(lvCSendToNode* pSendToNode);
	lvCSendToNode()	{ InfID = _lvCSendToNode_; parDir=512; first=true; RepeatInTime=true; };
	~lvCSendToNode()	{};
public:
	int	parDir;
	int	parType;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	int		Complite();
	virtual	void	GetCopy(lvCOperation** pCopy);

public:
	SAVE(lvCSendToNode);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,parDir);
		REG_MEMBER(_int,parType);
	ENDSAVE;
};
class lvCSelectUnits	: public lvCOperation
{
public:
	lvCSelectUnits(lvCSelectUnits* pSelectUnits);
	lvCSelectUnits()	{ InfID = _lvCSelectUnits_; };
	~lvCSelectUnits()	{};
public:
	int		parGrp;
	bool	parAdd;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSelectUnits);
			REG_PARENT(lvCOperation);
			REG_MEMBER(_bool,parAdd);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSetUnitState	: public lvCOperation
{
public:
	lvCSetUnitState(lvCSetUnitState* pSetUnitState);
	lvCSetUnitState()	{ InfID = _lvCSetUnitState_; };
	~lvCSetUnitState()	{};
public:
	int		parGrp;
	int		parmode;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetUnitState);
			REG_PARENT(lvCOperation);
			REG_ENUM(_index,parmode,BE_UNIT_MOVE_MODE);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSetState	: public lvCOperation
{
public:
	lvCSetState(lvCSetState* pSetState);
	lvCSetState()	{ InfID = _lvCSetState_; parmode=0;};
	~lvCSetState()	{};
public:
	int		parmode;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetState);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,parmode,BE_UNIT_MOVE_MODE);
	ENDSAVE;
};
class lvCReformation	: public lvCOperation
{
public:
	lvCReformation(lvCReformation* pReformation);
	lvCReformation()	{ InfID = _lvCReformation_; TypeForm=0; first=true; UseInNode=true; };
	~lvCReformation()	{};
public:
	int		TypeForm;
	bool	UseInNode;
	int		vGrp;
//	int		Dir;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCReformation);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,TypeForm);
		REG_MEMBER(_bool,UseInNode);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
//		REG_MEMBER(_int,Dir);
	ENDSAVE;
};
class lvCBrigReformation	: public lvCOperation
{
public:
	lvCBrigReformation(lvCBrigReformation* pBrigReformation);
	lvCBrigReformation()	{ InfID = _lvCBrigReformation_; TypeForm=0; };
	~lvCBrigReformation()	{};
public:
	int		GrpID;
	int		TypeForm;
//	int		Dir;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCBrigReformation);
			REG_PARENT(lvCOperation);
			REG_MEMBER(_int,TypeForm);
//			REG_MEMBER(_int,Dir);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,GrpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCChangeFriends	: public lvCOperation
{
public:
	lvCChangeFriends(lvCChangeFriends* pChangeFriends);
	lvCChangeFriends()	{ InfID = _lvCChangeFriends_; Nation=0; Value=1;};
	~lvCChangeFriends()	{};
public:
	int		Nation;
	int		Value;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCChangeFriends);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nation);
		REG_MEMBER(_int,Value);
	ENDSAVE;
};
int ChangeFriends_lua(int nat, int state);
class lvCSetFriends	: public lvCOperation
{
public:
	lvCSetFriends(lvCSetFriends* pSetFriends);
	lvCSetFriends()	{ InfID = _lvCSetFriends_; fstNat=0; secNat=1;};
	~lvCSetFriends()	{};
public:
	int		fstNat;
	int		secNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetFriends);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,fstNat);
		REG_MEMBER(_int,secNat);
	ENDSAVE;
};
int SetFriends_lua(int nat0,int nat1);
class lvCChangeNation	: public lvCOperation
{
public:
	lvCChangeNation(lvCChangeNation* pChangeNation);
	lvCChangeNation()	{ InfID = _lvCChangeNation_; nwNat=0;};
	~lvCChangeNation()	{};
public:
	int		nwNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCChangeNation);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,nwNat);
	ENDSAVE;
};
class lvCChangeNationG	: public lvCOperation
{
public:
	lvCChangeNationG(lvCChangeNationG* pChangeNationG);
	lvCChangeNationG()	{ InfID = _lvCChangeNationG_; nwNat=0;};
	~lvCChangeNationG()	{};
public:
	int		GrpID;
	int		nwNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCChangeNationG);
	REG_PARENT(lvCOperation);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,GrpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
			REG_MEMBER(_int,nwNat);
	ENDSAVE;
};
class lvCSetLightSpot	: public lvCOperation
{
public:
	lvCSetLightSpot(lvCSetLightSpot* pSetLightSpot);
	lvCSetLightSpot()	{ InfID = _lvCSetLightSpot_; Radius=1;};
	~lvCSetLightSpot()	{};
public:
	int		ZoneID;
	int		Radius;
	int		Index;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetLightSpot);
	REG_PARENT(lvCOperation);
	REG_ENUM(_index,ZoneID,ALL_ZONES_ON_MAP);
	REG_MEMBER(_int,Radius);
	REG_MEMBER(_int,Index);
	ENDSAVE;
};
int SetLightSpot_lua(int x,int y,int R,int index);
class lvCClearLightSpot	: public lvCOperation
{
public:
	lvCClearLightSpot(lvCClearLightSpot* pClearLightSpot);
	lvCClearLightSpot()	{ InfID = _lvCClearLightSpot_; };
	~lvCClearLightSpot()	{};
public:
	int		Index;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCClearLightSpot);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_int,Index);
	ENDSAVE;
};
int ClearLightSpot_lua(int index);
class lvCSetStartPoint	: public lvCOperation
{
public:
	lvCSetStartPoint(lvCSetStartPoint* pSetStartPoint);
	lvCSetStartPoint()	{ InfID = _lvCSetStartPoint_; };
	~lvCSetStartPoint()	{};
public:
	bool	Use_VVal;
	int		ZoneID;
	ClassRef<vvINTEGER>	sX;
	ClassRef<vvINTEGER>	sY;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
			DWORD	GetClassMask();
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetStartPoint);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_bool,Use_VVal);
	SAVE_SECTION(0x00000001);
		REG_ENUM(_index,ZoneID,ALL_ZONES_ON_MAP);
	SAVE_SECTION(0x00000002);
		REG_AUTO(sX);
		REG_AUTO(sY);
	ENDSAVE;
};
int SetStartPoint_lua(int x,int y);
class lvCShowVictory	: public lvCOperation
{
public:
	lvCShowVictory(lvCShowVictory* pShowVictory);
	lvCShowVictory()	{ InfID = _lvCShowVictory_; first=true; Nat=0xFF; };
	~lvCShowVictory()	{};
public:
	_str	TextID;
	int		Nat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCShowVictory);
		REG_PARENT(lvCOperation);
		REG_AUTO(TextID);
		REG_MEMBER(_int,Nat);
	ENDSAVE;
};
int ShowVictory_lua(int nat,const char* TextID);
class lvCLooseGame	: public lvCOperation
{
public:
	lvCLooseGame(lvCLooseGame* pLooseGame);
	lvCLooseGame()	{ InfID = _lvCLooseGame_; first=true; Nat=0xFF; };
	~lvCLooseGame()	{};
public:
	_str	TextID;
	int		Nat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCLooseGame);
		REG_PARENT(lvCOperation);
		REG_AUTO(TextID);
		REG_MEMBER(_int,Nat);
	ENDSAVE;
};
int LooseGame_lua(int nat,const char* TextID);
class lvCSetTrigg	: public lvCOperation
{
public:
	lvCSetTrigg(lvCSetTrigg* pSetTrigg);
	lvCSetTrigg()	{ InfID = _lvCSetTrigg_; };
	~lvCSetTrigg()	{};
public:
	int		TID;
	char	TName;
	int		NewVal;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetTrigg);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_int,TID);
	REG_MEMBER(_char,TName);
	REG_MEMBER(_int,NewVal);
	ENDSAVE;
};

class lvCTakeFood	: public lvCOperation
{
public:
	lvCTakeFood(lvCTakeFood* pTakeFood);
	lvCTakeFood()	{ InfID = _lvCTakeFood_; };
	~lvCTakeFood()	{};
public:
	int		GrpID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCTakeFood);
		REG_PARENT(lvCOperation);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,GrpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCTakeWood	: public lvCOperation
{
public:
	lvCTakeWood(lvCTakeWood* pTakeWood);
	lvCTakeWood()	{ InfID = _lvCTakeWood_; };
	~lvCTakeWood()	{};
public:
	int		GrpID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCTakeWood);
		REG_PARENT(lvCOperation);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,GrpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCTakeStone	: public lvCOperation
{
public:
	lvCTakeStone(lvCTakeStone* pTakeStone);
	lvCTakeStone()	{ InfID = _lvCTakeStone_;};
	~lvCTakeStone()	{};
public:
	int		GrpID;
//	char*	GrpName;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCTakeStone);
		REG_PARENT(lvCOperation);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,GrpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSetValue : public lvCOperation
{
public:
	lvCSetValue(lvCSetValue* pSetValue);
	lvCSetValue()	{ InfID = _lvCSetValue_; UseVV=false;};
	~lvCSetValue()	{};

	int						TypeID;
	bool					UseVV;
	ClassRef<vvBASE>		Value;
	ClassRef<vvTRIGER>		ValueTG;
	bool					SetTG;
	ClassRef<vvTRIGER>		vTG;
	ClassRef<vvWORD>		ValueWD;
	DWORD					SetWD;
	ClassRef<vvWORD>		vWD;
	ClassRef<vvINTEGER>		ValueIN;
	int						SetIN;
	ClassRef<vvINTEGER>		vIN;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual bool			AskParentForUsingExpansionClass(char* MemberName,char* ClassName);
			DWORD			GetClassMask();
	virtual	void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetValue);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,TypeID,BE_VALUE_TYPE);
		REG_MEMBER(_bool,UseVV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(ValueTG);
			REG_MEMBER(_bool,SetTG);
		SAVE_SECTION(0x00000002);
			REG_AUTO(ValueWD);
			REG_MEMBER(_DWORD,SetWD);
		SAVE_SECTION(0x00000004);
        	REG_AUTO(ValueIN);
			REG_MEMBER(_int,SetIN);
		SAVE_SECTION(0x00000010);
			REG_AUTO(ValueTG);
			REG_AUTO(vTG);
		SAVE_SECTION(0x00000020);
			REG_AUTO(ValueWD);
			REG_AUTO(vWD);
		SAVE_SECTION(0x00000040);
        	REG_AUTO(ValueIN);
			REG_AUTO(vIN);
		SAVE_SECTION(0x80000000);
			REG_AUTO(Value);
	ENDSAVE;
};
class lvCAddToInt : public lvCOperation
{
public:
	lvCAddToInt(lvCAddToInt* pAddToInt);
	lvCAddToInt()	{ InfID=_lvCAddToInt_; };
	~lvCAddToInt()	{};

	ClassRef<vvINTEGER>	IntValue;
	int					IntAdd;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual	int				Complite();
	virtual	void			GetCopy(lvCOperation** pCopy);

	SAVE(lvCAddToInt);
		REG_PARENT(lvCOperation);
		REG_AUTO(IntValue);
		REG_MEMBER(_int,IntAdd);
	ENDSAVE;
};
class lvCAddToIntEx : public lvCOperation
{
public:
	lvCAddToIntEx(lvCAddToIntEx* pAddToIntEx);
	lvCAddToIntEx()	{ InfID=_lvCAddToIntEx_; };
	~lvCAddToIntEx()	{};

	ClassRef<vvINTEGER>	IntValue;
	int					IntAdd;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual	void			GetCopy(lvCOperation** pCopy);
	
	SAVE(lvCAddToIntEx);
		REG_PARENT(lvCOperation);
		REG_AUTO(IntValue);
		REG_MEMBER(_int,IntAdd);
	ENDSAVE;
};
class lvCKillNUnits	: public lvCOperation
{
public:
	lvCKillNUnits(lvCKillNUnits* pKillNUnits);
	lvCKillNUnits()	{ InfID = _lvCKillNUnits_; };
	~lvCKillNUnits()	{};
public:
	int		GrpID;
	int		UCount;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCKillNUnits);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,UCount);
	ENDSAVE;
};
class lvCEraseNUnits	: public lvCOperation
{
public:
	lvCEraseNUnits(lvCEraseNUnits* pEraseNUnits);
	lvCEraseNUnits()	{ InfID = _lvCEraseNUnits_; };
	~lvCEraseNUnits()	{};
public:
	int		GrpID;
	int		UCount;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCEraseNUnits);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,UCount);
	ENDSAVE;
};
class lvCSavePosition : public lvCOperation
{
public:
	lvCSavePosition(lvCSavePosition* pSavePosition);
	lvCSavePosition()	{ InfID = _lvCSavePosition_; UseVV=false; };
	~lvCSavePosition()	{};
public:
	int		GrpID;
	bool	UseVV;
	ClassRef<vvPOINT_SET>		VVpPos;
	ClassRef<vvPOINT2D>			vvPoint;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
			DWORD	GetClassMask();
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSavePosition);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,UseVV);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
			REG_AUTO(vvPoint);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
			REG_AUTO(VVpPos);
	ENDSAVE;
};
int SavePosition_lua(lvCGroup* pGrp,vvPOINT2D* pPos);
int SavePositionArr_lua(lvCGroup* pGrp,vvPOINT_SET* pPosArr);
class lvCSendToPosition	: public lvCOperation
{
public:
	lvCSendToPosition(lvCSendToPosition* pSendToPosition);
	lvCSendToPosition()	{ InfID = _lvCSendToPosition_;};
	~lvCSendToPosition()	{};
public:
	int	parGrp;
	ClassRef<vvPOINT_SET>		VVpPos;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSendToPosition);
		REG_PARENT(lvCOperation);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
			REG_AUTO(VVpPos);
	ENDSAVE;
};
class lvCSetRessource : public lvCOperation
{
public:
	lvCSetRessource(lvCSetRessource* pSetRessource);
	lvCSetRessource()	{ InfID = _lvCSetRessource_; Nation=Food=Wood=Stown=Gold=Coal=Iron=0; UseVV=false; };
	~lvCSetRessource()	{};
public:
	int		Nation;
	bool	UseVV;
	int		Food;			ClassRef<vvINTEGER>		FoodVV;
	int		Wood;			ClassRef<vvINTEGER>		WoodVV;
	int		Stown;			ClassRef<vvINTEGER>		StownVV;
	int		Gold;			ClassRef<vvINTEGER>		GoldVV;
	int		Iron;			ClassRef<vvINTEGER>		IronVV;
	int		Coal;			ClassRef<vvINTEGER>		CoalVV;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
			DWORD	GetClassMask();
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetRessource);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,UseVV);
		REG_MEMBER(_int,Nation);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,Food);
			REG_MEMBER(_int,Wood);
			REG_MEMBER(_int,Stown);
			REG_MEMBER(_int,Gold);
			REG_MEMBER(_int,Iron);
			REG_MEMBER(_int,Coal);
		SAVE_SECTION(0x00000001);
			REG_AUTO(FoodVV);
			REG_AUTO(WoodVV);
			REG_AUTO(StownVV);
			REG_AUTO(GoldVV);
			REG_AUTO(IronVV);
			REG_AUTO(CoalVV);
	ENDSAVE;
};
int SetResource_lua(int nat,int resID,int Amount);
class lvCAddRessource : public lvCOperation
{
public:
	lvCAddRessource(lvCAddRessource* pAddRessource);
	lvCAddRessource()	{ InfID=_lvCAddRessource_; };
	~lvCAddRessource()	{};

	int					RessType;
	int					Nat;
	bool				Use_VV;
	int					Value;
	ClassRef<vvINTEGER>	vValue;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
					DWORD	GetClassMask(){
						if (Use_VV)		return 0x00000002;
										return 0x00000001;
					};
	virtual	void	GetCopy(lvCOperation** pCopy);
	
	SAVE(lvCAddRessource);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_ENUM(_index,RessType,RESTYPE);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_MEMBER(_int,Value);
		SAVE_SECTION(0x00000002);
			REG_AUTO(vValue);
	ENDSAVE;
};
int AddRessource_lua(int nat,int resID,int Amount);
class lvCStartAIEx : public lvCOperation
{
public:
	lvCStartAIEx(lvCStartAIEx* pStartAIEx);
	lvCStartAIEx()	{ InfID = _lvCStartAIEx_; NameXML=""; Nation=Land=Money=ResOnMap=Difficulty=0; Use_VV=false; };
	~lvCStartAIEx()	{};
public:
	int		Nation;
	_str	NameXML;
	int		Land;
	int		Money;
	int		ResOnMap;
	bool	Use_VV;
	ClassRef<vvINTEGER>	vDifficulty;
	int		Difficulty;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
			DWORD	GetClassMask();
	virtual	void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCStartAIEx);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nation);
		REG_AUTO(NameXML);
		REG_MEMBER(_int,Land);
		REG_MEMBER(_int,Money);
		REG_MEMBER(_int,ResOnMap);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(vDifficulty);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,Difficulty);
	ENDSAVE;
};
int ActivateTacticalAI_lua(int nat);
int StartAIEx_lua(int nat,const char* file,int lend,int mony,int res,int diff);
class lvCSetAIEnableState : public lvCOperation
{
public:
	lvCSetAIEnableState(lvCSetAIEnableState* pSetAIEnableState);
	lvCSetAIEnableState()	{ InfID=_lvCSetAIEnableState_; };
	~lvCSetAIEnableState()	{};

	int		Nat;
	bool	State;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetAIEnableState);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_MEMBER(_bool,State);
	ENDSAVE;
};
int SetAIEnableState_lua(int nat,bool state);
class lvCShowDialog : public lvCOperation
{
public:
	lvCShowDialog(lvCShowDialog* pShowDialog);
	lvCShowDialog()		{ InfID = _lvCShowDialog_; };
	~lvCShowDialog()	{};

	ClassRef<vvDIALOG>	Dialog;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCShowDialog);
		REG_PARENT(lvCOperation);
		REG_AUTO(Dialog);
	ENDSAVE;
};
int ShowDialog_lua(vvDIALOG* pDLG);
class lvCAddTextToDlg : public lvCOperation
{
public:
	lvCAddTextToDlg(lvCAddTextToDlg* pAddTextToDlg);
	lvCAddTextToDlg()	{ InfID = _lvCAddTextToDlg_; };	
	~lvCAddTextToDlg()	{};

	ClassRef<vvDIALOG>	Dialog;
	ClassRef<vvTEXT>	Text;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCAddTextToDlg);
		REG_PARENT(lvCOperation);
		REG_AUTO(Dialog);
		REG_AUTO(Text);
	ENDSAVE;
};
int AddTextToDlg_lua(vvDIALOG* pDLG,vvTEXT* pTXT);
class lvCClearDialog : public lvCOperation
{
public:
	lvCClearDialog(lvCClearDialog* pClearDialog);
	lvCClearDialog()	{ InfID = _lvCClearDialog_; };
	~lvCClearDialog()	{};

	ClassRef<vvDIALOG>	Dialog;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCClearDialog);
		REG_PARENT(lvCOperation);
		REG_AUTO(Dialog);
	ENDSAVE;
};
int ClearDialog_lua(vvDIALOG* pDLG);
class lvCSetScrollLimit : public lvCOperation
{
public:
	lvCSetScrollLimit(lvCSetScrollLimit* pSetScrollLimit);
	lvCSetScrollLimit()		{ InfID = _lvCSetScrollLimit_; LockAroud=true; };
	~lvCSetScrollLimit()	{};

	ClassRef<vvPOINT2D>		PosLT;
	ClassRef<vvPOINT2D>		PosRB;
	bool					LockAroud;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetScrollLimit);
		REG_PARENT(lvCOperation);
		REG_AUTO(PosLT);
		REG_AUTO(PosRB);
		REG_MEMBER(_bool,LockAroud);
	ENDSAVE;
};
int SetScrollLimit_lua(vvPOINT2D* pPosLT,vvPOINT2D* pPosRB,bool LockAroud);
class lvTypeNum : public BaseClass{
public:
	lvTypeNum(lvTypeNum* pTypeNum);
	lvTypeNum()	{ InfID = _lvTypeNum_; UnitType=0; };
	~lvTypeNum(){};

	DWORD	InfID;
	int		vgUnitsID;		// Группа в которую строим юнитов
	int		UnitType;
	bool	Use_VV;
	int		Num;
	ClassRef<vvINTEGER>	vNum;

	DWORD	GetClassMask(){
		if (Use_VV)	return 0x00000001;
					return 0x00000002;
	};
	bool	Prepare();
	virtual	void	GetCopy(lvTypeNum** pCopy);

	SAVE(lvTypeNum);
		REG_ENUM(_index,vgUnitsID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_UnitType,UnitType);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(vNum);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,Num);
	ENDSAVE;
};
class lvCSpotNUnits : public lvCOperation
{
public:
	lvCSpotNUnits(lvCSpotNUnits* pSpotNUnits);
	lvCSpotNUnits()		{ InfID = _lvCSpotNUnits_; Status = 0; UGRP_ID=0xFFFF; };
	~lvCSpotNUnits()	{};

	int						vgBildsID;		// Здания преднозначенные для постройки юнитов (ОДНО ЗДАНИЕ!!!)
	int						aZDestPoint;	// Зона в которую строим юнитов
	bool					Use_VV;
	int						ProduceTime;	// Врея постройки юнита.
	ClassRef<vvINTEGER>		vProduceTime;
	ClonesArray<lvTypeNum>	TypeNum;		// Тип юнита - кол-во

	UnitsGroup*				pUnitsGRP;		// Группа в которую строим юнитов.
	int						UGRP_ID;
    int						LastProdTime;	// Время последнего заказа юнита.
	int						Status;
	int						TypePID;

	DWORD	GetClassMask(){
		if (Use_VV)	return 0x00000001;
					return 0x00000002;
	};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual	void			GetCopy(lvCOperation** pCopy);

    		void			Create();

	SAVE(lvCSpotNUnits);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vgBildsID,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,aZDestPoint,ALL_ZONES_ON_MAP);
		REG_AUTO(TypeNum);
		REG_MEMBER(_int,UGRP_ID);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(vProduceTime);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,ProduceTime);
	ENDSAVE;
};
class lvGrpNumBld : public BaseClass
{
public:
	lvGrpNumBld(lvGrpNumBld* pGrpNumBld);
	lvGrpNumBld()	{ InfID=_lvGrpNumBld_; Diff=0; RemoveAfterSend=true; };
	~lvGrpNumBld()	{};

	DWORD	InfID;
	ClassArray<lvCCondition>	Cond;		// Условие
	int							vgUnits;	// Группа на отправку в бой.
	int							vgBildg;	// Здание в котором строятся войска.
	bool						Use_VV;
	int							Num;		// Сколько юнитов для отправки в бой должно быть в группе.
	ClassRef<vvINTEGER>			vNum;
	int							AttackerType;
	int							Diff;
	bool						RemoveAfterSend;

	void						Process(int time);
	bool						Prepare();
	virtual	void				GetCopy(lvGrpNumBld** pCopy);

	DWORD	GetClassMask(){
		if (Use_VV)	return 0x00000001;
					return 0x00000002;
	};

	SAVE(lvGrpNumBld);
		REG_AUTO(Cond);
		REG_ENUM(_index,vgUnits,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,vgBildg,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,AttackerType,BE_ATTACKER_TYPE);
		REG_MEMBER(_bool,RemoveAfterSend);
		REG_MEMBER(_int,Diff);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(vNum);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,Num);
	ENDSAVE;
};

class lvCGoInBattle : public lvCOperation
{
public:
	lvCGoInBattle(lvCGoInBattle* pGoInBattle);
	lvCGoInBattle()		{ InfID = _lvCGoInBattle_; };
	~lvCGoInBattle()	{};

	ClonesArray<lvGrpNumBld>	GrpNumBld;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual	void			GetCopy(lvCOperation** pCopy);

	SAVE(lvCGoInBattle);
		REG_PARENT(lvCOperation);
		REG_AUTO(GrpNumBld);
	ENDSAVE;
};
class lvCArtAttack : public lvCOperation
{
public:
	lvCArtAttack(lvCArtAttack* pArtAttack);
	lvCArtAttack()	{ InfID=_lvCArtAttack_; AttackImid=true; };
	~lvCArtAttack()	{};

	int					vgArtID;
	int					vgTargID;
	bool				AttackImid;
	ClassRef<vvTRIGER>	AttackActive;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual	int				Complite();
	virtual	void			GetCopy(lvCOperation** pCopy);

	SAVE(lvCArtAttack);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vgArtID,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,vgTargID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,AttackImid);
		REG_AUTO(AttackActive);
	ENDSAVE;
};
class lvCPutNewSquad : public lvCOperation
{
public:
	lvCPutNewSquad(lvCPutNewSquad* pPutNewSquad);
	lvCPutNewSquad()	{ InfID=_lvCPutNewSquad_; FormT=0; };
	~lvCPutNewSquad()	{};

	int		GrpID;
	int		Nat;
	int		UnitType;
	int		SizeType;
	int		FormT;
	bool	Use_Zone;
	int		x;
	int		y;
	int		dir;
	int		ZoneID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int				Process(int time);
	virtual	void			GetCopy(lvCOperation** pCopy);

	DWORD	GetClassMask(){
		if (Use_Zone)	return 0x00000002;
						return 0x00000001;
	};

	SAVE(lvCPutNewSquad);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,Nat);
		REG_MEMBER(_UnitType,UnitType);
		REG_MEMBER(_int,SizeType);
		REG_MEMBER(_int,dir);
		REG_MEMBER(_bool,Use_Zone);
		SAVE_SECTION(0x00000001);
			REG_MEMBER(_int,x);
			REG_MEMBER(_int,y);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,ZoneID,ALL_ZONES_ON_MAP);
	ENDSAVE;
};
int GetUTypeByName_lua(const char* UTName);
int PutNewSquad_lua(lvCGroup* pGRP,int nat,int uType,int size,int x,int y,int dir);
class lvCPutNewFormation : public lvCOperation
{
public:
	lvCPutNewFormation(lvCPutNewFormation* pPutNewFormation);
	lvCPutNewFormation()	{ InfID=_lvCPutNewFormation_; Nat=UType=0; };
	~lvCPutNewFormation()	{};

	int						vGrpID;
	int						Nat;
	int						Form;
	int						UType;
	int						dir;
	bool					Use_Zone;
	int						ZoneID;
	ClassRef<vvPOINT2D>		Point;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual	void			GetCopy(lvCOperation** pCopy);

					DWORD	GetClassMask(){
								if (Use_Zone)	return 0x00000001;
												return 0x00000002;
					};

    SAVE(lvCPutNewFormation);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,Nat);
		REG_ENUM(_index,Form,ALL_FORMATIONS);
		REG_MEMBER(_UnitType,UType);
		REG_MEMBER(_int,dir);
		REG_MEMBER(_bool,Use_Zone);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,ZoneID,ALL_ZONES_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_AUTO(Point);
	ENDSAVE;
};
int GetFormationID_lua(const char* formID);
int PutNewFormation_lua(lvCGroup* pGRP,int nat,int uType,int form,int x,int y,int dir);
class lvCSetUnitStateCII	: public lvCOperation
{
public:
	lvCSetUnitStateCII(lvCSetUnitStateCII* pSetUnitStateCII);
	lvCSetUnitStateCII()	{ InfID = _lvCSetUnitStateCII_; LineI=LineII=LineIII=false; SetSG_Immediately=false; };
	~lvCSetUnitStateCII()	{};
public:
	int		GrpID;
	bool	Fire;		// Разрешить стрельбу. 
	bool	LineI;		// Разрешить стрельбу первой  линии.
	bool	LineII;		// Разрешить стрельбу второй  линии.
	bool	LineIII;	// Разрешить стрельбу третьей линии.
	bool	Stiki;		// Опустить, поднять штыки - только если запрещенно стрелять.
	bool	SetSG_Immediately;	
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	void	GetCopy(lvCOperation** pCopy);
	DWORD	GetClassMask(){
		if (Fire)	return 0x00000001;
					return 0x00000002;
	};

public:
	SAVE(lvCSetUnitStateCII);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,SetSG_Immediately);
		REG_MEMBER(_bool,Fire);
		SAVE_SECTION(0x00000001);
			REG_MEMBER(_bool,LineI);
			REG_MEMBER(_bool,LineII);
			REG_MEMBER(_bool,LineIII);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_bool,Stiki);
	ENDSAVE;
};
int SetUnitStateCII_lua(lvCGroup* pGrp,bool shtiki,bool Fire,bool LI,bool LII,bool LIII);
class lvCSendStikiToZone : public lvCOperation
{
public:
	lvCSendStikiToZone(lvCSendStikiToZone* plvCSendStikiToZone);
	lvCSendStikiToZone()	{ InfID = _lvCSendStikiToZone_; };
	~lvCSendStikiToZone()	{};

	int		GrpID;
	int		ZoneID;
	int		dir;
	int		prio;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSendStikiToZone);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,ZoneID,ALL_ZONES_ON_MAP);
		REG_MEMBER(_int,dir);
		REG_MEMBER(_int,prio);
	ENDSAVE;
};

class lvCSetUnitEnableState : public lvCOperation
{
public:
	lvCSetUnitEnableState(lvCSetUnitEnableState* pSetUnitEnableState);
	lvCSetUnitEnableState()		{ InfID=_lvCSetUnitEnableState_; Nat=0; TypeID=0; State=1; };
	~lvCSetUnitEnableState()	{};

	int		Nat;
	int		TypeID;
	int		State;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetUnitEnableState);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_MEMBER(_int,State);
		REG_MEMBER(_UnitType,TypeID);
	ENDSAVE;
};

class lvCSetUpgradeEnableStatus : public lvCOperation
{
public:
	lvCSetUpgradeEnableStatus(lvCSetUpgradeEnableStatus* pSetUpgradeEnableStatus);
	lvCSetUpgradeEnableStatus()		{ InfID=_lvCSetUpgradeEnableStatus_; Nat=0; UpgradeID=0; State=1; };
	~lvCSetUpgradeEnableStatus()	{};

	int	Nat;
	int UpgradeID;
	int	State;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetUpgradeEnableStatus);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_MEMBER(_int,State);
		REG_ENUM(_index,UpgradeID,ALL_UPGRADES);
	ENDSAVE;
};

class lvCSetUpgradeDone : public lvCOperation
{
public:
	lvCSetUpgradeDone(lvCSetUpgradeDone* pSetUpgradeDone);
	lvCSetUpgradeDone()		{ InfID=_lvCSetUpgradeDone_; Nat=0; };
	~lvCSetUpgradeDone()	{};

	int	Nat;
	int GrpID;
	int	UpgradeID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetUpgradeDone);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,UpgradeID,ALL_UPGRADES);
	ENDSAVE;
};

class lvCTeleport : public lvCOperation
{
public:
	lvCTeleport(lvCTeleport* pTeleport);
	lvCTeleport()	{ InfID=_lvCTeleport_; Direction=512; dX=dY=100; };
	~lvCTeleport()	{};
	
	int					vGrpID;		// Телкпортируемая группа.
	int					Direction;	// Направление после телепорта (512 - не разворачивать)
	int					dX,dY;
    bool				Use_VVal;	
	int					ZoneID;
	ClassRef<vvPOINT2D>	Point;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
					DWORD	GetClassMask(){
						if (Use_VVal)	return 0x00000002;
										return 0x00000001;
					};

	SAVE(lvCTeleport);	
		REG_PARENT(lvCOperation);
        REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,Direction);
		REG_MEMBER(_int,dX);
		REG_MEMBER(_int,dY);
		REG_MEMBER(_bool,Use_VVal);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,ZoneID,ALL_ZONES_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_AUTO(Point);
	ENDSAVE;
};

class lvCDisband : public lvCOperation
{
public:
	lvCDisband(lvCDisband* pDisband);
	lvCDisband()	{ InfID=_lvCDisband_; };
	~lvCDisband()	{};

	int		vGrpID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCDisband);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};

class lvCScare : public lvCOperation
{
public:
	lvCScare(lvCScare* pScare);
	lvCScare()		{ InfID=_lvCScare_; };
	~lvCScare()		{};

	int		vGrpID;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
	
	SAVE(lvCScare);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};

class lvCClearSG : public lvCOperation
{
public:
	lvCClearSG(lvCClearSG* pClearSG);
	lvCClearSG()		{ InfID=_lvCClearSG_; };
	~lvCClearSG()		{};

	int		vGrpID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCClearSG);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCUnloadSquad : public lvCOperation
{
public:
	lvCUnloadSquad(){InfID=_lvCUnloadSquad_;};
	lvCUnloadSquad(lvCUnloadSquad* pUnloadSquad);
	~lvCUnloadSquad(){};

	int vGRP;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCUnloadSquad);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGRP,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSpotGrpByUType : public lvCOperation
{
public:
	lvCSpotGrpByUType()		{ InfID = _lvCSpotGrpByUType_; };
	lvCSpotGrpByUType(lvCSpotGrpByUType* pSpotGrpByUType);
	~lvCSpotGrpByUType()	{};

	int		vGrpID;
	int		UnitType;
	int		Nat;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSpotGrpByUType);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_UnitType,UnitType);
	ENDSAVE;
};
class lvCRemoveNUnitsTo : public lvCOperation
{
public:
	lvCRemoveNUnitsTo()		{ InfID=_lvCRemoveNUnitsTo_; N=0; };
	lvCRemoveNUnitsTo(lvCRemoveNUnitsTo* pRemoveNUnitsTo);
	~lvCRemoveNUnitsTo()	{};

	int	vGrpSource;
	int vGrpDest;
	int	N;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCRemoveNUnitsTo);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpSource,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,vGrpDest,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,N);
	ENDSAVE;
};

class lvCTeraforming;
class lvCApplyTerafoming : public lvCOperation
{
public:
	lvCApplyTerafoming()	{ InfID=_lvCApplyTerafoming_; LastUpdateTime=0; xV=yV=rV=RV=hV=HV=0.f; x0=x1=y1=y0; };
	lvCApplyTerafoming(lvCApplyTerafoming* pApplyTerafoming);
	~lvCApplyTerafoming()	{};

	bool									visible;
	DWORD									color;
	ClonesArray< ClassRef<lvCTeraforming> >	TerraPointsArr;
	int										MinStep;
	int										MinFromDest;

	bool									Use_VV;
	int										Speed;		// Pixels per sec (1pixel==1000) by max distance
	ClassRef<vvINTEGER>						vSpeed;		// Pixels per sec (1pixel==1000) by max distance
	ClassRef<vvINTEGER>						vNOfComplitePoints;	// Кол-во пройденных точек.

	int	x0,y0,x1,y1;									// For update all ways

	DWORD									LastUpdateTime;
	float									xV;
	float									yV;
	float									rV;
	float									RV;
	float									hV;
	float									HV;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
	virtual			void	Draw();
	virtual			void	SetSpeed(int _speed);

					bool			TestIndexPos			(int index);
					lvCTeraforming*	GetPossition			(int index);
					float			Get_Speed				();
					float			Get_DT					(int p0=0,int p1=1);
					float			Get_DS					(int p0=0,int p1=1);
					float			Get_Dx					(int p0=0,int p1=1);
					float			Get_Dy					(int p0=0,int p1=1);
					float			Get_Dr					(int p0=0,int p1=1);
					float			Get_DR					(int p0=0,int p1=1);
					float			Get_Dh					(int p0=0,int p1=1);
					float			Get_DH					(int p0=0,int p1=1);
					float			Get_MaxShift			(float dt,int p0=0,int p1=1);
					float			Get_MaxDist				(int p0=0,int p1=1);
					void			Set_xV					(int p0=0,int p1=1);
					void			Set_yV					(int p0=0,int p1=1);
					void			Set_rV					(int p0=0,int p1=1);
					void			Set_RV					(int p0=0,int p1=1);
					void			Set_hV					(int p0=0,int p1=1);
					void			Set_HV					(int p0=0,int p1=1);

	SAVE(lvCApplyTerafoming);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,visible);
		REG_MEMBER(_color,color);
		REG_AUTO(TerraPointsArr);
		REG_MEMBER(_int,MinStep);
		REG_MEMBER(_int,MinFromDest);
		REG_MEMBER(_bool,Use_VV);
		REG_MEMBER(_int,Speed);
		REG_AUTO(vSpeed);
		REG_MEMBER(_int,x0);
		REG_MEMBER(_int,x1);
		REG_MEMBER(_int,y0);
		REG_MEMBER(_int,y1);
		REG_AUTO(vNOfComplitePoints);
	ENDSAVE;
};

class lvCSetMyNation : public lvCOperation
{
public:
	lvCSetMyNation()	{ InfID=_lvCSetMyNation_; Nat=0; };	
	lvCSetMyNation(lvCSetMyNation* pSetMyNation);
	~lvCSetMyNation()	{};

	int Nat;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetMyNation);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
	ENDSAVE;
};

class lvCEqualizeSpeed : public lvCOperation
{
public:
	lvCEqualizeSpeed()	{ InfID=_lvCEqualizeSpeed_; };
	lvCEqualizeSpeed(lvCEqualizeSpeed* pEqualizeSpeed);
	~lvCEqualizeSpeed()	{};
    
	int		vGrp0;
	int		vGrp1;

	int		EqType;	// Тип усреднения

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCEqualizeSpeed);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp0,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,vGrp1,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,EqType,BE_ASTIMATE_TYPE);
	ENDSAVE;
};
class lvCCreateBrigade : public lvCOperation
{
public:
	lvCCreateBrigade()	{ InfID=_lvCCreateBrigade_; iSize=-1; };
	lvCCreateBrigade(lvCCreateBrigade* pCreateBrigade);
	~lvCCreateBrigade()	{  };

	int					vGrp;
	int					vBrigGrp;
	bool				Use_VV;
	int					iSize;
	ClassRef<vvINTEGER>	vSize;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

					DWORD	GetClassMask(){
								if (Use_VV)	return 0x00000002;
								return 0x00000001;
							};

					int		GetSize();


	SAVE(lvCCreateBrigade);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,vBrigGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_MEMBER(_int,iSize);
		SAVE_SECTION(0x00000002);
			REG_AUTO(vSize);
	ENDSAVE;
};
class lvCAddWallSegment : public lvCOperation
{
public:
	lvCAddWallSegment()		{ InfID=_lvCAddWallSegment_; };
	lvCAddWallSegment(lvCAddWallSegment* pAddWallSegment);
	~lvCAddWallSegment()	{}; 

	int					Nat;
	int					Type;
	ClassRef<vvPOINT2D>	BegPos;
	ClassRef<vvPOINT2D>	EndPos;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCAddWallSegment);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_MEMBER(_int,Type);
		REG_AUTO(BegPos);
		REG_AUTO(EndPos);
	ENDSAVE;
};
class lvCAddFarms : public lvCOperation
{
public:
	lvCAddFarms()	{ InfID=_lvCAddFarms_; addFarms=0; setFarms=-1; };
	lvCAddFarms(lvCAddFarms* pAddFarms);
	~lvCAddFarms()	{};

	
	int		Nat;
	int		addFarms;
	int		setFarms;	// if ==-1 do nothing

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCAddFarms);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Nat);
		REG_MEMBER(_int,addFarms);
		REG_MEMBER(_int,setFarms);
	ENDSAVE;
};
class lvCSetSerchWFlag : public lvCOperation
{
public:
	lvCSetSerchWFlag()	{ InfID=_lvCSetSerchWFlag_; };
	lvCSetSerchWFlag(lvCSetSerchWFlag* pSetSerchWFlag);
	~lvCSetSerchWFlag() {};

	int		vGrp;
	bool	SearchWict;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetSerchWFlag);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,SearchWict);
	ENDSAVE;
};
class lvCClearOrders : public lvCOperation
{
public:
	lvCClearOrders(){ InfID=_lvCClearOrders_; };
	lvCClearOrders(lvCClearOrders* pClearOrders);
	~lvCClearOrders(){};

	int vGrp;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCClearOrders);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCClearDead : public lvCOperation
{
public:
	lvCClearDead(){ InfID=_lvCClearDead_; };
	lvCClearDead(lvCClearDead* pClearDead);
	~lvCClearDead(){};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCClearDead);
		REG_PARENT(lvCOperation);
	ENDSAVE;
};
class lvCGroupMovement : public lvCOperation
{
public:
	lvCGroupMovement(){ InfID=_lvCGroupMovement_; };
	lvCGroupMovement(lvCGroupMovement* pGroupMovement);
	~lvCGroupMovement(){};

	int MovementState;
	int vGrp;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCGroupMovement);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,MovementState,BE_NODE_ACTION);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSetTired : public lvCOperation
{
public:
	lvCSetTired(){InfID=_lvCSetTired_;SetedTiredValue=1000;};
	lvCSetTired(lvCSetTired* pSetTired);
	~lvCSetTired(){};
	int vGrp;
	int SetedTiredValue;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetTired);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,SetedTiredValue);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
int	SetTired_lua(lvCGroup* pGRP,int VAL);
//======================================================================//
//=================	 OPERATION FOR TRANSPORT	 =======================//
//======================================================================//
class lvCPushNUnitAway : public lvCOperation
{
public:
	lvCPushNUnitAway()	{ InfID=_lvCPushNUnitAway_; N=0; };
	lvCPushNUnitAway(lvCPushNUnitAway* pPushNUnitAway);
	~lvCPushNUnitAway()	{};

	int		vGrpID;
	int		N;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCPushNUnitAway);
		REG_PARENT(lvCOperation);
        REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,N);
	ENDSAVE;
};
class lvCSendUnitsToTransport : public lvCOperation
{
public:
	lvCSendUnitsToTransport()	{ InfID=_lvCSendUnitsToTransport_; };
	lvCSendUnitsToTransport(lvCSendUnitsToTransport* pSendUnitsToTransport);
	~lvCSendUnitsToTransport()	{};

	int vGrpTransID;	// Транспорты
	int vGrpUnitsID;	// Юниты

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSendUnitsToTransport);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpTransID,ALL_vGROUPS_ON_MAP);
		REG_ENUM(_index,vGrpUnitsID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
//////////////////////////////////////////////////////////////////////////

//======================================================================//
//=================		OPERATION FOR FILM		========================//
//======================================================================//
class lvCSetFGV : public lvCOperation
{
public:
	lvCSetFGV(lvCSetFGV* pSetFGV);
	lvCSetFGV()		{ InfID=_lvCSetFGV_; State=false; Immediately=false; };
	~lvCSetFGV()	{};
public:
	ClassRef<lvCGraphObject>	GraphObj;
	ClassRef<vvBASE>			newGraphObj;
	bool						State;
	bool						Immediately;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetFGV);
		REG_PARENT(lvCOperation);
		REG_AUTO(GraphObj);
		REG_AUTO(newGraphObj);
		REG_MEMBER(_bool,State);
		REG_MEMBER(_bool,Immediately);
	ENDSAVE;
};

class lvCPlayOGMiss : public lvCOperation
{
public:
	lvCPlayOGMiss(lvCPlayOGMiss* pPlayOGMiss);
	lvCPlayOGMiss()		{ InfID=_lvCPlayOGMiss_; pStream=0; pCyclic=false; };
	~lvCPlayOGMiss()	{};
public:
	_str		pFileName;
	DWORD		pStream;
	bool		pCyclic;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCPlayOGMiss);
		REG_PARENT(lvCOperation);
		REG_FILEPATH(pFileName,".ogg");
		REG_MEMBER(_DWORD,pStream);
		REG_MEMBER(_bool,pCyclic);
	ENDSAVE;
};
class lvCStopOGMiss : public lvCOperation
{
public:
	lvCStopOGMiss(lvCStopOGMiss* pStopOGMiss);
	lvCStopOGMiss()		{ InfID=_lvCStopOGMiss_; pStream=0;};
	~lvCStopOGMiss()	{};
public:
	DWORD		pStream;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCStopOGMiss);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_DWORD,pStream);
	ENDSAVE;
};
class lvCOGSetVolume : public lvCOperation
{
public:
	lvCOGSetVolume(lvCOGSetVolume* pOGSetVolume);
	lvCOGSetVolume()		{ InfID=_lvCOGSetVolume_; pStream=0;};
	~lvCOGSetVolume()	{};
public:
	int			Volume;
	DWORD		pStream;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCOGSetVolume);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_int,Volume);
	REG_MEMBER(_DWORD,pStream);
	ENDSAVE;
};
class lvCOGFinishMiss : public lvCOperation
{
public:
	lvCOGFinishMiss(lvCOGFinishMiss* pOGFinishMiss);
	lvCOGFinishMiss()		{ InfID=_lvCOGFinishMiss_; pStream=0;};
	~lvCOGFinishMiss()	{};
public:
	DWORD		pStream;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCOGFinishMiss);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_DWORD,pStream);
	ENDSAVE;
};
class lvCSetPlayGameMode : public lvCOperation
{
public:
	lvCSetPlayGameMode(lvCSetPlayGameMode* pSetPlayGameMode);
	lvCSetPlayGameMode()		{ InfID=_lvCSetPlayGameMode_; };
	~lvCSetPlayGameMode()	{};
public:
	int		par;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetPlayGameMode);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,par);
	ENDSAVE;
};
class lvCFreezeGame : public lvCOperation
{
public:
	lvCFreezeGame(lvCFreezeGame* pFreezeGame);
	lvCFreezeGame()		{ InfID=_lvCFreezeGame_; };
	~lvCFreezeGame()	{};
public:
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCFreezeGame);
	REG_PARENT(lvCOperation);
	ENDSAVE;
};
class lvCUnFreezeGame : public lvCOperation
{
public:
	lvCUnFreezeGame(lvCUnFreezeGame* pUnFreezeGame);
	lvCUnFreezeGame()		{ InfID=_lvCUnFreezeGame_; };
	~lvCUnFreezeGame()	{};
public:
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCUnFreezeGame);
	REG_PARENT(lvCOperation);
	ENDSAVE;
};
class lvCUnFreezeGroup : public lvCOperation
{
public:
	lvCUnFreezeGroup(lvCUnFreezeGroup* pUnFreezeGroup);
	lvCUnFreezeGroup()		{ InfID=_lvCUnFreezeGroup_; };
	~lvCUnFreezeGroup()	{};
public:
	int			GrpID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCUnFreezeGroup);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCFreezeAndHidden : public lvCOperation
{
public:
	lvCFreezeAndHidden(lvCFreezeAndHidden* pFreezeAndHidden);
	lvCFreezeAndHidden()		{ InfID=_lvCFreezeAndHidden_; bHome=false;};
	~lvCFreezeAndHidden()	{};
public:
	bool		bHome;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCFreezeAndHidden);
	REG_PARENT(lvCOperation);
	REG_MEMBER(_bool,bHome);
	ENDSAVE;
};
class lvCFreezeAndHiddenGame : public lvCOperation
{
public:
	lvCFreezeAndHiddenGame(lvCFreezeAndHiddenGame* pFreezeAndHiddenGame);
	lvCFreezeAndHiddenGame()		{ InfID=_lvCFreezeAndHiddenGame_; bHome=false;};
	~lvCFreezeAndHiddenGame()	{};
public:
	int			pZone;
	bool		bHome;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCFreezeAndHiddenGame);
	REG_PARENT(lvCOperation);
	REG_ENUM(_index,pZone,ALL_ZONES_ON_MAP);
	REG_MEMBER(_bool,bHome);
	ENDSAVE;
};
class lvCUnFreezeAndUnHidden : public lvCOperation
{
public:
	lvCUnFreezeAndUnHidden(lvCUnFreezeAndUnHidden* pUnFreezeAndUnHidden);
	lvCUnFreezeAndUnHidden()		{ InfID=_lvCUnFreezeAndUnHidden_;};
	~lvCUnFreezeAndUnHidden()	{};
public:
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCUnFreezeAndUnHidden);
	REG_PARENT(lvCOperation);
	ENDSAVE;
};
class lvCUnFreezeAndUnHiddenGame : public lvCOperation
{
public:
	lvCUnFreezeAndUnHiddenGame(lvCUnFreezeAndUnHiddenGame* pUnFreezeAndUnHiddenGame);
	lvCUnFreezeAndUnHiddenGame()		{ InfID=_lvCUnFreezeAndUnHiddenGame_;};
	~lvCUnFreezeAndUnHiddenGame()	{};
public:
	int			pZone;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCUnFreezeAndUnHiddenGame);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,pZone,ALL_ZONES_ON_MAP);
	ENDSAVE;
};
class lvCUnFreezeAndUnHiddenGroup : public lvCOperation
{
public:
	lvCUnFreezeAndUnHiddenGroup(lvCUnFreezeAndUnHiddenGroup* pUnFreezeAndUnHiddenGroup);
	lvCUnFreezeAndUnHiddenGroup()		{ InfID=_lvCUnFreezeAndUnHiddenGroup_;};
	~lvCUnFreezeAndUnHiddenGroup()	{};
public:
	int			GrpID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCUnFreezeAndUnHiddenGroup);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCSetLeftPort : public lvCOperation
{
public:
	lvCSetLeftPort(lvCSetLeftPort* pSetLeftPort);
	lvCSetLeftPort()	{ InfID = _lvCSetLeftPort_; FileID=0xFFFF; };
	~lvCSetLeftPort()	{};
public:
	ClassRef<lvCDeffFilmMenu>	MENU;
	word						FileID;
	int							SpriteID;
	ClassRef<vvTEXT>			HName;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetLeftPort);
		REG_PARENT(lvCOperation);
		REG_AUTO(MENU);
		REG_MEMBER(_gpfile,FileID);
		REG_SPRITE(SpriteID,FileID);
		REG_AUTO(HName);
	ENDSAVE;
};
class lvCSetRightPort : public lvCOperation
{
public:
	lvCSetRightPort(lvCSetRightPort* pSetRightPort);
	lvCSetRightPort()	{ InfID = _lvCSetRightPort_; };
	~lvCSetRightPort()	{};
public:
	ClassRef<lvCDeffFilmMenu>	MENU;
	word						FileID;
	int							SpriteID;
	ClassRef<vvTEXT>			HName;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetRightPort);
		REG_PARENT(lvCOperation);
		REG_AUTO(MENU);
		REG_MEMBER(_gpfile,FileID);
		REG_SPRITE(SpriteID,FileID);
		REG_AUTO(HName);
	ENDSAVE;
};
class lvCPlayText : public lvCOperation
{
public:
	lvCPlayText(lvCPlayText* pPlayText);
	lvCPlayText()	{ InfID = _lvCPlayText_; };
	~lvCPlayText()	{};
public:
	ClassRef<lvCDeffFilmMenu>	MENU;
	int							canal;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCPlayText);
		REG_PARENT(lvCOperation);
		REG_AUTO(MENU);
		REG_MEMBER(_int,canal);
	ENDSAVE;
};
class lvCSetText : public lvCOperation
{
public:
	lvCSetText(lvCSetText* pSetText);
	lvCSetText()	{ InfID = _lvCSetText_; };
	~lvCSetText()	{};
public:
	ClassRef<lvCDeffFilmMenu>	MENU;
	ClassRef<vvTEXT>			TEXT;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetText);
		REG_PARENT(lvCOperation);
		REG_AUTO(MENU);
		REG_AUTO(TEXT);
	ENDSAVE;
};
class lvCSetActivFrame : public lvCOperation
{
public:
	lvCSetActivFrame(lvCSetActivFrame* pSetActivFrame);
	lvCSetActivFrame()	{ InfID = _lvCSetActivFrame_; };
	~lvCSetActivFrame()	{};
public:
	ClassRef<lvCDeffFilmMenu>	MENU;
	int							STATE;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetActivFrame);
		REG_PARENT(lvCOperation);
		REG_AUTO(MENU);
		REG_ENUM(_index,STATE,BE_FRAME_STATE);
	ENDSAVE;
};
class lvCRunTimer : public lvCOperation
{
public:
	lvCRunTimer(lvCRunTimer* pRunTimer);
	lvCRunTimer()	{ InfID = _lvCRunTimer_; UseVV=false; TrTime=false;};
	~lvCRunTimer()	{};
public:
	ClassRef<vvINTEGER>	TimerID;
	int					Time;
	bool	UseVV;
	ClassRef<vvINTEGER>		VVpInt;
	bool	TrTime;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
			DWORD	GetClassMask();
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCRunTimer);
		REG_PARENT(lvCOperation);
		REG_AUTO(TimerID);
		REG_MEMBER(_bool,TrTime);
		REG_MEMBER(_bool,UseVV);
		SAVE_SECTION(0x00000001);
			REG_MEMBER(_int,Time);
		SAVE_SECTION(0x00000002);
			REG_AUTO(VVpInt);
	ENDSAVE;
};
class lvCSetGameMode : public lvCOperation
{
public:
	lvCSetGameMode(lvCSetGameMode* pSetGameMode);
	lvCSetGameMode()	{ InfID = _lvCSetGameMode_; };
	~lvCSetGameMode()	{};
public:
	int		ModeST;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetGameMode);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,ModeST);
	ENDSAVE;
};
class lvCSetCamera : public lvCOperation
{
public:
	lvCSetCamera(lvCSetCamera* pSetCamera);
	lvCSetCamera()	{ InfID = _lvCSetCamera_; };
	~lvCSetCamera()	{};
public:
	ClassRef<vvVector3D>	POS;
	ClassRef<vvVector3D>	DIR;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
public:
	SAVE(lvCSetCamera);
		REG_PARENT(lvCOperation);
		REG_AUTO(POS);
		REG_AUTO(DIR);
	ENDSAVE;
};
class lvCMoveCamera : public lvCOperation
{
public:
	lvCMoveCamera(lvCMoveCamera* pMoveCamera);
	lvCMoveCamera()		{ InfID = _lvCMoveCamera_; };
	~lvCMoveCamera()	{};
public:
	ClassRef<vvVector3D>	POS0;
	ClassRef<vvVector3D>	POS1;
	ClassRef<vvVector3D>	DIR0;
	ClassRef<vvVector3D>	DIR1;
	bool					useMapXY;
	int						Time;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	int				MoveType();
public:
	SAVE(lvCMoveCamera);
		REG_PARENT(lvCOperation);
		REG_AUTO(POS0);
		REG_AUTO(POS1);
		REG_AUTO(DIR0);
		REG_AUTO(DIR1);
		REG_MEMBER(_bool,useMapXY);
		REG_MEMBER(_int,Time);			
	ENDSAVE;
};
class lvCAttachCameraToGroup : public lvCOperation
{
public:
	lvCAttachCameraToGroup(lvCAttachCameraToGroup* pAttachCameraToGroup);
	lvCAttachCameraToGroup()	{ InfID=_lvCAttachCameraToGroup_; };
	~lvCAttachCameraToGroup()	{};

	int						vGrpID;		// Двигаться за группой

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCAttachCameraToGroup);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};

class lvCFreeCamera : public lvCOperation
{
public:
	lvCFreeCamera(lvCFreeCamera* pFreeCamera);
	lvCFreeCamera()		{ InfID=_lvCFreeCamera_; };
	~lvCFreeCamera()	{};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCFreeCamera);
		REG_PARENT(lvCOperation);
	ENDSAVE;
};

class lvCSetLMode : public lvCOperation
{
public:
	lvCSetLMode(lvCSetLMode* pSetLMode);
	lvCSetLMode()	{ InfID = _lvCSetLMode_; };
	~lvCSetLMode()	{};

	ClassRef<vvINTEGER>	vMode;
	int					iMode;
	bool				Use_VV;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual	int		Complite();
			DWORD	GetClassMask(){
						if (Use_VV)	return 0x00000001;
						return 0x00000002;
					};

	virtual			void	GetCopy(lvCOperation** pCopy);
	SAVE(lvCSetLMode);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(vMode);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,iMode);
	ENDSAVE;
};
class lvCSetFogMode : public lvCOperation
{
public:
	lvCSetFogMode(lvCSetFogMode* pSetFogMode);
	lvCSetFogMode()		{ InfID=_lvCSetFogMode_; };
	~lvCSetFogMode()	{};

	int		Mode;	// 0-нет, 1-есть.
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetFogMode);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_int,Mode);
	ENDSAVE;
};
class lvCSetMessageState : public lvCOperation
{
public:
	lvCSetMessageState(lvCSetMessageState* pSetMessageState);
	lvCSetMessageState()	{ InfID=_lvCSetMessageState_; MessType=0; };
	~lvCSetMessageState()	{};

	ClassRef<CSingleMessage>	MESSAGE;
	ClassRef<CPhraseChunk>		TALK_LST;
	ClassRef<COneMissHint>		ONE_HINT;

	bool		Visible;		//Текс отображается.
	bool		Deleted;		//Текст зачеркнут.
	DWORD		Color;			//Цвер текста.
	int			x,y;			//Координаты для подсветки.

	bool		TaskListVisible;

	int			MessType;		// Тип сообщения 0-Task, 1-Talk List, 2-Hint List.


	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
					DWORD	GetClassMask(){
								DWORD MessTypeDW = 0x00000001;
								switch(MessType) {
								case 0:
									MessTypeDW=0x00000001;
									break;
								case 1:
									MessTypeDW=0x00000002;
									break;
								case 2:
									MessTypeDW=0x00000004;
									break;
								case 3:
									MessTypeDW=0x00000008;
									break;
								};
								return MessTypeDW;
							};

	SAVE(lvCSetMessageState);
		REG_PARENT(lvCOperation);
		
		REG_ENUM(_index,MessType,BE_MESSAGE_TYPE_EDITE);

		SAVE_SECTION(0x00000001);		// TASK
			REG_AUTO(MESSAGE);
			REG_MEMBER(_bool,Visible);
			REG_MEMBER(_bool,Deleted);
			REG_MEMBER(_color,Color);
			REG_MEMBER(_int,x);
			REG_MEMBER(_int,y);
			
		SAVE_SECTION(0x00000002);		// TALK LIST
			REG_AUTO(TALK_LST);
			REG_MEMBER(_bool,Visible);

		SAVE_SECTION(0x00000004);		// HINT LIST
			REG_AUTO(ONE_HINT);
			REG_MEMBER(_bool,Visible);
		
		SAVE_SECTION(0x00000008)
			REG_MEMBER(_bool,TaskListVisible);
		
	ENDSAVE;
};

class lvCSaveScreenPos : public lvCOperation
{
public:
	lvCSaveScreenPos()		{ InfID=_lvCSaveScreenPos_; };
	lvCSaveScreenPos(lvCSaveScreenPos* pSaveScreenPos);
	~lvCSaveScreenPos()		{};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	ClassRef<vvVector3D>	vCameraPos;
	ClassRef<vvVector3D>	vCameraDir;

	SAVE(lvCSaveScreenPos);
		REG_PARENT(lvCOperation);
		REG_AUTO(vCameraPos);
		REG_AUTO(vCameraDir);
	ENDSAVE;
};
class lvCFilmCopliteState : public lvCOperation
{
public:
	lvCFilmCopliteState()	{ InfID=_lvCFilmCopliteState_; };
	lvCFilmCopliteState(lvCFilmCopliteState* pFilmCopliteState);
	~lvCFilmCopliteState()	{};

	ClassRef<lvCFilm>	Film;
	bool				Complite;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCFilmCopliteState);
		REG_PARENT(lvCOperation);
		REG_AUTO(Film);
		REG_MEMBER(_bool,Complite);
	ENDSAVE;
};
//======================================================================//
//=================  GLOBAL APPLYNING FUNCTION  ========================//
//======================================================================//
class lvCSetGameSpeed : public lvCOperation
{
public:
	lvCSetGameSpeed()	{ InfID=_lvCSetGameSpeed_; };
	lvCSetGameSpeed(lvCSetGameSpeed* pSetGameSpeed);
	~lvCSetGameSpeed()	{  };

	bool				Use_VV;
	int					iSpeed;
	ClassRef<vvINTEGER>	vSpeed;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
					DWORD	GetClassMask(){
								if (Use_VV)	return 0x00000001;
								return 0x00000002;
							};

	SAVE(lvCSetGameSpeed);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,Use_VV);
		SAVE_SECTION(0x00000001);
			REG_AUTO(vSpeed);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,iSpeed);
	ENDSAVE;
};
class lvCGetGameSpeed : public lvCOperation
{
public:
	lvCGetGameSpeed()	{ InfID=_lvCGetGameSpeed_; };
	lvCGetGameSpeed(lvCGetGameSpeed* pGetGameSpeed);
	~lvCGetGameSpeed()	{};
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	ClassRef<vvINTEGER>	vSpeed;

	SAVE(lvCGetGameSpeed);
		REG_PARENT(lvCOperation);
		REG_AUTO(vSpeed);
	ENDSAVE;
};
class lvCShowPanel : public lvCOperation
{
public:
	lvCShowPanel()	{ InfID = _lvCShowPanel_; OpenTime=1500; ShowTime=2500; CloseTime=1500; };
	lvCShowPanel(lvCShowPanel* pShowPanel);
	~lvCShowPanel()	{};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	_str		TextID;

	int OpenTime;
	int ShowTime;
	int CloseTime;

	SAVE(lvCShowPanel);
		REG_PARENT(lvCOperation);
		REG_AUTO(TextID);
		REG_MEMBER(_int,OpenTime);
		REG_MEMBER(_int,ShowTime);
		REG_MEMBER(_int,CloseTime);
	ENDSAVE;
};
class lvCQuestData : public lvCOperation
{
public:
	lvCQuestData()	{ InfID=_lvCQuestData_; OperType=0; };
	lvCQuestData(lvCQuestData* pQuestData);
	~lvCQuestData()	{};

	ClassRef<vvMissionLOG>	Data;

	int		QuestN;					// Кол-во квестов на карте
	int		QuestTotalExperience;	// Экспириенс за все квесты
	int		KillsTotalExperience;	// Полный экспириенс за убитых
	int		TimeEverage;			// Среднее время прохождения миссии
	int		TimeEverageExperience;	// Экспа за среднее время прохождения мисии

	int		QuestComleteExperience;	// Полученный экспириенс

	int		OperType;				// Set new data, add comlete quest, save data
	DWORD	GetClassMask(){
				DWORD	ret = 0x00000001;
				switch(OperType) {
				case 0:	// "SetQuestData"
					ret = 0x00000001;
					break;
				case 1: // "AddComplQuest"
					ret = 0x00000002;
					break;
				case 2: // "SaveData"
					ret = 0x00000004;
					break;
				};
				return ret;
			};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCQuestData);
		REG_PARENT(lvCOperation);
		REG_AUTO(Data);
		REG_ENUM(_index,OperType,BE_QUEST_COM_TYPE);
		SAVE_SECTION(0x00000001);
			REG_MEMBER(_int,QuestN);
			REG_MEMBER(_int,QuestTotalExperience);
			REG_MEMBER(_int,KillsTotalExperience);
			REG_MEMBER(_int,TimeEverage);
			REG_MEMBER(_int,TimeEverageExperience);
		SAVE_SECTION(0x00000002);
			REG_MEMBER(_int,QuestComleteExperience);
		SAVE_SECTION(0x00000004);
			
	ENDSAVE;
};
class lvCReStartSquadShema : public lvCOperation
{
public:
	lvCReStartSquadShema()	{ InfID=_lvCReStartSquadShema_; NodeID=-1; };
	lvCReStartSquadShema(lvCReStartSquadShema* pReStartSquadShema);
	~lvCReStartSquadShema()	{};

	int	vGroup;
	int NodeID;	// -1 if not used

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCReStartSquadShema);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGroup,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,NodeID);
	ENDSAVE;
};
class lvCPAUSE : public lvCOperation
{
public:
	lvCPAUSE()		{ InfID=_lvCPAUSE_; };
	lvCPAUSE(lvCPAUSE* pPAUSE);
	~lvCPAUSE()		{};

	bool	state;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCPAUSE);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,state);
	ENDSAVE;
};
class lvCSetSilence : public lvCOperation
{
public:
	lvCSetSilence()	{ InfID=_lvCSetSilence_; };
	lvCSetSilence(lvCSetSilence* pSetSilence);
	~lvCSetSilence(){};

	bool State;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSetSilence);
		REG_PARENT(lvCOperation);
		REG_MEMBER(_bool,State);
	ENDSAVE;
};
//======================================================================//
//========================  COSSAKS II  ================================//
//======================================================================//
class lvCShowMessageII : public lvCOperation
{
public:
	lvCShowMessageII();
	lvCShowMessageII(lvCShowMessageII* pShowMessageII);
	~lvCShowMessageII();

	bool				autoHideElse;
	word				FileID;			// Файл с картинкой (0xFFFF - ничего нет)
	int					SpriteID;		// Индекс картинки	(-1 - ничего нет)
	_str				TextID;
	ClassArray<vvBASE>	paramList;
	
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

					bool	CheckState();
					void	PrepareString(_str& FullString);

	class MS_PreShow : public BaseFunction
	{
	public:
		void	EvaluateFunction();
		SAVE(MS_PreShow);
			REG_PARENT(BaseFunction);
		ENDSAVE;
	}fPreProcess;

	SAVE(lvCShowMessageII);
		REG_PARENT(lvCOperation);
		
		REG_MEMBER(_bool,autoHideElse);

		REG_MEMBER(_gpfile,FileID);
		REG_SPRITE(SpriteID,FileID);

		REG_AUTO(TextID);
		REG_AUTO(paramList);

		REG_AUTO(fPreProcess);
	ENDSAVE;
};

extern	struct cvs_BrigPanel;
class lvCBrigPanelSet : public lvCOperation
{
public:
	lvCBrigPanelSet();
	lvCBrigPanelSet(lvCBrigPanelSet* pBrigPanelSet);
	~lvCBrigPanelSet();

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SubSection			Settings;
	bool				Bayonet;		
	bool				Rifle;
	bool				Grenade;
	bool				Formation;
	bool				Disband;
	bool				Fill;
	bool				Stop;

	void	ApplyParams(cvs_BrigPanel& BP);

	class MS_PreShow : public BaseFunction
	{
	public:
		void	EvaluateFunction();
		SAVE(MS_PreShow);
			REG_PARENT(BaseFunction);
		ENDSAVE;
	}fPreProcess;


	SAVE(lvCBrigPanelSet);
		REG_PARENT(lvCOperation);
		REG_AUTO(Settings);
			REG_MEMBER(_bool,Bayonet);
			REG_MEMBER(_bool,Rifle);
			REG_MEMBER(_bool,Grenade);
			REG_MEMBER(_bool,Formation);
			REG_MEMBER(_bool,Disband);
			REG_MEMBER(_bool,Fill);
			REG_MEMBER(_bool,Stop);
			REG_AUTO(fPreProcess);
	ENDSAVE;
};
class lvCGroupHoldNode : public lvCOperation
{
public:
	lvCGroupHoldNode()	{InfID=_lvCGroupHoldNode_;};
	lvCGroupHoldNode(lvCGroupHoldNode* pGroupHoldNode);
	~lvCGroupHoldNode()	{};

	int		vGrp;
	_str	RulesFile;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCGroupHoldNode);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);		
		REG_FILEPATH(RulesFile,".sia");
	ENDSAVE;
};
int GroupHoldPOS_AI_lua(lvCGroup* pGRP,int x,int y,const char* FNane);
class lvCAddElemTHE_CII : public lvCOperation
{
public:
	lvCAddElemTHE_CII();
	lvCAddElemTHE_CII(lvCAddElemTHE_CII* pAddElemTHE_CII);
	~lvCAddElemTHE_CII(){};

	ClassRef<vvTASKS_CII>	OBJECT;

	SubSection	Task;
	bool		add_TASK;
	bool		TASK_Dublicate;
	int			TASK_POS;
	_ClassIndex TASK;
	
	SubSection	Hint;
	bool		add_HINT;
	bool		HINT_Dublicate;
	int			HINT_POS;
	_ClassIndex HINT;

	SubSection	Else;
	bool		add_ELSE;
	bool		ELSE_Dublicate;
	int			ELSE_POS;
	_ClassIndex ELSE;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCAddElemTHE_CII);
		REG_PARENT(lvCOperation);
		REG_AUTO(OBJECT);
		REG_AUTO(Task);
			REG_MEMBER(_bool,add_TASK);
			REG_MEMBER(_bool,TASK_Dublicate);
			REG_MEMBER(_int,TASK_POS);
			REG_AUTO2(TASK,"OBJECT^REF/TASK Name");
		REG_AUTO(Hint);
			REG_MEMBER(_bool,add_HINT);
			REG_MEMBER(_bool,HINT_Dublicate);
			REG_MEMBER(_int,HINT_POS);
			REG_AUTO2(HINT,"OBJECT^REF/HINT Name");
		REG_AUTO(Else);
			REG_MEMBER(_bool,add_ELSE);
			REG_MEMBER(_bool,ELSE_Dublicate);
			REG_MEMBER(_int,ELSE_POS);
			REG_AUTO2(ELSE,"OBJECT^REF/ELSE Name");
	ENDSAVE;
};
class lvCDelElemTHE_CII : public lvCOperation
{
public:
	lvCDelElemTHE_CII(){ InfID=_lvCDelElemTHE_CII_; FULL_DELETE=false; TASK_USE=HINT_USE=ELSE_USE=false; };
	lvCDelElemTHE_CII(lvCDelElemTHE_CII* pDelElemTHE_CII);
	~lvCDelElemTHE_CII(){};

	ClassRef<vvTASKS_CII>	OBJECT;

	SubSection	TASK_S;
	bool		TASK_USE;
	_ClassIndex TASK;
	bool		FULL_DELETE;

	SubSection	HINT_S;
	bool		HINT_USE;
	_ClassIndex HINT;

	SubSection	ELSE_S;
	bool		ELSE_USE;
	_ClassIndex ELSE;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCDelElemTHE_CII);
		REG_PARENT(lvCOperation);
		REG_AUTO(OBJECT);

		REG_AUTO(TASK_S);
			REG_MEMBER(_bool,TASK_USE);
			REG_AUTO2(TASK,"OBJECT^REF/TASK Name");
			REG_MEMBER(_bool,FULL_DELETE);

		REG_AUTO(HINT_S);
			REG_MEMBER(_bool,HINT_USE);
			REG_AUTO2(HINT,"OBJECT^REF/HINT Name");

		REG_AUTO(ELSE_S);
			REG_MEMBER(_bool,ELSE_USE);
			REG_AUTO2(ELSE,"OBJECT^REF/ELSE Name");
	ENDSAVE;
};
class lvCSET_MISS_MANAGER : public lvCOperation
{
public:
	lvCSET_MISS_MANAGER(){InfID=_lvCSET_MISS_MANAGER_; PAUSE_Animate=RESTART_Animate=NEXT_Animate=false;};
	lvCSET_MISS_MANAGER(lvCSET_MISS_MANAGER* pSET_MISS_MANAGER);
	~lvCSET_MISS_MANAGER(){};

	ClassRef<vvMISSMGR>	MISS_MANAGER;
	bool	PAUSE,		PAUSE_Animate;
	bool	RESTART,	RESTART_Animate;
	bool	NEXT,		NEXT_Animate;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);

	SAVE(lvCSET_MISS_MANAGER);
		REG_PARENT(lvCOperation);
		REG_AUTO(MISS_MANAGER);
		REG_MEMBER(_bool,PAUSE);
		REG_MEMBER(_bool,PAUSE_Animate);
		REG_MEMBER(_bool,RESTART);
		REG_MEMBER(_bool,RESTART_Animate);
		REG_MEMBER(_bool,NEXT);
		REG_MEMBER(_bool,NEXT_Animate);
	ENDSAVE;
};
class lvCArtChangeCharge : public lvCOperation
{
/*
	Change curent charge for artilery, 0/1 - variants
*/
public:
	lvCArtChangeCharge(){ InfID=_lvCArtChangeCharge_; };
	lvCArtChangeCharge(lvCArtChangeCharge* pArtChangeCharge);
	~lvCArtChangeCharge(){};
	int vGrp;
	int Charge;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
	SAVE(lvCArtChangeCharge);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,Charge);
	ENDSAVE;
};
class lvCArtAttackPoint : public lvCOperation
{
/*
	Attack point by artirely
*/
public:
	lvCArtAttackPoint(){ InfID=_lvCArtAttackPoint_; };
	lvCArtAttackPoint(lvCArtAttackPoint* pArtAttackPoint);
	~lvCArtAttackPoint(){};
	int vGrp;
	int NTimes;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		Process(int time);
	virtual			void	GetCopy(lvCOperation** pCopy);
	SAVE(lvCArtAttackPoint);
		REG_PARENT(lvCOperation);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_int,NTimes);
	ENDSAVE;
};
//======================================================================//
//=================    FUNCTION FOR CONDITION	========================//
//======================================================================//
class lvCBaseFunction	: public lvCBaseOperCond
{
public:
	lvCBaseFunction()		{ InfID = _lvCBaseFunctions_; };
	lvCBaseFunction(lvCBaseFunction* pBaseFunction);
	~lvCBaseFunction()		{};
public:
	virtual	void	GetCopy(lvCBaseFunction** pCopy);
	virtual	int		GetValue(int time)	{ return	0; };
	virtual	int		Power()	{ return 0; };
public:
	SAVE(lvCBaseFunction);
		REG_PARENT(lvCBaseOperCond);
	ENDSAVE;
};
class lvCGetValue : public lvCBaseFunction
{
public:
	lvCGetValue(lvCGetValue* pGetValue);
	lvCGetValue()	{ InfID = _lvCGetValue_; };
	~lvCGetValue()	{};
public:
	ClassRef<vvBASE>		Value;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	virtual			int		Power()	{ return 1; };
public:
	SAVE(lvCGetValue);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(Value);
	ENDSAVE;
};

class lvCBool	: public lvCBaseFunction
{
public:
	lvCBool(lvCBool* pBool);
	lvCBool()	{ InfID = _lvCBool_; };
	~lvCBool(){};
public:
	bool	lvB;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCBool);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_bool,lvB);
	ENDSAVE;
};
class lvCInt	: public lvCBaseFunction
{
public:
	lvCInt(lvCInt* pInt);
	lvCInt()	{ InfID = _lvCInt_; };
	~lvCInt(){};
public:
	int		lvI;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCInt);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,lvI);
	ENDSAVE;
};
class lvCGetScreenXY : public lvCBaseFunction
{
public:
	lvCGetScreenXY(lvCGetScreenXY* pGetScreenXY);
	lvCGetScreenXY()	{ InfID=_lvCGetScreenXY_; };
	~lvCGetScreenXY(){};
public:
	ClassRef<vvINTEGER>		sX;
	ClassRef<vvINTEGER>		sY;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetScreenXY);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(sX);
		REG_AUTO(sY);
	ENDSAVE;
};
class lvCChkTime	: public lvCBaseFunction
{
public:
	lvCChkTime(lvCChkTime* pChkTime);
	lvCChkTime()	{ InfID = _lvCChkTime_; };
	~lvCChkTime(){};
public:
	int		timeOt;
	int		timeDo;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCChkTime);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,timeOt);
		REG_MEMBER(_int,timeDo);
	ENDSAVE;
};
class lvCGetAmount : public lvCBaseFunction
{
public:
	lvCGetAmount(lvCGetAmount* pGetAmount);
	lvCGetAmount() { InfID = _lvCGetAmount_; Buildings=false; };
	~lvCGetAmount(){};
public:
	int		parNat;
	bool	Buildings;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetAmount);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,parNat);
		REG_MEMBER(_bool,Buildings);
	ENDSAVE;
};
int	GetAmount_lua(int nat,bool buildings);
class lvCGetUnitsAmount0 : public lvCBaseFunction
{
public:
	lvCGetUnitsAmount0(lvCGetUnitsAmount0* pGetUnitsAmount0);
	lvCGetUnitsAmount0() { InfID = _lvCGetUnitsAmount0_; };
	~lvCGetUnitsAmount0(){};
public:
	int		parZn;
	int		parNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	virtual			int		Power()	{ return 1; };
public:
	SAVE(lvCGetUnitsAmount0);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,parZn,ALL_ZONES_ON_MAP);
		REG_MEMBER(_int,parNat);
	ENDSAVE;
};
int GetUnitsAmount0_lua(int nat,int x,int y,int R);
class lvCGetUnitsAmount1 : public lvCBaseFunction
{
public:
	lvCGetUnitsAmount1(lvCGetUnitsAmount1* pGetUnitsAmount1);
	lvCGetUnitsAmount1() { InfID = _lvCGetUnitsAmount1_; };
	~lvCGetUnitsAmount1(){};
public:
	int		parZn;
	int		parGrp;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetUnitsAmount1);
	REG_PARENT(lvCBaseFunction);
	REG_ENUM(_index,parZn,ALL_ZONES_ON_MAP);
	SAVE_SECTION(0x00000001);
		REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
	SAVE_SECTION(0x00000002);
		REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCGetUnitsAmount2 : public lvCBaseFunction
{
public:
	lvCGetUnitsAmount2(lvCGetUnitsAmount2* pGetUnitsAmount2);
	lvCGetUnitsAmount2() { InfID = _lvCGetUnitsAmount2_; };
	~lvCGetUnitsAmount2(){};
public:
	int		parZn;
	int		UnitType;
	int		parNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetUnitsAmount2);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,parZn,ALL_ZONES_ON_MAP);
		REG_MEMBER(_UnitType,UnitType);
		REG_MEMBER(_int,parNat);
	ENDSAVE;
};
int GetUnitsAmount2_lua(int nat,int UT,int x,int y,int R);
class lvCGetUnitsAmount3 : public lvCBaseFunction
{
public:
	lvCGetUnitsAmount3(lvCGetUnitsAmount3* pGetUnitsAmount3);
	lvCGetUnitsAmount3() { InfID = _lvCGetUnitsAmount3_; };
	~lvCGetUnitsAmount3(){};
public:
	int		vGrp;
	int		parRad;
	int		parNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetUnitsAmount3);
	REG_PARENT(lvCBaseFunction);
	REG_MEMBER(_int,parNat);
	REG_MEMBER(_int,parRad);
	SAVE_SECTION(0x00000001);
		REG_ENUM(_index,vGrp,ALL_GROUPS_ON_MAP);
	SAVE_SECTION(0x00000002);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCGetTotalAmount0 : public lvCBaseFunction
{
public:
	lvCGetTotalAmount0(lvCGetTotalAmount0* pGetTotalAmount0);
	lvCGetTotalAmount0() { InfID = _lvCGetTotalAmount0_; };
	~lvCGetTotalAmount0(){};
public:
	int		parGrp;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetTotalAmount0);
		REG_PARENT(lvCBaseFunction);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCGetTotalAmount1 : public lvCBaseFunction
{
public:
	lvCGetTotalAmount1(lvCGetTotalAmount1* pGetTotalAmount1);
	lvCGetTotalAmount1() { InfID = _lvCGetTotalAmount1_; };
	~lvCGetTotalAmount1(){};
public:
	int		UnitType;
	int		parNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetTotalAmount1);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_UnitType,UnitType);
		REG_MEMBER(_int,parNat);
	ENDSAVE;
};
int GetTotalAmount1_lua(int nat,int UT);
class lvCGetTotalAmount2 : public lvCBaseFunction
{
public:
	lvCGetTotalAmount2(lvCGetTotalAmount2* pGetTotalAmount2);
	lvCGetTotalAmount2() { InfID = _lvCGetTotalAmount2_; };
	~lvCGetTotalAmount2(){};
public:
	int		parGrp;
 	int		UnitType;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetTotalAmount2);
		REG_PARENT(lvCBaseFunction);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,parGrp,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_UnitType,UnitType);
	ENDSAVE;
};
class lvCGetReadyAmount : public lvCBaseFunction
{
public:
	lvCGetReadyAmount(lvCGetReadyAmount* pGetReadyAmount);
	lvCGetReadyAmount() { InfID = _lvCGetReadyAmount_; };
	~lvCGetReadyAmount(){};
public:
	int		UnitType;
	int		parNat;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetReadyAmount);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_UnitType,UnitType);
		REG_MEMBER(_int,parNat);
	ENDSAVE;
};
int GetReadyAmount_lua(int nat,int UT);
class lvCGetResource : public lvCBaseFunction
{
public:
	lvCGetResource(lvCGetResource* pGetResource);
	lvCGetResource() { InfID = _lvCGetResource_; };
	~lvCGetResource(){};
public:
	int		parNat;
	int		parID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetResource);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,parNat);
		REG_ENUM(_index,parID,RESTYPE);
	ENDSAVE;
};
int GetResource_lua(int nat,int resid);
class lvCGetDiff : public lvCBaseFunction
{
public:
	lvCGetDiff(lvCGetDiff* pGetDiff);
	lvCGetDiff() { InfID = _lvCGetDiff_; };
	~lvCGetDiff(){};
public:
	int		parNI;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetDiff);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,parNI);
	ENDSAVE;
};
int GetDiff_lua(int nat);
class lvCProbably	: public lvCBaseFunction
{
public:
	lvCProbably(lvCProbably* pProbably);
	lvCProbably()	{ InfID = _lvCProbably_; };
	~lvCProbably()	{};
public:
	int	parVer;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCProbably);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,parVer);
	ENDSAVE;
};
class lvCGetUnitState	: public lvCBaseFunction
{
public:
	lvCGetUnitState(lvCGetUnitState* pGetUnitState);
	lvCGetUnitState()	{ InfID = _lvCGetUnitState_; };
	~lvCGetUnitState()	{};
public:
	int	parGrp;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGetUnitState);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCTrigg : public lvCBaseFunction
{
public:
	lvCTrigg(lvCTrigg* pTrigg);
	lvCTrigg() { InfID = _lvCTrigg_; };
	~lvCTrigg(){};
public:
	int		TID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCTrigg);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,TID);
	ENDSAVE;
};
int Trigg_lua(int trigid);
class lvCGrpInNode : public lvCBaseFunction
{
public:
	lvCGrpInNode(lvCGrpInNode* pGrpInNode);
	lvCGrpInNode() { InfID = _lvCGrpInNode_; };
	~lvCGrpInNode(){};
public:
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGrpInNode);
		REG_PARENT(lvCBaseFunction);
	ENDSAVE;
};
class lvCGrpInNodeFree : public lvCBaseFunction
{
public:
	lvCGrpInNodeFree(lvCGrpInNodeFree* pGrpInNodeFree);
	lvCGrpInNodeFree() { InfID = _lvCGrpInNodeFree_; };
	~lvCGrpInNodeFree(){};
public:
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCGrpInNodeFree);
		REG_PARENT(lvCBaseFunction);
	ENDSAVE;
};
class lvCAllGrpInNode : public lvCBaseFunction
{
public:
	lvCAllGrpInNode(lvCAllGrpInNode* pAllGrpInNode);
	lvCAllGrpInNode() { InfID = _lvCAllGrpInNode_; };
	~lvCAllGrpInNode(){};
public:
	int		grpID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCAllGrpInNode);
		REG_PARENT(lvCBaseFunction);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,grpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,grpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCAllGrpInNodeFree : public lvCBaseFunction
{
public:
	lvCAllGrpInNodeFree(lvCAllGrpInNodeFree* pAllGrpInNodeFree);
	lvCAllGrpInNodeFree() { InfID = _lvCAllGrpInNodeFree_; };
	~lvCAllGrpInNodeFree(){};
public:
	int		grpID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCAllGrpInNodeFree);
		REG_PARENT(lvCBaseFunction);
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,grpID,ALL_GROUPS_ON_MAP);
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,grpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class FG_Visible : public lvCBaseFunction
{
public:
	FG_Visible(FG_Visible* pFG_Visible);
	FG_Visible()	{ InfID=_FG_Visible_; };
	~FG_Visible()	{};
public:
	ClassRef<lvCGraphObject>	GraphObj;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(FG_Visible);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(GraphObj);
	ENDSAVE;
};
class FG_InVisible : public lvCBaseFunction
{
public:
	FG_InVisible(FG_InVisible* pFG_InVisible);
	FG_InVisible()	{ InfID=_FG_InVisible_; };
	~FG_InVisible()	{};
public:
	ClassRef<lvCGraphObject>	GraphObj;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(FG_InVisible);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(GraphObj);
	ENDSAVE;
};
class ogSTOP : public lvCBaseFunction
{
public:
	ogSTOP(ogSTOP* pogSTOP);
	ogSTOP()	{ InfID = _ogSTOP_; };
	~ogSTOP(){};
public:
	int		canal;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(ogSTOP);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,canal);
	ENDSAVE;
};
int ogSTOP_lua(int canal);
class lvCTimerDone : public lvCBaseFunction
{
public:
	lvCTimerDone(lvCTimerDone* pTimerDone);
	lvCTimerDone()	{ InfID = _lvCTimerDone_; };
	~lvCTimerDone()	{};
public:
	ClassRef<vvINTEGER>	TimerID;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCTimerDone);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(TimerID);
	ENDSAVE;
};
class lvCChekPosition : public lvCBaseFunction
{
public:
	lvCChekPosition(lvCChekPosition* pChekPosition);
	lvCChekPosition()	{ InfID = _lvCChekPosition_; };
	~lvCChekPosition()	{};
public:
	int	parGrp;
	ClassRef<vvPOINT_SET>		VVpPos;
public:
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
public:
	SAVE(lvCChekPosition);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,parGrp,ALL_vGROUPS_ON_MAP);
		REG_AUTO(VVpPos);
	ENDSAVE;
};
class lvCCameraSTOP : public lvCBaseFunction
{
public:
	lvCCameraSTOP(lvCCameraSTOP* pCameraSTOP);
	lvCCameraSTOP()		{ InfID=_lvCCameraSTOP_; };
	~lvCCameraSTOP()	{};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCCameraSTOP);
		REG_PARENT(lvCBaseFunction);
	ENDSAVE;
};
int CameraSTOP_lua();
class lvCNationIsErased : public lvCBaseFunction
{
public:
	lvCNationIsErased(lvCNationIsErased* pNationIsErased);
	lvCNationIsErased()		{ InfID=_lvCNationIsErased_; Nat=0; };
	~lvCNationIsErased()	{};

	int		Nat;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCNationIsErased);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,Nat);
	ENDSAVE;
};
class lvCGetLMode : public lvCBaseFunction
{
public:
	lvCGetLMode(lvCGetLMode* pGetLMode);
	lvCGetLMode()	{ InfID=_lvCGetLMode_; };
	~lvCGetLMode()	{};

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCGetLMode);
		REG_PARENT(lvCBaseFunction);
	ENDSAVE;
};
int GetLMode_lua();
class lvCCheckButton : public lvCBaseFunction
{
public:
	lvCCheckButton(lvCCheckButton* pCheckButton);
	lvCCheckButton()	{ InfID=_lvCCheckButton_; };
	~lvCCheckButton()	{};

	int		vkID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCCheckButton);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,vkID);
	ENDSAVE;
};
int CheckButton_lua(int vkid);
class lvCIsBrigade : public lvCBaseFunction
{
public:
	lvCIsBrigade(lvCIsBrigade* pIsBrigade);
	lvCIsBrigade()	{ InfID=_lvCIsBrigade_; checkKOM=false; };
	~lvCIsBrigade()	{};

	int		vgGrpID;
	bool	checkKOM;	// Проверить наличие командного состава.

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

    SAVE(lvCIsBrigade);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vgGrpID,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,checkKOM);
	ENDSAVE;
};

class lvCGetFormationType : public lvCBaseFunction
{
public:
	lvCGetFormationType(lvCGetFormationType* pGetFormationType);
	lvCGetFormationType()	{ InfID=_lvCGetFormationType_; };
	~lvCGetFormationType()	{};

	int vgGrpID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);		// 0,1,2 - типы формаций, -1 - это не возможно узнать.
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCGetFormationType);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vgGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
int GetFormationType_lua(lvCGroup* pGrp);
class lvCPrOfSquadInNode : public lvCBaseFunction
{
public:
	lvCPrOfSquadInNode()	{ InfID=_lvCPrOfSquadInNode_; };
	lvCPrOfSquadInNode(lvCPrOfSquadInNode* pPrOfSquadInNode);
	~lvCPrOfSquadInNode()	{};

	int rate;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);		// 0,1,2 - типы формаций, -1 - это не возможно узнать.
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	virtual			int		Power()	{ return 1; };
    
	SAVE(lvCPrOfSquadInNode);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,rate);
	ENDSAVE;
};
class lvCGetNofNewUnitInGrp : public lvCBaseFunction
{
public:
	lvCGetNofNewUnitInGrp()	{ InfID=_lvCGetNofNewUnitInGrp_; ClearNew=false; };
	lvCGetNofNewUnitInGrp(lvCGetNofNewUnitInGrp* pGetNofNewUnitInGrp);
	~lvCGetNofNewUnitInGrp(){};

	int		vGrp;
	bool	ClearNew;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	
	SAVE(lvCGetNofNewUnitInGrp);
		REG_PARENT(lvCBaseFunction);
        REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,ClearNew);
	ENDSAVE;
};
class lvCGetNofMyVillage : public lvCBaseFunction
{
public:
	lvCGetNofMyVillage()	{ InfID=_lvCGetNofMyVillage_; };
	lvCGetNofMyVillage(lvCGetNofMyVillage* pGetNofMyVillage);
	~lvCGetNofMyVillage()	{};

	int Owner;
	int ResType;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCGetNofMyVillage);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,Owner);
		REG_ENUM(_index,ResType,RESTYPE);
	ENDSAVE;
};
class lvCCheckRBBP : public lvCBaseFunction // lvCCheckReadyBuildingBetwinPosition
{
public:
	lvCCheckRBBP()	{ InfID=_lvCCheckRBBP_; MaxR=50; };
	lvCCheckRBBP(lvCCheckRBBP* pCheckRBBP);
	~lvCCheckRBBP()	{};

	ClassRef<vvPOINT2D>	P0;
	ClassRef<vvPOINT2D>	P1;
	int					MaxR;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCCheckRBBP);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(P0);
		REG_AUTO(P1);
		REG_MEMBER(_int,MaxR);
	ENDSAVE;
};
class lvCIsTired : public lvCBaseFunction
{
public:
	lvCIsTired(){ InfID=_lvCIsTired_; };
	lvCIsTired(lvCIsTired* pIsTired);
	~lvCIsTired(){};

	int vGrp;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCIsTired);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCBrigadesAmount : public lvCBaseFunction
{
public:
	lvCBrigadesAmount(){ InfID=_lvCBrigadesAmount_; Nat=0; };
	lvCBrigadesAmount(lvCBrigadesAmount* pBrigadesAmount);
	~lvCBrigadesAmount(){};

	int Nat;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCBrigadesAmount);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,Nat);
	ENDSAVE;
};
class lvCTestFillingAbility : public lvCBaseFunction
{
/*
	Retern true if enabele to fill one or more object in group
 */
public:
	lvCTestFillingAbility(){ InfID=_lvCTestFillingAbility_; };
	lvCTestFillingAbility(lvCTestFillingAbility* pTestFillingAbility);
	~lvCTestFillingAbility(){};

	int vGrp;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCTestFillingAbility);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
int TestFillingAbility_lua(lvCGroup* pGrp);
class lvCInStandGround : public lvCBaseFunction
{
/*
	Return true if all brigades in group in stand ground state
*/
public:
	lvCInStandGround(){ InfID=_lvCInStandGround_; AllBrigades=true; };
	lvCInStandGround(lvCInStandGround* pInStandGround);
	~lvCInStandGround(){};
	int		vGrp;
	bool	AllBrigades;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	SAVE(lvCInStandGround);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
		REG_MEMBER(_bool,AllBrigades);
	ENDSAVE;
};
int CInStandGround_lua(lvCGroup* pGrp,int AllBrigades);
class lvCVillageOwner : public lvCBaseFunction
{
public:
	lvCVillageOwner(){ InfID=_lvCVillageOwner_; };
	lvCVillageOwner(lvCVillageOwner* pVillageOwner);
	~lvCVillageOwner(){};
	_str VillageName;	// Central group name for village
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	SAVE(lvCVillageOwner);
		REG_PARENT(lvCBaseFunction);
		REG_AUTO(VillageName);
	ENDSAVE;
};
int VillageOwner_lua(const char* vilname);
class lvCGetNofBrigInNode : public lvCBaseFunction
{
public:
	lvCGetNofBrigInNode(){ InfID=_lvCGetNofBrigInNode_; };
	lvCGetNofBrigInNode(lvCGetNofBrigInNode* pGetNofBrigInNode);
	~lvCGetNofBrigInNode(){};
	int Nat;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	SAVE(lvCGetNofBrigInNode);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_int,Nat);
	ENDSAVE;
};
int GetNofBrigInNode_lua(int nat,int x,int y,int R);
//======================================================================//
//=================	 FUNCTION FOR TRANSPORT	 =======================//
//======================================================================//
class lvCGetNInside : public lvCBaseFunction
{
public:
	lvCGetNInside()		{ InfID=_lvCGetNInside_; Max=false; };
	lvCGetNInside(lvCGetNInside* pGetNInside);
	~lvCGetNInside()	{};

	bool	Max;
	int		vGrpID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCGetNInside);
		REG_PARENT(lvCBaseFunction);
		REG_MEMBER(_bool,Max);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCCheckLeaveAbility : public lvCBaseFunction
{
public:
	lvCCheckLeaveAbility()	{ InfID=_lvCCheckLeaveAbility_; };
	lvCCheckLeaveAbility(lvCCheckLeaveAbility* pCheckLeaveAbility);
	~lvCCheckLeaveAbility()	{};

	int		vGrpID;

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	virtual			int		Power()	{ return 1; };

	SAVE(lvCCheckLeaveAbility);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrpID,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCLoadingCoplite : public lvCBaseFunction
{
public:
	lvCLoadingCoplite()		{ InfID=_lvCLoadingCoplite_; };
	lvCLoadingCoplite(lvCLoadingCoplite* pLoadingCoplite);
	~lvCLoadingCoplite()	{};

	int vGrpTransport;	// Транспорты

	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);

	SAVE(lvCLoadingCoplite);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrpTransport,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
int LoadingCoplite_lua(lvCGroup* pvGrp);
class lvCGetCurGrpORDER : public lvCBaseFunction
{
public:
	lvCGetCurGrpORDER(){InfID=_lvCGetCurGrpORDER_;};
	lvCGetCurGrpORDER(lvCGetCurGrpORDER* pGetCurGrpORDER);
	~lvCGetCurGrpORDER(){};
	int vGrp;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	SAVE(lvCGetCurGrpORDER);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
class lvCGetNofBRLoadedGun : public lvCBaseFunction
{
public:
	lvCGetNofBRLoadedGun(){InfID=_lvCGetNofBRLoadedGun_;};
	lvCGetNofBRLoadedGun(lvCGetNofBRLoadedGun* pGetNofBRLoadedGun);
	~lvCGetNofBRLoadedGun(){};
	int vGrp;
	virtual const	char*	GetThisElementView(const char* LocalName);
	virtual			int		GetValue(int time);
	virtual			void	GetCopy(lvCBaseFunction** pCopy);
	SAVE(lvCGetNofBRLoadedGun);
		REG_PARENT(lvCBaseFunction);
		REG_ENUM(_index,vGrp,ALL_vGROUPS_ON_MAP);
	ENDSAVE;
};
//////////////////////////////////////////////////////////////////////////
void REG_BE_FUNCTIONS_class();
//////////////////////////////////////////////////////////////////////////
#endif//__BE_FUNCTIONS__































