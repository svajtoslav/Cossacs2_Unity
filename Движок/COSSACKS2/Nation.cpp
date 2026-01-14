#include "stdheader.h"
#include "UnitAbility.h"
void AssignHint1(char* s,int time);
int NMyUnits;
int NThemUnits;
extern MotionField UnitsField;
extern int tmtmt;
extern int HREQRADIUS;
extern word FlyMops[256][256];
extern byte* NPresence;
extern City CITY[8];
void ShowFog(OneObject* ZZ);
//byte CreateExObjR(Weapon* Wep,short x,short y,
//				 short dx,short dy,short v,byte Mask,OneObject* Send,word Dist);
//byte CreateExObjDPointR(Weapon* Wep,short x,short y,
//				 short dx,short dy,short v,byte Mask,OneObject* Send,byte dsx,byte dsy,word Range);
word MAXOBJECT;
//+¸ûø ÿþ¸ªºÿðõª ÿ¨øúð÷ º¨þòý <16 ø ªòð¨¹, úþªþ¨º¦
//ýºöýþ ðªðúþòðª¹ ¸ªþøª ôðû¹°õ ¤ªþóþ ¨ð¸¸ªþ ýø , ªþ
//ÿ¨øúð÷ ýõ òvÿþûý õª¸ 
static MaxReplyDistance=50;

int PathAsks;
const int drr[9]={7,6,5,0,0,4,1,2,3};
const byte drrb[9]={7,6,5,0,0,4,1,2,3};
const idrx[8]={0,1,1,1,0,-1,-1,-1};
const idry[8]={-1,-1,0,1,1,1,0,-1};
//¦ð¸¸øò ôû  þÿªøüø÷ð¡øø ÿºªø
static int OptA[256][256];
static SIndex;
int counter;
static byte xi[256][256];
static byte yi[256][256];
byte RNTB[64][256];
//HUGE ARRAY!!!!!!!!!!!!
OneObject OBJECTS[ULIMIT];
//+¨óðýø÷ð¡ø  þ¢õ¨õôø ýð òvÿþûýõýøõ ôþûóþóþ ÿ¨øúð÷ð
word ComStc[StSize];
word StHead;
word StTile;
void MCHK(){
/*	if(int(Group[150]->LocalOrder)){
		if(Group[150]->LocalOrder->OrderType==2&&
			Group[150]->LocalOrder->info.MoveToXY.y==0){
			int uuu=0;
			return;
		};
	}*/
};
void InitStack(){
	StHead=0;
	StTile=0;
};
bool CheckAttAbility(OneObject* OB,word Patient){
	if(OB&&Patient!=0xFFFF){
		OneObject* EN=Group[Patient];
		if(!EN)return false;
		GeneralObject* EGO=EN->Ref.General;
		if(OB->newMons->Priest){
			if(!EN->NMask&OB->NMask)return false;
			if(EN->Life>=EN->MaxLife){
				if(EN->BrigadeID==0xFFFF)return false;
				Brigade* BR=CITY[EN->NNUM].Brigs+EN->BrigadeID;
				if(BR->Morale>=BR->MaxMorale)return false;
			}
		}else{
			if(OB->newMons->Shaman){
				if(EN->Life>=EN->MaxLife){
					if(EN->BrigadeID==0xFFFF)return false;
					Brigade* BR=CITY[EN->NNUM].Brigs+EN->BrigadeID;
					if(BR->Morale>=BR->MaxMorale)return false;
				}
			}else
			if(EN->NMask&OB->NMask)return false;
		};
		GeneralObject* GO=OB->Ref.General;
		return true;
	}else{
		return false;
	};

};
word PopCmd(){
	return 0xFFFF;
};
void PushCmd(word Cmd){
	
};
void CarryOutOrder(){
	
};
RLCTable MImage[maximage];
RLCTable miniMImage[maximage];
int MaxImage;
void Err(LPCSTR s)
{
	MessageBox(hwnd,s,"Nation loading failed...",MB_ICONWARNING|MB_OK);
};
void LoadMonsters(){
	/*
	GFILE* f=Gopen("monsters.lst","r");
	char c[64];
	char cc[64];
	for(int i=0;!feof(f);i++){
		Gscanf(f,"%s",c);
		strcpy(cc,c);
		strcat(c,".rlc");
		strcat(cc,".rlx");
		if(c[0]!=0)LoadRLC(c,&MImage[i]);
		if(cc[0]!=0)LoadRLC(cc,&miniMImage[i]);
	};
	ADDSUMM(MImage,1024);
	
	MaxImage=i;
	for(byte j=0;j<64;j++){
		byte q=0;
		for(int t=0;t<256;t++){
			RNTB[j][t]=q;
			q++;
			if(q>j)q=0;
		};
	};*/
};

//¦ðó¨º÷úð ýð¡øø ø÷ ¯ðùûð
void Nation::CreateNation(byte Mask,byte NI){
	char tmp[32];
	NMask=Mask;
	CITY=NULL;
	NNUM=NI;
	NGidot=0;
	NFarms=0;
	CasheSize=0;
	PathAsks=0;
};
word LastObject;
//Tþ÷ôðª¹ ÿð÷õüývù þñ·õúª ªøÿð üþý¸ø¨ð ¸ úþþ¨ôøýðªðüø (x,y)
//ø ýþüõ¨þü n ò ªðñûø¡õ üþý¸ª¨þò ôðýýþù ýð¡øø

//¦ðó¨º÷øª¹ ðýøüð¡ø¦ ¸ ôðýývüø ÿþúð÷ðªõû üø ò þôøý ø÷ 5-ªø òýºª¨.
//¨õóø¸ª¨þò 

void OneObject::FreeAsmLink(){
	
};
//+¸ýþòývõ ¸ª¨ºúªº¨v ôû  þñ¨ðñþªúø ÿõ¨õüõ•õýøù
//÷ðÿ¨þ¸ ýð ÿõ¨õüõ•õýøõ
struct AskMove{
	word ReqID;//øýôõú¸ ÿõ¨õüõ•ðõüþóþ
	word PreID;//øýôõú¸ öðöôº•õóþ ôþ¨òðª¹¸  ò ¤ªº úûõªúº
	byte x;
	byte y;
	char dx;
	char dy;
};
int NAsk;//¦þûø¢õ¸ªòþ ÷ðÿ¨þ¸þò
AskMove Ask[8192];//¦ð¸¸øò ÷ðÿ¨þ¸þò
bool FailLink[8192];//¦ð¸¸øò ýõ¨ð÷¨õ°õýýv¿ ÿõ¨õüõ•õýøù
word CurInd;
word IDMap[256][256];
word RQMap[256][256];//:3-÷ðÿõ•õýýþõ ýðÿ¨ðòûõýøõ ..
					 //:13-ýþüõ¨ ò ªðñûø¡õ ÷ðÿ¨þ¸þò
//-þñðòøª¹ ÷ðÿ¨þ¸ ò ¸ø¸ªõüº ÷ðÿ¨þ¸þò ýð ÿõ¨õüõ•õýøõ
void AddAsk(word ReqID,byte x,byte y,char zdx,char zdy){
	//DEBUGGING
	OneObject* OB=Group[ReqID];
#ifdef DEBLEVEL1
	//assert(OB->x==x&&OB->y==y);
	//assert(!OB->AskMade);
	//assert((!OB->OnWater)&&abs(zdx)<2&&abs(zdy)<2);
	//if(OB->OnWater) assert(abs(zdx)<3&&abs(zdy)<3);
	//else assert(abs(zdx)<2&&abs(zdy)<2);
#endif
	OB->AskMade=true;
	//ENDDEBUG
	__asm{
//		inc		NAsk
		mov		eax,NAsk
		shl		eax,3
		add		eax,offset Ask
		mov		bx,ReqID
		mov		word ptr [eax],bx
		mov		word ptr [eax+2],0FFFFh
		mov		bl,x
		mov		bh,y
		mov		word ptr [eax+4],bx
		mov		bl,zdx
		mov		bh,zdy
		mov		word ptr [eax+6],bx
		inc		NAsk
	};
};
//+ñ¨ðñþªúð ÷ðÿ¨þ¸þò, ¸ýð¢ðûð þ¸òþñþöôðõü ò¸õ ¸ªð¨võ úûõªúø
//ÿþªþü ÷ðÿþûý õü ø ÿ¨þòõ¨ õü

void PrepareProcessing(){
	NAsk=0;
	//Only for DEBUGGIONG 
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB)OB->AskMade=false;
	};
};
inline void Spot(int x,int y){
	/*
	if(x>0&&y>0&&x<msx&&y<msy){
		__asm{
			xor		eax,eax
			mov		al,byte ptr x
			mov		ah, byte ptr y
			shl		eax,1
			mov		word ptr[fmap+eax],16383;
		};
	};
	*/
};
//Inline- úþüðýôv ÿþ¸ª¨þõýø  òýºª¨õýýø¿ úþüðýô
int Ofst;
char* NowBuf;
inline void ChkOfst(int size){
	
};
inline void cmSetXY(char x,char y){
	ChkOfst(3);
	//assert(abs(x)<2&&abs(y)<2);
	NowBuf[Ofst]=4;
	NowBuf[Ofst+1]=x;
	NowBuf[Ofst+2]=y;
	Ofst+=3;
};
inline void cmSetXYDir(byte x,byte y,byte dir,byte n){
	ChkOfst(4);
	NowBuf[Ofst]=18;
	NowBuf[Ofst+1]=x;
	NowBuf[Ofst+2]=y;
	NowBuf[Ofst+3]=(dir&7)|(n<<4);
	Ofst+=4;
};
inline void cmSetXYDirX(byte x,byte y,char dx,char dy,byte n){
	ChkOfst(4);
	//assert(dx<2&&dy<2);
	byte dr=drr[(dx+1)*3+dy+1];
	NowBuf[Ofst]=18;
	NowBuf[Ofst+1]=x;
	NowBuf[Ofst+2]=y;
	NowBuf[Ofst+3]=(dr&7)|(n<<4);
	Ofst+=4;
};

inline void cmChkXY(byte x,byte y){
	ChkOfst(3);
	NowBuf[Ofst]=44;
	NowBuf[Ofst+1]=x;
	NowBuf[Ofst+2]=y;
	Ofst+=3;
};
inline void cmSetDir(int dx,int dy){
	if(dx==0&&dy==0)return;
	byte dr=drr[(dx+1)*3+dy+1];
	ChkOfst(2);
	NowBuf[Ofst]=5;
	NowBuf[Ofst+1]=dr;
	Ofst+=2;
};
void cmSetDirD(byte dr){
	ChkOfst(2);
	NowBuf[Ofst]=5;
	NowBuf[Ofst+1]=dr&7;
	Ofst+=2;
};
inline void cmLoadAnm(byte stype,byte dtype,word kind){
	ChkOfst(5);
	NowBuf[Ofst]=6;
	NowBuf[Ofst+1]=dtype;
	NowBuf[Ofst+2]=stype;
	NowBuf[Ofst+3]=byte(kind);
	NowBuf[Ofst+4]=0;
	Ofst+=5;
};
inline void cmPerfAnm(byte n){
	ChkOfst(2);
	NowBuf[Ofst]=8;
	NowBuf[Ofst+1]=n;
	Ofst+=2;
};
inline void cmRet(){
	NowBuf[Ofst]=1;
	Ofst+=1;
};
inline void cmDone(){
	NowBuf[Ofst]=3;
	Ofst+=1;
};
//¦õ¨õüõ¸ªøª¹ þñ·õúª ò ªþ¢úº (x,y)
typedef byte xxx[64];

void SendToLink(OneObject* OBJ);
//Lªðúþòðª¹ point
void AttackPointLink(OneObject* OBJ);
void OneObject::AttackPoint(byte x,byte y,byte wep,int Prio){
	if(CheckOrderAbility())return;
	if(Prio>=16)StandGround=false;
	if(UnlimitedMotion)return;
	if(Media==2){
		//FlyAttack(OID,Prio);
		return;
	};
	if(Ref.General->NWeap<=wep)return;
	if(Prio<PrioryLevel)return;
	if(!Ready)return;
	Order1* Or1=GetOrdBlock();
	if(!int(Or1))return;
	Or1->PrioryLevel=Prio&127;
	Or1->NextOrder=LocalOrder;
	Or1->OrderType=33;//Lªðúð
	Or1->OrderTime=0;
	Or1->DoLink=&AttackPointLink;
	Or1->info.AttackXY.x=x;
	Or1->info.AttackXY.y=y;
	Or1->info.AttackXY.wep=wep;
	//EnemyID=OID;
	Order1* LOR=LocalOrder;
	//if(int(InLineCom))FreeAsmLink();
	LocalOrder=Or1;
	//OrderReport=NULL;
	//MessageKind=0;
	//Sender=0xFFFF;
	//PrioryLevel=Prio&127;
};	
void AttackPointLink(OneObject* OBJ){
	
};
//Lªðúþòðª¹ point
void ContinueAttackPointLink(OneObject* OBJ);
void OneObject::ContinueAttackPoint(byte x,byte y,int Prio){
	if(CheckOrderAbility())return;
	if(Prio>=16)StandGround=false;
	if(UnlimitedMotion)return;
	if(Media==2){
		//FlyAttack(OID,Prio);
		return;
	};
	//if(Ref.Visual->info.Basic.AttackRange<2||!Weap)return;
	
	if(Prio<PrioryLevel)return;
	if(!Ready)return;
	Order1* Or1=GetOrdBlock();
	if(!int(Or1))return;
	ClearOrders();
	Or1->PrioryLevel=Prio&127;
	Or1->NextOrder=LocalOrder;
	Or1->OrderType=34;//Lªðúð
	Or1->OrderTime=0;
	Or1->DoLink=&ContinueAttackPointLink;
	Or1->info.AttackXY.x=x;
	Or1->info.AttackXY.y=y;
	//EnemyID=OID;
	Order1* LOR=LocalOrder;
	//if(int(InLineCom))FreeAsmLink();
	LocalOrder=Or1;
	//OrderReport=NULL;
	//MessageKind=0;
	//Sender=0xFFFF;
	//PrioryLevel=Prio&127;
};	
void ContinueAttackPointLink(OneObject* OBJ){
};
//Lªðúþòðª¹ ¸ªõýº
void ContinueAttackWallLink(OneObject* OBJ);
void OneObject::ContinueAttackWall(byte x,byte y,int Prio){
	if(CheckOrderAbility())return;
	if(Prio>=16)StandGround=false;
	if(UnlimitedMotion)return;
	if(Media==2){
		//FlyAttack(OID,Prio);
		return;
	};
	//if(Ref.Visual->info.Basic.AttackRange<2||!Weap)return;
	
	if(Prio<PrioryLevel)return;
	if(!Ready)return;
	Order1* Or1=GetOrdBlock();
	if(!int(Or1))return;
	ClearOrders();
	Or1->PrioryLevel=Prio&127;
	Or1->NextOrder=LocalOrder;
	Or1->OrderType=34;//Lªðúð
	Or1->OrderTime=0;
	Or1->DoLink=&ContinueAttackWallLink;
	Or1->info.AttackXY.x=x;
	Or1->info.AttackXY.y=y;
	//EnemyID=OID;
	Order1* LOR=LocalOrder;
	//if(int(InLineCom))FreeAsmLink();
	LocalOrder=Or1;
	//OrderReport=NULL;
	//MessageKind=0;
	//Sender=0xFFFF;
	PrioryLevel=Prio&127;
};	
void ContinueAttackWallLink(OneObject* OBJ){
    	
};
void MakeExpl(int xx,int yy);
void RestoreLock(int x,int y,int lx,int ly);
int GetHeight(int,int);
void BClrPt(int x,int y);
void DelWall(int x,int y){
	
};
void EliminateBuilding(OneObject* OB){
	if(OB&&OB->NewBuilding){
		NewMonster* NM=OB->newMons;
		int xx,yy;
        OB->GetCornerXY(&xx,&yy);
		if(OB->Stage<OB->Ref.General->MoreCharacter->ProduceStages&&NM->NBLockPt){
			for(int i=0;i<NM->NBLockPt;i++){
				BClrPt(xx+NM->BLockX[i],yy+NM->BLockY[i]);
			};
		}else{
			for(int i=0;i<NM->NLockPt;i++){
				BClrPt(xx+NM->LockX[i],yy+NM->LockY[i]);
			};
		};
	};
};
word DeathList[64];
word DeathSN[64];
int NDeath;
void InitDeathList(){
	NDeath=0;
};
void AddToDeathList(OneObject* OB){
	if(NDeath<64){
		DeathList[NDeath]=OB->Index;
		DeathSN[NDeath]=OB->Serial;
		NDeath++;
	};
};
#define MinDeath 2000
#define DeathDec 5
void ProcessDeathList(){
	for(int i=0;i<NDeath;i++){
		OneObject* OB=Group[DeathList[i]];
		bool Erase=false;
		if(OB&&OB->Serial==DeathSN[i]){
			if(OB->Life>(OB->MaxLife>>1))Erase=true;
			if(OB->Life>10)OB->Life-=DeathDec;
			else{
				Erase=true;
				OB->Die();
			};
		}else{
			if(OB){
				word d=OB->Serial-DeathSN[i];
				if(d==1||d==0xFFFF)DeathSN[i]=OB->Serial;
				else Erase=true;
			};
		};
		if(Erase){
			if(i<NDeath-1){
				memcpy(DeathList+i,DeathList+i+1,(NDeath-i-1)<<1);
				memcpy(DeathSN+i,DeathSN+i+1,(NDeath-i-1)<<1);
			};
			NDeath--;
		};
	};
	if((tmtmt&127)==17){
		for(i=0;i<NDeath;i++){
			OneObject* OB=Group[DeathList[i]];
			if(OB&&OB->Serial==DeathSN[i]){
				OB->Sdoxlo=3333;
			};
		};
		for(i=0;i<MAXOBJECT;i++){
			OneObject* OB=Group[i];
			if(OB&&OB->Ready){
				NewMonster* NM=OB->newMons;
				if(NM->Building&&OB->Life<MinDeath&&NM->SlowDeath&&!OB->Sdoxlo){
					AddToDeathList(OB);
				};
			};
		}
		for(i=0;i<NDeath;i++){
			OneObject* OB=Group[DeathList[i]];
			if(OB&&OB->Serial==DeathSN[i]){
				OB->Sdoxlo=0;
			};
		};
	};
};
void DestructBuilding(OneObject* OB){
	NewMonster* NM=OB->newMons;
	if(NM->SlowDeath&&(OB->Ready||OB->LocalOrder)&&OB->Stage>=OB->NStages){
		if(OB->Life<MinDeath)return;
		OB->Life=MinDeath-1;
		//OB->InFire=1;
		return;
	};
	OB->Die();
};
void CheckArmies(City*);
int CheckShipDirection(char Dir);
void RotateShipAndDie(OneObject* OBJ);
void RotateShipAndDieLink(OneObject* OBJ);
extern int DetonationX;
extern int DetonationY;
extern int DetonationForce;
void DetonateUnit(OneObject* OB,int CenterX,int CenterY,int Force);
void OnDie(int Killer,int Victim);
int GetDistanceToBuilding(OneObject* BLD,int x,int y){
	if(BLD->NewBuilding){
		x>>=4;
		y>>=4;
		int cx,cy;
		BLD->GetCornerXY(&cx,&cy);
		NewMonster* NM=BLD->newMons;
        int NP=NM->BuildPtX.GetAmount();
		int minr=100000;
		for(int i=0;i<NP;i++){
			int x1=cx+NM->BuildPtX[i];
			int y1=cy+NM->BuildPtY[i];
			int r=Norma(x1-x,y1-y);
			if(r<minr){
				minr=r;
			}
		}
		return minr<<4;
	}else return 100000;
}
int GetBuildingsInRadius(int x,int y,int R,word* DestBuf,int Max){
	int N=0;
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB&&OB->NewBuilding&&N<Max){
            int r=Norma(x-(OB->RealX>>4),y-(OB->RealY>>4));
			if(r<1000){
				r=GetDistanceToBuilding(OB,x,y);
				if(r<R){
                    DestBuf[N]=OB->Index;
					N++;
				}
			}
		}
	}
	return N;
}
void KillWallObject(OneObject* OB);
void OneObject::Die(){
	if(!ext_OnUnitDie(this,NULL))false;
	if(CObjIndex!=0xFFFF){
		bool DieComplexObject(OneObject* OB);
		if(!DieComplexObject(this))return;
	}
	OnDie(0xFFFF,Index);
	if(NewBuilding&&!Sdoxlo){
		LogBattle(NNUM,"^ST_BLOST^ %s(%d)",GetSt_UName(NIndex),CITY[NNUM].ReadyAmount[NIndex]-1);
	}
	FogRec* FR;
	if(Sdoxlo&&!Hidden)return;
	KillWallObject(this);
	NewMonster* NM=newMons;
	if(newMons->NoDestruct){
		int NSTG=Ref.General->MoreCharacter->ProduceStages;
		if(Stage==0||LoLayer==NM->GetAnimation(anm_DeathLie1))return;
		Life=3;
		Stage=(NSTG*3)>>2;
		Ready=0;
		LoLayer=NM->GetAnimation(anm_DeathLie1);
		goto DESTRUCTING;
	};
	if(BrigadeID!=0xFFFF){
		Brigade* BR=CITY[NNUM].Brigs+BrigadeID;		
		BR->lastTimeGetCenter=0;
		if(EngSettings.MoraleType==1){//Rome
			BR->Morale-=20000;
			if(BR->Morale<0)BR->Morale=0;
		}
	}
	addname(Index);
	if(LocalOrder&&LocalOrder->DoLink==&RotateShipAndDieLink)return;
	addname(Index);
	/*if(LockType==1){
		if(CheckShipDirection(RealDir)||delay<1000){
			RotateShipAndDie(this);
			return;
		};
	};*/
	addname(Index);
	if(!(NewBuilding||LockType==1||newMons->Artilery||Sdoxlo>2||newMons->DontRotateOnDeath))RealDir+=(rando()>>8)-64;
	Nat->CITY->Account-=newMons->Ves*2;
	if(FireOwner<8&&FireOwner!=NNUM){		
		City* CT=CITY+FireOwner;
		CT->Account+=3*newMons->Ves;
		FireOwner=0xFF;
	};
	//if(BigadeID!=0xFFFF){
	//	Brigade* BR=Nat->CITY->Brigs+BrigadeID;
	//	int 

	//};
	if(newMons->CommandCenter)Nat->CITY->LastBarrackDestroyTime=tmtmt;
	Nat->NKilled[NIndex]++;
	rando();
	Nat->CITY->UnRegisterNewUnit(this);
	if(GlobalOrder){
		if(GlobalOrder->Disconnect)GlobalOrder->Disconnect(this,GlobalOrder,0,0);
	};
	
	if(NM->DeathSoundID!=-1){
		AddEffect(RealX>>4,(RealY>>5)-GetHeight(RealX>>4,RealY>>4),NM->DeathSoundID);
	};
	if(NInside){
		for(int j=NInside-1;j>=0;j--){
			if(j<NInside){
				word MID=Inside[j];
				if(MID!=0xFFFF){
					OneObject* OB=Group[MID];
					if(OB){
						OB->Hidden=0;
						OB->Sdoxlo=0;
						OB->Die();
						if(Group[MID])OB->Sdoxlo=5000;
					}
				}
			}
		}
		if(Inside)free(Inside);
		Inside=NULL;
		NInside=0;
	};
	//removing from wall/transport/...
	if(LockedInBuilding!=0xFFFF){
        OneObject* OB=Group[LockedInBuilding];
		if(OB){
			int N=OB->NInside;
			word* INS=OB->Inside;
			if(INS){
				for(int j=0;j<N;j++){
					if(INS[j]==Index){
						//need to remove
						if(j<N-1)memcpy(INS+j,INS+j+1,(N-j-1)<<1);
						N--;
						OB->NInside--;
					}
				}
			}            
		}
	}
	//removing from mine if need
	if(Hidden){
		int idx=Index;
		for(int i=0;i<MAXOBJECT;i++){
			OneObject* OB=Group[i];
			if(OB&&OB->NInside){
				int N=OB->NInside;
				word* INS=OB->Inside;
				if(INS){
					for(int j=0;j<N;j++){
						if(INS[j]==idx){
							//need to remove
							if(j<N-1)memcpy(INS+j,INS+j+1,(N-j-1)<<1);
							N--;
							OB->NInside--;
						};
					};
				};
			};
		};
	};
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	if(NewBuilding && NNUM==NI && !NotSelectable && delay<1500 && tmtmt>20 && newMons->MineRadius==0){
		char ccc[200];
		sprintf(ccc,LOSTBLD,Ref.General->Message);
		AssignHint1(ccc,100,32);
		LastActionX=RealX>>4;
		LastActionY=RealY>>4;

	};
	UnitsField.BClrBar(x,y,Lx);
	if(GLock){
		MFIELDS[LockType].BClrBar(x,y,Lx);
	};
	Selected=0;
	ImSelected=0;
	if(newMons->NBars)Delete3DBar(Index);
	if(newMons->LongDeath){
		Life=0;
		Sdoxlo=1;
	}
	if(newMons->Usage==TowerID){
		GNFO.ClearTow(this);
	};
	if(IFire){
		IFire->Erase();
		IFire=NULL;
	};
    if(NewBuilding){
		//wMonster* NM=newMons;
		if(NM->Port){
			int PortBuiX=WallX;
			int PortBuiY=WallY;
			int LL=NM->BuiDist;
			MFIELDS[1].BClrBar(PortBuiX-LL-1,PortBuiY-1,3);
			MFIELDS[1].BClrBar(PortBuiX+LL-1,PortBuiY-1,3);
			MFIELDS[1].BClrBar(PortBuiX-1,PortBuiY-LL-1,3);
			MFIELDS[1].BClrBar(PortBuiX-1,PortBuiY+LL-1,3);
		};
        FR=&NM->Destruct;
		if(Stage<Ref.General->MoreCharacter->ProduceStages){
			if(!NM->GetAnimation(anm_DeathLie2)->Enabled){
				//DestructBuilding(this);
				DelObject(this);
				Group[Index]=NULL;
			}else{
				Sdoxlo=true;
				Life=0;
				LoLayer=NM->GetAnimation(anm_DeathLie2);
				NewAnm=NULL;
			};

		}else
		if(!NM->GetAnimation(anm_DeathLie1)->Enabled){
			///DestructBuilding(this);
			//DelObject(this);
			//Group[Index]=NULL;
			return;
		}else{
			Sdoxlo=true;
			Life=0;
			LoLayer=NM->GetAnimation(anm_DeathLie1);
			NewAnm=NULL;
		};
        if(delay>=1500||(Stage<(Ref.General->MoreCharacter->ProduceStages>>1))||!FR->NWeap)return;
		if(NM->MineRadius){
			word IDX[4];
			int N=GetBuildingsInRadius(RealX>>4,RealY>>4,NM->MineRadius,IDX,4);
			for(int i=0;i<N;i++)if(IDX[i]!=Index){
				Group[IDX[i]]->MakeDamage(NM->MineDamage,NM->MineDamage,this);
			}
			Sdoxlo=10000;
		}
DESTRUCTING:
		FR=&NM->Destruct;
		if(delay<2000){
			int nn=FR->WProb;
			int xp,yp;
			int xx,yy;
			GetCornerXY(&xx,&yy);
			for(int i=0;i<NM->NCheckPt;i++){
				xp=(xx+NM->CheckX[i])<<4;
				yp=(yy+NM->CheckY[i])<<4;
				int nw=(rando()*FR->NWeap)>>15;
				if(rando()<nn)Create3DAnmObject(WPLIST[FR->Weap[nw]],xp,yp,GetHeight(xp,yp)+16,xp,yp,1000,NULL,0,0xFFFF);
			};
			return;
		};
	}else{
		///RealDir+=(rando()&63)-32;
	}
	if(Hidden){
		Sdoxlo=5000;
		Life=0;
		Hidden=false;
	}else if(!Sdoxlo)Sdoxlo=1;
	UnBlockUnit();	
	if(DetonationForce!=-1&&!(NewBuilding||LockType)){
		DetonateUnit(this,DetonationX,DetonationY,DetonationForce);
	};	
}
void HealWalls(){
	
};
void OneObject::Eliminate(){
	
};
void OneObject::MakeDamage(int Fundam,int Persist,OneObject* Sender){
	MakeDamage(Fundam,Persist,Sender,0);
};
extern short AlarmSoundID;
extern byte   WeaponFlags[32];
extern int LastAttackDelay;
int LastAttackX=-1;
int LastAttackY=-1;
int AlarmDelay=0;

int LastSignalX=-1;
int LastSignalY=-1;
int SignalDelay=-1;
DWORD SignalColor=0xFFFF0000;

int GetUnitActivity(OneObject* OB);
void DamageInside(OneObject* OB,int Dam,OneObject* Sender,byte AttType){
	if(AttType==0&&Sender&&Sender->newMons->DamWeap[1]&&!Sender->NewBuilding)return;
	int p=(int(rando())*101)>>15;
	if(Sender&&p<OB->newMons->PromaxPercent[Sender->newMons->PromaxCategory])return;
	int N=OB->NInside;
	int rp=(int(rando())*N)>>15;
	word pp=OB->Inside[rp];
	if(pp!=0xFFFF){
		OneObject* IOB=Group[pp];
		if(OB){
			IOB->MakeDamage(Dam,Dam,Sender,AttType);
		};
	};
};
void ViewDamage(OneObject* VIC,int value);
void PanicUnit(OneObject* OB);
void MakePanicInRadius(int x,int y,byte NI,int r,int FearID,int sx,int sy,bool Strong=0);
extern City CITY[8];
word GetDir(int dx,int dy);
void OnDie(int Killer,int Victim);
int NCallsMakeDamage=0;
void AddKillDelay(OneObject* OB){
	int dd=OB->newMons->KillDelay;
	if(dd>3)dd=3;
	OB->NMisses=dd;    
}
bool CheckObjectForFreeStatus(OneObject* OB);
int OneObject::MakeDamage(int Fundam,int Persist,OneObject* Sender,byte AttType,bool Act){
	if(newMons->UnbeatableWhenFree&&CheckObjectForFreeStatus(this)){
		if(Sender){
			if( !( Sender->newMons->KillMask & MathMask ) )return 0;
		}else return 0;
	}
	if(newMons->Immortal)return 0;
	if(Act)NCallsMakeDamage++;
	Illusion=false;
	if(Sender&&Sender==this)return 0;
	if(Act&&Sender){
		if(LockedInBuilding!=0xFFFF){
            OneObject* OWB=Group[LockedInBuilding];
			if(OWB&&OWB->NInside){
				if(OWB->newMons->MissProbabilityForInsideUnits>rando()%100){
					if(Act)ViewDamage(this,0);
					return 0;
				}
			}
		}
		if(newMons->MissProbabilityOnHeightDiff100){
			int DH=RZ-Sender->RZ;
			if(DH>0){
				int pr=DH*newMons->MissProbabilityOnHeightDiff100/100;
				if(pr>newMons->MaxMissProbabilityOnHeightDiff){
					pr=newMons->MaxMissProbabilityOnHeightDiff;
				}
				if(pr&&pr>rando()%100){
					if(Act)ViewDamage(this,0);
					return 0;
				}
			}
		}
	}
	if(Sender&&Sender->Index!=Index&&!(Sender->newMons->Animal||newMons->Animal||Sender->AI_Guard||AI_Guard)){
		int ac1=GetUnitActivity(Sender);//0-neytral,1-my territory,-1-enemy's territory
		int ac2=GetUnitActivity(this);
		if(ac1!=1&&ac2==1){
			if(Sender->NNUM!=NNUM&&Act){
				Sender->Die();
			}
			return 0;
		};
		if(ac1==1&&ac2==-1){
			if(Sender->NNUM!=NNUM&&Act)Die();
			return 0;
		};
		if(ac2==1)return 0;
	};
	if(Zombi&&Act){
		if(BrigadeID!=0xFFFF){
			Brigade* BR=Nat->CITY->Brigs+BrigadeID;
			if(BR->ArmyID!=0xFFFF){
				AI_Army* AIR=Nat->CITY->ARMS+BR->ArmyID;
				AIR->Bitva();
			};
		};
	};
	int dam=Persist;//+(Persist>>2);
	if(dam<=0)return 0;
	//dam-=(rando()*Persist)>>16;
	int sum_shld=0;
	if(BrigadeID!=0xFFFF){
		Brigade* BR=CITY[NNUM].Brigs+BrigadeID;
		int s,exs;
        BR->GetBrigadeProtection(AttType,s,exs,Sender,this);
		//dam-=exs;
		sum_shld+=exs;
	};
	if(Sender){
		NewMonster* SNM=Sender->newMons;
		if(Sender->BrigadeID!=0xFFFF){
			Brigade* BR=CITY[Sender->NNUM].Brigs+Sender->BrigadeID;
			int d,exd;
			BR->GetBrigadeDamage(AttType,d,exd,Sender,this);
			dam+=exd;
		}else{
			if(Sender->newMons->SkillDamageBonus){
				byte msk=Sender->newMons->SkillDamageMask;
				if(msk&(1<<AttType)){
					int ddm=(int(Sender->Kills)*int(Sender->newMons->SkillDamageBonus))/10;
					if(ddm>dam*19)ddm=dam*19;
					dam+=ddm;
				};
			};
		};
	};	
	AdvCharacter* ADC=Ref.General->MoreCharacter;
	char DamageKind=-1;
	//  ;-)  // dam=div(dam*(100-ADC->Shield),100).quot;
	int dam0=dam;	
	if(AttType!=255){
		//dam-=ADC->Shield;
		sum_shld+=ADC->Shield;
		if(ActiveAbility)ActiveAbility->ActiveAbilities.modifyShield(AttType,sum_shld,Act?Sender:NULL);
	}
	if(Sender){
		bool ACTION_OnUnitDamage(OneObject* Sender,OneObject* Take,int& Damage);
		if(Act&&!ACTION_OnUnitDamage(Sender,this,dam))return 0;
		int Fear=Sender->newMons->FearType[AttType];
		if(Fear!=0xFF&&Act){
			void OnUnitDamage(word MID,word Sender,int Att);
			OnUnitDamage(Index,Sender->Index,Sender->newMons->FearType[AttType]);					
		};
		if(Sender->newMons->ShotPtZ&&AttType<1&&Act){
			//stronghold
			int NS=0;
			int MAXS=Sender->Ref.General->MoreCharacter->MaxInside;
			for(int i=0;i<Sender->NInside;i++){
				word MID=Sender->Inside[i];
				if(MID!=0xFFFF){
					OneObject* IOB=Group[MID];
					if(IOB){
						byte USE=IOB->newMons->Usage;
						if(USE==StrelokID||USE==HorseStrelokID||USE==ArcherID)NS++;
					};
				};
			};
			if(MAXS&&AttType!=255){
				int dam0=Sender->newMons->MinDamage[AttType];
				int dam1=Sender->Ref.General->MoreCharacter->MaxDamage[AttType];
				dam=dam0+(((dam1-dam0)*NS)/MAXS)-ADC->Shield;
			};
		};
		int damtype=0;
		if(AttType<NAttTypes)damtype=Sender->Ref.General->MoreCharacter->WeaponKind[AttType];
		byte* RF=Sender->Ref.General->MoreCharacter->WeaponKind;
		addrand(AttType);
		addrand(damtype);
		if(damtype>=0&&AttType!=255){
			byte wf=WeaponFlags[damtype];
			addrand(wf);
			if(wf&32&&Act){
				addrand(7);
				//shot
				if(NewBuilding/*&&NInside*/){
					addrand(8);
					if(NInside)DamageInside(this,dam,Sender,AttType);
					else if(Sender&&Sender->NewBuilding&&Sender->Attack&&Sender->EnemyID==Index&&Sender->EnemySN==Serial){
						Sender->EnemySN+=256;
					};
					return 0;
				};
			};
			addrand(9);
			if(AttType!=255&&(wf&2)&&rando()<1310&&!(newMons->Building||newMons->No25)){
				addrand(10);
				if(!Act)
					return 1000000;
				if(Sender){
					void OnUnitDeath(word MID,word Sender);
					OnUnitDeath(Index,Sender->Index);
				};
				int idx=Index;
				addrand(11);
				if(!ext_OnUnitDie(this,Sender))return 0;
				addrand(12);
				Die();
				addrand(13);
				if(Sender){
					OnDie(Sender->Index,idx);
					Sender->Kills++;
					AddKillDelay(Sender);
					if(Sender->BrigadeID!=0xFFFF){
						Brigade* BR=CITY[Sender->NNUM].Brigs+Sender->BrigadeID;
						BR->IncBrigExperience(100);
						if(EngSettings.MoraleType==1){//Rome
                            BR->Morale+=10000;
						}
					}
				};
				addrand(14);
				return 0;
			};
			int protect=ADC->Protection[damtype];
			if(ActiveAbility)ActiveAbility->ActiveAbilities.modifyProtection(AttType,protect,protect,Act?Sender:NULL);
			//  ;-)  //dam=(dam*(256-protect))>>8;
			//dam-=protect;
			sum_shld+=protect;
			if((wf&4)&&Sender->AddDamage&&Sender->BrigadeID==0xFFFF){
				int adm=Sender->AddDamage;
				dam+=Sender->AddDamage;
			};
			
			if(AddShield&&BrigadeID==0xFFFF){
				int adm=AddShield;
				//dam-=AddShield;
				sum_shld+=AddShield;
			};
			if(sum_shld<0)sum_shld=0;
			dam-=sum_shld;
			if(Act){
				if(wf&16){
					FiringStage=newMons->FireLimit+1;
					InFire=1;
					FireOwner=0xFF;
					if(Sender&&(!Sdoxlo)){
						if(!(LocalOrder&&LocalOrder->DoLink==&RotateShipAndDieLink))FireOwner=Sender->NNUM;
					};
				}else
				if(wf&8){
					if(FiringStage<newMons->FireLimit)FiringStage++;
					else{ 
						InFire=1;
						if(Sender)FireOwner=Sender->NNUM;
						else FireOwner=0xFF;
					};
				};
			};
		}else{
			if(sum_shld<0)sum_shld=0;
			dam-=sum_shld;
		}		
		if(BrigadeID!=0xFFFF){
			// last units which made damage on this brigade
			Brigade* BR=CITY[NNUM].Brigs+BrigadeID;
			if(BR->WarType){
				BR->vLastEnemyID=Sender->Index;
				BR->vLastEnemySN=Sender->Serial;
				BR->vLastEnemyNI=Sender->NNUM;
			}
		}
	};
	if(dam<=0)dam=1;
	if(ActiveAbility)ActiveAbility->ActiveAbilities.modifyDamage(AttType,dam,dam,Act?Sender:NULL);
	if(!Act)return dam;
	ViewDamage(this,-dam);
	//PanicUnit(this);
	if(Sender&&(!LastAttackDelay)&&NNUM==MyNation){
		if(Sender->NNUM!=NNUM){
			LastAttackDelay=900;
			if(AlarmSoundID!=-1){
				AddEffect((mapx<<5)+500,(mapy<<4)+300,AlarmSoundID);
				LastActionX=RealX>>4;
				LastActionY=RealY>>4;
				LastAttackX=RealX;
				LastAttackY=RealY;
				AlarmDelay=60;
			};
		};
	};
	addrand(14);
	if(Act&&Sender&&Sender->newMons->DetonationForce){
		int dx=RealX-Sender->RealX;
		int dy=RealY-Sender->RealY;
		int n=Norma(dx,dy)+1;
		dx=(rando()&7)-3+(dx*16)/n;
		dy=(rando()&7)-3+(dy*16)/n;
        DetonateUnit(this,(RealX>>4)+dx,(RealY>>4)+dy,Sender->newMons->DetonationForce);
	}
	addrand(12);
	if(!ext_OnUnitDamage(this,Sender,dam))return 0;
	addrand(13);
	if(Life>dam){
		void OnDamage(int Killer,int Victim,int Damage);		
		OnDamage(Sender?Sender->Index:0xFFFF,Index,dam);
		addrand(15);
		Life-=dam;
		if(Sender){			
			int RMAX=Sender->Ref.General->MoreCharacter->AttackRadius2[AttType];
			if(RMAX<200&&!newMons->RotationAtPlaceSpeed){
				int RMIN=Sender->Ref.General->MoreCharacter->AttackRadius1[AttType];
				int RA=(RMAX*3+RMIN)<<2;
				if(Norma(Sender->RealX-RealX,Sender->RealY-RealY)<RA){
                    int dx=Sender->RealX-RealX;
					int dy=Sender->RealY-RealY;
					int N=Norma(dx,dy);
					if(N){
						dx=80*dx/N;
						dy=80*dy/N;
						RealX-=dx;
						RealY-=dy;
						x=(RealX-((Lx)<<7))>>8;
						y=(RealY-((Lx)<<7))>>8;										
					}
				}
			}
		}
		addrand(16);
		if(LockType!=1&&Sender&&newMons->StrikeFlyMaxSpeed&&Sender->newMons->StrikeProbability>(rando()%100)){
			int dx=Sender->RealX-RealX;
			int dy=Sender->RealY-RealY;
            int N=Norma(dx,dy);
			if(N){
				NewAnimation* NA=newMons->GetAnimation(anm_FallDown);
				if(NA){
					void PushUnitBack(OneObject* OB,byte OrdType,int Force);
					int F=newMons->StrikeFlySpeed+((int(rando())*(newMons->StrikeFlyMaxSpeed-newMons->StrikeFlySpeed))>>15);
					F=(F*Sender->newMons->StrikeForce)/150;//was 100
					PushUnitBack(this,1,F);
				}else{
					NewAnimation* GA=newMons->GetAnimation(anm_MotionL);
					int NF=(GA->NFrames*newMons->MotionDist)/24;
					dx=NF*dx/N;
					dy=NF*dy/N;
					void StayForSomeTime(OneObject* OB,byte OrdType,int Time);
					StayForSomeTime(this,1,256*25);
					NewMonsterPreciseSendTo(RealX-(dx<<4),RealY-(dy<<4),128+16,1);				
					BackMotion=1;
					//GroupSpeed=newMons->MotionDist/2;
				}
			}
		}
		addrand(17);
		if(newMons->CompxCraft){
			int LEV=0;
			int CLF=Life;
			int LIFE0=Ref.General->MoreCharacter->Life;
			for(int i=0;i<5;i++){
				word ms=(StageState>>(i*3))&7;
				if(Life<=LIFE0){
					if(ms==1)StageState=StageState&(~(7<<(i*3)));
					if(ms==2){
						StageState=(StageState&(~(7<<(i*3))))|(3<<(i*3));
						//exploding section(if possible)
						NewMonster* NM=newMons;
						int Nex=NM->CompxCraft->Stages[i].NExplPoints;
						if(Nex){
							short* exps=NM->CompxCraft->Stages[i].ExplCoor;
							int xsm=0;
							int ysm=0;
							for(int j=0;j<Nex;j++){
								short x=exps[j+j];
								short y=exps[j+j+1];
								xsm+=x;
								ysm+=y;
							};
							xsm/=Nex;
							ysm/=Nex;
							int zpt=NM->CompxCraft->Stages[i].Ready.SortY;
							zpt+=80-(ysm<<3);
							FogRec* FR=&NM->Destruct;
							int nn=FR->WProb;
							int xp,yp;
							int xx,yy;
							GetCornerXY(&xx,&yy);
							for(int i=0;i<Nex;i++){
								xp=(xx+exps[i+i])<<4;
								yp=((yy+exps[i+i+1])<<4)+zpt*2;
								int nw=(rando()*FR->NWeap)>>15;
								if(rando()<nn)Create3DAnmObject(WPLIST[FR->Weap[nw]],xp,yp,GetHeight(xp,yp)+zpt,xp,yp,1000,NULL,0,0xFFFF);
							};
							if(NM->NSLockPt[i]){
								for(int j=0;j<NM->NSLockPt[i];j++){
									BClrPt(xx+NM->SLockX[i][j],yy+NM->SLockY[i][j]);
								};
							};
						};
					};
					SingleUpgLevel=LEV+2;
				}else{
					SingleUpgLevel=i+2;
				};
				if(ms==2){
					LIFE0+=newMons->CompxCraft->Stages[i].AddPoints;
				};
			};
		};
	}else{
		//if(NNUM!=7){
		{
			if(Sender&&Sender->NNUM!=NNUM){
				if(Sdoxlo&&!Hidden)return 0;
				if(LocalOrder&&LocalOrder->DoLink==&RotateShipAndDieLink)return 0;
				Sender->Kills++;
				City* CT=Sender->Nat->CITY;
				CT->Account+=3*newMons->Ves;
				if(Sender->BrigadeID!=0xFFFF)CT->Brigs[Sender->BrigadeID].IncBrigExperience(100);
				FireOwner=0xFF;
				if(NInside){
					for(int j=0;j<NInside;j++){
						word MID=Inside[j];
						if(MID!=0xFFFF){
							OneObject* OB=Group[MID];
							if(OB){
								CT->Account+=OB->newMons->Ves*3;
							};
						};
					};
				};
			}else if((!Sender)&&FireOwner<8&&FireOwner!=NNUM){
				City* CT=CITY+FireOwner;
				CT->Account+=3*newMons->Ves;
				FireOwner=0xFF;
			};
		};
		if(NewBuilding&&Sender&&NNUM==MyNation){
			LastAttackDelay=900;
			if(AlarmSoundID!=-1){
				AddEffect((mapx<<5)+500,(mapy<<4)+300,AlarmSoundID);
				LastAttackX=RealX;
				LastAttackY=RealY;
				AlarmDelay=60;
			};
		};
		if(Sender&&AttType!=255){
			void OnUnitDeath(word MID,word Sender);
			OnUnitDeath(Index,Sender->Index);
		};
		if(!ext_OnUnitDie(this,Sender))return 0;
		if(Sender){
			OnDie(Sender->Index,Index);
			AddKillDelay(Sender);
			if(Sender->BrigadeID!=0xFFFF){
				Brigade* BR=CITY[Sender->NNUM].Brigs+Sender->BrigadeID;
				if(EngSettings.MoraleType==1){//Rome
					BR->Morale+=10000;
				}
			}
		}
		Die();
		return 0;
	};
	if(EnemyID==0xFFFF&&BrigadeID!=0xFFFF&&((ActivityState==0&&EngSettings.BrigadeVseZaOdnogoInNormalMode)||
		ActivityState==2&&EngSettings.BrigadeVseZaOdnogoInAgresivMode))
	{
		Brigade* BR=CITY[NNUM].Brigs+BrigadeID;
		if (BR->NewBOrder==NULL) 
		{
			BR->AttEnm=true;
			byte drr= GetDir(Sender->RealX-RealX,Sender->RealY-RealY);
			BR->HumanGlobalSendTo(Sender->RealX/16,Sender->RealY/16,drr,128,0);
		}

		if(Sender){
			for(int i=0;i<BR->NMemb;i++)
			{
				if(BR->Memb[i]!=0xFFFF)
				{
					OneObject* BOB = Group[BR->Memb[i]];
					if(BOB&&BOB->EnemyID==0xFFFF&&!(BOB->Sdoxlo||BOB->newMons->DontAnswerOnAttack))
					{
						BOB->AttackObj(Sender->Index,1);
					}
				}
			}
		}
	}
	if(Sender&&!(newMons->Priest||Zombi||newMons->MotionStyle==3||newMons->Usage==PushkaID||NoSearchVictim||ActivityState==1||newMons->DontAnswerOnAttack))
		AttackObj(Sender->Index,1);
	return 0;
};
void OneObject::SearchSupport(word OBJ){
	
	return;

};

void WinnerControl(bool Anyway){
	//assert(MAXOBJECT<8000);
	int NMyPeasants=0;
	int NMyCenters=0;
	int NThemPeasants=0;
	int NThemCenters=0;
	int MyMask=1<<GSets.CGame.cgi_NatRefTBL[MyNation];
	byte UnLockN[8];
	memset(UnLockN,0,8);
	byte MyNT=GSets.CGame.cgi_NatRefTBL[MyNation];
	for(int i=0;i<8;i++)if(NATIONS[i].VictState!=1)UnLockN[GSets.CGame.cgi_NatRefTBL[i]]=1;
	if(Anyway||!(tmtmt&15)){
		NMyUnits=0;
		NThemUnits=0;
		for(int i=0;i<MAXOBJECT;i++){
			OneObject* OB=Group[i];
			if(OB&&UnLockN[OB->NNUM]&&OB->NNUM!=7&&!((OB->Sdoxlo||(OB->UnlimitedMotion&&!OB->LocalOrder))&&!OB->Hidden)){
				byte USE=OB->newMons->Usage;
				if(USE==PeasantID){
					//if(OB->Nat->AI_Enabled){
					//	if(!OB->Hidden){
					//		if(OB->NNUM==MyNT)NMyPeasants++;
					//		else if(!(OB->NMask&MyMask))NThemPeasants++;
					//	};
					//}else{
						if(OB->NNUM==MyNT)NMyPeasants++;
						else if(!(OB->NMask&MyMask))NThemPeasants++;
					//};
				}else
				if(USE==CenterID){
					if(OB->NNUM==MyNT)NMyCenters++;
					else if(!(OB->NMask&MyMask))NThemCenters++;
				};
				if(!(OB->LockType||OB->NewBuilding||OB->Wall)){
					//if(OB->Nat->AI_Enabled){
					//	if(!OB->Hidden){
					//		if(OB->NNUM==MyNT)NMyUnits++;
					//		else if(!(OB->NMask&MyMask))NThemUnits++;
					//	};
					//}else{
						if(OB->NNUM==MyNT)NMyUnits++;
						else if(!(OB->NMask&MyMask))NThemUnits++;
					//};
				};
			};
		};
		if(NMyPeasants==0&&NMyCenters==0&&NMyUnits<10)NMyUnits=0;
		if(NThemPeasants==0&&NThemCenters==0&&NThemUnits<10)NThemUnits=0;
	};
};
void OneObject::DefaultSettings(GeneralObject* GO){
	Ustage=0;
	NUstages=0;
	//Magic=0;
	//capMagic=GO->capMagic;
	//xForce=16;
	//DoubleForce=false;
	//TripleForce=false;
	//QuadraForce=false;
	//MTime=0;
	NInside=0;
	Transport=GO->newMons->Transport;
	Serial=rando();
	Absent=false;
	//Slow=false;
	Invisible=false;
	InFire=false;
	FiringStage=0;
	AbRes=0;
	//MagSrcID=0xFFFF;
	//if(GO->AGold)AbRes|=2;
	//if(GO->AWood)AbRes|=4;
	RefreshLife=GO->RefreshLife;
	//DoWalls=false;
	Use_AI=false;
	AntiNuc=GO->AntiNuc;
	if(AntiNuc){
		RAmount=1;
		delay=3000;
		MaxDelay=3000;
	}else RAmount=0;
	//UFOTrans=GO->UFOTrans;
	DstX=0;
	DstY=0;
	//Repair=false;
	PathX=NULL;
	PathY=NULL;
	NIPoints=0;
	CurIPoint=0;
	CPdestX=0;
	CPdestY=0;
	Kind=GO->Kind;
	//VisRadius=GO->VisRadius;
	//VisSpots=GO->VisSpots;
	//SpotType=GO->SpotType;
	//SpotSize=GO->SpotSize;
	//DangerZone=GO->DangerZone;
	addrand(NoSearchVictim);
	NoSearchVictim=GO->NoSearchVictim;
	addrand(NoSearchVictim);
	//wepX=GO->wepX;
	//wepY=GO->wepY;
	MaxDelay=GO->delay;
	GroupIndex=NULL;
	if(!GO->SizeX)GO->SizeX=1;
	if(!GO->SizeY)GO->SizeY=1;
	Lx=GO->SizeX;
	Ly=GO->SizeY;
	TempFlag=false;
	Mobilised=false;
	//Wars=NULL;
    NStages=GO->NStages;
	
	Stuff=0xFFFF;
};
void PatrolLink(OneObject* OBJ);
void OneObject::Patrol(int x,int y,int x1,int y1,byte Prio){
	if(InArmy)return;
	if(CheckOrderAbility())return;
	if(NewBuilding)return;
	if(UnlimitedMotion)return;
	if(!Ready)return;
	addrand(1025);
	Order1* Or1=CreateOrder(0);
	if(!int(Or1))return;
	Or1->PrioryLevel=0;
	Or1->OrderType=81;//Lªðúð
	Or1->OrderTime=0;
	Or1->DoLink=&PatrolLink;
	Or1->info.Patrol.x=x;
	Or1->info.Patrol.y=y;
	Or1->info.Patrol.x1=x1;
	Or1->info.Patrol.y1=y1;
	Or1->info.Patrol.dir=1;
	Or1->info.Patrol.dir=1;
	PrioryLevel=0;
	NewMonsterSmartSendTo(x1,y1,0,0,0,1);
};	
void PatrolLink(OneObject* OBJ){
	OBJ->PrioryLevel=0;
	Order1* OR1=OBJ->LocalOrder;
	if(OR1->info.Patrol.dir){
		OBJ->NewMonsterSmartSendTo(OR1->info.Patrol.x,OR1->info.Patrol.y,0,0,0,1);
		OR1->info.Patrol.dir=0;
	}else{
		OBJ->NewMonsterSmartSendTo(OR1->info.Patrol.x1,OR1->info.Patrol.y1,0,0,0,1);
		OR1->info.Patrol.dir=1;
	};
};
void OneObject::EnableDoubleForce(){

};
void OneObject::DisableDoubleForce(){
};
void OneObject::EnableTripleForce(){
};
void OneObject::DiasableTripleForce(){
};
void OneObject::EnableQuadraForce(){
};
void OneObject::DisableQuadraForce(){
};
void WaitForRepairLink(OneObject* OBJ);
void OneObject::WaitForRepair(){
	if(CheckOrderAbility())return;
	if(!Ref.General->CanRepair)return;
	Order1* Or1=GetOrdBlock();
	if(!int(Or1))return;
	Or1->PrioryLevel=0;
	Or1->NextOrder=LocalOrder;
	Or1->OrderType=103;//Lªðúð
	Or1->OrderTime=0;
	Or1->DoLink=&WaitForRepairLink;
	Or1->info.MoveToXY.x=x;
	Or1->info.MoveToXY.y=y;
	PrioryLevel=0;
	LocalOrder=Or1;
};
void WaitForRepairLink(OneObject* OBJ){
	
};
//####################################################//
//################                 ###################//
//##############   NUCLEAR SECTION   #################//
//################                 ###################//
//####################################################//
extern bool EUsage[8192];
extern word LastAnmIndex;
extern AnmObject* GAnm[8192];
word NucList[128];
word NucSN[128];
bool NDone[128];
word NNuc;
void InitNucList(){
	memset(NucList,255,sizeof NucList);
	memset(NucSN,255,sizeof NucSN);
	NNuc=0;
};
void RegisterNuc(word ID){
	if(!EUsage[ID])return;
	for(int i=0;i<128;i++){
		if(NucList[i]==0xFFFF){
			NucList[i]=ID;
			NucSN[i]=GAnm[ID]->ASerial;
			NDone[i]=false;
			NNuc++;
			return;
		};
	};
};
/*void HandleAntiNuc(){
	if(!NNuc)return;
	for(int i=0;i<128;i++){
		if(NucList[i]!=0xFFFF){
			if(!EUsage[NucList[i]]){
				NucList[i]=0xFFFF;
				NNuc--;
			}else{
				AnmObject* EO=GAnm[NucList[i]];
				if(EO->ASerial!=NucSN[i]){
					NucList[i]=0xFFFF;
					NNuc--;
				}else
				if(!NDone[i]){
					byte NMS=EO->NMask;
					int dpx=EO->destX;
					int dpy=EO->destY;
					word AID=0xFFFF;
					int adist=1000;
					int diss;
					//find nearest enemy antinuclear;
					for(int j=0;j<MAXOBJECT;j++){
						OneObject* OB=Group[j];
						if(OB&&OB->RAmount&&OB->AntiNuc&&!(OB->NMask&NMS)){
							diss=abs(dpx-int(OB->x))+abs(dpy-int(OB->y));
							if(diss<adist){
								AID=j;
								adist=diss;
							};
						};
					};
					if(AID!=0xFFFF){
						OneObject* OB=Group[AID];
						OB->RAmount--;
						//CreateLeadingObject(OB->Weap,((OB->x<<5)+OB->wepX)<<2,((OB->y<<5)+OB->wepY)<<2,128,OB->NMask,OB,NucList[i]);
						NDone[i]=true;
					};
				};
			};
		};
	};
};*/
/*void ShowNucl(){
	if(!NNuc)return;
	for(int i=0;i<128;i++){
		if(NucList[i]!=0xFFFF){
			AnmObject* EO=GAnm[NucList[i]];
			int xx=minix+(EO->x>>16);
			int yy=miniy+(EO->y>>16);
			Hline(xx-1,yy,xx+1,255);
			Vline(xx,yy-1,yy+1,255);
			xx=minix+(EO->destX>>1);
			yy=minix+(EO->destY>>1);
			Hline(xx-2,yy,xx-1,clrYello);
			Hline(xx+1,yy,xx+2,clrYello);
			Vline(xx,yy-2,yy-1,clrYello);
			Vline(xx,yy+1,yy+2,clrYello);
		};
	};
};*/

//**************************************************//
//                                                  //
//            INITIALISATION OF THE GAME            //
//                                                  //
//**************************************************//
void InitGame(){
	InitNucList();
};
typedef char* lpchar;
char** NatNames;
char** NatScripts;
int NNations;
char** NatsIDS;
char NatCharLo[32][8];
char NatCharHi[32][8];
byte NTex1[32];
byte NTex2[32];

int GetNationByID(char* Name){
	for(int i=0;i<NNations;i++){
		if(!strcmp(Name,NatsIDS[i]))return i;
	};
	return -1;
};

void AttackObjLink(OneObject* OBJ);
ReportFn* AttackLink=(ReportFn*)0x652419;
void __stdcall CDGINIT_INIT2(){
	AttackLink=&AttackObjLink;
};
extern bool ProtectionMode;
void LoadMessages();
char* GetTextByID(char* ID);
void LoadNations(){
	//ProtectionMode=1;
	GFILE* f=Gopen("Nations.lst","r");
	ProtectionMode=0;
	AText("Nations.lst");
	int z=Gscanf(f,"%d",&NNations);
#ifdef INETTESTVERSION
	//NNations=4;
#endif
	NatNames=znew(lpchar,NNations);
	NatScripts=znew(lpchar,NNations);
	NatsIDS=znew(lpchar,NNations);
	memset(NTex1,0xFF,32);
	memset(NTex2,0xFF,32);	
	for(int i=0;i<NNations;i++){
		char NName[64];
		char NScrp[64];
		char NIDS[64];
		char vv[16];
		vv[0]=0;
		z=Gscanf(f,"%s%s%s%s",NIDS,NName,NScrp,vv);
		if(vv[0]=='#'){
			int t1,t2;
			z=Gscanf(f,"%d%d",&t1,&t2);
			NTex1[i]=t1;
			NTex2[i]=t2;
			Gscanf(f,"%s%s",NatCharLo[i],NatCharHi[i]);
		}else{
			strcpy(NatCharLo[i],vv);
			Gscanf(f,"%s",NatCharHi[i]);
		};
		char CC4[64];
		sprintf(CC4,"#%s",NIDS);
		strcpy(NName,GetTextByID(CC4));
		NatNames[i]=znew(char,strlen(NName)+1);
		strcpy(NatNames[i],NName);
		NatScripts[i]=znew(char,strlen(NScrp)+1);
		strcpy(NatScripts[i],NScrp);
		NatsIDS[i]=znew(char,strlen(NIDS)+1);
		strcpy(NatsIDS[i],NIDS);		
	};
	Gclose(f);
};
void FreeNations(){
	for(int i=0;i<NNations;i++){
		free(NatNames[i]);
		free(NatScripts[i]);
		free(NatsIDS[i]);
	};
	free(NatNames);
	free(NatScripts);
	free(NatsIDS);
};
void Nation::AddPopul(word N){
	if(NPopul>=MaxPopul){
		MaxPopul+=64;
		Popul=(word*)realloc(Popul,2*MaxPopul);
	};
	Popul[NPopul]=N;
	NPopul++;
	if(NAccount>=MaxAccount){
		MaxAccount+=64;
		Account=(word*)realloc(Account,2*MaxAccount);
	};
	int acc=CITY->Account/100;
	if(acc<0)acc=0;
	Account[NAccount]=acc;
	NAccount++;
	if(N)ThereWasUnit=1;
};
void Nation::AddUpgrade(word ID,int time){
	if(NUpgMade>=MaxUpgMade){
		MaxUpgMade+=64;
		UpgIDS=(word*)realloc(UpgIDS,2*MaxUpgMade);
		UpgTime=(int*)realloc(UpgTime,4*MaxUpgMade);
	};
	UpgIDS[NUpgMade]=ID;
	UpgTime[NUpgMade]=time;
	NUpgMade++;
};
void EnumPopulation(){
	int NMN[8]={0,0,0,0,0,0,0,0};
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB&&(OB->Hidden||!OB->Sdoxlo)&&(!OB->NewBuilding)){
			NMN[OB->NNUM]++;
		};
	};
	for(i=0;i<7;i++){
		NATIONS[i].AddPopul(NMN[i]);
	};
};
void AnimationsScope::Add(NewAnimation* NA,DWORD Code){
	ANM.Add(NA);
	CreateHashTable();
}
NewAnimation* AnimationsScope::Get(DWORD Code){
	DWORD Idx=Code%AnmHashSize;
	NewAnimation* NA=AHASH[Idx];
	NewAnimation* NA0=NA;
	if(NA){
		while(NA&&NA->Code!=Code){
			NA=NA->Next;
		}
		if(NA){
            int n=NA->NSuchAnimations;
			int p=n>1?rando()%n:0;
			if(p==0)return NA;			
			NA=NA0;			
			while(NA){
				while(NA&&NA->Code!=Code){
					NA=NA->Next;
				}            
				if(NA){
					if(p==0)return NA;
					NA=NA->Next;
					p--;
				}else return NA0;
			}
		}
	}
	static NewAnimation SAN;
	SAN.Enabled=false;
	return &SAN;
}
NewAnimation* AnimationsScope::Get(DWORD Code,int Index){
	DWORD Idx=Code%AnmHashSize;
	NewAnimation* NA=AHASH[Idx];
	if(NA){
		while(NA&&NA->Code!=Code){
			NA=NA->Next;
		}
		if(NA){
            int n=NA->NSuchAnimations;
			if(Index<n){
				int p=Index;
				if(p==0)return NA;
				NewAnimation* NA0=NA;
				while(NA){
					while(NA&&NA->Code!=Code){
						NA=NA->Next;
					}            
					if(NA){
						if(!p)return NA;
						NA=NA->Next;
						p--;
					}else return NA0;
				}
			}
		}
		return NULL;
	}
	static NewAnimation SAN;
	SAN.Enabled=false;
	return &SAN;
}
void AnimationsScope::CreateHashTable(){
	memset(AHASH,0,sizeof AHASH);
	for(int i=0;i<ANM.GetAmount();i++){
		NewAnimation* NA=ANM[i];
		int idx=NA->Code%AnmHashSize;
        NA->Next=AHASH[idx];
		AHASH[idx]=NA;
	}
	for(int i=0;i<AnmHashSize;i++)if(AHASH[i]){
		NewAnimation* NA=AHASH[i];
		int NN=0;
		DWORD COD=NA->Code;
		while(NA){
			if(NA->Code==COD)NN++;
			NA=NA->Next;
		}
		NA=AHASH[i];
		while(NA){
			if(NA->Code==COD)NA->NSuchAnimations=NN;
			NA=NA->Next;
		}
	}
}
#define ranm(x) {\
	char cc[32];\
	strcpy(cc,#x);\
    strupr(cc);\
	E->Add(cc,anm_##x);\
}
#define ranm2(x,y) {\
	char cc[32];\
	strcpy(cc,#y);\
	strupr(cc);\
	E->Add(cc,anm_##x);\
}
#define ranmN(x)\
	ranm(x);\
	for(int i=0;i<NAttTypes;i++){\
		char cc[32];\
		sprintf(cc,#x"%d",i);\
		strupr(cc);\
		E->Add(cc,anm_##x+i);\
	}
#define ranmN2(x,y)\
	ranm(x);\
	for(int i=0;i<NAttTypes;i++){\
	char cc[32];\
	sprintf(cc,#y"%d",i);\
	strupr(cc);\
	E->Add(cc,anm_##x+i);\
	}
void CreateAnimationsEnumerator(){
	Enumerator* E=ENUM.Get("MON_ANIMATIONS");
	ranm2(MotionL,Motion_L);
	ranm2(MotionR,Motion_R);
	ranm2(MotionLB,Motion_LB);
	ranm2(MotionRB,Motion_RB);
	ranm(MiniStepL);
	ranm(MiniStepR);
	ranm(MiniStepLB);
	ranm(MiniStepRB);
	ranm(Fist);
	ranm(Death);
	ranm(DeathLie1);
	ranm(DeathLie2);
	ranm(DeathLie3);
	ranm(Stand);
	ranm(Work);
	ranm(Trans01);
	ranm(Trans10);
	ranm(FallDown);
	ranm(StandUp);

	ranmN(Attack);
	ranmN(PAttack);
	ranmN(UAttack);
	ranmN(PStand);
	ranmN2(PMotionL,Motion_L);
	ranmN2(PMotionR,Motion_R);
	ranmN(PMotionLB);
	ranmN(PMotionRB);
	ranmN(PMiniStepL);
	ranmN(PMiniStepR);
	ranmN(PMiniStepLB);
	ranmN(PMiniStepRB);
	ranmN2(BuildLo,BuildLo_);


	ranm(StandHi);
	ranm(StandLo);
	ranm(Build);
	ranm(BuildHi);
	ranm(Damage);

	ranm(Rest);
	ranm(Rest1);
	ranm(Rest2);
	ranm(Rest3);

	ranm(RotateR);
	ranm(RotateL);

	ranm(WorkTree);
	ranm(WorkStone);
	ranm(WorkField);

	ranm(TransX3);
	ranm(Trans3X);
	ranm(RotateAtPlace);
	ranm(Greeting);
	ranm(Scare);

	ranm(RestA1);

	for(int i=0;i<10;i++){
		for(int j=0;j<10;j++){
            char cc[32];
			sprintf(cc,"TRANS%d%d",i,j);
            E->Add(cc,anm_Trans+i+j*10);
		}
	}
	ranmN(Temp);
	Enumerator* DE=ENUM.Get("DIES_ANIMATIONS");
	int N=E->GetAmount();
	for(int i=0;i<N;i++){
		char s[64];
		s[0]='#';
		strcpy(s+1,E->GetStr(i));
		DE->Add(s,E->GetVal(i));
	}
}
int Nation::GetNationlKillingExpirience(){
	int N=0;
	for(int i=0;i<NMon;i++){
		N+=NErased[i]*Mon[i]->newMons->Ves;
	}
	return N;
}