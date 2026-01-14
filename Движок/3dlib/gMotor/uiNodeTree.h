/*****************************************************************************/
/*	File:	uiNodeTree.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	07-07-2003
/*****************************************************************************/
#ifndef __UINODETREE_H__
#define __UINODETREE_H__

namespace sg{
const int c_MaxTreeDepth		= 128;
/*****************************************************************************/
/*	Class:	NodeTree
/*	Desc:	Browser of the scene graph subtree
/*****************************************************************************/
class NodeTree : public Dialog
{
public:
						NodeTree				();
	virtual bool		OnMouseLButtonDblclk	( int mX, int mY );
	bool				OnMouseLButtonDown		( int mX, int mY );
	
	bool				OnMouseRButtonDown		( int mX, int mY );
	bool				OnMouseRButtonUp		( int mX, int mY );

	bool				OnKeyUp					( DWORD keyCode, DWORD flags );
	bool				OnKeyDown				( DWORD keyCode, DWORD flags );

	bool				OnMouseMove				( int mX, int mY, DWORD keys );

	bool				OnMouseMButtonUp		( int mX, int mY );
	bool				OnMouseMButtonDown		( int mX, int mY );

	Node*				GetSelectedNode			() const;
	Node*				GetRootNode				() const;		
	Node*				GetDraggedNode			() const;

	void				SetRightHand			( bool val = true ) { m_bRightHand		= val; }
	void				SetVisibleRoot			( bool val = true ) { m_bHasVisibleRoot = val; }
	void				SetAcceptOnDrop			( bool val = true ) { m_bAcceptOnDrop	= val; }
	void				SetEditable				( bool val = true ) { m_bEditable		= val; }
	void				SetDragLeafsOnly		( bool val = true ) { m_bDragLeafsOnly	= val; }

	void				SelectNode				( Node* pNode );
	void				SetRootNode				( Node* pNode );
	void				SetRootPos				( int x, int y ) { m_RootX = x; m_RootY = y; }

	void				SelectPrev				();
	void				SelectNext				();

	void				SwapPrev				();
	void				SwapNext				();


	Node*				PickNode				( int mX, int mY );
	virtual void		OnDrop					( int mX, int mY, DWORD ctx, DWORD obj );

	virtual void		Expose					( PropertyMap& pm );
	virtual void		Render					();

	NODE(NodeTree, Dialog, NOTR);

protected:

	virtual bool		DrawNode				(	Node* pNode,	const Rct& rct, 
													Node* pParent = NULL,	
													const Rct& prct = Rct::null );

	bool				PickNode				(	Node* pNode,	const Rct& rct, 
													Node* pParent,	const Rct& prct );

	virtual Rct			GetRootRct				() const;


	typedef bool		(NodeTree::*ItCallback)	(	Node* pNode,	const Rct& rct, 
													Node* pParent,	const Rct& prct );

	void				Iterate( ItCallback process );

	virtual DWORD		GetNodeBgColor			( Node* pNode ) const;
	virtual int			GetNodeGlyph			( Node* pNode ) const;


	DWORD				m_RootID;				//  current root node ID
	DWORD				m_DragID;				//  ID of currently dragged node
	bool				m_bDragCopy;			//  when true node is moved, else it is copied

	int					m_Path[c_MaxTreeDepth];	
	int					m_Depth;				//  current opened tree depth
	bool				m_bSelCollapse;			//  whether selected node is collapsed
	bool				m_bRightHand;			//  root node is rightmost
	bool				m_bDragLeafsOnly;		//  whether only leafs could be dragged out
	bool				m_bDropToItself;		//  whether nodes from us can be dropped here
	bool				m_bShowGlyphs;			//	whether to show glyph pictures on buttons
	bool				m_bHasVisibleRoot;		
	bool				m_bAcceptOnDrop;	
	bool				m_bEditable;


	int					m_NodeWidth;
	int					m_NodeHeight;
	int					m_HNodeSpacing;
	int					m_VNodeSpacing;

	int					m_RootX, m_RootY;		//  coordinates of the root node left top

	DWORD				m_LinesColorBeg;
	DWORD				m_LinesColorEnd;
	DWORD				m_TextColor;
	DWORD				m_DefaultNodeColor;

	//  current mouse position
	int					m_MX;
	int					m_MY;

	//  temporary values used in callbacks
	//	do not use them for anything else!!!
	Node*				m_PickResult;
	Rct					m_PickRct;
	Rct					m_ClipRct;
	Node*				m_SelNode;
	Node*				m_RootNode;
	int					m_StopPath[c_MaxTreeDepth];	
	int					m_StopDepth;				

}; // class NodeTree

}; // namespace sg

#endif // __UINODETREE_H__
