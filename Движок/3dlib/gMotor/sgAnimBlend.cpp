/*****************************************************************/
/*  File:   sgAnimBlend.cpp
/*  Desc:   Animation blending routines
/*  Author: Silver, Copyright (C) GSC Game World
/*  Date:   Nov 2003
/*****************************************************************/
#include "stdafx.h"
#include "sgAnimBlend.h"
#include "IMesh.h"
#include "vMeshProcess.h"

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	AnimationBlock implementation
/*****************************************************************************/
AnimationBlock::AnimationBlock()
{
}

void AnimationBlock::Render()
{
	Parent::Render();
	PushTime( GetCurrentTime() );
	for (int i = 0; i < GetNChildren(); i++)
	{
		Node* pNode = GetChild( i );
		if (Owns( pNode ) && !pNode->IsInvisible()) pNode->Render();
	}
	PopTime();
} // AnimationBlock::Render

void AnimationBlock::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
} // AnimationBlock::Unserialize

void AnimationBlock::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // AnimationBlock::Unserialize

/*****************************************************************/
/*	AnimBlend implementation
/*****************************************************************/
AnimBlend::AnimBlend()
{
}

AnimBlend::~AnimBlend()
{
}

/*****************************************************************/
/*	AnimationBind implementation
/*****************************************************************/
AnimationBind::AnimationBind()
{
    m_StartTime         = 0.0f;
    m_PlayTime          = 0.0f;
    m_PlayRate          = 1.0f;
    m_InFade            = 0.0f;
    m_OutFade           = 0.0f;
    m_PlaybackMode      = pmLoop;
    m_bPlayBackwards    = false;
     
    m_ModelID           = 0xFFFFFFFF;
    m_AnimID            = 0xFFFFFFFF;
    m_MaxWeight         = 1.0f;
} // AnimationBind::AnimationBind

void AnimationBind::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "AnimationBind", this );
    pm.p( "Model", GetModelName, SetModelName, "file|Models" );
    pm.p( "Animation", GetAnimName, SetAnimName, "file|Models" );
    pm.f( "Weight",         m_MaxWeight         );
    pm.f( "StartTime",      m_StartTime         );
    pm.f( "PlayTime",       m_PlayTime          );
    pm.f( "PlayRate",       m_PlayRate          );
    pm.f( "InFade",         m_InFade            );  
    pm.f( "OutFade",        m_OutFade           ); 
    pm.p( "AnmDuration",    GetAnimTime         );
    pm.f( "PlaybackMode",   m_PlaybackMode      ); 
    pm.f( "PlayBackwards",  m_bPlayBackwards    );
} // AnimationBind::Expose

void AnimationBind::Unserialize( InStream& is )
{
    Parent::Unserialize( is );
    is >> m_ModelName >> m_AnimName >> m_MaxWeight >> 
            m_StartTime >> m_PlayTime >> m_PlayRate >> 
            m_InFade >> m_OutFade >> Enum2Byte( m_PlaybackMode ) >> m_bPlayBackwards;
} // AnimationBind::Unserialize

void AnimationBind::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_ModelName << m_AnimName << m_MaxWeight << 
            m_StartTime << m_PlayTime << m_PlayRate << 
            m_InFade << m_OutFade << Enum2Byte( m_PlaybackMode ) << m_bPlayBackwards;
} // AnimationBind::Serialize

DWORD AnimationBind::GetAnimID() const
{
    if (m_AnimID == 0xFFFFFFFF) m_AnimID = IMM->GetModelID( m_AnimName.c_str() );
    return m_AnimID;
}

DWORD AnimationBind::GetModelID() 
{
    if (m_ModelName.size() == 0) return 0xFFFFFFFF;
    if (m_ModelID == 0xFFFFFFFF) m_ModelID = IMM->GetModelID( m_ModelName.c_str() );
    return m_ModelID;
}

void AnimationBind::SetModelName( const char* name ) 
{ 
    m_ModelName = ToRelativePath( name );
}

void AnimationBind::SetAnimName( const char* name ) 
{ 
    m_AnimName = ToRelativePath( name );
    SetName( ParseFileName( name ) );
} // AnimationBind::SetAnimName

float AnimationBind::ToLocalTime( float t ) const
{
    float tEnd = IMM->GetAnimTime( m_AnimID );
    t *= m_PlayRate;
    if (m_PlaybackMode == pmOnce)
    {
        clamp( t, 0.0f, tEnd );
    }
    else if (m_PlaybackMode == pmPong)
    {
        int loop = t/tEnd;
        t = fmod( t, tEnd );
        if (loop&1) t = tEnd - t;
    }
    else if (m_PlaybackMode == pmLoop)
    {
        t = fmod( t, tEnd );
    }

    if (m_bPlayBackwards) t = tEnd - t;
    return t;
} // AnimationBind::ToLocalTime

float AnimationBind::GetWeight( float t ) const
{
    if (m_InFade == 0.0f && m_OutFade == 0.0f) return m_MaxWeight;
    
    return m_MaxWeight;
} // AnimationBind::GetWeight

bool AnimationBind::ApplyAnimation( DWORD modelID, float cTime )
{
    DWORD animID = GetAnimID();
    cTime = ToLocalTime( cTime );
    
    
    IMM->Animate( modelID, animID, cTime );

    return true; 
} // AnimationBind::ApplyAnimation

float AnimationBind::GetAnimTime() const
{
    return IMM->GetAnimTime( GetAnimID() );
} // AnimationBind::GetAnimTime

/*****************************************************************/
/*	AnimationSet implementation
/*****************************************************************/
AnimationSet::AnimationSet()
{
    m_Time          = 0.0f;
    m_ModelID       = 0xFFFFFFFF;
    m_CurTime       = 0.0f;

    m_bLooped       = false;
    m_bPlayed       = true;
    m_bPaused       = false;
    m_ActiveAnim    = 0;
    m_PlayMode      = apmPlayActive;

    m_ModelScale    = 1.0f;
}

void AnimationSet::OnChangeChildren()
{
    for (int i = 0; i < GetNChildren(); i++)
    {
        Node* pChild = GetChild( i );
        if (!pChild->IsA<AnimationBind>()) RemoveChild( i );
    }
} // AnimationSet::OnChangeChildren

bool AnimationSet::FromXML( XMLNode* pRoot )
{
    return false;
}

XMLNode* AnimationSet::ToXML()
{
    return NULL;
}

void AnimationSet::SetModelName( const char* name )
{
    m_ModelName = ToRelativePath( name );
    SetName( ParseFileName( name ) );
} // AnimationSet::SetModelName

void AnimationSet::Render()
{
    if (m_ModelID == 0xFFFFFFFF) m_ModelID = IMM->GetModelID( m_ModelName.c_str() );
    if (m_bPlayed && !m_bPaused)
    {
        m_CurTime += Animation::CurTimeDelta();
    }
    
    if (m_PlayMode == apmPlayActive)
    {
        AnimationBind* pAnim = (AnimationBind*)GetChild( m_ActiveAnim );
        if (!pAnim) return;
        pAnim->ApplyAnimation( m_ModelID, m_CurTime );
    }
    else if (m_PlayMode == apmBlendAll)
    {
        BlendAnimationTasks();
    }
    else if (m_PlayMode == apmBlendSequence)
    {
        
    }

    Matrix4D tm;
    tm.scaling( m_ModelScale );

    IRS->SetTextureFactor( 0xFFFF0000 );
    IMM->Render( m_ModelID, &tm );
} // AnimationSet::Render

void AnimationSet::BlendAnimationTasks()
{
    int nCh = GetNChildren();
    
    if (nCh < 2)return;
    if (nCh > 2) nCh = 2;

    //  flatten blending weights
    float sumW = 0.0f;
    for (int i = 0; i < nCh; i++)
    {
        AnimationBind* pAnim = GetAnimTask( i );
        sumW += pAnim->GetBlendWeight( m_CurTime );
    }
    
    //  blend with flattened weights
    for (int i = 0; i < nCh; i++)
    {
        AnimationBind* pAnim = GetAnimTask( i );
        float w = pAnim->GetBlendWeight( m_CurTime );
    }

    AnimationBind* pAnim1 = GetAnimTask( 0 );
    AnimationBind* pAnim2 = GetAnimTask( 1 );
    
    float t1 = pAnim1->ToLocalTime( m_CurTime );
    float t2 = pAnim2->ToLocalTime( m_CurTime );
    IMM->Animate( m_ModelID, 
                    pAnim1->GetBlendWeight( m_CurTime )/sumW, 
                    pAnim1->GetAnimID(), t1,
                    pAnim2->GetAnimID(), t2 );

} // AnimationSet::BlendAnimationTasks

void AnimationSet::Unserialize( InStream& is )
{
    Parent::Unserialize( is );
    is >> m_ModelName >> m_ActiveAnim >> m_Time >> m_bLooped >> m_ModelScale >> Enum2Byte( m_PlayMode );
} // AnimationSet::Unserialize

void AnimationSet::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_ModelName << m_ActiveAnim << m_Time << m_bLooped << m_ModelScale << Enum2Byte( m_PlayMode );
} // AnimationSet::Serialize

void AnimationSet::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "AnimationSet", this );
    pm.f( "CurTime",    m_CurTime, NULL, true );
    pm.p( "Model",      GetModelName, SetModelName, "file|Models" );
    pm.p( "ActiveAnim", GetActiveAnim, SetActiveAnim );
    pm.f( "Looped",     m_bLooped       );
    pm.f( "ModelScale", m_ModelScale    );
    pm.f( "PlayMode",   m_PlayMode      );

    pm.m( "Play",       Play            );
    pm.m( "Pause",      Pause           );
    pm.m( "Stop",       Stop            );
    pm.m( "Optimize",   Optimize        );
} // AnimationSet::Expose

void AnimationSet::SetActiveAnim( int val )
{
    if (val < 0 || val >= GetNChildren()) return;
    m_ActiveAnim = val;
} // AnimationSet::SetActiveAnim

void AnimationSet::Play()
{
    m_bPlayed = true;
    m_bPaused = false;
    m_CurTime = 0.0f;
} // AnimationSet::Play

void AnimationSet::Pause()
{
    m_bPaused = !m_bPaused;
}

void AnimationSet::Stop()
{
    m_bPlayed = false;
    m_bPaused = false;
}

void AnimationSet::Optimize()
{
    IMesh* iMdl = ConvertModel( m_ModelID );
}

END_NAMESPACE(sg)
