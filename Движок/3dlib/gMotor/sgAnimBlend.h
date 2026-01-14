/*****************************************************************/
/*  File:   sgAnimBlend.h
/*  Desc:   Animation blending routines
/*  Author: Silver, Copyright (C) GSC Game World
/*  Date:   Nov 2003
/*****************************************************************/
#ifndef __SGANIMBLEND_H__
#define __SGANIMBLEND_H__

namespace sg{

/*****************************************************************************/
/*	Class:	AnimationBlock
/*	Desc:	Group of animation nodes, synchronized by time
/*****************************************************************************/
class AnimationBlock : public Animation
{
public:	
							AnimationBlock	();

	virtual void			Render		();
	virtual void			Unserialize	( InStream& is	);
	virtual void			Serialize	( OutStream& os ) const;

	NODE(AnimationBlock,Animation,ANMB);
}; // class AnimationBlock

enum AnimPlaybackMode
{
    pmOnce      = 0,    //  played once
    pmLoop      = 1,    //  played looped
    pmPong      = 2,    //  played ping-pong style
    pmPose      = 3     //  static position
}; // enum AnimPlaybackMode

}; // namespace sg
ENUM(sg::AnimPlaybackMode,"AnimPlaybackMode", 
        en_val(sg::pmOnce,"Once") << 
        en_val(sg::pmLoop,"Loop") << 
        en_val(sg::pmPong,"Pong") << 
        en_val(sg::pmPose,"Pose") );
namespace sg{

/*****************************************************************************/
/*	Class:	AnimationBind
/*	Desc:	binding of the model animation
/*****************************************************************************/
class AnimationBind : public Node
{
    std::string         m_ModelName;
    std::string         m_AnimName;
    float               m_MaxWeight;

    float               m_StartTime;
    float               m_PlayTime;
    float               m_PlayRate;

    float               m_InFade;
    float               m_OutFade;

    AnimPlaybackMode    m_PlaybackMode;
    bool                m_bPlayBackwards;

    DWORD               m_ModelID;
    mutable DWORD       m_AnimID;

    std::vector<BYTE>   m_MaskBones;

public:	
                        AnimationBind   ();
    void                SetModelName    ( const char* name );
    void                SetAnimName     ( const char* name ); 

    const char*         GetModelName    () const { return m_ModelName.c_str(); }
    const char*         GetAnimName     () const { return m_AnimName.c_str(); }
    float               GetAnimTime     () const;


    float               GetMaxWeight    () const { return m_MaxWeight; }
    float               GetBlendWeight  ( float t ) const { return GetWeight( ToLocalTime( t ) ); }
    float               GetWeight       ( float t ) const;
 
    virtual void        Expose          ( PropertyMap& pm );
    
    float               ToLocalTime     ( float t ) const;


    virtual void		Unserialize	    ( InStream& is	);
    virtual void		Serialize	    ( OutStream& os ) const;

    bool                ApplyAnimation  ( DWORD modelID, float cTime );

    DWORD               GetAnimID       () const;
    DWORD               GetModelID      ();

    NODE(AnimationBind,Node,ANBI);
}; // class AnimationBind

enum AnimSetPlayMode
{
    apmPlayActive       = 1,
    apmBlendAll         = 2,
    apmBlendSequence    = 3,
};

}; // namespace sg
ENUM(sg::AnimSetPlayMode, "AnimSetPlayMode",    en_val(sg::apmPlayActive,    "PlayActive"   ) <<
                                                en_val(sg::apmBlendAll,      "BlendAll"     ) <<
                                                en_val(sg::apmBlendSequence, "BlendSequence") );
namespace sg{

/*****************************************************************************/
/*	Class:	AnimationSet
/*	Desc:	Set of the model animations
/*****************************************************************************/
class AnimationSet : public Node
{
    std::string         m_ModelName;
    int                 m_ActiveAnim;

    float               m_ModelScale;
    
    float               m_CurTime;
    float               m_Time;

    DWORD               m_ModelID;

    bool                m_bPaused;
    bool                m_bPlayed;
    bool                m_bLooped;

    AnimSetPlayMode     m_PlayMode;

public:	
                        AnimationSet        ();
    void                SetModelName        ( const char* name );
    const char*         GetModelName        () const { return m_ModelName.c_str(); }
    
    virtual void        Render              ();
    virtual void        Expose              ( PropertyMap& pm );

    void                Play                ();
    void                Pause               ();
    void                Stop                ();

    void                Optimize            ();

    virtual void		OnChangeChildren	();
    virtual bool		FromXML				( XMLNode* pRoot );
    virtual XMLNode*	ToXML				();

    int                 GetActiveAnim       () const { return m_ActiveAnim; }
    void                SetActiveAnim       ( int val );

    void                BlendAnimationTasks ();

    virtual void		Unserialize	        ( InStream& is	);
    virtual void		Serialize	        ( OutStream& os ) const;

    AnimationBind*      GetAnimTask         ( int idx ) { return (AnimationBind*)GetChild( idx ); }


    NODE(AnimationSet,Node,ANMS);
}; // class AnimationSet

/*****************************************************************************/
/*	Class:	Sequence
/*	Desc:	Mix of the number of the animations
/*****************************************************************************/
class Sequence : public Animation
{
public:	
    
    virtual void            Render(){}
    virtual void            Expose( PropertyMap& pm ){}
    

	NODE(Sequence,Animation,SEQU);
}; // class Sequence

/*****************************************************************/
/*	Class:	AnimBlend
/*	Desc:	Animation blender
/*****************************************************************/
class AnimBlend : public Animation
{
public:
			AnimBlend();
			~AnimBlend();
	NODE(AnimBlend,Animation,ANBL);
}; // class AnimBlend

}; // namespace sg

#endif //__SGANIMBLEND_H__

