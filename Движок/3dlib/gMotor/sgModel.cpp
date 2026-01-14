/*****************************************************************************/
/*	File:	sgModel.cpp
/*	Desc:	
/*	Author:	Ruslan Shestopalyuk
/*	Date:	15.04.2003
/*****************************************************************************/
#include "stdafx.h"
#include "sgNodePool.h"
#include "sgNode.h"
#include "sgMovable.h"
#include "sgDummy.h"
#include "sgCamera.h"
#include "sgModel.h"

#ifndef _INLINES
#include "sgModel.inl"
#endif // !_INLINES

BEGIN_NAMESPACE( sg )

/*****************************************************************************/
/*	ModelManager implementation
/*****************************************************************************/
ModelManager::ModelManager()
{
	SetName( "ModelManager" );
}

void ModelManager::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void ModelManager::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

void ModelManager::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "ModelManager", this );
}

void ModelManager::AddModel( const char* name, Node* pNode )
{
	ModelFile* pModel = AddChild<ModelFile>( name );
    pModel->SetAABB( CalculateAABB( pNode ) );
	pModel->AddChild( pNode );
} // ModelManager::AddModel

/*****************************************************************************/
/*	AnimationManager implementation
/*****************************************************************************/
AnimationManager::AnimationManager()
{
	SetName( "AnimationManager" );
}

void AnimationManager::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
}

void AnimationManager::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
}

void AnimationManager::Expose( PropertyMap& pm )
{
	pm.start<Parent>( "AnimationManager", this );
}

void AnimationManager::AddAnimation( const char* name, Node* pNode )
{
	AnimationFile* pAnim = AddChild<AnimationFile>( name );
	pAnim->AddChild( pNode );
} // ModelManager::AddModel


END_NAMESPACE( sg )