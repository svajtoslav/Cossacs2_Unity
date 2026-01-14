#include "stdheader.h"
#include "Extensions.h"
class OneSelectionType:public BaseClass{
public:
	_str Name;
	int   TextureID;
	int   x;
	int   y;
	int   Lx;
	int   Ly;
	int   CenterX;
	int   CenterY;
	float StandartScale;
	float RotationSpeed;
	SAVE(OneSelectionType);
		REG_AUTO(Name);
		REG_MEMBER(_TextureID,TextureID);
		REG_MEMBER(_int,x);
		REG_MEMBER(_int,y);
		REG_MEMBER(_int,Lx);
		REG_MEMBER(_int,Ly);
		REG_MEMBER(_int,CenterX);
		REG_MEMBER(_int,CenterY);
		REG_MEMBER(_float,StandartScale);
		REG_MEMBER(_float01,RotationSpeed);
	ENDSAVE;
	const char* GetThisElementView(const char*){
		char* s=GetGlobalBuffer();
		sprintf(s,"{tex %s 0 0 %d %d  %d %d %d %d} %s",IRS->GetTextureName(TextureID),Lx,Ly,x,y,x+Lx,y+Ly,Name.str);
		return s;
	}
};
class SelectionTypeList:public BaseClass{
public:
	ClonesArray<OneSelectionType> Selections;
	SAVE(SelectionTypeList);
		REG_AUTO(Selections);
	ENDSAVE;
};
SelectionTypeList SelTypes;
void RegSelEditor(){
	REG_CLASS(OneSelectionType);
	SelTypes.SafeReadFromFile("Dialogs\\SelType.xml");
	AddStdEditor("SelectionTypeEditor",&SelTypes,"Dialogs\\SelType.xml",RCE_DEFAULT);
	Enumerator* E=ENUM.Get("TEXTURE_CURSOR_TYPES");
    E->Clear();
	E->Add("---nothing---",0xFFFFFFFF);
	for(int i=0;i<SelTypes.Selections.GetAmount();i++){
		char* s=SelTypes.Selections[i]->Name.str;
		if(!s)s="NULL";
        E->Add(s,i);
	}
}
int GetSelType(char* name){    
	for(int i=0;i<SelTypes.Selections.GetAmount();i++){
		char* s=SelTypes.Selections[i]->Name.str;
		if(s&&!strcmp(s,name))return i;
	}
	return -1;
}
bool DrawSelPatch(float x,float y,int Type,float ScaleX,float ScaleY,DWORD Color){
	if(Type<SelTypes.Selections.GetAmount()){
		OneSelectionType* ST=SelTypes.Selections[Type];
		float r=0;
		if(ST->RotationSpeed>0.0001){
			static int T=GetTickCount();
			int DT=float(GetTickCount()-T)/10;
			r=DT*ST->RotationSpeed;
		}
		const TextureDescr* td=IRS->GetTextureDescr(ST->TextureID);
		if(td){
			float w=td->getSideX();
			float h=td->getSideY();
			DrawTerrainPatch(x,y,ST->Lx*ScaleX*ST->StandartScale,ST->Ly*ScaleY*ST->StandartScale,r,Rct(float(ST->x)/w,float(ST->y)/h,float(ST->Lx)/w,float(ST->Ly)/h)
				,float( ST->CenterX )/ST->Lx,float( ST->CenterY )/ST->Ly,ST->TextureID,ModDWORD(GPS.GetCurrentDiffuse(),Color),false);
			return true;
		}
	}
}
bool DrawSelPatchDir(float x,float y,byte Dir,int Type,float ScaleX,float ScaleY,DWORD Color){
	if(Type<SelTypes.Selections.GetAmount()){
		OneSelectionType* ST=SelTypes.Selections[Type];
		float r=float(Dir)*360/256;
		if(ST->RotationSpeed>0.0001){
			static int T=GetTickCount();
			int DT=float(GetTickCount()-T)/10;
			r+=DT*ST->RotationSpeed;
		}
		const TextureDescr* td=IRS->GetTextureDescr(ST->TextureID);
		if(td){
			float w=td->getSideX();
			float h=td->getSideY();
			DrawTerrainPatch(x,y,ST->Lx*ScaleX*ST->StandartScale,ST->Ly*ScaleY*ST->StandartScale,r,Rct(float(ST->x)/w,float(ST->y)/h,float(ST->Lx)/w,float(ST->Ly)/h)
				,float( ST->CenterX )/ST->Lx,float( ST->CenterY )/ST->Ly,ST->TextureID,ModDWORD(GPS.GetCurrentDiffuse(),Color),false);
			return true;
		}
	}
}
bool DrawSelPatch(float x,float y,int Type,float Radius,DWORD Color){
	if(Type<SelTypes.Selections.GetAmount()){
		OneSelectionType* ST=SelTypes.Selections[Type];
		if(ST->Lx&&ST->Ly){
			float ScaleX=2*Radius/ST->Lx;
			float ScaleY=2*Radius/ST->Ly;
			DrawSelPatch( x,y,Type,ScaleX,ScaleY,Color );
			return true;
		}
	}
	return false;
}