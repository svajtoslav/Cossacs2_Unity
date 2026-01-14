/*****************************************************************
/*  File:   IRenderSystem.h                                      *
/*  Desc:   3D rendering device abstract interface               *
/*  Date:   Feb 2002											 *
/*****************************************************************/
#ifndef __RENDERSYSTEM_H__ 
#define __RENDERSYSTEM_H__
#pragma	once

#include "windows.h"
#include "gmDefines.h"
#include "kColorValue.h"
#include "rsRenderSystem.h"

enum VertexFormat
{
	vfUnknown		= 0,
	vfTnL			= 1,
	vf2Tex			= 2,
	vfN				= 3,
	vfTnL2			= 4,
	vfT				= 5,
	vfMP1			= 6,
	vfNMP1			= 7,
	vfTnL2S			= 8,
	vfNMP2			= 9,
	vfNMP3			= 10,
	vfNMP4			= 11,
	vfN2T			= 12,
	vfXYZD			= 13,
	vfXYZW			= 14,
	vfTS			= 15,

	vf1W			= 16,
	vf2W			= 17,
	vf3W			= 18,
	vf4W			= 19,

	vfLAST			= 20
};  // enum VertexFormat

const int c_NumVertexTypes = (int)vfLAST;

class Matrix4D;
class Vector4D;
class Matrix3D;
class Plane;
class BaseMesh;
class DSBlock;

namespace sg
{
	class DirectionalLight;
	class PointLight;
	class SpotLight;

	class Material;
	class Fog;

	class RenderStateBlock;
	class TextureStateBlock;
	class StateBlock;
}

/*****************************************************************
/*  Class:  IVertexShader, abstract interface                                  
/*  Desc:   Interface to operate vertex shader
/*****************************************************************/
class IVertexShader
{
public:
	virtual bool			Create			( const char* name ) = 0;
	virtual bool			Apply			() = 0;
	virtual void			Reset			() = 0;
	virtual const char*		GetName			() const = 0;
	virtual bool			SetVertexFormat	( VertexFormat vf ) = 0;
	virtual void			SetSoftwareMode ( bool val = true ) = 0;
	virtual bool			IsLoaded		() = 0;
}; // class IVertexShader

class IDeviceClient;
/*****************************************************************
/*  Class:  IRenderSystem, abstract interface                                  
/*  Desc:   Incapsulates all rendering commands	passed to hardware 
/*				API				 
/*****************************************************************/
class  IRenderSystem  
{
public:
//  O==  PUBLIC INTERFACE  =======================================O
	//  initialize render device 
	virtual void			Init			( HWND hWnd, bool subclassWnd = true ) = 0;
	//  close & cleanup
	virtual void			ShutDown		() = 0;

	//  -- Rendering----------------------------------------------
	//  current observer view matrix
	virtual void			SetViewMatrix	( const Matrix4D& vmatr ) = 0;
    virtual const Matrix4D& GetViewMatrix   () const = 0;

	//  current camera projection matrix
	virtual void			SetProjectionMatrix( const Matrix4D& pmatr ) = 0;
    virtual const Matrix4D& GetProjectionMatrix() const = 0;

    //  current model/world transformation
	virtual void			SetWorldMatrix	( const Matrix4D& wmatr ) = 0;
    virtual const Matrix4D& GetWorldMatrix  () const = 0;

	virtual void			ResetWorldMatrix() = 0;
	virtual void			SetTextureMatrix( const Matrix4D& tmatr, int stage = 0 ) = 0;
	virtual void			SetBumpMatrix	( const Matrix3D& bmatr, int stage = 0 ) = 0;	
	virtual void			GetClientSize	( int& width, int& height ) = 0;

	virtual void			AddOnDestroyNotified( IDeviceClient* iNotify ) = 0;

	//  clears backbuffer 
	virtual void  			ClearDeviceTarget( DWORD color = 0x00000000 ) = 0;
	virtual void  			ClearDeviceZBuffer() = 0;
	virtual void  			ClearDeviceStencil() = 0;

	virtual void  			ClearDevice		( bool bColor, DWORD color = 0, bool bDepth = true, bool bStencil = true ) = 0;
	virtual void  			PresentBackBuffer( const RECT* rect = 0 ) = 0;
	
	//  hardware cursor support
	virtual bool  			SetCursor		( int texID, const Rct& rctOnTex, int hotspotX = 0, int hotspotY = 0 ) = 0;
	virtual bool  			UpdateCursor	( int x, int y, bool drawNow = false ) = 0;
	virtual void  			ShowCursor		( bool bShow = true ) = 0;
	
	//  begin/end scene
	virtual void  			BeginScene		() = 0;
	virtual void  			EndScene		() = 0;
	virtual void  			OnFrame			() = 0;

	virtual void  			Draw			( BaseMesh& bm ) = 0;
	virtual void  			DrawPrim		( BaseMesh& bm ) = 0;

	virtual bool  			RecompileAllShaders() = 0;
	virtual bool  			ReloadAllTextures() = 0;

	virtual bool  			SetScreenProperties	( const ScreenProp& prop ) = 0;	
	virtual ScreenProp		GetScreenProperties	() = 0;	

	
	//  -- Texture management -----------------------------------------
	virtual void			SetTexture		( int texID, int stage = 0 ) = 0;
	virtual int				GetTexture		( int stage = 0 ) = 0;

	//  saves texture to the file
	virtual void			SaveTexture		( int texID, const char* fname ) = 0;
	
	//  copies one texture to another
    virtual void            CopyTexture     ( int destID, int srcID, const Rct* rct = NULL, int nRect = 1 ) = 0;
    
    virtual int				CreateTexture	( const char* texName, const TextureDescr& td ) = 0;
	virtual void			CreateMipLevels	( int texID ) = 0;
	virtual int				GetTextureID	( const char* texName, const TextureDescr& td, BYTE* pMemFile = 0, int memFileSize = 0 ) = 0;
	virtual int				GetTextureID	( const char* texName, BYTE* pMemFile = 0, int memFileSize = 0 ) = 0;

	virtual int				LoadTexture		( const char* texName, const TextureDescr& td, BYTE* pMemFile = 0, int memFileSize = 0 ) = 0;
	virtual int				LoadTexture		( const char* texName, BYTE* pMemFile = 0, int memFileSize = 0 ) = 0;
    
    virtual int             CreateNormalMap ( int texID, float amplitude = 1.0f ) = 0;

	virtual const TextureDescr*	  GetTextureDescr( int texID )	= 0;
	virtual bool			DeleteTexture	( int texID ) = 0;
	virtual const char*		GetTextureName	( int texID ) = 0;
	virtual int				GetShaderID		( const char* shaderName, BYTE* shBuf = NULL, int size = 0 ) = 0;
	virtual const char*		GetShaderName	( int shID ) const = 0;

	virtual IVertexShader*	CreateVertexShader() = 0;	
	virtual bool			SetVSConstant	( int cIdx, const Vector4D& cVec ) = 0;
	virtual bool			SetVSConstant	( int cIdx, const Matrix4D& cMatr, bool bFull = true ) = 0;
	
	
	virtual bool		SetClipPlane		( DWORD idx, const Plane& plane ) = 0;

	//  obtain pointer to the texture pixel data
	virtual BYTE*		LockTexBits			( int texID, int& pitch, int level = 0 ) = 0;
    virtual BYTE* 		LockTexBits		    ( int texID, const Rct& rect, int& pitch, int level = 0	) = 0;

	//  unlock texture pixel data 
	virtual void		UnlockTexBits		( int texID, int level = 0 )	= 0;

    //  lock vertex buffer data
    virtual BYTE*		LockVertexBuffer	( int vbID ) = 0;
    //  unlock vertex buffer data 
    virtual void		UnlockVertexBuffer	( int vbID ) = 0;

	virtual int			GetTextureSizeBytes	( int texID ) = 0;
	virtual bool		SetRenderTarget		( int texID, int dsID = 0 ) = 0;
	virtual Rct			SetViewPort			( const Rct& vp ) = 0;
	virtual Rct			GetViewPort			() const = 0;
	virtual void		SetViewPort			( float x, float y, float w, float h, float zn = 0.0f, float zf = 1.0f ) = 0;
	
	
	virtual int			GetVBufferID		() = 0;
	virtual void  		SetCurrentShader	( int shaderID ) = 0;
	virtual bool  		IsShaderValid		( const char* shaderName ) = 0;
	virtual int   		GetTexMemorySize() = 0;

	//  some mucking with rendering sataes
	virtual void  		SetTextureFactor	( DWORD tfactor		  ) = 0;
	virtual void  		SetAlphaRef			( BYTE alphaRef		  ) = 0;
	virtual void  		SetZEnable			( bool bEnable = true ) = 0;
	virtual void  		SetZWriteEnable		( bool bEnable = true ) = 0;
    virtual void  		SetDitherEnable		( bool bEnable = true ) = 0;
	virtual void  		SetWireframe		( bool bEnable = true ) = 0;

	virtual void		Dump				( const char* fname = 0 )	= 0;
	virtual bool  		ApplyStateBlock		( DWORD id ) = 0;
	virtual bool  		DeleteStateBlock	( DWORD id ) = 0;

	virtual int			GetNDisplayModes	() = 0;
	virtual void		GetDisplayMode		( int idx, int& width, int& height ) = 0;

	//=====================================================================
	//  sg dependency
	virtual void  		SetDirectionalLight	( sg::DirectionalLight*	pLight, int& index ) = 0;
	virtual void  		SetPointLight		( sg::PointLight*		pLight, int& index ) = 0;
	virtual void  		SetSpotLight		( sg::SpotLight*		pLight, int& index ) = 0;	
	
	virtual void  		SetRenderStateBlock	( sg::RenderStateBlock*	pBlock			   ) = 0;
	virtual void  		SetTextureStateBlock( sg::TextureStateBlock* pBlock, int stage ) = 0;
	virtual DWORD 		CreateStateBlock	( sg::StateBlock*		 pBlock			   ) = 0;
	virtual void  		SetMaterial			( sg::Material* pMaterial )				  = 0;
	virtual void  		SetFog				( sg::Fog*		pFog = NULL )			  = 0;
	//=====================================================================

	virtual DWORD 		GetCurFrame			() const = 0;
	virtual void  		DisableLights		() = 0;	
}; // class IRenderSystem

extern IRenderSystem* IRS;
const char* GetRootDirectory();

/*****************************************************************
/*  Class:  IDeviceClient
/*  Desc:   Notified when render system is destroyed, for 
/*				example, when screen resolution is being changed
/*****************************************************************/
class IDeviceClient
{
public:
	virtual void	OnDestroyRenderSystem	() = 0;
	virtual void	OnCreateRenderSystem	() = 0;

}; // class IDeviceClient


#endif // __RENDERSYSTEM_H__