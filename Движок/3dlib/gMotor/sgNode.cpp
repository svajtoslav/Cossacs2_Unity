/*****************************************************************************/
/*	File:	sgNode.cpp
/*	Desc:	Scene graph node
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "kIOHelpers.h"
#include "kPropertyMap.h"

#ifndef _INLINES 
#include "sgNode.inl"
#endif // _INLINES

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Node implementation
/*****************************************************************************/
Node::NodePtrMap		Node::s_NodeMap;
Node::NodeIdMap			Node::s_NodeIdMap;
Node::NodePtrList		Node::s_NodeList;
char					Node::NameFilter::m_Name[c_MaxNodeNameLen];

void Node::Serialize( OutStream& os, DWORD nBytes ) const
{
	DWORD magic	= GetMagic();
	os << magic << nBytes;
	Serialize( os );
}

void Node::Serialize( OutStream& os ) const
{
	DWORD nChildren = GetNChildren();
	DWORD parentID	= c_BadID;

	NodePtrMap::iterator it;
	it = s_NodeMap.find( GetParent() );
	if (it != s_NodeMap.end() && GetParent() != NULL)
	{
		parentID = (*it).second;
	}

	os << m_Name << m_Flags << parentID << nChildren;
	
	DWORD nodeID;
	for (int i = 0; i < m_Children.size(); i++)
	{
		it = s_NodeMap.find( GetChild( i ) );
		if (it == s_NodeMap.end() || GetChild( i ) == NULL)
		{
			os << c_BadID;
		}
		else
		{
			nodeID = (*it).second;
			os << nodeID;
		}
	}
} // Node::Serialize

void Node::Unserialize( InStream& is )
{
	DWORD nodeID, nChildren;
	DWORD parentID;
	DWORD blockSize = 0;
	is >> blockSize >> m_Name >> m_Flags >> parentID >> nChildren;

	m_pParent = reinterpret_cast<Node*>( parentID );

	int nNodes = nChildren;
	for (int i = 0; i < nNodes; i++)
	{
		is >> nodeID;
		m_Children.push_back( reinterpret_cast<Node*>( nodeID ) );
	}
} // Node::Unserialize

Node*	Node::CreateFromFile( const char* fileName )
{
	FInStream is( fileName );
	if (is.NoFile()) return NULL;
	Node* pNode = Node::UnserializeSubtree( is );
	return pNode;
} // Node::CreateFromFile

bool Node::WriteToFile( const char* fileName ) 
{
	FOutStream os( fileName );
	if (os.NoFile()) return false;
	SerializeSubtree( os );
	return true;
} // Node::WriteToFile

void Node::Expose( PropertyMap& pm )
{
	pm.start( "Node", this );

	pm.section( "Base" );
	pm.p( "Name",			GetName, SetName	);
	pm.p( "Class",			GetClassName		);
	pm.p( "ChildrenNum",	GetNChildren		);
	//pm.p( "TotalChildren",	CountNChildrenTotal	);
	//pm.p( "ID",				GetID				);
	//pm.p( "NumRef",			GetNRef				);
	pm.p( "Invisible",		IsInvisible, SetInvisible	);
	pm.p( "Disabled",		IsDisabled,	 SetDisabled	);
	pm.p( "DrawGizmo",		DoDrawGizmo, SetDrawGizmo	);
	pm.p( "DrawAABB",		DoDrawAABB,  SetDrawAABB	);
} // Node::Expose

void Node::PreSerialize() const
{
	if (s_NodeMap.find( (Node*)this ) == s_NodeMap.end()) 
	{
		s_NodeMap[(Node*)this] = s_NodeList.size();
		s_NodeList.push_back( (Node*)this );
	}

	for (int i = 0; i < GetNChildren(); i++)
	{
		if (Owns( GetChild( i ) )) GetChild( i )->PreSerialize();
	}
} // Node::PreSerialize

bool Node::Destroy()
{
	assert( false );
	return false;
} // Node::Delete

Node* Node::PostUnserialize( int nodeIdx )
{
	if (nodeIdx == c_BadID || 
		nodeIdx < 0 || 
		nodeIdx >= s_NodeList.size())
	{
		return NULL;
	}
	assert( nodeIdx >= 0 && nodeIdx < s_NodeList.size() );
	Node* pNode = s_NodeList[nodeIdx];
	return pNode;
} // Node::PostUnserialize

void Node::PostUnserialize()
{
	m_pParent = Node::PostUnserialize( reinterpret_cast<int>( m_pParent ) );
	for (int i = 0; i < m_Children.size(); i++)
	{
		m_Children[i] = Node::PostUnserialize( reinterpret_cast<int>( m_Children[i] ) );
		if (m_Children[i] == NULL) continue;
		m_Children[i]->AddRef();
	}
	for (int i = 0; i < m_Children.size(); i++)
	{
		if (m_Children[i] == NULL) m_Children.erase( m_Children.begin() + i );
	}
	OnChangeChildren();
} // Node::PostUnserialize

Node* Node::UnserializeSubtree( InStream& is )
{
	if (!is) return NULL;
    int nNodes = 1;

	char chMagic[5]; chMagic[4] = 0;
	try{
		DWORD magic, lastMagic = 0;
		
		s_NodeList.clear();

		//  fetch root node
		is >> magic;
		Node* root = NodeFactory::instance().CreateNode( magic );
		if (!root) return NULL;

		root->Unserialize( is );
		s_NodeList.push_back( root );
		
		//  fetch all other nodes in file
		while (is)
		{
			lastMagic = magic;
			is >> magic;
			Node* cNode = NodeFactory::instance().CreateNode( magic );
			if (!cNode)
			{
				*((DWORD*)chMagic) = lastMagic;
				Log.Warning( "Could not read node from input stream. Last node read: <%s>", 
							&chMagic );
				return NULL;
			}

			assert( cNode );
			cNode->Unserialize( is );
			s_NodeList.push_back( cNode );
			nNodes++;
		}

		//  post process pointers to nodes
		for (int i = 0; i < s_NodeList.size(); i++)
		{
			Node* cNode = s_NodeList[i];
			cNode->PostUnserialize();
		}

		s_NodeList.clear();
		return root;
	}
	catch (...)
	{
		Log.Error( "Could not load model. Error in node:<%s>", &chMagic );
		s_NodeList.clear();
		return NULL;
	}
}// Node::UnserializeSubtree

bool Node::SerializeSubtree( OutStream& os ) const
{
	s_NodeMap.clear();
	s_NodeList.clear();

	PreSerialize();

	int nNodes = s_NodeList.size();
	for (int i = 0; i < nNodes; i++)
	{
		Node* pNode = s_NodeList[i];
		CountStream cs;
		pNode->Serialize( cs );
		int nBytes = cs.GetNBytes();
		pNode->Serialize( os, nBytes );
	}

	s_NodeMap.clear();
	s_NodeList.clear();

	return true;
} // Node::Serialize

Node* Node::Clone() const
{
	CountStream cs;
	SerializeSubtree( cs );
	int nBytes = cs.GetNBytes();
	MemOutStream os( nBytes );
	SerializeSubtree( os );
	MemInStream is;
	Node* newNode = UnserializeSubtree( is );
	if (!newNode) return NodeFactory::instance().CreateNode( GetMagic() );
	//newNode->AdjustClonedName( GetName() );
	return newNode;
} // Node::Clone

void Node::AdjustClonedName( const char* name )
{
	const char* pos = &name[strlen( name ) - 1];
	while (isdigit( *pos )) pos--;
	int idx = 0;
	int nDig = sscanf( pos + 1, "%d", &idx );
	if (nDig == 0) idx = 0; else idx++;
	char buf[64];
	std::string adjName;
	do{
		sprintf( buf, "%02d", idx );
        adjName = std::string( name, pos - name + 1 );
		adjName += buf;
		idx++;
	} while (NodePool::GetNodeByName( adjName.c_str() ));
	m_Name = adjName;
} // Node::AdjustClonedName

bool Node::IsEqual( const Node* node ) const
{
	return	node->HasName( m_Name.c_str() ) && (node->m_Flags == m_Flags);
}

DWORD Node::Release()
{
	m_RefCount--;
	assert( m_RefCount >= 0 );
	if (m_RefCount == 0) 
	{
		NodePool::instance().DestroyNode( this );
		return 0;
	}
	return m_RefCount;
} // Node::Release

int	Node::CountNChildrenTotal() const
{
	Iterator it( (Node*)this ); ++it; 
	int nCh = 0;
	while (it) 
	{
		if (it.GetParent() && !it.GetParent()->Owns( it )) it.Up();
		nCh++;
		++it; 
	}
	return nCh;
}

const char*	Node::GetSymbolicPath( Node* pRoot )
{
	static char path[c_MaxNodePathLen];
	path[0] = 0;

	Iterator it( pRoot );
	while (it)
	{
		if ((Node*)it == this)
		{
			for (int i = 0; i < it.GetDepth(); i++)
			{
				Node* pParent = it.GetParent( i );
				if (!pParent) continue;
				strcat( path, pParent->GetName() );
				strcat( path, "." );
			}
			path[strlen( path ) - 1] = 0;
		}
		++it;
	}

	return path;
} // Node::GetSymbolicPath

//-------------------------------------------------------------------------------
//  Func:  Node::AttachSubtree
//  Desc:  Sets correspondent nodes from pRoot with the same names as inputs to
//			the original subtree
//  Parm:  pRoot - subtree root
//  Ret:   true if merged ok
//  Rmrk:  Roots are always attached, even if they have different names
//-------------------------------------------------------------------------------
bool Node::AttachSubtree( Node* pRoot )
{
	if (HasInput( pRoot )) return true;

	AddInput( pRoot );

	Iterator it( this );
	while (it)
	{
		Node* pNode = it;
		while (it.GetParent() && (!it.GetParent()->Owns( pNode )))
		{
			it.Up();
			++it;
			pNode = it;
		}
		if (!pNode) break;

		NameFilter filt( pNode->GetName() );
		Iterator inpIt( pRoot, filt );
		if (inpIt)
		{
			pNode->AddInput( (Node*)inpIt );
		}
		++it;
	}

	return true;
} // Node::AttachSubtree

Node* Node::CreateSubtree( XMLNode* pRoot )
{
	s_NodeIdMap.clear();
	Node* pNode = CreateFromXML( pRoot );
	if (!pNode) return NULL;
	Iterator it( pNode );
	pNode->FixInputs();
	return pNode;
} // Node::CreateSubtreeFromXML

void Node::FixInputs()
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		
	}
} // Node::CreateSubtreeFromXML

Node* Node::CreateFromXML( XMLNode* pRoot )
{
	Node* pNode = NodeFactory::instance().CreateNodeByClassName( pRoot->GetTag() );
	pNode->FromXML( pRoot );
	
	DWORD id = 0;
	if (!pRoot->GetAttr( "id", id ))
	{
		Log.Error( "Node should have <id> attribute!" );
	}
	const char* name;
	if (!pRoot->GetAttr( "name", name ))
	{
		Log.Error( "Node should have <name> attribute!" );
	}
	pNode->SetName( name );

	XMLNode* pChild = pRoot->FirstChild();
	while (pChild)
	{
		if (pChild->GetAttr( "id", id ))
		{
			const char* ref = NULL;
			if (pChild->GetAttr( "ref", ref ))
			{
				s_NodeIdMap[id] = PNodePair( pNode, NULL );
			}
			else 
			{
				Node* pChNode = CreateFromXML( pChild );
				s_NodeIdMap[id] = PNodePair( pNode, pChNode );
			}
			pNode->m_Children.push_back( reinterpret_cast<Node*>( id ) );
		}
		pChild = pChild->NextSibling();
	}
	
	return pNode;
} // Node::CreateFromXML

bool Node::FromXML( XMLNode* pRoot )
{
    return false;
} // Node::FromXML

const int c_XMLBufSize = 1024;
XMLNode* Node::ToXML()
{
	XMLNode* pNode = new XMLNode();
	pNode->SetTag( GetClassName() );

	pNode->AddAttr( "name", GetName() );
	pNode->AddAttr( "id", GetID() );

	int nCh = GetNChildren();
	if (nCh > 0)
	{
		for (int i = 0; i < nCh; i++)
		{
			XMLNode* pChild = NULL;
			if (Owns(GetChild( i ))) pChild = GetChild( i )->ToXML();
			else
			{
				pChild = new XMLNode();
				pChild->SetTag( GetChild( i )->GetClassName() );
				const char* input = "in";
				pChild->AddAttr( "ref", input );
				pChild->AddAttr( "name", GetChild( i )->GetName() );
				pChild->AddAttr( "id", GetChild( i )->GetID() );
			}
			pNode->AddChild( pChild );
		}
	}
	return pNode;
} // Node::ToXML

Node* Node::FindChildByName( const char* nodeName )
{
	Iterator it( this );
	while (it)
	{
		Node* pNode = (Node*)it;
		if (pNode->HasName( nodeName )) return pNode;	
		++it;
		pNode = (Node*)it;
		if (!it.GetParent()->Owns( pNode )) it.Up();
	}
	return NULL;
} // FindChild

Node* Node::FindChildByNameCI( const char* nodeName )
{
	Iterator it( this );
	while (it)
	{
		Node* pNode = (Node*)it;
		if (pNode->HasNameCI( nodeName )) return pNode;	
		++it;
		pNode = (Node*)it;
		if (!it.GetParent()->Owns( pNode )) it.Up();
	}
	return NULL;
} // FindChild

bool Node::ReplaceChild( Node* pChild, Node* pNewChild )
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (GetChild( i ) == pChild)
		{
			m_Children[i] = pNewChild;
			pNewChild->AddRef();
			if (!Owns( pChild )) pChild->SetParent( NULL );
			pChild->Release();
			return true;
		}
	}
	return false;
} // ReplaceChild

bool Node::Filter( const sg::Node* pNode )	
{																	
	return pNode->GetMagic() == *((DWORD*)"NODE");					
}	

bool Node::FilterChildren( const sg::Node* pNode )						
{																	
	return (!pNode->GetParent() || pNode->GetParent()->Owns( pNode ));					
}	

bool Node::HasFn( const char* magic ) const 
{ 
	return ((*((DWORD*)magic) == *((DWORD*)"NODE"))); 
}

bool Node::HasFn( DWORD magic ) const 
{ 
	return (magic == *((DWORD*)"NODE")); 
}

Node* Node::CreateInstance() 
{
	return new Node(); 
}	

Node* Node::CloneSubtree()
{
	Node* pClone = Clone();
	return pClone;
} // Node::CloneSubtree


END_NAMESPACE( sg )
