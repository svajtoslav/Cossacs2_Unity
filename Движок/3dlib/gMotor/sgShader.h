/*****************************************************************************/
/*	File:	sgShader.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#ifndef __SGSHADER_H__
#define __SGSHADER_H__

namespace sg{

/*****************************************************************************/
/*	Class:	Material
/*	Desc:	Object material properties
/*****************************************************************************/
class Material : public Node
{
	DWORD						m_Ambient;
	DWORD						m_Diffuse;
	DWORD						m_Specular;
	float						m_Shininess;

	BYTE						m_Transparency;

	static Material*			s_pCurMtl;

public:
	_inl						Material();
	
	_inl virtual void			Render();

	_inl void					SetDiffuse		( DWORD _diffuse	);
	_inl void					SetSpecular		( DWORD _specular	);
	_inl void					SetAmbient		( DWORD _ambient	);
	_inl void					SetShininess	( float _shininess  );
	_inl void					SetTransparency	( BYTE _transparency);

	_inl DWORD					GetDiffuse		() const;
	_inl DWORD					GetSpecular		() const;
	_inl DWORD					GetAmbient		() const;
	_inl float					GetShininess	() const;
	_inl BYTE					GetTransparency	() const;

	static _inl Material*		GetCurMaterial	() { return s_pCurMtl; }

	virtual void				Serialize		( OutStream& os ) const;
	virtual void				Unserialize		( InStream& is  );
	virtual void				VisitAttributes	(){ s_pCurMtl = this; }
	virtual void				Expose			( PropertyMap& pm );	
	virtual bool				IsEqual			( const Node* node ) const;

	NODE(Material,Node,MATL);
}; // Material

/*****************************************************************************/
/*	Class:	BumpMatrix
/*	Desc:	Bump-mapping matrix node
/*****************************************************************************/
class BumpMatrix : public Transform2D
{
	int					m_Stage;

public:
						BumpMatrix		() : m_Stage(0) { tm.setIdentity(); }
	virtual void		Render			();
	virtual void		Serialize		( OutStream& os ) const;
	virtual void		Unserialize		( InStream& is	);
	virtual void		Expose			( PropertyMap& pm );


	NODE(BumpMatrix,Transform2D,BMTR);
}; // class BumpMatrix

/*****************************************************************************/
/*	Class:	TextureMatrix
/*	Desc:	UV transformation matrix node
/*****************************************************************************/
class TextureMatrix : public TransformNode
{
	int					m_Stage;

public:
						TextureMatrix();
	virtual void		Render			();
	virtual void		Serialize		( OutStream& os ) const;
	virtual void		Unserialize		( InStream& is	);
	virtual void		Expose			( PropertyMap& pm );

	void				SetStage		( int stage ) { m_Stage = stage; }
	void				SetTextureTM	( const Matrix4D& m );


	NODE(TextureMatrix,TransformNode,TMTR);
}; // class TextureMatrix

/*****************************************************************************/
/*	Class:	DeviceStateSet
/*	Desc:	Set of device render states and texture stage states
/*****************************************************************************/
class DeviceStateSet : public AssetNode
{
	int							m_StateBlockHandle;
	static bool					s_bFreeze;

public:
	_inl						DeviceStateSet	();
	_inl						DeviceStateSet	( const char* name );
	
	static void			        Freeze			() { s_bFreeze = true;  }
	static void			        Unfreeze		() { s_bFreeze = false; }
    static bool                 IsFrosen        () { return s_bFreeze; }
	
	const char*					GetScriptFile	() const { return GetName(); }
	void						SetScriptFile	( const char* file );
	void						Update			();
	_inl virtual void			Render			();
	void						Expose			( PropertyMap& pm );

	virtual void				Serialize		( OutStream& os ) const;
	virtual void				Unserialize		( InStream& is );


	NODE(DeviceStateSet,AssetNode,DSST);
}; // DeviceStateSet

class Texture;
class TextureMatrix;
class DeviceStateSet;

const int c_DetailTextureStage = 1;
/*****************************************************************************/
/*	Class:	DetailMap
/*	Desc:	Applies detail mapping to the underlying objects
/*****************************************************************************/
class DetailMap : public AssetNode
{
	float				m_UVScale;

	Texture*			m_pTexture;		//  detail texture on stage 1
	DeviceStateSet*		m_pDSS;			//  detail mapping shader
	TextureMatrix*		m_pTextureTM;	//  texture matrix on stage 1

public:
						DetailMap	();
	virtual void 		Expose		( PropertyMap& pm );
	virtual void 		Serialize	( OutStream& os ) const;
	virtual void 		Unserialize	( InStream& is );
	virtual void		Render		();

	virtual void		Init		();

	void				SetUVScale	( float scale ); 
	float				GetUVScale	() const { return m_UVScale; }
	
	NODE(DetailMap,AssetNode,DETM);
}; // class DetailMap

}; // namespace sg

#ifdef _INLINES
#include "sgShader.inl"
#endif // _INLINES

#endif // __SGSHADER_H__