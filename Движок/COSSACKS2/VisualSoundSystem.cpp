#include "stdheader.h"
#include "VisualSoundSystem.h"
extern CDirSound* CDS;
AllSounds VisualSound;
BaseClass* GetSoundClass(){
	return &VisualSound;
}
bool ProcessSoundClass(ClassEditor* CE,BaseClass* BC,int Options){
	if(Options==3){
		void ReloadSounds();
		ReloadSounds();
	}
	return false;
}
extern int ItemChoose;
bool MMItemChoose(SimpleDialog* SD);
extern CDirSound CDIRSND;
void ReloadSounds(){
	void CloseSoundSystem();
	CloseSoundSystem();
	CDIRSND.~CDirSound();
	CDIRSND.CreateDirSound(hwnd);
	CDS=&CDIRSND;
	LoadSounds("SoundList.txt");
}
void ProcessSoundsEditor(){
	xmlQuote xml;
	ItemChoose=-1;
	//VisualSound.reset_class(&VisualSound);
	//VisualSound.ReadFromFile("Sound\\Sounds.xml");
	ReloadSounds();
	DialogsSystem DSS;
	if(DSS.SafeReadFromFile("Dialogs\\SoundEditor.DialogsSystem.xml")){				
		SimpleDialog* Desk=DSS.Find("OptionsDesk");
		SimpleDialog* OK=DSS.Find("OK");
		SimpleDialog* CANCEL=DSS.Find("CANCEL");
		if(Desk&&OK&&CANCEL){
			int x0,y0,x1,y1;
			DSS.GetDialogsFrame(x0,y0,x1,y1);
			if(x1>x0){
				DSS.x=(RealLx-x1+x0)/2;
				DSS.y=(RealLy-y1+y0)/2;
				OK->OnUserClick=&MMItemChoose;
				OK->UserParam=1;
				CANCEL->OnUserClick=&MMItemChoose;
				CANCEL->UserParam=1;
				ClassEditor CE;
				CE.CreateFromClass(Desk,0,0,Desk->x1-Desk->x,Desk->y1-Desk->y,&VisualSound,3,"EmptyBorder");
				do{
					ProcessMessages();					
					DSS.ProcessDialogs();
					CE.Process();
					DSS.RefreshView();
				}while(ItemChoose==-1);
			}
		}
	}
	VisualSound.WriteToFile("Sound\\Sounds.xml");
	ReloadSounds();
}
OneSound::OneSound(){
	Volume=1.0f;
	Looped=false;
}
short GetVolByFloat01(float Vol){
	float w=1000*log(Vol+0.00000001)/log(0.5);
	if(w<0)w=0;
	if(w>10000)w=10000;
	return short(w);
}
extern char* SoundID[MaxSnd];
extern word SndTable[MaxSnd][16];
extern byte SnDanger[MaxSnd];
extern int SndPause[MaxSnd];
extern int SndPauseVar[MaxSnd];
extern word NSnd[MaxSnd];
extern word NSounds;
extern int SndLastPlayTime[MaxSnd];
bool AllSounds::Load(xmlQuote& xml,void* ClassPtr,ErrorPager* Error,void* Extra){
	BaseClass::Load(xml,ClassPtr,Error,Extra);
	for(int i=0;i<SoundsList.GetAmount();i++){
		SoundsGroup* SG=SoundsList[i];
		if(SG&&SG->GroupName.str){
			if(NSounds<MaxSnd){				
				SoundID[NSounds]=znew(char,strlen(SG->GroupName.str)+1);
				strcpy(SoundID[NSounds],SG->GroupName.str);
				int csn=0;
				if(SG->Usage==2){//UseLikeGroupSound
					CDS->SetGroupOptions(SG->GroupSoundID,SG->GroupSoundStartFrequency,SG->GroupSoundEndFrequency);
				}
				for(int j=0;j<SG->List.GetAmount();j++){
					OneSound* OS=SG->List[j];
					char* Name=OS->WaveFile.str;
					if(Name){
						int idx=-1;
						OS->SoundID=-1;
						for(int q=0;q<MAXSND;q++){
							if(CDS->m_SoundNames[q]&&!strcmp(CDS->m_SoundNames[q],Name)){
								idx=q;
								OS->SoundID=CDS->DuplicateSoundBuffer(idx);								
							}
						}
						if(idx==-1){
							CWave CW(Name);
							if(CW.WaveOK()){
								OS->SoundID=CDS->CreateSoundBuffer(&CW);
								CDS->CopyWaveToBuffer(&CW,OS->SoundID);
							}else{
								PrintError("Unable to load sound: %s",Name);
							}
						}
						if(SG->Usage==1){//UseGroupSound
							CDS->SetSoundCategory(OS->SoundID,SG->GroupSoundID,0,j);
						}
						if(SG->Usage==2){//UseLikeGroupSound
							CDS->SetSoundCategory(OS->SoundID,SG->GroupSoundID,SG->GroupSoundID,j);
							CDS->AddGroupSound(SG->GroupSoundID,OS->SoundID);
							CDS->StartGroupFreq[SG->GroupSoundID]=SG->GroupSoundStartFrequency;
							CDS->FinalGroupFreq[SG->GroupSoundID]=SG->GroupSoundEndFrequency;
						}
						if(OS->SoundID!=-1&&csn<16){
							CDS->Volume[OS->SoundID]=GetVolByFloat01(OS->Volume)+GetVolByFloat01(SG->Volume);
							CDS->LoopedSound[OS->SoundID]=OS->Looped;
							CDS->RiseUpTime[OS->SoundID]=int(OS->StartSharpness*65536.0);
							CDS->FallDnTime[OS->SoundID]=int(OS->EndSharpness*65536.0);
                            SndTable[NSounds][csn]=OS->SoundID;
							csn++;
							NSnd[NSounds]=csn;
						}
						SnDanger[NSounds]=0;
						if(SG->DangerousSound)SnDanger[NSounds]|=1;
						if(SG->DontPlayInDangerousSituation)SnDanger[NSounds]|=2;
						SndPause[NSounds]=SG->MinPause;
						SndPauseVar[NSounds]=SG->PauseVariation;
						SndLastPlayTime[NSounds]=GetTickCount()-1000;
					}
				}
				NSounds++;
			}
		}
	}
	return true;
}
const char* VSLIST::GetEditableElementView(int Index,void* DataPtr,const char* LocalName,DWORD Option){
	if(Index<GetAmount()){	
		char* s=GetGlobalBuffer();		
		strcpy(s,"???noname???");
		const char* s0=(*this)[Index]->GroupName.str;
		if(s0&&s0[0]){
            strcpy(s,s0);
			if((*this)[Index]->Usage==2){
                sprintf(s+strlen(s)," used like group sound - ID=%d, range %d..%d",(*this)[Index]->GroupSoundID,(*this)[Index]->GroupSoundStartFrequency,(*this)[Index]->GroupSoundEndFrequency);
			}
			if((*this)[Index]->Usage==1){
				sprintf(s+strlen(s)," uses group sound - ID=%d",(*this)[Index]->GroupSoundID);
			}
		}		
		return s;
	}else{
		return ClonesArray<SoundsGroup>::GetEditableElementView(Index,DataPtr,LocalName,Option);
	}
}
const char* WSLIST::GetEditableElementView(int Index,void* DataPtr,const char* LocalName,DWORD Option){
	if(Index<GetAmount()){	
		const char* s=(*this)[Index]->WaveFile.str;
		if(s&&s[0]){
			const char* s1=s;
			const char* s2=s1;
			while(s1){
				s1=strstr(s1,"\\");
                if(s1)s1++;
				if(s1)s2=s1;
			}
			return s2;
		}return "???noname???";		
	}else{
		return ClonesArray<OneSound>::GetEditableElementView(Index,DataPtr,LocalName,Option);
	}
}