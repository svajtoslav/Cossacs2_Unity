/*****************************************************************************/
/*	File:	uiWidget.h
/*	Desc:	Brand-new ui
/*	Author:	Ruslan Shestopalyuk
/*	Date:	08-20-2003
/*****************************************************************************/
#ifndef __UIWIDGET_H__
#define __UIWIDGET_H__

namespace sg{
/*****************************************************************************/
/*	Class:	Widget
/*	Desc:	General control class
/*	Rmrk:	All size values are in "1024x768" units, which may be scaled
/*				with global scaling value
/*****************************************************************************/	
class Widget : public Node
{
protected:

	/*****************************************************************************/	
	/*	Enum:	WidgetFlags
	/*****************************************************************************/	
	enum WidgetFlags
	{
		wfResolutionScaling	= 0x01,	//  whether to rescale widget when resolution changes
		wfInvisible			= 0x02,
		wfDisabled			= 0x03,
	}; // enum WidgetFlags

	Rct						m_Extents;			//  extents of control, local to parent
	Vector3D				m_Pivot;			//  pivot point, local to parent
	DWORD					m_Flags;

	Widget*					m_pParent;			//  parent widget
	std::vector<Widget*>	m_ChildControl;		//  list of child controls

	static float			s_ResolutionScale;	//  different from 1 when screen resolution
	//  is not 1024x768
public:

	virtual void			Render			();
	virtual void			Expose			( PropertyMap& pm );

	virtual void			Serialize		( OutStream& os ) const;
	virtual void			Unserialize		( InStream& is );

	NODE(Widget,Node,WIDG);
}; // class Widget

/*****************************************************************************/
/*	Class:	Window
/*	Desc:	Window control
/*****************************************************************************/	
class Window : public Widget
{
public:
	NODE(Window,Widget,WICR);
}; // class Window

/*****************************************************************************/
/*	Class:	ColorGradient
/*	Desc:	Keyframed color gradient control
/*****************************************************************************/	
class ColorGradient : public Widget
{
public:
	NODE(ColorGradient,Widget,CLGR);
}; // class ColorGradient

/*****************************************************************************/
/*	Class:	PickColor
/*	Desc:	Color picker control
/*****************************************************************************/	
class PickColor : public Widget
{
public:
	NODE(PickColor,Widget,PCLR);
}; // class PickColor

/*****************************************************************************/
/*	Class:	PushButton
/*	Desc:	General push button
/*****************************************************************************/
class PushButton : public Widget
{
public:
	void				Render(){}
	void				SetText( const char* txt ) { m_Text = txt; }

	NODE(PushButton,Widget,PBTN);

private:
	std::string			m_Text;
}; // class PushButton 

/*****************************************************************************/
/*	Class:	ChBox
/*	Desc:	Checkbox
/*****************************************************************************/
class ChBox : public Widget
{
public:
	void				Render(){}

	NODE(ChBox,Widget,BNCH);

private:
}; // class ChBox 

/*****************************************************************************/
/*	Class:	Label
/*	Desc:	Piece of text hangin' somewhere
/*****************************************************************************/
class Label : public Widget
{
	std::string		m_Text;

public:
	Label(){}
	NODE(Label,Widget,LABL);
}; // class Label

/*****************************************************************************/
/*	Class:	Slider
/*	Desc:	Changes position [0..1]
/*****************************************************************************/
class Slider : public Widget
{
	float			m_Position;

public:
	Slider(){}

	NODE(Slider,Widget,SLID);
}; // class Slider

/*****************************************************************************/
/*	Class:	Progress
/*	Desc:	Progress bar
/*****************************************************************************/
class Progress : public Slider
{
public:
	NODE(Progress,Widget,PRGB);
}; // class Progress

/*****************************************************************************/
/*	Class:	ScrollBox
/*	Desc:	Something scrollable inside
/*****************************************************************************/
class ScrollBox : public Widget
{
public:
	ScrollBox(){}
	virtual			~ScrollBox(){}

	NODE(ScrollBox,Widget,SCRB);
}; // class Scrollbox

/*****************************************************************************/
/*	Class:	MainWindow
/*	Desc:	Father Of All Windows
/*****************************************************************************/	
class MainWindow : public Window, public InputDispatcher
{
public:

	NODE(MainWindow,Window,MAWI);
}; // class MainWindow

}; // namespace sg

#endif // __UIWIDGET_H__