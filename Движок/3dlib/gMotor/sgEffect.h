/*****************************************************************************/
/*	File:	sgEffect.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	20-04-2004
/*****************************************************************************/
#ifndef __SGEFFECT_H__
#define __SGEFFECT_H__
#include "IEffectManager.h"
#include "kHash.hpp"
#include "kContext.h"

/*****************************************************************************/
/*	Enum:	EffectBlendMode
/*****************************************************************************/
enum EffectBlendMode
{
	bmUnknown		= 0,
	bmAdd			= 1,
	bmMul			= 2,
}; // enum EffectBlendMode

/*****************************************************************************/
/*	Enum:	EffectIntensity
/*****************************************************************************/
enum EffectIntensity
{
	bmNormal		= 0,
	bmIntense		= 1,
	bmSuperIntense	= 2,
}; // enum EffectIntensity

/*****************************************************************************/
/*	Enum:	EffectZBias
/*****************************************************************************/
enum EffectZBias
{
	zbStage0			= 0,
	zbStage1			= 1,
	zbStage2			= 2,
	zbStage3			= 3,
}; // enum EffectZBias

/*****************************************************************************/
/*	Enum:	EffectTrigger
/*****************************************************************************/
enum EffectTrigger
{
	etUnknown			= 0,
	etCreateEffect		= 1,	//  creates another effect instance
	etDeath				= 2,	//  kill particle
	etReflect			= 3,	//  change particle velocity appropriately
	etResetAge			= 4,	//  reset particle age
	etStop				= 5,	//	reset particle velocity
	etStopRotation		= 6,	//	reset particle angular velocity
}; // enum EffectZBias

/*****************************************************************************/
/*	Enum:	InfluenceAxis
/*****************************************************************************/
enum InfluenceAxis
{
	aUnknown		= 0,
	aX				= 1,
	aY				= 2,
	aXY				= 3,
	aZ				= 4,
	aXZ				= 5,
	aYZ				= 6,
	aXYZ			= 7
}; // enum InfluenceAxis

/*****************************************************************************/
/*	Enum:	OrbitType
/*****************************************************************************/
enum OrbitType
{
	otUnknown		= 0,
	otSphere		= 1,
	otCylinder		= 2,
	otCircle		= 3
}; // enum OrbitType

/*****************************************************************************/
/*	Enum:	BillboardAlignment
/*****************************************************************************/
enum BillboardAlignment
{
	baUnknown		= 0,
	baNone			= 1,	//  no alignment at all
	baCamera		= 2,	//  always faced to camera
	baPlane			= 3,	//	aligned to the plane
	baBeam			= 4		//	aligned to camera, but keeping direction along principal axis
}; // enum BillboardAlignment

/*****************************************************************************/
/*	Enum:	SphereType
/*****************************************************************************/
enum SphereType
{
	stIcosa2		= 0,
	stIcosa3		= 1,
	stIcosa4		= 2
}; // enum BillboardAlignment

ENUM( EffectBlendMode, "EffectBlendMode", 
		en_val( bmAdd,	"Additive"			) <<
		en_val( bmMul,	"Multiplicative"	) );

ENUM( EffectIntensity, "EffectIntensity", 
		en_val( bmNormal,		"Normal"		) <<
		en_val( bmIntense,		"Intense"		) << 
		en_val( bmSuperIntense,	"SuperIntense"	) );

ENUM( EffectZBias, "EffectZBias", 
	 	en_val( zbStage0,	"None"		) <<
	 	en_val( zbStage1,	"Low"		) <<
	 	en_val( zbStage2,	"Medium"	) <<
	 	en_val( zbStage3,	"High"		) );

ENUM( EffectTrigger, "EffectTrigger", 
	 	en_val( etCreateEffect,	"CreateEffect"	) <<
	 	en_val( etDeath,		"Death"			) <<
	 	en_val( etReflect,		"Deflect"		) <<
	 	en_val( etResetAge,		"ResetAge"		) <<
		en_val( etStop,			"Stop"			) <<
		en_val( etStopRotation,	"StopRotation"	) );

ENUM( InfluenceAxis, "InfluenceAxis", 
	 	en_val( aX,		"X"		) <<
	 	en_val( aY,		"Y"		) <<
	 	en_val( aZ,		"Z"		) <<
	 	en_val( aXY,	"XY" 	) <<
	 	en_val( aYZ,	"YZ" 	) <<
	 	en_val( aXZ,	"XZ" 	) <<
	 	en_val( aXYZ,	"XYZ"	) );

ENUM( OrbitType, "OrbitType", 
	 	en_val( otSphere,	"Sphere"	) <<
	 	en_val( otCylinder,	"Cylinder"	) << 
	 	en_val( otCircle,	"Circle"	) );

ENUM( BillboardAlignment, "BillboardAlignment", 
	 en_val( baNone	,	"None"		) <<
	 en_val( baCamera,	"Camera"	) <<
	 en_val( baPlane,	"Plane"		) <<
	 en_val( baBeam,	"Beam"		) );

ENUM( SphereType, "SphereType", 
	 en_val( stIcosa2,	"Icosa2"	) <<
	 en_val( stIcosa3,	"Icosa3"	) <<
	 en_val( stIcosa4,	"Icosa4"	) );

namespace sg{

/*****************************************************************************/
/*	Enum:	ParticleFlag
/*	Desc:	Different particle flags
/*****************************************************************************/
enum ParticleFlag
{
	pfDead			= 0x00000001,	//  whether particle is dead	
	pfJustBorn		= 0x00000002,	//  paricle was just emitted
	pfJustHit		= 0x00000004,	//  particle just hit something
	pfJustDied		= 0x00000008,	//  particle lifetime just depleted
	pfEmitter		= 0x00000010,	//  particle is emitter itself
	pfUsedSlot		= 0x00000020,	//  particle slot is used by some particle
	pfImmortal		= 0x00000040	//  particle is goretz
}; // enum ParticleFlag

/*****************************************************************************/
/*	Class:	PParticle	
/*	Desc:	Single particle instance
/*****************************************************************************/
class PParticle
{
public:
	Vector3D		m_Velocity;		//  velocity vector
	Vector3D		m_AngVelocity;	//  angular velocity vector

    Vector3D		m_Size;			//  size, depending of particle type 1, 2, or all 3 components may be used
	Matrix3D        m_Rotation;     //  particle rotation matrix
    float           m_Roll;         //  particle screen space rotation hack
    Vector3D		m_Position;		//  position in the (world) space
    Vector3D        m_PrevPosition; //  previous particle position

	DWORD			m_Color;		//  particle color/transparency AARRGGBB
	float			m_Age;			//	particle age, in seconds
	float			m_TimeToLive;	//  particle's death time

	Rct				m_UV;			//	texture coordinates
	Rct				m_UV2;			//	2nd texture coordinates

	DWORD			m_ID;			//  index at the global array
	DWORD			m_EmitterID;	//  id of the emitter instance, which emitted this particle
	DWORD			m_Flags;		//  particle state flags
	DWORD			m_Frame;		//  current frame for animated particles
	float			m_FrameTime;	//  time spent in current frame

	PParticle*		m_pPrev;		//  pointer to the previously emitted particle
	PParticle*		m_pNext;		//  pointer to the next emitted particle

	void			SetFlag( ParticleFlag f, bool set = true ) { ::SetFlag( m_Flags, f, set ); }
	bool			GetFlag( ParticleFlag f ) const { return ((f&m_Flags) != 0); }
    
	Matrix4D		GetTransform() const 
	{  
        return Matrix4D( m_Size, Matrix3D::identity, m_Position );
	}

    Matrix4D GetFrame() const 
    {  
        return Matrix4D( Vector3D::one, Matrix3D::identity, m_Position );
    }

}; // class PParticle

struct PEmitterInstance;
/*****************************************************************************/
/*	Class:	POperator	
/*	Desc:	Influences particles in some way
/*****************************************************************************/
class POperator : public Node
{
public:
							POperator			();
	virtual void			Expose				( PropertyMap& pm );
	virtual void			OnChangeChildren	();
	virtual void			Process				( PEmitterInstance* pEmitter ){}
	virtual DWORD			GetColor			() const { return 0xFFDDCC44; }
    bool                    IsVisual            () const { return m_bVisual; }

	NODE(POperator,Node,2POP);

protected:
    bool                    m_bVisual;
}; // class POperator

enum PEmitterFlag
{
    efLooped        = 0x01,
    efAutoUpdate    = 0x02,
    efPlayForever   = 0x04,
    efPosDependent  = 0x08
}; // enum PEmitterFlag

/*****************************************************************************/
/*	Class:	PEmitter	
/*	Desc:	Creates particles at given place with given rate
/*****************************************************************************/
class PEmitter : public POperator
{
protected:

	float					m_StartTime;	//  time to start emitting particles (from parent effect start time)
	float					m_TotalTime;	//  total time for which this emitter is active
	int						m_MaxParticles; //  maximal number of simultaneously alive particle emitted by this emitter
	bool					m_bWorldSpace;  //  particle cluster is in world space, else it is local to parent
    
    BYTE                    m_Flags;        //  flags set

	float					m_TimeToLive;	//	particle time-to-live mean
	float					m_TimeToLiveV;	//	particle time-to-live variation

    float                   m_Probability;  //  probability to activate emitter (when it is taking part in the trigger)

	std::vector<POperator*>	m_Operators;	//  list of the operators
	PEmitter*				m_pParentEmitter;//  parent emitter

	int						m_AttachTo;		//  index of particle we are coupled to (if any)

	friend class			PEffectMgr;
public:
							PEmitter		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			OnChangeChildren();
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	virtual DWORD			GetColor		() const { return 0xFFFFB9B9; }

	void					AddOperator		( POperator* pOp ) { m_Operators.push_back( pOp ); }
	int						GetNOperators	() const { return m_Operators.size(); }
	POperator*				GetOperator		( int idx ) { return m_Operators[idx]; }

    float                   GetProbability  () const { return m_Probability; }
    void                    SetProbability  ( float val ) { m_Probability = val; }

	virtual void			Process			( PEmitterInstance* pEmitter );

	float					GetTotalTime	() const { return m_TotalTime; }
	float					GetStartTime	() const { return m_StartTime; }

    bool                    IsLooped        () const { return ((m_Flags&efLooped) != 0); }
    bool                    IsAutoUpdated   () const { return ((m_Flags&efAutoUpdate) != 0); }
    bool                    IsPlayedForever () const { return ((m_Flags&efPlayForever) != 0); }
    bool                    IsPosDependent  () const { return ((m_Flags&efPosDependent) != 0); }
    bool                    IsWorldSpace    () const { return m_bWorldSpace; }

    void                    SetLooped       ( bool val ) { SetFlag( m_Flags, efLooped, val ); }
    void                    SetAutoUpdated  ( bool val ) { SetFlag( m_Flags, efAutoUpdate, val ); }
    void                    SetPlayedForever( bool val ) { SetFlag( m_Flags, efPlayForever, val ); }
    void                    SetPosDependent ( bool val ) { SetFlag( m_Flags, efPosDependent, val ); }
    void                    SetWorldSpace   ( bool val ) { m_bWorldSpace = val; }
    

	//  defined in children emitter type classes, gets number of particles emitted at this update moment
	//	curTime  - current update time, from parent effect start time
	//  prevTime - previous update time
	//  nEmitted - number of particles already emitted
	virtual int				NumToEmit		( PEmitterInstance* pInstance ) { return 0; }
	virtual float			GetTimeToLive	( const PParticle& p );

	NODE(PEmitter,POperator,2EMI);
protected:
}; // class PEmitter

/*****************************************************************************/
/*	Class:	PConstEmitter	
/*	Desc:	Particles are emitted with constant rate
/*****************************************************************************/
class PConstEmitter : public PEmitter
{
	float					m_Rate;		//  number of particles per second
public:
							PConstEmitter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	virtual int				NumToEmit		( PEmitterInstance* pInstance );

	NODE(PConstEmitter,PEmitter,2CMI);
protected:
}; // class PConstEmitter

/*****************************************************************************/
/*	Class:	PStaticEmitter	
/*	Desc:	Single particle living forever
/*****************************************************************************/
class PStaticEmitter : public PEmitter
{
public:
							PStaticEmitter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	virtual int				NumToEmit		( PEmitterInstance* pInstance );

	void					SetStaticTotalTime ( float val ) { m_TotalTime = val; m_TimeToLive = val; }
	float					GetStaticTotalTime () const { return m_TotalTime; }

	NODE(PStaticEmitter,PEmitter,2SMI);
protected:
}; // class PStaticEmitter

/*****************************************************************************/
/*	Class:	PBurstEmitter	
/*	Desc:	Emits bunch of particles at once at start time
/*****************************************************************************/
class PBurstEmitter : public PEmitter
{
	int						m_PNumber;

public:
							PBurstEmitter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	virtual int				NumToEmit		( PEmitterInstance* pInstance );

	NODE(PBurstEmitter,PEmitter,2BMI);
protected:
}; // class PBurstEmitter

/*****************************************************************************/
/*	Class:	PRampEmitter	
/*	Desc:	Emitter with keyframed emit rate
/*****************************************************************************/
class PRampEmitter : public PEmitter
{
public:
							PRampEmitter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	virtual int				NumToEmit		( PEmitterInstance* pInstance );

	NOT_IMPLEMENTED

	NODE(PRampEmitter,PEmitter,2KMI);
protected:
}; // class PRampEmitter

/*****************************************************************************/
/*	Class:	PModularEmitter	
/*	Desc:	Complex effect, consisting of the multiple emitters
/*****************************************************************************/
class PModularEmitter : public PStaticEmitter
{
	int						m_RndSeed;			//  seed used in psevdo-random number generator

public:
							PModularEmitter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	NODE(PModularEmitter,PStaticEmitter,2EFF);
protected:
}; // class PModularEmitter

/*****************************************************************************/
/*	Class:	PInitializer	
/*	Desc:	Initializes particle cluster in some specific manner
/*****************************************************************************/
class PInitializer : public POperator
{
public:
	virtual void			InitParticle	( PParticle& p ){}
	virtual void			Process			( PEmitterInstance* pEmitter );

	virtual DWORD			GetColor		() const { return 0xFFFFBB77; }

	NODE(PInitializer,POperator,2PIN);

protected:
}; // class PInitializer

/*****************************************************************************/
/*	Class:	PShooter	
/*	Desc:	Gives initial velocity to the particles
/*****************************************************************************/
class PShooter : public PInitializer
{
	float					m_Velocity;
	float					m_VelocityD;

	Vector3D				m_AngVelocity;
	Vector3D				m_AngVelocityD;

	bool					m_bAffectRotation;

public:
							PShooter		();
	virtual void			Process			( PEmitterInstance* pEmitter );
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual Vector3D		GetDirection	( const PParticle& p ){ return Vector3D::null; }

	virtual DWORD			GetColor		() const { return 0xFF33B7FF; }

	NODE(PShooter,POperator,2SHO);

protected:
}; // class PShooter

/*****************************************************************************/
/*	Class:	PConeShooter	
/*	Desc:	Gives particles random initial velocity from the cone domain
/*****************************************************************************/
class PConeShooter : public PShooter
{
	float					m_ConeAngle;
	bool					m_bPlanar;

public:
							PConeShooter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
    virtual void			Render          ();
	virtual Vector3D		GetDirection	( const PParticle& p );

	NODE(PConeShooter,PShooter,2PCS);

protected:
}; // class PConeShooter

/*****************************************************************************/
/*	Class:	PDirectShooter	
/*	Desc:	Gives particles fixed direction initial velocity 
/*****************************************************************************/
class PDirectShooter : public PShooter
{
	Vector3D				m_Direction;

public:
							PDirectShooter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual Vector3D		GetDirection	( const PParticle& p );
    virtual void            Render          ();

	NODE(PDirectShooter,PShooter,2PDS);

protected:
}; // class PDirectShooter

/*****************************************************************************/
/*	Class:	PRadialShooter	
/*	Desc:	Shoots particle in random direction
/*****************************************************************************/
class PRadialShooter : public PShooter
{
	bool					m_bBindToOrigin;
	bool					m_bPlanar;

public:
							PRadialShooter	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual Vector3D		GetDirection	( const PParticle& p );

	NODE(PRadialShooter,PShooter,2PRS);

protected:
}; // class PRadialShooter

/*****************************************************************************/
/*	Class:	PRampShooter	
/*	Desc:	Shoots particle with keyframed velocity
/*****************************************************************************/
class PRampShooter : public PShooter
{
	AlphaRamp				m_Ramp;			//  keyframed by emitter time particle velocity
	float					m_Variation;	//  random variation
	int						m_Repeat;		//  number of repeats per emitter life

public:
							PRampShooter		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }

	AlphaRamp*				GetRamp			() const { return (AlphaRamp*)&m_Ramp; }
	void					SetRamp			( AlphaRamp* pRamp ) { assert( pRamp == &m_Ramp ); }

	NODE(PRampShooter,PShooter,2SHR);
protected:
}; // class PRampShooter

/*****************************************************************************/
/*	Class:	PPlacer	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PPlacer : public PInitializer
{
protected:
	Vector3D				m_Position;     // position of the center of the placer's domain

public:
							PPlacer			() : m_Position( Vector3D::null ) {}
	virtual Vector3D		GetPosition		( const PParticle& p ) { return Vector3D::null; }
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );
	virtual DWORD			GetColor		() const { return 0xFFE6ADF8; }
    virtual void            Render          ();

	NODE(PPlacer,PInitializer,2IPL);

protected:
}; // class PPlacer

/*****************************************************************************/
/*	Class:	PSpherePlacer	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PSpherePlacer : public PPlacer
{
	float					m_Radius;
	bool					m_bSurface;
	bool					m_bPlanar;

public:
							PSpherePlacer	();
	virtual Vector3D		GetPosition		( const PParticle& p );
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
    virtual void            Render          ();

	NODE(PSpherePlacer,PPlacer,2SPL);

protected:
}; // class PSpherePlacer

/*****************************************************************************/
/*	Class:	PBoxPlacer	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PBoxPlacer : public PPlacer
{
	float					m_DX;
	float					m_DY;
	float					m_DZ;
	bool					m_bPlanar;

public:
							PBoxPlacer		();
	virtual Vector3D		GetPosition		( const PParticle& p );
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
    virtual void            Render          ();


	NODE(PBoxPlacer,PPlacer,2BPL);

protected:
}; // class PBoxPlacer

/*****************************************************************************/
/*	Class:	PCylinderPlacer	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PCylinderPlacer : public PPlacer
{
	float					m_Radius;
	float					m_Height;
	bool					m_bTopCap;
	bool					m_bBottomCap;
	bool					m_bSphericalCaps;
	bool					m_bOnSurface;

public:
							PCylinderPlacer	();
	virtual Vector3D		GetPosition		( const PParticle& p );
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
    virtual void            Render          ();

	NODE(PCylinderPlacer,PPlacer,2CPL);

protected:
}; // class PCylinderPlacer

/*****************************************************************************/
/*	Class:	PCirclePlacer	
/*	Desc:	Gives initial position in the consequent points on the circle 
/*****************************************************************************/
class PCirclePlacer : public PPlacer
{
	float					m_Radius;
	float					m_Velocity;

public:
							PCirclePlacer	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PCirclePlacer,PPlacer,2CIL);

protected:
}; // class PCirclePlacer

/*****************************************************************************/
/*	Class:	PLinePlacer	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PLinePlacer : public PPlacer
{
	Vector3D				m_End;		//  line endpoint

public:
							PLinePlacer		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
    virtual void            Render          ();
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PLinePlacer,PPlacer,2LPL);

protected:
}; // class PLinePlacer

/*****************************************************************************/
/*	Class:	PPointPlacer	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PPointPlacer : public PPlacer
{

public:
	virtual Vector3D		GetPosition		( const PParticle& p ) { return Vector3D::null; }
	
	NODE(PPointPlacer,PPlacer,2PPP);

protected:
}; // class PPointPlacer

/*****************************************************************************/
/*	Class:	PModelPlacer	
/*	Desc:	Places particles on the surface of the some model's mesh
/*****************************************************************************/
class PModelPlacer : public PPlacer
{
    std::string             m_ModelName;
    DWORD                   m_ModelID;
    float                   m_Scale;

public:
                            PModelPlacer    ();
    virtual void            Process         ( PEmitterInstance* pEmitter );
    virtual void			Expose			( PropertyMap& pm );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );
    virtual void            Render          ();
    
    void                    SetModelFile    ( const char* file );
    const char*             GetModelFile    () const { return m_ModelName.c_str(); }

    NODE(PModelPlacer,PPlacer,2MPL);
protected:
}; // class PModelPlacer

/*****************************************************************************/
/*	Class:	CoastNode	
/*	Desc:	Describes node in coastal chain node
/*****************************************************************************/
class CoastNode
{
public:
    Vector3D        m_Pos;      //  node position
    Vector3D        m_Dir;      //  normal direction

    int             m_CellX;    
    int             m_CellY;    

    CoastNode       ( const Vector3D& p )  { m_Pos = p; }
    CoastNode       () {}

    unsigned int hash() const
    {
        unsigned int h = m_CellX + 541*m_CellY;
        h = (h << 13) ^ h;
        h += 15731;
        return h;  
    }

    bool equal( const CoastNode& el )
    {
        return (m_CellX == el.m_CellX) && (m_CellY == el.m_CellY);
    }

    void copy( const CoastNode& el )
    {
        m_Pos   = el.m_Pos;
        m_Dir   = el.m_Dir;
        m_CellX = el.m_CellX;
        m_CellY = el.m_CellY;
    }
};  // class CoastNode

typedef Hash<CoastNode>     CoastHash;
/*****************************************************************************/
/*	Class:	PCoastLinePlacer	
/*	Desc:	Places particles on the terrain coast line (to simulate waves)
/*****************************************************************************/
class PCoastLinePlacer : public PPlacer
{
    float                   m_Width;        //  width of the processed area
    float                   m_Height;       //  height of the processed area
    float                   m_Distance;     //  distance from the coast where we are spawned
    
    float                   m_GridStep;
    float                   m_HeightDelta;
    float                   m_WaterLevel;
    int                     m_NSamples;     //  number of samples when iterative picking coast points
    
    float                   m_WaveFront;    //  width of wave front 
    float                   m_WaveFrontBend;//  parabolic wave front bending ratio

    float                   m_SlopeBias;

    static CoastHash        s_Coast;        //  cached coast line description

public:
                            PCoastLinePlacer();
    virtual void            Process         ( PEmitterInstance* pEmitter );
    virtual void			Expose			( PropertyMap& pm );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );
    virtual void			Render          ();

    void                    UpdateCache     ();

    void                    SetCoastDistance( float val );
    void                    SetZoneWidth    ( float val );
    void                    SetZoneHeight   ( float val );

    float                   GetCoastDistance() const { return m_Distance;   }
    float                   GetZoneWidth    () const { return m_Width;      }
    float                   GetZoneHeight   () const { return m_Height;     }

    int                     GetNPoints      () const { return s_Coast.numElem(); }

    NODE(PCoastLinePlacer,PPlacer,2CLP);
protected:
}; // class PCoastLinePlacer

/*****************************************************************************/
/*	Class:	PColorInit	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PColorInit : public PInitializer
{
	DWORD					m_AvgColor;	//  color mean value (including alpha) 
	int						m_R, m_G, m_B, m_A; // cached mean color components

	int						m_RedV;		//	red channel variation (0-256)
	int						m_GreenV;	//	green channel variation
	int						m_BlueV;	//	blue channel variation
	int						m_AlphaV;	//	alpha channel variation

public:
							PColorInit		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			InitParticle	( PParticle& p );

	void					SetAvgColor		( DWORD val );
	DWORD					GetAvgColor		() const { return m_AvgColor; }

	NODE(PColorInit,PInitializer,2CLI);

protected:
}; // class PColorInit

/*****************************************************************************/
/*	Class:	PColorRampInit	
/*	Desc:	
/*****************************************************************************/
class PColorRampInit : public PInitializer
{
	ColorRamp				m_Ramp;

public:
							PColorRampInit	();
	ColorRamp*				GetColorRamp	() const { return (ColorRamp*)&m_Ramp; }
	void					SetColorRamp	( ColorRamp* pRamp ) { assert( pRamp == &m_Ramp ); }

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }

	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			InitParticle	( PParticle& p );

	NODE(PColorRampInit,POperator,2CRI);
}; // class PColorRampInit

/*****************************************************************************/
/*	Class:	PSizeInit	
/*	Desc:	Gives initial position to the particles
/*****************************************************************************/
class PSizeInit : public PInitializer
{
	Vector3D				m_Size;		//  particle size mean value (on all three axes)
	float					m_SizeV;	//  size variation

public:
							PSizeInit		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			InitParticle	( PParticle& p );

	NODE(PSizeInit,PInitializer,2SZI);

protected:
}; // class PSizeInit

enum PFrameInitFlags
{
    fifSecondChannel    = 0x01,
    fifSequentialInit   = 0x02
}; // enum PFrameInitFlags

/*****************************************************************************/
/*	Class:	PFrameInit	
/*	Desc:	Gives initial position on the texture surface
/*****************************************************************************/
class PFrameInit : public PInitializer
{
	int						m_NRows;		//  number of frame rows
	int						m_NCols;		//  number of frame columns

    BYTE                    m_Flags;        

public:
							PFrameInit		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			InitParticle	( PParticle& p );

	bool					IsSecondChannel     () const { return ((m_Flags & fifSecondChannel ) != 0); }
    void                    SetIsSecondChannel  ( bool val ) { SetFlag( m_Flags, fifSecondChannel, val ); }

    bool					IsSequentialInit    () const { return ((m_Flags & fifSequentialInit ) != 0); }
    void                    SetIsSequentialInit ( bool val ) { SetFlag( m_Flags, fifSequentialInit, val ); }

	NODE(PFrameInit,PInitializer,2FZI);
protected:
}; // class PFrameInit

/*****************************************************************************/
/*	Class:	PTrigger	
/*	Desc:	Triggers events depending on particle state
/*****************************************************************************/
class PTrigger : public POperator
{
public:
							PTrigger		() :	m_pEmitter(NULL), 
													m_TriggerType( etCreateEffect ), 
													m_Damping(0.0f),
													m_Timeout(0.0f) {}

	virtual void			Trigger			( const PEmitterInstance* pEmitter, PParticle& p );
	virtual void			Deflect			( PParticle& p ){}
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			OnChangeChildren();

	virtual DWORD			GetColor		() const { return 0xFFB3D8F2; }

	NODE(PTrigger,POperator,2TRI);

protected:
    PEmitter*				m_pEmitter;
    float					m_Timeout;		//  apply trigger not earlier than timeout
    EffectTrigger			m_TriggerType;	//	triggered action type
	float					m_Damping;		//  triggered action intensity, if possible

}; // class PTrigger

/*****************************************************************************/
/*	Class:	POnDeath	
/*	Desc:	Triggers event when particle dies
/*****************************************************************************/
class POnDeath : public PTrigger
{
    bool                    m_bInheritPosition;

public:
                            POnDeath        () : m_bInheritPosition(true) {}
	virtual void			Process         ( PEmitterInstance* pEmitter );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );
    bool                    InheritPosition () const { return m_bInheritPosition; }

    virtual void			Expose			( PropertyMap& pm );

	NODE(POnDeath,PTrigger,2OND);
protected:
}; // class POnDeath

/*****************************************************************************/
/*	Class:	POnBirth	
/*	Desc:	Triggers event when particle is born
/*****************************************************************************/
class POnBirth : public PTrigger
{
public:

	virtual void			Process( PEmitterInstance* pEmitter );

	NODE(POnBirth,PTrigger,2ONB);
protected:
}; // class POnBirth

/*****************************************************************************/
/*	Class:	POnHitGround	
/*	Desc:	Triggers event when particle hits terrain
/*****************************************************************************/
class POnHitGround : public PTrigger
{
public:

	virtual void			Process( PEmitterInstance* pEmitter );
	virtual void			Deflect( PParticle& p );
	
	NODE(POnHitGround,PTrigger,2OHG);
protected:
}; // class POnHitGround

/*****************************************************************************/
/*	Class:	POnHitWater	
/*	Desc:	Triggers event when particle hits water
/*****************************************************************************/
class POnHitWater : public PTrigger
{
public:

	virtual void			Process( PEmitterInstance* pEmitter );
	virtual void			Deflect( PParticle& p );

	NODE(POnHitWater,PTrigger,2OHW);
protected:
}; // class POnHitWater

/*****************************************************************************/
/*	Class:	POnTimer	
/*	Desc:	Triggers event when some amount of time passes
/*****************************************************************************/
class POnTimer : public PTrigger
{
	float					m_Time;		//  time passed from particle birth
public:
							POnTimer		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Deflect			( PParticle& p );

	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(POnTimer,PTrigger,2ONT);
protected:
}; // class POnTimer

enum PTargetSource
{
    tsFixedTarget       = 0x01,     //  target is fixed, taken from internal position
    tsRootTarget        = 0x02,     //  target is propagated from root of the effect
    tsChildParticle     = 0x03      //  target is taken from any particle in child effect
}; // enum PTargetFlags

}; // namespace sg
ENUM( sg::PTargetSource, "PTargetSource", 
            en_val( sg::tsFixedTarget,	    "FixedTarget"	) <<
            en_val( sg::tsRootTarget,	    "RootTarget"	)/* <<
            en_val( sg::tsChildParticle,	"ChildParticle"	)*/ );
namespace sg{

/*****************************************************************************/
/*	Class:	PTarget	
/*	Desc:	Operator which uses some other entity as target
/*****************************************************************************/
class PTarget : public POperator
{
protected:
	Vector3D		        m_TargetPos;
	Vector3D		        m_TargetDir;
	PTargetSource           m_TargetSource;


public:
							PTarget			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual DWORD			GetColor		() const { return 0xFFFFFF00; }
	virtual void			OnChangeChildren();

	Vector3D				GetTargetPos	( PEmitterInstance* pEmitter ) const;
	
	NODE(PTarget,POperator,2TGT);
protected:
}; // class PTarget

/*****************************************************************************/
/*	Class:	PRetarget	
/*	Desc:	Retargets emitters target to uderlying particle
/*****************************************************************************/
class PRetarget : public POperator
{
public:
                            PRetarget		();
    virtual void			OnChangeChildren();
    virtual void			Process			( PEmitterInstance* pEmitter );

    NODE(PRetarget,POperator,2RTG);
protected:
}; // class PRetarget

/*****************************************************************************/
/*	Class:	PLightning
/*	Desc:	Simulates lightning effect
/*****************************************************************************/
class PLightning : public PTarget
{
	float					m_Spread;	
	float					m_FrameRate;	//  change rate, fps
	float					m_Wildness;		//  rate of the points irregularity along direction

public:
							PLightning		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PLightning,PTarget,2LIG);

private:
	void					Fractalize		( PParticle* pl, PParticle* pr, int nP );
	int						m_NPoints;
}; // class PLightning

/*****************************************************************************/
/*	Class:	PHoming
/*	Desc:	Attracts particles to the target
/*****************************************************************************/
class PHoming : public PTarget
{
public:
							PHoming			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NOT_IMPLEMENTED

	NODE(PHoming,PTarget,2HOM);
protected:
}; // class PHoming

/*****************************************************************************/
/*	Enum:	PRendererFlags
/*****************************************************************************/
enum PRendererFlags
{
	rfUnknown			= 0x00000000,
	rfDoZTest			= 0x00000001,
	rfDoZWrite			= 0x00000002,
	rfScreenSpace		= 0x00000004,
	rfHeadMoveDir		= 0x00000008,
    rfDitherDisable     = 0x00000010,
    rfNullLayer         = 0x00000020
}; // enum PRendererFlags

const int c_ParticleRenderBinSize = 128;
/*****************************************************************************/
/*	Class:	PRenderer	
/*	Desc:	Renders particle cluster in some specific manner
/*****************************************************************************/
class PRenderer : public POperator
{
	std::string				m_TexName;
	std::string				m_TexName2;

protected:
	EffectBlendMode			m_BlendMode;
	EffectIntensity			m_Intensity;
	DWORD					m_Flags;
	DWORD					m_ZBias;

	int						m_TexID;		//  texture handle
	int						m_TexID2;		//  second texture handle
	int						m_ShaderID;		//	shader handle
	DWORD					m_Tint;			//  global color multiplier

    BaseMesh                m_RenderBin;    //  pool for batching particles' geometry 

public:
							PRenderer		();
	virtual void			Expose			( PropertyMap& pm );

	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	bool					IsZTest			() const { return ((m_Flags & rfDoZTest     ) != 0); }
	bool					IsZWrite		() const { return ((m_Flags & rfDoZWrite    ) != 0); }
	bool					IsHeadMoveDir	() const { return ((m_Flags & rfHeadMoveDir ) != 0); }
    bool                    IsDitherEnable  () const { return ((m_Flags & rfDitherDisable)== 0); }
    bool                    IsNullLayer     () const { return ((m_Flags & rfNullLayer   ) != 0); }

	EffectZBias				GetZBias		() const { return  EffectZBias( m_ZBias ); }
	void					SetZBias		( EffectZBias val ) { m_ZBias = val; }

	EffectBlendMode			GetBlendMode	() const { return m_BlendMode; }
	EffectIntensity			GetIntensity	() const { return m_Intensity; }

	void					SetBlendMode	( EffectBlendMode val ) { m_BlendMode = val; UpdateShader(); }
	void					SetIntensity	( EffectIntensity val ) { m_Intensity = val; UpdateShader(); }

	void					SetZTest		( bool val ) { SetFlag( m_Flags, rfDoZTest, val );  UpdateShader(); }
	void					SetZWrite		( bool val ) { SetFlag( m_Flags, rfDoZWrite, val ); UpdateShader(); }
	void					SetHeadMoveDir	( bool val ) { SetFlag( m_Flags, rfHeadMoveDir, val ); }
    void                    SetDitherEnable ( bool val ) { SetFlag( m_Flags, rfDitherDisable, !val ); }
    void                    SetNullLayer    ( bool val ) { SetFlag( m_Flags, rfNullLayer, val ); }

	const char*				GetTexName		() const { return m_TexName.c_str(); }
	void					SetTexName		( const char* val );

	void					SetTexID		( int id ) { }
	int						GetTexID		() const { return m_TexID; }

	const char*				GetTexName2		() const { return m_TexName2.c_str(); }
	void					SetTexName2		( const char* val );

	void					SetTexID2		( int id ) { }
	int						GetTexID2		() const { return m_TexID2; }
	virtual DWORD			GetColor		() const { return 0xFFB0FA8D; }

    virtual bool            FillGeometry    ( PEmitterInstance* pEmitter ) { return true; }
    virtual void            RenderGeometry  ( PEmitterInstance* pEmitter ) {}

	NODE(PRenderer,POperator,2PRE);

protected:

	void					UpdateShader	();
}; // class PRenderer

/*****************************************************************************/
/*	Class:	PModelRenderer	
/*	Desc:	Renders particle as model
/*****************************************************************************/
class PModelRenderer : public PRenderer
{
    std::string             m_ModelName;
    DWORD                   m_ModelID;

public:
							PModelRenderer		();
	virtual void			Expose				( PropertyMap& pm );
    virtual void			RenderGeometry      ( PEmitterInstance* pEmitter );


    const char*             GetModelName        () const { return m_ModelName.c_str(); }
    void                    SetModelName        ( const char* name ) { m_ModelName = name; }

    virtual void			Serialize			( OutStream& os ) const;
    virtual void			Unserialize			( InStream& is  );

	NODE(PModelRenderer,PRenderer,2DRE);
}; // class PModelRenderer

/*****************************************************************************/
/*	Class:	PBillboardRenderer	
/*	Desc:	Renders particle as billboard quads
/*****************************************************************************/
class PBillboardRenderer : public PRenderer
{
	BillboardAlignment		m_Alignment;
	Vector3D				m_RefPoint;

public:
							PBillboardRenderer	();
	virtual void			Expose				( PropertyMap& pm );
	virtual void			Serialize			( OutStream& os ) const;
	virtual void			Unserialize			( InStream& is  );

    virtual bool            FillGeometry    ( PEmitterInstance* pEmitter );
    virtual void            RenderGeometry  ( PEmitterInstance* pEmitter );

	NODE(PBillboardRenderer,PRenderer,2BRE);
}; // class PBillboardRenderer

/*****************************************************************************/
/*	Class:	PConeRenderer	
/*	Desc:	Renders particle as cone primitive
/*****************************************************************************/
class PConeRenderer : public PRenderer
{
	int						m_HeightSegs;	//  number of height segments
	int						m_CircleSegs;	//  number of circle segments
	float					m_TopRadius;	//	top radius of the cone
	float					m_BotRadius;	//  bottom radius of the cone
	float					m_Height;		//	height of the cone
    float					m_TopAlpha;	    //	alpha value at the top
    float					m_BotAlpha;	    //  alpha value at the bottom

	BaseMesh				m_Mesh;
	BaseMesh				m_WorkMesh;

public:
							PConeRenderer	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			RenderGeometry  ( PEmitterInstance* pEmitter );

	void					SetNHSegs		( int val ) { m_HeightSegs = val; UpdateMesh(); }
	void					SetNCSegs		( int val ) { m_CircleSegs = val; UpdateMesh(); }
	
	int						GetNHSegs		() const { return m_HeightSegs; }
	int						GetNCSegs		() const { return m_CircleSegs; }

	void					SetTopR			( float val ) { m_TopRadius = val; UpdateMesh(); }
	void					SetBotR			( float val ) { m_BotRadius = val; UpdateMesh(); }

	float					GetTopR			() const { return m_TopRadius; }
	float					GetBotR			() const { return m_BotRadius; }

    void					SetTopAlpha		( float val ) { m_TopAlpha = val; UpdateMesh(); }
    void					SetBotAlpha		( float val ) { m_BotAlpha = val; UpdateMesh(); }

    float					GetTopAlpha		() const { return m_TopAlpha; }
    float					GetBotAlpha		() const { return m_BotAlpha; }


	void					SetConeHeight	( float val ) { m_Height = val; UpdateMesh(); }
	float					GetConeHeight	() const { return m_Height; }

	NODE(PConeRenderer,PRenderer,2HRE);

protected:
	void					UpdateMesh();

}; // class PConeRenderer

/*****************************************************************************/
/*	Class:	PSphereRenderer	
/*	Desc:	Renders particle as sphere primitive
/*****************************************************************************/
class PSphereRenderer : public PRenderer 
{
	bool					m_bHemisphere;	//  whether only half is used
	int						m_HeightSegs;	//  number of height segments
	int						m_CircleSegs;	//  number of circle segments
	float					m_Radius;		//  bottom radius of the cone

    float                   m_PolePhi;      //  pole lattitude
    float                   m_PoleTheta;    //  pole longtitude

	BaseMesh				m_Mesh;
	BaseMesh				m_WorkMesh;

public:
							PSphereRenderer	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			RenderGeometry	( PEmitterInstance* pEmitter );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	void					SetNHSegs		( int val ) { m_HeightSegs = val; UpdateMesh(); }
	void					SetNCSegs		( int val ) { m_CircleSegs = val; UpdateMesh(); }
	
	void					SetHemisphere	( bool val = true ) { m_bHemisphere = val; UpdateMesh(); }
	bool					GetHemisphere	() const { return m_bHemisphere; }

	int						GetNHSegs		() const { return m_HeightSegs; }
	int						GetNCSegs		() const { return m_CircleSegs; }

	void					SetRadius		( float val ) { m_Radius = val; UpdateMesh(); }
	float					GetRadius		() const { return m_Radius; }

    void					SetPolePhi		( float val ) { m_PolePhi = val; UpdateMesh(); }
    float					GetPolePhi		() const { return m_PolePhi; }
    
    void					SetPoleTheta	( float val ) { m_PoleTheta = val; UpdateMesh(); }
    float					GetPoleTheta	() const { return m_PoleTheta; }

	NODE(PSphereRenderer,PRenderer,2TRE);

protected:
	void					UpdateMesh();
}; // class PSphereRenderer

enum PChainRendererFlags
{
    crfBindHead         = 0x1,
    crfAlignGround      = 0x2,
}; // enum PChainRendererFlags

/*****************************************************************************/
/*	Class:	PChainRenderer	
/*	Desc:	Renders particle as connected chain
/*****************************************************************************/
class PChainRenderer : public PRenderer
{
    BYTE                    m_Flags;
    float                   m_MaxLength;

public:
							PChainRenderer	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			RenderGeometry	( PEmitterInstance* pEmitter );
    virtual bool			FillGeometry	( PEmitterInstance* pEmitter );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );

    bool                    IsBindHead      () const { return (m_Flags&crfBindHead) != 0; }
    bool                    IsAlignGround   () const { return (m_Flags&crfAlignGround) != 0; }

    void                    SetBindHead     ( bool val = true ) { SetFlag( m_Flags, crfBindHead, val ); }
    void                    SetAlignGround  ( bool val = true ) { SetFlag( m_Flags, crfAlignGround, val ); }

	NODE(PChainRenderer,PRenderer,2CRE);
}; // class PChainRenderer

/*****************************************************************************/
/*	Class:	PTerrainDecal	
/*	Desc:	Renders particle as decal on the landscape
/*****************************************************************************/
class PTerrainDecal : public PRenderer
{
public:
							PTerrainDecal	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			RenderGeometry	( PEmitterInstance* pEmitter );

	NODE(PTerrainDecal,PRenderer,2TEE);
}; // class PTerrainDecal

/*****************************************************************************/
/*	Class:	PWaterDecal	
/*	Desc:	Renders particle as decal on the landscape
/*****************************************************************************/
class PWaterDecal : public PRenderer
{
public:
							PWaterDecal		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PWaterDecal,PRenderer,2WEE);
}; // class PWaterDecal

/*****************************************************************************/
/*	Class:	PForce	
/*	Desc:	Applies constant force to the particles
/*****************************************************************************/
class PForce : public POperator
{
	Vector3D				m_Force;
public:
							PForce();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PForce,POperator,2FOR);
}; // class PForce

enum AttractFadeMode
{
    afmConstant     = 0,
    afmLinear       = 1,
    afmQuadratic    = 2,
}; // enum AttractFadeMode

}; // namespace sg
ENUM( sg::AttractFadeMode, "AttractFadeMode", 
        en_val( sg::afmConstant,	"Constant"	) <<
        en_val( sg::afmLinear,	    "Linear"	) << 
        en_val( sg::afmQuadratic,	"Quadratic"	) );
namespace sg{

/*****************************************************************************/
/*	Class:	PAttract	
/*	Desc:	All particle inside radius are attracted towards the point
/*****************************************************************************/
class PAttract : public POperator
{
    Vector3D                m_Pos;
    float                   m_Magnitude;
    float                   m_Radius;
    AttractFadeMode         m_FadeMode;

public:
                            PAttract        ();
    virtual void			Expose			( PropertyMap& pm );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );
    virtual void			Render          ();
    virtual void			Process			( PEmitterInstance* pEmitter );

    NODE(PAttract,POperator,2ATT);
}; // class PAttract

/*****************************************************************************/
/*	Class:	PWind	
/*	Desc:	Imitates wind blowing on particles
/*****************************************************************************/
class PWind : public POperator
{
	Vector3D				m_Magnitude;	//  blowing direction
	float					m_Frequency;	
	float					m_Shift;

public:
							PWind();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PWind,POperator,2WIN);
}; // class PWind

/*****************************************************************************/
/*	Class:	PTorque
/*	Desc:	Applies rotational force to the particles
/*****************************************************************************/
class PTorque : public POperator
{
	Vector3D				m_Torque;

public:
							PTorque			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PTorque,POperator,2TOR);
}; // class PTorque

/*****************************************************************************/
/*	Class:	PFollow	
/*	Desc:	Makes particle follow next particle in chain
/*****************************************************************************/
class PFollow : public POperator
{
	float					m_Magnitude;
	bool					m_bFollowOlder;
public:
	PFollow();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PFollow,POperator,2FOL);
}; // class PFollow

/*****************************************************************************/
/*	Class:	PMouseBind	
/*	Desc:	Binds particle positions to mouse cursor position
/*****************************************************************************/
class PMouseBind : public PRenderer
{
	float					m_Depth;

public:
	PMouseBind	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Process			( PEmitterInstance* pEmitter );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );

	NODE(PMouseBind,PRenderer,2SRE);
}; // class PMouseBind

/*****************************************************************************/
/*	Class:	PAlphaFade	
/*	Desc:	Linearly changes particle transparency
/*****************************************************************************/
class PAlphaFade : public POperator
{
	float					m_StartAlpha;
	float					m_EndAlpha;
	bool					m_bAbsolute;

public:
	PAlphaFade();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PAlphaFade,POperator,2ALF);
}; // class PAlphaFade

/*****************************************************************************/
/*	Class:	PAVelRamp	
/*	Desc:	Keyframes particle angular speed
/*****************************************************************************/
class PAVelRamp : public POperator
{
	AlphaRamp				m_Ramp;
	float					m_MinVel;
	float					m_MaxVel;
	int						m_Repeat;
	InfluenceAxis			m_Axis; 

public:
							PAVelRamp		();

	AlphaRamp*				GetVelRamp	() const { return (AlphaRamp*)&m_Ramp; }
	void					SetVelRamp	( AlphaRamp* pRamp ) { assert( pRamp == &m_Ramp ); }

	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }


	NODE(PAVelRamp,POperator,2APP);
}; // class PAVelRamp

/*****************************************************************************/
/*	Class:	PVelRamp	
/*	Desc:	Keyframes particle angular speed
/*****************************************************************************/
class PVelRamp : public POperator
{
    AlphaRamp				m_Ramp;
    float					m_MinVel;
    float					m_MaxVel;
    int						m_Repeat;

public:
                            PVelRamp		();
    AlphaRamp*				GetVelRamp	    () const { return (AlphaRamp*)&m_Ramp; }
    void					SetVelRamp	    ( AlphaRamp* pRamp ) { assert( pRamp == &m_Ramp ); }
    virtual void			Expose			( PropertyMap& pm );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );
    virtual void			Process			( PEmitterInstance* pEmitter );

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }

    NODE(PVelRamp,POperator,2VPP);
}; // class PAVelRamp

/*****************************************************************************/
/*	Class:	PCoastBreak	
/*	Desc:	Affects particle velocity depending of the coastline proximity
/*****************************************************************************/
class PCoastBreak : public POperator
{
    float                   m_WaterLevel;
    float                   m_Magnitude;

public:
                            PCoastBreak		();
    virtual void			Expose			( PropertyMap& pm );
    virtual void			Serialize		( OutStream& os ) const;
    virtual void			Unserialize		( InStream& is  );
    virtual void			Process			( PEmitterInstance* pEmitter );
    virtual void			Trigger			( const PEmitterInstance* pEmitter, PParticle& p );

    NODE(PCoastBreak,POperator,2CPP);
}; // class PCoastBreak

/*****************************************************************************/
/*	Class:	PAlphaRamp	
/*	Desc:	Keyframed particle transparency
/*****************************************************************************/
class PAlphaRamp : public POperator
{
	AlphaRamp				m_Ramp;
	int						m_NRepeats;

public:
							PAlphaRamp		();

	AlphaRamp*				GetAlphaRamp	() const { return (AlphaRamp*)&m_Ramp; }
	void					SetAlphaRamp	( AlphaRamp* pRamp ) { assert( pRamp == &m_Ramp ); }

	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }

	NODE(PAlphaRamp,POperator,2ALR);
}; // class PAlphaRamp

/*****************************************************************************/
/*	Class:	PColorRamp	
/*	Desc:	Keyframes particle color
/*****************************************************************************/
class PColorRamp : public POperator
{
	ColorRamp				m_Ramp;
	int						m_NRepeats;

public:
							PColorRamp		();
	
	ColorRamp*				GetColorRamp	() const { return (ColorRamp*)&m_Ramp; }
	void					SetColorRamp	( ColorRamp* pRamp ) { assert( pRamp == &m_Ramp ); }

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }

	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PColorRamp,POperator,2ACR);
}; // class PColorRamp

/*****************************************************************************/
/*	Class:	PSizeFade	
/*	Desc:	Linearly changes particle size
/*****************************************************************************/
class PSizeFade : public POperator
{
	float					m_StartSize;
	float					m_EndSize;
	bool					m_bAbsolute;

public:
	PSizeFade();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PSizeFade,POperator,2SIF);
}; // class PSizeFade

/*****************************************************************************/
/*	Class:	PFluctuate	
/*	Desc:	Randomly displaces particle
/*****************************************************************************/
class PFluctuate : public POperator
{
	Vector3D				m_Direction;
	float					m_Variation;

public:
							PFluctuate		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PFluctuate,POperator,2FLU);
}; // class PFluctuate

/*****************************************************************************/
/*	Class:	PFrame	
/*	Desc:	Does frame animation of the particle's uv
/*****************************************************************************/
class PFrame : public POperator
{
	float					m_Rate;			//  framerate, FPS
	float					m_DRate;		//  framerate variation, FPS
	int						m_NRows;		//  number of frame rows
	int						m_NCols;		//  number of frame columns
	bool					m_bUV2;			//  apply to second texture channel

public:
							PFrame			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PFrame,POperator,2PFR);
}; // class PFrame

/*****************************************************************************/
/*	Class:	PUVMove	
/*	Desc:	Scrolls particle uv 
/*****************************************************************************/
class PUVMove : public POperator
{
	float					m_UScroll;	//  u scrolling speed, % per second
	float					m_VScroll;	//  v scrolling speed, % per second
	float					m_UTile;	//  u tiling factor
	float					m_VTile;	//  v tiling factor

	bool					m_bUV2;		//  apply to second texture channel


public:
							PUVMove			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PUVMove,POperator,2UVM);
}; // class PUVMove

/*****************************************************************************/
/*	Class:	PSizeRamp	
/*	Desc:	Keyframes particle size
/*****************************************************************************/
class PSizeRamp : public POperator
{
	AlphaRamp				m_Ramp;
	float					m_MinSize;
	float					m_MaxSize;
	int						m_Repeat;
	InfluenceAxis			m_Axis; 

public:
							PSizeRamp		();

	AlphaRamp*				GetSizeRamp	() const { return (AlphaRamp*)&m_Ramp; }
	void					SetSizeRamp	( AlphaRamp* pRamp ) { assert( pRamp == &m_Ramp ); }

	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

    int                     GetNKeys        () const { return m_Ramp.GetNKeys(); }


	NODE(PSizeRamp,POperator,2SIR);
}; // class PSizeRamp


/*****************************************************************************/
/*	Class:	PDrag	
/*	Desc:	Applies drag field to the particles
/*****************************************************************************/
class PDrag	: public POperator
{
private:
	//  cached values
	float					m_A;				
	float					m_B;

protected:
	float					m_Density;			//  fluid density
	float					m_Viscosity;		//  fluid viscosity
	bool					m_bOverrideRadius;	//	whether override radius
	float					m_ParticleRadius;	//  overriden particle radius

public:
							PDrag			();

	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	void					SetDensity		( float val );
	void					SetViscosity	( float val );
	float					GetDensity		() const { return m_Density; }
	float					GetViscosity	() const { return m_Viscosity; }
	void					SetToAir		();
	void					SetToWater		();

	NODE(PDrag,POperator,2DRA);
}; // class PDrag

/*****************************************************************************/
/*	Class:	PClampVelocity	
/*	Desc:	Clamps particle velocity
/*****************************************************************************/
class PClampVelocity	: public POperator
{
protected:
	float					m_MinVel;
	float					m_MaxVel;

public:
							PClampVelocity	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );


	NODE(PClampVelocity,POperator,2CLV);
}; // class PClampVelocity

/*****************************************************************************/
/*	Class:	PVortex	
/*	Desc:	Applies vortex-like acceleration to the particles
/*****************************************************************************/
class PVortex : public POperator
{
	float					m_Intensity;
	float					m_CenterAttraction;

public:
							PVortex			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PVortex,POperator,2VOR);
}; // class PVortex

/*****************************************************************************/
/*	Class:	POrbit	
/*	Desc:	Forces particles to stay on the cylinder surface
/*****************************************************************************/
class POrbit : public POperator
{
	float					m_Radius;

	float					m_Velocity;
	OrbitType				m_Type;
	Vector3D				m_Center;

public:
							POrbit			();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NOT_IMPLEMENTED

	NODE(POrbit,POperator,2RTR);
}; // class POrbit

/*****************************************************************************/
/*	Class:	PTurbulence	
/*	Desc:	Applies turbulence field to the particles
/*****************************************************************************/
class PTurbulence : public POperator
{
	float					m_Frequency;
	Vector3D				m_Phase;
    float                   m_Magnitude;

public:
							PTurbulence		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is  );
	virtual void			Process			( PEmitterInstance* pEmitter );

	NODE(PTurbulence,POperator,2TUR);
}; // class PTurbulence

/*****************************************************************************/
/*	Enum:	PInstanceFlag
/*****************************************************************************/
enum PInstanceFlag
{
	ifUnknown		= 0,
	ifNeedDraw		= 0x01,     //  instance should be rendered at this frame
	ifNeedUpdate	= 0x02,     //  instance should be drawn at this frame
    ifMoved         = 0x04,     //  instance has changed position on current frame
    ifAutoUpdate    = 0x08,     //  instance should be updated automatically
}; // enum PInstanceFlag

const DWORD	c_InstanceFrameTimeout	= 256;

/*****************************************************************************/
/*	Struct:	PEmitterInstance	
/*	Desc:	Single particle emitter instance, contains current emitter 
/*				parameters and instanced particles
/*****************************************************************************/
struct PEmitterInstance
{
	PEmitter*			m_pEmitter;			//  pointer to the emitter description
	PParticle*			m_pParticle;		//  pointer to the array of particle instances
	float				m_LastTime;			//	last update time  
	float				m_CurTime;			//	current update time  
    float               m_Timeout;          //  auto-update timeout
	float				m_EmitAccum;		//  emit accumulator, used to discretize emit rate

	float				m_StartTime;		//  starting time for the emitter
	float				m_TotalTime;		//	total emitting time
	float				m_Alpha;			//  emitter's alpha multiplier
    float				m_Intensity;		//  emitter's intensity multiplier

	Matrix4D			m_WorldTM;			//  absolute world transform, affects all child particles
    Matrix4D			m_PrevWorldTM;		//  world transform from previous step

    Matrix4D			m_EmitterTM;	    //  emitter transform, does not affect particles
    Vector3D            m_Target;           //  target position, for targetting effects

    DWORD               m_Cycles;           //  number of cycles processed for looped emitter

	int					m_ID;				//  this instance id
    int					m_StampedID;		//  id stamped with identity watermark
	int					m_ParentID;			//	parent's instance id
	int					m_ParticleID;		//  id of the particle this instance is attached to (if any)

	DWORD				m_LastFrame;		//  last frame when we were rendered
	DWORD				m_Flags;			//  flag bits container
    
    PEmitterInstance() : m_StampedID(0xFFFFFFFF){}

	void copy( const PEmitterInstance& ctx )
	{
		memcpy( this, &ctx, sizeof( PEmitterInstance ) );
	}

	float				GetTimeDelta() const { return m_CurTime - m_LastTime; }
	
	void				Reset		();	
	void				SetFlag     ( PInstanceFlag f, bool set = true ) { ::SetFlag( m_Flags, f, set ); }
	bool				GetFlag     ( PInstanceFlag f ) const { return ((f&m_Flags) != 0); }
    int                 GetNBorn    () const;
    int                 GetNParticles() const 
    {
        int res = 0;
        PParticle* pParticle = m_pParticle;
        while (pParticle)
        {
            res++;
            pParticle = pParticle->m_pNext;
        }
        return res;
    }

}; // struct PEmitterInstance

class PEffect;
const int c_ParticlePoolSize	= 16384;
const int c_EmitterPoolSize		= 4096;
const int c_MaxEffects			= 256;
typedef StaticHash<	PEmitterInstance, EntityContext, c_DefTableSize, c_EmitterPoolSize>	PInstanceHash;

/*****************************************************************************/
/*	Struct:	Particle effect atomic render task
/*	Desc:	Particle effect render task
/*****************************************************************************/
struct PRenderTask
{
    PRenderer*      m_pRenderer;
    int             m_InstID;
    
    static int CmpRenderTasks( const void *e1, const void *e2 )
    {
        const PRenderTask& task1 = *((const PRenderTask*)e1);
        const PRenderTask& task2 = *((const PRenderTask*)e2);
        return task1.m_pRenderer - task2.m_pRenderer;
    }

}; // struct PRenderTask

const DWORD c_UsedParticleSlot  = 1;
const DWORD c_FreeParticleSlot  = 0;
const int   c_RenderQSize       = 1024;
/*****************************************************************************/
/*	Class:	PEffectMgr	
/*	Desc:	Manages all particle effects in the scene
/*****************************************************************************/
class PEffectMgr : public Node, public IEffectManager
{
	float					m_ZBiasMultiplier;
	
	// global particle pool
	PParticle				m_Particle[c_ParticlePoolSize]; 
    
    // array of the particles "used slot" status
    // separated it because of frequent access 
	DWORD                   m_ParticleStatus[c_ParticlePoolSize];

    int						m_NParticles;
	int						m_LastUsed;

	float					m_MinDt;
	float					m_MaxDt;
	float					m_TimeDelta;
	float					m_InstanceFrameTimeout;

    bool                    m_bPaused;
    bool                    m_bAutoupdate;
    //  auto-update timeout
    float                   m_Timeout;
    float                   m_FadeTime;

    float                   m_PlayRate;
	
	//  currently active emitter instances
	PInstanceHash			m_Emitters;
    PRenderTask             m_RenderQ[c_RenderQSize];
    int                     m_RenderQSize;

    //  pack of the effect bindings
    struct EffectSet
    {
        std::string             m_File;
        std::vector<IEffect*>   m_Effect;
    }; // struct EffectSet

    //  list of loaded effect sets
    std::vector<EffectSet>      m_EffectSet;

public:
	//  auxiliary stuff
	Plane					m_ScreenPlane;
	Vector3D				m_PlaneUpVec;
	Vector3D				m_PlaneNVec;
	
public:
							PEffectMgr		();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Render			();

    virtual void			PreRender		();
    virtual void			Evaluate		();
    virtual void			PostRender		();

    virtual void			Pause           ( bool bPause = true );
	virtual void			Reset			();
	virtual DWORD			InstanceEffect	( DWORD modelID );

    //  emitter-instance related interface implementation
	virtual void			UpdateInstance	( DWORD hInst, const Matrix4D& tm = Matrix4D::identity );
	virtual void			DestroyInstance	( DWORD hInst );
    virtual bool            IsValid         ( DWORD hInst ) const;
    virtual void			RewindEffect	( DWORD hInst, float amount, float step = 0.02f, bool bRewChildren = true );
    virtual float			GetCurTime		( DWORD hInst ) const;
    virtual void            SetAlphaFactor  ( DWORD hInst, float factor );
    virtual void            SetIntensity    ( DWORD hInst, float intensity );
    virtual float			GetCurTime		() const;
    virtual void            SetAlphaFactor  ( float factor );
    virtual void            SetIntensity    ( float intensity );
    virtual void            EnableAutoUpdate( bool bEnable = true, 
                                                float timeout = 10.0f,
                                                float fadeTime = 5.0f ) { m_bAutoupdate = bEnable; m_Timeout = timeout; m_FadeTime = fadeTime;}
    
    float                   GetPlayRate     () const { return m_PlayRate; }
    void                    SetPlayRate     ( float rate ) { m_PlayRate = rate; }
    bool                    IsPaused        () const { return m_bPaused; }


	void					DumpToFile		( const char* fname );
	void					Dump			();

    virtual int             GetEffectSetID  ( const char* fileName );
    virtual int             GetNEffects     ( int setID );
    virtual bool            BindEffectSet   ( int setID, DWORD modelID );
    virtual int             FindEffectByName( int setID, const char* effName );

    virtual bool            UpdateInstance  ( int setID, int effID, const Matrix4D& tm = Matrix4D::identity );
    virtual void            SetAlphaFactor  ( int setID, int effID, float alpha );
    virtual void            SetIntensity    ( int setID, int effID, float intensity );

    virtual void            SetTarget       ( const Vector3D& pos );

    virtual void			UpdateInstance	( const Matrix4D& tm = Matrix4D::identity );
    virtual void			DestroyInstance	();

	float					GetZBiasMultiplier() const { return m_ZBiasMultiplier; }

	virtual int				GetNEmitterInst () const { return m_Emitters.numElem(); }
	virtual int				GetNParticles	() const { return m_NParticles; }
    DWORD					SpawnEmitter	( PEmitter* pEmitter, int particleID = -1, 
                                                int parentID = -1, bool bAttach = false );
    bool                    EmitParticle    ( PEmitterInstance& em );

	NODE(PEffectMgr,Node,2EFM);

protected:
    void                    ResetInstance       ( DWORD hInst, bool bResetChildren );
	int						GetParticlePoolSize () const { return sizeof( m_Particle ); }
	void					StepEmitter		    ( PEmitterInstance& em, float dt );
    PEmitterInstance&       GetEmitterInstance  ( DWORD hInst ) { return m_Emitters.elem( hInst&0x0000FFFF ); }
    const PEmitterInstance& GetEmitterInstance  ( DWORD hInst ) const { return m_Emitters.elem( hInst&0x0000FFFF ); }

    PEffect*                GetEffectFromSet    ( int setID, int idx );
    DWORD                   GetCurEffInstance   () const;

    Matrix4D                GetParticleWorldTM  ( const PParticle& p ) const;

}; // class PEffectMgr

enum EffectFlags
{
    efDefVisible = 0x1,
}; // enum EffectFlags

/*****************************************************************************/
/*	Class:	PEffect
/*	Desc:	Instanced particle system effect
/*****************************************************************************/
class PEffect : public TransformNode, public IEffect
{
	DWORD			m_EffectID;		//  id of the effect in the manager
	DWORD			m_InstanceID;	//  id of the current instance
	std::string		m_EffectFile;	//  path to the effect file

	float			m_WarmupTime;	//	time to rewind effect before starting
    
    //  not used right now
    int             m_Priority;     //  effect priority value, for arranging inside model
    std::string     m_BoneName;     //  name of the bone we are attached to
    float           m_StartTime;    //  time of activation, in global animation context time
    float           m_ActiveTime;   //  time of being active, in global animation context time
    
    float           m_Alpha;        //  total alpha multiplier for effect
    float           m_Intensity;    //  total emitter intensity multiplier for effect
    DWORD           m_Flags;        
    
public:
					PEffect			();
	virtual	void	Render			();
	virtual void	Expose			( PropertyMap& pm );
	virtual void	Serialize		( OutStream& os ) const;
	virtual void	Unserialize		( InStream& is  );

	void			SetEffectID		( DWORD id ) { m_EffectID = id; }
    virtual	DWORD   GetEffectID	    () const { return m_EffectID; }

	const char*		GetEffectFile	() const { return m_EffectFile.c_str(); }
	void			SetEffectFile	( const char* fname );
    const char*     GetParentBoneName() const;

    bool            IsDefaultVisible() const { return ((m_Flags & efDefVisible) != 0);  }
    void            SetDefaultVisible( bool val ) { SetFlag( m_Flags, efDefVisible, val );  }
    
    int             GetPriority     () const { return m_Priority; }
    void            SetPriority     ( int val ) { m_Priority = val; }

    float           GetAlpha        () const { return m_Alpha; }
    void            SetAlpha        ( float val ) { m_Alpha = val; }

    float           GetIntensity    () const { return m_Intensity; }
    void            SetIntensity    ( float val ) { m_Intensity = val; }

    XMLNode*        ToXML           () const;
    const char*		GetEffectName	() const { return GetName(); }
    bool            FromXML         ( XMLNode* node );

	NODE(PEffect, TransformNode, 2PIS);
}; // class PEffect

enum OverlayFlags
{
    ofAnimate       = 0x01,
    ofShiftBack     = 0x02,
}; // enum OverlayFlags

/*****************************************************************************/
/*	Class:	Overlay	
/*	Desc:	Overlay hightlighting effect
/*****************************************************************************/
class Overlay : public Node
{
	float			m_SizeX;
	float			m_SizeY;

	float			m_ShiftX;
	float			m_ShiftY;

	float			m_USpeed, m_VSpeed;
	float			m_U2Speed, m_V2Speed;
	
	DWORD			m_Color;

	Rct				m_UV;
	Rct				m_UV2;
	BYTE			m_Flags;

public:
					Overlay			();
	virtual void	Expose			( PropertyMap& pm );
	virtual void	Render			();
	virtual void	Serialize		( OutStream& os ) const;
	virtual void	Unserialize		( InStream& is  );

    bool            IsAnimated      () const { return (m_Flags&ofAnimate)!=0; }
    bool            IsShiftedBack   () const { return (m_Flags&ofShiftBack)!=0; }

    void            SetAnimated     ( bool val ) { SetFlag( m_Flags, ofAnimate, val ); } 
    void            SetShiftedBack  ( bool val ) { SetFlag( m_Flags, ofShiftBack, val ); } 

	NODE(Overlay,Node,OVER);

protected:
}; // class Overlay

}; // namespace sg

#ifdef _INLINES
#include "sgEffect.inl"
#endif // _INLINES

#endif __SGEFFECT_H__
