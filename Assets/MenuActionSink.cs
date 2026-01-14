using System;
using UnityEngine;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;  // ✅ Для IUiActionSink

/// <summary>
/// Minimal "executor" for UI actions coming from renderers.
/// </summary>
public sealed class MenuActionSink : MonoBehaviour, IUiActionSink
{
    private Cossacks2Bridge.UnityAdapters.MenuBootstrap _bootstrap;

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
            case "cva_MM_Start":
            {
                var id = ExtractTargetFromPayload(action.Payload);
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
                if (string.IsNullOrWhiteSpace(id))
                    id = TryGetTag(action.Payload, "Name");

                Debug.Log($"[C2:SINK] Desk_Set -> id='{id}'");
                if (_bootstrap != null && !string.IsNullOrWhiteSpace(id))
                    _bootstrap.RenderByScreenId(id);
                else
                    Debug.LogWarning("[C2:SINK] Desk_Set ignored (missing ID or bootstrap)");
                break;
            }

            case "cva_MM_SinStart":
                {
                    Debug.Log("[C2:SINK] SinStart -> go 'Single'");
                    _bootstrap?.RenderByScreenId("Single");
                    break;
                }

            case "cva_MM_Close":
            {
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
            {
                Debug.Log("[C2:SINK] MultiJoin stub (no net yet)");
                break;
            }
            case "cva_MM_MultiCreate":
            {
                Debug.Log("[C2:SINK] MultiCreate stub (no net yet)");
                break;
            }

            default:
                Debug.Log("[C2:SINK] Unhandled action: " + action.Name);
                break;
            case "cva_MM_MultiEnter":
                {
                    _bootstrap?.RenderByScreenId("Multi");
                    break;
                }

            case "cva_DemoDisable":
                {
                    Debug.Log("[C2:SINK] DemoDisable stub");
                    break;
                }
        }
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