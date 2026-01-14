/*****************************************************************************/
/*	File:	sgNode.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/

BEGIN_NAMESPACE( sg )

_inl OutStream& operator<<( OutStream& os, const Node* pNode )
{

	return os;
}

_inl InStream& operator>>( InStream& is, NodePtr& pNode )
{

	return is;
}

/*****************************************************************************/
/*	Node implementation
/*****************************************************************************/
_inl Node::Node() : m_Name("")
{
	m_Flags		= 0;
	m_RefCount	= 0;
	m_pParent	= NULL;
}

_inl Node::Node( const char* _name )
{
	m_Flags		= 0;
	m_RefCount	= 1;
	m_Name		= _name;
	m_pParent	= NULL;
}

_inl Node::~Node() 
{ 
}

_inl DWORD Node::AddRef()
{
	m_RefCount++;
	return m_RefCount;
}

_inl const char* Node::GetName() const
{
	return m_Name.c_str();
}

_inl DWORD Node::GetID() const
{
	return m_Id;
}

_inl void Node::SetName( const char* _name )
{
	m_Name = _name;
}

_inl bool Node::HasName( const char* m_Name ) const
{
	if (!m_Name) return false;
	return (strcmp( m_Name, GetName() ) == 0);
}

_inl bool Node::HasNameCI( const char* m_Name ) const
{
	if (!m_Name) return false;
	return (stricmp( m_Name, GetName() ) == 0);
}

_inl void Node::Render()
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (!GetChild( i )->GetFlagState( nfInvisible )) GetChild( i )->Render();
	}
} // Node::Render

_inl void Node::SetParent( Node* pNode )
{
	m_pParent = pNode;
} // Node::SetParent

_inl Node* Node::GetParent() const
{
	return m_pParent;
} // Node::SetParent

_inl void Node::AddChild( Node* pNode )
{
	if (!pNode) return;
	m_Children.push_back( pNode );
	pNode->AddRef();
	pNode->SetParent( this );
	OnChangeChildren();
} // Node::AddChild

_inl void Node::AddInput( Node* pNode )
{
	if (!pNode) return;
	m_Children.push_back( pNode );
	pNode->AddRef();
	OnChangeChildren();
}

_inl void Node::SwapChildren( int ch1, int ch2 )
{
	if (ch1 < 0 || ch2 < 0 || ch1 >= GetNChildren() || ch2 >= GetNChildren()) return;
	Node* temp = m_Children[ch1];
	m_Children[ch1] = m_Children[ch2];
	m_Children[ch2] = temp;
}

_inl int Node::GetChildIndex( Node* pChild ) const
{
	for (int i = 0; i < GetNChildren(); i++) if (GetChild( i ) == pChild) return i;
	return -1;
} // Node::GetChildIndex

_inl void Node::AddChild( Node* pNode, int position )
{
	if (!pNode) return;
	if (position < 0) position = 0;
	if (position >= GetNChildren()) position = GetNChildren() - 1;
	m_Children.insert( m_Children.begin() + position, pNode );
	pNode->AddRef();
	pNode->SetParent( this );
	OnChangeChildren();
} // Node::AddChild

_inl Node* Node::AddChild( const char* magic, const char* nodeName )
{
	Node* pNode = NodeFactory::instance().CreateNode( magic );
	if (!pNode) return NULL;
	pNode->SetName( nodeName );
	AddChild( pNode );
	OnChangeChildren();
	return pNode;
}


_inl void Node::operator <<	( Node* pNode )
{
	AddChild( pNode );
} // Node::operator <<	

_inl bool Node::RemoveChild( Node* pNode )
{
	bool erased = false;
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (GetChild( i ) == pNode)
		{
			m_Children.erase( m_Children.begin() + i );
			erased = true;
			if (Owns( pNode )) pNode->SetParent( NULL );
			pNode->Release();
			OnChangeChildren();
		}
	}
	return erased;
} // Node::RemoveChild

_inl void Node::RemoveChildren()
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (Owns( GetChild( i ) )) GetChild( i )->SetParent( NULL );
		GetChild( i )->Release();
	}
	m_Children.clear();
	OnChangeChildren();
} // Node::RemoveChildren

_inl bool Node::RemoveChild( int idx )
{
	if (idx < 0 || idx >= GetNChildren()) return false;
	Node* pNode = GetChild( idx );
	m_Children.erase( m_Children.begin() + idx );
	if (Owns( pNode )) pNode->SetParent( NULL );
	pNode->Release();
	OnChangeChildren();
	return true;
} // Node::RemoveChild

_inl Node* Node::GetChild( int idx )
{
	if (idx < 0 || idx >= GetNChildren()) return NULL;
	return m_Children[idx];
}

_inl Node* Node::GetChild( int idx ) const
{
	if (idx < 0 || idx >= GetNChildren()) return NULL;
	return m_Children[idx];
}

_inl bool Node::Owns( const Node* pNode ) const
{
	if (!pNode) return false;
	if (pNode->GetParent() == this) return true;
	return false;
}

_inl int Node::GetNChildren() const
{
	return m_Children.size();
}

_inl bool Node::GetFlagState( NodeFlags flag ) const
{
	return (m_Flags & flag) != 0;
}

_inl void Node::SetFlagState( NodeFlags flag, bool state )
{
	if (state == true)
	{
		m_Flags |= flag;
	}
	else
	{
		m_Flags &= ~flag;
	}
} // Node::SetFlagState

_inl bool Node::HasMagic( const char* Magic ) const
{
	 return (GetMagic() == *((DWORD*)Magic));
} // Node::HasMagic

_inl bool Node::HasInput( Node* pChild )
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (pChild == GetChild( i ) && !Owns( GetChild( i ) )) return true;
	}
	return false;
} // Node::HasInput

_inl bool Node::HasChild( Node* pChild, bool bSearchSubtree )
{
	if (!bSearchSubtree)
	{
		for (int i = 0; i < GetNChildren(); i++)
		{
			if (GetChild( i ) == pChild) return true;
		}
		return false;
	}

	Iterator it( this );
	while (it)
	{
		if ((Node*)it == pChild) return true;
		++it;
	}
	return false;
} // Node::HasChild

_inl Node* Node::GetInput( int idx )
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (!Owns( GetChild( i ) ))
		{
			if (idx == 0) return GetChild( i );
			idx--;
		}
	}
	return NULL;
} // Node::GetInput

_inl const char* Node::GetClassName() const 
{ 
	return GetNodeClassName(); 
}

_inl bool Node::IsInvisible() const	
{ 
	return GetFlagState( nfInvisible ); 
}

_inl void Node::SetInvisible( bool val )	
{ 
	SetFlagState( nfInvisible, val ); 
}

_inl bool Node::HasFocus() const
{
    return GetFlagState( nfHasFocus ); 
}
_inl void Node::SetFocus( bool val )
{
    SetFlagState( nfHasFocus, val ); 
}

_inl bool Node::IsImmortal() const				
{ 
	return GetFlagState( nfImmortal ); 
}

_inl void Node::SetImmortal( bool val )	
{ 
	SetFlagState( nfImmortal, val ); 
}

_inl bool Node::IsDisabled() const				
{ 
	return GetFlagState( nfDisabled ); 
}

_inl void Node::SetDisabled( bool val )	
{ 
	SetFlagState( nfDisabled, val ); 
}

_inl bool Node::DoDrawGizmo() const				
{ 
	return GetFlagState( nfDrawGizmo ); 
}

_inl void Node::SetDrawGizmo( bool val )	
{ 
	SetFlagState( nfDrawGizmo, val ); 
}

_inl bool Node::DoDrawAABB() const				
{ 
	return GetFlagState( nfDrawAABB ); 
}

_inl void Node::SetDrawAABB( bool val )	
{ 
	SetFlagState( nfDrawAABB, val ); 
}

END_NAMESPACE( sg )