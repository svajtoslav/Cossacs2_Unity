/*****************************************************************************/
/*	File:	uiManipulator.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	18.06.2004
/*****************************************************************************/
#include "stdafx.h"
#include "kInput.h"
#include "uiManipulator.h"

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*  TranslateTool implementation
/*****************************************************************************/
TranslateTool::TranslateTool()
{
    m_DragMode          = dmNone;
    m_XColor            = 0xFFFF0000;
    m_YColor            = 0xFF00FF00;
    m_ZColor            = 0xFF0000FF;
    m_PColor            = 0xAA00FFFF;    
    m_SelColor          = 0xFFFFFF00;
    m_ArrowLen          = 0.15f;        
    m_MinSelDist        = 0.5f;  
    m_HeadLen           = 0.3f;         
    m_HeadR             = 0.06f;           
    m_SGizmoSide        = 20;      
    m_StartPos          = Vector3D::null;
    m_StartMX           = 0;
    m_StartMY           = 0;
    m_InitPos           = Vector3D::null;      
    m_CurPos            = Vector3D::null;
} // TranslateTool::TranslateTool

void TranslateTool::SetPosition( const Vector3D& pos )
{
    m_InitPos   = pos;
    m_CurPos    = pos;
    m_StartPos  = pos;
    m_DragMode  = dmNone;
} // TranslateTool::SetPosition

void TranslateTool::BindNode( TransformNode* pNode )
{
    if (!pNode) { m_pNode = NULL; return; }
    SetPosition( pNode->GetWorldTM().getTranslation() );
    m_pNode = pNode;
} // TranslateTool::BindNode

void TranslateTool::UnbindNode()
{
    m_pNode = NULL;
}

float TranslateTool::GetWorldFrame( Vector3D&x, Vector3D& y, Vector3D& z )
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return 0.0f;
    Vector4D vs( m_CurPos );
    pCam->WorldToProjectionSpace( vs ); vs.x += m_ArrowLen;
    pCam->ProjectionToWorldSpace( vs ); vs -= m_CurPos;
    float len = vs.norm();
    x = Vector3D( len, 0, 0 );
    y = Vector3D( 0, len, 0 );
    z = Vector3D( 0, 0, len );
    return len;
} // TranslateTool::GetWorldFrame

void TranslateTool::Render()
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return;

    //  calculate world-space length of the gizmo arrows 
    //  (to keep constant length in screen space)
    Vector3D dX, dY, dZ;
    GetWorldFrame( dX, dY, dZ );

    //  draw arrows
    rsEnableZ( false );
    IRS->ResetWorldMatrix();
    DWORD clrX = (m_DragMode == dmDragX) ? m_SelColor : m_XColor;
    DWORD clrY = (m_DragMode == dmDragY) ? m_SelColor : m_YColor;
    DWORD clrZ = (m_DragMode == dmDragZ) ? m_SelColor : m_ZColor;
    DWORD clrP = (m_DragMode == dmDragScreen) ? m_SelColor : m_PColor;
    DrawArrow( m_CurPos, dX, clrX, 1.0f, m_HeadLen, m_HeadR );
    DrawArrow( m_CurPos, dY, clrY, 1.0f, m_HeadLen, m_HeadR );
    DrawArrow( m_CurPos, dZ, clrZ, 1.0f, m_HeadLen, m_HeadR );

    //  draw screen space drag gizmo
    Vector4D spos( m_CurPos );
    pCam->WorldToScreenSpace( spos );
    float x = spos.x - m_SGizmoSide*0.5f;
    float y = spos.y - m_SGizmoSide*0.5f;
    x = ceilf( x ); y = ceilf( y );
    Rct rct( x, y, m_SGizmoSide, m_SGizmoSide );
    rsFrame( rct, 0, clrP );

    rsFlushLines2D();
    rsFlushLines3D();
    rsFlushPoly3D();
} // TranslateTool::Render

void TranslateTool::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "TranslateTool", this );
    pm.f( "DragMode",   m_DragMode, NULL, true );
    pm.f( "XColor",     m_XColor, "color"   );
    pm.f( "YColor",     m_YColor, "color"   );
    pm.f( "ZColor",     m_ZColor, "color"   );
    pm.f( "SColor",     m_PColor, "color"   );         
    pm.f( "ArrowLen",   m_ArrowLen          );
    pm.f( "MinSelDist", m_MinSelDist        );
    pm.f( "HeadLen",    m_HeadLen           );
    pm.f( "HeadR",      m_HeadR             );
    pm.f( "SGizmoSide", m_SGizmoSide        );
} // TranslateTool::Expose

bool TranslateTool::OnMouseLButtonDown( int mX, int mY )
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return false;

    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );

    Vector3D dX, dY, dZ;
    float len = GetWorldFrame( dX, dY, dZ );
    dX += m_CurPos;
    dY += m_CurPos;
    dZ += m_CurPos;

    float dx = ray.dist2ToPoint( dX );
    float dy = ray.dist2ToPoint( dY );
    float dz = ray.dist2ToPoint( dZ );
    float dp = ray.dist2ToPoint( m_CurPos );

    float dmin = tmin( dx, dy, dz, dp );
    float minSelDist = len*m_MinSelDist;
    minSelDist *= minSelDist;

    if (m_DragMode == dmNone) {m_StartMX = mX; m_StartMY = mY; m_StartPos = m_CurPos;}
    m_DragMode = dmNone;
    if (dx == dmin && dx < minSelDist) m_DragMode = dmDragX;
    if (dy == dmin && dy < minSelDist) m_DragMode = dmDragY;
    if (dz == dmin && dz < minSelDist) m_DragMode = dmDragZ;
    if (dp == dmin && dp < minSelDist) m_DragMode = dmDragScreen;
    return false;
} // TranslateTool::OnMouseLButtonDown

bool TranslateTool::OnMouseMove( int mX, int mY, DWORD keys )
{
    if ((keys & MK_LBUTTON) == 0) m_DragMode = dmNone;
    if (m_DragMode == dmNone) return false; 
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return false;

    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );
    Vector4D vs( m_StartPos ); pCam->WorldToScreenSpace( vs );
    vs.x += mX - m_StartMX;
    vs.y += mY - m_StartMY;
    pCam->ScreenToWorldSpace( vs );
    vs -= m_StartPos;
    vs.w = 0.0f;

    if (m_DragMode == dmDragX) 
    {
        vs.x = sign( vs.x ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.y = vs.z = 0; 
    }
    else if (m_DragMode == dmDragY) 
    {
        vs.y = sign( vs.y ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.x = vs.z = 0; 
    }
    else if (m_DragMode == dmDragZ) 
    {
        vs.z = sign( vs.z ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.x = vs.y = 0; 
    }

    m_CurPos.add( m_StartPos, vs );

    if (m_pNode) 
    {
        Matrix4D tm = m_pNode->GetWorldTM();
        tm.setTranslation( m_CurPos );
        m_pNode->SetWorldTM( tm );
    }
    return false;
} // TranslateTool::OnMouseMove

bool TranslateTool::OnMouseLButtonUp( int mX, int mY )
{
    m_DragMode = dmNone;
    return false;
} // TranslateTool::OnMouseLButtonUp

/*****************************************************************************/
/*  ScaleTool implementation
/*****************************************************************************/
ScaleTool::ScaleTool()
{
    m_DragMode          = dmNone;
    m_XColor            = 0xFFFF0000;
    m_YColor            = 0xFF00FF00;
    m_ZColor            = 0xFF0000FF;
    m_PColor            = 0xAA00FFFF;    
    m_SelColor          = 0xFFFFFF00;
    m_ArrowLen          = 0.1f;        
    m_MinSelDist        = 0.5f;  
    m_HeadLen           = 0.2f;         
    m_StartPos          = Vector3D::null;
    m_StartMX           = 0;
    m_StartMY           = 0;
    m_InitPos           = Vector3D::null;      
    m_CurPos            = Vector3D::null;
} // ScaleTool::ScaleTool

void ScaleTool::SetPosition( const Vector3D& pos )
{
    m_InitPos   = pos;
    m_CurPos    = pos;
    m_StartPos  = pos;
    m_DragMode  = dmNone;
} // ScaleTool::SetPosition

void ScaleTool::BindNode( TransformNode* pNode )
{
    if (!pNode) { m_pNode = NULL; return; }
    SetPosition( pNode->GetWorldTM().getTranslation() );
    m_pNode = pNode;
} // ScaleTool::BindNode

void ScaleTool::UnbindNode()
{
    m_pNode = NULL;
}

float ScaleTool::GetWorldFrame( Vector3D&x, Vector3D& y, Vector3D& z )
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return 0.0f;
    Vector4D vs( m_CurPos );
    pCam->WorldToProjectionSpace( vs ); vs.x += m_ArrowLen;
    pCam->ProjectionToWorldSpace( vs ); vs -= m_CurPos;
    float len = vs.norm();
    x = Vector3D( len, 0, 0 );
    y = Vector3D( 0, len, 0 );
    z = Vector3D( 0, 0, len );
    return len;
} // ScaleTool::GetWorldFrame

void ScaleTool::Render()
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return;

    //  calculate world-space length of the gizmo arrows  
    //  (to keep constant length in screen space)
    Vector3D dX, dY, dZ;
    float len = GetWorldFrame( dX, dY, dZ );

    //  draw arrows
    rsEnableZ( false );
    IRS->ResetWorldMatrix();
    DWORD clrX = (m_DragMode == dmDragX     ) ? m_SelColor : m_XColor;
    DWORD clrY = (m_DragMode == dmDragY     ) ? m_SelColor : m_YColor;
    DWORD clrZ = (m_DragMode == dmDragZ     ) ? m_SelColor : m_ZColor;
    DWORD clrP = (m_DragMode == dmDragScreen) ? m_SelColor : m_PColor;
    
    float hside = len*m_HeadLen*0.5f;

    rsLine( m_CurPos, dX, m_XColor );
    rsLine( m_CurPos, dY, m_YColor );
    rsLine( m_CurPos, dZ, m_ZColor );
   
    DrawAABB( AABoundBox( dX, hside ), clrX, 0 );
    DrawAABB( AABoundBox( dY, hside ), clrY, 0 );
    DrawAABB( AABoundBox( dZ, hside ), clrZ, 0 );
    DrawAABB( AABoundBox( m_CurPos, hside ), clrP, 0 );

    rsFlushLines2D();
    rsFlushLines3D();
    rsFlushPoly3D();

} // ScaleTool::Render

void ScaleTool::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "ScaleTool", this );
    pm.f( "DragMode",   m_DragMode, NULL, true );
    pm.f( "XColor",     m_XColor, "color"   );
    pm.f( "YColor",     m_YColor, "color"   );
    pm.f( "ZColor",     m_ZColor, "color"   );
    pm.f( "SColor",     m_PColor, "color"   );         
    pm.f( "ArrowLen",   m_ArrowLen          );
    pm.f( "MinSelDist", m_MinSelDist        );
    pm.f( "HeadLen",    m_HeadLen           );
} // ScaleTool::Expose

bool ScaleTool::OnMouseLButtonDown( int mX, int mY )
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return false;

    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );

    Vector3D dX, dY, dZ;
    float len = GetWorldFrame( dX, dY, dZ );
    dX += m_CurPos;
    dY += m_CurPos;
    dZ += m_CurPos;

    float dx = ray.dist2ToPoint( dX );
    float dy = ray.dist2ToPoint( dY );
    float dz = ray.dist2ToPoint( dZ );
    float dp = ray.dist2ToPoint( m_CurPos );

    float dmin = tmin( dx, dy, dz, dp );
    float minSelDist = len*m_MinSelDist;
    minSelDist *= minSelDist;

    if (m_DragMode == dmNone) {m_StartMX = mX; m_StartMY = mY; m_StartPos = m_CurPos;}
    m_DragMode = dmNone;
    if (dx == dmin && dx < minSelDist) m_DragMode = dmDragX;
    if (dy == dmin && dy < minSelDist) m_DragMode = dmDragY;
    if (dz == dmin && dz < minSelDist) m_DragMode = dmDragZ;
    if (dp == dmin && dp < minSelDist) m_DragMode = dmDragScreen;
    return false;
} // ScaleTool::OnMouseLButtonDown

bool ScaleTool::OnMouseMove( int mX, int mY, DWORD keys )
{
    if ((keys & MK_LBUTTON) == 0) m_DragMode = dmNone;
    if (m_DragMode == dmNone) return false; 
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return false;

    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );
    Vector4D vs( m_StartPos ); pCam->WorldToScreenSpace( vs );
    vs.x += mX - m_StartMX;
    vs.y += mY - m_StartMY;
    pCam->ScreenToWorldSpace( vs );
    vs -= m_StartPos;
    vs.w = 0.0f;

    if (m_DragMode == dmDragX) 
    {
        vs.x = sign( vs.x ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.y = vs.z = 0; 
    }
    else if (m_DragMode == dmDragY) 
    {
        vs.y = sign( vs.y ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.x = vs.z = 0; 
    }
    else if (m_DragMode == dmDragZ) 
    {
        vs.z = sign( vs.z ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.x = vs.y = 0; 
    }

    m_CurPos.add( m_StartPos, vs );

    if (m_pNode) 
    {
        Matrix4D tm = m_pNode->GetWorldTM();
        tm.setTranslation( m_CurPos );
        m_pNode->SetWorldTM( tm );
    }
    return false;
} // ScaleTool::OnMouseMove

bool ScaleTool::OnMouseLButtonUp( int mX, int mY )
{
    m_DragMode = dmNone;
    return false;
} // ScaleTool::OnMouseLButtonUp

/*****************************************************************************/
/*  RotateTool implementation
/*****************************************************************************/
RotateTool::RotateTool()
{
    m_DragMode          = dmNone;
    m_XColor            = 0xFFFF0000;
    m_YColor            = 0xFF00FF00;
    m_ZColor            = 0xFF0000FF;
    m_PColor            = 0xAA00FFFF;    
    m_SelColor          = 0xFFFFFF00;
    m_ArrowLen          = 0.1f;        
    m_MinSelDist        = 0.5f;  
    m_HeadLen           = 0.2f;         
    m_StartPos          = Vector3D::null;
    m_StartMX           = 0;
    m_StartMY           = 0;
    m_InitPos           = Vector3D::null;      
    m_CurPos            = Vector3D::null;
} // RotateTool::RotateTool

void RotateTool::SetPosition( const Vector3D& pos )
{
    m_InitPos   = pos;
    m_CurPos    = pos;
    m_StartPos  = pos;
    m_DragMode  = dmNone;
} // RotateTool::SetPosition

void RotateTool::BindNode( TransformNode* pNode )
{
    if (!pNode) { m_pNode = NULL; return; }
    SetPosition( pNode->GetWorldTM().getTranslation() );
    m_pNode = pNode;
} // RotateTool::BindNode

void RotateTool::UnbindNode()
{
    m_pNode = NULL;
}

float RotateTool::GetWorldFrame( Vector3D&x, Vector3D& y, Vector3D& z )
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return 0.0f;
    Vector4D vs( m_CurPos );
    pCam->WorldToProjectionSpace( vs ); vs.x += m_ArrowLen;
    pCam->ProjectionToWorldSpace( vs ); vs -= m_CurPos;
    float len = vs.norm();
    x = Vector3D( len, 0, 0 );
    y = Vector3D( 0, len, 0 );
    z = Vector3D( 0, 0, len );
    return len;
} // RotateTool::GetWorldFrame

void RotateTool::Render()
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return;

    //  calculate world-space length of the gizmo arrows  
    //  (to keep constant length in screen space)
    Vector3D dX, dY, dZ;
    float len = GetWorldFrame( dX, dY, dZ );

    //  draw arrows
    rsEnableZ( false );
    IRS->ResetWorldMatrix();
    DWORD clrX = (m_DragMode == dmDragX     ) ? m_SelColor : m_XColor;
    DWORD clrY = (m_DragMode == dmDragY     ) ? m_SelColor : m_YColor;
    DWORD clrZ = (m_DragMode == dmDragZ     ) ? m_SelColor : m_ZColor;
    DWORD clrP = (m_DragMode == dmDragScreen) ? m_SelColor : m_PColor;
    
    float hside = len*m_HeadLen*0.5f;

    rsLine( m_CurPos, dX, m_XColor );
    rsLine( m_CurPos, dY, m_YColor );
    rsLine( m_CurPos, dZ, m_ZColor );
   
    DrawAABB( AABoundBox( dX, hside ), clrX, 0 );
    DrawAABB( AABoundBox( dY, hside ), clrY, 0 );
    DrawAABB( AABoundBox( dZ, hside ), clrZ, 0 );
    DrawAABB( AABoundBox( m_CurPos, hside ), clrP, 0 );

    rsFlushLines2D();
    rsFlushLines3D();
    rsFlushPoly3D();

} // RotateTool::Render

void RotateTool::Expose( PropertyMap& pm )
{
    pm.start<Parent>( "RotateTool", this );
    pm.f( "DragMode",   m_DragMode, NULL, true );
    pm.f( "XColor",     m_XColor, "color"   );
    pm.f( "YColor",     m_YColor, "color"   );
    pm.f( "ZColor",     m_ZColor, "color"   );
    pm.f( "SColor",     m_PColor, "color"   );         
    pm.f( "ArrowLen",   m_ArrowLen          );
    pm.f( "MinSelDist", m_MinSelDist        );
    pm.f( "HeadLen",    m_HeadLen           );
} // RotateTool::Expose

bool RotateTool::OnMouseLButtonDown( int mX, int mY )
{
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return false;

    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );

    Vector3D dX, dY, dZ;
    float len = GetWorldFrame( dX, dY, dZ );
    dX += m_CurPos;
    dY += m_CurPos;
    dZ += m_CurPos;

    float dx = ray.dist2ToPoint( dX );
    float dy = ray.dist2ToPoint( dY );
    float dz = ray.dist2ToPoint( dZ );
    float dp = ray.dist2ToPoint( m_CurPos );

    float dmin = tmin( dx, dy, dz, dp );
    float minSelDist = len*m_MinSelDist;
    minSelDist *= minSelDist;

    if (m_DragMode == dmNone) {m_StartMX = mX; m_StartMY = mY; m_StartPos = m_CurPos;}
    m_DragMode = dmNone;
    if (dx == dmin && dx < minSelDist) m_DragMode = dmDragX;
    if (dy == dmin && dy < minSelDist) m_DragMode = dmDragY;
    if (dz == dmin && dz < minSelDist) m_DragMode = dmDragZ;
    if (dp == dmin && dp < minSelDist) m_DragMode = dmDragScreen;
    return false;
} // RotateTool::OnMouseLButtonDown

bool RotateTool::OnMouseMove( int mX, int mY, DWORD keys )
{
    if ((keys & MK_LBUTTON) == 0) m_DragMode = dmNone;
    if (m_DragMode == dmNone) return false; 
    ICamera* pCam = GetCamera();
    if (!pCam || !m_pNode) return false;

    Ray3D ray;
    pCam->GetPickRay( mX, mY, ray );
    Vector4D vs( m_StartPos ); pCam->WorldToScreenSpace( vs );
    vs.x += mX - m_StartMX;
    vs.y += mY - m_StartMY;
    pCam->ScreenToWorldSpace( vs );
    vs -= m_StartPos;
    vs.w = 0.0f;

    if (m_DragMode == dmDragX) 
    {
        vs.x = sign( vs.x ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.y = vs.z = 0; 
    }
    else if (m_DragMode == dmDragY) 
    {
        vs.y = sign( vs.y ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.x = vs.z = 0; 
    }
    else if (m_DragMode == dmDragZ) 
    {
        vs.z = sign( vs.z ) * tmax( fabs( vs.x ), fabs( vs.y ), fabs( vs.z ) );
        vs.x = vs.y = 0; 
    }

    m_CurPos.add( m_StartPos, vs );

    if (m_pNode) 
    {
        Matrix4D tm = m_pNode->GetWorldTM();
        tm.setTranslation( m_CurPos );
        m_pNode->SetWorldTM( tm );
    }
    return false;
} // RotateTool::OnMouseMove

bool RotateTool::OnMouseLButtonUp( int mX, int mY )
{
    m_DragMode = dmNone;
    return false;
} // RotateTool::OnMouseLButtonUp

END_NAMESPACE(sg)
