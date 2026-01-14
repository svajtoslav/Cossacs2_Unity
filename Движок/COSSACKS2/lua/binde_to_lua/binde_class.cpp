
#include <../../BE_HEADERS.h>

#ifdef __LUA__

// class C++ -> Lua //////////////////////////////////////////////////////

void	bind_lvCNode(lua_State* L){
	using namespace luabind;
	module(L)
	[
		def(	"ShowWarning"	,	ggg_WarnigMessage	)
	];

	module(L,"NODE")
	[
		class_<lvCNode>("node")
		// constructors
		.def(	constructor<>()						)
		.def(	constructor<int,int>()				)
		.def(	constructor<int,int,int>()			)
		.def(	constructor<int,int,int,int>()		)
		.def(	constructor<int,int,int,int,int>()	)
		// space node params
		//			.def(	"SetXY"			,	&lvCNode::vSetXY		)	// Safe change node position
		.def(	"SetR"			,	&lvCNode::vSetR			)	// Change node R
		.def(	"SetDir"		,	&lvCNode::vSetDir		)	// Change node Dir
		.def(	"SetSegmFR"		,	&lvCNode::vSetSegmFR	)	// Change GegmFR
		.def(	"SetGParam"		,	&lvCNode::vSetGParam	)	// Change All spase param
		// description node params
		.def(	"SetNodeName"	,	&lvCNode::vSetNodeName	)	// Change node name
		// node movement
		.def(	"AddX"			,	&lvCNode::vAddX			)	// Add to x coord
		.def(	"AddY"			,	&lvCNode::vAddY			)	// Add to y coord
		.def(	"AddXY"			,	&lvCNode::vAddXY		)	// Add to (x,y) coord
		// property
		.property(	"x"		,	&lvCNode::vGetX		,	&lvCNode::vSetX		)
		.property(	"y"		,	&lvCNode::vGetY		,	&lvCNode::vSetY		)
		.property(	"R"		,	&lvCNode::vGetR		,	&lvCNode::vSetR		)
		.property(	"Dir"	,	&lvCNode::vGetDir	,	&lvCNode::vSetDir	)
		.property(	"SegmFR",	&lvCNode::vGetSegmFR,	&lvCNode::vSetSegmFR)
		,
		def(	"GetNode"		,		__getNodeByName			),
		def(	"GetNode"		,		__getNodeByID			)
	];
};



void	bind_lvCGroup(lua_State* L){
	using namespace luabind;
	module(L,"GRP")
	[
		class_<lvCGroup>("group")
			.def(	constructor<const char*>()										)
			.def(	"GetORDER"				,		&lvCGroup::GetORDER				)
			.def(	"GetUnitORDER"			,		&lvCGroup::GetUnitORDER			)
			.def(	"RemoveDeadUnits"		,		&lvCGroup::RemoveDeadUnits		)
			.def(	"RemoveNUnitsToCGroup"	,		&lvCGroup::RemoveNUnitsToCGroup	)
			.def(	"GetGroupName"			,		&lvCGroup::GetGroupName			)
			.def(	"KillUnits"				,		&lvCGroup::KillUnits			)
			.def(	"EraseUnits"			,		&lvCGroup::EraseUnits			)
			.def(	"SetNation"				,		&lvCGroup::SetNation			)
			.def(	"SendTo"				,		&lvCGroup::SendTo				)	
			.def(	"SendToPosition"		,		&lvCGroup::SendToPosition		)
			.def(	"ChangeDirection"		,		&lvCGroup::ChangeDirection		)	
			.def(	"SetAgresiveST"			,		&lvCGroup::SetAgresiveST		)
			.def(	"SetInStandGround"		,		&lvCGroup::SetInStandGround		)
			.def(	"ChengeFormation"		,		&lvCGroup::ChengeFormation		)
			.def(	"SelectUnits"			,		&lvCGroup::SelectUnits			)
			.def(	"TakeRess"				,		&lvCGroup::TakeRess				)
			.def(	"TakeFood"				,		&lvCGroup::TakeFood				)
			.def(	"TakeWood"				,		&lvCGroup::TakeWood				)
			.def(	"TakeStone"				,		&lvCGroup::TakeStone			)
			.def(	"UnitNumByType"			,		&lvCGroup::GetTotalAmount2		)
			.def(	"UnitNumTotal"			,		&lvCGroup::GetTotalAmount		)
			.def(	"UnitNumInZone"			,		&lvCGroup::GetAmountInZone		)
			.def(	"GetGroupCenter"		,		&lvCGroup::GetGroupCenter		)
			.def(	"GetGroupX"				,		&lvCGroup::GetGroupX			)
			.def(	"GetGroupY"				,		&lvCGroup::GetGroupY			)
			.def(	"GetDirection"			,		&lvCGroup::GetDirection			)
			.def(	"GetNation"				,		&lvCGroup::GetNation			)
			.def(	"GetAgresiveState"		,		&lvCGroup::GetAgresiveState		)
			.def(	"GetBrigadeList"		,		&lvCGroup::GetBrigadeList		)
			.def(	"GetAmountOfNewUnits"	,		&lvCGroup::GetAmountOfNewUnits	)
			.def(	"GetIsTired"			,		&lvCGroup::GetIsTired			)
			.def(	"GetNInside"			,		&lvCGroup::GetNInside			)
			.def(	"GetLeaveAbility"		,		&lvCGroup::GetLeaveAbility		)
			.def(	"GetNofBRLoadedGun"		,		&lvCGroup::GetNofBRLoadedGun	)
			.def(	"ChekPosition"			,		&lvCGroup::ChekPosition			)
			.enum_("UnitState")
			[
				value(	"_NO_ORDERS"	,	0	),
				value(	"_MOVE"			,	1	),
				value(	"_ATTACK"		,	2	),
				value(	"_SOME_ORDER"	,	4	),
				value(	"_BRIG_ORDER"	,	8	)
			]
			,
			def(	"GetGroup"				,	( lvCGroup* (*) (const char*) )	__getGrpByName	),
			def(	"GetGroup"				,	( lvCGroup* (*) (int) )			__getGrpByID	)
	];
};



void	bind_groupMAP(lua_State* L){
	using namespace luabind;
	module(L)
	[
		def(	"GetGroupByName"		,	( lvCGroup* (*) (const char*) )	__getGrpByName	)
	];
};

void	bind_valuesMAP(lua_State* L){
	using namespace luabind;
	module(L,"VALUE")
	[
		class_<	vvBASE						>	(	"vvBASE"			)
		.property(	"Name"		,	&vvBASE::GetName		,		&vvBASE::SetName		)
		,
		class_<	vvTRIGER		,	vvBASE	>	(	"vvTRIGER"			)
		.property(	"Value"		,	&vvTRIGER::GetValue		,		&vvTRIGER::SetValue		)
		,
		class_<	vvWORD			,	vvBASE	>	(	"vvWORD"			)
		.property(	"Value"		,	&vvWORD::GetValue		,		&vvWORD::SetValue		)
		,
		class_<	vvINTEGER		,	vvBASE	>	(	"vvINTEGER"			)
		.property(	"Value"		,	&vvINTEGER::GetValue	,		&vvINTEGER::SetValue	)
		,
		class_<	vvTEXT			,	vvBASE	>	(	"vvTEXT"			)
		.property(	"TextID"	,	&vvTEXT::Get_TextID		,		&vvTEXT::Set_TextID		)
		.property(	"oggFile"	,	&vvTEXT::Get_oggFile	,		&vvTEXT::Set_oggFile	)
		.property(	"SpeakerID"	,	&vvTEXT::Get_SpeakerID	,		&vvTEXT::Set_SpeakerID	)
		,
		class_<	vvPICTURE		,	vvBASE	>	(	"vvPICTURE"			)
		.property(	"SpriteNUM"	,	&vvPICTURE::GetSpriteNUM								)
		.property(	"SpriteID"	,	&vvPICTURE::GetSpriteID	,		&vvPICTURE::SetSpriteID	)
		.def_readonly(	"FileID"	,	&vvPICTURE::FileID	)
		.def_readwrite(	"dx"		,	&vvPICTURE::dx		)
		.def_readwrite(	"dy"		,	&vvPICTURE::dy		)
		.def_readwrite(	"lx"		,	&vvPICTURE::lx		)
		.def_readwrite(	"ly"		,	&vvPICTURE::ly		)
		,
		class_<	vvPOINT2D		,	vvBASE	>	(	"vvPOINT2D"			)
		.property(	"x"			,	&vvPOINT2D::GetX		,		&vvPOINT2D::SetX		)
		.property(	"y"			,	&vvPOINT2D::GetY		,		&vvPOINT2D::SetY		)
		,
		class_<	vvPOINT_SET		,	vvBASE	>	(	"vvPOINT_SET"		)
		.def(	"Clear"				,	&vvPOINT_SET::CleanARR			)
		.def(	"Add"				,	&vvPOINT_SET::AddPoint			)
		.property(	"N"			,	&vvPOINT_SET::GetNUM									)
		.def(	"GetPoint"			,	&vvPOINT_SET::Get_vvPOINT2D		)
		,
		class_<	vvVector3D		,	vvBASE	>	(	"vvVector3D"		)
		.def_readwrite(	"x"			,	&vvVector3D::x		)
		.def_readwrite(	"y"			,	&vvVector3D::y		)
		.def_readwrite(	"z"			,	&vvVector3D::z		)
		.def_readwrite(	"mX"		,	&vvVector3D::mX		)
		.def_readwrite(	"mY"		,	&vvVector3D::mY		)
		,
		class_<	vvDIALOG		,	vvBASE	>	(	"vvDIALOG"			)
		,
		class_<	vvMissionLOG	,	vvBASE	>	(	"vvMissionLOG"		)
		.def(	"Clear"				,	&vvMissionLOG::Clear			)
		.def(	"SetQuestData"		,	&vvMissionLOG::SetQuestData		)
		.def(	"SetKilsData"		,	&vvMissionLOG::SetKilsData		)
		.def(	"SetTimeData"		,	&vvMissionLOG::SetTimeData		)
		.def(	"AddCopmleteQuest"	,	&vvMissionLOG::AddCopmleteQuest	)
		.def(	"AddKillsCopmlete"	,	&vvMissionLOG::AddKillsCopmlete	)
		.def(	"AddTimeCopmlete"	,	&vvMissionLOG::AddTimeCopmlete	)
		.def(	"WriteToLogClass"	,	&vvMissionLOG::WriteToLogClass	)
		,
		class_<	vvMESSGES		,	vvBASE	>	(	"vvMESSGES"			)
		,
		class_<	vvMISSMGR		,	vvBASE	>	(	"vvMISSMGR"			)
		.def(	"PAUSE"				,	&vvMISSMGR::STPS_ENABLED		)
		.def(	"RESET"				,	&vvMISSMGR::REST_ENABLED		)
		.def(	"NEXT"				,	&vvMISSMGR::NEXT_ENABLED		)
		,
		class_<	vvBrigAI		,	vvBASE	>	(	"vvBrigAI"			)
		,
		class_<	vvTASKS_CII		,	vvBASE	>	(	"vvTASKS_CII"		)
		.def(	"AddTASK"			, &vvTASKS_CII::addLT_TASK_lua		)
		.def(	"AddHINT"			, &vvTASKS_CII::addLT_HINT_lua		)
		.def(	"AddELSE"			, &vvTASKS_CII::addLT_ELSE_lua		)
		.def(	"DelTASK"			, &vvTASKS_CII::delLT_TASK_lua		)
		.def(	"DelHINT"			, &vvTASKS_CII::delLT_HINT_lua		)
		.def(	"DelELSE"			, &vvTASKS_CII::delLT_ELSE_lua		)
		.def(	"SetTASK_compl"		, &vvTASKS_CII::setLT_TASK_COMPLITE	)
		,
		class_<	lvCTeraforming	,	vvBASE	>	(	"lvCTeraforming"	)
		,
		def(	"get_vv"	,	__getValByName<vvBASE>			),
		def(	"get_tg"	,	__getValByName<vvTRIGER>		),
		def(	"get_wd"	,	__getValByName<vvWORD>			),
		def(	"get_in"	,	__getValByName<vvINTEGER>		),
		def(	"get_tx"	,	__getValByName<vvTEXT>			),
		def(	"get_pc"	,	__getValByName<vvPICTURE>		),
		def(	"get_p2"	,	__getValByName<vvPOINT2D>		),
		def(	"get_ps"	,	__getValByName<vvPOINT_SET>		),
		def(	"get_v3"	,	__getValByName<vvVector3D>		),
		def(	"get_dl"	,	__getValByName<vvDIALOG>		),
		def(	"get_ml"	,	__getValByName<vvMissionLOG>	),
		def(	"get_ms"	,	__getValByName<vvMESSGES>		),
		def(	"get_mm"	,	__getValByName<vvMISSMGR>		),
		def(	"get_bi"	,	__getValByName<vvBrigAI>		),
		def(	"get_tc"	,	__getValByName<vvTASKS_CII>		),
		def(	"get_tr"	,	__getValByName<lvCTeraforming>	),
		def(	"get_fz"	,	__getValByName<vvFuzzyRule>		)
		,
		class_<	vvFuzzyRule		,	vvBASE	>	(	"vvFuzzyRule"	)
		.def(	"getDegree"	,	&vvFuzzyRule::IsTrueToWhatDegree	)
		.def(	"getName"	,	&vvFuzzyRule::GetName				)
		,
		def(	"FuzzyAND"	,	__FuzzyAND							)
	];
};

void	bind_GraphObjMAP(lua_State* L){
	using namespace luabind;
	module(L,"GRAPH")
	[
		class_<	lvCGraphObject								>	(	"lvCGraphObject"		)
		.def(	"isVissible"		, &lvCGraphObject::isVissible	)
		.def(	"isInVisible"		, &lvCGraphObject::isInVisible	)
		,
		class_<	lvCDialogBased			,	lvCGraphObject	>	(	"lvCDialogBased"		)
		,
		class_<	lvCBlackScreen			,	lvCGraphObject	>	(	"lvCBlackScreen"		)
		.def(	"isVissible"		, &lvCBlackScreen::isVissible	)
		.def(	"isInVisible"		, &lvCBlackScreen::isInVisible	)
		,
		class_<	lvCMoveGP				,	lvCGraphObject	>	(	"lvCMoveGP"				)
		.def(	"isVissible"		, &lvCMoveGP::isVissible		)
		.def(	"isInVisible"		, &lvCMoveGP::isInVisible		)
		,
		class_<	lvCAAppearGP			,	lvCBlackScreen	>	(	"lvCAAppearGP"			)
		,
		class_<	lvCAnimateGP			,	lvCAAppearGP	>	(	"lvCAnimateGP"			)
		,
		class_<	lvCDeffFilmMenu			,	lvCGraphObject	>	(	"lvCDeffFilmMenu"		)
		.def(	"isVissible"		, &lvCDeffFilmMenu::isVissible	)
		.def(	"isInVisible"		, &lvCDeffFilmMenu::isInVisible	)
		,
		class_<	lvCDeffAnimeFilmMenu	,	lvCGraphObject	>	(	"lvCDeffAnimeFilmMenu"	)
		,
		def(	"GetGraph"				,	__getGraphByName		)
	];
};

//////////////////////////////////////////////////////////////////////////

#endif//__LUA__







































