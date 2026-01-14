/*****************************************************************************/
/*	File:	ITerrain.h
/*	Desc:	Interface for working with terrain
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-15-2003
/*****************************************************************************/
#ifndef __ITERRAIN_H__
#define __ITERRAIN_H__

class Vector3D;
class Rct;
class AABoundBox;
class ITerrainChunk;
class BaseMesh;

typedef bool		    (*TextureCallback)		( int texID, const Rct& mapExt );
typedef bool		    (*GeometryCallback)		( const Rct& mapExt, int lod );
typedef void		    (*AABBCallback)			( const Rct& mapExt, AABoundBox& aabb );

//  terrain callbacks
typedef int			    (*PVSCallback)			( DWORD* quadID, int maxPVSQuads );
typedef int			    (*GetHeightCallback)	( int, int );
typedef ITerrainChunk*  (*FactoryCallback)      ();

/*****************************************************************************/
/*	Class:	ITerrainChunk
/*	Desc:	Interface to single terrain piece
/*****************************************************************************/
class ITerrainChunk
{
public:
    virtual             ~ITerrainChunk(){}
    
}; // class ITerrainChunk

/*****************************************************************************/
/*	Class:	ITerrain
/*	Desc:	Interface for terrain manipulation
/*****************************************************************************/
class ITerrain
{
public:
	//  renders terain
	virtual void		Render				()							= 0;
	
	//  callback for getting height
	virtual void		SetCallback			( GetHeightCallback	callb ) = 0;
	//  callback for setting height
	virtual void		SetCallback			( SetHeightCallback	callb ) = 0;
	//  callback for creating quad texture
	virtual void		SetCallback			( TextureCallback	callb ) = 0;
	//  callback for creating quad geometry
	virtual void		SetCallback			( GeometryCallback	callb ) = 0;
	//  callback for calculating quad bounding box
	virtual void		SetCallback			( AABBCallback		callb ) = 0;
	//  callback for calculating potentially visible set
	virtual void		SetCallback			( PVSCallback		callb ) = 0;

    virtual void		SetCallback			( FactoryCallback   callb ) = 0;

	virtual void		InvalidateTexture	( const Rct* rct = NULL )	= 0;
	virtual void		InvalidateGeometry	( const Rct* rct = NULL )	= 0;
	virtual void		InvalidateAABB		( const Rct* rct = NULL )	= 0;

	virtual void		EnableVertexLighting( bool val = true )			= 0;
    virtual void        ClearPVS            ()                          = 0;

	
	//  forces to use quads of given LOD only
	virtual void		ForceLOD			( int lod )					= 0;
	
	//  sets extents of the whole terrain
	virtual void		SetExtents			( const Rct& ext )			= 0;
	virtual void		SetHeightmapPow		( int hpow )				= 0;
	virtual void		SetLODBias			( float bias )				= 0;
	
	virtual Vector3D	GetNormal			( float x, float y ) const	= 0;
	virtual float		GetH				( float x, float y ) const	= 0;
	virtual void		SetH				( int x, int y, float z )	= 0;

	virtual void		Show				( bool bShow = true )		= 0;
	virtual bool		IsShown				() const					= 0;

    virtual void        DrawBorder          ()                          = 0;
    virtual bool        SetBorderConfigFile ( const char* fname )       = 0;
    virtual void        Init                ()                          = 0;
    virtual void	    SetDrawCulling		( bool bValue = true )      = 0;
    virtual void	    SetDrawGeomCache    ( bool bValue = true )      = 0;
    virtual void	    SetDrawTexCache     ( bool bValue = true )      = 0;
    virtual void        ResetDrawQueue      ()                          = 0;
    virtual BaseMesh*   AllocateGeometry    ()                          = 0;

    virtual void        ShowNormals         ( bool bShow = true )       = 0;
    virtual Rct         GetExtents          () const                    = 0;
    virtual void        SetGeometryCacheSize( int nGeom )               = 0;

	//  casts ray to the heightmap
	virtual bool		Pick				( const Vector3D& orig, const Vector3D& dir, Vector3D& pt ) = 0;
	virtual bool		Pick				( int mX, int mY, Vector3D& pt ) = 0;
	
}; // class ITerrain

extern ITerrain*			ITerra;
#endif // __ITERRAIN_H__