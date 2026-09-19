using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // UNIT LOGIC REMOVED, BUILDING HUD KEPT.
    // This keeps building produce card UI/queue state alive without spawning any unit objects.
    public struct C2BuildingProduceCardStateV114LikeOriginal
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
        private RectTransform _rt2;
        private Image _img;
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
            _img = GetComponent<Image>();
            _img2 = secondPass;
            _rt = transform as RectTransform;
            _rt2 = secondPass != null ? secondPass.rectTransform : null;
            Update();
        }

        private void Update()
        {
            if (_rt == null) _rt = transform as RectTransform;
            if (_img == null) _img = GetComponent<Image>();
            C2BuildingProduceCardStateV114LikeOriginal st =
                C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(_building, _unitId);
            bool visible = st.Count > 0 || st.Infinite;
            if (_img != null) _img.enabled = visible;
            if (_img2 != null) _img2.enabled = visible;
            if (_rt != null)
            {
                int h = visible && st.Progress01 > 0.0f ? Mathf.Clamp(Mathf.RoundToInt(_fullH * Mathf.Clamp01(st.Progress01)), 1, _fullH) : 1;
                ApplyBottomUpRectLikeOriginal(_rt, h);
                ApplyBottomUpRectLikeOriginal(_rt2, h);
            }
        }

        private void ApplyBottomUpRectLikeOriginal(RectTransform rt, int h)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(_x, -(_topY + _fullH));
            rt.sizeDelta = new Vector2(_w, h);
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
            bool visible = st.Count > 0 || st.Infinite;
            if (_plate != null) _plate.enabled = visible;
            if (_plate2 != null) _plate2.enabled = visible;
            if (_amount != null)
            {
                _amount.enabled = visible;
                _amount.text = st.Infinite ? "∞" : (st.Count > 0 ? st.Count.ToString() : string.Empty);
            }
        }
    }

    internal sealed class C2BuildingProductionOrderV114
    {
        public C2OriginalProduceItemV13 Item;
        public string UnitId = string.Empty;
        public int Count;
        public bool Infinite;
        public float Stage;
        public int MaxStage = 100;

        // V259: original order belongs to one exact building object (OBJ->newMons/OBJ->Nat).
        public int OwnerRecordIndex;
        public int OwnerNation;
        public int OwnerRealX;
        public int OwnerRealY;
        public string OwnerSourceMonsterId = string.Empty;
        public string OwnerKindName = string.Empty;
    }

    public sealed class C2BuildingProductionCardsRuntimeV114 : MonoBehaviour
    {
        // V257A: helper belongs to the runtime queue class, not the progress-bar UI component.
        private static bool IsProductionOrderCompatibleWithCurrentBuildingV257LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2BuildingProductionOrderV114 q,
            out string audit)
        {
            audit = string.Empty;
            if (building == null || q == null || q.Item == null)
                return true;

            string buildingMember = NormalizeOwnerTokenV257LikeOriginal(building.SourceMonsterId);
            string buildingMd = NormalizeOwnerTokenV257LikeOriginal(building.KindName);
            string orderBuilder = NormalizeOwnerTokenV257LikeOriginal(q.Item.BuilderId);
            string orderBuilderMd = NormalizeOwnerTokenV257LikeOriginal(q.Item.BuilderMd);

            bool hasOrderBuilder = !string.IsNullOrEmpty(orderBuilder);
            bool hasBuildingMember = !string.IsNullOrEmpty(buildingMember);
            bool hasOrderMd = !string.IsNullOrEmpty(orderBuilderMd);
            bool hasBuildingMd = !string.IsNullOrEmpty(buildingMd);

            bool memberOk = hasOrderBuilder && hasBuildingMember &&
                            string.Equals(orderBuilder, buildingMember, StringComparison.OrdinalIgnoreCase);
            bool mdOk = hasOrderMd && hasBuildingMd &&
                        string.Equals(orderBuilderMd, buildingMd, StringComparison.OrdinalIgnoreCase);

            if (memberOk || mdOk)
                return true;

            // If older objects do not carry builder metadata, do not block them.
            if (!hasOrderBuilder && !hasOrderMd)
                return true;

            audit = "buildingMember='" + (building.SourceMonsterId ?? string.Empty) +
                    "' buildingMd='" + (building.KindName ?? string.Empty) +
                    "' orderBuilder='" + (q.Item.BuilderId ?? string.Empty) +
                    "' orderBuilderMd='" + (q.Item.BuilderMd ?? string.Empty) +
                    "' unit='" + (q.Item.UnitId ?? string.Empty) +
                    "' unitMd='" + (q.Item.MdName ?? string.Empty) + "'";
            return false;
        }

        private static bool IsProductionOrderCompatibleWithCurrentBuildingV259LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2BuildingProductionOrderV114 q,
            out string audit)
        {
            audit = string.Empty;
            if (!IsProductionOrderCompatibleWithCurrentBuildingV257LikeOriginal(building, q, out audit))
                return false;

            if (building == null || q == null)
                return true;

            bool recordOk = q.OwnerRecordIndex == 0 || q.OwnerRecordIndex == building.RecordIndex;
            bool nationOk = q.OwnerNation == building.Nation;
            bool memberOk = string.IsNullOrEmpty(q.OwnerSourceMonsterId) ||
                            string.Equals(q.OwnerSourceMonsterId, building.SourceMonsterId ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            bool mdOk = string.IsNullOrEmpty(q.OwnerKindName) ||
                        string.Equals(q.OwnerKindName, building.KindName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // Allow small runtime/proxy offsets, but never allow the order to jump to another barracks across the map.
            int dist = Mathf.Abs(q.OwnerRealX - building.RealX) + Mathf.Abs(q.OwnerRealY - building.RealY);
            bool realOk = (q.OwnerRealX == 0 && q.OwnerRealY == 0) || dist <= 4096;

            if (recordOk && nationOk && memberOk && mdOk && realOk)
                return true;

            audit = "ownerRecord=" + q.OwnerRecordIndex.ToString(CultureInfo.InvariantCulture) +
                    " buildingRecord=" + building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                    " ownerNation=" + q.OwnerNation.ToString(CultureInfo.InvariantCulture) +
                    " buildingNation=" + building.Nation.ToString(CultureInfo.InvariantCulture) +
                    " owner='" + (q.OwnerSourceMonsterId ?? string.Empty) +
                    "' building='" + (building.SourceMonsterId ?? string.Empty) +
                    "' ownerMd='" + (q.OwnerKindName ?? string.Empty) +
                    "' buildingMd='" + (building.KindName ?? string.Empty) +
                    "' ownerReal=(" + q.OwnerRealX.ToString(CultureInfo.InvariantCulture) + "," + q.OwnerRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " buildingReal=(" + building.RealX.ToString(CultureInfo.InvariantCulture) + "," + building.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " dist=" + dist.ToString(CultureInfo.InvariantCulture);
            return false;
        }

        private static string NormalizeOwnerTokenV257LikeOriginal(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;
            return s.Trim();
        }

        private const string ContractV14LikeOriginal = "V14_OLD13_PRODUCE_SPEED_ROUND_ROBIN";
        private const float OriginalStageTicksPerSecondLikeOriginal = 18.0f;
        private static float s_suppressMapSelectionUntilV126LikeOriginal;
        private static readonly Dictionary<string, int> s_produceStagesCacheV14LikeOriginal =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<C2BuildingProductionOrderV114> _queue = new List<C2BuildingProductionOrderV114>(16);

        public static void SchedulePrewarmForBuildingMenuV127LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, List<C2OriginalProduceItemV13> items)
        {
            // UNIT LOGIC REMOVED: no visual prewarm.
        }

        public void EnqueueLikeOriginal(C2OriginalProduceItemV13 item)
        {
            EnqueueLikeOriginal(item, 1, false, GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>());
        }

        public void EnqueueLikeOriginal(C2OriginalProduceItemV13 item, int amount, bool infinite)
        {
            EnqueueLikeOriginal(item, amount, infinite, GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>());
        }

        public void EnqueueLikeOriginal(C2OriginalProduceItemV13 item, int amount, bool infinite, C2SettlementBuildingSelectableV1LikeOriginal ownerBuilding)
        {
            if (item == null || item.Building) return;
            string id = item.UnitId ?? string.Empty;
            if (id.Length == 0) return;

            C2OriginalProduceItemV13 ownedItem = CloneProduceItemForOwnerV259LikeOriginal(item, ownerBuilding);
            int ownerRecord = ownerBuilding != null ? ownerBuilding.RecordIndex : 0;
            int ownerNation = ownerBuilding != null ? ownerBuilding.Nation : ownedItem.Nation;
            int ownerRealX = ownerBuilding != null ? ownerBuilding.RealX : 0;
            int ownerRealY = ownerBuilding != null ? ownerBuilding.RealY : 0;
            string ownerSource = ownerBuilding != null ? (ownerBuilding.SourceMonsterId ?? string.Empty) : string.Empty;
            string ownerKind = ownerBuilding != null ? (ownerBuilding.KindName ?? string.Empty) : string.Empty;

            for (int i = 0; i < _queue.Count; i++)
            {
                C2BuildingProductionOrderV114 q = _queue[i];
                if (string.Equals(q.UnitId, id, StringComparison.OrdinalIgnoreCase) &&
                    q.OwnerRecordIndex == ownerRecord &&
                    q.OwnerNation == ownerNation &&
                    string.Equals(q.OwnerSourceMonsterId ?? string.Empty, ownerSource, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(q.OwnerKindName ?? string.Empty, ownerKind, StringComparison.OrdinalIgnoreCase))
                {
                    if (infinite) q.Infinite = true;
                    else q.Count += Mathf.Max(1, amount);
                    return;
                }
            }

            _queue.Add(new C2BuildingProductionOrderV114
            {
                Item = ownedItem,
                UnitId = id,
                Count = infinite ? 0 : Mathf.Max(1, amount),
                Infinite = infinite,
                MaxStage = Mathf.Max(1, C2BuildingProductionResolveUnitProduceStagesLikeOriginal(ownedItem.MdName, id)),
                Stage = 0.0f,
                OwnerRecordIndex = ownerRecord,
                OwnerNation = ownerNation,
                OwnerRealX = ownerRealX,
                OwnerRealY = ownerRealY,
                OwnerSourceMonsterId = ownerSource,
                OwnerKindName = ownerKind
            });
        }

        private static C2OriginalProduceItemV13 CloneProduceItemForOwnerV259LikeOriginal(
            C2OriginalProduceItemV13 src,
            C2SettlementBuildingSelectableV1LikeOriginal ownerBuilding)
        {
            C2OriginalProduceItemV13 dst = new C2OriginalProduceItemV13();
            if (src == null) return dst;

            dst.BuilderId = src.BuilderId ?? string.Empty;
            dst.BuilderMd = src.BuilderMd ?? string.Empty;
            dst.UnitId = src.UnitId ?? string.Empty;
            dst.MdName = src.MdName ?? string.Empty;
            dst.GridX = src.GridX;
            dst.GridY = src.GridY;
            dst.Nation = ownerBuilding != null ? ownerBuilding.Nation : src.Nation;
            dst.Enabled = src.Enabled;
            dst.Building = src.Building;
            dst.Peasant = src.Peasant;
            dst.IconFileId = src.IconFileId ?? string.Empty;
            dst.IconSpriteId = src.IconSpriteId;
            dst.RootSpriteId = src.RootSpriteId;
            dst.Source = src.Source ?? string.Empty;
            dst.DisplayNameKey = src.DisplayNameKey ?? string.Empty;
            dst.DisplayText = src.DisplayText ?? string.Empty;
            dst.HotKey = src.HotKey ?? string.Empty;
            return dst;
        }

        public bool CancelAllLikeOriginal(string unitId)
        {
            unitId = unitId ?? string.Empty;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_queue[i].UnitId, unitId, StringComparison.OrdinalIgnoreCase))
                {
                    _queue.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void CancelOneLikeOriginal(string unitId)
        {
            unitId = unitId ?? string.Empty;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                C2BuildingProductionOrderV114 q = _queue[i];
                if (!string.Equals(q.UnitId, unitId, StringComparison.OrdinalIgnoreCase)) continue;
                if (q.Infinite) q.Infinite = false;
                else q.Count--;
                if (!q.Infinite && q.Count <= 0) _queue.RemoveAt(i);
                return;
            }
        }

        public C2BuildingProduceCardStateV114LikeOriginal GetCardStateLikeOriginal(string unitId)
        {
            unitId = unitId ?? string.Empty;
            for (int i = 0; i < _queue.Count; i++)
            {
                C2BuildingProductionOrderV114 q = _queue[i];
                if (!string.Equals(q.UnitId, unitId, StringComparison.OrdinalIgnoreCase)) continue;
                return new C2BuildingProduceCardStateV114LikeOriginal
                {
                    Count = q.Count,
                    Infinite = q.Infinite,
                    Progress01 = Mathf.Clamp01(q.Stage / Mathf.Max(1, q.MaxStage)),
                    MaxStage = q.MaxStage
                };
            }
            return new C2BuildingProduceCardStateV114LikeOriginal();
        }

        public static bool TryHandleBuildingProduceClickLikeOriginal(C2OriginalProduceItemV13 item)
        {
            if (item == null || item.Building) return false;

            string ownerAudit;
            C2SettlementBuildingSelectableV1LikeOriginal building = FirstSelectedReadyBuildingForItemV258LikeOriginal(item, out ownerAudit);
            if (building == null)
            {
                Debug.LogWarning("[C2:BUILDING PRODUCE V259 OWNER] click_rejected " + (ownerAudit ?? string.Empty));
                return false;
            }

            SuppressMapSelectionFromHudClickV126LikeOriginal();

            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null) rt = building.gameObject.AddComponent<C2BuildingProductionCardsRuntimeV114>();

            ResolveBuildingProduceClickAmountLikeOriginal(out int amount, out bool infinite);

            // Original keeps the order on OBJ and uses OBJ->Nat/NNUM at completion.
            // Do not keep stale Item.Nation from a reused HUD card.
            item.Nation = building.Nation;
            rt.EnqueueLikeOriginal(item, amount, infinite, building);

            PreserveSelectedBuildingOnlyV126LikeOriginal(building);
            Debug.Log("[C2:BUILDING PRODUCE V259 OWNER] click_ok " + (ownerAudit ?? string.Empty) +
                      " unit='" + (item.UnitId ?? string.Empty) +
                      "' unitMd='" + (item.MdName ?? string.Empty) +
                      "' buildingNation=" + building.Nation.ToString(CultureInfo.InvariantCulture));
            return true;
        }

        public static bool TryHandleBuildingProduceClickForBuildingLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            int amount,
            bool infinite,
            string source)
        {
            if (item == null || item.Building) return false;

            if (building == null || !building.isActiveAndEnabled || !building.gameObject.activeInHierarchy ||
                building.NotSelectable || !building.ReadyLikeOriginal)
            {
                Debug.LogWarning("[C2:BUILDING PRODUCE V260 DIRECT] click_rejected source='" + (source ?? string.Empty) +
                                 "' reason=no_live_ready_building unit='" + (item.UnitId ?? string.Empty) + "'");
                return false;
            }

            string ownerAudit;
            if (!IsProductionItemCompatibleWithBuildingV258LikeOriginal(building, item, out ownerAudit))
            {
                Debug.LogWarning("[C2:BUILDING PRODUCE V260 DIRECT] click_rejected source='" + (source ?? string.Empty) +
                                 "' " + (ownerAudit ?? string.Empty));
                return false;
            }

            SuppressMapSelectionFromHudClickV126LikeOriginal();

            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null) rt = building.gameObject.AddComponent<C2BuildingProductionCardsRuntimeV114>();

            item.Nation = building.Nation;
            rt.EnqueueLikeOriginal(item, Mathf.Max(1, amount), infinite, building);

            Debug.Log("[C2:BUILDING PRODUCE V260 DIRECT] click_ok source='" + (source ?? string.Empty) +
                      "' " + (ownerAudit ?? string.Empty) +
                      " unit='" + (item.UnitId ?? string.Empty) +
                      "' unitMd='" + (item.MdName ?? string.Empty) +
                      "' buildingNation=" + building.Nation.ToString(CultureInfo.InvariantCulture) +
                      " amount=" + Mathf.Max(1, amount).ToString(CultureInfo.InvariantCulture) +
                      " infinite=" + infinite.ToString());
            return true;
        }

        private static void ResolveBuildingProduceClickAmountLikeOriginal(out int amount, out bool infinite)
        {
            bool ctrl = false;
            bool shift = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                ctrl = ctrl || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                shift = shift || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            catch
            {
                // Input System-only projects throw on legacy Input.GetKey.
            }
#endif

            if (ctrl)
            {
                amount = 1;
                infinite = false;
                return;
            }

            if (shift)
            {
                amount = 10;
                infinite = false;
                return;
            }

            amount = 0;
            infinite = true;
        }

        public static bool TryHandleBuildingProduceCancelClickLikeOriginal(C2OriginalProduceItemV13 item)
        {
            if (item == null || item.Building) return false;

            string ownerAudit;
            C2SettlementBuildingSelectableV1LikeOriginal building = FirstSelectedReadyBuildingForItemV258LikeOriginal(item, out ownerAudit);
            if (building == null)
            {
                Debug.LogWarning("[C2:BUILDING PRODUCE V259 OWNER] cancel_rejected " + (ownerAudit ?? string.Empty));
                return false;
            }

            SuppressMapSelectionFromHudClickV126LikeOriginal();
            C2BuildingProductionCardsRuntimeV114 rt = building.GetComponent<C2BuildingProductionCardsRuntimeV114>();
            if (rt == null) return false;
            bool cancelled = rt.CancelAllLikeOriginal(item.UnitId ?? string.Empty);
            PreserveSelectedBuildingOnlyV126LikeOriginal(building);
            return cancelled;
        }

        public static void SuppressMapSelectionFromHudClickV126LikeOriginal()
        {
            s_suppressMapSelectionUntilV126LikeOriginal = Mathf.Max(s_suppressMapSelectionUntilV126LikeOriginal, Time.realtimeSinceStartup + 0.12f);
        }

        public static bool ShouldSuppressMapSelectionFromHudClickV126LikeOriginal()
        {
            return Time.realtimeSinceStartup < s_suppressMapSelectionUntilV126LikeOriginal;
        }

        public static void PreserveSelectedBuildingOnlyV126LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal keep)
        {
            if (keep == null) return;
            C2SettlementBuildingSelectableV1LikeOriginal[] all = UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null) all[i].SetSelected(all[i] == keep);
            }
            C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal = keep;
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
            List<string> parts = new List<string>(rt._queue.Count);
            for (int i = 0; i < rt._queue.Count; i++)
            {
                C2BuildingProductionOrderV114 q = rt._queue[i];
                parts.Add((q.UnitId ?? string.Empty) + ":" + (q.Infinite ? "inf" : q.Count.ToString()));
            }
            return "queue=" + string.Join(",", parts.ToArray());
        }

        private static C2SettlementBuildingSelectableV1LikeOriginal FirstSelectedReadyBuildingLikeOriginal()
        {
            C2SettlementBuildingSelectableV1LikeOriginal direct = C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal;
            if (IsUsableSelectedBuildingV258LikeOriginal(direct)) return direct;

            C2SettlementBuildingSelectableV1LikeOriginal[] all = UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            C2SettlementBuildingSelectableV1LikeOriginal best = null;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (!IsUsableSelectedBuildingV258LikeOriginal(b)) continue;
                if (best == null || b.SortKey < best.SortKey) best = b;
            }
            return best;
        }

        // V258:
        // Original production is not a global "last selected card" queue.
        // Multi.cpp::ProduceObject() first calls DiscardUnitsOfOtherPlayer(NI), then
        // OneObject::Produce() stores the order on THIS selected building object.
        // Build.cpp::ProduceObjLink() later uses OBJ->Nat / OBJ->NNUM / OBJ->newMons.
        // Therefore a card built for EngKaz must enqueue only on the selected EngKaz instance,
        // and the spawned unit's player color/team must come from that building instance.
        private static C2SettlementBuildingSelectableV1LikeOriginal FirstSelectedReadyBuildingForItemV258LikeOriginal(C2OriginalProduceItemV13 item, out string audit)
        {
            audit = string.Empty;

            C2SettlementBuildingSelectableV1LikeOriginal direct = C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal;
            if (IsUsableSelectedBuildingV258LikeOriginal(direct) &&
                IsProductionItemCompatibleWithBuildingV258LikeOriginal(direct, item, out audit))
                return direct;

            C2SettlementBuildingSelectableV1LikeOriginal[] all = UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            C2SettlementBuildingSelectableV1LikeOriginal best = null;
            string lastReject = audit;

            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (!IsUsableSelectedBuildingV258LikeOriginal(b)) continue;

                string localAudit;
                if (!IsProductionItemCompatibleWithBuildingV258LikeOriginal(b, item, out localAudit))
                {
                    lastReject = localAudit;
                    continue;
                }

                if (best == null || b.SortKey < best.SortKey)
                    best = b;
            }

            if (best != null)
            {
                audit = "ok building='" + (best.SourceMonsterId ?? string.Empty) +
                        "' md='" + (best.KindName ?? string.Empty) +
                        "' nation=" + best.Nation.ToString(CultureInfo.InvariantCulture);
                return best;
            }

            audit = "no_selected_compatible_building itemBuilder='" + (item != null ? (item.BuilderId ?? string.Empty) : string.Empty) +
                    "' itemBuilderMd='" + (item != null ? (item.BuilderMd ?? string.Empty) : string.Empty) +
                    "' lastReject=[" + (lastReject ?? string.Empty) + "]";
            return null;
        }

        private static bool IsUsableSelectedBuildingV258LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            return b != null && b.isActiveAndEnabled && b.gameObject.activeInHierarchy && b.IsSelected && !b.NotSelectable;
        }

        private static bool IsProductionItemCompatibleWithBuildingV258LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            out string audit)
        {
            audit = string.Empty;
            if (building == null)
            {
                audit = "building=<null>";
                return false;
            }
            if (item == null)
            {
                audit = "item=<null>";
                return false;
            }

            string buildingMember = NormalizeOwnerTokenV257LikeOriginal(building.SourceMonsterId);
            string buildingMd = NormalizeOwnerTokenV257LikeOriginal(building.KindName);
            string itemBuilder = NormalizeOwnerTokenV257LikeOriginal(item.BuilderId);
            string itemBuilderMd = NormalizeOwnerTokenV257LikeOriginal(item.BuilderMd);

            bool memberOk = !string.IsNullOrEmpty(buildingMember) &&
                            !string.IsNullOrEmpty(itemBuilder) &&
                            string.Equals(buildingMember, itemBuilder, StringComparison.OrdinalIgnoreCase);
            bool mdOk = !string.IsNullOrEmpty(buildingMd) &&
                        !string.IsNullOrEmpty(itemBuilderMd) &&
                        string.Equals(buildingMd, itemBuilderMd, StringComparison.OrdinalIgnoreCase);

            if (memberOk || mdOk)
            {
                audit = "compatible building='" + (building.SourceMonsterId ?? string.Empty) +
                        "' md='" + (building.KindName ?? string.Empty) +
                        "' itemBuilder='" + (item.BuilderId ?? string.Empty) +
                        "' itemBuilderMd='" + (item.BuilderMd ?? string.Empty) + "'";
                return true;
            }

            audit = "incompatible building='" + (building.SourceMonsterId ?? string.Empty) +
                    "' md='" + (building.KindName ?? string.Empty) +
                    "' itemBuilder='" + (item.BuilderId ?? string.Empty) +
                    "' itemBuilderMd='" + (item.BuilderMd ?? string.Empty) +
                    "' unit='" + (item.UnitId ?? string.Empty) +
                    "' unitMd='" + (item.MdName ?? string.Empty) + "'";
            return false;
        }

        private void Update()
        {
            if (_queue.Count == 0) return;

            C2SettlementBuildingSelectableV1LikeOriginal building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            C2BuildingProductionOrderV114 q = _queue[0];

            if (building == null || !building.isActiveAndEnabled || building.NotSelectable)
            {
                _queue.RemoveAt(0);
                Debug.LogWarning("[C2:BUILDING PRODUCE V259 OWNER] dropped_queue_no_live_building");
                return;
            }

            if (!IsProductionOrderCompatibleWithCurrentBuildingV259LikeOriginal(building, q, out string ownerAuditV259))
            {
                Debug.LogWarning("[C2:BUILDING PRODUCE V259 OWNER] dropped_foreign_or_stale_queue " + ownerAuditV259);
                _queue.RemoveAt(0);
                return;
            }

            if (q.Item != null)
                q.Item.Nation = building.Nation;

            q.Stage += Time.deltaTime * OriginalStageTicksPerSecondLikeOriginal;

            if (q.Stage < Mathf.Max(1, q.MaxStage))
                return;

            q.Stage = 0.0f;

            C2BattleTerrainMode battle = building != null && building.OwnerMode != null
                ? building.OwnerMode
                : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            C2NeutralPeasantUnitInfoV2LikeOriginal spawned;
            string spawnAudit = battle == null ? "battle=<null>" : string.Empty;
            bool spawnedOk = false;
            if (battle != null)
            {
                int spawnNationV258 = building.Nation;
                spawnedOk = battle.C2BuildingProductionSpawnUnitFromBuildingV114LikeOriginal(
                    building,
                    q.Item,
                    spawnNationV258,
                    out spawned,
                    out spawnAudit);
            }
            if (!spawnedOk)
            {
                Debug.LogWarning("[C2:BUILDING PRODUCE V114] spawn_failed unit='" + (q.UnitId ?? string.Empty) + "' " + (spawnAudit ?? string.Empty));
                return;
            }

            if (q.Infinite)
            {
                if (_queue.Count > 1)
                {
                    _queue.RemoveAt(0);
                    q.Stage = 0.0f;
                    _queue.Add(q);
                }
            }
            else
            {
                q.Count--;
                if (q.Count <= 0)
                    _queue.RemoveAt(0);
            }
        }

        private static int C2BuildingProductionResolveUnitProduceStagesLikeOriginal(string mdName, string unitId)
        {
            string cacheKey = ((mdName ?? string.Empty).Trim() + "|" + (unitId ?? string.Empty).Trim()).ToUpperInvariant();
            int cachedStages;
            if (s_produceStagesCacheV14LikeOriginal.TryGetValue(cacheKey, out cachedStages))
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
                        if (TryReadProduceStagesLikeOriginal(paths[p], out stages))
                        {
                            s_produceStagesCacheV14LikeOriginal[cacheKey] = stages;
                            return stages;
                        }
                    }
                }
            }

            s_produceStagesCacheV14LikeOriginal[cacheKey] = 100;
            return 100;
        }

        private static string[] BuildNameCandidatesLikeOriginal(string mdName, string unitId)
        {
            var list = new List<string>(4);
            AddNameLikeOriginal(list, mdName);
            AddNameLikeOriginal(list, StripNationSuffixLikeOriginal(mdName));
            AddNameLikeOriginal(list, unitId);
            AddNameLikeOriginal(list, StripNationSuffixLikeOriginal(unitId));
            return list.ToArray();
        }

        private static void AddNameLikeOriginal(List<string> list, string s)
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

        private static bool TryReadProduceStagesLikeOriginal(string path, out int stages)
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
                    int.TryParse(t[1], out stages) && stages > 0)
                    return true;
            }
            return false;
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        public bool C2BuildingProductionPrewarmUnitVisualsV127LikeOriginal(string unitId, int nation, out string audit)
        {
            audit = "unit_logic_removed prewarm_disabled";
            return false;
        }

        public bool C2BuildingProductionPrewarmUnitVisualsV128LikeOriginal(string unitId, string mdName, int nation, out string audit)
        {
            audit = "unit_logic_removed prewarm_disabled";
            return false;
        }

        public bool C2BuildingProductionPrewarmExitPathForBuildingV136LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string source, out string audit)
        {
            audit = "unit_logic_removed exit_path_disabled";
            return false;
        }

        public bool C2BuildingProductionSpawnUnitFromBuildingV114LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            int nation,
            out C2NeutralPeasantUnitInfoV2LikeOriginal spawned,
            out string audit)
        {
            return C2UnitOriginalRuntimeAndRendererV1.TrySpawnProducedUnitFromBuildingLikeOriginal(
                this,
                building,
                item,
                nation,
                out spawned,
                out audit);
        }
    }
}
