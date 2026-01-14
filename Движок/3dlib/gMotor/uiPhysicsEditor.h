/*****************************************************************************/
/*	File:	uiPhysicsEditor.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	05-23-2004
/*****************************************************************************/
#ifndef __UIPHYSICSEDITOR_H__
#define __UIPHYSICSEDITOR_H__

namespace sg{
class NodeTree;
/*****************************************************************************/
/*	Class:	PhysicsEditor	
/*	Desc:	Editor of the physics objects
/*****************************************************************************/
class PhysicsEditor : public Dialog
{
public:
							PhysicsEditor	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Render			();

	void					Play			();
	void					Stop			();
	void					Reset			();
	void					Load			();
	void					Reload			();
	void					Save			();
	void					SaveAs			();
	Node*					GetPhysicsRoot	();

	virtual bool			OnChar			( DWORD charCode, DWORD flags );

	Group*					GetPalette		() { return m_pPalette; }
	
	NODE(PhysicsEditor,Dialog,PHED);

protected:
	Group*					m_pPalette;	
	Node*					m_pBackScene;	//  background scene used for convenience	
}; // class PhysicsEditor

}; // namespace sg

#endif // __UIPHYSICSEDITOR_H__