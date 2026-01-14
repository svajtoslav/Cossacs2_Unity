/*****************************************************************
/*  File:   Math2D.inl                                           *
/*  Author: Silver, Copyright (C) GSC Game World                 *
/*  Date:   January 2002                                         *
/*****************************************************************/
#include "mUtil.h"
#include <math.h>

/*****************************************************************
/*	Rct implementation
/*****************************************************************/
_inl Rct::Rct( float _x, float _y, float _w, float _h )
{
	x = _x;
	y = _y;
	w = _w;
	h = _h;
	if (w < 0.0f) { x += w; w = -w; }
	if (h < 0.0f) { y += h; h = -h; }
} // Rct::Rct

_inl float Rct::GetAspect() const
{
	if (fabs( w ) < c_SmallEpsilon && fabs( h ) < c_SmallEpsilon) return 1.0f;
     return w / h;
}

_inl float Rct::GetRight() const
{
	return x + w;
}

_inl float Rct::GetBottom() const
{
	return y + h;
}

_inl float Rct::GetCenterX() const
{
	return x + w*0.5f;
}

_inl float Rct::GetCenterY() const
{
	return y + h*0.5f;
}

_inl float Rct::MaxSide()	const
{
	return w > h ? w : h;
}

_inl bool Rct::PtIn( float pX, float pY ) const
{
	return  (pX >= x) && (pX <= GetRight()) &&
			(pY >= y) && (pY <= GetBottom()); 
}

_inl bool Rct::PtInStrict( float pX, float pY ) const
{
	return  (pX > x) && (pX < GetRight()) && (pY > y) && (pY < GetBottom()); 
}

_inl void Rct::Copy( const Rct& orig )
{
	x = orig.x;
	y = orig.y;
	h = orig.h;
	w = orig.w;
}

_inl void Rct::operator *=( float val )
{
	x *= val;
	y *= val;
	w *= val;
	h *= val;
}

_inl void Rct::operator /=( float val )
{
	float iv = 1.0f / val;
	x *= iv;
	y *= iv;
	w *= iv;
	h *= iv;
}

_inl void Rct::operator +=( const Vector2Df& delta )
{
	x += delta.x;
	y += delta.y;
}

_inl void Rct::operator +=( const Rct& delta )
{
	x += delta.x;
	y += delta.y;
	w += delta.w;
	h += delta.h;
}

_inl void Rct::Set( float _x, float _y, float _w, float _h )
{
	x = _x;
	y = _y;
	w = _w;
	h = _h;
}

_inl bool Rct::IsOutside( const Rct& rct ) const
{
	return	(GetRight() < rct.x)	|| 
			(x > rct.GetRight())	||
			(GetBottom() < rct.y)	||
			(y > rct.GetBottom());
} // Rct::IsOutside

_inl bool Rct::Overlap( const Rct& rct ) const
{
	return !IsOutside( rct ) && !rct.IsOutside( *this );

} // Rct::IsOutside

//  fits this rectangle into the rct, constraining the proportion
_inl void  Rct::FitInto( const Rct& rct )
{
     float asp = GetAspect();
     if (rct.GetAspect() > asp)
     {
         h = rct.h;
         w = h * asp;
         x = rct.x + (rct.w - w) / 2.0f;
         y = rct.y;
     }
     else
     {
         w = rct.w;
         h = w / asp;
         x = rct.x;
         y = rct.y + (rct.h - h) / 2.0f;
     }
}


_inl void Rct::CenterInto( const Rct& rct )
{
	x = rct.x + (rct.w - w)*0.5f;
	y = rct.y + (rct.h - h)*0.5f;
}

_inl void Rct::Deflate( float amt )
{
	x -= amt;
	y -= amt;
	w += amt * 2.0f;
	h += amt * 2.0f;
} // Rct::Deflate

_inl void Rct::Inflate( float top, float right, float bottom, float left )
{
	x += left;
	y += top;
	w -= left + right;
	h -= top + bottom;
} // Rct::Inflate

_inl void Rct::Inflate( float val )
{
	x -= val;
	y -= val;
	w += val * 2.0f;
	h += val * 2.0f;
} // Rct::Inflate

//-------------------------------------------------------------------------------
//  Func:  Rct::ClipSegment
//  Desc:  Clips line segment against rectangle
//  Parm:  a, b - segment endpoints, clipped values are returned there too
//  Ret:   false when segment is totally outside, true when it was clipped	
//  Rmrk:  Cohen-Sutherland algorithm was used
//-------------------------------------------------------------------------------
_inl bool Rct::ClipSegment( Vector2Df& a, Vector2Df& b ) const
{
	float xmax = GetRight();
	float ymax = GetBottom();

	//  fill endpoints status flag bytes
	BYTE stA = 0, stB = 0;
	if (a.y > ymax) stA |= 0x1;
	if (a.y < y)	stA |= 0x2;
	if (a.x > xmax) stA |= 0x4;
	if (a.x < x)	stA |= 0x8;

	if (b.y > ymax) stB |= 0x1;
	if (b.y < y)	stB |= 0x2;
	if (b.x > xmax) stB |= 0x4;
	if (b.x < x)	stB |= 0x8;

	if ((stA & stB) != 0) 
	{
		return false;
	}

	if (stA == 0) 
	{
		if (stB == 0) return true;
		//	point b is outside rectangle
		if (stB & 0x1)
		//  intersection with ymax
		{
			b.x = a.x + (ymax - a.y)*(b.x - a.x)/(b.y - a.y);
			b.y = ymax;
			return ClipSegment( a, b );
		}

		if (stB & 0x2)
			//  intersection with ymin
		{
			b.x = a.x + (y - a.y)*(b.x - a.x)/(b.y - a.y);
			b.y = y;
			return ClipSegment( a, b );
		}

		if (stB & 0x4)
			//  intersection with xmax
		{
			b.y = a.y + (xmax - a.x)*(b.y - a.y)/(b.x - a.x);
			b.x = xmax;
			return ClipSegment( a, b );
		}

		if (stB & 0x8)
			//  intersection with xmin
		{
			b.y = a.y + (x - a.x)*(b.y - a.y)/(b.x - a.x);
			b.x = x;
			return ClipSegment( a, b );
		}
	}
	else
	{
		if (stA & 0x1)
			//  intersection with ymax
		{
			a.x = b.x + (ymax - b.y)*(a.x - b.x)/(a.y - b.y);
			a.y = ymax;
			return ClipSegment( a, b );
		}

		if (stA & 0x2)
			//  intersection with ymin
		{
			a.x = b.x + (y - b.y)*(a.x - b.x)/(a.y - b.y);
			a.y = y;
			return ClipSegment( a, b );
		}

		if (stA & 0x4)
			//  intersection with xmax
		{
			a.y = b.y + (xmax - b.x)*(a.y - b.y)/(a.x - b.x);
			a.x = xmax;
			return ClipSegment( a, b );
		}

		if (stA & 0x8)
			//  intersection with xmin
		{
			a.y = b.y + (x - b.x)*(a.y - b.y)/(a.x - b.x);
			a.x = x;
			return ClipSegment( a, b );
		}
	}
	
	assert( false );
	return true;
} // Rct::ClipSegment

_inl float Rct::Dist2ToPt( float pX, float pY )	const
{
	float x1 = GetRight();
	float y1 = GetBottom();
	float dx = pX < x ? (x - pX) : (pX > x1 ? (pX - x1) : 0.0f);
	float dy = pY < y ? (y - pY) : (pY > y1 ? (pY - y1) : 0.0f);
	return dx*dx + dy*dy;
} // Rct::Dist2ToPt

_inl void Rct::Zero()
{
	x = y = w = h = 0.0f; 
}

_inl void Rct::SetPositiveDimensions()
{
	if (w < 0.0f)
	{
		x += w;
		w = -w;
	}

	if (h < 0.0f)
	{
		y += h;
		h = -h;
	}
} // Rct::SetPositiveDimensions

_inl bool Rct::ClipHLine( float& px1, float& px2, float py ) const
{
	if (py < y || py >= y + h) return false; 
	px1 = px1 < x ? x : px1;
	px2 = px2 < x ? x : px2;

	int vpRight  = x + w - 1;

	px1 = px1 > vpRight ? vpRight : px1;
	px2 = px2 > vpRight ? vpRight : px2;
	return (px1 != px2);
} // Rct::ClipHLine

_inl bool Rct::IsRectInside( float px, float py, float pw, float ph ) const
{
	return (px >= x && px + pw < x + w && py >= y && py + ph < y + h);
}

_inl bool Rct::ClipVLine( float px, float& py1, float& py2 ) const
{
	if (px < x || px >= x + w) return false;
	py1 = py1 < y ? y : py1;
	py2 = py2 < y ? y : py2;

	int vpBottom  = y + h - 1;

	py1 = py1 > vpBottom ? vpBottom : py1;
	py2 = py2 > vpBottom ? vpBottom : py2;
	return (py1 != py2);
} // Rct::ClipVLine

_inl bool Rct::Clip( Rct& rct ) const
{
	int px1 = rct.x + rct.w;
	int py1 = rct.y + rct.h;

	if (rct.x < int( x ))
	{
		rct.x = x;
	}

	if (rct.y < int( y ))
	{
		rct.y = y;
	}

	int vpRight  = x + w;
	int vpBottom = y + h;
	if (px1 > vpRight)  
	{
		px1 = vpRight;
	}

	if (py1 > vpBottom) 
	{
		py1 = vpBottom;
	}

	rct.w = px1 - rct.x;
	rct.h = py1 - rct.y;

	if (rct.w < 0) rct.w = 0;
	if (rct.h < 0) rct.h = 0;

	return ((rct.w > 0) && (rct.h > 0));
} // Rct::ClipRect

_inl void Rct::Union( const Rct& rct )
{
	x = tmin( x, rct.x );
	y = tmin( y, rct.y );
	w = tmax( w, rct.w, rct.x + rct.w - x, x + w - rct.x );
	h = tmax( h, rct.h, rct.y + rct.h - y, y + h - rct.y );
} // Rct::Union

/*****************************************************************
/*	Line2D implementation
/*****************************************************************/
_inl Line2D::Line2D( const Vector2Df& v1, const Vector2Df& v2 )
{
	a.copy( v1 );
	b.copy( v2 );
}

_inl Line2D::Line2D( float ax, float ay, float bx, float by )
{
	a.x = ax; a.y = ay; 
	b.x = bx; b.y = by; 
}

_inl void Line2D::Init( const Vector2Df& v1, const Vector2Df& v2 )
{
	a.copy( v1 );
	b.copy( v2 );
}

_inl bool Line2D::OneSide( const Vector2Df& v1, const Vector2Df& v2 ) const
{
	return ( ((v1.x - a.x)*(b.y - a.y) - 
		(v1.y - a.y)*(b.x - a.x)) * 
		((v2.x - a.x)*(b.y - a.y) - 
		(v2.y - a.y)*(b.x - a.x))
		) >= 0; 
}

_inl bool Line2D::InLeftHalf( const Vector2Df& pt ) const
{
	return ((pt.x - a.x)*(b.y - a.y) - (pt.y - a.y)*(b.x - a.x)) <= 0;
}

_inl bool Line2D::InLeftHalf( float x, float y ) const
{
	return ((x - a.x)*(b.y - a.y) - (y - a.y)*(b.x - a.x)) <= 0;
}

/*****************************************************************
/*	Line2D::Rasterizer implementation
/*****************************************************************/
_inl Line2D::Rasterizer::Rasterizer( int x0, int y0, int x1, int y1 )
{
	dy = y1 - y0;
	dx = x1 - x0;
	if (dy < 0) { dy = -dy;  stepy = -1; } else { stepy = 1; }
	if (dx < 0) { dx = -dx;  stepx = -1; } else { stepx = 1; }
	dy *= 2;                                                  
	dx *= 2;                                                 
	if (dx > dy) 
	{ 
		fraction = dy - (dx / 2);                         
	} 
	else 
	{
		fraction = dx - (dy / 2);
	}
	cx = x0; cy = y0;
	endX = x1; endY = y1;
} // Line2D::Rasterizer::Rasterizer

_inl bool Line2D::Rasterizer::Step()
{
	if (cx == endX && cy == endY) return false;
	
	if (dx > dy)
	{
		if (fraction > 0) 
		{
			cy += stepy;
			fraction -= dx;                                 
		}
		cx += stepx;
		fraction += dy;        
	}
	else
	{
		if (fraction > 0) 
		{
			cx += stepx;
			fraction -= dy;
		} 
		cy += stepy;
		fraction += dx;
	}
	return true;
}// Line2D::Rasterizer::Step

_inl Line2D::Rasterizer::operator bool() const
{
	return (cx < endX || cy < endY);
}


