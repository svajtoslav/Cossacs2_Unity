#ifndef __BE_BLOCK__
#define __BE_BLOCK__

class ssBaseFunction;
class csBaseModul;
class ccFunction_List;
class ssCALL;
class scCALL_List;

//////////////////////////////////////////////////////////////////////////
// ssBaseFunction ////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
class ssBaseFunction : public ReferableBaseClass
{
public:
	ssBaseFunction();

	const	char*	GetThisElementView(const char* LocalName);
	virtual	bool	CheckIfObjectIsGlobal();

	SAVE(ssBaseFunction);
	SAVE_SECTION(0x00000001);
		REG_AUTO	(	Name		);
	SAVE_SECTION(0x00000002);
		REG_AUTO	(	Parm_List	);
		REG_AUTO	(	RetValue	);
	ENDSAVE;
public:
	DWORD					ClassTypeID;	// Class Type ID
//	_str					Name;			// In ReferableBaseClass::Name
	scParam_List			Parm_List;		// Parameters List for correct work
	ssParametr				RetValue;		// Returned value
};

//////////////////////////////////////////////////////////////////////////
// ccFunction_List ///////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
class ccFunction_List : public ClassArray<ssBaseFunction>
{
public:
	ccFunction_List();

	int			 ExpansionRules;		//0-no expansion 1-expand with base type only 2-expand with child classes
	virtual int	 GetExpansionRules();
			void SetExpansionRules(int ExRul);
public:
	SAVE(ccFunction_List);
	ENDSAVE;
};

//////////////////////////////////////////////////////////////////////////
// ssCALL ////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
class ssCALL	: public BaseClass
{
public:
	ClassRef<ssBaseFunction>	FUNC;
	scValue_List				VALUES;
	int			CALL();

	class CALL_CREATE_PARAM : public BaseFunction
	{
	public:
		void	EvaluateFunction();
		SAVE(CALL_CREATE_PARAM);
			REG_PARENT(BaseFunction);
		ENDSAVE;
	}CREATE_PARAM;
public:
	SAVE(ssCALL);
		REG_AUTO(FUNC);
		REG_AUTO(CREATE_PARAM);
		REG_AUTO(VALUES);
	ENDSAVE;
protected:
	void	CreateDefValueList();
};

//////////////////////////////////////////////////////////////////////////
// scCALL_List ///////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
class scCALL_List	: public ClassArray<ssCALL>
{
public:
	virtual int GetExpansionRules();
public:
	SAVE(scCALL_List);
	ENDSAVE;
protected:
};

//////////////////////////////////////////////////////////////////////////
// csBaseModul ///////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
bool rce_BaseModulProcCallback(ClassEditor* CE,BaseClass* BC,int Options);
class csBaseModul : public ssBaseFunction
{
public:
	csBaseModul();

	virtual	DWORD	GetClassMask();

	class BM_EDIT	: public BaseFunction
	{
	public:
		void EvaluateFunction();
		SAVE(BM_EDIT);
			REG_BASE(BaseFunction);
			REG_PARENT(BaseFunction);
		ENDSAVE;
	}EDIT;

	SAVE(csBaseModul);
		REG_BASE	( ssBaseFunction	);
		REG_PARENT	( ssBaseFunction	);
	SAVE_SECTION(0x00000004);
		REG_AUTO	( Value_List		);
	SAVE_SECTION(0x00000008);
		REG_AUTO	( CALL_List			);
	SAVE_SECTION(0x00000010);
		REG_AUTO	( EDIT				);
	ENDSAVE;
public:
	scValue_List	Value_List;	// Value List (lockal for curent object)
	scCALL_List		CALL_List;	// Call another func/moduls list
};

//////////////////////////////////////////////////////////////////////////
// Class Registration ////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
void	REG_CLASS_BLOCK_H();

//////////////////////////////////////////////////////////////////////////

#endif//__BE_BLOCK__








































