#include "stdheader.h"
#include "HashTop.h"
#include "LoadSave.h"

//MediaTop GTOP[2];

Radio Rarr[RRad];
void CreateRadio(){
	for(int ix=-RRad;ix<=RRad;ix++){
		for(int iy=-RRad;iy<=RRad;iy++){
			int r=int(sqrt(ix*ix+iy*iy));
			if(r<RRad)Rarr[r].N++;
		};
	};
	for(int i=0;i<RRad;i++){
		if(Rarr[i].N){
			Rarr[i].xi=znew(char,Rarr[i].N);
			Rarr[i].yi=znew(char,Rarr[i].N);
			Rarr[i].N=0;
		};
	};
	for(ix=-RRad;ix<=RRad;ix++){
		for(int iy=-RRad;iy<=RRad;iy++){
			int r=int(sqrt(ix*ix+iy*iy));
			if(r<RRad){
				int N=Rarr[r].N;
				Rarr[r].xi[N]=ix;
				Rarr[r].yi[N]=iy;
				Rarr[r].N++;
			};
		};
	};
	Rarr[1].xi[0]=-1;
	Rarr[1].yi[0]=0;
	Rarr[1].xi[1]=1;
	Rarr[1].yi[1]=0;
	Rarr[1].xi[2]=0;
	Rarr[1].yi[2]=-1;
	Rarr[1].xi[3]=0;
	Rarr[1].yi[3]=1;

	Rarr[1].xi[4]=-1;
	Rarr[1].yi[4]= 1;
	Rarr[1].xi[5]=-1;
	Rarr[1].yi[5]=-1;
	Rarr[1].xi[6]= 1;
	Rarr[1].yi[6]=-1;
	Rarr[1].xi[7]= 1;
	Rarr[1].yi[7]= 1;

}
void ClearRadio(){
	for(int i=0;i<RRad;i++){
		free(Rarr[i].xi);
		free(Rarr[i].yi);
	};
};
void CreateLinkInfo();
int GetLinkScale(int N1,int N2,int t);

extern int MAXSPR;
void EraseAreas();
bool GetTCStatus(int x,int y){
	int xxx=x<<2;
	int yyy=y<<2;
	if(!MFIELDS->CheckBar(xxx,yyy,4,4))return true;
	return false;
};
void CheckBright();
void ResearchIslands();
void CreateCostPlaces();
void StopDynamicalTopology();
void PreCreateTopLinks(int);
void SetClearBuildigsLock(bool Set);
void CreateAreas(){
	SetClearBuildigsLock(0);
	for(int i=0;i<NMFIELDS;i++){
		HashTable[i].CreateAreas();
	}
	SetClearBuildigsLock(1);
	return;
};
void TestCreateAreas(){	
	for(int i=0;i<NMFIELDS;i++){
		HashTable[i].ReCreateAreas(0,0,10000,10000);
	}	
	return;
};

void ClearLinkInfo(){
	
};
int OneIteration(){
	return 0;
};
void CreateRoadsNet();
extern bool MiniMade;
void InitAreas(){
};
void EraseAreas(){
	for(int i=0;i<NMFIELDS;i++){
		HashTable[i].EraseAreas();
	}	
};

bool WasOnlyOpen;
//procesing variables
void ResearchIslands();
void ProcessDynamicalTopology(){	
};

void StopDynamicalTopology(){	
};

//------------------Roads tecnology------------------
// -------- Topology extern function --------------

#ifdef HASH_TOP //------------- HASH_TOP --------------------

word GetLinksDist(int Ofs, byte TopType,byte NI){
	HashTop* HT=HashTable[TopType].GetHashTop(Ofs,NI);
	if(HT){
		return HT->LD;
	}	
	return 0xFFFF;
}

word GetMotionLinks(int Ofs, byte TopType,byte NI){
	HashTop* HT=HashTable[TopType].GetHashTop(Ofs,NI);
	if(HT){
		int ML=HT->ML;
		if(ML==8191)ML=0xFFFF;
		return ML;
	}
	return 0xFFFF;
}

word GetTopRef(int Ofs, byte TopType){
	int mo=TopLx<<TopSH;
	if(TopType<NMFIELDS&&Ofs>=0&&Ofs<mo) return HashTable[TopType].TopRef[Ofs];
	else return 0xFFFF;
}

Area* GetTopMap(int Ofs, byte TopType){
	if(TopType<NMFIELDS) return HashTable[TopType].TopMap+Ofs;
	return NULL;
}

int GetNAreas(byte TopType){
	if(TopType<NMFIELDS) return HashTable[TopType].NAreas;
	return 0;
}
#endif //------------- HASH_TOP --------------------


// --- Save-Load *.sav ---
extern bool NeedProcessTop;
extern bool WasOnlyOpen;


void LS_SaveTopology(SaveBuf* SB){
	/*for(int i=0;i<NMFIELDS;i++){
		int N=HashTable[i].NAreas;
		xBlockWrite(SB,&N,4);
		HashTopTable* HT=HashTable+i;
		for(int j=0;j<N;j++){
			xBlockWrite(SB,&HT->TopMap[j].x,4);
			xBlockWrite(SB,&HT->TopMap[j].y,4);
			int nl=HT->TopMap[j].NLinks;
			xBlockWrite(SB,&nl,4);			
			xBlockWrite(SB,&HT->Link,nl*sizeof OneLinkInfo);			
		}
	}*/
};
void CreateRoadsNet();
void ClearLinkInfo();
void LS_LoadTopology(SaveBuf* SB){
	/*ClearTopology();
	for(int i=0;i<NMFIELDS;i++){
		int N;
		xBlockRead(SB,&N,4);

		int N=HashTable[i].NAreas;
		xBlockWrite(SB,&N,4);
		HashTopTable* HT=HashTable+i;
		for(int j=0;j<N;j++){
			xBlockWrite(SB,&HT->TopMap[j].x,4);
			xBlockWrite(SB,&HT->TopMap[j].y,4);
			int nl=HT->TopMap[j].NLinks;
			xBlockWrite(SB,&nl,4);			
			xBlockWrite(SB,&HT->Link,nl*sizeof OneLinkInfo);			
		}
	}*/
};

// --- Save-Load *.m3d ---
void CreateTotalLocking();
void EraseAreas();
void SaveTopology(ResFile f1){
	return;
};
void CreateRoadsNet();
void SaveWTopology(ResFile f1){
	return;
};
void LoadTopology1(ResFile f1){
	return;
};
void ResearchIslands();
void LoadWTopology1(ResFile f1){
	return;
};

// --- Init ---
void ClearTopology(){
	
};
void SetupTopology(){
	for(int i=0;i<NMFIELDS;i++){
		HashTable[i].SetUp(i);
	}
};

void FreeTopology(){
	for(int i=0;i<NMFIELDS;i++){
		HashTable[i].Free();
	}
};
