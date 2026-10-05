using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// V394: explicit Cossacks II 1.4 compatibility context.
    ///
    /// IMPORTANT: C2 1.4 has more than one "nation" domain.  They must never
    /// be collapsed into a single legacy table:
    ///
    /// 1) Engine nation-name registry (9 entries) proven by engine_1.4.exe:
    ///    ENGLAND, FRANCE, AUSTRIA, PRUSSIA, RUSSIA, EGYPT, POLAND, SPAIN, REIN.
    ///    This registry is NOT automatically the roster of every UI screen.
    ///
    /// 2) GlobalAI roster loaded by the 1.4 engine from Ai\\Ai.dat.
    ///    The final installed 1.4 DataRoot can expose all nine nations.  A
    ///    bundled six-row base ai.dat must not override that final table.
    ///    cva_ProfAdd_Race and cva_BR_PlRace iterate this GlobalAI roster.
    ///
    /// Cossacks-I-compatible Nations.lst / unitslist.dat are deliberately NOT
    /// accepted here as menu race rosters.
    /// </summary>
    public static class C2Version14Context
    {
        public const string SourceEngine14 = "C2_14_ENGINE";
        public const string SourceData14 = "C2_14_DATA";
        public const string SourceSource10 = "C2_10_SOURCE";
        public const string SourceLegacyC1 = "C1_LEGACY";

        public sealed class EngineNationName14
        {
            public int Index;
            public string Id = string.Empty;
            public string LocKey = string.Empty;

            public override string ToString() => $"{Index}:{Id}";
        }

        public sealed class GlobalAiNation14
        {
            public int Index;
            public string Id = string.Empty;
            public string Message = string.Empty;
            public string UnitToken = string.Empty;
            public string HeroCode = string.Empty;
            public int NPeas;
            public int NLandAI;
            public int NWaterAI;
            public string PortraitRel = string.Empty;
            public string HeroPrefix = string.Empty;

            public override string ToString() => $"{Index}:{Id}->{NWaterAI}";
        }

        public sealed class GlobalAiSnapshot14
        {
            public string DataRoot = string.Empty;
            public string Source = SourceData14;
            public int DeclaredNAi;
            public int DeclaredNComp;
            public readonly List<string> DifficultyKeys = new();
            public readonly List<GlobalAiNation14> Nations = new();

            public string BuildMapLog()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < Nations.Count; i++)
                {
                    if (i != 0) sb.Append(',');
                    sb.Append(Nations[i]);
                }
                return sb.ToString();
            }
        }

        // Exact 1.4 engine mapping from the nine-case GetTextByID switch.
        // This is kept separate from GlobalAI on purpose.
        private static readonly EngineNationName14[] s_engineNationNames =
        {
            new() { Index = 0, Id = "ENGLAND", LocKey = "#ENGLAND" },
            new() { Index = 1, Id = "FRANCE",  LocKey = "#FRANCE"  },
            new() { Index = 2, Id = "AUSTRIA", LocKey = "#AUSTRIA" },
            new() { Index = 3, Id = "PRUSSIA", LocKey = "#PRUSSIA" },
            new() { Index = 4, Id = "RUSSIA",  LocKey = "#RUSSIA"  },
            new() { Index = 5, Id = "EGYPT",   LocKey = "#EGYPT"   },
            new() { Index = 6, Id = "POLAND",  LocKey = "#POLAND"  },
            new() { Index = 7, Id = "SPAIN",   LocKey = "#SPAIN"   },
            new() { Index = 8, Id = "REIN",    LocKey = "#REIN"    },
        };

        private static readonly Dictionary<string, Dictionary<string, string>> s_sourceTextCache =
            new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<EngineNationName14> EngineNationNameRegistry => s_engineNationNames;

        public static string ResolveCanonical14DataRoot(CoreFileSystem fallbackFs)
        {
            // V394: for logical GlobalAI data, a validated final 1.4 DataRoot
            // wins over the bundled six-row base data.  XML source selection
            // remains independent and can still be StreamingAssets-clean14.
            string active = fallbackFs?.DataRoot ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(active))
            {
                GlobalAiSnapshot14 activeAi = LoadGlobalAiFromRoot(active);
                if (IsFinal14NineNationRoster(activeAi))
                    return active;
            }

            try
            {
                string bundled = Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data");
                string ai = Path.Combine(bundled, "AI", "ai.dat");
                if (Directory.Exists(bundled) && File.Exists(ai))
                    return bundled;
            }
            catch
            {
                // Keep the fallback deterministic; never invent another root.
            }

            return active;
        }

        public static GlobalAiSnapshot14 LoadEffectiveGlobalAi(CoreFileSystem fallbackFs, Menu14SourceContext sourceContext = null)
        {
            string active = fallbackFs?.DataRoot ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(active))
            {
                GlobalAiSnapshot14 runtime = LoadGlobalAiFromRoot(active);
                if (IsFinal14NineNationRoster(runtime))
                    return runtime;
            }

            if (sourceContext != null)
            {
                GlobalAiSnapshot14 bound = LoadGlobalAi(sourceContext);
                if (bound.Nations.Count > 0)
                    return bound;
            }

            string root = ResolveCanonical14DataRoot(fallbackFs);
            return LoadGlobalAiFromRoot(root);
        }

        public static bool IsFinal14NineNationRoster(GlobalAiSnapshot14 snap)
        {
            if (snap == null || snap.DeclaredNAi != 9 || snap.DeclaredNComp < 9 || snap.Nations.Count != 9)
                return false;

            string[] ids = { "FRANCE", "RUSSIA", "ENGLAND", "PRUSSIA", "AUSTRIA", "EGIPET", "POLAND", "SPAIN", "REIN" };
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenFlags = new HashSet<int>();
            for (int i = 0; i < snap.Nations.Count; i++)
            {
                GlobalAiNation14 n = snap.Nations[i];
                seenIds.Add(n.Id ?? string.Empty);
                seenFlags.Add(n.NWaterAI);
            }

            for (int i = 0; i < ids.Length; i++)
                if (!seenIds.Contains(ids[i])) return false;

            for (int flag = 0; flag < 9; flag++)
                if (!seenFlags.Contains(flag)) return false;

            return true;
        }

        public static GlobalAiSnapshot14 LoadGlobalAi(Menu14SourceContext sourceContext)
        {
            var snap = new GlobalAiSnapshot14
            {
                DataRoot = sourceContext?.DataRoot ?? string.Empty,
                Source = SourceData14
            };

            if (sourceContext == null) return snap;
            string text = sourceContext.ReadAllText(@"AI\ai.dat", GetCp1251());
            ParseAiDat(text, snap);
            return snap;
        }

        public static GlobalAiSnapshot14 LoadGlobalAiFromRoot(string dataRoot)
        {
            var snap = new GlobalAiSnapshot14
            {
                DataRoot = dataRoot ?? string.Empty,
                Source = SourceData14
            };

            if (string.IsNullOrWhiteSpace(dataRoot)) return snap;
            string path = Path.Combine(dataRoot, "AI", "ai.dat");
            if (!File.Exists(path)) return snap;

            try
            {
                string text = File.ReadAllText(path, GetCp1251());
                ParseAiDat(text, snap);
            }
            catch (Exception e)
            {
                Debug.LogError($"[C2:1.4 AUDIT V394] failed to read '{path}': {e.Message}");
            }
            return snap;
        }

        public static string ResolveDataNationLabel(
            Menu14SourceContext sourceContext,
            LocDb loc,
            string nationId,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(nationId)) return fallback ?? string.Empty;
            string id = nationId.Trim();
            string key = "#" + id;
            string aliasKey = id.Equals("EGIPET", StringComparison.OrdinalIgnoreCase) ? "#EGYPT" :
                              id.Equals("EGYPT", StringComparison.OrdinalIgnoreCase) ? "#EGIPET" : string.Empty;

            string local = loc?.Resolve(key);
            if (!string.IsNullOrWhiteSpace(local) && !local.Equals(key, StringComparison.OrdinalIgnoreCase))
                return local;
            if (!string.IsNullOrEmpty(aliasKey))
            {
                local = loc?.Resolve(aliasKey);
                if (!string.IsNullOrWhiteSpace(local) && !local.Equals(aliasKey, StringComparison.OrdinalIgnoreCase))
                    return local;
            }

            string fromBundle = ResolveTextKeyFromBundle(sourceContext, key);
            if (string.IsNullOrWhiteSpace(fromBundle) && !string.IsNullOrEmpty(aliasKey))
                fromBundle = ResolveTextKeyFromBundle(sourceContext, aliasKey);
            if (!string.IsNullOrWhiteSpace(fromBundle))
                return fromBundle;

            return string.IsNullOrWhiteSpace(fallback) ? nationId : fallback;
        }

        public static string ResolveDataNationLabel(
            string dataRoot,
            LocDb loc,
            string nationId,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(nationId)) return fallback ?? string.Empty;
            string id = nationId.Trim();
            string key = "#" + id;
            string aliasKey = id.Equals("EGIPET", StringComparison.OrdinalIgnoreCase) ? "#EGYPT" :
                              id.Equals("EGYPT", StringComparison.OrdinalIgnoreCase) ? "#EGIPET" : string.Empty;

            string local = loc?.Resolve(key);
            if (!string.IsNullOrWhiteSpace(local) && !local.Equals(key, StringComparison.OrdinalIgnoreCase))
                return local;
            if (!string.IsNullOrEmpty(aliasKey))
            {
                local = loc?.Resolve(aliasKey);
                if (!string.IsNullOrWhiteSpace(local) && !local.Equals(aliasKey, StringComparison.OrdinalIgnoreCase))
                    return local;
            }

            string fromBundle = ResolveTextKeyFromRoot(dataRoot, key);
            if (string.IsNullOrWhiteSpace(fromBundle) && !string.IsNullOrEmpty(aliasKey))
                fromBundle = ResolveTextKeyFromRoot(dataRoot, aliasKey);
            if (!string.IsNullOrWhiteSpace(fromBundle))
                return fromBundle;

            return string.IsNullOrWhiteSpace(fallback) ? nationId : fallback;
        }

        /// <summary>
        /// Exact C2 1.4 cva_BR_PlRace::Init contract:
        /// first '#NationCombo_Random', then every GlobalAI.Ai[j].Message.
        /// </summary>
        public static string[] BuildBattleRoomRaceCombo(CoreFileSystem fallbackFs, LocDb loc)
        {
            GlobalAiSnapshot14 snap = LoadEffectiveGlobalAi(fallbackFs);
            string root = snap.DataRoot;
            var result = new List<string>(snap.Nations.Count + 1);

            string randomKey = "#NationCombo_Random";
            string random = loc?.Resolve(randomKey);
            if (string.IsNullOrWhiteSpace(random) || random.Equals(randomKey, StringComparison.OrdinalIgnoreCase))
                random = "Случайно";
            result.Add(random);

            for (int i = 0; i < snap.Nations.Count; i++)
            {
                GlobalAiNation14 n = snap.Nations[i];
                result.Add(ResolveDataNationLabel(root, loc, n.Id, n.Message));
            }

            return result.ToArray();
        }

        public static void AuditStartup(CoreFileSystem fallbackFs, LocDb loc, string canonical14Root)
        {
            GlobalAiSnapshot14 ai = LoadEffectiveGlobalAi(fallbackFs);
            string root = ai.DataRoot;

            bool engineRegistryOk = s_engineNationNames.Length == 9 &&
                                    s_engineNationNames[0].Id == "ENGLAND" &&
                                    s_engineNationNames[8].Id == "REIN";

            // Proven by engine_1.4.exe: profile and BR race combo both use
            // DAT_02a5d3d8/DAT_02a5d3dc loaded directly from Ai\\Ai.dat.
            bool profileContractOk = ai.Nations.Count == ai.DeclaredNAi && ai.DeclaredNAi > 0;
            bool battleRoomContractOk = profileContractOk;

            string status = engineRegistryOk && profileContractOk && battleRoomContractOk ? "PASS" : "FAIL";
            string line =
                $"[C2:1.4 AUDIT V394] engineNationNameRegistry={s_engineNationNames.Length} source={SourceEngine14} " +
                $"globalAiRoster={ai.Nations.Count}/{ai.DeclaredNAi} nComp={ai.DeclaredNComp} source={SourceData14} " +
                $"profileRoster={ai.Nations.Count} profileRosterSource={SourceData14}:Ai\\Ai.dat " +
                $"battleRoomRoster={ai.Nations.Count}+random battleRoomRosterSource={SourceData14}:Ai\\Ai.dat " +
                $"legacyC1NationRegistryUsed=0 unitslistRosterUsed=0 NationsLstRosterUsed=0 " +
                $"status={status} map='{ai.BuildMapLog()}'";

            if (status == "PASS") Debug.Log(line);
            else Debug.LogError(line);
        }

        private static void ParseAiDat(string text, GlobalAiSnapshot14 snap)
        {
            if (snap == null || string.IsNullOrEmpty(text)) return;
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int i = 0;

            int diffCount = 0;
            for (; i < lines.Length; i++)
            {
                string line = CleanLine(lines[i]);
                if (line.Length == 0) continue;
                if (int.TryParse(line, out diffCount))
                {
                    i++;
                    break;
                }
            }

            for (int d = 0; d < diffCount && i < lines.Length; i++)
            {
                string line = CleanLine(lines[i]);
                if (line.Length == 0) continue;
                string[] p = SplitWs(line);
                if (p.Length > 0)
                {
                    snap.DifficultyKeys.Add(p[0]);
                    d++;
                }
            }

            for (; i < lines.Length; i++)
            {
                string line = CleanLine(lines[i]);
                if (line.Length == 0) continue;
                string[] p = SplitWs(line);
                if (p.Length >= 2 && int.TryParse(p[0], out int nAi) && int.TryParse(p[1], out int nComp))
                {
                    snap.DeclaredNAi = nAi;
                    snap.DeclaredNComp = nComp;
                    i++;
                    break;
                }
            }

            for (; i < lines.Length && (snap.DeclaredNAi <= 0 || snap.Nations.Count < snap.DeclaredNAi); i++)
            {
                string line = CleanLine(lines[i]);
                if (line.Length == 0) continue;
                string[] p = SplitWs(line);
                if (p.Length < 8) continue;
                if (!int.TryParse(p[3], out int nPeas)) continue;
                if (!int.TryParse(p[4], out int nLandAI)) continue;
                if (!int.TryParse(p[5], out int nWaterAI)) continue;

                string heroCode = string.Empty;
                Match m = Regex.Match(p[2], @"\((?<c>[^)]+)\)");
                if (m.Success) heroCode = m.Groups["c"].Value;

                snap.Nations.Add(new GlobalAiNation14
                {
                    Index = snap.Nations.Count,
                    Id = p[0],
                    Message = p[1],
                    UnitToken = p[2],
                    HeroCode = heroCode,
                    NPeas = nPeas,
                    NLandAI = nLandAI,
                    NWaterAI = nWaterAI,
                    PortraitRel = p[6],
                    HeroPrefix = p[7]
                });
            }
        }

        private static string CleanLine(string raw)
        {
            string line = (raw ?? string.Empty).Trim();
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("//", StringComparison.Ordinal))
                return string.Empty;
            return line;
        }

        private static string[] SplitWs(string s)
        {
            return Regex.Split((s ?? string.Empty).Trim(), @"\s+");
        }


        private static Encoding GetCp1251()
        {
            try
            {
                return Encoding.GetEncoding(1251);
            }
            catch
            {
                try
                {
                    var t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                    var inst = t?.GetProperty("Instance")?.GetValue(null, null) as EncodingProvider;
                    if (inst != null) Encoding.RegisterProvider(inst);
                    return Encoding.GetEncoding(1251);
                }
                catch
                {
                    return Encoding.UTF8;
                }
            }
        }

        public static string ReadTextFromRoot(string dataRoot, string relativePath, Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(dataRoot) || string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            string path = ResolveCaseInsensitive(dataRoot, relativePath);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return string.Empty;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                return (encoding ?? GetCp1251()).GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveCaseInsensitive(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            string[] parts = relativePath.Replace('/', '\\').Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string cur = root;
            for (int i = 0; i < parts.Length; i++)
            {
                string direct = Path.Combine(cur, parts[i]);
                if (File.Exists(direct) || Directory.Exists(direct))
                {
                    cur = direct;
                    continue;
                }
                if (!Directory.Exists(cur)) return string.Empty;
                string match = null;
                try
                {
                    foreach (string entry in Directory.EnumerateFileSystemEntries(cur))
                    {
                        if (string.Equals(Path.GetFileName(entry), parts[i], StringComparison.OrdinalIgnoreCase))
                        {
                            match = entry;
                            break;
                        }
                    }
                }
                catch { return string.Empty; }
                if (string.IsNullOrEmpty(match)) return string.Empty;
                cur = match;
            }
            return cur;
        }

        private static string ResolveTextKeyFromBundle(Menu14SourceContext sourceContext, string key)
        {
            if (sourceContext == null) return string.Empty;
            string cacheKey = "bundle|" + sourceContext.DataRoot;
            Dictionary<string, string> map = GetOrBuildTextMap(cacheKey, rel => sourceContext.ReadAllText(rel, GetCp1251()));
            return map.TryGetValue(key, out string value) ? value : string.Empty;
        }

        private static string ResolveTextKeyFromRoot(string dataRoot, string key)
        {
            if (string.IsNullOrWhiteSpace(dataRoot)) return string.Empty;
            string cacheKey = "root|" + dataRoot;
            Dictionary<string, string> map = GetOrBuildTextMap(cacheKey, rel =>
            {
                string path = Path.Combine(dataRoot, rel.Replace('\\', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) return string.Empty;
                try { return File.ReadAllText(path, GetCp1251()); }
                catch { return string.Empty; }
            });
            return map.TryGetValue(key, out string value) ? value : string.Empty;
        }

        private static Dictionary<string, string> GetOrBuildTextMap(string cacheKey, Func<string, string> reader)
        {
            if (s_sourceTextCache.TryGetValue(cacheKey, out Dictionary<string, string> cached))
                return cached;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] files =
            {
                @"Text\Nations.txt",
                @"Text\BigMapData.txt",
                @"Text\dialogs.txt"
            };

            for (int i = 0; i < files.Length; i++)
            {
                string text = reader(files[i]);
                if (string.IsNullOrEmpty(text)) continue;
                string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                for (int j = 0; j < lines.Length; j++)
                {
                    string line = lines[j].Trim();
                    if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                        continue;
                    int sp = line.IndexOfAny(new[] { ' ', '\t' });
                    if (sp <= 0) continue;
                    string k = line.Substring(0, sp).Trim();
                    string v = line.Substring(sp).Trim();
                    if (k.Length > 0 && v.Length > 0 && !map.ContainsKey(k))
                        map[k] = v;
                }
            }

            s_sourceTextCache[cacheKey] = map;
            return map;
        }
    }
}
