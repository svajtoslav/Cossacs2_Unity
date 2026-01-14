/*****************************************************************************/
/*	File:	sgG17.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	03.03.2003
/*****************************************************************************/
#include "stdafx.h"

#include "kHash.hpp"
#include "kResource.h"
#include "sgSpriteManager.h"
#include "sgG17.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	G17Creator implementation
/*****************************************************************************/
G17Creator::G17Creator()
{
	SpritePackage::RegisterCreator( this );
}

SpritePackage* G17Creator::CreatePackage( char* fileName, const BYTE* data )
{
	return NULL;
} // G17Creator::Load

const char*	G17Creator::Description() const
{
	return "G17 Sprite Loader";
} // G17Creator::Description


END_NAMESPACE(sg)

	
