#include "oggvor.h"
#include "kLog.h"

static BOOL bInitialized=FALSE;
static HMODULE hVopyl=NULL;

typedef void (__cdecl *LPINIT_FUNC)(LPDIRECTSOUND pDS, HWND hWnd);
typedef void (__cdecl *LPDONE_FUNC)();
typedef void (__cdecl *LPPLAY_FUNC)(LPCSTR pcszFileName,BYTE ucStream);
typedef void (__cdecl *LPSTOP_FUNC)(BYTE ucStream);
typedef void (__cdecl *LPSETVOLUME_FUNC)(BYTE ucVolume,BYTE ucStream);
typedef void (__cdecl *LPCYCLIC_FUNC)(bool bCyclic,BYTE ucStream);
typedef WORD (__cdecl *LPGETSTREAMLENGTH_FUNC)(BYTE ucStream);
typedef bool (__cdecl *LPSTREAMFINISHED_FUNC)(BYTE ucStream);

LPINIT_FUNC				pInit				= NULL;
LPDONE_FUNC				pDone				= NULL;
LPPLAY_FUNC				pov_Play			= NULL;
LPSTOP_FUNC				pov_Stop			= NULL;
LPSETVOLUME_FUNC		pov_SetVolume		= NULL;
LPCYCLIC_FUNC			pov_Cyclic			= NULL;
LPGETSTREAMLENGTH_FUNC	pov_GetStreamLength	= NULL;
LPSTREAMFINISHED_FUNC	pov_StreamFinished	= NULL;

void VoplError(const char* pcszError){
	MessageBox(NULL,pcszError,"Vopyl Critical Error",MB_OK | MB_ICONHAND);
	ExitProcess(1);
}

void ov_Init(LPDIRECTSOUND pDS, HWND hWnd)
{
	if(bInitialized){
		MessageBox(NULL,"Vopyl library already initialized!","Vopyl Warning",MB_OK | MB_ICONHAND);
		return;
	};

	char	szOVSD[MAX_PATH];

	GetModuleFileName(NULL,szOVSD,MAX_PATH);
	
	*(strrchr(szOVSD,'\\'))='\0';
	strcat(szOVSD,"\\vopyl.dll");

	hVopyl=LoadLibrary(szOVSD);
	
	if(hVopyl==NULL)
		VoplError("Unable to load vopyl.dll library!");

	pInit				= (LPINIT_FUNC)				GetProcAddress(hVopyl,"nvInit");
	pDone				= (LPDONE_FUNC)				GetProcAddress(hVopyl,"nvDone");
	pov_Play			= (LPPLAY_FUNC)				GetProcAddress(hVopyl,"nvPlay");
	pov_Stop			= (LPSTOP_FUNC)				GetProcAddress(hVopyl,"nvStop");
	pov_SetVolume		= (LPSETVOLUME_FUNC)		GetProcAddress(hVopyl,"nvSetVolume");
	pov_Cyclic			= (LPCYCLIC_FUNC)			GetProcAddress(hVopyl,"nvCyclic");
	pov_GetStreamLength	= (LPGETSTREAMLENGTH_FUNC)	GetProcAddress(hVopyl,"nvGetStreamLength");
	pov_StreamFinished	= (LPSTREAMFINISHED_FUNC)	GetProcAddress(hVopyl,"nvStreamFinished");

	if(	(!pInit)||
		(!pDone)||
		(!pov_Play)||
		(!pov_Stop)||
		(!pov_SetVolume)||
		(!pov_Cyclic)||
		(!pov_GetStreamLength)||
		(!pov_StreamFinished))
			VoplError("Unable to resolve all symbols from vopyl.dll library!");

	pInit(pDS,hWnd);

	bInitialized=TRUE;
}

void ov_Done()
{
	if(!bInitialized){
		MessageBox(NULL,"Vopyl library not yet initialized!","Vopyl Warning",MB_OK | MB_ICONHAND);
		return;
	};

	pDone();
	FreeLibrary(hVopyl);

	pInit				= NULL;
	pDone				= NULL;
	pov_Play			= NULL;
	pov_Stop			= NULL;
	pov_SetVolume		= NULL;
	pov_Cyclic			= NULL;
	pov_GetStreamLength	= NULL;
	pov_StreamFinished	= NULL;

	bInitialized=FALSE;
}

void ov_Play(LPCSTR pcszFileName,BYTE ucStream)
{
	if(GetFileAttributes(pcszFileName)==INVALID_FILE_ATTRIBUTES){
		// write to log
		Log.Warning("Vopyl: can't find file [%s] to play",pcszFileName);
		return;
	}

	if(pov_Play)
		pov_Play(pcszFileName,ucStream);
}

void ov_Stop(BYTE ucStream)
{
	if(pov_Stop)
		pov_Stop(ucStream);
}

void ov_SetVolume(BYTE ucVolume,BYTE ucStream)
{
	if(pov_SetVolume)
		pov_SetVolume(ucVolume,ucStream);
}

void ov_Cyclic(bool bCyclic,BYTE ucStream)
{
	if(pov_Cyclic)
		pov_Cyclic(bCyclic,ucStream);
}

WORD ov_GetStreamLength(BYTE ucStream)
{
	if(pov_GetStreamLength)
		return pov_GetStreamLength(ucStream);
	else
		return 0;
}

bool ov_StreamFinished(BYTE ucStream)
{
	if(pov_StreamFinished)
		return pov_StreamFinished(ucStream);
	else
		return true;
}
