using System;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2WaterNativeBridge : IDisposable
    {
        public bool IsCreated => false;
        public int SeaLx => 0;
        public int SeaLy => 0;
        public int WaveLx => 0;
        public int WaveLy => 0;
        public byte[] WaterDeep => null;
        public byte[] WaterBright => null;
        public byte[] SourceWaterDeep => null;
        public byte[] SourceWaterBright => null;
        public short[] WaveCurrent => null;
        public uint TickCounter => 0;

        public bool Create(string selectedId, C2WaterData water, int mapSX, int mapSY, out string info)
        {
            info = "native water stripped";
            return false;
        }

        public void Dispose()
        {
        }

        public bool SetViewRect(int mapx, int mapy, int smaplx, int smaply) => false;

        public bool TickAndRefresh(int steps, out string info)
        {
            info = "native water stripped";
            return false;
        }

        public bool RefreshViews() => false;

        public short SampleWave01(float u, float v) => 0;
    }

    internal sealed class C2WaterNativeSurface : IDisposable
    {
        private Mesh _mesh;
        private string _buildInfo = "native water surface stripped";

        public Mesh Mesh => _mesh;
        public string BuildInfo => _buildInfo;

        public bool Build(string selectedId, C2WaterNativeBridge bridge, Func<int, int, int> sampleHeight, int cellStep, float horizontalScale, float verticalScale, float waveAmplitude)
        {
            _mesh = null;
            _buildInfo = "native water surface stripped";
            return false;
        }

        public void Dispose()
        {
            _mesh = null;
        }

        public bool UpdateFromNative(C2WaterNativeBridge bridge) => false;
    }
}
