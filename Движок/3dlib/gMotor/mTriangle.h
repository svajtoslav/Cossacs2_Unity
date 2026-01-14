/*****************************************************************************/
/*	File:	mTriangle.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	07-21-2003
/*****************************************************************************/
#ifndef __MTRIANGLE_H__
#define __MTRIANGLE_H__

/*****************************************************************************/
/*	Class:	Triangle2D
/*****************************************************************************/
class Triangle2D
{
public:

	typedef bool		(*PixelCallback)( int x, int y );

	Vector2Df			v[3];

						Triangle2D();
						Triangle2D( float x0, float y0, 
									float x1, float y1, 
									float x2, float y2 );

	void				Extrude( float amount );
	void				Rasterize( DWORD* arr, int arrW, int arrH, 
									float pixelSide, DWORD value = 0xFFFFFFFF ) const;

	void				Rasterize( float pixelSide, PixelCallback putPixel ) const;

	_inl Vector2Df		GetCenter() const;
	_inl float			Area() const;
	_inl void			GetAABB( float& x, float& y, float& w, float& h ) const;
	_inl bool			PtInside( float x, float y ) const;
	_inl bool			SameSide(	const Vector2Df& p1,
									const Vector2Df& p2,
									const Vector2Df& a,
									const Vector2Df& b ) const;
	
	_inl void			operator *=( float val );
	_inl void			operator +=( const Vector2Df& vec );
	_inl void			operator /=( float val );
	_inl void			operator -=( const Vector2Df& vec );


	Vector3D			CalcBaryCoords	( const Vector2Df& pt );
	static int			SortByXY		( const void *pV1, const void *pV2 );

	/*****************************************************************
	/*  Class:  Rasterizer 	                                             
	/*  Desc:   Rasterizes triangle
	/*  Rmrk:	Works by Bresenham's algorithm
	/*****************************************************************/
	class Rasterizer 
	{
	public:
		_inl			Rasterizer	( int x0, int y0, int x1, int y1, int x2, int y2 );
		_inl bool		Step		();
		_inl int		GetCurX		() const { return cx; }
		_inl int		GetCurY		() const { return cy; }
		_inl operator	bool		() const;

	protected:
		int					cx, cy;
		Line2D::Rasterizer	r0, r1, r2;
	}; // class Rasterizer 

}; // class Triangle2D

Vector3D BaryCoords(	float ax, float ay, 
						float bx, float by, 
						float cx, float cy,
						float ptX, float ptY );

bool BaryCoords( float ax, float ay, float bx, float by, float cx, float cy,
                 float ptX, float ptY, Vector3D& res );

bool BaryCoords(    double ax, double ay, 
                    double bx, double by, 
                    double cx, double cy,
                    double ptX, double ptY, 
                    double& bcX, double& bcY, double& bcZ );

#ifdef _INLINES
#include "mTriangle.inl"
#endif // _INLINES

#endif // __MTRIANGLE_H__