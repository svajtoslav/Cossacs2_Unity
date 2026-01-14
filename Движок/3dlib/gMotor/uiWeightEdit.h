/*****************************************************************************/
/*	File:	uiWeightEdit.h
/*	Desc:	Weight [0..1] value editor
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-12-2003
/*****************************************************************************/
#ifndef __UIWEIGHTEDIT_H__
#define __UIWEIGHTEDIT_H__

namespace sg{
/*****************************************************************************/
/*	Class:	WeightEdit 
/*	Desc:	Weight [0..1] value editor
/*****************************************************************************/
class WeightEdit : public Control
{
	bool			m_bDrag;
	float			m_Weight;

	DWORD			m_MarksColor;
	DWORD			m_SeparatorColor;
	DWORD			m_SliderColor;
	DWORD			m_DigitColor;

	Rct				m_SliderExt;
	bool			m_bAutoHide;

public:
					WeightEdit			();

	float			GetWeight			() const { return m_Weight; }
	void			SetWeight			( float val ) { m_Weight = val; }
	void			SetDrag				( bool bDrag = true ) { m_bDrag = bDrag; }

	virtual	void	Render				();
	virtual	void	Expose				( PropertyMap& pm );
	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );
	virtual bool 	OnMouseLButtonDown	( int mX, int mY );		
	virtual bool	OnMouseLButtonUp	( int mX, int mY );

	NODE(WeightEdit,Control,WEET);
}; // class WeightEdit

} // namespace sg

#endif // __UIRAMPEDIT_H__