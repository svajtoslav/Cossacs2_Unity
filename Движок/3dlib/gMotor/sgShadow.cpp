/*****************************************************************************/
/*	File:	sgShadow.cpp
/*	Desc:	Shadow mapping vodoo
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-28-2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgHardwareCaps.h"
#include "sgDecal.h"
#include "sgShadow.h"

#include "IMediaManager.h"
#include "mHeightmap.h"
#include "uiControl.h"
#include "sgTerrain.h"
#include "IShadowManager.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	ShadowMapper implementation
/*****************************************************************************/
ShadowMapper::ShadowMapper()
{
	m_pLightSource = NULL;
	m_pLightSource = Root::instance()->FindChild<DirectionalLight>( "GameLight" );
	AddInput( m_pLightSource );
}

void ShadowMapper::AddCaster( DWORD nodeID, const Matrix4D& tm )
{
	ShadowCaster caster( nodeID, tm );
	Node* pNode = NodePool::instance().GetNode( nodeID );
	if (!pNode) return;
	caster.m_AABB = CalculateAABB( pNode );
	caster.m_AABB.Transform( tm );

	m_Casters.push_back( caster );
} // ShadowMapper::AddCaster

/*****************************************************************************/
/*	BlobShadowMapper implementation
/*****************************************************************************/
BlobShadowMapper::BlobShadowMapper()
{
	m_pTextureMatrix = NULL;

	m_BlobWidth			= 120.0f;
	m_BlobHeight		= 50.0f;
	m_BlobType			= 0;
	m_BlobUV.Set( 0.0f, 0.0f, 1.0f, 1.0f );
	m_ModelPivotHeight	= 20.0f;
	m_BlobColor			= 0xFF000000;
	m_Pivot.zero();
}

void BlobShadowMapper::Render()
{	
	//if (!m_pShadowCaster || !m_pLightCamera) return;

	//OrthoCamera* pCam = (OrthoCamera*) m_pLightCamera;
	//pCam->SetOrthoW( m_BlobWidth, m_BlobWidth / m_BlobHeight, 1.0f, 10000.0f );
	//pCam->SetPos( m_Pivot  );
	//
	//BaseCamera* pCurCamera = BaseCamera::GetActiveCamera();
	//if (!pCurCamera) return;
	//
	//Matrix4D texTM;

	////  light camera projection space to texture space matrix
	//Matrix4D proj2tex;
	//proj2tex.st( Vector3D( 0.5f, -0.5f, 1.0f ), Vector3D( 0.5f, 0.5f, 0.0f ) );
	////  current camera view space to world space
	//const Matrix4D& cam2world = pCurCamera->GetTransform();
	//
	////  world space to light camera projection space
	//Matrix4D world2proj = pCam->WorldToProjectionSpace();

	//texTM = proj2tex;
	//texTM.mulLeft( world2proj );
	//texTM.mulLeft( cam2world );

	//m_pTextureMatrix->SetTransform( texTM );
	//IRS->SetTextureFactor( m_BlobColor );

	//m_pTextureMatrix->Render();
	//m_pBlobTexture->Render();
	//Texture::Freeze();
	//m_pDecalZBias->Render();
	////m_pBlobShadowShader->Render();
	//Texture::Unfreeze();
} // BlobShadowMapper::Render

void BlobShadowMapper::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "BlobShadowMapper", this );
	pm.f( "BlobWidth", m_BlobWidth );
	pm.f( "BlobHeight", m_BlobHeight );
	pm.f( "ModelPivotHeight", m_ModelPivotHeight );
	pm.f( "BlobColor", m_BlobColor, "color" );
	pm.f( "Pivot", m_Pivot, "direction" );
}

void BlobShadowMapper::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void BlobShadowMapper::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

void BlobShadowMapper::Create()
{
	//ShadowMapper::Create();
	//m_pBlobTexture		= AddChild<Texture>( "blob_round.tga" );
	//m_pLightCamera		= AddChild<OrthoCamera>( "LightCamera" );
	//m_pDecalZBias		= AddChild<ZBias>( "DecalZBias" );
	//m_pDecalZBias->SetBias( 0.00005f );
	//m_pBlobShadowShader	= m_pDecalZBias->AddChild<DeviceStateSet>( "blobShadow" );
	//m_pBlobShadowShader->AddInput( m_pShadowReceiver );

	//m_pTextureMatrix	= AddChild<TextureMatrix>( "TexCoorTM" );

	//m_pLightCamera->SetTransform( m_pLightSource->GetTransform() );

} // BlobShadowMapper::Create			

/*****************************************************************************/
/*	DBlobShadowMapper implementation
/*****************************************************************************/
DBlobShadowMapper::DBlobShadowMapper()
{
	m_pQuadMesh		= NULL;
}

void DBlobShadowMapper::Create()
{
	BlobShadowMapper::Create();
	m_pQuadMesh = AddChild<Geometry>( "QuadMesh" );
	m_pQuadMesh->Create( 4, 0, vf2Tex );
	m_pQuadMesh->GetPrimitive().setIsQuadList( true );

	if (m_pBlobShadowShader)
	{
		m_pBlobShadowShader->RemoveChildren();
		m_pBlobShadowShader->AddInput( m_pQuadMesh );
	}
} // DBlobShadowMapper::Create

void DBlobShadowMapper::Render()
{
	Parent::Render();
} // DBlobShadowMapper::Render

void DBlobShadowMapper::SetBlobQuad( const Vector3D& a,
									 const Vector3D& b,
									 const Vector3D& c,
									 const Vector3D& d )
{
	m_pQuadMesh->GetPrimitive().setNVert( 0 );
	m_pQuadMesh->GetPrimitive().setNPri( 0 );
	m_pQuadMesh->AddQuad( a, b, c, d, &m_BlobUV );
} // DBlobShadowMapper::SetBlobQuad

/*****************************************************************************/
/*	ProjectiveShadowMapper implementation
/*****************************************************************************/
ProjectiveShadowMapper::ProjectiveShadowMapper()
{
}

void ProjectiveShadowMapper::Render()
{
	m_pLightSource = (Light*)GetInput( 0 );
	if (!m_pLightSource->IsA<Light>()) { m_pLightSource = NULL; return; }

	int nObj = m_Casters.size();
	for (int i = 0; i < nObj; i++)
	{
		const ShadowCaster& inst = m_Casters[i];
		IMM->Render( inst.m_ModelID, &inst.m_TM );
	}

	if (m_pLightSource)
	{
		for (int i = 0; i < nObj; i++)
		{
			const ShadowCaster& inst = m_Casters[i];
			DrawAABB( inst.m_AABB, 0, ColorValue::Red );
			Frustum frustum = m_pLightSource->GetFrustum( inst.m_AABB );
			DrawFrustum( frustum, 0, ColorValue::Yellow, true );
		}
		IRS->ResetWorldMatrix();
		rsFlushLines3D();
	}

	m_Casters.clear();
} // ProjectiveShadowMapper::Render

void ProjectiveShadowMapper::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ProjectiveShadowMapper", this );
}

void ProjectiveShadowMapper::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void ProjectiveShadowMapper::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

/*****************************************************************************/
/*	ShadowVolumeMapper implementation
/*****************************************************************************/
ShadowVolumeMapper::ShadowVolumeMapper()
{

}

void ShadowVolumeMapper::Render()
{

}

void ShadowVolumeMapper::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ShadowVolumeMapper", this );
}

void ShadowVolumeMapper::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void ShadowVolumeMapper::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}
/*****************************************************************************/
/*	IDShadowMapper implementation
/*****************************************************************************/
IDShadowMapper::IDShadowMapper()
{

}

void IDShadowMapper::Render()
{

}

void IDShadowMapper::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "IDShadowMapper", this );
}

void IDShadowMapper::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void IDShadowMapper::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

/*****************************************************************************/
/*	ShadowBlob implementation
/*****************************************************************************/
bool ShadowBlob::s_bFrozen = false;
ShadowBlob::ShadowBlob()
{
	m_Height		= 100.0f;
	m_Width			= 100.0f;
	m_Color			= 0xFFFFFFFF;
	m_NSegments 	= 2;

	m_ShiftCenterX 	= 0;
	m_ShiftCenterY 	= 0;
} // ShadowBlob::ShadowBlob	

void ShadowBlob::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Height << m_Width << m_Color << m_NSegments << m_ShiftCenterX << m_ShiftCenterY;
}

void ShadowBlob::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Height >> m_Width >> m_Color >> m_NSegments >> m_ShiftCenterX >> m_ShiftCenterY;
}

void ShadowBlob::Render()
{
    if (IShadowMgr->GetShadowQuality() != sqBlobs) return;
	if (s_bFrozen) return;
	IRS->SetTextureFactor	( m_Color );
	Node::Render			();
	RenderMorphedGeometry	();
} // ShadowBlob::Render

void ShadowBlob::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ShadowBlob", this );
	pm.p( "BlobWidth",		GetBlobWidth,	SetBlobWidth );
	pm.p( "BlobHeight",		GetBlobHeight,	SetBlobHeight );
	pm.f( "BlobColor",		m_Color, "color"	 );
	pm.p( "ShiftCenterX",	GetShiftX, SetShiftX );
	pm.p( "ShiftCenterY",	GetShiftY, SetShiftY );
	pm.p( "NumSegments",	GetNSegments, SetNSegments );
} // ShadowBlob::Expose

void ShadowBlob::OnProcessGeometry()
{
	const Matrix4D& tm = TransformNode::TMStackTop();
	Vertex2t* sV = (Vertex2t*)m_OriginalMesh.getVertexData();
	Vertex2t* dV = (Vertex2t*)GetPrimitive().getVertexData();
	int nV = GetPrimitive().getNVert();

	if (ITerra)
	{
		for (int i = 0; i < nV; i++)
		{
			Vector3D v = sV[i];
			tm.transformPt( v );
			v.z = ITerra->GetH( v.x, v.y );
			dV[i].diffuse = m_Color;
			dV[i] = v;
		}
	}
	else
	{
		for (int i = 0; i < nV; i++)
		{
			Vector3D v = sV[i];
			tm.transformPt( v );
			v.z = 0.0f;
			dV[i].diffuse = m_Color;
			dV[i] = v;
		}
	}
} // ShadowBlob::OnProcessGeometry

void ShadowBlob::OnChangeStructure()
{
	Rct ext( -m_Width *0.5f + m_ShiftCenterX, -m_Height*0.5f + m_ShiftCenterY, m_Width, m_Height );
	CreatePatchGrid<Vertex2t>( GetPrimitive(), ext, m_NSegments, m_NSegments );
	ReplicateMesh();
} // ShadowBlob::OnChangeStructure

/*****************************************************************************/
/*	RubberBlob implementation
/*****************************************************************************/
RubberBlob::RubberBlob()
{
	SetNSegments( 4 );
	m_Damping = 1.0f;

	Vector3D ldir(	-sin( DegToRad( 30 ) ), 
					-cos( DegToRad( 26 ) ), 
					-sin( DegToRad( 90 - 26 ) ) );
	ldir.normalize();

	Vector3D ax = Vector3D::oX;
	Vector3D ay = Vector3D::oY;
	ldir.orthonormalize( ax, ay );
	
	m_LightTM.toBasis( ax, ay, ldir, Vector3D::null );
	m_bDrawWeights = false;
}		

void RubberBlob::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_BoneOffset << m_Damping;
}

void RubberBlob::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_BoneOffset >> m_Damping;
}

void RubberBlob::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "RubberBlob", this );
	pm.p( "Damping", GetDamping, SetDamping );
	pm.f( "DrawWeights", m_bDrawWeights );
}

void RubberBlob::OnProcessGeometry()
{
	const Matrix4D& tm = TransformNode::TMStackTop();
	BaseMesh& sbm = GetPrimitive();
	if (m_OriginalMesh.getNVert() != sbm.getNVert()) sbm.copy( m_OriginalMesh );

	VertexNMP4* sv = (VertexNMP4*)m_OriginalMesh.getVertexData();
	VertexNMP4* dv = (VertexNMP4*)sbm.getVertexData();

	const int c_MaxRubberBones = 16;
	static Matrix4D bmatr[c_MaxRubberBones];

	int curBone = 0;
	for (int i = 0; i < GetNChildren(); i++)
	{
		TransformNode* pNode = (TransformNode*)GetChild( i );
		if (!pNode->IsA<TransformNode>()) continue;
		bmatr[i] = GetWorldTM( pNode );
		if (m_BoneOffset.size() > curBone) bmatr[i].mulLeft( m_BoneOffset[curBone++] );
		//bmatr[i] *= m_LightTM;
	}

	int nV = sbm.getNVert();
	Matrix4D boneM = Matrix4D::identity;
	for (int i = 0; i < nV; i++)
	{
		Vector3D pos( sv[i].x, sv[i].y, sv[i].z );
		boneM.Blend4(   
                        bmatr[sv[i].GetBlendI( 0 )], sv[i].GetBlendW( 0 ),
			            bmatr[sv[i].GetBlendI( 1 )], sv[i].GetBlendW( 1 ),
			            bmatr[sv[i].GetBlendI( 2 )], sv[i].GetBlendW( 2 ),
			            bmatr[sv[i].GetBlendI( 3 )], sv[i].GetBlendW( 3 ) );
		boneM.transformPt	( pos );
		pos.x = pos.x * (1.0f - m_Damping) + m_Damping * sv[i].x;
		pos.y = pos.y * (1.0f - m_Damping) + m_Damping * sv[i].y;
		tm.transformPt		( pos );
		dv[i].x = pos.x;
		dv[i].y = pos.y;
		dv[i].z = 0.0f; // ( pos.x, pos.y );
	}

	if (m_bDrawWeights)
	{
		const float c_HandleLen = 80.0f;
		DWORD c_MColors[] = { 0, 0, 0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 
			0xFFFFFF00, 0xFFFF00FF, 0xFFFFFFFF, 0xFF00FFFF };

		for (int i = 0; i < nV; i++)
		{
			Vector3D pos( dv[i].x, dv[i].y, dv[i].z );

			int idx0 = sv[i].GetBlendI( 0 );
			Vector3D d0( GetWorldTM( GetChild( idx0 ) ).getTranslation() );
			d0 -= pos;
			d0.normalize();
			d0 *= sv[i].weight[0] * c_HandleLen;
			d0 += pos;
			rsLine( pos, d0, c_MColors[idx0], c_MColors[idx0] );

			int idx1 = sv[i].GetBlendI( 1);
			Vector3D d1( GetWorldTM( GetChild( idx1 ) ).getTranslation() );
			d1 -= pos;
			d1.normalize();
			d1 *= sv[i].weight[1] * c_HandleLen;
			d1 += pos;
			rsLine( pos, d1, c_MColors[idx1], c_MColors[idx1] );

			int idx2 = sv[i].GetBlendI( 2 );
			Vector3D d2( GetWorldTM( GetChild( idx2 ) ).getTranslation() );
			d2 -= pos;
			d2.normalize();
			d2 *= sv[i].weight[2] * c_HandleLen;
			d2 += pos;
			rsLine( pos, d2, c_MColors[idx2], c_MColors[idx2] );

			int idx3 = sv[i].GetBlendI( 3 );
			Vector3D d3( GetWorldTM( GetChild( idx3 ) ).getTranslation() );
			d3 -= pos;
			d3.normalize();
			d3 *= sv[i].GetBlendW( 3 ) * c_HandleLen;
			d3 += pos;
			rsLine( pos, d3, c_MColors[idx3], c_MColors[idx3] );
		}

		for (int i = 0; i < GetNChildren(); i++)
		{
			TransformNode* pNode = (TransformNode*)GetChild( i );
			if (!pNode->IsA<TransformNode>()) continue;
			DrawAABB( AABoundBox( GetWorldTM( pNode ).getTranslation(), c_HandleLen*0.25f ), 
				0, c_MColors[i] );
		}
		rsFlushLines3D();
	}
} // RubberBlob::OnProcessGeometry

void RubberBlob::SetDamping( float damping )
{
	clamp( damping, 0.0f, 1.0f );
	m_Damping = damping;
}

struct RubberVertex 
{
	int			bidx[4];
	float		w[4];

	RubberVertex() { bidx[0] = bidx[1] = bidx[2] = bidx[3] = -1; 
						w[0] = w[1] = w[2] = w[3] = 0.0f; }
	void	Normalize()
	{
		float max0 = tmax( w[1], w[2], w[3] );
		float max1 = tmax( w[0], w[2], w[3] );
		float max2 = tmax( w[0], w[1], w[3] );
		float max3 = tmax( w[0], w[1], w[2] );

		w[0] *= (1.0f - max0);
		w[1] *= (1.0f - max1);
		w[2] *= (1.0f - max2);
		w[3] *= (1.0f - max3);

		float sum = w[0] + w[1] + w[2] + w[3];
		w[0] /= sum;
		w[1] /= sum;
		w[2] /= sum;
		w[3] /= sum;
	}

	void AddWeight( int idx, float weight )
	{
		if (bidx[0] == idx) { w[0] += weight; return; }
		if (bidx[1] == idx) { w[1] += weight; return; }
		if (bidx[2] == idx) { w[2] += weight; return; }
		if (bidx[3] == idx) { w[3] += weight; return; }

		int pos = argmin_idx( w[0], w[1], w[2], w[3] );
		w[pos] = weight;
		bidx[pos] = idx;
	}
}; // struct RubberVertex

void RubberBlob::OnChangeStructure()
{
	Rct ext( -m_Width*0.5f, -m_Height*0.5f, m_Width, m_Height );
	CreatePatchGrid<VertexNMP4>( GetPrimitive(), ext, m_NSegments, m_NSegments );

	VertexNMP4* v = (VertexNMP4*)GetPrimitive().getVertexData();
	int nV = GetPrimitive().getNVert();
	
	RubberVertex* rv = new RubberVertex[nV];

	//  find max distance from input bones to morph targets 
	float maxd = 0.0f;
	for (int i = 0; i < GetNChildren(); i++)
	{
		TransformNode* pNode = (TransformNode*)GetChild( i );
		if (!pNode->IsA<TransformNode>()) continue;
		Matrix4D tm = GetWorldTM( pNode );

		for (int j = 0; j < GetNChildren(); j++)
		{
			TransformNode* pNode1 = (TransformNode*)GetChild( j );
			if (!pNode1->IsA<TransformNode>()) continue;
			Matrix4D tm1 = GetWorldTM( pNode1 );
			float dist = tm1.getTranslation().distance2( tm.getTranslation() );
			if (dist > maxd) maxd = dist;
		}
	}
	
	m_BoneOffset.clear();

	//  assign weights to the morph targets
	for (int i = 0; i < GetNChildren(); i++)
	{
		TransformNode* pNode = (TransformNode*)GetChild( i );
		if (!pNode->IsA<TransformNode>()) continue;
		Matrix4D tm = GetWorldTM( pNode );
		Matrix4D bOffs;
		bOffs.inverse( tm );
		m_BoneOffset.push_back( bOffs );

		for (int j = 0; j < nV; j++)
		{
			Vector3D tr = tm.getTranslation();
			tr -= Vector3D( v[j].x, v[j].y, v[j].z );
			tr.z = 0.0f;
			float w = 1.0f - tr.norm2()/maxd;
			if (w > 0.0f) rv[j].AddWeight( i, w );
		}
	}

	for (int i = 0; i < nV; i++)
	{
		rv[i].Normalize();
		v[i].weight[0] = rv[i].w[0];
		v[i].weight[1] = rv[i].w[1];
		v[i].weight[2] = rv[i].w[2];
		
		v[i].SetBlendI( 0, rv[i].bidx[0] );
		v[i].SetBlendI( 1, rv[i].bidx[1] );
		v[i].SetBlendI( 2, rv[i].bidx[2] );
		v[i].SetBlendI( 3, rv[i].bidx[3] );
	}

	delete []rv;
	ReplicateMesh();
} // RubberBlob::OnChangeStructure

/*****************************************************************************/
/*	ProjectiveBlob implementation
/*****************************************************************************/
ProjectiveBlob::ProjectiveBlob()
{
	DirectionalLight* pLight = sg::Root::instance()->FindChild<DirectionalLight>( "GameLight" );
	if (pLight)
	{
		m_LightDir = pLight->GetDir();
	}
	else
	{
		m_LightDir.set(	-sin( DegToRad( 30 ) ), -cos( DegToRad( 26 ) ), -sin( DegToRad( 90 - 26 ) ) );
		m_LightDir.normalize();
	}

	Vector3D up( Vector3D::oZ );
	Vector3D right;
	Vector3D dir( m_LightDir );
	dir.reverse();
	dir.z = 0.0f;
	right.cross( dir, up );
	dir.orthonormalize( up, right );

	m_PatchRotTM.SetRows( right, dir, up );

	m_pLightCamera	= NULL;
	m_pTarget		= NULL;
	m_pTexture		= NULL;
	m_pReceiverDSS	= NULL;
	m_pShadowMapDSS = NULL;

	m_ShadowMapSide = 128;
}

const float c_ZNear = -1000.0f;
const float c_ZFar = 1000.0f;
void ProjectiveBlob::Render()
{
	if (s_bFrozen) return;

	if (!m_pTarget)			m_pTarget		= FindChild<RenderTarget>	( "ShadowTarget"		);
	if (!m_pShadowMapDSS)	m_pShadowMapDSS	= m_pTarget->FindChild<DeviceStateSet>	( "ShadowMap"			);
	if (!m_pTexture)		m_pTexture		= FindChild<Texture>		( "ShadowMap"			);
	if (!m_pLightCamera)	m_pLightCamera	= FindChild<OrthoCamera>	( "LightCamera"			);
	if (!m_pReceiverDSS)	m_pReceiverDSS	= FindChild<DeviceStateSet>	( "ShadowMapReceiver"	);

	if (!m_pLightCamera || !m_pTarget || !m_pTexture) return;
	m_pTarget->SetTarget( m_pTexture );
	
	Matrix4D objTM = TransformNode::TMStackTop();

	m_pLightCamera->SetPos( objTM.getTranslation() );
	m_pLightCamera->SetDirUp( m_LightDir, Vector3D::oZ );
	float objScale = objTM.GetXRow().norm();
	m_pLightCamera->SetOrthoW( objScale * m_Width, objScale * m_Width / m_Height, 
								c_ZNear, c_ZFar );

	BaseCamera* pCurCamera = BaseCamera::GetActiveCamera();

	Matrix4D vTM;
	vTM.translation( m_ShiftCenterX, m_ShiftCenterY, 0.0f );
	vTM.mulLeft( m_pLightCamera->GetViewM() );
	IRS->SetViewMatrix( vTM );
	IRS->SetProjectionMatrix( m_pLightCamera->GetProjM() );

	ShadowBlob::Freeze();
	m_pTarget->Render();
	ShadowBlob::Unfreeze();

	if (pCurCamera) pCurCamera->Render();
	
	m_pTexture->Render();
	m_pReceiverDSS->Render();

	//  find where light ray coming from object center intersects receiver geometry
	if (fabs( m_LightDir.z ) < c_SmallEpsilon) return;
	float t = - objTM.e32 / m_LightDir.z;

	Vector3D cPatch( objTM.e30 + m_LightDir.x * t, objTM.e31 + m_LightDir.y * t, 0 ); 
	Matrix4D wtm( Vector3D( objScale, objScale, objScale ), m_PatchRotTM, cPatch );
	TransformNode::ResetTMStack( &wtm );
	RenderMorphedGeometry();
	TransformNode::ResetTMStack();
} // ProjectiveBlob::Render

void ProjectiveBlob::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ProjectiveBlob", this );
	pm.f( "ShadowMapSide", m_ShadowMapSide );
}

void ProjectiveBlob::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void ProjectiveBlob::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

void ProjectiveBlob::OnChangeStructure()
{
	ShadowBlob::OnChangeStructure();

	m_pTarget = GetChild<RenderTarget>( "ShadowTarget" );
	m_pTarget->SetClearColor( 0xFF000000 );
	m_pTarget->EnableClear();

	m_pShadowMapDSS	= m_pTarget->GetChild<DeviceStateSet>( "ShadowMap" );
	m_pReceiverDSS = GetChild<DeviceStateSet>( "ShadowMapReceiver" );

	if (!m_pTexture) m_pTexture = FindChild<Texture>( "ShadowMap" );
	if (!m_pTexture)
	{
		m_pTexture = AddChild<Texture>( "ShadowMap" );
		m_pTexture->SetUsage		( tuRenderTarget		 );
		m_pTexture->SetMemoryPool	( mpVRAM				 );
		m_pTexture->SetWidth		( m_ShadowMapSide		 );
		m_pTexture->SetHeight		( m_ShadowMapSide		 );
		m_pTexture->SetNMips		( 1						 );
		m_pTexture->SetColorFormat	( cfRGB565				 );
		m_pTexture->CreateTexture	();
		m_pTarget->SetTarget		( m_pTexture );	
	}

	m_pLightCamera = GetChild<OrthoCamera>( "LightCamera" );

	ReplicateMesh();
} // ProjectiveBlob::OnChangeStructure

END_NAMESPACE(sg)
