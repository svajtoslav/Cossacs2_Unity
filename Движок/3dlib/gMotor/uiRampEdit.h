/*****************************************************************************/
/*	File:	uiRampEdit.h
/*	Desc:	Color/alpha ramp editors
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-12-2003
/*****************************************************************************/
#ifndef __UIRAMPEDIT_H__
#define __UIRAMPEDIT_H__

namespace sg{
/*****************************************************************************/
/*	Class:	ColorRampEdit 
/*	Desc:	Editor of the color ramp values
/*****************************************************************************/
class ColorRampEdit : public Control
{
	ColorRamp*		m_pRamp;
	int				m_SelectedKey;
	bool			m_bDragKey;

public:
					ColorRampEdit		();
	virtual	void	Render				();

	void			SetRamp				( ColorRamp* pRamp ) { m_pRamp = pRamp; }
	ColorRamp*		GetRamp				() const { return m_pRamp; }
	
	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );
	virtual bool 	OnMouseLButtonDown	( int mX, int mY );		
	virtual bool	OnMouseLButtonUp	( int mX, int mY );
	virtual bool 	OnMouseRButtonDown	( int mX, int mY );			
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags );
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY );


	NODE(ColorRampEdit,Control,CORE);

protected:
	bool			AskColor			( DWORD& col );
	int				GetKey				( int mX, int mY );
	float			GetTimeInPoint		( int mX, int mY );

}; // class ColorRampEdit

/*****************************************************************************/
/*	Class:	AlphaRampEdit 
/*	Desc:	Editor of the color ramp values
/*****************************************************************************/
class AlphaRampEdit : public Control
{
	AlphaRamp*		m_pRamp;
	int				m_SelectedKey;
	bool			m_bDragKey;

	WeightEdit		m_WeightEdit;

public:
					AlphaRampEdit		();
	virtual	void	Render				();

	void			SetRamp				( AlphaRamp* pRamp ) { m_pRamp = pRamp; }
	AlphaRamp*		GetRamp				() const { return m_pRamp; }

	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );
	virtual bool 	OnMouseLButtonDown	( int mX, int mY );		
	virtual bool	OnMouseLButtonUp	( int mX, int mY );
	virtual bool 	OnMouseRButtonDown	( int mX, int mY );			
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags );
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY );


	NODE(AlphaRampEdit,Control,ALRE);

protected:
	void			AskWeight			( float& w, int mX, int mY );
	int				GetKey				( int mX, int mY );
	float			GetTimeInPoint		( int mX, int mY );

}; // class AlphaRampEdit

} // namespace sg

#endif // __UIRAMPEDIT_H__