#pragma once
//////////////////////////////////////////////////////////////////////////
#define MAXVDWORDS 8
#define MAXVFUNC (MAXVDWORDS*32)
class GameExtension:public BaseClass{	
	DWORD Usage[MAXVDWORDS];
public:
	_str ExtensionName;
	DWORD GetCode();
	_inline void UnMask (int idx){Usage[idx>>5]&=0xFFFFFFFF-(1<<(idx&31));}
	_inline void Mask   (int idx){Usage[idx>>5]|=1<<(idx&31);}
	_inline bool Check  (int idx){return Usage[idx>>5]&(1<<(idx&31));}

	const char* GetExtName(){return ExtensionName.str;}
	GameExtension(){memset(Usage,0xFF,sizeof Usage);};
	//---------actions of the game----------//
	virtual void ProcessingGame()  											{UnMask(0);}//
	virtual void OnGameStart()     											{UnMask(1);}//
	virtual void OnGameEnd()       											{UnMask(2);}//
	virtual void OnVictory(byte Nation)										{UnMask(3);}//
	virtual void OnDefeat(byte Nation)										{UnMask(4);}//
	virtual void OnManualExit(byte Nation)									{UnMask(5);}//
	virtual void OnClassRegistration()										{UnMask(6);}//
	virtual void OnUnloading()  											{UnMask(7);}//
	virtual void OnReloading()  											{UnMask(8);}//
	virtual void OnInitAfterMapLoading()  									{UnMask(9);}//
	//
	virtual void OnEditorStart()   											{UnMask(10);}//
	virtual void OnEditorEnd()     											{UnMask(11);}//
	virtual bool OnMapSaving(xmlQuote& xml)									{UnMask(12);return false;}
	virtual bool OnMapLoading(xmlQuote& xml)								{UnMask(13);return false;}
	virtual bool OnGameSaving(xmlQuote& xml)								{UnMask(14);return false;}
	virtual bool OnGameLoading(xmlQuote& xml)								{UnMask(15);return false;}
	virtual bool OnCheatEntering(const char* Cheat)   						{UnMask(16);return false;}//
	virtual bool OnCheatReceived(byte Nation,const char* Cheat)				{UnMask(17);return false;}//
	virtual bool OnMouseHandling
		(int mx,int my,bool& LeftPressed,bool& RightPressed,
		int MapCoordX,int MapCoordY,bool OverMiniMap)						{UnMask(18);return false;}//
	//
	virtual bool OnEndGameMessage(int NI,int VictStatus)					{UnMask(19);return false;}
	//
	virtual bool OnMapUnLoading()											{UnMask(20);return false;}
	//integrated editor management
	virtual bool CheckActivityOfEditor()									{UnMask(21);return false;}
	virtual void ClearActivityOfEditor()									{UnMask(22);}
	virtual bool DrawEditorInterface()					                    {UnMask(23);return false;}
	virtual bool GetEditorAttributes(int& gp_file,
		int& ActiveSprite,int& PassiveSprite,_str& Hint)					{UnMask(24);return false;}	
	virtual bool CheckIfMouseOverEditorInterface(int x,int y)               {UnMask(25);return false;}
	virtual bool OnMouseWheel(int Delta)									{UnMask(26);return false;}
	virtual void ActivateEditor()                                           {UnMask(27);}

	virtual void OnDrawOnMapAfterLandscape()								{UnMask(30);}//
	virtual void OnDrawOnMapAfterUnits()									{UnMask(31);}//
	virtual void OnDrawOnMapBeforeWater()									{UnMask(32);}//
	virtual void OnDrawOnMapAfterWater()									{UnMask(33);}//
	virtual void OnDrawOnMapAfterTransparentEffects()						{UnMask(34);}//
	virtual void OnDrawOnMapAfterFogOfWar()									{UnMask(35);}//
	virtual void OnDrawOnMapOverAll()										{UnMask(36);}//

	virtual void OnDrawOnMiniMap(int x,int y,int Lx,int Ly)					{UnMask(40);}//

	virtual void OnUnitBirth(OneObject* NewUnit)							{UnMask(50);}//
	virtual bool OnUnitDie(OneObject* Dead,OneObject* Killer)				{UnMask(51);return true;}
	virtual bool OnUnitDamage
		(OneObject* DamagedUnit,OneObject* Damager,int& Damage)				{UnMask(52);return true;}
	virtual bool OnUnitCapture(OneObject* CapturedUnit,OneObject* Capturer)	{UnMask(53);return true;}
	virtual bool OnUnitPanic(OneObject* Panicer)							{UnMask(54);return true;}
	virtual bool OnAttemptToAttack(OneObject* Attacker,OneObject* Victim)	{UnMask(55);return true;}
	virtual bool OnAttemptToMove(OneObject* Unit,int x,int y)				{UnMask(56);return true;}
	virtual bool OnAttemptToTakeResource
		(OneObject* Unit,int x,int y,int ResType)							{UnMask(57);return true;}
	virtual bool OnCheckingBuildPossibility(byte NI,int Type,int& x,int& y)	{UnMask(58);return true;}

	virtual void OnBrigadeCreated(Brigade* BR)								{UnMask(80);}
	virtual void OnBrigadeFreedManually(Brigade* BR)						{UnMask(81);}
	virtual void OnBrigadeKilled(Brigade* BR,byte KillerNation)				{UnMask(82);}
	//virtual bool OnBrigadeReformed(Brigade* BR,int StartForm,int EndForm)	{UnMask(83);return true;}    

	virtual bool OnUnknownCommandInMD_File
		(NewMonster* NM,const char* Command,const char* File,int Line)      {UnMask(100);return false;}
	virtual bool OnUnknownCommandInNDS_File
		(byte NI,const char* SectionName,
		const char* Command,const char* File,int Line)						{UnMask(101);return false;}
	virtual bool OnUnknownCommandInRDS_File
		(SprGroup* SG,ObjCharacter* OC,const char* SectionName,
		const char* Command,const char* File,int Line)						{UnMask(102);return false;}	
};
void InstallExtension(GameExtension* Ext,const char* Name);
void UnInstallExtension(const char* Name);

void ext_OnMapSaving(xmlQuote& xml);
void ext_OnMapLoading(xmlQuote& xml);
void ext_OnGameSaving(xmlQuote& xml);
void ext_OnGameLoading(xmlQuote& xml);
