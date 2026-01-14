/*****************************************************************************/
/*	File:	sgHardwareCaps.h
/*	Desc:	Device capabilities description node
/*	Author:	Ruslan Shestopalyuk
/*	Date:	11-24-2003
/*****************************************************************************/
#ifndef __SGHARDWARECAPS_H__
#define __SGHARDWARECAPS_H__

namespace sg{
/*****************************************************************************/
/*	Class:	HardwareCaps
/*	Desc:	
/*****************************************************************************/
class HardwareCaps : public Node, public PSingleton<HardwareCaps>
{

public:
	HardwareCaps();

	NODE(HardwareCaps,Node,HARC);
}; // class HardwareCaps

}; // namespace sg

#endif // __SGHARDWARECAPS_H__