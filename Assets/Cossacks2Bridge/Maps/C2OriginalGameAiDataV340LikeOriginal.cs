using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Runtime reader for the AI shipped with Cossacks II. The old engine did
    // not own these decisions: ai_router.cpp selected Data/AI/*.ai.xml and
    // BrigadeAI::Init loaded Data/AI/BrigadeAI/Rules.ai, BattleRules.ai and
    // every additional .sia file. Keep the data external and executable so a
    // modded Data folder changes behaviour without recompiling Unity code.
    internal static class C2OriginalGameAiDataV340LikeOriginal
    {
        private sealed class BrigadeRule
        {
            public string File = string.Empty;
            public string Section = string.Empty;
            public readonly HashSet<string> UnitIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public string Condition = string.Empty;
            public string Action = string.Empty;
            public int Priority;
        }

        private sealed class SettlementPriorityBand
        {
            public float MinTime;
            public float MaxTime = float.PositiveInfinity;
            public readonly Dictionary<int, int> PriorityByResource = new Dictionary<int, int>();
        }

        private static readonly List<BrigadeRule> BrigadeRules = new List<BrigadeRule>();
        private static readonly Dictionary<string, List<string>> Routes =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<SettlementPriorityBand>> SettlementBands =
            new Dictionary<string, List<SettlementPriorityBand>>(StringComparer.OrdinalIgnoreCase);
        private static bool _loaded;
        private static string _audit = "not_loaded";

        internal static string AuditLikeOriginal
        {
            get { EnsureLoadedLikeOriginal(); return _audit; }
        }

        internal static void EnsureLoadedLikeOriginal()
        {
            if (_loaded) return;
            _loaded = true;
            string root = ResolveDataRootLikeOriginal();
            if (string.IsNullOrEmpty(root))
            {
                _audit = "data_root_not_found";
                return;
            }
            string aiRoot = Path.Combine(root, "AI");
            ParseRouterLikeOriginal(Path.Combine(aiRoot, "router.xml"));
            foreach (KeyValuePair<string, List<string>> route in Routes)
                for (int i = 0; i < route.Value.Count; i++)
                    ParseNationSettlementPrioritiesLikeOriginal(route.Key, Path.Combine(root, route.Value[i].Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));

            string brigadeRoot = Path.Combine(aiRoot, "BrigadeAI");
            ParseBrigadeRulesLikeOriginal(Path.Combine(brigadeRoot, "Rules.ai"));
            ParseBrigadeRulesLikeOriginal(Path.Combine(brigadeRoot, "BattleRules.ai"));
            if (Directory.Exists(brigadeRoot))
            {
                string[] sia = Directory.GetFiles(brigadeRoot, "*.sia", SearchOption.TopDirectoryOnly);
                Array.Sort(sia, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < sia.Length; i++) ParseBrigadeRulesLikeOriginal(sia[i]);
            }
            _audit = "data='" + root + "' routes=" + Routes.Count.ToString(CultureInfo.InvariantCulture) +
                     " nationBands=" + SettlementBands.Count.ToString(CultureInfo.InvariantCulture) +
                     " brigadeRules=" + BrigadeRules.Count.ToString(CultureInfo.InvariantCulture);
            Debug.Log("[C2:GAME AI DATA V340] " + _audit);
        }

        internal static int SettlementPriorityLikeOriginal(string nation, int resourceType, float gamingTimeSeconds)
        {
            EnsureLoadedLikeOriginal();
            List<SettlementPriorityBand> bands;
            if (string.IsNullOrWhiteSpace(nation) || !SettlementBands.TryGetValue(nation.Trim(), out bands)) return 0;
            int result = 0;
            for (int i = 0; i < bands.Count; i++)
            {
                SettlementPriorityBand band = bands[i];
                int value;
                if (gamingTimeSeconds >= band.MinTime && gamingTimeSeconds < band.MaxTime &&
                    band.PriorityByResource.TryGetValue(resourceType, out value))
                    result = value;
            }
            return result;
        }

        internal static bool TryChoosePolkWeaponModeLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float distanceOriginalPixels,
            int unitsAmount,
            out int weaponMode,
            out string audit)
        {
            weaponMode = -1;
            audit = "no_matching_brigade_rule";
            EnsureLoadedLikeOriginal();
            if (unit == null) return false;
            string member = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(unit);
            if (string.IsNullOrWhiteSpace(member)) member = unit.SourceMonsterId ?? string.Empty;
            float morale, maxMorale;
            C2CombatRuntimeV334LikeOriginal.TryGetMoraleSnapshotLikeOriginal(unit, out morale, out maxMorale);
            float tired = C2CombatRuntimeV334LikeOriginal.GetFormationTiringRemainingLikeOriginal(unit);
            BrigadeRule best = null;
            int bestMode = -1;
            for (int i = 0; i < BrigadeRules.Count; i++)
            {
                BrigadeRule rule = BrigadeRules[i];
                if (!rule.UnitIds.Contains(member) || !EvaluateKnownConditionsLikeOriginal(
                        rule.Condition, distanceOriginalPixels, unitsAmount, morale, tired))
                    continue;
                int mode = ActionWeaponModeLikeOriginal(rule.Action);
                if (mode < 0) continue;
                if (best == null || rule.Priority > best.Priority)
                {
                    best = rule;
                    bestMode = mode;
                }
            }
            if (best == null) return false;
            weaponMode = bestMode;
            audit = Path.GetFileName(best.File) + "#" + best.Section + " action=" + best.Action +
                    " priority=" + best.Priority.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static int ActionWeaponModeLikeOriginal(string action)
        {
            if (string.Equals(action, "MeleeAttack", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(action, "Fire", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(action, "EnableFire", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(action, "AttackMT", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(action, "ThrowGrenade", StringComparison.OrdinalIgnoreCase)) return 2;
            return -1;
        }

        private static bool EvaluateKnownConditionsLikeOriginal(
            string condition, float distance, int amount, float morale, float tired)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            MatchCollection matches = Regex.Matches(condition,
                @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?<op><=|>=|=|<|>)\s*(?<value>-?\d+)");
            bool inspected = false;
            for (int i = 0; i < matches.Count; i++)
            {
                string name = matches[i].Groups["name"].Value;
                float actual;
                if (name.Equals("DistToMT", StringComparison.OrdinalIgnoreCase)) actual = distance;
                else if (name.Equals("UnitsAmount", StringComparison.OrdinalIgnoreCase)) actual = amount;
                else if (name.Equals("Moral", StringComparison.OrdinalIgnoreCase)) actual = morale;
                else if (name.Equals("GetTired", StringComparison.OrdinalIgnoreCase)) actual = tired;
                else continue; // state variables are owned by the formation runtime
                inspected = true;
                float wanted;
                if (!float.TryParse(matches[i].Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out wanted)) continue;
                string op = matches[i].Groups["op"].Value;
                if (op == "<" && !(actual < wanted) || op == ">" && !(actual > wanted) ||
                    op == "<=" && !(actual <= wanted) || op == ">=" && !(actual >= wanted) ||
                    op == "=" && Mathf.Abs(actual - wanted) > 0.001f)
                    return false;
            }
            return inspected;
        }

        private static void ParseRouterLikeOriginal(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                XDocument doc = XDocument.Load(path, LoadOptions.None);
                foreach (XElement route in doc.Descendants("OneAI_route"))
                {
                    string nation = ValueLikeOriginal(route, "Nation");
                    if (string.IsNullOrWhiteSpace(nation)) continue;
                    List<string> scripts;
                    if (!Routes.TryGetValue(nation, out scripts)) Routes[nation] = scripts = new List<string>();
                    foreach (XElement script in route.Descendants("ScriptName"))
                        if (!string.IsNullOrWhiteSpace(script.Value)) scripts.Add(script.Value.Trim());
                }
            }
            catch (Exception ex) { Debug.LogWarning("[C2:GAME AI DATA V340] router " + ex.Message); }
        }

        private static void ParseNationSettlementPrioritiesLikeOriginal(string nation, string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                XDocument doc = XDocument.Load(path, LoadOptions.None);
                List<SettlementPriorityBand> bands;
                if (!SettlementBands.TryGetValue(nation, out bands)) SettlementBands[nation] = bands = new List<SettlementPriorityBand>();
                foreach (XElement trigger in doc.Descendants("AI_Trigger"))
                {
                    List<XElement> priorities = new List<XElement>(trigger.Descendants("SettlementsPriory"));
                    if (priorities.Count == 0) continue;
                    var band = new SettlementPriorityBand();
                    foreach (XElement gaming in trigger.Descendants("GamingTime"))
                    {
                        float time;
                        if (!float.TryParse(ValueLikeOriginal(gaming, "Time"), NumberStyles.Float, CultureInfo.InvariantCulture, out time)) continue;
                        string op = ValueLikeOriginal(gaming, "OpType");
                        if (op.IndexOf("more", StringComparison.OrdinalIgnoreCase) >= 0) band.MinTime = Mathf.Max(band.MinTime, time);
                        if (op.IndexOf("less", StringComparison.OrdinalIgnoreCase) >= 0) band.MaxTime = Mathf.Min(band.MaxTime, time);
                    }
                    for (int i = 0; i < priorities.Count; i++)
                    {
                        int resource = ResourceIndexLikeOriginal(ValueLikeOriginal(priorities[i], "ResType"));
                        int value;
                        if (resource >= 0 && int.TryParse(ValueLikeOriginal(priorities[i], "Prio"), out value))
                            band.PriorityByResource[resource] = value;
                    }
                    if (band.PriorityByResource.Count > 0) bands.Add(band);
                }
            }
            catch (Exception ex) { Debug.LogWarning("[C2:GAME AI DATA V340] nation='" + nation + "' " + ex.Message); }
        }

        private static void ParseBrigadeRulesLikeOriginal(string path)
        {
            if (!File.Exists(path)) return;
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch { return; }
            string section = string.Empty;
            var units = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string pendingCondition = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = (lines[i] ?? string.Empty).Trim();
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    section = string.Empty;
                    units.Clear();
                    string header = line.Substring(1).Trim();
                    if (header.Length == 0) continue;
                    string[] tokens = Regex.Split(header, @"\s+");
                    section = tokens[0];
                    for (int t = 1; t < tokens.Length; t++)
                        if (!string.IsNullOrWhiteSpace(tokens[t])) units.Add(tokens[t].Trim());
                    continue;
                }
                if (line.StartsWith("/", StringComparison.Ordinal)) continue;
                if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
                {
                    pendingCondition = line.Substring(3).Trim();
                    continue;
                }
                if (!line.StartsWith("do ", StringComparison.OrdinalIgnoreCase) || units.Count == 0) continue;
                string[] actionTokens = Regex.Split(line.Substring(3).Trim(), @"\s+");
                if (actionTokens.Length == 0) continue;
                var rule = new BrigadeRule { File = path, Section = section, Condition = pendingCondition, Action = actionTokens[0] };
                int priority;
                if (actionTokens.Length > 1 && int.TryParse(actionTokens[actionTokens.Length - 1], out priority)) rule.Priority = priority;
                foreach (string unit in units) rule.UnitIds.Add(unit);
                BrigadeRules.Add(rule);
                pendingCondition = string.Empty;
            }
        }

        private static string ResolveDataRootLikeOriginal()
        {
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; roots != null && i < roots.Length; i++)
                if (!string.IsNullOrWhiteSpace(roots[i]) && File.Exists(Path.Combine(roots[i], "AI", "router.xml")))
                    return roots[i];
            return string.Empty;
        }

        private static string ValueLikeOriginal(XElement parent, string name)
        {
            XElement child = parent != null ? parent.Element(name) : null;
            return child != null ? (child.Value ?? string.Empty).Trim() : string.Empty;
        }

        private static int ResourceIndexLikeOriginal(string name)
        {
            if (name.Equals("Wood", StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.Equals("Gold", StringComparison.OrdinalIgnoreCase)) return 1;
            if (name.Equals("Stone", StringComparison.OrdinalIgnoreCase)) return 2;
            if (name.Equals("Food", StringComparison.OrdinalIgnoreCase)) return 3;
            if (name.Equals("Iron", StringComparison.OrdinalIgnoreCase)) return 4;
            if (name.Equals("Coal", StringComparison.OrdinalIgnoreCase)) return 5;
            return -1;
        }
    }
}
