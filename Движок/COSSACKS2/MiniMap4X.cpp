#include "stdheader.h"
#include "DrawModels.h"
#include "kContext.h"
#include "IShadowManager.h"
#include "UnitAbility.h"

#ifdef _USE3D
#pragma pack (push)
#pragma pack (8)

#include "Scape3D.h"
#pragma pack (pop)
#endif // _USE3D

#define M4CellLX 32
#define M4CellLY 32

byte* Tmp4XBuf=NULL;
short* TmpOwnerX=NULL;
short* TmpOwnerY=NULL;

int CurTmpPos=0;
int MaxTmpPos=0;

byte** ReadyMap=NULL;
word*  PosMap=NULL;
byte * MiniMap=NULL;
//byte*  ReadyMap=NULL;
int OFSTBL[4096];
int MM_Lx;
int MM_Ly;
int MM_Y0;
int MMSZ=0;
int MMR_Lx;
int MMR_Ly;
//V1--\
//     ----\
//          ===V3
//     ----/
//V2--/
extern short randoma[8192];
int GPLP=0;
#define SETPX1 \
	off=ofs-OFSTBL[64+(H>>8)];\
	c0=MiniMap[off];\
	if(V>170*256)c=TexPtr[tofs];\
	else if(V>85*256)c=trans8[TexPtr[tofs]+(c0<<8)];\
	if(V>85*256){\
		MiniMap[off]=c;\
		//assert(c!=232);\
	};
#define SETPX \
	off=ofs-OFSTBL[64+(H>>8)];\
	if(off>=0&&off<MMSZ){\
		c0=MiniMap[off];\
		if(V>190*256)c=TexPtr[tofs];\
		else if(V>153*256)c=trans4[(int(TexPtr[tofs])<<8)+c0];\
		else if(V>102*256)c=trans8[TexPtr[tofs]+(c0<<8)];\
		else if(V>65*256 )c=trans4[TexPtr[tofs]+(c0<<8)];\
		if(V>51*256)MiniMap[off]=c;\
	};
#define SETLIT \
	off=ofs-OFSTBL[64+(H>>8)];\
	if(off>=0&&off<MMSZ){\
		MiniMap[off]=darkfog[16384+MiniMap[off]+(L&0xFF00)+(randoma[GPLP&8191]&0x100)-256];\
		GPLP++;\
	};
	
/*

#define SETPX \
	off=ofs-OFSTBL[H>>8];\
	c=MiniMap[off];\
	if(V>128)c=TexPtr[tofs];\
	MiniMap[off]=c;//darkfog[16384+c+(L&0xFF00)];\
	assert(V>=-3);
*/

DWORD GetNatColor( int );

/*---------------------------------------------------------------------------*/
/*	Func:	FindRelativeZ	
/*	Desc:	Projects point (px, py) from screen space onto the ground plane
/*			and finds its z-value relatively to the (cx, cy, 0)
/*---------------------------------------------------------------------------*/
float FindRelativeZ( float px, float py, float cx, float cy )
{
	return c_Cos30 * (cy - py); 
}

int mrand();
extern byte trans4[65536];
extern byte trans8[65536];
extern byte darkfog[40960];
void DrawTriangleR(int x,int y,int H1,int H2,int H3,
				   int L1,int L2,int L3,int V1,int V2,int V3,
				   byte* TexPtr){
	GPLP+=rand();
	//assert(V1==255&&V2==255&V3==255);
	//assert(!(V1>0&&V2>0&&V3<255));
	int ofs=x+(MM_Y0+y)*MM_Lx;
	L1<<=8;
	L2<<=8;
	L3<<=8;
	H1<<=8;
	H2<<=8;
	H3<<=8;
	V1<<=8;
	V2<<=8;
	V3<<=8;

	int Dvx=(V3-((V1+V2)>>1))>>3;
	int Dvy=(V2-V1)>>3;
	int DLx=(L3-((L1+L2)>>1))>>3;
	int DLy=(L2-L1)>>3;
	int DHx=(H3-((H1+H2)>>1))>>3;
	int DHy=(H2-H1)>>3;
	int L=L1;
	int H=H1;
	int V=V1;
	int tofs=(mrand()&15)+((mrand()&15)<<8);

	int off;
	byte c;
	int c0;

	for(int i=0;i<8;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
	tofs+=1-512;
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	V+=Dvx-Dvy-Dvy;
	ofs+=1-MM_Lx-MM_Lx;
	for(i=0;i<6;i++){
		SETPX;
		tofs-=256;
		L-=DLy;
		H-=DHy;
		V-=Dvy;
		ofs-=MM_Lx;
	};
	tofs+=1+256;
	L+=DLx+DLy;
	H+=DHx+DHy;
	V+=Dvx+Dvy;
	ofs+=MM_Lx+1;
	for(i=0;i<6;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
	tofs+=1-512;
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	V+=Dvx-Dvy-Dvy;
	ofs+=1-MM_Lx-MM_Lx;
	for(i=0;i<4;i++){
		SETPX;
		tofs-=256;
		L-=DLy;
		H-=DHy;
		V-=Dvy;
		ofs-=MM_Lx;
	};
	tofs+=1+256;
	L+=DLx+DLy;
	H+=DHx+DHy;
	V+=Dvx+Dvy;
	ofs+=MM_Lx+1;
	for(i=0;i<4;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
	tofs+=1-512;
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	V+=Dvx-Dvy-Dvy;
	ofs+=1-MM_Lx-MM_Lx;
	for(i=0;i<2;i++){
		SETPX;
		tofs-=256;
		L-=DLy;
		H-=DHy;
		V-=Dvy;
		ofs-=MM_Lx;
	};
	tofs+=1+256;
	L+=DLx+DLy;
	H+=DHx+DHy;
	V+=Dvx+Dvy;
	ofs+=MM_Lx+1;
	for(i=0;i<2;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};

};
//            V1
//      .     .
//V3          .
//      .     .
//            V2
void DrawTriangleL(int x,int y,int H1,int H2,int H3,
				   int L1,int L2,int L3,int V1,int V2,int V3,
				   byte* TexPtr){
	GPLP+=rand();
	//assert(V1==255&&V2==255&V3==255);
	int ofs=x+(MM_Y0+y)*MM_Lx;
	L1<<=8;
	L2<<=8;
	L3<<=8;
	H1<<=8;
	H2<<=8;
	H3<<=8;
	V1<<=8;
	V2<<=8;
	V3<<=8;
	int Dvx=(V3-((V1+V2)>>1))>>3;
	int Dvy=(V2-V1)>>3;
	int DLx=(L3-((L1+L2)>>1))>>3;
	int DLy=(L2-L1)>>3;
	int DHx=(H3-((H1+H2)>>1))>>3;
	int DHy=(H2-H1)>>3;
	int L=L1;
	int H=H1;
	int V=V1;
	int tofs=(mrand()&15)+((mrand()&15)<<8);

	int off;
	byte c;
	int c0;
	ofs--;
	V+=Dvx;

	for(int i=0;i<8;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
	tofs+=1-512;
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	V+=Dvx-Dvy-Dvy;
	ofs+=-1-MM_Lx-MM_Lx;
	for(i=0;i<6;i++){
		SETPX;
		tofs-=256;
		L-=DLy;
		H-=DHy;
		V-=Dvy;
		ofs-=MM_Lx;
	};
	tofs+=1+256;
	L+=DLx+DLy;
	H+=DHx+DHy;
	V+=Dvx+Dvy;
	ofs+=MM_Lx-1;
	for(i=0;i<6;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
	tofs+=1-512;
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	V+=Dvx-Dvy-Dvy;
	ofs+=-1-MM_Lx-MM_Lx;
	for(i=0;i<4;i++){
		SETPX;
		tofs-=256;
		L-=DLy;
		H-=DHy;
		V-=Dvy;
		ofs-=MM_Lx;
	};
	tofs+=1+256;
	L+=DLx+DLy;
	H+=DHx+DHy;
	V+=Dvx+Dvy;
	ofs+=MM_Lx-1;
	for(i=0;i<4;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
	tofs+=1-512;
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	V+=Dvx-Dvy-Dvy;
	ofs+=-1-MM_Lx-MM_Lx;
	for(i=0;i<2;i++){
		SETPX;
		tofs-=256;
		L-=DLy;
		H-=DHy;
		V-=Dvy;
		ofs-=MM_Lx;
	};
	tofs+=1+256;
	L+=DLx+DLy;
	H+=DHx+DHy;
	V+=Dvx+Dvy;
	ofs+=MM_Lx-1;
	for(i=0;i<2;i++){
		SETPX;	
		tofs+=256;
		L+=DLy;
		H+=DHy;
		V+=Dvy;
		ofs+=MM_Lx;
	};
};
void LiteTriangleR(int x,int y,int H1,int H2,int H3,
				   int L1,int L2,int L3){
	//assert(V1==255&&V2==255&V3==255);
	//assert(!(V1>0&&V2>0&&V3<255));
	int ofs=x+(MM_Y0+y)*MM_Lx;
	L1<<=8;
	L2<<=8;
	L3<<=8;
	H1<<=8;
	H2<<=8;
	H3<<=8;

	int DLx=(L3-((L1+L2)>>1))>>3;
	int DLy=(L2-L1)>>3;
	int DHx=(H3-((H1+H2)>>1))>>3;
	int DHy=(H2-H1)>>3;
	int L=L1;
	int H=H1;

	int off;
	byte c;
	int c0;

	for(int i=0;i<8;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	ofs+=1-MM_Lx-MM_Lx;
	for(i=0;i<6;i++){
		SETLIT;	
		L-=DLy;
		H-=DHy;
		ofs-=MM_Lx;
	};
	L+=DLx+DLy;
	H+=DHx+DHy;
	ofs+=MM_Lx+1;
	for(i=0;i<6;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	ofs+=1-MM_Lx-MM_Lx;
	for(i=0;i<4;i++){
		SETLIT;	
		L-=DLy;
		H-=DHy;
		ofs-=MM_Lx;
	};
	L+=DLx+DLy;
	H+=DHx+DHy;
	ofs+=MM_Lx+1;
	for(i=0;i<4;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	ofs+=1-MM_Lx-MM_Lx;
	for(i=0;i<2;i++){
		SETLIT;	
		L-=DLy;
		H-=DHy;
		ofs-=MM_Lx;
	};
	L+=DLx+DLy;
	H+=DHx+DHy;
	ofs+=MM_Lx+1;
	for(i=0;i<2;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};

};
//            V1
//      .     .
//V3          .
//      .     .
//            V2
void LiteTriangleL(int x,int y,int H1,int H2,int H3,
				   int L1,int L2,int L3){
	//assert(V1==255&&V2==255&V3==255);
	int ofs=x+(MM_Y0+y)*MM_Lx;
	L1<<=8;
	L2<<=8;
	L3<<=8;
	H1<<=8;
	H2<<=8;
	H3<<=8;
	int DLx=(L3-((L1+L2)>>1))>>3;
	int DLy=(L2-L1)>>3;
	int DHx=(H3-((H1+H2)>>1))>>3;
	int DHy=(H2-H1)>>3;
	int L=L1;
	int H=H1;

	int off;
	byte c;
	int c0;
	ofs--;

	for(int i=0;i<8;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	ofs+=-1-MM_Lx-MM_Lx;
	for(i=0;i<6;i++){
		SETLIT;	
		L-=DLy;
		H-=DHy;
		ofs-=MM_Lx;
	};
	L+=DLx+DLy;
	H+=DHx+DHy;
	ofs+=MM_Lx-1;
	for(i=0;i<6;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	ofs+=-1-MM_Lx-MM_Lx;
	for(i=0;i<4;i++){
		SETLIT;	
		L-=DLy;
		H-=DHy;
		ofs-=MM_Lx;
	};
	L+=DLx+DLy;
	H+=DHx+DHy;
	ofs+=MM_Lx-1;
	for(i=0;i<4;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
	L+=DLx-DLy-DLy;
	H+=DHx-DHy-DHy;
	ofs+=-1-MM_Lx-MM_Lx;
	for(i=0;i<2;i++){
		SETLIT;	
		L-=DLy;
		H-=DHy;
		ofs-=MM_Lx;
	};
	L+=DLx+DLy;
	H+=DHx+DHy;
	ofs+=MM_Lx-1;
	for(i=0;i<2;i++){
		SETLIT;	
		L+=DLy;
		H+=DHy;
		ofs+=MM_Lx;
	};
};
#define VMAX1 (255-(randoma[X1&8191]&127))
#define VMAX2 (255-(randoma[X2&8191]&127))
#define VMAX3 (255-(randoma[X3&8191]&127))
#define VMIN1 (randoma[X1&8191]&127)
#define VMIN2 (randoma[X2&8191]&127)
#define VMIN3 (randoma[X3&8191]&127)
int GetBmOfst(int i);
void DrawTriR(int x,int y,
			  int H1,int H2,int H3,
			  int L1,int L2,int L3,
			  int T1,int T2,int T3,
			  int X1,int X2,int X3){};
void DrawTriL(int x,int y,
			  int H1,int H2,int H3,
			  int L1,int L2,int L3,
			  int T1,int T2,int T3,
			  int X1,int X2,int X3){};
void SaveBMP8(char* Name,int lx,int ly,byte* Data);
void DrawMiniMaskOnScreen(byte* Buf,int x,int y,int cx,int cy,int BufWidth);
void DrawMiniSubWaterSquare(byte* Buf,int x,int y,int cx,int cy,int BufWidth);
extern int MAXSPR;
struct OneSprInfo{
	short x,y,file,spr;
};
#define MAXLY 3000
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
SmallZBuffer::SmallZBuffer(int Ny){
	//memset(this,0,sizeof* this);
	//SPRS=(OneSprInfo**)malloc(4*Ny);
	memset(SPRS,0,4*Ny);
	//NSprs=(int*)malloc(4*Ny);
	memset(NSprs,0,4*Ny);
	//MaxSprs=(int*)malloc(4*Ny);
	memset(MaxSprs,0,4*Ny);
	MaxLines=Ny;
};
SmallZBuffer::~SmallZBuffer(){
	for(int i=0;i<MaxLines;i++){
		if(SPRS[i])free(SPRS[i]);
		SPRS[i]=NULL;
	};
	//if(SPRS)free(SPRS);
	//if(NSprs)free(NSprs);
	//if(MaxSprs)free(MaxSprs);
};
void SmallZBuffer::Add(int y,int sx,int sy,int FID,int spr){
	if(y>=0&&y<MaxLines){
		if(NSprs[y]>=MaxSprs[y]){
			MaxSprs[y]+=4;
			SPRS[y]=(OneSprInfo*)realloc(SPRS[y],MaxSprs[y]*sizeof OneSprInfo);
		};
		OneSprInfo* OSI=SPRS[y]+NSprs[y];
		NSprs[y]++;
		OSI->file=FID;
		OSI->spr=spr;
		OSI->x=sx;
		OSI->y=sy;
	};
};
int NDR=0;
void SmallZBuffer::Draw(){
	NDR=0;
	for(int i=0;i<MaxLines;i++){
		int N=NSprs[i];
		if(N){
			OneSprInfo* OSI=SPRS[i];
			for(int j=0;j<N;j++){
				GPS.ShowGP(OSI[j].x,OSI[j].y,OSI[j].file,OSI[j].spr,0);
				NDR++;
			};
		};
	};
};
int RSIZE;
void GenerateMiniMap(){
//#ifdef _USE3D
//	return;
//#endif // _USE3D

	TempWindow TW;
	PushWindow(&TW);
	void* sptr=ScreenPtr;
	MM_Lx=(1920<<ADDSH)+2;
	MM_Ly=(MM_Lx>>1)+2;
	MM_Y0=64;
	MMR_Lx=(MM_Lx/M4CellLX)+1;
	MMR_Ly=(MM_Ly/M4CellLY)+1;
	if(!Tmp4XBuf){
		MaxTmpPos=8000000/(M4CellLX*M4CellLY);
		Tmp4XBuf=(byte*)malloc(MaxTmpPos*M4CellLX*M4CellLY);
		TmpOwnerX=(short*)malloc(MaxTmpPos*2);
		TmpOwnerY=(short*)malloc(MaxTmpPos*2);
	};
	if(!ReadyMap){
		ReadyMap=(byte**)malloc(MMR_Lx*MMR_Ly*4);
		PosMap=(word*)malloc(MMR_Lx*MMR_Ly*2);
	};
	memset(ReadyMap,0,MMR_Lx*MMR_Ly*4);
	memset(PosMap,0xFF,MMR_Lx*MMR_Ly*2);
	memset(TmpOwnerX,0xFF,MaxTmpPos*2);
	memset(TmpOwnerY,0xFF,MaxTmpPos*2);
	CurTmpPos=0;
	MMSZ=MM_Lx*(MM_Ly*2+MM_Y0*2);
	RSIZE=MMR_Lx*MMR_Ly;
	return;
};

byte* ReserveMemoryFor4X(word x,word y){
	if(TmpOwnerX[CurTmpPos]!=-1){
		int ofs=int(TmpOwnerX[CurTmpPos])+int(TmpOwnerY[CurTmpPos])*MMR_Lx;
		//assert(ofs>=0&&ofs<RSIZE);
		ReadyMap[ofs]=NULL;
		PosMap[ofs]=0xFFFF;
	};
	TmpOwnerX[CurTmpPos]=x;
	TmpOwnerY[CurTmpPos]=y;
	byte* mem=Tmp4XBuf+CurTmpPos*M4CellLX*M4CellLY;
	int ofs=int(x)+int(y)*MMR_Lx;
	ReadyMap[ofs]=mem;
	PosMap[ofs]=CurTmpPos;
	CurTmpPos++;
	if(CurTmpPos>=MaxTmpPos)CurTmpPos=0;
	return mem;
};

bool LMode=0;
extern int RealLx;
extern int RealLy;
void PrepareSound();


void MakeAllDirtyGBUF();
void ReverseLMode();
void SetLMode(int L){
	if(LMode){
		ReverseLMode();
	};
	if(L){
		int dx=mapx+(mouseX>>5);
		int dy=mapy+(mouseY>>4);
		Shifter=5-L;
		LMode=1;
		//if(!ReadyMap)GenerateMiniMap();
		smaplx<<=(5-Shifter);
		smaply<<=(5-Shifter);
		smapy=RealLy-(smaply*(16>>(5-Shifter)));

		mapx=dx-(smaplx>>1);
		mapy=dy-(smaply>>1);
		if(mapx<=0)mapx=1;
		if(mapy<=0)mapy=1;
		if(mapx+smaplx>msx+1)mapx=msx-smaplx+1;
		if(mapy+smaply>msy+1)mapy=msy-smaply+1;
		SetCursorPos(RealLx>>1,RealLy>>1);
		MakeAllDirtyGBUF();
	};
	mapx=mapx&0xFFFC;
	mapy=mapy&0xFFFC;
	PrepareSound();
};

void RebuildWaterMesh();
void SetupCamera();

void ApplyLMode()
{
    int dx = mapx+(mouseX>>5);
    int dy = mapy+(mouseY>>4);
    Shifter = 4;
    if (GetKeyState(VK_SHIFT)&0x8000) Shifter = 2;
    LMode = 1;
    smaplx <<= (5-Shifter);
    smaply <<= (5-Shifter);
    smapy = RealLy - (smaply*(16>>(5-Shifter)));

    mapx=dx-(smaplx>>1);
    mapy=dy-(smaply>>1);
    if(mapx<=0)mapx=1;
    if(mapy<=0)mapy=1;
    if(mapx+smaplx>msx+1)mapx=msx-smaplx+1;
    if(mapy+smaply>msy+1)mapy=msy-smaply+1;
}

void UnapplyLMode()
{
    int dx=mapx+(mouseX>>(Shifter));
    int dy=mapy+(mouseY>>(Shifter-1));
    LMode=0;
    smaplx>>=(5-Shifter);
    smaply>>=(5-Shifter);
    smapy=RealLy-smaply*16;
    Shifter=5;
    mapx=dx-(smaplx>>1);
    mapy=dy-(smaply>>1);
    if(mapx<=0)mapx=1;
    if(mapy<=0)mapy=1;
    if(mapx+smaplx>msx+1)mapx=msx-smaplx+1;
    if(mapy+smaply>msy+1)mapy=msy-smaply+1;
}

void ReverseLMode(){
    bool bToL;
	if(!LMode){
		ApplyLMode();
        bToL = true;
	}else bToL = false;
		
	mapx = mapx&0xFFFC;
	mapy = mapy&0xFFFC;
    PrepareSound();

    Vector3D moveDir( Vector3D::null );    
    Vector3D start = ScreenToWorldSpace( RealLx/2, RealLy/2 );
    Vector3D end   = ScreenToWorldSpace( mouseX, mouseY );
    moveDir.sub( end, start );
    AnimateLModeSwitch( moveDir, bToL );
    SetupCamera();
    RebuildWaterMesh();
    FillGroundZBuffer();
    AnimateLModeSwitch( moveDir, bToL );
    //SetCursorPos(RealLx>>1,RealLy>>1);
}; // ReverseLMode

extern int RealLx;
extern int RealLy;
int DD=0;
void CheckMM_Change();
void CheckMM4XReady(int x,int y,int Lx,int Ly);
void CopyTo16(int x,int y,byte* Src,int Pitch,int Lx,int Ly);
void DrawOneTerrSquare(byte* offs,int x,int y){
//#ifdef _USE3D
//	return;
//#endif // _USE3D
#ifdef _USE3D
	if (GPS.GetClipArea().IsRectInside( x, y, M4CellLX, M4CellLY ))
	{
		CopyTo16(x,y,offs,M4CellLX,M4CellLX,M4CellLY);
#else
	if(x>=WindX&&y>=WindY&&x+M4CellLX<=WindX1&&y+M4CellLY<=WindY1){
		int mofs=int(ScreenPtr)+x+y*ScrWidth;
		int sdx=ScrWidth-M4CellLX;
		__asm{
			push esi
			push edi
			push ecx
			pushf
			cld
			mov  esi,offs
			mov  edi,mofs
			mov  ebx,M4CellLY
LLP1:		mov  ecx,M4CellLX/4
			rep  movsd
			add  edi,sdx
			dec  ebx
			jnz  LLP1
			popf
			pop  ecx
			pop  edi
			pop  esi
		};
#endif
		return;
	};

#ifndef _USE3D
	if(x+M4CellLX<=WindX||y+M4CellLY<=WindY||x>=WindX1||y>=WindY1)return;
	int txX=0;
	int txY=0;
	int txLx=M4CellLX;
	int txLy=M4CellLY;
	if(x<WindX){
		txX=WindX-x;
		txLx-=txX;
		x=0;
	};
	if(y<WindY){
		txY=WindY-y;
		txLy-=txY;
		y=0;
	};
	if(x+txLx>WindX1){
		txLx=WindX1+1-x;
	};
	if(y+txLy>WindY1){
		txLy=WindY1+1-y;
	};
	if(txX<0||txY<0||txLx<=0||txLy<=0)return;
#else
	const Rct& vp = GPS.GetClipArea();
	if (!vp.IsRectInside( x, y, M4CellLX, M4CellLY )) return;
	int txX=0;
	int txY=0;
	int txLx=M4CellLX;
	int txLy=M4CellLY;
	if(x<vp.x){
		txX=vp.x-x;
		txLx-=txX;
		x=0;
	};
	if(y<vp.y){
		txY=vp.y-y;
		txLy-=txY;
		y=0;
	};
	if(x+txLx>vp.GetRight()){
		txLx=vp.GetRight()+1-x;
	};
	if(y+txLy>vp.GetBottom()){
		txLy=vp.GetBottom()+1-y;
	};
	if(txX<0||txY<0||txLx<=0||txLy<=0)return;
#endif // !_USE3D
	
	int toff=int(offs)+txX+txY*M4CellLX;
#ifdef _USE3D
	CopyTo16(x,y,(byte*)toff,M4CellLX,txLx,M4CellLY);
#else
	int mofs=int(ScreenPtr)+x+y*ScrWidth;
	int sdx=ScrWidth-txLx;
	int adm=M4CellLX-txLx;
	__asm{
		push esi
		push edi
		push ecx
		pushf
		cld
		mov  esi,toff
		mov  edi,mofs
		mov  ebx,txLy
LLP2:	mov  ecx,txLx
		shr  ecx,2
		rep  movsd
		add  esi,adm
		add  edi,sdx
		dec  ebx
		jnz  LLP2
		popf
		pop  ecx
		pop  edi
		pop  esi
	};
#endif
	return;
};

void DrawLGround(){
	CheckMM_Change();
	//if(GetKeyState('Q')&0x8000)DD+=4;
	//if(GetKeyState('W')&0x8000)DD-=4;
	int x0=mapx<<3;
	int y0=mapy<<2;
	int Lx=RealLx-DD;
	int Ly=RealLy;
	CheckMM4XReady(x0,y0,Lx,Ly);
	int cx0=((mapx<<3)/M4CellLX)-1;
	int cy0=((mapy<<2)/M4CellLY)-1;
	int cx1=(((mapx+smaplx)<<3)/M4CellLX)+1;
	int cy1=(((mapy+smaply)<<2)/M4CellLY)+1;
	if(cx0<0)cx0=0;
	if(cy0<0)cy0=0;
	if(cx1>=MMR_Lx)cx1=MMR_Lx-1;
	if(cy1>=MMR_Ly)cy1=MMR_Ly-1;
	for(int ix=cx0;ix<=cx1;ix++){
		for(int iy=cy0;iy<=cy1;iy++){
			int ofs=ix+iy*MMR_Lx;
			if(ReadyMap[ofs])DrawOneTerrSquare(ReadyMap[ofs],ix*M4CellLX-x0,iy*M4CellLY-y0);
		};
	};
};
#define zoomsh (5-(Shifter))
int CLM_Size=8;
int CLM_Shift=2;
int CLM_ShiftY=2;

extern int tmtmt;
#define zoom(x) ((x)>>(5 - (Shifter)))
extern int PortBuiX,PortBuiY;
extern bool NOPAUSE;
void ShowFires(OneObject* OB,int x0,int y0);
void DRAW_WAVES();
void PROSESS_WAVES();
void ShowProducedShip(OneObject* Port,int CX,int CY);

void AddPointEx(short XL,short YL,short x,short y,OneObject* OB,word FileID,word SpriteID,word FileIDex,word SpriteIDex,int Param1,int Param2);
void SetZBias(int b);

int GT0,GT1,GT2,GT3,GT4,GT5;
int PREVLX=0;
byte* BUF=NULL;
int BUFSZ=0;
int StnGP=-1;
int DerGP=-1;
int HolesGP=-1;
void GenerateMiniMapSquare(int x0,int y0,int nx,int ny){
//#ifdef _USE3D
//	return;
//#endif // _USE3D

	if(StnGP==-1){
		StnGP=GPS.PreLoadGPImage("L_Mode\\Stones");
		DerGP=GPS.PreLoadGPImage("L_Mode\\Der");
		HolesGP=GPS.PreLoadGPImage("L_Mode\\Holes");
	};
	int T0=GetTickCount();
	//if(!MiniMap)return;
	
	byte* mptr0=MiniMap;
	
	int LX0=MM_Lx;
	int LY0=MM_Ly;
	int Y0=MM_Y0;
	MM_Lx=nx<<4;
	MM_Ly=ny<<2;
	MM_Y0=256;

	MMSZ=MM_Lx*(MM_Ly+MM_Y0)*2;
	byte* mpart;
	if(MMSZ>BUFSZ){
		mpart=(byte*)realloc(BUF,MMSZ);
		BUF=mpart;
		BUFSZ=MMSZ;
	}else mpart=BUF;
	MiniMap=mpart;

	if(PREVLX!=MM_Lx){
		for(int i=0;i<2000;i++)OFSTBL[i]=(i-64)*MM_Lx;
		PREVLX=MM_Lx;
	};

	TempWindow TW;
	PushWindow(&TW);
	void* sptr=ScreenPtr;

	memset(mpart,0,MMSZ);
	int NX0=x0<<1;
	int NY0=y0;
	int NLX=MM_Lx>>4;
	int NLY=MM_Ly>>2;
	int MYY=(NLY)<<2;
	int ND=0;
	int NND=0;
	GT0=GetTickCount()-T0;
	T0=GetTickCount();
	bool F1=1;
	bool F2=0;
	for(int iy=0;iy<NLY+40&&(F1||F2);iy++){
		F2=0;
		for(int ix=0;ix<NLX;ix++){
			int YY=NY0+iy;
			if(YY>=0&&YY<MaxTH-2){
				int ofs=(NY0+iy)*VertInLine+ix+ix+NX0;
				int VR0=ofs;
				int VR1=ofs+1;
				int VR2=ofs+2;
				int VR3=ofs+VertInLine;
				int VR4=ofs+VertInLine+1;
				int VR5=ofs+VertInLine+2;
				int H0=THMap[VR0]>>1;
				int H1=THMap[VR1]>>1;
				int H2=THMap[VR2]>>1;
				int H3=THMap[VR3]>>1;
				int H4=THMap[VR4]>>1;
				int H5=THMap[VR5]>>1;
				if(H0<0)H0=0;
				if(H1<0)H1=0;
				if(H2<0)H2=0;
				if(H3<0)H3=0;
				if(H4<0)H4=0;
				if(H5<0)H5=0;

				int T0=TexMap[VR0];
				int T1=TexMap[VR1];
				int T2=TexMap[VR2];
				int T3=TexMap[VR3];
				int T4=TexMap[VR4];
				int T5=TexMap[VR5];
				int L0=GetLighting(VR0);
				int L1=GetLighting(VR1);
				int L2=GetLighting(VR2);
				int L3=GetLighting(VR3);
				int L4=GetLighting(VR4);
				int L5=GetLighting(VR5);
				int X0=ix<<4;
				int Y0=iy<<3;
				int YY=Y0>>1;
				int RY0=YY-H0;
				int RY1=YY-2-H1;
				int RY2=YY-H2;
				int RY3=YY+4-H3;
				int RY4=YY+2-H4;
				int RY5=YY+4-H5;
				if(RY0<MYY||RY1<MYY||RY2<MYY||RY3<MYY||RY4<MYY||RY5<MYY){
					F2=1;
					F1=0;
					DrawTriR(X0,Y0,H0,H3,H4,L0,L3,L4,T0,T3,T4,VR0,VR3,VR4);
					DrawTriR(X0+8,Y0-4,H1,H4,H2,L1,L4,L2,T1,T4,T2,VR1,VR4,VR2);
					DrawTriL(X0+8,Y0-4,H1,H4,H0,L1,L4,L0,T1,T4,T0,VR1,VR4,VR0);
					DrawTriL(X0+16,Y0,H2,H5,H4,L2,L4,L4,T2,T5,T4,VR2,VR5,VR4);
					ND++;
				}else NND++;
			};
		};
	};
	GT1=GetTickCount()-T0;
	T0=GetTickCount();
	//shrinking twice
	int ofs0=0;
	int ofs1=MM_Y0*MM_Lx;
	int NN1=MM_Lx*20;
	for(iy=0;iy<MM_Ly;iy++){
		for(int ix=0;ix<MM_Lx;ix++){
			byte c=MiniMap[ofs1];
			if(c)MiniMap[ofs0]=c;
			else{
				int p=MM_Lx;
				while(p<NN1&&!(c=MiniMap[ofs1+p]))p+=MM_Lx;
				MiniMap[ofs0]=c;
			};
			ofs0++;
			ofs1++;
		};
		ofs1+=MM_Lx;
	};
	GT2=GetTickCount()-T0;
	T0=GetTickCount();
	//render add-details level
	int NDX=MM_Lx>>3;
	int NDY=MM_Ly>>3;
	for(iy=0;iy<NDY;iy++){
		for(int ix=0;ix<NDX;ix++){
			DrawMiniMaskOnScreen(MiniMap,ix<<3,iy<<3,ix+(x0<<1),iy+(y0>>1),MM_Lx);
			DrawMiniSubWaterSquare(MiniMap,ix<<3,iy<<3,ix+(x0<<1),iy+(y0>>1),MM_Lx);
		};
	};

	ScreenPtr=sptr;
	MiniMap=mptr0;
	MM_Lx=LX0;
	MM_Ly=LY0;
	MM_Y0=Y0;
	GT4=GetTickCount()-T0;
	T0=GetTickCount();
	int NX=(nx<<4)/M4CellLX;
	int NY=(ny<<2)/M4CellLY;
	int _X0=(x0<<4)/M4CellLX;
	int _Y0=(y0<<2)/M4CellLY;
	for(int ix=0;ix<NX;ix++){
		for(int iy=0;iy<NY;iy++){
			byte* dat=ReserveMemoryFor4X(_X0+ix,_Y0+iy);
			int ofsp=int(mpart)+(ix+iy*NX*M4CellLY)*M4CellLX;
			int adofs=(NX-1)*M4CellLX;
			__asm{
				push esi
				push edi
				cld
				mov  esi,ofsp
				mov  edi,dat
				mov  ebx,M4CellLY
LPP1:			mov  ecx,M4CellLX/4
				rep  movsd
				add  esi,adofs
				dec  ebx
				jnz  LPP1
				pop  edi
				pop  esi
			};
		};
	};
	//GT5=GetTickCount()-T0;
};
int MM_MinChangeX=10000;
int MM_MinChangeY=10000;
int MM_MaxChangeX=-10000;
int MM_MaxChangeY=-10000;
void ReportCoorChange(int x,int y){
	//return;
	if(!(ReadyMap&&MM_Lx&&MM_Ly))return;
	if(x<0)x=0;
	if(y<0)y=0;
	if(x<MM_MinChangeX)MM_MinChangeX=x;
	if(y<MM_MinChangeY)MM_MinChangeY=y;
	if(x>MM_MaxChangeX)MM_MaxChangeX=x;
	if(y>MM_MaxChangeY)MM_MaxChangeY=y;
};
void ReportVertexChange(int v){
	int x=(v%VertInLine)*32;
	int y=(v/VertInLine)*32;
	if(x<0)x=0;
	if(y<0)y=0;
	if(x<MM_MinChangeX)MM_MinChangeX=x;
	if(y<MM_MinChangeY)MM_MinChangeY=y;
	if(x>MM_MaxChangeX)MM_MaxChangeX=x;
	if(y>MM_MaxChangeY)MM_MaxChangeY=y;
};
extern SprGroup WALLS;
void SetTexturesShadowInSquare(int x0,int y0,int x1,int y1);
void CheckMM_Change(){	
	if(MM_MinChangeX<=MM_MaxChangeX){
		SetTexturesShadowInSquare(MM_MinChangeX,MM_MinChangeY,MM_MaxChangeX,MM_MaxChangeY);		
		void CreateMiniMapPart(int x0,int y0,int x1,int y1,bool);
		CreateMiniMapPart(MM_MinChangeX>>6,MM_MinChangeY>>6,MM_MaxChangeX>>6,MM_MaxChangeY>>6,false);
		MM_MinChangeX= 10000;
		MM_MinChangeY= 10000;
		MM_MaxChangeX=-10000;
		MM_MaxChangeY=-10000;		
	};
};

extern int TGP;
extern DWORD T_Diff;
void ProcessTChanels();
void ClearFastSprites();

void DrawSpriteTrees()
{
    ClearFastSprites();
    ProcessTChanels();
    GPS.SetCurrentDiffuse( T_Diff );

    if (TGP == -1) TGP = GPS.PreLoadGPImage( "TreesAll" );

    //  find view frustum extents on the map
    const Vector3D* vc = GetCameraIntersectionCorners();
    Frustum fr;
    ICam->GetFrustum( fr );
    Vector3D ltn = fr.ltn();
    Vector3D rtn = fr.rtn();
    Vector3D rbn = fr.rbn();
    Vector3D lbn = fr.lbn();
    float minFx = tmin( tmin( ltn.x, rtn.x, rbn.x, lbn.x ), tmin( vc[0].x, vc[1].x, vc[2].x, vc[3].x ) );
    float minFy = tmin( tmin( ltn.y, rtn.y, rbn.y, lbn.y ), tmin( vc[0].y, vc[1].y, vc[2].y, vc[3].y ) );
    float maxFx = tmax( tmax( ltn.x, rtn.x, rbn.x, lbn.x ), tmax( vc[0].x, vc[1].x, vc[2].x, vc[3].x ) );
    float maxFy = tmax( tmax( ltn.y, rtn.y, rbn.y, lbn.y ), tmax( vc[0].y, vc[1].y, vc[2].y, vc[3].y ) );

    if (maxFx <= minFx || maxFy <= minFy) return;

    int spx0 = minFx/4.0f/32.0f - 1;
    int spy0 = minFy/4.0f/32.0f - 1;
    int spx1 = maxFx/4.0f/32.0f + 1;
    int spy1 = maxFy/4.0f/32.0f + 1;

    clamp( spx0, 0, VAL_SPRNX - 1 );
    clamp( spx1, 0, VAL_SPRNX - 1 );
    clamp( spy0, 0, VAL_SPRNX - 1 );
    clamp( spy1, 0, VAL_SPRNX - 1 );

	int x0 = mapx<<5;
	int y0 = (mapy)<<4;
	int Lx = smaplx<<5;
	int Ly = (smaply)<<4;
	int x1 = x0+Lx;
	int y1 = y0+Ly;
	int SH = 5-Shifter;
	static int shTrees = IRS->GetShaderID( "sprite_buildings" );
	GPS.SetCurrentShader( shTrees );
    GPS.SetCurrentDiffuse( 0xFF808080 );
	bool CheckIfNewTerrain();
	bool bNewTerr = CheckIfNewTerrain();

	for (int spx = spx0; spx <= spx1; spx++)
    {
		int ofst = spx + (spy0<<SprShf);
		int maxy;
		int xx = (spx<<7) + 64;
		for (int spy = spy0; spy <= spy1; spy++)
        {
			int  N    = NSpri[ofst];
			int* List = SpRefs[ofst];
            ofst += VAL_SPRNX;
			if (!N || !List) continue;
			for(int i = 0; i < N; i++)
            {
				const OneSprite& OS = Sprites[List[i]];
				if (!OS.Enabled) continue;
                int z=OS.z;//GetHeight(OS.x,OS.y);
				if(!Mode3D)z=0;
				int ry=((OS.y)>>1)-y0;
				int ry1=ry-z;
				int rx=OS.x-x0;
				const ObjCharacter* OC = OS.OC;
                int R = tmax( OC->CenterX, OC->CenterY );
                //  test rough object/frustum intersection
				if (!CheckObjectVisibility( OS.x, OS.y, OS.z, R )) continue;
                
                //  play sound effect 
				if(OC->SoundID>0&&rand()<1310)
                {
					static NewAnimation NA;
					static bool init=false;
					if(!init){
						NA.ActivePtX = NULL;
						NA.ActivePtY = NULL;
						NA.LineInfo  = NULL;
						NA.Name      = NULL;
						init         = true;								
                    }
					NA.SoundID=OC->SoundID;
					NA.SoundProbability=OC->SoundProb*GameSpeed/256;
					if(NA.SoundProbability<2)NA.SoundProbability=2;
					NA.HotFrame=0xFF;
					NA.NFrames=1;
					PlayAnimation(&NA,0,OS.x,OS.y);
				}

				SprGroup* SG=OS.SG;
				if(OC->UseTexture){
                    void AddFastSprite(const OneSprite& OS);
					AddFastSprite( OS );                                
				}else{
					if(OC->ViewType==1){
						if(OS.M4){
							RenderModels.Add(OC->ModelManagerID,OS.M4,OS.y,OC->MShiftY);
						}else{
							Matrix4D M4;
							OC->GetMatrix4D(M4,OS.x,OS.y,OS.z);
							RenderModels.Add(OC->ModelManagerID,&M4,OS.y,OC->MShiftY);
						}
                        continue;
					}else
					if(OS.M4)
                    {
						GPS.DrawWSprite(OC->FileID,OC->SpriteID,*OS.M4,0);
						continue;
					}
                    if(SG==&COMPLEX){
						bool GetObjectVisibilityInFog(int x,int y,int z,OneObject* OB);
						//if(GetObjectVisibilityInFog(OS.x,OS.y,OS.z,NULL)){
							void DrawFPatch(int x,int y,int z,int W,int H,float G,float S);
							DrawFPatch(OS.x,OS.y,OS.z,64,64,float(OC->Z0)/255.0,float(OC->DZ)/255.0);
						//}
					}else
					if(OC->Stand){
						int tm1=div(tmtmt,OC->Delay).quot;
						int fr=div(tm1+OS.x*47+OS.y*83,OC->Frames).rem;
						int spr=fr*OC->Parts;
						int z0=ry+OC->Z0;
						int XX=rx-OC->CenterX;
						int YY=ry-OC->CenterY-z;
						NewAnimation* NA=OC->Stand;
						for(int p=0;p<OC->Parts;p++){
							NewFrame* OF=NA->Frames[spr+p];
							//AddPoint(rx>>2,z0>>2,XX>>2,YY>>2,NULL,OF->FileID,OF->SpriteID,0,0);
							AddWorldPoint(OS.x,OS.y,OS.z,OC->CenterX,OC->CenterY,NULL,OF->FileID,OF->SpriteID);
							z0+=OC->DZ;
						};
					}else{
						if(SG==&ANMSPR){
							int sp=SG->Objects[OS.SGIndex]->SpriteID;
							int dx=OC->CenterX;
							if(sp>=4096)AddSuperLoPoint((rx+dx)>>2,(ry1-OC->CenterY)>>2,NULL,OC->FileID,sp,0,0);
							else AddSuperLoPoint((rx-dx)>>2,(ry1-OC->CenterY)>>2,NULL,OC->FileID,sp,0,0);
						}else if(SG==&TREES||SG==&WALLS||(SG==&STONES&&bNewTerr)){
							if(OC->Amplitude/*&&!LMode*/){
								int GetFractalVal(int x,int y);
								int DT=GetTickCount()/10;
								float mod=float((GetFractalVal(OS.x+DT,OS.y+DT)*GetFractalVal(OS.x-DT,OS.y+DT/2)))/256.0f/256.0f;
								float ang=0.0025f*mod*OC->Amplitude*cos(float(GetTickCount())/(450.0f+(OS.x+OS.y)%100)+float((OS.x+OS.y)&1023))/30;
								AddWorldPoint(OS.x,OS.y,OS.z,OC->CenterX,OC->CenterY,NULL,OC->FileID,OC->SpriteID, 0xFF808080,true,1.0f,1.0f,ang);
							}else{
								AddWorldPoint(OS.x,OS.y,OS.z,OC->CenterX,OC->CenterY,NULL,OC->FileID,OC->SpriteID);
							}
                            /*extern bool NewSurface;
                            if (NewSurface)
                            {
                                Matrix4D shTM = GetAlignGroundTransform( Vector3D( OC->CenterX, OC->CenterY, 0.0f ) );
                                shTM.translate( SkewPt( OS.x,OS.y,OS.z ) );
                                ISM->DrawWSprite( TGP, OC->SpriteID, shTM );
                            }*/
						}
					}
				}
			}
		}
	}
    GPS.FlushBatches();
} // DrawSpriteTrees

void ShowSpritesShadows()
{
    if (TGP == -1) TGP = GPS.PreLoadGPImage( "TreesAll" );

    //  find view frustum extents on the map
    const Vector3D* vc = GetCameraIntersectionCorners();
    Frustum fr;
    ICam->GetFrustum( fr );
    Vector3D ltn = fr.ltn();
    Vector3D rtn = fr.rtn();
    Vector3D rbn = fr.rbn();
    Vector3D lbn = fr.lbn();
    float minFx = tmin( tmin( ltn.x, rtn.x, rbn.x, lbn.x ), tmin( vc[0].x, vc[1].x, vc[2].x, vc[3].x ) );
    float minFy = tmin( tmin( ltn.y, rtn.y, rbn.y, lbn.y ), tmin( vc[0].y, vc[1].y, vc[2].y, vc[3].y ) );
    float maxFx = tmax( tmax( ltn.x, rtn.x, rbn.x, lbn.x ), tmax( vc[0].x, vc[1].x, vc[2].x, vc[3].x ) );
    float maxFy = tmax( tmax( ltn.y, rtn.y, rbn.y, lbn.y ), tmax( vc[0].y, vc[1].y, vc[2].y, vc[3].y ) );

    if (maxFx <= minFx || maxFy <= minFy) return;

    //  for every object on the map
    int spx0 = minFx/4.0f/32.0f - 1;
    int spy0 = minFy/4.0f/32.0f - 1;
    int spx1 = maxFx/4.0f/32.0f + 1;
    int spy1 = maxFy/4.0f/32.0f + 1;

    if(spx0<0)spx0=0;else
        if(spx0>=VAL_SPRNX)spx0=VAL_SPRNX-1;
    if(spy0<0)spy0=0;else
        if(spy0>=VAL_SPRNX)spy0=VAL_SPRNX-1;
    if(spx1<0)spx1=0;else
        if(spx1>=VAL_SPRNX)spx1=VAL_SPRNX-1;

    int x0=mapx<<5;
    int y0=(mapy)<<4;
    int Lx=smaplx<<5;
    int Ly=(smaply)<<4;
    int x1=x0+Lx;
    int y1=y0+Ly;
    int SH=5-Shifter;
    static int h2=IRS->GetShaderID("hud2_shadow_L");
    GPS.SetCurrentShader(h2);
    GPS.SetCurrentDiffuse( EngSettings.ShadowsColor );

    for(int spx=spx0;spx<=spx1;spx++){
        int ofst=spx+(spy0<<SprShf);
        int spy=spy0;
        int maxy;
        int xx=(spx<<7)+64;
        do{
            int N=NSpri[ofst];
            int* List=SpRefs[ofst];
            if(N&&List){
                int st=1;//(N/10)+1;				
                for(int i=0;i<N;i+=st){
                    OneSprite* OS=Sprites+List[i];
                    if(OS->Enabled){
                        int z=OS->z;//GetHeight(OS->x,OS->y);
                        if(!Mode3D)z=0;
                        int ry=((OS->y)>>1)-y0;
                        int ry1=ry-z;
                        int rx=OS->x-x0;
                        int SZ=128;
                        ObjCharacter* OC=OS->OC;
                        if(OC->ViewType==1)SZ=400;
                        if(CheckObjectVisibility(OS->x,OS->y,OS->z,180)){                            
                            extern bool NewSurface;
                            if (NewSurface)
                            {
                                Matrix4D shTM = GetAlignGroundTransform( Vector3D( OC->CenterX, OC->CenterY, 0.0f ) );
                                shTM.translate( SkewPt( OS->x,OS->y,OS->z ) );
                                ISM->DrawWSprite( TGP, OC->SpriteID, shTM );
                            }
                        }
                    }
                }
            }
            spy++;
            ofst+=VAL_SPRNX;
            maxy=spy<<6;
            if(Mode3D)maxy-=GetHeight(xx,maxy<<1);
        }while(spy<VAL_SPRNX&&maxy<(y1+250));        
    };
    GPS.FlushBatches();
};
void GSSetup800();
extern int CurPalette;
void ClearAllWaves();
void ClearAllLModeData(){
	try{
		ClearAllWaves();
		if(LMode)ReverseLMode();
		if(ReadyMap){
			free(ReadyMap);
		};
	}catch(...){
	};
	MiniMap=NULL;
	ReadyMap=NULL;
	LMode=0;
};
void CheckMM4XReady(int x,int y,int Lx,int Ly){
	int NX=M4CellLX>>4;
	int NY=M4CellLX>>2;
	bool NeedDraw=0;
	int DRX,DRY,DNY;
	int x0=x/M4CellLX;
	int y0=y/M4CellLY;
	int x1=((x+Lx)/M4CellLX)+1;
	int y1=((y+Ly)/M4CellLY)+1;
	if(x1>=MMR_Lx)x1=MMR_Lx-1;
	if(y1>=MMR_Ly)y1=MMR_Ly-1;
	for(int ix=x0;ix<=x1;ix++){
		for(int iy=y0;iy<=y1;iy++){
			int ofs=ix+iy*MMR_Lx;
			if(ReadyMap[ofs]){
				if(NeedDraw){
					if(DNY==1){
						int NXX=1;
						for(int ixx=ix+1;ixx<=x1;ixx++){
							int ofs=ixx+DRY*MMR_Lx;
							if(!ReadyMap[ofs]){
								NXX++;
								ReadyMap[ofs]=(byte*)1;
								assert(ofs<RSIZE);
							}else ixx=x1+1;
						};
						GenerateMiniMapSquare(DRX*NX,DRY*NY,NX*NXX,DNY*NY);
						NeedDraw=0;
					}else{
						GenerateMiniMapSquare(DRX*NX,DRY*NY,NX,DNY*NY);
						NeedDraw=0;
					};
				};
			}else{
				ReadyMap[ofs]=(byte*)1;
				assert(ofs<RSIZE);
				if(NeedDraw)DNY++;
				else{
					DRX=ix;
					DRY=iy;
					DNY=1;
					NeedDraw=1;
				};
			};
		};
		if(NeedDraw){
			if(DNY==1){
				int NXX=1;
				for(int ixx=ix+1;ixx<=x1;ixx++){
					int ofs=ixx+DRY*MMR_Lx;
					if(!ReadyMap[ofs]){
						NXX++;
						ReadyMap[ofs]=(byte*)1;
						assert(ofs<RSIZE);
					}else ixx=x1+1;
				};
				GenerateMiniMapSquare(DRX*NX,DRY*NY,NX*NXX,DNY*NY);
				NeedDraw=0;
			}else{
				GenerateMiniMapSquare(DRX*NX,DRY*NY,NX,DNY*NY);
				NeedDraw=0;
			};
		};
	};
	//assert(_CrtCheckMemory());
};
//////////////////////////////////////////////////////////////////////////
class OneBldFlag:public BaseClass{
public:
	_str  Name;
	short gpFile;
	int   cx;
	int   cy;
	int   Start;
	int   Final;
	SAVE(OneBldFlag);
	REG_AUTO(Name);
	REG_MEMBER(_gpfile,gpFile);
	REG_MEMBER(_int,cx);
	REG_MEMBER(_int,cy);
	REG_MEMBER(_int,Start);
	REG_MEMBER(_int,Final);
	ENDSAVE;
};
class FlagsOnBuildings:public BaseClass{
public:
	ClassArray<OneBldFlag> Flags;
	SAVE(FlagsOnBuildings);
	REG_CLASS(OneBldFlag);
	REG_AUTO(Flags);
	ENDSAVE;
};
FlagsOnBuildings FBLD;
void LoadFlagsInfo(){
	xmlQuote xml;
	if(xml.ReadFromFile("dialogs\\Flags.xml")){
		ErrorPager EP;
		FBLD.Load(xml,&FBLD,&EP);
	}
}
int GetFlagIndex(char* Name){
	static bool FBINIT=0;
	if(!FBINIT){
		LoadFlagsInfo();
		FBINIT=1;
	}
	for(int i=0;i<FBLD.Flags.GetAmount();i++)if(!strcmp(FBLD.Flags[i]->Name.str,Name))return i;
	return -1;
}
int GetInterpFOW(int x,int y);
extern int FogMode;
extern byte BaloonState;
bool GetObjectVisibilityInFog(int x,int y,int z,OneObject* OB){
	if(OB&&OB->NNUM==7)return true;
	bool usefog=FogMode&&BaloonState!=1&&(!NATIONS[GSets.CGame.cgi_NatRefTBL[MyNation]].Vision);
	bool OK=true;
	if(usefog/*&&!(OB&&OB->NNUM==7&&OB->NewBuilding)*/){
		int dp=GetInterpFOW(x,(y>>1)-z);		
		if(dp<850)OK=false;
	}
	return OK;
}
// 0 - fully invisible 255 - fully visible
int GetObjectVisibilityValueInFog(int x,int y,int z,OneObject* OB){
	if(OB&&OB->NNUM==7)return 255;
	bool usefog=FogMode&&BaloonState!=1&&(!NATIONS[GSets.CGame.cgi_NatRefTBL[MyNation]].Vision);
	int OK=255;
	if(usefog){
		int dp=GetInterpFOW(x,(y>>1)-z);		
		if(dp<850)OK=0;
		else if(dp>1100)return 255;
		else if(dp<950){
			//int GetFOW2(int x,int y);
			//int V=GetFOW2(x,y/2-z);
			OK=((dp-850)*255)/(940-850);
			//OK=255-V;
			if(OK<0)OK=0;
			if(OK>255)OK=255;
		}
	}
	return OK;
}
#define NewDraw

bool g_bRenderShadows;
_inline void DrawActiveAbility(OneObject* OB){	
	if(OB->Sdoxlo==0 && OB->ActiveAbility && OB->ActiveAbility->ActiveAbilities.EfAnimationMask){
		int n=OB->ActiveAbility->ActiveAbilities.GetAmount();
		for(int i=0;i<n;i++){
			ActiveUnitAbility* AA=OB->ActiveAbility->ActiveAbilities[i];
			if(AA->EfAnimationMask){
				UnitAbility* UA=AA->GetA();
				if(UA){
					NewAnimation* NA=UA->eAn.Get();
					if(NA&&NA->Enabled){
						AddAnimation(OB->RealX>>4,OB->RealY>>4,OB->RZ,NA,0,OB->RealDir,0xFFFFFFFF,OB);
					}
				}
			}
		}
	}
}
void DrawUnits()
{	
    //  sprites on the map are drawn with enabled z-buffer
	GPS.EnableZBuffer( true );
	GPS.SetScale( 1.0f / float( 1 << (5-Shifter) ) );
	ClearVisibleGP();
	
	int SCSHIFT=5-Shifter;
	CLM_Shift=SCSHIFT;
	CLM_ShiftY=SCSHIFT;
	ClearZBuffer();
	IRS->ResetWorldMatrix();

	int x0=mapx<<(5-CLM_Shift);
	int y0=(mapy<<(4-CLM_Shift));
	int Lx1=smaplx<<(5-CLM_Shift);
	int Ly1=smaply<<(4-CLM_Shift);


	int xx,yy;
	int mpdy    = mapy<<(4-CLM_Shift);
	int dxx     = mapx<<(5-CLM_Shift);
	int dyy     = mapy<<(4-CLM_Shift);

    //  find view frustum extents on the map
	const Vector3D* vc = GetCameraIntersectionCorners();
    Frustum fr;
    ICam->GetFrustum( fr );
    Vector3D ltn = fr.ltn();
    Vector3D rtn = fr.rtn();
    Vector3D rbn = fr.rbn();
    Vector3D lbn = fr.lbn();
    float minFx = tmin( tmin( ltn.x, rtn.x, rbn.x, lbn.x ), tmin( vc[0].x, vc[1].x, vc[2].x, vc[3].x ) );
    float minFy = tmin( tmin( ltn.y, rtn.y, rbn.y, lbn.y ), tmin( vc[0].y, vc[1].y, vc[2].y, vc[3].y ) );
    float maxFx = tmax( tmax( ltn.x, rtn.x, rbn.x, lbn.x ), tmax( vc[0].x, vc[1].x, vc[2].x, vc[3].x ) );
    float maxFy = tmax( tmax( ltn.y, rtn.y, rbn.y, lbn.y ), tmax( vc[0].y, vc[1].y, vc[2].y, vc[3].y ) );
    
    if (maxFx <= minFx || maxFy <= minFy) return;

	//  for every object on the map
	int CX0 = minFx/4.0f/32.0f - 1;
	int CY0 = minFy/4.0f/32.0f - 1;
	int CX1 = maxFx/4.0f/32.0f + 1;
	int CY1 = maxFy/4.0f/32.0f + 1;

	int VDX=32;
	if(CX0<0)CX0=0;
	if(CY0<0)CY0=0;
    
    //  scan through cells and draw visible (non-bulding) objects
	for (int dx = CX0; dx < CX1; dx++)
    {
		for (int dy = CY0; dy < CY1; dy++)
        {
			int cell = 1 + dx + ((dy + 1)<<VAL_SHFCX);
			if(cell >= VAL_MAXCIOFS) continue;
			int NMon = MCount[cell];
			if (NMon == 0) continue;
			int ofs1 = cell<<SHFCELL;
			for (int i = 0; i < NMon; i++)
            {
				WORD MID=GetNMSL(ofs1+i);
				if(MID==0xFFFF) continue;
				OneObject* OB=Group[MID];
				if(!OB||OB->Hidden) continue;
				GPS.SetCurrentDiffuse(0xFF808080);
				xx=(OB->RealX>>(4+CLM_Shift))-x0;
				yy=(OB->RealY>>(5+CLM_Shift))-y0;
				int zz=yy;
				zz-=zoom(OB->RZ+int(OB->OverEarth));
                int R = 300;
                if (!CheckObjectVisibility( OB->RealX>>4, OB->RealY>>4, OB->RZ, R )) continue;
				DWORD V=GetObjectVisibilityValueInFog(OB->RealX>>4,OB->RealY>>4,OB->RZ+OB->OverEarth,OB);
				if (V==0) continue;
				NewAnimation* NAM=OB->NewAnm;
				if(!NAM) continue;
				if(OB->newMons->MotionStyle==8)
                {
					int vx=TCos[OB->RealDir];
					int vy=TSin[OB->RealDir];
					float N1=sqrt(vx*vx+vy*vy);
					float N2=sqrt(OB->ForceX*OB->ForceX+OB->ForceY*OB->ForceY);
					float s=0;
					if(N1&&N2)s=atan(float(vx*OB->ForceY-vy*OB->ForceX)/N1/500.0f);
					AddAnimation(OB->RealX>>4,OB->RealY>>4,OB->RZ+OB->OverEarth,NAM,OB->CurrentFrameLong,
						float(OB->RealDirPrecise)/256.0f,0xFF808080,OB,1.0f,0,s,0x23000000&OB->Serial);
				}else{
					int frame=OB->CurrentFrameLong;
					if(V<240){						
						if(NAM->Code>=23&&NAM->Code<=26){//rest
                            NAM=OB->newMons->GetAnimation(anm_Stand);
							frame=0;
						}else 
						if(NAM->Code==37){//rest A1
							NAM=OB->newMons->GetAnimation(anm_Stand+1);
							frame=0;
						}
					}
					AddAnimation(OB->RealX>>4,OB->RealY>>4,OB->RZ+OB->OverEarth,NAM,
									frame,float(OB->RealDirPrecise)/256.0f,0x00808080+(V<<24),OB,1.0f,0,0,0x23000000&OB->Serial);
				}
				DrawActiveAbility(OB);
			}
		}
	}

    //  cycle on buildings
    //  widen cell bounds, because big buildings can span more than one cell
    CX0 -= 5; CX1 += 5;
    CY0 -= 5; CY1 += 5;
    for (int dx = CX0; dx < CX1; dx++)
    {
        for (int dy = CY0; dy < CY1; dy++)
        {
            int cell = 1 + dx + ((dy + 1)<<VAL_SHFCX);
            if(cell >= VAL_MAXCIOFS) continue;
            WORD GetOneBld( int cell, int pos = 0 );
            WORD MID;
            for (int pos = 0; (MID = GetOneBld(cell,pos)) != 0xFFFF; pos++)
            {				
                OneObject* OB=Group[MID];
                DWORD nationalColor = 0x00000000;
                if (OB)
                {
                    nationalColor = GetNatColor( OB->NNUM );
                }
                if (!OB || !OB->NewBuilding || OB->Hidden) continue;				
                
                NewMonster* NM = OB->newMons;
                int R = 300;
                if (NM)
                {
                    R = tmax( abs( NM->BuildX1 - NM->BuildX0 ), abs( NM->BuildY1 - NM->BuildY0 ) )*2;
                }
                if (!CheckObjectVisibility( OB->RealX>>4, OB->RealY>>4, OB->RZ, R )) continue;

                if (!OB->TempFlag&&!GetObjectVisibilityInFog(OB->RealX>>4,OB->RealY>>4,OB->RZ+OB->OverEarth,OB)) continue;
                if(OB->NewBuilding)OB->TempFlag=1;
                if(OB->ImSelected&(1<<MyNation)){
                    int dc=int(sin(float(GetTickCount()%100000)/200.0f)*50.0f)+0x40;
                    GPS.SetCurrentDiffuse(0xFFC0C0C0-dc-(dc<<8)-(dc<<16));
                }else GPS.SetCurrentDiffuse(0xFF808080);
                if(OB->LoLayer){
                    DWORD C=0xFF808080;
                    if(OB->ImSelected&(1<<MyNation)){
                        int dc=int(sin(float(GetTickCount()%100000)/200.0f)*50.0f)+0x40;
                        C=0xFFC0C0C0-dc-(dc<<8)-(dc<<16);
                    }
                    if(OB->Sdoxlo>2000-150){
                        if(OB->Sdoxlo>2000){
                            C&=0xFFFFFF;
                        }else{
                            C&=0xFFFFFF;
                            C|=((2000-OB->Sdoxlo)*255/150)<<24;
                        }
                    }
                    AddAnimation(OB->RealX>>4,OB->RealY>>4,OB->RZ,OB->LoLayer,0,0,C,OB,1.0,0,0,0x23000000&OB->Serial);
                }
                if(OB->NewAnm){
                    //OB->LoLayer->DrawAt(0,OB->NNUM,OB->RealX>>4,OB->RealY>>4,OB->RZ,OB->RealDir,1.0f,0,0,OB);
                    extern int AnimTime;
                    AddAnimation(OB->RealX>>4,OB->RealY>>4,OB->RZ,OB->NewAnm,OB->NewAnm->NFrames?(AnimTime<<1)%(OB->NewAnm->NFrames<<8):0,0,0xFF808080,OB,1.0f,0,0,0x23000000&OB->Serial);
                }
                if(OB->Sdoxlo&&OB->Sdoxlo<240)
                {
                    NewAnimation* NA=OB->newMons->GetAnimation(anm_StandLo);
                    if(NA){
                        Vector3D V0(OB->RealX>>4,OB->RealY>>4,OB->RZ);
                        Vector3D CP=ICam->GetPos();
                        Vector3D V1=CP;
                        Vector3D V2=SkewPt(OB->RealX>>4,OB->RealY>>4,OB->RZ);
                        V1-=V2;
                        V1.normalize();
                        V1*=8;
                        V0+=V1;
                        AddAnimation(V0.x,V0.y,V0.z,NA,0,0,((255-(OB->Sdoxlo*255)/240)<<24)+0x808080,OB,1.0f,0,0,0x23000000&OB->Serial);
                    }
                }
                xx=(OB->RealX>>(4+zoomsh))-x0;
                yy=(OB->RealY>>(5+zoomsh))-y0;
                int zz=yy;
                if(Mode3D)zz-=zoom(OB->RZ);
                int xx0=xx+zoom(NM->PicDx);
                int yy0=zz+zoom(NM->PicDy);				
                if(!OB->Sdoxlo)ShowFires(OB,xx0,yy0);
                if(OB->newMons->Port){
                    void ShowProducedShip(OneObject* Port,int CX,int CY);
                    ShowProducedShip(OB,OB->WallX,OB->WallY);
                }
				DrawActiveAbility(OB);
            }
        }
    }

	//  disable z-buffer to let interface elements be drawn in overlay mode
	GPS.EnableZBuffer( false );
} // DrawUnits

void RegisterVisibleGP(OneObject* OB,int gpID,int sprID,int x,int y);

DynArray<int> SprCollection;

float	FallStage	= 0.0f;
int		FallAxeY	= 0;
bool	CollectMode	= 0;
float	GlobalScale	= 1.0f;

inline void DrawFallingSprite(int x,int y, float z, int FileID,int Sprite,byte NI,int FAxeY=FallAxeY,float FStage=FallStage)
{
	if(CollectMode)
	{
		SprCollection.Add(x);
		SprCollection.Add(y);
		SprCollection.Add(FileID);
		SprCollection.Add(Sprite);
		SprCollection.Add(NI);
		SprCollection.Add(FAxeY);
		SprCollection.Add(*((int*)&FStage));
		SprCollection.Add(*((int*)&z));		
	}
	GPS.SetScale( GlobalScale );
	
	if (fabs( FStage ) < 0.04)
	{
		GPS.ShowGP( x, y, z, FileID, Sprite, NI );
		return;
	}

	Plane plane;
	plane.from3Points(	Vector3D( 0, FAxeY, 0 ), 
						Vector3D( 1, FAxeY, 0 ),
						Vector3D( 0, 0, float( FAxeY ) * FStage * c_CosPId6 * 2.0f / GetZRange()) );
	//GPS.ShowGPAligned( x, y, z, plane, FileID, Sprite, NI );
} // DrawFallingSprite

struct WorldSprite
{
	int			m_FileID;
	int			m_FrameID;
	DWORD		m_NatColor;
	Matrix4D	m_TM;
	DWORD		Diffuse;
};

const int c_MaxWorldSprites = 8192;
WorldSprite WorldCollection[c_MaxWorldSprites];
int NWorldSprites = 0;
DWORD CurDiffuse=0;
void DrawWorldSprite( int FileID,int SpriteID, const Matrix4D& tm, byte NI )
{
	DWORD color = GetNatColor( NI );

	if(CollectMode)
	{
		WorldSprite& ws = WorldCollection[NWorldSprites++];
		assert(NWorldSprites < c_MaxWorldSprites);
		ws.m_FileID = FileID;
		ws.m_FrameID = SpriteID;
		ws.m_NatColor = color;
		ws.m_TM = tm;
		ws.Diffuse=(GPS.GetCurrentDiffuse()&0xFF000000)+0x808080;
	}

	GPS.DrawWSprite( FileID, SpriteID, tm, color );
}

void ShowCollection()
{
	CollectMode=0;
	int N=SprCollection.GetAmount();
	int* iptr=SprCollection.GetValues();
	for(int i=0;i<N;i+=8){
		DrawFallingSprite(iptr[0],iptr[1],*((float*)(iptr+7)), iptr[2],iptr[3],iptr[4],iptr[5],*((float*)(iptr+6)));
		iptr+=8;
	}

	for (int i = 0; i < NWorldSprites; i++)
	{
		WorldSprite& ws = WorldCollection[i];
		GPS.SetCurrentDiffuse(ws.Diffuse);
		GPS.DrawWSprite( ws.m_FileID, ws.m_FrameID, ws.m_TM, ws.m_NatColor );
	}
	GPS.FlushBatches();
}

extern int NWorldSprites;
void ClearCollection()
{
	SprCollection.NValues=0;
	NWorldSprites = 0;
}

void SetCollectMode(int mode)
{
    CollectMode=mode;
}

extern bool CINFMOD;
//--------------------------------------------------------------------------
//	Func:	DrawDebugBuildingInfo
//	Desc:	Draws building extents
//--------------------------------------------------------------------------
void DrawDebugBuildingInfo( const OneObject* OB )
{
	if(CINFMOD && OB && OB->NewBuilding && !LMode)
	{
		NewMonster* NM=OB->newMons;
		int CX=OB->RealX>>4;
		int CY=OB->RealY>>4;
		int X0=CX+NM->BuildX0;
		int Y0=CY+NM->BuildY0;
		int X1=CX+NM->BuildX1;
		int Y1=CY+NM->BuildY1;
		int D=(Y1-Y0+X1-X0)>>1;
		int D2=D>>1;
		int mpdx=mapx<<5;
		int mpdy=(mapy<<4)+OB->RZ;
		Y0>>=1;
		Y1>>=1;
		X0-=mpdx;Y0-=mpdy;
		X1-=mpdx;Y1-=mpdy;
		void PtLine(int x,int y,int x1,int y1,byte c);
		PtLine(X0,Y0,X0+D,Y0+D2,0xB0);
		PtLine(X0,Y0,X1-D,Y1-D2,0xB0);
		PtLine(X1,Y1,X0+D,Y0+D2,0xB0);
		PtLine(X1,Y1,X1-D,Y1-D2,0xB0);
	};
} // DrawDebugBuildingInfo

extern word TransparentBuildingID;
extern word PrevTransparentBuildingID;
extern word TransparentBuildingAlpha;
//--------------------------------------------------------------------------
//	Func:	VariateUnitColor
//	Desc:	Adjusts unit color to make it look slightly different
//--------------------------------------------------------------------------
void VariateUnitColor( OneObject* OB, DWORD& Color )
{
	if(!OB || !OB->newMons) return;
	//checking for hilighting
	byte M=OB->HighlightMask;
	OB->HighlightMask=0;
	DWORD CA=Color&0xFF000000;
	if(M){
		if(M&1){//enemy
			Color=CA | (EngSettings.EnemyHighliting & 0xFFFFFF);
		}else
		if(M&2){//friend
			Color=CA | (EngSettings.AllyHighliting & 0xFFFFFF);
		}else
		if(M&4){//me
			Color=CA | (EngSettings.FriendsHighliting & 0xFFFFFF);
		}			
	}
	if(OB){
		extern bool TransMode;
		if(OB->NewBuilding && TransMode){
			Color=(0x80<<24) | (Color & 0x00FFFFFF);            
		}else{
			int id=OB->Index;
			if(id==TransparentBuildingID||id==PrevTransparentBuildingID){		
				Color=((255-DWORD(TransparentBuildingAlpha/40))<<24) | (Color & 0x00FFFFFF);
			}
		}
	}
	if(OB->ImSelected&(1<<MyNation)){
		switch(GSets.SVOpt.SelectionType){
			case 1://Blinking
				{
					DWORD C=0;
					DWORD c1=Color;
					extern int SelColor;
					int dc=SelColor;
					for(int i=0;i<3;i++){
						int CC=(c1&255);
						CC=(CC*dc)>>7;
						if(CC>255)CC=255;
						c1>>=8;
						C|=DWORD(CC)<<(i<<3);
					}
					Color=C|(Color&0xFF000000);
					return;
				}
			case 2://red clor
				{
					if(OB->NMask&(1<<GSets.CGame.cgi_NatRefTBL[MyNation])){
						Color=0xFF00FF00;
					}else{
						Color=0xFFFF0000;
					}
					return;
				}
				return;
		}
	}

	int dc=OB->newMons->ColorVariation;
	if(dc){
		extern short randoma[8192];
		int idx=OB->Index;
		idx=idx+(idx/47);
		if(idx&1){
			dc=136+((idx*13177)&8191%(dc));
		}else{
			dc=136-((idx*13177)&8191%(dc));
		}
		DWORD C=0;
		DWORD c1=Color;
		for(int i=0;i<3;i++){
            int CC=(c1&255);
			CC=(CC*dc)>>7;
			if(CC>255)CC=255;
            c1>>=8;
			C|=DWORD(CC)<<(i<<3);
		}
		Color=C|(Color&0xFF000000);
	}		
} // VariateUnitColor

//--------------------------------------------------------------------------
//	Func:	IsVisible
//	Desc:	Whether OneObject instance is visible for nation
//--------------------------------------------------------------------------
bool IsVisible( OneObject* OB, int nationIdx )
{
	if (!OB) return true;
	if (OB->Invisible) return false;
	if (OB->InvisibleForEnemy && !(OB->NMask&NATIONS[nationIdx].NMask)) return false;
	return true;
} // IsVisible

DWORD GetNatColor( int );

enum AnimType
{
	at2D		= 0,
	at3D		= 1,
	atPatch		= 2
}; // enum AnimType

const int	c_AlignGround	= -10000; 
const int	c_AlignTopmost	= 10000;
void RegisterVisibleGP( OneObject* OB, int modelID, const Matrix4D& tm );
void ShowFiresNearBuilding(OneObject* OB,Matrix4D& M4, float planefactor)
{
    if(!OB)return;
    if(OB->Stage<OB->NStages)return;
	if(OB->Sdoxlo>255)return;
	Matrix4D Mi;
	Mi.inverse(M4);
	int x=OB->newMons->PicDx;
	int y=OB->newMons->PicDy;
	int LP=100-OB->Life*100/OB->MaxLife;
	if(LP==0)return;
	Matrix4D LW2W=ScreenToWorldSpace();
	LW2W.mulLeft(M4);
	for(int k=0;k<2;k++){
        int NF=OB->newMons->NFires[k]*LP/100;
		for(int j=0;j<NF;j++){            
			int mid=-1;
			float scale=1.0f;
			int sid=-1;
			if (k==0){
				int nf=EngSettings.FiresList.GetAmount();
				if(nf){
					int id=j%nf;
                    mid=EngSettings.FiresList[id]->ModelID;
					scale=EngSettings.FiresList[id]->Scale;
					sid=EngSettings.FiresList[id]->SoundID;
				}
			}else{
				int nf=EngSettings.SmokeList.GetAmount();
				if(nf){
					int id=j%nf;
					mid=EngSettings.SmokeList[id]->ModelID;
					scale=EngSettings.SmokeList[id]->Scale;
					sid=EngSettings.SmokeList[id]->SoundID;
				}
			}
			if(mid!=-1){
				Matrix4D M;
				M.scaling(scale);
                //Vector3D pos = SkewPt(  OB->RealX/16 + x + OB->newMons->FireX[k][j],
                //                      OB->RealY/16,
                //                      OB->RZ - planefactor*(y + OB->newMons->FireY[k][j]) );
				Vector4D pos (  x + OB->newMons->FireX[k][j],0,-y - OB->newMons->FireY[k][j],1 );
                pos*=LW2W;
				pos.normW();

				M.setTranslation( pos );

				PushEntityContext(j*713+173+k*9775+OB->Index*12347);
				int id=IEffMgr->InstanceEffect(mid);				
				IEffMgr->SetAlphaFactor(id,float(255-OB->Sdoxlo)/256);
				IEffMgr->UpdateInstance(id,M);
				PopEntityContext();
				if(sid>0){
					extern CDirSound* CDS;
					CDS->HitSound(sid);
					AddEffect(OB->RealX/16,OB->RealY/16,sid);
				}
			}
		}
	}
}

const c_NormalStep = 32.0f;
Vector3D GetTotalNormal( int x, int y )
{
    float ndenom = 0.5f / c_NormalStep;

    float l = GetTotalHeight( x - c_NormalStep, y );		
    float r = GetTotalHeight( x + c_NormalStep, y );		
    float u = GetTotalHeight( x, y - c_NormalStep );		
    float d = GetTotalHeight( x, y + c_NormalStep );		

    Vector3D normal;
    normal.x = (l - r) * ndenom;
    normal.y = (u - d) * ndenom;
    normal.z = 2.0f;
    normal.normalize();
    return normal;
}
class Matrix2DF{
public:
	float e00,e01,e10,e11;
	Matrix2DF(float a,float b,float c,float d){
		e00=a;
		e01=b;
		e10=c;
		e11=d;
	}
	Matrix2DF(){

	}
	Matrix2DF& operator = (Matrix2DF& M){
		Matrix2DF(M.e00,M.e01,M.e10,M.e11);
		return M;
	}
	float Inverse(Matrix2DF& M){
		float D=e00*e11-e01*e10;		
		M.e00=e11/D;
		M.e01=-e01/D;
		M.e10=-e10/D;
		M.e11=e00/D;
		return D;
	}
	Vector2Df mul(Vector2Df V){
		Vector2Df Vd;
		Vd.x=V.x*e00+V.y*e10;
		Vd.y=V.x*e01+V.y*e11;
		return Vd;
	}
};
Matrix4D GetPseudoProjectionTM(const Vector3D& pos,NewMonster* NM,float& planeFactor){
	if(NM->Use3pAlign){
		float p=0;
		Matrix4D m=GetPseudoProjectionTM(pos,p);		

		int dx=NM->PicDx;
		int dy=NM->PicDy;

		Vector3D V1=SkewPt(dx+NM->AlignPt1x,(dy+NM->AlignPt1y+NM->AlignPt1z)*2,NM->AlignPt1z);
		Vector3D V2=SkewPt(dx+NM->AlignPt2x,(dy+NM->AlignPt2y+NM->AlignPt2z)*2,NM->AlignPt2z);
		Vector3D V3=SkewPt(dx+NM->AlignPt3x,(dy+NM->AlignPt3y+NM->AlignPt3z)*2,NM->AlignPt3z);

		int f1=NM->AlignPt1y;
		int f2=NM->AlignPt2y;
		int f3=NM->AlignPt3y;

		if(f1<f2&&f1<f3){
			swap(V3.x,V1.x);
			swap(V3.y,V1.y);
			swap(V3.z,V1.z);
		}else
			if(f2<f1&&f2<f3){
				swap(V2.x,V3.x);
				swap(V2.y,V3.y);
				swap(V2.z,V3.z);
			}

			float fx=(V3.x-V1.x)/(V2.x-V1.x);
			Vector3D V4=V2;
			V4*=fx;
			Vector3D V=V1;
			V*=(1-fx);
			V4+=V;

			Vector3D S1=V1;
			Vector3D S2=V2;
			Vector3D S3=V3;
			Vector3D P=pos;
			S1+=P;
			S2+=P;
			S3+=P;

			WorldToScreenSpace(S1);		
			WorldToScreenSpace(S2);
			WorldToScreenSpace(S3);

			m.transformPt(V1);
			m.transformPt(V2);
			m.transformPt(V3);
			m.transformPt(V4);

			float DY=V3.y-V4.y;
			float DX=V3.x-V4.x;

			if( fabs(DX) > fabs(DY) ){
				float A=-DY/DX;
				Matrix2DF M2(V1.x*A+V1.y,V2.x*A+V2.y,1,1);
				Matrix2DF M2I;
				M2.Inverse(M2I);
				Vector2Df S(S1.x,S2.x);
				Vector2Df K1020;
				K1020 = M2I.mul(S);

				float K00=A*K1020.x;
				Vector3D K011121 (S1.y,S2.y,S3.y);
				Matrix3D M3( V1.x, V2.x,    V3.x,
					V1.y, V2.y, V3.y,
					1   ,    1,    1);
				M3.inverse();
				K011121*=M3;			
				Matrix4D K
					(    K00, K011121.x, 0, 0,
					K1020.x, K011121.y, 0, 0,
					0,         0, 1, 0,
					K1020.y, K011121.z, 0, 1);
				if(K.e11<0||K.e11>3||K.e00<0||K.e00>3){
					K.setIdentity();
				}
				m*=K;
				return m;
			}else{
				float B=-DX/DY;
				Matrix2DF M2(V1.x+V1.y*B,V2.x+V2.y*B,1,1);
				Matrix2DF M2I;
				M2.Inverse(M2I);
				Vector2Df S(S1.x,S2.x);
				Vector2Df K0020;
				K0020 = M2I.mul(S);
				float K10=B*K0020.x;

				float DS3Y=-(S2.y-S1.y)/(S2.x-S1.x)*(V3.x*K0020.x+V3.y*K10+K0020.y-S3.x);

				Vector3D K011121 (S1.y,S2.y,S3.y+DS3Y);
				Matrix3D M3( V1.x, V2.x,    V3.x,
					V1.y, V2.y, V3.y,
					1   ,    1,    1);
				M3.inverse();
				K011121*=M3;

				Matrix4D K
					(K0020.x, K011121.x, 0, 0,
					K10, K011121.y, 0, 0,
					0,         0, 1, 0,
					K0020.y, K011121.z, 0, 1);
				if(K.e11<0||K.e11>2||K.e00<0||K.e00>2){
					K.setIdentity();
				}
				m*=K;
				return m;
			}		
	}else{
		return GetPseudoProjectionTM(pos,planeFactor);
	}
}
Matrix4D GetPseudoProjectionTM4(const Vector3D& pos,NewMonster* NM,float& planeFactor){
	if(NM->Use3pAlign){
		float p=0;
		Matrix4D m=GetPseudoProjectionTM(pos,p);		

		int dx=NM->PicDx;
		int dy=NM->PicDy;

		Vector3D V1=SkewPt(dx+NM->AlignPt1x,(dy+NM->AlignPt1y+NM->AlignPt1z)*2,NM->AlignPt1z);
		Vector3D V2=SkewPt(dx+NM->AlignPt2x,(dy+NM->AlignPt2y+NM->AlignPt2z)*2,NM->AlignPt2z);
		Vector3D V3=SkewPt(dx+NM->AlignPt3x,(dy+NM->AlignPt3y+NM->AlignPt3z)*2,NM->AlignPt3z);

		int f1=NM->AlignPt1y;
		int f2=NM->AlignPt2y;
		int f3=NM->AlignPt3y;

		if(f1<f2&&f1<f3){
			swap(V3.x,V1.x);
			swap(V3.y,V1.y);
			swap(V3.z,V1.z);
		}else
		if(f2<f1&&f2<f3){
			swap(V2.x,V3.x);
			swap(V2.y,V3.y);
			swap(V2.z,V3.z);
		}

		float fx=(V3.x-V1.x)/(V2.x-V1.x);
		Vector3D V4=V2;
		V4*=fx;
		Vector3D V=V1;
		V*=(1-fx);
		V4+=V;

		Vector3D S1=V1;
		Vector3D S2=V2;
		Vector3D S3=V3;
		Vector3D P=pos;
		S1+=P;
		S2+=P;
		S3+=P;

		WorldToScreenSpace(S1);		
		WorldToScreenSpace(S2);
		WorldToScreenSpace(S3);

		m.transformPt(V1);
		m.transformPt(V2);
		m.transformPt(V3);
		m.transformPt(V4);

		float DY=V3.y-V4.y;
		float DX=V3.x-V4.x;

		if( fabs(DX) > fabs(DY) ){
			float A=-DY/DX;
			Matrix2DF M2(V1.x*A+V1.y,V2.x*A+V2.y,1,1);
			Matrix2DF M2I;
			M2.Inverse(M2I);
			Vector2Df S(S1.x,S2.x);
			Vector2Df K1020;
			K1020 = M2I.mul(S);

			float K00=A*K1020.x;
			Vector3D K011121 (S1.y,S2.y,S3.y);
			Matrix3D M3( V1.x, V2.x,    V3.x,
				V1.y, V2.y, V3.y,
				1   ,    1,    1);
			M3.inverse();
			K011121*=M3;			
			Matrix4D K
				(    K00, K011121.x, 0, 0,
				K1020.x, K011121.y, 0, 0,
				0,         0, 1, 0,
				K1020.y, K011121.z, 0, 1);
			if(K.e11<0||K.e11>3||K.e00<0||K.e00>3){
				K.setIdentity();
			}
			m*=K;
			return m;
		}else{
			float B=-DX/DY;
			Matrix2DF M2(V1.x+V1.y*B,V2.x+V2.y*B,1,1);
			Matrix2DF M2I;
			M2.Inverse(M2I);
			Vector2Df S(S1.x,S2.x);
			Vector2Df K0020;
			K0020 = M2I.mul(S);
			float K10=B*K0020.x;

			Vector3D K011121 (S1.y,S2.y,S3.y);
			Matrix3D M3( V1.x, V2.x,    V3.x,
				V1.y, V2.y, V3.y,
				1   ,    1,    1);
			M3.inverse();
			K011121*=M3;

			Matrix4D K
				(K0020.x, K011121.x, 0, 0,
				K10, K011121.y, 0, 0,
				0,         0, 1, 0,
				K0020.y, K011121.z, 0, 1);
			if(K.e11<0||K.e11>2||K.e00<0||K.e00>2){
				K.setIdentity();
			}
			m*=K;
			return m;
		}		
	}else{
		return GetPseudoProjectionTM(pos,planeFactor);
	}
}
Matrix4D GetPseudoProjectionTM3(const Vector3D& pos,NewMonster* NM,float& planeFactor){
	if(NM->Use3pAlign){
		float p=0;
		Matrix4D m=GetPseudoProjectionTM(pos,p);		

		int dx=NM->PicDx;
		int dy=NM->PicDy;

		Vector3D V1=SkewPt(dx+NM->AlignPt1x,(dy+NM->AlignPt1y+NM->AlignPt1z)*2,NM->AlignPt1z);
		Vector3D V2=SkewPt(dx+NM->AlignPt2x,(dy+NM->AlignPt2y+NM->AlignPt2z)*2,NM->AlignPt2z);
		Vector3D V3=SkewPt(dx+NM->AlignPt3x,(dy+NM->AlignPt3y+NM->AlignPt3z)*2,NM->AlignPt3z);

		int f1=NM->AlignPt1y;
		int f2=NM->AlignPt2y;
		int f3=NM->AlignPt3y;

		if(f1<f2&&f1<f3){
			swap(V3.x,V1.x);
			swap(V3.y,V1.y);
			swap(V3.z,V1.z);
		}else
			if(f2<f1&&f2<f3){
				swap(V2.x,V3.x);
				swap(V2.y,V3.y);
				swap(V2.z,V3.z);
			}

			float fx=(V3.x-V1.x)/(V2.x-V1.x);
			Vector3D V4=V2;
			V4*=fx;
			Vector3D V=V1;
			V*=(1-fx);
			V4+=V;

			Vector3D S1=V1;
			Vector3D S2=V2;
			Vector3D S3=V3;
			Vector3D P=pos;
			S1+=P;
			S2+=P;
			S3+=P;

			WorldToScreenSpace(S1);		
			WorldToScreenSpace(S2);
			WorldToScreenSpace(S3);

			m.transformPt(V1);
			m.transformPt(V2);
			m.transformPt(V3);
			m.transformPt(V4);

			float DY=V3.y-V4.y;
			float DX=V3.x-V4.x;

			if( fabs(DX) > fabs(DY) ){
				float A=-DY/DX;
				Matrix2DF M2(V1.x*A+V1.y,V2.x*A+V2.y,1,1);
				Matrix2DF M2I;
				M2.Inverse(M2I);
				Vector2Df S(S1.x,S2.x);
				Vector2Df K1020;
				K1020 = M2I.mul(S);

				Vector3D K011121 (S1.y,S2.y,S3.y-V3.x*K1020.x);
				Matrix3D M3( V1.x, V2.x,    0,
					V1.y, V2.y, V3.y,
					1   ,    1,    1);
				M3.inverse();
				K011121*=M3;
				float K00=A*K1020.x;
				Matrix4D K
					(    K00, K011121.x, 0, 0,
					K1020.x, K011121.y, 0, 0,
					0,         0, 1, 0,
					K1020.y, K011121.z, 0, 1);
				if(K.e11<0||K.e11>3||K.e00<0||K.e00>3){
					K.setIdentity();
				}
				m*=K;
				return m;
			}else{
				float B=-DX/DY;
				Matrix2DF M2(V1.x+V1.y*B,V2.x+V2.y*B,1,1);
				Matrix2DF M2I;
				M2.Inverse(M2I);
				Vector2Df S(S1.x,S2.x);
				Vector2Df K0020;
				K0020 = M2I.mul(S);
				float K10=B*K0020.x;

				Vector3D K011121 (S1.y,S2.y,S3.y-V3.x*K10);
				Matrix3D M3( V1.x, V2.x,    0,
					V1.y, V2.y, V3.y,
					1   ,    1,    1);
				M3.inverse();
				K011121*=M3;

				Matrix4D K
					(K0020.x, K011121.x, 0, 0,
					K10, K011121.y, 0, 0,
					0,         0, 1, 0,
					K0020.y, K011121.z, 0, 1);
				if(K.e11<0||K.e11>2||K.e00<0||K.e00>2){
					K.setIdentity();
				}
				m*=K;
				return m;
			}		
	}else{
		return GetPseudoProjectionTM(pos,planeFactor);
	}
}
Matrix4D GetPseudoProjectionTM2(const Vector3D& pos,NewMonster* NM,float& planeFactor){
	if(NM->Use3pAlign){
		float p=0;
        Matrix4D m=GetPseudoProjectionTM(pos,p);		

		int dx=NM->PicDx;
		int dy=NM->PicDy;

		Vector3D V1=SkewPt(dx+NM->AlignPt1x,(dy+NM->AlignPt1y+NM->AlignPt1z)*2,NM->AlignPt1z);
		Vector3D V2=SkewPt(dx+NM->AlignPt2x,(dy+NM->AlignPt2y+NM->AlignPt2z)*2,NM->AlignPt2z);
		Vector3D V3=SkewPt(dx+NM->AlignPt3x,(dy+NM->AlignPt3y+NM->AlignPt3z)*2,NM->AlignPt3z);

		Vector3D V1N=V1;
		Vector3D V2N=V2;
		Vector3D V3N=V3;
		Vector3D P=pos;
		V1+=P;
		V2+=P;
		V3+=P;

		WorldToScreenSpace(V1);		
		WorldToScreenSpace(V2);
		WorldToScreenSpace(V3);
		//WorldToScreenSpace(P);
		//V1-=P;
		//V2-=P;
		//V3-=P;

		Matrix3D XW(
			V1.x,V1.y,1,
			V2.x,V2.y,1,
			V3.x,V3.y,1);

		m.transformPt(V1N);
		m.transformPt(V2N);
		m.transformPt(V3N);

		Matrix3D X(
			V1N.x,V1N.y,1,
			V2N.x,V2N.y,1,
			V3N.x,V3N.y,1);
        X.inverse();
		X*=XW;
		Matrix4D K(
			X.e00,X.e01,0,0,
			X.e10,X.e11,0,0,
			    0,    0,1,0,
			X.e20,X.e21,0,1);
		m*=K;
		m.e10=0;
		return m;
		/*
		int dx=NM->PicDx;
		int dy=NM->PicDx;
        Vector3D V1=SkewPt(pos.x+dx+NM->AlignPt1x,pos.y+(dy+NM->AlignPt1y+NM->AlignPt1z)*2,pos.z+NM->AlignPt1z);
		Vector3D V2=SkewPt(pos.x+dx+NM->AlignPt2x,pos.y+(dy+NM->AlignPt2y+NM->AlignPt2z)*2,pos.z+NM->AlignPt2z);
		Vector3D V3=SkewPt(pos.x+dx+NM->AlignPt3x,pos.y+(dy+NM->AlignPt3y+NM->AlignPt3z)*2,pos.z+NM->AlignPt3z);
		Vector3D V4=SkewPt(pos.x,pos.y,pos.z);

		Matrix4D MR0(V1.x, V1.y, V1.z, 1.0f, 
			V2.x, V2.y, V2.z, 1.0f,
			V3.x, V3.y, V3.z, 1.0f,
			V4.x, V4.y, V4.z, 1.0f);

		Matrix4D MR(V1.x, V1.y, V1.z, 1.0f, 
					V2.x, V2.y, V2.z, 1.0f,
					V3.x, V3.y, V3.z, 1.0f,
					V4.x, V4.y, V4.z, 1.0f);
		WorldToScreenSpace(V1);		
		WorldToScreenSpace(V2);
		WorldToScreenSpace(V3);
		WorldToScreenSpace(V4);
		Matrix4D MS(V1.x, V1.y, V1.z, 1.0f, 
					V2.x, V2.y, V2.z, 1.0f,
					V3.x, V3.y, V3.z, 1.0f,
					V4.x, V4.y, V4.z, 1.0f );
		MR.inverse();
		MS.mulLeft(MR);
		MR0*=MS;
		return MS;
		*/
	}else{
        return GetPseudoProjectionTM(pos,planeFactor);
	}
}
bool g_bRenderShadowsPass = false;
extern Rct g_ScreenViewport;
extern int ExtraDir;
void PlayAnimationEx(OneObject* OB,NewAnimation* NA,int Frame,int x,int y);
NewAnimation* LastDrawnNA = NULL;
int LastFrame = 0;
void DrawCollision(OneObject* OB);
extern byte LockGrid;

void NewAnimation::DrawSpriteUnit( OneObject* OB, const Vector3D& pos, int frame, float Dir, byte NI )
{
    Vector3D wPos = SkewPt( pos.x, pos.y, pos.z );
    Vector3D sPos( wPos );
    WorldToScreenSpace( sPos );

    sPos.x = roundf( sPos.x );
    sPos.y = roundf( sPos.y );
    DWORD nationalColor = GetNatColor( NI );

    //  force to skip some frames according to animation quality
    extern int CurrentAnmQuality;
    if (CurrentAnmQuality > 0)
    {
        int r = CurrentAnmQuality + 1;
        frame = (((frame>>8)/r)*r) << 8;
    }

    //  frame index vodoo
    byte RealDir=byte(Dir);
    int octs,oc2,sesize,oc1,ocM,dir;				
    if(OB&&(Rotations==16||Rotations==9))
    {
        if(OB->OctantInfo==0xFF)
        {
            OB->OctantInfo=(OB->RealDir+8)>>4;
        }else{
            int cd=int(OB->OctantInfo&15)<<4;
            int dd=int(char(char(cd)-char(OB->RealDir)));
            int ad=abs(dd);
            if(ad<=8){
                OB->OctantInfo&=0x0F;
                RealDir=OB->OctantInfo<<4;
            }else if(ad<16){
                int ot=OB->OctantInfo>>4;
                if(ot<12){
                    RealDir=(OB->OctantInfo&15)<<4;
                    ot+=ad>>3;
                    OB->OctantInfo=(OB->OctantInfo&15)+(ot<<4);
                }else{
                    OB->OctantInfo=(OB->RealDir+8)>>4;
                    RealDir=OB->OctantInfo<<4;
                }
            }else{
                OB->OctantInfo=(OB->RealDir+8)>>4;
                RealDir=OB->OctantInfo<<4;
            }
        }
    }

    RealDir-=ExtraDir;

    if (Inverse) RealDir=128-RealDir;
    if (Rotations == 1)
    {
        octs=Rotations;
        oc2=Rotations;
        ocM=0;
        if(!octs)octs=1;
        sesize=0;
        oc1=octs;
        dir=0;
    }
    else if (Rotations & 1)
    {
        octs=(Rotations-1)*2;
        oc2=Rotations-1;
        if(!octs)octs=1;
        sesize=255/(octs*2);
        oc1=octs;
        ocM=oc2;
        dir=(((RealDir+64+sesize)&255)*octs)>>8;
    }
    else
    {				
        octs=Rotations;
        oc2=Rotations;
        ocM=0;
        if(!octs)octs=1;
        sesize=128/octs;
        oc1=octs;
        dir=(((RealDir+64+sesize+128)&255)*octs)>>8;
    }			

    byte dir2=dir;
    if (frame/256 >= NFrames)
    {
        return;
    }

    NewFrame* NF=Frames[frame/256];

    //  fade if sdoxlo
    if (OB&&OB->Sdoxlo)
    {
        FallStage=float(OB->Sdoxlo)/50.0f;
        if(FallStage>1.0f)FallStage=1.0f;

        int C=255-(OB->Sdoxlo/5);
        if(C<0)return;
        GPS.SetCurrentDiffuse((DWORD(C)<<24)+0x00808080);
    } else FallStage = 0;

    //  prevent z-fighting of the closely standing units
    const float c_ZVariation = 4.0f;
    const float c_ZAmplitude = c_DoublePI/100.0f;
    float zVariation = 0.0f;
    if (OB && OB->BrigadeID != 0xFFFF)
    {
        rndInit( OB->Serial );
        zVariation = sinf( wPos.x*c_ZAmplitude )*c_ZVariation;
    }
    extern Vector3D g_LastDir;
    Vector3D var( g_LastDir );
    var *= zVariation; 

    if (dir < ocM)
    //  reverse
    {
        Matrix4D tm;
        if(Inverse){
            Vector3D pivot( -NF->dx, -NF->dy, 0.0f );
            tm = (FallStage == 1.0f) ? 
                GetAlignTerraTransform( wPos, pivot )
                :	GetRolledBillboardTransform( pivot, 0.0f );
            tm.translate( wPos );
        }else{
            Vector3D pivot( NF->dx, -NF->dy, 0.0f );
            tm = (FallStage == 1.0f) ? 
                GetAlignTerraTransform( wPos, pivot )
                :	GetRolledBillboardTransform( pivot, 0.0f );					
            tm.getV0().reverse();
            tm.getV2().reverse();
            tm.translate( wPos );
        }
        
        tm.translate( var );

        if (DoubleAnm)
        {
            NewFrame* NF1=Frames[(frame>>8)+NFrames];

            int sp1 = oc2 - dir + Rotations*NF->SpriteID + 4096;
            int sp2 = oc2 - dir + Rotations*NF1->SpriteID + 4096;		


            DrawWorldSprite( NF->FileID, sp1, tm, NI );	  							
            DrawWorldSprite( NF1->FileID, sp2, tm, NI );	  

            if(OB&&!OB->Sdoxlo){
                RegisterVisibleGP( OB, NF->FileID, sp1, sPos.x + zoom( NF->dx ), sPos.y + zoom( NF->dy ) );
                RegisterVisibleGP( OB, NF1->FileID, sp2, sPos.x - zoom( NF1->dx ), sPos.y + zoom( NF1->dy ) );
            }
        }
        else
        {
            int sp = oc2 - dir + Rotations*NF->SpriteID;							
            DrawWorldSprite( NF->FileID, sp, tm, NI );	  				
            if(OB&&!OB->Sdoxlo){
                RegisterVisibleGP( OB, NF->FileID, Inverse ? sp : sp + 4096, sPos.x + zoom( NF->dx ), sPos.y + zoom( NF->dy ) );
            }
        }
    }
    else
        //  non-reverse
    {
        Matrix4D tm;
        if(Inverse){
            Vector3D pivot( NF->dx, -NF->dy, 0.0f );
            tm = (FallStage == 1.0f) ? 
                GetAlignTerraTransform( wPos, pivot )
                :	GetRolledBillboardTransform( pivot, 0.0f );					
            tm.getV0().reverse();
            tm.getV2().reverse();
            tm.translate( wPos );						
        }else{
            Vector3D pivot( -NF->dx, -NF->dy, 0.0f );
            tm = (FallStage == 1.0f) ? 
                GetAlignTerraTransform( wPos, pivot )
                :	GetRolledBillboardTransform( pivot, 0.0f );
            tm.translate( wPos );
        }

        tm.translate( var );
        dir=oc1-dir;
        if (DoubleAnm)
        {
            NewFrame* NF1 = Frames[(frame>>8)+NFrames];

            int sp1 = oc2 - dir + Rotations*NF->SpriteID;
            int sp2 = oc2 - dir + Rotations*NF1->SpriteID;

            DrawWorldSprite( NF->FileID, sp1, tm, NI );	  
            if(OB&&!OB->Sdoxlo){
                RegisterVisibleGP( OB, NF->FileID, sp1, sPos.x + zoom( NF->dx ), sPos.y + zoom( NF->dy ) );							
            }

            DrawWorldSprite( NF1->FileID, sp2, tm, NI );	  
            if(OB&&!OB->Sdoxlo){
                RegisterVisibleGP( OB, NF1->FileID, sp2, sPos.x + zoom( NF1->dx ), sPos.y + zoom( NF1->dy ) );
            }
        }
        else
        {
            int sp = oc2 - dir + Rotations*NF->SpriteID;
            DrawWorldSprite( NF->FileID, sp, tm, NI );	  
            if(OB&&!OB->Sdoxlo){
                RegisterVisibleGP( OB, NF->FileID, Inverse ? sp+4096 : sp, sPos.x + zoom( NF->dx ), sPos.y + zoom( NF->dy ) );
            }
        }
    }
} // DrawSpriteUnit

const int c_MaxSpriteBuildingParts = 512;
typedef Hash<SpriteBuilding,1779,c_MaxSpriteBuildingParts> SpriteBuildingHash;
SpriteBuildingHash  g_BldHash;
extern bool g_bCameraChanged;
bool        g_bUpdateBldHash = true;
bool        g_bForceBldCache = false;
void RegisterVisibleGP( OneObject* OB, SpriteBuilding* sb );

void NewAnimation::DrawSpriteBuilding( OneObject* OB, const Vector3D& pos, int frame, byte NI )
{
    if (!g_bCameraChanged) g_bUpdateBldHash = false;
    if (g_bCameraChanged && !g_bUpdateBldHash)
    {
        g_BldHash.reset();
        g_bUpdateBldHash = true;
        g_bForceBldCache = false;
    }

    // BAAAAAAD HACK.... I hate releases
    g_bForceBldCache = true;

    Vector3D wPos = SkewPt( pos.x, pos.y, pos.z );
    Vector3D sPos( wPos );
    WorldToScreenSpace( sPos );

    sPos.x = roundf( sPos.x );
    sPos.y = roundf( sPos.y );
    DWORD nationalColor = GetNatColor( NI );

    Matrix4D m; 
    float pf = 0.0f;
    if (IsPerspCameraMode())
    {
        pf = OB ? OB->newMons->PlaneFactor : 0.0f;
        m = OB ? GetPseudoProjectionTM( wPos, OB->newMons, pf ) : 
                 GetPseudoProjectionTM( wPos, pf );
    }

    //  iterate on building parts
    int s0 = frame/256;
    int s1 = s0;
    if (LineInfo)
    {
        s0 = 0;
        s1 = NFrames - 1;
    } 

    SpriteBuilding* pPart = NULL;
    for (int i = s0; i <= s1; i++)
    {
        NewFrame* NF = Frames[i];
        int gpID = NF->FileID;
        int frID = NF->SpriteID;

        bool bNeedUpdate = (g_bUpdateBldHash || g_bForceBldCache);

        SpriteBuilding sb( OB ? OB->Index : 0xBAADF00D, i + Code*1317, gpID, frID );
        int hID = g_BldHash.find( sb );
        if (hID == NO_ELEMENT)
        {
            bNeedUpdate = true;
			if (g_BldHash.numElem() < c_MaxSpriteBuildingParts) 
			{
				hID = g_BldHash.add( sb );
			}
			else continue;
        }
        SpriteBuilding* pNextPart = &g_BldHash.elem( hID );
        if (pPart) pPart->m_pNextPart = pNextPart;
        
        pPart = pNextPart;
        pPart->m_GPID    = gpID;
        pPart->m_FrameID = frID;
        if (!pPart->m_WorldPos.isEqual( wPos )) bNeedUpdate = true;

        if (bNeedUpdate)
        {
            Matrix4D fullTM = Matrix4D::identity;
            Vector3D pivot = SkewPt( -float( NF->dx ), -float( NF->dy ), 0.0f );
            if (LineInfo)
            {
                int p = i*4;
                int x1 = LineInfo[p	   ];
                int y1 = LineInfo[p + 1];
                int x2 = LineInfo[p + 2];
                int y2 = LineInfo[p + 3];

                if (x1 == c_AlignGround)
                {
                    fullTM = GetAlignGroundTransform( pivot );
                } 
                else if (x1 == c_AlignTopmost)
                {
                    fullTM = GetRolledBillboardTransform( pivot, 0 );
                }
                else
                { 
                    fullTM = GetAlignLineTransform( pivot, x1, y1, x2, y2 );
                }
            }
            else
            {
                fullTM *= GetRolledBillboardTransform( pivot, 0.0f );
                if (!IsPerspCameraMode())
                {
                    //  what is it for?..
                    fullTM.translate( SkewPt( 0, 80, 40 ) );
                }
            }

            if (!IsPerspCameraMode()) { m.translation( wPos ); m *= ICam->WorldToScreenSpace(); }
            fullTM *= m;
            
            pPart->m_SpriteToScreenTM = fullTM;
            pPart->m_ScreenToSpriteTM.inverse( fullTM );

            //  calculate screen-space bounding frame
            Rct rct;
            ISM->GetBoundFrame( gpID, frID, rct, nationalColor );
            Vector3D rb( rct.GetRight(), rct.GetBottom(), 0.0f );
            Vector3D lt( rct.x, rct.y, 0.0f );
            fullTM.transformPt( rb );
            fullTM.transformPt( lt );

            pPart->m_ScreenBounds.x = lt.x;
            pPart->m_ScreenBounds.y = lt.y;
            
            pPart->m_ScreenBounds.w = rb.x - lt.x;
            pPart->m_ScreenBounds.h = rb.y - lt.y;

            pPart->m_WorldPos = wPos;
        }
        
        //  clip to viewport
        const Rct& bound = pPart->m_ScreenBounds;
        if (bound.x > RealLx - 2 || bound.y > RealLy - 2 || 
            bound.GetRight() < 0 || bound.GetBottom() < 0) continue;
        
        const Matrix4D& tm = pPart->m_SpriteToScreenTM;
        if (ISM->HasColorData( Frames[0]->FileID ))
        //  building with normalized color
        {
            static int nshID = IRS->GetShaderID( "building_normcolor" );
            IRS->SetTextureFactor( ISM->GetCurrentDiffuse() );
            ISM->SetCurrentShader( nshID );
            ISM->DrawNSprite( pPart->m_GPID, pPart->m_FrameID, tm, nationalColor );
            ISM->FlushBatches();
        }
        else
        {
            ISM->DrawSprite( pPart->m_GPID, pPart->m_FrameID, tm, nationalColor );
        }

        RegisterVisibleGP( OB, pPart );

        DrawDebugBuildingInfo( OB );
        if (CINFMOD&&LineInfo)
        {
            Vector3D pivot1( -float( NF->dx ), -float( NF->dy*2 ), 0.0f );
            Vector3D lp = wPos;
            lp -= pivot1;
            WorldToScreenSpace( lp );

            int p = i*4;
            int x1 = LineInfo[p	   ] + lp.x;
            int y1 = LineInfo[p + 1] + lp.y;
            int x2 = LineInfo[p + 2] + lp.x;
            int y2 = LineInfo[p + 3] + lp.y;
            GPS.DrawLine( x1, y1, x2, y2, 0xFFFF0000 );
        }
    }
    ShowFiresNearBuilding( OB, m, pf );
} // DrawSpriteBuilding

//--------------------------------------------------------------------------
//	Func:	NewAnimation::DrawAt	
//	Desc:	Draws animated object instance at given position and given
//			animation time
//--------------------------------------------------------------------------
void NewAnimation::DrawAt(	int	frame,					//  animation frame
							byte NI,					//  nation index
							float x, float y, float z,	//  wolrd-space position
							float Dir,					//  direction
							float Scale,				//  scale
							DWORD Color,				//  color multiplier
							float fiDir,float fiOrt,	//  orientation
							OneObject* OB				//  object instance
						  )
{	
    //  skip weapons in shadow rendering pass
    if (Code == 0xBAADF00D && g_bRenderShadowsPass) return;

	LastDrawnNA     = this;
	LastFrame       = frame>>8;
	if (frame < 0) frame = 0;

	if (!IsVisible( OB, MyNation )) return;
	//if(LockGrid)DrawCollision(OB);

    Vector3D pos( x, y, z );

	//  play sound effect
	PlayAnimationEx( OB, this, frame, x, y );	

	//  slightly change unit color
	CurDiffuse=Color;
	VariateUnitColor( OB, Color );
	
	if(Code==34){//rotate on place
		Dir=0;
	}

	GPS.SetCurrentDiffuse(Color);
	Dir+=AddDirection;
	z+=AddHeight;

	if (AnimationType == atPatch)
	{
		Rct uv( TexXL, TexYL, TexXR - TexXL, TexYR - TexYL );
		if (VerticalPatch)
		{
			IPMgr->AddQuad( Vector3D( x, y, z ), 
				Vector3D( Dir, fiDir, fiOrt ), 
				Scale, 
				Color, 
				uv, PatchTextureID );
		}
		else
		{
			IPMgr->AddBillboard( Vector3D( x, y, z ), 
				Dir, Scale, Color, 
				uv, PatchTextureID );
		}
	}
	else if (AnimationType == at3D)
	{		
        Vector3D wPos = SkewPt( x, y, z );
        Vector3D sPos( wPos );
        WorldToScreenSpace( sPos );

        sPos.x = roundf( sPos.x );
        sPos.y = roundf( sPos.y );
        DWORD nationalColor = GetNatColor( NI );

		int Anim=-1;
		int Model=-1;
		float ATime=0;
		float exscale=1.0f;
		if(AnimSet3D.GetAmount()){			
            int cf=frame>>8;
			int df=0;
			int as_nf=AnimSet3D.GetAmount();
			int nf=0;
			for(int i=0;i<as_nf&&cf>=(nf=AnimSet3D[i]->NFrames);i++){				
				cf-=nf;
				df+=nf;
			}
			if(i>=as_nf)i=as_nf-1;
			AnimFrame3D* AF=AnimSet3D[i];
			Anim=AF->Animation;
			Model=AF->Model;
			int frame1=frame-(df<<8);
			if(AF->NFrames)ATime=AF->StartAnmTime+frame1*(AF->FinalAnmTime-AF->StartAnmTime)/AF->NFrames/256.0f;
			else ATime=0;
			if(ATime>AF->FinalAnmTime&&AF->FinalAnmTime>AF->StartAnmTime)ATime=AF->FinalAnmTime;
			exscale=AF->Scale;
			Dir+=AF->AddDir;
		}else{
			Anim=AnimationID;
			Model=ModelID;
			ATime=NFrames?frame/NFrames/256.0f:0;
		}
		IRS->SetTextureFactor(Color);
		if(OB&&OB->LockType==1){
			fiOrt+=0.02f*cos(float(GetTickCount())/400.0f);
			fiDir+=0.01f*cos(float(GetTickCount())/815.0f);
		}
        
        //  unit facing direction
        float ang = Dir*3.1415f/128.0f;

        //  build object orientation matrix
        float cosa = cosf( ang );
        float sina = sinf( ang );
        Vector3D vz = GetTotalNormal( x, y );
        Vector3D vx(  cosa, sina, 0.0f );
        Vector3D vy( -sina, cosa, 0.0f );

        //  for ground objects skew them to align with ground normal
        if (GetTotalHeight( x, y ) > 0.0f && Code != 0xBAADF00D)
        {
            vy.cross( vz, vx );
            vy.normalize();
            vx.cross( vy, vz );
        }
        vz = Vector3D::oZ;
        Matrix4D M4;
        M4.e00 = vx.x; M4.e01 = vx.y; M4.e02 = vx.z; M4.e03 = 0.0f;
        M4.e10 = vy.x; M4.e11 = vy.y; M4.e12 = vy.z; M4.e13 = 0.0f;
        M4.e20 = vz.x; M4.e21 = vz.y; M4.e22 = vz.z; M4.e23 = 0.0f;
        M4.e30 = 0.0f; M4.e31 = 0.0f; M4.e32 = 0.0f; M4.e33 = 1.0f;

        if (fabs( fiOrt ) > 0.0001f)
        {
            Matrix4D m;
            m.rotation( Vector3D::oX, fiOrt );
            M4.mulLeft( m );   
        }
        if (fabs( fiDir ) > 0.0001f) 
        {
            Matrix4D m;
            m.rotation( Vector3D::oY, fiDir );
            M4.mulLeft( m );
        }

		Matrix4D M41;	
        //  sink dead objects into the ground
		if (OB&&OB->Sdoxlo) z-=float(OB->Sdoxlo-50)/50.0f;

        //  scale-transform
        M41.st(this->Scale*Scale*exscale, Vector3D(x,y,z));
		M4 *= M41;
		M4 *= GetSkewTM();
		
        DWORD anmID     = 0xFFFFFFFF;
        float anmTime   = 0.0f;
		//  self-playing animation
		if(TimeAnimationID>0&&TimeAnimationFrames>0)
		{
			float atime=IMM->GetAnimTime(TimeAnimationID);
			if(atime>0.0001){
				extern int TrueTime;
				int frame=TrueTime+
					int(float(TimeAnimationVariation)*sin(float(TrueTime)/2000))+
					int(float(TimeAnimationVariation)*sin(float(TrueTime)/1654))+
					int(float(TimeAnimationVariation)*sin(float(TrueTime)/1113));
				anmTime = atime*float(frame%TimeAnimationFrames)/TimeAnimationFrames;
                anmID   = TimeAnimationID;
			}
		}

		float LPhase=0;
		float RPhase=0;
		
        IRS->SetTextureFactor( nationalColor );

        //  setup model animation
		if(Anim==-1)
		//  no animation
		{
			if (TimeAnimationID < 1)
            {
                anmTime = 0.0f;
                anmID   = 0;
            }
		}else
		{
			if(SecondAnimationID>0)
			//  double-animation
			{
				float AT=IMM->GetAnimTime(AnimationID);
				if(NFrames){		
					int NF=NFrames*256;
					int P=0;
					if(OB){
						P=OB->Phase;
					}
					float T1=(int(NF*100+frame+(P*DirFactor))%NF)*AT/NF;
					float T2=(int(NF*100+frame-(P*DirFactor))%NF)*AT/NF;
					IMM->Animate(ModelID,Anim,T1);
					LPhase=AT>0.00001?T1/AT:0;

                    anmTime = T2;
                    anmID   = SecondAnimationID;

					RPhase=AT>0.00001?T2/AT:0;
				}
                else 
                {
                    anmTime = 0.0f;
                    anmID   = Anim;
                }
			}
			else
			//  normal 3d animation
			{
				float AT = IMM->GetAnimTime(Anim);
                anmTime = NFrames ? ATime*AT : 0.0f;
                anmID   = Anim;
                LPhase=anmTime/AT;
				RPhase=anmTime/AT;
			}
		}

        //  rendering shadow
        if (g_bRenderShadowsPass)
        {
            if (ReflectionID <= 0)
            {
                IShadowMgr->AddCaster( ModelID, anmID, anmTime, M4 );
                IMM->RenderShadow( ModelID, &M4 );
            }
            return;
        }

        //  apply animation
        IMM->Animate( ModelID, anmID, anmTime );

        //  render water reflection
        if (ReflectionID > 0) IRMap->AddObject( ReflectionID, &M4 );

        if (OB) PushEntityContext( OB->Serial );
        
        //  render model
        IMM->Render(ModelID,&M4);
        
        //  register on-click
        if(OB&&!OB->Sdoxlo)RegisterVisibleGP( OB, ModelID, M4 );
		
		// on-click for carcass building
		int gp,fr;
		Vector3D C;
		if (IMM->GetModelGP( ModelID, gp, fr, C ))
		{
			if(OB&&!OB->Sdoxlo)RegisterVisibleGP( OB, gp, fr, sPos.x - C.x,sPos.y - C.y );
		}		

		//  particle emitter
		for(int i=0;i<Effects.GetAmount();i++){
			DWORD A=Effects[i]->GetAlpha(OB);			
			if(A){
				float f=Effects[i]->GetIntensity(OB);
				if(f>0.0f){
					IEffMgr->EnableAutoUpdate(true,6,3);
					IEffMgr->SetIntensity(Effects[i]->EffectFileID,Effects[i]->EffectNameID,f);
					IEffMgr->SetAlphaFactor(Effects[i]->EffectFileID,Effects[i]->EffectNameID,float(A)/255.0f);
					IEffMgr->UpdateInstance(Effects[i]->EffectFileID,Effects[i]->EffectNameID,M4);
					IEffMgr->EnableAutoUpdate(false);
					if(Effects[i]->SoundID>0){
						extern CDirSound* CDS;
						CDS->HitSound(Effects[i]->SoundID);
						AddEffect(x,y,Effects[i]->SoundID);
					}
				}
			}
		}
        if (OB) PopEntityContext();
	}
	else if (AnimationType == at2D)
	{		
		if ( (Rotations == 1 && (!OB || OB->NewBuilding)) ||
            (NFrames > 0 && ISM->HasDepthData( Frames[0]->FileID ) ))
		{
            DrawSpriteBuilding( OB, pos, frame, NI );
		}
		else
		{
            DrawSpriteUnit( OB, pos, frame, Dir, NI );
		}        
	}
	
	//  draw health for units
	extern byte PlayGameMode;
	if(OB&&(GetKeyState(VK_MENU)&0x8000)&&PlayGameMode==0)
	{
		void DrawHealth(OneObject* OB);
		DrawHealth(OB);
	}

	//dust&water
	extern int LastFlipTime;
	if (OB && OB->LockType != 1)
    {
		if(OB->GroupSpeed>20&&OB->RZ>15&&OB->DestX>0&&EngSettings.RunDustList.GetAmount())
        {
			int tofs=(OB->RealX>>9)+(OB->RealY>>9)*VertInLine;
			if(tofs>0&&tofs<MaxPointIndex){
				int T=TexMap[tofs];
				extern DWORD TXCOLOR[64];
				DWORD TC=TXCOLOR[T];

				int TB=TC&255;
				int TG=(TC>>8)&255;
				int TR=(TC>>16)&255;
				int TA=(TR+TG+TG+TB)/4;
				if(TG*8<TA*9&&TA>80&&TB<TA&&TA<180){
					int p=rand()%EngSettings.RunDustList.GetAmount();
					OneRandomEffectInfo* EF=EngSettings.RunDustList[p];
					static int LastEfTime=GetTickCount()-10000;
					int dt=(LastFlipTime-LastEfTime)*EF->MaxBirthPerSecond;			
					if(EF&&EF->Probability*dt*4>rand()){				
						PushEntityContext(OB->RealX+OB->RealY*37);
						IEffMgr->EnableAutoUpdate();
						int mid=EF->ModelID;
						Matrix4D M;
						M.scaling(EF->Scale);
						M.setTranslation(SkewPt(OB->RealX/16,OB->RealY/16,OB->RZ));
						IMM->Render(mid,&M);
						PopEntityContext();
						IEffMgr->EnableAutoUpdate( false );
						LastEfTime=LastFlipTime;
					}            
				}
			}
		}
		if(OB->RZ<=2&&EngSettings.WaterBlobsList.GetAmount()){
            int p=rand()%EngSettings.WaterBlobsList.GetAmount();
			OneRandomEffectInfo* EF=EngSettings.WaterBlobsList[p];
			static int LastEfTime=GetTickCount()-10000;
			int dt=(LastFlipTime-LastEfTime)*EF->MaxBirthPerSecond;			
			if(EF&&EF->Probability*dt*4>rand()){
                PushEntityContext(OB->RealX+OB->RealY*34);
				IEffMgr->EnableAutoUpdate();
				int mid=EF->ModelID;
				Matrix4D M;
				M.scaling(EF->Scale);
				M.setTranslation(SkewPt(OB->RealX/16,OB->RealY/16,OB->RZ));
				IMM->Render(mid,&M);
                IEffMgr->EnableAutoUpdate( false );
				PopEntityContext();
				LastEfTime=LastFlipTime;
			}
		}
	}

	//  additional animation (flags on buildings, for example)
	for(int i=0;i<AnmExt.GetAmount();i++)
	{
		byte D=byte(Dir);
		int dx=int(AnmExt[i]->dx*TCos[D]-AnmExt[i]->dy*TSin[D])>>8;
		int dy=int(AnmExt[i]->dy*TCos[D]+AnmExt[i]->dx*TSin[D])>>8;
		PushEntityContext(i*777+333);
		NewAnimation* NA=AnmExt[i]->NA;
		if(NA->NFrames)
		{
			int p = AnmExt[i]->Period;
			if (p == 0) p = 1;
			NA->DrawAt(	(NA->NFrames*256)*(GetTickCount()%p)/p,
						NI,
						x + dx, y + dy, z + AnmExt[i]->dz,
						Dir + AnmExt[i]->dDir,
						Scale*AnmExt[i]->Scale,
						Color, 0, 0, OB );
		}
		PopEntityContext();
	}
} // NewAnimation::DrawAt

bool OneBoundEffect::Parse(char* s,_str& ErrLog){
	char s1[64];
	char s2[64];
	int z=sscanf(s,"%s%s",s1,s2);
	if(z!=2)return false;
	EffectFile=s1;
	EffectName=s2;
	EffectFileID=IEffMgr->GetEffectSetID(s1);
	if(EffectFileID==-1){
		ErrLog.print("Unable to load effect: %s",s1);
		return false;
	}
	EffectNameID=IEffMgr->FindEffectByName(EffectFileID,s2);
	if(EffectNameID==-1){
		ErrLog.print("Unable to find effect <%s> in file <%s>",s2,s1);
		return false;
	}
	return true;
}
DWORD LifeDependentEffect::GetAlpha(OneObject* OB){
	if(OB&&LifePercent){
		if(OB->MaxLife){
            int A=255-OB->Life*255*100/OB->MaxLife/LifePercent;
			if(A<0)return 0;
			A*=2;
			if(A>255)A=255;
            return A;			
		}
	}
	return 0;
}
bool LifeDependentEffect::Parse(char* s,_str& ErrLog){
	char s1[64];
	char s2[64];	
	char s4[64];
	char ss[32]="";
	int lp;
	int z=sscanf(s,"%s%d%s%s%s",s4,&lp,s1,s2,ss);
	if(z<4)return false;
	if(strcmp(s4,"LIFE"))return false;
	LifePercent=lp;
	EffectFile=s1;
	EffectName=s2;
	EffectFileID=IEffMgr->GetEffectSetID(s1);
	if(EffectFileID==-1){
		ErrLog.print("Unable to load effect: %s",s1);
		return false;
	}
	EffectNameID=IEffMgr->FindEffectByName(EffectFileID,s2);
	SoundID=GetSound(ss);
	if(EffectNameID==-1){
		ErrLog.print("Unable to find effect <%s> in file <%s>",s2,s1);
		return false;
	}
	return true;
}
bool MoveDependentEffect::Parse(char* s,_str& ErrLog){
	char s1[64];
	char s2[64];	
	char s4[64];
	char ss[32]="";
	int lp;
	int z=sscanf(s,"%s%s%s%s",s4,s1,s2,ss);
	if(z<3)return false;
	if(strcmp(s4,"MOVE"))return false;
	EffectFile=s1;
	EffectName=s2;
	EffectFileID=IEffMgr->GetEffectSetID(s1);
	if(EffectFileID==-1){
		ErrLog.print("Unable to load effect: %s",s1);
		return false;
	}
	EffectNameID=IEffMgr->FindEffectByName(EffectFileID,s2);
	int GetSound(char* Name);
	SoundID=GetSound(ss);
	if(EffectNameID==-1){
		ErrLog.print("Unable to find effect <%s> in file <%s>",s2,s1);
		return false;
	}
	return true;
}
DWORD MoveDependentEffect::GetAlpha(OneObject* OBJ){
	return 0xFF;
}	
float MoveDependentEffect::GetIntensity(OneObject* OB){
	if(OB){
		if(OB->DestX>0){			
			return 1.0f;
		}
	}
	return 0;
}
