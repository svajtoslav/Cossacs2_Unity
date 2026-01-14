#include "stdheader.h"
#include ".\cvi_HeroButtons.h"
//////////////////////////////////////////////////////////////////////////
cvi_HeroButtons vHeroButtons;
//DynArray<word> cva_Hero_Button::hIndex;
//DynArray<word> cva_Hero_Button::hSerial;
//////////////////////////////////////////////////////////////////////////

/*
inline bool cvi_HeroButtons::isHero(OneObject* OB){
	if(OB){
		int id=hIndex.find(OB->Index);
        return id>0&&OB->Serial==hSerial[id];
	}
	return false;
};
*/

//
void cva_Hero_Button::SetFrameState(SimpleDialog* SD){
	SD->Visible=false;
	if(!vHeroButtons.GetVisible()) return;	
	ParentFrame* PF=(ParentFrame*)SD;	
	SD->ID=0xFFFF;
	int n=vHeroButtons.hIndex.GetAmount();
	int j=0;
	for(int i=0;i<n;i++){
		word id=vHeroButtons.hIndex[i];
		word sn=vHeroButtons.hSerial[i];		
		OneObject* OB=Group[id];
		if(OB&&OB->Serial==sn&&!OB->Sdoxlo){
			if(OB->NNUM==GSets.CGame.cgi_NatRefTBL[MyNation]){
				if(j==Index){
					ActiveRomeHero* ARH=NULL;
					if(OB->ActiveAbility){
						for(int a=0;a<OB->ActiveAbility->ActiveAbilities.GetAmount();a++){
							ARH=dynamic_cast<ActiveRomeHero*>(OB->ActiveAbility->ActiveAbilities[a]);
							if(ARH) break;
						}
					}
					SD->Visible=true;
					SD->ID=OB->Index;
					SD->UserParam=OB->Serial;
					bool pain=vHeroButtons.hDamageTime[i]>TrueTime&&(TrueTime&512);
					if(OB->Sdoxlo){
						SD->Enabled=false;
						PF->Diffuse=0x80FFFFFF;
					}else{
						SD->Enabled=true;						
						PF->Diffuse=pain?0xFFFF0000:0xFFFFFFFF;
					}
					GPPicture* GP=Pic.Get();
					if(GP){
						NewMonster* NM=OB->newMons;
						if(NM->MinIconFile!=0xFFFF){
							GP->SetFileID(&NM->MinIconFile);
							GP->SetSpriteID(NM->MinIconIndex);
						}else{
							GP->SetFileID(&NM->IconFileID);
							GP->SetSpriteID(NM->IconID);
						}
					}
					if(OB->newMons->LongMessage[0]){
						SD->SetHint(OB->newMons->LongMessage);
					}else{
						SD->SetHint(OB->newMons->Message);
					}
					if(SD->DSS.GetAmount()>1){
						ProgressBar* PB=dynamic_cast<ProgressBar*>(SD->DSS[1]);
						if(PB){
							PB->Value=OB->Life;
							PB->MaxValue=OB->MaxLife;
							PB->BackColor=pain?0xFFFF0000:0xFFFFFFFF;
							PB->ProgressColor=OB->Life<OB->MaxLife/4?0xFFFF0000:0xFFFFFFFF;
						}
					}
					VitButton* vbFL=FreeLevel.Get();
					if(vbFL){
						vbFL->Visible=false;
						if(ARH){
							int fl=ARH->GetAmountFreeLevels();
							if(fl){
								vbFL->Visible=true;
								char txt[32];
								itoa(fl,txt,10);
								vbFL->SetMessage(txt);
							}
						}
					}
					break;
				}
				j++;
			}
		}else{
			vHeroButtons.hIndex.Del(i,1);
			vHeroButtons.hSerial.Del(i,1);
			i--;
			n--;
		}
	}
};
bool cva_Hero_Button::LeftClick(SimpleDialog* SD){	
	if(SD->ID!=0xFFFF){
		OneObject* OB=Group[SD->ID];
		if(OB&&OB->Serial==SD->UserParam&&!OB->Sdoxlo){
			byte NI=MyNation;
			if(OB->ImSelected){
				SetScreenCenterToXY(OB->RealX>>4,OB->RealY>>4);
				void SetCentralUnit(OneObject* OB);
				SetCentralUnit(OB);
				if(!(GetKeyState(VK_SHIFT)&0x8000)&&ImNSL[NI]>1){
					byte NI=MyNation;
					PrepareToImSelection(NI);					
					ImClearSelection(NI);					
					AddUnitToImSelected(NI,OB);
					FinalizeImSelection(NI);			
				}
			}else{				
				PrepareToImSelection(NI);
				if(!(GetKeyState(VK_SHIFT)&0x8000)){
					ImClearSelection(NI);
				}
				AddUnitToImSelected(NI,OB);
				FinalizeImSelection(NI);			
			}
		}
	}
	return true;
};