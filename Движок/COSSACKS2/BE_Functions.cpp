#include "stdheader.h"
#include "BE_HEADERS.h"
#include "mp3\\oggvor.h"
#include "HashTop.h"


#define BE_MinR	500 
int GetMinR_ByNUnits(int N){
	if (N==1)			return	50;
	if (1<N&&N<=10)		return	150;
	if (10<N&&N<=36)	return	360;
	if (36<N&&N<=72)	return	550;
	if (72<N&&N<=130)	return	600;
						return	800;
};

extern	bool		NOPAUSE;
extern	_str		g_vvElementView;
typedef bool tpUnitsCallback(OneObject* OB,void* param);
int				PerformActionOverUnitsInRadius(int xc,int yc,int R,tpUnitsCallback* CB,void* Param);
void			SendSelectedToXY(byte NI,int xx,int yy,short Dir,byte Prio,byte Type);
void			MakeReformation(byte NI,word BrigadeID,byte FormType);
void			CopyReIm(byte NI);
void			ImClearSelection(byte Nat);
void			BSetSQ(int x,int y,int Lx,int Ly);		// Set   locks in area (in all layers) (pixel coord / 16)
void			BClrSQ(int x,int y,int Lx,int Ly);		// Clear locks in area (in all layers) (pixel coord / 16)
CEXPORT int vdf_GetAmountOfSettlements(byte Owner,	int ResType, bool CheckUpgrades, int Level); // COSS 2
CEXPORT	  void	StartAIEx(byte Nat,char* Name,int Land,int Money,int ResOnMap,int Difficulty);
DLLEXPORT void	ShowVictory(char* Message=NULL, byte NI=0xFF);
DLLEXPORT void	LooseGame(char* Message=NULL, byte NI=0xFF);
CEXPORT	  void	FreezeGame();
CEXPORT	  void	UnFreezeGame();
DLLEXPORT void	ChangeFriends(byte Nat,byte Flags);
void  AddUnitToSelected(byte NI,OneObject* OB);
DLLEXPORT void	ClearSelection(byte Nat);
DLLEXPORT void	AddUnitToSel(int Index);
DLLEXPORT int	GetDiff(byte NI);
DLLEXPORT int	GetResource(byte Nat,byte ID);
DLLEXPORT void	SetResource(byte Nat,byte ID,int Amount);
DLLEXPORT int	GetOrderType(OneObject* OB);
DLLEXPORT void	OBJ_Rotate(word Index,byte Dir,byte OrdType);
DLLEXPORT bool	NationIsErased(byte Nat);
DLLEXPORT void	AddResource(byte Nat,byte ID,int Amount);
DLLEXPORT int	GetBrigadsAmount0(byte NI);
CEXPORT	  int	PutNewFormationBySizeIndex(byte NI, word UnitType, byte SizeIndex, int formT, int x, int y, byte Dir=0);
CEXPORT	  void	SetOrderedStateForComplexObject(OneObject* OB, int State);
bool			DetectArcher(GeneralObject* GO);
bool			DetectShortRangeUnit(GeneralObject* GO);
bool			DetectShootingUnit(GeneralObject* GO);
bool			DetectTamagavkUnit(GeneralObject* GO);
CEXPORT	void SetFreezeDipSysProcess(bool State);
CEXPORT	int GetFormationIndexBySizeIndex(byte NI, word UnitType, byte SizeIndex);
CEXPORT byte GetTribeOwner(char* MainGroup);
CEXPORT void ActivateTacticalAI(byte NI);
double	lvBE_GetVecFi(double x, double y)	//Угол между вектором (x,y) и (1,0); Fi>=0&&Fi<2*Pi
{
	if (x==0&&y==0)		return 0.0;
	
	const double Pi = 3.141592;

	double	dx = x;	if (x<0)	{ dx = -x; };
	double	dy = y;	if (y<0)	{ dy = -y; };


	//x==0||y==0
	if (dx<=0.01&&dy<=0.01)	{ return 0.0;			};
	if (dx<=0.01&&y>0.0)	{ return Pi/2.0;		};
	if (dx<=0.01&&y<0.0)	{ return (3.0*Pi)/2.0;	};
	if (dy<=0.01&&x>0.0)	{ return 0.0;			};
	if (dy<=0.01&&x<0.0)	{ return Pi;			};

	//x<>0&&y<>0
	double	Fi = acos(abs(x)/sqrt(x*x+y*y));

	if (x<0.0&&y>0.0)		{ Fi = Pi-Fi;		};
	if (x<0.0&&y<0.0)		{ Fi = Pi+Fi;		};
	if (x>0.0&&y<0.0)		{ Fi = 2.0*Pi-Fi;	};

	return	Fi;
};
int		lvBE_GetDirec(int x0,int y0){
	double dFi = lvBE_GetVecFi(double(x0),double(y0));
	int Fi = (int)((dFi/(2.0*3.1415))*255.0);
	return Fi;
};	
int		lvBE_GetGrpDir(int grpID,int &x,int &y) //Вычисление направления группы
{
	int Nu=AGroups[grpID]->Units.GetAmount();
	byte bd_;
	if (Nu==0)	return -1;
	while (Nu--) {
		word UID=AGroups[grpID]->Units[Nu].ID;
		if(UID!=0xFFFF){
			OneObject* OB=Group[UID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[Nu].SN){
				if (OB->BrigadeID!=0xFFFF){
					Brigade* BR=&CITY[OB->NNUM].Brigs[OB->BrigadeID];
					BR->GetCenter(&x,&y,&bd_);
				};
			};
		};
	};
	return bd_;
};
bool	lvBE_TruePrior(OneObject* OB,int Dir){
	if (GetOrderType(OB)==Dir){
		return true;
	}else{
		return false;
	}
};
bool	lvBE_TrueGrpPrior(int grpID,int Dir,int percent){
	int ret=0;
	int Nu=AGroups[grpID]->Units.GetAmount();
	if (Nu==0)	return false;
	int N=0;
	while (Nu--) {
		word UID=AGroups[grpID]->Units[Nu].ID;
		if(UID!=0xFFFF){
			OneObject* OB=Group[UID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[Nu].SN){
				if (!lvBE_TruePrior(OB,Dir)) {
					ret++;
				};
				N++;
			};
		};
	};
	int res=100*ret/N;
	return res<percent;
};
extern	ClassArray<ActiveGroup> AGroups;
bool	lvBE_GrpInZone(int grpID,int x,int y,int R){
	if (AGroups.GetAmount()<grpID||grpID<0)	return false;
	int Nu=AGroups[grpID]->Units.GetAmount();
	if (Nu==0)	return false;
	word MID=0;
	bool	ret = true;
	while (ret&&Nu--) {
		MID=AGroups[grpID]->Units[Nu].ID;
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[Nu].SN){
				if (Norma(OB->RealX/16-x,OB->RealY/16-y)>R) {
					ret = false;
				};
			};
		};
	};
	return ret;
};

bool	lvBE_GrpFree(int grpID,int percent=0){
	if (AGroups.GetAmount()<grpID||grpID<0)	return false;
	int Nu=AGroups[grpID]->Units.GetAmount();
	if (Nu==0)	return false;
	int NNN = 0;
	word MID=0;
	int	ret = 0;
	while (Nu--) {
		MID=AGroups[grpID]->Units[Nu].ID;
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[Nu].SN){
				NNN++;
				if (((OB->StandTime>0)&&(!OB->Attack))&&(OB->DestX==-1)) {
					ret++;
				};		
			};
		};
	};
	if (NNN==0)	return false;
	int res=100*ret/NNN;
	return res>(100-percent);
};

void	lvBE_ClearSelection(int grpID){
	if (AGroups.GetAmount()<grpID||grpID<0)	return;
	int Nat=-1;
	int Nu=AGroups[grpID]->Units.GetAmount();
	if (Nu==0)	return;
	while(Nu--){
		word MID=AGroups[grpID]->Units[Nu].ID;
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[Nu].SN){
				ClearSelection(OB->NNUM);
				return;
			};
		};
	};
};

void	lvBE_SelectGroup(int grpID){	
	if (AGroups.GetAmount()<grpID||grpID<0)	return;
	int Nu=AGroups[grpID]->Units.GetAmount();
	if (Nu==0)	return;
	byte NI=0xFF;
	for(int i=0;i<Nu;i++){
		word MID=AGroups[grpID]->Units[i].ID;
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[i].SN){
				AddUnitToSel(OB->Index);
				NI=OB->NNUM;				
			};			
		};
	};
	if(NI!=0xFF) CopyReIm(NI);
};

void	lvBE_SelSendGroupToXY(int grpID, int x, int y, int dir, int prior, int ndX=0, int ndY=0){
	if (AGroups.GetAmount()<grpID||grpID<0)	return;
	int Nu=AGroups[grpID]->Units.GetAmount();
	if (Nu==0)	return;
	while(Nu--){
		word MID=AGroups[grpID]->Units[Nu].ID;
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[grpID]->Units[Nu].SN){
				if (dir<0) {
					SendSelectedToXY(OB->NNUM,x<<4,y<<4,lvBE_GetDirec(x-ndX,y-ndY),16,prior);
					return;
				}else{
					SendSelectedToXY(OB->NNUM,x<<4,y<<4,dir,16,prior);
					return;
				};
			};
			};
	};
};

void	lvBE_SendGroupToXY(int grpID, int x, int y, int dir, int prior, int ndX=0, int ndY=0){
	lvBE_SelectGroup(grpID);
	lvBE_SelSendGroupToXY(grpID,x,y,dir,prior,ndX,ndY);
};
void	lvBE_TakeResource(word UID,int ResID,int SN){
	if(UID!=0xFFFF){
		OneObject* OB=Group[UID];
		if (OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==SN&&(!OB->LocalOrder)&&OB->newMons->Peasant) {
			OB->TakeResource(OB->RealX>>4,OB->RealY>>4,ResID,128,0);
		};
	};
};
bool	lvBE_GroupInNodeFree(int grpID, int x, int y, bool vGrp){
	if (vGrp) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(grpID);
		if (pvGRP) {
			int Nu=pvGRP->GetTotalAmount();
			if (Nu==0) return false;
			int NU;
			NU=pvGRP->GetAmountInZone(x, y, GetMinR_ByNUnits(Nu));
			if ( (Nu==NU) && (pvGRP->GetORDER()==vgNO_ORDERS) ) return true;
			return false;
		}else{
			return false;
		}
	}else{
		if ( AGroups[grpID]!=NULL ) {
			if ( lvBE_GrpInZone(grpID,x,y,BE_MinR) && lvBE_GrpFree(grpID,0) ) return true;
			return false;
		};
		return false;
	};
	return false;
};
bool	lvBE_GroupInNode(int grpID, int x, int y, bool vGrp,int* N=NULL){
	if (vGrp) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(grpID);
		if (pvGRP) {
			int Nu=pvGRP->GetTotalAmount();
			if (Nu==0) {
				return false;
			};
			int NU;
			NU=pvGRP->GetAmountInZone(x, y, GetMinR_ByNUnits(Nu));
			if (N!=NULL)	*N=NU;
			if ((Nu==NU)) {
				return true;
			}else{
				return false;
			}
		}else{
			return false;
		}
	}else
		if ((AGroups[grpID]!=NULL)) {
			if (lvBE_GrpInZone(grpID,x,y,BE_MinR)) {
				return true;
			}else{
				return false;
			};
		}else return false;
};
int		GetAmountOfMoversTo(OneObject* Dest,OneObject* Mover=NULL);
// GLOBAL ////////////////////////////////////////////////////////////////
void SetIllusion(OneObject* OBJ,bool State);
void SetInvisiblen(OneObject* OBJ,bool State);
bool	CheckFilePath(char* _FilePath){
	if (_FilePath==NULL)	return false;
	FILE* file = fopen(_FilePath,"r");
	bool ret=(file!=NULL);
	if (file)	fclose(file);
	return ret;
};
bool	CheckFilePath(_str& _FilePath){
	return CheckFilePath(_FilePath.str);
};
struct lvSSum {
	int Sum;
};
struct lvSSumb {
	int Sum;
	bool bHome;
};
struct lvSSumNat {
	int Sum;
	int Nat;
};
struct lvSSumNatType {
	int Sum;
	int Nat;
	int TypeID;
};
struct lvSSumGr {
	int GrpID;
	int Sum;
};
class  lvSSumSquad {
public:
	lvSSumSquad(){};
	~lvSSumSquad(){ arrBrID.Clear(); };
	bool	checkBrID(int BrID){
		if (BrID==0xFFFF)	return false;
		if (Nat<0||Nat>7)	return false;
		Brigade* pBR=&CITY[Nat].Brigs[ BrID ];
		return ( (pBR!=NULL) && (pBR->Enabled==true) );
	};
	bool	addID(OneObject* pOB){ 
		if (pOB==NULL||pOB->Sdoxlo||pOB->NNUM!=Nat)	return false;
		if (checkBrID(pOB->BrigadeID)==false)	return false;
		bool newID=true; 
		int N=getAmount(); 
		while (N--){ 
			if(arrBrID[N]==pOB->BrigadeID) { newID=false; N=0; };
		}; 
		if (newID)	arrBrID.Add(pOB->BrigadeID);
		return newID;
	};
	int		getAmount()		{ return arrBrID.GetAmount(); };
	LinearArray<int,_int>	arrBrID;	// list of brig id in zone 
	int						Nat;
};
// Add Brigade id in list if unit in Brigade
bool	AddBrIDifPresent(OneObject* OB,void* param){
	lvSSumSquad* pPar = (lvSSumSquad*)(param);
	pPar->addID(OB);
	return true;
};
bool	AddUnitToSumHide(OneObject* OB,void* param){
	lvSSumb* pPar = reinterpret_cast<lvSSumb*>(param);
	if(OB&&(!OB->Sdoxlo)){
		if (OB->NewBuilding==false) {
		//	OB->Invisible = true;
			SetInvisiblen(OB,true);
		}else if (pPar->bHome) {
		//	OB->Invisible = true;
			SetInvisiblen(OB,true);
		};
		//OB->Illusion = true;
		SetIllusion(OB,true);
		(pPar->Sum)++;
	};
	return true;
};
bool	AddUnitToSumUnHide(OneObject* OB,void* param){
	lvSSum* pPar = reinterpret_cast<lvSSum*>(param);
	if(OB&&(!OB->Sdoxlo)){
		//OB->Invisible = false;
		SetInvisiblen(OB,false);
		//OB->Illusion = false;
		SetIllusion(OB,false);
		(pPar->Sum)++;
	};
	return true;
};
bool	AddUnitToSumN(OneObject* OB,void* param){
	lvSSumNat* pPar = reinterpret_cast<lvSSumNat*>(param);
	if(OB&&OB->NNUM==pPar->Nat&&(!OB->Sdoxlo)&&OB->NewBuilding==false){
		(pPar->Sum)++;
	};
	return true;
};
bool	AddUnitToSumType(OneObject* OB,void* param){
	lvSSumNatType* pPar = reinterpret_cast<lvSSumNatType*>(param);
	if(OB&&(!OB->Sdoxlo)&&OB->NNUM==pPar->Nat&&OB->NIndex==pPar->TypeID&&OB->NewBuilding==false){
		(pPar->Sum)++;
	};
	return true;
};
bool	AddUnitToSumGr(OneObject* OB,void* param){
	lvSSumGr* pPar = reinterpret_cast<lvSSumGr*>(param);

	if (pPar->GrpID<AGroups.GetAmount()) {
		int Nu=AGroups[pPar->GrpID]->Units.GetAmount();
		for(int i=0;i<Nu;i++){
			if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Index==AGroups[pPar->GrpID]->Units[i].ID&&OB->Serial==AGroups[pPar->GrpID]->Units[i].SN&&OB->NewBuilding==false){
				(pPar->Sum)++;
			};
		};
	};
	return true;
};

void	FilterUnitsByCategory(lvCGroup* pvg_Src,lvCGroup* pvg_Dst,int Ctg){
	if (pvg_Src==NULL||pvg_Dst==NULL)	return;
	
	int N=pvg_Src->GetTotalAmount();
	OneObject*	pOB = NULL;
	bool		ok = false;
	for (int i=0;i<N;i++){
		pOB = pvg_Src->GetOneObj_lID(i);
		if (pOB!=NULL){
			ok=false;
			switch(Ctg){
				case 0://archer
					ok=DetectArcher(pOB->Ref.General);
					break;
				case 1://short range unit
					ok=DetectShortRangeUnit(pOB->Ref.General);
					break;
				case 2://shooter
					ok=DetectShootingUnit(pOB->Ref.General);
					break;
				case 3://pesants
					ok=pOB->newMons->Peasant;
					break;
				case 4://not hiden
					ok=!pOB->Hidden;
					break;
				case 5://tomagavk
					ok=DetectTamagavkUnit(pOB->Ref.General);
					break;
			};
			if (ok){
				pvg_Src->RemUnitGPT(pOB);
				pvg_Dst->AddUnitGPT(pOB);
			};
			pOB=NULL;
		};
	};
};

void	BClrBar_InR(int x0,int y0,int R){
	int x16 = (int)((float)x0/16.f+.5f);
	int y16 = (int)((float)y0/16.f+.5f);
	
	int Lx = R/16;

	BClrBar(x16,y16,Lx);
};

bool	KillUnitsinZone(OneObject* OB,void* param){
	lvSSumNat* pPar = reinterpret_cast<lvSSumNat*>(param);
	if(OB&&OB->NNUM==pPar->Nat&&(!OB->Sdoxlo)&&(OB->NewBuilding==false)){
		OneObject* pOB = NULL;
			pOB=OB;
			if(pOB){
				pOB->delay=6000;
				if(pOB->LockType==1)pOB->RealDir=32;
				pOB->Die();
				pOB=OB;
				if(pOB){
					pOB->Sdoxlo=2500;
				};
			};
			pOB = NULL;
		};
	(pPar->Sum)++;
	return true;
};

//////////////////////////////////////////////////////////////////////////
//						PRO MISSION										//
//////////////////////////////////////////////////////////////////////////
CPRO_MISS_FILER	g_PMF;
const	char*	COPERCOND_CALLDESCR::GetThisElementView(const char* LocalName){
	_str myview; myview=NCALL; return myview.str;
};
void	COPERCOND_CALLDESCR::saveToFile(FILE* file){
	if (file!=NULL){
		fprintf(file,"---------------------------------------\n");
		fprintf(file,"DESCR : %s\n",DESCR.str);
		fprintf(file,"VIEW  : %s\n",VIEW.str );
		fprintf(file,"NCALL : %d\n",NCALL    );
	};
};
void	CPRO_MISS_FILER::addCALL(int ID){
	if (0<=ID&&ID<_SIZE)	DATA[ID]->NCALL += 1;
};
void	CPRO_MISS_FILER::clearCALL_DATA(){
	DATA.Clear();
	_SIZE=0;
};
void	CPRO_MISS_FILER::CCLEAR::EvaluateFunction(){
	g_PMF.clearCALL_DATA();
};
void	CPRO_MISS_FILER::clearSD(){
	int N=SORT_DATA.GetAmount();
	while (N--) {
		SORT_DATA[N]=NULL;
	};
	SORT_DATA.Clear();
};
void	CPRO_MISS_FILER::setSD_MoreThenNCall(int NCall){
	clearSD();
	for (int i=0; i<_SIZE; i++){
		if (DATA[i]->NCALL>=NCall)	SORT_DATA.Add( DATA[i] );
	};
};
void	CPRO_MISS_FILER::CPARM_DEPEND_ONLY::EvaluateFunction(){
	g_PMF.setSD_MoreThenNCall(MoreThenNCall);	
};
void	CPRO_MISS_FILER::Save_log(char* FName){
	if (FName!=NULL){
		FILE* file = fopen(FName,"w");
		assert(file!=NULL);
		if (file) {
			int N=SORT_DATA.GetAmount();
			for (int i=0; i<N; i++){
				SORT_DATA[i]->saveToFile(file);
			};
			fprintf(file,"---------------------------------------\n");
			fclose(file);
		};
	};
};
void	CPRO_MISS_FILER::CSAVE_LOG::EvaluateFunction(){
	g_PMF.Save_log(FName.str);
};
//////////////////////////////////////////////////////////////////////////
//======================================================================//
//=================    OPERATION FOR SCRIPT		========================//
//======================================================================//
lvCBaseOperCond::lvCBaseOperCond(){
	InfID = _lvCBaseOperCond_;
//	use_vGroup=DriveMode()->USE_vGRP;
	use_vGroup=true;
	first=true;
	UseNode = false;
};
lvCBaseOperCond::lvCBaseOperCond(lvCBaseOperCond* pBaseOperCond){
	InfID = _lvCBaseOperCond_;
	use_vGroup=true;
	first=true;
	if (pBaseOperCond!=NULL) {
		InfID			= pBaseOperCond->InfID;
		Descr			= pBaseOperCond->Descr.str;
		x0				= pBaseOperCond->x0;
		y0				= pBaseOperCond->y0;
		x1				= pBaseOperCond->x1;
		y1				= pBaseOperCond->y1;
		squardID		= pBaseOperCond->squardID;
		timeInProc		= pBaseOperCond->timeInProc;
		use_vGroup		= pBaseOperCond->use_vGroup;
		first			= pBaseOperCond->first;
		UseNode			= pBaseOperCond->UseNode;
		parNode			= pBaseOperCond->parNode;
	};
};

void			lvCBaseOperCond::GetCopy(lvCBaseOperCond** pCopy){
	*pCopy = new lvCBaseOperCond(this);
}

// lvCOperation //////////////////////////////////////////////////////////
int		lvCOperation::Process(int time){ 
	if (g_PMF.USE_CALL_OPERATION){
		if (myID==-1)	myID=g_PMF.getNEW_SELL<lvCOperation>(dynamic_cast<BaseClass*>(this));
		g_PMF.addCALL(myID);
	};
	AddIteration(); 
	CheckIteration(); 
	return 0; 
};
void			lvCOperation::GetCopy(lvCOperation** pCopy){
	*pCopy = new lvCOperation(this);
};

void			lvCOperation::CheckIteration()	{ 
	if (RepeatInTime&&CurIter>50) { 
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP!=NULL&&pvGRP->GetORDER()!=vgATTACK) {
			CurIter=0; first=true; 
		};
	}; 
};
char*			lvCOperation::GetSourceCode(int shift/* = 0*/){
	GetThisElementView(NULL);
	return Descr.str;
};
// lvCKillNatinZone //////////////////////////////////////////////////////////
lvCKillNatinZone::lvCKillNatinZone(lvCKillNatinZone* pKillNatinZone) : lvCOperation(dynamic_cast<lvCOperation*>(pKillNatinZone)) {
	if (pKillNatinZone!=NULL){
		parNat=pKillNatinZone->parNat;
		parZone=pKillNatinZone->parZone;
	};
};

void			lvCKillNatinZone::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCKillNatinZone(this));
};

const char*		lvCKillNatinZone::GetThisElementView(const char* LocalName){
	Descr="";
	if((0<=parNat&&parNat<8)&&(0<AZones.GetAmount()&&parZone<AZones.GetAmount())&&(AZones[parZone]!=NULL)){
		Descr += "KillNatinZone(";
		Descr += parNat;
		Descr += ", ";
		Descr += AZones[parZone]->Name.str;
		Descr += ")";
	}else{
		Descr="KillNatinZone(NULL)";
	};
	return Descr.str;
};
int				lvCKillNatinZone::Process(int time){
	lvCOperation::Process(time);
	bool ff;
	int xxx=-1;
	int yyy=-1;
	int RRR=-1;
	if ( UseNode==true ){
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL){
			xxx = pNode->vGetX();
			yyy = pNode->vGetY();
			RRR = pNode->vGetR();
		};
	}else{
		if ((0<=parNat&&parNat<8)&&(0<AZones.GetAmount()&&parZone<AZones.GetAmount())&&(AZones[parZone]!=NULL)&&(0<AZones[parZone]->R)) {
			xxx = AZones[parZone]->x;
			yyy = AZones[parZone]->y;
			RRR = AZones[parZone]->R;
		};
	};
	if (xxx!=-1&&yyy!=-1&&RRR!=-1)	return KillNatinInPOS_lua(parNat,xxx,yyy,RRR);
	return 0;
};
int KillNatinInPOS_lua(int nat,int x,int y,int R){
	lvSSumNat	Ret;	Ret.Sum = 0; Ret.Nat=nat;
	if ( 0<=nat&&nat<8 ) {
		Ret.Nat = nat;
		PerformActionOverUnitsInRadius(x,y,R,KillUnitsinZone,&Ret);
	};
	return 1;
}
// lvCKillNatNear //////////////////////////////////////////////////////////
lvCKillNatNear::lvCKillNatNear(lvCKillNatNear* pKillNatNear) : lvCOperation(dynamic_cast<lvCOperation*>(pKillNatNear)) {
	if (pKillNatNear!=NULL){
		parNat=pKillNatNear->parNat;
		vGrp=pKillNatNear->vGrp;
		parRad=pKillNatNear->parRad;
	};
};

void			lvCKillNatNear::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCKillNatNear(this));
};

const char*		lvCKillNatNear::GetThisElementView(const char* LocalName){
	Descr="";
	if((0<=parNat&&parNat<8)&&(0<parRad)){
		Descr += "KillNatNear(";
		Descr += parNat;
		Descr += ", ";
		if(use_vGroup){
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(vGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			}else{
				Descr+="NULL";
			};
		}else{
			Descr+="NULL";
		};
		Descr += ", ";
		Descr += parRad;
		Descr += ")";
	}else{
		Descr="lvCKillNatNear(NULL)";
	};
	return Descr.str;
};
int				lvCKillNatNear::Process(int time){
	lvCOperation::Process(time);
	bool ff;
	lvSSumNat	Ret;	Ret.Sum = 0; 
	if ((0<=parNat&&parNat<8)&&(0<parRad)) {
		Ret.Nat = parNat;
		int vgX;
		int vgY;
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(vGrp);
			if (pvGRP) {
				pvGRP->GetGroupCenter(vgX,vgY);
				PerformActionOverUnitsInRadius(	vgX,
					vgY,
					parRad,
					KillUnitsinZone,
					&Ret			 );
			};
		};
	};
	if(!ff) ff=true;
	if (ff) {
		return 1;
	}else{
		return 0;
	};
};
// lvCSelectAll //////////////////////////////////////////////////////////
lvCSelectAll::lvCSelectAll(lvCSelectAll* pSelectAll) : lvCOperation(dynamic_cast<lvCOperation*>(pSelectAll)) {
	if (pSelectAll!=NULL){
		parNat=pSelectAll->parNat;
	};
};

void			lvCSelectAll::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSelectAll(this));
};

const char*		lvCSelectAll::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parNat&&parNat<8){
		Descr += "SelectAll(";
		Descr += parNat;
		Descr += ")";
	}else{
		Descr="SelectAll(NULL)";
	};
	return Descr.str;
};
int				lvCSelectAll::Process(int time){
	lvCOperation::Process(time);
	return SelectAll_lua(parNat);
};
int SelectAll_lua(int nat){
	bool ff;
	OneObject*	pUnit = NULL;
	ClearSelection(nat);
	for (int i=0; i<MAXOBJECT; i++){
		pUnit = Group[i];
		if (pUnit&&!pUnit->Sdoxlo) {
			if (pUnit->NNUM==nat) {
				AddUnitToSelected(nat,pUnit);
				if(!ff) ff=true;
			};
		};
	};
	if (ff) {
		return 1;
	}else{
		return 0;
	};
}
// lvCChangeAS //////////////////////////////////////////////////////////
lvCChangeAS::lvCChangeAS(lvCChangeAS* pChangeAS) : lvCOperation(dynamic_cast<lvCOperation*>(pChangeAS)) {
	if (pChangeAS!=NULL){
		parNat=pChangeAS->parNat;
		parState=pChangeAS->parState;
	};
};

void			lvCChangeAS::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCChangeAS(this));
};

const char*		lvCChangeAS::GetThisElementView(const char* LocalName){
	Descr="";
	Enumerator* E=ENUM.Get("BE_UNIT_MOVE_MODE");
	if(0<=parNat&&parNat<8){
		Descr += "ChangeActiveState(";
		Descr += parNat;
		Descr += ", ";
		Descr += E->GetStr(parState);
		Descr += ")";
	}else{
		Descr="ChangeActiveState(NULL)";
	};
	return Descr.str;
};
int				lvCChangeAS::Process(int time){
	lvCOperation::Process(time);
	return ChangeAS_lua(parNat,parState);
};
int ChangeAS_lua(int nat,int state){
	bool ff;
	OneObject*	pUnit = NULL;
	ClearSelection(nat);
	for (int i=0; i<MAXOBJECT; i++){
		pUnit = Group[i];
		if (pUnit&&!pUnit->Sdoxlo) {
			if (pUnit->NNUM==nat) {
				pUnit->ActivityState=state;
				if(!ff) ff=true;
			};
		};
	};
	if (ff) {
		return 1;
	}else{
		return 0;
	};
}
// lvCSelSendTo //////////////////////////////////////////////////////////
lvCSelSendTo::lvCSelSendTo(lvCSelSendTo* pSelSendTo) : lvCOperation(dynamic_cast<lvCOperation*>(pSelSendTo)) {
	if (pSelSendTo!=NULL) {
		parNat	= pSelSendTo->parNat;
		parZn	= pSelSendTo->parZn;
		parDir	= pSelSendTo->parDir;
		parType	= pSelSendTo->parType;
	};
};

void			lvCSelSendTo::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSelSendTo(this));
};

const char*		lvCSelSendTo::GetThisElementView(const char* LocalName){
	Descr="";
	if((0<=parNat&&parNat<8)&&(0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)){
		Descr += "SelSendTo(";
		Descr += parNat;
		Descr += ", ";
		Descr += AZones[parZn]->Name.str;
		Descr += ", ";
		Descr += parDir;
		Descr += ", ";
		Descr += parType;
		Descr += ")";
	}else{
		Descr="lvCSelSendTo(NULL)";
	};
	return Descr.str;
};
int				lvCSelSendTo::Process(int time){
	lvCOperation::Process(time);
	if ((0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)){
		ActiveZone* AZ=AZones[parZn];
		SendSelectedToXY(parNat,AZ->x<<4,AZ->y<<4,parDir,16,parType);
	};
	return 0;
};
int SelSendTo_lua(int nat,int x,int y,int dir,int type){
	SendSelectedToXY(nat,x<<4,y<<4,dir,16,type);
	return 1;
}
// lvCGroupSendTo ////////////////////////////////////////////////////////
lvCGroupSendTo::lvCGroupSendTo(lvCGroupSendTo* pGroupSendTo) : lvCOperation(dynamic_cast<lvCOperation*>(pGroupSendTo)) {
	if (pGroupSendTo!=NULL) {
		parGrp		= pGroupSendTo->parGrp;
		parZn		= pGroupSendTo->parZn;	
		parDir		= pGroupSendTo->parDir;
		parType		= pGroupSendTo->parType;
	};
};

void			lvCGroupSendTo::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCGroupSendTo(this));
};

const char*		lvCGroupSendTo::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parGrp){
		Descr += "GroupSendTo(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ", ";
		if (UseNode){
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode!=NULL)	Descr += pNode->vGetName();
			else				Descr += "NoNode";
		}else if( 0<AZones.GetAmount() && parZn<AZones.GetAmount() && AZones[parZn]!=NULL ){
			Descr += AZones[parZn]->Name.str;
		}else Descr += "NoZone";
		Descr += ", ";
		Descr += parDir;
		Descr += ", ";
		Descr += parType;
		Descr += ")";
	}else{
		Descr="lvCGroupSendTo(NULL)";
	};
	return Descr.str;
};
int				lvCGroupSendTo::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		int xxx=-1;
		int	yyy=-1;
		int RRR=-1;
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode!=NULL) {
				xxx = pNode->vGetX();
				yyy = pNode->vGetY();
				RRR = pNode->vGetR();
			};
		}else if ((0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&AZones[parZn]!=NULL){
			ActiveZone* AZ=AZones[parZn];
			xxx = AZ->x;
			yyy = AZ->y;
			RRR = AZ->R;
		};

		if (xxx!=-1&&yyy!=-1&&RRR!=-1){
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				if (!pvGRP->GetORDER()||(parType==0&&first)||pvGRP->newElement||RepeatInTime){
					if (pvGRP->newElement)	pvGRP->newElement=false;
					if ((pvGRP->GetDirection()!=parDir) || pvGRP->GetAmountInZone(xxx,yyy,RRR)<=pvGRP->GetTotalAmount()){
						pvGRP->SendTo(xxx,yyy,parDir);
						if (first) first=false;
						return 1;
					};
				};
			};
		};

	}else
	if ((0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL) && (AGroups[parGrp]!=NULL)){
		lvBE_ClearSelection(parGrp);
		lvBE_SelectGroup(parGrp);
		ActiveZone* AZ=AZones[parZn];
		if (first || (!lvBE_GrpInZone(parGrp,AZ->x,AZ->y,AZ->R)&&lvBE_GrpFree(parGrp,10)) ) {
			lvBE_SelSendGroupToXY(parGrp,AZ->x,AZ->y,parDir,parType,x0,y0);
			if (first) first=false;
		};
	};
	return 0;
};
// lvCSelSendToNode //////////////////////////////////////////////////////
lvCSelSendToNode::lvCSelSendToNode(lvCSelSendToNode* pSelSendToNode) : lvCOperation(dynamic_cast<lvCOperation*>(pSelSendToNode)) {
	if (pSelSendToNode!=NULL) {
		parNat		= pSelSendToNode->parNat;
		parDir		= pSelSendToNode->parDir;
		parType		= pSelSendToNode->parType;
	};
};

void			lvCSelSendToNode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSelSendToNode(this));
};

const char*		lvCSelSendToNode::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parNat&&parNat<8){
		Descr += "SelSendToNode(";
		Descr += parNat;
		Descr += ", ";
		Descr += parDir;
		Descr += ", ";
		Descr += parType;
		Descr += ")";
	}else{
		Descr="lvCSelSendToNode(NULL)";
	};
	return Descr.str;
};
int				lvCSelSendToNode::Process(int time){
	lvCOperation::Process(time);
	if (parDir<0) {
		SendSelectedToXY(parNat,x1<<4,y1<<4,lvBE_GetDirec(x1-x0,y1-y0),16,parType);
		return 1;
	}else{
		SendSelectedToXY(parNat,x1<<4,y1<<4,parDir,16,parType);
		return 1;
	};
	return 0;
};
// lvCGroupSendToNode ////////////////////////////////////////////////////
lvCGroupSendToNode::lvCGroupSendToNode(lvCGroupSendToNode* pGroupSendToNode) : lvCOperation(dynamic_cast<lvCOperation*>(pGroupSendToNode)) {
	if (pGroupSendToNode!=NULL) {
		parGrp		= pGroupSendToNode->parGrp;
		parDir		= pGroupSendToNode->parDir;	
		parType		= pGroupSendToNode->parType;
	};
};

void			lvCGroupSendToNode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCGroupSendToNode(this));
};

const char*		lvCGroupSendToNode::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parGrp){
		Descr += "GroupToNode(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ", ";
		Descr += parDir;
		Descr += ", ";
		Descr += parType;
		Descr += ")";
	}else{
		Descr="lvCGroupSendToNode(NULL)";
	};
	return Descr.str;
};
int				lvCGroupSendToNode::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			if (!pvGRP->GetORDER()){
				if (first || (!pvGRP->GetAmountInZone(x1,y1,GetMinR_ByNUnits(pvGRP->GetTotalAmount()))) || (pvGRP->GetDirection()!=parDir)){
					pvGRP->SendTo(x1,y1,parDir);
					if (first) first=false;
					return 1;
				};
			};
		};
	}else{
		if ((AGroups[parGrp]!=NULL)) {
			lvBE_ClearSelection(parGrp);
			lvBE_SelectGroup(parGrp);
			if (first || (!lvBE_GrpInZone(parGrp,x1,y1,BE_MinR)&&lvBE_GrpFree(parGrp,10)) ) {
				lvBE_SendGroupToXY(parGrp,x1,y1,parDir,parType,x0,y0);
				if (first) first=false;
			};
		};
	};
	return 0;
};
// lvCRotateGroup ////////////////////////////////////////////////////////
lvCRotateGroup::lvCRotateGroup(lvCRotateGroup* pRotateGroup) : lvCOperation(dynamic_cast<lvCOperation*>(pRotateGroup)) {
	if (pRotateGroup!=NULL) {
		parGrp	= pRotateGroup->parGrp;
		parDir	= pRotateGroup->parDir;
		prior	= pRotateGroup->prior;
	};
}

void			lvCRotateGroup::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCRotateGroup(this));
};
const char*		lvCRotateGroup::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parGrp){
		Descr += "RotateGroup(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ", ";
		Descr += parDir;
		Descr += ", ";
		Descr += prior;
		Descr += ")";
	}else{
		Descr="lvCRotateGroup(NULL)";
	};
	return Descr.str;
};
int				lvCRotateGroup::Process(int time){
	lvCOperation::Process(time);
	int	bx_,by_;
	bx_=x1;
	by_=y1;
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			if (!pvGRP->GetORDER()){
				if (first || (pvGRP->GetDirection()!=parDir) ) {
				//	pvGRP->GetGroupCenter(bx_,by_);
				//	pvGRP->SendTo(bx_,by_,parDir);
					pvGRP->ChangeDirection(parDir);
					if (first){ first=false;};
					return 1;
				};
			};
		};
	}else{
		if ((AGroups[parGrp]!=NULL)) {
			word MID=0;
			byte	bd_;
			if (first && (lvBE_GrpFree(parGrp,10)&&!lvBE_TrueGrpPrior(parGrp,parDir,10))) {
				int Nu=AGroups[parGrp]->Units.GetAmount();
				for(int i=0;i<Nu;i++){
					MID=AGroups[parGrp]->Units[i].ID;
					if(MID!=0xFFFF){
						OneObject* OB=Group[MID];
						if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[parGrp]->Units[i].SN){
							if(!lvBE_TruePrior(OB,parDir)){
								if (first && (OB->BrigadeID!=0xFFFF)){
									Brigade* BR=&CITY[OB->NNUM].Brigs[OB->BrigadeID];
									BR->GetCenter(&bx_,&by_,&bd_);
									first=false;
								};
								OBJ_Rotate(MID,parDir,prior);
							};
						};
					};
				};
				lvBE_SendGroupToXY(squardID,bx_,by_,parDir,prior,x0,y0);
				if (first) {first=false;};
			};
		};
	};
	return 0;
};
// lvCRotate /////////////////////////////////////////////////////////////
lvCRotate::lvCRotate(lvCRotate* pRotate) : lvCOperation(dynamic_cast<lvCOperation*>(pRotate)) {
	if (pRotate!=NULL) {
		parDir	= pRotate->parDir;
		prior	= pRotate->prior;
	};
};

void			lvCRotate::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCRotate(this));
};

const char*		lvCRotate::GetThisElementView(const char* LocalName){
	Descr="";
	if (parDir>=0){
		Descr += "Rotate(";
		Descr += parDir;
		Descr += ", ";
		Descr += prior;
		Descr += ")";
	}else{
		Descr="lvCRotate(NULL)";
	};
	return Descr.str;
};
int				lvCRotate::Process(int time){
	lvCOperation::Process(time);
	int	bx_,by_;
	bx_=x1;
	by_=y1;
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP) {
			if (!pvGRP->GetORDER()){
				if (first || abs(pvGRP->GetDirection()-parDir)>20 ) {
				//	pvGRP->GetGroupCenter(bx_,by_);
				//	pvGRP->SendTo(bx_,by_,parDir);
					pvGRP->ChangeDirection(parDir);
					if (first){ first=false;};
					return 1;
				};
			};
		};
	}else{
		if ((AGroups[squardID]!=NULL)) {
			word MID=0;
			if (first && (lvBE_GrpFree(squardID,10)&&!lvBE_TrueGrpPrior(squardID,parDir,10))) {
				int Nu=AGroups[squardID]->Units.GetAmount();
				for(int i=0;i<Nu;i++){
					MID=AGroups[squardID]->Units[i].ID;
					if(MID!=0xFFFF){
						OneObject* OB=Group[MID];
						if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Serial==AGroups[squardID]->Units[i].SN){
							if(lvBE_GetGrpDir(squardID,bx_,by_)!=parDir){
								if (first && (OB->BrigadeID!=0xFFFF)){
									Brigade* BR=&CITY[OB->NNUM].Brigs[OB->BrigadeID];
									first=false;
								};
								OBJ_Rotate(MID,parDir,prior);
							};
						};
					};
				};
				lvBE_SendGroupToXY(squardID,bx_,by_,parDir,prior,x0,y0);
				if (first) {first=false;};
			};
		};
	};
	return 0;
};

int				lvCRotate::Complite(){
	if (use_vGroup)	{
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP!=NULL && abs(pvGRP->GetDirection()-parDir)<=20 && !pvGRP->GetORDER())	return 1;
	};
	return 0;
};
// lvCSendToNode /////////////////////////////////////////////////////////
lvCSendToNode::lvCSendToNode(lvCSendToNode* pSendToNode) : lvCOperation(dynamic_cast<lvCOperation*>(pSendToNode)) {
	if (pSendToNode!=NULL) {
		parDir	 = pSendToNode->parDir;
		parType  = pSendToNode->parType;
	};
};

void			lvCSendToNode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSendToNode(this));
};

const char*		lvCSendToNode::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=squardID){
		Descr += "SendToNode(";
		Descr += parDir;
		Descr += ", ";
		Descr += parType;
		Descr += ")";
	}else{
		Descr="lvCSendToNode(NULL)";
	};
	return Descr.str;
};
int				lvCSendToNode::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP) {
			if (pvGRP->GetORDER()==vgNO_ORDERS || (parType==0 && first)) {
				if (	first 
						|| 
						pvGRP->GetAmountInZone(x1,y1,GetMinR_ByNUnits(pvGRP->GetTotalAmount()))!=pvGRP->GetTotalAmount() 
						||
						( parDir!=512 && GetCircleDif(pvGRP->GetDirection(),parDir)>32 ) 
				   )
				{
					pvGRP->SendTo(x1,y1,parDir);
					if (first) first=false;
					return 1;
				};
			};
		};
	}else{
		if ((AGroups[squardID]!=NULL)) {
			lvBE_ClearSelection(squardID);
			lvBE_SelectGroup(squardID);
			/*!lvBE_TrueGrpPrior(squardID,parDir,10) || */
			if (first || (lvBE_GrpFree(squardID,10)&&(!lvBE_GrpInZone(squardID,x1,y1,BE_MinR)))) {
				lvBE_SendGroupToXY(squardID,x1,y1,parDir,parType,x0,y0);
				if (first) first=false;
			};
		};
	};
	return 0;
};

int				lvCSendToNode::Complite(){
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP
			&&
			!pvGRP->GetORDER()
			&&
			pvGRP->GetAmountInZone(x1,y1,GetMinR_ByNUnits(pvGRP->GetTotalAmount()))==pvGRP->GetTotalAmount()
			)
		{
			if ( (parDir!=512 && GetCircleDif(pvGRP->GetDirection(),parDir)<=32) || parDir==512) return 1;
		};
	};
	return 0;
};
// lvCSelectUnits ////////////////////////////////////////////////////////
lvCSelectUnits::lvCSelectUnits(lvCSelectUnits* pSelectUnits) : lvCOperation(dynamic_cast<lvCOperation*>(pSelectUnits)) {
	if (pSelectUnits!=NULL) {
		parGrp = pSelectUnits->parGrp;
		parAdd = pSelectUnits->parAdd;
	};
};

void			lvCSelectUnits::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSelectUnits(this));
};

const char*		lvCSelectUnits::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parGrp){
		Descr += "SelectUnits(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ", ";
		if (parAdd) {
			Descr += "True";
		}else{
			Descr += "False";
		}
		Descr += ")";
	}else{
		Descr="lvCSelectUnits(NULL)";
	};
	return Descr.str;
};
int				lvCSelectUnits::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			if (!parAdd) {
				pvGRP->SelectUnits(0);
			}else{
				pvGRP->SelectUnits(1);
			};
		};
	}else{
		if ((AGroups[parGrp]!=NULL)) {
			if (!parAdd) {
				lvBE_ClearSelection(parGrp);
			};
			lvBE_SelectGroup(parGrp);
		};
	};
		return 0;
};
// lvCSetUnitState ///////////////////////////////////////////////////////
lvCSetUnitState::lvCSetUnitState(lvCSetUnitState* pSetUnitState) : lvCOperation(dynamic_cast<lvCOperation*>(pSetUnitState)) {
	if (pSetUnitState!=NULL) {
		parGrp	= pSetUnitState->parGrp;
		parmode = pSetUnitState->parmode;
	};
}

void			lvCSetUnitState::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetUnitState(this));
};

const char*		lvCSetUnitState::GetThisElementView(const char* LocalName){
	Descr="";
	Enumerator* E=ENUM.Get("BE_UNIT_MOVE_MODE");
	if(0<=parGrp){
		Descr += "SetUnitState(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if (AGroups[parGrp]!=NULL) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ", ";
		Descr += E->GetStr(parmode);
		Descr += ")";
	}else{
		Descr="lvCSetUnitState(NULL)";
	};
	return Descr.str;
};
int				lvCSetUnitState::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			if (pvGRP->SetAgresiveST(parmode)==1)	return 1;
		};
	}else{
		if ((AGroups[parGrp]!=NULL)) {
			int Nu=AGroups[parGrp]->Units.GetAmount();
			int NU=0;
			for(int i=0;i<Nu;i++){
				word MID=AGroups[parGrp]->Units[i].ID;
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if (OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Index==AGroups[parGrp]->Units[i].ID&&OB->Serial==AGroups[parGrp]->Units[i].SN) {
						OB->ActivityState=parmode;
					};
				};
			};
		};
	};
	return 0;
};
// lvCSetState ///////////////////////////////////////////////////////////
lvCSetState::lvCSetState(lvCSetState* pSetState) : lvCOperation(dynamic_cast<lvCOperation*>(pSetState)) {
	if (pSetState!=NULL) {
		parmode = pSetState->parmode;
	};
}

void			lvCSetState::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetState(this));
};

const char*		lvCSetState::GetThisElementView(const char* LocalName){
	Descr="";
	Enumerator* E=ENUM.Get("BE_UNIT_MOVE_MODE");
	if(0<=parmode){
		Descr += "SetState(";
		Descr += E->GetStr(parmode);
		Descr += ")";
	}else{
		Descr="lvCSetState(NULL)";
	};
	return Descr.str;
};
int				lvCSetState::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP) {
			if (pvGRP->SetAgresiveST(parmode)) return 1;		
		};
	}else{
		if ((AGroups[squardID]!=NULL)) {
			int Nu=AGroups[squardID]->Units.GetAmount();
			int NU=0;
			for(int i=0;i<Nu;i++){
				word MID=AGroups[squardID]->Units[i].ID;
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if (OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Index==AGroups[squardID]->Units[i].ID&&OB->Serial==AGroups[squardID]->Units[i].SN) {
						OB->ActivityState=parmode;
					};
				};
			};
		};
	};
	return 0;
};
// lvCReformation ////////////////////////////////////////////////////////
lvCReformation::lvCReformation(lvCReformation* pReformation) : lvCOperation(dynamic_cast<lvCOperation*>(pReformation)) {
	if (pReformation!=NULL) {
		TypeForm = pReformation->TypeForm;
	};
};

void			lvCReformation::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCReformation(this));
};

const char*		lvCReformation::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=TypeForm){
		Descr += "Reformation(";
		Descr += TypeForm;
		if (UseInNode) {
			Descr += ")";
		}else{
			Descr += ",";
			lvCGroup* pvGRP_A = GroupsMap()->GetGroupID(vGrp);
			if (pvGRP_A!=NULL&&pvGRP_A->NAME.str!=NULL)	Descr += pvGRP_A->NAME.str;
			else										Descr += "NoGroup";
			Descr += ")";
		};
		
	}else{
		Descr="lvCReformation(NULL)";
	};
	return Descr.str;
};
int				lvCReformation::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP) {
			if (first || pvGRP->GetORDER()==vgNO_ORDERS || UseInNode==true ){
				pvGRP->ChengeFormation(TypeForm);
				if (first)	first=false;
			}else if (UseInNode==false) {
				lvCGroup* pvGRP_A = GroupsMap()->GetGroupID(vGrp);
				if (pvGRP_A!=NULL)	pvGRP_A->ChengeFormation(TypeForm);
			};
		};
	}else{
		if ((AGroups[squardID]!=NULL)) {
			int Nu=AGroups[squardID]->Units.GetAmount();
			int NU=0;
			for(int i=0;i<Nu;i++){
				word MID=AGroups[squardID]->Units[i].ID;
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if (OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Index==AGroups[squardID]->Units[i].ID&&OB->Serial==AGroups[squardID]->Units[i].SN) {
						MakeReformation(OB->NNUM,OB->BrigadeID,TypeForm);
					};
				};
			};
		};
	};
	return TypeForm;
};
// lvCBrigReformation ////////////////////////////////////////////////////
lvCBrigReformation::lvCBrigReformation(lvCBrigReformation* pBrigReformation) : lvCOperation(dynamic_cast<lvCOperation*>(pBrigReformation)) {
	if (pBrigReformation!=NULL) {
		GrpID		= pBrigReformation->GrpID;
		TypeForm	= pBrigReformation->TypeForm;
	};
};

void			lvCBrigReformation::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCBrigReformation(this));
};

const char*		lvCBrigReformation::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=TypeForm&&0<=GrpID){
		Descr += "BrigReformation(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[GrpID]!=NULL)) {
				Descr += AGroups[GrpID]->Name.str;
			};
		};
		Descr += ", ";
		Descr += TypeForm;
		Descr += ")";
	}else{
		Descr="lvCBrigReformation(NULL)";
	};
	return Descr.str;
};
int				lvCBrigReformation::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			pvGRP->ChengeFormation(TypeForm);
		};
	}else{
		if ((AGroups[GrpID]!=NULL)) {
			int Nu=AGroups[GrpID]->Units.GetAmount();
			int NU=0;
			for(int i=0;i<Nu;i++){
				word MID=AGroups[GrpID]->Units[i].ID;
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if (OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Index==AGroups[GrpID]->Units[i].ID&&OB->Serial==AGroups[GrpID]->Units[i].SN) {
						MakeReformation(OB->NNUM,OB->BrigadeID,TypeForm);
					};
				};
			};
		}
	};
	return TypeForm;
};
// lvCChangeFriends //////////////////////////////////////////////////////
lvCChangeFriends::lvCChangeFriends(lvCChangeFriends* pChangeFriends) : lvCOperation(dynamic_cast<lvCOperation*>(pChangeFriends)) {
	if (pChangeFriends!=NULL) {
		Nation	= pChangeFriends->Nation;
		Value	= pChangeFriends->Value;
	};
};

void			lvCChangeFriends::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCChangeFriends(this));
};

const char*		lvCChangeFriends::GetThisElementView(const char* LocalName){
	Descr="";
	if( 0<=Nation&&Nation<8 ){
		Descr += "Change Friends(";
		Descr += Nation;
		Descr += ", ";
		Descr += Value;
		Descr += ")";
	}else{
		Descr="NoSetting";
	};
	return Descr.str;
};
int				lvCChangeFriends::Process(int time){
	lvCOperation::Process(time);
	return ChangeFriends_lua( Nation, Value );
};
int ChangeFriends_lua(int nat, int state){
	if( 0<=nat&&nat<8 ){
		ChangeFriends( nat, state );
		return 1;
	};	
	return 0;
};
// lvCSetFriends /////////////////////////////////////////////////////////
lvCSetFriends::lvCSetFriends(lvCSetFriends* pSetFriends) : lvCOperation(dynamic_cast<lvCOperation*>(pSetFriends)) {
	if (pSetFriends!=NULL) {
		fstNat	= pSetFriends->fstNat;
		secNat	= pSetFriends->secNat;		
	};
};

void			lvCSetFriends::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetFriends(this));
};

const char*		lvCSetFriends::GetThisElementView(const char* LocalName){
	Descr="";
	if( (0<=fstNat&&fstNat<8)&&(0<=secNat&&secNat<8) ){
		Descr += "Add Friends(";
		Descr += fstNat;
		Descr += ", ";
		Descr += secNat;
		Descr += ")";
	}else{
		Descr="NoSetting";
	};
	return Descr.str;
};
int				lvCSetFriends::Process(int time){
	lvCOperation::Process(time);
	return SetFriends_lua(fstNat,secNat);
};
int SetFriends_lua(int nat0,int nat1){
	int val=1;
	int Flags;
	if( (0<=nat0&&nat0<8)&&(0<=nat1&&nat1<8) ){
		for(int i=0;i<nat1;i++) val*=2;
		int i=MAXOBJECT;
		while (i--){
			OneObject* OB=Group[i];
			if(OB&&OB->NNUM==nat0){
				Flags=OB->NMask;
				break;
			};
		};
		if (i==-1) {
			return 0;
		}
		val|=Flags;
		ChangeFriends( nat0, val );
		return 1;
	};	
	return 0;
};
// lvCChangeNation ///////////////////////////////////////////////////////
lvCChangeNation::lvCChangeNation(lvCChangeNation* pChangeNation) : lvCOperation(dynamic_cast<lvCOperation*>(pChangeNation)) {
	if (pChangeNation!=NULL) {
		nwNat = pChangeNation->nwNat;
	};
};

void			lvCChangeNation::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCChangeNation(this));
};

const char*		lvCChangeNation::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "Change Nation(";
	if(0<=nwNat && nwNat<8){
		Descr += nwNat;
	}else{
		Descr+="NULL";
	};
	Descr += ")";
	return Descr.str;
};
int				lvCChangeNation::Process(int time){
	lvCOperation::Process(time);
	if(use_vGroup){
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(squardID);
		if (pvGRP) {
			pvGRP->SetNation(nwNat);
		};
		return nwNat;
	}else{
		return 0;
	};
};
// lvCChangeNationG //////////////////////////////////////////////////////
lvCChangeNationG::lvCChangeNationG(lvCChangeNationG* pChangeNationG) : lvCOperation(dynamic_cast<lvCOperation*>(pChangeNationG)) {
	if (pChangeNationG!=NULL) {
		GrpID = pChangeNationG->GrpID;
		nwNat = pChangeNationG->nwNat;
	};
};

void			lvCChangeNationG::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCChangeNationG(this));
};

const char*		lvCChangeNationG::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "Change Nation(";
	if(0<=nwNat && nwNat<8){
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[GrpID]!=NULL)) {
				Descr += AGroups[GrpID]->Name.str;
			};
		};
		Descr += ", ";
		Descr += nwNat;
	}else{
		Descr+="NULL";
	};
	Descr += ")";
	return Descr.str;
};
int				lvCChangeNationG::Process(int time){
	lvCOperation::Process(time);
	if(use_vGroup){
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			pvGRP->SetNation(nwNat);
		};
		return nwNat;
	}else{
		return 0;
	};
};
// lvCShowVictory ////////////////////////////////////////////////////////
lvCShowVictory::lvCShowVictory(lvCShowVictory* pShowVictory) : lvCOperation(dynamic_cast<lvCOperation*>(pShowVictory)) {
	if (pShowVictory!=NULL) {
		
	};
};

void			lvCShowVictory::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCShowVictory(this));
};

const char*		lvCShowVictory::GetThisElementView(const char* LocalName){
	Descr="ShowVictory(";
	if (TextID.str)	Descr+=TextID.str;
	Descr+=")";
	return Descr.str;
};
int				lvCShowVictory::Process(int time){
	lvCOperation::Process(time);
	if (first) {
		if (TextID.str)	ShowVictory( GetTextByID(TextID.str), Nat );
		else			ShowVictory();
		first=false;
		DriveMode()->PROCESS=false;
	};
	return 0;
};
int ShowVictory_lua(int nat,const char* TextID){
	if (TextID!=NULL){
        char* pText=(char*)malloc(strlen(TextID)+1);
		strcpy(pText,TextID);
		pText[strlen(TextID)]=0;
		ShowVictory( GetTextByID(pText), nat );
		free(pText);
	}else{
		ShowVictory();
	};
	DriveMode()->PROCESS=false;
	return 1;
};
// lvCLooseGame //////////////////////////////////////////////////////////
lvCLooseGame::lvCLooseGame(lvCLooseGame* pLooseGame) : lvCOperation(dynamic_cast<lvCOperation*>(pLooseGame)) {
	if (pLooseGame!=NULL) {

	};
};

void			lvCLooseGame::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCLooseGame(this));
};

const char*		lvCLooseGame::GetThisElementView(const char* LocalName){
	Descr="LooseGame(";
	if (TextID.str)	Descr+=TextID.str;
	Descr+=")";
	return Descr.str;
};
int				lvCLooseGame::Process(int time){
	lvCOperation::Process(time);
	if (first) {
		if (TextID.str)	LooseGame( GetTextByID(TextID.str), Nat );
		else			LooseGame();
		first=false;
		DriveMode()->PROCESS=false;
	};
	return 0;
};
int LooseGame_lua(int nat,const char* TextID){
	if (TextID!=NULL){
		char* pText=(char*)malloc(strlen(TextID)+1);
		strcpy(pText,TextID);
		pText[strlen(TextID)]=0;
		LooseGame( GetTextByID(pText), nat );
		free(pText);
	}else{
		LooseGame();
	};
	DriveMode()->PROCESS=false;
	return 1;
};
// lvCSetTrigg ///////////////////////////////////////////////////////////
lvCSetTrigg::lvCSetTrigg(lvCSetTrigg* pSetTrigg) : lvCOperation(dynamic_cast<lvCOperation*>(pSetTrigg)) {
	if (pSetTrigg!=NULL) {
		TID		= pSetTrigg->TID;
		TName	= pSetTrigg->TName;
		NewVal	= pSetTrigg->NewVal;
	};
};

void			lvCSetTrigg::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetTrigg(this));
};

const char*		lvCSetTrigg::GetThisElementView(const char* LocalName){
	Descr="";
	if(TID<=511){
		Descr += "SetTrigg(";
		Descr += TID;
		Descr += ", ";
		Descr += TName;
		Descr += ", ";
		Descr += NewVal;
		Descr += ")";
	}else{
		Descr = "lvCSetTrigg(NULL)";
	};
	return Descr.str;
};
int				lvCSetTrigg::Process(int time){
	lvCOperation::Process(time);
 	if(TID>511){
		return 0;
	};
	SCENINF.TRIGGER[TID]=~NewVal;
	return NewVal;
};
// lvCSetLightSpot ///////////////////////////////////////////////////////
lvCSetLightSpot::lvCSetLightSpot(lvCSetLightSpot* pSetLightSpot) : lvCOperation(dynamic_cast<lvCOperation*>(pSetLightSpot)) {
	if (pSetLightSpot!=NULL) {
		ZoneID	= pSetLightSpot->ZoneID;
		Radius	= pSetLightSpot->Radius;
		Index	= pSetLightSpot->Index;
	};
};

void			lvCSetLightSpot::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetLightSpot(this));
};

const char*		lvCSetLightSpot::GetThisElementView(const char* LocalName){
	Descr="";
	if((0<Radius)&&(0<AZones.GetAmount()&&ZoneID<AZones.GetAmount())&&(AZones[ZoneID]!=NULL)){
		Descr += "SetLightSpot(";
		Descr += AZones[ZoneID]->Name.str;
		Descr += ", ";
		Descr += Radius;
		Descr += ", ";
		Descr += Index;
		Descr += ")";
	}else{
		Descr="lvCSetLightSpot(NULL)";
	};
	return Descr.str;
};
int				lvCSetLightSpot::Process(int time){
	lvCOperation::Process(time);
	if(Index>63){
		return 0;
	};
	int x0=-1;
	int	y0=-1;
	if (UseNode){
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL) {
			x0=pNode->vGetX();
			y0=pNode->vGetY();
		};
	}else if ((0<AZones.GetAmount()&&ZoneID<AZones.GetAmount())&&(AZones[ZoneID]!=NULL)){
		ActiveZone* AZ=AZones[ZoneID];
		x0=AZ->x;
		y0=AZ->y;
	};

	if (x0!=-1&&y0!=-1){
		return SetLightSpot_lua(x0,y0,Radius,Index);
	};

	return 0;
};
int SetLightSpot_lua(int x,int y,int R,int index){
	if(index>63)	return 0;  
	SCENINF.LSpot[index].x=x;
	SCENINF.LSpot[index].y=y;
	SCENINF.LSpot[index].Type=R;
	return 1;
};
// lvCClearLightSpot /////////////////////////////////////////////////////
lvCClearLightSpot::lvCClearLightSpot(lvCClearLightSpot* pClearLightSpot) : lvCOperation(dynamic_cast<lvCOperation*>(pClearLightSpot)) {
	if (pClearLightSpot!=NULL) {
		Index = pClearLightSpot->Index;
	};
};

void			lvCClearLightSpot::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCClearLightSpot(this));
};

const char*		lvCClearLightSpot::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "ClearLightSpot(";
	Descr += Index;
	Descr += ")";
	return Descr.str;
};
int				lvCClearLightSpot::Process(int time){
	lvCOperation::Process(time);
	return ClearLightSpot_lua(Index);
};
int ClearLightSpot_lua(int index){
	if(index>63) return 0;
	SCENINF.LSpot[index].x=0;
	SCENINF.LSpot[index].y=0;
	SCENINF.LSpot[index].Type=0;
	return 1;
};
// lvCSetStartPoint //////////////////////////////////////////////////////
lvCSetStartPoint::lvCSetStartPoint(lvCSetStartPoint* pSetStartPoint) : lvCOperation(dynamic_cast<lvCOperation*>(pSetStartPoint)) {
	if (pSetStartPoint!=NULL) {
		Use_VVal = pSetStartPoint->Use_VVal;
		if (Use_VVal) {
			sX.Set(pSetStartPoint->sX.Get());
			sY.Set(pSetStartPoint->sY.Get());
		}else{
			ZoneID = pSetStartPoint->ZoneID;
		};
	};
};

void			lvCSetStartPoint::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetStartPoint(this));
};

const char*		lvCSetStartPoint::GetThisElementView(const char* LocalName){
	Descr = "SetStartPoint(";
	if (Use_VVal) {
		vvINTEGER*	psX = sX.Get();
		vvINTEGER*	psY = sY.Get();
		if (psX!=NULL&&psY!=NULL) {
			Descr += psX->GetName();
			Descr += ",";
			Descr += psY->GetName();
			Descr += ")";
		};
	}else{
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode!=NULL) {
					Descr += pNode->vGetName();
			}else	Descr += "NoNode";
		}else{
			if((0<AZones.GetAmount()&&ZoneID<AZones.GetAmount())&&(AZones[ZoneID]!=NULL)){
				Descr += AZones[ZoneID]->Name.str;
				Descr += ")";
			}else{
				Descr="NoZone";
			};
		};
	};	
	Descr += ")";
	return Descr.str;
};
int				lvCSetStartPoint::Process(int time){
	lvCOperation::Process(time);
	int x=0;
	int y=0;
	if (Use_VVal) {
		vvINTEGER*	psX = sX.Get();
		vvINTEGER*	psY = sY.Get();
		if (psX!=NULL&&psY!=NULL) {
			x = psX->Value;
			y = psY->Value;
		};
	}else{
		int xxx=-1;
		int yyy=-1;
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode!=NULL) {
				xxx=pNode->vGetX();
				yyy=pNode->vGetY();
			};
			return	SetStartPoint_lua(xxx,yyy);
		}else{
			if (0<=ZoneID&&ZoneID<AZones.GetAmount()){
				ActiveZone* AZ=AZones[ZoneID];
				if (AZ!=NULL) {
					xxx=AZ->x;
					yyy=AZ->y;
				};
			};
		};

		x=xxx;
		y=yyy;
		x=(x-(RealLx>>1))>>5;
		y=(y-RealLy)>>5;
		if(x<1)x=1;
		if(y<1)y=1;
		if(x+smaplx>msx-1)x=msx-smaplx-1;
		if(y+smaply>msy-1)y=msy-smaply-1;
	};
	if (x!=0||y!=0){
		void SetCameraPos(float x,float y);
		SetCameraPos((float)x,(float)y);
		return 1;
	};
	return 0;
};
DWORD			lvCSetStartPoint::GetClassMask(){
	if (Use_VVal)	return	0x00000002;
					return	0x00000001;
};
int SetStartPoint_lua(int x,int y){
	x=(x-(RealLx>>1))>>5;
	y=(y-RealLy)>>5;
	if(x<1)x=1;
	if(y<1)y=1;
	if(x+smaplx>msx-1)x=msx-smaplx-1;
	if(y+smaply>msy-1)y=msy-smaply-1;
	if (x!=0||y!=0) {
		void SetCameraPos(float x,float y);
		SetCameraPos((float)x,(float)y);
		return 1;
	};
	return 0;
};
// lvCTakeFood ///////////////////////////////////////////////////////////
lvCTakeFood::lvCTakeFood(lvCTakeFood* pTakeFood) : lvCOperation(dynamic_cast<lvCOperation*>(pTakeFood)) {
	if (pTakeFood!=NULL) {
		GrpID = pTakeFood->GrpID;
	};
};

void			lvCTakeFood::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCTakeFood(this));
};

const char*		lvCTakeFood::GetThisElementView(const char* LocalName){
	Descr="TakeFood(";
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP!=NULL) {
			Descr += pvGRP->GetGroupName();
			
		}else{
			Descr += "NULL";
		};
	}else{
		if (0<=GrpID&&GrpID<AGroups.GetAmount()){
			Descr += AGroups[GrpID]->Name.str;
		}else{
			Descr += "NULL";
		};
	};
	Descr += ")";
	return Descr.str;
};
int				lvCTakeFood::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP!=NULL) {
			pvGRP->TakeFood();
			return 1;
		};
	}else{
		if (0<=GrpID&&GrpID<AGroups.GetAmount()){
			int Nu=AGroups[GrpID]->Units.GetAmount();
			int NU=0;
			for(int i=0;i<Nu;i++){
				word MID=AGroups[GrpID]->Units[i].ID;
				lvBE_TakeResource(MID,FoodID,AGroups[GrpID]->Units[i].SN);
			};
			return 1;
		};
	};
	return 0;
};
// lvCTakeWood ///////////////////////////////////////////////////////////
lvCTakeWood::lvCTakeWood(lvCTakeWood* pTakeWood) : lvCOperation(dynamic_cast<lvCOperation*>(pTakeWood)) {
	if (pTakeWood!=NULL) {
		GrpID = pTakeWood->GrpID;
//		char* GrpName;
	};
};

void			lvCTakeWood::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCTakeWood(this));
};

const char*		lvCTakeWood::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=GrpID){
		Descr += "TakeWood(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[GrpID]!=NULL)) {
				Descr += AGroups[GrpID]->Name.str;
			};
		};
		Descr += ")";
	}else{
		Descr="lvCTakeWood(NULL)";
	};
	return Descr.str;
};
int				lvCTakeWood::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP!=NULL) {
			pvGRP->TakeWood();
			return 1;
		};
	}else if ((AGroups[GrpID]!=NULL)) {
		int Nu=AGroups[GrpID]->Units.GetAmount();
		int NU=0;
		for(int i=0;i<Nu;i++){
			word MID=AGroups[GrpID]->Units[i].ID;
			lvBE_TakeResource(MID,TreeID,AGroups[GrpID]->Units[i].SN);
		};
	};
	return 0;
};
// lvCTakeStone //////////////////////////////////////////////////////////
lvCTakeStone::lvCTakeStone(lvCTakeStone* pTakeStone) : lvCOperation(dynamic_cast<lvCOperation*>(pTakeStone)) {
	if (pTakeStone!=NULL) {
		GrpID = pTakeStone->GrpID;
	};
};

void			lvCTakeStone::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCTakeStone(this));
};

const char*		lvCTakeStone::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=GrpID){
		Descr += "TakeStone(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[GrpID]!=NULL)) {
				Descr += AGroups[GrpID]->Name.str;
			};
		};
		Descr += ")";
	}else{
		Descr="lvCTakeStone(NULL)";
	};
	return Descr.str;
};
int				lvCTakeStone::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP!=NULL) {
			pvGRP->TakeStone();
			return 1;
		};
	}else if ((AGroups[GrpID]!=NULL)) {
		int Nu=AGroups[GrpID]->Units.GetAmount();
		int NU=0;
		for(int i=0;i<Nu;i++){
			word MID=AGroups[GrpID]->Units[i].ID;
			lvBE_TakeResource(MID,StoneID,AGroups[GrpID]->Units[i].SN);
		};
	};
	return 0;
};
// lvCSetValue ///////////////////////////////////////////////////////////
lvCSetValue::lvCSetValue(lvCSetValue* pSetValue) : lvCOperation(dynamic_cast<lvCOperation*>(pSetValue)) {
	if (pSetValue!=NULL) {
		TypeID	= pSetValue->TypeID;	
		UseVV	= pSetValue->UseVV;
		if (pSetValue->Value.Get()!=NULL)	Value.Set(pSetValue->Value.Get());
		if (pSetValue->ValueTG.Get()!=NULL)	ValueTG.Set(pSetValue->ValueTG.Get());
		SetTG	= pSetValue->SetTG;
		if (pSetValue->vTG.Get()!=NULL)		vTG.Set(pSetValue->vTG.Get());
		if (pSetValue->ValueWD.Get()!=NULL)	ValueWD.Set(pSetValue->ValueWD.Get());
		SetWD	= pSetValue->SetWD;
		if (pSetValue->vWD.Get()!=NULL)		vWD.Set(pSetValue->vWD.Get());
		if (pSetValue->ValueIN.Get()!=NULL)	ValueIN.Set(pSetValue->ValueIN.Get());
		SetIN	= pSetValue->SetIN;
		if (pSetValue->vIN.Get()!=NULL)		vIN.Set(pSetValue->vIN.Get());
	};
};

void			lvCSetValue::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetValue(this));
};

const	char*	lvCSetValue::GetThisElementView(const char* LocalName){
	vvBASE*	pVValue = NULL;
	
	switch(TypeID) {
	case 0:			// Triger
		pVValue = ValueTG.Get();
		if (pVValue!=NULL) {
			Descr  = pVValue->Name.str;
			Descr += " = ";
			if (UseVV) {
				vvTRIGER*	pVV = vTG.Get();
				if (pVV!=NULL) {
					Descr +=pVV->Name.str;
				};
			}else{
				if (SetTG)	Descr += "true";
				else		Descr += "false";
			};
		};		
		break;
	case 1:			// Word
		pVValue = ValueWD.Get();
		if (pVValue!=NULL) {
			Descr  = pVValue->Name.str;
			Descr += " = ";
			if (UseVV) {
				vvWORD*	pVV = vWD.Get();
				if (pVV!=NULL) {
					Descr +=pVV->Name.str;
				};
			}else{
				Descr	+= SetWD;
			};
		};
		break;
	case 2:			// Integer
		pVValue = ValueIN.Get();
		if (pVValue!=NULL) {
			Descr  = pVValue->Name.str;
			Descr += " = ";
			if (UseVV) {
				vvINTEGER*	pVV = vIN.Get();
				if (pVV!=NULL) {
					Descr +=pVV->Name.str;
				};
			}else{
				Descr	+= SetIN;
			};
		};
		break;
	};
	
	if (pVValue==NULL) Descr = "NoValue";
	return	Descr.str;
};

int				lvCSetValue::Process(int time){
	lvCOperation::Process(time);
	switch(TypeID) {
	case 0:			// Triger
		if (UseVV) {
			vvTRIGER*	pVV = vTG.Get();
			if (pVV!=NULL) {
				SetTG=pVV->Value;
			};
		};
		if (ValueTG.Get()!=NULL) (ValueTG.Get())->Set(&SetTG);
		break;
	case 1:			// Word
		if (UseVV) {
			vvWORD*	pVV = vWD.Get();
			if (pVV!=NULL) {
				SetWD=pVV->Value;
			};
		};
		if (ValueWD.Get()!=NULL) (ValueWD.Get())->Set(&SetWD);
		break;
	case 2:			// Integer
		if (UseVV) {
			vvINTEGER*	pVV = vIN.Get();
			if (pVV!=NULL) {
				SetIN=pVV->Value;
			};
		};
		if (ValueIN.Get()!=NULL) (ValueIN.Get())->Set(&SetIN);
		break;
	};
	return	1;
};

bool			lvCSetValue::AskParentForUsingExpansionClass(char* MemberName,char* ClassName){
	if (strcmp(MemberName,"Value")!=0)	return true;
	
	if (TypeID==0 && strcmp(ClassName,"vvTRIGER")	==0)	return true;		// Triger
	if (TypeID==1 && strcmp(ClassName,"vvWORD")		==0)	return true;		// Word
	if (TypeID==2 && strcmp(ClassName,"vvINTEGER")	==0)	return true;		// Integer
	
	return false;
};

DWORD			lvCSetValue::GetClassMask(){
	switch(TypeID) {
	case 0:			// Triger
		if (UseVV) {
			return 0x00000010;
		}else{
			return 0x00000001;
		};
		break;
	case 1:			// Word
		if (UseVV) {
			return 0x00000020;
		}else{
			return 0x00000002;
		};
		break;
	case 2:			// Integer
		if (UseVV) {
			return 0x00000040;
		}else{
			return 0x00000004;
		};
		break;
	};
	return	0x80000000;
};
// lvCAddToInt ///////////////////////////////////////////////////////////
lvCAddToInt::lvCAddToInt(lvCAddToInt* pAddToInt) : lvCOperation(dynamic_cast<lvCOperation*>(pAddToInt)) {
	if (pAddToInt!=NULL) {
		if (pAddToInt->IntValue.Get()!=NULL) IntValue.Set(pAddToInt->IntValue.Get());
		IntAdd = pAddToInt->IntAdd;
	};
};

void			lvCAddToInt::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddToInt(this));
};

const	char*	lvCAddToInt::GetThisElementView(const char* LocalName){
	Descr = "AddValue";
	if (IntValue.Get()!=NULL) {
		Descr = "";
		Descr += IntValue.Get()->GetName();
		if (IntAdd>=0){
			Descr += " + ";
		}else{
			Descr += " - ";
		};
		Descr += abs(IntAdd);
	};
	return Descr.str;
};

int				lvCAddToInt::Process(int time){
	lvCOperation::Process(time);
	if (first&&IntValue.Get()!=NULL){
		IntValue.Get()->Value += IntAdd;
		first=false;
		return 1;
	};
    return 0;
};

int				lvCAddToInt::Complite(){
	return (first==false);
};
// lvCAddToIntEx ///////////////////////////////////////////////////////////
lvCAddToIntEx::lvCAddToIntEx(lvCAddToIntEx* pAddToIntEx) : lvCOperation(dynamic_cast<lvCOperation*>(pAddToIntEx)) {
	if (pAddToIntEx!=NULL) {
		if (pAddToIntEx->IntValue.Get()!=NULL)	IntValue.Set(pAddToIntEx->IntValue.Get());
		IntAdd	= pAddToIntEx->IntAdd;
	};
};

void			lvCAddToIntEx::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddToIntEx(this));
};

const	char*	lvCAddToIntEx::GetThisElementView(const char* LocalName){
	Descr = "AddValueEx";
	if (IntValue.Get()!=NULL) {
		Descr = "";
		Descr += IntValue.Get()->GetName();
		if (IntAdd>=0){
			Descr += " ++ ";
		}else{
			Descr += " -- ";
		};
			Descr += abs(IntAdd);
	};
	return Descr.str;
};

int				lvCAddToIntEx::Process(int time){
	lvCOperation::Process(time);
	if (IntValue.Get()!=NULL){
		IntValue.Get()->Value += IntAdd;
		return 1;
	};
	return 0;
};

// lvCKillNUnits /////////////////////////////////////////////////////////
lvCKillNUnits::lvCKillNUnits(lvCKillNUnits* pKillNUnits) : lvCOperation(dynamic_cast<lvCOperation*>(pKillNUnits)) {
	if (pKillNUnits!=NULL) {
		GrpID	= pKillNUnits->GrpID;
		UCount	= pKillNUnits->UCount;
	};
};

void			lvCKillNUnits::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCKillNUnits(this));
};

const char*		lvCKillNUnits::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=GrpID){
		Descr += "Kill N Units(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		};
		Descr += ", ";
		Descr += UCount;
		Descr += ")";
	}else{
		Descr="lvCKillNUnits(NULL)";
	};
	return Descr.str;
};
int				lvCKillNUnits::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			pvGRP->KillUnits(UCount);
		};
	};
	return UCount;
};
// lvCEraseNUnits ////////////////////////////////////////////////////////
lvCEraseNUnits::lvCEraseNUnits(lvCEraseNUnits* pEraseNUnits) : lvCOperation(dynamic_cast<lvCOperation*>(pEraseNUnits)) {
	if (pEraseNUnits!=NULL) {
		GrpID	= pEraseNUnits->GrpID;
		UCount	= pEraseNUnits->UCount;
	};
};

void			lvCEraseNUnits::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCEraseNUnits(this));
};

const char*		lvCEraseNUnits::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=GrpID){
		Descr += "Erase N Units(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		};
		Descr += ", ";
		Descr += UCount;
		Descr += ")";
	}else{
		Descr="lvCEraseNUnits(NULL)";
	};
	return Descr.str;
};
int				lvCEraseNUnits::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			pvGRP->EraseUnits(UCount);
		};
	};
	return UCount;
};
// lvCSavePosition ///////////////////////////////////////////////////////
lvCSavePosition::lvCSavePosition(lvCSavePosition* pSavePosition) : lvCOperation(dynamic_cast<lvCOperation*>(pSavePosition)) {
	if (pSavePosition!=NULL) {
		GrpID		= pSavePosition->GrpID;
		UseVV		= pSavePosition->UseVV;
		if (pSavePosition->VVpPos.Get()!=NULL)	VVpPos.Set(pSavePosition->VVpPos.Get());
	};
};

void			lvCSavePosition::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSavePosition(this));
};

const	char*	lvCSavePosition::GetThisElementView(const char* LocalName){
	vvPOINT_SET* vPS=VVpPos.Get();
	vvPOINT2D*	 vvP=vvPoint.Get();
    Descr="";
	Descr+=	"Save Position(";
		if (UseVV){
			if (vPS!=NULL) {
				Descr += vPS->GetName();
			}else{
				Descr += "NoValue";
			};
		}else{
			if (vvP!=NULL) {
				Descr += vvP->GetName();
			}else{
				Descr += "NoValue";
			};
		};
	Descr+= ")";
    return Descr.str;
};
int				lvCSavePosition::Process(int time){
	lvCOperation::Process(time);
	vvPOINT_SET* vPS=VVpPos.Get();
	vvPOINT2D*	 vvP=vvPoint.Get();
	if (vPS==NULL&&vvP==NULL) {
		return 0;
	};
	lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
	if (pvGRP==NULL)	return 0;
	if (UseVV=true&&vPS!=NULL){
		int c=pvGRP->GetTotalAmount();
		vPS->CleanARR();
		while (c--) {
			OneObject* OB=pvGRP->GetOneObj_lID(c);
			if(OB){
				vPS->AddPoint(OB->x,OB->y);
			};
		}
	}else if (vvP!=NULL) {
		int xcc,ycc;
		pvGRP->GetGroupCenter(xcc,ycc);
		if (xcc!=0||ycc!=0) {
			vvP->SetTR(xcc,ycc);
		};
	};
	return 1;
};
DWORD			lvCSavePosition::GetClassMask(){
	if (UseVV==true)	return 0x00000001;
						return 0x00000002;
};
int SavePosition_lua(lvCGroup* pGrp,vvPOINT2D* pPos){
	if (pGrp==NULL||pPos==NULL) return 0;
	int xcc,ycc;
	pGrp->GetGroupCenter(xcc,ycc);
	pPos->SetTR(xcc,ycc);
	return 1;
}
int SavePositionArr_lua(lvCGroup* pGrp,vvPOINT_SET* pPosArr){
	if (pGrp==NULL||pPosArr==NULL) return 0;
	int c=pGrp->GetTotalAmount();
	pPosArr->CleanARR();
	while (c--) {
		OneObject* OB=pGrp->GetOneObj_lID(c);
		if(OB){
			pPosArr->AddPoint(OB->x,OB->y);
		};
	};
	return 1;
};
// lvCSendToPosition /////////////////////////////////////////////////////
lvCSendToPosition::lvCSendToPosition(lvCSendToPosition* pSendToPosition) : lvCOperation(dynamic_cast<lvCOperation*>(pSendToPosition)) {
	if (pSendToPosition!=NULL) {
		parGrp	= pSendToPosition->parGrp;
		if (pSendToPosition->VVpPos.Get()!=NULL) VVpPos.Set(pSendToPosition->VVpPos.Get());
	};
};

void			lvCSendToPosition::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSendToPosition(this));
};

const	char*	lvCSendToPosition::GetThisElementView(const char* LocalName){
	vvPOINT_SET* vPS=VVpPos.Get();
	Descr="";
	Descr+=	"Send to Position(";
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			Descr += pvGRP->GetGroupName();
		};
	};
	Descr+= ", ";
	if (vPS!=NULL) {
		Descr+= vPS->GetName();
	};
	Descr+= ")";
	return Descr.str;
};
int				lvCSendToPosition::Process(int time){
	lvCOperation::Process(time);
	vvPOINT_SET* vPS=VVpPos.Get();
	if (vPS==NULL) {
		return 0;
	};
	if (use_vGroup){
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			if (!(pvGRP->ChekPosition(vPS))&&(pvGRP->GetORDER()==0)){
				pvGRP->SendToPosition(vPS);
			};
		};
	};
	return 1;
};
// lvCSetRessource ///////////////////////////////////////////////////////
lvCSetRessource::lvCSetRessource(lvCSetRessource* pSetRessource) : lvCOperation(dynamic_cast<lvCOperation*>(pSetRessource)) {
	if (pSetRessource!=NULL) {
		Nation	= pSetRessource->Nation;
		UseVV	= pSetRessource->UseVV;
		Food	= pSetRessource->Food;			
		Wood	= pSetRessource->Wood;			
		Stown	= pSetRessource->Stown;			
		Gold	= pSetRessource->Gold;			
		Iron	= pSetRessource->Iron;			
		Coal	= pSetRessource->Coal;
		if (pSetRessource->FoodVV.Get()!=NULL)	FoodVV.Set(pSetRessource->FoodVV.Get());
		if (pSetRessource->WoodVV.Get()!=NULL)	WoodVV.Set(pSetRessource->WoodVV.Get());
		if (pSetRessource->StownVV.Get()!=NULL)	StownVV.Set(pSetRessource->StownVV.Get());
		if (pSetRessource->GoldVV.Get()!=NULL)	GoldVV.Set(pSetRessource->GoldVV.Get());
		if (pSetRessource->IronVV.Get()!=NULL)	IronVV.Set(pSetRessource->IronVV.Get());
		if (pSetRessource->CoalVV.Get()!=NULL)	CoalVV.Set(pSetRessource->CoalVV.Get());
	};
};

void			lvCSetRessource::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetRessource(this));
};

const	char*	lvCSetRessource::GetThisElementView(const char* LocalName){
	Descr="";
	Descr+=	"SetRess ";
	Descr+=	"F[";
	Descr+= Food;
	Descr+= "] W[";
	Descr+= Wood;
	Descr+= "] S[";
	Descr+= Stown;
	Descr+= "] G[";
	Descr+= Gold;
	Descr+= "] I[";
	Descr+= Iron;
	Descr+= "] C[";
	Descr+= Coal;
	Descr+= "]";
	return Descr.str;
};

int				lvCSetRessource::Process(int time){
	lvCOperation::Process(time);
	if (UseVV==true){	
		if (WoodVV.Get()!=NULL) {
			SetResource(Nation,0,WoodVV.Get()->Value);		// WOOD
		};
		if (GoldVV.Get()!=NULL) {
			SetResource(Nation,1,GoldVV.Get()->Value);		// GOLD
		};
		if (StownVV.Get()!=NULL) {
			SetResource(Nation,2,StownVV.Get()->Value);		// STOWN
		};
		if (FoodVV.Get()!=NULL) {
			SetResource(Nation,3,FoodVV.Get()->Value);		// FOOD
		};
		if (IronVV.Get()!=NULL) {
			SetResource(Nation,4,IronVV.Get()->Value);		// IRON 
		};
		if (CoalVV.Get()!=NULL) {
			SetResource(Nation,5,CoalVV.Get()->Value);		// COAL
		};
	}else{
		SetResource(Nation,0,Wood);		// WOOD
		SetResource(Nation,1,Gold);		// GOLD
		SetResource(Nation,2,Stown);	// STOWN
		SetResource(Nation,3,Food);		// FOOD
		SetResource(Nation,4,Iron);		// IRON 
		SetResource(Nation,5,Coal);		// COAL
	};
	return 1;
};

DWORD			lvCSetRessource::GetClassMask(){
	if (UseVV==true)	return 0x00000001;
						return 0x00000002;
};
int SetResource_lua(int nat,int resID,int Amount){
	if ( 0<=nat&&nat<8 && 0<=resID&&resID<6 ){
		SetResource(nat,resID,Amount);
		return 1;
	};
	return 0;
};
// lvCAddRessource ///////////////////////////////////////////////////////
lvCAddRessource::lvCAddRessource(lvCAddRessource* pAddRessource) : lvCOperation(dynamic_cast<lvCOperation*>(pAddRessource)) {
	if (pAddRessource!=NULL) {
		RessType	= pAddRessource->RessType;
		Nat			= pAddRessource->Nat;
		Use_VV		= pAddRessource->Use_VV;
		Value		= pAddRessource->Value;
		if (pAddRessource->vValue.Get()!=NULL) vValue.Set(pAddRessource->vValue.Get());
	};
};

void			lvCAddRessource::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddRessource(this));
};

const	char*	lvCAddRessource::GetThisElementView(const char* LocalName){
	Descr = "AddRessource(";
	Descr += "Nat[";
	Descr += Nat;
	Descr += "]";
	Descr += ",";
	switch(RessType) {
	case 0:
		Descr += "Wood";
		break;
	case 1:
		Descr += "Gold";
		break;
	case 2:
		Descr += "Stone";
		break;
	case 3:
		Descr += "Food";
		break;
	case 4:
		Descr += "Iron";
		break;
	case 5:
		Descr += "Coal";
		break;
	};
	Descr += "[";
	if (Use_VV) {
		vvINTEGER* pvValue = vValue.Get();
		if (pvValue!=NULL) {
			Descr += pvValue->Value;
		}else{
			Descr += "NoVal";
		};
	}else{
		Descr += Value;
	};
	Descr += "]";
    Descr += ")";
	return Descr.str;
};

int				lvCAddRessource::Process(int time){
	lvCOperation::Process(time);
	if (Nat<0||Nat>7) return 0;
	if (Use_VV) {
		vvINTEGER* pvValue = vValue.Get();
		if (pvValue!=NULL) {
			AddResource(Nat,RessType,pvValue->Value);
			return 1;
		};
	}else{
		AddResource(Nat,RessType,Value);
		return 1;
	};
	return 0;
};
int AddRessource_lua(int nat,int resID,int Amount){
	if ( 0<=nat&&nat<8 && 0<=resID&&resID<6 ){
		AddResource(nat,resID,Amount);
		return 1;
	};
	return 0;
};
// lvCStartAIEx //////////////////////////////////////////////////////////
lvCStartAIEx::lvCStartAIEx(lvCStartAIEx* pStartAIEx) : lvCOperation(dynamic_cast<lvCOperation*>(pStartAIEx)) {
	if (pStartAIEx!=NULL) {
		Nation	= pStartAIEx->Nation;
		NameXML = pStartAIEx->NameXML.str;
		Land	= pStartAIEx->Land;
		Money	= pStartAIEx->Money;
		ResOnMap= pStartAIEx->ResOnMap;
		Use_VV	= pStartAIEx->Use_VV;
		if (pStartAIEx->vDifficulty.Get()!=NULL) vDifficulty.Set(pStartAIEx->vDifficulty.Get());
		Difficulty = pStartAIEx->Difficulty;
	};
};

void			lvCStartAIEx::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCStartAIEx(this));
};

const	char*	lvCStartAIEx::GetThisElementView(const char* LocalName){
	Descr  = "StartAI(";
	
	Descr += Nation;
	Descr += ",";
	Descr += NameXML.str;
	Descr += ",";
	Descr += Land;
	Descr += ",";
	Descr += Money;
	Descr += ",";
	Descr += ResOnMap;
	Descr += ",";
	Descr += Difficulty;
	Descr += ")";
	
	return Descr.str;
};

int				lvCStartAIEx::Process(int time){
	lvCOperation::Process(time);
	if (EngSettings.GameName==1){
		ActivateTacticalAI(Nation);
	};
	if (Use_VV && vDifficulty.Get()!=NULL){
		StartAIEx(Nation,NameXML.str,Land,Money,ResOnMap,vDifficulty.Get()->Value);
		return 1;
	};
	if (!Use_VV){
		StartAIEx(Nation,NameXML.str,Land,Money,ResOnMap,Difficulty);
		return 1;
	};
	return 0;
};

DWORD			lvCStartAIEx::GetClassMask(){
	if (Use_VV)	return 0x00000001;
				return 0x00000002;
};
int ActivateTacticalAI_lua(int nat){
	if (EngSettings.GameName==1) ActivateTacticalAI(nat);
	return 1;
};
int StartAIEx_lua(int nat,const char* file,int lend,int mony,int res,int diff){
	_str FName; FName=file;
	StartAIEx(nat,FName.str,lend,mony,res,diff);
	return 1;
};
// lvCSetAIEnableState ///////////////////////////////////////////////////
lvCSetAIEnableState::lvCSetAIEnableState(lvCSetAIEnableState* pSetAIEnableState) : lvCOperation(dynamic_cast<lvCOperation*>(pSetAIEnableState)) {
	if (pSetAIEnableState!=NULL) {
		Nat		= pSetAIEnableState->Nat;
		State	= pSetAIEnableState->State;
	};
};
const	char*	lvCSetAIEnableState::GetThisElementView(const char* LocalName){
	Descr = "SetAIEnableState(Nat[";
	if (0<=Nat&&Nat<8)	Descr += Nat;
	else				Descr += "NoNation";
	Descr += "],";
	Descr += State;
	Descr += ")";
	return Descr.str;
};
int				lvCSetAIEnableState::Process(int time){
	lvCOperation::Process(time);
	return SetAIEnableState_lua(Nat,State);
};
void			lvCSetAIEnableState::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetAIEnableState(this));
};
int SetAIEnableState_lua(int nat,bool state){
	if(0<=nat&&nat<8){
		NATIONS[nat].AI_Enabled=state;
		return 1;
	};
	return 0;
}
// lvCSetCamera //////////////////////////////////////////////////////////
lvCSetCamera::lvCSetCamera(lvCSetCamera* pSetCamera) : lvCOperation(dynamic_cast<lvCOperation*>(pSetCamera)) {
	if (pSetCamera!=NULL) {
		if (pSetCamera->POS.Get()!=NULL)	POS.Set(pSetCamera->POS.Get());
		if (pSetCamera->DIR.Get()!=NULL)	DIR.Set(pSetCamera->DIR.Get());
	};
};

void			lvCSetCamera::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetCamera(this));
};

const	char*	lvCSetCamera::GetThisElementView(const char* LocalName){
	Descr = "SetCamera(Pos[";
	if (POS.Get()!=NULL)	Descr += POS.Get()->GetName();
	Descr += "],Dir[";
	if (DIR.Get()!=NULL)	Descr += DIR.Get()->GetName();
	Descr += "])";
	return Descr.str;
};

int				lvCSetCamera::Process(int time){
	lvCOperation::Process(time);
	if (POS.Get()!=NULL||DIR.Get()!=NULL){
		vvVector3D* pPos = POS.Get();
		vvVector3D* pDir = DIR.Get();
		if (pPos!=NULL){
			Vector3D vPos(pPos->x,pPos->y,pPos->z);
			ICam->SetPos(vPos);
		};
		if (pDir!=NULL){
			Vector3D vDir(pDir->x,pDir->y,pDir->z);
			ICam->SetDir(vDir);
		};
		BE_NewSetCamera();
		return 1;
	};
	return 0;
};
// lvCMoveCamera /////////////////////////////////////////////////////////
lvCMoveCamera::lvCMoveCamera(lvCMoveCamera* pMoveCamera) : lvCOperation(dynamic_cast<lvCOperation*>(pMoveCamera)) {
	if (pMoveCamera!=NULL) {
		if (pMoveCamera->POS0.Get()!=NULL)	POS0.Set(pMoveCamera->POS0.Get());
		if (pMoveCamera->POS1.Get()!=NULL)	POS1.Set(pMoveCamera->POS1.Get());
		if (pMoveCamera->DIR0.Get()!=NULL)	DIR0.Set(pMoveCamera->DIR0.Get());
		if (pMoveCamera->DIR1.Get()!=NULL)	DIR1.Set(pMoveCamera->DIR1.Get());
		useMapXY	= pMoveCamera->useMapXY;
		Time		= pMoveCamera->Time;
	};
};

void			lvCMoveCamera::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCMoveCamera(this));
};

int				lvCMoveCamera::MoveType(){
	bool	bPOS0 = (POS0.Get()!=NULL);
	bool	bPOS1 = (POS1.Get()!=NULL);
	bool	bDIR0 = (DIR0.Get()!=NULL);
	bool	bDIR1 = (DIR1.Get()!=NULL);
	if (bPOS0 && !bPOS1 && !bDIR0 && !bDIR1)	return	1;
	if (bPOS0 && !bPOS1 &&  bDIR0 && !bDIR1)	return	2;
	if (bPOS0 &&  bPOS1 && !bDIR0 && !bDIR1)	return	3;
	if (bPOS0 &&  bPOS1 &&  bDIR0 &&  bDIR1)	return	4;
};

const	char*	lvCMoveCamera::GetThisElementView(const char* LocalName){
	Descr = "MoveCamera(";
	int	ttt = MoveType();
	switch(ttt) {
	case 1:
		Descr += "Pos[";
		Descr += POS0.Get()->Name.str;
		Descr += "]";
		break;
	case 2:
		Descr += "Pos[";
		Descr += POS0.Get()->Name.str;
		Descr += "]";
		Descr += "Dir[";
		Descr += DIR0.Get()->Name.str;
		Descr += "]";
		break;
	case 3:
		Descr += "Pos[";
		Descr += POS0.Get()->Name.str;
		Descr += "]->";
		Descr += "Pos[";
		Descr += POS1.Get()->Name.str;
		Descr += "]";
		break;
	case 4:
		Descr += "Pos[";
		Descr += POS0.Get()->Name.str;
		Descr += "]";
		Descr += "Dir[";
		Descr += DIR0.Get()->Name.str;
		Descr += "]->";
		Descr += "Pos[";
		Descr += POS1.Get()->Name.str;
		Descr += "]";
		Descr += "Dir[";
		Descr += DIR1.Get()->Name.str;
		Descr += "]->";
		break;
	};
	Descr += ")";
	return	Descr.str;
};

int				lvCMoveCamera::Process(int time){
	lvCOperation::Process(time);
	if (Time==0){
//		CameraDriver()->MoveType=lvCRotateCamera::enMoveType::beTO_FREE;
		CameraDriver()->Stop();
		return 1;
	};

	Vector3D	Pos0(0.f,0.f,0.f);
	Vector3D	Pos1(0.f,0.f,0.f);
	Vector3D	Dir0(0.f,0.f,0.f);
	Vector3D	Dir1(0.f,0.f,0.f);
	int	ttt = MoveType();
	switch(ttt) {
	case 1:
		{
			Pos0.set(POS0.Get()->x,POS0.Get()->y,POS0.Get()->z);
			CameraDriver()->Set(&Pos0,Time);
		}
		break;
	case 2:
		{
			Pos0.set(POS0.Get()->x,POS0.Get()->y,POS0.Get()->z);
			Dir0.set(DIR0.Get()->x,DIR0.Get()->y,DIR0.Get()->z);
			CameraDriver()->Set(&Pos0,&Dir0,Time);
		}
		break;
	case 3:
		{
				if (!useMapXY){
				Pos0.set(POS0.Get()->x,POS0.Get()->y,POS0.Get()->z);
				Pos1.set(POS1.Get()->x,POS1.Get()->y,POS1.Get()->z);
				CameraDriver()->Set(&Pos0,&Pos1,Time,0);
			}else{
                Pos0.set(POS0.Get()->mX,POS0.Get()->mY,0.f);
				Pos1.set(POS1.Get()->mX,POS1.Get()->mY,0.f);
				CameraDriver()->Set(&Pos0,&Pos1,Time,0);
			};
		}
		break;
	case 4:
		{
			Pos0.set(POS0.Get()->x,POS0.Get()->y,POS0.Get()->z);
			Dir0.set(DIR0.Get()->x,DIR0.Get()->y,DIR0.Get()->z);
			Pos1.set(POS1.Get()->x,POS1.Get()->y,POS1.Get()->z);
			Dir1.set(DIR1.Get()->x,DIR1.Get()->y,DIR1.Get()->z);
			CameraDriver()->Set(&Pos0,&Dir0,&Pos1,&Dir1,Time);
		}
		break;
	};
	CameraDriver()->Set(useMapXY);
	return 1;
};
// lvCAttachCameraToGroup ////////////////////////////////////////////////
lvCAttachCameraToGroup::lvCAttachCameraToGroup(lvCAttachCameraToGroup* pAttachCameraToGroup) : lvCOperation(dynamic_cast<lvCOperation*>(pAttachCameraToGroup)) {
	if (pAttachCameraToGroup!=NULL) {
		vGrpID = pAttachCameraToGroup->vGrpID;
	};
};

void			lvCAttachCameraToGroup::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAttachCameraToGroup(this));
};

const	char*	lvCAttachCameraToGroup::GetThisElementView(const char* LocalName){
	Descr = "AttachCameraToGroup(";
	lvCGroup*	pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	return Descr.str;
};

int				lvCAttachCameraToGroup::Process(int time){
	lvCOperation::Process(time);
	lvCGroup*	pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		CameraDriver()->Set(vGrpID);
		return 1;
	};
	return 0;
};

// lvCFreeCamera /////////////////////////////////////////////////////////
lvCFreeCamera::lvCFreeCamera(lvCFreeCamera* pFreeCamera) : lvCOperation(dynamic_cast<lvCOperation*>(pFreeCamera)) {
	if (pFreeCamera!=NULL) {

	};
};

void			lvCFreeCamera::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCFreeCamera(this));
};

const	char*	lvCFreeCamera::GetThisElementView(const char* LocalName){
	Descr = "FreeCamera()";
	return Descr.str;
};

int				lvCFreeCamera::Process(int time){
	lvCOperation::Process(time);
	CameraDriver()->Stop();
	return 1;
};

// lvCShowDialog /////////////////////////////////////////////////////////
lvCShowDialog::lvCShowDialog(lvCShowDialog* pShowDialog) : lvCOperation(dynamic_cast<lvCOperation*>(pShowDialog)) {
	if (pShowDialog!=NULL) {
		if (pShowDialog->Dialog.Get()!=NULL) Dialog.Set(pShowDialog->Dialog.Get());
	};
};

void			lvCShowDialog::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCShowDialog(this));
};

const	char*	lvCShowDialog::GetThisElementView(const char* LocalName){
	Descr = "ShowDialog(";
	if (Dialog.Get()!=NULL) {
		Descr += Dialog.Get()->GetName();
	};
	Descr += ")";
	return Descr.str;
};

void ProcessScreen();

int				lvCShowDialog::Process(int time){
	lvCOperation::Process(time);
	return ShowDialog_lua(Dialog.Get());
};
int ShowDialog_lua(vvDIALOG* pDLG){
	if (pDLG!=NULL){
		DialogsSystem* pDialog  = reinterpret_cast<DialogsSystem*>(pDLG->Get());
		if (pDialog==NULL)	return 0;
		int OldItemChoose = ItemChoose;
		while (ItemChoose!=0) {
			ProcessScreen();
			ProcessMessages();
			pDialog->ProcessDialogs();
			pDialog->RefreshView();
		};
		ItemChoose = OldItemChoose;
		return 1;
	};
	return 0;
}
// lvCAddTextToDlg ///////////////////////////////////////////////////////
lvCAddTextToDlg::lvCAddTextToDlg(lvCAddTextToDlg* pAddTextToDlg) : lvCOperation(dynamic_cast<lvCOperation*>(pAddTextToDlg)) {
	if (pAddTextToDlg!=NULL) {
		if (pAddTextToDlg->Dialog.Get()!=NULL)	Dialog.Set(pAddTextToDlg->Dialog.Get());
		if (pAddTextToDlg->Text.Get()!=NULL)	Text.Set(pAddTextToDlg->Text.Get());
	};
};

void			lvCAddTextToDlg::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddTextToDlg(this));
};

const	char*	lvCAddTextToDlg::GetThisElementView(const char* LocalName){
	Descr = "AddText(";
	if (Text.Get()!=NULL){
		Descr += Text.Get()->GetName();
	};
	Descr += "->";
	if (Dialog.Get()!=NULL){
		Descr += Dialog.Get()->GetName();
	};
	Descr += ")";
	return Descr.str;
};

int				lvCAddTextToDlg::Process(int time){
	lvCOperation::Process(time);
	return AddTextToDlg_lua(Dialog.Get(),Text.Get());
};
int AddTextToDlg_lua(vvDIALOG* pDLG,vvTEXT* pTXT){
	if (pDLG!=NULL&&pTXT!=NULL){
		DialogsSystem* pDialog  = reinterpret_cast<DialogsSystem*>(pDLG->Get());
		ListDesk* pLD = dynamic_cast<ListDesk*>(pDialog->Find("MainDesck"));
		if (pLD!=NULL){
			vvTEXT* pText = pTXT;
			pLD->AddElement(GetTextByID(pText->TextID.str));
			return 1;
		};
	};
	return 0;
};
// lvCClearDialog ////////////////////////////////////////////////////////
lvCClearDialog::lvCClearDialog(lvCClearDialog* pClearDialog) : lvCOperation(dynamic_cast<lvCOperation*>(pClearDialog)) {
	if (pClearDialog!=NULL) {
		if (pClearDialog->Dialog.Get()!=NULL) Dialog.Set(pClearDialog->Dialog.Get()); 
	};
};

void			lvCClearDialog::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCClearDialog(this));
};

const	char*	lvCClearDialog::GetThisElementView(const char* LocalName){
	Descr = "Clear(";
	if (Dialog.Get()!=NULL){
		Descr += Dialog.Get()->GetName();
	};
	Descr += ")";
	return	Descr.str;
};

int				lvCClearDialog::Process(int time){
	lvCOperation::Process(time);
	return ClearDialog_lua(Dialog.Get());
};
int ClearDialog_lua(vvDIALOG* pDLG){
	if (pDLG==NULL) return 0;
	DialogsSystem* pDialog  = reinterpret_cast<DialogsSystem*>(pDLG->Get());
	if (pDialog!=NULL){
		ListDesk* pLD = dynamic_cast<ListDesk*>(pDialog->Find("MainDesck"));
		if (pLD!=NULL){
			// Delete all objects
			pLD->DSS.Clear();
			return 1;
		};
	};
	return 0;
};
// lvCSetScrollLimit /////////////////////////////////////////////////////
lvCSetScrollLimit::lvCSetScrollLimit(lvCSetScrollLimit* pSetScrollLimit) : lvCOperation(dynamic_cast<lvCOperation*>(pSetScrollLimit)) {
	if (pSetScrollLimit!=NULL) {
		if (pSetScrollLimit->PosLT.Get()!=NULL) PosLT.Set(pSetScrollLimit->PosLT.Get());
		if (pSetScrollLimit->PosRB.Get()!=NULL) PosRB.Set(pSetScrollLimit->PosRB.Get());
	};
};

void			lvCSetScrollLimit::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetScrollLimit(this));
};

const	char*	lvCSetScrollLimit::GetThisElementView(const char* LocalName){
	Descr = "ScrollLimit[";
	if (PosLT.Get()!=NULL)	Descr += PosLT.Get()->GetName();
	Descr += ",";
	if (PosRB.Get()!=NULL)	Descr += PosRB.Get()->GetName();
	Descr += "]";
	return Descr.str;
};

int				lvCSetScrollLimit::Process(int time){
	lvCOperation::Process(time);
	return	SetScrollLimit_lua(PosLT.Get(),PosRB.Get(),LockAroud);
};
int SetScrollLimit_lua(vvPOINT2D* pPosLT,vvPOINT2D* pPosRB,bool LockAroud){
	if (pPosLT!=NULL&&pPosRB!=NULL){
		void SetScrollLimitations(int x0,int y0,int x1,int y1);
		int x0 = pPosLT->Value.x;
		int y0 = pPosLT->Value.y;
		int x1 = pPosRB->Value.x;
		int y1 = pPosRB->Value.y;
		SetScrollLimitations(x0,y0,x1,y1);
		// set lock areas around scrool limitation
		if (LockAroud==true){
			int xl0 = x0/16;
			int yl0 = y0/16;
			int xl1 = x1/16;
			int yl1 = y1/16;
			BSetSQ(xl0-1,yl0-1,xl1-xl0+5,2);
			BSetSQ(xl0-1,yl1+1,xl1-xl0+5,2);
			BSetSQ(xl0-1,yl0-1,2,yl1-yl0+5);
			BSetSQ(xl1+1,yl0-1,2,yl1-yl0+5);
		};
		return 1;
	};
	return	0;
}
// lvCSpotNUnits /////////////////////////////////////////////////////////
lvTypeNum::lvTypeNum(lvTypeNum* pTypeNum){
	if (pTypeNum!=NULL) {
		InfID		= pTypeNum->InfID;
		vgUnitsID	= pTypeNum->vgUnitsID;
		UnitType	= pTypeNum->UnitType;
		Use_VV		= pTypeNum->Use_VV;
		Num			= pTypeNum->Num;
		if (pTypeNum->vNum.Get()!=NULL) vNum.Set(pTypeNum->vNum.Get());
	};
};

void			lvTypeNum::GetCopy(lvTypeNum** pCopy){
	*pCopy = new lvTypeNum(this);
};

bool			lvTypeNum::Prepare(){
	if (Use_VV){
		if (vNum.Get()!=NULL){
			Num = vNum.Get()->Value;
		}else{
			return false;
		};
	};
	return true;
};

lvCSpotNUnits::lvCSpotNUnits(lvCSpotNUnits* pSpotNUnits) : lvCOperation(dynamic_cast<lvCOperation*>(pSpotNUnits)) {
	if (pSpotNUnits!=NULL) {
		vgBildsID		= pSpotNUnits->vgBildsID;	
		aZDestPoint		= pSpotNUnits->aZDestPoint;
		Use_VV			= pSpotNUnits->Use_VV;
		ProduceTime		= pSpotNUnits->ProduceTime;
		if (pSpotNUnits->vProduceTime.Get()!=NULL) vProduceTime.Set(pSpotNUnits->vProduceTime.Get());

		lvTypeNum* pTypeNum = NULL;
		for (int i=0; i<pSpotNUnits->TypeNum.GetAmount(); i++){
			pSpotNUnits->TypeNum[i]->GetCopy(&pTypeNum);
			if (pTypeNum!=NULL) {
				TypeNum.Add(pTypeNum);
			};
			pTypeNum=NULL;
		};
		
		UGRP_ID			= pSpotNUnits->UGRP_ID;
	};
};

void			lvCSpotNUnits::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSpotNUnits(this));
};

const	char*	lvCSpotNUnits::GetThisElementView(const char* LocalName){
	Descr = "SpotUnitsInGroup(";

	// Bld grop
	lvCGroup* pvBLD = GroupsMap()->GetGroupID(vgBildsID);
	Descr += " BLD[";
	if (pvBLD!=NULL) {
		Descr += pvBLD->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += "],";

	// Dest zone
	if (UseNode) {
		Descr += " NODE[";
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL) {
			Descr += pNode->vGetName();
		}else{
			Descr += "NoNode";
		};
	}else{
		Descr += " ZONE[";
		if (0<=aZDestPoint && aZDestPoint<AZones.GetAmount()){
			ActiveZone* pZ = AZones[aZDestPoint];
			if (pZ!=NULL) {
				Descr += pZ->Name.str;
			}else{
				Descr += "NoZone";
			}
		}else{
			Descr += "NoZone";
		};
	};
	Descr += "],";

	// Dest grp
	int Nat= ( (pvBLD!=NULL) ? (pvBLD->GetNation()) : (0) );
	Descr += " Grop[";
	int N = TypeNum.GetAmount();
	if (N!=0) {
		while (N--) {
			lvCGroup* pvUGrp = GroupsMap()->GetGroupID(TypeNum[N]->vgUnitsID);
			if (pvUGrp!=NULL) {
				Descr += pvUGrp->NAME.str;
				Descr += "(";
				if (0<=Nat&&Nat<8)	{Descr += NATIONS[Nat].Mon[TypeNum[N]->UnitType]->Message;}
				else				{Descr += "NoType";}
				Descr += ")";
				if (N!=0) Descr += ", ";
			};
		};
	}else{
		Descr += "NoGrp(NoType)";		
	};
	Descr += "]";

	Descr += ")";
	return Descr.str;
};

int				lvCSpotNUnits::Process(int time){
	lvCOperation::Process(time);
	Create();
	if (Use_VV){
		if (vProduceTime.Get()!=NULL){
			ProduceTime = vProduceTime.Get()->Value;
		}else{
			return 0;
		};		
	};
	for (int i=0; i<TypeNum.GetAmount(); i++){
		if (TypeNum[i]->Prepare()==false)	return 0;
	};
	pUnitsGRP->RefreshUnits();
	switch(Status) {
	case 0:
		if (GetTickCount()-LastProdTime>=ProduceTime&&pUnitsGRP->N==0){
			lvCGroup* pvBLD = GroupsMap()->GetGroupID(vgBildsID);
			if (pvBLD!=NULL) {
				OneObject* pOB = pvBLD->GetOneObj_lID(0);
				if (pOB!=NULL) {
                    // Выбираем че нам строить.
					lvCGroup* pvg = NULL;
					for (int i=0; i<TypeNum.GetAmount(); i++){
						pvg = GroupsMap()->GetGroupID(TypeNum[i]->vgUnitsID);
						if (pvg!=NULL) {
							if (pvg->GetTotalAmount2(TypeNum[i]->UnitType)<TypeNum[i]->Num){
								pOB->Produce(TypeNum[i]->UnitType,pUnitsGRP->Index);
								TypePID = i;
								i = TypeNum.GetAmount();

								Order1* Ord=pOB->LocalOrder;
								if (Ord!=NULL){
									while(Ord->NextOrder){
										Ord=Ord->NextOrder;
									};
									Ord->info.Produce.Progress = Ord->info.Produce.NStages;
								};
								LastProdTime = GetTickCount();
								Status = 1;
							};
						};
					};
				};
			};
		}else if (pUnitsGRP->N>0&&GetTickCount()-LastProdTime>=ProduceTime){
			Status=1;
		};
		break;
	case 1:
		if (pUnitsGRP->N==0&&GetTickCount()-LastProdTime>=ProduceTime){
			Status=0;
			LastProdTime = GetTickCount();
		};
		if (pUnitsGRP->N>0) {
			int N=pUnitsGRP->N;
			while (N--) {
				OneObject* pOB = Group[pUnitsGRP->IDS[N]];
				if (pOB!=NULL&&(!pOB->Sdoxlo||pOB->Hidden)){
					lvCGroup* pvUNIT = GroupsMap()->GetGroupID(TypeNum[TypePID]->vgUnitsID);
					if (pvUNIT!=NULL){
						pvUNIT->AddUnitGPT(pOB);
						pvUNIT->GetTotalAmount();
						pUnitsGRP->RemoveUnit(pOB);
					};
				};
			};
		};
		break;
	case 2:
		break;
	};
	return 0;
};

void			lvCSpotNUnits::Create(){
	if (UGRP_ID==0xFFFF){
		pUnitsGRP = SCENINF.GetNewGroup();
		UGRP_ID = pUnitsGRP->Index;
		LastProdTime = GetTickCount();
		lvCGroup* pvBLD = GroupsMap()->GetGroupID(vgBildsID);
		OneObject* OB = pvBLD->GetOneObj_lID(0);
		if(OB&&(OB->Hidden||!OB->Sdoxlo)&&(!OB->LocalOrder)&&OB->Ref.General->CanDest){
			if (UseNode) {
				lvCNode* pNode = NodesMap()->vGetNode(parNode);
				if (pNode!=NULL) {
					OB->DestX=pNode->vGetX();
					OB->DestY=pNode->vGetY();
				};
			}else if (0<=aZDestPoint && aZDestPoint<AZones.GetAmount()){
				ActiveZone* pZ = AZones[aZDestPoint];
				if (pZ!=NULL){
					OB->DstX=(pZ->x)<<4;
					OB->DstY=(pZ->y)<<4;
				};
			};
		};
	};
	pUnitsGRP = &(SCENINF.UGRP[UGRP_ID]);
};
// lvCGoInBattle /////////////////////////////////////////////////////////
lvGrpNumBld::lvGrpNumBld(lvGrpNumBld* pGrpNumBld){
	if (pGrpNumBld!=NULL) {
		InfID	= pGrpNumBld->InfID;
        lvCCondition* pCond=NULL;
		for (int i=0; i<pGrpNumBld->Cond.GetAmount(); i++){
			pGrpNumBld->Cond[i]->GetCopy(&pCond);
			if (pCond!=NULL) {
				Cond.Add(pCond);
			};
			pCond=NULL;
		};
		vgUnits	= pGrpNumBld->vgUnits;
		vgBildg	= pGrpNumBld->vgBildg;
		Use_VV	= pGrpNumBld->Use_VV;
		Num		= pGrpNumBld->Num;
		if (pGrpNumBld->vNum.Get()!=NULL) vNum.Set(pGrpNumBld->vNum.Get());
		AttackerType	= pGrpNumBld->AttackerType;
		Diff			= pGrpNumBld->Diff;
	};
};

void			lvGrpNumBld::GetCopy(lvGrpNumBld** pCopy){
	*pCopy = new lvGrpNumBld(this);
};

bool			lvGrpNumBld::Prepare(){
	if (Use_VV){
		if (vNum.Get()!=NULL){
			Num = vNum.Get()->Value;
		}else{
			return false;
		};
	};
	return true;
};

void			lvGrpNumBld::Process(int time){
	int CondN = Cond.GetAmount();
	int	a_start = 1;
	while (a_start==1&&CondN--) {
		if (Cond[CondN]->GetValue(time)==0) a_start=0;
	};
	lvCGroup* pvgUnits = GroupsMap()->GetGroupID(vgUnits);
	lvCGroup* pvgBuild = GroupsMap()->GetGroupID(vgBildg);
	if (pvgUnits==NULL)	return;
	if ( ( a_start && pvgUnits->GetTotalAmount()>=Num ) || ( pvgBuild!=NULL && pvgBuild->GetTotalAmount()==0 && pvgUnits->GetTotalAmount()>0 ) ){
		switch(AttackerType) {
		case 0:
			AddFirers(pvgUnits,pvgUnits->GetNation(),RemoveAfterSend);
			break;
		case 1:
			AddTomahawks(pvgUnits,pvgUnits->GetNation(),RemoveAfterSend,0,0,0);
			break;
		case 2:
			AddPsKillers(pvgUnits,pvgUnits->GetNation(),RemoveAfterSend,false);
			break;
		case 3:
			AddStorm(pvgUnits,pvgUnits->GetNation(),Diff,RemoveAfterSend);
			break;
		};
	};
};

lvCGoInBattle::lvCGoInBattle(lvCGoInBattle* pGoInBattle) : lvCOperation(dynamic_cast<lvCOperation*>(pGoInBattle)) {
	if (pGoInBattle!=NULL) {
		lvGrpNumBld* pGNB=NULL;
		for (int i=0; i<pGoInBattle->GrpNumBld.GetAmount(); i++){
			pGoInBattle->GrpNumBld[i]->GetCopy(&pGNB);
			if (pGNB!=NULL) {
				GrpNumBld.Add(pGNB);
			};
			pGNB=NULL;
		};	
	};
};

void			lvCGoInBattle::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCGoInBattle(this));
};

const	char*	lvCGoInBattle::GetThisElementView(const char* LocalName){
	Descr = "SendUnitsInBattle";
	return Descr.str;
};

int				lvCGoInBattle::Process(int time){
	lvCOperation::Process(time);
	int N = GrpNumBld.GetAmount();
	while (N--) {
		if (GrpNumBld[N]->Prepare()){
			GrpNumBld[N]->Process(time);
		};
	};
	return 0;
};
// lvCArtAttack //////////////////////////////////////////////////////////
lvCArtAttack::lvCArtAttack(lvCArtAttack* pArtAttack) : lvCOperation(dynamic_cast<lvCOperation*>(pArtAttack)) {
	if (pArtAttack!=NULL) {
		vgArtID		= pArtAttack->vgArtID;
		vgTargID	= pArtAttack->vgTargID;
		AttackImid	= pArtAttack->AttackImid;
		if (pArtAttack->AttackActive.Get()!=NULL)	AttackActive.Set(pArtAttack->AttackActive.Get());
	};
};

void			lvCArtAttack::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCArtAttack(this));
};

const	char*	lvCArtAttack::GetThisElementView(const char* LocalName){
	Descr = "ArtAttack(";
	if (true) {
	};
	Descr += ")";
	return Descr.str;
};

int				lvCArtAttack::Process(int time){
	lvCOperation::Process(time);
	if (AttackActive.Get()!=NULL&&AttackActive.Get()->Value==true){
		lvCGroup* pvgArt  = GroupsMap()->GetGroupID(vgArtID);		
		lvCGroup* pvgTarg = GroupsMap()->GetGroupID(vgTargID);
		if (pvgTarg!=NULL&&pvgTarg->GetTotalAmount()==0){
			return 1;
		};
		if (pvgArt!=NULL&&pvgTarg!=NULL){
			if (!pvgArt->GetORDER()) {
				// Select target
				int N = pvgTarg->GetTotalAmount();
				OneObject* pOTrg = NULL;
				while (pOTrg==NULL&&N--) {
					pOTrg = pvgTarg->GetOneObj_lID(N);
				};
				assert(pOTrg);
				if (pOTrg==NULL)	return 1;
				// Attack
				N = pvgArt->GetTotalAmount();
				OneObject* pOArt = NULL;
				while (N--) {
					pOArt = pvgArt->GetOneObj_lID(N);
					if (pOArt!=NULL) {
						int minAD = (int)pOArt->Ref.General->MoreCharacter->MinR_Attack;
						int maxAD = (int)pOArt->Ref.General->MoreCharacter->MaxR_Attack;
						int Dist = Norma((pOArt->RealX-pOTrg->RealX)>>4,(pOArt->RealY-pOTrg->RealY)>>4);
						if (minAD<Dist&&Dist<maxAD){
							pOArt->AttackObj(pOTrg->Index,128+15,0,0);
						};
					//	pOArt->AttackObj(pOTrg->Index,128+15,0,0);
					};
				};
			};
		};
	};
	return 0;
};

int				lvCArtAttack::Complite(){
	lvCGroup* pvgTarg = GroupsMap()->GetGroupID(vgTargID);
	if (pvgTarg!=NULL&&pvgTarg->GetTotalAmount()==0){
		return 1;
	};
	return 0;
};

// lvCPutNewSquad ////////////////////////////////////////////////////////
lvCPutNewSquad::lvCPutNewSquad(lvCPutNewSquad* pPutNewSquad) : lvCOperation(dynamic_cast<lvCOperation*>(pPutNewSquad)) {
	if (pPutNewSquad!=NULL) {
		GrpID		= pPutNewSquad->GrpID;
		Nat			= pPutNewSquad->Nat;
		UnitType	= pPutNewSquad->UnitType;
		SizeType	= pPutNewSquad->SizeType;
		Use_Zone	= pPutNewSquad->Use_Zone;
		x			= pPutNewSquad->x;
		y			= pPutNewSquad->y;
		dir			= pPutNewSquad->dir;
		ZoneID		= pPutNewSquad->ZoneID;
	};
};

void			lvCPutNewSquad::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCPutNewSquad(this));
};

const	char*	lvCPutNewSquad::GetThisElementView(const char* LocalName){
	Descr = "PutNewSquad(";
	lvCGroup*	pGrp = GroupsMap()->GetGroupID(GrpID);
	if (pGrp!=NULL){
		Descr += pGrp->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += ",";
	if (Nat>=0&&Nat<8){
		Descr += Nat;
	}else{
		Descr += "NoNat";
	};
	Descr += ",";
	Descr += SizeType;
	Descr += ",";
	if (Use_Zone) {
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode){
				Descr += pNode->vGetName();
			}else	Descr += "NoNode";
		}else if (0<=ZoneID&&ZoneID<AZones.GetAmount()){
			Descr += AZones[ZoneID]->Name.str;
		}else	Descr += "NoZone";
	}else{
		Descr += x;
		Descr += ",";
		Descr += y;
	};
	Descr += ",";
	Descr += dir;
	Descr += ")";
	return Descr.str;
};

int				lvCPutNewSquad::Process(int time){
	lvCOperation::Process(time);
	if (Nat<0||Nat>=7)	return 0;
	lvCGroup*	pGrp = GroupsMap()->GetGroupID(GrpID);
	if (pGrp!=NULL){
		if (pGrp->GetTotalAmount()!=0&&pGrp->GetNation()!=Nat)	return 0;

		if (Use_Zone){
			if (UseNode) {
				lvCNode* pNode = NodesMap()->vGetNode(parNode);
				if (pNode) {
					x = pNode->vGetX();
					y = pNode->vGetY();
				}else return 0;
			}else if (0<=ZoneID&&ZoneID<AZones.GetAmount()) {
				x = AZones[ZoneID]->x;
				y = AZones[ZoneID]->y;
			}else return 0;			
		}else	return 0;

		FormT = SizeType/100;
		int newBrigID = PutNewFormationBySizeIndex((byte)Nat,(word)UnitType,SizeType%100,FormT,x<<4,y<<4,(byte)dir);
		if (newBrigID!=0xFFFF){
			Brigade* pBR=CITY[Nat].Brigs+newBrigID;
			if (pBR!=NULL){
				for (int i=0; i<pBR->NMemb&&pBR->Memb!=NULL; i++){
					pGrp->AddUnitGID(pBR->Memb[i]);
				};
				return 1;
			};
		};
	};
	return 0;
};
int GetUTypeByName_lua(const char* UTName){
	int TID=-1;
	for(int i=0;(TID==-1)&&(i<NATIONS->NMon);i++){
		if(NATIONS->Mon[i]->MonsterID && !strcmp(UTName,NATIONS->Mon[i]->MonsterID)){
			TID=i;
		}
	}
	return TID;
};
int PutNewSquad_lua(lvCGroup* pGRP,int nat,int uType,int size,int x,int y,int dir){
	int newBrigID = PutNewFormationBySizeIndex((byte)nat,(word)uType,size%100,size/100,x<<4,y<<4,(byte)dir);
	if (newBrigID!=0xFFFF){
		Brigade* pBR=CITY[nat].Brigs+newBrigID;
		if (pBR!=NULL){
			for (int i=0; i<pBR->NMemb&&pBR->Memb!=NULL; i++){
				pGRP->AddUnitGID(pBR->Memb[i]);
			};
			return 1;
		};
	};
	return 0;
};
// lvCPutNewFormation ////////////////////////////////////////////////////
lvCPutNewFormation::lvCPutNewFormation(lvCPutNewFormation* pPutNewFormation) : lvCOperation(dynamic_cast<lvCOperation*>(pPutNewFormation)) {
	if (pPutNewFormation!=NULL) {
		vGrpID		= pPutNewFormation->vGrpID;
		Nat			= pPutNewFormation->Nat;
		Form		= pPutNewFormation->Form;
		UType		= pPutNewFormation->UType;
		dir			= pPutNewFormation->dir;
		Use_Zone	= pPutNewFormation->Use_Zone;
		ZoneID		= pPutNewFormation->ZoneID;
		if (pPutNewFormation->Point.Get()!=NULL) Point.Set(pPutNewFormation->Point.Get());
	};
};

void			lvCPutNewFormation::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCPutNewFormation(this));
};

const	char*	lvCPutNewFormation::GetThisElementView(const char* LocalName){
	Descr = "PutNewFormation(";
	lvCGroup*	pGrp = GroupsMap()->GetGroupID(vGrpID);
	// VGroup
	if (pGrp!=NULL){					
		Descr += pGrp->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += ",";
	// Nation
	if (0<=Nat&&Nat<=7){
		Descr += Nat;
	}else{
		Descr += "NoNat";
	};
	Descr += ",";
	// Formation
	OrderDescription* ODS=NULL;
	if (0<=Form&&Form<255) {
		ODS = ElementaryOrders+Form;
	};
	if (ODS!=NULL) {
		Descr += ODS->ID;
	}else{
		Descr += "NoForm";
	};
	Descr += ",";
	// Units Type
	Descr += NATIONS[Nat].Mon[UType]->Message;
	Descr += ",";
	// Direction
	Descr += dir;
	Descr += ",";
	// Creation Position
	if (Use_Zone) {
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode){
				Descr += pNode->vGetName();
			}else	Descr += "NoNode";
		}else if (0<=ZoneID&&ZoneID<AZones.GetAmount()){
			Descr += AZones[ZoneID]->Name.str;
		}else{
			Descr += "NoZone";
		};
	}else{
		if (Point.Get()!=NULL){
			Descr += (Point.Get())->GetName();
		}else{
			Descr += "NoPoint";
		};
	};
	
	Descr += ")";
	return Descr.str;
};	

int				lvCPutNewFormation::Process(int time){
	lvCOperation::Process(time);
	lvCGroup*	pGrp = GroupsMap()->GetGroupID(vGrpID);

	int xc=-1;
	int yc=-1;
	if (Use_Zone) {
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode) {
				xc = pNode->vGetX()<<4;
				yc = pNode->vGetY()<<4;
			};
		}else if (ZoneID>=0||ZoneID<AZones.GetAmount()) {
			xc = (AZones[ZoneID]->x)<<4;
			yc = (AZones[ZoneID]->y)<<4;
		};
	}else if (Point.Get()!=NULL){
		xc = ((Point.Get())->Value.x)<<4;
		yc = ((Point.Get())->Value.y)<<4;
	};


	if (pGrp==NULL||
		Nat<0||Nat>7||
		Form<0||Form>255||
		xc==-1||yc==-1)		return 0;

	OrderDescription* ODS=ElementaryOrders+Form;
	int N=ODS->NUnits;
	PORD.CreateSimpleOrdPos(xc,yc,dir,ODS->NUnits,NULL,ODS);
	word NewIds[1024];
	int NU=0;
	for(int j=0;j<N;j++){
		int CreateNewTerrMons2(byte NI,int x,int y,word Type);
		int ID=CreateNewTerrMons2(Nat,PORD.px[j],PORD.py[j],UType);
		if(ID!=-1&&NU<1024){
			NewIds[NU]=ID;
			NU++;
		};
	};
	for(int j=0;j<NU;j++){
		pGrp->AddUnitGID(NewIds[j]);
	};
	return 1;
};
//extern int NEOrders;
//extern OrderDescription ElementaryOrders [256];
int GetFormationID_lua(const char* formID){
	for(int i=0;i<NEOrders;i++)if(!strcmp(formID,ElementaryOrders[i].ID))return i;
	return 0xFFFFFFFF;
};
int PutNewFormation_lua(lvCGroup* pGRP,int nat,int uType,int form,int x,int y,int dir){
	if (pGRP==NULL||nat<0||nat>7||form<0||form>255)		return 0;
	OrderDescription* ODS=ElementaryOrders+form;
	int N=ODS->NUnits;
	PORD.CreateSimpleOrdPos(x<<4,y<<4,dir,ODS->NUnits,NULL,ODS);
	word NewIds[1024];
	int NU=0;
	for(int j=0;j<N;j++){
		int CreateNewTerrMons2(byte NI,int x,int y,word Type);
		int ID=CreateNewTerrMons2(nat,PORD.px[j],PORD.py[j],uType);
		if(ID!=-1&&NU<1024){
			NewIds[NU]=ID;
			NU++;
		};
	};
	for(int j=0;j<NU;j++){
		pGRP->AddUnitGID(NewIds[j]);
	};
	return 1;
};
// lvCSetUnitStateCII ///////////////////////////////////////////////////////
lvCSetUnitStateCII::lvCSetUnitStateCII(lvCSetUnitStateCII* pSetUnitStateCII) : lvCOperation(dynamic_cast<lvCOperation*>(pSetUnitStateCII)) {
	if (pSetUnitStateCII!=NULL) {
		GrpID	= pSetUnitStateCII->GrpID;
		Fire	= pSetUnitStateCII->Fire;
		LineI	= pSetUnitStateCII->LineI;
		LineII	= pSetUnitStateCII->LineII;
		LineIII	= pSetUnitStateCII->LineIII;
		Stiki	= pSetUnitStateCII->Stiki;
	};
};

void			lvCSetUnitStateCII::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetUnitStateCII(this));
};

const	char*	lvCSetUnitStateCII::GetThisElementView(const char* LocalName){
	Descr = "SetBrigStateCII()";
	return Descr.str;
};

int				lvCSetUnitStateCII::Process(int time){
	lvCOperation::Process(time);
	lvCGroup*	pGrp = GroupsMap()->GetGroupID(GrpID);
	if (pGrp==NULL)	return 0;

	if (SetSG_Immediately){
		pGrp->SetInStandGround();
		return 1;
	};

	int Nat = pGrp->GetNation();
	if (Nat<0||Nat>7)	return 0;
	// Set RifleAttack, Brigade not need.
	int N = pGrp->GetTotalAmount();
	OneObject* pOB = NULL;
	bool	FireState = Fire;
	if (Fire&&(LineI||LineII||LineIII))	FireState=false;
	while (N--) {
		pOB = pGrp->GetOneObj_lID(N);
		if (pOB!=NULL) {
			pOB->RifleAttack=FireState;
		};
	};
	// Finde Brigade by line.
	N = pGrp->GetTotalAmount();
	Brigade* pBR = NULL;
	pOB = NULL;
	while (pBR==NULL&&N--) {
		pOB = pGrp->GetOneObj_lID(N);
		if (pOB!=NULL&&pOB->BrigadeID!=0xFFFF) {
			pBR = CITY[Nat].Brigs+pOB->BrigadeID;
		};
	};
	if (pBR==NULL)	return 0;
	if (Fire&&(LineI||LineII||LineIII)){
		void ComShotLine(byte NI,byte State,int Line,int BID);
		if (LineI)		ComShotLine(Nat,0,4,pBR->ID);
		if (LineII)		ComShotLine(Nat,0,2,pBR->ID);
		if (LineIII)	ComShotLine(Nat,0,0,pBR->ID);
	};
	// Opustit shtiki
	if (Fire==false){
		pBR->AttEnm=Stiki;
		N = pGrp->GetTotalAmount();
		while (N--) {
			pOB = pGrp->GetOneObj_lID(N);
			if (pOB!=NULL) {
				if (Stiki)	pOB->NewState=1;
				else		pOB->NewState=0;
			};
		};
	};
	return 1;
};
int SetUnitStateCII_lua(lvCGroup* pGrp,bool shtiki,bool Fire,bool LI,bool LII,bool LIII){
	if (pGrp==NULL) return 0;
	int Nat = pGrp->GetNation();
	if (Nat<0||Nat>7)	return 0;
	// Set RifleAttack, Brigade not need.
	int N = pGrp->GetTotalAmount();
	OneObject* pOB = NULL;
	bool	FireState = Fire;
	if (Fire&&(LI||LII||LIII))	FireState=false;
	while (N--) {
		pOB = pGrp->GetOneObj_lID(N);
		if (pOB!=NULL) {
			pOB->RifleAttack=FireState;
		};
	};
	// Finde Brigade by line.
	N = pGrp->GetTotalAmount();
	Brigade* pBR = NULL;
	pOB = NULL;
	while (pBR==NULL&&N--) {
		pOB = pGrp->GetOneObj_lID(N);
		if (pOB!=NULL&&pOB->BrigadeID!=0xFFFF) {
			pBR = CITY[Nat].Brigs+pOB->BrigadeID;
		};
	};
	if (pBR==NULL)	return 0;
	if (Fire&&(LI||LII||LIII)){
		void ComShotLine(byte NI,byte State,int Line,int BID);
		if (LI)		ComShotLine(Nat,0,4,pBR->ID);
		if (LII)	ComShotLine(Nat,0,2,pBR->ID);
		if (LIII)	ComShotLine(Nat,0,0,pBR->ID);
	};
	// Opustit shtiki
	if (Fire==false){
		pBR->AttEnm=shtiki;
		N = pGrp->GetTotalAmount();
		while (N--) {
			pOB = pGrp->GetOneObj_lID(N);
			if (pOB!=NULL) {
				if (shtiki)	pOB->NewState=1;
				else		pOB->NewState=0;
			};
		};
	};
	return 1;
};
// lvCSendStikiToZone ////////////////////////////////////////////////////
lvCSendStikiToZone::lvCSendStikiToZone(lvCSendStikiToZone* plvCSendStikiToZone) : lvCOperation(dynamic_cast<lvCOperation*>(plvCSendStikiToZone)) {
	if (plvCSendStikiToZone!=NULL) {
		GrpID	= plvCSendStikiToZone->GrpID;
		ZoneID	= plvCSendStikiToZone->ZoneID;
		dir		= plvCSendStikiToZone->dir;
		prio	= plvCSendStikiToZone->prio;	
	};
};

void			lvCSendStikiToZone::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSendStikiToZone(this));
};

const	char*	lvCSendStikiToZone::GetThisElementView(const char* LocalName){
	Descr = "SendStikiToZone()";
	return Descr.str;
};

int				lvCSendStikiToZone::Process(int time){
	lvCOperation::Process(time);
	lvCGroup*	pGrp = GroupsMap()->GetGroupID(GrpID);
	if (pGrp==NULL)	return 0;

	int Nat = pGrp->GetNation();
	if (Nat<0||Nat>7)	return 0;

    if (0>ZoneID&&ZoneID>=AZones.GetAmount())	return 0;
	int rX = (AZones[ZoneID]->x)<<4;
	int rY = (AZones[ZoneID]->y)<<4;

	int	N = pGrp->GetTotalAmount();
	Brigade* pBR = NULL;
	OneObject* pOB = NULL;
	while (pBR==NULL&&N--) {
		pOB = pGrp->GetOneObj_lID(N);
		if (pOB!=NULL&&pOB->BrigadeID!=0xFFFF) {
			pBR = CITY[Nat].Brigs+pOB->BrigadeID;
		};
	};
	if (pBR==NULL)	return 0;

	pBR->AttEnm=true;
	N = pGrp->GetTotalAmount();
	while (N--) {
		pOB = pGrp->GetOneObj_lID(N);
		if (pOB!=NULL) {
			pOB->NewState=1;
		};
	};

	pBR->HumanLocalSendTo(rX,rY,dir,128,0);

	return 1;
};

// lvCSetUnitEnableState /////////////////////////////////////////////////
lvCSetUnitEnableState::lvCSetUnitEnableState(lvCSetUnitEnableState* pSetUnitEnableState) : lvCOperation(dynamic_cast<lvCOperation*>(pSetUnitEnableState)) {
	if (pSetUnitEnableState!=NULL) {
		Nat		= pSetUnitEnableState->Nat;
		TypeID	= pSetUnitEnableState->TypeID;
		State	= pSetUnitEnableState->State;
	};
};

void			lvCSetUnitEnableState::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetUnitEnableState(this));
};

const	char*	lvCSetUnitEnableState::GetThisElementView(const char* LocalName){
	Descr = "SetUnitEnableState(";
	Descr += Nat;
	Descr += ",";
	Descr += State;
	Descr += ",";
	Descr += NATIONS[Nat].Mon[TypeID]->Message;
	Descr += ")";
	return Descr.str;
};

int				lvCSetUnitEnableState::Process(int time){
	lvCOperation::Process(time);
	if (Nat<0||Nat>7)	return 0;

	GeneralObject* GO=NATIONS[Nat].Mon[TypeID];
	GO->ManualEnable=0;
	GO->ManualDisable=0;
	if(State>0)GO->ManualEnable=1;
	else if(State<0)GO->ManualDisable=1;

	return 1;
};

// lvCSetUpgradeEnableStatus /////////////////////////////////////////////
lvCSetUpgradeEnableStatus::lvCSetUpgradeEnableStatus(lvCSetUpgradeEnableStatus* pSetUpgradeEnableStatus) : lvCOperation(dynamic_cast<lvCOperation*>(pSetUpgradeEnableStatus)) {
	if (pSetUpgradeEnableStatus!=NULL) {
		Nat			= pSetUpgradeEnableStatus->Nat;
		UpgradeID	= pSetUpgradeEnableStatus->UpgradeID;
		State		= pSetUpgradeEnableStatus->State;
	};
};

void			lvCSetUpgradeEnableStatus::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetUpgradeEnableStatus(this));
};

const	char*	lvCSetUpgradeEnableStatus::GetThisElementView(const char* LocalName){
	Descr = "SetUpgradeEnableStatus(";
	Descr += Nat;
	Descr += ",";
	Descr += State;
	Descr += ",";
	Descr += NATIONS[Nat].UPGRADE[UpgradeID]->Name;
	Descr += ")";
	return Descr.str;
};

int				lvCSetUpgradeEnableStatus::Process(int time){
	lvCOperation::Process(time);
	if (Nat<0||Nat>7)	return 0;

	NewUpgrade* NU=NATIONS[Nat].UPGRADE[UpgradeID];
	NU->ManualEnable=0;
	NU->ManualDisable=0;
	if(State>0)NU->ManualEnable=1;
	else if(State<0)NU->ManualDisable=1;

	return 1;
};

void PerformNewUpgrade(Nation* NT,int UIndex,OneObject* OB);
// lvCSetUpgradeDone /////////////////////////////////////////////////////
lvCSetUpgradeDone::lvCSetUpgradeDone(lvCSetUpgradeDone* pSetUpgradeDone) : lvCOperation(dynamic_cast<lvCOperation*>(pSetUpgradeDone)) {
	if (pSetUpgradeDone!=NULL) {
		Nat			= pSetUpgradeDone->Nat;
		GrpID		= pSetUpgradeDone->GrpID;
		UpgradeID	= pSetUpgradeDone->UpgradeID;
	};
};

void			lvCSetUpgradeDone::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetUpgradeDone(this));
};

const	char*	lvCSetUpgradeDone::GetThisElementView(const char* LocalName){
	Descr = "SetUpgradeDone(";
	Descr += Nat;
	Descr += ",";
	if(0<=GrpID){
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			Descr += pvGRP->GetGroupName();
		};
	}else{
		Descr +="NULL";
	};
	Descr += ",";
	Descr += NATIONS[Nat].UPGRADE[UpgradeID]->Name;
	Descr += ")";
	return Descr.str;
};

int				lvCSetUpgradeDone::Process(int time){
	lvCOperation::Process(time);
	if (Nat>-1&&Nat<8)
	{
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if(pvGRP){
			int lInd=0;
			OneObject* pOB = pvGRP->GetOneObj_lID(lInd);
			if(pOB)
			{
				Nation* NNT=&NATIONS[Nat];
				PerformNewUpgrade(NNT,UpgradeID,pOB);
				NewUpgrade* NU=NNT->UPGRADE[UpgradeID];
				NU->Done=true;
				NU->PermanentEnabled=0;
				NU->Enabled=0;
				return 1;
			};
		};
	};
	return 0;
};
// lvCTeleport ///////////////////////////////////////////////////////////
lvCTeleport::lvCTeleport(lvCTeleport* pTeleport) : lvCOperation(dynamic_cast<lvCOperation*>(pTeleport)) {
	if (pTeleport!=NULL) {
		vGrpID		= pTeleport->vGrpID;	
		Use_VVal	= pTeleport->Use_VVal;	
		ZoneID		= pTeleport->ZoneID;
		if (pTeleport->Point.Get()!=NULL)	Point.Set(pTeleport->Point.Get());
	};
};

void			lvCTeleport::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCTeleport(this));
};

const	char*	lvCTeleport::GetThisElementView(const char* LocalName){
	Descr = "TeleportTo";
	if (Use_VVal)	Descr += "Point";
	else{
		if (UseNode)	Descr += "Node";
		else			Descr += "Zone";
	};
	Descr += "(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ",";
	if (!Use_VVal) {
		if (UseNode) {
			lvCNode* pNode = NodesMap()->vGetNode(parNode);
			if (pNode!=NULL) {
				Descr += pNode->vGetName();
			}else{
				Descr += "NoNode";
			}
		}else{
			if (0<=ZoneID&&ZoneID<AZones.GetAmount()){
				Descr += AZones[ZoneID]->Name.str;
			}else{
				Descr += "NoZone";
			};		
		};
	}else{
		if (Point.Get()!=NULL) {
			Descr += (Point.Get())->Name.str;
		}else{
			Descr += "NoPoint";
		};
	};
	Descr += ")";

	return Descr.str;
};

int				lvCTeleport::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		int destRX=0;
		int destRY=0;
		if (Use_VVal) {
			if (Point.Get()!=NULL) {
				destRX = ((Point.Get())->Value.x)<<4;
				destRY = ((Point.Get())->Value.y)<<4;
			};
		}else{
			if (UseNode) {
				lvCNode* pNode = NodesMap()->vGetNode(parNode);
				if (pNode!=NULL){
					destRX = pNode->vGetX()<<4;
					destRY = pNode->vGetY()<<4;
				};
			}else{
				if (0<=ZoneID&&ZoneID<AZones.GetAmount()){
					destRX = (AZones[ZoneID]->x)<<4;
					destRY = (AZones[ZoneID]->y)<<4;
				};
			};
		};
		int deltaX = 0;
		int deltaY = 0;
		if (destRX!=0||destRY!=0) {
			int cRX,cRY;
			pvGrp->GetGroupCenter(cRX,cRY);
			cRX = cRX<<4;
			cRY = cRY<<4;
			int dlRX = destRX-cRX;
			int dlRY = destRY-cRY;
			if (Direction!=512)	{
				float fi = ((float)Direction/256.f) * (2.f*3.1414f);
				deltaX = (int)(-(float)dX*cosf(fi))<<4;
				deltaY = (int)(-(float)dY*sinf(fi))<<4;
			};
			OneObject* pOB = NULL;
			int N = pvGrp->GetTotalAmount();
			void ChangeUnitCoor(OneObject* OB,int newX,int newY);
			int newRX=0;
			int newRY=0;
			while (N--) {
				pOB = pvGrp->GetOneObj_lID(N);
				if (pOB!=NULL) {
					pOB->ClearOrders();
					newRX = pOB->RealX + dlRX + deltaX;
					newRY =	pOB->RealY + dlRY + deltaY;
					ChangeUnitCoor(pOB,newRX,newRY);
					pOB->DeletePath();
					pOB->DestX=-1;
				};
			};
			// change position for Briade (if brigade present)
			LinearArray<int,_int> BrigIDList;
			pvGrp->GetBrigadeList(false,&BrigIDList);
			int NBR=BrigIDList.GetAmount();
			if (NBR>0){
				int Nat=pvGrp->GetNation();
				Brigade* pBR = NULL;
				while (NBR--) {
					pBR = CITY[Nat].Brigs+BrigIDList[NBR];
					// change position for cur Brigade
					pBR->ClearNewBOrders();
					for (int ii=3; ii<pBR->NMemb; ii++){
						if (pBR->Memb[ii]!=0xFFFF){
							pOB = Group[pBR->Memb[ii]];
							if (pOB!=NULL&&(!pOB->Sdoxlo||pOB->Hidden)){
								pBR->posX[ii] = Group[pBR->Memb[ii]]->RealX>>4/*dlRX>>4*/;
								pBR->posY[ii] = Group[pBR->Memb[ii]]->RealY>>4/*dlRY>>4*/;
							};
						};						
					};
				};
			};
			if (Direction!=512){
				pvGrp->SendTo((cRX+dlRX)>>4,(cRY+dlRY)>>4,Direction);
			};
			return 1;
		};
	};
	return 0;
};

// lvCDisband ////////////////////////////////////////////////////////////
lvCDisband::lvCDisband(lvCDisband* pDisband) : lvCOperation(dynamic_cast<lvCOperation*>(pDisband)) {
	if (pDisband!=NULL) {
		vGrpID = pDisband->vGrpID;
	};
};

void			lvCDisband::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCDisband(this));
};

const	char*	lvCDisband::GetThisElementView(const char* LocalName){
	Descr = "Disband(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	return Descr.str;
};

int				lvCDisband::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		LinearArray<int,_int> ListBR;
		int NI = pvGrp->GetNation();
		pvGrp->GetBrigadeList(false,&ListBR);
		if (0<=NI && NI<8){
			if (ListBR.GetAmount()>0) {
				Brigade* pBR=&CITY[NI].Brigs[ListBR[0]];
				if (pBR!=NULL) {
					void EraseBrigade(Brigade* BR);
					EraseBrigade(pBR);
					return 1;
				};	
			};
		};
	};
	return 0;
};

// lvCScare //////////////////////////////////////////////////////////////
lvCScare::lvCScare(lvCScare* pScare) : lvCOperation(dynamic_cast<lvCOperation*>(pScare)) {
	if (pScare!=NULL) {
		vGrpID = pScare->vGrpID;
	};
};

void			lvCScare::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCScare(this));
};

const	char*	lvCScare::GetThisElementView(const char* LocalName){
	Descr = "Scare(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	return Descr.str;
};

int				lvCScare::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		LinearArray<int,_int> ListBR;
		int NI = pvGrp->GetNation();
		pvGrp->GetBrigadeList(false,&ListBR);
		if (0<=NI && NI<8){
			if (ListBR.GetAmount()>0) {
				Brigade* pBR=&CITY[NI].Brigs[ListBR[0]];
				if (pBR!=NULL) {
					pBR->Morale=0;
					return 1;
				};	
			};
		};
	};
	return 0;
};

// lvCClearSG ////////////////////////////////////////////////////////////
lvCClearSG::lvCClearSG(lvCClearSG* pClearSG) : lvCOperation(dynamic_cast<lvCOperation*>(pClearSG)) {
	if (pClearSG!=NULL) {
		vGrpID = pClearSG->vGrpID;
	};
};

void			lvCClearSG::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCClearSG(this));
};

const	char*	lvCClearSG::GetThisElementView(const char* LocalName){
	Descr = "ClearSG(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	return Descr.str;
};

int				lvCClearSG::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		LinearArray<int,_int> ListBR;
		int N=pvGrp->GetTotalAmount();
		OneObject* pOB=NULL;
		while (N--) {
			pOB=pvGrp->GetOneObj_lID(N);
			if (pOB) pOB->StandTime=0;
		};
		int NI = pvGrp->GetNation();
		pvGrp->GetBrigadeList(false,&ListBR);
		if (0<=NI && NI<8){
			for (int i=0; i<ListBR.GetAmount(); i++){
				Brigade* pBR=&CITY[NI].Brigs[ListBR[i]];
				if (pBR!=NULL) {
					void CancelStandGroundAnyway(Brigade* BR);
					CancelStandGroundAnyway(pBR);
				};	
			};
			return 1;
		};
	};
	return 0;
};
// lvCUnloadSquad ////////////////////////////////////////////////////////
lvCUnloadSquad::lvCUnloadSquad(lvCUnloadSquad* pUnloadSquad): lvCOperation(dynamic_cast<lvCOperation*>(pUnloadSquad)){
	if (pUnloadSquad!=NULL) {
		vGRP = pUnloadSquad->vGRP;
	};
};
const	char*	lvCUnloadSquad::GetThisElementView(const char* LocalName){
	Descr = "UnloadSquad(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGRP);
	if (pvGrp!=NULL&&pvGrp->NAME.str!=NULL)		Descr += pvGrp->NAME.str;
	else										Descr += "NoGroup";
	Descr += ")";
	return Descr.str;
};
int				lvCUnloadSquad::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGRP);
	if (pvGrp!=NULL) {
		OneObject* pOB=NULL;
		int N=pvGrp->GetTotalAmount();
		while (N--) {
			pOB=pvGrp->GetOneObj_lID(N);
			if (pOB!=NULL) {
				pOB->MaxDelay=200;
				pOB->delay=200;
			};
			pOB=NULL;
		};
		return 1;
	};
	return 0;
};
void			lvCUnloadSquad::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCUnloadSquad(this));
};
// lvCSpotGrpByUType /////////////////////////////////////////////////////
lvCSpotGrpByUType::lvCSpotGrpByUType(lvCSpotGrpByUType* pSpotGrpByUType) : lvCOperation(dynamic_cast<lvCOperation*>(pSpotGrpByUType)) {
	if (pSpotGrpByUType!=NULL) {
		vGrpID		= pSpotGrpByUType->vGrpID;
		UnitType	= pSpotGrpByUType->UnitType;
		Nat			= pSpotGrpByUType->Nat;
	};
};

const	char*	lvCSpotGrpByUType::GetThisElementView(const char* LocalName){
	Descr = "SpotGrpByUType(";
	if (0<=Nat&&Nat<8)	Descr += Nat;
	else				Descr += "NoNation";
	Descr += ",";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ",";
    if((UnitType!=NULL)&&(0<=Nat&&Nat<8))
		Descr += NATIONS[Nat].Mon[UnitType]->newMons->Name;
	else
		Descr += "NoType";

	Descr += ")";
	return Descr.str;
};

int				lvCSpotGrpByUType::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp==NULL||Nat<0||Nat>=8||UnitType==NULL)	return 0;

    OneObject*	pUnit = NULL;
	for (int i=0; i<MAXOBJECT; i++){
		pUnit = Group[i];
		if (pUnit&&!pUnit->Sdoxlo) {
			if (pUnit->NNUM==Nat&&pUnit->NIndex==UnitType) {
				pvGrp->AddUnitGPT(pUnit);	
			};
		};
	};
	return 1;
};

void			lvCSpotGrpByUType::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSpotGrpByUType(this));
};

// lvCRemoveNUnitsTo /////////////////////////////////////////////////////
lvCRemoveNUnitsTo::lvCRemoveNUnitsTo(lvCRemoveNUnitsTo* pRemoveNUnitsTo) : lvCOperation(dynamic_cast<lvCOperation*>(pRemoveNUnitsTo)) {
	if (pRemoveNUnitsTo!=NULL) {
		vGrpSource	= pRemoveNUnitsTo->vGrpSource;
		vGrpDest	= pRemoveNUnitsTo->vGrpDest;
		N			= pRemoveNUnitsTo->N;
	};
};

const	char*	lvCRemoveNUnitsTo::GetThisElementView(const char* LocalName){
	Descr = "RemoveNUnitsTo(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpSource);
	if (pvGrp!=NULL){
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += " -> ";
	pvGrp = GroupsMap()->GetGroupID(vGrpDest);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += ",";
	if (N<=0) {
		Descr += "All";
	}else{
		Descr += N;
	};
	Descr += " Units)";
	return Descr.str;
};

int				lvCRemoveNUnitsTo::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrpSource	= GroupsMap()->GetGroupID(vGrpSource);
	lvCGroup* pvGrpDest		= GroupsMap()->GetGroupID(vGrpDest);
	if (pvGrpSource==NULL)	return 0;
	if (N==0) {
		if (pvGrpDest==NULL) {
			pvGrpSource->RemAllUnits();
		}else{
			pvGrpSource->RemoveNUnitsToCGroup(pvGrpDest);
		};
	}else{
		if (pvGrpDest==NULL) {
			pvGrpSource->RemoveNUnitsToCGroup(NULL,N);
		}else{
			pvGrpSource->RemoveNUnitsToCGroup(pvGrpDest,N);
		};
	}
	return 1;
};

void			lvCRemoveNUnitsTo::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCRemoveNUnitsTo(this));
};

// lvCApplyTerafoming ////////////////////////////////////////////////////
lvCApplyTerafoming::lvCApplyTerafoming(lvCApplyTerafoming* pApplyTerafoming) : lvCOperation(dynamic_cast<lvCOperation*>(pApplyTerafoming)) {
	if (pApplyTerafoming!=NULL) {
		// ... 
	};
};
const	char*	lvCApplyTerafoming::GetThisElementView(const char* LocalName){
	Descr = "ApplyTerafoming(";
	Descr += ")";
	return Descr.str;
};
int				lvCApplyTerafoming::Process(int time){
	lvCOperation::Process(time);
	if (LastUpdateTime==0){
		LastUpdateTime=GetTickCount();
		return 1;
	};
	int PosN = TerraPointsArr.GetAmount();
    if (PosN<=0)	return 0;
	lvCTeraforming* pCurPos = GetPossition(0);
	if (PosN==1){
		if (pCurPos!=NULL) {
			pCurPos->Apply();
			TerraPointsArr.Clear();
			return 1;
		};
	}else if(PosN>1) {
		SetSpeed(Get_Speed());
		float	dt = (float)(GetTickCount()-LastUpdateTime);
		float MaxShift = Get_MaxShift(dt,0,1);
		if (Get_MaxDist()<(float)MinFromDest) {
            TerraPointsArr.DelElement(0);
			if (vNOfComplitePoints.Get()!=NULL) {
				vNOfComplitePoints.Get()->Value += 1;
			};
		}else if (MaxShift>=(float)MinStep) {
			lvCTeraforming* pDestPos = GetPossition(1);
			if (pDestPos!=NULL) {
				float maxR = max(pCurPos->r,pDestPos->r);
				x0 = (int)(min(pCurPos->PosXYZ.x,pDestPos->PosXYZ.x)-maxR-.5f)/64;
				y0 = (int)(min(pCurPos->PosXYZ.y,pDestPos->PosXYZ.y)-maxR-.5f)/64;
				x1 = (int)(max(pCurPos->PosXYZ.x,pDestPos->PosXYZ.x)+maxR+.5f)/64;
				y1 = (int)(max(pCurPos->PosXYZ.y,pDestPos->PosXYZ.y)+maxR+.5f)/64;
			};

			LastUpdateTime=GetTickCount();
			pCurPos->Add_X( xV*dt, MaxShift );
			pCurPos->Add_Y( yV*dt, MaxShift );
			pCurPos->Add_h( hV*dt, MaxShift );
			pCurPos->Add_H( HV*dt, MaxShift );
			pCurPos->Add_r( rV*dt, MaxShift );
			pCurPos->Add_R( RV*dt, MaxShift );
			pCurPos->Apply();

			BClrBar_InR(pCurPos->PosXYZ.x,pCurPos->PosXYZ.y,pCurPos->r);
			for(int i=0;i<NMFIELDS;i++){
				HashTable[i].ReCreateAreas(x0,y0,x1,y1);
			};
			x0=x1=y1=y0=0;
		};
		return 1;
	};

	return 0;
};
void			lvCApplyTerafoming::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCApplyTerafoming(this));
};
void			lvCApplyTerafoming::Draw(){
	if (visible==false)	return;
	int PosN = TerraPointsArr.GetAmount();
	Vector3D	p0(0.f,0.f,0.f),p1(0.f,0.f,0.f);
	lvCTeraforming*	pPos;
	while (PosN--) {
		pPos = TerraPointsArr[PosN]->Get();
		if (pPos!=NULL) {
//			pPos->visible=true;
			if (p0.x==0.f&&p0.y==0.f) {
				p0.set(pPos->PosXYZ.x,pPos->PosXYZ.y,pPos->PosXYZ.z+(float)pPos->R);
			}else{
				p1.set(pPos->PosXYZ.x,pPos->PosXYZ.y,pPos->PosXYZ.z+(float)pPos->R);
				GPS.DrawLine(p0,p1,color);
				p0.set(pPos->PosXYZ.x,pPos->PosXYZ.y,pPos->PosXYZ.z+(float)pPos->R);
			};
			void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
			void WorldToScreenSpace ( Vector4D& vec );

			Vector4D	p(p0.x,p0.y,p0.z,1);
			WorldToScreenSpace(p);
			char s[128];
			sprintf(s,"%s,%s,%d",pPos->GetName()," ID=",PosN);
			ShowStringEx(p.x,p.y,s,&SmallWhiteFont);
		};
	};
};
void			lvCApplyTerafoming::SetSpeed(int _speed){
	bool	recul=false;
	recul = (_speed!=Get_Speed())||(xV==0.f&&yV==0.f&&rV==0.f&&RV==0.f&&hV==0.f&&HV==0.f);
	if (recul) {
		Set_xV();
		Set_yV();
		Set_rV();
		Set_RV();
		Set_hV();
		Set_HV();
	};
};
lvCTeraforming*	lvCApplyTerafoming::GetPossition(int index){
	lvCTeraforming* pRet=NULL;
	if (TestIndexPos(index)){
		pRet = TerraPointsArr[index]->Get();
	};
	return pRet;
};
bool			lvCApplyTerafoming::TestIndexPos(int index){
	return (0<=index&&index<TerraPointsArr.GetAmount()&&TerraPointsArr[index]->Get()!=NULL);
};
float			lvCApplyTerafoming::Get_Speed(){
	float _speed=0;
	if (Use_VV&&vSpeed.Get()!=NULL) {
		_speed=(float)(vSpeed.Get()->Value)/10000.f;
	}else{
		_speed=(float)Speed/10000.f;
	};
	return _speed;
};
float			lvCApplyTerafoming::Get_DT(int p0,int p1){
	float DS = Get_DS(p0,p1);
	if (DS==0.f||Get_Speed()==0.f) return 0.f;
	return DS/Get_Speed();
};
float			lvCApplyTerafoming::Get_DS(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = abs( sqrt( (pPos0->PosXYZ.x-pPos1->PosXYZ.x)*(pPos0->PosXYZ.x-pPos1->PosXYZ.x)+
						  (pPos0->PosXYZ.y-pPos1->PosXYZ.y)*(pPos0->PosXYZ.y-pPos1->PosXYZ.y)	
					    )
				  );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_Dx(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = ( pPos1->PosXYZ.x - pPos0->PosXYZ.x );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_Dy(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = ( pPos1->PosXYZ.y - pPos0->PosXYZ.y );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_Dr(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = ( pPos1->r - pPos0->r );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_DR(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = ( pPos1->R - pPos0->R );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_Dh(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = ( pPos1->h - pPos0->h );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_DH(int p0,int p1){
	float dist=0.f;
	lvCTeraforming* pPos0 = GetPossition(p0);
	lvCTeraforming* pPos1 = GetPossition(p1);
	if (pPos0!=NULL&&pPos1!=NULL) {
		dist = ( pPos1->H - pPos0->H );
	};
	return dist;
};
float			lvCApplyTerafoming::Get_MaxShift(float dt,int p0,int p1){
	float _MaxShift=0.f;
	if (abs(xV*dt)>abs(_MaxShift))	_MaxShift = abs(xV*dt);
	if (abs(yV*dt)>abs(_MaxShift))	_MaxShift = abs(yV*dt);
	if (abs(rV*dt)>abs(_MaxShift))	_MaxShift = abs(rV*dt);
	if (abs(RV*dt)>abs(_MaxShift))	_MaxShift = abs(RV*dt);
	if (abs(hV*dt)>abs(_MaxShift))	_MaxShift = abs(hV*dt);
	if (abs(HV*dt)>abs(_MaxShift))	_MaxShift = abs(HV*dt);
	return _MaxShift;
};
float			lvCApplyTerafoming::Get_MaxDist(int p0,int p1){
	float _Max_Dist=0.f;
	if (abs(Get_Dx(p0,p1))>_Max_Dist)	_Max_Dist=abs(Get_Dx(p0,p1));
	if (abs(Get_Dy(p0,p1))>_Max_Dist)	_Max_Dist=abs(Get_Dy(p0,p1));
	if (abs(Get_Dr(p0,p1))>_Max_Dist)	_Max_Dist=abs(Get_Dr(p0,p1));
	if (abs(Get_DR(p0,p1))>_Max_Dist)	_Max_Dist=abs(Get_DR(p0,p1));
	if (abs(Get_Dh(p0,p1))>_Max_Dist)	_Max_Dist=abs(Get_Dh(p0,p1));
	if (abs(Get_DH(p0,p1))>_Max_Dist)	_Max_Dist=abs(Get_DH(p0,p1));
	return _Max_Dist;
};
void			lvCApplyTerafoming::Set_xV(int p0,int p1){
	float DX = Get_Dx(p0,p1);
	if (DX==0.f||Get_DT(p0,p1)==0.f) {
		xV=0.f;
		return;
	};
	xV = DX/Get_DT(p0,p1);
};
void			lvCApplyTerafoming::Set_yV(int p0,int p1){
	float DY = Get_Dy(p0,p1);
	if (DY==0.f||Get_DT(p0,p1)==0.f) {
		yV=0.f;
		return;
	};
	yV = DY/Get_DT(p0,p1);
};
void			lvCApplyTerafoming::Set_rV(int p0,int p1){
	float Dr = Get_Dr(p0,p1);
	if (Dr==0.f||Get_DT(p0,p1)==0.f) {
		rV=0.f;
		return;
	};
	rV = Dr/Get_DT(p0,p1);
};
void			lvCApplyTerafoming::Set_RV(int p0,int p1){
	float DR = Get_DR(p0,p1);
	if (DR==0.f||Get_DT(p0,p1)==0.f) {
		RV=0.f;
		return;
	};
	RV = DR/Get_DT(p0,p1);
};
void			lvCApplyTerafoming::Set_hV(int p0,int p1){
	float Dh = Get_Dh(p0,p1);
	if (Dh==0.f||Get_DT(p0,p1)==0.f) {
		hV=0.f;
		return;
	};
	hV = Dh/Get_DT(p0,p1);
};
void			lvCApplyTerafoming::Set_HV(int p0,int p1){
	float DH = Get_DH(p0,p1);
	if (DH==0.f||Get_DT(p0,p1)==0.f) {
		HV=0.f;
		return;
	};
	HV = DH/Get_DT(p0,p1);
};
// lvCSetMyNation ////////////////////////////////////////////////////////
lvCSetMyNation::lvCSetMyNation(lvCSetMyNation* pSetMyNation) : lvCOperation(dynamic_cast<lvCOperation*>(pSetMyNation)) {
	if (pSetMyNation!=NULL) {
		Nat = pSetMyNation->Nat;
	};
};
const	char*	lvCSetMyNation::GetThisElementView(const char* LocalName){
	Descr = "SetMyNation(";
	Descr += Nat;
	Descr += ")";
	return Descr.str;
};
DLLEXPORT	void	SetPlayerNation(int NI);
int				lvCSetMyNation::Process(int time){
	lvCOperation::Process(time);
	if (0<=Nat&&Nat<8)
		SetPlayerNation(Nat);
//		SetMyNation(Nat);
	return 1;
};
void			lvCSetMyNation::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetMyNation(this));
};

// lvCEqualizeSpeed //////////////////////////////////////////////////////
lvCEqualizeSpeed::lvCEqualizeSpeed(lvCEqualizeSpeed* pEqualizeSpeed) : lvCOperation(dynamic_cast<lvCOperation*>(pEqualizeSpeed)) {
	if (pEqualizeSpeed!=NULL) {
		vGrp0	= pEqualizeSpeed->vGrp0;
		vGrp1	= pEqualizeSpeed->vGrp1;
		EqType	= pEqualizeSpeed->EqType;
	};
};
const	char*	lvCEqualizeSpeed::GetThisElementView(const char* LocalName){
	Descr = "EqualizeSpeed(";
	Descr += ")";
	return Descr.str;
};
int				lvCEqualizeSpeed::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp0		= GroupsMap()->GetGroupID(vGrp0);
	lvCGroup* pvGrp1		= GroupsMap()->GetGroupID(vGrp1);
	if (pvGrp0==NULL&&pvGrp1==NULL)	return 0;
	if (pvGrp0!=NULL||pvGrp1!=NULL) {
		lvCGroup* pGrp=( (pvGrp0!=NULL) ? (pvGrp0) : (pvGrp1) );
		int speed=0;
		switch(EqType) {
		case 0:		// Min
			speed=pGrp->GetMinSpeed();
			break;
		case 1:		// Max
			speed=pGrp->GetMaxSpeed();
			break;
		case 2:		// Average
			speed=pGrp->GetAverageSpeed();
			break;
		};
		pGrp->SetSpeed(speed);
		if (pvGrp1)	pvGrp1->SetSpeed(speed);
	};
	return 1;
};
void			lvCEqualizeSpeed::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCEqualizeSpeed(this));
};
// lvCCreateBrigade //////////////////////////////////////////////////////
lvCCreateBrigade::lvCCreateBrigade(lvCCreateBrigade* pCreateBrigade) : lvCOperation(dynamic_cast<lvCOperation*>(pCreateBrigade)) {
	if (pCreateBrigade!=NULL) {
		vGrp = pCreateBrigade->vGrp;
	};
};
const	char*	lvCCreateBrigade::GetThisElementView(const char* LocalName){
	Descr = "CreateBrigade(";
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += ")";
	return Descr.str;
};
int		lvCCreateBrigade::GetSize(){
	int Size = -1;
    if (Use_VV&&vSize.Get()!=NULL) {
		Size=vSize.Get()->Value;
	}else{
		Size=iSize;
	}
	return Size;
};
int				lvCCreateBrigade::Process(int time){
	lvCOperation::Process(time);
	if (GetSize()==-1)	return 0;

	lvCGroup* pvGrp		= GroupsMap()->GetGroupID(vGrp);
	lvCGroup* pvBrigGrp	= GroupsMap()->GetGroupID(vBrigGrp);

	if (pvGrp->GetTotalAmount()<GetSize())	return 0;

	LinearArray<int,_int>	RealCandidate;

	OneObject* pOB=NULL;
	int N=pvGrp->GetTotalAmount();
	word NIIII = 0xFFFF;
	for (int i=0; i<N; i++){
		pOB=pvGrp->GetOneObj_lID(i);
		if ( pOB && pOB->BrigadeID==0xFFFF ){
			if (NIIII==0xFFFF) NIIII=pOB->NIndex;
			if (NIIII==pOB->NIndex) RealCandidate.Add(pOB->Index);
		};
	};
	
	if (RealCandidate.GetAmount()<GetSize())	return 0;

	LinearArray<int,_int>	NewBrigad;

	int NI = pvGrp->GetNation();
	if (0<=NI&&NI<8) {
		UnitsGroup* pUG = new UnitsGroup;
		OneObject* pOB=Group[ RealCandidate[0] ];
		int n=GetSize();
		int N=n;
		while (pOB==NULL&&n--) {
			pOB = Group[ RealCandidate[n] ];
		};
		if (pOB!=NULL) {
			word bNIndex=pOB->NIndex;
			pUG->AddNewUnit(pOB);
			pUG->IDS[0]=0xFFFF;
			for (int i=0; i<GetSize(); i++){
				pOB = Group[ RealCandidate[i] ];
				pUG->AddNewUnit(pOB);
				NewBrigad.Add(pOB->Index);
			};
			//////////////////////////////////////////////////////////////////////////
			int FormID=GetFormationIndexBySizeIndex(NI,bNIndex,0);
			int BID=CITY[NI].GetFreeBrigade();
			if(BID!=-1)
			{
				Brigade* BR=CITY[NI].Brigs+BID;
				BR->Enabled=1;
				BR->Direction=0;
				if(BR->CreateFromGroup(pUG,FormID,0))
				{				
					for(int i=0;i<N;i++){
						OneObject* OB=Group[pUG->IDS[i]];
						if(OB&&OB->Serial==pUG->SNS[i]&&!OB->Sdoxlo&&OB->InArmy){
							pUG->IDS[i]=0xFFFF;
						};
					};
					ext_OnBrigadeCreated(BR);
				};			
			};		
			//////////////////////////////////////////////////////////////////////////
		};
		if (pUG) delete pUG;
		if ( pvBrigGrp==NULL || vGrp==vBrigGrp ){
			// Do nothing
		}else{
			// Copy units to newBrig
			int NNN=NewBrigad.GetAmount();
			while (N--) {
				pvGrp->RemUnitGID(NewBrigad[N]);
				pvBrigGrp->AddUnitGID(NewBrigad[N]);
			};
		};
		return 1;
	};
	return 0;
};
void			lvCCreateBrigade::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCCreateBrigade(this));
};

// lvCAddWallSegment /////////////////////////////////////////////////////
lvCAddWallSegment::lvCAddWallSegment(lvCAddWallSegment* pAddWallSegment) : lvCOperation(dynamic_cast<lvCOperation*>(pAddWallSegment)) {
	if (pAddWallSegment!=NULL){
		Nat		= pAddWallSegment->Nat;
		Type	= pAddWallSegment->Type;
		if (pAddWallSegment->BegPos.Get()!=NULL)	BegPos.Set(pAddWallSegment->BegPos.Get());
		if (pAddWallSegment->EndPos.Get()!=NULL)	EndPos.Set(pAddWallSegment->EndPos.Get());
	};
};
const	char*	lvCAddWallSegment::GetThisElementView(const char* LocalName){
	Descr = "AddWalSegment(";
	if (0<=Nat&&Nat<8) {
		Descr += Nat;
		Descr += NATIONS[Nat].Mon[Type]->newMons->Name;
	}else{
		Descr += "NoNat,NoType,";
	};
	vvPOINT2D* pBegPos = BegPos.Get();
	vvPOINT2D* pEndPos = EndPos.Get();
	if (pBegPos!=NULL){
		Descr += pBegPos->GetName();
	}else{
		Descr += "NoBegPos";
	};
	Descr += ",";
	if (pEndPos!=NULL){
		Descr += pEndPos->GetName();
	}else{
		Descr += "NoEndPos";
	};
	Descr += ")";
	return	Descr.str;
};
int				lvCAddWallSegment::Process(int time){
	lvCOperation::Process(time);
	void AddWallSegment(byte NI,int Type,int x0,int y0,int x1,int y1);
	void AddCheapWallSegment(byte NI,int Type,int x0,int y0,int x1,int y1);
	vvPOINT2D* pBegPos = BegPos.Get();
	vvPOINT2D* pEndPos = EndPos.Get();
	if (Nat<0||Nat>=8||pBegPos==NULL||pEndPos==NULL)	return 0;
//	AddWallSegment(Nat,Type,pBegPos->Value.x,pBegPos->Value.y,pEndPos->Value.x,pEndPos->Value.y);
	AddCheapWallSegment(Nat,Type,pBegPos->Value.x,pBegPos->Value.y,pEndPos->Value.x,pEndPos->Value.y);
	return 1;
};
void			lvCAddWallSegment::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddWallSegment(this));
};
// lvCAddFarms ///////////////////////////////////////////////////////////
lvCAddFarms::lvCAddFarms(lvCAddFarms* pAddFarms) : lvCOperation(dynamic_cast<lvCOperation*>(pAddFarms)) {
	if (pAddFarms!=NULL) {
		Nat				= pAddFarms->Nat;
		addFarms		= pAddFarms->addFarms;
	};
};
const	char*	lvCAddFarms::GetThisElementView(const char* LocalName){
	Descr = "AddFarmsForUnit(Nation[";
	Descr += Nat;
	Descr += "],Add(";
	Descr += addFarms;
	Descr += "),Set(";
	if (setFarms==-1)	Descr += "Leave Current";
	else				Descr += setFarms;
	Descr += ")";
	return Descr.str;
};
int				lvCAddFarms::Process(int time){
	lvCOperation::Process(time);
	if (0<=Nat&&Nat<8) {
		if (setFarms!=-1)	NATIONS[Nat].AddFarms=setFarms;
		NATIONS[Nat].AddFarms += addFarms;
		return 1;
	};
	return 0;
};
void			lvCAddFarms::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddFarms(this));
};
// lvCSetSerchWFlag //////////////////////////////////////////////////////
lvCSetSerchWFlag::lvCSetSerchWFlag(lvCSetSerchWFlag* pSetSerchWFlag) : lvCOperation(dynamic_cast<lvCOperation*>(pSetSerchWFlag)) {
	if (pSetSerchWFlag!=NULL) {
		vGrp = pSetSerchWFlag->vGrp;
		SearchWict = pSetSerchWFlag->SearchWict;
	};
};
const	char*	lvCSetSerchWFlag::GetThisElementView(const char* LocalName){
	Descr = "SearchWiktem(";
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGrp";
	};
	Descr += ", Search = ";
    Descr += SearchWict;
	Descr += ")";
	return Descr.str;
};
int				lvCSetSerchWFlag::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL) {
		int N = pvGrp->GetTotalAmount();
		OneObject* pOB=NULL;
		while (N--) {
			pOB=pvGrp->GetOneObj_lID(N);
			if (pOB!=NULL) {
				pOB->NoSearchVictim=!SearchWict;
			};
			pOB=NULL;
		};
		return 1;
	};
	return 0;
};
void			lvCSetSerchWFlag::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetSerchWFlag(this));
};

// lvCClearOrders ////////////////////////////////////////////////////////
lvCClearOrders::lvCClearOrders(lvCClearOrders* pClearOrders) : lvCOperation(dynamic_cast<lvCOperation*>(pClearOrders)) {
	if (pClearOrders!=NULL) {
		vGrp = pClearOrders->vGrp;
	};
};
const	char*	lvCClearOrders::GetThisElementView(const char* LocalName){
	Descr = "ClearOrders(";
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL&&pvGrp->NAME.str)	Descr += pvGrp->NAME.str;
	else								Descr += "NoGroup";
	Descr += ")";
	return Descr.str;
};
int				lvCClearOrders::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL) {
		pvGrp->ClearOrders();
		return 1;
	};
	return 0;
};
void			lvCClearOrders::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCClearOrders(this));
};
// lvCClearOrders ////////////////////////////////////////////////////////
lvCClearDead::lvCClearDead(lvCClearDead* pClearDead) : lvCOperation(dynamic_cast<lvCOperation*>(pClearDead)) {
	if (pClearDead!=NULL) {
	};
};
const	char*	lvCClearDead::GetThisElementView(const char* LocalName){
	Descr = "ClearDeadUnits(";
	Descr += ")";
	return Descr.str;
};
int				lvCClearDead::Process(int time){
	lvCOperation::Process(time);
	int N=MAXOBJECT;
	while (N--) {
		OneObject* OB = Group[N];
		if(OB&&OB->Sdoxlo>0&&!OB->NewBuilding){
			OB->Sdoxlo=1000;
		}
	};
	return 0;
};
void			lvCClearDead::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCClearDead(this));
};
// lvCGroupMovement //////////////////////////////////////////////////////
lvCGroupMovement::lvCGroupMovement(lvCGroupMovement* pGroupMovement): lvCOperation(dynamic_cast<lvCOperation*>(pGroupMovement)){
	if (pGroupMovement!=NULL) {
		MovementState	= pGroupMovement->MovementState;
		vGrp			= pGroupMovement->vGrp;
	};	
};
const	char*	lvCGroupMovement::GetThisElementView(const char* LocalName){
	Descr = "GroupMovement(GRP[";
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL&&pvGrp->NAME.str!=NULL) {	
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoName";
	};
	Descr += "],";
	if (MovementState==0) {	// ADD
		Descr += "Add To Donot Move"; 
	}else if (MovementState==1) {	// DELETE
		Descr += "Rem From Donot Move"; 
	}else{
		Descr += "NoParam!!!";
	};
	Descr += ")";
	return Descr.str;
};
int				lvCGroupMovement::Process(int time){
	lvCOperation::Process(time);

	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL) {
		if (MovementState==0) {	// ADD
			GroupsMap()->__AddDonMoveGrp(pvGrp);
		}else if (MovementState==0) {	// DELETE
			GroupsMap()->__RemDonMoveGrp(pvGrp);
		}else{
			return 0;
		};
	};
	return 1;
};
void			lvCGroupMovement::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCGroupMovement(this));
};
// lvCSetTired ///////////////////////////////////////////////////////////
lvCSetTired::lvCSetTired(lvCSetTired* pSetTired) :lvCOperation(dynamic_cast<lvCOperation*>(pSetTired)) {
	if (pSetTired!=NULL) {
		vGrp			= pSetTired->vGrp;
		SetedTiredValue	= pSetTired->SetedTiredValue;
	};
};
const	char*	lvCSetTired::GetThisElementView(const char* LocalName){
	Descr = "SetTired(Group[";
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL)	Descr += pvGrp->NAME.str;
	else				Descr += "NoGroup";
	Descr += ",Tired[";
	Descr += SetedTiredValue;
	Descr += "])";
	return Descr.str;
};
int				lvCSetTired::Process(int time){
	lvCOperation::Process(time);

	if (SetedTiredValue<0)	return 0;
	lvCGroup* pvGrp	= GroupsMap()->GetGroupID(vGrp);
	if (pvGrp==NULL) return 0;
	int N=pvGrp->GetTotalAmount();
	OneObject* pOB=NULL;
	while (N--) {
		pOB = pvGrp->GetOneObj_lID(N);
		if (pOB!=NULL)	pOB->GetTired=SetedTiredValue;
		pOB=NULL;
	};
	return 1;
};
void			lvCSetTired::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetTired(this));
};

int	SetTired_lua(lvCGroup* pGRP,int VAL){
	if (VAL<0)	return 0;
	if (pGRP==NULL) return 0;
	int N=pGRP->GetTotalAmount();
	OneObject* pOB=NULL;
	while (N--) {
		pOB = pGRP->GetOneObj_lID(N);
		if (pOB!=NULL)	pOB->GetTired=VAL;
		pOB=NULL;
	};
	return 1;
};
//======================================================================//
//=================	 OPERATION FOR TRANSPORT	 =======================//
//======================================================================//
// lvCPushUnitAway ///////////////////////////////////////////////////////
lvCPushNUnitAway::lvCPushNUnitAway(lvCPushNUnitAway* pPushNUnitAway) : lvCOperation(dynamic_cast<lvCOperation*>(pPushNUnitAway)) {
	if (pPushNUnitAway!=NULL) {
		N = pPushNUnitAway->N;
		vGrpID = pPushNUnitAway->vGrpID;
	};
};
const	char*	lvCPushNUnitAway::GetThisElementView(const char* LocalName){
	if (N==0)	Descr = "PushAllUnitAway(";
	else		Descr = "PushNUnitAway(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL)	Descr += pvGrp->NAME.str;
	else				Descr += "NoGrp";
	if (N!=0) {
		Descr += ",";
		Descr += N;
	};
	Descr += ")";
	return Descr.str;
};
int				lvCPushNUnitAway::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) {
		pvGrp->PushNUnitAway(N);
		return 1;
	};
	return 0;
};
void			lvCPushNUnitAway::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCPushNUnitAway(this));
};

// lvCSendUnitsToTransport ///////////////////////////////////////////////
lvCSendUnitsToTransport::lvCSendUnitsToTransport(lvCSendUnitsToTransport* pSendUnitsToTransport) : lvCOperation(dynamic_cast<lvCOperation*>(pSendUnitsToTransport)) {
	if (pSendUnitsToTransport!=NULL) {
		vGrpTransID = pSendUnitsToTransport->vGrpTransID;
		vGrpUnitsID = pSendUnitsToTransport->vGrpUnitsID;
	};
};
const	char*	lvCSendUnitsToTransport::GetThisElementView(const char* LocalName){
	Descr = "SendUnitsToTransport(";
    lvCGroup* pvGrpT = GroupsMap()->GetGroupID(vGrpTransID);
	lvCGroup* pvGrpU = GroupsMap()->GetGroupID(vGrpUnitsID);
	if (pvGrpU!=NULL)	Descr += pvGrpU->NAME.str;
	else				Descr += "NoUnits";
	Descr += " -> ";
	if (pvGrpT)			Descr += pvGrpT->NAME.str;
	else				Descr += "NoTransport";
	Descr += ")";
	return Descr.str;
};
int				lvCSendUnitsToTransport::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGrpT = GroupsMap()->GetGroupID(vGrpTransID);
	lvCGroup* pvGrpU = GroupsMap()->GetGroupID(vGrpUnitsID);

	if (pvGrpT==NULL||pvGrpU==NULL)	return 0;

	int NT = pvGrpT->GetTotalAmount();
	int NU = pvGrpU->GetTotalAmount();

	if (NT==0||NU==0)	return 0;

	OneObject* pOBT=NULL;
	OneObject* pOBU=NULL;

	int* pOBArrU = new int[NU];
	for (int i=0; i<NU; i++){
		pOBArrU[i]=(int)(pvGrpU->GetOneObj_lID(i));
	};
	int key=0;
	for (int i=1; i<NU; i++){
		if (pOBArrU[i]!=0){
			key=i;
			while ( key>0 && ((OneObject*)pOBArrU[key])->newMons->NPlacesInTransport > ((OneObject*)pOBArrU[key-1])->newMons->NPlacesInTransport ) {
				int pTemp = pOBArrU[key];
				pOBArrU[key]=pOBArrU[key-1];
				pOBArrU[key-1]=pTemp;
				key--;
			}
		};
	};

	int CurTransID=0;
	for (int i=0; i<NU; i++){
		if (pOBArrU[i]!=0) {
			bool stop=false;
			int  iter=0;
			do {
				pOBT=pvGrpT->GetOneObj_lID(CurTransID);
				if (pOBT!=NULL && ((OneObject*)pOBArrU[i])->GoToMine(pOBT->Index,128+16,0)) {
					stop=true;
				}else{
					CurTransID = (CurTransID+1)%NT;
				};
				iter++;
            } while(!stop&&iter<NT);			
		};
	};

	delete[]pOBArrU;
	pOBArrU=NULL;

	return 1;
};
void			lvCSendUnitsToTransport::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSendUnitsToTransport(this));
};
//////////////////////////////////////////////////////////////////////////

//======================================================================//
//=================		OPERATION FOR FILM		========================//
//======================================================================//
// lvCSetFGV /////////////////////////////////////////////////////////////
lvCSetFGV::lvCSetFGV(lvCSetFGV* pSetFGV) : lvCOperation(dynamic_cast<lvCOperation*>(pSetFGV)) {
	if (pSetFGV!=NULL) {
		if (pSetFGV->GraphObj.Get()!=NULL) GraphObj.Set(pSetFGV->GraphObj.Get());
		State		= pSetFGV->State;
		Immediately	= pSetFGV->Immediately;
	};
};

void			lvCSetFGV::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetFGV(this));
};

const	char*	lvCSetFGV::GetThisElementView(const char* LocalName){
	lvCGraphObject* pObj = dynamic_cast<lvCGraphObject*>(GraphObj.Get());
	vvBASE*		pnObj = dynamic_cast<vvBASE*>(newGraphObj.Get());
	if (pObj!=NULL||pnObj!=NULL){
		Descr = "";
		if (pObj!=NULL)			Descr += pObj->Name.str;
		else if (pnObj!=NULL)	Descr += pnObj->Name.str;
		else					Descr += "NoObject";
		Descr += "->";
		if (State)			Descr += "true";
		else				Descr += "false";
		Descr += "[";
		if (Immediately)	Descr += "true";
		else				Descr += "false";
		Descr += "]";
	}else{
		Descr = "NoGraph";
	};
	return Descr.str;
};
int				lvCSetFGV::Process(int time){
	lvCOperation::Process(time);
	lvCGraphObject* pObj = dynamic_cast<lvCGraphObject*>(GraphObj.Get());
	vvBASE*		pnObj = dynamic_cast<vvBASE*>(newGraphObj.Get());
	if (pObj!=NULL){
		if (Immediately){
			pObj->SetVisible(State);
		}else{
			if (State)	pObj->StartShow();
			else		pObj->StopShow();
		};
	};
	if (pnObj!=NULL) pnObj->visible=State;
	return 1;
};
// lvCPlayOGMiss /////////////////////////////////////////////////////////
lvCPlayOGMiss::lvCPlayOGMiss(lvCPlayOGMiss* pPlayOGMiss) : lvCOperation(dynamic_cast<lvCOperation*>(pPlayOGMiss)) {
	if (pPlayOGMiss!=NULL) {
		pFileName	= pPlayOGMiss->pFileName.str;
		pStream		= pPlayOGMiss->pStream;
		pCyclic		= pPlayOGMiss->pCyclic;
	};
};

void			lvCPlayOGMiss::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCPlayOGMiss(this));
};

const	char*	lvCPlayOGMiss::GetThisElementView(const char* LocalName){
		Descr = "";
	if (pFileName.str!=NULL){
		Descr += "Play OG Mission(";
		Descr += pFileName.str;
		Descr += ", ";
		Descr += pStream;
		Descr += ")";
	}else{
		Descr = "PlayOGMiss(NULL)";
	};
	return Descr.str;
};
int				lvCPlayOGMiss::Process(int time){
	lvCOperation::Process(time);
//	if (first) {
		ov_Play(pFileName.str,pStream);
		//void ov_Cyclic(BOOL bCyclic,DWORD dwStream);
		if (pCyclic) ov_Cyclic(TRUE,pStream);
		first=false;
		return 1;
//	}
	return 0;
};
// lvCStopOGMiss /////////////////////////////////////////////////////////
lvCStopOGMiss::lvCStopOGMiss(lvCStopOGMiss* pStopOGMiss) : lvCOperation(dynamic_cast<lvCOperation*>(pStopOGMiss)) {
	if (pStopOGMiss!=NULL) {
		pStream = pStopOGMiss->pStream;
	};
};

void			lvCStopOGMiss::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCStopOGMiss(this));
};

const	char*	lvCStopOGMiss::GetThisElementView(const char* LocalName){
	Descr = "";
	Descr += "Stop OG Mission(";
	Descr += pStream;
	Descr += ")";
	return Descr.str;
};
int				lvCStopOGMiss::Process(int time){
	lvCOperation::Process(time);
	ov_Stop(pStream);
	return 1;
};
// lvCOGSetVolume ////////////////////////////////////////////////////////
lvCOGSetVolume::lvCOGSetVolume(lvCOGSetVolume* pOGSetVolume) : lvCOperation(dynamic_cast<lvCOperation*>(pOGSetVolume)) {
	if (pOGSetVolume!=NULL) {
		Volume	= pOGSetVolume->Volume;
		pStream = pOGSetVolume->pStream;
	};
};

void			lvCOGSetVolume::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCOGSetVolume(this));
};

const	char*	lvCOGSetVolume::GetThisElementView(const char* LocalName){
	Descr = "";
	Descr += "OG Set Volume(";
	Descr += Volume;
	Descr += ", ";
	Descr += pStream;
	Descr += ")";
	return Descr.str;
};

int				lvCOGSetVolume::Process(int time){
	lvCOperation::Process(time);
	ov_SetVolume(Volume,pStream);
	return 1;
};
// lvCOGFinishMiss ///////////////////////////////////////////////////////
lvCOGFinishMiss::lvCOGFinishMiss(lvCOGFinishMiss* pOGFinishMiss) : lvCOperation(dynamic_cast<lvCOperation*>(pOGFinishMiss)) {
	if (pOGFinishMiss!=NULL) {
		pStream = pOGFinishMiss->pStream;
	};
};

void			lvCOGFinishMiss::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCOGFinishMiss(this));
};

const	char*	lvCOGFinishMiss::GetThisElementView(const char* LocalName){
	Descr = "";
	Descr += "OG Finish Mission(";
	Descr += pStream;
	Descr += ")";
	return Descr.str;
};
int				lvCOGFinishMiss::Process(int time){
	lvCOperation::Process(time);
	return (ov_StreamFinished(pStream)!=0);
};
// lvCFreezeGame /////////////////////////////////////////////////////////
lvCFreezeGame::lvCFreezeGame(lvCFreezeGame* pFreezeGame) : lvCOperation(dynamic_cast<lvCOperation*>(pFreezeGame)) {
	if (pFreezeGame!=NULL) {

	};
};

void			lvCFreezeGame::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCFreezeGame(this));
};

const char*		lvCFreezeGame::GetThisElementView(const char* LocalName){
	Descr="Freeze Game ()";
	return Descr.str;
};
int				lvCFreezeGame::Process(int time){
	lvCOperation::Process(time);
	FreezeGame();
	SetFreezeDipSysProcess(true);
	return 0;
};
// lvCUnFreezeGame ///////////////////////////////////////////////////////
lvCUnFreezeGame::lvCUnFreezeGame(lvCUnFreezeGame* pUnFreezeGame) : lvCOperation(dynamic_cast<lvCOperation*>(pUnFreezeGame)) {
	if (pUnFreezeGame!=NULL) {

	};
};

void			lvCUnFreezeGame::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCUnFreezeGame(this));
};

const char*		lvCUnFreezeGame::GetThisElementView(const char* LocalName){
	Descr="UnFreeze Game ()";
	return Descr.str;
};
int				lvCUnFreezeGame::Process(int time){
	lvCOperation::Process(time);
	UnFreezeGame();
	SetFreezeDipSysProcess(false);
	return 0;
};
// lvCUnFreezeGroup //////////////////////////////////////////////////////
lvCUnFreezeGroup::lvCUnFreezeGroup(lvCUnFreezeGroup* pUnFreezeGroup) : lvCOperation(dynamic_cast<lvCOperation*>(pUnFreezeGroup)) {
	if (pUnFreezeGroup!=NULL) {
		GrpID = pUnFreezeGroup->GrpID;
	};
};

void			lvCUnFreezeGroup::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCUnFreezeGroup(this));
};

const char*		lvCUnFreezeGroup::GetThisElementView(const char* LocalName){
	Descr="UnFreeze Group(";
	if (use_vGroup){
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			Descr += pvGRP->GetGroupName();
		};
	};
	Descr+=" ) ";
	return Descr.str;
};
int				lvCUnFreezeGroup::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			int NU=pvGRP->GetTotalAmount();
			OneObject*	pUnit = NULL;
			for (int i=0; i<NU; i++){
				pUnit = pvGRP->GetOneObj_lID(i);
				if (pUnit) {
					//pUnit->Invisible = false;
					SetInvisiblen(pUnit,false);
					//pUnit->Illusion = false;
					SetIllusion(pUnit,false);
				};
			};
		};
	};
	return 0;
};
// lvCFreezeAndHidden ////////////////////////////////////////////////////
lvCFreezeAndHidden::lvCFreezeAndHidden(lvCFreezeAndHidden* pFreezeAndHidden) : lvCOperation(dynamic_cast<lvCOperation*>(pFreezeAndHidden)) {
	if (pFreezeAndHidden!=NULL) {
		bHome = pFreezeAndHidden->bHome;
	};
};

void			lvCFreezeAndHidden::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCFreezeAndHidden(this));
};

const char*		lvCFreezeAndHidden::GetThisElementView(const char* LocalName){
	Descr="Freeze and Hidden(";
	if (bHome) {
		Descr+= "True";
	}else{
		Descr+= "False";
	};
	Descr+=")";
	return Descr.str;
};
int				lvCFreezeAndHidden::Process(int time){
	lvCOperation::Process(time);
	OneObject*	pUnit = NULL;
	for (int i=0; i<MAXOBJECT; i++){
		pUnit = Group[i];
		if (pUnit) {
			if (pUnit->NewBuilding==false) {
			//	pUnit->Invisible = true;
				SetInvisiblen(pUnit,true);
			}else if (bHome) {
			//	pUnit->Invisible = true;
				SetInvisiblen(pUnit,true);
			}

			//pUnit->Illusion = true;
			SetIllusion(pUnit,true);
		}
	}
	SetFreezeDipSysProcess(true);
	return 0;
};
// lvCUnFreezeAndUnHidden ////////////////////////////////////////////////
lvCUnFreezeAndUnHidden::lvCUnFreezeAndUnHidden(lvCUnFreezeAndUnHidden* pUnFreezeAndUnHidden) : lvCOperation(dynamic_cast<lvCOperation*>(pUnFreezeAndUnHidden)) {
	if (pUnFreezeAndUnHidden!=NULL) {

	};
};

void			lvCUnFreezeAndUnHidden::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCUnFreezeAndUnHidden(this));
};

const char*		lvCUnFreezeAndUnHidden::GetThisElementView(const char* LocalName){
	Descr="UnFreeze and UnHidden ()";
	return Descr.str;
};
int				lvCUnFreezeAndUnHidden::Process(int time){
	lvCOperation::Process(time);
	OneObject*	pUnit = NULL;
	for (int i=0; i<MAXOBJECT; i++){
		pUnit = Group[i];
		if (pUnit) {
			//pUnit->Invisible = false;
			SetInvisiblen(pUnit,false);
			//pUnit->Illusion = false;
			SetIllusion(pUnit,false);
		}
	}
	SetFreezeDipSysProcess(false);
	return 0;
};
// lvCFreezeAndHiddenGame ////////////////////////////////////////////////
lvCFreezeAndHiddenGame::lvCFreezeAndHiddenGame(lvCFreezeAndHiddenGame* pFreezeAndHiddenGame) : lvCOperation(dynamic_cast<lvCOperation*>(pFreezeAndHiddenGame)) {
	if (pFreezeAndHiddenGame!=NULL) {
		pZone	= pFreezeAndHiddenGame->pZone;
		bHome	= pFreezeAndHiddenGame->bHome;
	};
};

void			lvCFreezeAndHiddenGame::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCFreezeAndHiddenGame(this));
};

const char*		lvCFreezeAndHiddenGame::GetThisElementView(const char* LocalName){
	Descr="Freeze and Hidden Game(";
	if ((0<AZones.GetAmount()&&pZone<AZones.GetAmount())&&(AZones[pZone]!=NULL)) {
		Descr+=AZones[pZone]->Name.str;
		Descr+=", ";
	};
	if (bHome) {
		Descr+= "True";
	}else{
		Descr+= "False";
	};
	Descr+=")";
	return Descr.str;
};
int				lvCFreezeAndHiddenGame::Process(int time){
	lvCOperation::Process(time);
	int	zR = -1;
	int x  = -1;
	int y  = -1;
	if (UseNode==true){
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL){
			x  = pNode->vGetX();
			y  = pNode->vGetY();
			zR = pNode->vGetR();
		};
	}else{
		if ((0<AZones.GetAmount()&&pZone<AZones.GetAmount())&&(AZones[pZone]!=NULL)) {
			ActiveZone* AZ=AZones[pZone];
			zR=AZ->R;
			x=AZ->x;
			y=AZ->y;
		};
	};
	
	if (x!=-1&&y!=-1&&zR!=-1){
		lvSSumb	Ret;	Ret.Sum = 0;	Ret.bHome = bHome;
		PerformActionOverUnitsInRadius(	x,y,zR,AddUnitToSumHide,&Ret);
	}
	
	return 1;
};
// lvCUnFreezeAndUnHiddenGame ////////////////////////////////////////////
lvCUnFreezeAndUnHiddenGame::lvCUnFreezeAndUnHiddenGame(lvCUnFreezeAndUnHiddenGame* pUnFreezeAndUnHiddenGame) : lvCOperation(dynamic_cast<lvCOperation*>(pUnFreezeAndUnHiddenGame)) {
	if (pUnFreezeAndUnHiddenGame!=NULL) {
		pZone = pUnFreezeAndUnHiddenGame->pZone;
	};
};

void			lvCUnFreezeAndUnHiddenGame::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCUnFreezeAndUnHiddenGame(this));
};

const char*		lvCUnFreezeAndUnHiddenGame::GetThisElementView(const char* LocalName){
	Descr="UnFreeze and UnHidden Game(";
	if ((0<AZones.GetAmount()&&pZone<AZones.GetAmount())&&(AZones[pZone]!=NULL)) {
		Descr+=AZones[pZone]->Name.str;
	};
	Descr+=")";
	return Descr.str;
};
int				lvCUnFreezeAndUnHiddenGame::Process(int time){
	lvCOperation::Process(time);
	if ((0<AZones.GetAmount()&&pZone<AZones.GetAmount())&&(AZones[pZone]!=NULL)) {
		ActiveZone* AZ=AZones[pZone];
		int	zR = 0;
		zR=AZ->R;
		int x,y;
		x=AZ->x;
		y=AZ->y;
		lvSSum	Ret;	Ret.Sum = 0;
		PerformActionOverUnitsInRadius(	x,y,zR,
										AddUnitToSumUnHide,
										&Ret);
	};
	return 0;
};
// lvCUnFreezeAndUnHiddenGroup ///////////////////////////////////////////
lvCUnFreezeAndUnHiddenGroup::lvCUnFreezeAndUnHiddenGroup(lvCUnFreezeAndUnHiddenGroup* pUnFreezeAndUnHiddenGroup) : lvCOperation(dynamic_cast<lvCOperation*>(pUnFreezeAndUnHiddenGroup)) {
	if (pUnFreezeAndUnHiddenGroup!=NULL) {
		GrpID = pUnFreezeAndUnHiddenGroup->GrpID;
	};
};

void			lvCUnFreezeAndUnHiddenGroup::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCUnFreezeAndUnHiddenGroup(this));
};

const char*		lvCUnFreezeAndUnHiddenGroup::GetThisElementView(const char* LocalName){
	Descr="UnFreeze and UnHidden Group(";
	if (use_vGroup){
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			Descr += pvGRP->GetGroupName();
		};
	}else{
		Descr+="NULL group";
	};
	Descr+=")";
	return Descr.str;
};
int				lvCUnFreezeAndUnHiddenGroup::Process(int time){
	lvCOperation::Process(time);
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(GrpID);
		if (pvGRP) {
			int NU=pvGRP->GetTotalAmount();
			OneObject*	pUnit = NULL;
			for (int i=0; i<NU; i++){
				pUnit = pvGRP->GetOneObj_lID(i);
				if (pUnit) {
					//pUnit->Invisible = false;
					SetInvisiblen(pUnit,false);
					//pUnit->Illusion = false;
					SetIllusion(pUnit,false);
				};
			};
		};
	};
	return 0;
};
// lvCSetLeftPort ////////////////////////////////////////////////////////
lvCSetLeftPort::lvCSetLeftPort(lvCSetLeftPort* pSetLeftPort) : lvCOperation(dynamic_cast<lvCOperation*>(pSetLeftPort)) {
	if (pSetLeftPort!=NULL) {
		if (pSetLeftPort->MENU.Get()!=NULL)	MENU.Set(pSetLeftPort->MENU.Get());
		FileID		= pSetLeftPort->FileID;	
		SpriteID	= pSetLeftPort->SpriteID;
	};
};

void			lvCSetLeftPort::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetLeftPort(this));
};

const	char*	lvCSetLeftPort::GetThisElementView(const char* LocalName){
	Descr = "LeftMorda[";
	Descr += SpriteID;
	Descr += "]";
	return Descr.str;
};

int				lvCSetLeftPort::Process(int time){
	lvCOperation::Process(time);
	lvCDeffFilmMenu* pMenu = MENU.Get();
	if (pMenu!=NULL) {
		if (FileID!=0xFFFF) {
			pMenu->LEFT_MORDA.FileID = FileID;
			pMenu->SetLeftMorda(SpriteID);

			// Set hero name for menu
			if (pMenu->CII_HeroName_style){
				vvTEXT* pName = HName.Get();
				if (pName!=NULL&&pName->SpeakerID.str!=NULL) {
					pMenu->LeftName.Set(pName);
				};
			};	

			return 1;
		};
	};
	return 0;
};
// lvCSetRightPort ///////////////////////////////////////////////////////
lvCSetRightPort::lvCSetRightPort(lvCSetRightPort* pSetRightPort) : lvCOperation(dynamic_cast<lvCOperation*>(pSetRightPort)) {
	if (pSetRightPort!=NULL) {
		if (pSetRightPort->MENU.Get()!=NULL) MENU.Set(pSetRightPort->MENU.Get());
		FileID		= pSetRightPort->FileID;
		SpriteID	= pSetRightPort->SpriteID;
	};
};

void			lvCSetRightPort::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetRightPort(this));
};

const	char*	lvCSetRightPort::GetThisElementView(const char* LocalName){
	Descr = "RightMorda[";
	Descr += SpriteID;
	Descr += "]";
	return Descr.str;
};

int				lvCSetRightPort::Process(int time){
	lvCOperation::Process(time);
	lvCDeffFilmMenu* pMenu = MENU.Get();
	if (pMenu!=NULL) {
		if (FileID!=0xFFFF) {
			pMenu->RIGHT_MORDA.FileID = FileID;
			pMenu->SetRightMorda(SpriteID);

			// Set hero name for menu
			if (pMenu->CII_HeroName_style){
				vvTEXT* pName = HName.Get();
				if (pName!=NULL&&pName->SpeakerID.str!=NULL) {
					pMenu->RightName.Set(pName);
				};
			};	

			return 1;
		};
	};
	return 0;
};
// lvCPlayText ///////////////////////////////////////////////////////////
lvCPlayText::lvCPlayText(lvCPlayText* pPlayText) : lvCOperation(dynamic_cast<lvCOperation*>(pPlayText)) {
	if (pPlayText!=NULL) {
		if (pPlayText->MENU.Get()!=NULL) MENU.Set(pPlayText->MENU.Get());
		canal	= pPlayText->canal;
	};
};

void			lvCPlayText::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCPlayText(this));
};

const	char*	lvCPlayText::GetThisElementView(const char* LocalName){
	Descr = "Play[";
	if (MENU.Get()!=NULL){
		Descr += MENU.Get()->Name.str;
	};
	Descr += "]";
	return Descr.str;
};

int				lvCPlayText::Process(int time){
	lvCOperation::Process(time);
	if (MENU.Get()!=NULL){
		MENU.Get()->ShowText(true);
		MENU.Get()->PlayText(canal);
		return 1;
	};
	return 0;
};
// lvCSetText ////////////////////////////////////////////////////////////
lvCSetText::lvCSetText(lvCSetText* pSetText) : lvCOperation(dynamic_cast<lvCOperation*>(pSetText)) {
	if (pSetText!=NULL) {
		if (pSetText->MENU.Get()!=NULL) MENU.Set(pSetText->MENU.Get());
		if (pSetText->TEXT.Get()!=NULL) TEXT.Set(pSetText->TEXT.Get());
	}
};

void			lvCSetText::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetText(this));
};

const	char*	lvCSetText::GetThisElementView(const char* LocalName){
	Descr = "SetText[";
	if (MENU.Get()!=NULL){
		Descr += MENU.Get()->Name.str;
		Descr += "->";
		if ( TEXT.Get() != NULL ){
			Descr += TEXT.Get()->TextID.str;
		};
	};
	Descr += "]";
	return Descr.str;
};

int				lvCSetText::Process(int time){
	lvCOperation::Process(time);
	if (MENU.Get()!=NULL&&TEXT.Get()!=NULL){
		MENU.Get()->SetText(TEXT.Get());
		return 1;
	};
	return 0;
};
// lvCSetActivFrame //////////////////////////////////////////////////////
lvCSetActivFrame::lvCSetActivFrame(lvCSetActivFrame* pSetActivFrame) : lvCOperation(dynamic_cast<lvCOperation*>(pSetActivFrame)) {
	if (pSetActivFrame!=NULL) {
		if (pSetActivFrame->MENU.Get()!=NULL) MENU.Set(pSetActivFrame->MENU.Get());
		STATE	= pSetActivFrame->STATE;
	}
};

void			lvCSetActivFrame::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetActivFrame(this));
};

const	char*	lvCSetActivFrame::GetThisElementView(const char* LocalName){
	Descr = "ActiveFrame[";
	Descr += STATE;
	Descr += "]";
	return Descr.str;
};

int				lvCSetActivFrame::Process(int time){
	lvCOperation::Process(time);
	if (MENU.Get()!=NULL){
		MENU.Get()->SetFarmeState(STATE);
		return 1;
	};
	return 0;
};
// lvCRunTimer ///////////////////////////////////////////////////////////
DLLEXPORT	void RunTimer(byte ID, int Long, bool trueTime);
lvCRunTimer::lvCRunTimer(lvCRunTimer* pRunTimer) : lvCOperation(dynamic_cast<lvCOperation*>(pRunTimer)) {
	if (pRunTimer!=NULL) {
		if (pRunTimer->TimerID.Get()!=NULL) TimerID.Set(pRunTimer->TimerID.Get());
		Time	= pRunTimer->Time;
		UseVV	= pRunTimer->UseVV;
		if (pRunTimer->VVpInt.Get()!=NULL) VVpInt.Set(pRunTimer->VVpInt.Get());
	};
};

void			lvCRunTimer::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCRunTimer(this));
};

const	char*	lvCRunTimer::GetThisElementView(const char* LocalName){
	Descr = "RunTimer[";
	if (TimerID.Get()!=NULL){
		Descr += "ID(";
		Descr += TimerID.Get()->Name.str;
		Descr += ") ";
		if (UseVV) {
			vvINTEGER* vInt=VVpInt.Get();
			if (vInt!=NULL) {
				Descr +=vInt->Name.str;
			};
		}else{
			Descr += Time;
		};
	};
	Descr += "]";
	if (TrTime) {
		Descr += "ms";
	};
	return Descr.str;
};
int				lvCRunTimer::Process(int time){
	lvCOperation::Process(time);
	if (TimerID.Get()!=NULL){
		if (UseVV) {
			vvINTEGER* vInt=VVpInt.Get();
			if (vInt!=NULL) {
				RunTimer(*(reinterpret_cast<int*>(TimerID.Get()->Get())),vInt->Value,TrTime);
				return 1;
			};
		}else{
			RunTimer(*(reinterpret_cast<int*>(TimerID.Get()->Get())),Time,TrTime);
			return 1;
		};
	};
	return 0;
};
DWORD			lvCRunTimer::GetClassMask(){
	if (UseVV==true)	return 0x00000002;
	return 0x00000001;
};
// lvCSetGameMode ////////////////////////////////////////////////////////
lvCSetGameMode::lvCSetGameMode(lvCSetGameMode* pSetGameMode) : lvCOperation(dynamic_cast<lvCOperation*>(pSetGameMode)) {
	if (pSetGameMode!=NULL) {
		ModeST = pSetGameMode->ModeST;
	};
};

void			lvCSetGameMode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetGameMode(this));
};

const	char*	lvCSetGameMode::GetThisElementView(const char* LocalName){
	Descr = "GameMode[";
	Descr += ModeST;
	Descr += "]";
	return Descr.str;
};

int				lvCSetGameMode::Process(int time){
	lvCOperation::Process(time);
	// Unselect player units
	ClearSelection(MyNation);
	ImClearSelection(MyNation);
	GSets.CGame.ViewMask = ModeST;
	if (ModeST==0) {
		SetCurPtr(15);
	};
	return 1;
};
// lvCSetLMode ///////////////////////////////////////////////////////////
lvCSetLMode::lvCSetLMode(lvCSetLMode* pSetLMode) : lvCOperation(dynamic_cast<lvCOperation*>(pSetLMode)) {
	if (pSetLMode!=NULL) {
		if (pSetLMode->vMode.Get()!=NULL) vMode.Set(pSetLMode->vMode.Get());
		iMode	= pSetLMode->iMode;
		Use_VV	= pSetLMode->Use_VV;
	};
};

void			lvCSetLMode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetLMode(this));
};

const	char*	lvCSetLMode::GetThisElementView(const char* LocalName){
	Descr = "SetLMode(";
	if (Use_VV){
		if (vMode.Get()!=NULL){
			Descr += vMode.Get()->Value;
		}else{
			Descr += "NoValue";
		};
	}else{
		Descr += iMode;
	};
	Descr += ")";
	return Descr.str;
};

int				lvCSetLMode::Process(int time){
	lvCOperation::Process(time);
	if (true){
		void ReverseLMode();
		bool	lModeLoc = 0;
		if (Use_VV){
			if (vMode.Get()!=NULL){
				if (vMode.Get()->Value==0||vMode.Get()->Value==1){
					lModeLoc = vMode.Get()->Value;
				};
			};
		}else{
			if (iMode==1||iMode==0){
				lModeLoc=iMode;
			};
		};
		LMode=lModeLoc;
		ReverseLMode();
		return 1;
	};
	return 0;
};

int				lvCSetLMode::Complite(){
	return 1;
};
// lvCSetFogMode /////////////////////////////////////////////////////////
lvCSetFogMode::lvCSetFogMode(lvCSetFogMode* pSetFogMode) : lvCOperation(dynamic_cast<lvCOperation*>(pSetFogMode)) {
	if (pSetFogMode!=NULL) {
		Mode = pSetFogMode->Mode;
	};
};

void			lvCSetFogMode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetFogMode(this));
};

const	char*	lvCSetFogMode::GetThisElementView(const char* LocalName){
	Descr = "SetFogeMode(";
	Descr += Mode;
	Descr += ")";
	return Descr.str;
};

int				lvCSetFogMode::Process(int time){
	lvCOperation::Process(time);
	extern int FogMode;
	FogMode = Mode;
	return 1;
};
// lvCSetMessageState ////////////////////////////////////////////////////
lvCSetMessageState::lvCSetMessageState(lvCSetMessageState* pSetMessageState) : lvCOperation(dynamic_cast<lvCOperation*>(pSetMessageState)) {
	if (pSetMessageState!=NULL) {
		if (pSetMessageState->MESSAGE.Get()!=NULL) MESSAGE.Set(pSetMessageState->MESSAGE.Get());
		if (pSetMessageState->TALK_LST.Get()!=NULL) TALK_LST.Set(pSetMessageState->TALK_LST.Get());
		Visible	 = pSetMessageState->Visible;
		Deleted	 = pSetMessageState->Deleted;
		Color	 = pSetMessageState->Color;
		x		 = pSetMessageState->x;
		y		 = pSetMessageState->y;	
	};
};

void			lvCSetMessageState::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetMessageState(this));
};

const	char*	lvCSetMessageState::GetThisElementView(const char* LocalName){

	switch(MessType) {
	case 0:
		Descr = "SetMessageState(";
		if (MESSAGE.Get()!=NULL){
			Descr += MESSAGE.Get()->Name.str;
			Descr += ",";
			if (Visible)	Descr += "Visible";
			else			Descr += "InVisible";
			Descr += ",";
			if (Deleted)	Descr += "Complit";
			else			Descr += "NoComplit";
		};
		Descr += ")";
		break;
	case 1:
		Descr ="SetTalkListState(";
		if (TALK_LST.Get()!=NULL&&TALK_LST.Get()->TitleID.str!=NULL)	Descr+=TALK_LST.Get()->TitleID.str;
		else															Descr+="NoTitle";
		Descr+=",";
		if (Visible)	Descr += "Visible";
		else			Descr += "InVisible";
		Descr+=")";
		break;
	case 2:
		Descr = "Hint(";
		if (ONE_HINT.Get()!=NULL&&ONE_HINT.Get()->TextID.str!=NULL)		Descr+=ONE_HINT.Get()->TextID.str;
		else															Descr+="NoHint";
		Descr+=",";
		if (Visible)	Descr += "Visible";
		else			Descr += "InVisible";
		Descr+=")";
		break;
	};
	
	return Descr.str;
};

int				lvCSetMessageState::Process(int time){
	lvCOperation::Process(time);

	switch(MessType) {
	case 0:
		if (MESSAGE.Get()!=NULL){
			MESSAGE.Get()->Visible = Visible;
			MESSAGE.Get()->Deleted = Deleted;

			if (EngSettings.MIS_SET.DonotShowComleteQuest&&MESSAGE.Get()->Deleted==true)	
				MESSAGE.Get()->Visible=false;

			//	MESSAGE.Get()->Color   = Color;
			//	MESSAGE.Get()->x	   = x;
			//	MESSAGE.Get()->y	   = y;
			
			// Play some sound
			if (EngSettings.MIS_SET.Play_Task_Sound){
				if ( Visible && !Deleted ) {
					if (EngSettings.MIS_SET.PTS_FileName.str!=NULL&&
						EngSettings.MIS_SET.PTS_FileName.str[0]!=0){
							ov_Play(EngSettings.MIS_SET.PTS_FileName.str,0);
					};
				};
				if ( Visible && Deleted ) {
					if (EngSettings.MIS_SET.PTS_FileName_Give.str!=NULL&&
						EngSettings.MIS_SET.PTS_FileName_Give.str[0]!=0){
							ov_Play(EngSettings.MIS_SET.PTS_FileName_Give.str,0);
					};
				};
			};

			return 1;
		};
		break;
	case 1:
		if (TALK_LST.Get()!=NULL){
			TALK_LST.Get()->Visible=Visible;
			return 1;
		};
		break;
	case 2:
		if (ONE_HINT.Get()!=NULL){
			ONE_HINT.Get()->Visible=Visible;
			ONE_HINT.Get()->HasRead=false;
		};
		return 1;
		break;
	case 3:
		{
			vvMESSGES* pMess = dynamic_cast<vvMESSGES*>(vValuesMap()->GetVValueTypeID(_vvMESSGES_));
			if (pMess!=NULL) {
				pMess->SetMessDS_Visbility(TaskListVisible);
			};
		};
	};

	return 0;
};
// lvCSaveScreenPos //////////////////////////////////////////////////////
lvCSaveScreenPos::lvCSaveScreenPos(lvCSaveScreenPos* pSaveScreenPos) : lvCOperation(dynamic_cast<lvCOperation*>(pSaveScreenPos)) {
	if (pSaveScreenPos!=NULL) {
		if (pSaveScreenPos->vCameraPos.Get()!=NULL) vCameraPos.Set(pSaveScreenPos->vCameraPos.Get());
		if (pSaveScreenPos->vCameraDir.Get()!=NULL) vCameraDir.Set(pSaveScreenPos->vCameraDir.Get());
	};
};
const	char*	lvCSaveScreenPos::GetThisElementView(const char* LocalName){
	Descr = "SaveScreenPosDir(";
	vvVector3D* pPos = vCameraPos.Get();
	vvVector3D* pDir = vCameraDir.Get();
	if(pPos!=NULL) {
		Descr += "Pos -> ";
		Descr += pPos->GetName();
	};
	if (pDir!=NULL) {
		Descr += ", Dir -> ";
		Descr += pDir->GetName();
	};
	Descr += ")";
	return Descr.str;
};
int				lvCSaveScreenPos::Process(int time){
	lvCOperation::Process(time);
	vvVector3D* pPos = vCameraPos.Get();
	vvVector3D* pDir = vCameraDir.Get();
	if (pPos!=NULL) pPos->fSetPos.EvaluateFunction();
	if (pDir!=NULL) pDir->fSetDir.EvaluateFunction();
	return 1;
};
void			lvCSaveScreenPos::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSaveScreenPos(this));
};
// lvCFilmCopliteState ///////////////////////////////////////////////////
lvCFilmCopliteState::lvCFilmCopliteState(lvCFilmCopliteState* pFilmCopliteState) : lvCOperation(dynamic_cast<lvCOperation*>(pFilmCopliteState)) {
	if (pFilmCopliteState!=NULL) {
		if (pFilmCopliteState->Film.Get()!=NULL) Film.Set(pFilmCopliteState->Film.Get());
		Complite = pFilmCopliteState->Complite;
	};
};
const	char*	lvCFilmCopliteState::GetThisElementView(const char* LocalName){
	Descr = "FilmCopliteState(";
	lvCFilm* pFilm = Film.Get();
	if (pFilm!=NULL) Descr += pFilm->Name.str;
	else			 Descr += "NoFilm";
	Descr += ",";
	Descr += Complite;
	Descr += ")";
	return Descr.str;
};
int				lvCFilmCopliteState::Process(int time){
	lvCOperation::Process(time);
	lvCFilm* pFilm = Film.Get();
	if (pFilm!=NULL) {
		pFilm->ScriptComplit = Complite;
	};
	return 1;
};
void			lvCFilmCopliteState::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCFilmCopliteState(this));
};
//======================================================================//
//=================  GLOBAL APPLYNING FUNCTION  ========================//
//======================================================================//
// lvCSetGameSpeed ///////////////////////////////////////////////////////
lvCSetGameSpeed::lvCSetGameSpeed(lvCSetGameSpeed* pSetGameSpeed) : lvCOperation(dynamic_cast<lvCOperation*>(pSetGameSpeed)) {
	if (pSetGameSpeed!=NULL) {
		if (pSetGameSpeed->vSpeed.Get()!=NULL)	vSpeed.Set(pSetGameSpeed->vSpeed.Get());
		Use_VV = pSetGameSpeed->Use_VV;
		iSpeed = pSetGameSpeed->iSpeed;
	};
};
const	char*	lvCSetGameSpeed::GetThisElementView(const char* LocalName){
	Descr = "SetGameSpeed(";
	if (Use_VV){
		if (vSpeed.Get())	Descr += vSpeed.Get()->Value;
		else				Descr += "NoValue";
	}else{
		if (iSpeed)			Descr += iSpeed;
		else				Descr += "NoValue";
	};	
	Descr += ")";
	return Descr.str;
};
int				lvCSetGameSpeed::Process(int time){
	lvCOperation::Process(time);
	if (Use_VV){
		if (vSpeed.Get()){
			GSets.SVOpt.RequiredMsPerFrame=vSpeed.Get()->Value;
			return 1;
		};
	}else{
		GSets.SVOpt.RequiredMsPerFrame=iSpeed;
		return 1;
	};	
	return 0;
};
void			lvCSetGameSpeed::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetGameSpeed(this));
};
// lvCGetGameSpeed ///////////////////////////////////////////////////////
lvCGetGameSpeed::lvCGetGameSpeed(lvCGetGameSpeed* pGetGameSpeed) : lvCOperation(dynamic_cast<lvCOperation*>(pGetGameSpeed)) {
	if (pGetGameSpeed!=NULL) {
		if (pGetGameSpeed->vSpeed.Get()!=NULL)	vSpeed.Set(pGetGameSpeed->vSpeed.Get());
	};
};
const	char*	lvCGetGameSpeed::GetThisElementView(const char* LocalName){
	Descr = "GetGameSpeed(";
	if (vSpeed.Get())	Descr += vSpeed.Get()->Value;
	else				Descr += "NoValue";
	Descr += ")";
	return Descr.str;
};
int				lvCGetGameSpeed::Process(int time){
	lvCOperation::Process(time);
	if (vSpeed.Get()){
		vSpeed.Get()->Value = GSets.SVOpt.RequiredMsPerFrame;
		return 1;
	};
	return 0;
};
void			lvCGetGameSpeed::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCGetGameSpeed(this));
};

// lvCShowPanel //////////////////////////////////////////////////////////
lvCShowPanel::lvCShowPanel(lvCShowPanel* pShowPanel) : lvCOperation(dynamic_cast<lvCOperation*>(pShowPanel)) {
	if (pShowPanel!=NULL) {
		if (pShowPanel->TextID.str!=NULL)	TextID=pShowPanel->TextID.str;
	};
};
const	char*	lvCShowPanel::GetThisElementView(const char* LocalName){
	Descr = "ShowPanel(";
	if (TextID.str!=NULL)	Descr += TextID.str;
	else					Descr += "NoText";
	Descr += ",";
	Descr += "OpenTime[";
	Descr += OpenTime;
	Descr += "],";
	Descr += "ShowTime[";
	Descr += ShowTime;
	Descr += "],";
	Descr += "CloseTime[";
	Descr += CloseTime;
	Descr += "]";

	Descr += ")";

	return Descr.str;
};
int				lvCShowPanel::Process(int time){
	lvCOperation::Process(time);
	if (TextID.str!=NULL) vmIGP.StartFrame( GetTextByID(TextID.str),OpenTime,ShowTime,CloseTime);
	return 1;
};
void			lvCShowPanel::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCShowPanel(this));
};
// lvCShowPanel //////////////////////////////////////////////////////////
lvCQuestData::lvCQuestData(lvCQuestData* pQuestData) : lvCOperation(dynamic_cast<lvCOperation*>(pQuestData)) {
    if (pQuestData!=NULL) {
		vvMissionLOG* pMLOG = pQuestData->Data.Get();
		if (pMLOG!=NULL)		Data.Set(pMLOG);
		QuestN					= pQuestData->QuestN;
		QuestTotalExperience	= pQuestData->QuestTotalExperience;
		QuestComleteExperience	= pQuestData->QuestComleteExperience;
		OperType				= pQuestData->OperType;
    };
};
const	char*	lvCQuestData::GetThisElementView(const char* LocalName){
	switch(OperType) {
		case 0:	// "SetQuestData"
			Descr = "SetQuestData(QuestN[";
			Descr += QuestN;
			Descr += "],TotalExp[";
			Descr += QuestTotalExperience;
			Descr += "])";
			break;
		case 1: // "AddComplQuest"
			Descr = "AddComplQuest(";
			Descr += QuestComleteExperience;
			Descr += ")";
			break;
		case 2: // "SaveData"
			Descr = "SaveData()";
			break;
	};
	return Descr.str;
};
int				lvCQuestData::Process(int time){
	lvCOperation::Process(time);
	vvMissionLOG* pMLOG = Data.Get();
	if (pMLOG==NULL) return 0;
	switch(OperType) {
		case 0:	// "SetQuestData"
			pMLOG->SetQuestData(QuestN,QuestTotalExperience);
			pMLOG->SetKilsData(KillsTotalExperience);
			pMLOG->SetTimeData(TimeEverage,TimeEverageExperience);
			break;
		case 1: // "AddComplQuest"
			pMLOG->AddCopmleteQuest(QuestComleteExperience);
			break;
		case 2: // "SaveData"
			pMLOG->WriteToLogClass();
			break;
	};
	return 1;
};
void			lvCQuestData::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCQuestData(this));
};
// lvCReStartSquadShema //////////////////////////////////////////////////
lvCReStartSquadShema::lvCReStartSquadShema(lvCReStartSquadShema* pReStartSquadShema) : lvCOperation(dynamic_cast<lvCOperation*>(pReStartSquadShema)) {
	if (pReStartSquadShema!=NULL) {
		vGroup = pReStartSquadShema->vGroup;
		NodeID = pReStartSquadShema->NodeID;
	};
};
const	char*	lvCReStartSquadShema::GetThisElementView(const char* LocalName){
	Descr = "RestartNodeShema(Group[";
	lvCGroup* pvGroup = GroupsMap()->GetGroupID(vGroup);
	if (pvGroup!=NULL) {
		Descr += pvGroup->NAME.str;
	}else{
		Descr += "NoGroup";
	}
	Descr += "]";
	if (NodeID!=-1) {
		Descr += ",NewStartNodeID[";
		Descr += NodeID;
		Descr += "]";
	};
	Descr += ")";
	return Descr.str;
};
int				lvCReStartSquadShema::Process(int time){
	lvCOperation::Process(time);
	lvCSquardShema* pSS = BattleShema()->vGetSqShemaID(vGroup);
	lvCProcSquad*	pPS = BattleHandler()->vGetSquadScriptID(vGroup);

	if (pSS==NULL||pPS==NULL) return 0;

	if (NodeID!=-1) {
		pPS->NodeID		= NodeID;
	}else{
		pPS->NodeID		= pSS->vGetProbablyFirstNodeID();
	};
	pPS->EdgeID		= 0xFFFF;
	pPS->TimeInProc	= 0;

	// Set first -> true in all edges in shema
	pSS->vRestartEdges();

	return 1;
};
void			lvCReStartSquadShema::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCReStartSquadShema(this));
};
//////////////////////////////////////////////////////////////////////////
lvCPAUSE::lvCPAUSE(lvCPAUSE* pPAUSE): lvCOperation(dynamic_cast<lvCOperation*>(pPAUSE)){

};
const	char*	lvCPAUSE::GetThisElementView(const char* LocalName){
	Descr = "PAUSE(";	
	Descr += state;
	Descr += ")";
	return Descr.str;
};
int				lvCPAUSE::Process(int time){
	lvCOperation::Process(time);

	NOPAUSE = state;
	return 1;
};
void			lvCPAUSE::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCPAUSE(this));
};
//////////////////////////////////////////////////////////////////////////
lvCSetSilence::lvCSetSilence(lvCSetSilence* pSetSilence) : lvCOperation(dynamic_cast<lvCOperation*>(pSetSilence)){
	if (pSetSilence!=NULL){
		State = pSetSilence->State;
	};
};
const	char*	lvCSetSilence::GetThisElementView(const char* LocalName){
	Descr = "SetSilence(";
	Descr += State;
	Descr += ")";
	return Descr.str;
};
int				lvCSetSilence::Process(int time){
	lvCOperation::Process(time);
	GSets.CGame.SilenceMessageEvents=State;
	return 1;
};
void			lvCSetSilence::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSetSilence(this));
};
//======================================================================//
//========================  COSSAKS II  ================================//
//======================================================================//
// lvCShowMessageII //////////////////////////////////////////////////////
lvCShowMessageII::lvCShowMessageII(){
	InfID = _lvCShowMessageII_;
	FileID = 0xFFFF;
	SpriteID = -1;
	autoHideElse=true;
};
lvCShowMessageII::lvCShowMessageII(lvCShowMessageII* pShowMessageII) : lvCOperation(dynamic_cast<lvCOperation*>(pShowMessageII)) {
	if (pShowMessageII!=NULL) {
		FileID		= pShowMessageII->FileID;
		SpriteID	= pShowMessageII->SpriteID;
		TextID		= pShowMessageII->TextID.str;
		// copy param list - not in proc
		paramList.Clear();
	};
};
lvCShowMessageII::~lvCShowMessageII(){
};
const	char*	lvCShowMessageII::GetThisElementView(const char* LocalName){
	Descr = "ShowMessage(";

	if (FileID!=0xFFFF)				Descr += FileID;
	else							Descr += "NoFile";

	Descr += ",";

	if (SpriteID!=-1)				Descr += SpriteID;
	else							Descr += "NoSprite";

	Descr += ",";

	if (TextID.str!=NULL)			Descr += TextID.str;
	else							Descr += "NoText";

	Descr += ",";

	if (paramList.GetAmount()>0)	Descr += "SomeParam";
	else							Descr += "NoParam";

	Descr += ")";
	return Descr.str;
};
int				lvCShowMessageII::Process(int time){
	lvCOperation::Process(time);
	if (CheckState()==false)	return 0;
	_str	MessgeText;
	PrepareString(MessgeText);
	if (autoHideElse){
//		vValuesMap()->VIEW_OBJS(_vvMISSMGR_,false);
		vValuesMap()->VIEW_OBJS(_vvTASKS_CII_,false);
	};
	ShowMessageII(FileID,SpriteID,MessgeText.str);
	if (autoHideElse){
//		vValuesMap()->VIEW_OBJS(_vvMISSMGR_,true);
		vValuesMap()->VIEW_OBJS(_vvTASKS_CII_,true);
	};
	return 1;
};
bool			lvCShowMessageII::CheckState(){
	bool state = true;
	state = state && ( FileID!=0xFFFF );
	state = state && ( SpriteID!=-1 );
	state = state && ( TextID.str!=NULL );
	return state;
};
void			lvCShowMessageII::PrepareString(_str& FullString){
	if (FullString.str!=NULL) FullString.Clear();
	char	data[4096];
	char*	sPart = NULL;
	char	first[4096];
	char	second[4096];
	sprintf(data,"%s",GetTextByID(TextID.str));
	int NP = paramList.GetAmount();
	int cP=0;
	if (NP==0){
		FullString = data;
		return;
	};

	// Have some params for insert
	bool stop=false;
	int iteration=0;
	while (!stop) {
		iteration++;
		sprintf(first,"%s","");
		sprintf(second,"%s","");
		sPart = strstr(data,"%");
		if (sPart!=NULL) {
			int nc = (int)(sPart-data+1);
			strncpy(first,data,nc+2);
			first[nc+2]=0;
			if (data[nc+2]!=0)	sprintf(second,"%s",&(data[nc+2]));
			// set params
			if (cP<NP){
				int paramType = paramList[cP]->InfID;
				switch(paramType) {
				case _vvINTEGER_:	// insetr int param
					sprintf(data,first,((vvINTEGER*)(paramList[cP]))->Value);
					strcpy(first,data);
					break;
				case _vvTEXT_:		// insert string by TextID in it
					sprintf(data,first,GetTextByID(((vvTEXT*)(paramList[cP]))->TextID.str));
					strcpy(first,data);
					break;
				case _vvPICTURE_:
					_str picture;
					if ( ((vvPICTURE*)(paramList[cP]))->GetAsStringForMessage(picture) ){
						sprintf(data,first,picture.str);
						strcpy(first,data);
					}else{
						sprintf(data,first,"NO PICTURE");
						strcpy(first,data);
					};
					break;
				};
				cP++;
			};
			// create new data string
			sprintf(data,"%s%s",first,second);
		};
		if (sPart==NULL||cP>=NP||iteration>=100) {
			stop=true;
		};
	};

	FullString = data;
};
void			lvCShowMessageII::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCShowMessageII(this));
};
void			lvCShowMessageII::MS_PreShow::EvaluateFunction(){
	lvCShowMessageII* pParent = get_parent<lvCShowMessageII>();
	if (pParent!=NULL) {
		pParent->Process(0);
	};
};
// lvCBrigPanelSet ///////////////////////////////////////////////////////
lvCBrigPanelSet::lvCBrigPanelSet(){
	InfID		= _lvCBrigPanelSet_;
	Bayonet		= true;		
	Rifle		= true;
	Grenade		= true;
	Formation	= true;
	Disband		= true;
	Fill		= true;
};
lvCBrigPanelSet::lvCBrigPanelSet(lvCBrigPanelSet* pBrigPanelSet) : lvCOperation(dynamic_cast<lvCOperation*>(pBrigPanelSet)) {
	if (pBrigPanelSet!=NULL) {
		Bayonet		= pBrigPanelSet->Bayonet;		
		Rifle		= pBrigPanelSet->Rifle;
		Grenade		= pBrigPanelSet->Grenade;
		Formation	= pBrigPanelSet->Formation;
		Disband		= pBrigPanelSet->Disband;
		Fill		= pBrigPanelSet->Fill;
	};
};
lvCBrigPanelSet::~lvCBrigPanelSet(){
};
const	char*	lvCBrigPanelSet::GetThisElementView(const char* LocalName){
	Descr = "BrigPanelSet(";

	Descr += "Bayonet[";
	if (Bayonet==true)		Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += "Rifle[";
	if (Rifle==true)		Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += "Bayonet[";
	if (Bayonet==true)		Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += "Grenade[";
	if (Grenade==true)		Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += "Formation[";
	if (Formation==true)	Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += "Disband[";
	if (Disband==true)		Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += "Fill[";
	if (Fill==true)			Descr += "true";
	else					Descr += "false";
	Descr += "],";

	Descr += ")";
	return Descr.str;
};
int		lvCBrigPanelSet::Process(int time){
	lvCOperation::Process(time);
	cvs_BrigPanel BP;
	ApplyParams(BP);
	void SetBrigPanel(cvs_BrigPanel& BP);
	SetBrigPanel(BP);
	return 1;
};
void	lvCBrigPanelSet::MS_PreShow::EvaluateFunction(){
	lvCBrigPanelSet* pParent = get_parent<lvCBrigPanelSet>();
	if (pParent!=NULL) {
		pParent->Process(0);
	};
};
void	lvCBrigPanelSet::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCBrigPanelSet(this));
};
void	lvCBrigPanelSet::ApplyParams(cvs_BrigPanel& BP){
	BP.Weapon[0]=Bayonet;
	BP.Weapon[1]=Rifle;
	BP.Weapon[2]=Grenade;

	BP.Formation		= Formation;
	BP.Disband			= Disband;
	BP.Fill				= Fill;
	BP.Stop				= Stop;
};
// lvCGroupHoldNode //////////////////////////////////////////////////////
bool g_BrigadeAttackPoint(int NI, word index, int x, int y, const char *rulesFileName);
lvCGroupHoldNode::lvCGroupHoldNode(lvCGroupHoldNode* pGroupHoldNode) : lvCOperation(dynamic_cast<lvCOperation*>(pGroupHoldNode)) {
	if (pGroupHoldNode!=NULL) {
		vGrp = pGroupHoldNode->vGrp;
	};
};
const	char*	lvCGroupHoldNode::GetThisElementView(const char* LocalName){
	lvCGroup* pvGroup	= GroupsMap()->GetGroupID(vGrp);
	lvCNode*  pNode		= NodesMap()->vGetNode(parNode);
	Descr = "GroupHoldNode(";
	if (pvGroup!=NULL&&pNode!=NULL) {
		Descr += "GRP[";
		if (pvGroup->NAME.str!=NULL)	Descr += pvGroup->NAME.str;
		Descr += "],NODE[";
		if (pNode->vGetName()!=NULL)	Descr += pNode->vGetName();
		Descr += "]";
	}else{
		Descr += "BadParams";
	};
	Descr += ")";
	return Descr.str;
};
int				lvCGroupHoldNode::Process(int time){
	lvCOperation::Process(time);
	lvCGroup* pvGroup	= GroupsMap()->GetGroupID(vGrp);
	lvCNode*  pNode		= NodesMap()->vGetNode(parNode);
	if (pvGroup!=NULL&&pNode!=NULL) {
		int xxx,yyy;
		xxx=pNode->vGetX();
		yyy=pNode->vGetY();
		int NI = pvGroup->GetNation();
		LinearArray<int,_int> BR_LIST;
		pvGroup->GetBrigadeList(false,&BR_LIST);
		int N = BR_LIST.GetAmount();
		while (N--) {
			g_BrigadeAttackPoint(NI,BR_LIST[N],xxx,yyy,RulesFile.str);
		};
		return 1;
	};
	return 0;
};
void			lvCGroupHoldNode::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCGroupHoldNode(this));
};
int GroupHoldPOS_AI_lua(lvCGroup* pGRP,int x,int y,const char* FNane){
	if (pGRP!=NULL && FNane!=NULL){
		int NI = pGRP->GetNation();
		LinearArray<int,_int> BR_LIST;
		pGRP->GetBrigadeList(false,&BR_LIST);
		int N = BR_LIST.GetAmount();
		while (N--) {
			g_BrigadeAttackPoint(NI,BR_LIST[N],x,y,FNane);
		};
		return 1;
	};
	return 0;
};
// lvCAddElemTHE_CII /////////////////////////////////////////////////////
lvCAddElemTHE_CII::lvCAddElemTHE_CII(){
	InfID=_lvCAddElemTHE_CII_; 
	add_TASK=add_HINT=add_ELSE=false; 
	TASK_Dublicate=HINT_Dublicate=ELSE_Dublicate=false;
};
lvCAddElemTHE_CII::lvCAddElemTHE_CII(lvCAddElemTHE_CII* pAddElemTHE_CII) : lvCOperation(dynamic_cast<lvCOperation*>(pAddElemTHE_CII)) {
	// ...
};
const	char*	lvCAddElemTHE_CII::GetThisElementView(const char* LocalName){
	Descr = "lvCAddElemTHE_CII()";
	return Descr.str;
};
int				lvCAddElemTHE_CII::Process(int time){
	lvCOperation::Process(time);

	vvTASKS_CII* pTHE = OBJECT.Get();
	if (pTHE==NULL)	return 0;
	
	pTHE->SETIN_OBJECT();

	if (add_TASK&&TASK.str!=NULL) pTHE->addLT_TASK(TASK.str,TASK_POS,TASK_Dublicate);
	if (add_HINT&&HINT.str!=NULL) pTHE->addLT_HINT(HINT.str,HINT_POS,HINT_Dublicate);
	if (add_ELSE&&ELSE.str!=NULL) pTHE->addLT_ELSE(ELSE.str,ELSE_POS,ELSE_Dublicate);
	
	pTHE->ApplyMM_state();

	if      (add_TASK)		pTHE->vv_TASK_LCLIC();
//	else if (add_HINT)		pTHE->vv_HINT_LCLIC();
//	else if (add_ELSE)		pTHE->vv_ELSE_LCLIC();

	return 0;
};
void			lvCAddElemTHE_CII::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCAddElemTHE_CII(this));
};
// lvCDelElemTHE_CII /////////////////////////////////////////////////////
lvCDelElemTHE_CII::lvCDelElemTHE_CII(lvCDelElemTHE_CII* pDelElemTHE_CII) : lvCOperation(dynamic_cast<lvCOperation*>(pDelElemTHE_CII)) {
	// ...
	// ...
	// ...
};
const	char*	lvCDelElemTHE_CII::GetThisElementView(const char* LocalName){
	Descr = "lvCDelElemTHE_CII()";
	return Descr.str;
};
int				lvCDelElemTHE_CII::Process(int time){
	lvCOperation::Process(time);

	vvTASKS_CII* pTHE = OBJECT.Get();
	if (pTHE==NULL)	return 0;

	if (TASK_USE){
		if (FULL_DELETE)	pTHE->delLT_TASK(TASK.str);
		else				pTHE->setLT_TASK_COMPLITE(TASK.str);
	};
	if (HINT_USE)			pTHE->delLT_HINT(HINT.str);
	if (ELSE_USE)			pTHE->delLT_ELSE(ELSE.str);
		
	return 0;
};
void			lvCDelElemTHE_CII::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCDelElemTHE_CII(this));
};
// lvCSET_MISS_MANAGER ///////////////////////////////////////////////////
lvCSET_MISS_MANAGER::lvCSET_MISS_MANAGER(lvCSET_MISS_MANAGER* pSET_MISS_MANAGER) : lvCOperation(dynamic_cast<lvCOperation*>(pSET_MISS_MANAGER)){
	if (pSET_MISS_MANAGER!=NULL) {
		MISS_MANAGER	= pSET_MISS_MANAGER->MISS_MANAGER;
		PAUSE			= pSET_MISS_MANAGER->PAUSE;
		PAUSE_Animate	= pSET_MISS_MANAGER->PAUSE_Animate;
		RESTART			= pSET_MISS_MANAGER->RESTART;
		RESTART_Animate	= pSET_MISS_MANAGER->RESTART_Animate;
		NEXT			= pSET_MISS_MANAGER->NEXT;
		NEXT_Animate	= pSET_MISS_MANAGER->NEXT_Animate;
	};
};
const	char*	lvCSET_MISS_MANAGER::GetThisElementView(const char* LocalName){
	Descr = "SET_MISS_MANAGER(MANAGER[";
	vvMISSMGR* pMM = MISS_MANAGER.Get();
	if (pMM)	Descr += pMM->Name.str;
	else		Descr += "NOOBJECT";
	Descr += "],PAUSE[";
	Descr += PAUSE;
	if (PAUSE_Animate) Descr += ",Animate";
	Descr += "],RESTART[";
	Descr += RESTART;
	if (RESTART_Animate) Descr += ",Animate";
	Descr += "],NEXT[";
	Descr += NEXT;
	if (NEXT_Animate) Descr += ",Animate";
	Descr += "])";
	return Descr.str;
};
int				lvCSET_MISS_MANAGER::Process(int time){
	lvCOperation::Process(time);

	vvMISSMGR* pMM = MISS_MANAGER.Get();
	if (pMM!=NULL) {
		pMM->STPS_ENABLED	( PAUSE				);
		pMM->STPS_ANIM		( PAUSE_Animate		);
		pMM->REST_ENABLED	( RESTART			);
		pMM->REST_ANIM		( RESTART_Animate	);
		pMM->NEXT_ENABLED	( NEXT				);
		pMM->NEXT_ANIM		( NEXT_Animate		);
		return 1;
	};
	return 0;
};
void			lvCSET_MISS_MANAGER::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCSET_MISS_MANAGER(this));
};
// lvCArtChangeCharge ////////////////////////////////////////////////////
lvCArtChangeCharge::lvCArtChangeCharge(lvCArtChangeCharge* pArtChangeCharge): lvCOperation(dynamic_cast<lvCOperation*>(pArtChangeCharge)){
	if (pArtChangeCharge!=NULL) {
		vGrp	= pArtChangeCharge->vGrp;
		Charge	= pArtChangeCharge->Charge;
	}
};
const	char*	lvCArtChangeCharge::GetThisElementView(const char* LocalName){
	Descr = "lvCArtChangeCharge(";
	lvCGroup* pvGroup	= GroupsMap()->GetGroupID(vGrp);
	if (pvGroup!=NULL&&pvGroup->NAME.str!=NULL)	Descr += pvGroup->NAME.str;
	else										Descr += "NoGroup";
	Descr += ",Charge[";
	if (Charge==0)		Descr += "BALL";
	else if (Charge==1)	Descr += "CASE-SHOT";
	else				Descr += "NotSpecify";
	Descr += "])";
	return Descr.str;
};
int				lvCArtChangeCharge::Process(int time){
	lvCOperation::Process(time);

	lvCGroup* pvGroup	= GroupsMap()->GetGroupID(vGrp);
	if (pvGroup!=NULL) {
		OneObject* pOB=NULL;
		int N=pvGroup->GetTotalAmount();
		while (N--) {
			pOB=pvGroup->GetOneObj_lID(N);
			if (pOB!=NULL) SetOrderedStateForComplexObject(pOB,Charge);
			pOB=NULL;
		};
		return 1;
	};

	return 0;
};
void			lvCArtChangeCharge::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCArtChangeCharge(this));
};
// lvCArtAttackPoint /////////////////////////////////////////////////////
lvCArtAttackPoint::lvCArtAttackPoint(lvCArtAttackPoint* pArtAttackPoint): lvCOperation(dynamic_cast<lvCOperation*>(pArtAttackPoint)){
	if (pArtAttackPoint!=NULL) {
		vGrp = pArtAttackPoint->vGrp;
	}
};
const	char*	lvCArtAttackPoint::GetThisElementView(const char* LocalName){
	Descr = "lvCArtAttackPoint(";
	lvCGroup* pvGroup	= GroupsMap()->GetGroupID(vGrp);
	if (pvGroup!=NULL&&pvGroup->NAME.str!=NULL)	Descr += pvGroup->NAME.str;
	else										Descr += "NoGroup";
	Descr += ",";
	lvCNode*  pNode		= NodesMap()->vGetNode(parNode);
	if (pNode!=NULL&&UseNode==true&&pNode->vGetName()!=NULL)	Descr += pNode->vGetName();
	else														Descr += "NoAttackedPoint";
	Descr += ",NTimes[";
	Descr += NTimes;
	Descr += "])";
	return Descr.str;
};
int				lvCArtAttackPoint::Process(int time){
	lvCOperation::Process(time);

	lvCGroup* pvGroup	= GroupsMap()->GetGroupID(vGrp);
	lvCNode*  pNode		= NodesMap()->vGetNode(parNode);
	if (pvGroup!=NULL&&pNode!=NULL&&UseNode==true) {
		OneObject* pOB=NULL;
		int xxx=pNode->vGetX();
		int yyy=pNode->vGetY();
		int N=pvGroup->GetTotalAmount();
		while (N--) {
			pOB=pvGroup->GetOneObj_lID(N);
			if (pOB!=NULL) pOB->NewAttackPoint(xxx,yyy,128+16,0,NTimes);
			pOB=NULL;
		};
		return 1;
	};

	return 0;
};
void			lvCArtAttackPoint::GetCopy(lvCOperation** pCopy){
	*pCopy = dynamic_cast<lvCOperation*>(new lvCArtAttackPoint(this));
};
//======================================================================//
//=================    FUNCTION FOR CONDITION	========================//
//======================================================================//
// lvCBaseFunction ///////////////////////////////////////////////////////
lvCBaseFunction::lvCBaseFunction(lvCBaseFunction* pBaseFunction) : lvCBaseOperCond( dynamic_cast<lvCBaseOperCond*>(pBaseFunction) ) {
	if (pBaseFunction!=NULL) {
		
	};
};

void			lvCBaseFunction::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = new lvCBaseFunction(this);
};

// lvCGetValue ///////////////////////////////////////////////////////////
lvCGetValue::lvCGetValue(lvCGetValue* pGetValue) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetValue)) {
	if (pGetValue!=NULL) {
		if (pGetValue->Value.Get()!=NULL) Value.Set(pGetValue->Value.Get());
	};
};

void			lvCGetValue::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetValue(this));
};

const	char*	lvCGetValue::GetThisElementView(const char* LocalName){
	Descr="";
	if (Value.Get()!=NULL) {
		Descr += Value.Get()->GetName();
	}else{
		Descr = "No Value";
	};
	return	Descr.str;
};

int				lvCGetValue::GetValue(int time){
	int RetVal = 0;
	if (Value.Get()!=NULL) {
		vvBASE* pValue = dynamic_cast<vvBASE*>(Value.Get());
		if (pValue!=NULL) {
			if (pValue->InfID==_vvTRIGER_){
				vvTRIGER* pTrigg = dynamic_cast<vvTRIGER*>(pValue);
				if (pTrigg!=NULL) {
					(pTrigg->Value) ? (RetVal=1) : (RetVal=0);
				};
			}else if (pValue->InfID==_vvWORD_){
				vvWORD* pWord = dynamic_cast<vvWORD*>(pValue);
				if (pWord!=NULL) {
					RetVal = (int)(pWord->Value);
				};
			}else if (pValue->InfID==_vvINTEGER_) {
				vvINTEGER* pInteger = dynamic_cast<vvINTEGER*>(pValue);
				if (pInteger!=NULL) {
					RetVal = pInteger->Value;
				};
			};
		};
	};
	return	RetVal;
};
// lvCBool ///////////////////////////////////////////////////////////////
lvCBool::lvCBool(lvCBool* pBool) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pBool)) {
	if (pBool!=NULL) {
		lvB = pBool->lvB;
	};
};

void			lvCBool::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCBool(this));
};

const char*		lvCBool::GetThisElementView(const char* LocalName){
	Descr="";
	if(lvB)
		Descr="True";
	else
		Descr="False";
return Descr.str;
};
int				lvCBool::GetValue(int time){
	return lvB;
};
// lvCInt ////////////////////////////////////////////////////////////////
lvCInt::lvCInt(lvCInt* pInt) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pInt)) {
	if (pInt!=NULL) {
		lvI = pInt->lvI;
	};
};

void			lvCInt::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCInt(this));
};

const char*		lvCInt::GetThisElementView(const char* LocalName){
	Descr="";
	Descr=lvI;
return Descr.str;
};
int				lvCInt::GetValue(int time){
 return lvI;
};
// lvCGetScreenXY ////////////////////////////////////////////////////////
lvCGetScreenXY::lvCGetScreenXY(lvCGetScreenXY* pGetScreenXY) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetScreenXY)) {
	if (pGetScreenXY!=NULL) {
		if (pGetScreenXY->sX.Get()!=NULL) sX.Set(pGetScreenXY->sX.Get());
		if (pGetScreenXY->sY.Get()!=NULL) sY.Set(pGetScreenXY->sY.Get());
	};
};

void			lvCGetScreenXY::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetScreenXY(this));
};

const	char*	lvCGetScreenXY::GetThisElementView(const char* LocalName){
	Descr = "SaveScreenPos(";
	vvINTEGER* psX = sX.Get();
	vvINTEGER* psY = sY.Get();
	if (psX!=NULL&&psY!=NULL) {
		Descr += psX->GetName();
		Descr += ",";
		Descr += psY->GetName();
		Descr += ")";
	}else{
		Descr += "-,-)";
	};
	return Descr.str;
};

int				lvCGetScreenXY::GetValue(int time){
	vvINTEGER* psX = sX.Get();
	vvINTEGER* psY = sY.Get();
	if (psX!=NULL&&psY!=NULL) {
		psX->Set(&mapx);
		psY->Set(&mapy);
		return 1;
	};
	return	0;
};
// lvCChkTime ////////////////////////////////////////////////////////////
lvCChkTime::lvCChkTime(lvCChkTime* pChkTime) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pChkTime)) {
	if (pChkTime!=NULL) {
		timeOt	= pChkTime->timeOt;
		timeDo	= pChkTime->timeDo;
	};
};

void			lvCChkTime::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCChkTime(this));
};

const char*		lvCChkTime::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "ChekTimer(";
	Descr += timeOt;
	Descr += ", ";
	Descr += timeDo;
	Descr += ")";
return Descr.str;
};
int				lvCChkTime::GetValue(int time){
	if (timeOt<=time&&time<=timeDo) {
		return 1;
	}else{
		return 0;
	};
};
// lvCGetAmount //////////////////////////////////////////////////////////
lvCGetAmount::lvCGetAmount(lvCGetAmount* pGetAmount) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetAmount)) {
	if (pGetAmount!=NULL) {
		parNat = pGetAmount->parNat;
	};
};

void			lvCGetAmount::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetAmount(this));
};

const char*		lvCGetAmount::GetThisElementView(const char* LocalName){
	Descr = "";
	Descr += "Num(";
	if (0<=parNat&&parNat<8) {
		Descr+=parNat;
	};
	Descr += ")";
	return Descr.str;
};
int				lvCGetAmount::GetValue(int time){
	return GetAmount_lua(parNat,Buildings);
};
int	GetAmount_lua(int nat,bool buildings){
	int NU=0;
	OneObject*	pUnit = NULL;
	for (int i=0; i<MAXOBJECT; i++){
		pUnit = Group[i];
		if (pUnit&&!pUnit->Sdoxlo) {
			if (buildings==false&&pUnit->NNUM==nat&&pUnit->NewBuilding==false) {
				NU++;
			}else if (buildings==true&&pUnit->NNUM==nat&&pUnit->NewBuilding==true) {
				NU++;
			};
		}
	}
	return NU;
};
// lvCGetUnitsAmount0 ////////////////////////////////////////////////////
lvCGetUnitsAmount0::lvCGetUnitsAmount0(lvCGetUnitsAmount0* pGetUnitsAmount0) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetUnitsAmount0)) {
	if (pGetUnitsAmount0!=NULL) {
		parZn	= pGetUnitsAmount0->parZn;
		parNat	= pGetUnitsAmount0->parNat;
	};
};

void			lvCGetUnitsAmount0::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetUnitsAmount0(this));
};

const char*		lvCGetUnitsAmount0::GetThisElementView(const char* LocalName){
	Descr="";
	if (UseNode){
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL){
			Descr += "Num(";
			Descr += pNode->vGetName();
			Descr += ", ";
			Descr += parNat;
			Descr += ")";
		}else{
			Descr="GetUnitsAmount0(NULL)";
		};
	}else if((0<=parNat&&parNat<8)&&(0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)){
		Descr += "Num(";
		Descr += AZones[parZn]->Name.str;
		Descr += ", ";
		Descr += parNat;
		Descr += ")";
	}else{
		Descr="GetUnitsAmount0(NULL)";
	};
return Descr.str;
};
int				lvCGetUnitsAmount0::GetValue(int time){
	int xxx=-1;
	int yyy=-1;
	int RRR=-1;

	if (UseNode) {
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL) {
			xxx=pNode->vGetX();
			yyy=pNode->vGetY();
			RRR=pNode->vGetR();
		};
	}else if ((0<=parNat&&parNat<8)&&(0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)&&(0<AZones[parZn]->R)) {
		xxx=AZones[parZn]->x;
		yyy=AZones[parZn]->y;
		RRR=AZones[parZn]->R;
	}

	return GetUnitsAmount0_lua(parNat,xxx,yyy,RRR);
};
int GetUnitsAmount0_lua(int nat,int x,int y,int R){
	lvSSumNat	Ret;	Ret.Sum = 0;
	if (x!=-1&&y!=-1&&R!=-1) {
		Ret.Nat = nat;
		PerformActionOverUnitsInRadius(	x,y,R,AddUnitToSumN,&Ret );
	};
	return Ret.Sum;
};
// lvCGetUnitsAmount1 ////////////////////////////////////////////////////
lvCGetUnitsAmount1::lvCGetUnitsAmount1(lvCGetUnitsAmount1* pGetUnitsAmount1) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetUnitsAmount1)) {
	if (pGetUnitsAmount1!=NULL) {
		parZn	= pGetUnitsAmount1->parZn;
		parGrp	= pGetUnitsAmount1->parGrp;
	};
};

void			lvCGetUnitsAmount1::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetUnitsAmount1(this));
};

const char*		lvCGetUnitsAmount1::GetThisElementView(const char* LocalName){
	Descr="";

	// Finde name
	_str NZName;
	if (UseNode) {
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL) {
			NZName = pNode->vGetName();
		}else{
			NZName = "NoNodeName";
		};
	}else if (0<=parGrp&&(0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)) {
		Descr += AZones[parZn]->Name.str;
	};

	if(NZName.str!=NULL){
		Descr += "Num(";
		Descr += NZName.str;
		Descr += ", ";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ")";
	}else{
		Descr="GetUnitsAmount1(NULL)";
	};
	return Descr.str;
};
int				lvCGetUnitsAmount1::GetValue(int time){
	lvSSumGr	Ret;	Ret.Sum = 0; 
	
	int xxx=-1;
	int yyy=-1;
	int RRR=-1;

	if (UseNode) {
		lvCNode* pNode = NodesMap()->vGetNode(parNode);
		if (pNode!=NULL) {
			xxx=pNode->vGetX();
			yyy=pNode->vGetY();
			RRR=pNode->vGetR();
		};
	}else if ((0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)) {
		xxx=AZones[parZn]->x;
		yyy=AZones[parZn]->y;
		RRR=AZones[parZn]->R;
	};

	if (xxx!=-1&&yyy!=-1&&RRR!=-1){
		if (use_vGroup) {
				lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
				if (pvGRP) {
					Ret.Sum=pvGRP->GetAmountInZone(xxx,yyy,RRR);
				};
		}else{
			if ((AGroups[parGrp]!=NULL)&&(0<AZones[parZn]->R)) {
				Ret.GrpID = parGrp;
				PerformActionOverUnitsInRadius(	xxx,
												yyy,
												RRR,
												AddUnitToSumGr,
												&Ret			 );
			};
		};
	};
	return Ret.Sum;
};
// lvCGetUnitsAmount2 ////////////////////////////////////////////////////
lvCGetUnitsAmount2::lvCGetUnitsAmount2(lvCGetUnitsAmount2* pGetUnitsAmount2) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetUnitsAmount2)) {
	if (pGetUnitsAmount2!=NULL) {
		parZn		= pGetUnitsAmount2->parZn;
		UnitType	= pGetUnitsAmount2->UnitType;
		parNat		= pGetUnitsAmount2->parNat;
	};
};

void			lvCGetUnitsAmount2::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetUnitsAmount2(this));
};

const char*		lvCGetUnitsAmount2::GetThisElementView(const char* LocalName){
	Descr="";
	if((UnitType!=NULL)&&(0<=parNat&&parNat<8)&&(0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&(AZones[parZn]!=NULL)){
		Descr += "Num(";
		Descr += AZones[parZn]->Name.str;
		Descr += ", ";
		Descr += NATIONS[parNat].Mon[UnitType]->newMons->Name;
		Descr += ", ";
		Descr += parNat;
		Descr += ")";
	}else{
		Descr="GetUnitsAmount2(NULL)";
	};
	return Descr.str;
};
int				lvCGetUnitsAmount2::GetValue(int time){
	lvSSumNatType	Ret;	Ret.Sum = 0; 
	if ((0<=parNat&&parNat<8)&&(0<UnitType)&&
		(0<AZones.GetAmount()&&parZn<AZones.GetAmount())&&
		(AZones[parZn]!=NULL)&&(0<AZones[parZn]->R)			) 
	{
		Ret.Nat = parNat;
		Ret.TypeID  = UnitType;
		PerformActionOverUnitsInRadius(	AZones[parZn]->x,
										AZones[parZn]->y,
										AZones[parZn]->R,
										AddUnitToSumType,
										&Ret			 );
	};
	return Ret.Sum;
};
int GetUnitsAmount2_lua(int nat,int UT,int x,int y,int R){
	lvSSumNatType	Ret;	Ret.Sum = 0; 
	if (0<=nat&&nat<8&&R>0){
		Ret.Nat = nat;
		Ret.TypeID  = UT;
		PerformActionOverUnitsInRadius(	x,y,R,AddUnitToSumType,&Ret );
	};
	return Ret.Sum;
};
// lvCGetUnitsAmount3 ////////////////////////////////////////////////////
lvCGetUnitsAmount3::lvCGetUnitsAmount3(lvCGetUnitsAmount3* pGetUnitsAmount3) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetUnitsAmount3)) {
	if (pGetUnitsAmount3!=NULL) {
		vGrp	= pGetUnitsAmount3->vGrp;
		parRad	= pGetUnitsAmount3->parRad;
		parNat	= pGetUnitsAmount3->parNat;
	};
};

void			lvCGetUnitsAmount3::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetUnitsAmount3(this));
};

const char*		lvCGetUnitsAmount3::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "Near(";
		if(use_vGroup){
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(vGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			}else{
				Descr+="NULL";
			};
		}else{
			Descr+="NULL";
		};
		Descr += ", ";
		Descr += parRad;
		Descr += ", ";
		if((0<=parNat&&parNat<8)){
			Descr += parNat;
		}else{
			Descr+="NULL";
		};
	Descr += ")";
	return Descr.str;
};
int				lvCGetUnitsAmount3::GetValue(int time){
	lvSSumNat	Ret;	Ret.Sum = 0; 
	if ((0<=parNat&&parNat<8)) {
		Ret.Nat = parNat;
		int vgX;
		int vgY;
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(vGrp);
			if (pvGRP) {
				pvGRP->GetGroupCenter(vgX,vgY);
				PerformActionOverUnitsInRadius(	vgX,
												vgY,
												parRad,
												AddUnitToSumN,
												&Ret			 );
			};
		};
	};
	return Ret.Sum;
};
// lvCGetTotalAmount0 ////////////////////////////////////////////////////
lvCGetTotalAmount0::lvCGetTotalAmount0(lvCGetTotalAmount0* pGetTotalAmount0) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetTotalAmount0)) {
	if (pGetTotalAmount0!=NULL) {
		parGrp = pGetTotalAmount0->parGrp;
	};
};

void			lvCGetTotalAmount0::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetTotalAmount0(this));
};

const char*		lvCGetTotalAmount0::GetThisElementView(const char* LocalName){
	Descr = "";
	if(0<=parGrp){
		Descr += "Num(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		};
		Descr += ")";
	}else{
        Descr="GetTotalAmount0(NULL)";
	};
return Descr.str;
};
int				lvCGetTotalAmount0::GetValue(int time){
	int NU=0;
	if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				NU=pvGRP->GetTotalAmount();
			};
	}else{
		if ((AGroups[parGrp]!=NULL)) {
			if (parGrp<AGroups.GetAmount()) {
				int Nu=AGroups[parGrp]->Units.GetAmount();
				for(int i=0;i<Nu;i++){
					word MID=AGroups[parGrp]->Units[i].ID;
					if(MID!=0xFFFF){
						OneObject* OB=Group[MID];
						if(OB&&(OB->Hidden||!OB->Sdoxlo)) NU++;
					};
				};
			};
		};
	};
	return NU;
};
// lvCGetTotalAmount1 ////////////////////////////////////////////////////
lvCGetTotalAmount1::lvCGetTotalAmount1(lvCGetTotalAmount1* pGetTotalAmount1) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetTotalAmount1)) {
	if (pGetTotalAmount1!=NULL) {
		UnitType	= pGetTotalAmount1->UnitType;
		parNat		= pGetTotalAmount1->parNat;
	};
};

void			lvCGetTotalAmount1::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetTotalAmount1(this));
};

const char*		lvCGetTotalAmount1::GetThisElementView(const char* LocalName){
	Descr="";
	if((UnitType!=NULL)&&(0<=parNat&&parNat<8)){
		Descr += "Num(";
		Descr += NATIONS[parNat].Mon[UnitType]->newMons->Name;
		Descr += ", ";
		Descr += parNat;
		Descr += ")";
	}else{
		Descr = "GetTotalAmount1(NULL)";
	};
	return Descr.str;
};
int				lvCGetTotalAmount1::GetValue(int time){
	return GetTotalAmount1_lua(parNat,UnitType);
};
int GetTotalAmount1_lua(int nat,int UT){
	int NU=0;
	if (0<=nat&&nat<8) {
		if(UT<NATIONS->NMon){
			NU = NATIONS[nat].CITY->UnitAmount[UT];
		};
	};
	return NU;
};
// lvCGetTotalAmount2 ////////////////////////////////////////////////////
lvCGetTotalAmount2::lvCGetTotalAmount2(lvCGetTotalAmount2* pGetTotalAmount2) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetTotalAmount2)) {
	if (pGetTotalAmount2!=NULL) {
		parGrp		= pGetTotalAmount2->parGrp;
		UnitType	= pGetTotalAmount2->UnitType;
	};
};

void			lvCGetTotalAmount2::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetTotalAmount2(this));
};

const char*		lvCGetTotalAmount2::GetThisElementView(const char* LocalName){
	Descr="";
	if((0<=parGrp)&&(UnitType!=NULL)){
		Descr += "Num(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
				Descr += ", ";
				Descr += NATIONS[pvGRP->GetNation()].Mon[UnitType]->newMons->Name;
			};
		}else{
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
				Descr += ", ";
				word MID=AGroups[parGrp]->Units[0].ID;
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if (OB) {
						Descr += NATIONS[OB->NNUM].Mon[UnitType]->newMons->Name;
					};
				};
			};
		};
		Descr += ")";
	}else{
		Descr = "GetTotalAmount2(NULL)";
	};
	return Descr.str;
};
int				lvCGetTotalAmount2::GetValue(int time){
	int NU=0;
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			NU=pvGRP->GetTotalAmount2(UnitType);
		};
	}else{
		if ((AGroups[parGrp]!=NULL)) {
			int Nu=AGroups[parGrp]->Units.GetAmount();
			for(int i=0;i<Nu;i++){
				word MID=AGroups[parGrp]->Units[i].ID;
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if(OB&&(OB->Hidden||!OB->Sdoxlo)&&OB->Index==AGroups[parGrp]->Units[i].ID&&OB->Serial==AGroups[parGrp]->Units[i].SN&&OB->NIndex==UnitType) NU++;
				};
			};
		};
	};
	return NU;
};

// lvCGetReadyAmount /////////////////////////////////////////////////////
lvCGetReadyAmount::lvCGetReadyAmount(lvCGetReadyAmount* pGetReadyAmount) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetReadyAmount)) {
	if (pGetReadyAmount!=NULL) {
		UnitType	= pGetReadyAmount->UnitType;
		parNat		= pGetReadyAmount->parNat;
	};
};

void			lvCGetReadyAmount::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetReadyAmount(this));
};

const char*		lvCGetReadyAmount::GetThisElementView(const char* LocalName){
	Descr="";
	if((UnitType!=NULL)&&(0<=parNat&&parNat<8)){
		Descr += "Num(";
		Descr += NATIONS[parNat].Mon[UnitType]->newMons->Name;
		Descr += ", ";
		Descr += parNat;
		Descr += ")";
	}else{
		Descr = "GetReadyAmount(NULL)";
	};
	return Descr.str;
};
int				lvCGetReadyAmount::GetValue(int time){
	return GetReadyAmount_lua(parNat,UnitType);
};	
int GetReadyAmount_lua(int nat,int UT){
	int NU=0;
	if((0<UT)&&(0<=nat&&nat<8)){
		return NATIONS[nat].CITY->ReadyAmount[UT];
	};
	return NU;
};
// lvCGetResource ////////////////////////////////////////////////////////
lvCGetResource::lvCGetResource(lvCGetResource* pGetResource) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetResource)) {
	if (pGetResource!=NULL) {
		parNat	= pGetResource->parNat;
		parID	= pGetResource->parID;
	};
};

void			lvCGetResource::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetResource(this));
};

const char*		lvCGetResource::GetThisElementView(const char* LocalName){
	Descr="";
	Enumerator* E=ENUM.Get("RESTYPE");
	if((0<=parNat&&parNat<8)){
		Descr += "GetResource(";
		Descr += parNat;
		Descr += ", ";
		Descr += E->GetStr(parID);
		Descr += ")";
	}else{
		Descr = "GetResource(NULL)";
	};
	return Descr.str;
};
int				lvCGetResource::GetValue(int time){
	return GetResource_lua(parNat,parID);
};
int GetResource_lua(int nat,int resid){
	int NU=0;
	NU=GetResource(nat,resid);
	return NU;
};
// lvCGetDiff ////////////////////////////////////////////////////////////
lvCGetDiff::lvCGetDiff(lvCGetDiff* pGetDiff) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetDiff)) {
	if (pGetDiff!=NULL) {
		parNI = pGetDiff->parNI;
	};
};

void			lvCGetDiff::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetDiff(this));
};

const char*		lvCGetDiff::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parNI){
		Descr += "GetDiff(";
		Descr += parNI;
		Descr += ")";
	}else{
		Descr = "GetDiff(NULL)";
	};
return Descr.str;
};
int				lvCGetDiff::GetValue(int time){
	return GetDiff_lua(parNI);	
};
int GetDiff_lua(int nat){
	int NU=0;
	NU=GetDiff(nat);	
	return NU;
};
// lvCProbably ///////////////////////////////////////////////////////////
lvCProbably::lvCProbably(lvCProbably* pProbably) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pProbably)) {
	if (pProbably!=NULL) {
		parVer = pProbably->parVer;
	};
};

void			lvCProbably::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCProbably(this));
};

const char*		lvCProbably::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=parVer){
		Descr += "Probably(";
		Descr += parVer;
		Descr += ")";
	}else{
		Descr = "lvCProbably(NULL)";
	};
	return Descr.str;
};
int				lvCProbably::GetValue(int time){
	int NU=rando();
	if(parVer>(100*NU/32768)){
		return 1;
	}else{
		return 0;
	};
};
// lvCGetUnitState ///////////////////////////////////////////////////////
lvCGetUnitState::lvCGetUnitState(lvCGetUnitState* pGetUnitState) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetUnitState)) {
	if (pGetUnitState!=NULL) {
		parGrp = pGetUnitState->parGrp;	
	};
};

void			lvCGetUnitState::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetUnitState(this));
};

const char*		lvCGetUnitState::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "GetUnitState(";
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			Descr += pvGRP->GetGroupName();
		}else{
			Descr += "NULL";
		};
	}else{
		if(0<=parGrp){
			if ((AGroups[parGrp]!=NULL)) {
				Descr += AGroups[parGrp]->Name.str;
			};
		}else{
			Descr += "NULL";
		};
	};
	Descr += ")";
	return Descr.str;
};
int				lvCGetUnitState::GetValue(int time){
	int NU=1;
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			NU=pvGRP->GetAgresiveState();
		};
	}else{
		if (0<=parGrp) {
			if ((AGroups[parGrp]!=NULL)) {
				int Nu=AGroups[parGrp]->Units.GetAmount();
				while(Nu--){
					word MID=AGroups[parGrp]->Units[Nu].ID;
					if(MID!=0xFFFF){
						OneObject* OB=Group[MID];
						if (OB&&(OB->Hidden||!OB->Sdoxlo)) {
							NU=OB->ActivityState;
							break;
						};
					};
				};
			};
		};
	};
	return NU;
};

// lvCTrigg //////////////////////////////////////////////////////////////
lvCTrigg::lvCTrigg(lvCTrigg* pTrigg) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pTrigg)) {
    if (pTrigg!=NULL) {
		TID = pTrigg->TID;
    };
};

void			lvCTrigg::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCTrigg(this));
};

const char*		lvCTrigg::GetThisElementView(const char* LocalName){
	Descr="";
	if(TID<=511){
		Descr += "Trigg(";
		Descr += TID;
		Descr += ")";
	}else{
		Descr = "lvCTrigg(NULL)";
	};
	return Descr.str;
};
int				lvCTrigg::GetValue(int time){
	return Trigg_lua(TID);
};
int Trigg_lua(int trigid){
	return ( (trigid>511) ? (0) : (~SCENINF.TRIGGER[trigid]) );
};
// lvCGrpInNode //////////////////////////////////////////////////////////
lvCGrpInNode::lvCGrpInNode(lvCGrpInNode* pGrpInNode) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGrpInNode)) {
	if (pGrpInNode!=NULL) {
		
	};
};

void			lvCGrpInNode::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGrpInNode(this));
};

const char*		lvCGrpInNode::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "GroupInNode ()";
	return Descr.str;
};
int				lvCGrpInNode::GetValue(int time){
	bool ret;
	ret=lvBE_GroupInNode(squardID,x1,y1,use_vGroup);
	return ret;
};
// lvCGrpInNodeFree //////////////////////////////////////////////////////
lvCGrpInNodeFree::lvCGrpInNodeFree(lvCGrpInNodeFree* pGrpInNodeFree) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGrpInNodeFree)) {
	if (pGrpInNodeFree!=NULL) {
		
	};
};

void			lvCGrpInNodeFree::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGrpInNodeFree(this));
};

const char*		lvCGrpInNodeFree::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "FreeGroupInNode ()";
	return Descr.str;
};
int				lvCGrpInNodeFree::GetValue(int time){
	bool ret;
	ret=lvBE_GroupInNodeFree(squardID,x1,y1,use_vGroup);
	return ret;
};
// lvCAllGrpInNode ///////////////////////////////////////////////////////
lvCAllGrpInNode::lvCAllGrpInNode(lvCAllGrpInNode* pAllGrpInNode) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pAllGrpInNode)) {
	if (pAllGrpInNode!=NULL) {
		grpID = pAllGrpInNode->grpID;
	};
};

void			lvCAllGrpInNode::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCAllGrpInNode(this));
};

const char*		lvCAllGrpInNode::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=grpID){
		Descr += "GroupInNode(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(grpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[grpID]!=NULL)) {
				Descr += AGroups[grpID]->Name.str;
			};
		};
		Descr += ")";
	}else{
		Descr = "lvCAllGrpInNode(NULL)";
	};
	return Descr.str;
};
int				lvCAllGrpInNode::GetValue(int time){
	bool ret;
	ret=lvBE_GroupInNode(grpID,x1,y1,use_vGroup);
	return ret;
};
// lvCAllGrpInNodeFree ///////////////////////////////////////////////////
lvCAllGrpInNodeFree::lvCAllGrpInNodeFree(lvCAllGrpInNodeFree* pAllGrpInNodeFree) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pAllGrpInNodeFree)) {
	if (pAllGrpInNodeFree!=NULL) {
		grpID = pAllGrpInNodeFree->grpID;
	};
};

void			lvCAllGrpInNodeFree::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCAllGrpInNodeFree(this));
};

const char*		lvCAllGrpInNodeFree::GetThisElementView(const char* LocalName){
	Descr="";
	if(0<=grpID){
		Descr += "FreeGroupInNode(";
		if (use_vGroup) {
			lvCGroup* pvGRP = GroupsMap()->GetGroupID(grpID);
			if (pvGRP) {
				Descr += pvGRP->GetGroupName();
			};
		}else{
			if ((AGroups[grpID]!=NULL)) {
				Descr += AGroups[grpID]->Name.str;
			};
		};
		Descr += ")";
	}else{
		Descr = "lvCAllGrpInNodeFree(NULL)";
	};
	return Descr.str;
};
int				lvCAllGrpInNodeFree::GetValue(int time){
	bool ret;
	ret=lvBE_GroupInNodeFree(grpID,x1,y1,use_vGroup);
	return ret;
};
// FG_Visible ////////////////////////////////////////////////////////////
FG_Visible::FG_Visible(FG_Visible* pFG_Visible) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pFG_Visible)) {
	if (pFG_Visible!=NULL) {
		if (pFG_Visible->GraphObj.Get()!=NULL) GraphObj.Set(pFG_Visible->GraphObj.Get());
	};
};

void			FG_Visible::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new FG_Visible(this));
};

const	char*	FG_Visible::GetThisElementView(const char* LocalName){
	Descr = "";
	lvCGraphObject* pObj = GraphObj.Get();
	if (pObj){
		Descr += "isVisible(";
		Descr += pObj->Name.str;
		Descr += ")";
	}else{
		Descr += "NoGraphObj";
	};
	return Descr.str;
};

int				FG_Visible::GetValue(int time){
	lvCGraphObject* pObj = GraphObj.Get();
	if (pObj){
		if (pObj->isVissible())	return 1;
	};
	return 0;
};

// FG_InVisible //////////////////////////////////////////////////////////
FG_InVisible::FG_InVisible(FG_InVisible* pFG_InVisible) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pFG_InVisible)) {
	if (pFG_InVisible!=NULL) {
		if (pFG_InVisible->GraphObj.Get()!=NULL) GraphObj.Set(pFG_InVisible->GraphObj.Get());
	};
};

void			FG_InVisible::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new FG_InVisible(this));
};

const	char*	FG_InVisible::GetThisElementView(const char* LocalName){
	Descr = "";
	lvCGraphObject* pObj = GraphObj.Get();
	if (pObj){
		Descr += "isInVisible(";
		Descr += pObj->Name.str;
		Descr += ")";
	}else{
		Descr += "NoGraphObj";
	};
	return Descr.str;
};

int				FG_InVisible::GetValue(int time){
	lvCGraphObject* pObj = GraphObj.Get();
	if (pObj){
		if (pObj->isInVisible())	return	1;
	};
	return 0;
};
// ogSTOP ////////////////////////////////////////////////////////////////
ogSTOP::ogSTOP(ogSTOP* pogSTOP) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pogSTOP)) {
	if (pogSTOP!=NULL) {
		canal = pogSTOP->canal;
	};
};

void			ogSTOP::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new ogSTOP(this));
};

const	char*	ogSTOP::GetThisElementView(const char* LocalName){
	Descr = "SoundStop[";
	Descr += canal;
	Descr += "]";
	return Descr.str;
};

int				ogSTOP::GetValue(int time){
	return ogSTOP_lua(canal);
};
int ogSTOP_lua(int canal){
	if (ov_StreamFinished((DWORD)canal)!=0)	return 1;
	return 0;
};
// lvCTimerDone //////////////////////////////////////////////////////////
DLLEXPORT bool TimerDone(byte ID);
lvCTimerDone::lvCTimerDone(lvCTimerDone* pTimerDone) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pTimerDone)) {
	if (pTimerDone!=NULL) {
		if (pTimerDone->TimerID.Get()!=NULL)	TimerID.Set(pTimerDone->TimerID.Get());
	};
};

void			lvCTimerDone::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCTimerDone(this));
};

const	char*	lvCTimerDone::GetThisElementView(const char* LocalName){
	Descr = "TimerDone[";
	if (TimerID.Get()!=NULL) {
		Descr += TimerID.Get()->Name.str;
	};
	Descr += "]";
	return Descr.str;
};

int				lvCTimerDone::GetValue(int time){
	if (TimerID.Get()!=NULL){
		vvINTEGER* pGetVal = reinterpret_cast<vvINTEGER*>(TimerID.Get());
		if (pGetVal!=NULL){
			if ( TimerDone(pGetVal->Value) ) return 1;
		};
	};
	return 0;
};

// lvCChekPosition ///////////////////////////////////////////////////////
lvCChekPosition::lvCChekPosition(lvCChekPosition* pChekPosition) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pChekPosition)) {
	if (pChekPosition!=NULL) {
		parGrp	= pChekPosition->parGrp;
		if (pChekPosition->VVpPos.Get()!=NULL) VVpPos.Set(pChekPosition->VVpPos.Get());
	};
};

void			lvCChekPosition::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCChekPosition(this));
};

const char*		lvCChekPosition::GetThisElementView(const char* LocalName){
	vvPOINT_SET* vPS=VVpPos.Get();
	Descr="";
	Descr+=	"Chek position(";
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			Descr += pvGRP->GetGroupName();
		};
	};
	Descr+= ", ";
	if (vPS!=NULL) {
		Descr+= vPS->GetName();
	};
	Descr+= ")";
	return Descr.str;
};
int				lvCChekPosition::GetValue(int time){
	vvPOINT_SET* vPS=VVpPos.Get();
	bool ret;
	ret=false;
	if (vPS==NULL) {
		return ret;
	};
	if (use_vGroup) {
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(parGrp);
		if (pvGRP) {
			ret=pvGRP->ChekPosition(vPS);
		};
	};
	return ret;
};
// lvCCameraSTOP /////////////////////////////////////////////////////////
lvCCameraSTOP::lvCCameraSTOP(lvCCameraSTOP* pCameraSTOP) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pCameraSTOP)) {
	if (pCameraSTOP!=NULL) {
		
	};
};

void			lvCCameraSTOP::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCCameraSTOP(this));
};

const	char*	lvCCameraSTOP::GetThisElementView(const char* LocalName){
	Descr = "IsCameraStop";
	return Descr.str;
};

int				lvCCameraSTOP::GetValue(int time){
	return CameraSTOP_lua();
};
int CameraSTOP_lua(){
	if ((int)(CameraDriver()->MoveType)==0)	return 1;
	return 0;
};
// lvCNationIsErased /////////////////////////////////////////////////////
lvCNationIsErased::lvCNationIsErased(lvCNationIsErased* pNationIsErased) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pNationIsErased)) {
	if (pNationIsErased!=NULL) {
		Nat = pNationIsErased->Nat;
	};
};

void			lvCNationIsErased::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCNationIsErased(this));
};

const	char*	lvCNationIsErased::GetThisElementView(const char* LocalName){
	Descr = "NationIsErased(";
	Descr += Nat;
	Descr += ")";
	return Descr.str;
};

int				lvCNationIsErased::GetValue(int time){
	return NationIsErased(Nat);
};
// lvCGetLMode ///////////////////////////////////////////////////////////
lvCGetLMode::lvCGetLMode(lvCGetLMode* pGetLMode) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetLMode)) {
	if (pGetLMode!=NULL) {
		
	};
};

void			lvCGetLMode::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetLMode(this));
};

const	char*	lvCGetLMode::GetThisElementView(const char* LocalName){
	Descr = "GetLMode()";
	return Descr.str;
};

int				lvCGetLMode::GetValue(int time){
	return	GetLMode_lua();
};
int GetLMode_lua(){
	if (LMode)	return 1;
	return 0;
};
// lvCCheckButton ////////////////////////////////////////////////////////
lvCCheckButton::lvCCheckButton(lvCCheckButton* pCheckButton) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pCheckButton)) {
	if (pCheckButton!=NULL) {
		vkID = pCheckButton->vkID;
	};
};

void			lvCCheckButton::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCCheckButton(this));
};

const	char*	lvCCheckButton::GetThisElementView(const char* LocalName){
	Descr = "CheckButton()";
	return	Descr.str;
};

int				lvCCheckButton::GetValue(int time){
	return CheckButton_lua(VK_ESCAPE);
};
int CheckButton_lua(int vkid){
	if(GetKeyState(vkid)&0x8000){
		return 1;
	};
	return 0;				
};
// lvCIsBrigade //////////////////////////////////////////////////////////
lvCIsBrigade::lvCIsBrigade(lvCIsBrigade* pIsBrigade) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pIsBrigade)) {
	if (pIsBrigade!=NULL) {
		vgGrpID = pIsBrigade->vgGrpID;
	};
};

void			lvCIsBrigade::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCIsBrigade(this));
};

const	char*	lvCIsBrigade::GetThisElementView(const char* LocalName){
	Descr = "IsBrigade(";
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vgGrpID);
	if (pGrp!=NULL) {
		Descr += pGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	return Descr.str;
};

int				lvCIsBrigade::GetValue(int time){
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vgGrpID);
	if (pGrp!=NULL) {
		if (pGrp->GetBrigadeList(checkKOM)) {
			return 1;
		};
	};
	return 0;
};

// lvCGetFormationType ///////////////////////////////////////////////////
lvCGetFormationType::lvCGetFormationType(lvCGetFormationType* pGetFormationType) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetFormationType)) {
	if (pGetFormationType!=NULL) {
		vgGrpID = pGetFormationType->vgGrpID;
	};
};

void			lvCGetFormationType::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetFormationType(this));
};

const	char*	lvCGetFormationType::GetThisElementView(const char* LocalName){
	Descr = "GetFormationType(";
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vgGrpID);
	if (pGrp!=NULL) {
		Descr += pGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	return Descr.str;
};

int				lvCGetFormationType::GetValue(int time){
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vgGrpID);
	return GetFormationType_lua(pvGrp);
};
int GetFormationType_lua(lvCGroup* pGrp){
	if (pGrp!=NULL) {
		LinearArray<int,_int> ListBR;
		int NI = pGrp->GetNation();
		pGrp->GetBrigadeList(false,&ListBR);
		if (0<=NI && NI<8){
			if (ListBR.GetAmount()>0) {
				Brigade* pBR=&CITY[NI].Brigs[ListBR[0]];
				if (pBR!=NULL) {
					return pBR->GetFormIndex();
				};	
			};
		};
	};
	return -1;
};
// lvCGrpInNodeFree //////////////////////////////////////////////////////
lvCPrOfSquadInNode::lvCPrOfSquadInNode(lvCPrOfSquadInNode* pPrOfSquadInNode) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pPrOfSquadInNode)) {
	if (pPrOfSquadInNode!=NULL) {
		rate = pPrOfSquadInNode->rate;
	};
};

void			lvCPrOfSquadInNode::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCPrOfSquadInNode(this));
};

const char*		lvCPrOfSquadInNode::GetThisElementView(const char* LocalName){
	Descr="";
	Descr += "PrOfSquadInNode (rate=";
	Descr += rate;
	Descr += ")";
	return Descr.str;
};
int				lvCPrOfSquadInNode::GetValue(int time){
	int N=0;
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(squardID);
	/*
	int TN = 0;
	if (pvGrp!=NULL) TN = pvGrp->GetTotalAmount();
	lvBE_GroupInNode(squardID,x1,y1,use_vGroup,&N);
	if (TN!=0&&((float)N/(float)TN)*100.f>=rate){
		return 1;
	};
	*/
	if (pvGrp!=NULL) {
		int xc=0,yc=0;
		pvGrp->GetGroupCenter(xc,yc);
		if (Norma(xc-x1,yc-y1)<=rate) {
			return 1;
		};
	};
	return 0;
};
//======================================================================//
//=================	 OPERATION FOR TRANSPORT	 =======================//
//======================================================================//
// lvCGetNInsode /////////////////////////////////////////////////////////
lvCGetNInside::lvCGetNInside(lvCGetNInside* pGetNInside) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetNInside)) {
	if (pGetNInside!=NULL) {
		Max = pGetNInside->Max;
		vGrpID = pGetNInside->vGrpID;
	};
};

const	char*	lvCGetNInside::GetThisElementView(const char* LocalName){
	if (Max)	Descr = "GetMaxNInside(";
	else		Descr = "GetNInside(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL) Descr += pvGrp->NAME.str;
	else			 Descr += "NoGrp";
	Descr += ")";
	return Descr.str;
};

int				lvCGetNInside::GetValue(int time){
	int retN=0;
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL)	retN += pvGrp->GetNInside(Max);
	return retN;
};

void			lvCGetNInside::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetNInside(this));
};

// lvCCheckLeaveAbility //////////////////////////////////////////////////
lvCCheckLeaveAbility::lvCCheckLeaveAbility(lvCCheckLeaveAbility* pCheckLeaveAbility) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pCheckLeaveAbility)) {
	if (pCheckLeaveAbility!=NULL) {
		vGrpID = pCheckLeaveAbility->vGrpID;
	};
};

const	char*	lvCCheckLeaveAbility::GetThisElementView(const char* LocalName){
	Descr = "Landing(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL)	Descr += pvGrp->NAME.str;
	else				Descr += "NoGrp";
	Descr += ")";
	return Descr.str;
};

int				lvCCheckLeaveAbility::GetValue(int time){
	bool retB=false;
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpID);
	if (pvGrp!=NULL)	retB = pvGrp->GetLeaveAbility();
	return retB;
};

void			lvCCheckLeaveAbility::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCCheckLeaveAbility(this));
};

// lvCLoadingCoplite /////////////////////////////////////////////////////
lvCLoadingCoplite::lvCLoadingCoplite(lvCLoadingCoplite* pLoadingCoplite){
	lvCLoadingCoplite::lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pLoadingCoplite)); 
	if (pLoadingCoplite!=NULL) {
		vGrpTransport = pLoadingCoplite->vGrpTransport;
	};
};
const	char*	lvCLoadingCoplite::GetThisElementView(const char* LocalName){
	Descr = "LoadingComplite(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpTransport);
	if (pvGrp!=NULL)	Descr += pvGrp->NAME.str;
	else				Descr += "NoGrp";
	Descr += ")";
	return Descr.str;
};
int				lvCLoadingCoplite::GetValue(int time){
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrpTransport);
	return LoadingCoplite_lua(pvGrp);
};
void			lvCLoadingCoplite::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCLoadingCoplite(this));
};
int LoadingCoplite_lua(lvCGroup* pvGrp){
	int NMovers=0;
	if (pvGrp!=NULL) {
		OneObject* pOB=NULL;
		int N=pvGrp->GetTotalAmount();
		while (N--) {
			pOB = pvGrp->GetOneObj_lID(N);
			if (pOB!=NULL) {
				NMovers += GetAmountOfMoversTo(pOB);
			};
		};
	};
	return (NMovers==0);
};
// lvCGetNofNewUnitInGrp /////////////////////////////////////////////////
lvCGetNofNewUnitInGrp::lvCGetNofNewUnitInGrp(lvCGetNofNewUnitInGrp* pGetNofNewUnitInGrp) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetNofNewUnitInGrp)){
	if (pGetNofNewUnitInGrp!=NULL) {
		vGrp = pGetNofNewUnitInGrp->vGrp;
	};
};
const	char*	lvCGetNofNewUnitInGrp::GetThisElementView(const char* LocalName){
	Descr = "GetNofNewUnitInGrp(";
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL&&pvGrp->NAME.str!=NULL) {
		Descr += pvGrp->NAME.str;
	}else{
		Descr += "NoGroup";
	};
	Descr += ")";
	
	return	Descr.str;
};
int				lvCGetNofNewUnitInGrp::GetValue(int time){
	lvCGroup* pvGrp = GroupsMap()->GetGroupID(vGrp);
	if (pvGrp!=NULL) {
		return pvGrp->GetAmountOfNewUnits(ClearNew);
	};
	return 0;
};
void			lvCGetNofNewUnitInGrp::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetNofNewUnitInGrp(this));
};
// lvCGetNofMyVillage ////////////////////////////////////////////////////
lvCGetNofMyVillage::lvCGetNofMyVillage(lvCGetNofMyVillage* pGetNofMyVillage) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetNofMyVillage)) {
	if (pGetNofMyVillage!=NULL)	{
		Owner = pGetNofMyVillage->Owner;
		ResType = pGetNofMyVillage->ResType;
	};
};
const	char*	lvCGetNofMyVillage::GetThisElementView(const char* LocalName){
	Descr = "GetNofMyVillage(Nation[";
	Descr += Owner;
	Descr += "],";
	Enumerator* E=ENUM.Get("RESTYPE");
	Descr += E->GetStr(ResType);
	Descr += ")";
	return Descr.str;
};
int				lvCGetNofMyVillage::GetValue(int time){
	return vdf_GetAmountOfSettlements(Owner,ResType,false,0);
};
void			lvCGetNofMyVillage::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetNofMyVillage(this));
};
// lvCCheckRBBP //////////////////////////////////////////////////////////
lvCCheckRBBP::lvCCheckRBBP(lvCCheckRBBP* pCheckRBBP) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pCheckRBBP)) {
	if (pCheckRBBP!=NULL) {
		if (pCheckRBBP->P0.Get()!=NULL)	P0.Set(pCheckRBBP->P0.Get());
		if (pCheckRBBP->P1.Get()!=NULL)	P1.Set(pCheckRBBP->P1.Get());
		MaxR = pCheckRBBP->MaxR;
	};
};
const	char*	lvCCheckRBBP::GetThisElementView(const char* LocalName){
	Descr = "CheckRBBP(";
	vvPOINT2D* pP0 = P0.Get();
	vvPOINT2D* pP1 = P1.Get();
	if (pP0!=NULL)	Descr += pP0->GetName();
	else			Descr += "No P0";
	Descr	+= ",";
	if (pP1!=NULL)	Descr += pP1->GetName();
	else			Descr += "No P1";
	Descr += ",";
	Descr += MaxR;
	Descr += ")";
	return Descr.str;
};
int				lvCCheckRBBP::GetValue(int time){
	int NU=0;
	vvPOINT2D* pP0 = P0.Get();
	vvPOINT2D* pP1 = P1.Get();
	if (pP0!=NULL&&pP1!=NULL) {
		int x = (pP0->Value.x+pP1->Value.x)/2;
		int y = (pP0->Value.y+pP1->Value.y)/2;
		OneObject*	pUnit = NULL;
		for (int i=0; i<MAXOBJECT&&NU==0; i++){
			pUnit = Group[i];
			if (pUnit&&!pUnit->Sdoxlo&&pUnit->NewBuilding==true) {
				if (Norma(x-(pUnit->RealX>>4),y-(pUnit->RealY>>4))<=MaxR&&pUnit->NStages==pUnit->Stage) {
					NU++;
				};
			};
		};
	};
	return NU;
};
void			lvCCheckRBBP::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCCheckRBBP(this));
};
// lvCIsTired ////////////////////////////////////////////////////////////
lvCIsTired::lvCIsTired(lvCIsTired* pIsTired) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pIsTired)){
	if (pIsTired!=NULL) {
		vGrp = pIsTired->vGrp;
	};
};
const	char*	lvCIsTired::GetThisElementView(const char* LocalName){
	Descr = "IsTired(";
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL&&pGrp->NAME.str!=NULL)	Descr += pGrp->NAME.str;
	else									Descr += "NoGroup";
	Descr += ")";
	return Descr.str;
};
int				lvCIsTired::GetValue(int time){
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL&&pGrp->GetIsTired()) return 1;
	return 0;
};
void			lvCIsTired::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCIsTired(this));
};
// lvCBrigadesAmount /////////////////////////////////////////////////////
lvCBrigadesAmount::lvCBrigadesAmount(lvCBrigadesAmount* pBrigadesAmount) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pBrigadesAmount)) {
	if (pBrigadesAmount!=NULL) {
		Nat = pBrigadesAmount->Nat;
	};	
};
const	char*	lvCBrigadesAmount::GetThisElementView(const char* LocalName){
	Descr = "BrigadesAmount(";
	Descr += Nat;
	Descr += ")";
	return Descr.str;
};
int				lvCBrigadesAmount::GetValue(int time){
	return GetBrigadsAmount0(Nat);
};
void			lvCBrigadesAmount::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCBrigadesAmount(this));
};
// lvCTestFillingAbility /////////////////////////////////////////////////
lvCTestFillingAbility::lvCTestFillingAbility(lvCTestFillingAbility* pTestFillingAbility) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pTestFillingAbility)) {
	if (pTestFillingAbility!=NULL) {
		vGrp = pTestFillingAbility->vGrp;
	};
};
const	char*	lvCTestFillingAbility::GetThisElementView(const char* LocalName){
	Descr = "TestFillingAbility(";	
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL&&pGrp->NAME.str) {
		Descr += pGrp->NAME.str;
	}else{
		Descr += "NoName";
	}
	Descr += ")";
	return Descr.str;
};
int				lvCTestFillingAbility::GetValue(int time){
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	return TestFillingAbility_lua(pGrp);	
};	
void			lvCTestFillingAbility::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCTestFillingAbility(this));
};
int TestFillingAbility_lua(lvCGroup* pGrp){
	bool	CanFill=false;
	bool TestFillingAbility(OneObject* OB);
	if (pGrp!=NULL) {
		int N=pGrp->GetTotalAmount();
		while (N--&&!CanFill) {
			CanFill = TestFillingAbility( pGrp->GetOneObj_lID(N) );
		};
	};
	return CanFill;
};
// lvCInStandGround //////////////////////////////////////////////////////
lvCInStandGround::lvCInStandGround(lvCInStandGround* pInStandGround) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pInStandGround)) {
	if (pInStandGround!=NULL) {
		vGrp			=pInStandGround->vGrp;
		AllBrigades		=pInStandGround->AllBrigades;
	};
};
const	char*	lvCInStandGround::GetThisElementView(const char* LocalName){
	Descr = "InStandGround(Group[";
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL&&pGrp->NAME.str!=NULL)	Descr += pGrp->NAME.str;
	else									Descr += "NoName";
	Descr += "],TestAllBrigades[";
	Descr += AllBrigades;
	Descr += "])";
	return Descr.str;
};
int				lvCInStandGround::GetValue(int time){
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	return CInStandGround_lua(pGrp,AllBrigades);
};
void			lvCInStandGround::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCInStandGround(this));
};
int CInStandGround_lua(lvCGroup* pGrp,int AllBrigades){
	if (pGrp!=NULL) {
		int inSG_N	=0;
		int NB		=0;
		LinearArray<int,_int> BrigList;
		pGrp->GetBrigadeList(false,&BrigList);
		NB=BrigList.GetAmount();
		if (NB>0){
			Brigade* pBR=NULL;
			int Nat=pGrp->GetNation();
			for (int i=0; i<NB; i++){
				pBR=&CITY[Nat].Brigs[ BrigList[i] ];
				if (pBR!=NULL&&pBR->InStandGround==true) inSG_N++;
			};
		};
		if (AllBrigades==true&&(inSG_N>0)&&inSG_N==NB) return 1;
		if (AllBrigades==false&&inSG_N>0)			   return 1;
	};
	return 0;
};
// lvCVillageOwner ///////////////////////////////////////////////////////
lvCVillageOwner::lvCVillageOwner(lvCVillageOwner* pVillageOwner) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pVillageOwner)){
	if (pVillageOwner!=NULL) {
		if (pVillageOwner->VillageName.str!=NULL)	VillageName=pVillageOwner->VillageName.str;
	};
};
const	char*	lvCVillageOwner::GetThisElementView(const char* LocalName){
	Descr = "GetVillageOwner( ";
	if (VillageName.str!=NULL)	Descr += VillageName.str;
	else						Descr += "NoName";
	Descr += " )";
	return Descr.str;
};
int				lvCVillageOwner::GetValue(int time){
	return VillageOwner_lua(VillageName.str);
};
void			lvCVillageOwner::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCVillageOwner(this));
};
int VillageOwner_lua(const char* vilname){
	_str	ttt; ttt = vilname;
	int owner = -1;
	if (ttt.str!=NULL){
		owner = (int)( GetTribeOwner(ttt.str) );	
	};
	return owner;
};
// lvCGetNofBrigInNode ///////////////////////////////////////////////////
lvCGetNofBrigInNode::lvCGetNofBrigInNode(lvCGetNofBrigInNode* pGetNofBrigInNode) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetNofBrigInNode)){
	if (pGetNofBrigInNode!=NULL) {
		Nat = pGetNofBrigInNode->Nat;
	};
};
const	char*	lvCGetNofBrigInNode::GetThisElementView(const char* LocalName){
	Descr = "GetNofBrigInNode(Nation[";
	Descr += Nat;
	Descr += "],Node[";
	lvCNode* pNode = NodesMap()->vGetNode(parNode);
	if (UseNode&&pNode!=NULL) Descr += pNode->vGetName();
	else					  Descr += "NoNode";
	Descr += "])";
	return Descr.str;
};
int				lvCGetNofBrigInNode::GetValue(int time){
	lvCNode* pNode = NodesMap()->vGetNode(parNode);
	if (UseNode&&pNode!=NULL) {
		return GetNofBrigInNode_lua(Nat,pNode->vGetX(),pNode->vGetY(),pNode->vGetR());
	};
	return 0;
};
void			lvCGetNofBrigInNode::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetNofBrigInNode(this));
};
int GetNofBrigInNode_lua(int nat,int x,int y,int R){
	lvSSumSquad	Ret;	
	Ret.Nat = nat;
	PerformActionOverUnitsInRadius(	x,y,R,AddBrIDifPresent,&Ret );
	return Ret.getAmount();
};
// lvCGetCurGrpORDER /////////////////////////////////////////////////////
lvCGetCurGrpORDER::lvCGetCurGrpORDER(lvCGetCurGrpORDER* pGetCurGrpORDER) : lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetCurGrpORDER)){
	if (pGetCurGrpORDER!=NULL) {
		vGrp = pGetCurGrpORDER->vGrp;
	};
};
const	char*	lvCGetCurGrpORDER::GetThisElementView(const char* LocalName){
	Descr = "GetCurGrpORDER(";
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL&&pGrp->NAME.str!=NULL)	Descr += pGrp->NAME.str;
	else									Descr += "NoName";
	Descr += ")";
	return Descr.str;
};
int				lvCGetCurGrpORDER::GetValue(int time){
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL) {
		return (int)(pGrp->GetORDER());
	};
	return 0;
};
void			lvCGetCurGrpORDER::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetCurGrpORDER(this));
};
// lvCGetNofBRLoadedGun //////////////////////////////////////////////////
lvCGetNofBRLoadedGun::lvCGetNofBRLoadedGun(lvCGetNofBRLoadedGun* pGetNofBRLoadedGun): lvCBaseFunction(dynamic_cast<lvCBaseFunction*>(pGetNofBRLoadedGun)){
	if (pGetNofBRLoadedGun!=NULL) {
		vGrp = pGetNofBRLoadedGun->vGrp;
	}
};
const	char*	lvCGetNofBRLoadedGun::GetThisElementView(const char* LocalName){
	Descr = "GetNofBRLoadedGun(";
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL&&pGrp->NAME.str!=NULL)	Descr += pGrp->NAME.str;
	else									Descr += "NoName";
	Descr += ")";
	return Descr.str;
};
int				lvCGetNofBRLoadedGun::GetValue(int time){
	lvCGroup* pGrp = GroupsMap()->GetGroupID(vGrp);
	if (pGrp!=NULL) {
		return (int)(pGrp->GetNofBRLoadedGun());
	};
	return 0;
};
void			lvCGetNofBRLoadedGun::GetCopy(lvCBaseFunction** pCopy){
	*pCopy = dynamic_cast<lvCBaseFunction*>(new lvCGetNofBRLoadedGun(this));
};
//////////////////////////////////////////////////////////////////////////
void REG_BE_FUNCTIONS_class(){
	REG_CLASS(COPERCOND_CALLDESCR);
	REG_CLASS(CPRO_MISS_FILER);

	REG_CLASS(lvTypeNum);
	REG_CLASS(lvGrpNumBld);
	
	REG_CLASS(lvCBaseOperCond);

	REG_CLASS(lvCBaseFunction);
	REG_CLASS(lvCOperation);

	REG_CLASS(ClassRef<lvCTeraforming>);

	//==========// OPERATION //==========//

	REG_CLASS_EX(lvCGroupSendTo,				"GROUP");			// +++ Node
	REG_CLASS_EX(lvCSendToPosition,				"GROUP");
	REG_CLASS_EX(lvCDisband,					"GROUP");
	REG_CLASS_EX(lvCTeleport,					"GROUP");			// +++ Node
	REG_CLASS_EX(lvCRotateGroup,				"GROUP");
	REG_CLASS_EX(lvCSetUnitState,				"GROUP");
	REG_CLASS_EX(lvCBrigReformation,			"GROUP");
	REG_CLASS_EX(lvCChangeNationG,				"GROUP");
	REG_CLASS_EX(lvCPutNewSquad,				"GROUP");			// +++ Node
	REG_CLASS_EX(lvCPutNewFormation,			"GROUP");			// +++ Node
	REG_CLASS_EX(lvCTakeFood,					"GROUP");
	REG_CLASS_EX(lvCTakeWood,					"GROUP");
	REG_CLASS_EX(lvCTakeStone,					"GROUP");
	REG_CLASS_EX(lvCKillNUnits,					"GROUP");
	REG_CLASS_EX(lvCEraseNUnits,				"GROUP");
	REG_CLASS_EX(lvCSpotGrpByUType,				"GROUP");
	REG_CLASS_EX(lvCRemoveNUnitsTo,				"GROUP");
	REG_CLASS_EX(lvCEqualizeSpeed,				"GROUP");
	REG_CLASS_EX(lvCCreateBrigade,				"GROUP");
	REG_CLASS_EX(lvCSavePosition,				"GROUP");
	REG_CLASS_EX(lvCSelectUnits,				"GROUP");
	REG_CLASS_EX(lvCSelectAll,					"GROUP");
	REG_CLASS_EX(lvCSelSendTo,					"GROUP");
	REG_CLASS_EX(lvCSetSerchWFlag,				"GROUP");
	REG_CLASS_EX(lvCClearOrders,				"GROUP");
	REG_CLASS_EX(lvCGroupMovement,				"GROUP");

	REG_CLASS_EX(lvCSetValue,					"GLOBAL_VALUE");
	REG_CLASS_EX(lvCAddToInt,					"GLOBAL_VALUE");
	REG_CLASS_EX(lvCAddToIntEx,					"GLOBAL_VALUE");

	REG_CLASS_EX(lvCSetFriends,					"NATION");
	REG_CLASS_EX(lvCChangeFriends,				"NATION");
	REG_CLASS_EX(lvCSetRessource,				"NATION");
	REG_CLASS_EX(lvCAddRessource,				"NATION");
	REG_CLASS_EX(lvCStartAIEx,					"NATION");
	REG_CLASS_EX(lvCSetAIEnableState,			"NATION");
	REG_CLASS_EX(lvCSetUnitEnableState,			"NATION");
	REG_CLASS_EX(lvCSetUpgradeEnableStatus,		"NATION");
	REG_CLASS_EX(lvCSetMyNation,				"NATION");
	REG_CLASS_EX(lvCChangeAS,					"NATION");
	REG_CLASS_EX(lvCKillNatinZone,				"NATION");
	REG_CLASS_EX(lvCKillNatNear,				"NATION");
	REG_CLASS_EX(lvCSetUpgradeDone,				"NATION");
	
	REG_CLASS_EX(lvCRunTimer,					"SYSTEM");
	REG_CLASS_EX(lvCSetScrollLimit,				"SYSTEM");
	REG_CLASS_EX(lvCShowVictory,				"SYSTEM");
	REG_CLASS_EX(lvCLooseGame,					"SYSTEM");
	REG_CLASS_EX(lvCSetGameSpeed,				"SYSTEM");
	REG_CLASS_EX(lvCGetGameSpeed,				"SYSTEM");
	REG_CLASS_EX(lvCSetLMode,					"SYSTEM");
	REG_CLASS_EX(lvCSetFogMode,					"SYSTEM");
	REG_CLASS_EX(lvCSetGameMode,				"SYSTEM");
	REG_CLASS_EX(lvCPAUSE,						"SYSTEM");
	REG_CLASS_EX(lvCSetSilence,					"SYSTEM");
	
	REG_CLASS_EX(lvCSetFGV,						"GRAPHIC");
	REG_CLASS_EX(lvCShowPanel,					"GRAPHIC");

	REG_CLASS_EX(lvCPlayOGMiss,					"SOUND");
	REG_CLASS_EX(lvCStopOGMiss,					"SOUND");
	REG_CLASS_EX(lvCOGSetVolume,				"SOUND");
	REG_CLASS_EX(lvCOGFinishMiss,				"SOUND");

	REG_CLASS_EX(lvCSendToNode,					"NODE_GROUP");
	REG_CLASS_EX(lvCRotate,						"NODE_GROUP");
	REG_CLASS_EX(lvCReformation,				"NODE_GROUP");
	REG_CLASS_EX(lvCSetState,					"NODE_GROUP");
	REG_CLASS_EX(lvCChangeNation,				"NODE_GROUP");
	REG_CLASS_EX(lvCSelSendToNode,				"NODE_GROUP");
	REG_CLASS_EX(lvCGroupSendToNode,			"NODE_GROUP");
    
	REG_CLASS_EX(lvCPushNUnitAway,				"TRANSPORT");
	REG_CLASS_EX(lvCSendUnitsToTransport,		"TRANSPORT");

	REG_CLASS_EX(lvCSetCamera,					"CAMERA");
	REG_CLASS_EX(lvCMoveCamera,					"CAMERA");
	REG_CLASS_EX(lvCAttachCameraToGroup,		"CAMERA");
	REG_CLASS_EX(lvCFreeCamera,					"CAMERA");
	REG_CLASS_EX(lvCSetStartPoint,				"CAMERA");
	REG_CLASS_EX(lvCSaveScreenPos,				"CAMERA");

	REG_CLASS_EX(lvCSetLeftPort,				"MOVIE");
	REG_CLASS_EX(lvCSetRightPort,				"MOVIE");
	REG_CLASS_EX(lvCPlayText,					"MOVIE");
	REG_CLASS_EX(lvCSetText,					"MOVIE");
	REG_CLASS_EX(lvCSetActivFrame,				"MOVIE");
	REG_CLASS_EX(lvCShowDialog,					"MOVIE");
	REG_CLASS_EX(lvCAddTextToDlg,				"MOVIE");
	REG_CLASS_EX(lvCClearDialog,				"MOVIE");
	REG_CLASS_EX(lvCSetMessageState,			"MOVIE");
	REG_CLASS_EX(lvCFilmCopliteState,			"MOVIE");

	REG_CLASS_EX(lvCFreezeGame,					"FREEZE");
	REG_CLASS_EX(lvCUnFreezeGame,				"FREEZE");
	REG_CLASS_EX(lvCFreezeAndHidden,			"FREEZE");
	REG_CLASS_EX(lvCUnFreezeAndUnHidden,		"FREEZE");
	REG_CLASS_EX(lvCFreezeAndHiddenGame,		"FREEZE");
	REG_CLASS_EX(lvCUnFreezeAndUnHiddenGame,	"FREEZE");
	REG_CLASS_EX(lvCUnFreezeGroup,				"FREEZE");
	REG_CLASS_EX(lvCUnFreezeAndUnHiddenGroup,	"FREEZE");
	
	REG_CLASS_EX(lvCSetUnitStateCII,			"COSSAKSII");
	REG_CLASS_EX(lvCSendStikiToZone,			"COSSAKSII");
	REG_CLASS_EX(lvCScare,						"COSSAKSII");
	REG_CLASS_EX(lvCClearSG,					"COSSAKSII");
	REG_CLASS_EX(lvCShowMessageII,				"COSSAKSII");
	REG_CLASS_EX(lvCBrigPanelSet,				"COSSAKSII");
	REG_CLASS_EX(lvCGroupHoldNode,				"COSSAKSII");
	REG_CLASS_EX(lvCUnloadSquad,				"COSSAKSII");
	REG_CLASS_EX(lvCAddElemTHE_CII,				"COSSAKSII");
	REG_CLASS_EX(lvCDelElemTHE_CII,				"COSSAKSII");
	REG_CLASS_EX(lvCSET_MISS_MANAGER,			"COSSAKSII");
	REG_CLASS_EX(lvCSetTired,					"COSSAKSII");
    
	REG_CLASS_EX(lvCAddWallSegment,				"SPECIAL");
	REG_CLASS_EX(lvCSetLightSpot,				"SPECIAL");
	REG_CLASS_EX(lvCClearLightSpot,				"SPECIAL");
	REG_CLASS_EX(lvCSpotNUnits,					"SPECIAL");		// +++ Node
	REG_CLASS_EX(lvCGoInBattle,					"SPECIAL");
	REG_CLASS_EX(lvCArtAttack,					"SPECIAL");
	REG_CLASS_EX(lvCApplyTerafoming,			"SPECIAL");
	REG_CLASS_EX(lvCAddFarms,					"SPECIAL");
	REG_CLASS_EX(lvCQuestData,					"SPECIAL");
	REG_CLASS_EX(lvCReStartSquadShema,			"SPECIAL");
	REG_CLASS_EX(lvCArtChangeCharge,			"SPECIAL");
	REG_CLASS_EX(lvCArtAttackPoint,				"SPECIAL");
	REG_CLASS_EX(lvCClearDead,					"SPECIAL");
		
	//==========// CONDITION //==========//

	REG_CLASS_EX(lvCGetUnitsAmount1,	"F_GROUP");				// +++ Node			// +++ __LAU__
	REG_CLASS_EX(lvCGetUnitsAmount3,	"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCGetTotalAmount0,	"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCGetTotalAmount2,	"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCChekPosition,		"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCAllGrpInNode,		"F_GROUP");									// --- __LAU__
	REG_CLASS_EX(lvCAllGrpInNodeFree,	"F_GROUP");									// --- __LAU__
	REG_CLASS_EX(lvCGetFormationType,	"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCGetUnitState,		"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCIsBrigade,			"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCGetNofNewUnitInGrp,	"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCIsTired,			"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCTestFillingAbility,	"F_GROUP");									// +++ __LAU__
	REG_CLASS_EX(lvCInStandGround,		"F_GROUP");									// +++ __LAU__

	REG_CLASS_EX(lvCGetValue,			"F_GLOBAL_VALUE");							// +++ __LAU__
	REG_CLASS_EX(lvCBool,				"F_GLOBAL_VALUE");							// +++ __LAU__						
	REG_CLASS_EX(lvCInt,				"F_GLOBAL_VALUE");							// +++ __LAU__
	REG_CLASS_EX(lvCTrigg,				"F_GLOBAL_VALUE");							// +++ __LAU__
		
	REG_CLASS_EX(lvCGetAmount,			"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCGetUnitsAmount0,	"F_NATION");			// +++ Node			// +++ __LAU__
	REG_CLASS_EX(lvCGetUnitsAmount2,	"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCGetTotalAmount1,	"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCGetReadyAmount,		"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCGetResource,		"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCNationIsErased,		"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCGetNofMyVillage,	"F_NATION");								// +++ __LAU__				
	REG_CLASS_EX(lvCBrigadesAmount,		"F_NATION");								// +++ __LAU__
	REG_CLASS_EX(lvCGetNofBrigInNode,	"F_NATION");								// +++ __LAU__

	REG_CLASS_EX(lvCTimerDone,			"F_SYSTEM");								// +++ __LAU__
	REG_CLASS_EX(lvCCheckButton,		"F_SYSTEM");								// +++ __LAU__
	REG_CLASS_EX(lvCCameraSTOP,			"F_SYSTEM");								// +++ __LAU__
	REG_CLASS_EX(lvCGetDiff,			"F_SYSTEM");								// +++ __LAU__
	REG_CLASS_EX(lvCGetScreenXY,		"F_SYSTEM");								// --- __LAU__
	REG_CLASS_EX(lvCGetLMode,			"F_SYSTEM");								// +++ __LAU__
	REG_CLASS_EX(lvCProbably,			"F_SYSTEM");								// --- __LAU__

	REG_CLASS_EX(FG_Visible,			"F_GRAPHIC");								// +++ __LAU__
	REG_CLASS_EX(FG_InVisible,			"F_GRAPHIC");								// +++ __LAU__

	REG_CLASS_EX(ogSTOP,				"F_SOUND");									// +++ __LAU__

	REG_CLASS_EX(lvCGrpInNode,			"F_NODE_GROUP");							// --- __LAU__
	REG_CLASS_EX(lvCGrpInNodeFree,		"F_NODE_GROUP");							// --- __LAU__
	REG_CLASS_EX(lvCPrOfSquadInNode,	"F_NODE_GROUP");							// --- __LAU__
	REG_CLASS_EX(lvCChkTime,			"F_NODE_GROUP");							// --- __LAU__

	REG_CLASS_EX(lvCGetNInside,			"F_TRANSPORT");								// +++ __LAU__
	REG_CLASS_EX(lvCCheckLeaveAbility,	"F_TRANSPORT");								// +++ __LAU__
	REG_CLASS_EX(lvCLoadingCoplite,		"F_TRANSPORT");								// +++ __LAU__

	REG_CLASS_EX(lvCCheckRBBP,			"F_SPECIAL");								// --- __LAU__
	REG_CLASS_EX(lvCVillageOwner,		"F_SPECIAL");								// +++ __LAU__
	REG_CLASS_EX(lvCGetCurGrpORDER,		"F_SPECIAL");								// +++ __LAU__
	REG_CLASS_EX(lvCGetNofBRLoadedGun,	"F_SPECIAL");								// +++ __LAU__
	
};
























