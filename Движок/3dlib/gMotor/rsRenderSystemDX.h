/*****************************************************************
/*  File:   rsRenderSystemDX.h                                   
/*  Desc:   Direct3D rendering system                             
/*  Author: Silver, Copyright (C) GSC Game World                  
/*  Date:   Feb 2002                                             
/*****************************************************************/
#ifndef __D3DRENDERSYSTEM_H__
#define __D3DRENDERSYSTEM_H__

#include "IRenderSystem.h"
#include "rsSettings.h"

const int c_MaxAdapters				= 3;
const int c_MaxAdapterDevices		= 5;
const int c_MaxDeviceDisplayModes	= 150;
const int c_MaxDisplayFormatsInMode	= 10;

const int c_MaxLights				= 8;

/*****************************************************************************/
/*	Class:	D3DModeInfo
/*	Desc:	Direct3D display mode description
/*****************************************************************************/
class D3DModeInfo
{
public:
	void			Dump( FILE* fp );

protected:
	int			width;
	int			height;
	int			refreshRate;
	D3DFORMAT	format;
	DWORD		behavior;
	D3DFORMAT	depthStencil;

private:
	friend class D3DRenderSystem;
	friend class D3DDeviceInfo;
}; // class D3DModeInfo

/*****************************************************************************/
/*	Class:	D3DDeviceInfo
/*	Desc:	Direct3D device description
/*****************************************************************************/
class D3DDeviceInfo
{
public:
	D3DDeviceInfo() : nModes(0) {}

	void			Dump( FILE* fp );
	D3DModeInfo*	FindMode( int width, int height );

protected:
	D3DDEVTYPE		devType;
    DXCaps			caps;
	const char*		strDesc;
	bool			canDoWindowed;

    int				nModes;
	D3DModeInfo		modes[c_MaxDeviceDisplayModes];

private:
	friend class D3DRenderSystem;
	friend class D3DAdapterInfo;
}; // class D3DDeviceInfo

/*****************************************************************************/
/*	Class:	D3DAdapterInfo
/*	Desc:	Direct3D adapter info
/*****************************************************************************/
class D3DAdapterInfo
{
public:
	D3DAdapterInfo() : nDevices(0) {}
	
	void			Dump( FILE* fp );
	D3DDeviceInfo*	FindDevice( D3DDEVTYPE devType );

protected:
    D3DADAPTER_IDENTIFIER8		adapterID;
    D3DDISPLAYMODE				desktopDisplayMode;
    int							nDevices;
    D3DDeviceInfo				devices[c_MaxAdapterDevices];

private:
	friend class D3DRenderSystem;
}; // class D3DAdapterInfo

class IniFile;
class Shader;
class Input;

/*****************************************************************
/*	Class:	D3DRenderSystem
/*	Desc:	Direct3D rendering system
/*****************************************************************/
class D3DRenderSystem : public Singleton<D3DRenderSystem>, public IRenderSystem
{
public:
						D3DRenderSystem	    ();
						~D3DRenderSystem    ();

	void 				Init			    ( HWND hWnd, bool subclassWnd = true );
	void 				ShutDown		    ();
	
    void 				SetViewMatrix	    ( const Matrix4D& vmatr );
	void 				SetProjectionMatrix ( const Matrix4D& pmatr );
	void 				SetWorldMatrix	    ( const Matrix4D& wmatr );

    const Matrix4D&     GetViewMatrix       () const { return m_ViewMatrix;         }
    const Matrix4D&     GetProjectionMatrix () const { return m_ProjectionMatrix;   }
    const Matrix4D&     GetWorldMatrix      () const { return m_WorldMatrix;        } 

	void				ResetWorldMatrix();
	void 				PushWorldMatrix	( const Matrix4D& wmatr );
	const Matrix4D&		PopWorldMatrix	();

	void				SetTextureMatrix( const Matrix4D& tmatr, int stage = 0 );
	void				SetBumpMatrix	( const Matrix3D& bmatr, int stage = 0 );

	void 				ClearDeviceTarget( DWORD color = 0x00000000 );
	void 				ClearDeviceZBuffer();
	void 				ClearDeviceStencil();
	void 				ClearDevice		( bool bColor, DWORD color = 0x00000000, 
													bool bDepth = true, bool bStencil = true );

	void				AddOnDestroyNotified( IDeviceClient* iNotify );


	void  				PresentBackBuffer( const RECT* rect = 0 );
	void  				BeginScene		();
	void  				EndScene		();
	void  				Draw			( BaseMesh& bm );
	void  				DrawPrim		( BaseMesh& bm );

	bool				SetCursor		( int texID, const Rct& rctOnTex, int hotspotX = 0, int hotspotY = 0 );
	bool				UpdateCursor	( int x, int y, bool drawNow = false );
	void				ShowCursor		( bool bShow = true );

	void 				SetTexture		( int texID, int stage = 0 );
	int 				GetTexture		( int stage = 0 );

	void 				SaveTexture		( int texID, const char* fname	);
	bool 				DeleteTexture	( int texID );
	BYTE* 				LockTexBits		( int texID, int& pitch, int level = 0	);
    BYTE* 			    LockTexBits		( int texID, const Rct& rect, int& pitch, int level = 0	);

	void  				UnlockTexBits	( int texID, int level = 0				);
	
	Rct 				SetViewPort		( const Rct& vp );
	Rct					GetViewPort		() const;
	void				SetViewPort		( float x, float y, float w, float h, float zn = 0.0f, float zf = 1.0f );

	bool 				SetRenderTarget	( int texID, int dsID = 0 );

	void*				DbgGetDevice	();
	void				OnFrame			();

	void 				Dump			( const char* fname = 0 );

	void 				SetCurrentShader( int shaderID );
	int	 				GetShaderID		( const char* shaderName, BYTE* shBuf = NULL, int size = 0 );
	const char*			GetShaderName	( int shID ) const;

	bool 				IsShaderValid	( const char* shaderName );
	virtual bool		SetClipPlane	( DWORD idx, const Plane& plane );

	int	 				GetTextureID	( const char* texName, const TextureDescr& td, BYTE* pMemFile = 0, int memFileSize = 0 );
	int	 				GetTextureID	( const char* texName, BYTE* pMemFile = 0, int memFileSize = 0 );
	const char*			GetTextureName	( int texID );

	int					LoadTexture		( const char* texName, const TextureDescr& td, BYTE* pMemFile = 0, int memFileSize = 0 );
	int					LoadTexture		( const char* texName, BYTE* pMemFile = 0, int memFileSize = 0 );
	int	 				CreateTexture	( const char* texName, const TextureDescr& td );

	void				SetRootDir			( const char* rootDir );
	bool				SetScreenProperties	( const ScreenProp& prop );	
	ScreenProp			GetScreenProperties	();	

	void				CreateMipLevels		( int texID );
	void 				BindToTexture		( BaseMesh* drawn );
    void                CopyTexture         ( int destID, int srcID, const Rct* rct = NULL, int nRect = 1 );


	bool 				RecompileAllShaders	();
	bool 				ReloadAllTextures	();

	void 				SetTextureFactor	( DWORD tfactor );
	void				SetZEnable			( bool bEnable = true );
	void				SetZWriteEnable		( bool bEnable = true );
    void  		        SetDitherEnable		( bool bEnable = true );

	void				SetAlphaRef			( BYTE alphaRef );
	void  				SetWireframe		( bool bEnable = true );

	IVertexShader*		CreateVertexShader	() { return new DXVertexShader(); }
	bool				SetVSConstant		( int cIdx, const Vector4D& cVec );
	bool				SetVSConstant		( int cIdx, const Matrix4D& cMatr, bool bFull = true );


	int	 				GetVBufferID		();
	void 				GetClientSize		( int& width, int& height );
	int					GetTextureSizeBytes	( int texID );
	const TextureDescr*	GetTextureDescr		( int texID );

	int  				GetTexMemorySize();

	void				SetDirectionalLight	( sg::DirectionalLight*	pLight, int& index );
	void				SetPointLight		( sg::PointLight*		pLight, int& index );
	void				SetSpotLight		( sg::SpotLight*		pLight, int& index );
	void				DisableLights		();

	void 				SetMaterial			( sg::Material* pMaterial );
	void				SetFog				( sg::Fog* pFog = NULL );

	const char*			GetRootDir          ();
	
	void				SetRenderStateBlock	( sg::RenderStateBlock* pBlock			   );
	void				SetTextureStateBlock( sg::TextureStateBlock* pBlock, int stage );
	
	DWORD				CreateStateBlock	( sg::StateBlock* pBlock );
	bool				ApplyStateBlock		( DWORD id );
	bool				DeleteStateBlock	( DWORD id );


	bool				ApplyStateBlock		( DSBlock* pBlock );
	DWORD				CreateStateBlock	( DSBlock* pBlock );
	bool				GetCurrentStateBlock( DSBlock& dsb ) const;
	DXDevice*			GetDevice			() { return m_pDevice; }
	DXSurface*			GetTexSurface		( int texID );

	bool				VSMode              () const { return m_bVSMode; }
	void				SetVSMode           ( bool val = true ) { m_bVSMode = val; }

	int					GetNDisplayModes	();
	void				GetDisplayMode		( int idx, int& width, int& height );
    int                 CreateNormalMap     ( int texID, float amplitude = 1.0f );

    BYTE*		        LockVertexBuffer	( int vbID ) { return NULL; }
    void		        UnlockVertexBuffer	( int vbID ) {}

protected:
	bool				ResetDevice					();
	
	HRESULT				InitDeviceObjects			();
	HRESULT				RestoreDeviceObjects		();
	HRESULT				InvalidateDeviceObjects		();

    bool                SetupDisplaySettings        ();

	void				AdjustWindowPos				( int x, int y, int w, int h );
	bool				InitD3D						();
	bool				InitDeviceD3D				();
	bool				ShutDeviceD3D				();

	void				GetPresentParameters		( D3DPRESENT_PARAMETERS& presParm );
	void				BuildDeviceList				();
	void				DumpDeviceList				( FILE* fp );
	void				RestoreDesktopDisplayMode	();

	DWORD				GetCurFrame					() const { return m_CurFrame; }

    int					GetVertexShaderID           ( const char* shaderName );
    int					ApplyVertexShader           ( DWORD id );

private:
	DXAPI*					m_pD3D;
	DXDevice*				m_pDevice;
	bool					m_bVSMode;
	
	DXSurface*				m_pRenderTarget; 
	DXSurface*				m_pBackBuffer;
	DXSurface*				m_pDepthStencil;

	char					m_RootDirectory[_MAX_PATH];
	HWND					m_hRenderWindow;
	
	int						m_RenderTargetID;
	D3DVIEWPORT8			m_CurViewPort;
    D3DVIEWPORT8			m_FullViewPort;
	int						m_VBufID;
	DWORD					m_CurStateBlockID;

	Settings				m_Settings;
	ScreenProp				m_ScreenProp;
	DEVMODE					m_dmDesktop;

	TextureManager			m_TextureManager;

	ShaderCache				m_ShaderCache;
	PrimitiveCache			m_PrimitiveCache;

	D3DAdapterInfo			m_AdapterList[c_MaxAdapters];
	int						m_NAdapters;
	D3DDeviceInfo*			m_CurDeviceInfo;

	TextureDescr			m_CursorTD;
	int						m_NumActiveLights;

	DWORD					m_CurFrame;
    bool                    m_bInited;

    Matrix4D                m_ViewMatrix;
    Matrix4D                m_ProjectionMatrix;
    Matrix4D                m_WorldMatrix;

	std::vector<DXVertexShader>		m_VertexShaders;
	std::vector<IDeviceClient*>	m_NotifyDestroy;
}; // class D3DRenderSystem

#endif // __D3DRENDERSYSTEM_H__