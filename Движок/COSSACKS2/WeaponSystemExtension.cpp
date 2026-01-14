//==================================================================================================================//
#include "stdheader.h"
#include "WeaponSystem.h"
#include "GameExtension.h"
#include "WeaponSystemExtension.h"
//==================================================================================================================//
void WeaponSystemExtension::OnUnloading()
{
	GameWeaponSystem.ClearAllActiveWeapons();
}
bool WeaponSystemExtension::OnGameSaving(xmlQuote& xml)
{
	GameWeaponSystem.ActiveWeapons.Save(xml,&GameWeaponSystem.ActiveWeapons);
	return true;
}
bool WeaponSystemExtension::OnGameLoading(xmlQuote& xml)
{
	OnUnloading();
	ErrorPager Error;
	GameWeaponSystem.ActiveWeapons.Load(xml,&GameWeaponSystem.ActiveWeapons,&Error,NULL);
	return true;
}
//==================================================================================================================//