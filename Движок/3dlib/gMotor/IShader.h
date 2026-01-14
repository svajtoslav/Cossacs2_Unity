/*****************************************************************
/*  File:   IShader.h                                      
/*  Desc:   Interface to the shader manipulation
/*	Author:	Ruslan Shestopalyuk
/*  Date:   Jun 2004											 
/*****************************************************************/
#ifndef __ISHADER_H__ 
#define __ISHADER_H__

/*****************************************************************/
/*	Enum:	AutoShaderConstant
/*	Desc:	Predefined types of the shader constants
/*****************************************************************/
enum AutoShaderConstant
{
	acWorldTM			= 0x1,	    //	current object's world transform
	acViewTM			= 0x2,	    //	camera view transform matrix
	acProjTM			= 0x4,	    //  camera projection transform matrix
	acViewProjTM		= 0x8,	    //	combined view/projection matrix
	acWorldViewProjTM	= 0x10,	    //	combined world/view/projection matrix
	acWorldViewTM		= 0x20,	    //	combined world/view matrix

	acLightPos			= 0x40,	    //	light source position
	acLightDir			= 0x80,	    //	light source direction
	acLightPosObjSpace	= 0x100,    //	light source position in object's space
	acLightDirObjSpace	= 0x200,    //	light source direction in object's space

	acLightDiffuse		= 0x400,    //	light diffuse color
	acLightSpecular		= 0x800,    //	light specular color
}; // enum AutoShaderConstant

/*****************************************************************/
/*	Class:	IShaderInstance
/*  Decs:   Shader, instanced with the model-specific parameter set
/*****************************************************************/
class IShaderInstance
{
public:

}; // class IShaderInstance

/*****************************************************************/
/*	Class:	IShader
/*	Desc:	Interface to the object shader, describes most of the 
/*				possible object rendering aspects
/*****************************************************************/
class IShader
{
public:
    virtual const char*             GetName     () const = 0;
    
    virtual int                     GetNLODs    () const = 0;
    virtual int                     GetNPasses  ( int lodID ) = 0;
    virtual const char*             GetLODInfo  ( int lodID ) = 0;

}; // class IShader

#endif // __ISHADER_H__ 