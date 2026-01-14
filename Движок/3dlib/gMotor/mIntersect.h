/*****************************************************************************/
/*	File:	mIntersect.h
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	07-10-2003
/*****************************************************************************/
#ifndef __MINTERSECT_H__
#define __MINTERSECT_H__

bool AABBTriangleX( const AABoundBox& aabb, 
					const Vector3D& v0,
					const Vector3D& v1,
					const Vector3D& v2 );

bool AABBRayX(	const AABoundBox& aabb, 
				const Line3D& ray, Vector3D& point );

bool RayTriangleX(	const Line3D& ray, 
					const Vector3D&	v0, 
					const Vector3D&	v1, 
					const Vector3D&	v2, 
					float& u, float& v, 
					float& t );

bool TriTri2DX( const Vector2Df& a0, const Vector2Df& a1, const Vector2Df& a2, 
			   const Vector2Df& b0, const Vector2Df& b1, const Vector2Df& b2 );

bool RayTriangleX( const Vector3D& v1, const Vector3D& v2, const Vector3D& v3, 
				  const Vector3D& org, const Vector3D& dir, 
				  Vector3D& xpt );

#endif // __MINTERSECT_H__