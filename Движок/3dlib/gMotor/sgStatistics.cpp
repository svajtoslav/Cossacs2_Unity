#include "stdafx.h"
#include "sgFont.h"
#include "uiControl.h"
#include "kStatistics.h"
#include "kTimer.h"
#include "sgStatistics.h"
#include "IWidgetManager.h"
#include "IEffectManager.h"

IStatistics* IStats = NULL;
BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	StatManager implementation
/*****************************************************************************/
StatManager::StatManager()
{
    IStats = this;
}

void StatManager::Render()
{
	static Timer s_Timer;
	float sec = s_Timer.seconds(); 
	s_Timer.start();

	float fps = 0.0f;
	if (sec > 0.0f) 
	{ 
		fps = 1.0f / sec;
		m_FPS.push( fps );
	}

	static s_FontID = IWM->CreateFont( "Tahoma", 8 );
	
	Vector3D pos( 10, 10, 0.0f );
	char text[256];
	sprintf( text, "FPS:  %.2f", fps );
	IWM->DrawString( s_FontID, text, pos, 0xFFFF1111 );

#ifndef _NOSTAT
	sprintf( text, "Poly: %.0f", GET_COUNTER( Polygons ) );
	pos.y += 10;
	IWM->DrawString( s_FontID, text, pos, 0xFFAAAAFF );

	sprintf( text, "Dips: %.0f", GET_COUNTER( Dips ) );
	pos.y += 10;
	IWM->DrawString( s_FontID, text, pos, 0xFFFFFF00 );

	sprintf( text, "DSS:  %.0f", GET_COUNTER( ShaderChanges ) );
	pos.y += 10;
	IWM->DrawString( s_FontID, text, pos, 0xFFAAAAFF );


	sprintf( text, "Tex:  %.0f", GET_COUNTER( TexSwitches ) );
	pos.y += 10;
	IWM->DrawString( s_FontID, text, pos, 0xFFFFFF00 );

    sprintf( text, "GPTex:  %.0f", GET_COUNTER( GPTex ) );
    pos.y += 10;
    IWM->DrawString( s_FontID, text, pos, 0xFFAAAAFF );
#endif // !_NOSTAT

    IWM->FlushText( s_FontID );

	Node::Render();
	Stats::OnFrame();
} // StatManager::Render

END_NAMESPACE(sg)

