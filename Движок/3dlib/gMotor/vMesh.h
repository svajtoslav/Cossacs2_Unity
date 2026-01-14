/*****************************************************************************/
/*	File:	vMesh.h
/*	Desc:	Mesh interface implementation
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#ifndef __VMESH_H__
#define __VMESH_H__

/*****************************************************************************/
/*  Class:  MeshInstance
/*  Desc:   mesh instance implementation
/*****************************************************************************/
class MeshInstance : public IMeshInstance
{

public:
    virtual DWORD               GetHandle       () { return 0; }
    virtual IMesh*              GetMesh         () { return NULL; }
    virtual void                SetTransform    ( const Matrix4D& tm ) {}
    virtual const Matrix4D&     GetTransform    () const { return Matrix4D::identity; }
    virtual const Sphere&       GetBoundSphere  () const { return Sphere::null; }
    virtual const AABoundBox&   GetAABoundBox   () const { return AABoundBox::null; }
    virtual int					GetNBones		() const { return 0; }
    virtual const Matrix4D&		GetBoneTM		( int idx ) const { return Matrix4D::identity; }
    virtual void				SetBoneTM		( int idx, const Matrix4D& tm ) {}
    virtual int                 GetLOD          () const { return 0; }
    virtual bool                SetLOD          ( int lod ) { return false; }
    virtual int                 GetNAnimations  () const { return 0; }
    virtual IAnimInstance*      GetAnimInstance ( int idx ) { return NULL; }
    virtual void                ResetTransforms () {}
    virtual IMeshInstance*      GetParentInst   () { return NULL; }
    virtual int                 GetParentBoneID () const { return 0; }
    virtual int                 GetNumChildren  () const { return 0; }
    virtual IMeshInstance*      GetChildInst    () { return NULL; }

};  // class MeshInstance

/*****************************************************************************/
/*  Class:  Mesh
/*  Desc:   mesh interface implementation
/*****************************************************************************/
class Mesh : public IMesh
{
protected:
    char*                       m_Name;         

    //  bone data
    Bone*                       m_Bones;
    Matrix4D*                   m_SkinMatrix;   //  array of bone matrices ready for skinning

    int                         m_NBones;
    
    //  submesh data
    Submesh*                    m_Submesh;
    int                         m_NSubmeshes;
    
    //  vertex data
    int                         m_NVerts;
    //  mesh vertex components. If some component is not present, corresponding pointer is NULL
    Vector3D*                   m_Position;
    
    Vector3D*                   m_Normal;
    Vector3D*                   m_Tangent;
    Vector3D*                   m_Binormal;

    DWORD*                      m_Diffuse;
    DWORD*                      m_Specular;

    UVCoord*                    m_UV            [c_MaxTextureCoordinates];
    WORD*                       m_BlendIndex    [c_MaxBlendWeights];
    float*                      m_BlendWeight   [c_MaxBlendWeights];

    //  index data
    int                         m_NIndices;
    int                         m_NFaces; 
    DWORD*                      m_Idx;

    //  lod data
    int                         m_NLODs;

    //  hardware buffer caching stamp
    DWORD                       m_CacheStamp;

    //  static bounding volumes
    AABoundBox                  m_AABB;
    Sphere                      m_BSphere;

    friend class                MeshFactory;

public:
                                Mesh            ();
    virtual const char*         GetName         () const { return m_Name; }
    virtual int					GetNSubmeshes	() const { return m_NSubmeshes; }
    virtual const Submesh*		GetSubmesh		( int idx )	const { return &m_Submesh[idx]; }
    virtual int					GetNShaders		() const { return 0; }
    virtual int					GetNFaces		() const { return m_NFaces; }
    virtual int					GetNVerts		() const { return m_NVerts; }
    virtual int                 GetNLODs        () const { return m_NLODs; }
    virtual float               GetLODMetric    ( int lodID ) const { return 0; }
    virtual DWORD               GetCacheStamp   () const { return m_CacheStamp; }
    virtual const AABoundBox&   GetAABoundBox   () const { return m_AABB; }
    virtual const Sphere&       GetBoundSphere  () const { return m_BSphere; }
    virtual int					GetNBones		() const { return m_NBones; }
    virtual Matrix4D		    GetBoneTM		( int idx ) const 
    {   assert( m_Bones && idx > 0 && idx < m_NBones ); 
        return m_Bones[idx].m_Transform; 
    }
    
    virtual const char*			GetBoneName		( int idx ) const 
    { assert( m_Bones && idx > 0 && idx < m_NBones ); return m_Bones[idx].m_Name.c_str(); }

    virtual void				SetBoneTM		( int idx, const Matrix4D& tm ) {}
    virtual void				SetBoneName		( int idx, const char* name ) {}
    virtual IMeshInstance*      CreateInstance  () { return NULL; }
    virtual bool                SaveToXML       ( const char* fname );
    virtual void                Reset           ();

    //  resets all submeshes to make their transforms identity
    virtual void                Flatten         ();
	virtual void                Render			();

    virtual int					GetVertexSubmesh( int vertIdx ) const;
    virtual int					GetPolySubmesh  ( int polyIdx ) const;
}; // class Mesh

const int c_MaxMeshBones = 1024;
/*****************************************************************************/
/*  Class:  MeshFactory
/*  Desc:   Mesh creator interface implementation
/*****************************************************************************/
class MeshFactory : public IMeshFactory
{
    std::vector<Bone>       m_Bones;
    std::vector<Vector3D>   m_Position;
    std::vector<Vector3D>   m_Normal;
    std::vector<Vector3D>   m_Tangent;
    std::vector<Vector3D>   m_Binormal;
    std::vector<UVCoord>    m_UV            [c_MaxTextureCoordinates];
    std::vector<WORD>       m_BlendIndex    [c_MaxBlendWeights];
    std::vector<float>      m_BlendWeight   [c_MaxBlendWeights];
    std::vector<DWORD>      m_Diffuse;
    std::vector<DWORD>      m_Specular;
    std::vector<DWORD>      m_Idx;

public:
                MeshFactory ();
                ~MeshFactory();

    bool        CreateMesh  ( IMesh* imesh );
    IMesh*      CreateMesh  ();    
    void        Reset       ();

	int			GetNVerts	() const { return m_Position.size(); }
    int         AddPos      ( const Vector3D& v ) { m_Position.push_back( v ); return m_Position.size() - 1; }
    int         AddNormal   ( const Vector3D& v ) { m_Normal.push_back( v ); return m_Normal.size() - 1; }
    int         AddTangent  ( const Vector3D& v ) { m_Tangent.push_back( v ); return m_Tangent.size() - 1; }
    int         AddBinormal ( const Vector3D& v ) { m_Binormal.push_back( v ); return m_Binormal.size() - 1; }
    int         AddUV       ( float u, float v, int set = 0 ) { m_UV[set].push_back( UVCoord( u, v ) ); return m_UV[set].size(); }
    int         AddDiffuse  ( DWORD clr ) { m_Diffuse.push_back( clr ); return m_Diffuse.size() - 1; }
    int         AddSpecular ( DWORD clr ) { m_Specular.push_back( clr ); return m_Specular.size() - 1; }
    int         AddBlendW   ( float weight, int wIdx ) 
    { 
        m_BlendWeight[wIdx].push_back( weight );
        return m_BlendWeight[wIdx].size() - 1;
    }

    int         AddBlendI   ( WORD idx, int wIdx )
    { 
        m_BlendIndex[wIdx].push_back( idx );
        return m_BlendIndex[wIdx].size() - 1;
    }

    int         AddBone     ( const char* name, const Matrix4D& tm ) 
    { 
        m_Bones.push_back( Bone( name, tm ) ); 
        return m_Bones.size() - 1; 
    }
    int         AddTriangle ( WORD v0, WORD v1, WORD v2 )
    {
        m_Idx.push_back( v0 );
        m_Idx.push_back( v1 );
        m_Idx.push_back( v2 );
        return (m_Idx.size() - 3)/3;
    }

    bool        SanityOK    ();

}; // class MeshFactory

#endif // __VMESH_H__