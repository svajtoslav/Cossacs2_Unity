#pragma once
#include "MapTemplates.h"
//////////////////////////////////////////////////////////////////////////
class vui_GlobalHotKey: public ReferableBaseClass{
public:
	int KeyID;
	bool Ctrl;
	bool Shift;
	bool Alt;
	//char* Name;
	virtual void Action() {};
	virtual int GetState() { return 0; };
	virtual char* GetName(){ return Name.str; };
	//
	virtual const char* GetThisElementView(const char* LocalName);
	virtual bool CheckIfObjectIsGlobal(){ return true; }
	//
	SAVE(vui_GlobalHotKey){		
		REG_ENUM(_index,KeyID,ve_HotKey);
		REG_MEMBER(_bool,Ctrl);
		REG_MEMBER(_bool,Shift);
		REG_MEMBER(_bool,Alt);
		//REG_MEMBER(_textid,Name);
		REG_MEMBER(_str,Name);
	}ENDSAVE;
};
/*
class vui_GHK_Help: public vui_GlobalHotKey { public: void Action(); char* GetName(){ return "Список горячих клавишь."; }; };
class vui_GHK_ReloadUnitInterface: public vui_GlobalHotKey { public: void Action(); char* GetName(){ return "Обновить интерфейс юнитов из файла."; }; };
class vui_GHK_VScripts: public vui_GlobalHotKey { public: void Action(); char* GetName(){ return "Витины скрипты."; }; };
class vui_GHK_VBattle: public vui_GlobalHotKey { public: void Action(); char* GetName(){ return "Скрипты для передвижения армии."; }; }; //  [2/22/2004] Vitya
class vui_GHK_AIEditor: public vui_GlobalHotKey { public: void Action(); char* GetName(){ return "Редактировать АИ."; }; };
class vui_GHK_DialogEditor: public vui_GlobalHotKey { public: void Action(); char* GetName(){ return "Редактировать диалоги."; }; };
*/
//
#define reg_GHK(x,y,z) class x: public vui_GlobalHotKey{ \
	public: void Action(); y;\
	SAVE(x) REG_PARENT(vui_GlobalHotKey); z; ENDSAVE; }
#define reg_GHX(x,y,z) class x: public vui_GlobalHotKey{ \
	public: void Action(); int GetState(); y;\
	SAVE(x) REG_PARENT(vui_GlobalHotKey); z; ENDSAVE; }
//
reg_GHK(vui_GHK_ImpassableZones);
reg_GHK(vui_GHK_Transparency);
reg_GHX(vui_GHK_HealthMode);
reg_GHK(vui_GHK_GoToCurSelPosition);
reg_GHK(vui_GHK_GoToLastActions);
reg_GHK(vui_GHK_Pause);
reg_GHK(vui_GHK_GoAndAttackMode);
reg_GHX(vui_GHK_MiniMapMode);
reg_GHX(vui_GHK_LMode);
reg_GHX(vui_GHK_TaskHint);
reg_GHK(vui_GHK_AddActiveWeapon,
	_str WeaponName;
	int Damage;
	int AttType;
	int Radius;
	,
	REG_AUTO(WeaponName);
	REG_MEMBER(_int,Damage);
	REG_MEMBER(_int,AttType);
	REG_MEMBER(_int,Radius);
);
//cva_Peasant_Idle				
//cva_Peasant_AutoWork			
//
class vui_GlobalHotKeys: public BaseClass{
public:
	vui_GlobalHotKeys(){ ShowHelp=0; }
	ClassArray<vui_GlobalHotKey> HotKeys;
	bool ShowHelp;
	void Process();
	void DrawHelp();
	SAVE(vui_GlobalHotKeys){
		REG_AUTO(HotKeys);
	}ENDSAVE;
};
//
extern vui_GlobalHotKeys v_GlobalHotKeys;
extern char* v_GlobalHotKeysXML;
//
void vgf_CreateHotKeysArray();
//
regAc(cva_HotKeyAction, vfS vfL vfRt
	ClassRef<vui_GlobalHotKey> Action;
	,
	REG_AUTO(Action);
);