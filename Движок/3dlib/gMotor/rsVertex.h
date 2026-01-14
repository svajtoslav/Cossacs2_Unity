/*****************************************************************
/*  File:   rsVertex.h                                             
/*	Desc:	Set of vertex formats
/*  Date:   November 2001                                        
/*  Modify: Feb 2002, Silver
/*****************************************************************/
#ifndef __S_CUSTOMVERTEX_H__
#define __S_CUSTOMVERTEX_H__
#pragma	once
#include "gmDefines.h"

class AABoundBox;
/*****************************************************************
/*	Class:	Vertex
/*  Desc:	Generic vertex class - serves as different vertex
/*				types factory.                                              
/*****************************************************************/
class Vertex
{
public:
	static _inl int		GetStride		    ( VertexFormat vf );
	static _inl int		GetDiffuseStride    ( VertexFormat vf );
    static _inl int		GetSpecularStride   ( VertexFormat vf );
	static _inl int		GetUVStride		    ( VertexFormat vf );
	static _inl int		GetUV2Stride	    ( VertexFormat vf );
	static _inl int		GetNormalStride	    ( VertexFormat vf );
    static _inl int		GetBlendWStride	    ( VertexFormat vf );
    static _inl int		GetBlendIStride	    ( VertexFormat vf );

	static void*		CreateVBuf          ( VertexFormat vf, int numVert );

	//  vertex component setters
	void				SetPos				( const Vector3D& p ){}
	void				SetW				( float rhw ){}
	void				SetNormal			( const Vector3D& n ){}
	void				SetUV				( float tu, float tv  ){}
	void				SetUV2				( float tu, float tv  ){}
	void				SetDiffuse			( DWORD clr ){} 
	void				SetSpecular			( DWORD clr ){}

	void				SetBlendI  			( int link, DWORD idx ) {}
	void				SetBlendW  			( int link, float blendW ) {}

	//  vertex component getters
	const Vector3D&		GetPos				() const { return Vector3D::null; }
	const Vector3D&		GetNormal			() const { return Vector3D::oZ; }
	float				GetU				() const { return 0.0f; }
	float				GetV				() const { return 0.0f; }
	float				GetU2				() const { return 0.0f; }
	float				GetV2				() const { return 0.0f; }
	DWORD				GetDiffuse			() const { return 0; } 
	DWORD				GetSpecular			() const { return 0; }
	int					GetBlendI  			( int link ) const { return 0; }
	float				GetBlendW  			( int link ) const { return 1.0f; }
	int     			GetNBlendI 			() const { return 0; }
	int     			GetNBlendW 			() const { return 0; }
}; // class Vertex

class Vector3D;
/*****************************************************************************/
/*	Class:	Vertex32
/*	Desc:	This class is a hack for working with 32-bytes vertices
/*				in vertex coordinates mode only
/*****************************************************************************/
class Vertex32
{
public:
	float	x, y, z;

private:
	DWORD	dummy[5];
}; // class Vertex32

/*****************************************************************
/*  Class:  Vertex2t											 
/*****************************************************************/
class Vertex2t : public Vertex
{
public:
							Vertex2t();

	float					x;
	float					y;
	float					z;

	DWORD					diffuse;	//  diffuse vertex color
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates

	const Vertex2t&	operator =(const Vector3D& vec);
	operator Vector3D() const{return Vector3D( x, y, z );}

	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 

	static VertexFormat		format()	{ return vf2Tex;	}
};  // class Vertex2t

/*****************************************************************
/*  Class:  VertexTS											 
/*****************************************************************/
class VertexTS : public Vertex
{
public:
	Vertex2t();

	float					x;
	float					y;
	float					z;

	DWORD					diffuse;	//  diffuse vertex color
	DWORD					specular;
	float					u, v;		//  1st texture coordinates

	const VertexTS&	operator =(const Vector3D& vec);
	operator Vector3D() const { return Vector3D( x, y, z );}

	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetSpecular			( DWORD clr )			{ specular = clr; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	DWORD				GetDiffuse			() const { return diffuse; } 
	DWORD				GetSpecular			() const { return specular; }

	static VertexFormat		format()	{ return vfTS;	}
};  // class VertexTS

/*****************************************************************
/*  Class:  VertexMP1											 
/*  Desc:	Vertex used in matrix palette blending, vertex is
/*				bound to single bone
/*****************************************************************/
class VertexMP1 : public Vertex
{
public:
							VertexMP1();

	float					x;
	float					y;
	float					z;

	DWORD					matrIdx;	//  index of the corresponding transform matrix in palette
	DWORD					diffuse;	//  diffuse vertex color
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates
	
	const Vertex2t& operator =( const Vector3D& vec );

	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetBlendI  			( int link, DWORD idx ) { if (link == 0) matrIdx = idx; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 
	int					GetBlendI  			( int link ) const { return matrIdx; }

	static VertexFormat		format()	{ return vfMP1;		}
};  // class VertexMP1

/*****************************************************************
/*  Class:  VertexNMP1											 
/*  Desc:	Vertex used in matrix palette blending, vertex is
/*				bound to single bone, also normal is included
/*****************************************************************/
class VertexNMP1 : public Vertex
{
public:
	VertexNMP1(){}

	float					x;
	float					y;
	float					z;

	DWORD					matrIdx;	//  index of the corresponding transform matrix
										//  in palette
	float					nx;
	float					ny;
	float					nz;

	DWORD					diffuse;	//  diffuse vertex color
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates
	

	const Vertex2t& operator =(const Vector3D& vec);

//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetNormal			( const Vector3D& n ){ nx = n.x; ny = n.y; nz = n.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetBlendI  			( int link, DWORD idx ) { matrIdx = idx; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	const Vector3D&		GetNormal			() const { return *((Vector3D*)&nx); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 

	int					GetBlendI  			( int link ) const { return matrIdx; }
    float				GetBlendW  			( int link ) const { return 1.0f; }
    int     			GetNBlendI 			() const { return 1; }
    int     			GetNBlendW 			() const { return 0; }

	static VertexFormat		format()	{ return vfNMP1;	}
};  // class VertexNMP1

/*****************************************************************
/*  Class:  VertexNMP2											 
/*  Desc:	Vertex used in matrix palette blending, vertex is
/*				bound to 2 bones, also normal is included
/*****************************************************************/
class VertexNMP2 : public Vertex
{
public:
	VertexNMP2(){}

	float					x;
	float					y;
	float					z;

	float					weight[1];
	DWORD					matrIdx;	//  index of the corresponding transform matrix
										//  in palette
	float					nx;
	float					ny;
	float					nz;

	DWORD					diffuse;	//  diffuse vertex color
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates

	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetNormal			( const Vector3D& n ){ nx = n.x; ny = n.y; nz = n.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetBlendI  			( int link, DWORD idx ) { matrIdx &= ~(0x000000FF << (link << 3)); matrIdx |= (idx << (link << 3)); }
	void				SetBlendW  			( int link, float blendW ) { if (link == 0) weight[0] = blendW; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	const Vector3D&		GetNormal			() const { return *((Vector3D*)&nx); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 

    int					GetBlendI  			( int link ) const { return 0x000000FF & ( matrIdx >> (link << 3) ); }
    float				GetBlendW  			( int link ) const { return link == 0 ? weight[0] : 1.0f - weight[0]; }
    int     			GetNBlendI 			() const { return 2; }
    int     			GetNBlendW 			() const { return 1; }

	static VertexFormat		format()	{ return vfNMP2;	}
};  // class VertexNMP2

/*****************************************************************
/*  Class:  VertexNMP3											 
/*  Desc:	Vertex used in matrix palette blending, vertex is
/*				bound to 3 bones, also normal is included
/*****************************************************************/
class VertexNMP3 : public Vertex
{
public:
	VertexNMP3(){}

	float					x;
	float					y;
	float					z;

	float					weight[2];
	DWORD					matrIdx;	//  index of the corresponding transform matrix
										//  in palette
	float					nx;
	float					ny;
	float					nz;

	DWORD					diffuse;	//  diffuse vertex color
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates
	

	const Vertex2t& operator =(const Vector3D& vec);
	
	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetNormal			( const Vector3D& n ){ nx = n.x; ny = n.y; nz = n.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetBlendI  			( int link, DWORD idx ) { matrIdx &= ~(0x000000FF << (link << 3)); matrIdx |= (idx << (link << 3)); }
	void				SetBlendW  			( int link, float blendW ) { if (link <= 1) weight[link] = blendW; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	const Vector3D&		GetNormal			() const { return *((Vector3D*)&nx); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 

    int					GetBlendI  ( int link ) const { return 0x000000FF & (matrIdx >> (link << 3)); }
    float				GetBlendW  ( int link ) const { return link < 2 ? weight[link] : 1.0f - weight[0] - weight[1]; }
    int     			GetNBlendI () const { return 3; }
    int     			GetNBlendW () const { return 2; }

	static VertexFormat		format()	{ return vfNMP3;	}
};  // class VertexNMP3

/*****************************************************************
/*  Class:  VertexNMP4										 
/*  Desc:	Vertex used in matrix palette blending, vertex is
/*				bound to 4 bones, also normal is included
/*****************************************************************/
class VertexNMP4 : public Vertex
{
public:
	VertexNMP4(){}

	float					x;
	float					y;
	float					z;

	float					weight[3];
	DWORD					matrIdx;	//  index of the corresponding transform matrix
										//  in palette
	float					nx;
	float					ny;
	float					nz;

	DWORD					diffuse;	//  diffuse vertex color
	DWORD					specular;	//  specular vertex color
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates
	

	const VertexNMP4& operator =(const Vector3D& vec);
	
	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetNormal			( const Vector3D& n ){ nx = n.x; ny = n.y; nz = n.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetBlendI  			( int link, DWORD idx ) {}
	void				SetBlendW  			( int link, float blendW ) { if (link <= 2) weight[link] = blendW; }
	void				SetSpecular			( DWORD clr )			{ specular = clr; }


	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	const Vector3D&		GetNormal			() const { return *((Vector3D*)&nx); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 
	DWORD				GetSpecular			() const { return specular; }
	
    int					GetBlendI  			( int link ) const { return 0x000000FF & (matrIdx >> (link << 3)); }
    float				GetBlendW  			( int link ) const { return link < 3 ? weight[link] : 1.0f - weight[0] - weight[1] - weight[2]; }
    int     			GetNBlendI 			() const { return 4; }
    int     			GetNBlendW 			() const { return 3; }

	static VertexFormat		format()	{ return vfNMP4;	}
};  // class VertexNMP4

/*****************************************************************
/*  Class:  VertexN											 
/*  Desc:	Vertex with normal and pair of tex coords
/*****************************************************************/
class VertexN : public Vertex
{
public:
							VertexN();

	float					x;
	float					y;
	float					z;

	float					nx;
	float					ny;
	float					nz;

	float					u, v;		//  1st texture coordinates
	VertexN&				operator =( const Vector3D& vec ); 

	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetNormal			( const Vector3D& n ){ nx = n.x; ny = n.y; nz = n.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	const Vector3D&		GetNormal			() const { return *((Vector3D*)&nx); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }

	static VertexFormat		format()	{ return vfN;	}
};  // class VertexN

/*****************************************************************
/*  Class:  VertexN2T											 
/*****************************************************************/
class VertexN2T : public Vertex
{
public:
	VertexN2T(){}

	float					x, y, z;	//  position
	float					nx, ny, nz;	//  normal
	
	DWORD					diffuse;
	DWORD					specular;
	
	float					u, v;		//  1st texture coordinates
	float					u2, v2;		//  2nd texture coordinates
	
	VertexN2T&				operator =( const Vector3D& vec ); 

	//  vertex component setters
	void					SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void					SetNormal			( const Vector3D& n ){ nx = n.x; ny = n.y; nz = n.z; }
	void					SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void					SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void					SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void					SetSpecular			( DWORD clr )			{ specular = clr; } 

	//  vertex component getters
	const Vector3D&			GetPos				() const { return *((Vector3D*)&x); }
	const Vector3D&			GetNormal			() const { return *((Vector3D*)&nx); }
	float					GetU				() const { return u; }
	float					GetV				() const { return v; }
	float					GetU2				() const { return u2; }
	float					GetV2				() const { return v2; }
	DWORD					GetDiffuse			() const { return diffuse; } 
	DWORD					GetSpecular			() const { return specular; }

	static VertexFormat		format()	{ return vfN2T;	}
};  // class VertexN2T

/*****************************************************************
/*  Class:  VertexT											 
/*  Desc:	XYZUV
/*****************************************************************/
class VertexT : public Vertex
{
public:
						VertexT();

	float				x;
	float				y;
	float				z;

	float				u;
	float				v;
	
	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	
	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	
	static VertexFormat	format()			{ return vfT;	}
};  // class VertexT

/*****************************************************************
/*  Class:  VertexTnL		                                     *
/*  Desc:   Already transformed and lit vertex   				 *
/*****************************************************************/
class VertexTnL : public Vertex
{
public:
						VertexTnL();

    float				x, y;		//  x and y coords in screen space
	float				z;			//  depth coordinate
	float				w;			//  reciprocal homogeneous w

    DWORD				diffuse;	//  diffuse color
	DWORD				specular;	
	float				u, v;		//  texture coordinates

	VertexTnL&			operator =( const Vector3D& vec ); 

	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetW				( float rhw ){ w = rhw; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetSpecular			( DWORD clr )			{ specular = clr; }


	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	DWORD				GetDiffuse			() const { return diffuse; } 
	DWORD				GetSpecular			() const { return specular; }

	static VertexFormat	format()			{ return vfTnL;	}
}; // class VertexTnL

/*****************************************************************
/*  Class:  VertexTnL2		                                     *
/*  Desc:   Already transformed and lit vertex   				 *
/*****************************************************************/
class VertexTnL2 : public Vertex
{
public:
    float				x, y;		//  x and y coords in screen space
	float				z;			//  depth coordinate
	float				w;			//  reciprocal homogeneous w

    DWORD				diffuse;	//  diffuse color
	float				u, v;		//  texture coordinates
	float				u2, v2;		//  texture coordinates

	
	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetW				( float rhw ){ w = rhw; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 

	static VertexFormat	format()	{ return vfTnL2;	}
}; // class VertexTnL
/*****************************************************************
/*  Class:  VertexTnL2S		                                     *
/*  Desc:   Already transformed and lit vertex   				 *
/*****************************************************************/
class VertexTnL2S : public Vertex
{
public:
    float				x, y;		//  x and y coords in screen space
	float				z;			//  depth coordinate
	float				w;			//  reciprocal homogeneous w

    DWORD				diffuse;	//  diffuse color
	DWORD				specular;	//  diffuse color
	float				u, v;		//  texture coordinates
	float				u2, v2;		//  texture coordinates
	
	//  vertex component setters
	void				SetPos				( const Vector3D& p  ){ x = p.x; y = p.y; z = p.z; }
	void				SetW				( float rhw )			{ w = rhw; }
	void				SetUV				( float tu, float tv  )	{ u = tu; v = tv; }
	void				SetUV2				( float tu, float tv  )	{ u2 = tu; v2 = tv; }
	void				SetDiffuse			( DWORD clr )			{ diffuse = clr; } 
	void				SetSpecular			( DWORD clr )			{ specular = clr; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return *((Vector3D*)&x); }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }
	float				GetU2				() const { return u2; }
	float				GetV2				() const { return v2; }
	DWORD				GetDiffuse			() const { return diffuse; } 
	DWORD				GetSpecular			() const { return specular; }

	static VertexFormat	format()	{ return vfTnL2S;	}
}; // class VertexTnL

/*****************************************************************
/*  Class:  Vertex1W                                            
/*  Desc:   Vertex bound to single bone									     
/*****************************************************************/
class Vertex1W : public Vertex
{
public:
	Vector3D        	pos;        //  position
	Vector3D        	normal;     //  normal
	DWORD           	m;          //  bone index
	float           	u, v;       //  texture coordinates

	//  vertex component setters
	void				SetPos				( const Vector3D& p )	{ pos = p; }
	void				SetNormal			( const Vector3D& n )	{ normal = n; }
	void				SetUV				( float tu, float tv )	{ u = tu; v = tv; }
	void				SetBlendI  			( int link, DWORD idx ) { if (link == 0) m = idx; }

	//  vertex component getters
	const Vector3D&		GetPos				() const { return pos; }
	const Vector3D&		GetNormal			() const { return normal; }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }

	int					GetBlendI  			( int link ) const { return m; }
	float				GetBlendW  			( int link ) const { if (link == 0) return 1.0f; else return 0.0f; }
	int     			GetNBlendI 			() const { return 1; }
	int     			GetNBlendW 			() const { return 0; }

	static VertexFormat		format()	{ return vf1W;	}

}; // struct Vertex1W

/*****************************************************************
/*  Class:  Vertex2W                                            
/*  Desc:   Vertex bound to 2 bones									     
/*****************************************************************/
class Vertex2W : public Vertex
{
public:
	Vector3D        pos;        //  position
	Vector3D        normal;     //  normal
	DWORD           m0;         //  bone index 0
	DWORD           m1;         //  bone index 1
	float           w;          //  bone blending weight
	float           u, v;       //  texture coordinates

	//  vertex component setters
	void				SetPos				( const Vector3D& p )	{ pos = p; }
	void				SetNormal			( const Vector3D& n )	{ normal = n; }
	void				SetUV				( float tu, float tv )	{ u = tu; v = tv; }
	void				SetBlendI  			( int link, DWORD idx ) { if (link == 0) m0 = idx; else if (link == 1) m1 = idx; }
	void				SetBlendW  			( int link, float blendW ) { if (link == 0) w = blendW; }


	//  vertex component getters
	const Vector3D&		GetPos				() const { return pos; }
	const Vector3D&		GetNormal			() const { return normal; }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }

	int					GetBlendI  			( int link ) const { if (link == 0) return m0; else return m1; }
	float				GetBlendW  			( int link ) const { if (link == 0) return w; else if (link == 1) return 1.0f - w; else return 0.0f; }
	int     			GetNBlendI 			() const { return 2; }
	int     			GetNBlendW 			() const { return 1; }

	static VertexFormat	format()			{ return vf2W;	}
}; // struct Vertex2W

/*****************************************************************
/*  Class:  Vertex3W                                            
/*  Desc:   Vertex bound to 3 bones									     
/*****************************************************************/
class Vertex3W : public Vertex
{
public:
	Vector3D        pos;        //  position
	Vector3D        normal;     //  normal
	DWORD           m0;         //  bone index 0
	DWORD           m1;         //  bone index 1
	DWORD           m2;         //  bone index 2
	float           w0;         //  bone blending weight 0
	float           w1;         //  bone blending weight 1
	float           u, v;       //  texture coordinates

	//  vertex component setters
	void				SetPos				( const Vector3D& p )	{ pos = p; }
	void				SetNormal			( const Vector3D& n )	{ normal = n; }
	void				SetUV				( float tu, float tv )	{ u = tu; v = tv; }
	void				SetBlendI  			( int link, DWORD idx ) { if (link == 0) m0 = idx; else if (link == 1) m1 = idx; else if (link == 2) m2 = idx; }
	void				SetBlendW  			( int link, float blendW ) { if (link == 0) w0 = blendW; else if (link == 1) w1 = blendW; }


	//  vertex component getters
	const Vector3D&		GetPos				() const { return pos; }
	const Vector3D&		GetNormal			() const { return normal; }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }

	int					GetBlendI  			( int link ) const { if (link == 0) return m0; else if (link == 1) return m1; else return m2; }
	float				GetBlendW  			( int link ) const { if (link == 0) return w0; 
																	else if (link == 1) return w1; 
																	else if (link == 2) return 1.0f - w0 - w1; 
																	else return 0.0f; }
	int     			GetNBlendI 			() const { return 3; }
	int     			GetNBlendW 			() const { return 2; }

	static VertexFormat	format()			{ return vf3W;	}
}; // struct Vertex3W

/*****************************************************************
/*  Class:  Vertex4W                                            
/*  Desc:   Vertex bound to 4 bones									     
/*****************************************************************/
class Vertex4W : public Vertex
{
public:
	Vector3D        pos;        //  position
	Vector3D        normal;     //  normal
	DWORD           m0;         //  bone index 0
	DWORD           m1;         //  bone index 1
	DWORD           m2;         //  bone index 2
	DWORD           m3;         //  bone index 3
	float           w0;         //  bone blending weight 0
	float           w1;         //  bone blending weight 1
	float           w2;         //  bone blending weight 2
	float           u, v;       //  texture coordinates

	//  vertex component setters
	void				SetPos				( const Vector3D& p )	{ pos = p; }
	void				SetNormal			( const Vector3D& n )	{ normal = n; }
	void				SetUV				( float tu, float tv )	{ u = tu; v = tv; }
	void				SetBlendI  			( int link, DWORD idx ) { if (link == 0) m0 = idx; 
                                                                        else if (link == 1) m1 = idx; 
                                                                        else if (link == 2) m2 = idx; 
                                                                        else if (link == 3) m3 = idx; }
	void				SetBlendW  			( int link, float blendW ) { if (link == 0) w0 = blendW; 
                                                                         else if (link == 1) w1 = blendW;
                                                                         else if (link == 2) w2 = blendW;}


	//  vertex component getters
	const Vector3D&		GetPos				() const { return pos; }
	const Vector3D&		GetNormal			() const { return normal; }
	float				GetU				() const { return u; }
	float				GetV				() const { return v; }

	int					GetBlendI  			( int link ) const { if (link == 0) return m0; 
                                                                    else if (link == 1) return m1; 
                                                                    else if (link == 2) return m2; 
                                                                    else return m3; }
	float				GetBlendW  			( int link ) const { if (link == 0) return w0; 
																	else if (link == 1) return w1;
                                                                    else if (link == 2) return w2;
																	else if (link == 3) return 1.0f - w0 - w1 - w2; 
																	else return 0.0f; }
	int     			GetNBlendI 			() const { return 4; }
	int     			GetNBlendW 			() const { return 3; }

	static VertexFormat	format()			{ return vf4W;	}
}; // struct Vertex4W

/*****************************************************************************/
/*	Class:	VertexIterator
/*	Desc:	Iterates vertex arrays of any vertex type
/*	Example:
/*				VertexIterator v;
/*				v << mesh;
/*				while(v)
/*				{
/*					v.z() =+ v.x();
/*					v.diffuse() = 0xFFFFFFFF;
/*					++v;
/*				} 
/*****************************************************************************/
class VertexIterator
{
	BYTE*					pVert;
	int						stride;

	int						diffuseStride;
	int						specularStride;
	int						uvStride;
	int						uv2Stride;
	int						normStride;
    int                     blendWStride;
    int                     blendIStride;

	int						nVert;
	int						cVert;
	VertexFormat			vertexFormat;

public:

	_inl 					VertexIterator();
	_inl 					VertexIterator( BYTE* vbuf, int nV, VertexFormat vf );
	
	//  set vertex iterator in initial position
	_inl void VertexIterator::reset( BYTE* vbuf, int nV, VertexFormat vf );

	// prefix increment operator
	_inl VertexIterator&	operator++();			

	// postfix increment operator
	_inl VertexIterator&	operator++( int );		

	//  true when reached end of vertex array
	_inl operator			bool() const;

	//  casting first three floats to position vector
	_inl operator			Vector3D&();

	_inl Vector3D&			pos();
	_inl Vector3D&			n();

	_inl DWORD&				diffuse();
	_inl DWORD&				specular();

	_inl float&				u();
	_inl float&				v();

	_inl float&				u2();
	_inl float&				v2();
	
	//  position vector of i-th vertex from current
	_inl Vector3D&			operator []( int idx );
	_inl Vector3D&			operator ()( int idx );

	_inl Vector3D&			pos		( int idx );
	_inl Vector3D&			n		( int idx );

	_inl DWORD&				diffuse	( int idx );
	_inl DWORD&				specular( int idx );

	_inl float&				u		( int idx );
	_inl float&				v		( int idx );

	_inl float&				u2		( int idx );
	_inl float&				v2		( int idx );

	_inl bool				HasDiffuse	() const { return diffuseStride		!= -1; }
	_inl bool				HasSpecular	() const { return specularStride	!= -1; }
	_inl bool				HasUV		() const { return uvStride			!= -1; }
	_inl bool				HasUV2		() const { return uv2Stride			!= -1; }
	_inl bool				HasNormal	() const { return normStride		!= -1; }
    _inl bool				HasBlendW	() const { return blendWStride		!= -1; }
    _inl bool				HasBlendI	() const { return blendIStride		!= -1; }

}; // class VertexIterator


const int c_vfStride[] = 
{
	0,	// vfUnknown
	sizeof( VertexTnL	),	
	sizeof( Vertex2t	),	
	sizeof( VertexN		),	
	sizeof( VertexTnL2	),	
	sizeof( VertexT		),	
	sizeof( VertexMP1	),	
	sizeof( VertexNMP1	),	
	sizeof( VertexTnL2S	),	
	sizeof( VertexNMP2	),	
	sizeof( VertexNMP3	),	
	sizeof( VertexNMP4	),	
	sizeof( VertexN2T	),	
	16,		
	16,
	sizeof( VertexTS    ),
    sizeof( Vertex1W    ),
    sizeof( Vertex2W    ),
    sizeof( Vertex3W    ),
    sizeof( Vertex4W    ),
};

const int c_vfUVStride[] = 
{
	-1,	// vfUnknown
		20, // vfTnL
		16, // vf2Tex
		24, // vfN
		20, // vfTnL2
		12, // vfT
		20, // vfMP1			
		32, // vfNMP1
		24, // vfTnL2S
		36, // vfNMP2
		40, // vfNMP3
		44, // vfNMP4
		32, // vfN2T
		-1, // vfXYZD
		-1, // vfXYZW
        20, // vfTS
        28, // vf1W
        36, // vf2W
        44, // vf3W
        52, // vf4W
};

const int c_vfUV2Stride[] = 
{
	-1,	// vfUnknown
	-1, // vfTnL
	24, // vf2Tex
	-1, // vfN
	28, // vfTnL2
	-1, // vfT
	28, // vfMP1			
	40, // vfNMP1
	32, // vfTnL2S
	44, // vfNMP2
	48, // vfNMP3
	52, // vfNMP4
	40, // vfN2T
	-1, // vfXYZD
	-1, // vfXYZW
	-1, // vfTS
    -1, // vf1W
    -1, // vf2W
    -1, // vf3W
    -1, // vf4W
};

const int c_vfDiffuseStride[] = 
{
	-1,	// vfUnknown
		16, // vfTnL
		12, // vf2Tex
		-1, // vfN
		16, // vfTnL2
		-1, // vfT
		16, // vfMP1			
		28, // vfNMP1
		16, // vfTnL2S
		32, // vfNMP2
		36, // vfNMP3
		40, // vfNMP4
		24, // vfN2T
		12, // vfXYZD
		-1, // vfXYZW
		12,  // vfTS
        -1, // vf1W
        -1, // vf2W
        -1, // vf3W
        -1, // vf4W
};

const int c_vfSpecularStride[] = 
{
        -1,	// vfUnknown
        20, // vfTnL
        -1, // vf2Tex
        -1, // vfN
        -1, // vfTnL2
        -1, // vfT
        -1, // vfMP1			
        -1, // vfNMP1
        20, // vfTnL2S
        -1, // vfNMP2
        -1, // vfNMP3
        44, // vfNMP4
        28, // vfN2T
        -1, // vfXYZD
        -1, // vfXYZW
		16,  // vfTS
        -1, // vf1W
        -1, // vf2W
        -1, // vf3W
        -1, // vf4W
};

const int c_vfNormalStride[] = 
{
	-1,	// vfUnknown
		-1, // vfTnL
		-1, // vf2Tex
		12, // vfN
		-1, // vfTnL2
		-1, // vfT
		-1, // vfMP1			
		16, // vfNMP1
		-1, // vfTnL2S
		20, // vfNMP2
		24, // vfNMP3
		28, // vfNMP4
		12, // vfN2T
		-1, // vfXYZD
		-1, // vfXYZW
		-1,  //vfTS
        12, // vf1W
        12, // vf2W
        12, // vf3W
        12, // vf4W
}; 

const int c_vfBlendWStride[] = 
{
    -1,	// vfUnknown
    -1, // vfTnL
    -1, // vf2Tex
    -1, // vfN
    -1, // vfTnL2
    -1, // vfT
    -1, // vfMP1			
    -1, // vfNMP1
    -1, // vfTnL2S
    12, // vfNMP2
    12, // vfNMP3
    12, // vfNMP4
    -1, // vfN2T
    -1, // vfXYZD
    -1, // vfXYZW
	-1,  // vfTS
    -1, // vf1W
    -1, // vf2W
    -1, // vf3W
    -1, // vf4W
};

const int c_vfBlendIStride[] = 
{
    -1,	// vfUnknown
    -1, // vfTnL
    -1, // vf2Tex
    -1, // vfN
    -1, // vfTnL2
    -1, // vfT
    12, // vfMP1			
    12, // vfNMP1
    -1, // vfTnL2S
    16, // vfNMP2
    20, // vfNMP3
    24, // vfNMP4
    -1, // vfN2T
    -1, // vfXYZD
    -1, // vfXYZW
    -1,  // vfTS
    -1, // vf1W
    -1, // vf2W
    -1, // vf3W
    -1, // vf4W
};

#ifdef _INLINES
#include "rsVertex.inl"
#endif // _INLINES

#endif // __S_CUSTOMVERTEX_H__