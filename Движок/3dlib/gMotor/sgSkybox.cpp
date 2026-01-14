#include "stdafx.h"
#include "sgSkybox.h"

BEGIN_NAMESPACE(sg)

/*****************************************************************************/
/*	Skybox implementation
/*****************************************************************************/
bool Skybox::s_bFrozen = false;

Skybox::Skybox()
{
	topColor = bottomColor = 0xFFFFFFFF;
	bNeedReassignColors = false;
}

void Skybox::Render()
{
	if (s_bFrozen) return;

	if (bNeedReassignColors) ReassignColors();

	Matrix4D skyTM( tm );
	
	//  place skybox center into camera position
	BaseCamera* pCam = BaseCamera::GetActiveCamera();
	if (pCam)
	{
		Vector3D pos;
		pCam->GetPos( pos );
		skyTM.setTranslation( pos );
	}

	s_TMStack.Push( skyTM );
	m_WorldTM = s_TMStack.Top();

	Node::Render();
	s_TMStack.Pop();

} // Skybox::Render

void Skybox::Serialize( OutStream& os ) const
{
	Parent::Serialize( os );
	os << topColor << bottomColor;
} // Skybox::Serialize

void Skybox::Unserialize( InStream& is )
{
	Parent::Unserialize( is );
	is >> topColor >> bottomColor;
	bNeedReassignColors = true;
} // Skybox::Unserialize

void Skybox::ReassignColors()
{
	/*Node::Iterator it( this, Geometry::FnFilter );
	while (it)
	{
		Node* pNode = (Node*)it;
		Geometry* pGeom = (Geometry*)pNode;
		BaseMesh& bm = pGeom->GetPrimitive();
		AABoundBox aabb;
		bm.GetAABB( aabb );
		float zMin = aabb.minv.z;
		float zMax = aabb.maxv.z;

		VertexIterator vit;
		vit << bm;
		
		while (vit)
		{
			Vector3D& vec = vit;
			float grad = (vec.z - zMin)/(zMax - zMin);
			vit.diffuse() = ColorValue::Gradient( bottomColor, topColor, grad );
			++vit;
		}

		++it;	
	}*/

	bNeedReassignColors = false;
} // Skybox::ReassignColors

END_NAMESPACE(sg)
