/*****************************************************************************/
/*	File:	uiEffectEditor.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	11-23-2003
/*****************************************************************************/
#include "stdafx.h"
#include "uiControl.h"
#include "sgEffect.h"
#include "uiNodeTree.h"
#include "uiEffectEditor.h"
#include "ITerrain.h"
#include "kSystemDialogs.h"
#include "IWidgetManager.h"

extern sg::PEffectMgr g_EMgr;
BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	EffectStub implemetation
/*****************************************************************************/
void EffectStub::OnChangeChildren()
{
	m_EffectID = 0xFFFFFFFF;
	for (int i = 0; i < GetNChildren(); i++)
	{
		Node* pEmitter = GetChild( i );
		if (!pEmitter->IsA<PEmitter>()) 
		{
			RemoveChild( pEmitter );
		}
		else
		{
			m_EffectID = pEmitter->GetID();
		}
	}
} // EffectStub::OnChangeChildren

void EffectStub::Render()
{
    Node::Render();
} // EffectStub::Render

/*****************************************************************************/
/*	EffectEditor implementation
/*****************************************************************************/
EffectEditor::EffectEditor()
{
	m_pPalette = CreateTemplateGroup();
	m_pPalette->SetInvisible();
	m_pEffect = AddChild<EffectStub>( "Effect" );

	Rct ext = IRS->GetViewPort();
	ext.x = ext.w - 90;
	ext.w = 83;
	ext.y = 100.0f;
	ext.h = 300.0f;
	SetExtents( ext );

	m_ClrTop		    = 0x2DFFFFFF;
	m_ClrMdl		    = 0x2DD6D3CE;
	m_ClrBot		    = 0x2D848284;
	m_InstanceID	    = -1;
	m_EffectID		    = -1;

    m_AlphaFactor       = 1.0f;
    m_IntensityFactor   = 1.0f;
    m_CurShotTime       = 0.0f;
    m_bShot             = false;
    m_bTerraVisible     = true;
    m_PlayRate          = 1.0f;

    SetEffectFireMode( fmParabolic );

	FInStream is( "Models\\EffectEditor.c2m" );
	m_pBackScene = Node::UnserializeSubtree( is );

	SetActive();
} // EffectEditor::EffectEditor

Node* EffectEditor::GetEffectRoot()
{ 
	return m_pEffect; 
}

Group* EffectEditor::CreateTemplateGroup()
{
	Group* pCreate = new Group();
	pCreate->SetName( "Create" );	
	Group* pEmitter = pCreate->AddChild<Group>( "Emitter" );
	pEmitter->AddChild<PModularEmitter>		( "Modular"				);
	pEmitter->AddChild<PStaticEmitter>		( "Static" 				);
	pEmitter->AddChild<PConstEmitter>		( "Const"  				);
	pEmitter->AddChild<PBurstEmitter>		( "Burst"  				);
	pEmitter->AddChild<PRampEmitter>		( "Ramp"    			);

	Group* pRender = pCreate->AddChild<Group>( "Render" );
	pRender->AddChild<PBillboardRenderer>	( "Billboard"			);
	pRender->AddChild<PChainRenderer>		( "Chain"				);
	pRender->AddChild<PSphereRenderer>		( "Sphere"				);
	pRender->AddChild<PConeRenderer>		( "Cone"				);
	pRender->AddChild<PTerrainDecal>		( "TerraDecal"			);
	pRender->AddChild<PModelRenderer>		( "Model"				);

	Group* pInit = pCreate->AddChild<Group>( "Init" );
	pInit->AddChild<PSizeInit>				( "SizeInit"	  		);
	pInit->AddChild<PColorInit>				( "ColorInit"	  		);
	pInit->AddChild<PColorRampInit>			( "ColorRamp"	  		);
	pInit->AddChild<PFrameInit>				( "FrameInit"	  		);

	Group* pPlacer = pCreate->AddChild<Group>( "Placer" );
	pPlacer->AddChild<PSpherePlacer>		( "Sphere"				);
	pPlacer->AddChild<PBoxPlacer>			( "Box"				    );
	pPlacer->AddChild<PCylinderPlacer>		( "Cylinder"			);
	pPlacer->AddChild<PLinePlacer>			( "Line"				);
	pPlacer->AddChild<PCirclePlacer>		( "Circle"				);
	pPlacer->AddChild<PPointPlacer>			( "Point"				);
    pPlacer->AddChild<PModelPlacer>			( "Model"				);
    pPlacer->AddChild<PCoastLinePlacer>		( "CoastLine"		    );

	Group* pShooter = pCreate->AddChild<Group>( "Shooter" );
	pShooter->AddChild<PConeShooter>		( "Cone"	  			);
	pShooter->AddChild<PRadialShooter>		( "Radial" 				);
	pShooter->AddChild<PDirectShooter>		( "Direct" 				);
	pShooter->AddChild<PRampShooter>		( "Ramp" 				);

	Group* pVelo = pCreate->AddChild<Group>	( "Velocity" );
	pVelo->AddChild<PForce>					( "Force"				);
    pVelo->AddChild<PAttract>				( "Attract"				);
	pVelo->AddChild<PTorque>				( "Torque"				);
    pVelo->AddChild<PVelRamp>				( "VelRamp"			    );
    pVelo->AddChild<PAVelRamp>				( "AVelRamp"			);
	pVelo->AddChild<PWind>					( "Wind"				);
	pVelo->AddChild<PFollow>				( "Follow"				);
	pVelo->AddChild<PFluctuate>				( "Fluctuate"			);
	pVelo->AddChild<PDrag>					( "Drag"				);
	pVelo->AddChild<PVortex>				( "Vortex"				);
	pVelo->AddChild<POrbit>					( "Orbit"				);
	pVelo->AddChild<PTurbulence>			( "Turbulence"  		);
	pVelo->AddChild<PClampVelocity>			( "ClampV"				);
    pVelo->AddChild<PCoastBreak>			( "CoastBreak"  		);

	Group* pColor = pCreate->AddChild<Group>( "Color" );
	pColor->AddChild<PColorRamp>			( "ColorRamp"			);
	pColor->AddChild<PAlphaFade>			( "AlphaFade"			);
	pColor->AddChild<PAlphaRamp>			( "AlphaRamp"			);

	Group* pSize = pCreate->AddChild<Group>( "Size" );
	pSize->AddChild<PSizeFade>				( "SizeFade"			);
	pSize->AddChild<PSizeRamp>				( "SizeRamp"			);

	Group* pTex = pCreate->AddChild<Group>( "Texture" );
	pTex->AddChild<PFrame>					( "FrameAnim"			);
	pTex->AddChild<PUVMove>					( "UVMove"				);

	Group* pTrigger = pCreate->AddChild<Group>( "Trigger" );
	pTrigger->AddChild<POnDeath>			( "Death"  				);
	pTrigger->AddChild<POnBirth>			( "Birth"  				);
	pTrigger->AddChild<POnTimer>			( "Timer"  				);
	pTrigger->AddChild<POnHitGround>		( "HitGround"  			);
	pTrigger->AddChild<POnHitWater>			( "HitWater"  			);

	Group* pTarget = pCreate->AddChild<Group>( "Target" );
    pTarget->AddChild<PRetarget>			( "Retarget"  			);
	pTarget->AddChild<PLightning>			( "Lightning"  			);
	pTarget->AddChild<PHoming>				( "Homing"  			);

	Group* pMisc = pCreate->AddChild<Group>( "Misc" );
	pMisc->AddChild<PMouseBind>				( "MouseBind"			);
	pMisc->AddChild<PEffect>				( "Instance"			);

	return pCreate;
} // EffectEditor::CreateTemplateGroup

void EffectEditor::SetEffectFireMode( EffectFireMode fm )
{
    if (fm == fmParabolic)
    {
        m_ShotTime      = 5.0f;
        m_ShotVelocity  = 200.0f;
        m_ShotAngle     = c_PI/4.0f;
        m_ShotHeight    = 30.0f;
        m_ShotGravity   = 100.0f;
    }
    else if (fm == fmGroundMove)
    {
        m_ShotTime      = 500.0f;
        m_ShotVelocity  = 50.0f;
        m_ShotAngle     = 0.0f;
        m_ShotHeight    = 30.0f;
        m_ShotGravity   = 0.0f;
    }
    m_FireMode = fm;
} // EffectEditor::SetEffectFireMode

void EffectEditor::Expose( PropertyMap& pm )
{
	pm.start( "PEffect", this );
    pm.f( "AlphaFactor",        m_AlphaFactor       );    
    pm.f( "IntensityFactor",    m_IntensityFactor   );    
    pm.f( "ShotTime",           m_ShotTime          );
    pm.f( "ShotVelocity",       m_ShotVelocity      );
    pm.f( "ShotAhgle",          m_ShotAngle         );
    pm.f( "ShotHeight",         m_ShotHeight        );
    pm.f( "ShotGravity",        m_ShotGravity       );
    pm.p( "FireMode",           GetEffectFireMode, SetEffectFireMode );
    pm.m( "Fire!",			    Fire			    );

	pm.p( "ShowTerrain",	    IsTerraVisible, SetTerraVisible );
	pm.p( "ShowBackdrop",	    ShowBackdrop,	ShowBackdrop	);
	pm.p( "ShowGrid",		    ShowGrid,		ShowGrid		);
	pm.p( "Camera",			    GetCameraMode,	SetCameraMode	);

	pm.p( "TotalTime",		    GetTotalTime	    );
	pm.p( "CurrentTime",	    GetCurTime		    );
    pm.p( "PlayRate",           GetPlayRate, SetPlayRate );
	pm.p( "EffectFile",		    GetEffectFile	    );
	pm.m( "Reset",			    Reset			    );
	pm.m( "Load",			    Load			    );
	pm.m( "Save",			    Save			    );
	pm.m( "SaveAs", 		    SaveAs			    );
	pm.m( "Play",			    PlayEffect		    );
    pm.m( "Pause",              Pause               );
	pm.m( "Stop",			    StopEffect		    );
} // EffectEditor::Expose

void EffectEditor::Fire()
{
    PlayEffect();
    m_bShot = true;
    m_CurShotTime = 0.0f;
    m_StartShotTime = GetTickCount();
} // EffectEditor::Fire

float EffectEditor::GetPlayRate() const
{
    return m_PlayRate;
}

void EffectEditor::SetPlayRate( float rate )
{
    m_PlayRate = rate;
    IEffMgr->SetPlayRate( rate );
}

void EffectEditor::Render()
{
    Matrix4D tm = Matrix4D::identity;

    if (m_bShot)
    {
        DWORD tick = GetTickCount();
        float tPass = tick - m_StartShotTime;
        tPass *= 0.001f;
        if (tPass > m_ShotTime)
        {
            StopEffect();
            m_bShot = false;
            return;
        }
        m_CurShotTime = tPass;
        float x = m_ShotVelocity*cosf( m_ShotAngle )*m_CurShotTime;
        float y = m_ShotVelocity*sinf( m_ShotAngle )*m_CurShotTime - 
                        0.5f*m_ShotGravity*m_CurShotTime*m_CurShotTime;
        tm.e30 = x;
        tm.e32 = y;

        Vector3D vX( m_ShotVelocity*cosf( m_ShotAngle ), 0.0f, 
                     m_ShotVelocity*sinf( m_ShotAngle ) - m_ShotGravity*m_CurShotTime );
        Vector3D vZ( Vector3D::oY );
        Vector3D vY;
        vX.normalize();
        vY.cross( vX, vZ );
        vY.normalize();
        tm.getV0() = vX;
        tm.getV1() = vY;
        tm.getV2() = vZ;
    }

	if (m_pBackScene && !m_pBackScene->IsInvisible()) m_pBackScene->Render();
    if (ITerra && m_bTerraVisible) ITerra->Render();
	if (m_EffectID != m_pEffect->m_EffectID) 
	{
		m_EffectID = m_pEffect->m_EffectID;
		m_InstanceID = IEffMgr->InstanceEffect( m_EffectID );
	}
	
    //  updating effect
    if (m_pEffect->GetNChildren() > 0) 
    {
        IEffMgr->SetAlphaFactor ( m_InstanceID, m_AlphaFactor );
        IEffMgr->SetIntensity   ( m_InstanceID, m_IntensityFactor );
        IEffMgr->UpdateInstance ( m_InstanceID, tm );
    }

    //  render effect tree, for drawing gizmos, etc.
    m_pEffect->Render();

    //  render statistics
    char text[256];
    static s_FontID = IWM->CreateFont( "Tahoma", 8 );
    
    Vector3D pos( 10, 60, 0.0f );
    sprintf( text, "Emitters:  %d", IEffMgr->GetNEmitterInst() );
    pos.y += 10;
    IWM->DrawString( s_FontID, text, pos, 0xFFFFFFBB );

    sprintf( text, "Particles:  %d", IEffMgr->GetNParticles() );
    pos.y += 10;
    IWM->DrawString( s_FontID, text, pos, 0xFFFF33FF );

} // EffectEditor::Render

bool EffectEditor::OnChar( DWORD charCode, DWORD flags )
{
	if (IsInvisible()) return false;
	if (charCode == ' ' && m_pEffect) PlayEffect();
	return false;
} // EffectEditor::OnChar

bool EffectEditor::OnKeyDown( DWORD keyCode, DWORD flags )
{
    if (IsInvisible()) return false;
    if (keyCode == VK_RETURN && m_pEffect) Fire();
    return false;
} // EffectEditor::OnKeyDown

void EffectEditor::SetTerraVisible( bool val )
{
	ITerra->Show( val );
    m_bTerraVisible = val;
}

bool EffectEditor::IsTerraVisible() const
{
	return ITerra->IsShown();
}

void EffectEditor::Reset()
{
	IEffMgr->DestroyInstance( m_InstanceID );
	m_InstanceID = 0xFFFFFFFF;
	m_EffectID	 = 0xFFFFFFFF;
	m_EffectFile = "";
	m_pEffect->RemoveChildren();
	IEffMgr->Reset();
} // EffectEditor::Reset

void EffectEditor::Pause()
{
    IEffMgr->Pause( !IEffMgr->IsPaused() );
} // EffectEditor::Pause

void EffectEditor::Load()
{
	const char* fileName = NULL;

	_chdir( GetRootDirectory() );
	_chdir( "Models\\Effects\\" );
	OpenFileDialog dlg;
	dlg.AddFilter( "Binary Model Files", "*.c2m" );
	dlg.AddFilter( "XML Effect Files", "*.eff" );
	dlg.SetDefaultExtension( "c2m" );

	static char lpstrFile[_MAX_PATH];
	bool dlgRes = dlg.Show();
	_chdir( GetRootDirectory() );
	RelaxDialog();
	if (!dlgRes) return;

	fileName = dlg.GetFilePath();
	if (fileName)
	{
		FInStream is( fileName );
		Node* pEffect = Node::UnserializeSubtree( is );
		if (!pEffect) 
		{
			Log.Error( "Could not find effect file: %s", fileName );
			return;
		}
		if (!pEffect->IsA<PEmitter>())
		{
			Log.Error( "Bad effect file: %s", fileName );
			return;
		}

		Reset();

		char drive		[_MAX_DRIVE];
		char directory	[_MAX_DIR  ];
		char fname		[_MAX_PATH ];
		char ext		[_MAX_EXT  ];

		_splitpath( fileName, drive, directory, fname, ext );
		strcat( fname, ext );

		m_EffectID = pEffect->GetID();
		m_pEffect->AddChild( pEffect );
		PlayEffect();
		m_EffectFile = fname;
	}
} // EffectEditor::Load

void EffectEditor::Save()
{
	_chdir( GetRootDirectory() );
	_chdir( "Models\\Effects\\" );

	FOutStream os( m_EffectFile.c_str() );
	if (os.NoFile()) 
	{
		SaveAs();
	}
	else
	{
		Node* pEmitter = m_pEffect->GetChild( 0 );
		if (!pEmitter) return;
		pEmitter->SerializeSubtree( os );
	}
} // EffectEditor::Save

void EffectEditor::SaveAs()
{
	_chdir( GetRootDirectory() );
	_chdir( "Models\\Effects\\" );

	SaveFileDialog dlg;

	dlg.AddFilter( "Binary Model Files", "*.c2m" );
	dlg.AddFilter( "XML Model Files", "*.eff" );
	dlg.SetDefaultExtension( "c2m" );

	bool dlgRes = dlg.Show();
	RelaxDialog();
	_chdir( GetRootDirectory() );
	if (!dlgRes) return;

	m_EffectFile = dlg.GetFilePath();
	if (strstr( m_EffectFile.c_str(), "eff" ))
	{
	}
	else 
	{
		Node* pEmitter = m_pEffect->GetChild( 0 );
		if (!pEmitter) return;
		FOutStream os( m_EffectFile.c_str() );
		if (os.NoFile()) return;
        //pEmitter->SetName( ParseFileName( m_EffectFile.c_str() ) );
		pEmitter->SerializeSubtree( os );
	}
} // EffectEditor::SaveAs

void EffectEditor::PlayEffect()
{
	if (!m_pEffect) return;
	if (m_EffectID == -1) 
	{
		DWORD id = IMM->GetModelID( m_EffectFile.c_str() );
		Node* pEffect = (PEmitter*)GetChild( 0 );
		if (pEffect && pEffect->IsA<PEmitter>()) m_EffectID = pEffect->GetID();
	}
	IEffMgr->DestroyInstance( m_InstanceID );
    IEffMgr->Pause( false );
    m_bShot = false;
	
	m_InstanceID = IEffMgr->InstanceEffect( m_EffectID );
} // EffectEditor::PlayEffect

void EffectEditor::StopEffect()
{
	IEffMgr->DestroyInstance( m_InstanceID );
    IEffMgr->Pause( false );
}

void EffectEditor::SetEffectFile( const char* fname )
{
	m_EffectFile = fname;
	m_EffectID = 0xFFFFFFFF;
} // EffectEditor::SetEffectFile

float EffectEditor::GetTotalTime() const
{
	PEmitter* pEmitter = (PEmitter*)GetChild( 0 );
	if (!pEmitter) return 0.0f;
	return pEmitter->GetTotalTime(); 
}

float EffectEditor::GetCurTime() const
{
	return IEffMgr->GetCurTime( m_InstanceID ); 
}

END_NAMESPACE(sg)