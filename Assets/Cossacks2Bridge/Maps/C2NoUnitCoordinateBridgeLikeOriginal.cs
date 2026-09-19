using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        public bool C2NoUnitWorldToOriginalPixelLikeOriginal(Vector3 world, out float x, out float y)
        {
            x = 0.0f;
            y = 0.0f;
            if (_map == null) return false;

            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            return C2OriginalWorldCoordinatesV371LikeOriginal.ToOriginal(
                world.x, world.z, kernel.BackingStepXWorld, kernel.BackingStepZWorld,
                kernel.CenterX, kernel.CenterZ, WorldZSign, out x, out y);
        }

        public Vector3 C2NoUnitOriginalPixelToWorldLikeOriginal(float x, float y)
        {
            return WallOriginalXYToWorldV1LikeOriginal(x, y, 0.0f);
        }

        internal float C2OriginalSphereRadiusToWorldV1LikeOriginal(float originalRadius)
        {
            if (_map == null) return Mathf.Max(0.0f, originalRadius);
            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            float xScale = Mathf.Abs(kernel.BackingStepXWorld) / 32.0f;
            float zScale = Mathf.Abs(kernel.BackingStepZWorld) / 32.0f;
            float heightScale = Mathf.Abs(kernel.HeightScale);
            float worldPerOriginalPixel = Mathf.Max(xScale, Mathf.Max(zScale, heightScale));
            return Mathf.Max(0.0f, originalRadius) * Mathf.Max(0.000001f, worldPerOriginalPixel);
        }

        internal int C2OriginalFogViewScaleV1LikeOriginal()
        {
            return 1 << Mathf.Clamp(GetStrictScShiftLikeOriginal(), 0, 2);
        }

        internal bool C2OriginalCameraPlaneScreenToPixelV1LikeOriginal(
            Camera cam,
            Vector2 screen,
            out float x,
            out float y)
        {
            x = 0.0f;
            y = 0.0f;
            if (cam == null || _map == null) return false;
            Plane originalXOyPlane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(screen);
            float enter;
            if (!originalXOyPlane.Raycast(ray, out enter) || enter < 0.0f) return false;
            return C2NoUnitWorldToOriginalPixelLikeOriginal(ray.GetPoint(enter), out x, out y);
        }

        internal bool C2OriginalFogMapConfigV1LikeOriginal(out int addsh, out int vertInLine, out int maxTH)
        {
            addsh = 1;
            vertInLine = 0;
            maxTH = 0;
            ParsedMap map = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (map == null) map = _map;
            if (map == null) return false;

            addsh = Mathf.Clamp(map.Addsh, 1, 3);
            vertInLine = Mathf.Max(0, map.VertInLine);
            maxTH = Mathf.Max(0, map.MaxTH);
            return vertInLine > 1 && maxTH > 1;
        }

        internal int C2OriginalFogTerrainHeightV1LikeOriginal(int originalPixelX, int originalPixelY)
        {
            return GetStrictTotalHeightLikeOriginal(originalPixelX, originalPixelY);
        }

        internal bool C2OriginalFogShaderMappingV1LikeOriginal(out Vector4 map, out Vector4 map2)
        {
            map = Vector4.zero;
            map2 = Vector4.zero;
            if (_map == null) return false;
            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            if (Mathf.Abs(kernel.BackingStepXWorld) < 0.000001f ||
                Mathf.Abs(kernel.BackingStepZWorld) < 0.000001f ||
                Mathf.Abs(kernel.HeightScale) < 0.000001f ||
                Mathf.Abs(WorldZSign) < 0.000001f)
                return false;
            map = new Vector4(
                kernel.CenterX,
                kernel.CenterZ,
                1.0f / kernel.BackingStepXWorld,
                1.0f / kernel.BackingStepZWorld);
            map2 = new Vector4(
                WorldZSign,
                // Constant original-map -> terrain-origin translation (V371).
                kernel.BackingOddColumnOffsetZWorld,
                1.0f / kernel.HeightScale,
                1.0f);
            return true;
        }
    }
}
