/*****************************************************************
/*  File:   rsRenderSystemDX.cpp                                 *
/*  Desc:   Direct3D rendering system implementation             *
/*  Author: Silver, Copyright (C) GSC Game World                 *
/*  Date:   Feb 2002                                             *
/*****************************************************************/
#include "stdafx.h"

#include "direct.h"
#include "rsDX.h"
#include "sgFog.h"
#include "sgApplication.h"
#include "kStatistics.h"
#include "kStrUtil.h"
#include "kTimer.h"
#include "rsDeviceStates.h"

int g_RefreshRateBias = 85;

D3DCOLORVALUE DwordToD3DCOLORVALUE( DWORD col )
{
	D3DCOLORVALUE res;
	res.a = float( (col & 0xFF000000)>>24 ) / 255.0f;
	res.r = float( (col & 0x00FF0000)>>16 ) / 255.0f;
	res.g = float( (col & 0x0000FF00)>>8  ) / 255.0f;
	res.b = float( (col & 0x000000FF)     ) / 255.0f;
	return res;
}

//  O== PUBLIC INTERFACE ========================================O
D3DRenderSystem::D3DRenderSystem() 
{
	m_RenderTargetID	= 0;
	m_pBackBuffer		= NULL;
	m_pDepthStencil		= NULL;
	m_pRenderTarget		= NULL;
	m_NAdapters			= 0;
	m_RootDirectory[0]	= 0;
	m_NumActiveLights	= 0;
	m_bVSMode			= false;
	m_CurFrame			= 0;
	m_CurStateBlockID	= 0xFFFFFFFF;
    m_bInited           = false;
}

D3DRenderSystem::~D3DRenderSystem()
{
	ShutDown();
}

const char*	D3DRenderSystem::GetRootDir()
{ 
	return m_RootDirectory; 
}

void  D3DRenderSystem::SetViewMatrix( const Matrix4D& vmatr )
{
    m_ViewMatrix = vmatr;
	DX_CHK( m_pDevice->SetTransform( D3DTS_VIEW, (D3DMATRIX*)&vmatr ) );
}
void  D3DRenderSystem::SetProjectionMatrix( const Matrix4D& pmatr )
{
    m_ProjectionMatrix = pmatr;
	DX_CHK( m_pDevice->SetTransform( D3DTS_PROJECTION, (D3DMATRIX*)&pmatr ) );
}

void D3DRenderSystem::ResetWorldMatrix()
{
    m_WorldMatrix = Matrix4D::identity;
	DX_CHK( m_pDevice->SetTransform( D3DTS_WORLD, (D3DMATRIX*)&Matrix4D::identity ) );
}

void  D3DRenderSystem::SetWorldMatrix( const Matrix4D& wmatr )
{
    m_WorldMatrix = wmatr;
	DX_CHK( m_pDevice->SetTransform( D3DTS_WORLD, (D3DMATRIX*)&wmatr ) );
}

void  D3DRenderSystem::SetTextureMatrix( const Matrix4D& tmatr, int stage )
{
	DX_CHK( m_pDevice->SetTransform( (D3DTRANSFORMSTATETYPE)(D3DTS_TEXTURE0 + stage), 
				(D3DMATRIX*)&tmatr ) );
} // D3DRenderSystem::SetTextureMatrix

bool D3DRenderSystem::SetClipPlane( DWORD idx, const Plane& plane )
{
	HRESULT hres = m_pDevice->SetClipPlane( idx, (float*)&plane );
	if (hres != S_OK)
	{
		Log.Error( "Could not set user clipping plane %d", idx );
		return false;
	}
	return true; 
} // D3DRenderSystem::SetClipPlane


void  D3DRenderSystem::ClearDeviceTarget( DWORD color )
{
	DX_CHK( m_pDevice->Clear( 0, NULL, D3DCLEAR_TARGET, color, 1.0, 0 ) );
}

void  D3DRenderSystem::ClearDeviceZBuffer()
{
	DX_CHK( m_pDevice->Clear( 0, NULL, D3DCLEAR_ZBUFFER, 0, 1.0, 0 ) );
}

void  D3DRenderSystem::ClearDevice( bool bColor, DWORD color, bool bDepth, bool bStencil )
{
	DWORD flags = 0;
	if (bColor) flags |= D3DCLEAR_TARGET;
	if (bDepth) flags |= D3DCLEAR_ZBUFFER;
	if (bStencil) flags |= D3DCLEAR_STENCIL;

	DX_CHK( m_pDevice->Clear( 0, NULL, flags, color, 1.0, 0 ) );
} // D3DRenderSystem::ClearDevice

void  D3DRenderSystem::ClearDeviceStencil()
{
	DX_CHK( m_pDevice->Clear( 0, NULL, D3DCLEAR_STENCIL, 0, 1.0, 0 ) );
}

void  D3DRenderSystem::BeginScene()
{
	DX_CHK( m_pDevice->BeginScene() );
}
void  D3DRenderSystem::EndScene()
{
	DX_CHK( m_pDevice->EndScene() );
}
void  D3DRenderSystem::PresentBackBuffer( const RECT* rect )
{
	assert( m_pDevice );
	HRESULT hres = m_pDevice->TestCooperativeLevel();
	
	if (hres == D3DERR_DEVICELOST)
	{
		return;
	}

	if (hres == D3DERR_DEVICENOTRESET)
    {
		Log.Warning( "Device is lost. Resetting device and recreating/reloading"
						" all device-dependent resources..." );

        for (int i = 0; i < m_NotifyDestroy.size(); i++) m_NotifyDestroy[i]->OnDestroyRenderSystem();
	    InvalidateDeviceObjects();
        ShutDeviceD3D();
        FORCE_RELEASE( m_pD3D );
        InitD3D();
        InitDeviceD3D();
        RestoreDeviceObjects();
        for (int i = 0; i < m_NotifyDestroy.size(); i++) m_NotifyDestroy[i]->OnCreateRenderSystem();
        return;
    }
	
	DX_CHK( m_pDevice->Present( rect, rect, NULL, NULL ) );
} // D3DRenderSystem::PresentBackBuffer

void D3DRenderSystem::OnFrame()
{
	m_ShaderCache.OnFrame();

	m_CurStateBlockID = 0xFFFFFFFF;
	PresentBackBuffer();
	DisableLights();
	m_CurFrame++;
	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGENABLE, FALSE ) );
    Stats::OnFrame();

    static Timer s_Timer;
    if (s_Timer.seconds() > 30.0f)
    {
        POINT pt;
        GetCursorPos( &pt );
        SetCursorPos( pt.x + 1, pt.y + 1 );
        SetCursorPos( pt.x, pt.y );
        s_Timer.start();
    }

	m_CurViewPort = m_FullViewPort;
} // D3DRenderSystem::OnFrame

int  D3DRenderSystem::GetVBufferID()
{
	if (m_VBufID != -1) return m_VBufID;
	//  create vbuffer texture
	TextureDescr vbTD;
	vbTD.setValues( m_ScreenProp.m_Width, 
					m_ScreenProp.m_Width/* m_ScreenProp.height*/, 
					m_ScreenProp.m_ColorFormat, 
					mpVRAM, 1, tuRenderTarget );
	m_VBufID = CreateTexture( "VBuffer", vbTD );
	assert( m_VBufID != -1 );
	return m_VBufID;
} // D3DRenderSystem::GetVBufferID

void  D3DRenderSystem::DrawPrim( BaseMesh& bm )
{
#ifdef _TRACE
	Log.Info( "D3DRenderSystem::DrawPrim <v:%d,i:%d>", bm.getNVert(), bm.getNInd() );
#endif // _TRACE

	DX_CHK( m_pDevice->BeginScene() );
	m_PrimitiveCache.Draw( bm );
	DX_CHK( m_pDevice->EndScene() );
} // D3DRenderSystem::DrawPrim

void  D3DRenderSystem::Draw( BaseMesh& bm )
{
#ifdef _TRACE
	Log.Info( "D3DRenderSystem::Draw <v:%d,i:%d>", bm.getNVert(), bm.getNInd() );
#endif // _TRACE

	DX_CHK( m_pDevice->BeginScene() );
	m_TextureManager.SetTexture( bm.getTexture( 0 ), 0 );
	m_TextureManager.SetTexture( bm.getTexture( 1 ), 1 );
	m_ShaderCache.ApplyShader( bm.getShader() ); 

	m_PrimitiveCache.Draw( bm );
	DX_CHK( m_pDevice->EndScene() );
} // D3DRenderSystem::Draw

void D3DRenderSystem::ShowCursor( bool bShow )
{
	DX_CHK( m_pDevice->ShowCursor( bShow ? TRUE : FALSE ) );
}

bool D3DRenderSystem::SetCursor( int texID, const Rct& rctOnTex, int hotspotX, int hotspotY )
{
	int cTex = m_CursorTD.getID();
	
	//  wrong side of the cached cursor bitmap surface
	if (m_CursorTD.getSideX() != rctOnTex.w || 
		m_CursorTD.getSideY() != rctOnTex.h)
	{
		m_TextureManager.DeleteTexture( cTex );
		m_CursorTD.setID( c_NoID );
	}

	if (cTex == c_NoID)
	{
		//  create cached cursor bitmap surface
		m_CursorTD.setSideX		( rctOnTex.w );
		m_CursorTD.setSideY		( rctOnTex.h );
		m_CursorTD.setMemPool	( mpManaged );
		m_CursorTD.setColFmt	( cfARGB8888 );
		m_CursorTD.setNMips		( 1 );
		m_CursorTD.setTexUsage	( tuProcedural );
		
		cTex = CreateTexture	( "HWCursor_Surface", m_CursorTD );
		m_CursorTD.setID		( cTex );
	}

	//  copy pixels from the cursors texture surface to the cached cursor surface
	POINT	pt; 
	RECT	rct;
	pt.x = pt.y = 0;
	rct.left	= rctOnTex.x;
	rct.right	= rctOnTex.GetRight() - 1;
	rct.top		= rctOnTex.y;
	rct.bottom	= rctOnTex.GetBottom() - 1;
	
	DXSurface* surfSrc   = GetTexSurface( texID );
	DXSurface* surfDest  = GetTexSurface( cTex );

	if (!surfDest || !surfSrc) return false;

	DX_CHK( m_pDevice->CopyRects( surfSrc, &rct, 1, surfDest, &pt ));
	DX_CHK( m_pDevice->SetCursorProperties( hotspotX, hotspotY, surfDest ) );
	DX_CHK( m_pDevice->ShowCursor( TRUE ) );
	return true;
} // D3DRenderSystem::SetCursor

bool D3DRenderSystem::UpdateCursor( int x, int y, bool drawNow )
{
	assert( m_pDevice );
	DWORD flags = drawNow ? D3DCURSOR_IMMEDIATE_UPDATE : 0;
	m_pDevice->SetCursorPosition( x, y, flags );
	//DX_CHK( m_pDevice->ShowCursor( TRUE ) ); // ?
	return true;
} // D3DRenderSystem::UpdateCursor

IDirect3DSurface8* D3DRenderSystem::GetTexSurface( int texID )
{
	return m_TextureManager.GetTextureSurface( texID, 0 );
} // D3DRenderSystem::GetTexSurface

int  D3DRenderSystem::GetTexture( int stage )
{
	return m_TextureManager.GetCurrentTexture( stage );
}  // D3DRenderSystem::GetTexture

void  D3DRenderSystem::SetTexture( int texID, int stage )
{
	if (texID < 0) return;
	m_TextureManager.SetTexture( texID, stage );
} // D3DRenderSystem::SetTexture
const TextureDescr*  D3DRenderSystem::GetTextureDescr( int texID )
{
	return m_TextureManager.GetTextureDescr( texID );
}

int  D3DRenderSystem::GetTextureID( const char* texName, const TextureDescr& td, BYTE* pMemFile, int memFileSize )
{
	if (!texName || texName[0] == 0) 
	{
		return -1;
	}

	int id = -1;
	
	if (td.getColFmt() == cfBackBufferCompatible)
	{
		TextureDescr tdex( td );
		tdex.setColFmt( m_ScreenProp.m_ColorFormat );
		id = m_TextureManager.GetTextureID( texName, tdex, pMemFile, memFileSize );
	}
	else id = m_TextureManager.GetTextureID( texName, td, pMemFile, memFileSize );
	
	if (id >= 0) return id;

	char fname[_MAX_PATH];
	strcpy( fname, GetRootDirectory() );
	strcat( fname, "textures\\" );
	if (!LocateFile( texName, fname ))
	{
		Log.Warning( "Texture file does not exist: %s", texName );
		return -1;
	}
	strcat( fname, texName );

	if (td.getColFmt() == cfBackBufferCompatible)
	{
		TextureDescr tdex( td );
		tdex.setColFmt( m_ScreenProp.m_ColorFormat );
		id = m_TextureManager.GetTextureID( fname, tdex, pMemFile, memFileSize );
	}
	else id = m_TextureManager.GetTextureID( fname, td, pMemFile, memFileSize );
	return id;
} // D3DRenderSystem::LoadTexture

int	  D3DRenderSystem::CreateTexture( const char* texName, const TextureDescr& td )
{
	if (td.getColFmt() == cfBackBufferCompatible)
	{
		TextureDescr tdex( td );
		tdex.setColFmt( m_ScreenProp.m_ColorFormat );
		return m_TextureManager.CreateTexture( texName, tdex );
	}
	return m_TextureManager.CreateTexture( texName, td );
} // D3DRenderSystem::CreateTexture

void D3DRenderSystem::CreateMipLevels( int texID )
{
	m_TextureManager.CreateMipLevels( texID );
} // D3DRenderSystem::CreateMipLevels

bool  D3DRenderSystem::DeleteTexture( int texID )
{
	return m_TextureManager.DeleteTexture( texID );
} // D3DRenderSystem::DeleteTexture

void  D3DRenderSystem::SaveTexture( int texID, const char* fname )
{
	if (texID == 0)
	{
		assert( m_pBackBuffer );
		char ext[64];
		ParseExtension( fname, ext );

		D3DXIMAGE_FILEFORMAT type = D3DXIFF_DDS;
		if (!strcmp( ext, "bmp" )) type = D3DXIFF_BMP;
		if (!strcmp( ext, "tga" )) type = D3DXIFF_TGA;
		if (!strcmp( ext, "dds" )) type = D3DXIFF_DDS;

		HRESULT hres = D3DXSaveSurfaceToFile( fname, type, m_pBackBuffer, NULL, NULL );
		DX_CHK( hres );
		return;
	}
	m_TextureManager.SaveTexture( texID, fname );
} // D3DRenderSystem::SaveTexture

int  D3DRenderSystem::GetTextureSizeBytes( int texID )
{
	return m_TextureManager.GetTextureSizeBytes( texID );
}  // D3DRenderSystem::GetTextureSizeBytes

int   D3DRenderSystem::GetTexMemorySize()
{
	massert( m_pDevice, "Device pointer is NULL." );
	UINT res = m_pDevice->GetAvailableTextureMem();
	return res;
} // D3DRenderSystem::GetTexMemorySize

BYTE*   D3DRenderSystem::LockTexBits( int texID, int& pitch, int level )
{
	if (texID < 0) return NULL;
	return m_TextureManager.LockTexture( texID, pitch, level );
}  // D3DRenderSystem::LockTexBits

BYTE* D3DRenderSystem::LockTexBits( int texID, const Rct& rect, int& pitch, int level )
{
    DXTexture* pTex = m_TextureManager.GetDXTex( texID );
    if (!pTex) return NULL;
    RECT rc;
    rc.left     = rect.x;
    rc.top      = rect.y;
    rc.right    = rect.GetRight();
    rc.bottom   = rect.GetBottom();

    D3DLOCKED_RECT d3dRect;
    DX_CHK( pTex->LockRect( level, &d3dRect, &rc, 0 ) );
    pitch = d3dRect.Pitch;
    return (BYTE*)(d3dRect.pBits);
}  // D3DRenderSystem::LockTexBits

void  D3DRenderSystem::UnlockTexBits( int texID, int level )
{
	m_TextureManager.UnlockTexture( texID, level );
} // D3DRenderSystem::UnlockTexBits

bool  D3DRenderSystem::ReloadAllTextures()
{
	m_TextureManager.ReloadTextures();
    m_TextureManager.LogStatus();
	return true;
}

void  D3DRenderSystem::GetClientSize( int& width, int& height )
{
	assert( false );
}  // D3DRenderSystem::GetClientSize

Rct  D3DRenderSystem::SetViewPort( const Rct& vp )
{
    Rct old( m_CurViewPort.X, m_CurViewPort.Y, m_CurViewPort.Width, m_CurViewPort.Height );
    SetViewPort( vp.x, vp.y, vp.w, vp.h, 0.0f, 1.0f );
    return old;
} // D3DRenderSystem::SetViewPort

Rct  D3DRenderSystem::GetViewPort() const
{
    return Rct( m_CurViewPort.X, m_CurViewPort.Y, m_CurViewPort.Width, m_CurViewPort.Height ); 
}

void D3DRenderSystem::SetViewPort( float x, float y, float w, float h, float zn, float zf )
{
	Rct cvp( x, y, w, h );
    Rct svp( 0, 0, m_ScreenProp.m_Width, m_ScreenProp.m_Height );
    svp.Clip( cvp );

	D3DVIEWPORT8 dvp;
	dvp.X		= cvp.x;
	dvp.Y		= cvp.y;
	dvp.Width	= cvp.w;
    dvp.Height	= cvp.h;
    dvp.MinZ	= 0.0f;
    dvp.MaxZ	= 1.0f;
    m_pDevice->SetViewport( (D3DVIEWPORT8*)&dvp );	

    Rct old( m_CurViewPort.X, m_CurViewPort.Y, m_CurViewPort.Width, m_CurViewPort.Height );
    m_CurViewPort = dvp;
} // D3DRenderSystem::SetViewPort

bool  D3DRenderSystem::SetRenderTarget( int texID, int dsID )
{
	if (m_RenderTargetID == texID) return true;
	m_RenderTargetID = texID;
	
	if (m_RenderTargetID == 0)
	{
		HRESULT hRes = m_pDevice->SetRenderTarget( m_pBackBuffer, m_pDepthStencil );
        if (hRes != S_OK)
        {
            DX_CHK( S_OK );
            return false;
        }
	}
	else
	{
		m_pRenderTarget			 = m_TextureManager.GetTextureSurface( m_RenderTargetID );
		DXSurface* pDepthStencil = m_TextureManager.GetTextureSurface( dsID );
		HRESULT hRes = m_pDevice->SetRenderTarget( m_pRenderTarget, pDepthStencil );
        if (hRes != S_OK)
        {
            DX_CHK( S_OK );
            return false;
        }
        SAFE_DECREF( m_pRenderTarget );
	}
    return true;
} // D3DRenderSystem::SetRenderTarget

void D3DRenderSystem::CopyTexture( int destID, int srcID, const Rct* rct, int nRect ) 
{ 
    IDirect3DTexture8* pSrc = m_TextureManager.GetDXTex( srcID );
    IDirect3DTexture8* pDst = m_TextureManager.GetDXTex( destID );
    if (!pSrc || !pDst) return;
    
    Rct rect;
    if (!rct)
    {
        rect.x = 0;
        rect.y = 0;
        rect.w = m_TextureManager.GetTextureDescr( srcID )->getSideX();
        rect.h = m_TextureManager.GetTextureDescr( srcID )->getSideY();
        nRect  = 1;
        rct    = &rect;
    }

    for (int i = 0; i < nRect; i++)
    {
        RECT wrct;
        wrct.left    = rct[i].x;
        wrct.top     = rct[i].y;
        wrct.right   = rct[i].GetRight();
        wrct.bottom  = rct[i].GetBottom();
        pDst->AddDirtyRect( &wrct );
    }

    DX_CHK( m_pDevice->UpdateTexture( pSrc, pDst ) );
} //D3DRenderSystem::CopyTexture

void D3DRenderSystem::AdjustWindowPos( int x, int y, int w, int h )
{
	SetWindowLong( m_hRenderWindow, GWL_STYLE, WS_POPUP ); 
	SetWindowPos( m_hRenderWindow, HWND_NOTOPMOST, x, y, w, h, 0 );
} // D3DRenderSystem::AdjustWindowPos

void*  D3DRenderSystem::DbgGetDevice()
{
	return (void*)m_pDevice;
}

bool D3DRenderSystem::InitD3D()
{
	m_pD3D = Direct3DCreate8( D3D_SDK_VERSION );
	massert( m_pD3D != NULL, "Failed to create DirectX device" );
	BuildDeviceList();

	//  pick default device
	m_CurDeviceInfo = m_AdapterList[0].FindDevice( D3DDEVTYPE_HAL );
	massert( m_CurDeviceInfo, "No render device available." );
	return true;
} // D3DRenderSystem::InitD3D

bool D3DRenderSystem::ShutDeviceD3D()
{
	SetTexture( 0, 0 );
	SetTexture( 0, 1 );
	//SetCurrentShader( 0 );

	m_TextureManager.Shut();
	m_PrimitiveCache.Shut();

	SAFE_RELEASE( m_pBackBuffer );
	SAFE_RELEASE( m_pDepthStencil );
	FORCE_RELEASE( m_pDevice );	

    m_pDevice = NULL;
	return true;
} // D3DRenderSystem::ShutDeviceD3D

bool D3DRenderSystem::SetupDisplaySettings()
{
    DEVMODE devMode;
    ZeroMemory( &devMode, sizeof( devMode ) );
    devMode.dmSize              = sizeof( DEVMODE );
    devMode.dmBitsPerPel	    = 16;
    devMode.dmPelsWidth         = m_ScreenProp.m_Width;
    devMode.dmPelsHeight        = m_ScreenProp.m_Height;
    devMode.dmDisplayFrequency  = m_ScreenProp.m_RefreshRate;
    devMode.dmFields            = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL;

    DWORD displayModeFlags = 0;
    if (m_ScreenProp.m_bFullScreen) displayModeFlags |= CDS_FULLSCREEN;
    int devModeRes = ChangeDisplaySettings( &devMode, displayModeFlags );
    DWORD winErr = GetLastError();
    if (devModeRes != DISP_CHANGE_SUCCESSFUL)
    {
        Log.Error( GetDispChangeErrorDesc( devModeRes ) );
        return false;
    }

    if (m_ScreenProp.m_bCoverDesktop)
    {
        RECT drct;
        HWND hDesk = GetDesktopWindow();
        ::GetWindowRect( hDesk, &drct );
        SystemParametersInfo( SPI_GETWORKAREA, 0, &drct, 0 );
        m_ScreenProp.m_Width  = drct.right  - drct.left;
        m_ScreenProp.m_Height = drct.bottom - drct.top;
    }
    
    int wX = 0;
    int wY = 0;
    int wW = m_ScreenProp.m_Width;
    int wH = m_ScreenProp.m_Height;

    SetWindowPos( m_hRenderWindow, HWND_NOTOPMOST, wX, wY, wW, wH, SWP_SHOWWINDOW );
    ShowWindow( m_hRenderWindow, SW_SHOW );

    RECT	wrct;
    ::GetWindowRect( m_hRenderWindow, &wrct );
    m_FullViewPort.X		= 0.0f;
    m_FullViewPort.Y		= 0.0f;
    m_FullViewPort.Width 	= wrct.right - wrct.left;
    m_FullViewPort.Height	= wrct.bottom - wrct.top;
    m_FullViewPort.MinZ  	= 0.0f;
    m_FullViewPort.MaxZ  	= 1.0f;

    m_CurViewPort = m_FullViewPort;
    return true;
} // D3DRenderSystem::SetupDisplaySettings

bool D3DRenderSystem::InitDeviceD3D()
{
    D3DPRESENT_PARAMETERS d3dpp;
    ZeroMemory( &d3dpp, sizeof(d3dpp) );
	
	BOOL bWindowed                  = (m_ScreenProp.m_bFullScreen == false);
    d3dpp.Windowed                  = bWindowed;
	d3dpp.SwapEffect                = D3DSWAPEFFECT_DISCARD;
	m_ScreenProp.m_ColorFormat      = cfRGB565;
	d3dpp.EnableAutoDepthStencil    = TRUE;
	d3dpp.AutoDepthStencilFormat    = D3DFMT_D16;
    d3dpp.BackBufferFormat		    = ColorFormatG2DX( m_ScreenProp.m_ColorFormat );
    d3dpp.BackBufferCount           = 1;
    d3dpp.hDeviceWindow             = m_hRenderWindow;

	if (!m_ScreenProp.m_bCoverDesktop && !m_ScreenProp.m_bFullScreen) 
	{
		RECT rctClient;
		GetClientRect( m_hRenderWindow, &rctClient );
		m_ScreenProp.m_Width  = rctClient.right - rctClient.left;
		m_ScreenProp.m_Height = rctClient.bottom - rctClient.top;
	}
	
	d3dpp.BackBufferWidth   = m_FullViewPort.Width;
	d3dpp.BackBufferHeight  = m_FullViewPort.Height;

	DWORD flags = D3DCREATE_MIXED_VERTEXPROCESSING;
	//if (m_Settings.hardwareTnL) flags |= D3DCREATE_HARDWARE_VERTEXPROCESSING;
	//	else flags |= D3DCREATE_SOFTWARE_VERTEXPROCESSING;
	

	if (m_Settings.pureDevice) flags |= D3DCREATE_PUREDEVICE;
	
	D3DDEVTYPE devType = m_Settings.softwareRendering ? D3DDEVTYPE_REF : D3DDEVTYPE_HAL;

    // Create the D3DDevice
    HRESULT res = m_pD3D->CreateDevice(	m_Settings.adapterOrdinal, 
										devType, 
										m_hRenderWindow,
										flags,
										&d3dpp, 
										&m_pDevice );

	if (res != S_OK)
	{
		d3dpp.AutoDepthStencilFormat = D3DFMT_D16;
		res = m_pD3D->CreateDevice(	m_Settings.adapterOrdinal, 
									devType, 
									m_hRenderWindow,
									flags,
									&d3dpp, 
									&m_pDevice );
	
	}
	DX_CHK( res );

	if (!m_pDevice) return false;

	DX_CHK( m_pDevice->ShowCursor( FALSE ) );
	return true;
} // D3DRenderSystem::InitDeviceD3D

void  D3DRenderSystem::Init( HWND hWnd, bool subclassWnd )
{
	IRS = this;
    
    if (m_bInited)
    {
        Log.Error( "Trying to initialize render device twice." );
        return;
    }
	
    if (hWnd == NULL)
    {
        Log.Error( "D3D Render System - No window handle." );
        return;
    }

	getcwd( m_RootDirectory, _MAX_PATH );
	m_hRenderWindow = hWnd;

    DWORD len = 0;
    char* buf = (char*)LoadFile( "VideoConfig.txt", len );
    if (buf)
    {
        char* pBuf = strstr( buf, "=" );
        if (pBuf)
        {
            pBuf++;
            sscanf( pBuf, "%d", &g_RefreshRateBias );
        }
    }

	Log.Info( "Initializing D3D Render System..." );
	InitD3D();
	
    D3DDISPLAYMODE d3ddm;
    assert( m_pD3D );
	DX_CHK( m_pD3D->GetAdapterDisplayMode( D3DADAPTER_DEFAULT, &d3ddm ) );

	assert( m_CurDeviceInfo );
	D3DModeInfo* modeInfo = m_CurDeviceInfo->FindMode( d3ddm.Width, d3ddm.Height );
	if (!modeInfo)
    {
        Log.Warning( "Current display mode is not supported." );
    }
	m_ScreenProp.m_Width		= d3ddm.Width;
    m_ScreenProp.m_Height       = d3ddm.Height;
	m_ScreenProp.m_RefreshRate	= modeInfo ? modeInfo->refreshRate : 0;
    m_ScreenProp.m_ColorFormat	= ColorFormatDX2G( d3ddm.Format );

	//  get current desktop display mode in order to restore it at shutdown
	EnumDisplaySettings( NULL, ENUM_CURRENT_SETTINGS, &m_dmDesktop );

	RECT rct;
	GetClientRect( hWnd, &rct );
	int clWidth = rct.right - rct.left;
	int clHeight = rct.bottom - rct.top;
    if (!SetupDisplaySettings()) return;
	if (!InitDeviceD3D()) return;	
	InitDeviceObjects();

	D3DCAPS8 caps;
	m_pD3D->GetDeviceCaps( D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, &caps );
	if (caps.MaxTextureWidth < 1024 || 
		caps.MaxTextureHeight < 1024 ||
		((caps.DevCaps & D3DDEVCAPS_TEXTUREVIDEOMEMORY) == 0)
		)
	{
		MessageBox(hWnd,"Your graphics card does not support required caps.","Critical error!",MB_ICONERROR);
	}

	Log.Info( "D3D Render System was initialized succesfully." );

	SetWorldMatrix( Matrix4D::identity );
    m_bInited = true;
} // D3DRenderSystem::Init

HRESULT D3DRenderSystem::InitDeviceObjects()
{
	DX_CHK( m_pDevice->GetBackBuffer( 0, D3DBACKBUFFER_TYPE_MONO, &m_pBackBuffer ) );
	DX_CHK( m_pDevice->GetDepthStencilSurface( &m_pDepthStencil ) );

	m_PrimitiveCache.Init();
	m_ShaderCache.Init();
	m_TextureManager.Init();

	m_VBufID = -1;
	return S_OK;
} // D3DRenderSystem::InitDeviceObjects

HRESULT D3DRenderSystem::RestoreDeviceObjects()
{
	m_PrimitiveCache.RestoreDeviceObjects();
	m_TextureManager.RestoreDeviceObjects();

    DX_CHK( m_pDevice->GetBackBuffer( 0, D3DBACKBUFFER_TYPE_MONO, &m_pBackBuffer ) );
    DX_CHK( m_pDevice->GetDepthStencilSurface	( &m_pDepthStencil ) );

	if (m_VBufID != -1)
	{
		m_VBufID = -1;
		m_VBufID = GetVBufferID();
	}

	return S_OK;
} // D3DRenderSystem::RestoreDeviceObjects

void  D3DRenderSystem::SetRootDir( const char* rootDir )
{
	strcpy( m_RootDirectory, rootDir );
} // D3DRenderSystem::SetRootDir

void  D3DRenderSystem::ShutDown()
{
    if (!m_bInited) return;
	ShutDeviceD3D();
	RestoreDesktopDisplayMode();
	FORCE_RELEASE( m_pD3D );
    m_bInited = false;
}//  D3DRenderSystem::ShutDown

void D3DRenderSystem::RestoreDesktopDisplayMode()
{
	int devModeRes = ChangeDisplaySettings( &m_dmDesktop, 0 );
	if (devModeRes != DISP_CHANGE_SUCCESSFUL)
	{
		Log.Error( "Could not restore desktop display mode: %s", 
							GetDispChangeErrorDesc( devModeRes ) );
	}
} // D3DRenderSystem::RestoreDesktopDisplayMode


void  D3DRenderSystem::Dump( const char* fname )
{
	FILE* fp = fname ? fopen( fname, "wt" ) : fopen( "c:\\dumps\\DeviceDump.txt", "wt" );
	if (!fp) return;

	fprintf( fp, "--------- CURRENT DEVICE STATE VALUES --------" );
	fprintf( fp, "--------- --------" );

	fprintf( fp, "\nTransforms:\n" );
	D3DMATRIX matr;
	m_pDevice->GetTransform( D3DTS_VIEW, &matr );
	fprintf( fp, "\n\nView, Det=%f:\n", D3DXMatrixfDeterminant( (D3DXMATRIX*)&matr ) );
	//fpmatr( fp, matr );
	m_pDevice->GetTransform( D3DTS_PROJECTION, &matr );
	fprintf( fp, "\n\nProjection, Det=%f:", D3DXMatrixfDeterminant( (D3DXMATRIX*)&matr ) );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nWorld: Det=%f\n", D3DXMatrixfDeterminant( (D3DXMATRIX*)&matr ) );
	m_pDevice->GetTransform( D3DTS_WORLD, &matr );
	//fpmatr( fp, matr );

	// texture matrices
	fprintf( fp, "\n\nTexture0:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE0, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture1:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE1, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture2:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE2, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture3:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE3, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture4:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE4, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture5:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE5, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture6:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE6, &matr );
	//fpmatr( fp, matr );
	fprintf( fp, "\n\nTexture7:\n" );
	m_pDevice->GetTransform( D3DTS_TEXTURE7, &matr );
	//fpmatr( fp, matr );

	D3DVIEWPORT8 vp;
	m_pDevice->GetViewport( &vp );
	fprintf( fp, "\nViewport: x=%d; y=%d; w=%d; h =%d; minz=%f; maxz=%f\n",
				vp.X, vp.Y, vp.Width, vp.Height, vp.MinZ, vp.MaxZ );
	IDirect3DSurface8* rt;
	m_pDevice->GetRenderTarget( &rt );
	fprintf( fp, "\nRender target: %X\n", rt );

	fclose( fp );
}

//  shaders
void  D3DRenderSystem::SetCurrentShader( int shaderID )
{
	bool res = m_ShaderCache.ApplyShader( shaderID ); 
	//Dump();
} // D3DRenderSystem::SetCurrentShader

bool D3DRenderSystem::ApplyStateBlock( DWORD id )
{
	if (!m_pDevice) return false;
	if (m_CurStateBlockID == id) return true;
	m_CurStateBlockID = id;
	DX_CHK( m_pDevice->ApplyStateBlock( id ) );
	return true;
} // D3DRenderSystem::ApplyStateBlock

bool D3DRenderSystem::DeleteStateBlock( DWORD id )
{
	if (!m_pDevice || id == DSBlock::c_BadDevHandle) return false;
	DX_CHK( m_pDevice->DeleteStateBlock( id ) );
	return true;
} // D3DRenderSystem::DeleteStateBlock

DWORD D3DRenderSystem::CreateStateBlock( DSBlock* pBlock )
{
	if (!m_pDevice || m_pDevice->BeginStateBlock() != S_OK) return 0xFFFFFFFF;
	DWORD id;
	ApplyStateBlock( pBlock );
	if (m_pDevice->EndStateBlock( &id ) != S_OK) return 0xFFFFFFFF;
	return id; 
}

bool D3DRenderSystem::GetCurrentStateBlock( DSBlock& dsb ) const
{
	for (int i = 0; i < dsb.GetNRS(); i++)
	{
		DeviceState* pState = dsb.GetRS( i );
		DX_CHK( m_pDevice->GetRenderState(	(D3DRENDERSTATETYPE)pState->devID, 
											&pState->value ) );
	}

	for (int i = 0; i < c_MaxTextureStages; i++)
	{
		for (int j = 0; j < dsb.GetNTSS( i ); j++)
		{
			DeviceState* pState =  dsb.GetTSS( i, j );
			DX_CHK( m_pDevice->GetTextureStageState(	i, 
													(D3DTEXTURESTAGESTATETYPE)pState->devID, 
													&pState->value ) );
		}
	}
	return true;
} // D3DRenderSystem::GetCurrentStateBlock

bool D3DRenderSystem::ApplyStateBlock( DSBlock* pBlock )
{
	if (!m_pDevice) return false;
	for (int i = 0; i < pBlock->GetNRS(); i++)
	{
		const DeviceState* pState = pBlock->GetRS( i );
		DX_CHK( m_pDevice->SetRenderState( (D3DRENDERSTATETYPE)pState->devID, pState->value ) );
	}

	for (int i = 0; i < c_MaxTextureStages; i++)
	{
		for (int j = 0; j < pBlock->GetNTSS( i ); j++)
		{
			const DeviceState* pState = pBlock->GetTSS( i, j );
			DX_CHK( m_pDevice->SetTextureStageState( i,
								(D3DTEXTURESTAGESTATETYPE)pState->devID, 
								pState->value ) );
		}
	}
	return true;
} // D3DRenderSystem::ApplyStateBlock

bool D3DRenderSystem::SetVSConstant( int cIdx, const Vector4D& cVec )
{
	DX_CHK( m_pDevice->SetVertexShaderConstant( cIdx, (void*)&cVec, 1 ) );
	return true;
} // D3DRenderSystem::SetVSConstant

bool D3DRenderSystem::SetVSConstant( int cIdx, const Matrix4D& cMatr, bool bFull )
{
	DX_CHK( m_pDevice->SetVertexShaderConstant( cIdx, (void*)&cMatr, bFull ? 4 : 3 ) );
	return true;
} // D3DRenderSystem::SetVSConstant

const char*	 D3DRenderSystem::GetTextureName( int texID )
{
	return m_TextureManager.GetTextureName( texID );
} // D3DRenderSystem::GetTextureName

int   D3DRenderSystem::GetTextureID( const char* texName, BYTE* pMemFile, int memFileSize )
{
	if (!texName || texName[0] == 0) 
	{
		return -1;
	}
	
	int id = m_TextureManager.GetTextureID( texName, pMemFile, memFileSize );
	if (id >= 0) return id;

	char fname[_MAX_PATH];
	strcpy( fname, GetRootDirectory() );
	strcat( fname, "textures\\" );
	if (!LocateFile( texName, fname ))
	{
		Log.Warning( "Texture file does not exist: %s", texName );
		return -1;
	}
	strcat( fname, texName );

	id = m_TextureManager.GetTextureID( fname, pMemFile, memFileSize );
	return id;
} // D3DRenderSystem::GetTextureID

int D3DRenderSystem::LoadTexture( const char* texName, const TextureDescr& td, BYTE* pMemFile, int memFileSize )
{
	if (td.getColFmt() == cfBackBufferCompatible)
	{
		TextureDescr tdex( td );
		tdex.setColFmt( m_ScreenProp.m_ColorFormat );
		return m_TextureManager.GetTextureID( texName, tdex, pMemFile, memFileSize );
	}
	return m_TextureManager.GetTextureID( texName, td, pMemFile, memFileSize );
}

int D3DRenderSystem::LoadTexture( const char* texName, BYTE* pMemFile, int memFileSize )
{
	if (!texName) return 0;
	return m_TextureManager.GetTextureID( texName, pMemFile, memFileSize );
}

bool  D3DRenderSystem::IsShaderValid( const char* shaderName )
{
	//  create file name
	char fname[_MAX_PATH];
	sprintf( fname, "%s\\shaders\\%s.sha", m_RootDirectory, shaderName );

	ID3DXEffect* eff;
	ID3DXBuffer* compileErr;
	HRESULT hres = D3DXCreateEffectFromFile( m_pDevice, fname, &eff, &compileErr );
	if (hres != S_OK) 
	{
		if (!compileErr)
		{
			Log.Warning( "Could not load shader file %s", fname );
			return false;
		}
		else
		{
			char* errbuf = (char*)compileErr->GetBufferPointer();
			Log.Warning( "Could not compile shader <%s>. Error message: %s", 
							shaderName, errbuf );
			SAFE_RELEASE( compileErr );
			return false;
		}
	}
	hres = eff->SetTechnique( "T0" );
	bool valid = true;
	if (hres != S_OK) valid = false;
	//  Checking if device is capable to set this shader
	if (eff->Validate() != S_OK) valid = false;
	SAFE_RELEASE( eff );
	return valid;
} // D3DRenderSystem::IsShaderValid

const char* D3DRenderSystem::GetShaderName( int shID ) const
{
	return m_ShaderCache.GetShaderName( shID );
} // D3DRenderSystem::GetShaderName

int	D3DRenderSystem::GetShaderID( const char* shaderName, BYTE* shBuf, int size )
{
	return m_ShaderCache.GetShaderID( shaderName, shBuf, size );
} // D3DRenderSystem::GetShaderID

bool  D3DRenderSystem::RecompileAllShaders()
{	
	m_ShaderCache.ReloadShaders();
    //m_TextureManager.LogStatus();
	return true;
} // D3DRenderSystem::RecompileAllShaders

void  D3DRenderSystem::SetTextureFactor( DWORD tfactor )
{
	DX_CHK( m_pDevice->SetRenderState( D3DRS_TEXTUREFACTOR, tfactor ) );	
}

void D3DRenderSystem::SetZEnable( bool bEnable )
{
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ZENABLE, bEnable ? TRUE : FALSE ) );	
}

void D3DRenderSystem::SetZWriteEnable( bool bEnable )
{
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ZWRITEENABLE, bEnable ? TRUE : FALSE ) );	
}

void D3DRenderSystem::SetDitherEnable( bool bEnable )
{
    DX_CHK( m_pDevice->SetRenderState( D3DRS_DITHERENABLE, bEnable ? TRUE : FALSE ) );
}

void D3DRenderSystem::SetWireframe( bool bEnable )
{
	DX_CHK( m_pDevice->SetRenderState( D3DRS_FILLMODE, bEnable ? D3DFILL_WIREFRAME : D3DFILL_SOLID ) );	
}

void  D3DRenderSystem::SetAlphaRef( BYTE alphaRef )
{
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ALPHAREF, alphaRef ) );	
}

void D3DRenderSystem::GetPresentParameters( D3DPRESENT_PARAMETERS& presParm )
{
	if (m_ScreenProp.m_bFullScreen)
	{
		presParm.BackBufferWidth	= m_ScreenProp.m_Width;
		presParm.BackBufferHeight	= m_ScreenProp.m_Height;
	}
	else
	{
		RECT rect;
		assert( m_hRenderWindow );
		GetClientRect( m_hRenderWindow, &rect );
		presParm.BackBufferWidth	= rect.right - rect.left;
		presParm.BackBufferHeight	= rect.bottom - rect.top;
	}

	presParm.BackBufferFormat		= ColorFormatG2DX( m_ScreenProp.m_ColorFormat );
	presParm.BackBufferCount		= 1;
									
	presParm.MultiSampleType		= D3DMULTISAMPLE_NONE;
									
	presParm.SwapEffect				= D3DSWAPEFFECT_COPY;
	presParm.hDeviceWindow			= m_hRenderWindow;
	presParm.Windowed				= m_ScreenProp.m_bFullScreen ? FALSE : TRUE;
	presParm.EnableAutoDepthStencil = TRUE;
	presParm.AutoDepthStencilFormat = D3DFMT_D16;
	presParm.Flags					= 0;

    presParm.FullScreen_RefreshRateInHz			= m_ScreenProp.m_bFullScreen ? D3DPRESENT_RATE_DEFAULT : 0;
    presParm.FullScreen_PresentationInterval	= D3DPRESENT_INTERVAL_DEFAULT;
}

bool D3DRenderSystem::ResetDevice()
{
	InvalidateDeviceObjects();

	D3DPRESENT_PARAMETERS presParm;
	GetPresentParameters( presParm );
	
	//  set display fullscreen mode
	DEVMODE devMode;
	ZeroMemory( &devMode, sizeof( devMode ) );
	devMode.dmSize          = sizeof( DEVMODE );

	devMode.dmPelsWidth     = m_ScreenProp.m_Width;
	devMode.dmPelsHeight    = m_ScreenProp.m_Height;
	devMode.dmFields        = DM_PELSWIDTH | DM_PELSHEIGHT;
	
	DWORD flags = m_ScreenProp.m_bFullScreen ? CDS_FULLSCREEN : 0;
	int devModeRes = ChangeDisplaySettings( &devMode, flags );
	if (devModeRes != DISP_CHANGE_SUCCESSFUL)
	{
		assert( false );
		return false;
	}

	//  resetting device
	HRESULT hres = m_pDevice->Reset( &presParm );
	if (hres != S_OK) 
	{
		return false;
	}

	//  reinitialize all previously released resources
	RestoreDeviceObjects();

	D3DSURFACE_DESC bbDesc;
	LPDIRECT3DSURFACE8 pBackBuffer;
    m_pDevice->GetBackBuffer( 0, D3DBACKBUFFER_TYPE_MONO, &pBackBuffer );
    pBackBuffer->GetDesc( &bbDesc );
    pBackBuffer->Release();


	DWORD numPasses;
	DX_CHK( m_pDevice->ValidateDevice( &numPasses ) );	
	return true;
} // D3DRenderSystem::ResetDevice

ScreenProp  D3DRenderSystem::GetScreenProperties()
{
	return m_ScreenProp;
} // D3DRenderSystem::GetScreenProperties

bool  D3DRenderSystem::SetScreenProperties( const ScreenProp& prop )
{
    if (!m_pDevice)
    {
        Log.Error( "Trying to change screen properties with unitialized render device." );
        return false;
    }

	//  check if nothing changes
	ScreenProp rProp = prop;
	if (rProp.m_ColorFormat == cfBackBufferCompatible) rProp.m_ColorFormat = m_ScreenProp.m_ColorFormat;
	///if (rProp.equal( m_ScreenProp )) return true;
	m_ScreenProp = rProp;

	//  select maximal possible refresh rate
	assert( m_CurDeviceInfo );
	D3DModeInfo* modeInfo = m_CurDeviceInfo->FindMode( m_ScreenProp.m_Width, m_ScreenProp.m_Height );
	if (!modeInfo)
	{
		Log.Error( "Display mode %dx%d is not supported.", m_ScreenProp.m_Width, m_ScreenProp.m_Height );
		return false;
	}

	m_ScreenProp.m_RefreshRate = modeInfo->refreshRate;

	if (prop.m_ColorFormat != cfBackBufferCompatible)
	{
		m_ScreenProp.m_ColorFormat = prop.m_ColorFormat;
	}
	
	for (int i = 0; i < m_NotifyDestroy.size(); i++)
	{
		m_NotifyDestroy[i]->OnDestroyRenderSystem();
	}

	InvalidateDeviceObjects();
	ShutDeviceD3D();
	SAFE_RELEASE( m_pD3D );
    SetupDisplaySettings();
	InitD3D();
	InitDeviceD3D();
	RestoreDeviceObjects();

	for (int i = 0; i < m_NotifyDestroy.size(); i++)
	{
		m_NotifyDestroy[i]->OnCreateRenderSystem();
	}
	return true;
} // D3DRenderSystem::SetScreenProperties		

void  D3DRenderSystem::SetMaterial( sg::Material* pMaterial )
{
	D3DMATERIAL8 mtl;

	mtl.Ambient		= DwordToD3DCOLORVALUE( pMaterial->GetAmbient()		);
	mtl.Diffuse		= DwordToD3DCOLORVALUE( pMaterial->GetDiffuse()		);
	mtl.Specular	= DwordToD3DCOLORVALUE( pMaterial->GetSpecular()	);
	mtl.Emissive	= DwordToD3DCOLORVALUE( 0							);

	mtl.Power		= pMaterial->GetShininess();

	DX_CHK( m_pDevice->SetMaterial( &mtl ) );
} // D3DRenderSystem::SetMaterial

void D3DRenderSystem::DisableLights()
{
	for (int i = 0; i < m_NumActiveLights; i++)
	{
		DX_CHK( m_pDevice->LightEnable( i, FALSE ) );
	}

	m_NumActiveLights = 0;
} // D3DRenderSystem::DisableLights

void D3DRenderSystem::SetDirectionalLight( sg::DirectionalLight* pLight, int& index )
{
	if (!pLight) return;
	
	if (index == -1)
	{
		index = m_NumActiveLights;
		if (m_NumActiveLights == c_MaxLights) return;
	}

	m_NumActiveLights++;


	Vector3D dir = pLight->GetDir();

	D3DLIGHT8 light;
	light.Type		= D3DLIGHT_DIRECTIONAL ;            
	light.Diffuse	= DwordToD3DCOLORVALUE( pLight->GetDiffuse()  );
	light.Specular	= DwordToD3DCOLORVALUE( pLight->GetSpecular() );        
	light.Ambient	= DwordToD3DCOLORVALUE( pLight->GetAmbient()  );   

	light.Direction.x	= dir.x; 
	light.Direction.y	= dir.y;
	light.Direction.z	= dir.z;
	
	//  Now, folks, docs say that "for directional light position/range is ignored"
	//  That is not true, at least on some drivers. So do them happy:
	light.Range			= 100000.0f;
	light.Position.x	= 0.0f; 
	light.Position.y	= 0.0f;
	light.Position.z	= 0.0f;

	light.Attenuation0	= 1.0f;
	light.Attenuation1	= 0.0f;
	light.Attenuation2	= 0.0f;

	light.Falloff		= 1.0f;
	light.Theta			= c_PI;
	light.Phi			= c_PI;
	
	DX_CHK( m_pDevice->SetLight( index, &light ) );
	DX_CHK( m_pDevice->LightEnable( index, TRUE ) );
} // D3DRenderSystem::SetDirectionalLight

void D3DRenderSystem::SetPointLight( sg::PointLight* pLight, int& index )
{
	if (!pLight) return;

	if (index == -1)
	{
		index = m_NumActiveLights;
		if (m_NumActiveLights == c_MaxLights) return;
	}

	m_NumActiveLights++;

	Vector3D pos = pLight->GetPos();
	Vector3D dir = pLight->GetDir();

	D3DLIGHT8 light;
	light.Type			= D3DLIGHT_POINT;            
	light.Diffuse		= DwordToD3DCOLORVALUE( pLight->GetDiffuse()  );
	light.Specular		= DwordToD3DCOLORVALUE( pLight->GetSpecular() );        
	light.Ambient		= DwordToD3DCOLORVALUE( pLight->GetAmbient()  );   

	light.Position.x	= pos.x; 
	light.Position.y	= pos.y;
	light.Position.z	= pos.z;

	light.Direction.x	= dir.x; 
	light.Direction.y	= dir.y;
	light.Direction.z	= dir.z;

	light.Range			= pLight->GetRange();   

	light.Attenuation0	= pLight->GetAttenuationA();
	light.Attenuation1	= pLight->GetAttenuationB();
	light.Attenuation2	= pLight->GetAttenuationC();

	light.Falloff		= 1.0f;
	light.Theta			= c_PI;
	light.Phi			= c_PI;

	DX_CHK( m_pDevice->SetLight( index, &light ) );
	DX_CHK( m_pDevice->LightEnable( index, TRUE ) );
} // D3DRenderSystem::SetPointLight

void D3DRenderSystem::SetSpotLight( sg::SpotLight* pLight, int& index ) 
{
	if (!pLight) return;
	
	if (index == -1)
	{
		index = m_NumActiveLights;
		if (m_NumActiveLights == c_MaxLights) return;
	}

	m_NumActiveLights++;

	Vector3D pos = pLight->GetPos();
	Vector3D dir = pLight->GetDir();

	D3DLIGHT8 light;
	light.Type		= D3DLIGHT_DIRECTIONAL ;            
	light.Diffuse	= DwordToD3DCOLORVALUE( pLight->GetDiffuse()  );
	light.Specular	= DwordToD3DCOLORVALUE( pLight->GetSpecular() );        
	light.Ambient	= DwordToD3DCOLORVALUE( pLight->GetAmbient()  );   

	light.Position.x	= pos.x; 
	light.Position.y	= pos.y;
	light.Position.z	= pos.z;

	light.Direction.x	= dir.x; 
	light.Direction.y	= dir.y;
	light.Direction.z	= dir.z;

	light.Range			= pLight->GetRange();   

	light.Attenuation0	= pLight->GetAttenuationA();
	light.Attenuation1	= pLight->GetAttenuationB();
	light.Attenuation2	= pLight->GetAttenuationC();

	light.Falloff		= pLight->GetConeFalloff();        
	light.Theta			= pLight->GetInnerCone();          
	light.Phi			= pLight->GetOuterCone();

	DX_CHK( m_pDevice->SetLight( index, &light ) );
	DX_CHK( m_pDevice->LightEnable( index, TRUE ) );
} // D3DRenderSystem::SetSpotLight

inline DWORD F2DW( float v )
{
	return *((DWORD*)&v);
}

D3DFOGMODE ConvertFogMode( sg::Fog::FogMode mode )
{
	if (mode == sg::Fog::fmLinear) return D3DFOG_LINEAR;
	if (mode == sg::Fog::fmExp) return D3DFOG_EXP;
	if (mode == sg::Fog::fmExp2) return D3DFOG_EXP2;
	return D3DFOG_NONE;
}

void D3DRenderSystem::SetFog( sg::Fog* pFog )
{
	if (pFog == NULL) 
	{
		DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGENABLE, FALSE ) );
		return; 
	}

	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGENABLE,	TRUE ) );

	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGCOLOR,	pFog->GetColor()		 ) );

	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGSTART,	F2DW(pFog->GetStart())	 ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGEND,		F2DW(pFog->GetEnd())	 ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGDENSITY,	F2DW(pFog->GetDensity()) ) );

	if (pFog->GetType() == sg::Fog::ftVertex)
	{
		DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGVERTEXMODE, ConvertFogMode( pFog->GetMode() ) ) );
		DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGTABLEMODE, D3DFOG_NONE ) );
		DX_CHK( m_pDevice->SetRenderState( D3DRS_RANGEFOGENABLE, F2DW(pFog->GetIsRangeBased()) ) );

	}
	else if (pFog->GetType() == sg::Fog::ftPixel)
	{
		DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGTABLEMODE, ConvertFogMode( pFog->GetMode() ) ) );
		DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGVERTEXMODE, D3DFOG_NONE ) );
	}

} // D3DRenderSystem::SetFog

void D3DRenderSystem::SetBumpMatrix( const Matrix3D& matr, int stage )
{
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT00, F2DW( matr.e00 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT10, F2DW( matr.e10 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT01, F2DW( matr.e01 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT11, F2DW( matr.e11 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVLSCALE,  F2DW(matr.e22) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVLOFFSET, F2DW(matr.e20) ) );
} // D3DRenderSystem::SetBumpMatrix

//  O== INTERNAL METHODS ========================================O
HRESULT D3DRenderSystem::InvalidateDeviceObjects()
{
	for (int i = 0; i < c_MaxTextureStages; i++)
	{
		SetTexture( 0, i );
	}

	SetCurrentShader( 0 );
	SetRenderTarget	( 0 );

	m_PrimitiveCache.InvalidateDeviceObjects();
	m_TextureManager.InvalidateDeviceObjects();
	m_ShaderCache.InvalidateDeviceObjects();

	SAFE_DECREF( m_pBackBuffer );
	SAFE_DECREF( m_pDepthStencil );

	return S_OK;
} // D3DRenderSystem::InvalidateDeviceObjects

/*
void D3DRenderSystem::GetDeviceCaps( DevCaps& caps )
{
	if (m_pD3D)
	{
		D3DADAPTER_IDENTIFIER8 id;
		m_pD3D->GetAdapterIdentifier( d3dCaps.AdapterOrdinal, 0, &id );
		strncpy( caps.devDescr,	id.Description, c_MaxDevCapsStr - 1 );
		strncpy( caps.devDriver, id.Driver, c_MaxDevCapsStr - 1 );

	}

	if (d3dCaps.DeviceType == D3DDEVTYPE_HAL) caps.devType = dtHAL;
		else caps.devType = dtREF;

	caps.adapterOrdinal = d3dCaps.AdapterOrdinal;

	if (d3dCaps.DevCaps & D3DDEVCAPS_NPATCHES) 
			caps.hwNPatches = true; else caps.hwNPatches = false;
	if (d3dCaps.DevCaps & D3DDEVCAPS_HWRASTERIZATION  ) 
			caps.hwRasterization = true; else caps.hwRasterization = false;
	if (d3dCaps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT ) 
			caps.hwTnL = true; else caps.hwTnL = false;
	if (d3dCaps.DevCaps & D3DDEVCAPS_PUREDEVICE) 
			caps.hwPure  = true; else caps.hwPure = false;
	if (d3dCaps.DevCaps & D3DDEVCAPS_QUINTICRTPATCHES) 
			caps.hwBezier = true; else caps.hwBezier = false;
	if (d3dCaps.DevCaps & D3DDEVCAPS_RTPATCHES ) 
			caps.hwRTPatches = true; else caps.hwRTPatches = false;

	caps.texBlendStages		= d3dCaps.MaxTextureBlendStages;
	caps.texInSinglePass		= d3dCaps.MaxSimultaneousTextures;

	caps.dsfD16			= _CheckDSAvail( D3DFMT_D16,			d3dCaps );
	caps.dsfD15S1		= _CheckDSAvail( D3DFMT_D15S1,			d3dCaps );
	caps.dsfD24X8		= _CheckDSAvail( D3DFMT_D24X8,			d3dCaps );
	caps.dsfD24S8		= _CheckDSAvail( D3DFMT_D24S8,			d3dCaps );
	caps.dsfD24X4S4		= _CheckDSAvail( D3DFMT_D24X4S4,		d3dCaps );
	caps.dsfD32			= _CheckDSAvail( D3DFMT_D32,			d3dCaps );
	caps.dsfD16Lockable	= _CheckDSAvail( D3DFMT_D16_LOCKABLE,	d3dCaps );

	caps.rttARGB4444	= _CheckRenderToTexAvail( D3DFMT_A4R4G4B4,	d3dCaps );
	caps.rttRGB565		= _CheckRenderToTexAvail( D3DFMT_R5G6B5,	d3dCaps );
	caps.rttRGB888		= _CheckRenderToTexAvail( D3DFMT_R8G8B8,	d3dCaps );
	caps.rttA8			= _CheckRenderToTexAvail( D3DFMT_A8,		d3dCaps );
	caps.rttARGB1555	= _CheckRenderToTexAvail( D3DFMT_A1R5G5B5,	d3dCaps );

	/*
	D3DCAPS8 d3dCaps;
    m_pDevice->GetDeviceCaps( &d3dCaps );

	switch (cap)
	{
	case dcIndexedVertexBlending:
		return (d3dCaps.MaxVertexBlendMatrixIndex >= 255);
	case dcBumpEnvMap:
		return ((d3dCaps.TextureOpCaps & D3DTEXOPCAPS_BUMPENVMAP) != 0);
	case dcBumpEnvMapLuminance:
		return ((d3dCaps.TextureOpCaps & D3DTEXOPCAPS_BUMPENVMAPLUMINANCE) != 0);
	default:
		return false;
	}
	*/

	/*
	if (!m_pD3D) return false;
	D3DDISPLAYMODE mode;
	m_pD3D->GetAdapterDisplayMode( d3dCaps.AdapterOrdinal, &mode );
	return (m_pD3D->CheckDeviceFormat( d3dCaps.AdapterOrdinal, 
									 D3DDEVTYPE_HAL, 
									 mode.Format, 
									 D3DUSAGE_DEPTHSTENCIL,
									 D3DRTYPE_SURFACE,
									 fmt
									 ) == D3D_OK);

  if (!m_pD3D) return false;
	D3DDISPLAYMODE mode;
	m_pD3D->GetAdapterDisplayMode( d3dCaps.AdapterOrdinal, &mode );
	return (m_pD3D->CheckDeviceFormat( d3dCaps.AdapterOrdinal, 
									 D3DDEVTYPE_HAL, 
									 mode.Format, 
									 D3DUSAGE_RENDERTARGET,
									 D3DRTYPE_TEXTURE,
									 fmt
									 ) == D3D_OK);
	
} // D3DRenderSystem::_FillDevCaps
*/

const int			c_NumDeviceTypes	= 2;
const char*			c_StrDeviceDescs[]	= {"HAL", "REF" };
const D3DDEVTYPE	c_DeviceTypes[]		= { D3DDEVTYPE_HAL, D3DDEVTYPE_REF };

static int SortModesCallback( const VOID* arg1, const VOID* arg2 )
{
    D3DDISPLAYMODE* p1 = (D3DDISPLAYMODE*)arg1;
    D3DDISPLAYMODE* p2 = (D3DDISPLAYMODE*)arg2;

    if( p1->Format > p2->Format )   return -1;
    if( p1->Format < p2->Format )   return +1;
    if( p1->Width  < p2->Width )    return -1;
    if( p1->Width  > p2->Width )    return +1;
    if( p1->Height < p2->Height )   return -1;
    if( p1->Height > p2->Height )   return +1;

    return 0;
}

int	D3DRenderSystem::GetNDisplayModes()
{
	return m_CurDeviceInfo->nModes;
} // D3DRenderSystem::GetNDisplayModes

void D3DRenderSystem::GetDisplayMode( int idx, int& width, int& height )
{
	D3DModeInfo& modeInfo = m_CurDeviceInfo->modes[idx];
	width	= modeInfo.width;
	height	= modeInfo.height;
} // D3DRenderSystem::GetDisplayMode

int D3DRenderSystem::CreateNormalMap( int texID, float amplitude )
{
    const TextureDescr*  pDescr = m_TextureManager.GetTextureDescr( texID );
    if (!pDescr) return -1;
    
    char name[256];
    sprintf( name, "%s_NormalMap", m_TextureManager.GetTextureName( texID ) );
    int dstID = m_TextureManager.CreateTexture( name, *pDescr );
    
    DXTexture* pSrc  = m_TextureManager.GetDXTex( texID );
    DXTexture* pDest = m_TextureManager.GetDXTex( dstID );
    if (!pSrc || !pDest) return -1;

    D3DXComputeNormalMap( pDest, pSrc, NULL, 0, D3DX_CHANNEL_RED, amplitude );
    return dstID;
} // D3DRenderSystem::CreateNormalMap

/*---------------------------------------------------------------------------*/
/*	Func:	D3DRenderSystem::BuildDeviceList
/*	Desc:	Builds list of available devices and their display modes
/*	Remark:	Adopted from D3D samples 
/*---------------------------------------------------------------------------*/
void D3DRenderSystem::BuildDeviceList()
{
	assert( m_pD3D );
	int totalAdapters = m_pD3D->GetAdapterCount();
	m_NAdapters = 0;
    for (int i = 0; i < totalAdapters; i++)
    {
        // Fill in adapter info
        D3DAdapterInfo& curAdapter = m_AdapterList[i];
        m_pD3D->GetAdapterIdentifier( i, D3DENUM_NO_WHQL_LEVEL, &(curAdapter.adapterID) );
        m_pD3D->GetAdapterDisplayMode( i, &(curAdapter.desktopDisplayMode) );
        curAdapter.nDevices	= 0;

        // Enumerate all display modes on this adapter
        D3DDISPLAYMODE	modes[c_MaxDeviceDisplayModes];
        D3DFORMAT		formats[c_MaxDisplayFormatsInMode];
        int				numFormats  = 0;
        int				numModes	= 0;
		int				totalModes	= m_pD3D->GetAdapterModeCount( i );

        // Add the adapter's current desktop format to the list of formats
        formats[numFormats++] = curAdapter.desktopDisplayMode.Format;

        for (int j = 0; j < totalModes; j++)
        {
            //  getting next display mode
			D3DDISPLAYMODE dispMode;
            m_pD3D->EnumAdapterModes( i, j, &dispMode );
            
			if (dispMode.Width  < 640 || dispMode.Height < 400) continue;

			//  we select display mode with highest refresh rate available
            bool addMode = true;
			int k = 0;
			for (; k < numModes; k++)
            {
                if ((modes[k].Width  == dispMode.Width ) &&
                    (modes[k].Height == dispMode.Height) &&
                    (modes[k].Format == dispMode.Format))
				{
					addMode = false;
					if (modes[k].RefreshRate < dispMode.RefreshRate &&
                        dispMode.RefreshRate <= g_RefreshRateBias)
					{
						modes[k] = dispMode;
					}
				}
            }

            if (k == numModes && addMode)
            {
                modes[numModes].Width       = dispMode.Width;
                modes[numModes].Height      = dispMode.Height;
                modes[numModes].Format      = dispMode.Format;
                modes[numModes].RefreshRate = dispMode.RefreshRate;
                numModes++;

                //  check if the mode's format already exists
                int n = 0;
				for (; n < numFormats; n++)
                {
                    if (dispMode.Format == formats[n]) break;
                }

                if (n == numFormats) formats[numFormats++] = dispMode.Format;
            }
        }
		
        //  sort the list of display modes (by format, then width, then height)
        qsort( modes, numModes, sizeof(D3DDISPLAYMODE), SortModesCallback );

        //  add devices to adapter
        for (int k = 0; k < c_NumDeviceTypes; k++ )
        {
            D3DDeviceInfo& curDevice = curAdapter.devices[curAdapter.nDevices];
            curDevice.devType = c_DeviceTypes[k];
            m_pD3D->GetDeviceCaps( i, c_DeviceTypes[k], &curDevice.caps );
            curDevice.strDesc       = c_StrDeviceDescs[k];
            curDevice.nModes		= 0;
            curDevice.canDoWindowed = false;

            //  add all valid modes to device info
			for (int m = 0; m < numModes; m++)
            {
                for (int f = 0; f < numFormats; f++)
                {
                    if (modes[m].Format == formats[f])
                    {
                        //if( bFormatConfirmed[f] == TRUE )
                        {
                            //  add this mode to the device's list of valid modes
                            D3DModeInfo& curMode = curDevice.modes[curDevice.nModes];
							curMode.width				= modes[m].Width;
                            curMode.height				= modes[m].Height;
                            curMode.format				= modes[m].Format;
							curMode.refreshRate			= modes[m].RefreshRate;
                            /*
							curMode.behavior			= dwBehavior[f];
                            curMode.depthStencil		= fmtDepthStencil[f];
                            */
							curDevice.nModes++;
                        }
                    }
                }
            }

			if (curDevice.nModes > 0) curAdapter.nDevices++;

			/*
            // Select any 640x480 mode for default (but prefer a 16-bit mode)
            for( m=0; m<m_pDevice->dwNumModes; m++ )
            {
                if( m_pDevice->modes[m].Width==640 && m_pDevice->modes[m].Height==480 )
                {
                    m_pDevice->dwCurrentMode = m;
                    if( m_pDevice->modes[m].Format == D3DFMT_R5G6B5 ||
                        m_pDevice->modes[m].Format == D3DFMT_X1R5G5B5 ||
                        m_pDevice->modes[m].Format == D3DFMT_A1R5G5B5 )
                    {
                        break;
                    }
                }
            }

            // Check if the device is compatible with the desktop display mode
            // (which was added initially as formats[0])
            if( bFormatConfirmed[0] && (m_pDevice->d3dCaps.Caps2 & D3DCAPS2_CANRENDERWINDOWED) )
            {
                m_pDevice->bCanDoWindowed = TRUE;
                m_pDevice->bWindowed      = TRUE;
            }

            // If valid modes were found, keep this device
            if( m_pDevice->dwNumModes > 0 )
                pAdapter->dwNumDevices++;
        */
		}
        //  if valid devices were found, keep this adapter
        if (curAdapter.nDevices > 0) m_NAdapters++;
    }
	/*
    // Return an error if no compatible devices were found
    if( 0L == m_dwNumAdapters )
        return D3DAPPERR_NOCOMPATIBLEDEVICES;

    // Pick a default device that can render into a window
    // (This code assumes that the HAL device comes before the REF
    // device in the device array).
    for( DWORD a=0; a<m_dwNumAdapters; a++ )
    {
        for( DWORD d=0; d < m_Adapters[a].dwNumDevices; d++ )
        {
            if( m_Adapters[a].devices[d].bWindowed )
            {
                m_Adapters[a].dwCurrentDevice = d;
                m_dwAdapter = a;
                m_bWindowed = TRUE;

                // Display a warning message
                if( m_Adapters[a].devices[d].DeviceType == D3DDEVTYPE_REF )
                {
                    if( !bHALExists )
                        DisplayErrorMsg( D3DAPPERR_NOHARDWAREDEVICE, MSGWARN_SWITCHEDTOREF );
                    else if( !bHALIsSampleCompatible )
                        DisplayErrorMsg( D3DAPPERR_HALNOTCOMPATIBLE, MSGWARN_SWITCHEDTOREF );
                    else if( !bHALIsWindowedCompatible )
                        DisplayErrorMsg( D3DAPPERR_NOWINDOWEDHAL, MSGWARN_SWITCHEDTOREF );
                    else if( !bHALIsDesktopCompatible )
                        DisplayErrorMsg( D3DAPPERR_NODESKTOPHAL, MSGWARN_SWITCHEDTOREF );
                    else // HAL is desktop compatible, but not sample compatible
                        DisplayErrorMsg( D3DAPPERR_NOHALTHISMODE, MSGWARN_SWITCHEDTOREF );
                }

                return S_OK;
            }
        }
    }

    return D3DAPPERR_NOWINDOWABLEDEVICES;*/
} // D3DRenderSystem::BuildDeviceList

void D3DRenderSystem::DumpDeviceList( FILE* fp )
{
	fprintf( fp, "Dumping available device modes list...\n" );
	fprintf( fp, "Number of adapters: %d\n", m_NAdapters );
	for (int i = 0; i < m_NAdapters; i++)
	{
		m_AdapterList[i].Dump( fp );
	}
} // D3DRenderSystem::DumpDeviceList

DWORD D3DRenderSystem::CreateStateBlock( sg::StateBlock* pBlock )
{
	if (m_pDevice->BeginStateBlock() != S_OK) return 0xFFFFFFFF;
	DWORD id;
	pBlock->Node::Render();
	if (m_pDevice->EndStateBlock( &id ) != S_OK) return 0xFFFFFFFF;
	return id; 
} // D3DRenderSystem::CreateStateBlock

void D3DRenderSystem::SetRenderStateBlock( sg::RenderStateBlock* pBlock	)
{
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ZENABLE,				(DWORD)pBlock->m_bZEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_FILLMODE,				(DWORD)pBlock->m_FillMode ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_SHADEMODE,				(DWORD)pBlock->m_ShadeMode ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ZWRITEENABLE,			(DWORD)pBlock->m_bZWriteEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ALPHATESTENABLE, 		(DWORD)pBlock->m_bAlphaTestEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_SRCBLEND,				(DWORD)pBlock->m_SrcBlend ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_DESTBLEND,				(DWORD)pBlock->m_DestBlend ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_CULLMODE,				(DWORD)pBlock->m_CullMode ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ZFUNC,					(DWORD)pBlock->m_ZFunc ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ALPHAREF,				(DWORD)pBlock->m_AlphaRef ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ALPHAFUNC,				(DWORD)pBlock->m_AlphaFunc ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_DITHERENABLE,			(DWORD)pBlock->m_bDitherEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_ALPHABLENDENABLE,		(DWORD)pBlock->m_bAlphaBlendEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_FOGENABLE,				(DWORD)pBlock->m_bFogEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_SPECULARENABLE,		(DWORD)pBlock->m_bSpecularEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILENABLE,			(DWORD)pBlock->m_bStencilEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILFAIL,			(DWORD)pBlock->m_StencilFail ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILZFAIL,			(DWORD)pBlock->m_StencilZFail ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILPASS,			(DWORD)pBlock->m_StencilPass ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILFUNC,			(DWORD)pBlock->m_StencilFunc ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILREF,			(DWORD)pBlock->m_StencilRef ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILMASK,			(DWORD)pBlock->m_StencilMask ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_STENCILWRITEMASK,		(DWORD)pBlock->m_StencilWriteMask ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_TEXTUREFACTOR,			(DWORD)pBlock->m_TextureFactor ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_LIGHTING,				(DWORD)pBlock->m_bLighting ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_AMBIENT,				(DWORD)pBlock->m_Ambient ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_COLORVERTEX,			(DWORD)pBlock->m_bColorVertex ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_LOCALVIEWER,			(DWORD)pBlock->m_bSpecularLocalViewer ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_NORMALIZENORMALS,		(DWORD)pBlock->m_bNormalizeNormals ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_DIFFUSEMATERIALSOURCE,	(DWORD)pBlock->m_DiffuseMaterialSource ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_SPECULARMATERIALSOURCE,(DWORD)pBlock->m_SpecularMaterialSource ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_AMBIENTMATERIALSOURCE,	(DWORD)pBlock->m_AmbientMaterialSource ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_EMISSIVEMATERIALSOURCE,(DWORD)pBlock->m_EmissiveMaterialSource ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_VERTEXBLEND,			(DWORD)pBlock->m_VertexBlend ) );
	
	DWORD clip = 0;
	if (pBlock->m_bClipPlaneEnable[0]) clip |= 1 << 0;
	if (pBlock->m_bClipPlaneEnable[1]) clip |= 1 << 1;
	if (pBlock->m_bClipPlaneEnable[2]) clip |= 1 << 2;
	if (pBlock->m_bClipPlaneEnable[3]) clip |= 1 << 3;
	if (pBlock->m_bClipPlaneEnable[4]) clip |= 1 << 4;
	if (pBlock->m_bClipPlaneEnable[5]) clip |= 1 << 5;

	DX_CHK( m_pDevice->SetRenderState( D3DRS_CLIPPLANEENABLE,			clip ) );
	
	DX_CHK( m_pDevice->SetRenderState( D3DRS_SOFTWAREVERTEXPROCESSING,(DWORD)pBlock->m_bSoftwareVertexProcessing ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_COLORWRITEENABLE,		(DWORD)pBlock->m_ColorWriteEnable ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_TWEENFACTOR,				F2DW( pBlock->m_TweenFactor ) ) );
	DX_CHK( m_pDevice->SetRenderState( D3DRS_BLENDOP,					(DWORD)pBlock->m_Blendop ) );
} // D3DRenderSystem::SetRenderStateBlock

void D3DRenderSystem::SetTextureStateBlock( sg::TextureStateBlock* pBlock, int stage )
{
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_COLOROP,					(DWORD)pBlock->m_ColorOp ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_COLORARG1,				(DWORD)pBlock->m_ColorArg1 ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_COLORARG2,				(DWORD)pBlock->m_ColorArg2 ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ALPHAOP,					(DWORD)pBlock->m_AlphaOp ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ALPHAARG1, 				(DWORD)pBlock->m_AlphaArg1 ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ALPHAARG2,				(DWORD)pBlock->m_AlphaArg2 ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT00,			F2DW( pBlock->m_BumpEnvMat00 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT01,			F2DW( pBlock->m_BumpEnvMat01 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT10,			F2DW( pBlock->m_BumpEnvMat10 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVMAT11,			F2DW( pBlock->m_BumpEnvMat11 ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_TEXCOORDINDEX,			(DWORD)pBlock->m_TexCoordIndex ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ADDRESSU,				(DWORD)pBlock->m_AddressU ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ADDRESSV,				(DWORD)pBlock->m_AddressV ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BORDERCOLOR,				(DWORD)pBlock->m_BorderColor ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MAGFILTER,				(DWORD)pBlock->m_MagFilter ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MINFILTER,				(DWORD)pBlock->m_MinFilter ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MIPFILTER,				(DWORD)pBlock->m_MipFilter ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MIPMAPLODBIAS,			(DWORD)pBlock->m_MipmapLodBias ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MAXMIPLEVEL,				(DWORD)pBlock->m_MaxMipLevel ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MIPMAPLODBIAS,			F2DW( pBlock->m_MipmapLodBias ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_MAXANISOTROPY,			(DWORD)pBlock->m_MaxAnisotropy ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVLSCALE,			F2DW( pBlock->m_BumpEnvlScale ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_BUMPENVLOFFSET,			F2DW( pBlock->m_BumpEnvlOffset ) ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_TEXTURETRANSFORMFLAGS,	(DWORD)pBlock->m_TextureTransformFlags ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ADDRESSW,				(DWORD)pBlock->m_AddressW ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_COLORARG0,				(DWORD)pBlock->m_ColorArg0 ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_ALPHAARG0,				(DWORD)pBlock->m_AlphaArg0 ) );
	DX_CHK( m_pDevice->SetTextureStageState( stage, D3DTSS_RESULTARG,				(DWORD)pBlock->m_ResultArg ) );
} // D3DRenderSystem::SetTextureStateBlock

void D3DRenderSystem::AddOnDestroyNotified( IDeviceClient* iNotify )
{
	m_NotifyDestroy.push_back( iNotify );
}

/*****************************************************************************/
/*	D3DModeInfo implementation
/*****************************************************************************/
void D3DModeInfo::Dump( FILE* fp )
{
	fprintf( fp, "%4d x %4d x %2d", width, height, format == D3DFMT_X8R8G8B8 ? 32 : 16 );
	fprintf( fp, " %3dHz ", refreshRate );

	switch (depthStencil)
	{
	case D3DFMT_D16:
		fprintf( fp, "(D16)\n" );
		break;
	case D3DFMT_D15S1:
	    fprintf( fp, "(D15S1)\n" );
		break;
	case D3DFMT_D24X8:
	    fprintf( fp, "(D24X8)\n" );
		break;
	case D3DFMT_D24S8:
	    fprintf( fp, "(D24S8)\n" );
	    break;
	case D3DFMT_D24X4S4:
	    fprintf( fp, "(D24X4S4)\n" );
	    break;
	case D3DFMT_D32:
	    fprintf( fp, "(D32)\n" );
	    break;
	default:
		fprintf( fp, "\n" );
	}
 
} // D3DModeInfo::Dump

/*****************************************************************************/
/*	D3DDeviceInfo implementation
/*****************************************************************************/
void D3DDeviceInfo::Dump( FILE* fp )
{
	fprintf( fp, "Device: %s NumModes: %d ", 
					strDesc, nModes );
	if (canDoWindowed) fprintf( fp, "WindowMode: Yes\n" );
		else fprintf( fp, "WindowMode: No\n" );
	for (int i = 0; i < nModes; i++)
	{
		modes[i].Dump( fp );
	}
} // D3DModeInfo::Dump

D3DModeInfo* D3DDeviceInfo::FindMode( int width, int height )
{
	for (int i = 0; i < nModes; i++)
	{
		if (modes[i].width == width && modes[i].height == height)
		{
			return &(modes[i]);
		}
	}
	return NULL;
} // D3DDeviceInfo::FindMode

/*****************************************************************************/
/*	D3DAdapterInfo implementation
/*****************************************************************************/
void D3DAdapterInfo::Dump( FILE* fp )
{
	fprintf( fp, "Adapter: %s\nDriver: %s (Ver %d)\nNum Devices: %d\n", 
					adapterID.Description, 
					adapterID.Driver, 
					adapterID.DriverVersion,
					nDevices );
	for (int i = 0; i < nDevices; i++)
	{
		devices[i].Dump( fp );
	}
} // D3DAdapterInfo::Dump

D3DDeviceInfo* D3DAdapterInfo::FindDevice( D3DDEVTYPE devType )
{
	for (int i = 0; i < nDevices; i++)
	{
		if (devices[i].devType == devType) return &(devices[i]);
	}
	return NULL;
} // D3DAdapterInfo::FindDevice




