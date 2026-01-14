#include "stdheader.h"
#include "UnitAbility.h"
#include "GameExtension.h"
#include "UnitAbilityExtension.h"
#include "ua_Item.h"
//==================================================================================================================//
DWORD ActiveAbilityListArray::CurSerial=0;
void UnitAbilityExtension::OnClassRegistration(){
	REG_CLASS(ActiveAbilityListArray);
	//
	REG_CLASS(NewItem);
	REG_CLASS(NewMagazine);
}
void UnitAbilityExtension::OnUnloading()
{
	ActiveAbilityListArray::CurSerial=0;
	for(int i=0;i<MAXOBJECT;i++)
	{
		OneObject* OB=Group[i];
		if(OB)
		{
			if(OB->ActiveAbility)
			{
				delete (OB->ActiveAbility);
				OB->ActiveAbility=NULL;
			}
		}
	};
}
bool UnitAbilityExtension::OnGameSaving(xmlQuote& xml)
{
	for(int i=0;i<MAXOBJECT;i++)
	{
		OneObject* OB=Group[i];
		if(OB)
		{
			if(OB->ActiveAbility)
			{
				int n = OB->ActiveAbility->ActiveAbilities.GetAmount();
				if(n)
				{
					xmlQuote* ua = new xmlQuote("ActiveAbilities");
					xmlQuote* ua0 = new xmlQuote("ActiveAbilities");
					ua->Assign_int(i);
					OB->ActiveAbility->Save(*ua0,OB->ActiveAbility,NULL);
					ua->AddSubQuote(ua0);
					xml.AddSubQuote(ua);
				}
			}
		}
	};
	return true;
}
bool UnitAbilityExtension::OnGameLoading(xmlQuote& xml)
{
	int n = xml.GetNSubQuotes();
	for(int i=0;i<n;i++)
	{
		xmlQuote* ua = xml.GetSubQuote(i);
		int ind = ua->Get_int();
		OneObject* OB=Group[ind];
		if(OB)
		{
			ErrorPager Error;
			if(!OB->ActiveAbility)
				OB->ActiveAbility = new UnitActiveAbilityList();
			OB->ActiveAbility->Load(*(ua->GetSubQuote(0)),OB->ActiveAbility,&Error,NULL);
		}
	}
	return true;
}
//==================================================================================================================//