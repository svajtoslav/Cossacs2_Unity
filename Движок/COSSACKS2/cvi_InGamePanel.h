#pragma once
#include "MapTemplates.h"
//////////////////////////////////////////////////////////////////////////

class cvi_InGamePanel : public BaseClass
{
public:
	cvi_InGamePanel(void);
	//int Level;
	//bool VirtualUp;
	//int AbIndex;
	void StartFrame(char* Message, int OpenTime, int ShowTime, int CloseTime);
	char* Mess;
	int Start;
	int Open;
	int Show;
	int Close;
	SAVE(cvi_InGamePanel){
		//REG_MEMBER(_int,Level);
		//REG_MEMBER(_bool,VirtualUp);
		//REG_MEMBER(_int,AbIndex);
	}ENDSAVE;
};
extern cvi_InGamePanel vmIGP;

//////////////////////////////////////////////////////////////////////////

regAc(cva_IGP_Frame, vfS 
	ClassRef<TextButton> Mess,
	REG_AUTO(Mess);
);
regAc(cva_IGP_Credits, vfS , );
