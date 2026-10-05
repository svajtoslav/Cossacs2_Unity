using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    /// <summary>
    /// External map-load lighting path matching LoadSave.cpp: SetLight(0,20,30) after map load.
    /// Terrain must only read global light state and never initialize it internally.
    /// </summary>
    public static class C2MapLoadLighting
    {
        public static void ApplyMapLoadDefaultsLikeOriginal()
        {
            C2GlobalLighting.SetLightLikeOriginal(0, 20, 30);
        }


        public static void ApplyBattleMapLoadDefaultsLikeOriginal()
        {
            ApplyMapLoadDefaultsLikeOriginal();
        }
    }
}
