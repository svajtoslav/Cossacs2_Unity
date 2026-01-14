/*****************************************************************************/
/*	File:	sgEffect.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	20-04-2004
/*****************************************************************************/
#include "stdafx.h"
#include "uiControl.h"
#include "sgEffect.h"
#include "kTimer.h"
#include "kSystemDialogs.h"
#include "mNoise.h"
#include "mShapes.h"

#include "ITerrain.h"

#ifndef _INLINES
#include "sgEffect.inl"
#endif // _INLINES

sg::PEffectMgr g_EMgr;
IEffectManager* IEffMgr = &g_EMgr;

BEGIN_NAMESPACE(sg)
REGNODE( PModularEmitter		);
REGNODE( PEmitter				);
REGNODE( PConstEmitter			);
REGNODE( PStaticEmitter			);
REGNODE( PBurstEmitter			);
REGNODE( PRampEmitter			);

REGNODE( POperator				);
REGNODE( PInitializer			);

REGNODE( PShooter				);
REGNODE( PConeShooter			);
REGNODE( PDirectShooter			);
REGNODE( PRadialShooter			);
REGNODE( PRampShooter			);

REGNODE( PForce					);
REGNODE( PWind					);
REGNODE( PTorque				);
REGNODE( PDrag					);
REGNODE( PVortex				);
REGNODE( PAttract				);
REGNODE( POrbit					);
REGNODE( PTurbulence			);
REGNODE( PClampVelocity			);
REGNODE( PFollow				);
REGNODE( PAlphaFade				);
REGNODE( PSizeFade				);
REGNODE( PAVelRamp				);
REGNODE( PVelRamp				);
REGNODE( PFluctuate				);
REGNODE( PAlphaRamp				);
REGNODE( PSizeRamp				);
REGNODE( PColorRamp				);
REGNODE( PColorRampInit			);
REGNODE( PFrame					);
REGNODE( PUVMove				);

REGNODE( PTarget				);
REGNODE( PRetarget				);
REGNODE( PLightning				);
REGNODE( PHoming				);

REGNODE( PPlacer				);
REGNODE( PSpherePlacer			);
REGNODE( PPointPlacer			);
REGNODE( PBoxPlacer				);
REGNODE( PCylinderPlacer		);
REGNODE( PLinePlacer			);
REGNODE( PCirclePlacer			);
REGNODE( PModelPlacer			);
REGNODE( PCoastLinePlacer		);
REGNODE( PCoastBreak		    );

REGNODE( PSizeInit				);
REGNODE( PColorInit				);
REGNODE( PFrameInit				);

REGNODE( PRenderer				);
REGNODE( PModelRenderer			);
REGNODE( PTerrainDecal			);
REGNODE( PWaterDecal			);
REGNODE( PMouseBind		        );
REGNODE( PChainRenderer			);		
REGNODE( PSphereRenderer		);
REGNODE( PBillboardRenderer		);
REGNODE( PConeRenderer			);

REGNODE( PTrigger				);
REGNODE( POnDeath				);
REGNODE( POnBirth				);
REGNODE( POnTimer				);
REGNODE( POnHitGround			);
REGNODE( POnHitWater			);

REGNODE( PEffectMgr				);
REGNODE( PEffect				);

/*****************************************************************************/
/*	PEmitter implementation
/*****************************************************************************/
PEmitter::PEmitter()
{
	m_StartTime			= 0.0f;	
	m_TotalTime			= 10.0f;	
	m_MaxParticles		= 1; 

	SetLooped           ( false );	
    SetAutoUpdated      ( false );
    SetPlayedForever    ( false );
    SetPosDependent     ( false );

	m_TimeToLive		= 10.0f;	
	m_TimeToLiveV		= 0.0f;
	m_bWorldSpace		= true;
	m_pParentEmitter	= NULL;
	m_Probability		= 1.0f;
    
	//  FIXME: this was needed to prevent deleting emitter which is being used by manager
	AddRef();
} // PEmitter::PEmitter

float PEmitter::GetTimeToLive( const PParticle& p )
{
	return rndValuef( m_TimeToLive - m_TimeToLiveV, m_TimeToLive + m_TimeToLiveV );
}

void PEmitter::Expose( PropertyMap& pm )
{
	pm.start( "PEmitter", this );
    pm.p( "Name",           GetName, SetName                    );
	pm.p( "Invisible",		IsInvisible, SetInvisible           );

	pm.f( "StartTime",		m_StartTime 	                    );	
	pm.f( "TotalTime",		m_TotalTime 	                    );	
	pm.p( "Looped",			IsLooped, SetLooped                 );	
    pm.p( "AutoUpdate",	    IsAutoUpdated, SetAutoUpdated       );	
    pm.p( "PlayForever",	IsPlayedForever, SetPlayedForever   );	

	pm.f( "TimeToLive",		m_TimeToLive	                    );	
	pm.f( "+/- TimeToLive",	m_TimeToLiveV	                    );	
	pm.p( "WorldSpace",		IsWorldSpace, SetWorldSpace	        );	
    pm.p( "PositionDependent", IsPosDependent, SetPosDependent	);	
    pm.f( "Probability",    m_Probability                       );
} // PEmitter::Expose

void PEmitter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_StartTime << m_TotalTime << m_Flags << 
		m_Probability << m_TimeToLive << m_TimeToLiveV << m_bWorldSpace;
} // PEmitter::Serialize

void PEmitter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_StartTime >> m_TotalTime >> m_Flags >> 
		m_Probability >> m_TimeToLive >> m_TimeToLiveV >> m_bWorldSpace;
} // PEmitter::Unserialize

void PEmitter::OnChangeChildren()
{
	m_Operators.clear();
	for (int i = 0; i < GetNChildren(); i++)
	{
		Node* pChild = GetChild( i );
		if (pChild->IsA<POperator>()) 
		{
			m_Operators.push_back( (POperator*)pChild );
		}
		else
		{
			RemoveChild( pChild );
		}
	}
} // PEmitter::OnChangeChildren

void PEmitter::Process( PEmitterInstance* pEmitter )
{
    if (!pEmitter || pEmitter->m_Cycles > 0) return;
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->GetFlag( pfJustBorn ))
		{
			PushEntityContext( GetID() );
			PushEntityContext( p->m_ID );
			g_EMgr.SpawnEmitter( this, p->m_ID, pEmitter->m_ID, true );
			PopEntityContext();
			PopEntityContext();
		}
		p = p->m_pNext;
	}
} // PEmitter::Process

/*****************************************************************************/
/*	PConstEmitter implementation
/*****************************************************************************/
PConstEmitter::PConstEmitter()
{
	m_Rate = 2;
}

void PConstEmitter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PConstEmitter", this );
	pm.f( "Rate", m_Rate );
}

void PConstEmitter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Rate;
}

void PConstEmitter::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Rate;
}

int PConstEmitter::NumToEmit( PEmitterInstance* pInstance )
{
    if (pInstance->m_CurTime < m_StartTime || 
        pInstance->m_CurTime > m_TotalTime || 
        m_Rate < c_Epsilon) return 0;

    if (pInstance->m_pEmitter->IsPosDependent())
    {
        Vector3D pos        = pInstance->m_WorldTM.getTranslation();
        Vector3D prevPos    = pInstance->m_PrevWorldTM.getTranslation();

        float ds    = pos.distance2( prevPos )*0.01f;
        float rate  = m_Rate*pInstance->m_Intensity;
        float res   = ds*rate + pInstance->m_EmitAccum;
        pInstance->m_EmitAccum += floorf( res ) / rate;
        return res;
    }

	float dt    = pInstance->m_CurTime - pInstance->m_EmitAccum;
    float rate  = m_Rate*pInstance->m_Intensity;
	float res   = dt*rate;
    pInstance->m_EmitAccum += floorf( res ) / rate;
	return res;
} // PConstEmitter::NumToEmit

/*****************************************************************************/
/*	PStaticEmitter implementation
/*****************************************************************************/
PStaticEmitter::PStaticEmitter()
{
    m_bWorldSpace = false;
}

void PStaticEmitter::Expose( PropertyMap& pm )
{
	pm.start( "PStaticEmitter", this );
    pm.p( "Name",           GetName, SetName                        );
	pm.p( "Invisible",		IsInvisible, SetInvisible               );
	pm.f( "StartTime",		m_StartTime 	                        );	
	pm.p( "TotalTime",		GetStaticTotalTime, SetStaticTotalTime  );
	pm.p( "Looped",			IsLooped, SetLooped                     );	
    pm.p( "AutoUpdate",	    IsAutoUpdated, SetAutoUpdated           );	
    pm.p( "PlayForever",	IsPlayedForever, SetPlayedForever       );	
    pm.f( "WorldSpace",		m_bWorldSpace	                        );
}

void PStaticEmitter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void PStaticEmitter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
}

int PStaticEmitter::NumToEmit( PEmitterInstance* pInstance )
{
    if (pInstance->m_CurTime < m_StartTime) return 0;
	int res = pInstance->m_EmitAccum > m_StartTime ? 0 : 1;
    if (res > 0) pInstance->m_EmitAccum += 1.0f;
    return res;
}

/*****************************************************************************/
/*	PBurstEmitter implementation
/*****************************************************************************/
PBurstEmitter::PBurstEmitter()
{
	m_PNumber = 10;
}

void PBurstEmitter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PBurstEmitter", this );
	pm.f( "Number", m_PNumber );
}

void PBurstEmitter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_PNumber;
}

void PBurstEmitter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_PNumber;
}

int PBurstEmitter::NumToEmit(  PEmitterInstance* pInstance )
{
	if (pInstance->m_CurTime < m_StartTime) return 0;
    if (pInstance->m_EmitAccum <= 0.0f)
    {
        int res = m_PNumber*pInstance->m_Intensity;
        pInstance->m_EmitAccum = res;
        return res;
    }
    return 0;
} // PBurstEmitter::NumToEmit

/*****************************************************************************/
/*	PRampEmitter implementation
/*****************************************************************************/
PRampEmitter::PRampEmitter()
{
}

void PRampEmitter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PRampEmitter", this );
}

void PRampEmitter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void PRampEmitter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
}

int PRampEmitter::NumToEmit( PEmitterInstance* pInstance )
{
	return 0;
}

/*****************************************************************************/
/*	PModularEmitter implementation
/*****************************************************************************/
PModularEmitter::PModularEmitter()
{
	m_RndSeed  = 15731;
}

void PModularEmitter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PModularEmitter", this );
}

void PModularEmitter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void PModularEmitter::Unserialize( InStream& is  )
{	
	Parent::Unserialize( is );
	m_bWorldSpace = false;
}

/*****************************************************************************/
/*	PInitializer implementation
/*****************************************************************************/
void PInitializer::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age == 0.0f) InitParticle( *p );
		p = p->m_pNext;
	}
} // PInitializer::Process

/*****************************************************************************/
/*	PPlacer implementation
/*****************************************************************************/
void PPlacer::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age == 0.0f) 
		{
			p->m_Position += GetPosition( *p );
			p->m_Position += m_Position;
		}
		p = p->m_pNext;
	}
} // PPlacer::Process

void PPlacer::Render()
{
    if (!DoDrawGizmo()) return;
    DrawPoint( m_Position, GetColor(), 5.0f );
    DrawText( m_Position, GetColor(), "%s", GetName() );
    FlushText();
    rsFlushLines3D();
} // PPlacer::Render

void PPlacer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PPlacer", this );
	pm.f( "PosX", m_Position.x );
	pm.f( "PosY", m_Position.y );
	pm.f( "PosZ", m_Position.z );
} // PPlacer::Expose

void PPlacer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Position;
}

void PPlacer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Position;
}

/*****************************************************************************/
/*	PModelPlacer implementation
/*****************************************************************************/
PModelPlacer::PModelPlacer()
{
    m_ModelName = "model.c2m";
    m_Scale     = 1.0f;
    m_ModelID   = 0xFFFFFFFF;
}

void PModelPlacer::SetModelFile( const char* file )
{
    char path[_MAX_PATH];
    strcpy( path, file );
    ToRelativePath( path, _MAX_PATH );
    m_ModelName = path;
    m_ModelID = IMM->GetModelID( file );
} // PModelPlacer::SetModelFile

void PModelPlacer::Render()
{
    Parent::Render();
    if (DoDrawGizmo())
    {
        Node* pNode = NodePool::GetNode( m_ModelID );
        if (!pNode) return;

        Iterator it( pNode, Geometry::FnFilter );
        Geometry* pGeom = (Geometry*)(Node*)it;
        if (!pGeom) return;
        BaseMesh& bm = pGeom->GetPrimitive();
        Matrix4D tm = GetWorldTM( pGeom );
        IRS->SetWorldMatrix( tm );
        static int wSh = IRS->GetShaderID( "wireShaded" );
        IRS->SetCurrentShader( wSh );
        IRS->DrawPrim( bm );
    }
} // PModelPlacer::Render

void PModelPlacer::Process( PEmitterInstance* pEmitter )
{
    Node* pNode = NodePool::GetNode( m_ModelID );
    if (!pNode) return;
    
    Iterator it( pNode, Geometry::FnFilter );
    Geometry* pGeom = (Geometry*)(Node*)it;
    if (!pGeom) return;
    Primitive& bm = pGeom->GetPrimitive();
    Matrix4D tm = GetWorldTM( pGeom );

    PParticle* p = pEmitter->m_pParticle;
    while (p)
    {
        if (p->m_Age == 0.0f) 
        {
            Vector3D pos = GetRandomPoint( bm );
            pos *= tm;
            pos *= m_Scale;
            p->m_Position += pos;
            p->m_Position += m_Position;
        }
        p = p->m_pNext;
    }
} // PModelPlacer::Process

void PModelPlacer::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "PModelPlacer", this );
    pm.p( "ModelFile", GetModelFile, SetModelFile, "file|Models" );
}

void PModelPlacer::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_ModelName << m_Scale;
} // PModelPlacer::Serialize

void PModelPlacer::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_ModelName >> m_Scale;
    SetModelFile( m_ModelName.c_str() );
} // PModelPlacer::Unserialize

/*****************************************************************************/
/*	PCoastLinePlacer implementation
/*****************************************************************************/
CoastHash  PCoastLinePlacer::s_Coast;
PCoastLinePlacer::PCoastLinePlacer()
{
    m_Width         = 2000.0f;
    m_Height        = 2000.0f;
    m_Distance      = 100.0f;
    m_GridStep      = 128.0f;
    m_HeightDelta   = 8.0f;
    m_WaterLevel    = 50.0f;
    m_NSamples      = 5;
    m_WaveFront     = 128.0f;   
    m_WaveFrontBend = 0.1f;
    m_SlopeBias     = 0.99f;

    UpdateCache();
} // PCoastLinePlacer::PCoastLinePlacer

const float c_UpdateStep = 100.0f;
void PCoastLinePlacer::Process( PEmitterInstance* pEmitter )
{
    Render();
    
    float dt = pEmitter->GetTimeDelta();
    if (pEmitter->GetFlag( ifMoved )) 
    {
        Vector3D pos = pEmitter->m_WorldTM.getTranslation();
        if (pos.distance2( m_Position ) > c_UpdateStep*c_UpdateStep)
        {
            m_Position = pos;
            UpdateCache();
        }
    }

    int nPoints = s_Coast.numElem();
    int nP = pEmitter->GetNParticles();
    PParticle* p = pEmitter->m_pParticle;
    int nMaxParticles = nPoints*m_NSamples;
    if (nP > nMaxParticles)
    {
        for (int i = 0; i < nP - nMaxParticles; i++)
        {
            if (p->m_Age == 0.0f) p->m_Age = p->m_TimeToLive;
            p = p->m_pNext;
            if (!p) return;
        }
    }

    p = pEmitter->m_pParticle;
    if (m_NSamples == 0) return;
    int nParticles = pEmitter->GetNBorn();
    float ptRatio = float( nParticles ) / float( nPoints );

    float ratio = 0.0f;
    while (p)
    {
        int pIdx = rndValue( 0, nPoints - 1 );
        const CoastNode& node = s_Coast.elem( pIdx );
        Vector3D pos( node.m_Pos ); pos.z = m_WaterLevel;
        Vector3D dir = node.m_Dir;
        for (int i = 0; i < m_NSamples; i++)
        {
            if (p->m_Age == 0.0f) 
            {
                p->m_Position = pos;
                float vmag = p->m_Velocity.norm();                 
                float pos = rndValuef( -m_WaveFront, m_WaveFront );
                p->m_Position.x += -dir.y*pos;
                p->m_Position.y +=  dir.x*pos;

                float dy = pos*pos*m_WaveFrontBend;
                p->m_Position.x += dir.x*dy;
                p->m_Position.y += dir.y*dy;
                Vector3D n = ITerra->GetNormal( p->m_Position.x, p->m_Position.y );
                p->m_Velocity.x = -n.x;
                p->m_Velocity.y = -n.y;
                p->m_Velocity.z = 0.0f;
                p->m_Velocity *= vmag;
                ratio += ptRatio;
            }
            else
            {
                float H = ITerra->GetH( p->m_Position.x, p->m_Position.y );
                if (fabs( H - m_WaterLevel ) < m_HeightDelta*2.0f)
                {
                   p->m_Velocity.normalize();
                   p->m_Velocity *= 5.0f;
                }
            }
            p = p->m_pNext;
            if (!p) break;
        }
    }
} // PCoastLinePlacer::Process

void PCoastLinePlacer::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "PCoastLinePlacer", this );
    pm.f( "GridStep",       m_GridStep );
    pm.f( "HeightDelta",    m_HeightDelta );
    pm.p( "NPoints",        GetNPoints );
    pm.p( "Width",          GetZoneWidth, SetZoneWidth );    
    pm.p( "Height",         GetZoneHeight, SetZoneHeight );   
    pm.p( "Distance",       GetCoastDistance, SetCoastDistance );
    pm.f( "WaterLevel",     m_WaterLevel        );
    pm.f( "NumSamples",     m_NSamples          );
    pm.f( "WaveFront",      m_WaveFront         );
    pm.f( "WaveFrontBend",  m_WaveFrontBend     );
} // PCoastLinePlacer::Expose

void PCoastLinePlacer::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_Width << m_Height << m_Distance << m_GridStep << 
        m_HeightDelta << m_WaterLevel << m_NSamples << m_WaveFront << m_WaveFrontBend;
} // PCoastLinePlacer::Serialize

void PCoastLinePlacer::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_Width >> m_Height >> m_Distance >> m_GridStep >> 
            m_HeightDelta >> m_WaterLevel >> m_NSamples >> m_WaveFront >> m_WaveFrontBend;
    UpdateCache();
} // PCoastLinePlacer::Unserialize

void PCoastLinePlacer::SetCoastDistance( float val )
{
    m_Distance = val;
    UpdateCache();
} // PCoastLinePlacer::SetCoastDistance

void PCoastLinePlacer::SetZoneWidth( float val )
{
    m_Width = val;
    UpdateCache();
} // PCoastLinePlacer::SetZoneWidth

void PCoastLinePlacer::SetZoneHeight( float val )
{
    m_Height = val;
    UpdateCache();
} // PCoastLinePlacer::SetZoneHeight

void PCoastLinePlacer::Render()
{
    if (DoDrawGizmo())
    {
        int nP = s_Coast.numElem();
        IRS->ResetWorldMatrix();
        for (int i = 0; i < nP; i++)
        {
            const CoastNode& node = s_Coast.elem( i );
            Vector3D pos = node.m_Pos;
            DrawAnchor( pos, 5.0f, 0xFFFF0000 );
        }

        float begX = m_Position.x - m_Width *0.5f;
        float endX = m_Position.x + m_Width *0.5f;
        float begY = m_Position.y - m_Height*0.5f;
        float endY = m_Position.y + m_Height*0.5f;
        Vector3D a( begX, begY, m_WaterLevel );
        Vector3D b( endX, begY, m_WaterLevel );
        Vector3D c( endX, endY, m_WaterLevel );
        Vector3D d( begX, endY, m_WaterLevel );
        rsLine( a, b, 0xFFFF0000 );
        rsLine( b, c, 0xFFFF0000 );
        rsLine( c, d, 0xFFFF0000 );
        rsLine( d, a, 0xFFFF0000 );

        rsFlushPoly3D();
    }
} // PCoastLinePlacer::Render

void PCoastLinePlacer::UpdateCache()
{
    if (!ITerra) return;
    s_Coast.reset();

    float begX = m_Position.x - m_Width *0.5f;
    float endX = m_Position.x + m_Width *0.5f;
    float begY = m_Position.y - m_Height*0.5f;
    float endY = m_Position.y + m_Height*0.5f;
    
    for (float x = begX; x < endX; x += m_GridStep)
    {
        for (float y = begY; y < endY; y += m_GridStep)
        {
            float H = ITerra->GetH( x, y );
            if (m_WaterLevel - H < m_HeightDelta && H < m_WaterLevel)
            {
                Vector3D n = ITerra->GetNormal( x, y );
                Vector3D nd( n ); nd.z = 0.0f;
                nd.normalize();
                Vector3D dir( nd ); 
                nd *= m_Distance;
                CoastNode node;
                nd += Vector3D( x, y, 0.0f );
                float nH = ITerra->GetH( nd.x, nd.y );
                node.m_Pos = Vector3D( nd.x, nd.y, nH );
                if (nH >= m_WaterLevel - m_HeightDelta) continue;
                
                //  check if we are in "sleeve"
                Vector3D cPos( x, y, 0.0f );
                cPos.addWeighted( dir, m_Distance*2.0f );
                if (ITerra->GetH( cPos.x, cPos.y ) > nH) break;
                cPos.addWeighted( dir, m_Distance*4.0f );
                if (ITerra->GetH( cPos.x, cPos.y ) > nH) break;
                cPos.addWeighted( dir, m_Distance*4.0f );
                if (ITerra->GetH( cPos.x, cPos.y ) > nH) break;

                dir.reverse();
                node.m_Dir      = dir;
                node.m_CellX    = node.m_Pos.x / m_GridStep;
                node.m_CellY    = node.m_Pos.y / m_GridStep;
                s_Coast.add( node );
            }
        }
    }

} // PCoastLinePlacer::UpdateCache

/*****************************************************************************/
/*	PLinePlacer implementation
/*****************************************************************************/
PLinePlacer::PLinePlacer()
{
	m_End.set( 0.0f, 0.0f, 100.0f );
}

void PLinePlacer::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age == 0.0f) 
		{
			Vector3D dir( m_End );
			dir -= m_Position;
			float len = dir.normalize();
			dir *= rndValuef( 0.0f, len );
			dir += m_Position;
			p->m_Position += dir;
		}
		p = p->m_pNext;
	}
} // PLinePlacer::Process

void PLinePlacer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PLinePlacer", this );
	pm.f( "EndX", m_End.x );
	pm.f( "EndY", m_End.y );
	pm.f( "EndZ", m_End.z );
}

void PLinePlacer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_End;
}

void PLinePlacer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_End;
}

void PLinePlacer::Render()
{
    Parent::Render();
    if (DoDrawGizmo())
    {
        rsLine( m_Position, m_End, 0xFF0000AA, 0xFF0000AA );
    }
} // PLinePlacer::Render

/*****************************************************************************/
/*	PSpherePlacer implementation
/*****************************************************************************/
PSpherePlacer::PSpherePlacer()
{
	m_Radius	= 100.0f;
	m_bSurface	= false;
	m_bPlanar	= false;
}

Vector3D PSpherePlacer::GetPosition( const PParticle& p )
{
	if (m_bPlanar)
	{
		if (m_bSurface)
		{
			float ang = rndValuef( 0.0f, c_DoublePI );
			Vector3D res( m_Radius * cosf( ang ), m_Radius * sinf( ang ), 0.0f );
			return res;
		}
		else
		{
			float r = m_Radius*sqrtf( rndValuef() );
			float phi = rndValuef( 0, c_DoublePI );
			return Vector3D( r*cosf( phi ), r*sinf( phi ), 0.0f );
		}
	}
	else
	{
		if (m_bSurface)
		{
			float z = rndValuef( -m_Radius, m_Radius );
			float phi = rndValuef( 0, c_DoublePI );
			float cosTheta = sqrtf( 1.0f - z*z/m_Radius/m_Radius );

			Vector3D res( m_Radius * cosTheta * cosf( phi ), 
				m_Radius * cosTheta * sinf( phi ), z );
			return res;
		}
		else
		{
			float x, y, z;
			while (true)
			{
				x = rndValuef( -1.0f, 1.0f );
				y = rndValuef( -1.0f, 1.0f );
				z = rndValuef( -1.0f, 1.0f );
				if (x*x + y*y + z*z <= 1.0f) break;
			}
			return Vector3D( m_Radius*x, m_Radius*y, m_Radius*z );
		}
	}
} // PSpherePlacer::GetPosition

void PSpherePlacer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PSpherePlacer", this );
	pm.f( "Radius",		m_Radius );
	pm.f( "OnSurface",	m_bSurface );
	pm.f( "Planar",		m_bPlanar );
}

void PSpherePlacer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Radius << m_bSurface << m_bPlanar;
}

void PSpherePlacer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Radius >> m_bSurface >> m_bPlanar;
}

void PSpherePlacer::Render()
{
    Parent::Render();
    if (DoDrawGizmo())
    {
        if (m_bPlanar)
        {
            if (m_bSurface)
            {
                DrawCircle( m_Position, Vector3D::oZ, m_Radius , 0, 0xFF0000AA, 16 );
            }
            else
            {
                DrawCircle( m_Position, Vector3D::oZ, m_Radius , 0x660000AA, 0xFF0000AA, 16 );
            }
        }
        else
        {
            if (m_bSurface)
            {
                DrawSphere( Sphere( m_Position, m_Radius ), 0, 0xFF0000AA, 16 );
            }
            else
            {
                DrawSphere( Sphere( m_Position, m_Radius ), 0x660000AA, 0xFF0000AA, 16 );
            }
        }
    }
} // PSpherePlacer::Render

/*****************************************************************************/
/*	PBoxPlacer implementation
/*****************************************************************************/
PBoxPlacer::PBoxPlacer()
{
	m_DX = 100.0f;
	m_DY = 100.0f;
	m_DZ = 100.0f;
	m_bPlanar = false;
}

Vector3D PBoxPlacer::GetPosition( const PParticle& p )
{
	Vector3D pos( Vector3D::null );
	pos.x = rndValuef( 0.0f, m_DX ) - m_DX * 0.5f;
	pos.y = rndValuef( 0.0f, m_DY ) - m_DY * 0.5f;
	if (m_bPlanar) pos.z = m_DZ*0.5f; 
	else pos.z = rndValuef( 0.0f, m_DZ ) - m_DZ * 0.5f;
	return pos;
} // PBoxPlacer::GetPosition

void PBoxPlacer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PBoxPlacer", this );
	pm.f( "DX", m_DX );
	pm.f( "DY", m_DY );
	pm.f( "DZ", m_DZ );
	pm.f( "Planar", m_bPlanar );
} // PBoxPlacer::Expose

void PBoxPlacer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_DX << m_DY << m_DZ << m_bPlanar;
}

void PBoxPlacer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_DX >> m_DY >> m_DZ >> m_bPlanar;
}

void PBoxPlacer::Render()
{
    Parent::Render();
    if (DoDrawGizmo())
    {
        if (m_bPlanar)
        {
        
        }
        else
        {
            DrawAABB( AABoundBox( m_Position, m_DX*0.5f, m_DY*0.5f, m_DZ*0.5f ), 0x660000AA, 0xFF0000AA );
        }
    }
} // PBoxPlacer::Render

/*****************************************************************************/
/*	PCylinderPlacer implementation
/*****************************************************************************/
PCylinderPlacer::PCylinderPlacer()
{
	m_Radius			= 20.0f;
	m_Height			= 100.0f;
	m_bOnSurface		= false;
	m_bTopCap			= true;
	m_bBottomCap		= true;
	m_bSphericalCaps	= false;
} // PCylinderPlacer::PCylinderPlacer

Vector3D PCylinderPlacer::GetPosition( const PParticle& p )
{
	float z = rndValuef( 0.0f, m_Height );
	float x, y;
	
	if (m_bOnSurface)
	{
		float ang = rndValuef( 0.0f, c_DoublePI );
		x = m_Radius * cosf( ang );
		y = m_Radius * sinf( ang );

	}
	else
	{
		float r = m_Radius*sqrtf( rndValuef() );
		float phi = rndValuef( 0, c_DoublePI );
		x = r*cosf( phi );
		y = r*sinf( phi );
	}
	return Vector3D( x, y, z );
} // PCylinderPlacer::GetPosition

void PCylinderPlacer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PCylinderPlacer", this );
	pm.f( "Radius",			m_Radius		);
	pm.f( "Height",			m_Height		);
	pm.f( "OnSurface",		m_bOnSurface	);
} // PCylinderPlacer::Expose

void PCylinderPlacer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Radius << m_Height << 
		m_bTopCap << m_bBottomCap << m_bSphericalCaps << m_bOnSurface;
}

void PCylinderPlacer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Radius >> m_Height >>
		m_bTopCap >> m_bBottomCap >> m_bSphericalCaps >> m_bOnSurface;
}

void PCylinderPlacer::Render()
{
    Parent::Render();
    if (DoDrawGizmo())
    {
        Vector3D end( m_Position ); end.z += m_Height;
        Cylinder cyl( Line3D( m_Position, end ), m_Radius );
        if (m_bOnSurface)
        {
            DrawCylinder( cyl, 0, 0xFF0000AA, false, 16 );
        }
        else
        {
            DrawCylinder( cyl, 0x660000AA, 0xFF0000AA, true, 16 );
        }
    }
} // PCylinderPlacer::Render

/*****************************************************************************/
/*	PCirclePlacer implementation
/*****************************************************************************/
PCirclePlacer::PCirclePlacer()
{
	m_Radius			= 100.0f;
	m_Velocity			= 5.0f;
} // PCirclePlacer::PCirclePlacer

void PCirclePlacer::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	float alpha = (pEmitter->m_CurTime - pEmitter->m_StartTime)*m_Velocity;
	while (p)
	{
		if (p->m_Age == 0.0f) 
		{
			p->m_Position.x += m_Radius*cosf( alpha );
			p->m_Position.y += m_Radius*sinf( alpha );
		}
		p = p->m_pNext;
	}
} // PCirclePlacer::Process

void PCirclePlacer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PCirclePlacer", this );
	pm.f( "Radius",			m_Radius		);
	pm.f( "Velocity",		m_Velocity		);
} // PCirclePlacer::Expose

void PCirclePlacer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Radius << m_Velocity;
}

void PCirclePlacer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Radius >> m_Velocity;
}

/*****************************************************************************/
/*	PShooter implementation
/*****************************************************************************/
PShooter::PShooter()
{
	m_Velocity			= 100.0f;
	m_VelocityD			= 0.0f;
	m_AngVelocity		= Vector3D::null;
	m_AngVelocityD		= Vector3D::null;
	m_bAffectRotation	= false;
}

void PShooter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PShooter", this );
	pm.f( "Velocity",			m_Velocity );
	pm.f( "+/- Velocity",		m_VelocityD );
	pm.f( "AngVelocity",		m_AngVelocity.x );
	pm.f( "+/- AngVelocity",	m_AngVelocityD.x );
	pm.f( "AffectRotation",		m_bAffectRotation );
} // PShooter::Expose

void PShooter::Process( PEmitterInstance* pEmitter )
{
    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age == 0.0f) 
		{
			float mag = rndValuef( m_Velocity - m_VelocityD, m_Velocity + m_VelocityD );
			Vector3D vel = GetDirection( *p );
			if (pEmitter->m_pEmitter->IsWorldSpace()) pEmitter->m_WorldTM.transformVec( vel );
			vel.normalize();
			vel *= mag*s;
			p->m_Velocity += vel;
			
			p->m_AngVelocity.x = rndValuef(	m_AngVelocity.x - m_AngVelocityD.x, m_AngVelocity.x + m_AngVelocityD.x );
			p->m_AngVelocity.y = rndValuef(	m_AngVelocity.y - m_AngVelocityD.y, m_AngVelocity.y + m_AngVelocityD.y );
			p->m_AngVelocity.z = rndValuef(	m_AngVelocity.z - m_AngVelocityD.z, m_AngVelocity.z + m_AngVelocityD.z );

			if (m_bAffectRotation)
			{
				Vector3D vec( p->m_Velocity );
				g_EMgr.m_ScreenPlane.ProjectVec( vec );
				p->m_Roll = vec.Angle( g_EMgr.m_PlaneUpVec, g_EMgr.m_PlaneNVec );
			}
		}
		p = p->m_pNext;
	}
} // PShooter::Process

void PShooter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Velocity << m_VelocityD << m_AngVelocity << m_AngVelocityD << m_bAffectRotation;
} // PShooter::Serialize

void PShooter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Velocity >> m_VelocityD >> m_AngVelocity >> m_AngVelocityD >> m_bAffectRotation;
} // PShooter::Unserialize

/*****************************************************************************/
/*	PConeShooter implementation
/*****************************************************************************/
PConeShooter::PConeShooter()
{
	m_ConeAngle = 10;
	m_bPlanar	= false;
}

void PConeShooter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PConeShooter", this );
	pm.f( "ConeAngle", m_ConeAngle );
	pm.f( "Planar", m_bPlanar );
}

Vector3D PConeShooter::GetDirection( const PParticle& p )
{
    clamp( m_ConeAngle, 0.0f, 180.0f );
	if (m_bPlanar)
	{
		float ang = DegToRad( rndValuef( -m_ConeAngle, m_ConeAngle ) );
		return Vector3D( sinf( ang ), 0.0f, cosf( ang ) );
	}
	else 
	{
		float r1 = rndValuef();
		float r2 = rndValuef();

		float m1 = 1.0f - r2 * (1.0f - cosf( DegToRad( m_ConeAngle  ) )); 
		float m2 = sqrtf( 1.0f - m1*m1 );
		return Vector3D( m2*cosf( c_DoublePI * r1 ), m2*sinf( c_DoublePI * r1 ), m1 );
	}
} // PConeShooter::GetDirection

void PConeShooter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_ConeAngle << m_bPlanar;
}

void PConeShooter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_ConeAngle >> m_bPlanar;
}

void PConeShooter::Render()
{
    clamp( m_ConeAngle, 0.0f, 180.0f );
    if (DoDrawGizmo())
    {
        float ang = DegToRad( m_ConeAngle );
        Vector3D top ( Vector3D::null );
        Vector3D base( Vector3D::oZ );
        base *= 100.0f*cosf( ang );
        const Matrix4D& tm = TransformNode::TMStackTop();
        tm.transformPt( top );
        tm.transformPt( base );
        Cone cone( top, base, ang );
        if (m_bPlanar)
        {
            const Matrix4D c_FlatTM = Matrix4D( 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 );
            IRS->SetWorldMatrix( c_FlatTM ); 
            DrawCone( cone, 0x33FF0000, 0x77FF0000, 2 );
        }
        else
        {
            DrawCone( cone, 0x33FF0000, 0x77FF0000, 16 );
        }
        rsFlushLines3D();
        rsFlushPoly3D();
    }
} // PConeShooter::Render

/*****************************************************************************/
/*	PRadialShooter implementation
/*****************************************************************************/
PRadialShooter::PRadialShooter()
{
	m_bBindToOrigin = false;
	m_bPlanar		= false;
}

void PRadialShooter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PRadialShooter", this );
	pm.f( "BindToCentroid", m_bBindToOrigin );
	pm.f( "Planar",			m_bPlanar );
} // PRadialShooter::Expose

Vector3D PRadialShooter::GetDirection( const PParticle& p )
{
	if (m_bBindToOrigin)
	{
		Vector3D dir = p.m_Position;
		if (dir.normalize() > c_SmallEpsilon) return dir;
	}

	if (!m_bPlanar)
	{
		float r1 = rndValuef();
		float r2 = rndValuef();
		float m  = sqrtf( r2 * (1.0f - r2) );
		return Vector3D(2.0f * cosf( c_DoublePI * r1 )*m, 
						2.0f * sinf( c_DoublePI * r1 )*m, 
						1.0f - 2.0f*r2 );
	}
	else
	{
		float phi = rndValuef( 0, c_DoublePI );
		return Vector3D( cosf( phi ), sinf( phi ), 0.0f );
	}
	return Vector3D::null;
} // PRadialShooter::GetDirection

void PRadialShooter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_bBindToOrigin << m_bPlanar;
}

void PRadialShooter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_bBindToOrigin >> m_bPlanar;
}

/*****************************************************************************/
/*	PRampShooter implementation
/*****************************************************************************/
PRampShooter::PRampShooter()
{
	m_Repeat  = 1;
	m_Variation = 0.0f;
}

void PRampShooter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PRampShooter", this );
	pm.f( "Variation", m_Variation );
	pm.f( "Repeats", m_Repeat );
    pm.p( "NumKeys", GetNKeys );
	pm.p( "Ramp", GetRamp, SetRamp, "alpha_ramp" );
}

void PRampShooter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	m_Ramp.Serialize( os );
	os << m_Repeat << m_Variation;
}

void PRampShooter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	m_Ramp.Unserialize( is );
	is >> m_Repeat >> m_Variation;
}

void PRampShooter::Process( PEmitterInstance* pEmitter )
{
	float t = m_Repeat * (pEmitter->m_CurTime - pEmitter->m_StartTime)/pEmitter->m_TotalTime;	
	t -= floorf( t );

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age == 0.0f)
		{
			Vector3D vel = p->m_Velocity;
			if (vel.normalize() <= c_SmallEpsilon) vel = Vector3D::GetRandomDir();
			float mag = m_Ramp.GetAlpha( t ) + rndValuef( -m_Variation, m_Variation );
			vel *= mag;
			p->m_Velocity = vel;
		}
		p = p->m_pNext;
	}
} // PRampShooter::Process

/*****************************************************************************/
/*	PDirectShooter implementation
/*****************************************************************************/
PDirectShooter::PDirectShooter()
{
	m_Direction = Vector3D::oZ;
}

void PDirectShooter::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PDirectShooter", this );
	pm.f( "DirX", m_Direction.x );
	pm.f( "DirY", m_Direction.y );
	pm.f( "DirZ", m_Direction.z );
}

void PDirectShooter::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Direction;
}

void PDirectShooter::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Direction;
}

Vector3D PDirectShooter::GetDirection( const PParticle& p )
{
	return m_Direction;
}

void PDirectShooter::Render()
{
    Parent::Render();
    if (DoDrawGizmo())
    {
        DrawArrow( Vector3D::null, m_Direction, 0xFF22AA22, 100.0f );
    }
} // PDirectShooter::Render

/*****************************************************************************/
/*	POperator implementation
/*****************************************************************************/
POperator::POperator()
{
    m_bVisual = false;
}

void POperator::Expose( PropertyMap& pm )
{
	pm.start( "POperator", this );
	pm.p( "Invisible",		IsInvisible, SetInvisible );
    pm.p( "DrawGizmo",		DoDrawGizmo, SetDrawGizmo );
} // POperator::Expose

void POperator::OnChangeChildren()
{
	for (int i = 0; i < GetNChildren(); i++)
	{
		if (!GetChild( i )->IsA<PEmitter>()) RemoveChild( i );
	}
} // POperator::OnChangeChildren

/*****************************************************************************/
/*	PTrigger implementation
/*****************************************************************************/
void PTrigger::Trigger( const PEmitterInstance* pEmitter, PParticle& p )
{
	if (p.m_Age < m_Timeout) return;
	switch (m_TriggerType)
	{
	case etCreateEffect: 
        {
	    	if (!m_pEmitter) return;
	    	PushEntityContext( p.m_ID );
	    	g_EMgr.SpawnEmitter( m_pEmitter, p.m_ID, pEmitter->m_ID );
            Matrix4D tm; tm.translation( p.m_Position );
            IEffMgr->UpdateInstance( tm );
	    	PopEntityContext();
        }
		break;	
	case etDeath: 
        {
		    p.m_Age = p.m_TimeToLive; 
		    p.SetFlag( pfDead );
        }
		break;
	case etReflect: 
        {
		    Deflect( p );
        }
		break;		
	case etResetAge: 
        {
		    p.m_Age = 0.0f;
        }
		break;		
	case etStop: 
        {
		    p.m_Velocity.zero();
        }
		break;			
	case etStopRotation: 
        {
		    p.m_AngVelocity.zero();
        }
		break;	
	}
} // PTrigger::Trigger

void PTrigger::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PTrigger", this );
	pm.f( "Action", m_TriggerType );
	pm.f( "Damping", m_Damping );
	pm.f( "Timeout", m_Timeout );
} // PTrigger::Expose

void PTrigger::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << Enum2Byte( m_TriggerType ) << m_Damping << m_Timeout;
} // PTrigger::Serialize

void PTrigger::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> Enum2Byte( m_TriggerType ) >> m_Damping >> m_Timeout;
} // PTrigger::Unserialize

void PTrigger::OnChangeChildren()
{
	m_pEmitter = (PEmitter*)GetChild( 0 );
	if (!m_pEmitter->IsA<PEmitter>()) 
	{
		RemoveChild( m_pEmitter );
		m_pEmitter = NULL; 
	}
} // PTrigger::OnChangeChildren

/*****************************************************************************/
/*	POnDeath implementation
/*****************************************************************************/
void POnDeath::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->GetFlag( pfDead ))
		{
            if (m_TriggerType == etCreateEffect)
            {
                if (!m_pEmitter) { p = p->m_pNext; continue; }
                PushEntityContext( p->m_ID );
                int hInst = g_EMgr.SpawnEmitter( m_pEmitter, -1, pEmitter->m_ID, false );
                if (InheritPosition())
                {
                    Matrix4D tm;
                    tm.translation( p->GetTransform().getTranslation() );
                    IEffMgr->UpdateInstance( tm );
                }
                PopEntityContext();
            }
            else Trigger( pEmitter, *p );
		}
		p = p->m_pNext;
	}
} // POnDeath::Process

void POnDeath::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_bInheritPosition;
}
void POnDeath::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_bInheritPosition;
}

void POnDeath::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "POnDeath", this );
    pm.f( "InheritPosition", m_bInheritPosition );
} // POnDeath::Expose

/*****************************************************************************/
/*	POnBirth implementation
/*****************************************************************************/
void POnBirth::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age == 0.0f)
		{
			Trigger( pEmitter, *p );
		}
		p = p->m_pNext;
	}
} // POnBirth::Process

/*****************************************************************************/
/*	POnHitGround implementation
/*****************************************************************************/
void POnHitGround::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age > 0.0f)
        {	
            float h = ITerra->GetH( p->m_Position.x, p->m_Position.y );
            if ((p->m_Position.z - h)*(p->m_PrevPosition.z - h) < 0.0f) 
            {
                Trigger( pEmitter, *p );
            }
		}
		p = p->m_pNext;
	}
} // POnHitGround::Process

void POnHitGround::Deflect( PParticle& p )
{
	Vector3D normV, planeV;
	Plane pl( p.m_Position, ITerra->GetNormal( p.m_Position.x, p.m_Position.y ) );
	pl.Decompose( p.m_Velocity, normV, planeV );
	normV.reverse();
    if (normV.dot( p.m_Velocity ) >= 0.0f) 
    {
        return;
    }
	p.m_Velocity.add( normV, planeV );
	Vector3D dampV( p.m_Velocity ); 
	if (dampV.normalize() > m_Damping)
	{
		dampV *= m_Damping;
		p.m_Velocity -= dampV;
	}
	else p.m_Velocity = Vector3D::null;
} // POnHitGround::Deflect

/*****************************************************************************/
/*	POnHitWater implementation
/*****************************************************************************/
void POnHitWater::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age > 0.0f)
        {
            float h = ITerra->GetH( p->m_Position.x, p->m_Position.y );
            if (p->m_Position.z*p->m_PrevPosition.z <= 0.0f && h < 0.0f)
		    {
			    Trigger( pEmitter, *p );
		    }
        }
		p = p->m_pNext;
	}
} // POnHitWater::Process

void POnHitWater::Deflect( PParticle& p )
{
	p.m_Velocity.z = -p.m_Velocity.z;
	p.m_Velocity *= 1.0f - m_Damping;
	p.m_AngVelocity *= 1.0f - m_Damping;
}

/*****************************************************************************/
/*	POnTimer implementation
/*****************************************************************************/
POnTimer::POnTimer()
{
	m_Time = 5.0f;
}

void POnTimer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "POnTimer", this );
	pm.f( "Time", m_Time );
}

void POnTimer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Time;
}

void POnTimer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Time;
}

void POnTimer::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (p->m_Age < m_Time && p->m_Age + dt >= m_Time)
		{
			Trigger( pEmitter, *p );
		}
		p = p->m_pNext;
	}
} // POnTimer::Process

void POnTimer::Deflect( PParticle& p )
{
	p.m_Velocity.reverse();
}

/*****************************************************************************/
/*	PRenderer implementation
/*****************************************************************************/
PRenderer::PRenderer()
{
	m_TexID			= -1;
	m_TexName		= "particle.tga";
	m_TexName2		= "";
	m_BlendMode		= bmAdd;
	m_Intensity		= bmNormal;
	m_Tint			= 0xFFFFFFFF;
	m_Flags			= rfDoZTest;
	m_ZBias			= zbStage0;
    m_bVisual       = true;

    m_RenderBin.create( c_ParticleRenderBinSize, 0, vf2Tex );
    m_RenderBin.setIsQuadList( true );
} // PRenderer::PRenderer

void PRenderer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PRenderer", this );
	pm.p( "Pixels",			GetTexID,		SetTexID,		"texture"		);
	pm.p( "Texture",		GetTexName,		SetTexName,		"file|Textures" );
	pm.p( "Pixels2",		GetTexID2,		SetTexID2,		"texture"		);
	pm.p( "Texture2",		GetTexName2,	SetTexName2,	"file|Textures" );
	pm.p( "BlendMode",		GetBlendMode,	SetBlendMode	);
	pm.p( "Intensity",		GetIntensity,	SetIntensity	);
	pm.p( "DoZTest",		IsZTest,		SetZTest		);
	pm.p( "DoZWrite",		IsZWrite,		SetZWrite		);
    pm.p( "Dither",		    IsDitherEnable,	SetDitherEnable );
	pm.p( "HeadMoveDir",	IsHeadMoveDir,	SetHeadMoveDir	);
    pm.p( "NullLayer",      IsNullLayer,    SetNullLayer    );
	pm.p( "ZBias",			GetZBias,		SetZBias		);
    pm.f( "Tint",           m_Tint, "color" );
} // PRenderer::Expose

void PRenderer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_TexName << m_TexName2 << Enum2Byte( m_BlendMode ) << 
			Enum2Byte( m_Intensity ) << m_Tint << m_Flags << m_ZBias;			
}

void PRenderer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_TexName >> m_TexName2 >> Enum2Byte( m_BlendMode ) >> 
		Enum2Byte( m_Intensity ) >> m_Tint >> m_Flags >> m_ZBias;	

	SetTexName( m_TexName.c_str() );
	SetTexName2( m_TexName2.c_str() );
	UpdateShader();
} // PRenderer::Unserialize

void PRenderer::UpdateShader()
{
	static int ps_Add  = IRS->GetShaderID( "ps_Add"   );
	static int ps_Add2 = IRS->GetShaderID( "ps_Add2x" );
	static int ps_Add4 = IRS->GetShaderID( "ps_Add4x" );

	static int ps_Mod  = IRS->GetShaderID( "ps_Mod"   );
	static int ps_Mod2 = IRS->GetShaderID( "ps_Mod2x" );
	static int ps_Mod4 = IRS->GetShaderID( "ps_Mod4x" );

	if (m_BlendMode == bmAdd) 
	{
		if (m_Intensity == bmNormal		 ) { m_ShaderID = ps_Add; return; }
		if (m_Intensity == bmIntense	 ) { m_ShaderID = ps_Add2; return; }
		if (m_Intensity == bmSuperIntense) { m_ShaderID = ps_Add4; return; }
	}
	else 
	{
		if (m_Intensity == bmNormal		 ) { m_ShaderID = ps_Mod; return; }
		if (m_Intensity == bmIntense	 ) { m_ShaderID = ps_Mod2; return; }
		if (m_Intensity == bmSuperIntense) { m_ShaderID = ps_Mod4; return; }
	}
} // PRenderer::UpdateShader

void PRenderer::SetTexName( const char* val )
{ 
	char file		[_MAX_PATH	];
	char p_drive	[_MAX_DRIVE	];
	char p_dir		[_MAX_DIR	];
	char p_file		[_MAX_FNAME	];
	char p_ext		[_MAX_EXT	];

	_splitpath( val, p_drive, p_dir, p_file, p_ext );
	sprintf( file, "%s%s", p_file, p_ext );

	m_TexName = file; 
	m_TexID = IRS->GetTextureID( m_TexName.c_str() );
	UpdateShader();
} // PRenderer::SetTexName

void PRenderer::SetTexName2( const char* val )
{ 
	char file		[_MAX_PATH	];
	char p_drive	[_MAX_DRIVE	];
	char p_dir		[_MAX_DIR	];
	char p_file		[_MAX_FNAME	];
	char p_ext		[_MAX_EXT	];

	_splitpath( val, p_drive, p_dir, p_file, p_ext );
	sprintf( file, "%s%s", p_file, p_ext );

	m_TexName2 = file; 
	m_TexID2 = IRS->GetTextureID( m_TexName2.c_str() );
	UpdateShader();
} // PRenderer::SetTexName2

/*****************************************************************************/
/*	PModelRenderer implemetation
/*****************************************************************************/
PModelRenderer::PModelRenderer()
{
    m_ModelID   = 0xFFFFFFFF;
    m_ShaderID  = -1;
    m_TexID     = -1;
    m_TexID2    = -1;
}

void PModelRenderer::RenderGeometry( PEmitterInstance* pEmitter )
{
    if (!pEmitter->GetFlag( ifNeedDraw )) return;

    DWORD tint = m_Tint & 0x00FFFFFF;
    tint |= (((DWORD)(pEmitter->m_Alpha*255.0f))<<24);
    IRS->SetTextureFactor( tint );

    if (IsZWrite()) IRS->SetZWriteEnable();
    if (!IsZTest()) IRS->SetZEnable( false );
    IRS->SetDitherEnable( IsDitherEnable() );

    Matrix4D wtm = Matrix4D::identity;
    if (!pEmitter->m_pEmitter->IsWorldSpace())
    {
        Vector3D tr( pEmitter->m_WorldTM.getTranslation() );
        Matrix4D tm;
        tm.scaling( pEmitter->m_WorldTM.getV0().norm() );
        tm.translate( tr );
        wtm = tm;
    }

    if (m_ModelID == 0xFFFFFFFF) m_ModelID = IMM->GetModelID( m_ModelName.c_str() );
    if (m_ModelID == 0xFFFFFFFF) return;
    PParticle* p = pEmitter->m_pParticle;

    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    
    if (!pCam) return;
    if (m_ZBias > 0)
    {
        float shiftZ = -g_EMgr.GetZBiasMultiplier()*float( m_ZBias )*s;
        pCam->ShiftZ( -shiftZ );
        pCam->Render();	
        while (p)
        {
            if (p->m_Age > 0.0f)
            {
                Matrix4D tm = p->GetTransform();
                tm.mulLeft( wtm );
                IMM->Render( m_ModelID, &tm );
            }
            p = p->m_pNext;
        }
        pCam->ShiftZ( shiftZ );
        pCam->Render();	
    }
    else
    {
        while (p)
        {
            if (p->m_Age > 0.0f)
            {
                Matrix4D tm = p->GetTransform();
                IMM->Render( m_ModelID, &tm );
            }
            p = p->m_pNext;
        }
    }
} // PModelRenderer::Process

void PModelRenderer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PModelRenderer", this );
    pm.p( "Model", GetModelName, SetModelName, "file|Models" );
} // PModelRenderer::Expose

void PModelRenderer::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_ModelName;
}

void PModelRenderer::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_ModelName;
}

/*****************************************************************************/
/*	PBillboardRenderer implemetation
/*****************************************************************************/
PBillboardRenderer::PBillboardRenderer()
{
	m_Alignment = baCamera;
	m_RefPoint = Vector3D( 0.0f, 0.0f, 0.0f );
}

template <class TVert>
bool AddQuad( Primitive& pri, const Vector3D& pos, 
				float sizeX, float sizeY,
                float refX, float refY,
				float rot, DWORD color, 
				const Rct& uv, const Rct& uv2, 
				const Matrix4D& camTM )
{
	int nVert = pri.getNVert();
	if (nVert == pri.getMaxVert()) return false;
	TVert* v = ((TVert*)pri.getVertexData() + nVert);

    float cr = 1.0f;
    float sr = 0.0f;
    if (rot > 0.0f)
    {
        cr = cosf( rot );
        sr = sinf( rot );
    }
	float xc = refX * sizeX;
    float yc = refY * sizeY;

    Matrix4D bTM(   cr*sizeX,       sr*sizeX,       0.0f, 0.0f,
                    -sr*sizeY,      cr*sizeY,       0.0f, 0.0f, 
                    0.0f,           0.0f,           1.0f, 0.0f,
                    -cr*xc + sr*yc, -sr*xc - cr*yc, 0.0f, 1.0f );

    bTM *= camTM;
	Vector4D lt( 0.0f, 0.0f, 0.0f, 1.0f );
	Vector4D rt( 1.0f, 0.0f, 0.0f, 1.0f );
	Vector4D lb( 0.0f, 1.0f, 0.0f, 1.0f );
	Vector4D rb( 1.0f, 1.0f, 0.0f, 1.0f );

    lt *= bTM;
    rt *= bTM;
    lb *= bTM;
    rb *= bTM;

	v[0].x = lt.x + pos.x;
	v[0].y = lt.y + pos.y;
	v[0].z = lt.z + pos.z;

	v[1].x = rt.x + pos.x;
	v[1].y = rt.y + pos.y;
	v[1].z = rt.z + pos.z;

	v[2].x = lb.x + pos.x;
	v[2].y = lb.y + pos.y;
	v[2].z = lb.z + pos.z;

	v[3].x = rb.x + pos.x;
	v[3].y = rb.y + pos.y;
	v[3].z = rb.z + pos.z;

	//  color and texture coordinates
	v[0].diffuse = color;
	v[0].u		 = uv.x;
	v[0].v		 = uv.y + uv.h;
	v[0].u2		 = uv2.x;
	v[0].v2		 = uv2.y + uv2.h;

	v[1].diffuse = color;
	v[1].u		 = uv.x + uv.w;
	v[1].v		 = uv.y + uv.h;
	v[1].u2		 = uv2.x + uv2.w;
	v[1].v2		 = uv2.y + uv2.h;

	v[2].diffuse = color;
	v[2].u		 = uv.x;
	v[2].v		 = uv.y;
	v[2].u2		 = uv2.x;
	v[2].v2		 = uv2.y;

	v[3].diffuse = color;
	v[3].u		 = uv.x + uv.w;
	v[3].v		 = uv.y;
	v[3].u2		 = uv2.x + uv2.w;
	v[3].v2		 = uv2.y;

	pri.setNPri ( nVert/2 + 2 );
	pri.setNVert( nVert + 4 );
	return true;
} // AddQuad

bool PBillboardRenderer::FillGeometry( PEmitterInstance* pEmitter ) 
{ 
    if (!pEmitter->GetFlag( ifNeedDraw )) return false;

    PParticle* p = pEmitter->m_pParticle;
    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return false;

    Matrix4D alignTM = Matrix4D::identity;
    Vector3D vx, vy, vz;
    if (m_Alignment == baCamera)
    {
        vx = pCam->GetTransform().getV0();
        vy = pCam->GetTransform().getV1();
        vz = pCam->GetTransform().getV2();
    }
    else if (m_Alignment == baPlane)
    {
        vz = Vector3D::oZ;
        vx = pEmitter->m_WorldTM.getV0();
        vy.cross( vz, vx );
        vy.normalize();
        vx.cross( vy, vz );
    }
    else if (m_Alignment == baBeam)
    {
        vz = pCam->GetTransform().getV2();
        vx = pEmitter->m_WorldTM.getV0();
        vy.cross( vz, vx );
        vy.normalize();
        vz.cross( vx, vy );
    }

    alignTM.e00 = vx.x; alignTM.e01 = vx.y; alignTM.e02 = vx.z;
    alignTM.e10 = vy.x; alignTM.e11 = vy.y; alignTM.e12 = vy.z;
    alignTM.e20 = vz.x; alignTM.e21 = vz.y; alignTM.e22 = vz.z;

    if (!pEmitter->m_pEmitter->IsWorldSpace())
    {
        Vector3D tr( pEmitter->m_WorldTM.getTranslation() );
        Matrix4D tm;
        tm.scaling( pEmitter->m_WorldTM.getV0().norm() );
        tm.translate( tr );
        alignTM *= tm;
    }

    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    
    while (p)
    {
        if (m_RenderBin.getNVert() + 4 >= c_ParticleRenderBinSize) 
        {
            RenderGeometry( pEmitter );
        }
        if (p->GetFlag( pfDead ))       { p = p->m_pNext; continue; }
        if (p->GetFlag( pfJustBorn ))   { p = p->m_pNext; continue; }
        if (IsHeadMoveDir()) 
        {
            alignTM.getV1() = p->m_Velocity; 
            alignTM.getV1().normalize();
            alignTM.getV0().cross( alignTM.getV1(), alignTM.getV2() );
        }
        AddQuad<Vertex2t>( m_RenderBin, p->m_Position, 
                            p->m_Size.x*s, p->m_Size.y*s, 
                            m_RefPoint.x + 0.5f, m_RefPoint.y + 0.5f, 
                            p->m_Roll, p->m_Color, p->m_UV, p->m_UV2,
                            alignTM );
        p = p->m_pNext;
    }
    return true; 
} // PBillboardRenderer::FillGeometry

void PBillboardRenderer::RenderGeometry( PEmitterInstance* pEmitter ) 
{
    if (m_RenderBin.getNVert() == 0) return;
    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return;

    IRS->SetCurrentShader( m_ShaderID	);
    IRS->SetTexture		 ( m_TexID		);
    if (m_TexID2 != -1)
    {
        IRS->SetTexture		 ( m_TexID2, 1	);
    }
    else
    {
        static int wTex = IRS->GetTextureID( "white.tga" );
        IRS->SetTexture		 ( wTex, 1 );
    }

    DWORD tint = m_Tint & 0x00FFFFFF;
    tint |= (((DWORD)(pEmitter->m_Alpha*255.0f))<<24);
    IRS->SetTextureFactor( tint );

    if (IsZWrite()) IRS->SetZWriteEnable();
    if (!IsZTest()) IRS->SetZEnable( false );
    IRS->SetDitherEnable( IsDitherEnable() );
    IRS->ResetWorldMatrix();

    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    

    if (m_ZBias > 0)
    {
        if (!pCam) return;
        float shiftZ = -g_EMgr.GetZBiasMultiplier()*float( m_ZBias )*s;
        pCam->ShiftZ( -shiftZ );
        pCam->Render();	

        IRS->DrawPrim( m_RenderBin );

        pCam->ShiftZ( shiftZ );
        pCam->Render();	
    }
    else
    {
        IRS->DrawPrim( m_RenderBin );
    }
    m_RenderBin.setNVert( 0 );
    m_RenderBin.setNPri( 0 );
} // PBillboardRenderer::RenderGeometry

void PBillboardRenderer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PBillboardRenderer", this );
	pm.f( "Align", m_Alignment );
	pm.f( "RefX", m_RefPoint.x );
	pm.f( "RefY", m_RefPoint.y );
} // PBillboardRenderer::Expose

void PBillboardRenderer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << Enum2Byte( m_Alignment ) << m_RefPoint;
}

void PBillboardRenderer::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> Enum2Byte( m_Alignment ) >> m_RefPoint;
}

/*****************************************************************************/
/*	PConeRenderer implemetation
/*****************************************************************************/
PConeRenderer::PConeRenderer()
{
	m_HeightSegs	= 1;
	m_CircleSegs	= 16;
	m_TopRadius		= 0.2f;
	m_BotRadius		= 1.0f;
	m_Height		= 2.0f;	
    m_TopAlpha      = 1.0f;
    m_BotAlpha      = 1.0f;
	UpdateMesh();
} // PConeRenderer::PConeRenderer

static Primitive*	s_pMesh = NULL;
static int			s_Vert	= 0;
static int			s_Poly	= 0;
void PutVert( float x, float y, float z, float u, float v )
{
	if (!s_pMesh) return;
	Vertex2t& cv = *((Vertex2t*)s_pMesh->getVertexData() + s_Vert);
	cv.x = x;
	cv.y = y;
	cv.z = z;
	cv.u = u;
	cv.v = v;
	s_Vert++;
} // PutVert

void PutPoly( int a, int b, int c )
{
	if (!s_pMesh) return;
	WORD* idx = s_pMesh->getIndices() + s_Poly*3;
	idx[0] = a;
	idx[1] = b;
	idx[2] = c;
	s_Poly++;
} // PutPoly

void PConeRenderer::UpdateMesh()
{
	int nV = CreateCone( m_HeightSegs, m_CircleSegs, m_Height, m_TopRadius, m_BotRadius, NULL, NULL, scGetNVert );
	int nP = CreateCone( m_HeightSegs, m_CircleSegs, m_Height, m_TopRadius, m_BotRadius, NULL, NULL, scGetNPoly );
	m_Mesh.create( nV, nP*3, vf2Tex );
	s_pMesh = &m_Mesh;
	s_Vert  = 0;
	s_Poly  = 0;
	CreateCone( m_HeightSegs, m_CircleSegs, m_Height, m_TopRadius, m_BotRadius, PutVert, PutPoly, scCreate );
	m_Mesh.setNVert	( nV );
	m_Mesh.setNPri	( nP );
	m_Mesh.setNInd	( nP*3 );

	m_WorkMesh = m_Mesh;
} // PConeRenderer::UpdateMesh

void PConeRenderer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_HeightSegs << m_CircleSegs << m_TopRadius << m_BotRadius << 
            m_Height << m_TopAlpha << m_BotAlpha;	
} // PConeRenderer::Serialize

void PConeRenderer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_HeightSegs >> m_CircleSegs >> m_TopRadius >> m_BotRadius >> 
            m_Height >> m_TopAlpha >> m_BotAlpha;	
	UpdateMesh();
} // PConeRenderer::Unserialize
	
void PConeRenderer::RenderGeometry( PEmitterInstance* pEmitter )
{
	if (!pEmitter->GetFlag( ifNeedDraw )) return;

	PParticle* p = pEmitter->m_pParticle;
	m_WorkMesh.setShader ( m_ShaderID );
	m_WorkMesh.setTexture( m_TexID );
    if (m_TexID2 != -1)
    {
        IRS->SetTexture		 ( m_TexID2, 1	);
    }
    else
    {
        static int wTex = IRS->GetTextureID( "white.tga" );
        IRS->SetTexture		 ( wTex, 1 );
    }
	
    Matrix4D baseTM = Matrix4D::identity;
    if (!pEmitter->m_pEmitter->IsWorldSpace())
    {
        baseTM = pEmitter->m_WorldTM;
    }

    IRS->SetTextureFactor( m_Tint );
    while (p)
    {
        if (p->m_Age == 0.0f) { p = p->m_pNext; continue; }
        Matrix4D tm = p->GetTransform();
        tm *= baseTM;
        IRS->SetWorldMatrix( tm );

		int nV = m_Mesh.getNVert();
		Vertex2t* vs = (Vertex2t*)m_Mesh.getVertexData();
		Vertex2t* vd = (Vertex2t*)m_WorkMesh.getVertexData();
		for (int i = 0; i < nV; i++)
		{
            float alpha = float((p->m_Color&0xFF000000)>>24)/255.0f;
            float dAlpha = m_BotAlpha + (m_TopAlpha - m_BotAlpha)*vs->z/m_Height;
            alpha *= dAlpha;
            alpha *= pEmitter->m_Alpha;
            clamp( alpha, 0.0f, 1.0f );
            alpha *= 255.0f;
            DWORD ba = ((DWORD)alpha)<<24;
			vd->diffuse = (p->m_Color&0x00FFFFFF)|ba;
			vd->u  = vs->u*p->m_UV.w  + p->m_UV.x;
			vd->v  = vs->v*p->m_UV.h  + p->m_UV.y;
			vd->u2 = vs->u*p->m_UV2.w + p->m_UV2.x;
			vd->v2 = vs->v*p->m_UV2.h + p->m_UV2.y;
			vd++; vs++;
		}

		IRS->Draw( m_WorkMesh );
		p = p->m_pNext;
	}
} // PConeRenderer::RenderGeometry

void PConeRenderer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PConeRenderer", this );
	pm.p( "HeightSegs",		GetNHSegs,      SetNHSegs       );
	pm.p( "CircleSegs",		GetNCSegs,      SetNCSegs       );
	pm.p( "TopRadius",		GetTopR,        SetTopR         );
	pm.p( "BottomRadius",	GetBotR,        SetBotR         );
	pm.p( "Height",			GetConeHeight,  SetConeHeight   );
    pm.p( "TopAlpha",		GetTopAlpha,    SetTopAlpha     );
    pm.p( "BotAlpha",	    GetBotAlpha,    SetBotAlpha     );
} // PConeRenderer::Expose

/*****************************************************************************/
/*	PSphereRenderer implemetation
/*****************************************************************************/
PSphereRenderer::PSphereRenderer()
{
	m_bHemisphere	= true;
	m_HeightSegs	= 6.0f;
	m_CircleSegs	= 16.0f;
	m_Radius		= 1.0f;	
    m_PolePhi       = 0.0f;  
    m_PoleTheta     = 0.0f;
	UpdateMesh();
} // PSphereRenderer::PSphereRenderer

void PSphereRenderer::UpdateMesh()
{
	int nV = 0;
	int nP = 0;
	if (m_bHemisphere)
	{
		nV = CreateHemisphere( m_HeightSegs, m_CircleSegs, m_Radius, 
                                NULL, NULL, scGetNVert, m_PolePhi, m_PoleTheta );
		nP = CreateHemisphere( m_HeightSegs, m_CircleSegs, m_Radius, 
                                NULL, NULL, scGetNPoly, m_PolePhi, m_PoleTheta );
	}
	else
	{
		nV = CreateSphere( m_HeightSegs, m_CircleSegs, m_Radius, 
                                NULL, NULL, scGetNVert, m_PolePhi, m_PoleTheta );
		nP = CreateSphere( m_HeightSegs, m_CircleSegs, m_Radius, 
                                NULL, NULL, scGetNPoly, m_PolePhi, m_PoleTheta );
	}
	
	m_Mesh.create( nV, nP*3, vf2Tex );
	s_pMesh = &m_Mesh;
	s_Vert  = 0;
	s_Poly  = 0;

	if (m_bHemisphere)
	{
		CreateHemisphere( m_HeightSegs, m_CircleSegs, m_Radius, 
                            PutVert, PutPoly, scCreate, m_PolePhi, m_PoleTheta );
	}
	else
	{
		CreateSphere( m_HeightSegs, m_CircleSegs, m_Radius, 
                            PutVert, PutPoly, scCreate, m_PolePhi, m_PoleTheta );
	}
	m_Mesh.setNVert	( nV );
	m_Mesh.setNPri	( nP );
	m_Mesh.setNInd	( nP*3 );

	m_WorkMesh = m_Mesh;
} // PSphereRenderer::UpdateMesh

void PSphereRenderer::RenderGeometry( PEmitterInstance* pEmitter )
{
	if (!pEmitter->GetFlag( ifNeedDraw )) return;

	PParticle* p = pEmitter->m_pParticle;
	m_WorkMesh.setShader ( m_ShaderID );
	m_WorkMesh.setTexture( m_TexID );
    if (m_TexID2 != -1)
    {
        IRS->SetTexture		 ( m_TexID2, 1	);
    }
    else
    {
        static int wTex = IRS->GetTextureID( "white.tga" );
        IRS->SetTexture		 ( wTex, 1 );
    }

    Matrix4D baseTM = Matrix4D::identity;
    if (!pEmitter->m_pEmitter->IsWorldSpace())
    {
        baseTM = pEmitter->m_WorldTM;
    }
    
    IRS->SetTextureFactor( m_Tint );
	while (p)
	{
		if (p->m_Age == 0.0f) { p = p->m_pNext; continue; }
		Matrix4D tm = p->GetTransform();
        tm *= baseTM;
        IRS->SetWorldMatrix( tm );

		int nV = m_Mesh.getNVert();
		Vertex2t* vs = (Vertex2t*)m_Mesh.getVertexData();
		Vertex2t* vd = (Vertex2t*)m_WorkMesh.getVertexData();
		for (int i = 0; i < nV; i++)
		{
			vd->diffuse = p->m_Color;
			vd->u  = vs->u*p->m_UV.w  + p->m_UV.x;
			vd->v  = vs->v*p->m_UV.h  + p->m_UV.y;
			vd->u2 = vs->u*p->m_UV2.w + p->m_UV2.x;
			vd->v2 = vs->v*p->m_UV2.h + p->m_UV2.y;
			vd++; vs++;
		}
        
		IRS->Draw( m_WorkMesh );
        p = p->m_pNext;
	}
} // PSphereRenderer::RenderGeometry

void PSphereRenderer::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_bHemisphere << m_HeightSegs << m_CircleSegs << m_Radius << m_PolePhi << m_PoleTheta;										  
}																  
																  
void PSphereRenderer::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_bHemisphere >> m_HeightSegs >> m_CircleSegs >> m_Radius >> m_PolePhi >> m_PoleTheta;
	UpdateMesh();
} // PSphereRenderer::Unserialize

void PSphereRenderer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PSphereRenderer", this );
	pm.p( "Hemisphere", GetHemisphere, SetHemisphere );
	pm.p( "HeightSegs",	GetNHSegs, SetNHSegs );
	pm.p( "CircleSegs",	GetNCSegs, SetNCSegs );
	pm.p( "TopRadius",	GetRadius, SetRadius );
    pm.p( "PolePhi",	GetPolePhi, SetPolePhi );
    pm.p( "PoleTheta",	GetPoleTheta, SetPoleTheta );
} // PSphereRenderer::Expose

/*****************************************************************************/
/*	PChainRenderer implemetation
/*****************************************************************************/
PChainRenderer::PChainRenderer()
{
    SetBindHead();
    m_MaxLength = 100.0f;
}

template <class TVert>
bool AddQuad(	Primitive& pri, const Vector3D& a, const Vector3D& b, 
				const Vector3D& c, const Vector3D& d, 
				DWORD ca, DWORD cb, DWORD cc, DWORD cd, 
				const Rct& uv, const Rct& uv2 )
{
	int nVert = pri.getNVert();
	if (nVert == pri.getMaxVert()) return false;
	TVert* v = ((TVert*)pri.getVertexData() + nVert);

	v[0] = a;
	v[1] = b;
	v[2] = c;
	v[3] = d;

	//  color and texture coordinates
	v[0].diffuse = ca;
	v[0].u		 = uv.x;
	v[0].v		 = uv.y + uv.h;
	v[0].u2		 = uv2.x;
	v[0].v2		 = uv2.y + uv2.h;

	v[1].diffuse = cb;
	v[1].u		 = uv.x + uv.w;
	v[1].v		 = uv.y + uv.h;
	v[1].u2		 = uv2.x + uv2.w;
	v[1].v2		 = uv2.y + uv2.h;

	v[2].diffuse = cc;
	v[2].u		 = uv.x;
	v[2].v		 = uv.y;
	v[2].u2		 = uv2.x;
	v[2].v2		 = uv2.y;

	v[3].diffuse = cd;
	v[3].u		 = uv.x + uv.w;
	v[3].v		 = uv.y;
	v[3].u2		 = uv2.x + uv2.w;
	v[3].v2		 = uv2.y;

	pri.setNPri ( nVert/2 + 2 );
	pri.setNVert( nVert + 4 );
	return true;
} // AddQuad

void PChainRenderer::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_Flags;
} // PChainRenderer::Serialize

void PChainRenderer::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_Flags;
} // PChainRenderer::Unserialize

bool PChainRenderer::FillGeometry( PEmitterInstance* pEmitter )
{
    if (!pEmitter->GetFlag( ifNeedDraw )) return false;

    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return false;
    Matrix3D camTM = pCam->GetTransform();
    const Vector3D cDir = IsAlignGround() ? Vector3D::oZ : pCam->GetDir();

    PParticle* p = pEmitter->m_pParticle;
    while (p && (p->m_Age == 0.0f || p->GetFlag( pfDead ))) p = p->m_pNext;
    if (!p) return false;
    PParticle* pn = p->m_pNext;
    if (!pn) return false;

    Vector3D a, b, c, d;
    Vector3D right;
    Vector3D forward;

    if (!IsBindHead())
    {
        a = p->m_Position;
        b = p->m_Position;
    }
    else
    {
        a = pEmitter->m_WorldTM.getTranslation();
        b = a;
    }

    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    
    Matrix4D tm = Matrix4D::identity;
    if (!pEmitter->m_pEmitter->IsWorldSpace())
    {
        Vector3D tr( pEmitter->m_WorldTM.getTranslation() );
        tm.scaling( pEmitter->m_WorldTM.getV0().norm() );
        tm.translate( tr );
        IRS->SetWorldMatrix( tm );
    }

    bool bFirst = true;
    while (p)
    {
        if (m_RenderBin.getNVert() + 4 >= c_ParticleRenderBinSize) 
        {
            RenderGeometry( pEmitter );
        }

        pn = p->m_pNext;
        if (pn &&   p->m_Age  > 0.0f && 
            pn->m_Age > 0.0f &&
            !p->GetFlag ( pfDead ) && 
            !pn->GetFlag( pfDead ))
        {
            forward.sub( pn->m_Position, p->m_Position );
            float len = forward.normalize();
            if (len >= m_MaxLength || len < c_Epsilon) 
            {
                p = pn;
                continue;
            }
            right.cross( cDir, forward );
            //  correct it
            c = pn->m_Position;
            d = pn->m_Position;
            c.addWeighted( right, -pn->m_Size.x*0.5f*s );
            d.addWeighted( right,  pn->m_Size.x*0.5f*s );
            if (bFirst)
            {
                a.addWeighted( right, -p->m_Size.x*0.5f*s );
                b.addWeighted( right,  p->m_Size.x*0.5f*s );
                bFirst = false;
            }

            Vector3D va( a ), vb( b ), vc( c ), vd( d );
            tm.transformPt( va );
            tm.transformPt( vb );
            tm.transformPt( vc );
            tm.transformPt( vd );

            AddQuad<Vertex2t>( m_RenderBin, va, vb, vc, vd, 
                p->m_Color, p->m_Color, pn->m_Color, pn->m_Color,
                p->m_UV, p->m_UV2 );
            a = c; b = d;
        }
        p = pn;
    }
    return true;
} // PChainRenderer::FillGeometry

void PChainRenderer::RenderGeometry( PEmitterInstance* pEmitter )
{
    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (!pCam) return;

    if (m_RenderBin.getNVert() == 0) return;
    m_RenderBin.setShader ( m_ShaderID );
    m_RenderBin.setTexture( m_TexID );

    DWORD tint = m_Tint & 0x00FFFFFF;
    tint |= (((DWORD)(pEmitter->m_Alpha*255.0f))<<24);
    IRS->SetTextureFactor( tint );

    IRS->ResetWorldMatrix();
    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    
    if (m_ZBias > 0)
    {
        if (!pCam) return;
        float shiftZ = -g_EMgr.GetZBiasMultiplier()*float( m_ZBias )*s;
        pCam->ShiftZ( -shiftZ );
        pCam->Render();	
        IRS->Draw( m_RenderBin );
        pCam->ShiftZ( shiftZ );
        pCam->Render();	
    }
    else
    {
        IRS->Draw( m_RenderBin );
    }
    m_RenderBin.setNVert( 0 );
    m_RenderBin.setNPri( 0 );
} // PChainRenderer::RenderGeometry

void PChainRenderer::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PChainRenderer", this );
    pm.p( "BindHead", IsBindHead, SetBindHead );
    pm.p( "AlignGround", IsAlignGround, SetAlignGround );
    pm.f( "MaxLength", m_MaxLength );
} // PChainRenderer::Expose

/*****************************************************************************/
/*	PMouseBind implemetation
/*****************************************************************************/
PMouseBind::PMouseBind()
{
	m_Depth = 0.5f;
}

void PMouseBind::Process( PEmitterInstance* pEmitter )
{
	if (!pEmitter->GetFlag( ifNeedDraw )) return;

	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (!pCam) return;
	
	POINT pt;
	GetCursorPos( &pt );
	float mX = pt.x;
	float mY = pt.y;
	
	Vector4D pos( mX, mY, m_Depth, 1.0f );
	pCam->ScreenToWorldSpace( pos );

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p->m_Position = pos;
		p = p->m_pNext;
	}
} // PMouseBind::Process

void PMouseBind::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PMouseBind", this );
	pm.f( "Depth", m_Depth );
}

void PMouseBind::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Depth;										  
}																  

void PMouseBind::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Depth;
} // PMouseBind::Unserialize

/*****************************************************************************/
/*	PTerrainDecal implemetation
/*****************************************************************************/
PTerrainDecal::PTerrainDecal()
{
}

void PTerrainDecal::RenderGeometry( PEmitterInstance* pEmitter )
{
	if (!pEmitter->GetFlag( ifNeedDraw )) return;

	PParticle* p = pEmitter->m_pParticle;

	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (!pCam) return;
	
	m_RenderBin.setNVert( 0 );
	m_RenderBin.setNPri ( 0 );
    
    Matrix4D wtm = Matrix4D::identity;
    if (!pEmitter->m_pEmitter->IsWorldSpace())
    {
        Vector3D tr( pEmitter->m_WorldTM.getTranslation() );
        wtm.scaling( pEmitter->m_WorldTM.getV0().norm() );
        wtm.translate( tr );
    }

    float s = pEmitter->m_pEmitter->IsWorldSpace() ? pEmitter->m_WorldTM.getV0().norm() : 1.0f;    
	while (p)
	{
        if (p->GetFlag( pfDead ))       { p = p->m_pNext; continue; }
        if (p->GetFlag( pfJustBorn ))   { p = p->m_pNext; continue; }
		
		Vector3D pos( p->m_Position );
        Vector3D tpos( pos );
        wtm.transformPt( tpos );
		tpos.z = ITerra->GetH( tpos.x, tpos.y );

		Vector3D vz = ITerra->GetNormal( tpos.x, tpos.y );
        Vector3D vx = pEmitter->m_WorldTM.getV0();
        vx.normalize();
        Vector3D vy; vy.cross( vx, vz ); 
        vy.normalize();
        vx.cross( vy, vz ); 

        Matrix4D tm( vx, vy, vz );
        //tm.translate( pos );
        AddQuad<Vertex2t>( m_RenderBin, tpos, p->m_Size.x*s, p->m_Size.y*s, 0.5f, 0.5f,
                            p->m_Roll, p->m_Color, p->m_UV, p->m_UV2, tm );
        p = p->m_pNext;
	}

    wtm.setTranslation( Vector3D::null );
	
    IRS->SetCurrentShader( m_ShaderID	);
    IRS->SetTexture		 ( m_TexID		);
    if (m_TexID2 != -1)
    {
        IRS->SetTexture		 ( m_TexID2, 1	);
    }
    else
    {
        static int wTex = IRS->GetTextureID( "white.tga" );
        IRS->SetTexture		 ( wTex, 1 );
    }

    DWORD tint = m_Tint & 0x00FFFFFF;
    tint |= (((DWORD)(pEmitter->m_Alpha*255.0f))<<24);
    IRS->SetTextureFactor( tint );

    if (IsZWrite()) IRS->SetZWriteEnable();
    if (!IsZTest()) IRS->SetZEnable( false );
    IRS->SetDitherEnable( IsDitherEnable() );
    
    if (m_ZBias > 0)
    {
        if (!pCam) return;
        float shiftZ = -g_EMgr.GetZBiasMultiplier()*float( m_ZBias )*s;
		pCam->ShiftZ( -shiftZ );
        pCam->Render();	
        IRS->DrawPrim( m_RenderBin );
        pCam->ShiftZ( shiftZ );
        pCam->Render();	
    }
    else
    {
        IRS->DrawPrim( m_RenderBin );
    }
} // PTerrainDecal::RenderGeometry

void PTerrainDecal::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PTerrainDecal", this );
}

/*****************************************************************************/
/*	PWaterDecal implemetation
/*****************************************************************************/
PWaterDecal::PWaterDecal()
{
}

void PWaterDecal::Process( PEmitterInstance* pEmitter )
{
	if (!pEmitter->GetFlag( ifNeedDraw )) return;

	PParticle* p = pEmitter->m_pParticle;
	Matrix3D camTM = Matrix3D::identity;

	m_RenderBin.setNVert( 0 );
	m_RenderBin.setNPri ( 0 );
	while (p)
	{
		if (p->m_Age > 0.0f || p->GetFlag( pfImmortal ))
		{
			AddQuad<Vertex2t>(	m_RenderBin, p->m_Position, 
								p->m_Size.x, p->m_Size.y, 0.5f, 0.5f,
								p->m_Roll,
								p->m_Color, 
								p->m_UV, 
								p->m_UV2,
								camTM );
		}
		p = p->m_pNext;
	}

	m_RenderBin.setShader ( m_ShaderID );
	m_RenderBin.setTexture( m_TexID );
    if (m_TexID2 != -1)
    {
        IRS->SetTexture		 ( m_TexID2, 1	);
    }
    else
    {
        static int wTex = IRS->GetTextureID( "white.tga" );
        IRS->SetTexture		 ( wTex, 1 );
    }
	IRS->SetTextureFactor( m_Tint );

	IRS->Draw( m_RenderBin );
} // PWaterDecal::Process

void PWaterDecal::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PWaterDecal", this );
}

/*****************************************************************************/
/*	PColorInit implementation
/*****************************************************************************/
PColorInit::PColorInit()
{
	m_AvgColor	= 0xFFFFFFFF;	
	m_R = m_G = m_B = m_A = 255;
	m_RedV		= 0;		
	m_GreenV	= 0;	
	m_BlueV		= 0;	
	m_AlphaV	= 0;	
}

void PColorInit::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PColorInit", this );
	pm.p( "Color", GetAvgColor, SetAvgColor, "color" );
	pm.f( "dRed",	m_RedV		);
	pm.f( "dGreen", m_GreenV	);
	pm.f( "dBlue",	m_BlueV		);
	pm.f( "dAlpha", m_AlphaV	);
}

void PColorInit::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_AvgColor << m_RedV << m_GreenV << m_BlueV << m_AlphaV;
}

void PColorInit::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_AvgColor >> m_RedV >> m_GreenV >> m_BlueV >> m_AlphaV;
	SetAvgColor( m_AvgColor );
}

void PColorInit::InitParticle( PParticle& p )
{
	int r = rndValue( -m_RedV,		m_RedV	 ) + m_R;
	int g = rndValue( -m_GreenV,	m_GreenV ) + m_G;
	int b = rndValue( -m_BlueV,		m_BlueV  ) + m_B;
	int a = rndValue( -m_AlphaV,	m_AlphaV ) + m_A;
	clamp( r, 0, 255 );
	clamp( g, 0, 255 );
	clamp( b, 0, 255 );
	clamp( a, 0, 255 );

	p.m_Color = (a << 24)|(r << 16)|(g << 8)|(b);
} // PColorInit::InitParticle

void PColorInit::SetAvgColor( DWORD val )
{
	m_AvgColor = val;
	m_A = (val & 0xFF000000)>>24;
	m_R = (val & 0x00FF0000)>>16;
	m_G = (val & 0x0000FF00)>>8;
	m_B = (val & 0x000000FF);
}

/*****************************************************************************/
/*	PColorRampInit implemetation
/*****************************************************************************/
PColorRampInit::PColorRampInit()
{
}

void PColorRampInit::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PColorRampInit", this );
    pm.p( "NumKeys", GetNKeys );
	pm.p( "Ramp", GetColorRamp, SetColorRamp, "color_ramp" );
} // PColorRampInit::Expose

void PColorRampInit::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	m_Ramp.Serialize( os );
}

void PColorRampInit::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	m_Ramp.Unserialize( is );
} // PColorRampInit::Unserialize

void PColorRampInit::InitParticle( PParticle& p )
{
	p.m_Color = m_Ramp.GetColor( rndValuef() );
}

/*****************************************************************************/
/*	PSizeInit implementation
/*****************************************************************************/
PSizeInit::PSizeInit()
{
	m_Size	= Vector3D( 100.0f, 100.0f, 100.0f );		
	m_SizeV	= 0.0f;
}

void PSizeInit::InitParticle( PParticle& p )
{
	p.m_Size = m_Size;
	float dsize = rndValuef( -m_SizeV, m_SizeV );
	p.m_Size.x += dsize;
	p.m_Size.y += dsize;
	p.m_Size.z += dsize;
}

void PSizeInit::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PSizeInit", this );
	pm.f( "SizeX",	m_Size.x );
	pm.f( "SizeY",	m_Size.y );
	pm.f( "SizeZ",	m_Size.z );
	pm.f( "dSize",	m_SizeV );
} // PSizeInit::Expose

void PSizeInit::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Size << m_SizeV;
}

void PSizeInit::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Size >> m_SizeV;
}

/*****************************************************************************/
/*	PFrameInit implementation
/*****************************************************************************/
PFrameInit::PFrameInit()
{
	m_NRows = 4;
	m_NCols = 4;
	m_Flags = 0;
}

void PFrameInit::InitParticle( PParticle& p )
{
	int nF = m_NRows*m_NCols;
	float w = 1.0f / m_NCols;
	float h = 1.0f / m_NRows;
	
	int frame = 0;
    
    if (IsSequentialInit())
    {
        PParticle* pNext = p.m_pNext;
        if (pNext)
        {
            
        }
    } else frame = rndValue( 0, nF - 1 );

 	int row = frame / m_NCols;
	int col = frame % m_NCols;
    p.m_Frame = frame;

	if (IsSecondChannel())
	{
		p.m_UV2.x = float( col )*w;
		p.m_UV2.y = float( row )*h;
		p.m_UV2.w = w;
		p.m_UV2.h = h;
	}
	else
	{
		p.m_UV.x = float( col )*w;
		p.m_UV.y = float( row )*h;
		p.m_UV.w = w;
		p.m_UV.h = h;
	}
} // PFrameInit::InitParticle

void PFrameInit::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PFrameInit", this );
	pm.f( "Columns",	    m_NCols );
	pm.f( "Rows",		    m_NRows );
	pm.p( "SecondChannel",  IsSecondChannel, SetIsSecondChannel );
    pm.p( "SequentialInit", IsSequentialInit, SetIsSequentialInit );
} // PFrameInit::Expose

void PFrameInit::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_NCols << m_NRows << m_Flags;
}

void PFrameInit::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_NCols >> m_NRows >> m_Flags;
}

/*****************************************************************************/
/*	PAttract implemetation
/*****************************************************************************/
PAttract::PAttract()
{
    m_Pos           = Vector3D::null;   
    m_Magnitude     = 1.0f;
    m_Radius        = 20.0f;  
    m_FadeMode      = afmLinear;
} // PAttract::PAttract

void PAttract::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "PAttract", this );
    pm.f( "PosX",       m_Pos.x     );
    pm.f( "PosY",       m_Pos.y     );
    pm.f( "PosZ",       m_Pos.z     );
    pm.f( "Magnitude",  m_Magnitude );
    pm.f( "Radius",     m_Radius    );
    pm.f( "FadeMode",   m_FadeMode  );
} // PAttract::Expose

void PAttract::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_Pos << m_Magnitude << m_Radius << Enum2Byte( m_FadeMode );
}

void PAttract::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_Pos >> m_Magnitude >> m_Radius >> Enum2Byte( m_FadeMode );
}

void PAttract::Render()
{
    if (!DoDrawGizmo()) return;
    Vector3D pos = m_Pos;
    DrawStar( Sphere( pos, 5.0f ), 0xFFCCFF33, 0xFFCCFF33 );
    DrawSphere( Sphere( pos, m_Radius ), 0x33CCFF33, 0x77CCFF33, 16 );
} // PAttract::Render

void PAttract::Process( PEmitterInstance* pEmitter )
{
    if (m_Radius < c_SmallEpsilon || fabs( m_Magnitude ) < c_SmallEpsilon) return;
    float dt = pEmitter->GetTimeDelta();   	
   	PParticle* p = pEmitter->m_pParticle;
   	while (p)
   	{
        Vector3D dir;
        dir.sub( m_Pos, p->m_Position );
        float norm2 = dir.norm2();
        if (norm2 > m_Radius*m_Radius) 
        //  outside influence
        { 
            p = p->m_pNext; 
            continue; 
        }
        float dist = dir.normalize();
        dir *= dt;
        if (m_FadeMode == afmConstant)
        {
            dir *= m_Magnitude;
        }
        else if (m_FadeMode == afmLinear)
        {
            dir *= m_Magnitude*(1.0f - dist/m_Radius);
        }
        else if (m_FadeMode == afmQuadratic)
        {
            const float c_QBend = 0.1f;
            dist = m_Radius - dist;
            dir *= dist*dist*m_Magnitude/(m_Radius*m_Radius);
        }
   		p->m_Velocity += dir;
   		p = p->m_pNext;
   	} 
} // PAttract::Process

/*****************************************************************************/
/*	PForce implemetation
/*****************************************************************************/
PForce::PForce()
{
	m_Force.x = 0.0f;
	m_Force.y = 0.0f;
	m_Force.z = -50.0f;
}

void PForce::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PForce", this );
	pm.f( "ForceX", m_Force.x );
	pm.f( "ForceY", m_Force.y );
	pm.f( "ForceZ", m_Force.z );
} // PForce::Expose

void PForce::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Force;
} // PForce::Serialize

void PForce::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Force;
} // PForce::Unserialize

void PForce::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	Vector3D dv = m_Force;
	dv *= dt;
	
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p->m_Velocity += dv;
		p = p->m_pNext;
	}
} // PForce::Process

/*****************************************************************************/
/*	PWind implemetation
/*****************************************************************************/
PWind::PWind()
{
	m_Magnitude.x	= 50.0f;
	m_Magnitude.y	= 0.0f;
	m_Magnitude.z	= 0.0f;
	m_Frequency		= 1.0f;
	m_Shift			= 0.0f;
}

void PWind::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PWind", this );
	pm.f( "MagX",		m_Magnitude.x	);
	pm.f( "MagY",		m_Magnitude.y	);
	pm.f( "MagZ",		m_Magnitude.z	);
	pm.f( "Frequency",	m_Frequency		);
	pm.f( "Shift",		m_Shift			);
} // PWind::Expose

void PWind::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Magnitude << m_Frequency << m_Shift;
} // PWind::Serialize

void PWind::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Magnitude >> m_Frequency >> m_Shift;
} // PWind::Unserialize

void PWind::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	Vector3D dv = m_Magnitude;
	float ampl = PerlinNoise( m_Shift*m_Frequency, 
								(pEmitter->m_CurTime - pEmitter->m_StartTime)*m_Frequency );
	dv *= dt*ampl;
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p->m_Velocity += dv;
		p = p->m_pNext;
	}
} // PWind::Process


/*****************************************************************************/
/*	PTorque implemetation
/*****************************************************************************/
PTorque::PTorque()
{
	m_Torque = Vector3D::oZ;
}

void PTorque::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PTorque", this );
	pm.f( "TorqueX", m_Torque.x );
	pm.f( "TorqueY", m_Torque.y );
	pm.f( "TorqueZ", m_Torque.z );
} // PTorque::Expose

void PTorque::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Torque;
} // PTorque::Serialize

void PTorque::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> m_Torque;
} // PTorque::Unserialize

void PTorque::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	Vector3D dv = m_Torque;
	dv *= dt;

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p->m_AngVelocity += dv;
		p = p->m_pNext;
	}
} // PTorque::Process


/*****************************************************************************/
/*	PFollow implemetation
/*****************************************************************************/
PFollow::PFollow()
{
	m_Magnitude = 10.0f;
	m_bFollowOlder = true;
}

void PFollow::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PFollow", this );
	pm.f( "Magnitude", m_Magnitude );
	pm.f( "FollowOlder", m_bFollowOlder );
} // PFollow::Expose

void PFollow::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Magnitude << m_bFollowOlder;
}

void PFollow::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Magnitude >> m_bFollowOlder;
} // PFollow::Unserialize

void PFollow::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	PParticle* p = pEmitter->m_pParticle;

	if (m_bFollowOlder)
	{
		while (p)
		{
			Vector3D v;
			PParticle* pNext = p->m_pNext;
			if (pNext)
			{
				v.sub( pNext->m_Position, p->m_Position );
				v.normalize();
				v *= m_Magnitude;
				p->m_Velocity += v;
			}
			p = p->m_pNext;
		}
	}
	else
	{
		while (p)
		{
			Vector3D v;
			PParticle* pPrev = p->m_pPrev;
			if (pPrev)
			{
				v.sub( pPrev->m_Position, p->m_Position );
				v.normalize();
				v *= m_Magnitude;
				p->m_Velocity += v;
			}
			p = p->m_pNext;
		}
	}
} // PFollow::Process

/*****************************************************************************/
/*	PAlphaFade implemetation
/*****************************************************************************/
PAlphaFade::PAlphaFade()
{
	m_bAbsolute  = true;
	m_StartAlpha = 1.0f;
	m_EndAlpha	 = 0.0f;
}

void PAlphaFade::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PAlphaFade", this );
	pm.f( "StartAlpha", m_StartAlpha );
	pm.f( "EndAlpha", m_EndAlpha );
	pm.f( "Absolute", m_bAbsolute );
} // PAlphaFade::Expose

void PAlphaFade::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_StartAlpha << m_EndAlpha << m_bAbsolute;
}

void PAlphaFade::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_StartAlpha >> m_EndAlpha >> m_bAbsolute;
} // PAlphaFade::Unserialize

void PAlphaFade::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		float alpha = m_StartAlpha + (m_EndAlpha - m_StartAlpha)*p->m_Age/p->m_TimeToLive;
		clamp( alpha, 0.0f, 1.0f );
		if (!m_bAbsolute)
		{
			alpha *= (p->m_Color&0xFF000000)>>24;
		}
		else
		{
			alpha *= 255.0f;
		}
		p->m_Color &= 0x00FFFFFF;
		p->m_Color |= (DWORD( alpha ))<<24;
		p = p->m_pNext;
	}
} // PAlphaFade::Process


/*****************************************************************************/
/*	PAVelRamp implemetation
/*****************************************************************************/
PAVelRamp::PAVelRamp()
{
	m_MinVel  = -2.0f;
	m_MaxVel  = 2.0f;
	m_Repeat  = 1;
	m_Axis	  = aXYZ; 
}

void PAVelRamp::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PAVelRamp", this );
	pm.f( "MinVel",		m_MinVel );
	pm.f( "MaxVel",		m_MaxVel );
	pm.f( "Axis",		m_Axis );
	pm.f( "Repeat",		m_Repeat );
    pm.p( "NumKeys", GetNKeys );
	pm.p( "Ramp", GetVelRamp, SetVelRamp, "alpha_ramp" );
} // PAVelRamp::Expose

void PAVelRamp::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	m_Ramp.Serialize( os );
	os << m_MinVel << m_MaxVel << m_Repeat << Enum2Byte( m_Axis );
}

void PAVelRamp::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	m_Ramp.Unserialize( is );
	is >> m_MinVel >> m_MaxVel >> m_Repeat >> Enum2Byte( m_Axis );
} // PAVelRamp::Unserialize

void PAVelRamp::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		float t = m_Repeat * p->m_Age / p->m_TimeToLive;
		t -= floorf( t );
		float av = m_MinVel +  m_Ramp.GetAlpha( t )*(m_MaxVel - m_MinVel);
		if (m_Axis&1) p->m_AngVelocity.x = av;
		if (m_Axis&2) p->m_AngVelocity.y = av;
		if (m_Axis&4) p->m_AngVelocity.z = av;
		p = p->m_pNext;
	}
} // PAVelRamp::Process

/*****************************************************************************/
/*	PVelRamp implemetation
/*****************************************************************************/
PVelRamp::PVelRamp()
{
    m_MinVel  = -2.0f;
    m_MaxVel  = 2.0f;
    m_Repeat  = 1;
}

void PVelRamp::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "PVelRamp", this );
    pm.f( "MinVel",		m_MinVel );
    pm.f( "MaxVel",		m_MaxVel );
    pm.f( "Repeat",		m_Repeat );
    pm.p( "NumKeys", GetNKeys );
    pm.p( "Ramp", GetVelRamp, SetVelRamp, "alpha_ramp" );
} // PVelRamp::Expose

void PVelRamp::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    m_Ramp.Serialize( os );
    os << m_MinVel << m_MaxVel << m_Repeat;
}

void PVelRamp::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    m_Ramp.Unserialize( is );
    is >> m_MinVel >> m_MaxVel >> m_Repeat;
} // PVelRamp::Unserialize

void PVelRamp::Process( PEmitterInstance* pEmitter )
{
    float dt = pEmitter->GetTimeDelta();

    PParticle* p = pEmitter->m_pParticle;
    while (p)
    {
        float t = m_Repeat * p->m_Age / p->m_TimeToLive;
        t -= floorf( t );
        float v = m_MinVel +  m_Ramp.GetAlpha( t )*(m_MaxVel - m_MinVel);
        p->m_Velocity.normalize();
        p->m_Velocity *= v;
        p = p->m_pNext;
    }
} // PVelRamp::Process

/*****************************************************************************/
/*	PCoastBreak implemetation
/*****************************************************************************/
PCoastBreak::PCoastBreak()
{
    m_Magnitude     = 0.1f;
    m_WaterLevel    = 50.0f;
}

void PCoastBreak::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "PCoastBreak", this );
    pm.f( "Magnitude", m_Magnitude );
    pm.f( "WaterLevel", m_WaterLevel );
} // PCoastBreak::Expose

void PCoastBreak::Serialize( OutStream& os ) const
{
    Parent::Serialize( os );
    os << m_Magnitude << m_WaterLevel;
}

void PCoastBreak::Unserialize( InStream& is  )
{
    Parent::Unserialize( is );
    is >> m_Magnitude >> m_WaterLevel;
} // PCoastBreak::Unserialize

void PCoastBreak::Process( PEmitterInstance* pEmitter )
{
    if (!ITerra) return;
    float dt = pEmitter->GetTimeDelta();

    PParticle* p = pEmitter->m_pParticle;
    while (p)
    {
        float H = ITerra->GetH( p->m_Position.x, p->m_Position.y );
        if (H > m_WaterLevel)
        {
            p->m_Velocity *= m_Magnitude;
            p->m_Frame = 1;
            Trigger( pEmitter, *p );
        }
        p = p->m_pNext;
    }
} // PCoastBreak::Process

void PCoastBreak::Trigger( const PEmitterInstance* pEmitter, PParticle& p )
{
    
} // PCoastBreak::Trigger

/*****************************************************************************/
/*	PAlphaRamp implemetation
/*****************************************************************************/
PAlphaRamp::PAlphaRamp()
{
	m_NRepeats = 1;
}

void PAlphaRamp::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PAlphaRamp", this );
	pm.f( "Repeat", m_NRepeats );
    pm.p( "NumKeys", GetNKeys );
	pm.p( "Ramp", GetAlphaRamp, SetAlphaRamp, "alpha_ramp" );
} // PAlphaRamp::Expose

void PAlphaRamp::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	m_Ramp.Serialize( os );
	os << m_NRepeats;
}

void PAlphaRamp::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	m_Ramp.Unserialize( is );
	is >> m_NRepeats;
} // PAlphaRamp::Unserialize

void PAlphaRamp::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		float t = m_NRepeats * p->m_Age / p->m_TimeToLive;
		t -= floorf( t );
		float alpha = m_Ramp.GetAlpha( t )*255.0f;
		p->m_Color &= 0x00FFFFFF;
		p->m_Color |= ((DWORD)alpha)<<24;
		p = p->m_pNext;
	}
} // PAlphaRamp::Process

/*****************************************************************************/
/*	PColorRamp implemetation
/*****************************************************************************/
PColorRamp::PColorRamp()
{
	m_NRepeats = 1;
}

void PColorRamp::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PColorRamp", this );
	pm.f( "Repeat", m_NRepeats );
    pm.p( "NumKeys", GetNKeys );
	pm.p( "Ramp", GetColorRamp, SetColorRamp, "color_ramp" );
} // PColorRamp::Expose

void PColorRamp::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	m_Ramp.Serialize( os );
	os << m_NRepeats;
}

void PColorRamp::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	m_Ramp.Unserialize( is );
	is >> m_NRepeats;
} // PColorRamp::Unserialize

void PColorRamp::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		float t = m_NRepeats * p->m_Age / p->m_TimeToLive;
		t -= floorf( t );
        p->m_Color &= 0xFF000000;
		p->m_Color |= (m_Ramp.GetColor( t )&0x00FFFFFF);
		p = p->m_pNext;
	}
} // PColorRamp::Process

/*****************************************************************************/
/*	PFluctuate implemetation
/*****************************************************************************/
PFluctuate::PFluctuate()
{
	m_Direction = Vector3D::oX;
	m_Variation = 5.0f;
}

void PFluctuate::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PFluctuate", this );
	pm.f( "DirX", m_Direction.x );
	pm.f( "DirY", m_Direction.y );
	pm.f( "DirZ", m_Direction.z );
	pm.f( "Variation", m_Variation );
} // PFluctuate::Expose

void PFluctuate::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Direction << m_Variation;
}

void PFluctuate::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Direction >> m_Variation;
} // PFluctuate::Unserialize

void PFluctuate::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		//  TODO: make it to be undependent of the framerate
		Vector3D f( m_Direction );
		f.x += rndValuef( -m_Variation, m_Variation );
		f.y += rndValuef( -m_Variation, m_Variation );
		f.z += rndValuef( -m_Variation, m_Variation );
		f *= dt;
		p->m_Velocity += f;
		p = p->m_pNext;
	}
} // PFluctuate::Process

/*****************************************************************************/
/*	PSizeRamp implemetation
/*****************************************************************************/
PSizeRamp::PSizeRamp()
{
	m_MinSize = 0.0f;
	m_MaxSize = 100.0f;
	m_Repeat  = 1;
	m_Axis	  = aXYZ; 
}

void PSizeRamp::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PSizeRamp", this );
	pm.f( "MinSize",	m_MinSize );
	pm.f( "MaxSize",	m_MaxSize );
	pm.f( "Axis",		m_Axis );
	pm.f( "Repeat",		m_Repeat );
    pm.p( "NumKeys", GetNKeys );
	pm.p( "Ramp", GetSizeRamp, SetSizeRamp, "alpha_ramp" );
} // PSizeRamp::Expose

void PSizeRamp::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	m_Ramp.Serialize( os );
	os << m_MinSize << m_MaxSize << m_Repeat << Enum2Byte( m_Axis );
}

void PSizeRamp::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	m_Ramp.Unserialize( is );
	is >> m_MinSize >> m_MaxSize >> m_Repeat >> Enum2Byte( m_Axis );
} // PSizeRamp::Unserialize

void PSizeRamp::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		float t = m_Repeat * p->m_Age / p->m_TimeToLive;
		t -= floorf( t );
		float sz = m_MinSize +  m_Ramp.GetAlpha( t )*(m_MaxSize - m_MinSize);
		if (m_Axis&1) p->m_Size.x = sz;
		if (m_Axis&2) p->m_Size.y = sz;
		if (m_Axis&4) p->m_Size.z = sz;
		p = p->m_pNext;
	}
} // PSizeRamp::Process

/*****************************************************************************/
/*	PSizeFade implemetation
/*****************************************************************************/
PSizeFade::PSizeFade()
{
	m_bAbsolute  = true;
	m_StartSize  = 100.0f;
	m_EndSize	 = 200.0f;
}

void PSizeFade::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PSizeFade", this );
	pm.f( "StartSize", m_StartSize );
	pm.f( "EndSize", m_EndSize );
	pm.f( "Absolute", m_bAbsolute );
} // PSizeFade::Expose

void PSizeFade::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_StartSize << m_EndSize << m_bAbsolute;
}

void PSizeFade::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_StartSize >> m_EndSize >> m_bAbsolute;
} // PSizeFade::Unserialize

void PSizeFade::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		if (m_bAbsolute)
		{
			float sz = m_StartSize + (m_EndSize - m_StartSize)*p->m_Age/p->m_TimeToLive;

			p->m_Size.x = sz;
			p->m_Size.y = sz;
			p->m_Size.z = sz;
		}
		else
		{
			float sz = (m_EndSize - m_StartSize)*dt/p->m_TimeToLive;

			p->m_Size.x += sz;
			p->m_Size.y += sz;
			p->m_Size.z += sz;
		}
		p = p->m_pNext;
	}
} // PSizeFade::Process

/*****************************************************************************/
/*	PFrame implemetation
/*****************************************************************************/
PFrame::PFrame()
{
	m_Rate	= 10.0f;
	m_DRate	= 0.0f;
	m_NRows	= 4;
	m_NCols = 4;
	m_bUV2 = false;
}

void PFrame::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PFrame", this );
	pm.f( "SecondChannel", m_bUV2 );
	pm.f( "NCols", m_NCols );
	pm.f( "NRows", m_NRows );
	pm.f( "Rate",  m_Rate  );
	pm.f( "dRate", m_DRate );
} // PFrame::Expose

void PFrame::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Rate << m_DRate << m_NRows << m_NCols << m_bUV2;
}

void PFrame::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Rate >> m_DRate >> m_NRows >> m_NCols >> m_bUV2;
} // PFrame::Unserialize

void PFrame::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	int nF = m_NRows*m_NCols;
	float w = 1.0f / m_NCols;
	float h = 1.0f / m_NRows;

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p->m_FrameTime -= dt; 
		if (p->m_FrameTime <= 0.0f) 
		{
			//  assign next frame time
			float rdt = rndValuef( m_Rate - m_DRate, m_Rate + m_DRate );
			if (rdt < c_Epsilon) rdt = c_Epsilon;
			rdt = 1.0f / rdt;
			p->m_FrameTime += rdt;
			p->m_Frame++;
			//  last frame reached
			if (p->m_Frame >= nF)
			{
				p->m_Frame = 0;
			}

			//  assign uv
			int row = p->m_Frame / m_NCols;
			int col = p->m_Frame % m_NCols;

			if (m_bUV2)
			{
				p->m_UV2.x = float( col )*w;
				p->m_UV2.y = float( row )*h;
				p->m_UV2.w = w;
				p->m_UV2.h = h;
			}
			else
			{
				p->m_UV.x = float( col )*w;
				p->m_UV.y = float( row )*h;
				p->m_UV.w = w;
				p->m_UV.h = h;
			}
		}
		p = p->m_pNext;
	}
} // PFrame::Process

/*****************************************************************************/
/*	PUVMove implemetation
/*****************************************************************************/
PUVMove::PUVMove()
{
	m_UScroll = 10.0f;
	m_VScroll = 0.0f;
	m_UTile   = 1.0f;
	m_VTile   = 1.0f;
	m_bUV2 = false;
}

void PUVMove::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PUVMove", this );
	pm.f( "SecondChannel", m_bUV2 );
	pm.f( "UScroll", m_UScroll );
	pm.f( "VScroll", m_VScroll );
	pm.f( "UTile", m_UTile );
	pm.f( "VTile", m_VTile );
} // PUVMove::Expose

void PUVMove::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_UScroll << m_VScroll << m_bUV2 << m_UTile << m_VTile;
}

void PUVMove::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_UScroll >> m_VScroll >> m_bUV2 >> m_UTile >> m_VTile;
} // PUVMove::Unserialize

void PUVMove::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	float du = m_UScroll*dt*0.01f;
	float dv = m_VScroll*dt*0.01f;
	float UScale = 1.0f / float( m_UTile );
	float VScale = 1.0f / float( m_VTile );

	PParticle* p = pEmitter->m_pParticle;

	if (m_bUV2)
	{
		while (p)
		{
			p->m_UV2.x += du;
			p->m_UV2.y += dv;
			p->m_UV2.w = UScale;
			p->m_UV2.h = VScale;
			p = p->m_pNext;
		}
	}
	else
	{
		while (p)
		{
			p->m_UV.x += du;
			p->m_UV.y += dv;
			p->m_UV.w = UScale;
			p->m_UV.h = VScale;
			p = p->m_pNext;
		}
	}
} // PUVMove::Process

/*****************************************************************************/
/*	PDrag implemetation
/*****************************************************************************/
PDrag::PDrag()
{
	m_Density			= 0.001f;			
	m_Viscosity			= 1.8e-5f;		
	m_bOverrideRadius	= 1;	
	m_ParticleRadius	= 1.0f;	
	m_A					= 6.0f * c_PI * m_Viscosity;
	m_B					= 0.2f * c_PI * m_Density;
    SetDensity  ( m_Density );
    SetViscosity( m_Viscosity );
} // PDrag::PDrag

void PDrag::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Density << m_Viscosity << m_bOverrideRadius << m_ParticleRadius;
} // PDrag::Serialize

void PDrag::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Density >> m_Viscosity >> m_bOverrideRadius >> m_ParticleRadius;
	SetDensity( m_Density );
	SetViscosity( m_Viscosity );
} // PDrag::Unserialize

void PDrag::SetDensity( float val )
{
	m_Density = val;
	m_B = 0.2f * c_PI * m_Density;
} // PDrag::SetDensity

void PDrag::SetViscosity( float val )
{
	m_Viscosity = val;
	m_A = 6.0f * c_PI * m_Viscosity;
} // PDrag::SetViscosity

void PDrag::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();

    //  HACK: do not allow big time deltas
    const float c_DragTimeBias = 0.05f;
    if (dt > c_DragTimeBias) dt = c_DragTimeBias;

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		Vector3D v = p->m_Velocity;
		float nv = v.normalize();
		float radius = m_bOverrideRadius ? m_ParticleRadius : p->m_Size.norm();

		float R = m_A * radius * nv + m_B * radius * radius * nv * nv;
		float dv = R * dt;
		clamp( dv, 0.0f, nv );
		v *= -dv;
		p->m_Velocity += v;
		p = p->m_pNext;
	}
} // PDrag::Process

void PDrag::SetToAir()
{
	SetViscosity( 1.8e-5f	);
	SetDensity	( 1.2929f	);
} // PDrag::SetToAir

void PDrag::SetToWater()
{
	SetViscosity( 1.002e-3f	);
	SetDensity	( 1.0f		);
} // PDrag::SetToWater

void PDrag::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PDrag", this );
	pm.p( "FluidDensity",		GetDensity,		SetDensity	 );
	pm.p( "FluidViscosity",	GetViscosity,	SetViscosity );
	pm.f( "OverrideRadius",	m_bOverrideRadius	);
	pm.f( "ParticleRadius",	m_ParticleRadius	);
	pm.m( "SetToAir",		SetToAir			);
	pm.m( "SetToWater",		SetToWater			);
} // PDrag::Expose

/*****************************************************************************/
/*	PVortex implemetation
/*****************************************************************************/
PVortex::PVortex()
{
	m_Intensity			= 100.0f;
	m_CenterAttraction	= 100.0f;
}

void PVortex::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PVortex", this );
	pm.f( "Intensity", m_Intensity );
	pm.f( "CenterAttraction", m_CenterAttraction );
} // PVortex::Expose

void PVortex::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Intensity << m_CenterAttraction;
} // PVortex::Serialize

void PVortex::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Intensity >> m_CenterAttraction;
} // PVortex::Unserialize

void PVortex::Process( PEmitterInstance* pEmitter )
{
	float dt = pEmitter->GetTimeDelta();
	float intensity  = m_Intensity * dt;
	float centerAttr = m_CenterAttraction * dt;

	Vector3D center( 0, 0, 0 );
	Vector3D dir   ( 0, 0, 1 );

	const float c_DeadZone = 0.1f;

	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		Vector3D diff( p->m_Position );
		diff -= center;
		float pr = diff.dot( dir );
		if (pr > c_DeadZone)
		{
			diff = center; diff.addWeighted( dir, pr );
			Vector3D vr( p->m_Position );
			vr -= diff;
			Vector3D accDir; accDir.cross( vr, dir );
			if (accDir.norm2() > c_DeadZone*c_DeadZone)
			{
				accDir *= intensity;
				accDir.addWeighted( vr, -centerAttr );
				p->m_Velocity += accDir;
			}
		}

		p = p->m_pNext;
	}
} // PVortex::Process

/*****************************************************************************/
/*	POrbit implemetation
/*****************************************************************************/
POrbit::POrbit()
{
	m_Type		= otSphere;
	m_Radius	= 100.0f; 
	m_Center	= Vector3D::null; 
	m_Velocity	= 0.1f;
}

void POrbit::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "POrbit", this );
	pm.f( "Type",		m_Type		);
	pm.f( "Velocity",	m_Velocity	);
	pm.f( "CenterX",	m_Center.x	);
	pm.f( "CenterY",	m_Center.y	);
	pm.f( "CenterZ",	m_Center.z	);
} // POrbit::Expose

void POrbit::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Radius << m_Center << Enum2Byte( m_Type ) << m_Velocity;
} // POrbit::Serialize

void POrbit::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Radius >> m_Center >> Enum2Byte( m_Type ) >> m_Velocity;
} // POrbit::Unserialize

void POrbit::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;	
	
    float dt = pEmitter->GetTimeDelta();
    Matrix4D rot;
    rot.rotation( Vector3D::oZ, dt*m_Velocity );

	if (m_Type == otSphere)
	{
        //  sphere
		while (p)
		{
			Vector3D dir;
			rot.transformPt( p->m_Position );
            rot.transformVec( p->m_Velocity );
            p = p->m_pNext;
		}
	}
	else
	{
		//  cylinder
		while (p)
		{
			Vector3D dir;
            rot.transformPt( p->m_Position );
            rot.transformVec( p->m_Velocity );
            p = p->m_pNext;
        }
    }
} // POrbit::Process

/*****************************************************************************/
/*	PClampVelocity implemetation
/*****************************************************************************/
PClampVelocity::PClampVelocity()
{
	m_MinVel = 0.0f;
	m_MaxVel = 1.0f;
}

void PClampVelocity::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PClampVelocity", this );
} // PClampVelocity::Expose

void PClampVelocity::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // PClampVelocity::Serialize

void PClampVelocity::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
} // PClampVelocity::Unserialize

void PClampVelocity::Process( PEmitterInstance* pEmitter )
{
	assert( false );
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p = p->m_pNext;
	}
} // PClampVelocity::Process

/*****************************************************************************/
/*	PTurbulence implemetation
/*****************************************************************************/
PTurbulence::PTurbulence()
{
    m_Magnitude = 1.0f;
    m_Frequency = 1.0f;
    m_Phase = Vector3D( -1.0f, 0.0f, 1.0f );
} // PTurbulence::PTurbulence

void PTurbulence::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PTurbulence", this );
    pm.f( "Magnitude",  m_Magnitude );
    pm.f( "Frequency",  m_Frequency );
    pm.f( "PhaseX",     m_Phase.x );
    pm.f( "PhaseY",     m_Phase.y );
    pm.f( "PhaseZ",     m_Phase.z );
} // PTurbulence::Expose

void PTurbulence::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
} // PTurbulence::Serialize

void PTurbulence::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
} // PTurbulence::Unserialize

void PTurbulence::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		Vector3D mpos = p->m_Position;
        mpos *= m_Frequency;
		Vector3D dv;
        dv.x = PerlinNoise( mpos.x + m_Phase.x, mpos.y + m_Phase.x, mpos.z + m_Phase.x );
        dv.y = PerlinNoise( mpos.x + m_Phase.y, mpos.y + m_Phase.y, mpos.z + m_Phase.y );
        dv.z = PerlinNoise( mpos.x + m_Phase.z, mpos.y + m_Phase.z, mpos.z + m_Phase.z );
		dv *= m_Magnitude;
        p->m_Velocity += dv;
        p = p->m_pNext;
	}
} // PTurbulence::Process

/*****************************************************************************/
/*	PTarget implemetation
/*****************************************************************************/
PTarget::PTarget()
{
	m_TargetPos		= Vector3D( 0, 0, 200 ); 
	m_TargetDir		= Vector3D( 0, 0, -1 ); 
	m_TargetSource	= tsFixedTarget;
} // PTarget::PTarget

void PTarget::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PTarget", this );
	pm.f( "TargetSource", m_TargetSource );
	pm.f( "TargetX", m_TargetPos.x );
	pm.f( "TargetY", m_TargetPos.y );
	pm.f( "TargetZ", m_TargetPos.z );
} // PTarget::Expose

void PTarget::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << Enum2Byte( m_TargetSource ) << m_TargetPos << m_TargetDir;
}

void PTarget::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> Enum2Byte( m_TargetSource ) >> m_TargetPos >> m_TargetDir;
}

void PTarget::OnChangeChildren()
{
    for (int i = 0; i < GetNChildren(); i++)
    {
    
    }
} // PTarget::OnChangeChildren

Vector3D PTarget::GetTargetPos( PEmitterInstance* pEmitter ) const
{
    if (m_TargetSource == tsFixedTarget)
    {
        Vector3D dst = m_TargetPos;
        pEmitter->m_WorldTM.transformPt( dst );
        return dst;
    }
    else if (m_TargetSource == tsRootTarget)
    { 
        return pEmitter->m_Target;
    }
    else if (m_TargetSource == tsChildParticle)
    {
        
    }
    return Vector3D( 0.0f, 0.0f, 300.0f );
} // PTarget::GetTargetPos

/*****************************************************************************/
/*	PRetarget implemetation
/*****************************************************************************/
PRetarget::PRetarget()
{
}

void PRetarget::OnChangeChildren()
{
    for (int i = 0; i < GetNChildren(); i++)
    {
        Node* pChild = GetChild( i );
        if (!pChild->IsA<PEmitter>())
        {
            RemoveChild( pChild );
        }
    }
} // PRetarget::OnChangeChildren

void PRetarget::Process( PEmitterInstance* pEmitter )
{
    PParticle* p = pEmitter->m_pParticle;
    if (!p) return;
    //IEffMgr->SetTarget( pEmitter->m_ParentID, p->m_Position ); 
} // PRetarget::Process

/*****************************************************************************/
/*	PLightning implemetation
/*****************************************************************************/
PLightning::PLightning()
{
	m_Spread	= 10.0f;	
	m_FrameRate	= 5.0f;
	m_Wildness	= 2.0f;
} // PLightning::PLightning

void PLightning::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PLightning", this );
	pm.f( "Spread",		m_Spread	);
	pm.f( "FrameRate",	m_FrameRate );
	pm.f( "Wildness",	m_Wildness	);
} // PLightning::Expose

void PLightning::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_Spread << m_FrameRate << m_Wildness;
}

void PLightning::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_Spread >> m_FrameRate >> m_Wildness;
}

void PLightning::Process( PEmitterInstance* pEmitter )
{
	Vector3D org = pEmitter->m_WorldTM.getTranslation();
    Vector3D dst = GetTargetPos( pEmitter );

	PParticle* pr = pEmitter->m_pParticle;
	PParticle* pl = pr; 
	if (!pr) return;
	int nP = 1;
	while( true )
	{
		if (!pr->m_pNext) break;
		nP++;
		pr = pr->m_pNext;
	}

	int seed = (pEmitter->m_CurTime - pEmitter->m_StartTime)/m_FrameRate;
	rndInit( seed );
	pl->m_Position = org;
	pr->m_Position = dst;
	m_NPoints = nP;
	Fractalize( pl, pr, nP );
	
	//  postprocess particle positions

} // PLightning::Process

void PLightning::Fractalize( PParticle* pl, PParticle* pr, int nP )
{
	if (pr == pl->m_pNext) return;
	PParticle* pm = pl;
	int mP = nP / 2;
	for (int i = 0; i < mP; i++) pm = pm->m_pNext;
	float fscale = float( nP ) / m_NPoints;
	pm->m_Position.z = (pl->m_Position.z + pr->m_Position.z)*0.5f + rndValuef( -m_Wildness, m_Wildness )*fscale;
	pm->m_Position.x = (pl->m_Position.x + pr->m_Position.x)*0.5f + rndValuef( -m_Spread, m_Spread )*fscale;
	pm->m_Position.y = (pl->m_Position.y + pr->m_Position.y)*0.5f + rndValuef( -m_Spread, m_Spread )*fscale;

	Fractalize( pl, pm, mP );
	Fractalize( pm, pr, nP - mP );
} // PLightning::Fractalize

/*****************************************************************************/
/*	PHoming implemetation
/*****************************************************************************/
PHoming::PHoming()
{
} // PHoming::PHoming

void PHoming::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PHoming", this );
} // PHoming::Expose

void PHoming::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void PHoming::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
}

void PHoming::Process( PEmitterInstance* pEmitter )
{
	PParticle* p = pEmitter->m_pParticle;
	while (p)
	{
		p = p->m_pNext;
	}
} // PHoming::Process

/*****************************************************************************/
/*	PEffectMgr implemetation
/*****************************************************************************/
PEffectMgr::PEffectMgr()
{
	m_ZBiasMultiplier		= 80.0f;
	m_NParticles			= 0;
	m_LastUsed				= 0;
	m_MinDt					= 0.0005f;
	m_MaxDt					= 0.1f;
	m_TimeDelta				= 0.0f;
    m_bPaused               = false;
	m_InstanceFrameTimeout	= c_InstanceFrameTimeout;
    m_bAutoupdate           = false; 
    m_Timeout               = 10.0f;
    m_FadeTime              = 5.0f;
    m_RenderQSize           = 0;
    m_PlayRate              = 1.0f;

	memset( &m_Particle, 0, sizeof( PParticle ) * c_ParticlePoolSize );
    memset( m_ParticleStatus, 0, sizeof( m_ParticleStatus ) ); 
	SetName( "EffectManager" );
} // PEffectMgr::PEffectMgr

void PEffectMgr::Reset()
{
	m_NParticles		= 0;
	m_LastUsed			= 0;
    m_bPaused           = false;
    m_RenderQSize       = 0;
    m_PlayRate          = 1.0f;
	m_Emitters.reset();
	memset( &m_Particle, 0, sizeof( PParticle ) * c_ParticlePoolSize );
    memset( m_ParticleStatus, 0, sizeof( DWORD ) * c_ParticlePoolSize ); 
} // PEffectMgr::Reset

void PEffectMgr::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "PEffectMgr", this );
    pm.f( "Paused",             m_bPaused               );
	pm.f( "ZBiasMultiplier",	m_ZBiasMultiplier       );
	pm.f( "MinTimeDelta",		m_MinDt				    );
	pm.f( "MaxTimeDelta",		m_MaxDt				    );
	pm.f( "InstanceTimeout",	m_InstanceFrameTimeout  );
	pm.p( "NumEmitterInst",		GetNEmitterInst		    );
	pm.f( "NumParticles",		m_NParticles,		NULL, true );
	pm.f( "LastFreed",			m_LastUsed,		NULL, true );
	pm.p( "PoolSize", GetParticlePoolSize );
	pm.m( "Dump", Dump );
} // PEffectMgr::Expose

DWORD PEffectMgr::GetCurEffInstance() const
{
    DWORD idx = m_Emitters.find( GetEntityContext() );
    if (idx == NO_ELEMENT) return NO_ELEMENT;
    const PEmitterInstance& inst = m_Emitters.elem( idx );
    return inst.m_StampedID;
} // PEffectMgr::GetCurEffInstance

DWORD PEffectMgr::SpawnEmitter( PEmitter* pEmitter, int particleID, int parentID, bool bAttach )
{
    int id = m_Emitters.find( GetEntityContext() );
    if (id != NO_ELEMENT) return m_Emitters.elem( id ).m_StampedID;
    if (m_Emitters.numElem() == m_Emitters.poolSize())
    {
        Log.Warning( "Could not spawn emitter - too much of them already" );
        return 0xFFFFFFFF;
    }

	PEmitterInstance inst;
	inst.Reset();
	inst.m_pEmitter		= pEmitter;
	inst.m_StartTime	= pEmitter->GetStartTime();
	inst.m_TotalTime	= pEmitter->GetTotalTime();
	inst.m_LastFrame	= IRS->GetCurFrame();
	inst.m_ParentID		= parentID;
	inst.m_WorldTM		= Matrix4D::identity;
    inst.m_PrevWorldTM	= Matrix4D::identity;
    inst.m_EmitterTM	= Matrix4D::identity;
    inst.m_Alpha        = 1.0f;
    inst.m_Intensity    = 1.0f;
    inst.m_Target       = Vector3D( 0.0f, 0.0f, 300.0f );
    inst.m_Cycles       = 0;
    inst.m_Timeout      = m_Timeout;

    inst.SetFlag( ifAutoUpdate, m_bAutoupdate || pEmitter->IsAutoUpdated() );

    //  stamp used to validate emitter instances
    static WORD s_InstanceStamp = 0;
    s_InstanceStamp++;
    
	if (bAttach && particleID != -1)
	{
		inst.m_ParticleID = particleID;
	}

	id = m_Emitters.add( GetEntityContext(), inst );
    DWORD stampedID = id | (((DWORD)s_InstanceStamp) << 16);
    m_Emitters.elem( id ).m_ID          = id;
    m_Emitters.elem( id ).m_StampedID   = stampedID;
	return stampedID;
} // PEffectMgr::SpawnEmitter

void PEffectMgr::Pause( bool bPause )
{
    m_bPaused = bPause;
} // PEffectMgr::Pause

DWORD PEffectMgr::InstanceEffect( DWORD effID )
{
	PEmitter* pEff = (PEmitter*)NodePool::GetNode( effID );
	if (!pEff || !pEff->IsA<PEmitter>()) return 0xFFFFFFFF;
	DWORD res = SpawnEmitter( pEff );
	return res;
} // PEffectMgr::InstanceEffect

void PEffectMgr::RewindEffect( DWORD hInst, float amount, float step, bool bRewChildren )
{
    if (!IsValid( hInst )) return;
    PEmitterInstance& em = GetEmitterInstance( hInst );
	
	DWORD curFrame = IRS->GetCurFrame();
	rndInit( curFrame^GetTickCount() );

	em.SetFlag( ifNeedDraw, false );
	em.SetFlag( ifNeedUpdate );

	//  scan through all instances and set children 'need update' flags
	//  process all active emitter instances
	PInstanceHash::iterator it = m_Emitters.begin();
	while (it)
	{
		PEmitterInstance& cem = *it;
		if (cem.m_ParticleID != -1)
		{
			const PParticle& attach = m_Particle[cem.m_ParticleID]; 
			PEmitterInstance& pem = m_Emitters.elem( attach.m_EmitterID );
			cem.SetFlag( ifNeedUpdate, pem.GetFlag( ifNeedUpdate ) );
		}
		++it;
	}
	
	//  now update who needs that
	PInstanceHash::iterator uit = m_Emitters.begin();
	while (uit)
	{
		PEmitterInstance& cem = *uit;
		if (cem.GetFlag( ifNeedUpdate ))
		{
			float cTime = cem.m_CurTime;
			PEmitter* pEmitter = cem.m_pEmitter;
			for (float t = 0.0f; t < amount; t += step)
			{
				StepEmitter( cem, step );
			}
			cem.m_LastFrame = curFrame;
		}
		++uit;
	}
} // PEffectMgr::RewindEffect

float PEffectMgr::GetCurTime( DWORD hInst ) const
{
    if (!IsValid( hInst )) return 0.0f;
    const PEmitterInstance& em = GetEmitterInstance( hInst );
    return em.m_CurTime;
} // PEffectMgr::GetCurTime

void PEffectMgr::SetAlphaFactor( DWORD hInst, float factor )
{
    if (!IsValid( hInst )) return;
    PEmitterInstance& em = GetEmitterInstance( hInst );
    em.m_Alpha = factor;
} // PEffectMgr::SetAlphaFactor

void PEffectMgr::SetIntensity( DWORD hInst, float intensity )
{
    if (!IsValid( hInst )) return;
    PEmitterInstance& em = GetEmitterInstance( hInst );
    em.m_Intensity = intensity;
} // PEffectMgr::SetIntensity

void PEffectMgr::UpdateInstance( DWORD hInst, const Matrix4D& tm )
{
	if (!IsValid( hInst )) return;
	PEmitterInstance& inst = GetEmitterInstance( hInst );
    inst.SetFlag( ifMoved, !inst.m_WorldTM.equal( tm ) );
    inst.m_PrevWorldTM  = inst.m_WorldTM;
	inst.m_WorldTM      = tm;
	inst.SetFlag( ifNeedDraw );
} // PEffectMgr::SetTransform

void PEffectMgr::UpdateInstance( const Matrix4D& tm )
{
    UpdateInstance( GetCurEffInstance(), tm );
}

void PEffectMgr::DestroyInstance()
{
    DestroyInstance( GetCurEffInstance() );
}

bool PEffectMgr::IsValid( DWORD hInst ) const
{
    if (hInst == 0xFFFFFFFF) return false;
    DWORD index = hInst&0x0000FFFF;
    if (index > m_Emitters.maxElem() || m_Emitters.numElem() == 0) return false;
    const PEmitterInstance& em = m_Emitters.elem( index );
    return (em.m_StampedID == hInst);
} // PEffectMgr::IsValid

void PEffectMgr::DestroyInstance( DWORD hInst )
{
	if (!IsValid( hInst )) return;
	PEmitterInstance& em = GetEmitterInstance( hInst );
	
	//  free all emitter's particles 
	PParticle* p = em.m_pParticle;
	while (p) 
    { 
        p->m_Flags = 0; 
        m_ParticleStatus[p->m_ID] = c_FreeParticleSlot; 
        p = p->m_pNext; 
        m_NParticles--; 
    }
	
	//  delete emitter from the pool
	m_Emitters.delElem( em.m_ID );

	//  destroy all children emitters
	PInstanceHash::iterator it = m_Emitters.begin();
	while (it)
	{
		PEmitterInstance& cem = *it;
		if (cem.m_ParentID == em.m_ID) DestroyInstance( cem.m_StampedID );
		++it;
	}
    em.m_StampedID = 0xFFFFFFFF;
}// PEffectMgr::DestroyInstance

void PEffectMgr::ResetInstance( DWORD hInst, bool bResetChildren )
{
    if (!IsValid( hInst )) return;
    PEmitterInstance& em = GetEmitterInstance( hInst );
	em.m_CurTime    = em.m_StartTime;
    em.m_LastTime   = em.m_CurTime - m_TimeDelta;
    em.m_EmitAccum  = em.m_StartTime;
    em.m_Cycles++;

    if (bResetChildren)
    {
        PInstanceHash::iterator it = m_Emitters.begin();
        while (it)
        {
            PEmitterInstance& cem = *it;
            if (cem.m_ParentID == em.m_ID) ResetInstance( cem.m_StampedID, true );
            ++it;
        }
    } 
} // PEffectMgr::ResetInstance

void PEffectMgr::PreRender()
{
    DWORD curFrame = IRS->GetCurFrame();
    IRS->ResetWorldMatrix();

    //  render null layer renderables
    //  fill geometry pass
    PInstanceHash::iterator it = m_Emitters.begin();
    while (it)
    {
        PEmitterInstance& em = *it;
        PEmitter* pEmitter = em.m_pEmitter;

        //  apply render-only operators to particles 
        int nOp = pEmitter->GetNOperators();
        for (int i = 0; i < nOp; i++)
        {
            PRenderer* pR = (PRenderer*)pEmitter->GetOperator( i );
            if (pR->IsInvisible()) continue;
            if (!pR->IsVisual() || !pR->IsNullLayer()) continue;

            pR->FillGeometry( &em );
        }	
        ++it;
    }

    //  render geometry pass
    PInstanceHash::iterator rit = m_Emitters.begin();
    while (rit)
    {
        PEmitterInstance& em = *rit;
        PEmitter* pEmitter = em.m_pEmitter;

        //  apply render-only operators to particles 
        int nOp = pEmitter->GetNOperators();
        for (int i = 0; i < nOp; i++)
        {
            PRenderer* pR = (PRenderer*)pEmitter->GetOperator( i );
            if (pR->IsInvisible()) continue;
            if (!pR->IsVisual() || !pR->IsNullLayer()) continue;

            pR->RenderGeometry( &em );
        }	
        ++rit;
    }
} // PEffectMgr::PreRender

void PEffectMgr::Evaluate()
{
    //  auxiliary setup
    BaseCamera* pCam = BaseCamera::GetActiveCamera();
    if (pCam)
    {
        m_ScreenPlane.fromPointNormal( pCam->GetPos(), pCam->GetDir() );
        m_PlaneUpVec = Vector3D::oZ;
        m_ScreenPlane.ProjectVec( m_PlaneUpVec );
        if (m_PlaneUpVec.normalize() < c_Epsilon)
        {
            m_PlaneUpVec = Vector3D::oX;
            m_ScreenPlane.ProjectVec( m_PlaneUpVec );
            if (m_PlaneUpVec.normalize() < c_Epsilon)
            {
                m_PlaneUpVec = Vector3D::oY;
                m_ScreenPlane.ProjectVec( m_PlaneUpVec );
                m_PlaneUpVec.normalize();
            }
        }
        m_PlaneNVec = pCam->GetDir();
    }
    else
    {
        m_ScreenPlane = Plane::xOy;
        m_PlaneUpVec  = Vector3D::oX;
        m_PlaneNVec	  = Vector3D::oZ;
    }

    DWORD curFrame = IRS->GetCurFrame();

    //  get time delta
    static Timer s_Timer;
    float dt = s_Timer.seconds(); 
    m_TimeDelta = dt;
    s_Timer.start();
    clamp( dt, m_MinDt, m_MaxDt );
    rndInit( curFrame^GetTickCount() );
    m_TimeDelta *= m_PlayRate;
    if (m_bPaused) m_TimeDelta = 0.0f;

    //  propagate instance attributes
    PInstanceHash::iterator it = m_Emitters.begin();
    while (it)
    {
        PEmitterInstance& em = *it;
        PEmitter* pEmitter = em.m_pEmitter;

        if (em.m_ParticleID != -1)
        {
            const PParticle& attach = m_Particle[em.m_ParticleID]; 
            PEmitterInstance& pem = m_Emitters.elem( attach.m_EmitterID );
            if (pem.GetFlag( ifNeedDraw )) em.SetFlag( ifNeedDraw );
            em.m_Alpha      = pem.m_Alpha;
            em.m_Intensity  = pem.m_Intensity;
            em.m_Target     = pem.m_Target;
        } 
        if (em.GetFlag( ifAutoUpdate )) em.SetFlag( ifNeedDraw );
        ++it;
    }

    //  process all active emitter instances
    it.reset();
    int nEmitters = m_Emitters.numElem();
    int cEmitter = 0;
    while (it)
    {
        PEmitterInstance& em = *it;
        if (em.GetFlag( ifAutoUpdate )) em.SetFlag( ifNeedDraw );
        StepEmitter( em, m_TimeDelta );		
        if (em.GetFlag( ifNeedDraw )) em.m_LastFrame = curFrame;
        ++it; cEmitter++;
    }

    //  delete emitters that are over
    it.reset();
    while (it)
    {
        PEmitterInstance& em = *it; ++it;
        PEmitter* pEmitter = em.m_pEmitter;
        //  if instance has not been rendered during number of frames, then instance is over 
        if (curFrame - em.m_LastFrame > m_InstanceFrameTimeout) 
        {
            DestroyInstance( em.m_StampedID );
            continue;
        }

        //  if instance is being auto-updated, and timeout is reached
        if (em.GetFlag( ifAutoUpdate ) && em.m_Timeout <= 0.0f)
        {
            DestroyInstance( em.m_StampedID );
            continue;
        }

        if ((em.m_CurTime > pEmitter->m_TotalTime) ||                                    //  time is over
            (em.m_ParticleID != -1 && m_Particle[em.m_ParticleID].GetFlag( pfDead ))     //  parent is over
            ) 
        {
            if (pEmitter->IsLooped()) 
            {
                ResetInstance( em.m_StampedID, true );
            }
            else
            {
                if (em.m_pParticle || pEmitter->IsPlayedForever()) continue;
                DestroyInstance( em.m_StampedID );                
            }
        }
    }
} // PEffectMgr::Evaluate

void PEffectMgr::PostRender()
{
    static DWORD curFrame = IRS->GetCurFrame() - 1;
    curFrame = IRS->GetCurFrame();
    IRS->ResetWorldMatrix();

    //  render top layer renderables
    //  fill geometry pass
    PInstanceHash::iterator it = m_Emitters.begin();
    while (it)
    {
        PEmitterInstance& em = *it;
        PEmitter* pEmitter = em.m_pEmitter;

        //  apply render-only operators to particles 
        int nOp = pEmitter->GetNOperators();
        for (int i = 0; i < nOp; i++)
        {
            PRenderer* pR = (PRenderer*)pEmitter->GetOperator( i );
            if (pR->IsInvisible()) continue;
            if (!pR->IsVisual() || pR->IsNullLayer()) continue;

            pR->FillGeometry( &em );
        }	
        ++it;
    }

    //  render geometry pass
    PInstanceHash::iterator rit = m_Emitters.begin();
    while (rit)
    {
        PEmitterInstance& em = *rit;
        PEmitter* pEmitter = em.m_pEmitter;

        //  apply render-only operators to particles 
        int nOp = pEmitter->GetNOperators();
        for (int i = 0; i < nOp; i++)
        {
            PRenderer* pR = (PRenderer*)pEmitter->GetOperator( i );
            if (pR->IsInvisible()) continue;
            if (!pR->IsVisual() || pR->IsNullLayer()) continue;

            pR->RenderGeometry( &em );
        }	
        em.SetFlag( ifNeedDraw, false );
        ++rit;
    }
} // PEffectMgr::PostRender

void PEffectMgr::Render()
{
    return;
    Evaluate();
    PreRender();
    PostRender();
} // PEffectMgr::Render

bool PEffectMgr::EmitParticle( PEmitterInstance& em )
{
    if (m_NParticles == c_ParticlePoolSize) return false;
    
    //	allocate particle slot
    int pID = ++m_LastUsed;

    for (; pID < c_ParticlePoolSize; pID++)
    {
        if (m_ParticleStatus[pID] == c_FreeParticleSlot) break;
    }
    //  reached the end of the pool
    if (pID == c_ParticlePoolSize)
    {
        for (pID = 0; pID < c_ParticlePoolSize; pID++)
        {
            if (m_ParticleStatus[pID] == c_FreeParticleSlot) break;
        }
    }
    assert( m_ParticleStatus[pID] == c_FreeParticleSlot );

    m_NParticles++;
    m_ParticleStatus[pID] = c_UsedParticleSlot;
    m_LastUsed = pID;

    PParticle* tail = em.m_pParticle;
    PParticle& p = m_Particle[pID];
    //  initialize allocated particle
    p.m_Flags		= 0;
    p.m_Age			= 0.0f;
    p.m_ID			= pID;
    p.m_pNext		= tail;
    p.m_pPrev		= NULL;
    p.m_Frame		= 0;
    p.m_FrameTime	= 0.0f;
    p.m_AngVelocity = Vector3D::null;
    p.m_Velocity	= Vector3D::null;
    p.m_Size		= Vector3D( 20, 20, 20 );
    p.m_Color		= 0xFFFFFFFF;
    p.m_EmitterID	= em.m_ID;
    p.m_TimeToLive	= em.m_pEmitter->GetTimeToLive( p );
    p.m_UV			= Rct::unit;
    p.m_UV2			= Rct::unit;
    p.m_Roll		= 0.0f;
    p.m_Position    = Vector3D::null;

    if (&p == p.m_pNext)
    {
        Log.Error( "Impossible loop in the particle system." );
        return false;
    }
    
    if (em.m_pEmitter->IsWorldSpace())
    {
        em.m_WorldTM.transformPt( p.m_Position );
        em.m_WorldTM.transformVec( p.m_Velocity );
    }
    else
    {
        p.m_Position.zero();
    }
    p.m_PrevPosition = p.m_Position;

    if (p.m_TimeToLive <= 0.0f) p.SetFlag( pfImmortal );
    p.SetFlag( pfJustBorn );

    //  add allocated particles to the current emitter's chain
    if (tail) tail->m_pPrev = &p;
    em.m_pParticle = &p;
    return true;
} // PEffectMgr::EmitParticle

Matrix4D PEffectMgr::GetParticleWorldTM( const PParticle& p ) const
{
    Vector3D vz( p.m_Velocity );
    float v = vz.normalize();
    if (v < c_Epsilon) vz = Vector3D::oZ;
    Vector3D vy, vx( Vector3D::oX );
    vy.cross( vz, vx );
    vy.normalize();
    vx.cross( vy, vz );
    Matrix4D tm( vx, vy, vz, p.m_Position );    

    const PEmitterInstance& em = m_Emitters.elem( p.m_EmitterID ); 
    if (em.m_pEmitter->IsWorldSpace()) return tm;
    tm *= em.m_WorldTM;
    return tm;
} // PEffectMgr::GetParticleWorldTM

void PEffectMgr::StepEmitter( PEmitterInstance& em, float dt )
{
	PEmitter* pEmitter = em.m_pEmitter;
	if (!pEmitter || pEmitter->IsInvisible()) return;

    //  propagate emitter attributes
    if (em.m_ParentID != -1)
    {
        PEmitterInstance& pem = m_Emitters.elem( em.m_ParentID );
        if (pem.GetFlag( ifNeedDraw )) em.SetFlag( ifNeedDraw );
        em.m_Alpha      = pem.m_Alpha;
        em.m_Intensity  = pem.m_Intensity;
        em.m_Target     = pem.m_Target;
    }

    if (em.GetFlag( ifAutoUpdate ) && em.m_Timeout < m_FadeTime)
    {
        em.m_Alpha = em.m_Timeout/m_FadeTime;
    }

    //  update emitter transform
    if (em.m_ParticleID > 0)
    {
	    const PParticle& attach = m_Particle[em.m_ParticleID]; 
        em.m_WorldTM = GetParticleWorldTM( attach );
    }

	//  emit new particles
	int nEmit = pEmitter->NumToEmit( &em );
	for (int j = 0; j < nEmit; j++) EmitParticle( em );

    //  apply non render-only operators to particles 
    int nOp = pEmitter->GetNOperators();
    for (int i = 0; i < nOp; i++)
    {
        PRenderer* pR = (PRenderer*)pEmitter->GetOperator( i );
        if (pR->IsInvisible()) continue;
        if (pR->IsVisual()) continue;
        pR->Process( &em );
    }	
			
	// update common particle attributes
	PParticle* p = em.m_pParticle;
	while (p)
	{
        p->m_PrevPosition = p->m_Position;
		p->m_Position.addWeighted( p->m_Velocity, dt );
		p->m_Age += dt;
		p->SetFlag( pfJustBorn, false );
	
		p->m_Roll  += p->m_AngVelocity.x * dt;
	
		//  if particle is dead, remove it from the cluster
		if (p->GetFlag( pfDead ))
		{
			p->m_Flags = 0;
			PParticle* prev = p->m_pPrev;
			PParticle* next = p->m_pNext;
	
			if (prev) prev->m_pNext = next; else em.m_pParticle = next;
			if (next) next->m_pPrev = prev;
            m_ParticleStatus[p->m_ID] = c_FreeParticleSlot;
			p = next;
			m_NParticles--;
            assert( m_NParticles >= 0 );
			continue;
		}
		
		//  particle is about to die
		if (p->m_Age >= p->m_TimeToLive && !p->GetFlag( pfImmortal ))
		{
			p->SetFlag( pfDead, true );
		}
	
		p = p->m_pNext;
	}
	
	//  update emitter instance parameters
	em.m_LastTime = em.m_CurTime;
	em.m_CurTime  += dt;
    em.m_Timeout  -= dt;
} // PEffectMgr::StepEmitter

void PEffectMgr::Dump()
{
	DumpToFile( "c:\\dumps\\effmgr.txt" );
}

void PEffectMgr::DumpToFile( const char* fname )
{
	FILE* fp = fopen( fname, "wt" );
	if (!fp) return;
	
	fprintf( fp, "Effect manager dump.\nNEmitters=%d\nNParticles=%d\nLastFreed=%d\n", 
					m_Emitters.numElem(), m_NParticles, m_LastUsed );
	for (int i = 0; i < c_ParticlePoolSize; i++)
	{
		PParticle& p = m_Particle[i];
		if (!p.GetFlag( pfUsedSlot )) continue;
		fprintf( fp, "id:%d\t\t", p.m_ID );
		if (p.GetFlag( pfEmitter	)) fprintf( fp, " emitter "  );
		if (p.GetFlag( pfDead		)) fprintf( fp, " dead "	 );
		if (p.GetFlag( pfImmortal	)) fprintf( fp, " immortal " );
		if (p.m_pPrev) fprintf( fp, " prev: %d", p.m_pPrev->m_ID );
		if (p.m_pNext) fprintf( fp, " next: %d", p.m_pNext->m_ID );
		fprintf( fp, "\n" );
	}

	for (int i = 0; i < m_Emitters.numElem(); i++)
	{
		PEmitterInstance& em = m_Emitters.elem( i );
		fprintf( fp, "em:%d\t\t", i );
		fprintf( fp, "\n" );
	}
	fclose( fp );
} // PEffectMgr::DumpToFile

int PEffectMgr::GetEffectSetID( const char* fileName )
{
    int id = m_EffectSet.size();
    if (!fileName) return - 1;
    FInStream is( fileName );
    if (is.NoFile()) return -1;
    XMLNode root( is );
    int nEff = root.GetNChildren();
    XMLNode* pChild = root.FirstChild();
    EffectSet effSet;
    for (int i = 0; i < nEff; i++)
    {
        PEffect* pEff = new PEffect();
        pEff->FromXML( pChild );
        effSet.m_Effect.push_back( pEff );
        pChild = pChild->NextSibling();
    }
    effSet.m_File = fileName;
    m_EffectSet.push_back( effSet );
    return id;
} // PEffectMgr::GetEffectSetID

int PEffectMgr::GetNEffects( int setID )
{
    if (setID < 0 || setID >= m_EffectSet.size()) return 0;
    return m_EffectSet[setID].m_Effect.size();
} // PEffectMgr::GetNEffects

bool PEffectMgr::BindEffectSet( int setID, DWORD modelID )
{
    Node* pModel = NodePool::GetNode( modelID );
    if (!pModel) return false;
    int nEff = GetNEffects( setID );
    for (int i = 0; i < nEff; i++)
    {
        PEffect* pEff = GetEffectFromSet( setID, i );
        if (!pEff) return false;
        pEff->SetInvisible( !pEff->IsDefaultVisible() );
        Node* pBone = pModel->FindChild<TransformNode>( pEff->GetParentBoneName() );
        if (!pBone) pBone = pModel->FindChild<Group>( pEff->GetParentBoneName() );
        if (pBone) pBone->AddChild( pEff );
    }
    return false;
} // PEffectMgr::BindEffectSet

bool PEffectMgr::UpdateInstance( int setID, int effID, const Matrix4D& tm )
{
    PEffect* pEff = GetEffectFromSet( setID, effID );
    if (!pEff) return false;
    TransformNode::Push( tm );
    Node* pParent = pEff->GetParent();
    if (pParent)
    {
        Matrix4D tm;
        tm.translation( GetWorldTM( pParent ).getTranslation() );
        TransformNode::Push( tm );
    }
    pEff->Render();
    if (pParent) TransformNode::Pop();
    TransformNode::Pop();
    return true;
} // PEffectMgr::UpdateInstance

int PEffectMgr::FindEffectByName( int setID, const char* effName )
{
    if (setID < 0 || setID >= m_EffectSet.size()) return NULL;
    EffectSet& effs = m_EffectSet[setID];
    int nEff = effs.m_Effect.size();
    for (int i = 0; i < nEff; i++)
    {
        if (!stricmp( effName , effs.m_Effect[i]->GetEffectName() )) return i; 
    }
    return -1;
} // PEffectMgr::FindEffectByName

void PEffectMgr::SetAlphaFactor( int setID, int effID, float alpha )
{
    PEffect* pEff = GetEffectFromSet( setID, effID );
    if (!pEff) return;
    PushEntityContext( pEff->GetID() );
    SetAlphaFactor( alpha );
    PopEntityContext();
} // PEffectMgr::SetAlphaFactor

void PEffectMgr::SetIntensity( int setID, int effID, float intensity )
{
    PEffect* pEff = GetEffectFromSet( setID, effID );
    if (!pEff) return;
    PushEntityContext( pEff->GetID() );
    SetIntensity( intensity );
    PopEntityContext();
} // PEffectMgr::SetIntensity

float PEffectMgr::GetCurTime() const
{
    return GetCurTime( GetCurEffInstance() );
}

void PEffectMgr::SetAlphaFactor( float factor )
{
    SetAlphaFactor( GetCurEffInstance(), factor );
}

void PEffectMgr::SetIntensity( float intensity )
{
    SetIntensity( GetCurEffInstance(), intensity );
}

void PEffectMgr::SetTarget( const Vector3D& pos )
{
    DWORD hInst = GetCurEffInstance();
    if (!IsValid( hInst )) return;
    PEmitterInstance& em = GetEmitterInstance( hInst );
    em.m_Target = pos;
} // PEffectMgr::SetTarget

PEffect* PEffectMgr::GetEffectFromSet( int setID, int idx )
{
    if (setID < 0 || setID >= m_EffectSet.size()) return NULL;
    EffectSet& effs = m_EffectSet[setID];
    if (idx < 0 || idx >= effs.m_Effect.size()) return NULL;
    return (PEffect*)effs.m_Effect[idx];
} // PEffectMgr::GetEffectFromSet

/*****************************************************************************/
/*	PEmitterInstance implemetation
/*****************************************************************************/
void PEmitterInstance::Reset()
{
	m_pParticle		= NULL;
	m_EmitAccum		= 0.0f;
	m_CurTime		= 0.0f;
	m_LastTime		= 0.0f;
	m_ParticleID	= -1;
	m_ParentID		= -1;
	m_Flags			= 0;
	m_LastFrame		= 0;
} // PEmitterInstance::Reset

int PEmitterInstance::GetNBorn() const
{
    int nP = 0;
    PParticle* p = m_pParticle;
    while (p)
    {
        if (p->m_Age == 0.0f) nP++;
        p = p->m_pNext;
    }
    return nP;
} // PEmitterInstance::GetNBorn

/*****************************************************************************/
/*	PEffect implemetation
/*****************************************************************************/
PEffect::PEffect()
{
	m_EffectID		= 0xFFFFFFFF;	
	m_InstanceID	= 0xFFFFFFFF;
    m_BoneName      = "";  
    m_StartTime     = 0.0f; 
    m_ActiveTime    = 0.0f;
	m_WarmupTime	= 0.0f;
	m_Priority      = 0;
    m_Flags         = 0;
} // PEffect::PEffect

void PEffect::SetEffectFile( const char* fname ) 
{ 
    char path[_MAX_PATH];
    strcpy( path, fname );
    ToRelativePath( path, _MAX_PATH );
	m_EffectFile	= path; 
	m_EffectID		= IMM->GetModelID( fname ); 
	m_InstanceID	= 0xFFFFFFFF; 
} // PEffect::SetEffectFile

void PEffect::Render()
{
    //  HACK to disable rendering effect into the shadowmap
    if (DeviceStateSet::IsFrosen()) return;

	PushEntityContext( GetID() );
	if (m_EffectID == 0xFFFFFFFF) m_EffectID = IMM->GetModelID( m_EffectFile.c_str() ); 

	DWORD instID = IEffMgr->InstanceEffect( m_EffectID );
	if (m_InstanceID != instID) IEffMgr->RewindEffect( instID, m_WarmupTime );
	m_InstanceID = instID;
    
	Matrix4D m = TransformNode::TMStackTop();
    m.mulLeft( tm );  
    IEffMgr->UpdateInstance( m_InstanceID, m );
    PopEntityContext();

    if (DoDrawGizmo())
    {
        IRS->ResetWorldMatrix();
        rsEnableZ( false );
        DrawSphere( Sphere( m.getTranslation(), 5.0f ), 0, 0xFFFFFFFF, 8 );
        rsFlushLines3D();
    }
} // PEffect::Render

void PEffect::Expose( PropertyMap& pm )
{
	pm.start( "PEffect", this );
    pm.p( "Name",		GetName,        SetName	        );
    pm.p( "Invisible",	IsInvisible,    SetInvisible	);
    pm.p( "DrawGizmo",	DoDrawGizmo,    SetDrawGizmo	);
    pm.p( "DrawAABB",	DoDrawAABB,     SetDrawAABB     );
	pm.f( "WarmupTime", m_WarmupTime					);
    pm.p( "Scale",      GetScaleX,      SetScaleX       );
    pm.p( "PosX",       GetPosX,        SetPosX         );
    pm.p( "PosY",       GetPosY,        SetPosY         );
    pm.p( "PosZ",       GetPosZ,        SetPosZ         );
	pm.p( "RotX",       GetEulerX,      SetEulerX		);
	pm.p( "RotY",       GetEulerY,      SetEulerY		);
	pm.p( "RotZ",       GetEulerZ,      SetEulerZ		);
    pm.p( "Priority",   GetPriority,    SetPriority     );
    pm.p( "DefaultVisible", IsDefaultVisible, SetDefaultVisible );
	pm.p( "EffectFile",	GetEffectFile,	SetEffectFile, "file|Models\\Effects" );
} // PEffect::Expose

void PEffect::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
    DWORD m_Reserved = 0;
	os << m_EffectFile << m_Priority << m_BoneName << 
        m_StartTime << m_ActiveTime << m_WarmupTime << m_Flags << m_Reserved;
} // PEffect::Serialize

void PEffect::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
    DWORD m_Reserved = 0;
	is >> m_EffectFile >> m_Priority >> m_BoneName >> 
        m_StartTime >> m_ActiveTime >> m_WarmupTime >> m_Flags >> m_Reserved;
} // PEffect::Unserialize

XMLNode* PEffect::ToXML() const
{
    XMLNode* pRoot = new XMLNode();
    pRoot->SetTag( "Effect" );
    pRoot->AddAttr( "Name", GetName() );
    pRoot->AddValue( "EffectFile", GetEffectFile() );
    pRoot->AddValue( "ParentBone", GetParentBoneName() );
    pRoot->AddValue( "Position", GetPos() );
    float scale = GetScaleX();
    if (fabs( scale - 1.0f) > c_SmallEpsilon) pRoot->AddValue( "Scale", scale );
    if (m_Priority > 0)         pRoot->AddValue( "Priority", m_Priority );
    if (m_WarmupTime > 0.0f)    pRoot->AddValue( "Warmup", m_WarmupTime );
    pRoot->AddValue( "StartTime", m_StartTime );
    pRoot->AddValue( "ActiveTime", m_ActiveTime );
    if (IsDefaultVisible()) pRoot->AddValue( "DefaultVisible", true );
    return pRoot;
} // PEffect::ToXML

const char* PEffect::GetParentBoneName() const
{
    Node* pParent = GetParent();
    if (!pParent) return m_BoneName.c_str();
    ((PEffect*)this)->m_BoneName = pParent->GetName();
    return m_BoneName.c_str();
} // PEffect::GetParentBoneName

bool PEffect::FromXML( XMLNode* node )
{
    if (!node) return false;
    const char* name = NULL;
    if (node->GetAttr( "Name", name )) SetName( name );
    if (node->GetValue( "EffectFile", name )) SetEffectFile( name );
    if (node->GetValue( "ParentBone", name )) m_BoneName = name;
    node->GetValue( "StartTime", m_StartTime );
    node->GetValue( "ActiveTime", m_ActiveTime );
    node->GetValue( "Priority", m_Priority );
    node->GetValue( "Warmup", m_WarmupTime );
    bool defVisible = false;
    node->GetValue( "DefaultVisible", defVisible );
    SetDefaultVisible( defVisible );
    Vector3D pos;
    if (node->GetValue( "Position", pos )) SetPos( pos );
    float scale = 1.0f;
    if (node->GetValue( "Scale", scale )) 
    {
        SetScaleX( scale );
        SetScaleY( scale );
        SetScaleZ( scale );
    }
    return false;
} // PEffect::FromXML

/*****************************************************************************/
/*	Overlay implemetation
/*****************************************************************************/
Overlay::Overlay()
{
	m_SizeX		= 256.0f;
	m_SizeY		= 256.0f;

	m_ShiftX	= 0.0f;
	m_ShiftY	= 0.0f;

	m_USpeed	= 0.0f;
	m_VSpeed	= 0.0f;
	m_U2Speed	= 0.0f;
	m_V2Speed	= 0.0f;

	m_Color		= 0xFFFFFFFF;

	m_UV		= Rct::unit;
	m_UV2		= Rct::unit;

	SetAnimated( false );
    SetShiftedBack( false );
} // Overlay::Overlay

void Overlay::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "Overlay", this );
	pm.f( "SizeX",		m_SizeX  );
	pm.f( "SizeY",		m_SizeY  );
	pm.f( "ShiftX",		m_ShiftX );
	pm.f( "ShiftY",		m_ShiftY );

	pm.f( "USpeed",		m_USpeed  );
	pm.f( "VSpeed",		m_VSpeed  );
	pm.f( "U2Speed",	m_U2Speed );
	pm.f( "V2Speed",	m_V2Speed );

	pm.f( "Color",		m_Color, "color" );

	pm.f( "u",			m_UV.x );
	pm.f( "v",			m_UV.y );
	pm.f( "du",			m_UV.w );
	pm.f( "dv",			m_UV.h );

	pm.f( "u2",			m_UV2.x );
	pm.f( "v2",			m_UV2.y );
	pm.f( "du2",		m_UV2.w );
	pm.f( "dv2",		m_UV2.h );

	pm.p( "Animate",	IsAnimated, SetAnimated );
    pm.p( "ShiftBack",	IsShiftedBack, SetShiftedBack );
} // Overlay::Expose

void Overlay::Render()
{
    if (DeviceStateSet::IsFrosen()) return;

    float t = float( GetTickCount() )*0.001f;
	if (IsAnimated())
	{
		m_UV.x  = t*m_USpeed;
		m_UV.y  = t*m_VSpeed;
		m_UV2.x = t*m_U2Speed;
		m_UV2.y = t*m_V2Speed;
        m_UV.x  -= floorf( m_UV.x  );
        m_UV.y  -= floorf( m_UV.y  );
        m_UV2.x -= floorf( m_UV2.x );
        m_UV2.y -= floorf( m_UV2.y );
	}

	Vector3D a( -1.0f,  1.0f, 0.0f );
	Vector3D b(  1.0f,  1.0f, 0.0f );
	Vector3D c( -1.0f, -1.0f, 0.0f );
	Vector3D d(  1.0f, -1.0f, 0.0f );

	Matrix4D tm = Matrix3D::identity;
	tm.translation( m_ShiftX/m_SizeX, m_ShiftY/m_SizeY, 0.0f );
	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (pCam)
	{
		tm *= Matrix4D( Vector3D( m_SizeX, m_SizeY, 1.0f ), 
						Matrix3D( pCam->GetRight(), pCam->GetUp(), pCam->GetDir() ),
						Vector3D::null );
	}

    const float c_ZShift = 4.0f;
    if (IsShiftedBack())
    {
        pCam->ShiftZ( -c_ZShift );
    }

	tm.translate( TransformNode::TMStackTop().getTranslation() );
	tm.transformPt( a );
	tm.transformPt( b );
	tm.transformPt( c );
	tm.transformPt( d );

	rsFlushPoly3D();
    IRS->ResetWorldMatrix();
    Parent::Render();
    static int shID = IRS->GetShaderID( "overlay" );
    IRS->SetCurrentShader( shID );
	rsQuad( a, b, c, d, m_UV, m_UV2, m_Color );
	rsFlushPoly3D( false );

    if (IsShiftedBack())
    {
        pCam->ShiftZ( c_ZShift );
    }

} // Overlay::Render

void Overlay::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << m_SizeX << m_SizeY << m_ShiftX << m_ShiftY << 
			m_USpeed << m_VSpeed << m_U2Speed << 
			m_V2Speed << m_Color << m_UV << m_UV2 << m_Flags;
} // Overlay::Serialize

void Overlay::Unserialize( InStream& is  )
{
	Parent::Unserialize( is );
	is >> m_SizeX >> m_SizeY >> m_ShiftX >> m_ShiftY >> 
			m_USpeed >> m_VSpeed >> m_U2Speed >> 
			m_V2Speed >> m_Color >> m_UV >> m_UV2 >> m_Flags;
} // Overlay::Unserialize

END_NAMESPACE(sg)