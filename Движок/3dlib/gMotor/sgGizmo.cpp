#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgGeometry.h"
#include "sgGizmo.h"

BEGIN_NAMESPACE( sg )

/*****************************************************************************/
/*	VectorField implementation
/*****************************************************************************/
bool VectorField::CreateNormalField( sg::Geometry* pGeom, DWORD color )
{
	if (!pGeom) return false;

	BaseMesh& bm = pGeom->GetPrimitive();
	int nV = bm.getNVert();
	CreateVectorField( nV );

	AABoundBox aabb;
	bm.GetAABB( aabb );
	float scale = 0.02f * aabb.GetDiagonal();

	Vertex2t vert;
	vert.diffuse = color;

	VertexIterator v; v << bm;
	for (int i = 0; i < nV; i++)
	{
		Vector3D& p = v;
		Vector3D n = v.n();
		n.normalize();
		n *= scale;

		AddVector( p, n, color );
		++v;
	}
	
	BaseMesh& tbm = GetPrimitive();
	tbm.setNVert( nV*2 );
	tbm.setNPri( nV );

	return true;
} // VectorField::CreateNormalField

bool VectorField::CreateVectorField( int nVectors )
{
	Create( nVectors * 2, 0, vf2Tex, ptLineList );
	return true;
}

bool VectorField::AddVector( const Vector3D& pos, const Vector3D& dir, DWORD color )
{
	Vertex2t vert;
	vert.diffuse = color;
	BaseMesh& bm = GetPrimitive();

	int nV = bm.getNVert();
	int nP = bm.getNPri();

	if (nV > bm.getMaxVert() - 2) return false;

	vert.x = pos.x;
	vert.y = pos.y;
	vert.z = pos.z;
	AddVertex( &vert );

	vert.x += dir.x;
	vert.y += dir.y;
	vert.z += dir.z;
	AddVertex( &vert );

	bm.setNVert( nV + 2 );
	bm.setNPri( nP + 1 );
	return true;
} // VectorField::AddVector

END_NAMESPACE( sg )


