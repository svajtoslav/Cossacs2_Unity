/*****************************************************************************/
/*	File:	kTimer.h
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	10-04-2004
/*****************************************************************************/
#ifndef __KTIMER_H__
#define __KTIMER_H__

/*****************************************************************************/
/*	Class:	Timer
/*	Desc:	High resolution timer
/*****************************************************************************/
class Timer
{
	LARGE_INTEGER			m_TimeTotal;	//  total timer time
	LARGE_INTEGER			m_TimeStart;	//  current start time 
	LARGE_INTEGER			m_Frequency;	//  timer frequency

public:
	Timer()
	{
		if (QueryPerformanceFrequency( &m_Frequency ) == FALSE)
		{
			assert( !"No performance counter available!" );
		}
		reset();
	}

	void start()
	{
		reset();
		QueryPerformanceCounter( &m_TimeStart );
	}

	void stop()
	{
		LARGE_INTEGER stopTime;

		QueryPerformanceCounter( &stopTime );
		m_TimeTotal.QuadPart += stopTime.QuadPart - m_TimeStart.QuadPart;
	}

	void reset()
	{
		memset( &m_TimeTotal, 0, sizeof( m_TimeTotal ) );
		memset( &m_TimeStart, 0, sizeof( m_TimeStart ) );
	}

	void cont()
	{
		QueryPerformanceCounter( &m_TimeStart );
	}

	float seconds() const
	{
		LARGE_INTEGER curTime;

		QueryPerformanceCounter( &curTime );
		curTime.QuadPart -= m_TimeStart.QuadPart;
		return (float)curTime.QuadPart/(float)m_Frequency.QuadPart;
	}

}; // class Timer

#endif // __KTIMER_H__