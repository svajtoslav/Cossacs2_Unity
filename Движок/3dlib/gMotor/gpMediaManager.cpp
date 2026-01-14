/*****************************************************************************/
/*	File:	gpModelManager.cpp
/*	Desc:	Realization of the model manager
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-15-2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "kHash.hpp"
#include "IMediaManager.h"
#include "sgSpriteCarcass.h"
#include "sgGeometry.h"
#include "sgDummy.h"
#include "sgHardwareCaps.h"
#include "sgShadow.h"
#include "sgParticleSystem.h"
#include "kTemplates.hpp"
#include "uiControl.h"
#include "sgTerrain.h"
#include "IEffectManager.h"
#include "sgEffect.h"

using namespace sg;
/*****************************************************************************/
/*	Class:	MediaManagerImpl
/*	Desc:	Implementaiton of the model manager interface
/*****************************************************************************/
class MediaManagerImpl :	public Group, 
							public IMediaManager, 
							public PSingleton<MediaManagerImpl>
{
public:
						MediaManagerImpl();
	virtual DWORD		GetModelID		( const char* fname );
	virtual const char* GetModelFileName( DWORD modelID );
	virtual void		SetVisible		( DWORD nodeID, bool bVisible = true );

	virtual DWORD		GetNodeID		( DWORD modelID, const char* nodeName );
	virtual DWORD		GetNodeID		( const char* name );
	virtual void		ShowNode		( DWORD id, bool bShow = true );
	virtual Matrix4D	GetNodeTransform( DWORD nodeID, bool bLocalSpace = false );
	virtual void		SetNodeTransform( DWORD nodeID, const Matrix4D& m, 
											bool bLocalSpace = false );
	virtual ICamera*	GetCamera		( DWORD nodeID );
	virtual ILight*		GetLight		( DWORD nodeID );
	virtual IGeometry*	GetGeometry		( DWORD nodeID );

	virtual int			GetNumGeometries( DWORD modelID );
	virtual IGeometry*	GetGeometry		( DWORD modelID, int idx, Matrix4D& tm );

	virtual int			GetNumSubNodes	( DWORD modelID );
	virtual const char*	GetSubNodeName	( DWORD modelID, int idx );

	virtual DWORD		CloneNode		( DWORD nodeID );
	virtual void		DeleteNode		( DWORD nodeID );

	virtual void		OnFrame			();

	virtual IParticleSystem*	GetParticleSystem( DWORD nodeID );

	virtual void		BeginThumbnail	( const Rct& rect, const Vector3D* viewDir = NULL, 
											DWORD bgColor = 0, bool mouseControlled = true );
	virtual void		EndThumbnail	();

	bool				MakeParent		 	( DWORD parentID, DWORD childID, bool bParent );
	virtual void		Render			 	( DWORD id, const Matrix4D* pTransform = NULL );
	virtual void		Render				();
	virtual void		Render				( DWORD id, int mX, int mY, float scale = 1.0f );


	virtual void		RenderShadow	 	( DWORD id, const Matrix4D* pTransform = NULL );
	virtual bool		GetModelGP			( DWORD id, int& gpID, int& frameID, Vector3D& center );
	virtual AABoundBox	GetBoundBox			( DWORD id );

	virtual void		Animate				( DWORD modelID, DWORD animID, float animTime );
	virtual void		Animate				( DWORD modelID, float blendFactor,
												 DWORD animID1, float animTime1,
												 DWORD animID2, float animTime2 );
	virtual float		GetAnimTime			( DWORD animID );
	virtual bool		GetHeight			( DWORD id, Vector3D& pt, const Matrix4D* pTransform = NULL );
	virtual bool		GetLock				( DWORD id, const Vector3D& pt, const Matrix4D* pTransform = NULL );
	virtual bool		SwitchTo			( DWORD id, int index );
	virtual void		ReloadModels		();
	virtual void		SetCamera			( const Vector3D& lookAt, float viewVolWidth ){}
	virtual void		Init				();
	virtual void		SetViewPort			( float x, float y, float w, float h );

	virtual bool		IsBoxVisible		( const Vector3D& minv, const Vector3D& maxv );
	virtual bool		IsSphereVisible		( const Vector3D& center, float radius );
	virtual bool		IsPointVisible		( const Vector3D& pt );

	virtual void		RegisterVisible		( DWORD objID, DWORD modelID, const Matrix4D& tm );
	virtual void		RegisterVisible		( DWORD objID, const Vector3D& minv, const Vector3D& maxv );
	virtual void		RegisterVisible		( DWORD objID, const Vector3D& center, float radius );
	virtual void		RegisterVisible		( DWORD objID, DWORD gpID, int frameID, const Matrix4D& tm );
	virtual void		ClearVisibleCache	();
	virtual DWORD		PickVisible			( float scrX, float scrY, DWORD prevID = 0xFFFFFFFF );
	virtual void		ScanHeightmap		( DWORD nodeID, const Matrix4D& tm, float stepx, float stepy, SetHeightCallback callb );
	virtual void		ScanHeightmap		( DWORD nodeID, const Matrix4D& tm, float stepx, float stepy, VisitHeightCallback callb );
	virtual int			GetSegmentList		( DWORD nodeID, WSegment* segArr, int maxSeg, const Matrix4D* tm = NULL );
	virtual void		FreezeShaders		( bool freeze = true );
    virtual bool        Intersects          ( float mX, float mY, DWORD modelID, const Matrix4D& tm, float& minDist );


	NODE(MediaManagerImpl,Group,MMGI);
private:
	Thumbnail*						m_pThumbnail;		
	bool							m_bThumbnailMode;
}; // class MediaManagerImpl

REGNODE( MediaManagerImpl );

//  global interface pointer
IMediaManager*		IMM		= MediaManagerImpl::instance();
IParticleManager*	IPMgr   = ParticleManager::instance();
IReflectionMap*		IRMap	= NULL;

bool  ConvertModel( Node* pModel );

/*****************************************************************************/
/*	MediaManagerImpl implementation
/*****************************************************************************/
MediaManagerImpl::MediaManagerImpl()
{
	SetName( "Model Manager" );
	AddRef();

	m_pThumbnail		= NULL;
	m_bThumbnailMode	= false;
} // MediaManagerImpl::MediaManagerImpl

DWORD	MediaManagerImpl::GetModelID( const char* fname )
{
	if (!fname || fname[0] == 0) return c_BadID;
	Node* pModel = NodePool::GetNodeByName( fname );
	if (!pModel)
	{
		FInStream is( fname );
		if (is.NoFile()) return c_BadID;
	
		pModel = Node::UnserializeSubtree( is );
        is.CloseFile();
		if (!pModel) return c_BadID;
        
        ////  convert to new format, if needed
        //if (ConvertModel( pModel ))
        //{
        //    FOutStream os( fname );
        //    if (!os.NoFile())
        //    {
        //        pModel->SerializeSubtree( os );
        //        os.CloseFile();
        //    }
        //}

		if (pModel->IsA<Animation>()) 
		{
			AnimationManager::instance()->AddAnimation( fname, pModel );
		}
		else
		{
			ModelManager::instance()->AddModel( fname, pModel );
		}

		return pModel->GetID();
	}
	Node* pChild = pModel->GetChild( 0 );
	if (pChild) return pChild->GetID();
	return pModel->GetID();
} // MediaManagerImpl::GetModelID

const char* MediaManagerImpl::GetModelFileName( DWORD modelID )
{
	Node* pModel = NodePool::GetNode( modelID );
	if (!pModel) return "";
	Node* pParent = pModel->GetParent();
	if (!pParent) return pModel->GetName();
	return pParent->GetName();
} // MediaManagerImpl::

void MediaManagerImpl::Render( DWORD id, int mX, int mY, float scale )
{
	Node* pModel = NodePool::GetNode( id );
	if (!pModel) return;

	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (!pCam) return;
	Ray3D ray;
	Matrix3D rot( Matrix3D::identity );
	
	pCam->GetPickRay( mX, mY, ray );
	Vector3D xPt;

	if (ITerra)
	{
		if (!ITerra->Pick( ray.getOrig(), ray.getDir(), xPt )) return;
	}
	else
	{
		if (!ray.IntersectPlane( Plane::xOy, xPt )) return;
	}

	Matrix4D tm( Vector3D( scale, scale, scale ), rot, xPt );
	TransformNode::ResetTMStack( &tm );
	pModel->Render();
} // MediaManagerImpl::Render


void MediaManagerImpl::Render( DWORD id, const Matrix4D* pTransform )
{
	Node* pModel = NodePool::GetNode( id );
	if (!pModel) return;

	if (pTransform) TransformNode::Push( *pTransform );

	if (pModel->IsA<CarcassBuilding>())
	{
		((CarcassBuilding*)pModel)->RenderMain();
	}
	else if (pModel->IsA<PEmitter>())
	{
		int inst = IEffMgr->InstanceEffect( pModel->GetID() );
		if (pTransform) IEffMgr->UpdateInstance( inst, *pTransform );
        else (IEffMgr->UpdateInstance( inst ));
	}
	else
	{
		if (m_bThumbnailMode) 
		{
			if (pModel->IsA<Animation>())
			{
				Group* pRealMdl = NULL;
				Iterator it( ModelManager::instance(), Group::FnFilter );
				while (it)
				{
					Group* pGroup = (Group*)(Node*)it;
					if (!strncmp( pGroup->GetName(), pModel->GetName(), 5 ))
					{
						pRealMdl = pGroup;
						break;
					}
					++it;
				}
				if (pRealMdl)
				{
					Animation* pAnimation = (Animation*)pModel;
					pModel = pRealMdl;
					pAnimation->SetCurrentTime( fmod( GetTickCount(), 
												pAnimation->GetAnimationTime() ) );
					pAnimation->BindNode( pModel );
					pAnimation->Render();
				}
			}
			m_pThumbnail->AddItem( pModel );
		}
		else
		{
		    pModel->Render();
            /*AABoundBox aabb = CalculateAABB( pModel );
            if (pTransform) aabb.Transform( *pTransform );
            IRS->ResetWorldMatrix();
            DrawAABB( aabb, 0, 0xFFFF0000 );
            rsFlushLines3D();*/
		}
	}
    
    if (pTransform) TransformNode::Pop();
} // MediaManagerImpl::Render

bool MediaManagerImpl::GetModelGP( DWORD id, int& gpID, int& frameID, Vector3D& center )
{
	Node* pModel = NodePool::GetNode( id );
	if (!pModel) return false;
	if (!pModel->IsA<CarcassBuilding>()) return false; 
	CarcassBuilding* pBuilding = (CarcassBuilding*)pModel;
	gpID	= pBuilding->GetMainGP();
	frameID = pBuilding->GetMainFrame();
	center  = pBuilding->GetPivot();
	return true;
} // MediaManagerImpl::GetModelGP

const float c_ShadowZBias = 64.0f;
void MediaManagerImpl::RenderShadow( DWORD id, const Matrix4D* pTransform )
{
	Node* pModel = NodePool::GetNode( id );
	if (!pModel) return;
	
	if (pTransform)
	{
		TransformNode::ResetTMStack( pTransform );
	}

	if (pModel->IsA<CarcassBuilding>())
	{
		((CarcassBuilding*)pModel)->RenderShadow();
	}
	else
	{
		for (int i = 0; i < pModel->GetNChildren(); i++)
		{
			Node* pChild = pModel->GetChild( i );
			if (pChild->IsA<ShadowBlob>())
			{
				pChild->Render();
				break;
			}
		}
	}

	if (pTransform)
	{
		TransformNode::ResetTMStack();
		IRS->ResetWorldMatrix();
	}

} // MediaManagerImpl::RenderShadow

//-------------------------------------------------------------------------------
//  Func:  MediaManagerImpl::GetHeight
//  Desc:  Returns height at the given (pt.x, pt.y) point (result is placed in pt.z)
//  Ret:   true if there is point of mesh at (pt.x, pt.y)
//  Rmrk:  Do not call it too often, if you don't want you program to 
//			execute forever
//-------------------------------------------------------------------------------
bool MediaManagerImpl::GetHeight( DWORD id, Vector3D& pt, const Matrix4D* pTransform )
{
	Node* pModel = NodePool::GetNode( id );
	if (!pModel) return false;
		
	pt.z = 0.0f;
	Vector3D dir;
	dir.set( 0.0f, -0.5f, c_CosPId6 );

	static const float c_Away = 10000.0f;
	Vector3D pos( pt.x, pt.y, 0.0f );

	pos.addWeighted( dir, c_Away );
	dir.reverse();

	Line3D ray( pos, dir );
	int			minIdx		= -1;
	Geometry*	pMinGeom	= pModel->FindChild<Geometry>( "Navimesh" );
	if (!pMinGeom) pMinGeom = pModel->FindChild<Geometry>( "Navimesh_geom" );
	float		minDist		= 0.0f;

	if (!pMinGeom) return false;
	{
		Ray3D tray( ray );
		Matrix4D lToW( *pTransform );
		lToW.mulLeft( GetWorldTM( pMinGeom ) );
		Matrix4D wToL; wToL.inverse( lToW );
		tray.Transform( wToL );
		int triIdx;
		float dist = pMinGeom->GetPrimitive().PickPoly( tray, triIdx );
		if (dist < 0.0f || triIdx < 0) return false;
		Vector4D hit( tray.getDir() );
		hit *= dist;
		hit.w = 0.0f;
		hit *= lToW;
		minDist	= hit.norm();
	}
	if (pMinGeom)
	{
		pt.z = c_Away - minDist;
		return true;
	}

	return false;
} // MediaManagerImpl::GetHeight

bool MediaManagerImpl::GetLock( DWORD id, const Vector3D& pt, const Matrix4D* pTransform )
{
	Node* pModel = NodePool::GetNode( id );
	if (!pModel) return false;

	Vector3D ptLoc( pt );
	ptLoc.z = 0.0f;

	//  transform query point to the local model space
	if (pTransform)
	{
		Matrix4D invT;
		invT.inverse( *pTransform );
		ptLoc *= invT;
	}

	const float c_BigFloat = 10000.0f;
	Vector3D dir( 0.0f, 0.5f, -c_CosPId6 );
	Vector3D pos( ptLoc.x, ptLoc.y, 0.0f );
	dir.reverse();

	Line3D ray( pos, dir );

	int				minIdx		= -1;
	Geometry*	pMinGeom	= pModel->FindChild<Geometry>( "Lockmesh" );
	float			minDist		= c_BigFloat;

	if (!pMinGeom) return false;

	minDist = pMinGeom->GetPrimitive().PickPoly( ray, minIdx );
	if (minIdx == -1) return false;
	return true;
} // MediaManagerImpl::GetLock

bool MediaManagerImpl::SwitchTo( DWORD id, int index )
{
	Switch* pSwitch = NodePool::GetNode<Switch>( id );
	if (!pSwitch) return false;
	pSwitch->SwitchTo( index );
	return true;
} // MediaManagerImpl::SetCursor

void MediaManagerImpl::Animate( DWORD modelID, DWORD animID, float animTime )
{
	Node* pModel = NodePool::GetNode( modelID );
	if (!pModel) return;

	Animation*	pAnim = NodePool::GetNode<Animation>( animID );
	if (!pAnim) 
	{
		Iterator it( this, TransformNode::FnFilter );
		while (it) 
		{
			TransformNode* pNode = (TransformNode*)(Node*)it;
			pNode->SetToInitial();
			++it;
		}
		return;
	}

	if (pModel) pAnim->BindNode( pModel );
	pAnim->SetCurrentTime( animTime + pAnim->GetStartTime() );
	Animation::PushTime( animTime + pAnim->GetStartTime() );
	pAnim->Render();
	Animation::PopTime();
} // MediaManagerImpl::Animate

void MediaManagerImpl::Animate( DWORD modelID, float blendFactor,
								DWORD animID1, float animTime1,
								DWORD animID2, float animTime2 )
{
	Node* pModel = NodePool::GetNode( modelID );
	if (!pModel) return;

	Animation* pAnim1 = NodePool::GetNode<Animation>( animID1 );
	Animation* pAnim2 = NodePool::GetNode<Animation>( animID2 );

	if (!pAnim1 || !pAnim2) return; 
	
	pAnim1->BindNode( pModel );
	pAnim2->BindNode( pModel );

	Iterator it1( pAnim1, PRSAnimation::FnFilter );
	Iterator it2( pAnim2, PRSAnimation::FnFilter );

	while (it1)
	{
		PRSAnimation* pAnm1 = (PRSAnimation*)(Node*)it1;
		PRSAnimation* pAnm2 = (PRSAnimation*)(Node*)it2;
		TransformNode* pTM = (TransformNode*)pAnm1->GetInput( 0 );
		if (pTM && pTM->IsA<TransformNode>())
		{
			assert( !strcmp( pAnm1->GetName(), pAnm2->GetName() ) && 
					!strcmp( pAnm1->GetName(), pTM->GetName() ) );
			Matrix4D tm = PRSAnimation::GetTransform( pAnm1, animTime1, pAnm2, animTime2, blendFactor );
			pTM->SetTransform( tm );
		}
		++it1;
		++it2;
	}

} // MediaManagerImpl::Animate

float MediaManagerImpl::GetAnimTime( DWORD animID )
{
	Animation* pAnim = NodePool::GetNode<Animation>( animID );
	if (!pAnim) return 0.0f;
	return pAnim->GetAnimationTime();
} // MediaManagerImpl::GetAnimTime

DWORD MediaManagerImpl::GetNodeID( DWORD modelID, const char* nodeName )
{
	Node* pModel = NodePool::GetNode( modelID );
	if (!pModel) return c_BadID;
	Node* pChild = pModel->FindChildByName( nodeName );
	if (!pChild) return c_BadID;
	return pChild->GetID();
} // MediaManagerImpl::GetNodeID

DWORD MediaManagerImpl::GetNodeID( const char* name )
{
	Node* pNode = NodePool::GetNodeByName( name );
	if (!pNode) return c_BadID;
	return pNode->GetID();
}

void MediaManagerImpl::ShowNode( DWORD id, bool bShow )
{
	Node* pNode = NodePool::GetNode( id );
	if (pNode) pNode->SetInvisible( !bShow );
} // MediaManagerImpl::ShowNode

Matrix4D MediaManagerImpl::GetNodeTransform( DWORD nodeID, bool bLocalSpace )
{
	if (nodeID == c_BadID) return Matrix4D::identity;
	TransformNode* pNode = NodePool::GetNode<TransformNode>( nodeID );
	if (!pNode) return Matrix4D::identity;
	if (bLocalSpace) return pNode->GetTransform(); 
		else return pNode->GetWorldTM();
} // MediaManagerImpl::GetNodeTransform

void MediaManagerImpl::SetNodeTransform( DWORD nodeID, const Matrix4D& m, 
											bool bLocalSpace )
{
	TransformNode* pNode = NodePool::GetNode<TransformNode>( nodeID );
	if (!pNode) return;
	if (bLocalSpace) pNode->SetTransform( m ); else pNode->SetWorldTM( m );
} // MediaManagerImpl::SetNodeTransform

void MediaManagerImpl::SetVisible( DWORD nodeID, bool bVisible )
{
	Node* pNode = NodePool::GetNode( nodeID );
	if (pNode)
	{
		pNode->SetInvisible( !bVisible );
	}
} // MediaManagerImpl::SetVisible

bool MediaManagerImpl::MakeParent( DWORD parentID, DWORD childID, bool bParent )
{
	Node* pParent = NodePool::GetNode( parentID );
	Node* pChild  = NodePool::GetNode( childID );
	
	if (!pParent || !pChild) return false;
	
	if (bParent)
	{
		pParent->RemoveChild( pChild );
		pParent->AddInput( pChild );
	}
	else
	{
		pParent->RemoveChild( pChild );
	}
	return true;
} // MediaManagerImpl::MakeParent

void MediaManagerImpl::ReloadModels()
{
	//  nothin here right now...
} // MediaManagerImpl::ReloadModels

ICamera*  MediaManagerImpl::GetCamera( DWORD nodeID )
{
	BaseCamera* pCamera = NodePool::GetNode<BaseCamera>( nodeID );
	return pCamera;
}

ILight*	MediaManagerImpl::GetLight( DWORD nodeID )
{
	Light* pLight = NodePool::GetNode<Light>( nodeID );
	return pLight;
}

IParticleSystem*  MediaManagerImpl::GetParticleSystem( DWORD nodeID )
{
	Node* pPS = NodePool::GetNode( nodeID );
	if (!pPS->IsA<ParticleSystem>())
	{
		pPS = pPS->FindChildFn<ParticleSystem>();
	}
	return (ParticleSystem*)pPS;
}

IGeometry*	MediaManagerImpl::GetGeometry( DWORD nodeID )
{
	Node* pG = NodePool::GetNode( nodeID );
	if (!pG->IsA<Geometry>())
	{
		pG = pG->FindChildFn<Geometry>();
	}
	return (Geometry*)pG;
} // MediaManagerImpl::GetGeometry

void MediaManagerImpl::Init()
{
	if (Root::instance()->GetNChildren() == 0) Root::instance()->CreateGuts();
}

void MediaManagerImpl::Render()
{
	Root::instance()->Render();
}

void MediaManagerImpl::BeginThumbnail( const Rct& rect, const Vector3D* viewDir, 
										DWORD bgColor, bool mouseControlled )
{
	if (!m_pThumbnail) m_pThumbnail = sg::Root::instance()->FindChild<Thumbnail>( "Thumbnail" );
	if (m_pThumbnail) 
	{
		Thumbnail::ControlMode  controlMode = mouseControlled ? 
									Thumbnail:: cmExhibition : Thumbnail::cmNone;
		if (viewDir) 
		{
			m_pThumbnail->SetViewDir	( *viewDir		  );
		}
		m_pThumbnail->SetRect			( rect			  );
		m_pThumbnail->SetControlMode	( controlMode	  );
		m_pThumbnail->ClearItems		();
		m_bThumbnailMode = true;
	}
} // MediaManagerImpl::BeginThumbnail

void MediaManagerImpl::EndThumbnail()
{
	if (m_pThumbnail) m_pThumbnail->Render();
    m_bThumbnailMode = false;
} // MediaManagerImpl::EndThumbnail

bool MediaManagerImpl::IsBoxVisible( const Vector3D& minv, const Vector3D& maxv ) 
{
	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (!pCam) return false;
	return pCam->GetFrustum().Overlap( AABoundBox( minv, maxv ) );
}

bool MediaManagerImpl::IsSphereVisible( const Vector3D& center, float radius ) 
{
	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (!pCam) return false;
	return pCam->GetFrustum().Overlap( Sphere( center, radius ) );
}

bool MediaManagerImpl::IsPointVisible( const Vector3D& pt ) 
{
	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (!pCam) return false;
	return pCam->GetFrustum().PtIn( pt );
}

void MediaManagerImpl::RegisterVisible( DWORD objID, DWORD modelID, const Matrix4D& tm )
{
}

void MediaManagerImpl::RegisterVisible( DWORD objID, const Vector3D& minv, const Vector3D& maxv )
{
}

void MediaManagerImpl::RegisterVisible( DWORD objID, const Vector3D& center, float radius )
{
}

void MediaManagerImpl::RegisterVisible( DWORD objID, DWORD gpID, int frameID, const Matrix4D& tm )
{
}

void MediaManagerImpl::ClearVisibleCache()
{
}

DWORD MediaManagerImpl::PickVisible( float scrX, float scrY, DWORD prevID )
{
	return 0xFFFFFFFF;
}

void MediaManagerImpl::ScanHeightmap( DWORD nodeID, const Matrix4D& tm, 
										float stepx, float stepy,
										SetHeightCallback callb )
{
	Node* pG = NodePool::GetNode( nodeID );
	if (!pG->IsA<Geometry>()) return;

	Vector3D dir = Vector3D::oZ;
	Primitive& pri = ((Geometry*)pG)->GetPrimitive();

	Matrix4D mdlTM = GetWorldTM( pG );
	mdlTM *= tm; 
	sg::ScanHeightmap( pri, mdlTM, stepx, stepy, callb );
} // MediaManagerImpl::ScanHeightmap

void MediaManagerImpl::ScanHeightmap( DWORD nodeID, const Matrix4D& tm, 
									  float stepx, float stepy,
									  VisitHeightCallback callb )
{
	Node* pG = NodePool::GetNode( nodeID );
	if (!pG->IsA<Geometry>()) return;

	Vector3D dir = Vector3D::oZ;
	Primitive& pri = ((Geometry*)pG)->GetPrimitive();

	Matrix4D mdlTM = GetWorldTM( pG );
	mdlTM *= tm; 
	sg::ScanHeightmap( pri, mdlTM, stepx, stepy, callb );
} // MediaManagerImpl::ScanHeightmap

void MediaManagerImpl::SetViewPort( float x, float y, float w, float h )
{
} // MediaManagerImpl::SetViewPort

int MediaManagerImpl::GetSegmentList( DWORD nodeID, WSegment* segArr, int maxSeg, const Matrix4D* tm )
{
	SegmentSystem* pS = (SegmentSystem*)NodePool::GetNode( nodeID );
	if (!pS->IsA<SegmentSystem>()) return 0;
	
	int cSeg = 0;
	Iterator it( pS, SegmentNode::FnFilter );
	while (it)
	{
		if (cSeg >= maxSeg) break;
		SegmentNode* pSeg = (SegmentNode*)(Node*)it;
		segArr[cSeg].beg	= pSeg->GetBeg();
		segArr[cSeg].end	= pSeg->GetEnd();
		segArr[cSeg].width	= pSeg->GetWidth();
		segArr[cSeg].normal = pSeg->GetNormal();
		cSeg++;
		++it;
	}
	
	if (tm)
	{
		for (int i = 0; i < cSeg; i++)
		{
			tm->transformPt( segArr[i].beg );
			tm->transformPt( segArr[i].end );
			tm->transformVec( segArr[i].normal );
			Vector3D s( 0, 0, segArr[i].width );
			tm->transformVec( s );
			segArr[i].width = s.norm();
		}
	}
	return cSeg;
} // MediaManagerImpl::GetSegmentList

void MediaManagerImpl::FreezeShaders( bool freeze )
{
	if (freeze)
	{
		DeviceStateSet::Freeze();
	}
	else
	{
		DeviceStateSet::Unfreeze();	
	}
} // MediaManagerImpl::FreezeShaders

int	MediaManagerImpl::GetNumGeometries( DWORD modelID ) 
{
	Node* pRoot = NodePool::GetNode( modelID );
	int nG = 0;
	Iterator it( pRoot, Geometry::FnFilter );
	while (it) { nG++; ++it; }
	return nG;
} // MediaManagerImpl::GetNumGeometries

IGeometry* MediaManagerImpl::GetGeometry( DWORD modelID, int idx, Matrix4D& tm )
{
	Node* pRoot = NodePool::GetNode( modelID );
	int nG = 0;
	Iterator it( pRoot, Geometry::FnFilter );
	while (it) 
	{ 
		if (nG == idx) 
		{
			Geometry* pGeom = (Geometry*)(Node*)it;
			tm = GetWorldTM( pGeom );
			return (IGeometry*)pGeom;
		}
		nG++; 
		++it; 
	}
	return NULL;
} // MediaManagerImpl::GetGeometry

DWORD MediaManagerImpl::CloneNode( DWORD nodeID )
{
	Node* pRoot = NodePool::GetNode( nodeID );
	if (!pRoot) return c_BadID;
	Node* pNewNode = pRoot->CloneSubtree();
	return pNewNode->GetID();
}

void MediaManagerImpl::DeleteNode( DWORD nodeID )
{
	Node* pNode = NodePool::GetNode( nodeID );
	if (!pNode) return;
	delete pNode;
} // MediaManagerImpl::DeleteNode

int	MediaManagerImpl::GetNumSubNodes( DWORD modelID )
{
	Node* pRoot = NodePool::GetNode( modelID );
	int nG = 0;
	Iterator it( pRoot, TransformNode::FnFilter );
	while (it) { nG++; ++it; }
	return nG;
} // MediaManagerImpl::GetNumSubNodes

const char*	MediaManagerImpl::GetSubNodeName( DWORD modelID, int idx )
{
	Node* pRoot = NodePool::GetNode( modelID );
	int nG = 0;
	Iterator it( pRoot, TransformNode::FnFilter );
	while (it) 
	{ 
		if (nG == idx) 
		{
			Node* pNode = (Node*)it;
			return pNode->GetName();
		}
		nG++; 
		++it; 
	}
	return "";
} // MediaManagerImpl::GetSubNodeName

void MediaManagerImpl::OnFrame()
{
	Animation::SetupTimeDelta();
}

AABoundBox MediaManagerImpl::GetBoundBox( DWORD id )
{
	Node* pNode = NodePool::GetNode( id );
	if (!pNode) return AABoundBox::null;
	return CalculateAABB( pNode );
}

bool MediaManagerImpl::Intersects( float mX, float mY, DWORD modelID, 
                                    const Matrix4D& tm, float& minDist )
{
    Node* pNode = NodePool::GetNode( modelID );
    if (!pNode) return false;
    ModelFile* pMF = dynamic_cast<ModelFile*>( pNode->GetParent() );
    AABoundBox aabb = pMF ? pMF->GetAABB() : CalculateAABB( pNode );
    
    ICamera* pCam = ::GetCamera();
    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );
    
    Matrix4D itm;
    itm.inverse( tm );
    ray.Transform( itm );
    if (!aabb.Overlap( ray )) return false;
    Vector3D pt;
    //if (!PickNode( ray, pNode, pt, minDist )) return false;
    return true;
} // MediaManagerImpl::Intersects

#include "sgAnimBlend.h"
bool ConvertModel( Node* pModel )
{
    if (pModel->IsA<sg::AnimationBlock>()) return false;
    std::vector<Node*>  gNodes;
    Node::Iterator it( pModel, SkinnedGeometry::FnFilter );
    while (it)
    {
        Node* pNode = (Node*)it;
        Node* pParent = pNode->GetParent();
        while (pParent != pModel)
        {
            pNode = pParent;
            pParent = pParent->GetParent();
        }
        gNodes.push_back( pNode );
        ++it;
    }

    for (int i = 0; i < gNodes.size(); i++)
    {
        Node* pNode = gNodes[i];
        pNode->AddRef();
        pModel->RemoveChild( pNode );
        pModel->AddChild( pNode );
        pNode->Release();
    }

    Node::Iterator git( pModel, SkinnedGeometry::FnFilter );
    while (git)
    {
        SkinnedGeometry* pGeom = (SkinnedGeometry*)(Node*)git;
        BaseMesh* mesh = &pGeom->GetPrimitive();
        VertexFormat vf = mesh->getVertexFormat();
        switch (vf)
        {
        case vfNMP1: 
            ConvertVF<Vertex1W, VertexNMP1>( *mesh );
            break;
        case vfNMP2: 
            ConvertVF<Vertex2W, VertexNMP2>( *mesh );
            break;
        case vfNMP3: 
            ConvertVF<Vertex3W, VertexNMP3>( *mesh );
            break;
        case vfNMP4:
            ConvertVF<Vertex4W, VertexNMP4>( *mesh );
            break;
        }
        ++git;
    }

    return gNodes.size() != 0;
} // ConvertModel


bool LoadRawModel( const char* fname, std::vector<Vector3D>& vert, std::vector<int>& idx )
{
    DWORD mdlID = IMM->GetModelID( fname );
    if (mdlID == 0xFFFFFFFF) return false;
    Node* pMdl = NodePool::GetNode( mdlID );
    if (!pMdl) return false;
    Node::Iterator it( pMdl, Geometry::FnFilter );
    int cV = 0;
    while (it)
    {
        Geometry* pGeom = (Geometry*)(Node*)it;
        BaseMesh& bm = pGeom->GetPrimitive();
        Matrix4D tm = GetWorldTM( pGeom );
        VertexIterator vit;
        vit << bm;
        while (vit)
        {
            Vector3D v = vit.pos();
            tm.transformPt( v );
            vert.push_back( v );
            ++vit;
        }
        int nInd = bm.getNInd();
        WORD* widx = bm.getIndices();
        for (int i = 0; i < nInd; i++)
        {
            idx.push_back( widx[i] + cV );
        }
        cV += bm.getNVert();
        ++it;
    }
    return true;
} // LoadRawModel















