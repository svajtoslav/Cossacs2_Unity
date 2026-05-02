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
        public int SeaDeepAbove128Count;
        public int SeaDeepShallowCount;

        public bool HasRenderableWater => HasSea2Payload && SeaDeepAbove128Count > 0;
        public bool HasSea2Payload => SeaLx > 0 && SeaLy > 0 && WaterDeep != null && WaterBright != null && WaterDeep.Length >= SeaLx * SeaLy && WaterBright.Length >= SeaLx * SeaLy;
        public bool HasRgbwPayload => RgbPayloadBytes > 0;
        public bool HasRiv1Payload => RivPayloadBytes > 0 && RivDir != null && RivVol != null;

        public string BuildSummary()
        {
            if (!HasSea2Payload && !HasRgbwPayload && !HasRiv1Payload)
                return "water-empty";

            return $"SEA2={SeaLx}x{SeaLy} deepNonZero={SeaDeepNonZeroCount} deepAbove128={SeaDeepAbove128Count} shallow={SeaDeepShallowCount} brightNonZero={SeaBrightNonZeroCount} RGBWBytes={RgbPayloadBytes} RGBWSize={RgbSize} RIV1Bytes={RivPayloadBytes} RIVSize={RivSize} rivDirNonZero={RivDirNonZeroCount} rivVolNonZero={RivVolNonZeroCount}";
        }

        public byte GetWaterDeep(int x, int y)
        {
            if (!HasSea2Payload || x < 0 || y < 0 || x >= SeaLx || y >= SeaLy)
                return 0;
            return WaterDeep[y * SeaLx + x];
        }

        public byte GetWaterBright(int x, int y)
        {
            if (!HasSea2Payload || x < 0 || y < 0 || x >= SeaLx || y >= SeaLy)
                return 0;
            return WaterBright[y * SeaLx + x];
        }

        public Color32 GetRgbColor01(float u, float v)
        {
            if (RgbSize <= 0 || Red == null || Green == null || Blue == null)
                return new Color32(128, 128, 128, 255);

            int count = RgbSize * RgbSize;
            if (Red.Length < count || Green.Length < count || Blue.Length < count)
                return new Color32(128, 128, 128, 255);

            int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Repeat(u, 1.0f) * (RgbSize - 1)), 0, RgbSize - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Repeat(v, 1.0f) * (RgbSize - 1)), 0, RgbSize - 1);
            int index = y * RgbSize + x;
            return new Color32(Red[index], Green[index], Blue[index], 255);
        }
    }
}
