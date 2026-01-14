#include "stdheader.h"
#include "ua_Item.h"
//
#include "UnitsInterface.h"
//==================================================================================================================//
//
void ProduceObjLink(OneObject* OBJ);
bool AddActiveUnitAbility(word Unit, ActiveUnitAbility* AUA);
void EraseObject(OneObject* OB);
DLLEXPORT int GetRND(int Max);
//
//==================================================================================================================//
bool NewItem::OnUnitBirth(OneObject* Newbie){
	if(Newbie){
		OneItem* NA = new OneItem;
		NA->SetA(this);		
		CopyToActive(NA);
		return AddActiveUnitAbility(Newbie->Index,NA);
	}
	return false;
};
bool NewMagazine::OnUnitBirth(OneObject* Newbie){
	if(Newbie){
		OneMagazine* NA = new OneMagazine;
		NA->SetA(this);		
		CopyToActive(NA);
		return AddActiveUnitAbility(Newbie->Index,NA);
	}
	return false;
};
//
NewItem* DetectItem(word NIndex){
	NewMonster* NM=NATIONS->Mon[NIndex]->newMons;
	if(NM->Ability){
		NM->Ability->Init(NM);
		int n=NM->Ability->AbilitiesList.GetAmount();
		for(int i=0;i<n;i++){
			NewItem* NI=dynamic_cast<NewItem*>(NM->Ability->AbilitiesList[i]);
			if(NI){
				return NI;
			}
		}
	}
	return NULL;
}
HeroVariableStorage* DetectHero(OneObject* OB){
	if(OB&&OB->ActiveAbility){
		int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<n;i++){
			HeroVariableStorage* HA=dynamic_cast<HeroVariableStorage*>(OB->ActiveAbility->ActiveAbilities[i]);
			if(HA){
				return HA;
			}
		}
	}
	return NULL;
}
bool OneMagazine::Process(OneObject* OB){
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	if(/*OB->NNUM==NI&&*/(OB->ImSelected&(1<<NI))){
		// найти героя в радиусе магазина
		OneObject* Hero=Group[HeroID];
		if(HeroID==0xFFFF||HeroSN!=Hero->Serial||!DetectHero(Hero)||Norma(OB->RealX-Hero->RealX,OB->RealY-Hero->RealY)>16*1000){
			Hero=NULL;
			HeroID=0xFFFF;
			itr_UnitsInRadius.Create(OB->RealX>>4,OB->RealY>>4,1000);
			while(OneObject* O=itr_UnitsInRadius.Next()){
				if(DetectHero(O)){
					Hero=O;
					HeroID=O->Index;
					HeroSN=O->Serial;
					break;
				}
			}
		}
		// установить возможность постройки предметов
		Nation* Nat=NATIONS+NI;
		//City* CT=CITY+NI;
		//word NIndex=SP->NIndex;
		//
		int NBuild=Nat->PACount[OB->NIndex];
		word* Build=Nat->PAble[OB->NIndex];
		for(int i=0;i<NBuild;i++,Build++){
			if(DetectItem(*Build)){
				GeneralObject* IGO=Nat->Mon[*Build];
				IGO->Enabled=Hero;
			}
		}
		// очистка инвентаря
		int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<n;i++){
			ActiveUnitAbility* AUA=OB->ActiveAbility->ActiveAbilities[i];
			if(AUA&&AUA->TypeAbil==4){
				//OB->ActiveAbility->ActiveAbilities.DelElement(i);
				//i--;
				//n--;				
				((OneItem*)AUA)->Deleted=true;
			}
		}
		if(Hero){
			// поиск строящегося предмета в приказах здания
			if(OB->LocalOrder&&OB->LocalOrder->DoLink==&ProduceObjLink){
				word NIndex=OB->LocalOrder->info.Produce.ObjIndex;
				NewItem* NI=DetectItem(NIndex);
				if(NI){
					// delete order
					OB->DeleteLastOrder();
					// add one item
					OneItem* I=new OneItem;
					//
					I->SetA(NI);
					NI->CopyToActive(I);				
					AddActiveUnitAbility(Hero->Index,I);
					//
					I->fst=false;
					I->NIndex=NIndex;
				}
			}
			// отображение предметов в инвентаре героя
			int n=Hero->ActiveAbility->ActiveAbilities.GetAmount();
			for(int i=0;i<n;i++){
				ActiveUnitAbility* AUA=Hero->ActiveAbility->ActiveAbilities[i];
				if(AUA&&AUA->TypeAbil==4){
					OneItem* I=new OneItem;
					AUA->Copy(I);
					AddActiveUnitAbility(OB->Index,I);
				}
			}
		}
	}
	return true;
};
//
bool OneItem::Process(){
	if(Deleted){
		return false;
	}
	if(!Droped){		
		MagicCardActive::Process();
		return true;
	}
	return false;
};
bool OneItem::OnRightClick(){
	const int r=50*16;
	int x=OB->RealX+r-GetRND(r*2);
	int y=OB->RealY+r-GetRND(r*2);
	if(NATIONS[OB->NNUM].CreateNewMonsterAt(x,y,NIndex,true)){
		Droped=true;
	}	
	return true;
};
//
bool ItemPickUp(word ItemID){
	if(ItemID!=0xFFFF&&OIS.SelPoint.GetAmount()){
		vui_SelPoint* SP=OIS.GetLastSelPoint();
		if(SP&&DetectHero(SP->OB)){
			OneObject* Hero=SP->OB;
			OneObject* IOB=Group[ItemID];
			NewItem* NI=DetectItem(IOB->NIndex);
			if(NI){
				if(Norma(IOB->RealX-Hero->RealX,IOB->RealY-Hero->RealY)<150*16){
					// delete object
					EraseObject(IOB);
					// add one item
					OneItem* I=new OneItem;
					//
					I->SetA(NI);
					NI->CopyToActive(I);				
					AddActiveUnitAbility(Hero->Index,I);
					//
					I->fst=false;
					I->NIndex=IOB->NIndex;
					return true;
				}
			}
		}
	}
	return false;
}