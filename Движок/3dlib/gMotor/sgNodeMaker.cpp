/*****************************************************************************/
/*	File:	sgNodeMaker.cpp
/*	Desc:	Stub file for registering scene node types
/*	Author:	Ruslan Shestopalyuk
/*	Date:	06-26-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kTemplates.hpp"
#include "kInput.h"

#include "sgFont.h"
#include "sgGizmo.h"
#include "sndSoundTrack.h"
#include "uiControl.h"
#include "uiTrackEdit.h"
#include "csCameraController.h"
#include "sgBillboardGroup.h"
#include "sgPipeline.h"
#include "sgSkybox.h"

#include "sgParticleSystem.h"
#include "sgParticleClusters.h"
#include "sgParticleEmitters.h"
#include "sgParticleAffectors.h"

#include "sgSprite.h"
#include "sgSpriteCarcass.h"
#include "csBodyMover.h"
#include "sgFont.h"
#include "sgFog.h"
#include "sgReflection.h"
#include "sgLensFlare.h"
#include "sgCursor.h"
#include "sgHardwareCaps.h"
#include "sgDeviceSettings.h"
#include "sgTexture.h"
#include "sgApplication.h"
#include "sgShadow.h"
#include "sgTerrain.h"
#include "sgDecal.h"
#include "sgStatistics.h"
#include "sgAnimBlend.h"
#include "sgVSPS.h"
#include "sgConstraint.h"
#include "sgSpriteManager.h"

#include "rsDeviceStates.h"
#include "sgStateBlock.h"

#include "uiControl.h"
#include "uiObjectInspector.h"
#include "uiNodeTree.h"
#include "uiTransformTools.h"
#include "uiEffectEditor.h"
#include "uiPhysicsEditor.h"
#include "uiWidgetManager.h"
#include "uiRenderFarm.h"
#include "uiWeightEdit.h"
#include "uiRampEdit.h"
#include "uiPropEditors.h"
#include "uiCameraTrack.h"
#include "uiSceneEditor.h"
#include "uiManipulator.h"

#include "sgSurfaceCache.h"
#include "sgEffect.h"

#include "uiWidget.h"
#include "uiPalette.h"
#include "uiPicture.h"

BEGIN_NAMESPACE(sg)

Node::NodeCreator::NodeCreator()
{
	printf( "" );
	NodeFactory::instance().RegisterNodeType( 'EDON', "Node", "", Node::CreateInstance );	
}

REGNODE( Node				);
REGNODE( TransformNode		);
REGNODE( HudNode			);

REGNODE( Application		);
REGNODE( AppWindow			);

REGNODE( AssetNode			);

REGNODE( Light				);
REGNODE( PointLight			);
REGNODE( DirectionalLight	);
REGNODE( SpotLight			);

REGNODE( GeometryRef		);
REGNODE( Geometry			);
REGNODE( Locator			);
REGNODE( Group				);
REGNODE( OrthoCamera		);
REGNODE( PerspCamera		);
REGNODE( Material			);
REGNODE( Texture	    	);
REGNODE( DeviceStateSet 	);

REGNODE( StateBlock 		);
REGNODE( RenderStateBlock 	);
REGNODE( TextureStateBlock 	);

REGNODE( VertexShader		);
REGNODE( PixelShader		);
REGNODE( VSConstBlock		);
REGNODE( VSConstant			);
REGNODE( VSVectorConstant	);
REGNODE( VSMatrixConstant	);
REGNODE( PSConstBlock		);

REGNODE( Root				);
REGNODE( Canvas				);
REGNODE( Bone				);
REGNODE( SkinnedGeometry	);
REGNODE( MorphedGeometry	);
REGNODE( ZBias				);
REGNODE( Skybox				);
REGNODE( PRSAnimation		);
REGNODE( UVAnimation		);
REGNODE( BaseCamera			);
REGNODE( Controller			);
REGNODE( Animation			);
REGNODE( VectorField		);
REGNODE( SoundTrack			);
REGNODE( Control			);
REGNODE( TreeControl		);
REGNODE( Dialog				);
REGNODE( Button				);
REGNODE( EditBox			);
REGNODE( CheckBox			);
REGNODE( Chart				);
REGNODE( KeyframeChart		);
REGNODE( FloatTrackEdit		);
REGNODE( QuatTrackEdit		);
REGNODE( ColorTrackEdit		);
REGNODE( ColorRampEdit		);
REGNODE( AlphaRampEdit		);
REGNODE( WeightEdit			);
REGNODE( AnimationBlock		);
REGNODE( AnimationSet       );
REGNODE( AnimationBind      );
REGNODE( Sequence			);
REGNODE( CameraController	);
REGNODE( FlyController		);
REGNODE( RTSController		);
REGNODE( MayaController		);
REGNODE( BillboardGroup		);
REGNODE( PipeSection		);
REGNODE( Pipeline			);
REGNODE( PipeData			);
REGNODE( PipeDataBlock		);

//----------------------------
//  brand-new ui
REGNODE( Widget				);
REGNODE( Window				);
REGNODE( ColorGradient		);
REGNODE( PickColor			);
REGNODE( PushButton			);
REGNODE( ChBox				);
REGNODE( Label				);
REGNODE( Slider				);
REGNODE( Progress			);
REGNODE( ScrollBox			);
REGNODE( MainWindow			);
REGNODE( Palette			);
REGNODE( Picture			);
//----------------------------


REGNODE( CarcassObject		);
REGNODE( CarcassShadow		);
REGNODE( CarcassSprite		);
REGNODE( CarcassBuilding	);
REGNODE( SpriteManager		);
REGNODE( ScreenSprite		);
REGNODE( WorldSprite		);

REGNODE( BodyMover			);
REGNODE( HudMover			);
REGNODE( GlyphPage			);
REGNODE( GlyphSet			);
REGNODE( Font				);
REGNODE( IconSet			);
REGNODE( Fog				);
REGNODE( LensFlare			);
REGNODE( LensFlareElement	);
REGNODE( WaterPatch			);
REGNODE( WaterScape			);
REGNODE( Transform2D		);
REGNODE( BumpMatrix			);
REGNODE( TextureMatrix		);
REGNODE( Switch				);
REGNODE( RenderTarget		);
REGNODE( DetailMap			);

//  cursors
REGNODE( Cursor				);
REGNODE( SystemCursor		);
REGNODE( TexturedCursor		);
REGNODE( HardwareCursor		);
REGNODE( FramerateCursor	);

//  system stuff
REGNODE( HardwareCaps		);
REGNODE( DeviceSettings		);
REGNODE( DiskFolder			);

//  kangaroo stuff
REGNODE( ObjectInspector	);
REGNODE( EffectEditor		);
REGNODE( PhysicsEditor		);
REGNODE( StatManager		);
REGNODE( RenderFarm			);

//  manipulators
REGNODE( TransformTool		);
REGNODE( MoveTool			);
REGNODE( RotateTool			);
REGNODE( ScaleTool			);
REGNODE( SelectionTool		);

// shadows
REGNODE( ShadowMapper			);
REGNODE( BlobShadowMapper		);
REGNODE( DBlobShadowMapper		);
REGNODE( ProjectiveShadowMapper );
REGNODE( ShadowVolumeMapper		);
REGNODE( IDShadowMapper			);
REGNODE( ShadowBlob				);
REGNODE( RubberBlob				);
REGNODE( ProjectiveBlob			);

//  terrain
REGNODE( Terrain			);
//  effects
REGNODE( DecalManager		);
REGNODE( ParticleSystem		);
REGNODE( ParticleManager	);

REGNODE( ParticleCluster	);
REGNODE( BillboardCluster	);
REGNODE( QuadCluster		);
REGNODE( LineCluster		);
REGNODE( BeamCluster		);
REGNODE( PlaneCluster		);
REGNODE( PolyObjectCluster	);
REGNODE( MeshPolyCluster	);

REGNODE( ParticleEmitter	);
REGNODE( ConeEmitter		);
REGNODE( LineEmitter		);
REGNODE( TargetEmitter		);
REGNODE( PointEmitter		);
REGNODE( SphereEmitter		);
REGNODE( BoxEmitter			);
REGNODE( CylinderEmitter	);
REGNODE( MeshEmitter		);
REGNODE( TerrainEmitter		);

REGNODE( ParticleAffector		);
REGNODE( DeltaAffector			);
REGNODE( ForceAffector			);
REGNODE( RandomForceAffector	);
REGNODE( RandomTorqueAffector	);
REGNODE( Turbulence				);
REGNODE( VortexAffector			);
REGNODE( ExplodeAffector		);
REGNODE( FollowAffector			);
REGNODE( FluidFrictionAffector	);
REGNODE( MatchVelocityAffector	);
REGNODE( ChainEffectAffector	);
REGNODE( HitAffector			);
REGNODE( PlaneHitAffector		);
REGNODE( LightningAffector		);
REGNODE( ColorAnimateAffector	);
REGNODE( SizeAnimateAffector	);
REGNODE( SphereProjectAffector	);
REGNODE( TerrainHitAffector		);
REGNODE( UVAnimateAffector		);
REGNODE( UVInitAffector			);


REGNODE( Constraint				);
REGNODE( PosConstraint			);
REGNODE( ModelFile				);
REGNODE( AnimationFile			);

//  services
REGNODE( StateBlockManager 		);
REGNODE( ModelManager			);
REGNODE( AnimationManager		);
REGNODE( VertexShaderManager	);
REGNODE( PixelShaderManager 	);
REGNODE( TextureManager			);

REGNODE( SurfaceCache			);
REGNODE( SurfaceCacheItem		);

REGNODE( ReflectionMap			);
REGNODE( Thumbnail				);

REGNODE( SegmentSystem			);
REGNODE( SegmentNode			);

REGNODE( EffectStub				);

REGNODE( Overlay				);

REGNODE( WidgetManager			);
REGNODE( NodeTree				);
REGNODE( CameraPathEditor       );
REGNODE( SceneEditor            );

REGNODE( FieldPatch             );
REGNODE( Model                  );

int DetNodeGlyph( Node* pNode )
{
	if (!pNode) return 0;

	if (pNode->HasFn( "TERC"			  )) return 55;
	if (pNode->HasFn( "TERR"			  )) return 56;
	if (pNode->HasFn( "LENS"			  )) return 57;
	if (pNode->HasFn( "STAT"			  )) return 58;
	if (pNode->HasFn( "GPFR"			  )) return 64;
	if (pNode->HasFn( "CPUN"			  )) return 65;

	//  physics
	if (pNode->HasFn( "RIGB"			  )) return 89;
	if (pNode->HasFn( "COLB"			  )) return 80;
	if (pNode->HasFn( "COLS"			  )) return 84;
	if (pNode->HasFn( "COLC"			  )) return 81;
	if (pNode->HasFn( "CLCC"			  )) return 81;
	if (pNode->HasFn( "CLPL"			  )) return 83;
	if (pNode->HasFn( "CLPR"			  )) return 82;

	if (pNode->HasFn( "JBSO"			  )) return 86;
	if (pNode->HasFn( "JHIN"			  )) return 87;
	if (pNode->HasFn( "JAMO"			  )) return 94;
	if (pNode->HasFn( "JUNI"			  )) return 92;
	if (pNode->HasFn( "JHI2"			  )) return 93;
	if (pNode->HasFn( "JSLI"			  )) return 91;


	if (pNode->HasFn( "PHYJ"			  )) return 85;
	if (pNode->HasFn( "ARTJ"			  )) return 88;
	if (pNode->HasFn( "PHJC"			  )) return 85;
	if (pNode->HasFn( "CLSP"			  )) return 82;

	if (pNode->IsA<ParticleSystem		>()) return 13;
	if (pNode->IsA<ParticleEmitter	>()) return 91;
	if (pNode->IsA<ParticleCluster	>()) return 89;
	if (pNode->IsA<ParticleAffector   >()) return 90;

	if (pNode->IsA<ModelFile			>()) return 88;
	if (pNode->IsA<AnimationFile		>()) return 87;
	if (pNode->IsA<StateBlock			>()) return 10;
	if (pNode->IsA<RenderStateBlock	>()) return 86;
	if (pNode->IsA<TextureStateBlock  >()) return 85;

	if (pNode->IsA<ModelManager		>()) return 79;
	if (pNode->IsA<VertexShaderManager>()) return 80;
	if (pNode->IsA<StateBlockManager  >()) return 81;
	if (pNode->IsA<PixelShaderManager >()) return 82;
	if (pNode->IsA<AnimationManager	>()) return 83;
	if (pNode->IsA<TextureManager		>()) return 84;

	if (pNode->IsA<Locator			>()) return 51;
	if (pNode->IsA<PixelShader		>()) return 77;
	if (pNode->IsA<PSConstBlock		>()) return 78;
	if (pNode->IsA<VertexShader		>()) return 75;
	if (pNode->IsA<VSConstBlock		>()) return 76;

	if (pNode->IsA<DiskFolder			>()) return 73;
	if (pNode->IsA<Font				>()) return 47;
	if (pNode->IsA<TextureMatrix		>()) return 62;
	if (pNode->IsA<GlyphSet			>()) return 61;
	if (pNode->IsA<DecalManager		>()) return 54;
	if (pNode->IsA<PointLight			>()) return 2;
	if (pNode->IsA<DirectionalLight	>()) return 70;
	if (pNode->IsA<SpotLight			>()) return 70;
	if (pNode->IsA<Light				>()) return 20;
	if (pNode->IsA<ZBias				>()) return 33;
	if (pNode->IsA<Material			>()) return 3;
	if (pNode->IsA<Geometry			>()) return 4;
	if (pNode->IsA<Texture			>()) return 5;
	if (pNode->IsA<Canvas			>()) return 8;
	if (pNode->IsA<RenderTarget		>()) return 9;
	if (pNode->IsA<DeviceStateSet		>()) return 10;
	if (pNode->IsA<Root				>()) return 22;
	if (pNode->IsA<Bone				>()) return 31;
	if (pNode->IsA<Application		>()) return 32;
	if (pNode->IsA<DeviceSettings		>()) return 40;
	if (pNode->IsA<SoundTrack			>()) return 35;
	if (pNode->IsA<Pipeline			>()) return 72;
	if (pNode->IsA<PipeSection		>()) return 74;
	if (pNode->IsA<PipeData			>()) return 26; 
	if (pNode->IsA<Skybox				>()) return 15;
	if (pNode->IsA<SystemCursor		>()) return 45;
	if (pNode->IsA<HardwareCursor		>()) return 44;
	if (pNode->IsA<FramerateCursor	>()) return 46;
	if (pNode->IsA<Switch				>()) return 68;
	if (pNode->IsA<AnimationBlock		>()) return 49;
	if (pNode->IsA<PerspCamera		>()) return 1;
	if (pNode->IsA<OrthoCamera		>()) return 50;
	if (pNode->IsA<BaseCamera			>()) return 1;
	if (pNode->IsA<AppWindow			>()) return 52;
	if (pNode->IsA<HardwareCaps		>()) return 53;
	if (pNode->IsA<Animation			>()) return 6;
	if (pNode->IsA<Controller			>()) return 28;
	if (pNode->IsA<TransformNode		>()) return 63;
	if (pNode->IsA<Group				>()) return 71;

	return 17;	
}

Group* CreateMinNodeTemplateSet()
{
    Group* pG = new Group();
    pG->SetName( "Templates" );
    pG->AddChild<TransformNode	    >( "TransformNode"      );
    pG->AddChild<PEffect            >( "Effect"  			);
    pG->AddChild<Texture	    	>( "Texture"	        );	
    pG->AddChild<DeviceStateSet 	>( "DeviceStateSet"     );	
    pG->AddChild<StateBlock	        >( "StateBlock"		    );	
    pG->AddChild<RenderStateBlock   >( "RenderStateBlock"	);	
    pG->AddChild<TextureStateBlock  >( "TextureStateBlock"	);	
    pG->AddChild<PointLight		    >( "PointLight"	        );	
    pG->AddChild<DirectionalLight   >( "DirectionalLight"   );
    pG->AddChild<SpotLight		    >( "SpotLight"          );
    pG->AddChild<ScreenSprite	    >( "ScreenSprite"       );
    pG->AddChild<WorldSprite	    >( "WorldSprite"        );
    pG->AddChild<Picture		    >( "Picture"            );			
    pG->AddChild<Overlay            >( "Overlay"  			);
    return pG;
} // CreateNodeTemplateSet

Group* CreateNodeTemplateSet()
{
	Group* pG = new Group();
	pG->SetName( "Templates" );

	Group* pGeneral = pG->AddChild<Group>( "General" );
	pGeneral->AddChild<TransformNode	>( "TransformNode"  );
	pGeneral->AddChild<Locator			>( "Locator"		);
	pGeneral->AddChild<Group			>( "Group"			);
	pGeneral->AddChild<Bone				>( "Bone"			);
	pGeneral->AddChild<ZBias			>( "ZBias"			);
	pGeneral->AddChild<Switch			>( "Switch"			);
    pGeneral->AddChild<Model			>( "Model"			);

	Group* pShading = pG->AddChild<Group>( "Shading"		);
	pShading->AddChild<Material			>( "Material"		);		
	pShading->AddChild<Texture	    	>( "Texture"	    );	
	pShading->AddChild<DeviceStateSet 	>( "DeviceStateSet" );	
	pShading->AddChild<StateBlock	>	 ( "StateBlock"		);	
	pShading->AddChild<RenderStateBlock	>( "RenderStateBlock"		);	
	pShading->AddChild<TextureStateBlock>( "TextureStateBlock"		);	


	pShading->AddChild<VertexShader		>( "VertexShader"	);	
	pShading->AddChild<PixelShader		>( "PixelShader"	);	
	pShading->AddChild<VSConstBlock		>( "VSConstBlock"	);	
	pShading->AddChild<VSVectorConstant	>( "VSVectorConstant" );	
	pShading->AddChild<VSMatrixConstant	>( "VSMatrixConstant" );	

	pShading->AddChild<PSConstBlock		>( "PSConstBlock"	);	

	pShading->AddChild<BumpMatrix		>( "BumpMatrix"		);
	pShading->AddChild<TextureMatrix	>( "TextureMatrix"	);
	pShading->AddChild<RenderTarget		>( "RenderTarget"	);	
	pShading->AddChild<DetailMap		>( "DetailMap"		);

	Group* pView = pG->AddChild<Group>	( "View" );
	pView->AddChild<Canvas		>	( "Canvas"		);
	pView->AddChild<OrthoCamera		>	( "OrthoCamera"	);
	pView->AddChild<PerspCamera		>	( "PerspCamera"	);	

	Group* pLighting = pG->AddChild<Group>( "Lighting" );
	pLighting->AddChild<PointLight		>( "PointLight"	);	
	pLighting->AddChild<DirectionalLight>( "DirectionalLight" );
	pLighting->AddChild<SpotLight		>( "SpotLight" );

	pLighting->AddChild<BlobShadowMapper		>( "BlobShadowMapper"		);		 
	pLighting->AddChild<DBlobShadowMapper		>( "DBlobShadowMapper"		);		 
	pLighting->AddChild<ProjectiveShadowMapper	>( "ProjectiveShadowMapper" );	 
	pLighting->AddChild<ShadowVolumeMapper		>( "ShadowVolumeMapper"		);		 
	pLighting->AddChild<IDShadowMapper			>( "IDShadowMapper"			);	
	pLighting->AddChild<ShadowBlob				>( "ShadowBlob"				);
	///pLighting->AddChild<RubberBlob				>( "RubberBlob"				);
	pLighting->AddChild<ProjectiveBlob			>( "ProjectiveBlob"			);


	Group* pGeometry = pG->AddChild<Group>( "Geometry" );
	pGeometry->AddChild<Geometry>			( "Geometry" );
	pGeometry->AddChild<SkinnedGeometry>	( "SkinnedGeometry" );
	pGeometry->AddChild<SegmentSystem>		( "SegmentSystem" );
	pGeometry->AddChild<SegmentNode>		( "Segment" );
	
	Group* pAnimation = pG->AddChild<Group>( "Animation" );
    pAnimation->AddChild<AnimationSet   >( "AnimSet"	);
    pAnimation->AddChild<AnimationBind  >( "AnimTrack"	);

	pAnimation->AddChild<PRSAnimation	>( "PRSAnimation"	);
	pAnimation->AddChild<UVAnimation	>( "UVAnimation"	);
	pAnimation->AddChild<SoundTrack		>( "SoundTrack"		);			
	pAnimation->AddChild<AnimationBlock	>( "AnimationBlock"		);			
	pAnimation->AddChild<Sequence		>( "Sequence"		);			
	pAnimation->AddChild<PosConstraint	>( "PosConstraint"	);

	pAnimation->AddChild<FlyController	>( "FlyController"	);		
	pAnimation->AddChild<RTSController	>( "RTSController"	);		
	pAnimation->AddChild<MayaController	>( "MayaController" );	

	Group* pDialog = pG->AddChild<Group>( "Dialog" );
	pDialog->AddChild<Dialog		>( "Dialog"		);			
	pDialog->AddChild<Button		>( "Button"		);				
	pDialog->AddChild<Label			>( "Label"		);					
	pDialog->AddChild<EditBox		>( "EditBox"	);					
	pDialog->AddChild<ScrollBox		>( "ScrollBox"	);				
	pDialog->AddChild<CheckBox		>( "CheckBox"	);
	pDialog->AddChild<Thumbnail		>( "Thumbnail"	);
	pDialog->AddChild<Palette		>( "Palette"	);

	Group* pUI = pG->AddChild<Group>( "Brand-new ui" );
	pUI->AddChild<Window		>( "Window"			);			
	pUI->AddChild<PushButton	>( "PushButton"		);		
	pUI->AddChild<ChBox			>( "CheckBox"		);		
	pUI->AddChild<Label			>( "Label"			);			
	pUI->AddChild<Slider		>( "Slider"			);		
	pUI->AddChild<Progress		>( "Progress"		);			
	pUI->AddChild<ScrollBox		>( "ScrollBox"		);			
	pUI->AddChild<Palette		>( "Palette"		);			
	pUI->AddChild<Picture		>( "Picture"		);			
	pUI->AddChild<PickColor		>( "PickColor"		);		
	pUI->AddChild<ColorGradient >( "ColorGradient"	);	

	pUI->AddChild<ColorRampEdit >( "ColorRampEdit"	);	
	pUI->AddChild<AlphaRampEdit >( "AlphaRampEdit"	);	
	pUI->AddChild<WeightEdit >( "WeightEdit"	);	

	
	Group* pSprites = pG->AddChild<Group>( "Sprites" );
	pSprites->AddChild<ScreenSprite	>	( "ScreenSprite" );
	pSprites->AddChild<WorldSprite	>	( "WorldSprite" );
	pSprites->AddChild<GlyphSet		>	( "GlyphSet" );		
	pSprites->AddChild<Font			>	( "Font" );			
	pSprites->AddChild<IconSet		>	( "IconSet" );	
	pSprites->AddChild<BillboardGroup>	( "BillboardGroup" );
	pSprites->AddChild<Picture		>	( "Picture" );			

	Group* pCursors = pG->AddChild<Group>( "Cursors" );
	pCursors->AddChild<SystemCursor		>( "SystemCursor" );		
	pCursors->AddChild<HardwareCursor	>( "HardwareCursor" );	
	pCursors->AddChild<FramerateCursor	>( "FramerateCursor" );	

	Group* pEnvironment = pG->AddChild<Group>( "Environment" );
	//pEnvironment->AddChild<Terrain			>( "Terrain" );
	pEnvironment->AddChild<WaterPatch		>( "WaterPatch" );
	pEnvironment->AddChild<WaterScape		>( "WaterScape" );
	pEnvironment->AddChild<LensFlare		>( "LensFlare" );
	pEnvironment->AddChild<LensFlareElement	>( "LensFlareElement" );
	pEnvironment->AddChild<Fog				>( "Fog" );
	pEnvironment->AddChild<Skybox			>( "Skybox" );
	pEnvironment->AddChild<ReflectionMap	>( "ReflectionMap" );
    pEnvironment->AddChild<FieldPatch	    >( "FieldPatch" );

	Group* pParticles = pG->AddChild<Group>( "Particles" );
	pParticles->AddChild<PEffect>		( "Effect"  			);
	pParticles->AddChild<Overlay>		( "Overlay"  			);


	Group* pEmitters = pParticles->AddChild<Group>( "Emitters" );
	pEmitters->AddChild<ConeEmitter>	( "Cone"		);
	pEmitters->AddChild<PointEmitter>	( "Point"		);
	pEmitters->AddChild<SphereEmitter>	( "Sphere"		);
	pEmitters->AddChild<BoxEmitter>		( "Box"			);
	pEmitters->AddChild<CylinderEmitter>( "Cylinder"	);
	pEmitters->AddChild<LineEmitter>	( "Line"		);
	pEmitters->AddChild<TargetEmitter>	( "Connect"		);
	pEmitters->AddChild<MeshEmitter>	( "Mesh"		);
	pEmitters->AddChild<TerrainEmitter>	( "Terrain"		);

	Group* pAffectors = pParticles->AddChild<Group>( "Affectors" ); 
	pAffectors->AddChild<DeltaAffector>			( "Delta"				);
	pAffectors->AddChild<ForceAffector>			( "Force"				);
	pAffectors->AddChild<RandomForceAffector>	( "RandomForce"			);
	pAffectors->AddChild<RandomTorqueAffector>	( "RandomTorque"		);
	pAffectors->AddChild<Turbulence>			( "Turbulence"			);
	pAffectors->AddChild<VortexAffector>		( "Vortex"				);
	pAffectors->AddChild<FollowAffector>		( "Follow"				);
	pAffectors->AddChild<ExplodeAffector>		( "Explode"				);
	pAffectors->AddChild<FluidFrictionAffector>	( "FluidFriction"		);
	pAffectors->AddChild<PlaneHitAffector>		( "PlaneHit"			);
	pAffectors->AddChild<LightningAffector>		( "Lightning"			);
	pAffectors->AddChild<MatchVelocityAffector>	( "MatchVelocity"		);
	pAffectors->AddChild<ChainEffectAffector>	( "ChainEffect"			);
	pAffectors->AddChild<ColorAnimateAffector>	( "AnimateColor"		);
	pAffectors->AddChild<SizeAnimateAffector>	( "AnimateSize"			);
	pAffectors->AddChild<SphereProjectAffector>	( "SphereProjection"	);
	pAffectors->AddChild<TerrainHitAffector>	( "TerrainHit"			);
	pAffectors->AddChild<UVAnimateAffector>		( "UVAnimateAffector"	);

	Group* pClusters = pParticles->AddChild<Group>( "Clusters" ); 
	pClusters->AddChild<BillboardCluster>		( "Billboard"	);
	pClusters->AddChild<PlaneCluster>	 		( "Plane"		);
	pClusters->AddChild<QuadCluster>	 		( "Quad"		);
	pClusters->AddChild<LineCluster>	 		( "Line"		);
	pClusters->AddChild<BeamCluster>	 		( "Beam"		);
	pClusters->AddChild<PolyObjectCluster>	 	( "PolyObject"	);
	pClusters->AddChild<MeshPolyCluster>	 	( "MeshPoly"	);

	pParticles->AddChild<ParticleSystem>( "ParticleSystem" );

	Group* pTemp = pG->AddChild<Group>( "Temp" );
	pTemp->AddChild<SurfaceCache>( "SurfaceCache" );

	return pG;
}

END_NAMESPACE(sg)