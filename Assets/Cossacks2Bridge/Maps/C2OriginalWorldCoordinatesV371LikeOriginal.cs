using System;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // RealX/16 and RealY/16 are continuous map coordinates, not THMap column/row
    // indices. GetTriY is row*32 - 16 for odd columns; the Unity terrain uses
    // row*stepZ + stepZ/2 for even columns. Their origins differ by a CONSTANT
    // half cell. Applying that offset conditionally would make straight rows zigzag.
    internal static class C2OriginalWorldCoordinatesV371LikeOriginal
    {
        internal static void ToWorld(float x, float y, float stepX, float stepZ,
            float centerX, float centerZ, float zSign, out float worldX, out float worldZ)
        {
            worldX = x * (stepX / 32.0f) - centerX;
            worldZ = ((y + 16.0f) * (stepZ / 32.0f) - centerZ) * zSign;
        }

        internal static bool ToOriginal(float worldX, float worldZ, float stepX, float stepZ,
            float centerX, float centerZ, float zSign, out float x, out float y)
        {
            x = 0.0f; y = 0.0f;
            if (Math.Abs(stepX) < 0.000001f || Math.Abs(stepZ) < 0.000001f || Math.Abs(zSign) < 0.000001f)
                return false;
            x = (worldX + centerX) * 32.0f / stepX;
            y = (worldZ / zSign + centerZ) * 32.0f / stepZ - 16.0f;
            return true;
        }
    }
}
