/*****************************************************************************/
/*	File:	uiManipulator.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	18.06.2004
/*****************************************************************************/
#ifndef __UIMANIPULATOR_H__
#define __UIMANIPULATOR_H__

namespace sg{
enum DragMode
{
    dmNone          = 0,
    dmDragX         = 1,
    dmDragY         = 2,
    dmDragZ         = 3,
    dmDragScreen    = 4
}; // enum DragMode

/*****************************************************************************/
/*	Class:	TranslateTool
/*	Desc:	Translating manipulator
/*****************************************************************************/
class TranslateTool : public Node, public InputDispatcher
{
    DragMode            m_DragMode;

    DWORD               m_XColor;
    DWORD               m_YColor;
    DWORD               m_ZColor;
    DWORD               m_PColor;       //  color of screen plane moving gizmo
    DWORD               m_SelColor;     //  selected gizmo element color

    float               m_ArrowLen;     //  length of the gizmo arrow, fraction of viewport side
    float               m_MinSelDist;   

    float               m_HeadLen;      //  length of the arrow head, fraction of arrow length
    float               m_HeadR;        //  radius of the arrow head, fraction of arrow length
    float               m_SGizmoSide;   //  side of screen space drag gizmo, in pixels

    Vector3D            m_StartPos;
    int                 m_StartMX, m_StartMY;

    Vector3D            m_InitPos;      
    Vector3D            m_CurPos;

    TransformNode*      m_pNode;

    float               GetWorldFrame       ( Vector3D&x, Vector3D& y, Vector3D& z );

public:
                        TranslateTool       ();
    virtual void		Render				();
    virtual void		Expose				( PropertyMap& pm );
    virtual bool 	    OnMouseLButtonDown	( int mX, int mY );
    virtual bool 	    OnMouseMove			( int mX, int mY, DWORD keys );
    virtual bool 	    OnMouseLButtonUp	( int mX, int mY );

    void                SetPosition         ( const Vector3D& pos );
    const Vector3D&     GetPosition         () const { return m_CurPos; }

    void                BindNode            ( TransformNode* pNode );
    void                UnbindNode          ();

    NODE(TranslateTool,Node,TRTL)
}; // class TranslateTool

/*****************************************************************************/
/*	Class:	ScaleTool
/*	Desc:	Scaling manipulator
/*****************************************************************************/
class ScaleTool : public Node, public InputDispatcher
{
    DragMode            m_DragMode;

    DWORD               m_XColor;
    DWORD               m_YColor;
    DWORD               m_ZColor;
    DWORD               m_PColor;       //  color of screen plane moving gizmo
    DWORD               m_SelColor;     //  selected gizmo element color

    float               m_ArrowLen;     //  length of the gizmo arrow, fraction of viewport side
    float               m_MinSelDist;   
    float               m_HeadLen;      //  length of the arrow head, fraction of arrow length

    Vector3D            m_StartPos;
    int                 m_StartMX, m_StartMY;

    Vector3D            m_InitPos;      
    Vector3D            m_CurPos;

    TransformNode*      m_pNode;

    float               GetWorldFrame       ( Vector3D&x, Vector3D& y, Vector3D& z );

public:
                        ScaleTool           ();
    virtual void		Render				();
    virtual void		Expose				( PropertyMap& pm );
    virtual bool 	    OnMouseLButtonDown	( int mX, int mY );
    virtual bool 	    OnMouseMove			( int mX, int mY, DWORD keys );
    virtual bool 	    OnMouseLButtonUp	( int mX, int mY );

    void                SetPosition         ( const Vector3D& pos );
    const Vector3D&     GetPosition         () const { return m_CurPos; }

    void                BindNode            ( TransformNode* pNode );
    void                UnbindNode          ();

    NODE(ScaleTool,Node,SCTL)
}; // class ScaleTool

/*****************************************************************************/
/*	Class:	RotateTool
/*	Desc:	Rotating manipulator
/*****************************************************************************/
class RotateTool : public Node, public InputDispatcher
{
	DragMode            m_DragMode;

	DWORD               m_XColor;
	DWORD               m_YColor;
	DWORD               m_ZColor;
	DWORD               m_PColor;       //  color of screen plane moving gizmo
	DWORD               m_SelColor;     //  selected gizmo element color

	float               m_ArrowLen;     //  length of the gizmo arrow, fraction of viewport side
	float               m_MinSelDist;   
	float               m_HeadLen;      //  length of the arrow head, fraction of arrow length

	Vector3D            m_StartPos;
	int                 m_StartMX, m_StartMY;

	Vector3D            m_InitPos;      
	Vector3D            m_CurPos;

	TransformNode*      m_pNode;

	float               GetWorldFrame       ( Vector3D&x, Vector3D& y, Vector3D& z );

public:
						RotateTool           ();
	virtual void		Render				();
	virtual void		Expose				( PropertyMap& pm );
	virtual bool 	    OnMouseLButtonDown	( int mX, int mY );
	virtual bool 	    OnMouseMove			( int mX, int mY, DWORD keys );
	virtual bool 	    OnMouseLButtonUp	( int mX, int mY );

	void                SetPosition         ( const Vector3D& pos );
	const Vector3D&     GetPosition         () const { return m_CurPos; }

	void                BindNode            ( TransformNode* pNode );
	void                UnbindNode          ();

	NODE(RotateTool,Node,ROTL)
}; // class RotateTool

}; // namespace sg

#endif // __UIMANIPULATOR_H__
