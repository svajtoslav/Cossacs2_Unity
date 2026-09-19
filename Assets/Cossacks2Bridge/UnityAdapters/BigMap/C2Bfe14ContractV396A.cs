using System;
using System.IO;
using System.Text;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.Core.Loaders;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.BigMap
{
    /// <summary>
    /// V396A: single semantic contract for the final 1.4 Battle for Europe layer.
    /// Country-sized loops MUST use CountryCount. Resource/rank loops deliberately
    /// remain six-wide; never mechanically replace every legacy literal 6 with 9.
    /// </summary>
    public static class C2Bfe14ContractV396A
    {
        public const int CountryCount = 9;
        public const int EconomyResourceCount = 6;
        public const int HeroRankCount = 6;
        public const int MaxHeroesPerCountry = 10;
        public const int MainPageCount = 5;
        public const int SectorSecondMinimapAtlasStart = 24;
        // V396A2.1 compatibility alias; canonical name is SectorSecondMinimapAtlasStart.
        public const int SectorSecondAtlasStart = SectorSecondMinimapAtlasStart;

        public static readonly string[] BigMapCountryIds =
        {
            "ENGLAND", "FRANCE", "AUSTRIA", "PRUSSIA", "RUSSIA",
            "EGYPT", "POLAND", "SPAIN", "REIN"
        };

        public static readonly string[] BigMapCountryTextKeys =
        {
            "#ENGLAND", "#FRANCE", "#AUSTRIA", "#PRUSSIA", "#RUSSIA",
            "#EGYPT", "#POLAND", "#SPAIN", "#REIN"
        };

        public static int CampaignNationFromId(string id)
        {
            string normalized = (id ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized == "EGIPET") normalized = "EGYPT"; // data spelling alias

            for (int i = 0; i < BigMapCountryIds.Length; i++)
                if (normalized == BigMapCountryIds[i]) return i;
            return -1;
        }

        public static string CampaignNationTextKey(int id)
        {
            return id >= 0 && id < BigMapCountryTextKeys.Length
                ? BigMapCountryTextKeys[id]
                : string.Empty;
        }
    }

    public sealed class C2Bfe14AuditResultV396A
    {
        public string DataRoot = string.Empty;
        public int AiDeclaredNAi;
        public int AiDeclaredNComp;
        public int AiNationCount;
        public bool IsNineNationRoster;
        public int DeclaredSectorCount;
        public int ParsedSectorCount;
        public bool HasHeroArmy;
        public bool HasQuests;
        public bool HasSecondMinimapAtlas;
        public bool HasArmyFlag;
        public bool HasBmElements5;
        public bool HasAttackHero;
        public bool HasAttackForAlly;
        public bool HasDefeatNation;
        public bool LooksLikeFinal14;
        public string Status = string.Empty;
    }

    /// <summary>
    /// Runtime evidence gate for BigMap. It does not guess missing final assets and
    /// never upgrades bundled 6-country/24-sector data into "final 1.4" by assumption.
    /// </summary>
    public static class C2Bfe14DataRootAuditV396A
    {
        private const string BaseFallbackStatus = "BASE/FALLBACK DATA — NOT FINAL 1.4";
        private const string FinalStatus = "FINAL_14";

        public static string ResolveEffectiveDataRoot(CoreFileSystem fs)
        {
            // Menu logical data is already the preferred 1.4 source when a real
            // installed nine-nation DataRoot has been discovered.
            string logical = Menu14ActionStateRuntime.CurrentLogicalDataRoot;
            if (IsNineNationRoot(logical)) return logical;

            string active = fs != null ? (fs.DataRoot ?? string.Empty) : string.Empty;
            if (IsNineNationRoot(active)) return active;

            // If neither root proves final 1.4, keep the caller's active root.
            // C2BigMapData14 may still use its bundled fallback, but the audit will
            // label that data explicitly instead of silently treating it as final.
            if (!string.IsNullOrWhiteSpace(active)) return active;
            return logical ?? string.Empty;
        }

        public static C2Bfe14AuditResultV396A Audit(string dataRoot, C2BigMapData14.Data data)
        {
            var r = new C2Bfe14AuditResultV396A();
            r.DataRoot = dataRoot ?? string.Empty;

            C2Version14Context.GlobalAiSnapshot14 ai = C2Version14Context.LoadGlobalAiFromRoot(r.DataRoot);
            if (ai != null)
            {
                r.AiDeclaredNAi = ai.DeclaredNAi;
                r.AiDeclaredNComp = ai.DeclaredNComp;
                r.AiNationCount = ai.Nations.Count;
                r.IsNineNationRoster = C2Version14Context.IsFinal14NineNationRoster(ai);
            }

            r.DeclaredSectorCount = data != null ? data.DeclaredSectorCount : 0;
            r.ParsedSectorCount = data != null ? data.Sectors.Count : 0;
            r.HasHeroArmy = FileExists(r.DataRoot, @"Missions\StatData\HeroArmy.dat");
            r.HasQuests = FileExists(r.DataRoot, @"Missions\StatData\Quests.dat");
            r.HasSecondMinimapAtlas = HasResourceStem(r.DataRoot, @"Interf3\TotalWarGraph", "lva_SectMiniMP2");
            r.HasArmyFlag = HasResourceStem(r.DataRoot, @"Interf3\TotalWarGraph", "armyflag");
            r.HasBmElements5 = HasResourceStem(r.DataRoot, @"Interf3\TotalWarGraph", "bmElements5");

            if (r.HasQuests)
            {
                string q = ReadTextCp1251(Path.Combine(r.DataRoot, "Missions", "StatData", "Quests.dat"));
                r.HasAttackHero = ContainsToken(q, "AttackHero");
                r.HasAttackForAlly = ContainsToken(q, "AttackForAlly");
                r.HasDefeatNation = ContainsToken(q, "DefeatNation");
            }

            // The two structural facts established by the audit are enough to
            // distinguish the known bundled/base layer from the observed final data.
            // Asset-file presence is reported separately because some installations
            // may resolve graphics through packed archives rather than loose files.
            r.LooksLikeFinal14 = r.IsNineNationRoster && r.ParsedSectorCount > C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart;
            r.Status = r.LooksLikeFinal14 ? FinalStatus : BaseFallbackStatus;

            Log(r);
            return r;
        }

        private static bool IsNineNationRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return false;
            C2Version14Context.GlobalAiSnapshot14 ai = C2Version14Context.LoadGlobalAiFromRoot(root);
            return C2Version14Context.IsFinal14NineNationRoster(ai);
        }

        private static void Log(C2Bfe14AuditResultV396A r)
        {
            Debug.Log($"[C2:BFE14 CONTRACT V396A] countries={C2Bfe14ContractV396A.CountryCount} resources={C2Bfe14ContractV396A.EconomyResourceCount} ranks={C2Bfe14ContractV396A.HeroRankCount} heroesPerCountry={C2Bfe14ContractV396A.MaxHeroesPerCountry}");
            Debug.Log($"[C2:BFE14 DATAROOT V396A] '{r.DataRoot}'");
            Debug.Log($"[C2:BFE14 AI V396A] declaredNAi={r.AiDeclaredNAi} declaredNComp={r.AiDeclaredNComp} nations={r.AiNationCount} final14={(r.IsNineNationRoster ? "YES" : "NO")}");
            Debug.Log($"[C2:BFE14 SECTORS V396A] declared={r.DeclaredSectorCount} parsed={r.ParsedSectorCount} dataDriven=YES");
            Debug.Log($"[C2:BFE14 ATLAS V396A] sectors0_23=lva_SectMiniMP sectors24+=lva_SectMiniMP2 looseSecondAtlas={(r.HasSecondMinimapAtlas ? "YES" : "NO")}");
            Debug.Log($"[C2:BFE14 FILES V396A] HeroArmy.dat={(r.HasHeroArmy ? "YES" : "NO")} Quests.dat={(r.HasQuests ? "YES" : "NO")} armyflag={(r.HasArmyFlag ? "YES" : "NO")} bmElements5={(r.HasBmElements5 ? "YES" : "NO")}");
            Debug.Log($"[C2:BFE14 QUESTS V396A] AttackHero={(r.HasAttackHero ? "YES" : "NO")} AttackForAlly={(r.HasAttackForAlly ? "YES" : "NO")} DefeatNation={(r.HasDefeatNation ? "YES" : "NO")}");

            string statusLine = $"[C2:BFE14 DATA STATUS V396A] {r.Status}";
            if (r.LooksLikeFinal14) Debug.Log(statusLine);
            else Debug.LogWarning(statusLine);
        }

        private static bool FileExists(string root, string rel)
        {
            if (string.IsNullOrWhiteSpace(root)) return false;
            string normalized = rel.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(Path.Combine(root, normalized));
        }

        private static bool HasResourceStem(string root, string relDir, string stem)
        {
            if (string.IsNullOrWhiteSpace(root)) return false;
            string normalized = relDir.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            string dir = Path.Combine(root, normalized);
            if (!Directory.Exists(dir)) return false;

            try
            {
                string[] files = Directory.GetFiles(dir, stem + ".*");
                if (files != null && files.Length > 0) return true;
                return File.Exists(Path.Combine(dir, stem));
            }
            catch { return false; }
        }

        private static bool ContainsToken(string text, string token)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReadTextCp1251(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return string.Empty;
            try
            {
                TryRegisterCodePages();
                return File.ReadAllText(path, Encoding.GetEncoding(1251));
            }
            catch
            {
                try { return File.ReadAllText(path); }
                catch { return string.Empty; }
            }
        }

        private static void TryRegisterCodePages()
        {
            try
            {
                Type t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                if (t == null) return;
                var prop = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                object inst = prop != null ? prop.GetValue(null, null) : null;
                if (inst == null) return;
                var m = typeof(Encoding).GetMethod("RegisterProvider", new[] { typeof(EncodingProvider) });
                if (m != null) m.Invoke(null, new[] { inst });
            }
            catch { }
        }
    }
}
