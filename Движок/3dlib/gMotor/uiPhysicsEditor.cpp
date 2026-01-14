/*****************************************************************************/
/*	File:	uiPhysicsEditor.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	11-23-2003
/*****************************************************************************/
#include "stdafx.h"
#include "uiControl.h"
#include "uiNodeTree.h"
#include "uiPhysicsEditor.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	PhysicsEditor implementation
/*****************************************************************************/
//Group* CreatePhysicsTemplates();
PhysicsEditor::PhysicsEditor()
{
	//m_pPalette = CreatePhysicsTemplates();
	m_pPalette = new Group();
	m_pPalette->SetInvisible();

	m_ClrTop		= 0x2DFFFFFF;
	m_ClrMdl		= 0x2DD6D3CE;
	m_ClrBot		= 0x2D848284;
	FInStream is( "Models\\PhysicsEditor.c2m" );
	m_pBackScene = Node::UnserializeSubtree( is );

	SetActive();
} // PhysicsEditor::PhysicsEditor

Node* PhysicsEditor::GetPhysicsRoot()
{ 
	return this; 
}

void PhysicsEditor::Play()
{
} // PhysicsEditor::Play

void PhysicsEditor::Stop()
{
} // PhysicsEditor::Stop

void PhysicsEditor::Reset()
{

}

void PhysicsEditor::Load()
{

}

void PhysicsEditor::Reload()
{

}

void PhysicsEditor::Save()
{

}

void PhysicsEditor::SaveAs()
{

}

void PhysicsEditor::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PhysicsEditor", this );
	pm.m( "Play",	Play	);
	pm.m( "Stop",	Stop	);
	pm.m( "Reset",	Reset	);
	pm.m( "Load",	Load	);
	pm.m( "Reload", Reload	);
	pm.m( "Save",	Save	);
	pm.m( "SaveAs", SaveAs	);
} // PhysicsEditor::Expose

void PhysicsEditor::Render()
{
	if (m_pBackScene) m_pBackScene->Render();
} // PhysicsEditor::Render

bool PhysicsEditor::OnChar( DWORD charCode, DWORD flags )
{
	if (IsInvisible()) return false;
	//if (charCode == ' ' && m_pEffect) m_pEffect->PlayEffect();
	return false;
} // PhysicsEditor::OnChar


END_NAMESPACE(sg)