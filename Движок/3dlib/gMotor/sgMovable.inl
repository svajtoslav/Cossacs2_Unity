/*****************************************************************************/
/*	File:	sgMovable.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	22.04.2003
/*****************************************************************************/

BEGIN_NAMESPACE( sg )
/*****************************************************************************/
/*	TransformNode implementation
/*****************************************************************************/
_inl TransformNode::TransformNode() 
	:	m_InitialTM	( Matrix4D::identity ),
		tm			( Matrix4D::identity )
{
}

_inl const Matrix4D& TransformNode::GetTransform() const
{
	return tm;
}

_inl void TransformNode::SetTransform( const Matrix4D& matr )
{
	tm = matr;
}


_inl void TransformNode::Transform( const Matrix4D& matr )
{
	tm *= matr;
}

_inl const Matrix4D& TransformNode::GetTopTM() const
{
	return m_WorldTM;
}

_inl void TransformNode::Reset() 
{
	tm.setIdentity();
}

_inl Vector3D TransformNode::GetPos() const
{
	return Vector3D( tm.e30, tm.e31, tm.e32 );
}

_inl Vector3D TransformNode::GetDirX() const
{
	return Vector3D( tm.e00, tm.e01, tm.e02 );
}

_inl Vector3D TransformNode::GetDirY() const
{
	return Vector3D( tm.e10, tm.e11, tm.e12 );
}

_inl Vector3D TransformNode::GetDirZ() const
{
	return Vector3D( tm.e20, tm.e21, tm.e22 );
}

_inl void TransformNode::SetPos ( Vector3D v )
{
	tm.e30 = v.x; tm.e31 = v.y; tm.e32 = v.z;
}

_inl void TransformNode::SetDirX( Vector3D v )
{
	tm.e00 = v.x; tm.e01 = v.y; tm.e02 = v.z;
	m_InitialTM.e00 = v.x; m_InitialTM.e01 = v.y; m_InitialTM.e02 = v.z;
}

_inl void TransformNode::SetDirY( Vector3D v )
{
	tm.e10 = v.x; tm.e11 = v.y; tm.e12 = v.z;
}

_inl void TransformNode::SetDirZ( Vector3D v )
{
	tm.e20 = v.x; tm.e21 = v.y; tm.e22 = v.z;
}

_inl float TransformNode::GetPosX() const
{
	return tm.e30;
}

_inl float TransformNode::GetPosY() const
{
	return tm.e31;
}

_inl float TransformNode::GetPosZ() const
{
	return tm.e32;
}

_inl void TransformNode::SetPosX( float v )
{
	tm.e30 = v;
}

_inl void TransformNode::SetPosY( float v )
{
	tm.e31 = v;
}

_inl void TransformNode::SetPosZ( float v )
{
	tm.e32 = v;
}

_inl float TransformNode::GetScaleX() const
{
	return GetDirX().norm();
}

_inl float TransformNode::GetScaleY() const
{
	return GetDirY().norm();
}

_inl float TransformNode::GetScaleZ() const
{
	return GetDirZ().norm();
}	

_inl void TransformNode::SetScaleX( float val )
{
	Vector3D dir = GetDirX();
    if (dir.normalize() < c_SmallEpsilon) dir = Vector3D::oX;
	dir *= val;
	SetDirX( dir );
}

_inl void TransformNode::SetScaleY( float val )
{
	Vector3D dir = GetDirY();
    if (dir.normalize() < c_SmallEpsilon) dir = Vector3D::oY;
	dir *= val;
	SetDirY( dir );
}

_inl void TransformNode::SetScaleZ( float val )
{
	Vector3D dir = GetDirZ();
    if (dir.normalize() < c_SmallEpsilon) dir = Vector3D::oZ;
	dir *= val;
	SetDirZ( dir );
}

_inl float TransformNode::GetEulerX() const
{
	Matrix3D rot = tm;
	return RadToDeg( rot.EulerXYZ().x );
} // TransformNode::GetEulerX

_inl float TransformNode::GetEulerY() const
{
	Matrix3D rot = tm;
	return RadToDeg( rot.EulerXYZ().y );
} // TransformNode::GetEulerY

_inl float TransformNode::GetEulerZ() const
{
	Matrix3D rot = tm;
	return RadToDeg( rot.EulerXYZ().z );
} // TransformNode::GetEulerZ

_inl void TransformNode::SetEulerX( float val )
{
	clamp( val, -180.0f, 180.0f );
	float sx = tm.getV0().norm();
	float sy = tm.getV1().norm();
	float sz = tm.getV2().norm();

	Matrix3D rot;
	rot.rotation( DegToRad( val ), DegToRad( GetEulerY() ), DegToRad( GetEulerZ() ) );
	tm.setRotation( rot );

	tm.getV0() *= sx;
	tm.getV1() *= sy;
	tm.getV2() *= sz;
} // TransformNode::SetEulerX

_inl void TransformNode::SetEulerY( float val )
{
	clamp( val, -180.0f, 180.0f );
	float sx = tm.getV0().norm();
	float sy = tm.getV1().norm();
	float sz = tm.getV2().norm();

	Matrix3D rot;
	rot.rotation( DegToRad( GetEulerX() ), DegToRad( val ), DegToRad( GetEulerZ() ) );
	tm.setRotation( rot );

	tm.getV0() *= sx;
	tm.getV1() *= sy;
	tm.getV2() *= sz;
} // TransformNode::SetEulerY

_inl void TransformNode::SetEulerZ( float val )
{
	clamp( val, -180.0f, 180.0f );
	float sx = tm.getV0().norm();
	float sy = tm.getV1().norm();
	float sz = tm.getV2().norm();

	Matrix3D rot;
	rot.rotation( DegToRad( GetEulerX() ), DegToRad( GetEulerY() ), DegToRad( val ) );
	tm.setRotation( rot );

	tm.getV0() *= sx;
	tm.getV1() *= sy;
	tm.getV2() *= sz;
} // TransformNode::SetEulerZ

/*****************************************************************************/
/*	HudNode implementation
/*****************************************************************************/
_inl float	HudNode::GetPosX() const
{
	return pos.x;
}

_inl float	HudNode::GetPosY() const
{
	return pos.y;
}

_inl float	HudNode::GetPosZ() const
{
	return pos.z;
}

_inl void	HudNode::SetPosX( float val )
{
	pos.x = val;
}

_inl void	HudNode::SetPosY( float val )
{
	pos.y = val;
}

_inl void	HudNode::SetPosZ( float val )
{
	pos.z = val;
}

_inl float HudNode::GetWidth() const
{
	return width;
}

_inl float HudNode::GetHeight() const
{
	return height;
}

_inl void HudNode::SetWidth( float val )
{
	width = val;
}

_inl void HudNode::SetHeight( float val )
{
	height = val;
}

_inl float HudNode::GetScale() const
{
	return scale;
}

_inl void HudNode::SetScale( float val )
{
	scale = val;
}

END_NAMESPACE( sg )