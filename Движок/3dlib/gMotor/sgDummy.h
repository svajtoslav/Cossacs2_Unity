/*****************************************************************************/
/*	File:	sgDummy.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGDUMMY_H__
#define __SGDUMMY_H__

#include "akField.h"

namespace sg{
/*****************************************************************************/
/*	Class:	Locator
/*	Desc:	Dummy node used for object placing
/*****************************************************************************/
class Locator : public TransformNode
{
public:
	NODE(Locator,TransformNode,LCTR);
}; // class Locator 

/*****************************************************************************/
/*	Class:	Bone
/*	Desc:	Dummy node used for skinning bone
/*****************************************************************************/
class Bone : public TransformNode
{
public:

	virtual void			Render();

	NODE(Bone,TransformNode,BONE);
}; // class Bone 

/*****************************************************************************/
/*	Class:	Model
/*	Desc:	
/*****************************************************************************/
class Model : public Node
{
    std::string             m_ModelName;
    DWORD                   m_ModelID;

    AABoundBox              m_AABB;
    Sphere                  m_BoundSphere;
    Cylinder                m_BoundCylinder;


public:
                            Model       () : m_ModelID(0xFFFFFFFF) {}
    virtual void			Render      ();
    virtual void			Serialize   ( OutStream& os ) const;
    virtual void			Unserialize ( InStream& is );
    virtual void			Expose      ( PropertyMap& pm );

    const char*             GetModelName() const { return m_ModelName.c_str(); }
    void                    SetModelName( const char* name ) { m_ModelName = name; m_ModelID = 0xFFFFFFFF; }

    NODE(Model,Node,MMDL);
}; // class Model 

/*****************************************************************************/
/*	Class:	ZBias
/*	Desc:	Node which performs z-biasing of all its children	
/*****************************************************************************/
class ZBias : public Node
{
	float						m_Bias;
public:
	ZBias();

	virtual void				Serialize( OutStream& os ) const;
	virtual void				Unserialize( InStream& is );
	virtual void				Render();
	virtual void				Expose( PropertyMap& pm );

	float						GetBias() const { return m_Bias; }
	void						SetBias( float bias ) { m_Bias = bias; }

	NODE(ZBias,Node,ZBIA);
}; // class ZBias 

/*****************************************************************************/
/*	Class:	Group
/*	Desc:	Grouping node helper
/*****************************************************************************/
class Group : public Node
{
public:

	NODE(Group,Node,GRUP);
}; // class Group 

/*****************************************************************************/
/*	Class:	Switch
/*	Desc:	Switching node helper
/*****************************************************************************/
class Switch : public Node
{
	int						curActive;
public:
							Switch() : curActive(-1) {}

	virtual void			Render		();
	virtual void			Serialize	( OutStream& os ) const;
	virtual void			Unserialize	( InStream& is	);
	virtual void			Expose		( PropertyMap& pm );

	void					SwitchTo	( int nodeIdx );
	int						GetActive	() const	{ return curActive; }

	NODE(Switch,Node,SWIT);
}; // class Group 

/*****************************************************************************/
/*	Class:	FieldPatch
/*	Desc:	
/*****************************************************************************/
class FieldPatch : public Node
{
    FieldModel      m_Field;

    int             m_PatchesW;
    int             m_PatchesH;
    float           m_Width;
    float           m_Height;
    float           m_Age;

public:
                    FieldPatch		();
    virtual void	Serialize		( OutStream& os ) const;
    virtual void	Unserialize		( InStream& is );
    virtual void	Render			();
    virtual void	Expose			( PropertyMap& pm );

    NODE( FieldPatch, Node, FPAT );
}; // class FieldPatch

} // namespace sg

#ifdef _INLINES
#include "sgDummy.inl"
#endif // _INLINES

#endif // __SGDUMMY_H__