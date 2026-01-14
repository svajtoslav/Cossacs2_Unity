/*****************************************************************************/
/*	File:	mTriangle.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	07-21-2003
/*****************************************************************************/

/*****************************************************************************/	
/*	Triangle2D implementation
/*****************************************************************************/
_inl Vector2Df Triangle2D::GetCenter() const
{
	return Vector2Df(	(v[0].x + v[1].x + v[2].x)/3.0f, 
						(v[0].y + v[1].y + v[2].y)/3.0f );
}

_inl void Triangle2D::GetAABB( float& x, float& y, float& w, float& h ) const
{
	x = tmin( v[0].x, v[1].x, v[2].x );
	y = tmin( v[0].y, v[1].y, v[2].y );
	w = tmax( v[0].x, v[1].x, v[2].x ) - x;
	h = tmax( v[0].y, v[1].y, v[2].y ) - y;
} // Triangle2D::GetAABB

_inl float Triangle2D::Area() const
{
	return 0.5f*fabs(	v[0].x * (v[2].y - v[1].y) +
						v[1].x * (v[0].y - v[2].y) +
						v[2].x * (v[1].y - v[0].y) ); 
} // Triangle2D::Area

_inl void Triangle2D::operator *=( float val )
{
	v[0] *= val;
	v[1] *= val;
	v[2] *= val;
}

_inl void Triangle2D::operator +=( const Vector2Df& vec )
{
	v[0] += vec;
	v[1] += vec;
	v[2] += vec;
}

_inl void Triangle2D::operator /=( float val )
{
	float ival = 1.0f / val;
	v[0] *= ival;
	v[1] *= ival;
	v[2] *= ival;
}

_inl void Triangle2D::operator -=( const Vector2Df& vec )
{
	v[0] -= vec;
	v[1] -= vec;
	v[2] -= vec;
}

/*****************************************************************
/*	Triangle2D::Rasterizer implementation
/*****************************************************************/
_inl Triangle2D::Rasterizer::Rasterizer( int x0, int y0, int x1, int y1, int x2, int y2 ) : 
	r0( x0, y0, x1, y1 ), r1( x0, y0, x2, y2 ), r2( x1, y1, x2, y2 )
{
	
} // Triangle2D::Rasterizer::Rasterizer

_inl bool Triangle2D::Rasterizer::Step()
{
	return true;
}// Triangle2D::Rasterizer::Step

_inl Triangle2D::Rasterizer::operator bool() const
{
	return false;
} // Triangle2D::bool



