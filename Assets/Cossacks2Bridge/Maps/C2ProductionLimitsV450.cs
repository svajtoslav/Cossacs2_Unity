using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // UnitsInterface.cpp::SetProduce, Megapolis.cpp::EnumUnits and
    // Build.cpp::ProduceObjLink. Rebuilt with City counters, never per HUD card.
    internal static class C2ProductionLimitsV450
    {
        private static readonly Dictionary<string, (string Counted, int Max)> Locks =
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<(int Nation, string Id), int> Amounts = new Dictionary<(int, string), int>();
        private static readonly int[] Depots = new int[8];
        private static readonly int[,] Guns = new int[8, 4];
        private static int autoFoodLimit = -1;

        internal static void RegisterLock(string id, string counted, int max) => Locks[id] = (counted, Math.Max(0, max));
        internal static void BeginCount()
        {
            Amounts.Clear(); Array.Clear(Depots, 0, Depots.Length); Array.Clear(Guns, 0, Guns.Length);
        }
        internal static void Count(int nation, string id, C2OriginalProduceCatalogV13.C2MdIconInfoV13 md, bool ready)
        {
            if ((uint)nation >= 7) return;
            if (!string.IsNullOrEmpty(id))
            {
                var key = (nation, id.ToUpperInvariant());
                Amounts.TryGetValue(key, out int amount); Amounts[key] = amount + 1;
            }
            if (!ready) return;
            if (md.ArtDepotV450) Depots[nation]++;
            if (md.ArtSetV450 > 0 && md.ArtSetV450 <= 4) Guns[nation, md.ArtSetV450 - 1]++;
        }
        internal static int Amount(int nation, string id) =>
            Amounts.TryGetValue((nation, (id ?? "").ToUpperInvariant()), out int amount) ? amount : 0;

        internal static bool Allows(int nation, string id, out string reason)
        {
            reason = "";
            if ((uint)nation >= 7) { reason = "Недопустимый владелец"; return false; }
            if (id != null && Locks.TryGetValue(id, out var rule) && Amount(nation, rule.Counted) >= rule.Max)
            {
                reason = "Достигнут лимит: " + Amount(nation, rule.Counted) + " / " + rule.Max;
                return false;
            }
            return ArtilleryAllows(nation, id, out reason);
        }
        internal static bool ArtilleryAllows(int nation, string id, out string reason)
        {
            reason = "";
            if ((uint)nation >= 7) return false;
            var md = C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(id);
            if (md.ArtSetV450 <= 0 || md.ArtSetV450 > 4) return true;
            int current = Guns[nation, md.ArtSetV450 - 1], max = Depots[nation] * md.MaxInDepotV450;
            if (current < max) return true;
            reason = "Лимит артдепо: " + current + " / " + max;
            return false;
        }
        internal static bool AutoFoodAllows(int nation)
        {
            if (autoFoodLimit < 0)
            {
                // EngineSettings.xml uses a nameless root, so read only this scalar.
                autoFoodLimit = 200;
                foreach (string root in C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal())
                {
                    string path = Path.Combine(root, "EngineSettings.xml");
                    if (!File.Exists(path)) continue;
                    var match = Regex.Match(File.ReadAllText(path), @"<AutoProduceFoodLimit>\s*(\d+)\s*</AutoProduceFoodLimit>");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int value)) { autoFoodLimit = value; break; }
                }
            }
            return C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(nation, 3) >= autoFoodLimit;
        }
    }
}
