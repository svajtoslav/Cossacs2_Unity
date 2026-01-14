/*****************************************************************************/
/*	File:	sgLight.inl
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	Light implementation
/*****************************************************************************/
_inl Light::Light()
{
	m_Ambient = 0xFF000000;	
	m_Diffuse = 0xFFFFFFFF;
	m_Specular= 0xFFFFFFFF;

	m_Index = -1;
} // Light::Light

_inl Light::Light( const Vector3D& dir )
{
	SetDir( dir ); 
	
	m_Ambient = 0xFF000000;	
	m_Diffuse = 0xFFFFFFFF;
	m_Specular= 0xFFFFFFFF;

	m_Index = -1;
} // Light::Light

_inl DWORD	Light::GetAmbient()	const
{
	return m_Ambient;
}

_inl DWORD Light::GetDiffuse() const
{
	return m_Diffuse;
}

_inl DWORD Light::GetSpecular()	const
{
	return m_Specular;
}

_inl Vector3D Light::GetPos() const
{
	return  Vector3D( tm.e30, tm.e31, tm.e32 );
}

_inl Vector3D Light::GetDir() const
{
	return  Vector3D( tm.e20, tm.e21, tm.e22 );
} // Light::GetDir

_inl void Light::SetPos( const Vector3D& pos )
{
	tm.e30 = pos.x;
	tm.e31 = pos.y;
	tm.e32 = pos.z;
}

_inl void Light::SetDir( const Vector3D& dir )
{
	Vector3D dx = GetDirX();
	Vector3D dy = GetDirY();
	Vector3D dz( dir );
	
	Vector3D::orthonormalize( dz, dx, dy );
	
	SetDirX( dx );
	SetDirY( dy );
	SetDirZ( dz );
} // Light::SetDir

_inl void Light::SetDiffuse( DWORD diffuse )
{
	m_Diffuse = diffuse;
}

_inl void Light::SetAmbient( DWORD ambient )
{
	m_Ambient = ambient;
}

_inl void Light::SetSpecular( DWORD specular )
{
	m_Specular = specular;
}

/*****************************************************************************/
/*	PointLight implementation
/*****************************************************************************/
_inl Sphere	PointLight::GetLightSphere() const
{
	return Sphere( GetPos(), GetRange() ); 
} // PointLight::GetLightSphere

/*****************************************************************************/
/*	DirectionalLight implementation
/*****************************************************************************/
_inl Ray3D DirectionalLight::GetLightRay() const
{
	return Ray3D( GetPos(), GetDir() );
} // DirectionalLight::GetLightRay

END_NAMESPACE( sg )
