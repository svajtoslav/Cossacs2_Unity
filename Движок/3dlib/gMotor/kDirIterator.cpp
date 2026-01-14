/*****************************************************************************/
/*	File:	kDirIterator.cpp
/*	Desc:	Directory tree manipulation class
/*	Author:	Ruslan Shestopalyuk
/*	Date:	06-11-2003
/*****************************************************************************/
#include "stdafx.h"
#include "kDirIterator.h"
#include <direct.h>

/*****************************************************************************/
/*	DirIterator implementation
/*****************************************************************************/
DirIterator::DirIterator( const char* _startDir )
{
    strcpy( startDir, _startDir );
    int dnlen = strlen( startDir );
    if (startDir[dnlen - 1] != '\\') strcat( startDir, "\\" );
	_splitpath( startDir, fileDrive, fileDir, fileName, fileExt ); 

	strcpy( startDir, fileDrive );
	strcat( startDir, fileDir	);
	strcpy( rootDir, startDir );
	strcat( startDir, "*.*" );

	searchEnded		= false;
	pExtF			= extF;
	nFilters		= 0;
	searchHandle	= NULL;

	extF		[0]	= '\0';

	fileName	[0]	= '\0';
	fileExt		[0]	= '\0';	
	fileDir		[0]	= '\0';	
	fileDrive	[0]	= '\0';
	fileNameExt	[0]	= '\0';

	Reset();
} // DirIterator::DirIterator

bool DirIterator::Reset()
{
	searchHandle = FindFirstFile( startDir, &findData );
	if (searchHandle == INVALID_HANDLE_VALUE)
	{
		searchEnded = true;
		return false;
	}

	OnFoundFile();

	if (IsSkipped()) OnNextFile();
	searchEnded = false;
	return true;
} // DirIterator::Reset

bool DirIterator::IsFile() const
{
	if (searchEnded) return false;
	DWORD flags = findData.dwFileAttributes;
	if (flags & FILE_ATTRIBUTE_SYSTEM) return false;
	if (flags & FILE_ATTRIBUTE_DIRECTORY) return false;
	if (GetFileName()[0] == 0) return false;

	return true;
} // DirIterator::IsFile

bool DirIterator::IsDirectory() const
{
	return ((findData.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY) != 0)&&
			fileExt[0] != '.'; 
} // DirIterator::IsDirectory

bool DirIterator::AddFilter( const char* _extF )
{
	int len = strlen( _extF ) + 1;
	if ((pExtF - extF) + len >= c_MaxExtNamesLen) return false;
	strcpy( pExtF, _extF );
	nFilters++;

	if (IsSkipped()) OnNextFile();
	return true;
} // DirIterator::AddFilter

DirIterator& DirIterator::operator++()
{
	OnNextFile();
	return *this;
} // DirIterator::operator++ prefix

DirIterator DirIterator::operator++( int )
{
	OnNextFile();
	return *this;
} // DirIterator::operator++ postfix

bool DirIterator::IsSkipped()
{
	if (searchEnded) return true;

	if (nFilters == 0) return false;
	for (int i = 0; i < nFilters; i++)
	{
		const char* pFilter = extF;
		const char* pFileExt = fileExt;

		//  skip possible dots in extensions
		if (fileExt[0] == '.') pFileExt++;
		if (pFilter[0] == '.') pFilter++;

		if (!stricmp( pFilter, pFileExt )) return false;
		pFilter += strlen( pFilter );
	}
	return true;
} // DirIterator::IsSkipped

const char*	DirIterator::GetFullFilePath() const
{
	_makepath( fileFullPath, "", rootDir, fileName, fileExt );
	return fileFullPath;
}

const char*	DirIterator::GetFullFileName() const
{
	if (searchEnded) return NULL;
	return findData.cFileName;
} // DirIterator::GetFullFileName

const char*	DirIterator::GetFileName() const
{
	if (searchEnded) return NULL;
	return fileName;
} // DirIterator::GetFileName

const char*	DirIterator::GetFileNameExt() const
{
	if (searchEnded) return NULL;
	return fileNameExt;
} // DirIterator::GetFileNameExt

void DirIterator::OnNextFile()
{
	assert( searchHandle );

	while( true )
	{
		BOOL res = FindNextFile( searchHandle, &findData );
		if (res == 0)
		{
			searchEnded = true;
			FindClose( searchHandle );
			searchHandle = NULL;
			return;
		}
		OnFoundFile();
		if (!IsSkipped()) break;
	} 

	searchEnded = false;
} // DirIterator::OnNextFile

void DirIterator::OnFoundFile()
{
	if (searchEnded) return;
	const char* fullPath = findData.cFileName;
	_splitpath( fullPath, fileDrive, fileDir, fileName, fileExt );

	strcpy( fileNameExt, fileName );
	strcat( fileNameExt, fileExt );

} // DirIterator::OnFoundFile