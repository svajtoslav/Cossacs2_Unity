/*****************************************************************************/
/*	File:	uiPalette.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "uiControl.h"
#include "sgFont.h"
#include "sgController.h"

#include "uiWidget.h"
#include "uiPalette.h"

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	Palette implementation
/*****************************************************************************/
Palette::Palette()
{
}

void Palette::Render()
{
	
	Parent::Render();
} // Palette::Render

void Palette::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Palette", this );
} // Palette::Expose

END_NAMESPACE(sg)
