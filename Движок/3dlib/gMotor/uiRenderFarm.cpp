/*****************************************************************************/
/*	File:	uiRenderFarm.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-12-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"
#include "uiControl.h"
#include "uiRenderFarm.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	RenderFarm implementation
/*****************************************************************************/
RenderFarm::RenderFarm()
{
}

void RenderFarm::Render()
{

} // RenderFarm::Render

void RenderFarm::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "RenderFarm", this );
} // RenderFarm::Expose


END_NAMESPACE(sg)

