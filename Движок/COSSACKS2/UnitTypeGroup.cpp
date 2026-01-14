//----------------------------------------------------------------------------------------------------------------//
#include "stdheader.h"
#include "UnitAbility.h"
#include "UnitTypeGroup.h"
//----------------------------------------------------------------------------------------------------------------//
const char* TypeGroup::GetThisElementView(const char* LocalName){
	if(Name.str){
		static char cc[256];
		sprintf(cc,"%s: {CW}%s{C}",LocalName,Name.str);
		return cc;
	}else return LocalName;
}
//----------------------------------------------------------------------------------------------------------------//
int TypeGroupList::GetExpansionRules()
{
	return 1;
}
//----------------------------------------------------------------------------------------------------------------//
bool UnitTypeGroup::GetNewUnitType(int OldUnitType,byte NewNationID, int& NewUnitType)
{
	int n=Groups.GetAmount();
	for(int i=0;i<n;i++)
	{
		int nt=Groups[i]->UnitList.GetAmount();
		for(int j=0;j<nt;j++)
		{
			if(Groups[i]->UnitList[j]->UnitType==OldUnitType)
			{
				for(int k=0;k<nt;k++)
				{
					int NT=Groups[i]->UnitList[k]->UnitType;
					if(NATIONS[0].Mon[NT]->NatID==NewNationID)
					{
						NewUnitType=NT;
						return true;
					}
				}
			}
		}
	}
	return false;
}
word UnitTypeGroup::GetType(word Index, byte NationID, bool Hero){
	int n=Groups.GetAmount();
	if(Index<n) for(int i=0;i<n;i++){
		int nt=Groups[i]->UnitList.GetAmount();
		if(!Hero||Groups[i]->Hero){ //for(int j=0;j<nt;j++)
			 for(int k=0;k<nt;k++){
				int NT=Groups[i]->UnitList[k]->UnitType;
				if(NATIONS[0].Mon[NT]->NatID==NationID){
					if(Index==0){
						return NT;
					}
					Index--;
					break;
				}
			}
		}
	}
	return 0xFFFF;
};
//----------------------------------------------------------------------------------------------------------------//
UnitTypeGroup UnitTypeGroups;
//----------------------------------------------------------------------------------------------------------------//
bool GetNewUnitType(int OldUnitType,byte NewNationID, int& NewUnitType)
{
	return UnitTypeGroups.GetNewUnitType( OldUnitType, NewNationID, NewUnitType);
}
//----------------------------------------------------------------------------------------------------------------//
void LoadUnitTypeGroups(){
	UnitTypeGroups.reset_class(&UnitTypeGroups);
	UnitTypeGroups.SafeReadFromFile("dialogs\\UnitTypeGroups.xml");
}