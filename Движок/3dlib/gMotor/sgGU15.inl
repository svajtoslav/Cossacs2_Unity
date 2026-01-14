/*****************************************************************************/
/*	File:	sgGU15.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	03.03.2003
/*****************************************************************************/

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	GU15Package implementation
/*****************************************************************************/
_inl const GU15SpriteHdr* GU15Package::GetSpriteHeader( int frameID ) 
{
	const BYTE* pData = GetFileData();
	if (!pData) return NULL;	
	return (GU15SpriteHdr*)( pData + sizeof(GU15Header) + m_InfoLen + frameID*sizeof(GU15SpriteHdr)); 
} // GU15Package::GetSpriteHeader

END_NAMESPACE(sg)
