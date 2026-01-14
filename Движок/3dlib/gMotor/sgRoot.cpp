/*****************************************************************************/
/*	File:	sgRoot.cpp
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#include "stdafx.h"
#include "sgDummy.h"
#include "sgRoot.h"

#include "csCameraController.h"

#include "sgApplication.h"
#include "sgDeviceSettings.h"
#include "sgHardwareCaps.h"

#include "sgFont.h"
#include "sgCursor.h"
#include "sgPipeline.h"
#include "sgShadow.h"
#include "sgDecal.h"
#include "sgFog.h"

#include "mHeightmap.h"

#include "sgStatistics.h"
#include "sgReflection.h"
#include "sndSoundTrack.h"
#include "IMediaManager.h"

#include "rsDeviceStates.h"
#include "sgStateBlock.h"
#include "sgAnimBlend.h"
#include "sgParticleSystem.h"
#include "sgTerrain.h"

#include "sgSpriteManager.h"
#include "sgVSPS.h"
#include "sgEffect.h"

#include "uiControl.h"

#include "uiNodeTree.h"
#include "uiEffectEditor.h"
#include "uiPhysicsEditor.h"
#include "uiObjectInspector.h"
#include "uiSceneEditor.h"

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Root implementation
/*****************************************************************************/
Root::Root() : Node()
{
	AddRef();
	SetImmortal();
	SetName( "Root" );

	bool res = Vector3D( 4.0f, 0.0f, 0.0f ).isColinear( Vector3D::oX );
	res = Vector3D( 4.0f, 1.0f, 0.0f ).isColinear( Vector3D::oX );

	AABoundBox aabb( Vector3D::null, 100.0f );
	for (int i = 0; i < 1000; i++)
	{
		Plane pl;
		pl.normal() = Vector3D::GetRandomDir();
		pl.d = rndValuef( -100.0f, 100.0f );
		
		Triangle tri;
		Vector3D pt[2];
		tri.Random( aabb, 10.0f, 100.0f );
		int nClip = tri.Clip( pl, pt );
		
		bool res1 = pl.Contains( pt[0] );
		bool res2 = pl.Contains( pt[1] );

		float dt1 = pt[0].dot( pl.normal() ) + pl.d;
		float dt2 = pt[1].dot( pl.normal() ) + pl.d;

	}	
} // Root constructor

Root::~Root()
{
}

void Root::CreateGuts()
{
	AddChild( CreateConfig()			);
	AddChild( CreateServices()			);			
	AddChild( CreateTemplates()			);
	AddChild( CreateGameSceneSetup()	);	
	AddChild( CreateEditorSceneSetup()	);	
	AddChild( CreateFrameContainer()	);	
	AddChild( CreateEditors()			);

#ifdef _TRACE
	Log.Info( "Root::CreateGuts" );
#endif // _TRACE

} // Root::CreateGuts

void Root::Render()
{
#ifdef _TRACE
	Log.Info( "In Root::Render" );
#endif // _TRACE

	if (GetNChildren() == 0) CreateGuts();
	IRS->DisableLights();
	IRS->SetFog( NULL );
	IRS->ResetWorldMatrix();

	Animation::SetupTimeDelta();

	Node::Render();

	//  flush primitive buckets
	rsFlushLines3D();
	rsFlushLines2D();
	rsFlushPoly3D();
	rsFlushPoly2D();

#ifdef _TRACE
	Log.Info( "Out Root::Render" );
#endif // _TRACE

} // Root::Render

Group* Root::CreateConfig()
{
	Group* pG = new Group();
	pG->SetName( "System" );
	pG->AddChild( Application	::instance() );
	pG->AddChild( AppWindow		::instance() );
	pG->AddChild( DeviceSettings::instance() );
	pG->AddChild( HardwareCaps	::instance() );
	pG->AddChild( CPU			::instance() );
	return pG;
} // Root::CreateConfig

Group* Root::CreateEditorSceneSetup()
{
	if (!IRS) return NULL;

	Group* pG = new Group();
	pG->SetName( "EditorSetup" );

	//  far camera
	PerspCamera* pFCam = new PerspCamera();
	pFCam->SetName( "FarCamera" );
	pFCam->SetPos( Vector3D( 0.0f, 0.0f, 100.0f ) );
	pFCam->SetDirUp( Vector3D::oY, Vector3D::oZ );
	pFCam->SetPerspFOVx( c_HalfPI, IRS->GetViewPort().GetAspect(), 300.0f, 10000.0f );
	pG->AddChild( pFCam );
	pFCam->SetInvisible();

	FlyController* pFFCam = pG->AddChild<FlyController>( "FlyController" );
	pFFCam->SetActive( true );
	pFFCam->AddInput( pFCam );

	//  default material
	Material* pMtl = new Material();
	pMtl->SetDiffuse	( 0xFFFFFFFF );
	pMtl->SetAmbient	( 0xFFFFFFFF );
	pMtl->SetSpecular	( 0xFFE5E5E5 );
	pMtl->SetShininess	( 30.0f		 );

	pMtl->SetName		( "DefaultMaterial" );
	pG->AddChild		( pMtl );

	//  default device state set
	DeviceStateSet* pDss = new DeviceStateSet( "Default" );
	pG->AddChild( pDss );

	//  default light
	Light* pLight = new DirectionalLight();
	pLight->SetName		( "DefaultLight"	);
	pLight->SetDiffuse	( 0xFF808080		);
	pLight->SetAmbient	( 0xFF606060		);
	pLight->SetSpecular	( 0xFFFFFFFF		);
	
	Vector3D ldir;
	ldir.set( -10.0f, -5.0f, -5.0f );
	ldir.normalize();
	
	pLight->SetDir		( ldir );
	pLight->SetPos		( Vector3D( -170.0f, 95.0f, 120.0f ) );
	pG->AddChild( pLight );

	//  default texture
	Texture* pTex0 = new Texture( "white.tga" );
	Texture* pTex1 = new Texture( "white.tga" );
	pTex1->SetStage( 1 );
	pG->AddChild( pTex0 );
	pG->AddChild( pTex1 );

	return pG;
} // Root::CreateEditorSceneSetup

Group* Root::CreateGameSceneSetup()
{
	if (!IRS) return NULL;

	Group* pG = new Group();
	pG->SetName( "GameSetup" );
	pG->SetInvisible();

	Canvas* svp = new Canvas();
	svp->SetExtents( 0.0f, 0.0f, 1024.0f, 768.0f );
	svp->SetBgColor( 0x00000000 );

	svp->SetName( "GameViewport" );
	//svp->SetViewportFlag( Canvas::vfClearColor );
	//svp->SetViewportFlag( Canvas::vfClearDepth );
	pG->AddChild( svp );
	
	//  game camera
	OrthoCamera* pGCam = new OrthoCamera();
	pGCam->SetName( "GameCamera" );
	pGCam->SetTweakAspect( false );

	float volBound = 1024.0f;
	float cdist = volBound * 2.0f;
	float zn = cdist * 0.5f;
	float zf = zn + cdist;

	Vector3D dir( 0.0f, -cos( c_PI/6.0f ), -sin( c_PI/6.0f ) );
	pGCam->SetDirUp( dir, Vector3D::oZ );
	dir.reverse();
	dir *= cdist;
	pGCam->SetPos( dir );
	pGCam->SetOrthoW( volBound, IRS->GetViewPort().GetAspect(), zn, zf );

	pG->AddChild( pGCam );

	//  alternative game camera
	PerspCamera* pCam = pG->AddChild<PerspCamera>( "AltCamera" );
	pCam->SetTweakAspect( false );

	float fovx = DegToRad( 20.0f );
	Vector3D gdir( 0.0f, -cos( c_PI/6.0f ), -sin( c_PI/6.0f ) );
	pCam->SetDirUp( gdir, Vector3D::oZ );
	zn = 1200.0f; 
	zf = 10400.0f;
	pCam->SetPos( 0.0f, 2500, 1500 );
	pCam->SetPerspFOVx( fovx, IRS->GetViewPort().GetAspect(), zn, zf );

	MayaController* pCtl = pG->AddChild<MayaController>( "GameController" );
	pCtl->SetActive( true );
	pCtl->AddInput( pGCam );

	//  default material
	Material* pMtl = new Material();
	pMtl->SetDiffuse	( 0xFFFFFFFF );
	pMtl->SetAmbient	( 0xFFFFFFFF );
	pMtl->SetSpecular	( 0xFFFFFFFF );
	pMtl->SetShininess	( 70.0f		 );

	pMtl->SetName		( "GameMaterial" );
	pG->AddChild		( pMtl );

	//  default light
	Light* pLight = new DirectionalLight();
	pLight->SetName		( "GameLight"	);
	pLight->SetDiffuse	( 0xFFFFFFFF	);
	pLight->SetAmbient	( 0xFFFFFFFF	);
	pLight->SetSpecular	( 0xFFFFFFFF	);

	Vector3D ldir;
	ldir.set( -10.0f, -5.0f, -5.0f );
	ldir.normalize();

	pLight->SetDir		( ldir );
	pLight->SetPos		( Vector3D( -170.0f, 95.0f, 120.0f ) );
	pG->AddChild( pLight );

	return pG;
} // Root::CreateGameSceneSetup

Group* Root::CreateFrameContainer()	
{
	Group* pG = new Group();
	pG->SetName( "Frame" );
    SceneEditor* pEd = pG->AddChild<SceneEditor>( "SceneEditor" );
	
	WaterPatch* pWater = pG->AddChild<WaterPatch>( "Water" );
	pWater->SetInvisible();

	Node* pTerrEd = pG->AddChild( "TERG", "TerraEditor" );
	if (pTerrEd)
	{
		pTerrEd->SetInvisible();
	}

	pG->AddChild( ParticleManager::instance() );

	SpriteManager* pSpriteManager = (SpriteManager*)GetSpriteManager();
	pG->AddChild( pSpriteManager );
	//if (pSpriteManager) pSpriteManager->Init();

	ReflectionMap* pMap = pG->AddChild<ReflectionMap>( "ReflectionMap" );
	pMap->Init();
	IRMap = pMap;

	PEffectMgr* pEffMgr = (PEffectMgr*)IEffMgr; 
	pG->AddChild( pEffMgr );

	Thumbnail* pThumb = pG->AddChild<Thumbnail>( "Thumbnail" );
	pThumb->SetInvisible();

	Node* pPhysMgr = pG->AddChild( "PHYS", "Physics" );
	return pG;
} // Root::CreateFrameContainer

Group* Root::CreateServices()
{
	Group* pG = new Group();
	pG->SetInvisible();
	pG->SetName( "Services" );

	pG->AddChild( ModelManager::instance() );
	pG->AddChild( AnimationManager::instance() );

	pG->AddChild( StateBlockManager::instance() );
	pG->AddChild( TextureManager::instance() );
	pG->AddChild( VertexShaderManager::instance() );
	pG->AddChild( PixelShaderManager::instance() );

    Terrain* pGround = pG->AddChild<Terrain>( "Terrain" );
	pGround->SetInvisible();
	
	return pG;
} // Root::CreateServices

Group* CreateNodeTemplateSet();
Group* Root::CreateTemplates()
{
	Group* pG = CreateNodeTemplateSet();
	pG->SetInvisible();
	return pG;
} // Root::CreateTemplates

Group* Root::CreateEditors()
{
	Group* pG = new Group();
	pG->SetName( "Editors" );

	Canvas* pVP = new Canvas();
	pVP->SetExtents( 0.0f, 0.0f, 1024.0f, 768.0f );
	pVP->SetBgColor( 0x00000000 );

	pVP->SetName( "EditorsViewPort" );
	pG->AddChild( pVP );

	Pipeline* pCarc = (Pipeline*)pG->AddChild( "CARC", "Carcass Compiler" );
	if (pCarc) pCarc->Construct();
	if (pCarc) pCarc->SetInvisible( false );

	Node* pGluer = pG->AddChild( "GLUE", "Gluer" );	
	return pG;
} //  Root::CreateEditors

END_NAMESPACE( sg )