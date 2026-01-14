//////////////////////////////////////////////////////////////////////////
#pragma once
#include "supereditor.h"
//////////////////////////////////////////////////////////////////////////
class GetHeroVariable: public NumericalReturner
{
public:
	GetHeroVariable();
	//ClassPtr<StringReturner> Op;
	_str StrParam;
	SAVE(GetHeroVariable)
		REG_PARENT(BoolReturner);
		REG_PARENT(Returner);
		REG_AUTO(StrParam);
	ENDSAVE

	virtual void GetViewMask(DString& ST) { ST.Add("Hero:");ST.Add(StrParam.str); };
	virtual int GetNArguments() { return 1; };
	virtual Operand* GetArgument(int Index) { return NULL; };
	virtual void SetArgument(int index, Operand* Op);
	virtual bool Get(BaseType* BT);
	virtual const char* GetFunctionName() { return "Hero"; };
};
