#include "stdheader.h"
#include "GameExtension.h"
#include "BoidsExtension.h"

extern int tmtmt;
void FillNeighboringList();
void CalculatePushForce();
extern DynArray<word> NeighboringUnits;
extern DynArray<int> UnitsCoordAndPushForce;
extern EngineSettings EngSettings;
void BoidsExtension::ProcessingGame()
{
	if(MAXOBJECT<EngSettings.BoidsOffLimit)
	{
		if(tmtmt%17==0)
			FillNeighboringList();
		CalculatePushForce();
	}
}
void BoidsExtension::OnGameStart()
{
	NeighboringUnits.Clear();
	UnitsCoordAndPushForce.Clear();
}