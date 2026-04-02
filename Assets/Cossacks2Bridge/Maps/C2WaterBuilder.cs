using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2WaterBuilder
    {
        public static bool TryParseSea2Payload(byte[] payload, C2WaterData water, out string info)
        {
            info = "SEA2 stripped";
            return false;
        }

        public static bool TryParseRiv1Payload(byte[] payload, C2WaterData water, out string info)
        {
            info = "RIV1 stripped";
            return false;
        }

        public static bool TryParseRgbwPayload(byte[] payload, C2WaterData water, out string info)
        {
            info = "RGBW stripped";
            return false;
        }

        public static Texture2D TryBuildDebugOverlayTexture(string selectedId, C2WaterData water, out string info)
        {
            info = "water debug overlay stripped";
            return null;
        }

        public static Mesh TryBuildWaterHexMesh(string selectedId, Mesh terrainMesh, C2WaterData water, System.Func<int, int, int> getHeight, float worldZSign, out string info)
        {
            info = "water mesh stripped";
            return null;
        }
    }
}
