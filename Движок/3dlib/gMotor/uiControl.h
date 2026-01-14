/*****************************************************************************/
/*	File:	uiControl.h
/*	Desc:	Scene graph UI controls
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-20-2003
/*****************************************************************************/
#ifndef __UICONTROL_H__
#define __UICONTROL_H__

namespace sg{

class Font;
/*****************************************************************************/
/*	Class:	Control
/*	Desc:	Base UI control class
/*****************************************************************************/
class Control : public HudNode, public InputDispatcher
{
public:
						Control();
	virtual				~Control();

	void				Render();
	virtual void		RenderNonClientArea(); 

	Rct					GetExtents		() const { return Rct( pos.x, pos.y, width, height ); }
	void				SetExtents		( float _x, float _y, float _w, float _h );
	void				SetExtents		( const Rct& rct );

	_inl DWORD			GetClrTop		() const { return m_ClrTop; }
	_inl DWORD			GetClrMdl		() const { return m_ClrMdl; }
	_inl DWORD			GetClrBot		() const { return m_ClrBot; }

	_inl void			SetClrTop		( DWORD val ) { m_ClrTop = val; }
	_inl void			SetClrMdl		( DWORD val ) { m_ClrMdl = val; }
	_inl void			SetClrBot		( DWORD val ) { m_ClrBot = val; }

	virtual	void		Expose			( PropertyMap& pm );
	virtual void		Serialize		( OutStream& os ) const;
	virtual void		Unserialize		( InStream& is );

	virtual void		OnDrop			( int mX, int mY, DWORD ctx, DWORD obj ){}

	Rct					ClientToScreen	( const Rct& rct ) const;
	Rct					ScreenToClient	( const Rct& rct ) const;

	void				ClientToScreen	( float& x, float& y ) const;
	void				ScreenToClient	( float& x, float& y ) const;

	virtual _inl Rct	GetClientRect	() const { return GetExtents(); }

	//  i don't like the drag, but the drag likes me
	void				BeginDrag		( float mx, float my );
	void				OnDrag			( float mx, float my );
	void				EndDrag			( float mx, float my );
	bool				IsDragged		() const { return m_bDragged; }
	void				EnableDrag		( bool val = true ) { m_bEnableDrag = val; } 

	Font*				GetParentFont	() const;
	Font*				GetFont			();
	void				SetFont			( Font* pFont ) { m_pFont = pFont; }

	_inl DWORD			GetFgColor		() const { return m_ClrFg; }
	_inl void			SetFgColor		( DWORD val ) { m_ClrFg = val; }

	NODE(Control,HudNode,CTRL);

protected:
	DWORD				m_ClrTop;
	DWORD				m_ClrMdl;
	DWORD				m_ClrBot;
	DWORD				m_ClrFg;

	Rct					m_ClientRct;
	
	float				m_DragX, m_DragY;
	bool				m_bDragged;
	bool				m_bEnableDrag;

private:
	Font*				m_pFont;

}; // class Control

/*****************************************************************************/
/*	Class:	Dialog
/*	Desc:	UI dialog class
/*****************************************************************************/
class Dialog : public Control
{
public:
						Dialog();
	virtual				~Dialog();

	virtual _inl Rct	GetClientRect			() const;
	_inl Rct			GetHeaderRect			() const;

	_inl const char*	GetFontName				() const { return m_FontName.c_str(); }  
	_inl void			SetFontName				( const char* val ) { m_FontName = val; }  

	virtual void		RenderNonClientArea		(); 
	virtual bool 		OnMouseLButtonDown		( int mX, int mY );
	virtual bool 		OnMouseMove				( int mX, int mY, DWORD keys );
	virtual bool 		OnMouseLButtonUp		( int mX, int mY );

	virtual bool		OnMouseMButtonDown		( int mX, int mY );
	virtual bool		OnMouseMButtonUp		( int mX, int mY );


	void				Expose					( PropertyMap& pm );

	Font*				GetFont();
	
	NODE(Dialog,Control,DLOG);

protected:
	int					m_HeaderHeight;	
	DWORD				m_ClrHeader;	
	float				m_BorderWidth;
	float				m_BorderHeight;
	
	std::string			m_FontName;
	int					m_FontHeight;

private:
	Font*				m_pFont;

}; // class Dialog

/*****************************************************************************/
/*	Class:	DialogDispatcher, singleton
/*	Desc:	Manages messages flow in the dialogs system
/*****************************************************************************/
class DialogDispatcher : public InputDispatcher
{
public:

	static DialogDispatcher&		instance();

protected:

}; // class DialogDispatcher

/*****************************************************************************/
/*	Class:	TreeControl
/*	Desc:	Well, tree control...
/*****************************************************************************/
class TreeControl : public Control
{
public:
	TreeControl(){}
	virtual			~TreeControl(){}

	NODE(TreeControl,Control,TRCT);
}; // class TreeControl

/*****************************************************************************/
/*	Class:	Thumbnail
/*	Desc:	Model preview control
/*****************************************************************************/
class Thumbnail : public Control
{
public:
	enum ControlMode
	{
		cmNone			= 0,
		cmExhibition	= 1,
		cmMouseRoll		= 2
	}; // enum ControlMode
							
							Thumbnail		();
	virtual void			Render			();
	virtual void			Expose			( PropertyMap& pm );
	
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is );

	void					SetControlMode	( ControlMode mode )		{ m_ControlMode = mode; }
	void					SetViewDir		( const Vector3D& viewDir ) { m_ViewDir = viewDir; }
	void					SetRect			( const Rct& rct );

	void					ClearItems		();
	void					AddItem			( Node* pItem );

	void					SetFOV			( float fov );
	float					GetFOV			() const { return m_FOV; }

	virtual bool			OnMouseMove			( int mX, int mY, DWORD keys );
	virtual bool			OnMouseLButtonDown	( int mX, int mY );
	virtual bool			OnMouseLButtonUp	( int mX, int mY );

	NODE(Thumbnail,Control,THUM);

protected:
	void					UpdateCamera	();

	Group*					m_pModels;
	PerspCamera*			m_pCamera;				
		
	Vector3D				m_ViewDir;
	ControlMode				m_ControlMode;
	float					m_FOV;
	DWORD					m_BackgroundColor;
	float					m_RotationSpeed;
	
	float					m_StartTime;
	Matrix4D				m_Transform;

	bool					m_bMouseRolling;
	float					m_MouseRollPos;
	float					m_RotAngleDelta;

	float					m_MouseRollAngle;
	float					m_Angle;

	float					m_Radius;
	float					m_MinRadius;
}; // class Thumbnail

}; // namespace sg

ENUM( sg::Thumbnail::ControlMode, "ControlMode", 
		en_val( sg::Thumbnail::cmNone,			"None"			) <<
		en_val(  sg::Thumbnail::cmExhibition,	"Exhibition"	) <<
		en_val(  sg::Thumbnail::cmMouseRoll,	"MouseRoll"		) );

namespace sg{

/*****************************************************************************/
/*	Class:	Button
/*	Desc:	Pushbutton
/*****************************************************************************/
class Button : public Control
{
public:
	enum State
	{
		bsUnknown	= 0,
		bsIdle		= 1,
		bsPressed	= 2
	};
	Button() : m_State( bsIdle ) {}
	void				Render();
	void				SetText( const char* txt ) { m_Text = txt; }

	virtual bool		OnMouseLButtonDown( int mX, int mY );
	virtual bool		OnMouseLButtonUp( int mX, int mY );

	NODE(Button,Control,BUTN);

private:
	std::string			m_Text;
	State				m_State;

}; // class Button 

/*****************************************************************************/
/*	Class:	CheckBox
/*****************************************************************************/
class CheckBox : public Control
{
	bool				m_bChecked;

public:

	void				Render();

	NODE(CheckBox,Control,CHBX);

}; // class CheckBox 

/*****************************************************************************/
/*	Class:	EditBox
/*	Desc:	Input text control
/*****************************************************************************/
class EditBox : public Control
{
	std::string			m_Text;

	float				m_TextViewPos;
	int					m_CaretPos;
	DWORD				m_ClrText;
	DWORD				m_ClrCaret;

	DWORD				m_CaretBlinkOn;
	DWORD				m_CaretBlinkOff;

public:
	EditBox();

	void				Render();
	void				SetText( const char* txt ) { m_Text = txt; }
	const char*			GetText() const { return m_Text.c_str(); }

	NODE(EditBox,Control,EDIT);

protected:
	void				RenderCaret();

}; // class EditBox 

}; // namespace sg

#endif // __UICONTROL_H__