#include "stdheader.h"
#include "GameExtension.h"
#include ".\cext_Cheats.h"
#include "BoidsExtension.h"
#include ".\cext_VisualInterface.h"
#include "ai_scripts.h"
#include "WeaponSystemExtension.h"
#include "UnitAbilityExtension.h"
#include "BrigadeAbilityExtension.h"
#include "BattleShipAI.h"
#include "PlaceInAJob.h"
#include "Surface.h"


//////////////////////////////////////////////////////////////////////////
typedef ClassArray<GameExtension> ExtScope;
typedef DynArray<GameExtension*> ExtRef;
//
ExtRef ExtReferences[MAXVFUNC];
ExtScope* ExtList=NULL;
static bool RefInit[MAXVFUNC];
//
#define ITERATE_REF(idx,expr,argslist,args)\
void ext_##expr##argslist##{\
	ExtRef* ER=&ExtReferences[idx];\
	int N=ER->GetAmount();\
	for(int i=0;i<N;i++){\
		(*ER)[i]->expr##args;\
	}\
	if(!RefInit[idx]){\
		for(int i=N-1;i>=0;i--){\
			if(!(*ER)[i]->Check(idx)){\
				ER->Del(i,1);\
			}\
		}\
		RefInit[idx]=1;\
	}\
}
#define ITERATE_BOOL_REF(idx,expr,argslist,args)\
bool ext_##expr##argslist##{\
	ExtRef* ER=&ExtReferences[idx];\
	int N=ER->GetAmount();\
	bool R=0;\
	for(int i=0;i<N;i++)if(i<ER->GetAmount()){\
		R|=(*ER)[i]->expr##args;\
	}\
	if(!RefInit[idx]){\
		for(int i=N-1;i>=0;i--){\
			if(!(*ER)[i]->Check(idx)){\
				ER->Del(i,1);\
			}\
		}\
		RefInit[idx]=1;\
	}\
	return R;\
}
#define ITERATE_TBOOL_REF(idx,expr,argslist,args)\
bool ext_##expr##argslist##{\
	ExtRef* ER=&ExtReferences[idx];\
	int N=ER->GetAmount();\
	bool R=1;\
	for(int i=0;i<N;i++){\
		R&=(*ER)[i]->expr##args;\
	}\
	if(!RefInit[idx]){\
		for(int i=N-1;i>=0;i--){\
			if(!(*ER)[i]->Check(idx)){\
				ER->Del(i,1);\
			}\
		}\
		RefInit[idx]=1;\
	}\
	return R;\
	}
#define ITERATE_TYPE_REF(idx,type,expr,argslist,args)\
type ext_##expr##argslist##{\
	ExtRef* ER=&ExtReferences[idx];\
	int N=ER->GetAmount();\
	for(int i=0;i<N;i++){\
		(*ER)[i]->expr##args;\
	}\
	if(!RefInit[idx]){\
		for(int i=N-1;i>=0;i--){\
			if(!(*ER)[i]->Check(idx)){\
				ER->Del(i,1);\
			}\
		}\
		RefInit[idx]=1;\
	}\
	return 0;\
}

#define ITERATE_NOTHING(idx,type,expr,argslist,args)

#define GEN_EXT_CODE
#include "GameExtensionInternal.h"
DWORD GameExtension::GetCode(){
	DWORD p=0;
	int L=strlen(ExtensionName.str);
	for(int i=0;i<L;i++)p+=(i+20)*DWORD(ExtensionName.str[i]);
	return p;
}
void ext_OnGameSaving(xmlQuote& xml){
    xml.ClearAll();
	ExtRef* ER=&ExtReferences[14];
	int N=ER->GetAmount();
	xmlQuote* xTemp=new xmlQuote;	
	for(int i=0;i<N;i++){		
		if((*ER)[i]->OnGameSaving(*xTemp)){
			DWORD V=(*ER)[i]->GetCode();
			char c[32];
			sprintf(c,"%X",V);
            xTemp->SetQuoteName(c);
			xml.AddSubQuote(xTemp);
			xTemp=new xmlQuote;
		}
	}
	delete(xTemp);
	if(!RefInit[14]){
		for(int i=N-1;i>=0;i--){
			if(!(*ER)[i]->Check(14)){
				ER->Del(i,1);
			}
		}
		RefInit[14]=1;
	}	
}
void ext_OnGameLoading(xmlQuote& xml){
	///xml.ClearAll();
	ExtRef* ER=&ExtReferences[15];
	int N=ER->GetAmount();
	int NS=xml.GetNSubQuotes();	
	for(int i=0;i<N;i++){	
		DWORD Code=(*ER)[i]->GetCode();
		char cc[16];
		sprintf(cc,"%X",Code);
		for(int j=0;j<NS;j++){
			xmlQuote* xq=xml.GetSubQuote(j);
			const char* s=xq->GetQuoteName();
			if(s&&!strcmp(s,cc)){
                bool r=(*ER)[i]->OnGameLoading(*xq);
			}
		}
	}
	if(!RefInit[15]){
		for(int i=N-1;i>=0;i--){
			if(!(*ER)[i]->Check(15)){
				ER->Del(i,1);
			}
		}
		RefInit[15]=1;
	}
}
void ext_OnMapSaving(xmlQuote& xml){
	xml.ClearAll();
	ExtRef* ER=&ExtReferences[12];
	int N=ER->GetAmount();
	xmlQuote* xTemp=new xmlQuote;	
	for(int i=0;i<N;i++){		
		if((*ER)[i]->OnMapSaving(*xTemp)){
			DWORD V=(*ER)[i]->GetCode();
			char c[32];
			sprintf(c,"%X",V);
			xTemp->SetQuoteName(c);
			xml.AddSubQuote(xTemp);
			xTemp=new xmlQuote;
		}
	}
	delete(xTemp);
	if(!RefInit[12]){
		for(int i=N-1;i>=0;i--){
			if(!(*ER)[i]->Check(14)){
				ER->Del(i,1);
			}
		}
		RefInit[12]=1;
	}	
}
void ext_OnMapLoading(xmlQuote& xml){
	xml.ClearAll();
	ExtRef* ER=&ExtReferences[13];
	int N=ER->GetAmount();
	int NS=xml.GetNSubQuotes();	
	for(int i=0;i<N;i++){	
		DWORD Code=(*ER)[i]->GetCode();
		char cc[16];
		sprintf(cc,"%X",Code);
		for(int j=0;j<NS;j++){
			xmlQuote* xq=xml.GetSubQuote(j);
			const char* s=xq->GetQuoteName();
			if(s&&!strcmp(s,cc)){
				bool r=(*ER)[i]->OnMapLoading(*xq);
			}
		}
	}
	if(!RefInit[13]){
		for(int i=N-1;i>=0;i--){
			if(!(*ER)[i]->Check(13)){
				ER->Del(i,1);
			}
		}
		RefInit[13]=1;
	}
}
void InitExtensions(){
    if(!ExtList)ExtList=new ExtScope;
    memset(RefInit,0,sizeof RefInit);
}
void InstallExtension(GameExtension* Ext,const char* Name){
	InitExtensions();
	Ext->ExtensionName=Name;
	DWORD Code=Ext->GetCode();
	for(int i=0;i<ExtList->GetAmount();i++)if((*ExtList)[i]->GetCode()==Code){
		char cc[128];
		sprintf(cc,"WARNING! Extension <%s> installed twice! You should specify another name!",Name);
		MessageBox(NULL,cc,"Extension install error",0);
		assert(1);
	}
    ExtList->Add(Ext);
	for(int i=0;i<MAXVFUNC;i++){
        ExtReferences[i].Add(Ext);
	}
}
void UnInstallExtension(const char* Name){

}
///////////////////////////////////////////////////////////////////////
//////////////INSERT THERE YOUR EXTENSION INSTALLATION/////////////////
///////////////////////////////////////////////////////////////////////
//TEST SAMPLE
class TestExtension:public GameExtension{
public:
	bool Tormoz;
	TestExtension(){
		Tormoz=false;
	}
	virtual void ProcessingGame(){
		if(Tormoz){
			Sleep(200);
		}                
	}
	bool OnUnitDie(OneObject* Dead,OneObject* Killer){
        //UMS units
		if(Killer&&Dead&&Killer->NNUM!=Dead->NNUM){
			for(int i=0;i<6;i++){
				AddXRESRC(Killer->NNUM,i,Dead->newMons->UMS_Res[i]);
			}
		}
		return true;
	}
	virtual bool OnCheatEntering(const char* Cheat){
		if(!strcmp(Cheat,"fdl")){
			void testFileDialog();
			testFileDialog();
			//AssignHint1("mi tut vezde!!",200,0);
			return true;
		}
		if(!strncmp(Cheat,"genm ",5)){
			bool GenerateRandomMap(char* MapName);
			GenerateRandomMap((char*)(Cheat)+5);
		}
		if(!strcmp(Cheat,"normf")){
			void NormalizeFormations();
			NormalizeFormations();
		}
		if(!strcmp(Cheat,"noflag")){
			void NormalizeFlagbearers();
			NormalizeFlagbearers();
		}
		if(!strcmp(Cheat,"tormozz")){
			Tormoz=!Tormoz;
		}
		if(!strcmp(Cheat,"lzons")){
			void LoadZonesOnlyDialog();
			LoadZonesOnlyDialog();
		}
		if(!strcmp(Cheat,"delg17")){
			void DeleteRedundantG17();
			DeleteRedundantG17();
		}
		if(!strcmp(Cheat,"bldplane")){
			for(int i=0;i<MAXOBJECT;i++){
				OneObject* OB=Group[i];
				if(OB&&OB->NewBuilding){
					void CreatePlaneUnderBuilding(int xc,int yc,NewMonster* NM);
					CreatePlaneUnderBuilding(OB->RealX>>4,OB->RealY>>4,OB->newMons);
				}
			}			
		}
		if(!strcmp(Cheat,"testg17")){
			void TestAllG17();
			TestAllG17();
		}
		if(!strcmp(Cheat,"debugweap")){
			extern bool DebugWeapMode;
			DebugWeapMode=true;			
		}
		if(!strcmp(Cheat,"remroad")){
			void RemoveRoadsFromLockPoints();
			RemoveRoadsFromLockPoints();
		}
		if(strstr(Cheat,"frwater")){
			char cc1[32];
			int h1=-30;
			int h2=-80;
			int sc=32;
			sscanf(Cheat,"%s%d%d%d",cc1,&h1,&h2,&sc);
			for(int ix=0;ix<VertInLine;ix++){
				for(int iy=0;iy<MaxTH;iy++){
                    int xx=ix<<5;
					int yy=(iy<<5)-((ix&1)*16);
					int H=THMap[ix+iy*VertInLine];
					int GetFractalVal(int x,int y);
					int Hc=h1+((h2-h1)*GetFractalVal(xx*sc/32,yy*sc/32)/512);
					if(H<Hc)THMap[ix+iy*VertInLine]=Hc;
				}
			}
			void MakeAllDirtyGBUF();
			MakeAllDirtyGBUF();
			void ResetWaterHash();
			ResetWaterHash();
			void ResetGroundCache();
			ResetGroundCache();		
			void CreateCostPlaces();
			CreateCostPlaces();
			void CreateMiniMap();
		}
		if(!strcmp(Cheat,"bbridge")){
			bool UseGroupLikeBridgeBuilders(UnitsGroup* ugrp,int BridgeType,byte NI);
			UnitsGroup* UG=SCENINF.GetNewGroup();
			for(int i=0;i<ImNSL[MyNation];i++){
				word id=ImSelm[MyNation][i];
				word sn=ImSerN[MyNation][i];
				UG->AddNewUnit(Group[id]);
			}
			UseGroupLikeBridgeBuilders(UG,3,MyNation);
		}
		return false;
	}
};
#define MaxBInPt 8
word GetOneBld(int cell,int pos){
	if(cell>=0&&cell<VAL_MAXCIOFS&&pos<MaxBInPt){
        if(BLDList[cell])return BLDList[cell][pos];
	}
	return 0xFFFF;
}
void RefreshBiuldings(){
	void SetMonstersInCells();
	SetMonstersInCells();
	OneObject* OB;
	int ofst,ofst1,k;
	for(int i=0;i<MAXOBJECT;i++){
		OB=Group[i];
		if(OB){
			if(OB->NewBuilding){
				word MID;
				ofst=(OB->RealX>>11)+((OB->RealY>>11)<<VAL_SHFCX)+VAL_MAXCX+1;
				for(int pos=0;(MID=GetOneBld(ofst,pos))!=0xFFFF;pos++){
					if(MID!=OB->Index){
						OneObject* OBJ=Group[MID];
						if(OBJ&&Norma(OBJ->RealX-OB->RealX,OBJ->RealY-OB->RealY)<16*16){
							void EliminateBuilding(OneObject* OB);
							EliminateBuilding(OBJ);
                            Group[MID]=NULL;
						}
					}
				}				
			}
		}
	}
}
void NormalizeFlagbearers(){
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB&&OB->BrigadeID==0xFFFF){
			if(OB->Ref.General->UsualFormID!=0xFFFF){
                OneObject* TransformUnitType(OneObject* OB,int DestType);
				TransformUnitType(OB,OB->Ref.General->UsualFormID);
			}
		}
	}
}
void NormalizeFormations(){
	for(int ni=0;ni<8;ni++){
		City* CT=CITY+ni;
		for(int j=0;j<CT->NBrigs;j++){
			Brigade* BR=CT->Brigs+j;
			if(BR->Enabled&&BR->WarType){
                int fidx=BR->GetFormIndex();                
				bool GetOfficersType(byte NI, word UnitType, word &OffID, word &BarabanID, word &FlagID);
				word ofid,barid,flagid;
				if(GetOfficersType(ni,BR->MembID,ofid,barid,flagid)){
					GeneralObject* GO=NATIONS[ni].Mon[ofid];
					if(GO->OFCR){
						OfficerRecord* OFR=GO->OFCR;
						for(int j=0;j<OFR->NStroi;j++){
                            StroiDescription* SDS=OFR->SDES+j;
							for(int q=0;q<SDS->NAmount;q++)if(SDS->Amount[q]==BR->NMemb-NBPERSONAL){
								OrderDescription* ODS0=ElementaryOrders+BR->WarType-1;
								OrderDescription* ODS=ElementaryOrders+SDS->LocalID[q];
								if(ODS->GroupID!=0xFF&&ODS0->GroupID!=0xFF&&ODS->GroupID%3==ODS0->GroupID%3){									
									BR->WarType=SDS->LocalID[q]+1;
									int xx,yy;
									BR->GetCenter(&xx,&yy);
									BR->CreateOrderedPositions(xx*16,yy*16,BR->Direction);
									BR->ResortMembByPos();								
									BR->KeepPositions(0,128+16);
								}
							}
						}
					}
				}
			}
		}
	}
}
#include "CNavigationExtension.h"
#include "CCombineExtension.h"
#include "CRefreshExtension.h"
#include "CCopyExtension.h"
#include "CPasteExtension.h"
#include "CFillmodeExtension.h"
#include "CUndoExtension.h"

// Insert there
bool g_NewTerrainExtensionIsActive = false;
void IstalAllExtensions(){
	InitExtensions();
	bool CheckIfNewTerrain(void);
	if(CheckIfNewTerrain()) // New terrain extensions:
	{
		InstallExtension(&g_RefreshExtension, "Refresh");
		InstallExtension(&g_CopyExtension, "Copy");
		InstallExtension(&g_PasteExtension, "Paste");
		extern CCombineExtension g_CombineExtension;
		InstallExtension(&g_CombineExtension, "Combine");
		InstallExtension(&g_FillmodeExtension, "Fillmode: Solid / Wireframe");
		InstallExtension(&g_UndoExtension, "Undo");
	}
	InstallExtension(&g_NavigationExtension, "CNavigationExtension");

	InstallExtension(new TestExtension,"TestExtension");	
	// cheats	
	InstallExtension(new cext_Cheat_StartAI,"Start AI in editor for current nations");
	InstallExtension(new BoidsExtension,"Boids Process");
	InstallExtension(new WeaponSystemExtension,"WeaponSystemExtension");
	InstallExtension(new UnitAbilityExtension,"UnitAbilityExtension");
	InstallExtension(new BrigadeAbilityExtension,"BrigadeAbilityExtension");
	InstallExtension(new BattleShipAI,"BattleShipAI");
	InstallExtension(new PlaceInAJob,"PlaceInAJob");

	
	//Fantasy AI
	//void InstallFantasyAI();
	//InstallFantasyAI();

	// Vitya //  [3/16/2004] // Battle Editor
	void BE_InstallExtension();
	BE_InstallExtension();
	// Vitya //  [6/16/2004] // Alert Editor
	void Alert_InstallExtension();
	Alert_InstallExtension();
	// Vitya //  [9/28/2004] // Battle Editor 2
	void BE2_InstallExtension();
	BE2_InstallExtension();
	// vital
	InstallExtension(new cext_VisualInterface,	"Message when: defeat, victory or disconnect");
	InstallExtension(new cv_AI_ScriptExt,	"ai script");
	void RegisterBrigAbl();
	RegisterBrigAbl();
	void Install_cext_VeryStupid();
	Install_cext_VeryStupid();
	void InstallWallsSaver();
	InstallWallsSaver();
	void mai_mInit();
	mai_mInit();
	void RegisterVC_Saver();
	RegisterVC_Saver();
}
