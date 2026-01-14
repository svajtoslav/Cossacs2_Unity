/*****************************************************************************/
/*	File:	uiRampEdit.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-12-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"
#include "uiControl.h"
#include "uiWeightEdit.h"
#include "uiRampEdit.h"
#include "kSystemDialogs.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	ColorRampEdit inplementation
/*****************************************************************************/
ColorRampEdit::ColorRampEdit() : m_SelectedKey(-1), m_bDragKey(false)
{
	SetActive( true );
	m_pRamp = false;
}

void ColorRampEdit::Render()
{
	Rct ext = GetExtents();
	if (!m_pRamp)
	{
		rsRect( ext, GetPosZ(), 0xFFFF0000 );
		rsFlushPoly2D();
		return;
	}

	int nK = m_pRamp->GetNKeys();
	
	//  draw gradient bar
	for (int i = 1; i < nK; i++)
	{
		DWORD c0 = m_pRamp->GetKey( i - 1 );
		DWORD c1 = m_pRamp->GetKey( i );
		float t0 = m_pRamp->GetKeyTime( i - 1 );
		float t1 = m_pRamp->GetKeyTime( i );
		Rct rct( ext.x + ext.w*t0, ext.y, ext.w*(t1 - t0), ext.h );
		rsRect( rct, GetPosZ(), c0, c1, c0, c1 );
	}

	//  draw keys
	for (int i = 0; i < nK; i++)
	{
		float t = m_pRamp->GetKeyTime( i );
		Rct rct( ext.x + ext.w*t - 2, ext.y, 4, ext.h );
		//  check whether key is selected
		if (i == m_SelectedKey)
		{
			rsPanel( rct, GetPosZ(), 0xFFFFFFFF, 0xFFFF0000, 0xFF848284 );
		}
		else
		{
			rsPanel( rct, GetPosZ() );
		}
	}
	
	rsFlushPoly2D();
} // ColorRampEdit::Render

bool ColorRampEdit::AskColor( DWORD& col )
{
	DWORD alpha = 0xFF000000&col;
	PickColorDialog dlg( col );
	if (dlg.Show())
	{
		col = dlg.GetColor();
		col &= 0x00FFFFFF;
		col |= alpha;
		return true;
	}
	return false;
} // ColorRampEdit::AskColor

float ColorRampEdit::GetTimeInPoint( int mX, int mY )
{
	Rct ext = GetExtents();
	if (ext.w < c_SmallEpsilon) return -1.0f;
	return (mX - ext.x)/ext.w;
} // ColorRampEdit::GetWeight

int	ColorRampEdit::GetKey( int mX, int mY )
{
	if (!m_pRamp) return -1;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) return -1;
	float t = GetTimeInPoint( mX, mY );
	
	int nK = m_pRamp->GetNKeys();
	for (int i = 1; i < nK; i++)
	{
		float t0 = m_pRamp->GetKeyTime( i - 1 );
		float t1 = m_pRamp->GetKeyTime( i );
		if (t < t0 || t > t1) continue;
		if (fabs( t - t0 ) < fabs( t - t1 )) return i - 1; else return i; 
	}
	return -1;
} // ColorRampEdit::GetKey

bool ColorRampEdit::OnMouseMove( int mX, int mY, DWORD keys )
{
	if (!m_pRamp) return false;
	if (m_bDragKey)
	{
		if ( m_SelectedKey <= 0 || m_SelectedKey >= m_pRamp->GetNKeys() - 1) return false;
		float w = GetTimeInPoint( mX, mY );
		float wl = m_pRamp->GetKeyTime( m_SelectedKey - 1 );
		float wr = m_pRamp->GetKeyTime( m_SelectedKey + 1 );
		if (w > wl && w < wr) m_pRamp->SetKeyTime( m_SelectedKey, w );
	}
	return false;
} // ColorRampEdit::OnMouseMove

bool ColorRampEdit::OnMouseLButtonDown( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) { m_bDragKey = false; return false; }
	int cKey = GetKey( mX, mY );
	if (cKey != -1) 
	{
		m_SelectedKey = cKey;
		m_bDragKey = true;
	}
	return false;
} // ColorRampEdit::OnMouseLButtonDown

bool ColorRampEdit::OnMouseLButtonDblclk( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) { return false; }

	int cKey = GetKey( mX, mY );
	if (cKey == -1) return false;
	m_SelectedKey = cKey;
	DWORD color = m_pRamp->GetKey( m_SelectedKey );
	if (AskColor( color ))
	{
		m_pRamp->SetKey( m_SelectedKey, color );
	}
	return false;
} // ColorRampEdit::OnMouseLButtonDblclk

bool ColorRampEdit::OnMouseRButtonDown( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) { return false; }

	float t = GetTimeInPoint( mX, mY );
	if (t < 0.0f || t > 1.0f) return false;
	DWORD color = 0xFFFFFFFF;
	if (AskColor( color )) m_pRamp->AddKey( t, color );
	return false;
} // ColorRampEdit::OnMouseRButtonDown

bool ColorRampEdit::OnMouseLButtonUp( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	/*if (m_bDragKey && !ext.PtIn( mX, mY ))
	{
		m_pRamp->DeleteKey( m_SelectedKey );
	}*/
	m_bDragKey = false;
	return false;
} // ColorRampEdit::OnMouseLButtonUp

bool ColorRampEdit::OnKeyDown( DWORD keyCode, DWORD flags )
{
    if (keyCode == VK_BACK) m_pRamp->DeleteKey( m_SelectedKey );
	return false;
} // ColorRampEdit::OnKeyDown


/*****************************************************************************/
/*	AlphaRampEdit inplementation
/*****************************************************************************/
AlphaRampEdit::AlphaRampEdit() : m_SelectedKey(-1), m_bDragKey(false)
{
	SetActive( true );
	m_pRamp = false;
}

void AlphaRampEdit::Render()
{
	Rct ext = GetExtents();
	if (!m_pRamp)
	{
		rsRect( ext, GetPosZ(), 0xFFFF0000 );
		rsFlushPoly2D();
		return;
	}
	
	if (!m_WeightEdit.IsInvisible()) m_pRamp->SetKey( m_SelectedKey, m_WeightEdit.GetWeight() );

	int nK = m_pRamp->GetNKeys();

	//  draw gradient bar
	for (int i = 1; i < nK; i++)
	{
		float a0 = m_pRamp->GetKey( i - 1 );
		float a1 = m_pRamp->GetKey( i );
		ColorValue c0( 1.0f, a0, a0, a0 );
		ColorValue c1( 1.0f, a1, a1, a1 );
		float t0 = m_pRamp->GetKeyTime( i - 1 );
		float t1 = m_pRamp->GetKeyTime( i );
		Rct rct( ext.x + ext.w*t0, ext.y, ext.w*(t1 - t0), ext.h );
		rsRect( rct, GetPosZ(), c0, c1, c0, c1 );
		rsLine( ext.x + ext.w*t0, ext.y + ext.h*(1.0f - a0), 
				ext.x + ext.w*t1, ext.y + ext.h*(1.0f - a1), GetPosZ(), 0xFFFF6666 );
	}

	//  draw keys
	for (int i = 0; i < nK; i++)
	{
		float t = m_pRamp->GetKeyTime( i );
		Rct rct( ext.x + ext.w*t - 2, ext.y, 4, ext.h );
		//  check whether key is selected
		if (i == m_SelectedKey)
		{
			rsPanel( rct, GetPosZ(), 0xFFFFFFFF, 0xFFFF0000, 0xFF848284 );
		}
		else
		{
			rsPanel( rct, GetPosZ() );
		}
	}

	rsFlushPoly2D();

	m_WeightEdit.Render();
} // AlphaRampEdit::Render

void AlphaRampEdit::AskWeight( float& w, int mX, int mY )
{
	m_WeightEdit.SetWeight( w );
	m_WeightEdit.SetExtents( mX, mY, 13, 120 );
	m_WeightEdit.SetDrag();
	m_WeightEdit.SetInvisible( false );
} // AlphaRampEdit::AskWeight

float AlphaRampEdit::GetTimeInPoint( int mX, int mY )
{
	Rct ext = GetExtents();
	if (ext.w < c_SmallEpsilon) return -1.0f;
	return (mX - ext.x)/ext.w;
} // AlphaRampEdit::GetWeight

int	AlphaRampEdit::GetKey( int mX, int mY )
{
	if (!m_pRamp) return -1;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) return -1;
	float t = GetTimeInPoint( mX, mY );

	int nK = m_pRamp->GetNKeys();
	for (int i = 1; i < nK; i++)
	{
		float t0 = m_pRamp->GetKeyTime( i - 1 );
		float t1 = m_pRamp->GetKeyTime( i );
		if (t < t0 || t > t1) continue;
		if (fabs( t - t0 ) < fabs( t - t1 )) return i - 1; else return i; 
	}
	return -1;
} // AlphaRampEdit::GetKey

bool AlphaRampEdit::OnMouseMove( int mX, int mY, DWORD keys )
{
	if (!m_pRamp) return false;
	if (m_bDragKey)
	{
		if ( m_SelectedKey <= 0 || m_SelectedKey >= m_pRamp->GetNKeys() - 1) return false;
		float w = GetTimeInPoint( mX, mY );
		float wl = m_pRamp->GetKeyTime( m_SelectedKey - 1 );
		float wr = m_pRamp->GetKeyTime( m_SelectedKey + 1 );
		if (w > wl && w < wr) m_pRamp->SetKeyTime( m_SelectedKey, w );
	}
	return false;
} // AlphaRampEdit::OnMouseMove

bool AlphaRampEdit::OnMouseLButtonDown( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) { m_bDragKey = false; return false; }
	int cKey = GetKey( mX, mY );
	if (cKey != -1) 
	{
		m_SelectedKey = cKey;
		m_bDragKey = true;
	}
	return false;
} // AlphaRampEdit::OnMouseLButtonDown

bool AlphaRampEdit::OnMouseLButtonDblclk( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) { return false; }

	int cKey = GetKey( mX, mY );
	if (cKey == -1) return false;
	m_SelectedKey = cKey;
	float w = m_pRamp->GetKey( m_SelectedKey );
	AskWeight( w, mX, mY );
	return false;
} // AlphaRampEdit::OnMouseLButtonDblclk

bool AlphaRampEdit::OnMouseRButtonDown( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	if (!ext.PtIn( mX, mY )) { return false; }

	float t = GetTimeInPoint( mX, mY );
	if (t < 0.0f || t > 1.0f) return false;
	float w = 1.0f;
	m_SelectedKey = m_pRamp->AddKey( t, w );
	AskWeight( w, mX, mY ); 
	return false;
} // AlphaRampEdit::OnMouseRButtonDown

bool AlphaRampEdit::OnMouseLButtonUp( int mX, int mY )
{
	if (!m_pRamp) return false;
	Rct ext = GetExtents();
	/*if (m_bDragKey && !ext.PtIn( mX, mY ))
	{
		m_pRamp->DeleteKey( m_SelectedKey );
	}*/
	m_bDragKey = false;
	return false;
} // AlphaRampEdit::OnMouseLButtonUp

bool AlphaRampEdit::OnKeyDown( DWORD keyCode, DWORD flags )
{
    if (keyCode == VK_BACK) m_pRamp->DeleteKey( m_SelectedKey );
	return false;
} // AlphaRampEdit::OnKeyDown

END_NAMESPACE(sg)

