#include "stdheader.h"
#include "UnitAbility.h"
#include "UnitChar.h"
#include "ClassEditorsRegistration.h"

char* newstr(char* s);

void  BasicObjectChars::ConvertFromNewMonster (NewMonster* NM){
	Building=NM->Building;
	UnitMD=NM->MD_File;
	UnitID=MonNames[NM->MyIndex];
	IconFileID=NM->IconFileID;
	IconID=NM->IconID;
	Hitpoints=NM->Life;
	ProduceStages=NM->ProduceStages;
	for(int i=0;i<8;i++)Cost[i]=NM->NeedRes[i];
	for(int i=0;i<16;i++)Protection[i]=NM->Protection[i];
	MatherialMask=NM->MathMask;
	KillingMask=NM->KillMask;
	VisionRange=NM->VisionType;
	LockType=NM->LockType;
	if(NM->Ability){
		for(int i=0;i<NM->Ability->AbilityNames.GetAmount();i++){
			AblName* s=new AblName;
			s->AbilityName=(NM->Ability->AbilityNames[i])->pchar();
			Ability.Add(s);
		}
	}
	Message=NM->Message;
	LongMessage=NM->LongMessage;
	PieceName=NM->PieceName;
	MaxMana=NM->MaxMana;
	ExpForKillingThisUnit=NM->Expa;
	DontAnswerOnAttack=NM->DontAnswerOnAttack;
	DontAffectFogOfWar=NM->DontAffectFogOfWar;
	InvisibleOnMinimap=NM->InvisibleOnMinimap;
	CanBeInFocusOfFormation=NM->CanBeInFocusOfFormation;
	CanBeCaptured=NM->Capture;
	CantCapture=NM->NeverCapture;
	ShowAttackDelay=NM->ShowDelay;
	NotSelectable=NM->NotSelectable;
	RectDx=NM->RectDx;
	RectDy=NM->RectDy;
	RectLx=NM->RectLx;
	RectLy=NM->RectLy;
	DontConsumerLivingPlaces=NM->NoFarm;
	ExtraConsumingResource=NM->ResConsID;
	ExtraConsumingResourceSpeed=NM->ResConsumer;
	SelectionType=NM->selIndex;
	SelectionTypeInFormation=NM->selIndexBR;
	SelectionScaleX=NM->selScaleX;
	SelectionScaleY=NM->selScaleY;
	SelectionShift=NM->selShift;
	ColorVariation=NM->ColorVariation;
}
void  BasicObjectChars::ConvertToNewMonster   (NewMonster* NM){
	NM->Building=Building;
	NM->MD_File=newstr(UnitMD.str);
	if(MonNames[NM->MyIndex])free(MonNames[NM->MyIndex]);
	MonNames[NM->MyIndex]=newstr(UnitID.str);
	NM->IconFileID=IconFileID;
	NM->IconID=IconID;
	NM->Life=Hitpoints;
	NM->ProduceStages=ProduceStages;
	for(int i=0;i<8;i++)NM->NeedRes[i]=Cost[i];
	for(int i=0;i<16;i++)NM->Protection[i]=Protection[i];
	NM->MathMask=MatherialMask;
	NM->KillMask=KillingMask;
	NM->VisionType=VisionRange;
	NM->LockType=LockType;
	if(Ability.GetAmount()){
		NM->Ability=new MonsterAbility;
		for(int i=0;i<Ability.GetAmount();i++){
			_str* s=new _str;
			*s=Ability[i]->AbilityName;
			NM->Ability->AbilityNames.Add(s);
		}
	}
	NM->Message=newstr(Message.str);
	NM->LongMessage=newstr(LongMessage.str);
	NM->PieceName=newstr(PieceName.str);
	NM->MaxMana=MaxMana;
	NM->Expa=ExpForKillingThisUnit;
	NM->DontAnswerOnAttack=DontAnswerOnAttack;
	NM->DontAffectFogOfWar=DontAffectFogOfWar;
	NM->InvisibleOnMinimap=InvisibleOnMinimap;
	NM->CanBeInFocusOfFormation=CanBeInFocusOfFormation;
	NM->NotSelectable=NotSelectable;
	NM->Capture=CanBeCaptured;
	NM->NeverCapture=CantCapture;
	NM->ShowDelay=ShowAttackDelay;
	NM->RectDx=RectDx;
	NM->RectDy=RectDy;
	NM->RectLx=RectLx;
	NM->RectLy=RectLy;
	NM->NoFarm=DontConsumerLivingPlaces;
	NM->ResConsID=ExtraConsumingResource;
	NM->ResConsumer=ExtraConsumingResourceSpeed;
	NM->selIndex=SelectionType;
	NM->selIndexBR=SelectionTypeInFormation;
	NM->selScaleX=SelectionScaleX;
	NM->selScaleY=SelectionScaleY;
	NM->selShift=SelectionShift;
	NM->ColorVariation=ColorVariation;
}
void BasicUnitChars::ConvertFromNewMonster(NewMonster* NM){	
	Razbros=NM->Razbros;
	for(int i=0;i<NM->Animations.ANM.GetAmount();i++){
		NewAnimation* NA=new NewAnimation;
		NM->Animations.ANM[i]->Copy(NA,false);
		Animations.Add(NA);
	}	
	int na=0;
	for(int i=0;i<NAttTypes;i++){
		if(NM->MaxDamage[i]||NM->AttackRadius1[i]||NM->AttackRadius2[i]||NM->DamWeap[i])na=i+1;			
	}
	Enumerator* WE=ENUM.Get("WEAPONS");
	for(int i=0;i<na;i++){
		OneAttStateInfo* AT=new OneAttStateInfo;
		AT->MinAttackRadius=NM->AttackRadius1[i];
		AT->MaxAttackRadius=NM->AttackRadius2[i];
		AT->MinDetRadius=NM->DetRadius1[i];
		AT->MaxDetRadius=NM->DetRadius2[i];		
		AT->Weapon=WE->Get((DWORD)NM->DamWeap);
		AT->MotionRate=NM->Rate[i];
		AT->AttackPause=NM->AttackPause[i];
		AT->Damage=NM->MaxDamage[i];
		AT->WeaponKind=NM->WeaponKind[i];
		AT->DamageDecrementRadius=NM->DamageDecr[i];
		AT->AttackMask=NM->AttackMask[i];
		AT->FearType=NM->FearType[i];
		AT->FearRadius=NM->FearRadius[i];
		AT->NoPausedAttack=(NM->NoWaitMask&(1<<i))!=0;
		AttackTypes.Add(AT);
	}		
	SrcZPoint=NM->SrcZPoint;
	DstZPoint=NM->DstZPoint;
	RedRadius=NM->VisibleRadius1;
	YellowRadius=NM->VisibleRadius2;	
}
void BasicUnitChars::ConvertToNewMonster(NewMonster* NM){	
	BasicObjectChars::ConvertToNewMonster(NM);
	NM->Razbros=Razbros;
	for(int i=0;i<Animations.GetAmount();i++){
		NewAnimation* N=new NewAnimation;
		Animations[i]->Copy(N,false);		
		NM->Animations.Add(N,N->Code);
	}	
	Enumerator* WE=ENUM.Get("WEAPONS");
	NM->NoWaitMask=0;
	for(int i=0;i<AttackTypes.GetAmount();i++){
		OneAttStateInfo* AT=AttackTypes[i];
		NM->AttackRadius1[i]=AT->MinAttackRadius;
		NM->AttackRadius2[i]=AT->MaxAttackRadius;
		NM->DetRadius1[i]=AT->MinDetRadius;
		NM->DetRadius2[i]=AT->MaxDetRadius;
		NM->DamWeap[i]=NULL;
		DWORD W=WE->Get(AT->Weapon.str?AT->Weapon.str:"");
		if(W!=0xFFFFFFFF)NM->DamWeap[i]=(Weapon*)W;
		else NM->DamWeap[i]=NULL;
		NM->Rate[i]=AT->MotionRate;
		NM->AttackPause[i]=AT->AttackPause;
		NM->MaxDamage[i]=AT->Damage;
		NM->WeaponKind[i]=AT->WeaponKind;
		NM->DamageDecr[i]=AT->DamageDecrementRadius;
		NM->AttackMask[i]=AT->AttackMask;
		NM->FearType[i]=AT->FearType;
		NM->FearRadius[i]=AT->FearRadius;		
		if(AT->NoPausedAttack)NM->NoWaitMask|=1<<i;
	}		
	NM->SrcZPoint=SrcZPoint;
	NM->DstZPoint=DstZPoint;
	NM->VisibleRadius1=RedRadius;
	NM->VisibleRadius2=YellowRadius;	
}
void UnitChars::ConvertFromNewMonster (NewMonster* NM){
	Building=false;
	MotionStyle=NM->MotionStyle;
	UnitSpeed=NM->MotionDist;
	UnitRadius=NM->Radius2>>4;
	UnitRadiusForWeapon=NM->UnitRadius;
	RotationSpeed=NM->MinRotator;
	StartFlyHeight=NM->StartFlyHeight;
	FlyHeight=NM->FlyHeight;
	Officer=NM->Officer;
	Drummer=NM->Baraban;
	Peasant=NM->Peasant;
	Transport=NM->Transport;
	Priest=NM->Priest;
	Shaman=NM->Shaman;
	BornBehindBuilding=NM->BornBehindBuilding;
	DontRotateOnDeath=NM->DontRotateOnDeath;	
	DontStuckInEnemy=NM->DontStuckInEnemy;
	NikakixMam=NM->NikakixMam;
	HighUnit=NM->HighUnit;
	Animal=NM->Animal;
	UnitCanShoot=NM->CanShoot;
	RadiusOfArmAttack=NM->ArmRadius;
	SpeedScale=(NM->SpeedScale*100)/256;
	SpeedScaleOnTrees=(NM->SpeedScaleOnTrees*100)/256;
	CanSitInFormation=NM->SitInFormations;
	DontTransformToChargeState=NM->DontTransformToChargeState;
}
void UnitChars::ConvertToNewMonster (NewMonster* NM){	
	BasicUnitChars::ConvertToNewMonster(NM);
	NM->Building=false;
	NM->MotionStyle=MotionStyle;
	NM->MotionDist=UnitSpeed;
	NM->Radius2=UnitRadius<<4;
	NM->UnitRadius=UnitRadiusForWeapon;
	NM->MinRotator=RotationSpeed;
	NM->StartFlyHeight=StartFlyHeight;
	NM->FlyHeight=FlyHeight;
	NM->Officer=Officer;
	NM->Baraban=Drummer;
	NM->Peasant=Peasant;
	NM->Transport=Transport;
	NM->Priest=Priest;
	NM->Shaman=Shaman;
	NM->BornBehindBuilding=BornBehindBuilding;
	NM->DontRotateOnDeath=DontRotateOnDeath;	
	NM->DontStuckInEnemy=DontStuckInEnemy;
	NM->NikakixMam=NikakixMam;
	NM->HighUnit=HighUnit;
	NM->Animal=Animal;
	NM->CanShoot=UnitCanShoot;
	NM->ArmRadius=RadiusOfArmAttack;
	NM->SpeedScale=(SpeedScale*256)/100;
	NM->SpeedScaleOnTrees=(SpeedScaleOnTrees*256)/100;
	NM->SitInFormations=CanSitInFormation;
	NM->DontTransformToChargeState=DontTransformToChargeState;
}
void BuildingChars::ConvertFromNewMonster (NewMonster* NM){
	PictureCenterX=-NM->PicDx;
	PictureCenterY=-NM->PicDy;
	Building=true;
	Lockpoints="";
	Lockpoints.print("%d  ",NM->NLockPt);
	for(int i=0;i<NM->NLockPt;i++)Lockpoints.print("%d %d ",NM->LockX[i],NM->LockY[i]);

	LockpointsDuringBuildStages="";
	LockpointsDuringBuildStages.print("%d  ",NM->NBLockPt);
	for(int i=0;i<NM->NBLockPt;i++)LockpointsDuringBuildStages.print("%d %d ",NM->BLockX[i],NM->BLockY[i]);

	CheckPoints="";
	CheckPoints.print("%d  ",NM->NCheckPt);
	for(int i=0;i<NM->NCheckPt;i++)CheckPoints.print("%d %d ",NM->CheckX[i],NM->CheckY[i]);		
	
	BuildPoints="";
	BuildPoints.print("%d  ",NM->BuildPtX.GetAmount());
	for(int i=0;i<NM->BuildPtX.GetAmount();i++)BuildPoints.print("%d %d ",NM->BuildPtX[i],NM->BuildPtY[i]);		

	ComingInPoints="";
	ComingInPoints.print("%d  ",NM->ConcPtX.GetAmount());
	for(int i=0;i<NM->ConcPtX.GetAmount();i++)ComingInPoints.print("%d %d ",NM->ConcPtX[i],NM->ConcPtY[i]);		

	PositionsOfUnits="";
	PositionsOfUnits.print("%d  ",NM->PosPtX.GetAmount());
	for(int i=0;i<NM->PosPtX.GetAmount();i++)PositionsOfUnits.print("%d %d ",NM->PosPtX[i],NM->PosPtY[i]);	

	GoingOutPoins="";
	GoingOutPoins.print("%d  ",NM->BornPtX.GetAmount());
	for(int i=0;i<NM->BornPtX.GetAmount();i++)GoingOutPoins2.print("%d %d ",NM->BornPtX[i],NM->BornPtY[i]);

	GoingOutPoins2="";
	GoingOutPoins2.print("%d  ",NM->CraftPtX.GetAmount());
	for(int i=0;i<NM->CraftPtX.GetAmount();i++)GoingOutPoins2.print("%d %d ",NM->CraftPtX[i],NM->CraftPtY[i]);

	SmokePoints="";
	SmokePoints.print("%d  ",NM->NFires[0]);
	for(int i=0;i<NM->NFires[0];i++)SmokePoints.print("%d %d ",NM->FireX[0][i],NM->FireY[0][i]);

	FirePoints="";
	FirePoints.print("%d  ",NM->NFires[1]);
	for(int i=0;i<NM->NFires[1];i++)FirePoints.print("%d %d ",NM->FireX[1][i],NM->FireY[1][i]);

	//NM->MultiWp.Copy(&MultiWp);
	
	MineRadius=NM->MineRadius;
	MineDamage=NM->MineDamage;
	BuildNearBuildingRadius=NM->BuildNearBuildingRadius;
	UnitsCanEnter=NM->UnitAbsorber;
	PeasantsCanEnter=NM->PeasantAbsorber;
	HighUnitCantEnter=NM->HighUnitCantEnter;
	CanBeUsedLikeStorage=NM->Producer;
	StorageMask=NM->ProdType;
	Port=NM->Port;
	Wall=NM->Wall;
	Farm=NM->Farm;
	SpriteObject=NM->SpriteObject;
	Market=NM->Rinok;
	CommandCenter=NM->CommandCenter;
	GlobalCommandCenter=NM->GlobalCommandCenter;	
}
int nextint(char** s){
	if((*s)[0]==' '){
		while((*s)[0]==' ')(*s)++;
	}
    int v=atoi(*s);
	if((*s)[0]!=0){
		while( (*s)[0]!=' ' && (*s)[0]!=0)(*s)++;
	}
    return v;
}
void ReadXY(_str& s,word& N,byte** x,byte** y){
	if(s.str&&s.str[0]){
		char* str=s.str;
        int n=nextint(&str);
		*x=znew(byte,n);
		*y=znew(byte,n);
		for(int i=0;i<n;i++){
            (*x)[i]=nextint(&str);
			(*y)[i]=nextint(&str);
		}
	}
}
void ReadXY(_str& s,short& N,short** x,short** y){
	if(s.str&&s.str[0]){
		char* str=s.str;
		int n=nextint(&str);
		*x=znew(short,n);
		*y=znew(short,n);
		for(int i=0;i<n;i++){
			(*x)[i]=nextint(&str);
			(*y)[i]=nextint(&str);
		}
	}
}
void ReadXY(_str& s,DynArray<short>* x,DynArray<short>* y){
	if(s.str&&s.str[0]){
		char* str=s.str;
		int n=nextint(&str);
		for(int i=0;i<n;i++){
			(*x).Add(nextint(&str));
			(*y).Add(nextint(&str));
		}
	}
}
void BuildingChars::ConvertToNewMonster (NewMonster* NM){
	BasicUnitChars::ConvertToNewMonster(NM);
	NM->Building=true;
	NM->PicDx=-PictureCenterX;
	NM->PicDy=-PictureCenterY;
	ReadXY(Lockpoints,NM->NLockPt,&NM->LockX,&NM->LockY);
	ReadXY(LockpointsDuringBuildStages,NM->NBLockPt,&NM->BLockX,&NM->BLockY);
	ReadXY(CheckPoints,NM->NCheckPt,&NM->CheckX,&NM->CheckY);

	ReadXY(BuildPoints,&NM->BuildPtX,&NM->BuildPtY);
	ReadXY(ComingInPoints,&NM->ConcPtX,&NM->ConcPtY);
	ReadXY(PositionsOfUnits,&NM->PosPtX,&NM->PosPtY);
	ReadXY(GoingOutPoins,&NM->BornPtX,&NM->BornPtY);
	ReadXY(GoingOutPoins2,&NM->CraftPtX,&NM->CraftPtY);

	ReadXY(FirePoints,NM->NFires[0],&NM->FireX[0],&NM->FireY[0]);
	ReadXY(SmokePoints,NM->NFires[1],&NM->FireX[1],&NM->FireY[1]);
	//MultiWp.Copy(&NM->MultiWp);

	NM->MineRadius=MineRadius;
	NM->MineDamage=MineDamage;
	NM->BuildNearBuildingRadius=BuildNearBuildingRadius;
	NM->UnitAbsorber=UnitsCanEnter;
	NM->PeasantAbsorber=PeasantsCanEnter;
	NM->HighUnitCantEnter=HighUnitCantEnter;
	NM->Producer=CanBeUsedLikeStorage;
	NM->ProdType=StorageMask;
	NM->Port=Port;
	NM->Wall=Wall;
	NM->Farm=Farm;
	NM->SpriteObject=SpriteObject;
	NM->Rinok=Market;
	NM->CommandCenter=CommandCenter;
	NM->GlobalCommandCenter=GlobalCommandCenter;	
}
void ComplexObjectChar::ConvertToNewMonster(NewMonster* NM){
	BasicObjectChars::ConvertToNewMonster(NM);
    NM->ComplexObjIndex=ComplexObjectName;
	NM->CO_MathMask=MatherialMaskForComplexObject;
}
void ComplexObjectChar::ConvertFromNewMonster(NewMonster* NM){
	ComplexObjectName=NM->ComplexObjIndex;
	MatherialMaskForComplexObject=NM->CO_MathMask;
}
BasicObjectChars* CreateCharacterFromNewMonster(NewMonster* NM){
	BasicObjectChars* C=NULL;
	if(NM->ComplexObjIndex!=0xFFFF){
		ComplexObjectChar* COC=new ComplexObjectChar;
		COC->BasicObjectChars::ConvertFromNewMonster(NM);
		COC->ConvertFromNewMonster(NM);
		C=COC;
	}else{
		if(NM->Building){
			BuildingChars* BC=new BuildingChars;
            BC->BasicObjectChars::ConvertFromNewMonster(NM);			
			BC->BasicUnitChars::ConvertFromNewMonster(NM);
			BC->ConvertFromNewMonster(NM);
			C=BC;
		}else{
            UnitChars* UC=new UnitChars;
			UC->BasicObjectChars::ConvertFromNewMonster(NM);
			UC->BasicUnitChars::ConvertFromNewMonster(NM);
			UC->ConvertFromNewMonster(NM);
			C=UC;
		}
	}
	return C;
}
ClassArray<BasicObjectChars> AllUnitsChars;
void InitClasses(){
	REG_CLASS(OneAttStateInfo);
	REG_CLASS(BasicObjectChars);
	REG_CLASS(BasicUnitChars);
	REG_CLASS(UnitChars);
	REG_CLASS(BuildingChars);
	REG_CLASS(ComplexObjectChar);
	REG_CLASS(AblName);
}
void CreateAllChars(){
	InitClasses();
    AllUnitsChars.Clear();
	for(int i=0;i<NNewMon;i++){
		BasicObjectChars* BOC=CreateCharacterFromNewMonster(&NewMon[i]);
		AllUnitsChars.Add(BOC);
	}
}
bool rce_CharsCallback(ClassEditor* CE,BaseClass* BC,int Options){//Options=1-init, 2-process, 3-ok pressed, 4-cancel pressed 
	if(Options==1){
		CreateAllChars();		
	}
	if(Options==3){
		AllUnitsChars.WriteToFile("allunits.xml");
	}
	return false;
}
extern NewMonster NewMon[1024];
void LoadAllUnitsFromXML(){
	InitClasses();
	AllUnitsChars.SafeReadFromFile("allunits.xml");
    NNewMon=AllUnitsChars.GetAmount();
	for(int i=0;i<NNewMon;i++){
		BasicObjectChars* BOC=AllUnitsChars[i];
		NewMonster* NM=&NewMon[i];
		NM->MyIndex=i;
        NM->InitNM(BOC->UnitMD.pchar());
		NM->MyIndex=i;
		BOC->ConvertToNewMonster(NM);
	}
}
void AddAllCharsEditor(){
	AddStdEditor("UnitsParams",&AllUnitsChars,"",RCE_DEFAULT,rce_CharsCallback);
}


