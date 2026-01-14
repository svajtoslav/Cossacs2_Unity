/*****************************************************************************/
/*	File:	edBodyMover.h
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	06-13-2003
/*****************************************************************************/
#ifndef __EDBODYMOVER_H__
#define __EDBODYMOVER_H__

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	Class:	BodyMover
/*	Desc:	Controller for setting position & orientation of the object in 3D
/*****************************************************************************/
class BodyMover : public Controller, public InputDispatcher
{
	bool			bDrawHeightMarker;
	bool			bRiseRotateMode;
	bool			bMoveXYMode;
	bool			bChangedPosition;
	bool			bScaleMode;

	float			markerBase;

	Line3D			pickRay;

	BaseCamera*		pCamera;

public:
	BodyMover();
	virtual			~BodyMover();

	//  input handling
	virtual bool 	OnMouseWheel		( int delta );					
	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );	
	virtual bool 	OnMouseLButtonDown	( int mX, int mY );	
	virtual bool 	OnMouseRButtonDown	( int mX, int mY );	
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY );				
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags );	

	virtual void	OnDraw				();

	void			SetCamera			( BaseCamera* _pCamera ){pCamera = _pCamera;}

	void			SetChangedPosition( bool changed = true );

	NODE(BodyMover,Controller,BODM);

protected:
	void			DrawHeightMarker( const Matrix4D& tr );
	void			OnMoveNode( int mX, int mY, const Line3D* ray = NULL );

}; // class BodyMover

/*****************************************************************************/
/*	Class:	HudMover
/*	Desc:	Controller for setting positions of screen-space objects
/*****************************************************************************/
class HudMover : public Controller, public InputDispatcher
{
	bool			bDragging;
	bool			bScaling;

public:
					HudMover();


	virtual bool 	OnMouseWheel		( int delta );					
	virtual bool 	OnMouseMove			( int mX, int mY, DWORD keys );	
	virtual bool 	OnMouseLButtonDown	( int mX, int mY );	
	virtual bool 	OnMouseLButtonUp	( int mX, int mY );	
	virtual bool 	OnMouseRButtonDown	( int mX, int mY );	
	virtual bool 	OnMouseLButtonDblclk( int mX, int mY );				
	virtual bool	OnKeyDown			( DWORD keyCode, DWORD flags );	

	virtual void	OnDraw				();

	NODE(HudMover,Controller,HUDM);
}; // class HudMover

END_NAMESPACE(sg)
#endif // __EDBODYMOVER_H__