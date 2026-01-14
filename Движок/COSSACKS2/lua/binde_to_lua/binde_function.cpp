
#include <../../BE_HEADERS.h>

#ifdef __LUA__

// functions C++ -> Lua //////////////////////////////////////////////////

// base lua function in defoult lib
void	lua_base(lua_State* L){
	lua_baselibopen(L);
	lua_tablibopen(L);
	lua_iolibopen(L);
	lua_strlibopen(L);
	lua_mathlibopen(L);
	lua_dblibopen(L);
};

// functions from "ActiveScenary.cpp"
DLLEXPORT	byte	Trigg			(byte ID);
DLLEXPORT	void	SetTrigg		(byte ID,byte Val);
DLLEXPORT	word	WTrigg			(byte ID);
DLLEXPORT	void	SetWTrigg		(byte ID,word Val);
			void	RunTimer		(byte ID, int Long, bool trueTime);
DLLEXPORT	void	RunTimer		(byte ID,int Long);
DLLEXPORT	bool	TimerDone		(byte ID);
DLLEXPORT	bool	TimerDoneFirst	(byte ID);
DLLEXPORT	bool	TimerIsEmpty	(byte ID);
DLLEXPORT	void	FreeTimer		(byte ID);
DLLEXPORT	int		GetTime			(byte ID);
DLLEXPORT	int		GetGlobalTime	();
DLLEXPORT	int		GetAnimTime		();
DLLEXPORT	bool	NationIsErased	(byte Nat);
// COSS 2
CEXPORT		int		vdf_GetAmountOfSettlements(byte Owner,	int ResType, bool CheckUpgrades, int Level); 
DLLEXPORT	int		GetBrigadsAmount0(byte NI);

bool	lua_module(const char* fileName);

void	bind_ActiveScenary(lua_State* L){
	using namespace luabind;
	module(L)
	[
		def(	"lua_module"		,	(bool(*)(const char*))		lua_module		),

		def(	"Trigg"				,	Trigg										),
		def(	"SetTrigg"			,	SetTrigg									),
		def(	"WTrigg"			,	WTrigg										),
		def(	"SetWTrigg"			,	SetWTrigg									),

		def(	"RunTimer"			,	(void(*)(byte,int,bool))	RunTimer		),
		def(	"RunTimer"			,	(void(*)(byte,int))			RunTimer		),
		def(	"TimerDone"			,								TimerDone		),
		def(	"TimerDoneFirst"	,								TimerDoneFirst	),
		def(	"TimerIsEmpty"		,								TimerIsEmpty	),
		def(	"FreeTimer"			,								FreeTimer		),
		def(	"GetTime"			,								GetTime			),
		def(	"GetGlobalTime"		,								GetGlobalTime	),
		def(	"GetAnimTime"		,								GetAnimTime		),

		def(	"NationIsErased"	,								NationIsErased	),
		def(	"vdf_GetAmountOfSettlements",			vdf_GetAmountOfSettlements	),
		def(	"GetBrigadsAmount0"	,							 GetBrigadsAmount0	)
	];
};

// function from Condition List
void	bind_Condition(lua_State* L){
	using namespace luabind;
	module(L,"COND")
	[
		def(	"GetAmount_lua"			,						GetAmount_lua			),
        def(	"GetUnitsAmount0_lua"	,						GetUnitsAmount0_lua		),
		def(	"GetUnitsAmount2_lua"	,						GetUnitsAmount2_lua		),
		def(	"GetTotalAmount1_lua"	,						GetTotalAmount1_lua		),
		def(	"GetReadyAmount_lua"	,						GetReadyAmount_lua		),
		def(	"GetResource_lua"		,						GetResource_lua			),
		def(	"GetDiff_lua"			,						GetDiff_lua				),
		def(	"Trigg_lua"				,						Trigg_lua				),
		def(	"ogSTOP_lua"			,						ogSTOP_lua				),
		def(	"CameraSTOP_lua"		,						CameraSTOP_lua			),
		def(	"GetLMode_lua"			,						GetLMode_lua			),
		def(	"CheckButton_lua"		,						CheckButton_lua			),
		def(	"GetFormationType_lua"	,						GetFormationType_lua	),
		def(	"TestFillingAbility_lua",						TestFillingAbility_lua	),
		def(	"CInStandGround_lua"	,						CInStandGround_lua		),
		def(	"VillageOwner_lua"		,						VillageOwner_lua		),
		def(	"GetNofBrigInNode_lua"	,						GetNofBrigInNode_lua	),
		def(	"LoadingCoplite_lua"	,						LoadingCoplite_lua		)
	];
};

// function from Operation List
void	bind_Operation(lua_State* L){
	using namespace luabind;    
	module(L,"OPER")
	[
		def(	"SelectAll_lua"			,						SelectAll_lua			),
		def(	"ChangeAS_lua"			,						ChangeAS_lua			),
		def(	"SelSendTo_lua"			,						SelSendTo_lua			),
		def(	"ChangeFriends_lua"		,						ChangeFriends_lua		),
		def(	"SetFriends_lua"		,						SetFriends_lua			),
		def(	"SetLightSpot_lua"		,						SetLightSpot_lua		),
		def(	"ClearLightSpot_lua"	,						ClearLightSpot_lua		),
		def(	"SetStartPoint_lua"		,						SetStartPoint_lua		),
		def(	"ShowVictory_lua"		,						ShowVictory_lua			),
		def(	"LooseGame_lua"			,						LooseGame_lua			),
		def(	"SavePosition_lua"		,						SavePosition_lua		),
		def(	"SavePositionArr_lua"	,						SavePositionArr_lua		),
		def(	"SetResource_lua"		,						SetResource_lua			),
		def(	"AddRessource_lua"		,						AddRessource_lua		),
		def(	"ActivateTacticalAI_lua",						ActivateTacticalAI_lua	),
		def(	"StartAIEx_lua"			,						StartAIEx_lua			),
		def(	"SetAIEnableState_lua"	,						SetAIEnableState_lua	),
		def(	"ShowDialog_lua"		,						ShowDialog_lua			),
		def(	"AddTextToDlg_lua"		,						AddTextToDlg_lua		),
		def(	"ClearDialog_lua"		,						ClearDialog_lua			),
		def(	"SetScrollLimit_lua"	,						SetScrollLimit_lua		),
		def(	"GetUTypeByName_lua"	,						GetUTypeByName_lua		),
		def(	"PutNewSquad_lua"		,						PutNewSquad_lua			),
		def(	"GetFormationID_lua"	,						GetFormationID_lua		),
		def(	"PutNewFormation_lua"	,						PutNewFormation_lua		),
		def(	"SetUnitStateCII_lua"	,						SetUnitStateCII_lua		),
		def(	"GroupHoldPOS_AI_lua"	,						GroupHoldPOS_AI_lua		),
		def(	"SetTired_lua"			,						SetTired_lua			),
		def(	"KillNatinInPOS_lua"	,						KillNatinInPOS_lua		)
	];
};

//////////////////////////////////////////////////////////////////////////

#endif//__LUA__






































