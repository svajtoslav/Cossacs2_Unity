#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgAssetNode.h"
#include "rsDeviceStates.h"
#include "sgTexture.h"
#include "sgCamera.h"

#ifndef _INLINES
#include "sgTexture.inl"
#endif // _INLINES

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	RenderTarget implementation
/*****************************************************************************/
RenderTarget::RenderTarget() : m_pTexture(NULL), m_pDepthBuffer(NULL)
{
}

RenderTarget::~RenderTarget()
{
}

void RenderTarget::Render()
{ 
	if (!m_pTexture) 
	{
		Node* pNode = GetInput( 0 );
		if (pNode && pNode->IsA<Texture>() )
		{
			m_pTexture = (Texture*)pNode;
		}
	}

	DWORD depthID = m_pDepthBuffer ? m_pDepthBuffer->GetTexID() : -1;

	if (m_pTexture) IRS->SetRenderTarget( m_pTexture->GetTexID(), depthID );
	
	Rct curVP = IRS->GetViewPort();
	IRS->SetViewPort( m_ViewPort );

	if (m_bClear) 
	{
		if (m_pDepthBuffer && m_pDepthBuffer->GetTexID() != -1) 
		{
			IRS->ClearDevice( true, m_ClearColor, true, false );
		}
		else
		{
			IRS->ClearDeviceTarget( m_ClearColor );
		}
	}
	Node::Render();
	if (m_pTexture) IRS->SetRenderTarget( 0 );
	IRS->SetViewPort( curVP );

} // RenderTarget::Render

void RenderTarget::SetViewPort( float x, float y, float w, float h )
{
	m_ViewPort.Set( x, y, w, h );
}

void RenderTarget::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "RenderTarget", this );
	pm.f( "Clear",		m_bClear );
	pm.f( "ClearColor", m_ClearColor, "color" );
} // RenderTarget::Expose

void RenderTarget::Serialize( OutStream& os ) const
{
	Parent::Serialize( os ); 
	os << m_ClearColor << m_bClear;
}

void RenderTarget::Unserialize( InStream&  is )
{
	Parent::Unserialize( is ); 
	is >> m_ClearColor >> m_bClear;
}

void RenderTarget::SetTarget(Texture* pTex, Texture* pDepth )
{ 
	m_pTexture		= pTex; 
	m_pDepthBuffer	= pDepth;
	m_ViewPort.Set( 0.0f, 0.0f, pTex->GetWidth(), pTex->GetHeight() );
}

/*****************************************************************************/
/*	Texture implementation
/*****************************************************************************/
Texture* Texture::s_pCurTexture[Texture::c_MaxStages];
bool	 Texture::s_bFrozen = false;

void Texture::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Stage;
	os.Write( &m_Descr, sizeof( m_Descr ) );
} // Texture::Serialize

void Texture::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Stage;
	is.Read( &m_Descr, sizeof( m_Descr ) );
	m_Descr.setID( -1 );

	if (m_Descr.getTexUsage() == tuProcedural	||
		m_Descr.getTexUsage() == tuDepthStencil ||
		m_Descr.getTexUsage() == tuRenderTarget)
	{
		CreateTexture();
	}

} // Texture::Unserialize

void Texture::VisitAttributes()
{
	s_pCurTexture[m_Stage] = this;
}

bool Texture::CreateTexture()
{
	if (!IRS) return false;
	if (m_TexID != -1)
	{
		IRS->DeleteTexture( m_TexID );
		m_TexID = -1;
	}
	m_TexID = IRS->CreateTexture( GetName(), m_Descr );
	return false;
} // Texture::CreateTexture

void Texture::CreateMipLevels()
{
	IRS->CreateMipLevels( m_TexID );
} // Texture::CreateMipLevels

BYTE* Texture::LockBits( int& pitch, int level )
{
	return IRS->LockTexBits( m_TexID, pitch, level );
} // Texture::LockBits

void Texture::UnlockBits( int level )
{
	IRS->UnlockTexBits( m_TexID, level );
} // Texture::UnlockBits

bool Texture::IsEqual( const Node* node ) const
{
	if (!Parent::IsEqual( node )) return false;
	Texture* pTex = (Texture*)node;
	return (m_Stage == pTex->m_Stage) && (m_Descr.equal( pTex->m_Descr ));
} // Texture::IsEqual

bool Texture::Save( const char* fname )
{
	if (m_TexID == -1) return false;
	IRS->SaveTexture( m_TexID, fname );
	return true;
} // Texture::Save

void Texture::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Texture", this );
	pm.p( "TexID",			GetTexID, SetTexID			);
	pm.p( "Stage",			GetStage, SetStage			);
	pm.p( "Width",			GetWidth					);
	pm.p( "Height",			GetHeight					);
	pm.p( "ColorFormat",	GetColorFormat	 			);
	pm.p( "DepthStencil",	GetDepthStencilFormat		);
	pm.p( "Procedural",		IsProcedural	 			);
	pm.p( "RenderTarget",	IsRT				);
	pm.p( "MemoryPool",		GetMemoryPool	 			);
	pm.p( "Usage",			GetUsage		 			);
	pm.p( "NMips",			GetNMips		 			);
	pm.p( "Pixels",			GetTexID, SetTexID, "texture" );
	pm.p( "File",			GetTextureFile, SetTextureFile, "file|Textures" );
} // Texture::Expose

void Texture::SetTextureFile( const char* file )
{
	char drive		[_MAX_DRIVE];
	char directory	[_MAX_DIR  ];
	char filename	[_MAX_PATH ];
	char ext		[_MAX_EXT  ];

	_splitpath( file, drive, directory, filename, ext );

	strcat( filename, ext );
	
	SetName( filename );
	m_TexID = -1;
} // DeviceStateSet::SetScriptFile

END_NAMESPACE(sg)
