/*****************************************************************************/
/*	File:	uiObjectInspector.h
/*	Desc:	Control used for visual editing scene node properties
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-13-2003
/*****************************************************************************/
#ifndef __UIOBJECTINSPECTOR_H__
#define __UIOBJECTINSPECTOR_H__

#include "kEnumTraits.h"

class ClassMember;
namespace sg{

class PropertyEditor;
/*****************************************************************************/
/*	Class:	InspectorItem
/*	Desc:	Visual aspect of the single object inspector item
/*****************************************************************************/
class InspectorItem
{
protected:
								InspectorItem();
	
	Rct							m_Extents;
	std::string					m_Caption;
	std::string					m_StrValue;
	int							m_IndexInMap;
	DWORD						m_FgColor;
	DWORD						m_BgColor;
	bool						m_bSelected;
	ClassMember*				m_pMember;
	PropertyEditor*				m_pEditor;
	
	bool						m_bSection;
	bool						m_bCollapsed;

	std::vector<InspectorItem>	m_SubItems;

	friend class				ObjectInspector;
	friend class				PropertyEditor;

}; // class InspectorItem

/*****************************************************************************/
/*	Class:	ObjectInspector
/*	Desc:	Control used for editing object property map
/*****************************************************************************/
class ObjectInspector : public Dialog, public PSingleton<ObjectInspector>
{
	PropertyMap*				m_pMap;	    //  pointer to the property map being shown
	std::vector<InspectorItem>	m_Items;    //  list of current items
	Node*						m_pNode;    //  scene node we are being inspecting

    //  metric settings
	int							m_DefItemHeight;
    int							m_MaxColWidth;
    int							m_MinColWidth;

    //  color settings
	DWORD						m_SelBgColor;
	DWORD						m_SelFgColor;
	DWORD						m_BgColor;
	DWORD						m_FgColor;
	DWORD						m_DisabledFgColor;
	DWORD						m_BgAltColor;
	DWORD						m_LinesColor1;
	DWORD						m_LinesColor2;
	DWORD						m_SectionFgColor;
	DWORD						m_SectionBgColor;
	
    bool						m_bShowParent;  //  when false show leaf class properties only

	typedef std::map<std::string, std::string> EditorsMap;
	EditorsMap					m_EditorsMap;


public:
					ObjectInspector		();
					~ObjectInspector	();

	void			BindNode			( Node* pNode );

	virtual void	Render				();
	virtual void	Serialize			( OutStream& os ) const;
	virtual void	Unserialize			( InStream& is );
	virtual void	Expose				( PropertyMap& pm );

	virtual bool 	OnMouseLButtonDown	( int mX, int mY );
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY );
	virtual bool 	OnMouseLButtonUp	( int mX, int mY );

	virtual bool 	OnMouseMButtonDown	( int mX, int mY );
	virtual bool 	OnMouseMButtonUp	( int mX, int mY );
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags );
	virtual bool	OnChar				( DWORD charCode, DWORD flags );

	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );

	int				GetSelectIdx		() const;
	void			SetSelectIdx		( int idx );
	int				GetNItems			() const;

	NODE(ObjectInspector,Dialog,OBJI);

protected:
	void			UpdateItems			();
	InspectorItem* 	GetItemByPt			( float pX, float pY );
	int				GetSelectedItem		();
	void			ClearSelection		();
	float			GetMaxCaptionWidth	();
	float			GetMaxValueWidth	();

	float			GetLeftColWidth		();
	float			GetRightColWidth	();
	PropertyEditor* CreateEditor		( const char* typeName );
}; // class ObjectInspector

/*****************************************************************************/
/*	Class:	PropertyEditor
/*	Desc:	Base class for editors of the values of the properties
/*****************************************************************************/
class PropertyEditor : public Control
{
public:
						PropertyEditor() : m_pMember( NULL ) {}
	void				SetClassMember( ClassMember* pMember ) { m_pMember = pMember; }
	void				SetEditedNode( Node* pNode ) { m_pEditedNode = pNode; }

	virtual void		OnForceEndEdit() {}

	virtual bool		OnKeyDown( DWORD keyCode, DWORD flags );
	void				Render();

	NODE(PropertyEditor,Control,PRED);

protected:
	ClassMember*		m_pMember;
	Node*				m_pEditedNode;
}; // class PropertyEditor

}; // namespace sg

#endif // __UIOBJECTINSPECTOR_H__