/*****************************************************************************/
/*	File:	ICamera.h
/*	Desc:	Camera access interface
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-15-2003
/*****************************************************************************/
#ifndef __ICAMERA_H__
#define __ICAMERA_H__


/*****************************************************************************/
/*	Class:	ICamera
/*	Desc:	Interface for cameras manipulation
/*****************************************************************************/
class ICamera
{
public:

    //  current camera position
    virtual void		SetPos				( const Vector3D& pos ) = 0;
    virtual Vector3D	GetPos				() const = 0;
    virtual Vector3D	GetLookAt			() const = 0;

    //  current camera orientation
    virtual bool		SetDir				( const Vector3D& dir ) = 0;
    virtual Vector3D	GetDir				() const = 0;
    virtual void	    SetLookAt			( const Vector3D& v ) = 0;

    virtual bool		SetDirUp			( const Vector3D& dir,	const Vector3D& up ) = 0;


    virtual void		SetProjection		( float volW, float aspect, float zn, float zf ) = 0;
    virtual Matrix4D	GetWorldMatrix		() const = 0;
    virtual Matrix4D	GetViewProjM		() const = 0;

    //  applies z-bias
    virtual void		ShiftZ				( float amount ) = 0;

    //  near/far clip plane
    virtual float		GetZn				() const = 0;
    virtual float		GetZf				() const = 0;
    virtual void        SetFOV              ( float fov ) = 0;
    virtual float       GetFOV              () const = 0;

    //  set camera as current for render device
    virtual void		Render				() = 0;

    //  camera's view volume frustum
    virtual void		GetFrustum			( Frustum& fr ) const = 0;

    virtual Matrix4D    GetCameraTM         () const = 0;

    //  returns world space picking ray, corresponding to the mouse position in screen space
    virtual void		GetPickRay			( float curX, float curY, Line3D& ray, const Matrix4D* worldMatr = NULL ) = 0;

    //  linearly interpolates current camera lookAt/orientation
    virtual void		Interpolate			( float t, const Vector3D& sLookAt, const Vector3D& sDir,
        const Vector3D& dLookAt, const Vector3D& dDir ) = 0;
    //  linearly interpolates current camera lookAt only
    virtual void		Interpolate			( float t, const Vector3D& sPos, const Vector3D& dPos ) = 0;

    //  conversion utilities
    virtual void		WorldToScreenSpace	    ( Vector4D& pos, const Rct* pViewport = NULL ) const = 0;
    virtual void		WorldToScreenSpace	    ( Vector3D& pos ) const = 0;
    virtual void		WorldToProjectionSpace  ( Vector4D& pos ) const = 0;
    virtual void		WorldToCameraSpace      ( Vector4D& pos ) const = 0;
    virtual void		ProjectionToWorldSpace	( Vector4D& pos ) const = 0;
    virtual void        ScreenToWorldSpace      ( Vector4D& pos, const Rct* pViewport = NULL ) const = 0;
    virtual Matrix4D    ScreenToWorldSpace      ( const Rct* pViewport = NULL ) const = 0;
    virtual Matrix4D    WorldToScreenSpace      ( const Rct* pViewport = NULL ) const = 0;

}; // class ICamera

ICamera* GetCamera();

Matrix4D GetCameraTM();
Matrix4D GetCameraProjTM();

#endif // __ICAMERA_H__