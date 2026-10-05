using System;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Build.cpp::ProduceObjLink; Ddex1.cpp Harch; NewMon.cpp HUNGPROB.
    // Unity simulation is 25 ticks/s at native GameSpeed 256.
    internal static class C2PopulationEconomyV449
    {
        private static readonly long[] Remainders = new long[8];
        private static readonly bool[] Hungry = new bool[8];
        private static int lastTick = -1, resource = 3, perUnit = 3;
        private static uint randomState = 449;
        private static bool loaded;
        internal static void Reset()
        {
            Array.Clear(Remainders, 0, 8); Array.Clear(Hungry, 0, 8);
            lastTick = -1; randomState = 449;
        }
        internal static bool NoFarm(string id) => C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(id).NoFarm;
        internal static bool CanTrain(int nation, string id)
        {
            if ((uint)nation >= 7) return false;
            if (NoFarm(id)) return true;
            return C2NationCityRuntimeV384ALikeOriginal.GetCurrentUnitsLikeOriginal(nation) <
                C2NationCityRuntimeV384ALikeOriginal.GetMaxUnitsLikeOriginal(nation);
        }
        private static void Load()
        {
            if (loaded) return; loaded = true;
            foreach (string root in C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal())
            {
                string path = Path.Combine(root, "nres.dat");
                if (!File.Exists(path)) continue;
                foreach (string raw in File.ReadLines(path))
                {
                    string[] p = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(raw).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 3 && p[0] == "RES/UNIT" && int.TryParse(p[2], out int amount))
                    { int r = C2ResourceStorageV445.ResourceId(p[1]); if (r >= 0) { resource = r; perUnit = Math.Max(0, amount); return; } }
                }
            }
        }
        internal static bool IsHungry(int nation) => (uint)nation < 7 && Hungry[nation];
        internal static void Tick(int tick)
        {
            if (lastTick < 0 || tick < lastTick) { lastTick = tick; return; }
            int elapsed = tick - lastTick;
            if (elapsed <= 0) return;
            lastTick = tick;
            Load();
            // City refreshes population at its existing 0.2 s cadence. Do not
            // rescan the entire world at 25 Hz merely to charge food.
            // Check hunger each native tick, including during low frame rates.
            // Do not charge elapsed wall time while the simulation is paused.
            for (int t = 0; t < elapsed; t++)
            {
                bool checkDeaths = false;
                for (int n = 0; n < 7; n++)
                {
                    int stock = C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(n, resource);
                    Remainders[n] += (long)C2NationCityRuntimeV384ALikeOriginal.GetCurrentUnitsLikeOriginal(n) * perUnit;
                    int due = (int)Math.Min(int.MaxValue, Remainders[n] / 2000);
                    if (due > 0)
                    {
                        C2NationResourceEconomyV348LikeOriginal.AddResourceLikeOriginal(n, resource, -Math.Min(stock, due), "army_upkeep");
                        Remainders[n] = stock > due ? Math.Max(1, Remainders[n] - (long)due * 2000) : 0;
                    }
                    // Ddex1.cpp replenishes Harch from FoodID after a delivery.
                    // NewMon.cpp tests Harch, NOT the stock: an empty granary
                    // only triggers deaths on the ticks when this counter is zero.
                    if (Remainders[n] == 0 && C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(n, 3) > 0)
                        Remainders[n] = 1;
                    Hungry[n] = Remainders[n] == 0;
                    checkDeaths |= Hungry[n] && C2NationCityRuntimeV384ALikeOriginal.HasHungryUnitsV449(n);
                }
                if (!checkDeaths) continue;
                foreach (var u in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
                {
                    if (u == null || u.IsDeadLikeOriginal || u.LifeLikeOriginal <= 0 || !IsHungry(u.Nation)) continue;
                    var md = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u);
                    if (md.Building || md.NotHungryV449) continue;
                    // Independent random stream: presentation and framerate cannot
                    // change the probability, and starvation never awards a kill.
                    randomState = unchecked(randomState * 1664525u + 1013904223u);
                    if ((randomState >> 17) <= 50)
                        C2CombatCoreV408LikeOriginal.KillUnitV408LikeOriginal(u, null, "NewMon.cpp::HUNGPROB");
                }
            }
        }
    }
}
