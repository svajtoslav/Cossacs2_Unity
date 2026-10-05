// C2BuildingPassabilityOverlayHotkeyCompatV246.cs
// V247: real Q overlay controller for current 23_05 building renderer.
// 0 off
// 1 LOCKPOINTS
// 2 BUILDLOCKPOINTS
// 3 CHECKPOINTS
// 4 BUILDPOINTS
// 5 BORN/CONC active route tester: Q cycles one visible/active path at a time
// V295 Q order: OFF -> BORN_V283 -> CONC_V283 -> RAW_BORN -> RAW_CONC -> LOCKPOINTS -> BUILDLOCKPOINTS -> CHECKPOINTS -> BUILDPOINTS -> LINESORT -> OFF
// V296: mode 5 draws the active route on all buildings in world space; no camera-refresh flicker.
// V301 order: OFF -> LOCKPOINTS -> BUILDLOCKPOINTS -> CHECKPOINTS -> BUILDPOINTS -> BORN_V283_EXIT -> CONC_V283_ENTRY -> LINESORT -> OFF.
// 6 LINESORT

using System.Globalization;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2BuildingPassabilityOverlayHotkeyLikeOriginal : MonoBehaviour
    {
        private static C2BuildingPassabilityOverlayHotkeyLikeOriginal s_active;
        private static int s_mode;

        public static int CurrentModeLikeOriginal
        {
            get { return s_mode; }
        }

        public static string CurrentModeLabelLikeOriginal
        {
            get
            {
                if (s_mode == 1) return "LOCKPOINTS";
                if (s_mode == 2) return "BUILDLOCKPOINTS";
                if (s_mode == 3) return "CHECKPOINTS";
                if (s_mode == 4) return "BUILDPOINTS";
                if (s_mode == 5) return C2BuildingRuntimeInfoV247LikeOriginal.ActiveBornConcRouteLabelV294LikeOriginal;
                if (s_mode == 6) return "LINESORT";
                return "OFF";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstallV247LikeOriginal()
        {
            if (s_active != null) return;
            GameObject go = new GameObject("C2_Building_Q_OverlayHotkey_V247");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave;
            s_active = go.AddComponent<C2BuildingPassabilityOverlayHotkeyLikeOriginal>();
            s_mode = 0;
            C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(0);
            C2BuildingRuntimeInfoV247LikeOriginal.HardCleanupOverlayV295LikeOriginal();
            Debug.Log("[C2:BUILDING Q OVERLAY V301] installed modes=0_OFF/1_LOCKPOINTS/2_BUILDLOCKPOINTS/3_CHECKPOINTS/4_BUILDPOINTS/5_HARD_BORN_CONC_ROUTE/6_LINESORT qOrder=OFF->LOCKPOINTS->BUILDLOCKPOINTS->CHECKPOINTS->BUILDPOINTS->BORN_V283_EXIT->CONC_V283_ENTRY->LINESORT->OFF hard_rule=exit_green_BORN_entry_fuchsia_CONC wide_zone_overlay=1 raw_routes_removed_from_q=1");
        }

        private float _nextMode5RefreshUnscaled;

        private void Awake()
        {
            s_active = this;
            s_mode = 0;
            C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(0);
            C2BuildingRuntimeInfoV247LikeOriginal.HardCleanupOverlayV295LikeOriginal();
        }

        private void Update()
        {
            if (!WasQPressedV247LikeOriginal())
            {
                // V296 uses world-attached overlay; no per-camera-refresh rebuild, otherwise
                // the line flickers and may appear detached from the map while panning.
                return;
            }

            if (s_mode == 0)
            {
                s_mode = 1;
                C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(0);
            }
            else if (s_mode >= 1 && s_mode < 4)
            {
                s_mode++;
            }
            else if (s_mode == 4)
            {
                s_mode = 5;
                C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(0);
            }
            else if (s_mode == 5)
            {
                int nextRoute = C2BuildingRuntimeInfoV247LikeOriginal.ActiveBornConcRouteV294LikeOriginal + 1;
                if (nextRoute < C2BuildingRuntimeInfoV247LikeOriginal.BornConcRouteCountV294LikeOriginal)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(nextRoute);
                }
                else
                {
                    C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(0);
                    s_mode = 6;
                }
            }
            else
            {
                s_mode = 0;
                C2BuildingRuntimeInfoV247LikeOriginal.SetActiveBornConcRouteV294LikeOriginal(0);
            }

            _nextMode5RefreshUnscaled = 0.0f;
            C2BuildingRuntimeInfoV247LikeOriginal.RebuildOverlayForCurrentModeLikeOriginal();

            // Placement preview has its own optional overlay; keep old hook alive.
            C2BuildingPlacementPreviewV27.C2BuildPlacementRefreshDebugOverlayLikeOriginal();

            Debug.Log("[C2:BUILDING Q OVERLAY V301] mode=" +
                      s_mode.ToString(CultureInfo.InvariantCulture) +
                      " label='" + CurrentModeLabelLikeOriginal + "'" +
                      " activeRoute=" + C2BuildingRuntimeInfoV247LikeOriginal.ActiveBornConcRouteV294LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " activeRouteLabel='" + C2BuildingRuntimeInfoV247LikeOriginal.ActiveBornConcRouteLabelV294LikeOriginal + "'");
        }

        private static bool WasQPressedV247LikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Q))
                    return true;
            }
            catch
            {
            }
#endif
            return false;
        }
    }
}
