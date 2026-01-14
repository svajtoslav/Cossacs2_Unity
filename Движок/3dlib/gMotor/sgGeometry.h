/*****************************************************************************/
/*	File:	sgGeometry.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGGEOMETRY_H__
#define __SGGEOMETRY_H__

#include "IMediaManager.h"

ENUM(PrimitiveType, "Primitive Type",
	 en_val( ptUnknown,			"Unknown"		) <<		
	 en_val( ptPointList,		"PointList"		) <<	
	 en_val( ptLineList,		"LineList"		) <<	
	 en_val( ptLineStrip,		"LineStrip"		) <<	
	 en_val( ptTriangleList,	"TriangleList"	) <<
	 en_val( ptTriangleStrip,	"TriangleStrip"	) <<	
	 en_val( ptTriangleFan,		"TriangleFan"	) );

namespace sg{
/*****************************************************************************/
/*	Class:	Geometry
/*	Desc:	Geometry data container node
/*****************************************************************************/
class Geometry : public Node, public IGeometry
{
    //  debug purposes
    bool				m_bShowMeshNormals;	
    float				m_NormalLen;

protected:
	BaseMesh			m_Mesh;				//  mesh data
	AABoundBox			m_AABB;				//  axis-aligned bounding box	

public:
						Geometry	() : m_bShowMeshNormals( false ), m_NormalLen( 20.0f ) {}
	void				Create		( int nVert, int nIdx, VertexFormat vertFormat, PrimitiveType ptype = ptTriangleList );
	_inl int			AddVertex	( void* pVert );
	_inl int			AddPoly		( WORD v1, WORD v2, WORD v3 );
	_inl bool			AddQuad		(	const Vector3D& a, const Vector3D& b,
										const Vector3D& c, const Vector3D& d,
										const Rct* pUV = NULL );

	_inl BaseMesh&		GetPrimitive    ()	 { return m_Mesh;  }
	_inl int			GetNPoly	    () const { return m_Mesh.getNPri();    }
	_inl int			GetNVert	    () const { return m_Mesh.getNVert();   }
	_inl PrimitiveType	GetPriType      () const { return m_Mesh.getPriType(); }
	_inl VertexFormat	GetVertexFormat () const { return m_Mesh.getVertexFormat(); }

	_inl const AABoundBox&	GetAABB	    () const { return m_AABB; }
	_inl void				SetAABB	    ( const AABoundBox& aabb ) { m_AABB = aabb; }

	void				DumpToCPP	    ( FILE* fp );
	void				DumpToCPP	    ();
	Sphere				GetBoundSphere  ();

	DWORD				GetDiffuse	    () const;
	void				SetDiffuse	    ( DWORD color );
	virtual void		CalculateNormals();
	virtual void	    FlipNormals	    ();

	virtual void		Render		    ();
	virtual void		Serialize	    ( OutStream&	os ) const;
	virtual void		Unserialize	    ( InStream&		is );
	virtual void		Expose		    ( PropertyMap&  pm );

	virtual void		GetVertexIterator( VertexIterator& it );
	bool				IsStatic		() const { return m_Mesh.isStatic(); }

	virtual int			GetVertexStride	() const { return m_Mesh.getVertexStride(); }
	virtual int			GetIndexStride	() const { return 2; }
	virtual int			GetNumVertices	() const { return m_Mesh.getNVert(); }
	virtual int			GetNumIndices	() const { return m_Mesh.getNInd(); }
	virtual Vector3D	GetCenter		() const { Vector3D c; m_Mesh.GetCentroid( c ); return c; }
	virtual WORD*		GetIndices		() { return m_Mesh.getIndices(); }
	virtual int			Pick			( const Vector3D& org, const Vector3D& dir, Vector3D& pt );
	virtual void		SetIsStatic	    ( bool val = true ); 

    void                PostRender      ();

	NODE(Geometry,Node,GEOM);
}; // class Geometry 

/*****************************************************************************/
/*	Class:	MorphedGeometry
/*	Desc:	Geometry with vertex position bound to another scene graph node
/*				transforms. 
/*****************************************************************************/
class MorphedGeometry : public Geometry
{
public:
							MorphedGeometry () : m_bDisableMorphing( false ) {}
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os		) const;
	virtual void			Unserialize		( InStream& is		);

	static void				Freeze			() { s_bFrozen = true; }
	static void				Unfreeze		() { s_bFrozen = false; }
	virtual void			Render			();

	_inl void				ProcessGeometry ();
	_inl void				RenderMorphedGeometry();

    BaseMesh*               GetOriginalPrimitive() { return &m_OriginalMesh; }

	NODE(MorphedGeometry,Geometry,MGEO);

	void					ReplicateMesh	() { GetPrimitive().copy( m_OriginalMesh ); }

protected:

	//  callback, which is implemented by actual geometry morphers
	virtual void			OnProcessGeometry() {}

	BaseMesh				m_OriginalMesh;		// transformed mesh
	bool					m_bDisableMorphing;
	static bool				s_bFrozen;
}; // class MorphedGeometry

/*****************************************************************************/
/*	Class:	SkinnedGeometry
/*	Desc:	Geometry with vertex position bound to another scene graph node
/*				transforms. 
/*****************************************************************************/
class SkinnedGeometry : public MorphedGeometry
{
public:
							SkinnedGeometry	() : m_NWeights(0), m_BoneTM(NULL), m_NBones(0) {}
	virtual					~SkinnedGeometry();
	
	virtual void			Serialize		( OutStream& os		) const;
	virtual void			Unserialize		( InStream& is		);
	virtual void			Expose			( PropertyMap& pm );
	void					AddBoneOffset	( const Matrix4D& matr ) { m_BoneOffset.push_back( matr ); }
	_inl const Matrix4D& 	GetMatrix		( int idx );
	virtual void			Render			();

	void					SetIsStatic		( bool val = true ) {}
    virtual void            FlipNormals     ();
    virtual void            CalculateNormals();

	NODE(SkinnedGeometry,MorphedGeometry,SKIN);

protected:
	void					OnProcessGeometry();

	Matrix4D*	            m_BoneTM;
    int                     m_NBones;
	std::vector<Matrix4D>	m_BoneOffset;
	int						m_NWeights;		//  number of skinning weights
}; // class SkinnedGeometry

/*****************************************************************************/
/*	Class:	SegmentNode
/*	Desc:	Contains set of segment chains in the space
/*****************************************************************************/
class SegmentNode : public Node
{
	Vector3D		m_Beg, m_End;
	Vector3D		m_Normal;
	float			m_Width;
public:
					SegmentNode		();
	virtual void	Serialize		( OutStream& os		) const;
	virtual void	Unserialize		( InStream& is		);
	virtual void	Expose			( PropertyMap& pm );
	virtual void	Render			();

	const Vector3D&	GetBeg			() const { return m_Beg; }
	const Vector3D&	GetEnd			() const { return m_End; }
	const Vector3D&	GetNormal		() const { return m_Normal; }
	float			GetWidth		() const { return m_Width; }

	void			SetBeg			( const Vector3D& v ) { m_Beg = v; }
	void			SetEnd			( const Vector3D& v ) { m_End = v; }
	void			SetNormal		( const Vector3D& v ) { m_Normal = v; }
	void			SetWidth		( float val ) { m_Width = val; }


	NODE(SegmentNode,Node,SGNO);
}; // class SegmentNode

/*****************************************************************************/
/*	Class:	SegmentSystem
/*	Desc:	Contains set of segment chains in the space
/*****************************************************************************/
class SegmentSystem : public Node
{
	DWORD			m_DrawColor;
	DWORD			m_CoreColor;
	bool			m_bRoundEnds;
	bool			m_bEnableZ;
public:
					SegmentSystem();

	virtual void	Serialize		( OutStream& os		) const;
	virtual void	Unserialize		( InStream& is		);
	virtual void	Expose			( PropertyMap& pm );
	virtual void	Render			();

	void			SetColor		( DWORD clr ) { m_DrawColor = clr; }
	void			SetCoreColor	( DWORD clr ) { m_CoreColor = clr; }

	NODE(SegmentSystem,Node,SGST);
}; // class SegmentSystem

/*****************************************************************************/
/*	Class:	GeometryRef
/*	Desc:	Geometry subset from some polygon soup node
/*****************************************************************************/
class GeometryRef : public Node
{
	Geometry*			pool;

	DWORD				firstInd;
	DWORD				nInd;
	DWORD				firstVert;
	DWORD				nVert;

public:
	_inl				GeometryRef();

	virtual void		Draw		(){}
	virtual void		Serialize	( OutStream&	os ) const;
	virtual void		Unserialize	( InStream&		is );

	NODE(GeometryRef,Node,GREF);

}; // class GeometryRef





enum MeshPointSample
{
    mpsUnknown      = 0,
    mpsVertex       = 1,    //  sampling at each mesh vertex
    mpsPolyCenter   = 2,    //  sampling at center of each polygon
    mpsPolyRandom   = 3,    //  sampling randomly in mesh polygons
}; // enum MeshPointSample
Vector3D    GetRandomPoint( const Primitive& p, MeshPointSample sample = mpsVertex );

Sphere		GetStaticBoundSphere( Node* pNode );
AABoundBox	CalculateAABB		( Node* pNode );


void FlattenStaticHierarchy	( Node* pRootNode );
void SortPolys( const Ray3D& ray, BaseMesh& bm );

int  CountPolygons( Node* pNode );
int  CountVertices( Node* pNode );

//  splits mesh by plane into two meshes
bool Split( const Primitive& mesh, 
		    const Plane& plane, 
			Primitive& posMesh, Primitive& negMesh );

void ScanHeightmap( const Primitive& pri, const Matrix4D& tm, float stepx, float stepy, SetHeightCallback put );
void ScanHeightmap( const Primitive& pri, const Matrix4D& tm, float stepx, float stepy, VisitHeightCallback put );

Node* PickNode( const Ray3D& ray, Node* pNode, Vector3D& pt, float& minDist );

} // namespace sg

#ifdef _INLINES
#include "sgGeometry.inl"
#endif // _INLINES

#endif // __SGGEOMETRY_H__