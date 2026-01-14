//----------------------------------------------------------------------------------------------------------------//
#include "stdheader.h"
//----------------------------------------------------------------------------------------------------------------//
typedef bool FileAction(HANDLE ActionFile, WIN32_FIND_DATA* WFD, const char* FileName, void* Param);

int ProcessTree(const char* lpszTreeNode , FileAction* FA, void* Param)
{
	WIN32_FIND_DATA	wfd;
	HANDLE			hFindFile;
	CHAR			szDelPath[255];
	CHAR			szNewLevel[255];
	CHAR			szDelStr[255];
	BOOL			Found;

	strcpy(szDelPath,lpszTreeNode);
	strcat(szDelPath,"\\*.*");

	hFindFile=FindFirstFile(szDelPath,&wfd);

	if(hFindFile!=INVALID_HANDLE_VALUE)
		Found=TRUE;
	else
		Found=FALSE;

	while(Found)
	{
		if((wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)!=FILE_ATTRIBUTE_DIRECTORY)
		{
			strcpy(szDelStr,lpszTreeNode);
			strcat(szDelStr,"\\");
			strcat(szDelStr,wfd.cFileName);
			FA(hFindFile,&wfd, szDelStr, Param);
		}
		else
		{
			// Enter new level
			if((strcmp(wfd.cFileName,".")!=0) && (strcmp(wfd.cFileName,"..")!=0))
			{
				strcpy(szNewLevel,lpszTreeNode);
				strcat(szNewLevel,"\\");
				strcat(szNewLevel,wfd.cFileName);
				ProcessTree(szNewLevel, FA, Param);
			};
		};			
		Found=FindNextFile(hFindFile,&wfd);
	};

	FindClose(hFindFile);
	return 0;
}
//----------------------------------------------------------------------------------------------------------------//
bool AddFileName(HANDLE ActionFile, WIN32_FIND_DATA* WFD, const char* FileName, void* Param)
{
	if(FileName)
	{
		_strupr((char*)FileName);
		if(strstr(FileName,".G2D")||strstr(FileName,".G16")||strstr(FileName,".G17"))
		{
			static Enumerator* En = ENUM.Get("CheckFileEnumerator");
			if(FileName[0]=='\\')
				En->Add((char*)(FileName+1),1);
			if(FileName[0]=='.')
				En->Add((char*)(FileName+2),1);
			else
				En->Add((char*)(FileName),1);
		}
	}
	return false;
}
//----------------------------------------------------------------------------------------------------------------//
bool CheckFileEnumeratorFirstProcess=false;
bool CheckFileExistsEnumerator(const char* FileName)
{
	bool rez=false;
	if(FileName)
	{
		if(!CheckFileEnumeratorFirstProcess)
		{
			ProcessTree(".", &AddFileName, NULL);
			CheckFileEnumeratorFirstProcess=true;
		}
		/*
		char Upp[256];
		strcpy(Upp,FileName);
		_strupr(Upp);
		*/
		static Enumerator* En = ENUM.Get("CheckFileEnumerator");
		
		if(En->Get((char*)FileName)==1)
		{
			rez=true;
		}
		else
		{
			rez=false;
		}
	}
	return rez;
}
//----------------------------------------------------------------------------------------------------------------//