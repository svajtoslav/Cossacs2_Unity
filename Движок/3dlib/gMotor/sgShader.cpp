/*****************************************************************************/
/*	File:	sgShader.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgAssetNode.h"
#include "rsDeviceStates.h"
#include "sgShader.h"
#include "sgTexture.h"

#ifndef _INLINES
#include "sgShader.inl"
#endif // !_INLINES

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Material implementation
/*****************************************************************************/
Material*			Material::s_pCurMtl = NULL;

void Material::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Ambient << m_Diffuse << m_Specular << m_Shininess << m_Transparency;
} // Material::Serialize

void Material::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Ambient >> m_Diffuse >> m_Specular >> m_Shininess >> m_Transparency;
} // Material::Unserialize

void Material::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Material", this );
	pm.f( "Ambient",		m_Ambient,	"color" );
	pm.f( "Diffuse",		m_Diffuse,	"color" );
	pm.f( "Specular",		m_Specular,	"color" );
	pm.f( "Shininess",	m_Shininess );
	pm.f( "Transparency", m_Transparency );
} // Material::Expose

bool Material::IsEqual( const Node* node ) const
{
	if (!Parent::IsEqual( node )) return false;
	Material* pMtl = (Material*)node;
	return (m_Ambient		== pMtl->m_Ambient		) &&
		   (m_Diffuse		== pMtl->m_Diffuse		) &&
		   (m_Specular		== pMtl->m_Specular		) &&
		   (m_Shininess		== pMtl->m_Shininess	) &&
		   (m_Transparency	== pMtl->m_Transparency	);
} // Material::IsEqual

/*****************************************************************************/
/*	DeviceStateSet implementation
/*****************************************************************************/
bool DeviceStateSet::s_bFreeze = false;

void DeviceStateSet::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // DeviceStateSet::Serialize

void DeviceStateSet::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
} // DeviceStateSet::Unserialize

void DeviceStateSet::Update()
{
	IRS->RecompileAllShaders();
}

void DeviceStateSet::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "DeviceStateSet", this );
	pm.p( "Script", GetScriptFile, SetScriptFile, "file|Shaders\\DeviceStates" );
} // DeviceStateSet::Expose

void DeviceStateSet::SetScriptFile( const char* file )
{
	char drive		[_MAX_DRIVE];
	char directory	[_MAX_DIR  ];
	char filename	[_MAX_PATH ];
	char ext		[_MAX_EXT  ];

	_splitpath( file, drive, directory, filename, ext );

	SetName( filename );
	m_StateBlockHandle = -1;
} // DeviceStateSet::SetScriptFile

/*****************************************************************************/
/*	TextureMatrix implementation
/*****************************************************************************/
TextureMatrix::TextureMatrix()
{
	m_Stage = 0;
	tm.setIdentity();
} // TextureMatrix::TextureMatrix

void TextureMatrix::Render()
{
	IRS->SetTextureMatrix( tm, m_Stage );
	Node::Render();
} // TextureMatrix::Render

void TextureMatrix::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Stage;
} // TextureMatrix::Serialize

void TextureMatrix::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Stage;
} // TextureMatrix::Unserialize

void TextureMatrix::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "TextureMatrix", this );
	pm.f( "Stage", m_Stage );
} // TextureMatrix::Expose

void TextureMatrix::SetTextureTM( const Matrix4D& m )
{
	tm.e00 = m.e00; tm.e01 = m.e01; tm.e02 = 0.0f; tm.e03 = 0.0f;
	tm.e10 = m.e10; tm.e11 = m.e11; tm.e00 = 0.0f; tm.e01 = 0.0f;
	tm.e00 = m.e30; tm.e01 = m.e31; tm.e00 = 1.0f; tm.e01 = 0.0f;
	tm.e00 = 0.0f;  tm.e01 = 0.0f;  tm.e00 = 0.0f; tm.e01 = 1.0f;
} // TextureMatrix::SetTextureTM

/*****************************************************************************/
/*	BumpMatrix implementation
/*****************************************************************************/
void BumpMatrix::Render()
{
	IRS->SetBumpMatrix( tm, m_Stage );
	Node::Render();
} // BumpMatrix::Render

void BumpMatrix::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Stage;
} // BumpMatrix::Serialize

void BumpMatrix::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Stage;
} // BumpMatrix::Unserialize

void BumpMatrix::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "BumpMatrix", this );
	pm.f( "Stage", m_Stage );
	pm.f( "e00",	 tm.e00 );
	pm.f( "e01",	 tm.e01 );
	pm.f( "e10",	 tm.e10 );
	pm.f( "e11",	 tm.e11 );
	pm.f( "e11",	 tm.e11 );
	pm.f( "LuminanceScale", tm.e20 );
	pm.f( "LuminanceOffset", tm.e21 );

} // BumpMatrix::Expose

/*****************************************************************************/
/*	DetailMap implementation
/*****************************************************************************/
DetailMap::DetailMap() : m_UVScale( 8.0f )
{
}

void DetailMap::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "DetailMap", this );
	pm.p( "UVScale", GetUVScale, SetUVScale );
} // DetailMap::Expose

void DetailMap::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_UVScale;
} // DetailMap::Serialize

void DetailMap::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_UVScale;
} // DetailMap::Unserialize

void DetailMap::Render()
{
	Parent::Render();
} // DetailMap::Render

void DetailMap::SetUVScale( float scale )
{
	m_UVScale = scale;
	Matrix4D tm;
	tm.scaling( scale );
	if (m_pTextureTM) m_pTextureTM->SetTransform( tm );
} // DetailMap::SetUVScale

void DetailMap::Init()
{
	m_pTexture	 = GetChild<Texture>		( "detail\\default.jpg" );
	m_pDSS		 = GetChild<DeviceStateSet>	( "detail" );
	m_pTextureTM = GetChild<TextureMatrix>	( "detailUV" );
	
	m_pTexture->SetStage	( c_DetailTextureStage );
	m_pTextureTM->SetStage	( c_DetailTextureStage );

	SetUVScale( m_UVScale );
} // DetailMap::Init

END_NAMESPACE( sg )