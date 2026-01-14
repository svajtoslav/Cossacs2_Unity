#include "stdheader.h"
#include "WeaponSystem.h"
#include "UnitAbility.h"
#include "HeroAbility.h"
#include "UF_NumericalReturner.h"
//==================================================================================================================//
#define ToRealCoord 0
#define ToPixelCoord (ToRealCoord+4)
#define TRUETIME (AnimTime*10/64)
extern int TrueTime;
extern int AnimTime;
word GetDir(int,int);
typedef bool tpUnitsCallback(OneObject* OB,void* param);
int PerformActionOverUnitsInRadius(int xc,int yc,int R,tpUnitsCallback* CB,void* Param);
typedef bool cbCheckSprite(OneSprite* OS, void* Param);
int GetSpritesInRadius(int x, int y, int Radius, cbCheckSprite* cbF, void* Param);
extern HeroVariableStorage* CurrentHeroAbility;
HeroVariableStorage* GetHeroVariableStorage(OneObject* OB);
DLLEXPORT void OBJ_ChangeNation(OneObject* OB, byte DstNat);
extern int GetHeight(int x, int y);
void EraseSprite(int Index);
int GetBar3DHeight(int x,int y);
int GetBar3DOwner(int x,int y);
bool GetObjectVisibilityInFog(int x,int y,int z,OneObject* OB);
void PushUnitBack(OneObject* OB,byte OrdType,int Force, int EpicenterX, int EpicenterY);
int GetPointToLineDist(int x,int y,int x1,int y1,int x2,int y2);
void DetonateUnit(OneObject* OB,int CenterX,int CenterY,int Force);

//==================================================================================================================//
TargetDesignation::TargetDesignation()
{
	UnitIndex=-1;
	x=0;
	y=0;
	z=0;
}
//==================================================================================================================//
AdditionalWeaponParams::AdditionalWeaponParams()
{
	Damage=0;
	Radius=0;
	NI=0;
	N=0;
}
//==================================================================================================================//
WeaponParams::WeaponParams()
{
	WeaponModificatorP=NULL;
	OwnerWeaponIndex=-1;
	//Damage=0;
	BirthTime=TRUETIME;
	LastMoveTime=TRUETIME;
	TraveledDistance=0;
	x=0;
	y=0;
	z=0;
	NeedDelete=false;
	Dir=0;
	CheckHero=false;
	HVS=NULL;
	OnceProcesed=false;
}
bool WeaponParams::Process()
{
	CurrentHeroAbility=GetHeroStorage();
	if(WeaponModificatorP==NULL)
	{
		Enumerator* En = ENUM.Get("WeaponModificatorEnum");
		WeaponModificatorP = (WeaponModificator*)(En->Get(WeaponModificatorName.str));
	}
	if((int)WeaponModificatorP!=-1)
	{
		return WeaponModificatorP->Process(this);
		OnceProcesed=true;
	}
	return false;
}
bool WeaponParams::Draw()
{
	if(WeaponModificatorP==NULL&&!WeaponModificatorName.isClear())
	{
		Enumerator* En = ENUM.Get("WeaponModificatorEnum");
		WeaponModificatorP = (WeaponModificator*)(En->Get(WeaponModificatorName.str));
	}
	if(WeaponModificatorP&&(int)WeaponModificatorP!=-1&&IsOnScreen())
	{
		return WeaponModificatorP->Draw(this);
	}
	return false;
}
bool WeaponParams::IsOnScreen()
{
	int x0=mapx<<5;
	int y0=(mapy)<<4;
	int Lx=smaplx<<5;
	int Ly=(smaply)<<4;
	
	//int x1=x0+Lx;
	//int y1=y0+Ly;

	int zz=z>>ToPixelCoord;
	int ry=((y>>ToPixelCoord)>>1)-y0;
	int ry1=ry-zz;
	int rx=(x>>ToPixelCoord)-x0;
	
	const int SZ=128;

	if(ry1>-SZ&&ry1<Ly+SZ&&rx>-SZ&&rx<Lx+SZ)
	{
		if(GetObjectVisibilityInFog(x>>ToPixelCoord,y>>ToPixelCoord,z>>ToPixelCoord,NULL))
		{
			return true;
		}
	}
	return false;
}
HeroVariableStorage* WeaponParams::GetHeroStorage()
{
	HeroVariableStorage* rez=HVS;
	if(!CheckHero)
	{
		if(From.UnitIndex!=0xFFFF)
		{
			HVS=GetHeroVariableStorage(Group[From.UnitIndex]);
		}
		CheckHero=true;
	}
	return rez;
}
//==================================================================================================================//
bool PointModificator::MakeOneStep(WeaponParams* WP)
{
	return false;
}
bool PointModificator::CanDraw(WeaponParams* WP)
{
	return false;
}
bool PointModificator::Draw(WeaponParams* WP)
{
	return false;
}
//==================================================================================================================//
bool WeaponEvent::Check(WeaponParams* WP)
{
	return false;
}
//==================================================================================================================//
int WeaponModificatorList::GetExpansionRules()
{
	return 2; 
}
bool WeaponProcess::Check(WeaponParams* WP)
{
	if(Event.Get())
	{
		return Event.Get()->Check(WP);
	}
	return false;
}
bool WeaponProcess::Process(WeaponParams* WP)
{
	bool rez=false;
	int n = WeaponModificators.GetAmount();
	for(int i=0;i<n;i++)
	{
		bool prr = WeaponModificators[i]->MakeOneStep(WP);
		if(prr&&(!rez))rez=true;
	}
	return rez;
}
bool WeaponProcess::CanDraw(WeaponParams* WP)
{
	bool rez=false;
	int n = WeaponModificators.GetAmount();
	for(int i=0;i<n;i++)
	{
		bool prr = WeaponModificators[i]->CanDraw(WP);
		if(prr&&(!rez))rez=true;
	}
	return rez;
}
bool WeaponProcess::Draw(WeaponParams* WP)
{
	bool rez=false;
	int n = WeaponModificators.GetAmount();
	for(int i=0;i<n;i++)
	{
		bool prr = WeaponModificators[i]->Draw(WP);
		if(prr&&(!rez))rez=true;
	}
	return rez;
}
//==================================================================================================================//
WeaponModificator::WeaponModificator()
{
	WOwner=NULL;
}
bool WeaponModificator::Process(WeaponParams* WP)
{
	bool rez=false;
	int n = WPL.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(WPL[i]->Check(WP))
		{
			bool prr = WPL[i]->Process(WP);
			if(prr&&(!rez))rez=true;
		}
	}
	return rez;
}
bool WeaponModificator::Draw(WeaponParams* WP)
{
	bool rez=false;
	int n = WPL.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(WPL[i]->CanDraw(WP)&&WPL[i]->Check(WP))
		{
			bool prr = WPL[i]->Draw(WP);
			if(prr&&(!rez))rez=true;
		}
	}
	return rez;
}
const char* WeaponModificator::GetThisElementView(const char* LocalName){
	if(Name.str){
		static char cc[256];
		sprintf(cc,"%s: {CW}%s{C}",LocalName,Name.str);
		return cc;
	}else return LocalName;
}
//==================================================================================================================//
WeaponSystem::WeaponSystem()
{
	LastSerial=0;
}
void WeaponSystem::RefreshEnumerator()
{
	int n=AllWeaponModificators.GetAmount();
	Enumerator* En = ENUM.Get("WeaponModificatorEnum");
	En->Clear();
	//Weapons.Clear();
	for(int i=0;i<n;i++)
	{
		if(AllWeaponModificators[i]->Name.L)
		{
			En->Add(AllWeaponModificators[i]->Name.str,(DWORD)(AllWeaponModificators[i]));
			/*
			if(i<Weapons.GetAmount())
			{
				Weapons[i].Modificator=AllWeaponModificators[i];
			}
			else
			{
				Weapon W;
				W.Modificator=AllWeaponModificators[i];
				Weapons.Add(W);
			}
			*/
		}
	}
	n = ActiveWeapons.GetAmount();
	for(i=0;i<n;i++)
	{
		ActiveWeapons[i]->WeaponModificatorP=NULL;
	}
}
void WeaponSystem::Process()
{
	int n=ActiveWeapons.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(ActiveWeapons[i]&&(!ActiveWeapons[i]->NeedDelete))
		{
			if(ActiveWeapons[i]->BirthTime<TRUETIME)
				ActiveWeapons[i]->Process();
		}
		else
		{
			if(ActiveWeapons[i])
			{
				delete (ActiveWeapons[i]);
				ActiveWeapons[i]=NULL;
			}
			ActiveWeapons.Del(i,1);
			i--;
			n--;
		}
	}
}
void WeaponSystem::Draw()
{
	int n=ActiveWeapons.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(ActiveWeapons[i]&&(!ActiveWeapons[i]->NeedDelete))
			ActiveWeapons[i]->Draw();
	}
}
bool WeaponSystem::LoadAllWeaponModificators(char* FileName)
{
	if(FileName)
	{
		xmlQuote Inf;
		if(Inf.ReadFromFile(FileName))
		{
			AllWeaponModificators.Clear();
			ErrorPager Error;
			if(AllWeaponModificators.Load(Inf,&AllWeaponModificators,&Error))
			{
				RefreshEnumerator();
				return true;
			}
		}
	}
	return false;
}
void WeaponSystem::ClearAllActiveWeapons()
{
	ActiveWeapons.Clear();
}
void WeaponSystem::AddActiveWeapon(WeaponParams* W)
{
	int z=W->z>>ToPixelCoord;
    int x=W->x>>ToPixelCoord;
	int y=W->y>>ToPixelCoord;
	int H=GetTotalHeight(x,y);
	if(H<0)H=0;
	if(z<H)W->z=H<<ToPixelCoord;
	W->Serial=++LastSerial;
	ActiveWeapons.Add(W);
}
//==================================================================================================================//
//==================================================================================================================//
WeaponSystem GameWeaponSystem;
BaseClass* GetWeaponClass(){
	return &GameWeaponSystem.AllWeaponModificators;
}
bool ProcessWeaponClass(ClassEditor* CE,BaseClass* BC,int Options){
	if(Options==2)GameWeaponSystem.RefreshEnumerator();
	return false;
}
//==================================================================================================================//
void DrawWeaponSystem()
{
	GameWeaponSystem.Draw();
} 
void ProcessWeaponSystem()
{
	GameWeaponSystem.Process();
}
void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, AdditionalWeaponParams* AddParams)
{
	WeaponParams* P = new WeaponParams();
	//P->WeaponModificatorP=Weap->Modificator;
	P->WeaponModificatorName.Clear();
	P->WeaponModificatorName.Add(WMName);
	P->From.UnitIndex=Index;
	if(Index==0xFFFF)P->From.UnitIndex=-1;
	P->From.x=sx<<ToPixelCoord;
	P->From.y=sy<<ToPixelCoord;
	P->From.z=sz<<ToPixelCoord;

	P->x=P->From.x;
	P->y=P->From.y;
	P->z=P->From.z;

	P->AdditionalParams.Set(AddParams);
	//P->Damage=Damage;
	//P->AttType=AttType;

	P->To.UnitIndex=DestIndex;
	if(DestIndex==0xFFFF)
	{
		P->To.UnitIndex=-1;
		if(P->From.UnitIndex!=-1)
		{
			OneObject* OB = Group[P->From.UnitIndex];
			if(OB&&OB->EnemyID!=0xFFFF)
			{
				P->To.UnitIndex=OB->EnemyID;
			}
		}
	}
	P->To.x=dx<<ToPixelCoord;
	P->To.y=dy<<ToPixelCoord;
	P->To.z=dz<<ToPixelCoord;

	GameWeaponSystem.AddActiveWeapon(P);
}
void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, int Damage, int AttType)
{
	AdditionalWeaponParams* AddParams = new  AdditionalWeaponParams();
	AddParams->Damage=Damage;
	AddParams->AttType=AttType;
	CreateNewActiveWeapon(WMName,Index,sx,sy,sz,DestIndex,dx,dy,dz,AddParams);
}

void CreateNewActiveWeapon(Weapon* Weap,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, int Damage, int AttType)
{
	if(Weap->Modificator&&Weap->Modificator->Get())
		CreateNewActiveWeapon(Weap->Modificator->Get()->Name.str,Index,sx,sy,sz,DestIndex,dx,dy,dz,Damage,AttType);
}

bool LoadAllWeaponModificators(char* FileName)
{
	return GameWeaponSystem.LoadAllWeaponModificators(FileName);
}
Weapon* GetWeaponWithModificator(char* Name)
{
	/*
	Enumerator* En = ENUM.Get("WeaponModificatorEnum");
	if(En)
	{
		WeaponModificator* WM = (WeaponModificator*)(En->Get(Name));
		if((int)WM!=-1)
		{
			int n=GameWeaponSystem.Weapons.GetAmount();
			for(int i=0;i<n;i++)
			{
				if(GameWeaponSystem.Weapons[i].Modificator==WM)
					return &GameWeaponSystem.Weapons[i];
			}
		}
	}
	return NULL;
	*/
	Weapon* rez=NULL;
	int n=GameWeaponSystem.Weapons.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(!strcmp(GameWeaponSystem.Weapons[i]->Modificator->GetObjectName(),Name))
		{
			rez=GameWeaponSystem.Weapons[i];
			break;
		}
	}
	if(!rez)
	{
		Weapon* W = new Weapon();
		GameWeaponSystem.Weapons.Add(W);
		rez=GameWeaponSystem.Weapons[n];
		rez->Modificator = new ClassRef<WeaponModificator>;
		rez->Modificator->SetObjectName(Name);
		WeaponModificator* WM=rez->Modificator->Get();
		if(WM)WM->WOwner=W;

	}
	return rez;
}
Weapon* GetWeaponFromModificator(WeaponModificator* WM){
	if(WM->WOwner)return WM->WOwner;
	Weapon* W = new Weapon();	
	int n=GameWeaponSystem.Weapons.GetAmount();
	GameWeaponSystem.Weapons.Add(W);
	W=GameWeaponSystem.Weapons[n];
	W->Modificator = new ClassRef<WeaponModificator>;
	W->Modificator->Set(WM);    		
	WM->WOwner=W;
	return W;
}
int GetNNewWeap(){
	return GameWeaponSystem.Weapons.GetAmount();
}
char* GetNewWeapName(int idx){
	return (char*)GameWeaponSystem.Weapons[idx]->Modificator->GetObjectName();
}
int GetNewWeapIdx(char* name){
	int n=GameWeaponSystem.Weapons.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(!strcmp(GameWeaponSystem.Weapons[i]->Modificator->GetObjectName(),name))return i;		
	}
	return -1;
}
Weapon* GetNewWeaponPtr(int idx){
	return GameWeaponSystem.Weapons[idx];
}
//==================================================================================================================//
extern int ItemChoose;
bool MMItemChoose(SimpleDialog* SD);

void ProcessWeaponSystemEditor(){
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
				CE.CreateFromClass(Desk,0,0,Desk->x1-Desk->x,Desk->y1-Desk->y,&GameWeaponSystem,3,"EmptyBorder");
				do{
					GameWeaponSystem.RefreshEnumerator();
					ProcessMessages();					
					DSS.ProcessDialogs();
					CE.Process();
					DSS.RefreshView();
				}while(ItemChoose==-1);
			}
		}
	}	
}
//==================================================================================================================//
//==================================================================================================================//
DrawOne::DrawOne()
{
	Frame=0;
	ScaleByRadius=0;
}
bool DrawOne::CanDraw(WeaponParams* WP)
{
	return WP->IsOnScreen();
}
void AddAnimation(int x,int y,int z,NewAnimation* ANM,int Frame,float Dir,DWORD Diffuse,OneObject* OB,float Scale,float fiDir,float fiOrt,DWORD Handle);
bool DrawOne::Draw(WeaponParams* WP)
{
	float Scale=1.0;
	if(ScaleByRadius!=0&&WP->AdditionalParams.Get()&&WP->AdditionalParams.Get()->Radius!=0)
	{
		Scale=((float)WP->AdditionalParams.Get()->Radius)/((float)ScaleByRadius);
	}
	AddAnimation(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord,WP->z>>ToPixelCoord,&Anim,Frame,
					WP->Dir,0xFF808080,NULL,Scale,
                    WP->fiDir, WP->fiOrt, 
                    WP->Serial);
    Anim.Code = 0xBAADF00D;
	return true;
}
//==================================================================================================================//
bool SelfMurder::MakeOneStep(WeaponParams* WP)
{
	if (WP) 
	{
		WP->NeedDelete=true;
		return true;
	}
	return false;
}
//==================================================================================================================//
StaticMotion::StaticMotion()
{
	Vx=0;
	Ax=0;
	Vy=0;
	Ay=0;
	Vz=0;
	Az=0;
	FirstStep=true;
}
bool StaticMotion::MakeOneStep(WeaponParams* WP)
{
	Dir=GetDir(WP->To.x-WP->From.x,WP->To.y-WP->From.y); 
	int n=Norma(WP->To.x-WP->From.x,WP->To.y-WP->From.y);
	int DirZ=GetDir(n,WP->To.z-WP->From.z);

	Vmx=(Vx*TCos[Dir]+Vy*TSin[Dir])>>8;
	Vmy=(Vx*TSin[Dir]-Vy*TCos[Dir])>>8;
	Amx=(Ax*TCos[Dir]+Ay*TSin[Dir])>>8;
	Amy=(Ax*TSin[Dir]-Ay*TCos[Dir])>>8;
	
	int ti=TRUETIME-WP->BirthTime;
	
	int Vxx=Vmx+Amx*ti/100;
	int Vyy=Vmy+Amy*ti/100;
	int nV=Norma(Vxx,Vyy);
	int Vzz=Vz+Az*ti/100;
	WP->V=Norma(Vzz,nV);
	Vzz+=((nV*TCos[DirZ])>>8);
	WP->Dir=GetDir(Vxx,Vyy);
	WP->DirZ=DirZ;

	WP->x=WP->From.x+Vmx*ti/100+Amx*ti*ti/20000;
	WP->y=WP->From.y+Vmy*ti/100+Amy*ti*ti/20000;
	WP->z=WP->From.z+Vzz*ti/100+Az*ti*ti/20000;
	WP->LastMoveTime=TRUETIME;
	return true;
}
//==================================================================================================================//
BalisticMotion::BalisticMotion()
{
	ConstSpeed=0;
	ConstHieght=0;
	SetTargetHieghtOnGround=false;
	StopInDestPoint=false;
}
bool BalisticMotion::MakeOneStep(WeaponParams* WP)
{
	byte Dir=GetDir(WP->To.x-WP->From.x,WP->To.y-WP->From.y); 
	float sq_norma(float x,float y);
	int S=sq_norma(WP->To.x-WP->From.x,WP->To.y-WP->From.y);
	
	if(S==0&&StopInDestPoint)
		return true;

	int Vmx=0;//(Vx*TCos[Dir]+Vy*TSin[Dir])>>8;
	int Vmy=0;//(Vx*TSin[Dir]-Vy*TCos[Dir])>>8;
	int Vmz=0;
	int Amz=-g;

    int ti=TRUETIME-WP->BirthTime;
	int MaxFlyTime=0;

    int V = 0;
    float Vz = 0.0f;
	if (SetTargetHieghtOnGround) 
	{
		WP->To.z=GetHeight(WP->To.x>>4,WP->To.y>>4)<<4;
		if (WP->To.z < 0) WP->To.z = 0;
	}

	if (ConstSpeed)
	{
		int mt=S*100/ConstSpeed;
		if(mt==0)
			mt=1;
		Vmz=g*mt/200+100*(WP->To.z-WP->From.z)/mt;
		Vmx=(ConstSpeed*TCos[Dir])>>8;
		Vmy=(ConstSpeed*TSin[Dir])>>8;
        V  = ConstSpeed;
        //  current z velocity component
        Vz = Vmz + Amz*ti/100;
		
		MaxFlyTime=mt;
	} 
    else 
	if (ConstHieght)
	{
		int t=sqrt(2*ConstHieght/g);
		if(!t)
			t=1;
		Vmz=ConstHieght/t+(WP->To.z-WP->From.z)/t;
		V=S/t+(WP->To.z-WP->From.z)/t;
		Vmx=(V*TCos[Dir])>>8;
		Vmy=(V*TSin[Dir])>>8;

		MaxFlyTime=t*2;
	}
	
	WP->x = WP->From.x + Vmx*ti/100;
	WP->y = WP->From.y + Vmy*ti/100;
	WP->z = WP->From.z + Vmz*ti/100 + Amz*ti*ti/20000;

	if(StopInDestPoint&&MaxFlyTime<=ti)
	{
		WP->x = WP->To.x;
		WP->y = WP->To.y;
		WP->z = WP->To.z;
	}
    
    //  4.08.2004, by Silver 
    WP->fiDir = c_DoublePI - atan2f( Vz, static_cast<float>( V ) );
    WP->fiOrt = 0.0f;  // assume missile is not rotating around its axis 

    WP->LastMoveTime=TRUETIME;
	WP->Dir=GetDir(Vmx,Vmy);
	//WP->DirZ=GetDir(Norma(Vmx*ti/100,Vmy*ti/100),Vmz*ti/100+Amz*ti*ti/20000);
	return true;
}
//==================================================================================================================//
Jump::Jump()
{
	JumpDist=0;
	JumpToEnd=false;
}
bool Jump::MakeOneStep(WeaponParams* WP)
{
	bool rez=false;
	if(WP)
	{
		if(JumpToEnd)
		{
			WP->x=WP->To.x;
			WP->y=WP->To.y;
			WP->z=WP->To.z;
		}
		else
		if(JumpDist)
		{
			int dx=WP->To.x-WP->x;
			int dy=WP->To.y-WP->y;
			int dz=WP->To.z-WP->z;
			int sd=abs(dx)+abs(dy)+abs(dz);
			int ReD=JumpDist<<ToPixelCoord;
			dx=(dx*ReD)/sd;
			dy=(dy*ReD)/sd;
			dz=(dz*ReD)/sd;
			WP->x=WP->x+dx;
			WP->y=WP->y+dy;
			WP->z=WP->z+dz;
		}
		rez=true;
	}
	return rez;
}
//==================================================================================================================//
bool Motion::MakeOneStep(WeaponParams* WP)
{
	int ti=TRUETIME-WP->LastMoveTime;
	WP->x=WP->x+((WP->V*ti*TCos[WP->Dir])>>8)/100;
	WP->y=WP->y+((WP->V*ti*TSin[WP->Dir])>>8)/100;
	WP->z=WP->z+((WP->V*ti*TSin[WP->DirZ])>>8)/100;
	WP->LastMoveTime=TRUETIME;
	return true;
}
//==================================================================================================================//
HarmonicMotion::HarmonicMotion()
{
	Hx=0;
	Tx=0;
	Dx=0;
	Hy=0;
	Ty=0;
	Dy=0;
	Hz=0;
	Tz=0;
	Dz=0;
}
bool HarmonicMotion::MakeOneStep(WeaponParams* WP)
{
	int ti=TRUETIME-WP->BirthTime;
	int dx=0;
	if(Tx)
	{
		byte d=(byte)(((ti%Tx)*256)/Tx+Dx);
		dx=(TSin[d]*Hx)>>8;
	}
	int dy=0;
	if(Ty)
	{
		byte d=(byte)(((ti%Ty)*256)/Ty+Dy);
		dy=(TSin[d]*Hy)>>8;
	}
	int dz=0;
	if(Tz)
	{
		byte d=(byte)(((ti%Tz)*256)/Tz+Dz);
		dz=(TSin[d]*Hz)>>8;
	}

	int dxx=(dx*TCos[WP->Dir]+dy*TSin[WP->Dir])>>8;
	int dyy=(dx*TSin[WP->Dir]-dy*TCos[WP->Dir])>>8;

	WP->x+=dxx;
	WP->y+=dyy;
	WP->z+=dz;
	return true;
}
//==================================================================================================================//
FollowUnit::FollowUnit()
{
	F=0;
}
bool FollowUnit::MakeOneStep(WeaponParams* WP)
{
	if(WP->To.UnitIndex!=-1&&WP->To.UnitIndex!=0xFFFF)
	{
		OneObject* OB = Group[WP->To.UnitIndex];
		if(OB)
		{
			WP->To.x=OB->RealX<<ToRealCoord;
			WP->To.y=OB->RealY<<ToRealCoord;
			WP->To.z=OB->RZ<<ToPixelCoord;
			int dx=WP->To.x-WP->x;
			int dy=WP->To.y-WP->y;
			int dz=WP->To.z-WP->z;
			if(F==0)
			{
				WP->Dir=GetDir(dx,dy);
				WP->DirZ=GetDir(Norma(dx,dy),dz);
			}
			/*
			WP->To.x=OB->RealX<<ToRealCoord;
			WP->To.y=OB->RealY<<ToRealCoord;
			//WP->To.z=OB->RZ<<ToPixelCoord;
			int N1=sqrt(WP->Vx*WP->Vx+WP->Vy*WP->Vy+WP->Vz*WP->Vz);
			int nVx = 0;
			int nVy = 0;
			int nVz = 0;

			if(N1)
			{
				nVx = (WP->Vx*10000)/N1;
				nVy = (WP->Vy*10000)/N1;
				nVz = (WP->Vz*10000)/N1;
			}

			int dx=WP->To.x-WP->x;
			int dy=WP->To.y-WP->y;
			int dz=WP->To.z-WP->z;
			int N2=sqrt(dx*dx+dy*dy+dz*dz);
			
			int nDx = 0;
			int nDy = 0;
			int nDz = 0;

			if(N2)
			{
				nDx = (dx*10000)/N2;
				nDy = (dy*10000)/N2;
				nDz = (dz*10000)/N2;
			}

			if(F==0)
			{
				WP->Vx=(nDx*N1)/10000;
				WP->Vy=(nDy*N1)/10000;
				WP->Vz=(nDz*N1)/10000;
			}
			else
			{
				int dX=nVx-nDx;
				int dY=nVy-nDy;
				int dZ=nVz-nDz;

				WP->Vx+=(dX*F)/200;
				WP->Vy+=(dY*F)/200;
				WP->Vz+=(dZ*F)/200;
			}
			WP->Dir=GetDir(WP->Vx,WP->Vy);
			*/
			return true;
		}
	}
	return false;
}
//==================================================================================================================//
BirthNew::BirthNew()
{
	DamageChange=100;
	BirthPause=0;
	LeaveFromPoint=false;
	Fr_AddX=0;
	Fr_AddY=0;
	Fr_AddZ=0;
	To_RandomUnitInRadius=0;
	To_RandomPosInRadius=0;
	To_AddX=0;
	To_AddY=0;
	To_AddZ=0;
}
bool BirthNew::MakeOneStep(WeaponParams* WP)
{
	if(WP)
	{
		WeaponParams* NWP = new WeaponParams();
		Enumerator* En = ENUM.Get("WeaponModificatorEnum");
		if(En)
		{
			WeaponModificator* WM = (WeaponModificator*)(En->Get(NewWeaponModificator.str));
			if((int)WM==-1)return false;
			NWP->WeaponModificatorP=WM;
			NWP->WeaponModificatorName.Clear();
			NWP->WeaponModificatorName.Add(NewWeaponModificator.str);
		}
		if(WP->AdditionalParams.Get())
		{
			NWP->AdditionalParams.Set(new AdditionalWeaponParams());
			WP->AdditionalParams.Get()->Copy(NWP->AdditionalParams.Get());
			NWP->AdditionalParams.Get()->Damage=(WP->AdditionalParams.Get()->Damage*DamageChange)/100;
		}
		NWP->OwnerWeaponIndex=WP->OwnerWeaponIndex;
		NWP->BirthTime+=BirthPause;
		NWP->From.UnitIndex=WP->From.UnitIndex;
		if(LeaveFromPoint)
		{
			NWP->From.x=WP->From.x;
			NWP->From.y=WP->From.y;
			NWP->From.z=WP->From.z;
		}
		else
		{
			NWP->From.x=WP->x;
			NWP->From.y=WP->y;
			NWP->From.z=WP->z;
		}
		NWP->From.x+=(Fr_AddX<<ToPixelCoord);
		NWP->From.y+=(Fr_AddY<<ToPixelCoord);
		NWP->From.z+=(Fr_AddZ<<ToPixelCoord);
		
		
		NWP->To.x=WP->To.x;
		NWP->To.y=WP->To.y;
		NWP->To.z=WP->To.z;
		if(To_RandomPosInRadius)
		{
			int dx=rando()%To_RandomPosInRadius;
			int dy=rando()%To_RandomPosInRadius;
			NWP->To.x+=dx-To_RandomPosInRadius/2;
			NWP->To.y+=dy-To_RandomPosInRadius/2;
		}
		NWP->x=NWP->From.x;
		NWP->y=NWP->From.y;
		NWP->z=NWP->From.z;

		NWP->To.UnitIndex=WP->To.UnitIndex;

		GameWeaponSystem.AddActiveWeapon(NWP);
		return true;
	}
	return false;
}
//==================================================================================================================//
bool TargetFinder::GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD)
{
	return false;
}

//==================================================================================================================//
bool ParseAndFillExpression(Operand* OP,DString* DS);
int UserFriendlyNumericalReturner_editor::CreateControl(ParentFrame* Base,int x,int y,int x1,int y1, BaseClass* Class,void* DataPtr,void* ExtraPtr, ControlParams* CParam)
{
	IB=Base->addInputBox(NULL,x,y,str,120,x1-x+1,y1-y+1,&CED_Font,&CED_AFont);
	NR=(UserFriendlyNumericalReturner*)DataPtr;
	strcpy(str2,str);
	return y1;
}
bool UserFriendlyNumericalReturner_editor::Assign(xmlQuote* xml)
{
	if(NR)
	{
		DString DS;
		NR->Value.GetAssembledView(DS,false);
		if(DS.str)
		{
			strcpy(str,DS.str);
			strcpy(str2,str);
		}
	}
	return true;
}
bool ParseAndFillExpression(Operand* OP,DString* DS);
int UserFriendlyNumericalReturner_editor::Get(xmlQuote* xml)
{
	if(strcmp(str,str2))
	{
		strcpy(str2,str);
		DString DS=str;
		if(NR->Value.Op.Get())
		{
			delete (NR->Value.Op.Get());
			NR->Value.Op.Set(NULL);
		}
		
		if(Parser.ParseAndFillExpression(&NR->Value,&DS))
		{
			IB->AFont=&YellowFont;
		}
		else
		{
			IB->AFont=&RedFont;
		}
		/*
		if(ParseAndFillExpression((Operand*) &NR->Value,&DS))
		{
			int g=0;
		}
		*/
	}
	return true;
}
//==================================================================================================================//
UnitsInRadius::UnitsInRadius()
{
	//Radius=0;
	//MaxUnits=0;
	Frendly=0;
	Enemy=0;
	FillList=false;
	Owner=0xFFFF;
}
bool UnitsInRadius::GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD)
{
	bool rez=false;
	if(WP&&TD)
	{
		if(N==0)
		{
			FindedUnits.Clear();
			int n=PerformActionOverUnitsInRadius(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord,Radius.Get(),&UnitsInRadius::CheckUnitsInRadius,(void*)this);
			Owner=WP->From.UnitIndex;
			FillList=true;
		}
		int n=FindedUnits.GetAmount();
		if(N<n&&N<MaxUnits.Get())
		{
			OneObject* OB = Group[FindedUnits[N]];
			if(OB)
			{
				TD->UnitIndex=OB->Index;
				TD->x=OB->RealX<<ToRealCoord;
				TD->y=OB->RealY<<ToRealCoord;
				TD->z=OB->RZ<<ToPixelCoord;
				rez=true;
			}
		}
	}
	if(!rez)FillList=false;
	return rez;
}
bool UnitsInRadius::CheckUnitsInRadius(OneObject* OB,void* param)
{
	bool rez=false;
	if(OB&&param)
	{
		UnitsInRadius* Ui = (UnitsInRadius*)param;
		if(!OB->Sdoxlo)
		{
			OneObject* OW=NULL;
			if(Ui->Owner!=0xFFFF)OW=Group[Ui->Owner];
			if(Ui->Frendly&&OW&&(OW->NMask&OB->NMask))
				rez=true;
			if(Ui->Enemy&&OW&&(!(OW->NMask&OB->NMask)))
				rez=true;
			if(!Ui)
				rez=true;
		}
		if(rez)Ui->FindedUnits.Add(OB->Index);
	}
	return rez;
}
//==================================================================================================================//
bool RandomPosInRadius::GetTargetDesignation(WeaponParams* WP, int Nn,TargetDesignation* TD)
{
	bool rez=false;
	int NN=0;
	int Rad=0;
	//if(WP->AdditionalParams.Get())
	//{
	//	NN=WP->AdditionalParams.Get()->N;
	//	Rad=WP->AdditionalParams.Get()->Radius;
	//}
	//else
	{
		NN=N.Get();
		Rad=Radius.Get();
	}
	if(Nn<NN&&Rad)
	{
		int dx=Rad-rando()%(Rad*2);
		int dy=Rad-rando()%(Rad*2);
		TD->x=WP->x+(dx<<ToPixelCoord);
		TD->y=WP->y+(dy<<ToPixelCoord);
		TD->z=WP->z;	
		rez=true;
	}
	return rez;
}
bool UserDefinedPoints::GetTargetDesignation(WeaponParams* WP, int N,TargetDesignation* TD){
	if(N<Points.GetAmount()){
        int R=Radius.Get();
        TD->x=WP->x+(int(Points[N]->x*R)<<ToPixelCoord);
		TD->y=WP->y+(int(Points[N]->y*R)<<ToPixelCoord);
		TD->z=WP->z+(int(Points[N]->z*R)<<ToPixelCoord);
		return true;
	}else return false;
}
//==================================================================================================================//
MassBirthNew::MassBirthNew()
{
	DamageChange=100;
	BirthPause=0;
	LeaveFromPoint=false;
	NewWeaponModificator="";
}
bool MassBirthNew::MakeOneStep(WeaponParams* WP)
{
	if(WP)
	{
		WeaponModificator* WM =NULL;
		Enumerator* En = ENUM.Get("WeaponModificatorEnum");
		if(En)
		{
			WeaponModificator* WM = (WeaponModificator*)(En->Get(NewWeaponModificator.str));
			if((int)WM==-1)return false;
		}
		WeaponParams* NWP = new WeaponParams();
		int i=0;
		while (NewTargetList.Get()->GetTargetDesignation(WP,i,&NWP->To)) 
		{
			NWP->WeaponModificatorP=WM; 
			NWP->WeaponModificatorName=NewWeaponModificator.str;
			if(WP->AdditionalParams.Get())
			{
				NWP->AdditionalParams.Set(new AdditionalWeaponParams());
				WP->AdditionalParams.Get()->Copy(NWP->AdditionalParams.Get());
				NWP->AdditionalParams.Get()->Damage=(WP->AdditionalParams.Get()->Damage*DamageChange)/100;
			}
			NWP->OwnerWeaponIndex=WP->OwnerWeaponIndex; 
			NWP->BirthTime+=BirthPause;
			NWP->From.UnitIndex=WP->From.UnitIndex;
			if(LeaveFromPoint)
			{
				NWP->From.x=WP->From.x;
				NWP->From.y=WP->From.y;
				NWP->From.z=WP->From.z;
			}
			else
			{
				NWP->From.x=WP->x;
				NWP->From.y=WP->y;
				NWP->From.z=WP->z;
			}
			NWP->x=WP->x;
			NWP->y=WP->y;
			NWP->z=WP->z;
			NWP->V=100;//WP->V;
			GameWeaponSystem.AddActiveWeapon(NWP);
			i++;
			NWP = new WeaponParams();
		}
		delete NWP;
		return true;
	}
	return false;
}
//==================================================================================================================//
ChangeModificator::ChangeModificator()
{
	LeaveFromPoint=false;
	Wm=NULL;
	NewWeaponModificator="";
	CheckedName="";
}
bool ChangeModificator::MakeOneStep(WeaponParams* WP)
{
	if(strcmp(CheckedName.str,NewWeaponModificator.str))
	{
		Enumerator* En = ENUM.Get("WeaponModificatorEnum");
		if(En)
		{
			WeaponModificator* WM = (WeaponModificator*)(En->Get(NewWeaponModificator.str));
			if((int)WM==-1)return false;
			CheckedName.Clear();
			CheckedName.Add(NewWeaponModificator.str);
			Wm=WM;
		}
	}
	if(Wm)
	{
		WP->WeaponModificatorP=Wm;
		WP->WeaponModificatorName.Clear();
		WP->WeaponModificatorName.Add(NewWeaponModificator.str);
		if(!LeaveFromPoint)
		{
			WP->From.x=WP->x;
			WP->From.y=WP->y;
			WP->From.z=WP->z;
			WP->BirthTime=TRUETIME;
			WP->LastMoveTime=TRUETIME;
		}
	}
	return true;
}
//==================================================================================================================//
MakeDamage::MakeDamage()
{
	OnlyTargetUnits=false;
	InRadius=1;
	OnlyEnemyUnits=false;
	MaxUnits=1000;
	PushUnitsForce=0;
}
bool MakeDamage::MakeOneStep(WeaponParams* WP)
{
	int Rad=InRadius;
	if(WP->AdditionalParams.Get()&&WP->AdditionalParams.Get()->Radius!=0)
		Rad=WP->AdditionalParams.Get()->Radius;
	if(OnlyTargetUnits)
	{
		OneObject* OW = Group[WP->From.UnitIndex];
		OneObject* TO = Group[WP->To.UnitIndex];
		if(OW&&TO&&(!TO->Sdoxlo)&&WP->AdditionalParams.Get())
		{
			TO->MakeDamage(WP->AdditionalParams.Get()->Damage,WP->AdditionalParams.Get()->Damage,OW,WP->AdditionalParams.Get()->AttType);
			if(PushUnitsForce)
				PushUnitBack(TO,1,PushUnitsForce, WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		}
	}
	else
	{
		if(DamageBuilding)
		{
			int Bd = GetBar3DOwner(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
			if(Bd!=0xFFFF)
			{
				OneObject* OW = Group[WP->From.UnitIndex];
				OneObject* OBd = Group[Bd];
				if((!OnlyEnemyUnits)||(OnlyEnemyUnits&&!(OW->NMask&OBd->NMask)))
				{
					if(OW&&OBd&&(!OBd->Sdoxlo)&&WP->AdditionalParams.Get())
					{
						OBd->MakeDamage(WP->AdditionalParams.Get()->Damage,WP->AdditionalParams.Get()->Damage,OW,WP->AdditionalParams.Get()->AttType);
					}
				}
			}
		}
		if(Rad)
		{
			int Par[10];
			Par[0]=OnlyEnemyUnits;
			Par[1]=0;
			Par[2]=MaxUnits;
			Par[3]=WP->From.UnitIndex;
			Par[4]=0;
			Par[5]=0;
			if(WP->AdditionalParams.Get())
			{
				Par[4]=WP->AdditionalParams.Get()->Damage;
				Par[5]=WP->AdditionalParams.Get()->AttType;
			}
			Par[6]=Rad;
			Par[7]=WP->x>>ToPixelCoord;
			Par[8]=WP->y>>ToPixelCoord;
			Par[9]=PushUnitsForce;
			PerformActionOverUnitsInRadius(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord,Rad+300,&MakeDamage::MakeDam,(void*)Par);
		}
	}
	return true;
}
bool MakeDamage::MakeDam(OneObject* OB,void* param)
{
	bool rez=false;
	int* Pr = (int*)param;
	int OnlyEnemyUnits=Pr[0];
	int NUnits=Pr[1];
	int MaxU=Pr[2];
	int Owner=Pr[3];
	int Damage=Pr[4];
	int AttType=Pr[5];
	if((!OB->Sdoxlo)&&(MaxU==0||NUnits<MaxU))
	{
		OneObject* OW = Group[Owner];
		if(OW){
			if(OnlyEnemyUnits)
			{
				if(OW->NMask&OB->NMask)
				{
					return false;
				}
			}
			int RX=OB->GetAttX();
			int RY=OB->GetAttY();
			int ds = Norma(Pr[7]-RX/16,Pr[8]-RY/16);
			if(ds<(Pr[6]+OB->newMons->EMediaRadius))
			{
				OB->MakeDamage(Damage,Damage,OW,AttType);
				if(Pr[9])
					PushUnitBack(OB,1,Pr[9], Pr[7], Pr[8]);
				Pr[1]++;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
Wave::Wave()
{
	H=0;
	MaxR=0;
	MinR=0;
	PushUnitsForce=0;
	LinearWidth=0;
	OnlyEnemyUnits=false;
	Damage=false;
}
bool Wave::MakeOneStep(WeaponParams* WP)
{
	int pr[2];
	pr[0]=(int)WP;
	pr[1]=(int)this;
	int ds=Norma((WP->x>>ToPixelCoord)-(WP->From.x>>ToPixelCoord),(WP->y>>ToPixelCoord)-(WP->From.y>>ToPixelCoord));
	if(ds>MinR&&ds<MaxR)
	{
		if(LinearWidth==0)
		{
			
			PerformActionOverUnitsInRadius(WP->From.x>>ToPixelCoord,WP->From.y>>ToPixelCoord,ds+100,&Wave::MakeWave,(void*)pr);
		}
		else
		{
			PerformActionOverUnitsInRadius(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord,LinearWidth/2,&Wave::MakeWave,(void*)pr);
		}
	}
	return true;
}
bool Wave::MakeWave(OneObject* OB,void* param)
{
	if(OB&&!OB->Sdoxlo)
	{
		int* p=(int*)param;
		WeaponParams* wp=(WeaponParams*)p[0];
		Wave* w=(Wave*)p[1];
		bool fr=true;
		OneObject* ovn=NULL;Group[wp->From.UnitIndex];
		if(wp->From.UnitIndex!=0xFFFF)
		{
			ovn=Group[wp->From.UnitIndex];
		}
		if(w->OnlyEnemyUnits)
		{
			fr=false;
			if(ovn&&!(ovn->NMask&OB->NMask))
			{
				fr=true;
			}
		}
		if(fr)
		{
			int ds=Norma((wp->x>>ToPixelCoord)-(wp->From.x>>ToPixelCoord),(wp->y>>ToPixelCoord)-(wp->From.y>>ToPixelCoord));
			int uds=Norma(OB->RealX/16-(wp->From.x>>ToPixelCoord),OB->RealY/16-(wp->From.y>>ToPixelCoord));
			if(w->LinearWidth)
			{
				if(ds>w->MinR&&ds<w->MaxR)
				{
					byte drr=GetDir((wp->x>>ToPixelCoord)-(wp->From.x>>ToPixelCoord),(wp->y>>ToPixelCoord)-(wp->From.y>>ToPixelCoord));
					int wht=w->LinearWidth/2;
					int x1=(wp->x>>ToPixelCoord)-((TSin[drr]*wht)>>8);
					int y1=(wp->y>>ToPixelCoord)+((TCos[drr]*wht)>>8);
					int x2=(wp->x>>ToPixelCoord)+((TSin[drr]*wht)>>8);
					int y2=(wp->y>>ToPixelCoord)-((TCos[drr]*wht)>>8);
					int lds=GetPointToLineDist(OB->RealX/16,OB->RealY/16,x1,y1,x2,y2);
					if(lds<5&&OB->OverEarth==0)
					{
						OB->OverEarth=w->H;
						if(w->Damage&&ovn)
						{
							if(wp->AdditionalParams.Get())
							{
								int damage=wp->AdditionalParams.Get()->Damage;
								int attType=wp->AdditionalParams.Get()->AttType;
								OB->MakeDamage(damage,damage,ovn,attType);
							}
						}
						if(w->PushUnitsForce)
						{
							PushUnitBack(OB,1,w->PushUnitsForce, wp->From.x>>ToPixelCoord, wp->From.y>>ToPixelCoord);
							DetonateUnit(OB,wp->From.x>>ToPixelCoord,wp->From.y>>ToPixelCoord,w->PushUnitsForce*100);
						}
					}
				}
			}
			else
			{
				if(uds>w->MinR&&uds<(w->MaxR+100))
				{
					int dw=abs(ds-uds)+1;
					if(dw<100)
					{
						OB->OverEarth=w->H/dw;
						if(w->Damage&&ovn)
						{
							if(wp->AdditionalParams.Get())
							{
								int damage=wp->AdditionalParams.Get()->Damage;
								int attType=wp->AdditionalParams.Get()->AttType;
								OB->MakeDamage(damage,damage,ovn,attType);
							}
						}
						if(dw<5&&w->PushUnitsForce)
						{
							PushUnitBack(OB,1,w->PushUnitsForce, wp->From.x>>ToPixelCoord, wp->From.y>>ToPixelCoord);
							DetonateUnit(OB,wp->From.x>>ToPixelCoord,wp->From.y>>ToPixelCoord,w->PushUnitsForce*100);
						}
					}
				}
			}
		}
	}
	return false;
}
//==================================================================================================================//
bool BirthNewUnit::MakeOneStep(WeaponParams* WP)
{
	if(WP)
	{
		if(WP->From.UnitIndex!=0xFFFF)
		{
			OneObject* OB=Group[WP->From.UnitIndex];
			if(OB)
			{
				OneObject* NOB=Group[NATIONS[OB->NNUM].CreateNewMonsterAt(WP->x,WP->y,UT.UnitType,true)];
				int NLife=UnitLife.Get();
				if(NLife)
				{
					NOB->Life=NLife;
					NOB->MaxLife=NLife;
				}
				int n=AdditionalAbilites.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(AdditionalAbilites[i]->Get())
					{
						AdditionalAbilites[i]->Get()->OnUnitBirth(NOB);
					}
				}
				return true;
			}
		}
	}
	return false;
}
//==================================================================================================================//
BirthNewUnitsFromSprites::BirthNewUnitsFromSprites()
{
	DeleteSprites=false;
}
bool BirthNewUnitsFromSprites::MakeOneStep(WeaponParams* WP)
{
	bool rez=false;
	if(WP)
	{
		int R = Radius.Get();
		int MaxU = MaxUnits.Get();
		if(WP->AdditionalParams.Get())
		{
			if(WP->AdditionalParams.Get()->Radius!=0)
				R=WP->AdditionalParams.Get()->Radius;
			if(WP->AdditionalParams.Get()->N!=0)
				MaxU=WP->AdditionalParams.Get()->N;
		}
		//typedef bool cbCheckSprite(OneSprite* OS, void* Param);
		if(WP->From.UnitIndex!=0xFFFF)
		{
			OneObject* OB=Group[WP->From.UnitIndex];
			if(OB)
			{
				int Param[4];
				Param[0]=UT.UnitType;
				Param[1]=MaxU;
				Param[2]=(int)this;
				Param[3]=OB->NNUM;
				int n = GetSpritesInRadius(WP->x>>ToPixelCoord, WP->y>>ToPixelCoord, R, &BirthNewUnitsFromSprites::CheckSprite, (void*) Param);
			}
		}
	}
	return rez;
}
bool BirthNewUnitsFromSprites::CheckSprite(OneSprite* OS,void* Param)
{
	bool rez=false;
	int* P=(int*)Param;
	if(P[1]>0)
	{
		BirthNewUnitsFromSprites* Th = (BirthNewUnitsFromSprites*)P[2];
		OneObject* NOB=Group[NATIONS[P[3]].CreateNewMonsterAt(OS->x<<4,OS->y<<4,P[0],true)];
		if(Th->DeleteSprites)
		{
			EraseSprite(OS->Index);
		}
		int NLife=Th->UnitLife.Get();
		if(NLife)
		{
			NOB->Life=NLife;
			NOB->MaxLife=NLife;
		}
		int n=Th->AdditionalAbilites.GetAmount();
		for(int i=0;i<n;i++)
		{
			if(Th->AdditionalAbilites[i]->Get())
			{
				Th->AdditionalAbilites[i]->Get()->OnUnitBirth(NOB);
			}
		}
		P[1]--;
		rez=true;
	}
	return rez;
}
//==================================================================================================================//
ChangeNation::ChangeNation()
{
	FromNI=-1;
	AnyEnemyNation=false;
	AnyFriendlyNation=false;
	ToNI=-1;
	Radius=0;
	NUnits=1000;
	OnlyTargetUnit=false;
}
bool ChangeNation::MakeOneStep(WeaponParams* WP)
{
	bool rez=false;
	int Rad=Radius;
	int NEWNI=ToNI;
	OneObject* OW = Group[WP->From.UnitIndex];
	if(OW&&ToNI==-1)
		NEWNI=OW->NNUM;
	int MaxUnits=NUnits;
	if(WP->AdditionalParams.Get())
	{
		if(WP->AdditionalParams.Get()->Radius!=-1)
			Rad=WP->AdditionalParams.Get()->Radius;
		if(WP->AdditionalParams.Get()->NI!=-1)
			NEWNI=WP->AdditionalParams.Get()->NI;
		if(WP->AdditionalParams.Get()->N!=-1)
			MaxUnits=WP->AdditionalParams.Get()->N;
	}
	int Par[8];
	Par[0]=FromNI;
	Par[1]=AnyEnemyNation;
	Par[2]=AnyFriendlyNation;
	Par[3]=NEWNI;
	Par[4]=MaxUnits;
	Par[5]=(int)this;
	Par[6]=(int)OW->NMask;
	Par[7]=OW->NNUM;
	if(OnlyTargetUnit)
	{
		OneObject* TO = Group[WP->To.UnitIndex];
		rez=bool(0<ChangeObjectNation(TO,(void*) Par));
	}
	else
	{
		rez=0<PerformActionOverUnitsInRadius(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord,Rad,&ChangeNation::ChangeObjectNation,(void*)Par);
	}
	return rez;
}
bool ChangeNation::ChangeObjectNation(OneObject* OB,void* param)
{
	bool rez=false;
	if(OB&&!OB->Sdoxlo)
	{
		int* Par=(int*)param;
		if(Par[4]>0)
		{
			bool NOK=false;
			if(Par[0]==OB->NNUM)
				NOK=true;
			else
			if(Par[1]==1&&!(OB->NMask&((byte)Par[6])))
				NOK=true;
			else
			if(Par[2]==1&&(OB->NMask&((byte)Par[6]))&&Par[7]!=OB->NNUM)
				NOK=true;
			if(NOK)
			{
				ChangeNation* CN = (ChangeNation*)Par[5];
				int n=CN->TypeList.GetAmount();
				bool TypeOK=true;
				if(n)
				{
					TypeOK=false;
					for(int i=0;i<n&&!TypeOK;i++)
					{
						if(CN->TypeList[i]->UnitType==OB->NIndex)
							TypeOK=true;
					}
				}
				if(TypeOK)
				{
					OBJ_ChangeNation(OB, Par[3]);
					Par[4]--;
				}
			}
		}
	}
	return rez;
}
//==================================================================================================================//
bool PlaySomeSound::MakeOneStep(WeaponParams* WP){
	if(SoundID>0){
		extern CDirSound* CDS;
		CDS->HitSound(SoundID);
		AddEffect(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord,SoundID);		
	}
	return true;
}
//==================================================================================================================//
bool True::Check(WeaponParams* WP)
{
	return true;
}
//==================================================================================================================//
bool IsTargetDie::Check(WeaponParams* WP)
{
	bool rez=true;
	if(WP)
	{
		if(WP->To.UnitIndex!=0xFFFF)
		{
			OneObject* OB = Group[WP->To.UnitIndex];
			if(OB&&!OB->Sdoxlo)
			{
				rez=false;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
IsTargetInvisible::IsTargetInvisible()
{
	Not=true;
}
bool IsTargetInvisible::Check(WeaponParams* WP)
{
	bool rez=false;
	if(WP)
	{
		if(WP->To.UnitIndex!=0xFFFF)
		{
			OneObject* OB = Group[WP->To.UnitIndex];
			if(OB&&OB->Invisible)
			{
				rez=true;
			}
		}
	}
	if(Not)
		rez=!rez;
	return rez;
}
//==================================================================================================================//
Conditions::Conditions()
{
	LifeTimeMore=-1;
	LifeTimeLess=-1;
	TraveledDistanceMore=-1;
	TraveledDistanceLess=-1;
	RemainderDistanceMore=-1;
	RemainderDistanceLess=-1;
	HeightMore=-1;
	HeightLess=-1;
}
bool Conditions::Check(WeaponParams* WP)
{
	bool rez=false;
	bool add=false;
	int LT=TRUETIME-WP->BirthTime;
	int TD=Norma((WP->From.x>>ToPixelCoord)-(WP->x>>ToPixelCoord),(WP->From.y>>ToPixelCoord)-(WP->y>>ToPixelCoord));
	TD=Norma(TD,(WP->From.z>>ToPixelCoord)-(WP->z>>ToPixelCoord));
	int RD=Norma((WP->To.x>>ToPixelCoord)-(WP->x>>ToPixelCoord),(WP->To.y>>ToPixelCoord)-(WP->y>>ToPixelCoord));
	RD=Norma(RD,(WP->To.z>>ToPixelCoord)-(WP->z>>ToPixelCoord));
	if(LifeTimeMore!=-1){ add=true;	rez=LifeTimeMore<LT; } if(add&&!(rez)) return false;
	if(LifeTimeLess!=-1){ add=true;	rez=LifeTimeLess>LT; } if(add&&!(rez)) return false;
	if(TraveledDistanceMore!=-1){ add=true;	rez=TraveledDistanceMore<TD; } if(add&&!(rez)) return false;
	if(TraveledDistanceLess!=-1){ add=true;	rez=TraveledDistanceLess>TD; } if(add&&!(rez)) return false;
	if(RemainderDistanceMore!=-1){ add=true; rez=RemainderDistanceMore<RD; } if(add&&!(rez)) return false;
	if(RemainderDistanceLess!=-1){ add=true; rez=RemainderDistanceLess>RD; } if(add&&!(rez)) return false;
	int H=0;
	bool GetH=false;
	if(IsInBuilding)
	{
		if(!GetH)
			H=GetHeight(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		int BH=GetBar3DHeight(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		extern word OWNER;
		if(OWNER==0xFFFF||OWNER!=WP->From.UnitIndex){
			if(BH&&(WP->z>>ToPixelCoord)>H&&(WP->z>>ToPixelCoord)<(H+BH))
				return true;		
		}
	}
	if(HeightMore!=-1||HeightLess!=-1||AbsHeightMore!=-1||AbsHeightLess!=-1)
	{
		H=GetHeight(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		if(H<0)
			H=0;
		GetH=true;
		if(HeightMore!=-1){ add=true;	rez=HeightMore<((WP->z>>ToPixelCoord)-H); } if(add&&!(rez)) return false;
		if(HeightLess!=-1){ add=true;	rez=HeightLess>((WP->z>>ToPixelCoord)-H); } if(add&&!(rez)) return false;
		if(AbsHeightMore!=-1){ add=true;	rez=AbsHeightMore<((WP->z>>ToPixelCoord)); } if(add&&!(rez)) return false;
		if(AbsHeightLess!=-1){ add=true;	rez=AbsHeightLess>((WP->z>>ToPixelCoord)); } if(add&&!(rez)) return false;
	}	
	return rez;
}
//==================================================================================================================//
bool TargetReached::Check(WeaponParams* WP){
	int H=-10000;
	if(EarthOrWaterReached){
		H=GetTotalHeight(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		if(H<0)H=0;		
		if( (WP->z>>ToPixelCoord) < (H-8) )return true;
	}
	if(TargetPointReached){
		int RD=Norma((WP->To.x>>ToPixelCoord)-(WP->x>>ToPixelCoord),(WP->To.y>>ToPixelCoord)-(WP->y>>ToPixelCoord));
		//RD=Norma(RD,(WP->To.z>>ToPixelCoord)-(WP->z>>ToPixelCoord));
		if(RD<=TargetPointDistance)return true;
	}
	if(IsInsideBuilding){
		if(H==-10000)H=GetTotalHeight(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		if(H<0)H=0;
		int BH=GetBar3DHeight(WP->x>>ToPixelCoord,WP->y>>ToPixelCoord);
		extern word OWNER;
		if(OWNER==0xFFFF||OWNER!=WP->From.UnitIndex){
			if(BH&&(WP->z>>ToPixelCoord)>H&&(WP->z>>ToPixelCoord)<(H+BH))
				return true;
		}
	}
}
IsFirstStep::IsFirstStep()
{
	Not=false;
}
bool IsFirstStep::Check(WeaponParams* WP)
{
	bool rez=false;
	if(WP&&!WP->OnceProcesed)
		rez=true;
	if(Not)
		rez=!rez;
	return rez;
}
//==================================================================================================================//
