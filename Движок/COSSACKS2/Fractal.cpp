#include <math.h>
#include <stdlib.h>
//#include "StdAfx.h"
#define MLX 1024
#define MMSK 1023
#define MSHF 10
short fMap[MLX*MLX];
int mrpos2=20;
int XXP2=0;
extern short randoma[8192];
int mrand2(){
	mrpos2++;
	if(mrpos2>8191)XXP2+=0x3571;
	mrpos2&=8191;
	return (randoma[mrpos2]^XXP2)&32767;
};
int GetRand(int amp){
	return ((mrand2()*amp)>>14)-amp;
};
int GetfMap(int x,int y){
	x&=MMSK;
	y&=MMSK;
	return fMap[x+(y<<MSHF)];
};
void SetfMap(int x,int y,int val){
	if(x<0||y<0||x>=MLX||y>=MLX)return;
	fMap[x+(y<<MSHF)]=short(val);
};
void Generate(int N){
	int LX=MLX>>N;
	short A0=2048;
	int x,y;
	for(x=0;x<MLX;x+=LX){
		for(y=0;y<MLX;y+=LX){
			SetfMap(x,y,GetRand(A0));
		};
	};
	while(LX>1){
		///A0>>=1;
		A0=(A0*9)/20;
		for(x=0;x<MLX;x+=LX){
			for(y=0;y<MLX;y+=LX){
				int L1=LX>>1;
				SetfMap(x+L1,y,GetRand(A0)+((GetfMap(x,y)+GetfMap(x+LX,y))>>1));
				SetfMap(x+L1,y+LX,GetRand(A0)+((GetfMap(x,y+LX)+GetfMap(x+LX,y+LX))>>1));
				SetfMap(x,y+L1,GetRand(A0)+((GetfMap(x,y)+GetfMap(x,y+LX))>>1));
				SetfMap(x+LX,y+L1,GetRand(A0)+((GetfMap(x+LX,y)+GetfMap(x+LX,y+LX))>>1));
				SetfMap(x+L1,y+L1,GetRand(A0)+((GetfMap(x+L1,y)+GetfMap(x+L1,y+LX)+GetfMap(x,y+L1)+GetfMap(x+LX,y+L1))>>2));
			};
		};
		LX>>=1;
	};
	int fmin=100000;
	int fmax=-100000;
	for(x=0;x<MLX;x+=LX){
		for(y=0;y<MLX;y+=LX){
			int f=GetfMap(x,y);
			if(f<fmin)fmin=f;
			if(f>fmax)fmax=f;
		};
	};
	int dx=fmax-fmin;
	for(x=0;x<MLX;x+=LX){
		for(y=0;y<MLX;y+=LX){
			int f=(int(GetfMap(x,y)-fmin)<<9)/dx;
			SetfMap(x,y,(int(GetfMap(x,y)-fmin)<<9)/dx);
		};
	};
	//soft
	for(x=0;x<MLX;x+=LX){
		for(y=0;y<MLX;y+=LX){
			SetfMap(x,y,(GetfMap(x+1,y)+GetfMap(x-1,y)+GetfMap(x,y+1)+GetfMap(x,y-1)+GetfMap(x,y))/5);
		};
	};
};
#define scale 32
bool FGenerated=0;
void addmr1(int v,char* s,int L);
#define addmr(v) addmr1(v,__FILE__,__LINE__)
int GetFractalVal(int x,int y){
	if(!FGenerated){
		FGenerated=1;
		void CreateFractal();
		CreateFractal();
	}
	int x0=(x/scale)&MMSK;
	int y0=(y/scale)&MMSK;
	int x1=(x0+1)&MMSK;
	int y1=(y0+1)&MMSK;
	int dx=x%scale;
	int dy=y%scale;
	int v0=GetfMap(x0,y0);
	int v1=GetfMap(x1,y0);
	int v2=GetfMap(x0,y1);
	int v3=GetfMap(x1,y1);
    addmr(v0);
	addmr(v1);
	addmr(v2);
	addmr(v3);
	return v0+(v1-v0)*dx/scale+(v2-v0)*dy/scale+((v3+v0-v1-v2)*dx/scale)*dy/scale;
};
int GetFractalValEx(int x,int y,int Type){
	int V=GetFractalVal((x+y)*3/4,(x-y)*3/4);
	if(Type==0)return V;
	if(Type==1){//Strips
		V<<=1;
		if(V>512)V=1024-V;
		return V;
	}
	if(Type==2){//Combo
		int V1=GetFractalVal((x+y)*3/4+11329,(x-y)*3/4+13799)<<1;
		if(V1>512)V1=1024-V1;
		return V*V1/512;
    }
	if(Type==3){//Combo2
		V<<=1;
		int V1=GetFractalVal((x+y)*3/8+11929,(x-y)*3/8+19799);
		if(V>512)V=1024-V;
		return V*V1/512;
	}
	if(Type==4){//Circles
		V=V*3-256*3;
        if(V<0)V=-V;
		if(V>512)V=1024-V;
		return V;
	}
	return 0;
};
void CreateFractal(){
	mrpos2=20;
	XXP2=0;
	Generate(6);
};