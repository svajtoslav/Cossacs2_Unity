using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;
using Cossacks2Bridge.UnityAdapters.Maps;
using Cossacks2Bridge.UnityAdapters.Profiles;
using Cossacks2Bridge.UnityAdapters.Renderers;

/// <summary>
/// Minimal "executor" for UI actions coming from renderers.
/// </summary>
public sealed class MenuActionSink : MonoBehaviour, IUiActionSink
{
    private const int LobbyPlayerCount = 8;
    private static readonly int[] SingleBattlesPlayerColorBySlot = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly int[] SingleBattlesPlayerTeamBySlot = { 1, 0, 0, 0, 0, 0, 0, 0 };

    private Cossacks2Bridge.UnityAdapters.MenuBootstrap _bootstrap;
    private bool _pendingBfeEntryV396A2;
    public static string CurrentProfileName { get; private set; } = "";

    // V395A: keep the V387B1 multiplayer nickname contract.
    // OptionsRenderer still binds cva_MU_NickInput through these public members.
    public static string NetworkPlayerNick { get; private set; } = "";

    public static void SetCurrentProfileFromRuntime(string name)
    {
        CurrentProfileName = (name ?? string.Empty).Trim();

        // Original V387B1 behavior: a selected profile supplies the default
        // network nickname until the user edits it in the multiplayer desk.
        if (string.IsNullOrWhiteSpace(NetworkPlayerNick))
            NetworkPlayerNick = SanitizeNetworkNickV387B1(CurrentProfileName);
    }
    public static bool SingleBattlesShowBattles { get; set; } = false;
    public static bool SingleBattlesShowLoad { get; set; } = false;
    public static string SingleBattlesSelectedId { get; set; } = "";
    public static bool SingleBattlesArcadeModeEnabled { get; set; } = false;

    public static int GetSingleBattlesPlayerColor(int slot)
    {
        slot = Mathf.Clamp(slot, 0, LobbyPlayerCount - 1);
        return Mathf.Clamp(SingleBattlesPlayerColorBySlot[slot], 0, 7);
    }

    public static void SetSingleBattlesPlayerColor(int slot, int color)
    {
        slot = Mathf.Clamp(slot, 0, LobbyPlayerCount - 1);
        // Cossacks II exposes colors 0..6 to players. Color/nation 7 is the
        // brown neutral side and is not a normal room color.
        SingleBattlesPlayerColorBySlot[slot] = Mathf.Clamp(color, 0, 6);
    }

    public static int GetSingleBattlesPlayerTeam(int slot)
    {
        slot = Mathf.Clamp(slot, 0, LobbyPlayerCount - 1);
        return Mathf.Clamp(SingleBattlesPlayerTeamBySlot[slot], 0, 4);
    }

    public static void SetSingleBattlesPlayerTeam(int slot, int team)
    {
        slot = Mathf.Clamp(slot, 0, LobbyPlayerCount - 1);
        SingleBattlesPlayerTeamBySlot[slot] = Mathf.Clamp(team, 0, 4);
    }

    private static void ApplySingleBattlesLobbyToRuntimeLikeOriginal()
    {
        for (int slot = 0; slot < LobbyPlayerCount; slot++)
            C2PlayerColorsLikeOriginal.SetPlayerColorId(slot, GetSingleBattlesPlayerColor(slot));

        // Internally map objects still belong to player slot 0. Its selected
        // ColorID only changes their flag/unit tint, just like PL_INFO.ColorID
        // and cgi_NatRefTBL are separate in the original engine.
        C2EditorRuntimeStateV333LikeOriginal.ControlledNation = 0;
        Debug.Log($"[C2:LOBBY] localSlot=0 color={GetSingleBattlesPlayerColor(0)} team={GetSingleBattlesPlayerTeam(0)} controlledNation=0");
    }

    private static string ExtractTargetFromPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return "";

        var id = TryGetTag(payload, "ID");
        if (!string.IsNullOrWhiteSpace(id)) return id;

        var name = TryGetTag(payload, "Name");
        if (!string.IsNullOrWhiteSpace(name)) return name;

        return payload.Trim();
    }

    private void Awake()
    {
        _bootstrap = FindFirstObjectByType<Cossacks2Bridge.UnityAdapters.MenuBootstrap>();
        if (_bootstrap == null)
            Debug.LogWarning("[C2:SINK] MenuBootstrap not found (desk switching won't work)");
    }

    public void OnAction(string buttonKey, UiAction action)
    {
        if (action == null)
        {
            Debug.LogWarning($"[C2:SINK] button='{buttonKey}' action=NULL");
            return;
        }

        Debug.Log($"[C2:SINK] button='{buttonKey}' action='{action.Name}'");

        switch (action.Name)
        {
            case "cva_ProfAdd_Accept":
                {
                    CaptureProfileNameFromScene();
                    var p = C2ProfileRuntime14.AddProfile(
                        CurrentProfileName,
                        Menu14ActionStateRuntime.CurrentProfileNationIndex,
                        Menu14ActionStateRuntime.CurrentProfileDifficultyIndex,
                        Cossacks2Bridge.UnityAdapters.AddProfile.AddProfileCommanderController.CurrentHeroIndex);
                    SetCurrentProfileFromRuntime(p?.m_chName ?? CurrentProfileName);
                    _bootstrap?.SetHasProfile(true);
                    // When AddProfile was opened from SelProfile, return to the
                    // profile desk; initial single-player creation continues to Single.
                    if (_bootstrap != null && string.Equals(_bootstrap.PreviousScreenId, "SelProfile", StringComparison.OrdinalIgnoreCase))
                        _bootstrap.RenderByScreenId("SelProfile");
                    else
                        _bootstrap?.RenderByScreenId("Single");
                    break;
                }

            case "cva_ProfAdd_Cancel":
                {
                    if (C2ProfileRuntime14.HasProfiles) _bootstrap?.RenderByScreenId("SelProfile");
                    else _bootstrap?.RenderByScreenId("Main");
                    break;
                }

            case "cva_ProfSel_Add":
                {
                    if (ProfileSelectionRenderer.DeletePending) break;
                    ProfileSelectionRenderer.SetDeletePending(false);
                    _bootstrap?.RenderByScreenId("AddProfile");
                    break;
                }

            case "cva_ProfSel_Accept":
                {
                    if (ProfileSelectionRenderer.DeletePending) break;
                    ProfileSelectionRenderer.SetDeletePending(false);
                    int idx = ProfileSelectionRenderer.SelectedIndex;
                    if (C2ProfileRuntime14.SelectIndex(idx))
                    {
                        SetCurrentProfileFromRuntime(C2ProfileRuntime14.Current?.m_chName ?? string.Empty);
                        _bootstrap?.SetHasProfile(true);
                        _bootstrap?.RenderByScreenId("Single");
                    }
                    break;
                }

            case "cva_ProfSel_Cancel":
                {
                    if (ProfileSelectionRenderer.DeletePending) break;
                    ProfileSelectionRenderer.SetDeletePending(false);

                    // Original cva_ProfSel_Cancel reloads Profiles from disk before
                    // leaving the screen, restoring any deletion that was confirmed
                    // inside the modal but not committed by the outer Accept button.
                    if (C2ProfileRuntime14.HasProfiles)
                        C2ProfileRuntime14.ReloadFromDisk();

                    SetCurrentProfileFromRuntime(C2ProfileRuntime14.Current?.m_chName ?? string.Empty);
                    _bootstrap?.RenderByScreenId(C2ProfileRuntime14.HasProfiles ? "Single" : "Main");
                    break;
                }

            case "cva_ProfDelete":
                {
                    // VUI_Actions.cpp: only raise vCurProfDel when CurPlayer exists.
                    int idx = ProfileSelectionRenderer.SelectedIndex;
                    if (idx >= 0 && idx < C2ProfileRuntime14.Count)
                    {
                        ProfileSelectionRenderer.SetDeletePending(true);
                        _bootstrap?.RenderByScreenId("SelProfile");
                    }
                    break;
                }

            case "cva_ProfDel_Accept":
                {
                    int idx = ProfileSelectionRenderer.SelectedIndex;
                    bool deleted = C2ProfileRuntime14.DeleteIndex(idx);
                    ProfileSelectionRenderer.SetSelectedIndex(0);
                    ProfileSelectionRenderer.SetDeletePending(false);
                    if (!deleted) break;

                    if (C2ProfileRuntime14.HasProfiles)
                    {
                        // Original DeleteProfile does NOT SaveXML here. The user can
                        // still press the OUTER Cancel and restore the saved list.
                        SetCurrentProfileFromRuntime(C2ProfileRuntime14.Current?.m_chName ?? string.Empty);
                        _bootstrap?.RenderByScreenId("SelProfile");
                    }
                    else
                    {
                        // Original special case: deleting the final profile opens
                        // AddProfile and immediately persists the empty list.
                        C2ProfileRuntime14.Save();
                        SetCurrentProfileFromRuntime(string.Empty);
                        _bootstrap?.SetHasProfile(false);
                        _bootstrap?.RenderByScreenId("AddProfile");
                    }
                    break;
                }

            case "cva_ProfDel_Cancel":
                {
                    ProfileSelectionRenderer.SetDeletePending(false);
                    _bootstrap?.RenderByScreenId("SelProfile");
                    break;
                }

            case "cva_ProfStats":
                {
                    _bootstrap?.RenderByScreenId("EW2CampStat");
                    break;
                }

            case "cva_ProfLoad":
                {
                    C2ProfileRuntime14.EnsureLoaded();
                    SetCurrentProfileFromRuntime(C2ProfileRuntime14.Current?.m_chName ?? string.Empty);
                    break;
                }

            case "cva_MM_Start":
                {
                    var id = ExtractTargetFromPayload(action.Payload);
                    Debug.Log($"[C2:SINK] MM_Start -> go '{id}' (payload='{action.Payload}')");
                    if (IsConquestTargetV395B(id))
                    {
                        OpenConquestOfEuropeV395B("cva_MM_Start:" + id);
                    }
                    else if (_bootstrap != null && !string.IsNullOrWhiteSpace(id))
                    {
                        _bootstrap.RenderByScreenId(id);
                    }
                    else
                    {
                        Debug.LogWarning("[C2:SINK] MM_Start ignored (missing target id or bootstrap)");
                    }
                    break;
                }

            case "cva_MM_Cancel":
                {
                    Debug.Log("[C2:SINK] MM_Cancel -> back");
                    _bootstrap?.RenderPreviousOrMain();
                    break;
                }

            case "cva_MM_Accept":
                {
                    Debug.Log("[C2:SINK] MM_Accept -> back (stub)");
                    _bootstrap?.RenderPreviousOrMain();
                    break;
                }

            case "Options":
                {
                    Debug.Log("[C2:SINK] Options -> go 'Options'");
                    _bootstrap?.RenderByScreenId("Options");
                    break;
                }

            case "Cancel":
                {
                    Debug.Log("[C2:SINK] Cancel -> back");
                    _bootstrap?.RenderPreviousOrMain();
                    break;
                }

            case "Accept":
                {
                    Debug.Log("[C2:SINK] Accept -> back (stub)");
                    _bootstrap?.RenderPreviousOrMain();
                    break;
                }

            case "cva_InGameMenu_MainDesk_Set":
                {
                    var id = TryGetTag(action.Payload, "ID");
                    if (string.Equals(id, "SinBattles", StringComparison.OrdinalIgnoreCase))
                        id = "SingleBattles";

                    Debug.Log($"[C2:SINK] Desk_Set -> id='{id}'");
                    if (IsConquestTargetV395B(id))
                    {
                        // Original M_Single executes four actions on Battle for Europe.
                        // The final action cva_SPD_CampMessageCheck decides whether the
                        // first-run information window stays open or Campaign Start is
                        // invoked immediately. V395B opened BigMap on action #1, which
                        // bypassed that original gate completely. Defer only this exact
                        // Single-screen chain until CampMessageCheck arrives.
                        if (_bootstrap != null && string.Equals(_bootstrap.CurrentScreenId, "Single", StringComparison.OrdinalIgnoreCase))
                        {
                            _pendingBfeEntryV396A2 = true;
                            Debug.Log($"[C2:BFE14 ENTRY V396A2] Desk_Set target='{id}' deferredUntil=CampMessageCheck");
                        }
                        else
                        {
                            OpenConquestOfEuropeV395B("cva_InGameMenu_MainDesk_Set:" + id);
                        }
                    }
                    else if (_bootstrap != null && !string.IsNullOrWhiteSpace(id))
                    {
                        _bootstrap.RenderByScreenId(id);
                    }
                    break;
                }

            // Original 1.0/1.1 cva_MM_Campaign calls ProcessBigMap(0).
            // M_Single 1.4 still contains this action on the Campaign Start button.
            // V395A did not handle it, so depending on which canonical Campaign
            // button MainMenuRenderer selected, clicking "Conquest of Europe"
            // could do nothing. Route the original action to the V395 BigMap port.
            case "cva_MM_Campaign":
                {
                    _bootstrap?.CloseCampaignModalOriginalV396A3();
                    Debug.Log("[C2:BFE14 ENTRY V396A3] source Campaign Start -> original XML modal close -> BigMap");
                    OpenConquestOfEuropeV395B("cva_MM_Campaign");
                    break;
                }

            case "cva_MM_SinStart":
                {
                    Debug.Log("[C2:SINK] SinStart -> go 'Single'");
                    _bootstrap?.RenderByScreenId("Single");
                    break;
                }

            case "cva_MM_MultiEnter":
                {
                    _bootstrap?.RenderByScreenId("Multi");
                    break;
                }


            case "cva_Battles_Mode_Skirmish":
                {
                    SingleBattlesShowBattles = false;
                    SingleBattlesShowLoad = false;
                    SingleBattlesSelectedId = "";
                    _bootstrap?.RenderByScreenId("SingleBattles");
                    break;
                }

            case "cva_Battles_Mode_Battles":
                {
                    SingleBattlesShowBattles = true;
                    SingleBattlesShowLoad = false;
                    SingleBattlesSelectedId = "";
                    _bootstrap?.RenderByScreenId("SingleBattles");
                    break;
                }


            case "cva_Battles_Mode_Load":
                {
                    SingleBattlesShowBattles = false;
                    SingleBattlesShowLoad = true;
                    SingleBattlesSelectedId = "";
                    _bootstrap?.RenderByScreenId("SingleBattles");
                    break;
                }

            case "cva_Battles_Select":
                {
                    SingleBattlesSelectedId = action.Payload ?? "";
                    _bootstrap?.RenderByScreenId("SingleBattles");
                    break;
                }



            case "cva_Battles_ArcadeToggle":
                {
                    SingleBattlesArcadeModeEnabled = !SingleBattlesArcadeModeEnabled;
                    _bootstrap?.RenderByScreenId("SingleBattles");
                    break;
                }


            case "cva_Battles_Start":
                {
                    Debug.Log("[C2:SINK] Battles_Start -> open terrain mode");
                    ApplySingleBattlesLobbyToRuntimeLikeOriginal();
                    Cossacks2Bridge.UnityAdapters.Maps.C2MapLoadLighting.ApplyMapLoadDefaultsLikeOriginal();
                    C2BattleTerrainMode.OpenFromBattles(_bootstrap);
                    break;
                }

            case "cva_Battles_Back":
                {
                    _bootstrap?.RenderByScreenId("Single");
                    break;
                }

            case "cva_MM_Close":
                {
                    if (_bootstrap != null && string.Equals(_bootstrap.CurrentScreenId, "Single", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("[C2:SINK] Close on Single -> Main");
                        _bootstrap.RenderByScreenId("Main");
                        break;
                    }

                    Debug.Log("[C2:SINK] Close -> Application.Quit()");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
                }

            case "cva_MM_MultiBack":
                {
                    _bootstrap?.RenderPreviousOrMain();
                    break;
                }

            case "cva_M_ModalDeskSet":
                {
                    string modalName = TryGetTag(action.Payload, "Name");
                    if (_pendingBfeEntryV396A2 && string.Equals(modalName, "Campaign", StringComparison.OrdinalIgnoreCase))
                    {
                        // Exact source order: Battle4Europe requests ModalDesk=Campaign
                        // before cva_SPD_CampMessageCheck decides whether it should
                        // remain visible on this first entry.
                        Debug.Log("[C2:BFE14 ENTRY V396A3] ModalDesk=Campaign source request deferredUntil=CampMessageCheck");
                    }
                    else if (string.Equals(modalName, "Campaign", StringComparison.OrdinalIgnoreCase))
                    {
                        bool shown = _bootstrap != null && _bootstrap.ShowCampaignModalOriginalV396A3(this);
                        Debug.Log($"[C2:BFE14 ENTRY V396A3] ModalDesk=Campaign sourceXmlShown={(shown ? 1 : 0)}");
                    }
                    else if (string.Equals(modalName, "Main", StringComparison.OrdinalIgnoreCase))
                    {
                        _bootstrap?.CloseCampaignModalOriginalV396A3();
                        Debug.Log("[C2:BFE14 ENTRY V396A3] ModalDesk=Main -> original Campaign modal closed");
                    }
                    else
                    {
                        Debug.Log("[C2:SINK] Ignored action: " + action.Name);
                    }
                    break;
                }

            case "cva_SPD_CampMessageCheck":
                {
                    if (_pendingBfeEntryV396A2)
                    {
                        _pendingBfeEntryV396A2 = false;
                        C2ProfileRuntime14.EnsureLoaded();
                        var p = C2ProfileRuntime14.Current;
                        if (p == null)
                        {
                            Debug.LogWarning("[C2:BFE14 ENTRY V396A2] CampMessageCheck has no CurPlayer -> AddProfile");
                            _bootstrap?.RenderByScreenId("AddProfile");
                            break;
                        }

                        // V396A5 migration: show the corrected source-driven intro once after
                        // the CP1251/font-metrics fix so existing profiles can verify the repaired
                        // original modal. Afterwards DisableTutorialMessage again has original semantics.
                        string sourceModalRepairKey = "C2_BFE14_V396A5_SOURCE_TEXT_FIXED_SHOWN_" + (p.m_chName ?? string.Empty);
                        bool repairOldSyntheticDisplay = p.DisableTutorialMessage && PlayerPrefs.GetInt(sourceModalRepairKey, 0) == 0;

                        if (repairOldSyntheticDisplay)
                        {
                            bool shown = _bootstrap != null && _bootstrap.ShowCampaignModalOriginalV396A3(this);
                            if (shown)
                            {
                                PlayerPrefs.SetInt(sourceModalRepairKey, 1);
                                PlayerPrefs.Save();
                                Debug.Log("[C2:BFE14 ENTRY V396A5] migration=SOURCE_TEXT_FIXED -> show repaired SOURCE Campaign modal once; profile rulesSeen remains YES");
                            }
                            else
                            {
                                Debug.LogError("[C2:BFE14 ENTRY V396A3] source Campaign modal migration failed; no synthetic fallback");
                            }
                        }
                        else if (p.DisableTutorialMessage)
                        {
                            Debug.Log("[C2:BFE14 ENTRY V396A3] rulesSeen=YES -> Campaign Start -> BigMap");
                            OpenConquestOfEuropeV395B("cva_SPD_CampMessageCheck:rulesSeen");
                        }
                        else
                        {
                            // Exact 1.1 semantic: mark the message as seen and save
                            // immediately, while leaving the source Campaign modal visible.
                            p.DisableTutorialMessage = true;
                            C2ProfileRuntime14.SaveCurrent();
                            bool shown = _bootstrap != null && _bootstrap.ShowCampaignModalOriginalV396A3(this);
                            if (shown)
                            {
                                PlayerPrefs.SetInt(sourceModalRepairKey, 1);
                                PlayerPrefs.Save();
                                Debug.Log("[C2:BFE14 ENTRY V396A3] rulesSeen=NO -> markSeen=YES save=YES modal=SOURCE M_Single/DialogsDesk Campaign");
                            }
                            else
                            {
                                // Do not consume first-run state if the original source
                                // modal could not be constructed. A later launch may retry.
                                p.DisableTutorialMessage = false;
                                C2ProfileRuntime14.SaveCurrent();
                                Debug.LogError("[C2:BFE14 ENTRY V396A3] source Campaign modal failed -> rulesSeen restored to NO; no synthetic fallback");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[C2:SINK] Ignored action: " + action.Name);
                    }
                    break;
                }

            case "cva_MM_MultiJoin":
            case "cva_MM_MultiCreate":
            case "cva_DemoDisable":
            case "cva_vGameMode_Set":
                {
                    Debug.Log("[C2:SINK] Ignored action: " + action.Name);
                    break;
                }

            default:
                {
                    Debug.Log("[C2:SINK] Unhandled action: " + action.Name);
                    break;
                }
        }
    }

    private static bool IsConquestTargetV395B(string id)
    {
        return string.Equals(id, "SinGlobalMap", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(id, "BigMap", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(id, "Campaign", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(id, "ConquestOfEurope", StringComparison.OrdinalIgnoreCase);
    }

    private void OpenConquestOfEuropeV395B(string source)
    {
        C2ProfileRuntime14.EnsureLoaded();

        if (!C2ProfileRuntime14.HasProfiles || C2ProfileRuntime14.Current == null)
        {
            Debug.LogWarning($"[C2:BIGMAP V395B] blocked source='{source}': no CurPlayer -> AddProfile");
            _bootstrap?.SetHasProfile(false);
            _bootstrap?.RenderByScreenId("AddProfile");
            return;
        }

        SetCurrentProfileFromRuntime(C2ProfileRuntime14.Current.m_chName);
        _bootstrap?.SetHasProfile(true);
        Debug.Log($"[C2:BIGMAP V395B] open source='{source}' profile='{C2ProfileRuntime14.Current.m_chName}' -> SinGlobalMap");
        _bootstrap?.RenderByScreenId("SinGlobalMap");
    }

    public static string SetNetworkPlayerNickV387B1(string value)
    {
        NetworkPlayerNick = SanitizeNetworkNickV387B1(value);
        return NetworkPlayerNick;
    }

    private static string SanitizeNetworkNickV387B1(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string s = value
            .Replace('\\', ' ')
            .Replace('{', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        s = s.TrimStart();
        if (s.Length > 17)
            s = s.Substring(0, 17);

        return s;
    }

    private void CaptureProfileNameFromScene()
    {
        string value = "";

        var tmpFields = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var field in tmpFields)
        {
            if (field == null) continue;
            value = field.text?.Trim();
            if (!string.IsNullOrWhiteSpace(value)) break;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            var legacyFields = FindObjectsByType<InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var field in legacyFields)
            {
                if (field == null) continue;
                value = field.text?.Trim();
                if (!string.IsNullOrWhiteSpace(value)) break;
            }
        }

        if (!string.IsNullOrWhiteSpace(value))
            CurrentProfileName = value;
    }

    private static string TryGetTag(string xmlLike, string tag)
    {
        if (string.IsNullOrEmpty(xmlLike) || string.IsNullOrEmpty(tag)) return "";
        var open = "<" + tag + ">";
        var close = "</" + tag + ">";
        int a = xmlLike.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        if (a < 0) return "";
        a += open.Length;
        int b = xmlLike.IndexOf(close, a, StringComparison.OrdinalIgnoreCase);
        if (b < 0) return "";
        return xmlLike.Substring(a, b - a).Trim();
    }
}
