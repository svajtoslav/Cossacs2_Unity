/*****************************************************************************/
/*	File:	vMesh.cpp
/*	Desc:	Mesh interface implementation
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#include "stdafx.h"
#include "IMesh.h"
#include "vMesh.h"

/*****************************************************************************/
/*  Mesh implementation
/*****************************************************************************/
Mesh::Mesh()
{
    m_Bones         = NULL;
    m_NBones        = 0;
    m_Submesh       = NULL;
    m_NSubmeshes    = 0;
    m_NVerts        = 0;
    m_Position      = NULL;
    m_Normal        = NULL;
    m_Tangent       = NULL;
    m_Binormal      = NULL;
    m_Diffuse       = NULL;
    m_Specular      = NULL;

    for (int i = 0; i < c_MaxTextureCoordinates; i++) m_UV[i] = NULL;
    for (int i = 0; i < c_MaxBlendWeights; i++) 
    {
        m_BlendIndex[i] = NULL;
        m_BlendWeight[i] = NULL;
    }

    m_NIndices      = 0;
    m_Idx           = NULL;  
    m_NLODs         = 0;
    m_CacheStamp    = 0;
    m_AABB          = AABoundBox::null;
    m_BSphere       = Sphere::null;

    m_Name          = NULL;
} // Mesh::Mesh

void Mesh::Reset()
{
    delete []m_Bones;
    m_Bones         = NULL;
    m_NBones        = 0;

    delete []m_Submesh;
    m_Submesh       = NULL;
    m_NSubmeshes    = 0;
    m_NVerts        = 0;

    delete []m_Position;
    delete []m_Normal;
    delete []m_Tangent;
    delete []m_Binormal;
    delete []m_Diffuse;
    delete []m_Specular;

    m_Position      = NULL;
    m_Normal        = NULL;
    m_Tangent       = NULL;
    m_Binormal      = NULL;
    m_Diffuse       = NULL;
    m_Specular      = NULL;

    for (int i = 0; i < c_MaxTextureCoordinates; i++) m_UV[i] = NULL;
    for (int i = 0; i < c_MaxBlendWeights; i++) 
    {
        delete [](m_BlendIndex[i]);
        delete [](m_BlendWeight[i]);
        m_BlendIndex[i] = NULL;
        m_BlendWeight[i] = NULL;
    }

    m_NIndices      = 0;
    delete []m_Idx;
    m_Idx           = NULL;  
    m_NLODs         = 0;

    m_CacheStamp    = 0;
    m_AABB          = AABoundBox::null;
    m_BSphere       = Sphere::null;
    m_Name          = NULL;
} // Mesh::Reset

bool Mesh::SaveToXML( const char* fname )
{
    XMLNode root;
    FOutStream os( fname );
    if (os.NoFile()) return false;
    root.SetTag( "Mesh" );    
    if (m_Name) root.AddAttr( "Name", m_Name );
    
	XMLNode* pBones = root.AddChild( "Bones" );
	pBones->AddAttr( "Number", m_NBones );
	for (int i = 0; i < m_NBones; i++)
	{
		const Bone& bone = m_Bones[i];
		XMLNode* pBone = pBones->AddChild( "Bone" );
		pBone->AddAttr( "Name", bone.m_Name					);
        Vector3D pos( bone.m_Transform.getTranslation() );
		pBone->AddAttr( "Position", pos	);
		pBone->AddAttr( "NumChildren", bone.m_NumChildren	);
	}
	
	int bufSize = 8 * m_NIndices;
	char* buf = new char[bufSize]; buf[0] = 0;
	for (int i = 0; i < m_NFaces; i++)
	{
		int len = strlen( buf );
		if (len + 30 >= bufSize) 
		{
			assert( false );
			break;
		}
		if (i % 4 == 0) {sprintf( buf + len, "\n\t\t\t" ); len += 4; }
		sprintf( buf + len, "%d %d %d  \t", m_Idx[i*3], m_Idx[i*3 + 1], m_Idx[i*3 + 2] );
	}
	sprintf( buf + strlen( buf ), "\n\t\t" );
	
	XMLNode* pFaces = root.AddChild( "Faces" );
	pFaces->AddAttr( "Number", m_NFaces );
	pFaces->SetValue( (const char*)buf );
	delete []buf;

	bufSize = 16 * m_NVerts * 3;
	buf = new char[bufSize]; buf[0] = 0;
	for (int i = 0; i < m_NVerts; i++)
	{
		int len = strlen( buf );
		if (len + 64 >= bufSize) 
		{
			assert( false );
			break;
		}
		const Vector3D& pos = m_Position[i];
		sprintf( buf + len, "\n\t\t\t%f \t%f \t%f \t", pos.x, pos.y, pos.z );
	}
	sprintf( buf + strlen( buf ), "\n\t\t" );
	
	XMLNode* pPos = root.AddChild( "Positions" );
	pPos->AddAttr( "Number", m_NVerts );
	pPos->SetValue( (const char*)buf );

	if (m_Normal)
	{
		buf[0] = 0;
		for (int i = 0; i < m_NVerts; i++)
		{
			int len = strlen( buf );
			if (len + 64 >= bufSize) 
			{
				assert( false );
				break;
			}
			const Vector3D& pos = m_Normal[i];
			sprintf( buf + len, "\n\t\t\t%f \t%f \t%f \t", pos.x, pos.y, pos.z );
		}
		sprintf( buf + strlen( buf ), "\n\t\t" );

		XMLNode* pN = root.AddChild( "Normals" );
		pN->AddAttr( "Number", m_NVerts );
		pN->SetValue( (const char*)buf );
	}

	delete []buf;

    root.Write( os );
    return true;
} // Mesh::SaveToXML

int Mesh::GetVertexSubmesh( int vertIdx ) const
{
    return 0;
}

int Mesh::GetPolySubmesh  ( int polyIdx ) const
{
    return 0;
}

void Mesh::Flatten()
{

}

void Mesh::Render()
{

}

/*****************************************************************************/
/*  MeshFactory implementation
/*****************************************************************************/
MeshFactory::MeshFactory()
{
}

MeshFactory::~MeshFactory()
{
}

void MeshFactory::Reset()
{
    m_Bones.clear();
    m_Position.clear();
    m_Normal.clear();
    m_Tangent.clear();
    m_Binormal.clear();
    for (int i = 0; i < c_MaxTextureCoordinates; i++) m_UV[i].clear();
    for (int i = 0; i < c_MaxBlendWeights; i++) 
    { 
        m_BlendIndex[i].clear();
        m_BlendWeight[i].clear();
    }
    m_Diffuse.clear();
    m_Specular.clear();
} // MeshFactory::Reset

bool MeshFactory::CreateMesh( IMesh* imesh )
{
    Mesh* mesh = (Mesh*)imesh;
    if (!mesh) return false;
    
    mesh->Reset();
    mesh->m_NBones      = m_Bones.size();
    mesh->m_NVerts      = m_Position.size();
    mesh->m_NIndices    = m_Idx.size();
	mesh->m_NFaces		= m_Idx.size()/3;


    delete []mesh->m_Position;
    mesh->m_Position = new Vector3D[ mesh->m_NVerts];
    memcpy( mesh->m_Position, &m_Position[0], mesh->m_NVerts*sizeof(Vector3D) );

	delete []mesh->m_Normal; mesh->m_Normal = NULL;
	if (m_Normal.size() > 0)
	{
		mesh->m_Normal = new Vector3D[mesh->m_NVerts];
		memcpy( mesh->m_Normal, &m_Normal[0], mesh->m_NVerts*sizeof(Vector3D) );
	}

	delete []mesh->m_Idx;
	mesh->m_Idx = new DWORD[mesh->m_NIndices];
	memcpy( mesh->m_Idx, &m_Idx[0], m_Idx.size()*sizeof(DWORD) );

	delete []mesh->m_Bones;
	mesh->m_Bones = new Bone[mesh->m_NBones];
    for (int i = 0; i < mesh->m_NBones; i++) mesh->m_Bones[i] = m_Bones[i];
    return true;
} // MeshFactory::CreateMesh

IMesh* MeshFactory::CreateMesh()
{
    Mesh* mesh = new Mesh();
    if (!CreateMesh( mesh )) return NULL;    
    return mesh;
} // MeshFactory::CreateMesh

bool MeshFactory::SanityOK()
{
    return false;
} // MeshFactory::SanityOK



