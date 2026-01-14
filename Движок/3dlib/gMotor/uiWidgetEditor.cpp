/*****************************************************************************/
/*	File:	uiWidgetEditor.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	11-23-2003
/*****************************************************************************/
#include "stdafx.h"
#include "uiControl.h"
#include "uiNodeTree.h"
#include "uiWidgetEditor.h"
#include "kSystemDialogs.h"
BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	WidgetEditor implementation
/*****************************************************************************/
WidgetEditor::WidgetEditor()
{
	SetActive();
} // WidgetEditor::WidgetEditor

Group* WidgetEditor::CreateTemplateGroup()
{
	Group* pCreate = new Group();
	pCreate->SetName( "Create" );	

	//Group* pTarget = pCreate->AddChild<Group>( "Target" );
	//pTarget->AddChild<PLightning>			( "Lightning"  			);
	//pTarget->AddChild<PHoming>				( "Homing"  			);

	//Group* pMisc = pCreate->AddChild<Group>( "Misc" );
	//pMisc->AddChild<PMouseBind>				( "MouseBind"			);
	//pMisc->AddChild<PEffect>				( "Instance"			);

	return pCreate;
} // WidgetEditor::CreateTemplateGroup

void WidgetEditor::Expose( PropertyMap& pm )
{
	pm.start( "WidgetEditor", this );
} // WidgetEditor::Expose

void WidgetEditor::Render()
{
} // WidgetEditor::Render

bool WidgetEditor::OnChar( DWORD charCode, DWORD flags )
{
	if (IsInvisible()) return false;
	return false;
} // WidgetEditor::OnChar

END_NAMESPACE(sg)