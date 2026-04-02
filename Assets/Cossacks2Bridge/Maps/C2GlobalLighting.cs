using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    /// <summary>
    /// Global light state matching the engine-wide SetLight()/LightDX/LightDY/LightDZ/TL0 path from 3DSurf.cpp.
    /// Terrain and any future systems must read light only from here.
    /// </summary>
    public static class C2GlobalLighting
    {
        public static bool IsInitialized { get; private set; }
        public static int SourceDX { get; private set; }
        public static int SourceDY { get; private set; }
        public static int SourceDZ { get; private set; } = 30;

        public static int LightDX { get; private set; }
        public static int LightDY { get; private set; }
        public static int LightDZ { get; private set; } = 255;

        // Mirrors extern int TL0 in 3DSurf.cpp / Factures3D.cpp.
        public static int TL0 { get; set; } = -1;

        public static void SetLightLikeOriginal(int ldx, int ldy, int ldz)
        {
            SourceDX = ldx;
            SourceDY = ldy;
            SourceDZ = ldz;

            int ab = (int)Mathf.Sqrt(ldx * ldx + ldy * ldy + ldz * ldz);
            if (ab < 1)
                ab = 1;

            LightDX = (ldx << 8) / ab;
            LightDY = (ldy << 8) / ab;
            LightDZ = (ldz << 8) / ab;

            TL0 = -1;
            IsInitialized = true;
        }
    }
}
