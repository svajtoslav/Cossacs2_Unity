/*****************************************************************************/
/*	File:	kDirIterator.h
/*	Desc:	Directory tree manipulation class
/*	Author:	Ruslan Shestopalyuk
/*	Date:	06-11-2003
/*****************************************************************************/
#ifndef __KDIREITERATOR_H__
#define __KDIREITERATOR_H__

const int c_MaxExtNamesLen = _MAX_EXT*16;
/*****************************************************************************/
/*	Class:	DirIterator
/*	Desc:	Allows searching in the directories
/*	Example: 
/*			
/*			DirIterator it( "c:\\images" );
/*			it.AddFilter( "jpg" );
/*			it.AddFilter( "bmp" );
/*			it.AddFilter( "tga" );
/*
/*			while (it)
/*			{
/*				const char* fname = it.GetFullFileName();
/*				printf( "%s\n", fname );
/*				it++;
/*			}
/*
/*****************************************************************************/
class DirIterator
{
	char				startDir[_MAX_PATH];
	char				extF[c_MaxExtNamesLen];
	char*				pExtF;
	int					nFilters;
	bool				searchEnded;
	WIN32_FIND_DATA		findData;
	HANDLE				searchHandle;

	char				fileName	[_MAX_FNAME];
	char				fileExt		[_MAX_EXT];
	char				fileDir		[_MAX_DIR];
	char				fileDrive	[_MAX_DRIVE];

	char				fileNameExt	[_MAX_PATH];
	char				rootDir		[_MAX_PATH];
	mutable char		fileFullPath[_MAX_PATH];

public:
						DirIterator		( const char* _startDir );
	bool				AddFilter		( const char* _extF );
	const char*			GetFullFileName	() const;
	const char*			GetFullFilePath	() const;
	const char*			GetFileName		() const;
	const char*			GetFileExt		() const { return fileExt; }
	DWORD				GetFileSize		() const { return findData.nFileSizeLow; }
	const char*			GetFileNameExt	() const;
	bool				Reset			();
	bool				IsFile			() const;
	bool				IsDirectory		() const;
	DirIterator&		operator++		();			// Prefix increment operator.
	DirIterator			operator++		( int );	// Postfix increment operator.
	operator			bool			() const { return !searchEnded; }

protected:
	void				OnFoundFile		();
	void				OnNextFile		();
	bool				IsSkipped		();

}; // class DirIterator

#endif // __KDIREITERATOR_H__
