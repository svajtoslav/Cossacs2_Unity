class cva_GS_Player:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_Player)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_Resource:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_Resource)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_FormationDesk:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_FormationDesk)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_UnitsDesk:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
	bool Building;
	SAVE(cva_GS_UnitsDesk){		
		REG_PARENT(vui_Action);
		REG_MEMBER(_bool,Building);
	}ENDSAVE;
};
class cva_GS_Formation:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_Formation)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_Unit:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_Unit)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_PlayerName:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_PlayerName)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_PlayerRace:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_PlayerRace)
    REG_PARENT(vui_Action);
    ENDSAVE
};
class cva_GS_Desk:public vui_Action{
public:
    virtual void SetFrameState(SimpleDialog* SD);
    SAVE(cva_GS_Desk)
    REG_PARENT(vui_Action);
    ENDSAVE
};
