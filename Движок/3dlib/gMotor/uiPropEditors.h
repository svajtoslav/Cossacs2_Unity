/*****************************************************************************/
/*	File:	uiPropEditors.h
/*	Desc:	Property editors for the mostly used value types
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-13-2003
/*****************************************************************************/
#ifndef __UIPROPEDITORS_H__
#define __UIPROPEDITORS_H__

namespace sg{

/*****************************************************************************/
/*	Class:	PropertyEditorEx
/*	Desc:	Expandablle property editor
/*****************************************************************************/
class PropertyEditorEx : public PropertyEditor
{
public:
						PropertyEditorEx() : m_pButton(NULL) {}
	virtual void		Render();

	virtual bool		OnMouseLButtonDown( int mX, int mY );
	virtual bool		OnMouseLButtonUp( int mX, int mY );

	NODE(PropertyEditorEx, PropertyEditor, PEEX);

protected:
	Button*				m_pButton;

}; // class PropertyEditorEx

/*****************************************************************************/
/*	Class:	StringEditor
/*	Desc:	String value property editor
/*****************************************************************************/
class StringEditor : public PropertyEditor
{
	EditBox*			m_pEdit;
	int					m_CursorPos;
public:
						StringEditor() : m_pEdit( NULL ), m_CursorPos(0){}
	virtual void		Render();
	
	virtual bool		OnMouseLButtonDown( int mX, int mY );
	virtual bool		OnKeyDown( DWORD keyCode, DWORD flags );
	virtual bool		OnChar( DWORD charCode, DWORD flags );


	virtual void		OnForceEndEdit();

	NODE(StringEditor,PropertyEditor,STPE);
}; // class StringEditor

/*****************************************************************************/
/*	Class:	IntegerEditor
/*	Desc:	Integer value property editor
/*****************************************************************************/
class IntegerEditor : public StringEditor
{
public:	
	virtual bool		OnChar( DWORD charCode, DWORD flags );
	virtual bool		OnKeyDown( DWORD keyCode, DWORD flags );

	NODE(IntegerEditor,StringEditor,NVPE);

protected:
	void				Increase();
	void				Decrease();
}; // class IntegerEditor

/*****************************************************************************/
/*	Class:	FloatEditor
/*	Desc:	float value property editor
/*****************************************************************************/
class FloatEditor : public StringEditor
{
public:	
	virtual bool		OnChar( DWORD charCode, DWORD flags );
	virtual bool		OnKeyDown( DWORD keyCode, DWORD flags );

	NODE(FloatEditor,StringEditor,FVPE);

protected:
	void				Increase();
	void				Decrease();
	float				GetChangeRatio() const;
}; // class FloatEditor

/*****************************************************************************/
/*	Class:	BoolEditor
/*	Desc:	String value property editor
/*****************************************************************************/
class BoolEditor : public PropertyEditor
{
public:
	void		Render();
	bool 		OnMouseLButtonDown( int mX, int mY );
		

	NODE(BoolEditor,PropertyEditor,BOPE);
}; // class BoolEditor

/*****************************************************************************/
/*	Class:	FilePathEditor
/*	Desc:	File path value property editor
/*****************************************************************************/
class FilePathEditor : public PropertyEditorEx
{
	char				m_Root[_MAX_PATH];
public:
	FilePathEditor() { m_Root[0] = 0; }
	FilePathEditor( const char* root ) { strcpy( m_Root, root ); }
	virtual bool		OnMouseLButtonUp( int mX, int mY );

	NODE(FilePathEditor,PropertyEditor,FPPE);
}; // class FilePathEditor

class FloatTrackEdit;
/*****************************************************************************/
/*	Class:	FloatCurveEditor
/*	Desc:	Float value animation track property editor
/*****************************************************************************/
class FloatCurveEditor : public PropertyEditorEx
{
	FloatTrackEdit*		m_pTrackEdit;

public:
						FloatCurveEditor();
	virtual bool		OnMouseLButtonUp( int mX, int mY );
	virtual bool		OnKeyDown		( DWORD keyCode, DWORD flags );
	virtual void		OnForceEndEdit	();

	virtual void		Render			();

	NODE(FloatCurveEditor,PropertyEditorEx,FLAC);
}; // class FloatCurveEditor

class QuatTrackEdit;
/*****************************************************************************/
/*	Class:	QuatCurveEditor
/*	Desc:	Quat value animation track property editor
/*****************************************************************************/
class QuatCurveEditor : public PropertyEditorEx
{
	QuatTrackEdit*		m_pTrackEdit;

public:
						QuatCurveEditor	();
	virtual bool		OnMouseLButtonUp( int mX, int mY );
	virtual bool		OnKeyDown		( DWORD keyCode, DWORD flags );
	virtual void		OnForceEndEdit	();

	virtual void		Render			();

	NODE(QuatCurveEditor,PropertyEditorEx,QUAC);
}; // class QuatCurveEditor

/*****************************************************************************/
/*	Class:	ColorCurveEditor
/*	Desc:	Quat value animation track property editor
/*****************************************************************************/
class ColorCurveEditor : public PropertyEditorEx
{
	ColorTrackEdit*		m_pTrackEdit;

public:
						ColorCurveEditor();
	virtual bool		OnMouseLButtonUp( int mX, int mY );
	virtual bool		OnKeyDown		( DWORD keyCode, DWORD flags );
	virtual void		OnForceEndEdit	();

	virtual void		Render			();

	NODE(ColorCurveEditor,PropertyEditorEx,COAC);
}; // class ColorCurveEditor

/*****************************************************************************/
/*	Class:	ColorRampProperty
/*	Desc:	
/*****************************************************************************/
class ColorRampProperty : public PropertyEditor
{
	ColorRampEdit		m_Ramp;

public:
	virtual void		Render			();

	NODE(ColorRampProperty,PropertyEditor,CORP);
}; // class ColorRampProperty

/*****************************************************************************/
/*	Class:	AlphaRampProperty
/*	Desc:	
/*****************************************************************************/
class AlphaRampProperty : public PropertyEditor
{
	AlphaRampEdit		m_Ramp;

public:
	virtual void		Render			();

	NODE(AlphaRampProperty,PropertyEditor,ALRP);
}; // class AlphaRampProperty

/*****************************************************************************/
/*	Class:	MethodEditor
/*	Desc:	Method execution button, which stands for method property editor
/*****************************************************************************/
class MethodEditor : public PropertyEditor
{
	Button*				m_pButton;
public:
						MethodEditor	();
	virtual void		Render			();
	bool				OnMouseLButtonUp( int mX, int mY );

	NODE(MethodEditor,PropertyEditor,MEPE);
}; // class MethodEditor

/*****************************************************************************/
/*	Class:	ColorPicker
/*	Desc:	Editor for the color/alpha
/*****************************************************************************/
class ColorPicker : public Dialog
{
	DeviceStateSet*		m_pHexShader;
public:
						ColorPicker();
	virtual void		Render();

	NODE(ColorPicker, Dialog, COLP);
}; // class ColorPicker

/*****************************************************************************/
/*	Class:	ColorSelector
/*	Desc:	Editor for the color/alpha
/*****************************************************************************/
class ColorSelector : public PropertyEditor
{
public:
						ColorSelector() : m_pColorPicker( NULL ) {}
	virtual void		Render();
	virtual bool 		OnMouseLButtonDown( int mX, int mY );

	virtual bool		OnChar( DWORD charCode, DWORD flags );
	virtual bool		OnKeyDown( DWORD keyCode, DWORD flags );

	NODE(ColorSelector,PropertyEditor,CSPE);

private:
	void				Increase();
	void				Decrease();

	ColorPicker*		m_pColorPicker;

}; // class ColorSelector

/*****************************************************************************/
/*	Class:	TextEditor
/*	Desc:	Editor for the text (script whatever)
/*****************************************************************************/
class TextEditor : public StringEditor
{

public:
	
	NODE(TextEditor,StringEditor,TEPE);
}; // class TextEditor

/*****************************************************************************/
/*	Class:	TextureView
/*	Desc:	Editor for the texture (viewer)
/*****************************************************************************/
class TextureView : public Dialog
{
	int					m_TexID;
	TextureDescr		m_TD;

public:
	virtual void		Render();
	void				SetTexID( int texID );

	NODE(TextureView,Dialog,TXVI);
}; // class TextureView

/*****************************************************************************/
/*	Class:	TextureEditor
/*	Desc:	Represents texture pixels in the object inspector
/*****************************************************************************/
class TextureEditor : public PropertyEditorEx
{
	TextureView*			m_pTextureView;

public:
					TextureEditor() : m_pTextureView( NULL ) {}
	bool			OnMouseLButtonDown( int mX, int mY );
	bool			OnKeyDown( DWORD keyCode, DWORD flags );


	NODE(TextureEditor,PropertyEditor,TXPE);
}; // class TextureEditor

/*****************************************************************************/
/*	Class:	EnumEditor
/*	Desc:	Editor for the enumeration
/*****************************************************************************/
class EnumEditor : public PropertyEditor
{
public:

	NODE(EnumEditor,PropertyEditor,ENPE);
}; // class EnumEditor

/*****************************************************************************/
/*	Class:	DirectionEditor
/*	Desc:	Editor for the direction vector
/*****************************************************************************/
class DirectionEditor : public PropertyEditorEx
{
public:

	NODE(DirectionEditor,PropertyEditorEx,DIRE);
}; // class DirectionEditor

}; // namespace sg

#endif // __UIPROPEDITORS_H__