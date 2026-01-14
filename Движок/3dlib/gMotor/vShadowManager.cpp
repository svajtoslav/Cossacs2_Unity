/*****************************************************************************/
/*	File:	vShadowManager.cpp
/*	Desc:	Shadow manager interface implementation
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#include "stdafx.h"
#include "vShadowManager.h"
#include "ITerrain.h"

ShadowManager g_ShadowMgr;
IShadowManager* IShadowMgr = &g_ShadowMgr;

/*****************************************************************************/
/*  ShadowManager implementation
/*****************************************************************************/
ShadowManager::ShadowManager()
{
    m_bDrawDebugInfo    = false;
    m_ShadowColor       = 0xFFAAAAAA;
    m_ShadowMapID       = -1;
    m_SMapWidth         = 0;
    m_SMapHeight        = 0;
    m_bNeedClearSMap    = true;
    m_LightDir          = Vector3D( -0.5f, -0.1f, -1.0f );
    m_bEnabled          = true;
    m_ClipBias          = 16.0f;
    m_ShadowQuality     = sqUnknown;

    m_LightDir.normalize();

    m_ShadowMapTM.setIdentity();
} // ShadowManager::ShadowManager

void ShadowManager::Init()
{
    if (m_bInited) return;
    SetShadowQuality( m_ShadowQuality );
    m_bInited = true;
} // ShadowManager::Init

void ShadowManager::SetShadowMapSide( int w, int h )
{
    if (h == 0) h = w;
    m_SMapWidth  = w;
    m_SMapHeight = h;
    if (m_ShadowMapID != -1) IRS->DeleteTexture( m_ShadowMapID );

    float texMem        = IRS->GetTexMemorySize();
    int tmMB = texMem/1024.0f/1024.0f;
    Log.Info( "Creating shadow map. TexMem: %dMb", tmMB );

    TextureDescr td;
    td.setValues( m_SMapWidth, m_SMapHeight, cfRGB565, mpVRAM, 1, tuRenderTarget );
    m_ShadowMapID = IRS->CreateTexture( "ShadowMap", td );
    
    //  clear shadowmap
    IRS->SetRenderTarget    ( m_ShadowMapID     );
    IRS->ClearDeviceTarget  ( 0xFFFFFFFF        );
    IRS->SetRenderTarget    ( 0                 );

} // ShadowManager::SetShadowMapSide

void ShadowManager::SetShadowQuality( ShadowQuality quality )
{
    if (m_ShadowQuality != quality)
    {
        switch (quality)
        {
        case sqNoShadows:
            //SetShadowMapSide( 2 );
            //m_bEnabled = false;
            //break;
        case sqBlobs:
            //SetShadowMapSide( 2 );
            //m_bEnabled = false;
            //break;
        case sqLow:
            SetShadowMapSide( 256 );
            m_bEnabled = true;
            break;
        case sqMedium:
            SetShadowMapSide( 512 );
            m_bEnabled = true;
            break;
        case sqHigh:
            SetShadowMapSide( 1024 );
            m_bEnabled = true;
            break;
        }
    }
    m_ShadowQuality = quality;
} // ShadowManager::SetShadowQuality

void ShadowManager::SetLightDir( const Vector3D& dir )
{
    m_LightDir = dir;
} // ShadowManager::SetLightDir

void ShadowManager::Render()
{
    if (!m_bEnabled) return;
    if (!m_bInited) Init();

    ICamera* pCam = GetCamera();
    if (!pCam) return;

    int nCasters = m_DynamicCasters.size();

    IRS->SetRenderTarget    ( m_ShadowMapID     );
    IRS->ClearDeviceTarget  ( 0xFFFFFFFF        );

    if (nCasters == 0) 
    {
        IRS->SetRenderTarget( 0 );
        return;
    }

    //  find bounding box of all casters
    AABoundBox aabb = IMM->GetBoundBox( m_DynamicCasters[0].mdlID );
    aabb.Transform( m_DynamicCasters[0].wTM );
    for (int i = 1; i < nCasters; i++)
    {
        AABoundBox cbox = IMM->GetBoundBox( m_DynamicCasters[i].mdlID );
        cbox.Transform( m_DynamicCasters[i].wTM );
        aabb.Union( cbox );
    }

    //  create light view matrix
    Vector3D lookAt = aabb.GetCenter();
    float radius = aabb.GetDiagonal() * 0.5f;

    //  to save fillrate
    if (radius < c_MinShadowBoxRatio*m_SMapWidth) radius = c_MinShadowBoxRatio*m_SMapWidth;

    Matrix4D viewM = pCam->GetCameraTM();
    Vector3D vZ = m_LightDir;
    Vector3D vX = Vector3D::oX; 
    Vector3D vY = Vector3D::oY; 
    Vector3D::orthonormalize( vZ, vX, vY );
    m_LightViewTM = Matrix4D( vX, vY, vZ, lookAt );
    m_LightViewTM.inverse();

    //  create light projection matrix
    OrthoProjectionTM( m_LightProjTM, radius*2.0f, 1.0f, -radius, radius );
    
    //  render all casters with the same shadow shader, ignore textures
    sg::DeviceStateSet::Freeze();
    //sg::Texture::Freeze();

    Matrix4D viewTM = IRS->GetViewMatrix();
    Matrix4D projTM = IRS->GetProjectionMatrix();

    static int s_ShadowShader = IRS->GetShaderID( "projected_shadow_caster" );

    IRS->SetCurrentShader   ( s_ShadowShader    );
    IRS->SetTextureFactor   ( m_ShadowColor     );

    IRS->SetViewMatrix      ( m_LightViewTM );
    IRS->SetProjectionMatrix( m_LightProjTM );
    IRS->ResetWorldMatrix   ();

    for (int i = 0; i < nCasters; i++)
    {
        ShadowCaster& cst = m_DynamicCasters[i];
        Plane plane;
        Vector3D pos = cst.wTM.getTranslation();
        float h = ITerra->GetH( pos.x, pos.y );
        const Plane c_TopPlane = Plane( Vector3D( 0.0f, 0.0f, 10000.0f ), 
                                        Vector3D( 0.0f, 0.0f, -1.0f ) );
        if (h > pos.z)
        {
            pos.z = h;
            plane.fromPointNormal(  Vector3D( pos.x, pos.y, pos.z - m_ClipBias ), 
                                    ITerra->GetNormal( pos.x, pos.y ) );
            IRS->SetClipPlane( 0, plane );
        }
        else IRS->SetClipPlane( 0, c_TopPlane );

        if (cst.anmID != 0xFFFFFFFF)
        {
            IMM->Animate( cst.mdlID, cst.anmID, cst.anmTime );
        }

        IMM->Render( cst.mdlID, &cst.wTM );
    }

    //  draw white frame to prevent border effects when clamping uv
    rsFrame( Rct( 0.0f, 0.0f, m_SMapWidth, m_SMapWidth ), 0.0f, 0xFFFFFFFF );
    rsFlushLines2D();

    //  restore render target
    IRS->SetRenderTarget( 0 );

    //  restore camera parameters
    IRS->SetViewMatrix( viewTM );
    IRS->SetProjectionMatrix( projTM );

    if (m_bDrawDebugInfo)
    {
        IRS->ResetWorldMatrix();
        //  bounding box
        DrawAABB( aabb, 0, 0xFFFF0000 );

        //  shadow map texture
        static int sh = IRS->GetShaderID( "hud" );
        rsSetShader( sh );
        rsSetTexture( m_ShadowMapID );
        DWORD clr = 0x88FFFFFF;
        rsRect( Rct( 0.0f, 0.0f, m_SMapWidth, m_SMapHeight ), Rct::unit, 0.0f, clr, clr, clr, clr );
        rsFlushLines3D();
        rsFlushPoly2D();
        rsRestoreShader();
        DrawText( 10, m_SMapHeight - 20, 0xFF000000, "NCasters: %d", nCasters );
        FlushText();
    } 

    //  clear casters list
    m_DynamicCasters.clear();

    //sg::Texture::Unfreeze();
    sg::DeviceStateSet::Unfreeze();
    m_bNeedClearSMap = true;
} // ShadowManager::Render

const Matrix4D& ShadowManager::CalcShadowMapTM()
{
    ICamera* pCam = GetCamera();
    if (!pCam) return m_ShadowMapTM;
    Matrix4D camTM = pCam->GetCameraTM();
    const Matrix4D c_ProjToUV = Matrix4D(   0.5f,   0.0f,   0.0f, 0.0f,
                                            0.0f,   -0.5f,  0.0f, 0.0f,
                                            0.0f,   0.0f,   1.0f, 0.0f,
                                            0.5f,   0.5f,   0.0f, 1.0f );
    //  create shadow map texture matrix
    //  uv's for shadow layer are generated from CameraSpacePosition texgen
    m_ShadowMapTM = camTM;          // from camera space to world space
    m_ShadowMapTM *= m_LightViewTM; // from world space to light camera space
    m_ShadowMapTM *= m_LightProjTM; // from light camera space to light projection space
    m_ShadowMapTM *= c_ProjToUV;    // from light projection space to uv space
    return m_ShadowMapTM;
} // ShadowManager::CalcShadowMapTM

void ShadowManager::ClearCache()
{
    m_StaticCasters.clear();
} // ShadowManager::ClearCache

void ShadowManager::UpdateCache()
{

} // ShadowManager::UpdateCache

bool ShadowManager::AddCaster( DWORD modelID, const Matrix4D& tm, bool bStatic )
{
    if (!m_bEnabled) return false;
    if (modelID == 0xFFFFFFFF) return false;
    ShadowCaster caster;
    caster.frame    = IRS->GetCurFrame();
    caster.mdlID    = modelID;
    caster.wTM      = tm;
    caster.anmID    = 0xFFFFFFFF;
    caster.anmTime  = 0.0f;

    if (bStatic) 
    { 
        m_StaticCasters.push_back( caster );
    }
    else
    {
        m_DynamicCasters.push_back( caster );
    }

    return true;
} // ShadowManager::AddCaster

bool ShadowManager::AddCaster( DWORD modelID, DWORD anmID, float anmTime, const Matrix4D& tm, bool bStatic )
{
    if (!m_bEnabled) return false;
    if (modelID == 0xFFFFFFFF) 
    {
        return false;
    }
    ShadowCaster caster;
    caster.frame    = IRS->GetCurFrame();
    caster.mdlID    = modelID;
    caster.wTM      = tm;
    caster.anmID    = anmID;
    caster.anmTime  = anmTime;

    if (bStatic) 
    { 
        m_StaticCasters.push_back( caster );
    }
    else
    {
        m_DynamicCasters.push_back( caster );
    }

    return true;
} // ShadowManager::AddCaster

void ShadowManager::RemoveCaster( int shadowID )
{

} // ShadowManager::RemoveCaster

void ShadowManager::SetShadowColor( DWORD color )
{
    ColorValue fColor( color );
    float ratio = 1.0f - fColor.a;
    fColor.r = ratio + (1.0f - ratio)*fColor.r;
    fColor.g = ratio + (1.0f - ratio)*fColor.g;
    fColor.b = ratio + (1.0f - ratio)*fColor.b;
    m_ShadowColor = fColor;
} // ShadowManager::SetShadowColor
