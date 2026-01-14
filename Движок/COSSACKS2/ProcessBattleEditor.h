
#ifndef __PROCESS_BATTLE_EDITOR__
#define __PROCESS_BATTLE_EDITOR__

#ifdef	__LUA_DEBUGGER__
	extern	CScriptDebugger	g_LUA_DBG;
#endif//__LUA_DEBUGGER__

class	CBE_HANDLER;
extern int ItemChoose;

void	SetCurrentHelp(tpMessageArray** ppTASKS,tpMessageArray** ppTALKS,tpMessageArray** ppHINTS);
// Timer /////////////////////////////////////////////////////////////////
class TempTimerClass : public BaseClass
{	
public:
	TempTimerClass()	{};
	~TempTimerClass()	{};

	int TimerID;
	int Time;
	bool Used;
	bool First;
	int LastCheckAnimTime;
	bool trueTime;

	SAVE(TempTimerClass);
		REG_MEMBER(_int,TimerID);
		REG_MEMBER(_int,Time);
		REG_MEMBER(_bool,Used);
		REG_MEMBER(_bool,First);
		REG_MEMBER(_int,LastCheckAnimTime);
		REG_MEMBER(_bool,trueTime);
	ENDSAVE;
};
// BE_EDIT_PANEL /////////////////////////////////////////////////////////
class BE_EDIT_PANEL : public BaseClass
{
public:
	BE_EDIT_PANEL();
	~BE_EDIT_PANEL();

	bool	EP_LOAD();
	bool	EP_DRAW();
	bool	EP_UPDATE();

	DialogsDesk*	EP_PANEL();
	TextButton*		EP_TEXT();
    
	void			EP_VISIBLE(bool state);
	void			EP_SET_TEXT(char* text);
	
	DialogsSystem	DS_PANEL;
	bool			Visible;
	bool			Error;

	int				X0,Y0,dY,mW,tX;
protected:
	void			EP_PROPER_SIZE_POS();

	void			EP_PANEL_VISIBLE(bool state);
	void			EP_TEXT_VISIBLE(bool state);
	void			EP_PANEL_POSITION(int x,int y);
	void			EP_TEXT_POSITION(int x,int y);
	void			EP_PANEL_SIZE(int w,int h);
	void			EP_TEXT_MAXWIDTH(int maxw);
};
extern	BE_EDIT_PANEL	MAIN_EDIT_PANEL;
// CBEDriveMode_ST ///////////////////////////////////////////////////////
class SingletonCBEDriveMode : public BaseClass
{
public:
	SingletonCBEDriveMode()				{};
	virtual ~SingletonCBEDriveMode()	{};
	static SingletonCBEDriveMode* GetObj()			{ return m_pObj; };
	static void SetObj(SingletonCBEDriveMode* pObj)	{ m_pObj = pObj; };
	static void Delete() { if (m_pObj!=NULL) { delete m_pObj; m_pObj=NULL; } };
	virtual void Accetss() {};
protected:
	static	SingletonCBEDriveMode* m_pObj;
};

inline SingletonCBEDriveMode* StCBEDriveMode(){
	if (SingletonCBEDriveMode::GetObj()==NULL) SingletonCBEDriveMode::SetObj(new SingletonCBEDriveMode);
	return SingletonCBEDriveMode::GetObj();
};

class CBEDriveMode_ST : public SingletonCBEDriveMode
{
public:
	CBEDriveMode_ST();
	virtual ~CBEDriveMode_ST();

	virtual void Access();
public:
	bool	visible;
	SubSection	FilmsMenu;
	SubSection	ProcessMenu;
	SubSection	GrpSetMenu;
	SubSection	NodeSetMenu;
	SubSection	EdgeSetMenu;
	SubSection	NodeViewMode;
	SubSection	EdgeViewMode;
	// OBJECTS
	int		OBJECT;
	bool	USE_vGRP;		// Использовать vGROUPS
		// NODE
		int	NodeAct;
		int	NodeView;
		bool	ND_RECT;
		bool	ND_XY;
		bool	ND_NAME;
		bool	ND_DESCR;
		class CBE_SAVE_NODE_LUA : public BaseFunction {
		public:
			void EvaluateFunction();
			SAVE(CBE_SAVE_NODE_LUA);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}	fNodeList;
		// EDGE
		int	EdgeAct;
		int EdgeView;
		bool	EG_RECT;
		bool	EG_DIRECT;
		bool	EG_PROC;
		bool	EG_START;
		bool	EG_MODE;
		int		SQUARD;
		// SCRIPT
		int		PR_MODE;
		bool	PR_CREATE;
		class CBE_MainEdit : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_MainEdit);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}	fMAIN_EDIT;

		ClassRef<lvCFilm>	scrFILM;

		class CBE_AddFilm : public BaseFunction
		{
		public:
			CBE_AddFilm();
			~CBE_AddFilm();

			_str	scrName;
			_str	scrDescr;

			void EvaluateFunction();

			SAVE(CBE_AddFilm);
				REG_PARENT(BaseFunction);
				REG_AUTO(scrName);
				REG_AUTO(scrDescr);
			ENDSAVE();
		}	fADD_FILM;

		class CBE_DelFilm : public BaseFunction
		{
		public:
			void EvaluateFunction();			

			SAVE(CBE_DelFilm);
				REG_PARENT(BaseFunction);
			ENDSAVE();
		}	fDEL_FILM;
	
		class CBE_EditFilm : public BaseFunction
		{
		public:
			void EvaluateFunction();

			SAVE(CBE_EditFilm);
				REG_PARENT(BaseFunction);
			ENDSAVE();
		}	fEDIT_FILM;
		// ScriptGraph
		class CBE_EditGraph : public BaseFunction
		{
		public:
			void EvaluateFunction();

			SAVE(CBE_EditGraph);
				REG_PARENT(BaseFunction);
			ENDSAVE();
		}	fEDIT_GRAPH;
		class CBE_SAVE_GRAPH_LUA : public BaseFunction {
		public:
			_str FileName;
			void EvaluateFunction();
			SAVE(CBE_SAVE_GRAPH_LUA);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}	fGraphList;

		bool	PR_MAIN;
		bool	PR_SQUAD;
		bool	PROCESS;
		class CBE_Process : public BaseFunction
		{
		public:
			CBE_Process();
			bool	SQUARD_PR;
			bool	MAIN_PR;
			void EvaluateFunction();

			SAVE(CBE_Process);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_bool,MAIN_PR);
				REG_MEMBER(_bool,SQUARD_PR);
			ENDSAVE;

		}	fPROCESS;
		int		PR_TIME;
		// SAVE
		bool	BE_SAVE;		// true for save in file
		_str	FileForScript;	// fro save all script in another file *.xml
		char*	GetSaveSFileName(); // return NULL if file not finde
		// vGROUPS
        int		BE_vGROUPS;
		bool	BE_vGRP_VISIBLE;
		class CBE_vGRP_VISIBLE_ALL : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_vGRP_VISIBLE_ALL);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}	fSHOW_ALL;
		class CBE_vGRP_VISIBLE : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_vGRP_VISIBLE);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}		fSHOW_CURR;
		class CBE_ADD_vGRP : public BaseFunction
		{
		public:
			_str Name;
			void EvaluateFunction();
			SAVE(CBE_ADD_vGRP);
				REG_PARENT(BaseFunction);
				REG_AUTO(Name);
			ENDSAVE;
		}			fADD_GRP;			// Создать новую vGROUP
		class CBE_ADD_vGRP_SMART : public BaseFunction
		{
		public:
			_str Name;
			void EvaluateFunction();
			SAVE(CBE_ADD_vGRP_SMART);
				REG_PARENT(BaseFunction);
				REG_AUTO(Name);
			ENDSAVE;
		}		fADD_GRP_SAMRT;		// Создать новую vGROUP
		class CBE_DEL_vGRP : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_DEL_vGRP);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fDEL_GRP;			// Удалить существующую vGROUP
		class CBE_EDIT_vGRP : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_EDIT_vGRP);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fEDIT_GRP;			// Редактировать vGROUP
		class CBE_ADD_SelUnits : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_ADD_SelUnits);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}		fADD_SEL;			// Добавить выделленых юнитов к vGROUP
		class CBE_ON_SCREEN : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_ON_SCREEN);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fON_SCREEN;			// Центр экрана на vGROUP
		class CBE_CREATE_DEFAULT_VG : public BaseFunction
		{
		public:
			int		Nat;

			void EvaluateFunction();

			SAVE(CBE_CREATE_DEFAULT_VG);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,Nat);
			ENDSAVE;
		}	fCREATE_DEFAULT_VG;
		class CBE_SAVE_GROUP_STRUCT : public BaseFunction {
		public:
			_str FileName;
			void EvaluateFunction();
			SAVE(CBE_SAVE_GROUP_STRUCT);
				REG_PARENT(BaseFunction);
				REG_FILEPATH(FileName,".txt");
			ENDSAVE;
		}	fSaveGroupStruct;
		class CBE_SAVE_GROUP_LUA : public BaseFunction {
		public:
			_str FileName;
			void EvaluateFunction();
			SAVE(CBE_SAVE_GROUP_LUA);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}	fGroupList;
		//  TESTING vGROUPS comand
		bool	BE_vGRP_TEST;
		class CBE_REM_N_UNT : public BaseFunction
		{
		public:
			CBE_REM_N_UNT();
			int destGRP_ID;
			int number;
			void EvaluateFunction();
			SAVE(CBE_REM_N_UNT);
				REG_PARENT(BaseFunction);
				REG_ENUM(_index,destGRP_ID,ALL_vGROUPS_ON_MAP);
				REG_MEMBER(_int,number);
			ENDSAVE;
		}				fREM_N_UNT;		// Убрать N юнитов из vGROUP
		class CBE_KILL_N_UNT : public BaseFunction
		{
		public:
			CBE_KILL_N_UNT();
			int number;
			void EvaluateFunction();
			SAVE(CBE_KILL_N_UNT);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,number);
			ENDSAVE;
		}				fKILL_N_UNT;	// Убить N юнитов из vGROUP
		class CBE_ERASE_N_UNT : public BaseFunction
		{
		public:
			CBE_ERASE_N_UNT();
			int number;
			void EvaluateFunction();
			SAVE(CBE_ERASE_N_UNT);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,number);
			ENDSAVE;
		}			fERASE_N_UNT;	// Исчезнуть N юнитов из vGROUP
		class CBE_UN_SELECT : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_ERASE_N_UNT);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}				fUN_SELECT;		// Снять выделение
		class CBE_SELECT : public BaseFunction
		{
		public:
			CBE_SELECT();
			bool add;
			void EvaluateFunction();
			SAVE(CBE_SELECT);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_bool,add);
			ENDSAVE;
		}					fSELECT;		// Выделить группу
		class CBE_SELECT_IN_XYR : public BaseFunction
		{
		public:

			int x,y,r;
			bool add;
			void EvaluateFunction();
			SAVE(CBE_SELECT_IN_XYR);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,x);
				REG_MEMBER(_int,y);
				REG_MEMBER(_int,r);
				REG_MEMBER(_bool,add);
			ENDSAVE;
		}			fSELECT_XYR;	// Выделить тодько юнитов в зоне x,y,r
		class CBE_SET_NATION : public BaseFunction
		{
		public:
			int NI;
			void EvaluateFunction();
			SAVE(CBE_SET_NATION);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,NI);
			ENDSAVE;
		}				fSET_NATION;	// Выставить цвет
		class CBE_SET_AGRESIVITY : public BaseFunction
		{
		public:
			int state;
			void EvaluateFunction();
			SAVE(CBE_SET_AGRESIVITY);
				REG_PARENT(BaseFunction);
				REG_ENUM(_index,state,BE_UNIT_MOVE_MODE);
			ENDSAVE;
		}			fSET_AGRESIV;	// Выставить уровень агресивности
		class CBE_SEND_TO : public BaseFunction
		{
		public:
			int x,y,dir,type;
			void EvaluateFunction();
			SAVE(CBE_SEND_TO);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,x);
				REG_MEMBER(_int,y);
				REG_MEMBER(_int,dir);
				REG_MEMBER(_int,type);
			ENDSAVE;
		}				fSEND_TO;		// Отправить в точку
		class CBE_SET_DIR : public BaseFunction
		{
		public:
			int dir,type;
			void EvaluateFunction();
			SAVE(CBE_SET_DIR);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,dir);
				REG_MEMBER(_int,type);
			ENDSAVE;
		}				fSET_DIR;		// Повернуть в заданном направлении
		class CBE_SET_FORM : public BaseFunction
		{
		public:
			int formType;
			void EvaluateFunction();
			SAVE(CBE_SET_FORM);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,formType);
			ENDSAVE;
		}				fSET_FORM;		// Поменять построение
		class CBE_SET_FLAGBR : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_SET_FLAGBR);
			REG_PARENT(BaseFunction);
			ENDSAVE;
		}				fSET_FLAGBR;			// Назначить флагоносцев в бригаде
		class CBE_CLEAR_FLAGBR : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_CLEAR_FLAGBR);
			REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fCLEAR_FLAGBR;			// Убрать флагоносцев из бригады

		//	vVALUES
		class CBE_vValuesEdit : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_vValuesEdit);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fvVALUES_EDIT;
		class CBE_CameraPossEdit : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_CameraPossEdit);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fCameraPoss;
		class CBE_BattleHandler : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_BattleHandler);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fBattleHendler;

		//	SETTINGS
		int		MainEditType;
		DWORD	vGroupNameColor;
		DWORD	vGroupLineColor;
		DWORD	EdgeNameColor;
		DWORD	EdgeLineColor;
		DWORD	NodeNameColor;
		DWORD	NodeLineColor;
		DWORD	NodeIDColor;
		int		NodeStyle;
		class CBE_MISS_SET : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_MISS_SET);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}				fMISS_SET;
		class CBE_WCP_EDIT : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_WCP_EDIT);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}				fEDIT_WCP;
		class CSK_TASK_ED : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CSK_TASK_ED);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}fSkirmisTaskEditor;	
		class CSK_TASK_ADD : public BaseFunction
		{
		public:
			int NI; int x; int y; _str name;
			void EvaluateFunction();
			SAVE(CSK_TASK_ADD);
				REG_PARENT(BaseFunction);
				REG_MEMBER(_int,NI);
				REG_MEMBER(_int,x);
				REG_MEMBER(_int,y);
				REG_AUTO(name);
			ENDSAVE;
		}fSkirmisTaskAdd;	
		class CSK_TASK_DEL : public BaseFunction
		{
		public:
			_str name;
			void EvaluateFunction();
			SAVE(CSK_TASK_DEL);
				REG_PARENT(BaseFunction);
				REG_AUTO(name);
			ENDSAVE;
		}fSkirmisTaskDel;	

		//  EDIT
		int				EditOperation;		// Select Edge,Node,Squard.
		lvCEdge*		SelectedEdge;
		lvCNode*		SelectedNodeBeg;
		lvCNode*		SelectedNodeEnd;
		lvCSquardShema*	SelectedSquadShema;
		vvBASE*			SelectedValue;
		DWORD			CorectionType;
		class CBE_COPY_EDGE : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_COPY_EDGE);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}			fCOPY_EDGE;			// Копировать выбранный edge
		class CBE_SELECT_SQUAD : public BaseFunction
		{
			public:
				void EvaluateFunction();
				SAVE(CBE_SELECT_SQUAD);
					REG_PARENT(BaseFunction);
				ENDSAVE;
		}		fSELECT_SQUAD;			// Копировать выбранный edge
		class CBE_COPY_SQUAD : public BaseFunction
		{
		public:
			int	GrpID;
			void EvaluateFunction();
			SAVE(CBE_COPY_SQUAD);
				REG_PARENT(BaseFunction);
				REG_ENUM(_index,GrpID,ALL_vGROUPS_ON_MAP);
			ENDSAVE;
		}			fCOPY_SQUAD;			// Копировать выбранный edge
		class CBE_MESSGES_MGR : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_MESSGES_MGR);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}		fMESSGES_MGR;
		class CBE_ADD_MESSGES : public BaseFunction
		{
		public:
			_str					ParentName;
			ClassPtr<BaseMesMgrST>	Message;
			void EvaluateFunction();
			SAVE(CBE_ADD_MESSGES);
				REG_PARENT(BaseFunction);
				REG_AUTO(ParentName);
				REG_AUTO(Message);
			ENDSAVE;
		}		fADD_MESSGES;
		class CBE_FORM_SQUARD : public BaseFunction
		{
		public:
			ClonesArray<IIPara>		SquadForm;
			void EvaluateFunction();
			SAVE(CBE_FORM_SQUARD);
				REG_PARENT(BaseFunction);
				REG_AUTO(SquadForm);
			ENDSAVE;
		}		fFORM_SQUAD;
		class CBE_WRITE_LOG : public BaseFunction
		{
		public:
			ClassRef<vvMissionLOG>	vvLOG;
			void EvaluateFunction();
			SAVE(CBE_WRITE_LOG);
				REG_PARENT(BaseFunction);
				REG_AUTO(vvLOG);
			ENDSAVE;
		}			fWRITE_LOG;
		class CBE_SHOW_PROMF : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_SHOW_PROMF);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		}fBE_SHOW_PROMF;
		class CBE_MAS_EDITOR : public BaseFunction
		{
		public:
			void EvaluateFunction();
			SAVE(CBE_MAS_EDITOR);
				REG_PARENT(BaseFunction);
			ENDSAVE;
		} fMultyAnimateStoreEditor;
		//	TIMER
		ClonesArray<TempTimerClass>		TimerData;	// Data for cur timer
		void			UpdateTimerData();
		//  NOTHING
public:
	int		GetSquardID();
	DWORD	GetClassMask();
	DWORD	NodeMode();
	DWORD	EdgeMode();
	void	StartMission();
	void	StartMissionAfterLoad();

public:
	SAVE(CBEDriveMode_ST);
		REG_ENUM(_index,OBJECT,BE_OBJECT_TYPE_LIST);
		// "NODE"
		SAVE_SECTION(0x00000001);
			REG_ENUM(_index,NodeAct,BE_NODE_ACTION);
			REG_ENUM(_index,NodeView,BE_NODE_VIEW_MODE);
			REG_ENUM(_index,BE_vGROUPS,ALL_vGROUPS_ON_MAP);
			REG_AUTO(NodeViewMode);
			REG_MEMBER(_bool,ND_RECT);
			REG_MEMBER(_bool,ND_XY);
			REG_MEMBER(_bool,ND_NAME);
			REG_MEMBER(_bool,ND_DESCR);
			REG_AUTO(fNodeList);
		// "EDGE"
		SAVE_SECTION(0x00000002);
			REG_ENUM(_index,EdgeAct,BE_EDGE_ACTION);
			REG_ENUM(_index,EdgeView,BE_EDGE_VIEW_MODE);
			REG_ENUM(_index,BE_vGROUPS,ALL_vGROUPS_ON_MAP);
			REG_AUTO(EdgeViewMode);
			REG_MEMBER(_bool,EG_RECT);
			REG_MEMBER(_bool,EG_DIRECT);
			REG_MEMBER(_bool,EG_PROC);
			REG_MEMBER(_bool,EG_START);
			REG_MEMBER(_bool,EG_MODE);
		// "SCRIPT"
		SAVE_SECTION(0x00000004);
			REG_MEMBER(_int,PR_TIME);
			REG_AUTO(fMAIN_EDIT);
			REG_AUTO(FilmsMenu);
			REG_AUTO(scrFILM);
			REG_AUTO(fADD_FILM);
			REG_AUTO(fDEL_FILM);
			REG_AUTO(fEDIT_FILM);
			REG_AUTO(fEDIT_GRAPH);
			REG_AUTO(fGraphList);
			REG_AUTO(ProcessMenu);
			REG_ENUM(_index,PR_MODE,BE_SCREPT_PROCESS_MODE);
			REG_MEMBER(_bool,PR_CREATE);
			REG_AUTO(fPROCESS);
			REG_ENUM(_index,BE_vGROUPS,ALL_vGROUPS_ON_MAP);
		// "SAVE"
		SAVE_SECTION(0x00000008);
			REG_MEMBER(_bool,BE_SAVE);
			REG_FILEPATH(FileForScript,".xml");
		// "vGROUPS"
		SAVE_SECTION(0x00000010);	
			REG_ENUM(_index,BE_vGROUPS,ALL_vGROUPS_ON_MAP);
			REG_AUTO(fADD_GRP);
			REG_AUTO(fADD_GRP_SAMRT);
			REG_AUTO(fDEL_GRP);
			REG_AUTO(fEDIT_GRP);
			REG_AUTO(fADD_SEL);
			REG_AUTO(fON_SCREEN);
			REG_AUTO(fSHOW_CURR);
			REG_AUTO(fSHOW_ALL);
			REG_AUTO(fCREATE_DEFAULT_VG);
			REG_AUTO(fSaveGroupStruct);
			REG_AUTO(fGroupList);
			REG_MEMBER(_bool,BE_vGRP_TEST);
		// "vGROUPS"
		SAVE_SECTION(0x00000020);
			REG_MEMBER(_bool,BE_vGRP_TEST);
			REG_ENUM(_index,BE_vGROUPS,ALL_vGROUPS_ON_MAP);
			REG_AUTO(fREM_N_UNT);
			REG_AUTO(fKILL_N_UNT);
			REG_AUTO(fERASE_N_UNT);
			REG_AUTO(fUN_SELECT);
			REG_AUTO(fSELECT);
			REG_AUTO(fSELECT_XYR);
			REG_AUTO(fSET_NATION);
			REG_AUTO(fSET_AGRESIV);
			REG_AUTO(fSEND_TO);
			REG_AUTO(fSET_DIR);
			REG_AUTO(fSET_FORM);
			REG_AUTO(fSET_FLAGBR);
			REG_AUTO(fCLEAR_FLAGBR);
		// "EDIT"
			SAVE_SECTION(0x00000200);
			REG_ENUM(_index,EditOperation,BE_EDIT_TYPE_LIST);
			REG_ENUM(_index,BE_vGROUPS,ALL_vGROUPS_ON_MAP);
			REG_AUTO(fCOPY_EDGE);
			REG_AUTO(fSELECT_SQUAD);
			REG_AUTO(fCOPY_SQUAD);
			REG_AUTO(fMESSGES_MGR);
			REG_AUTO(fADD_MESSGES);
			REG_AUTO(fFORM_SQUAD);
			REG_AUTO(fWRITE_LOG);
			REG_AUTO(fSkirmisTaskEditor);
			REG_AUTO(fSkirmisTaskAdd);
			REG_AUTO(fSkirmisTaskDel);
			REG_AUTO(fBE_SHOW_PROMF);
			REG_AUTO(fMultyAnimateStoreEditor);
		// "vVALUES"
		SAVE_SECTION(0x00000040);
			REG_AUTO(fvVALUES_EDIT);
			REG_AUTO(fCameraPoss);
			REG_AUTO(fBattleHendler);
		// "Settings"
		SAVE_SECTION(0x00000080);
			REG_AUTO(fMISS_SET);
			REG_AUTO(fEDIT_WCP);
			REG_AUTO(ProcessMenu);
				REG_MEMBER(_int,MainEditType);
			REG_AUTO(GrpSetMenu);
				REG_MEMBER(_color,vGroupNameColor);
				REG_MEMBER(_color,vGroupLineColor);
			REG_AUTO(EdgeSetMenu);
				REG_MEMBER(_color,EdgeNameColor);
				REG_MEMBER(_color,EdgeLineColor);
			REG_AUTO(NodeSetMenu);
				REG_MEMBER(_color,NodeNameColor);
				REG_MEMBER(_color,NodeLineColor);
				REG_MEMBER(_color,NodeIDColor);
				REG_ENUM(_index,NodeStyle,BE_SETTINGS_NODE_STYLE_LIST);
		// "TIMER"
		SAVE_SECTION(0x00000400);
			REG_AUTO(TimerData);
		// "NOTHING"
		SAVE_SECTION(0x80000000);
	ENDSAVE;
};

inline CBEDriveMode_ST* DriveMode(){
	if (CBEDriveMode_ST::GetObj()==NULL){
		CBEDriveMode_ST::SetObj(new CBEDriveMode_ST);

		BaseClass* pDriveMode = dynamic_cast<BaseClass*>( CBEDriveMode_ST::GetObj() );
		ReplaceEditor("BattleEditor",pDriveMode);
	};
	return (CBEDriveMode_ST*)CBEDriveMode_ST::GetObj();
};
//////////////////////////////////////////////////////////////////////////

// CBE_HANDLER ///////////////////////////////////////////////////////////
class CBE_HANDLER : public GameExtension
{
public:
	
	// Save current game (in missions)
	virtual bool OnGameSaving(xmlQuote& xml);
	virtual bool OnGameLoading(xmlQuote& xml);

	virtual bool OnCheckingBuildPossibility(byte NI,int Type,int& x,int& y);

	virtual	void OnInitAfterMapLoading();
	virtual void OnClassRegistration();
	virtual void OnUnloading();
	virtual bool OnMapUnLoading();
	virtual	void OnEditorStart();
	virtual void ProcessingGame();
	virtual bool OnAttemptToMove(OneObject* Unit,int x,int y);
	virtual void OnDrawOnMapAfterFogOfWar();
	virtual void OnDrawOnMapOverAll();
	virtual void OnDrawOnMiniMap(int x,int y,int Lx,int Ly);
	virtual bool OnCheatEntering(const char* Cheat);
	virtual bool OnMouseHandling(	int mx,int my,
									bool& LeftPressed,
									bool& RightPressed,
									int MapCoordX,int MapCoordY,
									bool OverMiniMap);
};
void BE_InstallExtension();
//////////////////////////////////////////////////////////////////////////

// ProcessBattleEditor ///////////////////////////////////////////////////
void	ProcessBattleEditor();
//////////////////////////////////////////////////////////////////////////

// BE_PROCESS_SCREPT /////////////////////////////////////////////////////
void	BE_PROCESS_SCREPT();
//////////////////////////////////////////////////////////////////////////

// BE_HandlerMouse ///////////////////////////////////////////////////////
bool	BE_MouseInEditor();
bool	BE_HandlerMouse();
//////////////////////////////////////////////////////////////////////////

// BE_EDIT_CLASS /////////////////////////////////////////////////////////
void	Add_Class_To_Main_Editor(DWORD _rce_,DWORD _DILOG_EDITOR_);
//////////////////////////////////////////////////////////////////////////

#endif//__PROCESS_BATTLE_EDITOR__
//////////////////////////////////////////////////////////////////////////

































