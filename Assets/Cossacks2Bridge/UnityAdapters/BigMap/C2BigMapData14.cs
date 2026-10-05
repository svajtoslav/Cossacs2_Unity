using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters.Profiles;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.BigMap
{
    public static class C2BigMapData14
    {
        public const int Wood = 0;
        public const int Food = 1;
        public const int Stone = 2;
        public const int Gold = 3;
        public const int Iron = 4;
        public const int Coal = 5;
        public const int Recruits = 6;

        [Serializable]
        public sealed class ArrowDefinition
        {
            public int X;
            public int Y;
            public int SpriteOrDirection;
        }

        /// <summary>
        /// Immutable-by-contract DataRoot definition. Mutable campaign ownership,
        /// defence, population and recruits live in C2ProfileRuntime14.SectorState.
        /// </summary>
        [Serializable]
        public sealed class SectorDefinition
        {
            public int Id;
            public int CenterX, CenterY;
            public int CityX, CityY;
            public int FortX, FortY;
            public int DefOwner;
            public int Defence;
            public int Population;
            public string SectorName = string.Empty;
            public readonly List<int> Neighbors = new List<int>();
            public readonly List<ArrowDefinition> Arrows = new List<ArrowDefinition>();
            public int ArrowOffset;
            public int Villages;
            public int Resource;
            public int Owner;
        }

        public sealed class Data
        {
            public int MapWidth = 990;
            public int MapHeight = 825;
            public int DeclaredSectorCount;
            public string SourceDataRoot = string.Empty;
            public C2Bfe14AuditResultV396A Audit;
            public readonly List<SectorDefinition> Sectors = new List<SectorDefinition>();
            public readonly Dictionary<string, int> Constants = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        // V395P: BigMapConst.dat and Sectors.dat are static game definitions.
        // Before this cache, opening EW2 statistics synchronously read and parsed
        // both files every time (and V395L/M/N/O did it twice on the first frame).
        // The returned Data is used as read-only definition data; mutable campaign
        // state lives in ProfileRecord.SectorState instead.
        private static Data s_cachedDataV396A;
        private static string s_cachedDataRootV396A = string.Empty;

        public static Data Load(CoreFileSystem fs)
        {
            string effectiveRoot = C2Bfe14DataRootAuditV396A.ResolveEffectiveDataRoot(fs);
            string cacheKey = NormalizeRoot(effectiveRoot);
            if (s_cachedDataV396A != null && string.Equals(s_cachedDataRootV396A, cacheKey, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[C2:BIGMAP DATA V396A] cacheHit=1 dataRoot='{effectiveRoot}' files=BigMapConst.dat+Sectors.dat");
                return s_cachedDataV396A;
            }

            long perfStartV396A = System.Diagnostics.Stopwatch.GetTimestamp();
            CoreFileSystem effectiveFs = !string.IsNullOrWhiteSpace(effectiveRoot) ? new CoreFileSystem(effectiveRoot) : fs;
            var d = new Data { SourceDataRoot = effectiveRoot ?? string.Empty };
            ParseConstants(ReadCp1251(effectiveFs, @"Missions\StatData\BigMapConst.dat"), d.Constants);
            ParseSectors(ReadCp1251(effectiveFs, @"Missions\StatData\Sectors.dat"), d);
            d.Audit = C2Bfe14DataRootAuditV396A.Audit(d.SourceDataRoot, d);

            s_cachedDataV396A = d;
            s_cachedDataRootV396A = cacheKey;

            double elapsedMsV396A = (System.Diagnostics.Stopwatch.GetTimestamp() - perfStartV396A) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Debug.Log($"[C2:BIGMAP DATA V396A] cacheHit=0 parsed=1 dataRoot='{d.SourceDataRoot}' constants={d.Constants.Count} sectors={d.Sectors.Count} elapsedMs={elapsedMsV396A:F2}");
            return d;
        }

        public static int CampaignNationFromProfileNation(int profileNationIndex)
        {
            if (!Menu14ActionStateRuntime.TryGetNationRecord(profileNationIndex, out var n) || n == null)
                return -1;
            return CampaignNationFromId(n.Id);
        }

        public static int CampaignNationFromId(string id)
        {
            return C2Bfe14ContractV396A.CampaignNationFromId(id);
        }

        public static string CampaignNationTextKey(int id)
        {
            return C2Bfe14ContractV396A.CampaignNationTextKey(id);
        }

        public static void EnsureCampaignInitialized(C2ProfileRuntime14.ProfileRecord p, Data d)
        {
            if (p == null || d == null || p.campaignInitialized) return;

            int cn = CampaignNationFromProfileNation(p.m_iNation);
            p.campaignNation = cn;
            p.m_iCurMenuId = 0;
            p.m_inCurTurn = 0;

            int diff = Mathf.Clamp(p.m_iDifficulty, 0, 2);
            int baseRes = Get(d, "#PLAYER_RES" + diff.ToString(CultureInfo.InvariantCulture), diff == 0 ? 10000 : 5000);
            int foodRatio = Get(d, "#FOOD_RATIO", 4);
            p.resources = new int[7];
            for (int i = 0; i <= Coal; i++) p.resources[i] = baseRes;
            p.resources[Food] = foodRatio * baseRes;

            p.sectors.Clear();
            int maxRecruit = Get(d, "#SECT_MAX_RECRTS", 120);
            foreach (SectorDefinition s in d.Sectors)
            {
                p.sectors.Add(new C2ProfileRuntime14.SectorState
                {
                    id = s.Id,
                    owner = s.Owner,
                    defence = s.Defence,
                    population = s.Population,
                    resource = s.Resource,
                    villages = s.Villages,
                    recruits = (s.Population + 1) * maxRecruit
                });
            }

            p.m_iCurSecId = FindCapitalSector(p, d);
            p.resources[Recruits] = SumRecruits(p);
            p.campaignInitialized = true;

            // Original ProcessBigMap saves turn-0 statistics immediately through
            // CPlayerSAVE::GetCountry -> SaveStatistics.  Keep the same one-sample
            // per-turn contract in the bridge profile.
            CaptureStatisticsSnapshot(p, d);
            C2ProfileRuntime14.SaveCurrent();

            Debug.Log($"[C2:BIGMAP V396A] campaign initialized profile='{p.m_chName}' profileNation={p.m_iNation} campaignNation={p.campaignNation} sectors={p.sectors.Count} baseRes={baseRes} food={p.resources[Food]}");
        }

        public static void EndTurn(C2ProfileRuntime14.ProfileRecord p, Data d)
        {
            if (p == null || d == null) return;
            EnsureCampaignInitialized(p, d);
            if (p.campaignNation < 0) return;

            int regRecruit = Get(d, "#SECT_REG_RECRTS", 30);
            int maxRecruit = Get(d, "#SECT_MAX_RECRTS", 120);

            foreach (var ps in p.sectors)
            {
                if (ps == null || ps.owner != p.campaignNation) continue;

                int resId = ps.resource;
                if (resId == 1) p.resources[Food] += Get(d, "#SECT_ADDING_FOOD", 4000);
                else if (resId == 2) p.resources[Gold] += Get(d, "#SECT_ADDING_GOLD", 1500);
                else if (resId == 3) p.resources[Iron] += Get(d, "#SECT_ADDING_IRON", 1000);
                else if (resId == 4) p.resources[Coal] += Get(d, "#SECT_ADDING_COAL", 1000);

                p.resources[Wood] += Get(d, "#SECT_INCOME_WOOD", 750);
                p.resources[Food] += Get(d, "#SECT_INCOME_FOOD", 2000);
                p.resources[Stone] += Get(d, "#SECT_INCOME_STONE", 750);
                p.resources[Gold] += Get(d, "#SECT_INCOME_GOLD", 500);
                p.resources[Iron] += Get(d, "#SECT_INCOME_IRON", 0);
                p.resources[Coal] += Get(d, "#SECT_INCOME_COAL", 0);

                int pop = ps.population + 1;
                ps.recruits += pop * regRecruit;
                int max = pop * maxRecruit;
                if (ps.recruits > max) ps.recruits = max;
            }

            p.resources[Recruits] = SumRecruits(p);
            p.m_inCurTurn++;
            CaptureStatisticsSnapshot(p, d);
            C2ProfileRuntime14.SaveCurrent();
            Debug.Log($"[C2:BIGMAP V396A] EndTurn legacyApproximation=YES turn={p.m_inCurTurn} resources={string.Join(",", p.resources)}");
        }

        /// <summary>
        /// CPlayerSAVE::SaveStatistics port for the non-battle campaign state that
        /// is already present in the bridge.  SecNum/ResSup/RecrtN/NatPow are
        /// reconstructed from sector state exactly; the current player's ResAmo
        /// is reconstructed from its resource vector.  Army/hero/kill series stay
        /// zero until their campaign runtime state is ported rather than inventing
        /// data that the bridge does not own.
        /// </summary>
        public static bool CaptureStatisticsSnapshot(C2ProfileRuntime14.ProfileRecord p, Data d, bool force = false)
        {
            if (p == null || d == null || !p.campaignInitialized) return false;
            if (!force && p.lastStatsUpdate == p.m_inCurTurn) return false;

            // Final engine_1.4.exe campaign-stat viewer indexes 9 slots.
            C2ProfileRuntime14.EnsureStatsRecords(p, C2Bfe14ContractV396A.CountryCount);

            for (int nat = 0; nat < C2Bfe14ContractV396A.CountryCount; nat++)
            {
                C2ProfileRuntime14.CampaignStatsRecord st = C2ProfileRuntime14.GetStats(p, nat);
                if (st == null) continue;

                int secNum = 0;
                int supp = 0;
                int natPow = 0;
                int recruits = 0;

                if (p.sectors != null)
                {
                    foreach (C2ProfileRuntime14.SectorState sec in p.sectors)
                    {
                        if (sec == null || sec.owner != nat) continue;
                        secNum++;
                        natPow += sec.defence + 1;
                        recruits += Math.Max(0, sec.recruits);
                        supp += SectorSupplyValue(d, sec.resource);
                    }
                }

                int resAll = 0;
                if (nat == p.campaignNation && p.resources != null)
                {
                    for (int res = 0; res <= Coal; res++)
                        resAll += (int)(GetResSafe(p.resources, res) / ResourceDivisor(res));
                }

                st.SecNum.Add(secNum);
                st.ResSup.Add(supp);
                st.ResAmo.Add(resAll);
                st.RecrtN.Add(recruits);
                st.ArmPow.Add(0);
                st.NatPow.Add(natPow);
                st.GenExp.Add(0);
                st.FKill.Add(0);
                st.FLost.Add(0);
            }

            p.lastStatsUpdate = p.m_inCurTurn;
            Debug.Log($"[C2:CAMPSTAT V395L] snapshot turn={p.m_inCurTurn} stats={p.stats?.Count ?? 0} " +
                      "available=SecNum/ResSup/RecrtN/NatPow+playerResAmo pending=ArmPow/GenExp/FKill/FLost+AIResAmo");
            return true;
        }

        private static int SectorSupplyValue(Data d, int resourceId)
        {
            int supp = 0;
            if (resourceId == 1) supp += (int)(Get(d, "#SECT_ADDING_FOOD", 4000) / ResourceDivisor(Food));
            else if (resourceId == 2) supp += (int)(Get(d, "#SECT_ADDING_GOLD", 1500) / ResourceDivisor(Gold));
            else if (resourceId == 3) supp += (int)(Get(d, "#SECT_ADDING_IRON", 1000) / ResourceDivisor(Iron));
            else if (resourceId == 4) supp += (int)(Get(d, "#SECT_ADDING_COAL", 1000) / ResourceDivisor(Coal));

            supp += (int)(Get(d, "#SECT_INCOME_FOOD", 2000) / ResourceDivisor(Food));
            supp += (int)(Get(d, "#SECT_INCOME_WOOD", 750) / ResourceDivisor(Wood));
            supp += (int)(Get(d, "#SECT_INCOME_STONE", 750) / ResourceDivisor(Stone));
            supp += (int)(Get(d, "#SECT_INCOME_GOLD", 500) / ResourceDivisor(Gold));
            supp += (int)(Get(d, "#SECT_INCOME_IRON", 0) / ResourceDivisor(Iron));
            supp += (int)(Get(d, "#SECT_INCOME_COAL", 0) / ResourceDivisor(Coal));
            return supp;
        }

        private static float ResourceDivisor(int res)
        {
            if (res == Gold) return 1f;
            if (res == Coal) return 2f;
            if (res == Food) return 9f;
            if (res == Iron) return 4f;
            if (res == Wood) return 7f;
            if (res == Stone) return 7f;
            return 1f;
        }

        private static int GetResSafe(int[] r, int i)
        {
            return r != null && i >= 0 && i < r.Length ? r[i] : 0;
        }

        public static int CampaignNationFromGlobalAiIndex(int aiIndex)
        {
            // V396A: GlobalAI index and BigMap country id are different domains.
            // Mapping is valid only through the nation string ID.
            if (!Menu14ActionStateRuntime.TryGetNationRecord(aiIndex, out var n) || n == null)
                return -1;
            return CampaignNationFromId(n.Id);
        }

        public static Color32 CampaignMapColor(Data d, int campaignNation)
        {
            int fallback;
            switch (campaignNation)
            {
                case 0: fallback = unchecked((int)0x10EF4123); break;
                case 1: fallback = unchecked((int)0x104E45C0); break;
                case 2: fallback = unchecked((int)0x10808040); break;
                case 3: fallback = unchecked((int)0x10A84DBB); break;
                case 4: fallback = unchecked((int)0x1077D564); break;
                case 5: fallback = unchecked((int)0x10FFCF00); break;
                case 6: fallback = unchecked((int)0x00D0D0D0); break;
                case 7: fallback = unchecked((int)0xFF6A4300); break;
                case 8: fallback = unchecked((int)0xFF6A4300); break;
                default: fallback = unchecked((int)0xFF6A4300); break;
            }
            int raw = Get(d, "NATCOLOR" + campaignNation.ToString(CultureInfo.InvariantCulture), fallback);
            uint col = unchecked((uint)raw) | 0xFF000000u; // cva_CampStat_Color / Graphs
            return new Color32((byte)((col >> 16) & 0xFF), (byte)((col >> 8) & 0xFF), (byte)(col & 0xFF), (byte)((col >> 24) & 0xFF));
        }

        public static int TotalRecruits(C2ProfileRuntime14.ProfileRecord p)
        {
            return SumRecruits(p);
        }

        public static int DeleteRecruitsForDefence(C2ProfileRuntime14.ProfileRecord p, Data d, int sectorId, int amount)
        {
            if (p == null || d == null || amount <= 0 || p.sectors == null) return amount;
            int remaining = amount;

            // Original DelRecruitsInSectors starts with the selected sector and
            // its neighbors, then continues through other owned sectors.  The C++
            // version adds random skips; for the player-side bridge we keep the
            // same priority but deterministic ordering so save/replay state is stable.
            var order = new List<int>();
            SectorDefinition src = SectorById(d, sectorId);
            if (src != null)
            {
                foreach (int n in src.Neighbors) if (!order.Contains(n)) order.Add(n);
            }
            if (!order.Contains(sectorId)) order.Add(sectorId);
            foreach (var st in p.sectors)
                if (st != null && st.owner == p.campaignNation && !order.Contains(st.id)) order.Add(st.id);

            for (int i = order.Count - 1; i >= 0 && remaining > 0; i--)
            {
                SectorStateUse(p, order[i], ref remaining);
            }
            return remaining;
        }

        private static void SectorStateUse(C2ProfileRuntime14.ProfileRecord p, int id, ref int remaining)
        {
            if (remaining <= 0) return;
            C2ProfileRuntime14.SectorState st = StateFor(p, id);
            if (st == null || st.owner != p.campaignNation || st.recruits <= 0) return;
            int take = Math.Min(st.recruits, remaining);
            st.recruits -= take;
            remaining -= take;
        }

        public static int CapturedSectors(C2ProfileRuntime14.ProfileRecord p)
        {
            if (p == null || p.sectors == null || p.campaignNation < 0) return 0;
            int c = 0;
            foreach (var s in p.sectors) if (s != null && s.owner == p.campaignNation) c++;
            return c;
        }

        public static int FindCapitalSector(C2ProfileRuntime14.ProfileRecord p, Data d)
        {
            int owner = p != null ? p.campaignNation : -1;
            if (owner < 0) return d.Sectors.Count > 0 ? d.Sectors[0].Id : 0;
            foreach (SectorDefinition s in d.Sectors) if (s.Owner == owner) return s.Id;
            return d.Sectors.Count > 0 ? d.Sectors[0].Id : 0;
        }

        public static C2ProfileRuntime14.SectorState StateFor(C2ProfileRuntime14.ProfileRecord p, int id)
        {
            if (p?.sectors == null) return null;
            foreach (var s in p.sectors) if (s != null && s.id == id) return s;
            return null;
        }

        public static SectorDefinition SectorById(Data d, int id)
        {
            if (d == null) return null;
            foreach (SectorDefinition s in d.Sectors) if (s.Id == id) return s;
            return null;
        }

        public static int Get(Data d, string key, int fallback)
        {
            return d != null && d.Constants.TryGetValue(key, out int v) ? v : fallback;
        }

        private static int SumRecruits(C2ProfileRuntime14.ProfileRecord p)
        {
            int n = 0;
            if (p?.sectors != null)
                foreach (var s in p.sectors) if (s != null && s.owner == p.campaignNation) n += Math.Max(0, s.recruits);
            return n;
        }

        private static string ReadCp1251(CoreFileSystem fs, string rel)
        {
            if (fs == null) return string.Empty;
            string p = fs.ResolvePath(rel);
            if (!File.Exists(p))
            {
                string bundled = Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", rel.Replace('\\', Path.DirectorySeparatorChar));
                if (File.Exists(bundled)) p = bundled;
            }
            if (!File.Exists(p)) return string.Empty;
            try
            {
                TryRegisterCodePages();
                return File.ReadAllText(p, Encoding.GetEncoding(1251));
            }
            catch { return File.ReadAllText(p); }
        }

        private static void TryRegisterCodePages()
        {
            try
            {
                var t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                if (t == null) return;
                var prop = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var inst = prop != null ? prop.GetValue(null, null) : null;
                if (inst == null) return;
                var m = typeof(Encoding).GetMethod("RegisterProvider", new[] { typeof(EncodingProvider) });
                if (m != null) m.Invoke(null, new[] { inst });
            }
            catch { }
        }

        private static void ParseConstants(string txt, Dictionary<string, int> dst)
        {
            if (string.IsNullOrEmpty(txt)) return;
            using (var sr = new StringReader(txt))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("//") || line.StartsWith("/")) continue;
                    string[] p = Split(line);
                    if (p.Length >= 2 && (p[0].StartsWith("#") || char.IsLetter(p[0][0])))
                    {
                        if (int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                            dst[p[0]] = v;
                        else if (uint.TryParse(p[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hex))
                            dst[p[0]] = unchecked((int)hex);
                    }
                }
            }
        }

        private static void ParseSectors(string txt, Data d)
        {
            if (string.IsNullOrEmpty(txt) || d == null) return;
            SectorDefinition cur = null;
            using (var sr = new StringReader(txt))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("//") || line.StartsWith("/")) continue;
                    string[] p = Split(line);
                    if (p.Length == 0) continue;
                    if (p[0].Equals("#SECTORN#", StringComparison.OrdinalIgnoreCase) && p.Length >= 2)
                    {
                        d.DeclaredSectorCount = I(p, 1);
                        continue;
                    }
                    if (p[0].Equals("#MAPSIZE#", StringComparison.OrdinalIgnoreCase) && p.Length >= 3)
                    {
                        int.TryParse(p[1], out d.MapWidth); int.TryParse(p[2], out d.MapHeight); continue;
                    }
                    if (p[0].Equals("#SECTOR#", StringComparison.OrdinalIgnoreCase))
                    {
                        cur = new SectorDefinition(); d.Sectors.Add(cur); continue;
                    }
                    if (cur == null) continue;
                    switch (p[0])
                    {
                        case "ID": cur.Id = I(p,1); break;
                        case "Center": cur.CenterX = I(p,1); cur.CenterY = I(p,2); break;
                        case "SityXY": cur.CityX = I(p,1); cur.CityY = I(p,2); break;
                        case "FortXY": cur.FortX = I(p,1); cur.FortY = I(p,2); break;
                        case "DefOwner": cur.DefOwner = I(p,1); break;
                        case "Defence": cur.Defence = I(p,1); break;
                        case "Population": cur.Population = I(p,1); break;
                        case "SectorName": cur.SectorName = p.Length > 1 ? p[1] : ("Sector" + cur.Id); break;
                        case "Neighbors":
                            cur.Neighbors.Clear();
                            int count = I(p,1);
                            for (int i = 0; i < count && i + 2 < p.Length; i++) cur.Neighbors.Add(I(p,i+2));
                            break;
                        case "Arrow":
                            cur.Arrows.Clear();
                            // Original CSectStatData::ReadInitData consumes m_inNON*3
                            // integers: X, Y, sprite/direction for each neighbor.
                            int arrowCount = cur.Neighbors.Count;
                            for (int i = 0; i < arrowCount; i++)
                            {
                                int baseIndex = 1 + i * 3;
                                if (baseIndex + 2 >= p.Length) break;
                                cur.Arrows.Add(new ArrowDefinition
                                {
                                    X = I(p, baseIndex),
                                    Y = I(p, baseIndex + 1),
                                    SpriteOrDirection = I(p, baseIndex + 2)
                                });
                            }
                            break;
                        case "Villages": cur.Villages = I(p,1); break;
                        case "Ressource": cur.Resource = I(p,1); break;
                        case "Owner": cur.Owner = I(p,1); break;
                    }
                }
            }

            // Original CSectData assigns each sector a cumulative offset into the
            // shared arrow-picture array. Preserve that definition now so rendering
            // can be ported later without reparsing Sectors.dat.
            int arrowOffset = 0;
            foreach (SectorDefinition sector in d.Sectors)
            {
                sector.ArrowOffset = arrowOffset;
                arrowOffset += sector.Arrows.Count;
            }

            if (d.DeclaredSectorCount > 0 && d.DeclaredSectorCount != d.Sectors.Count)
                Debug.LogWarning($"[C2:BFE14 SECTORS V396A] declared={d.DeclaredSectorCount} parsed={d.Sectors.Count} mismatch=YES");
        }

        private static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) return string.Empty;
            try { return Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch { return root.Trim(); }
        }

        private static string[] Split(string line)
        {
            int c = line.IndexOf("//", StringComparison.Ordinal);
            if (c >= 0) line = line.Substring(0, c);
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int I(string[] p, int i)
        {
            return i >= 0 && i < p.Length && int.TryParse(p[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }
    }
}
