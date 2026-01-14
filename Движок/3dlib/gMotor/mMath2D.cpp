/*****************************************************************
/*  File:   mMath2D.cpp                                          *
/*  Author: Silver, Copyright (C) GSC Game World                 *
/*  Date:   January 2002                                         *
/*****************************************************************/
#include "stdafx.h"
#include "mMath2D.h"

#ifndef _INLINES
#include "mMath2D.inl"
#endif // !_INLINES

/*****************************************************************/
/*	Rct implementation
/*****************************************************************/
const Rct Rct::unit = Rct( 0.0f, 0.0f, 1.0f, 1.0f );
const Rct Rct::null = Rct( 0.0f, 0.0f, 0.0f, 0.0f );

//-------------------------------------------------------------------------------
//  Func:  Line2D::Rasterize
//  Desc:  Performs line rasterisation with given grid step
//  Parm:	step - grid discretisation
//		  	putPixel - set pixel callback
//  Rmrk:  Bresenham algorithm is used
//-------------------------------------------------------------------------------
void Line2D::Rasterize( float step, RasterizeCallback setPixel )
{	
	if (!setPixel) return;

	int x0 = int( a.x / step );
	int y0 = int( a.y / step );
	
	int x1 = int( b.x / step );
	int y1 = int( b.y / step );

	Rasterizer rast( x0, y0, x1, y1 );
	do {
		setPixel( rast.GetCurX(), rast.GetCurY() );	
	} while ( rast.Step() );
} // Line2D::Rasterize







