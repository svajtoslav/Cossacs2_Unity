/*****************************************************************************/
/*	File:	rsRenderSystem.h
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	23.01.2003
/*****************************************************************************/
#ifndef __RSRENDERSYSTEM_H__
#define __RSRENDERSYSTEM_H__
#pragma once

#include <vector>
/*****************************************************************************/
/*	Enum:	MemoryPool
/*	Desc:	Memory location of resource
/*****************************************************************************/
enum MemoryPool
{
	mpUnknown		= 0,
	mpSysMem		= 1,
	mpVRAM			= 2,
	mpManaged		= 3
};  // enum MemoryPool

/*****************************************************************************/
/*	Enum:	BufferFormat
/*	Desc:	Type of used resource buffer	
/*****************************************************************************/
enum BufferFormat
{
	bfUnknown		= 0,
	bfStatic		= 1,
	bfDynamic		= 2
};	//  enum BufferFormat

/*****************************************************************************/
/*	Enum:	TextureUsage	
/*****************************************************************************/
enum TextureUsage
{
	tuUnknown		= 0,
	tuLoadable		= 1,
	tuProcedural	= 2,
	tuRenderTarget  = 3,
	tuDynamic		= 4,
	tuDepthStencil	= 5
};	// enum TextureUsage

/*****************************************************************************/
/*	Enum:	DeviceCapability
/*****************************************************************************/
enum DeviceCapability
{
	dcUnknown						= 0,
	dcIndexedVertexBlending			= 1,
	dcBumpEnvMap					= 2,
	dcBumpEnvMapLuminance			= 3,
	dcDynamicTextures				= 4,
	dcTnL							= 5,
	dcPure							= 6,
	dcRasterization					= 7,
	dcBezier						= 8,
	dcRTPatches						= 9,
	dcNPatches						= 10,
	dcREF							= 11
};	// enum DeviceCapability

/*****************************************************************************/
/*	Enum: DepthStencilFormat
/*****************************************************************************/
enum DepthStencilFormat
{
	dsfUnknown			= 0,
	dsfD16Lockable		= 1,
	dsfD32				= 2,
	dsfD15S1			= 3,
	dsfD24S8			= 4,
	dsfD16				= 5
}; // DepthStencilFormat

/*****************************************************************************/
/*	Enum: ScreenResolution
/*****************************************************************************/
enum ScreenResolution
{
	srUnknown			= 0,
	sr640x480			= 1,
	sr800x600			= 2,
	sr1024x768			= 3,
	sr1280x1024			= 4,
	sr1600x1200			= 5
}; // ScreenResolution

/*****************************************************************************/
/*	Enum: ScreenBitDepth
/*****************************************************************************/
enum ScreenBitDepth
{
	bdUnknown			= 0,
	bd16				= 1,
	bd32				= 2
}; // enum ScreenBitDepth

/*****************************************************************************/
/*	Class:	TextureDescr
/*	Desc:	Texture description class
/*****************************************************************************/
class TextureDescr
{
public:
	TextureDescr();
	
	bool equal( const TextureDescr& td );
	bool less( const TextureDescr& td );
	void copy( const TextureDescr& orig );
	
	void			setValues( int sx, int sy, 
								ColorFormat		cf		= cfARGB4444,
								MemoryPool		mp		= mpVRAM, 
								int				nmips	= 1,
								TextureUsage	tu		= tuLoadable );

	int				getSideX	()	const { return sideX;					}
	int				getSideY	()	const { return sideY;					}
	ColorFormat		getColFmt	()	const { return (ColorFormat)colFmt;		}
	DepthStencilFormat getDsFmt	()	const { return (DepthStencilFormat)dsFormat; }

	MemoryPool		getMemPool	()	const { return (MemoryPool)memPool;		}
	TextureUsage	getTexUsage	()	const { return (TextureUsage)texUsage;	}
	int				getNMips	()		const { return numMips;					}
	const char*		getPoolStr	()	const;
	const char*		getColFmtStr()	const;
	const char*		getUsageStr	()	const;
	int				getID		()			const { return id; }

	void			setSideX	( int sx			)	{ sideX		= sx;		}
	void			setSideY	( int sy			)	{ sideY		= sy;		}
	void			setColFmt	( ColorFormat cf	)	{ colFmt	= (BYTE)cf; }
	void			setDsFmt	( DepthStencilFormat dsf	)	{ dsFormat	= (BYTE)dsf; }
	void			setMemPool	( MemoryPool mp		)	{ memPool	= (BYTE)mp;	}
	void			setTexUsage	( TextureUsage tu	)	{ texUsage	= (BYTE)tu; }
	void			setNMips	( int n				)	{ numMips	= n;		}
	void			setID		( int _id			)	{ id		= _id;		}
	
	void			invalidate	() { valid = false; }

protected:
	int				sideX;		//  width
	int				sideY;		//  height

	BYTE			colFmt;		//  color format
	BYTE			memPool;	//  memory pool
	BYTE			texUsage;	//  usage 
	BYTE			dsFormat;	//  depth stencil format if texture is depth buffer
	
	int				numMips;	//  number of mip levels

	//  runtime properties
	int				id;
	bool			valid;
	DWORD			reserved;
}; // class TextureDescr

/*****************************************************************************/
/*	Class:	ScreenProp
/*	Desc:	Describes screen properties
/*****************************************************************************/
class ScreenProp
{
public:
	ScreenProp();

	int				m_Width;
	int				m_Height;

	ColorFormat		m_ColorFormat;
	bool			m_bFullScreen;
	bool			m_bCoverDesktop;
	int				m_RefreshRate;

	bool equal( const ScreenProp& prop ) const;

}; // class ScreenProp

/*****************************************************************************/
/*	Class:	DeviceDescr
/*	Desc:	3D device capabilities
/*****************************************************************************/
class DeviceDescr
{
public:
	DeviceDescr();

	int				adapterOrdinal;
	std::string		devDriver;
	std::string		devDescr;
	
	int				texBlendStages;
	int				texInSinglePass;

	std::vector<DeviceCapability>		capBits;
	std::vector<DepthStencilFormat>	depthStencil;
	std::vector<ColorFormat>			renderTarget;	
}; // class DeviceDescr

//  primitive drawing helper utilities
class Rct;
class Vector3D;

void rsLine			( float x1, float y1, float x2, float y2, float z, DWORD color );
void rsLine			( float x1, float y1, float x2, float y2, float z, DWORD color1, DWORD color2 );
void rsFrame		( const Rct& rct, float z, DWORD color );

void rsFlushLines2D	( bool bShaded = true );
void rsFlushLines3D	( bool bShaded = true );
void rsFlushPoly2D	( bool bShaded = true );
void rsFlushPoly3D	( bool bShaded = true );
void rsFlush        ( bool bShaded = true );

void rsLine			( const Vector3D& a, const Vector3D& b, DWORD color );
void rsLine			( const Vector3D& a, const Vector3D& b, DWORD color1, DWORD color2 );
void rsEnableZ		( bool enable = true );

void rsRestoreShader();
void rsSetShader	( int shader );
void rsSetTexture	( int texID, int stage = 0 );

void rsRect			( const Rct& rct, float z, DWORD color );
void rsRect			( const Rct& rct, float z, DWORD ca, DWORD cb, DWORD cc, DWORD cd );
void rsRect			( const Rct& rct, const Rct& uv, float z, DWORD color );
void rsRect			( const Rct& rct, const Rct& uv, float z, DWORD ca, DWORD cb, DWORD cc, DWORD cd );

void rsPanel		(  const Rct& rct, float z, 
						DWORD clrTop = 0xFFFFFFFF, 
						DWORD clrMdl = 0xFFD6D3CE, 
						DWORD clrBot = 0xFF848284 );

void rsPoly			( const Vector3D& a, const Vector3D& b, const Vector3D& c, 
						DWORD acol, DWORD bcol, DWORD ccol );
void rsPoly			( const Vector3D& a, const Vector3D& b, const Vector3D& c, 
						float au, float av, float bu, float bv, float cu, float cv,
						DWORD acol, DWORD bcol, DWORD ccol );
void rsPoly			( const Vector3D& a, const Vector3D& b, const Vector3D& c, 
					 float au, float av, float bu, float bv, float cu, float cv,
					 float au2, float av2, float bu2, float bv2, float cu2, float cv2,
					 DWORD acol, DWORD bcol, DWORD ccol );

void rsPoly			( const Vector3D& a, const Vector3D& b, const Vector3D& c, DWORD color );
void rsQuad			( const Vector3D& a, const Vector3D& b, const Vector3D& c, const Vector3D& d, 
					 DWORD acol, DWORD bcol, DWORD ccol, DWORD dcol );

void rsQuad			( const Vector3D& a, const Vector3D& b, const Vector3D& c, const Vector3D& d, 
						const Rct& uv, DWORD acol, DWORD bcol, DWORD ccol, DWORD dcol );
void rsQuad			( const Vector3D& a, const Vector3D& b, const Vector3D& c, const Vector3D& d, 
					  const Rct& uv, DWORD col );
void rsQuad			( const Vector3D& a, const Vector3D& b, const Vector3D& c, const Vector3D& d, 
					  const Rct& uv, const Rct& uv2, DWORD col );

void rsQuad			( const Vector3D& a, const Vector3D& b, const Vector3D& c, const Vector3D& d, DWORD color );


//  Drawing different useful primitives
const int		c_DefaultSphereSegments = 8;
const float		c_DefRayLen				= 10000.0f; 
const float		c_DefHandleSide			= 5.0f;
const float		c_DefQuadSide			= 30.0f;
const float		c_DefNormalLen			= 8.0f;

class Matrix4D;

class Frustum;
class Plane;
class Line3D;
class Triangle;
class Sphere;
class Cylinder;
class Capsule;
class Cone;
class AABoundBox;
class BaseMesh;

void CreateArrow	( BaseMesh& bm, const Vector3D& start, const Vector3D& dir, DWORD color, float len = 1.0f );
void DrawArrow		( const Vector3D& start, const Vector3D& dir, DWORD color, 
                        float len = 1.0f, float head = 0.1f, float headR = 0.05f );

void DrawFrustum	( const Frustum& frustum, 
					 DWORD fillColor, DWORD linesColor, 
					 bool drawNormals = false );
void DrawCube		( const Vector3D& center, DWORD color, float side = c_DefHandleSide );
void DrawRay		( const Line3D& ray, DWORD linesColor, 
					 float rayLen = c_DefRayLen, 
					 float handleSide = c_DefHandleSide );

void DrawPlane		( const Plane& plane, DWORD fillColor, DWORD color,
					 const Vector3D* center = NULL,
					 float qSide = c_DefQuadSide, 
					 float nLen = c_DefNormalLen );

void DrawTriangle	( const Triangle& tri, DWORD linesColor, DWORD fillColor );
void DrawAABB		( const AABoundBox& aabb, DWORD fillColor, DWORD linesColor );
void DrawSphere		( const Sphere& sphere, 
					 DWORD fillColor, DWORD linesColor, 
					 int nSegments = c_DefaultSphereSegments );
void DrawCircle		( const Vector3D& center, const Vector3D& normal, float radius, 
					 DWORD fillColor, DWORD linesColor, int nSegments );
void DrawCircle		( float x, float y, float radius, DWORD fillColor, DWORD linesColor, int nSegments );
void DrawCircle8	( const Vector3D& center, const Vector3D& normal, float radius, 
					 DWORD fillColor, DWORD linesColor );

void DrawSpherePatch( const Sphere& sphere, 
					 DWORD fillColor, DWORD linesColor, 
					 float phiBeg, float phiEnd,
					 float thetaBeg, float thetaEnd,
					 int nSegments, const Matrix4D* pTM = NULL );
void DrawStar		( const Sphere& sphere, DWORD begColor, DWORD endColor, 
					 int nSegments = c_DefaultSphereSegments );

void DrawAnchor     ( const Vector3D& pos, float side, DWORD color, bool bBothSides = true );
void DrawAnchor     ( const Vector3D& pos, float side, DWORD cx, DWORD cy, DWORD cz, bool bBothSides = true );

void DrawCylinder	( const Cylinder& cylinder, 
					 DWORD fillColor, DWORD linesColor, bool bCapped,
					 int nSegments = c_DefaultSphereSegments );
void DrawCapsule	( const Capsule& capsule, 
					 DWORD fillColor, DWORD linesColor, 
					 int nSegments = c_DefaultSphereSegments );

void DrawCone		( const Cone& cone, 
					 DWORD fillColor, DWORD linesColor, 
					 int nSegments = c_DefaultSphereSegments );

void DrawFatSegment( const Vector3D& beg, const Vector3D& end, const Vector3D& normal, 
						float width, bool bRoundEnds, DWORD color, DWORD coreColor = 0 );

void DrawPoint      ( const Vector3D& pt, DWORD clr = 0xFFFF0000, float rad = 5.0f );

bool DrawText       ( int x, int y, DWORD color, const char* format,  ... );
bool DrawText       ( const Vector3D& pos, DWORD color, const char* format, ... );
void FlushText      ();

void DrawFaces      ( BaseMesh& bm, bool bDrawNumbers = true );
void DrawVertices   ( BaseMesh& bm, bool bDrawNumbers = true, bool bDrawHandles = true );
void DrawRawMesh    (   std::vector<Vector3D>& vert, 
                        std::vector<int>& ind, const Matrix4D& tm );

void DrawCurve( float width, int nPoints, const Vector3D* points, const DWORD* colors );


#endif // __RSRENDERSYSTEM_H__