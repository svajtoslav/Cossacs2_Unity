/*****************************************************************
/*  File:   kInput.h                                             *
/*  Desc:   Input dispatching manager                            *
/*  Author: Silver, Copyright (C) GSC Game World                 *
/*  Date:   Mar 2002                                             *
/*****************************************************************/
#ifndef __KINPUT_H__
#define __KINPUT_H__

/*****************************************************************************/
/*	Class:	InputDispatcher
/*****************************************************************************/
class InputDispatcher
{
public:
	InputDispatcher();
	virtual	~InputDispatcher();  

	virtual bool 	OnMouseWheel		( int delta )					{ return false; }
	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys )	{ return false; }
	virtual bool 	OnMouseLButtonDown	( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseMButtonDown	( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseRButtonDown	( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseLButtonUp	( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseMButtonUp	( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseRButtonUp	( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseRButtonDblclk( int mX, int mY )				{ return false; }
	virtual bool 	OnMouseMButtonDblclk( int mX, int mY )				{ return false; }
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags )	{ return false; }
	virtual bool	OnChar				( DWORD charCode, DWORD flags )	{ return false; }
	virtual bool	OnKeyUp				( DWORD keyCode, DWORD flags )	{ return false; }
	virtual void	OnUpdate			()								{ return; }
	virtual void	OnDraw				()								{ return; }
	virtual void	OnInit				()								{ return; }
	virtual void	OnActivate			( bool activate = true )		{ return; }


	static _inl bool 	MouseWheel		( int delta );					
	static _inl bool 	MouseMove		( int mX, int mY, DWORD keys );
	static _inl bool 	MouseLButtonDown( int mX, int mY );			
	static _inl bool 	MouseMButtonDown( int mX, int mY );			
	static _inl bool 	MouseRButtonDown( int mX, int mY );				
	static _inl bool 	MouseLButtonUp	( int mX, int mY );				
	static _inl bool 	MouseMButtonUp	( int mX, int mY );				
	static _inl bool 	MouseRButtonUp	( int mX, int mY );				
	static _inl bool 	MouseLButtonDblclk( int mX, int mY );				
	static _inl bool 	MouseRButtonDblclk( int mX, int mY );	
	static _inl bool 	MouseMButtonDblclk( int mX, int mY );
	static _inl bool	KeyDown			( DWORD keyCode, DWORD flags );	
	static _inl bool	Char			( DWORD charCode, DWORD flags );	
	static _inl bool	KeyUp			( DWORD keyCode, DWORD flags );	

	static _inl void	Update			();	
	static _inl void	Draw			();	
	static _inl void	Init			();

	_inl void			SetActive( bool _active = true );
	_inl bool			IsActive() const;

	static _inl void	SetCoreDispatcher( InputDispatcher* _pDisp );

protected:
	static InputDispatcher*		pDisp;
	BOOL						bActive;

}; // class InputDispatcher

/*****************************************************************************/
/*	Class:	InputManager
/*	Desc:	Generic input manager	
/*****************************************************************************/
class InputManager : public InputDispatcher
{
	std::vector<InputDispatcher*>	disp;

public:
	virtual bool 	OnMouseWheel		( int delta );
	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );
	virtual bool 	OnMouseLButtonDown	( int mX, int mY );
	virtual bool 	OnMouseMButtonDown	( int mX, int mY );
	virtual bool 	OnMouseRButtonDown	( int mX, int mY );
	virtual bool 	OnMouseLButtonUp	( int mX, int mY );
	virtual bool 	OnMouseMButtonUp	( int mX, int mY );
	virtual bool 	OnMouseRButtonUp	( int mX, int mY );
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY );
	virtual bool 	OnMouseRButtonDblclk( int mX, int mY );
	virtual bool 	OnMouseMButtonDblclk( int mX, int mY );
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags );
	virtual bool	OnChar				( DWORD keyCode, DWORD flags );
	virtual bool	OnKeyUp				( DWORD keyCode, DWORD flags );

	virtual void	OnUpdate			();
	virtual void	OnDraw				();
	virtual void	OnInit				();

	static bool		AddDispatcher( InputDispatcher* iDisp );
	static bool		AddDispatcher( InputDispatcher* iDisp, int priority );
	static bool		DelDispatcher( InputDispatcher* iDisp );

	static InputManager&	instance();

private:
	InputManager(){ bActive = true; }
	
	bool			_AddDispatcher( InputDispatcher* iDisp );
	bool			_AddDispatcher( InputDispatcher* iDisp, int priority );

	bool			_DelDispatcher( InputDispatcher* iDisp );

}; // class InputManager

#ifdef _INLINES
#include "kInput.inl"
#endif // _INLINES

#endif // __KINPUT_H__
