using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private static void ExpandTerrainMeshBoundsLikeAdapted(Mesh mesh)
        {
            if (mesh == null)
                return;

            Bounds bounds = mesh.bounds;

            if (!IsFiniteLikeAdapted(bounds.center) || !IsFiniteLikeAdapted(bounds.extents))
            {
                mesh.RecalculateBounds();
                bounds = mesh.bounds;
            }

            Vector3 ext = bounds.extents;
            const float padXZ = 4096.0f;
            const float padY = 2048.0f;

            ext.x = Mathf.Max(ext.x + padXZ, padXZ);
            ext.y = Mathf.Max(ext.y + padY, padY);
            ext.z = Mathf.Max(ext.z + padXZ, padXZ);

            bounds.extents = ext;
            mesh.bounds = bounds;
        }

        private static bool IsFiniteLikeAdapted(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                     float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                     float.IsNaN(v.z) || float.IsInfinity(v.z));
        }
    }
}
