// C2BuildingHudStateCompatV240.cs
// Minimal building HUD state bridge required by the old SelPoint XML HUD file.
// Construction/building production logic is intentionally not enabled here.

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2BuildingHudStateV113LikeOriginal
    {
        public string MdName = string.Empty;
        public bool Ready = true;

        public int Life = 1;
        public int LifeMax = 1;

        public int Places = 0;

        public int Population = 0;
        public int PopulationMax = 0;

        public int Stage = 1;
        public int StageMax = 1;
    }
}
