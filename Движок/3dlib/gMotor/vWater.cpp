/*****************************************************************************/
/*	File:	vWater.cpp
/*	Desc:	Water interface implementation
/*	Author:	Ruslan Shestopalyuk
/*****************************************************************************/
#include "stdafx.h"
#include "IWater.h"
#include "ITerrain.h"
#include "kHash.hpp"

//  global water interface pointer
IWaterscape*        IWater;

/*****************************************************************************/
/*  Class:  CoastNode
/*  Desc:   Describes node in coastal chain
/*****************************************************************************/
class CoastNode
{
public:
    Vector3D        m_Pos;      //  node position
    Vector3D        m_Dir;      //  normal direction
    
    int             m_Prev;     //  previous node in chain  
    int             m_Next;     //  next node in chain

    CoastNode       ( const Vector3D& p ) : m_Prev(-1), m_Next(-1) { m_Pos = p; }
    CoastNode       () : m_Prev(-1), m_Next(-1) {}

};  // struct CoastNode

const int c_MaxCoastNodesInCell = 64;
/*****************************************************************************/
/*  Class:  CoastCell
/*  Desc:   Map cell describing coast line
/*****************************************************************************/
struct CoastCell
{
    int             cX, cY;
    int             m_NNodes;
    int             m_Nodes[c_MaxCoastNodesInCell];
    
    CoastCell( int cx, int cy ) : cX(cx), cY(cy), m_NNodes(0){}
    CoastCell(){}

    unsigned int hash() const
    {
        DWORD h = cX + 541*cY;
        h = (h << 13) ^ h;
        h += 15731;
        return h;
    }

    bool equal( const CoastCell& el )
    {
        return (cX == el.cX && cY == el.cY);
    }

    void copy( const CoastCell& el )
    {
        cX = el.cX;
        cY = el.cY;
        m_NNodes = el.m_NNodes;
        for (int i = 0; i < m_NNodes; i++) m_Nodes[i] = el.m_Nodes[i];
    }
};  // struct CoastCell

typedef Hash<CoastCell>     CoastCellHash;
/*****************************************************************************/
/*  Class:  Waterscape
/*  Desc:   Implementation of the water interface
/*****************************************************************************/
class Waterscape : public IWaterscape
{
    CoastCellHash               m_CoastHash;        //  hash of the coastline data

    float                       m_CellW;            //  width of the coastline cell
    float                       m_CellH;            //  height of the coastline cell

    bool                        m_bDrawDebugInfo;   
    std::vector<int>            m_DrawQ;            //  indices of currently visible cells

public:
                                Waterscape      ();
    virtual void                Render          ();
    virtual void                SetCellSide     ( float w, float h );

protected:

    void                        InvalidateCoast     ( const Rct* pArea = NULL );
    bool                        UpdateCoastCell     ( int cX, int cY );
    void                        DoVisibilityCulling ();
    
}; // class Waterscape
Waterscape      g_Water;

/*****************************************************************************/
/*  Waterscape  implementation
/*****************************************************************************/
Waterscape::Waterscape()
{
    m_bDrawDebugInfo    = true;
    m_CellW             = 512;
    m_CellH             = 512;
    IWater              = this;
} // Waterscape::Waterscape

void Waterscape::SetCellSide( float w, float h )
{
    m_CellW = w;
    m_CellH = h;
} // Waterscape::SetCoastCellSide

void Waterscape::Render()
{
    if (m_bDrawDebugInfo)
    {
        
    }
} // Waterscape::Render

void Waterscape::InvalidateCoast( const Rct* pArea )
{
    Rct ext     = ITerra->GetExtents();
    int nWCells = ext.w/m_CellW;
    int nHCells = ext.h/m_CellH;
    int qBegX, qBegY, qEndX, qEndY;

    if (pArea == NULL)
    {
        qBegX = 0;
        qBegY = 0;  
        qEndX = nWCells;
        qEndY = nHCells; 
    }
    else
    {
        qBegX = (pArea->x - ext.x)/m_CellW - 1;
        qBegY = (pArea->y - ext.y)/m_CellH - 1;  
        qEndX = (pArea->GetRight()  - ext.x)/m_CellW + 1;
        qEndY = (pArea->GetBottom() - ext.y)/m_CellH + 1; 
    }

    for (int j = qBegY; j < qEndY; j++)
    {
        for (int i = qBegX; i < qEndX; i++)
        {
            UpdateCoastCell( i, j );
        }
    }	
} // Waterscape::InvalidateCoast

const int c_NSamples = 8;
bool Waterscape::UpdateCoastCell( int cX, int cY )
{
    CoastCell cell( cX, cY );
    

    
    if (cell.m_NNodes == 0) return false;
    //  update in the hash
    int idx = m_CoastHash.add( cell );
    m_CoastHash.elem( idx ) = cell;
    return true;    
} // Waterscape::UpdateCoastCell

void Waterscape::DoVisibilityCulling()
{
    /*
    m_DrawQ.clear();
    
    BaseCamera* pCam = GetCamera();
    if (!pCam) return;
    
    Frustum cullFrustum = GetCameraFrustum();
    Vector3D corners[8];
    int nV = cullFrustum.Intersection( Plane::xOy, corners );	
    
    Vector3D v[12]; 
    cullFrustum.Intersection( Plane::xOy, v );
    float xMin =  FLT_MAX;
    float yMin =  FLT_MAX;
    float xMax = -FLT_MAX;
    float yMax = -FLT_MAX;
    for (int i = 0; i < nV; i++)
    {
        if (v[i].x < xMin) xMin = v[i].x;
        if (v[i].y < yMin) yMin = v[i].y;
        if (v[i].x > xMax) xMax = v[i].x;
        if (v[i].y > yMax) yMax = v[i].y;
    }

    Rct ext = ITerra->GetExtents();
    xMin = tmax( ext.x, xMin );
    yMin = tmax( ext.y, yMin );
    xMax = tmin( ext.GetRight(), xMax );
    yMax = tmin( ext.GetBottom(), yMax );
    
    DWORD frame = IRS->GetCurFrame();
    
    int qBegX = (xMin - m_Extents.x)/m_CellW;
    int qBegY = (yMin - m_Extents.y)/m_CellH;  
    int qEndX = (xMax - m_Extents.x)/m_CellW;
    int qEndY = (yMax - m_Extents.y)/m_CellH;  
    for (int j = qBegY; j < qEndY; j++)
    {
        for (int i = qBegX; i < qEndX; i++)
        {
            int qIdx = i + j*sideCells;
            if (qIdx < 0 || qIdx >= ql.nQuads) continue;
            TerrainQuad* pQuad = &m_Quads[ql.firstQuad + qIdx];
            pQuad->SetAlreadyDrawn( true );
            if (!visFrustum.Overlap( pQuad->GetQuadAABB() )) continue; 
            pQuad->SetLastFrame( frame );
            pQuad->SetAlreadyDrawn( false );
            m_QDrawn.push_back( pQuad );
        }
    }*/
} // Waterscape::DoVisibilityCulling


