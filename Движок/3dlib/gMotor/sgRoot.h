/*****************************************************************************/
/*	File:	sgRoot.h
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#ifndef __SGROOT_H__
#define __SGROOT_H__

namespace sg{
class Group;
class DiskFolder;

/*****************************************************************************/
/*	Class:	Root, singleton
/*	Desc:	One and only entrance to the scene
/*****************************************************************************/
class Root : public Node, public PSingleton<Root>
{
public:
								Root		();
	virtual void				Render		();

	void						CreateGuts	();
	
	NODE(Root,Node,ROOT);

protected:
	friend class				NodePool;
	virtual						~Root();

	Group*						CreateConfig			();			
	Group*						CreateServices			();			
	Group*						CreateEditorSceneSetup	();	
	Group*						CreateGameSceneSetup	();	
	Group*						CreateFrameContainer	();	
	Group*						CreateEditors			();
	Group*						CreateModelManager		();
	Group*						CreateTemplates			();
}; // class Root


} // namespace sg


#endif // __SGROOT_H__