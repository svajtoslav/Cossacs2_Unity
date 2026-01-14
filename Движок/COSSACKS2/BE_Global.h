
//#define SAVE_BF(x)	SAVE(x);REG_PARENT(BaseFunction);ENDSAVE;

//////////////////////////////////////////////////////////////////////////
// Global Enumerators ////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	BE_CREATE_VALUE_ENUM();

//////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////
// Global Class //////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
class ggHANDLER : public GameExtension
{
public:
	virtual void OnClassRegistration();					// In USE
	virtual bool OnCheatEntering(const char* Cheat);	// In USE
	virtual void ProcessingGame();						// In USE
};

//////////////////////////////////////////////////////////////////////////
// Global Class //////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	BE2_InstallExtension();
void	BE2_ReggClassEditors();
void	BE2_CALL_ENGINE_FUNC(ssBaseFunction* pFUNC,scValue_List* pVAL_LIST);

//////////////////////////////////////////////////////////////////////////
// Global Objects ////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
class ggGLOBAL_STORE	: public BaseClass
{
public:
	ggGLOBAL_STORE();
public:
	
	SubSection	Chits;
	// Use chipts in game for BE2
	bool	ggBE2_UseChits;
	//////////////////////////////////////////////////////////////////////////
	
	SubSection	EngineFunction;
	// General functions
	ccFunction_List		ggFUNC_LIST;
	bool	Load_FUNC_LIST(char* FileName);
	bool	Save_FUNC_LIST(char* FileName);
	class GS_SHOW_FUNC_LIST : public BaseFunction
	{
	public:
		void EvaluateFunction();
	//	SAVE_BF(GS_SHOW_FUNC_LIST);
		SAVE(GS_SHOW_FUNC_LIST);
			REG_PARENT(BaseFunction);
		ENDSAVE;
	}SHOW_FUNC_LIST;
	class GS_SAVE_FUNC_LIST : public BaseFunction
	{
	public:
		_str	FileName;
		void EvaluateFunction();
		SAVE(GS_SAVE_FUNC_LIST);
			REG_PARENT(BaseFunction);
			REG_FILEPATH(FileName,".xml");
		ENDSAVE;
	}SAVE_FUNC_LIST;
	class GS_LOAD_FUNC_LIST : public BaseFunction
	{
	public:
		_str	FileName;
		void EvaluateFunction();
		SAVE(GS_LOAD_FUNC_LIST);
			REG_PARENT(BaseFunction);
			REG_FILEPATH(FileName,".xml");
		ENDSAVE;
	}LOAD_FUNC_LIST;
	//////////////////////////////////////////////////////////////////////////

	SubSection	PROCEDERES;
	// Moduls used in game 
	// Save section mode for csBaseModul
		// 0x00000001	- Module ::Name
		// 0x00000002	- Module ::Parm_List
		// 0x00000004	- Module ::Value_List
		// 0x00000008	- Module ::CALL_List
		// 0x00000010	- Module ::EDIT
	DWORD				ggMODUL_TVM;
	ccFunction_List		ggPROC_LIST;
	//////////////////////////////////////////////////////////////////////////
	
	SubSection	ValueViewMode;
	// Save section mode for ssValue
		//	0x00000001	- Value ::Name
		//	0x00000002	- Value ::DATA
	DWORD	ggVALUE_TVM;
	//////////////////////////////////////////////////////////////////////////

	SAVE(ggGLOBAL_STORE);
		REG_AUTO(Chits);
			REG_MEMBER(_bool,ggBE2_UseChits);
		REG_AUTO(EngineFunction);
			REG_AUTO(SHOW_FUNC_LIST);
			REG_AUTO(SAVE_FUNC_LIST);
			REG_AUTO(LOAD_FUNC_LIST);
		REG_AUTO(PROCEDERES);
			REG_AUTO(ggPROC_LIST);
	ENDSAVE;
};
extern	ggGLOBAL_STORE	BE_GLOBAL_STORE;

//////////////////////////////////////////////////////////////////////////
// Class Registration ////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	REG_CLASS_GLOBAL_H();

//////////////////////////////////////////////////////////////////////////

















