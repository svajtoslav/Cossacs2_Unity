/*****************************************************************************/
/*	File:	sgNodeResMgr.hpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/
#ifndef __SGNODERESMGR_HPP__
#define __SGNODERESMGR_HPP__

namespace sg{
/*****************************************************************************/
/*	Class:	NodeResMgr
/*	Desc:	Teplate for resource managers of concrete types, e.g 
/*				models, textures, fonts, shaders, stateblocks, sounds,
/*				animations, materials etc.
/*****************************************************************************/
template <class TRes>
class NodeResMgr : public Node
{
public:

}; // class NodeResMgr

}; // namespace sg

#endif // __SGNODERESMGR_HPP__
