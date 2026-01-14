/*****************************************************************************/
/*	File:	sgModel.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGMODEL_H__
#define __SGMODEL_H__

namespace sg{
/*****************************************************************************/
/*	Class:	ModelManager
/*	Desc:	Node which manages models	
/*****************************************************************************/
class ModelManager : public Node, public PSingleton<ModelManager>
{
public:
								ModelManager	();
	virtual void				Serialize		( OutStream& os ) const;
	virtual void				Unserialize		( InStream& is );
	virtual void				Expose			( PropertyMap& pm );
	virtual void				AddModel		( const char* name, Node* pNode );


	NODE(ModelManager,Node,MMGR);
}; // class ModelManager 

/*****************************************************************************/
/*	Class:	ModelFile
/*	Desc:	Placeholder for the model file
/*****************************************************************************/
class ModelFile : public Node
{
    AABoundBox      m_AABB;
public:

    void                SetAABB( const AABoundBox& aabb ) { m_AABB = aabb; }
    const AABoundBox&   GetAABB() const { return m_AABB; } 

	NODE(ModelFile,Node,MFIL);
}; // class ModelFile 

/*****************************************************************************/
/*	Class:	AnimationManager
/*	Desc:	Node which manages animation	
/*****************************************************************************/
class AnimationManager : public Node, public PSingleton<AnimationManager>
{
public:
								AnimationManager();
	virtual void				Serialize		( OutStream& os ) const;
	virtual void				Unserialize		( InStream& is );
	virtual void				Expose			( PropertyMap& pm );
	virtual void				AddAnimation	( const char* name, Node* pNode );


	NODE(AnimationManager,Node,AMGR);
}; // class AnimationManager 

/*****************************************************************************/
/*	Class:	AnimationFile
/*	Desc:	Placeholder for the animation file
/*****************************************************************************/
class AnimationFile : public Node
{
public:
	NODE(AnimationFile,Node,AFIL);
}; // class AnimationFile 

} // namespace sg

#ifdef _INLINES
#include "sgModel.inl"
#endif // _INLINES

#endif // __SGMODEL_H__