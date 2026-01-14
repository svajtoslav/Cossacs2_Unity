class OneAttStateInfo:public BaseClass{
public:
	int   MinAttackRadius;
	int   MaxAttackRadius;
	int   MinDetRadius;
	int   MaxDetRadius;
    _str  Weapon;
	int   MotionRate;
	int   AttackPause;
	bool  NoPausedAttack;
	int   Damage;
	int   WeaponKind;
	int   DamageDecrementRadius;	
	DWORD AttackMask;
	int   FearType;
    int   FearRadius;
	int   ShotCost[8];

	SAVE(OneAttStateInfo);
		REG_MEMBER(_int,MinAttackRadius);
		REG_MEMBER(_int,MaxAttackRadius);
		REG_MEMBER(_int,MinDetRadius);
		REG_MEMBER(_int,MaxDetRadius);
		REG_ENUM(_strindex,Weapon,WEAPONS);
		REG_MEMBER(_int,MotionRate);
		REG_MEMBER(_int,AttackPause);
		REG_MEMBER(_bool,NoPausedAttack);
		REG_MEMBER(_int,Damage);
		REG_MEMBER(_int,WeaponKind);
		REG_MEMBER(_int,DamageDecrementRadius);
		REG_MEMBER(_DWORD,AttackMask);
		REG_MEMBER(_int,FearType);
		REG_MEMBER(_int,FearRadius);		
		REG_MEMBER_EX(_int,ShotCost[0],ShotRes0_Cost);
		REG_MEMBER_EX(_int,ShotCost[1],ShotRes1_Cost);
		REG_MEMBER_EX(_int,ShotCost[2],ShotRes2_Cost);
		REG_MEMBER_EX(_int,ShotCost[3],ShotRes3_Cost);
		REG_MEMBER_EX(_int,ShotCost[4],ShotRes4_Cost);
		REG_MEMBER_EX(_int,ShotCost[5],ShotRes5_Cost);
	ENDSAVE;
};
class AblName:public BaseClass{
public:
	_str AbilityName;
	SAVE(AblName);
		REG_ENUM(_strindex,AbilityName,ABILKI);
	ENDSAVE;
	bool ForceSimplification(){return true;}
};
class BasicObjectChars:public BaseClass{
public:
	//internal, not for registration
	bool						Building;
	//for registration
	_str   						UnitID;
	_str						UnitMD;
	short  						IconFileID;
	int    						IconID;
	int    						Hitpoints;
	int							ProduceStages;
	int                         Cost[8];
	int							Protection[16];
	DWORD 						MatherialMask;
	DWORD 						KillingMask;
	int   						VisionRange;
	int   						LockType;
	_str						Message;
	_str						LongMessage;
	_str						PieceName;
	int							MaxMana;
	int							ExpForKillingThisUnit;
	bool						DontAnswerOnAttack;
	bool						DontAffectFogOfWar;
	bool						InvisibleOnMinimap;
	bool						CanBeInFocusOfFormation;
	bool						NotSelectable;	
	bool  						CanBeCaptured;
	bool  						CantCapture;
	bool						ShowAttackDelay;
	bool  						DontConsumeFood;
	bool						DontConsumerLivingPlaces;
	int							ExtraConsumingResource;
	int							ExtraConsumingResourceSpeed;
	ClonesArray<AblName>		Ability;
	int							RectDx;
	int							RectDy;
	int							RectLx;
	int							RectLy;
	int							SelectionType;
	int							SelectionTypeInFormation;
	float						SelectionScaleX;
	float						SelectionScaleY;
	int							SelectionShift;
	int							ColorVariation;

	const char* GetThisElementView(const char* x){
		char* s=GetGlobalBuffer();
		sprintf(s,"%s: %s",x,UnitID.str);
		return s;
	}    
	virtual void  ConvertToNewMonster   (NewMonster* NM);
	virtual void  ConvertFromNewMonster (NewMonster* NM);
	SAVE(BasicObjectChars);
		REG_AUTO(UnitID);
		REG_AUTO(UnitMD);		
		REG_AUTO(Message);
		REG_AUTO(LongMessage);
		REG_AUTO(PieceName);
		REG_SPRITE(IconID,IconFileID);
		REG_MEMBER(_int,Hitpoints);
		REG_MEMBER(_int,ProduceStages);
		REG_MEMBER(_int,MaxMana);
		REG_MEMBER(_int,ExpForKillingThisUnit);	
		REG_AUTO(Ability);
		REG_MEMBER(_int,RectDx);
		REG_MEMBER(_int,RectDy);
		REG_MEMBER(_int,RectLx);
		REG_MEMBER(_int,RectLy);

		REG_MEMBER_EX(_int,Cost[0],Res0_Cost);
		REG_MEMBER_EX(_int,Cost[1],Res1_Cost);
		REG_MEMBER_EX(_int,Cost[2],Res2_Cost);
		REG_MEMBER_EX(_int,Cost[3],Res3_Cost);
		REG_MEMBER_EX(_int,Cost[4],Res4_Cost);
		REG_MEMBER_EX(_int,Cost[5],Res5_Cost);

		REG_MEMBER_EX(_int,Protection[0],Protection0);
		REG_MEMBER_EX(_int,Protection[1],Protection1);
		REG_MEMBER_EX(_int,Protection[2],Protection2);
		REG_MEMBER_EX(_int,Protection[3],Protection3);
		REG_MEMBER_EX(_int,Protection[4],Protection4);
		REG_MEMBER_EX(_int,Protection[5],Protection5);

		REG_MEMBER(_DWORD,MatherialMask);
		REG_MEMBER(_DWORD,KillingMask);
		REG_MEMBER(_int,VisionRange);
		REG_ENUM(_index,LockType,LOCKTYPE);				

		REG_MEMBER(_bool,DontAnswerOnAttack);
		REG_MEMBER(_bool,DontAffectFogOfWar);
		REG_MEMBER(_bool,InvisibleOnMinimap);
		REG_MEMBER(_bool,CanBeInFocusOfFormation);
		REG_MEMBER(_bool,NotSelectable);
		REG_MEMBER(_bool,CanBeCaptured);
		REG_MEMBER(_bool,CantCapture);
		REG_MEMBER(_bool,ShowAttackDelay);
		REG_MEMBER(_bool,DontConsumeFood);
		REG_MEMBER(_bool,DontConsumerLivingPlaces);
		REG_ENUM(_index,ExtraConsumingResource,RESOURCES);
		REG_MEMBER(_int,ExtraConsumingResourceSpeed);
		REG_ENUM(_index,SelectionType,TEXTURE_CURSOR_TYPES);
		REG_ENUM(_index,SelectionTypeInFormation,TEXTURE_CURSOR_TYPES);
		REG_MEMBER(_float,SelectionScaleX);
		REG_MEMBER(_float,SelectionScaleY);
		REG_MEMBER(_int,SelectionShift);
		REG_MEMBER(_int,ColorVariation);
	ENDSAVE;
};
class BasicUnitChars:public BasicObjectChars{
public:	
	
	int							Razbros;
	ClonesArray<NewAnimation> 	Animations;
	ClonesArray<OneAttStateInfo> AttackTypes;	
	int							SrcZPoint;
	int							DstZPoint;
	int							RedRadius;
	int							YellowRadius;		
	virtual void  ConvertToNewMonster   (NewMonster* NM);
	virtual void  ConvertFromNewMonster (NewMonster* NM);
	SAVE(BasicUnitChars);	
		REG_PARENT(BasicObjectChars);
		REG_MEMBER(_int,Razbros);
		REG_AUTO(Animations);
		REG_AUTO(AttackTypes);		
		REG_MEMBER(_int,SrcZPoint);
		REG_MEMBER(_int,DstZPoint);
		REG_MEMBER(_int,RedRadius);
		REG_MEMBER(_int,YellowRadius);				
	ENDSAVE;
};
class UnitChars:public BasicUnitChars{
public:
	int							MotionStyle;
	int							UnitRadius;
	int							UnitRadiusForWeapon;
	int    						UnitSpeed;
	int							RotationSpeed;
	int							StartFlyHeight;
	int							FlyHeight;
	//flags
	bool						Officer;
	bool						Drummer;
	bool						Peasant;
	bool						Transport;
	bool						Priest;
	bool						Shaman;
	bool						BornBehindBuilding;
	bool						DontRotateOnDeath;	
	bool						DontStuckInEnemy;
	bool						NikakixMam;
	bool						HighUnit;
	bool						Animal;
	bool						Cossacks2AttackStyle;
	bool						Cossacks2RechargeStyle;
	bool						UnitCanShoot;
	bool						CanSitInFormation;
	bool						DontTransformToChargeState;
	int							RadiusOfArmAttack;
	int							SpeedScale;
	int							SpeedScaleOnTrees;
	
	virtual void  ConvertToNewMonster   (NewMonster* NM);
	virtual void  ConvertFromNewMonster (NewMonster* NM);

	SAVE(UnitChars);
		REG_PARENT(BasicUnitChars);
		REG_BASE(BasicObjectChars);
		REG_ENUM(_index,MotionStyle,MOTIONSTYLE);
		REG_MEMBER(_int,UnitSpeed);
		REG_MEMBER(_int,UnitRadius);
		REG_MEMBER(_int,UnitRadiusForWeapon);
		REG_MEMBER(_int,RotationSpeed);
		REG_MEMBER(_int,StartFlyHeight);
		REG_MEMBER(_int,FlyHeight);
		//flags
		REG_MEMBER(_bool,Officer);
		REG_MEMBER(_bool,Drummer);
		REG_MEMBER(_bool,Peasant);
		REG_MEMBER(_bool,Transport);
		REG_MEMBER(_bool,Priest);
		REG_MEMBER(_bool,Shaman);
		REG_MEMBER(_bool,BornBehindBuilding);
		REG_MEMBER(_bool,DontRotateOnDeath);	
		REG_MEMBER(_bool,DontStuckInEnemy);
		REG_MEMBER(_bool,NikakixMam);
		REG_MEMBER(_bool,HighUnit);
		REG_MEMBER(_bool,Animal);
		REG_MEMBER(_bool,Cossacks2AttackStyle);
		REG_MEMBER(_bool,Cossacks2RechargeStyle);
		REG_MEMBER(_bool,UnitCanShoot);
		REG_MEMBER(_bool,CanSitInFormation);
		REG_MEMBER(_bool,DontTransformToChargeState);
		REG_MEMBER(_int,RadiusOfArmAttack);
		REG_MEMBER(_int,SpeedScale);
		REG_MEMBER(_int,SpeedScaleOnTrees);

	ENDSAVE;
};
class BuildingChars:public BasicUnitChars{
public:
	int							PictureCenterX;
	int							PictureCenterY;
	//lockpoints
	_str						Lockpoints;
	_str						LockpointsDuringBuildStages;
	_str						CheckPoints;
	//other "points"
	_str						BuildPoints;
	_str						ComingInPoints;
	_str						PositionsOfUnits;
	_str						GoingOutPoins;
	_str						GoingOutPoins2;
	_str						FirePoints;
	_str						SmokePoints;
	ClassArray<ComplexFireSource> MultiWp;     
	int							MineRadius;
	int	 						MineDamage;
	int 						BuildNearBuildingRadius;
	bool						UnitsCanEnter;
	bool						PeasantsCanEnter;
    bool                        CanBeUsedLikeStorage;
	DWORD                       StorageMask;
    bool						Port;
	bool						Wall;
	bool						Farm;
	bool						SpriteObject;
	bool						Market;
	bool						CommandCenter;
	bool						GlobalCommandCenter;
	bool						HighUnitCantEnter;
	virtual void  ConvertToNewMonster   (NewMonster* NM);
	virtual void  ConvertFromNewMonster (NewMonster* NM);

	SAVE(BuildingChars);
		REG_PARENT(BasicUnitChars);
		REG_BASE(BasicObjectChars);
		//lockpoints
		INVISIBLE REG_AUTO(Lockpoints);
		INVISIBLE REG_AUTO(LockpointsDuringBuildStages);
		INVISIBLE REG_AUTO(CheckPoints);
		//other "points"
		INVISIBLE REG_AUTO(BuildPoints);
		INVISIBLE REG_AUTO(ComingInPoints);
		INVISIBLE REG_AUTO(PositionsOfUnits);
		INVISIBLE REG_AUTO(GoingOutPoins);
		INVISIBLE REG_AUTO(GoingOutPoins2);
		INVISIBLE REG_AUTO(FirePoints);
		INVISIBLE REG_AUTO(SmokePoints);
		REG_AUTO(MultiWp);
		REG_MEMBER(_int,MineRadius);
		REG_MEMBER(_int,MineDamage);
		REG_MEMBER(_int,BuildNearBuildingRadius);
		REG_MEMBER(_bool,UnitsCanEnter);
		REG_MEMBER(_bool,PeasantsCanEnter);
		REG_MEMBER(_bool,CanBeUsedLikeStorage);
		REG_MEMBER(_DWORD,StorageMask);
		REG_MEMBER(_bool,Port);
		REG_MEMBER(_bool,Wall);
		REG_MEMBER(_bool,Farm);
		REG_MEMBER(_bool,SpriteObject);
		REG_MEMBER(_bool,Wall);
		REG_MEMBER(_bool,Market);
		REG_MEMBER(_bool,CommandCenter);
		REG_MEMBER(_bool,GlobalCommandCenter);
		REG_MEMBER(_bool,HighUnitCantEnter);
	ENDSAVE;
};
class ComplexObjectChar:public BasicObjectChars{
public:
	int ComplexObjectName;
	int MatherialMaskForComplexObject;
	virtual void  ConvertToNewMonster   (NewMonster* NM);
	virtual void  ConvertFromNewMonster (NewMonster* NM);
	SAVE(ComplexObjectChar);
		REG_PARENT(BasicObjectChars);
		REG_ENUM(_index,ComplexObjectName,"COMPLEXOBJECTS");
		REG_MEMBER(_int,MatherialMaskForComplexObject);
	ENDSAVE;
};