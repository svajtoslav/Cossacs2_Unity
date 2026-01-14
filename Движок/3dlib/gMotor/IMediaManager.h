/*****************************************************************************/
/*	File:	IMediaManager.h
/*	Desc:	Interface for the straightforward model management
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-15-2003
/*****************************************************************************/
#ifndef __IMEDIAMANAGER_H__
#define __IMEDIAMANAGER_H__

class Vector3D;
class Vector4D;
class Matrix3D;
class Matrix4D;
class Frustum;
class Line3D;
class Rct;
class AABoundBox;
class Primitive;
class VertexIterator;

#include "ICamera.h"

typedef void		(*SetHeightCallback)	( int, int, float );
typedef void		(*VisitHeightCallback)	( int, int );

/*****************************************************************************/
/*	Class:	ILight
/*	Desc:	Interface for lights manipulation
/*****************************************************************************/
class ILight
{
public:
	virtual void		Render		() = 0;

	virtual Vector3D	GetPos		() const = 0;
	virtual Vector3D	GetDir		() const = 0;

	virtual DWORD		GetAmbient	() const = 0;
	virtual DWORD		GetDiffuse	() const = 0;
	virtual DWORD		GetSpecular	() const = 0;
	virtual float		GetRange	() const = 0;

	virtual DWORD		GetIndex	()	const = 0;
	virtual void		SetIndex	( DWORD index ) = 0;
	virtual void		SetPos		( const Vector3D& pos ) = 0;
	virtual void		SetDir		( const Vector3D& dir ) = 0;
	virtual void		SetDiffuse	( DWORD diffuse	 ) = 0;
	virtual void		SetAmbient	( DWORD ambient	 ) = 0;
	virtual void		SetSpecular	( DWORD specular ) = 0;
	virtual void		SetRange	( float range	 ) = 0;

}; // class ILight

/*****************************************************************************/
/*	Class:	IGeometry
/*	Desc:	Interface for geometry manipulation
/*****************************************************************************/
class IGeometry
{
public:
	//  renders geometry
	virtual void			Render				() = 0;
	
	//  returns stride, in bytes, of the vertex in the vertex array
	virtual int				GetVertexStride		() const = 0;

	//  returns stride, in bytes, of the index in the index array
	virtual int				GetIndexStride		() const = 0;

	//  number of vertices
	virtual int				GetNumVertices		() const = 0;
	
	//  number of indices
	virtual int				GetNumIndices		() const = 0;
	virtual WORD*			GetIndices			() = 0;

	//  returns center of mass of the geometry
	virtual Vector3D		GetCenter			() const = 0;

	//  provides access to the vertices
	virtual void			GetVertexIterator	( VertexIterator& it ) = 0;

	//  ray picking, returns also intersection point
	virtual int				Pick				( const Vector3D& org, const Vector3D& dir, Vector3D& pt ) = 0;

}; // class IGeometry

/*****************************************************************************/
/*	Class:	IReflectionMap
/*	Desc:	Interface for manipulation of models, animations, cursors
/*				and other stuff. Plays role of the facade to the sg entities.
/*****************************************************************************/
class IReflectionMap
{
public:

	virtual void		AddObject				( DWORD id, const Matrix4D* objTM ) = 0;
	virtual void		CleanObjects			() = 0;
	virtual int			GetReflectionTextureID	() const = 0;
	virtual void		Render					() = 0;
	///virtual void		SetViewPort				( float x, float y, float w, float h ) = 0;
	virtual Matrix4D	GetReflectionTexTM		() const = 0;


}; // class IReflectionMap

/*****************************************************************************/
/*	Class:	IParticleSystem
/*	Desc:	Interface for particle system manipulation
/*****************************************************************************/
class IParticleSystem
{
public:
	virtual void	SetEmitRateMultiplier	( float val ) = 0;
	virtual void	SetAlphaMultiplier		( float val ) = 0;
	virtual void	SetBirthAlphaMultiplier	( float val ) = 0;
	virtual void	SetTTLMultiplier		( float val ) = 0;
}; // class IParticleSystem

/*****************************************************************************/
/*	Class:	IParticleManager
/*	Desc:	Interface for scene particles management
/*****************************************************************************/
class IParticleManager
{
public:
	virtual void		Render			() = 0;
	virtual void		EnableBatching	( bool bEnable = true ) = 0;
	virtual void		AddQuad			( const Vector3D& pos, const Vector3D& rot,
											float scale, DWORD color, 
											const Rct& uv, int texID ) = 0;
	virtual void		AddBillboard	( const Vector3D& pos, float dir, float scale, 
											DWORD color, const Rct& uv, int texID ) = 0;
}; // IParticleManager


struct WSegment;
/*****************************************************************************/
/*	Class:	IMediaManager
/*	Desc:	Interface for manipulation of models, animations, cursors
/*				and other stuff. Plays role of the facade to the sg entities.
/*****************************************************************************/
class IMediaManager
{
public:
	//  returns handle of the model with given file name
	virtual DWORD		GetModelID	( const char* fname ) = 0;
	virtual const char* GetModelFileName( DWORD modelID ) = 0;

	//  returns handle to the child node of the given model
	virtual DWORD		GetNodeID	( DWORD modelID, const char* nodeName ) = 0;
	//  returns handle to node in the global node hierarchy
	virtual DWORD		GetNodeID	( const char* name ) = 0;
	virtual void		ShowNode	( DWORD id, bool bShow = true ) = 0;
	virtual void		SetVisible	( DWORD nodeID, bool bVisible = true ) = 0;
	
	virtual DWORD		CloneNode	( DWORD nodeID ) = 0;
	virtual void		DeleteNode	( DWORD nodeID ) = 0;

	//  returns current node transform
	virtual Matrix4D	        GetNodeTransform ( DWORD nodeID, bool bLocalSpace = false ) = 0;	
	
	virtual ICamera*			GetCamera		 ( DWORD nodeID ) = 0;
	virtual ILight*				GetLight		 ( DWORD nodeID ) = 0;
	virtual IParticleSystem*	GetParticleSystem( DWORD nodeID ) = 0;
	virtual IGeometry*			GetGeometry		 ( DWORD nodeID ) = 0;

	virtual int					GetNumGeometries ( DWORD modelID ) = 0;
	virtual IGeometry*			GetGeometry		 ( DWORD modelID, int idx, Matrix4D& tm ) = 0;

	virtual int					GetNumSubNodes	( DWORD modelID ) = 0;
	virtual const char*			GetSubNodeName	( DWORD modelID, int idx ) = 0;

	virtual void				Init			() = 0;
	virtual void				OnFrame			() = 0;
	
	//  sets current node transform 
	virtual void	SetNodeTransform( DWORD nodeID, const Matrix4D& m, bool bLocalSpace = false ) = 0;
	//  makes node parentID parent/non-parent to the node childID
	virtual bool	MakeParent      ( DWORD parentID, DWORD childID, bool bParent = true ) = 0;
	//  renders models with given handle/world transform matrix
	//		If pTransform is NULL, model will be rendered with the current
	//		world transform
	virtual void	Render			( DWORD id, const Matrix4D* pTransform = NULL ) = 0;
	virtual void	Render			() = 0;

	//  renders model on the terrain, corresponding to the screen space point (mX,mY)
	virtual void	Render			( DWORD id, int mX, int mY, float scale = 1.0f ) = 0;

	//  rendering into the thumbnail window
	virtual void	BeginThumbnail	( const Rct& rect, const Vector3D* viewDir = NULL, 
									  DWORD bgColor = 0, bool mouseControlled = true ) = 0;
	virtual void	EndThumbnail	() = 0;

	//  sets animation(animID) of the model (modelID) at time animTime
	virtual void	Animate         ( DWORD modelID, DWORD animID, float animTime ) = 0;

	//  sets blended animation of the model (modelID) 
	virtual void	Animate         ( DWORD modelID, float blendFactor,
								        DWORD animID1, float animTime1,
								        DWORD animID2, float animTime2 ) = 0;

	//  returns total time of the given animation, in ms
	virtual float	GetAnimTime		( DWORD animID ) = 0;
	virtual void	SetCamera		( const Vector3D& lookAt, float viewVolWidth ) = 0;

	//  draws shadow for the model with given handle/world transform
	virtual void	RenderShadow	( DWORD id, const Matrix4D* pTransform = NULL ) = 0;
	//  returns sprite package/frame index, associated with the model
	virtual bool	GetModelGP		( DWORD id, int& gpID, int& frameID, Vector3D& center ) = 0;
	
    //  returns model bounding box
    virtual AABoundBox GetBoundBox	( DWORD id ) = 0;

	//  finds maximum z value of the model's mesh at the point (pt.x, pt.y), result
	//		is placed into the pt.z, returns false if no such point 
	virtual bool	GetHeight		( DWORD id, Vector3D& pt, const Matrix4D* pTransform = NULL ) = 0; 
	
	//  finds passability of the mesh at the point
	//  returns true when the point is locked
	virtual bool	GetLock			( DWORD id, const Vector3D& pt, const Matrix4D* pTransform = NULL ) = 0; 
	
	//  sets current switch state
	virtual bool	SwitchTo		( DWORD id, int index ) = 0;
	virtual void	ReloadModels	() = 0;

	//  occlusion utilities
	virtual bool	IsBoxVisible	( const Vector3D& minv, const Vector3D& maxv ) = 0;
	virtual bool	IsSphereVisible	( const Vector3D& center, float radius ) = 0;
	virtual bool	IsPointVisible	( const Vector3D& pt ) = 0;
	
	//  world objects space manipulation
	//  registering static model
	virtual void	RegisterVisible ( DWORD objID, DWORD modelID, const Matrix4D& tm ) = 0;
	//  registering box
	virtual void	RegisterVisible ( DWORD objID, const Vector3D& minv, const Vector3D& maxv	) = 0;
	//  registering sphere
	virtual void	RegisterVisible ( DWORD objID, const Vector3D& center, float radius ) = 0;
	//  registering sprite
	virtual void	RegisterVisible ( DWORD objID, DWORD gpID, int frameID, const Matrix4D& tm ) = 0;
	
	//  removes all previously registered visible objects
	virtual void	ClearVisibleCache() = 0;

	//  picking with mouse
	virtual DWORD	PickVisible	    ( float scrX, float scrY, DWORD prevID = 0xFFFFFFFF ) = 0;
	virtual void	ScanHeightmap   ( DWORD nodeID, const Matrix4D& tm, float stepx, float stepy, SetHeightCallback callb ) = 0;
	virtual void	ScanHeightmap   ( DWORD nodeID, const Matrix4D& tm, float stepx, float stepy, VisitHeightCallback callb ) = 0;
	
	virtual int		GetSegmentList  ( DWORD nodeID, WSegment* segArr, int maxSeg, const Matrix4D* tm = NULL ) = 0;
	virtual void	FreezeShaders   ( bool freeze = true ) = 0;
    virtual bool    Intersects      ( float mX, float mY, DWORD modelID, const Matrix4D& tm, float& minDist ) = 0;
}; // class IMediaManager

//  use this 
extern IMediaManager*		IMM;
extern IReflectionMap*		IRMap;
extern IParticleManager*	IPMgr;

#include <vector>
bool LoadRawModel( const char* fname, std::vector<Vector3D>& vert, std::vector<int>& idx );

#endif // __IMEDIAMANAGER_H__