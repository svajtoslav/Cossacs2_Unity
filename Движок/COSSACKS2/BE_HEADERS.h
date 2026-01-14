

// COSSACKS2
#include "../stdheader.h"	
#include "TestEditor.h"
#include "GameExtension.h"				
#include "Save_XML_ToMap.h"				
#include "AI_Scripts.h"
#include "ClassEditorsRegistration.h"
#include "ActiveZone.h"

#include "vui_Action.h"
#include "vui_Effects.h"

#include "AlertOnMiniMap.h"

// MANOWAR
#include "mp3\oggvor.h"

// MESSAGES
#include "HelpSystem.h"
#include ".\cvi_RomeHelp.h"
#include ".\cvi_InGamePanel.h"

// Lua ///////////////////////////////////////////////////////////////////
#include	<lua_define.hpp>
#ifdef  __LUA__ 
	#include <lua_header.h>
	#ifdef  __LUA_DEBUGGER__ 
		#include <lua_debugger.h>
	#endif//__LUA_DEBUGGER__
#endif//__LUA__

// BATTLE EDITOR
#include "ClassDeffIDS.h"				

#include "MessageMgr.h"

#include "BE_BaseClasses.h"

#include "BE_OPERATIONS.h"

#include "DataStorageXML.h"				
#include "BattlePainter.h"
#include "ProcessBattleEditor.h"

// BE2
#include "ClassTypeList.h"
#include "BE_Value.h"
#include "BE_Block.h"
#include "BE_Global.h"

// std
#include <stdio.h>
#include <stdarg.h>


