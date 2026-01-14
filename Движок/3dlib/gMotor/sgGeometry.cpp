/*****************************************************************************/
/*	File:	sgGeometry.cpp
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgGeometry.h"
#include "sgDummy.h"
#include "sgCamera.h"

#include "kIOHelpers.h"
#include "IWidgetManager.h"
#include "mSpatial.h"
#include "mSkin.h"

#ifndef _INLINES
#include "sgGeometry.inl"
#endif // !_INLINES

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Geometry	implementation
/*****************************************************************************/
void Geometry::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Mesh;
} // Geometry::Serialize

void Geometry::Unserialize( InStream& is ) 
{
	Parent::Unserialize( is );
	is >> m_Mesh;
	m_Mesh.GetAABB( m_AABB );
} // Geometry::Serialize

void Geometry::SetIsStatic( bool val ) 
{ 
    m_Mesh.setIsStatic( val ); 
    m_Mesh.setDevHandle( NULL ); 
}

void Geometry::Create( int nVert, int nIdx, VertexFormat vertFormat, PrimitiveType ptype )
{
    m_Mesh.create( nVert, nIdx, vertFormat, ptype );
}

void Geometry::PostRender()
{
    if (m_bShowMeshNormals)
    {
        VertexIterator it;
        it << GetPrimitive();
        while (it)
        {
            Vector3D norm = it.n();
            Vector3D pos  = it;
            norm *= m_NormalLen;
            norm += pos;
            rsLine( pos, norm, 0xFFFF0000, 0xFFFF0000 );
            ++it;
        }
        rsFlushLines3D();
    }

    if (DoDrawGizmo())
    {
        static shWire = IRS->GetShaderID( "wire" );
        IRS->SetCurrentShader( shWire );
        IRS->DrawPrim( m_Mesh );
    }
} // Geometry::PostRender

void Geometry::Render()
{
	Node::Render();
	IRS->SetWorldMatrix( TransformNode::TMStackTop() );
	IRS->DrawPrim( m_Mesh );
	PostRender();
} // Geometry::Render

void Geometry::GetVertexIterator( VertexIterator& it )
{
	it << m_Mesh;
} // Geometry::GetVertexIterator

DWORD Geometry::GetDiffuse() const
{
	if (m_Mesh.getNVert() == 0) return 0;
	BaseMesh* pri = const_cast<BaseMesh*>( &m_Mesh );
	VertexIterator vit;
	vit << *pri;
	int nV = 0;
	float a, r, g, b;
	float ta = 0.0f;
	float tr = 0.0f;
	float tg = 0.0f;
	float tb = 0.0f;
	if (!vit.HasDiffuse()) return 0;

	while (vit)
	{
		ColorValue::FromARGB( vit.diffuse(), a, r, g, b );
		ta += a;
		tr += r;
		tg += g;
		tb += b;

		nV++;
		++vit;
	}
	
	float fnv = float( nV );
	return ColorValue::ToARGB( ta/fnv, tr/fnv, tg/fnv, tb/fnv );
} // Geometry::GetDiffuse
 
void Geometry::SetDiffuse( DWORD color )
{
	GetPrimitive().setDiffuseColor( color );
} // Geometry::SetDiffuse

void Geometry::Expose( PropertyMap&  pm )
{
	pm.start<Parent>( "Geometry", this );
	pm.p( "Polygons",		GetNPoly );
	pm.p( "Vertices",		GetNVert );
	pm.p( "Diffuse",		GetDiffuse, SetDiffuse, "color" );
	pm.p( "Static",		IsStatic,	SetIsStatic );
	pm.p( "PrimitiveType",	GetPriType		);
	pm.p( "VertexFormat",	GetVertexFormat	);
	pm.f( "AABB xyz",		m_AABB.minv, "direction", true );
	pm.f( "AABB XYZ",		m_AABB.maxv, "direction", true );

	pm.f( "ShowMeshNormals",m_bShowMeshNormals	);
	pm.f( "MeshNormalLen",	m_NormalLen			);
	pm.m( "CalculateNormals", CalculateNormals );
	pm.m( "FlipNormals",	FlipNormals );
	pm.m( "DumpToCPP",		DumpToCPP );
} // Geometry::Expose

void Geometry::CalculateNormals()
{
	m_Mesh.calcNormals();
}

void Geometry::FlipNormals()
{
	VertexIterator vit;
	vit << m_Mesh;
	if (!vit.HasNormal()) return;
	while (vit)
	{
		vit.n().reverse();
		++vit;
	}
} // Geometry::FlipNormals

int	Geometry::Pick( const Vector3D& org, const Vector3D& dir, Vector3D& pt )
{
	return -1;
}

Sphere Geometry::GetBoundSphere()
{
	Vector3D c;
	float r = 0.0f;
	m_Mesh.GetCentroid( c );

	VertexIterator it;
	it << m_Mesh;
	while (it)
	{
		float d2 = c.distance2( it );
		if (d2 > r) r = d2;
		++it;
	}
	return Sphere( c, sqrtf( r ) );
} // Geometry::GetBoundSphere

void Geometry::DumpToCPP()
{
	FILE* fp = fopen( "c:\\dumps\\geomdump.cpp", "wt" );
	if (!fp) return;
	DumpToCPP( fp );
	fclose( fp );
}

void Geometry::DumpToCPP( FILE* fp )
{
	VertexIterator v;
	v << m_Mesh;
	fprintf( fp, "const float c_%sV[%d][5] = {\n", GetName(), m_Mesh.getNVert() );
	while (v)
	{
		Vector3D vec = v;
		fprintf( fp, "\t{%.5f, %.5f, %.5f, %.5f, %.5f},\n", vec.x, vec.y, vec.z, v.u(), v.v() );
		++v;
	}
	fprintf( fp, "};\n\n" );
	
	WORD* idx = m_Mesh.getIndices();
	int nPri = m_Mesh.getNPri();
	fprintf( fp, "const WORD c_%sI[%d][3] = {\n", GetName(), nPri );
	for (int i = 0; i < nPri; i++)
	{
		fprintf( fp, "\t{%d, %d, %d},\n", idx[i*3], idx[i*3+1], idx[i*3+2] );
	}
} // Geometry::DumpToCPP

/*****************************************************************************/
/*	MorphedGeometry	implementation
/*****************************************************************************/
bool MorphedGeometry::s_bFrozen = false;

void MorphedGeometry::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "MorphedGeometry", this );
	pm.f( "DisableMorphing", m_bDisableMorphing );
}

void MorphedGeometry::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // MorphedGeometry::Serialize

void MorphedGeometry::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	ReplicateMesh();
} // MorphedGeometry::Unserialize

void MorphedGeometry::Render()
{
	Node::Render();
	RenderMorphedGeometry();
} // MorphedGeometry::Render

/*****************************************************************************/
/*	SkinnedGeometry	implementation
/*****************************************************************************/
SkinnedGeometry::~SkinnedGeometry()
{
    aligned_delete_nodestruct( m_BoneTM );
}

void SkinnedGeometry::Serialize( OutStream& os ) const
{
	Node::Serialize( os );
	os << m_OriginalMesh;
    os << m_BoneOffset;
} // SkinnedGeometry::Serialize

void SkinnedGeometry::FlipNormals()
{
    VertexIterator vit;
    vit << m_OriginalMesh;
    if (!vit.HasNormal()) return;
    while (vit)
    {
        vit.n().reverse();
        ++vit;
    }
} // Geometry::FlipNormals

void SkinnedGeometry::CalculateNormals()
{
    m_OriginalMesh.calcNormals();
}

void SkinnedGeometry::Unserialize( InStream& is )
{
	Node::Unserialize( is );
	is >> m_OriginalMesh;
    is >> m_BoneOffset;

	m_OriginalMesh.GetAABB( m_AABB );
    m_Mesh.create   ( m_OriginalMesh.getNVert(), m_OriginalMesh.getNInd(), vfN );
    m_Mesh.setNVert ( m_OriginalMesh.getNVert() );
    m_Mesh.setNPri  ( m_OriginalMesh.getNPri() );
    m_Mesh.setNInd  ( m_OriginalMesh.getNInd() );
    m_Mesh.setIndices( m_OriginalMesh.getIndices(), m_OriginalMesh.getNInd() );

	VertexFormat vf = m_OriginalMesh.getVertexFormat();
	switch (vf)
	{
    case vfNMP1: m_NWeights = 1; break;
    case vfNMP2: m_NWeights = 2; break;
    case vfNMP3: m_NWeights = 3; break;
    case vfNMP4: m_NWeights = 4; break;
	case vf1W:   m_NWeights = 1; break;
	case vf2W:   m_NWeights = 2; break;
    case vf3W:   m_NWeights = 3; break;
    case vf4W:   m_NWeights = 4; break;
	default: m_NWeights = 0;
	}

    m_NBones = m_BoneOffset.size();
    m_BoneTM = aligned_new<Matrix4D>( m_NBones, 32 );
} // SkinnedGeometry::Unserialize

void SkinnedGeometry::Render()
{
    ProcessGeometry();
    IRS->ResetWorldMatrix();
    IRS->DrawPrim( m_Mesh );
    PostRender();
} // SkinnedGeometry::Render

void SkinnedGeometry::OnProcessGeometry()
{
	BaseMesh& skin = GetPrimitive();
	
	int nV = skin.getNVert();
	int nTM = GetNChildren();
    const Matrix4D& topTM = TransformNode::TMStackTop();
	for (int i = 0; i < nTM; i++)
	{
		m_BoneTM[i] = GetMatrix( i );
		m_BoneTM[i].mulLeft( m_BoneOffset[i] );
	}
	const Matrix4D* pBones = m_BoneTM;
	
	switch (m_NWeights)
	{		
	case 1: 
		{
			Vertex1W*	sBuf = (Vertex1W*)m_OriginalMesh.getVertexData();
			VertexOut*	dBuf = (VertexOut*)skin.getVertexData();
			Skin1( sBuf, dBuf, nV, pBones );
		} break;
	case 2: 
		{	
			Vertex2W*	sBuf = (Vertex2W*)m_OriginalMesh.getVertexData();
			VertexOut*	dBuf = (VertexOut*)skin.getVertexData();
			Skin2( sBuf, dBuf, nV, pBones );	
		} break;
	case 3: 
		{
			Vertex3W*	sBuf = (Vertex3W*)m_OriginalMesh.getVertexData();
			VertexOut*	dBuf = (VertexOut*)skin.getVertexData();
			Skin3( sBuf, dBuf, nV, pBones );			
		} break;
	case 4: 
		{
			Vertex4W*	sBuf = (Vertex4W*)m_OriginalMesh.getVertexData();
			VertexOut*	dBuf = (VertexOut*)skin.getVertexData();
			Skin4( sBuf, dBuf, nV, pBones );			
		} break;
	}
} // SkinnedGeometry::ProcessSkin

void SkinnedGeometry::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "SkinnedGeometry", this );
} // SkinnedGeometry::Expose

/*****************************************************************************/
/*	SegmentNode	implementation
/*****************************************************************************/
SegmentNode::SegmentNode()
{
	m_Beg = m_End = Vector3D::null;
	m_Normal = Vector3D::oZ;
	m_Width = 10.0f;
}

void SegmentNode::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Beg << m_End << m_Width;
}		  

void SegmentNode::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Beg >> m_End >> m_Width;
}

void SegmentNode::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "SegmentNode", this );
	pm.f( "Width", m_Width );
	pm.f( "BegX", m_Beg.x );
	pm.f( "BegY", m_Beg.y );
	pm.f( "BegZ", m_Beg.z );
	pm.f( "EndX", m_End.x );
	pm.f( "EndY", m_End.y );
	pm.f( "EndZ", m_End.z );
}

void SegmentNode::Render()
{
}

/*****************************************************************************/
/*	SegmentSystem implementation
/*****************************************************************************/
SegmentSystem::SegmentSystem()
{
	m_DrawColor		= 0xAAEEEE22;
	m_bRoundEnds	= true;
	m_bEnableZ		= true;
	m_CoreColor		= 0xFFFFFF00;
}

void SegmentSystem::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_DrawColor << m_CoreColor << m_bRoundEnds << m_bEnableZ;
}		  
		  
void SegmentSystem::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_DrawColor >> m_CoreColor >> m_bRoundEnds >> m_bEnableZ;
}

void SegmentSystem::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "SegmentSystem", this );
	pm.f( "Color", m_DrawColor, "color" );
	pm.f( "CoreColor", m_CoreColor, "color" );
	pm.f( "RoundEnds", m_bRoundEnds );	
	pm.f( "EnableZ", m_bEnableZ );
} // SegmentSystem::Expose

void SegmentSystem::Render()
{
	IRS->SetWorldMatrix( TransformNode::TMStackTop() );
	for (int i = 0; i < GetNChildren(); i++)
	{
		SegmentNode* pSeg = (SegmentNode*)GetChild( i );
		if (pSeg->IsInvisible()) continue;
		if (pSeg->IsA<SegmentNode>())
		{
			DrawFatSegment( pSeg->GetBeg(), pSeg->GetEnd(), pSeg->GetNormal(), 
							pSeg->GetWidth(), m_bRoundEnds, 
							m_DrawColor, m_CoreColor );
		}
	}
	rsEnableZ( m_bEnableZ );
	rsFlushPoly3D();
	rsEnableZ( false );
	rsFlushLines3D();
} // SegmentSystem::Render

/*****************************************************************************/
/*	GeometryRef	implementation
/*****************************************************************************/
void GeometryRef::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );

	os << firstInd << nInd << firstVert << nVert;

	DWORD poolID = c_BadID;
	if (pool)
	{
		NodePtrMap::iterator it = s_NodeMap.find( (Node*)pool );
		if (it != s_NodeMap.end())
		{
			poolID = (*it).second;
		}
	}

	os << poolID;
} // GeometryRef::Serialize

void GeometryRef::Unserialize( InStream& is ) 
{
	Parent::Unserialize( is );

	DWORD poolID;
	is >> firstInd >> nInd >> firstVert >> nVert >> poolID;
} // GeometryRef::Serialize


void FlattenStaticHierarchy( Node* pRootNode )
{
	if (!pRootNode) return;
	Node::Iterator it( pRootNode );
	
	Matrix4D rootTM = GetWorldTM( pRootNode );
	Matrix4D invRootTM;
	invRootTM.inverse( rootTM );

	Group* pResult = new Group();

	Geometry* pBase = NULL;
	
	while (it)
	{
		Node* pNode = it;
		if (pNode->IsInvisible()) it.Up();
		if (pNode->HasFn( Geometry::Magic() ))
		{
			Geometry* pGeom = (Geometry*)pNode;
			Matrix4D geomTM( GetWorldTM( pNode ) );
			geomTM *= invRootTM;
			pGeom->GetPrimitive().transform( geomTM );
			//pResult->AddChild( pGeom );

			if (pBase)
			{
				pBase->GetPrimitive() += pGeom->GetPrimitive();
			}
			else
			{
				pBase = pGeom;
				pResult->AddChild( pBase );
			}
		}
		++it;
	}

	if (pResult->GetNChildren() > 0) 
	{
		pRootNode->RemoveChildren();
		pRootNode->AddChild( pResult );
	}
	else
	{
		delete pResult;
	}
} // FlattenStaticHierarchy

static BYTE* s_VertArray;
static int	 s_VertStride;
static Ray3D s_Ray;

int DistCmp( const void* p1, const void* p2 )
{
	WPoly* pPoly1 = (WPoly*)p1;
	WPoly* pPoly2 = (WPoly*)p2;
	
	Triangle tri1;
	pPoly1->GetTriangle( tri1, s_VertArray, s_VertStride );

	Triangle tri2;
	pPoly2->GetTriangle( tri2, s_VertArray, s_VertStride );
	
	float d1 = tri1.Distance( s_Ray );
	float d2 = tri2.Distance( s_Ray );
	
	return d1 < d2;
} // DistCmp

void SortPolys( const Ray3D& ray, BaseMesh& bm )
{
	/*BSPTree<VertexN, WPoly> bsp;
	bsp.Create( (VertexN*)bm.getVertexData(), bm.getNVert(), 
				(WPoly*)bm.getIndices(), bm.getNPri() );*/
	
	s_VertStride = bm.getVertexStride();
	s_VertArray  = (BYTE*)bm.getVertexData();
	s_Ray = ray;
	qsort( bm.getIndices(), bm.getNPri(), sizeof( WPoly ), DistCmp );
	
} // SortPolys

int CountPolygons( Node* pNode )
{
	int nP = 0;
	Node::Iterator it( pNode, Geometry::FnFilter );
	while (it) 
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		BaseMesh& bm = pGeom->GetPrimitive();
		nP += bm.getNPri();
		++it;
	}
	return nP;
} // CountPolygons

int CountVertices( Node* pNode )
{
	int nV = 0;
	Node::Iterator it( pNode, Geometry::FnFilter );
	while (it) 
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		BaseMesh& bm = pGeom->GetPrimitive();
		nV += bm.getNVert();
		++it;
	}
	return nV;
} // CountVertices

Sphere GetStaticBoundSphere( Node* pNode )
{
	Sphere sphere( Vector3D::null, 0.0f );
	if (!pNode) return sphere; 

	Node::Iterator it( pNode, Geometry::FnFilter );
	
	bool bFirst = true;

	while (it)
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		Sphere locSphere = pGeom->GetBoundSphere();
		locSphere.Transform( GetWorldTM( pGeom ) );
		if (bFirst)
		{
			bFirst = false;
			sphere = locSphere;
			++it;
		}
		sphere += locSphere;
		++it;
	}
	return sphere;
} // GetStaticBoundSphere

AABoundBox	CalculateAABB( Node* pNode )
{
	AABoundBox aabb( Vector3D::null, 0.0f );
	if (!pNode) 
    {
        return aabb;
    }

	Node::Iterator it( pNode, Geometry::FnFilter );
	bool bFirst = true;

	while (it)
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		AABoundBox cbox = pGeom->GetAABB();
		cbox.Transform( GetWorldTM( pGeom ) );
		if (bFirst)
		{
			bFirst = false;
			aabb = cbox;
			++it;
            continue;
		}
		aabb.Union( cbox );
		++it;
	}
	return aabb;
} // CalculateAABB

Node* PickNode( const Ray3D& ray, Node* pNode, Vector3D& pt, float& minDist )
{
	Node* pSelNode = NULL;
	if (!pNode) return false;
	minDist = FLT_MAX;
	Node::Iterator it( pNode, Geometry::FnFilter );
	while (it)
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
        BaseMesh& bm = pGeom->IsA<SkinnedGeometry>() ? 
                    *((SkinnedGeometry*)pGeom)->GetOriginalPrimitive() : pGeom->GetPrimitive();

		Matrix4D tm = GetWorldTM( pGeom );
		Matrix4D invTM;
		invTM.inverse( tm );
		Ray3D tray( ray ); 
		invTM.transformPt( tray.Orig() );
		invTM.transformVec( tray.Dir() );
		int triIdx = -1;
        if (bm.getNVert() > 5000) 
        {
            minDist  = 0.0f;
            pSelNode = pGeom;
            ++it;
            continue;
        }
		float dist = bm.PickPoly( tray, triIdx );
		if (triIdx >= 0)
		{
			pt = tray.getPoint( dist );
			tm.transformPt( pt );
			dist = pt.distance( ray.getOrig() );
			if (dist < minDist || !pSelNode)
			{
				pSelNode = pGeom;
				minDist = dist;
			}
		}
		++it;
	}
	return pSelNode;
} // PickNode


bool Split( const Primitive& mesh, const Plane& plane, Primitive& posMesh, Primitive& negMesh )
{
	if (mesh.getPriType() != ptTriangleList) return false;
	WPoly* poly = (WPoly*)mesh.getIndices();
	int nP = mesh.getNPri();

	Triangle tri;
	int stride = mesh.getVertexStride();
	for (int i = 0; i < nP; i++)
	{
		poly->GetTriangle( tri, mesh.getVertexData(), stride );
		XStatus xs = tri.Intersect( plane );
		//if (xs == xsInside) posMesh.Add( tri );
		//else if (xs == xsOutside) negMesh.Add( tri );
		//else
		////  split triangle
		//{
		//
		//}
	}
	return true;
} // Split


void ScanHeightmap( const Primitive& pri, const Matrix4D& tm, 
				   float stepx, float stepy, SetHeightCallback put )
{
	assert( put );
	AABoundBox aabb, AABB;
	pri.GetAABB( aabb );
	AABB = aabb;
	AABB.Transform( tm );

	Matrix4D inv;
	inv.inverse( tm );

	float bbdx=AABB.maxv.x-AABB.minv.x;
	float bbdy=AABB.maxv.y-AABB.minv.y;

	AABB.minv.x-=bbdx;
	AABB.minv.y-=bbdy;
	AABB.maxv.x+=bbdx;
	AABB.maxv.y+=bbdy;

	int	  begnx = floorf( AABB.minv.x/stepx );
	float begx  = floorf( AABB.minv.x/stepx ) * stepx;

	int	  begny = floorf( AABB.minv.y/stepy );
	float begy  = floorf( AABB.minv.y/stepy ) * stepy;

	int ny = begny;
	int triIdx = -1;
	Vector3D ldir(0,-0.5,0.866);
	inv.transformVec(ldir);
	Vector3D ldir1;
	ldir1=inv.getV2();
	Ray3D ray( tm.getTranslation(), ldir );

	Vector3D org( 0, 0, 0 );	    
	
	
	for (float y = begy; y <= AABB.maxv.y; y += stepy)
	{
		int nx = begnx;
		for (float x = begx; x <= AABB.maxv.x; x += stepx)
		{
			org.set( x, y, 0.0f );
			inv.transformPt( org );
			ray.setOrig( org );
			float H = pri.PickPoly( ray, triIdx )/* + aabb.minv.z*/;
			//H*= l;
			if (triIdx != -1)
			{
				put( nx, ny, H );
			}
			nx++;
		}
		ny++;
	}		
} // ScanHeightmap


void ScanHeightmap( const Primitive& pri, const Matrix4D& tm, 
				   float stepx, float stepy, VisitHeightCallback put )
{
	assert( put );
	AABoundBox aabb, AABB;
	pri.GetAABB( aabb );
	AABB = aabb;
	AABB.Transform( tm );

	Matrix4D inv;
	inv.inverse( tm );

	int	  begnx = floorf( AABB.minv.x/stepx );
	float begx  = floorf( AABB.minv.x/stepx ) * stepx;

	int	  begny = floorf( AABB.minv.y/stepy );
	float begy  = floorf( AABB.minv.y/stepy ) * stepy;

	int ny = begny;
	int triIdx = -1;

	Vector3D ldir( inv.getV2() );
	Ray3D ray( tm.getTranslation(), ldir );
	Vector3D org( 0, 0, 0 );

	for (float y = begy; y <= AABB.maxv.y; y += stepy)
	{
		int nx = begnx;
		for (float x = begx; x <= AABB.maxv.x; x += stepx)
		{
			org.set( x, y, aabb.minv.z );
			inv.transformPt( org );
			ray.setOrig( org );
			float H = pri.PickPoly( ray, triIdx ) + aabb.minv.z;
			if (triIdx != -1)
			{
				put( nx, ny );
			}
			nx++;
		}
		ny++;
	}		
} // ScanHeightmap


static Vector3D s_Axis;
static int		s_VStride;
static BYTE*	s_Vert;

int __cdecl PolyCmp( const void *t1, const void *t2 )
{
	WORD* pT1 = (WORD*)t1;
	WORD* pT2 = (WORD*)t2;
	Vector3D* v10 = (Vector3D*)(s_Vert + s_VStride*pT1[0]);
	Vector3D* v11 = (Vector3D*)(s_Vert + s_VStride*pT1[1]);
	Vector3D* v12 = (Vector3D*)(s_Vert + s_VStride*pT1[2]);

	Vector3D* v20 = (Vector3D*)(s_Vert + s_VStride*pT2[0]);
	Vector3D* v21 = (Vector3D*)(s_Vert + s_VStride*pT2[1]);
	Vector3D* v22 = (Vector3D*)(s_Vert + s_VStride*pT2[2]);
	
	float d1 = tmax( s_Axis.dot( *v10 ), s_Axis.dot( *v11 ), s_Axis.dot( *v12 ) );
	float d2 = tmax( s_Axis.dot( *v20 ), s_Axis.dot( *v21 ), s_Axis.dot( *v22 ) );
	if (d1 < d2) return -1;
	if (d1 > d2) return  1;
	return 0;
}

void SortPolygons( Primitive& pri, const Vector3D& axis )
{
	WORD* idx	= pri.getIndices();
	int nTri	= pri.getNPri();
	s_Axis		= axis;
	s_VStride	= pri.getVertexStride();
	s_Vert		= (BYTE*)pri.getVertexData();
	qsort( idx, nTri, sizeof(WORD)*3, PolyCmp );
} // SortPolygons

void DrawPolygonNumbers( Primitive& pri, int fontID )
{
	WORD* idx	= pri.getIndices();
	int nTri	= pri.getNPri();
	int stride	= pri.getVertexStride();
	BYTE* vert	= (BYTE*)pri.getVertexData();

	for (int i = 0; i < nTri; i++)
	{
		Vector3D* v0 = (Vector3D*)(vert + stride*idx[i*3 + 0]);
		Vector3D* v1 = (Vector3D*)(vert + stride*idx[i*3 + 1]);
		Vector3D* v2 = (Vector3D*)(vert + stride*idx[i*3 + 2]);
		Vector3D v( *v0 );
		v += *v1;
		v += *v2;
		v *= 1.0f/3.0f;
		char str[256];
		sprintf( str, "%d", i );
		IWM->DrawStringW( fontID, str, v );
	}

} // DrawPolygonNumbers

Vector3D GetRandomPoint( const Primitive& p, MeshPointSample sample )
{
    if (sample == mpsVertex)
    {
        int nV = p.getNVert();
        int vIdx = rndValue( 0, nV - 1 );
        float* v = (float*)(p.getVertexData() + vIdx*p.getVertexStride());
        return Vector3D( v[0], v[1], v[2] );
    }
    assert( false );
    return Vector3D::null;
} // GetRandomPoint

END_NAMESPACE( sg )

