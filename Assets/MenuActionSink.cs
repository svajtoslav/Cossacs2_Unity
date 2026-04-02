using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;
using Cossacks2Bridge.UnityAdapters.Maps;

/// <summary>
/// Minimal "executor" for UI actions coming from renderers.
/// </summary>
public sealed class MenuActionSink : MonoBehaviour, IUiActionSink
{
    private Cossacks2Bridge.UnityAdapters.MenuBootstrap _bootstrap;
    public static string CurrentProfileName { get; private set; } = "";
    public static bool SingleBattlesShowBattles { get; set; } = false;
    public static bool SingleBattlesShowLoad { get; set; } = false;
    public static string SingleBattlesSelectedId { get; set; } = "";
    public static bool SingleBattlesArcadeModeEnabled { get; set; } = false;

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
                    _bootstrap?.SetHasProfile(true);
                    _bootstrap?.RenderByScreenId("Single");
                    break;
                }

            case "cva_ProfAdd_Cancel":
                {
                    _bootstrap?.RenderByScreenId("Main");
                    break;
                }

            case "cva_MM_Start":
                {
                    var id = ExtractTargetFromPayload(action.Payload);
                    if (string.Equals(id, "SelProfile", StringComparison.OrdinalIgnoreCase))
                        id = "AddProfile";

                    Debug.Log($"[C2:SINK] MM_Start -> go '{id}' (payload='{action.Payload}')");
                    if (_bootstrap != null && !string.IsNullOrWhiteSpace(id))
                        _bootstrap.RenderByScreenId(id);
                    else
                        Debug.LogWarning("[C2:SINK] MM_Start ignored (missing target id or bootstrap)");
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
                    else if (string.Equals(id, "SinGlobalMap", StringComparison.OrdinalIgnoreCase))
                        id = "";

                    Debug.Log($"[C2:SINK] Desk_Set -> id='{id}'");
                    if (_bootstrap != null && !string.IsNullOrWhiteSpace(id))
                        _bootstrap.RenderByScreenId(id);
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

            case "cva_MM_MultiJoin":
            case "cva_MM_MultiCreate":
            case "cva_DemoDisable":
            case "cva_vGameMode_Set":
            case "cva_M_ModalDeskSet":
            case "cva_SPD_CampMessageCheck":
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
