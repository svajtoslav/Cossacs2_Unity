/*****************************************************************
/*  File:   rsVertex.cpp                                          
/*  Author: Silver, Copyright (C) GSC Game World                 
/*  Date:   January 2002                                         
/*****************************************************************/
#include "stdafx.h"
#include "rsVertex.h"

#ifndef _INLINES
#include "rsVertex.inl"
#endif // _INLINES

void* Vertex::CreateVBuf( VertexFormat vf, int numVert )
{
    return aligned_new<BYTE>( numVert*GetStride( vf ), 32 );
}

Vertex2t::Vertex2t()
{
	x		= 0.0f;
	y		= 0.0f;
	z		= 0.0f;
	diffuse	= 0xFF584E40;	

	u = u2 = v = v2 = 0.0f;
}

/*****************************************************************
/*  Vertex2t implementation                                      *
/*****************************************************************/
const Vertex2t& Vertex2t::operator =( const Vector3D& vec )
{
	x = vec.x; y = vec.y; z = vec.z;
	return *this;
}

/*****************************************************************
/*  VertexTnL implementation                                     *
/*****************************************************************/
VertexTnL::VertexTnL()
{
	x			= 0.0f;
	y			= 0.0f;
	z			= 0.01f;
	w			= 1.0f;

	diffuse		= 0xFFFFFFFF;
	specular	= 0xFFFFFFFF;

	u = v = 0.0f;
}

VertexTnL& VertexTnL::operator =( const Vector3D& vec )
{
	x = vec.x;
	y = vec.y;
	z = vec.z;
	return *this;
}

/*****************************************************************
/*  VertexN implementation                                       *
/*****************************************************************/
VertexN::VertexN()
{
	x			= 0.0f;
	y			= 0.0f;
	z			= 0.0f;
	
	nx			= 0.0f;
	ny			= 0.0f;
	nz			= 0.0f;

	u = v = 0.0f;
}

VertexN& VertexN::operator =( const Vector3D& vec )
{
	x = vec.x;
	y = vec.y;
	z = vec.z;
	return *this;
} 
