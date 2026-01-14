#include "stdheader.h"
#include "Extensions.h"
#include "FileDialog.h"
class NewMonster;
int NextRealChar(const char* str){
	int ch=0;
	int L=strlen(str);
	while(str[ch]>=0 && str[ch]<=32){
		ch++;
		if(ch>=L) return 0;
	}
	return ch;
}
int NextLine(const char* str){
	int ch=0;
	int L=strlen(str);
	while(str[ch]!=10){
		ch++;
		if(ch>=L) return 0;
	}
	ch++;
	return ch;
}
int GetWord(const char* str,xmlQuote& xml,char* name){
	int ch=0;
	char word[128];
	while(str[ch]>32 || str[ch]<0){
		word[ch]=str[ch];
		ch++;
	}
	if(ch){
		word[ch]=0;
		xml.AddSubQuote(name,word);
	}
	return ch;
}
const char* GetWord(char* str,int& L){
	int ch=0;
	char word[128];
	int L1=NextRealChar(str);
	str+=L1;
	while(str[0]>32 || str[ch]<0){
		word[ch]=str[0];
		str++;
		ch++;
	}
	word[ch]=0;
	L+=ch+L1;
	return word;
}
void ConvertToLow(char* str){
	while(str[0]){
		if(str[0]>='A' && str[0]<='Z'){
			str[0]-='A'-'a';
		}
		str++;
	}
}
class MDCommand:public BaseClass{
public:
	virtual void Initialize(NewMonster* NM){};
	SAVE(MDCommand);
	ENDSAVE;
};
class MDCommandsList:public ClassArray<MDCommand>{
public:
};
/////////////////////////////////////////////////////////////
short NAStartDx=0;
short NAStartDy=0;
byte NAParts=1;
byte NAPartSize=96;
int MaxRLC=-1;
word RLCRef[128];
short RLCdx[128];
short RLCdy[128];
extern short TCos[257];
extern char* SoundID[MaxSnd];
extern Weapon* WPLIST[1024];
extern byte WeaponFlags[32];
class MonsterAbility;
class NewAnimation;
int GetIconByName(char* Name);
NewAnimation* GetNewAnimationByName(char* Name);
int GetResID(char* gy);
int GetResByName(char* gy);
void UpConv(char* str);
int GetWeaponIndex(char* str);
int GetWeaponType(char* Name);
int GetMatherialType(char* str);
void SetDefaultAnmTiring(NewAnimation* NA,char* Name);
///////////////////// Animation //////////////////////////////////////////////////
class ANIMATION1:public BaseClass{
public:
	int NFrames;
	float Scale;
	_str mesh;
	_str anim;
	float AddDir;
	float StartAnmTime;
	float FinalAnmTime;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=NFrames;
		Dest+=" ";
		Dest+=Scale;
		Dest+=" ";
		Dest+=mesh;
		Dest+=" ";
		Dest+=anim;
		Dest+=" ";
		Dest+=AddDir;
		Dest+=" ";
		Dest+=StartAnmTime;
		Dest+=" ";
		Dest+=FinalAnmTime;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(ANIMATION1);
	REG_MEMBER(_int,NFrames);
	REG_MEMBER(_float,Scale);
	REG_AUTO(mesh);
	REG_AUTO(anim);
	REG_MEMBER(_float,AddDir);
	REG_MEMBER(_float,StartAnmTime);
	REG_MEMBER(_float,FinalAnmTime);
	ENDSAVE;
};
class MDANIMATION1:public MDCommand{
public:
	_str animation;
	ClonesArray<ANIMATION1> animation1;
	void Initialize(NewMonster* NM){
		char cc[64];
		sprintf(cc,"#%s",animation.str+1);
		NewAnimation* NANM=NM->CreateAnimation(cc);
		if(NANM){//*ANIMID Nparts    NFrames1 Scale1 Model1 Animation1 AddDir1 StartTime1 EndTime1  ...
			NANM->Enabled=true;
			NANM->AnimationType=1;
            NANM->NFrames=0;
			int N=animation1.GetAmount();
			for(int j=0;j<N;j++){
				AnimFrame3D* AF3=new AnimFrame3D;
				AF3->Model=IMM->GetModelID(animation1[j]->mesh.str);
				AF3->Scale=animation1[j]->Scale;
				AF3->AddDir=animation1[j]->AddDir;
				AF3->StartAnmTime=animation1[j]->StartAnmTime;
				AF3->FinalAnmTime=animation1[j]->FinalAnmTime;
				AF3->NFrames=animation1[j]->NFrames;
				NANM->NFrames+=animation1[j]->NFrames;
				if(strcmp(animation1[j]->anim.str,"none"))
					AF3->Animation=IMM->GetModelID(animation1[j]->anim.str);
				else 
					AF3->Animation=-1;
                NANM->AnimSet3D.Add(AF3);
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=animation;
		Dest+="{CW} ";
		int N=animation1.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>3)N1=3;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=animation1[i]->NFrames;
			Dest+=" ";
			Dest+=animation1[i]->Scale;
			Dest+=" ";
			Dest+=animation1[i]->mesh;
			Dest+=" ";
			Dest+=animation1[i]->anim;
			Dest+=" ";
			Dest+=animation1[i]->AddDir;
			Dest+=" ";
			Dest+=animation1[i]->StartAnmTime;
			Dest+=" ";
			Dest+=animation1[i]->FinalAnmTime;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDANIMATION1);
	REG_ENUM(_strindex,animation,ALL_ANIMATIONS);	
	REG_CLASS(ANIMATION1);
	REG_AUTO(animation1);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDANIMATION2:public MDCommand{
public:
	_str animation;
	int NFrames;
	float Scale;
	_str mesh;
	_str anim;
	void Initialize(NewMonster* NM){
		char cc[64];
		sprintf(cc,"#%s",animation.str+1);
		NewAnimation* NANM=NM->CreateAnimation(cc);
		if(NANM){
			NANM->Enabled=true;
			NANM->ModelID=IMM->GetModelID(mesh.str);
			NANM->AnimationType=true;
			NANM->Scale=Scale;
			NANM->NFrames=abs(NFrames);
			NANM->Inverse=NFrames<0;
			if(strcmp(anim.str,"none"))
				NANM->AnimationID=IMM->GetModelID(anim.str);
			else 
				NANM->AnimationID=-1;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=animation;
		Dest+="{CW} ";
		Dest+=NFrames;
		Dest+=" ";
		Dest+=Scale;
		Dest+=" ";
		Dest+=mesh;
		Dest+=" ";
		Dest+=anim;
		Dest+="{C} ";
		return Dest.str;
	}
	SAVE(MDANIMATION2);
	REG_ENUM(_strindex,animation,DIES_ANIMATIONS);	
	REG_MEMBER(_int,NFrames);
	REG_MEMBER(_float,Scale);
	REG_AUTO(mesh);
	REG_AUTO(anim);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class ANIMATION3:public BaseClass{
public:
	int number;
	int SpriteID;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=SpriteID;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(ANIMATION3);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,SpriteID);
	ENDSAVE;
};
class MDANIMATION3:public MDCommand{
public:
	_str animation;
	int Rotations;
	ClonesArray<ANIMATION3> animation3;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->CreateAnimation(animation.str);
		if(NANM){
			SetDefaultAnmTiring(NANM,animation.str);
			int N=animation3.GetAmount();
			NANM->StartDx=NAStartDx;
			NANM->StartDy=NAStartDy;
			NANM->Parts=NAParts;
			NANM->PartSize=NAPartSize;
			NANM->Enabled=true;
			NANM->NFrames=N;
			NANM->Rotations=Rotations;
			int nrot=Rotations;
			NANM->ActiveFrame=0;
			if(NANM->ActivePtX)free(NANM->ActivePtX);
			if(NANM->ActivePtY)free(NANM->ActivePtY);
			NANM->ActivePtX=znew(short,Rotations+Rotations+Rotations);
			NANM->ActivePtY=znew(short,Rotations+Rotations+Rotations);
			NANM->HotFrame=0;
			NANM->SoundID=-1;
			NANM->LineInfo=NULL;
			for(int i=0;i<Rotations+Rotations+Rotations;i++){
				NANM->ActivePtX[i]=0;
				NANM->ActivePtY[i]=0;
			}
			NANM->Frames.Clear();
			for(i=0;i<N;i++)
				NANM->Frames.Add(new NewFrame());
			for(i=0;i<N;i++){
				if(animation3[i]->number>MaxRLC)
					continue;
				NewFrame* NF=NANM->Frames[i];
				NF->FileID=RLCRef[animation3[i]->number];
				if((animation3[i]->SpriteID+1)*nrot>GPS.GPNFrames(RLCRef[animation3[i]->number]))
					continue;
				NF->SpriteID=animation3[i]->SpriteID;							
				if(NM->Building){
					NF->dx=NM->PicDx;
					NF->dy=NM->PicDy;
				}else{
					NF->dx=RLCdx[animation3[i]->number];
					NF->dy=RLCdy[animation3[i]->number];
				}
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=animation;
		Dest+="{CW} ";
		Dest+=Rotations;
		Dest+=" ";
		int N=animation3.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=animation3[i]->number;
			Dest+=" ";
			Dest+=animation3[i]->SpriteID;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDANIMATION3);	
	REG_ENUM(_strindex,animation,DIES_ANIMATIONS);	
	REG_MEMBER(_int,Rotations);
	REG_CLASS(ANIMATION3);
	REG_AUTO(animation3);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDANIMATION4:public MDCommand{
public:
	_str animation;
	int Rotations;
	int number;
	int SpriteID;
	int SpriteID2;
	void Initialize(NewMonster* NM){
		char cc[64];
		sprintf(cc,"#%s",animation.str+1);
		NewAnimation* NANM=NM->CreateAnimation(cc);
		if(NANM){
			SetDefaultAnmTiring(NANM,cc);
			NANM->StartDx=NAStartDx;
			NANM->StartDy=NAStartDy;
			NANM->Parts=NAParts;
			NANM->PartSize=NAPartSize;
			NANM->Enabled=true;
			NANM->Rotations=Rotations;
			NANM->ActiveFrame=0;
			NANM->HotFrame=0;
			NANM->SoundID=-1;
			if(NANM->ActivePtX)free(NANM->ActivePtX);
			if(NANM->ActivePtY)free(NANM->ActivePtY);
			NANM->ActivePtX=znew(short,Rotations+Rotations+Rotations);
			NANM->ActivePtY=znew(short,Rotations+Rotations+Rotations);
			NANM->LineInfo=NULL;
			for(int i=0;i<Rotations+Rotations+Rotations;i++){
				NANM->ActivePtX[i]=0;
				NANM->ActivePtY[i]=0;
			};
			int dz,nz;
			int p1=RLCRef[number];
			if(SpriteID>=SpriteID2){
				if((SpriteID+1)*Rotations>GPS.GPNFrames(p1))
					return;
				dz=-1;
				nz=SpriteID-SpriteID2+1;
			}else{
				if((SpriteID2+1)*Rotations>GPS.GPNFrames(p1))
					return;
				dz=1;
				nz=SpriteID2-SpriteID+1;


			};
			NANM->NFrames=nz;
			NANM->Frames.Clear();for(i=0;i<nz;i++)NANM->Frames.Add(new NewFrame());
			int z3=SpriteID;
			for(i=0;i<nz;i++){
				NewFrame* NF=NANM->Frames[i];
				NF->FileID=p1;
				NF->SpriteID=z3;
				z3+=dz;
				if(NM->Building&&(NANM->Code<1500||NANM->Code>=1600)){
					NF->dx=NM->PicDx;
					NF->dy=NM->PicDy;
				}else{
					NF->dx=RLCdx[number];
					NF->dy=RLCdy[number];
				}
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=animation;
		Dest+="{CW} ";
		Dest+=Rotations;
		Dest+=" ";
		Dest+=number;
		Dest+=" ";
		Dest+=SpriteID;
		Dest+=" ";
		Dest+=SpriteID2;
		Dest+="{C} ";
		return Dest.str;
	}
	SAVE(MDANIMATION4);
	REG_ENUM(_strindex,animation,DIES_ANIMATIONS);
	REG_MEMBER(_int,Rotations);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,SpriteID);
	REG_MEMBER(_int,SpriteID2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class ANIMATION5:public BaseClass{
public:
	int P_File;
	int P_Start;
	int P_End;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=P_File;
		Dest+=" ";
		Dest+=P_Start;
		Dest+=" ";
		Dest+=P_End;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(ANIMATION5);
	REG_MEMBER(_int,P_File);
	REG_MEMBER(_int,P_Start);
	REG_MEMBER(_int,P_End);
	ENDSAVE;
};
class MDANIMATION5:public MDCommand{
public:
	_str animation;
	int Rotations;
	ClonesArray<ANIMATION5> animation5;
	void Initialize(NewMonster* NM){
		char cc[64];
		sprintf(cc,"#%s",animation.str+1);
		NewAnimation* NANM=NM->CreateAnimation(cc);
		if(NANM){
			SetDefaultAnmTiring(NANM,cc);
			int NPARTS=0;
			int NF=0;
			int N=animation5.GetAmount();
			for(int i=0;i<N;i++){
				NF+=abs(animation5[i]->P_Start-animation5[i]->P_End)+1;
			};
			NANM->StartDx=NAStartDx;
			NANM->StartDy=NAStartDy;
			NANM->Parts=NAParts;
			NANM->PartSize=NAPartSize;
			NANM->Enabled=true;
			NANM->Rotations=Rotations;
			NANM->ActiveFrame=0;
			NANM->HotFrame=0;
			NANM->SoundID=-1;
			if(NANM->ActivePtX)free(NANM->ActivePtX);
			if(NANM->ActivePtY)free(NANM->ActivePtY);
			NANM->ActivePtX=znew(short,Rotations+Rotations+Rotations);
			NANM->ActivePtY=znew(short,Rotations+Rotations+Rotations);
			NANM->LineInfo=NULL;
			for(i=0;i<Rotations+Rotations+Rotations;i++){
				NANM->ActivePtX[i]=0;
				NANM->ActivePtY[i]=0;
			}
			NANM->NFrames=NF;
			NANM->Frames.Clear();
			for(i=0;i<NF;i++)
				NANM->Frames.Add(new NewFrame());
			int p=0;
			for(i=0;i<N;i++){
				int s=animation5[i]->P_Start>animation5[i]->P_End?-1:1;
				int fn=animation5[i]->P_End+s;
				for(int sp=animation5[i]->P_Start;sp!=fn;sp+=s){
					NewFrame* NF=NANM->Frames[p];
					NF->FileID=RLCRef[animation5[i]->P_File];
					NF->SpriteID=sp;
					if(NM->Building){
						NF->dx=NM->PicDx;
						NF->dy=NM->PicDy;
					}else{
						NF->dx=RLCdx[animation5[i]->P_File];
						NF->dy=RLCdy[animation5[i]->P_File];
					}
					p++;
				}
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=animation;
		Dest+="{CW} ";
		Dest+=Rotations;
		Dest+=" ";
		int N=animation5.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=animation5[i]->P_File;
			Dest+=" ";
			Dest+=animation5[i]->P_Start;
			Dest+=" ";
			Dest+=animation5[i]->P_End;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDANIMATION5);
	REG_ENUM(_strindex,animation,DIES_ANIMATIONS);
	REG_MEMBER(_int,Rotations);
	REG_CLASS(ANIMATION5);
	REG_AUTO(animation5);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
//////////////////////////////////////////////////////////////////////////////////
class BARS3D:public BaseClass{
public:
	int XB;
	int YB;
	int L1;
	int L2;
	int Hi;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=XB;
		Dest+=" ";
		Dest+=YB;
		Dest+=" ";
		Dest+=L1;
		Dest+=" ";
		Dest+=L2;
		Dest+=" ";
		Dest+=Hi;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(BARS3D);
	REG_MEMBER(_int,XB);
	REG_MEMBER(_int,YB);
	REG_MEMBER(_int,L1);
	REG_MEMBER(_int,L2);
	REG_MEMBER(_int,Hi);
	ENDSAVE;
};
class MDBARS3D:public MDCommand{
public:
	ClonesArray<BARS3D> bars3d;
	void Initialize(NewMonster* NM){
		int N=bars3d.GetAmount();
		NM->NBars=N;
		NM->Bars3D=znew(short,N*5);
		for(int i=0;i<N;i++){
			NM->Bars3D[i*5]=bars3d[i]->XB;
			NM->Bars3D[i*5+1]=bars3d[i]->YB;
			NM->Bars3D[i*5+2]=bars3d[i]->L1;
			NM->Bars3D[i*5+3]=bars3d[i]->L2;
			NM->Bars3D[i*5+4]=bars3d[i]->Hi;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=bars3d.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>3)N1=3;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=bars3d[i]->XB;
			Dest+=" ";
			Dest+=bars3d[i]->YB;
			Dest+=" ";
			Dest+=bars3d[i]->L1;
			Dest+=" ";
			Dest+=bars3d[i]->L2;
			Dest+=" ";
			Dest+=bars3d[i]->Hi;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDBARS3D);
	REG_CLASS(BARS3D);
	REG_AUTO(bars3d);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDABILITY:public MDCommand{
public:
	_str ability;
	void Initialize(NewMonster* NM){
		bool AddMonsterAbility(MonsterAbility** MA,char* Name);
		AddMonsterAbility(&NM->Ability,ability.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ability;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDABILITY);
	REG_AUTO(ability);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDADDHDIR:public MDCommand{
public:
	_str animation;
	int direction;
	int height;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(animation.str,0);
		if(NANM){
			NANM->AddDirection=direction;
			NANM->AddHeight=height;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=direction;
		Dest+=" ";
		Dest+=height;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDADDHDIR);
	REG_AUTO(animation);
	REG_MEMBER(_int,direction);
	REG_MEMBER(_int,height);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDADDSHOTRADIUS:public MDCommand{
public:
	int addshotradius;
	void Initialize(NewMonster* NM){
		NM->AddShotRadius=addshotradius;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=addshotradius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDADDSHOTRADIUS);
	REG_MEMBER(_int,addshotradius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDADD_ATTACK_RADIUS:public MDCommand{
public:
	int number;
	int add_attack_radius;
	void Initialize(NewMonster* NM){
		NM->AttackRadiusAdd[number]=add_attack_radius;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=add_attack_radius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDADD_ATTACK_RADIUS);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,add_attack_radius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDAISHIP:public MDCommand{
public:
	_str aiship;
	int AI_PreferredAttR_Min;
	int AI_PreferredAttR_Max;
	void Initialize(NewMonster* NM){
		if(!strcmp(aiship.str,"B"))
			NM->AI_use_against_buildings=1;
		NM->AI_PreferredAttR_Min=AI_PreferredAttR_Min;
		NM->AI_PreferredAttR_Max=AI_PreferredAttR_Max;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=aiship;
		Dest+=" ";
		Dest+=AI_PreferredAttR_Min;
		Dest+=" ";
		Dest+=AI_PreferredAttR_Max;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDAISHIP);
	REG_AUTO(aiship);
	REG_MEMBER(_int,AI_PreferredAttR_Min);
	REG_MEMBER(_int,AI_PreferredAttR_Max);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDANMEXT:public MDCommand{
public:
	_str animation;
	_str sub_animation;
	int x;
	int y;
	int z;
	float scale;
	int period;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(animation.str,0);
		if(NANM){
			NewAnimation* SUB=NM->LoadNewAnimationByName(sub_animation.str,0);													
			if(SUB){
				AnimationExtension* AEX=new AnimationExtension;
				AEX->NA=SUB;
				AEX->dx=x;
				AEX->dy=y;
				AEX->dz=z;
				AEX->Scale=scale;
				AEX->dFi=0;
				AEX->dDir=0;
				AEX->Period=period;
				NANM->AnmExt.Add(AEX);
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=sub_animation;
		Dest+=" ";
		Dest+=x;
		Dest+=" ";
		Dest+=y;
		Dest+=" ";
		Dest+=z;
		Dest+=" ";
		Dest+=scale;
		Dest+=" ";
		Dest+=period;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDANMEXT);
	REG_AUTO(animation);
	REG_AUTO(sub_animation);
	REG_MEMBER(_int,x);
	REG_MEMBER(_int,y);
	REG_MEMBER(_int,z);
	REG_MEMBER(_float,scale);
	REG_MEMBER(_int,period);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDARCHER:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->Archer=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDARCHER);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDARMRADIUS:public MDCommand{
public:
	int armradius;
	void Initialize(NewMonster* NM){
		NM->ArmRadius=armradius;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=armradius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDARMRADIUS);
	REG_MEMBER(_int,armradius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDATTACK_ANGLES:public MDCommand{
public:
	int number;
	int AngleDn;
	int AngleUp;
	void Initialize(NewMonster* NM){
		if(number<NAttTypes){
			NM->AngleUp[number]=AngleUp;
			NM->AngleDn[number]=AngleDn;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=AngleDn;
		Dest+=" ";
		Dest+=AngleUp;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDATTACK_ANGLES);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,AngleDn);
	REG_MEMBER(_int,AngleUp);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDATTACK_PAUSE:public MDCommand{
public:
	int number;
	int attack_pause;
	void Initialize(NewMonster* NM){
		if(number<NAttTypes)
			NM->AttackPause[number]=attack_pause;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=attack_pause;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDATTACK_PAUSE);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,attack_pause);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDATTACK_RADIUS:public MDCommand{
public:
	int number;
	int attack_radius1;
	int attack_radius2;
	void Initialize(NewMonster* NM){
		if(number<NAttTypes){
			NM->AttackRadius1[number]=attack_radius1;
			NM->AttackRadius2[number]=attack_radius2;
			NM->DetRadius1[number]=attack_radius1;
			NM->DetRadius2[number]=attack_radius2;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=attack_radius1;
		Dest+=" ";
		Dest+=attack_radius2;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDATTACK_RADIUS);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,attack_radius1);
	REG_MEMBER(_int,attack_radius2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class ATTMASK:public BaseClass{
public:
	_str mask;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=mask;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(ATTMASK);
	REG_AUTO(mask);
	ENDSAVE;
};
class MDATTMASK:public MDCommand{
public:
	int number;
	ClonesArray<ATTMASK> attmask;
	void Initialize(NewMonster* NM){
		NM->AttackMask[number]=0;
		int N=attmask.GetAmount();
		for(int i=0;i<N;i++){
			int p2=GetMatherialType(attmask[i]->mask.str);
			if(p2!=-1){
				NM->AttackMask[number]|=p2;
				if(!strcmp(attmask[i]->mask.str,"BUILDING"))
					NM->AttBuild=true;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+="  ";
		int N=attmask.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" ";
			Dest+=attmask[i]->mask;
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDATTMASK);
	REG_MEMBER(_int,number);
	REG_CLASS(ATTMASK);
	REG_AUTO(attmask);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDAUTOROTATE:public MDCommand{
public:
	int dx;
	int dy;
	_str autorotateL;
	_str autorotateR;
	void Initialize(NewMonster* NM){
		NewAnimation* L=NM->CreateAnimation("#ROTATEL");
		NewAnimation* R=NM->CreateAnimation("#ROTATER");
		int LF=GPS.PreLoadGPImage(autorotateL.str);
		int RF=GPS.PreLoadGPImage(autorotateR.str);
		for(int i=0;i<128;i++){
			NewFrame* NFL=new NewFrame;
			NFL->FileID=LF;
			NFL->dx=dx;
			NFL->dy=dy;
			NFL->SpriteID=((16-(i/16)*2)%16)+(i%16)*16;
			L->Frames.Add(NFL);
			NewFrame* NFR=new NewFrame;
			NFR->FileID=RF;
			NFR->dx=dx;
			NFR->dy=dy;
			NFR->SpriteID=((i/16)*2)+(i%16)*16;
			R->Frames.Add(NFR);
		}
		L->NFrames=128;
		R->NFrames=128;
		L->Rotations=1;
		R->Rotations=1;
		L->Enabled=1;
		R->Enabled=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=dx;
		Dest+=" ";
		Dest+=dy;
		Dest+=" ";
		Dest+=autorotateL;
		Dest+=" ";
		Dest+=autorotateR;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDAUTOROTATE);
	REG_MEMBER(_int,dx);
	REG_MEMBER(_int,dy);
	REG_AUTO(autorotateL);
	REG_AUTO(autorotateR);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBIGICON:public MDCommand{
public:
	_str bigicon;
	int BigIconIndex;
	void Initialize(NewMonster* NM){
		NM->BigIconFile=0xFFFF;
		NM->BigIconFile=GPS.PreLoadGPImage(bigicon.str);
		if(NM->BigIconFile!=0xFFFF)
			NM->BigIconIndex=BigIconIndex;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=bigicon;
		Dest+=" ";
		Dest+=BigIconIndex;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBIGICON);
	REG_AUTO(bigicon);
	REG_MEMBER(_int,BigIconIndex);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBOIDSMOVING:public MDCommand{
public:
	int BoidsMovingMinDist;
	int BoidsMovingWeight;
	void Initialize(NewMonster* NM){
		NM->BoidsMoving=true;
		NM->BoidsMovingMinDist=BoidsMovingMinDist;
		NM->BoidsMovingWeight=BoidsMovingWeight;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=BoidsMovingMinDist;
		Dest+=" ";
		Dest+=BoidsMovingWeight;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBOIDSMOVING);
	REG_MEMBER(_int,BoidsMovingMinDist);
	REG_MEMBER(_int,BoidsMovingWeight);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class BORNPOINTS2:public BaseClass{
public:
	int ptx;
	int pty;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ptx;
		Dest+=" ";
		Dest+=pty;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(BORNPOINTS2);
	REG_MEMBER(_int,ptx);
	REG_MEMBER(_int,pty);
	ENDSAVE;
};
class MDBORNPOINTS2:public MDCommand{
public:
	ClonesArray<BORNPOINTS2> bornpoints2;
	void Initialize(NewMonster* NM){
		NM->BornPtX.Clear();
		NM->BornPtY.Clear();
		int N=bornpoints2.GetAmount();
		for(int i=0;i<N;i++){
			NM->BornPtX.Add(bornpoints2[i]->ptx);
			NM->BornPtY.Add(bornpoints2[i]->pty<<1);
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=bornpoints2.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=bornpoints2[i]->ptx;
			Dest+=" ";
			Dest+=bornpoints2[i]->pty;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDBORNPOINTS2);
	REG_CLASS(BORNPOINTS2);
	REG_AUTO(bornpoints2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBRANDOMPOS:public MDCommand{
public:
	int brandompos;
	void Initialize(NewMonster* NM){
		NM->BRandomPos=brandompos;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=brandompos;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBRANDOMPOS);
	REG_MEMBER(_int,brandompos);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBRANDOMSPEED:public MDCommand{
public:
	int brandomspeed;
	void Initialize(NewMonster* NM){
		NM->BRandomSpeed=brandomspeed;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=brandomspeed;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBRANDOMSPEED);
	REG_MEMBER(_int,brandomspeed);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBREAKANIMATION:public MDCommand{
public:
	_str breakanimation;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(breakanimation.str,0);
		if(NANM)
			NANM->CanBeBroken=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=breakanimation;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBREAKANIMATION);
	REG_AUTO(breakanimation);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBREFLECT:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->BReflection=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBREFLECT);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBUILDBAR:public MDCommand{
public:
	int dx0;
	int dy0;
	int dx1;
	int dy1;
	void Initialize(NewMonster* NM){
		NM->BuildX0=NM->PicDx+(dx0<<4);
		NM->BuildY0=(NM->PicDy+(dy0<<3))<<1;
		NM->BuildX1=NM->PicDx+(dx1<<4);
		NM->BuildY1=(NM->PicDy+(dy1<<3))<<1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=dx0;
		Dest+=" ";
		Dest+=dy0;
		Dest+=" ";
		Dest+=dx1;
		Dest+=" ";
		Dest+=dy1;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBUILDBAR);
	REG_MEMBER(_int,dx0);
	REG_MEMBER(_int,dy0);
	REG_MEMBER(_int,dx1);
	REG_MEMBER(_int,dy1);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class BUILDLOCKPOINTS:public BaseClass{
public:
	int BLockX;
	int BLockY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=BLockX;
		Dest+=" ";
		Dest+=BLockY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(BUILDLOCKPOINTS);
	REG_MEMBER(_int,BLockX);
	REG_MEMBER(_int,BLockY);
	ENDSAVE;
};
class MDBUILDLOCKPOINTS:public MDCommand{
public:
	ClonesArray<BUILDLOCKPOINTS> buildlockpoints;
	void Initialize(NewMonster* NM){
		int N=buildlockpoints.GetAmount();
		NM->NBLockPt=N;
		NM->BLockX=znew(byte,N);
		NM->BLockY=znew(byte,N);
		for(int i=0;i<N;i++){
			NM->BLockX[i]=buildlockpoints[i]->BLockX;
			NM->BLockY[i]=buildlockpoints[i]->BLockY;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=buildlockpoints.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=buildlockpoints[i]->BLockX;
			Dest+=" ";
			Dest+=buildlockpoints[i]->BLockY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDBUILDLOCKPOINTS);
	REG_CLASS(BUILDLOCKPOINTS);
	REG_AUTO(buildlockpoints);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class BUILDPOINTS:public BaseClass{
public:
	int BuildPtX;
	int BuildPtY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=BuildPtX;
		Dest+=" ";
		Dest+=BuildPtY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(BUILDPOINTS);
	REG_MEMBER(_int,BuildPtX);
	REG_MEMBER(_int,BuildPtY);
	ENDSAVE;
};
class MDBUILDPOINTS:public MDCommand{
public:
	ClonesArray<BUILDPOINTS> buildpoints;
	void Initialize(NewMonster* NM){
		int N=buildpoints.GetAmount();
		NM->BuildPtX.Clear();
		NM->BuildPtY.Clear();
		for(int i=0;i<N;i++){
			NM->BuildPtX.Add(buildpoints[i]->BuildPtX);
			NM->BuildPtY.Add(buildpoints[i]->BuildPtY);
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=buildpoints.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=buildpoints[i]->BuildPtX;
			Dest+=" ";
			Dest+=buildpoints[i]->BuildPtY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDBUILDPOINTS);
	REG_CLASS(BUILDPOINTS);
	REG_AUTO(buildpoints);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDBUILDSTAGES:public MDCommand{
public:
	int buildstages;
	void Initialize(NewMonster* NM){
		NM->ProduceStages=buildstages;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=buildstages;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDBUILDSTAGES);
	REG_MEMBER(_int,buildstages);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class CANKILL:public BaseClass{
public:
	_str object;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=object;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(CANKILL);
	REG_AUTO(object);
	ENDSAVE;
};
class MDCANKILL:public MDCommand{
public:
	ClonesArray<CANKILL> cankill;
	void Initialize(NewMonster* NM){
		int N=cankill.GetAmount();
		for(int i=0;i<N;i++){
			int p2=GetMatherialType(cankill[i]->object.str);
			if(p2!=-1)NM->KillMask|=p2;
		}
		for(i=0;i<NAttTypes;i++){
			NM->AttackMask[i]=NM->KillMask;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=cankill.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" ";
			Dest+=cankill[i]->object;
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDCANKILL);
	REG_CLASS(CANKILL);
	REG_AUTO(cankill);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCANSTORM:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->CanStorm=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCANSTORM);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCANTCAPTURE:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->CantCapture=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCANTCAPTURE);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCAPTURE:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->Capture=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCAPTURE);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class CHECKPOINTS:public BaseClass{
public:
	int CheckX;
	int CheckY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=CheckX;
		Dest+=" ";
		Dest+=CheckY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(CHECKPOINTS);
	REG_MEMBER(_int,CheckX);
	REG_MEMBER(_int,CheckY);
	ENDSAVE;
};
class MDCHECKPOINTS:public MDCommand{
public:
	ClonesArray<CHECKPOINTS> checkpoints;
	void Initialize(NewMonster* NM){
		int N=checkpoints.GetAmount();
		NM->NCheckPt=N;
		NM->CheckX=znew(byte,N);
		NM->CheckY=znew(byte,N);
		for(int i=0;i<N;i++){
			NM->CheckX[i]=checkpoints[i]->CheckX;
			NM->CheckY[i]=checkpoints[i]->CheckY;
		};
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=checkpoints.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=checkpoints[i]->CheckX;
			Dest+=" ";
			Dest+=checkpoints[i]->CheckY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDCHECKPOINTS);
	REG_CLASS(CHECKPOINTS);
	REG_AUTO(checkpoints);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCOLORVARIATION:public MDCommand{
public:
	int colorvariation;
	void Initialize(NewMonster* NM){
		NM->ColorVariation=colorvariation;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=colorvariation;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCOLORVARIATION);
	REG_MEMBER(_int,colorvariation);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCOMMANDCENTER:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->CommandCenter=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCOMMANDCENTER);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCOMPLEXOBJECT:public MDCommand{
public:
	_str complexobject;
	void Initialize(NewMonster* NM){
		word GetComplexObjectIndex(char* Name);
		NM->ComplexObjIndex=GetComplexObjectIndex(complexobject.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=complexobject;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCOMPLEXOBJECT);
	REG_AUTO(complexobject);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class CONCENTRATOR2:public BaseClass{
public:
	int ConcPtX;
	int ConcPtY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ConcPtX;
		Dest+=" ";
		Dest+=ConcPtY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(CONCENTRATOR2);
	REG_MEMBER(_int,ConcPtX);
	REG_MEMBER(_int,ConcPtY);
	ENDSAVE;
};
class MDCONCENTRATOR2:public MDCommand{
public:
	ClonesArray<CONCENTRATOR2> concentrator2;
	void Initialize(NewMonster* NM){
		int N=concentrator2.GetAmount();
		NM->ConcPtX.Clear();
		NM->ConcPtY.Clear();
		for(int i=0;i<N;i++){
			NM->ConcPtX.Add(concentrator2[i]->ConcPtX);
			NM->ConcPtY.Add(concentrator2[i]->ConcPtY<<1);
		};
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=concentrator2.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=concentrator2[i]->ConcPtX;
			Dest+=" ";
			Dest+=concentrator2[i]->ConcPtY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDCONCENTRATOR2);
	REG_CLASS(CONCENTRATOR2);
	REG_AUTO(concentrator2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDCOSTPERCENT:public MDCommand{
public:
	int costpercent;
	void Initialize(NewMonster* NM){
		NM->CostPercent=costpercent;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=costpercent;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDCOSTPERCENT);
	REG_MEMBER(_int,costpercent);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDDAMAGE:public MDCommand{
public:
	int number;
	int damage;
	void Initialize(NewMonster* NM){
		NM->MinDamage[number]=damage;
		NM->MaxDamage[number]=damage;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=damage;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDDAMAGE);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,damage);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class DAMPOINTS:public BaseClass{
public:
	int DamPtX;
	int DamPtY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=DamPtX;
		Dest+=" ";
		Dest+=DamPtY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(DAMPOINTS);
	REG_MEMBER(_int,DamPtX);
	REG_MEMBER(_int,DamPtY);
	ENDSAVE;
};
class MDDAMPOINTS:public MDCommand{
public:
	ClonesArray<DAMPOINTS> dampoints;
	void Initialize(NewMonster* NM){
		int N=dampoints.GetAmount();
		NM->DamPtX.Clear();
		NM->DamPtY.Clear();
		for(int i=0;i<N;i++){
			NM->DamPtX.Add(dampoints[i]->DamPtX);
			NM->DamPtY.Add(dampoints[i]->DamPtY<<1);
		};
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=dampoints.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=dampoints[i]->DamPtX;
			Dest+=" ";
			Dest+=dampoints[i]->DamPtY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDDAMPOINTS);
	REG_CLASS(DAMPOINTS);
	REG_AUTO(dampoints);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class DESTRUCT:public BaseClass{
public:
	_str Weapon;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=Weapon;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(DESTRUCT);
	REG_AUTO(Weapon);
	ENDSAVE;
};
class MDDESTRUCT:public MDCommand{
public:
	int WProb;
	ClonesArray<DESTRUCT> destruct;
	void Initialize(NewMonster* NM){
		NM->Destruct.WProb=WProb;
		int N=destruct.GetAmount();
		NM->Destruct.NWeap=N;
		NM->Destruct.Weap=znew(word,N);
		for(int i=0;i<N;i++){
		    int p3=GetWeaponIndex(destruct[i]->Weapon.str);
			if(p3!=-1)
				NM->Destruct.Weap[i]=p3;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=WProb;
		Dest+=" ";
		int N=destruct.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" ";
			Dest+=destruct[i]->Weapon;
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDDESTRUCT);
	REG_MEMBER(_int,WProb);
	REG_CLASS(DESTRUCT);
	REG_AUTO(destruct);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDDET_RADIUS:public MDCommand{
public:
	int number;
	int DetRadius1;
	int DetRadius2;
	void Initialize(NewMonster* NM){
		if(number<NAttTypes){
			NM->DetRadius1[number]=DetRadius1;
			NM->DetRadius2[number]=DetRadius2;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=DetRadius1;
		Dest+=" ";
		Dest+=DetRadius2;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDDET_RADIUS);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,DetRadius1);
	REG_MEMBER(_int,DetRadius2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDEXITPAUSE:public MDCommand{
public:
	int exitpause;
	void Initialize(NewMonster* NM){
		NM->ExitPause=exitpause;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=exitpause;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDEXITPAUSE);
	REG_MEMBER(_int,exitpause);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDEXPLMEDIA:public MDCommand{
public:
	_str explmedia;
	int EMediaRadius;
	void Initialize(NewMonster* NM){
		int p2=GetExMedia(explmedia.str);
		if(p2!=-1){
			NM->ExplosionMedia=p2;
			NM->EMediaRadius=EMediaRadius;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=explmedia;
		Dest+=" ";
		Dest+=EMediaRadius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDEXPLMEDIA);
	REG_AUTO(explmedia);
	REG_MEMBER(_int,EMediaRadius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDEXSPRITES:public MDCommand{
public:
	_str exsprites;
	int BRadius;
	int Radius1;
	int MotionDist;
	int Radius2;
	void Initialize(NewMonster* NM){
		NM->BRadius=BRadius;
		NM->Radius1=Radius1;
		NM->MotionDist=MotionDist;
		NM->Radius2=Radius2;
		int p1=COMPLEX.GetIndexByName(exsprites.str);
		if(p1!=-1){
			NM->ExField=1;
			NM->Sprite=p1;
			NM->SpriteVisual=p1;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=exsprites;
		Dest+=" ";
		Dest+=BRadius;
		Dest+=" ";
		Dest+=Radius1;
		Dest+=" ";
		Dest+=MotionDist;
		Dest+=" ";
		Dest+=Radius2;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDEXSPRITES);
	REG_AUTO(exsprites);
	REG_MEMBER(_int,BRadius);
	REG_MEMBER(_int,Radius1);
	REG_MEMBER(_int,MotionDist);
	REG_MEMBER(_int,Radius2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
/*class MDFEARFACTOR:public MDCommand{
public:
	int idx;
	int fearfactor;
	void Initialize(NewMonster* NM){
		if(idx<NFEARSUBJ)
			NM->FearFactor[idx]=fearfactor;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=idx;
		Dest+=" ";
		Dest+=fearfactor;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFEARFACTOR);
	REG_MEMBER(_int,idx);
	REG_MEMBER(_int,fearfactor);
	REG_PARENT(MDCommand);
	ENDSAVE;
};*/
class MDFEARRADIUS:public MDCommand{
public:
	int idx;
	int fearradius;
	void Initialize(NewMonster* NM){
		if(idx<NFEARSUBJ){
			if(fearradius>255)fearradius=255;
			NM->FearRadius[idx]=fearradius;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=idx;
		Dest+=" ";
		Dest+=fearradius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFEARRADIUS);
	REG_MEMBER(_int,idx);
	REG_MEMBER(_int,fearradius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDFEARSTART:public MDCommand{
public:
	int fearstart;
	void Initialize(NewMonster* NM){
		NM->StartMorale=fearstart;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=fearstart;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFEARSTART);
	REG_MEMBER(_int,fearstart);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDFEARTYPE:public MDCommand{
public:
	int idx;
	int feartype;
	void Initialize(NewMonster* NM){
		if(idx<NAttTypes && feartype<NFEARSUBJ)
			NM->FearType[idx]=feartype;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=idx;
		Dest+=" ";
		Dest+=feartype;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFEARTYPE);
	REG_MEMBER(_int,idx);
	REG_MEMBER(_int,feartype);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class FIRES:public BaseClass{
public:
	int FireX;
	int FireY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=FireX;
		Dest+=" ";
		Dest+=FireY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(FIRES);
	REG_MEMBER(_int,FireX);
	REG_MEMBER(_int,FireY);
	ENDSAVE;
};
class MDFIRES:public MDCommand{
public:
	ClonesArray<FIRES> fires;
	void Initialize(NewMonster* NM){
		int N=fires.GetAmount();
		NM->FireX[0]=znew(short,N);
		NM->FireY[0]=znew(short,N);
		NM->NFires[0]=N;
		for(int i=0;i<N;i++){
			NM->FireX[0][i]=fires[i]->FireX;
			NM->FireY[0][i]=fires[i]->FireY;
		};
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=fires.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=fires[i]->FireX;
			Dest+=" ";
			Dest+=fires[i]->FireY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDFIRES);
	REG_CLASS(FIRES);
	REG_AUTO(fires);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class FLAGS:public BaseClass{
public:
	int x1;
	int x2;
	int y2;
	int dy;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=x1;
		Dest+=" ";
		Dest+=x2;
		Dest+=" ";
		Dest+=y2;
		Dest+=" ";
		Dest+=dy;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(FLAGS);
	REG_MEMBER(_int,x1);
	REG_MEMBER(_int,x2);
	REG_MEMBER(_int,y2);
	REG_MEMBER(_int,dy);
	ENDSAVE;
};
class MDFLAGS:public MDCommand{
public:
	int xr;
	ClonesArray<FLAGS> flags;
	void Initialize(NewMonster* NM){
		int N=flags.GetAmount();
		NM->FLAGS=(Flags3D*)malloc(sizeof(Flags3D)-48*2+N*8);
		NM->FLAGS->N=N;
		NM->FLAGS->Xr=xr;
		for(int i=0;i<N;i++){
			int i3=i<<2;
			NM->FLAGS->Points[i3]=flags[i]->x1;
			NM->FLAGS->Points[i3+1]=flags[i]->x2;
			NM->FLAGS->Points[i3+2]=flags[i]->y2;
			NM->FLAGS->Points[i3+3]=flags[i]->dy;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=xr;
		Dest+=" ";
		int N=flags.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
        int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=flags[i]->x1;
			Dest+=" ";
			Dest+=flags[i]->x2;
			Dest+=" ";
			Dest+=flags[i]->y2;
			Dest+=" ";
			Dest+=flags[i]->dy;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDFLAGS);
	REG_MEMBER(_int,xr);
	REG_CLASS(FLAGS);
	REG_AUTO(flags);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDFORCE:public MDCommand{
public:
	int force;
	void Initialize(NewMonster* NM){
		NM->Force=force;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=force;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFORCE);
	REG_MEMBER(_int,force);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDFORMDISTSCALE:public MDCommand{
public:
	int formdistscale;
	void Initialize(NewMonster* NM){
		NM->FormationDistanceScale=formdistscale;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=formdistscale;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFORMDISTSCALE);
	REG_MEMBER(_int,formdistscale);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDFORMLIKESHOOTERS:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->FormLikeShooters=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFORMLIKESHOOTERS);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDFREESHOTDIST:public MDCommand{
public:
	int freeshotdist;
	void Initialize(NewMonster* NM){
		NM->FreeShotDist=freeshotdist;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=freeshotdist;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDFREESHOTDIST);
	REG_MEMBER(_int,freeshotdist);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDGATE:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->UseLikeGate=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDGATE);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDGEOMETRY:public MDCommand{
public:
	int Radius1;
	int Radius2;
	int MotionDist;
	void Initialize(NewMonster* NM){
		NM->Radius1=Radius1<<4;
		NM->Radius2=Radius2<<4;
		NM->MotionDist=MotionDist;
		for(int i=0;i<256;i++){
			NM->POneStepDX[i]=(TCos[i]*NM->MotionDist)>>4;
			NM->POneStepDY[i]=(TSin[i]*NM->MotionDist)>>4;
			NM->OneStepDX[i]=(TCos[i]*NM->MotionDist)>>8;
			NM->OneStepDY[i]=(TSin[i]*NM->MotionDist)>>8;
		};
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=Radius1;
		Dest+=" ";
		Dest+=Radius2;
		Dest+=" ";
		Dest+=MotionDist;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDGEOMETRY);
	REG_MEMBER(_int,Radius1);
	REG_MEMBER(_int,Radius2);
	REG_MEMBER(_int,MotionDist);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDICON:public MDCommand{
public:
	_str icon;
	void Initialize(NewMonster* NM){
		int p2=GetIconByName(icon.str);
		if(p2!=-1){
			NM->IconFileID=0;
			NM->IconID=p2;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=icon;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDICON);
	REG_AUTO(icon);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDINFO:public MDCommand{
public:
	int InfType;
	int PictureID;
	void Initialize(NewMonster* NM){
		NM->InfType=InfType;
		NM->PictureID=PictureID;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=InfType;
		Dest+=" ";
		Dest+=PictureID;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDINFO);
	REG_MEMBER(_int,InfType);
	REG_MEMBER(_int,PictureID);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDINMENUICON:public MDCommand{
public:
	_str inmenuicon;
	int InMenuIconIndex;
	void Initialize(NewMonster* NM){
		NM->InMenuIconFile=0xFFFF;
		NM->InMenuIconFile=GPS.PreLoadGPImage(inmenuicon.str);
		if(NM->InMenuIconFile!=0xFFFF)
			NM->InMenuIconIndex=InMenuIconIndex;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=inmenuicon;
		Dest+=" ";
		Dest+=InMenuIconIndex;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDINMENUICON);
	REG_AUTO(inmenuicon);
	REG_MEMBER(_int,InMenuIconIndex);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDLIFE:public MDCommand{
public:
	int life;
	void Initialize(NewMonster* NM){
		NM->Life=life;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=life;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDLIFE);
	REG_MEMBER(_int,life);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDLINEARSORT:public MDCommand{
public:
	int linearsort;
	void Initialize(NewMonster* NM){
		NM->LinearLength=linearsort;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=linearsort;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDLINEARSORT);
	REG_MEMBER(_int,linearsort);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class LINESORT:public BaseClass{
public:
	SAVE(LINESORT);
	ENDSAVE;
};
class LINELINESORT:public LINESORT{
public:
	int x1;
	int y1;
	int x2;
	int y2;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+="LINE";
		Dest+="{CW} ";
		Dest+=x1;
		Dest+=" ";
		Dest+=y1;
		Dest+=" ";
		Dest+=x2;
		Dest+=" ";
		Dest+=y2;
		Dest+=" {C}";
		return Dest.str;
	}

	SAVE(LINELINESORT);
	REG_PARENT(LINESORT);
	REG_MEMBER(_int,x1);
	REG_MEMBER(_int,y1);
	REG_MEMBER(_int,x2);
	REG_MEMBER(_int,y2);
	ENDSAVE;
};
class POINTLINESORT:public LINESORT{
public:
	int x1;
	int y1;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+="POINT";
		Dest+="{CW} ";
		Dest+=x1;
		Dest+=" ";
		Dest+=y1;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(POINTLINESORT);
	REG_PARENT(LINESORT);
	REG_MEMBER(_int,x1);
	REG_MEMBER(_int,y1);
	ENDSAVE;
};
class GROUNDLINESORT:public LINESORT{
public:
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+="GROUND";
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(GROUNDLINESORT);
	REG_PARENT(LINESORT);
	ENDSAVE;
};
class TOPLINESORT:public LINESORT{
public:
const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+="TOP";
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(TOPLINESORT);
	REG_PARENT(LINESORT);
	ENDSAVE;
};
class MDLINESORT:public MDCommand{
public:
	_str animation;
	_str line;
	ClassArray<LINESORT> linesort;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(animation.str,0);
		if(NANM){
			int L=0;
			short* LINF=znew(short,NANM->NFrames<<2);
			NANM->LineInfo=LINF; 
			int LoIdx[20];
			int NLo=0;
			int MinY=10000;
			int MinX=10000;
			int MaxX=-10000;
			for(int i=0;i<NANM->NFrames;i++){ 
				int x1,y1,x2,y2;
				char gy[128];
				sprintf(gy,"%s",GetWord(line.str+L,L));
				int p=i<<2;
				if(!strcmp(gy,"LINE")){
					LINELINESORT* LS=new LINELINESORT;
					linesort.Add(LS);
					LS->x1=atoi(GetWord(line.str+L,L));
					LS->y1=atoi(GetWord(line.str+L,L));
					LS->x2=atoi(GetWord(line.str+L,L));
					LS->y2=atoi(GetWord(line.str+L,L));
					LINF[ p ]=LS->x1;
					LINF[p+1]=LS->y1;
					LINF[p+2]=LS->x2;
					LINF[p+3]=LS->y2;
					if(LS->y1<MinY)MinY=LS->y1;
					if(LS->x1<MinX)MinX=LS->x1;
					if(LS->x1>MaxX)MaxX=LS->x1;
					if(LS->y2<MinY)MinY=LS->y2;
					if(LS->x2<MinX)MinX=LS->x2;
					if(LS->x2>MaxX)MaxX=LS->x2;
				}else
				if(!strcmp(gy,"POINT")){
					POINTLINESORT* LS=new POINTLINESORT;
					linesort.Add(LS);
					LS->x1=atoi(GetWord(line.str+L,L));
					LS->y1=atoi(GetWord(line.str+L,L));
					int p=i<<2;
					LINF[ p ]=LS->x1;
					LINF[p+1]=LS->y1;
					LINF[p+2]=LS->x1;
					LINF[p+3]=LS->y1;
					if(LS->y1<MinY)MinY=LS->y1;
					if(LS->x1<MinX)MinX=LS->x1;
					if(LS->x1>MaxX)MaxX=LS->x1;
				}else
				if(!strcmp(gy,"GROUND")){
					GROUNDLINESORT* LS=new GROUNDLINESORT;
					linesort.Add(LS);
					LINF[p  ]=-10000;
					LINF[p+1]=-10000;
					LINF[p+2]=-10000;
					LINF[p+3]=-10000;
					if(NLo<20){
						LoIdx[NLo]=i;
						NLo++;
					};
				}else
				if(!strcmp(gy,"TOP")){
					TOPLINESORT* LS=new TOPLINESORT;
					linesort.Add(LS);
					LINF[p  ]=10000;
					LINF[p+1]=10000;
					LINF[p+2]=10000;
					LINF[p+3]=10000;
				}
			}
			if(NLo){
				MinY=-10;
				int avx=(MinX+MaxX)>>1;
				for(i=0;i<NLo;i++){
					int idx=LoIdx[i]<<2;
#ifdef _USE3D
					if(LINF[idx  ]!=-10000)LINF[idx  ]=avx;
#else
					LINF[idx  ]=avx;
#endif
					LINF[idx+1]=MinY;
					LINF[idx+2]=avx;
					LINF[idx+3]=MinY;
				}
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		int N=linesort.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>5)N1=5;
		for(int i=0;i<N1;i++){
			Dest+=linesort[i]->GetThisElementView("");
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDLINESORT);
	REG_AUTO(animation);
	REG_AUTO(line);
	REG_CLASS(LINESORT);
	REG_CLASS(LINELINESORT);
	REG_CLASS(POINTLINESORT);
	REG_CLASS(GROUNDLINESORT);
	REG_CLASS(TOPLINESORT);
	REG_AUTO(linesort);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDLOCATION:public MDCommand{
public:
	int PicDx;
	int PicDy;
	int PicLx;
	int PicLy;
	void Initialize(NewMonster* NM){
		NM->PicDx=PicDx;
		NM->PicDy=PicDy;
		NM->PicLx=PicLx;
		NM->PicLy=PicLy;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=PicDx;
		Dest+=" ";
		Dest+=PicDy;
		Dest+=" ";
		Dest+=PicLx;
		Dest+=" ";
		Dest+=PicLy;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDLOCATION);
	REG_MEMBER(_int,PicDx);
	REG_MEMBER(_int,PicDy);
	REG_MEMBER(_int,PicLx);
	REG_MEMBER(_int,PicLy);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class LOCKPOINTS:public BaseClass{
public:
	int LockX;
	int LockY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=LockX;
		Dest+=" ";
		Dest+=LockY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(LOCKPOINTS);
	REG_MEMBER(_int,LockX);
	REG_MEMBER(_int,LockY);
	ENDSAVE;
};
class MDLOCKPOINTS:public MDCommand{
public:
	ClonesArray<LOCKPOINTS> lockpoints;
	void Initialize(NewMonster* NM){
		int N=lockpoints.GetAmount();
		NM->NLockPt=N;
		NM->LockX=znew(byte,N);
		NM->LockY=znew(byte,N);
		for(int i=0;i<N;i++){
			NM->LockX[i]=lockpoints[i]->LockX;
			NM->LockY[i]=lockpoints[i]->LockY;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=lockpoints.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=lockpoints[i]->LockX;
			Dest+=" ";
			Dest+=lockpoints[i]->LockY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDLOCKPOINTS);
	REG_CLASS(LOCKPOINTS);
	REG_AUTO(lockpoints);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDLONGDEATH:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->LongDeath=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDLONGDEATH);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDLOWCOSTRADIUS:public MDCommand{
public:
	int lowcostradius;
	void Initialize(NewMonster* NM){
		NM->LowCostRadius=lowcostradius;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=lowcostradius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDLOWCOSTRADIUS);
	REG_MEMBER(_int,lowcostradius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MATHERIAL:public BaseClass{
public:
	_str object;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=object;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MATHERIAL);
	REG_AUTO(object);
	ENDSAVE;
};
class MDMATHERIAL:public MDCommand{
public:
	ClonesArray<MATHERIAL> matherial;
	void Initialize(NewMonster* NM){
		int N=matherial.GetAmount();
		for(int i=0;i<N;i++){
			int p2=GetMatherialType(matherial[i]->object.str);
			if(p2!=-1){
		        NM->MathMask|=p2;
				if(!strcmp(matherial[i]->object.str,"BUILDING")){
					NM->AttBuild=true;
				};
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=matherial.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" ";
			Dest+=matherial[i]->object;
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDMATHERIAL);
	REG_CLASS(MATHERIAL);
	REG_AUTO(matherial);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMEDIA:public MDCommand{
public:
	_str locktype;
	void Initialize(NewMonster* NM){
		if(!strcmp(locktype.str,"LAND"))NM->LockType=0;
		else if(!strcmp(locktype.str,"WATER"))NM->LockType=1;
		else if(!strcmp(locktype.str,"2"))NM->LockType=2;
		else if(!strcmp(locktype.str,"3"))NM->LockType=3;
		else if(!strcmp(locktype.str,"4"))NM->LockType=4;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=locktype;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMEDIA);
	REG_AUTO(locktype);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMESSAGE:public MDCommand{
public:
	_str message;
	void Initialize(NewMonster* NM){
		NM->Message=znew(char,strlen(message.str)+1);
		strcpy(NM->Message,message.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=message;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMESSAGE);
	REG_AUTO(message);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMFARM:public MDCommand{
public:
	int NInFarm;
	void Initialize(NewMonster* NM){
		NM->NInFarm=NInFarm;
		NM->Farm=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=NInFarm;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMFARM);
	REG_MEMBER(_int,NInFarm);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMINICON:public MDCommand{
public:
	_str minicon;
	int MinIconIndex;
	void Initialize(NewMonster* NM){
		NM->MinIconFile=0xFFFF;
		NM->MinIconFile=GPS.PreLoadGPImage(minicon.str);
		if(NM->MinIconFile!=0xFFFF)
			NM->MinIconIndex=MinIconIndex;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=minicon;
		Dest+=" ";
		Dest+=MinIconIndex;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMINICON);
	REG_AUTO(minicon);
	REG_MEMBER(_int,MinIconIndex);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMOREANIMATION:public MDCommand{
public:
	_str animation;
	_str model;
	float DirFactor;
	void Initialize(NewMonster* NM){
		NewAnimation* NA=NM->LoadNewAnimationByName(animation.str,0);
		if(NA){
			int aid=IMM->GetModelID(model.str);
			if(aid!=-1){
				NA->SecondAnimationID=aid;
				NA->DirFactor=DirFactor;
				NA->AnimationType=true;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=model;
		Dest+=" ";
		Dest+=DirFactor;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMOREANIMATION);
	REG_AUTO(animation);
	REG_AUTO(model);
	REG_MEMBER(_float,DirFactor);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMOTIONSTYLE:public MDCommand{
public:
	_str motionstyle;
	void Initialize(NewMonster* NM){
		if(!strcmp(motionstyle.str,"FASTROTATE&MOVE"))NM->MotionStyle=0;
		else if(!strcmp(motionstyle.str,"SLOWROTATE"))NM->MotionStyle=1;
		else if(!strcmp(motionstyle.str,"SHEEPS"))NM->MotionStyle=2;
		else if(!strcmp(motionstyle.str,"COMPLEXROTATE"))NM->MotionStyle=3;
		else if(!strcmp(motionstyle.str,"ROTATE&MOVE"))NM->MotionStyle=4;
		else if(!strcmp(motionstyle.str,"NEWSHEEPS"))NM->MotionStyle=5;
		else if(!strcmp(motionstyle.str,"COMPLEXOBJECT"))NM->MotionStyle=6;
		else if(!strcmp(motionstyle.str,"SINGLESTEP"))NM->MotionStyle=7;
		else if(!strcmp(motionstyle.str,"FLY"))NM->MotionStyle=8;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=motionstyle;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMOTIONSTYLE);
	REG_AUTO(motionstyle);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDMOVEBREAK:public MDCommand{
public:
	_str movebreak;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(movebreak.str,0);
		if(NANM)
			NANM->MoveBreak=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=movebreak;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMOVEBREAK);
	REG_AUTO(movebreak);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
/*class MDMULTIWEAPON:public MDCommand{
public:
	_str multiweapon;
	void Initialize(NewMonster* NM){
		NM->Multiweapon=znew(char,strlen(multiweapon.str)+1);
		strcpy(NM->Multiweapon,multiweapon.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=direction;
		Dest+=" ";
		Dest+=height;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDMULTIWEAPON);
	REG_AUTO(multiweapon);
	REG_PARENT(MDCommand);
	ENDSAVE;
};*/
class MDNAEMNIK:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->Behavior=2;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDNAEMNIK);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDNAME:public MDCommand{
public:
	_str name;
	void Initialize(NewMonster* NM){
		NM->Name=znew(char,strlen(name.str)+1);
		strcpy(NM->Name,name.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=name;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDNAME);
	REG_AUTO(name);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDNO25:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->No25=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDNO25);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDNOFARM:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->NoFarm=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDNOFARM);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDNO_HUNGRY:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->NotHungry=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDNO_HUNGRY);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDORDER_SOUND:public MDCommand{
public:
	_str order_sound;
	void Initialize(NewMonster* NM){
		NM->OrderSoundID=SearchStr(SoundID,order_sound.str,NSounds);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=order_sound;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDORDER_SOUND);
	REG_AUTO(order_sound);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPARTICLES:public MDCommand{
public:
	_str animation;
	_str node;
	int LinkTo;
	float Phase;
	float AverageDensity;
	void Initialize(NewMonster* NM){
		NewAnimation* NA=NM->LoadNewAnimationByName(animation.str,0);
		if(NA){
			AnmParticlesSource* AP=new AnmParticlesSource;
			AP->NodeName=node;
			AP->GetParticle(NA->ModelID);
			if(AP->Particle){
				AP->Phase=Phase;
				AP->AverageDensity=AverageDensity;
				AP->LinkedTo=LinkTo;
				NA->Particles.Add(AP);
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=node;
		Dest+=" ";
		Dest+=LinkTo;
		Dest+=" ";
		Dest+=Phase;
		Dest+=" ";
		Dest+=AverageDensity;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPARTICLES);
	REG_AUTO(animation);
	REG_AUTO(node);
	REG_MEMBER(_int,LinkTo);
	REG_MEMBER(_float,Phase);
	REG_MEMBER(_float,AverageDensity);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPEASANT:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->Peasant=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPEASANT);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPEASANTABSORBER:public MDCommand{
public:
	int MaxInside;
	void Initialize(NewMonster* NM){
		NM->PeasantAbsorber=true;
		NM->MaxInside=MaxInside;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=MaxInside;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPEASANTABSORBER);
	REG_MEMBER(_int,MaxInside);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPORTBRANCH:public MDCommand{
public:
	int portbranch;
	void Initialize(NewMonster* NM){
		NM->PortBranch=portbranch;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=portbranch;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPORTBRANCH);
	REG_MEMBER(_int,portbranch);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class PORTION:public BaseClass{
public:
	_str resource;
	int MaxResPortion;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" ";
		Dest+=MaxResPortion;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(PORTION);
	REG_ENUM(_strindex,resource,RESOURCES);
	REG_MEMBER(_int,MaxResPortion);
	ENDSAVE;
};
class MDPORTION:public MDCommand{
public:
	ClonesArray<PORTION> portion;
	void Initialize(NewMonster* NM){
		int N=portion.GetAmount();
		for(int i=0;i<N;i++){
			byte ms=0;
			ms=GetResID(portion[i]->resource.str);
			NM->MaxResPortion[ms]=portion[i]->MaxResPortion;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=portion.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>5)N1=5;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=portion[i]->resource.str;
			Dest+=" ";
			Dest+=portion[i]->MaxResPortion;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDPORTION);
	REG_CLASS(PORTION);
	REG_AUTO(portion);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPORT:public MDCommand{
public:
	_str port;
	int BuiDist;
	int MaxPortDist;
	void Initialize(NewMonster* NM){
		NM->BuiAnm=GetNewAnimationByName(port.str);
		if(NM->BuiAnm){
			NM->BuiDist=BuiDist;
			NM->MaxPortDist=MaxPortDist;
			NM->Port=true;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=port;
		Dest+=" ";
		Dest+=BuiDist;
		Dest+=" ";
		Dest+=MaxPortDist;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPORT);
	REG_AUTO(port);
	REG_MEMBER(_int,BuiDist);
	REG_MEMBER(_int,MaxPortDist);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class PRICE:public BaseClass{
public:
	_str resource;
	int NeedRes;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" ";
		Dest+=NeedRes;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(PRICE);
	REG_ENUM(_strindex,resource,RESOURCES);
	REG_MEMBER(_int,NeedRes);
	ENDSAVE;
};
class MDPRICE:public MDCommand{
public:
	ClonesArray<PRICE> price;
	void Initialize(NewMonster* NM){
		int N=price.GetAmount();
		for(int i=0;i<N;i++){
			int r=GetResByName(price[i]->resource.str);
			if(r>=0 && r<100)NM->NeedRes[r]=price[i]->NeedRes;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=price.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=price[i]->resource;
			Dest+=" ";
			Dest+=price[i]->NeedRes;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDPRICE);
	REG_CLASS(PRICE);
	REG_AUTO(price);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPRIEST:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->Priest=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPRIEST);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class PRODUCER:public BaseClass{
public:
	_str resource;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(PRODUCER);
	REG_ENUM(_strindex,resource,RESOURCES);
	ENDSAVE;
};
class MDPRODUCER:public MDCommand{
public:
	ClonesArray<PRODUCER> producer;
	int FreeAdd;
	int PeasantAdd;
	void Initialize(NewMonster* NM){
		int N=producer.GetAmount();
		NM->Producer=true;
		NM->ProdType=0;
		for(int i=0;i<N;i++){
		    if(!strcmp(producer[i]->resource.str,"WOOD" ))NM->ProdType|=1;else
			if(!strcmp(producer[i]->resource.str,"GOLD" ))NM->ProdType|=2;else
			if(!strcmp(producer[i]->resource.str,"STONE"))NM->ProdType|=4;else
			if(!strcmp(producer[i]->resource.str,"FOOD" ))NM->ProdType|=8;else
			if(!strcmp(producer[i]->resource.str,"IRON" ))NM->ProdType|=16;else
			if(!strcmp(producer[i]->resource.str,"COAL" ))NM->ProdType|=32;
	    }
		NM->FreeAdd=FreeAdd;
		NM->PeasantAdd=PeasantAdd;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=producer.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" ";
			Dest+=producer[i]->resource;
		}
		Dest+="...{CG}]{CW} ";
		Dest+=FreeAdd;
		Dest+=" ";
		Dest+=PeasantAdd;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPRODUCER);
	REG_CLASS(PRODUCER);
	REG_AUTO(producer);
	REG_MEMBER(_int,FreeAdd);
	REG_MEMBER(_int,PeasantAdd);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class PROTECTION:public BaseClass{
public:
	_str Weapon;
	int Protection;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=Weapon;
		Dest+=" ";
		Dest+=Protection;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(PROTECTION);
	REG_AUTO(Weapon);
	REG_MEMBER(_int,Protection);
	ENDSAVE;
};
class MDPROTECTION:public MDCommand{
public:
	ClonesArray<PROTECTION> protection;
	void Initialize(NewMonster* NM){
		int N=protection.GetAmount();
		for(int i=0;i<N;i++){
			int zz2=GetWeaponType(protection[i]->Weapon.str);
			if(zz2!=-1)
				NM->Protection[zz2]=protection[i]->Protection;//div(p2*255,100).quot;
		};
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=protection.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=protection[i]->Weapon;
			Dest+=" ";
			Dest+=protection[i]->Protection;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDPROTECTION);
	REG_CLASS(PROTECTION);
	REG_AUTO(protection);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDPSIXOZ:public MDCommand{
public:
	int psixoz;
	void Initialize(NewMonster* NM){
		NM->Psixoz=psixoz;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=psixoz;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDPSIXOZ);
	REG_MEMBER(_int,psixoz);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
/*class MDRANGE:public MDCommand{
public:
	_str range;
	void Initialize(NewMonster* NM){
		NM->Range=znew(char,strlen(range.str)+1);
		strcpy(NM->Range,range.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=direction;
		Dest+=" ";
		Dest+=height;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDRANGE);
	REG_AUTO(range);
	REG_PARENT(MDCommand);
	ENDSAVE;
};*/
class RASTRATA_NA_VISTREL2:public BaseClass{
public:
	_str resource;
	int rastrata;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" ";
		Dest+=rastrata;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(RASTRATA_NA_VISTREL2);
	REG_ENUM(_strindex,resource,RESOURCES);
	REG_MEMBER(_int,rastrata);
	ENDSAVE;
};
class MDRASTRATA_NA_VISTREL2:public MDCommand{
public:
	int ResAttType;
	int ResAttType1;
	ClonesArray<RASTRATA_NA_VISTREL2> rastrata_na_vistrel2;
	void Initialize(NewMonster* NM){
		NM->ResAttType=ResAttType;
		NM->ResAttType1=ResAttType1;
		int N=rastrata_na_vistrel2.GetAmount();
		if(NM->ShotRes)free(NM->ShotRes);
		NM->ShotRes=znew(word,N*2);
		NM->NShotRes=N;
		for(int i=0;i<N;i++){
			int p3=GetResByName(rastrata_na_vistrel2[i]->resource.str);
			if(p3>=0 && p3<100){
				NM->ShotRes[i*2]=p3;
				NM->ShotRes[i*2+1]=rastrata_na_vistrel2[i]->rastrata;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ResAttType;
		Dest+=" ";
		Dest+=ResAttType1;
		Dest+=" ";
		int N=rastrata_na_vistrel2.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=rastrata_na_vistrel2[i]->resource;
			Dest+=" ";
			Dest+=rastrata_na_vistrel2[i]->rastrata;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDRASTRATA_NA_VISTREL2);
	REG_MEMBER(_int,ResAttType);
	REG_MEMBER(_int,ResAttType1);
	REG_CLASS(RASTRATA_NA_VISTREL2);
	REG_AUTO(rastrata_na_vistrel2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDRAZBROS:public MDCommand{
public:
	int razbros;
	void Initialize(NewMonster* NM){
		NM->Razbros=razbros;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=razbros;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDRAZBROS);
	REG_MEMBER(_int,razbros);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDRECTANGLE:public MDCommand{
public:
	int RectDx;
	int RectDy;
	int RectLx;
	int RectLy;
	void Initialize(NewMonster* NM){
		NM->RectDx=RectDx;
		NM->RectDy=RectDy;
		NM->RectLx=RectLx;
		NM->RectLy=RectLy;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=RectDx;
		Dest+=" ";
		Dest+=RectDy;
		Dest+=" ";
		Dest+=RectLx;
		Dest+=" ";
		Dest+=RectLy;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDRECTANGLE);
	REG_MEMBER(_int,RectDx);
	REG_MEMBER(_int,RectDy);
	REG_MEMBER(_int,RectLx);	
	REG_MEMBER(_int,RectLy);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
/*class MDREFLECT:public MDCommand{
public:
	_str reflect;
	void Initialize(NewMonster* NM){
		NM->Reflect=znew(char,strlen(reflect.str)+1);
		strcpy(NM->Reflect,reflect.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=direction;
		Dest+=" ";
		Dest+=height;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDREFLECT);
	REG_AUTO(reflect);
	REG_PARENT(MDCommand);
	ENDSAVE;
};*/
class MDREFLECTMODEL:public MDCommand{
public:
	_str animation;
	_str reflectmodel;
	void Initialize(NewMonster* NM){
		NewAnimation* NA=NM->LoadNewAnimationByName(animation.str,0);
		if(NA)NA->ReflectionID=IMM->GetModelID(reflectmodel.str);
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=reflectmodel;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDREFLECTMODEL);
	REG_AUTO(animation);
	REG_AUTO(reflectmodel);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDRESCONSUMER:public MDCommand{
public:
	_str resource;
	int resconsumer;
	void Initialize(NewMonster* NM){
		int p1=GetResID(resource.str);
		if(p1!=-1){
			NM->ResConsID=p1;
			NM->ResConsumer=resconsumer;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" ";
		Dest+=resconsumer;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDRESCONSUMER);
	REG_ENUM(_strindex,resource,RESOURCES);
	REG_MEMBER(_int,resconsumer);	
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class RESOURCEBASE:public BaseClass{
public:
	_str resource;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(RESOURCEBASE);
	REG_ENUM(_strindex,resource,RESOURCES);
	ENDSAVE;
};
class MDRESOURCEBASE:public MDCommand{
public:
	ClonesArray<RESOURCEBASE> resourcebase;
	void Initialize(NewMonster* NM){
		int N=resourcebase.GetAmount();
		word ms=0;
		for(int i=0;i<N;i++){
			ms|=1<<GetResByName(resourcebase[i]->resource.str);
			NM->ResConcentrator=ms;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=resourcebase.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" ";
			Dest+=resourcebase[i]->resource;
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDRESOURCEBASE);
	REG_CLASS(RESOURCEBASE);
	REG_AUTO(resourcebase);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDRESSUBST:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->ResSubst=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDRESSUBST);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDROTATE:public MDCommand{
public:
	int MinRotator;
	void Initialize(NewMonster* NM){
		NM->MinRotator=MinRotator;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=MinRotator;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDROTATE);
	REG_MEMBER(_int,MinRotator);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDRPLACESPEED:public MDCommand{
public:
	int rplacespeed;
	void Initialize(NewMonster* NM){
		NewAnimation* NA=NM->GetAnimation(anm_RotateAtPlace);
		if(NA)NA->CanBeBroken=1;
		NM->RotationAtPlaceSpeed=rplacespeed;
		NM->DiscreteRotationDirections=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=rplacespeed;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDRPLACESPEED);
	REG_MEMBER(_int,rplacespeed);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSEARCH_ENEMY_RADIUS:public MDCommand{
public:
	int search_enemy_radius;
	void Initialize(NewMonster* NM){
		NM->VisRange=search_enemy_radius<<4;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=search_enemy_radius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSEARCH_ENEMY_RADIUS);
	REG_MEMBER(_int,search_enemy_radius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSELFTRANSFORM:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->SelfTransform=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSELFTRANSFORM);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class SETACTIVEPOINT:public BaseClass{
public:
	int ActivePtX;
	int ActivePtY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ActivePtX;
		Dest+=" ";
		Dest+=ActivePtY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(SETACTIVEPOINT);
	REG_MEMBER(_int,ActivePtX);
	REG_MEMBER(_int,ActivePtY);
	ENDSAVE;
};
class MDSETACTIVEPOINT:public MDCommand{
public:
	_str animation;
	int ActiveFrame;
	_str line;
	ClonesArray<SETACTIVEPOINT> setactivepoint;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(animation.str,0);
		if(NANM){
			int L=0;
			for(int i=0;i<NANM->Rotations;i++){ 
				SETACTIVEPOINT* SA=new SETACTIVEPOINT;
				setactivepoint.Add(SA);
				SA->ActivePtX=atoi(GetWord(line.str+L,L));
				NANM->ActivePtX[i]=SA->ActivePtX;
				SA->ActivePtY=atoi(GetWord(line.str+L,L));
				NANM->ActivePtY[i]=SA->ActivePtY;
				NANM->ActiveFrame=ActiveFrame;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=ActiveFrame;
		Dest+=" ";
		int N=setactivepoint.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>5)N1=5;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=setactivepoint[i]->ActivePtX;
			Dest+=" ";
			Dest+=setactivepoint[i]->ActivePtY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDSETACTIVEPOINT);
	REG_AUTO(animation);
	REG_MEMBER(_int,ActiveFrame);
	REG_AUTO(line);
	REG_CLASS(SETACTIVEPOINT);
	REG_AUTO(setactivepoint);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSETACTIVEPOINT0:public MDCommand{
public:
	_str animation;
	int ActiveFrame;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(animation.str,0);
		if(NANM)NANM->ActiveFrame=ActiveFrame;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=ActiveFrame;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSETACTIVEPOINT0);
	REG_AUTO(animation);
	REG_MEMBER(_int,ActiveFrame);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSETANMPARAM:public MDCommand{
public:
	int NAstartDx;
	int NAstartDy;
	int NAparts;
	int NApartSize;
	void Initialize(NewMonster* NM){
		NAStartDx=NAstartDx;
		NAStartDy=NAstartDy;
		NAParts=NAparts;
		NAPartSize=NApartSize;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=NAstartDx;
		Dest+=" ";
		Dest+=NAstartDy;
		Dest+=" ";
		Dest+=NAparts;
		Dest+=" ";
		Dest+=NApartSize;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSETANMPARAM);
	REG_MEMBER(_int,NAstartDx);
	REG_MEMBER(_int,NAstartDy);
	REG_MEMBER(_int,NAparts);
	REG_MEMBER(_int,NApartSize);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSETHOTFRAME:public MDCommand{
public:
	_str animation;
	int ActiveFrame;
	_str node;
	void Initialize(NewMonster* NM){
		NewAnimation* NANM=NM->LoadNewAnimationByName(animation.str,0);
		if(NANM && NANM->AnimationID>0){
			float AT=IMM->GetAnimTime(NANM->AnimationID);
			if(NANM->NFrames)
				IMM->Animate(NANM->ModelID,NANM->AnimationID,float(ActiveFrame)*AT/NANM->NFrames);
			else 
				IMM->Animate(NANM->ModelID,NANM->AnimationID,0);
			int nd=IMM->GetNodeID(NANM->ModelID,node.str);
			if(nd!=-1){
				Matrix4D M4=IMM->GetNodeTransform(nd);
				if(NANM->AddDirection){
					Matrix4D M1;
					M1.rotation(Vector3D::oZ,float(NANM->AddDirection)*3.1415/128.0);
					M4*=M1;
				}
				NANM->ActiveFrame=ActiveFrame;
				NANM->HotRadius=int(M4.e30);
				NANM->HotHeight=int(M4.e32);;
				NANM->HotShift=int(M4.e31);
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=ActiveFrame;
		Dest+=" ";
		Dest+=node;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSETHOTFRAME);
	REG_AUTO(animation);
	REG_MEMBER(_int,ActiveFrame);
	REG_AUTO(node);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class SHOTS:public BaseClass{
public:
	int ShotPtX;
	int ShotPtY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ShotPtX;
		Dest+=" ";
		Dest+=ShotPtY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(SHOTS);
	REG_MEMBER(_int,ShotPtX);
	REG_MEMBER(_int,ShotPtY);
	ENDSAVE;
};
class MDSHOTS:public MDCommand{
public:
	ClonesArray<SHOTS> shots;
	void Initialize(NewMonster* NM){
		int N=shots.GetAmount();
		NM->NShotPt=N;
		NM->ShotPtX=znew(short,N);
		NM->ShotPtY=znew(short,N);
		NM->ShotDir=0;
		NM->ShotDir=0;
	    for(int i=0;i<N;i++){
			NM->ShotPtX[i]=shots[i]->ShotPtX;
			NM->ShotPtY[i]=shots[i]->ShotPtY;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=shots.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=shots[i]->ShotPtX;
			Dest+=" ";
			Dest+=shots[i]->ShotPtY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDSHOTS);
	REG_CLASS(SHOTS);
	REG_AUTO(shots);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSHOWDELAY:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->ShowDelay=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSHOWDELAY);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSKILLDAMAGEBONUS:public MDCommand{
public:
	int skilldamagebonus;
	void Initialize(NewMonster* NM){
		NM->SkillDamageBonus=skilldamagebonus;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=skilldamagebonus;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSKILLDAMAGEBONUS);
	REG_MEMBER(_int,skilldamagebonus);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSKILLDAMAGEMASK:public MDCommand{
public:
	int skilldamagemask;
	void Initialize(NewMonster* NM){
		NM->SkillDamageMask=skilldamagemask;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=skilldamagemask;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSKILLDAMAGEMASK);
	REG_MEMBER(_int,skilldamagemask);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSLOWDEATH:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->SlowDeath=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSLOWDEATH);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class SMOKE:public BaseClass{
public:
	int FireX;
	int FireY;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=FireX;
		Dest+=" ";
		Dest+=FireY;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(SMOKE);
	REG_MEMBER(_int,FireX);
	REG_MEMBER(_int,FireY);
	ENDSAVE;
};
class MDSMOKE:public MDCommand{
public:
	ClonesArray<SMOKE> smoke;
	void Initialize(NewMonster* NM){
		int N=smoke.GetAmount();
		NM->FireX[1]=znew(short,N);
		NM->FireY[1]=znew(short,N);
		NM->NFires[1]=N;
		for(int i=0;i<N;i++){
			NM->FireX[1][i]=smoke[i]->FireX;
			NM->FireY[1][i]=smoke[i]->FireY;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		int N=smoke.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=smoke[i]->FireX;
			Dest+=" ";
			Dest+=smoke[i]->FireY;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDSMOKE);
	REG_CLASS(SMOKE);
	REG_AUTO(smoke);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSOUND:public MDCommand{
public:
	int HotFrame;
	_str sound;
	_str fn;
	void Initialize(NewMonster* NM){
		NewAnimation* NAN=GetNewAnimationByName(sound.str);
		if(NAN){
			NAN->SoundID=SearchStr(SoundID,fn.str,NSounds);
			if(NAN->SoundID!=-1){
				NAN->HotFrame=HotFrame;
				NAN->SoundProbability=32767;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=HotFrame;
		Dest+=" ";
		Dest+=sound;
		Dest+=" ";
		Dest+=fn;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSOUND);
	REG_MEMBER(_int,HotFrame);
	REG_AUTO(sound);
	REG_AUTO(fn);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSTANDGROUND:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->CanStandGr=1;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSTANDGROUND);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSTORMFORCE:public MDCommand{
public:
	int stormforce;
	void Initialize(NewMonster* NM){
		NM->StormForce=stormforce;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=stormforce;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSTORMFORCE);
	REG_MEMBER(_int,stormforce);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSTRIKEFLYSPEED:public MDCommand{
public:
	int strikeflyspeed;
	int strikeflymaxspeed;
	void Initialize(NewMonster* NM){
		NM->StrikeFlySpeed=strikeflyspeed;
		NM->StrikeFlyMaxSpeed=strikeflymaxspeed;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=strikeflyspeed;
		Dest+=" ";
		Dest+=strikeflymaxspeed;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSTRIKEFLYSPEED);
	REG_MEMBER(_int,strikeflyspeed);
	REG_MEMBER(_int,strikeflymaxspeed);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSTRIKEFORCE:public MDCommand{
public:
	int strikeforce;
	void Initialize(NewMonster* NM){
		NM->StrikeForce=strikeforce;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=strikeforce;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSTRIKEFORCE);
	REG_MEMBER(_int,strikeforce);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSTRIKEPROBABILITY:public MDCommand{
public:
	int strikeprobability;
	void Initialize(NewMonster* NM){
		NM->StrikeProbability=strikeprobability;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=strikeprobability;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSTRIKEPROBABILITY);
	REG_MEMBER(_int,strikeprobability);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDSTRIKEROTATE:public MDCommand{
public:
	int strikerotate;
	void Initialize(NewMonster* NM){
		NM->StrikeRotate=strikerotate;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=strikerotate;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDSTRIKEROTATE);
	REG_MEMBER(_int,strikerotate);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDTAKERESSTAGES:public MDCommand{
public:
	_str resource;
	int number;
	int GoWithStage;
	int TakeResStage;
	void Initialize(NewMonster* NM){
		int r=GetResByName(resource.str);
		if(r>=0 && r<8 && number<16 && number>=0){
			if(!NM->CompxUnit){
				NM->CompxUnit=new ComplexUnitRecord;
				memset(NM->CompxUnit,0,sizeof ComplexUnitRecord);
			}
			NM->CompxUnit->GoWithStage[number]=GoWithStage;
			NM->CompxUnit->TakeResStage[number]=TakeResStage;
			NM->CompxUnit->TransformTo[number]=r;
			NM->CompxUnit->CanTakeExRes=1;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=resource;
		Dest+=" ";
		Dest+=number;
		Dest+=" ";
		Dest+=GoWithStage;
		Dest+=" ";
		Dest+=TakeResStage;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDTAKERESSTAGES);
	REG_ENUM(_strindex,resource,RESOURCES);
	REG_MEMBER(_int,number);
	REG_MEMBER(_int,GoWithStage);
	REG_MEMBER(_int,TakeResStage);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDTIMEANIMATION:public MDCommand{
public:
	_str timeanimation;
	_str model;
	int frames;
	int variation;
	void Initialize(NewMonster* NM){
		int mod=IMM->GetModelID(model.str);
		if(mod!=-1){
			NewAnimation* NAN=NM->LoadNewAnimationByName(timeanimation.str,0);
			if(NAN){
				NAN->TimeAnimationID=mod;
				NAN->TimeAnimationFrames=frames;
				NAN->TimeAnimationVariation=variation;									
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=timeanimation;
		Dest+=" ";
		Dest+=model;
		Dest+=" ";
		Dest+=frames;
		Dest+=" ";
		Dest+=variation;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDTIMEANIMATION);
	REG_AUTO(timeanimation);
	REG_AUTO(model);
	REG_MEMBER(_int,frames);
	REG_MEMBER(_int,variation);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDTIREDCHANGE:public MDCommand{
public:
	_str animation;
	int TiringChange;
	void Initialize(NewMonster* NM){
		NewAnimation* NA=NM->LoadNewAnimationByName(animation.str,0);
		if(NA)NA->TiringChange=TiringChange;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=animation;
		Dest+=" ";
		Dest+=TiringChange;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDTIREDCHANGE);
	REG_AUTO(animation);
	REG_MEMBER(_int,TiringChange);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDTORG:public MDCommand{
public:
	void Initialize(NewMonster* NM){
		NM->Rinok=true;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDTORG);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDUNITRADIUS:public MDCommand{
public:
	int unitradius;
	void Initialize(NewMonster* NM){
		NM->UnitRadius=unitradius;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=unitradius;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDUNITRADIUS);
	REG_MEMBER(_int,unitradius);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDUSAGE:public MDCommand{
public:
	_str usage;
	void Initialize(NewMonster* NM){
		if(!strcmp(usage.str,FarmIDS))NM->Usage=FarmID;
		else if(!strcmp(usage.str,MelnicaIDS)){
			NM->Usage=MelnicaID;
			NM->Ves=100;
		}else if(!strcmp(usage.str,SkladIDS)){
			NM->Usage=SkladID;
			NM->Ves=30;
		}else if(!strcmp(usage.str,TowerIDS))NM->Usage=TowerID;
		else if(!strcmp(usage.str,CenterIDS))NM->Usage=CenterID;
		else if(!strcmp(usage.str,MineIDS))NM->Usage=MineID;
		else if(!strcmp(usage.str,FieldIDS))NM->Usage=FieldID;
		else if(!strcmp(usage.str,PeasantIDS))NM->Usage=PeasantID;
		else if(!strcmp(usage.str,FastHorseIDS))NM->Usage=FastHorseID;
		else if(!strcmp(usage.str,MortiraIDS)){
			NM->Usage=MortiraID;
			NM->Artilery=true;
		}else if(!strcmp(usage.str,PushkaIDS)){
			NM->Usage=PushkaID;
			NM->Artilery=true;
		}else if(!strcmp(usage.str,MultiCannonIDS)){
			NM->Usage=MultiCannonID;
			NM->Artilery=true;
		}else if(!strcmp(usage.str,GrenaderIDS))NM->Usage=GrenaderID;
		else if(!strcmp(usage.str,HardWallIDS))NM->Usage=HardWallID;
		else if(!strcmp(usage.str,WeakWallIDS))NM->Usage=WeakWallID;
		else if(!strcmp(usage.str,LinkorIDS))NM->Usage=LinkorID;
		else if(!strcmp(usage.str,WeakIDS))NM->Usage=WeakID;
		else if(!strcmp(usage.str,FisherIDS))NM->Usage=FisherID;
		else if(!strcmp(usage.str,ArtDepoIDS))NM->Usage=ArtDepoID;
		else if(!strcmp(usage.str,SupMortIDS)){
			NM->Usage=SupMortID;
			NM->Artilery=true;
		}else if(!strcmp(usage.str,PortIDS))NM->Usage=PortID;
		else if(!strcmp(usage.str,LightInfIDS))NM->Usage=LightInfID;
		else if(!strcmp(usage.str,StrelokIDS))NM->Usage=StrelokID;
		else if(!strcmp(usage.str,HardHorceIDS))NM->Usage=HardHorceID;
		else if(!strcmp(usage.str,HorseStrelokIDS))NM->Usage=HorseStrelokID;
		else if(!strcmp(usage.str,FregatIDS))NM->Usage=FregatID;
		else if(!strcmp(usage.str,GaleraIDS))NM->Usage=GaleraID;
		else if(!strcmp(usage.str,IaxtaIDS))NM->Usage=IaxtaID;
		else if(!strcmp(usage.str,ShebekaIDS))NM->Usage=ShebekaID;
		else if(!strcmp(usage.str,ParomIDS))NM->Usage=ParomID;
		else if(!strcmp(usage.str,ArcherIDS))NM->Usage=ArcherID;
		else if(!strcmp(usage.str,DiplomatIDS))NM->Usage=DiplomatID;
		else if(!strcmp(usage.str,MentIDS))NM->Usage=MentID;
		else if(!strcmp(usage.str,EgerIDS))NM->Usage=EgerID;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=usage;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDUSAGE);
	REG_AUTO(usage);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDUSERLC:public MDCommand{
public:
	int number;
	_str userlc;
	_str stage;
	int dx;
	int dy;
	void Initialize(NewMonster* NM){
		if(number>MaxRLC)MaxRLC=number;
		UpConv(userlc.str);
		int nr=GPS.PreLoadGPImage(userlc.str);
		RLCRef[number]=nr;
		RLCdx[number]=dx;
		RLCdy[number]=dy;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=userlc;
		Dest+=" ";
		Dest+=stage;
		Dest+=" ";
		Dest+=dx;
		Dest+=" ";
		Dest+=dy;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDUSERLC);
	REG_MEMBER(_int,number);
	REG_AUTO(userlc);
	REG_AUTO(stage);
	REG_MEMBER(_int,dx);
	REG_MEMBER(_int,dy);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDVES:public MDCommand{
public:
	int ves;
	void Initialize(NewMonster* NM){
		NM->Ves=ves;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=ves;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDVES);
	REG_MEMBER(_int,ves);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDVISION:public MDCommand{
public:
	int vision;
	void Initialize(NewMonster* NM){
		if(vision>=0 && vision<=8)
			NM->VisionType=vision;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=vision;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDVISION);
	REG_MEMBER(_int,vision);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDWATERCHECKDIST:public MDCommand{
public:
	int watercheckdist1;
	int watercheckdist2;
	void Initialize(NewMonster* NM){
		NM->WaterCheckDist1=watercheckdist1;
		NM->WaterCheckDist2=watercheckdist2;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=watercheckdist1;
		Dest+=" ";
		Dest+=watercheckdist2;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDWATERCHECKDIST);
	REG_MEMBER(_int,watercheckdist1);
	REG_MEMBER(_int,watercheckdist2);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class WAVES:public BaseClass{
public:
	int wx;
	int wy;
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=wx;
		Dest+=" ";
		Dest+=wy;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(WAVES);
	REG_MEMBER(_int,wx);
	REG_MEMBER(_int,wy);
	ENDSAVE;
};
class MDWAVES:public MDCommand{
public:
	int wx0;
	int wy0;
	int WaveDZ;
	ClonesArray<WAVES> waves;
	void Initialize(NewMonster* NM){
		int N=waves.GetAmount();
		NM->WaveDZ=WaveDZ;
		NM->NWaves=N;
		NM->WavePoints=znew(short,N*2);
		for(int i=0;i<N;i++){
			NM->WavePoints[i+i]=waves[i]->wx-wx0;
			NM->WavePoints[i+i+1]=(waves[i]->wy-wy0-WaveDZ)<<1;
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=wx0;
		Dest+=" ";
		Dest+=wy0;
		Dest+=" ";
		Dest+=WaveDZ;
		Dest+=" ";
		int N=waves.GetAmount();
		Dest+=N;
		Dest+=" {CG}[{CW}";
		int N1=N;
		if(N>7)N1=7;
		for(int i=0;i<N1;i++){
			Dest+=" (";
			Dest+=waves[i]->wx;
			Dest+=" ";
			Dest+=waves[i]->wy;
			Dest+=")";
		}
		if(N>N1)Dest+="...";
		Dest+="{CG}]{C}";
		return Dest.str;
	}
	SAVE(MDWAVES);
	REG_MEMBER(_int,wx0);
	REG_MEMBER(_int,wy0);
	REG_MEMBER(_int,WaveDZ);
	REG_CLASS(WAVES);
	REG_AUTO(waves);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDWEAPON:public MDCommand{
public:
	int number;
	_str weapon;
	void Initialize(NewMonster* NM){
		if(number<NAttTypes){
			int p2=GetWeaponIndex(weapon.str);
			if(p2!=-1)
				NM->DamWeap[number]=WPLIST[p2];
			else{
				Weapon* GetWeaponWithModificator(char* Name);
				Weapon* W = GetWeaponWithModificator(weapon.str);
				NM->DamWeap[number]=W;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=weapon;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDWEAPON);
	REG_MEMBER(_int,number);
	REG_AUTO(weapon);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDWEAPONKIND:public MDCommand{
public:
	int number;
	_str weaponkind;
	void Initialize(NewMonster* NM){
		if(number<NAttTypes){
			int zz2=GetWeaponType(weaponkind.str);
			if(zz2!=-1){
				NM->WeaponKind[number]=zz2;
				if(WeaponFlags[zz2]&(8+16))NM->CanFire=1;
			}
		}
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=number;
		Dest+=" ";
		Dest+=weaponkind;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDWEAPONKIND);
	REG_MEMBER(_int,number);
	REG_AUTO(weaponkind);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDZALP:public MDCommand{
public:
	int maxzalp;
	void Initialize(NewMonster* NM){
		if(maxzalp>255)maxzalp=255;
		NM->MaxZalp=maxzalp;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=maxzalp;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDZALP);
	REG_MEMBER(_int,maxzalp);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
class MDZPOINTS:public MDCommand{
public:
	int SrcZPoint;
	int DstZPoint;
	void Initialize(NewMonster* NM){
		NM->SrcZPoint=SrcZPoint;
		NM->DstZPoint=DstZPoint;
	}
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="{CG}";
		Dest+=Local;
		Dest+="{CW} ";
		Dest+=SrcZPoint;
		Dest+=" ";
		Dest+=DstZPoint;
		Dest+=" {C}";
		return Dest.str;
	}
	SAVE(MDZPOINTS);
	REG_MEMBER(_int,SrcZPoint);
	REG_MEMBER(_int,DstZPoint);
	REG_PARENT(MDCommand);
	ENDSAVE;
};
//////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////
class mdCmdElm:public BaseClass{
public:
	virtual void GetPreview(_str& Dest){
	}
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		return 0;
	}
	SAVE(mdCmdElm);
	ENDSAVE;
};
class mdCmdAnimation:public mdCmdElm{
public:
	_str Str;
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		if(Str.str && Str.str[0]==src[0])
			return GetWord(src,xml,Str.str+1);
		return 0;
	};
	SAVE(mdCmdAnimation);
		REG_PARENT(mdCmdElm);
		REG_AUTO(Str);
	ENDSAVE;
	void GetPreview(_str& Dest){
		Dest+=Str;
		Dest+=" ";
	}
};
class mdCmdStrAndSpace:public mdCmdElm{
public:
	_str Str;
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		if(Str.str){
			int L=strlen(Str.str);
			if(!NextRealChar(src+L))
				return 0;
			if(!strncmp(Str.str,src,L))
				return L;
		}
		return 0;
	};
	SAVE(mdCmdStrAndSpace);
		REG_PARENT(mdCmdElm);
		REG_AUTO(Str);
	ENDSAVE;
	void GetPreview(_str& Dest){
		Dest+=Str;
		Dest+=" ";
	}
};
class mdCmdStr:public mdCmdElm{
public:
	_str Str;
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		if(!strcmp(Str.str,"count")){
			int ch=0;
			char word[128];
			while(src[ch]>32){
				word[ch]=src[ch];
				ch++;
			}
			if(ch){
				word[ch]=0;
				NArray=atoi(word);
			}
			return ch;
		}else if(!strcmp(Str.str,"line")){
			int ch=0;
			char line[2048];
			while(src[ch]>13){
				line[ch]=src[ch];
				ch++;
			}
			if(ch){
				line[ch]=0;
				xml.AddSubQuote(Str.str,line);
			}
			return ch;
		}
		return 0;
	};
	SAVE(mdCmdStr);
	REG_PARENT(mdCmdElm);
	REG_AUTO(Str);
	ENDSAVE;
	void GetPreview(_str& Dest){
		Dest+=Str;
	}
};
class mdCmdIntField:public mdCmdElm{
public:
	_str FieldName;
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		if(FieldName.str)
			return GetWord(src,xml,FieldName.str);
		return 0;
	};
	SAVE(mdCmdIntField);
		REG_PARENT(mdCmdElm);
		REG_AUTO(FieldName);
	ENDSAVE;
	void GetPreview(_str& Dest){
		Dest+=FieldName;
		Dest+=" ";
	}
};
class mdCmdStrField:public mdCmdElm{
public:
	_str FieldName;
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		if(FieldName.str)
			return GetWord(src,xml,FieldName.str);
		return 0;
	};
	SAVE(mdCmdStrField);
		REG_PARENT(mdCmdElm);
		REG_AUTO(FieldName);
	ENDSAVE;
	void GetPreview(_str& Dest){
		Dest+=FieldName;
		Dest+=" ";
	}
};
class mdCmdCmdArray:public mdCmdElm{
public:
	ClassArray<mdCmdElm> Elements;
	virtual int Parse(const char* src,xmlQuote& xml,int& NArray){
		int L=0;
		int L1;
		int N=Elements.GetAmount();
		for(int i=0;i<N;i++){
			L1=Elements[i]->Parse(src+L,xml,NArray);
			if(!L1) return 0;
			L+=L1;
			L+=NextRealChar(src+L);
		}
		return L;
	};
	SAVE(mdCmdCmdArray);
		REG_PARENT(mdCmdElm);
		REG_AUTO(Elements);
	ENDSAVE;
	void GetPreview(_str& Dest){
		Dest+=" N{CG}[{CW}";
		for(int i=0;i<Elements.GetAmount();i++)Elements[i]->GetPreview(Dest);
		Dest+="...{CG}]{CW} ";
	}
};
class mdCmdParser:public BaseClass{
public:
	_str ClassName;
    ClassArray<mdCmdElm> Elements;
	SAVE(mdCmdParser);
		REG_AUTO(ClassName);
		REG_AUTO(Elements);
	ENDSAVE;
	int Parse(const char* str,MDCommandsList* List,NewMonster* NM){
		if(str[0]==47)
			return NextLine(str);
		xmlQuote xml(ClassName.str);
		int N=Elements.GetAmount();
		if(!N) return 0;
		int L=0;
		int NArray=0;
		for(int j=0;j<N;j++){
			int N1=0;
			int N2=NArray;
			do{
				NArray=0;
				int L1;
				if(N2){
					char cc[64];
					sprintf(cc,"%s",ClassName.str+2);
					xmlQuote* xml2=new xmlQuote(cc);
					ConvertToLow(cc);
					xmlQuote* xml1=new xmlQuote(cc);
					L1=Elements[j]->Parse(str+L,*xml2,NArray);
					xml1->AddSubQuote(xml2);
					xml.AddSubQuote(xml1);
				}else
					L1=Elements[j]->Parse(str+L,xml,NArray);
				if(!L1)return 0;
				L+=L1;
				L+=NextRealChar(str+L);
				N1++;
			}while(N1<N2);
		}
		if(L){			
			OneClassStorage* OCS=CGARB.GetClass(ClassName.str);
			if(OCS->OneMemb){
				MDCommand* MC=(MDCommand*)OCS->OneMemb->new_element();
				ErrorPager Error;
				MC->Load(xml,MC,&Error);
				MC->Initialize(NM);
				List->Add(MC);
			}else return 0;
		}
		return L;
	};
	const char* GetThisElementView(const char* Local){
		static _str Dest;
		Dest="";
		Dest+="{CG}";
		Dest+=ClassName;
		Dest+="{CW}";
		Dest+=" {CW}";
		for(int i=0;i<Elements.GetAmount();i++)Elements[i]->GetPreview(Dest);
		Dest+=" {C}";
		return Dest.str;
	}
};
ClonesArray<mdCmdParser> mdCmdList;
void reg_md_editor(){
	REG_CLASS(mdCmdElm);
	REG_CLASS(mdCmdAnimation);
	REG_CLASS(mdCmdStrAndSpace);
	REG_CLASS(mdCmdStr);
	REG_CLASS(mdCmdIntField);
	REG_CLASS(mdCmdStrField);
	REG_CLASS(mdCmdCmdArray);
	REG_CLASS(mdCmdParser);
	AddStdEditor("mdCmdEditor",&mdCmdList,"mdCmd.xml",RCE_DEFAULT);
}
void ConvertMDtoXML(char* name,MDCommandsList* List){
	if(!name) return;
	mdCmdList.SafeReadFromFile("mdCmd.xml");
	char Fn[128];
	sprintf(Fn,"%s.md",name);	
	ResFile f1=RReset(Fn);
	if(f1==INVALID_HANDLE_VALUE) return;
	int Lf=RFileSize(f1);
	int L=0;
	int L1=0;
	char* buf=new char[Lf+1];
	RBlockRead(f1,buf,Lf);
	buf[Lf]=0;
	NewMonster* NM=new NewMonster;
	while(L<Lf){
		int N=mdCmdList.GetAmount();
		for(int i=0;i<N;i++){
            L1=mdCmdList[i]->Parse(buf+L,List,NM);
			if(L1){
				L+=L1;
				break;
			}
		}
		if(!L1){ //not found
			L1=NextLine(buf+L);
			if(!L1) break;
			char* ss=new char[L1+1];
			strncpy(ss,buf+L,L1);
			MessageBox(hwnd,ss,"DON`T FOUND...",MB_ICONWARNING|MB_OK);
			delete[](ss);
			L+=L1;
		}
	}
	delete[](buf);
}
MDCommandsList UNITLIST;
void RegisterUnitlistEditor(){
	REG_CLASS(MDCommand);
	REG_CLASS(MDCommandsList);
	REG_CLASS(MDANIMATION1);
	REG_CLASS(MDANIMATION2);
	REG_CLASS(MDANIMATION3);
	REG_CLASS(MDANIMATION4);
	REG_CLASS(MDANIMATION5);
	REG_CLASS(MDBARS3D);
	REG_CLASS(MDABILITY);
	REG_CLASS(MDADDHDIR);
	REG_CLASS(MDADDSHOTRADIUS);
	REG_CLASS(MDADD_ATTACK_RADIUS);
	REG_CLASS(MDAISHIP);
	REG_CLASS(MDANMEXT);
	REG_CLASS(MDARCHER);
	REG_CLASS(MDARMRADIUS);
	REG_CLASS(MDATTACK_ANGLES);
	REG_CLASS(MDATTACK_PAUSE);
	REG_CLASS(MDATTACK_RADIUS);
	REG_CLASS(MDATTMASK);
	REG_CLASS(MDAUTOROTATE);
	REG_CLASS(MDBIGICON);
	REG_CLASS(MDBOIDSMOVING);
	REG_CLASS(MDBORNPOINTS2);
	REG_CLASS(MDBRANDOMPOS);
	REG_CLASS(MDBRANDOMSPEED);
	REG_CLASS(MDBREAKANIMATION);
	REG_CLASS(MDBREFLECT);
	REG_CLASS(MDBUILDBAR);
	REG_CLASS(MDBUILDLOCKPOINTS);
	REG_CLASS(MDBUILDPOINTS);
	REG_CLASS(MDBUILDSTAGES);
	REG_CLASS(MDCANKILL);
	REG_CLASS(MDCANSTORM);
	REG_CLASS(MDCANTCAPTURE);
	REG_CLASS(MDCAPTURE);
	REG_CLASS(MDCHECKPOINTS);
	REG_CLASS(MDCOLORVARIATION);
	REG_CLASS(MDCOMMANDCENTER);
	REG_CLASS(MDCOMPLEXOBJECT);
	REG_CLASS(MDCONCENTRATOR2);
	REG_CLASS(MDCOSTPERCENT);
	REG_CLASS(MDDAMAGE);
	REG_CLASS(MDDAMPOINTS);
	REG_CLASS(MDDESTRUCT);
	REG_CLASS(MDDET_RADIUS);
	REG_CLASS(MDEXITPAUSE);
	REG_CLASS(MDEXPLMEDIA);
	REG_CLASS(MDEXSPRITES);
	//REG_CLASS(MDFEARFACTOR);
	REG_CLASS(MDFEARRADIUS);
	REG_CLASS(MDFEARSTART);
	REG_CLASS(MDFEARTYPE);
	REG_CLASS(MDFIRES);
	REG_CLASS(MDFLAGS);
	REG_CLASS(MDFORCE);
	REG_CLASS(MDFORMDISTSCALE);
	REG_CLASS(MDFORMLIKESHOOTERS);
	REG_CLASS(MDFREESHOTDIST);
	REG_CLASS(MDGATE);
	REG_CLASS(MDGEOMETRY);
	REG_CLASS(MDICON);
	REG_CLASS(MDINFO);
	REG_CLASS(MDINMENUICON);
	REG_CLASS(MDLIFE);
	REG_CLASS(MDLINEARSORT);
	REG_CLASS(MDLINESORT);
	REG_CLASS(MDLOCATION);
	REG_CLASS(MDLOCKPOINTS);
	REG_CLASS(MDLONGDEATH);
	REG_CLASS(MDLOWCOSTRADIUS);
	REG_CLASS(MDMATHERIAL);
	REG_CLASS(MDMEDIA);
	REG_CLASS(MDMESSAGE);
	REG_CLASS(MDMFARM);
	REG_CLASS(MDMINICON);
	REG_CLASS(MDMOREANIMATION);
	REG_CLASS(MDMOTIONSTYLE);
	REG_CLASS(MDMOVEBREAK);
	//REG_CLASS(MDMULTIWEAPON);
	REG_CLASS(MDNAEMNIK);
	REG_CLASS(MDNAME);
	REG_CLASS(MDNO25);
	REG_CLASS(MDNOFARM);
	REG_CLASS(MDNO_HUNGRY);
	REG_CLASS(MDORDER_SOUND);
	REG_CLASS(MDPARTICLES);
	REG_CLASS(MDPEASANT);
	REG_CLASS(MDPEASANTABSORBER);
	REG_CLASS(MDPORTBRANCH);
	REG_CLASS(MDPORTION);
	REG_CLASS(MDPORT);
	REG_CLASS(MDPRICE);
	REG_CLASS(MDPRIEST);
	REG_CLASS(MDPRODUCER);
	REG_CLASS(MDPROTECTION);
	REG_CLASS(MDPSIXOZ);
	//REG_CLASS(MDRANGE);
	REG_CLASS(MDRASTRATA_NA_VISTREL2);
	REG_CLASS(MDRAZBROS);
	REG_CLASS(MDRECTANGLE);
	//REG_CLASS(MDREFLECT);
	REG_CLASS(MDREFLECTMODEL);
	REG_CLASS(MDRESCONSUMER);
	REG_CLASS(MDRESOURCEBASE);
	REG_CLASS(MDRESSUBST);
	REG_CLASS(MDROTATE);
	REG_CLASS(MDRPLACESPEED);
	REG_CLASS(MDSEARCH_ENEMY_RADIUS);
	REG_CLASS(MDSELFTRANSFORM);
	REG_CLASS(MDSETACTIVEPOINT);
	REG_CLASS(MDSETACTIVEPOINT0);
	REG_CLASS(MDSETANMPARAM);
	REG_CLASS(MDSETHOTFRAME);
	REG_CLASS(MDSHOTS);
	REG_CLASS(MDSHOWDELAY);
	REG_CLASS(MDSKILLDAMAGEBONUS);
	REG_CLASS(MDSKILLDAMAGEMASK);
	REG_CLASS(MDSLOWDEATH);
	REG_CLASS(MDSMOKE);
	REG_CLASS(MDSOUND);
	REG_CLASS(MDSTANDGROUND);
	REG_CLASS(MDSTORMFORCE);
	REG_CLASS(MDSTRIKEFLYSPEED);
	REG_CLASS(MDSTRIKEFORCE);
	REG_CLASS(MDSTRIKEPROBABILITY);
	REG_CLASS(MDSTRIKEROTATE);
	REG_CLASS(MDTAKERESSTAGES);
	REG_CLASS(MDTIMEANIMATION);
	REG_CLASS(MDTIREDCHANGE);
	REG_CLASS(MDTORG);
	REG_CLASS(MDUNITRADIUS);
	REG_CLASS(MDUSAGE);
	REG_CLASS(MDUSERLC);
	REG_CLASS(MDVES);
	REG_CLASS(MDVISION);
	REG_CLASS(MDWATERCHECKDIST);
	REG_CLASS(MDWAVES);
	REG_CLASS(MDWEAPON);
	REG_CLASS(MDWEAPONKIND);
	REG_CLASS(MDZALP);
	REG_CLASS(MDZPOINTS);
	//ConvertMDtoXML("del\\example_command",&UNITLIST);
	//AddStdEditor("UnitsList",&UNITLIST,"",RCE_DEFAULT);

	/*
	ConvertMDtoXML("AllKri",&UNITLIST);
	xmlQuote xml2;
	UNITLIST.Save(xml2,&UNITLIST);
	xml2.WriteToFile("del\\mdCmdTest.xml");
	*/
	AddStdEditor("UnitsList",&UNITLIST,"del\\mdCmdTest.xml",RCE_DEFAULT);
}
void testFileDialog(){
	ceFileDialog FD;
	FD.CreateDialogEditor(100,100,500,500);
}