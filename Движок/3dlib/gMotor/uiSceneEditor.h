/*****************************************************************************/
/*	File:	uiSceneEditor.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-31-2003
/*****************************************************************************/
#ifndef __UISCENEEDITOR_H__
#define __UISCENEEDITOR_H__

#include "ISceneEditor.h"

#include <set>

namespace sg{

class TranslateTool;
class ScaleTool;
class RotateTool;

/*****************************************************************************/
/*	Class:	SceneEditor
/*  Desc:	Core Kangaroo functionality
/*****************************************************************************/
class SceneEditor : public Dialog, public ISceneEditor
{
public:
						SceneEditor			();
	virtual				~SceneEditor		();

	virtual bool 		OnMouseLButtonDown	( int mX, int mY );
    virtual bool 		OnMouseMove			( int mX, int mY, DWORD keys );

	virtual void		Render				();
	virtual void		Expose				( PropertyMap& pm );
	virtual bool		OnKeyDown			( DWORD keyCode, DWORD flags );	
	virtual bool		OnChar				( DWORD keyCode, DWORD flags );	

	void				ShowCameras			( bool val = true );
	bool				GetShowCameras		() const { return m_bShowCameras; }

	void				SetActiveCamera 	( ActiveEditorCamera ac )	{ m_ActiveCamera = ac; }
	ActiveEditorCamera	GetActiveCamera		() const					{ return m_ActiveCamera; }
	void				ShowGrid			( bool bShow = true )		{ m_bShowGrid = bShow; }
	bool				IsShowGrid			() const					{ return m_bShowGrid; }
	
	virtual Node*		GetSelectedNode		() { return m_pTreeL->GetSelectedNode(); }
    virtual void        SelectNode          ( Node* pNode ) { m_pTreeL->SelectNode( pNode ); }

	NODE(SceneEditor, Dialog, SCED);

protected:

	void 				DeleteNode			();
	void				ToggleInspector		();
	void 				InsertNode			();
	void 				ToggleHideScene		();
	void 				ToggleStatistics	();
    void                ToggleDrawTerrain   ();
	void 				RecompileShaders	();
    void 				ReloadTextures      ();
	void 				ApplySelectionTool	();
	void 				ApplyTranslateTool	();
	void 				ApplyRotateTool		();
	void 				ApplyScaleTool		();
	void 				ZoomSelection		();
	void 				ZoomExtents			();

	void				ResetScene			();

	void				ToggleEffectEditor	();
	void				TogglePhysicsEditor	();
	void				ToggleGlobalRoot	();
    void                ToggleOptimizations ();

	void 				SaveSubtree			();
	void 				InstantiateInput	();
	void 				LoadScene			();
	void 				LoadSubtree			();
	void 				BindAnimation		();
	void				StartProcess		();

	void				ResetCameras		();

    void                SaveEffects         ();
    void                LoadEffects         ();

	void				TogglePauseAnimation();
	void				CopyNode			();
	void				PasteNode			();
	void				CutNode				();
	void				MoveUpNode			();
	void				MoveDownNode		();

	void				ToggleLeftPane		();
	void				ToggleRightPane		();
	void				TogglePalette		();
    void                TogglePaletteType   ();
	void				ToggleInvisible		();

	void				DrawBones			();
	void				DrawLights			();
	void				DrawTransforms		();
	void				DrawLocators		();
	void				DrawEffectEmitters	();
	void				DrawCameras			();
	void				DrawBoundBoxes		();
	void				DrawGrid			();
    void                DrawNormals         ();
    void                DrawModelStats      ();

	void				ToggleCameraControl ();
    void				ToggleTempDebugger  ();

	void				OptimizeScene		();
	void				CreateVIPM			();

    void                PlayCinematics      ();
    void                ResetCinematics     ();
    void                CycleActiveCamera   ();
    void                SnapshotCamera      ();
    void                UnsnapshotCamera    ();

	Node*				PickNode			( int mX, int mY );
	Bone*				PickBone			( int mX, int mY );
    Geometry*           PickGeom            ( int mX, int mY, Vector3D& pos );

	bool				LoadChildNode		( Node* pParent );

	bool				m_bShowCameras;
	bool				m_bShowLights;
	bool				m_bShowAABB;
	bool				m_bShowLocators;
	bool				m_bShowBones;
	bool				m_bShowTransformNodes;
    bool                m_bShowNormals;
    bool                m_bShowModelStats;

    bool                m_bDrawTerrain;

	float				m_Size;
	float				m_HandleSide;

	DWORD				m_FrustumColor;
	DWORD				m_FrustumLinesColor;

	DWORD				m_AABBFillColor;
	DWORD				m_AABBLinesColor;

	Node*				m_pSelectedNode;
	Node*				m_pRootSubst;

    Node*               m_pTempDebugger;

	NodeTree*			m_pTreeL;	//  left tree pane
	NodeTree*			m_pTreeR;	//  right tree pane
	NodeTree*			m_pPalTree;	//  current editor's node palette tree
	
	Group*				m_pPalette;	//  full editor palette
    Group*				m_pMPalette;//  minimal editor palette

	ObjectInspector*	m_pInspector;


	EffectEditor*		m_pEffectEditor;
	PhysicsEditor*		m_pPhysicsEditor;


	StatManager*		m_pStatMgr;
	Group*				m_pScene;

	OrthoCamera*		m_pGameOrthoCamera;
	PerspCamera*		m_pGamePerspCamera;
	PerspCamera*		m_pEditorCamera;

	MayaController*		m_pMController;
	RTSController*		m_pRController;

    TranslateTool*      m_pTranslateTool;
    ScaleTool*          m_pScaleTool;
	RotateTool*			m_pRotateTool;

	Canvas*				m_pCanvas;

	bool				m_bShowGrid;
	DWORD				m_GridColor;
	DWORD				m_GridSubColor;
	float				m_GridSide;
	int					m_NGridCells;

	ActiveEditorCamera	m_ActiveCamera;
    std::set<DWORD>     m_Selection;        //  set of currently selected nodes

private:
	Group*				CreatePalette();
    Group*              CreateMinimalPalette();

}; // class SceneEditor

ISceneEditor*			GetSceneEditor();

}; // namespace sg

#endif // __UISCENEEDITOR_H__