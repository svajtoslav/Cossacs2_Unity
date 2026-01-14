/*****************************************************************************/
/*	File:	sgNodePool.cpp
/*	Desc:	Scene graph node manager
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNode.h"
#include "sgNodePool.h"

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	NodePool implementation
/*****************************************************************************/
DWORD NodePool::s_CurStamp		= 0x00000000;

NodePool::NodePool()
{
	m_NumNodes  = 0;
	m_FirstFree = 0;
}

NodePool::~NodePool()
{
	for (int i = 0; i < m_NumNodes; i++) 
	{
//		delete m_Nodes[i];
//		m_Nodes[i] = NULL;
	}
} // NodePool::~NodePool

void NodePool::Dump()
{
	FILE* fp = fopen( "c:\\dumps\\nodepool.txt", "wt" );
	if (!fp) return;
	
	fprintf( fp, "NumNodes:%d FirstFree:%d\n", m_NumNodes, m_FirstFree );

	for (int i = 0; i < m_NumNodes; i++)
	{
		Node* pNode = m_Nodes[i];
		if (!pNode)
		{
			fprintf( fp, "-DEAD-\n" );
			continue;
		}
		fprintf( fp, "%d. <%s> ID: %X NumRef: %d\n", 
			i, pNode->GetName(), pNode->GetID(), pNode->GetNRef() );
	}

	fclose( fp );
} // NodePool::Dump

bool NodePool::DestroyNode( Node* pNode )
{
	if (!pNode) return false;
	DWORD id = pNode->GetID();
	if (instance()._GetNode( id ) != pNode)
	{
		Log.Error( "Node Pool is corrupt!!!" );
	}

	pNode->RemoveChildren(); 
	if (!pNode->IsImmortal()) 
	{
		instance()._ClearEntry( id );
		delete pNode; 
	}

	return true;
} // NodePool::DestroyNode

//  put it to the museum of hackery, when it opens
bool NodePool::HasName( Node* pNode, const char* name )
{
	if (!pNode) return false;
	return pNode->HasName( name );
}

Node* NodePool::_GetNode( DWORD id )
{
	Node* pNode = m_Nodes[id & 0x0000FFFF];
	if (!pNode || !EqualStamps( id, pNode->GetID() )) return NULL;
	return pNode;
} // NodePool::GetNode

END_NAMESPACE( sg )
