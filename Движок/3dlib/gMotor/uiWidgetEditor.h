/*****************************************************************************/
/*	File:	uiWidgetEditor.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	11-23-2003
/*****************************************************************************/
#ifndef __UIWIDGETEDITOR_H__
#define __UIWIDGETEDITOR_H__

#include "ISceneEditor.h"

namespace sg{
class NodeTree;
/*****************************************************************************/
/*	Class:	WidgetEditor	
/*	Desc:	Editor of the user interface elements
/*****************************************************************************/
class WidgetEditor : public Dialog
{
public:
							WidgetEditor	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Render			();
	virtual bool			OnChar			( DWORD charCode, DWORD flags );

	NODE(WidgetEditor,Dialog,2WED);

protected:
	Group*					m_pPalette;	
	Group*					CreateTemplateGroup();
}; // class WidgetEditor

}; // namespace sg

#endif // __UIWIDGETEDITOR_H__