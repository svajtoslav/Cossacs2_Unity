#include "stdheader.h"
#define FEXPATH
bool AllowPathDelay;

void UnLockComplexObject(OneObject* OB);
void LockComplexObject(OneObject* OB);

MotionBrush::MotionBrush(){
	Data=NULL;
	Lx=0;Ly=0;
	Dx=0;Dy=0;
};
MotionBrush::~MotionBrush(){
	if(Data){
		free(Data);
		Data=NULL;
		Lx=0;Ly=0;
		Dx=0;Dy=0;
	};
};
void MotionBrush::CreateRoundBrush(int d){
	Data=znew(int,d);
	memset(Data,0,d<<2);
	for(int y=0;y<d;y++)
		for(int x=0;x<d;x++){
			int rr=int(sqrt((x+x-d)*(x+x-d)+(y+y-d)*(y+y-d)));
			if(rr<d+1){
				Data[y]|=1<<x;
			};
		};
	Lx=d;
	Ly=d;
	Dx=d>>1;
	Dy=d>>1;
};
void MotionBrush::CreateSquareBrush(int d){
	Data=znew(int,d);
	memset(Data,0,d<<2);
	for(int y=0;y<d;y++)for(int x=0;x<d;x++)Data[y]|=1<<x;
	Lx=d;
	Ly=d;
	Dx=d>>1;
	Dy=d>>1;
};



//-------------------------------------------------------//
//                                                       //
//                    ÎÁÕÎÄ ÏÐÅÏßÒÑÒÂÈÉ                  //
//                                                       //
//-------------------------------------------------------//

const int Power2[24]={
		0x00000001,0x00000003,0x00000007,0x0000000F,
		0x0000001F,0x0000003F,0x0000007F,0x000000FF,
		0x000001FF,0x000003FF,0x000007FF,0x00000FFF,
		0x00001FFF,0x00003FFF,0x00007FFF,0x0000FFFF,
		0x0001FFFF,0x0003FFFF,0x0007FFFF,0x000FFFFF,
		0x001FFFFF,0x003FFFFF,0x007FFFFF,0x00FFFFFF};
void MotionField::ClearMaps(){
	//memset(MapH,0,MAPSY*BMSX);
	memset(MapV,0,MAPSY*BMSX);
};
void MotionField::BSetPt(int x,int y){
	//int MAPH=int(MapH);
	int MAPV=int(MapV);
	if(x>=0&&x<MAPSX&&y>=0&&y<MAPSY){
		switch(ADDSH){
		case 1:
			__asm{
				//Horisontal
				/*
				mov		eax,x
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,y
				shl		ebx,7//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPH
				or		[ebx],al
				*/
				//Vertical
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,7//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPV
				or		[ebx],al
			};
			break;
		case 2:
			__asm{
				//Horisontal
				/*
				mov		eax,x
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,y
				shl		ebx,8//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPH
				or		[ebx],al
				*/
				//Vertical
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,8//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPV
				or		[ebx],al
			};
			break;
		case 3:
			__asm{
				//Horisontal
				/*
				mov		eax,x
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,y
				shl		ebx,9//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPH
				or		[ebx],al
				*/
				//Vertical
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,9//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPV
				or		[ebx],al
			};
			break;
		};
	};
};
void MotionField::BClrPt(int x,int y){
	//int MAPH=int(MapH);
	int MAPV=int(MapV);
	if(x>=0&&x<MAPSX&&y>=0&&y<MAPSY){
		switch(ADDSH){
		case 1:
			__asm{
				//Horisontal
				/*
				mov		eax,x
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,y
				shl		ebx,7//MAPSHF
				add		ebx,eax
				mov		al,254	
				rol		al,cl
				add		ebx,MAPH
				and		[ebx],al
				*/
				//Vertical
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,7//MAPSHF
				add		ebx,eax
				mov		al,254
				rol		al,cl
				add		ebx,MAPV
				and 	[ebx],al
			};
			break;
		case 2:
			__asm{
				//Horisontal
				/*
				mov		eax,x
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,y
				shl		ebx,8//MAPSHF
				add		ebx,eax
				mov		al,254	
				rol		al,cl
				add		ebx,MAPH
				and		[ebx],al
				*/
				//Vertical
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,8//MAPSHF
				add		ebx,eax
				mov		al,254
				rol		al,cl
				add		ebx,MAPV
				and 	[ebx],al
			};
			break;
		case 3:
			__asm{
				//Horisontal
				/*
				mov		eax,x
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,y
				shl		ebx,9//MAPSHF
				add		ebx,eax
				mov		al,254	
				rol		al,cl
				add		ebx,MAPH
				and		[ebx],al
				*/
				//Vertical
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,9//MAPSHF
				add		ebx,eax
				mov		al,254
				rol		al,cl
				add		ebx,MAPV
				and 	[ebx],al
			};
			break;
		};

	};
};
void MotionField::BXorPt(int x,int y){
	int MAPV=int(MapV);
	if(x>=0&&x<MAPSX&&y>=0&&y<MAPSY){
		switch(ADDSH){
		case 1:
			__asm{
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,7//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPV
				xor		[ebx],al
			};
			break;
		case 2:
			__asm{
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,8//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPV
				xor		[ebx],al
			};
			break;
		case 3:
			__asm{
				mov		eax,y
				mov		ecx,eax
				and		ecx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,9//MAPSHF
				add		ebx,eax
				mov		al,1
				shl		al,cl
				add		ebx,MAPV
				xor		[ebx],al
			};
			break;
		};
	};
};
void MotionField::BSetBar(int x,int y,int Lx){
	for(int ix=x;ix<x+Lx;ix++)
		for(int iy=y;iy<y+Lx;iy++)BSetPt(ix,iy);
};
void MotionField::BClrBar(int x,int y,int Lx){
	for(int ix=x;ix<x+Lx;ix++)
		for(int iy=y;iy<y+Lx;iy++)BClrPt(ix,iy);
};
void MotionField::BXorBar(int x,int y,int Lx){
	for(int ix=x;ix<x+Lx;ix++)
		for(int iy=y;iy<y+Lx;iy++)BXorPt(ix,iy);
};
void MotionField::BSetSQ(int x,int y,int Lx,int Ly){
	for(int ix=x;ix<x+Lx;ix++)
		for(int iy=y;iy<y+Ly;iy++)BSetPt(ix,iy);
};
void MotionField::BXorSQ(int x,int y,int Lx,int Ly){
	for(int ix=x;ix<x+Lx;ix++)
		for(int iy=y;iy<y+Ly;iy++)BXorPt(ix,iy);
};
void MotionField::BClrSQ(int x,int y,int Lx,int Ly){
	for(int ix=x;ix<x+Lx;ix++)
		for(int iy=y;iy<y+Ly;iy++)BClrPt(ix,iy);
};
int MotionField::CheckVLine(int x,int y,int Lx){
	int MAPV=int(MapV);
	if(x>0&&y>0&&y+Lx-1<MAPSY&&x<MAPSX&&Lx<=24){
		switch(ADDSH){
		case 1:
			__asm{
				mov		eax,y
				mov		edx,eax
				and		edx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,7//MAPSHF
				add		ebx,eax
				mov		ecx,Lx
				mov		eax,1
				shl		eax,cl
				dec		eax
				mov		cl,dl
				rol		eax,cl
				add		ebx,MAPV
				and		eax,dword ptr [ebx]
			};
			break;
		case 2:
			__asm{
				mov		eax,y
				mov		edx,eax
				and		edx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,8//MAPSHF
				add		ebx,eax
				mov		ecx,Lx
				mov		eax,1
				shl		eax,cl
				dec		eax
				mov		cl,dl
				rol		eax,cl
				add		ebx,MAPV
				and		eax,dword ptr [ebx]
			};
			break;
		case 3:
			__asm{
				mov		eax,y
				mov		edx,eax
				and		edx,7
				shr		eax,3
				mov		ebx,x
				shl		ebx,9//MAPSHF
				add		ebx,eax
				mov		ecx,Lx
				mov		eax,1
				shl		eax,cl
				dec		eax
				mov		cl,dl
				rol		eax,cl
				add		ebx,MAPV
				and		eax,dword ptr [ebx]
			};
			break;
		};
		return;
	}else return true;
};
bool MotionField::CheckBar(int x,int y,int Lx,int Ly){
	for(int ix=0;ix<Lx;ix++)
		if(CheckVLine(x+ix,y,Ly))return true;
	return false;
}; 
DynArray<char*> SqList;
_inline void InitSqList(){
	if(!SqList.GetAmount()){
		for(int i=0;i<23;i++){
			char* L=new char[i+1];
			for(int j=0;j<=i;j++){
				int R=(i+1)*2-1;
				int x=j*4-i*2;
				float V=sqrt(R*R-x*x);
				if(i&1){					
					L[j]=2*int((V+2)/4);
				}else{
					L[j]=2*int(V/4)+1;
				}
			}
			SqList.Add(L);
		}
	}
}
bool MotionField::CheckRound(int x,int y,int D){
	InitSqList();
	if(D<1)return false;
    char* Li=SqList[D-1];
	for(int i=0;i<D;i++){
		int L=Li[i];
		if(CheckVLine(x+i,y+(D-L)/2,L))return true;
	}
	return false;
}
void MotionField::SetRound(int x,int y,int D){
	InitSqList();
	if(D<1)return;
	char* Li=SqList[D-1];
	for(int i=0;i<D;i++){
		int L=Li[i];
		int y0=y+(D-L)/2;
		for(int j=0;j<L;j++){
            BSetPt(x+i,y0+j);
		}		
	}	
}
void MotionField::ClearRound(int x,int y,int D){
	InitSqList();
	if(D<1)return;
	char* Li=SqList[D-1];
	for(int i=0;i<D;i++){
		int L=Li[i];
		int y0=y+(D-L)/2;
		for(int j=0;j<L;j++){
			BClrPt(x+i,y0+j);
		}		
	}	
}
void MotionField::XorRound(int x,int y,int D){
	InitSqList();
	if(D<1)return;
	char* Li=SqList[D-1];
	for(int i=0;i<D;i++){
		int L=Li[i];
		int y0=y+(D-L)/2;
		for(int j=0;j<L;j++){
			BClrPt(x+i,y0+j);
		}		
	}	
}
extern byte NewCirc[16];
void MotionField::FillNewCirc(int x,int y,int Lx,int Ly){
	int* xx=(int*)NewCirc;
	xx[0]=0;
	xx[1]=0;
	if(CheckHLine(x,y-1,Lx)){
		NewCirc[0]=1;
	};
	if(CheckHLine(x,y+Ly,Lx)){
		NewCirc[4]=1;
	};
	if(CheckVLine(x+Lx,y,Ly)){
		NewCirc[2]=1;
	};
	if(CheckVLine(x-1,y,Ly)){
		NewCirc[6]=1;
	};
	if(Ly<2){
		if(CheckPt(x-1,y-1))NewCirc[7]=1;
		if(CheckPt(x+Lx,y-1))NewCirc[1]=1;
		if(CheckPt(x-1,y+Ly))NewCirc[5]=1;
		if(CheckPt(x+Lx,y+Ly))NewCirc[3]=1;
	}else{
		if(Ly==2){
			if(CheckHLine(x+1,y-1,Lx)&&CheckPt(x+Lx,y))NewCirc[1]=1;
			if(CheckHLine(x+1,y+Ly,Lx)&&CheckPt(x+Lx,y+1))NewCirc[3]=1;
			if(CheckHLine(x-1,y+Ly,Lx)&&CheckPt(x-1,y+1))NewCirc[5]=1;
			if(CheckHLine(x-1,y-1,Lx)&&CheckPt(x-1,y))NewCirc[7]=1;
		}else{
			if(CheckHLine(x+1,y-1,Lx)||CheckVLine(x+Lx,y,Ly-1))NewCirc[1]=1;
			if(CheckHLine(x+1,y+Ly,Lx)||CheckVLine(x+Lx,y+1,Ly-1))NewCirc[3]=1;
			if(CheckHLine(x-1,y+Ly,Lx)||CheckVLine(x-1,y+1,Ly-1))NewCirc[5]=1;
			if(CheckHLine(x-1,y-1,Lx)||CheckVLine(x-1,y,Ly-1))NewCirc[7]=1;
		};
	};
	xx[2]=xx[0];
	xx[3]=xx[1];
};
//------------------------------------------------//
//          KERNEL OF THE MOTION ENGINE           //
//            Search for the best way             //
//------------------------------------------------//
#define MaxP (4096)
extern word pxx[MaxP+160];
extern word pyy[MaxP+160];
extern word RVIS[MaxP+160];
extern word LVIS[MaxP+160];
int GetLAngle(int dx,int dy,int Angle);
int GetRAngle(int dx,int dy,int Angle);
const int ndrr[9]={7,6,5,0,0,4,1,2,3};
const nidrx[8]={0,1,1,1,0,-1,-1,-1};
const nidry[8]={-1,-1,0,1,1,1,0,-1};

extern MotionField MFIELDS[NMFIELDS];//0-Land,1-Water

#ifdef COSSACKS2
void XorBlockComplexUnit(OneObject* OB);
#else
#define XorBlockComplexUnit(OB)
#endif 
#define XTEST(OB) XorBlockComplexUnit(OB);
#define OTSTUP 1
bool FastFindBestPositionOLD(OneObject* OB,int* xd,int* yd,int R0){
	MotionField* MFI=&MFIELDS[OB->LockType];
	if(!OB)return false;	
	if(!MFI->CheckPt(*xd,*yd)){
		return true;
	};
	if(R0>20)R0=20;
	int x=OB->x;
	int y=OB->y;
	int x0=*xd;
	int y0=*yd;
	for(int i=1;i<R0;i+=3){
		int N=Rarr[i].N;
		int bd=100000;
		int xb,yb;
		char* xi=Rarr[i].xi;
		char* yi=Rarr[i].yi;
		for(int j=0;j<N;j++){
            int xx=x0+xi[j];
			int yy=y0+yi[j];
			if(!MFI->CheckPt(xx,yy)){
                int r=Norma(xx-x,yy-y);
				if(r<bd){
					bd=r;
					xb=xx;
					yb=yy;
				}
			}
		}
		if(bd<100000){
			*xd=xb;
			*yd=yb;
			return true;
		}
	}
	return false;
}
bool FindBestPositionOLD(OneObject* OB,int* xd,int* yd,int R0){
	bool ps=OB->newMons->Peasant;
	if(OB->Lx==1&&!ps)return FastFindBestPositionOLD(OB,xd,yd,R0);
	MotionField* MFI=&MFIELDS[OB->LockType];
	if(!OB)return false;
	//XTEST(OB);
	int Otstup=ps?1:0;
	if(OB->Lx>1)Otstup=3;
	if(!MFI->CheckRound((*xd)-Otstup,(*yd)-Otstup,OB->Lx+Otstup*2)){
		//XTEST(OB);
		return true;
	};
	int Lx=OB->Lx+Otstup*2;
	int R1=R0;
	int bx=*xd;
	int by=*yd;
	int bdist=100000;
	int xxx=bx-Otstup;
	int yyy=by-Otstup;
	int LL=2;
	int bx1,by1,dstn;
	int x=OB->x;
	int y=OB->y;
	while(R1){
		for(int i=0;i<=LL;i++)if(!MFI->CheckRound(xxx+i-Otstup,yyy-Otstup,Lx)){
			bx1=xxx+i;
			by1=yyy;
			dstn=Norma(bx1-x,by1-y);
			if(dstn<bdist){
				bx=bx1;
				by=by1;
				bdist=dstn;
			};
		};
		for(i=0;i<=LL;i++)if(!MFI->CheckRound(xxx+i-Otstup,yyy+LL-Otstup,Lx)){
			bx1=xxx+i;
			by1=yyy+LL;
			dstn=Norma(bx1-x,by1-y);
			if(dstn<bdist){
				bx=bx1;
				by=by1;
				bdist=dstn;
			};
		};
		for(i=0;i<LL-1;i++)if(!MFI->CheckRound(xxx-Otstup,yyy+i-Otstup,Lx)){
			bx1=xxx;
			by1=yyy+i;
			dstn=Norma(bx1-x,by1-y);
			if(dstn<bdist){
				bx=bx1;
				by=by1;
				bdist=dstn;
			};
		};
		for(i=0;i<LL-1;i++)if(!MFI->CheckRound(xxx+LL-Otstup,yyy+i-Otstup,Lx)){
			bx1=xxx+LL;
			by1=yyy+i;
			dstn=Norma(bx1-x,by1-y);
			if(dstn<bdist){
				bx=bx1;
				by=by1;
				bdist=dstn;
			};
		};
		if(bdist<100000){
			*xd=bx;
			*yd=by;
			//XTEST(OB);
			return true;
		};
		R1--;
		LL+=2;
		xxx--;
		yyy--;
	};
	//XTEST(OB);
	return false;
};
bool FindBestPosition(OneObject* OB,int* xd,int* yd,int R0){
	if(*xd<-40||*yd<-30||*xd>1000||*yd>1000){
		return false;
	}
	if(Norma(OB->x-*xd,OB->y-*yd)<30){
		return FindBestPositionOLD(OB,xd,yd,R0);
	}
	bool ps=OB->newMons->Peasant;
	//if(OB->Lx==1&&!ps)return FastFindBestPosition(OB,xd,yd,R0);
	MotionField* MFI=&MFIELDS[OB->LockType];
	if(!OB)return false;
	XTEST(OB);
	int Otstup=ps?1:0;
	if(OB->Lx>1)Otstup=3;
	if(!MFI->CheckRound((*xd)-Otstup,(*yd)-Otstup,OB->Lx+Otstup*2)){
		XTEST(OB);
		return true;
	};
	int Lx=OB->Lx+Otstup*2;
	int R1=R0;
	int bx=*xd;
	int by=*yd;
	int bdist=100000;
	int xxx=bx-Otstup;
	int yyy=by-Otstup;
	int LL=2;
	int bx1,by1,dstn;
	int x=OB->x;
	int y=OB->y;
	int N=Norma(bx-x,by-y)+1;
	if(N<30){

	}
	
	for(int i=0;i<N;i+=2){
		int xxx=bx+(x-bx)*i/N;
		int yyy=by+(y-by)*i/N;
		if(!MFI->CheckRound(xxx-Otstup,yyy-Otstup,Lx)){
			*xd=xxx;
			*yd=yyy;
			XTEST(OB);
			return true;
		}
	}
	XTEST(OB);
	return FindBestPositionOLD(OB,xd,yd,R0);
	return false;	
};
extern int COUNTER;
void CreateFullPath(int x1,int y1,OneObject* OB){
	if(x1<4||y1<4)return;
	if(OB->PathDelay){
		OB->PathDelay--;
		return;
	}
	addrand(x1);
	addrand(y1);
	OB->x=(OB->RealX-(int(OB->Lx)<<7))>>8;
	OB->y=(OB->RealY-(int(OB->Lx)<<7))>>8;
    OB->DeletePath();
	OB->NIPoints=0;
	int xx1=x1;
	int yy1=y1;
	if(!FindBestPosition(OB,&xx1,&yy1,40))return;
	FastFreeMem(OB->PathX);
	FastFreeMem(OB->PathY);
	//free(OB->PathX);
	//free(OB->PathY);
	int xx0=OB->x;
	int yy0=OB->y;
	MotionField* MF=MFIELDS+OB->LockType;
	if(OB->GLock)MF->ClearRound(OB->x,OB->y,OB->Lx);
	if(!FindBestPosition(OB,&xx0,&yy0,40)){
		if(OB->GLock)MF->SetRound(OB->x,OB->y,OB->Lx);
		return;
	}	
	//UnLockComplexObject(OB);
	OB->PathX=0;
	OB->PathY=0;	
	MF->CurLx=OB->Lx;
	addrand(xx0);
	addrand(yy0);
	addrand(xx1);
	addrand(yy1);
	addrand(OB->LockType);
	FPoint* FP=MF->GetPath(xx0,yy0,xx1,yy1);
	//LockComplexUnit(OB);
	if(FP){
		int N;
		/*if(OB->Lx==1){
			FP=MF->MainPath.First;
			N=MF->MainPath.Len;
		}else{*/
			int NB=2+(OB->Index%3);
			if(NB)MF->Bending(NB);
			FP=MF->WayPoints.First;
			N=MF->WayPoints.Len;
		//}
		OB->PathX=(short*)FastGetMem(N<<1);
		OB->PathY=(short*)FastGetMem(N<<1);
		for(int i=0;i<N&&FP;i++){
			OB->PathX[N-i-1]=FP->x;
			OB->PathY[N-i-1]=FP->y;
			addrand(FP->x);
			addrand(FP->y);
			FP=FP->Next;
		}
		OB->NIPoints=N;
		addrand(N);
	}else OB->PathDelay=40;
	if(OB->GLock)MF->SetRound(OB->x,OB->y,OB->Lx);
};
//void addrand(int i);
extern int COUNTER;
void CorrectLockPosition(OneObject* OB){
	MotionField* MFI=MFIELDS+OB->LockType;
	int x0=OB->x;
	int y0=OB->y;
	int Lx=OB->Lx;
	for(int r=1;r<40;r++){
		char* xi=Rarr[r].xi;
		char* yi=Rarr[r].yi;
		int N=Rarr[r].N;
		for(int j=0;j<N;j++){
			int x1=x0+xi[j];
			int y1=y0+yi[j];
			if(!MFI->CheckBar(x1,y1,Lx,Lx)){
				OB->x=x1;
				OB->y=y1;
				OB->RealX=(x1<<8)+(Lx<<7);
				OB->RealY=(y1<<8)+(Lx<<7);
				addrand(OB->RealX);addrand(OB->RealY);
				return;
			};
		};
	};
};
#undef CreatePath(x1,y1)
void XorBlockComplexUnit(OneObject* OB);

void OneObject::ReallyCreatePath(int x1,int y1){
	//UnLockComplexObject(this);
	addrand(x1);
	addrand(y1);
	if(UnlimitedMotion){
		DestX=(x1<<8)+((Lx)<<7);
		DestY=(y1<<8)+((Lx)<<7);
		addrand(DestX);
		addrand(DestY);
		CPdestX=-1;
		CPdestY=-1;
		//LockComplexUnit(this);
		return;
	};
	MotionField* MFI=&MFIELDS[LockType];
	int D=1;//(newMons->StopDistance>>8)+1;
	if(LockType==1)D=3;

	if(Lx>2&&CObjIndex!=0xFFFF){
        itr_UnitsInRadius.Create(RealX/16,RealY/16,400);
		while(OneObject* OB=itr_UnitsInRadius.Next()){
			if(OB->Index!=Index&&OB->Sdoxlo==false&&OB->Lx>2&&OB->DestX>0&&OB->CObjIndex!=0xFFFF){
				if(rando()<700)CPdestX=x1-1;
                break;
			}
		}
	}

	if(CPdestX==x1&&CPdestY==y1){
		if(PathX){
			if(NIPoints){
				int cx=PathX[NIPoints-1];
				int cy=PathY[NIPoints-1];
				int dis=DistTo(cx,cy);
				bool CheckDirectConnectionToDestPoint(int xc,int yc,int x,int y);				
				if(dis<=D)NIPoints--;//shoul be changed
				if(NIPoints){
					cx=PathX[NIPoints-1];
				    cy=PathY[NIPoints-1];
					if(GLock)MFIELDS[1].ClearRound(x,y,Lx);					

					DestX=(cx<<8)+((Lx)<<7);
					DestY=(cy<<8)+((Lx)<<7);
					addrand(DestX);
					addrand(DestY);
                    bool L=MFI->CheckRound(x,y,Lx);
					if(MFI->CheckRound(cx,cy,Lx)||(L&&rando()<1024)){
						if(L)PathDelay=5;
						NIPoints=0;
						FastFreeMem(PathX);
						FastFreeMem(PathY);
						///free(PathX);
						///free(PathY);						
						PathX=NULL;
						PathY=NULL;
						addrand(22);
						CreateFullPath(x1,y1,this);
					}
					if(GLock)MFIELDS[1].SetRound(x,y,Lx);					
				}else{
					FastFreeMem(PathX);
					FastFreeMem(PathY);
//					free(PathX);
//					free(PathY);
					PathX=NULL;
					PathY=NULL;
					addrand(24);
					PathDelay=0;
					addrand(29);
					//XorBlockComplexUnit(this);
					CreateFullPath(x1,y1,this);
					//XorBlockComplexUnit(this);
					if(NIPoints==1){
						int cx1=PathX[0];
						int cy1=PathY[0];
						if(Norma(cx1-x,cy1-y)<3){
							FastFreeMem(PathX);
							FastFreeMem(PathY);
							PathX=NULL;
							PathY=NULL;
							NIPoints=0;
						};
					};
				};
			}else{
				addrand(34);
				//XorBlockComplexUnit(this);
				CreateFullPath(x1,y1,this);
				//XorBlockComplexUnit(this);
			};
		}else{
			if(GLock)MFIELDS[1].ClearRound(x,y,Lx);					
			if((MFI->CheckRound(x,y,Lx)&&rando()<512)||NIPoints==0){
				NIPoints=0;
				addrand(52);
				//XorBlockComplexUnit(this);
				CreateFullPath(x1,y1,this);
				//LockComplexUnit(this);
				CPdestX=x1;
				CPdestY=y1;
				if(GLock)MFIELDS[1].SetRound(x,y,Lx);					
				return;
			}
			if(GLock)MFIELDS[1].SetRound(x,y,Lx);					
		};
	}else{
		if(PathX){
			FastFreeMem(PathX);
			FastFreeMem(PathY);
//			free(PathX);
//			free(PathY);
			NIPoints=0;
			PathX=NULL;
			PathY=NULL;
		};
		addrand(19);		
		CreateFullPath(x1,y1,this);
		//LockComplexUnit(this);
		CPdestX=x1;
		CPdestY=y1;
		return;
	};
	CPdestX=x1;
	CPdestY=y1;
	//LockComplexUnit(this);
};

extern MotionField TempMF;
bool MotionField::fbCheckPt(int x, int y){
	bool R=TempMF.CheckPt(x,y);	
	return R;
}
void MotionField::fbBSetPt(int x, int y){
	TempMF.BSetPt(x,y);
}
void MotionField::fbBClrPt(int x, int y){
	TempMF.BClrPt(x,y);
}
//--------------PATHFINDING IN SEVERAL STEPS---------------//
struct OnePathRequest{
	bool Enabled;
	word Index;
	word Serial;
	int  toX;
	int  toY;
};
DynArray<OnePathRequest> PathReq;
void PrepareToPathfinding(){
    PathReq.FastClear();
}
void AddPathRequestor(OneObject* OB,int xd,int yd){
	OnePathRequest PR;
	PR.Enabled=true;
	PR.Index=OB->Index;
	PR.Serial=OB->Serial;
	PR.toX=xd;
	PR.toY=yd;
	PathReq.Add(PR);
}
void DisablePathRequest(OneObject* OB){
	for(int i=0;i<PathReq.GetAmount();i++){
		OnePathRequest& PR=PathReq[i];
		if(PR.Index==OB->Index && PR.Serial==OB->Serial){
			PR.Enabled=false;			
		}
	}
}
void PerformPathFiding(){
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB&&OB->CObjIndex!=0xFFFF){
			if(OB->StandTime<8)UnLockComplexObject(OB);
		}
	}
	for(int i=0;i<PathReq.GetAmount();i++){
		OnePathRequest& PR=PathReq[i];
		if(PR.Enabled){
			OneObject* OB=Group[PR.Index];
			if(OB&&OB->Serial==PR.Serial){
				OB->ReallyCreatePath(PR.toX,PR.toY);
			}
		}
	}
	for(int i=0;i<MAXOBJECT;i++){
		OneObject* OB=Group[i];
		if(OB&&OB->CObjIndex!=0xFFFF){
			LockComplexObject(OB); 
		}
	}
	PrepareToPathfinding();
}
void OneObject::CreatePath(int x1,int y1){
	AddPathRequestor(this,x1,y1);
}