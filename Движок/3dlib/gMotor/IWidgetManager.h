/*****************************************************************/
/*  File:   IWidgetManager.h
/*  Desc:   Interface for managing ui widgets
/*  Date:   Apr 2004											 
/*****************************************************************/
#ifndef __IWIDGETMANAGER_H__
#define __IWIDGETMANAGER_H__

/*****************************************************************/
/*  Class:	IWidgetManager
/*  Desc:	Interface for managing ui system
/*****************************************************************/
class IWidgetManager 
{
public:
	//  on-drop event
	virtual void	OnDrop( int mX, int mY, DWORD ctx, DWORD obj ) = 0;

	//  system input messages
	virtual void 	OnMouseWheel		( int delta ) = 0;					
	virtual void 	OnMouseMove			( int mX, int mY, DWORD keys ) = 0;	
	virtual void 	OnMouseLButtonDown	( int mX, int mY ) = 0;				
	virtual void 	OnMouseMButtonDown	( int mX, int mY ) = 0;				
	virtual void 	OnMouseRButtonDown	( int mX, int mY ) = 0;				
	virtual void 	OnMouseLButtonUp	( int mX, int mY ) = 0;				
	virtual void 	OnMouseMButtonUp	( int mX, int mY ) = 0;				
	virtual void 	OnMouseRButtonUp	( int mX, int mY ) = 0;				
	virtual void 	OnMouseLButtonDblclk( int mX, int mY ) = 0;				
	virtual void 	OnMouseRButtonDblclk( int mX, int mY ) = 0;				
	virtual void 	OnMouseMButtonDblclk( int mX, int mY ) = 0;				
	virtual void	OnKeyDown			( DWORD keyCode, DWORD flags ) = 0;	
	virtual void	OnChar				( DWORD charCode, DWORD flags ) = 0;	
	virtual void	OnKeyUp				( DWORD keyCode, DWORD flags ) = 0;	

	//  loads font description with given name, or returns id of already loaded font
	virtual int		GetFontID			( const char* name )													= 0;
	//  destroys font
	virtual void	DestroyFont			( int fontID )															= 0;
	//  procedurally creates font 
	virtual int		CreateFont			( const char* name, int height, DWORD charset = DEFAULT_CHARSET, 
											bool bBold = false, bool bItalic = false )							= 0;
	//  creates font from texture, assuming that all characters are the same width and height
	virtual int		CreateUniformFont	( const char* texName, int charW, int charH )							= 0;
	//  returns width of the string for a given font
	virtual int		GetStringWidth		( int fontID, const char* str, int spacing = 1 )						= 0;
	//  returns width of the given character in the font
	virtual int		GetCharWidth		( int fontID, BYTE ch ) = 0;
	//  returns height of the given character in the font
	virtual int		GetCharHeight		( int fontID, BYTE ch ) = 0;
	
	//  draws a string of the given font (drawing is actually batched)
	virtual bool	DrawString			( int fontID, const char* str, const Vector3D& pos, DWORD color = 0xFFFFFFFF, int spacing = 1 ) = 0;
	virtual bool	DrawStringW			( int fontID, const char* str, const Vector3D& pos, DWORD color = 0xFFFFFFFF, int spacing = 1 ) = 0;
	//  draws single character by its code
	virtual bool	DrawChar			( int fontID, const Vector3D& pos, BYTE ch, DWORD color = 0xFFFFFFFF )	= 0;
	//  draws single character by its texture coordinates
	virtual bool	DrawChar			( int fontID, const Vector3D& pos, const Rct& uv, DWORD color = 0xFFFFFFFF ) = 0;
	//  draws single character by its texture coordinates and screen extents
	virtual bool	DrawChar			( int fontID, const Vector3D& pos, const Rct& uv, 
											float w, float h, DWORD color = 0xFFFFFFFF ) = 0;

	//  flushes font batch, if fontID == -1, all fonts' batches are flushed 
	virtual void	FlushText			( int fontID = -1 )														= 0;

}; // IWidgetManager

extern IWidgetManager* IWM;

#endif // __IWIDGETMANAGER_H__