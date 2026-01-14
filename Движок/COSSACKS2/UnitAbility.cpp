#include "stdheader.h"
#include "UnitAbility.h"
#include "AI_Scripts.h"
#include "WeaponSystem.h"
#include "BrigadeAI.h"
#include "cvi_singleplayerdata.h"
#include "cvi_Campaign.h"
#include "UnitAbilityIcon.h"
//==================================================================================================================//
AbilityList Abilities;
BaseClass* GetAbilityClass(){
	return &Abilities.Abilities;
}
int GetTotalHeight0(int x,int y){
	int H=GetTotalHeight(x,y);
	if(H<0)H=0;
	return H;
}
bool ProcessAbilityClass(ClassEditor* CE,BaseClass* BC,int Options){
	if(Options==3)
	{
		for(int i=0;i<NNewMon;i++)
		{
			if(NewMon[i].Ability)
			{
				NewMon[i].Ability->AbilitiesList.Clear();
				NewMon[i].Ability->Feeled=false;
			}
		}
	}
	return false;
}
//==================================================================================================================//
extern int TrueTime;
extern int AnimTime;
typedef bool tpUnitsCallback(OneObject* OB,void* param);
int PerformActionOverUnitsInRadius(int xc,int yc,int R,tpUnitsCallback* CB,void* Param);
int PerformActionOverBuildingsInRadius(int xc,int yc,int R,tpUnitsCallback* CB,void* Param);
void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, int Damage, int AttType);
void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, AdditionalWeaponParams* AddParams);
word GetDir(int dx,int dy);
extern Nation NATIONS[8];
extern int GetHeight(int x, int y);
extern HeroVariableStorage* CurrentHeroAbility;
HeroVariableStorage* GetHeroVariableStorage(OneObject* OB);
void RotateMon(OneObject* OB,char angle);
void PanicUnit(OneObject* OBJ);
void PerformNewUpgrade(Nation* NT,int UIndex,OneObject* OB);
//void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, AdditionalWeaponParams* AddParams);
void AddGroundCircle(int x,int y,int R,DWORD Color);
extern BrigadeAI WiselyFormations;
typedef bool cbCheckSprite(OneSprite* OS, void* Param);
int GetSpritesInRadius(int x, int y, int Radius, cbCheckSprite* cbF, void* Param);
int GetPointToLineDist(int x,int y,int x1,int y1,int x2,int y2);
CEXPORT bool GetPeaceMode();
extern int vmCampID;
extern ClonesArray<cvi_Campaign> vmCampaigns;
extern veGameMode vGameMode;
char* GetTextByID(char* id);
extern bool vGameLoaing;
DLLEXPORT int GetMyNation();
//==================================================================================================================//
bool AddActiveUnitAbility(word Unit, ActiveUnitAbility* AUA);
void InitUnitAbilities(OneObject* OB){	
	NewMonster* NM=OB->newMons;
	if(NM->Ability){
		NM->Ability->Init(NM);
		for(int i=0;i<NM->Ability->AbilitiesList.GetAmount();i++){
			NM->Ability->AbilitiesList[i]->OnUnitBirth(OB);
		}
	}
}
UnitAbility::UnitAbility(void)
{
	Index=-1;
	Name="";
	Visible=true;
	//Targety=false;  
	//MaxCoolDown=0;
	//AutoCast=false;
	//Aura=false;
	//OnMakeDamage=false;
}
bool UnitAbility::OnUnitBirth(OneObject* Newbie)
{
	bool rez=false;
	ActiveUnitAbility* A = GetActiveAbility();
	if(A)
	{
		A->UnitIndex=Newbie->Index;
		A->OB=Newbie;
		A->Visible=Visible;
		A->SetA(this);
		A->FileID=FileID;
		A->SpriteID=SpriteID;
		A->Hint.Add(GetTextByID(Name.str));
		CopyToActive(A);
		rez = AddActiveUnitAbility(Newbie->Index, A);
		if(!rez)
			delete A;
	}
	return rez;
}
ActiveUnitAbility* UnitAbility::GetActiveAbility()
{
	return NULL;
}
// by vital
void UnitAbility::CopyToActive(ActiveUnitAbility* ab){
	/*
	ab->UnitIndex=Newbie->Index;
	ab->OB=Newbie;
	ab->Visible=Visible;
	ab->Hint.Add(Name);
	ab->SetA(this);
	*/
	//
	ab->FileID=FileID;
	ab->SpriteID=SpriteID;
	if(Hint) ab->Hint=GetTextByID(Hint);
};
const char* UnitAbility::GetThisElementView(const char* LocalName){
	if(Name.str){
		static char cc[256];
		sprintf(cc,"%s: {CW}%s{C}",LocalName,Name.str);
		return cc;
	}else return LocalName;
}
//==================================================================================================================//
UnitAbility2::UnitAbility2()
{
	UnitAbility2::UnitAbility();
}
//==================================================================================================================//
MagicSpell::MagicSpell(){
//	operand=-1;
	sign=true;
};
/*
switch(operand) {
case 0:
if(sign){
OB->Life+=num;
OB->MaxLife+=num;
realchng=num;
}
else{ 
OB->Life-=num;
OB->MaxLife-=num;
realchng=-num;
};
break;
case 1:
if(sign){ 
OB->Attack+=num;
realchng=num;
}
else{ 
OB->Attack-=num;
realchng=-num;
}
break;
case 2:
if(sign){ 
OB->UnitSpeed+=num;
realchng=num;
}
else{ 
OB->UnitSpeed-=num;
realchng=-num;
}
break;
case 3:
if(sign){ 
OB->AddShield+=num;
realchng=num;
}
else{ 
OB->AddShield-=num;
realchng=-num;
}
break;
default: realchng=0;
}
return realchng;
*/
int MagicSpell::CastSpell(OneObject* OB){
	return 0;
};
/*
switch(operand) {
case 0:
if(!sign){
OB->Life+=realchng;
OB->MaxLife+=realchng;
}
else{ 
OB->Life-=realchng;
OB->MaxLife-=realchng;
};
break;
case 1:
if(!sign){ 
OB->Attack+=realchng;
}
else{ 
OB->Attack-=realchng;
}
break;
case 2:
if(!sign){ 
OB->UnitSpeed+=realchng;
}
else{ 
OB->UnitSpeed-=realchng;
}
break;
case 3:
if(!sign){ 
OB->AddShield+=realchng;
}
else{ 
OB->AddShield-=realchng;
}
break;
default: realchng=0;
}
return realchng;
*/
int MagicSpell::CancelSpell(OneObject* OB){
	return 0;
};
//==================================================================================================================//
LifeCast::LifeCast(){
	sign=true;
	name="Life";
};
int LifeCast::CastSpell(OneObject* OB){
	realchng=0;
	if(sign){
		realchng=num;
	}
	else{ 
		realchng=-num;
	};
	OB->Life+=realchng;
	OB->MaxLife+=realchng;
	return realchng;
};
int LifeCast::CancelSpell(OneObject* OB){
	OB->Life-=realchng;
	OB->MaxLife-=realchng;
	return realchng;
};
//==================================================================================================================//
AttSpeedCast::AttSpeedCast(){
	sign=true;
	name="Atack Speed";
};
int AttSpeedCast::CastSpell(OneObject* OB){
	realchng=0;
	if(sign){ 
		realchng=num;
	}
	else{ 
		realchng=-num;
	}
//	OB-> +=realchng;
	return realchng;
};
int AttSpeedCast::CancelSpell(OneObject* OB){
//	OB->Attack-=realchng;
	return realchng;
};
//==================================================================================================================//
MoveSpeedCast::MoveSpeedCast(){
	sign=true;
	name="Move Speed";
};
int MoveSpeedCast::CastSpell(OneObject* OB){
	realchng=0;
	if(sign){ 
		realchng=num;
		if(realchng+OB->UnitSpeed>255){
			realchng=255-OB->UnitSpeed;
		}
		if(realchng+OB->Speed>255){
			realchng=255-OB->Speed;
		}
	}
	else{ 
		realchng=-num;
		if(OB->UnitSpeed-realchng<0){
			realchng=-OB->UnitSpeed;
		}
		if(OB->Speed-realchng<0){
			realchng=-OB->Speed;
		}
	}
	OB->UnitSpeed+=realchng;
	OB->Speed+=realchng;
	return realchng;
};
int MoveSpeedCast::CancelSpell(OneObject* OB){
	OB->UnitSpeed-=realchng;
	OB->Speed-=realchng;
	return realchng;
};
//==================================================================================================================//
ShieldCast::ShieldCast(){
	sign=true;
	name="Shield";
};
int ShieldCast::CastSpell(OneObject* OB){
	realchng=0;
	if(sign){ 
		realchng=num;
	}
	else{ 
		realchng=-num;
	}
	OB->AddShield+=realchng;
	return realchng;
};
int ShieldCast::CancelSpell(OneObject* OB){
	OB->AddShield-=realchng;
	return realchng;
};
//==================================================================================================================//
BaseClassUnitType::BaseClassUnitType()
{
	UnitType=0;
}
const char* BaseClassUnitType::GetThisElementView(const char* LocalName){
	static char cc[256];
	sprintf(cc,"UnitType: {CW}%s{C}",NATIONS[0].Mon[UnitType]->MonsterID);
	return cc;
}
//==================================================================================================================//
int UnitTypeList::GetExpansionRules()
{
	return 1;
}
UnitAbilityAura::UnitAbilityAura()
{
	AddDamage=0;
	AddShield=0;
	Cure=0;
	Radius=0;
}
bool UnitAbilityAura::OnUnitBirth(OneObject* Newbie)
{
	if(Newbie)
	{
		ActiveUnitAbilityAura* AA = new ActiveUnitAbilityAura();
		AA->UnitIndex=Newbie->Index;
		AA->OB=Newbie;
		AA->Visible=Visible;
		AA->SetA(this);
		CopyToActive(AA);
		return AddActiveUnitAbility(Newbie->Index, AA);
	}
	return false;
}
//==================================================================================================================//
ActiveUnitAbilityAura::ActiveUnitAbilityAura()
{
}
int ActiveUnitAbilityAura::GetRadius()
{
	UnitAbilityAura* UA = (UnitAbilityAura*)(GetA());
	if(UA)
	{
		return UA->Radius;
	}
	return 0;
}
bool ActiveUnitAbilityAura::Process()
{
	if(!OB)
	{
		if(UnitIndex!=0xFFFF)
			OB=Group[UnitIndex];
	}
	if(OB&&(!OB->Sdoxlo)&&GetA())
	{
		UnitAbilityAura* UA = (UnitAbilityAura*)(GetA());
		int pr[10];
		pr[0]=OB->NMask;
		pr[1]=(int)GetA()->Name.str;
		pr[2]=OB->Index;
		pr[3]=UA->Index;
		pr[4]=UA->AddDamage;
		pr[5]=UA->AddShield;
		pr[6]=UA->SumAuraEffect;
		pr[7]=UA->EnemyUnitEffect;
		pr[8]=UA->FriendlyUnitEffect;
		pr[9]=(int)(&UA->ChoiceUnitType);
		PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,UA->Radius,&ActiveUnitAbilityAura::CheckAbil,pr);
		return true;
	}
	return false;
}
bool ActiveUnitAbilityAura::CheckAbil(OneObject* OBn,void* param)
{
	int* pr = (int*)param;
	byte mas=(byte)pr[0];
	char* Nam=(char*)pr[1];
	int UnitIndex=pr[2];
	int AbilIndex=pr[3];
	int SumAura=pr[6];
	int EnE=pr[7];
	int FrE=pr[8];
	UnitTypeList* CUT=(UnitTypeList*)pr[9];
	if(OBn&&(!OBn->Sdoxlo))
	{
		bool fr=OBn->NMask&mas;
		if((fr&&FrE)||((!fr)&&EnE))
		{
			int nu=CUT->GetAmount();
			bool st=true;
			if(nu)
			{
				st=false;
				for(int j=0;j<nu;j++)
				{
					if((*CUT)[j]->UnitType==OBn->NIndex)
					{
						st=true;
						break;
					}
				}
			}
			if(st)
			{
				if(!OBn->ActiveAbility)
				{
					OBn->ActiveAbility = new UnitActiveAbilityList();
				}
				ActiveUnitAbility* PR=NULL;
				bool add=true;
				int n = OBn->ActiveAbility->ActiveAbilities.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(!strcmp(Nam,OBn->ActiveAbility->ActiveAbilities[i]->Name.str))
					{
						if(SumAura)
						{
							if(((AuraEffect*)OBn->ActiveAbility->ActiveAbilities[i])->Creator==UnitIndex)
							{
								add=false;
								break;
							}
						}
						else
						{
							add=false;
							break;
						}
					}
				}
				if(add)
				{
					AuraEffect* AE = new AuraEffect();
					AE->UnitIndex=OBn->Index;
					AE->Creator=UnitIndex;
					AE->AbilityIndex=AbilIndex;
					//CopyToActive(AE);
					OBn->ActiveAbility->AddActiveUnitAbility(AE);
					OBn->AddDamage+=pr[4];
					OBn->AddShield+=pr[5];
					AE->Name=Nam;
				}
			}
		}
	}
	return true;
}
//==================================================================================================================//
UnitAbilityMagicEffect::UnitAbilityMagicEffect()
{
	WeaponModificatorName="";
}
bool UnitAbilityMagicEffect::OnUnitBirth(OneObject* OBJ)
{
	if(OBJ)
	{
		ActiveUnitAbilityMagicEffect* AA = new ActiveUnitAbilityMagicEffect();
		AA->UnitIndex=OBJ->Index;
		AA->OB=OBJ;
		AA->SetA(this);
		AA->Visible=Visible;
		AA->HVS=GetHeroVariableStorage(OBJ);
		int n=Variables.GetAmount();
		for(int i=0;i<n;i++)
		{
			HeroVariable* HV = new HeroVariable();
			HV->Name=Variables[i]->Name.str;
			HV->Value=Variables[i]->Value;
			AA->HVS->Variables.Add(HV);
		}
		CopyToActive(AA);
		return AddActiveUnitAbility(OBJ->Index, AA);
	}
	return false;
}
//==================================================================================================================//
ActiveUnitAbilityMagicEffect::ActiveUnitAbilityMagicEffect()
{
	Tx=-1;
	Ty=-1;
	Tz=-1;
	Target=0xFFFF;
	CoolDownTime=0;
	OnOff=false;
	HVS=NULL;
	CheckHero=false;
	TempRadius=-1;
}
bool ActiveUnitAbilityMagicEffect::Process()
{
	if((!HVS)&&(!CheckHero))
	{
		HVS=GetHeroVariableStorage(OB);
		CheckHero=true;
	}
	CurrentHeroAbility=HVS;
	if((!OB)&&UnitIndex!=0xFFFF)
		OB=Group[UnitIndex];
	if(OB&&!OB->Sdoxlo)
	{
		if(AnimTime>=CoolDownTime)CoolDownTime=0;

		
		UnitAbilityMagicEffect* A = (UnitAbilityMagicEffect*) GetA();
		if(A)
		{
			Hint.Clear();
			Hint.Add(GetTextByID(Name.str));
			if(!CoolDownTime)
			{
				Hint.Add("\\Damage: ");
				Hint.Add(A->Damage.Get());
				Hint.Add(" Mana: ");
				Hint.Add(A->ManaCost.Get());
			}
			else
			{
				Hint="\\CoolDown: ";
				Hint.Add(((CoolDownTime-AnimTime)*100)/(A->CoolDownTime.Get()*25*256));
				Hint.Add("%");
			}

			if(OB->DestX<1&&A->ContinueUse&&Tx!=-1&&CanTarget(Target,Tx,Ty,Tz)&&OB->Mana>=A->ManaCost.Get())
			{
				if((AnimTime-UseTime)>=A->UsePause.Get()*25*256/10)
				{
					ApplayWeapon();
				}
			}
			else
			{
				Tx=-1;
			}
			if(A->ManualOnOff&&OnOff&&OB->Mana>=A->ManaCost.Get())
			{
				if((AnimTime-UseTime)>=A->UsePause.Get()*25*256/10)
				{
					ApplayWeapon();
				}
			}
			if(A->TipaAura)
			{
				if((AnimTime-UseTime)>=A->UsePause.Get()*25*256/10)
				{
					ApplayWeapon();
				}
			}
		}
	}
	return true;
}
bool ActiveUnitAbilityMagicEffect::isTarget()
{
	UnitAbilityMagicEffect* A = (UnitAbilityMagicEffect*) GetA();
	if(A)
	{
		return A->NeedTarget;
	}
	return false;
}
bool ActiveUnitAbilityMagicEffect::CanTarget(word TargetUnit, int x, int y, int z)
{
	UnitAbilityMagicEffect* A = (UnitAbilityMagicEffect*) GetA();
	if(OB&&A&&A->NeedTarget&&CoolDownTime==0)
	{
		int sx=OB->RealX>>4;
		int sy=OB->RealY>>4;
		int Ds=Norma(sx-x,sy-y);
		if(Ds>=A->MinDist.Get()&&Ds<A->MaxDist.Get())
		{
			if(A->EnemyUnitTarget||A->FriendlyUnitTarget)
			{
				if(TargetUnit!=0xFFFF)
				{
					OneObject* OT = Group[TargetUnit];
					if(OT)
					{
						if(OB->NMask&OT->NMask)
						{
							if(A->FriendlyUnitTarget)
								return true;
						}
						else
						{
							if(A->EnemyUnitTarget)
								return true;
						}
					}
				}
			}
			else
				return true;
		}
	}
	return false;
}
bool ActiveUnitAbilityMagicEffect::Execute(word TargetUnit, int x, int y, int z)
{
	bool rez=false;
	CurrentHeroAbility=HVS;
	UnitAbilityMagicEffect* A = (UnitAbilityMagicEffect*) GetA();
	if(CanTarget(TargetUnit,x,y,z)&&OB&&A&&OB->Mana>=A->ManaCost.Get())
	{
		Tx=x;
		Ty=y;
		Tz=z;
		Target=TargetUnit;
		if(Tz==0||Tz==-1)Tz=GetHeight(x,y);
		if(ApplayWeapon())
		{
			CoolDownTime=AnimTime+A->CoolDownTime.Get()*25*256;
			rez=true;
		}
	}
	TempRadius=-1;
	return rez;
}
bool ActiveUnitAbilityMagicEffect::OnClick()
{
	bool rez=false;
	UnitAbilityMagicEffect* A = (UnitAbilityMagicEffect*) GetA();
	if(OB&&A&&A->ManualOnOff)
	{
		OnOff=!OnOff;
		rez=true;
	}
	if(OB&&(!A->ManualOnOff)&&(!A->NeedTarget)&&CoolDownTime==0&&OB->Mana>=A->ManaCost.Get())
	{
		Tx=OB->RealX>>4;
		Ty=OB->RealY>>4;
		Tz=OB->RZ;
		Target=0xFFFF;
		CurrentHeroAbility=HVS;
		if(ApplayWeapon())
		{
			CoolDownTime=AnimTime+A->CoolDownTime.Get()*25*256;
			rez=true;
		}
	}
	return rez;
}
bool ActiveUnitAbilityMagicEffect::DrawCursor(int x,int y)
{
	if(TempRadius==-1)
	{
		TempRadius=0;
		UnitAbilityMagicEffect* BA = (UnitAbilityMagicEffect*)GetA();
		if(BA)
		{
			TempRadius=BA->Radius.Get();
		}
	}
	if(TempRadius)
	{
		static int bID = IMM->GetModelID( "Models\\banner.c2m" );
		IMM->Render(bID,x,y,TempRadius/200.0f);
	}
	return true;
}
bool ActiveUnitAbilityMagicEffect::ApplayWeapon()
{
	bool rez=false;
	UnitAbilityMagicEffect* A = (UnitAbilityMagicEffect*) GetA();
	if(OB&&A)
	{
		int sx=OB->RealX>>4;
		int sy=OB->RealY>>4;
		int sz=OB->RZ;
		if(A->SetPointTargetPoint)
		{
			sx=Tx;
			sy=Ty;
			sz=Tz;
		}
		AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
		AWP->Damage=A->Damage.Get();
		AWP->AttType=A->AttType;
		AWP->Radius=A->Radius.Get();
		AWP->N=A->N.Get();
		if(A->EnemyUnitTarget||A->FriendlyUnitTarget)
		{
			if(Target!=0xFFFF)
			{
				OneObject* OT = Group[Target];
				if(OT)
				{
					CreateNewActiveWeapon(A->WeaponModificatorName.str,UnitIndex,sx,sy,sz,Target,Tx,Ty,Tz,AWP);
				}
			}
		}
		else
		{
			CreateNewActiveWeapon(A->WeaponModificatorName.str,UnitIndex,sx,sy,sz,Target,Tx,Ty,Tz,AWP);
		}
		UseTime=AnimTime;
		OB->Mana-=A->ManaCost.Get();
	}
	return rez;
}
//==================================================================================================================//
VampiricAbility::VampiricAbility()
{
	LifeProc=0;
}
bool VampiricAbility::OnUnitBirth(OneObject* OBJ)
{
	if(OBJ)
	{
		ActiveVampiricAbility* ab = new ActiveVampiricAbility();
		ab->UnitIndex=OBJ->Index;
		ab->OB=OBJ;
		ab->Visible=Visible;
		ab->SetA(this);
		CopyToActive(ab);
		return AddActiveUnitAbility(OBJ->Index, ab);
	}
	return false;
}
//==================================================================================================================//
bool ActiveVampiricAbility::OnMakeDamage(OneObject* Take,int& Damage)
{
	if(Take&&OB&&(!OB->Sdoxlo)&&GetA()&&Take->NewBuilding!=1)
	{
		OB->Life+=(Take->MaxLife*((VampiricAbility*)GetA())->LifeProc)/100;
		if(OB->Life>OB->MaxLife)
			OB->Life=OB->MaxLife;
		return true;
	}
	return false;
}
bool ActiveVampiricAbility::Process()
{
	if((!OB)&&UnitIndex!=0xFFFF)
		OB=Group[UnitIndex];
	if(OB&&(!OB->Sdoxlo))
		return true;
	return false;
}
//==================================================================================================================//
ChangeMDAbiliti::ChangeMDAbiliti()
{
	Visible=true;
}
bool ChangeMDAbiliti::OnUnitBirth(OneObject* OBJ)
{
	if(OBJ)
	{
		ChangeMDAbilitiActive* ab = new ChangeMDAbilitiActive();
		ab->UnitIndex=OBJ->Index;
		ab->OB=OBJ;
		ab->Visible=Visible;
		ab->SetA(this);
		//ab->HVS=GetHeroVariableStorage(OBJ);	
		CopyToActive(ab);
		return AddActiveUnitAbility(OBJ->Index, ab);
	}
	return false;
}
/*
bool ChangeMDAbiliti::Execute(word Unit, word TargetUnit, int x, int y, int z)
{
	if(CanApply(Unit))
	{
		ChangeMDAbilitiActive* Ac = new ChangeMDAbilitiActive();
		Ac->UnitIndex=Unit;
		Ac->ThroughState=ThroughState;
		Ac->NewType.UnitType=NewType.UnitType;
		OneObject* OB=Group[Unit];
		OB->NewState=ThroughState;
		OB->GroundState=ThroughState;
		TryToStand(OB,false);
		//OB->UnlimitedMotion=true;
		return AddActiveUnitAbility(Unit,Ac);
	}
	return true;
}
bool ChangeMDAbiliti::CanApply(word Unit)
{
	if(Unit!=0xFFFF)
	{
		OneObject* OB=Group[Unit];
		if(OB&&OB->ActiveAbility)
		{
			int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
			for(int i=0;i<n;i++)
			{
				if(OB->ActiveAbility->ActiveAbilities[i]->Type==6)
				{
					return false;
				}
			}
		}
	}
	return true;
}
*/
//==================================================================================================================//
bool BlizardAbility::OnUnitBirth(OneObject* OBJ)
{
	if(OBJ)
	{
		ActiveBlizardAbility* ab = new ActiveBlizardAbility();
		ab->UnitIndex=OBJ->Index;
		ab->OB=OBJ;
		ab->Visible=Visible;
		ab->SetA(this);
		ab->HVS=GetHeroVariableStorage(OBJ);
		if(ab->HVS)
		{
			int n=Variables.GetAmount();
			for(int i=0;i<n;i++)
			{
				HeroVariable* HV = new HeroVariable();
				HV->Name=Variables[i]->Name.str;
				HV->Value=Variables[i]->Value;
				ab->HVS->Variables.Add(HV);
			}
		}
		CopyToActive(ab);
		return AddActiveUnitAbility(OBJ->Index, ab);
	}
	return false;
}
//==================================================================================================================//
ActiveBlizardAbility::ActiveBlizardAbility()
{
	Tx=-1;
	Ty=-1;
	InUseTime=-1;
	CoolDownTime=0;
	Hint="";
	CheckHero=false;
	HVS=NULL;
	TempRadius=-1;
}
bool ActiveBlizardAbility::Process()
{
	if((!OB)&&UnitIndex!=0xFFFF)
		OB=Group[UnitIndex];
	if((!HVS)&&(!CheckHero))
	{
		HVS=GetHeroVariableStorage(OB);
		CheckHero=true;
	}
	CurrentHeroAbility=HVS;

	BlizardAbility* A = (BlizardAbility*)GetA();
	if(OB&&!OB->Sdoxlo&&A)
	{
		if(AnimTime>=CoolDownTime)CoolDownTime=0;

		if(!CoolDownTime)
		{
			Hint="AcidicRain\\Damage: ";
			Hint.Add(A->Damage.Get());
			Hint.Add(" Mana: ");
			Hint.Add(A->ManaCost.Get());
		}
		else
		{
			Hint="AcidicRain\\CoolDown: ";
			Hint.Add(((CoolDownTime-AnimTime)*100)/(A->CoolDownTime.Get()*25*256));
			Hint.Add("%");
		}
		if(Tx!=-1)
		{
			if(OB->DestX<1&&OB->Mana>=A->ManaCost.Get())
			{
				if((AnimTime-InUseTime)>=(A->DamagePause*25*256/10))
				{
					AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
					AWP->Damage=A->Damage.Get();
					AWP->AttType=A->AttType;
					AWP->Radius=A->Radius.Get();
					CreateNewActiveWeapon(A->EffectName.str,UnitIndex,Tx,Ty,Tz,0xFFFF,Tx,Ty,Tz,AWP);
					InUseTime=AnimTime;
					OB->Mana-=A->ManaCost.Get();
				}			
			}
			else
			{
				Tx=-1;
			}
		}
	}
	return true;
}
bool ActiveBlizardAbility::isTarget()
{
	return true;
}
bool ActiveBlizardAbility::CanTarget(word TargetUnit, int x, int y, int z)
{
	bool rez=false;
	BlizardAbility* A = (BlizardAbility*)GetA();
	if(OB&&(!OB->Sdoxlo)&&CoolDownTime==0&&A)
	{
		int ds = Norma((OB->RealX>>4)-x,(OB->RealY>>4)-y);
		if(ds<A->MaxDist)
			rez=true;
	}
	return rez;
}
bool ActiveBlizardAbility::Execute(word TargetUnit, int x, int y, int z)
{
	bool rez=CanTarget(TargetUnit,x,y,z);
	if(rez)
	{
		BlizardAbility* A = (BlizardAbility*)GetA();
		CurrentHeroAbility=HVS;
		if(OB->Mana>=A->ManaCost.Get())
		{
			Tz=GetHeight(x,y)+300;
			Tx=x;
			Ty=y;
			AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
			AWP->Damage=A->Damage.Get();
			AWP->AttType=A->AttType;
			AWP->Radius=A->Radius.Get();
			CreateNewActiveWeapon(A->EffectName.str,UnitIndex,Tx,Ty,Tz,0xFFFF,Tx,Ty,Tz,AWP);
			InUseTime=AnimTime;
			CoolDownTime=AnimTime+A->CoolDownTime.Get()*25*256;
			OB->Mana-=A->ManaCost.Get();
		}
	}
	TempRadius=-1;
	return rez;
}
bool ActiveBlizardAbility::DrawCursor(int x,int y)
{
	if(TempRadius==-1)
	{
		TempRadius=0;
		BlizardAbility* BA = (BlizardAbility*)GetA();
		if(BA)
		{
			TempRadius=BA->Radius.Get();
		}
	}
	if(TempRadius)
	{
		static int bID = IMM->GetModelID( "Models\\banner.c2m" );
		IMM->Render(bID,x,y,TempRadius/200.0f);
	}
	return true;
}
//==================================================================================================================//
bool AbilityList::LoadAbilities(char* FileName)
{
	bool rez=false;
	if(FileName)
	{
		xmlQuote Inf;
		if(Inf.ReadFromFile(FileName))
		{
			Abilities.Clear();
			ErrorPager Error;
			if(Abilities.Load(Inf,&Abilities,&Error))
			{
				//RefreshEnumerator();
				rez=true;
			}
		}
	}
	if(rez)
	{
		int n= Abilities.GetAmount();
		for(int i=0;i<n;i++)
		{
			Abilities[i]->Index=i;
		}
	}
	return rez;
}
//==================================================================================================================//
int ArrayAbilities::GetExpansionRules()
{
	return 2;
}
//==================================================================================================================//
MonsterAbility::MonsterAbility()
{
	Feeled=false;
}
void MonsterAbility::Init(NewMonster* NM){
	if(!Feeled){
		int n = AbilityNames.GetAmount();
		AbilitiesList.Clear();
		for(int i=0;i<n;i++){
			int m = Abilities.Abilities.GetAmount();
			for(int j=0;j<m;j++){
				if(!strcmp(AbilityNames[i]->str,Abilities.Abilities[j]->Name.str)){
					AbilitiesList.Add(Abilities.Abilities[j]);
				}
			}
		}
		Feeled=true;
	}
};
bool MonsterAbility::Process(int UnitIndex)
{	
	/*
	int n = AbilitiesList.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(AbilitiesList[i]->AutoCast)
			AbilitiesList[i]->Execute(UnitIndex, 0xFFFF, -1, -1, -1);
	}
	*/
	return true;
}
//==================================================================================================================//
ActiveUnitAbility::ActiveUnitAbility()
{
	Name="";
	UnitIndex=0xFFFF;
	Type=0;
	TypeAbil=-1;
	Visible=false;
	FileID=0;
	SpriteID=0;
	Hint="";
	UseTime=0;
	IconInfo=NULL;
	InfluenceMask=0;
}
ActiveUnitAbility::~ActiveUnitAbility(void)
{
	if(IconInfo)
	{
		delete IconInfo;
		IconInfo=NULL;
	}
}
bool ActiveUnitAbility::Process()
{
	bool rez=false;
	if((!OB)&&UnitIndex!=0xFFFF)
	{
		OB=Group[UnitIndex];
	}
	if(OB)
		rez=Process(OB);
	return rez;
}
bool ActiveUnitAbility::Process(OneObject* OB)
{
	return false;
}
bool ActiveUnitAbility::OnClick()
{
	return false;
}
bool ActiveUnitAbility::OnRightClick()
{
	return false;
}
bool ActiveUnitAbility::Execute(word TargetUnit, int x, int y, int z)
{
	return true;
}
bool ActiveUnitAbility::CanApply()
{
	return true;
}
bool ActiveUnitAbility::isTarget()
{
	return false;
}
bool ActiveUnitAbility::CanTarget(word TargetUnit, int x, int y, int z)
{
	return true;
}
int ActiveUnitAbility::GetCoolDownProc()
{
	return 0;
}
bool ActiveUnitAbility::isActive()
{
	return false;
}
bool ActiveUnitAbility::OnMakeDamage(OneObject* Take,int& Damage)
{
	return false;
}
bool ActiveUnitAbility::DrawCursor(int x,int y)
{
	return false;
}
bool ActiveUnitAbility::IsHero()
{
	return false;
}
int ActiveUnitAbility::GetRadius()
{
	return 0;
}
bool ActiveUnitAbility::ShowRadius()
{
	if(OB)
	{
		int R=GetRadius();
		if(R)
		{
			AddGroundCircle(OB->RealX>>4,OB->RealY>>4,R,0xFFFFFF00);
			return true;
		}
	}
	return false;
}
bool ActiveUnitAbility::CanYouAddToObject(OneObject* OB,void* Param)
{
	return false;
}
bool ActiveUnitAbility::AddToObject(OneObject* OB,void* Param)
{
	if(FillParam(OB, Param))
	{
		return AddToObject(OB);
	}
	return false;
}
bool ActiveUnitAbility::AddToObject(OneObject* B)
{
	OB=B;
	UnitIndex=B->Index;
	if(!OB->ActiveAbility)
	{
		B->ActiveAbility = new UnitActiveAbilityList();	
	}
	return B->ActiveAbility->AddActiveUnitAbility(this);
}
bool ActiveUnitAbility::FillParam(OneObject* OB, void* Param)
{
	return false;
}
ActiveUnitAbility* ActiveUnitAbility::GetActionAbilityExample()
{
	return NULL;
}
int ActiveUnitAbility::GetUsePause()
{
	return 0;
}
int ActiveUnitAbility::AddActionAbilityOverUnitsInRadius()
{
	int rez=0;
	int R=GetRadius();
	if(OB&&AnimTime>(GetUsePause()*25*256/10+UseTime)&&GetCoolDownProc()==0&&R)
	{
		rez=PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,R,&ActiveUnitAbility::ApplyActionAbility,(void*)this);
		UseTime=AnimTime;
	}
	return rez;
}
const char* ActiveUnitAbility::GetHint()
{
	return Hint.str;
}
bool ActiveUnitAbility::ApplyActionAbility(OneObject* OB,void* Param)
{
	bool rez=false;
	if(OB&&Param)
	{
		ActiveUnitAbility* A = (ActiveUnitAbility*)Param;
		ActiveUnitAbility* Ex = A->GetActionAbilityExample();
		if(Ex)
		{
			if(Ex->CanYouAddToObject(OB,Param))
			{
				ActiveUnitAbility* N = (ActiveUnitAbility*) Ex->new_element();
				rez=N->AddToObject(OB,Param);
				if(!rez)
					delete N;
			}
		}
	}
	return rez;
}
UnitAbility* ActiveUnitAbility::GetA()
{
	return UnitAbilityRef.Get();
}
void ActiveUnitAbility::SetA(UnitAbility* A)
{
	UnitAbilityRef.Set(A);
}
UnitAbility* ActiveUnitAbility::GetAW()
{
	UnitAbility* rez=UnitAbilityPtr.Get();
	if((!rez)&&UnitAbilityRef.Get())
	{
		UnitAbility* UCopy =(UnitAbility*) UnitAbilityRef.Get()->new_element();
		if(UCopy)
		{
			UnitAbilityRef.Get()->Copy(UCopy);
			UnitAbilityPtr.Set(UCopy);
			rez=UCopy;
		}
	}
	return rez;
}
UnitAbilityIcon* ActiveUnitAbility::GetUnitAbilityIconInfo()
{
	if(Visible)
	{
		if(!IconInfo)
		{
			IconInfo = new UnitAbilityIcon();
			IconElement* IE = new IconElement();
			IE->FileID=FileID;
			IE->SpriteID=SpriteID;
			IE->Hint=Hint.str;
			IconInfo->AddElement(IE);
		}
		return IconInfo;
	}
	return NULL;
}
//==================================================================================================================//
CoolDown::CoolDown()
{
	Type=5;
	Name="CoolDown";
	UnitAbilityIndex=-1;
	Value=0;
	LastProcesedTime=TrueTime;
}
CoolDown::CoolDown(int UAbilityIndex, int V)
{
	Type=5;
	Name="CoolDown";
	UnitAbilityIndex=UAbilityIndex;
	Value=V;
	LastProcesedTime=TrueTime;
}
bool CoolDown::Process()
{
	int in=TrueTime-LastProcesedTime;
	Value-=in;
	if(Value<0)Value=0;
	LastProcesedTime=TrueTime;
	if(Value)return true;
	return false;
}
//==================================================================================================================//
UnitActiveAbilityList::UnitActiveAbilityList()
{
	ActiveAbilities.InfluenceMask=0;
}
void TestCGARB();
bool UnitActiveAbilityList::Process()
{
	bool rez=false;
	int n = ActiveAbilities.GetAmount();
	for(int i=0;i<n;i++)
	{
		ActiveUnitAbility* AA=ActiveAbilities[i];
		bool abl=AA->Process();
		if(!abl)
		{
			delete (ActiveAbilities[i]);
			ActiveAbilities[i]=NULL;
			ActiveAbilities.Del(i,1);
			i--;
			n--;
		}
		else
		{
			rez=true;
		}
	}
	return rez;
}
bool UnitActiveAbilityList::AddActiveUnitAbility(ActiveUnitAbility* AUA)
{
	if(AUA)
	{
		ActiveAbilities.Add(AUA);
		return true;
	}
	return false;
}
ActiveUnitAbility* UnitActiveAbilityList::GetActiveUnitAbility(const char* name)
{
	ActiveUnitAbility* rez=NULL;
	int n = ActiveAbilities.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(!strcmp(name,ActiveAbilities[i]->Name.str))
		{
			rez=(ActiveAbilities[i]);
			break;
		}
	}
	return rez;
}
//==================================================================================================================//
void ApplyUnitAbility(int AbilityIndex,word Unit, word TargtUnit, int x, int y, int z)
{
	/*
	int n = Abilities.Abilities.GetAmount();
	if(AbilityIndex>-1&&AbilityIndex<n)
	{
		Abilities.Abilities[AbilityIndex]->Execute(Unit,TargtUnit,x,y,z);
	}
	*/
}
void TestCGARB();
void UnitAbilityProcess()
{
	for(int i=0;i<MAXOBJECT;i++)
	{
		OneObject* OB=Group[i];
		if(OB)
		{
			/*if(OB->Sdoxlo)
			{
				if(OB->ActiveAbility)
				{
					delete (OB->ActiveAbility);
					OB->ActiveAbility=NULL;
				}
			}
			else*/
			{
				if(OB->newMons->Ability)
					OB->newMons->Ability->Process(i);
				if(OB->ActiveAbility)
					OB->ActiveAbility->Process();
				int x=0;
			};
		}
	};
}
void UnloadUnitsAbility()
{
	for(int i=0;i<MAXOBJECT;i++)
	{
		OneObject* OB=Group[i];
		if(OB)
		{
			if(OB->ActiveAbility)
			{
				delete (OB->ActiveAbility);
				OB->ActiveAbility=NULL;
			}
		}
	};
}
void OnMakeDamageProcess(OneObject* Sender,OneObject* Take,int& Damage)
{
	if(Sender&&Take)
	{
		if(Sender->ActiveAbility)
		{
			int n=Sender->ActiveAbility->ActiveAbilities.GetAmount();
			for(int i=0;i<n;i++)
			{
				Sender->ActiveAbility->ActiveAbilities[i]->OnMakeDamage(Take,Damage);
			}
		}
	}
}
extern int ItemChoose;
bool ProcessMessages();
bool MMItemChoose(SimpleDialog* SD);

void ProcessUnitAbilityEditor(){
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
				CE.CreateFromClass(Desk,0,0,Desk->x1-Desk->x,Desk->y1-Desk->y,&Abilities,3,"EmptyBorder");
				int SaveTime = GetTickCount();
				do{
					//GameWeaponSystem.RefreshEnumerator();
					ProcessMessages();					
					DSS.ProcessDialogs();
					CE.Process();
					DSS.RefreshView();
					int TT = GetTickCount();
					if((SaveTime+2000)<TT)
					{
						xmlQuote* sxml = new xmlQuote("ClassArray.UnitAbility");
						Abilities.Abilities.Save(*sxml,&Abilities.Abilities);
						sxml->WriteToFile("UnitsAbility\\base.ua.xml");
						SaveTime=TT;
						delete sxml;
					}
				}while(ItemChoose==-1);
			}
		}
	}	
}
extern word NNewMon;
extern NewMonster NewMon[1024];

bool LoadAbilities(char* FileName)
{
	for(int i=0;i<NNewMon;i++)
	{
		if(NewMon[i].Ability)
		{
			NewMon[i].Ability->AbilitiesList.Clear();
			NewMon[i].Ability->Feeled=false;
		}
	}
	return Abilities.LoadAbilities(FileName);
}
bool AddActiveUnitAbility(word Unit, ActiveUnitAbility* AUA)
{
	if(Unit!=0xFFFF)
	{
		OneObject* OB=Group[Unit];
		if(OB)
		{
			//
			AUA->Visible=true;
			AUA->OB=OB;
			AUA->UnitIndex=OB->Index;
			//
			if(!OB->ActiveAbility)
			{
				OB->ActiveAbility = new UnitActiveAbilityList();	
			}
			return OB->ActiveAbility->AddActiveUnitAbility(AUA);
		}
	}
	return false;
}
bool AddMonsterAbility(MonsterAbility** MA,char* Name)
{
	bool rez=false;
	if(MA&&Name)
	{
		if((*MA)==NULL)
		{
			(*MA)=new MonsterAbility();
		}
		_str* n = new _str();
		n->Add(Name);
		(*MA)->AbilityNames.Add(n);
	}
	return rez;
}
//==================================================================================================================//
AuraEffect::AuraEffect()
{
}
bool AuraEffect::Process()
{
	if(UnitIndex!=0xFFFF)
	{
		OneObject* OB = Group[UnitIndex];
		if(OB&&(!OB->Sdoxlo))
		{
			UnitAbilityAura* UA =(UnitAbilityAura*)(Abilities.Abilities[AbilityIndex]);
			if(Creator!=0xFFFF)
			{
				OneObject* C = Group[Creator];
				if(C&&(!C->Sdoxlo))
				{
					int ds=Norma((OB->RealX>>4)-(C->RealX>>4),(OB->RealY>>4)-(C->RealY>>4));
					if(ds<UA->Radius)
					{
						return true;
					}
				}
			}
			OB->AddDamage-=UA->AddDamage;
			OB->AddShield-=UA->AddShield;
		}
	}
	return false;
}
//==================================================================================================================//
ChangeMDAbilitiActive::ChangeMDAbilitiActive()
{
	Type=6;
}
bool ChangeMDAbilitiActive::Process()
{
	bool rez=false;
	ChangeMDAbiliti* NMD = (ChangeMDAbiliti*)GetA();
	if(NMD&&UnitIndex!=-1&&UnitIndex!=0xFFFF&&NMD->ThroughState!=-1)
	{
		OneObject* OB=Group[UnitIndex];
		if(OB&&!OB->Sdoxlo)
		{
			rez=true;
			if(OB->NewState==NMD->ThroughState)
			{
				OB->DestX=0;
				OB->DestY=0;
				if(OB->OverEarth>4)
					OB->OverEarth-=4;
				else
					OB->OverEarth=0;
				void StayForSomeTime(OneObject* OB,byte OrdType,int Time);
				//OB->UnlimitedMotion=false;
				StayForSomeTime(OB,1,0);
				//OB->UnlimitedMotion=true;
				if(OB->FrameFinished&&OB->LocalNewState==NMD->ThroughState)
				{
					if(NMD->NewType.UnitType>-1&&NMD->NewType.UnitType!=0xFFFF)
					{
						OB->NIndex=NMD->NewType.UnitType;
						OB->Ref.General=NATIONS[OB->NNUM].Mon[NMD->NewType.UnitType];
						OB->newMons=OB->Ref.General->newMons;
						OB->Life=(((OB->Life*100)/OB->MaxLife)*OB->Ref.General->MoreCharacter->Life)/100;
						OB->MaxLife=OB->Ref.General->MoreCharacter->Life;
						OB->LockType=OB->newMons->LockType;
						OB->NewState=0;
						OB->LocalNewState=0;
						OB->GroundState=0;
						OB->OverEarth=0;
						//OB->UnlimitedMotion=false;
					}
					rez=false;
				}
			}
		}
	}
	return rez;
}
bool ChangeMDAbilitiActive::OnClick()
{
	bool rez=false;
	ChangeMDAbiliti* NMD = (ChangeMDAbiliti*)GetA();
	if(NMD&&UnitIndex!=-1&&UnitIndex!=0xFFFF&&NMD->ThroughState!=-1)
	{
		OneObject* OB=Group[UnitIndex];
		if(OB&&OB->NewState!=NMD->ThroughState)
		{
			OB->NewState=NMD->ThroughState;
			OB->GroundState=NMD->ThroughState;
			TryToStand(OB,false);
			rez=true;
		}
	}
	return rez;
}

//==================================================================================================================//
class CBoidsParams : public BaseClass
{
public:
	CBoidsParams()
	{
		Radius=0;
		MinDist=0;
		OneUnitW=0;
		MainDirNorma=0;
		DensNorma=0;
		DensW=0;
		CentNorma=0;
		CentW=0;
		DeviationW=0;
		PushRadius=30;
		InFormation=100;
	};
	int Radius;
	int MinDist;
	int OneUnitW;
	int MainDirNorma;
	int DensNorma;
	int DensW;
	int CentNorma;
	int CentW;
	int DeviationW;
	int LockNorma;
	int LockW;
	int ChangeSpeedW;
	int PushRadius;
	int InFormation;
	SAVE(CBoidsParams)
		REG_MEMBER(_int,Radius);
		REG_MEMBER(_int,MinDist);
		REG_MEMBER(_int,OneUnitW);
		REG_MEMBER(_int,MainDirNorma);
		REG_MEMBER(_int,DensNorma);
		REG_MEMBER(_int,DensW);
		REG_MEMBER(_int,CentNorma);
		REG_MEMBER(_int,CentW);
		REG_MEMBER(_int,DeviationW);
		REG_MEMBER(_int,LockNorma);
		REG_MEMBER(_int,LockW);
		REG_MEMBER(_int,ChangeSpeedW);
		REG_MEMBER(_int,PushRadius);
		REG_MEMBER(_int,InFormation);
	ENDSAVE;
};
CBoidsParams BoidsParams;
bool LoadBoidsParams()
{
	bool rez=false;
	xmlQuote Inf;
	if(Inf.ReadFromFile("UnitsAbility\\BoidsParams.xml"))
	{
		ErrorPager Error;
		if(BoidsParams.Load(Inf,&BoidsParams,&Error))
		{
			rez=true;
		}
	}
	return rez;
}
bool CheckNearest(OneObject* OBn,void* param)
{
	int* pr=(int*)param;
	OneObject* OB=(OneObject*)(pr[0]);
	if(OB!=OBn&&OBn&&(!OBn->Sdoxlo)&&(OB->NIndex&OBn->NIndex))
	{
		int ddx=OBn->RealX-OB->RealX;
		int ddy=OBn->RealY-OB->RealY;
		int ddd=Norma(ddx,ddy)+1;
		
		int OUW1=BoidsParams.OneUnitW/2;
		int OUW2=BoidsParams.OneUnitW/2;
		int rz1=pr[1]/2;
		int rz2=pr[1]/2;
		if(OB->newMons->BoidsMovingMinDist!=-1)rz1=OB->newMons->BoidsMovingMinDist<<4;
		if(OBn->newMons->BoidsMovingMinDist!=-1)rz2=OBn->newMons->BoidsMovingMinDist<<4;
		if(OB->newMons->BoidsMovingWeight!=-1)OUW1=OB->newMons->BoidsMovingWeight;
		if(OBn->newMons->BoidsMovingWeight!=-1)OUW2=OBn->newMons->BoidsMovingWeight;
		int rzz=rz1+rz2;
		if(OB->BrigadeID!=0xFFFF&&OB->BrigadeID==OBn->BrigadeID)rzz=(BoidsParams.InFormation*rzz)/100;
		int rz=ddd-rzz;
		if(rz<0)
		{
			int dx=(OUW1+OUW2)*ddx*rz/rzz;
			int dy=(OUW1+OUW2)*ddy*rz/rzz;
			if(ddd<(rzz>>1)){
				dx<<=1;
				dy<<=1;
			}
			if(ddd<(rzz>>2)){
                dx<<=2;
				dy<<=2;
			}
			pr[2]+=dx;
			pr[3]+=dy;			
		}
		/*
		if(rz>0)
		{
			ddx=ddx*rz/ddd;
			ddy=ddy*rz/ddd;
		}
		pr[2]+=ddx;
		pr[3]+=ddy;
		*/
		if(OBn->DestX>0)
		{
			pr[4]+=OBn->RealX;
			pr[5]+=OBn->RealY;
			return true;
		}
	}
	return false;
}
void BoidsSingleStep(OneObject* OB,int &dx, int &dy, int &ChangeSpeed)
{
	if(OB->BrigadeID!=0xFFFF&&BoidsParams.InFormation==0)
		return;
	if(OB->UnlimitedMotion)
		return;
	dx>>=3;
	dy>>=3;	
	//int Radius=150;
	//int MinDist=50;
	//int MainDirW=10000; 
	int ds=Norma(dx,dy)+1;
	int Param[6];
	Param[0]=(int)OB;
	Param[1]=BoidsParams.MinDist<<4;
	Param[2]=0;//New dx
	Param[3]=0;//New dy;
	Param[4]=0;//cx
	Param[5]=0;//cy;
	int n=PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,BoidsParams.Radius,CheckNearest,Param);
	int idx=(dx*BoidsParams.MainDirNorma)/ds;
	int idy=(dy*BoidsParams.MainDirNorma)/ds;
	int ndd=Norma(Param[2],Param[3])+1;
	int ndx=Param[2];
	int ndy=Param[3];
	if(ndd>BoidsParams.DensNorma)
	{
		ndx=(ndx*BoidsParams.DensNorma)/ndd;
		ndy=(ndy*BoidsParams.DensNorma)/ndd;
	}
	//int ndx=0;
	//int ndy=0;
	if(n)
	{
		int cx=Param[4]/n;
		int cy=Param[5]/n;
		int dcx=cx-OB->RealX;
		int dcy=cy-OB->RealY;
		int ddc=Norma(dcx,dcy)+1;
		if(ddc>BoidsParams.CentNorma)
		{
			dcx=(dcx*BoidsParams.CentNorma)/ddc;
			dcy=(dcy*BoidsParams.CentNorma)/ddc;
		}
		ndx=ndx*BoidsParams.DensW+dcx*BoidsParams.CentW;
		ndy=ndy*BoidsParams.DensW+dcy*BoidsParams.CentW;
		
		/*
		int ndd2=Norma(ndx,ndy)+1;
		ndx=(ndx*1000)/ndd2;
		ndy=(ndy*1000)/ndd2;
		*/
		if(ndx||ndy)
		{
			int dnd=Norma(ndx,ndy);
			int ldx=(ndx*BoidsParams.LockNorma)/dnd;
			int ldy=(ndy*BoidsParams.LockNorma)/dnd;
			ChangeSpeed=(BoidsParams.ChangeSpeedW*(idx*ndx+idy*ndy)/BoidsParams.MainDirNorma)/1000;
			
			if(MFIELDS[OB->LockType].CheckBar(OB->x+(ldx>>8),OB->y+(ldy>>8),1,1))
			{
				//ndx-=(ldx*BoidsParams.LockW)/BoidsParams.LockNorma;
				//ndy-=(ldy*BoidsParams.LockW)/BoidsParams.LockNorma;
				ndx=0;
				ndy=0;
			}
		}
		
		

		char BaseDir=GetDir(dx,dy);
		char NewDir=GetDir(ndx,ndy);
		byte raz=abs(BaseDir-NewDir);
		//OB->Speed+=24*(64-raz)/64;
	}
	
	
	if(ndx||ndy)
	{
		dx=idx+ndx*BoidsParams.DeviationW;
		dy=idy+ndy*BoidsParams.DeviationW;
	}
}
bool UnitLitleShiftLin(OneObject* OBn,void* param)
{
	int* pr=(int*)param;
	OneObject* OB=(OneObject*)(pr[0]);
	if(OB!=OBn&&OBn&&(!OBn->Sdoxlo))
	{
		int ddx=OB->RealX-OBn->RealX;
		int ddy=OB->RealY-OBn->RealY;
		int ddd=Norma(ddx,ddy);
		if(ddd==0)ddd=1;
		ddx=ddx*4096/ddd;
		ddy=ddy*4096/ddd;
		if(ddd<pr[1])
		{
			int rz=pr[1]-ddd;
			pr[2]+=(ddx*rz)/pr[1];
			pr[3]+=(ddy*rz)/pr[1];
			return true;
		}
	}
	return false;
}
void UnitLitleShift(OneObject* OB)
{
	if(!BoidsParams.PushRadius)return;
	if(OB->UnlimitedMotion)
		return;
	if(OB->BrigadeID!=0xFFFF){
		Brigade* BR=CITY[OB->NNUM].Brigs+OB->BrigadeID;
		if(BR&&BR->WarType>0&&Norma(OB->RealX/16-BR->posX[OB->BrIndex],OB->RealY/16-BR->posY[OB->BrIndex])<8)return;
	}
	int Param[6];
	Param[0]=(int)OB;
	Param[1]=BoidsParams.PushRadius<<4;
	Param[2]=0;//New dx
	Param[3]=0;//New dy;
	int n=PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,40,UnitLitleShiftLin,Param);
	if(n)
	{
		int dx=Param[2];
		int dy=Param[3];
		int ddc=Norma(Param[2],Param[3])+1;
		if(ddc>600)
		{
			dx=(Param[2]*600)/ddc;
			dy=(Param[3]*600)/ddc;
		}
		addrand(dx);addrand(dy);
		OB->RealX+=dx/100;
		OB->RealY+=dy/100;
		addrand(OB->RealX);
		addrand(OB->RealY);
		extern MotionField UnitsField;
		UnitsField.BClrBar(OB->x,OB->y,OB->Lx);
		OB->x=(OB->RealX-((OB->Lx)<<7))>>8;
		OB->y=(OB->RealY-((OB->Lx)<<7))>>8;					
		UnitsField.BSetBar(OB->x,OB->y,OB->Lx);
	}
}

//==================================================================================================================//
//================================           NEW BOIDS              ================================================//
#define NeighborDist 60
DynArray<word> NeighboringUnits;
DynArray<int> UnitsCoordAndPushForce;
void AddUnitToNeighboringListFromCell(OneObject* OB,int xx, int yy)
{
	SAFENEW;
		addrand(xx);
		addrand(yy);
		if(xx>=0&&yy>=0&&xx<VAL_MAXCX&&yy<VAL_MAXCX)
		{
			int cell=xx+(yy<<VAL_SHFCX);
			int NMon=MCount[cell];
			if(NMon)
			{
				addrand(NMon);
				int xu=OB->RealX>>4;
				int yu=OB->RealY>>4;
				int ofs1=cell<<SHFCELL;
				word MID;
				for(int i=0;i<NMon;i++)
				{
					MID=GetNMSL(ofs1+i);
					if(MID!=0xFFFF&&MID>OB->Index)
					{
						OneObject* OBn=Group[MID];
						if(OBn&&(!OBn->Sdoxlo)&&Norma((OBn->RealX>>4)-xu,(OBn->RealY>>4)-yu)<NeighborDist)
						{
							NeighboringUnits.Add(OB->Index);
							NeighboringUnits.Add(MID);
							addrand(MID);
							addrand(NeighboringUnits.GetAmount());							
						}
					}
				}
			}
		}
	ENDNEW;
}
void AddUnitToNeighboringList(word Index)
{
	if(Index!=0xFFFF)
	{
		OneObject* OB=Group[Index];
		if(OB)
		{
			int x=OB->RealX>>9;
			int y=OB->RealY>>9;
			int x0=(x>>2)+1;
			int y0=(y>>2)+1;
			int x1=x0+(x&2)-1;
			int y1=y0+(y&2)-1;
			AddUnitToNeighboringListFromCell(OB,x0,y0);
			AddUnitToNeighboringListFromCell(OB,x0,y1);
			AddUnitToNeighboringListFromCell(OB,x1,y0);
			AddUnitToNeighboringListFromCell(OB,x1,y1);
		}
	}
}
void FillNeighboringList()
{
	SAFENEW;
		NeighboringUnits.FastClear();
		for(int i=0;i<MAXOBJECT;i++)
		{
			AddUnitToNeighboringList(i);
		}
	ENDNEW;
}
void CPF_Stage1(){
    SAFENEW;
		UnitsCoordAndPushForce.Check(MAXOBJECT<<2);
		int kk=UnitsCoordAndPushForce.GetAmount();
		if(kk<(MAXOBJECT<<2))
		{
			UnitsCoordAndPushForce.Add(0,(MAXOBJECT<<2)-kk);
		}
		//int Radius=BoidsParams.Radius<<4;
		int i;
		int ii;
		OneObject* OB;
		int* pUnitsCoordAndPushForce=UnitsCoordAndPushForce.GetValues();
		for(i=0;i<MAXOBJECT;i++)
		{
			ii=i<<2;
			OB=Group[i];
			if(OB)
			{
				OB->NextForceX=0;
				OB->NextForceY=0;
				pUnitsCoordAndPushForce[ii]=OB->RealX;
				pUnitsCoordAndPushForce[ii+1]=OB->RealY;
			}
			else
			{
				pUnitsCoordAndPushForce[ii]=0;
				pUnitsCoordAndPushForce[ii+1]=0;
			}
			pUnitsCoordAndPushForce[ii+2]=0;
			pUnitsCoordAndPushForce[ii+3]=0;
		}
	ENDNEW;
}
void CalculatePushForce()
{
	int n=NeighboringUnits.GetAmount()/2;
	addrand(n);
	if(n)
	{
        CPF_Stage1();		
		int MinDist=BoidsParams.MinDist<<4;
		int* pUnitsCoordAndPushForce=UnitsCoordAndPushForce.GetValues();
		word ActiveIndex=0xFFFF;
		OneObject* OB1=NULL;
		OneObject* OB2=NULL;
		int p=0;
		int dx;
		int dy;
		int Norm;
		int fx;
		int fy;
		int Rx1,Rx2;
		int Ry1,Ry2;
		int UPos1;
		int UPos2;
		for(int i=0;i<n;i++)
		{
			p=i<<1;
			//OB1=Group[NeighboringUnits[p]];
			//OB2=Group[NeighboringUnits[p+1]];
			UPos1=NeighboringUnits[p]<<2;
			UPos2=NeighboringUnits[p+1]<<2;
			addrand(UPos1);
			addrand(UPos2);
			//if(OB1&&OB2)
			{
				//dx=OB2->RealX-OB1->RealX;
				//dy=OB2->RealY-OB1->RealY;
				int * P1=pUnitsCoordAndPushForce+UPos1;
				int * P2=pUnitsCoordAndPushForce+UPos2;
				dx=(*P2)-(*P1);
				dy=(*(P2+1))-(*(P1+1));
				if(dx&&dy)
				{
					//addrand(dx);
					//addrand(dy);
					Norm=Norma(dx,dy)+1;
					int ddn=MinDist-Norm;
					if(ddn>0)
					{
						fx=(dx*ddn*MinDist)/Norm;
						fy=(dy*ddn*MinDist)/Norm;
						*(P1+2)+=-fx;
						*(P1+3)+=-fy;
						*(P2+2)+= fx;
						*(P2+3)+= fy;
						//addrand(fx);
						//addrand(fy);
					}
					/*
					else
					{
						fx=-(dx*10)/Norm;
						fy=-(dy*10)/Norm;
					}
					
					OB1->NextForceX+=-fx;
					OB1->NextForceY+=-fy;
					OB2->NextForceX+= fx;
					OB2->NextForceY+= fy;
					*/
					
				}
			}
		}
	}
}
void BoidsSingleStep2(OneObject* OB,int &dx, int &dy, int &ChangeSpeed)
{
	if(OB->UnlimitedMotion)
		return;
	UnitsCoordAndPushForce.Check(MAXOBJECT<<2);
	int kk=UnitsCoordAndPushForce.GetAmount();
	if(kk<(MAXOBJECT<<2))
	{
		SAFENEW;
			UnitsCoordAndPushForce.Add(0,(MAXOBJECT<<2)-kk);
		ENDNEW;
	}
	addrand(kk);
	dx>>=4;
	dy>>=4;
	int idd=Norma(dx,dy)+1;
	if(idd>16)
	{
		int idx=(dx*BoidsParams.MainDirNorma)/idd;
		int idy=(dy*BoidsParams.MainDirNorma)/idd;
		
		//int ndx=OB->NextForceX;
		//int ndy=OB->NextForceY;
		int UPos=OB->Index<<2;
		int ndx=UnitsCoordAndPushForce[UPos+2];
		addrand(ndx);
		int ndy=UnitsCoordAndPushForce[UPos+3];
		addrand(ndy);
		int ndd=Norma(ndx,ndy)+1;
		addrand(ndd);
		if(ndd>BoidsParams.DensNorma)
		{
			ndx=(ndx*BoidsParams.DensNorma)/ndd;
			ndy=(ndy*BoidsParams.DensNorma)/ndd;
		}
		addrand(ndx);
		addrand(ndy);

		ChangeSpeed=(BoidsParams.ChangeSpeedW*(idx*ndx+idy*ndy)/BoidsParams.MainDirNorma)/1000;
		addrand(ChangeSpeed);
		if(MFIELDS[OB->LockType].CheckPt(OB->x+(ndx>>8),OB->y+(ndy>>8)))
		{
			ndx=0;
			ndy=0;
		}
		dx=idx+ndx;
		dy=idy+ndy;		
	}
}
//==================================================================================================================//
//=====LEADING/SLAVING====//
bool LeadSeveralUnitsAbility::Process(){
	if(ObjID!=0xFFFF&&UnitIndex!=0xFFFF){
		OneObject* OBJ=Group[UnitIndex];
		if(OBJ&&!OBJ->Sdoxlo){
			OneObject* OB=Group[ObjID];
			if(OB&&OB->Serial==ObjSN){
				if(!OB->Sdoxlo){
					OneObject* OBJ=Group[UnitIndex];
					addname(OBJ->Index);
					addname(OB->Index);
					OB->RealX=OBJ->RealX+((int(TCos[OBJ->RealDir])*dx+int(TSin[OBJ->RealDir])*dy)>>4);
					OB->RealY=OBJ->RealY+((int(TCos[OBJ->RealDir])*dy-int(TSin[OBJ->RealDir])*dx)>>4);
					addrand(OB->RealX);addrand(OB->RealY);
					OB->RZ=GetTotalHeight(OB->RealX>>4,OB->RealY>>4);
					if(OB->RZ<2)OB->RZ=2;
					OB->OverEarth=dz;
					OB->NotSavable=1;
					OB->NoZBias=1;
					OB->NotSelectable=1;
					OB->CantRestrictMotion=1;
					OB->StandGround=1;
					OB->DestX=-1;
					if(OB->LocalOrder&&!OB->Attack){
						OB->ClearOrders();
					}
					//OB->RealY+=dz<<3;
					extern MotionField UnitsField;
					UnitsField.BClrBar(OB->x,OB->y,OB->Lx);
					OB->x=(OB->RealX-((OB->Lx)<<7))>>8;
					OB->y=(OB->RealY-((OB->Lx)<<7))>>8;					
					UnitsField.BSetBar(OB->x,OB->y,OB->Lx);
				}
			}
		}
	}
	return true;
}
bool BeSlaveOfUnit::Process(){
	if(ObjID!=0xFFFF){
		OneObject* OBJ=Group[UnitIndex];
		if(OBJ){
			OneObject* OB=Group[ObjID];
			if(OB&&OB->Serial==ObjSN){
				if(OB->Sdoxlo&&OB->Sdoxlo<400){
					if(OBJ->OverEarth>0){
						int dx=((GameSpeed*DeathSpeedX)/256);
						int dy=((GameSpeed*DeathSpeedY)/256);
						int COS=TCos[OB->GraphDir];
						int SIN=TSin[OB->GraphDir];
						int dx1=(dx*COS-dy*SIN)>>8;
						int dy1=(dy*COS+dx*SIN)>>8;
						OBJ->RealX+=dx1;
						OBJ->RealY+=dy1;
						addrand(OB->RealX);
						addrand(OB->RealY);
						LastZ-=(((GameSpeed*OBJ->Sdoxlo)>>8)*DeathAccelerationZ)/16;
						OBJ->OverEarth=LastZ/16;
						if(!OBJ->Sdoxlo)OBJ->Die();
						if(OBJ->OverEarth<=0){
							OBJ->OverEarth=0;							
						}
						return true;
					}
				}
				if(!OBJ->Sdoxlo){
					LastX=OBJ->RealX;
					LastY=OBJ->RealY;
					LastZ=OBJ->OverEarth<<4;
					return true;
				}
			}
			if(!OBJ->Sdoxlo)OBJ->Die();
		}
	}    
	return false;
}
bool UnitsComposition::OnUnitBirth(OneObject* OBJ){
	for(int i=0;i<Units.GetAmount();i++){
		OneUnitInComposition* OUIC=Units[i];
        int id=NATIONS[OBJ->NNUM].CreateNewMonsterAt(OBJ->RealX,OBJ->RealY,OUIC->UnitType,1,OBJ->RealDir);
		if(id!=-1){
			OneObject* OB=Group[id];
			LeadSeveralUnitsAbility* LSUA=new LeadSeveralUnitsAbility;
			LSUA->dx=OUIC->dx;
			LSUA->dy=OUIC->dy;
			LSUA->dz=OUIC->dz;
			LSUA->LeadDistance=OUIC->LeadDist;
			LSUA->ObjID=OB->Index;
			LSUA->ObjSN=OB->Serial;
            LSUA->ActionType=OUIC->LeadingType;
			LSUA->UnitIndex=OBJ->Index;
			BeSlaveOfUnit* BSOU=new BeSlaveOfUnit;
			BSOU->ObjID=OBJ->Index;
			BSOU->ObjSN=OBJ->Serial;
			BSOU->UnitIndex=id;
			BSOU->DeathAccelerationZ=OUIC->ZAccelerationWhenDie;
			BSOU->DeathSpeedX=OUIC->XSpeedWhenDie;
			BSOU->DeathSpeedY=OUIC->YSpeedWhenDie;
			CopyToActive(BSOU);
			AddActiveUnitAbility(id,BSOU);
			CopyToActive(LSUA);
			AddActiveUnitAbility(OBJ->Index,LSUA);
			LSUA->Process();
		}
	}
	return true;
}
//==================================================================================================================//
CannonAutoShot::CannonAutoShot()
{
	Visible=false;
}
bool CannonAutoShot::OnUnitBirth(OneObject* Newbie)
{
	if(Newbie)
	{
		CannonAutoShotActive* ab = new CannonAutoShotActive();
		ab->FileID=FileID;
		ab->SpriteID=SpriteID;
		ab->UnitIndex=Newbie->Index;
		ab->Visible=true;
		CopyToActive(ab);
		return AddActiveUnitAbility(Newbie->Index, ab);
	}
	return false;
}
//==================================================================================================================//
//==================================================================================================================//
bool GetPushkaChargeState(OneObject* OB,int& ChargeType,int& ChargeStage);
extern int GetEnemyUnitsAmount(byte NI,int xc,int yc,int R);
extern int GetFriendUnitsAmount(byte NI,int xc,int yc,int R);
int GetEnemyApproximateUnitsAmount(byte NI,int xc, int yc, int R)
{
	int rez=0;
	byte mask=NATIONS[NI].NMask;
	itr_UnitsInRadius.Create(xc,yc,R);
	while(OneObject* OB=itr_UnitsInRadius.Next()){
		if(!( (OB->NMask&mask) || OB->Sdoxlo || OB->BrigadeID==0xFFFF)){
			rez++;
		}
	}
	return rez;
}
bool CheckSectorForAutoShot(byte NI,int xc,int yc,byte ANG0,byte ANG1,int R){
	int NE=0;
	int NF=0;
	byte mask=NATIONS[NI].NMask;	
	char DA=ANG1-ANG0;
	if(DA<0){
		swap(ANG1,ANG0);
		DA=-DA;
	}
	byte AA=ANG0+DA/2;
	itr_UnitsInRadius.Create(xc+TCos[AA]*R/512,yc+TSin[AA]*R/512,R/2);
	while(OneObject* OB=itr_UnitsInRadius.Next()){
		if(!OB->Sdoxlo){
			byte A=GetDir(OB->RealX/16-xc,OB->RealY/16-yc);
            char DDA=A-ANG0;
			if(DDA>0&&DDA<DA){
				if(OB->NMask&mask){
					NF++;
					if(NF>3)return false;
				}else if(OB->BrigadeID!=0xFFFF){
					NE++;
				}
			}
		}
	}	
	return (NF<=3 && NE>2);
}
int FindCoordForCannon(OneObject* Cannon,int &xx,int &yy, byte Dir)
{
	int rez=0;
	if(Cannon)
	{
		//byte alfa=C->RealDir+EnmAng[SectorN].Dir;
		//byte from=alfa-16;
		byte from=(((Dir)>>4)<<4)-16;
		AdvCharacter* ADC=Cannon->Ref.General->MoreCharacter;
		int ChargeType;
		int ChargeStage;
		if(GetPushkaChargeState(Cannon,ChargeType, ChargeStage))
		{
			if(ChargeStage==100)
			{
				int MinDist=ADC->AttackRadius1[ChargeType];				
				int MaxDist=ADC->AttackRadius2[ChargeType];
				int MaxDistR=MaxDist;
				if(MinDist==0)MaxDist=MaxDist*10/6;
				if(MinDist<80){
					//kartech
					int pusd=(((Dir+8+512)>>4)&15)<<4;
					if(CheckSectorForAutoShot(Cannon->NNUM,Cannon->RealX/16,Cannon->RealY/16,pusd-13,pusd+13,MaxDist)){
						xx=Cannon->RealX/16+TCos[pusd]/2;
						yy=Cannon->RealY/16+TSin[pusd]/2;
                        return 1;
					}
					return 0;
				}
				MaxDist+=600;
				int StepN=64;
				int Step=(MaxDist-MinDist)/StepN;
				int x=Cannon->RealX>>4;
				int y=Cannon->RealY>>4;
				for(byte i=0;i<32;i++)
				{
					byte an=from+i;
					for(int k=0;k<StepN;k++)
					{
						int D=MinDist+k*Step;
						int dx=x+((TCos[an]*D)>>8);
						int dy=y+((TSin[an]*D)>>8);
						int dist=Norma(x-dx,y-dy);						
						if(dist<MaxDist)
						{
							char EnDir=char(GetDir(dx-x,dy-y));
							int enmd=((EnDir+8+512)>>4)&15;
							int enmd1=enmd;
							int enmd2=enmd;int pusd=((Dir+8+512)>>4)&15;							
							if(enmd==pusd)
							{
								int Razbros=100;//(512*(dist>>5)*ADC->Razbros)/32000;
								int Sq=(314*Razbros*Razbros)>>14;
								//int EAmo=GetEnemyUnitsAmount(Cannon->NNUM,dx,dy,Razbros);
								//int FAmo=GetFriendUnitsAmount(Cannon->NNUM,dx,dy,Razbros);
								int NE=GetEnemyApproximateUnitsAmount(Cannon->NNUM,dx,dy,Razbros);
								if(NE<3)NE=0;
								int Den=(NE*100000)/(Sq+1);//((EAmo-4*FAmo)*100000)/(Sq+1);
								if(Den>rez)
								{
									int DH=Cannon->RZ-GetTotalHeight0(xx,yy);
									if(DH<0)DH=0;
									else DH=(DH*EngSettings.CannonAddShotDistPer100_Height)/100;
									int R=Norma(dx-x,dy-y);
									if(R<MaxDistR+DH){
										rez=Den;
										xx=dx;
										yy=dy;
										if(MinDist==80){
											xx=x+((TCos[Cannon->RealDir]*120)>>8);
											yy=y+((TSin[Cannon->RealDir]*120)>>8);
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
	return rez;
}
int FindCoordForCannon(OneObject* Cannon,int &xx,int &yy)
{
	return FindCoordForCannon(Cannon,xx,yy,Cannon->RealDir);
}
//==================================================================================================================//
CannonAutoShotActive::CannonAutoShotActive()
{
	On=false;
	Hint=GetTextByID("#AutoShot_is_Off");
}
void ClearCannonAutoShot(OneObject* OB){
	if(OB->ActiveAbility){
		int N=OB->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<N;i++){
			ActiveUnitAbility* AUA=OB->ActiveAbility->ActiveAbilities[i];
			if(!strcmp(AUA->GetClassName(),"CannonAutoShotActive")){
				CannonAutoShotActive* CAS=(CannonAutoShotActive*)AUA;
				if(CAS->On)CAS->OnClick();
			}
		}
	}
}
bool CannonAutoShotActive::Process()
{
	if(On&&UnitIndex!=0xFFFF&&UnitIndex!=-1)
	{
		OneObject* OB = Group[UnitIndex];
		if(OB)
		{
			int GetDestDir(OneObject* OB);
			if( OB->DestX!=-1 || GetDestDir(OB)!=-1 )
			{
				OnClick();
				return true;
			}
			int xx,yy;
			if(FindCoordForCannon(OB,xx,yy))
			{
				OB->NewAttackPoint(xx,yy,128+16,0,0);
			}
		}
		bool TestFillingAbility(OneObject* OB);
		if(TestFillingAbility(OB)){
			OnClick();		
		}
	}	
	return true;
}
bool CannonAutoShotActive::OnClick()
{
	On=!On;
	if(On)
		Hint=GetTextByID("#AutoShot_is_ON");
	else
		Hint=GetTextByID("#AutoShot_is_OFF");
	return true;
}
//==================================================================================================================//
RomeHeroCollector RomeHeroes; 
//==================================================================================================================//
LeveledActiveUnitAbility::LeveledActiveUnitAbility()
{
	Level=1;
	VirtualUp=false;
	Le=&Level;
	Vi=&VirtualUp;
	HintParam=0;
}
bool LeveledActiveUnitAbility::UpLevel()
{
	bool rez=false;
	if(*Le<5)
	{
		(*Le)++;
		rez=true;
	}
	*Vi=false;
	return rez;
}
bool LeveledActiveUnitAbility::OnClick()
{
	if(*Vi)
	{
		if(OB)
		{
			OB->Life+=100;
			OB->MaxLife+=100;
			OB->AddDamage+=5;
			OB->AddShield+=1;
		}
		UpLevel();
		if(OB&&OB->ActiveAbility)
		{
			int n = OB->ActiveAbility->ActiveAbilities.GetAmount();
			for(int i=0;i<n;i++)
			{
				LeveledActiveUnitAbility* Lv = dynamic_cast<LeveledActiveUnitAbility*> (OB->ActiveAbility->ActiveAbilities[i]);
				if(Lv)
				{
					*(Lv->Vi)=false;
					UnitAbility2* A=(UnitAbility2*)Lv->GetA();
					if(A)
					{
						//Lv->SpriteID=A->SpriteID;
						//Lv->FileID=A->FileID;
					}
				}
			}
		}
		return true;
	}
	return false;
}
bool LeveledActiveUnitAbility::CanApply()
{
	return *Vi;
}
const char* LeveledActiveUnitAbility::GetHint()
{
	TempHint.Clear();
	for(int i=1;i<6;i++)
	{
		if(Le&&i==*Le)
		{
			TempHint.Add("{CR}");
			TempHint.Add(GetHintOnLevel(i));
			TempHint.Add("{C}");
			TempHint.Add("\\");
		}
		else
		{
			TempHint.Add(GetHintOnLevel(i));
			TempHint.Add("\\");
		}
	}
	/*
	TempHint=Hint.str;//GetTextByID(Hint.str);
	static char tm[256];
	tm[0]=0;
	if(HintParam)
	{
		//TempHint.Add(" %d");
		sprintf(tm,TempHint.str,HintParam);
	}
	else
	{
		strcpy(tm,TempHint.str);
	}
	//TempHint=GetTextByID("Level");
	//TempHint.Add(" ");
	//TempHint.Add(*Le);
	//TempHint.Add("\\");
	TempHint=tm;
	*/
	return TempHint.str;
}
const char* LeveledActiveUnitAbility::GetHintOnLevel(int Lev)
{
	TempHintLv=Hint.str;
	if(Lev>0&&Lev<6)
	{
		int HP = GetHintParamOnLevel(Lev);
		static char tm[256];
		tm[0]=0;
		if(HP)
		{
			sprintf(tm,TempHintLv.str,HP);
		}
		else
		{
			strcpy(tm,TempHintLv.str);
		}
		TempHintLv=tm;
	}
	return TempHintLv.str;
}
int LeveledActiveUnitAbility::GetHintParamOnLevel(int Lev)
{
	return 0;
}
//==================================================================================================================//
RomeHero::RomeHero()
{
	ExpProcIfNotKiller=100;
	IsHero=false;
	GatherExperienceInCampaign=false;
	UpOrder[0]=0;
	UpOrder[1]=0;
	UpOrder[2]=0;
	StayBack=false;
}
ActiveUnitAbility* RomeHero::GetActiveAbility()
{
	return new ActiveRomeHero();
}
//==================================================================================================================//
ActiveRomeHero::ActiveRomeHero()
{
	Level=1;
	Expa=0;
	ExpaToNextLevel=-1;
	WaitForAbilUp=false;
	LoadFromProfile=false;
	Le=NULL;
	Ex=NULL;
	Wa=NULL;
}
ActiveRomeHero::~ActiveRomeHero()
{
	RomeHeroes.DelRomeHero(this);
}
int ActiveRomeHero::GetExperienceToNextLevel()
{
	int rez=-1;
	RomeHero* Hero = (RomeHero*) GetA();
	if(Hero&&Le)
	{
		int n=Hero->LevelUp.GetAmount();
		if(*Le<=n)
		{
			for(int i=0;i<*Le;i++)
				rez+=Hero->LevelUp[i];
			if(rez>-1){
				rez++;
				//rez-=*Ex;
			}
		}	
	}
	return rez;
}
int ActiveRomeHero::UpLevel()
{
	return ++*Le;
}
int ActiveRomeHero::AddExperience(int Expa)
{
	return *Ex+=Expa*100;
}
int ActiveRomeHero::GetLevel()
{
	if(Le)
		return *Le;
	else
		return Level;
}
int ActiveRomeHero::GetExperience()
{
	if(Ex)
		return *Ex/100;
	else
		return Expa;
}
void ActiveRomeHero::GatherExperience(OneObject* Victim,OneObject* Killer, byte Part)
{
	RomeHero* Hero = (RomeHero*) GetA();
	if(Hero&&Ex&&((vGameMode==gmCamp&&Hero->GatherExperienceInCampaign)||vGameMode!=gmCamp))
	{
		int exp=0;
		if(OB==Killer)
			exp=Victim->newMons->Expa*100;
		else if (Part!=0)
			exp=(Victim->newMons->Expa*Hero->ExpProcIfNotKiller)/Part;
			//exp=(Victim->newMons->Expa*Hero->ExpProcIfNotKiller/100)/Part;
		(*Ex)+=exp;
		Hint=GetTextByID("Level: ");
		Hint.Add(*Le);
		Hint.Add(GetTextByID("\\ Experience: "));
		Hint.Add(*Ex/100);
	}
}
bool ActiveRomeHero::Process(OneObject* OB)
{
	if(!LoadFromProfile)
	{
		if(vGameMode==gmCamp)
		{
			int n=vmSinglePlayerData.Heri.GetAmount();
			SinglePlayerData_CampaignHeroInfo* CHI=NULL;
			if(n<=vmCampID)
			{
				for(int i=0;i<vmCampID-n+1;i++){
					CHI = new SinglePlayerData_CampaignHeroInfo();
					vmSinglePlayerData.Heri.Add(CHI);
				}
			}
			else
			{
				CHI = vmSinglePlayerData.Heri[vmCampID];
			}
			SinglePlayerData_HeroesInfoList* HIL=NULL;
			SinglePlayerData_HeroesInfoList* PreviosHIL=NULL;
			SinglePlayerData_HeroInfo* PreviosHr=NULL;
			if(CHI)
			{
				int nn=CHI->CampaignMissions.GetAmount();
				int nm=vmCampaigns[vmCampID]->curMission;
				if(nn<=nm)
				{
					for(int i=0;i<nm-nn+1;i++){
						HIL = new SinglePlayerData_HeroesInfoList();
						CHI->CampaignMissions.Add(HIL);
					}
				}
				else
				{
					HIL = CHI->CampaignMissions[nm];
				}
				int PreviosMissID=nm-1;
				while(PreviosHr==NULL&&PreviosMissID>-1&&PreviosMissID<=nn)
				{
					PreviosHIL = CHI->CampaignMissions[PreviosMissID];
					if(PreviosHIL)
					{
						int nnn=PreviosHIL->MissionHeroes.GetAmount();
						for(int i=0;i<nnn;i++)
						{
							if(PreviosHIL->MissionHeroes[i]->HeroType==OB->NIndex)
							{
								PreviosHr=PreviosHIL->MissionHeroes[i];
								break;
							}
						}
					}
					PreviosMissID--;
				}
			}
			//
			SinglePlayerData_HeroInfo* Hr=NULL;
			int nnn=HIL->MissionHeroes.GetAmount();
			for(int i=0;i<nnn;i++)
			{
				if(HIL->MissionHeroes[i]->HeroType==OB->NIndex)
				{
					Hr=HIL->MissionHeroes[i];
					break;
				}
			}
			
			
			if(!Hr)
			{
				Hr = new SinglePlayerData_HeroInfo();
				HIL->MissionHeroes.Add(Hr);
				Hr->HeroType=OB->NIndex;
				Hr->Level=Level;
				Hr->Expa=Expa;
				Hr->WaitForAbilUp=WaitForAbilUp;
			}
			
			Le=&Hr->Level;
			Ex=&Hr->Expa;
			Wa=&Hr->WaitForAbilUp;
			if(!vGameLoaing)
			{
				*Le=1;
				*Ex=0;
				*Wa=false;
			}
			
			n = OB->ActiveAbility->ActiveAbilities.GetAmount();
			
			int In=0;
			int na=Hr->Abilki.GetAmount();
			for(int i=0;i<n;i++)
			{
				LeveledActiveUnitAbility* Lv = dynamic_cast<LeveledActiveUnitAbility*> (OB->ActiveAbility->ActiveAbilities[i]);
				if(Lv)
				{
					SinglePlayerData_HeroAbilityInfo* AInfo = NULL;
					if(In<na)
					{
						AInfo=Hr->Abilki[In];
					}
					else
					{
						AInfo = new SinglePlayerData_HeroAbilityInfo();
						Hr->Abilki.Add(AInfo);
					}
					Lv->Le=&AInfo->Level;
					Lv->Vi=&AInfo->VirtualUp;
					if(!vGameLoaing)
					{
						*Lv->Le=1;
						*Lv->Vi=false;
					}
					In++;
				}
			}

			if(PreviosHr&&!vGameLoaing)
			{
				*Le=PreviosHr->Level;
				*Ex=PreviosHr->Expa;
				*Wa=PreviosHr->WaitForAbilUp;
				*Wa=false;

				n = OB->ActiveAbility->ActiveAbilities.GetAmount();
				int In=0;
				int na=PreviosHr->Abilki.GetAmount();
				for(int i=0;i<n;i++)
				{
					LeveledActiveUnitAbility* Lv = dynamic_cast<LeveledActiveUnitAbility*> (OB->ActiveAbility->ActiveAbilities[i]);
					if(Lv)
					{
						UnitAbility2* A=(UnitAbility2*)Lv->GetA();
						if(A)
						{
							if(In<na)
							{
								int l=PreviosHr->Abilki[In]->Level;
								//*(Lv->Le)=l;
								if(*(Lv->Vi))
								{
									//Lv->SpriteID=A->SpriteID2;
									//Lv->FileID=A->FileID2;
								}
								for(int k=1;k<l;k++)
								{
									Lv->UpLevel();
								}
								*(Lv->Vi)=PreviosHr->Abilki[In]->VirtualUp;
								*Lv->Vi=false;
							}
							In++;
						}
					}
				}
			}			
			
		}
		else
		{
			Le=&Level;
			Ex=&Expa;
			Wa=&WaitForAbilUp;
			//
			for(int j=0;j<MAXOBJECT;j++)
			{
				OneObject* OO = Group[j];
				if(OO&&OO->NIndex==OB->NIndex&&OO->NNUM==OB->NNUM&&OO->Sdoxlo>=400)
				{
					int n = OO->ActiveAbility->ActiveAbilities.GetAmount();
					int inL=0;
					for(int i=0;i<n;i++)
					{
						LeveledActiveUnitAbility* Lv = dynamic_cast<LeveledActiveUnitAbility*> (OO->ActiveAbility->ActiveAbilities[i]);
						if(Lv)
						{
							int m = OB->ActiveAbility->ActiveAbilities.GetAmount();
							int inLbase=0;
							for(int k=0;k<m;k++)
							{
								LeveledActiveUnitAbility* LvNew = dynamic_cast<LeveledActiveUnitAbility*> (OB->ActiveAbility->ActiveAbilities[k]);
								if(LvNew)
								{
									if(inL==inLbase)
									{
										*LvNew->Le=*(Lv->Le);
										*LvNew->Vi=*(Lv->Vi);
										break;
									}
									inLbase++;
								}
							}
							inL++;
						}
						else
						{
							ActiveRomeHero *OORh = dynamic_cast<ActiveRomeHero*> (OO->ActiveAbility->ActiveAbilities[i]);
							if(OORh)
							{
								*Le=*(OORh->Le);
								*Ex=*(OORh->Ex);
								*Wa=*(OORh->Wa);
							}
						}
					}
					OO->Sdoxlo=1001;
					OB->MaxLife=OO->MaxLife;
					OB->Life=OO->MaxLife;
					OB->AddDamage=OO->AddDamage;
					OB->AddShield=OO->AddShield;
					//
					break;
				}
			}
		}
		RomeHeroes.AddRomeHero(this);
		LoadFromProfile=true;
	}
	if(vGameMode!=gmCamp)
	{	
		if(ExpaToNextLevel==-1)
		{
			ExpaToNextLevel=GetExperienceToNextLevel();
		}
		if((*Ex/100)>=ExpaToNextLevel&&ExpaToNextLevel!=-1&&!(*Wa))
		{
			(*Le)++;
			ExpaToNextLevel=GetExperienceToNextLevel();
			if(OB&&OB->ActiveAbility)
			{
				int n = OB->ActiveAbility->ActiveAbilities.GetAmount();
				for(int i=0;i<n;i++)
				{
					LeveledActiveUnitAbility* Lv = dynamic_cast<LeveledActiveUnitAbility*> (OB->ActiveAbility->ActiveAbilities[i]);
					if(Lv&&*(Lv->Le)<5)
					{
						*(Lv->Vi)=true;
						(*Wa)=true;
						/*
						UnitAbility2* A=(UnitAbility2*)Lv->GetA();
						if(A)
						{
							//Lv->SpriteID=A->SpriteID2;
							//Lv->FileID=A->FileID2;
						}
						*/
					}
				}
			}
			if(OB&&OB->NNUM==GetMyNation())
			{
				void PlaySound(char* Name);
				PlaySound("HERO_CAN_UP_LEVEL");
			}

			Hint=GetTextByID("Level: ");
			Hint.Add(*Le);
			Hint.Add(GetTextByID("\\ Experience: "));
			Hint.Add(*Ex);
		}
		if(*Wa)
		{
			if(OB&&OB->ActiveAbility)
			{
				int n = OB->ActiveAbility->ActiveAbilities.GetAmount();
				*Wa=false;
				for(int i=0;i<n;i++)
				{
					LeveledActiveUnitAbility* Lv = dynamic_cast<LeveledActiveUnitAbility*> (OB->ActiveAbility->ActiveAbilities[i]);
					if(Lv)
					{
						if(*(Lv->Vi))
						{
							*Wa=true;
							break;
						}
					}
				}
			}
		}
	}
	if(OB->Sdoxlo>1000)
	{
		return false;
	}
	if(OB->Sdoxlo>400)
	{
		OB->Sdoxlo=400;
	}
	return true;
}
bool ActiveRomeHero::IsHero()
{
	RomeHero* HL = (RomeHero*)GetA();
	if(HL)
	{
		return HL->IsHero;
	}
	return false;
}
int ActiveRomeHero::GetAmountFreeLevels()
{
	int rez=0;
	if(Le&&Wa&&*Wa)
	{
		RomeHero* H = (RomeHero*)GetA();
		if(H)
		{
			int n = H->LevelUp.GetAmount();
			int Exp=0;
			for(int i=0;i<n;i++)
			{
				Exp+=H->LevelUp[i];
				if((i+2)>=*Le&&Exp!=0)
				{
					if(Exp<=*Ex/100)
					{
						rez++;
					}
					else
					{
						break;
					}
				}
			}
		}
	}
	return rez;
}
//==================================================================================================================//
RomeHeroCollector::RomeHeroCollector()
{
	Calk=false;
}
void RomeHeroCollector::ClearAll()
{
	Herosima.Clear();
	NHeroInMap.Clear();
}
bool RomeHeroCollector::AddExp(OneObject* Victim,OneObject* Killer)
{
	int n=Herosima.GetAmount();
	if(!Calk)
	{
		int tm=0;
		NHeroInMap.Clear();
		for(int i=0;i<8;i++)NHeroInMap.Add(0);
		for(i=0;i<n;i++)
		{
			ActiveRomeHero* RH=Herosima[i];
			if(RH&&RH->OB&&!RH->OB->Sdoxlo)
			{
				NHeroInMap[RH->OB->NNUM]++;
			}
		}
		Calk=true;
	}
	int nnh=NHeroInMap.GetAmount();
	if(nnh>Killer->NNUM)
	{
		int nh=NHeroInMap[Killer->NNUM];
		for(int i=0;i<n;i++)
		{
			ActiveRomeHero* RH=Herosima[i];
			if(RH&&RH->OB&&RH->OB->NNUM==Killer->NNUM)
			{
				if(!RH->OB->Sdoxlo)
				{
					RH->GatherExperience(Victim,Killer,NHeroInMap[RH->OB->NNUM]);
				}
			}
		}
	}
	return true;
}
void RomeHeroCollector::AddRomeHero(ActiveRomeHero* Her)
{
	Herosima.Add(Her);
	Calk=false;
}
void RomeHeroCollector::DelRomeHero(ActiveRomeHero* Her)
{
	int n=Herosima.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(Herosima[i]==Her)
		{
			Herosima[i]=NULL;
		}
	}
	Calk=false;
}
//==================================================================================================================//
LifeLimitation::LifeLimitation()
{
	DieWeaponEffect="";
}
bool LifeLimitation::OnUnitBirth(OneObject* Newbie)
{
	bool rez=false;
	if(Newbie)
	{
		ActiveLifeLimitation* ab = new ActiveLifeLimitation();
		ab->UnitIndex=Newbie->Index;
		ab->OB=Newbie;
		ab->Visible=true;
		ab->SetA(this);
		//ab->HVS=GetHeroVariableStorage(Newbie);
		ab->DieTime=AnimTime+LifeLength.Get()*25*256;
		CopyToActive(ab);
		return AddActiveUnitAbility(Newbie->Index, ab);
	}
	return rez;
}
//==================================================================================================================//
ActiveLifeLimitation::ActiveLifeLimitation()
{
	DieTime=0;
}
bool ActiveLifeLimitation::Process()
{
	bool rez=false;
	if(OB&&!OB->Sdoxlo)
	{
		if(DieTime!=0&&AnimTime>DieTime)
		{
			OB->Die();
			LifeLimitation* ab = (LifeLimitation*)GetA();
			if(ab->DieWeaponEffect.str!="")
			{
				AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
				CreateNewActiveWeapon(ab->DieWeaponEffect.str,OB->Index,OB->RealX>>4,OB->RealY>>4,OB->RZ,0xFFFF,OB->RealX>>4,OB->RealY>>4,OB->RZ,AWP);
			}
		}
		else
		{
			rez=true;
		}
	}
	return rez;
}
//==================================================================================================================//
GreetingMe::GreetingMe()
{
	Radius=0;
}
bool GreetingMe::OnUnitBirth(OneObject* Newbie)
{
	bool rez=false;
	if(Newbie)
	{
		ActiveGreetingMe* ab = new ActiveGreetingMe();
		ab->UnitIndex=Newbie->Index;
		ab->OB=Newbie;
		ab->Visible=Visible;
		ab->SetA(this);
		ab->Hint.Add(Name);
		CopyToActive(ab);
		return AddActiveUnitAbility(Newbie->Index, ab);
	}
	return rez;
}
//==================================================================================================================//
ActiveGreetingMe::ActiveGreetingMe()
{
	UseTime=0;
}
void TestCGARB();
bool ActiveGreetingMe::Process()
{
	bool rez=false;
	if((!OB)&&UnitIndex!=0xFFFF)
	{
		OB=Group[UnitIndex];
	}
	if(OB&&!OB->Sdoxlo)
	{
		if(OB->DestX>0)
		{
			if((UseTime+256*25*8)<AnimTime)
			{
				GreetingMe* Gm = (GreetingMe*)GetA();
				if(Gm)
				{
					int Param[6];
					Param[0]=OB->RealX>>4;
					Param[1]=OB->RealY>>4;
					Param[3]=OB->NNUM;
					Param[4]=OB->Index;
					PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,Gm->Radius,&ActiveGreetingMe::AddActiveGreeting,(void*)Param);
					UseTime=AnimTime;
				}
			}
		}
		rez=true;
	}
	return rez;
}
bool ActiveGreetingMe::AddActiveGreeting(OneObject* OB,void* Param)
{
	bool rez=false;
	if(OB&&!OB->Sdoxlo&&Param)
	{
		int* P = (int*) Param;
		if(OB->NNUM==P[3]&&OB->LocalOrder==NULL&&OB->Index!=P[4]&&OB->EnemyID==0xFFFF&&OB->DestX==-1&&OB->NewState==0)
		{
			OneObject* GreetingObject=Group[P[4]];
			char Dirr=(char)GetDir(GreetingObject->RealX-OB->RealX,GreetingObject->RealY-OB->RealY);
			char dd = char(OB->RealDir)-Dirr;
			if(abs(dd)<60)
			{
				if(OB->ActiveAbility)
				{
					int n = OB->ActiveAbility->ActiveAbilities.GetAmount();
					for(int i=0;i<n;i++)
					{
						if(OB->ActiveAbility->ActiveAbilities[i]->Type==9)
							return false;
					}
				}
				ActiveGreeting* g = new ActiveGreeting();
				g->GreetingObject=Group[P[4]];
				g->OB=OB;
				g->UnitIndex=OB->Index;
				//CopyToActive(g);
				if(AddActiveUnitAbility(OB->Index, g))
				{
					rez=true;
				}
				else
				{
					delete g;
				}
			}
		}
	}
	return rez;
}
//==================================================================================================================//
ActiveGreeting::ActiveGreeting()
{
	Type=9;
	GreetingObject=NULL;
	PrevDir=-1;
	State=0;
}
bool ActiveGreeting::Process()
{
	if((!OB)&&UnitIndex!=0xFFFF)
	{
		OB=Group[UnitIndex];
	}
	if(OB&&GreetingObject&&!OB->Sdoxlo&&OB->EnemyID==0xFFFF&&OB->DestX==-1)
	{
		if(OB->FrameFinished)
		{
			if(State==0)
			{
				char Dirr=(char)GetDir(GreetingObject->RealX-OB->RealX,GreetingObject->RealY-OB->RealY);
				char dd = char(OB->RealDir)-Dirr;
				if(abs(dd)<50)
				{
					NewAnimation* NA =OB->newMons->GetAnimation(anm_Greeting);
					if(NA&&NA->Enabled)
					{
						OB->NewAnm=NA;
						OB->SetZeroFrame();
						State=2;
						return true;
					}
				}
				/*
				NewAnimation* NA =OB->newMons->GetAnimation(anm_Greeting);
				if(NA&&NA->Enabled)
				{
					if(abs(dd)<50)
					{
						State=1;
						return true;
					}
					
					if(OB->BrigadeID==0xFFFF)
					{
						if(abs(dd)>16)
						{
							PrevDir=OB->RealDir;
							RotateMon(OB,-dd);
						}
						State=1;
					}
					else
					{
						if(abs(dd)<60)
						{
							State=1;
						}
						else
						{
							return false;
						}
					}
				}
				*/
				return false;
			}
			/*
			if(State==1)
			{
				NewAnimation* NA =OB->newMons->GetAnimation(anm_Greeting);
				if(NA&&NA->Enabled)
				{
					OB->NewAnm=NA;
					OB->SetZeroFrame();
					State=2;
					return true;
				}
			}
			*/
			if(State==2)
			{
				return false;
			}
		}
		return true;
	}
	return false;
}
//==================================================================================================================//
NationBonus::NationBonus()
{
	AddDamage=0;
	AddShield=0;
}
bool NationBonus::OnUnitBirth(OneObject* Newbie)
{
	bool rez=false;
	if(Newbie)
	{
		if(AddDamage||AddShield||UseUpgrade)
		{
			ActiveNationBonus* ab = new ActiveNationBonus();
			ab->UnitIndex=Newbie->Index;
			ab->OB=Newbie;
			ab->Visible=Visible;
			ab->FileID=FileID;
			ab->SpriteID=SpriteID;
			ab->Hint.Add(GetTextByID(Name.str));
			ab->SetA(this);
			CopyToActive(ab);
			if(AddActiveUnitAbility(Newbie->Index, ab))
			{
				int N = UnitsType.GetAmount();
				if(N)
				{
					for(int i=0;i<N;i++)
					{
						AdvCharacter* ADV = NATIONS[Newbie->NNUM].Mon[UnitsType[i]->UnitType]->MoreCharacter;
						if(ADV)
						{
							for(int j=0;j<NAttTypes;j++)
							{
								if(ADV->MaxDamage[j])
									ADV->MaxDamage[j]+=AddDamage;
							}
							ADV->Shield+=AddShield;
						}
					}
				}
				else
				{
					for(int i=0;i<NATIONS[Newbie->NNUM].NMon;i++)
					{
						AdvCharacter* ADV = NATIONS[Newbie->NNUM].Mon[i]->MoreCharacter;
						if(ADV)
						{
							for(int j=0;j<NAttTypes;j++)
							{
								if(ADV->MaxDamage[j])
									ADV->MaxDamage[j]+=AddDamage;
							}
							ADV->Shield+=AddShield;
						}
					}
				}
				if(UseUpgrade)
				{
					PerformNewUpgrade(NATIONS+Newbie->NNUM,UpgradeBonus,NULL);
				}
				rez=true;
			}
		} 
	}
	return rez;
}
//==================================================================================================================//
ActiveNationBonus::ActiveNationBonus()
{
	m_die=false;
}
bool ActiveNationBonus::Process()
{
	bool rez=false;
	if((!OB)&&UnitIndex!=0xFFFF)
	{
		OB=Group[UnitIndex];
	}
	if(OB)
	{
		if(*Le<6&&*Le>0)
		{
			NationBonus* NB = (NationBonus*) GetA();
			if(NB)
			{
				HintParam=NB->HintParam[*Le-1];
			}
		}
		rez=true;
	}
	if((!OB)||(OB&&OB->Sdoxlo))
	{
		rez=false;
		if(!m_die)
		{
			NationBonus* NB = (NationBonus*) GetA();
			if(NB)
			{
				int N = NB->UnitsType.GetAmount();
				if(N)
				{
					for(int i=0;i<N;i++)
					{
						AdvCharacter* ADV = NATIONS[OB->NNUM].Mon[NB->UnitsType[i]->UnitType]->MoreCharacter;
						if(ADV)
						{
							for(int j=0;j<NAttTypes;j++)
							{
								if(ADV->MaxDamage[j])
									ADV->MaxDamage[j]-=NB->AddDamage;
							}
							ADV->Shield-=NB->AddShield;
						}
					}
				}
				else
				{
					for(int i=0;i<NATIONS[OB->NNUM].NMon;i++)
					{
						AdvCharacter* ADV = NATIONS[OB->NNUM].Mon[i]->MoreCharacter;
						if(ADV)
						{
							for(int j=0;j<NAttTypes;j++)
							{
								if(ADV->MaxDamage[j])
									ADV->MaxDamage[j]-=NB->AddDamage;
							}
							ADV->Shield-=NB->AddShield;
						}
					}
				}
				if(NB->UseUpgrade)
				{
					if((*Le)>4)
						PerformNewUpgrade(NATIONS+OB->NNUM,NB->IfDieDowngradeL5,NULL);
					if((*Le)>3)
						PerformNewUpgrade(NATIONS+OB->NNUM,NB->IfDieDowngradeL4,NULL);
					if((*Le)>2)
						PerformNewUpgrade(NATIONS+OB->NNUM,NB->IfDieDowngradeL3,NULL);
					if((*Le)>1)
						PerformNewUpgrade(NATIONS+OB->NNUM,NB->IfDieDowngradeL2,NULL);
					PerformNewUpgrade(NATIONS+OB->NNUM,NB->IfDieDowngrade,NULL);
				}
			}
			m_die=true;
		}
	}
	if(OB&&OB->Sdoxlo>0&&OB->Sdoxlo<1000)
		rez=true;
	return rez;
}
bool ActiveNationBonus::UpLevel()
{
	bool rez=false;
	NationBonus* NB = (NationBonus*) GetA();
	if(*Le<5&&NB)
	{
		(*Le)++;
		int Up=-1;
		if(*Le==2)
			Up=NB->UpgradeBonusL2;
		if(*Le==3)
			Up=NB->UpgradeBonusL3;
		if(*Le==4)
			Up=NB->UpgradeBonusL4;
		if(*Le==5)
			Up=NB->UpgradeBonusL5;
		if(Up!=-1)
		{
			PerformNewUpgrade(NATIONS+OB->NNUM,Up,NULL);
			rez=true;
		}
	}
	return rez;
}
int ActiveNationBonus::GetHintParamOnLevel(int Lev)
{
	if(Lev<6&&Lev>0)
	{
		NationBonus* NB = (NationBonus*) GetA();
		if(NB)
		{
			return NB->HintParam[Lev-1];
		}
	}
	return 0;
}
//==================================================================================================================//
HeroLight::HeroLight()
{
	EffectName="";
	DieEffect="";
	IsHero=true;
}
bool HeroLight::OnUnitBirth(OneObject* Newbie)
{
	if(Newbie)
	{
		ActiveHeroLight* AA = new ActiveHeroLight();		
		AA->UnitIndex=Newbie->Index;
		AA->OB=Newbie;
		AA->SetA(this);
		AA->Visible=Visible;
		CopyToActive(AA);
		return AddActiveUnitAbility(Newbie->Index, AA);
	}
	return false;
}
//==================================================================================================================//
ActiveHeroLight::ActiveHeroLight()
{
	IsInit=false;
}
bool ActiveHeroLight::IsHero()
{
	HeroLight* HL = (HeroLight*)GetA();
	if(HL)
	{
		return HL->IsHero;
	}
	return false;
}
bool ActiveHeroLight::Process()
{
	bool rez=false;
	if(!OB)
	{
		if(UnitIndex!=0xFFFF)
			OB=Group[UnitIndex];
	}
	HeroLight* HL = (HeroLight*)GetA();
	if(OB&&HL)
	{
		if(!OB->Sdoxlo)
		{
			if(!IsInit&&HL->EffectName.L>0)
			{

				CreateNewActiveWeapon(HL->EffectName.str,OB->Index,OB->RealX>>4,OB->RealY>>4,OB->RZ,OB->Index,OB->RealX>>4,OB->RealY>>4,OB->RZ,NULL);
				IsInit=true;
			}
			rez=true;
		}
		else
		{
			if(HL->DieEffect.L>0)
				CreateNewActiveWeapon(HL->DieEffect.str,OB->Index,OB->RealX>>4,OB->RealY>>4,OB->RZ,OB->Index,OB->RealX>>4,OB->RealY>>4,OB->RZ,NULL);
		}
	}
	return rez;
}
//==================================================================================================================//
LifeRegeneration::LifeRegeneration()
{
	Regeneration=0;
	Radius=0;
	UsePause=0;
	CoolDownTime=0;
}
ActiveUnitAbility* LifeRegeneration::GetActiveAbility()
{
	return (ActiveUnitAbility*) new ActiveLifeRegeneration();
}
//==================================================================================================================//
ActiveLifeRegeneration::ActiveLifeRegeneration()
{
	LastUseTime=0;
}
bool ActiveLifeRegeneration::Process()
{
	bool rez=false;
	if(!OB)
	{
		if(UnitIndex!=0xFFFF)
			OB=Group[UnitIndex];
	}
	LifeRegeneration* A = (LifeRegeneration*)GetA();
	if(OB&&(!OB->Sdoxlo)&&A)
	{
		if(!A->CoolDownTime)
		{
			if(AnimTime>(LastUseTime+A->UsePause*256*25/10))
			{
				Exec();
			}
		}
		rez=true;
	}
	return rez;
}
bool ActiveLifeRegeneration::CanApply()
{
	LifeRegeneration* A = (LifeRegeneration*)GetA();
	if(A&&A->CoolDownTime)
	{
		return true;
	}
	return false;
}
int ActiveLifeRegeneration::GetCoolDownProc()
{
	int rez=0;
	LifeRegeneration* A = (LifeRegeneration*)GetA();
	if(A&&A->CoolDownTime)
	{
		int cool=A->CoolDownTime*25*256;
		if(cool)
			rez=(LastUseTime+cool-AnimTime)*100/cool;
		if(rez<0)
			rez=0;
	}
	return rez;
}
bool ActiveLifeRegeneration::ShowRadius()
{
	LifeRegeneration* A = (LifeRegeneration*)GetA();
	if(OB&&A&&A->Radius>10)
	{
		AddGroundCircle(OB->RealX>>4,OB->RealY>>4,A->Radius,0xFFFFFF00);
		return true;
	}
	return false;
}
bool ActiveLifeRegeneration::OnClick()
{
	LifeRegeneration* A = (LifeRegeneration*)GetA();
	if(A&&A->CoolDownTime)
	{
		if(GetCoolDownProc()==0)
		{
			return Exec();
		}
	}
	return false;
}
bool ActiveLifeRegeneration::Exec()
{
	LifeRegeneration* A = (LifeRegeneration*)GetA();
	if(OB&&A)
	{
		LastUseTime=AnimTime;
		PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,A->Radius,&ActiveLifeRegeneration::AddLife,(void*)this);
		return true;
	}
	return false;
}
bool ActiveLifeRegeneration::AddLife(OneObject* OB, void* Param)
{
	if(OB&&!OB->Sdoxlo)
	{
		ActiveLifeRegeneration* AC = (ActiveLifeRegeneration*)Param;
		LifeRegeneration* A = (LifeRegeneration*)AC->GetA();
		if((A->Regeneration>0&&OB->NMask&AC->OB->NMask)||(A->Regeneration<0&&!(OB->NMask&AC->OB->NMask)))
		{
			OB->Life+=A->Regeneration;
			if(OB->Life>OB->MaxLife)
				OB->Life=OB->MaxLife;
			if(OB->Life<0)
				OB->Life=0;
			return true;
		}
	}
	return false;
}
//==================================================================================================================//
SetMineBonus::SetMineBonus()
{
	Bonus=0;
	Radius=0;
}
ActiveUnitAbility* SetMineBonus::GetActiveAbility()
{
	return (ActiveUnitAbility*) new ActiveSetMineBonus();
}
//==================================================================================================================//
ActiveSetMineBonus::ActiveSetMineBonus()
{
	LastUseTime=0;
}
bool ActiveSetMineBonus::Process()
{
	bool rez=false;
	if((!OB)&&UnitIndex!=0xFFFF)
	{
		OB=Group[UnitIndex];
	}
	if(OB&&!OB->Sdoxlo)
	{
		SetMineBonus* A = (SetMineBonus*)GetA();
		if(A&&A->Bonus)
		{
			if(AnimTime>(LastUseTime+25*256))
			{
				HintParam=A->Bonus+((*Le)-1)*A->BonusAddForLevel;
				PerformActionOverBuildingsInRadius(OB->RealX>>4,OB->RealY>>4,A->Radius,&ActiveSetMineBonus::AddMineBonus,(void*)this);
				LastUseTime=AnimTime;
			}
			rez=true;
		}
	}
	if(OB&&OB->Sdoxlo>0&&OB->Sdoxlo<1000&&!rez)
		rez=true;
	return rez;
}
int ActiveSetMineBonus::GetRadius()
{
	int rez=0;
	if(OB)
	{
		SetMineBonus* A=(SetMineBonus*)GetA();
		if(A)
			rez=A->Radius;
	}
	return rez;
}
int ActiveSetMineBonus::GetHintParamOnLevel(int Lev)
{
	if(Lev<6&&Lev>0)
	{
		SetMineBonus* A=(SetMineBonus*)GetA();
		if(A)
		{
			return A->Bonus+(Lev-1)*A->BonusAddForLevel;
		}
	}
	return 0;
}
bool ActiveSetMineBonus::AddMineBonus(OneObject* OB, void* Param)
{
	if(OB&&OB->Usage==MineID&&!OB->Sdoxlo)
	{
		ActiveSetMineBonus* AA=(ActiveSetMineBonus*)Param;
		SetMineBonus* A=(SetMineBonus*)AA->GetA();
		if((A->Bonus>0&&OB->NMask&AA->OB->NMask)||(A->Bonus<0&&!(OB->NMask&AA->OB->NMask)))
		{
			bool have=false;
			if(OB->ActiveAbility)
			{
				int n= OB->ActiveAbility->ActiveAbilities.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(OB->ActiveAbility->ActiveAbilities[i]->Type==13)
					{
						ActiveMineBonus* AM = (ActiveMineBonus*)OB->ActiveAbility->ActiveAbilities[i];
						if(AM->HeroIndex==AA->OB->Index)
						{
							have=true;
							break;
						}
					}
				}
			}
			if(!have)
			{
				/*
				bool atype=false;
				int n=A->MineType.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(OB->Usage==A->MineType[i]->UnitType)
					{
						atype=true;
						break;
					}
				}
				*/
				//if(atype)
				{
					ActiveMineBonus* AMB = new ActiveMineBonus();
					AMB->Radius=A->Radius;
					AMB->Bonus=A->Bonus+(*(AA->Le)-1)*A->BonusAddForLevel;
					AMB->HeroIndex=AA->OB->Index;
					AMB->OB=OB;
					AMB->UnitIndex=OB->Index;
					//CopyToActive(AMB);
					AddActiveUnitAbility(OB->Index,AMB);
				}
			}
		}
	}
	return false;
}
//==================================================================================================================//
ActiveMineBonus::ActiveMineBonus()
{
	Type=13;
	Radius=0;
	Bonus=0;
	HeroIndex=-1;
}
bool ActiveMineBonus::Process()
{
	bool rez=false;
	if((!OB)&&UnitIndex!=0xFFFF)
	{
		OB=Group[UnitIndex];
	}
	if(OB&&!OB->Sdoxlo&&HeroIndex!=-1)
	{
		OneObject* Her = Group[HeroIndex];
		if(Her&&!Her->Sdoxlo)
		{
			int d = Norma((OB->RealX>>4)-(Her->RealX>>4),(OB->RealY>>4)-(Her->RealY>>4));
			if(d<Radius)
			{
				rez=true;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
IncreaseMaxLife::IncreaseMaxLife()
{
	Points=0;
	Radius=0;
}
ActiveUnitAbility* IncreaseMaxLife::GetActiveAbility()
{
	return (ActiveUnitAbility*) new ActiveIncreaseMaxLife();
}
//==================================================================================================================//
ActiveAdditionalLife::ActiveAdditionalLife()
{
	Type=14;
	HeroIndex=-1;
	LifeAdded=0;
}
bool ActiveAdditionalLife::Process(OneObject* OB)
{
	bool rez=false;
	if(HeroIndex!=-1&&!OB->Sdoxlo)
	{
		IncreaseMaxLife* A = (IncreaseMaxLife*)GetA();
		if(A)
		{
			OneObject* H = Group[HeroIndex];
			if(H&&!H->Sdoxlo)
			{
				int R=Norma((OB->RealX>>4)-(H->RealX>>4),(OB->RealY>>4)-(H->RealY>>4));
				if(R<A->Radius)
				{
					rez=true;
				}
			}
			if(!rez)
			{
				int pr=(OB->Life*10000)/OB->MaxLife;
				OB->MaxLife-=LifeAdded;//A->Points;
				OB->Life=(OB->MaxLife*pr)/10000;
			}
		}
	}
	return rez;
}
bool ActiveAdditionalLife::CanYouAddToObject(OneObject* B,void* Param)
{
	bool rez=false;
	if(!B->Sdoxlo)
	{
		ActiveIncreaseMaxLife* AA = (ActiveIncreaseMaxLife*) Param;
		IncreaseMaxLife* A = (IncreaseMaxLife*) AA->GetA();
		if((A->Points>0&&AA->OB->NMask&B->NMask)||(A->Points<0&&!(AA->OB->NMask&B->NMask)))
		{
			bool have=false;
			if(B->ActiveAbility)
			{
				int n=B->ActiveAbility->ActiveAbilities.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(B->ActiveAbility->ActiveAbilities[i]->Type==14)
					{
						ActiveAdditionalLife* AD = (ActiveAdditionalLife*)B->ActiveAbility->ActiveAbilities[i];
						if(AD->HeroIndex==AA->OB->Index)
						{
							have=true;
							break;
						}
					}
				}
			}
			if(!have)
			{
				int n = A->UnitType.GetAmount();
				if(n)
				{
					bool tt=false;
					for(int i=0;i<n;i++)
					{
						if(A->UnitType[i]->UnitType==B->NIndex)
						{
							tt=true;
							break;
						}
					}
					if(tt)
						rez=true;
				}
				else
				{
					rez=true;
				}
			}
		}
	}
	return rez;
}
bool ActiveAdditionalLife::FillParam(OneObject* B, void* Param)
{
	ActiveIncreaseMaxLife* AA = (ActiveIncreaseMaxLife*) Param;
	IncreaseMaxLife* A = (IncreaseMaxLife*) AA->GetA();
	if(AA)
	{
		HeroIndex=AA->OB->Index;
		SetA(A);
		LifeAdded=A->Points+A->AddForLevel*(*AA->Le-1);
		int pr=(B->Life*10000)/B->MaxLife;
		B->MaxLife+=LifeAdded;
		B->Life=(B->MaxLife*pr)/10000;
		return true;
	}
	return false;
}
//==================================================================================================================//
ActiveIncreaseMaxLife::ActiveIncreaseMaxLife()
{}
bool ActiveIncreaseMaxLife::Process(OneObject* OB)
{
	if(!OB->Sdoxlo)
	{
		AddActionAbilityOverUnitsInRadius();
		return true;
	}
	if(OB->Sdoxlo>0&&OB->Sdoxlo<1000)
		return true;
	return false;
}
int ActiveIncreaseMaxLife::GetRadius()
{
	IncreaseMaxLife* A = (IncreaseMaxLife*)GetA();
	if(A)
		return A->Radius;
	return 0;
}
ActiveUnitAbility* ActiveIncreaseMaxLife::GetActionAbilityExample()
{
	return &Example;
}
int ActiveIncreaseMaxLife::GetUsePause()
{
	return 20;
}
int ActiveIncreaseMaxLife::GetHintParamOnLevel(int Lev)
{
	if(Lev<6&&Lev>0)
	{
		IncreaseMaxLife* A = (IncreaseMaxLife*)GetA();
		if(A)
		{
			return A->Points+A->AddForLevel*(Lev-1);
		}
	}
	return 0;
}
//==================================================================================================================//
AddUnitBonus::AddUnitBonus()
{
	AddDamage=0;
	AddShield=0;
	EnemyTarget=false;
	CoolDownTime=0;
	EffectTime=0;
	UsePause=0;
	AddForLevel=0;
}
ActiveUnitAbility* AddUnitBonus::GetActiveAbility()
{
	return new ActiveAddUnitBonus();
}
//==================================================================================================================//
ActiveAddUnitBonus::ActiveAddUnitBonus()
{}
bool ActiveAddUnitBonus::Process(OneObject* OB)
{
	bool rez=false;
	AddUnitBonus* A = (AddUnitBonus*)GetA();
	if(OB&&(!OB->Sdoxlo)&&A)
	{
		if(!A->CoolDownTime)
		{
			if(AnimTime>(LastUseTime+A->UsePause*256*25/10))
			{
				Exec();
			}
		}
		if(A->AddDamage)
			HintParam=A->AddDamage+(*Le-1)*A->AddForLevel;
		else
		if(A->AddShield)
			HintParam=A->AddShield+(*Le-1)*A->AddForLevel;
			
				
		rez=true;
	}
	if(OB&&OB->Sdoxlo>0&&OB->Sdoxlo<1000&&!rez)
		rez=true;
	return rez;
}
bool ActiveAddUnitBonus::CanApply()
{
	AddUnitBonus* A = (AddUnitBonus*)GetA();
	if(A&&A->CoolDownTime)
	{
		return true;
	}
	return false;
}
int ActiveAddUnitBonus::GetCoolDownProc()
{
	int rez=0;
	AddUnitBonus* A = (AddUnitBonus*)GetA();
	if(A&&A->CoolDownTime)
	{
		int cool=A->CoolDownTime*25*256;
		if(cool)
			rez=(LastUseTime+cool-AnimTime)*100/cool;
		if(rez<0)
			rez=0;
	}
	return rez;
}
int ActiveAddUnitBonus::GetRadius()
{
	AddUnitBonus* A = (AddUnitBonus*)GetA();
	if(A)
	{
		return A->Radius;
	}
	return 0;
}
bool ActiveAddUnitBonus::OnClick()
{
	if(*Vi)
	{
		return LeveledActiveUnitAbility::OnClick();
	}
	AddUnitBonus* A = (AddUnitBonus*)GetA();
	if(A&&A->CoolDownTime)
	{
		if(GetCoolDownProc()==0)
		{
			return Exec();
		}
	}
	return false;
}
int ActiveAddUnitBonus::GetHintParamOnLevel(int Lev)
{
	if(Lev<6&&Lev>0)
	{
		AddUnitBonus* A = (AddUnitBonus*)GetA();
		if(A)
		{
			if(A->AddDamage)
				return A->AddDamage+(Lev-1)*A->AddForLevel;
			else
			if(A->AddShield)
				return A->AddShield+(Lev-1)*A->AddForLevel;
		}
	}
	return 0;
}
bool ActiveAddUnitBonus::Exec()
{
	AddUnitBonus* A = (AddUnitBonus*)GetA();
	if(OB&&A)
	{
		LastUseTime=AnimTime;
		PerformActionOverUnitsInRadius(OB->RealX>>4,OB->RealY>>4,A->Radius,&ActiveAddUnitBonus::SetBonus,(void*)this);
		return true;
	}
	return false;
}
bool ActiveAddUnitBonus::SetBonus(OneObject* OB, void* Param)
{
	if(OB&&!OB->Sdoxlo)
	{
		ActiveAddUnitBonus* AA = (ActiveAddUnitBonus*)Param;
		AddUnitBonus* A = (AddUnitBonus*)AA->GetA();
		if(((AA->OB->NMask&OB->NMask)&&!A->EnemyTarget)||((!(AA->OB->NMask&OB->NMask))&&A->EnemyTarget))
		{
			bool have=false;
			if(OB->ActiveAbility)
			{
				int n= OB->ActiveAbility->ActiveAbilities.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(OB->ActiveAbility->ActiveAbilities[i]->Type==16)
					{
						PassiveAddUnitBonus* ABB = (PassiveAddUnitBonus*)(OB->ActiveAbility->ActiveAbilities[i]);
						if(ABB->HeroIndex==AA->OB->Index&&ABB->GetA()==AA->GetA())
						{
							have=true;
							break;
						}
					}
				}
			}
			if(!have)
			{
				int nt = A->UnitType.GetAmount();
				bool intype=false;
				if(nt)
				{
					for(int i=0;i<nt;i++)
					{
						if(A->UnitType[i]->UnitType==OB->NIndex)
						{
							intype=true;
							break;
						}
					}
				}
				else
				{
					intype=true;
				}
				if(intype)
				{
					PassiveAddUnitBonus* ABB = new PassiveAddUnitBonus();
					ABB->HeroIndex=AA->OB->Index;
					ABB->SetA(A);
					AddActiveUnitAbility(OB->Index,ABB);  
					if(A->AddDamage)
					{
						AA->HintParam=A->AddDamage+(*(AA->Le)-1)*A->AddForLevel;
						ABB->AddDamage=AA->HintParam;
						OB->AddDamage+=ABB->AddDamage;
					}
					else
					if(A->AddShield)
					{
						AA->HintParam=A->AddShield+(*(AA->Le)-1)*A->AddForLevel;
						ABB->AddShield=AA->HintParam;
						OB->AddShield+=ABB->AddShield;
					}
				}
			}
		}
	}
	return false;
}
//==================================================================================================================//
PassiveAddUnitBonus::PassiveAddUnitBonus()
{
	Radius=0;
	AddDamage=0;
	AddShield=0;
	HeroIndex=0xFFFF;
	SetTime=AnimTime;
}
bool PassiveAddUnitBonus::Process(OneObject* OB)
{
	bool rez=false;
	if(OB&&HeroIndex!=0xFFFF)
	{
		AddUnitBonus* A = (AddUnitBonus*)GetA();
		if(A)
		{
			OneObject* Her = Group[HeroIndex];
			if(Her&&!Her->Sdoxlo)
			{
				if(A->EffectTime==0)
				{
					int R = Norma((Her->RealX>>4)-OB->RealX/16,(Her->RealY>>4)-OB->RealY/16);
					if(R<A->Radius)
					{
						rez=true;
					}
				}
				else
				if(AnimTime<(SetTime+A->EffectTime*25*256))
				{
					rez=true;
				}
			}
			if(!rez)
			{
				OB->AddDamage-=AddDamage;
				OB->AddShield-=AddShield;
				if(OB->AddDamage<0)
					OB->AddDamage=0;
				if(OB->AddShield<0)
					OB->AddShield=0;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
bool PushAllOnTheWay::OnUnitBirth(OneObject* OBJ){
	PushUnitsAbility* PUAB=new PushUnitsAbility;
	Copy(&PUAB->Params);//.Copy(=*this;	
	PUAB->OwnerID=OBJ->Index;
	PUAB->OwnerSN=OBJ->Serial;
	PUAB->SetA(this);
	CopyToActive(PUAB);
	AddActiveUnitAbility(OBJ->Index,PUAB);  
	return true;
}
struct PU_TempStruct{
	int x,y;
	int ux,uy;
	char Dir;
	OneObject* Own;
	PushAllOnTheWay* P;
	PushUnitsAbility* PA;
};
bool ABL_PushUnits(OneObject* OB,void* p){
	if(OB->NotSelectable)return true;	
	PU_TempStruct* PT=(PU_TempStruct*)p;
	if(OB->Index==PT->Own->Index)return true;
    if(OB->LockType==1)return true;	
	void PushUnitBackLink(OneObject* OBJ);
	if(OB->LocalOrder&&OB->LocalOrder->DoLink==&PushUnitBackLink)return false;
	char d0=GetDir((OB->RealX>>4)-PT->ux,(OB->RealY>>4)-PT->uy);
	int A=abs(int(d0-PT->Dir));
	int R=Norma(OB->RealX-(PT->ux<<4),OB->RealY-(PT->uy<<4))>>4;
	int R0=PT->P->PushForwardRadius-A*(PT->P->PushForwardRadius-PT->P->PushLeftRightRadius)/64;
	if(A<PT->P->PushAngle&&R<R0){		
		if(!OB->newMons->RotationAtPlaceSpeed)OB->RealDir=d0+128;
		void PushUnitBack(OneObject* OB,byte OrdType,int Force);
		if((PT->P->PushEnemyUnits&&!(OB->NMask&PT->Own->NMask))||
			(PT->P->PushFriendlyUnits&&OB->NMask&PT->Own->NMask)){
				if(!OB->newMons->RotationAtPlaceSpeed){
					OB->RealDir=d0+128;
					PushUnitBack(OB,1,PT->P->PushForce);
				}
			}
		if((PT->P->DamageEnemyUnits&&!(OB->NMask&PT->Own->NMask))||
			(PT->P->DamageFriendlyUnits&&OB->NMask&PT->Own->NMask)){
				int D=PT->P->MinDamage + ( ( ( PT->P->MaxDamage-PT->P->MinDamage )*rando()%100 )/100 );
                OB->MakeDamage( D,D,PT->Own,0 );
			}
	}
}
bool ABL_AskGoAway(OneObject* OB,void* p){
	if(OB->NotSelectable)return true;	
	PU_TempStruct* PT=(PU_TempStruct*)p;
	if(OB->Index==PT->Own->Index)return true;
	void PushUnitBackLink(OneObject* OBJ);
	if(OB->LocalOrder&&OB->LocalOrder->DoLink==&PushUnitBackLink)return false;
	char d0=GetDir(OB->RealX-PT->Own->RealX,OB->RealY-PT->Own->RealY);
	char dd=d0-PT->Own->RealDir;
	if(OB->NMask&PT->Own->NMask&&OB->LockType!=1){
		int R=Norma((OB->RealX>>4)-PT->x,(OB->RealY>>4)-PT->y);
		if(R<PT->P->PushForwardRadius){
			void RotUnitLink(OneObject* OB);
			void NewMonsterSendToLink(OneObject* OB);			
			if(OB->LocalOrder&&
				(OB->LocalOrder->DoLink==&NewMonsterSendToLink||
				OB->LocalOrder->DoLink==&RotUnitLink))return false;
			int pp=0;
			while(pp<100&&!OB->FrameFinished){
				OB->SetNextFrame();
				pp++;
			}
			int dy=PT->x-(PT->Own->RealX>>4);
			int dx=-(PT->y-(PT->Own->RealY>>4));
			int N=Norma(dx,dy);
			if(N){
				dx=dx*128*16/N;
				dy=dy*128*16/N;
			}            
			if(dd<0){
				OB->NewMonsterSendTo(OB->RealX-dx,OB->RealY-dy,128+16,1);            
			}else{
				OB->NewMonsterSendTo(OB->RealX+dx,OB->RealY+dy,128+16,1);
			}
		}
	}
	else
	{
		if(PT->P->CanScare)
		{
			if(OB->FrameFinished)
			{
				NewAnimation* NA =OB->newMons->GetAnimation(anm_Scare);
				if(NA&&NA->Enabled)
				{
					
					char Dirr=(char)GetDir(PT->Own->RealX-OB->RealX,PT->Own->RealY-OB->RealY);
					char dd = char(OB->RealDir)-Dirr;
					if(OB->BrigadeID==0xFFFF)
					{
						if(abs(dd)>16)
						{
							RotateMon(OB,-dd);
						}
					}

					if(abs(dd)<17)
					{
						OB->NewAnm=NA;
						PT->PA->Boldness++;
						OB->SetZeroFrame();
					}
				}
			}
		}
	}
}
bool enmUnits(OneObject* OB,void* param){
	byte NMask=byte(param);
	return !(OB->NMask&NMask||OB->Sdoxlo);
}
bool TestEnemyNearPoint(int x,int y,byte NMask){
	return PerformActionOverUnitsInRadius(x,y,180,enmUnits,(void*) NMask)!=0;
}
PushUnitsAbility::PushUnitsAbility()
{
	Boldness=0;
	LastTimeCheck=0;
}
bool PushUnitsAbility::Process(){
	OneObject* OB=Group[OwnerID];
	if(OB&&OB->Serial==OwnerSN){
		if(OB->DestX>0&&!OB->Sdoxlo){
			static PU_TempStruct PU;
			int x0=(OB->RealX>>4)+(int(TCos[OB->RealDir])*Params.PushCenterShift)/256;
			int y0=(OB->RealY>>4)+(int(TSin[OB->RealDir])*Params.PushCenterShift)/256;
			PU.P=(PushAllOnTheWay*)GetA();
			PU.ux=x0;
			PU.uy=y0;
			PU.x=x0;
			PU.y=y0;
			PU.Dir=OB->RealDir;
			PU.Own=OB;
			//PU.P=Params;
			PU.PA=this;
			PerformActionOverUnitsInRadius(x0,y0,Params.PushForwardRadius+64,&ABL_PushUnits,&PU);
			PU.x=(x0)+((int(TCos[OB->RealDir])*Params.AskGoAwayDistance)>>8);
			PU.y=(y0)+((int(TSin[OB->RealDir])*Params.AskGoAwayDistance)>>8);
			PerformActionOverUnitsInRadius(PU.x,PU.y,Params.PushForwardRadius+64,&ABL_AskGoAway,&PU);
			if(Params.UseAttack1NearEnemy){
				OB->GroundState=TestEnemyNearPoint(x0,y0,OB->NMask);
				if(OB->GroundState){
					OB->NoSearchVictim=1;
					//OB->ActivityState=0;
				}
			}else{
				OB->NoSearchVictim=0;
			}

		}
		if(!OB->Sdoxlo)
		{
			if(Params.CanScare)
			{
				if(Boldness>=Params.MaxBoldness)
				{
					PanicUnit(OB);
				}
				if(Boldness>0)
				{
					int re = ((AnimTime-LastTimeCheck)*Params.BoldnessRegeneration)/(25*256*10);
					if(re)
					{
						Boldness-=re;
						LastTimeCheck+=(re*25*256*10)/Params.BoldnessRegeneration;
					}
				}
				else
				{
					LastTimeCheck=AnimTime;
					Boldness=0;
				}
			}
		}
		return true;
	}
	return false;
}
//==================================================================================================================//
bool GetMineProduceBonus(OneObject* Mine,int& Proc)
{
	bool rez=false;
	if(Mine&&Mine->ActiveAbility)
	{
		int n= Mine->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<n;i++)
		{
			if(Mine->ActiveAbility->ActiveAbilities[i]->Type==13)
			{
				ActiveMineBonus* AM = (ActiveMineBonus*)Mine->ActiveAbility->ActiveAbilities[i];
				Proc+=AM->Bonus;
				rez=true;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
BuildingShield::BuildingShield()
{
	Radius=0;
	AddShield=0;
	EnemyTarget=false;
}
ActiveUnitAbility* BuildingShield::GetActiveAbility()
{
	return new ActiveAddBuildingShield();
}
//==================================================================================================================//
ActiveAddBuildingShield::ActiveAddBuildingShield()
{
	LastUseTime=0;
}
bool ActiveAddBuildingShield::Process(OneObject* OB)
{
	if(OB&&!OB->Sdoxlo)
	{
		if((LastUseTime+256*25*3)<AnimTime)
		{
			BuildingShield* Gm = (BuildingShield*)GetA();
			if(Gm)
			{
				int Param[6];
				Param[0]=OB->NMask;
				Param[1]=Gm->EnemyTarget;
				Param[3]=Gm->AddShield+(*Le-1)*Gm->ShieldAddForLevel;
				HintParam=Param[3];
				Param[4]=OB->Index;
				Param[5]=(int)Gm;
				PerformActionOverBuildingsInRadius(OB->RealX>>4,OB->RealY>>4,Gm->Radius,&ActiveAddBuildingShield::AddBuildingShield,(void*)Param);
				LastUseTime=AnimTime;
			}
		}
		return true;
	}
	if(OB&&OB->Sdoxlo>0&&OB->Sdoxlo<1000)
		return true;
	return false;
}
bool ActiveAddBuildingShield::AddBuildingShield(OneObject* Ob, void* param)
{
	if(Ob&&Ob->NewBuilding&&!Ob->Sdoxlo)
	{
		int* P=(int*)param;
		byte mas=(byte)P[0];
		if((mas&Ob->NMask&&!P[1])||(!(mas&Ob->NMask)&&P[1]))
		{
			if(Ob->ActiveAbility)
			{
				int n = Ob->ActiveAbility->ActiveAbilities.GetAmount();
				for(int i=0;i<n;i++)
				{
					if(Ob->ActiveAbility->ActiveAbilities[i]->Type==15)
					{
						ActiveBuildingShield* abs = (ActiveBuildingShield*)(Ob->ActiveAbility->ActiveAbilities[i]);
						if(abs->HeroIndex==P[4])
						{
							return false;
						}
					}
				}
			}
			ActiveBuildingShield* ABS = new ActiveBuildingShield();
			ABS->AddToObject(Ob);
			ABS->HeroIndex=P[4];
			ABS->SetA((UnitAbility*)P[5]);
			ABS->AddShield=P[3];
			Ob->AddShield+=P[3];
			return true;
		}
	}
	return false;
}
int ActiveAddBuildingShield::GetRadius()
{
	BuildingShield* UA = (BuildingShield*)(GetA());
	if(UA)
	{
		return UA->Radius;
	}
	return 0;
}
int ActiveAddBuildingShield::GetHintParamOnLevel(int Lev)
{
	if(Lev<6&&Lev>0)
	{
		BuildingShield* Gm = (BuildingShield*)GetA();
		if(Gm)
		{
			return Gm->AddShield+(Lev-1)*Gm->ShieldAddForLevel;
		}
	}
	return 0;
}
//==================================================================================================================//
ActiveBuildingShield::ActiveBuildingShield()
{
	HeroIndex=-1;
	LastUseTime=0;
	Type=15;
}
bool ActiveBuildingShield::Process(OneObject* OB)
{
	if(OB&&!OB->Sdoxlo&&HeroIndex!=-1)
	{
		if((LastUseTime+25*256*3)>AnimTime) 
			return true;
		OneObject* Her = Group[HeroIndex];
		BuildingShield* UA = (BuildingShield*)(GetA());
		if(Her&&UA&&!Her->Sdoxlo)
		{
			int d = Norma((OB->RealX>>4)-(Her->RealX>>4),(OB->RealY>>4)-(Her->RealY>>4));
			if(d<UA->Radius)
			{
				LastUseTime=AnimTime;
				return true;
			}
			OB->AddShield-=AddShield;
		}
	}
	return false;
}
//==================================================================================================================//
FollowBrigade::FollowBrigade()
{
	MoveOutEnemy=false;
}
ActiveUnitAbility* FollowBrigade::GetActiveAbility()
{
	return new ActiveFollowBrigade();
}
//==================================================================================================================//
ActiveFollowBrigade::ActiveFollowBrigade()
{
	lastUseTime=0;
	lastMoveBack=0;
}
bool ActiveFollowBrigade::Process(OneObject* OB)
{
	if(!OB->Sdoxlo)
	{
		if((lastUseTime+3*25*256)<AnimTime&&!GSets.CGame.isHumanPlayer(OB->NNUM))
		{
			int n = WiselyFormations.AllForm.GetAmount();
			int x=OB->RealX/16;
			int y=OB->RealY/16;
			int mind=999999;
			Atom* MA=NULL;
			for(int i=0;i<n;i++)
			{
				Atom* A=WiselyFormations.AllForm[i];
				if(A&&A->BSelf&&A->NI==OB->NNUM&&A->IsAlive())
				{
					int dist=Norma(A->x-x,A->y-y);
					dist+=A->BSelf->Morale/10000*20;
					if(mind>dist)
					{
						mind=dist;
						MA=A;
					}
				}
			}
			if(MA&&mind>300)
			{
				int xx=MA->x+((150*TCos[MA->BSelf->Direction+128])>>8)-50+rando()%100;
				int yy=MA->y+((150*TSin[MA->BSelf->Direction+128])>>8)-50+rando()%100;
				OB->NewMonsterSmartSendTo(xx,yy,0,0,128+16,0);
			}
			lastUseTime=AnimTime;
		}
		if((lastMoveBack+2*25*256)<AnimTime&&!GSets.CGame.isHumanPlayer(OB->NNUM))
		{
			FollowBrigade *A=(FollowBrigade*)GetA();
			if(A&&A->MoveOutEnemy)
			{
				int par[6];
				par[0]=0;
				par[1]=0;
				par[2]=0;
				par[3]=(int)OB->NMask;
				par[4]=OB->RealX/16;
				par[5]=OB->RealY/16;
				PerformActionOverUnitsInRadius(OB->RealX/16,OB->RealY/16,400,&ActiveFollowBrigade::CheckEnemy,(void*) par);
				if(par[2])
				{
					par[0]/=par[2];
					par[1]/=par[2];
					int ds=Norma(par[0],par[1]);
					if(ds)
					{
						par[0]=par[0]*150/ds;
						par[1]=par[1]*150/ds;
						OB->NewMonsterSmartSendTo(OB->RealX/16+par[0],OB->RealY/16+par[1],0,0,128+16,0);
					}
				}
			}
			lastMoveBack=AnimTime;
		}
		return true;
	}
	return false;
}
bool ActiveFollowBrigade::CheckEnemy(OneObject* OB,void* param)
{
	bool rez=false;
	if(OB&&!OB->Sdoxlo)
	{
		int *par=(int*)param;
		byte mm=(byte)par[3];
		if(!(OB->NMask&mm))
		{
			int ds=Norma(OB->RealX/16-par[4],OB->RealY/16-par[5]);
			if(ds<401)
			{
				par[0]+=((par[4]-OB->RealX/16)*100)/(401-ds);
				par[1]+=((par[5]-OB->RealY/16)*100)/(401-ds);
				par[2]++;
				rez=true;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
Behaviour::Behaviour()
{
	MoveDist=0;
	MoveTime=0;
	ChangeDir=0;
	MaxRestTime=0;
	Radius=0;
}
ActiveUnitAbility* Behaviour::GetActiveAbility()
{
	return new ActiveBehaviour();
}
//==================================================================================================================//
ActiveBehaviour::ActiveBehaviour()
{
	EndMoveTime=0;
	RestTime=0;
	BaseX=-1;
	BaseY=-1;
}
bool ActiveBehaviour::Process(OneObject* OB)
{
	if(!OB->Sdoxlo)
	{
		Behaviour* B = (Behaviour*)GetA();
		if(B)
		{
			if(!GetPeaceMode())
			{
				if(B->Radius>0&&BaseX==-1)
				{
					BaseX=OB->RealX/16;
					BaseY=OB->RealY/16;
				}
				if(EndMoveTime==0&&RestTime==0&&(B->MaxRestTime||B->MoveTime))
				{
					if(B->MaxRestTime)RestTime=AnimTime+25*256*(rando()%B->MaxRestTime);
					if(B->MoveTime)EndMoveTime=AnimTime+25*256*(rando()%B->MoveTime);
					if(RestTime<EndMoveTime)
						RestTime=0;
					else
						EndMoveTime=0;
				}
				if(EndMoveTime)
				{
					if(OB->DestX<0)
					{
						if(EndMoveTime>AnimTime||B->MaxRestTime==0)
						{
							Move(OB,B);
						}
						else
						{
							EndMoveTime=0;
							RestTime=AnimTime+25*256*(rando()%B->MaxRestTime);
						}
					}
				}
				if(RestTime&&B->MaxRestTime)
				{
					if(RestTime<AnimTime)
					{
						RestTime=0;
						EndMoveTime=AnimTime+25*256*(rando()%B->MoveTime);
						Move(OB,B);
					}
				}
			}
			return true;
		}
	}
	return false;
}
void ActiveBehaviour::Move(OneObject* OB, Behaviour* B)
{
	byte RDir=rando()%B->ChangeDir;
	int RDist=rando()%B->MoveDist;
	int cx=OB->RealX/16;
	int cy=OB->RealY/16;
	byte dr=OB->RealDir-B->ChangeDir/2+RDir;
	int dx=cx+RDist*TSin[dr]/256;
	int dy=cy+RDist*TCos[dr]/256;
	if(B->Radius>0)
	{
		if(Norma(BaseX-dx,BaseY-dy)>B->Radius)
			return;
	}
	int P[8];
	P[0]=cx;
	P[1]=cy;
	P[2]=dx;
	P[3]=dy;
	MotionField* MF=&MFIELDS[OB->LockType];

	if(!MF->CheckPt(dx>>4,dy>>4)&&GetSpritesInRadius((cx+dx)/2,(cy+dy)/2, B->MoveDist,ActiveBehaviour::CheckSprite, (void*)P)==0)
	{
		if(OB->LocalOrder==NULL)
			OB->NewMonsterSendTo(dx<<4,dy<<4,128+16,0);
		//OB->CreatePath(dx/16,dy/16);
	}
	else
	{
		RotateMon(OB,RDir-B->ChangeDir/2);
	}
}
bool ActiveBehaviour::CheckSprite(OneSprite* OS,void* Param)
{
	if(OS&&OS->OC->Aligning==2)//Vertical
	{
		int* P = (int*)Param;
		int dste=GetPointToLineDist(OS->x,OS->y,P[0],P[1],P[2],P[3]);
		if(dste<30)
		{
			return true;
		}
	}
	//int GetPointToLineDist(int x,int y,int x1,int y1,int x2,int y2);
	return false;
}
//==================================================================================================================//
bool aa_SlowCallback(OneObject* OB,void* param){
	DWORD M=(DWORD)param;
	return!(OB->NMask&M);
}
bool aa_BeSlowNearUnits::Process(OneObject* OB){
	int x0=OB->RealX/16+ABL.ShiftFromCenter*TCos[OB->RealDir]/256;
	int y0=OB->RealY/16+ABL.ShiftFromCenter*TSin[OB->RealDir]/256;
	DWORD M=ABL.BeSlowOnlyInEnemyGroup?OB->NMask:0;
    int N=PerformActionOverUnitsInRadius(x0,y0,ABL.Radius,aa_SlowCallback,(void*)M);
	if(N>ABL.MaxUnitsAmount){
		N=ABL.MaxUnitsAmount;
	}
	if(ABL.MaxUnitsAmount){
        float p=ABL.SlowDegree[N*(NSlowDegrees-1)/ABL.MaxUnitsAmount];
        OB->GroupSpeed=int(p*OB->newMons->MotionDist);
		OB->UnitSpeed=64;
	}
	return true;
}
ActiveUnitAbility* BeSlowNearUnits::GetActiveAbility(){
	return new aa_BeSlowNearUnits;
}
void BeSlowNearUnits::CopyToActive(ActiveUnitAbility* ab){
	aa_BeSlowNearUnits* AB=(aa_BeSlowNearUnits*)ab;
	Copy(&AB->ABL);
}
//=============================================================================================================================
ActiveUnitAbility* MakeDamageOnDeath::GetActiveAbility(){
	return new aa_MakeDamageOnDeath;
}
void MakeDamageOnDeath::CopyToActive(ActiveUnitAbility* ab){
	aa_MakeDamageOnDeath* AB=(aa_MakeDamageOnDeath*)ab;
	Copy(&AB->MD);
}
struct aa_md_param{
	DWORD Mask;
	int DamValue;
	OneObject* Source;
};
bool aa_MakeDamCallback(OneObject* OB,void* param){
	aa_md_param* M=(aa_md_param*)param;
	if(!(OB->NMask&M->Mask)){
		void PushUnitBack(OneObject* OB,byte OrdType,int Force);
		PushUnitBack(OB,0,100);
		OB->MakeDamage(M->DamValue,M->DamValue,M->Source,0);
	}
	return true;
}
bool aa_MakeDamageOnDeath::Process(OneObject* OB){
	if(OB->Sdoxlo&&!Done){
		aa_md_param M;
		M.DamValue=MD.DamageValue;
		M.Source=OB;
		M.Mask=(MD.DamageFriens?0:OB->NMask)|(MD.DamageEnemy?0:~OB->NMask);
		PerformActionOverUnitsInRadius(OB->RealX/16+(MD.xc*TCos[OB->RealDir]/256)-(MD.yc*TSin[OB->RealDir]/256)
			,OB->RealY/16+(MD.yc*TCos[OB->RealDir]/256)+(MD.xc*TSin[OB->RealDir]/256),MD.R,aa_MakeDamCallback,&M);
		Done=true;
	}
	return true;
}
//=============================================================================================================================
//
//NewItem* DetectItem(word NIndex);
HeroVariableStorage* DetectHero(OneObject* OB);
//
int ActiveAbilityListArray::Add(ActiveUnitAbility* V){
	// Set unical serial number
	if(V){
		V->Serial=CurSerial;
		CurSerial++;
	}
	int p=ClassArray<ActiveUnitAbility>::Add(V);
	// Set modificators
	if(V&&(V->TypeAbil==0||V->TypeAbil==4&&DetectHero(V->OB))){
		UnitAbility* UA=V->GetA();
		if(UA){        
			if(UA->eMoveSpeed.Get()) V->InfluenceMask|=ABL_MotionSpeed;
			if(UA->eProtection.GetAmount())	V->InfluenceMask|=ABL_Protection;
			for(int i=0;i<4;i++){
				if(UA->eDamage[i].Get()){
					V->InfluenceMask|=ABL_Damage;
					break;
				}
			}
			if(UA->eShield.Get()) V->InfluenceMask|=ABL_Shield;
			if(UA->eAttSpeed.GetAmount()) V->InfluenceMask|=ABL_AttackRate;			
			//ABL_MagicImmunity
			if(UA->eLifeRegen.Get()) V->InfluenceMask|=ABL_LifeRegeneration;
			//
			NewAnimation* A=UA->eAn.Get();
			if(A){
				EfAnimationMask=1;
				V->EfAnimationMask=1;
			}
			//
			if(UA->eWeapon.Get()) V->InfluenceMask|=ABL_Weapon;			
		}
	}
	// Recalculate InfluenceMask
	InfluenceMask=0;
	for(int i=0;i<GetAmount();i++){
		ActiveUnitAbility* AA=(*this)[i];
		if(AA){
			InfluenceMask|=AA->InfluenceMask;
		}
	}
	return p;
}
inline void ActiveAbilityListArray::Del(int pos,int N){
	ClassArray<ActiveUnitAbility>::Del(pos,N);
	// Recalculate InfluenceMask
	InfluenceMask=0;
	EfAnimationMask=0;
	for(int i=0;i<GetAmount();i++){
		ActiveUnitAbility* AA=(*this)[i];
		if(AA){
			InfluenceMask|=AA->InfluenceMask;
			//
			UnitAbility* UA=AA->GetA();
			if(UA){
				NewAnimation* NA=UA->eAn.Get();
				if(NA){
					EfAnimationMask=1;
				}
			}
		}
	}
	//
}
//
void ActiveUnitAbility::modifyMotionSpeed(int BasicSpeed,int& CurrentSpeed){
	UnitAbility* UA=GetA();
	if(UA){
		MagicSpell* MS=UA->eMoveSpeed.Get();
		if(MS->sign==0){
			// plus
			CurrentSpeed+=MS->num;
		}else
		if(MS->sign==1){
			// minus
			CurrentSpeed-=MS->num;
		}else
		if(MS->sign==2){
			// sub
			CurrentSpeed*=MS->num;
		}else
		if(MS->sign==3){
			// dev
			if(MS->num!=0) CurrentSpeed/=MS->num;
		}
		if(CurrentSpeed<0)CurrentSpeed=0;
	}
};
void ActiveUnitAbility::modifyProtection(int AttType,int BasicProtection,int& CurrentProtection,OneObject* Damager){
	UnitAbility* UA=GetA();
	if(UA&&AttType<UA->eProtection.GetAmount()){
		MagicSpell* MS=UA->eProtection[AttType];
		if(MS->sign==0){
			// plus
			CurrentProtection+=MS->num;
		}else
		if(MS->sign==1){
			// minus
			CurrentProtection-=MS->num;
		}else
		if(MS->sign==2){
			// sub
			CurrentProtection*=MS->num;
		}else
		if(MS->sign==3){
			// dev
			if(MS->num!=0) CurrentProtection/=MS->num;
		}
		if(CurrentProtection<0)CurrentProtection=0;
	}
};
void ActiveUnitAbility::modifyShield            (int BasicShield,int& CurrentShield,OneObject* Damager){
	UnitAbility* UA=GetA();
	if(UA){
		MagicSpell* MS=UA->eShield.Get();
		if(MS->sign==0){
			// plus
			CurrentShield+=MS->num;
		}else
		if(MS->sign==1){
			// minus
			CurrentShield-=MS->num;
		}else
		if(MS->sign==2){
			// sub
			CurrentShield*=MS->num;
		}else
		if(MS->sign==3){
			// dev
			if(MS->num!=0) CurrentShield/=MS->num;
		}
		if(CurrentShield<0)CurrentShield=0;
	}
};
void ActiveUnitAbility::modifyDamage            (int AttType,int BasicDamage,int& CurrentDamage,OneObject* Victim){
	UnitAbility* UA=GetA();
	if(UA&&AttType<4){
		MagicSpell* MS=UA->eDamage[AttType].Get();
		if(MS->sign==0){
			// plus
			CurrentDamage+=MS->num;
		}else
		if(MS->sign==1){
			// minus
			CurrentDamage-=MS->num;
		}else
		if(MS->sign==2){
			// sub
			CurrentDamage*=MS->num;
		}else
		if(MS->sign==3){
			// dev
			if(MS->num!=0) CurrentDamage/=MS->num;
		}
		if(CurrentDamage<0)CurrentDamage=0;
	}
};
void ActiveUnitAbility::modifyAttackRate        (int AttackType,int BasicRate,int& ChangedRate,OneObject* Victim){
	UnitAbility* UA=GetA();
	if(UA&&AttackType<UA->eAttSpeed.GetAmount()){
		MagicSpell* MS=UA->eAttSpeed[AttackType];
		if(MS->sign==0){
			// plus
			ChangedRate+=MS->num;
		}else
		if(MS->sign==1){
			// minus
			ChangedRate-=MS->num;
		}else
		if(MS->sign==2){
			// sub
			ChangedRate-=MS->num;
		}else
		if(MS->sign==3){
			// dev
			if(MS->num!=0) ChangedRate-=MS->num;
		}
		if(ChangedRate<0)ChangedRate=0;
	}
};
void ActiveUnitAbility::modifyMagicImmunity     (bool Basic,bool& Current,OneObject* Caster){
	// пока не нужно, будем использовать массивы блокирующих и запрещающих
};
void ActiveUnitAbility::modifyLifeRegeneration  (int Basic,int& Current){
	UnitAbility* UA=GetA();
	if(UA){
		MagicSpell* MS=UA->eLifeRegen.Get();
		if(MS->sign==0){
			// plus
			Current+=MS->num;
		}else
		if(MS->sign==1){
			// minus
			Current-=MS->num;
		}else
		if(MS->sign==2){
			// sub
			Current-=MS->num;
		}else
		if(MS->sign==3){
			// dev
			if(MS->num!=0) Current-=MS->num;
		}
		if(Current<0)Current=0;
	}
};
Weapon* GetWeaponFromModificator(WeaponModificator* WM);
void ActiveUnitAbility::modifyWeapon(Weapon* Basic,Weapon** Current){
	UnitAbility* UA=GetA();
	if(UA){
		WeaponModificator* WM=UA->eWeapon.Get();
		if(WM){
            Weapon* W=GetWeaponFromModificator(WM);
			if(W){
				*Current=W;
			}
		}
	}
}
//
/*
class UnitAbilityEnumerator:public ProcEnumerator{
public:
	virtual DWORD GetValue(const char* ID){
		for(int i=0;i<Abilities.Abilities.GetAmount();i++){
			if(Abilities.Abilities[i]->Name.equal(ID))return i;
		}
		return -1;
	}	
	virtual char* GetValue(DWORD ID){
		if(ID==0xFFFFFFFF)return (char*)"---none---";		
		else return (char*)Abilities.Abilities[ID]->Name();		
	}	
	virtual int   GetAmount(){
		return Abilities.Abilities.GetAmount()+1;
	}
	virtual char* GetIndexedString(int idx){
		if(idx>0)return (char*)Abilities.Abilities[idx-1]->Name();
		else return (char*)"---none---";
    }
	virtual DWORD GetIndexedValue (int idx){
		if(idx>0)return idx-1;
		return -1;
	}
	virtual char* GetCategory(int idx){
		return NULL;
	}	
};*/
void CreateUnitAbilityEnumeratorEnumerator(){
	//UnitAbilityEnumerator* CE=new UnitAbilityEnumerator;
	//Enumerator* E=ENUM.Get("ABILKI");
	//E->SetProcEnum(CE);
}
