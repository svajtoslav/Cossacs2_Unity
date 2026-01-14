/*****************************************************************
/*  File:   mPlane.inl                                          
/*  Author: Silver, Copyright (C) GSC Game World                 
/*  Date:   January 2002                                         
/*****************************************************************/

/*****************************************************************
/*	Plane implementation
/*****************************************************************/
_inl float Plane::from3Points( const Vector3D& v1, const Vector3D& v2, const Vector3D& v3 )
{
	Vector3D av, bv;
	av.sub( v1, v2 );
	bv.sub( v3, v2 );
	Vector3D* normal = reinterpret_cast<Vector3D*>( &a );

	normal->cross( av, bv );
	float area = normal->normalize();
	d = -normal->dot( v2 );
	return area*0.5f;
} // Plane::from3Points

_inl bool Plane::ClipSegment( const Segment3D& ray, Vector3D& pt ) const
{
	float alpha = dist2Pt( ray.getOrig() );
	const Vector3D& dir = ray.getDir();
	float det = dir.x * a + dir.y * b + dir.z * c;
	if (fabs( det ) < c_SmallEpsilon) return false;
	alpha /= -det;
	if (alpha < 0.0f || alpha > 1.0f) return false;
	pt = ray.getOrig(); 
	pt.addWeighted( dir, alpha );
	return true;
} // Plane::ClipSegment

_inl Vector3D Plane::GetPoint() const
{
	if (fabs( a ) > c_SmallEpsilon) return Vector3D( -d/a, 0.0f, 0.0f );
	if (fabs( b ) > c_SmallEpsilon) return Vector3D( 0.0f, -d/b, 0.0f );
	if (fabs( c ) > c_SmallEpsilon) return Vector3D( 0.0f, 0.0f, -d/c );
	assert( false );
	return Vector3D::null;
} // Plane::GetPoint

_inl void Plane::Transform( const Matrix4D& tm )
{
	Vector3D pt = GetPoint();
	Vector3D norm = normal();
	tm.transformPt( pt );
	tm.transformVec( norm );
	fromPointNormal( pt, norm );
} // Plane::Transform

_inl bool Plane::Contains( const Vector3D& pt, float eps ) const
{
	return fabs( pt.dot( normal() ) + d ) <= eps;
} // Plane::Contains

_inl void Plane::fromPointNormal(	const Vector3D& pt, 
									const Vector3D& norm )
{
	a = norm.x;
	b = norm.y;
	c = norm.z;
	d = - pt.dot( norm );
}

_inl void Plane::fromPointNormal( const Vector4D& pt, 
								  const Vector4D& norm )
{
	a = norm.x;
	b = norm.y;
	c = norm.z;
	d = - pt.x * norm.x - pt.y * norm.y - pt.z * norm.z;
}

_inl float Plane::getZ( float x, float y ) const
{
	if (fabs( c ) <= c_SmallEpsilon) return 0.0f;
	return (-a*x - b*y - d) / c;
}

_inl float Plane::getY( float x, float z ) const
{
	if (fabs( b ) <= c_SmallEpsilon) return 0.0f;
	return (-a*x - c*z - d) / b;
}

_inl float Plane::getX( float y, float z ) const
{
	if (fabs( a ) <= c_SmallEpsilon) return 0.0f;
	return (-b*y - c*z - d) / a;
}

_inl bool Plane::isPerpendicular( const Plane& p ) const
{
	//  check the dot product of plane normals
	return fabs( a * p.a + b * p.b + c * p.c ) <= c_SmallEpsilon;
}

_inl Vector4D& Plane::asVector()
{
	return *(reinterpret_cast<Vector4D*>( &a ));
}

_inl const Vector4D& Plane::asVector() const 
{
	return *(reinterpret_cast<const Vector4D*>( &a ));
}

_inl void Plane::txtSave( FILE* fp )
{
	asVector().txtSave( fp );
}

_inl void Plane::normalize()
{
	float n = normal().norm();
	a /= n;
	b /= n;
	c /= n;
	d /= n;
} // Plane::normalize

/*---------------------------------------------------------------------------*/
/*	Func:	Plane::intersect
/*	Desc:	finds intersection point of three planes
/*	Parm:	p1 - 2nd plane
/*			p2 - 3rd plane
/*			pt - intersection point to return 
/*	Ret:	true if such point exist
/*---------------------------------------------------------------------------*/
_inl bool Plane::Intersect( const Plane& p1, const Plane& p2, Vector3D& pt ) const
{
	double A[9];
    A[0] = a;
    A[1] = p1.a;
    A[2] = p2.a;

    A[3] = b;
    A[4] = p1.b;
    A[5] = p2.b;

    A[6] = c;
    A[7] = p1.c;
    A[8] = p2.c;

    double det;
	Inverse3x3( A, det );
    const double c_DetEpsilon = 0.0000000001;
	if (fabs( det ) < c_DetEpsilon) return false;

	pt.x = -d*A[0] - p1.d*A[3] - p2.d*A[6];
	pt.y = -d*A[1] - p1.d*A[4] - p2.d*A[7];
	pt.z = -d*A[2] - p1.d*A[5] - p2.d*A[8];

	return true;
} // Plane::Intersec

_inl bool Plane::intersect( const Line3D& ray, Vector3D& pt ) const
{
	return ray.IntersectPlane( *this, pt );
} // Plane::intersect

_inl float Plane::dist2Pt( const Vector3D& v ) const
{
	return a * v.x + b * v.y + c * v.z + d;
}

_inl void Plane::MoveToPoint( const Vector3D& pt )
{
	d = -pt.dot( normal() );
} // Plane::MoveToPoint

_inl Ray3D Plane::Mirror( const Ray3D& ray ) const
{
	float t = -ray.getOrig().dot( normal() ) - d;
	Vector3D proj( ray.getOrig() );
	proj.addWeighted( normal(), 2.0f * t );

	float rt = -(ray.getOrig().dot( normal() ) + d)/(ray.getDir().dot( normal() ));
	Vector3D rdir( ray.getOrig() );
	rdir.addWeighted( ray.getDir(), rt );
	rdir -= proj;
	rdir.normalize();

	return Ray3D( proj, rdir );
} // Plane::Mirror

_inl Matrix4D Plane::ReflectionTM() const
{
	Matrix4D m;

	m.e00 = -2.0f * a * a + 1.0f; 
	m.e01 = -2.0f * b * a;
	m.e02 = -2.0f * c * a;
	m.e03 = 0.0f;

	m.e10 = -2.0f * a * b;      
	m.e11 = -2.0f * b * b + 1.0f;  
	m.e12 = -2.0f * c * b;        
	m.e13 = 0.0f;
	
	m.e20 = -2.0f * a * c;     
	m.e21 = -2.0f * b * c;      
	m.e22 = -2.0f * c * c + 1.0f;    
	m.e23 = 0.0f;
	
	m.e30 = -2.0f * a * d;      
	m.e31 = -2.0f * b * d;      
	m.e32 = -2.0f * c * d;       
	m.e33 = 1.0f;
	
	return m;
} // Plane::ReflectionTM

_inl Matrix4D Plane::ProjectionTM( const Vector4D& dir ) const
{
	Matrix4D m;
	float d = normal().dot( dir );
	m.e00 = a * dir.x + d; 
	m.e01 = a * dir.y;
	m.e02 = a * dir.z;
	m.e03 = a * dir.w;

	m.e10 = b * dir.x; 
	m.e11 = b * dir.y + d;
	m.e12 = b * dir.z;
	m.e13 = b * dir.w;

	m.e20 = c * dir.x; 
	m.e21 = c * dir.y;
	m.e22 = c * dir.z + d;  
	m.e23 = c * dir.w;

	m.e30 = d * dir.x; 
	m.e31 = d * dir.y;
	m.e32 = d * dir.z;
	m.e33 = d * dir.w + d;
	return m;
} // Plane::ProjectionTM

_inl void Plane::ProjectVec( Vector3D& vec ) const
{
	float pr = vec.dot( normal() );
	vec.x -= a * pr;
	vec.y -= b * pr;
	vec.z -= c * pr;
} // Plane::ProjectVec

_inl void Plane::ProjectPt( Vector3D& pt ) const
{
	ProjectVec( pt );
	pt.addWeighted( normal(), d );
} // Plane::ProjectPt

//  decompose onto plane normal/plane projection components
_inl void Plane::Decompose( const Vector3D& vec, Vector3D& normC, Vector3D& projC ) const
{
	float pr = vec.dot( normal() );
	normC = normal();
	normC *= pr;
	projC.sub( vec, normC );
} // Plane::Decompose

_inl bool Plane::OnPositiveSide( const Vector3D& v ) const
{
	return a*v.x + b*v.y + c*v.z + d >= 0.0f;
}





