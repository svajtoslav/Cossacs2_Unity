/*****************************************************************************/
/*	File:	uiControl.cpp
/*	Desc:	Scene graph UI controls
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-20-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"
#include "mMath2D.h"
#include "sgFont.h"
#include "uiControl.h"
#include "IMediaManager.h"
#include "IEffectManager.h"

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	Control implementation
/*****************************************************************************/
Control::Control()
{
	m_ClrTop	= 0xFFFFFFFF;
	m_ClrMdl	= 0xFFD6D3CE;
	m_ClrBot	= 0xFF848284;

	m_ClrFg		= 0xFF000000;

	m_bDragged	= false;
	m_pFont		= NULL;
	m_bEnableDrag = true;
}

Control::~Control()
{
}

void Control::SetExtents( float _x, float _y, float _w, float _h )
{
	pos.x = _x;
	pos.y = _y;
	width = _w;
	height= _h;
}

void Control::SetExtents( const Rct& rct )
{
	pos.x = rct.x;
	pos.y = rct.y;
	width = rct.w;
	height= rct.h;
}

void Control::RenderNonClientArea()
{	
	rsPanel( GetExtents(), GetPosZ(), m_ClrTop, m_ClrMdl, m_ClrBot );
} // Dialog::RenderNonClientArea

void Control::Render()
{
	RenderNonClientArea();
	Node::Render();
} // Control::Render

void Control::Serialize	( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_ClrTop << m_ClrMdl << m_ClrBot << m_bEnableDrag; 
}

void Control::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_ClrTop >> m_ClrMdl >> m_ClrBot >> m_bEnableDrag; 
}

Rct Control::ClientToScreen( const Rct& rct ) const
{
	Rct rc = GetClientRect();
	return Rct( rct.x + rc.x, rct.y + rc.y, rct.w * scale, rct.h * scale );
}

Rct Control::ScreenToClient( const Rct& rct ) const
{
	Rct rc = GetClientRect();
	return Rct( rct.x - rc.x, rct.y - rc.y, rct.w / scale, rct.h / scale );
}

void Control::ClientToScreen( float& x, float& y ) const
{
	Rct rc = GetClientRect();
	x += rc.x; y += rc.y;
}

void Control::ScreenToClient( float& x, float& y ) const
{
	Rct rc = GetClientRect();
	x -= rc.x; y -= rc.y;
}

void Control::BeginDrag	( float mx, float my )
{
	if (!m_bEnableDrag) return;
	m_DragX = mx;
	m_DragY = my;
	m_bDragged = true;
}

void Control::OnDrag( float mx, float my )
{
	if (!m_bDragged) return;
	pos.x += mx - m_DragX;
	pos.y += my - m_DragY;
	m_DragX = mx;
	m_DragY = my;
}

void Control::EndDrag( float mx, float my )
{
	m_DragX = mx;
	m_DragY = my;
	m_bDragged = false;
}

void Control::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Control", this );
	pm.f( "BgTop",		m_ClrTop, "color"	);
	pm.f( "BgMdl",		m_ClrMdl, "color"	);
	pm.f( "MgBottom",	m_ClrBot, "color"	);
	pm.f( "Foreground",	m_ClrFg,  "color"	);
} // Control::Expose

Font* Control::GetParentFont() const
{
	const Node* pNode = this;
	while (pNode)
	{
		if (pNode->HasFn( Dialog::Magic() ))
		{
			Dialog* pDlg = (Dialog*) pNode;
			return pDlg->GetFont();
		}
		pNode = pNode->GetParent();
	}
	return NULL;
} // Control::GetParentFont

Font* Control::GetFont()
{
	if (!m_pFont) m_pFont = GetParentFont();
	return m_pFont;
}

/*****************************************************************************/
/*	Dialog implementation
/*****************************************************************************/
Dialog::Dialog()
{
	SetActive( true );

	m_BorderWidth		= 1.0f;
	m_BorderHeight		= 1.0f;
	m_HeaderHeight		= 0.0f;
	m_ClrHeader			= 0xFF000055;

	m_FontName			= "Tahoma";
	m_FontHeight		= 9;

	m_pFont				= NULL;
}

Dialog::~Dialog()
{
}

_inl Rct Dialog::GetClientRect() const
{
	Rct ext = GetExtents();
	ext.Inflate( m_HeaderHeight + m_BorderHeight*2.0f, m_BorderWidth, m_BorderHeight, m_BorderWidth );
	return ext;
}

Font* Dialog::GetFont()
{
	if (!m_pFont) 
	{
		//  try to find first
		for (int i = 0; i < GetNChildren(); i++)
		{
			Node* pNode = GetChild( i );
			if (pNode->HasFn( Font::Magic() ))
			{
				Font* pFont = (Font*)pNode;
				if (pFont->GetHeight() == m_FontHeight &&
					!strcmp( m_FontName.c_str(), pFont->GetFontName()))
				{
					m_pFont = pFont;
					return m_pFont;
				}
			}
		}
		//  create font
		if (m_FontName.size() == 0) return NULL;
		char name[256];
		sprintf( name, "%s%d", m_FontName.c_str(), m_FontHeight );
		m_pFont = AddChild<Font>( name );
		m_pFont->SetFontName	( m_FontName.c_str() );
		m_pFont->SetHeight		( m_FontHeight );
		m_pFont->Generate		();
	}
	
	return m_pFont;
} // Dialog::GetFont	

void Dialog::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Dialog", this );
	pm.f( "BorderWidth",	m_BorderWidth			);
	pm.f( "BorderHeight",	m_BorderHeight			);
	pm.f( "HeaderColor",	m_ClrHeader, "color"	);
	pm.f( "HeaderHeight",	m_HeaderHeight			);
	pm.p ( "FontName",		GetFontName, SetFontName);
	pm.f( "FontHeight",	m_FontHeight			);

} // Dialog::Expose

void Dialog::RenderNonClientArea()
{
	
	rsPanel( GetExtents(), GetPosZ(), m_ClrTop, m_ClrMdl, m_ClrBot );

	if (m_HeaderHeight > 0.0f)
	{
	Rct hRct = GetExtents();
	hRct.Inflate( m_BorderHeight, m_BorderWidth, 0.0f, m_BorderWidth );
	hRct.h = m_HeaderHeight;
	rsRect( hRct, GetPosZ(), m_ClrHeader );

	hRct.x += hRct.w - m_HeaderHeight;
	hRct.w = m_HeaderHeight;
	hRct.Inflate( 1.0f, 1.0f, 1.0f, 1.0f );
	rsPanel( hRct, GetPosZ(), m_ClrTop, m_ClrMdl, m_ClrBot );
	}
} // Dialog::RenderNonClientArea

bool Dialog::OnMouseLButtonDown( int mX, int mY )
{
	Rct hdr = GetHeaderRect(); 
	if (hdr.PtIn( mX, mY )) 
	{
		BeginDrag( mX, mY );
		return true;
	}
	
	float mx = mX, my = mY;
	//ScreenToClient( mx, my );
	Iterator it( this );
	while (it)
	{
		Node* pNode = it;
		if (pNode->HasFn( Control::Magic() ))
		{
			Control* pCtrl = (Control*) pNode;
			if (pCtrl != this && pCtrl->GetExtents().PtIn( mx, my ))
			{
				pCtrl->OnMouseLButtonDown( mX, mY );
			}
		}
		++it;
	}

	return false;
} // Dialog::OnMouseLButtonDown

bool Dialog::OnMouseMButtonDown( int mX, int mY )
{
	Rct hdr = GetExtents(); 
	if (hdr.PtIn( mX, mY ) && 
		(GetKeyState( VK_CONTROL ) >= 0) &&
		(GetKeyState( VK_SHIFT	 ) >= 0) &&
		(GetKeyState( VK_MENU	 ) >= 0)
		)
	{
		BeginDrag( mX, mY );
		return true;
	}
	return false;
}

bool Dialog::OnMouseMButtonUp( int mX, int mY )
{
	Rct hdr = GetExtents(); 
	if (hdr.PtIn( mX, mY )) 
	{
		EndDrag( mX, mY );
		return true;
	}
	return false;
}

bool Dialog::OnMouseMove( int mX, int mY, DWORD keys )
{
	if (IsDragged()) 
	{
		float mx = mX;
		float my = mY;
		OnDrag( mx, my );
		return true;
	}

	float mx = mX, my = mY;
	ScreenToClient( mx, my );
	Iterator it( this );
	while (it)
	{
		Node* pNode = it;
		if (pNode->HasFn( Control::Magic() ))
		{
			Control* pCtrl = (Control*) pNode;
			if (pCtrl != this && pCtrl->GetExtents().PtIn( mx, my ))
			{
				pCtrl->OnMouseMove( mX, mY, keys );
			}
		}
		++it;
	}

	return false;
} // Dialog::OnMouseMove

bool Dialog::OnMouseLButtonUp( int mX, int mY )
{
	if (IsDragged()) 
	{
		float mx = mX;
		float my = mY;
		EndDrag( mx, my );
		return true;
	}

	float mx = mX, my = mY;
	//ScreenToClient( mx, my );
	Iterator it( this );
	while (it)
	{
		Node* pNode = it;
		if (pNode->IsA<Control>())
		{
			Control* pCtrl = (Control*) pNode;
			if (pCtrl != this && pCtrl->GetExtents().PtIn( mx, my ))
			{
				pCtrl->OnMouseLButtonUp( mX, mY );
			}
		}
		++it;
	}

	return false;
} // Dialog::OnMouseLButtonUp

Rct Dialog::GetHeaderRect() const
{
	Rct rct( GetExtents() );
	rct.h = m_HeaderHeight;
	return rct;
} // Dialog::GetHeaderRect

/*****************************************************************************/
/*	Button implementation
/*****************************************************************************/
void Button::Render()
{
	Rct rct = GetExtents();
	if (m_State == bsPressed) 
	{	
		rct.x += 0; rct.y += 1;
		SetExtents( rct );
	}

	Control::Render();

	Font* pFont		= GetFont();
	DWORD fgColor	= GetFgColor();
	if (!pFont) return;

	float strW = pFont->GetStringWidth( m_Text.c_str() );
	Vector3D pos( rct.x + 1, rct.y, GetPosZ() );

	pos.x = tmax( pos.x, rct.GetCenterX() - strW * 0.5f );
	pFont->AddString( pos, m_Text.c_str(), fgColor );

	if (m_State == bsPressed) 
	{	
		rct.x -= 0; rct.y -= 1;
		SetExtents( rct );
	}

} // Button::Render

bool Button::OnMouseLButtonDown( int mX, int mY )
{
	m_State = bsPressed;
	return false;
}

bool Button::OnMouseLButtonUp( int mX, int mY )
{
	m_State = bsIdle;
	return false;
}

/*****************************************************************************/
/*	CheckBox implementation
/*****************************************************************************/
void CheckBox::Render()
{
	Control::Render();
}

/*****************************************************************************/
/*	EditBox implementation
/*****************************************************************************/
EditBox::EditBox()
{
	m_ClrTop  		= 0xFF848284;
	m_ClrMdl  		= 0xFFFFFFFF;
	m_ClrBot  		= 0xFFD6D3CE;
	m_ClrText 		= 0xFF000008;
	m_ClrCaret		= 0xFF080000;

	m_CaretBlinkOn	= 800;
	m_CaretBlinkOff = 500;
	m_TextViewPos	= 0.0f;

	m_CaretPos		= 0;
}

void EditBox::Render()
{
	Control::Render();
	rsFlushPoly2D();
	
	Font* pFont = GetFont();
	if (!pFont || m_Text.size() == 0) return;
	
	Rct ext = GetExtents();
	Vector3D pos( ext.x - m_TextViewPos, ext.y, GetPosZ() );

	Rct vp = IRS->GetViewPort();
	IRS->SetViewPort( ext );
	
	pFont->AddString( pos, m_Text.c_str(), m_ClrText );
	RenderCaret();
	rsFlushLines2D();
	
	IRS->SetViewPort( vp );
} // EditBox::Render

void EditBox::RenderCaret()
{
	Font* pFont = GetFont();
	if (!pFont) return;

	DWORD tc = GetTickCount();
	Rct ext = GetExtents();
	tc %= m_CaretBlinkOn + m_CaretBlinkOff;
	if (tc < m_CaretBlinkOn)
	{
		m_CaretPos = 2;
		float cpos = pFont->GetStringWidth( m_Text.c_str(), m_CaretPos ) - m_TextViewPos;
		rsLine( ext.x + cpos, ext.y, ext.x + cpos, ext.GetBottom(), m_ClrCaret, m_ClrCaret );
		rsFlushLines2D();
	}
} // EditBox::RenderCaret

/*****************************************************************************/
/*	Thumbnail implementation
/*****************************************************************************/
Thumbnail::Thumbnail()
{
	m_ViewDir			= Vector3D( 0.0f, -cos( c_PI/6.0f ), -sin( c_PI/6.0f ) );
	m_ControlMode		= cmExhibition;
	m_pCamera			= NULL;		
	m_pModels			= NULL;
	m_FOV				= 0.8f;
	m_BackgroundColor	= 0xFF3A3448;
	m_RotationSpeed		= 0.001f;
	m_StartTime			= 0.0f;

	m_bMouseRolling		= false;
	m_MouseRollPos		= -1.0f;
	m_RotAngleDelta		= 0.01f;

	m_MinRadius			= 10.0f;
	m_Radius			= m_MinRadius;
	
	m_MouseRollAngle = m_Angle = 0.0f;

	m_pModels = AddChild<Group>( "Models" );
	SetExtents( 0.0f, 0.0f, 200.0f, 200.0f );
} // Thumbnail::Thumbnail

void Thumbnail::Render()
{
	if (IsInvisible()) SetActive( false ); else SetActive( true );
	UpdateCamera();
	//  background
	TransformNode::ResetTMStack( &m_Transform );
	
	IPMgr->EnableBatching( false );
	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	m_pCamera->Render();
	m_pModels->Render();

    IEffMgr->Render();

	TransformNode::ResetTMStack();
	if (pCam) pCam->Render();
	IPMgr->EnableBatching( true );

} // Thumbnail::Render

void Thumbnail::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Thumbnail", this );
	pm.f( "FOV",				m_FOV			);
	pm.f( "ControlMode",		m_ControlMode	);
	pm.f( "ViewDirX",			m_ViewDir.x		);
	pm.f( "ViewDirY",			m_ViewDir.y		);
	pm.f( "ViewDirZ",			m_ViewDir.z		);
	pm.f( "RotationSpeed",	m_RotationSpeed );
	pm.f( "BackgroundColor",	m_BackgroundColor, "color" );
	pm.f( "RotAngleDelta",	m_RotAngleDelta );
	pm.f( "Radius",			m_Radius		);
} // Thumbnail::Expose

void Thumbnail::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	BYTE bmode = (BYTE)m_ControlMode;
	os << m_ViewDir << m_FOV << bmode << m_BackgroundColor << m_RotationSpeed;
} // Thumbnail::Serialize

void Thumbnail::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	BYTE bmode;
	is >> m_ViewDir >> m_FOV >> bmode >> m_BackgroundColor >> m_RotationSpeed;
	m_ControlMode = (ControlMode)bmode;
} // Thumbnail::Unserialize

void Thumbnail::ClearItems()
{
	m_pModels->RemoveChildren();
	m_Radius = m_MinRadius;
}

void Thumbnail::AddItem( Node* pItem )
{
	UpdateCamera();
	m_pModels->AddInput( pItem );
} // Thumbnail::AddItem

void Thumbnail::SetRect( const Rct& rct )
{
	SetExtents( rct );
}

void Thumbnail::UpdateCamera()
{
	m_pCamera = GetChild<PerspCamera>( "ThumbnailCamera" );
	
	Sphere bsphere = GetStaticBoundSphere( m_pModels );
	m_Radius = bsphere.GetRadius() + bsphere.GetCenter().norm();
	if (m_Radius == 0.0f) m_Radius = 200.0f;
	m_Radius*=0.66f;	
	float fov = m_FOV;
	float aspect = GetExtents().GetAspect();
	if (aspect > 1.0f) fov = 2.0f * atanf( tanf( m_FOV*0.5f ) / aspect );
	float camDist = m_Radius / tanf( fov * 0.5f );
	
	Vector3D dir( m_ViewDir );
	if (m_StartTime == 0.0f)
	{
		m_StartTime = float( GetTickCount() );
	}

	if (m_ControlMode == cmExhibition)
	{
		float curTime = GetTickCount();
		if (!m_bMouseRolling) m_Angle = (curTime - m_StartTime) * m_RotationSpeed;	
		m_Transform.rotation( Vector3D::oZ, m_Angle );
	}
	else
	{
		m_Transform.setIdentity();
	}

	Vector3D cpos = dir;
	cpos *= -camDist;

	m_pCamera->SetDirUp( m_ViewDir, Vector3D::oZ );
	m_pCamera->SetPos( cpos );
	m_pCamera->SetPerspFOVx( m_FOV, aspect, camDist - m_Radius, camDist + m_Radius );
	m_pCamera->SetTweakAspect( false );
	
} // Thumbnail::UpdateCamera

bool Thumbnail::OnMouseMove( int mX, int mY, DWORD keys )
{
	if (m_ControlMode == cmNone) return false;
	if (m_bMouseRolling)
	{
		m_Angle = (m_MouseRollPos - mX) * m_RotAngleDelta + m_MouseRollAngle;
	}

	return false;
} // Thumbnail::OnMouseMove

bool Thumbnail::OnMouseLButtonDown( int mX, int mY )
{
	if (!GetExtents().PtIn( mX, mY )) return false;
	if (m_ControlMode == cmNone) return false;
	if (m_ControlMode == cmExhibition || m_ControlMode == cmMouseRoll)
	{
		m_bMouseRolling		= true;
		m_MouseRollPos		= mX;
		m_MouseRollAngle	= m_Angle; 
	}
	return false; 
} // Thumbnail::OnMouseLButtonDown

bool Thumbnail::OnMouseLButtonUp( int mX, int mY )
{
	if (m_bMouseRolling)
	{
		m_Angle = m_MouseRollAngle;
	}
	m_bMouseRolling	= false;
	return false; 
} // Thumbnail::OnMouseLButtonDown

END_NAMESPACE(sg)

