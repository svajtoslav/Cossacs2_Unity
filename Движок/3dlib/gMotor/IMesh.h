/*****************************************************************
/*  File:   IMesh.h                                      
/*  Desc:   Interface to the mesh manipulation
/*	Author:	Ruslan Shestopalyuk
/*  Date:   Jun 2004											 
/*****************************************************************/
#ifndef __IMESH_H__ 
#define __IMESH_H__  

//  maximal possible number of texture coordinates in the mesh vertex
const int c_MaxTextureCoordinates   = 4;  
const int c_MaxBlendWeights         = 4;

/*****************************************************************/
/*	Enum:	VertCompUsage
/*	Desc:	Describes meaning of the vertex component value
/*	Remark:	Values are ordered by priority, as they are in the 
/*				actual vertex layout
/*****************************************************************/
enum VertCompUsage
{
	vcUnknown		= 0x0,

	vcPosition		= 0x1,	    //  vertex position	

	vcBlend0		= 0x2,	    //	blending weights
	vcBlend1		= 0x4,
	vcBlend2		= 0x8,
	vcBlend3		= 0x10,

	vcBlendIndices	= 0x20,	    //	blending indices (usually packed into DWORD value)		

	vcNormal		= 0x40,	    //  normal at the vertex 
	vcDiffuse		= 0x80,	    //	diffuse color component 
	vcSpecular		= 0x100,    //  specular color component 
	
	vcTexCoor0		= 0x200,    //  texture coordinates
	vcTexCoor1		= 0x400,
	vcTexCoor2		= 0x800,
	vcTexCoor3		= 0x1000,
	
	vcBinormal		= 0x2000,	//  binormal (oY axis)
	vcTangent		= 0x4000,	//	tangent  (oX axis)

    vcCustom        = 0x8000,  //  something defined by user

}; // enum VertCompUsage

/*****************************************************************/
/*	Enum:	VertCompType
/*	Desc:	Describes type of the vertex component value
/*****************************************************************/
enum VertCompType
{
	ctUnknown		= 0,
	ctFloat1		= 1,
	ctFloat2		= 2,
	ctFloat3		= 3,
	ctFloat4		= 4,
	ctColor			= 5,
	ctShort2		= 6,
	ctShort4		= 7,
	ctUByte4		= 8
}; // enum VertCompType 

/*****************************************************************/
/*	Enum:	VertElement
/*	Desc:	Describes single element of the vertex stream layout
/*****************************************************************/
class VertElement
{
public:
	int					m_Offset;	//	offset from the vertex data begin, in bytes
	VertCompUsage		m_Usage;	//  element meaning
	VertCompType		m_Type;		//	element data type

}; // class VertElement

const int c_MaxVertDeclElements = 16;
/*****************************************************************/
/*	Enum:	VertDeclaration
/*	Desc:	Declaration of the vertex stream data mapping
/*****************************************************************/
class VertDeclaration
{
public:
	VertElement			m_Element[c_MaxVertDeclElements];
	int					m_NElements;

}; // class VertDeclaration

/*****************************************************************/
/*	Struct:	Submesh
/*	Desc:	Portion of the mesh, rendered with different attributes
/*****************************************************************/
struct Submesh
{
	int			        m_ID;			//  id of the submesh within parent mesh
	int			        m_ShID;			//  shader id (local into parent mesh)
	int			        m_BoneID;		//	id of the first bone in parent bone list, this submesh is attached to
    int                 m_NumBones;     //  number of bones, this submesh is attached to

	int			        m_FirstPoly;	//	first polygon in the parent mesh
	int			        m_NumPoly;		//	number of polygons
	int			        m_FirstVert;	//	first vertex in parent mesh
	int			        m_NumVert;		//	number of mesh vertices

}; // struct Submesh

/*****************************************************************/
/*	Struct:	Bone
/*	Desc:	Node in the mesh transform hierarchy
/*****************************************************************/
struct Bone
{
    int			        m_ID;		    //  id of the bone within parent mesh bone list
    std::string         m_Name;         //  name of the bone
    
    Matrix4D            m_Transform;    //  bone transform matrix
    Matrix4D            m_Offset;       //  bone offset matrix

    DWORD               m_Flags;        //  bone status flags
    int                 m_NumChildren;  //  number of children 

    Bone() {}
	Bone( const char* name, const Matrix4D& tm ) : m_NumChildren(0) 
	{ 
		m_Transform	= tm; 
		m_Name		= name; 
		m_Flags		= 0;
	}
}; // struct Bone

/*****************************************************************/
/*	Struct:	UVCoord
/*****************************************************************/
struct UVCoord
{
    float u, v;
    UVCoord( float _u, float _v ) : u(_u), v(_v) {}
}; // struct UVCoord

class IAnimInstance;
class IMeshInstance;
class IShaderInstance;

/*****************************************************************/
/*	Class:	IMesh
/*	Desc:	Interface for managing mesh data
/*****************************************************************/
class IMesh
{
public:
    virtual const char*         GetName         () const = 0;

    //  submeshes manipulation
	virtual int					GetNSubmeshes	() const = 0;
	virtual const Submesh*		GetSubmesh		( int idx )	const = 0;
    //  returns index of the submesh which contains vertex with given index
    virtual int					GetVertexSubmesh( int vertIdx ) const = 0;
    //  returns index of the submesh which contains polygon with given index
    virtual int					GetPolySubmesh  ( int polyIdx ) const = 0;

    //  shaders manipulation
	virtual int					GetNShaders		() const = 0;

    //  geometry data manipulation
	virtual int					GetNFaces		() const = 0;
	virtual int					GetNVerts		() const = 0;

    //  level-of-detail manipulation
    virtual int                 GetNLODs        () const = 0;

    //  returns metric for given level-of-detail 
    //  it is squared z distance for which this lod is applied
    virtual float               GetLODMetric    ( int lodID ) const = 0;

    //  caching stamp
    virtual DWORD               GetCacheStamp   () const = 0;

    //  retrieving bounding volumes
    virtual const AABoundBox&   GetAABoundBox   () const = 0;
    virtual const Sphere&       GetBoundSphere  () const = 0;

    //  bones manipulation
    //	Bones are used not only for skinned meshes, but also they are
    //	transform nodes to which rigid objects are attached
    virtual int					GetNBones		() const = 0;
    virtual Matrix4D		    GetBoneTM		( int idx ) const = 0;
    virtual const char*			GetBoneName		( int idx ) const = 0;

    virtual void				SetBoneTM		( int idx, const Matrix4D& tm ) = 0;
    virtual void				SetBoneName		( int idx, const char* name ) = 0;

    virtual void                Flatten         () = 0;
	virtual void                Render			() = 0;

    //  creates instance of the mesh
    virtual IMeshInstance*      CreateInstance  () = 0;
    
    //  dumps mesh contents to XML file
    virtual bool                SaveToXML       ( const char* fname ) = 0;
}; // class IMesh

/*****************************************************************/
/*	Class:	IMeshInstance
/*	Desc:	Interface for managing unique mesh instances
/*****************************************************************/
class IMeshInstance
{
public:
    //  retrieves unique mesh instance handle
    virtual DWORD               GetHandle       () = 0;
    
    //  retrieves interface to the core mesh
    virtual IMesh*              GetMesh         () = 0;
    
    //  sets/gets current topmost mesh's world transform
    virtual void                SetTransform    ( const Matrix4D& tm ) = 0; 
    virtual const Matrix4D&     GetTransform    () const = 0; 
    
    //  retrieving current bounding volumes
    virtual const AABoundBox&   GetAABoundBox   () const = 0;
    virtual const Sphere&       GetBoundSphere  () const = 0;
    
    //  current bone states
    virtual int					GetNBones		() const = 0;
    virtual const Matrix4D&		GetBoneTM		( int idx ) const = 0;
    virtual void				SetBoneTM		( int idx, const Matrix4D& tm ) = 0;

    //  current LOD 
    virtual int                 GetLOD          () const = 0;
    virtual bool                SetLOD          ( int lod ) = 0;

    //  active animations
    virtual int                 GetNAnimations  () const = 0;
    virtual IAnimInstance*      GetAnimInstance ( int idx ) = 0;

    //  returns mesh instance to its original state
    virtual void                ResetTransforms () = 0;

    //  compositing meshes
    virtual IMeshInstance*      GetParentInst   () = 0;
    virtual int                 GetParentBoneID () const = 0;
    virtual int                 GetNumChildren  () const = 0;
    virtual IMeshInstance*      GetChildInst    () = 0;
    
};  // class IMeshInstance

/*****************************************************************/
/*	Class:	IMeshFactory
/*	Desc:	Interface for buiding meshes
/*****************************************************************/
class IMeshFactory
{
public:
    virtual bool        CreateMesh  ( IMesh* imesh ) = 0;
    virtual IMesh*      CreateMesh  () = 0;    
    virtual void        Reset       () = 0;
    virtual int         AddPos      ( const Vector3D& v ) = 0;
    virtual int         AddNormal   ( const Vector3D& v ) = 0;
    virtual int         AddTangent  ( const Vector3D& v ) = 0;
    virtual int         AddBinormal ( const Vector3D& v ) = 0;
    virtual int         AddUV       ( float u, float v, int set = 0 ) = 0;
    virtual int         AddDiffuse  ( DWORD clr ) = 0;
    virtual int         AddSpecular ( DWORD clr ) = 0;
    virtual int         AddBlendW   ( float weight, int wIdx ) = 0;
    virtual int         AddBlendI   ( WORD idx, int wIdx ) = 0;
    virtual int         AddBone     ( const char* name, const Matrix4D& tm ) = 0; 
    virtual int         AddTriangle ( WORD v0, WORD v1, WORD v2 ) = 0;
    virtual bool        SanityOK    () = 0;
}; // class IMeshFactory

IMesh*     CreateFromXML( InStream& is );


#endif // __IMESH_H__ 