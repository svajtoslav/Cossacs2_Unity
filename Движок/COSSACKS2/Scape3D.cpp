/*****************************************************************************/
/*	File:	Scape3D.cpp
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	14.11.2002
/*****************************************************************************/
#pragma pack( push )  
#pragma pack( 8 ) 
#include <malloc.h>

#include "windows.h"
#include "DDini.h" 

#include "gmDefines.h"
#include "kAssert.h"
#include "GP_Draw.h"
#include "Scape3D.h"
#include "IMediaManager.h"
#include "ITerrain.h"
#include "kInput.h"
#include "kResfile.h"
#include "kIO.h"
#include "kContext.h"
#include "IShadowManager.h"
#include "akField.h"
#include "kSystemDialogs.h"
#include "vStaticTerrain.h"
#include "kTimer.h"

#include <direct.h>


extern FieldModel	g_FieldModel;
int			LOD = 0;
bool		g_bPerspCamera		= true; 
bool		g_bDoBillboarding	= false;
bool		g_bDoSkew			= true;

Rct         g_ScreenViewport;
Matrix4D    g_CameraViewProjM;
Matrix4D    g_ScreenToWorldSpaceTM;
Matrix4D    g_CameraViewM;


Vector3D	g_FrustumXCorners[12];
Frustum		g_Frustum;

float		g_CameraDistance	= 4096.0f;
float		g_TerrainZBias		= 10.0f;
float       g_InitFOV           = 0.0f;
bool        g_bCameraChanged    = true;

ICamera*    g_pLastCamera       = NULL;
Vector3D    g_LastPos;
Vector3D    g_LastDir;

Matrix4D	g_CameraWorldTM;

ICamera*	ICam = NULL;

bool		g_bNeedUpdateTerrainCache = false;
Rct			g_DirtyTerrainRect = Rct::null;
float		g_StartZ = 0.0f, g_EndZ = 1.0f, g_ZRange = 1.0f, g_WorldZRange = 1.0f; 
float       g_ZNear = 1.0f;
float       g_ZFar  = 100.0f;

extern int Shifter;
extern bool NOPAUSE;

bool CreateTerrainDB( StaticTerrainDB& db );


const Matrix4D c_SkewTM( 1.0f, 0.0f,    0.0f,       0.0f,
						 0.0f, 1.0f,    0.0f,       0.0f,
						 0.0f, -0.5f,   c_CosPId6,  0.0f,
						 0.0f, 0.0f,    0.0f,       1.0f );

const Matrix4D c_UnskewTM(  1.0f, 0.0f,    0.0f,       0.0f,
                            0.0f, 1.0f,    0.0f,       0.0f,
                            0.0f, 0.5f/c_CosPId6,   1.0f/c_CosPId6,  0.0f,
                            0.0f, 0.0f,    0.0f,       1.0f );

void SetTerrainZBias( float bias )
{
	g_TerrainZBias = bias;
}

float GetTerrainZBias()
{
	return g_TerrainZBias;
}

void SetBillboardingMode( bool bSet )
{
    g_bDoBillboarding   = bSet;	
    g_bDoSkew           = !bSet;			    
}

const Matrix4D&	GetSkewTM()
{
	return g_bDoSkew ? c_SkewTM : Matrix4D::identity;
}

int g_TestTex;
extern int ADDSH;
DWORD GetMaxMapX()
{
	return (240 << ADDSH)*32;
}

DWORD GetMaxMapY()
{
	return (240 << ADDSH)*32;
}

class Tweaker 
{
public:
	Tweaker()
	{
		m_Scale		= 1.2f;
		m_Angle		= 0.0f;//c_PId6 + 0.1f;
		m_MapScaleY	= 0;
	}

	float		m_Scale;
	float		m_Angle;
	int			m_MapScaleY;

}; // class Tweaker


Tweaker*  g_pTweaker = NULL;
extern int VirtLx;
extern int VirtLy;

void RenderSurfaceToTexture(int TexID,int MapX,int MapY,int ScaleDeep,int HeightScale);
/*****************************************************************************/
/*	Globals implementation
/*****************************************************************************/
Vector3D SkewPt( float x, float y, float z )
{
	return g_bDoSkew ? Vector3D( x, y - 0.5f*z, z*c_CosPId6 ) : Vector3D( x, y, z ); 
}

float zHeightScale = 0.7f;
bool CreateGroundTexture( int texID, const Rct& mapExt )
{
	int qx = mapExt.x;
	int qy = mapExt.y;

	int qlod = 0;
	if (mapExt.w > 256.0f && mapExt.w <= 512.0f) qlod = 1;
	if (mapExt.w > 512.0f && mapExt.w <= 1024.0f) qlod = 2;

	RenderSurfaceToTexture( texID, qx, qy>>1, qlod, zHeightScale*256 );
	return true;
} // CreateGroundTexture

const float c_TerraUVBorder = 0.0f;

typedef VertexN TerrainVertex;

_inline void Vbias(TerrainVertex* V){
	if(V->v==1.0f)V->z--;
	if(V->v==0.0f)V->z++;
}

void FindAppVertex(TerrainVertex* v,TerrainVertex* V,int nv,int pos){
	TerrainVertex* v1=V;
	if(v->v<0&&pos<nv-9){
		//upsearch	
		int cpos=pos;
		while(v1->v<0&&cpos<nv){
			v1+=9;
			cpos+=9;
		}
		if(v1->v>=0){
			TerrainVertex* v0=v1-9;
			float dvdy=(v1->v-v0->v)/(v1->y-v0->y);
			float dzdy=(v1->z-v0->z)/(v1->y-v0->y);
			v->y=v0->y-v0->v/dvdy;
			v->z=v0->z+(v->y-v0->y)*dzdy;			
		}                
	}
	if(v->v>1.0f){
		//downsearch
		int cpos=pos;
		while(v1->v>1.0f&&cpos>=9){
			v1-=9;
			cpos-=9;
		}
		if(v1->v<=1.0f){
			TerrainVertex* v0=v1+9;
			float dvdy=(v1->v-v0->v)/(v1->y-v0->y);
			float dzdy=(v1->z-v0->z)/(v1->y-v0->y);
			v->y=v0->y+(1.0f-v0->v)/dvdy;
			v->z=v0->z+(v->y-v0->y)*dzdy;			
		}
	}
	clamp(v->v,0.0f,1.0f);
}
Vector3D GetPointDisplacement(int Index);
int GetHeightCB( int x, int y );
bool CreateGroundGeometry( const Rct& mapExt, int lod )
{
    BaseMesh* pMesh = ITerra->AllocateGeometry();
    if (!pMesh) return false;
    BaseMesh& mesh = *pMesh;
	Rct dext( mapExt ); dext.y -= mapExt.h * 2.0f; dext.h *= 5;  
	CreateJaggedGrid<TerrainVertex>( mesh, dext, 8, 80 );	 
	VertexIterator vit;
	vit << mesh;
	float dv=int(0.5f+mapExt.y/mapExt.h);
	float du=int(0.5f+mapExt.x/mapExt.w);
	while (vit)
	{
		Vector3D& v = vit;
		v.z = GetHeight( v.x, v.y );		
		vit.u()=(v.x)/mapExt.w-du;
		vit.v()=(v.y/2.0f-(v.z*zHeightScale))/mapExt.w-dv;
		vit.n() = ITerra->GetNormal( v.z, v.y );
		//vit.diffuse() = 0xFFFFFFFF;
		++vit;
	}

	WORD* idx = mesh.getIndices();
	TerrainVertex* v = (TerrainVertex*)mesh.getVertexData();
	int	nVert = mesh.getNVert();

	static TerrainVertex*	vbuf = NULL;
	static WORD*			fbuf = NULL; // vertex reindexing buffer
	static int maxv=0;
	if(nVert>maxv){
		delete []vbuf;
		delete []fbuf;
		vbuf = new TerrainVertex[nVert];
		fbuf = new WORD[nVert];
		maxv = nVert;
	}
	memcpy( vbuf, v, nVert*sizeof TerrainVertex );
	memset( fbuf, 0xFFFF, nVert*sizeof(WORD) );

	int cIdx = 0;
	int nIdx = mesh.getNInd();

	for (int i = 0; i < nIdx; i += 3)
	{
		int i1 = idx[i  ];
		int i2 = idx[i+1];
		int i3 = idx[i+2]; 
		TerrainVertex* v1 = v + i1;        
		TerrainVertex* v2 = v + i2;
		TerrainVertex* v3 = v + i3;

		TerrainVertex* V1 = vbuf + i1;        
		TerrainVertex* V2 = vbuf + i2;
		TerrainVertex* V3 = vbuf + i3;

		bool in1 = v1->v>0 && v1->v<1.0f;
		bool in2 = v2->v>0 && v2->v<1.0f;
		bool in3 = v3->v>0 && v3->v<1.0f;

		if (in1 || in2 || in3)
		{
			idx[cIdx  ] = i1;
			idx[cIdx+1] = i2;
			idx[cIdx+2] = i3;	

			fbuf[i1] = 0;
			fbuf[i2] = 0;
			fbuf[i3] = 0;

			if ( (!in1) || (!in2) || (!in3) )
			{
				FindAppVertex(V1,v1,nVert,i1);
				FindAppVertex(V2,v2,nVert,i2);
				FindAppVertex(V3,v3,nVert,i3);
			}
			cIdx+=3;
		}	
	}

	//  reindex vertices	
	int cV = 0;
	for (int i = 0; i < nVert; i++)
	{	
		if (fbuf[i] == 0) 
		{
			fbuf[i] = cV;  
			TerrainVertex& tv = v[cV++] = vbuf[i];
			int ix = (tv.x+0.001)/32;
			int iy = ((tv.y-0.1)/32)+1;
			extern int VertInLine;
			/*if(iy>0&&iy<VertInLine-2){			
				int vv=ix+iy*VertInLine;
				Vector3D GetPointDisplacement(int x,int y);
				Vector3D V=GetPointDisplacement(tv.x,tv.y);
				tv.x+=V.x;
				tv.y+=V.y;
				tv.z+=V.z;
			}*/
		}
	}

	//  write new indices to the triangles
	for (int i = 0; i < cIdx; i += 3)
	{
		idx[i  ] = fbuf[idx[i  ]];
		idx[i+1] = fbuf[idx[i+1]];
		idx[i+2] = fbuf[idx[i+2]];
	}

 	mesh.setNInd	( cIdx	 );
	mesh.setNVert	( cV	 );
	mesh.setNPri	( cIdx/3 );

	const Matrix4D& skTM = GetSkewTM();
	mesh.transform( skTM );	

    static int shID = IRS->GetShaderID( "terra_shadowed" );
    mesh.setShader( shID );

	return true;
} // CreateGroundGeometry

int GetTotalHeight(int x,int y);

extern bool Mode3D;
int GetHeightCB( int x, int y )
{
	if (!(THMap&&Mode3D)) return 0;
	int z=GetTotalHeight(x,y);
	if(z<0)z=0;
	return z;
}

void GetAABB( const Rct& mapExt, AABoundBox& aabb )
{
	aabb = AABoundBox( mapExt, 1000000.0f, -1000000.0f );

	float yborder = mapExt.h*4.0f;
	// hack here - we live in the skewed world
	float bx = mapExt.x;
	float by = mapExt.y - yborder;

	float ex = mapExt.GetRight();
	float ey = mapExt.GetBottom() + yborder;
	
	float dx = (ex - bx) / 32.0f;
	float dy = (ey - by) / 32.0f;

	if (dx < c_SmallEpsilon) dx = c_SmallEpsilon;
	if (dy < c_SmallEpsilon) dy = c_SmallEpsilon;

	for (float x = bx; x <= ex; x += dx)
	{
		for (float y = by; y <= ey; y += dy)
		{
			float h = ITerra->GetH( x, y );
			if (h < aabb.minv.z) aabb.minv.z = h;
			if (h > aabb.maxv.z) aabb.maxv.z = h;
		}
	}
} // GetAABB

void InitMapBorder()
{
    ITerra->SetBorderConfigFile( "Models\\Scripts\\mapborders.xml" );
}

bool ITCreateGeometry(const Rct &, int);
void SelectSurfaceType(bool New)
{
	if(New)
    {
		ITerra->SetCallback((TextureCallback)NULL);		
		ITerra->SetCallback((GeometryCallback)ITCreateGeometry);
        g_bDoSkew           = false;
        g_bDoBillboarding   = true;
	}else
    {
		ITerra->SetCallback	( CreateGroundTexture	);
		ITerra->SetCallback ( CreateGroundGeometry	);
	}
} // SelectSurfaceType

DWORD GetObjectsShadowColor();
DWORD GetObjectsShadowQuality();
bool NewSurface;
void InitGroundZbuffer()
{ 
	bool CheckIfNewTerrain();
	NewSurface = CheckIfNewTerrain();	
	

	InputDispatcher::SetCoreDispatcher( &InputManager::instance() );
	InputDispatcher::Init();

	g_FieldModel.Init();
	IMM->Init();
	IMM->ShowNode( IMM->GetNodeID( "EditorSetup" ), false );
	IMM->ShowNode( IMM->GetNodeID( "GameSetup" ), true );
	IPMgr->EnableBatching();

	InputManager::instance().SetActive( false );

	static terrID = IMM->GetNodeID( "terra" );
	if (!ITerra) return;

	ITerra->SetCallback( GetHeightCB );
	ITerra->SetCallback( GetAABB );

	SelectSurfaceType(NewSurface);
	
	ITerra->SetLODBias	( 5000.0f );
	ITerra->EnableVertexLighting( false );

    InitMapBorder();
	
	int mapSideX = (256 << ADDSH)*32;
	int mapSideY = (256 << ADDSH)*64;
	ITerra->SetHeightmapPow( (256 << ADDSH) >> 3 );
	ITerra->SetExtents( Rct( 0, 0, mapSideX, mapSideY ) ); 
    IShadowMgr->Enable( true );
    IShadowMgr->SetShadowColor( GetObjectsShadowColor() );
    IShadowMgr->SetShadowQuality( (ShadowQuality)GetObjectsShadowQuality() );
    IShadowMgr->Init();
    
	ISM->SetZBias( 0.0f );

} // InitGroundZbuffer

void InvalidateTerrainPatch( int x, int y, int w, int h )
{
	if (!g_bNeedUpdateTerrainCache)
	{
		g_bNeedUpdateTerrainCache = true;
		g_DirtyTerrainRect = Rct( x, y, w, h );
	}
	g_DirtyTerrainRect.Union( Rct( x, y, w, h ) );
} // InvalidateTerrainPatch

void GetGroundNormal( int x, int y, Vector3D& vec )
{
	float l = GetHeight( x - 64, y );		
	float r = GetHeight( x + 64, y );		
	float u = GetHeight( x, y - 64 );		
	float d = GetHeight( x, y + 64 );		
	
	vec.x = -(r - l) * 0.015625f * 0.5f;
	vec.y = -(d - u) * 0.015625f * 0.5f;
	vec.z = 1.0f;
	vec.normalize();
} // GetGroundNormal

const float c_1divStep = 0.0078125f;
void GetGroundNormal( int x, int y, Vector4D& vec )
{
	float l = GetHeight( x - 64, y );		
	float r = GetHeight( x + 64, y );		
	float u = GetHeight( x, y - 64 );		
	float d = GetHeight( x, y + 64 );		
	
	vec.x = -(r - l) * c_1divStep;
	vec.y = -(d - u) * c_1divStep; 
	vec.z = 1.0f;
	vec.w = 0.0f;
	vec.normalize(); 
} // GetGroundNormal 

void HandleWater();
void SetLight( DWORD ambient, DWORD diffuse, DWORD specular, const Vector3D* dir )
{
	ILight* iLight = GetGameLight();
	if (!iLight) return;
	iLight->SetAmbient( ambient );
	iLight->SetDiffuse( ambient );
	iLight->SetSpecular( ambient );
	if (dir) iLight->SetDir( *dir );
	iLight->Render();
} // SetLight

void GetLight( DWORD& ambient, DWORD& diffuse, DWORD& specular, Vector3D& dir )
{
	ILight* iLight = GetGameLight();
	if (!iLight) return;
	ambient = iLight->GetAmbient();
	diffuse = iLight->GetDiffuse();
	specular = iLight->GetSpecular();
	dir = iLight->GetDir();
} // GetLight

void ApplyGroundZBias(bool TurnOn){
    float bias = tmin( g_TerrainZBias, g_ZNear - 10.0f );
	ICam->ShiftZ( TurnOn?-bias:bias );
	ICam->Render();
}
/*---------------------------------------------------------------------------*/
/*	Func:	FillGroundZBuffer	
/*	Desc:	Draws z values of ground pixels into device z buffer
/*---------------------------------------------------------------------------*/
void FillGroundZBuffer()
{ 	
	static DWORD matID	 = IMM->GetModelID( "GameMaterial" ); 
	IMM->Render( matID );

	extern int Shifter;
	LOD = 5 - Shifter;  

	IRS->ResetWorldMatrix();
    ITerra->SetCallback	( GetHeight );
    
	if (ITerra) 
	{
		if (g_bNeedUpdateTerrainCache)  
		{
			ITerra->InvalidateAABB		( &g_DirtyTerrainRect );
			ITerra->InvalidateTexture	( &g_DirtyTerrainRect ); 
			ITerra->InvalidateGeometry	( &g_DirtyTerrainRect );
			g_bNeedUpdateTerrainCache = false;
		}

		ITerra->ForceLOD( LOD );
		ITerra->Render();
	}
	
	if (GetKeyState( 'M' ) < 0 &&
		GetKeyState( 'N' ) < 0 &&
		GetKeyState( VK_CONTROL ) < 0)
	{
		static DWORD lastTime1 = 0;
		if (GetTickCount() - lastTime1 > 500)
		{
			IRS->RecompileAllShaders();
			IMM->ReloadModels();
            InitMapBorder();
			lastTime1 = GetTickCount();
		}
	}

    if (GetKeyState( 'B' ) < 0 &&
        GetKeyState( 'N' ) < 0 &&
        GetKeyState( VK_CONTROL ) < 0)
    {
        IRS->ReloadAllTextures();
    }

    /*if (GetKeyState( '1' ) < 0 &&
        GetKeyState( '2' ) < 0 &&
        GetKeyState( '3' ) < 0 &&
        GetKeyState( VK_CONTROL ) < 0)
    {
        _chdir( GetRootDirectory() );
        _chdir( "Models" );

        SaveFileDialog dlg;
        dlg.AddFilter( "Terrain files", "*.ter" );
        dlg.SetDefaultExtension( "ter" );
        static char lpstrFile[_MAX_PATH];
        if (dlg.Show()) 
        {
            const char* fileName = dlg.GetFilePath();
            if (!fileName) return;
            StaticTerrainDB db;
            CreateTerrainDB( db );
            FOutStream os( fileName );
            if (!os.NoFile()) db.Serialize( os );
        }

        _chdir( GetRootDirectory() );
    }*/

	IEffMgr->Pause( !NOPAUSE ); 
    
    ITerra->SetCallback	( GetHeightCB );
    
    IShadowMgr->SetShadowColor( GetObjectsShadowColor() );
    IShadowMgr->SetShadowQuality( (ShadowQuality)GetObjectsShadowQuality() );
} // FillGroundZBuffer

void UpdateCoastWaves()
{
    static int wavesID = IMM->GetModelID( "Models\\Effects\\waves.c2m" );
    Matrix4D ctm;

    Vector3D pos = ICam->GetPos();
    Vector3D dir = ICam->GetDir();
    float t = -pos.z/dir.z;
    ctm.translation( pos.x + t*dir.x, pos.y + t*dir.y, 0.0f );
    ctm.e32 = 0.0f;

    PushEntityContext( 0xF00DF00D );
    IMM->Render( wavesID, &ctm );
    PopEntityContext();
} // UpdateCoastWaves

void ShowLoadProgress(char* Mark, int v, int vMax);
void ResetGroundCache() 
{
	if (ITerra) 
	{
		int L0=20;
		int L1=50;
		//
		//ShowLoadProgress("PrepareGameMedia",25,1000);
		//
		ITerra->InvalidateAABB();
		//
		//ShowLoadProgress("PrepareGameMedia",30,1000);
		//
		ITerra->InvalidateTexture();
		//
		//ShowLoadProgress("PrepareGameMedia",38,1000);
		//
		ITerra->InvalidateGeometry();
		//
		//ShowLoadProgress("PrepareGameMedia",42,1000);
		//
	}
} // ResetGroundCache

void FlushFieldImage()
{
	IRS->ResetWorldMatrix();
	g_FieldModel.Draw();
} // FlushFieldImage

void DrawFPatch(int x,int y,int z,int W,int H,float G,float S){
	Vector4D normal;
	bool res = false;
	int D=(W*14142)/20000;
	while (!res)
	{
		Vector3D lt = SkewPt( x, y + D, GetHeight( x, y + D ) );
		Vector3D rt = SkewPt( x + D, y, GetHeight( x + D, y ) );
		Vector3D lb = SkewPt( x - D, y, GetHeight( x - D, y ) );
		Vector3D rb = SkewPt( x, y - D, GetHeight( x, y - D ) );
		res = g_FieldModel.AddPatch( lt, rt, lb, rb, G, S );
		if (res) break; 
		FlushFieldImage();
	}
} // DrawFPatch

Matrix4D GetOrientedBillboardTransform( const Vector3D& pivot, float ang, float scaleY )
{
	Vector3D trans( pivot ); trans.reverse();
	Matrix4D tr;
	tr.translation( trans );
	
	Matrix3D rot;
	rot.rotationXY( cosf( ang ), sinf( ang ) );
	tr *= rot;

	Matrix4D sc;
	sc.scaling( 1.0f, scaleY, 1.0f );
	tr *= sc;
	return tr;
} // GetOrientedBillboardTransform

Matrix4D GetAlignTerraTransform( const Vector3D& pos, const Vector3D& pivot )
{
	Vector3D trans( pivot ); trans.reverse();
	Matrix4D tr( Vector3D( 1.0f, 1.0f, 1.0f ), Matrix3D::identity, trans );
	tr.e01 *=2.0f;
	tr.e11 *=2.0f;
	tr.e21 *=2.0f;
	tr.e31 *=2.0f;
	return tr;
} // GetAlignTerraTransform

Matrix4D GetAlignGroundTransform( const Vector3D& pivot )
{
	Vector3D trans( pivot ); trans.reverse();
	Matrix4D tr( Vector3D( 1.0f, 1.0f, 1.0f ), Matrix3D::identity, trans );
	tr.e01 *=2.0f;
	tr.e11 *=2.0f;
	tr.e21 *=2.0f;
	tr.e31 *=2.0f;
	return tr;
} // GetAlignGroundTransform

Matrix4D GetTopmostTransform( const Vector3D& pivot, const Vector3D& pos )
{
	Matrix4D tm = GetBillboardTransform( pivot );
	tm.translate( pos );
	return tm;
} // GetTopmostTransform

Matrix4D GetAlignLineTransform( const Vector3D& pivot, int x1, int y1, int x2, int y2 )
{
	Matrix4D trAlign;
	//  calculate aligning transform
	//  angle of aligning line in ground plane
	float dy = y2 - y1, dx = x2 - x1;
	dy /= 0.5f;
	float len = sqrtf( dx*dx + dy*dy );
	float cosPhi = dx / len;
	float sinPhi = dy / len;

	//  shear and shift to match aligning line in world space
	Matrix3D m;
	m.shearXZ( cosPhi, sinPhi );
	Matrix4D shM( m );
	Vector3D center( -float( x1 + x2 ) * 0.5f, -float( y1 + y2 ) * 0.5f, 0.0f );
	Matrix4D trToC, trFromC;
	trToC.translation( center );
	center.reverse();
	center.z += ((y1 + y2)*0.5f- pivot.y)/0.5f;
	trFromC.translation( center );
	trAlign = trToC;
	trAlign *= shM;
	trAlign *= trFromC;

	//  transform from sprite space to world space 
	Vector3D pshift( pivot ); pshift.reverse();
	Matrix3D rot;
	
	//if (g_bDoSkew) 
	//{
		rot.rotationYZ( 0.5f, -c_CosPId6 );
	/*}
	else
	{
		rot.rotationYZ( 0.0, -1.0f );
	}*/

	Matrix4D sToW;
	sToW.translation( pshift );
	sToW *= rot;

	//  combine transforms
	trAlign *= sToW;
	return trAlign;
} // GetAlignLineTransform

Matrix4D GetAlignLineTransformWithScape( const Vector3D& Center,const Vector3D& pivot, int x1, int y1, int x2, int y2 )
{
	Matrix4D trAlign;

	float xc=pivot.x;
	float yc=pivot.y;

	int z1=GetHeight(Center.x+x1-xc,Center.y+(y1-yc)*2);
	int z2=GetHeight(Center.x+x2-xc,Center.y+(y2-yc)*2);
	float dz=128;
	Vector2Df S1(x1,y1);
	Vector2Df S2(x2,y2);
	Vector2Df S3((x1+x2)/2,(y1+y2)/2-dz);
	float bias=0.2f;
	if(y1<y2)bias=-bias;
	Vector3D BV(0,-bias,bias/2.0f);
	Vector3D W1=SkewPt(Center.x+x1-xc,Center.y+2*(y1-yc),z1);
	W1+=BV;	
	Vector3D W2=SkewPt(Center.x+x2-xc,Center.y+2*(y2-yc),z2);
	W2-=BV;
	Vector3D W3=SkewPt(Center.x,Center.y,(z1+z2)/2+dz);
	trAlign.TransformationFromPlaneToWorld(S1,S2,S3,W1,W2,W3);
	return trAlign;
} // GetAlignLineTransformWithScape

Matrix4D GetRolledBillboardTransform( const Vector3D& pivot, float ang )
{
	Matrix3D rot;
	if (!g_bDoBillboarding)
	{
		float cosAng = cosf( ang );
		float sinAng = sinf( ang );	
		rot.rotationYZ( cosAng, sinAng );

		if (g_bDoSkew) 
		{
			rot.rotationYZ( 0.5f, -c_CosPId6 );
		}
		else
		{
			rot.rotationYZ( 0.0, -1.0f );
		}
	}
	else
	{
        rot = g_CameraWorldTM; 
        rot.getV1().reverse();
        //rot.getV2().reverse();
        //rot.setIdentity();
        //rot.getV2() *= c_CosPId6;
	}

	Vector3D trans( pivot ); trans.reverse();
	Matrix4D tr;
	tr.translation( trans );	
	tr *= rot;	
	return tr;
} // GetRolledBillboardTransform

Matrix4D GetBillboardTransform( const Vector3D& pivot )
{
	Vector3D trans( pivot ); trans.reverse();
	Matrix4D tr;
	tr.translation( trans );	
	
	Matrix3D rot;
	
	if (g_bDoSkew) 
	{
		rot.rotationYZ( 0.5f, -c_CosPId6 );
	}
	else
	{
		rot.rotationYZ( 0.0, -1.0f );
	}

	tr *= rot;
	return tr;
} // GetAlignGroundTransform

Matrix4D GetPseudoProjectionTM( const Vector3D& pos, float& planeFactor )
{
    Vector4D oO = pos;
    Vector4D oX( 1, 0, 0, 0 );
    Vector4D oY( 0, 1, 0, 0 );
    Vector4D oZ( 0, 0, 1, 0 );
    
    oX += oO; oY += oO; oZ += oO;
    
    ICam->WorldToScreenSpace( oO );
    ICam->WorldToScreenSpace( oX );
    ICam->WorldToScreenSpace( oY );
    ICam->WorldToScreenSpace( oZ );
    
    oX -= oO;
    oY -= oO;
    oZ -= oO;
	float scaleY = 1.0f + (2.0f*oY.y/oX.x - 1.0f)*planeFactor;						
	planeFactor=scaleY;
    
    Vector3D objDir = ICam->GetPos();
    objDir -= pos;
    objDir.normalize();
    
    Matrix3D M42( Vector3D::oX, objDir, Vector3D( 0.0f, -0.5f, c_CosPId6*scaleY ) );
    const float c_MDet = 0.25f + c_CosPId6*c_CosPId6;
    const Matrix3D M41 = Matrix3D(  1.0f, 0.0f,             0.0f,
                                    0.0f, c_CosPId6/c_MDet, -0.5f/c_MDet,
                                    0.0f, 0.5f/c_MDet,      c_CosPId6/c_MDet );
    M42.mulLeft( M41 );
    Matrix4D m;
    m.e00 = oX.x; m.e01 = oX.y; m.e02 = oX.z; m.e03 = 0.0f;
    m.e10 = oY.x; m.e11 = oY.y; m.e12 = oY.z; m.e13 = 0.0f; 
    m.e20 = oZ.x; m.e21 = oZ.y; m.e22 = oZ.z; m.e23 = 0.0f; 
    m.e30 = oO.x; m.e31 = oO.y; m.e32 = oO.z; m.e33 = 1.0f; 											
    m.mulLeft( M42 );
	//Matrix3D mdet( m );
	//float det = mdet.det();
    return m;
} // GetPseudoProjectionTM

float GetZRange()
{
    return g_WorldZRange;	
}

ICamera* GetGameCamera()
{
	static DWORD s_GameCameraID	 = IMM->GetNodeID( "GameCamera" );
	static DWORD s_PerspCameraID = IMM->GetNodeID( "AltCamera" );
	return g_bPerspCamera ? IMM->GetCamera( s_PerspCameraID ) :  
							IMM->GetCamera( s_GameCameraID );
} // GetGameCamera

ILight* GetGameLight()
{
	static DWORD s_GameLightID = IMM->GetNodeID( "GameLight" );
	return IMM->GetLight( s_GameLightID );
} // GetGameCamera

bool BE_UseCamera();

float fMapX=0;
float fMapY=0;
void LimitCamera();
void ShiftCamera(float dx,float dy){
	fMapX+=dx;
	fMapY+=dy;
	LimitCamera();
}
void SetCameraPos(float x,float y){
	fMapX=x;
	fMapY=y;
	LimitCamera();
	void SetCentralUnit(OneObject* OB);
	SetCentralUnit(NULL);
}
void SetCameraPosEx(float x,float y){
	fMapX=x;
	fMapY=y;
	LimitCamera();
}
float GetYawByZoom(float zoom){
	float n_yaw=c_PI/6.0f;
	float min_yaw=c_PI/8.0f;
	if(zoom<0.0f){
        return n_yaw-(min_yaw-n_yaw)*zoom/1100.0f;
	}else{
        return n_yaw;
	}
}
float roll	= 0.0f;
float zoom	= 0.0f;
float yaw	= c_PI/6.0f;
float fov   = g_InitFOV;

enum SwitchLMode
{
    slNone = 0,
    slTo1x = 1,
    slTo2x = 2
}; // enum SwitchLMode

Timer           g_LTimer;
SwitchLMode     g_SwitchLMode           = slNone;
const float     c_AnimateLModeTime      = 1.0f;
DWORD           g_StartAnimLModeFrame   = 0;
DWORD           g_NAnimLModeFrames      = 0;
Vector3D        g_AnimCameraDirection   = Vector3D::null;
float           g_AnimCameraLen         = 0.0f;

extern int Flips;
void AnimateLModeSwitch( const Vector3D& moveDir, bool bToL )
{
    g_LTimer.start();
    g_SwitchLMode           = bToL ? slTo2x : slTo1x;
    g_StartAnimLModeFrame   = IRS->GetCurFrame();
    g_NAnimLModeFrames      = c_AnimateLModeTime*float( Flips ) + 1;
    g_AnimCameraDirection   = moveDir;
    g_AnimCameraLen         = g_AnimCameraDirection.normalize();
} // AnimateLModeSwitch

void UnapplyLMode();
void SetupCamera()
{
    ICam = GetGameCamera();
    if (!ICam) return;
    if (g_InitFOV == 0.0f) g_InitFOV = ICam->GetFOV();
	bool bDrivenCamera  = BE_UseCamera();
	float scale		    = float(1<<(5-Shifter));
    
    //  lmode switching camera animation
    DWORD curF = IRS->GetCurFrame();

    if (curF < g_StartAnimLModeFrame + g_NAnimLModeFrames)
    {
        float ratio = float( curF - g_StartAnimLModeFrame )/float( g_NAnimLModeFrames );
        float dfx = 0.0f, dfy = 0.0f;
        if (g_SwitchLMode == slTo2x) { scale = 1.0f + ratio; dfx = -RealLx*0.5f; dfy = -RealLy; }
        if (g_SwitchLMode == slTo1x) { scale = 2.0f - ratio; dfx = RealLx*0.5f; dfy = RealLy; }
        Vector3D moveDir( g_AnimCameraDirection );
        static s_PrevFrame = curF;
        if (curF != s_PrevFrame)
        {
            fMapX += (moveDir.x*g_AnimCameraLen + dfx)/(32.0f*float( g_NAnimLModeFrames ));
            fMapY += (moveDir.y*g_AnimCameraLen + dfy)/(32.0f*float( g_NAnimLModeFrames ));
            if (curF == g_StartAnimLModeFrame + g_NAnimLModeFrames - 1)
            {
                if (g_SwitchLMode == slTo1x) UnapplyLMode();
            }
        }
        s_PrevFrame = curF;
    }else g_SwitchLMode = slNone;

	float viewVol	= RealLx * scale;	

    bool bBillboarding = bDrivenCamera | NewSurface;
    if (g_bDoSkew && bBillboarding) 
    {
        SetBillboardingMode( true );
        ITerra->InvalidateGeometry();
        ITerra->InvalidateTexture();
    }
    else if (!g_bDoSkew && !bBillboarding)
    {
        SetBillboardingMode( false );
        ITerra->InvalidateGeometry();
        ITerra->InvalidateTexture();
    }
	
    if (bDrivenCamera)
	{
		Vector3D pt = ICam->GetPos();
		Ray3D ray( ICam->GetPos(), ICam->GetDir() );
		Plane::xOy.intersect( ray, pt );	
		fMapX = (pt.x - viewVol/2) / c_WorldGridStepX;
		fMapY = (pt.y - RealLy*scale) / c_WorldGridStepY;
		roll	= 0.00f;
		zoom	= 0.0f;
		yaw		= c_PI/6.0f;
        fov     = g_InitFOV; 
	}		
	else
	{
		Matrix4D rotM( Matrix4D::identity );
		static float rollRate	= DegToRad( 0.04f );
		static float zoomRate	= 1.8f;
		static float yawRate	= DegToRad( 0.03f );
        static float fovStep    = DegToRad( 0.2f );
		
		static float maxYaw		= c_PI/6.0f;
		static float minYaw		= 0.3f;//c_PI/6.0f;//0.17f;
		static float minZoom	= -1100.0f;
		static float maxZoom	= 0.0f;//200.0f;


		static DWORD prevTick		= GetTickCount();
		DWORD curTick = GetTickCount();;

		float zoomStep = (curTick - prevTick)*zoomRate;
		float yawStep  = (curTick - prevTick)*yawRate;
		float rollStep = (curTick - prevTick)*rollRate;

		prevTick = curTick;

        //  toggle perspective camera mode
        if (GetKeyState( VK_F10 ) < 0)
        {
            static DWORD lastTime3 = 0;
            if (GetTickCount() - lastTime3 > 500)
            {
                g_bPerspCamera		= !g_bPerspCamera;
                //  force terrain visible set update
                ITerra->ClearPVS();
                FillGroundZBuffer();
                HandleWater();
                g_bDoBillboarding	= false;
                lastTime3 = GetTickCount();
            }
        }

		bool IsCameraLocked();
		if ( g_bPerspCamera && !IsCameraLocked() )
		{
			extern float fRotateCameraByAngle;  
			roll += fRotateCameraByAngle;
			fRotateCameraByAngle = 0;

            /*
			if (GetKeyState( VK_CONTROL ) < 0 && GetKeyState( VK_MENU ) < 0)
			{
				
					if (GetKeyState( '9' ) < 0) {yaw += yawStep; zoom += zoomStep*0.25f;}
					if (GetKeyState( '0' ) < 0) {yaw -= yawStep; zoom -= zoomStep*0.25f;}
					if (GetKeyState( '1' ) < 0) { zoom += zoomStep; }
					if (GetKeyState( '2' ) < 0) { zoom -= zoomStep; }
					if (GetKeyState( '3' ) < 0) roll += rollStep;
					if (GetKeyState( '4' ) < 0) roll -= rollStep;
					
					if (GetKeyState( '6' ) < 0) g_bDoBillboarding = !g_bDoBillboarding; 
					if (GetKeyState( '7' ) < 0) g_bDoSkew = !g_bDoSkew;
				
				
		    }
			if (GetKeyState( VK_PRIOR ) < 0){
				float y=GetYawByZoom(zoom);
				yaw+=(y-yaw)/4.0f;
                zoom += zoomStep;
			}
			if (GetKeyState( VK_NEXT  ) < 0){
				float y=GetYawByZoom(zoom);
				yaw+=(y-yaw)/4.0f;
                zoom -= zoomStep;
			}*/
            if (GetKeyState( VK_HOME  ) < 0) 
            //  restore camera position
            {
                zoom = roll = 0.0f;
                yaw = c_PI/6.0f;
            }
            clamp( yaw, minYaw, maxYaw );
            clamp( zoom, minZoom, maxZoom );

            if (zoom >= 0.0f) fov = g_InitFOV;
		}

		rotM.rotation( Vector3D::oZ, roll );

		Vector3D pos(	fMapX * c_WorldGridStepX + viewVol / 2,  
						fMapY * c_WorldGridStepY + RealLy * scale,     
						0.0f );

		Vector3D dir( 0.0f, -cos( yaw ), -sin( yaw ) );
		rotM.transformVec( dir );
		
        float GetCameraFactor();
		g_CameraDistance = viewVol*GetCameraFactor() + zoom;

		ICam->SetDirUp( dir, Vector3D::oZ );
		dir *= -g_CameraDistance;
		pos += dir;

       	float CamZn, CamZf;
		float ydist 	= RealLy*scale*c_Cos30*2.0f;
		CamZn			= g_CameraDistance - ydist - 4.0f * c_MaxMapHeight / c_Cos30;
		if (CamZn < 50.0f) CamZn = 50.0f;
        CamZf			= g_CameraDistance + ydist*2.0f; 
		
		if (IsPerspCameraMode())
		{
			CamZf += ydist * 10.0f;
		}

		const float c_MinZn = 10.0f;
		if (CamZn < c_MinZn) CamZn = c_MinZn;

		ICam->SetPos( pos );
		ICam->SetFOV( fov );
        ICam->SetProjection( viewVol, float( RealLx ) / float( RealLy ), CamZn, CamZf );

        Vector3D la = ICam->GetLookAt();
        int t = 0;
	}
	
	ICam->Render();
	
	Vector4D top( fMapX*c_WorldGridStepX, fMapY*c_WorldGridStepY, 0.0f, 1.0f );
	Vector4D bot( top.x, top.y + RealLy*2, 0.0f, 1.0f );

	ICam->WorldToScreenSpace( top );
	ICam->WorldToScreenSpace( bot );

	g_StartZ		= bot.z;
	g_EndZ			= top.z;
	g_ZRange		= g_EndZ - g_StartZ;
    g_ZNear         = ICam->GetZn();
    g_ZFar          = ICam->GetZf();
    g_WorldZRange   = g_ZFar - g_ZNear;

	g_CameraWorldTM = ICam->GetWorldMatrix();
	mapx=fMapX;
	mapy=fMapY;

    g_ScreenViewport	= IRS->GetViewPort();
    g_CameraViewProjM	= ICam->GetViewProjM();
    g_ScreenToWorldSpaceTM = ICam->ScreenToWorldSpace();
	g_CameraViewM		= ICam->GetWorldMatrix();	
	g_CameraViewM.inverse();
	
	ICam->GetFrustum( g_Frustum );
	int nV = g_Frustum.Intersection( Plane::xOy, g_FrustumXCorners );	

    g_bCameraChanged = ((g_pLastCamera != ICam) || 
                        !g_LastPos.isEqual( ICam->GetPos() ) ||
                        !g_LastDir.isEqual( ICam->GetDir() ));

    g_pLastCamera = ICam;
    g_LastPos = ICam->GetPos();
    g_LastDir = ICam->GetDir();

} // SetupCamera
void ResetCameraAngles(){
	zoom = roll = 0.0f;
	yaw = c_PI/6.0f;
}
bool CheckObjectVisibility(int x,int y,int z,int R)
{
	return g_Frustum.Overlap( Sphere( Vector3D( x, y, z ), R ) );
}

Matrix4D ScreenToWorldSpace()
{
    return g_ScreenToWorldSpaceTM;
} // ScreenToWorldSpace

Vector3D ScreenToWorldSpace( int mx, int my )
{
	if (!ITerra) return Vector3D::null;
	if (!ICam) return Vector3D::null;
	Ray3D ray;
	ICam->GetPickRay( mx, my, ray );
	bool CheckIfNewTerrain();
	if(!CheckIfNewTerrain())ray.Transform( c_UnskewTM );
    Vector3D xPt = Vector3D::null;
	ITerra->Pick( ray.getOrig(), ray.getDir(), xPt );
	return xPt;
} // ScreenToWorldSpace


bool IsPerspCameraMode()
{
	return g_bPerspCamera;
} // IsPerspCameraMode


_inl void ProjectionToScreenSpace( float& x, float& y ) 
{
    x = (x + 1.0f) * g_ScreenViewport.w * 0.5f;
    y = (1.0f - y) * g_ScreenViewport.h * 0.5f;

    x += g_ScreenViewport.x;
    y += g_ScreenViewport.y;
} // ProjectionToScreenSpace

void WorldToProjectionSpace( Vector4D& vec )
{
	vec.mul( g_CameraViewProjM );
    vec.normW();
}

void WorldToCameraSpace( Vector4D& vec )
{
	vec.mul( g_CameraViewM );
}

void WorldToScreenSpace( Vector4D& vec )
{
	WorldToProjectionSpace( vec );
    ProjectionToScreenSpace( vec.x, vec.y );
}

void WorldToScreenSpace( Vector3D& vec )
{
	Vector4D v( vec ); v.w = 1.0f;
	WorldToScreenSpace( v );
	vec = v;
}

void ProjectionToScreenSpace( Vector4D& vec )
{
	vec.x *= RealLx * 0.5f;
	vec.x += RealLx * 0.5f;
	vec.y *= -RealLy * 0.5f;
	vec.y += RealLx * 0.5f;
}

extern int MinMapX;
extern int MaxMapX;
extern int MinMapY;
extern int MaxMapY;


const Vector3D* GetCameraIntersectionCorners()
{
	return g_FrustumXCorners;
}

Rct g_rctMinimap;
CEXPORT int GetXOnMiniMap(int x,int y);
CEXPORT int GetYOnMiniMap(int x,int y);
void DrawMinimapFrustumProjection( const Rct& miniRct )
{
	/*
	g_rctMinimap = miniRct;
	Rct vp = GPS.GetClipArea();
	GPS.SetClipArea( miniRct.x, miniRct.y, miniRct.w, miniRct.h );
	
	const Vector3D* v = GetCameraIntersectionCorners();

	float W = (MaxMapX - MinMapX)*32;
	float H = (MaxMapY - MinMapY)*32;
	float minx = MinMapX*32;
	float miny = MinMapY*32;

	Vector3D mv[4];
	for (int i = 0; i < 4; i++)
	{
		mv[i].x = miniRct.x + miniRct.w*(v[i].x - minx)/W;
		mv[i].y = miniRct.y + miniRct.w*(v[i].y - miny)/H;
	}
	
	for (int i = 0; i < 4; i++)
	{
		int j = (i + 1)%4; 
		rsLine( mv[i].x, mv[i].y, mv[j].x, mv[j].y, 0.0f, 0xFFFFFFFF );
	}
	rsFlushLines2D();
	GPS.SetClipArea( vp.x, vp.y, vp.w, vp.h );
	*/
	g_rctMinimap = miniRct;
	Rct vp = GPS.GetClipArea();
	GPS.SetClipArea( miniRct.x, miniRct.y, miniRct.w, miniRct.h );

	const Vector3D* v = GetCameraIntersectionCorners();

	Vector3D mv[4];
	for (int i = 0; i < 4; i++)
	{
		mv[i].x = GetXOnMiniMap(v[i].x,v[i].y);
		mv[i].y = GetYOnMiniMap(v[i].x,v[i].y);
	}

	for (int i = 0; i < 4; i++)
	{
		int j = (i + 1)%4; 
		rsLine( mv[i].x, mv[i].y, mv[j].x, mv[j].y, 0.0f, 0xFFFFFFFF );
	}
	rsFlushLines2D();
	GPS.SetClipArea( vp.x, vp.y, vp.w, vp.h );
} // DrawMinimapFrustumProjection

void DrawMinimapRct( const Rct& rct )
{
	Rct vp = GPS.GetClipArea();
	GPS.SetClipArea( g_rctMinimap.x, g_rctMinimap.y, g_rctMinimap.w, g_rctMinimap.h );
	Vector3D v[4];
	v[0].set( rct.x, rct.y, 0.0f );
	v[1].set( rct.GetRight(), rct.y, 0.0f );
	v[2].set( rct.GetRight(), rct.GetBottom(), 0.0f );
	v[3].set( rct.x, rct.GetBottom(), 0.0f );

	float W = GetMaxMapX();
	float H = GetMaxMapY();

	for (int i = 0; i < 4; i++)
	{
		v[i].x = g_rctMinimap.x + g_rctMinimap.w*v[i].x/W;
		v[i].y = g_rctMinimap.y + g_rctMinimap.w*v[i].y/H;
	}

	for (int i = 0; i < 4; i++)
	{
		int j = (i + 1)%4; 
		rsLine( v[i].x, v[i].y, v[j].x, v[j].y, 0.0f, 0xFFFFFFFF );
	}
	rsFlushLines2D();
	GPS.SetClipArea( vp.x, vp.y, vp.w, vp.h );
} // DrawMinimapRct

bool g_bDrawKangaroo = false;
void SummonKangaroo()
{
	g_bDrawKangaroo = true;
}

void DrawKangaroo()
{
	if (g_bDrawKangaroo && GetKeyState( VK_ESCAPE ) < 0) g_bDrawKangaroo = false;
	if (g_bDrawKangaroo) IMM->Render();
	InputManager::instance().SetActive( g_bDrawKangaroo );
} // DrawKangaroo


BaseMesh    g_TerraPatchBM;
int         g_CurTerraPatchTex = -1;
int         g_CurTerraPatchSha = -1;
const float c_PatchGranularity = 32.0f;
const int   c_MaxTerraPatchV   = 1024;
const int   c_MaxPatchSegments = 32;
void DrawTerrainPatch( float worldX, float worldY, 
                       float width, float height, 
                       float rotation, 
                       const Rct& uv, float ux, float uy,
                       int texID, DWORD color, bool bAdditive )
{
    static int shAdd = IRS->GetShaderID( "terrain_patch_add" );
    static int shMul = IRS->GetShaderID( "terrain_patch_mul" );

    if (g_TerraPatchBM.getMaxVert() == 0)
    //  create terrain patch mesh
    {
        g_TerraPatchBM.create( c_MaxTerraPatchV, c_MaxTerraPatchV*6, vf2Tex, ptTriangleList );
    }
    int cTexID = texID;
    int cShaID = bAdditive ? shAdd : shMul;
    if (cTexID != g_TerraPatchBM.getTexture() || cShaID != g_TerraPatchBM.getShader())
    {
        FlushTerrainPatches();
        g_TerraPatchBM.setTexture( cTexID );
        g_TerraPatchBM.setShader ( cShaID );
    }

    int wSeg = tmin( tmax<int>( width/c_PatchGranularity, 1 ), c_MaxPatchSegments );
    int hSeg = tmin( tmax<int>( height/c_PatchGranularity, 1 ), c_MaxPatchSegments );
    int bV   = g_TerraPatchBM.getNVert(); 
    int bI   = g_TerraPatchBM.getNInd (); 
    int nV   = (wSeg + 1) * (hSeg + 1);
    int nI   = wSeg * hSeg * 6;
    if (bV + nV >= g_TerraPatchBM.getMaxVert() ||  
        bI + nI >= g_TerraPatchBM.getMaxInd()) 
    {
        FlushTerrainPatches();
        bI = bV = 0;
    }
    Vertex2t* v = ((Vertex2t*)g_TerraPatchBM.getVertexData()) + bV;
    WORD* idx = g_TerraPatchBM.getIndices() + bI;
    int cI = 0;
    int cV = bV;
    
    if (hSeg == 1 && wSeg == 1)
    //  simple quad case
    {
        idx[cI++] = cV;
        idx[cI++] = cV + 1;
        idx[cI++] = cV + 2;

        idx[cI++] = cV + 1;
        idx[cI++] = cV + 2;
        idx[cI++] = cV + 3;

        v[0].x          = -ux*width;
        v[0].y          = -uy*height;
        v[0].u          = uv.x;
        v[0].v          = uv.y;
        v[0].diffuse    = color;

        v[1].x          = ux*width;
        v[1].y          = -uy*height;
        v[1].u          = uv.x + uv.w;
        v[1].v          = uv.y;
        v[1].diffuse    = color;

        v[2].x          = -ux*width;
        v[2].y          = uy*height;
        v[2].u          = uv.x;
        v[2].v          = uv.y + uv.h;
        v[2].diffuse    = color;

        v[3].x          = ux*width;
        v[3].y          = uy*height;
        v[3].u          = uv.x + uv.w;
        v[3].v          = uv.y + uv.h;
        v[3].diffuse    = color;
    }
    else
    //  grid case
    {
        //  create indices
        for (int j = 0; j < hSeg; j++)
        {
            for (int i = 0; i < wSeg; i++)
            {
                if (i&1) 
                {
                    idx[cI++] = cV;
                    idx[cI++] = cV + 1;
                    idx[cI++] = cV + wSeg + 1; 

                    idx[cI++] = cV + wSeg + 1;
                    idx[cI++] = cV + 1;
                    idx[cI++] = cV + wSeg + 2; 
                }
                else
                {
                    idx[cI++] = cV;
                    idx[cI++] = cV + 1;
                    idx[cI++] = cV + wSeg + 2; 

                    idx[cI++] = cV;
                    idx[cI++] = cV + wSeg + 2;
                    idx[cI++] = cV + wSeg + 1; 
                }

                cV++;
            }
            cV++;
        }

        //  create vertices
        float wstep = width / float( wSeg );
        float hstep = height/float( hSeg );
        cV = 0;
        for (int j = 0; j <= hSeg; j++)
        {
            for (int i = 0; i <= wSeg; i++)
            {
                Vertex2t& vert = v[cV];
                vert.x = float( i )*wstep - ux*width;
                vert.y = float( j )*hstep - uy*height;
                vert.u = uv.x + uv.w*(vert.x/width + ux);
                vert.v = uv.y + uv.h*(vert.y/height + uy);
                vert.diffuse = color;
                cV++;
            }
        }
    }
    
    if (rotation > 0.0f)
    {
        //  create world transform matrix
        Matrix4D tm( Vector3D::one, Vector3D::oZ, DegToRad( rotation ), Vector3D( worldX, worldY, 0.0f ) );
        tm *= GetSkewTM();
        //  transform patch and fit to the ground
        for (int i = 0; i < nV; i++) 
        {
            v[i].z = 0.0f;
            tm.transformPt( *((Vector3D*)&v[i]) );
            v[i].z = GetHeight( v[i].x, v[i].y );
            *((Vector3D*)&v[i]) = SkewPt( v[i].x, v[i].y, v[i].z );
        }
    }
    else
    {
        for (int i = 0; i < nV; i++) 
        {
            v[i].x += worldX;
            v[i].y += worldY;
            v[i].z = GetHeight( v[i].x, v[i].y );
            *((Vector3D*)&v[i]) = SkewPt( v[i].x, v[i].y, v[i].z );
        }    
    }
    g_TerraPatchBM.setNVert( nV + bV );
    g_TerraPatchBM.setNInd ( nI + bI );
    g_TerraPatchBM.setNPri ( (nI + bI)/3 );
} // DrawTerrainPatch

void FlushTerrainPatches()
{
    IRS->Draw( g_TerraPatchBM );
    g_TerraPatchBM.setNVert( 0 );
    g_TerraPatchBM.setNPri ( 0 );
    g_TerraPatchBM.setNInd ( 0 );
} // FlushTerrainPatches

class TerraStub : public ITerrain
{
public:
    void		Render				()							{ }
    void		SetCallback			( GetHeightCallback	callb ) { }
    void		SetCallback			( SetHeightCallback	callb ) { }
    void		SetCallback			( TextureCallback	callb ) { }
    void		SetCallback			( GeometryCallback	callb ) { }
    void		SetCallback			( AABBCallback		callb ) { }
    void		SetCallback			( PVSCallback		callb ) { }
    void		SetCallback			( FactoryCallback   callb ) { }
    void		InvalidateTexture	( const Rct* rct = NULL )	{ }
    void		InvalidateGeometry	( const Rct* rct = NULL )	{ }
    void		InvalidateAABB		( const Rct* rct = NULL )	{ }
    void		EnableVertexLighting( bool val = true )			{ }
    void		ForceLOD			( int lod )					{ }
    void		SetExtents			( const Rct& ext )			{ }
    void		SetHeightmapPow		( int hpow )				{ }
    void		SetLODBias			( float bias )				{ }
    Vector3D	GetNormal			( float x, float y ) const	{ return Vector3D::oZ; }
    float		GetH				( float x, float y ) const	{ return 0.0f; }
    void		SetH				( int x, int y, float z )	{ }
    void		Show				( bool bShow = true )		{ }
    bool		IsShown				() const					{ return false; }
    void        DrawBorder          ()                          { }
    bool        SetBorderConfigFile ( const char* fname )       { return false; }
    void        Init                ()                          { }
    void	    SetDrawCulling		( bool bValue = true )      { }
    void	    SetDrawGeomCache    ( bool bValue = true )      { }
    void	    SetDrawTexCache     ( bool bValue = true )      { }
    void        ResetDrawQueue      ()                          { }
    void        ShowNormals         ( bool bShow = true )       { }
    Rct         GetExtents          () const                    { return Rct::null; }
    bool		Pick				( const Vector3D& orig, const Vector3D& dir, Vector3D& pt ) { return false; }
    bool		Pick				( int mX, int mY, Vector3D& pt ) { return false; }

    void        SetGeometryCacheSize( int nGeom ) {}

    BaseMesh*   AllocateGeometry    ()                          
    {
        if (!m_pChunk) return NULL;
        BaseMesh* bm = new BaseMesh();
        m_pChunk->m_Meshes.push_back( bm );
        return bm;
    }

    TerraStub() : m_pChunk(NULL), m_pDB(NULL) {}
    StaticTerrainChunk*             m_pChunk;
    StaticTerrainDB*                m_pDB;

    virtual void        ClearPVS    (){}

}; // class TerraStub
TerraStub g_TerraStub;

const int c_ChunkSideNodes = 8;
extern int VertInLine;
bool CreateTerrainDB( StaticTerrainDB& db )
{
    ITerrain* pOldTerra = ITerra;
    ITerra = &g_TerraStub;
    g_TerraStub.m_pDB = &db;
    int wNodes      = VertInLine - 1;
    int wChunks     = wNodes / c_ChunkSideNodes;
    int hChunks     = wChunks;
    float chunkW    = c_ChunkSideNodes*32.0f;
    float chunkH    = c_ChunkSideNodes*32.0f;
    db.SetExtents( wChunks, 0.0f, 0.0f, wNodes*32.0f, wNodes*32.0f );

    for (int j = 0; j < hChunks; j++)
    {
        for (int i = 0; i < wChunks; i++)
        {
            Rct rct( float( i*chunkW ), float( j*chunkH ), chunkW, chunkH );
            StaticTerrainChunk* pChunk = db.GetChunk( rct );
            if (!pChunk) continue;
            g_TerraStub.m_pChunk = pChunk;
            ITCreateGeometry( rct, 0 );
            int nMeshes = pChunk->m_Meshes.size();
            for (int t = 0; t < nMeshes; t++)
            {
                BaseMesh* bm = pChunk->m_Meshes[t];
                bm->setTexture( db.GetTextureID( IRS->GetTextureName( bm->getTexture() ) ) );
                bm->setTexture( db.GetTextureID( IRS->GetTextureName( bm->getTexture( 1 ) ) ), 1 );
                bm->setShader ( db.GetShaderID ( IRS->GetShaderName( bm->getShader() ) ) );
            }
        }
    }
    
    ITerra = pOldTerra;
    return true;
} // CreateTerrainDB

#pragma pack( pop ) 
 
