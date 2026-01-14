/*****************************************************************************/
/*	File:	sgMovable.h
/*	Desc:	Movable node classes
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGMOVABLE_H__
#define __SGMOVABLE_H__

namespace sg{

/*****************************************************************************/
/*	Class:	TransformNode
/*	Desc:	Scene graph node, which can be transformed 
/*****************************************************************************/
class TransformNode : public Node
{
public:
	_inl					TransformNode		();

	_inl const Matrix4D&	GetTransform		() const;
	_inl void				SetTransform		( const Matrix4D& matr );

	_inl const Matrix4D&	GetInitialTM		() const { return m_InitialTM; }
	_inl void				SetInitialTM		( const Matrix4D& matr ) { m_InitialTM = matr; }

	_inl const Matrix4D&	GetTopTM			() const;
	_inl void				Transform			( const Matrix4D& matr );

	_inl Vector3D			GetPos 				() const;
	_inl Vector3D			GetDirX				() const;
	_inl Vector3D			GetDirY				() const;
	_inl Vector3D			GetDirZ				() const;

	//  euler angles are in degrees
	_inl float				GetEulerX			() const;		
	_inl float				GetEulerY			() const;
	_inl float				GetEulerZ			() const;	

	_inl void				SetEulerX			( float val );
	_inl void				SetEulerY			( float val );
	_inl void				SetEulerZ			( float val ); 

	_inl void				SetPos 				( Vector3D v );
	_inl void				SetDirX				( Vector3D v );
	_inl void				SetDirY				( Vector3D v );
	_inl void				SetDirZ				( Vector3D v ); 

	_inl float				GetPosX				() const;
	_inl float				GetPosY				() const;
	_inl float				GetPosZ				() const;

	_inl void				SetPosX				( float v );
	_inl void				SetPosY				( float v );
	_inl void				SetPosZ				( float v ); 

	_inl float				GetScaleX			() const;
	_inl float				GetScaleY			() const;
	_inl float				GetScaleZ			() const;

	_inl void				SetScaleX			( float val );
	_inl void				SetScaleY			( float val );
	_inl void				SetScaleZ			( float val );

    void                    FlipAxis            ();

	_inl void				Reset				();
	void					SetToInitial		();
	void					SetSubtreeToInitial	();

	static const Matrix4D&	TMStackTop			() { return s_TMStack.Top(); }
	static void				ResetTMStack		( const Matrix4D* pTm = NULL );

	void					PushTM				( const Matrix4D& m ) { s_TMStack.Push( m ); m_WorldTM = s_TMStack.Top(); }
	const Matrix4D&			PopTM				() { return s_TMStack.Pop(); }

    static void				Push				( const Matrix4D& m ) { s_TMStack.Push( m ); }
    static const Matrix4D&	Pop				    () { return s_TMStack.Pop(); }


	Matrix4D				GetWorldTM			() const;
	void					SetWorldTM			( const Matrix4D& wTM );

	Matrix4D				GetParentWorldTM	() const;

	virtual void			Render				();
	virtual void			Serialize			( OutStream& os ) const;
	virtual void			Unserialize			( InStream& is  );
	virtual void			Expose				( PropertyMap& pm );

	NODE(TransformNode,Node,MOVB);
	
protected:
	Matrix4D				tm;				//  current node parent-related transform
	Matrix4D				m_InitialTM;	//  initial node parent-related transform
	Matrix4D				m_WorldTM;		//  accumulated world space transform

	static MatrixStack		s_TMStack;
}; // class TransformNode 

/*****************************************************************************/
/*	Class:	HudNode
/*	Desc:	Scene graph node, which has position in the screen space
/*****************************************************************************/
class HudNode : public Node
{
public:	
							HudNode() { scale = 1.0f; pos.zero(); width = height = 0.0f; }

	_inl float				GetPosX		() const;
	_inl float				GetPosY		() const;
	_inl float				GetPosZ		() const;

	_inl void				SetPosX		( float val );
	_inl void				SetPosY		( float val );
	_inl void				SetPosZ		( float val );

	_inl float				GetScale	() const;
	_inl void				SetScale	( float val );

	_inl float				GetWidth	() const;
	_inl float				GetHeight	() const;

	_inl void				SetWidth	( float val );
	_inl void				SetHeight	( float val );
	
	virtual Rct				GetBounds();

	virtual void			Serialize	( OutStream& os ) const;
	virtual void			Unserialize	( InStream& is );
	virtual void			Expose		( PropertyMap& pm );

	NODE(HudNode,Node,HUDL);

protected:
	
	Vector3D				pos;
	float					width, height;
	float					scale;

}; // class HudElement

/*****************************************************************************/
/*	Class:	Transform2D
/*	Desc:	Transformation node on the plane
/*****************************************************************************/
class Transform2D : public Node
{
public:
	virtual void Serialize			( OutStream& os ) const;
	virtual void Unserialize		( InStream& is	);
	void		 SetTransform		( const Matrix3D& m ) { tm = m; }
	
	NODE(Transform2D,Node,TR2D);

protected:
	Matrix3D		tm;
}; // class Transform2D

Matrix4D GetWorldTM( Node* pNode );

} // namespace sg

#ifdef _INLINES 
#include "sgMovable.inl"
#endif // _INLINES

#endif // __SGMOVABLE_H__