//==================================================================================================================//
#include "stdheader.h"
#include "unitability.h"
//==================================================================================================================//
CHeroesCollector HeroesCollector;
extern int TrueTime;
extern int AnimTime;
extern HeroVariableStorage* CurrentHeroAbility;
//==================================================================================================================//
bool AddActiveUnitAbility(word Unit, ActiveUnitAbility* AUA);
HeroVariableStorage* GetHeroVariableStorage(OneObject* OB);
bool DrawSelPatch(float x,float y,int Type,float Radius,DWORD Color);
//==================================================================================================================//
UpHeroParam::UpHeroParam()
{
	Index=-1;
	FileID=0;
	SpriteID=0;
	Hint="";
	Special=false;
	IsInPass=false;
}
bool UpHeroParam::Realize(OneObject* OB)
{
	return false;
}
//==================================================================================================================//
UpLife::UpLife()
{
	AddMaxLife=0;
}
bool UpLife::Realize(OneObject* OB)
{
	if(OB&&AddMaxLife)
	{
		OB->Ref.General->MoreCharacter->Life+=AddMaxLife;
		OB->MaxLife+=AddMaxLife;
		OB->Life+=AddMaxLife;
		return true;
	}
	return false;
}
//==================================================================================================================//
UpAttack::UpAttack()
{
	AttackAdd=0;
}
bool UpAttack::Realize(OneObject* OB)
{
	if(OB&&AttackAdd)
	{
		OB->AddDamage+=AttackAdd;
	}
	return false;
}
//==================================================================================================================//
UpVision::UpVision()
{
	VisionAdd=0;
}
bool UpVision::Realize(OneObject* OB)
{
	if(OB&&VisionAdd)
	{
		OB->Ref.General->MoreCharacter->VisionType+=VisionAdd;
		if(OB->Ref.General->MoreCharacter->VisionType>8)
			OB->Ref.General->MoreCharacter->VisionType=8;
	}
	return false;
}
//==================================================================================================================//
UpRange::UpRange()
{
	RageAdd=0;
	AttackType=0;
}
bool UpRange::Realize(OneObject* OB)
{
	if(OB&&RageAdd)
	{
		OB->Ref.General->MoreCharacter->AttackRadius2[AttackType]+=RageAdd;
	}
	return false;
}
//==================================================================================================================//
UpAttackSpeed::UpAttackSpeed()
{
	SpeedAdd=0;
}
bool UpAttackSpeed::Realize(OneObject* OB)
{
	if(OB&&SpeedAdd)
	{
		OB->Ref.General->MoreCharacter->AttackSpeed+=SpeedAdd;
	}
	return false;
}
//==================================================================================================================//
UpMotionSpeed::UpMotionSpeed()
{
	MotionSpeedAdd=0;
}
bool UpMotionSpeed::Realize(OneObject* OB)
{
	if(OB&&MotionSpeedAdd)
	{
		OB->Ref.General->MoreCharacter->Speed+=MotionSpeedAdd;
	}
	return false;
}
//==================================================================================================================//
UpShield::UpShield()
{
	ShieldAdd=0;
	AttackType=0;
}
bool UpShield::Realize(OneObject* OB)
{
	if(OB&&ShieldAdd)
	{
		OB->Ref.General->MoreCharacter->Protection[AttackType]+=ShieldAdd;
	}
	return false;
}
//==================================================================================================================//
UpLifeRegeneration::UpLifeRegeneration()
{
	RegenerationAdd=0;
}
bool UpLifeRegeneration::Realize(OneObject* OB)
{
	if(RegenerationAdd&&OB)
	{
		int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<n;i++)
		{
			if(OB->ActiveAbility->ActiveAbilities[i]->Type==7)//HeroVariableStorage
			{
				HeroVariableStorage* S = (HeroVariableStorage*)OB->ActiveAbility->ActiveAbilities[i];
				S->UpLifeRegeneration+=RegenerationAdd;
				break;
			}
		}
		return true;
	}
	return false;
}
//==================================================================================================================//
UpSearchEnemyRadius::UpSearchEnemyRadius()
{
	RadiusAdd=0;
}
bool UpSearchEnemyRadius::Realize(OneObject* OB)
{
	if(OB&&RadiusAdd)
	{
		OB->Ref.General->MoreCharacter->VisRange+=(RadiusAdd<<4);
		return true;
	}
	return false;
}
//==================================================================================================================//
UpVariable::UpVariable()
{
	VarName="";
	AddValue=0;
}
bool UpVariable::Realize(OneObject* OB)
{
	bool rez=false;
	if(OB&&AddValue)
	{
		HeroVariableStorage* HVS=GetHeroVariableStorage(OB);
		if(HVS)
		{
			int* V=HVS->GetVarRef(VarName.str);
			if(V)
			{
				(*V)+=AddValue;
				rez=true;
			}
		}
	}
	return rez;
}
//==================================================================================================================//
LetPass::LetPass()
{
	Special=true;
}
bool LetPass::Realize(OneObject* OB)
{
	int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(OB->ActiveAbility->ActiveAbilities[i]->Type==7)//HeroVariableStorage
		{
			HeroVariableStorage* S = (HeroVariableStorage*)OB->ActiveAbility->ActiveAbilities[i];
			S->PassSelectHeroParametr=true;
			S->SelectHeroParamState=false;
			break;
		}
	}
	return false;
}
//==================================================================================================================//
ChooseUpHeroParam::ChooseUpHeroParam()
{
	Type=8;
	TypeAbil=3;
}
bool ChooseUpHeroParam::OnClick()
{
	if(UnitIndex!=-1&&UnitIndex!=0xFFFF&&UpIndex!=-1&&HeroAbilityRef.Get())
	{
		OneObject* OB=Group[UnitIndex];
		if(OB&&OB->ActiveAbility)
		{
			HeroAbility* Hero = (HeroAbility*)HeroAbilityRef.Get();//OB->newMons->Ability->AbilitiesList[HeroAbilityIndex];
			Hero->UpHeroParams[UpIndex]->Realize(OB);
			int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
			for(int i=0;i<n;i++)
			{
				if(OB->ActiveAbility->ActiveAbilities[i]->Type==8)//ChooseUpHeroParam
				{
					OB->ActiveAbility->ActiveAbilities.Del(i,1);
					n--;
					i--;
				}
				if(OB->ActiveAbility->ActiveAbilities[i]->Type==7)//HeroVariableStorage
				{
					HeroVariableStorage* S = (HeroVariableStorage*)OB->ActiveAbility->ActiveAbilities[i];
					S->SelectHeroParamState=false;
				}
			}
		}
	}
	return false;
}
bool ChooseUpHeroParam::Process()
{
	return true;
}
//==================================================================================================================//
bool UpLevelParam::OnClick()
{
	return false;
}
bool UpLevelParam::Process()
{
	return true;
}
//==================================================================================================================//
int UpHeroParamList::GetExpansionRules()
{
	return 2;
}
//==================================================================================================================//
/*
int CardList::GetExpansionRules()
{
	return 2;
}
*/
//==================================================================================================================//
HeroAbility::HeroAbility()
{
	ExperienceRadius=0;
	GetExperienceProc=0;
	LifeRegeneration=0;
}
bool HeroAbility::OnUnitBirth(OneObject* Newbie)
{
	if(Newbie)
	{
		HeroVariableStorage* HVS = new HeroVariableStorage();
		HVS->UnitIndex=Newbie->Index;
		HVS->OB=Newbie;
		HVS->HeroAbilityIndex=Index;
		HVS->Hero=this;
		HVS->SetA(this);
		HVS->FileID=FileID;
		HVS->SpriteID=SpriteID;
		HVS->ReBornTime=ReBornTime;
		HVS->freePlaceCard=PlaceCard;
		//
		//HVS->coloda=baseColoda;
		/*for(int i=0;i<baseColoda.GetAmount();i++){
			CardRef* CR=new CardRef;
			CR->cardplace.Set(baseColoda[i]->cardplace.Get());
			HVS->coloda.Add(CR);
		}		*/
		//
		HVS->CardRegeneration=CardRegeneration.Get();
		Newbie->Mana=Mana;
		Newbie->Ref.General->MoreCharacter->MaxMana=Mana;
		AddActiveUnitAbility(Newbie->Index,HVS);
		HeroesCollector.Herosima.Add(HVS);
		return true;
	}
	return false;
}
//==================================================================================================================//
HeroVariable::HeroVariable()
{
	Name="";
	Value=0;
}
//==================================================================================================================//
HeroVariableStorage::HeroVariableStorage()
{
	Type=7;
	HeroAbilityIndex=-1;
	Level=1;
	Experience=0;
	AddExperienceRadius=0;;
	Knowledge=100;
	PassSelectHeroParametr=false;
	SelectHeroParamState=false;
	Hero=NULL;
	ExperienceToNextLevel=-1;
	Visible=true;
	UpLifeRegeneration=0;
	LastUpLifeRegenerationTime=-1;
	LastUpManaRegenerationTime=-1;
	DieTime=0;
	IsTired=false;
	colodaPointer=0;
	indexx=1;
	TypeAbil=2;
	freePlaceCard=0;
}
int* HeroVariableStorage::GetVarRef(const char* Name)
{
	int n=Variables.GetAmount();
	for(int i=0;i<n;i++)
	{
		if(!strcmp(Variables[i]->Name.str,Name))
		{
			return &Variables[i]->Value;
		}
	}
	return NULL;
}
int HeroVariableStorage::GatherExperience(OneObject* Victim,word Killer)
{
	int rez=0;
	if(UnitIndex!=0xFFFF)
	{
		OneObject* OB=Group[UnitIndex];
		if((!OB->Sdoxlo))
		{
			if(!Hero)
			{
				if(HeroAbilityIndex!=-1)
				{
					if(OB&&OB->newMons->Ability)
					{
						int n=OB->newMons->Ability->AbilitiesList.GetAmount();
						if(HeroAbilityIndex<n)
						{
							Hero = (HeroAbility*)GetA();//OB->newMons->Ability->AbilitiesList[HeroAbilityIndex];
						}
					}
				}
			}
			if(Hero)
			{
				if(Killer!=0xFFFF)
				{
					if(!(Victim->NMask&OB->NMask))
					{
						OneObject* Ki = Group[Killer];
						if(Killer==UnitIndex)
						{
							Experience+=Victim->newMons->Expa;
							rez=Victim->newMons->Expa;
						}
						else
						if(OB->NNUM==Ki->NNUM)
						{
							int dist=Norma((OB->RealX>>4)-(Victim->RealX>>4),(OB->RealY>>4)-(Victim->RealY>>4));
							if(dist<(Hero->ExperienceRadius+AddExperienceRadius))
							{
								rez=(Victim->newMons->Expa*Hero->GetExperienceProc)/100;
								Experience+=rez;
							}
						}
					}
				}
			}
		}
	}	
	return rez;
}
bool HeroVariableStorage::Process()
{
	Hint.Clear();
	Hint.Add("Level: ");
	Hint.Add(Level);
	Hint.Add("\\");
	Hint.Add("Experience: ");
	Hint.Add(Experience);
	Hint.Add("\\ExpToNext: ");
	Hint.Add(ExperienceToNextLevel);
	if(Hero)
	{
		OneObject* OB=Group[UnitIndex];
		if(OB&&OB->Sdoxlo)
		{
			if(DieTime==0)DieTime=AnimTime;
			if(OB->Sdoxlo>300)OB->Sdoxlo=300;
			if((DieTime+ReBornTime*25*256)<=AnimTime)
			{
				OB->Sdoxlo=0;
				OB->Life=OB->MaxLife;
				DieTime=0;
			}
		}
		if(ExperienceToNextLevel==-1)ExperienceToNextLevel=GetExperienceToNextLevel();
		if(ExperienceToNextLevel!=-1)
		{
			if(Experience>=ExperienceToNextLevel)
				UpLevel();
		}
		if(Hero->LifeRegeneration)
		{
			if(UnitIndex!=0xFFFF)
			{
				if((!OB->Sdoxlo)&&OB->Life<OB->MaxLife)
				{
					if(LastUpLifeRegenerationTime==-1)
						LastUpLifeRegenerationTime=AnimTime;
					int up =((AnimTime-LastUpLifeRegenerationTime)*(Hero->LifeRegeneration+UpLifeRegeneration))/(25*256)/10;
					if(up)
					{
						OB->Life+=up;
						if(OB->Life>=OB->MaxLife)
						{
							OB->Life=OB->MaxLife;
							LastUpLifeRegenerationTime=-1;
						}
						else
							LastUpLifeRegenerationTime+=up*25*256*10/(Hero->LifeRegeneration+UpLifeRegeneration);
					}
				}
			}
		}
		if(OB&&(!OB->Sdoxlo)&&Hero&&Hero->ManaRegeneration)
		{
			if(OB->Mana<OB->Ref.General->MoreCharacter->MaxMana)
			{
				if(LastUpManaRegenerationTime==-1)
					LastUpManaRegenerationTime=AnimTime;
				int up =((AnimTime-LastUpManaRegenerationTime)*(Hero->ManaRegeneration+UpManaRegeneration))/(25*256)/10;
				if(up)
				{
					OB->Mana+=up;
					if(OB->Mana>=OB->Ref.General->MoreCharacter->MaxMana)
					{
						OB->Mana=OB->Ref.General->MoreCharacter->MaxMana;
						LastUpManaRegenerationTime=-1;
					}
					else
						LastUpManaRegenerationTime+=up*25*256*10/(Hero->ManaRegeneration+UpManaRegeneration);
				}
			}
		}
		if(OB)
		{
			if(OB->GetTired<=0&&(!IsTired))
			{
				if(Hero->SpeedDownIfTired&&Hero->SpeedDownIfTired<101)
				{
					SpeedMinus=(OB->Ref.General->MoreCharacter->Speed*Hero->SpeedDownIfTired)/100;
					OB->Ref.General->MoreCharacter->Speed-=SpeedMinus;
					IsTired=true;
				}
			}
			if(IsTired&&OB->GetTired>0)
			{
				OB->Ref.General->MoreCharacter->Speed+=SpeedMinus;
				IsTired=false;
			}
		}
/*		if (freePlaceCard<coloda) {
		}*/
	}
	return true;
}
int HeroVariableStorage::GetExperienceToNextLevel()
{
	int rez=-1;
	if(Hero)
	{
		int n=Hero->LevelUp.GetAmount();
		if(Level<=n)
			rez=Hero->LevelUp[Level-1];
	}
	return rez;
}
void HeroVariableStorage::UpLevel()
{
	if(!SelectHeroParamState)
	{
		Level++;
		Experience=Experience-ExperienceToNextLevel;
		ExperienceToNextLevel=GetExperienceToNextLevel();
		CreateOptionUpHeroParams();
		CreateLevelUpParam();
	}
}
void HeroVariableStorage::CreateOptionUpHeroParams()
{
	int n=Hero->UpHeroParams.GetAmount();
	if(n)
	{
		ClassArray<Probability> W;
		int Sum=0;
		for(int i=0;i<n;i++)
		{
			int w1=0;
			int nn=Hero->UpHeroParams[i]->ProbabilityList.GetAmount();
			if(!Hero->UpHeroParams[i]->Special)
			for(int j=0;j<nn;j++)
			{
				if(Level<=Hero->UpHeroParams[i]->ProbabilityList[j]->Level)
				{
					w1=Hero->UpHeroParams[i]->ProbabilityList[j]->Weight;
					break;
				}
			}
			if(w1)
			{
				Probability* P = new Probability();
				P->Level=i;
				P->Weight=w1;
				W.Add(P);
				Sum+=w1;
			}
		}
		int wn=W.GetAmount();
		int wnn=wn;
		if((!PassSelectHeroParametr))
		{
			if(Sum)
			{
				//int s1=Sum;
				int u1 = rando()%Sum;
				for(int i=0;i<wn;i++)
				{
					u1-=W[i]->Weight;
					if(u1<=0)
					{
						AddChooseUpHeroParam(W[i]->Level);
						const char* UpName = Hero->UpHeroParams[W[i]->Level]->GetClassName();
						for(int k=0;k<wn;k++)
						{
							if(!strcmp(UpName,Hero->UpHeroParams[W[k]->Level]->GetClassName()))
							{
								Sum-=W[k]->Weight;
								W.Del(k,1);
								wn--;
								k--;
							}
						}
						break;
					}
				}
				if(Sum)
				{
					u1 = rando()%Sum;
					for(int i=0;i<wn;i++)
					{
						u1-=W[i]->Weight;
						if(u1<=0)
						{
							AddChooseUpHeroParam(W[i]->Level);
							break;
						}
					}
				}
			}
			if(wnn>2)
			{
				for(int i=0;i<n;i++)
				{
					if(Hero->UpHeroParams[i]->Special)
						AddChooseUpHeroParam(i);
				}
			}
		}
		else
		{
			for(int i=0;i<wn;i++)
			{
				if(Hero->UpHeroParams[W[i]->Level]->IsInPass)
					AddChooseUpHeroParam(W[i]->Level);
			}
			PassSelectHeroParametr=false;
		}
	}
}
void HeroVariableStorage::AddChooseUpHeroParam(int UpHeroParamIndex)
{
	UpHeroParam* U = Hero->UpHeroParams[UpHeroParamIndex];
	ChooseUpHeroParam* Ch = new ChooseUpHeroParam();
	Ch->UnitIndex=UnitIndex;
	Ch->UpIndex=UpHeroParamIndex;
	Ch->FileID=U->FileID;
	Ch->SpriteID=U->SpriteID;
	Ch->Visible=true;
	Ch->Hint=U->Hint;
	//Ch->HeroAbilityIndex=HeroAbilityIndex;
	Ch->HeroAbilityRef.Set(Hero);
	AddActiveUnitAbility(UnitIndex,Ch);
	SelectHeroParamState=true;
}
void HeroVariableStorage::CreateLevelUpParam()
{
	int n=Hero->UpParamForLevel.GetAmount();
	if(n)
	{
		ClassArray<Probability> W;
		int Sum=0;
		for(int i=0;i<Hero->UpParamForLevel.GetAmount();i++)
		{
			int w1=0;
			int nn=Hero->UpParamForLevel[i]->ProbabilityList.GetAmount();
			if(!Hero->UpParamForLevel[i]->Special) //UpHeroParams
				for(int j=0;j<nn;j++)
				{
					if(Level<=Hero->UpParamForLevel[i]->ProbabilityList[j]->Level)
					{
						w1=Hero->UpParamForLevel[i]->ProbabilityList[j]->Weight;
						break;
					}
				}
				if(w1)
				{
					Probability* P = new Probability();
					P->Level=i;
					P->Weight=w1;
					W.Add(P);
					Sum+=w1;
				}
		}
		int wn=W.GetAmount();
		if(Sum)
		{
			int u1 = rando()%Sum;
			for(int i=0;i<wn;i++)
			{
				u1-=W[i]->Weight;
				if(u1<=0)
				{
					UpHeroParam* U = Hero->UpParamForLevel[W[i]->Level];
					U->Realize(OB);
					break;
				}
			}
		}
	}
}
//==================================================================================================================//
CHeroesCollector::CHeroesCollector()
{}
void CHeroesCollector::OnDieProcess(OneObject* Victim,word Killer)
{
	if(Killer!=0xFFFF)
	{
		int n=Herosima.GetAmount();
		for(int i=0;i<n;i++)
		{
			if(Herosima[i])
			{
				OneObject* OB=Group[Herosima[i]->UnitIndex];
				if((!OB))//||(OB&&OB->Sdoxlo))
				{
					Herosima.Del(i,1);
					i--;
					n--;
				}
				else
				{
					if(!OB->Sdoxlo)
						Herosima[i]->GatherExperience(Victim,Killer);
				}
			}
		}
	}
}
//==================================================================================================================//
extern RomeHeroCollector RomeHeroes; 
void HeroesGatherExperience(OneObject* Victim,word Killer)
{
	HeroesCollector.OnDieProcess(Victim,Killer);
	if(Killer!=0xFFFF)
	{
		OneObject* OB=Group[Killer];
		if(OB)
		{
			RomeHeroes.AddExp(Victim,OB);
		}
	}
}
/*
void IfHiroBirth(OneObject* OB)
{
	if(OB&&OB->newMons->Ability)
	{
		int n = OB->newMons->Ability->AbilitiesList.GetAmount();
		if(!n)
		{
			OB->newMons->Ability->Process(OB->Index);
			n = OB->newMons->Ability->AbilitiesList.GetAmount();
		}
		for(int i=0;i<n;i++)
		{
			HeroAbility* HA = dynamic_cast<HeroAbility*> (OB->newMons->Ability->AbilitiesList[i]);
			if(HA)
			{
				HeroVariableStorage* HVS = new HeroVariableStorage();
				HVS->UnitIndex=OB->Index;
				HVS->HeroAbilityIndex=i;
				HVS->Hero=HA;
				HVS->FileID=HA->FileID;
				HVS->SpriteID=HA->SpriteID;
				HVS->ReBornTime=HA->ReBornTime;
				AddActiveUnitAbility(OB->Index,HVS);
				HeroesCollector.Herosima.Add(HVS);
			}
		}
	}
}
*/
//==================================================================================================================//
CUnitExperienceParm::CUnitExperienceParm()
{
	LifeK=0;
	DamageK=0;
	RangeK=0;
	SpeedK=0;
	xmlQuote xml;
	if(xml.ReadFromFile("UnitsAbility\\UnitExperienceParm.xml"))
	{
		ErrorPager EP;
		this->Load(xml,this,&EP);
	}
}
//==================================================================================================================//
CUnitExperienceParm UnitExperienceParm;
//==================================================================================================================//
void SetUnitExperience(NewMonster* NM)
{
	NM->Expa=(UnitExperienceParm.LifeK*NM->Life)/100+(UnitExperienceParm.SpeedK*NM->MotionDist)/100;
	int MDamage=0;
	int range=0;
	for(int i=0;i<NAttTypes;i++)
	{
		if(NM->MaxDamage[i]>MDamage)
		{
			MDamage=NM->MaxDamage[i];
			range=NM->AttackRadius2[i];
		}
	}
	NM->Expa+=(UnitExperienceParm.DamageK*MDamage)/100+(UnitExperienceParm.RangeK*range)/100;
}
//==================================================================================================================//
HeroVariableStorage* GetHeroVariableStorage(OneObject* OB)
{
	if(OB&&OB->ActiveAbility)
	{
		int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<n;i++)
		{
			if(OB->ActiveAbility->ActiveAbilities[i]->Type==7)//HeroVariableStorage
			{
				return (HeroVariableStorage*)OB->ActiveAbility->ActiveAbilities[i];
			}
		}
	}	
	return NULL;
}
//==================================================================================================================//
void CreateNewActiveWeapon(char* WMName,int Index,int sx, int sy, int sz, int DestIndex, int dx, int dy, int dz, AdditionalWeaponParams* AddParams);
MagicCardActive::MagicCardActive()
{
	fst=false;
	timer2=0;
	timer1=0;
	TypeAbil=-1;
};
bool MagicCardActive::Process()
{
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if(!CurrentHeroAbility&&(TypeAbil==1||TypeAbil==4)){
		return true;
	}
	if(!Visible&&(TypeAbil==1||TypeAbil==4)&&CurrentHeroAbility->freePlaceCard>0){
		timer1=TrueTime+CurrentHeroAbility->CardRegeneration/CurrentHeroAbility->indexx;
		CurrentHeroAbility->freePlaceCard--;
		Visible=true;
		CurrentHeroAbility->indexx=1;
	};
	if(Visible){
		MagicCard* A = (MagicCard*)GetA();
		if(A){
			if(TypeAbil==1||TypeAbil==4){
				Hint=A->Hint;
				Hint.Add("\\Damage: ");
				Hint.Add(A->Damage.Get());
				Hint.Add("\\Mana: ");
				Hint.Add(A->ManaCost.Get());
				if(GetCoolDownProc()>0){
					Hint.Add("\\CoolDown: ");
					Hint.Add(100-GetCoolDownProc());
					Hint.Add("%");
				}
				if(fst&&GetCoolDownProc()==0){
					fst=false;
					OneObject* OBj= NULL;
					if(A->EnemyTarget||A->FriendlyTarget){
						itr_UnitsInRadius.Create(Tx,Ty,A->Radius.Get());
						while(OBj=itr_UnitsInRadius.Next()){
							if((A->EnemyTarget&&!(OBj->NMask&OB->NMask))||(A->FriendlyTarget&&(OBj->NMask&OB->NMask))){
								bool mcb=true;
								/*for(int jj=0;jj<A->UnCast.GetAmountOfElements();jj++){
									for(int ii=0;ii<OBj->ActiveAbility->ActiveAbilities.GetAmountOfElements();ii++){
										UnitAbility* B = OBj->ActiveAbility->ActiveAbilities[ii]->GetA();
										if((B==A->UnCast[jj]->Get())&&OBj->ActiveAbility->ActiveAbilities[ii]->TypeAbil==0){
											for(int ll=0;ll<B->MSCast.GetAmount();ll++){
												B->MSCast[ll]->CancelSpell(OBj);
											}
										}
									}
								}*/
								if(OBj->ActiveAbility){
									for(int iii=0;iii<A->Blocking.GetAmountOfElements();iii++){
										for(int jjj=0;jjj<OBj->ActiveAbility->ActiveAbilities.GetAmount();jjj++){
											if(OBj->ActiveAbility->ActiveAbilities[jjj]->TypeAbil==0&&(OBj->ActiveAbility->ActiveAbilities[jjj]->GetA()==A->Blocking[iii]->Get())) mcb=false;
										}
									}
								}
								if(mcb){
									MagicCardActive* activecard=new MagicCardActive;
									activecard->OB=OBj;
									activecard->UnitIndex=OBj->Index;
									activecard->timer2=TrueTime+A->LongTime.Get();
									activecard->timer1=A->LongTime.Get();
									activecard->Radius=A->Radius.Get();
									activecard->Damage=A->Damage.Get();
									activecard->TypeAbil=0;
									activecard->FileID=A->EffectFileID;
									activecard->SpriteID=A->EffectSpriteID;
									activecard->fst=true;
									activecard->Visible=true;
									activecard->SetA(A);
									bool rez=AddActiveUnitAbility(OBj->Index,activecard);
									if(!rez){
										return true;
									}
/*									for(int jj=0;jj<A->MSCast.GetAmount();jj++){
										A->MSCast[jj]->CastSpell(OBj);
									}*/
								}
							}
						}
					}
					Visible=false;
					int cou=OB->ActiveAbility->ActiveAbilities.GetAmount();
					for(int jj=0;jj<cou;jj++){
						if(OB->ActiveAbility->ActiveAbilities[jj]->Serial==Serial){
							OB->ActiveAbility->ActiveAbilities.MoveElement(jj,cou-jj);
							CurrentHeroAbility->freePlaceCard++;
							return true;
						};
					};
				};
			}else
				if(TypeAbil==0){
					if (timer1!=0&&timer2!=0&&TrueTime<=timer2) {
						int tm=99-((timer2-TrueTime)*100)/(timer1);
						Hint=A->Hint;
						Hint.Add("\\Processed - ");
						Hint.Add(tm);
						Hint.Add("%");
						if(A->MSCast.GetAmount()>0){
							Hint.Add("\\");
							for(int jj=0;jj<A->MSCast.GetAmount();jj++){
								Hint.Add(A->MSCast[jj]->name);
								Hint.Add(" ");
								if(A->MSCast[jj]->sign)	Hint.Add("+");
								Hint.Add(A->MSCast[jj]->realchng);
								Hint.Add("  ");
							}
						}
						int cr=(tm)%5;
						if (cr==0&&fst){
							int Tz=GetHeight(OB->RealX>>4,OB->RealY>>4);
							AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
							AWP->Damage=Damage;
							AWP->AttType=0;
							CreateNewActiveWeapon(A->EffectName.str,UnitIndex,OB->RealX>>4,OB->RealY>>4,Tz,0xFFFF,OB->RealX>>4,OB->RealY>>4,Tz,AWP);
							fst=false;
						}
						if (cr!=0&&!fst) {
							fst=true;
						}
					};
					if(timer2!=0&&TrueTime>timer2){ 
						/*for(int jj=0;jj<A->MSCast.GetAmount();jj++){
							A->MSCast[jj]->CancelSpell(OB);
						}*/
						return false;
					}
				}
		}		
	}
	return true;
};
bool MagicCardActive::OnClick()
{
	if(TypeAbil==0||GetCoolDownProc()!=0) return false;
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if(isTarget()) fst=true;
	return false;
};
bool MagicCardActive::OnRightClick()
{
	if(TypeAbil==0||GetCoolDownProc()!=0) return false;
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	Visible=false;
	int cou=OB->ActiveAbility->ActiveAbilities.GetAmount();
	for(int jj=0;jj<cou;jj++){
		if(OB->ActiveAbility->ActiveAbilities[jj]->Serial==Serial){
			OB->ActiveAbility->ActiveAbilities.MoveElement(jj,cou-jj);
			CurrentHeroAbility->freePlaceCard++;
			CurrentHeroAbility->indexx=4;
			return true;
		}
	}
	return false;
};
bool MagicCardActive::Execute(word TargetUnit, int x, int y, int z)
{
	bool rez=CanTarget(TargetUnit,x,y,z);
	int Tz;
	if(rez)
	{
		MagicCard* A = (MagicCard*)GetA();
		if(A){
			CurrentHeroAbility=GetHeroVariableStorage(OB);
			/*if(A->EnemyTarget||A->FriendlyTarget){
				rez=false;
				itr_UnitsInRadius.Create(x,y,A->Radius.Get());
				OneObject* OOB=itr_UnitsInRadius.Next();
				while(OOB&&!rez){
					if((A->EnemyTarget&&!(OOB->NMask&OB->NMask))||(A->FriendlyTarget&&(OOB->NMask&OB->NMask))) rez=true;
					OOB=itr_UnitsInRadius.Next();
				}
				if(!rez) return rez;
			}*/
			if(OB->Mana>=A->ManaCost.Get())
			{
				Tz=GetHeight(x,y);
				AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
				AWP->Damage=A->Damage.Get();
				AWP->AttType=0;
				CreateNewActiveWeapon(A->EffectName.str,UnitIndex,x,y,Tz,0xFFFF,x,y,Tz,AWP);
				OB->Mana-=A->ManaCost.Get();
			}
		}
		Tx=x;
		Ty=y;
		fst=true;
	}
	return rez;
}
bool MagicCardActive::isTarget()
{
	if(GetCoolDownProc()!=0) return false;
	return true;
}
bool MagicCardActive::DrawCursor(int x,int y)
{	
	if(TypeAbil==1||TypeAbil==4){
		CurrentHeroAbility=GetHeroVariableStorage(OB);
		MagicCard* A = (MagicCard*)GetA();
		if(A){
			int r=A->Radius.Get();
			DrawSelPatch(x,y,A->CursType,r,A->CursColor);
			if(A->EnemyTarget||A->FriendlyTarget){
				itr_UnitsInRadius.Create(x,y,r);
				while(OneObject* obb=itr_UnitsInRadius.Next())
				{
					if(A->EnemyTarget&&!(obb->NMask&OB->NMask))
					{
						obb->HighlightMask=1;
					}
					if(A->FriendlyTarget&&obb->NMask&OB->NMask)
					{
						obb->HighlightMask=2;
					}
				}			
				return true;
			}
		}
	}
	return false;
}
bool MagicCardActive::CanTarget(word TargetUnit, int x, int y, int z)
{
	if(GetCoolDownProc()!=0) return false;
	/*UnitAbility* A = GetA();
	if(A){
		for(int i=0;i<A->Blocking.GetAmountOfElements();i++){
			for(int j=0;j<OB->ActiveAbility->ActiveAbilities.GetAmount();j++){
				if(OB->ActiveAbility->ActiveAbilities[j]->TypeAbil==0&&(OB->ActiveAbility->ActiveAbilities[j]->GetA()==A->Blocking[i]->Get())) return false;
			}
		}
	};*/
	return true;
}
int MagicCardActive::GetCoolDownProc()
{
	int rez=0;
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if (timer1>TrueTime&&CurrentHeroAbility->CardRegeneration!=0) {
		rez=100*(timer1-TrueTime)/(CurrentHeroAbility->CardRegeneration)+1;
	}
	return rez;
}
//==================================================================================================================//
MagicCard::MagicCard()
{
	CursorTexture=0;
	EnemyTarget=false;
	FriendlyTarget=false;
	ProcTime=0;
}
bool MagicCard::OnUnitBirth(OneObject* Newbie)
{
	bool rez=false;
	if (Newbie) 
	{
		MagicCardActive* MCA = new MagicCardActive();
		MCA->OB=Newbie;
		MCA->UnitIndex=Newbie->Index;
		MCA->Visible=false;
		MCA->timer2=LongTime.Get();
		MCA->TypeAbil=1;
		MCA->Radius=Radius.Get();
		MCA->Damage=Damage.Get();
		MCA->SetA(this);
		MCA->Hint=Hint;
		CopyToActive(MCA);
		rez=AddActiveUnitAbility(Newbie->Index,MCA);
	}
	return rez;
};
bool MagicCard::CanApply(OneObject* her, HeroVariableStorage* storage)
{
	return false;
}
int MagicCard::GetCoolDownProc(OneObject* her, HeroVariableStorage* storage)
{
	int rez=0;
	CurrentHeroAbility=storage;
	if (ProcTime>TrueTime&&CurrentHeroAbility->CardRegeneration!=0) {
		rez=100*(ProcTime-TrueTime)/(CurrentHeroAbility->CardRegeneration)+1;
	}
	if(ProcTime<=TrueTime) rez=0;
	return rez;
}
int MagicCard::GetRadius(OneObject* her, HeroVariableStorage* storage)
{
	CurrentHeroAbility=storage;
	return Radius.Get();
}
bool MagicCard::OnClick(OneObject* her, HeroVariableStorage* storage)
{
	return false;
}
bool MagicCard::Execute(OneObject* her, HeroVariableStorage* storage , word TargetUnit, int x, int y, int z)
{
	return false;
}
bool MagicCard::isTarget(OneObject* her, HeroVariableStorage* storage)
{
	return true;
}
bool MagicCard::CanTarget(OneObject* her, HeroVariableStorage* storage, word TargetUnit, int x, int y, int z)
{
	return false;
}
bool MagicCard::isActive(OneObject* her, HeroVariableStorage* storage)
{
	return false;
}
MagicCardActive* MagicCard::GetMCActiveAbility()
{
	return new MagicCardActive();
}
//==================================================================================================================//
//=======================================    Hero Card    ==========================================================//
//==================================================================================================================//
//=======================================    Thickskin    ==========================================================//
MCThickskin::MCThickskin()
{
	CursorTexture=0;
	EnemyTarget=false;
	FriendlyTarget=false;
}
bool MCThickskin::OnUnitBirth(OneObject* Newbie)
{
	bool rez=false;
	if (Newbie) 
	{
		CurrentHeroAbility=GetHeroVariableStorage(Newbie);
		MCThickskinActive* MCT = new MCThickskinActive();
		MCT->OB=Newbie;
		MCT->UnitIndex=Newbie->Index;
		MCT->Visible=false;
		MCT->Radius=Radius.Get();
		MCT->Damage=Damage.Get();
		MCT->TypeAbil=1;
		MCT->SetA(this);
		MCT->Hint=Hint;
		CopyToActive(MCT);
		rez=AddActiveUnitAbility(Newbie->Index,MCT);
	}
	return rez;
};
MagicCardActive* MCThickskin::GetMCActiveAbility()
{
	return new MCThickskinActive();
}
//==================================================================================================================//
MCThickskinActive::MCThickskinActive()
{
};
CEXPORT int CreateNewTerrMons2(byte NI,int x,int y,word Type);
/*for(int i=0;i<MAXOBJECT;i++){
OBj=Group[i];
//						if((A->FriendlyTarget&&(OB->NMask&OBj->NMask))||(A->EnemyTarget&&!(OB->NMask&OBj->NMask)))
if (OBj&&!OBj->Sdoxlo){*/
bool MCThickskinActive::Process()
{
	/*CurrentHeroAbility=GetHeroVariableStorage(OB);
	if(!Visible&&CurrentHeroAbility->freePlaceCard>0){
		timer1=TrueTime+CurrentHeroAbility->CardRegeneration/CurrentHeroAbility->indexx;
		CurrentHeroAbility->freePlaceCard--;
		Visible=true;
		CurrentHeroAbility->indexx=1;
	};
	if(Visible){
		MCThickskin* A = (MCThickskin*)GetA();
		if(A){
			if(TypeAbil==1){
				Hint=A->Hint;
				Hint.Add("\\Damage: ");
				Hint.Add(A->Damage.Get());
				Hint.Add("\\Mana: ");
				Hint.Add(A->ManaCost.Get());
				if(GetCoolDownProc()>0){
					Hint.Add("\\CoolDown: ");
					Hint.Add(100-GetCoolDownProc());
					Hint.Add("%");
				}
				if(fst&&GetCoolDownProc()==0){
					fst=false;
					OneObject* OBj= NULL;
					if(A->EnemyTarget||A->FriendlyTarget){
						itr_UnitsInRadius.Create(Tx,Ty,A->Radius.Get());
						while(OBj=itr_UnitsInRadius.Next()){
							if((A->EnemyTarget&&!(OBj->NMask&OB->NMask))||(A->FriendlyTarget&&(OBj->NMask&OB->NMask))){
								for(int jj=0;jj<A->UnCast.GetAmountOfElements();jj++)
								for(int ii=0;ii<OBj->ActiveAbility->ActiveAbilities.GetAmountOfElements();ii++){
									MCThickskin* B = (MCThickskin*)OBj->ActiveAbility->ActiveAbilities[ii]->GetA();
									if((B==(MCThickskin*)A->UnCast[jj])&&OBj->ActiveAbility->ActiveAbilities[ii]->TypeAbil==0){
										for(int ll=0;ll<B->MSCast.GetAmount();ll++){
											B->MSCast[ll]->CancelSpell(OBj);
										}
								MCThickskinActive* activecard=new MCThickskinActive;
								activecard->OB=OBj;
								activecard->UnitIndex=OBj->Index;
								activecard->timer2=TrueTime+A->LongTime.Get();
								activecard->timer1=A->LongTime.Get();
								activecard->Radius=A->Radius.Get();
								activecard->Damage=A->Damage.Get();
								activecard->TypeAbil=0;
								activecard->FileID=A->EffectFileID;
								activecard->SpriteID=A->EffectSpriteID;
								activecard->fst=true;
								activecard->Visible=true;
								activecard->SetA(A);
								bool rez=AddActiveUnitAbility(OBj->Index,activecard);
								if(!rez){
									return true;
								}
								for(int jj=0;jj<A->MSCast.GetAmount();jj++){
									A->MSCast[jj]->CastSpell(OBj);
								}
							}
						}
					}
					Visible=false;
					int cou=OB->ActiveAbility->ActiveAbilities.GetAmount();
					for(int jj=0;jj<cou;jj++){
						if(OB->ActiveAbility->ActiveAbilities[jj]->Serial==Serial){
							OB->ActiveAbility->ActiveAbilities.MoveElement(jj,cou-jj);
							CurrentHeroAbility->freePlaceCard++;
							return true;
						};
					};
				};
			}else
				if(TypeAbil==0){
					if (timer1!=0&&timer2!=0&&TrueTime<=timer2) {
						int tm=99-((timer2-TrueTime)*100)/(timer1);
						Hint=A->Hint;
						Hint.Add("\\Processed - ");
						Hint.Add(tm);
						Hint.Add("%");
						if(A->MSCast.GetAmount()>0){
							Hint.Add("\\");
							for(int jj=0;jj<A->MSCast.GetAmount();jj++){
								Hint.Add(A->MSCast[jj]->name);
								Hint.Add(" ");
								if(A->MSCast[jj]->sign)	Hint.Add("+");
								Hint.Add(A->MSCast[jj]->realchng);
								Hint.Add("  ");
							}
						}
						int cr=(tm)%5;
						if (cr==0&&fst){
							int Tz=GetHeight(OB->RealX>>4,OB->RealY>>4);
							AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
							AWP->Damage=Damage;
							AWP->AttType=0;
							CreateNewActiveWeapon(A->EffectName.str,UnitIndex,OB->RealX>>4,OB->RealY>>4,Tz,0xFFFF,OB->RealX>>4,OB->RealY>>4,Tz,AWP);
							fst=false;
						}
						if (cr!=0&&!fst) {
							fst=true;
						}
					};
					if(timer2!=0&&TrueTime>timer2){ 
						for(int jj=0;jj<A->MSCast.GetAmount();jj++){
							A->MSCast[jj]->CancelSpell(OB);
						}
						return false;
					}
				}
		}		
	}*/
	return true;
};
/*bool MCThickskinActive::OnClick()
{
	if(TypeAbil==0||GetCoolDownProc()!=0) return false;
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if(isTarget()) fst=true;
	if ((timer1==0||TrueTime>timer1)&&(timer2==0||TrueTime>timer2)) {
		MCThickskin* A = (MCThickskin*)GetA();
		if(A){
			if (A->CardRegeneration.Get()>=20){
				timer1=TrueTime+A->CardRegeneration.Get();
			}else{
				timer1=TrueTime+10000;
			};
			timer2=TrueTime+A->LongTime.Get();
		};
	};
	return false;
};*/
/*bool MCThickskinActive::OnRightClick()
{
	if(TypeAbil==0||GetCoolDownProc()!=0) return false;
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	Visible=false;
	int cou=OB->ActiveAbility->ActiveAbilities.GetAmount();
	for(int jj=0;jj<cou;jj++){
		if(OB->ActiveAbility->ActiveAbilities[jj]->Serial==Serial){
			OB->ActiveAbility->ActiveAbilities.MoveElement(jj,cou-jj);
			CurrentHeroAbility->freePlaceCard++;
			CurrentHeroAbility->indexx=4;
			return true;
		}
	}
	if ((timer1==0||AnimTime>timer1)&&(timer2==0||AnimTime>timer2)) {
		MCThickskin* A = (MCThickskin*)GetA();
		timer1=AnimTime+A->CardRegeneration.Get()*25*64;
		timer1=AnimTime;
		timer2=AnimTime;
	};
	return false;
};*/
/*bool MCThickskinActive::Execute(word TargetUnit, int x, int y, int z)
{
	bool rez=CanTarget(TargetUnit,x,y,z);
	int Tz;
	if(rez)
	{
		MCThickskin* A = (MCThickskin*)GetA();
		if(A){
			CurrentHeroAbility=GetHeroVariableStorage(OB);
			if(A->EnemyTarget||A->FriendlyTarget){
				rez=false;
				itr_UnitsInRadius.Create(x,y,A->Radius.Get());
				OneObject* OOB=itr_UnitsInRadius.Next();
				while(OOB&&!rez){
					if((A->EnemyTarget&&!(OOB->NMask&OB->NMask))||(A->FriendlyTarget&&(OOB->NMask&OB->NMask))) rez=true;
					OOB=itr_UnitsInRadius.Next();
				}
				if(!rez) return rez;
			}
			if(OB->Mana>=A->ManaCost.Get())
			{
				Tz=GetHeight(x,y);
				AdditionalWeaponParams* AWP = new AdditionalWeaponParams();
				AWP->Damage=A->Damage.Get();
				AWP->AttType=0;
				CreateNewActiveWeapon(A->EffectName.str,UnitIndex,x,y,Tz,0xFFFF,x,y,Tz,AWP);
				OB->Mana-=A->ManaCost.Get();
			}
		}
		Tx=x;
		Ty=y;
		fst=true;
	}
	return rez;
}*/
/*bool MCThickskinActive::isTarget()
{
	if(GetCoolDownProc()!=0) return false;
	return true;
}
bool MCThickskinActive::CanTarget(word TargetUnit, int x, int y, int z)
{
	if(GetCoolDownProc()!=0) return false;
	MCThickskin* A = (MCThickskin*)GetA();
	if(A){
		for(int i=0;i<A->Blocking.GetAmountOfElements();i++){
			for(int j=0;j<OB->ActiveAbility->ActiveAbilities.GetAmount();j++){
                if(OB->ActiveAbility->ActiveAbilities[j]->TypeAbil==0&&(OB->ActiveAbility->ActiveAbilities[j]->GetA()==A->Blocking[i])) return false;
			}
		}
	};
	return true;
}*/
/*//=======================================    Antimagic    ==========================================================//
MCAntimagic::MCAntimagic()
{
	CursorTexture=0;
	EnemyTarget=false;
	FriendlyTarget=false;
}
MagicCardActive* MCAntimagic::GetMCActiveAbility()
{
	return new MCAntimagicActive();
}
//==================================================================================================================//
MCAntimagicActive::MCAntimagicActive()
{
};
bool MCAntimagicActive::Process()
{
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if (timer1!=0&&timer2!=0&&AnimTime<=timer2) {
		int cr=(99-((timer2-AnimTime)*100)/(timer1*25*256))%20;
		if (cr==0&&fst){
//			CreateNewTerrMons2(OB->NNUM,OB->RealX+5,OB->RealY-5,110);
			fst=false;
		}
		if (cr!=0&&!fst) {
			fst=true;
		}
	};
	if (timer1!=0&&(timer2!=0&&AnimTime>timer2)) 	return false;
	return true;
};
bool MCAntimagicActive::Execute(word TargetUnit, int x, int y, int z)
{
	return true;
}

bool MCAntimagicActive::CanTarget(word TargetUnit, int x, int y, int z)
{
	return false;
}
//=======================================    Freezing     ==========================================================//
MCFreezing::MCFreezing()
{
	CursorTexture=0;
	EnemyTarget=false;
	FriendlyTarget=false;
}
MagicCardActive* MCFreezing::GetMCActiveAbility()
{
	return new MCFreezingActive();
}
//==================================================================================================================//
MCFreezingActive::MCFreezingActive()
{
};
bool MCFreezingActive::Process()
{
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if (timer1!=0&&timer2!=0&&AnimTime<=timer2) {
		int cr=(99-((timer2-AnimTime)*100)/(timer1*25*256))%20;
		if (cr==0&&fst){

			fst=false;
		}
		if (cr!=0&&!fst) {
			fst=true;
		}
	};
	if (timer1!=0&&(timer2!=0&&AnimTime>timer2)) 	return false;
	return true;
};
bool MCFreezingActive::Execute(word TargetUnit, int x, int y, int z)
{
	return true;
}

bool MCFreezingActive::CanTarget(word TargetUnit, int x, int y, int z)
{
	return false;
}
//=======================================     Berserk     ==========================================================//
MCBerserk::MCBerserk()
{
	CursorTexture=0;
	EnemyTarget=false;
	FriendlyTarget=false;
}
MagicCardActive* MCBerserk::GetMCActiveAbility()
{
	return new MCBerserkActive();
}
//==================================================================================================================//
MCBerserkActive::MCBerserkActive()
{
};
bool MCBerserkActive::Process()
{
	CurrentHeroAbility=GetHeroVariableStorage(OB);
	if (timer1!=0&&timer2!=0&&AnimTime<=timer2) {
		int cr=(99-((timer2-AnimTime)*100)/(timer1*25*256))%20;
		if (cr==0&&fst){

			fst=false;
		}
		if (cr!=0&&!fst) {
			fst=true;
		}
	};
	if (timer1!=0&&(timer2!=0&&AnimTime>timer2)) 	return false;
	return true;
};
bool MCBerserkActive::Execute(word TargetUnit, int x, int y, int z)
{
	return true;
}

bool MCBerserkActive::CanTarget(word TargetUnit, int x, int y, int z)
{
	return false;
}*/
//==================================================================================================================//
CardPlace::CardPlace()
{}
ActiveUnitAbility* CardPlace::GetActiveAbility()
{
	return new ActiveCardPlace();
}
//==================================================================================================================//
ActiveCardPlace::ActiveCardPlace()
{
	heroStorage=NULL;
	newCardSetTime=0;
	cardIndex=-1;
	p_card=NULL;
	OnClk=true;
	Restart=false;
	TypeAbil=1;
}
bool ActiveCardPlace::Process(OneObject* OBb)
{
	if(Restart) return false;
/*	if(Restart&&Visible) {
		Visible=false;
		return true;
	}
	if(Restart&&!Visible) {
		Visible=true;
		Restart=false;
		return true;
	}*/
	OB=OBb;
	heroStorage=GetHeroVariableStorage(OB);
	if(OB&&!OB->Sdoxlo&&heroStorage)
	{
		CurrentHeroAbility=heroStorage;
		if(!p_card&&0<CurrentHeroAbility->coloda.GetAmount()) 
		{
			OnClk=false;
			newCardSetTime=TrueTime+CurrentHeroAbility->CardRegeneration/CurrentHeroAbility->indexx;
			p_card=CurrentHeroAbility->coloda[0];			
			if(p_card)
			{
				p_card->ProcTime=newCardSetTime;
				oldFileID=FileID;
				FileID=p_card->FileID;
				oldSpriteID=SpriteID;
				SpriteID=p_card->SpriteID;
//				cardIndex=CurrentHeroAbility->colodaPointer;
				CurrentHeroAbility->coloda.Del(0,1);
			};
			CurrentHeroAbility->indexx=1;
		};
		if(p_card&&p_card->ProcTime!=0&&TrueTime<=newCardSetTime&&CurrentHeroAbility->CardRegeneration!=0) {
			Hint.Clear();
			Hint.Add("Load new card - ");
			Hint.Add(99-GetCoolDownProc());
			Hint.Add("%");
		}
		if (p_card&&newCardSetTime!=0&&TrueTime>newCardSetTime) {
			Hint=p_card->Hint;
			OnClk=true;
		}
	};
	return true;
}
bool ActiveCardPlace::CanApply()
{
	if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
	{
		CurrentHeroAbility=heroStorage;
		return p_card->CanApply(OB,heroStorage);
	}
	return false;
}
int ActiveCardPlace::GetCoolDownProc()
{
	if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
	{
		CurrentHeroAbility=heroStorage;
		return p_card->GetCoolDownProc(OB,heroStorage);
	}
	return 0;
}
int ActiveCardPlace::GetRadius()
{
	if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
	{
		CurrentHeroAbility=heroStorage;
		return p_card->GetRadius(OB,heroStorage);
	}
	return 0;
}
bool ActiveCardPlace::OnClick()
{
	bool rez=false;
	if(OnClk){
		if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
		{
			CurrentHeroAbility=heroStorage;
			MagicCardActive* activecard=p_card->GetMCActiveAbility();
			activecard->OB=OB;
			activecard->UnitIndex=OB->Index;
			activecard->timer2=AnimTime+p_card->LongTime.Get()*25*256;
			activecard->timer1=p_card->LongTime.Get();
			activecard->Radius=p_card->Radius.Get();
			activecard->Damage=p_card->Damage.Get();
			activecard->TypeAbil=1;
			rez=AddActiveUnitAbility(OB->Index,activecard);
			if (!rez)
			{
				return rez;
			}
		}
		//дальше - смена карты!...
		CardPlace* CP = (CardPlace*)GetA();
		ActiveCardPlace* AC = (ActiveCardPlace*)CP->GetActiveAbility();
		AC->SetA(CP);
		CP->CopyToActive(AC);
		AC->OB=OB;
		AC->UnitIndex=OB->Index;
/*		AC->FileID=oldFileID;
		AC->SpriteID=oldSpriteID;*/
		AC->heroStorage=heroStorage;
		AC->Visible=true;
		heroStorage->indexx=25*256;
		CardRef* CR = new CardRef();
		CR->cardplace.Set(p_card);
//		heroStorage->coloda.Add(CR);
		p_card=NULL;
		CR=NULL;
//		cardIndex=-1;
		rez=AddActiveUnitAbility(OB->Index,AC);
		if(rez){
			Restart=true;
		}
	}
	return rez;
}
bool ActiveCardPlace::OnRightClick()
{
	if(OnClk){
		if(OB&&!OB->Sdoxlo&&heroStorage)
		{
			CurrentHeroAbility=heroStorage;
			CardPlace* CP = (CardPlace*)GetA();
			ActiveCardPlace* AC = (ActiveCardPlace*)CP->GetActiveAbility();
			AC->SetA(CP);
			CP->CopyToActive(AC);
			AC->OB=OB;
			AC->UnitIndex=OB->Index;
/*			AC->FileID=oldFileID;
			AC->SpriteID=oldSpriteID;*/
			AC->heroStorage=heroStorage;
			AC->Visible=true;
			heroStorage->indexx=25*64;
//			heroStorage->colodaPointer--;
//			heroStorage->coloda.MoveElement(cardIndex,heroStorage->coloda.GetAmount()-1-cardIndex);
			CardRef* CR=new CardRef();;
			CR->cardplace.Set(p_card);
//			heroStorage->coloda.Add(CR);
			p_card=NULL;
			CR=NULL;
//			cardIndex=-1;
			bool rez=AddActiveUnitAbility(OB->Index,AC);
			if(rez){
				Restart=true;
			}
			return true;//activecard->OnRightClick();
		}
	}
	return false;
};
bool ActiveCardPlace::Execute(word TargetUnit, int x, int y, int z)
{
	if(OB&&!OB->Sdoxlo&&heroStorage/*&&activecard*/)
	{
		CurrentHeroAbility=heroStorage;
		return true;//activecard->Execute(TargetUnit, x, y, z);
	}
	return false;
}
bool ActiveCardPlace::isTarget()
{
	if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
	{
		CurrentHeroAbility=heroStorage;
		return p_card->isTarget(OB, heroStorage);
	}
	return false;
}
bool ActiveCardPlace::CanTarget(word TargetUnit, int x, int y, int z)
{
	if(OB&&!OB->Sdoxlo&&heroStorage/*&&activecard*/)
	{
		CurrentHeroAbility=heroStorage;
		return true;//activecard->CanTarget(TargetUnit, x, y, z);
	}
	return false;
}
bool ActiveCardPlace::isActive()
{
	if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
	{
		CurrentHeroAbility=heroStorage;
		return true;//activecard=p_card->GetMCActiveAbility();
	}
	return false;
}
bool ActiveCardPlace::DrawCursor(int x, int y)
{
	if(OB&&!OB->Sdoxlo&&heroStorage&&p_card)
	{
		int r = GetRadius();
		if(r>0)
		{
			int xx=x;
			int yy=y;
			/*
			Vector3D V=ScreenToWorldSpace(x,y);
			yy=V.y;
			xx=V.x;
			*/
			DrawSelPatch(xx,yy,p_card->CursorTexture,r,0x80FFFFFF);
			if(p_card->EnemyTarget||p_card->FriendlyTarget)
			{
				itr_UnitsInRadius.Create(xx,yy,r);
				if(OneObject* obb=itr_UnitsInRadius.Next())
				{
					if(p_card->EnemyTarget&&!(obb->NMask&OB->NMask))
					{
						obb->HighlightMask=4;
					}
					if(p_card->FriendlyTarget&&obb->NMask&OB->NMask)
					{
						obb->HighlightMask=4;
					}
				}
			}
		}
	}
	/*
	void DrawTerrainPatch( float worldX, float worldY, float width, float height, float rotation, 
	const Rct& uv, float ux, float uy,
	int texID, DWORD color = 0xFFFFFFFF, bool bAdditive = false );

	Vector3D V=ScreenToWorldSpace(x,y);
	ysy=V.y;
	xmx=V.x;

	bool DrawSelPatch(float x,float y,int Type,float ScaleX,float ScaleY,DWORD Color)

	byte HighlightMask;//bits of highliting : 1 - RED, 2 - GREEN, 4 - BLUE 8 - blinking effect

	bool DrawSelPatch(float x,float y,int Type,float Radius,DWORD Color)

	TEXTURE_CURSOR_TYPES

	*/
	return false;
}
//==================================================================================================================//