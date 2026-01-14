/*****************************************************************************/
/*	File:	sgController.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "mAlgo.h"
#include "kIOHelpers.h"
#include "mAnimCurve.hpp"

#ifndef _INLINES
#include "sgController.inl"
#endif // !_INLINES

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Controller implementation
/*****************************************************************************/
Controller::Controller()
{
}

Controller::~Controller()
{
}

void Controller::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void Controller::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

/*****************************************************************************/
/*	Animation implementation
/*****************************************************************************/
std::stack<float>	Animation::s_CurAnimTime;
std::stack<float>	Animation::s_Weight;
std::stack<float>	Animation::s_CurAnimTimeDelta;
DWORD				Animation::s_PrevTime = 0;

Animation::Animation() :	m_AnimationTime	( 0.0f ), 
							m_CurrentTime	( 0.0f ), 
							m_StartTime		( 0.0f ),
							m_bLooped		( true ),
							m_bPlayed		( false ),
							m_bPaused		( false ){}

void Animation::SetupTimeDelta()
{
	DWORD cTime = ::GetTickCount();
	if (s_PrevTime == 0) s_PrevTime = cTime;
	while (!s_CurAnimTimeDelta.empty()) s_CurAnimTimeDelta.pop();
	s_CurAnimTimeDelta.push( float( cTime - s_PrevTime ) );
	s_PrevTime = cTime;
} // Animation::SetupTimeDelta

void Animation::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	float reserved = 0.0f;
	os << reserved << m_AnimationTime << m_StartTime;
} // Animation::Serialize

void Animation::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	float reserved = 0.0f;
	is >> reserved >> m_AnimationTime >> m_StartTime;
} // Animation::Unserialize

void Animation::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Animation", this );
	pm.p( "TotalTime",		GetAnimationTime, SetAnimationTime );
	pm.p( "StartTime",		GetStartTime, SetStartTime );
	pm.f( "CurrentTime",	m_CurrentTime	);
	pm.f( "Looped",			m_bLooped		);
	pm.p( "Played",			IsPlaying		);
	pm.m( "Play",			Play			);
	pm.m( "Pause",			Pause			);
	pm.m( "Stop",			Stop			);
} // Animation::Expose

void Animation::Render()
{
	if (m_bPaused)
	{
		return;
	}

	if (m_bPlayed)
	{
		float cTime = GetCurrentTime() + CurTimeDelta();
		if (cTime > GetMaxTime()) 
		{
			if (m_bLooped)
			{
				cTime = fmod( cTime, GetMaxTime() ) + GetStartTime();
			}
			else
			{
				cTime = GetMaxTime();
			}
		}
		SetCurrentTime( cTime );
	}
	else
	{
		SetCurrentTime( CurTime() );
	}	
} // Animation::Render

void Animation::Play() 
{ 
    if (!m_bPlayed) SetCurrentTime( GetStartTime() );
    m_bPlayed = true;  
	m_bPaused = false;
}

bool Animation::IsPlaying() const 
{
	return m_bPlayed; 
}

void Animation::Pause() 
{
	m_bPaused = !m_bPaused;
}

void Animation::Stop() 
{ 
	m_bPlayed = false; 
	SetCurrentTime( 0.0f );
}

void Animation::Loop( bool bLoop ) 
{ 
	m_bLooped = bLoop; 
}

/*****************************************************************************/
/*	QuatAnimationCurve implementation
/*****************************************************************************/
bool QuatAnimationCurve::FromEuler(	const FloatAnimationCurve& yaw, 
									const FloatAnimationCurve& pitch,
									const FloatAnimationCurve& roll )
{
	if (yaw.	GetNKeys() == 0 &&
		pitch.	GetNKeys() == 0 && 
		roll.	GetNKeys() == 0)
	{
		Quaternion quat;
		quat.FromEulerAngles(	yaw.GetDefaultValue(), 
								pitch.GetDefaultValue(), 
								roll.GetDefaultValue() );
		SetDefaultValue( quat );
		return true;
	}

	Quaternion quat;
	float curTime = 0.0f;
	float maxTime = tmax( yaw.GetMaxTime(), pitch.GetMaxTime(), roll.GetMaxTime() ) + 1.0f;
	
	int maxY = yaw.		GetNKeys() ? yaw.  GetNKeys() - 1 : 0;
	int maxP = pitch.	GetNKeys() ? pitch.GetNKeys() - 1 : 0;
	int maxR = roll.	GetNKeys() ? roll. GetNKeys() - 1 : 0;
	
	int cY = 0; 
	int cP = 0;
	int cR = 0;

	float ty = yaw.	 GetNKeys()	? yaw.GetKeyTime	( 0 ) : maxTime;
	float tp = pitch.GetNKeys() ? pitch.GetKeyTime	( 0 ) : maxTime;
	float tr = roll. GetNKeys()	? roll.GetKeyTime	( 0 ) : maxTime;

	while (cY < maxY || cP < maxP || cR < maxR)
	{
		curTime = tmin( ty, tp, tr );

		if (ty == curTime) 
		{
			if (cY < maxY)
			{
				cY++;
				ty = yaw.GetKeyTime( cY );
			}
		}

		if (tp == curTime) 
		{
			if (cP < maxP)
			{
				cP++;
				tp = pitch.GetKeyTime( cP );
			}
		}

		if (tr == curTime) 
		{
			if (cR < maxR)
			{
				cR++;
				tr = roll.GetKeyTime( cR );
			}
		}

		quat.FromEulerAngles( yaw.	GetValue( curTime ), 
							  pitch.GetValue( curTime ),
							  roll. GetValue( curTime ) );
		AddKey( curTime, quat );	
	}

	return true;
} // QuatAnimationCurve::FromEuler

bool QuatAnimationCurve::ToEuler(	FloatAnimationCurve& yaw, 
									FloatAnimationCurve& pitch,
									FloatAnimationCurve& roll )
{
	assert( false );
	return false;
} // QuatAnimationCurve::ToEuler

void QuatAnimationCurve::LinearReduceKeys( float treshold )
{
	if (GetNKeys() == 0) return;

	int cKey = 0;
	float t1 = GetKeyTime( 0 );
	Quaternion v1 = GetKeyValue( 0 );

	float t2 = t1;
	Quaternion v2 = v1;
	Quaternion val;	

	int nKeys = GetNKeys();
	int curKey = 2;
	while (curKey < nKeys)
	{
		t2 = GetKeyTime( curKey );
		v2 = GetKeyValue( curKey );

		float t = (GetKeyTime( curKey - 1 ) - t1) / (t2 - t1);
		val.Slerp( v1, v2, t );
		val -= GetKeyValue( curKey - 1 );
		if (!v1.InSameHemisphere( v2 ))
		{
			int tt = 0;
		}
		if (val.norm2() <= treshold && v1.InSameHemisphere( v2 ))
		{
			DeleteKey( curKey - 1 );
			nKeys--;
		}
		else 
		{
			t1 = GetKeyTime ( curKey - 1 );
			v1 = GetKeyValue( curKey - 1 );
			curKey++;
		}
	}

	//  check if there are only two identical keys left
	if (nKeys == 2)
	{
		v2 -= v1;
		if (v2.norm2() <= treshold)
		{
			DeleteKey( 0 );
			DeleteKey( 0 );
			SetDefaultValue( v1 );
		}
	}
} // QuatAnimationCurve::LinearReduceKeys

bool QuatAnimationCurve::IsConstant( float tolerance ) const
{
	if (GetNKeys() == 0) return true;
	Quaternion first = GetKeyValue( 0 );

	for (int i = 1; i < GetNKeys(); i++)
	{
		Quaternion quat = GetKeyValue( i );
		quat -= first;
		if (fabs( quat.norm2() ) > tolerance) return false;
	}	
	return true;
} //  QuatAnimationCurve::IsConstant

//  corrects quaternion sequence to make all neighbor quaternions
//  oriented by shortest arc
void QuatAnimationCurve::CorrectOrientation()
{
	int nKeys = GetNKeys();
	for (int i = 1; i < nKeys; i++)
	{
		float cosTheta = m_Values[i - 1].dot( m_Values[i] );
		if (cosTheta < 0.0f)
		{
			m_Values[i].reverse();
		}
	}
} // QuatAnimationCurve::CorrectOrientation

/*****************************************************************************/
/*	FloatAnimationCurve implementation
/*****************************************************************************/
void FloatAnimationCurve::LinearReduceKeys( float treshold )
{
	if (GetNKeys() == 0) return;

	int cKey = 0;
	float t1 = GetKeyTime( 0 );
	float v1 = GetKeyValue( 0 );
	
	float t2 = t1;
	float v2 = v1;

	int nKeys = GetNKeys();
	int curKey = 2;
	while (curKey < nKeys)
	{
		t2 = GetKeyTime( curKey );
		v2 = GetKeyValue( curKey );
		float val = LinearInterpolate( GetKeyTime( curKey - 1 ), t1, v1, t2, v2 );
		if (fabs( val - GetKeyValue( curKey - 1 ) ) <= treshold)
		{
			DeleteKey( curKey - 1 );
			nKeys--;
		}
		else 
		{
			t1 = GetKeyTime( curKey - 1 );
			v1 = GetKeyValue( curKey - 1 );
			curKey++;
		}
	}

	//  check if there are only two identical keys left
	if (nKeys == 2 && fabs( v1 - v2 ) <= treshold)
	{
		DeleteKey( 0 );
		DeleteKey( 0 );
		SetDefaultValue( v1 );
	}
	
} // FloatAnimationCurve::LinearReduceKeys

float FloatAnimationCurve::GetMinVal() const
{
	if (m_Values.size() == 0) return m_DefaultValue;
	float minVal = m_Values[0];
	for (int i = 1; i < m_Values.size(); i++)
	{
		float val = m_Values[i];
		if (val < minVal) minVal = val;
	}
	return minVal;
} // FloatAnimationCurve::GetMinVal

float FloatAnimationCurve::GetMaxVal() const
{
	if (m_Values.size() == 0) return m_DefaultValue;
	float maxVal = m_Values[0];
	for (int i = 1; i < m_Values.size(); i++)
	{
		float val = m_Values[i];
		if (val > maxVal) maxVal = val;
	}
	return maxVal;
} // FloatAnimationCurve::GetMaxVal

bool FloatAnimationCurve::IsConstant( float tolerance ) const
{
	if (GetNKeys() == 0) return true;
	float first = GetKeyValue( 0 );
		
	for (int i = 1; i < GetNKeys(); i++)
	{
		if (fabs( GetKeyValue( i ) - first ) > tolerance) return false;
	}	
	return true;
}

/*****************************************************************************/
/*	PRSAnimation implementation
/*****************************************************************************/
bool	PRSAnimation::s_bFrozen						= false;
bool	PRSAnimation::s_bAnimateInvisible			= false;

void PRSAnimation::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	
	posX.Serialize( os );
	posY.Serialize( os );
	posZ.Serialize( os );
	rot .Serialize( os );
	scX .Serialize( os );
	scY .Serialize( os );
	scZ .Serialize( os );

	os << m_BaseAnimationName;
} // PRSAnimation::Serialize

void PRSAnimation::Unserialize( InStream& is )
{
	Parent::Unserialize( is );

	posX.Unserialize( is );
	posY.Unserialize( is );
	posZ.Unserialize( is );
	rot .Unserialize( is );
	scX .Unserialize( is );
	scY .Unserialize( is );
	scZ .Unserialize( is );

	is >> m_BaseAnimationName;
	m_AnimationTime = CalculateMaxTime();
} // PRSAnimation::Unserialize

//  blend between two animations
Matrix4D PRSAnimation::GetTransform(	const PRSAnimation* anm1, float time1,
										const PRSAnimation* anm2, float time2,
										float blendFactor )
{
	Vector3D sc1(	anm1->scX.GetValue( time1 ), 
					anm1->scY.GetValue( time1 ), 
					anm1->scZ.GetValue( time1 ) );

	Vector3D sc2(	anm2->scX.GetValue( time2 ), 
					anm2->scY.GetValue( time2 ), 
					anm2->scZ.GetValue( time2 ) );

	Quaternion quat1 = anm1->rot.GetValue( time1 );
	Quaternion quat2 = anm2->rot.GetValue( time2 );

	Vector3D tr1(	anm1->posX.GetValue( time1 ), 
					anm1->posY.GetValue( time1 ), 
					anm1->posZ.GetValue( time1 ) );

	Vector3D tr2(	anm2->posX.GetValue( time2 ), 
					anm2->posY.GetValue( time2 ), 
					anm2->posZ.GetValue( time2 ) );
	
	Vector3D	sc; 
	Quaternion	quat;  
	Vector3D	tr;  

	sc.addWeighted( sc1, sc2, 1.0f - blendFactor, blendFactor );
	quat.Slerp( quat1, quat2, blendFactor );
	tr.addWeighted( tr1, tr2, 1.0f - blendFactor, blendFactor );

	return Matrix4D( sc, quat, tr );
} // PRSAnimation::GetTransform

void PRSAnimation::Render()
{
	if (s_bFrozen) return;
	float curTime = CurTime();
	for (int i = 0; i < GetNChildren(); i++)
	{
		TransformNode* pNode = (TransformNode*)GetChild( i );
		if (!s_bAnimateInvisible && pNode->IsInvisible()) continue;
		if (Owns( pNode ) && !pNode->IsInvisible()) 
		{
			pNode->Render();
			continue;
		}
		if (!pNode->IsA<TransformNode>()) continue;
		float time = float( curTime );

		if (!IsDisabled()) pNode->SetTransform( GetTransform( time ) );
	}
} // PRSAnimation::Render

bool PRSAnimation::IsConstant()
{
	if (posX.GetNKeys() + posY.GetNKeys() + posZ.GetNKeys() > 0) return false;
	if (scX.GetNKeys() + scY.GetNKeys() + scZ.GetNKeys() > 0) return false;
	if (rot.GetNKeys() > 0) return false;
	Matrix4D tr = GetTransform( GetStartTime() );
	if (tr.equal( Matrix4D::identity )) return true;
	return false;
}

float PRSAnimation::CalculateMaxTime() const
{
	return tmax( tmax( scX.GetMaxTime(), scY.GetMaxTime(), scZ.GetMaxTime() ), 
				 rot.GetMaxTime(), 
				 tmax( posX.GetMaxTime(), posY.GetMaxTime(), posZ.GetMaxTime() ) );
}

void PRSAnimation::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PRSAnimation", this );
	pm.p( "BaseAnimationName", GetBaseAnimationName, SetBaseAnimationName );

	pm.p( "ScaleX", GetScaleX, SetScaleXAnimation, "floatAnimCurve" );
	pm.p( "ScaleY", GetScaleY, SetScaleYAnimation, "floatAnimCurve" );
	pm.p( "ScaleZ", GetScaleZ, SetScaleZAnimation, "floatAnimCurve" );
	pm.p( "PosX", GetPosX, SetPosXAnimation, "floatAnimCurve" );
	pm.p( "PosY", GetPosY, SetPosYAnimation, "floatAnimCurve" );
	pm.p( "PosZ", GetPosZ, SetPosZAnimation, "floatAnimCurve" );
	pm.p( "Rotation", GetRot, SetRotAnimation, "quatAnimCurve" );

    pm.m( "FlipXAxis", FlipXAxis );
    pm.m( "FlipYAxis", FlipYAxis );
    pm.m( "FlipZAxis", FlipZAxis );
} // PRSAnimation::Expose

void PRSAnimation::FlipXAxis()
{
    int nKeys = rot.GetNKeys();
    for (int i = 0; i < nKeys; i++)
    {
        Quaternion q = rot.GetKeyValue( i );
        
        Matrix3D m( q );
        m.getV1().reverse();
        m.getV2().reverse();
        q.FromMatrix( m );

        rot.SetKeyValue( i, q );
    }
    rot.CorrectOrientation();

    Quaternion q = rot.GetDefaultValue();
    Matrix3D m( q );
    m.getV1().reverse();
    m.getV2().reverse();
    q.FromMatrix( m );
    rot.SetDefaultValue( q );
} // PRSAnimation::FlipXAxis

void PRSAnimation::FlipYAxis()
{
    int nKeys = rot.GetNKeys();
    for (int i = 0; i < nKeys; i++)
    {
        Quaternion q = rot.GetKeyValue( i );

        Matrix3D m( q );
        m.getV0().reverse();
        m.getV2().reverse();
        q.FromMatrix( m );

        rot.SetKeyValue( i, q );
    }
    rot.CorrectOrientation();

    Quaternion q = rot.GetDefaultValue();
    Matrix3D m( q );
    m.getV0().reverse();
    m.getV2().reverse();
    q.FromMatrix( m );
    rot.SetDefaultValue( q );
} // PRSAnimation::FlipYAxis

void PRSAnimation::FlipZAxis()
{
    int nKeys = rot.GetNKeys();
    for (int i = 0; i < nKeys; i++)
    {
        Quaternion q = rot.GetKeyValue( i );

        Matrix3D m( q );
        m.getV0().reverse();
        m.getV1().reverse();
        q.FromMatrix( m );

        rot.SetKeyValue( i, q );
    }
    rot.CorrectOrientation();

    Quaternion q = rot.GetDefaultValue();
    Matrix3D m( q );
    m.getV0().reverse();
    m.getV1().reverse();
    q.FromMatrix( m );
    rot.SetDefaultValue( q );
} // PRSAnimation::FlipZAxis

void PRSAnimation::ReduceKeys( float scaleBias, float rotBias, float posBias )
{
	posX.LinearReduceKeys	( posBias 	);
	posY.LinearReduceKeys	( posBias 	);
	posZ.LinearReduceKeys	( posBias 	);
	rot.LinearReduceKeys	( rotBias 	);
	scX.LinearReduceKeys	( scaleBias );
	scY.LinearReduceKeys	( scaleBias );
	scZ.LinearReduceKeys	( scaleBias );
} // PRSAnimation::ReduceKeys

float PRSAnimation::GetScaleDiff( float anmTime, const Vector3D& sc )
{
	Vector3D cSc(	scX.GetValue( anmTime ), 
					scY.GetValue( anmTime ),
					scZ.GetValue( anmTime ) );
	cSc -= sc;
	return cSc.norm2();
} // PRSAnimation::GetScaleDiff

float PRSAnimation::GetPosDiff( float anmTime, const Vector3D& pos )
{
	Vector3D cPos(	posX.GetValue( anmTime ), 
					posY.GetValue( anmTime ),
					posZ.GetValue( anmTime ) );
	cPos -= pos;
	return cPos.norm2();
} // PRSAnimation::GetPosDiff

float PRSAnimation::GetRotDiff( float anmTime, const Quaternion& quat )
{
	Quaternion cQuat( rot.GetValue( anmTime ) );
	cQuat -= quat;
	return cQuat.norm2();
} // PRSAnimation::GetRotDiff

/*****************************************************************************/
/*	UVAnimation implementation
/*****************************************************************************/
UVAnimation::UVAnimation()
{
}

void UVAnimation::Render()
{
	if (s_bFrozen) return;
	float curTime = CurTime();
	for (int i = 0; i < GetNChildren(); i++)
	{
		TextureMatrix* pNode = (TextureMatrix*)GetChild( i );
		if (!s_bAnimateInvisible && pNode->IsInvisible()) continue;
		if (Owns( pNode ) && !pNode->IsInvisible()) 
		{
			pNode->Render();
			continue;
		}
		if (!pNode->IsA<TextureMatrix>()) continue;
		float time = float( curTime );

		if (!IsDisabled()) pNode->SetTextureTM( GetTransform( time ) );
	}
} // UVAnimation::Render

void UVAnimation::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	
	m_PosU.Serialize( os );
	m_PosV.Serialize( os );
	m_ScU.Serialize( os );
	m_ScV.Serialize( os );
	m_Rot.Serialize( os );
} // UVAnimation::Serialize

void UVAnimation::Unserialize( InStream& is	)
{
	Parent::Unserialize( is );
	
	m_PosU.Unserialize( is );
	m_PosV.Unserialize( is );
	m_ScU.Unserialize( is );
	m_ScV.Unserialize( is );
	m_Rot.Unserialize( is );
} // UVAnimation::Unserialize

void UVAnimation::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "UVAnimation", this );
	pm.p( "ScaleU",		GetScU,		SetScU,  "floatAnimCurve" );
	pm.p( "ScaleV",		GetScV,		SetScV,  "floatAnimCurve" );
	pm.p( "PosU",		GetPosU,	SetPosU, "floatAnimCurve" );
	pm.p( "PosV",		GetPosV,	SetPosV, "floatAnimCurve" );
	pm.p( "Rotation",	GetRot,		SetRot,  "floatAnimCurve" );
} // UVAnimation::Expose

END_NAMESPACE( sg )
