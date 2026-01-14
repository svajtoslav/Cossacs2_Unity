#include "stdheader.h"
#include "Iterators.h"

word GetOneBld(int cell,int pos);


void units_iterator::UnitsInRadius::Create(int xc,int yc,int R){
	nids=0;
	pos=0;
	int NR=(R>>7)+1;
	int cx0=(xc>>7)+1;
	int cy0=(yc>>7)+1;
	int NN=0;    
	_xc=xc;
	_yc=yc;
	_R2=R*R;
	for(int i=0;i<NR;i++){
		char* xi=Rarr[i].xi;
		char* yi=Rarr[i].yi;
        int N=Rarr[i].N;
		for(int j=0;j<N;j++){
			int xx=cx0+xi[j];
			int yy=cy0+yi[j];
			if(xx>=0&&yy>=0&&xx<VAL_MAXCX&&yy<VAL_MAXCX){
				int cell=xx+(yy<<VAL_SHFCX);
				int NMon=MCount[cell];
				if(NMon){
					int ofs1=cell<<SHFCELL;
					word MID;
					for(int i=0;i<NMon;i++){
						MID=GetNMSL(ofs1+i);
						if(MID!=0xFFFF&&nids<2048){
							ids[nids++]=MID;							
						}
					}
				}
			}
		}
	}	
}
OneObject* units_iterator::UnitsInRadius::Next(){
	while(pos<nids){
        OneObject* OB=Group[ids[pos++]];
		if(OB){			
			int dx=OB->RealX/16-_xc;
			int dy=OB->RealY/16-_yc;
			if(dx*dx+dy*dy<_R2)return OB;
		}
	}
	return NULL;
}
void units_iterator::GetUnitsInSquare::Create(int x,int y, int x1, int y1)
{
	if(x>x1)
	{
		_x=x1;
		_x1=x;
	}
	else
	{
		_x=x;
		_x1=x1;
	}
	if(y>y1)
	{
		_y=y1;
		_y1=y;
	}
	else
	{
		_y=y;
		_y1=y1;
	}
	nids=0;
	pos=0;

	int xx=(x>>7)+1;
	int xx1=(x1>>7)+2;

	int yy=(y>>7)+1;
	int yy1=(y1>>7)+2;

	for(int i=xx;i<xx1;i++)
	{
		if(xx>=0&&xx<VAL_MAXCX)
		{
			for(int j=yy;j<yy1;j++)
			{
				if(yy>=0&&yy<VAL_MAXCX)
				{
					int cell=xx+(yy<<VAL_SHFCX);
					int NMon=MCount[cell];
					if(NMon)
					{
						int ofs1=cell<<SHFCELL;
						word MID;
						for(int i=0;i<NMon;i++)
						{
							MID=GetNMSL(ofs1+i);
							if(MID!=0xFFFF&&nids<2048)
							{
								ids[nids++]=MID;							
							}
						}
					}
				}
			}
		}
	}
}
OneObject* units_iterator::GetUnitsInSquare::Next()
{
	while(pos<nids)
	{
		OneObject* OB=Group[ids[pos++]];
		if(OB)
		{			
			int x=OB->RealX/16;
			int y=OB->RealY/16;
			if(x>_x&&x<_x1&&y>_y&&y<_y1)
				return OB;
		}
	}
	return NULL;
}

void units_iterator::GetBuildingsInRadius::Create(int xc,int yc,int R){
	nids=0;
	pos=0;
	int NR=(R>>7)+1;
	int cx0=(xc>>7)+1;
	int cy0=(yc>>7)+1;
	int NN=0;    
	_xc=xc;
	_yc=yc;
	_R2=R*R;
	for(int i=0;i<NR;i++)
	{
		char* xi=Rarr[i].xi;
		char* yi=Rarr[i].yi;
		int N=Rarr[i].N;
		for(int j=0;j<N;j++)
		{
			int xx=cx0+xi[j];
			int yy=cy0+yi[j];
			if(xx>=0&&yy>=0&&xx<VAL_MAXCX&&yy<VAL_MAXCX)
			{
				int cell=xx+(yy<<VAL_SHFCX);
				int NMon=MCount[cell];
				if(NMon)
				{
					int ofs1=cell<<SHFCELL;
					word MID;
					for(int pos=0;(MID=GetOneBld(cell,pos))!=0xFFFF;pos++)
					{				
						if(nids<2048)
						{
							ids[nids++]=MID;							
						}
					}
				}
			}
		}
	}
}
OneObject* units_iterator::GetBuildingsInRadius::Next(){
	while(pos<nids){
		OneObject* OB=Group[ids[pos++]];
		if(OB){			
			int dx=OB->RealX/16-_xc;
			int dy=OB->RealY/16-_yc;
			if(dx*dx+dy*dy<_R2)return OB;
		}
	}
	return NULL;
}

void units_iterator::GetBuildingsInSquare::Create(int x,int y, int x1, int y1)
{
	if(x>x1)
	{
		_x=x1;
		_x1=x;
	}
	else
	{
		_x=x;
		_x1=x1;
	}
	if(y>y1)
	{
		_y=y1;
		_y1=y;
	}
	else
	{
		_y=y;
		_y1=y1;
	}
	nids=0;
	pos=0;

	int xx=(x>>7)+1;
	int xx1=(x1>>7)+2;

	int yy=(y>>7)+1;
	int yy1=(y1>>7)+2;

	for(int i=xx;i<xx1;i++)
	{
		if(xx>=0&&xx<VAL_MAXCX)
		{
			for(int j=yy;j<yy1;j++)
			{
				if(yy>=0&&yy<VAL_MAXCX)
				{
					int cell=xx+(yy<<VAL_SHFCX);
					int NMon=MCount[cell];
					if(NMon)
					{
						int ofs1=cell<<SHFCELL;
						word MID;
						for(int pos=0;(MID=GetOneBld(cell,pos))!=0xFFFF;pos++)
						{				
							if(nids<2048)
							{
								ids[nids++]=MID;							
							}
						}
					}
				}
			}
		}
	}
}
OneObject* units_iterator::GetBuildingsInSquare::Next()
{
	while(pos<nids)
	{
		OneObject* OB=Group[ids[pos++]];
		if(OB)
		{			
			int x=OB->RealX/16;
			int y=OB->RealY/16;
			if(x>_x&&x<_x1&&y>_y&&y<_y1)
				return OB;
		}
	}
	return NULL;
}
void units_iterator::GetUnitsOfBrigade::Create(Brigade* BR)
{
	Br=BR;
	pos=0;
}
OneObject* units_iterator::GetUnitsOfBrigade::Next()
{
	OneObject* rez=NULL;
	while(Br&&pos<Br->NMemb&&rez==NULL)
	{
		if(Br->Memb[pos]!=0xFFFF)
		{
			rez=Group[Br->Memb[pos]];
		}
		pos++;
	}
	return rez;
}
void units_iterator::GetNotCommandUnitsOfBrigade::Create(Brigade* BR)
{
	Br=BR;
	pos=NBPERSONAL;
}
OneObject* units_iterator::GetNotCommandUnitsOfBrigade::Next()
{
	OneObject* rez=NULL;
	while(Br&&pos<Br->NMemb&&rez==NULL)
	{
		if(Br->Memb[pos]!=0xFFFF)
		{
			rez=Group[Br->Memb[pos]];
		}
		pos++;
	}
	return rez;
}
void units_iterator::GetUnitsOfNation::Create(int NI)
{
	id=NatList[NI];
	n=NtNUnits[NI];
	pos=0;
}
OneObject* units_iterator::GetUnitsOfNation::Next()
{
	OneObject* rez=NULL;
	word mid;
	while(pos<n&&rez==NULL)
	{
		mid=id[pos];
		if(mid!=0xFFFF)
		{
			rez=Group[mid];
		}
		pos++;
	}
	return rez;
}
void units_iterator::GetAllUnits::Create()
{
	pos=0;
}
OneObject* units_iterator::GetAllUnits::Next()
{
	OneObject* rez=NULL;
	while(pos<MAXOBJECT&&rez==NULL)
	{
		rez=Group[pos++];
	}
	return rez;
}

void units_iterator::GetSelected::Create(int NI)
{
	n=NSL[NI];
	id=Selm[NI];
	pos=0;
}
OneObject* units_iterator::GetSelected::Next()
{
	OneObject* rez=NULL;
	word mid;
	while(pos<n&&rez==NULL)
	{
		mid=id[pos];
		if(mid!=0xFFFF)
		{
			rez=Group[mid];
		}
		pos++;
	}
	return rez;
}
void units_iterator::GetImSelected::Create(int NI)
{
	n=ImNSL[NI];
	id=ImSelm[NI];
	pos=0;
}
OneObject* units_iterator::GetImSelected::Next()
{
	OneObject* rez=NULL;
	word mid;
	while(pos<n&&rez==NULL)
	{
		mid=id[pos];
		if(mid!=0xFFFF)
		{
			rez=Group[mid];
		}
		pos++;
	}
	return rez;
}



units_iterator::UnitsInRadius itr_UnitsInRadius;
units_iterator::GetUnitsInSquare itr_GetUnitsInSquare;
units_iterator::GetBuildingsInRadius itr_GetBuildingsInRadius;
units_iterator::GetBuildingsInSquare itr_GetBuildingsInSquare;
units_iterator::GetUnitsOfBrigade itr_GetUnitsOfBrigade;
units_iterator::GetNotCommandUnitsOfBrigade itr_GetNotCommandUnitsOfBrigade;
units_iterator::GetUnitsOfNation itr_GetUnitsOfNation;
units_iterator::GetAllUnits itr_GetAllUnits;
units_iterator::GetSelected itr_GetSelected;
units_iterator::GetImSelected itr_GetImSelected;


int u_test(){
	
	itr_UnitsInRadius.Create(rand()/4,rand()/4,600);
	OneObject* OB;
	int N=0;
	while(OB=itr_UnitsInRadius.Next()){
		N++;
	}
	void DrawColoredCircle(int xc,int yc,int R0,int R1,DWORD LineColor1,DWORD LineColor2,DWORD FillColor1,DWORD FillColor2);
	DrawColoredCircle(512,768,0,200,0x00FF0000,0x80FF0000,0x00FF0000,0x80FF0000);
    return N;
}
//
CIMPORT UnitsGroup* GetUnitsGroup(GAMEOBJ* Group);
//
CEXPORT int AddUnlimitedUnitsInZone(byte NI,word NIndex,int x,int y,int r,GAMEOBJ* Dest){
	int A=0,N=0;
	UnitsGroup* UG=GetUnitsGroup(Dest);
	if(UG){
		itr_UnitsInRadius.Create(x,y,r);
		while(OneObject* OB=itr_UnitsInRadius.Next()){
			N++;
			if(OB->NNUM==NI&&OB->NIndex==NIndex&&OB->UnlimitedMotion){
				bool add=true;
				for(int i=0;i<UG->N;i++){
					if(OB->Index==UG->IDS[i]&&OB->Serial==UG->SNS[i]){
						add=false;
						break;
					}
				}
				if(add){
					A++;
					UG->AddNewUnit(OB);
				}
			}
		}
	}
	return A;
};