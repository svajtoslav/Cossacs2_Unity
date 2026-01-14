/*****************************************************************
/*  File:   IShadowManager.h                                      
/*  Desc:   Interface to the shadow mapping manipulation
/*	Author:	Ruslan Shestopalyuk
/*  Date:   Jun 2004											 
/*****************************************************************/
#ifndef __ISHADOWMANAGER_H__ 
#define __ISHADOWMANAGER_H__

/*****************************************************************************/
/*  Enum:   ShadowQuality
/*  Desc:   Level of the shadows rendering quality
/*****************************************************************************/
enum ShadowQuality
{
    sqHigh      = 0,
    sqMedium    = 1,
    sqLow       = 2,
    sqNoShadows = 3,
    sqBlobs     = 4,

    sqUnknown   = 100,
}; // enum ShadowQuality

/*****************************************************************************/
/*	Class:	IShadowManager
/*	Desc:	Interface for mapping shadows
/*****************************************************************************/
class IShadowManager
{
public:
    //  initializes shadow manager
    virtual void                Init            () = 0;
    //  renders all shadows
    virtual void                Render          () = 0;
    //  clears all cached shadow information
    virtual void                ClearCache      () = 0;
    //  updates cached shadow information
    virtual void                UpdateCache     () = 0;
    //  adds new caster into system, returns internal shadow handle
    virtual bool                AddCaster       ( DWORD modelID, const Matrix4D& tm, bool bStatic = false ) = 0;
    virtual bool                AddCaster       ( DWORD modelID, DWORD anmID, float anmTime, const Matrix4D& tm, bool bStatic = false ) = 0;
    //  removes caster from system
    virtual void                RemoveCaster    ( int shadowID ) = 0;
    //  sets global shadow color
    virtual void                SetShadowColor  ( DWORD color ) = 0;
    //  setups current light direction (single directional light is supported right now)    
    virtual void                SetLightDir     ( const Vector3D& dir ) = 0;
    //  retrieves texture ID of the shadow map    
    virtual int                 GetShadowMapID  () const = 0;
    //  retrieves texture transform of the shadow map layer (for CameraSpacePosition texgen)
    virtual const Matrix4D&     CalcShadowMapTM () = 0;

    virtual void                Enable          ( bool bEnable = true ) = 0;
    virtual void                SetShadowMapSide( int w, int h = 0 ) = 0;
    virtual void                SetShadowQuality( ShadowQuality quality ) = 0;
    virtual ShadowQuality       GetShadowQuality() const = 0;

    virtual void                SetClipBias     ( float bias ) = 0;

}; // class IShadowManager

extern IShadowManager* IShadowMgr;

#endif // __ISHADOWMANAGER_H__ 