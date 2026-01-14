#include "stdheader.h"
#include "LoadSave.h"
#include "Extensions.h"
#include "surface.h"
class RefreshSurface:public BaseFunction{
public:
	void EvaluateFunction(){
		void RefreshWater();
		RefreshWater();
		void MakeAllDirtyGBUF();
		MakeAllDirtyGBUF();
	}
	SAVE(RefreshSurface){
		REG_PARENT(BaseFunction);
	}ENDSAVE;
};
class OneFactureInfo:public BaseClass{
public:	
	RefreshSurface Refresh;
	int   FactureID;
	bool  UseBumpMap;
	float BumpDegree;
	float PBumpDegree;
	int   BumpTextureID;
	float BumpContrast;
	float BumpBrightness;
	int   NormaleTextureID;
	int   PrevBumpTex;
	int   Mapping;
	float UScale;
	float VScale;
	float UShift;
	float VShift;	
	virtual void reset_class(void* ptr){
		((OneFactureInfo*)(ptr))->Init();
	}
	OneFactureInfo(){
		Init();
	}
	void Init(){
		FactureID=IRS->GetTextureID("Textures\\ground\\tex1.bmp");
		Mapping=0;
		BumpTextureID=-1;
		NormaleTextureID=-1;
		UScale=1.0f;
		VScale=1.0f;
		UShift=0.0f;
		VShift=0.0f;
		BumpDegree=1.0f;
		PBumpDegree=1.0f;
		BumpContrast=0.6f;
		BumpBrightness=1.0f;
		PrevBumpTex=-1;
	}
	const char* GetThisElementView(const char* LocalName){
		char* s=GetGlobalBuffer();
		if(UseBumpMap){
			sprintf(s,"{$tx %s}{$tx %s} %s",IRS->GetTextureName(FactureID),IRS->GetTextureName(BumpTextureID),Mapping?"[V]":"[H]");
		}else{
			sprintf(s,"{$tx %s} %s - %s",IRS->GetTextureName(FactureID),Mapping?"[V]":"[H]",IRS->GetTextureName(FactureID));
		}		
		int L=strlen(s);
		for(int i=0;i<L;i++)if(s[i]=='\\')s[i]='/';
		return s;
	}
	SAVE(OneFactureInfo);
		REG_AUTO(Refresh);
		REG_MEMBER(_TextureID,FactureID);
		REG_MEMBER(_bool,UseBumpMap);
		SAVE_SECTION(1);
		REG_MEMBER(_float0_10,BumpDegree);
		REG_MEMBER(_TextureID,BumpTextureID);
		REG_MEMBER(_float01,BumpContrast);
		REG_MEMBER(_float01,BumpBrightness);
		SAVE_SECTION(0xFFFFFFFF);
		REG_ENUM(_index,Mapping,Mapping);
		REG_MEMBER(_float,UScale);
		REG_MEMBER(_float,VScale);
		REG_MEMBER(_float01,UShift);
		REG_MEMBER(_float01,VShift);
	ENDSAVE;
	DWORD GetClassMask(){
		if(UseBumpMap)return 1;
		else return 2;
	}
};
ClonesArray<OneFactureInfo> FacturesList;
class OneTextureInfo:public BaseClass{
public:
	OneTextureInfo(){
		AllowDisplacement=0;
		DisplacementDir=0;
		BumpStyle=0;
		ConstantDispl=0;
		HeightDispl=0;
	}
	int   TexIndex;
	RefreshSurface Refresh;
	bool  AllowDisplacement;
	byte  DisplacementDir;
	byte  BumpStyle;
	float ConstantDispl;
	float HeightDispl;
	_fractal Distribution;
	SAVE(OneTextureInfo);
		REG_AUTO  (Refresh);
		REG_MEMBER(_bool       ,AllowDisplacement);
		REG_ENUM  (_byte_index ,DisplacementDir,DispDir);
		REG_ENUM  (_byte_index ,BumpStyle,BumpStyle);
		REG_MEMBER(_float01    ,ConstantDispl);
		REG_MEMBER(_float01    ,HeightDispl);
        REG_AUTO  (Distribution);
    ENDSAVE;
	const char* GetThisElementView(const char* LocalName){
		static char cc[64];
		sprintf(cc,"{$stdtex%d}",TexIndex);
		return cc;
	}
};
class TexturesOpt:public BaseClass{
public:
	OneTextureInfo Textures[64];
	TexturesOpt(){
		for(int i=0;i<64;i++){
            Textures[i].TexIndex=i;
		}
	}
	SAVE(TexturesOpt);
	for(int i=0;i<64;i++){
        char cc[32];
		sprintf(cc,"Texture%d",i);
		REG_AUTO_EX2(Textures[i],cc);
	}
	ENDSAVE;
};
TexturesOpt Textures;
//Vector3D GetPointDisplacement(int Index,int TextureID){
//	OneTextureInfo* OTI=Textures.Textures+TextureID;
//	if(!OTI->AllowDisplacement){
//		Vector3D V(0,0,0);
//		return V;
//	}
//	float dx,dy,dz;
//	void GetVertexNormal(int Vertex,float& x,float& y,float& z);
//	GetVertexNormal(Index,dx,dy,dz);
//	int vx=(Index%VertInLine)*32;
//	int vy=(Index/VertInLine)*32;
//    float f=OTI->Distribution.GetFValue(vx,vy);
//	switch(OTI->BumpStyle){
//		case 0:
//            f*=2;
//			break;
//		case 1:
//			f*=-2;
//			break;
//		case 3:
//			f=(f-1/2)*2;
//			break;
//	}
//	float sq_norma(float x,float y);
//    f*=sq_norma(dx,dy);
//	float z=THMap[Index];
//    f*=OTI->ConstantDispl*256+OTI->HeightDispl*2*z;	
//	Vector3D V(dx*f,dy*f,OTI->DisplacementDir?0:dz*f);
//	return V;
//}
//extern byte* TexMapEx;
//extern byte* WTexMapEx;
//Vector3D GetPointDisplacement(int Index){
//    int T1=TexMap[Index];
//	int T2=TexMapEx[Index];
//	int W=WTexMapEx[Index];
//	Vector3D V1=GetPointDisplacement(Index,T1);
//	Vector3D V2=GetPointDisplacement(Index,T2);
//	V2*=float(W)/256;
//	V1*=float(255-W)/256;
//	Vector3D V=V2;
//	V+=V1;
//	return V;
//}
Vector3D GetPointDisplacement(int x,int y,int TextureID){
	OneTextureInfo* OTI=Textures.Textures+TextureID;
	if(!OTI->AllowDisplacement){
		Vector3D V(0,0,0);
		return V;
	}
	float dx,dy,dz;
	dx=GetHeight(x+64,y)-GetHeight(x-64,y);
	dy=GetHeight(x,y+64)-GetHeight(x,y-64);
	dz=128;
	float n=sqrt(dx*dx+dy*dy+dz*dz);
	dx/=n;dy/=n;dz/=n;

	float f=OTI->Distribution.GetFValue(x,y);
	switch(OTI->BumpStyle){
		case 0:
			f*=2;
			break;
		case 1:
			f*=-2;
			break;
		case 3:
			f=(f-1/2)*2;
			break;
	}
	float sq_norma(float x,float y);
	f*=sq_norma(dx,dy);
	float z=GetHeight(x,y);
	f*=OTI->ConstantDispl*256+OTI->HeightDispl*2*z;	
	Vector3D V(-dx*f,-dy*f,OTI->DisplacementDir?dz*f:0);
	return V;
}
extern byte* TexMapEx;
extern byte* WTexMapEx;
extern int VertInLine;
Vector3D GetPointDisplacement0(int x,int y){
	int vx=x/32;
	int vy;
	if(vx&1){
        vy=(y+16)/32;
	}else{
		vy=y/32;
	}	
	int Index=vx+vy*VertInLine;
	int T1=TexMap[Index];
	int T2=TexMapEx[Index];
	int W=WTexMapEx[Index];
	Vector3D V1=GetPointDisplacement(x,y,T1);
	Vector3D V2=GetPointDisplacement(x,y,T2);
	V2*=float(W)/256;
	V1*=float(255-W)/256;
	Vector3D V=V2;
	V+=V1;
	return V;
}
Vector3D GetPointDisplacement(int x,int y){
	Vector3D V0=GetPointDisplacement0(x-40,y);
	V0+=GetPointDisplacement0(x+40,y);
	V0+=GetPointDisplacement0(x,y-40);
	V0+=GetPointDisplacement0(x,y+40);
	V0/=4;
	return V0;
}
Vector3D GetTrueCoor(int x,int y){
	Vector3D V=GetPointDisplacement(x,y);
	V+=Vector3D(x,y,GetHeight(x,y));
	return V;
}
int GetLighting3DV(int dx,int dy,int dz);
int GetLighting3D0();
extern byte* LightMap;
int GetPointLighting(int i){
	int x=(i%VertInLine)*32;
	int y=(i/VertInLine)*32;
	Vector3D VL=GetTrueCoor(x-32,y);
	Vector3D VR=GetTrueCoor(x+32,y);
	Vector3D VU=GetTrueCoor(x,y-32);
	Vector3D VD=GetTrueCoor(x,y+32);
	VR-=VL;
	VD-=VU;
	Vector3D VN;
	VN.cross(VR,VL);
	VN.normalize();
	int lig;
	lig=GetLighting3DV(VN.x*256,VN.y*256,VN.z*256);
	if(LightMap){
		int L0;
		if(i>=VertInLine*2&&i<MaxPointIndex-4)L0=LightMap[i-VertInLine];
		else L0=0;
		if(L0<lig)lig=L0-((255-lig)/5);
	};
	static int TL0=-1;
	if(TL0==-1)TL0=GetLighting3D0();
	lig=(lig<<7)/TL0;	
	if(lig<0)lig=0;
	if(lig>255)lig=255;
	return lig;
}
void RegFactEditor(){
	AddStdEditor("Factures",&FacturesList,"FacturesList.xml",
		RCE_RIGHT_POSITION|RCE_SHOW_GAME_BACKGROUND|
		RCE_ALLOW_GAME_PROCESSING|RCE_AUTOSAVE|
		RCE_EXITONESCAPE|RCE_HIDEHEADER);
	AddStdEditor("Textures",&Textures,"textures.xml",
		RCE_RIGHT_POSITION|RCE_SHOW_GAME_BACKGROUND|
		RCE_ALLOW_GAME_PROCESSING|RCE_AUTOSAVE|
		RCE_EXITONESCAPE|RCE_HIDEHEADER);

}
int GetNFactures(){
	return FacturesList.GetAmount();
}
int GetFacture(int id){
	if(id<0||id>=GetNFactures()){
		if(GetNFactures())return FacturesList[0]->FactureID;
		else return 0;
	}
	return FacturesList[id]->FactureID;
}
bool UsesBump(int id){
	if(id<0||id>=GetNFactures()){
		return false;
	}
	return FacturesList[id]->UseBumpMap;
}
int GetFactureBump(int id,float& Contrast,float& Bright){
	Contrast=0;
	if(id<0||id>=GetNFactures()){
		return -1;
	}
	if(!FacturesList[id]->UseBumpMap)return -1;
	if(FacturesList[id]->NormaleTextureID==-1
		||FacturesList[id]->BumpDegree!=FacturesList[id]->PBumpDegree
		||FacturesList[id]->PrevBumpTex!=FacturesList[id]->BumpTextureID){

		if(FacturesList[id]->NormaleTextureID!=-1)IRS->DeleteTexture(FacturesList[id]->NormaleTextureID);
		FacturesList[id]->NormaleTextureID=IRS->CreateNormalMap(FacturesList[id]->BumpTextureID,FacturesList[id]->BumpDegree*2);
		FacturesList[id]->PBumpDegree=FacturesList[id]->BumpDegree;
		FacturesList[id]->PrevBumpTex=FacturesList[id]->BumpTextureID;
	}
	Contrast=FacturesList[id]->BumpContrast;
	Bright=FacturesList[id]->BumpBrightness;
	return FacturesList[id]->NormaleTextureID;
}
const char* GetFactureName(int id){
    id=GetFacture(id);
	return IRS->GetTextureName(id);
}
int GetFactureUsage(int id){
	if(id<0||id>=GetNFactures()){
		return 0;
	}
	return FacturesList[id]->Mapping;
}

Vector3D GetSurfaceN(int x,int y){
	Vector3D V(GetHeight(x-64,y)-GetHeight(x+64,y),GetHeight(x,y-64)-GetHeight(x,y+64),128);
	V.normalize();
	return V;
}
int GetFactureWeightFactor(int x,int y,int id){
    int u=GetFactureUsage(id);
	if(u==0||u==1){
        Vector3D V=GetSurfaceN(x,y);
		if(u==0){
            int v=(V.z-0.7)*1024;
			if(v<0)v=0;
			if(v>255)v=255;
			return v;
		}else{
			int v=(1.0f-V.z)*4096;
			if(v<64)v=0;
			if(v>255)v=255;
			return v;
		}
	}else{
		Vector3D V=GetSurfaceN(x,y);
		int v=(1.0f-V.z)*8192;
		if(v<0)v=0;
		if(v>255)v=255;
		if(v>32){
			V.z=0;
			V.normalize();
            Vector3D V1=GetSurfaceN(x+V.x*10,y+V.y*10);
			Vector3D V2=GetSurfaceN(x-V.x*10,y-V.y*10);
			static int vv1=0;
			static int vv2=350;
			int v1=(fabs(V2.z-V1.z)*256.0f-vv1)*vv2;
			if(v1<0)v1=0;
			if(v1>255)v1=255;
			return v*v1/255;
        }
	}
	return 0;
}
void GetUVInfo(int id,float& du,float& dv,float& su,float& sv){
	du=0;
	dv=0;
	su=1;
	sv=1;
	if(id>=0&&id<GetNFactures()){
        du=FacturesList[id]->UShift;
		dv=FacturesList[id]->VShift;
		su=FacturesList[id]->UScale;
		sv=FacturesList[id]->VScale;
	}
}
extern IRenderSystem*	IRS;
void CheckMemoryIntegrity();
class OneGTexture{
public:
	int TextureID;
	int BumpID;
	byte Usage;
};
int GetHeight(int x,int y);
void GetLightGradient(int& x,int& y){
	int DX=0;
	int DY=0;
	for(int j=1;j<10;j++){
		DX+=(GetHeight(x+j*16,y)-GetHeight(x-j*16,y))*4/j;
		DY+=(GetHeight(x,y+j*16)-GetHeight(x,y-j*16))*4/j;
	};
	x=DX;
	y=DY;
};
float gmax(float x,float y){return x>y?x:y;};
float gmin(float x,float y){return x>y?y:x;};
//float fabs(float f){return f<0?-f:f;};
float fnorm(float x,float y){return sqrt(x*x+y*y);};
float decFunc(float x,float x0,float L){return x<x0?1.0:exp(-(x-x0)/L);};
class Ground3D{
public:
	word*  TexID;
	byte*  TexWeight;
	//byte*  TexOpt;
	short*  Height;
	short* Options;

	int NItems;
	int SizeX;
	int SizeY;
	OneGTexture GTex[40];
	int NGTex;
	BaseMesh* SqMesh;
	int tempTexID;

	void LoadTexture(int idx,int dest,byte opt){
		char cc[64];
		sprintf(cc,"Textures\\Ground\\tex%d.bmp",idx);
		GTex[dest].TextureID=IRS->GetTextureID(cc);
		GTex[dest].Usage=opt;
		//sprintf(cc,"Ground\\tex%dbump.bmp",idx);
		//GTex[dest].BumpID=IRS->GetTextureID(cc);
		GTex[dest].BumpID=0;
	};
	void Create(int MapSizeX,int MapSizeY);
	void Setup();
	void Clear(){
		if(TexID)delete[] TexID;
		if(TexWeight)delete[] TexWeight;
		if(Height)delete[] Height;
		if(Options)delete[] Options;
		TexID=NULL;
		TexWeight=NULL;
		Height=NULL;
		Options=NULL;
	};
	void Render(int dstX,int dstY,int dstDevice,int x,int y,int Lx,int Ly);
	__forceinline void GetTexUV(float& u,float& v,int x,int y,byte Use,byte opt,int offset){
		if(Use==0){//planar mapping
			u=float(x<<5)/256.0+float(opt&15)/16.0;
			v=float((y<<5)+Height[offset])/256.0+float(opt&15)/16.0;
		}else if(Use==1){
			u=float(x<<5)/256.0+float(opt&15)/16.0-float(Options[offset])*0.02;
			v=-float(Height[offset])/180.0+float(opt&15)/16.0;
		};
	};
	float GetMaxPointWeight(int ix,int iy,int Tex){
		int ofs=ix+iy*SizeX;
		float u,v,uL,vL,uR,vR,uU,vU,uD,vD;
		int TID=Tex&0xFF;
		int Opt=GTex[TID].Usage;
		GetTexUV(u,v,ix,iy,Opt,0,ofs);
		GetTexUV(uL,vL,ix-1,iy,Opt,0,ofs-1);
		GetTexUV(uR,vR,ix-1,iy,Opt,0,ofs+1);
		GetTexUV(uU,vU,ix,iy-1,Opt,0,ofs-SizeX);
		GetTexUV(uD,vD,ix,iy+1,Opt,0,ofs+SizeX);
		float nu1=fnorm(uL-u,uD-u);
		float nu2=fnorm(uR-u,uD-u);
		float nu3=fnorm(uL-u,uU-u);
		float nu4=fnorm(uR-u,uU-u);

		float nv1=fnorm(vL-v,vD-v);
		float nv2=fnorm(vR-v,vD-v);
		float nv3=fnorm(vL-v,vU-v);
		float nv4=fnorm(vR-v,vU-v);
		 
		float Min=gmin(gmin(gmin(nu1,nu2),gmin(nu3,nu4)),gmin(gmin(nv1,nv2),gmin(nv3,nv4)));
		if(Min==0)Min=0.001;
		float DM=decFunc(1/Min,15,3);
		int MAXW=255;
		if(DM<1){
			int W=255*DM;
			MAXW=W;
		};
		float Max=gmax(gmax(gmax(nu1,nu2),gmax(nu3,nu4)),gmax(gmax(nv1,nv2),gmax(nv3,nv4)));
		DM=decFunc(Max,0.38,0.2);
		if(DM<1){
			int W=255*DM;
			if(MAXW>W)MAXW=W;
		};
		return MAXW;
	};
	void TestPointWeight(int ix,int iy){
		if(ix>0&&ix<SizeX-1&&iy>0&&iy<SizeY-1){
			int ofs=ix+iy*SizeX;
			int TID=TexID[ofs];
			int W;
			if(W=TexWeight[ofs]){
				int MW=GetMaxPointWeight(ix,iy,TID);
				if(W>MW)TexWeight[ofs]=MW;
			};
		};
	};
	void SetFactureRound(int x,int y,int r,int dr,int TexID,int Br);
	void ClearFactureRound(int x,int y,int r,int dr);
	Ground3D(){
		memset(this,0,sizeof *this);
	};
	~Ground3D(){
		Clear();
	};
};
int ConvScrY(int x,int y);
void MakeDirtyGBUF(int mx,int my,int sx,int sy);
void Ground3D::SetFactureRound(int x,int y,int r,int dr,int Tex,int Brg){
	Brg=Brg*255/100;
	int TrueID=Tex&255;
	byte Opt=GTex[TrueID].Usage;
	int x0=(x-r-dr-64)>>5;
	int y0=(y-(r+dr)/2-64)>>5;
	int x1=(x+r+dr+64)>>5;
	int y1=(y+(r+dr)/2+64)>>5;
	int Brg2=Brg-(Brg>>3);
	if(x0<0)x0=0;
	if(y0<0)y0=0;
	if(x1>=SizeX)x1=SizeX-1;
	if(y1>=SizeY)y1=SizeY-1;
	int SH=140;
	if(Opt==1){
		//calculating options
		int x00=x0-3;
		int y00=y0-3;
		int x11=x1+3;
		int y11=y1+3;
		if(x00<0)x00=0;
		if(y00<0)y00=0;
		if(x11>=SizeX)x11=SizeX-1;
		if(y11>=SizeY)y11=SizeY-1;
		for(int ix=x00;ix<=x11;ix++){
			for(int iy=y00;iy<=y11;iy++){
				int ofs=ix+iy*SizeX;
				int RX=ix<<5;
				int RY=ConvScrY(RX,iy<<5)<<1;
				int SY=iy<<5;
				int H=GetHeight(RX,RY);
				int ZR=GetHeight(RX+32,RY)+GetHeight(RX+64,RY)+GetHeight(RX+96,RY)+GetHeight(RX+128,RY);
				int ZL=GetHeight(RX-32,RY)+GetHeight(RX-64,RY)+GetHeight(RX-96,RY)+GetHeight(RX-128,RY);
				int ZU=GetHeight(RX,RY-32)+GetHeight(RX,RY-64)+GetHeight(RX,RY-96)+GetHeight(RX,RY-128);
				int ZD=GetHeight(RX,RY+32)+GetHeight(RX,RY+64)+GetHeight(RX,RY+96)+GetHeight(RX,RY+128);
				float NX=ZR-ZL;
				float NY=ZU-ZD;
				float NZ=64;
				float N=sqrt(NX*NX+NY*NY);
				int Nt=sqrt(NX*NX+NY*NY+NZ*NZ);
				NX/=N;
				NY/=N;
				NZ/=Nt;
				Options[ofs]=(((H-SH)*NX)/10)*NZ*10;
			};
		};
	};

	for(int ix=x0;ix<=x1;ix++){
		int SH=0;
		int NSH=0;
		for(int iy=y0;iy<=y1;iy++){
			int ofs=ix+iy*SizeX;
			int RX=ix<<5;
			int RY=ConvScrY(RX,iy<<5)<<1;
			int RYL=ConvScrY(RX-1,iy<<5)<<1;
			int RYR=ConvScrY(RX-1,iy<<5)<<1;
			int SY=iy<<5;
			int H=GetHeight(RX,RY);
			Height[ofs]=H;
			int R=sqrt((RX-x)*(RX-x)+(SY-y)*(SY-y)*4);
			int W;
			if(R<r)W=Brg;
			else if(R<r+dr)W=Brg*(r+dr-R)/dr;
			else W=0;
			if(W){
				int MAXW=GetMaxPointWeight(ix,iy,Tex);
				if(W>MAXW)W=MAXW;
			};
			if(W&&(W>=TexWeight[ofs]||W>Brg2)){
				if(W>255)W=255;
				if(W<0)W=0;
				TexWeight[ofs]=W;
				TexID[ofs]=Tex;
			};
		};
	};
	MakeDirtyGBUF(((x-r-dr)>>5)-5,((y-(r+dr)/2)>>4)-5,((r+dr)>>4)+10,((r+dr)>>4)+10);
};
void Ground3D::ClearFactureRound(int x,int y,int r,int dr){
	int x0=(x-r-dr-256)>>5;
	int y0=(y-(r+dr)/2-256)>>5;
	int x1=(x+r+dr+256)>>5;
	int y1=(y+(r+dr)/2+256)>>5;

	if(x0<0)x0=0;
	if(y0<0)y0=0;
	if(x1>=SizeX)x1=SizeX-1;
	if(y1>=SizeY)y1=SizeY-1;

	for(int ix=x0;ix<=x1;ix++){
		for(int iy=y0;iy<=y1;iy++){
			int ofs=ix+iy*SizeX;
			int RX=ix<<5;
			int SY=iy<<5;
			int R=sqrt((RX-x)*(RX-x)+(SY-y)*(SY-y)*4);
			int W;
			if(R<r)W=255;
			else if(R<r+dr)W=255*(r+dr-R)/dr;
			else W=0;
			W=255-W;
			if(W<TexWeight[ofs]){
				TexWeight[ofs]=W;
			};
		};
	};
	MakeDirtyGBUF(((x-r-dr)>>5)-5,((y-(r+dr)/2)>>4)-5,((r+dr)>>4)+10,((r+dr)>>4)+10);
};
void Ground3D::Setup(){
	//loading textures
	for(int i=0;i<40;i++)LoadTexture(i+1,i,0);
	for(i=9;i<19;i++)GTex[i].Usage=1;
	GTex[34].Usage=1;
	GTex[35].Usage=1;
	NGTex=40;
	GFILE* F=Gopen("Factures.dat","r");
	if(F){
		for(int i=0;i<40;i++){
			int us=0;
			Gscanf(F,"%d",&us);
			GTex[i].Usage=us;
		};
		Gclose(F);
	};
};
void Ground3D::Create(int MapSizeX,int MapSizeY){
	SizeX=MapSizeX;
	SizeY=MapSizeY>>1;
	NItems=SizeX*SizeY;
	Clear();
	TexID=new word[NItems];
	memset(TexID,0,2*NItems);
	TexWeight=new byte[NItems];
	memset(TexWeight,0,NItems);
	Height=new short[NItems];
	memset(Height,0,2*NItems);
	Options=new short[NItems];
	memset(Options,0,2*NItems);
};
void LoadFactures(){
	REG_CLASS(OneFactureInfo);
	bool CheckIfFileExists(char* Name);
	if(CheckIfFileExists("FacturesList.xml")){
        FacturesList.reset_class(&FacturesList);
		FacturesList.SafeReadFromFile("FacturesList.xml");
		Textures.SafeReadFromFile("Textures.xml");
	}else{
		//loading textures
		FacturesList.Clear();
		for(int i=0;i<40;i++){
			OneFactureInfo* OFU=new OneFactureInfo;
            char cc[64];
			sprintf(cc,"Textures\\ground\\tex%d.bmp",i+1);
			OFU->FactureID=IRS->GetTextureID(cc);
            FacturesList.Add(OFU);
		}
		GFILE* F=Gopen("Factures.dat","r");
		if(F){
			for(int i=0;i<40;i++){
				int us=0;
				Gscanf(F,"%d",&us);
				FacturesList[i]->Mapping=us;
			};
			Gclose(F);
		};
		FacturesList.WriteToFile("FacturesList.xml");
	}
}
extern int RealLx;
extern int RealLy;
void Ground3D::Render(int dstX,int dstY,int dstDevice,int x,int y,int Lx,int Ly){
	//IRS->SaveTexture(dstDevice,"0src.dds");
	//some clipping
	return;
	IRS->SetRenderTarget(dstDevice);

	int cx=x>>5;
	int cy=y>>5;
	//if(x&31)cx--;
	//if(y&31)cy--;
	int x1=x+Lx;
	int y1=y+Ly;
	int cx1=x1>>5;
	int cy1=y1>>5;
	if(x1&31)cx1++;
	if(y1&31)cy1++;
	if(cx<0)cx=0;
	if(cy<0)cy=0;
	if(cx1>=SizeX)cx1=SizeX-1;
	if(cy1>=SizeY)cy1=SizeY-1;
	//TempWindow TW;
	//PushWindow(&TW);
	//IntersectWindows(dstX,dstY,dstX+Lx-1,dstY+Ly-1);
	Rct vp;
	vp.x=dstX;
	vp.y=dstY;
	vp.w=Lx;
	vp.h=Ly;
	IRS->SetViewPort(vp);
	dstX+=(cx<<5)-x;
	dstY+=(cy<<5)-y;
	//at least rendering
	if(!SqMesh){
		//temp!!!
		Create(512,512);
		//-------
		if(!NGTex)Setup();
		SqMesh=new BaseMesh;
		SqMesh->create(5,12,vfTnL);
		word* IND=SqMesh->getIndices();
		IND[0]=0;
		IND[1]=1;
		IND[2]=4;

		IND[3]=1;
		IND[4]=3;
		IND[5]=4;

		IND[6]=3;
		IND[7]=2;
		IND[8]=4;

		IND[9]=2;
		IND[10]=0;
		IND[11]=4;

		VertexTnL* vert = (VertexTnL*)SqMesh->getVertexData();
		vert[0].w = vert[1].w = vert[2].w = vert[3].w = vert[4].w= 1.0f;
		/*
		TextureDescr TD;
		TD.setValues(32,32,cfBackBufferCompatible,mpVRAM,1,tuProcedural);
		tempTexID=IRS->CreateTexture("tempTexID",TD);
		*/
		SqMesh->setNInd(12);
		SqMesh->setNPri(4);
		SqMesh->setNVert(5);
		SqMesh->setShader(IRS->GetShaderID("Facture4"));

	};
	VertexTnL* VER =(VertexTnL*)SqMesh->getVertexData();
	/*
	VER[0].u=0;
	VER[1].u=1.0;
	VER[2].u=0;
	VER[3].u=1.0;
	VER[0].v=0;
	VER[1].v=0;
	VER[2].v=1.0;
	VER[3].v=1.0;

	VER[4].u=0.5;
	VER[4].v=0.5;
	*/
	int BBID=IRS->GetVBufferID();
	for(int iy=cy;iy<cy1;iy++){
		int ofs=iy*SizeX+cx;
		for(int ix=cx;ix<cx1;ix++){
			byte w1=TexWeight[ofs];
			byte w2=TexWeight[ofs+1];
			byte w3=TexWeight[ofs+SizeX];
			byte w4=TexWeight[ofs+SizeX+1];

			if(w1||w2||w3||w4){
				word T1=TexID[ofs];
				word T2=TexID[ofs+1];
				word T3=TexID[ofs+SizeX];
				word T4=TexID[ofs+SizeX+1];

				VER[0].x=dstX+((ix-cx)<<5);
				VER[0].y=dstY+((iy-cy)<<5);
				VER[0].z=0;

				VER[1].x=VER[0].x+32;
				VER[1].y=VER[0].y;
				VER[1].z=0;

				VER[2].x=VER[0].x;
				VER[2].y=VER[0].y+32;
				VER[2].z=0;

				VER[3].x=VER[0].x+32;
				VER[3].y=VER[0].y+32;
				VER[3].z=0;

				VER[4].x=VER[0].x+16;
				VER[4].y=VER[0].y+16;
				VER[4].z=0;

				RECT RC;
				RC.left=VER[0].x;
				RC.top=VER[0].y;
				RC.right=RC.left+32;
				RC.bottom=RC.top+32;

				POINT PP;
				PP.x=0;
				PP.y=0;

				byte DMASK=0xFF;

				if(w1){
					//IRS->CopyTexture(tempTexID,BBID,&RC,1,&PP);

					VER[0].diffuse=(w1<<24)+0xFFFFFF;
					VER[1].diffuse=T1==T2?(w2<<24)+0xFFFFFF:0xFFFFFF;
					VER[2].diffuse=T1==T3?(w3<<24)+0xFFFFFF:0xFFFFFF;
					VER[3].diffuse=T1==T4?(w4<<24)+0xFFFFFF:0xFFFFFF;

					VER[4].diffuse=((((VER[0].diffuse>>24)+(VER[1].diffuse>>24)+(VER[2].diffuse>>24)+(VER[3].diffuse>>24))>>2)<<24)+0xFFFFFF;

					//texture cooprdinates
					byte use=GTex[T1&255].Usage;
					int ID=GTex[T1&255].TextureID;
					GetTexUV(VER[0].u,VER[0].v,ix,iy,use,T1>>8,ofs);
					GetTexUV(VER[1].u,VER[1].v,ix+1,iy,use,T1>>8,ofs+1);
					GetTexUV(VER[2].u,VER[2].v,ix,iy+1,use,T1>>8,ofs+SizeX);
					GetTexUV(VER[3].u,VER[3].v,ix+1,iy+1,use,T1>>8,ofs+SizeX+1);

					VER[4].u=(VER[0].u+VER[1].u+VER[2].u+VER[3].u)/4;
					VER[4].v=(VER[0].v+VER[1].v+VER[2].v+VER[3].v)/4;

					SqMesh->setTexture(ID);
					//SqMesh->setTexture(tempTexID);
					IRS->Draw(*SqMesh);
					if(ix==cx&&iy==cy){
						//IRS->SaveTexture(dstDevice,"01.dds");
						//IRS->SaveTexture(tempTexID,"0t.dds");
					};
					if(T1==T2)DMASK&=~2;
					if(T1==T3)DMASK&=~4;
					if(T1==T4)DMASK&=~8;
				};
				if(w2&&(DMASK&2)){
					//IRS->CopyTexture(tempTexID,BBID,&RC,1,&PP);

					VER[0].diffuse=T2==T1?(w1<<24)+0xFFFFFF:0xFFFFFF;
					VER[1].diffuse=(w2<<24)+0xFFFFFF;
					VER[2].diffuse=T2==T3?(w3<<24)+0xFFFFFF:0xFFFFFF;
					VER[3].diffuse=T2==T4?(w4<<24)+0xFFFFFF:0xFFFFFF;

					VER[4].diffuse=((((VER[0].diffuse>>24)+(VER[1].diffuse>>24)+(VER[2].diffuse>>24)+(VER[3].diffuse>>24))>>2)<<24)+0xFFFFFF;

					//texture cooprdinates
					byte use=GTex[T2&255].Usage;
					int ID=GTex[T2&255].TextureID;
					GetTexUV(VER[0].u,VER[0].v,ix,iy,use,T2>>8,ofs);
					GetTexUV(VER[1].u,VER[1].v,ix+1,iy,use,T2>>8,ofs+1);
					GetTexUV(VER[2].u,VER[2].v,ix,iy+1,use,T2>>8,ofs+SizeX);
					GetTexUV(VER[3].u,VER[3].v,ix+1,iy+1,use,T2>>8,ofs+SizeX+1);

					VER[4].u=(VER[0].u+VER[1].u+VER[2].u+VER[3].u)/4;
					VER[4].v=(VER[0].v+VER[1].v+VER[2].v+VER[3].v)/4;

					SqMesh->setTexture(ID);
					//SqMesh->setTexture(tempTexID);
					IRS->Draw(*SqMesh);
					if(T2==T3)DMASK&=~4;
					if(T2==T4)DMASK&=~8;
				};
				if(w3&&(DMASK&4)){
					//IRS->CopyTexture(tempTexID,BBID,&RC,1,&PP);

					VER[0].diffuse=T3==T1?(w1<<24)+0xFFFFFF:0xFFFFFF;
					VER[1].diffuse=T3==T2?(w1<<24)+0xFFFFFF:0xFFFFFF;
					VER[2].diffuse=(w3<<24)+0xFFFFFF;
					VER[3].diffuse=T3==T4?(w4<<24)+0xFFFFFF:0xFFFFFF;

					VER[4].diffuse=((((VER[0].diffuse>>24)+(VER[1].diffuse>>24)+(VER[2].diffuse>>24)+(VER[3].diffuse>>24))>>2)<<24)+0xFFFFFF;

					//texture cooprdinates
					byte use=GTex[T3&255].Usage;
					int ID=GTex[T3&255].TextureID;
					GetTexUV(VER[0].u,VER[0].v,ix,iy,use,T3>>8,ofs);
					GetTexUV(VER[1].u,VER[1].v,ix+1,iy,use,T3>>8,ofs+1);
					GetTexUV(VER[2].u,VER[2].v,ix,iy+1,use,T3>>8,ofs+SizeX);
					GetTexUV(VER[3].u,VER[3].v,ix+1,iy+1,use,T3>>8,ofs+SizeX+1);

					VER[4].u=(VER[0].u+VER[1].u+VER[2].u+VER[3].u)/4;
					VER[4].v=(VER[0].v+VER[1].v+VER[2].v+VER[3].v)/4;

					SqMesh->setTexture(ID);
					//SqMesh->setTexture(tempTexID);
					//IRS->SaveTexture(IRS->GetVBufferID(),"001.dds");
					IRS->Draw(*SqMesh);
					//IRS->SaveTexture(IRS->GetVBufferID(),"002.dds");
					if(T3==T4)DMASK&=~8;
				};
				if(w4&&(DMASK&8)){
					//IRS->CopyTexture(tempTexID,BBID,&RC,1,&PP);
					//IRS->SaveTexture(tempTexID,"000.dds");

					VER[0].diffuse=T4==T1?(w1<<24)+0xFFFFFF:0xFFFFFF;
					VER[1].diffuse=T4==T2?(w1<<24)+0xFFFFFF:0xFFFFFF;
					VER[2].diffuse=T4==T3?(w3<<24)+0xFFFFFF:0xFFFFFF; 
					VER[3].diffuse=(w4<<24)+0xFFFFFF;

					VER[4].diffuse=((((VER[0].diffuse>>24)+(VER[1].diffuse>>24)+(VER[2].diffuse>>24)+(VER[3].diffuse>>24))>>2)<<24)+0xFFFFFF;

					//texture cooprdinates
					byte use=GTex[T4&255].Usage;
					int ID=GTex[T4&255].TextureID;
					GetTexUV(VER[0].u,VER[0].v,ix,iy,use,T4>>8,ofs);
					GetTexUV(VER[1].u,VER[1].v,ix+1,iy,use,T4>>8,ofs+1);
					GetTexUV(VER[2].u,VER[2].v,ix,iy+1,use,T4>>8,ofs+SizeX);
					GetTexUV(VER[3].u,VER[3].v,ix+1,iy+1,use,T4>>8,ofs+SizeX+1);

					VER[4].u=(VER[0].u+VER[1].u+VER[2].u+VER[3].u)/4;
					VER[4].v=(VER[0].v+VER[1].v+VER[2].v+VER[3].v)/4;

					SqMesh->setTexture(ID);
					//SqMesh->setTexture(tempTexID);
					//IRS->SaveTexture(ID,"4id.dds");
					//IRS->SaveTexture(IRS->GetVBufferID(),"001.dds");
					IRS->Draw(*SqMesh);
					//IRS->SaveTexture(IRS->GetVBufferID(),"002.dds");
				};
			};
			ofs++;
		};
	};
	
	//PopWindow(&TW);
	vp.x=0;
	vp.y=0;
	vp.w=RealLx;
	vp.h=RealLy;
	IRS->SetViewPort(vp);
	IRS->SetRenderTarget(0);
	//IRS->SaveTexture(dstDevice,"0fin.dds");
	
};
Ground3D F3D;
void Draw3DFactures(int dstX,int dstY,int dstDevice,int x,int y,int Lx,int Ly){
	//F3D.Render(dstX,dstY,dstDevice,x,y,Lx,Ly);
};
void DrawNewFacture(int x,int y,int r,int dr,int Tex,int Bright){
	//F3D.SetFactureRound(x,y,r,dr,Tex,Bright);
};
void ClearNewFacture(int x,int y,int r,int dr){
	//F3D.ClearFactureRound(x,y,r,dr);
};

//DRAWING 3D SURFACE!
byte* LightMap=NULL;
int GetTotalHeight(int x,int y);
void ScanLightOffset(int X0,int Y0){
	int DD=10;
	int HMax=0;
	int ofs=X0+Y0*VertInLine;
	int HP=0;
	int H=0;
	while(X0>=0&&Y0>=0){
		HP=H;
		//H=THMap[ofs];
		int x=(ofs%VertInLine);
		int y=(ofs/VertInLine);
		y=x&1?(y<<5)-16:y<<5;
		x=x<<5;
		H=GetTotalHeight(x,y);
		if(H>HMax)HMax=H;
		int DH=HMax-H;
		if(DH>0){
			DH*=2+abs(HP-H)/2;
			if(DH>100)DH=100;
			LightMap[ofs]=255-DH;
		}else LightMap[ofs]=255;
		if(X0&1){
			ofs-=VertInLine+1;
			Y0--;
		}else ofs--;
		X0--;
		HMax-=DD;
	};

};
void CreateLightMap(){
	byte* TempL=(byte*)malloc(VertInLine*MaxTH);
	if(!LightMap)LightMap=(byte*)malloc(VertInLine*MaxTH);
	memset(TempL,255,VertInLine*MaxTH);
	//scanning process
	for(int y=MaxTH-1;y>0;y--)ScanLightOffset(VertInLine-1,y);
	for(int x=0;x<VertInLine-1;x++)ScanLightOffset(x,MaxTH-1);
	for(int t=0;t<1;t++){
		int ofs=0;
		for(int iy=0;iy<MaxTH;iy++){
			for(int ix=0;ix<VertInLine;ix++){
				if(ix>0&&iy>0&&ix<VertInLine-3&&iy<MaxTH-3){
					int S=0;
					if(ix&1){
						TempL[ofs]=(int(LightMap[ofs+VertInLine])+int(LightMap[ofs])+int(LightMap[ofs-1])+int(LightMap[ofs+1])+int(LightMap[ofs-VertInLine-1])+int(LightMap[ofs-VertInLine])+int(LightMap[ofs-VertInLine+1]))/7;
					}else{
						TempL[ofs]=(int(LightMap[ofs-VertInLine])+int(LightMap[ofs])+int(LightMap[ofs-1])+int(LightMap[ofs+1])+int(LightMap[ofs+VertInLine-1])+int(LightMap[ofs+VertInLine])+int(LightMap[ofs+VertInLine+1]))/7;
					};
				};
				ofs++;
			};
		};
		memcpy(LightMap,TempL,VertInLine*MaxTH);
	};
	free(TempL);
};
int GetLighting(int i);
DWORD TColors[]={0xFF4A6F31,0xFF8F8872};
BaseMesh* STriang=NULL;
BaseMesh* GroundMesh=NULL;
BaseMesh* StoneMesh=NULL;
int GetHi(int i);
#define TriUnit		16
extern int LightDX;
extern int LightDY;
extern int LightDZ;
int TL0=-1;
int GetLighting3D0(){
	int dy=0;
	int dx=0;
	int dz=TriUnit+TriUnit+TriUnit+TriUnit;
	int lig=LightDZ;
	if(lig<150)lig=150;
	if(lig>250)lig=250;
	return lig;
};
__forceinline int GetLighting3DV(int dx,int dy,int dz){
	int lig=(dx*LightDX+dy*LightDY+dz*LightDZ)/sqrt(dx*dx+dy*dy+dz*dz);
	if(lig<120)lig=120;
	if(lig>250)lig=250;
	return lig;

}
//---lighting hash----
#include <kHash.hpp>
class LHashItem{
public:
	int Vertex;
	short H;
	byte Light;
	unsigned int	hash	() const{
		return Vertex;
	}
	bool			equal	( const LHashItem& el ){
		return Vertex==el.Vertex;
	}
	void			copy	( const LHashItem& el ){
		*this=el;
	}
};
Hash<LHashItem> LightHash;

void ResetLightHash(){
	LightHash.reset();
}
void DelLightHashItem(int V){
	LHashItem L;
	L.Vertex=V;
	int idx=LightHash.find(L);
	if(idx!=NO_ELEMENT){
		LHashItem* L0=&LightHash.elem(idx);
		L0->H=-10000;
	}
}
void SaveLightHash(ResFile F){
	int N=0;
	for(int i=0;i<LightHash.numElem();i++){
		LHashItem* L=&LightHash.elem(i);
		if(L->H>-10000)N++;
	}
	i='SAHL';
	RBlockWrite(F,&i,4);
	i=N*7+4+4;
	RBlockWrite(F,&i,4);
	RBlockWrite(F,&N,4);
	for(int i=0;i<LightHash.numElem();i++){
		LHashItem* L=&LightHash.elem(i);
		if(L->H>-10000){
			RBlockWrite(F,&L->Vertex,7);
		}
	}    
}
void LoadLightHash(ResFile F){
    LightHash.reset();
	int N;
	RBlockRead(F,&N,4);
	for(int i=0;i<N;i++){
		LHashItem L;
		RBlockRead(F,&L.Vertex,7);
        LightHash.add(L);
	}    
}
void AddToLightHash(int Vertex,byte Light){
	LHashItem L;
	L.H=THMap[Vertex];
	L.Vertex=Vertex;
	L.Light=Light;
	int idx=LightHash.find(L);
	if(idx!=NO_ELEMENT){
		LHashItem* lpL=&LightHash.elem(idx);
		lpL->Light=Light;
		lpL->H=L.H;
	}else{
		LightHash.add(L);
	}
}
bool GetHashLight(int Vertex,int& Lit){
	return false;
	LHashItem L;
	L.Vertex=Vertex;
	int idx=LightHash.find(L);
	if(idx!=NO_ELEMENT){
		LHashItem* lpL=&LightHash.elem(idx);
		if(lpL->H==THMap[Vertex]){
			Lit=lpL->Light;
			return true;
		}else return false;
	}else return false;
}
_inline int _GetHi(int i){
	if(i<0||i>MaxTH*(MaxTH+1))return 0;
	return THMap[i];
};
int GetLighting3D(int i){	
	//return GetPointLighting(i);
	int h1,h2,h3,h4,h5,h6;
	int H0=_GetHi(i);
    int vp=i%VertInLine;
	if(!(vp&1)){
		h1=_GetHi(i+VertInLine);
		h2=_GetHi(i+VertInLine+1);
		h3=_GetHi(i+1);
		h4=_GetHi(i-VertInLine);
		h5=_GetHi(i-1);
		h6=_GetHi(i+VertInLine-1);
	}else{
		h1=_GetHi(i+VertInLine);
		h2=_GetHi(i+1);
		h3=_GetHi(i-VertInLine+1);
		h4=_GetHi(i-VertInLine);
		h5=_GetHi(i-VertInLine-1);
		h6=_GetHi(i-1);
	};
	int dy=h4-h1;
	int dx=(h2-h5+h3-h6)>>1;
	int dz=TriUnit+TriUnit+TriUnit+TriUnit;
	int lig;
	lig=GetLighting3DV(dx,dy,dz);
	if(LightMap){
		int L0;
		if(i>=VertInLine*2&&i<MaxPointIndex-4)L0=LightMap[i-VertInLine];
		else L0=0;
		if(L0<lig)lig=L0-((255-lig)/5);
	};
	/*
	if(H0<32){
		if(H0<0)H0=0;
		lig=(lig*H0+TL0*(32-H0))>>5;
	}
	*/
	if(TL0==-1)TL0=GetLighting3D0();

	//int GetVertexDarkness(int V);
	//int DL=GetVertexDarkness(i)>>1;
	lig=(lig<<7)/TL0;
	//lig-=DL;
	if(lig<0)lig=0;
	if(lig>255)lig=255;
	return lig;
};
#define MSXH (240<<ADDSH)
#define lim(x) (x<=255?x>=0?x:0:255)
int GetLighting3DInPoint(int x,int y){
	if(x<0)x=0;
	if(y<32)y=32;
	if(x>MSXH*32)x=MSXH*32;
	if(y>MSXH*32)y=MSXH*32;
	int nx=x>>5;
	int DD=(randoma[(x+y*177)&8191]&31)-15;
	if(nx&1){
		int dd=(x&31);
		int dy=dd>>1;
		int oy=15-dy;
		int y1=(y+oy)>>5;
		int dy1=(y+oy)&31;
		if(dy1>32-dd){
			int Vert2=nx+y1*VertInLine+1;
			int Vert3=Vert2+VertInLine;
			int Vert1=Vert3-1;
			int H1=GetLighting3D(Vert1);
			int H2=GetLighting3D(Vert2);
			int H3=GetLighting3D(Vert3);
			int x0=(nx<<5);
			int y0=(y1<<5)+16;
			int HH=H1+(((x-x0)*(((H2+H3)>>1)-H1))>>5)+(((y-y0)*(H3-H2))>>5);
			//if(HH<0)HH=0;
			//assert(HH>=0);
			return lim(HH+DD);
		}else{
			int Vert2=nx+y1*VertInLine;
			int Vert3=Vert2+VertInLine;
			int Vert1=Vert2+1;
			int H1=GetLighting3D(Vert1);
			int H2=GetLighting3D(Vert2);
			int H3=GetLighting3D(Vert3);
			int x0=(nx<<5)+32;
			int y0=(y1<<5);
			int HH=H1-(((x-x0)*(((H2+H3)>>1)-H1))>>5)+(((y-y0)*(H3-H2))>>5);
			//if(HH<0)HH=0;
			//assert(HH>=0);
			return lim(HH+DD);
		};
	}else{
		int dd=(x&31);
		int dy=dd>>1;
		int oy=15-dy;
		int y1=(y+dy)>>5;
		int dy1=(y+dy)&31;
		if(dy1<dd){
			int Vert1=nx+y1*VertInLine;
			int Vert2=Vert1+1;
			int Vert3=Vert2+VertInLine;
			int H1=GetLighting3D(Vert1);
			int H2=GetLighting3D(Vert2);
			int H3=GetLighting3D(Vert3);
			int x0=(nx<<5);
			int y0=(y1<<5);
			int HH=H1+(((x-x0)*(((H2+H3)>>1)-H1))>>5)+(((y-y0)*(H3-H2))>>5);
			//if(HH<0)HH=0;
			return lim(HH+DD);
		}else{
			int Vert2=nx+y1*VertInLine;
			int Vert3=Vert2+VertInLine;
			int Vert1=Vert3+1;
			int H1=GetLighting3D(Vert1);
			int H2=GetLighting3D(Vert2);
			int H3=GetLighting3D(Vert3);
			int x0=(nx<<5)+32;
			int y0=(y1<<5)+16;
			int HH=H1-(((x-x0)*(((H2+H3)>>1)-H1))>>5)+(((y-y0)*(H3-H2))>>5);
			//if(HH<0)HH=0;
			return lim(HH+DD);
		};
	};
	
};
extern short randoma[8192];
int LSC(int L,int Min,int Max){
	L=(L<<7)/TL0;
	int LL;
	if(L<128){
		LL=128-(128-L)*(128-Min)/128;
	}else{
		LL=128+(L-128)*(Max-128)/128;
	};
	if(LL<0)LL=0;
	if(LL>255)LL=255;
	return LL;
};
DWORD GetDW_Light(int idx){	
	DWORD L=GetLighting3D(idx);
	DWORD alp=255;//127+(randoma[(idx*7)&8191]%64);
	//return (alp<<24)+LSC(L,0,180)+(LSC(L,0,200)<<8)+(LSC(L,-40,255)<<16);
	DWORD L0=(alp<<24)+(L<<16)+(L<<8)+L;
	if(L<128){
		int W=(128-L)*4;
		if(W>255)W=255;
        return MixDWORD(EngSettings.ShadowsColor,0xFF808080,W,255-W);
	}else{
		int W=(L-128)*4;
		if(W>255)W=255;
		return MixDWORD(EngSettings.SunColor,0xFF808080,W,255-W);      
	}
};
DWORD GETL(DWORD L){
	DWORD alp=255;//127+(randoma[(idx*7)&8191]%64);
	return (alp<<24)+LSC(L,0,180)+(LSC(L,0,200)<<8)+(LSC(L,-40,255)<<16);
}
void GetDW_LMin(DWORD& V,int idx){
	DWORD alp=0;//(randoma[(idx*13)&8191]%64);
	V&=0xFFFFFF;
	V|=(alp<<24);//+0xFFFFFF;
};
int min3(int v1,int v2,int v3){
	return v1<v2?(v3<v1?v3:v1):(v3<v2?v3:v2);
};
int min2(int v1,int v2){
	return v1<v2?v1:v2;
};
void swap2(int&v1,int&v2){
	v1=v1+v2;
	v2=v1-v2;
	v1=v1-v2;
};
void Sort3(int&v1,int&v2,int&v3){
	int v[3];
	v[0]=-v1;
	v[1]=-v2;
	v[2]=-v3;
	bool c;
	do{
		c=0;
		if(v[0]>v[1]){
			swap2(v[0],v[1]);
			c=1;
		};
		if(v[1]>v[2]){
			swap2(v[1],v[2]);
			c=1;
		};
	}while(c);
	v1=-v[0];
	v2=-v[1];
	v3=-v[2];
};
#define XCHG(a,b) a=a+b;b=a-b;a=a-b;
void Sort3L(int&v1,int&v2,int&v3){
	Sort3(v1,v2,v3);
	return;
	int v[3];
	v[0]=v1;
	v[1]=v2;
	v[2]=v3;
	bool c;
	do{
		c=0;
		if(v[0]>v[1]){
			swap2(v[0],v[1]);
			c=1;
		};
		if(v[1]>v[2]){
			swap2(v[1],v[2]);
			c=1;
		};
	}while(c);
	v1=v[0];
	v2=v[1];
	v3=v[2];
};
void Sort3R(int&v1,int&v2,int&v3){
	Sort3(v1,v2,v3);
	return;
	//return;
	int v[3];
	v[0]=v1;
	v[1]=v2;
	v[2]=v3;
	bool c;
	do{
		c=0;
		if(v[0]>v[1]){
			swap2(v[0],v[1]);
			c=1;
		};
		if(v[1]>v[2]){
			swap2(v[1],v[2]);
			c=1;
		};
	}while(c);
	v1=v[0];
	v2=v[1];
	v3=v[2];
};
#define TEXSIZE 512
#define TRISCALE 512
#define TEXNX 8
#define TEXNY 8

float GetBaseU(byte Tex){
	return float(Tex&(TEXNX-1))/TEXNX+1.0/TRISCALE;
};
float GetBaseV(byte Tex){
	return float(Tex/TEXNY)/TEXNY+1.0/TRISCALE;
};
int LastVUpdate=0;
void ReportVUpdate(){
	if(LastVUpdate==0)LastVUpdate=GetTickCount();
};
void MakeAllDirtyGBUF();
void ProcessLightMap(){
    return;
	if(LastVUpdate){
		if(GetTickCount()-LastVUpdate>3000){
			CreateLightMap();
			LastVUpdate=0;
			MakeAllDirtyGBUF();
		};
	};
};
extern byte* FactureMap;
extern byte* FactureWeight;
extern byte* FactureShift;
void GetVertexNormal(int Vertex,float& x,float& y,float& z){
	int vx=Vertex%VertInLine;
	int vy=Vertex/VertInLine;
	int tx=vx<<5;
	int ty=vy<<5;
	int HLU,HLD,HRU,HRD,HU,HD;
	int H=THMap[Vertex];
	if(vx&1){
		ty-=16;
		if(vy>0){
			HU=THMap[Vertex-VertInLine];
			HLU=THMap[Vertex-VertInLine-1];
			HRU=THMap[Vertex-VertInLine+1];
		}else{
			HU=H;
			HLU=H;
			HRU=H;
		};
		HLD=THMap[Vertex-1];
		HRD=THMap[Vertex+1];
		HD=THMap[Vertex+VertInLine];
	}else{
		if(vx>0){
			HLU=THMap[Vertex-1];
			HLD=THMap[Vertex+VertInLine-1];
		}else{
			HLU=H;
			HLD=H;
		};
		if(vy>0){
			HU=THMap[Vertex-VertInLine];
		}else HU=H;
		HRU=THMap[Vertex+1];
		HRD=THMap[Vertex+VertInLine+1];
		HD=THMap[Vertex+VertInLine];
	};
	x=(HLU+HLD-HRU-HRD)/2;
	y=(HD-HU);
	z=64;
	int n=sqrt(x*x+y*y+z*z);
	x/=n;
	y/=n;
	z/=n;
};
void SetDefaultFactureInVertex(int Vertex,int TexID){
	if(TexID==2)FactureMap[Vertex]=14;
	else FactureMap[Vertex]=0;
	FactureWeight[Vertex]=255;
	FactureShift[Vertex]=0;
};


int GetMostTexture(int V);
int GetMostTextureEx(int V,int tex);
extern bool Mode3D;
int HScale=256;
#define THMAP(x) (Mode3D?((THMap[x]*HScale)>>8):0)
#define THMAP2(x) ((Mode3D?THMap[x]:0)-(GETYSHIFT(x)<<1))
void SetNewFactureInRound(int x,int y,int r,int dr,int Facture,int Weight,bool UseCentralTexture){
	bool CheckIfNewTerrain(void);
	extern UniversalMap g_UniversalMap;
	if(CheckIfNewTerrain() == true)
	{
		if(GetKeyState(VK_LCONTROL) & 0x8000);//g_UniversalMap.CreateHollow(x, y, r, dr);
		else g_UniversalMap.SetFactureInRadius(x, y, r, Facture);
		return;
	}
	if(!dr)dr=1;
	int VX=x>>5;
	int VY=y>>5;
	int DR=3;
	int VX0=VX-DR;
	int VX1=VX+DR;
	int VY0=VY-DR;
	int VY1=VY+DR;
	if(VX0<0)VX0=0;
	if(VY0<0)VY0=0;
	if(VX1>=VertInLine)VX1=VertInLine-1;
	if(VY1>=MaxTH)VX1=MaxTH-1;
	int CTexID=-1;
	int NR=10000;
	for(int iy=VY0;iy<=VY1;iy++){
		for(int ix=VX0;ix<=VX1;ix++){
			int ofs=ix+iy*VertInLine;
			int xp=ix<<5;
			int yp=iy<<5;
			if(ix&1)yp-=16;
			int R=Norma(xp-x,yp-y);
			if(R<NR){
				NR=R;
				CTexID=GetMostTexture(ofs);
			};
		};
	};
	DR=(r+dr+64)>>5;
	VX0=VX-DR;
	VX1=VX+DR;
	VY0=VY-DR;
	VY1=VY+DR;
	if(VX0<0)VX0=0;
	if(VY0<0)VY0=0;
	if(VX1>=VertInLine)VX1=VertInLine-1;
	if(VY1>=MaxTH)VX1=MaxTH-1;

	int MinDirtyX=1000000;
	int MinDirtyY=1000000;
	int MaxDirtyX=0;
	int MaxDirtyY=0;

	for(iy=VY0;iy<=VY1;iy++){
		for(int ix=VX0;ix<=VX1;ix++){
			int ofs=ix+iy*VertInLine;
			if(GetMostTextureEx(ofs,CTexID)==CTexID||!UseCentralTexture){
				int xp=ix<<5;
				int yp=iy<<5;
				if(ix&1)yp-=16;
				int R=sqrt((xp-x)*(xp-x)+(yp-y)*(yp-y));
				int W;
				if(R<r)W=256;
				else if(R<r+dr)W=256*(r+dr-R)/dr;
				else W=0;
				int FW=GetFactureWeightFactor(xp,yp,Facture);
				W=FW*W/255;
				int WW=Weight>0?W*255/Weight:255;
				if((WW*2>FactureWeight[ofs]&&Facture!=FactureMap[ofs])
					||(WW>FactureWeight[ofs]&&Facture==FactureMap[ofs])){
					FactureWeight[ofs]=Weight*W/256;
					FactureMap[ofs]=Facture;
					int yr=(yp>>1)-THMAP(ofs);
					if(xp<MinDirtyX)MinDirtyX=xp;
					if(xp>MaxDirtyX)MaxDirtyX=xp;
					if(yr<MinDirtyY)MinDirtyY=yr;
					if(yr>MaxDirtyY)MaxDirtyY=yr;
					bool CheckFactureWeightInPoint(int V);
					CheckFactureWeightInPoint(ofs);
				};
			};
		};
	};
	if(MinDirtyX<MaxDirtyX)MakeDirtyGBUF((MinDirtyX>>(10-Shifter))-4,(MinDirtyY>>((9-Shifter)))-4,((MaxDirtyX-MinDirtyX)>>5)+8,((MaxDirtyY-MinDirtyY)>>4)+8);
};
int GetFactureWeightByIdx(int Vertex){
	if(Vertex>VertInLine){
		if(THMap[Vertex]-THMap[Vertex-VertInLine]<16){
			return FactureWeight[Vertex];//255;
		}else return 0;
	}else return 0;
};
int GetFactureIdx(int Vertex,int& dx,int& dy){
	int tex=TexMap[Vertex];
	dx=0;
	dy=0;
	int fid=FactureMap[Vertex];
	//byte use=F3D.GTex[fid].Usage;
	byte use=GetFactureUsage(fid);
	if(use==2){//edges
		int f=fid;
		float nx,ny,nz;
		GetVertexNormal(Vertex,nx,ny,nz);
		float anx=fabs(nx);
		ny=fabs(ny);
		if(ny>anx*2)return f+256;
		if(anx>ny*2)return f+512;
		if(nx<0)return f+768;
		else return f+1024;
	}else
	if(use==1){//vertical
		int f=fid;
		float nx,ny,nz;
		GetVertexNormal(Vertex,nx,ny,nz);
		float anx=fabs(nx);
		ny=fabs(ny);
		if(ny>anx*2)return f+256;
		if(anx>ny*2)return f+512;
		if(nx<0)return f+768;
		else return f+1024;
	}else{//planar
		int vx=Vertex%VertInLine;
		int vy=Vertex/VertInLine;
		int uu=(vx+vy)/12;
		int uv=(vx-vy)/12;
		//float nx,ny,nz;
		//GetVertexNormal(Vertex,nx,ny,nz);
		//int adopt=int(1.95/nz)-1;
		//if(adopt>3)adopt=3;
		//return fid+((randoma[(uu+uv*17)&8191]&63)+adopt*64)*256;
		return fid+((randoma[(uu+uv*17)&8191]&63))*256;
	};
};
int max3(int v1,int v2,int v3){
	return v1>v2?(v3>v1?v3:v1):(v3>v2?v3:v2);
};
int maxD3(int H1,int H2,int H3){
	return max3(abs(H1-H2),abs(H1-H3),abs(H2-H3));
};
float sq_norma(float x,float y);
void GetFactureUVW(int Vertex,int TIdx,float& u,float& v,int& MaxW,DWORD& Diffuse){
	int Idx=TIdx&255;
	int opt=TIdx>>8;
	//int Usage=F3D.GTex[Idx].Usage;
	int Usage=GetFactureUsage(Idx);

	int vx=Vertex%VertInLine;
	int vy=Vertex/VertInLine;

	int tx=(vx<<5)+(Usage==0?GETXSHIFT(Vertex):0);
	int ty=(vy<<5)-((vx&1)<<4)+(Usage==0?GETYSHIFT(Vertex):0);

	if(Usage==0){
		int adop=(opt>>6)+1;
		u=float(tx+(opt&7)*32)/256.0;
		v=float(ty+((opt/8)&7)*32)/512.0*adop;
		MaxW=255;
		if(!Idx)MaxW=0xA0;
		Diffuse=Vertex;
		return;
	}
	
	int HLU,HLD,HRU,HRD,HU,HD;
	int H=THMap[Vertex];
	if(vx&1){
		ty-=16;
		if(vy>0){
			HU=THMap[Vertex-VertInLine];
			HLU=THMap[Vertex-VertInLine-1];
			HRU=THMap[Vertex-VertInLine+1];
		}else{
			HU=H;
			HLU=H;
			HRU=H;
		};
		HLD=THMap[Vertex-1];
		HRD=THMap[Vertex+1];
		HD=THMap[Vertex+VertInLine];
	}else{
		if(vx>0){
			HLU=THMap[Vertex-1];
			HLD=THMap[Vertex+VertInLine-1];
		}else{
			HLU=H;
			HLD=H;
		};
		if(vy>0){
			HU=THMap[Vertex-VertInLine];
		}else HU=H;
		HRU=THMap[Vertex+1];
		HRD=THMap[Vertex+VertInLine+1];
		HD=THMap[Vertex+VertInLine];
	};
	int ry=(ty>>1)-H;
	if(Usage==0){
		int adop=(opt>>6)+1;
		u=float(tx+(opt&7)*32)/256.0;
		v=float(ty+((opt/8)&7)*32)/512.0*adop;
		MaxW=255;
		if(!Idx)MaxW=0xA0;
	}else if(Usage==1){		
		v=-float(H)/180.0;//+HU+HD+HLU+HLD+HRD+HRU)/180.0/7.0;
		MaxW=180*min(min3(maxD3(HLU,H,HLD),maxD3(HLU,HU,H),maxD3(HU,H,HRU)),min3(maxD3(HRU,HRD,H),maxD3(HRD,HD,H),maxD3(HD,H,HLD)));
		if(opt==1){
			u=float(tx)/256.0;
		}else
		if(opt==2){
			u=float(ty)/256.0;
		}else
		if(opt==3)u=float(tx+ty)/256/1.4142;
		else u=float(tx-ty)/256/1.4142;
		u*=1.5;
		v*=1.5;
		if(MaxW>255)MaxW=255;
	}else if(Usage==2){
		int x=vx<<5;
		int y=(vy<<5)-((vx&1)<<4);        
		/*float DHX=float(HRU+HRD-HLU-HLD)/128.0f;
		float DHY=float(HD-HU+HLD+HRD-HLU-HRU)/256.0f;
		float n=sq_norma(DHX,DHY);*/
		Vector3D V=GetSurfaceN(x,y);
		float n=(1-V.z)*2;
		if(n>1)n=1;	
		v=n;
		MaxW=180*min(min3(maxD3(HLU,H,HLD),maxD3(HLU,HU,H),maxD3(HU,H,HRU)),min3(maxD3(HRU,HRD,H),maxD3(HRD,HD,H),maxD3(HD,H,HLD)));
		if(opt==1){
			u=float(tx)/256.0;
		}else
			if(opt==2){
				u=float(ty)/256.0;
			}else
				if(opt==3)u=float(tx+ty)/256/1.4142;
				else u=float(tx-ty)/256/1.4142;
				u*=1.5;
				v*=1.5;
				if(MaxW>255)MaxW=255;
	}
	float du,dv,su,sv;
	GetUVInfo(Idx,du,dv,su,sv);
	u=(u+du)*su;
	v=(v+dv)*sv;	
	Diffuse=Vertex;
}
DWORD GetBumpDiffuseForVertex(int Vertex,float Contrast,float Bright){
	int vx=Vertex%VertInLine;
	int vy=Vertex/VertInLine;
	int x=vx<<5;
	int y=(vy<<5)-((vx&1)<<4);        
	Vector3D V=GetSurfaceN(x,y);	
	ILight* iLight = GetGameLight();
	Vector3D ldir = iLight->GetDir();
	ldir.reverse();		
	float dotp=ldir.dot(V);
	if(dotp<0)ldir.zero();
	Vector3D vn=V;	
	float pr1=dotp<0?Contrast:Contrast*(1-2*dotp);
	if(pr1<0)pr1=0;
	vn*=pr1;
	ldir;
	ldir+=vn;
	float n=ldir.norm();
	if(n>Bright){
		ldir*=Bright;
		ldir/=n;
	}
	return VectorToColor(ToTangentSpace(ldir,V,Vector3D(1,0,0)),0);
}
extern word TexFlags[256];
int GetVValue(int Vertex,int RSet);
bool PLAINMODE=0;
extern byte TexCrossing[4096];
extern byte* TexMapEx;
void NormalizeVertexTexR(VertexTnL2& V1,VertexTnL2& V2,VertexTnL2& V3,int GTex,int CTex,bool PLAIN=0,int VR=0){
	/*
	int idx=GetVValue(VR,111)&3;
	bool C1=(V1.diffuse>>24)>16;
	bool C2=(V2.diffuse>>24)>16;
	bool C3=(V3.diffuse>>24)>16;
	byte CC=int(C1)+(int (C2)<<1)+(int (C3)<<2);
	int x1,y1,x2,y2,x3,y3;
	switch(CC){
	case 0:
		x1=0;y1=0;
		x2=0;y2=0;
		x3=0;y3=0;
		break;
	case 1:
		x1=32;y1=32;
		x2=63;y2=16;
		x3=63;y3=48;
		break;
	case 2:
		x1=0;y1=48;
		x2=32;y2=32;
		x3=32;y3=62;
		break;
	case 3:
		x1=0;y1=208-128;
		x2=32;y2=192-128;
		x3=32;y3=224-128;
		break;
	case 4:
		x1=0;y1=16;
		x2=32;y2=0;
		x3=32;y3=32;
		break;
	case 5:
		x1=0;y1=240-128;
		x2=32;y2=224-128;
		x3=32;y3=254-128;
		break;
	case 6:
		x1=32;y1=224-128;
		x2=63;y2=208-128;
		x3=63;y3=240-128;
		break;
	case 7:
		x1=0;y1=196-128;
		x2=0;y2=196-128;
		x3=0;y3=196-128;
		break;
	};

	short G1=(TexFlags[GTex&255]>>8)&7;
	short G2=(TexFlags[CTex&255]>>8)&7;
	
	//if(abs(G2-G1)<3||PLAIN||PLAINMODE){
	if(PLAINMODE){
		//assert(C1==C2&&C2==C3);
		x1=0;y1=196-128;
		x2=0;y2=196-128;
		x3=0;y3=196-128;

		y1+=128;
		y2+=128;
		y3+=128;

	}else{
	
		//x1=64-x1;
		//x2=64-x2;
		//x3=64-x3;
		//y1=64-y1;
		//y2=64-y2;
		//y3=64-y3;
		//V1.diffuse|=0xFF000000;
		//V2.diffuse|=0xFF000000;
		//V3.diffuse|=0xFF000000;
	};
	V1.u2=float(x1+idx*64)/256.0;
	V1.v2=float(y1)/256.0;
	V2.u2=float(x2+idx*64)/256.0;
	V2.v2=float(y2)/256.0;
	V3.u2=float(x3+idx*64)/256.0;
	V3.v2=float(y3)/256.0;
	*/
	int T1=TexMap[VR];
	int T2=TexMapEx[VR];
	int crs=TexCrossing[T1+T2*64];
	bool C1=(V1.diffuse>>24)>200;
	bool C2=(V2.diffuse>>24)>200;
	bool C3=(V3.diffuse>>24)>200;
	if(C1&&C2&&C3&&!PLAINMODE){
		V1.u2=0;
		V1.v2=0;
		V2.u2=0;
		V2.v2=0;
		V3.u2=0;
		V3.v2=0;
	}else{
		/*
		V1.u2=V1.x/256.0;
		V1.v2=V1.y/256.0;
		V2.u2=V2.x/256.0;
		V2.v2=V2.y/256.0;
		V3.u2=V3.x/256.0;
		V3.v2=V3.y/256.0;
		*/
		float x0=float(64+(128*(crs&1))-32+(rand()&63))/256.0;
		float y0=float(64+(128*(crs>>1))-32+(rand()&63))/256.0;
		V1.u2=x0;
		V1.v2=y0;
		V2.u2=x0+((V2.x-V1.x)/256.0);
		V2.v2=y0+((V2.y-V1.y)/256.0);
		V3.u2=x0+((V3.x-V1.x)/256.0);
		V3.v2=y0+((V3.y-V1.y)/256.0);
	};
};
void NormalizeVertexTexL(VertexTnL2& V1,VertexTnL2& V2,VertexTnL2& V3,int GTex,int CTex,bool PLAIN=0,int VR=0){
	/*
	int idx=GetVValue(VR,113)&3;;
	bool C1=(V1.diffuse>>24)>16;
	bool C2=(V2.diffuse>>24)>16;
	bool C3=(V3.diffuse>>24)>16;
	byte CC=int(C1)+(int (C2)<<1)+(int (C3)<<2);
	int x1,y1,x2,y2,x3,y3;
	switch(CC){
	case 0:
		x1=0;y1=0;
		x2=0;y2=0;
		x3=0;y3=0;
		break;
	case 1:
		x1=32;y1=32;
		x2=63;y2=48;
		x3=32;y3=64;
		break;
	case 2:
		x1=0;y1=16;
		x2=32;y2=32;
		x3=0;y3=48;
		break;
	case 3:
		x1=32;y1=192-128;
		x2=63;y2=208-128;
		x3=32;y3=224-128;
		break;
	case 4:
		x1=32;y1=0;
		x2=63;y2=16;
		x3=32;y3=32;
		break;
	case 5:
		x1=0;y1=208-128;
		x2=32;y2=224-128;
		x3=0;y3=240-128;
		break;
	case 6:
		x1=32;y1=224-128;
		x2=63;y2=240-128;
		x3=32;y3=254-128;
		break;
	case 7:
		x1=0;y1=196-128;
		x2=0;y2=196-128;
		x3=0;y3=196-128;
		break;
	};

	short G1=(TexFlags[GTex&255]>>8)&7;
	short G2=(TexFlags[CTex&255]>>8)&7;
	
	//if(abs(G2-G1)<3||PLAIN||PLAINMODE){
	if(PLAINMODE){
		//assert(C1==C2&&C2==C3);
		x1=0;y1=196-128;
		x2=0;y2=196-128;
		x3=0;y3=196-128;

		y1+=128;
		y2+=128;
		y3+=128;

	}else{
	
		//x1=64-x1;
		//x2=64-x2;
		//x3=64-x3;
		//y1=64-y1;
		//y2=64-y2;
		//y3=64-y3;
		//V1.diffuse|=0xFF000000;
		//V2.diffuse|=0xFF000000;
		//V3.diffuse|=0xFF000000;
	};
	V1.u2=float(x1+idx*64)/256.0;
	V1.v2=float(y1)/256.0;
	V2.u2=float(x2+idx*64)/256.0;
	V2.v2=float(y2)/256.0;
	V3.u2=float(x3+idx*64)/256.0;
	V3.v2=float(y3)/256.0;
	*/
	bool C1=(V1.diffuse>>24)>200;
	bool C2=(V2.diffuse>>24)>200;
	bool C3=(V3.diffuse>>24)>200;
	int T1=TexMap[VR];
	int T2=TexMapEx[VR];
	int crs=TexCrossing[T1+T2*64];
	if(C1&&C2&&C3&&!PLAINMODE){
		V1.u2=0;
		V1.v2=0;
		V2.u2=0;
		V2.v2=0;
		V3.u2=0;
		V3.v2=0;
	}else{
		/*
		V1.u2=V1.x/256.0;
		V1.v2=V1.y/256.0;
		V2.u2=V2.x/256.0;
		V2.v2=V2.y/256.0;
		V3.u2=V3.x/256.0;
		V3.v2=V3.y/256.0;
		*/
		float x0=float(64+(128*(crs&1))-32+(rand()&63))/256.0;
		float y0=float(64+(128*(crs>>1))-32+(rand()&63))/256.0;
		V1.u2=x0;
		V1.v2=y0;
		V2.u2=x0+((V2.x-V1.x)/256.0);
		V2.v2=y0+((V2.y-V1.y)/256.0);
		V3.u2=x0+((V3.x-V1.x)/256.0);
		V3.v2=y0+((V3.y-V1.y)/256.0);
	};
};

int GetVValue(int Vertex,int RSet){
	return randoma[(Vertex*RSet)&8191];
};
float GetRHW(int V){
	int Tex=TexMap[V];
	//if(TexFlags[Tex]&8)return 1.0;
	//if(!Tex)return 0.1;

	return 1.0;//0.4+float(GetVValue(V,11)%60)/100.0;
	//return 1.0;
	int T=TexMap[V];
	if(T)return 0.1+float(GetVValue(V,11)%90)/100.0;
	else return 1.0;
};
bool CheckTexForStones(int Tex){
	if(Tex!=10)return 0;
	return 1;
};
bool CheckVertexForStone(int V){
	float x,y,z;
	GetVertexNormal(V,x,y,z);
	if(fabs(x)>fabs(y))return false;

	int vx=V%VertInLine;
	int vy=V/VertInLine;
	if(vx>2&&vy>2){
		int TU,TL,TR,TLD,TRD;
		int T=TexMap[V];
		if(vx&1){
			TU=TexMap[V-VertInLine];
			TL=TexMap[V-VertInLine-1];
			TR=TexMap[V-VertInLine+1];
			TLD=TexMap[V-1];
			TRD=TexMap[V+1];
		}else{
			TU=TexMap[V-VertInLine];
			TL=TexMap[V-1];
			TR=TexMap[V+1];
			TLD=TexMap[V+VertInLine-1];
			TRD=TexMap[V+VertInLine+1];
		};
		if(CheckTexForStones(T)&&!CheckTexForStones(TU))return true;
		//if(CheckTexForStones(T)&&CheckTexForStones(TLD)&&CheckTexForStones(TRD)&&
		// !(CheckTexForStones(TU)||CheckTexForStones(TL)||CheckTexForStones(TR)))return true;
	};
	return false;
};
void DrawRoads(int sx,int sy,int mx,int my,int lx,int ly,int scale);
#define VVV 28
int GetBumpDiffuse(int v,int Transp){
	if(GetKeyState(VK_CONTROL)&0x8000)return 0x8080C0;
	float x,y,z;
	GetVertexNormal(v,x,y,z);
	float nn=sqrt(x*x+y*y);
	x/=nn;
	y/=nn;
	int nx;
	float nT=y*1.5-x*2.0;
	if(nT<0){
		nx=(y*2.0+x*1.5)*40.0;
	}else{
		nx=(y*2.0+x*1.5)*40.0/(1+nT*3);
	};
	int ny=20;
	return 0x800000C0+((128+(nx*Transp/256))<<16)+((128+(ny*Transp/256))<<8);
};
#define MAXFVERT 65500
VertexTnL* GVG=NULL;
static VertexTnL* GARRAY[256];
static int NInGarr[256];
static int MaxInGarr[256];
void DrawGSprites(int Target,int x,int y,int gx,int gy,int lx,int ly,int scale);

byte* TexMapEx=NULL;
byte* WTexMapEx=NULL;
void ClearExMaps(){
	if(TexMapEx){
		memset(TexMapEx,0,MaxPointIndex);
		memset(WTexMapEx,0,MaxPointIndex);
	};
};
void FreeExMaps(){
	if(TexMapEx)free(TexMapEx);
	if(WTexMapEx)free(WTexMapEx);
	TexMapEx=NULL;
	WTexMapEx=NULL;
};
void SetupExMaps(){
	FreeExMaps();
	TexMapEx=(byte*)malloc(MaxPointIndex);
	WTexMapEx=(byte*)malloc(MaxPointIndex);
	ClearExMaps();
};
void MarkPointToDraw(int i);
int Norm(int dx,int dy);
#define WNORM(x) if(x<32)x=0;if(x>192)x=255;
void PerformTexSmoothingInPoint(int vx,int vy,int x0,int y0,int r){
	if(vx>0&&vx<VertInLine-1&&vy>0&&vy<MaxTH-1){
		int xx=vx<<5;
		int yy=vy<<5;
		if(vx&1)yy-=16;
		if(Norm(xx-x0,yy-y0)>r)return;
		int V0;
		int V[7];
		V0=vx+vy*VertInLine;
		V[0]=V0;
		V[1]=V0-VertInLine;
		V[4]=V0+VertInLine;
		if(vx&1){
			V[2]=V0-VertInLine+1;
			V[6]=V0-VertInLine-1;
			V[3]=V0+1;
			V[5]=V0-1;
		}else{
			V[2]=V0+1;
			V[6]=V0-1;
			V[3]=V0+VertInLine+1;
			V[5]=V0+VertInLine-1;
		};
		int NTEX=0;
		int TEXS[20];
		int TWS[20];
		int NTWS[20];
		for(int i=0;i<7;i++){
			byte stage=0;
			do{
				int T,W;
				if(stage){
					T=TexMapEx[V[i]];
					W=WTexMapEx[V[i]];
				}else{
					T=TexMap[V[i]];
					W=255-WTexMapEx[V[i]];
				};
				int WW=1;
				if(i==0){
					W<<=1;
					WW=2;
				};
				for(int j=0;j<NTEX;j++)if(TEXS[j]==T){
					NTWS[j]+=WW;
					TWS[j]+=W;
					break;
				};
				if(j==NTEX){
					NTWS[j]=WW;
					TWS[j]=W;
					TEXS[j]=T;
					NTEX++;
				};
				stage++;
			}while(stage<2);
		};
		bool ch=0;
		do{
			ch=0;
			for(int q=1;q<NTEX;q++){
				if(TWS[q-1]<TWS[q]){
					ch=1;
					swap(TWS[q-1],TWS[q]);
					swap(NTWS[q-1],NTWS[q]);
					swap(TEXS[q-1],TEXS[q]);
				};
			};
		}while(ch);
		if(NTEX>1){
			//if(rand()<16384){
				TWS[1]=TWS[1];
				TexMap[V0]=TEXS[0];
				TexMapEx[V0]=TEXS[1];
				int W0=TWS[0];
				int W1=TWS[1];
				int WW=255*W1/(W0+W1);
				int mrand();
				if(WW>100&&WW<145&&mrand()<16384){
					TexMap[V0]=TEXS[1];
					TexMapEx[V0]=TEXS[0];
					WTexMapEx[V0]=255-WW;
					MarkPointToDraw(V0);
				}else{
					WTexMapEx[V0]=WW;
					MarkPointToDraw(V0);
				};
			/*
			}else{
				TexMap[V0]=TEXS[1];
				TexMapEx[V0]=TEXS[0];
				int W0=TWS[0];
				int W1=TWS[1];
				WTexMapEx[V0]=255*W0/(W0+W1);
				WNORM(WTexMapEx[V0]);
				MarkPointToDraw(V0);
			};
			*/
		}else{
			TexMap[V0]=TEXS[0];
			WTexMapEx[V0]=0;
			TexMapEx[V0]=0;
		};
	};

};
void AssignExTex(int V,byte tex,byte W){
	TexMap[V]=tex;
	TexMapEx[V]=tex;
	WTexMapEx[V]=W;
};
void AssignExTex2(int V,byte tex1,byte tex2,int W){
	int W00=W;
	if(W>255)W=255;
	int W0=0;
	if(TexMap[V]==tex2){
		W0=255;
		if(TexMapEx[V]!=tex2)W0=255-WTexMapEx[V];
	}else{
		W0=0;
		if(TexMapEx[V]==tex2)W0=WTexMapEx[V];
	};
	if(W>W0){
		TexMap[V]=tex1;
		TexMapEx[V]=tex2;
		if(W>WTexMapEx[V]){
			WTexMapEx[V]=W;
			if(W>220){
				TexMap[V]=tex2;
				if(W00>255){
					W00=512-W00;
					if(W00<0)W00=0;
					WTexMapEx[V]=W00;
				};
			};
		};
	};

	/*
	TexMap[V]=tex1;
	TexMapEx[V]=tex2;
	if(W>WTexMapEx[V]){
		WTexMapEx[V]=W;
		if(W>240)TexMap[V]=tex2;
	};
	*/
};
int Lim(int x);
#define SCAL(x) x=Lim(x*6/5)
//x=Lim(int(x)*2)
//Lim(int(x)*4)
#define OTSTUP 0
void DrawTriStrip(int DevID,int x,int y,int mx,int Gy,int mLx,int GLy,int ScShift)
{
	VertexTnL2* VST=NULL;
	x-=32*OTSTUP;
	mx-=OTSTUP;
	mLx+=OTSTUP*2;
	int mx0=mx;
	int x0=x;
	static int BumpShader=IRS->GetShaderID("Facture4");
	static int LightShader=IRS->GetShaderID("Facture3");
	static int DotShader=IRS->GetShaderID("dot3");
	if(!STriang){
		STriang=new BaseMesh;
		STriang->create(MAXFVERT,MAXFVERT,vfTnL2);
		GroundMesh=new BaseMesh;
		GroundMesh->create(MAXFVERT,MAXFVERT,vfTnL);
		word* idx=GroundMesh->getIndices();
		VertexTnL2* VER=(VertexTnL2*)STriang->getVertexData();
		word* IDX=STriang->getIndices();
		for(int i=0;i<MAXFVERT;i++){
			VER[i].w=1;
			VER[i].z=0;
			IDX[i]=i;
			idx[i]=i;
		};
		STriang->setShader(IRS->GetShaderID("Surface"));
		STriang->setTexture(IRS->GetTextureID("Textures\\GroundTex.bmp"));
		STriang->setTexture(IRS->GetTextureID("BoundNew128.tga"),1);

		GroundMesh->setShader(IRS->GetShaderID("Facture4"));
		GroundMesh->setNInd(3);
		GroundMesh->setNPri(1);
		GroundMesh->setNVert(3);
		//if(!F3D.NGTex)F3D.Setup();
		if(!GetNFactures())LoadFactures();
		StoneMesh=new BaseMesh;
		StoneMesh->create(4,6,vfTnL2);
		StoneMesh->setTexture(IRS->GetTextureID("Textures\\GroundTex.bmp"));
		StoneMesh->setTexture(IRS->GetTextureID("kamni.tga"),1);
		StoneMesh->setShader(IRS->GetShaderID("Stones"));
		StoneMesh->setNInd(6);
		StoneMesh->setNPri(2);
		StoneMesh->setNVert(4);
		IDX=StoneMesh->getIndices();
		IDX[0]=0;
		IDX[1]=1;
		IDX[2]=2;
		IDX[3]=2;
		IDX[4]=1;
		IDX[5]=3;
		VST=(VertexTnL2*)StoneMesh->getVertexData();
		for(i=0;i<4;i++){
			VST[i].w=1;
			VST[i].z=0;
		};
	};
	if(!VST)VST=(VertexTnL2*)StoneMesh->getVertexData();
	VertexTnL2* GVER=(VertexTnL2*)STriang->getVertexData();
	VertexTnL2* VER=GVER;
	VertexTnL* LVG=(VertexTnL*)GroundMesh->getVertexData();
	int TIDX[MAXFVERT];
	int NGFVERT=0;
	if(!GVG)GVG=new VertexTnL[MAXFVERT];
	VertexTnL* VG=GVG;

	int TQuant=32>>ScShift;
	int HQuant=16>>ScShift;
	int SQuant=8>>ScShift;

	float fTQuant=float(TQuant)/TRISCALE;
	float fHQuant=float(HQuant)/TRISCALE;
	float fSQuant=float(SQuant)/TRISCALE;

	int ScLimit=Gy-(500>>ScShift);
	int Gy1=Gy+GLy+64;

	Rct vp;
	vp.x=x+32*OTSTUP;
	vp.y=y;
	vp.w=(mLx-OTSTUP*2)*TQuant;
	vp.h=GLy;
	IRS->SetViewPort(vp);
	
	int GNV=0;
	//Gy-=32;
	//Gy1+=32;
	for(int ix=0;ix<mLx;ix++){

		int NStones=0;
		int StoneY[32];
		int StoneX[32];
		int StoneTex[32];
		DWORD StoneColor[32];

		//we must determine first triangle to draw
		//scanning up
		int yp0=(Gy/TQuant)*2;
		if(yp0<0)yp0=0;
		int p0=mx+yp0*VertInLine;
		int Cgy;
		do{
			Cgy=(yp0*HQuant)-(THMAP(p0)>>ScShift);
			if(yp0&&Cgy>ScLimit){
				yp0--;
				p0-=VertInLine;
			}else break;
		}while(1);
		int YA;
		do{
			int VERTICES[128];
			int NVR=0;
			VertexTnL2* V0=VER;
			if(mx&1){
				NVR=0;
				int Y=yp0*HQuant;

				int S0=Y-SQuant;
				int S1=Y;
				int S2=Y+SQuant;
				int S3=Y+HQuant;

				int Y0=S0-(THMAP(p0)>>ScShift);
				int Y1=S1-(THMAP(p0+1)>>ScShift);
				int Y2=S2-(THMAP(p0+VertInLine)>>ScShift);
				int Y3=S3-(THMAP(p0+VertInLine+1)>>ScShift);

				YA=Y0;
				if(!((Y0<Gy&&Y1<Gy&&Y2<Gy&&Y3<Gy)||(Y0>Gy1&&Y1>Gy1&&Y2>Gy1&&Y3>Gy1))){
					int V0=p0;
					int V1=p0+1;
					int V2=p0+VertInLine;
					int V3=p0+VertInLine+1;

					DWORD L00=GetDW_Light(V0);
					DWORD L10=GetDW_Light(V1);
					DWORD L20=GetDW_Light(V2);
					DWORD L30=GetDW_Light(V3);

					int STAGE=0;
					do{
						byte T1,T2,T3,T4,W1,W2,W3,W4;
						STAGE++;
						if(STAGE==1){
							T1=TexMap[p0];
							T2=TexMap[p0+1];
							T3=TexMap[p0+VertInLine];
							T4=TexMap[p0+VertInLine+1];
							W1=WTexMapEx[p0];
							W2=WTexMapEx[p0+1];
							W3=WTexMapEx[p0+VertInLine];
							W4=WTexMapEx[p0+VertInLine+1];
							PLAINMODE=W1>80||W2>80||W3>80||W4>80;
							W1=0xFF;
							W2=0xFF;
							W3=0xFF;
							W4=0xFF;
						}else{
							T1=TexMapEx[p0];
							T2=TexMapEx[p0+1];
							T3=TexMapEx[p0+VertInLine];
							T4=TexMapEx[p0+VertInLine+1];
							W1=WTexMapEx[p0];
							W2=WTexMapEx[p0+1];
							W3=WTexMapEx[p0+VertInLine];
							W4=WTexMapEx[p0+VertInLine+1];
							SCAL(W1);
							SCAL(W2);
							SCAL(W3);
							SCAL(W4);
							/*
							W1=0xFF;
							W2=0xFF;
							W3=0xFF;
							W4=0xFF;
							*/
							/*
							W1=0;
							W2=0;
							W3=0;
							W4=0;
							*/
							
							
							PLAINMODE=1;
						};
						if(W1||W2||W3||W4){
							if(W1<1)W1=1;
							if(W2<1)W2=1;
							if(W3<1)W3=1;
							if(W4<1)W4=1;
							DWORD L0,L1,L2,L3;
							if(STAGE==7){//@@@
								L0=(0xFF0000)+(DWORD(W1)<<24);
								L1=(0xFF0000)+(DWORD(W2)<<24);
								L2=(0xFF0000)+(DWORD(W3)<<24);
								L3=(0xFF0000)+(DWORD(W4)<<24);
								if(V0==30993+VertInLine*5){
									//L0=(0xFFFFFFFF);//+(DWORD(W1)<<24);
								};
							}else{
								/*
								T1=23;
								T2=23;
								T3=23;
								T4=23;
								*/
								L0=(L00&0xFFFFFF)+(DWORD(W1)<<24);
								L1=(L10&0xFFFFFF)+(DWORD(W2)<<24);
								L2=(L20&0xFFFFFF)+(DWORD(W3)<<24);
								L3=(L30&0xFFFFFF)+(DWORD(W4)<<24);
							};
							int TMin1=T1;
							int TAve1=T2;
							int TMax1=T3;
							Sort3L(TMin1,TAve1,TMax1);

							int TMin2=T3;
							int TAve2=T2;
							int TMax2=T4;
							Sort3R(TMin2,TAve2,TMax2);

							VER[0].x=x+GETXSHIFT(V0);
							VER[1].x=x+TQuant+GETXSHIFT(V1);
							VER[2].x=x+GETXSHIFT(V2);

							VER[0].y=y+Y0-Gy+GETYSHIFT(V0);
							VER[1].y=y+Y1-Gy+GETYSHIFT(V1);
							VER[2].y=y+Y2-Gy+GETYSHIFT(V2);

							VER[0].z=0.5-float(y+S0-Gy)/3000.0;
							VER[1].z=0.5-float(y+S1-Gy)/3000.0;
							VER[2].z=0.5-float(y+S2-Gy)/3000.0;

							VER[3].x=x+GETXSHIFT(V2);
							VER[4].x=x+TQuant+GETXSHIFT(V1);
							VER[5].x=x+TQuant+GETXSHIFT(V3);

							VER[3].y=y+Y2-Gy+GETYSHIFT(V2);
							VER[4].y=y+Y1-Gy+GETYSHIFT(V1);
							VER[5].y=y+Y3-Gy+GETYSHIFT(V3);

							VER[3].z=0.5-float(y+S2-Gy)/3000.0;
							VER[4].z=0.5-float(y+S1-Gy)/3000.0;
							VER[5].z=0.5-float(y+S3-Gy)/3000.0;

							VER[0].diffuse=L0;
							VER[1].diffuse=L1;
							VER[2].diffuse=L2;
							VER[3].diffuse=L2;
							VER[4].diffuse=L1;
							VER[5].diffuse=L3;
							
							VERTICES[NVR  ]=V0;
							VERTICES[NVR+1]=V1;
							VERTICES[NVR+2]=V2;
							NVR+=3;

							VERTICES[NVR  ]=V2;
							VERTICES[NVR+1]=V1;
							VERTICES[NVR+2]=V3;

							VER[0].w=GetRHW(V0);
							VER[1].w=GetRHW(V1);
							VER[2].w=GetRHW(V2);
							VER[3].w=GetRHW(V2);
							VER[4].w=GetRHW(V1);
							VER[5].w=GetRHW(V3);

							NVR+=3;


							float dx0=GetBaseU(TMin1)+float(GetVValue(V0,71)&31)/TRISCALE;
							float dy0=GetBaseV(TMin1)+float(GetVValue(V0,77)%VVV)/TRISCALE;

							VER[0].u=dx0;
							VER[0].v=dy0;

							VER[1].u=dx0+fTQuant;
							VER[1].v=dy0+fHQuant;

							VER[2].u=dx0;
							VER[2].v=dy0+fTQuant;

							dx0=GetBaseU(TMin2)+float(GetVValue(V1,73)&31)/TRISCALE;
							dy0=GetBaseV(TMin2)+float(GetVValue(V0,79)%VVV)/TRISCALE;

							VER[3].u=dx0;
							VER[3].v=dy0+fHQuant;

							VER[4].u=dx0+fTQuant;
							VER[4].v=dy0;

							VER[5].u=dx0+fTQuant;
							VER[5].v=dy0+fTQuant;

							NormalizeVertexTexL(VER[0],VER[1],VER[2],TMin1,TMin1,0,V0);
							NormalizeVertexTexR(VER[3],VER[4],VER[5],TMin2,TMin2,0,V1);

							//drawing more triangles
							assert(!(STAGE==2&&VER[0].diffuse&0xFF000000==0x00));
							int NV=6;
							if(TAve1!=TMin1){
								VER[NV  ]=VER[0];
								VER[NV+1]=VER[1];
								VER[NV+2]=VER[2];

								dx0=GetBaseU(TAve1)+float(GetVValue(V3,93)&31)/TRISCALE;
								dy0=GetBaseV(TAve1)+float(GetVValue(V3,97)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0+fHQuant;

								VER[NV+2].u=dx0;
								VER[NV+2].v=dy0+fTQuant;

								if(T1!=TAve1)GetDW_LMin(VER[NV  ].diffuse,V0);
								if(T2!=TAve1)GetDW_LMin(VER[NV+1].diffuse,V1);
								if(T3!=TAve1)GetDW_LMin(VER[NV+2].diffuse,V2);

								VERTICES[NVR  ]=V0;
								VERTICES[NVR+1]=V1;
								VERTICES[NVR+2]=V2;

								NormalizeVertexTexL(VER[NVR  ],VER[NVR+1],VER[NVR+2],TMin1,TAve1,0,V2);

								NVR+=3;

								NV+=3;
							};
							if(TMax1!=TMin1&&TMax1!=TAve1){
								VER[NV  ]=VER[0];
								VER[NV+1]=VER[1];
								VER[NV+2]=VER[2];

								dx0=GetBaseU(TMax1)+float(GetVValue(V1,77)&31)/TRISCALE;
								dy0=GetBaseV(TMax1)+float(GetVValue(V0,67)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0+fHQuant;

								VER[NV+2].u=dx0;
								VER[NV+2].v=dy0+fTQuant;

								if(T1!=TMax1)GetDW_LMin(VER[NV  ].diffuse,V0);
								if(T2!=TMax1)GetDW_LMin(VER[NV+1].diffuse,V1);
								if(T3!=TMax1)GetDW_LMin(VER[NV+2].diffuse,V2);

								VERTICES[NVR  ]=V0;
								VERTICES[NVR+1]=V1;
								VERTICES[NVR+2]=V2;

								NormalizeVertexTexL(VER[NVR  ],VER[NVR+1],VER[NVR+2],TMax1,TAve1,0,V3);

								NVR+=3;

								NV+=3;
							};
							if(TAve2!=TMin2){
								VER[NV  ]=VER[3];
								VER[NV+1]=VER[4];
								VER[NV+2]=VER[5];

								dx0=GetBaseU(TAve2)+float(GetVValue(V0,63)&31)/TRISCALE;
								dy0=GetBaseV(TAve2)+float(GetVValue(V1,97)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0+fHQuant;

								VER[NV+2].u=dx0;
								VER[NV+2].v=dy0+fTQuant;

								if(T3!=TAve2)GetDW_LMin(VER[NV  ].diffuse,V2);
								if(T2!=TAve2)GetDW_LMin(VER[NV+1].diffuse,V1);
								if(T4!=TAve2)GetDW_LMin(VER[NV+2].diffuse,V3);
								VERTICES[NVR  ]=V2;
								VERTICES[NVR+1]=V1;
								VERTICES[NVR+2]=V3;

								NormalizeVertexTexR(VER[NVR  ],VER[NVR+1],VER[NVR+2],TMin2,TAve2,0,V1);

								NVR+=3;
								NV+=3;
							};
							if(TMax2!=TMin2&&TMax2!=TAve2){
								VER[NV  ]=VER[3];
								VER[NV+1]=VER[4];
								VER[NV+2]=VER[5];

								dx0=GetBaseU(TMax2)+float(GetVValue(V0,93)&31)/TRISCALE;
								dy0=GetBaseV(TMax2)+float(GetVValue(V0,61)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0+fHQuant;

								VER[NV+2].u=dx0;
								VER[NV+2].v=dy0+fTQuant;

								if(T3!=TMax2)GetDW_LMin(VER[NV  ].diffuse,V2);
								if(T2!=TMax2)GetDW_LMin(VER[NV+1].diffuse,V1);
								if(T4!=TMax2)GetDW_LMin(VER[NV+2].diffuse,V3);
								VERTICES[NVR  ]=V2;
								VERTICES[NVR+1]=V1;
								VERTICES[NVR+2]=V3;

								NormalizeVertexTexR(VER[NVR  ],VER[NVR+1],VER[NVR+2],TAve2,TMax2,0,V2);

								NVR+=3;
								NV+=3;
							};
							GNV+=NV;
							STriang->setNInd(GNV);
							STriang->setNVert(GNV);
							STriang->setNPri(GNV/3);

							for(int j=0;j<NVR;j++){
								int VV=VERTICES[j];
								int vx=VV%VertInLine;
								int vy=VV/VertInLine;
							};
							//IRS->Draw(*STriang);
							if(STAGE==2){
								if(T1!=TMin1)GetDW_LMin(VER[0].diffuse,V0);
								if(T2!=TMin1)GetDW_LMin(VER[1].diffuse,V1);
								if(T3!=TMin1)GetDW_LMin(VER[2].diffuse,V2);
								if(T3!=TMin2)GetDW_LMin(VER[3].diffuse,V2);
								if(T2!=TMin2)GetDW_LMin(VER[4].diffuse,V1);
								if(T4!=TMin2)GetDW_LMin(VER[5].diffuse,V3);
							};
							VER+=NV;
						};
					}while(STAGE!=2);
				};
			}else{
				NVR=0;
				int Y=yp0*HQuant;

				int S0=Y;
				int S1=Y-SQuant;
				int S2=Y+HQuant;
				int S3=Y+SQuant;

				int Y0=S0-(THMAP(p0)>>ScShift);
				int Y1=S1-(THMAP(p0+1)>>ScShift);
				int Y2=S2-(THMAP(p0+VertInLine)>>ScShift);
				int Y3=S3-(THMAP(p0+VertInLine+1)>>ScShift);

				YA=Y0;
				if(!((Y0<Gy&&Y1<Gy&&Y2<Gy&&Y3<Gy)||(Y0>Gy1&&Y1>Gy1&&Y2>Gy1&&Y3>Gy1))){
					int V0=p0;
					int V1=p0+1;
					int V2=p0+VertInLine;
					int V3=p0+VertInLine+1;

					DWORD L00=GetDW_Light(V0);
					DWORD L10=GetDW_Light(V1);
					DWORD L20=GetDW_Light(V2);
					DWORD L30=GetDW_Light(V3);

					int STAGE=0;
					do{
						byte T1,T2,T3,T4,W1,W2,W3,W4;
						STAGE++;
						if(STAGE==1){
							T1=TexMap[p0];
							T2=TexMap[p0+1];
							T3=TexMap[p0+VertInLine];
							T4=TexMap[p0+VertInLine+1];
							W1=WTexMapEx[p0];
							W2=WTexMapEx[p0+1];
							W3=WTexMapEx[p0+VertInLine];
							W4=WTexMapEx[p0+VertInLine+1];
							PLAINMODE=W1>80||W2>80||W3>80||W4>80;
							W1=0xFF;
							W2=0xFF;
							W3=0xFF;
							W4=0xFF;
						}else{
							T1=TexMapEx[p0];
							T2=TexMapEx[p0+1];
							T3=TexMapEx[p0+VertInLine];
							T4=TexMapEx[p0+VertInLine+1];
							W1=WTexMapEx[p0];
							W2=WTexMapEx[p0+1];
							W3=WTexMapEx[p0+VertInLine];
							W4=WTexMapEx[p0+VertInLine+1];
							SCAL(W1);
							SCAL(W2);
							SCAL(W3);
							SCAL(W4);
							/*
							W1=0xFF;
							W2=0xFF;
							W3=0xFF;
							W4=0xFF;
							*/
							/*
							W1=0;
							W2=0;
							W3=0;
							W4=0;
							*/
							
							PLAINMODE=1;
						};
						if(W1||W2||W3||W4){
							if(W1<1)W1=1;
							if(W2<1)W2=1;
							if(W3<1)W3=1;
							if(W4<1)W4=1;
							DWORD L0,L1,L2,L3;
							if(STAGE==7){//@@@
								L0=(0xFF0000)+(DWORD(W1)<<24);
								L1=(0xFF0000)+(DWORD(W2)<<24);
								L2=(0xFF0000)+(DWORD(W3)<<24);
								L3=(0xFF0000)+(DWORD(W4)<<24);
							}else{
								/*
								T1=27;
								T2=27;
								T3=27;
								T4=27;
								*/
								L0=(L00&0xFFFFFF)+(DWORD(W1)<<24);
								L1=(L10&0xFFFFFF)+(DWORD(W2)<<24);
								L2=(L20&0xFFFFFF)+(DWORD(W3)<<24);
								L3=(L30&0xFFFFFF)+(DWORD(W4)<<24);
							};
							int TMin1=T1;
							int TAve1=T2;
							int TMax1=T4;
							Sort3R(TMin1,TAve1,TMax1);

							int TMin2=T1;
							int TAve2=T4;
							int TMax2=T3;
							Sort3L(TMin2,TAve2,TMax2);


							VER[0].x=x+GETXSHIFT(V0);
							VER[1].x=x+TQuant+GETXSHIFT(V1);
							VER[2].x=x+TQuant+GETXSHIFT(V3);

							VER[0].y=y+Y0-Gy+GETYSHIFT(V0);
							VER[1].y=y+Y1-Gy+GETYSHIFT(V1);
							VER[2].y=y+Y3-Gy+GETYSHIFT(V3);

							VER[0].z=0.5-float(y+S0-Gy)/3000.0;
							VER[1].z=0.5-float(y+S1-Gy)/3000.0;
							VER[2].z=0.5-float(y+S3-Gy)/3000.0;

							VER[3].x=x+GETXSHIFT(V0);
							VER[4].x=x+TQuant+GETXSHIFT(V3);
							VER[5].x=x+GETXSHIFT(V2);

							VER[3].y=y+Y0-Gy+GETYSHIFT(V0);
							VER[4].y=y+Y3-Gy+GETYSHIFT(V3);
							VER[5].y=y+Y2-Gy+GETYSHIFT(V2);

							VER[3].z=0.5-float(y+S0-Gy)/3000.0;
							VER[4].z=0.5-float(y+S3-Gy)/3000.0;
							VER[5].z=0.5-float(y+S2-Gy)/3000.0;

							VER[0].diffuse=L0;
							VER[1].diffuse=L1;
							VER[2].diffuse=L3;
							VER[3].diffuse=L0;
							VER[4].diffuse=L3;
							VER[5].diffuse=L2;

							float dx0=GetBaseU(TMin1)+float(GetVValue(V0,47)&31)/TRISCALE;
							float dy0=GetBaseV(TMin1)+float(GetVValue(V0,49)%VVV)/TRISCALE;

							VER[0].u=dx0;
							VER[0].v=dy0+fHQuant;

							VER[1].u=dx0+fTQuant;
							VER[1].v=dy0;

							VER[2].u=dx0+fTQuant;
							VER[2].v=dy0+fTQuant;

							dx0=GetBaseU(TMin2)+float(GetVValue(V0,111)&31)/TRISCALE;
							dy0=GetBaseV(TMin2)+float(GetVValue(V0,113)%VVV)/TRISCALE;

							VER[3].u=dx0;
							VER[3].v=dy0;

							VER[4].u=dx0+fTQuant;
							VER[4].v=dy0+fHQuant;

							VER[5].u=dx0;
							VER[5].v=dy0+fTQuant;

							VERTICES[NVR  ]=V0;
							VERTICES[NVR+1]=V1;
							VERTICES[NVR+2]=V3;
							NVR+=3;

							VERTICES[NVR  ]=V0;
							VERTICES[NVR+1]=V3;
							VERTICES[NVR+2]=V2;
							NVR+=3;

							NormalizeVertexTexR(VER[0],VER[1],VER[2],TMin1,TMin1,0,V1);
							NormalizeVertexTexL(VER[3],VER[4],VER[5],TMin2,TMin2,0,V3);

							VER[0].w=GetRHW(V0);
							VER[1].w=GetRHW(V1);
							VER[2].w=GetRHW(V3);
							VER[3].w=GetRHW(V0);
							VER[4].w=GetRHW(V3);
							VER[5].w=GetRHW(V2);
							//assert(VER[0].diffuse&0xFF000000==0&&STAGE==2);

							//drawing more triangles
							int NV=6;
							if(TAve1!=TMin1){
								VER[NV  ]=VER[0];
								VER[NV+1]=VER[1];
								VER[NV+2]=VER[2];

								dx0=GetBaseU(TAve1)+float(GetVValue(V0,171)&31)/TRISCALE;
								dy0=GetBaseV(TAve1)+float(GetVValue(V0,371)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0+fHQuant;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0;

								VER[NV+2].u=dx0+fTQuant;
								VER[NV+2].v=dy0+fTQuant;

								if(T1!=TAve1)GetDW_LMin(VER[NV  ].diffuse,V0);
								if(T2!=TAve1)GetDW_LMin(VER[NV+1].diffuse,V1);
								if(T4!=TAve1)GetDW_LMin(VER[NV+2].diffuse,V3);
								VERTICES[NVR  ]=V0;
								VERTICES[NVR+1]=V1;
								VERTICES[NVR+2]=V3;

								NormalizeVertexTexR(VER[NVR  ],VER[NVR+1],VER[NVR+2],TMin1,TAve1,0,V3);

								NVR+=3;
								NV+=3;
							};
							if(TMax1!=TMin1&&TMax1!=TAve1){
								VER[NV  ]=VER[0];
								VER[NV+1]=VER[1];
								VER[NV+2]=VER[2];

								dx0=GetBaseU(TMax1)+float(GetVValue(V0,711)&31)/TRISCALE;
								dy0=GetBaseV(TMax1)+float(GetVValue(V0,211)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0+fHQuant;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0;

								VER[NV+2].u=dx0+fTQuant;
								VER[NV+2].v=dy0+fTQuant;

								if(T1!=TMax1)GetDW_LMin(VER[NV  ].diffuse,V0);
								if(T2!=TMax1)GetDW_LMin(VER[NV+1].diffuse,V1);
								if(T4!=TMax1)GetDW_LMin(VER[NV+2].diffuse,V3);
								VERTICES[NVR  ]=V0;
								VERTICES[NVR+1]=V1;
								VERTICES[NVR+2]=V3;

								NormalizeVertexTexR(VER[NVR  ],VER[NVR+1],VER[NVR+2],TAve1,TMax1,0,V1);

								NVR+=3;
								NV+=3;
							};
							if(TAve2!=TMin2){
								VER[NV  ]=VER[3];
								VER[NV+1]=VER[4];
								VER[NV+2]=VER[5];

								dx0=GetBaseU(TAve2)+float(GetVValue(V0,79)&31)/TRISCALE;
								dy0=GetBaseV(TAve2)+float(GetVValue(V0,39)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0+fHQuant;

								VER[NV+2].u=dx0;
								VER[NV+2].v=dy0+fTQuant;

								if(T1!=TAve2)GetDW_LMin(VER[NV  ].diffuse,V0);
								if(T4!=TAve2)GetDW_LMin(VER[NV+1].diffuse,V3);
								if(T3!=TAve2)GetDW_LMin(VER[NV+2].diffuse,V2);
								VERTICES[NVR  ]=V0;
								VERTICES[NVR+1]=V3;
								VERTICES[NVR+2]=V2;

								NormalizeVertexTexL(VER[NVR  ],VER[NVR+1],VER[NVR+2],TMin2,TAve2,0,V1);

								NVR+=3;

								NV+=3;
							};
							if(TMax2!=TMin2&&TMax2!=TAve2){
								VER[NV  ]=VER[3];
								VER[NV+1]=VER[4];
								VER[NV+2]=VER[5];

								dx0=GetBaseU(TMax2)+float(GetVValue(V0,29)&31)/TRISCALE;
								dy0=GetBaseV(TMax2)+float(GetVValue(V0,37)%VVV)/TRISCALE;

								VER[NV].u=dx0;
								VER[NV].v=dy0;

								VER[NV+1].u=dx0+fTQuant;
								VER[NV+1].v=dy0+fHQuant;

								VER[NV+2].u=dx0;
								VER[NV+2].v=dy0+fTQuant;

								if(T1!=TMax2)GetDW_LMin(VER[NV  ].diffuse,V0);
								if(T4!=TMax2)GetDW_LMin(VER[NV+1].diffuse,V3);
								if(T3!=TMax2)GetDW_LMin(VER[NV+2].diffuse,V2);
								VERTICES[NVR  ]=V0;
								VERTICES[NVR+1]=V3;
								VERTICES[NVR+2]=V2;

								NormalizeVertexTexL(VER[NVR  ],VER[NVR+1],VER[NVR+2],TAve2,TMax2,0,V2);

								NVR+=3;

								NV+=3;
							};


							GNV+=NV;
							STriang->setNInd(GNV);
							STriang->setNVert(GNV);
							STriang->setNPri(GNV/3);
							for(int j=0;j<NVR;j++){
								int VV=VERTICES[j];
								int vx=VV%VertInLine;
								int vy=VV/VertInLine;
							};
							if(STAGE==2){
								if(T1!=TMin1)GetDW_LMin(VER[0].diffuse,V0);
								if(T2!=TMin1)GetDW_LMin(VER[1].diffuse,V1);
								if(T4!=TMin1)GetDW_LMin(VER[2].diffuse,V3);
								if(T1!=TMin2)GetDW_LMin(VER[3].diffuse,V0);
								if(T4!=TMin2)GetDW_LMin(VER[4].diffuse,V3);
								if(T3!=TMin2)GetDW_LMin(VER[5].diffuse,V2);
							};
							//IRS->Draw(*STriang);
							VER+=NV;
						};
					}while(STAGE!=2);
				};
			};
			if(NVR){
				//VG=GVG+NGFVERT;
				for(int q=0;q<6;q+=3){
					int v1=VERTICES[q  ];
					int v2=VERTICES[q+1];
					int v3=VERTICES[q+2];
					int W1=GetFactureWeightByIdx(v1);
					int W2=GetFactureWeightByIdx(v2);
					int W3=GetFactureWeightByIdx(v3);
					if(W1||W2||W3){
						int fdx1,fdy1,fdx2,fdy2,fdx3,fdy3;
						int F1=GetFactureIdx(v1,fdx1,fdy1);
						int F2=GetFactureIdx(v2,fdx2,fdy2);
						int F3=GetFactureIdx(v3,fdx3,fdy3);
						if(W1){
							VG[0].z=V0[q  ].z;
							VG[1].z=V0[q+1].z;
							VG[2].z=V0[q+2].z;
							VG[0].w=1;
							VG[1].w=1;
							VG[2].w=1;
							VG[0].x=V0[q ].x;
							VG[0].y=V0[q ].y;
							VG[1].x=V0[q+1].x;
							VG[1].y=V0[q+1].y;
							VG[2].x=V0[q+2].x;
							VG[2].y=V0[q+2].y;
							int maxw1,int maxw2,int maxw3;
							DWORD Diff1,Diff2,Diff3;
							GetFactureUVW(v1,F1,VG[0].u,VG[0].v,maxw1,Diff1);
							GetFactureUVW(v2,F1,VG[1].u,VG[1].v,maxw2,Diff2);
							GetFactureUVW(v3,F1,VG[2].u,VG[2].v,maxw3,Diff3);
							if(F2!=F1)maxw2=0;
							if(F3!=F1)maxw3=0;
							if(W1<maxw1)maxw1=W1;
							if(W2<maxw2)maxw2=W2;
							if(W3<maxw3)maxw3=W3;
							VG[0].diffuse=(DWORD(maxw1)<<24)+Diff1;
							VG[1].diffuse=(DWORD(maxw2)<<24)+Diff2;
							VG[2].diffuse=(DWORD(maxw3)<<24)+Diff3;
							TIDX[NGFVERT]=F1&255;
							NGFVERT+=3;
							VG+=3;
						};
						if(W2&&F2!=F1){
							VG[0].z=V0[q  ].z;
							VG[1].z=V0[q+1].z;
							VG[2].z=V0[q+2].z;
							VG[0].w=1;
							VG[1].w=1;
							VG[2].w=1;
							VG[0].x=V0[q ].x;
							VG[0].y=V0[q ].y;
							VG[1].x=V0[q+1].x;
							VG[1].y=V0[q+1].y;
							VG[2].x=V0[q+2].x;
							VG[2].y=V0[q+2].y;
							int maxw1,int maxw2,int maxw3;
							DWORD Diff1,Diff2,Diff3;
							GetFactureUVW(v1,F2,VG[0].u,VG[0].v,maxw1,Diff1);
							GetFactureUVW(v2,F2,VG[1].u,VG[1].v,maxw2,Diff2);
							GetFactureUVW(v3,F2,VG[2].u,VG[2].v,maxw3,Diff3);
							maxw1=0;
							if(F3!=F2)maxw3=0;
							if(W2<maxw2)maxw2=W2;
							if(W3<maxw3)maxw3=W3;
							VG[0].diffuse=(DWORD(maxw1)<<24)+Diff1;
							VG[1].diffuse=(DWORD(maxw2)<<24)+Diff2;
							VG[2].diffuse=(DWORD(maxw3)<<24)+Diff3;
							TIDX[NGFVERT]=F2&255;
							NGFVERT+=3;
							VG+=3;							
						};
						if(W3&&F3!=F1&&F3!=F2){
							VG[0].z=V0[q  ].z;
							VG[1].z=V0[q+1].z;
							VG[2].z=V0[q+2].z;
							VG[0].w=1;
							VG[1].w=1;
							VG[2].w=1;
							VG[0].x=V0[q ].x;
							VG[0].y=V0[q ].y;
							VG[1].x=V0[q+1].x;
							VG[1].y=V0[q+1].y;
							VG[2].x=V0[q+2].x;
							VG[2].y=V0[q+2].y;
							int maxw1,int maxw2,int maxw3;
							DWORD Diff1,Diff2,Diff3;
							GetFactureUVW(v1,F2,VG[0].u,VG[0].v,maxw1,Diff1);
							GetFactureUVW(v2,F2,VG[1].u,VG[1].v,maxw2,Diff2);
							GetFactureUVW(v3,F2,VG[2].u,VG[2].v,maxw3,Diff3);
							maxw1=0;
							maxw2=0;
							if(W3<maxw3)maxw3=W3;
							VG[0].diffuse=(DWORD(maxw1)<<24)+Diff1;
							VG[1].diffuse=(DWORD(maxw2)<<24)+Diff2;
							VG[2].diffuse=(DWORD(maxw3)<<24)+Diff3;
							TIDX[NGFVERT]=F3&255;
							NGFVERT+=3;
							VG+=3;							
						};
					};
				};
			};
			yp0++;
			p0+=VertInLine;
		}while(yp0<MaxTH-1&&YA<Gy1+1000);
		//drawing stones
		float Scale=0.5;
		float fsize=TQuant*Scale;
		for(int j=1000;j<NStones;j++){
			for(int p=0;p<8;p++){
				int xc=StoneX[j];
				int yc=StoneY[j];
				if(p){
					xc+=(rand()&31)-15;
					yc+=(rand()&15)-12+p+p+p;
				};
				VST[0].x=xc-fsize;
				VST[0].y=yc-fsize;

				VST[1].x=xc+fsize;
				VST[1].y=yc-fsize;

				VST[2].x=xc-fsize;
				VST[2].y=yc+fsize;

				VST[3].x=xc+fsize;
				VST[3].y=yc+fsize;
				int sid=rand()%16;
				float sx=float(sid&3)*0.25;
				float sy=float((sid>>2)&3)*0.25;
				float tx=float(StoneTex[j]&3)*0.25;
				float ty=float((StoneTex[j]>>2)&3)*0.25;
				VST[0].u=tx;
				VST[0].v=ty;
				VST[1].u=tx+0.25;
				VST[1].v=ty;
				VST[2].u=tx;
				VST[2].v=ty+0.25;
				VST[3].u=tx+0.25;
				VST[3].v=ty+0.25;

				VST[0].u2=sx;
				VST[0].v2=sy;
				VST[1].u2=sx+0.25;
				VST[1].v2=sy;
				VST[2].u2=sx;
				VST[2].v2=sy+0.25;
				VST[3].u2=sx+0.25;
				VST[3].v2=sy+0.25;

				DWORD C=StoneColor[j];
				VST[0].diffuse=C;
				VST[1].diffuse=C;
				VST[2].diffuse=C;
				VST[3].diffuse=C;
				//IRS->SaveTexture(StoneMesh->getTexture(1),"otx.dds");

				IRS->Draw(*StoneMesh);
			};
		};
		mx++;
		x+=TQuant;
	};
	IRS->Draw(*STriang);
	GroundMesh->setShader(LightShader);
	//memset(&GARRAY,0,sizeof GARRAY);
	memset(&NInGarr,0,sizeof NInGarr);
	//memset(&MaxInGarr,0,sizeof MaxInGarr);
	for(int i=0;i<NGFVERT;i+=3){
		int fid=TIDX[i];
		if(fid<GetNFactures()){
			if(NInGarr[fid]>=MaxInGarr[fid]){
				MaxInGarr[fid]+=512*3;
				GARRAY[fid]=(VertexTnL*)realloc(GARRAY[fid],MaxInGarr[fid]*sizeof VertexTnL);
			};
			int ng=NInGarr[fid];
			memcpy(GARRAY[fid]+ng,GVG+i,3*sizeof VertexTnL);
			NInGarr[fid]+=3;
		}
	};
	for(i=0;i<GetNFactures();i++)if(GARRAY[i]){
		float Contrast;
		float Bright;
        int bt=GetFactureBump(i,Contrast,Bright);		
		GroundMesh->setVertexPtr(GARRAY[i]);
		GroundMesh->setNVert(NInGarr[i]);
		GroundMesh->setNInd(NInGarr[i]);
		GroundMesh->setNPri(NInGarr[i]/3);
		//GroundMesh->setTexture(F3D.GTex[i].TextureID);
		GroundMesh->setTexture(GetFacture(i));

		if(bt!=-1){
			GroundMesh->setShader(DotShader);
			GroundMesh->setTexture(bt,0);		
			GroundMesh->setTexture(GetFacture(i),1);
			int N=GroundMesh->getNVert();
			VertexTnL* VS=(VertexTnL*)GroundMesh->getVertexData();
			for(int j=0;j<N;j++){
				VS[j].diffuse=(VS[j].diffuse&0xFF000000)+GetBumpDiffuseForVertex(VS[j].diffuse&0xFFFFFF,Contrast,Bright);
			}
		}else{
			GroundMesh->setShader(LightShader);
			GroundMesh->setTexture(GetFacture(i));
		}

		IRS->Draw(*GroundMesh);
		//free(GARRAY[i]);
	};
	GroundMesh->setVertexPtr(LVG);
	//IRS->ClearDeviceZBuffer();

	IRS->SetViewPort(vp);
//	DrawGSprites(DevID,mx0<<5,Gy<<ScShift,vp.x,vp.y,vp.w,vp.h,ScShift);
	DrawRoads(x0,y,mx0,Gy,mLx<<5,GLy,ScShift);

	vp.x=0;
	vp.y=0;
	vp.w=RealLx;
	vp.h=RealLy;
	IRS->SetViewPort(vp);
	void TestNets();
	TestNets();
};
//-----------------------ROADS-------------------//
struct OneRoadDesc{
	int TextureID;
	char RoadName[32]; 
	int TexSizeX;
	int TexSizeY;
	int ColorY0;
	int ColorY1;
	int ReliefY0;
	int ReliefY1;
	int RWidth;
	int RScaleX;
	int AlphaFrequency;
	int AFactor;
	int AFactorMin;
	int RFactor;
	int GFactor;
	int BFactor;
	//extended information
	int GroupFirstID;
	int GroupID;
	int MapTextureID;
	int TexRMin;
	int TexRMax;
	int TexSmoothness;
	int FactureID;
	int RoadPreviewID;
	int RoadPreviewSpr;
	//physical
	short Speed; //256-normal speed
	short Tiring;//256-normal  tiring
	short Priory;//256-normal  priory
	//------BUMP------
	int BumpTextureID;
	int B1_Y1;
	int B1_Y2;
	int B2_Y1;
	int B2_Y2;
	//--------------------
	DWORD Diffuse;
};
#define NRDEP 50
int RDEP[]={
	16,17,18,20,22,
	18,17,16,19,23,
	25,27,32,25,27,
	23,18,16,19,22,
	23,24,25,26,21,
	18,19,17,16,17,
	18,19,17,16,20,
	22,23,24,26,24,
	25,27,26,24,20,
	19,18,17,16,17};

class OneRoad{
public:
	int Width;
	float* XY;
	DWORD* Weights;
	int Np;
	int Maxp;
	int XMin,YMin,XMax,YMax;
	int Type;
	int GetDistance(int x,int y,int precise,int& pos){
		int R0=10000000;
		for(int i=0;i<Np;i+=precise){
			int R=Norm(x-XY[i+i],y-XY[i+i+1]);
			if(R<R0){
				R0=R;
				pos=i;
			};
		};
		return R0;
	};
	void SurroundWithTexture();
};
class RoadDesc{
public:
	BaseMesh* RoadMesh;
	BaseMesh* ColorRoadMesh;
	OneRoadDesc* Roads;
	int NGrps;
	int GrpsElements[256];
	int NRoadsDesc;
	~RoadDesc(){
		if(RoadMesh){
			delete(RoadMesh);
			delete(ColorRoadMesh);
			delete(Roads);
		};
		memset(this,0,sizeof *this);
	};
};
class RoadsSystem{
public:
	RoadDesc* RDesc;
	OneRoad* Roads;
	int NRoads;
	RoadsSystem();
	int AddRoad(int Type);
	void AddPointToRoad(int idx,int x,int y);
	void ClearAll();
	void ClearRoads();
	void Draw(int sx,int sy,int mx,int my,int Lx,int Ly,int scale);
	void Init();
	~RoadsSystem(){
		ClearAll();
	};
};
void RoadsSystem::ClearAll(){
	for(int i=0;i<NRoads;i++){
		if(Roads[i].XY)free(Roads[i].XY);
		if(Roads[i].Weights)free(Roads[i].Weights);
	}
	if(Roads)delete[](Roads);
	if(RDesc)delete(RDesc);
	memset(this,0,sizeof * this);
};
void RoadsSystem::ClearRoads(){
	for(int i=0;i<NRoads;i++){
		if(Roads[i].XY)free(Roads[i].XY);
		if(Roads[i].Weights)free(Roads[i].Weights);
	}
	if(Roads)delete[](Roads);
	Roads=NULL;
	NRoads=0;
};
#define MAXROAD 1024
#define MAXROADDESC 64
RoadsSystem::RoadsSystem(){
	memset(this,0,sizeof *this);
};
int RoadsSystem::AddRoad(int Type){
	OneRoad* R=Roads;
	Roads=new OneRoad[NRoads+1];
	if(R){
		memcpy(Roads,R,NRoads*sizeof OneRoad);
		delete[](R);
	};
	OneRoad* OR=Roads+NRoads;
	memset(OR,0,sizeof *OR);
	OR->Type=Type;
	OR->Width=RDesc->Roads[Type].RWidth;
	NRoads++;
	return NRoads-1;
};
int GetRV(int x){
	int d=x>>6;
	int r=x&63;
	int V1=randoma[d&8191];
	int V2=randoma[(d+1)&8191];
	return V1+(((V2-V1)*r)>>6);
};
int Lim(int x){
	return x<0?x=0:x>255?255:x;
};
void RoadsSystem::AddPointToRoad(int idx,int x,int y){
	if(idx<NRoads){
		OneRoad* RO=Roads+idx;
		if(RO->Np>=RO->Maxp){
			RO->Maxp+=256;
			RO->XY=(float*)realloc(RO->XY,RO->Maxp<<3);
			RO->Weights=(DWORD*)realloc(RO->Weights,RO->Maxp<<3);
		};
		RO->XY[RO->Np<<1]=x;
		RO->XY[1+(RO->Np<<1)]=y;
		OneRoadDesc* RD=RDesc->Roads+RO->Type;

		int W=GetRV(RO->Np*RD->AlphaFrequency);
		DWORD A=RD->AFactorMin+(((RD->AFactor-RD->AFactorMin)*W)>>15);
		int Lt=GetLighting3DInPoint(x,y);
		int R=Lim((RD->RFactor*Lt)/128);
		int G=Lim((RD->GFactor*Lt)/128);
		int B=Lim((RD->BFactor*Lt)/128);

		RO->Weights[RO->Np<<1]=(A<<24)+(Lim((R*A)>>7)<<16)+(Lim((G*A)>>7)<<8)+Lim((B*A)>>7);
		W=32767-W;//GetRV((RO->Np+1103)*RD->AlphaFrequency);
		A=RD->AFactorMin+(((RD->AFactor-RD->AFactorMin)*W)>>15);
		RO->Weights[(RO->Np<<1)+1]=(A<<24)+(Lim((R*A)>>7)<<16)+(Lim((G*A)>>7)<<8)+Lim((B*A)>>7);

		RO->Np++;
		RO->XMin=1000000;
		RO->YMin=1000000;
		RO->XMax=0;
		RO->YMax=0;
		for(int i=0;i<RO->Np;i++){
			int X=RO->XY[i+i  ];
			int Y=RO->XY[i+i+1];
			int H=GetTotalHeight(X,Y);
			int YS=(Y>>1)-H;
			if(X<RO->XMin)RO->XMin=X;
			if(YS<RO->YMin)RO->YMin=YS;
			if(X>RO->XMax)RO->XMax=X;
			if(YS>RO->YMax)RO->YMax=YS;
		};
		RO->XMin-=64;
		RO->YMin-=64;
		RO->XMax+=64;
		RO->YMax+=64;
		int mx=x>>5;
		int my=((y>>1)-GetTotalHeight(x,y))>>4;
		MakeDirtyGBUF(mx-4,my-6,8,12);
	};
};
void RoadsSystem::Init(){
	if(RDesc)return;
	RDesc=new RoadDesc;
	RDesc->RoadMesh=new BaseMesh;
	RDesc->RoadMesh->create(2+MAXROAD*2,6*MAXROAD,vfTnL2);
	RDesc->ColorRoadMesh=new BaseMesh;
	RDesc->ColorRoadMesh->create(2+MAXROAD*2,6*MAXROAD,vfTnL);
	word* ids=RDesc->RoadMesh->getIndices();
	word* cids=RDesc->ColorRoadMesh->getIndices();
	for(int i=0;i<MAXROAD;i++){
		int i6=i*6;
		int i2=i<<1;
		ids [i6  ]=i2;
		ids [i6+1]=i2+3;
		ids [i6+2]=i2+1;
		ids [i6+3]=i2;
		ids [i6+4]=i2+2;
		ids [i6+5]=i2+3;

		cids[i6  ]=i2;
		cids[i6+1]=i2+3;
		cids[i6+2]=i2+1;
		cids[i6+3]=i2;
		cids[i6+4]=i2+2;
		cids[i6+5]=i2+3;
	};
	RDesc->RoadMesh->setShader(IRS->GetShaderID("road"));
	//loading config
	GFILE* F=Gopen("Roads.dat","r");
	if(F){
		int z;
		RDesc->Roads=new OneRoadDesc[MAXROADDESC];
		do{
			//texname tLx tLy  ColorY0 ColorY1 ReliafY0 ReliefY1
			char Tex[64];
			char Name[64];
			int p1,p2,p3,p4,p5,p6,p7,p8,a,r,g,b,a1,fr;
			z=Gscanf(F,"%s",Name);
			if(z==1){
				if(!stricmp(Name,"$EX")){
					OneRoadDesc* RD=RDesc->Roads+RDesc->NRoadsDesc;
					memcpy(RD,RD-1,sizeof OneRoadDesc);
					z=Gscanf(F,"%d%d%d%d%d",&p1,&p2,&p3,&p4,&p5);
					if(z==5){
						RD->MapTextureID=p1;
						RD->TexRMin=p2;
						RD->TexRMax=p3;
						RD->TexSmoothness=p4;
						RD->FactureID=p5;
						RDesc->NRoadsDesc++;
					};
				}else
				if(!stricmp(Name,"$PHYS")){
					OneRoadDesc* RD=RDesc->Roads+RDesc->NRoadsDesc;
					memcpy(RD,RD-1,sizeof OneRoadDesc);
					z=Gscanf(F,"%d%d%d",&p1,&p2,&p3);
					if(z==3){
						RD->Speed =p1;
						RD->Tiring=p2;
						RD->Priory=p3;
					};
				}else
				if(!stricmp(Name,"$BUMP")){
					OneRoadDesc* RD=RDesc->Roads+RDesc->NRoadsDesc-1;
					z=Gscanf(F,"%s%d%d%d%d",Tex,&p1,&p2,&p3,&p4);
					if(z==5){
						RD->BumpTextureID=IRS->GetTextureID(Tex);
						RD->B1_Y1=p1;
						RD->B1_Y2=p2;
						RD->B2_Y1=p3;
						RD->B2_Y2=p4;
					};
					z=5;
				}else
				if(!stricmp(Name,"$PREVIEW")){
					OneRoadDesc* RD=RDesc->Roads+RDesc->NRoadsDesc-1;
					z=Gscanf(F,"%s%d",Tex,&p1);
					if(z==2){
						RD->RoadPreviewID=GPS.PreLoadGPImage(Tex);
						RD->RoadPreviewSpr=p1;
					};
					z=5;
				}else{
					z=Gscanf(F,"%s%d%d%d%d%d%d%d%d%d%d%d%d%d%d",Tex,&p1,&p2,&p3,&p4,&p5,&p6,&p7,&p8,&fr,&a,&a1,&r,&g,&b);
					if(z==15){
						if(!fr)fr=1;
						OneRoadDesc* RD=RDesc->Roads+RDesc->NRoadsDesc;
						RDesc->NRoadsDesc++;
						RD->TextureID=IRS->GetTextureID(Tex);
						RD->TexSizeX =p1;
						RD->TexSizeY =p2;
						RD->ColorY0  =p3;
						RD->ColorY1  =p4;
						RD->ReliefY0 =p5;
						RD->ReliefY1 =p6;
						RD->RWidth   =p7;
						RD->RScaleX  =p8;
						RD->AlphaFrequency=fr;
						RD->AFactor  =a;
						RD->AFactorMin=a1;
						RD->RFactor  =r;
						RD->GFactor  =g;
						RD->BFactor  =b;
						RD->Diffuse=(a<<24)+(r<<16)+(g<<8)+b;
						RD->MapTextureID=-1;
						RD->BumpTextureID=-1;
						RD->Speed =256+64;
						RD->Tiring=-64;
						RD->Priory=256+400;
						strcpy(RD->RoadName,Name);
					};
				};
			};
		}while((z==15||z==5)&&RDesc->NRoadsDesc<MAXROADDESC);
		Gclose(F);
		//groups analysing
		RDesc->NGrps=0;
		for(int p=0;p<RDesc->NRoadsDesc;p++){
			OneRoadDesc* RD=RDesc->Roads+p;
			char* RN=RD->RoadName;
			RD->GroupFirstID=-1;
			for(int q=0;q<RDesc->NGrps;q++)if(!strcmp(RDesc->Roads[RDesc->GrpsElements[q]].RoadName,RN)){
				RD->GroupFirstID=RDesc->GrpsElements[q];
				RD->GroupID=q;
			};
			if(RD->GroupFirstID==-1){
				RDesc->GrpsElements[RDesc->NGrps]=p;
				RD->GroupFirstID=p;
				RD->GroupID=RDesc->NGrps;
				RDesc->NGrps++;
			};

		};
	};
	//testing
	/*
	int ns=360;
	int R=AddRoad(0);
	for(i=0;i<ns;i++){
		AddPointToRoad(R,700+cos(float(i)*0.1)*(150+i*2),1000+sin(float(i)*0.1)*(150+i*2));
	};
	*/
};

void TransDif(DWORD& D,int v){
	DWORD A=D>>24;
	DWORD R=(D>>16)&255;
	DWORD G=(D>>8)&255;
	DWORD B=(D&255);
	A=(A*v)>>8;
	R=(R*v)>>8;
	G=(G*v)>>8;
	B=(B*v)>>8;
	D=(A<<24)+(R<<16)+(G<<8)+B;
};
const float fPI=3.1415f;
extern int HScale;
void RoadsSystem::Draw(int sx,int sy,int mx,int Gy,int Lx,int Ly,int scale){
	Init();
	int rx=mx<<(5-scale);
	int ry=Gy;
	int rx1=rx+Lx;
	int ry1=ry+Ly;
	OneRoad* R=Roads;
	float sc=1.0/float(1<<scale);
	float scy=0.75;
	for(int i=0;i<NRoads;i++){
		if( (R->XMin>>scale)<rx1&&(R->YMin>>scale)<ry1&&
            (R->XMax>>scale)>rx&&(R->YMax>>scale)>ry){
			int N=R->Np;
			OneRoadDesc* RD=RDesc->Roads+R->Type;
			VertexTnL2* VR=(VertexTnL2*)RDesc->RoadMesh->getVertexData();
			VertexTnL2* VR0=VR;

			VertexTnL* VC=(VertexTnL*)RDesc->ColorRoadMesh->getVertexData();
			int i2=0;
			float Upos=0;
			float Upos0;
			for(int i=0;i<N;i++){
				Upos0=Upos;
				int W=R->Width/2;
				int Xp,Yp,Xn,Yn;
				if(i>0){
					Xp=R->XY[i2-2];
					Yp=R->XY[i2-1];
				}else{
					Xp=R->XY[i2];
					Yp=R->XY[i2+1];
				};
				if(i<N-1){
					Xn=R->XY[i2+2];
					Yn=R->XY[i2+3];
				}else{
					Xn=R->XY[i2];
					Yn=R->XY[i2+1];
				};
				int X=R->XY[i2];
				int Y=R->XY[i2+1];
				float DX=Xn-Xp;
				float DY=Yn-Yp;
				float PX=DY;
				float PY=-DX;
				int NP=sqrt(PX*PX+PY*PY);
				Upos+=sqrt((Xn-X)*(Xn-X)+(Yn-Y)*(Yn-Y))*float(RD->RScaleX)/100.0;
				PX=PX*W/NP;
				PY=PY*W/NP*1.2;
				VR[0].x=sx+(X+PX)*sc-rx;
				VR[0].y=sy+((Y+PY)/2-GetTotalHeight(X+PX,Y+PY)*HScale/256)*sc-ry;
				VR[1].x=sx+(X-PX)*sc-rx;
				VR[1].y=sy+((Y-PY)/2-GetTotalHeight(X-PX,Y-PY)*HScale/256)*sc-ry;
				VR[0].u=Upos/float(RD->TexSizeX);
				VR[0].v=float(RD->ReliefY0)/float(RD->TexSizeY);
				VR[1].u=VR[0].u;
				VR[1].v=float(RD->ReliefY1)/float(RD->TexSizeY);
				VR[0].diffuse=R->Weights[i2];
				VR[1].diffuse=R->Weights[i2+1];
				if(i<8){
					TransDif(VR[0].diffuse,i*32);
					TransDif(VR[1].diffuse,i*32);
					//VR[0].diffuse=((RD->AFactor*i/8)<<24)+((RD->RFactor*i/8)<<16)+((RD->GFactor*i/8)<<8)+(RD->BFactor*i/8);
					//VR[1].diffuse=VR[0].diffuse;
				};
				if(i>=N-8){
					TransDif(VR[0].diffuse,(N-i-1)*32);
					TransDif(VR[1].diffuse,(N-i-1)*32);
					//VR[0].diffuse=((RD->AFactor*(N-i-1)/8)<<24)+((RD->RFactor*(N-i-1)/8)<<16)+((RD->GFactor*(N-i-1)/8)<<8)+(RD->BFactor*(N-i-1)/8);
					//VR[1].diffuse=VR[0].diffuse;
				};
				//PX=64;
				//PY=0;
				float A=cos(3.1415/6);
				float B=-sin(3.1415/6);

				float PX0=30;//+PX*A+PY*B;
				float PY0=20;//-PX*B+PY*A;
				if(Upos<80){
					PX0*=Upos/80;
					PY0*=Upos/80;
				};

				VR[0].u2=VR[0].u+PX0/3000.0;
				VR[0].v2=VR[0].v-PY0/6000.0;
				VR[1].u2=VR[1].u+PX0/3000.0;
				VR[1].v2=VR[1].v-PY0/6000.0;

				VR[0].w=1.0;
				VR[1].w=1.0;
				VR[0].z=0.0;
				VR[1].z=0.0;

				i2+=2;
				VR+=2;
			};
			RDesc->RoadMesh->setTexture(RD->TextureID);
			RDesc->RoadMesh->setTexture(RD->TextureID,1);
			RDesc->RoadMesh->setNVert((N-1)*2);
			RDesc->RoadMesh->setNInd((N-2)*6);
			RDesc->RoadMesh->setNPri((N-2)*2);
			IRS->Draw(*RDesc->RoadMesh);
			if(RD->BumpTextureID!=-1){
				/*
				int sh=RDesc->RoadMesh->getShader();
				RDesc->RoadMesh->setTexture(RD->BumpTextureID);
				RDesc->RoadMesh->setTexture(RD->BumpTextureID,1);
				RDesc->RoadMesh->setShader(IRS->GetShaderID("RoadBump2"));
				VertexTnL2* VR0=(VertexTnL2*)RDesc->RoadMesh->getVertexData();
				int DX=R->XY[R->Np*2-2]-R->XY[0];
				int DY=R->XY[R->Np*2-1]-R->XY[1];
				int IDX,DIR;
				float angl=atan2(DY,DX*2);
				int idx=int( ((angl+fPI/8)/(fPI/4))+8 )&7;

				int idxS[8]={ 0, 3, 2, 1, 0, 3, 2, 1};
				int dirS[8]={ 1, 1,-1,-1,-1,-1, 1, 1};

				IDX=idxS[idx];
				DIR=dirS[idx];

				for(int i=0;i<N;i++){
					VR0[0].v=IDX*0.25f+0.125-DIR*0.125f;
					VR0[1].v=IDX*0.25f+0.125+DIR*0.125f;
					VR0[0].v2=IDX*0.25f+0.125-DIR*0.125f;
					VR0[1].v2=IDX*0.25f+0.125+DIR*0.125f;
					int A1=0xFF-Lim((VR0[0].diffuse>>24)*2);
					int A2=0xFF-Lim((VR0[1].diffuse>>24)*2);
					VR0[0].diffuse=(A1<<24)+(A1<<16)+(A1<<8)+A1;
					VR0[1].diffuse=(A2<<24)+(A2<<16)+(A2<<8)+A2;
					VR0+=2;					
				};
				IRS->Draw(*RDesc->RoadMesh);
				RDesc->RoadMesh->setShader(sh);
				*/
				int sh=RDesc->RoadMesh->getShader();
				RDesc->RoadMesh->setTexture(RD->BumpTextureID);
				RDesc->RoadMesh->setTexture(RD->BumpTextureID,1);
				RDesc->RoadMesh->setShader(IRS->GetShaderID("RoadBump2"));
				byte AL[4096];
				int MAXA=1;
				for(int i=0;i<N+N;i++){
					AL[i]=VR0[i].diffuse>>24;
					if(AL[i]>MAXA)MAXA=AL[i];
				};
				for(int q=0;q<8;q++){
					VertexTnL2* VR0=(VertexTnL2*)RDesc->RoadMesh->getVertexData();
					bool NDR=0;
					for(int i=0;i<N;i++){
						int DX=VR0[1].x-VR0[0].x;
						int DY=VR0[1].y-VR0[0].y;
						char angl=char(atan2(DY*2,DX)*128.0/fPI);
						//assert(!(DX>50&&DY<0));
						char oc0=q<<5;
						char da=angl-oc0;
						int MPL=32-abs(int(da));
						if(MPL<0)MPL=0;
						MPL<<=5;
						if(MPL>255)MPL=255;
						if(MPL){
							NDR=1;
						};
						int idxS[8]={ 2, 1, 0, 3, 2, 1, 0, 3};
						int dirS[8]={ -1,1, 1, 1 ,1,-1,-1,-1};
						int IDX=idxS[q];
						int DIR=dirS[q];
						VR0[0].v=IDX*0.25f+0.125-DIR*0.125f;
						VR0[1].v=IDX*0.25f+0.125+DIR*0.125f;
						VR0[0].v2=IDX*0.25f+0.125-DIR*0.125f;
						VR0[1].v2=IDX*0.25f+0.125+DIR*0.125f;
						int A10=AL[i+i  ];
						int A20=AL[i+i+1];
						int A1=0xFF-Lim((A10*MPL)/256);//MAXA/2);
						int A2=0xFF-Lim((A20*MPL)/256);//MAXA/2);
						VR0[0].diffuse=(A1<<24)+(A1<<16)+(A1<<8)+A1;
						VR0[1].diffuse=(A2<<24)+(A2<<16)+(A2<<8)+A2;
						//NDR=1;
						//};
						VR0+=2;
					};
					if(NDR){
						IRS->Draw(*RDesc->RoadMesh);
					};
				};
				RDesc->RoadMesh->setShader(sh);
			};
		};
		R++;
	};
};
RoadsSystem ROADSSYS;
void DrawRoads(int sx,int sy,int mx,int my,int lx,int ly,int scale){
	ROADSSYS.Draw(sx,sy,mx,my,lx,ly,scale);
};
void InterpolateCurve();
extern int CurveX[512];
extern int CurveY[512];
extern int NCurves;
void CreateRoad(int t){
	NCurves--;
	InterpolateCurve();
	int R=ROADSSYS.AddRoad(t);
	for(int i=0;i<NCurves-1;i++)ROADSSYS.AddPointToRoad(R,CurveX[i],CurveY[i]);
	ROADSSYS.Roads[R].SurroundWithTexture();
};
void CreateRoadLR(int t,byte Left){
	NCurves--;
	InterpolateCurve();
	int R=ROADSSYS.AddRoad(t);
	OneRoadDesc* RD=ROADSSYS.RDesc->Roads+ROADSSYS.Roads[R].Type;
	for(int i=0;i<NCurves-1;i++){
		ROADSSYS.AddPointToRoad(R,CurveX[i],CurveY[i]);
		int RM=(RD->AFactorMin+RD->AFactor)/2;
		DWORD A=RD->AFactor+((RM-RD->AFactor)*rand())/32768;
		DWORD C=(A<<24)+(((RD->RFactor*A)>>7)<<16)+(((RD->GFactor*A)>>7)<<8)+((RD->BFactor*A)>>7);
		DWORD C1=((A>>2)<<24)+(((RD->RFactor*A)>>9)<<16)+(((RD->GFactor*A)>>9)<<8)+((RD->BFactor*A)>>9);
		if(Left==1){
			ROADSSYS.Roads[R].Weights[i+i]=0;
			ROADSSYS.Roads[R].Weights[i+i+1]=C;
		}else
		if(Left==2){
			ROADSSYS.Roads[R].Weights[i+i+1]=0;
			ROADSSYS.Roads[R].Weights[i+i]=C;
		}else
		if(Left==3){
			ROADSSYS.Roads[R].Weights[i+i+1]=C1;
			ROADSSYS.Roads[R].Weights[i+i]=C1;
		};
	};
	ROADSSYS.Roads[R].SurroundWithTexture();
};
void xLine(int x,int y,int x1,int y1,byte c);
extern int HeightEditMode;
void DrawRoadWithLines(int idx){
	if(HeightEditMode!=13)return;
	if(idx<ROADSSYS.NRoads){
		OneRoad* OR=ROADSSYS.Roads+idx;
		int dx=mapx<<5;
		int dy=mapy<<4;
		int Np=OR->Np;
		int xgp=0;
		int ygp=0;
		for(int j=0;j<Np;j++){
			int xr=OR->XY[j+j];
			int yr=OR->XY[j+j+1];
			yr=(yr>>1)-GetTotalHeight(xr,yr);
			int xg=(xr-dx)>>(5-Shifter);
			int yg=(yr-dy)>>(5-Shifter);
			if(xgp||ygp){
				xLine(xgp,ygp,xg,yg,0xFF);
			};
			xgp=xg;
			ygp=yg;
		};
	};
};
int SearchNearestRoadToPoint(int x,int y,bool Show){
	int yg=(y>>1)-GetTotalHeight(x,y);
	OneRoad* OR=ROADSSYS.Roads;
	int N=ROADSSYS.NRoads;
	for(int i=0;i<N;i++){
		if(x>OR->XMin&&x<OR->XMax&&yg>OR->YMin&&yg<OR->YMax){
			int Np=OR->Np;
			for(int j=0;j<Np;j++){
				int xr=OR->XY[j+j];
				int yr=OR->XY[j+j+1];
				if(Norma(xr-x,yr-y)<OR->Width){
					if(Show)DrawRoadWithLines(i);
					return i;
				};
			};
		};
		OR++;
	};
};
void DeleteCurve(int idx){
	if(idx<ROADSSYS.NRoads){
		OneRoad* OR=ROADSSYS.Roads+idx;
		for(int i=0;i<OR->Np;i++){
			int x=OR->XY[i+i];
			int y=OR->XY[i+i+1];
			int mx=x>>5;
			int my=((y>>1)-GetTotalHeight(x,y))>>4;
			MakeDirtyGBUF(mx-4,my-6,8,12);
		};
		if(OR->XY)free(OR->XY);
		if(idx<ROADSSYS.NRoads-1){
			memcpy(OR,OR+1,(ROADSSYS.NRoads-idx-1)*sizeof *OR);
		};
		ROADSSYS.NRoads--;
	};
};
char stemp[256];
char* GetRoadsNames(){
	stemp[0]=0;
	int N=ROADSSYS.RDesc->NGrps;
	for(int i=0;i<N;i++){
		char cc[32];
		sprintf(cc,"%s",GetTextByID(ROADSSYS.RDesc->Roads[ROADSSYS.RDesc->GrpsElements[i]].RoadName));
		strcat(stemp,cc);
		if(i<N-1)strcat(stemp,"|");
	};
	return stemp;
};
int GetRoadIMG(int i,int& spr,int& grp){
	if(ROADSSYS.RDesc){
		spr=ROADSSYS.RDesc->Roads[i].RoadPreviewSpr;
		grp=ROADSSYS.RDesc->Roads[i].GroupID;
		return ROADSSYS.RDesc->Roads[i].RoadPreviewID;
	}else return 0;
};
int GetNRTypes(){
	if(ROADSSYS.RDesc)return ROADSSYS.RDesc->NRoadsDesc;
	else return 0;
};
void AttractRoad(int idx,int x,int y){
	if(idx<ROADSSYS.NRoads){
		OneRoad* OR=ROADSSYS.Roads+idx;
		int NP=OR->Np;
		for(int i=0;i<NP;i++){
			float xp=OR->XY[i+i];
			float yp=OR->XY[i+i+1];
			float dx=xp-x;
			float dy=yp-y;
			float R=Norma(dx,dy);
			float DL=2.0*exp(-float(R*R)/80.0/80.0);
			if(DL){
				float N=Norma(dx,dy);
				if(N>0){
					float Vx,Vy;
					if(i==0){
						Vx=OR->XY[0]-OR->XY[2];
						Vy=OR->XY[1]-OR->XY[3];
					}else
					if(i==OR->Np-1){
						Vx=OR->XY[i+i-2]-OR->XY[i+i];
						Vy=OR->XY[i+i-1]-OR->XY[i+i+1];
					}else{
						Vx=OR->XY[i+i-2]-OR->XY[i+i+2];
						Vy=OR->XY[i+i-1]-OR->XY[i+i+3];
					};
					int VX=Vy;
					int VY=-Vx;
					if(VX*dx+VY*dy>0){
						VX=-VX;
						VY=-VY;
					};
					
					float Vn=Norma(VX,VY);
					if(Vn>0){
						dx=-VX*DL/Vn;
						dy=-VY*DL/Vn;
						if(fabs(dx)>0.01||fabs(dy)>0.01){
							OR->XY[i+i]+=dx;
							OR->XY[i+i+1]+=dy;
							int mx=xp/32;
							int my=((yp/2)-GetTotalHeight(xp,yp))/16;
							MakeDirtyGBUF(mx-4,my-6,8,12);
						};
					};
				};
			};
		};
		for(i=1;i<NP-1;i++){
			float X=OR->XY[i+i  ];
			float Y=OR->XY[i+i+1];
			float Xp=OR->XY[i+i-2];
			float Yp=OR->XY[i+i-1];
			float Xn=OR->XY[i+i+2];
			float Yn=OR->XY[i+i+3];
			if(Norma(Xn-X,Yn-Y)<4||Norma(Xp-X,Yp-Y)<4){
				OR->XY[i+i  ]=(Xn+Xp)/2.0;
				OR->XY[i+i+1]=(Yn+Yp)/2.0;
			};
		};
		for(i=0;i<NP;i++){
			int X=OR->XY[i+i  ];
			int Y=OR->XY[i+i+1];
			int H=GetTotalHeight(X,Y);
			int YS=(Y>>1)-H;
			if(X<OR->XMin)OR->XMin=X;
			if(YS<OR->YMin)OR->YMin=YS;
			if(X>OR->XMax)OR->XMax=X;
			if(YS>OR->YMax)OR->YMax=YS;
		};
		OR->XMin-=64;
		OR->YMin-=64;
		OR->XMax+=64;
		OR->YMax+=64;
	};
};
void SaveRoadData(ResFile SB){
	return;
	int v='DAOR';
	RBlockWrite(SB,&v,4);
	v=4+ROADSSYS.NRoads;
	int sz=8;
	int N=ROADSSYS.NRoads;
	for(int i=0;i<N;i++){
		sz+=sizeof(ROADSSYS.Roads[i]);
		sz+=ROADSSYS.Roads[i].Np*8;
	};
	RBlockWrite(SB,&sz,4);
	RBlockWrite(SB,&N,4);
	for(i=0;i<N;i++){
		RBlockWrite(SB,&ROADSSYS.Roads[i],sizeof ROADSSYS.Roads[i]);
		RBlockWrite(SB,ROADSSYS.Roads[i].XY,ROADSSYS.Roads[i].Np*8);
	};
};
void ReloadRoads();
void LoadRoadData(ResFile SB){
	ROADSSYS.ClearAll();
	ReloadRoads();
	int N;
	RBlockRead(SB,&N,4);
	ROADSSYS.Roads=new OneRoad[N];
	ROADSSYS.NRoads=N;
	for(int i=0;i<N;i++){
		RBlockRead(SB,&ROADSSYS.Roads[i],sizeof ROADSSYS.Roads[i]);
		ROADSSYS.Roads[i].XY=(float*)malloc(ROADSSYS.Roads[i].Maxp*8);
		RBlockRead(SB,ROADSSYS.Roads[i].XY,ROADSSYS.Roads[i].Np*8);
	};	
};
void ReloadRoads(){
	if(ROADSSYS.RDesc)delete(ROADSSYS.RDesc);
	ROADSSYS.RDesc=NULL;
	ROADSSYS.Init();
};
void ClearAllRoadData(){
	ROADSSYS.ClearAll();
	ReloadRoads();
};
//--------------------------FAST TEXTURE SPRITES---------------------//
void ErrBox(char* sz,...)
{
        char ach[512];
        va_list va;
        va_start( va, sz );
        vsprintf ( ach, sz, va );   
        va_end( va );
		MessageBox(NULL,ach,"Error!",0);
	
};
struct OneGSpriteDesc{
	int TextureID;
	int NDivX;
	int NDivY;
	int CellSize;
	float CenterX;
	float CenterY;
	int R;
	byte Usage;//0-with background texture, 1-direct usage,like sprite,...
	byte ProjType;//0-simple 1-on ground
};
class GSpriteDesc{
public:
	OneGSpriteDesc PDESC[64];
	int NDesc;
	GSpriteDesc(){
		memset(this,0,sizeof *this);
		ReadDesc();
	};
	~GSpriteDesc(){
		memset(this,0,sizeof *this);
	};
	void ReadDesc(){
		GFILE* F=Gopen("Terrain\\GSprites.dat","r");
		if(F){
			int z;
			do{
				char tex[64];
				int type,proj,nx,ny,csz,R;
				float dx,dy;
				z=Gscanf(F,"%s%d%d%d%d%d%f%f%d",tex,&type,&proj,&nx,&ny,&csz,&dx,&dy,&R);
				if(z==9){
					if(NDesc<64){
						PDESC[NDesc].TextureID=IRS->GetTextureID(tex);
						PDESC[NDesc].NDivX=nx;
						PDESC[NDesc].NDivY=ny;
						PDESC[NDesc].CenterX=dx;
						PDESC[NDesc].CenterY=dy;
						PDESC[NDesc].CellSize=csz;
						PDESC[NDesc].R=R;
						PDESC[NDesc].ProjType=proj;
						NDesc++;
					};
				}else if(z!=0){
					ErrBox("Invalid file GSprites.dat.");
					break;
				};
			}while(z==9);
			Gclose(F);
		};
	};
};
GSpriteDesc* GSDESC=NULL;
void EnumGS_Types(char* dest){
	dest[0]=0;
	if(GSDESC){
		for(int i=0;i<GSDESC->NDesc;i++){
			if(dest[0])strcat(dest,"|");
			char cc[64];
			sprintf(cc,"GSPR_%d",i);
			strcat(dest,GetTextByID(cc));
		};
	};
};
struct OneGSprite{

	unsigned x:7;
	unsigned y:6;
	unsigned SType:5;
	unsigned SIndex:6;
	char RotParX;
	char RotParY;
	byte Light;
	byte BaseTex;
	short z;
};
OneGSprite** GSMAP=NULL;
word* NInGSMAP=NULL;

int SREFRLIST[16];
int SNREFR=0;
void AddGSprite(int x,int y,byte SType,byte SIndex);
bool CheckGSprite(int x,int y,int R);
void AddRandomGSP(int x,int y,byte SType,int R){
	if(GSDESC&&SType<GSDESC->NDesc){
		if(R==0)R=1;
		int N=R*R/100;
		if(!N)N=1;
		int R2=R*2;
		int RP2=R*R;
		for(int i=0;i<N;i++){
			int dx=rand()%R2-R;
			int dy=rand()%R2-R;
			if(dx*dx+dy*dy<=RP2){
				if(!CheckGSprite(x+dx,y+dy,GSDESC->PDESC[SType].R)){
					AddGSprite(x+dx,y+dy,SType,rand()%(GSDESC->PDESC[SType].NDivX*GSDESC->PDESC[SType].NDivY));
					int Y=((y+dy)/2-GetHeight(x+dx,y+dy))>>(9-Shifter);
					int X=(x+dx)>>(10-Shifter);
					MakeDirtyGBUF(X-2,Y-2,4,4);
				};
			};
		};
	};
};
void ClearGSInRadius(int x,int y,int R){
	int yr=(y>>1);
	int xc0=x>>7;
	int yc0=yr>>6;
	int yyy0=yr&63;
	int xxx0=x&127;
	int DX=(R>>7)+1;
	for(int dx=-DX;dx<=DX;dx++){
		for(int dy=-DX;dy<=DX;dy++){
			int xc=xc0+dx;
			int yc=yc0+dy;
			if(xc>=0&&xc<VAL_MAXCX-1&&yc>=0&&yc<VAL_MAXCX-1){
				int cell=xc+(yc<<VAL_SHFCX);
				int N=NInGSMAP[cell];
				OneGSprite* GP0=GSMAP[cell];
				int x0=xc<<7;
				int y0=yc<<7;
				for(int i=0;i<N;i++){
					int xx=x0+GP0->x;
					int yy=y0+(int(GP0->y)<<1);
					int r=Norma(xx-x,yy-y);
					if(r<R){
						int Y=(yy/2-GetHeight(xx,yy))>>(9-Shifter);
						int X=xx>>(10-Shifter);
						MakeDirtyGBUF(X-2,Y-2,4,4);
						if(i<N-1)memcpy(GP0,GP0+1,(N-i-1)*sizeof OneGSprite);
						NInGSMAP[cell]--;
						i--;
						N--;
						if(!N){
							free(GSMAP[cell]);
							GSMAP[cell]=NULL;
						};
					}else GP0++;
				};
			};
		};
	};
};
void InitGSMAP(){
	if(!GSDESC)GSDESC=new GSpriteDesc;
	int sz=VAL_MAXCX*VAL_MAXCX;
	GSMAP=(OneGSprite**)realloc(GSMAP,sz<<2);
	memset(GSMAP,0,sz<<2);
	NInGSMAP=(word*)realloc(NInGSMAP,sz<<1);
	memset(NInGSMAP,0,sz<<1);
	/*
	for(int i=0;i<360;i++)
		for(int j=0;j<360;j++){
			int x=600+i*3-(rand()&3);
			int y=600+j*3-(rand()&3);
			AddGSprite(x,y,0,rand()&15);
		};
	*/
};
void ClearAllGS(){
	int sz=VAL_MAXCX*VAL_MAXCX;
	for(int i=0;i<sz;i++)if(GSMAP[i])free(GSMAP[i]);
	memset(GSMAP,0,sz<<2);
	memset(NInGSMAP,0,sz<<1);
};
void SaveGSprites(ResFile F){
	int N=0;
	int sz='RPSG';
	RBlockWrite(F,&sz,4);
	int nn=VAL_MAXCX*VAL_MAXCX;
	sz=8;
	for(int i=0;i<nn;i++)if(NInGSMAP[i]){
		sz+=6;
		sz+=NInGSMAP[i]*sizeof OneGSprite;
		N++;
	};
	RBlockWrite(F,&sz,4);
	RBlockWrite(F,&N,4);
	for(i=0;i<nn;i++)if(NInGSMAP[i]){
		int x=i%VAL_MAXCX;
		int y=i/VAL_MAXCX;
		RBlockWrite(F,&x,2);
		RBlockWrite(F,&y,2);
		RBlockWrite(F,NInGSMAP+i,2);
		RBlockWrite(F,GSMAP[i],int(NInGSMAP[i])*sizeof OneGSprite);
	};
};
void LoadGSprites(ResFile F){
	ClearAllGS();
	int N;
	RBlockRead(F,&N,4);
	for(int i=0;i<N;i++){
		int x=0,y=0;
		RBlockRead(F,&x,2);
		RBlockRead(F,&y,2);
		int p=x+y*VAL_MAXCX;
		RBlockRead(F,NInGSMAP+p,2);
		GSMAP[p]=znew(OneGSprite,NInGSMAP[p]);
		RBlockRead(F,GSMAP[p],int(NInGSMAP[p])*sizeof OneGSprite);
	};
};
void ClearGSMAP(){
	int sz=VAL_MAXCX*VAL_MAXCX;

	memset(GSMAP,0,sz<<2);
	memset(NInGSMAP,0,sz<<1);
};
void freeGSMAP(){
	free(GSMAP);
	free(NInGSMAP);
	GSMAP=NULL;
	NInGSMAP=NULL;
};
int GetLight(int x,int y);
int GetLight2(int x,int y);
void UpdateGSprites(int cx,int cy){
	int cell=cx+(cy<<VAL_SHFCX);
	int NGPP=NInGSMAP[cell];
	OneGSprite* GPP=GSMAP[cell];
	//GSMAP[cell]=NULL;
	//NInGSMAP[cell]=0;
	if(NGPP){
		for(int i=0;i<NGPP;i++){
			int x=(cx<<7)+GPP[i].x;
			int y=(cy<<7)+(GPP[i].y<<1);
			GPP[i].z=GetHeight(x,y);
			GPP[i].Light=GetLighting3DInPoint(x,y);
			//int ys=((cy<<6)+GPP[i].y+GPP[i].z)<<1;
			//AddGSprite((cx<<7)+int(GPP[i].x),ys,GPP[i].SType,GPP[i].SIndex);
		};
		//free(GPP);
	};
};

void UpdateAllGSprites(){
	for(int ix=0;ix<VAL_MAXCX;ix++)
		for(int iy=0;iy<VAL_MAXCX;iy++)
			UpdateGSprites(ix,iy);
};
void MarkSqToRedraw(int sx,int sy);
int GetNearestStoneTex(int x,int y){
	int tx=x>>5;
	int ty=y>>5;
	for(int R=0;R<20;R++){
		char* dx=Rarr[R].xi;
		char* dy=Rarr[R].yi;
		int N=Rarr[R].N;
		for(int i=0;i<N;i++){
			int TX=tx+dx[i];
			int TY=ty+dy[i];
			if(TX>=0&&TY>=0&&TX<VertInLine&&TY<MaxTH){
				int ofs=TX+TY*VertInLine;
				int T=TexMap[ofs];
				if(WTexMapEx[ofs]>127)T=TexMapEx[ofs];
				///if(TexFlags[T]&256*16)
				return T;
			};
		};
	};
	return 8;
};
bool CheckGSprite(int x,int y,int R){
	int yr=(y>>1);
	int xc=x>>7;
	int yc=yr>>6;
	int yyy0=yr&63;
	int xxx0=x&127;
	if(xc>=0&&xc<VAL_MAXCX-1&&yc>=0&&yc<VAL_MAXCX-1){
		int cell=xc+(yc<<VAL_SHFCX);
		int N=NInGSMAP[cell];
		OneGSprite* GP0=GSMAP[cell];
		int L=GetLighting3DInPoint(x,y)*128/80;
		int T=GetNearestStoneTex(x,y);
		int x0=xc<<7;
		int y0=yc<<7;
		for(int i=0;i<N;i++){
			int xx=x0+GP0->x;
			int yy=y0+(int(GP0->y)<<1);
			int r=Norma(xx-x,yy-y);
			if(r<R)return true;
			GP0++;
		};
	};
	return false;
};
void AddGSprite(int x,int y,byte SType,byte SIndex){
	int z=GetHeight(x,y);
	int yr=(y>>1);//-z;
	int xc=x>>7;
	int yc=yr>>6;
	int yyy0=yr&63;
	int xxx0=x&127;
	if(xc>=0&&xc<VAL_MAXCX-1&&yc>=0&&yc<VAL_MAXCX-1){
		int cell=xc+(yc<<VAL_SHFCX);
		int N=NInGSMAP[cell];
		if(N<65535){
			if(!GSMAP[cell]){
				GSMAP[cell]=new OneGSprite;
				NInGSMAP[cell]=0;
			}else GSMAP[cell]=(OneGSprite*)realloc(GSMAP[cell],(N+1)*sizeof(OneGSprite));
			OneGSprite* GP0=GSMAP[cell];
			int L=GetLighting3DInPoint(x,y)*128/90;
			int T=GetNearestStoneTex(x,y);
			OneGSprite* GP;
			for(int i=N-1;i>=0;i--){
				OneGSprite* GP1=GP0+i;
				int yy=GP1->y;
				GP1=GP0+i+1;
				GP1->SType=SType;
				GP1->SIndex=SIndex;
				GP1->x=x&127;
				GP1->y=yr&63;
				GP1->z=z;
				GP1->Light=L;
				GP1->BaseTex=T;
				i=-2;
				GP=GP1;
			};
			if(i==-1){
				GP0->SType=SType;
				GP0->SIndex=SIndex;
				GP0->x=x&127;
				GP0->y=yr&63;
				GP0->z=z;
				GP0->Light=L;
				GP0->BaseTex=T;
				GP=GP0;
			};
			if(GSDESC->PDESC[SType].ProjType==1){
				int DR=GSDESC->PDESC[SType].CellSize;
				int DX=GetHeight(x+DR,y)-GetHeight(x-DR,y);
				int DY=GetHeight(x,y+DR)-GetHeight(x,y-DR);
				if(DX<-120)DX=-120;
				if(DX>120)DX=120;
				if(DY<-120)DY=-120;
				if(DY>120)DY=120;
				GP->RotParX=-DX;
				GP->RotParY=-DY;
			}else{
				GP->RotParX=0;
				GP->RotParY=0;
			};
			NInGSMAP[cell]++;
			int cx=x>>5;
			int cy=yr>>5;
			for(int dx=-1;dx<=1;dx++)
				for(int dy=-1;dy<=1;dy++)
					MarkSqToRedraw(cx+dx,cy+dy);
		};
	};
};
BaseMesh* GSDraw[64]={NULL};
void DrawGSprites(int Target,int x,int y,int gx,int gy,int lx,int ly,int scale){
	if(!GSDraw[0]){
		for(int i=0;i<GSDESC->NDesc;i++){
			GSDraw[i]=new BaseMesh;
			if(GSDESC->PDESC[i].Usage==1){
				GSDraw[i]->create(8192,8192,vfTnL);
				GSDraw[i]->setTexture(GSDESC->PDESC[i].TextureID);
			}else{
				GSDraw[i]->create(8192,8192,vfTnL2);
				GSDraw[i]->setTexture(IRS->GetTextureID("Textures\\GroundTex.bmp"),1);
				GSDraw[i]->setTexture(GSDESC->PDESC[i].TextureID);
				GSDraw[i]->setShader(IRS->GetShaderID("Stones2"));
			};
		};
	};

	//IRS->SetRenderTarget(Target);
	lx<<=scale;
	ly<<=scale;
	int cx0=(x-128)>>7;
	int cy0=(y-160)>>6;
	int cx1=(x+lx+128)>>7;
	int cy1=(y+ly+1024)>>6;
	int wx0=x-32;
	int wx1=x+lx+32;
	int wy0=y-64;
	int wy1=y+ly+64;
	int NInType[32];
	float tq=32/512.0;
	float sc=1.0/float(1<<scale);
	float ysc=0.5;
	for(int ix=cx0;ix<=cx1;ix++){
		for(int iy=cy0;iy<=cy1;iy++){
			if(ix>=0&&ix<VAL_MAXCX-1&&iy>=0&&iy<VAL_MAXCX-1){
				int cell=ix+(iy<<VAL_SHFCX);
				int N=NInGSMAP[cell];
				if(N){
					OneGSprite* OGS=GSMAP[cell];
					int xx0=ix<<7;
					int yy0=iy<<6;
					OGS--;
					for(int j=0;j<N;j++){
						OGS++;
						int xs=xx0+OGS->x;
						int ys=yy0+OGS->y-OGS->z;
						if(xs>wx0&&xs<wx1&&ys>wy0&&ys<wy1){
							int SType=OGS->SType;
							int SIndex=OGS->SIndex;
							BaseMesh* BM=GSDraw[SType];
							OneGSpriteDesc* SD=GSDESC->PDESC+SType;
							int nidx=BM->getNInd();
							if(nidx>8000){
								IRS->Draw(*GSDraw[SType]);
								BM->setNInd(0);
								BM->setNPri(0);
								BM->setNVert(0);
								nidx=0;
							};
							word* idx=BM->getIndices();
							int nv=BM->getNVert();
							idx[nidx  ]=nv;
							idx[nidx+1]=nv+1;
							idx[nidx+2]=nv+2;
							idx[nidx+3]=nv+1;
							idx[nidx+4]=nv+2;
							idx[nidx+5]=nv+3;
							if(GSDESC->PDESC[SType].Usage==0){
								//with backround texture
								VertexTnL2* V=((VertexTnL2*)BM->getVertexData())+nv;
								float fdx=1.0/float(SD->NDivX);
								float fdy=1.0/float(SD->NDivY);
								V[0].u=float(SIndex%SD->NDivX)*fdx;
								V[0].v=float(SIndex/SD->NDivX)*fdy;
								V[1].u=V[0].u+fdx;
								V[1].v=V[0].v;
								V[2].u=V[0].u;
								V[2].v=V[0].v+fdy;
								V[3].u=V[0].u+fdx;
								V[3].v=V[0].v+fdy;
								float x0=gx+((xs-x)>>scale);
								float y0=gy+((ys-y)>>scale);
								float CSZ=sc*SD->CellSize;
								float CSZ0=SD->CellSize;

								if(SD->ProjType==1){
									V[0].x=x0-CSZ;
									V[0].y=y0+(-CSZ0-float(OGS->RotParX)-float(OGS->RotParY))*ysc*sc;
									V[1].x=x0+CSZ;
									V[1].y=y0+(-CSZ0+float(OGS->RotParX)-float(OGS->RotParY))*ysc*sc;
									V[2].x=x0-CSZ;
									V[2].y=y0+(CSZ0-float(OGS->RotParX)+float(OGS->RotParY))*ysc*sc;
									V[3].x=x0+CSZ;
									V[3].y=y0+(CSZ0+float(OGS->RotParX)+float(OGS->RotParY))*ysc*sc;
								}else{
									V[0].x=x0-CSZ;
									V[0].y=y0-CSZ;
									V[1].x=x0+CSZ;
									V[1].y=y0-CSZ;
									V[2].x=x0-CSZ;
									V[2].y=y0+CSZ;
									V[3].x=x0+CSZ;
									V[3].y=y0+CSZ;
								};
								DWORD L=OGS->Light;
								L=GETL(L);
								if(OGS->RotParY<0)L=0;
								V[0].diffuse=L;
								V[1].diffuse=L;
								V[2].diffuse=L;
								V[3].diffuse=L;
								V[0].z=0;
								V[1].z=0;
								V[2].z=0;
								V[3].z=0;
								V[0].w=1.0;
								V[1].w=1.0;
								V[2].w=1.0;
								V[3].w=1.0;

								V[0].u2=GetBaseU(OGS->BaseTex);
								V[0].v2=GetBaseV(OGS->BaseTex);
								V[1].u2=V[0].u2+tq;
								V[1].v2=V[0].v2;
								V[2].u2=V[0].u2;
								V[2].v2=V[0].v2+tq;
								V[3].u2=V[0].u2+tq;
								V[3].v2=V[0].v2+tq;
							}else{
								//
							};
							BM->setNInd(nidx+6);
							BM->setNPri((nidx+6)/3);
							BM->setNVert(nv+4);
						};
					};
				};
			};
		};
	};
	for(int i=0;i<GSDESC->NDesc;i++){
		BaseMesh* BM=GSDraw[i];
		if(BM->getNInd()){
			IRS->Draw(*BM);
			BM->setNVert(0);
			BM->setNInd(0);
			BM->setNPri(0);
		};
	};
};
void MakeDirtySprite(int x,int y){
	int cy=((y>>1)-GetHeight(x,y))>>4;
	int cx=x>>5;
	if(SNREFR>=16)return;
	cy>>=2;
	cx>>=2;
	if(cx>=0&&cy<VAL_MAXCX&&cy>=0&&cy<=VAL_MAXCX){
		int cell=cx+(cy<<VAL_SHFCX);
		for(int i=0;i<SNREFR;i++)if(SREFRLIST[i]==cell)return;
		SREFRLIST[SNREFR]=cell;
		SNREFR++;
	};
};
void UpdateDirtySprites(){
	if(SNREFR==16)UpdateAllGSprites();
	else for(int i=0;i<SNREFR;i++){
		int c=SREFRLIST[i];
		UpdateGSprites(c%VAL_MAXCX,c/VAL_MAXCX);
	};
	SNREFR=0;
};
void RM_SaveGSprites(ResFile F,int x0,int y0,int Lx,int Ly,int xc,int yc){
	int sign='RPSG';
	RBlockWrite(F,&sign,4);
	int ns=0;
	int cx0=(x0>>7);
	int cy0=(y0>>6)-7;
	int cx1=((x0+Lx)>>7)+1;
	int cy1=((y0+Ly)>>6)+7;
	int NS=0;
	for(int ix=cx0;ix<=cx1;ix++){
		for(int iy=cy0;iy<=cy1;iy++){
			if(ix>=0&&ix<VAL_MAXCX&&iy>=0&&iy<VAL_MAXCX){
				int dx=ix<<7;
				int dy=iy<<6;
				int cell=ix+(iy<<VAL_SHFCX);
				int N=NInGSMAP[cell];
				OneGSprite* OG=GSMAP[cell];
				if(OG&&N){
					for(int i=0;i<N;i++){
						int xx=OG->x+dx;
						int yy=dy+OG->y-OG->z;
						if(xx>=x0&&xx<x0+Lx&&yy>=y0&&yy<y0+Ly){
							NS++;
						};
						OG++;
					};
				};
			};
		};
	};
	int i=NS*10+12;
	RBlockWrite(F,&i,4);
	RBlockWrite(F,&NS,4);
	for(ix=cx0;ix<=cx1;ix++){
		for(int iy=cy0;iy<=cy1;iy++){
			if(ix>=0&&ix<VAL_MAXCX&&iy>=0&&iy<VAL_MAXCX){
				int dx=ix<<7;
				int dy=iy<<6;
				int cell=ix+(iy<<VAL_SHFCX);
				int N=NInGSMAP[cell];
				OneGSprite* OG=GSMAP[cell];
				if(OG&&N){
					for(int i=0;i<N;i++){
						int xx=OG->x+dx;
						int yy=dy+OG->y-OG->z;
						if(xx>=x0&&xx<x0+Lx&&yy>=y0&&yy<y0+Ly){
							int p=xx-xc;
							RBlockWrite(F,&p,4);
							p=((dy+OG->y)<<1)-yc;
							RBlockWrite(F,&p,4);
							p=OG->SType;
							RBlockWrite(F,&p,1);
							p=OG->SIndex;
							RBlockWrite(F,&p,1);
						};
						OG++;
					};
				};
			};
		};
	};
};
void RM_LoadGSprites(ResFile F,int xc,int yc){
	int NS;
	RBlockRead(F,&NS,4);
	int maxx=(msx<<5)-32;
	int maxy=(msy<<5)-32;
	for(int i=0;i<NS;i++){
		int x,y;
		byte SType,SIndex;
		RBlockRead(F,&x,4);
		RBlockRead(F,&y,4);
		RBlockRead(F,&SType,1);
		RBlockRead(F,&SIndex,1);
		x+=xc;
		y+=yc;
		AddGSprite(x,y,SType,SIndex);
	};
};
void RM_LoadGSpritesEmpty(ResFile F,int xc,int yc){
	int NS;
	RBlockRead(F,&NS,4);
	int maxx=(msx<<5)-32;
	int maxy=(msy<<5)-32;
	for(int i=0;i<NS;i++){
		int x,y;
		byte SType,SIndex;
		RBlockRead(F,&x,4);
		RBlockRead(F,&y,4);
		RBlockRead(F,&SType,1);
		RBlockRead(F,&SIndex,1);
		x+=xc;
		y+=yc;
		AddGSprite(x,y,SType,SIndex);
	};
};
void FAST_RM_LoadGSprites(SaveBuf*SB,int xc,int yc){
	int NS;
	xBlockRead(SB,&NS,4);
	int maxx=(msx<<5)-32;
	int maxy=(msy<<5)-32;
	for(int i=0;i<NS;i++){
		int x,y;
		byte SType,SIndex;
		xBlockRead(SB,&x,4);
		xBlockRead(SB,&y,4);
		xBlockRead(SB,&SType,1);
		xBlockRead(SB,&SIndex,1);
		x+=xc;
		y+=yc;
		int z=GetHeight(x,y);
		if(z>1024)AddGSprite(x,y,SType,SIndex);
	};
};
void OneRoad::SurroundWithTexture(){
	extern bool InLoadRoadsNet;
	if(InLoadRoadsNet)return;
	if(ROADSSYS.RDesc){
		OneRoadDesc* RD=ROADSSYS.RDesc->Roads+Type;
		if(RD->MapTextureID>=0){
			int XMIN=10000000;
			int YMIN=10000000;
			int XMAX=-10000000;
			int YMAX=-10000000;
			for(int i=0;i<Np;i++){
				int x=XY[i+i];
				int y=XY[i+i+1];
				if(x<XMIN)XMIN=x;
				if(x>XMAX)XMAX=x;
				if(y<YMIN)YMIN=y;
				if(y>YMAX)YMAX=y;
			};
			if(XMIN<XMAX){
				int VX0=(XMIN-RD->TexRMax-64)>>5;
				int VX1=(XMAX+RD->TexRMax+64)>>5;
				int VY0=(YMIN-RD->TexRMax-64)>>5;
				int VY1=(YMAX+RD->TexRMax+64)>>5;
				for(int vx=VX0;vx<=VX1;vx++){
					for(int vy=VY0;vy<=VY1;vy++){
						int x=vx<<5;
						int y=vy<<5;
						if(vx&1)y-=16;
						int p=0;
						int R=GetDistance(x,y,1,p);
						int V=vx+vy*VertInLine;
						int r0=RD->TexRMin;//+((RD->TexRMax-RD->TexRMin)*(RDEP[p%NRDEP]-16))/16;
						int drmax=(r0*RD->RWidth)/200;
						int W=127-((R-r0)*128)/drmax;
						if(W>0){
							if(W>255)W=255;
							void AssignExTex2(int V,byte tex1,byte tex2,int W);
							AssignExTex2(V,0,RD->MapTextureID,W);
							if(W>10){
								FactureMap[V]=RD->FactureID;
								FactureWeight[V]=W;
							};
						};
					};
				};
			};
		};
	};
};
//-----------------------ROADS SYSTEM--------------------//
#define MaxLink 8
word GetDir(int,int);
typedef bool tpUnitsCallback(OneObject* OB,void* param);
CEXPORT int PerformActionOverUnitsInRadius(int xc,int yc,int R,tpUnitsCallback* CB,void* Param);
extern int tmtmt;
struct Dippol
{
	int x,y;
	byte dir;
};
struct OneNetKnot{
	int x,y;
	int NLinks;
	byte Hidden;
	word Links[MaxLink];
	word RoadID[MaxLink];
	byte LinkType[MaxLink];
	word WayPointToPointIndex[MaxLink];
	int xOnRoad, yOnRoad;
};
extern int AnimTime;
//================================================================================================================//
//  ласс экземпл€ры которого содержат информацыю о движенни по дорожным линкам 
//от StartKnot(индекс дорожной топзоны) до EndKnot 
class OneNetWayPointToPoint
{
public:
	OneNetWayPointToPoint();
	OneNetWayPointToPoint(int StartKnot, int EndKnot, OneNetKnot* Net);
	~OneNetWayPointToPoint();
	int StartKnot;
	int EndKnot;
	static int Step;

	//int NP;
	int Pi;
	int PiMax;
	Dippol* P;// промежуточные точки по которым происходит движение отр€дов

	bool FillWay(int StartKnot, int EndKnot, OneNetKnot* Net);//вычислени€ промежуточных точек
	int GetNextP(int FromIndex, int DestKnot, int &x, int &y);
	void ShowWayPoints();
	void Calk2P(int x1,int y1,int x2,int y2,int n,int p,int &nx, int &ny);//вычислени€ промежуточных точек
	void Calk3P(int x1,int y1,int x2,int y2,int x3,int y3,int n,int p,int &nx, int &ny);//вычислени€ промежуточных точек
	void AddNextPoint(int xx, int yy);//вычислени€ промежуточных точек
	int GetWayPoints(int From, int To, int* PointsXYList);
	int GetWayPoints(int From, int To, int* PointsXYList, word &Flag, DWORD Requestor);//ѕолучить массив(PointsXYList)
	// дл€ движени€ от From до To
	// Flag - положение на дороге 0 - ќ , 1 - ждать, 2 - обходим встречный отр€д
	int GetWayPointsNear(int From, int To, int* PointsXYList);
	int GetWayPointsNear(int From, int To, int* PointsXYList, byte Turn);
	DWORD Owner;// кем занет этот участок дороги
	bool OwnerDirection; // и в каком направлениии
	bool IsOwnerOnRoad();
	static bool IsOwnerOnRoadOB(OneObject* OB,void* param);
	int LockTime;
	word PreLock;//этот участок забронирован
	int PreLockTime;
	DWORD PreLockRequestor;// кто забронировал
	int SetPreLock();
	int SetPreLock(DWORD Requestor);// забронировать
	int GetPreLock();
	void Save(SaveBuf* SB);
	void Load(SaveBuf* SB);
	//
};
int OneNetWayPointToPoint::Step=26;

OneNetWayPointToPoint::OneNetWayPointToPoint()
{
	P=NULL;
}
OneNetWayPointToPoint::OneNetWayPointToPoint(int StartK, int EndK, OneNetKnot* Net)
{
	FillWay(StartK, EndK, Net);
	Owner=0xFFFFFFFF;
	LockTime=0;
}
bool OneNetWayPointToPoint::FillWay(int StartK, int EndK, OneNetKnot* Net)
{
	OneNetKnot* St=Net+StartK;
	OneNetKnot* En=Net+EndK;
	StartKnot=StartK; 
	EndKnot=EndK;
	int dst=Norma(St->xOnRoad-En->xOnRoad,St->yOnRoad-En->yOnRoad);
	int PMaxSize=dst/Step;
	//NP=PMaxSize;
	PiMax=PMaxSize+100;
	P = new Dippol[PiMax];
	//int mdNP=NP/2;
	Pi=0;
	int xx, yy;
	int mpx=(St->x+En->x)/2;
	int mpy=(St->y+En->y)/2;
	int p;
	int dst1=Norma(St->xOnRoad-mpx,St->yOnRoad-mpy);
	int dst2=Norma(En->xOnRoad-mpx,En->yOnRoad-mpy);
	//0.0-0.5
	//P[0].x=St->xOnRoad;
	//P[0].y=St->yOnRoad;
	if(St->NLinks==2)
	{
		int prx, pry;
		if(St->Links[0]==EndKnot)
		{
			prx=Net[St->Links[1]].x;
			pry=Net[St->Links[1]].y;
		}
		else
		{
			prx=Net[St->Links[0]].x;
			pry=Net[St->Links[0]].y;
		}
		int mpprx=(St->x+prx)/2;
		int mppry=(St->y+pry)/2;
		int dst1pr=Norma(mpx-mpprx,mpy-mppry);
		if(dst1pr==0)dst1pr=1;
		int Npp=dst1pr/Step;
		int sP=(Npp*dst1)/dst1pr;
		for(p=sP;p<Npp;p++)
		{
			Calk3P(mpprx,mppry,St->xOnRoad,St->yOnRoad,mpx,mpy,Npp,p,xx,yy);
			AddNextPoint(xx, yy);
		}
	}
	else
	{
		//line
		int Npp=dst1/Step;
		for(p=0;p<Npp;p++)
		{
			Calk2P(St->xOnRoad,St->yOnRoad,mpx,mpy,Npp,p,xx,yy);
			AddNextPoint(xx, yy);
		}
	}
	//
	AddNextPoint(mpx, mpy);
	//0.5-1.0
	if(En->NLinks==2)
	{
		int nnx, nny;
		if(En->Links[0]==StartKnot)
		{
			nnx=Net[En->Links[1]].x;
			nny=Net[En->Links[1]].y;
		}
		else
		{
			nnx=Net[En->Links[0]].x;
			nny=Net[En->Links[0]].y;
		}
		int mpprx=(En->x+nnx)/2;
		int mppry=(En->y+nny)/2;
		int dst2pr=Norma(mpx-mpprx,mpy-mppry);
		if(!dst2pr)dst2pr=1;
		int Npp=dst2pr/Step;
		int sP=(Npp*dst2)/dst2pr;
		for(p=1;p<(sP+1);p++)
		{
			Calk3P(mpx,mpy,En->xOnRoad,En->yOnRoad,mpprx,mppry,Npp,p,xx,yy);
			AddNextPoint(xx, yy);
		}
	}
	else
	{
		//line
		int Npp=dst2/Step;
		for(p=1;p<(Npp+1);p++)
		{
			Calk2P(mpx,mpy,En->xOnRoad,En->yOnRoad,Npp,p,xx,yy);
			AddNextPoint(xx, yy);
		}

	}
	for(int i=0;i<(Pi-2);i++)
	{
		P[i].dir=GetDir(P[i+2].x-P[i].x,P[i+2].y-P[i].y);
	}
	P[Pi-2].dir=P[i-1].dir;
	P[Pi-1].dir=P[i-1].dir;
	//NP=Pi;
	return true;
}
void OneNetWayPointToPoint::Calk2P(int x1,int y1,int x2,int y2,int n,int p,int &nx, int &ny)
{
	//nx=(x1*p+x2*(n-p))/n;
	//ny=(y1*p+y2*(n-p))/n;
	nx=(x1*(n-p)+x2*(p))/n;
	ny=(y1*(n-p)+y2*(p))/n;
}
void OneNetWayPointToPoint::Calk3P(int x1,int y1,int x2,int y2,int x3,int y3,int n,int p,int &nx, int &ny)
{
	//nx=(x1*p+x3*(n-p))/n+((4*x2-2*(x1+x3))*p*(n-p))/(n*n);
	//ny=(y1*p+y3*(n-p))/n+((4*y2-2*(y1+y3))*p*(n-p))/(n*n);
	nx=(x1*(n-p)+x3*(p))/n+((4*x2-2*(x1+x3))*p*(n-p))/(n*n);
	ny=(y1*(n-p)+y3*(p))/n+((4*y2-2*(y1+y3))*p*(n-p))/(n*n);
} 
OneNetWayPointToPoint::~OneNetWayPointToPoint()
{
	if(P)delete[](P);
}
int ABRAKADABRA=0;
void OneNetWayPointToPoint::AddNextPoint(int xx, int yy)
{
	ABRAKADABRA=1;
	if(Pi<PiMax&&Pi>0&&Norma(P[Pi-1].x-xx,P[Pi-1].x-yy)>Step) 
	{
		P[Pi].x=xx;
		P[Pi].y=yy;
		Pi++;
	}
	ABRAKADABRA=2;
	if(Pi==0)
	{
		P[Pi].x=xx;
		P[Pi].y=yy;
		Pi++;
	}
	ABRAKADABRA=3;
	ABRAKADABRA=0;
}
int OneNetWayPointToPoint::GetWayPoints(int From, int To, int* PointsXYList)
{
	int rez=0;
	if(From==StartKnot&&To==EndKnot)
	{
		for(int i=0;i<(Pi-1);i++)
		{
			if(PointsXYList)
			{
				PointsXYList[i*2]=P[i+1].x;
				PointsXYList[i*2+1]=P[i+1].y;
			}
			rez++;
		}
	}
	if(From==EndKnot&&To==StartKnot)
	{
		for(int i=1;i<Pi;i++)
		{
			if(PointsXYList)
			{
				PointsXYList[(i-1)*2]  =P[Pi-i-1].x;
				PointsXYList[(i-1)*2+1]=P[Pi-i-1].y;
			}
			rez++;
		}
	}
	return rez;
}
int OneNetWayPointToPoint::GetWayPointsNear(int From, int To, int* PointsXYList)
{
	int rez=0;
	int Shift=120; 
	if(PointsXYList)
	{
		if(From==StartKnot&&To==EndKnot)
		{
			for(int i=0;i<(Pi-1);i++)
			{
				byte ddr=P[i+1].dir;
				int xx=P[i+1].x-((Shift*TSin[ddr])>>8);
				int yy=P[i+1].y+((Shift*TCos[ddr])>>8);
				if(Norma(PointsXYList[(rez-1)*2]-xx, PointsXYList[(rez-1)*2+1]-yy)>Step)  
				{
					PointsXYList[rez*2]  =xx;
					PointsXYList[rez*2+1]=yy;
					rez++;
				}
			}
		}
		if(From==EndKnot&&To==StartKnot)
		{
			for(int i=1;i<Pi;i++)
			{ 
				byte ddr=P[Pi-i-1].dir+128;
				int xx=P[Pi-i-1].x-((Shift*TSin[ddr])>>8);
				int yy=P[Pi-i-1].y+((Shift*TCos[ddr])>>8);
				if(Norma(PointsXYList[(rez-1)*2]-xx, PointsXYList[(rez-1)*2+1]-yy)>Step) 
				{
					PointsXYList[rez*2]  =xx;
					PointsXYList[rez*2+1]=yy;
					rez++;
				}
			}
		}

	}
	return rez;
}
int OneNetWayPointToPoint::GetWayPointsNear(int From, int To, int* PointsXYList, byte Turn)
{
	int rez=0;
	int Shift=90;  
	if(PointsXYList)
	{
		int BShift;
		int ShiftStep=Shift/Pi;
		if(Turn==0)
		{
			BShift=Shift;  
			ShiftStep=0; 
		}
		else
		if(Turn==1) 
		{
			BShift=ShiftStep;
			//ShiftStep;
		}
		else
		if(Turn==2)
		{
			BShift=Shift-ShiftStep;  
			ShiftStep=-ShiftStep;
		}
		if(From==StartKnot&&To==EndKnot)
		{
			for(int i=0;i<(Pi-1);i++)
			{
				byte ddr=P[i+1].dir;
				int xx=P[i+1].x-(((BShift+ShiftStep*(i+1))*TSin[ddr])>>8);
				int yy=P[i+1].y+(((BShift+ShiftStep*(i+1))*TCos[ddr])>>8);
				if(Norma(PointsXYList[(rez-1)*2]-xx, PointsXYList[(rez-1)*2+1]-yy)>Step)  
				{
					PointsXYList[rez*2]  =xx;
					PointsXYList[rez*2+1]=yy;
					rez++;
				}
			}
		}
		if(From==EndKnot&&To==StartKnot)
		{
			for(int i=1;i<Pi;i++)
			{ 
				byte ddr=P[Pi-i-1].dir+128;
				int xx=P[Pi-i-1].x-(((BShift+ShiftStep*i)*TSin[ddr])>>8);
				int yy=P[Pi-i-1].y+(((BShift+ShiftStep*i)*TCos[ddr])>>8);
				if(Norma(PointsXYList[(rez-1)*2]-xx, PointsXYList[(rez-1)*2+1]-yy)>Step) 
				{
					PointsXYList[rez*2]  =xx;
					PointsXYList[rez*2+1]=yy;
					rez++;
				}
			}
		}

	}
	return rez;
}
int OneNetWayPointToPoint::GetWayPoints(int From, int To, int* PointsXYList, word &Flag, DWORD Requestor)
{
	bool AscDir;
	if(From==StartKnot&&To==EndKnot)
		AscDir=true;
	else
		AscDir=false;

	
	if(Owner==0xFFFFFFFF&&GetPreLock()==0) 
	{
		Owner=Requestor;
		return GetWayPoints( From, To, PointsXYList);
	}
	else
	if(GetPreLock()>1&&PreLockRequestor!=Requestor)
	{
		int Turn=0;
		if(Flag==0)
			Turn=1;
		Flag=2;
		if(OwnerDirection==AscDir)
		{
			Flag=1;//Wait
			return 0;
		}
		return GetWayPointsNear( From, To, PointsXYList, Turn);
	}
	else
	if(Requestor==Owner||(!IsOwnerOnRoad()))
	{
		Owner=Requestor;
		OwnerDirection=AscDir;
		LockTime=AnimTime;
		if(Flag==2)
		{
			Flag=0;
			return GetWayPointsNear( From, To, PointsXYList, 2);
		}
		Flag=0;//Free way
		return GetWayPoints( From, To, PointsXYList);
	}
	else
	{
		if(OwnerDirection==AscDir)
		{
			word Nat1=Owner>>16;
			word ID1=Owner&0xFFFF;
			word Nat2=Requestor>>16;
			word ID2=Requestor&0xFFFF;

			if(Nat1!=0xFFFF&&ID1!=0xFFFF)
			{
				Brigade* Br1=&CITY[Nat1].Brigs[ID1];
				Brigade* Br2=&CITY[Nat2].Brigs[ID2];
				if(Br1&&Br2&&Br1->WarType>0&&Br2->WarType>0)
				{
					if(Br1->Memb[3]!=0xFFFF&&Br2->Memb[3]!=0xFFFF)
					{
						OneObject* OB1=Group[Br1->Memb[3]];
						OneObject* OB2=Group[Br2->Memb[3]];
						if(OB1&&OB2)
						{
							if(OB2->newMons->MotionDist>OB1->newMons->MotionDist)
							{
								//Obgon
								int Turn=0;
								if(Flag==0)
									Turn=1;
								Flag=2;
								return GetWayPointsNear( From, To, PointsXYList, Turn);
							}
						}
					}
				}
			}
			Flag=1;//Wait
			return 0;
		}
		else
		{
			//coming from the opposite
			int Turn=0;
			if(Flag==0)
				Turn=1;
			Flag=2;
			return GetWayPointsNear( From, To, PointsXYList, Turn);
		}
	}
}
bool OneNetWayPointToPoint::IsOwnerOnRoad()
{
	word Nat=Owner>>16;
	word ID=Owner&0xFFFF;
	
	if((LockTime+100*256)>AnimTime)
		return true;
  	//if((LockTime+430*256)<AnimTime)
	//	return false;
	if(Nat==0xFFFF)
	{
		if(ID!=0xFFFF)
		{
			//find unit
			word pr[3]={0xFFFF,0xFFFF,ID};
			int xf=P[1].x;
			int yf=P[1].y;
			int nu=PerformActionOverUnitsInRadius(xf,yf,30,&OneNetWayPointToPoint::IsOwnerOnRoadOB,pr);
			xf=P[Pi-2].x;
			yf=P[Pi-2].y;
			nu+=PerformActionOverUnitsInRadius(xf,yf,30,&OneNetWayPointToPoint::IsOwnerOnRoadOB,pr);
			return nu>0;
		}
	}
	else
	{
		if(ID!=0xFFFF)
		{
			//find brigade
			word pr[3]={Nat,ID,0xFFFF};
			int xf=P[1].x;
			int yf=P[1].y;
			int nu=PerformActionOverUnitsInRadius(xf,yf,50,&OneNetWayPointToPoint::IsOwnerOnRoadOB,pr); 
			xf=P[Pi-2].x;
			yf=P[Pi-2].y;
			nu+=PerformActionOverUnitsInRadius(xf,yf,50,&OneNetWayPointToPoint::IsOwnerOnRoadOB,pr);
			return nu>0; 
		}
	}
	Owner=0xFFFFFFFF;
	return false;
}
bool OneNetWayPointToPoint::IsOwnerOnRoadOB(OneObject *OB, void *param)
{
	word* par=(word*)param;
	if(OB->Index==par[2])
		return true;
	if(OB->BrigadeID==par[1]&&OB->NNUM==par[0]){
        Brigade* BR=CITY[OB->NNUM].Brigs+OB->BrigadeID;
		return BR->NewBOrder&&BR->NewBOrder->GetBrigadeOrderID()==BRIGADEORDER_GOONROAD;		
	}
	return false;
}
//
void DrawPlaneLine(int x0,int y0,int x1,int y1,byte c);
void TransformToScreen(int x,int y,int z,float& scrx,float& scry,float& Scale);
void OneNetWayPointToPoint::ShowWayPoints()
{
	byte cl=clrYello;
	float fx,fy,Scale;
	for(int i=0;i<Pi;i++)
	{
		//TransformToScreen(P[i].x,P[i].y,0,fx,fy,Scale);
		DrawPlaneLine(P[i].x,P[i].y-2,P[i].x,P[i].y+2,cl);
		DrawPlaneLine(P[i].x-2,P[i].y,P[i].x,P[i].y,cl);
	}
}
int OneNetWayPointToPoint::SetPreLock()
{
	GetPreLock();
	PreLock++;
	PreLockTime=AnimTime;
	return PreLock;
}
int OneNetWayPointToPoint::SetPreLock(DWORD Requestor)
{
	GetPreLock();
	PreLock++;
	PreLockTime=AnimTime;
	PreLockRequestor=Requestor;
	return PreLock;
}
int OneNetWayPointToPoint::GetPreLock()
{
	if(PreLock)
		if((PreLockTime+64*256)<AnimTime) 
			PreLock=0;
	return PreLock;
}
void OneNetWayPointToPoint::Save(SaveBuf *SB)
{
	xBlockWrite(SB,&Owner,4);
	xBlockWrite(SB,&OwnerDirection,1);
	xBlockWrite(SB,&LockTime,4);
	xBlockWrite(SB,&PreLock,2);
	xBlockWrite(SB,&PreLockTime,4);
}
void OneNetWayPointToPoint::Load(SaveBuf *SB)
{
	xBlockRead(SB,&Owner,4);
	xBlockRead(SB,&OwnerDirection,1);
	xBlockRead(SB,&LockTime,4);
	xBlockRead(SB,&PreLock,2);
	xBlockRead(SB,&PreLockTime,4);
}
typedef tpDrawOnMapCallback();
tpDrawOnMapCallback* WayPointToPointCB=NULL;
tpDrawOnMapCallback* SetDrawOnMapCallback(tpDrawOnMapCallback* dc);
bool IsWayPointToPointCBSet=false;
int ShowWayPointToPointCB();
//================================================================================================================//
struct QSortAngl{
	byte RType;
	short Idx;
	byte Angle;
};
int AngCmp(const void* A1,const void* A2){
	return ((QSortAngl*)A1)->Angle<((QSortAngl*)A2)->Angle;
};
word GetDir(int,int);
struct NetParams{
	short Speed;
	short Tiring;
	short Priory;
};
class RoadsNet{
public:
	OneNetKnot* Net;
	NetParams*  NParams;
	OneNetWayPointToPoint** WayPointToPoint;// ¬се пути по дорогам
	int MaxWayPointToPointIndex;

	void FillWayPointToPoint();
	void ClearWayPointToPoint();
	void ShowWayPointToPoint();
	int GetNextWayPoints(int From, int Dest, int* PointsXYList);
	int GetNextWayPoints(int From, int Dest, int* PointsXYList, word &Flag, DWORD Requestor);
	int SetPreLock(int From, int Dest);
	int SetPreLock(int From, int Dest, DWORD Requestor);
	void SaveWayPointToPoint(SaveBuf *SB);
	void LoadWayPointToPoint(SaveBuf *SB);
	

	void SetupNetParams(){
		if(!NParams){
			NParams=new NetParams[NKnots];
			OneNetKnot* NK=Net;
			for(int i=0;i<NKnots;i++){
				int SS=0;
				int ST=0;
				int SP=0;
				for(int j=0;j<NK->NLinks;j++){
					OneRoadDesc* RD=ROADSSYS.RDesc->Roads+NK->LinkType[j];
					SS+=RD->Speed;					
					ST+=RD->Tiring;
					SP+=RD->Priory;
				};
				if(NK->NLinks){
					SS/=NK->NLinks;
					ST/=NK->NLinks;
					SP/=NK->NLinks;
				}else{
					SS=256;
					ST=256;
					SP=256;
				};
				NParams[i].Speed =SS;
				NParams[i].Tiring=ST;
				NParams[i].Priory=SP;
				NK++;
			};
		};
	};

	int NKnots;
	int MaxKnot;

	RoadsNet(){
		memset(this,0,sizeof *this);
		WayPointToPoint=NULL;
		MaxWayPointToPointIndex=0;
	};
	~RoadsNet(){
		ClearWayPointToPoint();
		if(Net)free(Net);
	};
	void ClearAll(){
		ClearWayPointToPoint();
		if(Net)free(Net);
		memset(this,0,sizeof *this);
		WayPointToPoint=NULL;
		MaxWayPointToPointIndex=0;
	}
	int AddKnot(int x,int y){
		if(NKnots>=MaxKnot){
			MaxKnot+=512;
			Net=(OneNetKnot*)realloc(Net,MaxKnot*sizeof OneNetKnot);
		};
		memset(Net+NKnots,0,sizeof OneNetKnot);
		Net[NKnots].x=x;
		Net[NKnots].y=y;
		Net[NKnots].Hidden=0;
		return NKnots++;
	};
	void TestKnots(){
		for(int i=0;i<NKnots;i++)/*if(Net[i].Hidden)*/{
			int NVIS=0;
			int NL=Net[i].NLinks;
			for(int j=0;j<NL;j++){
				int KID=Net[i].Links[j];
				if(!Net[KID].Hidden)NVIS++;
			};			
			if(!NVIS){
				DeleteKnot(i);
				ROADSSYS.ClearRoads();
				CreateNetSystem();
				MakeAllDirtyGBUF();
				return;
			};
		};
	};
	void DeleteKnot(int idx){
		if(Net[idx].Hidden){
			for(int i=0;i<NKnots;i++)if(i!=idx){
				int NL=Net[i].NLinks;
				int N=0;
				for(int j=0;j<NL;j++){
					int KID=Net[i].Links[j];
					if(KID!=idx){
						if(KID>idx){
							Net[i].Links[N]=KID-1;
						}else{
							Net[i].Links[N]=KID;
						};
						Net[i].RoadID[N]=Net[i].RoadID[j];
						Net[i].LinkType[N]=Net[i].LinkType[j];
						N++;
					};
				};
				Net[i].NLinks=N;
			};
			if(idx<NKnots-1){
				memcpy(Net+idx,Net+idx+1,(NKnots-idx-1)*sizeof OneNetKnot);
			};
			NKnots--;
		}else{
			Net[idx].Hidden=1;
		};
	};
	int GetNearestKnot(int &x,int &y,int r){
		int RM=1000000;
		int idx=-1;
		for(int i=0;i<NKnots;i++){
			int R=Norma(Net[i].x-x,Net[i].y-y);
			if(R<r&&RM>R){
				RM=R;
				idx=i;
			};
		};
		if(idx!=-1){
			x=Net[idx].x;
			y=Net[idx].y;
		};
		return idx;
	};
	void AddLink(int Start,int Fin,byte Type){
		ROADSSYS.Init();
		if(Start>=0&&Start<NKnots&&Fin>=0&&Fin<NKnots){
			if(Net[Start].NLinks<MaxLink){
				int NL=Net[Start].NLinks;
				Net[Start].Links[NL]=Fin;
				Net[Start].LinkType[NL]=Type;
				Net[Start].RoadID[NL]=0xFFFF;
				Net[Start].NLinks++;
				Net[Start].Hidden=0;
				NL=Net[Fin].NLinks;
				Net[Fin].Links[NL]=Start;
				Net[Fin].LinkType[NL]=Type;
				Net[Fin].RoadID[NL]=0xFFFF;
				Net[Fin].NLinks++;
				Net[Fin].Hidden=0;
			};
		};
	};
	void CreateNetSystem(){
		ROADSSYS.Init();
		for(int i=0;i<NKnots;i++)if(!Net[i].Hidden){
			int NL=Net[i].NLinks;
			QSortAngl QS[8];
			int x0=Net[i].x;
			int y0=Net[i].y;
			for(int j=0;j<NL;j++){
				int F=Net[i].Links[j];
				if(F>i){
					int DX=Net[i].x-Net[F].x;
					int DY=Net[i].y-Net[F].y;
					int NN=Norma(DX,DY);
					if(NN==0)NN=1;
					int W=ROADSSYS.RDesc->Roads[Net[i].LinkType[j]].RWidth/2;
					DX=DX*W/NN;
					DY=DY*W/NN;
					NCurves=3;
					CurveX[0]=Net[i].x-DX;
					CurveY[0]=Net[i].y-DY;
					CurveX[1]=Net[F].x+DX;
					CurveY[1]=Net[F].y+DY;
					CreateRoad(Net[i].LinkType[j]);
					Net[i].RoadID[j]=ROADSSYS.NRoads-1;
					for(int q=0;q<Net[F].NLinks;q++)if(Net[F].Links[q]==i)Net[F].RoadID[q]=ROADSSYS.NRoads-1;
					NCurves=0;
				};
				QS[j].Idx=F;
				QS[j].Angle=GetDir(Net[F].x-x0,(Net[F].y-y0)/2);
				QS[j].RType=Net[i].LinkType[j];
			};
			if(NL>1){
				for(int p=0;p<NL;p++){
					int RMinDA=1024;
					int LMinDA=-1024;
					for(int j=0;j<NL;j++)if(p!=j){
						char DA=QS[j].Angle-QS[p].Angle;
						int DAL=DA<0?DA:-256+DA;
						int DAR=DA>0?DA:256+DA;
						if(DAR<RMinDA)RMinDA=DAR;
						if(DAL>LMinDA)LMinDA=DAL;
					};
					RMinDA=char(RMinDA);
					LMinDA=char(LMinDA);
					for(j=0;j<NL;j++){
						int W1=ROADSSYS.RDesc->Roads[QS[j].RType].RWidth;
						int W2=ROADSSYS.RDesc->Roads[QS[p].RType].RWidth;
						if((W1==W2&&p>j)||W1<W2){
						//if(p>j){
							char DA=QS[j].Angle-QS[p].Angle;
							{
								int x=x0;
								int y=y0;
								int x1,y1,x2,y2;
								char A1,A2;
								x1=Net[QS[j].Idx].x;
								y1=Net[QS[j].Idx].y;
								x2=Net[QS[p].Idx].x;
								y2=Net[QS[p].Idx].y;
								NCurves=6;
								int W=ROADSSYS.RDesc->Roads[QS[j].RType].RWidth*2;
								int DD=20;
								if(abs(DA)<64)DD=20+64-abs(DA);
								if(DD>40)DD=40;
								if(abs(DA)<64)W=(W*DD)/20;
								int N1=Norma(x1-x,y1-y);
								if(N1==0)N1=1;
								int N2=Norma(x2-x,y2-y);
								if(N2==0)N2=1;
								if(W>N1)W=N1;
								if(W>N2)W=N2;
								x1=x+((x1-x)*W)/N1;
								y1=y+((y1-y)*W)/N1;
								x2=x+((x2-x)*W)/N2;
								y2=y+((y2-y)*W)/N2;
								CurveX[0]=x1;
								CurveY[0]=y1;
								CurveX[1]=(x1+x)/2;
								CurveY[1]=(y1+y)/2;
								CurveX[2]=(x1+x2+x+x+x+x)/6;
								CurveY[2]=(y1+y2+y+y+y+y)/6;
								CurveX[3]=(x2+x)/2;
								CurveY[3]=(y2+y)/2;
								CurveX[4]=x2;
								CurveY[4]=y2;
								//if(abs(int(DA))>96)CreateRoad(Net[i].LinkType[j]);
								//else if(DA>0)CreateRoadLR(Net[i].LinkType[j],1);
								//else CreateRoadLR(Net[i].LinkType[j],1);
								int NRD=2;
								for(int t=0;t<NL;t++)if(t!=p&&t!=j){
									int W3=ROADSSYS.RDesc->Roads[QS[t].RType].RWidth;
									if(abs(W1-W3)<W1/3||abs(W2-W3)<W2/3)NRD++;
								};
								if(NRD<=2){
									CreateRoad(Net[i].LinkType[j]);	
								}else{
									if(abs(DA)>32){
										if(DA==RMinDA){
											CreateRoadLR(Net[i].LinkType[j],1);
										}else
										if(DA==LMinDA){
											CreateRoadLR(Net[i].LinkType[j],2);
										}else{
											CreateRoadLR(Net[i].LinkType[j],3);
										};
									};
								};
								NCurves=0;
							};						
						};
					};
				};
			};
		};
		if(NParams)delete[](NParams);
		NParams=NULL;
		SetupNetParams();
		FillWayPointToPoint();
	};
};
void RoadsNet::ClearWayPointToPoint()
{
	if(WayPointToPoint)
	{
		for(int i=0;i<MaxWayPointToPointIndex;i++)
		{
			if(WayPointToPoint[i])
				delete (WayPointToPoint[i]);
		}
		free(WayPointToPoint);
		WayPointToPoint=NULL;
	}
	MaxWayPointToPointIndex=0;
}
void RoadsNet::FillWayPointToPoint()
{
	for(int i=0;i<NKnots;i++)
	{
		OneNetKnot* NK=&(Net[i]); 
		if(NK)
		{
			if(NK->NLinks==2)
			{
				NK->xOnRoad=(NK->x*6+Net[NK->Links[0]].x+Net[NK->Links[1]].x)/8;
				NK->yOnRoad=(NK->y*6+Net[NK->Links[0]].y+Net[NK->Links[1]].y)/8;
			}
			else
			{
				NK->xOnRoad=NK->x;
				NK->yOnRoad=NK->y;
			}
			memset(NK->WayPointToPointIndex,0xFF,MaxLink*sizeof(word));
		}
	}
	ClearWayPointToPoint();
	int MaxWayPoints=NKnots*4;
	WayPointToPoint=(OneNetWayPointToPoint**)malloc(MaxWayPoints<<2);
	memset(WayPointToPoint,0,MaxWayPoints<<2);
	if(NKnots>1)
	for(i=0;i<NKnots;i++)
	{
		OneNetKnot* NK=&(Net[i]);
		if(NK&&(!NK->Hidden))
		{
			for(int j=0;j<NK->NLinks;j++)
			{
				if(NK->WayPointToPointIndex[j]==0xFFFF)
				{
					word FL=0xFFFF;
					word O=0xFFFF;
					OneNetKnot* DNK=&(Net[NK->Links[j]]);
					if(DNK&&(!DNK->Hidden))
					{
						for(int k=0;k<DNK->NLinks;k++)
						{
							if(DNK->Links[k]==i&&DNK->WayPointToPointIndex[k]!=0xFFFF)
							{
								FL=DNK->WayPointToPointIndex[k];
								O=k;
							}
						}
						if(FL==0xFFFF)
						{
							WayPointToPoint[MaxWayPointToPointIndex] = new OneNetWayPointToPoint(i, NK->Links[j], Net);
							NK->WayPointToPointIndex[j]=MaxWayPointToPointIndex;
							//DNK->WayPointToPointIndex[O]=MaxWayPointToPointIndex;
							MaxWayPointToPointIndex++;
						}
						else
						{
							NK->WayPointToPointIndex[j]=FL;
						}
					}
				}
			}
		}
	}
	if(!IsWayPointToPointCBSet)
	{
		//WayPointToPointCB=SetDrawOnMapCallback(&ShowWayPointToPointCB);
		IsWayPointToPointCBSet=true;
	}
}
void RoadsNet::ShowWayPointToPoint()
{
	for(int i=0;i<MaxWayPointToPointIndex;i++)
		WayPointToPoint[i]->ShowWayPoints();
}
int RoadsNet::GetNextWayPoints(int From, int Dest, int* PointsXYList)
{
	if(Net){
		for(int i=0;i<Net[From].NLinks;i++)
		{
			if(Net[From].Links[i]==Dest)
			{
				if(Net[From].WayPointToPointIndex[i]!=0xFFFF)
				{
					return WayPointToPoint[Net[From].WayPointToPointIndex[i]]->GetWayPoints(From,Dest,PointsXYList);
				}
			}
		}
	}
	return 0;
}
int RoadsNet::GetNextWayPoints(int From, int Dest, int* PointsXYList, word &Flag, DWORD Requestor)
{
	if(From>-1&&From<NKnots)
	{
		for(int i=0;i<Net[From].NLinks;i++)
		{
			if(Net[From].Links[i]==Dest&&WayPointToPoint[Net[From].WayPointToPointIndex[i]]!=NULL)
				return WayPointToPoint[Net[From].WayPointToPointIndex[i]]->GetWayPoints(From,Dest,PointsXYList, Flag, Requestor);
		}
	}
	return 0;
}
int RoadsNet::SetPreLock(int From, int Dest)
{
	for(int i=0;i<Net[From].NLinks;i++)
	{
		if(Net[From].Links[i]==Dest)
		{
			if(Net[From].WayPointToPointIndex[i]!=0xFFFF)
				return WayPointToPoint[Net[From].WayPointToPointIndex[i]]->SetPreLock();
		}
	}
	return 0;
}
int RoadsNet::SetPreLock(int From, int Dest, DWORD Requestor)
{
	for(int i=0;i<Net[From].NLinks;i++)
	{
		if(Net[From].Links[i]==Dest)
		{
			if(Net[From].WayPointToPointIndex[i]!=0xFFFF)
				return WayPointToPoint[Net[From].WayPointToPointIndex[i]]->SetPreLock(Requestor);
		}
	}
	return 0;
}
void RoadsNet::SaveWayPointToPoint(SaveBuf *SB)
{
	for(int i=0;i<MaxWayPointToPointIndex;i++)
		WayPointToPoint[i]->Save(SB);
}
void RoadsNet::LoadWayPointToPoint(SaveBuf *SB)
{
	for(int i=0;i<MaxWayPointToPointIndex;i++)
		WayPointToPoint[i]->Load(SB);
}
RoadsNet RN;
///
int ShowWayPointToPointCB()
{
	if(WayPointToPointCB)WayPointToPointCB();
	RN.ShowWayPointToPoint();
	return 0;
}
void ClearRN(){
	RN.ClearAll();
}
int GetNextWayPoints(int From, int Dest, int *PointsXYList)
{
	return RN.GetNextWayPoints(From, Dest, PointsXYList);
}
int GetNextWayPoints(int From, int Dest, int *PointsXYList, word &Flag, DWORD Requestor)
{
	return RN.GetNextWayPoints(From, Dest, PointsXYList, Flag, Requestor);
}
int SetPreLock(int From, int Dest)
{
	return RN.SetPreLock(From, Dest);
}
int SetPreLock(int From, int Dest, DWORD Requestor)
{
	return RN.SetPreLock(From, Dest, Requestor);
}
void SaveWayPoints(SaveBuf *SB)
{
	RN.SaveWayPointToPoint(SB);
}
void LoadWayPionts(SaveBuf *SB)
{
	RN.LoadWayPointToPoint(SB);
}
///
void SaveRoadsNet(ResFile SB){
	int v='2ENR';
	RBlockWrite(SB,&v,4);
	int sz=8+RN.NKnots*sizeof OneNetKnot;
	RBlockWrite(SB,&sz,4);
	RBlockWrite(SB,&RN.NKnots,4);
	RBlockWrite(SB,RN.Net,RN.NKnots*sizeof  OneNetKnot);
};
void ReloadRoads();
void LoadRoadsNetOld(ResFile SB){
	RN.NKnots=0;
	int N;
	RBlockRead(SB,&N,4);
	if(N>RN.MaxKnot){
		RN.MaxKnot=N;
		RN.Net=(OneNetKnot*)realloc(RN.Net,RN.MaxKnot*sizeof OneNetKnot);
	};
	for(int i=0;i<N;i++)RBlockRead(SB,RN.Net+i,sizeof(OneNetKnot)-8-2*MaxLink);
	RN.NKnots=N;
	ROADSSYS.ClearRoads();
	RN.CreateNetSystem();
};
void CheckMemoryIntegrity();
bool InLoadRoadsNet=0;
void LoadRoadsNet(ResFile SB){
	RN.NKnots=0;
	int N;
	RBlockRead(SB,&N,4);
	if(N>RN.MaxKnot){
		RN.MaxKnot=N;
		RN.Net=(OneNetKnot*)realloc(RN.Net,RN.MaxKnot*sizeof OneNetKnot);
	};
	RBlockRead(SB,RN.Net,N*sizeof OneNetKnot);
	RN.NKnots=N;
	//void RemoveRoadsFromLockPoints();
	//RemoveRoadsFromLockPoints();
	ROADSSYS.ClearRoads();
	InLoadRoadsNet=1;
	RN.CreateNetSystem();
	InLoadRoadsNet=0;
};
void ClearRoads(){
	RN.NKnots=0;
	ROADSSYS.ClearRoads();
}
int STRT=0;
void TestNets(){
	
};
bool AddRoadMode=0;
extern int TP_CurRoad;
extern int ARoadsRef[256];
int CurStartX=-1;
int CurStartY=-1;
void SetStartConditionsForRoads(){
	AddRoadMode=0;
	TP_CurRoad=0;
	CurStartX=-1;
	CurStartY=-1;
};
extern bool Lpressed;
void UnPress();
extern bool Rpressed;
extern bool MouseOverMiniMap;
void ProcessRoadsEdition(int xc,int yc){
	if(MouseOverMiniMap)return;
	if(	(GetKeyState(VK_CONTROL)&0x8000)&&
		(GetKeyState(VK_MENU)&0x8000)&&
		(GetKeyState(VK_SHIFT)&0x8000)) CheckMemoryIntegrity();
	if(AddRoadMode){
		RN.TestKnots();
		if(Rpressed){
			if(CurStartX==-1){
				int xx=xc;
				int yy=yc;
				int K=RN.GetNearestKnot(xx,yy,128);
				if(K!=-1){
					RN.DeleteKnot(K);
					ROADSSYS.ClearRoads();
					RN.CreateNetSystem();
					MakeAllDirtyGBUF();
				};
			}else{
				CurStartX=-1;
				CurStartY=-1;
			};
			UnPress();
		};
		if(Lpressed){
			if(CurStartX==-1){
				int xx=xc;
				int yy=yc;
				if(GetKeyState(VK_SHIFT)&0x8000){
					CurStartX=xx;
					CurStartY=yy;
					Lpressed=0;
					UnPress();
				}else{
					int idx=RN.GetNearestKnot(xx,yy,256);
					if(GetKeyState(VK_CONTROL)&0x8000){
						if(idx!=-1){
							RN.Net[idx].x=xc;
							RN.Net[idx].y=yc;
							ROADSSYS.ClearRoads();
							RN.CreateNetSystem();
						};
					}else{
						CurStartX=xx;
						CurStartY=yy;
						Lpressed=0;
						UnPress();
					};				
				}
			}else{
				int xx=xc;
				int yy=yc;
				int idx0=GetKeyState(VK_SHIFT)&0x8000?-1:RN.GetNearestKnot(CurStartX,CurStartY,256);
				int idx1=RN.GetNearestKnot(xx,yy,256);
				int R=Norma(xx-CurStartX,yy-CurStartY);
				if(R>=256){
					int NL=R/180;
					if(idx0==-1){
						idx0=RN.AddKnot(CurStartX,CurStartY);
					};
					if(idx1==-1){
						idx1=RN.AddKnot(xx,yy);
					};
					if(NL>1){
						int idxp=-1;
						int dx,dy;
						if(RN.Net[idx0].NLinks==1&&!(GetKeyState(VK_MENU)&0x8000)){
							int id=RN.Net[idx0].Links[0];
							dx=RN.Net[idx0].x-RN.Net[id].x;
							dy=RN.Net[idx0].y-RN.Net[id].y;
						}else{
							dx=RN.Net[idx1].x-RN.Net[idx0].x;
							dy=RN.Net[idx1].y-RN.Net[idx0].y;
						};
						int dx0=RN.Net[idx1].x-RN.Net[idx0].x;
						int dy0=RN.Net[idx1].y-RN.Net[idx0].y;
						int N0=Norma(dx0,dy0);
						int NN=Norma(dx,dy);
						dx=dx*N0/NN;
						dy=dy*N0/NN;
						for(int j=1;j<NL;j++){
							int xn=CurStartX+dx*j/NL+(dx0-dx)*j*j/NL/NL;
							int yn=CurStartY+dy*j/NL+(dy0-dy)*j*j/NL/NL;
							//int xn=(j*CurStartX+(NL-j)*xx)/NL;
							//int yn=(j*CurStartY+(NL-j)*yy)/NL;
							int idx2=RN.AddKnot(xn,yn);
							if(idxp==-1){
								RN.AddLink(idx0,idx2,ARoadsRef[TP_CurRoad]);
							}else{
								RN.AddLink(idxp,idx2,ARoadsRef[TP_CurRoad]);
							};
							if(j==NL-1){
								RN.AddLink(idx2,idx1,ARoadsRef[TP_CurRoad]);
							};
							idxp=idx2;														
						};
					}else{
						RN.AddLink(idx0,idx1,ARoadsRef[TP_CurRoad]);
					};
					if(!GetKeyState(VK_SHIFT)&0x8000){
						CurStartX=-1;
						CurStartY=-1;
					}else{
						CurStartX=xx;
						CurStartY=yy;
					};
					ROADSSYS.ClearRoads();
					RN.CreateNetSystem();
					Lpressed=0;
					UnPress();
				};
			};
		}else{
			int IDX0=-1;
			int IDX1=-1;
			if(CurStartX!=-1){
				int xx=xc;
				int yy=yc;
				int idx=RN.GetNearestKnot(xx,yy,256);

				int DX=(mapx<<5);
				int DY=(mapy<<4);
				int SH=Shifter-5;
				int R=Norma(CurStartX-xx,CurStartY-yy);
				if(R==0){
					xx=xc;
					yy=yc;
				}else{
					if(idx!=-1){
						Xbar(((xx-DX)>>SH)-2,((yy/2-GetHeight(xx,yy)-DY)>>SH)-2,4,4,0xFF);
					};
				};
				if(idx!=-1){

				};
				if(R>256)xLine((CurStartX-DX)>>SH,(CurStartY/2-GetTotalHeight(CurStartX,CurStartY)-DY)>>SH,
					(xx-DX)>>SH,(yy/2-GetTotalHeight(xx,yy)-DY)>>SH,255);
				else xLine((CurStartX-DX)>>SH,(CurStartY/2-GetTotalHeight(CurStartX,CurStartY)-DY)>>SH,
					(xx-DX)>>SH,(yy/2-GetTotalHeight(xx,yy)-DY)>>SH,clrRed);
				int xx1=CurStartX;
				int yy1=CurStartY;
				IDX0=RN.GetNearestKnot(xx1,yy1,256);
				IDX1=idx;
			};
			int xx=xc;
			int yy=yc;
			int DX=(mapx<<5);
			int DY=(mapy<<4);
			int SH=Shifter-5;
			int idx=RN.GetNearestKnot(xx,yy,256);
			if(IDX0!=-1){
				idx=IDX0;
				xx=CurStartX;
				yy=CurStartY;
			};
			{
IDX1_draw:
				if(idx!=-1){
					int x2=((xx-DX)>>SH);
					int y2=((yy/2-GetTotalHeight(xx,yy)-DY)>>SH);
					int NLF=RN.Net[idx].NLinks;
					for(int j=0;j<NLF;j++){
						int id=RN.Net[idx].Links[j];
						int xx=RN.Net[id].x;
						int yy=RN.Net[id].y;
						int x3=((xx-DX)>>SH);
						int y3=((yy/2-GetTotalHeight(xx,yy)-DY)>>SH);
						xLine(x2,y2,x3,y3,clrRed);
						Xbar(x3-2,y3-2,5,5,clrRed);
						xLine(x3,y3-5,x3,y3+5,clrRed);
						xLine(x3-5,y3,x3+5,y3,clrRed);
					};
					Xbar(x2-2,y2-2,5,5,0xFF);
					xLine(x2,y2-5,x2,y2+5,0xFF);
					xLine(x2-5,y2,x2+5,y2,0xFF);
				};
				if(IDX1!=-1){
					idx=IDX1;
					xx=RN.Net[idx].x;
					yy=RN.Net[idx].y;
					IDX1=-1;
					goto IDX1_draw;
				};
			};
		};
	};
};
void SetRoadsEditMode(bool Rmode){
	AddRoadMode=Rmode;
	if(!Rmode){
		CurStartX=-1;
	};
};
int NRAreas=0;
bool AddArea(short x,short y,byte Sliv);
void AddArea(short x,short y,byte Sliv,int Type);
void PreCreateTopLinks(int Type){
	if(Type==1)return;
	NRAreas=0;
	for(int i=0;i<RN.NKnots;i++){
		if(RN.Net[i].NLinks==2){
			int x1,y1,x2,y2,x,y;
			char A1,A2;
			int Z1=RN.Net[i].Links[0];
			int Z2=RN.Net[i].Links[1];
			x=RN.Net[i].x;
			y=RN.Net[i].y;
			x1=RN.Net[Z1].x;
			y1=RN.Net[Z1].y;
			x2=RN.Net[Z2].x;
			y2=RN.Net[Z2].y;
			int W=ROADSSYS.RDesc->Roads[RN.Net[i].LinkType[0]].RWidth*2;
			int N1=Norma(x1-x,y1-y);
			if(N1==0)N1=1;
			int N2=Norma(x2-x,y2-y);
			if(N2==0)N2=1;
			x1=x+((x1-x)*W)/N1;
			y1=y+((y1-y)*W)/N1;
			x2=x+((x2-x)*W)/N2;
			y2=y+((y2-y)*W)/N2;
			//AddArea(((x1+x2+x+x+x+x)/6)>>6,((y1+y2+y+y+y+y)/6)>>6,0);
			AddArea(((x1+x2+x+x+x+x)/6)>>6,((y1+y2+y+y+y+y)/6)>>6,2,Type);
		}else{
			//AddArea(RN.Net[i].x>>6,RN.Net[i].y>>6,0);
			AddArea(RN.Net[i].x>>6,RN.Net[i].y>>6,2,Type);
		};
		NRAreas++;
	};

};
int GetLinkScale(int N1,int N2,int Type){
	if(Type&&Type!=2)return 16;
	if(N1<NRAreas&&N2<NRAreas){
		int NL=RN.Net[N1].NLinks;
		for(int q=0;q<NL;q++)if(RN.Net[N1].Links[q]==N2){
			int p=16*256/ROADSSYS.RDesc->Roads[RN.Net[N1].LinkType[q]].Priory;
			if(p<1)p=1;
			return p;
		};
	};
	///if((N1<NRAreas&&N2>=NRAreas)||(N1>=NRAreas&&N2<NRAreas))return 64;
	return 16;
};
int GetZoneSpeedBunus(int Zone){
	if(Zone<RN.NKnots){
		RN.SetupNetParams();
		return RN.NParams[Zone].Speed;
	}else return 256;
};
int GetZoneTiringBonus(int Zone){
	if(Zone<RN.NKnots){
		RN.SetupNetParams();
		return RN.NParams[Zone].Tiring;
	}else return 256;
};
int GetZonePrioryBunus(int Zone){
	if(Zone<RN.NKnots){
		RN.SetupNetParams();
		return RN.NParams[Zone].Priory;
	}else return 256;
};
int GetTiringBonus(int x,int y,int LockType){
	if(LockType==1)return 256;	
	int tx=x>>6;
	int ty=y>>6;
	if(tx>=0&&ty>=0&&tx<TopLx&&ty<TopLy){
		word TZ=GetTopRef(tx+(ty<<TopSH),LockType);
		return GetZoneTiringBonus(TZ);
	}else return 256;
};
void GetPreciseTopCenter(int Zone,int LockType,int& x,int& y){
	if(Zone<RN.NKnots&&LockType!=1){
		x=RN.Net[Zone].x;
		y=RN.Net[Zone].y;
	}else{
		Area* AR=GetTopMap(Zone,LockType);
		x=(AR->x<<6)+32;
		y=(AR->y<<6)+32;
	};
};
int GetKnotNeighbours(int Zone,word* Buf){
	if(Zone<RN.NKnots){
		memcpy(Buf,RN.Net[Zone].Links,RN.Net[Zone].NLinks*2);
		return RN.Net[Zone].NLinks;
	}else return 0;
}
bool CheckIfRoadZone(int Zone){
	return EngSettings.DontUseRoads?false:Zone<RN.NKnots;
};
void RemoveRoadsFromLockPoints(){
	int nfix=0;
	for(int i=0;i<RN.NKnots;i++){
		int x=RN.Net[i].x>>4;
		int y=RN.Net[i].y>>4;
		if(CheckBar(x-2,y-2,5,5)){
			bool fnd=false;
			for(int j=0;j<30&&fnd==false;j++){
                int N=Rarr[j].N;
				char* xi=Rarr[j].xi;
				char* yi=Rarr[j].yi;
				for(int k=0;k<N;k++){
					int xx=x+xi[k];
					int yy=y+yi[k];
					if(!CheckBar(xx-2,yy-2,5,5)){
						RN.Net[i].x=xx*16+8;
						RN.Net[i].y=yy*16+8;
						fnd=true;
						nfix++;
						break;
					}
				}
			}
		}
	}
	int nit=0;
	bool smt=false;
	do{
		smt=false;
		for(int i=0;i<RN.NKnots;i++){
			int x=RN.Net[i].x;
			int y=RN.Net[i].y;
			int NL=RN.Net[i].NLinks;
			int xs=0;
			int ys=0;
			bool fnd=false;
			for(int j=0;j<NL;j++){
                int id=RN.Net[i].Links[j];
				int x1=RN.Net[id].x;
				int y1=RN.Net[id].y;
				if(Norma(x-x1,y-y1)<70){
					fnd=true;
					smt=true;
				}
                xs+=x1;
				ys+=y1;
			}
			if(fnd){
				xs/=NL;
				ys/=NL;
				RN.Net[i].x=xs;
				RN.Net[i].y=ys;
			}
		}
		nit++;
	}while(nit<10&&smt);
	if(nfix){
		ROADSSYS.ClearRoads();
		RN.CreateNetSystem();
	}
}
void CreateWayNet(int x0,int y0,int Z1,int Z2,int *x,int *y,int& N,word UType,int DR){
	int Z2X=RN.Net[Z2].x;
	int Z2Y=RN.Net[Z2].y;
	x[0]=Z2X;
	y[0]=Z2Y;
	N=1;
	int dx=x[0]-x0;
	int dy=y[0]-y0;
	int NR=Norma(dx,dy);
	if(NR<64&&NR>0){
		dx=dx*64/NR;
		dy=dy*64/NR;
		x[0]=x0+dx;
		y[0]=y0+dy;
	};
	return;
	int NL=RN.Net[Z1].NLinks;
	for(int i=0;i<NL;i++)if(RN.Net[Z1].Links[i]==Z2){
		int RID=RN.Net[Z1].RoadID[i];
		if(RID!=0xFFFF){
			OneRoad* ORD=ROADSSYS.Roads+RID;
			int NN=ORD->Np;
			if(NN){
				int Z1X=RN.Net[Z1].x;
				int Z1Y=RN.Net[Z1].y;
				int Z2X=RN.Net[Z2].x;
				int Z2Y=RN.Net[Z2].y;

				int X0=ORD->XY[0];
				int Y0=ORD->XY[1];
				int X1=ORD->XY[(NN-1)*2  ];
				int Y1=ORD->XY[(NN-1)*2+1];

				int R1=Norma(Z1X-X0,Z1Y-Y0);
				int R2=Norma(Z1X-X1,Z1Y-Y1);
				if(R2<R1){
					swap(X0,X1);
					swap(Y0,Y1);
				};
				if(Norma(x0-X0,y0-Y0)<32){
					N=1;
					x[0]=X1;
					y[0]=Y1;
				}else{
					N=2;
					x[0]=X0;
					y[0]=Y0;
					x[1]=X1;
					y[1]=Y1;
				};
				if(RN.Net[Z2].NLinks==2){
					int x1,y1,x2,y2,x0,y0;
					char A1,A2;
					int TZ1=RN.Net[Z2].Links[0];
					int TZ2=RN.Net[Z2].Links[1];
					x0=RN.Net[Z2].x;
					y0=RN.Net[Z2].y;
					x1=RN.Net[TZ1].x;
					y1=RN.Net[TZ1].y;
					x2=RN.Net[TZ2].x;
					y2=RN.Net[TZ2].y;
					int W=ROADSSYS.RDesc->Roads[RN.Net[i].LinkType[0]].RWidth*2;
					int N1=Norma(x1-x0,y1-y0);
					if(N1==0)N1=0;
					int N2=Norma(x2-x0,y2-y0);
					if(N2==0)N2=0;
					x1=x0+((x1-x0)*W)/N1;
					y1=y0+((y1-y0)*W)/N1;
					x2=x0+((x2-x0)*W)/N2;
					y2=y0+((y2-y0)*W)/N2;
					x[N]=(x1+x2+x0+x0+x0+x0)/6;
					y[N]=(y1+y2+y0+y0+y0+y0)/6;
					N++;
					return;
				}else{
					x[0]=RN.Net[Z2].x;
					y[0]=RN.Net[Z2].y;
					N=1;
					return;
				};
			};
		};
	};
	N=2;
	x[0]=RN.Net[Z1].x;
	y[0]=RN.Net[Z1].y;
	x[1]=RN.Net[Z2].x;
	y[1]=RN.Net[Z2].y;
	return;
};
int GetMostTexture(int V){
	if(WTexMapEx[V]>128)return TexMapEx[V];
	else return TexMap[V];
};
int GetMostTextureEx(int V,int tex){
	int W=WTexMapEx[V];
	if(tex==TexMap[V]&&W<255-120)return TexMap[V];
	if(tex==TexMapEx[V]&&W>120)return TexMapEx[V];
	if(WTexMapEx[V]>128)return TexMapEx[V];
	else return TexMap[V];
};
void SetTexRadiusWeighted(int x,int y,int r0,int dr,int Tex,int TexWeight,int Facture,int FactWeight){
	int r=((r0+dr)>>5)+4;
	int minr=100000;
	int besttex=0;
	int utx=x>>5;
	int uty=y>>5;
	for(int tx=-r;tx<r;tx++){
		for(int ty=-r;ty<r;ty++){
			int vx=utx+tx;
			int vy=uty+ty;
			if(vx>=0&&vx<VertInLine&&vy>0&&vy<MaxTH){
				int vert=vx+vy*VertInLine;
				int rx=vx<<5;
				int ry=vy<<5;
				if(vx&1)ry-=16;
				int R=Norm(rx-x,ry-y);
				//byte a=byte(atan2(rx-x,ry-y)*255.0/3.1415/2)>>4;
				int R0=r0;//int(randoma[(rpos+a)&8191])*r0/16384;
				int W=0;
				if(R<R0){
					W=255;					
				}else
				if(R<R0+dr){
					W=(R0+dr-R)*255/dr;
				};
				if(W){
					if(Tex>=0&&Tex<64){
						int T0=GetMostTexture(vert);
						AssignExTex2(vert,T0,Tex,W*TexWeight/255);
						//MarkPointToDraw(vert);
						//MakeDirtyGBUF(vx-2,(vy>>1)-(THMap[vert]>>5)-2,4,4);
						void MakeDirtyPiece(int x,int y);
						int GetTriX(int i);
						int GetTriY(int i);
						MakeDirtyPiece(GetTriX(vert),GetTriY(vert));
					};
					if(Facture>=0&&Facture<255){
						W=W*FactWeight/255;
						if(W>32){
							FactureWeight[vert]=W;
							FactureMap   [vert]=Facture;
						};
					};
				};
			};
		};
	};
};
int GetNRoads(){
	return RN.NKnots;
}
int GetNRoadsLink(int index)
{
	if(RN.NKnots>index)
		return RN.Net[index].NLinks;
	return -1;
}
int GetKnotLink(int Index,int LinkIndex){
	if(RN.NKnots>Index)return RN.Net[Index].Links[LinkIndex];
	return -1;
}
int GetFactTexture(int x,int y){
	int vx=x>>5;
	int vy=y>>5;
	if(vx<0)vx=0;
	if(vy<0)vy=0;
	if(vx>=VertInLine)vx=VertInLine-1;
	if(vy>=MaxTH)vy=MaxTH-1;
	int ofs=vx+vy*VertInLine;
 	int idx=FactureMap[ofs];
	//return F3D.GTex[idx].TextureID;
	return GetFacture(idx);
}
void DrawLockedWays(){
	return;
	for(int i=0;i<RN.MaxWayPointToPointIndex;i++){
		if(RN.WayPointToPoint[i]->PreLock){
			int s=RN.WayPointToPoint[i]->StartKnot;
			int e=RN.WayPointToPoint[i]->EndKnot;
			OneNetKnot* SK=RN.Net+s;
			OneNetKnot* EK=RN.Net+e;
			void DrawWorldLine(float x0,float y0,float x1,float y1,DWORD Diffuse);

			int xs=SK->x;
			int ys=SK->y;
			int xd=EK->x;
			int yd=EK->y;

			DrawWorldLine(SK->x,SK->y,EK->x,EK->y,0xFFFFFFFF);

			int xx=(xs+xd)/2;
			int yy=(ys+yd)/2;
			Vector3D V=SkewPt(xx,yy,GetHeight(xx,yy));
			WorldToScreenSpace(V);
			if(V.x>-200&&V.y>-200&&V.x<RealLx+200&&V.y<RealLy+200){
                char cc[128];
				sprintf(cc,"(OW=%d)",RN.WayPointToPoint[i]->Owner);
				ShowString(V.x-GetRLen(cc,&RedFont)/2,V.y,cc,&RedFont);
			}
		}
	}
}