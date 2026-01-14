/*****************************************************************************/
/*	File:	sgController.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGCONTROLLER_H__
#define __SGCONTROLLER_H__

#include "mAnimCurve.hpp"
#include <stack>

namespace sg{

/*****************************************************************************/
/*	Class:	Controller
/*	Desc:	Controls scene node state
/*****************************************************************************/
class Controller : public Node
{
protected:

	virtual void			OnAttach(){}

public:
							Controller();
							~Controller();
	
	_inl void				AttachNode( Node* _pNode );
	_inl void				DetachNode( Node* _pNode );

	virtual void			Serialize( OutStream& os ) const;
	virtual void			Unserialize( InStream& is );


	NODE(Controller,Node,CLER);
}; // class Controller

const float c_ScaleTolerance = 0.0001f;
const float c_RotTolerance	 = 0.000001f;
const float c_PosTolerance	 = 0.001f;

/*****************************************************************************/
/*	Class:	FloatAnimationCurve
/*	Desc:	Keyframed float value curve
/*****************************************************************************/
class FloatAnimationCurve : public AnimationCurve<float>
{
public:
	FloatAnimationCurve() { m_DefaultValue = 0.0f; }

	void				LinearReduceKeys( float treshold );
	float				GetMinVal		() const;
	float				GetMaxVal		() const;
	virtual bool		IsConstant		( float tolerance = 0.0f ) const;
	virtual _inl float	GetValue( float time ) const;

}; // class FloatAnimationCurve

/*****************************************************************************/
/*	Class:	ColorAnimationCurve
/*	Desc:	Keyframed float value curve
/*****************************************************************************/
class ColorAnimationCurve : public AnimationCurve<ColorValue>
{
public:
						ColorAnimationCurve() { m_DefaultValue = ColorValue::White; }
	virtual _inl ColorValue	GetValue	( float time ) const;

}; // class ColorAnimationCurve

/*****************************************************************************/
/*	Class:	IntAnimationCurve
/*	Desc:	Keyframed integer value animation 
/*****************************************************************************/
class IntAnimationCurve : public AnimationCurve<int>
{
public:
	IntAnimationCurve() { m_DefaultValue = 0; }
}; // class FloatAnimationCurve

/*****************************************************************************/
/*	Class:	QuatAnimationCurve
/*	Desc:	Quaternion rotation animation interpolated curve
/*****************************************************************************/
class QuatAnimationCurve : public AnimationCurve<Quaternion>
{
public:
								QuatAnimationCurve() { m_DefaultValue.setIdentity(); } 
	bool						FromEuler(	const FloatAnimationCurve& yaw, 
											const FloatAnimationCurve& pitch,
											const FloatAnimationCurve& roll );

	bool						ToEuler( FloatAnimationCurve& yaw, 
										 FloatAnimationCurve& pitch,
										 FloatAnimationCurve& roll );

	virtual _inl Quaternion		GetValue			( float time ) const;
	virtual bool				IsConstant			( float tolerance = 0.0f ) const;
	void						LinearReduceKeys	( float treshold );
	void						CorrectOrientation	();
}; // QuatAnimationCurve 

/*****************************************************************************/
/*	Class:	Animation
/*	Desc:	Base animation class
/*****************************************************************************/
class Animation : public Controller
{
protected:

	float						m_AnimationTime;
	float						m_StartTime;
	float						m_CurrentTime;
	
	static bool					s_bFrozen;
	static bool					s_bAnimateInvisible;
	
	static std::stack<float>	s_CurAnimTime;
	static std::stack<float>	s_Weight;
	static std::stack<float>	s_CurAnimTimeDelta;

	static DWORD				s_PrevTime;

public:
							Animation			();
	_inl float				GetAnimationTime	() const { return m_AnimationTime; }
	_inl virtual void		SetAnimationTime	( float val ) { m_AnimationTime = val; }

	_inl float				GetMaxTime			() const { return m_StartTime + m_AnimationTime; }

	_inl float				GetStartTime		() const { return m_StartTime; }
	_inl virtual void		SetStartTime		( float val ) { m_StartTime = val; }

	_inl void				SetCurrentTime		( float val ) { m_CurrentTime = val; }
	_inl float				GetCurrentTime		() const { return m_CurrentTime; }

	void					Play				();  
	bool					IsPlaying			() const;  
	void					Pause				();  
	void					Stop				();  
	void					Loop				( bool bLoop = true );  


	virtual void			Serialize			( OutStream& os ) const;
	virtual void			Unserialize			( InStream& is );
	virtual void			Expose				( PropertyMap& pm );
	virtual void			Render				();

	virtual bool			BindNode			( Node* pNode ) { return AttachSubtree( pNode ); }

	static float			CurTime				() { return s_CurAnimTime.empty() ? 0.0f : s_CurAnimTime.top(); }
	static void				PushTime			( float anmTime ) { s_CurAnimTime.push( anmTime ); }
	static void				PopTime				() { if (!s_CurAnimTime.empty()) s_CurAnimTime.pop(); }

	static float			CurWeight			() { return s_Weight.empty() ? 0.0f : s_Weight.top(); }
	static void				PushWeight			( float w ) { s_Weight.push( w ); }
	static void				PopWeight			() { if (!s_Weight.empty()) s_Weight.pop(); }

	static float			CurTimeDelta		() { return s_CurAnimTimeDelta.empty() ? 0.0f : s_CurAnimTimeDelta.top(); }
	static void				PushTimeDelta		( float delta ) { s_CurAnimTimeDelta.push( delta ); }
	static void				PopTimeDelta		() { if (!s_CurAnimTimeDelta.empty()) s_CurAnimTimeDelta.pop(); }
	
	static void				SetupTimeDelta		();

	static void				Freeze				() { s_bFrozen = true; }
	static void				Unfreeze			() { s_bFrozen = false; }
	static void				AnimateInvisible	( bool anim = true ) { s_bAnimateInvisible = anim; }
	
	NODE(Animation,Controller,ANIM);

protected:
	bool						m_bLooped;
	bool						m_bPlayed;
	bool						m_bPaused;

}; // class Animation

/*****************************************************************************/
/*	Class:	PRSAnimation
/*	Desc:	Position/rotation/scaling animation sequence controller
/*****************************************************************************/
class PRSAnimation : public Animation
{
	//  position
	FloatAnimationCurve		posX, posY, posZ;
	//  rotation
	QuatAnimationCurve		rot;
	//  scale
	FloatAnimationCurve		scX, scY, scZ;

	//  symbolic identifier of the controlled node
	std::string				m_BaseAnimationName;

public:	
	_inl					PRSAnimation		() {}
	virtual void			Serialize			( OutStream& os ) const;
	virtual void			Unserialize			( InStream& is );
	virtual void			Expose				( PropertyMap& pm );
	virtual void			Render				();
	
	_inl Matrix4D			GetTransform		( float time ) const;
	
	//  blend beetween two animations
	static Matrix4D			GetTransform		( const PRSAnimation* anm1, float time1,
												  const PRSAnimation* anm2, float time2,
												  float blendFactor );
	
	_inl int				GetPosXNKeys		() const;
	_inl int				GetPosYNKeys		() const;
	_inl int				GetPosZNKeys		() const;

	_inl int				GetRotNKeys			() const;
	_inl int				GetScaleXNKeys		() const;
	_inl int				GetScaleYNKeys		() const;
	_inl int				GetScaleZNKeys		() const;

	bool					IsConstant			();	
	void					ReduceKeys			(	float scaleBias = c_ScaleTolerance, 
													float rotBias = c_RotTolerance, 
													float posBias = c_PosTolerance );

	float					CalculateMaxTime	() const;

	float 					GetScaleDiff		( float anmTime, const Vector3D& sc );
	float 					GetPosDiff			( float anmTime, const Vector3D& pos );
	float 					GetRotDiff			( float anmTime, const Quaternion& quat );

    void                    FlipXAxis           ();
    void                    FlipYAxis           ();
    void                    FlipZAxis           ();

	_inl void				SetBaseAnimationName( const char* basename );
	_inl const char*		GetBaseAnimationName() const;

	const FloatAnimationCurve*	GetPosX	() const { return &posX;	}
	const FloatAnimationCurve*	GetPosY	() const { return &posY;	}
	const FloatAnimationCurve*	GetPosZ	() const { return &posZ;	}

	const QuatAnimationCurve*	GetRot		() const { return &rot;	}
	const FloatAnimationCurve*	GetScaleX	() const { return &scX;	}
	const FloatAnimationCurve*	GetScaleY	() const { return &scY;	}
	const FloatAnimationCurve*	GetScaleZ	() const { return &scZ;	}

	FloatAnimationCurve*		GetPosXAnimation	(){ return &posX;	}
	FloatAnimationCurve*		GetPosYAnimation	(){ return &posY;	}
	FloatAnimationCurve*		GetPosZAnimation	(){ return &posZ;	}

	QuatAnimationCurve*			GetRotAnimation		(){ return &rot;	}
	FloatAnimationCurve*		GetScaleXAnimation	(){ return &scX;	}
	FloatAnimationCurve*		GetScaleYAnimation	(){ return &scY;	}
	FloatAnimationCurve*		GetScaleZAnimation	(){ return &scZ;	}


	void					SetPosXAnimation	( const FloatAnimationCurve* pPosX ) { if (pPosX) posX = *pPosX; }
	void					SetPosYAnimation	( const FloatAnimationCurve* pPosY ) { if (pPosY) posY = *pPosY; }
	void					SetPosZAnimation	( const FloatAnimationCurve* pPosZ ) { if (pPosZ) posZ = *pPosZ; }

	void					SetRotAnimation		( const QuatAnimationCurve* pRot  )	{ if (pRot) rot = *pRot; }
	void					SetScaleXAnimation	( const FloatAnimationCurve* pScX )	{ if (pScX) scX = *pScX; }
	void					SetScaleYAnimation	( const FloatAnimationCurve* pScY )	{ if (pScY) scY = *pScY; }
	void					SetScaleZAnimation	( const FloatAnimationCurve* pScZ )	{ if (pScZ) scZ = *pScZ; }


	NODE(PRSAnimation,Animation,PRSA);

}; // class PRSAnimation

/*****************************************************************************/
/*	Class:	UVAnimation
/*	Desc:	Texture matrix animation 
/*****************************************************************************/
class UVAnimation : public Animation
{
	FloatAnimationCurve		m_PosU, m_PosV;
	FloatAnimationCurve		m_ScU, m_ScV;
	FloatAnimationCurve		m_Rot;

public:
						UVAnimation		();
	virtual void		Render			();
	virtual void		Serialize		( OutStream& os ) const;
	virtual void		Unserialize		( InStream& is	);
	virtual void		Expose			( PropertyMap& pm );

	const FloatAnimationCurve*	GetPosU() const { return &m_PosU;	}
	const FloatAnimationCurve*	GetPosV() const { return &m_PosV;	}
	const FloatAnimationCurve*	GetRot () const { return &m_Rot;	}
	const FloatAnimationCurve*	GetScU () const { return &m_ScU;	}
	const FloatAnimationCurve*	GetScV () const { return &m_ScV;	}

	_inl Matrix3D		GetTransform	( float time ) const;

	void	SetPosU( const FloatAnimationCurve* pPosU ) { if (pPosU) m_PosU = *pPosU;	}
	void	SetPosV( const FloatAnimationCurve* pPosV ) { if (pPosV) m_PosU = *pPosV;	}
	void	SetRot ( const FloatAnimationCurve* pRot  )	{ if (pRot)  m_Rot	= *pRot;	}
	void	SetScU ( const FloatAnimationCurve* pScU  )	{ if (pScU)  m_ScU	= *pScU;	}
	void	SetScV ( const FloatAnimationCurve* pScV  )	{ if (pScV)  m_ScV  = *pScV;	}

	NODE(UVAnimation,Controller,UVAN);
}; // class UVAnimation

} // namespace sg

#ifdef _INLINES
#include "sgController.inl"
#endif // _INLINES

#endif // __SGCONTROLLER_H__
