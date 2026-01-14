/*****************************************************************************/
/*	File:	sgNode.h
/*	Desc:	Scene graph node
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGNODE_H__
#define __SGNODE_H__

#include <map>
#include <vector>

#define REGNODE(CName)				CName::CName##Creator volatile CName::__##CName##Creator;

#define NODE(CName,PName,MWord)	virtual const char*	GetNodeClassName() const { return #CName; }		\
									virtual DWORD	GetMagic() const { return *((DWORD*)#MWord);}		\
									static bool		Filter( const sg::Node* pNode )						\
									{																	\
										return pNode->GetMagic() == *((DWORD*)#MWord);					\
									}																	\
									static bool		FnFilter( const sg::Node* pNode )					\
									{																	\
										return pNode->IsA<CName>();										\
									}																	\
									virtual bool HasFn( const char* magic ) const						\
									{																	\
										if (PName::HasFn( magic )) return true;							\
										return (*((DWORD*)#MWord) == *((DWORD*)magic));					\
									}																	\
									virtual bool HasFn( DWORD magic ) const								\
									{																	\
										if (PName::HasFn( magic )) return true;							\
										return (*((DWORD*)#MWord) == magic);							\
									}																	\
									static Node* CreateInstance()										\
									{																	\
										return new CName;												\
									}																	\
									void* operator new( size_t size )									\
									{																	\
										return NodePool::CreateNode<CName>();							\
									}																	\
									void operator delete( void* ptr )									\
									{																	\
										free( ptr );													\
									}																	\
									static DWORD Magic() { return *((DWORD*)#MWord);}					\
									class CName##Creator												\
									{																	\
									public:																\
										CName##Creator()												\
										{																\
										NodeFactory::instance().RegisterNodeType( CName :: Magic(),		\
											#CName, #PName, CName :: CreateInstance );					\
										}																\
									};																	\
									static CName##Creator volatile __##CName##Creator;					\
									typedef PName Parent;


#define NOT_IMPLEMENTED		virtual DWORD GetColor() const { return 0xFFFF0000; }

class PropertyMap;
class XMLNode;

namespace sg{

const int	c_MaxNodePathLen = 512;
const DWORD	c_BadID			 = 0xFFFFFFFF;

class Node;
typedef Node* NodePtr;

/*****************************************************************************/
/*	Class:	Node
/*	Desc:	Base class for the scene graph node
/*****************************************************************************/
class Node
{
	DWORD						m_Id;			//  unique for this session scene graph node ID
	WORD						m_Flags;		//  set of node properties flags 	
	DWORD						m_RefCount;		//  node's reference count
	
	//  Node can have one parent - the one that owns this node
	//  However, any number of other nodes can reference to this node as to child,
	//    and all this references are actually treated as "inputs"
	Node*						m_pParent;

	
	std::string					m_Name;		//  node name
	std::vector<Node*>			m_Children;	//  node's children

protected:


	struct PNodePair
	{
		Node*	pNode;
		Node*	pParent;
		PNodePair() : pNode(NULL), pParent(NULL){}
		PNodePair( Node* node, Node* parent ) : pNode(node), pParent(parent){}
	};
	//  serialization helper stuff
	typedef						std::map<Node*, DWORD>		NodePtrMap;
	typedef						std::map<DWORD, PNodePair>	NodeIdMap;
	typedef						std::vector<Node*>			NodePtrList;

	static NodePtrMap			s_NodeMap;
	static NodeIdMap			s_NodeIdMap;
	static NodePtrList			s_NodeList;

public:
	//  node flags operations
	enum NodeFlags
	{
		nfDrawGizmo		= 0x0010,	//  drawing helper
		nfInvisible		= 0x0020,	//  node is invisible (or disabled)
		nfDrawAABB		= 0x0040,	//  drawing bounding box
		nfEmbeddedData	= 0x0100,	//  node has embedded data (which is else external -  in file, for example)
		nfImmortal		= 0x0200,	//  cannot destroy this node
		nfDisabled		= 0x0400,	//  node is not active, but all children are
        nfHasFocus      = 0x0800    //  user has input focus on this node
	}; // enum NodeFlags


	_inl						Node			();
	_inl						Node			( const char* _name );
	_inl virtual				~Node			();
	
	_inl virtual void			Render			();
	virtual bool				Destroy			();
	virtual void				Expose			( PropertyMap& pm );
	virtual bool				IsEqual			( const Node* node ) const;
	virtual Node*				Clone			() const;
	virtual void				Activate		() {}

	Node*						CloneSubtree	();

	_inl DWORD					AddRef			();
	_inl int					GetNRef			() const { return m_RefCount; }
		 DWORD					Release			();
	_inl bool					HasMagic		( const char* Magic ) const;
	_inl bool					HasName			( const char* name ) const;
	_inl bool					HasNameCI		( const char* name ) const;
	_inl void					SetID			( DWORD val ) { m_Id = val; }

	//  children operations
	virtual _inl void			AddChild		( Node* pNode );
	_inl void					AddInput		( Node* pNode );
	_inl Node*					AddChild		( const char* magic, const char* nodeName = NULL );
	
	_inl bool					RemoveChild		( Node* pNode );
	_inl bool					RemoveChild		( int idx );
	_inl void					RemoveChildren	();
	_inl void					AddChild		( Node* pNode, int position );

	_inl void					SetParent		( Node* pNode );
	_inl Node*					GetParent		() const;
	_inl bool					Owns			( const Node* pNode ) const; 
	_inl bool					HasChild		( Node* pChild, bool bSearchSubtree = true );
	_inl bool					HasInput		( Node* pChild );
	_inl Node*					GetInput		( int idx );


	bool						AttachSubtree	( Node* pRoot );
	static Node*				CreateFromFile	( const char* fileName );
	bool						WriteToFile		( const char* fileName );

	
	template <class NodeT> NodeT* AddChild( const char* nodeName = NULL )
	{
		NodeT* pNode = new NodeT();
		if (!pNode) return NULL;
		if (nodeName) pNode->SetName( nodeName );
		AddChild( pNode );
		return pNode;
	} // Node::AddChild

	template <class NodeT> NodeT* GetChild( const char* nodeName = NULL  )
	{
		NodeT* pChild = FindChild<NodeT>( nodeName );
		if (!pChild) pChild = AddChild<NodeT>( nodeName );
		return pChild;
	} // Node::GetChild

	template <class NodeT> NodeT* FindChild( const char* nodeName = NULL )
	{
		Iterator it( this );
		while (it)
		{
			Node* pNode = (Node*)it;
			if (pNode->IsA<NodeT>())
			{
				if (nodeName)
				{ 
					if (pNode->HasName( nodeName )) return (NodeT*)pNode;
				}
				else return (NodeT*)pNode;
			}
			++it;
			pNode = (Node*)it;
			if (!it.GetParent()->Owns( pNode )) it.Up();
		}
		return NULL;
	} // Node::FindChild

	template <class NodeT> NodeT* FindChildFn( const char* nodeName = NULL  )
	{
		Iterator it( this );
		while (it)
		{
			Node* pNode = (Node*)it;
			if (pNode->IsA<NodeT>())
			{
				if (nodeName)
				{ 
					if (pNode->HasName( nodeName )) return (NodeT*)pNode;
				}
				else return (NodeT*)pNode;
			}
			++it;
			pNode = (Node*)it;
			if (!it.GetParent()->Owns( pNode )) it.Up();
		}
		return NULL;
	} // Node::FindChildFn

	Node*						FindChildByName		( const char* nodeName );
	Node*						FindChildByNameCI	( const char* nodeName );
	bool						ReplaceChild		( Node* pChild, Node* pNewChild );

	_inl void					operator <<			( Node* pNode );
	_inl Node*					GetChild			( int idx );
	_inl Node*					GetChild			( int idx ) const;
	_inl int					GetNChildren		() const;
	_inl int					GetChildIndex		( Node* pChild ) const;
	_inl void					SwapChildren		( int ch1, int ch2 );
	
	_inl const char*			GetName				() const;
	_inl DWORD					GetID				() const;
	_inl void					SetName				( const char* _name );
	_inl const char*			GetClassName		() const;

	//  color for the node in the editor
	virtual DWORD				GetColor			() const { return 0xFFFFFFFF; }

	//  serialization
	virtual void				Serialize			( OutStream& os ) const;
	virtual void				Unserialize			( InStream& is	);

	//  called when children structure is changed
	virtual void				OnChangeChildren	(){}

	void						Serialize			( OutStream& os, DWORD nBytes ) const;
	bool						SerializeSubtree	( OutStream& os ) const;
	static Node*				UnserializeSubtree	( InStream& is	);

	void						AdjustClonedName	( const char* name );
	void						FixInputs			();
	
	static Node*				CreateSubtree		( XMLNode* pRoot );
	virtual bool				FromXML				( XMLNode* pRoot );
	virtual XMLNode*			ToXML				();
	
	virtual void				PreSerialize		() const;	
	virtual void				PostUnserialize		();

	static Node*				PostUnserialize		( int nodeIdx );
	const char*					GetSymbolicPath		( Node* pRoot );
	_inl bool					GetFlagState		( NodeFlags flag ) const;
	_inl void					SetFlagState		( NodeFlags flag, bool state = true );

	_inl bool					IsInvisible			() const;
	_inl bool					IsImmortal			() const;
	_inl bool					IsDisabled			() const;
	_inl bool					DoDrawGizmo			() const;
	_inl bool					DoDrawAABB			() const;
    _inl bool					HasFocus            () const;

	_inl void					SetInvisible		( bool val = true );
	_inl void					SetImmortal			( bool val = true );
	_inl void					SetDisabled			( bool val = true );
	_inl void					SetDrawGizmo		( bool val = true );
	_inl void					SetDrawAABB			( bool val = true );
    _inl void                   SetFocus            ( bool val = true );

	int							CountNChildrenTotal	() const;
	
	typedef TreeIterator<Node>  Iterator;

	class NameFilter
	{
		typedef bool (*FilterCallback)( const Node* pNode );
		static const int c_MaxNodeNameLen = 128;
	public:
		NameFilter( const char* _name )
		{
			if (_name) 
			{
				assert( strlen( _name ) < c_MaxNodeNameLen );
				strcpy( m_Name, _name );
			}
			else
			{
				m_Name[0] = 0;
			}
		}

		operator FilterCallback() const
		{
			return filter;
		}

	protected:
		static char m_Name[c_MaxNodeNameLen];

		static bool filter( const Node* pNode )
		{
			if (!pNode) return false;
			if (!strcmp( pNode->GetName(), m_Name )) return true;
			return false;
		}
	}; // class NameFilter

	template <class T> bool IsA() const					
	{																
		return (dynamic_cast<const T*>( this ) != NULL);
	}

	static void				PushVisit	( Node* node );
	static void				PopVisit	( Node* node );
	static void				Visit		( Node* node );

	virtual const char*		GetNodeClassName	() const { return "Node";			 }
	virtual DWORD			GetMagic			() const { return *((DWORD*)"NODE"); }
	virtual DWORD			GetParentMagic		() const { return *((DWORD*)"XXXX"); }

	static bool				Filter			( const sg::Node* pNode );	
	static bool				FilterChildren	( const sg::Node* pNode );						
	virtual bool			HasFn			( const char* magic ) const; 
	virtual bool			HasFn			( DWORD magic ) const;
	static Node*			CreateInstance	();

	class NodeCreator									
	{														
	public:													
		NodeCreator();									
	};

	static DWORD Magic() { return *((DWORD*)"NODE"); }
	static NodeCreator volatile __NodeCreator;

private:
	static Node*			CreateFromXML		( XMLNode* pRoot );

	friend InStream& operator>>( InStream& is, NodePtr& pNode );
}; // class Node

_inl OutStream&	operator<<( OutStream& os, const Node* pNode );
_inl InStream&	operator>>( InStream& is, NodePtr& pNode );

typedef Node* (*NodeInstanceCreator)();

/*****************************************************************************/
/*	Class:	NodeClassDesc
/*	Desc:	Description of the node class
/*****************************************************************************/
struct NodeClassDesc
{
	DWORD					magic;
	const char*				className;
	const char*				parentClassName;
	NodeInstanceCreator		creator;
	int						numCreated;

	NodeClassDesc() : 
						magic			( 0 ), 
						className		( "" ),
						parentClassName	( "" ),
						creator			( NULL ),
						numCreated		( 0 ) {}

	NodeClassDesc(	DWORD				_magic, 
					const char*			_className, 
					const char*			_parentClassName,
					NodeInstanceCreator _creator ) : 
						magic			( _magic ), 
						className		( _className ),
						parentClassName	( _parentClassName ),
						creator			( _creator ),
						numCreated		( 0 ) {}

}; // struct NodeClassDesc

/*****************************************************************************/
/*	Class:	NodeFactory
/*	Desc:	Creates nodes of different types
/*****************************************************************************/
class NodeFactory : public Singleton<NodeFactory>
{
public:
	bool	RegisterNodeType(	DWORD				magic, 
								const char*			className, 
								const char*			parentClassName,
								NodeInstanceCreator creator )
	{
		if (m_Reg.find( magic ) != m_Reg.end()) 
		{
			if (!strcmp( m_Reg[magic].className, className )) return true;
			assert( !"Magic word is already used." );
			return false;
		}
		m_Reg[magic] = NodeClassDesc( magic, className, parentClassName, creator );
		return true;
	}

	Node* CreateNode( const char* magic )
	{
		return CreateNode( *((DWORD*)magic) );
	}

	Node* CreateNodeByClassName( const char* cname )
	{
		std::map<DWORD, NodeClassDesc>::iterator it = m_Reg.begin();
		while (it != m_Reg.end())
		{
			const NodeClassDesc& ncd = (*it).second;
			if (!strcmp( ncd.className, cname )) return CreateNode( ncd.magic );
			++it;
		}
		return NULL;
	}

	Node* CreateNode( DWORD magic )
	{
		// dirty hack to prevent compiler's skipping translation unit with REGNODEs
		static Node::NodeCreator* __NodeCreator = new Node::NodeCreator();
		if (!__NodeCreator) printf( "Something wrong with node factory." );

		std::map<DWORD, NodeClassDesc>::iterator it = m_Reg.find( magic );
		if (it == m_Reg.end()) return NULL; 
		NodeInstanceCreator creator = ((*it).second).creator;
		((*it).second).numCreated++;
		return creator();
	}

	int	GetNTypes() const
	{
		return m_Reg.size();
	}

	const NodeClassDesc& GetTypeDesc( int idx )
	{
		std::map<DWORD, NodeClassDesc>::iterator it = m_Reg.begin();
		for (int i = 0; i < idx; i++, ++it);
		return (*it).second;
	}

	DWORD GetTypeMagic( int idx ) const
	{
		std::map<DWORD, NodeClassDesc>::const_iterator it = m_Reg.begin();
		while (idx > 0) 
		{
			it++;
			if (it == m_Reg.end()) return 0;
		}
		return (*it).first;
	}

private:
	std::map<DWORD, NodeClassDesc>	m_Reg;
}; // class NodeFactory

} // namespace sg

#ifdef _INLINES 
#include "sgNode.inl"
#endif // _INLINES

#endif // __SGNODE_H__