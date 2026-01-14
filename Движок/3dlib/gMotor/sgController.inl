/*****************************************************************************/
/*	File:	sgController.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/

#include "mSplines.h"
BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Controller implementation
/*****************************************************************************/
_inl void Controller::AttachNode( Node* _pNode )
{
	AddChild( _pNode );
	OnAttach();
} // Controller::AttachNode

_inl void Controller::DetachNode( Node* _pNode )
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (GetChild( i ) == _pNode)
		{
			RemoveChild( i );
		}
	}
} // Controller::AttachToNode

/*****************************************************************************/
/*	QuatAnimationCurve implementation
/*****************************************************************************/
_inl Quaternion QuatAnimationCurve::GetValue( float time ) const
{
	if (m_Values.size() == 0) return m_DefaultValue;

	Quaternion quat;
	int kfIdx1 = LocateTime( time );
	if (kfIdx1 < 0) 
	{
		return m_Values[0];
	}

	float t1 = m_Times[kfIdx1];
	const Quaternion&  q1 = m_Values[kfIdx1];

	int kfIdx2 = kfIdx1 + 1;
	if (kfIdx2 >= m_Times.size()) return q1;

	float t2 = m_Times[kfIdx2];
	const Quaternion&  q2 = m_Values[kfIdx2];
	if (time == t1) return q1;
    if (time == t2) return q2;

	float t = (time - t1) / (t2 - t1);

	quat.Slerp( q1, q2, t );
	return quat;
} // QuatAnimationCurve::GetValue

/*****************************************************************************/
/*	FloatAnimationCurve implementation
/*****************************************************************************/
_inl float FloatAnimationCurve::GetValue( float time ) const
{
	if (m_Values.size() == 0) return m_DefaultValue;
	
	int kfIdx1 = LocateTime( time );
	if (kfIdx1 < 0) return m_Values[0];

	float t1 = m_Times[kfIdx1];
	float v1 = m_Values[kfIdx1];
	
	int kfIdx2 = kfIdx1 + 1;
	if (kfIdx2 >= m_Values.size()) return v1;
	
	float t2 = m_Times[kfIdx2];
	float v2 = m_Values[kfIdx2];
	
	if (time == t1) return v1;
	if (time == t2) return v2;

	return LinearInterpolate( time, t1, v1, t2, v2 );
} // FloatAnimationCurve::GetValue

/*****************************************************************************/
/*	ColorAnimationCurve implementation
/*****************************************************************************/
_inl ColorValue	ColorAnimationCurve::GetValue( float time ) const
{
	if (m_Values.size() == 0) return m_DefaultValue;

	int kfIdx1 = LocateTime( time );
	if (kfIdx1 < 0) return m_Values[0];

	float		t1 = m_Times[kfIdx1];
	ColorValue	v1 = m_Values[kfIdx1];

	int kfIdx2 = kfIdx1 + 1;
	if (kfIdx2 >= m_Values.size()) return v1;

	float		t2 = m_Times[kfIdx2];
	ColorValue	v2 = m_Values[kfIdx2];

	if (time == t1) return v1;
	if (time == t2) return v2;

	ColorValue res;
	res.a = LinearInterpolate( time, t1, v1.a, t2, v2.a );
	res.r = LinearInterpolate( time, t1, v1.r, t2, v2.r );
	res.g = LinearInterpolate( time, t1, v1.g, t2, v2.g );
	res.b = LinearInterpolate( time, t1, v1.b, t2, v2.b );
	return res;
} // ColorValue::GetValue

/*****************************************************************************/
/*	PRSAnimation implementation
/*****************************************************************************/
_inl int PRSAnimation::GetPosXNKeys() const
{
	return posX.GetNKeys();
}

_inl int PRSAnimation::GetPosYNKeys() const
{
	return posY.GetNKeys();
}

_inl int PRSAnimation::GetPosZNKeys() const
{
	return posZ.GetNKeys();
}

_inl int PRSAnimation::GetRotNKeys() const
{
	return rot.GetNKeys();
}

_inl int PRSAnimation::GetScaleXNKeys() const
{
	return scX.GetNKeys();
}

_inl int PRSAnimation::GetScaleYNKeys() const
{
	return scY.GetNKeys();
}

_inl int PRSAnimation::GetScaleZNKeys() const
{
	return scZ.GetNKeys();
}

_inl void PRSAnimation::SetBaseAnimationName( const char* basename )
{
	m_BaseAnimationName = basename;
}

_inl const char* PRSAnimation::GetBaseAnimationName() const
{
	return m_BaseAnimationName.c_str();
}

_inl Matrix4D PRSAnimation::GetTransform( float time ) const
{
	Vector3D	sc( scX.GetValue( time ),  scY.GetValue( time ),  scZ.GetValue( time ) );
	Quaternion	quat = rot.GetValue( time );
	Vector3D	tr( posX.GetValue( time ), posY.GetValue( time ), posZ.GetValue( time ) );
	return Matrix4D( sc, quat, tr );
} // PRSAnimation::GetTransform

/*****************************************************************************/
/*	UVAnimation implementation
/*****************************************************************************/
_inl Matrix3D UVAnimation::GetTransform( float time ) const
{
	float scU  = m_ScU.GetValue ( time );
	float scV  = m_ScV.GetValue ( time );
	float posU = m_PosU.GetValue( time );
	float posV = m_PosV.GetValue( time );
	float rot  = m_Rot.GetValue ( time );

	float cosPhi = cosf( rot );
	float sinPhi = sinf( rot );
	
	Matrix3D m;
	m.e00 = scU*cosPhi;	
	m.e01 = scU*sinPhi;	
	m.e10 = -scV*sinPhi;	
	m.e11 = scV*cosPhi;	
	m.e20 = posU;
	m.e21 = posV;
	m.e02 = 0.0f;
	m.e12 = 0.0f;
	m.e22 = 1.0f;

	return m;
} // UVAnimation::GetTransform

END_NAMESPACE( sg )
