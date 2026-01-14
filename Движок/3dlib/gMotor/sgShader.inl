/*****************************************************************************/
/*	File:	sgShader.inl
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/

BEGIN_NAMESPACE( sg )

/*****************************************************************************/
/*	Material implementation
/*****************************************************************************/
_inl Material::Material() : 
	m_Diffuse		( 0xFFFFFFFF ),
	m_Ambient		( 0xFFFFFFFF ),
	m_Specular		( 0xFFFFFFFF ),
	m_Transparency	( 255		 ),
	m_Shininess		( 0.0f		 )
{
}

_inl void Material::Render()
{
	IRS->SetMaterial( this );
}

_inl void	Material::SetDiffuse( DWORD _diffuse )
{
	m_Diffuse = _diffuse;
}

_inl void	Material::SetSpecular( DWORD _specular )
{
	m_Specular = _specular;
}

_inl void	Material::SetAmbient( DWORD _ambient )
{
	m_Ambient = _ambient;
}

_inl void	Material::SetShininess( float _shininess )
{
	m_Shininess = _shininess;
}

_inl void	Material::SetTransparency( BYTE _transparency )
{
	_transparency = _transparency;
}

_inl DWORD	Material::GetDiffuse() const
{
	return m_Diffuse;
}

_inl DWORD	Material::GetSpecular() const
{
	return m_Specular;
}

_inl DWORD	Material::GetAmbient() const
{
	return m_Ambient;
}

_inl float	Material::GetShininess() const
{
	return m_Shininess;
}

_inl BYTE	Material::GetTransparency() const
{
	return m_Transparency;
}

/*****************************************************************************/
/*	DeviceStateSet implementation
/*****************************************************************************/
_inl DeviceStateSet::DeviceStateSet() : m_StateBlockHandle( -1 )
{
}

_inl DeviceStateSet::DeviceStateSet( const char* name ) : m_StateBlockHandle( -1 ), AssetNode( name ) 
{
}

_inl void DeviceStateSet::Render()
{
	if (s_bFreeze) return;

	if (m_StateBlockHandle == -1) 
	{
		if (GetFlagState( nfEmbeddedData ))
		{
			m_StateBlockHandle = IRS->GetShaderID( GetName(), GetData(), GetDataSize() );
		}
		else
		{
			m_StateBlockHandle = IRS->GetShaderID( GetName() );
		}
	}
	if (m_StateBlockHandle == -1) m_StateBlockHandle = 0;
	IRS->SetCurrentShader( m_StateBlockHandle );

	if (GetNChildren() > 0)
	{
		Freeze();
		Node::Render();
		Unfreeze();
	}

} // DeviceStateSet::Render

END_NAMESPACE( sg )
