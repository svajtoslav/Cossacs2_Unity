#include "stdheader.h"
#include "VisualSoundSystem.h"
int FieldDelay=0;
extern int WarSound;
extern int WorkSound;
extern int OrderSound;
extern int MidiSound;
static bool SoundOK;
CDirSound* CDS;

char* SoundID[MaxSnd];
word SndTable[MaxSnd][16];
byte SnDanger[MaxSnd];
int SndPause[MaxSnd];
int SndPauseVar[MaxSnd];
int SndLastPlayTime[MaxSnd];

word NSnd[MaxSnd];
word NSounds;
int NoMineSound=-1;
void Errs(LPCSTR s)
{
	MessageBox(hwnd,s,"Sound loading failed...",MB_ICONWARNING|MB_OK);
	assert(false);
};
extern short randoma[8192];
int srpos;
int srando(){
	srpos++;
	srpos&=8191;
	return randoma[srpos];
};
bool IsSound;
void normstr(char* str);
void NLine(GFILE* f);
void LoadSounds(char* fn){
	char sss[128];
	char idn[128];
	int srpos=0;
	int nsn,z;
	NSounds=0;
	SoundOK=CDS->DirectSoundOK()!=0;
	GFILE* f1=Gopen(fn,"rt");
	AFile(fn);
	char ccpr[128];
	ccpr[0]=0;
	if(f1){
	  do{
uuux:	int Extent=0;
		z=Gscanf(f1,"%s",idn);
		if(idn[0]=='/'){
			NLine(f1);
			goto uuux;
		};
		if(idn[0]=='#')Extent=1;
		nsn=0;
		z=Gscanf(f1,"%d",&nsn);
		NSnd[NSounds]=nsn;
		SoundID[NSounds]=znew(char,strlen(idn)+1-Extent);
		strcpy(SoundID[NSounds],idn+Extent);
		int CURCTG=0;
		int INCTG=0;
		int MINSN=0;
		int MAXSN=0;
		if(z){
			NLine(f1);
			SnDanger[NSounds]=0;
			for(int i=0;i<nsn;i++){
				int Vol=0;
uuu:			if(Extent){
					z=Gscanf(f1,"%s%d",sss,&Vol);
				}else{
					z=Gscanf(f1,"%s",sss);
				};
				if(sss[0]=='/'){
					NLine(f1);
					goto uuu;
				};
				if(!strcmp(sss,"DANGER")){
					SnDanger[NSounds]=1;
					goto uuu;
				};
				if(!strcmp(sss,"INGROUP")){
					int grid;
					z=Gscanf(f1,"%d",&grid);
					if(z==1&&grid>0&&grid<256){
						CURCTG=grid;
					};
					goto uuu;
				};
				if(!strcmp(sss,"FORGROUP")){
					int grid;
					z=Gscanf(f1,"%d%d%d",&grid,&MINSN,&MAXSN);
					if(z==3&&grid>0&&grid<256){
						INCTG=grid;
						CDS->SetGroupOptions(INCTG,MINSN,MAXSN);
					};
					goto uuu;
				};
				normstr(sss);
				if(SoundOK){
					if(!strcmp(ccpr,sss)){
						SndTable[NSounds][i]=CDS->DuplicateSoundBuffer(CDS->m_currentBufferNum);
						CDS->SetLastVolume(Vol);
						CDS->SetSoundCategory(CDS->m_currentBufferNum,CURCTG,INCTG,i);
						if(INCTG){
							CDS->AddGroupSound(INCTG,CDS->m_currentBufferNum);
						};
					}else{
						CWave CW(sss);
						AText(sss);
						int sdid;
						if(CW.WaveOK()){
							sdid=CDS->CreateSoundBuffer(&CW);
							CDS->SetSoundCategory(sdid,CURCTG,INCTG,0);
							if(INCTG)CDS->AddGroupSound(INCTG,sdid);
							CDS->SetLastVolume(Vol);
							if(CDS->DirectSoundOK()){
								CDS->CopyWaveToBuffer(&CW,sdid);
							}else{
								sprintf(idn,"Could not create sound buffer : %s",sss);
								Errs(idn);
							};
							SndTable[NSounds][i]=sdid;
							strcpy(ccpr,sss);
						}else{
							//sprintf(idn,"Could not load sound : %s",sss);
							//Errs(idn);
							nsn--;
							NSnd[NSounds]--;
							i--;
						};
					};
				};
			};
		};
		NSounds++;
	  }while (z);
	}else{
		//sprintf(sss,"Could not open sounds list : %s",fn);
		//Errs(sss);
	};
	//NSounds=0;
	Gclose(f1);
	//if(!CDS->DirectSoundOK()){
	//	NSounds=0;
	//};
	NoMineSound=GetSound("NOFREEMINES");
	VisualSound.reset_class(&VisualSound);
	VisualSound.ReadFromFile("Sound\\Sounds.xml");
};
extern bool NOPAUSE;
CEXPORT void PlayEffect(int n,int pan,int vol){	
	if(!(SoundOK&&NOPAUSE))return;
	if(FieldDelay>0&&(SnDanger[n]&2))return;
	vol-=(100-GSets.SVOpt.SoundVolume)*40;
	if(n<NSounds){
		if(SnDanger[n]&1)FieldDelay=400;
		if(NSnd[n]>0&&NSnd[n]<16){
			int maxsnd=NSnd[n];
			int u=maxsnd;
			int nnn=(srando()*maxsnd)>>15;
			bool sndmade=true;
			do{
				int sid=SndTable[n][nnn];
				bool poss=CDS->IsPlaying(sid);
				if(!poss){
					CDS->SetVolume(sid,vol-CDS->Volume[sid]/*+CDS->Volume[sid]*/);
					CDS->SetPan(sid,pan);
					CDS->PlaySound(sid,0);
					sndmade=false;
				}else{
					u--;
					nnn++;
					if(nnn>=maxsnd)nnn=0;
				};
			}while(sndmade&&u);
			/*
			if(sndmade&&srando()<200){
				int nnn=(srando()*maxsnd)>>15;
				CDS->SetVolume(SndTable[n][nnn],vol);
				CDS->SetPan(SndTable[n][nnn],pan);
				CDS->PlaySound(SndTable[n][nnn]);
			};
			*/
		};
	};
};
void PlayEffectDialogs(int n){
	bool np=NOPAUSE;
	NOPAUSE=true;
	PlayEffect(n,0,0);
	NOPAUSE=np;	
}
extern int LastFlipTime;
void PlayCoorEffect(int n,int x,int y,int pan,int vol){
	if(!SoundOK)return;
	if(FieldDelay>0&&(SnDanger[n]&2))return;
	static int LastReqTime[4096];
	static bool init=true;
	if(init){
		int T=GetTickCount();
		for(int i=0;i<4096;i++){
			LastReqTime[i]=T-1000;
		}
		init=false;
	}
	if(LastFlipTime-LastReqTime[n]<rand()%150){
		int sid=SndTable[n][0];
		CDS->MarkSoundLikePlaying(sid,x);
		return;
	}
	LastReqTime[n]=LastFlipTime;
	if(LastFlipTime-SndLastPlayTime[n]<0)return;
	SndLastPlayTime[n]=LastFlipTime+SndPause[n]*1000-100;
	if(SndPauseVar[n]>0){
		SndLastPlayTime[n]+=rand()%(SndPauseVar[n]*1000);
	}
	vol-=(100-GSets.SVOpt.SoundVolume)*40;
	if(n<NSounds){
		if(SnDanger[n]&1)FieldDelay=800*256;
		if(NSnd[n]>0&&NSnd[n]<16){
			int maxsnd=NSnd[n];
			int u=maxsnd;
			int nnn=(srando()*maxsnd)>>15;
			bool sndmade=true;
			int lastsid=-1;
			do{
				int sid=SndTable[n][nnn];
				bool poss=CDS->IsPlaying(sid);
				lastsid=sid;
				if(!poss){
					CDS->SetVolume(sid,vol-CDS->Volume[sid]/*+CDS->Volume[sid]*/);
					CDS->SetPan(sid,pan);
					CDS->PlayCoorSound(sid,x,y);
					sndmade=false;
				}else{
					u--;
					nnn++;
					if(nnn>=maxsnd)nnn=0;
				};
			}while(sndmade&&u);
			if(sndmade){
				CDS->MarkSoundLikePlaying(lastsid,x);
			};
			/*
			if(sndmade&&srando()<200){
				int nnn=(srando()*maxsnd)>>15;
				CDS->SetVolume(SndTable[n][nnn],vol);
				CDS->SetPan(SndTable[n][nnn],pan);
				CDS->PlaySound(SndTable[n][nnn]);
			};
			*/
		};
	};
};
void PlaySingleEffect(int n,int pan,int vol){
	if(!SoundOK)return;
	vol-=(100-GSets.SVOpt.SoundVolume)*40;
	if(n<NSounds){
		if(SnDanger[n])FieldDelay=800*256;
		if(NSnd[n]>0&&NSnd[n]<16){
			int maxsnd=NSnd[n];
			for(int i=0;i<maxsnd;i++){
				int sid=SndTable[n][i];
				if(CDS->IsPlaying(sid))return;
			};

			int u=maxsnd;
			int nnn=(srando()*maxsnd)>>15;
			bool sndmade=true;
			do{
				int sid=SndTable[n][nnn];
				bool poss=CDS->IsPlaying(sid);
				if(!poss){
					CDS->SetVolume(sid,vol-CDS->Volume[sid]);
					CDS->SetPan(sid,pan);
					CDS->PlaySound(sid,0);
					sndmade=false;
				}else{
					u--;
					nnn++;
					if(nnn>=maxsnd)nnn=0;
				};
			}while(sndmade&&u);
			/*
			if(sndmade&&srando()<200){
				int nnn=(srando()*maxsnd)>>15;
				CDS->SetVolume(SndTable[n][nnn],vol);
				CDS->SetPan(SndTable[n][nnn],pan);
				CDS->PlaySound(SndTable[n][nnn]);
			};
			*/
		};
	};
};

int SMinX,SMaxX,LoMinX,LoMaxX,SMinY,SMaxY,LoMinY,LoMaxY,CenterY;
int CenterX;
void PrepareSound(){

	if(FieldDelay>0)FieldDelay-=GameSpeed;
	if(FieldDelay<0)FieldDelay=0;

	SMinX=mapx<<5;
	SMaxX=(mapx+smaplx)<<5;
	CenterX=(SMinX+SMaxX)>>1;
	LoMinX=SMinX-1024;
	LoMaxX=SMaxX+1024;

	SMinY=mapy<<4;
	SMaxY=(mapy+smaply)<<5;
	CenterY=(SMinY+SMaxY)>>1;
	LoMinY=SMinY-512;
	LoMaxY=SMaxY+512;
};
word GetFog(int x,int y);
extern int FogMode;
void AddEffectV(int x,int y,int vx,int id){
	if(!SoundOK)return;
	if(id<0)return;
	if(x<LoMinX||x>LoMaxX||y<LoMinY||y>LoMaxY)return;
	//fog checking;
	if(FogMode&&(!NATIONS[MyNation].Vision)&&GetFog(x,y)<900)return;
	PROF;
	int mx0=mapx-8;
	int my0=mapy-8;
	int mx1=mapx+smaplx+16;
	int my1=mapy+smaply+16;
	//int maxp=div(8000,smaplx).quot;
	int pan=(x-CenterX)<<1;
	if(pan<-4000)pan=-4000;
	if(pan>4000)pan=4000;
	//int pan=-9999;
	if(x>=SMinX&&y>=SMinY&&x<SMaxX&&y<SMaxY){
		PlayCoorEffect(id,x,vx,pan,0);	
	}else{
		PlayCoorEffect(id,x,vx,pan,-800);
	};
	ENDPROF;
};
void AddEffect(int x,int y,int id){
	AddEffectV(x,y,0,id);
};
void AddUnlimitedEffect(int x,int y,int id){
	if(!SoundOK)return;
	if(id<0)return;
	//fog checking;
	//if(GetFog(x,y<<1)<900&&FogMode)return;
	
	int mx0=mapx-8;
	int my0=mapy-8;
	int mx1=mapx+smaplx+16;
	int my1=mapy+smaply+16;
	//int maxp=div(8000,smaplx).quot;
	int pan=(x-CenterX)<<1;
	if(pan<-4000)pan=-4000;
	if(pan>4000)pan=4000;
	//int pan=-9999;
	PlayEffect(id,pan,0);	
};
void AddSingleEffect(int x,int y,int id){
	if(!SoundOK)return;
	if(id<0)return;
	if(x<LoMinX||x>LoMaxX||y<LoMinY||y>LoMaxY)return;
	//fog checking;
	if(GetFog(x,y<<1)<900&&FogMode)return;
	
	int mx0=mapx-8;
	int my0=mapy-8;
	int mx1=mapx+smaplx+16;
	int my1=mapy+smaply+16;
	//int maxp=div(8000,smaplx).quot;
	int pan=(x-CenterX)<<1;
	if(pan<-4000)pan=-4000;
	if(pan>4000)pan=4000;
	//int pan=-9999;
	if(x>=SMinX&&y>=SMinY&&x<SMaxX&&y<SMaxY){
		PlaySingleEffect(id,pan,0);	
	}else{
		PlaySingleEffect(id,pan,-800);
	};
};
void AddWarEffect(int x,int y,int id){
	if(!SoundOK)return;
	if(GetV_fmap(x,y)<2000)return;
	if(id<0)return;
	int mx0=mapx-8;
	int my0=mapy-8;
	int mx1=mapx+smaplx+16;
	int my1=mapy+smaply+16;
	int maxp=div(8000,smaplx).quot;
	int pan=(x-(smaplx>>1)-mapx)*maxp;
	if(pan<-4000)pan=-4000;
	if(pan>4000)pan=4000;
	//int pan=-9999;
	if(x>=mapx&&y>=mapy&&x<mapx+smaplx&&y<mapy+smaply){
		PlayEffect(id,pan,GSets.SVOpt.SoundVolume);	
	}else{
		if(x>mx0&&y>my0&&x<mx1&&y<my1){
			PlayEffect(id,pan,GSets.SVOpt.SoundVolume-400);
		};
	};
};
void AddWorkEffect(int x,int y,int id){
	if(!SoundOK)return;
	if(GetV_fmap(x,y)<2000)return;
	if(id<0)return;
	int mx0=mapx-8;
	int my0=mapy-8;
	int mx1=mapx+smaplx+16;
	int my1=mapy+smaply+16;
	int maxp=div(8000,smaplx).quot;
	int pan=(x-(smaplx>>1)-mapx)*maxp;
	if(pan<-4000)pan=-4000;
	if(pan>4000)pan=4000;
	//int pan=-9999;
	if(x>=mapx&&y>=mapy&&x<mapx+smaplx&&y<mapy+smaply){
		PlayEffect(id,pan,WorkSound-200);	
	}else{
		if(x>mx0&&y>my0&&x<mx1&&y<my1){
			PlayEffect(id,pan,WorkSound-600);
		};
	};
};
void AddOrderEffect(int x,int y,int id){
	if(!SoundOK)return;
	if(GetV_fmap(x>>2,y>>2)<2000)return;
	if(id<0)return;
	int mx0=mapx-8;
	int my0=mapy-8;
	int mx1=mapx+smaplx+16;
	int my1=mapy+smaply+16;
	int maxp=div(8000,smaplx).quot;
	int pan=(x-(smaplx>>1)-mapx)*maxp;
	if(pan<-4000)pan=-4000;
	if(pan>4000)pan=4000;
	//int pan=-9999;
	if(x>=mapx&&y>=mapy&&x<mapx+smaplx&&y<mapy+smaply){
		PlayEffect(id,pan,OrderSound);	
	}else{
		if(x>mx0&&y>my0&&x<mx1&&y<my1){
			PlayEffect(id,pan,OrderSound-400);
		};
	};
};
void AddTEffect(int x,int y,int id){
	if(!SoundOK)return;
	int xx=x;
	if(xx<mapx)xx=mapx+1;
	if(xx>=mapx+smaplx)xx=mapx+smaplx-1;
	AddOrderEffect(xx,mapy+1,id);
};
void CloseSoundSystem(){
	for(int i=0;i<NSounds;i++){
		free(SoundID[i]);
	};
};
DIALOGS_API void PlaySound(char* Name,int x, int y)
{
	if(Name)
		AddEffect(x,y,GetSound(Name));
}
DIALOGS_API void PlaySound(char* Name)
{
	if(Name)
		AddUnlimitedEffect(0,0,GetSound(Name));
}
DLLEXPORT int GetMyNation();
void OfficerDieSound(OneObject* OB)
{
	if(OB&&OB->NNUM==GetMyNation()&&OB->newMons->Officer)
	{
		PlaySound("KILL_OFICER",OB->RealX>>4,OB->RealY>>4);
	}
}
void BuildingDieSound(OneObject* OB)
{
	if(OB&&OB->NNUM==GetMyNation()&&OB->NewBuilding)
	{
		PlaySound("ZDANIE_DIE",OB->RealX>>4,OB->RealY>>4);
	}
}
void DieSounds(OneObject* OB)
{
	OfficerDieSound(OB);
	BuildingDieSound(OB);
}
