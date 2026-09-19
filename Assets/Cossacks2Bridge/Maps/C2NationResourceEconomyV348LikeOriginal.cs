// C2NationResourceEconomyV348LikeOriginal.cs
// Dense URESRC[8][8]-style runtime resource ledger from Cossacks II Nature.cpp/MapDiscr.h.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public static class C2NationResourceEconomyV348LikeOriginal
    {
        public const int NationCount = 8;
        public const int ResourceCount = 8;
        public const int EditorStartingResourceAmount = 5000000;

        private static readonly int[,] Amounts = new int[NationCount, ResourceCount];
        private static readonly int[,] TotalsGathered = new int[NationCount, ResourceCount];
        private static readonly Dictionary<string, int[]> PriceCache =
            new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLikeOriginal()
        {
            Array.Clear(Amounts, 0, Amounts.Length);
            Array.Clear(TotalsGathered, 0, TotalsGathered.Length);
            PriceCache.Clear();
            _initialized = false;
        }

        public static void EnsureInitializedLikeOriginal()
        {
            if (_initialized) return;
            _initialized = true;
            // CurrentMapOptions.h defaults remain in games; the editor has the
            // user-requested test stock per player. Switching colors never refills it.
            int initial = C2BattleTerrainMode.EditorTestModeLikeOriginal ? EditorStartingResourceAmount : 5000;
            for (int nation = 0; nation < NationCount; nation++)
                for (int resource = 0; resource < 6; resource++)
                    Amounts[nation, resource] = initial;
        }

        public static void BeginMapLikeOriginal()
        {
            ResetLikeOriginal();
            EnsureInitializedLikeOriginal();
            C2NationCityRuntimeV384ALikeOriginal.BeginMapLikeOriginal();
        }

        public static int GetCurrentUnitsLikeOriginal(int nation)
        {
            return C2NationCityRuntimeV384ALikeOriginal.GetCurrentUnitsLikeOriginal(nation);
        }

        public static int GetMaxUnitsLikeOriginal(int nation)
        {
            return C2NationCityRuntimeV384ALikeOriginal.GetMaxUnitsLikeOriginal(nation);
        }

        public static int GetResourcePeasantsLikeOriginal(int nation, int resource)
        {
            return C2NationCityRuntimeV384ALikeOriginal.GetResourcePeasantsLikeOriginal(nation, resource);
        }

        public static int GetResourceLikeOriginal(int nation, int resource)
        {
            EnsureInitializedLikeOriginal();
            if ((uint)nation >= (uint)NationCount || (uint)resource >= (uint)ResourceCount) return 0;
            return Amounts[nation, resource];
        }

        public static void AddResourceLikeOriginal(int nation, int resource, int amount, string source)
        {
            EnsureInitializedLikeOriginal();
            if ((uint)nation >= (uint)NationCount || (uint)resource >= (uint)ResourceCount || amount == 0) return;
            long next = (long)Amounts[nation, resource] + amount;
            Amounts[nation, resource] = (int)Math.Max(0L, Math.Min((long)int.MaxValue, next));
            if (amount > 0) TotalsGathered[nation, resource] += amount;
        }

        public static bool TryGetUnitPriceLikeOriginal(string mdName, out int[] price, out string audit)
        {
            string key = StripNationSuffixLikeOriginal(mdName);
            if (PriceCache.TryGetValue(key, out int[] cached))
            {
                price = (int[])cached.Clone();
                audit = "cached PRICE md='" + key + "'";
                return true;
            }

            price = new int[ResourceCount];
            string path = FindMdPathLikeOriginal(key);
            if (string.IsNullOrEmpty(path))
            {
                audit = "md_not_found '" + key + "'";
                return false;
            }

            string[] lines = ReadLinesLikeOriginal(path);
            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                string line = CleanLineLikeOriginal(lines[i]);
                string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 2 || !string.Equals(p[0], "PRICE", StringComparison.OrdinalIgnoreCase)) continue;
                int count;
                if (!int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) continue;
                for (int n = 0, at = 2; n < count && at + 1 < p.Length; n++, at += 2)
                {
                    int resource = ResourceIdLikeOriginal(p[at]);
                    if (resource >= 0 && int.TryParse(p[at + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                        price[resource] = Mathf.Max(0, amount);
                }
                PriceCache[key] = (int[])price.Clone();
                audit = "PRICE md='" + key + "' path='" + path + "' values=" + FormatCostLikeOriginal(price);
                return true;
            }
            audit = "PRICE_missing md='" + key + "' path='" + path + "'";
            return false;
        }

        public static bool CanAffordLikeOriginal(int nation, int[] price, out string missing)
        {
            EnsureInitializedLikeOriginal();
            missing = string.Empty;
            if ((uint)nation >= (uint)NationCount || price == null) return false;
            var names = new List<string>();
            for (int i = 0; i < Mathf.Min(ResourceCount, price.Length); i++)
                if (price[i] > Amounts[nation, i]) names.Add(ResourceNameLikeOriginal(i));
            missing = string.Join(", ", names.ToArray());
            return names.Count == 0;
        }

        public static bool ApplyCostLikeOriginal(int nation, int[] price, string source, out string audit)
        {
            if (!CanAffordLikeOriginal(nation, price, out string missing))
            {
                audit = "not_enough=" + missing;
                return false;
            }
            for (int i = 0; i < Mathf.Min(ResourceCount, price.Length); i++)
                Amounts[nation, i] -= Mathf.Max(0, price[i]);
            audit = "ApplyCost " + FormatCostLikeOriginal(price) + " source='" + (source ?? string.Empty) + "'";
            return true;
        }

        public static void RestoreCostAfterRejectedCreationLikeOriginal(int nation, int[] price, string source)
        {
            EnsureInitializedLikeOriginal();
            if ((uint)nation >= (uint)NationCount || price == null) return;
            for (int i = 0; i < Mathf.Min(ResourceCount, price.Length); i++)
            {
                long restored = (long)Amounts[nation, i] + Mathf.Max(0, price[i]);
                Amounts[nation, i] = (int)Math.Min((long)int.MaxValue, restored);
            }
        }

        public static string FormatPlayerResourcesLikeOriginal(int nation)
        {
            EnsureInitializedLikeOriginal();
            return "Дерево " + GetResourceLikeOriginal(nation, 0).ToString(CultureInfo.InvariantCulture) +
                   "   Золото " + GetResourceLikeOriginal(nation, 1).ToString(CultureInfo.InvariantCulture) +
                   "   Камень " + GetResourceLikeOriginal(nation, 2).ToString(CultureInfo.InvariantCulture) +
                   "   Еда " + GetResourceLikeOriginal(nation, 3).ToString(CultureInfo.InvariantCulture) +
                   "   Железо " + GetResourceLikeOriginal(nation, 4).ToString(CultureInfo.InvariantCulture) +
                   "   Уголь " + GetResourceLikeOriginal(nation, 5).ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatCostLikeOriginal(int[] cost)
        {
            var parts = new List<string>();
            for (int i = 0; cost != null && i < Mathf.Min(6, cost.Length); i++)
                if (cost[i] > 0) parts.Add(ResourceNameLikeOriginal(i) + "=" + cost[i].ToString(CultureInfo.InvariantCulture));
            return string.Join(" ", parts.ToArray());
        }

        private static string ResourceNameLikeOriginal(int id)
        {
            string[] names = { "WOOD", "GOLD", "STONE", "FOOD", "IRON", "COAL", "RES6", "RES7" };
            return (uint)id < names.Length ? names[id] : "RES" + id.ToString(CultureInfo.InvariantCulture);
        }

        private static int ResourceIdLikeOriginal(string value)
        {
            switch ((value ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "WOOD": return 0;
                case "GOLD": return 1;
                case "STONE": return 2;
                case "FOOD": return 3;
                case "IRON": return 4;
                case "COAL": return 5;
                default: return -1;
            }
        }

        private static string StripNationSuffixLikeOriginal(string value)
        {
            string s = (value ?? string.Empty).Trim();
            int p = s.IndexOf('(');
            if (p > 0) s = s.Substring(0, p);
            if (s.EndsWith(".MD", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 3);
            return s.Trim();
        }

        private static string FindMdPathLikeOriginal(string mdName)
        {
            string[] roots =
            {
                @"C:\GSC Game World\Cossacks II\Data\UnitsMD",
                @"C:\GSC Game World\Cossacks II\Data",
                Path.Combine(Application.dataPath, "Resources", "UnitsMD"),
                Path.Combine(Application.dataPath, "Resources")
            };
            for (int i = 0; i < roots.Length; i++)
            {
                string[] candidates = { Path.Combine(roots[i], mdName + ".MD"), Path.Combine(roots[i], mdName + ".md") };
                for (int n = 0; n < candidates.Length; n++) if (File.Exists(candidates[n])) return candidates[n];
            }
            return null;
        }

        private static string[] ReadLinesLikeOriginal(string path)
        {
            try { return File.ReadAllLines(path, Encoding.GetEncoding(866)); }
            catch { try { return File.ReadAllLines(path, Encoding.GetEncoding(1251)); } catch { return File.ReadAllLines(path); } }
        }

        private static string CleanLineLikeOriginal(string line)
        {
            string s = line ?? string.Empty;
            int c = s.IndexOf("//", StringComparison.Ordinal);
            return (c >= 0 ? s.Substring(0, c) : s).Trim();
        }
    }
}
