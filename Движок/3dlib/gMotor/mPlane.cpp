/*****************************************************************
/*  File:   mPlane.cpp                                           
/*  Author: Silver, Copyright (C) GSC Game World                 
/*  Date:   January 2002                                         
/*****************************************************************/
#include "stdafx.h"
#include "mPlane.h"

#ifndef _INLINES
#include "mPlane.inl"
#endif // !_INLINES

Plane					Plane::xOz = Plane( 0.0f, 1.0f, 0.0f, 0.0f );
Plane					Plane::yOz = Plane( 1.0f, 0.0f, 0.0f, 0.0f );
Plane					Plane::xOy = Plane( 0.0f, 0.0f, 1.0f, 0.0f );







