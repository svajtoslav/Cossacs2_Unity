using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public enum C2GameplayTargetKindV1
    {
        None = 0,
        Terrain = 1,
        Tree = 2,
        Stone = 3,
        Field = 4,
        Enemy = 5,
        Building = 6,
        FriendlyUnit = 7,
        Unknown = 255
    }

    public sealed class C2GameplayInteractableZoneV1 : MonoBehaviour
    {
        public C2GameplayTargetKindV1 Kind = C2GameplayTargetKindV1.Unknown;
        public string Source = string.Empty;
    }

    public sealed class C2GameplayUnitTaskV1 : MonoBehaviour
    {
        public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
        public C2GameplayTargetKindV1 TaskKind;
        public Vector3 TargetWorld;
        public float WorkStartDistance = 1.35f;

        private float _phase;
        private float _until;
        private bool _active;

        public void Begin(C2NeutralPeasantUnitInfoV2LikeOriginal unit, C2GameplayTargetKindV1 kind, Vector3 targetWorld, float durationSeconds)
        {
            Unit = unit != null ? unit : GetComponent<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            TaskKind = kind;
            TargetWorld = targetWorld;
            _phase = 0.0f;
            _until = Time.realtimeSinceStartup + Mathf.Max(1.0f, durationSeconds);
            _active = Unit != null;

            if (Unit != null)
            {
                Unit.SetMoveDestinationLikeOriginal(targetWorld);
            }
        }

        private void Update()
        {
            if (!_active || Unit == null || Unit.SpriteAnimator == null)
                return;

            if (Time.realtimeSinceStartup > _until)
            {
                Unit.SpriteAnimator.SetMovingLikeOriginal(false);
                _active = false;
                return;
            }

            Vector3 flatSelf = Unit.transform.position; flatSelf.y = 0.0f;
            Vector3 flatTarget = TargetWorld; flatTarget.y = 0.0f;
            float dist = Vector3.Distance(flatSelf, flatTarget);
            if (dist > WorkStartDistance)
                return;

            Vector3 d = flatTarget - flatSelf;
            byte dir = DirectionFromWorldDelta(d);
            Unit.SetFacingDirectionLikeOriginal(dir);

            // Временный рабочий слой задачи. Реальные MD-actions рубки/добычи/жатвы/атаки подключаются отдельно.
            Unit.SpriteAnimator.SetMovingLikeOriginal(true);
            Unit.SpriteAnimator.SetMotionStateLikeOriginal(Unit.GraphDir, false);
            _phase += Time.deltaTime * Mathf.Max(1.0f, Unit.MotionDist) * 3.0f;
            Unit.SpriteAnimator.SetWalkPathFrameLikeOriginal(_phase, Mathf.Max(1.0f, Unit.MotionDist));
        }

        private static byte DirectionFromWorldDelta(Vector3 d)
        {
            if (d.sqrMagnitude < 0.0001f) return 0;
            float angle = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
            int raw = Mathf.RoundToInt(Mathf.Repeat(angle / 360.0f * 256.0f, 256.0f));
            int snapped = (raw + 8) & 0xF0;
            return (byte)(snapped & 255);
        }

        private static string Vec3(Vector3 v)
        {
            return "(" + v.x.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                   v.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                   v.z.ToString("0.00", CultureInfo.InvariantCulture) + ")";
        }
    }

    public sealed class C2GameplayInteractionControllerV1 : MonoBehaviour
    {
        private const string Contract = "V6J_NO_NATURE_MESHCOLLIDER_NO_RAYCASTALL_RESOURCE_TABLE_ONLY";
        private static C2GameplayInteractionControllerV1 _active;

        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _selected = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);
        private float _nextColliderScan;
        private float _nextLog;
        private float _nextUnitScan;
        private bool _hasBattleUnitsCached;
        private C2BattleTerrainMode _cachedMode;
        private float _nextModeLookup;
        private bool _loggedColliderDisabled;
        private bool _loggedNoPhysicsHover;
        private C2GameplayTargetKindV1 _hoverKind;
        private Vector3 _hoverWorld;
        private string _hoverSource = string.Empty;
        private C2SettlementBuildingSelectableV1LikeOriginal _hoverBuilding;
        private C2RuntimeConstructionSiteProxyLikeOriginal _hoverConstruction;

        private Canvas _cursorCanvas;
        private Image _cursorImage;
        private RectTransform _cursorRect;
        private bool _softwareCursorReady;
        private bool _cursorHidden;
        private int _lastCurPtr = int.MinValue;
        private int _lastHardwareCurPtr = int.MinValue;
        private readonly HashSet<int> _loggedCursorPtrOnce = new HashSet<int>();
        private C2OriginalHardCursorFrameV5 _lastCursorFrame;
        private bool _loggedMissingCursor;
        private Vector2 _guiMouseTopLeft;
        private bool _hasGuiMouseTopLeft;
        private int _lastLoggedSetCurPtr = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (_active != null) return;
            GameObject go = new GameObject("C2_GameplayInteraction_OriginalHardCursor_V6J");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _active = go.AddComponent<C2GameplayInteractionControllerV1>();
        }

        private void Awake()
        {
            _active = this;
            // V5D: cursor is applied through Unity hardware cursor API.
            // No overlay canvas and no OnGUI draw path: this is visible in GameView and costs almost nothing.
            string audit = C2OriginalHardCursorProviderV5.PrewarmOriginalHardCursors();
            _ = audit;
        }

        private void OnDestroy()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            _cursorHidden = false;
            _lastHardwareCurPtr = int.MinValue;
            if (_active == this) _active = null;
        }

        private void Update()
        {
            RefreshSelectedCached();

            bool hasBattleUnits = _hasBattleUnitsCached;
            if (hasBattleUnits && Time.realtimeSinceStartup >= _nextColliderScan)
            {
                _nextColliderScan = Time.realtimeSinceStartup + 3.0f;
                EnsureSceneInteractionColliders();
            }

            if (hasBattleUnits)
                UpdateHover();
            else
            {
                _hoverKind = C2GameplayTargetKindV1.None;
                _hoverWorld = Vector3.zero;
                _hoverSource = string.Empty;
            }

            HandleRightClickTaskOnly();
            UpdateOriginalHardCursor();

        }

        private void OnGUI()
        {
            // V5D: disabled intentionally.
            // V5C hid the system cursor and tried to repaint through IMGUI; in this project/GameView that path did not render.
            // Cursor.SetCursor below is the stable path.
        }

        private static Vector2 MousePositionBottomLeft()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.mousePosition;
#else
            Event e = Event.current;
            if (e != null)
                return new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            return Vector2.zero;
#endif
        }

        private static bool RightMouseButtonPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        private void EnsureSoftwareCursorCanvas()
        {
            if (_cursorCanvas != null && _cursorImage != null && _cursorRect != null)
            {
                _softwareCursorReady = true;
                return;
            }

            GameObject cgo = new GameObject("C2_OriginalHardCursor_Canvas_V5");
            cgo.transform.SetParent(transform, false);
            cgo.hideFlags = HideFlags.HideAndDontSave;

            _cursorCanvas = cgo.AddComponent<Canvas>();
            _cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _cursorCanvas.sortingOrder = 32767;

            GameObject igo = new GameObject("C2_OriginalHardCursor_Image_V5");
            igo.transform.SetParent(cgo.transform, false);
            _cursorImage = igo.AddComponent<Image>();
            _cursorImage.raycastTarget = false;
            _cursorImage.preserveAspect = true;
            _cursorImage.enabled = false;

            _cursorRect = igo.GetComponent<RectTransform>();
            _cursorRect.anchorMin = new Vector2(0.0f, 0.0f);
            _cursorRect.anchorMax = new Vector2(0.0f, 0.0f);
            _cursorRect.pivot = new Vector2(0.0f, 1.0f); // top-left; hotspot компенсируем вручную
            _cursorRect.sizeDelta = new Vector2(32.0f, 32.0f);

            _softwareCursorReady = true;
        }

        private void UpdateOriginalHardCursor()
        {
            int curptr = CursorPtrForHoverLikeOriginal(_hoverKind);
            if (curptr != _lastCurPtr || _lastCursorFrame == null || _lastCursorFrame.Texture == null)
            {
                _lastCurPtr = curptr;
                _lastCursorFrame = C2OriginalHardCursorProviderV5.LoadCursor(curptr, out string audit);
                if (_lastCursorFrame != null && _lastCursorFrame.Texture != null)
                {
                    _loggedMissingCursor = false;
                    if (!_loggedCursorPtrOnce.Contains(curptr))
                    {
                        _loggedCursorPtrOnce.Add(curptr);
                    }
                }
                else if (!_loggedMissingCursor)
                {
                    _loggedMissingCursor = true;
                    Debug.LogWarning("[C2:ORIGINAL HARD CURSOR V5G] missing curptr=" + curptr.ToString(CultureInfo.InvariantCulture) + " " + audit);
                }
            }

            if (_cursorImage != null)
                _cursorImage.enabled = false;

            if (_lastCursorFrame == null || _lastCursorFrame.Texture == null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.visible = true;
                _cursorHidden = false;
                _lastHardwareCurPtr = int.MinValue;
                return;
            }

            if (_lastHardwareCurPtr != curptr)
            {
                Vector2 hotspot = new Vector2(_lastCursorFrame.HotspotX, _lastCursorFrame.HotspotY);
                Cursor.SetCursor(_lastCursorFrame.Texture, hotspot, CursorMode.Auto);
                _lastHardwareCurPtr = curptr;
            }

            // Hardware cursor must stay visible. V5C hid it and depended on OnGUI, which was invisible in the Game window.
            if (!Cursor.visible)
                Cursor.visible = true;
            _cursorHidden = false;
        }

        private void HandleRightClickTaskOnly()
        {
            if (_selected.Count == 0)
                return;

            if (!RightMouseButtonPressedThisFrame())
                return;

            int curptr = CursorPtrForHoverLikeOriginal(_hoverKind);
            if ((_hoverKind == C2GameplayTargetKindV1.Tree ||
                 _hoverKind == C2GameplayTargetKindV1.Stone ||
                 _hoverKind == C2GameplayTargetKindV1.Field ||
                 _hoverKind == C2GameplayTargetKindV1.Enemy) && curptr != 0)
            {
                TryIssueRightClickTask(_hoverKind, _hoverWorld);
            }
            else if (_hoverKind == C2GameplayTargetKindV1.Terrain || curptr == 0)
            {
                IssueMoveOrderLikeOriginal(_hoverWorld);
            }
        }

        private bool IssueMoveOrderLikeOriginal(Vector3 world)
        {
            if (_selected.Count == 0) return false;

            C2BattleTerrainMode mode = null;
            for (int i = 0; i < _selected.Count && mode == null; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u != null && u.isActiveAndEnabled && u.CanReceiveOrdersLikeOriginal())
                    mode = u.OwnerMode;
            }

            if (mode != null)
            {
                float destPxX;
                float destPxY;
                if (mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(world, out destPxX, out destPxY))
                {
                    string audit;
                    int issued = C2GameplayLooseGroupMoveLikeOriginal.IssueMoveLikeOriginal(
                        _selected,
                        destPxX * 16.0f,
                        destPxY * 16.0f,
                        false,
                        0,
                        "move_order_interaction_v68",
                        out audit);

                    if (C2NeutralPeasantUnitsLogGateV45LikeOriginal.Verbose) Debug.Log("[C2:GAMEPLAY INTERACTION MOVE V68] " + audit);
                    return issued > 0;
                }
            }

            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u == null) continue;
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, "move_order_interaction_fallback_v68");
                u.SetMoveDestinationLikeOriginal(world);
            }

            return true;
        }

        private void RefreshSelectedCached()
        {
            // V6H: FindObjectsOfType is not allowed every frame. Selection is refreshed 4 times/sec.
            float now = Time.realtimeSinceStartup;
            if (now < _nextUnitScan)
                return;

            _nextUnitScan = now + 0.25f;
            _selected.Clear();

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            _hasBattleUnitsCached = all != null && all.Length > 0;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u != null && u.isActiveAndEnabled && u.IsSelected && u.CanReceiveOrdersLikeOriginal())
                    _selected.Add(u);
            }
        }

        private void EnsureSceneInteractionColliders()
        {
            // V6H: disabled intentionally. Old V5G added MeshCollider to C2_Nature_GA/TS/FIELD batch meshes.
            // Those batches are huge render meshes, not gameplay hitboxes, and Physics.RaycastAll over them freezes hover/cursor logic.
            // Resource hover is now resolved through C2OriginalResourceMapV1 buckets below, matching original DetermineResource-style lookup.
            if (!_loggedColliderDisabled)
            {
                _loggedColliderDisabled = true;
            }
        }

        private void UpdateHover()
        {
            _hoverKind = C2GameplayTargetKindV1.Terrain;
            _hoverWorld = Vector3.zero;
            _hoverSource = string.Empty;
            _hoverBuilding = null;
            _hoverConstruction = null;

            // UI only cancels gameplay-hover; it must not force a resource cursor.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _hoverKind = C2GameplayTargetKindV1.None;
                return;
            }

            Vector2 mp = MousePositionBottomLeft();
            Camera[] cams = BestPickCameras();
            Vector3 world;
            Camera usedCamera;

            if (!TryScreenToWorldPlaneNoPhysics(mp, cams, SelectedPlaneYLikeOriginal(), out world, out usedCamera))
            {
                _hoverKind = C2GameplayTargetKindV1.None;
                _hoverSource = "no_plane_hit_no_physics";
                return;
            }

            _hoverWorld = world;
            _hoverSource = usedCamera != null ? (usedCamera.name + ":plane_no_physics") : "plane_no_physics";

            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            if (mode == null)
            {
                _hoverKind = C2GameplayTargetKindV1.Terrain;
                return;
            }

            C2SettlementBuildingSelectableV1LikeOriginal hoverBuilding;
            float hoverBuildingDist;
            string hoverBuildingMode;
            if (TryPickBuildingAtScreenPointLikeOriginal(new Vector3(mp.x, mp.y, 0.0f), BestPickCameras(), out hoverBuilding, out hoverBuildingDist, out hoverBuildingMode))
            {
                _hoverBuilding = hoverBuilding;
                _hoverConstruction = hoverBuilding != null ? hoverBuilding.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>() : null;
                _hoverKind = C2GameplayTargetKindV1.Building;
                _hoverSource = "building_pick " + hoverBuildingMode;
                return;
            }

            float oxFloat;
            float oyFloat;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(world, out oxFloat, out oyFloat))
            {
                _hoverKind = C2GameplayTargetKindV1.Terrain;
                _hoverSource += ":world_to_original_failed";
                return;
            }

            int ox = Mathf.RoundToInt(oxFloat);
            int oy = Mathf.RoundToInt(oyFloat);

            // Build is cached inside C2OriginalResourceMapV1; after that hover uses only bucket lookup.
            if (!mode.C2OriginalResourceMapV1IsReadyLikeOriginal())
                mode.C2OriginalResourceMapV1TryBuildLikeOriginal("interaction-hover-v6j");

            byte resourceId;
            string audit;
            if (mode.C2OriginalResourceMapV1TryDetermineResourceLikeOriginal(ox, oy, out resourceId, out audit))
            {
                C2GameplayTargetKindV1 rk = TargetKindFromOriginalResourceId(resourceId);
                if (rk != C2GameplayTargetKindV1.Unknown && rk != C2GameplayTargetKindV1.None)
                {
                    _hoverKind = rk;
                    _hoverSource = "resource_lookup_no_physics " + audit;
                    if (!_loggedNoPhysicsHover)
                    {
                        _loggedNoPhysicsHover = true;
                    }
                    return;
                }
            }

            _hoverKind = C2GameplayTargetKindV1.Terrain;
        }

        private C2BattleTerrainMode GetBattleTerrainModeCached()
        {
            float now = Time.realtimeSinceStartup;
            if (_cachedMode != null && now < _nextModeLookup)
                return _cachedMode;

            _nextModeLookup = now + 1.0f;
            _cachedMode = FindObjectOfType<C2BattleTerrainMode>();
            return _cachedMode;
        }

        private float SelectedPlaneYLikeOriginal()
        {
            if (_selected.Count == 0)
                return 0.0f;

            float sum = 0.0f;
            int count = 0;
            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u == null) continue;
                sum += u.transform.position.y;
                count++;
            }

            return count > 0 ? sum / count : 0.0f;
        }

        private static bool TryScreenToWorldPlaneNoPhysics(Vector2 mouseBottomLeft, Camera[] cams, float planeY, out Vector3 world, out Camera usedCamera)
        {
            world = Vector3.zero;
            usedCamera = null;
            if (cams == null) return false;

            Plane plane = new Plane(Vector3.up, new Vector3(0.0f, planeY, 0.0f));
            for (int i = 0; i < cams.Length; i++)
            {
                Camera cam = cams[i];
                if (cam == null || !cam.isActiveAndEnabled) continue;

                Ray ray = cam.ScreenPointToRay(mouseBottomLeft);
                float enter;
                if (!plane.Raycast(ray, out enter) || enter < 0.0f)
                    continue;

                world = ray.GetPoint(enter);
                world.y = planeY;
                usedCamera = cam;
                return true;
            }

            return false;
        }

        private static C2GameplayTargetKindV1 TargetKindFromOriginalResourceId(byte resourceId)
        {
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceWoodV1LikeOriginal) return C2GameplayTargetKindV1.Tree;
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceStoneV1LikeOriginal) return C2GameplayTargetKindV1.Stone;
            if (resourceId == C2BattleTerrainMode.C2OriginalResourceFoodV1LikeOriginal) return C2GameplayTargetKindV1.Field;
            return C2GameplayTargetKindV1.Unknown;
        }

        private static int TargetPriority(C2GameplayTargetKindV1 kind)
        {
            switch (kind)
            {
                case C2GameplayTargetKindV1.Enemy: return 50;
                case C2GameplayTargetKindV1.Tree: return 40;
                case C2GameplayTargetKindV1.Stone: return 40;
                case C2GameplayTargetKindV1.Field: return 40;
                case C2GameplayTargetKindV1.Building: return 30;
                case C2GameplayTargetKindV1.FriendlyUnit: return 20;
                case C2GameplayTargetKindV1.Terrain: return 0;
                default: return -10;
            }
        }

        private bool TryIssueRightClickTask(C2GameplayTargetKindV1 kind, Vector3 targetWorld)
        {
            if (_selected.Count == 0) return false;
            if (CursorPtrForHoverLikeOriginal(kind) == 0) return false;
            if (kind != C2GameplayTargetKindV1.Tree &&
                kind != C2GameplayTargetKindV1.Stone &&
                kind != C2GameplayTargetKindV1.Field &&
                kind != C2GameplayTargetKindV1.Enemy)
                return false;

            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u == null) continue;
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, "new_task_interaction_v67");
                C2GameplayUnitTaskV1 task = u.GetComponent<C2GameplayUnitTaskV1>();
                if (task == null) task = u.gameObject.AddComponent<C2GameplayUnitTaskV1>();
                task.Begin(u, kind, targetWorld, kind == C2GameplayTargetKindV1.Enemy ? 5.0f : 8.0f);
            }
            return true;
        }

        private static C2GameplayTargetKindV1 KindFromHit(RaycastHit hit)
        {
            if (hit.collider == null) return C2GameplayTargetKindV1.Unknown;

            string n = hit.collider.gameObject != null ? (hit.collider.gameObject.name ?? string.Empty) : string.Empty;
            if (IsIgnoredCursorHitObjectName(n))
                return C2GameplayTargetKindV1.Unknown;

            C2GameplayInteractableZoneV1 z = hit.collider.GetComponent<C2GameplayInteractableZoneV1>();
            if (z != null)
            {
                // Old V5E could leave a Tree zone on shadow billboards created in a previous Play run.
                if (z.Source != null && IsIgnoredCursorHitObjectName(z.Source))
                    return C2GameplayTargetKindV1.Unknown;
                return z.Kind;
            }

            C2NeutralPeasantUnitInfoV2LikeOriginal u = hit.collider.GetComponentInParent<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            if (u != null) return u.IsSelected ? C2GameplayTargetKindV1.FriendlyUnit : C2GameplayTargetKindV1.Enemy;

            return ClassifyObjectName(n);
        }

        private static bool IsIgnoredCursorHitObjectName(string n)
        {
            if (string.IsNullOrWhiteSpace(n)) return false;

            // Shadow billboard batches are visual shadows only. They often cover empty ground and must never become Tree hover.
            if (n.IndexOf("C2_Nature_GA_ShadowBillboard", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("ShadowBillboard", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private static C2GameplayTargetKindV1 ClassifyObjectName(string n)
        {
            if (string.IsNullOrWhiteSpace(n)) return C2GameplayTargetKindV1.Unknown;
            if (IsIgnoredCursorHitObjectName(n)) return C2GameplayTargetKindV1.Unknown;
            if (n.IndexOf("C2_Nature_GA_", StringComparison.OrdinalIgnoreCase) >= 0) return C2GameplayTargetKindV1.Tree;
            if (n.IndexOf("C2_Nature_TS_", StringComparison.OrdinalIgnoreCase) >= 0) return C2GameplayTargetKindV1.Stone;
            if (n.IndexOf("C2_Nature_FIELD_", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("FIELDPATH", StringComparison.OrdinalIgnoreCase) >= 0) return C2GameplayTargetKindV1.Field;
            if (n.IndexOf("C2_SettlementBuildings", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("3INU_MD", StringComparison.OrdinalIgnoreCase) >= 0) return C2GameplayTargetKindV1.Building;
            if (n.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0) return C2GameplayTargetKindV1.Terrain;
            return C2GameplayTargetKindV1.Unknown;
        }

        private static Camera[] BestPickCameras()
        {
            List<Camera> result = new List<Camera>(4);
            Camera[] all = Camera.allCameras;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; all != null && i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled || result.Contains(c)) continue;
                    string n = c.name ?? string.Empty;
                    if (pass == 0 && n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0) result.Add(c);
                    if (pass == 1 && n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0) result.Add(c);
                    if (pass == 2 && c == Camera.main) result.Add(c);
                }
            }
            if (result.Count == 0 && Camera.main != null) result.Add(Camera.main);
            return result.ToArray();
        }

        private int CursorPtrForHoverLikeOriginal(C2GameplayTargetKindV1 kind)
        {
            // V67: only enable the original repair/build cursor for unfinished construction sites.
            // Other gameplay cursors stay conservative for now.
            if (kind == C2GameplayTargetKindV1.Building &&
                _hoverConstruction != null &&
                _hoverConstruction.CanAcceptBuildersLikeOriginal &&
                CanSelectedRepairLikeOriginal())
                return 3; // Cursors/Hard/mend.cur

            return 0; // Cursors/Hard/main.cur
        }

        private bool TryPickBuildingAtScreenPointLikeOriginal(
            Vector3 mousePosition,
            Camera[] cameras,
            out C2SettlementBuildingSelectableV1LikeOriginal hit,
            out float hitDist,
            out string hitMode)
        {
            hit = null;
            hitDist = float.PositiveInfinity;
            hitMode = "screenRect";

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (buildings == null || buildings.Length == 0 || cameras == null)
                return false;

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

            for (int c = 0; c < cameras.Length; c++)
            {
                Camera cam = cameras[c];
                if (cam == null || !cam.isActiveAndEnabled) continue;

                for (int i = 0; i < buildings.Length; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                    if (b == null || !b.isActiveAndEnabled || b.NotSelectable) continue;

                    Rect rect;
                    float dist;
                    if (!b.TryPickScreenPointLikeOriginal(cam, mousePosition, out rect, out dist))
                        continue;

                    hit = b;
                    hitDist = dist;
                    hitMode = "camera='" + cam.name + "' rect=(" +
                              rect.xMin.ToString("0", CultureInfo.InvariantCulture) + "," +
                              rect.yMin.ToString("0", CultureInfo.InvariantCulture) + "," +
                              rect.xMax.ToString("0", CultureInfo.InvariantCulture) + "," +
                              rect.yMax.ToString("0", CultureInfo.InvariantCulture) + ")";
                    return true;
                }
            }

            return false;
        }

        private bool HasSelectedOrderUnitsLikeOriginal()
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u != null && u.isActiveAndEnabled && u.CanReceiveOrdersLikeOriginal())
                    return true;
            }
            return false;
        }

        private bool CanSelectedTakeResourcesLikeOriginal()
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (IsSelectedPeasantLikeOriginal(u))
                    return true;
            }
            return false;
        }

        private bool CanSelectedRepairLikeOriginal()
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (IsSelectedPeasantLikeOriginal(u))
                    return true;
            }
            return false;
        }

        private bool CanSelectedEnterLikeOriginal()
        {
            return HasSelectedOrderUnitsLikeOriginal();
        }

        private bool CanSelectedAttackLikeOriginal()
        {
            // Until real NewMonster KillMask/AttBuild/Capture flags are parsed, any controllable selected unit may show AttackPtr.
            return HasSelectedOrderUnitsLikeOriginal();
        }

        private static bool IsSelectedPeasantLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            if (u == null || !u.isActiveAndEnabled || !u.CanReceiveOrdersLikeOriginal())
                return false;

            string id = ((u.SourceMonsterId ?? string.Empty) + " " + (u.ResolvedMd ?? string.Empty)).ToLowerInvariant();
            // Current real peasant in logs: SourceMonsterId='UnitKri(AU)', ResolvedMd='EngKri'.
            // Keep this strict so line infantry/artillery won't get wood/stone/food cursors later.
            return id.IndexOf("kri", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("peasant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("worker", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Vec3(Vector3 v)
        {
            return "(" + v.x.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                   v.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                   v.z.ToString("0.00", CultureInfo.InvariantCulture) + ")";
        }
    }

    internal sealed class C2OriginalHardCursorFrameV5
    {
        public Sprite Sprite;
        public Texture2D Texture;
        public int Width;
        public int Height;
        public int HotspotX;
        public int HotspotY;
        public string SourcePath;
    }

    internal static class C2OriginalHardCursorProviderV5
    {
        public const string Contract = "V5F_PARSE_S_CURSOR_C2M_HARD_CUR_SELECTED_GROUND_HIT";

        private static readonly Dictionary<int, C2OriginalHardCursorFrameV5> Cache = new Dictionary<int, C2OriginalHardCursorFrameV5>();
        private static readonly Dictionary<int, string> CurPtrToResource = new Dictionary<int, string>
        {
            { 0,  "Cursors/Hard/main" },
            { 1,  "Cursors/Hard/attack" },
            { 2,  "Cursors/Hard/into_house" },
            { 3,  "Cursors/Hard/mend" },
            { 4,  "Cursors/Hard/mining" },
            { 5,  "Cursors/Hard/stoun" },
            { 6,  "Cursors/Hard/wood" },
            { 7,  "Cursors/Hard/food" },
            { 8,  "Cursors/Game/rally" },
            { 9,  "Cursors/Hard/graundAt" },
            { 10, "Cursors/Game/guard" },
            { 11, "Cursors/Hard/into_house" },
            { 15, "Cursors/null" }
        };

        public static string PrewarmOriginalHardCursors()
        {
            int ok = 0;
            string first = string.Empty;
            int[] ptrs = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 9, 15 };
            for (int i = 0; i < ptrs.Length; i++)
            {
                C2OriginalHardCursorFrameV5 f = LoadCursor(ptrs[i], out string audit);
                if (f != null && f.Sprite != null) ok++;
                if (i == 0) first = audit;
            }
            return "prewarmOk=" + ok.ToString(CultureInfo.InvariantCulture) + "/" + ptrs.Length.ToString(CultureInfo.InvariantCulture) + " first={" + first + "}";
        }

        public static C2OriginalHardCursorFrameV5 LoadCursor(int curptr, out string audit)
        {
            if (Cache.TryGetValue(curptr, out C2OriginalHardCursorFrameV5 cached))
            {
                audit = "cached curptr=" + curptr.ToString(CultureInfo.InvariantCulture) + " src='" + (cached != null ? cached.SourcePath : "null") + "'";
                return cached;
            }

            string resourcePath = ResourcePathForCurPtr(curptr);
            byte[] bytes = TryReadCursorBytes(resourcePath, out string sourcePath);
            if ((bytes == null || bytes.Length == 0) && curptr != 0)
            {
                // Original-safe fallback: if special cursor is absent, keep main cursor visible instead of hiding it.
                bytes = TryReadCursorBytes("Cursors/Hard/main", out sourcePath);
            }

            if (bytes == null || bytes.Length == 0)
            {
                audit = "missing bytes curptr=" + curptr.ToString(CultureInfo.InvariantCulture) + " res='" + resourcePath + "'";
                Cache[curptr] = null;
                return null;
            }

            C2OriginalHardCursorFrameV5 frame = DecodeCur(bytes, sourcePath, out string decodeAudit);
            Cache[curptr] = frame;
            audit = "curptr=" + curptr.ToString(CultureInfo.InvariantCulture) + " res='" + resourcePath + "' source='" + sourcePath + "' " + decodeAudit;
            return frame;
        }

        private static string ResourcePathForCurPtr(int curptr)
        {
            if (CurPtrToResource.TryGetValue(curptr, out string path))
                return path;
            return "Cursors/Hard/main";
        }

        private static byte[] TryReadCursorBytes(string resourcePath, out string sourcePath)
        {
            sourcePath = string.Empty;

            TextAsset ta = Resources.Load<TextAsset>(resourcePath);
            if (ta != null && ta.bytes != null && ta.bytes.Length > 0)
            {
                sourcePath = "Resources.Load<TextAsset>('" + resourcePath + "')";
                return ta.bytes;
            }

            string rel = resourcePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string[] candidates = new[]
            {
                Path.Combine(Application.dataPath, "Resources", rel + ".cur"),
                Path.Combine(Application.dataPath, "Resources", rel + ".bytes"),
                Path.Combine(Application.dataPath, rel + ".cur")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string p = candidates[i];
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    sourcePath = p;
                    return File.ReadAllBytes(p);
                }
            }

            return null;
        }

        private static C2OriginalHardCursorFrameV5 DecodeCur(byte[] data, string sourcePath, out string audit)
        {
            audit = string.Empty;
            try
            {
                if (data == null || data.Length < 22)
                {
                    audit = "decodeFailed tooSmall";
                    return null;
                }

                ushort reserved = U16(data, 0);
                ushort type = U16(data, 2);
                ushort count = U16(data, 4);
                if (reserved != 0 || type != 2 || count == 0)
                {
                    audit = "decodeFailed badIconDir reserved=" + reserved + " type=" + type + " count=" + count;
                    return null;
                }

                int bestEntry = 6;
                int bestPixels = -1;
                for (int i = 0; i < count; i++)
                {
                    int e = 6 + i * 16;
                    if (e + 16 > data.Length) break;
                    int ew = data[e] == 0 ? 256 : data[e];
                    int eh = data[e + 1] == 0 ? 256 : data[e + 1];
                    int entryPixels = ew * eh;
                    if (entryPixels > bestPixels)
                    {
                        bestPixels = entryPixels;
                        bestEntry = e;
                    }
                }

                int entryW = data[bestEntry] == 0 ? 256 : data[bestEntry];
                int entryH = data[bestEntry + 1] == 0 ? 256 : data[bestEntry + 1];
                int hotX = U16(data, bestEntry + 4);
                int hotY = U16(data, bestEntry + 6);
                int bytesInRes = I32(data, bestEntry + 8);
                int imageOffset = I32(data, bestEntry + 12);

                if (imageOffset < 0 || imageOffset >= data.Length)
                {
                    audit = "decodeFailed badImageOffset=" + imageOffset;
                    return null;
                }

                // PNG cursor entry.
                if (imageOffset + 8 < data.Length && data[imageOffset] == 0x89 && data[imageOffset + 1] == 0x50 && data[imageOffset + 2] == 0x4E && data[imageOffset + 3] == 0x47)
                {
                    byte[] png = new byte[Mathf.Min(bytesInRes, data.Length - imageOffset)];
                    Buffer.BlockCopy(data, imageOffset, png, 0, png.Length);
                    Texture2D pngTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!pngTex.LoadImage(png, false))
                    {
                        audit = "decodeFailed pngLoadImage";
                        return null;
                    }
                    pngTex.filterMode = FilterMode.Point;
                    pngTex.wrapMode = TextureWrapMode.Clamp;
                    Sprite pngSp = Sprite.Create(pngTex, new Rect(0, 0, pngTex.width, pngTex.height), new Vector2(0, 1), 100.0f);
                    audit = "png " + pngTex.width + "x" + pngTex.height + " hot=" + hotX + "," + hotY;
                    return new C2OriginalHardCursorFrameV5 { Texture = pngTex, Sprite = pngSp, Width = pngTex.width, Height = pngTex.height, HotspotX = hotX, HotspotY = hotY, SourcePath = sourcePath };
                }

                int headerSize = I32(data, imageOffset);
                if (headerSize < 40 || imageOffset + headerSize > data.Length)
                {
                    audit = "decodeFailed badDibHeader=" + headerSize;
                    return null;
                }

                int dibW = I32(data, imageOffset + 4);
                int dibHRaw = I32(data, imageOffset + 8);
                ushort planes = U16(data, imageOffset + 12);
                ushort bpp = U16(data, imageOffset + 14);
                int compression = I32(data, imageOffset + 16);
                int colorsUsed = I32(data, imageOffset + 32);
                if (planes != 1 || compression != 0 || dibW <= 0 || dibHRaw == 0)
                {
                    audit = "decodeFailed unsupportedDib planes=" + planes + " compression=" + compression + " w=" + dibW + " h=" + dibHRaw;
                    return null;
                }

                bool bottomUp = dibHRaw > 0;
                int dibHAbs = Mathf.Abs(dibHRaw);
                int imgH = Math.Max(1, dibHAbs / 2);
                int imgW = dibW;
                if (entryW > 0) imgW = entryW;
                if (entryH > 0) imgH = entryH;

                int paletteEntries = 0;
                if (bpp <= 8)
                    paletteEntries = colorsUsed > 0 ? colorsUsed : (1 << bpp);

                int paletteOffset = imageOffset + headerSize;
                int xorOffset = paletteOffset + paletteEntries * 4;
                int xorStride = ((imgW * bpp + 31) / 32) * 4;
                int xorBytes = xorStride * imgH;
                int andOffset = xorOffset + xorBytes;
                int andStride = ((imgW + 31) / 32) * 4;

                if (xorOffset < 0 || xorOffset + xorBytes > data.Length)
                {
                    audit = "decodeFailed badXorData w=" + imgW + " h=" + imgH + " bpp=" + bpp + " xorOffset=" + xorOffset + " xorBytes=" + xorBytes;
                    return null;
                }

                Color32[] pixels = new Color32[imgW * imgH];
                for (int y = 0; y < imgH; y++)
                {
                    int srcY = bottomUp ? y : (imgH - 1 - y);
                    int row = xorOffset + srcY * xorStride;
                    for (int x = 0; x < imgW; x++)
                    {
                        byte r = 0, g = 0, b = 0, a = 255;
                        if (bpp == 32)
                        {
                            int p = row + x * 4;
                            b = data[p + 0];
                            g = data[p + 1];
                            r = data[p + 2];
                            a = data[p + 3];
                        }
                        else if (bpp == 24)
                        {
                            int p = row + x * 3;
                            b = data[p + 0];
                            g = data[p + 1];
                            r = data[p + 2];
                            a = 255;
                        }
                        else if (bpp == 8)
                        {
                            int idx = data[row + x];
                            int pal = paletteOffset + idx * 4;
                            if (pal + 3 < data.Length)
                            {
                                b = data[pal + 0]; g = data[pal + 1]; r = data[pal + 2]; a = 255;
                            }
                        }
                        else
                        {
                            audit = "decodeFailed unsupportedBpp=" + bpp;
                            return null;
                        }

                        if (andOffset + andStride * imgH <= data.Length)
                        {
                            int maskRow = andOffset + srcY * andStride;
                            int maskByte = data[maskRow + (x >> 3)];
                            bool transparent = (maskByte & (0x80 >> (x & 7))) != 0;
                            if (transparent) a = 0;
                        }

                        pixels[y * imgW + x] = new Color32(r, g, b, a);
                    }
                }

                Texture2D tex = new Texture2D(imgW, imgH, TextureFormat.RGBA32, false);
                tex.name = "C2OriginalHardCursor_" + Path.GetFileNameWithoutExtension(sourcePath);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SetPixels32(pixels);
                tex.Apply(false, false);

                Sprite sp = Sprite.Create(tex, new Rect(0, 0, imgW, imgH), new Vector2(0, 1), 100.0f);
                audit = "dib " + imgW + "x" + imgH + " bpp=" + bpp + " hot=" + hotX + "," + hotY + " bytes=" + data.Length;
                return new C2OriginalHardCursorFrameV5
                {
                    Texture = tex,
                    Sprite = sp,
                    Width = imgW,
                    Height = imgH,
                    HotspotX = hotX,
                    HotspotY = hotY,
                    SourcePath = sourcePath
                };
            }
            catch (Exception ex)
            {
                audit = "decodeException " + ex.GetType().Name + ": " + ex.Message;
                return null;
            }
        }

        private static ushort U16(byte[] d, int o)
        {
            return (ushort)(d[o] | (d[o + 1] << 8));
        }

        private static int I32(byte[] d, int o)
        {
            unchecked
            {
                return d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
            }
        }
    }
}
