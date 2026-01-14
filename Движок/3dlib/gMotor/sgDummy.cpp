/*****************************************************************************/
/*	File:	sgDummy.cpp
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgDummy.h"
#include "sgModel.h"
#include "sgGeometry.h"
#include "kIOHelpers.h"
#include "ITerrain.h"

#ifndef _INLINES
#include "sgDummy.inl"
#endif // !_INLINES

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	ZBias implementation
/*****************************************************************************/
ZBias::ZBias()
{
	m_Bias = 0.0000005f;
}

void ZBias::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Bias;
} // ZBias::Serialize

void ZBias::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Bias;
} // ZBias::Unserialize

void ZBias::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ZBias", this );
	pm.f( "Bias", m_Bias );
} // ZBias::Expose

void ZBias::Render()
{
	sg::BaseCamera* pC = BaseCamera::GetActiveCamera();
	if (!pC) return;

	pC->ZBiasIncrease( m_Bias );
	Matrix4D prM;
	pC->GetProjM( prM );
	IRS->SetProjectionMatrix( prM );

	Node::Render();

	pC->ZBiasDecrease( m_Bias );
	pC->GetProjM( prM );
	IRS->SetProjectionMatrix( prM );
} // ZBias::Render

/*****************************************************************************/
/*	Bone implementation
/*****************************************************************************/
void Bone::Render()
{
	s_TMStack.Push( tm );
	m_WorldTM = s_TMStack.Top();
	Node::Render();
	s_TMStack.Pop();
} // Bone::Render

/*****************************************************************************/
/*	Switch implementation
/*****************************************************************************/
void Switch::Render()
{
	if (curActive == -1) return;
	Node* pActive = GetChild( curActive );
	if (pActive) pActive->Render();
} // Switch::Render

void Switch::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << curActive;
} // Switch::Serialize

void Switch::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> curActive;
} // Switch::Unserialize

void Switch::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Switch", this );
	pm.p( "ActiveChild", GetActive, SwitchTo );
} // Switch::Expose

void Switch::SwitchTo( int nodeIdx )
{
	if (nodeIdx == curActive || nodeIdx < -1 || nodeIdx >= GetNChildren()) return;

	if (curActive >= 0)
	{
		if (curActive >= GetNChildren()) return;
		GetChild( curActive )->SetInvisible();
	}

	curActive = nodeIdx;
	
	if (nodeIdx < 0) return;
	if (nodeIdx >= GetNChildren()) 
	{
		curActive = -1;
		return;
	}
	
	GetChild( curActive )->SetInvisible( false );
	GetChild( curActive )->Activate();

} // Switch::SwitchTo

/*****************************************************************************/
/*	Model implementation
/*****************************************************************************/
void Model::Render()
{
    if (m_ModelID == 0xFFFFFFFF)
    {
        m_ModelID = IMM->GetModelID( m_ModelName.c_str() );
    }
    IMM->Render( m_ModelID );
} // Model::Render

void Model::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_ModelName;
}

void Model::Unserialize( InStream& is )
{
    Parent::Unserialize( is );
    is >> m_ModelName;
}

void Model::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "Model", this );
    pm.p( "FileName", GetModelName, SetModelName, "file|Models" );
}

/*****************************************************************************/
/*	FieldPatch implementation
/*****************************************************************************/
FieldPatch::FieldPatch()
{
    m_PatchesW  = 8.0f;
    m_PatchesH  = 8.0f;
    m_Width     = 500.0f;
    m_Height    = 500.0f;
    m_Age       = 1.0f;
} // FieldPatch::FieldPatch

void FieldPatch::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_PatchesW << m_PatchesH << m_Width << m_Height << m_Age;      
}

void FieldPatch::Unserialize( InStream& is )
{
    Parent::Unserialize( is );
    is >> m_PatchesW >> m_PatchesH >> m_Width >> m_Height >> m_Age;
}

void FieldPatch::Render()
{
    float patchW    = m_Width / float( m_PatchesW );
    float patchH    = m_Height / float( m_PatchesH );
    float hWidth    = m_Width*0.5f;
    float hHeight   = m_Height*0.5f;
    const Matrix4D& tm = TransformNode::TMStackTop();
    for (int i = 0; i < m_PatchesH; i++)
    {
        for (int j = 0; j < m_PatchesW; j++)
        {
            float x0 = i*patchW - hWidth;
            float y0 = j*patchH - hHeight;
            const float c_Cos45 = 0.707106781186f;
            float x = x0*c_Cos45 - y0*c_Cos45 + tm.e30;
            float y = x0*c_Cos45 + y0*c_Cos45 + tm.e31;
            bool res = false;
            while (!res)
            {
               float D = patchW * c_Cos45;
               Vector3D lt( x, y + D, ITerra->GetH( x, y + D ) );
               Vector3D rt( x + D, y, ITerra->GetH( x + D, y ) );
               Vector3D lb( x - D, y, ITerra->GetH( x - D, y ) );
               Vector3D rb( x, y - D, ITerra->GetH( x, y - D ) );
               res = m_Field.AddPatch( lt, rt, lb, rb, m_Age, 1.0f );
               if (res) break; 
               m_Field.Draw();
            }
        }
    }
    m_Field.Draw();
} // FieldPatch::Render

void FieldPatch::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "FieldPatch", this );
    pm.f( "PatchesW",   m_PatchesW  );
    pm.f( "PatchesH",   m_PatchesH  );
    pm.f( "Width",      m_Width     ); 
    pm.f( "Height",     m_Height    );
    pm.f( "Age",        m_Age       );
}

END_NAMESPACE( sg )