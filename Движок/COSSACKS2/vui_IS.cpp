#include "stdheader.h"
#include "unitability.h"
#include ".\cvi_brigcreate.h"
//////////////////////////////////////////////////////////////////////////
void vgf_CreateInterfSynchroAction(){
	vui_InterfSynchro::E=ENUM.Get("ve_InterfSynchro");	
	reg_v_InterfSynchro<vui_IS_GBSel>("выделить здания SpecialSel");
	reg_v_InterfSynchro<vui_IS_AttackGround2>("Пушка: залп по точке");
	reg_v_InterfSynchro<vui_IS_EraseObject>("Удалить мгновенно юнита");	
	reg_v_InterfSynchro<vui_IS_AttackGround>("Пушка: залп");
	reg_v_InterfSynchro<vui_IS_FillCannon>("Пушка: пополнение");
	reg_v_InterfSynchro<vui_IS_MakeMaxStage>("Здание: заготовку под ключ");
	reg_v_InterfSynchro<vui_IS_Sel_SetActivity>("sel set ActivityState");
	reg_v_InterfSynchro<vui_IS_Abl_Target>("ability target execute");
	reg_v_InterfSynchro<vui_IS_ActAbl_LeftClick>("active ability execute");
	reg_v_InterfSynchro<vui_IS_ActAbl_LeftClick2>("active ability execute Fantasy");
	reg_v_InterfSynchro<vui_IS_SelFearDown>("selection go scary movie");
	reg_v_InterfSynchro<vui_IS_SelRTF>("selection redy to fire/not fire");
	reg_v_InterfSynchro<vui_IS_SelChargeAll>("selection charge all");	
	reg_v_InterfSynchro<vui_IS_BrigHoldPosit>("selection brigs hold position");	
	reg_v_InterfSynchro<vui_IS_SelRifle>("selection rifle state");
	// cvi_BrigCreate
	reg_v_InterfSynchro<cvi_IS_BC_Create>("BrigCreate create: from object");
	reg_v_InterfSynchro<vui_IS_AddWallSection>("Add wall section");
	reg_v_InterfSynchro<vui_IS_LeaveMineAll>("Leve mine all units");
	reg_v_InterfSynchro<vui_IS_Sel_MakeReformation>("REFORMA");
	reg_v_InterfSynchro<vui_IS_Sel_ClearOrders>("");
	reg_v_InterfSynchro<vui_IS_CancelAttack>("Cancel orders for cannon");
};
//////////////////////////////////////////////////////////////////////////
void GetUnitCost(byte NI,word NIndex,int* Cost,int x,int y);
DLLEXPORT void ClearSelection(byte Nat);
void AddUnitToSelected(byte NI,OneObject* OB);
void CopyReIm(byte NI);
void MakeStandGround(Brigade* BR);
DLLEXPORT int GetRND(int);
void ClearCannonAutoShot(OneObject* OB);
void StopCannonRotation(OneObject* OB);
//////////////////////////////////////////////////////////////////////////
void vui_IS_EraseObject::SynchroAction(){
	OneObject* OB=Group[Data.ObjID];
	if(OB&&OB->Serial==Data.ObjSN&&!OB->Sdoxlo){
		void EraseObject(OneObject* OB);
		EraseObject(OB);
	}
};
void vui_IS_AttackGround::SynchroAction(){
	OneObject* OB=Group[Data.ObjID];
	if(OB&&OB->Serial==Data.ObjSN&&!OB->Sdoxlo){
		int x=(OB->RealX>>4)+TCos[OB->RealDir];
		int y=(OB->RealY>>4)+TSin[OB->RealDir];
		OB->NewAttackPoint(x,y,16+128,0,1);
		// cossacks2 only
		ClearCannonAutoShot(OB);
		/*
		if(OB->ActiveAbility){
			int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
			for(int i=0;i<n;i++){
				ActiveUnitAbility* UA=OB->ActiveAbility->ActiveAbilities[n];
				if(UA&&UA->Visible){
					CannonAutoShotActive* CAS=dynamic_cast<CannonAutoShotActive*>(UA);
					if(CAS){
						CAS->On=false;
					}
				}
			}				
		}
		*/
	}
};
void vui_IS_AttackGround2::SynchroAction(){
	OneObject* OB=Group[Data.ObjID];
	if(OB&&OB->Serial==Data.ObjSN&&!OB->Sdoxlo){
		OB->NewAttackPoint(Data.x,Data.y,16+128,0,20000);
		// cossacks2 only
		ClearCannonAutoShot(OB);
	}
};
void vui_IS_CancelAttack::SynchroAction(){
	OneObject* OB=Group[Data.ObjID];
	if(OB&&OB->Serial==Data.ObjSN&&!OB->Sdoxlo){
		OB->ClearOrders();
		// cossacks2 only
		ClearCannonAutoShot(OB);
		StopCannonRotation(OB);
	}
};
void vui_IS_Sel_ClearOrders::SynchroAction(){	
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	DynArray<word> brList;
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]){
				OB->ClearOrders();
				OB->DestX=-1;
				if(OB->BrigadeID!=0xFFFF)
				{
					int n=brList.GetAmount();
					bool fnd=false;
					for(int j=0;j<n;j++)
					{
						if(brList[j]==OB->BrigadeID)
						{
							fnd=true;
							break;
						}
					}
					if(!fnd)
						brList.Add(OB->BrigadeID);
				}
			}
		}
	}
	int n=brList.GetAmount();
	for(int i=0;i<n;i++)
	{
		byte NI=GSets.CGame.cgi_NatRefTBL[Data.NI];
		Brigade* br=&NATIONS[NI].CITY->Brigs[brList[i]];
		if(br&&br->Enabled)
		{
			bool stay=true;			
			for(int j=0;j<br->NMemb;j++)
			{
				if(br->Memb[j]!=0xFFFF)
				{
					if(OneObject* ob=Group[br->Memb[j]])
					{
						if(ob->DestX!=-1 || ob->LocalOrder)stay=false;
						ob->ClearOrders();
						ob->DestX=-1;
					}
				}
			}
			int x,y;
			byte D;
			br->lastTimeGetCenter=-1;
			if( stay==false && br->GetCenter(&x,&y,&D) ){
				x+=TCos[D]>>4;
				y+=TSin[D]>>4;
				br->HumanGlobalSendTo(x,y,int(D)+2048,128+16,0);
			}else br->ClearNewBOrders();
			/*
			for(int j=0;j<br->NMemb;j++)
			{
			if(br->Memb[j]!=0xFFFF)
			{
			if(OneObject* ob=Group[br->Memb[j]])
			{
			br->posX[j]=ob->RealX/16;
			br->posY[j]=ob->RealY/16;
			ob->ClearOrders();
			ob->DestX=-1;
			}
			}
			}
			*/
		}
	}
}
void vui_IS_FillCannon::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]){
				void FillObjectByUnits(OneObject* OB);
				FillObjectByUnits(OB);
			}
		}
	}
}
void vui_IS_MakeMaxStage::SynchroAction(){
	OneObject* OB=Group[Data.Index];
	if(OB){
		int Cost[8];
		GetUnitCost(OB->NNUM,OB->NIndex,Cost,-1,-1);
		for(int i=0;i<6;i++)if(XRESRC(OB->NNUM,i)<Cost[i])return;
		for(int i=0;i<6;i++)AddXRESRC(OB->NNUM,i,-Cost[i]);
		for(int i=OB->Stage;i<OB->NStages;i++)OB->NextStage();
		OB->Ready=true;
	}
	// Price
}
void vui_IS_GBSel::SynchroAction(){
	return;
	byte NI=Data.NI;
	ClearSelection(NI);
	word* ID=NatList[NI];	// list units in nation
	int N=NtNUnits[NI];
	for(int i=0;i<N;i++,ID++){
		OneObject* OB=Group[*ID];
		if(OB&&OB->NewBuilding&&OB->newMons->SpecialSel&&OB->Stage==OB->NStages){
			AddUnitToSelected(NI,OB);
			//break;
		}
	}
	CopyReIm(NI);
}
void vui_IS_Sel_SetActivity::SynchroAction(){
	if(EngSettings.DontUseAgressiveState)return;
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]){
				OB->ActivityState=Data.ActivityState;
				OB->GroundState=OB->ActivityState==2;
				if(OB->NewState!=OB->GroundState)OB->NewState=OB->GroundState;
			}
		}
	}
}
void vui_IS_Sel_MakeReformation::SynchroAction(){
	void MakeReformation(byte NI,word BrigadeID,byte FormType);
	MakeReformation(Data.NI,Data.BrigadeID,Data.FormType);
}
void vui_IS_Abl_Target::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]&&/*OB->NIndex==Data.NIndex&&*/OB->ActiveAbility/*&&
				Data.AblID<OB->ActiveAbility->ActiveAbilities.GetAmount()*/){
				for(int i=0;i<OB->ActiveAbility->ActiveAbilities.GetAmount();i++){
					ActiveUnitAbility* UA=OB->ActiveAbility->ActiveAbilities[i];
					if(UA&&UA->Serial==Data.AblID){
						UA->Execute(Data.TargOB,Data.TargX,Data.TargY,-1);
					}
				}				
			}
			/*
			if(OB&&OB->Serial==SNS[i]&&OB->NIndex==Data.NIndex&&OB->newMons->Ability){
				UnitAbility* UA=OB->newMons->Ability->AbilitiesList[Data.AblID];
				//UA->Execute(MID,Data.TargOB,Data.TargX,Data.TargY,-1);
			}
			*/
		}
	}
}
void vui_IS_ActAbl_LeftClick::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]&&/*OB->NIndex==Data.NIndex&&*/OB->ActiveAbility&&
				Data.AblID<OB->ActiveAbility->ActiveAbilities.GetAmount()){
				ActiveUnitAbility* UA=OB->ActiveAbility->ActiveAbilities[Data.AblID];
				bool click=true;
				CannonAutoShotActive* ac=dynamic_cast<CannonAutoShotActive*>(UA);
				if(ac){
					if(Data.On!=ac->On){
						click=false;
					}
				}
				if(click){
					if(Data.Right){
						UA->OnRightClick();
					}else{
						UA->OnClick();					
					}
				}
			}
		}
	}
}
void vui_IS_ActAbl_LeftClick2::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]&&/*OB->NIndex==Data.NIndex&&*/OB->ActiveAbility/*&&
				Data.AblID<OB->ActiveAbility->ActiveAbilities.GetAmount()*/){
				for(int j=0;j<OB->ActiveAbility->ActiveAbilities.GetAmount();j++){
					ActiveUnitAbility* UA=OB->ActiveAbility->ActiveAbilities[j];
					if(UA&&UA->Serial==Data.AblSerial){
						if(Data.Right){
							UA->OnRightClick();
						}else{
							UA->OnClick();					
						}
						break;
					}
				}
			}
		}
	}
}
void vui_IS_SelFearDown::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]){
				OB->Morale=0;
				if(OB->BrigadeID!=0xFFFF){
					Brigade* BR=&(CITY[Data.NI].Brigs[OB->BrigadeID]);
					if(BR->Enabled&&BR->WarType!=0)
					{
						BR->Morale=0;
					}
				}
			}
		}
	}
}
void vui_IS_SelRTF::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]){
				if(OB->BrigadeID!=0xFFFF){
					Brigade* BR=&(CITY[Data.NI].Brigs[OB->BrigadeID]);
					if(BR->Enabled&&BR->WarType!=0){
						BR->AttEnm=Data.State;
					}
				}
				OB->NewState=Data.State;
			}
		}			
	}
}
void vui_IS_SelChargeAll::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]){
				OB->delay=0;
			}
		}
	}
}
void vui_IS_BrigHoldPosit::SynchroAction(){
	City* C=CITY+Data.NI;
	for(int i=0;i<C->NBrigs;i++){
		Brigade* BR=C->Brigs+i;
		if(BR->Enabled&&BR->WarType){
			bool sel=false;
			for(int bp=0;bp<BR->NMemb;bp++){
				word ID=BR->Memb[bp];
				if(ID!=0xFFFF){
					OneObject* OB=Group[ID];
					if(OB&&OB->Serial==BR->MembSN[bp]&&!OB->Sdoxlo&&OB->Selected){
						sel=true;
					}
				}
			}
			if(sel){
				BR->ClearBOrders();
				BR->BrigDelay=0;
				if(!BR->BrigDelay) MakeStandGround(BR);
			}
		}
	}
}
void vui_IS_SelRifle::SynchroAction(){
	int N=NSL[Data.NI];
	word* IDS=Selm[Data.NI];
	word* SNS=SerN[Data.NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]&&OB->RifleAttack!=Data.State){
				if(GetRND(100)<20) OB->RifleAttack=Data.State;
			}
		}
	}
}
void vui_IS_LeaveMineAll::SynchroAction(){
	OneObject* OB=Group[Data.ObjID];
	if(OB&&OB->Serial==Data.ObjSN){
		OB->LeaveMine(0xFFFF);
	}
}