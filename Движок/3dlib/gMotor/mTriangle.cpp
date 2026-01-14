/*****************************************************************************/
/*	File:	mTriangle.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	07-21-2003
/*****************************************************************************/
#include "stdafx.h"
#include "mTriangle.h"

#ifndef _INLINES
#include "mTriangle.inl"
#endif // _INLINES

/*****************************************************************************/	
/*	Triangle2D implementation
/*****************************************************************************/
Triangle2D::Triangle2D()
{}

Triangle2D::Triangle2D( float x0, float y0, 
						float x1, float y1, 
						float x2, float y2 )
{
	v[0].x = x0; v[0].y = y0;
	v[1].x = x1; v[1].y = y1;
	v[2].x = x2; v[2].y = y2;
}

void Triangle2D::Extrude( float amount )
{
	Vector2Df c = GetCenter();
	for (int i = 0; i < 3; i++)
	{
		Vector2Df d( v[i] );
		d -= c;
		d.normalize();
		d *= amount;
		v[i] += d;
	}
} // Triangle2D::Extrude

int Triangle2D::SortByXY( const void *pV1, const void *pV2 )
{
	Vector2Df* v1 = (Vector2Df*)pV1;
	Vector2Df* v2 = (Vector2Df*)pV2;

	if (v1->x < v2->x) return -1;
	if (v1->x > v2->x) return 1;
	if (v1->y < v2->y) return -1;
	if (v1->y > v2->y) return 1;
	return 0;
} // Triangle2D::SortByXY

void Triangle2D::Rasterize( DWORD* arr, int arrW, int arrH, float pixelSide, DWORD value ) const
{	
	Vector2Df nv[3];
	for (int i = 0; i < 3; i++)
	{
		nv[i].x = v[i].x / pixelSide;
		nv[i].y = v[i].y / pixelSide;
	}

	Vector2Df va, vb;
	va.sub( nv[2], nv[0] );
	vb.sub( nv[1], nv[0] );

	float aLen = va.norm();
	float bLen = vb.norm();
	
	Vector2Df c;
	float step = 1.0f / tmax( aLen, bLen ) / 2.0f;

	for (float ci = 0.0f; ci <= 1.0f; ci += step )
	{
		for (float cj = 0.0f; cj <= 1.0f - ci; cj += step)
		{
			c.x = nv[0].x + ci * va.x + cj * vb.x;
			c.y = nv[0].y + ci * va.y + cj * vb.y;
			c.clamp( 0, 0, arrW - 1, arrH - 1 );
			arr[int( c.x ) + int( c.y )*arrW] = value;
		}
	}

} // Triangle2D::Rasterize

void Triangle2D::Rasterize( float pixelSide, Triangle2D::PixelCallback putPixel ) const
{	
	Vector2Df nv[3];
	for (int i = 0; i < 3; i++)
	{
		nv[i].x = v[i].x / pixelSide;
		nv[i].y = v[i].y / pixelSide;
	}

	//  sort vertices left-to right, top-to bottom
	Sort<Vector2Df::CmpLeftTop>( nv[0], nv[1], nv[2] );
	
	Line2D::Rasterizer r1( nv[0].x, nv[0].y, nv[1].x, nv[1].y );
	Line2D::Rasterizer r2( nv[1].x, nv[1].y, nv[2].x, nv[2].y );
	Line2D::Rasterizer r3( nv[0].x, nv[0].y, nv[2].x, nv[2].y );
	
	Line2D::Rasterizer* pCr = &r1;
	putPixel( r3.GetCurX(), r3.GetCurY() );

	while (r3.Step())
	{
		while (pCr->GetCurX() < r3.GetCurX())
		{
			if (pCr->Step()) continue;			
			pCr = &r2;
		}

		int begY, endY;
		begY = pCr->GetCurY();
		endY = r3.GetCurY();
		if (pCr->GetCurY() > r3.GetCurY()) Swap( begY, endY );
		for (int i = begY; i <= endY; i++) if (!putPixel( r3.GetCurX(), i )) return;
	}

} // Triangle2D::Rasterize

Vector3D Triangle2D::CalcBaryCoords( const Vector2Df& pt )
{
	return BaryCoords( v[0].x, v[0].y, v[1].x, v[1].y, v[2].x, v[2].y, pt.x, pt.y );
} // Triangle2D::CalcBaryCoords

Vector3D BaryCoords( float ax, float ay, float bx, float by, float cx, float cy,
						float ptX, float ptY )
{
	float acx	= ax - cx;
	float acy	= ay - cy;
	float bcx	= bx - cx;
	float bcy	= by - cy;
	float pcx	= ptX - cx;
	float pcy	= ptY - cy;
	float m00 	= acx*acx + acy*acy;
	float m01 	= acx*bcx + acy*bcy;
	float m11 	= bcx*bcx + bcy*bcy;
	float r0  	= acx*pcx + acy*pcy;
	float r1  	= bcx*pcx + bcy*pcy;
	float det 	= m00 * m11 - m01 * m01;
	assert( fabs( det ) > 0.0f );
	float invDet = 1.0f / det;

	Vector3D res;
	res.x = (m11 * r0 - m01 * r1) * invDet;
	res.y = (m00 * r1 - m01 * r0) * invDet;
	res.z = 1.0f - res.x - res.y;
	return res;
} // BaryCoords

bool BaryCoords( float ax, float ay, float bx, float by, float cx, float cy,
                    float ptX, float ptY, Vector3D& res )
{
    float acx	= ax - cx;
    float acy	= ay - cy;
    float bcx	= bx - cx;
    float bcy	= by - cy;
    float pcx	= ptX - cx;
    float pcy	= ptY - cy;
    float m00 	= acx*acx + acy*acy;
    float m01 	= acx*bcx + acy*bcy;
    float m11 	= bcx*bcx + bcy*bcy;
    float r0  	= acx*pcx + acy*pcy;
    float r1  	= bcx*pcx + bcy*pcy;
    float det 	= m00 * m11 - m01 * m01;
    if (fabs( det ) < c_SmallEpsilon) return false;
    float invDet = 1.0f / det;

    res.x = (m11 * r0 - m01 * r1) * invDet;
    res.y = (m00 * r1 - m01 * r0) * invDet;
    res.z = 1.0f - res.x - res.y;
    return true;
} // BaryCoords

double c_DoubleEpsilon = 1.0e-16;
bool BaryCoords(    double ax, double ay, 
                    double bx, double by, 
                    double cx, double cy,
                    double ptX, double ptY, 
                    double& bcX, double& bcY, double& bcZ )
{
    double acx	= ax - cx;
    double acy	= ay - cy;
    double bcx	= bx - cx;
    double bcy	= by - cy;
    double pcx	= ptX - cx;
    double pcy	= ptY - cy;
    double m00 	= acx*acx + acy*acy;
    double m01 	= acx*bcx + acy*bcy;
    double m11 	= bcx*bcx + bcy*bcy;
    double r0  	= acx*pcx + acy*pcy;
    double r1  	= bcx*pcx + bcy*pcy;
    double det 	= m00 * m11 - m01 * m01;
    if (fabs( det ) < c_DoubleEpsilon) return false;
    double invDet = 1.0 / det;

    bcX = (m11 * r0 - m01 * r1) * invDet;
    bcY = (m00 * r1 - m01 * r0) * invDet;
    bcZ = 1.0 - bcX - bcY;
    return true;
} // BaryCoords



