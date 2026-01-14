/*****************************************************************
/*  File:   mSkin.cpp                                      
/*	Desc:	Skinning code here					  
/*****************************************************************/
#include "stdafx.h"
#include "mSkin.h"

void fpu_Skin1( const Vertex1W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );                                 
void fpu_Skin2( const Vertex2W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );                                 
void fpu_Skin3( const Vertex3W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );                                 
void fpu_Skin4( const Vertex4W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );   

void sse_Skin1( const Vertex1W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );                                 
void sse_Skin2( const Vertex2W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );                                 
void sse_Skin3( const Vertex3W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones );                                 
void sse_Skin4( const Vertex4W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones ); 

void *sse_memcpy(void *dest,const void *src,size_t count)
{__asm{
    // Assuming we have SSE-capable processor and count is 4-byte aligned
    mov			esi,DWORD PTR [src]
    mov			edi,DWORD PTR [dest]
    mov			eax,DWORD PTR [count]
    ;
    prefetchnta	BYTE PTR [esi]
    ;
    mov			ebx,edi
        and			ebx,1111b
        jz			_al_start_
        mov			ecx,10000b
        sub			ecx,ebx
        sub			eax,ecx
        shr			ecx,2
_pr_loop_:
    mov			edx,DWORD PTR [esi]
    mov			DWORD PTR [edi],edx
        add			esi,4
        add			edi,4
        dec			ecx
        jnz			_pr_loop_
_al_start_:
    mov			edx,eax
        shr			edx,7
        jz			_vs_start_
        mov			ecx,edx
        test		esi,1111b
        jz			_bga_loop_
_bgu_loop_:
    movups		xmm0,XMMWORD PTR [esi+00]
    movups		xmm1,XMMWORD PTR [esi+16]
    movups		xmm2,XMMWORD PTR [esi+32]
    movups		xmm3,XMMWORD PTR [esi+48]
    movups		xmm4,XMMWORD PTR [esi+64]
    movups		xmm5,XMMWORD PTR [esi+80]
    movups		xmm6,XMMWORD PTR [esi+96]
    movups		xmm7,XMMWORD PTR [esi+112]
    ;
    movntps		XMMWORD PTR [edi+00],xmm0
        movntps		XMMWORD PTR [edi+16],xmm1
        movntps		XMMWORD PTR [edi+32],xmm2
        movntps		XMMWORD PTR [edi+48],xmm3
        movntps		XMMWORD PTR [edi+64],xmm4
        movntps		XMMWORD PTR [edi+80],xmm5
        movntps		XMMWORD PTR [edi+96],xmm6
        movntps		XMMWORD PTR [edi+112],xmm7
        ;
    add			esi,128
        add			edi,128
        dec			edx
        jnz			_bgu_loop_
        jmp short	_sm_start_
        ;
_bga_loop_:
    movaps		xmm0,XMMWORD PTR [esi+00]
    movaps		xmm1,XMMWORD PTR [esi+16]
    movaps		xmm2,XMMWORD PTR [esi+32]
    movaps		xmm3,XMMWORD PTR [esi+48]
    movaps		xmm4,XMMWORD PTR [esi+64]
    movaps		xmm5,XMMWORD PTR [esi+80]
    movaps		xmm6,XMMWORD PTR [esi+96]
    movaps		xmm7,XMMWORD PTR [esi+112]
    ;
    movntps		XMMWORD PTR [edi+00],xmm0
        movntps		XMMWORD PTR [edi+16],xmm1
        movntps		XMMWORD PTR [edi+32],xmm2
        movntps		XMMWORD PTR [edi+48],xmm3
        movntps		XMMWORD PTR [edi+64],xmm4
        movntps		XMMWORD PTR [edi+80],xmm5
        movntps		XMMWORD PTR [edi+96],xmm6
        movntps		XMMWORD PTR [edi+112],xmm7
        ;
    add			esi,128
        add			edi,128
        dec			edx
        jnz			_bga_loop_
_sm_start_:
    shl			ecx,7
        sub			eax,ecx
        jz			_loop_ok_
_vs_start_:
    shr			eax,2
_sm_loop_:
    mov			ecx,DWORD PTR [esi]
    mov			DWORD PTR [edi],ecx
        add			esi,4
        add			edi,4
        dec			eax
        jnz			_sm_loop_
_loop_ok_:
    sfence	
};
return dest;
}; // sse_memcpy

FuncSkin1 Skin1 = fpu_Skin1;
FuncSkin2 Skin2 = fpu_Skin2;
FuncSkin3 Skin3 = fpu_Skin3;
FuncSkin4 Skin4 = fpu_Skin4;
FuncMemCopy MemCopy = memcpy;

ProcOptimMode   g_ProcOptimMode = poNone;
void SetProcessorOptimizations( ProcOptimMode mode )
{
    g_ProcOptimMode = mode;
    switch (mode)
    {
    case poNone: 
        Skin1 = fpu_Skin1;
        Skin2 = fpu_Skin2;
        Skin3 = fpu_Skin3;
        Skin4 = fpu_Skin4;
        MemCopy = memcpy;
    break;
    case poSSE:
        Skin1 = sse_Skin1;
        Skin2 = sse_Skin2;
        Skin3 = sse_Skin3;
        Skin4 = sse_Skin4;
        MemCopy = sse_memcpy;
    break;
    }
} // SetProcessorOptimizations

void InitMath()
{
    if (HaveSSE())
    {
        SetProcessorOptimizations( poSSE );
    }
    else
    {
        SetProcessorOptimizations( poNone );
    }
} // InitMath

ProcOptimMode GetProcessorOptimizations()
{
    return g_ProcOptimMode;
} // SetProcessorOptimizations

void fpu_Skin1( const Vertex1W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones )
{
    for (int i = 0; i < nV; i++)
    {
        const Vertex1W& v = vSrc[i];
        Vector3D pos    = v.pos;
        Vector3D normal = v.normal;
        bones[v.m].transformPt	( pos );
        bones[v.m].transformVec	( normal );
        vDest[i].pos    = pos;
        vDest[i].normal = normal;
        vDest[i].u      = v.u;
        vDest[i].v      = v.v;
    }
} // fpu_Skin1

void fpu_Skin2( const Vertex2W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones )
{
    Matrix4D boneM; 
    for (int i = 0; i < nV; i++)
    {
        const Vertex2W& v = vSrc[i];
        Vector3D pos    = v.pos;
        Vector3D normal = v.normal;
        //  calculate blending matrix
        boneM.Blend2(   bones[v.m0], v.w, 
                        bones[v.m1], 1.0f - v.w );
        boneM.transformPt	( pos );
        boneM.transformVec	( normal );
        vDest[i].pos    = pos;
        vDest[i].normal = normal;
        vDest[i].u      = v.u;
        vDest[i].v      = v.v;
    }
} // fpu_Skin2

void fpu_Skin3( const Vertex3W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones )
{
    Matrix4D boneM;
    for (int i = 0; i < nV; i++)
    {
        const Vertex3W& v = vSrc[i];
        Vector3D pos    = v.pos;
        Vector3D normal = v.normal;
        //  calculate blending matrix
        boneM.Blend3(   bones[v.m0], v.w0, 
                        bones[v.m1], v.w1, 
                        bones[v.m2], 1.0f - v.w0 - v.w1 );
        boneM.transformPt	( pos );
        boneM.transformVec	( normal );
        vDest[i].pos    = pos;
        vDest[i].normal = normal;
        vDest[i].u      = v.u;
        vDest[i].v      = v.v;
    }
} // fpu_Skin3

void fpu_Skin4( const Vertex4W* vSrc, VertexOut* vDest, int nV, const Matrix4D* bones )
{
    Matrix4D boneM;
    for (int i = 0; i < nV; i++)
    {
        const Vertex4W& v = vSrc[i];
        Vector3D pos    = v.pos;
        Vector3D normal = v.normal;
        //  calculate blending matrix
        boneM.Blend4(   bones[v.m0], v.w0, 
                        bones[v.m1], v.w1, 
                        bones[v.m2], v.w2,
                        bones[v.m3], 1.0f - v.w0 - v.w1 - v.w2 );
        boneM.transformPt	( pos );
        boneM.transformVec	( normal );
        vDest[i].pos    = pos;
        vDest[i].normal = normal;
        vDest[i].u      = v.u;
        vDest[i].v      = v.v;
    }
} // fpu_Skin4