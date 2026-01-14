#pragma once
#include "vui_InterfSynchro.h"
//////////////////////////////////////////////////////////////////////////
void vgf_CreateInterfSynchroAction();
//////////////////////////////////////////////////////////////////////////
class vui_IS_AttackGround: public vui_IS_Base<vui_IS_AttackGround>{ 
public:
	void SynchroAction();
	struct{
		word ObjID,ObjSN;
	}Data;
};
class vui_IS_EraseObject: public vui_IS_Base<vui_IS_EraseObject>{ 
public:
	void SynchroAction();
	struct{
		word ObjID,ObjSN;
	}Data;
};
class vui_IS_CancelAttack: public vui_IS_Base<vui_IS_CancelAttack>{ 
public:
	void SynchroAction();
	struct{
		word ObjID,ObjSN;
	}Data;
};
class vui_IS_AttackGround2: public vui_IS_Base<vui_IS_AttackGround2>{
public:
	void SynchroAction();
	struct{
		word ObjID,ObjSN;
		int x,y;
	}Data;
};
class vui_IS_FillCannon: public vui_IS_Base<vui_IS_FillCannon>{
public:
	void SynchroAction();	
	struct{
		byte NI;
	}Data;
};
class vui_IS_MakeMaxStage: public vui_IS_Base<vui_IS_MakeMaxStage>{
public:
	void SynchroAction();	// здание быстро достроить
	struct{
		word Index;
	}Data;
};
class vui_IS_GBSel: public vui_IS_Base<vui_IS_GBSel>{
public:
	void SynchroAction();	// выделить здания SpecialSel
	struct{
		byte NI;
	}Data;
};
class vui_IS_Sel_SetActivity: public vui_IS_Base<vui_IS_Sel_SetActivity>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		byte ActivityState;
	}Data;
};
class vui_IS_Sel_ClearOrders: public vui_IS_Base<vui_IS_Sel_ClearOrders>{
public:
	void SynchroAction();	
	struct{
		byte NI;		
	}Data;
};
class vui_IS_Sel_MakeReformation: public vui_IS_Base<vui_IS_Sel_MakeReformation>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		word BrigadeID;
		byte FormType;		
	}Data;
};
class vui_IS_Abl_Target: public vui_IS_Base<vui_IS_Abl_Target>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		word NIndex;
		DWORD AblID;
		word TargOB;
		int TargX;
		int TargY;
	}Data;
};
class vui_IS_ActAbl_LeftClick: public vui_IS_Base<vui_IS_ActAbl_LeftClick>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		word NIndex;
		DWORD AblID;
		bool Right;
		bool On;
		//word TargOB;
		//int TargX;
		//int TargY;
	}Data;
};
class vui_IS_ActAbl_LeftClick2: public vui_IS_Base<vui_IS_ActAbl_LeftClick2>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		word NIndex;
		DWORD AblSerial;
		bool Right;
		bool On;
		//word TargOB;
		//int TargX;
		//int TargY;
	}Data;
};
class vui_IS_SelFearDown: public vui_IS_Base<vui_IS_SelFearDown>{
public:
	void SynchroAction();	
	struct{
		byte NI;
	}Data;
};
class vui_IS_SelRTF: public vui_IS_Base<vui_IS_SelRTF>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		byte State;
	}Data;
};
class vui_IS_SelChargeAll: public vui_IS_Base<vui_IS_SelChargeAll>{
public:
	void SynchroAction();	
	struct{
		byte NI;
	}Data;
};
class vui_IS_BrigHoldPosit: public vui_IS_Base<vui_IS_BrigHoldPosit>{
public:
	void SynchroAction();	
	struct{
		byte NI;
	}Data;
};
class vui_IS_SelRifle: public vui_IS_Base<vui_IS_SelRifle>{
public:
	void SynchroAction();	
	struct{
		byte NI;
		byte State;
	}Data;
};
class vui_IS_AddWallSection:public vui_IS_Base<vui_IS_AddWallSection>{
public:
	void SynchroAction(){
		void AddWallSegment(byte NI,int Type,int x0,int y0,int x1,int y1);
		AddWallSegment(Data.NI,Data.Type,Data.x0,Data.y0,Data.x1,Data.y1);
	}
	struct{
        byte NI;
		int  Type;
		int  x0;
		int  y0;
		int  x1;
		int  y1;
	}Data;
};
class vui_IS_LeaveMineAll:public vui_IS_Base<vui_IS_LeaveMineAll>{
public:
	void SynchroAction();
	struct{		
		word ObjID;
		word ObjSN;
	}Data;
};