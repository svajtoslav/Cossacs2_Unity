/*****************************************************************************/
/*	File:	sgReflection.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	09-18-2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgAssetNode.h"
#include "sgShader.h"
#include "sgTexture.h"
#include "sgCamera.h"
#include "sgDummy.h"
#include "sgRoot.h"
#include "sgGeometry.h"
#include "sgLight.h"
#include "sgStateBlock.h"
#include "kIOHelpers.h"
#include "sgReflection.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	ReflectionMap implementation
/*****************************************************************************/
ReflectionMap::ReflectionMap()
{
	m_BackdropGridNodes		= 8;
	m_ReflectionMapSide		= 256;
	m_bUseDepthBuffer		= false;
	m_ReflectionPlane		= Plane::xOy;
	m_bRenderReflection		= false;
	m_bRenderBackdrop		= false;
    m_bInited               = false;
    m_ReflID                = -1;
    m_DepthID               = -1;
    m_bDrawDebugInfo        = false;

} // ReflectionMap::ReflectionMap

void ReflectionMap::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_BackdropGridNodes << m_bRenderReflection << m_bRenderBackdrop;
}

void ReflectionMap::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_BackdropGridNodes >> m_bRenderReflection >> m_bRenderBackdrop;
}

void ReflectionMap::Render()
{
	if (!m_bInited) Init();

	Rct vp = IRS->GetViewPort();
    if (!IRS->SetRenderTarget( m_ReflID, m_DepthID )) return;

    IRS->SetViewPort( 0, 0, m_ReflectionMapSide, m_ReflectionMapSide );
    Matrix4D viewTM = IRS->GetViewMatrix();
	Matrix4D reflectTM = m_ReflectionPlane.ReflectionTM();
	Matrix4D reflViewTM( viewTM );
    reflViewTM.inverse();
    reflViewTM *= reflectTM;
    reflViewTM.inverse();
    IRS->SetViewMatrix( reflViewTM );
	IRS->ResetWorldMatrix();
	IRS->SetClipPlane( 0, m_ReflectionPlane );

	//  TODO: mirror light sources
    //  render objects
    int nObj = m_Object.size();
    DeviceStateSet::Freeze();
    static int shRefl = IRS->GetShaderID( "reflection_map" );
    IRS->SetCurrentShader( shRefl );
    for (int i = 0; i < nObj; i++)
    {
        const ReflectedObject& obj = m_Object[i];
        IMM->Render( obj.m_ModelID, &obj.m_TM );
    }
    DeviceStateSet::Unfreeze();
	
	//  restore rendering environment
	IRS->SetRenderTarget( 0 );
	IRS->SetViewMatrix( viewTM );
    IRS->SetViewPort( vp );
    
	//  calculate texture matrix
	Matrix4D projTM = IRS->GetProjectionMatrix();
    Matrix4D proj2tex;
	proj2tex.st( Vector3D( 0.5f, -0.5f, 1.0f ), Vector3D( 0.5f, 0.5f, 0.0f ) );
	m_TextureTM = proj2tex;
	m_TextureTM.mulLeft( projTM );

    if (m_bDrawDebugInfo) DrawDebugInfo();
	if (!m_bRenderReflection) return;
	
 //   //  render reflection geometry

    //BaseMesh bm;
    //Rct ext( 0.0f, 0.0f, m_ReflectionMapSide, m_ReflectionMapSide );
    //CreatePatchGrid<VertexTnL>( bm, ext, 
    //    m_BackdropGridNodes, m_BackdropGridNodes );
    //SetW<VertexTnL>( bm, 1.0f );

	//static BaseMesh quad;
	//if (quad.getNVert() == 0)
	//{
	//	float c_Ext = 1000.0f;
	//	Rct ext( -c_Ext, -c_Ext, c_Ext * 2.0f, c_Ext * 2.0f );
	//	CreatePatchGrid<Vertex2t>( quad, ext, 1, 1 );
	//}

	//m_pTexture->Render();
	//IRS->ResetWorldMatrix();

	//static int reflDSS = IRS->GetShaderID( "reflection" );
	//IRS->SetCurrentShader( reflDSS );

	//TransformNode::ResetTMStack();
	//IRS->DrawPrim( quad );

} // ReflectionMap::Render

Matrix4D ReflectionMap::GetReflectionTexTM() const
{
	return m_TextureTM;
}

void ReflectionMap::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ReflectionMap", this );
	pm.f( "ReflectionMapSide",  m_ReflectionMapSide );
	pm.f( "UseDepthBuffer",     m_bUseDepthBuffer   );
	pm.f( "BackdropGridNodes",  m_BackdropGridNodes );
	pm.f( "RenderReflection",   m_bRenderReflection );
	pm.f( "RenderBackdrop",     m_bRenderBackdrop   );
    pm.f( "DebugInfo",          m_bDrawDebugInfo    );
	pm.m( "ChangeStructure",    Init                );
} // ReflectionMap::Expose

void ReflectionMap::Init()
{    
    TextureDescr rTD, dTD;
    rTD.setValues( m_ReflectionMapSide, m_ReflectionMapSide, cfRGB565, mpVRAM, 1, tuRenderTarget );
    dTD.setValues( m_ReflectionMapSide, m_ReflectionMapSide, cfRGB565, mpVRAM, 1, tuDepthStencil );
    dTD.setDsFmt( dsfD16 );
    
    float texMem        = IRS->GetTexMemorySize();
    int tmMB = texMem/1024.0f/1024.0f;
    Log.Info( "Creating reflection map. TexMem: %dMb", tmMB );


    m_ReflID = IRS->CreateTexture( "ReflectionTarget", rTD );
    if (m_bUseDepthBuffer)
    {
        m_DepthID = IRS->CreateTexture( "ReflectionDepth", dTD );
    }
    m_bInited = true;
} // ReflectionMap::Init

void ReflectionMap::AddObject( DWORD id, const Matrix4D* objTM )
{
    if (!objTM) return;
	m_Object.push_back( ReflectedObject( id, *objTM ) );
} // ReflectionMap::AddObject

void ReflectionMap::CleanObjects()
{
    m_Object.clear();
} // ReflectionMap::CleanObjects

int ReflectionMap::GetReflectionTextureID() const
{
	return m_ReflID;
} // ReflectionMap::GetReflectionTextureID

void ReflectionMap::DrawDebugInfo()
{

} // ReflectionMap::DrawDebugInfo

END_NAMESPACE(sg)
