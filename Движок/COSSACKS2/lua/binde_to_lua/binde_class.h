
#pragma	once

// class C++ -> Lua //////////////////////////////////////////////////////

// binde lvCNode to lua
void	bind_lvCNode(lua_State* L);

// binde lvCGroup to lua
void	bind_lvCGroup(lua_State* L);

// binde GroupMAP to lua
void	bind_groupMAP(lua_State* L);

// binde all vvBASE inherit class
void	bind_valuesMAP(lua_State* L);

// binde all lvCGraphObject inherit class
void	bind_GraphObjMAP(lua_State* L);

//////////////////////////////////////////////////////////////////////////

