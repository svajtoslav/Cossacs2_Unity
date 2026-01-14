/*****************************************************************************/
/*	File:	uiRenderFarm.h
/*	Desc:	Render binning visualisation node
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-12-2003
/*****************************************************************************/
#ifndef __UIRENDERFARM_H__
#define __UIRENDERFARM_H__

namespace sg{
/*****************************************************************************/
/*	Class:	RenderFarm
/*	Desc:	Interactive visualisator of the render binning voodoo
/*****************************************************************************/
class RenderFarm : public Dialog
{
public:
					RenderFarm();
	
	virtual void	Render();
	virtual void	Expose( PropertyMap& pm );

	NODE(RenderFarm,Dialog,FARM);
}; // class RenderFarm

} // namespace sg

#endif // __UIRENDERFARM_H__