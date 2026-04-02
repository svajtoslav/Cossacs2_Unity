using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2WaterData
    {
        public int SeaLx;
        public int SeaLy;
        public byte[] WaterDeep;
        public byte[] WaterBright;
        public int SeaDeepNonZeroCount;
        public int SeaBrightNonZeroCount;
        public int RgbPayloadBytes;
        public int RgbSize;
        public byte[] Red;
        public byte[] Green;
        public byte[] Blue;
        public short[] Vx;
        public short[] Vy;
        public short[] Level;
        public int RivPayloadBytes;
        public int RivSize;
        public byte[] RivDir;
        public byte[] RivVol;
        public int RivDirNonZeroCount;
        public int RivVolNonZeroCount;

        public bool HasRenderableWater => false;
        public bool HasSea2Payload => false;
        public bool HasRgbwPayload => false;
        public bool HasRiv1Payload => false;

        public string BuildSummary() => "water-stripped";

        public byte GetWaterDeep(int x, int y) => 0;
        public byte GetWaterBright(int x, int y) => 0;
        public Color32 GetRgbColor01(float u, float v) => new Color32(128, 128, 128, 255);
    }
}
