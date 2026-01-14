/*****************************************************************************/
/*	File:	vShadowManager.h
/*	Desc:	shadow manager interface implementation
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#ifndef __VSHADOWMANAGER_H__
#define __VSHADOWMANAGER_H__
#include "IShadowManager.h"

/*****************************************************************************/
/*  Class:  ShadowCaster
/*  Desc:   Describes single shadow caster
/*****************************************************************************/
struct ShadowCaster
{
    DWORD       mdlID;      // model id of the caster
    DWORD       anmID;      // animation id of the caster
    DWORD       anmTime;    // animation time
    Matrix4D    wTM;        // model world transform
    Matrix4D    shTM;       // shadow transform matrix 
    DWORD       frame;      // frame when caster was added to the shadow system
    DWORD       color;      // shadow color
}; // struct ShadowCaster

const float c_MinShadowBoxRatio = 0.5f;
/*****************************************************************************/
/*	Class:	ShadowManager
/*	Desc:	Implementation of the shadow manager
/*****************************************************************************/
class ShadowManager : public IShadowManager
{
    std::vector<ShadowCaster>   m_DynamicCasters;   //  array of the dynamic shadow casters
    std::vector<ShadowCaster>   m_StaticCasters;    //  array of the static shadow casters

    DWORD                       m_ShadowColor;      //  color of the currently rendered shadows
    int                         m_ShadowMapID;      //  id of the shadow map texture
    Matrix4D                    m_ShadowMapTM;      //  texture transform for the shadow map
    bool                        m_bInited;          //  whether manager is initialized
    int                         m_SMapWidth;        //  width of the shadowmap texture
    int                         m_SMapHeight;       //  height of the shadowmap texture

    bool                        m_bDrawDebugInfo;
    float                       m_ClipBias;         //  clip plane z-direction shift

    Matrix4D                    m_LightViewTM;      //  light view matrix
    Matrix4D                    m_LightProjTM;      //  light projection matrix
    
    Vector3D                    m_LightDir;         //  current light direction
    bool                        m_bEnabled;
    bool                        m_bNeedClearSMap;
    ShadowQuality               m_ShadowQuality;    //  current shadow rendering quality level

public:
                                ShadowManager   ();
    virtual void                Render          ();
    virtual void                Init            ();
    virtual void                ClearCache      ();
    virtual void                UpdateCache     ();
    virtual bool                AddCaster       ( DWORD modelID, const Matrix4D& tm, bool bStatic = false );
    virtual bool                AddCaster       ( DWORD modelID, DWORD anmID, float anmTime, const Matrix4D& tm, bool bStatic = false );  
    virtual void                RemoveCaster    ( int shadowID );
    virtual void                SetShadowColor  ( DWORD color );
    virtual void                SetLightDir     ( const Vector3D& dir );
    virtual int                 GetShadowMapID  () const { return m_ShadowMapID; }
    virtual const Matrix4D&     CalcShadowMapTM ();
    virtual void                Enable          ( bool bEnable = true ){ m_bEnabled = bEnable; }
    virtual void                SetShadowMapSide( int w, int h = 0 );
    virtual void                SetShadowQuality( ShadowQuality quality );
    virtual ShadowQuality       GetShadowQuality() const { return m_ShadowQuality; }
    virtual void                SetClipBias     ( float bias ) { m_ClipBias = bias; }

}; // class ShadowManager

#endif // __VSHADOWMANAGER_H__