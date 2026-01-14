 /*****************************************************************************/
/*	File:	rsPrimitiveBufferDX.inl
/*	Author:	Ruslan Shestopalyuk
/*	Date:	05.02.2003
/*****************************************************************************/
#include "rsRenderSystemDX.h"
const DWORD  c_FVF[] = 
{
	0, // vfUnknown		
	D3DFVF_XYZRHW	| D3DFVF_DIFFUSE | D3DFVF_SPECULAR | D3DFVF_TEX1, // vfTnL			
	D3DFVF_XYZ		| D3DFVF_DIFFUSE | D3DFVF_TEX2, // vf2Tex			
	D3DFVF_XYZ		| D3DFVF_NORMAL	 | D3DFVF_TEX1, // vfN				
	D3DFVF_XYZRHW	| D3DFVF_DIFFUSE | D3DFVF_TEX2, // vfTnL2			
	D3DFVF_XYZ		| D3DFVF_TEX1, // vfT				
	D3DFVF_XYZB1	| D3DFVF_LASTBETA_UBYTE4 | D3DFVF_TEX2, // vfMP1			
	D3DFVF_XYZB1	| D3DFVF_LASTBETA_UBYTE4 | D3DFVF_NORMAL | D3DFVF_DIFFUSE | D3DFVF_TEX2, // vfNMP1			
	D3DFVF_XYZRHW	| D3DFVF_DIFFUSE |D3DFVF_SPECULAR | D3DFVF_TEX2, // vfTnL2S			
	D3DFVF_XYZB2	| D3DFVF_LASTBETA_UBYTE4 | D3DFVF_NORMAL | D3DFVF_DIFFUSE | D3DFVF_TEX2, // vfNMP2			
	D3DFVF_XYZB3	| D3DFVF_LASTBETA_UBYTE4 | D3DFVF_NORMAL | D3DFVF_DIFFUSE | D3DFVF_TEX2, // vfNMP3			
	D3DFVF_XYZB4	| D3DFVF_LASTBETA_UBYTE4 | D3DFVF_NORMAL | D3DFVF_DIFFUSE | D3DFVF_SPECULAR | D3DFVF_TEX2, // vfNMP4	
	D3DFVF_XYZ		| D3DFVF_NORMAL	 | D3DFVF_DIFFUSE | D3DFVF_SPECULAR | D3DFVF_TEX2, // vfN2T	
	D3DFVF_XYZ		| D3DFVF_DIFFUSE, // vfXYZD 
	D3DFVF_XYZRHW	// vfXYZW
};

_inl DWORD VertexFormatFVF( VertexFormat vf )
{
	assert( vf < c_NumVertexTypes );
	return c_FVF[(int)vf];
}

_inl D3DPRIMITIVETYPE PriTypeDX( PrimitiveType priType )
{
	D3DPRIMITIVETYPE d3dPri = D3DPT_POINTLIST;
	switch (priType)
	{
		case ptTriangleList:	d3dPri = D3DPT_TRIANGLELIST;	break;
		case ptTriangleStrip:	d3dPri = D3DPT_TRIANGLESTRIP;	break;
		case ptTriangleFan:
								d3dPri = D3DPT_TRIANGLEFAN;		break;
		case ptLineStrip:		d3dPri = D3DPT_LINESTRIP;		break;
		case ptLineList:		
								d3dPri = D3DPT_LINELIST;		break;
		case ptPointList:		d3dPri = D3DPT_POINTLIST;		break;
	}
	return d3dPri;
}  // RenderSystemD3D::_InterpretMemPool





