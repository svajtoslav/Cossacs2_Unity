/*****************************************************************************/
/*	File:	kFilePath.cpp
/*	Desc:	File path manipulations	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	7.11.2004
/*****************************************************************************/
#include "stdafx.h"
#include "kFilePath.h"

/*****************************************************************************/
/*  FilePath implementation
/*****************************************************************************/
void FilePath::ToLowercase()
{
    _strlwr( m_SourcePath );
    SetPath( m_SourcePath );
} // FilePath::ToLowercase

void FilePath::SetExt( const char* ext )
{
    m_Ext[0] = '.';
    strcpy( m_Ext + 1, ext );
    UpdatePath();
} // FilePath::SetExt

void FilePath::SetDir( const char* dir )
{
    char directory  [_MAX_PATH];
    char drive      [_MAX_PATH];
    char fileName   [_MAX_PATH];
    char extension  [_MAX_PATH];
    _splitpath( dir, drive, directory, fileName, extension );
    strcpy( m_Dir, directory );
    UpdatePath();
    if (drive[0] != 0) SetDrive( drive );
} // FilePath::SetDir

void FilePath::SetDrive( const char* drv )
{
    char directory  [_MAX_PATH];
    char drive      [_MAX_PATH];
    char fileName   [_MAX_PATH];
    char extension  [_MAX_PATH];
    _splitpath( drv, drive, directory, fileName, extension );
    strcpy( m_Drive, drive );
    UpdatePath();
} // FilePath::SetDrive

void FilePath::SetFileName( const char* fname )
{
    strcpy( m_FileName, fname );
    UpdatePath();
} // FilePath::SetFileName

void FilePath::AppendDir( const char* dir )
{
    char directory  [_MAX_PATH];
    char drive      [_MAX_PATH];
    char fileName   [_MAX_PATH];
    char extension  [_MAX_PATH];
    _splitpath( dir, drive, directory, fileName, extension );

    char* pEnd = m_Dir + strlen( m_Dir ) - 1;
    const char* pBeg = directory;
    while (*pEnd && (*pEnd == '\\' || *pEnd == '/')) pEnd--;
    pEnd++;
    while (*pBeg && (*pBeg == '\\' || *pBeg == '/')) pBeg++;
    strcpy( pEnd, "\\" );
    strcat( pEnd, pBeg );
    UpdatePath();
} // FilePath::AppendDir

void FilePath::SetPath( const char* path )
{
    strcpy( m_SourcePath, path );
    _splitpath( m_SourcePath, m_Drive, m_Dir, m_FileName, m_Ext );
    int fExtSz = strlen( m_FileName ) + strlen( m_Ext );
    m_pFileExt = m_SourcePath + strlen( m_SourcePath ) - fExtSz;
} // FilePath::SetPath

void FilePath::UpdatePath()
{
    _makepath( m_SourcePath, m_Drive, m_Dir, m_FileName, m_Ext );
    int fExtSz = strlen( m_FileName ) + strlen( m_Ext );
    m_pFileExt = m_SourcePath + strlen( m_SourcePath ) - fExtSz;
} // FilePath::UpdatePath