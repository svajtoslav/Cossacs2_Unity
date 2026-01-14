#include "stdheader.h"
#include "UnitsInterface.h"
#include ".\cvi_HeroButtons.h"
#define DIALOGS_USER
//////////////////////////////////////////////////////////////////////////
bool GetPushkaChargeState(OneObject*,int&,int&);
extern byte PlayGameMode;
extern bool EditMapMode;
extern int RealLx;
extern int RealLy;
void CmdChangeNPID(byte,word);
int	GetAmount(word ID);
int GetProgress(word,int*);
extern bool BuildMode;
void GetBrigadeParams(Brigade*,BrigParam*);
DLLEXPORT int GetCurrentUnits(byte NI);
DLLEXPORT int GetMaxUnits(byte NI);
void NewAttackPointLink(OneObject* OBJ);
//////////////////////////////////////////////////////////////////////////

//initial params
int FI_X0=0;
int FI_Y0=0;
int FI_File=-1;
int FI_WeapFile=-1;
int FI_ResFile=-1;
int FI_PortBackFile=-1;
int FI_IFile=-1;
int FI_Awards=-1;
int FI_PortretID=0;
int FI_PortretLX;
int FI_PortretLY;
int FI_RifleID=2;
int FI_RifleLX;
int FI_RifleLY;
int FI_SabreID=5;
int FI_SabreLX;
int FI_SabreLY;
int FI_GrenadeX;
int FI_GrenadeID=2;
int FI_GrenadeY;
int FI_GrenadeLX;
int FI_GrenadeLY;
int FI_IconSizeX=42;
int FI_IconSizeY=42;
#define FI_IconLX FI_IconSizeX
#define FI_IconLY FI_IconSizeY

char* FI_NetralName=NULL;

int FI_RifleX;
int FI_RifleY;
int FI_SabreX;
int FI_SabreY;
int FI_BoardX;
int FI_BoardY;

extern City CITY[8];
extern int FillF_Pos;
extern int FILLFORM_ICON;

void FILLFORM(int i);
void DelAbil(int i);
void UseAbil(int i);
void ThrowGrenade(int i);
void ENBLINE(int i);
void REFORMA(int i);
void SETATTSTATE_Pro(int i);
int LastNI,LastBID;
bool UseGrenades=0;

#include "Parser.h"
void ImSelBrigade(byte NI,byte Type,byte BNAT,int ID);
void SelBrigade(byte NI,byte Type,byte BNat,int ID);
extern OneTrigger VideoSeq;
extern OneTrigger VidOfSeq;
void ProcessVideoForBrigade(OneTrigger* OT,byte NI,word BrigID,int Action,int BackGP,int BackSprite,int VideoX,int VideoY, int vdx, int vdy, int PlayerID);

bool ReadOnly=0;
bool SetAtt(SimpleDialog* SD){
	if(ReadOnly)return false;
	SETATTSTATE_Pro(SD->UserParam);
	Lpressed=0;
	switch(SD->UserParam){
		case 1:
			ACT(5);//MELEE
			break;
		case 129:
			ACT(3);//RIFLE
			break;
		case 128:
			ACT(4);//NORIFLE
			break;
	};
	return 0;
}
bool MassSetAtt(SimpleDialog* SD){
	SETATTSTATE_Pro(SD->UserParam);
	Lpressed=0;
	return 0;
}
void ENBLINE(int i);
bool ShotLine(SimpleDialog* SD){
	if(ReadOnly)return false;
	ENBLINE(SD->UserParam);
	Lpressed=0;
	int V=SD->UserParam;
	int L=V&7;
	if(V>=8192){
		ACT(7+L);
	}else{
		ACT(10+L);
	}
	return 0;
}

void ThrowGrenade(int i);
bool Grenade(SimpleDialog* SD){
	if(ReadOnly)return false;
	ThrowGrenade(SD->UserParam);
	return 0;
}
void REFORMA(int i);
bool Reform(SimpleDialog* SD){
	if(ReadOnly)return false;
	REFORMA(SD->UserParam);
	Lpressed=0;
	return 0;
}
void FILLFORM(int i);
bool FillForm(SimpleDialog* SD){
	if(!ReadOnly&&SD->UserParam!=0xFFFF){
		FILLFORM(SD->UserParam);
	}
	Lpressed=0;
	return 0;
}
void FREEORD(int i);
bool FreeForm(SimpleDialog* SD){
	if(ReadOnly)return false;
	FREEORD(SD->UserParam);
	Lpressed=0;
	return 0;
}
void MSTANDGR(int i);
bool StandGround(SimpleDialog* SD){
	if(ReadOnly)return false;
	MSTANDGR(SD->UserParam);
	Lpressed=0;
	return 0;
}
bool NoClick(SimpleDialog* SD){
	if(ReadOnly)return false;
	Lpressed=0;
	return 0;
}
//void CmdChooseSelBrig(byte NI,word ID);
bool ClickOnBrig(SimpleDialog* SD){
	if(ReadOnly)return false;	
	if(GetKeyState(VK_MENU)&0x6000){
		if(SD->Diffuse==0xFFFFFFFF) SD->Diffuse=0xFF808080;
		else SD->Diffuse=0xFFFFFFFF;
	}else
	if(SD->UserParam<0xFFFF){
		if(GetKeyState(VK_SHIFT)&0x6000) CmdChooseUnSelBrig(MyNation,SD->UserParam);
			else CmdChooseSelBrig(MyNation,SD->UserParam);			
	}else{
		word id=SD->UserParam>>16;
		if(GetKeyState(VK_SHIFT)&0x6000) CmdChooseUnSelType(MyNation,id);
			else CmdChooseSelType(MyNation,id);
	}
	Lpressed=0;
	return 0;
}
void UseAbil(int i);
void DelAbil(int i);
bool AblMouseOver(SimpleDialog* SD){
	if(ReadOnly)return false;
	if(Lpressed){
		UseAbil(SD->UserParam);
        Lpressed=0;
	}
	if(Rpressed){
		DelAbil(SD->UserParam);
		Rpressed=0;
	}
	return 0;
}
#define HINT(x,y)\
{\
	static const char* y=GetTextByID(#y);\
	x->Hint=(char*)y;\
}

DialogsSystem SL_Interface;
DWORD CurSelHash=-1;
word CheckIfFormationInterface(byte& NI){
	word BrigID=0xFFFF;
	byte Nat;
	int N=ImNSL[NI];
	word* IDS=ImSelm[NI];
	word* SNS=ImSerN[NI];
	for(int i=0;i<N;i++){
		word MID=IDS[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB&&OB->Serial==SNS[i]&&!OB->Sdoxlo){
				if(OB->BrigadeID==0xFFFF)return 0xFFFF;
				else{
					if(BrigID==0xFFFF){
						BrigID=OB->BrigadeID;
						NI=OB->NNUM;
					}else if(BrigID!=OB->BrigadeID)return 0xFFFF;
				}
			}
		}
	}
	return BrigID;
}
extern int RealLy;
void GetBrigadeParams(Brigade* BR,BrigParam* BP){
	if(BR->WarType==0||!BR->Enabled) return;
	int L=0;
	int N=0;
	int NS=0;
	int T=0;
	BP->RifleAttack=0;
	int Delay=0;
	int MaxDelay=0;
	for(int i=NBPERSONAL;i<BR->NMemb;i++){
		word MID=BR->Memb[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB){
				L+=OB->Life;
				N++;
				if(!OB->delay)NS++;
				T+=OB->GetTired/1000;
				if(OB->RifleAttack)BP->RifleAttack=1;
				Delay+=OB->delay;
				MaxDelay+=OB->MaxDelay;
			}
		}
	}
	if(N){
		L/=N;
		T/=N;
		Delay/=N;
		MaxDelay/=N;
	}
	BP->NLiveMembers=N;
	BP->Life=L;
	if(BR->Morale>=0) BP->Morale=BR->Morale/10000;
		else BP->Morale=0;	
	BP->MaxMorale=BR->MaxMorale/10000;
	BP->MaxLife=NATIONS[BR->CT->NI].Mon[BR->MembID]->MoreCharacter->Life;
	BP->NShots=NS;
	BP->NKills=BR->GetBrigExp();
	BP->NGrenades=BR->NGrenades;
	BP->Tiring=T;
	BP->ReadyPercent=MaxDelay?100*(MaxDelay-Delay)/MaxDelay:100;
}
void ShowAString(int x,int y,RLCFont* F,byte Align,char* s,...){
	char ach[256];
    va_list va;

    va_start( va, s );
    vsprintf ( ach, s, va );   
    va_end( va );

	if(Align==1){
		ShowString(x-GetRLCStrWidth(ach,F)/2,y,ach,F);
	}else
	if(Align==2){
		ShowString(x-GetRLCStrWidth(ach,F),y,ach,F);
	}else ShowString(x,y,ach,F);
}
void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf, bool Horizontal);
void ShowAStringEx(int x,int y,RLCFont* F,byte Align,bool Horizontal,char* s,...){
	char ach[256];
    va_list va;

    va_start( va, s );
    vsprintf ( ach, s, va );   
    va_end( va );

	if(Align==1){
		if(Horizontal){
			x-=GetRLCStrWidth(ach,F)/2;
		}else{
			y+=GetRLCStrWidth(ach,F)/2;
		}
		ShowStringEx(x,y,ach,F,Horizontal);
	}else
	if(Align==2){
		if(Horizontal){
			x-=GetRLCStrWidth(ach,F);
		}else{
			y+=GetRLCStrWidth(ach,F);
		}
		ShowStringEx(x,y,ach,F,Horizontal);
	}else ShowStringEx(x,y,ach,F,Horizontal);
}
void LineRGB(int x,int y,int x1,int y1,DWORD Color);
int scl(int r,int g,int b,int s){
	return 0xFF000000+(((r*s)/100)<<16)+(((g*s)/100)<<8)+((b*s)/100);
}
void DrawVertLine(int x,int y,int L,int R,int G,int B){
	LineRGB(x-1,y,x-1,y-L,scl(R,G,B,60));
	LineRGB(x,y,x,y-L,scl(R,G,B,100));
	LineRGB(x+1,y,x+1,y-L,scl(R,G,B,60));
}
void DrawHorLine(int x,int y,int L,int R,int G,int B){
	LineRGB(x,y-3,x+L,y-3,scl(R,G,B,80));
	LineRGB(x,y-2,x+L,y-2,scl(R,G,B,100));
	LineRGB(x,y-1,x+L,y-1,scl(R,G,B,100));
	LineRGB(x,y  ,x+L,y  ,scl(R,G,B,100));
	LineRGB(x,y+1,x+L,y+1,scl(R,G,B,100));
	LineRGB(x,y+2,x+L,y+2,scl(R,G,B,100));
	LineRGB(x,y+3,x+L,y+3,scl(R,G,B,80));
}
extern byte   WeaponFlags[32];
// global var
void InitGlobalFIVar(){
	if(FI_File==-1){
		FI_File=GPS.PreLoadGPImage("Interf3\\FormInterface");
		if(FI_File!=-1){
			FI_IFile=GPS.PreLoadGPImage("Interf3\\f_icons");
			FI_Awards=GPS.PreLoadGPImage("Interf3\\awards");
			FI_PortretLX=GPS.GetGPWidth(FI_File,FI_PortretID);
			FI_PortretLY=GPS.GetGPHeight(FI_File,FI_PortretID);
            FI_RifleLX=68;//GPS.GetGPWidth(FI_File,FI_RifleID);
			FI_RifleLY=GPS.GetGPHeight(FI_File,FI_RifleID);
			FI_SabreLX=GPS.GetGPWidth(FI_File,FI_SabreID);
			FI_SabreLY=GPS.GetGPHeight(FI_File,FI_SabreID);
			FI_WeapFile=GPS.PreLoadGPImage("Interf3\\BigWeapon");
			FI_ResFile=GPS.PreLoadGPImage("Interf3\\ResPanel");
			FI_PortBackFile=GPS.PreLoadGPImage("Interf3\\PortBackBranch");
			FI_NetralName=GetTextByID("NetralSettlementName");
		}else{
			CEXPORT void VitalError(char*);
			VitalError("UnitsInterface.cpp: InitGlobalFIVar");
		}
	}	
}

extern OISelection OIS;
void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf, bool Horizontal);
void ShowAStringEx(int x,int y,RLCFont* F,byte Align,bool Horizontal,char* s,...);

// return true if mouseover 
CEXPORT void SetTopSelBrigID(word);
bool ShowOneSelBrig(DialogsSystem* DS, byte NI, DWORD BrID, int dx, int CurSelID, int ID){

	bool Enable = CurSelID==ID;

	Brigade* BR=NULL;
	GeneralObject* GO;
	if(BrID>=0xFFFF){
		GO=NATIONS[NI].Mon[BrID>>16];
	}else{
		BR=CITY[NI].Brigs+BrID;
		GO=NATIONS[NI].Mon[BR->MembID];
	}
	NewMonster* NM=GO->newMons;
	AdvCharacter* ADC=GO->MoreCharacter;
	UseGrenades=GO->newMons->MaxGrenadesInFormation;

	RLCFont* Font=&SmallYellowFont;

	GPPicture* GPP=(GPPicture*)DS->Find(1024);
	GPPicture* GBI=(GPPicture*)DS->Find(1025);
	GPPicture* GPback=(GPPicture*)DS->Find(1026);
	GPPicture* GPbranch=(GPPicture*)DS->Find(1027);
	GPPicture* GPbackSpr=(GPPicture*)DS->Find(1028);

	if(!GBI){
		FI_RifleX=FI_PortretLX+FI_SabreLX;
		FI_RifleY=-FI_RifleLY;
		FI_SabreX=FI_PortretLX;
		FI_SabreY=-FI_SabreLY;
		if(UseGrenades){
			FI_GrenadeX=FI_RifleX+FI_RifleLX;
			FI_GrenadeLX=GPS.GetGPWidth(FI_File,FI_GrenadeID);
			FI_GrenadeLY=GPS.GetGPHeight(FI_File,FI_GrenadeID);
			FI_GrenadeY=-FI_GrenadeLY;
		}else FI_GrenadeLX=0;
		FI_BoardX=FI_PortretLX+FI_SabreLX+FI_RifleLX+FI_GrenadeLX;
		FI_BoardY=-FI_IconLY*3;

		//DS.CloseDialogs();

		DS->SetHintStyle(0,1,1,30,0,0,FI_PortretLX+4,RealLy-FI_PortretLY+17,400,FI_PortretLY-FI_RifleLY-30,&SmallWhiteFont,0x90404040,0);
		
		DS->BaseX=FI_X0;
		DS->BaseY=RealLy-FI_Y0-1;
		
		GPP=DS->addGPPicture(NULL,dx,-FI_PortretLY,FI_File,FI_PortretID);
		GPP->AssignID(1024);
		GPP->ShapeFileID=FI_File;
		GPP->ShapeSpriteID=FI_PortretID;
		
		GPbackSpr=DS->addGPPicture(NULL,0,0,FI_File,0);
		GPbackSpr->AssignID(1028);
		GPbackSpr->Visible=0;

		GPback=DS->addGPPicture(NULL,0,0,FI_File,34);
		GPback->AssignID(1026);
		GPback->Visible=0;

		GPbranch=DS->addGPPicture(NULL,0,0,FI_PortBackFile,0);
		GPbranch->AssignID(1027);
		GPbranch->Visible=0;

		GBI=DS->addGPPicture(NULL,10+dx,-FI_PortretLY+43,0,0);
		GBI->AssignID(1025);
		GBI->Visible=0;

		// Messages
		//DS->addTextButton(NULL,CenterX+dx,-FI_PortretLY+20,NM->Message,Font,Font,Font,1);
		
		//hints subsystem		
		/*
		SimpleDialog* H1=DS->addViewPort(0,-FI_PortretLY+20,20,FI_PortretLY-40);
		HINT(H1,AVERAGE_LIFE_OF_FORMATION);
		H1=DS->addViewPort(FI_PortretLX-20,-FI_PortretLY+20,20,FI_PortretLY-40);
		HINT(H1,TIRING_OF_FORMATION);
		H1=DS->addViewPort(FI_PortretLX/2-20,-FI_PortretLY,40,16);
		HINT(H1,AMOUNT_OF_UNITS_IN_FORMATION);
		H1=DS->addViewPort(FI_PortretLX/2-20,-35,40,16);
		HINT(H1,MORALE_OF_FORMATION);
		H1=DS->addViewPort(20,-22,FI_PortretLX-40,20);
		HINT(H1,STRIPE_OF_MORALE);
		H1=DS->addViewPort(FI_RifleX+FI_RifleLX/2-20,FI_RifleY,40,16);
		HINT(H1,AMOUNT_OF_READY_RIFLES);
		H1=DS->addViewPort(FI_RifleX,-30,FI_RifleLX,30);
		HINT(H1,DAMAGE_OF_RIFLE);
		H1=DS->addViewPort(FI_SabreX,-30,FI_SabreLX,30);
		HINT(H1,DAMAGE_OF_SABRE);
		*/

		/*
		// buttons
		if(UseGrenades){
			GP_Button* GRENADE=SL_Interface.addGP_Button(NULL,FI_GrenadeX,FI_GrenadeY,FI_File,FI_GrenadeID+1,FI_GrenadeID);
			GRENADE->OnUserClick=&Grenade;
			GRENADE->UserParam=BR->ID;
		}
		HINT(SABRE,USE_MELEE_ATTACK);
		GPPicture* RIF=SL_Interface.addGPPicture(NULL,FI_RifleX+8,FI_RifleY+20,FI_File,4);
		RIF->AssignID(3);
		SL_Interface.addGPPicture(NULL,FI_SabreX,FI_SabreY+20,FI_File,7);		
		
		//commands Interface
		//if(!ReadOnly){
			GPPicture* 
			P=SL_Interface.addGPPicture(NULL,FI_BoardX,FI_BoardY,FI_IFile,2);
			HINT(P,SHOT_LINE1);
			P->AssignID(10);
			P=SL_Interface.addGPPicture(NULL,FI_BoardX,FI_BoardY+FI_IconLY,FI_IFile,1);
			HINT(P,SHOT_LINE2);
			P->AssignID(11);
			P=SL_Interface.addGPPicture(NULL,FI_BoardX,FI_BoardY+FI_IconLY*2,FI_IFile,0);
			HINT(P,SHOT_LINE3);
			P->AssignID(12);

			P=SL_Interface.addGPPicture(NULL,FI_BoardX+FI_IconLX,FI_BoardY,FI_IFile,3);
			HINT(P,FORM_LINE);
			P->AssignID(13);
			P=SL_Interface.addGPPicture(NULL,FI_BoardX+FI_IconLX,FI_BoardY+FI_IconLY,FI_IFile,4);
			HINT(P,FORM_SQUARE);
			P->AssignID(14);
			P=SL_Interface.addGPPicture(NULL,FI_BoardX+FI_IconLX,FI_BoardY+FI_IconLY*2,FI_IFile,5);
			HINT(P,FORM_KARE);
			P->AssignID(15);

			P=SL_Interface.addGPPicture(NULL,FI_BoardX+FI_IconLX,FI_BoardY-FI_IconLY*2,FI_IFile,6);
			HINT(P,FORM_FILL_FORMATION);
			P->AssignID(16);
			P->OnUserClick=&FillForm;
			P->UserParam=BR->ID;
			P=SL_Interface.addGPPicture(NULL,FI_BoardX,FI_BoardY-FI_IconLY*2,FI_IFile,7);
			P->Diffuse=0xFFFF4040;
			HINT(P,FORM_DISCARD);
			P->AssignID(17);
			P->OnUserClick=&FreeForm;
			//P=SL_Interface.addGPPicture(NULL,FI_BoardX+FI_IconLX*2,FI_BoardY+FI_IconLY*2,FI_IFile,8);
			//HINT(P,FORM_STANDGROUND);
			//P->AssignID(18);
			//P->OnUserClick=&StandGround;

		*/

			//Abilities
			for(int p=0;p<3;p++){
				GP_Button* ABL=SL_Interface.addGP_Button(NULL,FI_BoardX+p*FI_IconLX,FI_BoardY-FI_IconLY,0,0,0);
				ABL->Visible=0;
				ABL->AssignID(128+p);
			}
		//}
	}
	if(GBI){
		GBI->Visible=0;
	}
	// setup back
	if(GPP){
		// branch
		if(NM->PortBranch!=0xFFFF){
			GPbranch->SpriteID=NM->PortBranch;
			GPbranch->Visible=1;
		}else{
			GPbranch->Visible=0;
		}
		// back color
		if(NM->PortBackColor){
			GPback->Diffuse=NM->PortBackColor;
			GPback->Visible=true;
		}else{
			GPback->Visible=false;
		}
		GPbackSpr->Visible=0;
		// left
		if(ID<CurSelID){
			if(BR){
				GPP->SpriteID=10;
			}else
			if(NM->Peasant){
				GPP->SpriteID=16;
			}else{
				GPP->SpriteID=14;
			}
			GPP->x=dx*ID;
			GPP->x1=GPP->x+GPS.GetGPWidth(FI_File,10);
			GPP->y=RealLy-FI_PortretLY+16;
			GPP->y1=GPP->y+GPS.GetGPHeight(FI_File,10);
			GPP->ShapeFileID=0xFFFF;
			GPP->ShapeSpriteID=0xFFFF;
			// branch
			if(NM->PortBranch!=0xFFFF){
				GPbranch->x=GPP->x+31;
				GPbranch->y=GPP->y+36;
			}
			// back color
			if(NM->PortBackColor){
				if(BR){
					GPback->SpriteID=35;
				}else{
					GPback->SpriteID=37;
				}
				GPback->x=GPP->x;
				GPback->y=GPP->y;
			}
		}else		
		// right
		if(ID>CurSelID){			
			if(BR){
				GPP->SpriteID=11;
			}else
			if(NM->Peasant){
				GPP->SpriteID=17;
			}else{
				GPP->SpriteID=15;
			}
			GPP->x=dx*(ID-1)+156+10;
			GPP->x1=GPP->x+GPS.GetGPWidth(FI_File,11);
			GPP->y=RealLy-FI_PortretLY+16;
			GPP->y1=GPP->y+GPS.GetGPHeight(FI_File,11);
			GPP->ShapeFileID=0xFFFF;
			GPP->ShapeSpriteID=0xFFFF;
			// branch
			if(NM->PortBranch!=0xFFFF){
				GPbranch->x=GPP->x;
				GPbranch->y=GPP->y+36;
				GPbranch->Visible=false;
			}
			// back color
			if(NM->PortBackColor){
				if(BR){
					GPback->SpriteID=36;
				}else{
					GPback->SpriteID=38;
				}
				GPback->x=GPP->x;
				GPback->y=GPP->y;
			}		
		}else{			
		// top
			if(BR){
				GPP->SpriteID=FI_PortretID;
			}else			
			if(NM->Peasant){
				GPP->SpriteID=13;
			}else{
				GPP->SpriteID=12;
			}
			GPP->x=dx*ID;
			GPP->x1=GPP->x+GPS.GetGPWidth(FI_File,FI_PortretID);
			GPP->y=RealLy-FI_PortretLY;
			GPP->y1=GPP->y+GPS.GetGPHeight(FI_File,10);
			GPP->ShapeFileID=FI_File;
			GPP->ShapeSpriteID=FI_PortretID;
			if(GBI&&NM->BigIconFile<0xFFFF){
				GBI->Visible=1;
				GBI->x=GPP->x+21;
 				GBI->y=RealLy-FI_PortretLY+43;
				GBI->FileID=NM->BigIconFile;
				GBI->SpriteID=NM->BigIconIndex;
			}
			// branch
			if(NM->PortBranch!=0xFFFF){				
				GPbranch->x=GBI->x+10;
				GPbranch->y=GBI->y+10;
			}
			// back color
			if(NM->PortBackColor){
				GPback->SpriteID=34;
				GPback->x=GBI->x;
				GPback->y=GBI->y;
			}
			// back sprite
			if(NM->PortBackSprite!=0xFFFF){
				GPbackSpr->SpriteID=46+NM->PortBackSprite;
				GPbackSpr->x=GBI->x;
				GPbackSpr->y=GBI->y;
				GPbackSpr->Visible=1;
			}
		}
		// enable
		if(Enable){
			GPP->OnUserClick=&ClickOnBrig;
			GPP->UserParam=BrID;
		}else{
			GPP->OnUserClick=NULL;
		}
		if(NM->PortBackSprite!=0xFFFF){
			GPback->Visible=false;
		}
	}	

	//if(!dx) RunPF((5<<8)+4,"ShowOneSelBrig");
	DS->ProcessDialogs();
	//if(!dx) StopPF((5<<8)+4);

	// Messages		
	int CenterX=FI_PortretLX/2;
	dx*=ID;

	if(CurSelID==ID){
		ShowAString(CenterX+dx,RealLy-FI_PortretLY+20,Font,1,"%s",NM->Message);	
		if(BR){
			BrigParam BP;
			GetBrigadeParams(BR,&BP);

			//shield
			char str[32];
			int ADSH=BR->AddShield;
			ADSH+=BR->GetAbilityValue(48);//shield bonus
			if(ADSH){
				if(BR->AddShield>=0)sprintf(str,"%d+%d",ADC->Shield,BR->AddShield);
				else sprintf(str,"%d-%d",ADC->Shield,-BR->AddShield);
			}else{
				sprintf(str,"%d",ADC->Shield);
			};
			GPS.DrawFillRect(FI_PortretLX-55+4+dx,RealLy-50+10,30,16,0x40008000);
			ShowAString(FI_PortretLX-55+15+4+2+dx,RealLy-50+10,&SmallBlackFont,1,"%s",str);
			//kills
			GPS.DrawFillRect(FI_PortretLX-55+5+dx,RealLy-FI_PortretLY+45-3,30,10,0x20FF0000);
			ShowAString(FI_PortretLX-55+15+5+2+dx,RealLy-FI_PortretLY+45-3-2,&SmallBlackFont,1,"%d",BR->GetBrigExp());
			/*
			if(BR->NGrenades){
				int N=(GO->newMons->MaxGrenadesInFormation*(BR->NMemb-NBPERSONAL))/100;
				ShowAString(FI_GrenadeX+FI_GrenadeLX/2+dx,RealLy+FI_GrenadeY+3,Font,1,"%d/%d",BR->NGrenades,N);
			}
			*/
			if(BR->GetBrigExp()){
				GPS.ShowGP(FI_PortretLX-55+15+5+2-11+dx,RealLy-FI_PortretLY+45-3+12,FI_Awards,1,0);
				int N=0;
				if(BR->GetBrigExp()>10)N++;
				if(BR->GetBrigExp()>20)N++;
				if(BR->GetBrigExp()>50)N++;
				if(BR->GetBrigExp()>100)N++;
				if(BR->GetBrigExp()>200)N++;
				if(BR->GetBrigExp()>300)N++;
				for(int i=0;i<N;i++)GPS.ShowGP(FI_PortretLX-55+15+5+2-12+dx,RealLy-FI_PortretLY+45-3+7+11+i*9,FI_Awards,0,0);		
			}
			
			/*
			if(!BR->BOrder)GPS.DrawFillRect(FI_SabreX+6+dx,RealLy-22,(BRIGDELAY-BR->BrigDelay)*43/BRIGDELAY,15,0x60FF0000);
			*/
			ShowAString(CenterX+dx,RealLy-FI_PortretLY+3,Font,1,"%d/%d",BP.NLiveMembers,BR->NMemb-NBPERSONAL);
			ShowAString(CenterX+dx,RealLy-31,Font,1,"%d/%d",BP.Morale,BP.MaxMorale);
			
			// weapon numbers
			/*
			ShowAString(FI_RifleX+FI_RifleLX/2+dx,RealLy+FI_RifleY+3,Font,1,"%d",BP.NShots);						
			
			for(int i=0;i<2;i++){
				int dm=ADC->MaxDamage[i];
				int wk=ADC->WeaponKind[i];
				int ADM=0;
				char str[128];
				if(GO->newMons->SkillDamageMask&(1<<i)){
					int SC=GO->newMons->SkillDamageFormationBonusStep;
					int V;
					if(SC)V=(BR->NKills/SC)*SC;
					else V=BR->NKills;
					ADM=V*int(GO->newMons->SkillDamageFormationBonus)/100;
				};
				if(i==0){
					ADM+=BR->GetAbilityValue(32);
				}else if(i==1){
					ADM+=BR->GetAbilityValue(33);
				}
				if(WeaponFlags[wk]&4)ADM+=BR->AddDamage;
				if(ADM){
					sprintf(str,"%d+%d",dm,ADM);
				}else{
					sprintf(str,"%d",dm);
				};
				if(i==0)ShowAString(FI_SabreX+FI_SabreLX/2+dx,RealLy-23,Font,1,"%s",str);
				else ShowAString(FI_RifleX+FI_RifleLX/2+dx,RealLy-23,Font,1,"%s",str);
			}
			*/
			

			DrawVertLine(9+dx,RealLy-48,BP.Life*200/BP.MaxLife,40,255,40);
			DrawVertLine(FI_PortretLX-9+dx,RealLy-48,BP.Tiring*200/100,255,80,40);
			
			// Годовые стрелять
			//DrawVertLine(FI_RifleX+FI_RifleLX-6+dx,RealLy-10,BP.ReadyPercent*190/100,40,255,40);

			int MR=BP.Morale%100;
			int MT=BP.Morale/100;
			int x0=18;
			int y0=RealLy-12;
			int LL=148;
			int L30=LL*30/100;
			int MM=BP.MaxMorale-MT*100;
			if(MM>100)MM=100;
			DrawHorLine(x0+dx,y0,(MM*LL/100),110,90,20);
			if(!MT){		
				DrawHorLine(x0+dx,y0,(MR*LL/100),220,180,40);
				DrawHorLine(x0+dx,y0,(MR*LL/100),220,180,40);
				DrawHorLine(x0+dx,y0,MR<=30?MR*LL/100:L30,255,40,40);
			}else{
				DrawHorLine(x0+dx,y0,(MR*LL/100),220,180,40);
				int xx0=x0+LL/2-MT*5;
				for(int p=0;p<MT;p++){
					for(int q=0;q<8;q++){
						LineRGB(xx0+dx,y0-2,xx0+6+dx,y0+4,0xFFFFFF10);
						xx0++;
					}
					xx0+=2;
				}
			}
		}else{		
			ShowAString(CenterX+dx,RealLy-FI_PortretLY+3,Font,1,"%d",OIS.SelObjA[ID-OIS.NSelBr]);
		}
	}else{
		int dx,dy=0;
		if(CurSelID<ID)	dx=23;			
			else dx=11;	

		ShowAStringEx(GPP->x+dx,GPP->y+147,Font,1,0,"%s",NM->Message);
		if(BR){	
			BrigParam BP;
			GetBrigadeParams(BR,&BP);
			// morale and n warriors
			RLCFont* FontR=Font;
			if(!BP.Tiring/*||BP.Morale<45*/) FontR=&SmallRedFont;
			ShowAStringEx(GPP->x+dx,GPP->y+260,FontR,1,0,"%d",BP.Morale);
			ShowAStringEx(GPP->x+dx,GPP->y+25,Font,1,0,"%d",BP.NLiveMembers);
		}
	}
	
	GPS.FlushBatches();

	//if(GBI->Visible) return GBI->MouseOver;
	if(GPP) return GPP->MouseOver;
		else return 0;
}


void CreateFormationInterface(byte NI,word BrigID,int dx){
	
}
void CreateFormationInterface(byte NI,word BrigID){
	CreateFormationInterface(NI,BrigID,0);
}

#define MAXSEL 20
DialogsSystem DS_Sel[MAXSEL];
DialogsSystem DS_But;

DWORD LastTop=0xFFFFFFFF;
bool UI_ShiftSel=false;

CIMPORT void SetTopSelBrigID(word BID);
void CreateSelInterface(){

	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];

	//RunPF((5<<8)+1,"CreateSelInterface 1");

	InitGlobalFIVar();

	int N=min(OIS.NSelBr+OIS.NSelObj,MAXSEL);
	if(N==0) return;

	bool BrRifle=OIS.RifleAttEnabled;//true;
	BrigParam BP[200];

	word LTID=N-1;
	for(int i=0;i<OIS.NSelBr;i++){
		word bid=OIS.SelBr[i];
		Brigade* BR=CITY[NI].Brigs+bid;		
		GetBrigadeParams(BR,BP+i);
		//if(!BR->WarType||!BP[i].RifleAttack){
		//	BrRifle=false;
		//}
		if(LastTop==bid){
			LTID=i;
		}
	}
	for(i=0;i<OIS.NSelObj;i++){
		if(LastTop==(OIS.SelObj[i]<<16)){
			LTID=OIS.NSelBr+i;
		}
	}

	//StopPF((5<<8)+1);
	//RunPF((5<<8)+2,"CreateSelInterface 2");

	int NewTID=-1;
	
	for(i=0;i<N&&i<LTID;i++){
		DWORD ID;
		if(i<OIS.NSelBr){
			ID=OIS.SelBr[i];
		}else ID=OIS.SelObj[i-OIS.NSelBr]<<16;
		if(ShowOneSelBrig(DS_Sel+i,OIS.SelNation,ID,34,LTID,i)) NewTID=i;
	}
	//StopPF((5<<8)+2);
	//RunPF((5<<8)+3,"CreateSelInterface 3");
	
	// find selected GeneraalObject
	GeneralObject* TopGO=NULL;
	Brigade* TopBR=NULL;
	i=LTID;
	DWORD ID;
	if(i<OIS.NSelBr){		
		ID=OIS.SelBr[i];
		TopBR=CITY[OIS.SelNation].Brigs+ID;
		if(TopBR->Enabled&&TopBR->WarType){
			TopGO=NATIONS[OIS.SelNation].Mon[TopBR->MembID];
		}			
	}else{
		ID=OIS.SelObj[i-OIS.NSelBr]<<16;
		TopGO=NATIONS[OIS.SelNation].Mon[ID>>16];
	}

	for(i=N-1;i>=LTID;i--){
		DWORD ID;
		if(i<OIS.NSelBr){
			ID=OIS.SelBr[i];
			if(i==LTID) SetTopSelBrigID(ID);
		}else{
			ID=0xFFFF|(OIS.SelObj[i-OIS.NSelBr]<<16);
		}
		if(ShowOneSelBrig(DS_Sel+i,OIS.SelNation,ID,34,LTID,i)) NewTID=i;
	}

	if(NewTID!=-1){
		if(NewTID<OIS.NSelBr) LastTop=OIS.SelBr[NewTID];
			else LastTop=OIS.SelObj[NewTID-OIS.NSelBr]<<16;
	}
	//StopPF((5<<8)+3);

	// Select shift mode	
	if((GetKeyState(VK_MENU)&0x6000)){
		if(!UI_ShiftSel){
			UI_ShiftSel=true;
			for(int i=0;i<N;i++) DS_Sel[i].DSS[0]->Diffuse=0xFF808080;
		}
	}else{
		if(UI_ShiftSel){
			UI_ShiftSel=false;
			for(int i=0;i<N;i++) if(DS_Sel[i].DSS[0]->Diffuse==0xFFFFFFFF){
				UI_ShiftSel=true;
				break;
			};
			for(i=0;i<N;i++){
				SimpleDialog* SD=DS_Sel[i].DSS[0];
				if(UI_ShiftSel&&SD->Diffuse!=0xFFFFFFFF){
					if(i<OIS.NSelBr) CmdChooseUnSelBrig(MyNation,OIS.SelBr[i]);
						else CmdChooseUnSelType(MyNation,OIS.SelObj[i-OIS.NSelBr]);
				}
				SD->Diffuse=0xFFFFFFFF;
			}
		
			UI_ShiftSel=false;
		}
	}

	// mass buttons

	GP_Button* RIFLE=(GP_Button*)DS_But.Find(1);
	GP_Button* SABRE=(GP_Button*)DS_But.Find(2);
	GPPicture* RIF  =(GPPicture*)DS_But.Find(3);
	GPPicture* SAB  =(GPPicture*)DS_But.Find(4);

	if(!RIFLE){
		DS_But.BaseY=RealLy;

		RIFLE=DS_But.addGP_Button(NULL,FI_RifleX,FI_RifleY,FI_File,3,2);
		RIFLE->AssignID(1);
		RIFLE->OnUserClick=&MassSetAtt;
		RIFLE->UserParam=129;

		SABRE=DS_But.addGP_Button(NULL,FI_SabreX,FI_SabreY,FI_File,6,5);
		SABRE->AssignID(2);
		SABRE->OnUserClick=&MassSetAtt;
		SABRE->UserParam=1;

		RIF=DS_But.addGPPicture(NULL,FI_RifleX+8,FI_RifleY+20,FI_File,4);
		RIF->AssignID(3);
		
		SAB=DS_But.addGPPicture(NULL,FI_SabreX,FI_SabreY+20,FI_File,7);
		SAB->AssignID(4);
		//SL_Interface.addTextButton(NULL,CenterX,-FI_PortretLY+20,NM->Message,Font,Font,Font,1);
	}

	DS_But.SetHintStyle(0,1,1,30,0,0,34*(N-1)+FI_PortretLX+4,RealLy-FI_PortretLY+17,400,FI_PortretLY-FI_RifleLY-30,&SmallWhiteFont,0x90404040,0);

	if(SABRE){
		SABRE->x=34*(N-1)+FI_PortretLX;
		SABRE->x1=SABRE->x+FI_SabreLX;
		SABRE->y=RealLy+FI_SabreY;
		SABRE->y1=SABRE->y+FI_SabreLY;
		HINT(SABRE,USE_MELEE_ATTACK);
	}
	if(SAB){
		SAB->x=SABRE->x;
		//RIF->x1=RIF->x+FI_RifleLX;
		SAB->y=SABRE->y+20;
		//RIF->y1=RIF->y+FI_RifleLY;
		if(TopGO){
			if(TopGO->newMons->BigWeapFile){
                SAB->SpriteID=TopGO->newMons->BigFireWeapSprite;
				SAB->FileID=TopGO->newMons->BigWeapFile;
			}
		}
	}
	if(RIFLE){
		RIFLE->x=SABRE->x+FI_SabreLX;
		RIFLE->x1=RIFLE->x+FI_RifleLX;
		RIFLE->y=RealLy+FI_RifleY;
		RIFLE->y1=RIFLE->y+FI_RifleLY;
		if(TopBR){
			RIFLE->ActiveFrame=3;
			RIFLE->PassiveFrame=2;
		}else{
			RIFLE->ActiveFrame=6;
			RIFLE->PassiveFrame=5;
		}
		HINT(RIFLE,DISABLE_RIFLE_ATTACK);
		RIFLE->Visible=OIS.RifleAttackAllowed;
	}
	if(RIF){	
		if(BrRifle){
			RIFLE->UserParam=128;
			RIF->Diffuse=0xFFFFFFFF;
			HINT(RIFLE,DISABLE_RIFLE_ATTACK);
		}else{ 
			RIFLE->UserParam=129;
			RIF->Diffuse=0x60FF8080;
			HINT(RIFLE,ENABLE_RIFLE_ATTACK);
		}
		RIF->x=RIFLE->x+8;
		//RIF->x1=RIF->x+FI_RifleLX;
		RIF->y=RIFLE->y+20;
		//RIF->y1=RIF->y+FI_RifleLY;
		if(TopGO){
			if(TopGO->newMons->BigWeapFile){
				SAB->SpriteID=TopGO->newMons->BigColdWeapSprite;
				SAB->FileID=TopGO->newMons->BigWeapFile;
			}
		}
		RIF->Visible=OIS.RifleAttackAllowed;
	}
	
	if(!TopGO) DS_But.ProcessDialogs();
	
	// weapon charachters
	if(TopGO){
		RLCFont* Font=&SmallYellowFont;
		AdvCharacter* ADC=TopGO->MoreCharacter;		
				
		char stxt[128]="";
		char rtxt[128]="";

		for(int i=0;i<2;i++){
			int dm=ADC->MaxDamage[i];
			int wk=ADC->WeaponKind[i];
			int ADM=0;
			char str[128];
			if(TopBR){			
				if(TopGO->newMons->SkillDamageMask&(1<<i)){
					int SC=TopGO->newMons->SkillDamageFormationBonusStep;
					int V;
					if(SC)V=(TopBR->GetBrigExp()/SC)*SC;
					else V=TopBR->GetBrigExp();
					ADM=V*int(TopGO->newMons->SkillDamageFormationBonus)/100;
				};
				if(i==0){
					ADM+=TopBR->GetAbilityValue(32);
				}else if(i==1){
					ADM+=TopBR->GetAbilityValue(33);
				}
				if(WeaponFlags[wk]&4)ADM+=TopBR->AddDamage;
			}

			if(ADM){
				sprintf(str,"%d+%d",dm,ADM);
			}else{
				sprintf(str,"%d",dm);
			};
			if(i==0){
				if(dm+ADM){
					strcpy(stxt,str);
					SAB->Visible=1;
					SABRE->Visible=1;					
				}else{
					SAB->Visible=0;
					SABRE->Visible=0;
				}
			}else{
				if(dm+ADM){
					strcpy(rtxt,str);
					RIF->Visible=1;
					RIFLE->Visible=1;					
				}else{
					RIF->Visible=0;
					RIFLE->Visible=0;
				}				
			}
		}
		DS_But.ProcessDialogs();

		if(stxt[0])ShowAString(SABRE->x+FI_SabreLX/2,RealLy-21,Font,1,"%s",stxt);
		if(rtxt[0])ShowAString(RIFLE->x+FI_RifleLX/2,RealLy-21,Font,1,"%s",rtxt);

		if(TopBR){
			BrigParam BP;
			GetBrigadeParams(TopBR,&BP);

			ShowAString(RIFLE->x+FI_RifleLX/2,RealLy-FI_RifleLY+3,Font,1,"%d",BP.NShots);
			DrawVertLine(RIFLE->x+FI_RifleLX-4,RealLy-9,BP.ReadyPercent*190/100,40,40,40);
			DrawVertLine(RIFLE->x+FI_RifleLX-6,RealLy-9,BP.ReadyPercent*190/100,0,255,0);
			DrawVertLine(RIFLE->x+FI_RifleLX-5,RealLy-9,BP.ReadyPercent*190/100,0,255,0);				
		}
	}

}
//
CEXPORT void SelBrigadeExp(byte NI, byte Type, int ID){
	ImSelBrigade(NI,Type,GSets.CGame.cgi_NatRefTBL[NI],ID);
	SelBrigade(NI,Type,GSets.CGame.cgi_NatRefTBL[NI],ID);	
};
//
extern OISelection OIS;
void GetUnitsSelGroups(byte NI){
	OIS.Process(NI);
	
	int i;
	//return;

	// крестьян в конец
	GeneralObject** GO=NATIONS[NI].Mon;
	for(i=OIS.NSelObj-2;i>=0;i--){
		NewMonster* NM=GO[OIS.SelObj[i]]->newMons;
		NewMonster* N2=GO[OIS.SelObj[i+1]]->newMons;
		if(NM->Peasant&&!N2->Peasant){
			for(int j=i+2;j<OIS.NSelObj;j++){
				N2=GO[OIS.SelObj[j]]->newMons;
				if(N2->Peasant){
					break;
				}
			}
			//if(j>OIS.NSelObj) j=OIS.NSelObj;			
			swap(OIS.SelObj[i],OIS.SelObj[j-1]);
			swap(OIS.SelObjA[i],OIS.SelObjA[j-1]);
		}
	}
}

// ================================================================================
// ui_UnitInfoFill
// ================================================================================
void ui_UnitInfoFill(vui_UnitInfo *I, OneObject *Obj)
{
	NewMonster *NM = Obj->newMons;
	GeneralObject *GO = Obj->Ref.General, *gObj = NULL;
	AdvCharacter *ADC = GO->MoreCharacter;
	Nation *pNation = NULL;
	NewUpgrade *pNewUpgrade = NULL;
	int n = 0, i = 0;

	I->Speed = Obj->newMons->MotionDist * 
		Obj->Ref.General->MoreCharacter->Speed / 100;
	I->Spread = ADC->Razbros;
	I->BuildingTime = ADC->ProduceStages;
	memcpy(I->Price, ADC->NeedRes, sizeof(I->Price));
	I->Cost = NM->Ves;
	I->Vision = NM->VisionType;
	I->AttackRadius = NM->AttackRadius2[0];
	if(I->AttackRadius == 0) I->AttackRadius = NM->AttackRadius2[1];
	I->MoraleRegeneration = NM->MoraleRegenerationSpeed;
	I->StrikeProbability = NM->StrikeProbability;
	I->VeteranKills = NM->VeteranKills;
	I->ExpertKills = NM->ExpertKills;
	// Calculating AttackSpeed (in what units?)
	float GetAttSpped(OneObject *, int);
	I->AttackSpeed = 0;
	for(int i = 0; i <= 1; i++)
	{
		float AttackSpeed = GetAttSpped(Obj, i);
		if(AttackSpeed > 0.0f){
			I->AttackSpeed = AttackSpeed;
			break;
		}
	}
	// -----------------------------
	// extracting upgrades info
	// -----------------------------
	int GetWeaponType(char *);
	int iChopping = GetWeaponType("chopping"), 
		iPiercing = GetWeaponType("piercing"),
		iCrushing = GetWeaponType("crushing");

	I->AttackUpgrades[0] = I->AttackUpgrades[1] = I->AttackUpgrades[2] = 0;
	ZeroMemory(I->DefenceUpgrades, sizeof(I->DefenceUpgrades));
	pNation = &NATIONS[GO->NatID];
	if(pNation != NULL)
		for(n = 0; n < pNation->NUpgrades; n++)
		{
			pNewUpgrade = pNation->UPGRADE[n];
			if(pNewUpgrade != NULL)
			{
				if((pNewUpgrade->CtgUpgrade != 12) && // AttackUpgrades
					(pNewUpgrade->CtgUpgrade != 2) && // DefenceUpgrades
					(pNewUpgrade->UnitGroup == NULL) &&
					(pNewUpgrade->UnitValue == Obj->NIndex) &&
					(I->RUNumber < 10))
				{
					I->RUIconFileIDs[I->RUNumber] = 
						pNewUpgrade->IconFileID;
					I->RUIconSpriteIDs[I->RUNumber] = 
						pNewUpgrade->IconSpriteID;
					I->RUHints[I->RUNumber] = 
						pNewUpgrade->Message;
					I->RUNumber++;
				}
				if(pNewUpgrade->CtgUpgrade == 12) // AttackUpgrades
				{
					if((pNewUpgrade->UnitGroup == NULL) && 
						(pNewUpgrade->UnitValue == Obj->NIndex))
					{
						if((pNewUpgrade->Level >= 2) && (pNewUpgrade->Level <= 4))
							I->AttackUpgrades[pNewUpgrade->Level - 2] = 
							pNewUpgrade->Value;
					}

				}
				if(pNewUpgrade->CtgUpgrade == 2)	// DefenceUpgrades
				{
					if((pNewUpgrade->UnitGroup == NULL) &&
						(pNewUpgrade->UnitValue == Obj->NIndex))
						if((pNewUpgrade->Level >= 2) && (pNewUpgrade->Level <= 4))
							for(i = 0; i < pNewUpgrade->NCtg; i++)
							{
								if(pNewUpgrade->CtgGroup[i] == iChopping)	// 3
									I->DefenceUpgrades[0][pNewUpgrade->Level - 2] = 
									pNewUpgrade->Value;
								if(pNewUpgrade->CtgGroup[i] == iPiercing)	// 0
									I->DefenceUpgrades[1][pNewUpgrade->Level- 2] =
									pNewUpgrade->Value;
								if(pNewUpgrade->CtgGroup[i] == iCrushing)	// 2
									I->DefenceUpgrades[2][pNewUpgrade->Level -2] =
									pNewUpgrade->Value;
							}
				}				
				if(pNewUpgrade->UnitGroup != NULL)
				{
					for(i = 0; i < pNewUpgrade->NUnits; i++)
						if(pNewUpgrade->UnitGroup[i] == Obj->NIndex)
							if(I->RUNumber < 10)
							{
								// Prohibiting upgrades,
								// which have Message strarting from HER
								bool bProhibited = false;
								if(strlen(pNewUpgrade->Message) > 3)
									if((pNewUpgrade->Message[0] == 'H') &&
										(pNewUpgrade->Message[1] == 'E') &&
										(pNewUpgrade->Message[2] == 'R'))
										bProhibited = true;
								if(bProhibited == false)
								{
									I->RUIconFileIDs[I->RUNumber] =
										pNewUpgrade->IconFileID;
									I->RUIconSpriteIDs[I->RUNumber] =
										pNewUpgrade->IconSpriteID;
									I->RUHints[I->RUNumber] =
										pNewUpgrade->Message;
									I->RUNumber++;
								}
							}
				}				
			}
		}
	// LifeMax
	I->LifeMax = Obj->MaxLife;

	// AttackType
	if(Obj->newMons->Peasant == false)
		I->WeapType[0] = ADC->WeaponKind[0],
		I->WeapType[1] = ADC->WeaponKind[2];
}
vui_SelPoint::Init(OneObject* Obj){
	//memset(this,0,sizeof *this);
	ZeroMemory(this, sizeof(*this));
	vui_UpgradeInfo ui;

	OB=Obj;
	NewMonster* NM=Obj->newMons;
	GeneralObject* GO=Obj->Ref.General;
	AdvCharacter* ADC=GO->MoreCharacter;
	NatID=GO->NatID;
	NI=Obj->NNUM;
	NIndex=Obj->NIndex;
	rX=Obj->RealX;
	rY=Obj->RealY;
	SearchVictim=!Obj->NoSearchVictim;
	if(NM->VUI==0){	//	Cannon
		Type=ve_UT_cannon;
		vui_CannInfo* I=&Inf.Cannon;
		I->OB=Obj;
		GetPushkaChargeState(Obj,I->ChargeType,I->ChargeStage);
		I->Shield=ADC->Shield;
		I->NKills=Obj->Kills;
		I->Damage[0]=ADC->MaxDamage[0]+ADC->WeaponKind[0];
		I->Damage[1]=ADC->MaxDamage[1]+ADC->WeaponKind[1];
		I->Damage[2]=ADC->MaxDamage[2]+ADC->WeaponKind[2];
		if(Obj->LocalOrder&&Obj->LocalOrder->DoLink==NewAttackPointLink) I->NShots=1;
		else I->NShots=0;
	}else
	if(Obj->BrigadeID!=0xFFFF){	// Brigade
		Type=ve_UT_brigade;
		vui_BrigInfo *I = &Inf.Brigade;		
		I->BrigID=Obj->BrigadeID;
		I->MaxLife=-1;
		//
		ZeroMemory(&I->UI, sizeof(Inf.Units));
		//
	}else
	if(!Obj->newMons->Building){
		Type = ve_UT_units;	// Units
		vui_UnitInfo *I = &Inf.Units;
		//
		ZeroMemory(I, sizeof(Inf.Units));
		//
		I->GroundState=(Obj->GroundState==1);
		I->pAbilities = NM->Ability;
		OIS.ActiveState|=(1<<Obj->ActivityState);
		I->Amount=1;
		I->Life=Obj->Life;
		I->Damage[0]=0;
		I->Damage[1]=0;
		I->Defence[0]=OB->AddShield;
		I->Defence[1]=OB->AddShield;
		I->Defence[2]=OB->AddShield;
		I->Defence[3]=OB->AddShield;
		I->Shield=ADC->Shield;
		I->NKills=Obj->Kills;
		I->Patrol=false;
		Order1* Or=Obj->LocalOrder;
		while(Or){
			void PatrolLink(OneObject* OBJ);
			if(Or->DoLink==PatrolLink){
				I->Patrol=true;
				break;
			}
			Or=Or->NextOrder;
		}
		if(Obj->newMons->Peasant){
			char NATID[5];
			int NPID=CITY[NI].NationalPeasantID;
			if(NPID!=0xFFFF&&!PlayGameMode){
				char* S=NATIONS->Mon[NPID]->MonsterID;
				int L=strlen(S);
				strcpy(NATID,S+L-4);
			}else{
				NATID[0]=0;
			};
			bool CANDO1=1;
			if(NATID[0]){
				CANDO1=strstr(Obj->Ref.General->MonsterID,NATID);
			};
			bool OKK=1;
			if(Obj&&Obj->newMons->Peasant&&!(EditMapMode||PlayGameMode)){
				if(NPID==0xFFFF){
					CITY[NI].NationalPeasantID=Obj->NIndex;
					CmdChangeNPID(NI,Obj->NIndex);
				}else{
					if(NPID!=Obj->NIndex)OKK=0;
				};
			};
			bool CANDO=1;
			if(NATID[0]){
				CANDO=CANDO1;
			};
			I->Peasant=OKK&&CANDO&&Obj->Ready;
		}
		if(!Obj->newMons->Peasant||EngSettings.vInterf.ShowPeasantDamage){
			//I->Peasant=false;
			I->WeapType[0]=ADC->WeaponKind[0];
			I->WeapType[1]=ADC->WeaponKind[2];
			I->Damage[0]+=ADC->MaxDamage[0];			
			if(ADC->MaxDamage[1]>0) I->Damage[1]+=ADC->MaxDamage[1];
				else I->Damage[1]+=ADC->MaxDamage[2];
			if(I->Damage[0]) I->Damage[0]+=OB->AddDamage;
			if(I->Damage[1]) I->Damage[1]+=OB->AddDamage;
			I->Defence[0]+=ADC->Protection[3];
			I->Defence[1]+=ADC->Protection[0];
			I->Defence[2]+=ADC->Protection[2];
			I->Defence[3]+=ADC->Protection[1];
			I->WeapFileID=NM->BigWeapFile;
			I->WeapColdSprite=NM->BigColdWeapSprite;
			I->WeapFireSprite=NM->BigFireWeapSprite;
			I->RifleAttack=Obj->RifleAttack;
			I->NShots=Obj->delay?0:1;
			I->Delay=Obj->delay;
			I->DelayMax=Obj->MaxDelay;
		}
		I->Morale=Obj->Morale/10000;
		I->MoraleMax=Obj->MaxMorale?Obj->MaxMorale/10000:1;
		I->Kinetic=Obj->KineticPower;
		I->KineticMax=NM->KineticLimit;

		// LifeMax
		I->LifeMax = Obj->MaxLife;

		// AttackType
		if(Obj->newMons->Peasant == false)
			I->WeapType[0] = ADC->WeaponKind[0],
			I->WeapType[1] = ADC->WeaponKind[2];

	}else{	// Buildings
		Type=ve_UT_building;
		vui_BuildInfo* I=&Inf.Buildings;
		//
		ZeroMemory(&I->UI, sizeof(Inf.Units));
		I->Amount=1;
		I->Ready=Obj->Ready;
		I->Stage=Obj->Stage;
		I->StageMax=Obj->NStages;
		I->Places=NM->NInFarm;
		I->Population=GetCurrentUnits(NI);
		I->PopulationMax=GetMaxUnits(NI);
		I->Life=Obj->Life;
		I->LifeMax=Obj->MaxLife?Obj->MaxLife:1;
		I->SingleUpgLevel=Obj->SingleUpgLevel;
		I->OB[0]=Obj;
		// attack params: the same as in Units
		I->UI.WeapType[0] = ADC->WeaponKind[0];
		I->UI.WeapType[1] = ADC->WeaponKind[2];
		I->UI.Damage[0] = ADC->MaxDamage[0];			
		if(ADC->MaxDamage[1] > 0) I->UI.Damage[1] = ADC->MaxDamage[1];
			else I->UI.Damage[1] = ADC->MaxDamage[2];
		if(I->UI.Damage[0]) I->UI.Damage[0]+=OB->AddDamage;
		if(I->UI.Damage[1]) I->UI.Damage[1]+=OB->AddDamage;
		// defence params: the same as in Units
		I->UI.Defence[0] = OB->AddShield;
		I->UI.Defence[1] = OB->AddShield;
		I->UI.Defence[2] = OB->AddShield;
		I->UI.Defence[3] = OB->AddShield;
		I->UI.Defence[0] += ADC->Protection[3];
		I->UI.Defence[1] += ADC->Protection[0];
		I->UI.Defence[2] += ADC->Protection[2];
		I->UI.Defence[3] += ADC->Protection[1];
		//
		I->AllowShoot=!OB->NoSearchVictim;
	}
	// view sort priority
	Sort=Type;
	if(vHeroButtons.isHero(Obj)){
		Sort=ve_UT_hero;
	}
	//
	void PerformUpgradeLink(OneObject*);
	CanUpg=NULL;
	if(Obj->LocalOrder&&Obj->LocalOrder->DoLink==PerformUpgradeLink){
		vu_UpgInfo U;
		U.ID=Obj->LocalOrder->info.PUpgrade.NewUpgrade;
		U.Stage=Obj->LocalOrder->info.PUpgrade.Stage;
		U.StageMax=Obj->LocalOrder->info.PUpgrade.NStages;
		U.SingleUpgLevel=Obj->SingleUpgLevel;
		Upg.Add(U);
	}else{		
		CanUpg=Obj;
	}
	//
	Abl=NM->Ability;
	ActAbl=Obj->ActiveAbility;
};
int vui_SelPoint::Cmp(vui_SelPoint* SP){
	// 1 - SP<this, 0 - equal, - 1 - SP>this
	if(Sort==SP->Sort){
		if(NIndex==SP->NIndex){
			switch(Sort){
				case ve_UT_cannon:
					return SP->Inf.Cannon.OB->Index-Inf.Cannon.OB->Index;
				case ve_UT_brigade:
					return SP->Inf.Brigade.BrigID-Inf.Brigade.BrigID;
				case ve_UT_building:{
					vui_BuildInfo* SI=&SP->Inf.Buildings;
					vui_BuildInfo* I=&Inf.Buildings;
					if(SI->Stage==I->Stage) return 0;
					//if(SI->Stage<SI->StageMax) return 1;
					//if(I->Stage<I->StageMax) return -1;
					break;
				}
			}
			return 0;
		}else{
			switch(Sort){
				case ve_UT_units:
					if(Inf.Units.Peasant&&!SP->Inf.Units.Peasant) return -1;
					if(!Inf.Units.Peasant&&SP->Inf.Units.Peasant) return 1;
			}
			return SP->NIndex-NIndex;
		}		
	}else{
		return SP->Sort-Sort;
	}
};
bool vui_SelPoint::Add(vui_SelPoint* SP){
	if(Cmp(SP)==0){
		if(SP->SearchVictim)SearchVictim=true;
		if(!CanUpg){
			CanUpg=SP->CanUpg;
		}
		if(SP->Upg.GetAmount()){
			Upg.Add(SP->Upg[0]);
		}		
		switch(Type){
			case ve_UT_cannon:
				break;
			case ve_UT_brigade: {
				vui_BrigInfo* I=&Inf.Brigade;
				break; }
			case ve_UT_units: {
				vui_UnitInfo* I=&Inf.Units;
				I->Amount++;
				rX=(rX+SP->rX)/2;
				rY=(rY+SP->rY)/2;
				I->Life+=SP->Inf.Units.Life;
				I->NKills+=SP->Inf.Units.NKills;
				I->NShots+=SP->Inf.Units.NShots;
				I->Delay+=SP->Inf.Units.Delay;
				if(SP->Inf.Units.DelayMax>I->DelayMax)I->DelayMax=SP->Inf.Units.DelayMax;
				if(SP->Inf.Units.LifeMax>I->LifeMax){
					I->LifeMax=SP->Inf.Units.LifeMax;
				}
				I->GroundState|=SP->Inf.Units.GroundState;
				break; }
			case ve_UT_building: {
				vui_BuildInfo* I=&Inf.Buildings;
				if(I->Amount<10){
					I->OB[I->Amount]=SP->Inf.Buildings.OB[0];
				}				
				I->Amount++;
				I->Life+=SP->Inf.Buildings.Life;
				if(SP->Inf.Buildings.LifeMax>I->LifeMax){
					I->LifeMax=SP->Inf.Buildings.LifeMax;
				}
				I->AllowShoot|=SP->Inf.Buildings.AllowShoot;
				break; }
		}
		return true;
	}
	return false;
};
//
void OISelection::Clear() {
	SelPoint.FastClear();
	// old
	NSelBr=0; NSelObj=0;
	int n=Bld.GetAmount();
	for(int i=0;i<n;i++){
		if(Bld[i])free(Bld[i]);
	}
	Bld.Clear();
	Settlement=0xFFFF;
	Oboz=0xFFFF;
	RifleAttEnabled=1;
	RifleAttackAllowed=0;
}

void OISelection::Process(byte NI){
	OIS.ActiveState=0;
	// Init
	vui_SelPoint LSP;
	if(LastSP<SelPoint.GetAmount()){
		LSP=SelPoint[LastSP];
	}
	// AddObjects
	CreateFromSelection(NI);
	// Find LastSP
	LastSP=0xFFFF;
	for(int i=0;i<SelPoint.GetAmount();i++){
		if(SelPoint[i].Cmp(&LSP)==0){
			LastSP=i;
			break;
		}
	}
	if(LastSP==0xFFFF) LastSP=0;
	SetProduce();
	SetUpgrade();
}
bool BuyBlankBuilding(byte NI, word NIndex){
	Nation* Nat=NATIONS+NI;
	//
	/*
	int NBuild=Nat->PACount[BuilderNIndex];
	word* Build=Nat->PAble[BuilderNIndex];
	char* AIndex=Nat->AIndex[BuilderNIndex];
	*/
	//
	word* unID=NatList[NI];	// list units in nation
	int unN=NtNUnits[NI];
	//byte bAm[2048];	// amount current availible for construct
	//word bID[2048];	// index in Group[]
	//memset(bAm,0,sizeof(bAm));
	//memset(bID,0xFF,sizeof(bID));
	for(int i=0;i<unN;i++,unID++){
		OneObject* OB=Group[*unID];
		if(OB&&(!OB->Sdoxlo)&&OB->NewBuilding&&OB->NIndex==NIndex&&OB->Stage<OB->NStages){
			vui_IS_MakeMaxStage Com;
			Com.Data.Index=*unID;
			Com.InterfAction();
			return true;
		}
	}
	return false;
}
void OISelection::SetUpgrade(){
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	//
	Upgrade.FastClear();
	if(SelPoint.GetAmount()!=1) return;
	vui_SelPoint* SP=SelPoint+LastSP;
	//
	if(SP->Type==ve_UT_building&&(SP->Inf.Buildings.Stage<SP->Inf.Buildings.StageMax)) return;
	if(SP->NI!=NI) return;
	//
	Nation* Nat=NATIONS+NI;
	GeneralObject* GO=Nat->Mon[SP->NIndex];
	int NUpg=GO->NUpgrades;
	word* Upg=GO->Upg;

	for(int i=0;i<NUpg;i++,Upg++){
		NewUpgrade* U=Nat->UPGRADE[*Upg];
		if(U->ManualDisable) continue;
		bool IsDoing=false;
		if(U->IsDoing){
			for(int i=0;i<SP->Upg.GetAmount();i++){
				if(SP->Upg[i].ID==*Upg){
					IsDoing=true;
					break;
				}
			}
		}
		if(U&&!U->Done&&(U->ManualEnable||U->Enabled||IsDoing)){
			bool ok=true;			
			if(SP->Type==ve_UT_building&&U->Individual&&
				SP->Inf.Buildings.SingleUpgLevel!=U->Level) ok=false;				
			if(ok){
				vui_UpgradeInfo UI;

				UI.Upg=U;
				UI.Index=*Upg;
				UI.FileID=U->IconFileID;
				UI.SpriteID=U->IconSpriteID;
				UI.Message=U->Message;
				//UI.Building=nm->Building;
				//PI.Enabled=go->Enabled;
				//PI.NIndex=*Build;
				UI.x=U->IconPosition%12;
				UI.y=U->IconPosition/12;
				/*
				if(PI.Building){
					if(SP->Type==ve_UT_building){
						PI.NProduce=bAm[*Build];
						PI.NUnlimit=bID[*Build];
						if(bID[*Build]==0xFFFF) PI.NIndex=0xFFFF;
					}else{
						PI.NProduce=0;
						PI.NUnlimit=0;
					}			
					PI.Stage=0;
					PI.MaxStage=1;
				}else{
					PI.NProduce=GetAmount(*Build);
					PI.NUnlimit=0;
					if(PI.NProduce>=1200){
						PI.NUnlimit=PI.NProduce/1200;
						PI.NProduce=PI.NProduce%1200;
					}
					PI.Stage=GetProgress(*Build,&PI.MaxStage);
				};
				if(PI.MaxStage==0) PI.MaxStage=1;
				*/
				//PI.HotKey=go->newMons->BuildHotKey;
				Upgrade.Add(UI);
			}
		}
	}
}
void OISelection::SetProduce(){
	byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
	//
	Produce.FastClear();
	if(SelPoint.GetAmount()!=1) return;
	vui_SelPoint* SP=SelPoint+LastSP;
	if(BuildMode) return;
	//if(SP->Type==ve_UT_brigade) return;
	//if(SP->Type==ve_UT_building&&(!SP->Inf.Buildings.Ready||SelPoint.GetAmount()!=1)&&SP->Upg.ID==0xFFFF) return;
	if(SP->Type==ve_UT_building&&(!SP->Inf.Buildings.Ready||SelPoint.GetAmount()!=1)&&SP->Upg.GetAmount()==0) return;
	if(SP->NI!=NI) return;
	//			
	Nation* Nat=NATIONS+NI;
	City* CT=CITY+NI;
	word NIndex=SP->NIndex;
	//
	int NBuild=Nat->PACount[NIndex];
	word* Build=Nat->PAble[NIndex];
	char* AIndex=Nat->AIndex[NIndex];
	//	
	bool bld=false;	// if produce buildings
	word* unID=NatList[NI];	// list units in nation
	int unN=NtNUnits[NI];
	byte bAm[2048];	// amount current availible for construct
	word bID[2048];	// index in Group[]
	memset(bAm,0,sizeof(bAm));
	memset(bID,0xFF,sizeof(bID));
	for(int i=0;i<unN;i++,unID++){
		OneObject* OB=Group[*unID];
		if(OB&&(!OB->Sdoxlo)&&OB->NewBuilding&&OB->Stage<OB->NStages){
			word NIndex=OB->NIndex;
			bAm[NIndex]++;
			if(bID[NIndex]==0xFFFF) bID[NIndex]=*unID;
		}
	}
	//
	for(i=0;i<NBuild;i++,Build++,AIndex++){
		GeneralObject* go=Nat->Mon[*Build];
		NewMonster* nm=go->newMons;
		bld|=nm->Building&&!nm->SelfTransform;
		if(go->ManualDisable) continue;
		//if(!go->Enabled) continue;		
		/*
		bool okk=1;
		if(go->StageMask){
		byte m=go->StageMask;
		word s=OBJ->StageState;
		for(int i=0;i<5;i++){
		if(m&1){
		byte s1=s&7;
		if(s1!=2)okk=0;
		};
		m>>=1;
		s>>=3;
		};
		};
		if(!okk) continue;	
		*/
		//
		vui_ProduceInfo PI;
		PI.Building=nm->Building&&!nm->SelfTransform;
		//
		PI.Enabled=go->Enabled||go->ManualEnable;
		if(nm->ArtSet){
			int n=Nat->NArtUnits[nm->ArtSet-1];
			int m=Nat->NArtdep*nm->NInArtDepot;
			if(n>=m){
				PI.Enabled=false;
			}						
		}				
		if(go->LockID!=0xFFFF){
			int n=CT->UnitAmount[go->LockID];
			int m=go->NLockUnits;
			if(n>=m){
				PI.Enabled=false;
			}						
		};				
		//
		PI.NIndex=*Build;
		PI.x=(*AIndex)%12;
		PI.y=(*AIndex)/12;
		if(PI.Building){
			if(SP->Type==ve_UT_building){
				PI.NProduce=bAm[*Build];
				PI.NUnlimit=bID[*Build];
				if(bID[*Build]==0xFFFF) PI.NIndex=0xFFFF;
			}else{
				PI.NProduce=0;
				PI.NUnlimit=0;
			}			
			PI.Stage=0;
			PI.MaxStage=1;
		}else{
			PI.NProduce=GetAmount(*Build);
			PI.NUnlimit=0;
			if(PI.NProduce>=1200){
				PI.NUnlimit=PI.NProduce/1200;
				PI.NProduce=PI.NProduce%1200;
			}
			PI.Stage=GetProgress(*Build,&PI.MaxStage);
		};
		if(PI.MaxStage==0) PI.MaxStage=1;
		PI.HotKey=go->newMons->BuildHotKey;
		Produce.Add(PI);
	}
	if(SP->Type==ve_UT_building&&bld){
		for(i=0;i<Produce.GetAmount();i++){
			word* NInd=&Produce[i].NIndex;
			bool Bld=Produce[i].Building;
			(*NInd)|=0xFFFF*word(!Bld);
		}
	}
};
void OISelection::CreateFromSelection(byte NI){
	Clear();
	int N=ImNSL[NI];
	word* IDS=ImSelm[NI];
	word* SNS=ImSerN[NI];
	for(int i=0;i<N;i++){
		OneObject* OB=Group[IDS[i]];
		if(OB&&OB->Serial==SNS[i]&&!OB->Sdoxlo){
			AddObj(OB);			
		}
	}
	// Rome Unit Info
	N=SelPoint.GetAmount();
	extern bool m_U_Info;
	if(N==1&&m_U_Info) for(i=0;i<N;i++){
		vui_SelPoint* SP=SelPoint+i;
		if(SP){
			switch(SP->Type){
				case ve_UT_building:
					{
						//ZeroMemory(&SP->Inf.Buildings.UI, sizeof(SP->Inf.Buildings.UI));
						ui_UnitInfoFill(&SP->Inf.Buildings.UI, SP->OB);
						// Attack Speed is specific for this unit type
						OneObject *Obj = SP->OB;
						SP->Inf.Buildings.UI.AttackSpeed = 0;
						for(int i = 0; i <= 1; i++)
						{
							float AttackSpeed =
								Obj->Ref.General->MoreCharacter->AttackPause[i];
							if(AttackSpeed > 0.0f){
								SP->Inf.Buildings.UI.AttackSpeed = AttackSpeed;
								break;
							}
						}
						// Buildtime is specific for this unit type
						if(Obj->newMons != NULL)
							SP->Inf.Buildings.UI.BuildingTime *=
							Obj->newMons->BuildPtX.NValues;
					}
					break;
				case ve_UT_units:
					//ZeroMemory(&SP->Inf.Units, sizeof(SP->Inf.Units));
					ui_UnitInfoFill(&SP->Inf.Units, SP->OB);
					break;
				case ve_UT_brigade:
					//ZeroMemory(&SP->Inf.Brigade.UI, sizeof(SP->Inf.Brigade.UI));
					ui_UnitInfoFill(&SP->Inf.Brigade.UI, SP->OB);
					break;
			};
		}
	}
};
void OISelection::AddObj(OneObject* OB){
	// ignore commanders and flagbear
	if(OB->BrigadeID!=0xFFFF){
		Brigade* Br=CITY[OB->NNUM].Brigs+OB->BrigadeID;
		if(OB->BrIndex<3||OB->Ref.General->UsualFormID!=0xFFFF) return; //OB->NIndex!=Br->MembID
	}
	if(OB->NNUM!=7||!(OB->NewBuilding||OB->Stuff==0xFFFF)||OB->BrigadeID!=0xFFFF){
		int pos=SelPoint.GetAmount();
		static vui_SelPoint SP;	
		SP.Init(OB);
		for(int i=0;i<pos;i++){
			int cmp=SelPoint[i].Cmp(&SP);
			if(cmp<0){
				pos=i;
				break;
			}else
				if(cmp==0){
					SelPoint[i].Add(&SP);
					pos=-1;
					break;
				};
		}
		if(pos>=0){
			if(SP.Type==ve_UT_brigade)SP.Inf.Brigade.SetFromBrig(&SP);
			SelPoint.Insert(pos,SP);
		}
	}	
	///return;
	//if(OB->NNUM==7&&OB->Stuff==0xFFFF) return;
	SelNation=OB->NNUM;
	word BID=OB->BrigadeID;
	word NIndex=OB->NIndex;
	if(OB->newMons->DamWeap[1]){
		if(!OB->RifleAttack)RifleAttEnabled=0;
		RifleAttackAllowed=1;
	}
	if(BID!=0xFFFF){
		for(int i=0;i<NSelBr;i++){
			if(SelBr[i]==BID) return;
		}
		if(NSelBr>=MaxSelBr){
			MaxSelBr=NSelBr+32;
			SelBr=(word*)realloc(SelBr,MaxSelBr<<1);
		}
		SelBr[NSelBr]=BID;
		NSelBr++;
	}else{
		if(OB->NewBuilding){
			if(Settlement==0xFFFF&&OB->Stuff!=0xFFFF){
				Settlement=OB->Index;
				return;
			}
			int n=Bld.GetAmount();
			OIS_Bld* bld=NULL;
			for(int i=0;i<n;i++){
				if(Bld[i]->NIndex==OB->NIndex){
					bld=Bld[i];
					break;
				}
			}			
			if(!bld){
				bld=new OIS_Bld;
				bld->NIndex=OB->NIndex;
				Bld.Add(bld);
			}
			bld->ID.Add(OB->Index);
		}else{
			if(Oboz==0xFFFF&&OB->Stuff!=0xFFFF){
				Oboz=OB->Index;
				return;
			}
			for(int i=0;i<NSelObj;i++){
				if(SelObj[i]==NIndex){
					SelObjA[i]++;
					return;
				}
			}
			if(NSelObj>=MaxSelObj){
				MaxSelObj=NSelObj+32;
				SelObj=(word*)realloc(SelObj,MaxSelObj<<1);
				SelObjA=(word*)realloc(SelObjA,MaxSelObj<<1);
			}
			SelObj[NSelObj]=NIndex;
			SelObjA[NSelObj]=1;
			NSelObj++;
		}
	}
};
word OISelection::GetNIndex(word SelPointID){
	if(SelPointID<SelPoint.GetAmount()){
		return SelPoint[SelPointID].NIndex;
	}
	return 0;
};
GeneralObject* OISelection::GetGeneralObject(word SelPointID){
	if(SelPointID<SelPoint.GetAmount()){
		byte NI=GSets.CGame.cgi_NatRefTBL[MyNation];
		return NATIONS[NI].Mon[SelPoint[SelPointID].NIndex];
	}
	return NULL;
};
GeneralObject* OISelection::GetGeneralObject(ParentFrame* PF){
	if(PF){
		return GetGeneralObject(((SimpleDialog*)PF)->ID);
	}
	return NULL;
};
vui_SelPoint* OISelection::GetSelPoint(ParentFrame* PF){
	if(PF){
		word id=((SimpleDialog*)PF)->ID;
		if(id<SelPoint.GetAmount()){
			return SelPoint+id;
		}		
	}
	return NULL;
};
vui_SelPoint* OISelection::GetLastSelPoint(){
	if(SelPoint.GetAmount()>0&&OIS.LastSP>=0){
		if(OIS.LastSP<SelPoint.GetAmount())return SelPoint+LastSP;
		else return &SelPoint[0];
	}
	return NULL;
};
bool OISelection::SetLastSP(word LSP){
	if(LSP<SelPoint.GetAmount()){
		LastSP=LSP;
		return true;
	}
	LastSP=0;
	return false;
};
vui_ProduceInfo* OISelection::GetProduceInfo(word ID){
	if(ID<Produce.GetAmount()){
		return Produce+ID;
	}
	return NULL;
};
vui_UpgradeInfo* OISelection::GetUpgradeInfo(word ID){
	if(ID<Upgrade.GetAmount()){
		return Upgrade+ID;
	}
	return NULL;
};

int OISelection::GetUpgradeAmount(void){
	return Upgrade.GetAmount();
}
bool vui_BrigInfo::SetFromBrig(vui_SelPoint* SP){
	byte NI=SP->NI;
	BR=CITY[NI].Brigs+BrigID;
	if(BR->WarType==0) return false;
	GeneralObject* GO=NATIONS[NI].Mon[BR->MembID];
	NewMonster* NM=GO->newMons;
	AdvCharacter* ADC=GO->MoreCharacter;
	int L=0;
	int N=0;
	int NS=0;
	int T=0;
	RifleAttack=0;
	int Delay=0;
	int MaxDelay=0;
	//ActiveState=0;
	//MaxLife=GO->MoreCharacter->BirthLife;
	MaxLife=0;
	SP->rX=0;
	SP->rY=0;
	for(int i=NBPERSONAL;i<BR->NMemb;i++){
		word MID=BR->Memb[i];
		if(MID!=0xFFFF){
			OneObject* OB=Group[MID];
			if(OB){
				L+=OB->Life;
				N++;
				if(!OB->delay)NS++;
				T+=OB->GetTired/1000;
				if(OB->RifleAttack)RifleAttack=1;
				Delay+=OB->delay;
				MaxDelay+=OB->MaxDelay;
				OIS.ActiveState|=(1<<OB->ActivityState);
				if(OB->MaxLife>MaxLife){
					MaxLife=OB->MaxLife;
				}
				SP->rX+=OB->RealX;
				SP->rY+=OB->RealY;
			}
		}
	}
	if(N){
		L/=N;
		T/=N;
		Delay/=N;
		MaxDelay/=N;
		NLiveMembers=N;
		Life=L;
		SP->rX/=N;
		SP->rY/=N;
		//ReadyPercent=NS*100/N;
	}else{
		NLiveMembers=1;
		Life=1;
		MaxDelay=1;
		//ReadyPercent=0;
	}		
	ReadyPercent=MaxDelay?100*(MaxDelay-Delay)/MaxDelay:100;
	NMembers=BR->NMemb-NBPERSONAL;
	if(BR->Morale>=0) Morale=BR->Morale/10000;
		else Morale=0;	
	MaxMorale=BR->MaxMorale/10000;	
	if(MaxLife==0) MaxLife=1;
	NShots=NS;	
	Grenades=BR->NGrenades;
	GrenadesMax=GO->newMons->MaxGrenadesInFormation*NMembers/100;
	Tiring=T;
	isGrenaders=GO->newMons->MaxGrenadesInFormation;
	WeapFileID=NM->BigWeapFile;
	WeapColdSprite=NM->BigColdWeapSprite;
	WeapFireSprite=NM->BigFireWeapSprite;
	Shield=0;//ADC->Shield;
	ShieldAdd=BR->AddShield;
	int dam,adam;

	// extracting max defence add values
	ZeroMemory(MaxDefenceAdd, sizeof(MaxDefenceAdd));
	MaxDefenceAdd[0] = MaxDefenceAdd[1] = MaxDefenceAdd[2] = BR->AddShield;

	BR->GetBrigadeProtection(3,dam,adam);
	Defence[0]=dam;
	DefenceAdd[0]=adam;

	BR->GetBrigadeProtection(0,dam,adam);
	Defence[1]=dam;
	DefenceAdd[1]=adam;

	BR->GetBrigadeProtection(2,dam,adam);
	Defence[2]=dam;
	DefenceAdd[2]=adam;

	BR->GetBrigadeProtection(1,dam,adam);
	Defence[3]=dam;
	DefenceAdd[3]=adam;

	// extracting max damage additional values
	ZeroMemory(MaxDamageAdd, sizeof(MaxDamageAdd));
	MaxDamageAdd[0] = MaxDamageAdd[1] = BR->AddDamage;

	for(int i=0;i<3;i++){
		BR->GetBrigadeDamage(i,dam,adam);
		Damage[i]=dam;
		DamageAdd[i]=i==0?adam:0;
		int wk=ADC->WeaponKind[i];
		WeapType[i]=wk;		
	}
	if(Damage[1]==0){
		Damage[1]=Damage[2];
		DamageAdd[1]=DamageAdd[2];
		WeapType[1]=WeapType[2];
	}
	NKills=BR->GetBrigExp();
	// Fill
	isFillable=BR->InStandGround&&REALTIME>BR->FillDelay;
	// Reform
	byte fID=BR->GetFormIndex();
	memset(FormID,0xFF,sizeof(FormID));
	OrderDescription* ODE=ElementaryOrders+BR->WarType-1;
	if(ODE->GroupID!=0xFF){
		SingleGroup* FGD=FormGrp.Grp+ODE->GroupID;
		for(int j=0;j<FGD->NCommon&&j<3;j++){
			FormID[j]=FGD->IDX[j];
			if(fID==FormID[j]){
				CurForm=j;
			}
		}
	}
	ScaleFactor=BR->ScaleFactor;
	// Shot Line
	for(int p=0;p<3;p++){
		ShotLine[p]=0xFFFF;
	}
	int NA=ODE->NActualLines;
	if(NA&&GO->newMons->ArmAttack){
		int IDXS[3]={-1,-1,-1};
		bool LENB[3]={false,false,false};
		bool LPRS[3]={false,false,false};
		int FL=ODE->FirstActualLine;
		int bp=NBPERSONAL;
		int CP=0;		
		for(p=0;p<NA;p++){		
			int NU=ODE->LineNU[p+FL];
			if(NU){
				if(CP<3){
					IDXS[CP]=p;
					for(int q=0;q<NU;q++){
						if(bp<BR->NMemb){
							word ID=BR->Memb[bp];
							if(ID!=0xFFFF){
								OneObject* OB=Group[ID];
								if(OB&&OB->Serial==BR->MembSN[bp]){
									LPRS[CP]=true;
									if(OB->RifleAttack)LENB[CP]=true;
								};
							};
							bp++;
						};
					};
				};
				CP++;
			};
		}
		if(CP==3){
			for(int p=0;p<3;p++){
				if(LPRS[p]&&IDXS[p]!=-1){
					if(LENB[p]){
						ShotLine[p]=1*8*1024+IDXS[p];
					}else{
						ShotLine[p]=IDXS[p];
					}
				}
			}
		}
	}
	// Stand ground
	BrigDelay=BR->BrigDelay;
	BrigDelayMax=BR->MaxBrigDelay?BR->MaxBrigDelay:1;
	if(!BR->InStandGround&&BrigDelay==0) BrigDelay=BrigDelayMax;
	AttEnm=BR->AttEnm;
	NoOrder=BR->NewBOrder==NULL;	
	return true;
}
//////////////////////////////////////////////////////////////////////////////////
// cva_OIS_Scroll
int oisSX=0;
int oisLx=10000;
int oisSDx=64;
void cva_OIS_Scroll::SetFrameState(SimpleDialog* SD){
	SD->Visible=(OIS.SPSideLx<oisLx)&&(OIS.SelPoint.GetAmount()>1);
}
// cva_OIS_ScrollLeft
bool cva_OIS_ScrollLeft::LeftClick(SimpleDialog* SD){
	oisSX-=oisSDx;
	if(oisLx+oisSX<OIS.SPSideLx) oisSX=OIS.SPSideLx-oisLx;
	return true;
}
// cva_OIS_ScrollRight
bool cva_OIS_ScrollRight::LeftClick(SimpleDialog* SD){
	oisSX+=oisSDx;
	if(oisSX>0) oisSX=0;
	return true;
}
// cva_OIS_Rome
void cva_OIS_Rome::SetFrameState(SimpleDialog* SD){
	DialogsDesk* ddS=ddSingle.Get();
	DialogsDesk* ddM=ddMulti.Get();
	if(ddS&&ddM){		
		int NSP=OIS.SelPoint.GetAmount();
		if(NSP==1){
			ddS->Visible=true;
			ddM->Visible=false;
			OIS.SPSideLx=ddS->GetWidth();
		}else{
			ddS->Visible=false;
			ddM->Visible=true;
			//			
			int d=0;
			//int i=0;
			//
			for(int t=0;t<5;t++){
				if(d<ddM->DSS.GetAmount()){
					DialogsDesk* ddType=(DialogsDesk*)ddM->DSS[d];
					if(d>0){						
						ddType->Setx(ddM->DSS[d-1]->x1);
					}else{
						ddType->Setx(oisSX);
					}
					//GP_TextButton* tbTitle=DD->Find("Title");
					//GPPicture* gpFirst=DD->Find("First");				
					GPPicture* ddFirst=(GPPicture*)ddType->DSS[2];
					int x=ddFirst->Getx();
					int y=ddFirst->Gety();
					int w=ddFirst->GetWidth();
					int h=ddFirst->GetHeight();
					int dW=1;
					int j=2;				
					bool find=false;
					for(int i=0;i<NSP;i++){
						vui_SelPoint* SP=OIS.SelPoint+i;
						if(SP->Sort==t){
							find=true;
							GPPicture* DD=NULL;
							if(j<ddType->DSS.GetAmount()){
								DD=(GPPicture*)ddType->DSS[j];
							}else{
								DD=new GPPicture;
								ddFirst->Copy(DD);
								ddType->DSS.Add(DD);
							}
							if(DD){
								DD->Visible=true;
								DD->ID=i;
								if(j-2<6){
									DD->Setx(x+((j-2)%2)*w);
									DD->Sety(y+((j-2)/2)*h);
								}else{
									dW=(2+(j-8)/3);
									DD->Setx(x+dW*w);
									DD->Sety(y+((j-8)%3)*h);									
								}
							}
							j++;
						}else{							
							//break;
						}
					}					
					for(;j<ddType->DSS.GetAmount();j++){
						ddType->DSS[j]->Visible=false;
					}
					if(find){
						ddType->Visible=true;
						GP_TextButton* tbTitle=(GP_TextButton*)ddType->DSS[1];
						_str txt;
						txt.print("#SPTitle_%d",t);
						tbTitle->SetMessage(GetTextByID(txt.str));
						d++;
						ddType->SetWidth((dW+1)*w+12);
					}
				}			
			}
			if(d>0){
				int Lx=ddM->GetWidth();
				oisLx=ddM->DSS[d-1]->x1-ddM->DSS[0]->x;
				if(oisLx<Lx){
					OIS.SPSideLx=oisLx;
					oisSX=0;
				}else{
					OIS.SPSideLx=Lx;
				}				
			}			
			for(;d<ddM->DSS.GetAmount();d++){
				ddM->DSS[d]->Visible=false;				
			}
			//
		}
	}
}
//
cvs_BrigPanel vBrigPanel;
void SetBrigPanel(cvs_BrigPanel& BP){
	vBrigPanel=BP;
};
void BrigPanelShowAll(){	
	vBrigPanel.Restore();
};
// Bink Voideo Interface Commands for Scripting
//  0 - nothing
//	1 - MOVE motion
//	2 - ATTMOVE motion with attack
//	3 - rifle enable
//	4 - rifle disable
//	5 - melee
//	6 - grenade
//	7 - line 0 enable
//	8 - line 1 enable
//	9 - line 2 enable
// 10 - line 0 disable
// 11 - line 1 disable
// 12 - line 2 disable
// 13 - 
// 14 - 
// 15 - 
// 16 - disband formation
// 17 - fill formation
// 18 - stop
// 20 - 39 - forma tion ID (line,squre,care and e.t.) Nres.dat - [ORDERICONS]
// 40 - resorce not enought coal
// 41 - resorce not enought food
// 42 - army not enought living places (neLivingPlaces)
// 43 - detect enemy far
// 44 - detect enemy near
// 45 - detect enemy very close
// 46 - 
//
// 50 - settlement captured
// 51 - settlement lost
// 60 - 
// 61 - enemy make damage
// 62 - enemy melee
// 63 - enemy cannon make damage
// 64 - enemy cavalery make damage
// 70 - 
// 73 - formation lost (neBrigadeLost)
// 74 - formation defeat enemy (neBrigadeTerminated)
// 75 - game victory
// 76 - game defeat
// 80 - 
// 81 - first select
// 82 - peasant voice
// 90 -
// 92 - detect enemy cannon
// 93 - enemy cannon dispirited
// 94 - my cannon dispirited
// 95 - enemy cannon captured
// 96 - my cannon captured
// 100 - cannon fire yadro
// 101 - cannon fire
// 102 - cannon reload yadro
// 103 - cannon reload kartech
// 104 - cannon autofire on
// 105 - cannon autofire off
// 106 - cannon fill
// 107 - cannon turn
// 108 - cannon move
// 110 -
// 111 - detect enemy brigade near my settlemnt
// 112 - detect enemy settlemnt
// 113 - detect enemy brigade near my town center
// 114 - detect enemy town center
// 115 - detect enemy brigade near my strategic location
// 116 - detect enemy strategic location
// 120 - 
// 121 - my settlement protect last brigade
// 122 - enemy settlement protect last brigade
// 123 - my town center protect last brigade
// 124 - enemy town center protect last brigade
// 125 - my strategic location protect last brigade
// 126 - enemy strategic location protect last brigade

// 
void ACT(int x){ 
	if(GSets.CGame.SilenceMessageEvents) return;
	if(OIS.SelPoint.GetAmount()==1&&OIS.SelPoint[0].Type==ve_UT_brigade){
		LastNI=OIS.SelPoint[0].NI;
		LastBID=OIS.SelPoint[0].Inf.Brigade.BrigID;
		ProcessVideoForBrigade(&VideoSeq,LastNI,LastBID,x,FI_File,1,RealLx-535,RealLy-185,-14,-21,0);
	}
	extern ActiveObjectIndexses ActiveIndexses;
	ActiveIndexses.FormationSituation.FinalStateOrder=x;
	if(x){
		//
		void ExecSoundEvents();
		ExecSoundEvents();
	}
}
//////////////////////////////////////////////////////////////////////////////////