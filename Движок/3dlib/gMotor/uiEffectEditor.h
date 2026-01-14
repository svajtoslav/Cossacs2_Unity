/*****************************************************************************/
/*	File:	uiEffectEditor.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	11-23-2003
/*****************************************************************************/
#ifndef __UIEFFECTEDITOR_H__
#define __UIEFFECTEDITOR_H__

#include "ISceneEditor.h"

namespace sg{
class NodeTree;
class PEmitter;
class PEffect;

/*****************************************************************************/
/*	Class:	EffectStub
/*	Desc:	Instanced particle system effect
/*****************************************************************************/
class EffectStub : public Animation
{
protected:
	DWORD			m_EffectID;

	friend class EffectEditor;

public:
	EffectStub() : m_EffectID( 0xFFFFFFFF ) {}
	virtual	void	OnChangeChildren();
    virtual void    Render          ();

	NODE(EffectStub, Animation, 2EFS);
}; // class EffectStub

enum EffectFireMode
{
    fmParabolic         = 1,
    fmGroundMove        = 2
}; // enum EffectFireMode

}; // namespace sg
ENUM( sg::EffectFireMode, "EffectFireMode", 
            en_val( sg::fmParabolic, "Parabolic" ) << 
            en_val( sg::fmGroundMove, "GroundMove" ) );
namespace sg{

/*****************************************************************************/
/*	Class:	EffectEditor	
/*	Desc:	Editor of the particle effects
/*****************************************************************************/
class EffectEditor : public Dialog
{
public:
							EffectEditor	();
	virtual void			Expose			( PropertyMap& pm );
	virtual void			Render			();

	Node*					GetEffectRoot	();

	virtual bool			OnChar			( DWORD charCode, DWORD flags );
    virtual bool			OnKeyDown       ( DWORD keyCode, DWORD flags );
	Group*					GetPalette		() { return m_pPalette; }

	void 					SetTerraVisible	( bool val );
	bool 					IsTerraVisible	() const;
	void 					Reset			();
	void 					Load			();
    void 					Fire			();
    void                    Pause           ();
	void 					Save			();
	void 					SaveAs			();
	void 					StopEffect		();
	void 					PlayEffect		();
	void 					SetEffectFile	( const char* fname );
	float 					GetTotalTime	() const;
	float 					GetCurTime		() const;

    float                   GetPlayRate     () const;
    void                    SetPlayRate     ( float rate );

	ActiveEditorCamera		GetCameraMode	() const { return IScEd->GetActiveCamera(); }
	void					SetCameraMode	( ActiveEditorCamera val ){ IScEd->SetActiveCamera( val ); }
	bool					ShowGrid		() const { return IScEd->IsShowGrid(); }
	void					ShowGrid		( bool val = true ){ IScEd->ShowGrid( val ); }
	bool					ShowBackdrop	() const { return m_pBackScene ? !m_pBackScene->IsInvisible() : false; }
	void					ShowBackdrop	( bool val = true ) { if (m_pBackScene) m_pBackScene->SetInvisible( !val ); }

	const char*				GetEffectFile	() const { return m_EffectFile.c_str(); }

    void                    SetEffectFireMode( EffectFireMode fm );
    EffectFireMode          GetEffectFireMode() const { return m_FireMode; }

	NODE(EffectEditor,Dialog,2EED);

protected:
	Group*					m_pPalette;	
	EffectStub*				m_pEffect;
	std::string				m_EffectFile;
	Node*					m_pBackScene;	//  background scene used for convenience
	
	int						m_EffectID;		//  id of effect the currently being edited
	int						m_InstanceID;	//	id of the currently playing instance

    float                   m_AlphaFactor;
    float                   m_IntensityFactor;
    bool                    m_bTerraVisible;

    float                   m_PlayRate;
    float                   m_ShotTime;     
    float                   m_ShotVelocity;
    float                   m_ShotAngle;
    float                   m_ShotHeight;
    float                   m_ShotGravity;

    EffectFireMode          m_FireMode;

    float                   m_CurShotTime;
    DWORD                   m_StartShotTime;
    float                   m_bShot;

	Group*					CreateTemplateGroup();
}; // class EffectEditor

}; // namespace sg

#endif // __UIEFFECTEDITOR_H__