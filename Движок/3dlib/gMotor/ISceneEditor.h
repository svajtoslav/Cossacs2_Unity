/*****************************************************************/
/*  File:   ISceneEditor.h
/*  Desc:   Interface for scene editor
/*  Date:   Apr 2004											 
/*****************************************************************/
#ifndef __ISCENEEDITOR_H__
#define __ISCENEEDITOR_H__

/*****************************************************************************/
/*	Enum: ActiveEditorCamera
/*****************************************************************************/
enum ActiveEditorCamera
{
	acEditor	= 0,
	acGameOrtho	= 1,
	acGamePersp	= 2,

    acLAST      = 3,
}; // enum ActiveEditorCamera

/*****************************************************************/
/*  Class:	ISceneEditor
/*  Desc:	Interface for scene editor
/*****************************************************************/
class ISceneEditor 
{
public:
	//  render all effects
	virtual void				Render			() = 0;
	virtual void				SetActiveCamera ( ActiveEditorCamera ac ) = 0;
	virtual ActiveEditorCamera	GetActiveCamera	() const = 0;
	virtual void				ShowGrid		( bool bShow = true ) = 0;
	virtual bool				IsShowGrid		() const = 0;
	virtual sg::Node*			GetSelectedNode () = 0;
    virtual void                SelectNode      ( sg::Node* pNode ) = 0;

}; // ISceneEditor

extern ISceneEditor* IScEd;

int		GetEditorFontID();
int		GetEditorGlyphsID();

ENUM( ActiveEditorCamera, "ActiveEditorCamera", 
	 en_val( acEditor,	 "Editor"	 ) << 
	 en_val( acGameOrtho, "GameOrtho" ) << 
	 en_val( acGamePersp, "GamePersp" ) );

#endif // __ISCENEEDITOR_H__