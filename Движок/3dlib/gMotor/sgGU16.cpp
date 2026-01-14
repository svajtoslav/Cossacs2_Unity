/*****************************************************************************/
/*	File:	sgGU16.cpp
/*	Author:	Ruslan Shestopalyuk
/*	Date:	03.03.2003
/*****************************************************************************/
#include "stdafx.h"

#include "kHash.hpp"
#include "kResource.h"
#include "sgSpriteManager.h"
#include "sgGQuad.h"
#include "sgGU16.h"
#include "FPack.h"

#ifndef _INLINES
#include "sgGU16.inl"
#endif // _INLINES

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	GU16Package implementation
/*****************************************************************************/
GU16Package::GU16Package() : m_FramesPerSegment(0)
{
}

void GU16Package::Init( const BYTE* data )
{
	if (!data) return;
	GU16Header* pHeader = (GU16Header*)data;
	m_NFrames			= pHeader->m_NSprites;
	m_NSegments			= pHeader->m_NPackSegments;
	m_FramesPerSegment	= pHeader->m_NFramesPerSegment;
	m_WorkBufSize		= pHeader->m_MaxWorkbuf;
	m_PixelDataSize		= pHeader->m_BlockSize;
	m_FrameWidth		= pHeader->m_XSize;
	m_FrameHeight		= pHeader->m_YSize;
} // GU16Package::Init

const BYTE*	GU16Package::GetSegmentData( DWORD sprID, DWORD& dataSize, DWORD& segIdx, 
										 DWORD& firstInSeg, DWORD& nFrames, 
										 DWORD* frameOffset, DWORD color )
{
	const BYTE* pData = GetFileData();
	if (!pData) return NULL;

	segIdx = sprID / m_FramesPerSegment;
	nFrames = m_FramesPerSegment;
	const GU16SegHdr* pHeader = (GU16SegHdr*)(pData + sizeof(GU16Header) + segIdx*sizeof(GU16SegHdr)); 

	if (segIdx == m_NSegments - 1) 
	{
		nFrames = m_NFrames - (m_NSegments - 1)*m_FramesPerSegment;
		dataSize = m_PixelDataSize - pHeader->GetOffset();
	}
	else
	{
		const GU16SegHdr* pNextHeader = pHeader + 1; 
		dataSize = pNextHeader->GetOffset() - pHeader->GetOffset();
	}

	firstInSeg = segIdx * m_FramesPerSegment;

	const BYTE* pSeg = pData + pHeader->GetOffset();
	DWORD flags;
	flags = pHeader->GetPackFlags();
	
	const BYTE* pPalette0 = GetPalette( 0 );
	const BYTE* pPalette1 = GetPalette( 1 );
	if (pPalette0 && pPalette1) G16SetPalette( (BYTE*)pPalette0, (BYTE*)pPalette1 );
	G16SetNationalColor( (color & 0x00FF0000) >> 16, (color & 0x0000FF00) >> 8, color & 0x000000FF );

	AdjustWorkBuffer( GetWorkBufSize() );
	DWORD unpackedSize = *((DWORD*)pSeg);
	AdjustUnpackBuffer( unpackedSize );

	bool res = G16UnpackSegment( const_cast<BYTE*>( pSeg ), 
								 dataSize - 4, s_UnpackBuffer, s_WorkBuffer, 
								 (unsigned int*)frameOffset, nFrames, 
								 flags );
	if (!res) return NULL;
	return s_UnpackBuffer;
} // GU16Package::GetSegmentData

int GU16Package::GetFrameNSquares( int frameID )
{
	const GU16SpriteHdr* pHeader = GetSpriteHeader( frameID );
	return pHeader->GetNChunks();
} // GU16Package::GetFrameNSquares

/*****************************************************************************/
/*	GU16Creator implementation
/*****************************************************************************/
GU16Creator::GU16Creator()
{
	SpritePackage::RegisterCreator( this );
}

SpritePackage* GU16Creator::CreatePackage( char* fileName, const BYTE* data )
{
	if (!data) return NULL;
	DWORD magic = *((DWORD*)data);
	if (magic != '61UG') return NULL;
	GU16Package* pPackage = new GU16Package();
	pPackage->Init( data );
	return pPackage;
} // GU16Creator::Load

const char*	GU16Creator::Description() const
{
	return "Uniform G16 Sprite Loader";
} // GU16Creator::Description

//
//#include "FCompressor.h"
//FCompressor FCOMP;
//bool FC_IsInit=0;
//void ConvertG17_Name(char* Name)
//{
//	char STR[512];
//	char STR2[512];
//	strcpy(STR,Name);
//	int L=strlen(STR);
//	strcat(STR,".g16");
//	if(!CheckIfFileExists(STR)){
//	//checking in cash
//	char STR1[512]="Cash\\";
//	strcat(STR1,STR);
//	int L1=L+5;
//	for(int i=5;i<L1;i++)if(STR1[i]=='\\'||STR1[i]=='/')STR1[i]='_';
//	if(!CheckIfFileExists(STR1)){
//	GOTOCASH:
//	//checking for g17 file
//	STR[L]=0;
//	strcat(STR,".g17");
//	if(CheckIfFileExists(STR)){
//	_mkdir("Cash");
//				
//	ResFile F=RReset(STR);
//	if(F!=INVALID_HANDLE_VALUE){
//		char* ptr=NULL;		
//		DWORD sz=RFileSize(F);
//		bool alloc=0;
//		if(!ptr){
//			alloc=1;
//			ptr=(char*)malloc(sz);
//			RBlockRead(F,ptr,sz);
//		};
//		char* outbits=NULL;
//		unsigned int outsize=0;
//		if(FCOMP.DecompressBlock(&outbits,&outsize,ptr)){
//			ResFile F1=RRewrite(STR1);
//			if(F1!=INVALID_HANDLE_VALUE){
//				RBlockWrite(F1,outbits,outsize);
//				RClose(F1);
//			};
//		};
//		delete[](outbits);
//		if(alloc)free(ptr);
//		RClose(F);
//	};
//	strcpy(STR,STR1);
//			};
//		}else{
//			if(GSets.CheckG17_dates){
//				//checking if file is not old
//				struct _stat g17info;
//				struct _stat g16info;
//				strcpy(STR2,Name);
//				strcat(STR2,".g17");
//				if(!(_stat(STR1,&g16info)||_stat(STR2,&g17info))){
//					if(g17info.st_mtime>=g16info.st_mtime){
//						if(DeleteFile(STR1))goto GOTOCASH;
//					}				
//				}
//			}
//			strcpy(STR,STR1);
//		}
//	}
//	strcpy(Name,STR);
//};

END_NAMESPACE(sg)
