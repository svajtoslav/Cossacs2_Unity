/*****************************************************************************/
/*	File:	uiPropEditors.cpp
/*	Desc:	Standard set of property editors realization
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-13-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kPropertyMap.h"
#include "kInput.h"
#include "sgFont.h"
#include "uiControl.h"
#include "uiObjectInspector.h"
#include "sgFont.h"
#include "uiTrackEdit.h"
#include "uiWeightEdit.h"
#include "uiRampEdit.h"
#include "uiPropEditors.h"

#include "commdlg.h"
#include "kSystemDialogs.h"
#include "sgApplication.h"

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	PropertyEditorEx implementation
/*****************************************************************************/
void PropertyEditorEx::Render()
{
	if (!m_pMember) return;
	static const int c_BufSize = 256;
	char buf[c_BufSize];
	bool res = m_pMember->ToString( m_pEditedNode, buf, c_BufSize );

	Rct rct = GetExtents();
	
	Font* pFont		= GetFont();
	if (res && pFont) 
	{
		DWORD fgColor	= GetFgColor();
		Vector3D pos( rct.x + rct.h + 3, rct.y, GetPosZ() );
		pFont->AddString( pos, buf, fgColor );
	}
	
	//  button
	if (!m_pButton)
	{
		m_pButton = FindChild<Button>( "ExButton" );
		if (!m_pButton) m_pButton = AddChild<Button>( "ExButton" );
		m_pButton->SetText( "..." );
	}
	
	rct.w = rct.h + 2;
	m_pButton->SetExtents( rct );

	Node::Render();	
} // PropertyEditorEx::Render

bool PropertyEditorEx::OnMouseLButtonDown( int mX, int mY )
{
	Rct rct( GetExtents() );
	rct.w = rct.h;
	if (!rct.PtIn( mX, mY )) return false;
	if (m_pButton) m_pButton->OnMouseLButtonDown( mX, mY );
	return false;
} // PropertyEditorEx::OnMouseLButtonDown

bool PropertyEditorEx::OnMouseLButtonUp( int mX, int mY )
{
	if (m_pButton) m_pButton->OnMouseLButtonUp( mX, mY );
	return false;
} // PropertyEditorEx::OnMouseLButtonUp

/*****************************************************************************/
/*	StringEditor implementation
/*****************************************************************************/
void StringEditor::Render()
{
	if (m_pEdit)
	{
		m_pEdit->SetExtents( GetExtents() );
		m_pEdit->Render();
		return;
	}
	
	if (!m_pMember) return;
	static const int c_BufSize = 1024;
	char buf[c_BufSize];
	bool res = m_pMember->ToString( m_pEditedNode, buf, c_BufSize );
	if (!res) return;

	Font* pFont		= GetFont();
	DWORD fgColor	= GetFgColor();

	if (!pFont) return;
	
	Rct rct = GetExtents();
	Vector3D pos( rct.x + 2, rct.y, GetPosZ() );
	pFont->AddString( pos, buf, fgColor );
    
    if (HasFocus())
    {
        DWORD tick = GetTickCount()/500;
        if (tick&1)
        {
            float len = pFont->GetStringWidth( buf );
            rsLine( pos.x + len, rct.y, pos.x + len, rct.GetBottom() - 1, 0.0f, 0xFF000000, 0xFF000000 );
        }
    }

	Node::Render();
} // StringEditor::Render

bool StringEditor::OnMouseLButtonDown( int mX, int mY )
{return false;
	if (m_pEdit || !m_pMember || m_pMember->IsReadonly()) return false;
	m_pEdit = new EditBox();
	m_pEdit->SetExtents( GetExtents() );

	static const int c_BufSize = 1024;
	char buf[c_BufSize];
	bool res = m_pMember->ToString( m_pEditedNode, buf, c_BufSize );
	if (!res) return false;

	Font* pFont		= GetFont();
	DWORD fgColor	= GetFgColor();

	if (!pFont) return false;

	m_pEdit->SetFont( pFont );
	m_pEdit->SetText( buf );
	return false;
} // StringEditor::OnMouseLButtonDown

bool StringEditor::OnChar( DWORD charCode, DWORD flags )
{
	if (!m_pMember) return false;
	static const int c_BufSize = 1024;
	char buf[c_BufSize];
	bool res = m_pMember->ToString( m_pEditedNode, buf, c_BufSize );
	if (!res) return false;
	int len = strlen( buf );

	if (charCode == VK_DELETE)
	{
		buf[0] = 0;
	} 
    else if (charCode == VK_BACK)
	{
		if (len > 0) buf[len - 1] = 0;
	}
	else if (isalpha( charCode ) || isdigit( charCode ) || 
        charCode == '|' || charCode == '.' || charCode == '_')
	{
		if (len < c_BufSize)
		{
			buf[len++] = charCode;
			buf[len] = 0;
		}
	}

	 res = m_pMember->FromString( m_pEditedNode, buf );

	 return false;
} // StringEditor::OnChar


bool StringEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_RETURN)
	{
		if (!m_pMember || !m_pEditedNode || !m_pEdit) return false;
		m_pMember->FromString( m_pEditedNode, m_pEdit->GetText() );
		delete m_pEdit;
		m_pEdit = NULL;
	}
	return false;
} // StringEditor::OnKeyDown

void StringEditor::OnForceEndEdit()
{
	if (!m_pMember || !m_pEditedNode || !m_pEdit) return;
	m_pMember->FromString( m_pEditedNode, m_pEdit->GetText() );
	delete m_pEdit;
	m_pEdit = NULL;
} // StringEditor::OnForceEndEdit

/*****************************************************************************/
/*	IntegerEditor implementation
/*****************************************************************************/
bool IntegerEditor::OnChar( DWORD charCode, DWORD flags )
{
	if (charCode == '+') Increase();
    else if (charCode == '-') Decrease();
    else if (charCode == '*' && m_pMember && m_pEditedNode) 
    {
        m_pMember->Set( m_pEditedNode, 0 );
    }
    else
    {
        Parent::OnChar( charCode, flags );
    }
    return false;
} // IntegerEditor::OnChar

bool IntegerEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_RIGHT) Increase();
	else if (keyCode == VK_LEFT) Decrease();
    else
    {
        Parent::OnKeyDown( keyCode, flags );
    }
	return false;
} // IntegerEditor::OnKeyDown

void IntegerEditor::Increase()
{
	int val;
	if (!m_pMember || !m_pMember->Get( m_pEditedNode, val )) return;

	if (GetKeyState( VK_CONTROL ) < 0)		val += 10;
	else if (GetKeyState( VK_MENU ) < 0)	val += 100;
	else val++;
	m_pMember->Set( m_pEditedNode, val );
} // IntegerEditor::Increase

void IntegerEditor::Decrease()
{
	int val;
	if (!m_pMember || !m_pMember->Get( m_pEditedNode, val )) return;

	if (GetKeyState( VK_CONTROL ) < 0)		val -= 10;
	else if (GetKeyState( VK_MENU ) < 0)	val -= 100;
	else val--;
	m_pMember->Set( m_pEditedNode, val );
} // IntegerEditor::Decrease

/*****************************************************************************/
/*	FloatEditor implementation
/*****************************************************************************/
bool FloatEditor::OnChar( DWORD charCode, DWORD flags )
{
	if (charCode == '+') Increase();
    else if (charCode == '-') Decrease();
    else if (charCode == '*' && m_pMember && m_pEditedNode) 
    {
        m_pMember->Set( m_pEditedNode, 0.0f );
    }
    else
    {
        Parent::OnChar( charCode, flags );
    }
	return false;
} // FloatEditor::OnChar

bool FloatEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_RIGHT) Increase();
    else if (keyCode == VK_LEFT) Decrease();
    else
    {
        Parent::OnKeyDown( keyCode, flags );
    }
    return false;
} // FloatEditor::OnKeyDown

void FloatEditor::Increase()
{
	float val;
	if (!m_pMember || !m_pMember->Get( m_pEditedNode, val )) return;
	val += GetChangeRatio();
	m_pMember->Set( m_pEditedNode, val );
} // FloatEditor::Increase

void FloatEditor::Decrease()
{
	float val;
	if (!m_pMember || !m_pMember->Get( m_pEditedNode, val )) return;
	val -= GetChangeRatio();
	m_pMember->Set( m_pEditedNode, val );
} // FloatEditor::Decrease

float FloatEditor::GetChangeRatio() const
{
	float ratio = 1.0f;
	float multiplier = (GetKeyState( VK_CONTROL ) < 0) ? 0.1f : 10.0f;

	for (int i = 0; i < 9; i++)
	{
		ratio *= multiplier;
		if (GetKeyState( '1' + i ) < 0) return ratio;
	}
	return 1.0f;
} // FloatEditor::GetChangeRatio

/*****************************************************************************/
/*	BoolEditor implementation
/*****************************************************************************/
void BoolEditor::Render()
{
	if (!m_pMember || !m_pEditedNode) return;
	bool val;
	bool res = m_pMember->Get( m_pEditedNode, val );
	if (!res) return;

	Rct rct = GetExtents();
	rct.w = rct.h;

	
	
	DWORD bgColor = m_pMember->IsReadonly() ? 0xFFAAAAAA : 0xFFEEEEEE;
	rsPanel( rct, GetPosZ(), 0xFF848284, 0xFFEEEEEE, 0xFFFFFFFF );

	rct.Inflate( 1, 2, 2, 1 );
	if (val)
	{
		DWORD checkColor = m_pMember->IsReadonly() ? 0xFF777777 : 0xFF000000;
		rsPanel( rct, GetPosZ(), 0xFFFFFFFF, checkColor, 0xFF848284 );	
	}

	rsFlushPoly2D();

	Node::Render();
} // BoolEditor::Render

bool BoolEditor::OnMouseLButtonDown( int mX, int mY )
{
	if (!m_pMember || !m_pEditedNode) return false;

	Rct rct( GetExtents() );
	rct.w = rct.h;
	if (!rct.PtIn( mX, mY )) return false;

	bool val;
	bool res = m_pMember->Get( m_pEditedNode, val );
	if (!res) return false;
	val = !val;
	res = m_pMember->Set( m_pEditedNode, val );
	return true;
} // BoolEditor::OnMouseLButtonDown

/*****************************************************************************/
/*	ColorSelector implementation
/*****************************************************************************/
void ColorSelector::Render()
{
	if (!m_pMember) return;
	DWORD color;
	bool res = m_pMember->Get( m_pEditedNode, color );
	if (!res) return;

	
	Rct rct( GetExtents() );
	rct.w = rct.h * 2.0f;
	DWORD clrMain = 0xFF000000 | color;
	DWORD a = (color & 0xFF000000);
	DWORD clrAlpha = 0xFF000000 | (a >> 8) | (a >> 16) | (a >> 24);
	rct.Inflate( 1, 1, 1, 1 );
	rsRect( rct, GetPosZ(), clrMain );
	rct.x += rct.w + 1;
	rct.w *= 0.25f;
	rsRect( rct, GetPosZ(), clrAlpha );
	rsFlushPoly2D();

	Font* pFont = GetFont();
	if (pFont)
	{
		char clrStr[16];
		sprintf( clrStr, "%08X", color );
		Vector3D strPos( rct.GetRight() + 3, rct.y, GetPosZ() );
		pFont->AddString( strPos, clrStr, 0xFF000000 );
	}

	Node::Render();
} // ColorSelector::Render

bool ColorSelector::OnMouseLButtonDown( int mX, int mY )
{	
	Parent::OnMouseLButtonDown( mX, mY );
	
	if (!m_pMember) return false;

	Rct rct( GetExtents() );
	rct.w = rct.h * 2.0f;

	if (!rct.PtIn( mX, mY )) return false;

	DWORD color;
	if (!m_pMember->Get( m_pEditedNode, color )) return false;
	
	DWORD alpha = 0xFF000000&color;
	PickColorDialog dlg( color );
	if (dlg.Show())
	{
		color = dlg.GetColor();
		color &= 0x00FFFFFF;
		color |= alpha;
		return m_pMember->Set( m_pEditedNode, color );
	}

	return false;
} // ColorSelector::OnMouseLButtonDown

bool ColorSelector::OnChar( DWORD charCode, DWORD flags )
{
	if (charCode == '+') Increase();
	if (charCode == '-') Decrease();
	return false;
} // ColorSelector::OnKeyDown

bool ColorSelector::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_RIGHT) Increase();
	if (keyCode == VK_LEFT) Decrease();
	return false;
}

void ColorSelector::Increase()
{
	DWORD val;
	if (!m_pMember || !m_pMember->Get( m_pEditedNode, val )) return;
	
	DWORD alpha = (val & 0xFF000000) >> 24;

	if (GetKeyState( VK_CONTROL ) < 0)	alpha = alpha < (255-10) ? alpha + 10 : 255;
	else alpha = alpha < (255-1) ? alpha + 1 : 255;
	
	val &= 0x00FFFFFF;
	val |= (alpha << 24);

	m_pMember->Set( m_pEditedNode, val );
} // ColorSelector::Increase

void ColorSelector::Decrease()
{
	DWORD val;
	if (!m_pMember || !m_pMember->Get( m_pEditedNode, val )) return;
	
	DWORD alpha = (val & 0xFF000000) >> 24;

	if (GetKeyState( VK_CONTROL ) < 0)	alpha = alpha > 10 ? alpha - 10 : 0;
	else alpha = alpha > 0 ? alpha - 1 : 0;

	val &= 0x00FFFFFF;
	val |= (alpha << 24);

	m_pMember->Set( m_pEditedNode, val );
} // ColorSelector::Decrease

/*****************************************************************************/
/*	ColorPicker implementation
/*****************************************************************************/
ColorPicker::ColorPicker()
{
	SetExtents( 300, 300, 100, 100 );
	m_pHexShader = NULL;
}

void ColorPicker::Render()
{
	static BaseMesh hex;
	if (hex.getNVert() == 0)
	{
		const WORD c_HexIdx[] = { 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6, 0, 6, 1, 0 };
		hex.create	( 7, 18, vfTnL, ptTriangleFan );
		hex.setNVert( 7 );
		hex.setNInd ( 18 );
		hex.setIndexPtr( (WORD*)c_HexIdx );
	}

	if (!m_pHexShader) 
	{
		m_pHexShader = AddChild<DeviceStateSet>( "lines3D_blend" );
	}
	
	Rct rc = GetExtents();
	float radius = rc.w * 0.5f;
	VertexTnL* v = (VertexTnL*)hex.getVertexData();
	
	for (int i = 0; i < 6; i++)
	{
	
	}

	m_pHexShader->Render();
	IRS->DrawPrim( hex );
} // ColorPicker::Render

/*****************************************************************************/
/*	FilePathEditor implementation
/*****************************************************************************/
bool FilePathEditor::OnMouseLButtonUp( int mX, int mY )
{
	Parent::OnMouseLButtonUp( mX, mY );

	chdir( GetRootDirectory() );
	chdir( m_Root );
	OpenFileDialog dlg;
	if (dlg.Show() && m_pMember)
	{
		m_pMember->Set( m_pEditedNode, dlg.GetFilePath() );
	}
	chdir( GetRootDirectory() );
	return false;
} // FilePathEditor::OnMouseLButtonUp

/*****************************************************************************/
/*	TextureEditor implementation
/*****************************************************************************/
bool TextureEditor::OnMouseLButtonDown( int mX, int mY )
{
	Parent::OnMouseLButtonDown( mX, mY );
	if (!m_pTextureView)
	{
		m_pTextureView = FindChild<TextureView>( "TextureView" );
		if (!m_pTextureView) m_pTextureView = AddChild<TextureView>( "TextureView" );
	}
	
	if (!m_pMember || !m_pEditedNode) return false;
	int texID = 0;
	if (!m_pMember->Get( m_pEditedNode, texID )) return false;

	m_pTextureView->SetExtents( IRS->GetViewPort() );
	m_pTextureView->SetTexID( texID );
	m_pTextureView->SetInvisible( false );
	return false;
} // TextureEditor::OnMouseLButtonDown

bool TextureEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_ESCAPE)
	{
		if (m_pTextureView) m_pTextureView->SetInvisible();
	}
	return false;
} // TextureView::OnKeyDown

/*****************************************************************************/
/*	TextureView implementation
/*****************************************************************************/
void TextureView::Render()
{
	static BaseMesh bm;
	if (bm.getNVert() == 0)
	{
		bm.create( 4, 0, vfTnL, ptTriangleList );
		bm.setIsQuadList( true );
		bm.setNVert		( 4 );
		bm.setNPri		( 2 );
		bm.setShader	( IRS->GetShaderID( "hud" ) );
	}
	
	VertexTnL* v = (VertexTnL*)bm.getVertexData();
	
	float cX = GetExtents().GetCenterX();
	float cY = GetExtents().GetCenterY();
	Rct ext( cX - m_TD.getSideX() / 2, cY - m_TD.getSideY() / 2, m_TD.getSideX(), m_TD.getSideY() );
	

	Rct pext( ext );
	pext.Inflate( -3, -3, -3, -3 );
	
	rsPanel( pext, GetPosZ(), 0xFFFFFFFF, 0xFF00FFFF, 0xFF848284 );
	rsFlushPoly2D();


	v[0].x = ext.x;
	v[0].y = ext.y;
	v[0].u = 0.0f;
	v[0].v = 0.0f;
	v[0].diffuse = 0xFFFFFFFF;
	v[0].z = GetPosZ();
	v[0].w = 1.0f;

	v[1].x = ext.GetRight();
	v[1].y = ext.y;
	v[1].u = 1.0f;
	v[1].v = 0.0f;
	v[1].diffuse = 0xFFFFFFFF;
	v[1].z = GetPosZ();
	v[1].w = 1.0f;

	v[2].x = ext.x;
	v[2].y = ext.GetBottom();
	v[2].u = 0.0f;
	v[2].v = 1.0f;
	v[2].diffuse = 0xFFFFFFFF;
	v[2].z = GetPosZ();
	v[2].w = 1.0f;

	v[3].x = ext.GetRight();
	v[3].y = ext.GetBottom();
	v[3].u = 1.0f;
	v[3].v = 1.0f;
	v[3].diffuse = 0xFFFFFFFF;
	v[3].z = GetPosZ();
	v[3].w = 1.0f;

	bm.setTexture( m_TexID );

	IRS->Draw( bm );

} // TextureView::Render

void TextureView::SetTexID( int texID )
{
	m_TexID = texID;
	const TextureDescr* pDescr = IRS->GetTextureDescr( texID );
	if (pDescr)
	{
		m_TD = *pDescr;
	}

} // TextureView::SetTexID

/*****************************************************************************/
/*	FloatCurveEditor implementation
/*****************************************************************************/
FloatCurveEditor::FloatCurveEditor()
{
	m_pTrackEdit = NULL;
}

bool FloatCurveEditor::OnMouseLButtonUp( int mX, int mY )
{
	if (!m_pTrackEdit)
	{
		m_pTrackEdit = FloatTrackEdit::instance();
	}
	if (!m_pTrackEdit) return false;

	if (!m_pMember || !m_pEditedNode) return false;
	FloatAnimationCurve* pCurve = NULL;
	if (!m_pMember->Get( m_pEditedNode, pCurve )) return false;

	m_pTrackEdit->SetTrack( pCurve );

	Rct ext = IRS->GetViewPort();
	
	float h = 300.0f;
	ext.y = ext.GetBottom() - h;
	ext.h = h;

	ext.x += 10;
	ext.w -= 20;
	ext.h -= 10;

	m_pTrackEdit->SetExtents( ext );
	m_pTrackEdit->SetInvisible( false );
	return false;
} // FloatCurveEditor::OnMouseLButtonUp

bool FloatCurveEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_ESCAPE)
	{
		if (m_pTrackEdit) m_pTrackEdit->SetInvisible();
	}
	return false;
} // FloatCurveEditor::OnKeyDown

void FloatCurveEditor::OnForceEndEdit()
{
	if (m_pTrackEdit) m_pTrackEdit->SetInvisible();
}

void FloatCurveEditor::Render()
{
	if (!m_pMember) return;
	static const int c_BufSize = 256;
	char buf[c_BufSize];
	
	FloatAnimationCurve* pCurve = NULL;
	bool res = m_pMember->Get( m_pEditedNode, pCurve );
	if (res)
	{
		Rct		rct		= GetExtents();
		Font*	pFont	= GetFont();
		if (pFont) 
		{
			DWORD fgColor	= GetFgColor();
			Vector3D pos( rct.x + rct.h + 3, rct.y, GetPosZ() );
			if (pCurve->GetNKeys() > 0) sprintf( buf, "%d", pCurve->GetNKeys() );
			else
			{
				sprintf( buf, "{%f}", pCurve->GetDefaultValue() );
			}
			pFont->AddString( pos, buf, fgColor );
		}
	}
	
	if (m_pTrackEdit && !m_pTrackEdit->IsInvisible()) m_pTrackEdit->Render();

	Parent::Render();	
} // FloatCurveEditor::Render

/*****************************************************************************/
/*	QuatCurveEditor implementation
/*****************************************************************************/
QuatCurveEditor::QuatCurveEditor()
{
	m_pTrackEdit = NULL;
}

bool QuatCurveEditor::OnMouseLButtonUp( int mX, int mY )
{
	if (!m_pTrackEdit)
	{
		m_pTrackEdit = QuatTrackEdit::instance();
	}

	if (!m_pTrackEdit) return false;

	if (!m_pMember || !m_pEditedNode) return false;
	const QuatAnimationCurve* pCurve = NULL;
	if (!m_pMember->Get( m_pEditedNode, pCurve )) return false;

	m_pTrackEdit->SetTrack( pCurve );

	Rct ext = IRS->GetViewPort();

	float h = 300.0f;
	ext.y = ext.GetBottom() - h;
	ext.h = h;

	ext.x += 10;
	ext.w -= 20;
	ext.h -= 10;

	m_pTrackEdit->SetExtents( ext );
	m_pTrackEdit->SetInvisible( false );
	return false;
} // QuatCurveEditor::OnMouseLButtonUp

bool QuatCurveEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_ESCAPE)
	{
		if (m_pTrackEdit) m_pTrackEdit->SetInvisible();
	}
	return false;
} // QuatCurveEditor::OnKeyDown

void QuatCurveEditor::OnForceEndEdit()
{
	if (m_pTrackEdit) m_pTrackEdit->SetInvisible();
}

void QuatCurveEditor::Render()
{
	if (!m_pMember) return;
	static const int c_BufSize = 256;
	char buf[c_BufSize];

	QuatAnimationCurve* pCurve = NULL;
	bool res = m_pMember->Get( m_pEditedNode, pCurve );
	if (res)
	{
		Rct		rct		= GetExtents();
		Font*	pFont	= GetFont();
		if (pFont) 
		{
			DWORD fgColor	= GetFgColor();
			Vector3D pos( rct.x + rct.h + 3, rct.y, GetPosZ() );
			if (pCurve->GetNKeys() > 0) sprintf( buf, "%d", pCurve->GetNKeys() );
			else
			{
				Quaternion quat = pCurve->GetDefaultValue();
				Matrix3D rot; rot.rotation( quat );
				Vector3D euler = rot.EulerXYZ();
				euler.x = RadToDeg( euler.x );
				euler.y = RadToDeg( euler.y );
				euler.z = RadToDeg( euler.z );
				sprintf( buf, "{%.2f,%.2f,%.2f}", euler.x, euler.y, euler.z );
			}
			pFont->AddString( pos, buf, fgColor );
		}
	}
	
	if (m_pTrackEdit && !m_pTrackEdit->IsInvisible()) m_pTrackEdit->Render();

	Parent::Render();	
} // QuatCurveEditor::Render

/*****************************************************************************/
/*	ColorRampProperty implementation
/*****************************************************************************/
void ColorRampProperty::Render()
{
	ColorRamp* pRamp = NULL;
	if (!m_pMember->Get( m_pEditedNode, pRamp )) return;
	m_Ramp.SetExtents( GetExtents() );
	m_Ramp.SetRamp( pRamp );
	m_Ramp.Render();
} // ColorRampProperty::Render

/*****************************************************************************/
/*	AlphaRampProperty implementation
/*****************************************************************************/
void AlphaRampProperty::Render()
{
	AlphaRamp* pRamp = NULL;
	if (!m_pMember->Get( m_pEditedNode, pRamp )) return;
	m_Ramp.SetExtents( GetExtents() );
	m_Ramp.SetRamp( pRamp );
	m_Ramp.Render();
} // AlphaRampProperty::Render

/*****************************************************************************/
/*	ColorCurveEditor implementation
/*****************************************************************************/
ColorCurveEditor::ColorCurveEditor()
{
	m_pTrackEdit = NULL;
}  

bool ColorCurveEditor::OnMouseLButtonUp( int mX, int mY )
{
	if (!m_pTrackEdit)
	{
		m_pTrackEdit = ColorTrackEdit::instance();
	}

	if (!m_pTrackEdit) return false;

	if (!m_pMember || !m_pEditedNode) return false;
	ColorAnimationCurve* pCurve = NULL;
	if (!m_pMember->Get( m_pEditedNode, pCurve )) return false;

	m_pTrackEdit->SetTrack( pCurve );

	Rct ext = IRS->GetViewPort();

	float h = 300.0f;
	ext.y = ext.GetBottom() - h;
	ext.h = h;

	ext.x += 10;
	ext.w -= 20;
	ext.h -= 10;

	m_pTrackEdit->SetExtents( ext );
	m_pTrackEdit->SetInvisible( false );
	return false;
} // ColorCurveEditor::OnMouseLButtonUp

bool ColorCurveEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_ESCAPE)
	{
		if (m_pTrackEdit) 
		{
			m_pTrackEdit->SetInvisible();
			m_pTrackEdit->SetActive( false );
		}
	}
	return false;
} // ColorCurveEditor::OnKeyDown

void ColorCurveEditor::OnForceEndEdit()
{
	if (m_pTrackEdit) 
	{
		m_pTrackEdit->SetInvisible();
		m_pTrackEdit->SetActive( false );
	}
}

void ColorCurveEditor::Render()
{
	if (!m_pMember) return;
	static const int c_BufSize = 256;
	char buf[c_BufSize];

	ColorAnimationCurve* pCurve = NULL;
	bool res = m_pMember->Get( m_pEditedNode, pCurve );
	if (res)
	{
		Rct		rct		= GetExtents();
		Font*	pFont	= GetFont();
		if (pFont) 
		{
			DWORD fgColor	= GetFgColor();
			Vector3D pos( rct.x + rct.h + 3, rct.y, GetPosZ() );
			if (pCurve->GetNKeys() > 0) sprintf( buf, "%d", pCurve->GetNKeys() );
			else
			{
				ColorValue val = pCurve->GetDefaultValue();
				sprintf( buf, "{%.2f,%.2f,%.2f,%.2f}", val.a, val.r, val.g, val.b );
			}
			pFont->AddString( pos, buf, fgColor );
		}
	}

	if (m_pTrackEdit && !m_pTrackEdit->IsInvisible()) m_pTrackEdit->Render();
	Parent::Render();
} // ColorCurveEditor::Render

/*****************************************************************************/
/*	MethodEditor implementation
/*****************************************************************************/
MethodEditor::MethodEditor()
{
	m_pButton = NULL;
}

void MethodEditor::Render()
{
	if (!m_pMember || !m_pEditedNode) return;

	Rct rct = GetExtents();
	//  button
	if (!m_pButton) m_pButton = GetChild<Button>( "RunButton" );
	rct.x += rct.w / 4.0f;
	rct.w /= 2.0f;
	m_pButton->SetExtents( rct );
	m_pButton->SetText( m_pMember->GetName() );
	Node::Render();	
} // MethodEditor::Render

bool MethodEditor::OnMouseLButtonUp( int mX, int mY )
{
	Parent::OnMouseLButtonUp( mX, mY );
	if (m_pMember && m_pEditedNode)
	{
		m_pMember->Run( m_pEditedNode );
	}
	return false;
} // MethodEditor::OnMouseLButtonUp

END_NAMESPACE(sg)

