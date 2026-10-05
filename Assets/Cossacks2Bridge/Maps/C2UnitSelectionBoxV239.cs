// C2UnitSelectionBoxV239.cs
// Restores C2-like click/rectangle unit selection for the copied unit runtime.
// V244: dark original-like rectangle color; no build/construction logic.
// Build/construction logic is not touched.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    [DefaultExecutionOrder(32057)]
    public sealed class C2UnitSelectionBoxV239 : MonoBehaviour
    {
        private static C2UnitSelectionBoxV239 _active;
        private bool _dragging;
        private bool _placementGesture;
        private Vector2 _start;
        private Vector2 _current;
        private Texture2D _whiteTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (_active != null) return;
            GameObject go = new GameObject("C2_UnitSelectionBox_V239");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _active = go.AddComponent<C2UnitSelectionBoxV239>();
            Debug.Log("[C2:UNIT SELECT V239] installed click=1 rect=1 build_logic=0");
        }

        private void Awake()
        {
            _active = this;
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply(false, true);
        }

        private void Update()
        {
            bool held = MouseButtonHeldSafe(0);
            // Placement owns its entire press/release, even when confirmation
            // closes the preview earlier in this frame.
            if (C2BuildingPlacementV377InputGuardLikeOriginal() || _placementGesture)
            {
                _dragging = false;
                _placementGesture = held;
                return;
            }

            Vector2 mouse = MousePositionSafe();
            if (!_dragging && MouseButtonDownSafe(0) && !IsPointerOverUi())
            {
                _dragging = true;
                _start = mouse;
                _current = mouse;
            }
            if (!_dragging) return;
            _current = mouse;
            // Once begun on the map, the gesture owns mouse-up over the HUD too.
            // A lost release (focus/input transition) must never leave a stale box.
            if (MouseButtonUpSafe(0) || !held)
            {
                _dragging = false;
                Rect r = ScreenRectFromPoints(_start, _current);
                bool rectangle = r.width >= 6.0f || r.height >= 6.0f;
                if (rectangle) SelectInRect(r, false);
                else if (!IsPointerOverUi()) SelectSingle(_current, false);
            }
        }

        private static bool C2BuildingPlacementV377InputGuardLikeOriginal()
        {
            return C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal ||
                   C2BuildingPlacementPreviewV27.PlacementConsumedMouseFrame == Time.frameCount;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { _dragging = false; _placementGesture = false; }
        }

        private void OnDisable() { _dragging = false; _placementGesture = false; }

        private static Vector2 MousePositionSafe()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null)
                    return Mouse.current.position.ReadValue();
            }
            catch { }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.mousePosition; } catch { }
#endif
            return Vector2.zero;
        }

        private static bool MouseButtonDownSafe(int button)
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null)
                {
                    if (button == 0) return Mouse.current.leftButton.wasPressedThisFrame;
                    if (button == 1) return Mouse.current.rightButton.wasPressedThisFrame;
                    if (button == 2) return Mouse.current.middleButton.wasPressedThisFrame;
                }
            }
            catch { }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButtonDown(button); } catch { }
#endif
            return false;
        }

        private static bool MouseButtonHeldSafe(int button)
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null)
                {
                    if (button == 0) return Mouse.current.leftButton.isPressed;
                    if (button == 1) return Mouse.current.rightButton.isPressed;
                    if (button == 2) return Mouse.current.middleButton.isPressed;
                }
            }
            catch { }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButton(button); } catch { }
#endif
            return false;
        }

        private static bool MouseButtonUpSafe(int button)
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null)
                {
                    if (button == 0) return Mouse.current.leftButton.wasReleasedThisFrame;
                    if (button == 1) return Mouse.current.rightButton.wasReleasedThisFrame;
                    if (button == 2) return Mouse.current.middleButton.wasReleasedThisFrame;
                }
            }
            catch { }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButtonUp(button); } catch { }
#endif
            return false;
        }

        private void OnGUI()
        {
            if (!_dragging) return;
            Rect r = ScreenRectFromPoints(_start, _current);
            if (r.width < 6.0f && r.height < 6.0f) return;
            Rect gui = new Rect(r.xMin, Screen.height - r.yMax, r.width, r.height);

            if (_whiteTex == null) return;
            Color old = GUI.color;
            // V244: original-like dark selection rectangle instead of bright yellow debug box.
            // Keep it visible, but do not make it look like a warning/debug overlay.
            GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.10f);
            GUI.DrawTexture(gui, _whiteTex);
            GUI.color = new Color(0.02f, 0.02f, 0.02f, 0.88f);
            GUI.DrawTexture(new Rect(gui.xMin, gui.yMin, gui.width, 1), _whiteTex);
            GUI.DrawTexture(new Rect(gui.xMin, gui.yMax - 1, gui.width, 1), _whiteTex);
            GUI.DrawTexture(new Rect(gui.xMin, gui.yMin, 1, gui.height), _whiteTex);
            GUI.DrawTexture(new Rect(gui.xMax - 1, gui.yMin, 1, gui.height), _whiteTex);
            GUI.color = old;
        }

        private static bool IsPointerOverUi()
        {
            try
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return true;
            }
            catch { }
            return false;
        }

        private static Rect ScreenRectFromPoints(Vector2 a, Vector2 b)
        {
            float x1 = Mathf.Min(a.x, b.x);
            float x2 = Mathf.Max(a.x, b.x);
            float y1 = Mathf.Min(a.y, b.y);
            float y2 = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(x1, y1, x2, y2);
        }

        private static Camera FindBattleCamera()
        {
            Camera[] cams = Camera.allCameras;
            Camera best = null;
            for (int i = 0; cams != null && i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (c == null || !c.isActiveAndEnabled) continue;
                if (c.name.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                    return c;
                if (best == null && c.name.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0)
                    best = c;
            }
            return best != null ? best : Camera.main;
        }

        private static void SelectSingle(Vector2 mouse, bool additive)
        {
            Camera cam = FindBattleCamera();
            if (cam == null) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            float bestDist = float.MaxValue;

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled || u.NotSelectable || !u.CanReceivePlayerOrdersLikeOriginal()) continue;
                if (!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation)) continue;

                float alpha;
                Vector2 uv;
                if (u.TryPixelHit(cam, new Vector3(mouse.x, mouse.y, 0), out alpha, out uv) && alpha > 0.01f)
                {
                    Vector2 anchor;
                    Vector4 rect;
                    float dist;
                    if (u.TryGetScreenQuadDistance(cam, new Vector3(mouse.x, mouse.y, 0), out dist, out anchor, out rect) && dist < bestDist)
                    {
                        best = u;
                        bestDist = dist;
                    }
                }
            }

            if (best != null)
            {
                if (!additive) DeselectAll();
                int selected = SelectUnitOrWholeFormationV321LikeOriginal(best);
                C2GameplayHudV1.C2GameplayHudV28InvalidateBuildModeLikeOriginal();
                Debug.Log("[C2:UNIT SELECT V321] SELECT_BRIGADE_OR_ONE count=" + selected.ToString() +
                          " unit='" + (best.SourceMonsterId ?? string.Empty) + "'");
                return;
            }

            C2SettlementBuildingSelectableV1LikeOriginal bestBuilding = PickBuildingAtScreenPointV247LikeOriginal(cam, mouse);
            if (!additive) DeselectAll();
            if (bestBuilding != null)
            {
                bestBuilding.SetSelected(true);
                C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal = bestBuilding;
                C2GameplayHudV1.C2GameplayHudV28InvalidateBuildModeLikeOriginal();
                Debug.Log("[C2:BUILDING SELECT V247] SELECT_ONE building='" +
                          (bestBuilding.SourceMonsterId ?? bestBuilding.KindName ?? string.Empty) +
                          "' rec=" + bestBuilding.RecordIndex.ToString());
            }
        }

        private static void SelectInRect(Rect r, bool additive)
        {
            Camera cam = FindBattleCamera();
            if (cam == null) return;

            if (!additive) DeselectAll();

            int selected = 0;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || !u.isActiveAndEnabled || u.NotSelectable || !u.CanReceivePlayerOrdersLikeOriginal()) continue;
                if (!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation)) continue;

                Vector2 anchor;
                Vector4 rectv;
                float dist;
                if (!u.TryGetScreenQuadDistance(cam, new Vector3(r.center.x, r.center.y, 0), out dist, out anchor, out rectv))
                    continue;

                Rect unitRect = Rect.MinMaxRect(rectv.x, rectv.y, rectv.z, rectv.w);
                if (r.Overlaps(unitRect, true) || r.Contains(anchor, true))
                {
                    selected += SelectUnitOrWholeFormationV321LikeOriginal(u);
                }
            }

            C2GameplayHudV1.C2GameplayHudV28InvalidateBuildModeLikeOriginal();
            Debug.Log("[C2:UNIT SELECT V239] SELECT_RECT count=" + selected.ToString() +
                      " rect=(" + r.xMin.ToString("0") + "," + r.yMin.ToString("0") + "," + r.xMax.ToString("0") + "," + r.yMax.ToString("0") + ")");
        }

        private static int SelectUnitOrWholeFormationV321LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null)
                return 0;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> brigade;
            int groupId;
            string shape;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    unit, out brigade, out groupId, out shape) ||
                brigade == null || brigade.Count == 0)
            {
                bool wasSelected = unit.IsSelected;
                unit.SetSelected(true);
                unit.ForceUpdateSelectionVisualsV42LikeOriginal();
                return wasSelected ? 0 : 1;
            }

            int selected = 0;
            for (int i = 0; i < brigade.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = brigade[i];
                if (member == null || !member.isActiveAndEnabled || member.NotSelectable)
                    continue;
                if (!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(member.Nation))
                    continue;
                if (!member.IsSelected)
                    selected++;
                member.SetSelected(true);
                member.ForceUpdateSelectionVisualsV42LikeOriginal();
            }
            return selected;
        }

        private static C2SettlementBuildingSelectableV1LikeOriginal PickBuildingAtScreenPointV247LikeOriginal(Camera cam, Vector2 mouse)
        {
            if (cam == null) return null;

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (buildings == null || buildings.Length == 0)
                return null;

            Array.Sort(buildings, (a, b) =>
            {
                int sa = a != null ? a.SortKey : 0;
                int sb = b != null ? b.SortKey : 0;
                int c = sb.CompareTo(sa);
                if (c != 0) return c;
                int ia = a != null ? a.RecordIndex : 0;
                int ib = b != null ? b.RecordIndex : 0;
                return ib.CompareTo(ia);
            });

            C2SettlementBuildingSelectableV1LikeOriginal best = null;
            float bestDist = float.MaxValue;
            Vector3 sp = new Vector3(mouse.x, mouse.y, 0.0f);

            for (int i = 0; i < buildings.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                if (b == null || !b.isActiveAndEnabled || b.NotSelectable)
                    continue;
                if (!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(b.Nation))
                    continue;

                Rect rect;
                float dist;
                if (!b.TryPickScreenPointLikeOriginal(cam, sp, out rect, out dist))
                    continue;

                if (dist < bestDist)
                {
                    best = b;
                    bestDist = dist;
                }
            }

            return best;
        }

        private static void DeselectAll()
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                if (all[i] == null) continue;
                all[i].SetSelected(false);
                all[i].ForceUpdateSelectionVisualsV42LikeOriginal();
            }

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; buildings != null && i < buildings.Length; i++)
            {
                if (buildings[i] == null) continue;
                buildings[i].SetSelected(false);
            }
            C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal = null;
        }
    }
}
