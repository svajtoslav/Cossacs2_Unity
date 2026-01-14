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
#include "sgStateBlock.h"
#include "kIOHelpers.h"

#include "mHeightmap.h"
#include "akField.h"
#include "sgFog.h"

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	Fog implementation
/*****************************************************************************/
Fog::Fog()
{
	m_Color			= 0xFFFFFFFF;
	m_Start			= 1.0f;
	m_End			= 100.0f;

	m_Density		= 0.5f;
	m_Type			= ftVertex;
	m_Mode			= fmLinear;

	m_bRangeBased	= false;
	m_bEnabled		= true;
}

Fog::~Fog()
{
}

void Fog::Render()
{
	if (m_bEnabled)
	{
		IRS->SetFog( this );
		Node::Render();
		IRS->SetFog( NULL );
	}
	else
	{
		Node::Render();
	}
} // Fog::Render

void Fog::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Color << m_Start << m_End << m_Density;
	BYTE bType = (BYTE) m_Type;
	BYTE bMode = (BYTE) m_Mode;

	os << bType << bMode;
	os << m_bRangeBased;
} // Fog::Serialize

void Fog::Unserialize( InStream& is	)
{
	Parent::Unserialize( is );
	is >> m_Color >> m_Start >> m_End >> m_Density;
	BYTE bType;
	BYTE bMode;
	is >> bType >> bMode;
	m_Type = (FogType)bType;
	m_Mode = (FogMode)bMode;
	is >> m_bRangeBased;
} // Fog::Unserialize

void Fog::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Fog", this );
	pm.f( "Color",		m_Color, "color" );
	pm.f( "Start",		m_Start			);
	pm.f( "End",			m_End			);

	pm.f( "Density",		m_Density		);
	pm.f( "Type",			m_Type			);
	pm.f( "Mode",			m_Mode			);

	pm.f( "Range Based",	m_bRangeBased	);
	pm.f( "Enabled",		m_bEnabled		);
}

/*****************************************************************************/
/*	WaterPatch implementation
/*****************************************************************************/
WaterPatch::WaterPatch()
{
	m_WaterLevel	= 0.0f;
	m_WaveSpeed		= 8000.0f;
	m_PosX			= m_PosY = 0.0f;
	m_Side			= 2000.0f;
	m_SplashAmount	= 150.0f;
	m_SplashX		= 0.0f;
	m_SplashY		= 0.0f;
	m_SideSegments	= 32;
	m_DefaultWaterColor = 0xBB3333FF;
}

void WaterPatch::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "WaterPatch", this );
	pm.f	( "Side",			m_Side			);
	pm.p 	( "SideSegments",	GetNSideSegments, SetNSideSegments );
	pm.f	( "PosX",			m_PosX			);
	pm.f	( "PosY",			m_PosY			);
	pm.f	( "WaveSpeed",		m_WaveSpeed 	);
	pm.f	( "WaterLevel",		m_WaterLevel	);
	pm.m	( "Generate",		Generate		);
	pm.m	( "Splash",			Splash			);
	pm.f	( "SplashAmount",	m_SplashAmount	);
	pm.f	( "SplashX",		m_SplashX		);
	pm.f	( "SplashY",		m_SplashY		);
	pm.f	( "DefaultWaterColor", m_DefaultWaterColor, "color" );
} // WaterPatch::Expose

void WaterPatch::Generate()
{
	RemoveChildren();
	StateBlock* pStateBlock  = AddChild<StateBlock>( "water" );
	RenderStateBlock* pRS	 = pStateBlock->AddChild<RenderStateBlock>( "WaterRS" );
	pRS->EnableLighting();
	pRS->EnableSpecular();
	pRS->EnableDithering();
	pRS->EnableColorVertex();
	pRS->SetCullMode( cmCCW );

	TextureStateBlock* pTSS0 = pStateBlock->AddChild<TextureStateBlock>( "WaterTSS0" );
	pTSS0->SetAlphaOp( toModulate, taTexture, taDiffuse );
	pTSS0->SetColorOp( toModulate, taTexture, taDiffuse );
	pTSS0->SetSampling( tfLinear );

	TextureStateBlock* pTSS1 = pStateBlock->AddChild<TextureStateBlock>( "WaterTSS1" );
	pTSS1->Disable();
	pTSS1->SetStage( 1 );

	AddChild<Texture>( "oblaka123g.tga" );

	m_WaterHeight.		SetNSideNodes( m_SideSegments + 1 );
	m_WaterHeightPrev.	SetNSideNodes( m_SideSegments + 1 );
	m_Damping.			SetNSideNodes( m_SideSegments + 1 );
	m_WaterColor.		SetNSideNodes( m_SideSegments + 1 );
	
	m_WaterHeight.		SetHeight( m_WaterLevel );
	m_WaterHeightPrev.	SetHeight( m_WaterLevel );
	m_Damping.			SetHeight( 0.9999999f );
	m_WaterColor.		SetValue ( m_DefaultWaterColor );

	m_WaterHeight.		Scale( 24.0f );
	m_WaterHeightPrev.	Scale( 24.0f );

	Rct rct( -m_Side, -m_Side, m_Side*2.0f );
	AABoundBox aabb( Vector3D::null, m_Side, m_Side, 0.0f );
	m_WaterHeight.		SetAABB( aabb );
	m_WaterHeightPrev.	SetAABB( aabb );
	m_Damping.			SetAABB( aabb );

	float step = m_Side * 2.0f / float( m_SideSegments );
	m_WaterHeight.		SetGridStep( step );
	m_WaterHeightPrev.	SetGridStep( step );
	m_Damping.			SetGridStep( step );


	CreatePatchGrid<VertexN2T>( GetPrimitive(), rct, m_SideSegments, m_SideSegments );
} // WaterPatch::Create

void WaterPatch::Splash( const Vector3D& location, float radius )
{
	m_WaterHeightPrev.SetHeight( location.x, location.y, location.z );
	m_WaterHeight.SetHeight( location.x, location.y, location.z );

} // WaterPatch::Splash

void WaterPatch::Splash()
{
	Splash( Vector3D( m_SplashX, m_SplashY, m_SplashAmount ), m_Side * 0.5f );
} // WaterPatch::Splash

void WaterPatch::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_WaterHeight << 
			m_Damping << m_WaterHeightPrev << m_WaterColor << 
			m_WaterLevel << m_WaveSpeed;
} // WaterPatch::Serialize

void WaterPatch::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_WaterHeight >> 
			m_Damping >> m_WaterHeightPrev >> m_WaterColor >> 
			m_WaterLevel >> m_WaveSpeed;
} // WaterPatch::Unserialize

void WaterPatch::Render()
{
	UpdateGrid();
	UpdateMesh();

	Geometry::Render();
} // WaterPatch::Render

void WaterPatch::SetWaterLevel( float val )
{
	float dh = val = m_WaterLevel;
	int nNodes = m_WaterHeight.GetNSideNodes();
	for (int j = 0; j < nNodes; j++)
	{
		for (int i = 0; i < nNodes; i++)
		{
			m_WaterHeight.SetHeight( i, j, m_WaterHeight.GetHeight( i, j ) + dh );
			m_WaterHeightPrev.SetHeight( i, j, m_WaterHeight.GetHeight( i, j ) + dh );
		}
	}
	m_WaterLevel = val;
} // WaterPatch::SetWaterLevel

void WaterPatch::UpdateGrid()
{
	static DWORD cTime = GetTickCount();
	DWORD newTime = GetTickCount();
	
	if (newTime - cTime > 5) newTime = cTime + 5;

	float dt = float( newTime - cTime );
	dt *= 0.001f;
	cTime = newTime;
	
	float ch = m_Side;

	const float A = (m_WaveSpeed*dt/ch)*(m_WaveSpeed*dt/ch);
	const float B = 2.0f - 4.0f*A;
	
	HeightMap& z = m_WaterHeight;
	int nNodes = m_WaterHeight.GetNSideNodes();
	for (int j = 0; j < nNodes; j++)
	{
		for (int i = 0; i < nNodes; i++)
		{
			float h = A * (z.GetHeight( i - 1, j ) + z.GetHeight( i + 1, j ) + 
						 z.GetHeight( i, j - 1 ) + z.GetHeight( i, j + 1 )) + 
						 B * z.GetHeight( i, j ) - m_WaterHeightPrev.GetHeight( i, j );
			h *= m_Damping.GetHeight( i, j );
			m_WaterHeightPrev.SetHeight( i, j, h );
		}
	}
	
	m_WaterHeightPrev.Swap( z );
} // WaterPatch::UpdateGrid

void WaterPatch::UpdateMesh()
{
	if (m_WaterHeight.GetSize() == 0) return;
	VertexIterator vit;
	vit << GetPrimitive();
	int nV = GetPrimitive().getNVert();
	if (nV == 0) return;
	int nNodes = m_WaterHeight.GetNSideNodes();
	for (int j = 0; j < nNodes; j++)
	{
		for (int i = 0; i < nNodes; i++)
		{
			Vector3D& v = vit;
			v.z				= m_WaterHeight.GetHeight( i, j );
			vit.n()			= m_WaterHeight.GetNormal( i, j );
			vit.diffuse()	= m_WaterColor.GetValue( i, j );
			vit++;
		}
	}
} // WaterPatch::UpdateMesh

void WaterPatch::SetNSideSegments( int val )
{
	m_SideSegments = val;
	Generate();
} // WaterPatch::SetNSideNodes

/*****************************************************************************/
/*	WaterScape implementation
/*****************************************************************************/
WaterScape::WaterScape() 
{
} // WaterScape::WaterScape

void WaterScape::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // WaterScape::Serialize

void WaterScape::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
} // WaterScape::Unserialize

void WaterScape::Render()
{
	Parent::Render();
} // WaterScape::Render

void WaterScape::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "WaterScape", this );
} // WaterScape::Expose

END_NAMESPACE(sg)
