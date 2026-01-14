#pragma once
#include "stdheader.h"
#include "UnitAbility.h"
#include "vui_Action.h"
#include "vui_InterfSynchro.h"
#include ".\cvi_HeroButtons.h"
//////////////////////////////////////////////////////////////////////////
extern int TrueTime;
//////////////////////////////////////////////////////////////////////////

class cvi_HeroButtons: public BaseClass
{
private:
	bool Visible;
public:
	//static DynArray<word> hIndex;
	//static DynArray<word> hSerial;
	LinearArray<word,_WORD> hIndex;
	LinearArray<word,_WORD> hSerial;
	//DynArray<word> hOrder;
	LinearArray<int,_int> hDamageTime;
	//
	SAVE(cvi_HeroButtons){
		REG_MEMBER(_bool,Visible);
		REG_AUTO(hIndex);
		REG_AUTO(hSerial);
		REG_AUTO(hDamageTime);		
	}ENDSAVE;
	//
	cvi_HeroButtons(){
		Visible=true;
	}	
	void SetVisible(bool State){
		Visible=State;
	}
	bool GetVisible(){
		return Visible;
	}
	bool isHero(OneObject* OB){
		if(OB){
			int id=hIndex.find(OB->Index);
			return id>=0&&OB->Serial==hSerial[id];
		}
		return false;
	};	
	void checkDamage(OneObject* OB){
		if(OB){
			int id=hIndex.find(OB->Index);
			if(id>=0&&OB->Serial==hSerial[id]){
				hDamageTime[id]=TrueTime+2000;
			}
		}
	};
	//DynArray<int> Oredrs[8];
	bool Add(OneObject* OB){		
		UnitActiveAbilityList* AList=OB->ActiveAbility;
		if(AList){ //&&OB->NNUM==GSets.CGame.cgi_NatRefTBL[MyNation]
			ClassArray<ActiveUnitAbility>* AL=&AList->ActiveAbilities;
			for(int i=0;i<AL->GetAmount();i++){
				ActiveUnitAbility* A=(*AL)[i];
				if(A->IsHero()){
					hIndex.Add(OB->Index);
					hSerial.Add(OB->Serial);
					hDamageTime.Add(0);
					//int nid=Orders[OB->NNUM].find(OB->NIndex);
					//if(nid<0){
					//}
					return true;					
				}
			}
		}
		return false;
	}
	int GetAmount(byte NI){
		int n=hIndex.GetAmount();
		int j=0;
		for(int i=0;i<n;i++){
			word id=hIndex[i];
			word sn=hSerial[i];
			OneObject* OB=Group[id];
			//byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
			if(OB&&OB->Serial==sn&&!OB->Sdoxlo&&OB->NNUM==NI){
				j++;
			}
		}
		return j;
	}
	OneObject* GetObject(byte NI, int ID){
		int n=hIndex.GetAmount();
		int j=0;
		for(int i=0;i<n;i++){
			word id=hIndex[i];
			word sn=hSerial[i];
			OneObject* OB=Group[id];			
			if(OB&&OB->Serial==sn&&!OB->Sdoxlo&&OB->NNUM==NI){
				if(ID==j){
					return OB;
				}
				j++;
			}
		}
		return NULL;
	}
	void Clear(){
		SetVisible(true);
		hIndex.Clear();
		hSerial.Clear();
		hDamageTime.Clear();
	};
};

// vui_Actions
regAc(cva_Hero_Button, vfS vfL
	int Index;
	ClassRef<GPPicture> Pic;
	ClassRef<VitButton> FreeLevel,
	REG_MEMBER(_int,Index);
	REG_AUTO(Pic);
	REG_AUTO(FreeLevel);	
);

//////////////////////////////////////////////////////////////////////////
void SetScreenCenterToXY(int x, int y);
void ImClearSelection(byte Nat);
void AddUnitToImSelected(byte NI,OneObject* OB);
void PrepareToImSelection(byte NI);
void FinalizeImSelection(byte NI);
//////////////////////////////////////////////////////////////////////////
extern cvi_HeroButtons vHeroButtons;