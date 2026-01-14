/*****************************************************************************/
/*	File:	kPropertyMap.h
/*	Desc:	Code for the string interfacing the class members
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-13-2003
/*	Remark:	Welcome to the Big Ball Of Mud... ;)
/*****************************************************************************/
#ifndef __KPROPERTYMAP_H__
#define __KPROPERTYMAP_H__

class PropertyMap;
/*****************************************************************************/
/*	Class:	IExposed
/*	Desc:	Interface which exposed classes should implement
/*****************************************************************************/
class IExposed
{
public:
	virtual void		Expose( PropertyMap& pm ) = 0;
}; // class IExposed

/*****************************************************************************/
/*	Class:	ClassMember
/*	Desc:	Base abstract class for the property map member. All concrete  
/*				types (properties, methods, fields etc.) must implement this 
/*				kinda crappy interface
/*****************************************************************************/
class ClassMember
{
public:
	//  property attributes
	bool IsReadonly() const { return m_bReadonly; }
	bool IsDisabled() const { return m_bDisabled; }
	bool IsHidden  () const { return m_bHidden;   }

	//  returns name of the property
	const char*	GetName() const { return m_Name.c_str(); }
	
	//  returns description
	const char*	GetHint() const { return m_Hint.c_str(); }

	//  returns property type (in string format)
	const char*	GetType() const { return m_Type.c_str(); }

	//  gets property value by the string property name
	template <class TRes> bool Get( void* pObject, TRes& res )
		{ return _Get( pObject, &res ); }

	virtual bool ToString( void* pThis, char* buf, int bufSize ) const { return false; }
	virtual bool FromString( void* pThis, const char* val ) { return false; }
	virtual bool NextValue( void* pThis ) { return false; }
	virtual bool PrevValue( void* pThis ) { return false; }

	//  sets property value by the string property name
	template <class TVal> bool Set( void* pObject, const TVal& val )
		{ return _Set( pObject, &val ); }

	bool Run( void* pObject ) { return _Run( pObject ); }

protected:
	//  constructor is proptected, because only Property map has to manage
	//  instances of the property descriptions
	ClassMember( const char* name ) : 
		m_Name		( name		), 
		m_Type		( ""		),
		m_bReadonly	( false		),
		m_bDisabled	( false		),
		m_bHidden	( false		) {}
	
	//  this methods concrete member types must implement
	virtual bool _Get( void* pThis, void* pVal ) const = 0;
	virtual bool _Set( void* pThis, const void* pVal ) = 0;
	virtual bool _Run( void* pThis ) = 0;

	
	std::string			m_Name;		//  name of the property
	std::string			m_Hint;		//  property description in the human language
	std::string			m_Type;		//  type name
	std::string			m_Host;		//  host type name

	int					m_TypeID;	//  unique type identifier
	//  attribute flags
	bool				m_bDisabled;
	bool				m_bHidden;
	bool				m_bReadonly;
	
	friend class PropertyMap;
}; // class ClassMember

/*****************************************************************************/
/*	Class:	Property
/*	Desc:	Property type - imaginary field value which is read 
/*			 (and optionally written) through class member getters/setters
/*****************************************************************************/
template <class BaseT, class PropT>
class Property : public ClassMember
{
	typedef PropT (BaseT::*FnGetter) () const;
	typedef void  (BaseT::*FnSetter)( PropT val );

	FnGetter		m_fnGetter;	//  getter member function pointer
	FnSetter		m_fnSetter; //  setter member function pointer

public:
	Property( const char* name, const char* typeName, FnGetter get, FnSetter set = NULL ) : 
		ClassMember( name ), 
		m_fnGetter( get  ), 
		m_fnSetter( set  )
		{ 
			m_bReadonly = (set == NULL); 
			if (typeName != NULL) m_Type = typeName;
			else 
			{
				m_Type = TypeTraits<PropT>::TypeName();
			}
			m_TypeID = TypeTraits<PropT>::TypeID();
		}

		virtual bool _Get( void* pThis, void* pVal ) const
		{	
		 	if (!m_fnGetter || !pThis || !pVal) return false;
			*((PropT*)pVal) = ((reinterpret_cast<BaseT*>( pThis )->*m_fnGetter)());
			return true;
		}

		virtual bool _Set( void* pThis, const void* pVal )
		{	
			if (!m_fnSetter || !pThis || !pVal) return false;
			(reinterpret_cast<BaseT*>( pThis )->*m_fnSetter)( *((const PropT*)pVal) );
			return true;
		}

		virtual bool _Run( void* pThis )
		{	
			return false;
		}

		virtual bool ToString( void* pThis, char* buf, int bufSize ) const
		{
			PropT tmpVal;
			bool res = _Get( pThis, &tmpVal );
			if (res) res = TypeTraits<PropT>::ToString( tmpVal, buf, bufSize );
			return res;
		}

		virtual bool FromString( void* pThis, const char* val )
		{
			PropT tmpVal;
			bool res = TypeTraits<PropT>::FromString( tmpVal, val );
			if (res) _Set( pThis, &tmpVal );
			return res;
		}

		virtual bool NextValue( void* pThis ) 
		{ 
			if (!m_fnSetter || !m_fnGetter || !pThis) return false;
			PropT tmpVal = ((reinterpret_cast<BaseT*>( pThis )->*m_fnGetter)());
			if (!TypeTraits<PropT>::NextValue( tmpVal )) return false;
			(reinterpret_cast<BaseT*>( pThis )->*m_fnSetter)( tmpVal );
			return true; 
		}

		virtual bool PrevValue( void* pThis ) 
		{ 
			if (!m_fnSetter || !m_fnGetter || !pThis) return false;
			PropT tmpVal = ((reinterpret_cast<BaseT*>( pThis )->*m_fnGetter)());
			if (!TypeTraits<PropT>::PrevValue( tmpVal )) return false;
			(reinterpret_cast<BaseT*>( pThis )->*m_fnSetter)( tmpVal );
			return true;  
		}

}; // class Property

/*****************************************************************************/
/*	Class:	Method
/*	Desc:	Member function type - nothing takes, nothing returns, just works
/*****************************************************************************/
template <class BaseT>
class Method : public ClassMember
{
	typedef void (BaseT::*FnProcess)();

	FnProcess		m_fnProcess;

public:
	Method( const char* name, FnProcess call ) : 
		ClassMember	( name ), 
		m_fnProcess	( call ) 
		{
			m_TypeID = stMethod;
			m_Type = "method";
		}

	virtual bool _Get( void* pThis, void* pVal ) const
	{	
		return false;
	}

	virtual bool _Set( void* pThis, const void* pVal )
	{	
		return false;
	}

	virtual bool _Run( void* pThis )
	{	
		if (!m_fnProcess) return false;
		(reinterpret_cast<BaseT*>( pThis )->*m_fnProcess)();
		return true;
	}

}; // class Method

/*****************************************************************************/
/*	Class:	Method0
/*	Desc:	Member function type - returns value, takes 0 parameters
/*****************************************************************************/
template <class BaseT, class RetT>
class Method0 : public ClassMember
{
	typedef RetT (BaseT::*FnProcess)();

	FnProcess		m_fnProcess;
	RetT			m_Result;

public:
	Method0( const char* name, FnProcess call ) : ClassMember( name ), 
		m_fnProcess( call ) 
		{
			m_TypeID = stMethod;
			m_Type = "method";
		}

		virtual bool _Get( void* pThis, void* pVal ) const
		{	
			if (!m_fnProcess || !pThis || !pVal) return false;
			*((RetT*)pVal) = m_Result;
			return true;
		}

		virtual bool _Set( void* pThis, const void* pVal )
		{	
			return false;
		}

		virtual bool _Run( void* pThis )
		{	
			if (!m_fnProcess) return false;
			m_Result = (reinterpret_cast<BaseT*>( pThis )->*m_fnProcess)();
			return true;
		}

}; // class Method0

/*****************************************************************************/
/*	Class:	Method1
/*	Desc:	Member function type - returns value, takes 1 parameters
/*****************************************************************************/
template <class BaseT, class RetT, class Parm0T>
class Method1 : public ClassMember
{
	typedef RetT (BaseT::*FnProcess)(Parm0T);

	FnProcess		m_fnProcess;

	RetT			m_Result;
	Parm0T			parm0;

public:
	Method1( const char* name, FnProcess call ) : ClassMember( name ), 
		m_fnProcess( call ) 
		{
			m_TypeID = stMethod;
			m_Type = "method";
		}

		virtual bool _Get( void* pThis, void* pVal ) const
		{	
			if (!m_fnProcess || !pThis || !pVal) return false;
			*((RetT*)pVal) = m_Result;
			return true;
		}

		virtual bool _Set( void* pThis, const void* pVal )
		{	
			return false;
		}

		virtual bool _Run( void* pThis )
		{	
			if (!m_fnProcess) return false;
			m_Result = (reinterpret_cast<BaseT*>( pThis )->*m_fnProcess)( parm0 );
			return true;
		}

}; // class Method1

/*****************************************************************************/
/*	Class:	Method2
/*	Desc:	Member function type - returns value, takes 2 parameters
/*****************************************************************************/
template <class BaseT, class RetT, class Parm0T, class Parm1T>
class Method2 : public ClassMember
{
	typedef RetT (BaseT::*FnProcess)(Parm0T, Parm1T);

	FnProcess		m_fnProcess;

	RetT			m_Result;
	Parm0T			parm0;
	Parm1T			parm1;

public:
	Method2( const char* name, FnProcess call ) : ClassMember( name ), 
		m_fnProcess( call ) 
		{
			m_TypeID = stMethod;
			m_Type = "method";
		}

		virtual bool _Get( void* pThis, void* pVal ) const
		{	
			if (!m_fnProcess || !pThis || !pVal) return false;
			*((RetT*)pVal) = m_Result;
			return true;
		}

		virtual bool _Set( void* pThis, const void* pVal )
		{	
			return false;
		}

		virtual bool _Run( void* pThis )
		{	
			if (!m_fnProcess) return false;
			m_Result = (reinterpret_cast<BaseT*>( pThis )->*m_fnProcess)( parm0, parm1 );
			return true;
		}

}; // class Method2

/*****************************************************************************/
/*	Class:	Method3
/*	Desc:	Member function type - returns value, takes 3 parameters
/*****************************************************************************/
template <class BaseT, class RetT, class Parm0T, class Parm1T, class Parm2T>
class Method3 : public ClassMember
{
	typedef RetT (BaseT::*FnProcess)(Parm0T, Parm1T, Parm2T);

	FnProcess		m_fnProcess;

	RetT			m_Result;
	Parm0T			parm0;
	Parm1T			parm1;
	Parm2T			parm2;

public:
	Method3( const char* name, FnProcess call ) : ClassMember( name ), 
		m_fnProcess( call ) 
		{
			m_TypeID = stMethod;
			m_Type = "method";
		}

		virtual bool _Get( void* pThis, void* pVal ) const
		{	
			if (!m_fnProcess || !pThis || !pVal) return false;
			*((RetT*)pVal) = m_Result;
			return true;
		}

		virtual bool _Set( void* pThis, const void* pVal )
		{	
			return false;
		}

		virtual bool _Run( void* pThis )
		{	
			if (!m_fnProcess) return false;
			m_Result = (reinterpret_cast<BaseT*>( pThis )->*m_fnProcess)( parm0, parm1, parm2 );
			return true;
		}

}; // class Method3

/*****************************************************************************/
/*	Class:	Field
/*	Desc:	Class field member type with the straight access. 
/*****************************************************************************/
template <class FieldT>
class Field : public ClassMember
{
	int		m_Offset;

public:
	Field( const char* name, const char* typeName, void* pBase, 
			FieldT& field, bool bReadonly = false ) : ClassMember( name )
	{
		assert( pBase );
		m_Offset = (unsigned char*)(&field) - (unsigned char*)pBase;
		m_bReadonly = bReadonly;
		if (typeName != NULL) m_Type = typeName;
		else 
		{
			m_Type = TypeTraits<FieldT>::TypeName();
		}
		m_TypeID = TypeTraits<FieldT>::TypeID();
	}

	virtual bool _Get( void* pThis, void* pVal ) const
	{	
	 	 if (!pThis || !pVal) return false;
	 	 *((FieldT*)pVal) = FieldRef( pThis );
	 	 return true;
	}

	virtual bool _Set( void* pThis, const void* pVal )
	{	
		if (m_bReadonly || !pVal || !pThis) return false;
		FieldRef( pThis ) = *((const FieldT*)pVal);
		return true;
	}

	virtual bool _Run( void* pThis )
	{	
		return false;
	}

	virtual bool ToString( void* pThis, char* buf, int bufSize ) const
	{
		if (!pThis) return false;
		return TypeTraits<FieldT>::ToString( FieldRef( pThis ), buf, bufSize );
	}

	virtual bool FromString( void* pThis, const char* val )
	{
		if (m_bReadonly || !pThis) return false;
		return TypeTraits<FieldT>::FromString( FieldRef( pThis ), val );
	}
	
	virtual bool NextValue( void* pThis ) 
	{ 
		if (m_bReadonly || !pThis) return false;
		return TypeTraits<FieldT>::NextValue( FieldRef( pThis ) );
	}

	virtual bool PrevValue( void* pThis ) 
	{ 
		if (m_bReadonly || !pThis) return false;
		return TypeTraits<FieldT>::PrevValue( FieldRef( pThis ) );
	}

protected:
	FieldT&	FieldRef( void* pThis )
	{
		return *((FieldT*)((unsigned char*)pThis + m_Offset));
	}

	const FieldT& FieldRef( void* pThis ) const 
	{
		return *((FieldT*)((unsigned char*)pThis + m_Offset));
	}
}; // class Field

/*****************************************************************************/
/*	Enum:	SectionFlags
/*	Desc:	Set of the flag describing section properties
/*****************************************************************************/
enum SectionFlags
{
	sfClassSection		= 0x0000001,
	sfHidden			= 0x0000002,
	sfClosed			= 0x0000004,
	sfClosedByDefault	= 0x0000008,
	sfVoid				= 0x0000010
}; // enum SectionFlags

/*****************************************************************************/
/*	Class:	PropertySection
/*	Desc:	Section of the properties
/*****************************************************************************/
class PropertySection
{
protected:
	std::string					m_Name;
	std::vector<ClassMember*>	m_Member;
	DWORD						m_Flags;

public:
	PropertySection(){}	
	~PropertySection(){}

	const char*		GetName		() const	{ return m_Name.c_str();	}
	int				GetNMembers	() const	{ return m_Member.size();	}
	ClassMember*	GetMember	( int idx )	{ return m_Member[idx];		} 
	DWORD			GetFlags	() const	{ return m_Flags;			}
	void			AddMember	( ClassMember* pMem ) { m_Member.push_back( pMem ); }

	friend class PropertyMap;
}; // struct PropertySection

const int c_MaxSections = 64;
/*****************************************************************************/
/*	Class:	PropertyMap
/*	Desc:	Exposes object properties through string interface
/*****************************************************************************/
class PropertyMap
{
	void*							m_pObject;		//  pointer to the object we are mapping to
	std::string						m_ClassName;	//  name of the mapped object's class
	std::vector<PropertySection>	m_Sections;		//  property sections

public:
	PropertyMap() : m_pObject(NULL) { m_Sections.reserve( c_MaxSections ); }
	~PropertyMap() 
	{
		for (int i = 0; i < m_Sections.size(); i++)
		{
			PropertySection& sec = m_Sections[i];
			for (int j = 0; j < sec.GetNMembers(); j++) delete sec.GetMember( j );
		}
	}

	void	section( const char* name, DWORD flags = 0 )
	{
		AddSection( name, flags );
	}
	//  adding property
	template <class BaseT, class PropT>
		bool p( const char* name, PropT (BaseT::*get)() const, void  (BaseT::*set)( PropT val ) = NULL, const char* typeName = NULL )
	{
		return AddMember( new Property<BaseT, PropT>( name, typeName, get, set ) );
	}

	//  adding void method
	template <class BaseT> bool m( const char* name, void (BaseT::*call)() )
	{
		return AddMember( new Method<BaseT>( name, call ) );
	}

	template <class BaseT, class RetT> bool m0( const char* name, RetT (BaseT::*call)() )
	{
		return AddMember( new Method0<BaseT,RetT>( name, call ) );
	}

	template <class BaseT, class RetT, class Parm0T>
		bool m1( const char* name, RetT (BaseT::*call)( Parm0T ) )
	{
		return AddMember( new Method1<BaseT,RetT,Parm0T>( name, call ) );
	}

	template <class BaseT, class RetT, class Parm0T, class Parm1T>
		bool m2( const char* name, RetT (BaseT::*call)( Parm0T, Parm1T ) )
	{
		return AddMember( new Method2<BaseT,RetT,Parm0T,Parm1T>( name, call ) );
	}

	//  adding field
	template <class FieldT> bool f( const char* name, FieldT& field, 
									const char* typeName = NULL, bool readonly = false )
	{
		return AddMember( new Field<FieldT>( name, typeName, m_pObject, field, readonly ) );
	}

	void SetObject( void* pObject )
	{
		m_pObject = pObject;
	}

	void* GetObject() const { return m_pObject; }

	//  get value of the property/field or method result
	template <class TRes> bool get( const char* propName, TRes& res )
	{
		if (m_pObject == NULL) return false; 
		ClassMember* pProp = FindByName( propName );
		if (!pProp) return false;
		return pProp->Get( m_pObject, res );
	}

	//  get string representation of the member value
	bool get( const char* propName, char* buf, int bufSize )
	{
		if (m_pObject == NULL) return false; 
		ClassMember* pProp = FindByName( propName );
		if (!pProp) return false;
		return pProp->ToString( m_pObject, buf, bufSize );
	}

	//  get value of the property/field or method input parameter
	template <class TVal> bool set( const char* propName, const TVal& val )
	{
		if (m_pObject == NULL) return false; 
		ClassMember* pProp = FindByName( propName );
		if (!pProp) return false;
		return pProp->Set( m_pObject, val );
	}

	bool set( const char* propName, const char* val )
	{
		if (m_pObject == NULL) return false; 
		ClassMember* pProp = FindByName( propName );
		if (!pProp) return false;
		return pProp->FromString( m_pObject, val );
	}
	
	template <class TBase, class T> void start( const char* className, T* pObject, DWORD flags = 0 )
	{
		pObject->TBase::Expose( *this );
		SetObject( pObject );
		m_ClassName = std::string( className );
	}

	template <class T> void start( const char* className, T* pObject, DWORD flags = 0 )
	{
		SetObject( pObject );
		m_ClassName = std::string( className );
	}

	//  runs method with given name
	bool run( const char* propName )
	{
		if (m_pObject == NULL) return false; 
		ClassMember* pProp = FindByName( propName );
		if (!pProp) return false;
		return pProp->Run( m_pObject );
	}

	int	GetNSections() const { return m_Sections.size(); }
	
	PropertySection& GetSection( int idx )
	{
		return m_Sections[idx];
	} // GetSection

protected:

	//  adds new property to the map
	bool AddMember( ClassMember* pProperty )
	{
		if (m_Sections.size() == 0) AddSection( "", sfVoid );
		PropertySection& sec = m_Sections.back();
		sec.AddMember( pProperty );
		return true;
	}

	void AddSection( const char* name, DWORD flags = 0 )
	{
		m_Sections.push_back( PropertySection() );
		PropertySection& sec = m_Sections.back();
		sec.m_Name = name;
		sec.m_Flags = flags;
	}

	ClassMember* FindByName( const char* name )
	{
		for (int i = 0; i < GetNSections(); i++)
		{
			PropertySection& sec = GetSection( i );
			for (int j = 0; j < sec.GetNMembers(); j++)
			{
				ClassMember* pMember = sec.GetMember( j );
				if (!strcmp( pMember->GetName(), name )) return pMember;
			}
		}
		return NULL;
	}

}; // class PropertyMap

#endif // __KPROPERTYMAP_H__
