// C2BuildingProductionCardsRuntimeV114.cs
// V114: building produce-card runtime bridge.
// Original chain copied conceptually:
//   va_Unit_P_Box::LeftClick -> CmdProduceObj -> ProduceObject -> OneObject::Produce -> ProduceObjLink
// This file intentionally keeps unit HUD/build-preview logic untouched. It only handles clicks while a building is selected.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal struct C2BuildingProduceCardStateV114LikeOriginal
    {
        public int Count;
        public float Progress01;
        public bool Infinite;
        public int MaxStage;
    }

    internal sealed class C2BuildingProduceProgressBarUiV125LikeOriginal : MonoBehaviour
    {
        private C2SettlementBuildingSelectableV1LikeOriginal _building;
        private string _unitId = string.Empty;
        private int _x;
        private int _topY;
        private int _w;
        private int _fullH;
        private RectTransform _rt;
        private Image _img;
        private RectTransform _rt2;
        private Image _img2;

        public void InitLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string unitId, int x, int topY, int w, int fullH)
        {
            InitLikeOriginal(building, unitId, x, topY, w, fullH, null);
        }

        public void InitLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string unitId, int x, int topY, int w, int fullH, Image secondPass)
        {
            _building = building;
            _unitId = unitId ?? string.Empty;
            _x = x;
            _topY = topY;
            _w = Mathf.Max(1, w);
            _fullH = Mathf.Max(1, fullH);
            _rt = GetComponent<RectTransform>();
            _img = GetComponent<Image>();
            _img2 = secondPass;
            _rt2 = secondPass != null ? secondPass.GetComponent<RectTransform>() : null;
            Update();
        }

        private void Update()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_img == null) _img = GetComponent<Image>();
            if (_rt == null) return;

            C2BuildingProduceCardStateV114LikeOriginal st =
                C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(_building, _unitId);

            bool visible = st.Count > 0 || st.Infinite;
            SetImageVisibleV144LikeOriginal(_img, visible);
            SetImageVisibleV144LikeOriginal(_img2, visible);

            float p = Mathf.Clamp01(st.Progress01);
            int h = visible && p > 0.0f ? Mathf.Clamp(Mathf.RoundToInt(_fullH * p), 1, _fullH) : 1;
            ApplyRectLikeOriginal(_rt, _x, _topY, _w, _fullH, h);
            ApplyRectLikeOriginal(_rt2, _x, _topY, _w, _fullH, h);
        }

        private static void ApplyRectLikeOriginal(RectTransform rt, int x, int topY, int w, int fullH, int h)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -(topY + (fullH - h)));
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetImageVisibleV144LikeOriginal(Image img, bool visible)
        {
            if (img == null) return;
            if (img.gameObject.activeSelf != visible)
                img.gameObject.SetActive(visible);
            if (img.enabled != visible)
                img.enabled = visible;
        }
    }

    internal sealed class C2BuildingProduceAmountPlateUiV133LikeOriginal : MonoBehaviour
    {
        private C2SettlementBuildingSelectableV1LikeOriginal _building;
        private string _unitId = string.Empty;
        private Image _plate;
        private Image _plate2;
        private Text _amount;

        public void InitLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string unitId, Image plate, Text amount)
        {
            InitLikeOriginal(building, unitId, plate, null, amount);
        }

        public void InitLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string unitId, Image plate, Image secondPassPlate, Text amount)
        {
            _building = building;
            _unitId = unitId ?? string.Empty;
            _plate = plate;
            _plate2 = secondPassPlate;
            _amount = amount;
            Update();
        }

        private void Update()
        {
            C2BuildingProduceCardStateV114LikeOriginal st =
                C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(_building, _unitId);

            bool plateVisible = st.Count > 0 || st.Infinite;

            // V144: after Shift+LMB finite production reaches 0 the old double-pass plate could remain
            // because only Image.enabled was changed. Disable the whole GameObject set, so no stale
            // green amount plate can survive after the order is removed. New orders rebuild the HUD and
            // create fresh overlay objects.
            SetGraphicVisibleV144LikeOriginal(_plate, plateVisible);
            SetGraphicVisibleV144LikeOriginal(_plate2, plateVisible);

            bool textVisible = st.Count > 0 && !st.Infinite;
            if (_amount != null)
            {
                if (_amount.gameObject.activeSelf != textVisible)
                    _amount.gameObject.SetActive(textVisible);
                if (_amount.enabled != textVisible)
                    _amount.enabled = textVisible;
                string txt = st.Count.ToString(CultureInfo.InvariantCulture);
                if (_amount.text != txt)
                    _amount.text = txt;
            }
        }

        private static void SetGraphicVisibleV144LikeOriginal(Graphic g, bool visible)
        {
            if (g == null) return;
            if (g.gameObject.activeSelf != visible)
                g.gameObject.SetActive(visible);
            if (g.enabled != visible)
                g.enabled = visible;
        }
    }

    internal sealed class C2BuildingProductionCardsRuntimeV114 : MonoBehaviour
    {
        private const string Contract = "V144_ROUND_ROBIN_INFINITE_AND_CLEAR_PLATES";
        private const float OriginalStageTicksPerSecondLikeOriginal = 18.0f;
        private static readonly bool C2BuildingProductionAutoPrewarmMenuUnitsV135LikeOriginal = false;
        private static float s_suppressMapSelectionUntilV126LikeOriginal;
        private static readonly Dictionary<string, int> s_produceStagesCacheV134LikeOriginal =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<C2BuildingProductionOrderV114> _queue = new List<C2BuildingProductionOrderV114>(16);
        private readonly HashSet<string> _prewarmSeenV127LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<C2BuildingProductionPrewarmRequestV128> _prewarmQueueV127LikeOriginal = new Queue<C2BuildingProductionPrewarmRequestV128>();
        private bool _prewarmRunningV127LikeOriginal;

        private sealed class C2BuildingProductionPrewarmRequestV128
        {
            public C2OriginalProduceItemV13 Item;
            public byte RealDir;
            public bool WarmExitPath;
        }
        private C2SettlementBuildingSelectableV1LikeOriginal _building;
        private C2BattleTerrainMode _mode;
        private float _nextLog;

        private sealed class C2BuildingProductionOrderV114
        {
            public string UnitId = string.Empty;
            public string MdName = string.Empty;
            public int Nation;
            public int Count;
            public int MaxStage = 100;
            public float Stage;
            public bool Infinite;
            public string Display = string.Empty;
        }

        private void Awake()
        {
            _building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (_building != null) _mode = _building.OwnerMode;
            if (_mode == null) _mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
        }

        private void SchedulePrewarmVisibleItemsV127LikeOriginal(List<C2OriginalProduceItemV13> items)
        {
            if (items == null || items.Count == 0)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                C2OriginalProduceItemV13 item = items[i];
                if (item == null || item.Building)
                    continue;

                byte[] dirs = { 0, 19 };
                SchedulePrewarmItemDirsV136LikeOriginal(item, dirs, false);
            }

            if (!_prewarmRunningV127LikeOriginal && _prewarmQueueV127LikeOriginal.Count > 0)
                StartCoroutine(PrewarmVisibleItemsCoroutineV127LikeOriginal(true));
        }

        private void SchedulePrewarmQueuedItemV136LikeOriginal(C2OriginalProduceItemV13 item)
        {
            // V137: original va_Unit_P_Box::LeftClick -> CmdProduceObj only enqueues a produce command.
            // It does not build exit paths, does not search CONCENTRATOR, and does not warm G16/G17 banks on UI click.
            // Keep the click path light; unit visuals/path are resolved when production actually finishes.
            return;
        }

        private void SchedulePrewarmItemDirsV136LikeOriginal(C2OriginalProduceItemV13 item, byte[] dirs, bool warmExitPath)
        {
            if (item == null || item.Building || dirs == null || dirs.Length == 0)
                return;

            for (int d = 0; d < dirs.Length; d++)
            {
                string key = (item.UnitId ?? string.Empty) + "|" + (item.MdName ?? string.Empty) + "|" +
                             item.Nation.ToString(CultureInfo.InvariantCulture) + "|dir=" + dirs[d].ToString(CultureInfo.InvariantCulture);
                if (_prewarmSeenV127LikeOriginal.Contains(key))
                    continue;

                _prewarmSeenV127LikeOriginal.Add(key);
                _prewarmQueueV127LikeOriginal.Enqueue(new C2BuildingProductionPrewarmRequestV128
                {
                    Item = item,
                    RealDir = dirs[d],
                    WarmExitPath = warmExitPath
                });
            }
        }

        private System.Collections.IEnumerator PrewarmVisibleItemsCoroutineV127LikeOriginal(bool delayMenuFrames)
        {
            _prewarmRunningV127LikeOriginal = true;

            if (delayMenuFrames)
            {
                // V133: let the finished-building HUD appear first; warm banks after the menu has rendered.
                yield return null;
                yield return null;
            }
            else
            {
                yield return null;
            }

            while (_prewarmQueueV127LikeOriginal.Count > 0)
            {
                if (_mode == null)
                {
                    if (_building == null) _building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
                    if (_building != null && _building.OwnerMode != null) _mode = _building.OwnerMode;
                    if (_mode == null) _mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                }

                C2BuildingProductionPrewarmRequestV128 req = _prewarmQueueV127LikeOriginal.Dequeue();
                C2OriginalProduceItemV13 item = req != null ? req.Item : null;
                if (_mode != null && item != null && !item.Building)
                {
                    if (req.WarmExitPath && _building != null)
                    {
                        string exitAudit;
                        _mode.C2BuildingProductionPrewarmExitPathForBuildingV136LikeOriginal(
                            _building,
                            item.UnitId,
                            item.MdName,
                            out exitAudit);
                    }

                    string audit;
                    _mode.C2BuildingProductionPrewarmUnitVisualsV128LikeOriginal(
                        item.UnitId,
                        item.MdName,
                        item.Nation,
                        req.RealDir,
                        out audit);
                }

                // One produced-unit visual bank/direction per frame. This avoids moving the spawn hitch into the menu rebuild.
                yield return null;
            }

            _prewarmRunningV127LikeOriginal = false;
        }

        public static void SchedulePrewarmForBuildingMenuV127LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, List<C2OriginalProduceItemV13> items)
        {
            // V135: do not warm every produced unit as soon as a building becomes ready.
            // Barracks-like menus can expose many unit types; prewarming all visual banks here causes
            // the long freeze exactly on construction completion. Keep the menu responsive and let
            // produced-unit caches warm on explicit production paths instead.
            if (!C2BuildingProductionAutoPrewarmMenuUnitsV135LikeOriginal)
                return;

            if (building == null || items == null || items.Count == 0)
                return;

            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null) rt = building.gameObject.AddComponent<C2BuildingProductionCardsRuntimeV114>();
            rt.SchedulePrewarmVisibleItemsV127LikeOriginal(items);
        }

        private void Update()
        {
            if (_queue.Count == 0)
                return;

            if (_building == null)
            {
                _building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
                if (_building == null) return;
            }
            if (_mode == null) _mode = _building.OwnerMode != null ? _building.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (_mode == null) return;

            C2BuildingProductionOrderV114 order = _queue[0];
            order.Stage += Time.deltaTime * OriginalStageTicksPerSecondLikeOriginal;

            if (order.Stage < order.MaxStage)
                return;

            order.Stage = 0.0f;
            string spawnAudit;
            C2NeutralPeasantUnitInfoV2LikeOriginal spawned;
            bool ok = _mode.C2BuildingProductionSpawnUnitFromBuildingV114LikeOriginal(
                _building,
                order.UnitId,
                order.MdName,
                order.Nation,
                out spawned,
                out spawnAudit);

            if (!ok)
                return;

            if (order.Infinite)
            {
                // V144: multiple infinite produce cards must cycle like original command queue:
                // A+B => A, B, A, B; A+B+C => A, B, C, A, B, C.
                // A single infinite order is kept in place.
                if (_queue.Count > 1)
                {
                    _queue.RemoveAt(0);
                    order.Stage = 0.0f;
                    _queue.Add(order);
                }
            }
            else
            {
                order.Count--;

                if (order.Count <= 0)
                {
                    // V144: force the order out immediately; amount/progress UI components now
                    // disable their whole GameObject set on the very next Update, so the green
                    // finite plate cannot remain after the 10th Shift-produced unit.
                    _queue.RemoveAt(0);
                }
            }
        }

        public void EnqueueLikeOriginal(C2OriginalProduceItemV13 item)
        {
            EnqueueLikeOriginal(item, ResolveClickProduceAmountV142LikeOriginal(), ResolveClickProduceInfiniteV142LikeOriginal());
        }

        public void EnqueueLikeOriginal(C2OriginalProduceItemV13 item, int amount, bool infinite)
        {
            if (item == null) return;

            amount = Mathf.Max(1, amount);

            C2BuildingProductionOrderV114 existing = FindOrderLikeOriginal(item.UnitId);
            if (existing != null)
            {
                if (infinite)
                {
                    existing.Infinite = true;
                    existing.Count = 0;
                }
                else
                {
                    existing.Infinite = false;
                    existing.Count = Mathf.Max(0, existing.Count) + amount;
                }

                SchedulePrewarmQueuedItemV136LikeOriginal(item);
                return;
            }

            var order = new C2BuildingProductionOrderV114();
            order.UnitId = item.UnitId ?? string.Empty;
            order.MdName = item.MdName ?? string.Empty;
            order.Nation = item.Nation;
            order.Count = infinite ? 0 : amount;
            order.Infinite = infinite;
            order.MaxStage = Mathf.Max(1, C2BuildingProductionResolveUnitProduceStagesLikeOriginal(order.MdName, order.UnitId));
            order.Display = item.DisplayText ?? string.Empty;
            _queue.Add(order);
            SchedulePrewarmQueuedItemV136LikeOriginal(item);
        }

        private static bool ResolveClickProduceInfiniteV142LikeOriginal()
        {
            bool ctrl = IsCtrlPressedV142ALikeOriginal();
            bool shift = IsShiftPressedV142ALikeOriginal();
            return !ctrl && !shift;
        }

        private static int ResolveClickProduceAmountV142LikeOriginal()
        {
            // V142A: project uses the new Input System, so UnityEngine.Input.GetKey throws.
            // Original requested behavior:
            //   click        -> infinite production
            //   Ctrl + click -> one unit
            //   Shift+ click -> ten units
            if (IsCtrlPressedV142ALikeOriginal()) return 1;
            if (IsShiftPressedV142ALikeOriginal()) return 10;
            return 1;
        }

        private static bool IsCtrlPressedV142ALikeOriginal()
        {
            return IsModifierPressedV142ALikeOriginal(
#if ENABLE_INPUT_SYSTEM
                true,
#else
                false,
#endif
                KeyCode.LeftControl, KeyCode.RightControl);
        }

        private static bool IsShiftPressedV142ALikeOriginal()
        {
            return IsModifierPressedV142ALikeOriginal(
#if ENABLE_INPUT_SYSTEM
                false,
#else
                false,
#endif
                KeyCode.LeftShift, KeyCode.RightShift);
        }

        private static bool IsModifierPressedV142ALikeOriginal(bool ctrlInInputSystem, KeyCode legacyLeft, KeyCode legacyRight)
        {
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (ctrlInInputSystem)
                {
                    if ((kb.leftCtrlKey != null && kb.leftCtrlKey.isPressed) ||
                        (kb.rightCtrlKey != null && kb.rightCtrlKey.isPressed))
                        return true;
                }
                else
                {
                    if ((kb.leftShiftKey != null && kb.leftShiftKey.isPressed) ||
                        (kb.rightShiftKey != null && kb.rightShiftKey.isPressed))
                        return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKey(legacyLeft) || Input.GetKey(legacyRight))
                    return true;
            }
            catch (InvalidOperationException)
            {
                // Active Input Handling = Input System Package. Ignore legacy API.
            }
#endif
            return false;
        }

        public bool CancelAllLikeOriginal(string unitId)
        {
            C2BuildingProductionOrderV114 order = FindOrderLikeOriginal(unitId);
            if (order == null) return false;

            // V143: original-style RMB on a produce card cancels the queued/training item,
            // including infinite production. Removing the order makes the green progress animation
            // and amount plate disappear through GetCardStateLikeOriginal().
            _queue.Remove(order);
            return true;
        }

        public void CancelOneLikeOriginal(string unitId)
        {
            // Kept only for compatibility with older patches; V143 uses full cancel on RMB.
            CancelAllLikeOriginal(unitId);
        }

        private C2BuildingProductionOrderV114 FindOrderLikeOriginal(string unitId)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                C2BuildingProductionOrderV114 q = _queue[i];
                if (q != null && string.Equals(q.UnitId, unitId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    return q;
            }
            return null;
        }

        public C2BuildingProduceCardStateV114LikeOriginal GetCardStateLikeOriginal(string unitId)
        {
            C2BuildingProduceCardStateV114LikeOriginal st = new C2BuildingProduceCardStateV114LikeOriginal();
            C2BuildingProductionOrderV114 order = FindOrderLikeOriginal(unitId);
            if (order == null) return st;
            st.Count = Mathf.Max(0, order.Count);
            st.MaxStage = Mathf.Max(1, order.MaxStage);
            st.Progress01 = Mathf.Clamp01(order.Stage / Mathf.Max(1.0f, order.MaxStage));
            st.Infinite = order.Infinite;
            return st;
        }

        public static bool TryHandleBuildingProduceClickLikeOriginal(C2OriginalProduceItemV13 item)
        {
            if (item == null || item.Building)
                return false;

            C2SettlementBuildingSelectableV1LikeOriginal building = FirstSelectedReadyBuildingLikeOriginal();
            if (building == null)
                return false;

            SuppressMapSelectionFromHudClickV126LikeOriginal();

            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null) rt = building.gameObject.AddComponent<C2BuildingProductionCardsRuntimeV114>();
            rt.EnqueueLikeOriginal(item);

            // Keep the selected building exactly like original production UI.
            // HUD card clicks are commands, not map-selection clicks.
            PreserveSelectedBuildingOnlyV126LikeOriginal(building);
            return true;
        }

        public static bool TryHandleBuildingProduceCancelClickLikeOriginal(C2OriginalProduceItemV13 item)
        {
            if (item == null || item.Building)
                return false;

            C2SettlementBuildingSelectableV1LikeOriginal building = FirstSelectedReadyBuildingLikeOriginal();
            if (building == null)
                return false;

            SuppressMapSelectionFromHudClickV126LikeOriginal();

            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null)
                return false;

            bool cancelled = rt.CancelAllLikeOriginal(item.UnitId ?? string.Empty);

            // RMB on the card is also a HUD command, not a map-selection click.
            PreserveSelectedBuildingOnlyV126LikeOriginal(building);
            return cancelled;
        }

        public static void SuppressMapSelectionFromHudClickV126LikeOriginal()
        {
            s_suppressMapSelectionUntilV126LikeOriginal = Mathf.Max(
                s_suppressMapSelectionUntilV126LikeOriginal,
                Time.realtimeSinceStartup + 0.12f);
        }

        public static bool ShouldSuppressMapSelectionFromHudClickV126LikeOriginal()
        {
            return Time.realtimeSinceStartup < s_suppressMapSelectionUntilV126LikeOriginal;
        }

        public static void PreserveSelectedBuildingOnlyV126LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal keep)
        {
            if (keep == null) return;

            // V128: do not scan every unit/building from a HUD button click.
            // The building is already the selected object when its production cards are visible.
            // The map picker is suppressed separately; just keep this building selected.
            if (!keep.IsSelected)
                keep.SetSelected(true);
        }

        public static C2BuildingProduceCardStateV114LikeOriginal GetCardStateLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string unitId)
        {
            if (building == null) return new C2BuildingProduceCardStateV114LikeOriginal();
            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null) return new C2BuildingProduceCardStateV114LikeOriginal();
            return rt.GetCardStateLikeOriginal(unitId);
        }

        public static string BuildQueueStateKeyLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null) return "queue=<null>";
            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null || rt._queue.Count == 0) return "queue=0";
            return rt.BuildQueueStateKeyInstanceLikeOriginal();
        }

        private string BuildQueueStateKeyInstanceLikeOriginal()
        {
            if (_queue.Count == 0) return "queue=0";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            sb.Append("queue=").Append(_queue.Count.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < _queue.Count; i++)
            {
                C2BuildingProductionOrderV114 q = _queue[i];
                if (q == null) continue;
                // V125: do NOT include progress in the HUD state key.
                // V124 rebuilt the whole HUD every progress bucket; that recreated all cards and looked like flicker/FPS drops.
                // The green line is now updated in-place by C2BuildingProduceProgressBarUiV125LikeOriginal.
                sb.Append("|").Append(q.UnitId ?? string.Empty)
                  .Append(":").Append(q.Count.ToString(CultureInfo.InvariantCulture))
                  .Append(":").Append(q.Infinite ? "inf" : "finite");
            }
            return sb.ToString();
        }

        private static C2SettlementBuildingSelectableV1LikeOriginal FirstSelectedReadyBuildingLikeOriginal()
        {
            C2SettlementBuildingSelectableV1LikeOriginal cached = C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal;
            if (cached != null && cached.isActiveAndEnabled && !cached.NotSelectable && cached.IsSelected)
                return cached;

            // Fallback for old scenes only. Normal HUD clicks use the cached selected building above.
            C2SettlementBuildingSelectableV1LikeOriginal[] all = UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null || !b.isActiveAndEnabled || b.NotSelectable || !b.IsSelected) continue;
                return b;
            }
            return null;
        }

        private static int C2BuildingProductionResolveUnitProduceStagesLikeOriginal(string mdName, string unitId)
        {
            string cacheKey = ((mdName ?? string.Empty).Trim() + "|" + (unitId ?? string.Empty).Trim()).ToUpperInvariant();
            int cachedStages;
            if (s_produceStagesCacheV134LikeOriginal.TryGetValue(cacheKey, out cachedStages))
                return cachedStages;

            string[] names = BuildNameCandidatesLikeOriginal(mdName, unitId);
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int r = 0; r < roots.Length; r++)
            {
                if (string.IsNullOrWhiteSpace(roots[r])) continue;
                for (int n = 0; n < names.Length; n++)
                {
                    string name = names[n];
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string[] paths =
                    {
                        System.IO.Path.Combine(roots[r], "UnitsMD", name + ".md"),
                        System.IO.Path.Combine(roots[r], "UnitsMD", name + ".MD"),
                        System.IO.Path.Combine(roots[r], "UnitsMD", "Units", name + ".md"),
                        System.IO.Path.Combine(roots[r], "UnitsMD", "Units", name + ".MD"),
                        System.IO.Path.Combine(roots[r], name + ".md"),
                        System.IO.Path.Combine(roots[r], name + ".MD")
                    };
                    for (int p = 0; p < paths.Length; p++)
                    {
                        int stages;
                        if (TryReadBuildStagesLikeOriginal(paths[p], out stages))
                        {
                            s_produceStagesCacheV134LikeOriginal[cacheKey] = stages;
                            return stages;
                        }
                    }
                }
            }
            s_produceStagesCacheV134LikeOriginal[cacheKey] = 100;
            return 100;
        }

        private static string[] BuildNameCandidatesLikeOriginal(string mdName, string unitId)
        {
            var list = new List<string>(4);
            AddName(list, mdName);
            AddName(list, StripNationSuffixLikeOriginal(mdName));
            AddName(list, unitId);
            AddName(list, StripNationSuffixLikeOriginal(unitId));
            return list.ToArray();
        }

        private static void AddName(List<string> list, string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            s = s.Trim();
            for (int i = 0; i < list.Count; i++)
                if (string.Equals(list[i], s, StringComparison.OrdinalIgnoreCase)) return;
            list.Add(s);
        }

        private static string StripNationSuffixLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            int a = s.LastIndexOf('(');
            int b = s.LastIndexOf(')');
            if (a > 0 && b > a) return s.Substring(0, a);
            return s;
        }

        private static bool TryReadBuildStagesLikeOriginal(string path, out int stages)
        {
            stages = 0;
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return false;
            string[] lines;
            try { lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.GetEncoding(1251)); }
            catch { try { lines = System.IO.File.ReadAllLines(path); } catch { return false; } }
            for (int i = 0; i < lines.Length; i++)
            {
                string line = C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(lines[i]);
                if (line.Length == 0) continue;
                string[] t = C2OriginalProduceCatalogV13.SplitTokensForSiblingLoadersLikeOriginal(line);
                if (t.Length >= 2 &&
                    (string.Equals(t[0], "PRODUCESTAGES", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t[0], "PRODUCESTAGE", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t[0], "PRODUCETIME", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t[0], "BUILDSTAGES", StringComparison.OrdinalIgnoreCase)) &&
                    int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out stages) && stages > 0)
                    return true;
            }
            return false;
        }
    }


    internal sealed class C2ProducedUnitExitPathDriverV124LikeOriginal : MonoBehaviour
    {
        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private Vector2[] _path;
        private int _index;
        private float _speed;
        private byte _finalDir;
        private bool _issued;
        private string _source = string.Empty;

        public static void AttachLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2[] path,
            float speedOriginalPixelsPerSecond,
            byte finalDir,
            string source)
        {
            if (unit == null || path == null || path.Length <= 1)
                return;

            C2ProducedUnitExitPathDriverV124LikeOriginal driver =
                unit.GetComponent<C2ProducedUnitExitPathDriverV124LikeOriginal>();
            if (driver == null)
                driver = unit.gameObject.AddComponent<C2ProducedUnitExitPathDriverV124LikeOriginal>();

            driver._unit = unit;
            driver._path = path;
            driver._index = 1;
            driver._speed = Mathf.Max(1.0f, speedOriginalPixelsPerSecond);
            driver._finalDir = finalDir;
            driver._issued = false;
            driver._source = source ?? string.Empty;
            driver.IssueCurrentWaypointLikeOriginal();
        }

        private void Update()
        {
            if (_unit == null || _path == null || _index >= _path.Length)
            {
                Destroy(this);
                return;
            }

            if (!_issued)
                IssueCurrentWaypointLikeOriginal();

            Vector2 target = _path[_index];
            float dx = _unit.RealXFloat - target.x;
            float dy = _unit.RealYFloat - target.y;
            if (dx * dx + dy * dy <= 96.0f * 96.0f)
            {
                _index++;
                _issued = false;
                if (_index >= _path.Length)
                {
                    Destroy(this);
                    return;
                }
                IssueCurrentWaypointLikeOriginal();
            }
        }

        private void IssueCurrentWaypointLikeOriginal()
        {
            if (_unit == null || _path == null || _index >= _path.Length)
                return;

            Vector2 target = _path[_index];
            bool final = _index == _path.Length - 1;
            _unit.SetMoveDestinationRealLikeOriginal(
                target.x,
                target.y,
                _speed,
                final,
                _finalDir);
            _issued = true;
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        private static int s_C2BuildingProductionRuntimeUnitIndexV114 = 9100000;
        private readonly Dictionary<string, C2BuildingProductionExitPathCacheV136> _c2BuildingProductionExitPathCacheV136LikeOriginal =
            new Dictionary<string, C2BuildingProductionExitPathCacheV136>(StringComparer.OrdinalIgnoreCase);

        private sealed class C2BuildingProductionExitPathCacheV136
        {
            public Vector2[] Path;
            public string Audit = string.Empty;
        }

        public bool C2BuildingProductionPrewarmUnitVisualsV127LikeOriginal(
            string unitId,
            string mdName,
            int nation,
            out string audit)
        {
            return C2BuildingProductionPrewarmUnitVisualsV128LikeOriginal(unitId, mdName, nation, 0, out audit);
        }

        public bool C2BuildingProductionPrewarmUnitVisualsV128LikeOriginal(
            string unitId,
            string mdName,
            int nation,
            byte realDir,
            out string audit)
        {
            audit = "not_started";

            string monsterId = !string.IsNullOrWhiteSpace(unitId) ? unitId.Trim() : (mdName ?? string.Empty).Trim();
            string mdLookup = !string.IsNullOrWhiteSpace(mdName) ? mdName.Trim() : monsterId;
            C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(mdLookup);
            if (md == null || !md.Found)
                md = C2Settlement3InuMdV2ResolveMdLikeOriginal(monsterId);

            if (md == null || !md.Found)
            {
                audit = "md_not_found unit='" + monsterId + "' md='" + mdLookup + "'";
                return false;
            }

            C2Settlement3InuMdV2Record r = new C2Settlement3InuMdV2Record();
            r.Index = -9100000 - Mathf.Abs(((monsterId ?? string.Empty) + "|dir=" + realDir.ToString(CultureInfo.InvariantCulture)).GetHashCode() % 100000);
            r.Nation = (byte)Mathf.Clamp(nation, 0, 255);
            r.NIndex = 0;
            r.RealX = 0;
            r.RealY = 0;
            r.RealDir = realDir;
            r.Life = 0;
            r.Stage = 0;
            r.MonsterId = monsterId;

            List<C2NeutralPeasantUnitFrameV2LikeOriginal> idleFrames;
            List<C2NeutralPeasantUnitFrameV2LikeOriginal> walkFrames;
            string framesAudit;
            string walkAudit;
            bool walkAnimFound;
            bool visualCacheHit;
            bool visualOk = C2NeutralPeasantUnitsV19TryGetOrBuildVisualFramesLikeOriginal(
                md,
                r,
                out idleFrames,
                out walkFrames,
                out framesAudit,
                out walkAudit,
                out walkAnimFound,
                out visualCacheHit);

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] walkDirectionBanks;
            string walkBankAudit;
            bool walkBankCacheHit;
            C2NeutralPeasantUnitsV19GetOrBuildWalkDirectionBanksLikeOriginal(md, r, out walkDirectionBanks, out walkBankAudit, out walkBankCacheHit);

            C2NeutralPeasantUnitMotionBanksV20LikeOriginal motionBanks;
            string motionBankAudit;
            bool motionBankCacheHit;
            C2NeutralPeasantUnitsV20GetOrBuildMotionBanksLikeOriginal(md, r, out motionBanks, out motionBankAudit, out motionBankCacheHit);

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] idleDirectionBanks;
            string idleBankAudit;
            bool idleBankCacheHit;
            C2NeutralPeasantUnitsV23GetOrBuildIdleDirectionBanksLikeOriginal(md, r, out idleDirectionBanks, out idleBankAudit, out idleBankCacheHit);

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] restDirectionBanks;
            string restBankAudit;
            bool restBankCacheHit;
            C2NeutralPeasantUnitsV30GetOrBuildRestDirectionBanksLikeOriginal(md, r, out restDirectionBanks, out restBankAudit, out restBankCacheHit);

            audit = "ok=" + visualOk +
                    " unit='" + monsterId + "'" +
                    " md='" + (md.MdName ?? string.Empty) + "'" +
                    " dir=" + realDir.ToString(CultureInfo.InvariantCulture) +
                    " visualCache=" + visualCacheHit +
                    " walkFound=" + walkAnimFound +
                    " walkBankCache=" + walkBankCacheHit +
                    " motionBankCache=" + motionBankCacheHit +
                    " idleBankCache=" + idleBankCacheHit +
                    " restBankCache=" + restBankCacheHit;
            return visualOk;
        }

        public bool C2BuildingProductionPrewarmExitPathForBuildingV136LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            string unitId,
            string mdName,
            out string audit)
        {
            audit = "not_started";
            if (building == null)
            {
                audit = "no_building";
                return false;
            }

            string mdAudit;
            C2Settlement3InuMdV2Info md = C2BuildingProductionResolveBuildingMdForExitPathV136LikeOriginal(
                building,
                null,
                out mdAudit);
            if (md == null || !md.Found)
            {
                audit = mdAudit + " md_not_found building='" + (building.SourceMonsterId ?? string.Empty) + "'";
                return false;
            }

            Vector2[] exitPath;
            bool ok = C2BuildingProductionTryGetOrBuildExitPathV136LikeOriginal(building, md, out exitPath, out audit);
            audit = mdAudit + " " + audit;
            return ok;
        }

        public bool C2BuildingProductionSpawnUnitFromBuildingV114LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            string unitId,
            string mdName,
            int nation,
            out C2NeutralPeasantUnitInfoV2LikeOriginal spawned,
            out string audit)
        {
            spawned = null;
            audit = "not_started";
            if (building == null)
            {
                audit = "no_building";
                return false;
            }

            string monsterId = !string.IsNullOrWhiteSpace(unitId) ? unitId.Trim() : (mdName ?? string.Empty).Trim();
            string mdLookup = !string.IsNullOrWhiteSpace(mdName) ? mdName.Trim() : monsterId;
            C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(mdLookup);
            if (md == null || !md.Found)
            {
                md = C2Settlement3InuMdV2ResolveMdLikeOriginal(monsterId);
            }
            if (md == null || !md.Found)
            {
                audit = "md_not_found unit='" + monsterId + "' md='" + mdLookup + "'";
                return false;
            }

            Vector2[] exitPath;
            string exitAudit;
            string exitMdAudit;
            C2Settlement3InuMdV2Info exitMd = C2BuildingProductionResolveBuildingMdForExitPathV136LikeOriginal(
                building,
                md,
                out exitMdAudit);
            if (!C2BuildingProductionTryGetOrBuildExitPathV136LikeOriginal(building, exitMd, out exitPath, out exitAudit) ||
                exitPath == null ||
                exitPath.Length == 0)
            {
                audit = "exit_path_not_found unit='" + monsterId + "' md='" + (md.MdName ?? string.Empty) + "' exitAudit='" + exitMdAudit + " " + exitAudit + "'";
                return false;
            }
            exitAudit = exitMdAudit + " " + exitAudit;

            Vector2[] rallyPathV155;
            string rallyAuditV155;
            if (C2BuildingRallyPointRuntimeV155LikeOriginal.TryAppendRallyDestinationV155LikeOriginal(
                    building,
                    exitPath,
                    out rallyPathV155,
                    out rallyAuditV155) &&
                rallyPathV155 != null &&
                rallyPathV155.Length > 0)
            {
                exitPath = rallyPathV155;
                exitAudit += " " + rallyAuditV155;
            }
            else
            {
                exitAudit += " " + rallyAuditV155;
            }

            float spawnRealX = exitPath[0].x;
            float spawnRealY = exitPath[0].y;

            byte dir = building.RealDir;
            if (exitPath.Length > 1)
                dir = C2BuildingProductionDirectionFromDeltaV114LikeOriginal(exitPath[1].x - exitPath[0].x, exitPath[1].y - exitPath[0].y);

            C2Settlement3InuMdV2Record r = new C2Settlement3InuMdV2Record();
            r.Index = s_C2BuildingProductionRuntimeUnitIndexV114++;
            r.Nation = (byte)Mathf.Clamp(nation, 0, 255);
            r.NIndex = 0;
            r.RealX = Mathf.RoundToInt(spawnRealX);
            r.RealY = Mathf.RoundToInt(spawnRealY);
            r.RealDir = dir;
            r.Life = 0;
            r.Stage = 0;
            r.MonsterId = monsterId;

            string alias = C2NeutralPeasantUnitsV2ResolvedMdAliasLikeOriginal(r, md);
            if (string.IsNullOrWhiteSpace(alias)) alias = md.MdName ?? mdLookup;

            List<C2NeutralPeasantUnitFrameV2LikeOriginal> idleFrames;
            List<C2NeutralPeasantUnitFrameV2LikeOriginal> walkFrames;
            string framesAudit;
            string walkAudit;
            bool walkAnimFound;
            bool visualCacheHit;
            if (!C2NeutralPeasantUnitsV19TryGetOrBuildVisualFramesLikeOriginal(
                    md,
                    r,
                    out idleFrames,
                    out walkFrames,
                    out framesAudit,
                    out walkAudit,
                    out walkAnimFound,
                    out visualCacheHit) || idleFrames == null || idleFrames.Count == 0)
            {
                audit = "visual_not_found unit='" + monsterId + "' md='" + (md.MdName ?? string.Empty) + "' framesAudit='" + framesAudit + "' walkAudit='" + walkAudit + "'";
                return false;
            }

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] walkDirectionBanks;
            string walkBankAudit;
            bool walkBankCacheHit;
            C2NeutralPeasantUnitsV19GetOrBuildWalkDirectionBanksLikeOriginal(md, r, out walkDirectionBanks, out walkBankAudit, out walkBankCacheHit);

            C2NeutralPeasantUnitMotionBanksV20LikeOriginal motionBanks;
            string motionBankAudit;
            bool motionBankCacheHit;
            C2NeutralPeasantUnitsV20GetOrBuildMotionBanksLikeOriginal(md, r, out motionBanks, out motionBankAudit, out motionBankCacheHit);

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] idleDirectionBanks;
            string idleBankAudit;
            bool idleBankCacheHit;
            C2NeutralPeasantUnitsV23GetOrBuildIdleDirectionBanksLikeOriginal(md, r, out idleDirectionBanks, out idleBankAudit, out idleBankCacheHit);

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] restDirectionBanks;
            string restBankAudit;
            bool restBankCacheHit;
            C2NeutralPeasantUnitsV30GetOrBuildRestDirectionBanksLikeOriginal(md, r, out restDirectionBanks, out restBankAudit, out restBankCacheHit);

            C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal selInfo = C2NeutralPeasantUnitsV19GetSelectionInfoLikeOriginal(md);
            Transform root = C2BuildingProductionFindOrCreateProducedUnitRootV114LikeOriginal();
            int beforeChildCount = root != null ? root.childCount : 0;
            C2NeutralPeasantUnitsV2CreateUnitObjectLikeOriginal(
                root,
                r,
                md,
                idleFrames,
                idleDirectionBanks,
                restDirectionBanks,
                walkFrames,
                walkDirectionBanks,
                motionBanks,
                selInfo,
                alias,
                "runtime_produced_v114 | " + framesAudit + " | " + walkAudit + " | " + walkBankAudit + " | " + motionBankAudit + " | " + idleBankAudit + " | " + restBankAudit);

            int afterChildCount = root != null ? root.childCount : 0;
            if (root != null)
            {
                for (int i = Mathf.Max(0, beforeChildCount); i < afterChildCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child == null) continue;
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = child.GetComponentInChildren<C2NeutralPeasantUnitInfoV2LikeOriginal>(true);
                    if (u != null && u.RecordIndex == r.Index)
                    {
                        spawned = u;
                        break;
                    }
                }

                if (spawned == null)
                {
                    // Rare fallback only inside the produced-unit root, not the whole scene.
                    C2NeutralPeasantUnitInfoV2LikeOriginal[] produced = root.GetComponentsInChildren<C2NeutralPeasantUnitInfoV2LikeOriginal>(true);
                    for (int i = 0; i < produced.Length; i++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal u = produced[i];
                        if (u != null && u.RecordIndex == r.Index)
                        {
                            spawned = u;
                            break;
                        }
                    }
                }
            }

            if (spawned != null)
            {
                // V127: produced unit must not steal selection from the producing building.
                // Do not call PreserveSelectedBuildingOnly here: it scans every unit/building in the scene and caused
                // a visible hitch exactly when the trained unit appears.  The building is already selected; just make
                // the new unit non-selected and keep map-pick suppression alive for this frame window.
                spawned.SetSelected(false);
                // V146: produced units must not block map selection.
                // Production is autonomous; after the player clicks terrain/another unit/another building,
                // the HUD must be allowed to close even if the building is still producing.
                // Only the HUD card click itself suppresses the map picker.
            }

            if (spawned != null && exitPath != null && exitPath.Length > 1)
            {
                C2ProducedUnitExitPathDriverV124LikeOriginal.AttachLikeOriginal(
                    spawned,
                    exitPath,
                    C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    dir,
                    "building_produce_v155_rally_dstx_dsty");
            }

            audit = "ok unit='" + monsterId + "' md='" + (md.MdName ?? string.Empty) +
                    "' record=" + r.Index.ToString(CultureInfo.InvariantCulture) +
                    " spawned=" + (spawned != null) +
                    " beforeChildren=" + beforeChildCount.ToString(CultureInfo.InvariantCulture) +
                    " afterChildren=" + afterChildCount.ToString(CultureInfo.InvariantCulture) +
                    " exitPath=" + (exitPath != null ? exitPath.Length : 0).ToString(CultureInfo.InvariantCulture) +
                    " exitAudit='" + exitAudit + "'" +
                    " visualCache=" + visualCacheHit +
                    " walkFound=" + walkAnimFound;
            return spawned != null;
        }

        private C2Settlement3InuMdV2Info C2BuildingProductionResolveBuildingMdForExitPathV136LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2Settlement3InuMdV2Info fallbackMd,
            out string audit)
        {
            audit = "building_md=none";
            if (building != null)
            {
                string catalogMd = C2OriginalProduceCatalogV13.ResolveMdForSelectedBuildingLikeOriginal(building);
                C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(catalogMd);
                if (md != null && md.Found)
                {
                    audit = "building_md=catalog '" + catalogMd + "'";
                    return md;
                }

                string source = building.SourceMonsterId ?? string.Empty;
                md = C2Settlement3InuMdV2ResolveMdLikeOriginal(source);
                if (md != null && md.Found)
                {
                    audit = "building_md=source '" + source + "'";
                    return md;
                }

                string stripped = C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(source);
                md = C2Settlement3InuMdV2ResolveMdLikeOriginal(stripped);
                if (md != null && md.Found)
                {
                    audit = "building_md=stripped '" + stripped + "'";
                    return md;
                }
            }

            if (fallbackMd != null && fallbackMd.Found)
            {
                audit = "building_md=fallback_unit_md '" + (fallbackMd.MdName ?? string.Empty) + "'";
                return fallbackMd;
            }

            return null;
        }

        private bool C2BuildingProductionTryGetOrBuildExitPathV136LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2Settlement3InuMdV2Info md,
            out Vector2[] exitPath,
            out string audit)
        {
            exitPath = null;
            audit = "not_started";
            if (building == null)
            {
                audit = "no_building";
                return false;
            }

            string key = C2BuildingProductionExitPathCacheKeyV136LikeOriginal(building, md);
            C2BuildingProductionExitPathCacheV136 cached;
            if (!string.IsNullOrEmpty(key) &&
                _c2BuildingProductionExitPathCacheV136LikeOriginal.TryGetValue(key, out cached) &&
                cached != null &&
                cached.Path != null &&
                cached.Path.Length > 0)
            {
                exitPath = cached.Path;
                audit = "exit_path_cache_hit " + (cached.Audit ?? string.Empty);
                return true;
            }

            Vector2[] builtPath;
            string builtAudit;
            if (!C2BuildingProductionBuildBornConcentratorExitPathV124LikeOriginal(building, md, out builtPath, out builtAudit) ||
                builtPath == null ||
                builtPath.Length == 0)
            {
                float fallbackX = building.RealX + 384.0f;
                float fallbackY = building.RealY + 384.0f;
                builtPath = new Vector2[]
                {
                    new Vector2(fallbackX, fallbackY),
                    new Vector2(fallbackX + 512.0f, fallbackY + 256.0f)
                };
                builtAudit = "fallback_no_born_or_concentrator source='" + (builtAudit ?? string.Empty) + "'";
            }

            exitPath = builtPath;
            audit = builtAudit ?? string.Empty;

            if (!string.IsNullOrEmpty(key) && exitPath != null && exitPath.Length > 0)
            {
                _c2BuildingProductionExitPathCacheV136LikeOriginal[key] =
                    new C2BuildingProductionExitPathCacheV136
                    {
                        Path = exitPath,
                        Audit = audit
                    };
            }

            return exitPath != null && exitPath.Length > 0;
        }

        private static string C2BuildingProductionExitPathCacheKeyV136LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2Settlement3InuMdV2Info md)
        {
            if (building == null) return string.Empty;
            string mdKey = md != null
                ? (!string.IsNullOrEmpty(md.MdPath) ? md.MdPath : (md.MdName ?? string.Empty))
                : string.Empty;
            // V137: do not use Unity instance id here. Runtime building wrappers can be rebuilt while the
            // original building record stays the same; InstanceID would turn a valid original-style cache into misses.
            return "record=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                   "|real=" + building.RealX.ToString(CultureInfo.InvariantCulture) +
                   "," + building.RealY.ToString(CultureInfo.InvariantCulture) +
                   "|dir=" + building.RealDir.ToString(CultureInfo.InvariantCulture) +
                   "|source=" + (building.SourceMonsterId ?? string.Empty) +
                   "|md=" + mdKey;
        }

        private bool C2BuildingProductionBuildBornConcentratorExitPathV124LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2Settlement3InuMdV2Info md,
            out Vector2[] exitPath,
            out string audit)
        {
            exitPath = null;
            audit = "not_started";
            if (building == null)
            {
                audit = "no_building";
                return false;
            }

            var points = new List<Vector2>(8);
            bool bornFromMd = false;
            bool bornFromMotion = false;
            Vector2[] bornPath = null;

            // V137 original-like spawn exit:
            // Build.cpp uses NM->BornPtX/BornPtY directly:
            // CreateNewMonsterAt(BORNPOINT[0]) and then NewMonsterPreciseSendTo(BORNPOINT[1..N]).
            // No pathfinding to CONCENTRATOR is done on produce click or on spawn.
            if (md != null && md.Zones != null && md.Zones.BornPoints != null && md.Zones.BornPoints.Count > 0)
            {
                int cornerX;
                int cornerY;
                C2Settlement3InuMdV2Record br = new C2Settlement3InuMdV2Record();
                br.Index = building.RecordIndex;
                br.RealX = building.RealX;
                br.RealY = building.RealY;
                br.RealDir = building.RealDir;
                br.MonsterId = building.SourceMonsterId ?? string.Empty;
                C2Settlement3InuMdV2BuildingCornerCellLikeOriginal(br, md, out cornerX, out cornerY);

                for (int i = 0; i < md.Zones.BornPoints.Count; i++)
                {
                    int lx = md.Zones.BornPoints[i].X;
                    int ly = md.Zones.BornPoints[i].Y;
                    float rx = ((cornerX << 4) + lx) << 4;
                    float ry = ((cornerY << 4) + ly) << 4;
                    AppendUniqueExitPointV124LikeOriginal(points, new Vector2(rx, ry));
                }

                bornFromMd = points.Count > 0;
            }

            // Data-only fallback: use the already registered BORNPOINTS table if the MD object was not available here.
            // This is still original BORNPOINT data, not A*/CONCENTRATOR search.
            if (points.Count == 0)
            {
                bornFromMotion = C2BuildingMotionFieldV1TryGetBornExitPathRealLikeOriginal(building.RecordIndex, out bornPath) &&
                                 bornPath != null &&
                                 bornPath.Length > 0;
                if (bornFromMotion)
                    AppendUniqueExitPointsV124LikeOriginal(points, bornPath);
            }

            if (points.Count == 0)
            {
                audit = "no_bornpoints_original_like record=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            exitPath = points.ToArray();
            audit = "original_bornpoints_only" +
                    " bornFromMd=" + bornFromMd +
                    " bornFromMotion=" + bornFromMotion +
                    " bornCount=" + exitPath.Length.ToString(CultureInfo.InvariantCulture) +
                    " record=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static void AppendUniqueExitPointsV124LikeOriginal(List<Vector2> dst, Vector2[] src)
        {
            if (dst == null || src == null) return;
            for (int i = 0; i < src.Length; i++)
                AppendUniqueExitPointV124LikeOriginal(dst, src[i]);
        }

        private static void AppendUniqueExitPointV124LikeOriginal(List<Vector2> dst, Vector2 p)
        {
            if (dst == null) return;
            if (dst.Count > 0)
            {
                Vector2 last = dst[dst.Count - 1];
                if ((last - p).sqrMagnitude < 16.0f)
                    return;
            }
            dst.Add(p);
        }

        private Transform C2BuildingProductionFindOrCreateProducedUnitRootV114LikeOriginal()
        {
            string name = C2NeutralPeasantUnitsV2RootPrefixLikeOriginal + "ProducedRuntimeV114";
            Transform existing = transform.Find(name);
            if (existing != null) return existing;
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, true);
            return go.transform;
        }

        private static byte C2BuildingProductionDirectionFromDeltaV114LikeOriginal(float dx, float dy)
        {
            if (Mathf.Abs(dx) < 0.001f && Mathf.Abs(dy) < 0.001f) return 0;
            double a = Math.Atan2(dy, dx);
            if (a < 0.0) a += Math.PI * 2.0;
            int d = Mathf.RoundToInt((float)(a / (Math.PI * 2.0) * 256.0)) & 255;
            return (byte)d;
        }
    }
}
