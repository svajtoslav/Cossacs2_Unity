/*****************************************************************************/
/*	File:	sgNode.cpp
/*	Desc:	Scene graph node
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "kIOHelpers.h"
#include "kMathTypeTraits.h"
#include "sgMovable.h"

#ifndef _INLINES 
#include "sgMovable.inl"
#endif // _INLINES

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	TransformNode implementation
/*****************************************************************************/
MatrixStack		TransformNode::s_TMStack;
void TransformNode::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_InitialTM;
	tm = m_InitialTM;
} // TransformNode::Unserialize

void TransformNode::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << GetTransform();
} // TransformNode::Serialize

void TransformNode::Render()
{
	PushTM( tm );
	Node::Render();
	PopTM();
} // TransformNode::Render

void TransformNode::ResetTMStack( const Matrix4D* pTm )
{
	if (pTm) s_TMStack.Reset( *pTm ); else s_TMStack.Reset();
} // TransformNode::ResetTMStack

void TransformNode::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "TransformNode", this );
	pm.p( "ScaleX", GetScaleX, SetScaleX );
	pm.p( "ScaleY", GetScaleY, SetScaleY );
	pm.p( "ScaleZ", GetScaleZ, SetScaleZ );

	pm.p( "PosX", GetPosX, SetPosX );
	pm.p( "PosY", GetPosY, SetPosY );
	pm.p( "PosZ", GetPosZ, SetPosZ );

	pm.p( "RotX", GetEulerX, SetEulerX );
	pm.p( "RotY", GetEulerY, SetEulerY );
	pm.p( "RotZ", GetEulerZ, SetEulerZ );
	pm.m( "Reset", Reset );
	pm.m( "SetToInitial", SetToInitial );
	pm.m( "SetSubtreeToInitial", SetSubtreeToInitial );
    pm.m( "FlipAxis", FlipAxis );
} // TransformNode::Expose

void TransformNode::FlipAxis()
{
    tm.getV1().reverse();
    tm.getV2().reverse();
}

void TransformNode::SetToInitial()
{
	tm = m_InitialTM;
}

void TransformNode::SetSubtreeToInitial()
{
	Iterator it( this, TransformNode::FnFilter );
	while (it)
	{
		((TransformNode*)(Node*)it)->SetToInitial();
		++it;
	}
} // TransformNode::SetSubtreeToInitial

Matrix4D TransformNode::GetWorldTM() const
{
	Matrix4D m = GetTransform();
	m *= GetParentWorldTM();
	return m;
} // TransformNode::GetWorldTM

Matrix4D TransformNode::GetParentWorldTM() const
{
	Matrix4D m = Matrix4D::identity;
	Node* pNode = GetParent();
	while (pNode)
	{
		if (pNode->IsA<TransformNode>())
		{
			m *= ((TransformNode*)pNode)->GetTransform();
		}
        Node* pParent = pNode->GetParent();
		if (pNode == pParent) break;
        pNode = pParent;
	}
	return m;
} // TransformNode::GetParentWorldTM

void TransformNode::SetWorldTM( const Matrix4D& wTM )
{
	Matrix4D pTM = GetParentWorldTM();
	Matrix4D lTM;
	lTM.inverse( pTM );
	lTM.mulLeft( wTM );
	SetTransform( lTM );
} // TransformNode::SetWorldTM

/*****************************************************************************/
/*	HudNode implementation
/*****************************************************************************/
void HudNode::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << pos << width << height << scale;
} // HudNode::Serialize

void HudNode::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> pos >> width >> height >> scale;
} // HudNode::Unserialize

Rct HudNode::GetBounds()
{
	return Rct( pos.x, pos.y, width, height );
}

void HudNode::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "HudNode", this );
	pm.p( "PosX",		GetPosX,	SetPosX );
	pm.p( "PosY",		GetPosY,	SetPosY );
	pm.p( "PosZ",		GetPosZ,	SetPosZ );

	pm.p( "Width",		GetWidth,	SetWidth );
	pm.p( "Height",	GetHeight,	SetHeight );

	pm.p( "Scale",		GetScale,	SetScale );
} // HudNode::Expose

/*****************************************************************************/
/*	Transform2D implementation
/*****************************************************************************/
void Transform2D::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << tm;
}

void Transform2D::Unserialize( InStream& is	)
{
	Parent::Unserialize( is );
	is >> tm;
}


Matrix4D GetWorldTM( Node* pNode )
{
	while (pNode && !pNode->IsA<TransformNode>()) 
    {
        Node* pParent = pNode->GetParent();
        if (pParent == pNode) break;
        pNode = pParent;
    }
	if (pNode) return ((TransformNode*)pNode)->GetWorldTM();
	return Matrix4D::identity;
}

END_NAMESPACE( sg )
