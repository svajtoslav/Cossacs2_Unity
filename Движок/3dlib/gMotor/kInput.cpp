/*****************************************************************************/
/*	File:	kInput.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.05.2003
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"

#ifndef _INLINES
#include "kInput.inl"
#endif // _INLINES

/*****************************************************************************/
/*	InputDispatcher implementation
/*****************************************************************************/
InputDispatcher* InputDispatcher::pDisp = NULL;
InputDispatcher::InputDispatcher() : bActive( false )
{ 
	InputManager::AddDispatcher( this ); 
}

InputDispatcher::~InputDispatcher()  
{ 
	InputManager::DelDispatcher( this ); 
}

/*****************************************************************************/
/*	InputManager implementation
/*****************************************************************************/
InputManager& InputManager::instance()
{
	static InputManager me;
	return me;
}

bool InputManager::OnMouseWheel( int delta )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseWheel( delta )) return true;
	}
	return false;
} // InputManager::OnMouseWheel

bool InputManager::OnMouseMove( int mX, int mY, DWORD keys )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		InputDispatcher* pDisp = disp[i];
		assert( pDisp );
		if (!pDisp->IsActive()) continue;
		if (pDisp->OnMouseMove( mX, mY, keys )) return true;
	}
	return false;
} // InputManager::OnMouseMove

bool InputManager::OnMouseLButtonDown( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseLButtonDown( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseLButtonDown

bool InputManager::OnMouseMButtonDown( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseMButtonDown( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseMButtonDown

bool InputManager::OnMouseRButtonDown( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseRButtonDown( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseRButtonDown

bool InputManager::OnMouseLButtonUp( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseLButtonUp( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseLButtonUp

bool InputManager::OnMouseMButtonUp( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseMButtonUp( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseMButtonUp

bool InputManager::OnMouseRButtonUp( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseRButtonUp( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseRButtonUp

bool InputManager::OnMouseLButtonDblclk( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseLButtonDblclk( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseLButtonDblclk

bool InputManager::OnMouseMButtonDblclk( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseMButtonDblclk( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseMButtonDblclk

bool InputManager::OnMouseRButtonDblclk( int mX, int mY )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnMouseRButtonDblclk( mX, mY )) return true;
	}
	return false;
} // InputManager::OnMouseRButtonDblclk

bool InputManager::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnKeyDown( keyCode, flags )) return true;
	}
	return false;
} //  InputManager::OnKeyDown

bool InputManager::OnChar( DWORD charCode, DWORD flags )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		if (disp[i]->OnChar( charCode, flags )) return true;
	}
	return false;
} //  InputManager::OnChar

bool InputManager::OnKeyUp( DWORD keyCode, DWORD flags )
{
	if (!bActive) return false;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive() || disp[i] == this) continue;
		if (disp[i]->OnKeyUp( keyCode, flags )) return true;
	}
	return false;
} //  InputManager::OnKeyUp

void InputManager::OnUpdate()
{
	if (!bActive) return;
	//Log.Info( "There is %d of us there.", disp.size() );
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		disp[i]->OnUpdate();
	}
} //  InputManager::OnUpdate

void InputManager::OnDraw()
{
	if (!bActive) return;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		if (!disp[i]->IsActive()) continue;
		disp[i]->OnDraw();
	}
} //  InputManager::OnDraw

void InputManager::OnInit()
{
	if (!bActive) return;
	for (int i = 0; i < disp.size(); i++)
	{
		assert( disp[i] );
		disp[i]->OnInit();
	}
} //  InputManager::OnInit

bool InputManager::AddDispatcher( InputDispatcher* iDisp )
{
	return instance()._AddDispatcher( iDisp );
} // InputManager::AddDispatcher

bool InputManager::AddDispatcher( InputDispatcher* iDisp, int priority )
{
	return instance()._AddDispatcher( iDisp, priority );
} // InputManager::AddDispatcher

bool InputManager::DelDispatcher( InputDispatcher* iDisp )
{
	return instance()._DelDispatcher( iDisp );
} // InputManager::DelDispatcher

bool InputManager::_AddDispatcher( InputDispatcher* iDisp )
{
	for (int i = 0; i < disp.size(); i++)
	{
		if (disp[i] == iDisp)
		{
			return false;
		}
	}
	disp.push_back( iDisp );
	return true;
} // InputManager::_AddDispatcher

bool InputManager::_AddDispatcher( InputDispatcher* iDisp, int priority )
{
	for (int i = 0; i < disp.size(); i++)
	{
		if (disp[i] == iDisp)
		{
			disp.erase( disp.begin() + i );
			break;
		}
	}

	disp.insert( disp.begin() + priority, iDisp );
	return true;
} // InputManager::_AddDispatcher

bool InputManager::_DelDispatcher( InputDispatcher* iDisp )
{
	for (int i = 0; i < disp.size(); i++)
	{
		if (disp[i] == iDisp)
		{
			disp.erase( disp.begin() + i );
			return true; 
		}
	}
	return false;
} // InputManager::_DelDispatcher