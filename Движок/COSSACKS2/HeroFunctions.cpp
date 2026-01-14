#include "stdheader.h"
#include "SuperEditor.h"
#include "UnitAbility.h"

HeroVariableStorage* CurrentHeroAbility=NULL;

GetHeroVariable::GetHeroVariable()
{
	StrParam="";
}

void GetHeroVariable::SetArgument(int index, Operand* O)
{
	if(index==0)
	{
		StringType ST;
		if(O&&O->Get(&ST))
		{
			StrParam=ST.String.str;
		}
	}
}
bool GetHeroVariable::Get(BaseType* BT)
{
	bool rez=false;
	if(CurrentHeroAbility)
	{
		NumericalType* NT = dynamic_cast<NumericalType*> (BT);
		if(NT)
		{
			NT->Value=0;
			int* v=CurrentHeroAbility->GetVarRef(StrParam.str);
			if(v)
			{
				NT->Value=*v;
			}
			rez=true;
		}
	}
	return rez;	
}