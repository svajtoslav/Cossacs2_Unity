/*****************************************************************
/*  File:   ColorValue.cpp                                           
/*  Desc:                                                        
/*  Author: Silver, Copyright (C) GSC Game World                  
/*  Date:   January 2002                                          
/*****************************************************************/
#include "stdafx.h"
#include "kColorValue.h"

#ifndef _INLINES
#include "kColorValue.inl"
#endif // !_INLINES

/*****************************************************************
/*	ColorValue implementation
/*****************************************************************/
ColorValue ColorValue::White	= ColorValue( 0xFFFFFFFF );
ColorValue ColorValue::Black	= ColorValue( 0xFF000000 );
ColorValue ColorValue::Red		= ColorValue( 0xFFFF0000 );
ColorValue ColorValue::Green	= ColorValue( 0xFF00FF00 );
ColorValue ColorValue::Blue		= ColorValue( 0xFF0000FF );
ColorValue ColorValue::Yellow	= ColorValue( 0xFFFFFF00 );

void ColorValue::Dump()
{
	Log.Message( "a:%f r:%f g:%f b:%f", a, r, g, b );
}

/*****************************************************************
/*	ColorRamp implementation
/*****************************************************************/
int ColorRamp::AddKey( float t, DWORD val )
{
	ColorValue c = val;
	return AddKey( t, c.a, c.r, c.g, c.b );
}

int ColorRamp::AddKey( float t, float a, float r, float g, float b )
{
	if (m_NKeys == c_MaxRampSize) return -1;
	ColorValue val( a, r, g, b );

	if (t < 0.0f || t > 1.0f) return -1;
	if (t == 0.0f) { m_Key[0] = val; return 0; }
	if (t == 1.0f) { m_Key[m_NKeys - 1] = val; return m_NKeys - 1; }

	int lastKey = m_NKeys - 1;
	for (int i = 0; i < lastKey; i++)
	{
		if (m_Time[i] == t) { m_Key[i] = val; }
		if (m_Time[i] <= t && m_Time[i + 1] > t) 
		{
			for (int j = m_NKeys - 1; j > i; j--)
			{
				m_Time[j + 1] = m_Time[j];
				m_Key [j + 1] = m_Key [j];
			}
			m_Time[i + 1] = t;
			m_Key [i + 1] = val;
			m_NKeys++;
			return i + 1;
		}
	}
	return -1;
} // ColorRamp::AddKey

int ColorRamp::AddKey( float t, float r, float g, float b )
{
	return AddKey( t, 1.0f, r, g, b );
}

void ColorRamp::Serialize( OutStream& os ) const
{
	os << m_NKeys;
	for (int i = 0; i < m_NKeys; i++)
	{
		os << m_Time[i];
		DWORD col = m_Key[i];
		os << col;
	}
} // ColorRamp::Serialize

void ColorRamp::Unserialize( InStream& is )
{
	is >> m_NKeys;
	for (int i = 0; i < m_NKeys; i++)
	{
		is >> m_Time[i];
		DWORD col;
		is >> col;
		m_Key[i] = col;
	}
} // ColorRamp::Unserialize

/*****************************************************************
/*	AlphaRamp implementation
/*****************************************************************/
int AlphaRamp::AddKey( float t, float val )
{
	if (m_NKeys == c_MaxRampSize) return -1;
	if (t < 0.0f || t > 1.0f) return -1;
	if (t == 0.0f) { m_Key[0] = val; return 0; }
	if (t == 1.0f) { m_Key[m_NKeys - 1] = val; return m_NKeys - 1; }

	int lastKey = m_NKeys - 1;
	for (int i = 0; i < lastKey; i++)
	{
		if (m_Time[i] == t) { m_Key[i] = val; }
		if (m_Time[i] <= t && m_Time[i + 1] > t) 
		{
			for (int j = m_NKeys - 1; j > i; j--)
			{
				m_Time[j + 1] = m_Time[j];
				m_Key [j + 1] = m_Key [j];
			}
			m_Time[i + 1] = t;
			m_Key [i + 1] = val;
			m_NKeys++;
			return i + 1;
		}
	}
	return -1;
} // AlphaRamp::AddKey

void AlphaRamp::Serialize( OutStream& os ) const
{
	os << m_NKeys;
	for (int i = 0; i < m_NKeys; i++)
	{
		os << m_Time[i] << m_Key[i];
	}
} // AlphaRamp::Serialize

void AlphaRamp::Unserialize( InStream& is )
{
	is >> m_NKeys;
	for (int i = 0; i < m_NKeys; i++)
	{
		is >> m_Time[i] >> m_Key[i];
	}
} // AlphaRamp::Unserialize



