/*****************************************************************************/
/*	File:	uiPalette.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __UIPALETTE_H__
#define __UIPALETTE_H__

namespace sg{
/*****************************************************************************/
/*	Class:	Palette
/*	Desc:	
/*****************************************************************************/
class Palette : public Window
{
public:
						Palette();
	virtual void		Render			();
	virtual void		Expose			( PropertyMap& pm );
	NODE(Palette, Window, UIPL);
}; // class Palette

}; // namespace sg

#endif // __UIPALETTE_H__