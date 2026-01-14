#pragma once
///#include "AI_Scripts.h"

class qUnitType : public NumericalReturner
{
public:
	qUnitType();
	int Type;
	SAVE(qUnitType);
		REG_PARENT(NumericalReturner);
		REG_PARENT(Returner);
		REG_MEMBER(_UnitType,Type);
	ENDSAVE; 
	
	virtual void GetViewMask(DString& ST);
	virtual int GetNArguments();
	virtual void SetArgument(int index, Operand* Op);
	virtual bool Get(BaseType* BT);
	virtual const char* GetFunctionName();
};

class qBrigadeFormationType : public NumericalReturner 
{
public:
	qBrigadeFormationType();
	int Type;
	SAVE(qBrigadeFormationType);
		REG_PARENT(NumericalReturner);
		REG_PARENT(Returner);
		REG_ENUM(_index,Type,BrigadeFormationTypeEnum);
	ENDSAVE; 

	virtual void GetViewMask(DString& ST);
	virtual int GetNArguments();
	virtual void SetArgument(int index, Operand* Op);
	virtual bool Get(BaseType* BT);
	virtual const char* GetFunctionName();
};