#include "stdheader.h"
#include "TextVisualElements.h"
bool TVE_UnitPreview::Assign(char* TextCommand){
	if(!strncmp(TextCommand,"UNIT",4)){
        char* s=TextCommand+5;
		for(int i=0;i<NATIONS->NMon;i++)if(!strcmp(NATIONS->Mon[i]->MonsterID,s)){
			GO=NATIONS->Mon[i];
			return true;
		}
	}
	return false;
}
void TVE_UnitPreview::Draw(int x,int y,int Lx,int Ly,DWORD Color){
	if(GO){
		TempWindow TW;
		PushWindow(&TW);
		IntersectWindows(x,y,x+Lx-1,y+Ly-1);
		GPS.DrawFillRect(x,y,Lx,Ly,0x50000000);
		GPS.FlushBatches();
		NewAnimation* NA=GO->newMons->GetFirstAnimation(anm_MotionL);
		if(!NA)NA=GO->newMons->GetFirstAnimation(anm_Stand);
		if(NA){
			extern bool g_bPerspCamera; 
			bool p=g_bPerspCamera;
			g_bPerspCamera=0;
			Vector3D V=ICam->GetPos();
			Vector3D D=ICam->GetDir();			
			Rct vp=IRS->GetViewPort();
			IRS->SetViewPort(x,y,Lx,Ly);
			int NF=NA->NFrames*256;
			if(!NF)NF=1;
			ICam->SetPos(Vector3D(0,0,16*10));
			ICam->Render();
            NA->DrawAt((GetTickCount()*6)%NF,0,0,0,0,byte(GetTickCount()/64),1.0,0xFFFFFFFF,0,0,NULL);
			GPS.FlushBatches();
			IRS->SetViewPort(vp);
			ICam->SetPos(V);
			ICam->SetDir(D);
			ICam->Render();
			g_bPerspCamera=p;
		}
		PopWindow(&TW);
	}
}
void TVE_UnitPreview::GetBestSize(int FontSizeY,int& Lx,int& Ly,int& LowMarkPos){
	Lx=80;
	Ly=80;
	LowMarkPos=(Ly+FontSizeY)/2;
}
void DrawTexturedBar(float xL,float yL,float W,float H,
					 float txL,float tyL,float tW,float tH,
					 DWORD CLU,DWORD CRU,DWORD CLD,DWORD CRD,
					 int TextureID,int ShaderID);
bool TVE_FacturePreview::Assign(char* TextCommand){
	if(!strncmp(TextCommand,"$tx",2)){
        texid=0;
		texid=IRS->GetTextureID(TextCommand+4);
		return true;
	}
	return false;
};
void TVE_FacturePreview::Draw(int x,int y,int Lx,int Ly,DWORD Color){
	static sh=IRS->GetShaderID("hud");
	DrawTexturedBar(x,y,Lx,Ly-8,0,0,1,1,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF,texid,sh);
};
void TVE_FacturePreview::GetBestSize(int FontSizeY,int& Lx,int& Ly,int& LowMarkPos){
	Lx=128;
	Ly=128;
	LowMarkPos=64;//(Ly+FontSizeY)/2;
};
bool TVE_StdTexturePreview::Assign(char* TextCommand){
	if(!strncmp(TextCommand,"$stdtex",7)){
        texid=0;
		texid=atoi(TextCommand+7);
		return true;
	}
	return false;
};
void TVE_StdTexturePreview::Draw(int x,int y,int Lx,int Ly,DWORD Color){
	static sh=IRS->GetShaderID("hud");
	float tu=float(texid%8)/8.0f;
	float tv=float(texid/8)/8.0f;
	static int tex=IRS->GetTextureID("Textures\\GroundTex.bmp");
	DrawTexturedBar(x,y,Lx,Ly-8,tu,tv,0.124f,0.124f,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF,tex,sh);
};
void TVE_StdTexturePreview::GetBestSize(int FontSizeY,int& Lx,int& Ly,int& LowMarkPos){
	Lx=64;
	Ly=64+8;
	LowMarkPos=(Ly+FontSizeY)/2;
};
bool TVE_ColorRect::Assign(char* TextCommand){
	if(!strncmp(TextCommand,"#bar",4)){
		COLOR=0xFF808080;
		SizeX=20;
		sscanf(TextCommand+4,"%x %d",&COLOR,&SizeX);		
		return true;
	}
	return false;
};
void TVE_ColorRect::Draw(int x,int y,int Lx,int Ly,DWORD Color){
	GPS.DrawFillRect(x,y,SizeX,Ly,COLOR);	
};
void TVE_ColorRect::GetBestSize(int FontSizeY,int& Lx,int& Ly,int& LowMarkPos){
	Lx=SizeX;
	//Ly=24;
	//LowMarkPos=24;
};
bool TVE_TextureRect::Assign(char* TextCommand){
	if((!strncmp(TextCommand,"tex",3))||(!strncmp(TextCommand,"tid",3))){
		char tex[128];
		dx=0;dy=0;lx=18;ly=18;u=0;v=0;u1=1;v1=1;C1=0xFFFFFFFF;C2=0xFFFFFFFF;C3=0xFFFFFFFF;C4=0xFFFFFFFF;ActualWidth=-1;
		sscanf(TextCommand+4,"%s%d%d%d%d%f%f%f%f%X%X%X%X%d",tex,&dx,&dy,&lx,&ly,&u,&v,&u1,&v1,&C1,&C2,&C3,&C4,&ActualWidth);
		if(ActualWidth==-1)ActualWidth=lx;
		if(!strncmp(TextCommand,"tid",3)){
			TextureID=atoi(tex);
		}else{
			TextureID=IRS->GetTextureID(tex);
		}
		return true;
	}
	return false;
};
void TVE_TextureRect::Draw(int x,int y,int Lx,int Ly,DWORD Color){
	static int sh=IRS->GetShaderID("hud");
	const TextureDescr* td=IRS->GetTextureDescr(TextureID);
	if(td){
		float LX=td->getSideX();
		float LY=td->getSideY();
		DrawTexturedBar(x+dx,y+dy,lx,ly,u/LX,v/LY,(u1-u+1)/LX,(v1-v+1)/LY,C1,C2,C3,C4,TextureID,sh);	
	}
}
void TVE_TextureRect::GetBestSize(int FontSizeY,int& Lx,int& Ly,int& LowMarkPos){
	Lx=ActualWidth;
	Ly=ly+4;
	LowMarkPos=(Ly+FontSizeY)/2-2;
};
void RegisterTextVisualElements(){
	REG_CLASS(TextVisualElement);    
	REG_CLASS(TVE_UnitPreview);
	REG_CLASS(TVE_FacturePreview);
	REG_CLASS(TVE_StdTexturePreview);
	REG_CLASS(TVE_ColorRect);
	REG_CLASS(TVE_TextureRect);
}
