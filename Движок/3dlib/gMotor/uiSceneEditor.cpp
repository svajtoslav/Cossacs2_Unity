/*****************************************************************************/
/*	File:	uiSceneEditor.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-31-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"
#include "uiControl.h"
#include "sgStatistics.h"
#include "kCommand.h"
#include "kSystemDialogs.h"
#include "sgPipeline.h"
#include "sgEffect.h"
#include "uiManipulator.h"
#include "uiNodeTree.h"
#include "uiObjectInspector.h"
#include "uiEffectEditor.h"
#include "uiPhysicsEditor.h"
#include "csCameraController.h"
#include "uiSceneEditor.h"
#include "IWidgetManager.h"
#include "ITerrain.h"
#include "mLodder.h"
#include "mSkin.h"

int	GetEditorFontID()
{
	static int s_FontID = IWM->CreateFont( "Tahoma", 8 );
	return s_FontID;
}

int	GetEditorGlyphsID()
{
	static int s_GlyphsID = IWM->CreateUniformFont( "sgIcons.tga", 16, 16 );
	return s_GlyphsID;
}

ISceneEditor* IScEd = NULL;

bool DrawText( int x, int y, DWORD color, const char* format, ... )
{
    char buffer[1024]; 
    va_list argList;
    va_start( argList, format );
    vsprintf( buffer, format, argList );
    va_end( argList );
    IWM->DrawString( GetEditorFontID(), buffer, Vector3D( x, y, 0 ), color );
    return true;
} // DrawText

bool DrawText( const Vector3D& pos, DWORD color, const char* format, ... )
{
    char buffer[1024]; 
    va_list argList;
    va_start( argList, format );
    vsprintf( buffer, format, argList );
    va_end( argList );
    IWM->DrawStringW( GetEditorFontID(), buffer, pos, color );
    return true;
} // DrawText

void FlushText()
{
    IWM->FlushText( GetEditorFontID() );
}

bool ConvertModel( sg::Node* pModel );

BEGIN_NAMESPACE(sg)
void DumpToH3()
{
	FILE* fp = fopen( "d:\\dumps\\h3.lvlist.txt", "wt" );
	if (!fp) return;

	Node::Iterator it( sg::Root::instance() );
	while (it)
	{	
		Node* pNode = it;

		const char* group = "node";
		if (pNode->IsA<Group>()			) group = "group";
		if (pNode->IsA<TransformNode>()	) group = "transform";
		if (pNode->IsA<Geometry>()		) group = "geometry";
		if (pNode->IsA<Texture>()		) group = "texture";
		if (pNode->IsA<Control>()		) group = "control";
		if (pNode->IsA<Light>()			) group = "light";
		if (pNode->IsA<Material>()		) group = "material";
		if (pNode->IsA<Controller>()	) group = "controller";
		if (pNode->IsA<BaseCamera>()	) group = "camera";
		if (pNode->IsA<DeviceStateSet>()) group = "dss";

		int depth = it.GetDepth();
		fprintf( fp, "%d %s %d %s\x0A", depth, pNode->GetName(), depth - 1, group );	
		++it;
	}
	fclose( fp );
}

/*****************************************************************************/
/*	SceneEditor implementation
/*****************************************************************************/
SceneEditor::SceneEditor()
{
	IScEd = this;

	m_bShowCameras			= false;
	m_bShowLights			= false;
	m_bShowAABB				= false;
	m_bShowLocators			= false;
	m_bShowTransformNodes	= false;
	m_bShowBones			= false;
    m_bDrawTerrain          = false;
    m_bShowNormals          = false;
    m_bShowModelStats       = false;

	m_FrustumColor			= 0x0;
	m_FrustumLinesColor		= 0x88FF0000;

	m_AABBFillColor			= 0x0;
	m_AABBLinesColor		= 0x660000FF;
	m_Size					= 0.001f;
	m_HandleSide			= 2.0f;
	m_ActiveCamera			= acEditor;
	m_pRootSubst			= this;

	SetActive( true );

	m_pScene			= AddChild<Group>			( "Scene"			);
	m_pInspector		= AddChild<ObjectInspector>	( "Inspector"		);
	m_pEffectEditor		= AddChild<EffectEditor>	( "EffectEditor"	);
	m_pPhysicsEditor	= AddChild<PhysicsEditor>	( "PhysicsEditor"	);
	m_pStatMgr			= AddChild<StatManager>		( "Stats"			);

	m_pTreeL			= AddChild<NodeTree>		( "LeftTree"		);
	m_pTreeL->SetRootNode( m_pScene );
	m_pTreeL->SetDragLeafsOnly( false );
	
	m_pTreeR			= AddChild<NodeTree>		( "RightTree"		);
	m_pTreeR->SetRootNode( m_pScene );
	m_pTreeR->SetDragLeafsOnly( false );
	m_pTreeR->SetRightHand( true );
	m_pTreeR->SetInvisible();

	m_pPalette = CreatePalette();
	m_pPalette->SetInvisible();

    m_pMPalette = CreateMinimalPalette();
    m_pMPalette->SetInvisible();

	m_pPalTree = AddChild<NodeTree>( "PalTree" );
	m_pPalTree->SetRootNode		( m_pMPalette );
	m_pPalTree->SetRightHand	();
	m_pPalTree->SetVisibleRoot	( false );
	m_pPalTree->SetAcceptOnDrop	( false );
	m_pPalTree->EnableDrag		( true );
	m_pPalTree->SetEditable		( false );
	m_pPalTree->SetInvisible	();

    m_pTempDebugger = sg::Root::instance()->AddChild( "TDBG", "Debuggers" );
    if (m_pTempDebugger) m_pTempDebugger->SetInvisible( true );

	Rct ext = IRS->GetViewPort();
	m_pPalTree->SetRootPos( ext.w - 10, 200.0f );

	Rct vp = IRS->GetViewPort();
	ext.x = vp.w*0.5f - 200.0f;
	m_pInspector->SetExtents( ext );

	m_pEffectEditor->SetInvisible();
	m_pStatMgr->SetInvisible();

	//  editor scene setup
	m_pCanvas = AddChild<Canvas>( "Canvas" );
	m_pCanvas->SetExtents( 0.0f, 0.0f, 1024.0f, 768.0f );
	m_pCanvas->SetBgColor( 0xFF3A3448 );
	m_pCanvas->SetViewportFlag( Canvas::vfClearColor );
	m_pCanvas->SetViewportFlag( Canvas::vfClearDepth );


	//  editor camera
	m_pEditorCamera = AddChild<PerspCamera>( "EditorCamera" );

	//  perspective game camera
	m_pGamePerspCamera = AddChild<PerspCamera>( "AltCamera" );
	m_pGamePerspCamera->SetTweakAspect( false );

	//  ortho game camera
	m_pGameOrthoCamera = AddChild<OrthoCamera>( "GameCamera" );
	m_pGameOrthoCamera->SetTweakAspect( false );

	m_pMController = AddChild<MayaController>( "MController" );
	m_pMController->SetActive( true );
	m_pMController->AddInput( m_pEditorCamera );	
	m_pMController->AddInput( m_pGamePerspCamera );
	m_pMController->AddInput( m_pGameOrthoCamera );

	m_pRController = AddChild<RTSController>( "RController" );
	m_pRController->SetActive( false );
	m_pRController->AddInput( m_pEditorCamera );	
	m_pRController->AddInput( m_pGamePerspCamera );
	m_pRController->AddInput( m_pGameOrthoCamera );

    m_pTranslateTool = AddChild<TranslateTool>( "TranslateTool" );
    m_pTranslateTool->SetActive( false );
    m_pTranslateTool->SetInvisible( true );

    m_pScaleTool = AddChild<ScaleTool>( "ScaleTool" );
    m_pScaleTool->SetActive( false );
    m_pScaleTool->SetInvisible( true );

	m_pRotateTool = AddChild<RotateTool>( "RotateTool" );
	m_pRotateTool->SetActive( false );
	m_pRotateTool->SetInvisible( true );

	ResetCameras();

	m_bShowGrid		= true;				
	m_GridColor		= 0xFF000000;
	m_GridSubColor	= 0xB8454545;
	m_GridSide		= 800.0f;			
	m_NGridCells	= 20.0f;			
} // SceneEditor::SceneEditor

SceneEditor::~SceneEditor()
{
}

Group* CreateNodeTemplateSet();
Group* CreateMinNodeTemplateSet();
Group* SceneEditor::CreatePalette()
{
	return CreateNodeTemplateSet();
} // SceneEditor::CreatePalette

Group* SceneEditor::CreateMinimalPalette()
{
    return CreateMinNodeTemplateSet();
} // SceneEditor::CreateMinimalPalette

bool SceneEditor::OnMouseLButtonDown( int mX, int mY )
{
    if (/*m_bShowBones && */GetKeyState( VK_CONTROL ) < 0)
    {
	    Node* pPicked = NULL;
	    pPicked = PickBone( mX, mY );
        Vector3D pos = Vector3D::null;
        if (!pPicked) 
        {
            pPicked = PickGeom( mX, mY, pos );
            if (pPicked) pPicked = pPicked->GetParent();
        }
        if (pPicked)
        {
            m_pTreeL->SelectNode( pPicked );
            m_Selection.insert( pPicked->GetID() );
        }
        else m_Selection.clear();
    }
	return false;
} // SceneEditor::OnMouseLButtonDown

void SceneEditor::Expose( PropertyMap& pm )
{	
	pm.start( "SceneEditor", this );
    pm.p( "ShowCameras",		GetShowCameras, ShowCameras );
	pm.f( "ShowLights",			m_bShowLights				);
	pm.f( "ShowAABB",			m_bShowAABB					);
	pm.f( "ShowLocators",		m_bShowLocators				);
	pm.f( "ShowBones",			m_bShowBones				);
	pm.f( "ShowTransformNodes",	m_bShowTransformNodes		);
    pm.f( "ShowNormals",	    m_bShowNormals              );
	pm.f( "ShowGrid",			m_bShowGrid					);
    pm.f( "ShowModelStats",     m_bShowModelStats           );
	pm.f( "ActiveCamera",		m_ActiveCamera				);
	pm.m( "ResetScene",			ResetScene					);
	pm.m( "LoadModel",			LoadScene					);
    pm.m( "LoadAnimation",		BindAnimation               );
    pm.m( "LoadEffectSet",		LoadEffects                 );
    pm.m( "SaveEffectSet",		SaveEffects                 );
} // SceneEditor::Expose

void SceneEditor::ShowCameras( bool val )
{
	PRSAnimation::AnimateInvisible( val );
	m_bShowCameras = val;
} // SceneEditor::ShowCameras

void SceneEditor::Render()
{	
	m_pSelectedNode = m_pTreeL->GetSelectedNode();
	Node* pInspectorNode = m_pSelectedNode;
	if (pInspectorNode == m_pTreeL->GetRootNode() && m_pRootSubst)
	{
		pInspectorNode = m_pRootSubst;
	}
	m_pInspector->BindNode( pInspectorNode );

	if (!m_pCanvas->IsInvisible()) m_pCanvas->Render();
	//  set camera
	if (m_ActiveCamera == acEditor)
	{
		m_pEditorCamera->Render();
	}
	else if (m_ActiveCamera == acGamePersp)
	{
		m_pGamePerspCamera->Render();
	}
	else if (m_ActiveCamera == acGameOrtho)
	{
		m_pGameOrthoCamera->Render();
	}
	
    //  pre-render effects layer
    IEffMgr->PreRender();

    if (!m_pEffectEditor->IsInvisible())	m_pEffectEditor->Render();
    if (m_bDrawTerrain) ITerra->Render();
    
	//  render grid
	if (m_bShowGrid) DrawGrid();

	//  render scene
	if (!m_pScene->IsInvisible())	m_pScene->Render();
	if (m_bShowCameras)				DrawCameras();
	if (m_bShowLights)				DrawLights();
	if (m_bShowAABB)				DrawBoundBoxes();
	if (m_bShowLocators)			DrawLocators();
	if (m_bShowBones)				DrawBones();
	if (m_bShowTransformNodes)		DrawTransforms();
    if (m_bShowNormals)             DrawNormals();
    if (m_bShowModelStats)          DrawModelStats();

    if (m_pTempDebugger && !m_pTempDebugger->IsInvisible()) m_pTempDebugger->Render();

    if (!m_pTranslateTool->IsInvisible())   m_pTranslateTool->Render();
    if (!m_pScaleTool->IsInvisible())       m_pScaleTool->Render();
	if (!m_pRotateTool->IsInvisible())      m_pRotateTool->Render();

    IEffMgr->Evaluate();
    IEffMgr->PostRender();

	if (!m_pTreeL->IsInvisible())			m_pTreeL->Render();
	if (!m_pTreeR->IsInvisible())			m_pTreeR->Render();
    if (!m_pInspector->IsInvisible())		m_pInspector->Render();
	if (!m_pStatMgr->IsInvisible())			m_pStatMgr->Render();

	BaseCamera* pCurCam = BaseCamera::GetActiveCamera();
	if (!pCurCam) return;
	if (!m_pPalTree->IsInvisible()) m_pPalTree->Render();
} // SceneEditor::Render

const float c_RightB = 100.0f;
const float c_NumB   = 30.0f;
const float c_TopB   = 30.0f;
const float c_Height = 12.0f;   
void SceneEditor::DrawModelStats()
{
    int nPoly       = 0;
    int nVertices   = 0;
    int nShaders    = 0;
    int nTextures   = 0;
    int nBones      = 0;
    int nSubmeshes  = 0;
    int nVert1W     = 0;
    int nVert2W     = 0;
    int nVert3W     = 0;
    int nVert4W     = 0;
    
    float cX = IRS->GetViewPort().w - c_RightB;
    float cN = c_NumB;
    float cY = c_TopB;
    
    DrawText( cX, cY, 0xFFCCCCFF, "Polygons:" );
    cY += c_Height;
    
    DrawText( cX, cY, 0xFFCCCCFF, "Vertices:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Shaders:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Textures:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Bones:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Meshes:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Vert1W:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Vert2W:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Vert3W:" );
    cY += c_Height;

    DrawText( cX, cY, 0xFFCCCCFF, "Vert4W:" );
    cY += c_Height;
    
    FlushText();
} // SceneEditor::DrawModelStats

void SceneEditor::DrawGrid()
{
	IRS->ResetWorldMatrix();
	Rct grid( -m_GridSide*0.5f, -m_GridSide*0.5f, m_GridSide, m_GridSide );

	float dx = grid.w / m_NGridCells;
	float x = grid.x;
	for (int i = 0; i <= m_NGridCells; i++)
	{
		rsLine( Vector3D( x, grid.y, 0.0f ), Vector3D( x, grid.GetBottom(), 0.0f ), m_GridSubColor );
		x += dx;
	}

	float dy = grid.h / m_NGridCells;
	float y = grid.y;
	for (int i = 0; i <= m_NGridCells; i++)
	{
		rsLine( Vector3D( grid.x, y, 0.0f ), 
			Vector3D( grid.GetRight(), y, 0.0f ), m_GridSubColor );
		y += dy;
	}

	rsLine( Vector3D( 0.0f, grid.y, 0.0f ), Vector3D( 0.0f, grid.GetBottom(), 0.0f ), m_GridColor );
	rsLine( Vector3D( grid.x, 0.0f, 0.0f ), Vector3D( grid.GetRight(), 0.0f, 0.0f ), m_GridColor );
	rsFlushLines3D();
} // SceneEditor::DrawGrid

void SceneEditor::DrawNormals()
{
    IRS->ResetWorldMatrix();
    Iterator git( m_pScene, Geometry::FnFilter );
    while (git)
    {
        Geometry* pGeom = (Geometry*)(Node*)git;
        
        ++git;
    }
} // SceneEditor::DrawNormals

void SceneEditor::DrawLights()
{
	IRS->ResetWorldMatrix();
	Iterator dit( m_pScene, DirectionalLight::FnFilter );
	while (dit)
	{
		DirectionalLight* pLight = (DirectionalLight*)(Node*)dit;

		Matrix4D lightTM = pLight->GetWorldTM();
		Ray3D ray = pLight->GetLightRay();
		ray.Normalize();
		if (!pLight->IsInvisible()) DrawRay( ray, 0xFFFFFF00 );
		++dit;
	}

    IRS->ResetWorldMatrix();
	Iterator pit( m_pScene, PointLight::FnFilter );
	while (pit)
	{
		PointLight* pLight = (PointLight*)(Node*)pit;
		Sphere sp = pLight->GetLightSphere();
		sp.SetRadius( 40.0f );
		
		if (!pLight->IsInvisible()) DrawStar( sp, 0xFFFFFF00, 0x00FFFF00, 16 );
		++pit;
	}

	rsFlushPoly3D();
	rsFlushLines3D();
} // SceneEditor::DrawLights

void SceneEditor::DrawTransforms()
{
	IRS->ResetWorldMatrix();
	Iterator it( m_pScene, TransformNode::FnFilter );
	while (it)
	{
		TransformNode* pNode = (TransformNode*)(Node*)it;
		if (pNode->IsA<Locator>()) { ++it; continue; }

		Matrix4D wTM = pNode->GetTopTM();
		Vector3D pos = wTM.getTranslation();
		AABoundBox box( pos, m_HandleSide );
		DrawAABB( box, 0, 0xFFFFFF00 );
		Vector3D posX( pos ); 
		posX.addWeighted( Vector3D( wTM.e00, wTM.e01, wTM.e02 ), m_HandleSide * 3.0f );
		Vector3D posY( pos );
		posY.addWeighted( Vector3D( wTM.e10, wTM.e11, wTM.e12 ), m_HandleSide * 3.0f );
		Vector3D posZ( pos );
		posZ.addWeighted( Vector3D( wTM.e20, wTM.e21, wTM.e22 ), m_HandleSide * 3.0f );
		rsLine( pos, posX, 0xFFFF0000, 0xFFFF0000 );
		rsLine( pos, posY, 0xFF00FF00, 0xFF00FF00 );
		rsLine( pos, posZ, 0xFF0000FF, 0xFF0000FF );
		++it;
	}

	rsFlushPoly3D();
	rsFlushLines3D();
} // SceneEditor::DrawTransforms

void SceneEditor::DrawLocators()
{
	IRS->ResetWorldMatrix();
	Iterator it( m_pScene, Locator::FnFilter );
	while (it)
	{
		Locator* pNode = (Locator*)(Node*)it;

		Matrix4D wTM = pNode->GetWorldTM();
		Vector3D pos = wTM.getTranslation();
		AABoundBox box( pos, m_HandleSide );
		DrawAABB( box, 0, 0xFFFFFFFF );
		Vector3D posX( pos ); 
		posX.addWeighted( Vector3D( wTM.e00, wTM.e01, wTM.e02 ), m_HandleSide * 3.0f );
		Vector3D posY( pos );
		posY.addWeighted( Vector3D( wTM.e10, wTM.e11, wTM.e12 ), m_HandleSide * 3.0f );
		Vector3D posZ( pos );
		posZ.addWeighted( Vector3D( wTM.e20, wTM.e21, wTM.e22 ), m_HandleSide * 3.0f );
		rsLine( pos, posX, 0xFFFF0000, 0xFFFF0000 );
		rsLine( pos, posY, 0xFF00FF00, 0xFF00FF00 );
		rsLine( pos, posZ, 0xFF0000FF, 0xFF0000FF );
		++it;
	}

	rsFlushPoly3D();
	rsFlushLines3D();
} // SceneEditor::DrawLocators

void SceneEditor::DrawEffectEmitters()
{

}

void SceneEditor::DrawCameras()
{
	Iterator it( m_pScene, BaseCamera::FnFilter );
	while (it)
	{
		BaseCamera* pCam = (BaseCamera*)(Node*)it;
		
		//  render frustum
		Frustum frustum;
		pCam->GetWorldSpaceFrustum( frustum );
		IRS->ResetWorldMatrix();
		DrawFrustum( frustum, m_FrustumColor, m_FrustumLinesColor, true );
		
		++it;
	}

	rsFlushPoly3D();
	rsFlushLines3D();
} // SceneEditor::DrawCameras

void SceneEditor::DrawBoundBoxes()
{
	Iterator it( m_pScene, Geometry::FnFilter );
	while (it)
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		IRS->ResetWorldMatrix();
		AABoundBox aabb( pGeom->GetAABB() );
		aabb.Transform( GetWorldTM( pGeom ) );
		DrawAABB( aabb, m_AABBFillColor, m_AABBLinesColor );
		++it;
	}

	rsFlushPoly3D();
	rsFlushLines3D();
} //  SceneEditor::DrawAABB

void SceneEditor::DrawBones()
{
	Iterator it( m_pScene );
	if (!m_pScene) return;
	IRS->ResetWorldMatrix();
	rsEnableZ( false );
	float hside = m_HandleSide;
	int nBones = 0;
	while (it)
	{
		Bone* pBone = (Bone*)(Node*)it;	
		Bone* pParent = (Bone*)pBone->GetParent();
		if (!pParent->Owns( pBone ) || 
			pBone->IsA<SkinnedGeometry>() ||
			pBone->IsA<PRSAnimation>()) 
		{ 
			it.Up(); 
			continue; 
		}

		if (!pBone->IsA<Bone>()) 
		{
			++it;
			continue;
		}

		const Matrix4D wtm = pBone->GetTopTM();

		DrawCircle8( wtm.getTranslation(), wtm.getV0(), hside, 0, 0xCCAA0000 );
		DrawCircle8( wtm.getTranslation(), wtm.getV1(), hside, 0, 0xCC00AA00 );
		DrawCircle8( wtm.getTranslation(), wtm.getV2(), hside, 0, 0xCC0000AA );

		if (pParent->IsA<Bone>())
		{
			const Matrix4D ptm = pParent->GetTopTM();
            DWORD clrStart = 0xCCFFFF00;
            DWORD clrEnd = 0x33FFFF00;
            if (m_Selection.find( pBone->GetID() ) != m_Selection.end())
            {
                clrStart = 0xCCFF0000;
                clrEnd   = 0x33FF0000;
            }
			rsLine( wtm.getTranslation(), ptm.getTranslation(), clrStart, clrEnd );
		}
		++it;
		nBones++;
	}
	rsFlushLines3D();
} // SceneEditor::DrawBones
	
void SceneEditor::DeleteNode()
{
} // SceneEditor::DeleteNode

void SceneEditor::CycleActiveCamera()
{
    int cCam = (int)m_ActiveCamera;
    cCam++;
    m_ActiveCamera = (ActiveEditorCamera)cCam;
    if (m_ActiveCamera >= acLAST) m_ActiveCamera = acEditor;
}

void SceneEditor::InsertNode()
{
	//if (keyCode == VK_INSERT)
	//{
	//	m_bInsertMode = true;
	//	POINT pt;
	//	GetCursorPos( &pt );
	//	CheckInsertMode( pt.x, pt.y );
	//}
} // SceneEditor::InsertNode

void SceneEditor::ToggleHideScene()
{
	Group* pFrame = Root::instance()->FindChild<Group>( "Frame" );
	m_pScene->SetInvisible( !m_pScene->IsInvisible() );
	if (pFrame) pFrame->SetInvisible( !pFrame->IsInvisible() );
} // SceneEditor::ToggleHideScene

void SceneEditor::RecompileShaders()
{
	IRS->RecompileAllShaders();
} // SceneEditor::RecompileShaders

void SceneEditor::ReloadTextures()
{
    IRS->ReloadAllTextures();
} // SceneEditor::RecompileShaders

void SceneEditor::ApplyTranslateTool()
{
    TransformNode* pNode = ( TransformNode*)GetSelectedNode();
    if (!pNode->IsA<TransformNode>()) return;

    m_pTranslateTool->SetActive( true );
    m_pTranslateTool->SetInvisible( false );
    m_pTranslateTool->BindNode( pNode );
} // SceneEditor::ApplyTranslateTool

void SceneEditor::ApplyRotateTool()
{
	TransformNode* pNode = ( TransformNode*)GetSelectedNode();
	if (!pNode->IsA<TransformNode>()) return;

	m_pRotateTool->SetActive( true );
	m_pRotateTool->SetInvisible( false );
	m_pRotateTool->BindNode( pNode );
} // SceneEditor::ApplyRotateTool

void SceneEditor::ApplyScaleTool()
{
    TransformNode* pNode = ( TransformNode*)GetSelectedNode();
    if (!pNode->IsA<TransformNode>()) return;

    m_pScaleTool->SetActive( true );
    m_pScaleTool->SetInvisible( false );
    m_pScaleTool->BindNode( pNode );
} // SceneEditor::ApplyScaleTool

void SceneEditor::ApplySelectionTool()
{
    m_pTranslateTool->SetActive( false );
    m_pTranslateTool->SetInvisible( true );
    m_pTranslateTool->UnbindNode();

    m_pScaleTool->SetActive( false );
    m_pScaleTool->SetInvisible( true );
    m_pScaleTool->UnbindNode();

	m_pRotateTool->SetActive( false );
	m_pRotateTool->SetInvisible( true );
	m_pRotateTool->UnbindNode();

} // SceneEditor::ApplySelectionTool

void SceneEditor::ZoomSelection()
{

} // SceneEditor::ZoomSelection

void SceneEditor::ZoomExtents()
{

} // SceneEditor::ZoomExtents

void SceneEditor::SaveSubtree()
{
	const char* fileName = NULL;
	const char* fileExt = NULL;

	_chdir( GetRootDirectory() );
	_chdir( "Models" );

	SaveFileDialog dlg;
	dlg.AddFilter( "Binary Model Files", "*.c2m" );
	dlg.AddFilter( "XML Model Files", "*.x2m" );
	dlg.SetDefaultExtension( "c2m" );
	static char lpstrFile[_MAX_PATH];
	if (!dlg.Show()) return;

	_chdir( GetRootDirectory() );

	fileName = dlg.GetFilePath();
	if (!fileName) return;
	Node* pRoot = GetSelectedNode();
	if (pRoot)
	{
		FOutStream os( fileName );
		char drive	[_MAX_DRIVE	];
		char dir	[_MAX_DIR	];
		char file	[_MAX_PATH	];
		char ext	[_MAX_EXT	];

		_splitpath( fileName, drive, dir, file, ext );

		pRoot->SerializeSubtree( os );
	}
} // SceneEditor::SaveSubtree

void SceneEditor::InstantiateInput()
{
	/*SGBrowserNode* pSGNode = (SGBrowserNode*)m_pSelectedNode;
	SGBrowserNode* pSGParentNode = NULL;
	if (pSGNode) pSGParentNode = (SGBrowserNode*)m_pSelectedNode->m_pParent;
	if (pSGNode && pSGParentNode)  
	{
		Node* pNode			= pSGNode->m_pNode;
		Node* pParentNode	= pSGParentNode->m_pNode;
		if (!pParentNode->Owns( pNode ))
		{
			Node* pInstance = pNode->Clone();
			pParentNode->ReplaceChild( pNode, pInstance );
			pInstance->SetParent( pParentNode );
		}
		UpdateGraph();
	}*/
} //  SceneEditor::InstantiateInput

void SceneEditor::ResetScene()
{
	m_pScene->RemoveChildren();
}

bool SceneEditor::LoadChildNode( Node* pParent )
{
	if (!pParent) return false;
	const char* fileName = NULL;
	_chdir( GetRootDirectory() );
	_chdir( "Models" );

	OpenFileDialog dlg;
	dlg.AddFilter( "Binary Model Files", "*.c2m" );
	dlg.AddFilter( "XML Model Files", "*.x2m" );
	dlg.SetDefaultExtension( "c2m" );
	static char lpstrFile[_MAX_PATH];
	if (!dlg.Show()) return false;
	fileName = dlg.GetFilePath();

	_chdir( GetRootDirectory() );

	if (!fileName) return false;
	FInStream os( fileName );
	Node* pModel = Node::UnserializeSubtree( os );
	if (!pModel) 
	{
		Log.Warning( "Could not load model: %s", fileName );
		return false;
	}
    ConvertModel( pModel );
	pParent->AddChild( pModel );
	return true;
} // SceneEditor::LoadChildNode

void SceneEditor::LoadScene()
{
	LoadChildNode( m_pScene );
}

void SceneEditor::LoadSubtree()
{
	LoadChildNode( GetSelectedNode() );
} // SceneEditor::LoadSubtree

void SceneEditor::OptimizeScene()
{
	Iterator it( m_pScene, Geometry::FnFilter );
	while (it)
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		Primitive& pri = pGeom->GetPrimitive();
		bool OptimizeForGPUCache( Primitive& pri );
		OptimizeForGPUCache( pri );
		if (pGeom->IsA<MorphedGeometry>()) ((MorphedGeometry*)pGeom)->ReplicateMesh();
		++it;
	}
} // SceneEditor::OptimizeScene

void SceneEditor::CreateVIPM()
{
	Iterator it( m_pScene, Geometry::FnFilter );
	while (it)
	{
		Geometry* pGeom = (Geometry*)(Node*)it;
		Primitive& pri = pGeom->GetPrimitive();
		VIPMLodder lodder;
		int nV = pri.getNVert();
		int nF = pri.getNPri();
		VertexIterator vit; vit << pri;
		for (int i = 0; i < nV; i++) lodder.AddVertex( vit.pos( i ) );
		WORD* idx = pri.getIndices();
		for (int i = 0; i < nF; i++) lodder.AddFace( idx[i*3], idx[i*3 + 1], idx[i*3 + 2] );

		FILE* fp = fopen( "c:\\dumps\\vipm.txt", "at+" );
		fprintf( fp, "\n\n%s\n", pGeom->GetName() );
		fclose( fp );
		lodder.Process();
		++it;
	}
} // SceneEditor::CreateVIPM

void SceneEditor::BindAnimation()
{
	const char* fileName = NULL;
	_chdir( GetRootDirectory() );
	_chdir( "Models" );

	OpenFileDialog dlg;
	dlg.AddFilter( "Model Files", "*.c2m" );
	dlg.SetDefaultExtension( "c2m" );
	static char lpstrFile[_MAX_PATH];
	if (!dlg.Show()) return;
	fileName = dlg.GetFilePath();
	if (!fileName) return;
	FInStream os( fileName );
	//Node* pRoot = GetSelectedNode();
    Node* pRoot = m_pScene->GetChild( 0 );
	if (!pRoot) return;

	Node* pAnim = Node::UnserializeSubtree( os );
	Node* pScene = m_pScene;
	pScene->AddChild( pAnim );
	pAnim->AttachSubtree( pRoot );
	if (pAnim->IsA<sg::Animation>())
	{
		Animation* pAnimation = (Animation*)pAnim;
		pAnimation->Loop();
		pAnimation->Play();
	}
} //  SceneEditor::BindAnimation

void SceneEditor::SaveEffects()
{
    if (!m_pScene) return;
    const char* fileName = NULL;
    const char* fileExt = NULL;

    _chdir( GetRootDirectory() );
    _chdir( "Models\\Scripts" );

    SaveFileDialog dlg;
    dlg.AddFilter( "XML Files", "*.x2m" );
    dlg.SetDefaultExtension( "x2m" );
    static char lpstrFile[_MAX_PATH];
    if (!dlg.Show()) return;

    _chdir( GetRootDirectory() );

    fileName = dlg.GetFilePath();
    if (!fileName) return;    
    FOutStream os( fileName );
    if (os.NoFile()) return;
    
    XMLNode root;
    root.SetTag( "AnimDescr" );
    Iterator it( m_pScene, PEffect::FnFilter );
    while (it)
    {
        PEffect* pEff = (PEffect*)(Node*)it;
        root.AddChild( pEff->ToXML() );
        ++it;
    }
    root.Write( os );
} // SceneEditor::SaveEffects

void SceneEditor::LoadEffects()
{
    const char* fileName = NULL;
    _chdir( GetRootDirectory() );
    _chdir( "Models\\Scripts" );

    OpenFileDialog dlg;
    dlg.AddFilter( "XML Model Files", "*.x2m" );
    dlg.SetDefaultExtension( "x2m" );
    static char lpstrFile[_MAX_PATH];
    if (!dlg.Show()) return;
    fileName = dlg.GetFilePath();

    _chdir( GetRootDirectory() );

    if (!fileName) return;
    FInStream is( fileName );
    if (is.NoFile()) return;
    XMLNode root( is );
    int nEff = root.GetNChildren();
    XMLNode* pChild = root.FirstChild();
    for (int i = 0; i < nEff; i++)
    {
        PEffect* pEff = new PEffect();
        pEff->FromXML( pChild );
        Node* pBone = m_pScene->FindChild<TransformNode>( pEff->GetParentBoneName() );
        if (!pBone) pBone= m_pScene->FindChild<Group>( pEff->GetParentBoneName() );
        if (pBone) pBone->AddChild( pEff );
        pChild = pChild->NextSibling();
    }
} // SceneEditor::LoadEffects

void SceneEditor::ToggleStatistics()
{
	m_pStatMgr->SetInvisible( !m_pStatMgr->IsInvisible() );
}

void SceneEditor::ToggleDrawTerrain()
{
    m_bDrawTerrain = !m_bDrawTerrain;
    ITerra->SetDrawCulling  ( m_bDrawTerrain );
    ITerra->SetDrawGeomCache( m_bDrawTerrain );
    ITerra->SetDrawTexCache ( m_bDrawTerrain );
} // SceneEditor::ToggleDrawTerrain

void SceneEditor::ToggleLeftPane()
{
	m_pTreeL->SetInvisible( !m_pTreeL->IsInvisible() );
	if (!m_pTreeL->IsInvisible()) m_pTreeL->SetExtents( IRS->GetViewPort() );
} // SceneEditor::ToggleLeftPane

void SceneEditor::ToggleRightPane()
{
	m_pTreeR->SetInvisible( !m_pTreeR->IsInvisible() );
	if (!m_pTreeR->IsInvisible()) 
	{
		Rct vp = IRS->GetViewPort();
		m_pTreeR->SetExtents( vp );
		m_pTreeR->SetRootPos( vp.w - 50, vp.h*0.5f );
	}
} // SceneEditor::ToggleRightPane

bool SceneEditor::OnChar( DWORD keyCode, DWORD flags )
{
	if (keyCode == '`') ToggleInvisible();
	return false;
} // SceneEditor::OnChar

bool SceneEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
    if (m_pInspector->HasFocus()) return false;
	if (GetKeyState( VK_CONTROL ) < 0)
	{
		if (keyCode == 'F') 	ToggleStatistics();
		if (keyCode == 'A') 	ToggleInspector();
		if (keyCode == 'Q') 	RecompileShaders();
        if (keyCode == 'W') 	ReloadTextures();
		if (keyCode == 'S') 	SaveSubtree();
		if (keyCode == 'O') 	FlattenStaticHierarchy( GetSelectedNode() );
		if (keyCode == VK_UP)	MoveUpNode();
		if (keyCode == VK_DOWN) MoveDownNode();
		if (keyCode == 'C') 	CopyNode();
		if (keyCode == 'X') 	CutNode();
		if (keyCode == 'V') 	PasteNode();
		if (keyCode == 'L')		LoadSubtree();
        if (keyCode == 'T')		ToggleTempDebugger();
        if (keyCode == 'R')     ToggleGlobalRoot();
        if (keyCode == VK_F1 )  SnapshotCamera();
        if (keyCode == VK_F2 )  UnsnapshotCamera();
		return false;
	}

	if (keyCode == 'L'		) LoadScene();
	if (keyCode == VK_HOME	) ResetCameras();
	if (keyCode == VK_F1	) ToggleLeftPane();
	if (keyCode == VK_F2	) ToggleRightPane();
	if (keyCode == VK_F3	) TogglePalette();
    if (keyCode == VK_F6	) TogglePaletteType();

	if (keyCode == VK_TAB	) ToggleHideScene();
	if (keyCode == 'P'		) ToggleEffectEditor();
	if (keyCode == 'G'		) OptimizeScene();
	if (keyCode == 'C'		) ToggleCameraControl();
	if (keyCode == 'V'		) CreateVIPM();
	if (keyCode == 'O'		) ToggleOptimizations();
	if (keyCode == 'A'		) BindAnimation();
	if (keyCode == VK_PAUSE	) TogglePauseAnimation();

    if (keyCode == 'Q'      ) ApplySelectionTool();
    if (keyCode == 'W'      ) ApplyTranslateTool();
    if (keyCode == 'E'      ) ApplyRotateTool   ();
    if (keyCode == 'R'      ) ApplyScaleTool    ();
    if (keyCode == 'Z'      ) CycleActiveCamera ();
	
	if (keyCode == VK_END	) NodePool::instance().Dump();
	if (keyCode == VK_RETURN) StartProcess();

	if (keyCode == 'T') ToggleDrawTerrain();
	return false;
} // SceneEditor::OnKeyDown

void SceneEditor::ToggleOptimizations()
{
    ProcOptimMode mode = GetProcessorOptimizations();
    if (mode == poNone) mode = poSSE; else mode = poNone;
    SetProcessorOptimizations( mode );
}

void SceneEditor::ToggleInspector()
{
	m_pInspector->SetInvisible( !m_pInspector->IsInvisible() );
}

void SceneEditor::ToggleTempDebugger()
{
    if (!m_pTempDebugger) return;
    m_pTempDebugger->SetInvisible( !m_pTempDebugger->IsInvisible() );
} // SceneEditor::ToggleTempDebugger

void SceneEditor::ToggleInvisible()
{
	Node* pNode = GetSelectedNode();
	if (pNode) pNode->SetInvisible( !pNode->IsInvisible() );
} // SceneEditor::ToggleInvisible

void SceneEditor::TogglePauseAnimation()
{
	Node* pNode = GetSelectedNode();
	if (pNode)
	{
		if (pNode->IsA<Animation>())
		{
			Animation* pAnimation = (Animation*)pNode;
			pAnimation->Pause();
		}
	}
    IEffMgr->Pause( !IEffMgr->IsPaused() );
} // SceneEditor::TogglePauseAnimation

void SceneEditor::StartProcess()
{
	Pipeline* pPipe = (Pipeline*)GetSelectedNode();
	if (pPipe && pPipe->IsA<Pipeline>())
	{
		pPipe->Process();
	}
}

void SceneEditor::CopyNode()
{
	Node* pNode = GetSelectedNode();
	if (!pNode) return;
} // SceneEditor::CopyNode

void SceneEditor::PasteNode()
{
	Node* pNode = GetSelectedNode();
	if (!pNode) return;
} // SceneEditor::PasteNode

void SceneEditor::CutNode()
{
	Node* pNode = GetSelectedNode();
	if (!pNode) return;
} // SceneEditor::CutNode

void SceneEditor::MoveUpNode()
{
	Node* pNode = GetSelectedNode();
	if (!pNode) return;
    Node* pParent = pNode->GetParent();
    if (!pParent) return;
	int cIdx = pParent->GetChildIndex( pNode );
	if (pParent)
	{
		int nIdx = max( cIdx - 1, 0 );
		pParent->SwapChildren( cIdx, nIdx );
        SelectNode( pParent->GetChild( nIdx ) );
    }
} // SceneEditor::MoveUpNode

void SceneEditor::MoveDownNode()
{
	Node* pNode = GetSelectedNode();
	if (!pNode) return;
    Node* pParent = pNode->GetParent();
    if (!pParent) return;
	int cIdx = pParent->GetChildIndex( pNode );
	if (pParent)
	{
		int nIdx = max( cIdx + 1, 0 );
		pParent->SwapChildren( cIdx, nIdx );
        SelectNode( pParent->GetChild( nIdx ) );
    }
} // SceneEditor::MoveDownNode

void SceneEditor::ToggleGlobalRoot()
{
    Node* pRoot = m_pTreeL->GetRootNode();
	if (pRoot == Root::instance())
	{
		m_pTreeL->SetRootNode( m_pScene );
		m_pTreeR->SetRootNode( m_pScene );
	}
	else if (pRoot == m_pScene)
	{
		m_pTreeL->SetRootNode( Root::instance() );
		m_pTreeR->SetRootNode( Root::instance() );
	}
} // SceneEditor::ToggleGlobalRoot

void SceneEditor::ToggleEffectEditor()
{
	m_pEffectEditor->SetInvisible( !m_pEffectEditor->IsInvisible() );
	
	if (m_pEffectEditor->IsInvisible())
	{
		m_pRootSubst = this;
		m_pTreeL->SetRootNode( m_pScene );
		m_pPalTree->SetRootNode( m_pPalette );
		ITerra->Show( false );
	}
	else
	{
        IEffMgr->Pause( false ); 
		m_pRootSubst = m_pEffectEditor;
		m_pTreeL->SetRootNode( m_pEffectEditor->GetEffectRoot() );
		m_pPalTree->SetRootNode( m_pEffectEditor->GetPalette() );
		m_pInspector->SetInvisible( false );
		m_pPalTree->SetInvisible( false );
		ITerra->Show( true );
	}
} // SceneEditor::ToggleEffectEditor

void SceneEditor::TogglePhysicsEditor()
{
	m_pPhysicsEditor->SetInvisible( !m_pPhysicsEditor->IsInvisible() );

	if (m_pPhysicsEditor->IsInvisible())
	{
		m_pRootSubst = this;
		m_pTreeL->SetRootNode( m_pScene );
		m_pPalTree->SetRootNode( m_pPalette );
	}
	else
	{
		m_pRootSubst = m_pPhysicsEditor;
		m_pTreeL->SetRootNode( m_pPhysicsEditor->GetPhysicsRoot() );
		m_pPalTree->SetRootNode( m_pPhysicsEditor->GetPalette() );
		m_pInspector->SetInvisible( false );
		m_pPalTree->SetInvisible( false );
	}
} // SceneEditor::TogglePhysicsEditor

void SceneEditor::TogglePalette()
{
	m_pPalTree->SetInvisible( !m_pPalTree->IsInvisible() );
} // SceneEditor::TogglePalette

void SceneEditor::TogglePaletteType()
{
    if (m_pPalTree->GetRootNode() == m_pPalette)
    {
        m_pPalTree->SetRootNode( m_pMPalette );
    }
    else
    {
        m_pPalTree->SetRootNode( m_pPalette );
    }
} // SceneEditor::TogglePaletteType

void SceneEditor::ResetCameras()
{
	float aspect = IRS->GetViewPort().GetAspect();
	//  default camera
	float volBound = 800.0f;
	float fovx = DegToRad( 80 );
	Vector3D gdir( 0.0f, -cos( c_PI/6.0f ), -sin( c_PI/6.0f ) );
	m_pEditorCamera->SetDirUp( gdir, Vector3D::oZ );
	gdir.reverse();
	float cdist = volBound * 0.8f / tan( fovx * 0.5f );
	gdir *= cdist;
	float zn = 10.0f;
	cdist -= zn;
	float zf = zn + cdist*100.0f;

	m_pEditorCamera->SetPos( gdir );
	m_pEditorCamera->SetPerspFOVx( fovx, aspect, zn, zf );

	//  game camera
	volBound = 1024.0f;
	cdist	 = volBound * 2.0f;
	zn		 = cdist * 0.2f;
	zf		 = zn + cdist*4.0f;

	Vector3D dir( 0.0f, -cos( c_PI/6.0f ), -sin( c_PI/6.0f ) );
	m_pGameOrthoCamera->SetDirUp( dir, Vector3D::oZ );
	dir.reverse();
	dir *= cdist;
	m_pGameOrthoCamera->SetPos( dir );
	m_pGameOrthoCamera->SetOrthoW( volBound, IRS->GetViewPort().GetAspect(), zn, zf );

	//  alternative game camera

	fovx = DegToRad( 20.0f );
	gdir = Vector3D( 0.0f, -cos( c_PI/6.0f ), -sin( c_PI/6.0f ) );
	m_pGamePerspCamera->SetDirUp( gdir, Vector3D::oZ );
	zn = 1200.0f; 
	zf = 10400.0f;
	m_pGamePerspCamera->SetPos( 0.0f, 2500, 1500 );
	m_pGamePerspCamera->SetPerspFOVx( fovx, IRS->GetViewPort().GetAspect(), zn, zf );
} // SceneEditor::ResetCameras

void SceneEditor::ToggleCameraControl()
{
	m_pMController->SetActive( !m_pMController->IsActive() );
	m_pRController->SetActive( !m_pRController->IsActive() );
} // SceneEditor::ToggleCameraControl

void SceneEditor::SnapshotCamera()
{
    sg::BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return;
    FOutStream os( "Models\\camera_snapshot.c2m" );
    if (os.NoFile()) return;
    pCam->SerializeSubtree( os );
} // SceneEditor::SnapshotCamera

void SceneEditor::UnsnapshotCamera()
{
    sg::BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return;
    FInStream is( "Models\\camera_snapshot.c2m" );
    if (is.NoFile()) return;
    BaseCamera* sCam = (BaseCamera*)Node::UnserializeSubtree( is );
    if (!sCam || !sCam->IsA<BaseCamera>()) return;
    pCam->SetProjM( sCam->GetProjM() );
    pCam->SetTransform( sCam->GetTransform() );
    delete sCam;
} // SceneEditor::UnsnapshotCamera

Node* SceneEditor::PickNode( int mX, int mY )
{
    ICamera* pCam = GetCamera();
    if (!pCam) return NULL; 
    Line3D ray;
    pCam->GetPickRay( mX, mY, ray );

	return NULL;
	Iterator it( m_pScene );
	while (it)
	{
		Node* pNode = (Node*)it;
		if (pNode->IsA<Geometry>())
		{
		    Geometry* pGeom = (Geometry*)pNode;
            BaseMesh& bm = pGeom->GetPrimitive();
            int idx = -1;
            Line3D lray( ray );
            Matrix4D tm = GetWorldTM( pGeom );
            lray.Transform( tm );
            float dist = bm.PickPoly( lray, idx );
            if (idx != -1) return pGeom;
		}
		++it;
	}
	return NULL;
} // SceneEditor::PickNode

Geometry* SceneEditor::PickGeom( int mX, int mY, Vector3D& pos )
{
    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return NULL; 
    Line3D ray;
    pCam->GetPickRay( mX, mY, ray );
    Geometry* pNearest = NULL;
    float minDist = FLT_MAX;
    Iterator it( m_pScene, Geometry::FnFilter );
    while (it)
    {
        Geometry* pGeom = (Geometry*)(Node*)it;
        Matrix4D wtm = GetWorldTM( pGeom );
        wtm.inverse();
        Line3D tray( ray );
        tray.Transform( wtm );
        BaseMesh& bm = pGeom->GetPrimitive();
        int tri = -1;
        float dist = bm.PickPoly( tray, tri );
        if (tri > 0 && dist < minDist)
        {
            minDist = dist;
            pNearest = pGeom;
            if (tri < bm.getNPri())
            {
                Vector3D a, b, c;
                bm.GetTriangle( tri, a, b, c );
                pos = a;
                pos += b; pos += c;
                pos /= 3.0f;
            }
        }
        ++it;
    }    
    return pNearest;
} // SceneEditor::PickBone

Bone* SceneEditor::PickBone( int mX, int mY )
{
    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return NULL; 
    Line3D ray;
    pCam->GetPickRay( mX, mY, ray );
    Bone* pNearest = NULL;
    float minDist = FLT_MAX;
    Iterator it( m_pScene, Bone::FnFilter );
    while (it)
    {
        Bone* pBone = (Bone*)(Node*)it;
        Matrix4D wtm = pBone->GetWorldTM();
        Vector3D pos = wtm.getTranslation();
        Vector3D ppos( pos );
        Bone* pParent = (Bone*)pBone->GetParent();
        if (pParent->IsA<Bone>())
        {
            ppos = pParent->GetTopTM().getTranslation();
        }

        float dist = ray.dist2ToPoint( pos );
        if (dist < minDist)
        {
            minDist = dist;
            pNearest = pBone;
        }
        ++it;
    }

    const float c_MinSelDist = 10.0f;
    return (minDist < c_MinSelDist*c_MinSelDist) ? pNearest : NULL;
} // SceneEditor::PickBone

/*****************************************************************/
/*	Class:	IKChain
/*	Desc:	Chain of transforms taking part in the IK solution
/*****************************************************************/
const int c_MaxIKChainLen = 16;
class IKChain
{
    Matrix4D        m_TM[c_MaxIKChainLen];
    int             m_ChainLen;
public:

}; // class IKChain

bool SceneEditor::OnMouseMove( int mX, int mY, DWORD keys )
{
    if (GetKeyState( VK_CONTROL ) >= 0) return false;
    
    //  get target position
    ICamera* pCam = GetCamera();
    if (!pCam) return false;
    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );
    Vector3D pt;

    float x0 = ray.getOrig().x;
    float y0 = ray.getOrig().y;
    float xd = ray.getDir().x;
    float yd = ray.getDir().y;
    float R = 300.0f;
    
    float root = R*R*(xd*xd + yd*yd) - (xd*y0 - x0*yd);
    if (root < 0.0f) return false;
    float t = x0*xd + y0*yd + sqrtf( root );

    //  build IK chain
    std::set<DWORD>::iterator it = m_Selection.begin();
    while (it != m_Selection.end())
    {
        DWORD id = *it;
        Bone* pBone = (Bone*)NodePool::GetNode( id );
        if (!pBone) { ++it; continue; }
        ++it;
    }
    return false;
} // SceneEditor::OnMouseMove


END_NAMESPACE(sg)
