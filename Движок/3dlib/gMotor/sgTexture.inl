/*****************************************************************************/
/*	File:	sgTexture.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/

BEGIN_NAMESPACE(sg)
/*****************************************************************************/
/*	Texture implementation
/*****************************************************************************/
_inl Texture::Texture() : m_Stage(0), m_TexID(-1)
{
}

_inl Texture::Texture( const char* name, int stage ) 
		: m_Stage(stage), m_TexID(-1), AssetNode(name)
{
}

_inl void Texture::Render()
{
	if (s_bFrozen) return;

	if (m_TexID == -1)
	{
		if (GetFlagState( nfEmbeddedData ))
		{
			m_TexID = IRS->GetTextureID( GetName(), GetData(), GetDataSize() );
		}
		else
		{
			m_TexID = IRS->GetTextureID( GetName() );
		}

		if (m_TexID != 0 && m_TexID != 0xFFFFFFFF)
		{
			m_Descr = *(IRS->GetTextureDescr( m_TexID ));
		}
	}
	IRS->SetTexture( m_TexID, m_Stage );
} // Texture::Render

_inl int Texture::GetWidth() const
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0 || m_TexID == -1) return 0;
		m_Descr = *(IRS->GetTextureDescr( m_TexID ));
	}
	return m_Descr.getSideX();
}

_inl int Texture::GetHeight() const
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0 || m_TexID == -1) return 0;
		m_Descr = *(IRS->GetTextureDescr( m_TexID ));
	}
	return m_Descr.getSideY();
}

_inl TextureUsage Texture::GetUsage() const
{
	return m_Descr.getTexUsage();
}

_inl void Texture::SetUsage( TextureUsage  usage )
{
	m_Descr.setTexUsage( usage ); 
}


_inl void Texture::SetWidth	( int val )
{ 
	m_Descr.setSideX( val ); 
}

_inl void Texture::SetHeight( int val )
{ 
	m_Descr.setSideY( val ); 
}

_inl void Texture::SetColorFormat( ColorFormat format )
{
	m_Descr.setColFmt( format );
}

_inl ColorFormat Texture::GetColorFormat() const
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0) return cfUnknown;
		const TextureDescr* pDesc = IRS->GetTextureDescr( m_TexID );
		if (pDesc) m_Descr = *pDesc;
	}
	return m_Descr.getColFmt();
} // Texture::GetColorFormat


_inl void Texture::SetDepthStencilFormat( DepthStencilFormat format )
{
	m_Descr.setDsFmt( format );
}

_inl DepthStencilFormat Texture::GetDepthStencilFormat() const
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0) return dsfUnknown;
		const TextureDescr* pDesc = IRS->GetTextureDescr( m_TexID );
		if (pDesc) m_Descr = *pDesc;
	}
	return m_Descr.getDsFmt();
} // Texture::GetDepthStencilFormat

_inl bool Texture::IsProcedural() const 
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0) return 0;
		const TextureDescr* pDesc = IRS->GetTextureDescr( m_TexID );
		if (pDesc) m_Descr = *pDesc;
	}
	return (m_Descr.getTexUsage() == tuProcedural);
} // Texture::IsProcedural

_inl bool Texture::IsRT() const 
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0) return 0;
		const TextureDescr* pDesc = IRS->GetTextureDescr( m_TexID );
		if (pDesc) m_Descr = *pDesc;
	}
	return (m_Descr.getTexUsage() == tuRenderTarget);
} // Texture::IsRT

_inl void Texture::SetIsProcedural( bool val ) 
{
	if (val) m_Descr.setTexUsage( tuProcedural );
	else m_Descr.setTexUsage( tuLoadable );
} // Texture::SetIsProcedural

_inl void Texture::SetIsRT( bool val )
{
	m_Descr.setTexUsage( tuRenderTarget );
}

_inl Texture* Texture::GetCurTexture( int m_Stage ) 
{ 
	return s_pCurTexture[m_Stage]; 
}

_inl MemoryPool	Texture::GetMemoryPool() const
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0) return mpUnknown;
		const TextureDescr* pDesc = IRS->GetTextureDescr( m_TexID );
		if (pDesc) m_Descr = *pDesc;
	}
	return m_Descr.getMemPool();
}

_inl void Texture::SetMemoryPool( MemoryPool pool )
{
	m_Descr.setMemPool( pool );
}

_inl void Texture::SetNMips( int val )
{
	m_Descr.setNMips( val );
}

_inl int Texture::GetNMips() const
{
	if (m_TexID == -1 && IRS)
	{
		m_TexID = IRS->GetTextureID( GetName() );
		if (m_TexID == 0) return mpUnknown;
		const TextureDescr* pDesc = IRS->GetTextureDescr( m_TexID );
		if (pDesc) m_Descr = *pDesc;
	}
	return m_Descr.getNMips();
}

END_NAMESPACE(sg)
