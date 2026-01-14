#include "stdheader.h"
#include "GP_Draw.h"

extern IRenderSystem*	IRS;

extern byte *tex1;
extern RLCTable SimpleMask;
int mul3(int);
int prp34(int i);
//class that is used for fast cashing of the 3D surface

int VirtLx;
int VirtLy;
extern VirtualScreen SVSC;
void CheckSVSC(int set){
	return;
	int nn=SVSC.MaxTMY;
	int dy=SVSC.MaxTMX;
	int pos=set;
	/*
	for(int i=0;i<nn-3;i++){
		assert(SVSC.TriangMap[pos]-2<=SVSC.LoTriMap[pos]);
		pos+=dy;
	};
	*/
};
VirtualScreen::VirtualScreen(){
	CellQuotX=NULL;
};
#define xnew(s,t) (t*)malloc((s)*sizeof(t))
void VirtualScreen::SetSize(int scLx,int scLy){
	void SetupGBUF(int LX,int LY);
	SetupGBUF(scLx,scLy);
	if(CellQuotX){
		free(VirtualScreenPointer-RealVLx-RealVLx);
		free(CellQuotX);
		free(CellQuotY);
		free(CellFlags);
		free(MarkedX);
		free(TriangMap);
		free(LoTriMap);
	};
	MemReport("VirtualScreen(Start)");
	CellSX=TriUnit*2;
	CellSY=TriUnit34*8;
	int N=scLx/CellSX;
	if(N&1)N+=5;else N+=4;
	MaxSizeX=N*CellSX;
	N=scLy/CellSY;
	MaxSizeY=(N+4)*CellSY;
	//MaxSizeX=1600;
	//MaxSizeY=1200;
	RealVLx=MaxSizeX;
	RealVLy=MaxSizeY;
	//CellSX=TriUnit*2;
	//CellSY=TriUnit34*8;
	ShiftsPerCellX=div(CellSX,32).quot;
	ShiftsPerCellY=div(CellSY,mul3(8)).quot;
	CellNX=div(RealVLx,CellSX).quot;
	CellNY=div(RealVLy,CellSY).quot;
	RealVLx=CellNX*CellSX;
	RealVLy=CellNY*CellSY;
	NCells=CellNX*CellNY;
	CellQuotX=xnew(NCells,byte);
	CellQuotY=xnew(NCells,byte);
	CellFlags=xnew(NCells,byte);
	memset(CellQuotX,0,NCells);
	memset(CellQuotY,0,NCells);
	memset(CellFlags,0,NCells);
	Lx=128<<ADDSH;
	Ly=128<<ADDSH;
	VirtualScreenPointer=(xnew(RealVLx*(RealVLy+4),byte))+RealVLx+RealVLx;
	memset(VirtualScreenPointer-(RealVLx<<1),255,RealVLx<<1);
	memset(VirtualScreenPointer+(RealVLx*RealVLy),255,RealVLx<<1);
	MaxTMX=div(256<<ADDSH,ShiftsPerCellX).quot;
	MaxTMY=div(256<<ADDSH,ShiftsPerCellY).quot+1;
	TriangMap=xnew(MaxTMX*MaxTMY,int);
	LoTriMap=xnew(MaxTMX*MaxTMY,int);
	MarkedX=xnew(MaxTMX,byte);
	memset(MarkedX,1,MaxTMX);
	Grids=false;
	MemReport("VirtualScreen(End)");
};
VirtualScreen::~VirtualScreen(){
	free(VirtualScreenPointer-RealVLx-RealVLx);
	free(CellQuotX);
	free(CellQuotY);
	free(CellFlags);
	free(MarkedX);
	free(TriangMap);
	free(LoTriMap);
};
void VirtualScreen::SetVSParameters(int sLx,int sLy){
	Lx=sLx;
	Ly=sLy;
};

void CopyTo16(int x,int y,byte* Src,int Pitch,int Lx,int Ly){
	Lx&=~3;
	if(!(Lx&&Ly))return;
	//definitions
	void Update8to16(byte* src,int sPitch,int x,int y,int Lx,int Ly);
	void Copy16(byte* Src,int SrcPitch,byte* Dst,int DstPitch,int Lx,int Ly);
	Update8to16(Src,Pitch,x,y,Lx,Ly);
	return;	
};
void Copy16(byte* Src,int SrcPitch,byte* Dst,int DstPitch,int Lx,int Ly);
void LogIt(LPSTR sz,...);
void VirtualScreen::CopyVSPart(int vx,int vy,int sx,int sy,int SizeX,int SizeY){
	void CopyTo16(int x,int y,byte* Src,int Pitch,int Lx,int Ly);
	bool LockBackBuffer(int& Pitch,void** ptr);
	void UnlockBackBuffer();
	int Pitch;
	void* ptr;
	//if(LockBackBuffer(Pitch,&ptr)){
		//try{
			int vsofs=int(VirtualScreenPointer)+vx+vy*RealVLx;
			//int scofs=int(ptr)+(sx<<1)+sy*Pitch;
			if(SizeX&&SizeY){
				CopyTo16(sx,sy,(byte*)vsofs,RealVLx,SizeX,SizeY);
			};
		//}catch(...){};
		//UnlockBackBuffer();
	//};
	return;
};
void VirtualScreen::CopyVSPartMMX(int vx,int vy,int sx,int sy,int SizeX,int SizeY){
	return;
};
extern int RealLx;
extern int RealLy;
#define SRECT(r,a,b,c,d) r.left=a;r.top=b;r.right=c;r.bottom=d;
#define SPT(p,a,b) p.x=a;p.y=b;
int PrevMapX=-10000;
int PrevMapY=-10000;

bool Copy16dest=0;
void Draw3DFactures(int dstX,int dstY,int dstDevice,int x,int y,int Lx,int Ly);
void DrawTriStrip(int DevID,int x,int y,int mx,int Gy,int mLx,int GLy,int ScShift);
void CopyMRects(int TextureID,int TexSizeX,int TexSizeY,RECT* RC,int N,POINT* pt);
void FillTexture(int ID,DWORD COLOR,int DR){
	IRS->SetRenderTarget(ID);
	BaseMesh BM;
	BM.create(4,6,vfTnL);
	VertexTnL* VR=(VertexTnL*)BM.getVertexData();
	VR[0].x=DR;
	VR[0].y=DR;
	VR[0].diffuse=COLOR;
	VR[0].w=1.0;
	VR[0].z=0;

	VR[1].x=256-DR;
	VR[1].y=DR;
	VR[1].diffuse=COLOR;
	VR[1].w=1.0;
	VR[1].z=0;

	VR[2].x=DR;
	VR[2].y=256-DR;
	VR[2].diffuse=COLOR;
	VR[2].w=1.0;
	VR[2].z=0;

	VR[3].x=256-DR;
	VR[3].y=256-DR;
	VR[3].diffuse=COLOR;
	VR[3].w=1.0;
	VR[3].z=0;

	word* ids=BM.getIndices();
	ids[0]=0;
	ids[1]=1;
	ids[2]=2;
	ids[3]=1;
	ids[4]=3;
	ids[5]=2;

	BM.setNInd(6);
	BM.setNPri(2);
	BM.setNVert(4);
	BM.setShader(IRS->GetShaderID("fill"));
	IRS->Draw(BM);
};
void RenderScreenArea(int BackBufferX,int BackBufferY,int MapX,int MapY,int MpLx,int MpLy);
class GroundBuffer{
public:
	int LastCreateTime;
	int BBSize;
	int BackBufferIDS[256];
	int NBX,NBY,BSZ;
	int ZBufferID;
	short* QuotX;
	short* QuotY;
	int CellLX;
	int CellLY;
	int NCX;
	int NCY;
	void CopyFromMultiBuffer(RECT* RC,int N,POINT* pt){
		RECT R;
		POINT p;
		for(int i=0;i<N;i++){
			int x0=RC->left;
			int y0=RC->top;
			int x1=RC->right;
			int y1=RC->bottom;
			int XX=x0&0xFF00;
			int YY=y0&0xFF00;
			int XR=XX+256;
			int YD=YY+256;
			int dx=0;
			int dy=0;
			do{
				R.left=x0-XX;
				R.top=y0-YY;
				R.right=XR<x1?XR-XX:x1-XX;
				R.bottom=YD<y1?YD-YY:y1-YY;
				p.x=pt->x+dx;
				p.y=pt->y+dy;
				CopyMRects(BackBufferIDS[(x0>>8)+(y0>>8)*NBX],256,256,&R,1,&p);
				dx+=XR-x0;
				x0=XR;
				XX+=256;
				XR+=256;
				if(x0>=x1){
					dx=0;
					x0=RC->left;
					x1=RC->right;
					XX=x0&0xFF00;
					XR=XX+256;
					dy+=YD-y0;
					y0=YD;
					YY+=256;
					YD+=256;
					if(y0>=y1)break;
				};
			}while(1);
			RC++;
			pt++;
		};
	};
	GroundBuffer(){
		memset(this,0,sizeof*this);
	};
	~GroundBuffer(){Erase();};
	void Create(int SizeX,int SizeY,int OneLx,int OneLy){
		SizeX+=64;
		SizeY+=64;
		VirtLx=SizeX;
		VirtLy=SizeY;
		Erase();
		CellLX=OneLx;
		CellLY=OneLy;
		NCX=(SizeX/OneLx);
		NCY=(SizeY/OneLy);
		QuotX=new short[NCX*NCY];
		QuotY=new short[NCX*NCY];
		MakeAllDirty();
		NBX=SizeX>>8;
		if(NBX*256<SizeX)NBX++;
		NBY=SizeY>>8;
		if(NBY*256<SizeY)NBY++;
		int sz=NBX*NBY;

		BSZ=NBX*NBY;
		MakeAllDirty();
		LastCreateTime=GetTickCount();
	};
	void Erase(){
		if(QuotX)delete[](QuotX);
		if(QuotY)delete[](QuotY);
		QuotX=NULL;
		QuotY=NULL;
	};
	void MakeAllDirty(){
		if(QuotX&&QuotY){
			int sz=NCX*NCY*2;
			memset(QuotX,0xFF,sz);
			memset(QuotY,0xFF,sz);
		};
	};
	void MakeDirty(int mx,int my,int szx,int szy){
		int max=NCX*NCY;
		for(int iy=0;iy<szy;iy++){
			int qy=(my+iy)/NCY;
			int py=(my+iy)%NCY;
			int qx=mx/NCX;
			int px=mx%NCX;
			int ofs=px+py*NCX;
			for(int ix=0;ix<szx;ix++){
				if(ofs>=0&&ofs<max){
					if(QuotX[ofs]==qx&&QuotY[ofs]==qy){
						QuotX[ofs]=-1;
						QuotY[ofs]=-1;
					};
				}
				if(px==NCX-1){
					px=0;
					qx++;
					ofs-=NCX-1;
				}else{
					px++;
					ofs++;
				}
			}
		}
	};
	void RefreshForCurrentPosirtion(int mx,int my){
		if(LastCreateTime&&GetTickCount()-LastCreateTime>4000){
			LastCreateTime=0;
			MakeAllDirty();
		}
		int NC=16<<(5-Shifter);
		int Summ=0;
		for(int iy=0;iy<NCY;iy++){
			int qy=(my+iy)/NCY;
			int py=(my+iy)%NCY;
			int qx=mx/NCX;
			int px=mx%NCX;
			int ofs=px+py*NCX;
			for(int ix=0;ix<NCX;ix++){
				if(QuotX[ofs]!=qx||QuotY[ofs]!=qy){
					int DirtyLx=1;
					int DirtyLy=1;
					int dpx=px;
					int dqx=qx;
					int ofs2=ofs;
					if(dpx<NCX-1){
						dpx++;
						ofs2++;
						for(int dx=ix+1;dx<NCX&&dpx<NCX&&(QuotX[ofs2]!=qx||QuotY[ofs2]!=qy);dx++){
							DirtyLx++;
							dpx++;
							ofs2++;
						};
					};
					int RLX=DirtyLx<8?DirtyLx*NC:8*NC;
					Summ=RLX;
					int ok=1;
					int dpy=py+1;
					for(int dy=iy+1;dy<NCY&&dpy<NCY&&ok&&Summ<8000;dy++,dpy++){
						dpx=px;
						dqx=qx;
						ofs2=ofs+DirtyLy*NCX;
						for(int dx=0;dx<DirtyLx&&ok;dx++){
							if(QuotX[ofs2]==qx&&QuotY[ofs2]==qy)ok=0;
							dpx++;
							ofs2++;
						};
						if(ok){
							DirtyLy++;
							Summ+=RLX;
						};
					};
					for(dy=0;dy<DirtyLy;dy++){
						dpx=px;
						dqx=qx;
						ofs2=ofs+dy*NCX;
						for(int dx=0;dx<DirtyLx;dx++){
							QuotX[ofs2]=qx;
							QuotY[ofs2]=qy;
							dpx++;
							ofs2++;
						};
					};
					RenderScreenArea(px*CellLX,py*CellLY,mx+ix,my+iy,DirtyLx,DirtyLy);
					Summ=0;
				};
				if(px==NCX-1){
					px=0;
					qx++;
					ofs-=NCX-1;
				}else{
					px++;
					ofs++;
				};
			};
		};
	};
};

#define MAXLY 3000
struct OneSprInfo{
	short x,y,file,spr;
};
class SmallZBuffer{
public:
	int MaxLines;
	OneSprInfo* SPRS[MAXLY];
	int NSprs[MAXLY];
	int MaxSprs[MAXLY];
	SmallZBuffer(int Ny);
	~SmallZBuffer();
	void Add(int y,int sx,int sy,int FID,int spr);
	void Draw();
};
int TGP=-1;

void DrawTreesOnGround(int BackBufferX,int BackBufferY,int MapX,int MapY,int MpLx,int MpLy,int HScale){
	if(TGP==-1){
		TGP=GPS.PreLoadGPImage("TreesAll");
		if(TGP!=-1)GPS.LoadGP(TGP);
	};

    Rct clipArea=GPS.GetClipArea();
	SetWind(BackBufferX,BackBufferY,MpLx<<Shifter,MpLy);
	Rct VP=GPS.GetClipArea();
	int WindX=VP.x;
	int WindY=VP.y;
	int WindX1=VP.x+VP.w-1;
	int WindY1=VP.y+VP.h-1;

	SmallZBuffer ZB(800+MpLy);	
	int NT=0;
	int SE=0;
	int NS=0;
	int SH=5-Shifter;

	int DX=(MapX<<5)-(BackBufferX<<SH);
	int DY=(MapY-BackBufferY)<<SH;

	int spx0=(MapX>>2)-2;
	int spx1=(MapX>>2)+(MpLx>>2)+2;
	int spy0=(MapY>>(Shifter+1))-2;
	int spy1=(MapY>>(Shifter+1))+(MpLy>>(Shifter+1))+6;

	if(spx0<0)spx0=0;else
	if(spx0>=VAL_SPRNX)spx0=VAL_SPRNX-1;
	if(spy0<0)spy0=0;else
	if(spy0>=VAL_SPRNX)spy0=VAL_SPRNX-1;
	if(spx1<0)spx1=0;else
	if(spx1>=VAL_SPRNX)spx1=VAL_SPRNX-1;
	GPS.SetScale(1/float(1<<SH));
	static int trid=GPS.PreLoadGPImage("TREES");

	static int s1=IRS->GetShaderID("StonesOnGround"); 
	static int s0=IRS->GetShaderID("TreesShadow_L");	
	GPS.SetCurrentShader(s1);	
	GPS.SetCurrentDiffuse(0xFFFFFFFF);	
	for(int spx=spx0;spx<=spx1;spx++){
		int ofst=spx+(spy0<<SprShf);
		for(int spy=spy0;spy<=spy1;spy++){
			if(ofst<VAL_MAXCIOFS){
				int N=NSpri[ofst];
				int* List=SpRefs[ofst];
				if(N&&List){
					for(int i=0;i<N;i++){
						int p=List[i];
						extern int MAXSPR;
						if(p<=MAXSPR){
							OneSprite* OS=Sprites+p;
							ObjCharacter* OC=OS->OC;
							if(OC->ViewType==0&&OC->RenderType){						
								int SX=OS->x-DX;
								int SY=(OS->y>>1)-DY;
								int FID=OS->OC->FileID_forBackground;
								if(FID==0xFFFF)FID=-1;
								if(OS->OC->FileID==trid)FID=TGP;
								int sgi=OS->SGIndex;
								int SPR=OS->SG->Objects[sgi]->SpriteID_forBackground;
								int sdx=OS->OC->CenterX;
								int sdy=OS->OC->CenterY;
								int W=GPS.GetGPWidth(FID,SPR)>>SH;
								int H=GPS.GetGPHeight(FID,SPR)>>SH;
								int spx=(SX-sdx)>>SH;
								int spy=(SY-sdy-(((Mode3D?OS->z:0)*HScale)>>8))>>SH;
								SX>>=SH;
								SY>>=SH;
								if(spx<WindX1&&spy<WindY1&&spx+W>=WindX&&spy+H>=WindY){
									if(OS->Enabled){
										if(OC->RenderType==2){
											GPS.ShowGP(spx,spy,FID,SPR,0);
											NS++;
										};
										if(OC->RenderType==1){
											int sgi=OS->SGIndex;
											ZB.Add(SY+300,spx,spy,FID,SPR);
											NT++;
										}
									}
								}
							}
						}
					}
				}
			}
			ofst+=VAL_SPRNX;
		}
	}
	GPS.FlushBatches();
	GPS.SetCurrentShader(s0);
	GPS.SetCurrentDiffuse(EngSettings.ShadowColor);	
	ZB.Draw();
	GPS.SetScale(1);
	GPS.SetCurrentDiffuse(0xFF808080);
    GPS.SetClipArea(clipArea.x, clipArea.y, clipArea.w, clipArea.h);
};

GroundBuffer GBUF;
void RenderScreenArea(int BackBufferX,int BackBufferY,int MapX,int MapY,int MpLx,int MpLy){
	//int DVID=IRS->GetVBufferID();
	//IRS->SaveTexture(DVID,"001.dds");
	int BX0=BackBufferX&0xFF00;
	int BY0=BackBufferY&0xFF00;
	int x1=BackBufferX+(MpLx<<5);
	int y1=BackBufferY+(MpLy<<4);
	int BX1=(x1-1)&0xFF00;
	int BY1=(y1-1)&0xFF00;	
	for(int ix=BX0;ix<=BX1;ix+=256){
		for(int iy=BY0;iy<=BY1;iy+=256){
			int X0=ix>BackBufferX?ix:BackBufferX;
			int Y0=iy>BackBufferY?iy:BackBufferY;
			int X1=ix+256<x1?ix+256:x1;
			int Y1=iy+256<y1?iy+256:y1; 
			int ID=(X0>>8)+(Y0>>8)*GBUF.NBX;
			if(X1>X0&&Y1>Y0)DrawTriStrip(GBUF.BackBufferIDS[ID],X0-ix,Y0-iy,(MapX+((X0-BackBufferX)>>5))<<(5-Shifter),(MapY<<4)+Y0-BackBufferY,(X1-X0)>>Shifter,Y1-Y0,5-Shifter);
			
			TempWindow TW;
			PushWindow(&TW);
			GPS.FlushBatches();
			
			IRS->SetRenderTarget(GBUF.BackBufferIDS[ID]);
			
			static int shTreeShadow = IRS->GetShaderID( "TreesShadow_L" );
			GPS.SetCurrentShader( shTreeShadow );
			DrawTreesOnGround(X0-ix,Y0-iy,(MapX+((X0-BackBufferX)>>5))<<(5-Shifter),(MapY<<4)+Y0-BackBufferY,(X1-X0)>>Shifter,Y1-Y0,256);			
			GPS.FlushBatches();
			IRS->SetRenderTarget(0);
			
			PopWindow(&TW);
			
		};
	};
	SetWind(0,0,RealLx,RealLy);
	//DrawTriStrip(DVID,BackBufferX,BackBufferY,MapX,MapY<<4,MpLx,MpLy<<4,0);
	//Draw3DFactures(BackBufferX,BackBufferY,DVID,MapX<<5,MapY<<4,MpLx<<5,MpLy<<4);
	//IRS->SaveTexture(DVID,"001.dds");
};
extern int HScale;
void RenderSurfaceToTexture(int TexID,int MapX,int MapY,int ScaleDeep,int HeightScale)
{
	HScale=HeightScale;
	IRS->SetRenderTarget(TexID);

	if(ScaleDeep>=2){
		DrawTriStrip(TexID,0,0,MapX>>5,MapY>>ScaleDeep,4<<ScaleDeep,256,ScaleDeep);
		DrawTriStrip(TexID,128,0,(MapX>>5)+(4<<ScaleDeep),MapY>>ScaleDeep,4<<ScaleDeep,256,ScaleDeep);
	}else DrawTriStrip(TexID,0,0,MapX>>5,MapY>>ScaleDeep,8<<ScaleDeep,256,ScaleDeep);

	static int shID = IRS->GetShaderID( "TreesShadow_L" );
	GPS.SetCurrentShader( shID );

	DrawTreesOnGround(0,0,MapX>>5,MapY>>ScaleDeep,8<<ScaleDeep,256,HeightScale);
	GPS.FlushBatches();
	IRS->SetRenderTarget(0);
	HScale=256;
}


void TestSurface(int sh){
	TextureDescr td;
	td.setValues(256,256,cfBackBufferCompatible,mpVRAM,1,tuRenderTarget);
	static int tid=IRS->CreateTexture("tempSurf",td);
	void RenderSurfaceToTexture(int TexID,int MapX,int MapY,int ScaleDeep,int);
	RenderSurfaceToTexture(tid,mapx<<5,mapy<<4,5-Shifter,128+64+64.0*sin(float(GetTickCount())/500.0f));
	static BaseMesh* BM;
	if(!BM){
		BM=new BaseMesh;
		BM->create(4,6,vfTnL);
		word* idx=BM->getIndices();
		idx[0]=0;
		idx[1]=1;
		idx[2]=2;
		idx[3]=2;
		idx[4]=1;
		idx[5]=3;
		VertexTnL* V=(VertexTnL*)BM->getVertexData();
		float x=0;
		float y=0;
		float x1=255;
		float y1=255;
		float uL=0;
		float vL=0;
		float uR=1.0;
		float vR=1.0;
		DWORD Diffuse=0xFFFF8080;

		V[0].x=x;
		V[0].y=y;
		V[0].w=1.0;
		V[0].u=uL;
		V[0].v=vL;
		V[0].diffuse=Diffuse;        

		V[1].x=x1;
		V[1].y=y;
		V[1].w=1.0;
		V[1].u=uR;
		V[1].v=vL;
		V[1].diffuse=Diffuse;        

		V[2].x=x;
		V[2].y=y1;
		V[2].w=1.0;
		V[2].u=uL;
		V[2].v=vR;
		V[2].diffuse=Diffuse;        

		V[3].x=x1;
		V[3].y=y1;
		V[3].w=1.0;
		V[3].u=uR;
		V[3].v=vR;
		V[3].diffuse=Diffuse;        

		BM->setNInd(6);
		BM->setNPri(2);
		BM->setNVert(4);
		BM->setShader(sh);
		BM->setTexture(tid);
	}
	IRS->Draw(*BM);
}
void SetupGBUF(int LX,int LY){
	GBUF.Create(LX,LY,32,16);
};

void InvalidateTerrainPatch( int x, int y, int w, int h );
void MakeDirtyGBUF(int mx,int my,int sx,int sy){
	GBUF.MakeDirty(mx,my,sx,sy);
	InvalidateTerrainPatch( mx*32, my*32, sx*32, sy*2*32 );
};
void MakeAllDirtyGBUF(){
	GBUF.MakeAllDirty();
	void ResetGroundCache();
	ResetGroundCache();
};
BaseMesh* COPYMESH=NULL;
void CopyMRects(int TextureID,int TexSizeX,int TexSizeY,RECT* RC,int N,POINT* pt){
	if(!COPYMESH){
		COPYMESH=new BaseMesh;
		COPYMESH->create(16,24,vfTnL);
		COPYMESH->setShader(IRS->GetShaderID("copy")); 
		word* idx=COPYMESH->getIndices();
		for(int i=0;i<4;i++){
			int v6=i*6;
			int v4=i*4;
			idx[v6  ]=v4  ;
			idx[v6+1]=v4+1;
			idx[v6+2]=v4+2;
			idx[v6+3]=v4+1;
			idx[v6+4]=v4+3;
			idx[v6+5]=v4+2;
		};
	};
	VertexTnL* VR=(VertexTnL*)COPYMESH->getVertexData();
	COPYMESH->setNInd(N*6);
	COPYMESH->setNPri(N*2);
	COPYMESH->setNVert(N*4);
	COPYMESH->setTexture(TextureID);
	for(int i=0;i<N;i++){
		int imlx=RC[i].right-RC[i].left;
		int imly=RC[i].bottom-RC[i].top;
		VR[0].x=pt[i].x;
		VR[0].y=pt[i].y;
		VR[0].w=1;
		VR[0].z=0;
		VR[0].u=float(RC[i].left)/TexSizeX;
		VR[0].v=float(RC[i].top)/TexSizeY;

		VR[1].x=pt[i].x+imlx;
		VR[1].y=pt[i].y;
		VR[1].w=1;
		VR[1].u=float(RC[i].left+imlx)/TexSizeX;
		VR[1].v=float(RC[i].top)/TexSizeY;
		VR[1].z=0;

		VR[2].x=pt[i].x;
		VR[2].y=pt[i].y+imly;
		VR[2].w=1;
		VR[2].u=float(RC[i].left)/TexSizeX;
		VR[2].v=float(RC[i].top+imly)/TexSizeY;
		VR[2].z=0;

		VR[3].x=pt[i].x+imlx;
		VR[3].y=pt[i].y+imly;
		VR[3].w=1;
		VR[3].u=float(RC[i].left+imlx)/TexSizeX;
		VR[3].v=float(RC[i].top+imly)/TexSizeY;
		VR[3].z=0;
		//for(int j=0;j<4;j++){
		//	int dy=VR[j].y;
		//	VR[j].x=512+(VR[j].x-512)*3000/(3000-dy);
		//	VR[j].y=dy*4000/(4000-dy);
		//};
		VR+=4;
	};
	IRS->Draw(*COPYMESH);
};


void VirtualScreen::CopyVSToScreen()
{
	void ProcessLightMap();
	ProcessLightMap();
	
	GBUF.RefreshForCurrentPosirtion(mapx>>(5-Shifter),mapy>>(5-Shifter));
	//copying from surface to back buffer
	RECT CR[4];
	POINT PT[4];
	int Ncr=0;
	int xs=mapx<<Shifter;
	int ys=mapy<<(Shifter-1);
	int xp=xs%VirtLx;
	int yp=ys%VirtLy;
	xs=VirtLx-(xp%VirtLx);
	ys=VirtLy-(yp%VirtLy);
	if(xp&&yp){
		SRECT(CR[0],0,0,xp,yp);
		SPT(PT[0],xs,ys);

		SRECT(CR[1],xp,0,VirtLx,yp);
		SPT(PT[1],0,ys);

		SRECT(CR[2],0,yp,xp,VirtLy);
		SPT(PT[2],xs,0);

		SRECT(CR[3],xp,yp,VirtLx,VirtLy);
		SPT(PT[3],0,0);

		Ncr=4;
	}else if(xp){
		SRECT(CR[0],0,0,xp,VirtLy);
		SPT(PT[0],xs,0);

		SRECT(CR[1],xp,0,VirtLx,VirtLy);
		SPT(PT[1],0,0);

		Ncr=2;
	}else if(yp){
		SRECT(CR[0],0,0,VirtLx,yp);
		SPT(PT[0],0,ys);

		SRECT(CR[1],0,yp,VirtLx,VirtLy);
		SPT(PT[1],0,0);

		Ncr=2;
	}else{
		SRECT(CR[0],0,0,VirtLx,VirtLy);
		SPT(PT[0],0,0);
		Ncr=1;
	};
	GBUF.CopyFromMultiBuffer(CR,Ncr,PT);
	return;
};
void VirtualScreen::RenderVSPart(int QuotX,int QuotY,int cx,int cy,int clx,int cly){
	if(!(clx&&cly))return;
	//debugging part
	//assert(cx>=0&&cy>=0&&cx+clx<=CellNX&&cy+cly<=CellNY);
	//--------------
	int spos=cx+cy*CellNX;
	int StartCellY;
	int NCellY;
	bool CellStart=false;
	for(int px=0;px<clx;px++){
		int pos=spos+px;
		for(int py=0;py<cly;py++){
			if((!CellFlags[pos])||CellQuotX[pos]!=QuotX||CellQuotY[pos]!=QuotY){
				//need to be rendered
				if(CellStart)NCellY++;
				else{
					CellStart=true;
					StartCellY=py+cy;
					NCellY=1;
				};
			}else{
				if(CellStart){
					RenderVerticalSet(QuotX,QuotY,px+cx,StartCellY,NCellY);
					CellStart=false;
				};
			};
			pos+=CellNX;
		};
		if(CellStart){
			RenderVerticalSet(QuotX,QuotY,px+cx,StartCellY,NCellY);
			CellStart=false;
		};
	};
};
void VirtualScreen::RefreshSurface(){
	//calculating starting cell
	int scx=div(mapx,ShiftsPerCellX).quot;
	int scy=div(mapy,ShiftsPerCellY).quot;
	int scx1=div(mapx+smaplx-1,ShiftsPerCellX).quot;
	int scy1=div(mapy+smaply-1,ShiftsPerCellY).quot;
	int clnx=scx1-scx+1;
	int clny=scy1-scy+1;
	int vsx=div(scx,CellNX).rem;
	int vsy=div(scy,CellNY).rem;
	int Lx0,Ly0,Lx1,Ly1;
	if(vsx+clnx<=CellNX){
		Lx0=clnx;
		Lx1=0;
	}else{
		Lx0=CellNX-vsx;
		Lx1=clnx-Lx0;
	};
	if(vsy+clny<=CellNY){
		Ly0=clny;
		Ly1=0;
	}else{
		Ly0=CellNY-vsy;
		Ly1=clny-Ly0;
	};
	int QuotX=div(scx,CellNX).quot;
	int QuotY=div(scy,CellNY).quot;
	RenderVSPart(QuotX,QuotY,vsx,vsy,Lx0,Ly0);
	RenderVSPart(QuotX+1,QuotY,0,vsy,Lx1,Ly0);
	RenderVSPart(QuotX,QuotY+1,vsx,0,Lx0,Ly1);
	RenderVSPart(QuotX+1,QuotY+1,0,0,Lx1,Ly1);
};
extern byte ExtTex[256][4];
extern short randoma[8192];
byte DTX(byte v,int t){
	return ExtTex[v][randoma[t&8191]&3];
};
extern byte TileMap[256];
int VirtualScreen::ShowLimitedSector(int i,bool Mode3D,int HiLine,int LoLine,int QuotX,int QuotY){	
	return 1;
};
void CheckFirstLine();

void DrawOnePixCell(byte* Buf,int x,int y,int cx,int cy,int BufWidth);
void VirtualScreen::RenderVerticalSet(int QuotX,int QuotY,int cx,int cy,int cly){	
};
void VirtualScreen::CreateTrianglesMapping(){
	int NELM=MaxTMX*MaxTMY;
	memset(TriangMap,0xFF,NELM<<2);
	memset(LoTriMap,0xFF,NELM<<2);
	/*
	for(int nx=0;nx<MaxTH*2;nx++){
		CreateVerticalTrianglesMapping(nx);
	};
	*/
};
int GetMaxTriY(int StartTri,int InsTri,bool Minimax){
	div_t ddt=div(StartTri,MaxTH*2);
	int StartVertex=ddt.quot*VertInLine+(ddt.rem>>1);
	int y1,y2,y3;
	if(Mode3D){
		switch(InsTri){
		case 0:
			y1=(mul3(GetTriY(StartVertex))>>2)-THMap[StartVertex]-AddTHMap(StartVertex);
			y2=(mul3(GetTriY(StartVertex+VertInLine))>>2)-THMap[StartVertex+VertInLine]-AddTHMap(StartVertex+VertInLine);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2)-THMap[StartVertex+VertInLine+1]-AddTHMap(StartVertex+VertInLine+1);
			break;
		case 1:
			y1=(mul3(GetTriY(StartVertex))>>2)-THMap[StartVertex]-AddTHMap(StartVertex);
			y2=(mul3(GetTriY(StartVertex+1))>>2)-THMap[StartVertex+1]-AddTHMap(StartVertex+1);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2)-THMap[StartVertex+VertInLine+1]-AddTHMap(StartVertex+VertInLine+1);
			break;
		case 2:
			y1=(mul3(GetTriY(StartVertex+1))>>2)-THMap[StartVertex+1]-AddTHMap(StartVertex+1);
			y2=(mul3(GetTriY(StartVertex+2))>>2)-THMap[StartVertex+2]-AddTHMap(StartVertex+2);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2)-THMap[StartVertex+VertInLine+1]-AddTHMap(StartVertex+VertInLine+1);
			break;
		case 3:
			y1=(mul3(GetTriY(StartVertex+2))>>2)-THMap[StartVertex+2]-AddTHMap(StartVertex+2);
			y2=(mul3(GetTriY(StartVertex+VertInLine+2))>>2)-THMap[StartVertex+VertInLine+2]-AddTHMap(StartVertex+VertInLine+2);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2)-THMap[StartVertex+VertInLine+1]-AddTHMap(StartVertex+VertInLine+1);
		};
	}else{
		switch(InsTri){
		case 0:
			y1=(mul3(GetTriY(StartVertex))>>2);
			y2=(mul3(GetTriY(StartVertex+VertInLine))>>2);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2);
			break;
		case 1:
			y1=(mul3(GetTriY(StartVertex))>>2);
			y2=(mul3(GetTriY(StartVertex+1))>>2);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2);
			break;
		case 2:
			y1=(mul3(GetTriY(StartVertex+1))>>2);
			y2=(mul3(GetTriY(StartVertex+2))>>2);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2);
			break;
		case 3:
			y1=(mul3(GetTriY(StartVertex+2))>>2);
			y2=(mul3(GetTriY(StartVertex+VertInLine+2))>>2);
			y3=(mul3(GetTriY(StartVertex+VertInLine+1))>>2);
		};
	};
	if(Minimax){
		if(y1>y2&&y1>y3)return y1;
		if(y2>y1&&y2>y3)return y2;
		return y3;
	}else{
		if(y1<y2&&y1<y3)return y1;
		if(y2<y1&&y2<y3)return y2;
		return y3;
	};
};
void VirtualScreen::CreateVerticalTrianglesMapping(int VertSet){
	if(VertSet>=MaxTMX)return;
	int pos=VertSet;
	for(int i=0;i<MaxTMY;i++){
		TriangMap[pos]=-1;
		LoTriMap[pos]=-1;
		pos+=MaxTMX;
	};
	int VStart=((VertSet&65534)<<1)+(MaxTH-2)*MaxTH*2;
	int y1,yind;
	if(VertSet&1){
		for(int ny=MaxTH-2;ny>=0;ny--){
			y1=GetMaxTriY(VStart,3,true);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)TriangMap[yind*MaxTMX+VertSet]=VStart+3;
			y1=GetMaxTriY(VStart,2,true);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)TriangMap[yind*MaxTMX+VertSet]=VStart+2;
			VStart-=MaxTH*2;
		};
		VStart+=MaxTH*2;
		for(ny=MaxTH-2;ny>=0;ny--){
			y1=GetMaxTriY(VStart,2,false);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)LoTriMap[yind*MaxTMX+VertSet]=VStart+2;
			y1=GetMaxTriY(VStart,3,false);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)LoTriMap[yind*MaxTMX+VertSet]=VStart+3;
			VStart+=MaxTH*2;
		};
	}else{
		for(int ny=MaxTH-2;ny>=0;ny--){
			y1=GetMaxTriY(VStart,0,true);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)TriangMap[yind*MaxTMX+VertSet]=VStart;
			y1=GetMaxTriY(VStart,1,true);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)TriangMap[yind*MaxTMX+VertSet]=VStart+1;
			VStart-=MaxTH*2;
		};
		VStart+=MaxTH*2;
		for(ny=MaxTH-2;ny>=0;ny--){
			y1=GetMaxTriY(VStart,1,false);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)LoTriMap[yind*MaxTMX+VertSet]=VStart+1;
			y1=GetMaxTriY(VStart,1,false);
			yind=div(y1,CellSY).quot;
			if(yind<MaxTMY&&yind>=0)LoTriMap[yind*MaxTMX+VertSet]=VStart;
			VStart+=MaxTH*2;
		};
	};
	Sequrity();
	//CheckSVSC(VertSet);
	//assert(_CrtCheckMemory());	
};
void VirtualScreen::CheckVLINE(int V){
	int v=V%MaxTMX;
	if(MarkedX[v]){
		CreateVerticalTrianglesMapping(v);
		MarkedX[v]=0;
	};
};
void UpdateDirtyPieces();

void VirtualScreen::Execute()
{
	UpdateDirtyPieces();
	void ProcessLightMap();
	ProcessLightMap();

	//  commented by Silver, 2.04.2004, because of changing of terrain caching
	//CopyVSToScreen();
};

void VirtualScreen::Zero(){
	memset(VirtualScreenPointer,0xCD,RealVLx*RealVLy);
	CreateTrianglesMapping();
};
int GetHiDiff(int xx,int yy){
	int x=(xx<<4)+8;
	int y=(yy<<4)+8;
	int hi1=abs(GetHeight(x-16,y)-GetHeight(x+16,y));
	int hi2=abs(GetHeight(x,y-16)-GetHeight(x,y+16));
	if(abs(hi1)>abs(hi2))return hi1;else return hi2;
};
int GetBigHiDiff(int xx,int yy){
	int x=(xx<<4)+8;
	int y=(yy<<4)+8;
	int hi1=abs(GetHeight(x-32,y)-GetHeight(x+32,y));
	int hi2=abs(GetHeight(x,y-32)-GetHeight(x,y+32));
	if(abs(hi1)>abs(hi2))return hi1;else return hi2;
};
void SetLockPoint(int xx,int yy){
	int ddif=GetHiDiff(xx,yy);
	if(ddif>14&&GetBigHiDiff(xx,yy)>14)BSetPt(xx,yy);
	else BClrPt(xx,yy);
};
void VirtualScreen::RefreshTriangle(int i){
	//assert(_CrtCheckMemory());
	int ost=i%4096;
	div_t ddt=div(i,MaxTH*2);
	int sx=ddt.rem>>1;
	MarkedX[sx]=1;
	div_t sxdt=div(sx,CellNX);
	int Miny=div(GetMaxTriY(i&0xFFFFFFFC,i&3,false),CellSY).quot;
	int Maxy=div(GetMaxTriY(i&0xFFFFFFFC,i&3,true ),CellSY).quot-Miny+1;
	if(Miny<0)Miny=0;
	div_t sydt=div(Miny,CellNY);
	sx=sxdt.rem;
	int ofst=sx+sydt.rem*CellNX;
	int maxo=NCells;
	for(int dsy=0;dsy<Maxy;dsy++){
		if(CellQuotX[ofst]==sxdt.quot&&CellQuotY[ofst]==sydt.quot){
			if(ofst>=0&&ofst<maxo)CellFlags[ofst]=0;
		};
		sydt.rem++;
		if(sydt.rem>=CellNY){
			ofst=sx;
			sydt.rem=0;
			sydt.quot++;
		}else ofst+=CellNX;
	};


	//assert(_CrtCheckMemory());
	//Locking
	/*if(!(i&1)){
		div_t qq=div(i>>1,MaxTH);
		int xx=qq.rem<<1;
		int yy=qq.quot<<1;
		if(qq.rem&1){
			SetLockPoint(xx,yy);
			SetLockPoint(xx+1,yy);
			SetLockPoint(xx,yy+1);
			SetLockPoint(xx+1,yy+1);
		}else{
			SetLockPoint(xx,yy+1);
			SetLockPoint(xx+1,yy+1);
			SetLockPoint(xx,yy+2);
			SetLockPoint(xx+1,yy+2);
		};
	};
	*/
};
int CheckPt(int x,int y);
int GetNP(int x,int y){
	int np=0;
	if(CheckPt(x-1,y))np++;
	if(CheckPt(x+1,y))np++;
	if(CheckPt(x,y-1))np++;
	if(CheckPt(x,y+1))np++;
	if(CheckPt(x-1,y-1))np++;
	if(CheckPt(x-1,y+1))np++;
	if(CheckPt(x+1,y-1))np++;
	if(CheckPt(x+1,y+1))np++;
	return np;
};
void PrepareLandLocking(){
	int maxx=MaxTH<<1;
	int xx,yy;
	for(int ix=0;ix<MaxTH;ix++)
		for(int iy=0;iy<MaxTH;iy++){
			xx=iy+iy;
			yy=ix+ix;
			if(CheckPt(xx,yy)){
				int np=GetNP(xx,yy);
				if(np<=3)BClrPt(xx,yy);
			};
		};
	for(ix=0;ix<MaxTH;ix++)
		for(int iy=0;iy<MaxTH;iy++){
			xx=iy+iy+1;
			yy=ix+ix;
			if(CheckPt(xx,yy)){
				int np=GetNP(xx,yy);
				if(np<=3)BClrPt(xx,yy);
			};
		};
	for(ix=0;ix<MaxTH;ix++)
		for(int iy=0;iy<MaxTH;iy++){
			xx=iy+iy;
			yy=ix+ix+1;
			if(CheckPt(xx,yy)){
				int np=GetNP(xx,yy);
				if(np<=3)BClrPt(xx,yy);
			};
		};
	for(ix=0;ix<MaxTH;ix++)
		for(int iy=0;iy<MaxTH;iy++){
			xx=iy+iy+1;
			yy=ix+ix+1;
			if(CheckPt(xx,yy)){
				int np=GetNP(xx,yy);
				if(np<=3)BClrPt(xx,yy);
			};
		};
};
void VirtualScreen::RefreshScreen(){
	memset(MarkedX,1,MaxTMX);
	memset(CellFlags,0,CellNX*CellNY);
	memset(CellQuotX,0,NCells);
	memset(CellQuotY,0,NCells);
	memset(CellFlags,0,NCells);

	//for(int i=0;i<MaxTH*MaxTH*2;i++)RefreshTriangle(i);
	//softing surface
	int maxx=MaxTH<<1;
	//PrepareLandLocking();
	//PrepareLandLocking();
};
void VirtualScreen::ShowVerticalGrids(int QuotX,int QuotY,int cx,int cy,int cly){

};
void VirtualScreen::Sequrity(){
	int* tt=(int*)(VirtualScreenPointer-(RealVLx<<1));
	int nn=RealVLx>>1;
	//for(int i=0;i<nn;i++)assert(tt[i]==-1);
	tt=(int*)(VirtualScreenPointer+RealVLx*RealVLy);
	//for(i=0;i<nn;i++)assert(tt[i]==-1);
};
int AddTHMap(int i){
	return (TexFlags[TexMap[i]]&8?0:word(randoma[word(i%8133)])&7);
};
