/*****************************************************************************/
/*	File:	uiWidget.cpp
/*	Desc:	Brand-new ui
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-20-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"
#include "uiWidget.h"
BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	Widget implementation
/*****************************************************************************/
void Widget::Render()
{
	if (DoDrawGizmo())
	{

	}
} // Widget::Render

void Widget::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Widget", this );
} // Widget::Expose

void Widget::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // Widget::Serialize

void Widget::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
} // Widget::Unserialize

END_NAMESPACE(sg)
