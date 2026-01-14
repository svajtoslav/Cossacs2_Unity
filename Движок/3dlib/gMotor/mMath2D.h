/*****************************************************************
/*  File:   Math2D.h	                                         
/*	Desc:	
/*  Author: Silver, Copyright (C) GSC Game World                 
/*  Date:   January 2002                                         
/*****************************************************************/
#ifndef __GPMATH2D_H__
#define __GPMATH2D_H__
#pragma	once

/*****************************************************************
/*  Class:  Matrix2D                                             *
/*  Desc:   2-dimensional matrix							     *
/*****************************************************************/
template <class T>
class Matrix2D 			
{
public:
	T				e00;
	T				e10;
	T				e01;
	T				e11;

	void			Identity() { e00 = 1; e11 = 1; e10 = 0; e01 = 0; }
	void			FromAngle( float ang );
}; // class Matrix2D 

/*****************************************************************
/*  Class:  Vector2D                                             *
/*  Desc:   2-dimensional vector							     *
/*****************************************************************/
template <class T>
class Vector2D 			
{
public:
	T				x;
	T				y;

	Vector2D();
	Vector2D( const T _x, const T _y );

	_inl void set( T _x, T _y );
	_inl void sub( const Vector2D& v1, const Vector2D& v2 );
	_inl void sub( const Vector2D<T>& v );
	_inl float dot( const Vector2D<T>& v ) const;

	_inl void copy( const Vector2D& orig );

	_inl T norm() const;
	_inl T norm2() const;
	_inl T dist2( Vector2D<T> r ) const;

	_inl bool inAAQuad( T _x, T _y, T _side ) const;
	_inl void normalize();
	_inl bool InNeighborhood( const Vector2D& v )
	{
		return	(fabs( v.x - x ) <= c_SmallEpsilon) && 
			(fabs( v.y - y ) <= c_SmallEpsilon);
	}


	_inl const Vector2D& operator +=( const Vector2D& vec );
	_inl const Vector2D& operator -=( const Vector2D& vec );
	_inl const Vector2D& operator *=( const T val );
	_inl const Vector2D& operator *=( const Matrix2D<T>& m );
	_inl const Vector2D& operator /=( const T val );

	_inl T	triArea( const Vector2D<T>& v );

	_inl void clamp( T minX, T minY, T maxX, T maxY );

	void			Dump();

	class CmpLeftTop
	{
	public:
		_inl static bool Less( const Vector2D& a, const Vector2D& b )
		{
			if (a.x < b.x) return true;
			if (a.x == b.x) return (a.y < b.y);
			return false;
		}
	};
}; // class Vector2D

typedef Vector2D<float> 	Vector2Df;
typedef Vector2D<int>		Vector2Di;
typedef Matrix2D<float> 	Matrix2Df;
typedef Matrix2D<int>		Matrix2Di;

/*****************************************************************
/*  Class:  Line2D 	                                             
/*  Desc:   2D line											     
/*****************************************************************/
class Line2D 	
{
public:
	typedef  bool		(*RasterizeCallback)( int x, int y );


	_inl				Line2D		( const Vector2Df& v1, const Vector2Df& v2 );
	_inl				Line2D		( float ax, float ay, float bx, float by ); 

	_inl				Line2D		(){}

	_inl void			Init		( const Vector2Df& v1, const Vector2Df& v2 );
	_inl bool			OneSide		( const Vector2Df& v1, const Vector2Df& v2 ) const;
	_inl bool			InLeftHalf	( const Vector2Df& pt ) const;
	_inl bool			InLeftHalf	( float x, float y ) const;
	void				Rasterize	( float step, RasterizeCallback putPixel );

	Vector2Df			a, b;

	/*****************************************************************
	/*  Class:  Rasterizer 	                                             
	/*  Desc:   Little cute device for rasterizing lines
	/*  Rmrk:	Works by Bresenham's algorithm
	/*****************************************************************/
	class Rasterizer 
	{
	public:
		_inl			Rasterizer	( int x0, int y0, int x1, int y1 );
		_inl bool		Step		();
		_inl operator	bool		() const;
		_inl int		GetCurX		() const { return cx; }
		_inl int		GetCurY		() const { return cy; }

	protected:
		int				endX, endY;
		int				dx, dy;
		int				cx, cy;
		int				stepx, stepy;
		int				fraction;

		friend class	Line2D;
	}; // class Rasterizer 

}; // class Line2D

typedef Line2D				Line2Df;

/*****************************************************************
/*  Class:  Rct                                                 
/*  Desc:   2D rectangular area                                   
/*****************************************************************/
class Rct
{
public:
	Rct()											: x(0), y(0), w(0), h(0)			{}
	Rct( float _w, float _h )						: x(0.0f), y(0.0f), w(_w), h(_h)	{}
	Rct( float _x, float _y, float _side )			: x(_x), y(_y), w(_side), h(_side)	{}

	_inl Rct( float _x, float _y, float _w, float _h );

	_inl float		GetAspect	()							const;
	_inl float		MaxSide		()							const;
	_inl bool		PtIn		( float pX, float pY )		const;
	_inl bool		PtInStrict	( float pX, float pY )		const;

	_inl float		GetCenterX	()							const;
	_inl float		GetCenterY	()							const;
	_inl float		Dist2ToPt	( float pX, float pY )		const;

	_inl void		Copy		( const Rct& orig );
	_inl void		Deflate		( float amt );
	_inl void		Inflate		( float top, float right, float bottom, float left );
	_inl void		Inflate		( float val );

	_inl void		FitInto		( const Rct& rct );
	_inl void		CenterInto	( const Rct& rct );
	
	_inl void		Set			( float _x, float _y, float _w, float _h );
	_inl void		Zero		();
	_inl bool		Overlap		( const Rct& rct ) const;
	_inl bool		IsOutside	( const Rct& rct ) const;
	_inl void		Union		( const Rct& rct );

	_inl float		GetRight	() const;
	_inl float		GetBottom	() const;
	_inl bool		ClipSegment	( Vector2Df& a, Vector2Df& b ) const;
	_inl void		SetPositiveDimensions();

	_inl bool 		ClipHLine	( float& px1, float& px2, float py ) const;
	_inl bool 		ClipVLine	( float px, float& py1, float& py2 ) const;
	_inl bool 		Clip		( Rct& rct ) const;
	_inl bool		IsRectInside( float px, float py, float pw, float ph ) const;


	_inl void		operator *=( float val );
	_inl void		operator /=( float val );
	_inl void		operator +=( const Vector2Df& delta );
	_inl void		operator +=( const Rct& delta );
	
	float				x, y, w, h;

	static const Rct	unit;
	static const Rct	null;
};  // class Rct

#include "mMath2D.hpp"

#ifdef _INLINES
#include "mMath2D.inl"
#endif // _INLINES

#endif // __GPMATH2D_H__