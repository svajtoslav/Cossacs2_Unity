using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.Core.Loaders;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TemnyLessCodec;
using Cossacks2Bridge.UnityAdapters.Profiles;
using Cossacks2Bridge.UnityAdapters.Renderers;
using Cossacks2Bridge.UnityAdapters.BigMap;

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// V393: shared SetFrameState/Init runtime for menu XML actions with explicit C2 1.4 nation-domain separation.
    ///
    /// V394 rule: XML stays bound to its clean14 source, but final 1.4 GlobalAI
    /// data is resolved independently.  A validated 9-nation DataRoot Ai\Ai.dat
    /// overrides the bundled six-row base table, matching the final installed
    /// 1.4 runtime rather than truncating AddProfile to six nations.
    /// </summary>
    public static class Menu14ActionStateRuntime
    {
        private sealed class BoundControl
        {
            public UiNode Node;
            public GameObject GameObject;
        }

        public sealed class NationRecord
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

            public override string ToString()
            {
                return $"{Index}:{Id}->{NWaterAI}";
            }
        }

        private static readonly Dictionary<string, List<BoundControl>> s_byAction =
            new Dictionary<string, List<BoundControl>>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<NationRecord> s_aiNations = new List<NationRecord>();
        private static readonly List<string> s_aiDiffKeys = new List<string>();
        private static readonly Dictionary<string, int> s_heroInfoIndex =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static CoreFileSystem s_fs;
        private static UiDesk s_desk;
        private static LocDb s_loc;
        private static Menu14SourceContext s_sourceContext;
        private static string s_aiLoadedKey = string.Empty;
        private static string s_selfTestedKey = string.Empty;
        private static bool s_aiLoadAttempted;
        private static int s_aiNComp;
        private static int s_profileNationIndex;
        private static int s_profileDifficultyIndex;
        private static int s_campaignStatsMode;
        private static int s_campaignStatsDy = 1;
        private static string s_logicalDataRoot = string.Empty;
        private static readonly Dictionary<string, Sprite> s_gpRuntimeSpriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> s_addTextFallbacks;

        public static event Action<int> ProfileNationChanged;

        public static int CurrentProfileNationIndex => s_profileNationIndex;
        public static int CurrentProfileDifficultyIndex => s_profileDifficultyIndex;
        public static string CurrentSourceBundleId => s_sourceContext?.BundleId ?? string.Empty;
        public static string CurrentSourceDataRoot => s_sourceContext?.DataRoot ?? string.Empty;
        public static string CurrentLogicalDataRoot => s_logicalDataRoot;
        public static IReadOnlyList<NationRecord> Nations => s_aiNations;

        public static void BeginScreen(UiDesk desk, CoreFileSystem fs, LocDb loc = null)
        {
            s_desk = desk;
            s_fs = fs;
            s_loc = loc;
            s_sourceContext = Menu14SourceContext.FromDesk(desk, fs);
            s_byAction.Clear();
            s_profileNationIndex = 0;
            s_profileDifficultyIndex = 0;
            s_campaignStatsMode = 0;
            s_campaignStatsDy = 1;

            // V395J: restore the original ListDesk-owned <Element> prototype
            // as a generic per-screen source model before SetFrameState actions run.
            ListDeskSourceRuntime14.BeginScreen(desk);

            if (DeskNeedsAiState(desk))
                EnsureAiDatLoaded();
        }

        private static bool DeskNeedsAiState(UiDesk desk)
        {
            if (desk?.Children == null) return false;
            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (n?.Actions == null) continue;
                for (int j = 0; j < n.Actions.Count; j++)
                {
                    string a = n.Actions[j]?.Name ?? string.Empty;
                    if (a.StartsWith("cva_CampStat_", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfAdd_Race", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfAdd_RaceFlg", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfAdd_Diff", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfCur_Name", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfCur_Race", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfCur_Diff", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfCur_Port", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfCur_Ocup", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("cva_ProfCur_Desc", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        public static void RegisterControl(UiNode node, GameObject go, CoreFileSystem fs = null)
        {
            if (node == null || go == null) return;
            if (fs != null) s_fs = fs;
            if (node.Actions == null || node.Actions.Count == 0) return;

            for (int i = 0; i < node.Actions.Count; i++)
            {
                UiAction action = node.Actions[i];
                if (action == null || string.IsNullOrWhiteSpace(action.Name)) continue;

                if (!s_byAction.TryGetValue(action.Name, out List<BoundControl> list))
                {
                    list = new List<BoundControl>();
                    s_byAction[action.Name] = list;
                }

                list.Add(new BoundControl { Node = node, GameObject = go });
            }
        }

        public static bool TryGetBoundControlForAction(string actionName, out UiNode node, out GameObject go)
        {
            node = null;
            go = null;
            if (string.IsNullOrWhiteSpace(actionName)) return false;
            if (!s_byAction.TryGetValue(actionName, out List<BoundControl> list) || list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                BoundControl b = list[i];
                if (b == null || b.Node == null || b.GameObject == null) continue;
                node = b.Node;
                go = b.GameObject;
                return true;
            }
            return false;
        }

        public static List<string> GetNationDisplayNames(LocDb loc)
        {
            // C2 1.4 profile roster is the GlobalAI roster loaded from Ai\\Ai.dat.
            // The separate nine-entry engine nation-name registry is NOT substituted here.
            EnsureAiDatLoaded();
            var items = new List<string>(s_aiNations.Count);
            for (int i = 0; i < s_aiNations.Count; i++)
            {
                NationRecord n = s_aiNations[i];
                items.Add(C2Version14Context.ResolveDataNationLabel(
                    s_logicalDataRoot, loc, n.Id, n.Message));
            }
            return items;
        }

        public static List<string> GetDifficultyDisplayNames(LocDb loc)
        {
            EnsureAiDatLoaded();
            var items = new List<string>(s_aiDiffKeys.Count);
            for (int i = 0; i < s_aiDiffKeys.Count; i++)
            {
                string key = s_aiDiffKeys[i];
                string value = loc != null ? loc.Resolve(key) : key;
                items.Add(string.IsNullOrEmpty(value) ? key : value);
            }
            return items;
        }

        public static List<string> GetCampaignStatsModeDisplayNames(LocDb loc)
        {
            string[] keys =
            {
                "#EW2_sectors_num",
                "#EW2_supply",
                "#EW2_resources_amount",
                "#EW2_recruits_number",
                "#EW2_power_of_army",
                "#EW2_power_of_nation",
                "#EW2_commander_exp_amount",
                "#EW2_formation_killed",
                "#EW2_formation_lost"
            };
            var items = new List<string>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                string t = loc != null ? loc.Resolve(keys[i]) : keys[i];
                items.Add(string.IsNullOrEmpty(t) ? keys[i] : t);
            }
            return items;
        }

        public static bool TryGetNationRecord(int index, out NationRecord nation)
        {
            EnsureAiDatLoaded();
            if (index >= 0 && index < s_aiNations.Count)
            {
                nation = s_aiNations[index];
                return true;
            }
            nation = null;
            return false;
        }

        public static int GetHeroInfoNationIndex(int nationIndex)
        {
            if (!TryGetNationRecord(nationIndex, out NationRecord nation))
                return Math.Max(0, nationIndex);

            EnsureHeroInfoMapLoaded();
            if (!string.IsNullOrEmpty(nation.HeroCode) &&
                s_heroInfoIndex.TryGetValue(nation.HeroCode, out int mapped))
                return mapped;

            return Math.Max(0, nationIndex);
        }

        public static string ReadSourceText(string relativePath, Encoding encoding = null)
        {
            EnsureAiDatLoaded();
            string text = C2Version14Context.ReadTextFromRoot(s_logicalDataRoot, relativePath, encoding);
            if (!string.IsNullOrEmpty(text))
                return text;
            return s_sourceContext != null
                ? s_sourceContext.ReadAllText(relativePath, encoding)
                : string.Empty;
        }

        /// <summary>
        /// V395P: initialize an XML ComboBox without treating construction as a
        /// user change.  The original cva_CampStat_Mode::Init only sets CurLine=0;
        /// SetFrameState is performed later by the normal frame-state pass.
        /// This prevents EW2 statistics from being fully evaluated twice while
        /// preserving the old initialization behavior for every other ComboBox.
        /// </summary>
        public static bool InitializeComboSelection(UiComboBox box, int selectedIndex)
        {
            if (box == null || box.Actions == null) return false;

            for (int i = 0; i < box.Actions.Count; i++)
            {
                UiAction action = box.Actions[i];
                if (action == null || string.IsNullOrWhiteSpace(action.Name)) continue;
                if (!action.Name.Equals("cva_CampStat_Mode", StringComparison.OrdinalIgnoreCase)) continue;

                s_campaignStatsMode = Mathf.Clamp(selectedIndex, 0, 8);
                Debug.Log($"[C2:CAMPSTAT PERF V395P] combo-init mode={s_campaignStatsMode} deferredRefresh=1");
                return true;
            }

            return OnComboSelectionChanged(box, selectedIndex);
        }

        /// <summary>
        /// Called by generic ComboBox rendering when CurLine changes.
        /// </summary>
        public static bool OnComboSelectionChanged(UiComboBox box, int selectedIndex)
        {
            if (box == null || box.Actions == null) return false;

            bool handled = false;
            for (int i = 0; i < box.Actions.Count; i++)
            {
                UiAction action = box.Actions[i];
                if (action == null || string.IsNullOrWhiteSpace(action.Name)) continue;

                if (action.Name.Equals("cva_ProfAdd_Race", StringComparison.OrdinalIgnoreCase))
                {
                    handled = true;
                    EnsureAiDatLoaded();

                    int index = selectedIndex;
                    // Original cva_ProfAdd_Race::SetFrameState:
                    // if(CB->CurLine>=GlobalAI.NComp) CB->CurLine=0;
                    if (index < 0 || (s_aiNComp > 0 && index >= s_aiNComp))
                        index = 0;
                    if (s_aiNations.Count > 0)
                        index = Mathf.Clamp(index, 0, s_aiNations.Count - 1);
                    else
                        index = Math.Max(0, index);

                    s_profileNationIndex = index;
                    ApplyActionFrameState("cva_ProfAdd_RaceFlg");
                    ProfileNationChanged?.Invoke(index);
                }
                else if (action.Name.Equals("cva_ProfAdd_Diff", StringComparison.OrdinalIgnoreCase))
                {
                    handled = true;
                    s_profileDifficultyIndex = Math.Max(0, selectedIndex);
                }
                else if (action.Name.Equals("cva_CampStat_Mode", StringComparison.OrdinalIgnoreCase))
                {
                    handled = true;
                    s_campaignStatsMode = Mathf.Clamp(selectedIndex, 0, 8);
                    ApplyCampaignStatsFrameStates();
                }
            }

            return handled;
        }

        public static void ApplyAllFrameStates()
        {
            ApplyActionFrameState("cva_ProfAdd_RaceFlg");

            // Original cva_ProfList runs on the XML ListDesk itself, populates
            // LD->DSS with copies of its Element template, sets CurrentElement,
            // then va_ListDesk applies State=1 to the selected VitButton.
            ApplyActionFrameState("cva_ProfList");

            // V395C: these are the original SetFrameState actions for the
            // existing XML controls. No replacement TMP/Image layer is created.
            ApplyActionFrameState("cva_ProfCur_Name");
            ApplyActionFrameState("cva_ProfCur_Race");
            ApplyActionFrameState("cva_ProfCur_Diff");
            ApplyActionFrameState("cva_ProfCur_Port");
            ApplyActionFrameState("cva_ProfCur_Ocup");
            ApplyActionFrameState("cva_ProfCur_Desc");

            // Original VUI_Actions.cpp:
            // cva_ProfDel_Desk::SetFrameState => SD->Visible=vCurProfDel.
            // This call was missing in V395/V396 and caused DeletePending to disable
            // the normal buttons without ever showing the confirmation desk.
            ApplyActionFrameState("cva_ProfDel_Desk");

            // Original VUI_Actions.cpp disables these three controls while
            // vCurProfDel is active. Keep the XML buttons and change only their
            // Enabled state, exactly like SetFrameState.
            ApplyActionFrameState("cva_ProfSel_Accept");
            ApplyActionFrameState("cva_ProfSel_Add");
            ApplyActionFrameState("cva_ProfSel_Cancel");

            // EW2 campaign statistics.  These actions are order-dependent in the
            // original (cva_CampStat_Player establishes the slot for the child
            // controls), so they are processed together instead of action-by-action.
            ApplyCampaignStatsFrameStates();
        }

        private const string CampaignStatsDynamicRoot = "C2_CampaignStatsDynamic_V395L";

        private static bool IsCampaignStatsScreen()
        {
            string src = s_desk?.SourcePath ?? string.Empty;
            return src.IndexOf("EW2_CampaignStats", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ApplyCampaignStatsFrameStates()
        {
            if (!IsCampaignStatsScreen() || s_fs == null) return;

            long perfStartV395P = System.Diagnostics.Stopwatch.GetTimestamp();
            EnsureAiDatLoaded();
            C2ProfileRuntime14.EnsureLoaded();
            C2ProfileRuntime14.ProfileRecord profile = C2ProfileRuntime14.Current;
            if (profile == null) return;

            C2BigMapData14.Data data = C2BigMapData14.Load(s_fs);
            // Viewing statistics must not start/initialize a campaign.  Older bridge
            // profiles created before V395L can, however, already own valid campaign
            // state but have no STATS history yet; migrate one current-turn sample
            // from that existing state without changing the campaign itself.
            bool hasStats = profile.stats != null && profile.stats.Count > 0 &&
                            profile.stats.Exists(st => st != null && st.SecNum != null && st.SecNum.Count > 0);
            if (profile.campaignInitialized && !hasStats && C2BigMapData14.CaptureStatisticsSnapshot(profile, data))
                C2ProfileRuntime14.SaveCurrent();

            ResolveCampaignStatsTextFallbacks();
            ApplyCampaignStatsNames();
            ApplyCampaignStatsFlags();
            ApplyCampaignStatsScores(profile);

            int max = ComputeCampaignStatsMax(profile);
            if (max > 0)
            {
                if ((max % 7) > 0) max = (max / 7 + 1) * 7;
                s_campaignStatsDy = Math.Max(1, max / 7);
            }
            ApplyCampaignStatsOy();
            DrawCampaignStatsDynamic(profile, data, max);

            int visiblePlayers = 0;
            if (s_byAction.TryGetValue("cva_CampStat_PlName", out List<BoundControl> playerNameControls) && playerNameControls != null)
            {
                for (int i = 0; i < playerNameControls.Count; i++)
                    if (playerNameControls[i]?.GameObject != null && playerNameControls[i].GameObject.activeInHierarchy) visiblePlayers++;
            }
            Debug.Log($"[C2:CAMPSTAT ROSTER V395N] visiblePlayers={visiblePlayers} expected={C2Bfe14ContractV396A.CountryCount} layout=3x3 columnsX=83/387/691 ids=0..8");

            int points = 0;
            // Final engine_1.4.exe loops 9 statistic slots in GetMaxStats.
            for (int i = 0; i < C2Bfe14ContractV396A.CountryCount; i++)
            {
                List<int> a = C2ProfileRuntime14.GetStatsSeries(profile, i, s_campaignStatsMode);
                if (a != null && a.Count > points) points = a.Count;
            }
            Debug.Log($"[C2:CAMPSTAT V395N] mode={s_campaignStatsMode} max={max} dy={s_campaignStatsDy} points={points} " +
                      $"profile='{profile.m_chName}' turn={profile.m_inCurTurn} exit=cva_MM_Cancel");
            double perfMsV395P = (System.Diagnostics.Stopwatch.GetTimestamp() - perfStartV395P) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Debug.Log($"[C2:CAMPSTAT PERF V395P] frame-pass=1 elapsedMs={perfMsV395P:F2}");
        }

        private static void ResolveCampaignStatsTextFallbacks()
        {
            GameObject canvas = GameObject.Find("C2_OptionsCanvas");
            if (canvas == null) return;
            if (s_addTextFallbacks == null)
            {
                s_addTextFallbacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string raw = ReadSourceText(@"Text\add\text1.txt", Cp1251());
                if (!string.IsNullOrEmpty(raw))
                {
                    using (var sr = new StringReader(raw))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.Length == 0 || line[0] != '#') continue;
                            int ws = 0;
                            while (ws < line.Length && !char.IsWhiteSpace(line[ws])) ws++;
                            if (ws <= 0 || ws >= line.Length) continue;
                            string key = line.Substring(0, ws).Trim();
                            string val = line.Substring(ws).Trim();
                            if (key.Length > 0 && val.Length > 0) s_addTextFallbacks[key] = val;
                        }
                    }
                }
            }

            TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI t = texts[i];
                string key = t != null ? (t.text ?? string.Empty).Trim() : string.Empty;
                if (key.Length > 1 && key[0] == '#' && s_addTextFallbacks.TryGetValue(key, out string value))
                    t.text = value;
            }
        }

        private static void ApplyCampaignStatsNames()
        {
            if (!s_byAction.TryGetValue("cva_CampStat_PlName", out List<BoundControl> list) || list == null) return;
            // Final 1.4 shows all nine campaign nations in three 3-row tables.
            // The base Data1 XML only had six visible rows; V395N supplies the
            // final 1.4 three-column XML override. Hidden fourth-row templates
            // stay inactive and are skipped here.
            int visibleSlot = 0;
            for (int i = 0; i < list.Count && visibleSlot < C2Bfe14ContractV396A.CountryCount; i++)
            {
                BoundControl b = list[i];
                if (b?.GameObject == null || !b.GameObject.activeInHierarchy) continue;
                TextMeshProUGUI t = GetBoundText(b);
                if (t == null || !TryGetNationRecord(visibleSlot, out NationRecord nat) || nat == null) continue;
                t.text = C2Version14Context.ResolveDataNationLabel(s_logicalDataRoot, s_loc, nat.Id, nat.Message);
                visibleSlot++;
            }
        }

        private static void ApplyCampaignStatsFlags()
        {
            if (!s_byAction.TryGetValue("cva_CampStat_PlFlag", out List<BoundControl> list) || list == null) return;
            int visibleSlot = 0;
            for (int i = 0; i < list.Count && visibleSlot < C2Bfe14ContractV396A.CountryCount; i++)
            {
                BoundControl b = list[i];
                if (b?.GameObject == null || !b.GameObject.activeInHierarchy) continue;
                UiGPPicture gp = b.Node as UiGPPicture;
                Image image = b.GameObject.GetComponent<Image>();
                if (gp == null || image == null || !TryGetNationRecord(visibleSlot, out NationRecord nat) || nat == null) continue;
                int spriteId = nat.NWaterAI;
                Sprite sp = LoadGpSprite(gp.FileID, spriteId);
                if (sp != null)
                {
                    gp.SpriteID = spriteId;
                    image.sprite = sp;
                    image.enabled = true;
                    image.color = Color.white;
                    image.preserveAspect = true;
                }
                visibleSlot++;
            }
        }

        private static void ApplyCampaignStatsScores(C2ProfileRuntime14.ProfileRecord profile)
        {
            if (!s_byAction.TryGetValue("cva_CampStat_PlScore", out List<BoundControl> list) || list == null) return;
            int visibleSlot = 0;
            for (int i = 0; i < list.Count && visibleSlot < C2Bfe14ContractV396A.CountryCount; i++)
            {
                BoundControl b = list[i];
                if (b?.GameObject == null || !b.GameObject.activeInHierarchy) continue;
                TextMeshProUGUI t = GetBoundText(b);
                if (t == null) continue;
                int nid = C2BigMapData14.CampaignNationFromGlobalAiIndex(visibleSlot);
                List<int> a = C2ProfileRuntime14.GetStatsSeries(profile, nid, s_campaignStatsMode);
                t.text = a != null && a.Count > 0 ? a[a.Count - 1].ToString() : "0";
                visibleSlot++;
            }
        }

        private static int ComputeCampaignStatsMax(C2ProfileRuntime14.ProfileRecord profile)
        {
            int m = 0;
            for (int i = 0; i < C2Bfe14ContractV396A.CountryCount; i++)
            {
                List<int> a = C2ProfileRuntime14.GetStatsSeries(profile, i, s_campaignStatsMode);
                if (a == null || a.Count == 0) continue;
                if (s_campaignStatsMode != 0)
                {
                    for (int j = 0; j < a.Count; j++) if (m < a[j]) m = a[j];
                }
                else
                {
                    // Original GetMaxStats() for SecNum sums the first sample.
                    m += a[0];
                }
            }
            return m * 11 / 10;
        }

        private static void ApplyCampaignStatsOy()
        {
            if (!s_byAction.TryGetValue("cva_CampStat_Oy", out List<BoundControl> list) || list == null) return;
            int n = Math.Min(6, list.Count);
            // cva_CampStat_Oy::SetFrameState rounds only the displayed tick step;
            // graph scaling keeps the unrounded dy=Max/7.
            int displayDy = s_campaignStatsDy;
            if (displayDy >= 10)
            {
                if (displayDy > 1000) displayDy = displayDy / 50 * 50;
                else if (displayDy > 100) displayDy = displayDy / 5 * 5;
            }
            for (int i = 0; i < n; i++)
            {
                TextMeshProUGUI t = GetBoundText(list[i]);
                if (t != null) t.text = ((i + 1) * displayDy).ToString();
            }
        }

        private static void DrawCampaignStatsDynamic(C2ProfileRuntime14.ProfileRecord profile, C2BigMapData14.Data data, int max)
        {
            GameObject canvasGo = GameObject.Find("C2_OptionsCanvas");
            if (canvasGo == null) return;
            RectTransform root = canvasGo.GetComponent<RectTransform>();
            if (root == null) return;

            Transform old = root.Find(CampaignStatsDynamicRoot);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            var dyn = new GameObject(CampaignStatsDynamicRoot, typeof(RectTransform));
            dyn.transform.SetParent(root, false);
            RectTransform drt = (RectTransform)dyn.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.anchoredPosition = Vector2.zero;
            drt.sizeDelta = new Vector2(1024f, 768f);

            // The unified parser reports exactly seven generic controls for this
            // screen: six ColoredBar nodes and one Canvas.  They still retain the
            // original UiNode geometry/actions, so render those parsed coordinates
            // instead of duplicating the XML numbers here.
            var colorNodes = new List<UiNode>();
            UiNode graphNode = null;
            if (s_desk?.Children != null)
            {
                for (int i = 0; i < s_desk.Children.Count; i++)
                {
                    UiNode n = s_desk.Children[i];
                    if (n == null || !n.Visible) continue;
                    if (NodeHasAction(n, "cva_CampStat_Color")) colorNodes.Add(n);
                    else if (graphNode == null && NodeHasAction(n, "cva_CampStat_Graphs")) graphNode = n;
                }
            }

            int bars = Math.Min(C2Bfe14ContractV396A.CountryCount, colorNodes.Count);
            Debug.Log($"[C2:CAMPSTAT XML V395N] genericBars={colorNodes.Count} graph={(graphNode != null ? 1 : 0)} " +
                      (graphNode != null ? $"canvas=({graphNode.X:0.#},{graphNode.Y:0.#},{graphNode.Width:0.#},{graphNode.Height:0.#})" : "canvas=missing"));
            for (int ai = 0; ai < bars; ai++)
            {
                UiNode n = colorNodes[ai];
                int cid = C2BigMapData14.CampaignNationFromGlobalAiIndex(ai);
                Color32 col = C2BigMapData14.CampaignMapColor(data, cid);
                CreateCampaignStatsRect(drt, "CampStatColor_" + ai, n.X, n.Y, n.Width, n.Height, col);
            }

            if (max <= 0 || graphNode == null) return;

            float graphW = graphNode.Width;
            float graphH = graphNode.Height;
            var graph = new GameObject("CampStatGraph", typeof(RectTransform));
            graph.transform.SetParent(drt, false);
            RectTransform grt = (RectTransform)graph.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0f, 1f);
            grt.pivot = new Vector2(0f, 1f);
            grt.anchoredPosition = new Vector2(graphNode.X, -graphNode.Y);
            grt.sizeDelta = new Vector2(graphW, graphH);

            // Final engine_1.4.exe loops nine GlobalAI/stat slots here.
            for (int j = 0; j < C2Bfe14ContractV396A.CountryCount; j++)
            {
                int cid = C2BigMapData14.CampaignNationFromGlobalAiIndex(j);
                List<int> a = C2ProfileRuntime14.GetStatsSeries(profile, cid, s_campaignStatsMode);
                if (a == null || a.Count < 2) continue;

                int dx = 23;
                if (dx * a.Count > 800) dx = Math.Max(1, 800 / a.Count);
                Color32 col = C2BigMapData14.CampaignMapColor(data, cid);

                for (int i = 1; i < a.Count; i++)
                {
                    float x0 = (i - 1) * dx;
                    float y0 = graphH - a[i - 1] * graphH / max;
                    float x1 = i * dx;
                    float y1 = graphH - a[i] * graphH / max;
                    CreateCampaignStatsLine(grt, $"P{j}_S{i}", x0, y0, x1, y1, 5f, col);
                }
            }
        }

        private static bool NodeHasAction(UiNode node, string actionName)
        {
            if (node?.Actions == null || string.IsNullOrEmpty(actionName)) return false;
            for (int i = 0; i < node.Actions.Count; i++)
            {
                UiAction a = node.Actions[i];
                if (a != null && string.Equals(a.Name, actionName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static void CreateCampaignStatsRect(RectTransform parent, string name, float x, float y, float w, float h, Color32 color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            Image im = go.GetComponent<Image>();
            im.color = color;
            im.raycastTarget = false;
        }

        private static void CreateCampaignStatsLine(RectTransform parent, string name, float x0, float y0, float x1, float y1, float thickness, Color32 color)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len <= 0.001f) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x0, -y0);
            rt.sizeDelta = new Vector2(len, thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg);
            Image im = go.GetComponent<Image>();
            im.color = color;
            im.raycastTarget = false;
        }

        private static void ApplyActionFrameState(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return;
            if (!s_byAction.TryGetValue(actionName, out List<BoundControl> list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                BoundControl bound = list[i];
                if (bound == null || bound.Node == null || bound.GameObject == null) continue;

                if (actionName.Equals("cva_ProfAdd_RaceFlg", StringComparison.OrdinalIgnoreCase))
                    ApplyProfAddRaceFlag(bound);
                else if (actionName.Equals("cva_ProfList", StringComparison.OrdinalIgnoreCase))
                    ApplyProfList(bound);
                else if (actionName.Equals("cva_ProfCur_Name", StringComparison.OrdinalIgnoreCase))
                    ApplyProfCurName(bound);
                else if (actionName.Equals("cva_ProfCur_Race", StringComparison.OrdinalIgnoreCase))
                    ApplyProfCurRace(bound);
                else if (actionName.Equals("cva_ProfCur_Diff", StringComparison.OrdinalIgnoreCase))
                    ApplyProfCurDiff(bound);
                else if (actionName.Equals("cva_ProfCur_Port", StringComparison.OrdinalIgnoreCase))
                    ApplyProfCurPort(bound);
                else if (actionName.Equals("cva_ProfCur_Ocup", StringComparison.OrdinalIgnoreCase))
                    ApplyProfCurOcup(bound);
                else if (actionName.Equals("cva_ProfCur_Desc", StringComparison.OrdinalIgnoreCase))
                    ApplyProfCurDesc(bound);
                else if (actionName.Equals("cva_ProfDel_Desk", StringComparison.OrdinalIgnoreCase))
                    ApplyProfDelDesk(bound);
                else if (actionName.Equals("cva_ProfSel_Accept", StringComparison.OrdinalIgnoreCase) ||
                         actionName.Equals("cva_ProfSel_Add", StringComparison.OrdinalIgnoreCase) ||
                         actionName.Equals("cva_ProfSel_Cancel", StringComparison.OrdinalIgnoreCase))
                    ApplyProfSelEnabled(bound);
            }
        }

        /// <summary>
        /// Original source behavior:
        /// GP->SetSpriteID(GlobalAI.Ai[vNewProf.m_iNation].NWaterAI);
        /// </summary>
        private static void ApplyProfAddRaceFlag(BoundControl bound)
        {
            UiGPPicture gp = bound.Node as UiGPPicture;
            if (gp == null) return;

            EnsureAiDatLoaded();
            if (s_aiNations.Count == 0)
            {
                Debug.LogError("[C2:STATE V394B] cva_ProfAdd_RaceFlg blocked: source-bound AI\\ai.dat has no nations");
                return;
            }

            int nationIndex = Mathf.Clamp(s_profileNationIndex, 0, s_aiNations.Count - 1);
            NationRecord nation = s_aiNations[nationIndex];
            int spriteId = nation.NWaterAI;

            Image image = bound.GameObject.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning($"[C2:STATE V394B] cva_ProfAdd_RaceFlg sourceId={gp.SourceId} has no Image");
                return;
            }

            Sprite sprite = LoadGpSprite(gp.FileID, spriteId);
            if (sprite == null)
            {
                image.enabled = false;
                Debug.LogError(
                    $"[C2:STATE V394B] cva_ProfAdd_RaceFlg sourceId={gp.SourceId} nationIndex={nationIndex} " +
                    $"nation='{nation.Id}' NWaterAI={spriteId} gp='{gp.FileID}' sprite=missing");
                return;
            }

            gp.SpriteID = spriteId;
            image.sprite = sprite;
            image.enabled = true;
            image.color = Color.white;
            image.preserveAspect = true;

            Debug.Log(
                $"[C2:STATE V394B] cva_ProfAdd_RaceFlg sourceId={gp.SourceId} " +
                $"nationIndex={nationIndex} nation='{nation.Id}' NWaterAI={spriteId} " +
                $"gp='{gp.FileID}' sprite='{sprite.name}' source='{CurrentSourceBundleId}'");
        }

        private static C2ProfileRuntime14.ProfileRecord GetFrameProfile()
        {
            C2ProfileRuntime14.EnsureLoaded();
            if (!C2ProfileRuntime14.HasProfiles) return null;

            int index = 0;
            string src = s_desk?.SourcePath ?? string.Empty;
            if (src.IndexOf("M_PROF_SEL", StringComparison.OrdinalIgnoreCase) >= 0)
                index = Mathf.Clamp(ProfileSelectionRenderer.SelectedIndex, 0, C2ProfileRuntime14.Count - 1);

            return index >= 0 && index < C2ProfileRuntime14.Count
                ? C2ProfileRuntime14.Profiles[index]
                : C2ProfileRuntime14.Current;
        }

        private static TextMeshProUGUI GetBoundText(BoundControl bound)
        {
            if (bound?.GameObject == null) return null;
            TextMeshProUGUI t = bound.GameObject.GetComponent<TextMeshProUGUI>();
            if (t == null) t = bound.GameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            return t;
        }

        private static void ApplyProfDelDesk(BoundControl bound)
        {
            if (bound?.GameObject == null) return;
            bool visible = ProfileSelectionRenderer.DeletePending;
            bound.GameObject.SetActive(visible);
            Debug.Log($"[C2:PROFILE DELETE FIX] cva_ProfDel_Desk visible={(visible ? 1 : 0)} selected={ProfileSelectionRenderer.SelectedIndex}");
        }

        private static void ApplyProfSelEnabled(BoundControl bound)
        {
            if (bound?.GameObject == null) return;
            Button b = bound.GameObject.GetComponent<Button>();
            if (b == null) b = bound.GameObject.GetComponentInChildren<Button>(true);
            if (b != null) b.interactable = !ProfileSelectionRenderer.DeletePending;
        }


        private const string ProfileListRuntimeRoot = "C2Xml_ListDesk_RuntimeItems_V395K";

        /// <summary>
        /// V395J: source-faithful cva_ProfList on the generic ListDesk runtime.
        /// The profile action does not parse XML and does not own visual constants.
        /// Its responsibilities match VUI_Actions.cpp: clear/add profile messages,
        /// keep CurrentElement, and let ListDesk/va_ListDesk own placement/state.
        /// </summary>
        private static void ApplyProfList(BoundControl bound)
        {
            UiListDesk ld = bound?.Node as UiListDesk;
            RectTransform listRoot = bound?.GameObject != null ? bound.GameObject.GetComponent<RectTransform>() : null;
            if (ld == null || listRoot == null) return;

            if (!ListDeskSourceRuntime14.TryGet(ld.SourceId, out ListDeskSourceRuntime14.TemplateSpec spec) || !spec.HasVitButtonPrototype)
            {
                Debug.LogError($"[C2:PROFILE V395K] cva_ProfList blocked: generic ListDesk.Element/VitButton prototype unavailable sourceId={ld.SourceId} source='{s_desk?.SourcePath}'");
                return;
            }

            C2ProfileRuntime14.EnsureLoaded();
            int count = C2ProfileRuntime14.Count;
            int selected = count > 0
                ? Mathf.Clamp(ProfileSelectionRenderer.SelectedIndex, 0, count - 1)
                : -1;
            if (selected >= 0) ProfileSelectionRenderer.SetSelectedIndex(selected);

            Transform old = listRoot.Find(ProfileListRuntimeRoot);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            string[] legacyRoots =
            {
                "C2Xml_ListDesk_RuntimeItems_V395G",
                "C2Xml_ListDesk_RuntimeItems_V395H",
                "C2Xml_ListDesk_RuntimeItems_V395I",
                "C2Xml_ListDesk_RuntimeItems_V395J"
            };
            for (int i = 0; i < legacyRoots.Length; i++)
            {
                Transform legacy = listRoot.Find(legacyRoots[i]);
                if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            }

            // DialogsDesk clips its DSS children to the ListDesk window.  Keep the
            // runtime rows in one masked child above Fill and below the border.
            var itemsGo = new GameObject(ProfileListRuntimeRoot, typeof(RectTransform), typeof(RectMask2D));
            itemsGo.transform.SetParent(listRoot, false);
            RectTransform itemsRt = (RectTransform)itemsGo.transform;
            itemsRt.anchorMin = itemsRt.anchorMax = new Vector2(0f, 1f);
            itemsRt.pivot = new Vector2(0f, 1f);
            itemsRt.anchoredPosition = Vector2.zero;
            itemsRt.sizeDelta = new Vector2(ld.Width, ld.Height);
            itemsRt.SetSiblingIndex(Mathf.Min(1, Mathf.Max(0, listRoot.childCount - 1)));

            bool enabled = !ProfileSelectionRenderer.DeletePending;
            float rowW = spec.OriginalButtonWidth(ld.Width); // exact ListDesk::_Draw formula
            float rowH = Mathf.Max(1f, spec.Height);

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                int state = i == selected ? 1 : 0; // va_ListDesk::SetFrameState
                C2ProfileRuntime14.ProfileRecord p = C2ProfileRuntime14.Profiles[i];
                string name = p?.m_chName ?? string.Empty;

                int passive = ResolveListStateValueV395J(spec.SpritePassive, state, -1);
                int over = ResolveListStateValueV395J(spec.SpriteOver, state, passive);
                int sprDx = ResolveListStateValueV395J(spec.SpriteDx, state, 0);

                GameObject row = OptionsRenderer.CreateListDeskElementFromSourceTemplateV395J(
                    itemsRt,
                    name,
                    state,
                    enabled,
                    () => SelectProfileListIndex(idx),
                    spec.MarginX,
                    spec.OriginalRowY(i),
                    rowW,
                    rowH,
                    spec.GPFile,
                    passive,
                    over,
                    sprDx,
                    spec.FontPassive,
                    spec.FontOver,
                    spec.FontDx,
                    spec.FontDy,
                    spec.Align,
                    spec.OneSprited,
                    spec.DisableCycling);

                if (row != null)
                {
                    TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null)
                    {
                        // Original cva_ProfList::Init:
                        // LimitString(name, &BlackFont, SD->GetWidth()-20)
                        label.text = LimitStringToWidth(label, name, Mathf.Max(1f, ld.Width - 20f));
                    }
                }
            }

            Debug.Log(
                $"[C2:PROFILE V395K] cva_ProfList original-path sourceModel=ListDesk.Element " +
                $"parent={ld.SourceId} profiles={count} current={selected} " +
                $"row={rowW:0.#}x{rowH:0.#} pos0=({spec.MarginX:0.#},{spec.OriginalRowY(0):0.#}) stepY={rowH + spec.MarginY:0.#} " +
                $"border='{spec.BorderName}' borderLR={spec.BorderLeftMargin:0.#}/{spec.BorderRightMargin:0.#} " +
                $"gp='{spec.GPFile}' state0={spec.SpritePassive[0]}/{spec.SpriteOver[0]} state1={spec.SpritePassive[1]}/{spec.SpriteOver[1]} " +
                $"font='{spec.FontPassive}/{spec.FontOver}' actions='{string.Join(",", spec.PrototypeActions)}'");
        }

        private static int ResolveListStateValueV395J(int[] values, int state, int fallback)
        {
            if (values != null && state >= 0 && state < values.Length) return values[state];
            return fallback;
        }

        private static readonly Color32 ProfileBlack = new Color32(0x2E, 0x23, 0x17, 0xFF);
        private static readonly Color32 ProfileRed = new Color32(0x8A, 0x10, 0x00, 0xFF);

        private static void SelectProfileListIndex(int index)
        {
            if (ProfileSelectionRenderer.DeletePending) return;
            if (index < 0 || index >= C2ProfileRuntime14.Count) return;

            ProfileSelectionRenderer.SetSelectedIndex(index);

            // Original SetFrameState runs each frame after va_ListItem changes
            // CurrentElement. Re-render the same XML screen so every cva_ProfCur_*
            // updates from the newly selected CurPlayer preview.
            MenuBootstrap boot = UnityEngine.Object.FindFirstObjectByType<MenuBootstrap>();
            if (boot != null)
            {
                boot.RenderByScreenId("SelProfile");
                return;
            }

            ApplyAllFrameStates();
        }

        // V395H: the current generic TMP TextButton path does not yet carry the
        // original RLCFont metrics/state through to runtime text changes.  Do not
        // fall back to the global menu font here (that is what produced the huge
        // V395C values).  Keep the exact M_PROF_SEL source metrics for these four
        // existing XML TextButtons while cva_ProfCur_* changes Message only.
        private static void ApplyProfileValueSourceMetrics(BoundControl bound, TextMeshProUGUI t)
        {
            if (t == null) return;
            t.enableAutoSizing = false;
            t.fontSize = 14f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = TextAlignmentOptions.Right;
            t.verticalAlignment = VerticalAlignmentOptions.Middle;
            t.raycastTarget = false;
            t.color = ProfileRed;
        }

        private static void ApplyProfCurName(BoundControl bound)
        {
            C2ProfileRuntime14.ProfileRecord p = GetFrameProfile();
            TextMeshProUGUI t = GetBoundText(bound);
            if (p == null || t == null) return;
            ApplyProfileValueSourceMetrics(bound, t);
            // Original: LimitString(n, TB->PassiveFont, 95); TB->SetMessage(n).
            t.text = LimitStringToWidth(t, p.m_chName ?? string.Empty, 95f);
        }

        private static void ApplyProfCurRace(BoundControl bound)
        {
            C2ProfileRuntime14.ProfileRecord p = GetFrameProfile();
            TextMeshProUGUI t = GetBoundText(bound);
            if (p == null || t == null) return;
            ApplyProfileValueSourceMetrics(bound, t);
            EnsureAiDatLoaded();
            if (p.m_iNation < 0 || p.m_iNation >= s_aiNations.Count) return;
            NationRecord n = s_aiNations[p.m_iNation];
            t.text = C2Version14Context.ResolveDataNationLabel(
                s_logicalDataRoot, s_loc, n.Id, n.Message);
        }

        private static void ApplyProfCurDiff(BoundControl bound)
        {
            C2ProfileRuntime14.ProfileRecord p = GetFrameProfile();
            TextMeshProUGUI t = GetBoundText(bound);
            if (p == null || t == null) return;
            ApplyProfileValueSourceMetrics(bound, t);
            EnsureAiDatLoaded();
            if (p.m_iDifficulty < 0 || p.m_iDifficulty >= s_aiDiffKeys.Count) return;
            string key = s_aiDiffKeys[p.m_iDifficulty];
            string value = s_loc != null ? s_loc.Resolve(key) : key;
            t.text = string.IsNullOrEmpty(value) ? key : value;
        }

        private static void ApplyProfCurPort(BoundControl bound)
        {
            C2ProfileRuntime14.ProfileRecord p = GetFrameProfile();
            if (p == null || bound?.GameObject == null) return;

            EnsureAiDatLoaded();
            if (p.m_iNation < 0 || p.m_iNation >= s_aiNations.Count) return;
            NationRecord nation = s_aiNations[p.m_iNation];

            UiGPPicture gp = bound.Node as UiGPPicture;
            if (gp != null)
            {
                gp.FileID = nation.PortraitRel;
                gp.SpriteID = Mathf.Max(0, p.m_iCurHeroId);
            }

            Image image = bound.GameObject.GetComponent<Image>();
            if (image == null) return;

            Sprite sp = TryLoadPortraitSpriteForRenderer(nation.PortraitRel, p.m_iCurHeroId);
            image.sprite = sp;
            image.enabled = sp != null;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static void ApplyProfCurOcup(BoundControl bound)
        {
            C2ProfileRuntime14.ProfileRecord p = GetFrameProfile();
            TextMeshProUGUI t = GetBoundText(bound);
            if (p == null || t == null) return;
            ApplyProfileValueSourceMetrics(bound, t);
            try
            {
                C2BigMapData14.Data bm = C2BigMapData14.Load(s_fs);
                int cn = p.campaignInitialized
                    ? p.campaignNation
                    : C2BigMapData14.CampaignNationFromProfileNation(p.m_iNation);
                int captured = 0;
                int total = bm?.Sectors?.Count ?? 0;

                if (p.campaignInitialized && p.sectors != null && p.sectors.Count > 0)
                {
                    foreach (C2ProfileRuntime14.SectorState sec in p.sectors)
                        if (sec != null && sec.owner == cn) captured++;
                }
                else if (bm?.Sectors != null)
                {
                    foreach (C2BigMapData14.SectorDefinition sec in bm.Sectors)
                        if (sec != null && sec.Owner == cn) captured++;
                }

                if (total > 0) t.text = captured + "/" + total;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:PROFILE V395C] cva_ProfCur_Ocup: " + ex.Message);
            }
        }

        private static void ApplyProfCurDesc(BoundControl bound)
        {
            C2ProfileRuntime14.ProfileRecord p = GetFrameProfile();
            TextMeshProUGUI t = GetBoundText(bound);
            if (p == null || t == null) return;

            int heroNation = GetHeroInfoNationIndex(p.m_iNation);
            string raw = ReadSourceText(
                $@"Missions\Heroes\heroinf{heroNation}{Mathf.Max(0, p.m_iCurHeroId)}.txt",
                Cp1251());
            // Original DrawMultilineText understands {C ...}, {F...} and backslash
            // hard line breaks. Preserve those semantics through TMP rich text
            // instead of stripping every formatting tag.
            t.richText = true;
            t.fontSize = 14f;
            t.color = new Color32(0x2E, 0x23, 0x17, 0xFF);
            t.text = ConvertEngineTextToTmp(raw);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.verticalAlignment = VerticalAlignmentOptions.Top;
            // V395E reconstructs the parent XML DialogsDesk as a clipped scroll viewport.
            t.overflowMode = TextOverflowModes.Overflow;
        }

        private static string LimitStringToWidth(TextMeshProUGUI t, string text, float maxWidth)
        {
            string s = text ?? string.Empty;
            if (t == null || maxWidth <= 0f) return s;
            while (s.Length > 0 && t.GetPreferredValues(s).x > maxWidth)
                s = s.Substring(0, s.Length - 1);
            return s;
        }

        private static Encoding Cp1251()
        {
            try { return Encoding.GetEncoding(1251); }
            catch { return Encoding.UTF8; }
        }

        private static string ParseEngineText(string raw)
        {
            return StripTmpTags(ConvertEngineTextToTmp(raw));
        }

        public static string ConvertEngineTextToTmp(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            string s = raw.Replace("\\\r\\\n", "\n").Replace("\\\n", "\n");
            s = s.Replace("{CR}", "\n").Replace("\\\\", "\n").Replace("\\{", "{");

            var outText = new StringBuilder(s.Length + 64);
            var colorStack = new Stack<string>();
            int openSizeTags = 0;

            for (int i = 0; i < s.Length; )
            {
                if (s[i] == '{')
                {
                    int close = s.IndexOf('}', i + 1);
                    if (close >= 0)
                    {
                        string cmd = s.Substring(i + 1, close - i - 1).Trim();
                        if (TryAppendEngineTag(cmd, outText, colorStack, ref openSizeTags))
                        {
                            i = close + 1;
                            continue;
                        }
                    }
                }

                char c = s[i++];
                if (c == '<') outText.Append("&lt;");
                else if (c == '>') outText.Append("&gt;");
                else if (c == '&') outText.Append("&amp;");
                else if (c == '\\') outText.Append('\n');
                else outText.Append(c);
            }

            while (openSizeTags-- > 0) outText.Append("</size>");
            while (colorStack.Count > 0)
            {
                colorStack.Pop();
                outText.Append("</color>");
            }

            string result = outText.ToString();
            result = Regex.Replace(result, @"[ \t]+\n", "\n");
            result = Regex.Replace(result, @"\n{3,}", "\n\n");
            return result.Trim();
        }

        private static bool TryAppendEngineTag(string cmd, StringBuilder dst, Stack<string> colorStack, ref int openSizeTags)
        {
            if (cmd == null) return false;
            if (cmd.Length == 0) return true;

            if (cmd == "A" || cmd == "AL" || cmd == "AC" || cmd == "AR")
                return true;

            if (cmd == "C")
            {
                if (colorStack.Count > 0)
                {
                    colorStack.Pop();
                    dst.Append("</color>");
                }
                return true;
            }

            if (cmd.StartsWith("C ", StringComparison.OrdinalIgnoreCase))
            {
                string rgba = EngineArgbToTmpRgba(cmd.Substring(2).Trim());
                if (!string.IsNullOrEmpty(rgba))
                {
                    colorStack.Push(rgba);
                    dst.Append("<color=#").Append(rgba).Append('>');
                }
                return true;
            }

            if (cmd.Length == 2 && char.ToUpperInvariant(cmd[0]) == 'C')
            {
                string rgba = EngineNamedColorToTmp(cmd[1]);
                if (!string.IsNullOrEmpty(rgba))
                {
                    colorStack.Push(rgba);
                    dst.Append("<color=#").Append(rgba).Append('>');
                }
                return true;
            }

            if (cmd == "F")
            {
                if (openSizeTags > 0)
                {
                    dst.Append("</size>");
                    openSizeTags--;
                }
                // Original {F} restores DefaultFont AND DefaultColor.
                while (colorStack.Count > 0)
                {
                    colorStack.Pop();
                    dst.Append("</color>");
                }
                return true;
            }

            if (cmd.StartsWith("F", StringComparison.OrdinalIgnoreCase))
            {
                string font = cmd.Substring(1).Trim();
                float size = EngineFontSize(font);
                if (openSizeTags > 0)
                {
                    dst.Append("</size>");
                    openSizeTags--;
                }
                // Original {Fname} takes that font's DefColor. Profile hero text
                // then applies explicit {C ...} where needed; close old color scope.
                while (colorStack.Count > 0)
                {
                    colorStack.Pop();
                    dst.Append("</color>");
                }
                dst.Append("<size=").Append(size.ToString("0", System.Globalization.CultureInfo.InvariantCulture)).Append('>');
                openSizeTags++;
                return true;
            }

            if (cmd.StartsWith("P ", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("G ", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("I", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("R ", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string EngineArgbToTmpRgba(string argb)
        {
            if (string.IsNullOrWhiteSpace(argb)) return string.Empty;
            string h = argb.Trim().TrimStart('#');
            if (h.Length == 6) return h.ToUpperInvariant() + "FF";
            if (h.Length != 8) return string.Empty;
            return (h.Substring(2, 6) + h.Substring(0, 2)).ToUpperInvariant();
        }

        private static string EngineNamedColorToTmp(char code)
        {
            switch (char.ToUpperInvariant(code))
            {
                case 'B': return "000000FF";
                case 'R': return "B83B3FFF";
                case 'D': return "FF0000FF";
                case 'N': return "502515FF";
                case 'W': return "FFFFFFFF";
                case 'Y': return "FFFF00FF";
                case 'G': return "60A05AFF";
                default: return string.Empty;
            }
        }

        private static float EngineFontSize(string font)
        {
            if (string.IsNullOrEmpty(font)) return 14f;
            string f = font.ToUpperInvariant();
            if (f.Contains("C10") || f == "T") return 10f;
            if (f.Contains("C12")) return 12f;
            if (f.Contains("C14")) return 14f;
            if (f.Contains("C16")) return 16f;
            if (f.Contains("C18")) return 18f;
            return 14f;
        }

        private static string StripTmpTags(string rich)
        {
            if (string.IsNullOrEmpty(rich)) return string.Empty;
            return Regex.Replace(rich, @"<[^>]+>", string.Empty);
        }

        public static Sprite TryLoadPortraitSpriteForRenderer(string portraitFileId, int spriteId)
        {
            return TryLoadGpSpriteForRendererInternal(portraitFileId, spriteId, true, "portrait-ui");
        }

        /// <summary>
        /// Runtime fallback for XML GP banks that were not pre-exported to
        /// Resources. This is still source-driven: FileID/SpriteID come directly
        /// from the XML control (or original SetFrameState action).
        /// </summary>
        public static Sprite TryLoadGpSpriteForRenderer(string gpFileId, int spriteId, bool flipY)
        {
            return TryLoadGpSpriteForRendererInternal(gpFileId, spriteId, flipY, flipY ? "ui-gp" : "generic-ui");
        }

        private static Sprite TryLoadGpSpriteForRendererInternal(
            string gpFileId, int spriteId, bool flipY, string sourceTag)
        {
            if (spriteId < 0 || string.IsNullOrWhiteSpace(gpFileId)) return null;

            string file = gpFileId.Trim().Replace('/', '\\');
            string normalized = file.Replace('\\', '_').Replace('/', '_');
            string frame = $"frame_{spriteId:0000}";
            string cacheKey = sourceTag + "|" + normalized + "|" + spriteId;

            if (s_gpRuntimeSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            // Prefer an already-imported bank because those frames already have
            // the orientation/import settings used by the rest of the menu.
            string[] folders =
            {
                normalized + "_frames",
                normalized.ToUpperInvariant() + "_frames",
                normalized.ToLowerInvariant() + "_frames"
            };
            for (int i = 0; i < folders.Length; i++)
            {
                Sprite imported = Resources.Load<Sprite>(folders[i] + "/" + frame);
                if (imported != null)
                {
                    s_gpRuntimeSpriteCache[cacheKey] = imported;
                    return imported;
                }
            }

            var candidates = new List<string>();
            string root = !string.IsNullOrWhiteSpace(s_logicalDataRoot)
                ? s_logicalDataRoot
                : (s_fs?.DataRoot ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(root))
            {
                candidates.Add(Path.Combine(root, "Cash", normalized + ".g16"));
                candidates.Add(Path.Combine(root, file + ".g16"));
            }

            // V396A3: XML may intentionally come from the clean 1.4 bundle while
            // the authoritative final graphics live in the user's installed DataRoot.
            // Keep XML provenance, but also allow the SAME XML FileID/SpriteID to
            // resolve against that real game DataRoot. No alternate art is substituted.
            string physicalDataRoot = s_fs?.DataRoot ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(physicalDataRoot) &&
                !string.Equals(physicalDataRoot, root, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(Path.Combine(physicalDataRoot, "Cash", normalized + ".g16"));
                candidates.Add(Path.Combine(physicalDataRoot, file + ".g16"));
            }
            candidates.Add(Path.Combine(
                Application.streamingAssetsPath, "Cossacks2", "Data", "Cash", normalized + ".g16"));
            candidates.Add(Path.Combine(
                Application.streamingAssetsPath, "Cossacks2", "Data", file + ".g16"));

            for (int i = 0; i < candidates.Count; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                Sprite sp = DecodeG16Sprite(path, spriteId, cacheKey, frame, flipY ? sourceTag + "-flip" : sourceTag);
                if (sp != null) return sp;
            }

            return null;
        }

        private static Sprite LoadGpSprite(string gpFileId, int spriteId)
        {
            string file = gpFileId ?? string.Empty;
            string normalized = file.Replace('\\', '_').Replace('/', '_');
            string frame = $"frame_{spriteId:0000}";
            string cacheKey = normalized + "|" + spriteId;

            if (s_gpRuntimeSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            // V394A: final 1.4 uses a 9-entry GlobalAI roster whose NWaterAI values
            // form the complete 0..8 permutation.  The old Resources cache
            // INTERF3_FLAG_frames contains only the six base-game frames and is
            // therefore the WRONG semantic source for this roster even when a
            // requested frame number happens to exist there.
            //
            // The user's C2 1.4 resource set contains:
            //     Interf3\Addon\Flag.G16
            // with exactly 9 sprites at 32x24.  Resolve INTERF3\FLAG to that bank
            // before looking at the obsolete six-frame Resources cache.
            if (string.Equals(normalized, "INTERF3_FLAG", StringComparison.OrdinalIgnoreCase) &&
                IsFinal14NineNationRosterActive())
            {
                Sprite final14 = LoadFinal14NationFlagSprite(spriteId, cacheKey, frame);
                if (final14 != null)
                    return final14;

                // Do not silently fall back to the six-frame bank: that produced
                // historically wrong flags for Egypt/Rein and missing flags for
                // France/Russia/Prussia.
                Debug.LogError(
                    $"[C2:FLAG V394A] final-1.4 flag source unavailable sprite={spriteId}; " +
                    "refusing six-frame INTERF3_FLAG fallback");
                return null;
            }

            string[] folders =
            {
                normalized + "_frames",
                normalized.ToUpperInvariant() + "_frames",
                normalized.ToLowerInvariant() + "_frames"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                Sprite sp = Resources.Load<Sprite>(folders[i] + "/" + frame);
                if (sp != null)
                {
                    s_gpRuntimeSpriteCache[cacheKey] = sp;
                    return sp;
                }
            }

            // Generic fallback for non-final14 GP banks.
            string root = !string.IsNullOrWhiteSpace(s_logicalDataRoot)
                ? s_logicalDataRoot
                : (s_fs?.DataRoot ?? string.Empty);
            if (string.IsNullOrWhiteSpace(root)) return null;

            string cashDir = Path.Combine(root, "Cash");
            if (!Directory.Exists(cashDir)) return null;

            string g16Path = Path.Combine(cashDir, normalized + ".g16");
            if (!File.Exists(g16Path))
            {
                try
                {
                    foreach (string candidate in Directory.EnumerateFiles(cashDir, "*.g16", SearchOption.TopDirectoryOnly))
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(candidate), normalized, StringComparison.OrdinalIgnoreCase))
                        {
                            g16Path = candidate;
                            break;
                        }
                    }
                }
                catch { }
            }
            if (!File.Exists(g16Path)) return null;

            return DecodeG16Sprite(g16Path, spriteId, cacheKey, frame, "generic");
        }

        private static bool IsFinal14NineNationRosterActive()
        {
            if (s_aiNations.Count != 9 || s_aiNComp != 9)
                return false;

            bool[] seen = new bool[9];
            for (int i = 0; i < s_aiNations.Count; i++)
            {
                int f = s_aiNations[i].NWaterAI;
                if (f < 0 || f >= 9 || seen[f])
                    return false;
                seen[f] = true;
            }

            for (int i = 0; i < seen.Length; i++)
                if (!seen[i]) return false;

            return true;
        }

        private static Sprite LoadFinal14NationFlagSprite(int spriteId, string cacheKey, string frame)
        {
            var candidates = new List<string>();

            // 1) Active installed 1.4 data, if the add-on tree is loose.
            if (!string.IsNullOrWhiteSpace(s_logicalDataRoot))
            {
                candidates.Add(Path.Combine(s_logicalDataRoot, "Interf3", "Addon", "Flag.G16"));
                candidates.Add(Path.Combine(s_logicalDataRoot, "Cash", "Interf3_Addon_Flag.g16"));
                candidates.Add(Path.Combine(s_logicalDataRoot, "Cash", "INTERF3_ADDON_FLAG.g16"));
            }

            // 2) Bundled exact 9-frame 1.4 bank shipped by V394A.
            candidates.Add(Path.Combine(
                Application.streamingAssetsPath,
                "Cossacks2", "Data", "Interf3", "Addon", "Flag.G16"));

            for (int i = 0; i < candidates.Count; i++)
            {
                string p = candidates[i];
                if (string.IsNullOrWhiteSpace(p) || !File.Exists(p))
                    continue;

                Sprite sp = DecodeG16Sprite(p, spriteId, cacheKey, frame, "final14-addon");
                if (sp != null)
                {
                    Debug.Log(
                        $"[C2:FLAG V394B] sprite={spriteId} source='Interf3\\Addon\\Flag.G16' " +
                        $"path='{p}' result='{sp.name}' orientation=flipY");
                    return sp;
                }
            }

            return null;
        }

        private static Sprite DecodeG16Sprite(
            string g16Path, int spriteId, string cacheKey, string frame, string sourceTag)
        {
            if (!MelinojaCodecBridge.LoadG16ToMemory(g16Path, out var err, doubleOverlay: false))
            {
                Debug.LogWarning(
                    $"[C2:FLAG V394A] G16 load failed source={sourceTag} '{g16Path}': {err}");
                return null;
            }

            if (!MelinojaCodecBridge.TryGetG16FrameRGBA(
                    g16Path, spriteId, out int w, out int h, out byte[] rgba, out var err2))
            {
                Debug.LogWarning(
                    $"[C2:FLAG V394A] G16 frame {spriteId} failed source={sourceTag} '{g16Path}': {err2}");
                return null;
            }

            // Melinoja returns G16 rows in the file/renderer's top-to-bottom order.
            // Unity raw Texture2D data is addressed bottom-to-top for sprite display.
            // The final 1.4 Addon flag bank therefore needs a vertical row flip.
            // Keep this scoped to the 9-frame flag bank only: portraits and other
            // G16 users already have their own orientation contracts.
            if (string.Equals(sourceTag, "final14-addon", StringComparison.Ordinal) ||
                sourceTag.EndsWith("-flip", StringComparison.Ordinal))
                rgba = FlipRgbaRows(rgba, w, h);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // V396A6: original GP/G16 UI is pixel art rendered on integer pixels.
            // Bilinear filtering creates visible hairline seams between independently
            // tiled border/header sprites and softens bitmap decorations.
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, false);

            Sprite runtimeSprite = Sprite.Create(
                tex, new Rect(0, 0, w, h), new Vector2(0, 1), 1f);
            runtimeSprite.name = frame;
            s_gpRuntimeSpriteCache[cacheKey] = runtimeSprite;
            return runtimeSprite;
        }

        private static byte[] FlipRgbaRows(byte[] src, int width, int height)
        {
            if (src == null || width <= 0 || height <= 1)
                return src;

            int rowBytes = width * 4;
            if (src.Length < rowBytes * height)
                return src;

            byte[] dst = new byte[src.Length];
            for (int y = 0; y < height; y++)
                Buffer.BlockCopy(src, y * rowBytes, dst, (height - 1 - y) * rowBytes, rowBytes);

            return dst;
        }

        private static void EnsureAiDatLoaded()
        {
            if (s_sourceContext == null)
                s_sourceContext = Menu14SourceContext.FromDesk(s_desk, s_fs);

            string key = (s_sourceContext?.BundleId ?? string.Empty) + "|" +
                         (s_sourceContext?.DataRoot ?? string.Empty) + "|runtime=" +
                         (s_fs?.DataRoot ?? string.Empty);
            if (s_aiLoadAttempted && string.Equals(key, s_aiLoadedKey, StringComparison.OrdinalIgnoreCase))
                return;

            s_aiLoadAttempted = true;
            s_aiLoadedKey = key;
            s_aiNations.Clear();
            s_aiDiffKeys.Clear();
            s_heroInfoIndex.Clear();
            s_aiNComp = 0;

            if (s_sourceContext == null)
                return;

            C2Version14Context.GlobalAiSnapshot14 snap = C2Version14Context.LoadEffectiveGlobalAi(s_fs, s_sourceContext);
            s_logicalDataRoot = snap.DataRoot ?? string.Empty;
            string aiPath = Path.Combine(s_logicalDataRoot, "AI", "ai.dat");

            s_aiNComp = snap.DeclaredNComp;
            s_aiDiffKeys.AddRange(snap.DifficultyKeys);

            for (int i = 0; i < snap.Nations.Count; i++)
            {
                C2Version14Context.GlobalAiNation14 n = snap.Nations[i];
                s_aiNations.Add(new NationRecord
                {
                    Index = n.Index,
                    Id = n.Id,
                    Message = n.Message,
                    UnitToken = n.UnitToken,
                    HeroCode = n.HeroCode,
                    NPeas = n.NPeas,
                    NLandAI = n.NLandAI,
                    NWaterAI = n.NWaterAI,
                    PortraitRel = n.PortraitRel,
                    HeroPrefix = n.HeroPrefix
                });
            }

            EnsureHeroInfoMapLoaded();

            Debug.Log(
                $"[C2:SOURCE V394B] GlobalAI xmlSource='{s_sourceContext.BundleId}' dataRoot='{s_logicalDataRoot}' path='{aiPath}' " +
                $"nations={s_aiNations.Count}/{snap.DeclaredNAi} nComp={s_aiNComp} " +
                $"domain=GLOBAL_AI sourceTag={C2Version14Context.SourceData14} map='{BuildNationMapLog()}'");

            RunSourceSelfTest();
        }

        private static void EnsureHeroInfoMapLoaded()
        {
            if (s_heroInfoIndex.Count > 0)
                return;

            string text = C2Version14Context.ReadTextFromRoot(
                s_logicalDataRoot, @"Missions\Heroes\heroinf.dat", Encoding.GetEncoding(1251));
            if (string.IsNullOrEmpty(text) && s_sourceContext != null)
                text = s_sourceContext.ReadAllText(@"Missions\Heroes\heroinf.dat", Encoding.GetEncoding(1251));
            if (string.IsNullOrEmpty(text)) return;

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                Match m = Regex.Match(lines[i], @"\((?<c>[A-Za-z]{2})\)\s*(?<n>\d+)");
                if (!m.Success) continue;
                if (int.TryParse(m.Groups["n"].Value, out int n))
                    s_heroInfoIndex[m.Groups["c"].Value] = n;
            }
        }

        private static void RunSourceSelfTest()
        {
            if (s_sourceContext == null) return;
            string key = s_aiLoadedKey;
            if (string.Equals(key, s_selfTestedKey, StringComparison.OrdinalIgnoreCase))
                return;
            s_selfTestedKey = key;

            int engineRegistryCount = C2Version14Context.EngineNationNameRegistry.Count;
            bool engineRegistryOk = engineRegistryCount == 9;

            var snap = new C2Version14Context.GlobalAiSnapshot14
            {
                DataRoot = s_logicalDataRoot,
                DeclaredNAi = s_aiNations.Count,
                DeclaredNComp = s_aiNComp
            };
            for (int i = 0; i < s_aiNations.Count; i++)
            {
                NationRecord n = s_aiNations[i];
                snap.Nations.Add(new C2Version14Context.GlobalAiNation14
                {
                    Index = n.Index,
                    Id = n.Id,
                    Message = n.Message,
                    UnitToken = n.UnitToken,
                    HeroCode = n.HeroCode,
                    NPeas = n.NPeas,
                    NLandAI = n.NLandAI,
                    NWaterAI = n.NWaterAI,
                    PortraitRel = n.PortraitRel,
                    HeroPrefix = n.HeroPrefix
                });
            }

            bool fullNine = C2Version14Context.IsFinal14NineNationRoster(snap);
            int flagsFound = 0;
            for (int i = 0; i < s_aiNations.Count; i++)
                if (LoadGpSprite(@"INTERF3\FLAG", s_aiNations[i].NWaterAI) != null)
                    flagsFound++;

            bool pass = engineRegistryOk && fullNine && flagsFound == 9;
            string status = pass ? "PASS" : (fullNine ? "FLAGS_INCOMPLETE" : "NOT_FINAL_9_ROSTER");
            string line =
                $"[C2:SOURCE V394B] selftest xmlSource={s_sourceContext.BundleId} xmlRoot='{s_sourceContext.DataRoot}' " +
                $"logicalDataRoot='{s_logicalDataRoot}' engineNationNameRegistry={engineRegistryCount} " +
                $"globalAiRoster={s_aiNations.Count} nComp={s_aiNComp} profileRoster={s_aiNations.Count} " +
                $"battleRoomRoster={s_aiNations.Count}+random flags={flagsFound}/{s_aiNations.Count} " +
                $"FRANCE={GetFlagForLog("FRANCE")} RUSSIA={GetFlagForLog("RUSSIA")} " +
                $"ENGLAND={GetFlagForLog("ENGLAND")} PRUSSIA={GetFlagForLog("PRUSSIA")} " +
                $"AUSTRIA={GetFlagForLog("AUSTRIA")} EGIPET={GetFlagForLog("EGIPET")} " +
                $"POLAND={GetFlagForLog("POLAND")} SPAIN={GetFlagForLog("SPAIN")} REIN={GetFlagForLog("REIN")} " +
                $"legacyC1NationRegistryUsed=0 status={status} map='{BuildNationMapLog()}'";

            if (pass) Debug.Log(line);
            else Debug.LogWarning(line);
        }

        private static int GetFlagForLog(string id)
        {
            for (int i = 0; i < s_aiNations.Count; i++)
                if (s_aiNations[i].Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return s_aiNations[i].NWaterAI;
            return -1;
        }

        private static string[] SplitWs(string s)
        {
            return Regex.Split((s ?? string.Empty).Trim(), @"\s+");
        }

        private static string BuildNationMapLog()
        {
            if (s_aiNations.Count == 0) return string.Empty;
            var parts = new string[s_aiNations.Count];
            for (int i = 0; i < s_aiNations.Count; i++)
                parts[i] = s_aiNations[i].ToString();
            return string.Join(",", parts);
        }
    }
}
