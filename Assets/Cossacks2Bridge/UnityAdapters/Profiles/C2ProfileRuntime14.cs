using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Profiles
{
    /// <summary>
    /// Unity-side equivalent of CPlayerSAVE/CPlayerSAVE_STRUCT for the menu and
    /// the non-battle layer of Total War / Conquest of Europe.
    ///
    /// Original 1.0/1.1 contract used by this port:
    ///   Save\\Battle\\bmdat.sav            - profile list/header
    ///   Save\\Battle\\bmdat%d.sav          - one profile payload
    ///   selected profile is moved to index 0
    ///
    /// We deliberately keep the same field semantics/order in memory, while
    /// serializing a bridge XML next to Unity persistent data. This avoids
    /// pretending that Unity can byte-for-byte read GSC BaseClass XML before
    /// that serializer is ported, but preserves the original runtime model.
    /// </summary>
    public static class C2ProfileRuntime14
    {
        [Serializable]
        public sealed class SectorState
        {
            public int id;
            public int owner;
            public int defence;
            public int population;
            public int recruits;
            public int resource;
            public int villages;
        }

        [Serializable]
        public sealed class CampaignStatsRecord
        {
            public int indx;
            public List<int> SecNum = new List<int>();
            public List<int> ResSup = new List<int>();
            public List<int> ResAmo = new List<int>();
            public List<int> RecrtN = new List<int>();
            public List<int> ArmPow = new List<int>();
            public List<int> NatPow = new List<int>();
            public List<int> GenExp = new List<int>();
            public List<int> FKill = new List<int>();
            public List<int> FLost = new List<int>();

            public CampaignStatsRecord Clone()
            {
                return new CampaignStatsRecord
                {
                    indx = indx,
                    SecNum = new List<int>(SecNum ?? new List<int>()),
                    ResSup = new List<int>(ResSup ?? new List<int>()),
                    ResAmo = new List<int>(ResAmo ?? new List<int>()),
                    RecrtN = new List<int>(RecrtN ?? new List<int>()),
                    ArmPow = new List<int>(ArmPow ?? new List<int>()),
                    NatPow = new List<int>(NatPow ?? new List<int>()),
                    GenExp = new List<int>(GenExp ?? new List<int>()),
                    FKill = new List<int>(FKill ?? new List<int>()),
                    FLost = new List<int>(FLost ?? new List<int>())
                };
            }

            public List<int> Series(int mode)
            {
                switch (mode)
                {
                    case 0: return SecNum;
                    case 1: return ResSup;
                    case 2: return ResAmo;
                    case 3: return RecrtN;
                    case 4: return ArmPow;
                    case 5: return NatPow;
                    case 6: return GenExp;
                    case 7: return FKill;
                    case 8: return FLost;
                    default: return SecNum;
                }
            }
        }

        [Serializable]
        public sealed class ProfileRecord
        {
            public int m_iId;
            public string m_chName = string.Empty;
            public int m_iNation;
            public int m_iDifficulty;
            public int m_iCurHeroId;

            public int m_iCurMX0;
            public int m_iCurMY0;
            public int m_iCurSecId;
            public int m_iCurMenuId;
            public int m_inCurActiveAI;
            public int m_inCurHeroMove;
            public int m_inCurTurn;

            public int m_iConCost;
            public bool DisableTutorialMessage;
            // CBigMapHelp visited pages (bits 0..4). Original help auto-opens once per BigMap page.
            public int bigMapHelpVisitedMask;
            public int c_Difficulty;
            public int c_NDone;

            // Total-War non-battle state currently ported.
            public int campaignNation = -1;
            public bool campaignInitialized;
            public int[] resources = new int[7]; // WOOD FOOD STONE GOLD IRON COAL RECRT
            public List<SectorState> sectors = new List<SectorState>();

            // Original CPlayerSAVE_STRUCT::STATS.  One CStatsSAVE per Big Map
            // nation slot, each series containing one sample per campaign turn.
            public int lastStatsUpdate = -1;
            public List<CampaignStatsRecord> stats = new List<CampaignStatsRecord>();

            public ProfileRecord Clone()
            {
                var r = (ProfileRecord)MemberwiseClone();
                r.resources = resources != null ? (int[])resources.Clone() : new int[7];
                r.sectors = new List<SectorState>();
                if (sectors != null)
                {
                    foreach (SectorState s in sectors)
                    {
                        if (s == null) continue;
                        r.sectors.Add(new SectorState
                        {
                            id = s.id,
                            owner = s.owner,
                            defence = s.defence,
                            population = s.population,
                            recruits = s.recruits,
                            resource = s.resource,
                            villages = s.villages
                        });
                    }
                }
                r.stats = new List<CampaignStatsRecord>();
                if (stats != null)
                {
                    foreach (CampaignStatsRecord st in stats)
                        if (st != null) r.stats.Add(st.Clone());
                }
                return r;
            }
        }

        private static readonly List<ProfileRecord> s_profiles = new List<ProfileRecord>();
        private static bool s_loaded;
        private static string s_savePath;

        public static IReadOnlyList<ProfileRecord> Profiles
        {
            get { EnsureLoaded(); return s_profiles; }
        }

        public static int Count
        {
            get { EnsureLoaded(); return s_profiles.Count; }
        }

        public static bool HasProfiles => Count > 0;

        public static ProfileRecord Current
        {
            get
            {
                EnsureLoaded();
                return s_profiles.Count > 0 ? s_profiles[0] : null;
            }
        }

        public static string SavePath
        {
            get
            {
                EnsureLoaded();
                return s_savePath;
            }
        }

        public static void EnsureLoaded()
        {
            if (s_loaded) return;
            s_loaded = true;

            string dir = Path.Combine(Application.persistentDataPath, "Save", "Battle");
            Directory.CreateDirectory(dir);
            s_savePath = Path.Combine(dir, "bmdat_bridge_v395.xml");

            s_profiles.Clear();
            if (!File.Exists(s_savePath))
            {
                Debug.Log($"[C2:PROFILE V395] no bridge save yet path='{s_savePath}'");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(s_savePath, Encoding.UTF8);
                ProfileRecord cur = null;
                SectorState sec = null;
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.StartsWith("<Profile ", StringComparison.Ordinal))
                    {
                        cur = new ProfileRecord
                        {
                            m_iId = AttrInt(line, "id", 0),
                            m_chName = Attr(line, "name"),
                            m_iNation = AttrInt(line, "nation", 0),
                            m_iDifficulty = AttrInt(line, "difficulty", 0),
                            m_iCurHeroId = AttrInt(line, "hero", 0),
                            m_iCurMX0 = AttrInt(line, "mx", 0),
                            m_iCurMY0 = AttrInt(line, "my", 0),
                            m_iCurSecId = AttrInt(line, "sector", 0),
                            m_iCurMenuId = AttrInt(line, "menu", 0),
                            m_inCurActiveAI = AttrInt(line, "activeAI", 0),
                            m_inCurHeroMove = AttrInt(line, "heroMove", 0),
                            m_inCurTurn = AttrInt(line, "turn", 0),
                            m_iConCost = AttrInt(line, "contractCost", 0),
                            c_Difficulty = AttrInt(line, "cDifficulty", 0),
                            c_NDone = AttrInt(line, "cDone", 0),
                            campaignNation = AttrInt(line, "campaignNation", -1),
                            campaignInitialized = AttrInt(line, "campaignInitialized", 0) != 0,
                            lastStatsUpdate = AttrInt(line, "lastStatsUpdate", -1),
                            DisableTutorialMessage = AttrInt(line, "disableTutorial", 0) != 0,
                            bigMapHelpVisitedMask = AttrInt(line, "helpMask", 0)
                        };
                        s_profiles.Add(cur);
                    }
                    else if (cur != null && line.StartsWith("<Resources ", StringComparison.Ordinal))
                    {
                        cur.resources = new[]
                        {
                            AttrInt(line,"wood",0), AttrInt(line,"food",0), AttrInt(line,"stone",0),
                            AttrInt(line,"gold",0), AttrInt(line,"iron",0), AttrInt(line,"coal",0),
                            AttrInt(line,"recruits",0)
                        };
                    }
                    else if (cur != null && line.StartsWith("<Sector ", StringComparison.Ordinal))
                    {
                        sec = new SectorState
                        {
                            id = AttrInt(line,"id",0), owner = AttrInt(line,"owner",0),
                            defence = AttrInt(line,"defence",0), population = AttrInt(line,"population",0),
                            recruits = AttrInt(line,"recruits",0), resource = AttrInt(line,"resource",0),
                            villages = AttrInt(line,"villages",0)
                        };
                        cur.sectors.Add(sec);
                    }
                    else if (cur != null && line.StartsWith("<Stats ", StringComparison.Ordinal))
                    {
                        var st = new CampaignStatsRecord
                        {
                            indx = AttrInt(line, "nation", 0),
                            SecNum = ParseIntList(Attr(line, "SecNum")),
                            ResSup = ParseIntList(Attr(line, "ResSup")),
                            ResAmo = ParseIntList(Attr(line, "ResAmo")),
                            RecrtN = ParseIntList(Attr(line, "RecrtN")),
                            ArmPow = ParseIntList(Attr(line, "ArmPow")),
                            NatPow = ParseIntList(Attr(line, "NatPow")),
                            GenExp = ParseIntList(Attr(line, "GenExp")),
                            FKill = ParseIntList(Attr(line, "FKill")),
                            FLost = ParseIntList(Attr(line, "FLost"))
                        };
                        cur.stats.Add(st);
                    }
                }

                Debug.Log($"[C2:PROFILE V395] loaded profiles={s_profiles.Count} path='{s_savePath}'");
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:PROFILE V395] load failed: " + ex.Message);
                s_profiles.Clear();
            }
        }

        public static ProfileRecord AddProfile(string name, int nation, int difficulty, int hero)
        {
            EnsureLoaded();
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) name = "Player";

            var p = new ProfileRecord
            {
                m_iId = GetNextId(),
                m_chName = name,
                m_iNation = Mathf.Max(0, nation),
                m_iDifficulty = Mathf.Max(0, difficulty),
                m_iCurHeroId = Mathf.Max(0, hero),
                c_Difficulty = Mathf.Max(0, difficulty),
                m_iCurSecId = 0,
                m_iCurMenuId = 0,
                m_inCurTurn = 0
            };

            // CPlayerSAVE::AddProfile inserts at the beginning.
            s_profiles.Insert(0, p);
            Save();
            Debug.Log($"[C2:PROFILE V395] AddProfile id={p.m_iId} name='{p.m_chName}' nation={p.m_iNation} diff={p.m_iDifficulty} hero={p.m_iCurHeroId}");
            return p;
        }

        public static bool SelectIndex(int index)
        {
            EnsureLoaded();
            if (index < 0 || index >= s_profiles.Count) return false;
            if (index > 0)
            {
                ProfileRecord p = s_profiles[index];
                s_profiles.RemoveAt(index);
                s_profiles.Insert(0, p);
            }
            Save();
            Debug.Log($"[C2:PROFILE V395] CurPlayer='{s_profiles[0].m_chName}' index0 id={s_profiles[0].m_iId}");
            return true;
        }

        public static bool DeleteIndex(int index)
        {
            EnsureLoaded();
            if (index < 0 || index >= s_profiles.Count) return false;
            string n = s_profiles[index].m_chName;
            s_profiles.RemoveAt(index);

            // V396A7R2 source parity: CPlayerSAVE::DeleteProfile removes only
            // from the in-memory array. SaveXML is performed by the OUTER profile
            // Accept action. The only immediate-save case is deletion of the last
            // profile, handled by cva_ProfDel_Accept in MenuActionSink.
            Debug.Log($"[C2:PROFILE V396A7R2] DeleteProfile transient name='{n}' remaining={s_profiles.Count} persisted=NO");
            return true;
        }

        public static void ReloadFromDisk()
        {
            // Mirrors Profiles->reset_class(Profiles); Profiles->LoadXML(); used by
            // original cva_ProfSel_Cancel to undo uncommitted profile edits/deletes.
            s_loaded = false;
            EnsureLoaded();
            Debug.Log($"[C2:PROFILE V396A7R2] ReloadFromDisk profiles={s_profiles.Count}");
        }

        public static void SaveCurrent()
        {
            Save();
        }

        public static void Save()
        {
            EnsureLoaded();
            try
            {
                var sb = new StringBuilder(8192);
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.AppendLine($"<CPlayerSAVE pr=\"{s_profiles.Count}\" source=\"C2BridgeV395\">");
                foreach (ProfileRecord p in s_profiles)
                {
                    sb.Append("  <Profile")
                      .Append(" id=\"").Append(p.m_iId).Append('"')
                      .Append(" name=\"").Append(Xml(p.m_chName)).Append('"')
                      .Append(" nation=\"").Append(p.m_iNation).Append('"')
                      .Append(" difficulty=\"").Append(p.m_iDifficulty).Append('"')
                      .Append(" hero=\"").Append(p.m_iCurHeroId).Append('"')
                      .Append(" mx=\"").Append(p.m_iCurMX0).Append('"')
                      .Append(" my=\"").Append(p.m_iCurMY0).Append('"')
                      .Append(" sector=\"").Append(p.m_iCurSecId).Append('"')
                      .Append(" menu=\"").Append(p.m_iCurMenuId).Append('"')
                      .Append(" activeAI=\"").Append(p.m_inCurActiveAI).Append('"')
                      .Append(" heroMove=\"").Append(p.m_inCurHeroMove).Append('"')
                      .Append(" turn=\"").Append(p.m_inCurTurn).Append('"')
                      .Append(" contractCost=\"").Append(p.m_iConCost).Append('"')
                      .Append(" cDifficulty=\"").Append(p.c_Difficulty).Append('"')
                      .Append(" cDone=\"").Append(p.c_NDone).Append('"')
                      .Append(" campaignNation=\"").Append(p.campaignNation).Append('"')
                      .Append(" campaignInitialized=\"").Append(p.campaignInitialized ? 1 : 0).Append('"')
                      .Append(" lastStatsUpdate=\"").Append(p.lastStatsUpdate).Append('"')
                      .Append(" disableTutorial=\"").Append(p.DisableTutorialMessage ? 1 : 0).Append('"')
                      .Append(" helpMask=\"").Append(p.bigMapHelpVisitedMask).AppendLine("\">");

                    int[] r = p.resources ?? new int[7];
                    sb.Append("    <Resources")
                      .Append(" wood=\"").Append(GetRes(r,0)).Append('"')
                      .Append(" food=\"").Append(GetRes(r,1)).Append('"')
                      .Append(" stone=\"").Append(GetRes(r,2)).Append('"')
                      .Append(" gold=\"").Append(GetRes(r,3)).Append('"')
                      .Append(" iron=\"").Append(GetRes(r,4)).Append('"')
                      .Append(" coal=\"").Append(GetRes(r,5)).Append('"')
                      .Append(" recruits=\"").Append(GetRes(r,6)).AppendLine("\" />");

                    if (p.sectors != null)
                    {
                        foreach (SectorState s in p.sectors)
                        {
                            if (s == null) continue;
                            sb.Append("    <Sector")
                              .Append(" id=\"").Append(s.id).Append('"')
                              .Append(" owner=\"").Append(s.owner).Append('"')
                              .Append(" defence=\"").Append(s.defence).Append('"')
                              .Append(" population=\"").Append(s.population).Append('"')
                              .Append(" recruits=\"").Append(s.recruits).Append('"')
                              .Append(" resource=\"").Append(s.resource).Append('"')
                              .Append(" villages=\"").Append(s.villages).AppendLine("\" />");
                        }
                    }
                    if (p.stats != null)
                    {
                        foreach (CampaignStatsRecord st in p.stats)
                        {
                            if (st == null) continue;
                            sb.Append("    <Stats")
                              .Append(" nation=\"").Append(st.indx).Append('"')
                              .Append(" SecNum=\"").Append(JoinInts(st.SecNum)).Append('"')
                              .Append(" ResSup=\"").Append(JoinInts(st.ResSup)).Append('"')
                              .Append(" ResAmo=\"").Append(JoinInts(st.ResAmo)).Append('"')
                              .Append(" RecrtN=\"").Append(JoinInts(st.RecrtN)).Append('"')
                              .Append(" ArmPow=\"").Append(JoinInts(st.ArmPow)).Append('"')
                              .Append(" NatPow=\"").Append(JoinInts(st.NatPow)).Append('"')
                              .Append(" GenExp=\"").Append(JoinInts(st.GenExp)).Append('"')
                              .Append(" FKill=\"").Append(JoinInts(st.FKill)).Append('"')
                              .Append(" FLost=\"").Append(JoinInts(st.FLost)).AppendLine("\" />");
                        }
                    }
                    sb.AppendLine("  </Profile>");
                }
                sb.AppendLine("</CPlayerSAVE>");
                File.WriteAllText(s_savePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:PROFILE V395] save failed: " + ex.Message);
            }
        }

        public static void EnsureStatsRecords(ProfileRecord p, int count = 9)
        {
            if (p == null) return;
            if (p.stats == null) p.stats = new List<CampaignStatsRecord>();
            while (p.stats.Count < count)
                p.stats.Add(new CampaignStatsRecord { indx = p.stats.Count });
            for (int i = 0; i < p.stats.Count; i++)
                if (p.stats[i] == null) p.stats[i] = new CampaignStatsRecord { indx = i };
        }

        public static CampaignStatsRecord GetStats(ProfileRecord p, int nation)
        {
            if (p == null || nation < 0) return null;
            EnsureStatsRecords(p, Math.Max(9, nation + 1));
            return nation < p.stats.Count ? p.stats[nation] : null;
        }

        public static List<int> GetStatsSeries(ProfileRecord p, int nation, int mode)
        {
            CampaignStatsRecord st = GetStats(p, nation);
            return st != null ? st.Series(mode) : null;
        }

        private static List<int> ParseIntList(string s)
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(s)) return list;
            string[] parts = s.Split(',');
            for (int i = 0; i < parts.Length; i++)
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) list.Add(v);
            return list;
        }

        private static string JoinInts(List<int> values)
        {
            if (values == null || values.Count == 0) return string.Empty;
            return string.Join(",", values);
        }

        private static int GetNextId()
        {
            int max = -1;
            foreach (ProfileRecord p in s_profiles) if (p != null && p.m_iId > max) max = p.m_iId;
            return max + 1;
        }

        private static int GetRes(int[] r, int i) => r != null && i >= 0 && i < r.Length ? r[i] : 0;

        private static string Xml(string s)
        {
            return (s ?? string.Empty).Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string Attr(string line, string name)
        {
            string key = name + "=\"";
            int p = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (p < 0) return string.Empty;
            p += key.Length;
            int e = line.IndexOf('"', p);
            if (e < 0) return string.Empty;
            return line.Substring(p, e - p)
                .Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
        }

        private static int AttrInt(string line, string name, int fallback)
        {
            string s = Attr(line, name);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
        }
    }
}
