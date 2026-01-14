/*****************************************************************************/
/*	File:	sgGeometry.inl
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Geometry	implementation
/*****************************************************************************/
_inl int Geometry::AddVertex( void* pVert )
{
	int nV = m_Mesh.getNVert();
	assert( nV < m_Mesh.getMaxVert() );
	int stride = Vertex::GetStride( m_Mesh.getVertexFormat() );
	memcpy( m_Mesh.getVertexData() + stride * nV, pVert, stride );
	m_Mesh.setNVert( nV + 1 );
	return nV + 1;
} // Geometry::AddVertex

_inl int Geometry::AddPoly( WORD v1, WORD v2, WORD v3 )
{
	int nI = m_Mesh.getNInd();
	assert( nI + 3 <= m_Mesh.getMaxInd() );
	WORD* pIdx = m_Mesh.getIndices() + nI;
	pIdx[0] = v1; pIdx[1] = v2; pIdx[2] = v3;
	m_Mesh.setNInd( nI + 3 );
	m_Mesh.setNPri( m_Mesh.getNPri() + 1 );
	return nI + 3;
} // Geometry::AddPoly

_inl bool Geometry::AddQuad( const Vector3D& a, const Vector3D& b,
							 const Vector3D& c, const Vector3D& d,
							 const Rct* pUV )
{
	int nV = m_Mesh.getNVert();
	int nI = m_Mesh.getNInd();
	WORD* pIdx = m_Mesh.getIndices() + nI;
	
	if (pIdx && nI + 6 >= m_Mesh.getMaxInd())	return false;
	if (nV + 4 > m_Mesh.getMaxVert())			return false;

	int stride = Vertex::GetStride( m_Mesh.getVertexFormat() );
	BYTE* pVert = m_Mesh.getVertexData() + stride * nV;

	*((Vector3D*)pVert) = a; pVert += stride;
	*((Vector3D*)pVert) = b; pVert += stride;
	*((Vector3D*)pVert) = c; pVert += stride;
	*((Vector3D*)pVert) = d; 

	if (pIdx)
	{
		pIdx[0] = nV;		pIdx[1] = nV + 1; pIdx[2] = nV + 2;
		pIdx[3] = nV + 2;	pIdx[4] = nV + 1; pIdx[5] = nV + 3;
	}

	if (pUV)
	{
		VertexIterator it;
		it << GetPrimitive();
		it.u( nV	 ) = pUV->x;			it.v( nV	 ) = pUV->y;
		it.u( nV + 1 ) = pUV->GetRight();	it.v( nV + 1 ) = pUV->y;
		it.u( nV + 2 ) = pUV->x;			it.v( nV + 2 ) = pUV->GetBottom();
		it.u( nV + 3 ) = pUV->GetRight();	it.v( nV + 3 ) = pUV->GetBottom();
	}

	m_Mesh.setNVert( nV + 4 );
	m_Mesh.setNPri ( m_Mesh.getNPri() + 2 );
	return true;
} // Geometry::AddQuad

/*****************************************************************************/
/*	MorphedGeometry	implementation
/*****************************************************************************/
_inl void MorphedGeometry::ProcessGeometry()
{
	if (s_bFrozen || m_bDisableMorphing) return;
	OnProcessGeometry();
} // MorphedGeometry::ProcessGeometry

_inl void MorphedGeometry::RenderMorphedGeometry()
{
	ProcessGeometry();
	IRS->ResetWorldMatrix();
	IRS->DrawPrim( GetPrimitive() );
} // MorphedGeometry::RenderMorphedGeometry

/*****************************************************************************/
/*	SkinnedGeometry	implementation
/*****************************************************************************/
_inl const Matrix4D& SkinnedGeometry::GetMatrix( int idx )
{
	TransformNode* pNode = (TransformNode*)GetChild( idx );
	return pNode->GetTopTM();
} // SkinnedGeometry::GetMatrix

/*****************************************************************************/
/*	GeometryRef	implementation
/*****************************************************************************/
_inl GeometryRef::GeometryRef() : pool(NULL)
{
}

END_NAMESPACE( sg )
