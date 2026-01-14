/*****************************************************************************/
/*	File:	uiNodeTree.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	07-07-2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgFont.h"
#include "kPropertyMap.h"
#include "kInput.h"
#include "uiControl.h"
#include "uiNodeTree.h"
#include "IWidgetManager.h"

#include "uiObjectInspector.h"

int GetEditorGlyphsID();
int GetEditorFontID();

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	NodeTree implementation
/*****************************************************************************/
NodeTree::NodeTree()
{
	m_RootID			= 0xFFFFFFFF;
	m_DragID			= 0xFFFFFFFF;
	m_Depth				= 0;

	m_LinesColorBeg		= 0x1199FFFF;
	m_LinesColorEnd		= 0x33AAFFFF;
	m_TextColor			= 0xFF000040;
	m_DefaultNodeColor	= 0xCCD6D3CE;
	
	m_bRightHand		= false;
	m_bSelCollapse		= false;
	m_bShowGlyphs		= true;
	m_bDragLeafsOnly	= true;
	m_bDropToItself		= false;
	m_bDragCopy			= true;
	m_bHasVisibleRoot	= true;
	m_bAcceptOnDrop		= true;
	m_bEditable			= true;

	m_NodeWidth			= 80;
	m_NodeHeight		= 16;
	m_HNodeSpacing		= 10;
	m_VNodeSpacing		= 1;

	m_RootX = 10;
	m_RootY = IRS->GetViewPort().h/2;

} // NodeTree::NodeTree

bool NodeTree::OnMouseLButtonDblclk( int mX, int mY )
{
	return false;
}

bool NodeTree::OnMouseLButtonDown( int mX, int mY )
{
	Node* pNode = PickNode( mX, mY );

	if (pNode)
	{
		Node* pSel  = GetSelectedNode();
		if (pNode == pSel) 
		{
			m_bSelCollapse = !m_bSelCollapse;
		}
		else
		{
			SelectNode( pNode );
			m_bSelCollapse = false;
		}
	}
	return false;
} // NodeTree::OnMouseLButtonDown

bool NodeTree::OnMouseMButtonUp( int mX, int mY )
{
	if (IsDragged())
	{
		EndDrag( mX, mY );
		return true;
	}
	return false; 
} // TreeBrowser::OnMouseMButtonUp

bool NodeTree::OnMouseMButtonDown( int mX, int mY )
{
	Node* pNode = PickNode( mX, mY );
	if (pNode)
	{
		BeginDrag( mX, mY );
	} 
	return false;
} // TreeBrowser::OnMouseMButtonDown

bool NodeTree::OnMouseRButtonDown( int mX, int mY )
{
	Node* pSel = PickNode( mX, mY );
	if (pSel) 
	{
		m_DragID = pSel->GetID(); 
		if (m_bDragLeafsOnly && pSel->GetNChildren() != 0) m_DragID = 0xFFFFFFFF;
		return false;
	}
	
	m_DragID = 0xFFFFFFFF; 
	return false;
} // NodeTree::OnMouseRButtonDown

bool NodeTree::OnMouseRButtonUp( int mX, int mY )
{
	if (m_DragID != 0xFFFFFFFF && !IsInvisible()) 
	{
		IWM->OnDrop( mX, mY, GetID(), m_DragID );
		Node* pNode = NodePool::GetNode( m_DragID );
		if (!m_bDragCopy && pNode)
		{
			Node* pParent = pNode->GetParent();
			if (pParent) pParent->RemoveChild( pNode );
		}
	}
	m_DragID = 0xFFFFFFFF;
	return false;
} // NodeTree::OnMouseRButtonUp

void NodeTree::OnDrop( int mX, int mY, DWORD ctx, DWORD obj )
{
    if (IsInvisible()) return;

	if (!m_bAcceptOnDrop || !m_bEditable) return;
	Node* pParent = PickNode( mX, mY );

	if (!pParent && !m_bHasVisibleRoot && ctx != GetID())
	{
		pParent = GetRootNode();
	}

	Node* pChild  = NodePool::GetNode( obj );	
	if (!pParent || !pChild || pChild == pParent) return;
	if (m_bDropToItself && ctx == GetID()) return;
	
	Node* pClone = pChild->Clone();
	pParent->AddChild( pClone );
	SelectNode( pClone );
	
	m_bSelCollapse = false;
} // NodeTree::OnDrop

bool NodeTree::OnKeyUp( DWORD keyCode, DWORD flags )
{
	return false;
}

bool NodeTree::OnKeyDown( DWORD keyCode, DWORD flags )
{
	if (keyCode == VK_DELETE && m_bEditable)
	{
		Node* pSel = GetSelectedNode();	
		if (!pSel || pSel == GetRootNode()) return false;
		Node* pParent = pSel->GetParent();	
		if (!pParent) return false;
		pParent->RemoveChild( pSel );
		SelectNode( pParent );
		return true;
	}
	return false;
} // NodeTree::OnKeyDown

void NodeTree::SelectPrev()
{

}

void NodeTree::SelectNext()
{

}

void NodeTree::SwapPrev()
{

}

void NodeTree::SwapNext()
{

}

bool NodeTree::OnMouseMove( int mX, int mY, DWORD keys )
{
	m_MX = mX;
	m_MY = mY;

	if (IsDragged())
	{
		if (!(keys & MK_MBUTTON))
		{
			float mx = mX;
			float my = mY;
			EndDrag( mx, my );
		}
		else
		{
			OnDrag( mX, mY );
		}
		return true;
	}
	return false;
} // NodeTree::OnMouseMove

Node* NodeTree::GetSelectedNode() const
{
	Node* pNode = GetRootNode();
	int cDepth = 0;
	while (cDepth < m_Depth)
	{
		Node* pChild = pNode->GetChild( m_Path[cDepth] );
		if (!pChild) return pNode;
		pNode = pChild;
		cDepth++;
	}
	return pNode;
} // NodeTree::GetSelectedNode

Node* NodeTree::GetDraggedNode() const
{
	return NodePool::GetNode( m_DragID );
}

Node* NodeTree::GetRootNode() const
{
	return NodePool::GetNode( m_RootID );
}

void NodeTree::SelectNode( Node* pNode )
{
	Iterator it( GetRootNode() );
	m_Depth = 0;
	while (it)
	{
		Node* pCur = (Node*)it;
		Node* pParent = pCur->GetParent();
        if (pParent && !pParent->Owns( pCur )) 
        { 
            it.Up(); 
            ++it; 
            continue; 
        }
        if (pCur == pNode)
		{
			m_Depth = it.GetDepth();
			memcpy( m_Path, it.GetIdxPath(), m_Depth * sizeof( int ) );
			return;
		}
		++it;
	}
} // NodeTree::SelectNode

void NodeTree::SetRootNode( Node* pNode )
{
	m_RootID = pNode->GetID();
	SelectNode( pNode );
}

void NodeTree::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "NodeTree", this );
	pm.f( "RootX",			m_RootX				);
	pm.f( "RootY",			m_RootY				);
	pm.f( "NodeWidth",		m_NodeWidth			);
	pm.f( "NodeHeight",		m_NodeHeight		);
	pm.f( "HNodeSpacing",	m_HNodeSpacing		);
	pm.f( "VNodeSpacing",	m_VNodeSpacing		);		
	pm.f( "RightHand",		m_bRightHand		);
	pm.f( "ShowGlyphs",		m_bShowGlyphs		);
	pm.f( "DragLeafsOnly",	m_bDragLeafsOnly	);
	pm.f( "DropToItself",	m_bDropToItself		);
	pm.f( "DragCopy",		m_bDragCopy			);
	pm.f( "VisibleRoot",	m_bHasVisibleRoot	);
	pm.f( "AcceptOnDrop",	m_bAcceptOnDrop		);

	pm.f( "LinesColorBeg",	m_LinesColorBeg,	"color" );
	pm.f( "LinesColorEnd",	m_LinesColorEnd,	"color" );
	pm.f( "TextColor",		m_TextColor,		"color" );
	pm.f( "DefaultNodeColor", m_DefaultNodeColor, "color" );
} // NodeTree::Expose

int DetNodeGlyph( Node* pNode );
int	NodeTree::GetNodeGlyph( Node* pNode ) const
{
	return DetNodeGlyph( pNode );
} // NodeTree::GetNodeGlyph

DWORD NodeTree::GetNodeBgColor( Node* pNode ) const 
{ 	
	DWORD clr = pNode->GetColor(); //m_DefaultNodeColor;// 
	if (pNode->GetNChildren() == 0)
	{
		clr &= 0x00FFFFFF;
		clr |= 0x77000000;
	}
	else
	{
		clr &= 0x00FFFFFF;
		clr |= 0xBB000000;
	}
	return clr;
} // NodeTree::GetNodeBgColor

bool NodeTree::DrawNode( Node* pNode, const Rct& rct, Node* pParent, const Rct& prct )
{
	int fontID = GetEditorFontID();
	int glID = GetEditorGlyphsID();
	if (!pNode) return false;

	int glyphID = GetNodeGlyph( pNode );
	DWORD clr = GetNodeBgColor( pNode );

	if (pParent)
	{
		if (m_bRightHand)
		{
			rsLine( rct.GetRight(), rct.y + rct.h/2, 
					prct.x - 1, prct.y + prct.h/2, 
					GetPosZ(), 
					m_LinesColorBeg, m_LinesColorEnd );
		}
		else
		{
			rsLine( prct.GetRight(), prct.y + prct.h/2, 
					rct.x - 1, rct.y + m_NodeHeight/2, 
					GetPosZ(), 
					m_LinesColorBeg, m_LinesColorEnd );
		}
	}
	
	if (pNode == m_RootNode && m_bHasVisibleRoot)
	{
		Rct ruv( 0, 256 - 40, 40, 40 );
		ruv /= 256;
		IWM->DrawChar( glID, Vector3D( rct.x, rct.y, GetPosZ() ), ruv, clr );
		IWM->DrawChar( glID, Vector3D( rct.x + (40-16)/2, rct.y + (40-16)/2, GetPosZ() ), glyphID );
		if (pNode == m_SelNode)
		{
			DrawCircle( rct.GetCenterX(), rct.GetCenterY(), 22, 0, 0xCC1111FF, 32 );
		}
		return false;
	}

	if (!rct.Overlap( m_ClipRct )) return false;

	Rct uv( 256 - 80, 256 - 16, 80, 16 );
	uv /= 256;

	IWM->DrawChar( glID, Vector3D( rct.x, rct.y, GetPosZ() ), uv, clr );

	if (pNode == m_SelNode)
	{
		rsFrame( rct, GetPosZ(), 0xCC1111FF );
	}

	//  draw glyph
	IWM->DrawChar( glID, Vector3D( rct.x, rct.y, GetPosZ() ), glyphID );

	//  draw text
	DWORD textColor = m_TextColor;
	const char* nodeName = pNode->GetName();

	int glyphW = m_bShowGlyphs ? m_NodeHeight : 0;
	Rct txtRct( rct.x + glyphW + 3, rct.y + 2, rct.w - glyphW - 6, rct.h - 2 );

	//  if text is not fitting into our box, then clip it with viewport
	float len = IWM->GetStringWidth( fontID, nodeName );
	if (len > txtRct.w)
	{
		IWM->FlushText( glID );
		IWM->FlushText( fontID );
		Rct vp = IRS->GetViewPort();
		IRS->SetViewPort( txtRct );
		IWM->DrawString( fontID, nodeName, Vector3D( txtRct.x, txtRct.y, GetPosZ() ), textColor );
		IWM->FlushText( fontID );
		IRS->SetViewPort( vp );
	}
	else
	{
		IWM->DrawString( fontID, nodeName, Vector3D( txtRct.x, txtRct.y, GetPosZ() ), textColor );
	}
	return false;
} // NodeTree::DrawNode

Node* NodeTree::PickNode( int mX, int mY )
{
	m_MX = mX;
	m_MY = mY;
	m_PickResult = NULL;
	Iterate( PickNode );
	return m_PickResult;
} // NodeTree::PickNode

bool NodeTree::PickNode( Node* pNode, const Rct& rct, Node* pParent, const Rct& prct )
{
	if (rct.PtIn( m_MX, m_MY )) 
	{
		m_PickResult = pNode;
		m_PickRct	 = rct;
		return true;
	}
	return false;
} // NodeTree::PickNode

void NodeTree::Render()
{
	m_SelNode	= GetSelectedNode();
	m_RootNode	= GetRootNode();
	m_ClipRct	= IRS->GetViewPort();

	Iterate( DrawNode );
	if (m_DragID != 0xFFFFFFFF)
	{
		Node* pDrag = NodePool::GetNode( m_DragID );
		Rct rct( m_MX - m_NodeWidth/2, m_MY - m_NodeHeight/2, m_NodeWidth, m_NodeHeight );
		DrawNode( pDrag, rct );
	}

	IWM->FlushText( GetEditorGlyphsID() );
	IWM->FlushText( GetEditorFontID() );

	rsFlushLines2D();
	rsFlushPoly2D();
} // NodeTree::Render

Rct	NodeTree::GetRootRct() const
{
	return Rct( m_RootX + GetPosX(), m_RootY + GetPosY(), 40, 40 );
} // NodeTree::GetRootRct

void NodeTree::Iterate( ItCallback process )
{
	Node* pRoot		= GetRootNode();
	Node* pNode		= pRoot;
	if (!pNode) return;

	Rct rct( GetRootRct() );
	if (m_bHasVisibleRoot) (this->*process)( pNode, rct, NULL, Rct::null );
	int cDepth = 0;
	Rct pRct( rct );

	while (cDepth <= m_Depth && pNode)
	{
		if (m_bSelCollapse && cDepth == m_Depth) break;
		int nCh = pNode->GetNChildren();

		int colH = nCh*(m_NodeHeight + m_VNodeSpacing) - m_VNodeSpacing;
		rct.y = pRct.y + rct.h/2 - colH/2;
		
		float dw = rct.w + m_HNodeSpacing;
		if (m_bRightHand) dw = -(m_HNodeSpacing + m_NodeWidth);
		rct.x += dw;
		
		rct.w = m_NodeWidth;
		rct.h = m_NodeHeight;

		Rct newPRct( rct );
        Node* pParent = pNode->GetParent();

		for (int i = 0; i < nCh; i++)
		{
			Node* pChild = pNode->GetChild( i );	

            //  skip branches coming from the input nodes
            if (pParent && !pParent->Owns( pNode )) continue;

			Rct nodeRct( rct );

			if (i == m_Path[cDepth]) newPRct = rct;			
			rct.y += m_NodeHeight + m_VNodeSpacing;

			//  skip the node being currently moved
			if (pChild->GetID() == m_DragID && !m_bDragCopy) continue;
			
			//  execute callback for the node
			bool bStop = false;
			if (!m_bHasVisibleRoot && pNode == pRoot)
			{
				bStop = (this->*process)( pChild, nodeRct, NULL, Rct::null );
			}
			else
			{
				bStop = (this->*process)( pChild, nodeRct, pNode, pRct );
			}
			if (bStop) 
			{
				for (int j = cDepth - 1; j >= 0; j--) m_StopPath[j] = m_Path[j];
				m_StopDepth = cDepth;
				m_StopPath[cDepth - 1] = i;
				return;
			}
		}

		pRct = newPRct;
		pNode = pNode->GetChild( m_Path[cDepth] );
		cDepth++;
	}
} // NodeTree::Iterate


END_NAMESPACE(sg)
