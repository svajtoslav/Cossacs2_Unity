/*****************************************************************************/
/*	File:	sgReflection.h
/*	Author:	Ruslan Shestopalyuk
/*	Date:	09-18-2003
/*****************************************************************************/
#ifndef __SGREFLECTION_H__
#define __SGREFLECTION_H__

#include "IMediaManager.h"

namespace sg{

struct ReflectedObject
{
    DWORD       m_ModelID;
    Matrix4D    m_TM;

    ReflectedObject( DWORD mdlID, const Matrix4D& tm  ) : m_ModelID( mdlID ), m_TM( tm ) {}
}; // struct ReflectedObject

/*****************************************************************************/
/*	Class:	ReflectionMap
/*	Desc:	Environment reflection
/*****************************************************************************/
class ReflectionMap : public Node, public IReflectionMap
{
	int                             m_ReflID;               //  reflection texture
    int                             m_DepthID;              //  depth buffer surface
    Matrix4D                        m_TextureTM;
			
	int							    m_ReflectionMapSide;	//  reflection texture side
	bool						    m_bUseDepthBuffer;		//  whether z-buffer is used 
	Plane						    m_ReflectionPlane;	
	int							    m_BackdropGridNodes;

	bool						    m_bRenderReflection;
	bool						    m_bRenderBackdrop;
    bool                            m_bInited;
    bool                            m_bDrawDebugInfo;

	std::vector<ReflectedObject>	m_Object;

public:
					    ReflectionMap			();
	virtual void	    Serialize				( OutStream& os ) const;
	virtual void	    Unserialize				( InStream& is );
	virtual void	    Render					();
	virtual void	    Expose					( PropertyMap& pm );
	//virtual void	    SetViewPort				( float x, float y, float w, float h );

	virtual void	    AddObject				( DWORD id, const Matrix4D* objTM );
	virtual void	    CleanObjects			();
	virtual int		    GetReflectionTextureID	() const;
	virtual Matrix4D    GetReflectionTexTM		() const;
	void			    Init					();

	NODE( ReflectionMap, Node, REFL );


protected:
    void                DrawDebugInfo           ();
}; // ReflectionMap

}; // namespace sg


#endif // __SGREFLECTION_H__