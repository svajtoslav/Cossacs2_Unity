#ifndef __SGFOG_H__
#define __SGFOG_H__

#include "mHeightmap.h"
#include "IMediaManager.h"

namespace sg{

/*****************************************************************************/
/*	Class:	Fog
/*	Desc:	Applies fog effect to all children
/*****************************************************************************/
class Fog: public Node
{
public:
	enum FogType
	{
		ftUnknown	= 0,
		ftVertex	= 1,
		ftPixel		= 2
	};

	enum FogMode
	{
		fmUnknown	= 0,
		fmLinear	= 1,
		fmExp		= 2,
		fmExp2		= 3
	};

					Fog			();
					~Fog		();

	virtual void	Render		();
	virtual void	Serialize	( OutStream& os		) const;
	virtual void	Unserialize	( InStream& is		);
	virtual void	Expose		( PropertyMap& pm );


	_inl void		SetColor	( DWORD clr )		{ m_Color = clr;	}
	_inl DWORD		GetColor	() const			{ return m_Color;	}

	_inl void		SetStart	( float val )		{ m_Start = val;	}
	_inl float		GetStart	() const			{ return m_Start;	}
	
	_inl void		SetEnd		( float val )		{ m_End = val;		}
	_inl float		GetEnd		() const			{ return m_End;		}

	_inl void		SetDensity	( float val )		{ m_Density = val;	}
	_inl float		GetDensity	() const			{ return m_Density; }

	_inl void		SetType		( FogType val )		{ m_Type = val;		}
	_inl FogType	GetType		() const			{ return m_Type;	}

	_inl void		SetMode		( FogMode val )		{ m_Mode = val;		}
	_inl FogMode	GetMode		() const			{ return m_Mode;	}
	
	_inl bool		GetIsRangeBased() const			{ return m_bRangeBased; }
	_inl void		SetIsRangeBased( bool val )		{ m_bRangeBased = val; }

	NODE(Fog,Node,FOGN);

private:
	DWORD			m_Color;
	float			m_Start;
	float			m_End;

	float			m_Density;
	FogType			m_Type;
	FogMode			m_Mode;

	bool			m_bRangeBased;

	bool			m_bEnabled;

}; // Fog


}; // namespace sg
ENUM( sg::Fog::FogType, "Type", 
							en_val( sg::Fog::ftUnknown,	"Unknown"	) <<
							en_val( sg::Fog::ftVertex,	"Vertex"	) <<
							en_val( sg::Fog::ftPixel,	"Pixel"		) );

ENUM( sg::Fog::FogMode, "Mode", 
							en_val( sg::Fog::fmUnknown,	"Unknown"	) <<
							en_val( sg::Fog::fmLinear,	"Linear"	) <<
							en_val( sg::Fog::fmExp,		"Exp"		) <<
							en_val( sg::Fog::fmExp,		"Exp2"		) );
namespace sg{
/*****************************************************************************/
/*	Class:	WaterPatch
/*	Desc:	Piece of water 
/*****************************************************************************/
class WaterPatch : public Geometry
{
	HeightMap		m_WaterHeightPrev;
	HeightMap		m_WaterHeight;
	HeightMap		m_Damping;

	Lattice<DWORD>	m_WaterColor;

	
	float			m_Side;
	int				m_SideSegments;
	float			m_PosX, m_PosY;
	float			m_WaveSpeed;
	float			m_WaterLevel;
	float			m_SplashAmount;
	int				m_SplashX, m_SplashY;
	DWORD			m_DefaultWaterColor;

public:
					WaterPatch		();
	void			Generate		();
	void			Splash			( const Vector3D& location, float radius = 1.0f );
	void			Splash			();

	virtual void	Serialize		( OutStream& os ) const;
	virtual void	Unserialize		( InStream& is );
	virtual void	Render			();
	virtual void	Expose			( PropertyMap& pm );

	_inl float		GetWaveSpeed	() const { return m_WaveSpeed; }
	_inl float		GetWaterLevel	() const { return m_WaterLevel; }
	_inl int		GetNSideSegments() const { return m_SideSegments; }
	void			SetNSideSegments( int val );

	_inl void		SetWaveSpeed	( float val ) { m_WaveSpeed = val; }
	void			SetWaterLevel	( float val );	

	NODE( WaterPatch, Geometry, WTRP );

protected:
	void			UpdateGrid		();
	void			UpdateMesh		();

}; // class WaterPatch

/*****************************************************************************/
/*	Class:	WaterScape
/*	Desc:	Water manager
/*****************************************************************************/
class WaterScape : public Node
{
public:
					WaterScape		();
	virtual void	Serialize		( OutStream& os ) const;
	virtual void	Unserialize		( InStream& is );
	virtual void	Render			();
	virtual void	Expose			( PropertyMap& pm );

	NODE( WaterScape, Node, WSCA );
}; // class WaterScape

}; // namespace sg


#endif // __SGFOG_H__