/*****************************************************************************/
/*	File:	uiObjectInspector.cpp
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-13-2003
/*****************************************************************************/
#include "stdafx.h"

#include "sgTexture.h"
#include "kPropertyMap.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgFont.h"
#include "uiControl.h"
#include "uiObjectInspector.h"
#include "IMediaManager.h"
#include "sgApplication.h"
#include "uiTrackEdit.h"
#include "uiWeightEdit.h"
#include "uiRampEdit.h"
#include "uiPropEditors.h"

BEGIN_NAMESPACE(sg)

//  property editors
REGNODE( PropertyEditor		);
REGNODE( PropertyEditorEx	);
REGNODE( StringEditor		);
REGNODE( IntegerEditor		);
REGNODE( FloatEditor		);
REGNODE( BoolEditor			);
REGNODE( FilePathEditor		);
REGNODE( MethodEditor		);
REGNODE( ColorSelector		);
REGNODE( ColorPicker		);
REGNODE( TextEditor			);
REGNODE( TextureEditor		);
REGNODE( TextureView		);
REGNODE( EnumEditor			);
REGNODE( FloatCurveEditor	);
REGNODE( QuatCurveEditor	);
REGNODE( ColorRampProperty	);
REGNODE( AlphaRampProperty  );
REGNODE( ColorCurveEditor	);
REGNODE( DirectionEditor	);

/*****************************************************************************/
/*	InspectorItem implementation
/*****************************************************************************/
InspectorItem::InspectorItem()
{
	m_Extents.Zero();
	m_IndexInMap	= -1;
	m_bSelected		= false;
	m_FgColor		= 0xFF000000;
	m_BgColor		= 0xFFD6D3CE;
	m_pEditor		= NULL;
	m_pMember		= NULL;
	m_bSection		= false;
	m_bCollapsed	= false;
}; // class InspectorItem

/*****************************************************************************/
/*	ObjectInspector implementation
/*****************************************************************************/
ObjectInspector::ObjectInspector()
{
	m_DefItemHeight		= 11;
	m_pMap				= NULL;
	m_pNode				= NULL;

	m_FgColor			= 0xFF000000;
	m_BgColor			= 0x66D6D3CE;
	m_BgAltColor		= 0x66AAAABA;
	m_SelBgColor		= 0xFF6464FF;
	m_SelFgColor		= 0xFFFFFFFF;
	m_DisabledFgColor	= 0xFFCCCCCF;
	m_LinesColor1		= 0xFF303030;
	m_LinesColor2		= 0xFFFFFFFF;

	m_SectionFgColor	= 0xFFFFFF88;
	m_SectionBgColor	= 0x66969ED6;
	m_MinColWidth		= 200.0f;
	m_MaxColWidth		= 300.0f;

	m_ClrMdl			= 0x66D6D3CE;
	m_HeaderHeight		= 0;
	m_FontName			= "Tahoma";
	m_FontHeight		= 9;
	m_bShowParent		= true;

	SetExtents( IRS->GetViewPort() );
} // ObjectInspector::ObjectInspector

ObjectInspector::~ObjectInspector()
{
	delete m_pMap;
}

void ObjectInspector::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ObjectInspector", this );
	pm.f( "DefItemHeight",	m_DefItemHeight		);
	pm.f( "BorderWidth",	m_BorderWidth		);
	pm.f( "BorderHeight",	m_BorderHeight		);
	pm.f( "BgColor",		m_BgColor,			"color" );
	pm.f( "FgColor",		m_FgColor,			"color" );
	pm.f( "SelBgColor",		m_SelBgColor,		"color" );
	pm.f( "SelFgColor",		m_SelFgColor,		"color" );
	pm.f( "DisabledFgColor",m_DisabledFgColor,	"color" );
	pm.f( "BgAltColor",		m_BgAltColor,		"color" );
	pm.f( "LinesColor1",	m_LinesColor1,		"color" );
	pm.f( "LinesColor2",	m_LinesColor2,		"color" );
	pm.f( "SectionFgColor",	m_SectionFgColor,	"color" );
	pm.f( "SectionBgColor",	m_SectionBgColor,	"color" );
	pm.f( "MaxColWidth",	m_MaxColWidth );
	pm.f( "ShowParentMembers",m_bShowParent );
} // ObjectInspector::CreatePropertyMap

void ObjectInspector::BindNode( Node* pNode )
{
	if (pNode == m_pNode || !pNode) return;
	if (m_pMap) delete m_pMap;
	m_pMap = new PropertyMap();
	if (m_pNode) m_pNode->Release();
	pNode->AddRef();
	pNode->Expose( *m_pMap );
	m_pNode = pNode;				
	UpdateItems();
} // ObjectInspector::BindNode

PropertyEditor* ObjectInspector::CreateEditor( const char* typeName )
{
	if (!strcmp( typeName, "bool"		))		return new  BoolEditor			();
	if (!strcmp( typeName, "cstring"	)) 		return new  TextEditor		    ();
	if (!strcmp( typeName, "int"		))		return new  IntegerEditor		();
	if (!strcmp( typeName, "float"		))		return new  FloatEditor			();
	if (!strcmp( typeName, "double"		))		return new  FloatEditor			();
	if (!strncmp( typeName, "file", 4	))		return new  FilePathEditor		( typeName[4] == 0 ? "" : typeName + 5 );
	if (!strcmp( typeName, "method"		))		return new  MethodEditor		();
	if (!strcmp( typeName, "color"		))		return new  ColorSelector		();
	if (!strcmp( typeName, "texture"	))		return new  TextureEditor		();
	if (!strcmp( typeName, "enum"		))		return new  EnumEditor			();
	if (!strcmp( typeName, "direction"	))		return new  EnumEditor			();
	if (!strcmp( typeName, "floatAnimCurve" ))	return new  FloatCurveEditor	();
	if (!strcmp( typeName, "quatAnimCurve"	))	return new  QuatCurveEditor		();
	if (!strcmp( typeName, "colorAnimCurve" )) 	return	new  ColorCurveEditor	();
	if (!strcmp( typeName, "color_ramp"		)) 	return	new  ColorRampProperty	();
	if (!strcmp( typeName, "alpha_ramp"		)) 	return	new  AlphaRampProperty	();
	return new PropertyEditor();
} // ObjectInspector::CreateEditor

void ObjectInspector::UpdateItems()
{
	if (!m_pMap) return;
	
	Group* pEdGroup = FindChild<Group>( "Property Editors" );
	if (!pEdGroup)
	{
		pEdGroup = AddChild<Group>( "Property Editors" );
	}
	pEdGroup->RemoveChildren();

	m_Items.clear();

	Rct rctClient	= GetClientRect();
	Rct rctExt		= GetExtents();

	float cY = 0.0f;
	float cX = 0.0f;
	float cW = rctClient.w;
	int cIdx = 0;
	for (int i = 0; i < m_pMap->GetNSections(); i++)
	{
		PropertySection& sec = m_pMap->GetSection( i );

		m_Items.push_back( InspectorItem() );
		InspectorItem& sitem = m_Items.back();
		sitem.m_Caption		= sec.GetName();
		sitem.m_IndexInMap	= cIdx;
		sitem.m_pMember		= NULL;
		sitem.m_Extents		= Rct( cX, cY, cW, m_DefItemHeight );
		sitem.m_FgColor		= m_SectionFgColor;		
		sitem.m_BgColor		= m_SectionBgColor;
		sitem.m_pEditor		= NULL;
		sitem.m_StrValue	= "";
		sitem.m_bSection	= true;
		sitem.m_Extents		= Rct( cX, cY, cW, m_DefItemHeight );

		cY += m_DefItemHeight;
		float cLocY = 0.0f;
		for (int j = 0; j < sec.GetNMembers(); j++)
		{
			sitem.m_SubItems.push_back( InspectorItem() );
			InspectorItem& item = sitem.m_SubItems.back();
			ClassMember* pMember = sec.GetMember( j );
			item.m_Caption		= pMember->GetName();
			item.m_IndexInMap	= cIdx++;
			item.m_pMember		= pMember;
			item.m_bSection		= false;
			item.m_Extents		= Rct( cX, cY, cW, m_DefItemHeight );
			item.m_FgColor		= item.m_pMember->IsReadonly() ? m_DisabledFgColor : m_FgColor;		
			if (cIdx&1) item.m_BgColor = m_BgAltColor; else item.m_BgColor = m_BgColor;

			//  assign editor
			PropertyEditor* pEditor = CreateEditor( pMember->GetType() );
			if (pEditor) pEditor->SetName( item.m_pMember->GetName() );
			pEdGroup->AddChild( pEditor );
			if (pEditor)
			{
				item.m_pEditor = pEditor;
				pEditor->SetClassMember( pMember );
				pEditor->SetEditedNode( m_pNode );
				pEditor->SetInvisible();
			}

			static const int c_BufSize = 256;
			char buf[c_BufSize];
			bool res = pMember->ToString( m_pMap->GetObject(), buf, c_BufSize );
			if (res) item.m_StrValue = std::string( buf );		
			cY += m_DefItemHeight;
		}
	}
	SetHeight( rctExt.h - rctClient.h + cY );
	
} // ObjectInspector::UpdateItems

int	ObjectInspector::GetSelectedItem()
{
	for (int i = 0; i < m_Items.size(); i++)
	{
		if (m_Items[i].m_bSelected) return i;
	}
	return -1;
} // ObjectInspector::GetSelectedItem

float ObjectInspector::GetLeftColWidth()
{
	float w = GetMaxCaptionWidth();
	w += 2.0f * m_BorderWidth + 2.0f;
	if (w > m_MaxColWidth) w = m_MaxColWidth;
	if (w < m_MinColWidth) w = m_MinColWidth;
	return w;
} // ObjectInspector::GetLeftColWidth

float ObjectInspector::GetRightColWidth() 
{
	float w = GetMaxValueWidth();
	w += 2.0f * m_BorderWidth + 2.0f;
	if (w > m_MaxColWidth) w = m_MaxColWidth;
	if (w < m_MinColWidth) w = m_MinColWidth;
	return w + m_DefItemHeight;
} // ObjectInspector::GetRightColWidth

float ObjectInspector::GetMaxCaptionWidth() 
{
	Font* pFont = GetFont();	
	if (!pFont) return 0.0f;

	float maxW = 0.0f;
	for (int i = 0; i < m_Items.size(); i++)
	{
		float curW = pFont->GetStringWidth( m_Items[i].m_Caption.c_str() );
		if (i == 0 || curW > maxW)
		{
			maxW = curW;
		}
	}
	return maxW;
} // ObjectInspector::GetMaxCaptionWidth

float ObjectInspector::GetMaxValueWidth	()
{
	Font* pFont = GetFont();
	if (!pFont) return false;

	float maxW = 0.0f;
	for (int i = 0; i < m_Items.size(); i++)
	{
		float curW = pFont->GetStringWidth( m_Items[i].m_StrValue.c_str() );
		if (i == 0 || curW > maxW)
		{
			maxW = curW;
		}
	}
	return maxW;
} // ObjectInspector::GetMaxCaptionWidth


void ObjectInspector::ClearSelection()
{
	for (int i = 0; i < m_Items.size(); i++)
	{
		InspectorItem& sec = m_Items[i];
		for (int j = 0; j < sec.m_SubItems.size(); j++)
		{
			InspectorItem& item = sec.m_SubItems[j];
			if (item.m_bSelected)
			{
				if (item.m_pEditor) 
                {
                    item.m_pEditor->OnForceEndEdit();
                    item.m_pEditor->SetFocus( false );
                }
				item.m_bSelected = false;
			}
		}
	}
} // ObjectInspector::GetSelectedItem

bool ObjectInspector::OnMouseLButtonDblclk( int mX, int mY )
{
	if (IsInvisible()) return false;

	if (Dialog::OnMouseLButtonDblclk( mX, mY )) return true;
	
	Rct rctL( GetExtents() );
	rctL.w = GetLeftColWidth();
	if (rctL.PtIn( mX, mY ))
	{
		SetInvisible( true );
		return true;
	}
	return false;
} // ObjectInspector::OnMouseLButtonDblclk

bool ObjectInspector::OnMouseMButtonDown( int mX, int mY )
{
	if (IsInvisible()) return false;

	Rct rctL( GetExtents() );
	if (rctL.PtIn( mX, mY ))
	{
		BeginDrag( mX, mY );
		return false;
	}
	return false;
} // ObjectInspector::OnMouseMButtonDown

bool ObjectInspector::OnMouseMButtonUp( int mX, int mY )
{
	if (IsInvisible()) return false;

	if (Dialog::OnMouseMButtonUp( mX, mY )) return true;
	if (IsDragged()) 
	{
		float mx = mX;
		float my = mY;
		EndDrag( mx, my );
		return true;
	}
	return false;
} // ObjectInspector::OnMouseMButtonUp

bool ObjectInspector::OnMouseLButtonDown( int mX, int mY )				
{ 
	if (IsInvisible() || !GetExtents().PtIn( mX, mY )) return false;

	float mx = mX;
	float my = mY;

	ScreenToClient( mx, my );
	InspectorItem* pItem = GetItemByPt( mx, my );
	ClearSelection();
	if (pItem && mx > GetMaxCaptionWidth())
	//  select item, activate editor
	{
		if (pItem->m_bSection)
		{
			pItem->m_bCollapsed = !pItem->m_bCollapsed;
		}
		else if (!pItem->m_bSelected && pItem->m_pMember && !pItem->m_pMember->IsReadonly()) 
		{
			pItem->m_bSelected = true;
            if (pItem->m_pEditor) pItem->m_pEditor->SetFocus();
		}
	}	

	if (Dialog::OnMouseLButtonDown( mX, mY )) return false;

	return false; 
} // ObjectInspector::OnMouseLButtonDown

bool ObjectInspector::OnMouseLButtonUp( int mX, int mY )
{
	return false;
} // ObjectInspector::OnMouseLButtonUp

bool ObjectInspector::OnMouseMove( int mX, int mY, DWORD keys )
{
	if (IsInvisible()) return false;

	if (IsDragged()) 
	{
		float mx = mX;
		float my = mY;
		
		if (!(keys & MK_MBUTTON))
		{
			EndDrag( mx, my );
			return true;
		}
		
		OnDrag( mx, my );
		return true;
	}
	return false;
} // ObjectInspector::OnMouseMove

int	ObjectInspector::GetNItems() const
{
	int cIdx = 0;
	for (int i = 0; i < m_Items.size(); i++)
	{
        cIdx++;
		const InspectorItem& sec = m_Items[i];
		cIdx += sec.m_SubItems.size();
	}
	return cIdx;
}

bool ObjectInspector::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (IsInvisible()) return false;

	for (int i = 0; i < m_Items.size(); i++)
	{
		InspectorItem& sec = m_Items[i];
		for (int j = 0; j < sec.m_SubItems.size(); j++)
		{
			InspectorItem& item = sec.m_SubItems[j];
			if (item.m_bSelected && item.m_pEditor)
			{
				item.m_pEditor->OnKeyDown( keyCode, flags );
                item.m_pEditor->SetFocus( false );
			}
		}
	}

	int nItems = GetNItems();
	if (keyCode == VK_DOWN) 
	{
		int idx = GetSelectIdx();
		if (idx >= 0) SetSelectIdx( (idx + 1) % nItems);
	}

	if (keyCode == VK_UP)	
	{
		int idx = GetSelectIdx();
		if (idx >= 0) SetSelectIdx( (idx + nItems - 1) % nItems);
	}

	return false;
} // ObjectInspector::OnKeyDown

int ObjectInspector::GetSelectIdx() const
{
	int cIdx = 0;
	for (int i = 0; i < m_Items.size(); i++)
	{
		const InspectorItem& sec = m_Items[i];
		cIdx++;
		for (int j = 0; j < sec.m_SubItems.size(); j++)
		{
			const InspectorItem& item = sec.m_SubItems[j];
			if (item.m_bSelected) return cIdx;
			cIdx++;
		}
	}
	return -1;
} // ObjectInspector::GetSelectIdx

void ObjectInspector::SetSelectIdx( int idx ) 
{
	int cIdx = 0;
	for (int i = 0; i < m_Items.size(); i++)
	{
		InspectorItem& sec = m_Items[i];
		cIdx++;
		for (int j = 0; j < sec.m_SubItems.size(); j++)
		{
			InspectorItem& item = sec.m_SubItems[j];
			if (cIdx == idx)
			{
				if (item.m_pMember->IsReadonly()) return;
				ClearSelection();
				item.m_bSelected = true;
                if (item.m_pEditor) item.m_pEditor->SetFocus();
			}
			cIdx++;
		}
	}
} // ObjectInspector::GetSelectIdx

bool ObjectInspector::OnChar( DWORD charCode, DWORD flags )
{
	for (int i = 0; i < m_Items.size(); i++)
	{
		InspectorItem& sec = m_Items[i];
		for (int j = 0; j < sec.m_SubItems.size(); j++)
		{
			InspectorItem& item = sec.m_SubItems[j];
			if (item.m_bSelected && item.m_pEditor)
			{
				item.m_pEditor->OnChar( charCode, flags );
			}
		}
	}
	return false;
} // ObjectInspector::OnChar(

InspectorItem* ObjectInspector::GetItemByPt( float pX, float pY )
{
	for (int i = 0; i < m_Items.size(); i++)
	{
		InspectorItem& sec = m_Items[i]; 
		if (sec.m_Extents.PtIn( pX, pY )) return &sec;
		for (int j = 0; j < sec.m_SubItems.size(); j++)
		{
			InspectorItem& item = sec.m_SubItems[j];
			if (item.m_Extents.PtIn( pX, pY )) return &item;
		}
	}
	return NULL;
} // ObjectInspector::GetItemByPt

void ObjectInspector::Render()
{
    SetFocus( false );

	Rct ext = GetExtents();
	Rct vp = IRS->GetViewPort();
	if (ext.y < vp.y) ext.y += vp.y - ext.y;
	if (ext.y > vp.GetBottom()) ext.y = vp.GetBottom() - ext.h;
	SetExtents( ext );

	Font* pFont = GetFont();
	float leftColW	= GetLeftColWidth();
	float rightColW = GetRightColWidth();

	SetWidth( leftColW + rightColW + 4.0f );
	Dialog::RenderNonClientArea();

	for (int i = 0; i < m_Items.size(); i++)
	{
		InspectorItem& sitem = m_Items[i];

		sitem.m_Extents.w = leftColW + rightColW + 1.0f;
		Rct sExt = ClientToScreen( sitem.m_Extents );
		sExt.x++;
		rsPanel( sExt, GetPosZ(), 0x88FFFFFF, 0x88D6D3CE, 0x88848284 );

		if (sitem.m_bCollapsed) continue;
		for (int j = 0; j < sitem.m_SubItems.size(); j++)
		{
			InspectorItem& item = sitem.m_SubItems[j];

			item.m_Extents.w = leftColW + rightColW + 2.0f;
			Rct ext = ClientToScreen( item.m_Extents );

			Vector3D rpos( ext.x, ext.y, GetPosZ() );
			Vector3D vpos( ext.x + leftColW + 4, ext.y, GetPosZ() );

			DWORD fgColor = item.m_FgColor;
			DWORD bgColor = item.m_BgColor;

			if (item.m_bSelected) 
			{
				bgColor = m_SelBgColor;
				fgColor = m_SelFgColor;
			}

			//  draw item background
			if (bgColor != GetClrMdl()) rsRect( ext, GetPosZ(), bgColor );
			//  draw item caption text
			pFont->AddString( rpos, item.m_Caption.c_str(), fgColor );

			if (item.m_pEditor)
			{
				Rct edExt( vpos.x - 1, vpos.y, rightColW - m_BorderWidth, item.m_Extents.h );
				item.m_pEditor->SetExtents( edExt );
				item.m_pEditor->SetFgColor( fgColor );
				item.m_pEditor->Render();
                if (item.m_bSelected && item.m_pEditor->IsA<TextEditor>()) SetFocus( true );
			}
		}
	}
	
	Rct rctClient = GetClientRect();

	leftColW += rctClient.x;

	rsLine( leftColW, rctClient.y, 
			leftColW, rctClient.GetBottom(), 
			GetPosZ(), m_LinesColor1 );

	rsLine( leftColW + 1, rctClient.y, 
			leftColW + 1, rctClient.GetBottom(), 
			GetPosZ(), m_LinesColor2 );

	rsFlushPoly2D();
	rsFlushLines2D();
	
	Node::Render();
} // ObjectInspector::Render

void ObjectInspector::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void ObjectInspector::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

/*****************************************************************************/
/*	PropertyEditor implementation
/*****************************************************************************/
bool PropertyEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (!m_pMember || !m_pEditedNode) return false;
	if (keyCode == VK_LEFT)
	{
		return m_pMember->PrevValue( m_pEditedNode );
	}

	if (keyCode == VK_RIGHT)
	{
		return m_pMember->NextValue( m_pEditedNode );
	}
	return false;
} // PropertyEditor::

void PropertyEditor::Render()
{
	if (!m_pMember || !m_pEditedNode) return;
	Font* pFont = GetFont();
	if (!pFont) return;
	static const int c_BufSize = 256;
	char buf[c_BufSize];
	bool res = m_pMember->ToString( m_pEditedNode, buf, c_BufSize );
	DWORD fgColor	= GetFgColor();
	Rct rct = GetExtents();
	Vector3D pos( rct.x + 2, rct.y, GetPosZ() );
	pFont->AddString( pos, buf, fgColor );
} // PropertyEditor::Render

END_NAMESPACE(sg)

