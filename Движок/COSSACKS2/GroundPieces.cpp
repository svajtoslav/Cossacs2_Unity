#include "stdheader.h"
void ReportCoorChange(int x,int y);
struct OneGPieceDesc{
	SprGroup* WhoIs;
	int Index;
};
class GPiecesDesc{
public:
	OneGPieceDesc* PDESC;
	int NSpr;
	int MaxSpr;
	int AddOneDesc(SprGroup* GRP,int id);
	GPiecesDesc();
	~GPiecesDesc();
};
void LoadAnmWater();
GPiecesDesc::GPiecesDesc(){
	memset(this,0,sizeof GPiecesDesc);
	LoadAnmWater();
};
GPiecesDesc::~GPiecesDesc(){
	if(PDESC)free(PDESC);
	memset(this,0,sizeof GPiecesDesc);
};
int GPiecesDesc::AddOneDesc(SprGroup* GRP,int id){
	if(NSpr>=MaxSpr){
		MaxSpr+=32;
		PDESC=(OneGPieceDesc*)realloc(PDESC,MaxSpr*sizeof OneGPieceDesc);
	};
	PDESC[NSpr].WhoIs=GRP;
	PDESC[NSpr].Index=id;
	NSpr++;
	return NSpr-1;
};
GPiecesDesc GPDESC;
int AddOneGroundType(SprGroup* GRP,int id){
	return GPDESC.AddOneDesc(GRP,id);
};
struct OneGPiece{
	byte x,y,Shadow;
	short z;
	word Index;
};
OneGPiece** GPMAP=NULL;
word* NInGPMAP=NULL;

int REFRLIST[16];
int NREFR=0;


void AddGPiece(int x,int y,word Type);
void InitSFPICT();

void InitGPMAP(){
	InitSFPICT();
	int sz=VAL_MAXCX*VAL_MAXCX;
	GPMAP=(OneGPiece**)realloc(GPMAP,sz<<2);
	memset(GPMAP,0,sz<<2);
	NInGPMAP=(word*)realloc(NInGPMAP,sz<<1);
	memset(NInGPMAP,0,sz<<1);
	/*
	for(int i=0;i<150;i++)
		for(int j=0;j<150;j++)
			AddGPiece(600+i*10+(rand()&15),600+j*10+(rand()&15),rand()%3);
	*/
};
void ClearAllGSprites(){
	int sz=VAL_MAXCX*VAL_MAXCX;
	for(int i=0;i<sz;i++)if(GPMAP[i])free(GPMAP[i]);
	memset(GPMAP,0,sz<<2);
	memset(NInGPMAP,0,sz<<1);
};
/*
void SaveGSprites(ResFile F){
	int sz=VAL_MAXCX*VAL_MAXCX;
	int N=0;
	int sz=8;
	for(int i=0;i<sz;i++)if(NInGPMAP[i]){
		sz+=6;
		sz+=NInGPMAP[i]*sizeof OneGPiece;
		N++;
	};
	RBlockWrite(F,&sz,4);
	RBlockWrite(F,&N,4);
	for(int i=0;i<sz;i++)if(NInGPMAP[i]){
		int x=i%VAL_MAXCX;
		int y=i/VAL_MAXCX;
		RBlockWrite(F,&x,2);
		RBlockWrite(F,&y,2);
		RBlockWrite(F,NInGPMAP+i,2);
		RBlockWrite(F,GPMAP+i,int(NInGPMAP[i])*sizeof OneGPiece;
	};
};
*/
void FreeSFPICT();
void ClearGPMAP(){
	int sz=VAL_MAXCX*VAL_MAXCX;

	memset(GPMAP,0,sz<<2);
	memset(NInGPMAP,0,sz<<1);
};
void freeGPMAP(){
	free(GPMAP);
	free(NInGPMAP);
	GPMAP=NULL;
	NInGPMAP=NULL;
	FreeSFPICT();
};
int GetLight(int x,int y);
int GetLight2(int x,int y);
void UpdatePieces(int cx,int cy){
#ifdef _USE3D
	void UpdateGSprites(int cx,int cy);
	UpdateGSprites(cx,cy);
#endif
	int cell=cx+(cy<<VAL_SHFCX);
	int NGPP=NInGPMAP[cell];
	OneGPiece* GPP=GPMAP[cell];
	GPMAP[cell]=NULL;
	NInGPMAP[cell]=0;
	if(NGPP){
		for(int i=0;i<NGPP;i++){
			int ys=((cy<<6)+GPP[i].y+GPP[i].z)<<1;
			AddGPiece((cx<<7)+int(GPP[i].x),ys,GPP[i].Index);
		};
		free(GPP);
	};
};

void UpdateAllPieces(){
	for(int ix=0;ix<VAL_MAXCX;ix++)
		for(int iy=0;iy<VAL_MAXCX;iy++)
			UpdatePieces(ix,iy);
};
void MarkSqToRedraw(int sx,int sy);
void AddGPiece(int x,int y,word Type){
	int z=GetHeight(x,y);
	int yr=(y>>1)-z;
	int xc=x>>7;
	int yc=yr>>6;
	int yyy0=yr&63;
	int xxx0=x&127;
	if(xc>=0&&xc<VAL_MAXCX-1&&yc>=0&&yc<VAL_MAXCX-1){
		int cell=xc+(yc<<VAL_SHFCX);
		int N=NInGPMAP[cell];
		if(N<65535){
			if(!GPMAP[cell]){
				GPMAP[cell]=new OneGPiece;
				NInGPMAP[cell]=0;
			}else GPMAP[cell]=(OneGPiece*)realloc(GPMAP[cell],(N+1)*sizeof(OneGPiece));
			OneGPiece* GP0=GPMAP[cell];
			for(int i=N-1;i>=0;i--){
				OneGPiece* GP1=GP0+i;
				int yy=GP1->y;
				//if(yy>yyy0||(yy==yyy0&&GP1->x>xxx0)){
				//	GP0[i+1]=GP0[i];
				//}else{
					GP1=GP0+i+1;
					GP1->Index=Type;
					GP1->x=x&127;
					GP1->y=yr&63;
					GP1->z=z;
					GP1->Shadow=0;//GetLight2(x,y);
					i=-2;
				//};
			};
			if(i==-1){
				GP0->Index=Type;
				GP0->x=x&127;
				GP0->y=yr&63;
				GP0->z=z;
				GP0->Shadow=0;//GetLight2(x,y);
			};
			NInGPMAP[cell]++;
			int cx=x>>5;
			int cy=yr>>5;
			for(int dx=-1;dx<=1;dx++)
				for(int dy=-1;dy<=1;dy++)
					MarkSqToRedraw(cx+dx,cy+dy);
		};
	};
};
void SETWINDX(int x,int y,int lx,int ly){
#ifndef _USE3D
	WindX=x;
	WindY=y;
	WindX1=x+lx-1;
	WindY1=y+ly-1;
	WindLx=WindX1-WindX+1;
	WindLy=WindY1-WindY+1;
#else
	GPS.SetClipArea( x, y, lx, ly );
#endif
};
void DrawMaskOnScreen(byte* Buf,int x,int y,int cx,int cy,int BufWidth);

void MakeDirtyPiece(int x,int y){
	int cy=((y>>1)-GetHeight(x,y))>>4;
	int cx=x>>5;
#ifdef _USE3D
	void MakeDirtyGBUF(int mx,int my,int sx,int sy);
	MakeDirtyGBUF((cx>>(5-Shifter))-3,(cy>>(5-Shifter))-4,7,9);
#endif
	if(NREFR>=16)return;
	cy>>=2;
	cx>>=2;
	if(cx>=0&&cy<VAL_MAXCX&&cy>=0&&cy<=VAL_MAXCX){
		int cell=cx+(cy<<VAL_SHFCX);
		for(int i=0;i<NREFR;i++)if(REFRLIST[i]==cell)return;
		REFRLIST[NREFR]=cell;
		NREFR++;
	};
};
void UpdateDirtyPieces(){
	if(NREFR==16)UpdateAllPieces();
	else for(int i=0;i<NREFR;i++){
		int c=REFRLIST[i];
		UpdatePieces(c%VAL_MAXCX,c/VAL_MAXCX);
	};
	NREFR=0;
};
//------------------point-primitives----------------//
struct OneSurfPicture{
	word OFFS[8];
	word SIZE[8];
	byte points[1];
};


OneSurfPicture** SFPICT=NULL;
void SetPixSpot(int x,int y,int R,int h0,int h1);
void InitSFPICT(){
	int sz=VAL_MAXCX*VAL_MAXCX;
	SFPICT=(OneSurfPicture**)realloc(SFPICT,sz<<2);
	memset(SFPICT,0,sz<<2);
	//for(int i=0;i<30;i++)SetPixSpot(200+i*5,100+i*3,16,200,0);
};
void FreeSFPICT(){
	if(SFPICT){
		int sz=VAL_MAXCX*VAL_MAXCX;
		for(int i=0;i<sz;i++)if(SFPICT[i])free(SFPICT[i]);
		free(SFPICT);
		SFPICT=NULL;
	};
};
void ClearSFPICT(){
	if(SFPICT){
		int sz=VAL_MAXCX*VAL_MAXCX;
		for(int i=0;i<sz;i++)if(SFPICT[i]){
			free(SFPICT[i]);
			SFPICT[i]=NULL;
		};
	};
};
int DefPixel=0;
void UnpackFromBuf(byte* Pix,byte* ptr);
bool GetPixSquare(int cx,int cy,byte* Pix){
	int sx=cx>>2;
	int sy=cy>>1;
	if(sx>=0&&sx<VAL_MAXCX&&sy>=0&&sy<VAL_MAXCX){
		int cell=sx+(sy<<VAL_SHFCX);
		OneSurfPicture* OSP=SFPICT[cell];
		if(OSP){
			word ofs=OSP->OFFS[(cx&3)+((cy&1)<<2)];
			if(ofs){
				byte* ptr=OSP->points+ofs-32;
				UnpackFromBuf(Pix,OSP->points+ofs-32);
				return true;
			}else{ 
				memset(Pix,0,1024);
				return false;
			};
		}else{
			memset(Pix,0,1024);
			return false;
		};
	};
	return false;
};
int GetPackPixSquare(int cx,int cy,byte* Pix){
	int sx=cx>>2;
	int sy=cy>>1;
	if(sx>=0&&sx<VAL_MAXCX&&sy>=0&&sy<VAL_MAXCX){
		int cell=sx+(sy<<VAL_SHFCX);
		OneSurfPicture* OSP=SFPICT[cell];
		if(OSP){
			word ofs=OSP->OFFS[(cx&3)+((cy&1)<<2)];
			if(ofs){
				int sz=OSP->SIZE[(cx&3)+((cy&1)<<2)];
				if(Pix)memcpy(Pix,OSP->points+ofs-32,sz);
				return sz;
			}else return 0;
		}else return 0;
	};
	return 0;
};
void SetPackPixSquare(int cx,int cy,byte* Pix,int size){
	int sx=cx>>2;
	int sy=cy>>1;
	if(sx>=0&&sx<VAL_MAXCX&&sy>=0&&sy<VAL_MAXCX){
		int cell=sx+(sy<<VAL_SHFCX);
		OneSurfPicture* OSP=SFPICT[cell];
		OneSurfPicture TempOSP;

		if(!OSP){
			OSP=&TempOSP;
			memset(OSP,0,sizeof TempOSP);
		};
		int intcell=(cx&3)+((cy&1)<<2);
		byte IntBuf[8192];
		int ibpos=0;
		int oldsize=0;
		for(int i=0;i<8;i++){
			if(i!=intcell){
				if(OSP->OFFS[i]){
					memcpy(IntBuf+ibpos,OSP->points+OSP->OFFS[i]-32,OSP->SIZE[i]);
					OSP->OFFS[i]=32+ibpos;
					ibpos+=OSP->SIZE[i];
					oldsize+=OSP->SIZE[i];
				};
			}else{
				if(size){
					memcpy(IntBuf+ibpos,Pix,size);
					oldsize+=OSP->SIZE[i];
					OSP->OFFS[i]=32+ibpos;
					OSP->SIZE[i]=size;
				}else{
					oldsize+=OSP->SIZE[i];
					OSP->OFFS[i]=0;
					OSP->SIZE[i]=0;
				};
				ibpos+=size;
			};
		};
		if(ibpos>oldsize){
			if(OSP!=SFPICT[cell]){
				SFPICT[cell]=(OneSurfPicture*)realloc(SFPICT[cell],32+ibpos);
				memcpy(SFPICT[cell],OSP,32);
			}else{
				SFPICT[cell]=(OneSurfPicture*)realloc(SFPICT[cell],32+ibpos);
			};
			OSP=SFPICT[cell];
		};
		if(!ibpos){
			if(SFPICT[cell])free(SFPICT[cell]);
			SFPICT[cell]=NULL;
			return;
		};
		memcpy(OSP->points,IntBuf,ibpos);
	};
};
int PackIntoBuf(byte* Pix,byte* Buf){
	bool mod=0;
	int ofs=0;
	int dofs=0;
	bool npt=0;
	do{
		if(mod){
			int L=0;
			while(Pix[ofs+L]&&L<254){
				npt=1;
				L++;
			};
			if(L&1)L++;
			L>>=1;
			Buf[dofs]=L;
			dofs++;
			for(int i=0;i<L;i++){
				Buf[dofs]=(Pix[ofs]>>4)+(Pix[ofs+1]&0xF0);
				ofs+=2;
				dofs++;
			};
			mod=0;
		}else{
			int L=0;
			while(!Pix[ofs+L]&&L<254)L++;
			ofs+=L;
			Buf[dofs]=L;
			dofs++;
			mod=1;
		};
	}while(ofs<1024);
	if(npt)return dofs;
	else return 0;
};
void UnpackFromBuf(byte* Pix,byte* ptr){
	int ofs=0;
	bool FreeMode=1;
	do{
		byte L=ptr[0];
		ptr++;
		if(FreeMode){
			if(ofs+L>1024)L=1024-ofs;
			memset(Pix+ofs,DefPixel,L);
			ofs+=L;
			FreeMode=0;
		}else{
			for(int i=0;i<L;i++){
				byte B=ptr[i];
				if(ofs<1024)Pix[ofs]=(B&15)<<4;
				if(ofs<1023)Pix[ofs+1]=B&0xF0;
				ofs+=2;
			};
			FreeMode=1;
			ptr+=i;
		};
	}while(ofs<1024);
	/*
	for(int ix=0;ix<15;ix++){
		for(int iy=0;iy<15;iy++){
			int ofs=ix+ix+((iy+iy)<<5);
			int c0=char(Pix[ofs]);
			int c1=char(Pix[ofs+2]);
			int c2=char(Pix[ofs+64]);
			int c3=char(Pix[ofs+66]);
			Pix[ofs+1]=(c0+c1)>>1;
			Pix[ofs+32]=(c0+c2)>>1;
			Pix[ofs+34]=(c1+c3)>>1;
			Pix[ofs+65]=(c2+c3)>>1;
			Pix[ofs+33]=(c0+c1+c2+c3)>>2;
		};
	};
	*/
};
void SetPixSquare(int cx,int cy,byte* Pix){
	int sx=cx>>2;
	int sy=cy>>1;
	if(sx>=0&&sx<VAL_MAXCX&&sy>=0&&sy<VAL_MAXCX){
		int cell=sx+(sy<<VAL_SHFCX);
		OneSurfPicture* OSP=SFPICT[cell];
		OneSurfPicture TempOSP;

		if(!OSP){
			OSP=&TempOSP;
			memset(OSP,0,sizeof TempOSP);
		};
		int intcell=(cx&3)+((cy&1)<<2);
		byte IntBuf[8192];
		int ibpos=0;
		int oldsize=0;
		for(int i=0;i<8;i++){
			if(i!=intcell){
				if(OSP->OFFS[i]){
					memcpy(IntBuf+ibpos,OSP->points+OSP->OFFS[i]-32,OSP->SIZE[i]);
					OSP->OFFS[i]=32+ibpos;
					ibpos+=OSP->SIZE[i];
					oldsize+=OSP->SIZE[i];
				};
			}else{
				int sz=PackIntoBuf(Pix,IntBuf+ibpos);
				if(sz){
					oldsize+=OSP->SIZE[i];
					OSP->OFFS[i]=32+ibpos;
					OSP->SIZE[i]=sz;
				}else{
					oldsize+=OSP->SIZE[i];
					OSP->OFFS[i]=0;
					OSP->SIZE[i]=0;
				};
				ibpos+=sz;
			};
		};
		if(ibpos>oldsize){
			if(OSP!=SFPICT[cell]){
				SFPICT[cell]=(OneSurfPicture*)realloc(SFPICT[cell],32+ibpos);
				memcpy(SFPICT[cell],OSP,32);
			}else{
				SFPICT[cell]=(OneSurfPicture*)realloc(SFPICT[cell],32+ibpos);
			};
			OSP=SFPICT[cell];
		};
		if(!ibpos){
			if(SFPICT[cell])free(SFPICT[cell]);
			SFPICT[cell]=NULL;
			return;
		};
		memcpy(OSP->points,IntBuf,ibpos);
	};
};

struct OnePixUndoSegment{
	int cx,cy;
	bool First;
	int size;
	byte data[1];
};
OnePixUndoSegment** PixUndoBuf=NULL;
int NPixUndoSegments=0;
int MaxPixUndoSegments=0;
int UndoSize=0;
void ClearPixUndoStack(){
	for(int i=0;i<NPixUndoSegments;i++){
		free(PixUndoBuf[i]);
		PixUndoBuf[i]=NULL;
	};
	NPixUndoSegments=0;
};
void AddPixUndoSegment(int x,int y,bool first){
	if(NPixUndoSegments>=MaxPixUndoSegments){
		MaxPixUndoSegments+=64;
		PixUndoBuf=(OnePixUndoSegment**)realloc(PixUndoBuf,4*MaxPixUndoSegments);
	};
	byte pbuf[1024];
	int size=GetPackPixSquare(x,y,pbuf);
	PixUndoBuf[NPixUndoSegments]=(OnePixUndoSegment*)malloc(sizeof(OnePixUndoSegment)-1+size);
	memcpy(PixUndoBuf[NPixUndoSegments]->data,pbuf,size);
	PixUndoBuf[NPixUndoSegments]->cx=x;
	PixUndoBuf[NPixUndoSegments]->cy=y;
	PixUndoBuf[NPixUndoSegments]->First=first;
	PixUndoBuf[NPixUndoSegments]->size=size;
	NPixUndoSegments++;
	UndoSize+=size;
};
void PerformPixUndo(){
	bool doit=1;
	do{
		if(NPixUndoSegments){
			SetPackPixSquare(PixUndoBuf[NPixUndoSegments-1]->cx,PixUndoBuf[NPixUndoSegments-1]->cy,
				PixUndoBuf[NPixUndoSegments-1]->data,PixUndoBuf[NPixUndoSegments-1]->size);
			UndoSize-=PixUndoBuf[NPixUndoSegments-1]->size;
			int sx=PixUndoBuf[NPixUndoSegments-1]->cx;
			int sy=PixUndoBuf[NPixUndoSegments-1]->cy;
			SVSC.MarkedX[sx]=1;
			div_t sxdt=div(sx,SVSC.CellNX);
			int Miny=(sy<<5)/SVSC.CellSY;
			if(Miny<0)Miny=0;
			div_t sydt=div(Miny,SVSC.CellNY);
			sx=sxdt.rem;
			int ofst=sx+sydt.rem*SVSC.CellNX;
			int maxo=SVSC.NCells;
			if(SVSC.CellQuotX[ofst]==sxdt.quot&&SVSC.CellQuotY[ofst]==sydt.quot){
				if(ofst>=0&&ofst<maxo)SVSC.CellFlags[ofst]=0;
			};
			NPixUndoSegments--;
			if(PixUndoBuf[NPixUndoSegments]->First)doit=0;
			free(PixUndoBuf[NPixUndoSegments]);
		};
		
	}while(NPixUndoSegments&&doit);
};
byte FuncPrecomp[1024];
int CurR=0;
void CheckPrecomp(int R){
	if(R!=CurR){
		memset(FuncPrecomp,0,1024);
		int RR=R<<4;
		int RR2=RR*RR;
		for(int i=0;i<RR;i++){
			int c=(255*(RR-i))/RR;
			if(c<0)c=0;
			if(c>255)c=255;
			FuncPrecomp[i]=c;
		};
	};
	CurR=R;
};
extern byte* CurPixTexture;
extern int NTexPages;
int ConvScrY(int x,int y);
int Prop43(int y);
int GetTexStartOfs(int x,int ys){
	if(NTexPages<3)return 0;
	int yy=ConvScrY(x,ys);
	int y=Prop43(yy);
	int HR=GetHeight(x+32,y);
	int HL=GetHeight(x-32,y);
	int HU=GetHeight(x,y-32);
	int HD=GetHeight(x,y+32);
	if(HR==HL&&HU==HD)return 0;
	int nx=HL-HR;
	int ny=abs(HD-HU);
	float ang=atan2(float(nx),float(ny));
	int N=NTexPages>>1;
	float ang0=3.1415/NTexPages/2.0;
	if(ang>0){
		int pag=(ang+ang0)/(2*ang0);
		if(pag<0)pag=0;
		if(pag>N)pag=N;
		return pag!=0?pag<<16:0;
	}else{
		int pag=(-ang+ang0)/(2*ang0);
		if(pag<0)pag=0;
		if(pag>N)pag=N;
		return pag!=0?(N+pag)<<16:0;
	};
};
int TexStartDX=0;
int TexStartDY=0;
int GDX[8]={0,2,4,0,2,4,0,2};
int GDY[8]={4,4,4,2,2,2,0,0};
void SetPixSpot(int x,int y,int R,int h0,int h1){
	if(R>63)R=63;
	CheckPrecomp(R);
	int cx0=(x-R)>>5;
	int cy0=(y-R)>>5;
	int cx1=(x+R+1)>>5;
	int cy1=(y+R+1)>>5;
	byte Buf[1200];
	byte Buf0[1200];
	bool first=1;
	int stofs=GetTexStartOfs(x,y);
	for(int vx=cx0;vx<=cx1;vx++){
		for(int vy=cy0;vy<cy1;vy++){
			int pixx0=((vx+GDX[MyNation])<<5)&255;
			int pixy0=((vy+GDY[MyNation])<<5)&255;
			GetPixSquare(vx,vy,Buf);
			memcpy(Buf0,Buf,1024);
			int x0=vx<<5;
			int y0=vy<<5;
			int ofs=0;
			bool change=0;
			for(int iy=0;iy<32;iy++){
				for(int ix=0;ix<32;ix++){
					int dx=x-x0-ix;
					int dy=y-y0-iy;
					int r=sqrt(dx*dx+dy*dy);
					
					if(r<R){
						int c=h1-(((h1-h0)*FuncPrecomp[r<<4])>>8)>>1;
						if(c<0)c=0;
						if(c>127)c=127;
						if(CurPixTexture){
							if(h1==255){
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<0&&cci<-c){
									if(c<16)Buf[ofs]=0;
									else Buf[ofs]=-c;
									change=1;
								}else if(cci>0&&cci>c){
									Buf[ofs]=c;
									change=1;
								};
							}else{
								char cc=Buf[ofs];
								int cci=cc;
								int cpx=((-CurPixTexture[stofs+pixx0+ix+((pixy0+iy)<<8)]+127)*c)>>7;
								if(cpx<0&&cci>cpx){
									Buf[ofs]=cpx;
									change=1;
								}else if(cpx>0&&cci<cpx){
									Buf[ofs]=cpx;
									change=1;
								};
							};
						}else{
							if(h1==255){
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<0&&cci<-c){
									if(c<16)Buf[ofs]=0;
									else Buf[ofs]=-c;
									change=1;
								}else if(cci>0&&cci>c){
									Buf[ofs]=c;
									change=1;
								};
							}else{
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<c){
									Buf[ofs]=c;
									change=1;
								};
							};
						};
					};
					ofs++;
				};
			};
			if(change){
				AddPixUndoSegment(vx,vy,first);
				first=0;
				SetPixSquare(vx,vy,Buf);
				int sx=vx;
				int sy=vy;
				SVSC.MarkedX[sx]=1;
				div_t sxdt=div(sx,SVSC.CellNX);
				int Miny=(sy<<5)/SVSC.CellSY;
				if(Miny<0)Miny=0;
				div_t sydt=div(Miny,SVSC.CellNY);
				sx=sxdt.rem;
				int ofst=sx+sydt.rem*SVSC.CellNX;
				int maxo=SVSC.NCells;
				if(SVSC.CellQuotX[ofst]==sxdt.quot&&SVSC.CellQuotY[ofst]==sydt.quot){
					if(ofst>=0&&ofst<maxo)SVSC.CellFlags[ofst]=0;
				};
			};
		};
	};
};
void SetPixLine(int x0,int y0,int x1,int y1,int R,int h0,int h1){
	if(R>63)R=63;
	CheckPrecomp(R);
	int stofs=GetTexStartOfs(x0,y0);
	int cx0,cx1,cy0,cy1;
	if(x1<x0){
		cx0=(x1-R)>>5;
		cx1=(x0+R)>>5;
	}else{
		cx0=(x0-R)>>5;
		cx1=(x1+R)>>5;
	};
	if(y1<y0){
		cy0=(y1-R)>>5;
		cy1=(y0+R)>>5;
	}else{
		cy0=(y0-R)>>5;
		cy1=(y1+R)>>5;
	};
	byte Buf[1200];
	byte Buf0[1200];

	float Dx=x1-x0;
	float Dy=y1-y0;
	float D=Dx*Dx+Dy*Dy;

	if(!D){
		SetPixSpot(x0,y0,R,h0,h1);
		return;
	};
	float fx0=x0;
	float fx1=x1;
	float fy0=y0;
	float fy1=y1;
	float fR=R;
	float fh0=h0;
	float fh1=h1;
	float fr4=fR*fR*fR*fR;
	bool first=1;
	for(int vx=cx0;vx<=cx1;vx++){
		for(int vy=cy0;vy<=cy1;vy++){
			int pixx0=((vx+GDX[MyNation])<<5)&255;
			int pixy0=((vy+GDY[MyNation])<<5)&255;
			GetPixSquare(vx,vy,Buf);
			memcpy(Buf0,Buf,1024);
			int xx0=vx<<5;
			int yy0=vy<<5;
			int ofs=0;
			bool change=0;
			for(int iy=0;iy<32;iy++){
				for(int ix=0;ix<32;ix++){
					float x=xx0+ix;
					float y=yy0+iy;
					float N=(((x-x0)*Dx+(y-y0)*Dy))/D;
					float r;
					if(N<0)r=sqrt((x-fx0)*(x-fx0)+(y-fy0)*(y-fy0));
					else if(N>1)r=sqrt((x-fx1)*(x-fx1)+(y-fy1)*(y-fy1));
					else{
						r=(Dy*(x-fx0)-Dx*(y-fy0))/sqrt(D);
						if(r<0)r=-r;
					};
					if(r<R){
						float DFF=(fR-r)*(fR-r);
						int c=(h1-(((h1-h0)*FuncPrecomp[int(r*16)])>>8))>>1;
						if(c<0)c=0;
						if(c>127)c=127;
						if(CurPixTexture){
							if(h1==255){
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<0&&cci<-c){
									if(c<16)Buf[ofs]=0;
									else Buf[ofs]=-c;
									change=1;
								}else if(cci>0&&cci>c){
									Buf[ofs]=c;
									change=1;
								};
							}else{
								char cc=Buf[ofs];
								int cci=cc;
								int cpx=((-CurPixTexture[stofs+pixx0+ix+((pixy0+iy)<<8)]+127)*c)>>7;
								if(cpx<0&&cci>cpx){
									Buf[ofs]=cpx;
									change=1;
								}else if(cpx>0&&cci<cpx){
									Buf[ofs]=cpx;
									change=1;
								};
							};
						}else{
							if(h1==255){
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<0&&cci<-c){
									if(c<16)Buf[ofs]=0;
									else Buf[ofs]=-c;
									change=1;
								}else if(cci>0&&cci>c){
									Buf[ofs]=c;
									change=1;
								};
							}else{
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<c){
									Buf[ofs]=c;
									change=1;
								};
							};
						};
					};
					ofs++;
				};
			};
			if(change){
				AddPixUndoSegment(vx,vy,first);
				first=0;
				SetPixSquare(vx,vy,Buf);
				int sx=vx;
				int sy=vy;
				SVSC.MarkedX[sx]=1;
				div_t sxdt=div(sx,SVSC.CellNX);
				int Miny=(sy<<5)/SVSC.CellSY;
				if(Miny<0)Miny=0;
				div_t sydt=div(Miny,SVSC.CellNY);
				sx=sxdt.rem;
				int ofst=sx+sydt.rem*SVSC.CellNX;
				int maxo=SVSC.NCells;
				if(SVSC.CellQuotX[ofst]==sxdt.quot&&SVSC.CellQuotY[ofst]==sydt.quot){
					if(ofst>=0&&ofst<maxo)SVSC.CellFlags[ofst]=0;
				};
			};
		};
	};
};
void SetPixLine(int x0,int y0,int x1,int y1,int R,int R1,int h0,int h1,int h01){
	if(R<1)R=1;
	if(R1<1)R1=1;
	if(R>63)R=63;
	if(R1>63)R1=63;
	float MaxR=R1>R?R1:R;
	int maxh=h0>h01?h0:h01;
	CheckPrecomp(R);
	int stofs=GetTexStartOfs(x0,y0);
	int cx0,cx1,cy0,cy1;
	if(x1<x0){
		cx0=(x1-R)>>5;
		cx1=(x0+R)>>5;
	}else{
		cx0=(x0-R)>>5;
		cx1=(x1+R)>>5;
	};
	if(y1<y0){
		cy0=(y1-R)>>5;
		cy1=(y0+R)>>5;
	}else{
		cy0=(y0-R)>>5;
		cy1=(y1+R)>>5;
	};
	byte Buf[1200];
	byte Buf0[1200];

	float Dx=x1-x0;
	float Dy=y1-y0;
	float D=Dx*Dx+Dy*Dy;

	if(!D){
		SetPixSpot(x1,y1,MaxR,maxh,h1);
		return;
	};
	float fx0=x0;
	float fx1=x1;
	float fy0=y0;
	float fy1=y1;
	float fR=R;
	float fh0=h0;
	float fh1=h1;
	float fr4=fR*fR*fR*fR;
	bool first=1;
	for(int vx=cx0;vx<=cx1;vx++){
		for(int vy=cy0;vy<=cy1;vy++){
			int pixx0=((vx+GDX[MyNation])<<5)&255;
			int pixy0=((vy+GDY[MyNation])<<5)&255;
			GetPixSquare(vx,vy,Buf);
			memcpy(Buf0,Buf,1024);
			int xx0=vx<<5;
			int yy0=vy<<5;
			int ofs=0;
			bool change=0;
			for(int iy=0;iy<32;iy++){
				for(int ix=0;ix<32;ix++){
					float x=xx0+ix;
					float y=yy0+iy;
					float N=(((x-x0)*Dx+(y-y0)*Dy))/D;
					float r;
					if(N<0){
						r=sqrt((x-fx0)*(x-fx0)+(y-fy0)*(y-fy0));
						N=0;
					}else if(N>1){
						r=sqrt((x-fx1)*(x-fx1)+(y-fy1)*(y-fy1));
						N=1;
					}else{
						r=(Dy*(x-fx0)-Dx*(y-fy0))/sqrt(D);
						if(r<0)r=-r;
					};
					if(r<MaxR){
						float DFF=(fR-r)*(fR-r);
						int h=N*h01+(1-N)*h0;
						int RR=int(r*16*R/(N*R1+(1-N)*R));
						int c=(h1-(((h1-h)*FuncPrecomp[RR])>>8))>>1;
						if(c<0)c=0;
						if(c>127)c=127;
						if(CurPixTexture){
							if(h1==255){
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<0&&cci<-c){
									if(c<16)Buf[ofs]=0;
									else Buf[ofs]=-c;
									change=1;
								}else if(cci>0&&cci>c){
									Buf[ofs]=c;
									change=1;
								};
							}else{
								char cc=Buf[ofs];
								int cci=cc;
								int cpx=((-CurPixTexture[stofs+pixx0+ix+((pixy0+iy)<<8)]+127)*c)>>7;
								if(cpx<0&&cci>cpx){
									Buf[ofs]=cpx;
									change=1;
								}else if(cpx>0&&cci<cpx){
									Buf[ofs]=cpx;
									change=1;
								};
							};
						}else{
							if(h1==255){
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<0&&cci<-c){
									if(c<16)Buf[ofs]=0;
									else Buf[ofs]=-c;
									change=1;
								}else if(cci>0&&cci>c){
									Buf[ofs]=c;
									change=1;
								};
							}else{
								char cc=Buf[ofs];
								int cci=cc;
								if(cci<c){
									Buf[ofs]=c;
									change=1;
								};
							};
						};
					};
					ofs++;
				};
			};
			if(change){
				AddPixUndoSegment(vx,vy,first);
				first=0;
				SetPixSquare(vx,vy,Buf);
				int sx=vx;
				int sy=vy;
				SVSC.MarkedX[sx]=1;
				div_t sxdt=div(sx,SVSC.CellNX);
				int Miny=(sy<<5)/SVSC.CellSY;
				if(Miny<0)Miny=0;
				div_t sydt=div(Miny,SVSC.CellNY);
				sx=sxdt.rem;
				int ofst=sx+sydt.rem*SVSC.CellNX;
				int maxo=SVSC.NCells;
				if(SVSC.CellQuotX[ofst]==sxdt.quot&&SVSC.CellQuotY[ofst]==sydt.quot){
					if(ofst>=0&&ofst<maxo)SVSC.CellFlags[ofst]=0;
				};
			};
		};
	};
};
extern byte BrFog[8192];
extern byte InvBrFog[8192];
void DrawSubWaterSquare(byte* Buf,int x,int y,int cx,int cy,int BufWidth);
void DrawOnePixCell(byte* Buf,int x,int y,int cx,int cy,int BufWidth){
	DrawMaskOnScreen(Buf,x,y,cx,cy,BufWidth);
	DrawSubWaterSquare(Buf,x,y,cx,cy,BufWidth);
	byte TMP[1200];
	if(!GetPixSquare(cx,cy,TMP))return;
	TempWindow TW;
	PushWindow(&TW);
	int ww=ScrWidth;
	void* ptr=ScreenPtr;
	ScreenPtr=Buf;
	ScrWidth=BufWidth;
	int ofs0=int(Buf)+x+(y*BufWidth);
	int DDX=BufWidth-32;
	int tofs=int(TMP);
	__asm{
		push esi
		push edi
		mov  esi,tofs
		mov  edi,ofs0
		mov	 cx,0x2020
		xor  eax,eax
DOPC1:	lodsb
		mov  ah,al
		shr  eax,3
		mov  al,[edi]
		mov  bl,[BrFog+eax]
		mov  [edi],bl
		inc  edi
		dec  cl
		jnz  DOPC1
		add  edi,DDX
		mov  cl,0x20
		dec  ch
		jnz  DOPC1
		pop  edi
		pop  esi
	};
	PopWindow(&TW);
	ScreenPtr=ptr;
};
#define ABPTR(b) if(size+1>MaxSize){MaxSize+=16384;*ptr=(byte*)realloc(*ptr,MaxSize);};(*ptr)[size]=b;size++;
#define AWPTR(b) if(size+2>MaxSize){MaxSize+=16384;*ptr=(byte*)realloc(*ptr,MaxSize);};((word*)((*ptr)+size))[0]=b;size+=2;
#define ALPTR(b) if(size+4>MaxSize){MaxSize+=16384;*ptr=(byte*)realloc(*ptr,MaxSize);};((DWORD*)((*ptr)+size))[0]=b;size+=4;
#define ABUFPTR(b,sz) if(size+sz>MaxSize){MaxSize+=16384+sz;*ptr=(byte*)realloc(*ptr,MaxSize);};memcpy((*ptr)+size,b,sz);size+=sz;
int GetPixDataToSave(byte** ptr){
	int size=0;
	int MaxSize=0;
	*ptr=NULL;
	int N=VAL_MAXCX*VAL_MAXCX;
	int np=0;
	for(int i=0;i<N;i++)if(SFPICT[i]){
		np++;
	};
	ALPTR(np);
	for(i=0;i<N;i++)if(SFPICT[i]){
		int sz=0;
		for(int j=0;j<8;j++){
			sz+=SFPICT[i]->SIZE[j];
		};
		ALPTR(i);
		AWPTR(sz+32);
		ABUFPTR(SFPICT[i],sz+32);
	};
	return size;
};
void SetPixDataFromSave(byte* ptr){
	ClearPixUndoStack();
	ClearSFPICT();
	int np=((DWORD*)ptr)[0];
	ptr+=4;
	for(int i=0;i<np;i++){
		int p,sz;
		p=((DWORD*)ptr)[0];
		ptr+=4;
		sz=((WORD*)ptr)[0];
		ptr+=2;
		SFPICT[p]=(OneSurfPicture*)malloc(sz);
		memcpy(SFPICT[p],ptr,sz);
		ptr+=sz;
	};
};
//------------------------APPLICATIONS-------------------//
//1.Трещина
int rfnbr(int x){
	int rr=x*((64+(rand()&127))>>6);
	if(rr>255)rr=255;
	return rr;
};
void DrawHole_R0(int x,int y,int dir,int L,int br0,int N){
	if(N>10)return;
	bool DO=rand()<4096;
	int br1=br0*(256+((int(rand())*80)>>15)-70)>>8;
	if(DO)br1=rfnbr(br1);
	if(br1>255)br1=255;
	int yg0=(y>>1)-GetHeight(x,y);
	byte DD=dir;
	int x1=x+((int(TCos[DD])*L)>>8);
	int y1=y+((int(TSin[DD])*L)>>8);
	int yg1=(y1>>1)-GetHeight(x1,y1);
	SetPixLine(x,yg0,x1,yg1,br0/40,br1/40,(br0+190)>>1,0,(br1+190)>>1);
	if(br1>10){
		//DrawHole_R0(x1,y1,dir+(rand()&31)-15,L,br1);
		if(DO){
			DrawHole_R0(x1,y1,dir+(rand()&127)-64,L,br1,N+1);
			DrawHole_R0(x1,y1,dir+(rand()&127)-64,L,br1,N+1);
		}else DrawHole_R0(x1,y1,dir+(rand()&31)-15,L,br1,N+1);
	};
};
void DrawHole(int x,int y){
	int H0=GetHeight(x,y);
	int dir=rand()&255;
	for(int i=0;i<32;i++){
		int x1=x+(TCos[i<<3]>>3);
		int y1=y+(TSin[i<<3]>>3);
		int H1=GetHeight(x1,y1);
		if(H1<H0){
			dir=i<<3;
			H0=H1;
		};
	};
	DrawHole_R0(x,y,dir,5,100,0);
	DrawHole_R0(x,y,dir+128,5,100,0);
};
byte* CurPixTexture=NULL;
int NTexPages=1;
void LoadCurPixTexture(char* Name){
	if(CurPixTexture){
		free(CurPixTexture);
		CurPixTexture=NULL;
		NTexPages=0;
	};
	BMPformat BM;
	if(ReadBMP8(Name,&BM,&CurPixTexture)){
		if(BM.biWidth==256){
			int ost=BM.biHeight&255;
			int L=BM.biHeight>>8;
			NTexPages=L;
			if(ost||!(L&1)){
				free(CurPixTexture);
				CurPixTexture=NULL;
				NTexPages=0;
			};
		};
		//if(BM.biHeight==256*5)NTexPages=1;
		//else NTexPages=0;
	};
};
//--------------------Saving pieces-------------------
int* TempZBuffer=NULL;
int TZLX=0;
int TZLY=0;
int TZSH=0;
void AllocTZBuf(){
	int Ly=256<<ADDSH;

	TZLX=Ly<<1;
	TZLY=Ly;
	TZSH=9+ADDSH;
	TempZBuffer=znew(int,Ly*Ly*2);
	memset(TempZBuffer,0,Ly*Ly*2);
};
void FreeTZBuf(){
	if(TempZBuffer){
		free(TempZBuffer);
		TempZBuffer=NULL;
		TZLX=0;
		TZLY=0;
		TZSH=0;
	};
};
int GetTZValue(int ix,int iy){
	if(TempZBuffer){
		if(ix<0)ix=0;
		if(iy<0)iy=0;
		if(ix>TZLX-1)ix=TZLX-1;
		if(iy>TZLY-1)iy=TZLY-1;
		return TempZBuffer[ix+(iy<<TZSH)];
	}else return 0;
};
void SetTZValue(int ix,int iy,int v){
	if(TempZBuffer){
		if(ix<0)ix=0;
		if(iy<0)iy=0;
		if(ix>TZLX-1)ix=TZLX-1;
		if(iy>TZLY-1)iy=TZLY-1;
		TempZBuffer[ix+(iy<<TZSH)]=v;
	};
};
int GetPixelsIntoBuf(int x,int y,int x1,int y1,int xr,int yr,byte** ptr){
	int size=0;
	int MaxSize=0;
	*ptr=NULL;
	int NP=0;
	for(int ix=x;ix<=x1;ix++){
		for(int iy=y;iy<=y1;iy++){
			//if(GetPackPixSquare(ix,iy,NULL))
			NP++;
		};
	};
	ALPTR(NP);
	byte Buf[1024];
	int z00=yr<<6;
	for(ix=x;ix<=x1;ix++){
		for(int iy=y;iy<=y1;iy++){
			int sz=GetPackPixSquare(ix,iy,Buf);
			int z0=Prop43(ConvScrY(ix<<5,iy<<5))-z00;
			int z1=Prop43(ConvScrY((ix+1)<<5,iy<<5))-z00;
			int z2=Prop43(ConvScrY(ix<<5,(iy+1)<<5))-z00;
			int z3=Prop43(ConvScrY((ix+1)<<5,(iy+1)<<5))-z00;
			AWPTR(ix-xr);
			AWPTR(iy-yr);

			ALPTR(z0);
			ALPTR(z1);
			ALPTR(z2);
			ALPTR(z3);

			AWPTR(sz);
			ABUFPTR(Buf,sz);
		};
	};
	return size;
};
void SetPixelsFromBuf(int xr,int yr,byte* Buf,int size){
	if(!TempZBuffer)AllocTZBuf();
	int N=((DWORD*)Buf)[0];
	Buf+=4;
	byte* BUF0=Buf;
	for(int i=0;i<N;i++){
		int cx=xr+((short*)Buf)[0];
		Buf+=2;
		int cy=yr+((short*)Buf)[0];
		Buf+=2;

		int z0=((int*)Buf)[0]+(yr<<6);
		Buf+=4;
		int z1=((int*)Buf)[0]+(yr<<6);
		Buf+=4;
		int z2=((int*)Buf)[0]+(yr<<6);
		Buf+=4;
		int z3=((int*)Buf)[0]+(yr<<6);
		Buf+=4;

		int sz=((short*)Buf)[0];
		Buf+=2;
		int zp0=GetTZValue(cx,cy);
		int zp1=GetTZValue(cx+1,cy);
		int zp2=GetTZValue(cx,cy+1);
		int zp3=GetTZValue(cx+1,cy+1);
		int za1=(zp0+zp1+zp2+zp3)>>2;
		int za0=(z0+z1+z2+z3)>>2;

		int dz0=z0-zp0;
		int dz1=z1-zp1;
		int dz2=z2-zp2;
		int dz3=z3-zp3;

		int dzmin=dz0;
		if(dz1<dzmin)dzmin=dz1;
		if(dz2<dzmin)dzmin=dz2;
		if(dz3<dzmin)dzmin=dz3;

		int dzmax=dz0;
		if(dz1>dzmax)dzmax=dz1;
		if(dz2>dzmax)dzmax=dz2;
		if(dz3>dzmax)dzmax=dz3;

		if(dzmin>=0){
			SetPackPixSquare(cx,cy,Buf,sz);
		}else if(dzmax>0){
			//mixing
			int dz0=z0-zp0;
			int dz1=z1-zp1;
			int dz2=z2-zp2;
			int dz3=z3-zp3;

			byte PBuf[1024];
			byte NBuf[1024];

			GetPixSquare(cx,cy,PBuf);
			SetPackPixSquare(cx,cy,Buf,sz);
			GetPixSquare(cx,cy,NBuf);

			int ofs=0;
			int zz0=dz0<<10;
			int dx0=(dz1-dz0)<<5;
			int dy0=(dz2-dz0)<<5;
			int ddx=dz3+dz0-dz1-dz2;
			for(int iy=0;iy<32;iy++){
				int zz=zz0;
				for(int ix=0;ix<32;ix++){
					if(zz0<0)NBuf[ofs]=PBuf[ofs];
					else if(!PBuf[ofs]){
						char cc=NBuf[ofs];
						int z=cc;
						z=z*zz0/dzmax/1024;
						NBuf[ofs]=z;
					};
					zz0+=dx0;
					ofs++;
				};
				dx0+=ddx;
				zz0=zz+dy0;
			};
			SetPixSquare(cx,cy,NBuf);
		};
		Buf+=sz;
	};
	/*
	Buf=BUF0;
	for(i=0;i<N;i++){
		int cx=xr+((short*)Buf)[0];
		Buf+=2;
		int cy=yr+((short*)Buf)[0];
		Buf+=2;

		int z0=((int*)Buf)[0]+(yr<<6);
		Buf+=4;
		int z1=((int*)Buf)[0]+(yr<<6);
		Buf+=4;
		int z2=((int*)Buf)[0]+(yr<<6);
		Buf+=4;
		int z3=((int*)Buf)[0]+(yr<<6);
		Buf+=4;

		int sz=((short*)Buf)[0];
		Buf+=2;

		int zp0=GetTZValue(cx,cy);
		int zp1=GetTZValue(cx+1,cy);
		int zp2=GetTZValue(cx,cy+1);
		int zp3=GetTZValue(cx+1,cy+1);

		if(z0>zp0)SetTZValue(cx,cy,z0);
		if(z1>zp1)SetTZValue(cx+1,cy,z1);
		if(z2>zp2)SetTZValue(cx,cy+1,z2);
		if(z3>zp3)SetTZValue(cx+1,cy+1,z3);

		Buf+=sz;
	};
	*/
};
extern int RealLx;
extern int RealLy;
extern byte wfog[8192];
void ShowTexPreview(int cx,int cy){
	if(!CurPixTexture)return;
	cx&=65535-31;
	cy&=65535-31;
	cx-=128;
	cy-=128;
	for(int ix=0;ix<8;ix++){
		for(int iy=0;iy<8;iy++){
			int x0=cx+(ix<<5);
			int y0=cy+(iy<<5);
			if(x0>=0&&y0>=0&&x0<=RealLx-32&&y0<=RealLy-32){
				int sofs=int(ScreenPtr)+x0+y0*ScrWidth;
				int txofs=int(CurPixTexture)+( ((((cx>>5)+ix+TexStartDX)&7)<<5) + ((((cy>>5)+iy+TexStartDY)&7)<<13));
				__asm{
					push esi
					push edi
					pushf
					mov  esi,txofs
					mov  edi,sofs
					mov  cx,0x2010
					xor  eax,eax
LB1:				
					mov  ah,[esi]
					mov	 al,0//[edi]
					shr  ah,3
					mov  al,byte ptr[wfog+eax]
					mov  [edi],al
					add  edi,2
					add  esi,2
					dec  cl
					jnz  LB1
					mov  cl,16
					add  esi,256-32
					add  edi,ScrWidth
					//add  edi,ScrWidth
					sub  edi,32
					dec  ch
					jnz  LB1
					popf
					pop  edi
					pop  esi
				};
			};
		};
	};
};
//---------------Texture covering tool--------------//
struct OneCashedTexture{
	DWORD GTEXID;
	byte  TexPix[512];
};
class TexCoverage{
public:
	OneCashedTexture** TxCash;
	int NCTex;
	int MaxCTex;
	TexCoverage();
	~TexCoverage();
	int GetTexIndex(byte* Bytes);
	void LoadTexture(char* File);
};
TexCoverage::TexCoverage(){
	memset(this,0,sizeof TexCoverage);
};
TexCoverage::~TexCoverage(){
	for(int i=0;i<NCTex;i++)free(TxCash[i]);
	if(TxCash)free(TxCash);
	memset(this,0,sizeof TexCoverage);
};
TexCoverage TCOVER;
void PerformLightFiltering(byte* Data){
	DWORD L=0;
	for(int i=0;i<65536;i++)L+=Data[i];
	L>>=16;
	if(!L)return;
	for(i=0;i<65536;i++){
		int v=Data[i];
		v=v*112/L;
		if(v>255)v=255;
		Data[i]=v;
	};
};
void TexCoverage::LoadTexture(char* File){
	BMPformat BM;
	byte* TEX=NULL;
	if(ReadBMP8(File,&BM,&TEX)){
		if(BM.biWidth==256){
			//PerformLightFiltering(TEX);
			for(int iy=0;iy<8;iy++){
				for(int ix=0;ix<8;ix++){
					GetTexIndex(TEX+(ix<<5)+(iy<<13));
		};
			};
		};
		free(TEX);
	};
};
int TexCoverage::GetTexIndex(byte* Bytes){
	/*
	DWORD S;
	__asm{
		push esi
		push ecx
		mov  esi,Bytes
		cld
		xor  ebx,ebx
		mov	 ecx,256
RR1:	
		lodsd
		add  ebx,eax
		dec  ecx
		jnz  RR1
		mov  S,ebx
		pop  ecx
		pop  esi
	};
	for(int i=0;i<NCTex;i++)if(S==TxCash[i]->GTEXID)return i;
	*/
	if(NCTex>=MaxCTex){
		MaxCTex+=256;
		TxCash=(OneCashedTexture**)realloc(TxCash,MaxCTex<<2);
	};
	TxCash[NCTex]=znew(OneCashedTexture,1);
	int ofs=int(TxCash[NCTex]->TexPix);
	__asm{
		push esi
		push edi
		push ecx
		mov  esi,Bytes
		mov  edi,ofs
		mov  ecx,0x2010
		cld
FF3:    lodsw
		and  ax,0xF0F0
		shr  al,4
		or   al,ah
		stosb
		dec  cl
		jnz  FF3
		add  esi,256-32
		mov  cl,0x10 
		dec  ch
		jnz  FF3
		pop  ecx
		pop  edi
		pop  esi
	};
	NCTex++;
	return NCTex-1;
};
byte MultTable[512];
bool MT_IsInit=0;
void PutWeightToTexture(byte* SrcTex,byte* DstTex,int w1,int w2,int w3,int w4){
	if(w1<=0&&w2<=0&&w3<=0&&w4<=0)return;
	if(w1>=15&&w2>=15&&w3>=15&&w4>=15){
		//simple widing copy
		__asm{
			push esi
			push edi
			push ecx
			
			mov  esi,SrcTex
			mov  edi,DstTex
			mov  ecx,512
			cld
FF4:		lodsb
			mov  ah,al
			shr  ah,4
			and  al,15
			shl  ax,1
			stosw
			dec  ecx
			jnz  FF4
			
			pop  ecx
			pop  edi
			pop  esi
		};
	}else{
		if(!MT_IsInit){
			int ofs=0;
			for(int i=0;i<32;i++)
				for(int j=0;j<16;j++){
					MultTable[ofs]=(i*(2*j-15))/30;
					ofs++;
				};
			MT_IsInit=1;
		};
		w1<<=10;
		w2<<=10;
		w3<<=10;
		w4<<=10;
		int dwx=(w2-w1)>>4;
		int dwy=(w3-w1)>>5;
		int ddx=(w1+w4-w2-w3)>>9;
		int cv=32;
		__asm{
			push esi
			push edi
			push ecx
			
			mov  esi,SrcTex
			mov  edi,DstTex
			mov  ebx,w1
			mov	 cl,0x10
			xor  eax,eax
FF7:		lodsb
			mov  ch,al
			and  ax,0x0F
			mov  edx,ebx
			sar  edx,5
			cmp  edx,0x1F0
			jle  FF8
			mov  edx,0x1F0
FF8:		cmp  edx,16
			jge  FF9
			mov  edx,16
FF9:
			and  dx,0x1F0
			or   ax,dx
			mov  al,[MultTable+eax]
			add  [edi],al
			mov  al,ch
			and  ax,0xF0
			shr  ax,4
			inc  edi
			or   ax,dx
			mov  al,[MultTable+eax]
			add  [edi],al
			add  ebx,dwx
			inc  edi
			dec  cl
			jnz  FF7
			mov  ebx,w1
			add  ebx,dwy
			mov  w1,ebx
			mov  eax,dwx
			add  eax,ddx
			mov  dwx,eax
			mov  cl,0x10
			xor  eax,eax
			dec  cv
			jnz  FF7
			pop  ecx
			pop  edi
			pop  esi
		};
	};
};
char* PointTexWeight=NULL;
word* PointTexIndex=NULL;
int PT_Lx=0;
int PT_Ly=0;
int PT_SHW=0;
int PT_SHI=0;
int TexStDX=0;
int TexStDY=0;
int SavePointTexInfo(ResFile F){
	int sz=8+PT_Lx*PT_Ly*3;
	RBlockWrite(F,&sz,4);
	RBlockWrite(F,PointTexWeight,PT_Lx*PT_Ly);
	RBlockWrite(F,PointTexIndex,PT_Lx*PT_Ly*2);
	return 8+PT_Lx*PT_Ly*3;
};
void LoadPointTexInfo(ResFile F){
	RBlockRead(F,PointTexWeight,PT_Lx*PT_Ly);
	RBlockRead(F,PointTexIndex,PT_Lx*PT_Ly*2);
};
void InitFlowTex();
int PENA_GP=0;
void SetupPointTexs(){
	InitFlowTex();
	PT_Lx=256<<ADDSH;
	PT_Ly=128<<ADDSH;
	PointTexWeight=(char*)realloc(PointTexWeight,PT_Lx*PT_Ly);
	PointTexIndex=(word*)realloc(PointTexIndex,PT_Lx*PT_Lx);
	memset(PointTexWeight,0x81,PT_Lx*PT_Ly);
	memset(PointTexIndex,0xFF,PT_Lx*PT_Lx);
	PT_SHW=8+ADDSH;
	PT_SHI=8+ADDSH;
	//---testing---//
	///for(int i=0;i<40;i++){
	//	char cc[64];
	//	sprintf(cc,"Ground\\tex%d.bmp",i);
	//	TCOVER.LoadTexture(cc);
	//};
	PENA_GP=GPS.PreLoadGPImage("pena");
	//memset(PointTexWeight,0xFF,PT_Ly*PT_Ly);
	memset(PointTexIndex,0,PT_Lx*PT_Lx);
	int sz=PT_Ly*PT_Ly;
	//for(int i=0;i<sz;i++)PointTexWeight[i]=rand();
	//PointTexWeight[PT_Lx*8+3]=0x7F;
	/*
	for(int ix=10;ix<20;ix++){
		for(int iy=10;iy<20;iy++){
			PointTexIndex[ix+iy*PT_Lx]=36;
		};
	};
	*/

};
void FreePointsTex(){
	if(PointTexWeight){
		free(PointTexWeight);
		free(PointTexIndex);
		PointTexWeight=NULL;
		PointTexIndex=NULL;
	};
};
int GetTWeight(int cx,int cy){
	if(cx<0||cy<0||cx>=PT_Lx||cy>=PT_Ly)return 0;
	int ofs=cx+(cy<<PT_SHW);
	return PointTexWeight[ofs];
};
byte MiniTexMask[60]={
	0,0,0,0,0,0,0,0,0,0,
	1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,
	0,0,0,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1};

int GetMiniTWeight(int cx,int cy){
	if(cx<0||cy<0||cx>=PT_Lx||cy>=PT_Ly)return 0;
	int ofs=cx+(cy<<PT_SHW);
	int tex=PointTexIndex[ofs]>>6;
	if(!MiniTexMask[tex])return 0;
	return PointTexWeight[ofs];
};
void SetTWeight(int cx,int cy,int w){
	if(cx<0||cy<0||cx>=PT_Lx||cy>=PT_Ly)return;
	int ofs=cx+(cy<<PT_SHW);
	PointTexWeight[ofs]=w;
};
int GetPTexture(int cx,int cy){
	if(cx<0||cy<0||cx>=PT_Lx||cy>=PT_Ly)return 0xFFFF;
	int ofs=cx+(cy<<PT_SHI);
	return PointTexIndex[ofs];
};
void SetPTexture(int cx,int cy,int tex){
	if(cx<0||cy<0||cx>=PT_Lx||cy>=PT_Ly)return;
	int ofs=cx+(cy<<PT_SHI);
	PointTexIndex[ofs]=tex;
};
inline int GCM(int v,bool c){
	if(c&&v>15)return 15;
	else return v;
};
#define CW(x) GCM(x,!UNITEX)
void DrawMaskOnScreen(byte* Buf,int x,int y,int cx,int cy,int BufWidth){
	int w1=GetTWeight(cx,cy);
	int w2=GetTWeight(cx+1,cy);
	int w3=GetTWeight(cx,cy+1);
	int w4=GetTWeight(cx+1,cy+1);
	
	if(!(w1||w2||w3||w4))return;
	byte TMP[1024];
	memset(TMP,15,1024);
	int tex1=GetPTexture(cx,cy);
	int tex2=GetPTexture(cx+1,cy);
	int tex3=GetPTexture(cx,cy+1);
	int tex4=GetPTexture(cx+1,cy+1);
	bool UNITEX=tex1==tex2&&tex1==tex3&&tex1==tex4;
	if(tex1<TCOVER.NCTex){
		int tx=(cx+(tex1&7))&7;
		int ty=(cy+((tex1>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex1&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,CW(w1),tex1==tex2?CW(w2):0,tex1==tex3?CW(w3):0,tex1==tex4?CW(w4):0);
	};
	if(tex2<TCOVER.NCTex&&tex2!=tex1){
		int tx=(cx+(tex2&7))&7;
		int ty=(cy+((tex2>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex2&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,tex2==tex1?CW(w1):0,CW(w2),tex2==tex3?CW(w3):0,tex2==tex4?CW(w4):0);
	};
	if(tex3<TCOVER.NCTex&&tex3!=tex2&&tex3!=tex1){
		int tx=(cx+(tex3&7))&7;
		int ty=(cy+((tex3>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex3&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,tex3==tex1?CW(w1):0,tex3==tex2?CW(w2):0,CW(w3),tex3==tex4?CW(w4):0);
	};
	if(tex4<TCOVER.NCTex&&tex4!=tex3&&tex4!=tex2&&tex4!=tex3){
		int tx=(cx+(tex4&7))&7;
		int ty=(cy+((tex4>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex4&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,tex4==tex1?CW(w1):0,tex4==tex2?CW(w2):0,tex4==tex3?CW(w3):0,CW(w4));
	};
	// :-)  ready to stamp this ...(piece)
	TempWindow TW;
	PushWindow(&TW);
	int ww=ScrWidth;
	void* ptr=ScreenPtr;
	ScreenPtr=Buf;
	ScrWidth=BufWidth;
	int ofs0=int(Buf)+x+(y*BufWidth);
	int DDX=BufWidth-32;
	int tofs=int(TMP);
	__asm{
		push esi
		push edi
		mov  esi,tofs
		mov  edi,ofs0
		mov	 cx,0x2020
		xor  eax,eax
DOPC1:	lodsb
		cmp  al,31
		jl   TT1
		mov  al,31
TT1:    cmp  al,0
		jge  TT2
		mov  al,0
TT2:	mov  ah,al
		//shl  ah,1
		mov  al,[edi]
		and  ah,0x1F;
		mov  bl,[InvBrFog+eax]
		mov  [edi],bl
		inc  edi
		dec  cl
		jnz  DOPC1
		add  edi,DDX
		mov  cl,0x20
		dec  ch
		jnz  DOPC1
		pop  edi
		pop  esi
	};
	PopWindow(&TW);
	ScreenPtr=ptr;
};

void MarkSqToRedraw(int sx,int sy){
	if(sx<0||sy<0||sx>=PT_Lx||sy>=PT_Ly)return;
	SVSC.MarkedX[sx]=1;
	div_t sxdt=div(sx,SVSC.CellNX);
	int Miny=(sy<<5)/SVSC.CellSY;
	if(Miny<0)Miny=0;
	div_t sydt=div(Miny,SVSC.CellNY);
	sx=sxdt.rem;
	int ofst=sx+sydt.rem*SVSC.CellNX;
	int maxo=SVSC.NCells;
	if(SVSC.CellQuotX[ofst]==sxdt.quot&&SVSC.CellQuotY[ofst]==sydt.quot){
		if(ofst>=0&&ofst<maxo)SVSC.CellFlags[ofst]=0;
	};
#ifdef _USE3D
	void MakeDirtyGBUF(int mx,int my,int sx,int sy);
	MakeDirtyGBUF(sx,sy<<1,1,2);
#endif
};
extern int TexStDX;
extern int TexStDY;
extern int GroundBrightness;
extern int CurPressure;
extern bool WT_IS_INIT;
extern int CurGroundTexture;
void SetGroundTexRound(int TxIdx,int x0,int y0,int r,int dr){
#ifdef _USE3D
	void SetNewFactureInRound(int x,int y,int r,int dr,int Facture,int Weight,bool UseCentralTexture);
	SetNewFactureInRound(x0,y0,r,dr,TxIdx,GroundBrightness*255/100,GetKeyState(VK_CONTROL)&0x8000);
	return;
#endif
	bool GETTEXMOD=GetKeyState(VK_MENU)&0x8000;
	int xGroundBrightness=GroundBrightness;
	if(WT_IS_INIT){
		xGroundBrightness=GroundBrightness*CurPressure/768;
	};
	TxIdx=(TxIdx<<6)+TexStDX+(TexStDY<<3);
	if(dr<=4)dr=4;
	int cx0=(x0-r-dr-200)>>5;
	int cx1=(x0+r+dr+200)>>5;
	int cy0=(y0-(r+dr)/2-200)>>5;
	int cy1=(y0+(r+dr)/2+200)>>5;
	if(cx0<0)cx0=0;
	if(cy0<0)cy0=0;
	if(cx1>=PT_Lx)cx1=PT_Lx-1;
	if(cy1>=PT_Ly)cy1=PT_Ly-1;
	y0<<=1;
	int MaxBri=16*xGroundBrightness/100;
	if(MaxBri>=15)MaxBri=126;
	int besttex=0;
	for(int ix=cx0;ix<=cx1;ix++){
		for(int iy=cy0;iy<=cy1;iy++){
			int xx=ix<<5;
			int yy=iy<<6;
			int rr=sqrt((xx-x0)*(xx-x0)+(yy-y0)*(yy-y0));
			if(GETTEXMOD){
				if(rr<32){
					CurGroundTexture=GetPTexture(ix,iy)>>6;
				};
			}else{
				bool change=0;
				int cw=(15*(r+dr-rr))/dr;
				if(cw<-126)cw=-126;
				if(cw>MaxBri)cw=MaxBri;
				int LTxIdx=TxIdx;//+(rand()&7)+((rand()&7)<<3);
				int w=GetTWeight(ix,iy);
				if(cw>w){
					if(TxIdx>=0){
						SetTWeight(ix,iy,cw);
						SetPTexture(ix,iy,LTxIdx);
					};
					if(cw>0)change=1;	
				};
				if(cw>0){
					if(TxIdx>=0){
						int t=GetPTexture(ix,iy);
						if(t!=0xFFFF&&t!=LTxIdx){
							if(cw>10||cw>=MaxBri-1)SetPTexture(ix,iy,LTxIdx);
						}else SetPTexture(ix,iy,LTxIdx);
					};
					change=1;
				};
				if(change){
					ReportCoorChange(ix>>1,iy<<1);
					for(int dx=-1;dx<=1;dx++)
						for(int dy=-1;dy<=1;dy++){
							MarkSqToRedraw(ix+dx,iy+dy);
						};
				};
			};
		};
	};
};
void ClearGroundTexRound(int x0,int y0,int r,int dr){
#ifdef _USE3D
	void ClearNewFacture(int x,int y,int r,int dr);
	ClearNewFacture(x0,y0,r,dr);
#endif
	int xGroundBrightness=GroundBrightness;
	if(WT_IS_INIT){
		xGroundBrightness=GroundBrightness*CurPressure/768;
	};
	if(dr<=4)dr=4;
	int cx0=(x0-r-dr-200)>>5;
	int cx1=(x0+r+dr+200)>>5;
	int cy0=(y0-(r+dr)/2-200)>>5;
	int cy1=(y0+(r+dr)/2+200)>>5;
	if(cx0<0)cx0=0;
	if(cy0<0)cy0=0;
	if(cx1>=PT_Lx)cx1=PT_Lx-1;
	if(cy1>=PT_Ly)cy1=PT_Ly-1;
	y0<<=1;

	int MinBri=16*(100-xGroundBrightness)/100;
	if(MinBri<1)MinBri=-126;

	for(int ix=cx0;ix<=cx1;ix++){
		for(int iy=cy0;iy<=cy1;iy++){
			int xx=ix<<5;
			int yy=iy<<6;
			int rr=sqrt((xx-x0)*(xx-x0)+(yy-y0)*(yy-y0));
			bool change=0;
			int cw=(15*(rr-r))/dr;
			if(cw<MinBri)cw=MinBri;
			if(cw>126)cw=126;
			int w=GetTWeight(ix,iy);
			if(cw<w){
				SetTWeight(ix,iy,cw);
				if(w>0)change=1;
			};
			if(change){
				for(int dx=-1;dx<=1;dx++)
					for(int dy=-1;dy<=1;dy++)
						MarkSqToRedraw(ix+dx,iy+dy);
			};
		};
	};
};
byte* AnmWater=NULL;
void LoadAnmWater(){
	BMPformat BM;
	if(ReadBMP8("Water\\voda.bmp",&BM,&AnmWater)){
		for(int i=0;i<65536;i++){
			int v=AnmWater[i];
			v=8+(v-128)/16;
			if(v<0)v=0;
			if(v>15)v=15;
			AnmWater[i]=v;
		};
	};
};


void Fast_DrawGrayMask(int x,int y,byte* SrcPic,int Lx,int Ly,int LL){
	int scofs=int(ScreenPtr)+x+y*ScrWidth;
	int adbofs=LL-(Lx<<2);
	int adsofs=ScrWidth-(Lx<<2);
	int L;
	__asm{
		push esi
		push edi
		xor  ecx,ecx
		mov  esi,SrcPic
		mov  edi,scofs
Loop0:
		mov  eax,Lx
		mov  L,eax

Loop1:
		mov  edx,[esi]
		mov  ebx,[edi]

		mov  ch,dl
		mov  cl,bl
		mov  bl,[InvBrFog+2048+ecx]

		ror  edx,8
		ror  ebx,8

		mov  cl,dl
		mov  cl,bl
		mov  bl,[InvBrFog+2048+ecx]

		ror  edx,8
		ror  ebx,8

		mov  cl,dl
		mov  cl,bl
		mov  bl,[InvBrFog+2048+ecx]

		ror  edx,8
		ror  ebx,8

		mov  cl,dl
		mov  cl,bl
		mov  bl,[InvBrFog+2048+ecx]

		ror  edx,8
		ror  ebx,8

		mov  [edi],ebx
		add  esi,4		
		add  edi,4

		dec  L
		jnz  Loop1
		add  esi,adbofs
		add  edi,adsofs
		dec  Ly
		jnz  Loop0
		pop  edi
		pop  esi
	};
};
void Fast_DrawGrayIndexMask(int x,int y,int Index){
	//Index=(Index-20);
	if(Index<0||Index>32)return;
	Index/=4;
	if(AnmWater)Fast_DrawGrayMask(x-16,y-16,AnmWater+Index*32,8,32,256);
};

extern byte WaterCost[65536];
extern short randoma[8192];
void DrawSubWaterSquare(byte* Buf,int x,int y,int cx,int cy,int BufWidth){
#ifndef CONQUEST
	return;
#endif
#ifdef _USE3D
	return;
#endif
	int ofs=cx+(cy*MaxWX*2);
	int z1=int(WaterDeep[ofs])-127;
	int z2=int(WaterDeep[ofs+1])-127;
	int z3=int(WaterDeep[ofs+MaxWX+MaxWX])-127;
	int z4=int(WaterDeep[ofs+MaxWX+MaxWX+1])-127;
	if(z1<0&&z2<0&&z3<0)return;
	TempWindow TW;
	PushWindow(&TW);
	int ww=ScrWidth;
	void* ptr=ScreenPtr;
	ScreenPtr=Buf;
	ScrWidth=BufWidth;
	int ofs0=int(Buf)+x+(y*BufWidth);

	z1<<=10;
	z2<<=10;
	z3<<=10;
	z4<<=10;
	int dzx=(z2-z1)>>5;
	int dzy=(z3-z1)>>5;
	int dxx=(z1+z4-z3-z2)>>10;

	int sdx=ScrWidth-32;

	__asm{
		push esi
		push edi
		mov  edi,ofs0
		mov  ebx,z1
		mov  ecx,0x2020
		xor  eax,eax
zzz1:
		//mov  ah,[edi]
		mov  edx,ebx
		cmp  edx,0
		jge  rrr1
		mov  edx,0
rrr1:	cmp  edx,65000
		jle  rrr3
		mov  edx,65000
		jmp  rrr2

rrr3:	xor  eax,eax
		mov  ax,dx
		add  ax,di
		and  ax,8191
		mov  ax,[randoma+eax]
		shr  eax,2
		add  edx,eax
		cmp  edx,65000
		jle  rrr2
		mov  edx,65000
rrr2:
		mov  ah,[edi]
		mov  al,dh
		and  al,0xF0
		mov  al,[WaterCost+eax+1]
		mov  [edi],al
		add  ebx,dzx
		inc  edi
		dec  cl
		jnz  zzz1
		mov  ebx,dzx
		add  ebx,dxx
		mov  dzx,ebx
		mov  ebx,z1
		add  ebx,dzy
		mov  z1,ebx
		mov  cl,32
		add  edi,sdx
		dec  ch
		jnz  zzz1

		pop  edi
		pop  esi
	};
	PopWindow(&TW);
	ScreenPtr=ptr;
};
#define FLLX0 128
#define FLLX 256
#define FLLX2 512
byte* FlowTex;//[FLLX*FLLX];
void CreateWTexture();
void DrawPixShadowArray(int x,int y,byte* Pix,int Lx,int Ly);
byte* WaveTex=NULL;
void InitFlowTex(){
	BMPformat BM;
	ReadBMP8("Wave0.bmp",&BM,&WaveTex);
	//ReadBMP8("x2.bmp",&BM,&FlowTex);
	FlowTex=znew(byte,256*(256+24));
	CreateWTexture();
	return;
	byte Temp[64*64];
	for(int ix=0;ix<64;ix++)
		for(int iy=0;iy<64;iy++)
			Temp[ix+(iy<<6)]=rand()%3;
	//byte TempTex[16*16]
	for(ix=0;ix<FLLX/4;ix++)
		for(int iy=0;iy<FLLX/4;iy++){
			byte c;
			int z1=Temp[ix+(iy<<6)];
			int z2=Temp[ix+(((iy+1)&63)<<6)];
			if(z1==z2)c=0xB6;
			if(z1<z2)c=0xB7;
			if(z1>z2)c=0xB5;

			//byte c=0xB5+(rand()%3);
			int ofs=ix+ix+iy*FLLX2;
			FlowTex[ofs]=c;
			FlowTex[ofs+1]=c;
			FlowTex[ofs+FLLX]=c;
			FlowTex[ofs+FLLX+1]=c;

			ofs+=FLLX0;

			FlowTex[ofs]=c;
			FlowTex[ofs+1]=c;
			FlowTex[ofs+FLLX]=c;
			FlowTex[ofs+FLLX+1]=c;

			ofs+=-FLLX0+FLLX*FLLX0;

			FlowTex[ofs]=c;
			FlowTex[ofs+1]=c;
			FlowTex[ofs+FLLX]=c;
			FlowTex[ofs+FLLX+1]=c;

			ofs+=FLLX0;

			FlowTex[ofs]=c;
			FlowTex[ofs+1]=c;
			FlowTex[ofs+FLLX]=c;
			FlowTex[ofs+FLLX+1]=c;
		};
	ResFile F=RRewrite("x1.raw");
	RBlockWrite(F,FlowTex,65536);
	RClose(F);
};
//#define NOSQUARE

void DynDrawWaterFlow16(int x,int y,byte* Flow,int z1,int z2,int z3,int z4){
	//z1-=14;
	//z2-=14;
	//z3-=14;
	//z4-=14;
	if(z1<0&&z2<0&&z3<0&&z4<0)return;
	int ofs=int(ScreenPtr)+x+y*ScrWidth;
	int tofs=int(Flow);
	int dofs=ScrWidth-16;
	if(z1<0||z2<0||z3<0||z4<0){
		z1<<=7;
		z2<<=7;
		z3<<=7;
		z4<<=7;
		int dzx=(z2-z1)>>3;
		int dzy=(z3-z1)>>4;
		int dxx=(z1+z4-z2-z3)>>7;
		__asm{
			push esi
			push edi
			mov  esi,tofs
			mov  edi,ofs
			mov  ebx,z1
			mov  ecx,0x0808
BLP_1:
			cmp  ebx,0
			jl   BLP_2
			mov  al,[esi]
			mov  [edi],al
BLP_2:
			add  esi,2
			add  edi,2
			add  ebx,dzx
			dec  cl
			jnz  BLP_1

			mov  ebx,dzx
			add  ebx,dxx
			mov  dzx,ebx

			mov  ebx,z1
			add  ebx,dzy
			mov  z1,ebx

			mov  cl,8
			add  edi,dofs
			add  esi,256-16+1
			inc  edi
BLP_3:
			cmp  ebx,0
			jl   BLP_4
			mov  al,[esi]
			mov  [edi],al
BLP_4:
			add  esi,2
			add  edi,2
			add  ebx,dzx
			dec  cl
			jnz  BLP_3

			mov  ebx,dzx
			add  ebx,dxx
			mov  dzx,ebx

			mov  ebx,z1
			add  ebx,dzy
			mov  z1,ebx

			mov  cl,8
			add  edi,dofs
			add  esi,256-16-1
			dec  edi

			dec  ch
			jnz  BLP_1

			pop  edi
			pop  esi
		};
		return;
	};
	__asm{
		push esi
		push edi
		mov  esi,tofs
		mov  edi,ofs

		mov  ecx,0x0204
GGG1:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0xFF00FF00FF00
		and  ebx,0x00FF00FF00FF
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0x00FF00FF00FF
		and  ebx,0xFF00FF00FF00
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		sub  edi,4
		sub  esi,4
#endif
GGG1_1:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0xFF00FF00FF00
		and  ebx,0x00FF00FF00FF
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1_1
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2_1:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0x00FF00FF00FF
		and  ebx,0xFF00FF00FF00
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2_1

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		add  edi,4
		add  esi,4
#endif
GGG1_2:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0xFF00FF00FF00
		and  ebx,0x00FF00FF00FF
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1_2
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2_2:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0x00FF00FF00FF
		and  ebx,0xFF00FF00FF00
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2_2

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		sub  edi,4
		sub  esi,4
#endif
GGG1_3:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0xFF00FF00FF00
		and  ebx,0x00FF00FF00FF
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1_3
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2_3:
		mov  eax,[edi]
		mov  ebx,[esi]
		and  eax,0x00FF00FF00FF
		and  ebx,0xFF00FF00FF00
		or   eax,ebx
		mov  [edi],eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2_3

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		add  edi,4
		add  esi,4
#endif
		dec  ch
		jnz  GGG1

		pop  edi
		pop  esi
	};
};
void DynDrawWaterFlow16_1(int x,int y,byte* Flow,int z1,int z2,int z3,int z4){
	z1-=14;
	z2-=14;
	z3-=14;
	z4-=14;
	if(z1<0&&z2<0&&z3<0&&z4<0)return;
	int ofs=int(ScreenPtr)+x+y*ScrWidth;
	int tofs=int(Flow);
	int dofs=ScrWidth-16;
	if(z1<0||z2<0||z3<0||z4<0){
		z1<<=7;
		z2<<=7;
		z3<<=7;
		z4<<=7;
		int dzx=(z2-z1)>>3;
		int dzy=(z3-z1)>>4;
		int dxx=(z1+z4-z2-z3)>>7;
		__asm{
			push esi
			push edi
			mov  esi,tofs
			mov  edi,ofs
			mov  ebx,z1
			mov  ecx,0x0808
BLP_1:
			cmp  ebx,0
			jl   BLP_2
			mov  al,[esi]
			mov  [edi],al
BLP_2:
			add  esi,2
			add  edi,2
			add  ebx,dzx
			dec  cl
			jnz  BLP_1

			mov  ebx,dzx
			add  ebx,dxx
			mov  dzx,ebx

			mov  ebx,z1
			add  ebx,dzy
			mov  z1,ebx

			mov  cl,8
			add  edi,dofs
			add  esi,256-16+1
			inc  edi
BLP_3:
			cmp  ebx,0
			jl   BLP_4
			mov  al,[esi]
			mov  [edi],al
BLP_4:
			add  esi,2
			add  edi,2
			add  ebx,dzx
			dec  cl
			jnz  BLP_3

			mov  ebx,dzx
			add  ebx,dxx
			mov  dzx,ebx

			mov  ebx,z1
			add  ebx,dzy
			mov  z1,ebx

			mov  cl,8
			add  edi,dofs
			add  esi,256-16-1
			dec  edi

			dec  ch
			jnz  BLP_1

			pop  edi
			pop  esi
		};
		return;
	};
	__asm{
		push esi
		push edi
		mov  esi,tofs
		mov  edi,ofs

		mov  ecx,0x0204
GGG1:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		sub  edi,4
		sub  esi,4
#endif
GGG1_1:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1_1
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2_1:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2_1

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		add  edi,4
		add  esi,4
#endif
GGG1_2:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1_2
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2_2:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2_2

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		sub  edi,4
		sub  esi,4
#endif
GGG1_3:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG1_3
		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs

GGG2_3:
		mov  eax,[edi]
		mov  ebx,[esi]
		//and  eax,0xFF00FF00FF00
		//and  ebx,0x00FF00FF00FF
		//or   eax,ebx
		mov  [edi],ebx//eax
		add  edi,4
		add  esi,4
		dec  cl
		jnz  GGG2_3

		mov  cl,4
		add  esi,FLLX-16
		add  edi,dofs
#ifdef NOSQUARE
		add  edi,4
		add  esi,4
#endif
		dec  ch
		jnz  GGG1

		pop  edi
		pop  esi
	};
};
extern int RivNX;
extern int RivSH;
extern byte* RivDir;
extern byte* RivVol;
void GetRVD(int cx,int cy,int* dx,int* dy){
	if(cx<0||cx>=RivNX||cy<0||cy>=RivNX){
		*dx=0;
		*dy=0;
		return;
	};
	int ofs=cx+(cy<<RivSH);
	byte dir=RivDir[ofs];
	int vol=RivVol[ofs];
	*dx=(int(TCos[dir])*vol)>>8;
	*dy=(int(TSin[dir])*vol)>>9;
	return;
};
void GetRivDir(int x,int y,int* dx,int* dy){
	int Dx0,Dy0,Dx1,Dy1,Dx2,Dy2,Dx3,Dy3;
	x-=4;
	y-=2;

	int xx,yy;
	int cx=x&7;
	int cy=y&3;
	int XX=x>>3;
	int YY=y>>2;

	GetRVD(XX,YY,&Dx0,&Dy0);
	GetRVD(XX+1,YY,&Dx1,&Dy1);
	GetRVD(XX,YY+1,&Dx2,&Dy2);
	GetRVD(XX+1,YY+1,&Dx3,&Dy3);
	xx=cx;
	yy=cy;
	//*dx=Dx0+(((Dx1-Dx0)*xx)>>4)+(((Dx1-Dx0)*yy)>>3)+(((Dx0+Dx3-Dx1-Dx2)*xx*yy)>>7);
	//*dy=Dy0+(((Dy1-Dy0)*xx)>>4)+(((Dy1-Dy0)*yy)>>3)+(((Dy0+Dy3-Dy1-Dy2)*xx*yy)>>7);
	*dx=((Dx0+Dx2)*(15-xx-xx)+(Dx1+Dx3)*(xx+xx+1))>>4;
	*dy=((Dy0+Dy1)*(7-yy-yy)+(Dy2+Dy3)*(yy+yy+1))>>3;
};
void DrawFlowInMicroCell(int x,int y,int cx,int cy,int z1,int z2,int z3,int z4){
	if(z1<0&&z2<0&&z3<0&&z4<0)return;
	int dx,int dy;
	GetRivDir(cx,cy,&dx,&dy);
	int t=GetTickCount();
	if(dx||dy){
		int v=(cx*29+cy*17)&4095;
		dx=dx*(100-15+(randoma[v]&31))/100;
		dy=dy*(100-15+(randoma[v+1]&31))/100;
	};
	int xx=((cx<<4)-(t*dx)/800)&255;
	int yy=((cy<<4)-(t*dy)/1600)&255;
	DynDrawWaterFlow16(x,y,FlowTex+xx+(yy<<8),z1,z2,z3,z4);
};
int GetDeep(int x,int y){
	int cx=x>>5;
	int cy=y>>5;
	if(cx<0||cx>=MaxWX||cy<=0||cy>(MaxWX>>1))return 0;
	int ofs=cx+(cy*MaxWX*2);
	int z1=int(WaterDeep[ofs])-127;
	int z2=int(WaterDeep[ofs+1])-127;
	int z3=int(WaterDeep[ofs+MaxWX+MaxWX])-127;
	int z4=int(WaterDeep[ofs+MaxWX+MaxWX+1])-127;

	int rx=x&31;
	int ry=y&31;
	return z1+((rx*(z2-z1))>>5)+((ry*(z3-z1))>>5)+((rx*ry*(z1+z4-z2-z3))>>10);
};
int GetDeep2(int x,int y){
	int cx=x>>5;
	int cy=y>>5;
	if(cx<0||cx>=MaxWX||cy<=0||cy>=MaxWX)return -127;
	int ofs=cx+(cy*MaxWX);
	int z1=int(WaterDeep[ofs])-127;
	int z2=int(WaterDeep[ofs+1])-127;
	int z3=int(WaterDeep[ofs+MaxWX])-127;
	int z4=int(WaterDeep[ofs+MaxWX+1])-127;

	int rx=x&31;
	int ry=y&31;
	return z1+((rx*(z2-z1))>>5)+((ry*(z3-z1))>>5)+((rx*ry*(z1+z4-z2-z3))>>10);
};
int GetApproxDeep(int x,int y){
	int cx=(x+16)>>5;
	int cy=(y+16)>>5;
	if(cx<0||cx>=MaxWX||cy<=0||cy>=MaxWX)return 0;
	int ofs=cx+(cy*MaxWX);
	return WaterDeep[ofs];
};
void DynDrawWaterFlow(int x,int y,int cx,int cy){
	if(cx<0||cx>=MaxWX||cy<=0||cy>(MaxWX>>1))return;
	int ofs=cx+(cy*MaxWX*2);

	float v=cx*cy;
	v=40.0;//+27.0*sin(v/10.0);
	int z1=int(WaterDeep[ofs])-127;
	int z2=int(WaterDeep[ofs+1])-127;
	int z3=int(WaterDeep[ofs+MaxWX+MaxWX])-127;
	int z4=int(WaterDeep[ofs+MaxWX+MaxWX+1])-127;
	if(z1<0&&z2<0&&z3<0&&z4<0)return;

	int z12=(z1+z2)>>1;
	int z13=(z1+z3)>>1;
	int z34=(z3+z4)>>1;
	int z24=(z2+z4)>>1;
	int zc=(z1+z2+z3+z4)>>2;
/*
	float T=(GetTickCount()/v)+cx+cy;
	int xx=cx*77+int(T+sin(T/10.0)*3)&127;
	int yy=cy*33+int(-T+cos(T/12.0)*3)&127;
	DrawFlowInMicroCell(
	*/
	DrawFlowInMicroCell(x,y,cx*2,cy*2,z1,z12,z13,zc);

	DrawFlowInMicroCell(x+16,y,cx*2+1,cy*2,z12,z2,zc,z24);

	DrawFlowInMicroCell(x,y+16,cx*2,cy*2+1,z13,zc,z3,z34);

	DrawFlowInMicroCell(x+16,y+16,cx*2+1,cy*2+1,zc,z24,z34,z4);

/*

	DynDrawWaterFlow16(x,y,FlowTex+xx+yy*FLLX,z1,z12,z13,zc);

	DynDrawWaterFlow16(x+16,y,FlowTex+xx+16+yy*FLLX,z12,z2,zc,z24);

	DynDrawWaterFlow16(x,y+16,FlowTex+xx+(yy+16)*FLLX,z13,zc,z2,z34);

	DynDrawWaterFlow16(x+16,y+16,FlowTex+xx+16+(yy+16)*FLLX,zc,z24,z34,z4);
*/
};
//-------------------STREAMS---------------
struct OneStreamPiece{
	int x,y;
	int time0;
	int time;
	byte Type;
	bool Drawn;
};
int GetRotor(int cx,int cy){
	int tmp,VxU,VyL,VxD,VyR;
	GetRivDir(cx-1,cy,&tmp,&VyL);
	GetRivDir(cx+1,cy,&tmp,&VyR);
	GetRivDir(cx,cy-1,&VxU,&tmp);
	GetRivDir(cx,cy+1,&VxD,&tmp);
	return VxU-VxD+VyR-VyL;
};
int GetDivirgence(int cx,int cy){
	int tmp,VxL,VxR,VyU,VyD;
	GetRivDir(cx-1,cy,&VxL,&tmp);
	GetRivDir(cx+1,cy,&VxR,&tmp);
	GetRivDir(cx,cy-1,&tmp,&VyU);
	GetRivDir(cx,cy+1,&tmp,&VyD);
	return VxL-VxR+VyU-VyD;
};
class WaterStreams{
public:
	OneStreamPiece** Pena;
	int NPieces;
	int MaxPieces;

	WaterStreams();
	~WaterStreams();
	void AddOneStream(int x,int y,int type);
	void Process();
	void ClearAll();
	void BornPiece();
	void Draw();
};
WaterStreams::WaterStreams(){
	memset(this,0,sizeof WaterStreams);
};
WaterStreams::~WaterStreams(){
	if(Pena)free(Pena);
	memset(this,0,sizeof WaterStreams);
};
void WaterStreams::ClearAll(){
	if(Pena)free(Pena);
	memset(this,0,sizeof WaterStreams);
};
void WaterStreams::AddOneStream(int x,int y,int type){
	if(NPieces>=MaxPieces){
		MaxPieces+=64;
		Pena=(OneStreamPiece**)realloc(Pena,MaxPieces<<2);
	};
	Pena[NPieces]=new OneStreamPiece;
	Pena[NPieces]->x=x;
	Pena[NPieces]->y=y;
	Pena[NPieces]->time0=GetTickCount();
	Pena[NPieces]->time=Pena[NPieces]->time0;
	Pena[NPieces]->Type=type;
	NPieces++;
};
void WaterStreams::Process(){
	for(int i=0;i<NPieces;i++){
		OneStreamPiece* PP=Pena[i];
		int vx,vy;
		int t=GetTickCount();
		int dt=t-Pena[i]->time;
		Pena[i]->time=t;
		GetRivDir(PP->x>>4,PP->y>>4,&vx,&vy);
		int deep=GetDeep(PP->x,PP->y);
		if(deep<18){
			if(deep<0)deep=0;
			int sclx=160-(deep*128)/24;
			if(sclx>128)sclx=128;
			vx<<=8;
			vy<<=8;
			int DR=GetDeep(PP->x+8,PP->y);
			int DL=GetDeep(PP->x-8,PP->y);
			int DU=GetDeep(PP->x,PP->y-8);
			int DD=GetDeep(PP->x,PP->y+8);
			int dx=DR-DL;
			int dy=(DD-DU)>>1;
			int n=dx*dx+dy*dy;
			int n0=sqrt(vx*vx+vy*vy);
			if(n){
				int scl=(dx*vx+dy*vy);
				vx-=(sclx*scl*dx/n)>>7;
				vy-=(sclx*scl*dy/n)>>7;
				int n1=sqrt(vx*vx+vy*vy);
				if(n1){
					vx=(vx*n0/n1)>>8;
					vy=(vy*n0/n1)>>8;
				}
			}else{
				vx=0;
				vy=0;
			};

		};
		//PP->x+=(dt*vx)/400;
		//PP->y+=(dt*vy)/400;
	};
	for(i=0;i<0;i++)BornPiece();
};
void WaterStreams::BornPiece(){
	int rx=(mapx<<5)+((int(rand())*RealLx)>>15);
	int ry=(mapy<<4)+((int(rand())*RealLy)>>15);
	int deep=GetDeep(rx,ry);
	if(deep>3){
		AddOneStream(rx,ry,rand()%3);
	};
};
extern byte Bright[8192];
void CreateWTexture();
void DrawPixShadowArrayA(int x,int y,byte* Pix,int Lx,int Ly,byte Alpha);
void WaterStreams::Draw(){
	CreateWTexture();
	int x0=mapx<<5;
	int y0=mapy<<4;
	for(int i=0;i<NPieces;i++)Pena[i]->Drawn=0;
	for(i=0;i<NPieces;i++){
		int x=Pena[i]->x-x0;
		int y=Pena[i]->y-y0;
		int type=Pena[i]->Type;
		int dt=Pena[i]->time-Pena[i]->time0;
		if(dt<200&&x>-16&&y>-16&&x<RealLx+16&&y<RealLy+16){
			int sp=sin(float(dt)*3.1415/3000.0)*11.0;
			if(sp>8)sp=8;
			if(sp<0)sp=0;
			/*
			switch(type){
			case 0:
				sp=11-int(sin(float(dt)*3.1415/3000.0)*10);
				break;
			case 1:
				sp=23-int(sin(float(dt)*3.1415/3000.0)*10);
				break;
			case 2:
				sp=29-int(sin(float(dt)*3.1415/3000.0)*5);
				break;
			};
			*/
			//GPS.ShowGPPal(x-32,y-32,PENA_GP,sp,0,InvBrFog+4096);
			//DrawPixShadowArray(x-16,y-16,WaveTex,32,32);
			//DrawPixShadowArrayA(x-16,y-16,WaveTex,32,32,sp);
			if((dt<200)&1)Hline(x,y,x,2);
			Pena[i]->Drawn=1;
		};
	};
	for(i=0;i<NPieces;i++){
		if(!Pena[i]->Drawn){
			free(Pena[i]);
			if(i<NPieces-1){
				memcpy(Pena+i,Pena+i+1,(NPieces-i-1)*4);
			};
			NPieces--;
			i--;
		};
	};
};
WaterStreams WSTREAM;
void ProcessPena(){
	WSTREAM.Process();
	WSTREAM.Draw();
};
void DrawPixShadowArray8(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,3
		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArray7(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,3
		mov   bl,ah
		sub   bl,16
		sar   bl,3
		sub   ah,bl

		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArray6(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,3
		mov   bl,ah
		sub   bl,16
		sar   bl,2
		sub   ah,bl

		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArray5(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,3
		mov   bl,ah
		sub   bl,16
		sar   bl,3
		sub   ah,bl
		sar   bl,1
		sub   ah,bl

		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArray4(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,4
		add   ah,8

		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArray3(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,4
		add   ah,8
		mov   bl,ah
		sub   bl,16
		sar   bl,2
		sub   ah,bl

		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArray2(int x,int y,byte* Pix,int Lx,int Ly){
	if(x<0||y<0||x+Lx>=RealLx||y+Ly>=RealLy)return;
	int sofs=int(ScreenPtr)+x+y*ScrWidth;
	__asm{
		push  esi
		push  edi
		mov   edi,sofs
		mov   esi,Pix
		mov   cl,byte ptr Lx
		mov   ch,byte ptr Ly
		cld
		xor   eax,eax
Lpp1:
		mov   ah,[esi]
		shr   ah,5
		add   ah,12

		cmp   ah,16
		je    Lpp2
		mov   al,[edi]
		
		mov   al,[InvBrFog+eax]
		mov   [edi],al
Lpp2:	inc   edi
		inc   esi
		dec   cl
		jnz   Lpp1
		add   edi,ScrWidth
		sub   edi,Lx
		mov   cl,byte ptr Lx
		dec   ch
		jnz   Lpp1
		pop   edi
		pop   esi
	};
};
void DrawPixShadowArrayA(int x,int y,byte* Pix,int Lx,int Ly,byte Alpha){
	switch(Alpha){
	case 8:
		DrawPixShadowArray8(x,y,Pix,Lx,Ly);
		break;
	case 7:
		DrawPixShadowArray7(x,y,Pix,Lx,Ly);
		break;
	case 6:
		DrawPixShadowArray6(x,y,Pix,Lx,Ly);
		break;
	case 5:
		DrawPixShadowArray5(x,y,Pix,Lx,Ly);
		break;
	case 4:
		DrawPixShadowArray4(x,y,Pix,Lx,Ly);
		break;
	case 3:
		DrawPixShadowArray3(x,y,Pix,Lx,Ly);
		break;
	case 2:
		DrawPixShadowArray2(x,y,Pix,Lx,Ly);
		break;
	};
};

int GetGroundIntoBuf(int x,int y,int x1,int y1,int xr,int yr,byte** ptr){
	int size=0;
	int MaxSize=0;
	*ptr=NULL;
	int NP=(x1-x+1)*(y1-y+1);
	AWPTR(x1-x+1);
	AWPTR(y1-y+1);
	AWPTR(xr-x);
	AWPTR(yr-y);
	AWPTR(xr);
	AWPTR(yr);
	int z00=yr<<6;
	for(int ix=x;ix<=x1;ix++){
		for(int iy=y;iy<=y1;iy++){
			int z0=Prop43(ConvScrY(ix<<5,iy<<5))-z00;
			int ofs=ix+iy*PT_Lx;
			ABPTR(PointTexWeight[ofs]);
			AWPTR(PointTexIndex[ofs]);
			AWPTR(z0);
		};
	};
	return size;
};
void SetGroundFromBuf(int xr,int yr,byte* Buf,int size,int DZ){
	if(!TempZBuffer)AllocTZBuf();
	int nx=((short*)Buf)[0];
	Buf+=2;
	int ny=((short*)Buf)[0];
	Buf+=2;
	int dx=((short*)Buf)[0];
	Buf+=2;
	int dy=((short*)Buf)[0];
	Buf+=2;
	int dx0=((short*)Buf)[0];
	Buf+=2;
	int dy0=((short*)Buf)[0];
	Buf+=2;
	int sx=dx0-xr;
	int sy=dy0-yr;
	byte* B0=Buf;
	sy-=DZ;
	int pdy=DZ;//(dz+16)>>5;
	for(int ix=0;ix<nx;ix++){
		for(int iy=0;iy<ny;iy++){
			int cx=xr+ix-dx;
			int cy=yr+iy-dy;
			int zp=GetTZValue(cx,cy);
			char W=Buf[0];
			Buf++;
			int Idx=((short*)Buf)[0];
			int idx_x=Idx&7;
			int idx_y=(Idx&63)>>3;
			int idx_t=Idx>>6;
			Idx=(idx_t<<6)+((idx_x+sx)&7)+(((idx_y+sy)&7)<<3);
			Buf+=2;
			int z0=((short*)Buf)[0]+(yr<<6);
			Buf+=2;
			if(z0-zp>0){
				int ofst=cx+(cy-pdy)*PT_Lx;
				if(ofst>=0&&W>0){
					PointTexWeight[ofst]=W;
					PointTexIndex[ofst]=Idx;
				};
				//SetTZValue(cx,cy,z0);
			};
		};
	};
	Buf=B0;
	for(ix=0;ix<nx;ix++){
		for(int iy=0;iy<ny;iy++){
			int cx=xr+ix-dx;
			int cy=yr+iy-dy;
			int zp=GetTZValue(cx,cy);
			byte W=Buf[0];
			Buf++;
			int Idx=((short*)Buf)[0];
			Buf+=2;
			int z0=((short*)Buf)[0]+(yr<<6);
			Buf+=2;
			if(z0-zp>0){
				SetTZValue(cx,cy,z0);
			};
		};
	};
};

//------------------------Sea waves----------------------//
struct OneWave{
	bool Visible;
	int x,y;
	word BORNCELL;
	char GrDx;
	char GrDy;
	int CurSpr;
	int Speed;
	byte dir;
	byte Phase;
};
#define STBORN 188
#define WROWLEN 9
#define NWavesInGroup (WROWLEN*2+1)
word BORNTEST[32768];
class OneWavesGroup{
public:
	OneWave WAVE[NWavesInGroup];
	int  xc,yc;
	bool Exists:1;
	bool NowVisible:1;

	bool PerformOneStep();
	bool BornAt(int x,int y);
	bool PerformEvolution(int dt);
};
inline int GetHFN(int x,int y){
	return ((x>>6)+((y>>6)<<TopSH))&32767;
};
word GetDir(int dx,int dy);
bool OneWavesGroup::BornAt(int x,int y){
	int DL=GetDeep2(x-16,y);
	int DR=GetDeep2(x+16,y);
	int DU=GetDeep2(x,y-16);
	int DD=GetDeep2(x,y+16);
	int DX=DR-DL;
	int DY=DD-DU;
	float Phase=rand();
	if(DX||DY){
		byte dir=GetDir(-DX,-DY)+(rand()&15)-7;
		int RX=(TSin[dir]*3)>>5;
		int RY=(TCos[dir]*3)>>5;
		int p=0;
		int XT=0;
		int YT=0;
		for(int dx=-WROWLEN*8;dx<=WROWLEN*8;dx+=8){
			int vv=(dx*dx)>>6;
			int DD=dx+(rand()&7)-3;
			int DX=(DD*RX)/9;
			int DY=(DD*RY)/9;
			int Speed;
			if(abs(RY+RY)>abs(RX)){
				Speed=256+sin((Phase+dx)/20.0)*20;
			}else Speed=256+sin((Phase+dx)/10.0)*32;

			int x0=x+DX+(rand()&1)-(vv*RY)/64;
			int y0=y-DY+(rand()&1)-(vv*RX)/64;

			XT+=x0;
			YT+=y0;
			WAVE[p].Visible=1;
			WAVE[p].CurSpr=-1;
			WAVE[p].BORNCELL=GetHFN(x0,y0);
			WAVE[p].dir=dir+128;
			WAVE[p].GrDx=0;
			WAVE[p].GrDy=0;
			WAVE[p].Phase=dx*dx/90;
			WAVE[p].Speed=Speed;
			WAVE[p].x=x0<<8;
			WAVE[p].y=y0<<8;
			BORNTEST[WAVE[p].BORNCELL]+=80-WAVE[p].CurSpr;
			p++;
		};
		xc=XT/p;
		yc=YT/p;
		Exists=1;
		return true;
	}else return false;
};
bool OneWavesGroup::PerformOneStep(){
	int vx0=-int(TCos[WAVE->dir])<<1;
	int vy0=-int(TSin[WAVE->dir])<<1;
	int NV=0;
	for(int i=0;i<NWavesInGroup;i++){
		OneWave* OW=WAVE+i;
		if(OW->Visible){
			NV++;
			if(OW->CurSpr<80)BORNTEST[OW->BORNCELL]-=80-OW->CurSpr;
			int vx,vy;
			if(OW->CurSpr<100){
				int xx=OW->x>>8;
				int yy=OW->y>>8;
				int deep=GetApproxDeep(xx,yy);
				if(deep<STBORN)deep=GetDeep2(xx,yy)+127;
				//OW->CurSpr=(50-(deep-125))*2;
				OW->CurSpr=(STBORN-deep-8)*100/(STBORN-128-8);
				vx=(vx0*OW->Speed)>>8;
				vy=(vy0*OW->Speed)>>8;
			}else{;
				OW->CurSpr+=2;
				vx=-vx0>>2;
				vy=-vy0>>2;
			};
			OW->x+=vx;
			OW->y+=vy;
			if(OW->CurSpr<80)BORNTEST[OW->BORNCELL]+=80-OW->CurSpr;
			if(OW->CurSpr>200)OW->Visible=0;
		};
	};
	Exists=NV!=0;
	return true;
};
class SeaWaves{
public:
	//word BORNTEST[8192];
	short* BORNAREAS;
	int LastResearchTime;
	int MaxBAreas;
	int NBAreas;

	int WAVSH;
	int WAVLX;
	int WAVSZ;

	OneWavesGroup* WGRP;
	int NGroups;
	int MaxGroups;

	SeaWaves();
	~SeaWaves();
	void Process();
	void Draw();
	void CreateRandomWavesSet();
	void ResearchBornAreas();
};
extern byte* WaterBright;

void SeaWaves::ResearchBornAreas(){
	NBAreas=0;
	int nx=256<<ADDSH;
	int cell=0;
	for(int iy=0;iy<nx;iy++){
		for(int ix=0;ix<nx;ix++){
			int DP=WaterDeep[cell];
			int WB=WaterBright[cell];
			if(DP<STBORN&&DP>STBORN-3&&WB){
				if(NBAreas>=MaxBAreas){
					MaxBAreas+=4096;
					BORNAREAS=(short*)realloc(BORNAREAS,MaxBAreas<<2);
				};
				int c=NBAreas<<1;
				BORNAREAS[c]=ix;
				BORNAREAS[c+1]=iy;
				NBAreas++;
			};
			cell++;
		};
	};
	LastResearchTime=GetTickCount();
};
SeaWaves::SeaWaves(){
	memset(this,0,sizeof SeaWaves);
	memset(BORNTEST,0,sizeof BORNTEST);
};
SeaWaves::~SeaWaves(){
};

void SeaWaves::CreateRandomWavesSet(){
	if(!NBAreas)return;
	int area=rand()%NBAreas;
	area<<=1;
	int x=BORNAREAS[area]<<5;
	int y=BORNAREAS[area+1]<<5;
	int s=BORNTEST[GetHFN(x ,y  )]
		 +BORNTEST[GetHFN(x+64,y)]
		 +BORNTEST[GetHFN(x-64,y)]
		 +BORNTEST[GetHFN(x,y-64)]
		 +BORNTEST[GetHFN(x,y+64)];
	
	if(s>160)return;
	OneWavesGroup WGR;
	if(WGR.BornAt(x,y)){
		if(NGroups>=MaxGroups){
			MaxGroups+=128;
			WGRP=(OneWavesGroup*)realloc(WGRP,MaxGroups*sizeof OneWavesGroup);
		};
		WGRP[NGroups]=WGR;
		NGroups++;
	};
};
int WATER_GP=0;
int TIME1;
void SeaWaves::Process(){
	//TIME1=GetTickCount();
	if(!WATER_GP){
		WATER_GP=GPS.PreLoadGPImage("priboy");
	};
	int minx=mapx<<5;
	int miny=mapy<<5;
	int maxx=(mapx+smaplx)<<5;
	int maxy=(mapy+smaply)<<5;

	int MinGX=minx-356;
	int MinGY=miny-356;
	int MaxGX=maxx+356;
	int MaxGY=maxy+356;

	for(int i=0;i<NGroups;i++){
		OneWavesGroup* OWG=WGRP+i;
		if(OWG->xc>MinGX&&OWG->xc<MaxGX&&OWG->yc>MinGY&&OWG->yc<MaxGY){
			int NVIS=0;
			for(int j=0;j<NWavesInGroup;j++){
				OneWave* OW=OWG->WAVE+j;
				int wx=(OW->x>>8)+OW->GrDx;
				int wy=(OW->y>>8)+OW->GrDy;
				if(wx>minx&&wy>miny&&wx<maxx&&wy<maxy){
					NVIS++;
					if(OW->CurSpr>=0){
						byte Dir=OW->dir+128;
						if(Dir>=64&&Dir<=64+128){
							int dd1=(Dir-64)>>2;
							int sp=OW->CurSpr;
							if(sp<100)sp=(sp*40)/100;
							else sp=39-(((sp-100)*40)/100);
							int ph=OW->Phase;
							sp=sp*(100-ph)/100;
							if(sp>=0){
								int spr=sp+dd1*40;
								if(LMode){
									GPS.ShowGP((wx-minx-90)>>2,(((wy-miny)>>1)-100)>>2, WATER_GP ,spr,0);
								}else{
									//startTrans=-1024;//((sp*16/39)<<8);
									GPS.ShowGP(wx-minx-90,((wy-miny)>>1)-100,WATER_GP,spr,0);
								};
							};
						}else{
							int dd1;
							if(Dir<64)dd1=(64-Dir)>>2;
							else dd1=16+((255-Dir)>>2);
							int sp=OW->CurSpr;
							if(sp<100)sp=(sp*40)/100;
							else sp=39-(((sp-100)*40)/100);
							int ph=OW->Phase;
							sp=sp*(100-ph)/100;
							if(sp>0){
								int spr=sp+dd1*40;
								if(LMode){
									GPS.ShowGP((wx-minx+90)>>2,(((wy-miny)>>1)-100)>>2, WATER_GP ,spr+4096,0);
								}else{
									//startTrans=-1024;
									//startTrans=-((sp*16/39)<<8);
									GPS.ShowGP(wx-minx+90,((wy-miny)>>1)-100,WATER_GP,spr+4096,0);
								};
							};
						};
					};
				};
			};
			if(NVIS){
				WGRP[i].PerformOneStep();
				if(!WGRP[i].Exists){
					if(i<NGroups-1){
						memcpy(WGRP+i,WGRP+i+1,(NGroups-i-1)*sizeof OneWavesGroup);
					};
					i--;
					NGroups--;
				};
			};
		};
	};
	//TIME1=GetTickCount()-TIME1;
};

void SeaWaves::Draw(){
	
};
SeaWaves WAVES;
void ClearAllWaves(){
	WAVES.NGroups=0;
	WAVES.NBAreas=0;
};
void PROSESS_WAVES(){
	int NN=WAVES.NBAreas>>2;
	for(int i=0;i<NN;i++)WAVES.CreateRandomWavesSet();
	if(GetKeyState(VK_CONTROL)&0x8000)return;
	//WAVES.Process();
	if((!WAVES.LastResearchTime)||GetTickCount()-WAVES.LastResearchTime>30000){
		WAVES.ResearchBornAreas();
		WAVES.LastResearchTime=GetTickCount();
	};

};
void RESEARCH_WAVES(){
	WAVES.ResearchBornAreas();
	WAVES.LastResearchTime=GetTickCount();
};
void DRAW_WAVES(){
#ifndef _USE3D
	WAVES.Process();
#endif //_USE3D
	//WAVES.Draw();
};
//------------------------Mini-screen functions------------------//
void DrawMiniSubWaterSquare(byte* Buf,int x,int y,int cx,int cy,int BufWidth){
#ifndef CONQUEST
	return;
#endif
	int ofs=cx+(cy*MaxWX*2);
	int z1=int(WaterDeep[ofs])-127;
	int z2=int(WaterDeep[ofs+1])-127;
	int z3=int(WaterDeep[ofs+MaxWX+MaxWX])-127;
	int z4=int(WaterDeep[ofs+MaxWX+MaxWX+1])-127;
	if(z1<0&&z2<0&&z3<0)return;
	TempWindow TW;
	PushWindow(&TW);
	int ww=ScrWidth;
	void* ptr=ScreenPtr;
	ScreenPtr=Buf;
	ScrWidth=BufWidth;
	int ofs0=int(Buf)+x+(y*BufWidth);

	z1<<=10;
	z2<<=10;
	z3<<=10;
	z4<<=10;
	int dzx=(z2-z1)>>3;
	int dzy=(z3-z1)>>3;
	int dxx=(z1+z4-z3-z2)>>6;

	int sdx=ScrWidth-8;

	__asm{
		push esi
		push edi
		mov  edi,ofs0
		mov  ebx,z1
		mov  ecx,0x0808
		xor  eax,eax
zzz1:
		mov  ah,[edi]
		mov  edx,ebx
		cmp  edx,0
		jge  rrr1
		mov  edx,0
rrr1:	cmp  edx,65000
		jle  rrr2
		mov  edx,65000
rrr2:
		mov  al,dh
		and  al,0xF0
		mov  al,[WaterCost+eax+1]
		mov  [edi],al
		add  ebx,dzx
		inc  edi
		dec  cl
		jnz  zzz1
		mov  ebx,dzx
		add  ebx,dxx
		mov  dzx,ebx
		mov  ebx,z1
		add  ebx,dzy
		mov  z1,ebx
		mov  cl,8
		add  edi,sdx
		dec  ch
		jnz  zzz1

		pop  edi
		pop  esi
	};
	PopWindow(&TW);
	ScreenPtr=ptr;
};
void DrawMiniMaskOnScreen(byte* Buf,int x,int y,int cx,int cy,int BufWidth){
	int w1=GetMiniTWeight(cx,cy);
	int w2=GetMiniTWeight(cx+1,cy);
	int w3=GetMiniTWeight(cx,cy+1);
	int w4=GetMiniTWeight(cx+1,cy+1);
	
	if(!(w1>0||w2>0||w3>0||w4>0))return;
	byte TMP[1024];
	memset(TMP,15,1024);
	int tex1=GetPTexture(cx,cy);
	int tex2=GetPTexture(cx+1,cy);
	int tex3=GetPTexture(cx,cy+1);
	int tex4=GetPTexture(cx+1,cy+1);
	bool UNITEX=tex1==tex2&&tex1==tex3&&tex1==tex4;
	if(tex1<TCOVER.NCTex){
		int tx=(cx+(tex1&7))&7;
		int ty=(cy+((tex1>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex1&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,CW(w1),tex1==tex2?CW(w2):0,tex1==tex3?CW(w3):0,tex1==tex4?CW(w4):0);
	};
	if(tex2<TCOVER.NCTex&&tex2!=tex1){
		int tx=(cx+(tex2&7))&7;
		int ty=(cy+((tex2>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex2&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,tex2==tex1?CW(w1):0,CW(w2),tex2==tex3?CW(w3):0,tex2==tex4?CW(w4):0);
	};
	if(tex3<TCOVER.NCTex&&tex3!=tex2&&tex3!=tex1){
		int tx=(cx+(tex3&7))&7;
		int ty=(cy+((tex3>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex3&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,tex3==tex1?CW(w1):0,tex3==tex2?CW(w2):0,CW(w3),tex3==tex4?CW(w4):0);
	};
	if(tex4<TCOVER.NCTex&&tex4!=tex3&&tex4!=tex2&&tex4!=tex3){
		int tx=(cx+(tex4&7))&7;
		int ty=(cy+((tex4>>3)&7))&7;
		PutWeightToTexture(TCOVER.TxCash[(tex4&0xFFFC0)+tx+(ty<<3)]->TexPix,TMP,tex4==tex1?CW(w1):0,tex4==tex2?CW(w2):0,tex4==tex3?CW(w3):0,CW(w4));
	};
	// :-)  ready to stamp this ...(piece)
	TempWindow TW;
	PushWindow(&TW);
	int ww=ScrWidth;
	void* ptr=ScreenPtr;
	ScreenPtr=Buf;
	ScrWidth=BufWidth;
	int ofs0=int(Buf)+x+(y*BufWidth);
	int DDX=BufWidth-8;
	int tofs=int(TMP);
	__asm{
		push esi
		push edi
		mov  esi,tofs
		mov  edi,ofs0
		mov	 cx,0x0808
		xor  eax,eax
DOPC1:	lodsb
		add  al,[esi+1]
		add  al,[esi+2]
		add  al,[esi+3]
		shr  al,2
		add  esi,3
		cmp  al,31
		jl   TT1
		mov  al,31
TT1:    cmp  al,0
		jge  TT2
		mov  al,0
TT2:	mov  ah,al
		//shl  ah,1
		mov  al,[edi]
		and  ah,0x1F;
		mov  bl,[InvBrFog+eax]
		mov  [edi],bl
		inc  edi
		dec  cl
		jnz  DOPC1
		add  edi,DDX
		add  esi,32*3
		mov  cl,0x08
		dec  ch
		jnz  DOPC1
		pop  edi
		pop  esi
	};
	PopWindow(&TW);
	ScreenPtr=ptr;
};
