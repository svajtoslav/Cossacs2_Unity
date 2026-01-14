/*****************************************************************************/
/*	File:	sgNodePool.h
/*	Desc:	Scene graph node manager
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGNODEPOOL_H__
#define __SGNODEPOOL_H__

namespace sg{

class Node;
const int c_MaxNodesInPool = 65536;
/*****************************************************************************/
/*	Class:	NodePool
/*	Desc:	Scenegraph node manager
/*****************************************************************************/
class NodePool : public Singleton<NodePool>
{
	Node*					m_Nodes[c_MaxNodesInPool];	// nodes array
	int						m_NumNodes;					// current number of nodes (with empty entries)
	int						m_FirstFree;				// index of the first free entry
	static DWORD			s_CurStamp;

public:
							NodePool();
	virtual					~NodePool();

	template <class NodeType> static NodeType* CreateNode()
	{
		void* buf = malloc( sizeof( NodeType ) );
		NodeType* pNode = (NodeType*)buf;
		pNode->SetID( instance()._AddNode( pNode ) );
		return pNode;
	}

	template <class NodeType> static NodeType* GetNode( DWORD id )
	{
		Node* pNode = (NodeType*)instance()._GetNode( id );
		if (!pNode || !pNode->IsA<NodeType>() || !EqualStamps( id, pNode->GetID() )) return NULL;
		return (NodeType*)pNode;
	}

	static Node* GetNode( DWORD id )
	{
		return instance()._GetNode( id );
	}

	template <class NodeType> 
	static NodeType* GetNodeByName( const char* nodeName, DWORD firstID = 0 )
	{
		for (int i = firstID; i < instance().m_NumNodes; i ++)
		{
			Node* pNode = instance().m_Nodes[i];
			if (pNode && pNode->IsA<NodeType>() && HasName( pNode, nodeName ))
			{
				return (NodeType*)pNode;
			}
 		}
		return NULL;
	} // GetNodeByName

	static Node* GetNodeByName( const char* nodeName, DWORD firstID = 0 )
	{
		for (int i = firstID; i < instance().m_NumNodes; i ++)
		{
			Node* pNode = instance().m_Nodes[i];
			if (HasName( pNode, nodeName ))
			{
				return pNode;
			}
		}
		return NULL;
	} // GetNodeByName

	static bool DestroyNode( Node* pNode );

	void Dump();

protected:
	static bool HasName( Node* pNode, const char* name );
	static bool	EqualStamps( DWORD s1, DWORD s2 ) 
	{ 
		return (s1 & 0xFFFF0000) == (s2 & 0xFFFF0000); 
	}

	DWORD _AddNode( Node* pNode )
	{
		DWORD idx = 0;
		if (m_FirstFree < m_NumNodes)
		{
			while (m_Nodes[m_FirstFree] != NULL && m_FirstFree < m_NumNodes) m_FirstFree++;
		}
		if (m_Nodes[m_FirstFree] == NULL && m_FirstFree < m_NumNodes)
		{
			idx = m_FirstFree;	
			m_FirstFree++;
		}
		else
		{
			idx = m_NumNodes;
			m_FirstFree++;
			m_NumNodes++;
		}
		s_CurStamp += 0x00010000;
		m_Nodes[idx] = pNode;
		return idx | s_CurStamp;
	}

	void _ClearEntry( DWORD id )
	{
		DWORD idx = id & 0x0000FFFF;
		m_Nodes[idx] = NULL;
		if (m_FirstFree > idx) m_FirstFree = idx;
	}


	Node* _GetNode( DWORD id );

}; // class NodePool

}; // namespace sg 
#endif // __SGNODE_H__