using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Profiling;
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

        private enum ResourcePhaseV222
        {
            None = 0,
            MoveToResource = 1,
            WorkResource = 2,
            MoveToStore = 3,
            MoveIntoStore = 4,
            Deposit = 5,
            MoveOutOfStore = 6
        }

        private float _phase;
        private float _until;
        private bool _active;

        private bool _takeResourceV222;
        private byte _resourceIdV222;
        private int _resourceOriginalXV222;
        private int _resourceOriginalYV222;
        private int _resourceWorkOriginalXV222;
        private int _resourceWorkOriginalYV222;
        private int _resourceWorkRadiusV222;
        private Vector3 _resourceWorldV222;
        private bool _hasStoreV222;
        private int _storeRealXV222;
        private int _storeRealYV222;
        private int _storeDepositRealXV225;
        private int _storeDepositRealYV225;
        private Vector3 _storeWorldV222;
        private Vector2[] _storeConcentratorPathV228;
        private Vector2[] _storeBornPathV228;
        private ResourcePhaseV222 _resourcePhaseV222;
        private float _phaseStartedV222;
        private float _nextLogV222;
        private int _cycleV222;

        private bool _pendingTakeResourceV227;
        private C2GameplayTargetKindV1 _pendingKindV227;
        private byte _pendingResourceIdV227;
        private int _pendingResourceOriginalXV227;
        private int _pendingResourceOriginalYV227;
        private int _pendingResourceWorkRadiusV227;
        private Vector3 _pendingResourceWorldV227;
        private bool _pendingHasStoreV227;
        private int _pendingStoreRealXV227;
        private int _pendingStoreRealYV227;
        private int _pendingStoreDepositRealXV227;
        private int _pendingStoreDepositRealYV227;
        private Vector3 _pendingStoreWorldV227;
        private Vector2[] _pendingStoreConcentratorPathV228;
        private Vector2[] _pendingStoreBornPathV228;
        private C2SettlementFieldPatchV342LikeOriginal _settlementFieldV342;
        private C2SettlementDipVillageV336LikeOriginal _settlementFieldVillageV342;
        private float _nextSettlementFieldRetargetV342;
        private int _carriedResourceAmountV348;
        private int _resourceWorkCyclesV348;
        private static readonly Dictionary<string, int[]> s_resourcePortionsByMdV348 =
            new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);

        public static bool DebugResourceStorePathsV227LikeOriginal;
        public static bool LogResourceTaskEventsV343LikeOriginal;
        private GameObject _debugPathGoV227;
        private LineRenderer _debugPathLineV227;
        private LineRenderer _debugBornPathLineV229;

        private const float WorkStartDistanceOriginalPixelsV222 = 1.0f;
        private const float StoreReachDistanceOriginalPixelsV222 = 120.0f;
        private const float StorehouseApproachRadiusOriginalPixelsV223 = 180.0f;
        private const float StoreEnterReachDistanceOriginalPixelsV225 = 40.0f;
        private const float StoreEnterTimeoutSecondsV225 = 5.50f;
        private const float StoreDepositSecondsV222 = 1.00f;
        private const float ResourceWorkFpsV222 = 12.0f;

        public void Begin(C2NeutralPeasantUnitInfoV2LikeOriginal unit, C2GameplayTargetKindV1 kind, Vector3 targetWorld, float durationSeconds)
        {
            Begin(unit, kind, targetWorld, durationSeconds, null, null);
        }

        public void Begin(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2GameplayTargetKindV1 kind,
            Vector3 targetWorld,
            float durationSeconds,
            C2NeutralPeasantUnitInfoV2LikeOriginal targetUnit,
            C2SettlementBuildingSelectableV1LikeOriginal targetBuilding)
        {
            Unit = unit != null ? unit :
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2FindForGameObjectV365LikeOriginal(gameObject);
            SetHiddenInsideStoreV345LikeOriginal(false);

            TaskKind = kind;
            TargetWorld = targetWorld;
            _phase = 0.0f;
            _until = Time.realtimeSinceStartup + Mathf.Max(1.0f, durationSeconds);
            _active = Unit != null;
            _takeResourceV222 = false;
            _resourcePhaseV222 = ResourcePhaseV222.None;

            if (Unit != null && kind == C2GameplayTargetKindV1.Enemy)
            {
                C2CombatRuntimeV334LikeOriginal combat = Unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                GameObject unitProxy = combat == null ? Unit.EnsureUnityProxyLikeOriginal() : null;
                if (combat == null && unitProxy != null) combat = unitProxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
                if (combat == null) return;
                combat.BeginAttackLikeOriginal(Unit, targetUnit, targetBuilding, targetWorld);
                _active = false;
                return;
            }

            if (Unit != null)
            {
                C2UnitOriginalRuntimeLinkLikeOriginal link = Unit.RuntimeLinkCachedLikeOriginal;
                if (link != null) link.SetCarryResourceMotionV223LikeOriginal(0xFF, false);
                Unit.SetMoveDestinationLikeOriginal(targetWorld);
            }
        }

        public void BeginTakeResourceV222LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2GameplayTargetKindV1 kind,
            byte resourceId,
            int resourceOriginalX,
            int resourceOriginalY,
            int resourceWorkRadius,
            Vector3 resourceWorld,
            bool hasStore,
            int storeRealX,
            int storeRealY,
            int storeDepositRealX,
            int storeDepositRealY,
            Vector3 storeWorld,
            Vector2[] storeConcentratorPathV228,
            Vector2[] storeBornPathV228,
            bool preserveCarriedResourceV349 = false)
        {
            Unit = unit != null ? unit :
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2FindForGameObjectV365LikeOriginal(gameObject);

            if (Unit != null && _takeResourceV222 && IsCarryingResourceToStoreV227LikeOriginal())
            {
                QueuePendingTakeResourceV227LikeOriginal(
                    kind,
                    resourceId,
                    resourceOriginalX,
                    resourceOriginalY,
                    resourceWorkRadius,
                    resourceWorld,
                    hasStore,
                    storeRealX,
                    storeRealY,
                    storeDepositRealX,
                    storeDepositRealY,
                    storeWorld,
                    storeConcentratorPathV228,
                    storeBornPathV228);
                LogTakeResourceV222LikeOriginal("pending_new_resource_after_deposit_queued");
                return;
            }

            TaskKind = kind;
            TargetWorld = resourceWorld;
            _resourceIdV222 = resourceId;
            _resourceOriginalXV222 = resourceOriginalX;
            _resourceOriginalYV222 = resourceOriginalY;
            _resourceWorkOriginalXV222 = resourceOriginalX;
            _resourceWorkOriginalYV222 = resourceOriginalY;
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1TryReserveWorkPointForTargetLikeOriginal(
                    resourceOriginalX, resourceOriginalY, Unit,
                    out _resourceWorkOriginalXV222, out _resourceWorkOriginalYV222, out _);
            // Cossacks II reads the exact work radius from the sprite group's
            // [SOURCES] entry (normally 30).  The old Unity bridge forced 96,
            // so many workers considered the same central point reached and
            // continuously tried to occupy it together.
            _resourceWorkRadiusV222 = Mathf.Max(1, resourceWorkRadius);
            _resourceWorldV222 = resourceWorld;
            _hasStoreV222 = hasStore;
            _storeRealXV222 = storeRealX;
            _storeRealYV222 = storeRealY;
            _storeDepositRealXV225 = storeDepositRealX;
            _storeDepositRealYV225 = storeDepositRealY;
            _storeWorldV222 = storeWorld;
            _storeConcentratorPathV228 = CloneRealPathV228LikeOriginal(storeConcentratorPathV228);
            _storeBornPathV228 = CloneRealPathV228LikeOriginal(storeBornPathV228);
            _takeResourceV222 = Unit != null;
            _active = Unit != null;
            if (!preserveCarriedResourceV349)
            {
                _cycleV222 = 0;
                _carriedResourceAmountV348 = 0;
                _resourceWorkCyclesV348 = 0;
            }
            _phase = 0.0f;
            _until = Time.realtimeSinceStartup + 999999.0f;
            _nextLogV222 = 0.0f;

            if (_takeResourceV222)
            {
                MoveToResourceV222LikeOriginal("begin");
            }
        }

        public void BindSettlementFieldV342LikeOriginal(
            C2SettlementFieldPatchV342LikeOriginal patch,
            C2SettlementDipVillageV336LikeOriginal village)
        {
            if (_settlementFieldV342 != null && _settlementFieldV342 != patch)
                _settlementFieldV342.ReleaseReservationV342LikeOriginal(Unit);
            _settlementFieldV342 = patch;
            _settlementFieldVillageV342 = patch != null ? village : null;
        }

        public void CancelForExternalOrderLikeOriginal(string reason)
        {
            if (!_active && !_takeResourceV222 && !_pendingTakeResourceV227)
                return;

            // A Cossacks II unit has one LocalOrder chain.  A build order replaces
            // the current resource/task order; leaving this component active made
            // its Update() reissue resource movement or WORK after the builder had
            // already received its BUILDPOINT destination.
            StopResourceWorkV222LikeOriginal();
            SetCarryResourceMotionV223LikeOriginal(false);
            SetHiddenInsideStoreV345LikeOriginal(false);

            _active = false;
            _takeResourceV222 = false;
            _resourcePhaseV222 = ResourcePhaseV222.None;
            _pendingTakeResourceV227 = false;
            _pendingStoreConcentratorPathV228 = null;
            _pendingStoreBornPathV228 = null;
            _storeConcentratorPathV228 = null;
            _storeBornPathV228 = null;
            _phase = 0.0f;
            _until = 0.0f;
            if (_settlementFieldV342 != null)
                _settlementFieldV342.ReleaseReservationV342LikeOriginal(Unit);
            _settlementFieldV342 = null;
            _settlementFieldVillageV342 = null;
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(Unit);

            Debug.Log("[C2:UNIT TASK CANCEL] unit='" +
                      (Unit != null ? (Unit.SourceMonsterId ?? string.Empty) : "<null>") +
                      "' reason='" + (reason ?? string.Empty) + "'");
        }

        private void Update()
        {
            if (!_active || Unit == null)
                return;

            if (_takeResourceV222)
            {
                UpdateTakeResourceV222LikeOriginal();
                UpdateDebugResourceStorePathV227LikeOriginal();
                return;
            }
            UpdateDebugResourceStorePathV227LikeOriginal();

            if (Unit.SpriteAnimator == null)
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

            Unit.SpriteAnimator.SetMovingLikeOriginal(true);
            Unit.SpriteAnimator.SetMotionStateLikeOriginal(Unit.GraphDir, false);
            _phase += Time.deltaTime * Mathf.Max(1.0f, Unit.MotionDist) * 3.0f;
            Unit.SpriteAnimator.SetWalkPathFrameLikeOriginal(_phase, Mathf.Max(1.0f, Unit.MotionDist));
        }

        private void UpdateTakeResourceV222LikeOriginal()
        {
            if (Unit == null || Unit.IsDeadLikeOriginal)
            {
                SetHiddenInsideStoreV345LikeOriginal(false);
                _active = false;
                return;
            }

            switch (_resourcePhaseV222)
            {
                case ResourcePhaseV222.MoveToResource:
                    if (DistanceToRealPointOriginalPixelsV222(ResourceRealXV222, ResourceRealYV222) <= Mathf.Max(WorkStartDistanceOriginalPixelsV222, _resourceWorkRadiusV222))
                        StartResourceWorkV222LikeOriginal();
                    break;

                case ResourcePhaseV222.WorkResource:
                    TickResourceWorkV222LikeOriginal();
                    break;

                case ResourcePhaseV222.MoveToStore:
                    if (!_hasStoreV222)
                    {
                        StopResourceOrderNoStoreV348LikeOriginal();
                    }
                    else if (DistanceToRealPointOriginalPixelsV222(_storeRealXV222, _storeRealYV222) <= StoreReachDistanceOriginalPixelsV222)
                    {
                        MoveIntoStoreV225LikeOriginal();
                    }
                    break;

                case ResourcePhaseV222.MoveIntoStore:
                    if (DistanceToRealPointOriginalPixelsV222(_storeDepositRealXV225, _storeDepositRealYV225) <= StoreEnterReachDistanceOriginalPixelsV225 ||
                        Time.realtimeSinceStartup - _phaseStartedV222 >= StoreEnterTimeoutSecondsV225)
                    {
                        StartStoreDepositV225LikeOriginal();
                    }
                    break;

                case ResourcePhaseV222.Deposit:
                    TickStoreDepositV227LikeOriginal();
                    if (Time.realtimeSinceStartup - _phaseStartedV222 >= StoreDepositSecondsV222)
                    {
                        MoveOutOfStoreV225LikeOriginal();
                    }
                    break;

                case ResourcePhaseV222.MoveOutOfStore:
                    if (DistanceToRealPointOriginalPixelsV222(_storeRealXV222, _storeRealYV222) <= StoreReachDistanceOriginalPixelsV222 ||
                        Time.realtimeSinceStartup - _phaseStartedV222 >= StoreEnterTimeoutSecondsV225)
                    {
                        SetHiddenInsideStoreV345LikeOriginal(false);
                        if (_pendingTakeResourceV227)
                        {
                            ApplyPendingTakeResourceV227LikeOriginal();
                            break;
                        }

                        _cycleV222++;
                        if (_settlementFieldVillageV342 != null)
                        {
                            // The original TakeRes order returns to the same
                            // COMPLEX sprite after unloading while it still has
                            // a FOOD source.  Only select another square after
                            // FW5 has transformed to FW6.
                            if (_settlementFieldV342 != null &&
                                _settlementFieldV342.CanContinueWorkV349LikeOriginal(Unit))
                            {
                                MoveToResourceV222LikeOriginal(
                                    "return_same_field_" + _cycleV222.ToString(CultureInfo.InvariantCulture));
                                break;
                            }
                            if (Time.realtimeSinceStartup < _nextSettlementFieldRetargetV342)
                                break;
                            if (_settlementFieldVillageV342.QueueNextFieldAfterHarvestV342LikeOriginal(this, Unit))
                                break;
                            _nextSettlementFieldRetargetV342 = Time.realtimeSinceStartup + 0.50f;
                            break;
                        }
                        MoveToResourceV222LikeOriginal("return_cycle_" + _cycleV222.ToString(CultureInfo.InvariantCulture));
                    }
                    break;
            }
        }

        private int ResourceRealXV222 { get { return _resourceOriginalXV222 << 4; } }
        private int ResourceRealYV222 { get { return _resourceOriginalYV222 << 4; } }
        private int ResourceWorkRealXV222 { get { return _resourceWorkOriginalXV222 << 4; } }
        private int ResourceWorkRealYV222 { get { return _resourceWorkOriginalYV222 << 4; } }

        private static Vector2[] CloneRealPathV228LikeOriginal(Vector2[] src)
        {
            if (src == null || src.Length == 0) return null;
            Vector2[] dst = new Vector2[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = src[i];
            return dst;
        }

        private static Vector2[] TailRealPathV228LikeOriginal(Vector2[] src)
        {
            if (src == null || src.Length <= 1) return null;
            Vector2[] dst = new Vector2[src.Length - 1];
            for (int i = 1; i < src.Length; i++) dst[i - 1] = src[i];
            return dst;
        }

        private bool TrySetPreciseUnitPathV228LikeOriginal(Vector2[] path, string source)
        {
            if (Unit == null || path == null || path.Length == 0) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = Unit.RuntimeLinkCachedLikeOriginal;
            if (link == null) return false;
            link.SetMovePathRealLikeOriginal(
                path,
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false,
                0,
                true,
                source);
            return true;
        }


        private void MoveToResourceV222LikeOriginal(string reason)
        {
            _resourcePhaseV222 = ResourcePhaseV222.MoveToResource;
            _phaseStartedV222 = Time.realtimeSinceStartup;
            _phase = 0.0f;
            StopResourceWorkV222LikeOriginal();
            SetCarryResourceMotionV223LikeOriginal(false);

            if (Unit != null)
            {
                if (Unit.OwnerMode != null)
                {
                    Unit.OwnerMode.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(Unit);
                    if (!Unit.OwnerMode.C2OriginalResourceMapV1TryReserveWorkPointForTargetLikeOriginal(
                            _resourceOriginalXV222, _resourceOriginalYV222, Unit,
                            out _resourceWorkOriginalXV222, out _resourceWorkOriginalYV222, out _))
                    {
                        int rx, ry, wx, wy, wr;
                        string rn, ra;
                        if (Unit.OwnerMode.C2OriginalResourceMapV1TryFindResourceWorkTargetLikeOriginal(
                                _resourceIdV222, _resourceOriginalXV222, _resourceOriginalYV222, 1300,
                                Unit, out rx, out ry, out wx, out wy, out wr, out rn, out ra))
                        {
                            _resourceOriginalXV222 = rx;
                            _resourceOriginalYV222 = ry;
                            _resourceWorkOriginalXV222 = wx;
                            _resourceWorkOriginalYV222 = wy;
                            _resourceWorkRadiusV222 = Mathf.Max(1, wr);
                            _resourceWorldV222 = Unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(rx, ry);
                            TargetWorld = _resourceWorldV222;
                        }
                    }
                }
                Unit.SetMoveDestinationRealLikeOriginal(
                    ResourceWorkRealXV222,
                    ResourceWorkRealYV222,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false,
                    0);
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    Unit, C2UnitOrderKindV325LikeOriginal.ResourceApproach,
                    "resource_order", ResourceNameV222 + " " + reason);
            }

            LogTakeResourceV222LikeOriginal("move_to_resource " + reason);
        }

        private void MoveToStoreV222LikeOriginal()
        {
            _resourcePhaseV222 = ResourcePhaseV222.MoveToStore;
            _phaseStartedV222 = Time.realtimeSinceStartup;
            StopResourceWorkV222LikeOriginal();
            SetCarryResourceMotionV223LikeOriginal(true);
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(Unit);

            if (Unit != null && _hasStoreV222)
            {
                Unit.SetMoveDestinationRealLikeOriginal(
                    _storeRealXV222,
                    _storeRealYV222,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false,
                    0);
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    Unit, C2UnitOrderKindV325LikeOriginal.ResourceReturn,
                    "resource_order", ResourceNameV222 + " move_to_store");
            }
            else
            {
                StopResourceOrderNoStoreV348LikeOriginal();
            }

            LogTakeResourceV222LikeOriginal("move_to_store");
        }

        private void MoveIntoStoreV225LikeOriginal()
        {
            _resourcePhaseV222 = ResourcePhaseV222.MoveIntoStore;
            _phaseStartedV222 = Time.realtimeSinceStartup;
            StopResourceWorkV222LikeOriginal();
            SetCarryResourceMotionV223LikeOriginal(true);

            if (Unit != null && _hasStoreV222)
            {
                Vector2[] enterPath = TailRealPathV228LikeOriginal(_storeConcentratorPathV228);
                if (enterPath == null || enterPath.Length == 0)
                    enterPath = new Vector2[] { new Vector2(_storeDepositRealXV225, _storeDepositRealYV225) };

                if (!TrySetPreciseUnitPathV228LikeOriginal(enterPath, "take_resource_enter_store_born_to_concentrator_v235"))
                {
                    Unit.SetMoveDestinationRealLikeOriginal(
                        _storeDepositRealXV225,
                        _storeDepositRealYV225,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        false,
                        0);
                }
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    Unit, C2UnitOrderKindV325LikeOriginal.ResourceReturn,
                    "resource_order", ResourceNameV222 + " enter_store");
            }

            LogTakeResourceV222LikeOriginal("enter_store_concentrator_precise");
        }

        private void StartStoreDepositV225LikeOriginal()
        {
            _resourcePhaseV222 = ResourcePhaseV222.Deposit;
            _phaseStartedV222 = Time.realtimeSinceStartup;
            _phase = 0.0f;
            StopResourceWorkV222LikeOriginal();
            // Keep the carry resource state during the short unload animation; clear it only when leaving the store.
            SetCarryResourceMotionV223LikeOriginal(true);
            // OBJ_EnterMine/SGP_ComeIntoBuilding removes the worker from the
            // outside object/collision lists. The task keeps ticking, but the
            // worker is neither rendered nor allowed to block the doorway.
            SetHiddenInsideStoreV345LikeOriginal(true);
            if (Unit != null && _carriedResourceAmountV348 > 0)
            {
                C2NationResourceEconomyV348LikeOriginal.AddResourceLikeOriginal(
                    Unit.CombatNationLikeOriginal, _resourceIdV222, _carriedResourceAmountV348,
                    "TakeResLink deposit unit='" + (Unit.SourceMonsterId ?? string.Empty) + "'");
                _carriedResourceAmountV348 = 0;
            }
            if (Unit != null)
            {
                if (Unit.SpriteAnimator != null)
                    Unit.SpriteAnimator.SetMovingLikeOriginal(false);
                byte dir = DirectionToRealPointV222(_storeRealXV222, _storeRealYV222);
                Unit.SetFacingDirectionLikeOriginal(dir);
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    Unit, C2UnitOrderKindV325LikeOriginal.ResourceDeposit,
                    "resource_order", ResourceNameV222 + " deposit");
            }
            TickStoreDepositV227LikeOriginal();
            LogTakeResourceV222LikeOriginal("deposit_start_unload_anim");
        }

        private void MoveOutOfStoreV225LikeOriginal()
        {
            _resourcePhaseV222 = ResourcePhaseV222.MoveOutOfStore;
            _phaseStartedV222 = Time.realtimeSinceStartup;
            StopResourceWorkV222LikeOriginal();
            SetCarryResourceMotionV223LikeOriginal(false);

            if (Unit != null && _hasStoreV222)
            {
                Vector2[] exitPath = TailRealPathV228LikeOriginal(_storeBornPathV228);
                if (exitPath == null || exitPath.Length == 0)
                    exitPath = new Vector2[] { new Vector2(_storeRealXV222, _storeRealYV222) };

                if (!TrySetPreciseUnitPathV228LikeOriginal(exitPath, "take_resource_exit_store_bornpoints_v235"))
                {
                    Unit.SetMoveDestinationRealLikeOriginal(
                        _storeRealXV222,
                        _storeRealYV222,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        false,
                        0);
                }
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    Unit, C2UnitOrderKindV325LikeOriginal.ResourceApproach,
                    "resource_order", ResourceNameV222 + " exit_store");
            }

            LogTakeResourceV222LikeOriginal("exit_store_empty_bornpoints");
        }

        private void StartResourceWorkV222LikeOriginal()
        {
            _resourcePhaseV222 = ResourcePhaseV222.WorkResource;
            _phaseStartedV222 = Time.realtimeSinceStartup;
            _phase = 0.0f;
            SetCarryResourceMotionV223LikeOriginal(false);

            byte dir = DirectionToRealPointV222(ResourceRealXV222, ResourceRealYV222);
            Unit.SetFacingDirectionLikeOriginal(dir);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                Unit, C2UnitOrderKindV325LikeOriginal.ResourceWork,
                "resource_order", ResourceNameV222 + " work");
            TickResourceWorkV222LikeOriginal();
            LogTakeResourceV222LikeOriginal("work_start");
        }

        private void TickResourceWorkV222LikeOriginal()
        {
            byte dir = DirectionToRealPointV222(ResourceRealXV222, ResourceRealYV222);
            Unit.SetFacingDirectionLikeOriginal(dir);

            C2UnitOriginalRuntimeLinkLikeOriginal link = Unit.RuntimeLinkCachedLikeOriginal;
            float beforePhase = _phase;
            int workFrames = link != null ? link.GetTakeResourceFrameCountV348LikeOriginal(_resourceIdV222) : 0;
            if (workFrames <= 0) workFrames = 12;
            if (link != null)
            {
                _phase += Time.deltaTime * ResourceWorkFpsV222;
                link.SetTakeResourceFramePhaseV222LikeOriginal(_resourceIdV222, dir, _phase, false);
            }
            else
            {
                _phase += Time.deltaTime * ResourceWorkFpsV222;
            }

            int beforeCycle = Mathf.FloorToInt(beforePhase / workFrames);
            int afterCycle = Mathf.FloorToInt(_phase / workFrames);
            if (afterCycle > beforeCycle)
            {
                int completed = Mathf.Min(4, afterCycle - beforeCycle);
                for (int i = 0; i < completed; i++)
                {
                    int gathered = 0;
                    string audit;
                    bool worked;
                    if (_settlementFieldV342 != null)
                    {
                        bool exhausted;
                        worked = _settlementFieldV342.PerformWorkV349LikeOriginal(
                            Unit, out gathered, out exhausted);
                        audit = "settlement_field_complex_rsr_stage";
                        if (worked && exhausted)
                        {
                            _settlementFieldV342.ReleaseReservationV342LikeOriginal(Unit);
                            _settlementFieldV342 = null;
                        }
                    }
                    else
                    {
                        worked = Unit.OwnerMode != null &&
                                 Unit.OwnerMode.C2OriginalResourceMapV1PerformWorkLikeOriginal(
                                     _resourceOriginalXV222, _resourceOriginalYV222, _resourceIdV222,
                                     100, out gathered, out audit);
                    }

                    if (!worked || gathered <= 0)
                    {
                        MoveToResourceV222LikeOriginal("resource_retarget_after_missing");
                        return;
                    }
                    _resourceWorkCyclesV348++;
                    _carriedResourceAmountV348 += gathered;
                    if (_carriedResourceAmountV348 >= GetMaxResourcePortionV348LikeOriginal())
                    {
                        MoveToStoreV222LikeOriginal();
                        return;
                    }
                    if (_settlementFieldV342 == null && _settlementFieldVillageV342 != null)
                    {
                        // FG24 + FW0..FW5 yield seven 10-unit work cycles.
                        // CII keeps the partial load and retargets another FOOD
                        // sprite until PORTION is reached.
                        if (_settlementFieldVillageV342.QueueNextFieldAfterHarvestV342LikeOriginal(this, Unit))
                            return;
                        if (_carriedResourceAmountV348 > 0)
                            MoveToStoreV222LikeOriginal();
                        return;
                    }
                }
            }
        }

        private int GetMaxResourcePortionV348LikeOriginal()
        {
            string md = Unit != null ? (Unit.ResolvedMd ?? string.Empty) : string.Empty;
            if (!s_resourcePortionsByMdV348.TryGetValue(md, out int[] portions))
            {
                portions = new[] { 100, 10, 100, 100, 10, 10 };
                if (C2GameplayInteractionControllerV1.TryLoadMdTextV228LikeOriginal(md, out string text, out _))
                {
                    string[] lines = text.Replace("\r", string.Empty).Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (!line.StartsWith("PORTION ", StringComparison.OrdinalIgnoreCase)) continue;
                        int comment = line.IndexOf("//", StringComparison.Ordinal);
                        if (comment >= 0) line = line.Substring(0, comment);
                        string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        for (int n = 2; n + 1 < p.Length; n += 2)
                        {
                            byte rid = OriginalResourceIdFromNameV348LikeOriginal(p[n]);
                            if (rid < 6 && int.TryParse(p[n + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                                portions[rid] = Mathf.Max(1, value);
                        }
                        break;
                    }
                }
                s_resourcePortionsByMdV348[md] = portions;
            }
            return portions[Mathf.Clamp(_resourceIdV222, (byte)0, (byte)5)];
        }

        private static byte OriginalResourceIdFromNameV348LikeOriginal(string name)
        {
            switch ((name ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "WOOD": return 0;
                case "GOLD": return 1;
                case "STONE": return 2;
                case "FOOD": return 3;
                case "IRON": return 4;
                case "COAL": return 5;
                default: return 0xFF;
            }
        }

        private void StopResourceOrderNoStoreV348LikeOriginal()
        {
            StopResourceWorkV222LikeOriginal();
            SetCarryResourceMotionV223LikeOriginal(false);
            _active = false;
            _takeResourceV222 = false;
            _resourcePhaseV222 = ResourcePhaseV222.None;
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(Unit);
        }

        private void TickStoreDepositV227LikeOriginal()
        {
            if (Unit == null) return;

            byte dir = DirectionToRealPointV222(_storeRealXV222, _storeRealYV222);
            Unit.SetFacingDirectionLikeOriginal(dir);

            C2UnitOriginalRuntimeLinkLikeOriginal link = Unit.RuntimeLinkCachedLikeOriginal;
            if (link != null)
            {
                _phase += Time.deltaTime * ResourceWorkFpsV222;
                link.SetTakeResourceDepositFramePhaseV227LikeOriginal(_resourceIdV222, dir, _phase, false);
            }
        }

        private bool IsCarryingResourceToStoreV227LikeOriginal()
        {
            return _takeResourceV222 &&
                   (_resourcePhaseV222 == ResourcePhaseV222.MoveToStore ||
                    _resourcePhaseV222 == ResourcePhaseV222.MoveIntoStore ||
                    _resourcePhaseV222 == ResourcePhaseV222.Deposit);
        }

        private void QueuePendingTakeResourceV227LikeOriginal(
            C2GameplayTargetKindV1 kind,
            byte resourceId,
            int resourceOriginalX,
            int resourceOriginalY,
            int resourceWorkRadius,
            Vector3 resourceWorld,
            bool hasStore,
            int storeRealX,
            int storeRealY,
            int storeDepositRealX,
            int storeDepositRealY,
            Vector3 storeWorld,
            Vector2[] storeConcentratorPathV228,
            Vector2[] storeBornPathV228)
        {
            _pendingTakeResourceV227 = true;
            _pendingKindV227 = kind;
            _pendingResourceIdV227 = resourceId;
            _pendingResourceOriginalXV227 = resourceOriginalX;
            _pendingResourceOriginalYV227 = resourceOriginalY;
            _pendingResourceWorkRadiusV227 = resourceWorkRadius;
            _pendingResourceWorldV227 = resourceWorld;
            _pendingHasStoreV227 = hasStore;
            _pendingStoreRealXV227 = storeRealX;
            _pendingStoreRealYV227 = storeRealY;
            _pendingStoreDepositRealXV227 = storeDepositRealX;
            _pendingStoreDepositRealYV227 = storeDepositRealY;
            _pendingStoreWorldV227 = storeWorld;
            _pendingStoreConcentratorPathV228 = CloneRealPathV228LikeOriginal(storeConcentratorPathV228);
            _pendingStoreBornPathV228 = CloneRealPathV228LikeOriginal(storeBornPathV228);
        }

        private void ApplyPendingTakeResourceV227LikeOriginal()
        {
            if (!_pendingTakeResourceV227)
                return;

            C2GameplayTargetKindV1 kind = _pendingKindV227;
            byte resourceId = _pendingResourceIdV227;
            int resourceOriginalX = _pendingResourceOriginalXV227;
            int resourceOriginalY = _pendingResourceOriginalYV227;
            int resourceWorkRadius = _pendingResourceWorkRadiusV227;
            Vector3 resourceWorld = _pendingResourceWorldV227;
            bool hasStore = _pendingHasStoreV227;
            int storeRealX = _pendingStoreRealXV227;
            int storeRealY = _pendingStoreRealYV227;
            int storeDepositRealX = _pendingStoreDepositRealXV227;
            int storeDepositRealY = _pendingStoreDepositRealYV227;
            Vector3 storeWorld = _pendingStoreWorldV227;
            Vector2[] storeConcentratorPathV228 = CloneRealPathV228LikeOriginal(_pendingStoreConcentratorPathV228);
            Vector2[] storeBornPathV228 = CloneRealPathV228LikeOriginal(_pendingStoreBornPathV228);

            _pendingTakeResourceV227 = false;
            _pendingStoreConcentratorPathV228 = null;
            _pendingStoreBornPathV228 = null;

            TaskKind = kind;
            TargetWorld = resourceWorld;
            _resourceIdV222 = resourceId;
            _resourceOriginalXV222 = resourceOriginalX;
            _resourceOriginalYV222 = resourceOriginalY;
            _resourceWorkOriginalXV222 = resourceOriginalX;
            _resourceWorkOriginalYV222 = resourceOriginalY;
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1TryReserveWorkPointForTargetLikeOriginal(
                    resourceOriginalX, resourceOriginalY, Unit,
                    out _resourceWorkOriginalXV222, out _resourceWorkOriginalYV222, out _);
            _resourceWorkRadiusV222 = Mathf.Max(1, resourceWorkRadius);
            _resourceWorldV222 = resourceWorld;
            _hasStoreV222 = hasStore;
            _storeRealXV222 = storeRealX;
            _storeRealYV222 = storeRealY;
            _storeDepositRealXV225 = storeDepositRealX;
            _storeDepositRealYV225 = storeDepositRealY;
            _storeWorldV222 = storeWorld;
            _storeConcentratorPathV228 = CloneRealPathV228LikeOriginal(storeConcentratorPathV228);
            _storeBornPathV228 = CloneRealPathV228LikeOriginal(storeBornPathV228);
            _takeResourceV222 = Unit != null;
            _active = Unit != null;
            _cycleV222 = 0;
            _phase = 0.0f;
            _until = Time.realtimeSinceStartup + 999999.0f;

            if (_takeResourceV222)
                MoveToResourceV222LikeOriginal("pending_after_deposit");
        }

        internal bool C2TryGetResourcePeasantAssignmentV384ALikeOriginal(
            out int nation, out int resourceId, out bool settlementWorker)
        {
            nation = -1;
            resourceId = -1;
            settlementWorker = false;
            if (!_takeResourceV222 || Unit == null || Unit.IsDeadLikeOriginal) return false;
            if (_resourceIdV222 >= 6) return false;
            nation = Unit.CombatNationLikeOriginal;
            resourceId = _resourceIdV222;
            settlementWorker = Unit.SettlementAiControlledLikeOriginal;
            return nation >= 0 && nation < 8;
        }

        private void StopResourceWorkV222LikeOriginal()
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = Unit != null ? Unit.RuntimeLinkCachedLikeOriginal : null;
            if (link != null) link.StopTakeResourceWorkV222LikeOriginal();
        }

        private void SetCarryResourceMotionV223LikeOriginal(bool carrying)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = Unit != null ? Unit.RuntimeLinkCachedLikeOriginal : null;
            if (link != null) link.SetCarryResourceMotionV223LikeOriginal(_resourceIdV222, carrying);
        }

        private void SetHiddenInsideStoreV345LikeOriginal(bool hidden)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = Unit != null
                ? Unit.RuntimeLinkCachedLikeOriginal
                : null;
            if (link != null) link.SetHiddenInsideBuildingLikeOriginal(hidden);
        }

        private void OnDisable()
        {
            SetHiddenInsideStoreV345LikeOriginal(false);
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(Unit);
        }

        private void OnDestroy()
        {
            SetHiddenInsideStoreV345LikeOriginal(false);
            if (Unit != null && Unit.OwnerMode != null)
                Unit.OwnerMode.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(Unit);
        }

        private float DistanceToRealPointOriginalPixelsV222(float realX, float realY)
        {
            if (Unit == null) return float.MaxValue;
            float ux = Unit.RealXFloat != 0.0f ? Unit.RealXFloat : Unit.RealX;
            float uy = Unit.RealYFloat != 0.0f ? Unit.RealYFloat : Unit.RealY;
            float dx = (realX - ux) / 16.0f;
            float dy = (realY - uy) / 16.0f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private byte DirectionToRealPointV222(float realX, float realY)
        {
            if (Unit == null) return 0;
            float ux = Unit.RealXFloat != 0.0f ? Unit.RealXFloat : Unit.RealX;
            float uy = Unit.RealYFloat != 0.0f ? Unit.RealYFloat : Unit.RealY;
            Vector3 d = new Vector3((realX - ux) / 16.0f, 0.0f, -(realY - uy) / 16.0f);
            return DirectionFromWorldDelta(d);
        }

        private string ResourceNameV222
        {
            get
            {
                if (_resourceIdV222 == C2BattleTerrainMode.C2OriginalResourceWoodV1LikeOriginal) return "WOOD";
                if (_resourceIdV222 == C2BattleTerrainMode.C2OriginalResourceStoneV1LikeOriginal) return "STONE";
                if (_resourceIdV222 == C2BattleTerrainMode.C2OriginalResourceFoodV1LikeOriginal) return "FOOD";
                return "RES" + _resourceIdV222.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void LogTakeResourceV222LikeOriginal(string eventName)
        {
            // This diagnostic used to emit once per worker per second.  Large
            // settlements therefore spent measurable main-thread time formatting
            // strings and flushing Editor.log even when no debug overlay was active.
            if (!LogResourceTaskEventsV343LikeOriginal) return;
            if (Time.realtimeSinceStartup < _nextLogV222 && eventName.IndexOf("begin", StringComparison.OrdinalIgnoreCase) < 0)
                return;
            _nextLogV222 = Time.realtimeSinceStartup + 1.0f;

            Debug.Log("[C2:TAKE RESOURCE V235] event='" + eventName + "'" +
                      " unit='" + (Unit != null ? Unit.SourceMonsterId : "") + "'" +
                      " md='" + (Unit != null ? Unit.ResolvedMd : "") + "'" +
                      " res=" + ResourceNameV222 +
                      " resourcePix=(" + _resourceOriginalXV222.ToString(CultureInfo.InvariantCulture) + "," + _resourceOriginalYV222.ToString(CultureInfo.InvariantCulture) + ")" +
                      " store=" + (_hasStoreV222 ? (_storeRealXV222.ToString(CultureInfo.InvariantCulture) + "," + _storeRealYV222.ToString(CultureInfo.InvariantCulture)) : "missing") +
                      " cycle=" + _cycleV222.ToString(CultureInfo.InvariantCulture));
        }

        public static void ToggleDebugResourceStorePathsV227LikeOriginal()
        {
            // V231: Q is hard clear only. The V227-V229 debug overlay is unsafe and stays disabled.
            HardClearBrokenCyanDebugOverlayV231LikeOriginal(true);
        }

        public static void HardClearBrokenCyanDebugOverlayV231LikeOriginal(bool log)
        {
            // V232: the cyan garbage can exist even without Q, because old V227-V229 objects were
            // hidden DontSave LineRenderers and can survive outside normal FindObjectsOfType<GameObject>().
            // Use Resources.FindObjectsOfTypeAll and kill by component/material, not only by object name.
            DebugResourceStorePathsV227LikeOriginal = false;

            C2GameplayUnitTaskV1[] tasks = Resources.FindObjectsOfTypeAll<C2GameplayUnitTaskV1>();
            for (int i = 0; tasks != null && i < tasks.Length; i++)
            {
                if (tasks[i] == null) continue;
                tasks[i].HideDebugResourceStorePathV227LikeOriginal();
                tasks[i].DestroyOwnDebugResourceStorePathV231LikeOriginal();
            }

            int destroyed = DestroyAllResourceStoreDebugObjectsV231LikeOriginal();

            if (log)
                Debug.Log("[C2:TAKE RESOURCE V232] broken_cyan_debug_overlay_killed hard_clear_all=1 destroyed=" +
                          destroyed.ToString(CultureInfo.InvariantCulture) +
                          " q_not_required=1 mode=Resources.FindObjectsOfTypeAll line_and_renderer_sweep gameplay_untouched=1");
        }

        private static int DestroyAllResourceStoreDebugObjectsV231LikeOriginal()
        {
            int destroyed = 0;
            HashSet<GameObject> killed = new HashSet<GameObject>();

            LineRenderer[] allLines = Resources.FindObjectsOfTypeAll<LineRenderer>();
            for (int i = 0; allLines != null && i < allLines.Length; i++)
            {
                LineRenderer lr = allLines[i];
                if (lr == null || lr.gameObject == null) continue;
                if (!IsBrokenCyanDebugLineRendererV232LikeOriginal(lr)) continue;

                KillBrokenCyanDebugObjectV232LikeOriginal(lr.gameObject, killed, ref destroyed);
            }

            Renderer[] allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; allRenderers != null && i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r == null || r.gameObject == null) continue;
                if (r is LineRenderer) continue;
                if (!IsBrokenCyanDebugRendererV232LikeOriginal(r)) continue;

                KillBrokenCyanDebugObjectV232LikeOriginal(r.gameObject, killed, ref destroyed);
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; allObjects != null && i < allObjects.Length; i++)
            {
                GameObject go = allObjects[i];
                if (go == null) continue;
                if (!IsBrokenCyanDebugObjectV231LikeOriginal(go)) continue;

                KillBrokenCyanDebugObjectV232LikeOriginal(go, killed, ref destroyed);
            }

            return destroyed;
        }

        private static void KillBrokenCyanDebugObjectV232LikeOriginal(GameObject go, HashSet<GameObject> killed, ref int destroyed)
        {
            if (go == null || killed == null || killed.Contains(go))
                return;

            killed.Add(go);

            LineRenderer[] lines = go.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                lines[i].enabled = false;
                lines[i].positionCount = 0;
                lines[i].startWidth = 0.0f;
                lines[i].endWidth = 0.0f;
            }

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; renderers != null && i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].enabled = false;
            }

            go.SetActive(false);

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(go);
            else
                UnityEngine.Object.DestroyImmediate(go);

            destroyed++;
        }

        private static bool IsBrokenCyanDebugObjectV231LikeOriginal(GameObject go)
        {
            if (go == null) return false;

            string n = go.name ?? string.Empty;
            if (n.IndexOf("C2_TAKE_RESOURCE_STORE_PATH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("C2_TAKE_RESOURCE_MD_PATH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("TAKE_RESOURCE_STORE_PATH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("TAKE_RESOURCE_MD_PATH", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            LineRenderer lr = go.GetComponent<LineRenderer>();
            if (lr != null && IsBrokenCyanDebugLineRendererV232LikeOriginal(lr))
                return true;

            Renderer r = go.GetComponent<Renderer>();
            return r != null && IsBrokenCyanDebugRendererV232LikeOriginal(r);
        }

        private static bool IsBrokenCyanDebugLineRendererV232LikeOriginal(LineRenderer lr)
        {
            if (lr == null || lr.gameObject == null) return false;

            string n = lr.gameObject.name ?? string.Empty;
            if (n.IndexOf("C2_TAKE_RESOURCE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("TAKE_RESOURCE", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            bool cyan = IsCyanDebugColorV232LikeOriginal(lr.startColor) ||
                        IsCyanDebugColorV232LikeOriginal(lr.endColor);

            if (!cyan && lr.sharedMaterial != null)
                cyan = IsCyanDebugMaterialV232LikeOriginal(lr.sharedMaterial);
            // V234: never touch Renderer.material/LineRenderer.material while sweeping Resources.FindObjectsOfTypeAll; prefab assets throw here.
            // sharedMaterial is enough for identifying old debug line renderers.
            if (!cyan && lr.sharedMaterial != null)
                cyan = IsCyanDebugMaterialV232LikeOriginal(lr.sharedMaterial);

            if (!cyan) return false;

            // The broken overlay is a big turquoise route fan, not a normal tiny marker.
            if (lr.startWidth >= 0.5f || lr.endWidth >= 0.5f || lr.positionCount >= 2)
                return true;

            string lower = n.ToLowerInvariant();
            return lower.Contains("concentrator") || lower.Contains("bornpoints") || lower.Contains("store") || lower.Contains("path") || lower.Contains("resource");
        }

        private static bool IsBrokenCyanDebugRendererV232LikeOriginal(Renderer r)
        {
            if (r == null || r.gameObject == null) return false;

            string n = r.gameObject.name ?? string.Empty;
            bool nameLooksLikeDebug =
                n.IndexOf("C2_TAKE_RESOURCE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("TAKE_RESOURCE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("STORE_PATH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("MD_PATH", StringComparison.OrdinalIgnoreCase) >= 0;

            bool cyan = false;
            if (r.sharedMaterial != null)
                cyan = IsCyanDebugMaterialV232LikeOriginal(r.sharedMaterial);
            // V234: Renderer.material is forbidden on prefab assets found by Resources.FindObjectsOfTypeAll.
            // Use sharedMaterial only; no material instantiation, no prefab exception spam.
            if (!cyan && r.sharedMaterial != null)
                cyan = IsCyanDebugMaterialV232LikeOriginal(r.sharedMaterial);

            return cyan && nameLooksLikeDebug;
        }

        private static bool IsCyanDebugColorV232LikeOriginal(Color c)
        {
            return c.r <= 0.25f && c.g >= 0.65f && c.b >= 0.65f && c.a >= 0.25f;
        }

        private static bool IsCyanDebugMaterialV232LikeOriginal(Material m)
        {
            if (m == null) return false;

            try
            {
                if (m.HasProperty("_Color") && IsCyanDebugColorV232LikeOriginal(m.color))
                    return true;
                if (m.HasProperty("_BaseColor") && IsCyanDebugColorV232LikeOriginal(m.GetColor("_BaseColor")))
                    return true;
                if (m.HasProperty("_TintColor") && IsCyanDebugColorV232LikeOriginal(m.GetColor("_TintColor")))
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        private void UpdateDebugResourceStorePathV227LikeOriginal()
        {
            // V230: disabled. The previous implementation rendered one cyan overlay per worker task.
            // That made a massive blue fan and also confused the real resource path test.
            HideDebugResourceStorePathV227LikeOriginal();
            return;
        }

        private void SetDebugLineV229LikeOriginal(LineRenderer lr, Vector2[] realPath)
        {
            if (lr == null) return;
            if (realPath == null || realPath.Length == 0)
            {
                lr.enabled = false;
                lr.positionCount = 0;
                return;
            }

            lr.positionCount = realPath.Length;
            for (int i = 0; i < realPath.Length; i++)
                lr.SetPosition(i, RealToWorldDebugV227LikeOriginal(Mathf.RoundToInt(realPath[i].x), Mathf.RoundToInt(realPath[i].y)));
            lr.enabled = true;
        }

        private void EnsureDebugResourceStorePathV227LikeOriginal()
        {
            if (_debugPathLineV227 != null && _debugBornPathLineV229 != null)
                return;

            if (_debugPathGoV227 == null)
            {
                _debugPathGoV227 = new GameObject("C2_TAKE_RESOURCE_MD_PATH_V229_" + (Unit != null ? (Unit.SourceMonsterId ?? "unit") : "unit"));
                _debugPathGoV227.hideFlags = HideFlags.DontSave;
            }

            if (_debugPathLineV227 == null)
                _debugPathLineV227 = CreateDebugLineRendererV229LikeOriginal(_debugPathGoV227, "CONCENTRATOR");

            if (_debugBornPathLineV229 == null)
            {
                GameObject bornGo = new GameObject("C2_TAKE_RESOURCE_MD_PATH_V229_BORN_" + (Unit != null ? (Unit.SourceMonsterId ?? "unit") : "unit"));
                bornGo.hideFlags = HideFlags.DontSave;
                bornGo.transform.SetParent(_debugPathGoV227.transform, false);
                _debugBornPathLineV229 = CreateDebugLineRendererV229LikeOriginal(bornGo, "BORNPOINTS");
            }
        }

        private LineRenderer CreateDebugLineRendererV229LikeOriginal(GameObject go, string suffix)
        {
            if (go == null) return null;

            LineRenderer lr = go.GetComponent<LineRenderer>();
            if (lr == null) lr = go.AddComponent<LineRenderer>();

            lr.useWorldSpace = true;
            lr.positionCount = 0;
            lr.startWidth = 10.0f;
            lr.endWidth = 10.0f;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.startColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
            lr.endColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material m = new Material(shader);
                m.hideFlags = HideFlags.DontSave;
                m.color = new Color(0.0f, 1.0f, 1.0f, 1.0f);
                lr.sharedMaterial = m;
            }
            return lr;
        }

        private void HideDebugResourceStorePathV227LikeOriginal()
        {
            if (_debugPathLineV227 != null)
            {
                _debugPathLineV227.enabled = false;
                _debugPathLineV227.positionCount = 0;
            }
            if (_debugBornPathLineV229 != null)
            {
                _debugBornPathLineV229.enabled = false;
                _debugBornPathLineV229.positionCount = 0;
            }
        }

        private void DestroyOwnDebugResourceStorePathV231LikeOriginal()
        {
            if (_debugPathGoV227 != null)
                UnityEngine.Object.Destroy(_debugPathGoV227);

            if (_debugBornPathLineV229 != null && _debugBornPathLineV229.gameObject != null)
                UnityEngine.Object.Destroy(_debugBornPathLineV229.gameObject);

            _debugPathGoV227 = null;
            _debugPathLineV227 = null;
            _debugBornPathLineV229 = null;
        }

        private Vector3 RealToWorldDebugV227LikeOriginal(int realX, int realY)
        {
            Vector3 w;
            if (Unit != null && Unit.OwnerMode != null)
                w = Unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(realX >> 4, realY >> 4);
            else if (Unit != null)
                w = Unit.transform.position;
            else
                w = Vector3.zero;

            w.y += 18.0f;
            return w;
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
        private const float StorehouseApproachRadiusOriginalPixelsV224 = 180.0f;
        private static C2GameplayInteractionControllerV1 _active;
        private static float _nextBrokenCyanDebugSweepV231;
        private static Material _storeMdPathMaterialV233;

        private bool _storeMdPathsVisibleV233;
        private GameObject _storeMdPathsRootV233;
        private readonly List<LineRenderer> _storeMdPathLinesV233 = new List<LineRenderer>(32);

        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _selected = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);
        private C2NeutralPeasantUnitInfoV2LikeOriginal[] _allUnitsCached = Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private C2SettlementBuildingSelectableV1LikeOriginal[] _allBuildingsCached = Array.Empty<C2SettlementBuildingSelectableV1LikeOriginal>();
        private Camera[] _pickCamerasCached = Array.Empty<Camera>();
        private float _nextColliderScan;
        private float _nextLog;
        private float _nextUnitScan;
        private int _lastSelectionRevisionV346 = -1;
        private float _nextBuildingScan;
        private float _nextCameraScan;
        private bool _hasBattleUnitsCached;
        private C2BattleTerrainMode _cachedMode;
        private C2UnitOriginalRuntimeAndRendererV1 _unitRenderCoreV375;
        public int LastHoverUnitCandidatesV375LikeOriginal { get; private set; }
        private float _nextModeLookup;
        private bool _loggedColliderDisabled;
        private bool _loggedNoPhysicsHover;
        private C2GameplayTargetKindV1 _hoverKind;
        private Vector3 _hoverWorld;
        private string _hoverSource = string.Empty;
        private C2SettlementBuildingSelectableV1LikeOriginal _hoverBuilding;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _hoverUnit;
        private C2RuntimeConstructionSiteProxyLikeOriginal _hoverConstruction;
        private byte _hoverResourceIdV222 = C2BattleTerrainMode.C2OriginalResourceEmptyV1LikeOriginal;
        private int _hoverResourceOriginalXV222;
        private int _hoverResourceOriginalYV222;
        private int _hoverResourceWorkRadiusV222;
        private string _hoverResourceNameV222 = string.Empty;
        private bool _formationFacingDragActiveV321;
        private Vector2 _formationFacingDragStartScreenV321;
        // V354: Unity Editor/InputSystem mouse coordinates and Camera.WorldToScreenPoint
        // are not guaranteed to share the same origin. mapa.cpp compares the release
        // cursor against the projected press map point in one screen coordinate system.
        // Calibrate that projection to the actual input coordinates at RMB-down, then
        // reuse the same offset for preview and release. With a stationary camera and
        // cursor this guarantees rx=ry=0 exactly, as in the original.
        private Vector2 _formationFacingProjectionToInputOffsetV354;
        private Camera _formationFacingProjectionCameraV354;
        private Vector3 _formationFacingDragDestinationWorldV321;
        private bool _formationFacingPressFromMinimapV381;
        private int _formationFacingPressCursorPtrV352;
        private C2GameplayTargetKindV1 _formationFacingPressHoverKindV352;
        private byte _formationFacingPressResourceIdV352 = C2BattleTerrainMode.C2OriginalResourceEmptyV1LikeOriginal;
        private int _formationFacingPressResourceXV352;
        private int _formationFacingPressResourceYV352;
        private Vector3 _formationFacingCursorGroundWorldV337;
        private GameObject _formationFacingPreviewRootV322;
        private Mesh _formationFacingPreviewMeshV322;
        private Material _formationFacingPreviewMaterialV322;
        private GameObject _formationSlotPreviewRootV323;
        private Mesh _formationSlotPreviewMeshV323;
        private Material _formationSlotPreviewMaterialV323;
        private Canvas _formationCommandPreviewCanvasV324;
        private C2FormationCommandPreviewGraphicV324LikeOriginal _formationCommandPreviewGraphicV324;
        private bool _formationCommandPreviewAuditLoggedV324;

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
            // V231: kill stale cyan LineRenderer debug objects from V227-V229 even if they survived scene reload.
            C2GameplayUnitTaskV1.HardClearBrokenCyanDebugOverlayV231LikeOriginal(true);

            // V5D: cursor is applied through Unity hardware cursor API.
            // No overlay canvas and no OnGUI draw path: this is visible in GameView and costs almost nothing.
            string audit = C2OriginalHardCursorProviderV5.PrewarmOriginalHardCursors();
            _ = audit;
        }

        private void OnDestroy()
        {
            ClearFormationFacingPreviewV322LikeOriginal();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            _cursorHidden = false;
            _lastHardwareCurPtr = int.MinValue;
            if (_active == this) _active = null;
        }

        private static readonly ProfilerMarker SelectionProfileV377 = new ProfilerMarker("C2.Interaction.SelectionV377");
        private static readonly ProfilerMarker HoverCacheProfileV377 = new ProfilerMarker("C2.Interaction.HoverCacheV377");
        private static readonly ProfilerMarker HoverProfileV377 = new ProfilerMarker("C2.Interaction.HoverV377");
        private static readonly ProfilerMarker UnitPickProfileV377 = new ProfilerMarker("C2.Interaction.UnitPickV377");
        private static readonly ProfilerMarker CommandsProfileV377 = new ProfilerMarker("C2.Interaction.CommandsV377");

        private void Update()
        {
            using (SelectionProfileV377.Auto()) RefreshSelectedCached();
            using (HoverCacheProfileV377.Auto()) RefreshHoverPickCachesLikeOriginal();
            HandleResourceDebugPathHotkeyV227LikeOriginal();

            bool hasBattleUnits = _hasBattleUnitsCached;
            if (hasBattleUnits && Time.realtimeSinceStartup >= _nextColliderScan)
            {
                _nextColliderScan = Time.realtimeSinceStartup + 3.0f;
                EnsureSceneInteractionColliders();
            }

            if (hasBattleUnits)
            {
                using (HoverProfileV377.Auto()) UpdateHover();
            }
            else
            {
                _hoverKind = C2GameplayTargetKindV1.None;
                _hoverWorld = Vector3.zero;
                _hoverSource = string.Empty;
            }

            using (CommandsProfileV377.Auto())
            {
                HandleRightClickTaskOnly();
                UpdateFormationFacingPreviewV322LikeOriginal();
                HandleFormationFacingDragV321LikeOriginal();
                UpdateOriginalHardCursor();
            }

        }

        private static void SweepBrokenCyanDebugOverlayV231LikeOriginal()
        {
            if (Time.realtimeSinceStartup < _nextBrokenCyanDebugSweepV231)
                return;

            _nextBrokenCyanDebugSweepV231 = Time.realtimeSinceStartup + 0.25f;
            C2GameplayUnitTaskV1.HardClearBrokenCyanDebugOverlayV231LikeOriginal(false);
        }

        private void HandleResourceDebugPathHotkeyV227LikeOriginal()
        {
            // V247: plain Q is now reserved for the building zones/LINESORT overlay.
            // Keep the old storehouse path debug available as Ctrl+Q only.
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                Keyboard.current.qKey.wasPressedThisFrame &&
                (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed))
                pressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q) &&
                    (UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl)))
                    pressed = true;
            }
            catch
            {
            }
#endif
            if (pressed)
                ToggleStorehouseMdPathsDebugV233LikeOriginal();
        }

        private void ToggleStorehouseMdPathsDebugV233LikeOriginal()
        {
            if (_storeMdPathsVisibleV233)
            {
                DestroyStorehouseMdPathsDebugV233LikeOriginal();
                _storeMdPathsVisibleV233 = false;
                Debug.Log("[C2:TAKE RESOURCE V235] debug_store_md_paths=False hard_clear_own=1");
                return;
            }

            // V234: do not run the broad V232 cyan sweep here. It can kill unrelated cyan debug lines.
            // Awake already clears stale V227-V232 garbage once; Q now only toggles the safe MD-path overlay.
            _storeMdPathsVisibleV233 = true;
            RebuildStorehouseMdPathsDebugV233LikeOriginal();
        }

        private void DestroyStorehouseMdPathsDebugV233LikeOriginal()
        {
            if (_storeMdPathsRootV233 != null)
            {
                if (Application.isPlaying)
                    Destroy(_storeMdPathsRootV233);
                else
                    DestroyImmediate(_storeMdPathsRootV233);
            }

            _storeMdPathsRootV233 = null;
            _storeMdPathLinesV233.Clear();
        }

        private void RebuildStorehouseMdPathsDebugV233LikeOriginal()
        {
            DestroyStorehouseMdPathsDebugV233LikeOriginal();

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (buildings == null || buildings.Length == 0)
            {
                Debug.Log("[C2:TAKE RESOURCE V235] debug_store_md_paths=True drawn=0 reason=no_buildings");
                return;
            }

            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            _storeMdPathsRootV233 = new GameObject("C2_STORE_MD_DEBUG_V234_ROOT");
            _storeMdPathsRootV233.hideFlags = HideFlags.DontSave;

            int drawnBuildings = 0;
            int drawnLines = 0;
            for (int i = 0; i < buildings.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                if (b == null || !b.isActiveAndEnabled || b.NotSelectable)
                    continue;

                if (!IsStorehouseBuildingV222LikeOriginal(b))
                    continue;

                Vector2[] concPath;
                Vector2[] bornPath;
                string audit;
                if (!TryBuildStorehouseBornConcentratorPathsV228LikeOriginal(b, out concPath, out bornPath, out audit))
                {
                    Debug.Log("[C2:TAKE RESOURCE V235] debug_store_md_paths building='" +
                              (b.SourceMonsterId ?? b.KindName ?? string.Empty) +
                              "' skipped " + audit);
                    continue;
                }

                bool any = false;
                if (concPath != null && concPath.Length >= 2)
                {
                    CreateStorehouseMdPathLineV233LikeOriginal(
                        mode,
                        "CONCENTRATOR",
                        b.SourceMonsterId ?? b.KindName ?? "store",
                        concPath);
                    drawnLines++;
                    any = true;
                }

                if (bornPath != null && bornPath.Length >= 2)
                {
                    CreateStorehouseMdPathLineV233LikeOriginal(
                        mode,
                        "BORNPOINTS",
                        b.SourceMonsterId ?? b.KindName ?? "store",
                        bornPath);
                    drawnLines++;
                    any = true;
                }

                if (any)
                {
                    drawnBuildings++;
                    Debug.Log("[C2:TAKE RESOURCE V235] debug_store_md_paths building='" +
                              (b.SourceMonsterId ?? b.KindName ?? string.Empty) +
                              "' concCount=" + (concPath != null ? concPath.Length : 0).ToString(CultureInfo.InvariantCulture) +
                              " bornCount=" + (bornPath != null ? bornPath.Length : 0).ToString(CultureInfo.InvariantCulture) +
                              " " + audit);
                }
            }

            Debug.Log("[C2:TAKE RESOURCE V235] debug_store_md_paths=True drawnBuildings=" +
                      drawnBuildings.ToString(CultureInfo.InvariantCulture) +
                      " drawnLines=" + drawnLines.ToString(CultureInfo.InvariantCulture) +
                      " mode=V235_BORNPOINTS_PLUS_CONCENTRATOR_VISIBLE");
        }

        private void CreateStorehouseMdPathLineV233LikeOriginal(
            C2BattleTerrainMode mode,
            string kind,
            string buildingName,
            Vector2[] realPath)
        {
            if (_storeMdPathsRootV233 == null || realPath == null || realPath.Length < 2)
                return;

            GameObject go = new GameObject("C2_STORE_MD_DEBUG_V234_" + kind + "_" + (buildingName ?? "store"));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(_storeMdPathsRootV233.transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = realPath.Length;
            lr.startWidth = 6.0f;
            lr.endWidth = 6.0f;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.sortingOrder = 32767;
            lr.startColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
            lr.endColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);

            if (_storeMdPathMaterialV233 == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    _storeMdPathMaterialV233 = new Material(shader);
                    _storeMdPathMaterialV233.hideFlags = HideFlags.DontSave;
                    _storeMdPathMaterialV233.color = new Color(0.0f, 1.0f, 1.0f, 1.0f);
                }
            }

            if (_storeMdPathMaterialV233 != null)
                lr.sharedMaterial = _storeMdPathMaterialV233;

            for (int i = 0; i < realPath.Length; i++)
            {
                int rx = Mathf.RoundToInt(realPath[i].x);
                int ry = Mathf.RoundToInt(realPath[i].y);
                Vector3 w = mode != null
                    ? mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(rx >> 4, ry >> 4)
                    : Vector3.zero;
                w.y += 48.0f;
                lr.SetPosition(i, w);
            }

            _storeMdPathLinesV233.Add(lr);
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

        private static bool RightMouseButtonReleasedThisFrameV321LikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButtonUp(1);
#else
            return false;
#endif
        }

        private static bool RightMouseButtonHeldV346LikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.rightButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButton(1);
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

        private static bool ShiftHeldV352LikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
#endif
            return UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
        }

        private byte ResolveMoveOrdTypeV352LikeOriginal()
        {
            // Multi.cpp::SendSelectedToXY stores Control in OneDirection, then calls
            // GroupSendSelectedTo(..., Type&7).  The movement/order chain therefore
            // receives only 0 for an ordinary command or 2 for Shift append.
            return ShiftHeldV352LikeOriginal() ? (byte)2 : (byte)0;
        }

        private void ClearSelectedMoveOrderChainsV352ForExternalOrder()
        {
            for (int i = 0; i < _selected.Count; i++)
                C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(_selected[i]);
        }

        private void HandleRightClickTaskOnly()
        {
            if (_selected.Count == 0)
                return;

            Vector2 pressScreenV381 = MousePositionBottomLeft();
            if (C2EditorRuntimeStateV333LikeOriginal.PointerOverPalette ||
                C2EditorRuntimeStateV333LikeOriginal.IsPointerOverPaletteLikeOriginal(pressScreenV381))
                return;

            // mapa.cpp: Rpressed starts StrelMode.  No attack/resource/move command is
            // executed here; the action is chosen only when realRpressed becomes false.
            if (!RightMouseButtonPressedThisFrame())
                return;

            C2BattleTerrainMode pressModeV381 = GetBattleTerrainModeCached();
            float minimapOriginalXV381 = 0f;
            float minimapOriginalYV381 = 0f;
            Vector3 minimapWorldV381 = default; // V382: definite assignment when pressModeV381 == null.
            bool pressOverMinimapV381 = pressModeV381 != null &&
                pressModeV381.C2MinimapTryScreenToOriginalPixelV381LikeOriginal(
                    pressScreenV381, out minimapOriginalXV381, out minimapOriginalYV381, out minimapWorldV381);

            // All other UI remains non-gameplay input. The minimap is the one original
            // exception: mapa.cpp maps RMB there to xmx/yreal and sends the selected
            // units/brigade without moving the camera.
            if (!pressOverMinimapV381 && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            _formationFacingDragActiveV321 = true;
            _formationFacingDragStartScreenV321 = pressScreenV381;
            _formationFacingPressFromMinimapV381 = pressOverMinimapV381;
            _formationFacingDragDestinationWorldV321 = pressOverMinimapV381
                ? minimapWorldV381
                : _formationFacingCursorGroundWorldV337;

            // mapa.cpp stores SStartX/SStartY in map space and later projects that same
            // map point back to screen space. In Unity the input mouse and camera
            // projection can have a constant GameView/editor offset, so establish the
            // exact bridge once at press time. Do NOT derive drag distance directly
            // from raw WorldToScreenPoint coordinates.
            _formationFacingProjectionToInputOffsetV354 = Vector2.zero;
            C2BattleTerrainMode pressModeV354 = GetBattleTerrainModeCached();
            Camera pressCameraV354 =
                pressModeV354 != null ? pressModeV354.GetActiveBattleCameraLikeOriginal() : Camera.main;
            _formationFacingProjectionCameraV354 = pressCameraV354;
            if (pressCameraV354 != null)
            {
                Vector3 projectedPressV354 =
                    pressCameraV354.WorldToScreenPoint(_formationFacingDragDestinationWorldV321);
                if (projectedPressV354.z > 0.0f)
                {
                    _formationFacingProjectionToInputOffsetV354 =
                        _formationFacingDragStartScreenV321 -
                        new Vector2(projectedPressV354.x, projectedPressV354.y);
                }
            }

            _formationFacingPressCursorPtrV352 = CursorPtrForHoverLikeOriginal(_hoverKind);
            _formationFacingPressHoverKindV352 = _hoverKind;
            _formationFacingPressResourceIdV352 = _hoverResourceIdV222;
            _formationFacingPressResourceXV352 = _hoverResourceOriginalXV222;
            _formationFacingPressResourceYV352 = _hoverResourceOriginalYV222;
            _formationCommandPreviewAuditLoggedV324 = false;
            EnsureFormationFacingPreviewV322LikeOriginal();
        }

        private void HandleFormationFacingDragV321LikeOriginal()
        {
            if (!_formationFacingDragActiveV321)
                return;

            if (!RightMouseButtonReleasedThisFrameV321LikeOriginal() &&
                RightMouseButtonHeldV346LikeOriginal())
                return;

            _formationFacingDragActiveV321 = false;
            ClearFormationFacingPreviewV322LikeOriginal();

            // mapa.cpp release path:
            //   VS=SkewPt(SStartX,SStartY,H); WorldToScreenSpace(VS);
            //   rx=x-VS.x; ry=(y-VS.y)*2; Nr=sqrt(rx*rx+ry*ry);
            Vector2 endScreen = MousePositionBottomLeft();
            Vector2 projectedStart = _formationFacingDragStartScreenV321;
            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            Camera projectionCamera = _formationFacingProjectionCameraV354;
            if (projectionCamera == null)
                projectionCamera = mode != null ? mode.GetActiveBattleCameraLikeOriginal() : Camera.main;
            if (projectionCamera != null)
            {
                // mapa.cpp uses WorldToScreenSpace(SkewPt(SStartX,SStartY,H)) and cursor x/y
                // in the SAME coordinate system. Convert Unity's projection into the
                // actual InputSystem coordinate space using the offset captured at press.
                Vector3 p = projectionCamera.WorldToScreenPoint(_formationFacingDragDestinationWorldV321);
                if (p.z > 0.0f)
                {
                    projectedStart =
                        new Vector2(p.x, p.y) +
                        _formationFacingProjectionToInputOffsetV354;
                }
            }

            int rx = Mathf.RoundToInt(endScreen.x - projectedStart.x);
            // Unity/InputSystem screen Y grows upward. Cossacks II mapa.cpp receives
            // Win32 screen Y, which grows downward. Convert exactly at the boundary
            // before GetDir; distance is unchanged by the sign flip.
            int unityRy = Mathf.RoundToInt((endScreen.y - projectedStart.y) * 2.0f);
            int c2Ry = -unityRy;
            int nr = C2OriginalMovementMathV352.EuclideanLengthTruncated(rx, c2Ry);

            short direct = 512;
            if (nr > 60)
            {
                direct = C2OriginalMovementMathV352.Quantize16(
                    C2OriginalMovementMathV352.GetDir(rx, c2Ry));
            }

            if (mode == null)
                return;

            float destPxX;
            float destPxY;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(
                    _formationFacingDragDestinationWorldV321, out destPxX, out destPxY))
                return;

            // A dragged RMB is always the mapa.cpp CmdSendToXY branch: the start point
            // remains the destination and DIRECT carries the requested formation facing.
            if (nr > 60)
            {
                string dragAudit;
                int dragIssued = C2GameplayLooseGroupMoveLikeOriginal.IssueMoveLikeOriginal(
                    _selected,
                    destPxX * 16.0f,
                    destPxY * 16.0f,
                    true,
                    (byte)direct,
                    ResolveMoveOrdTypeV352LikeOriginal(),
                    "mapa_strelmode_drag_release_v354",
                    out dragAudit);
                Debug.Log("[C2:RMB ORIGINAL V354] release=drag nr=" + nr.ToString(CultureInfo.InvariantCulture) +
                          " rx=" + rx.ToString(CultureInfo.InvariantCulture) +
                          " ry=" + c2Ry.ToString(CultureInfo.InvariantCulture) +
                          " unityRy=" + unityRy.ToString(CultureInfo.InvariantCulture) +
                          " direct=" + direct.ToString(CultureInfo.InvariantCulture) +
                          " input=" + (_formationFacingPressFromMinimapV381 ? "minimap" : "world") +
                          " destPx=(" + destPxX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                          destPxY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                          " issued=" + dragIssued.ToString(CultureInfo.InvariantCulture) + " " + dragAudit);
                return;
            }

            // Click action is also chosen on release.  For resources mapa.cpp additionally
            // requires stptr==curptr, so changing the cursor target while RMB is held falls
            // through to the ordinary movement branch.
            int releaseCursor = CursorPtrForHoverLikeOriginal(_hoverKind);
            bool resourceCursorStable =
                releaseCursor == _formationFacingPressCursorPtrV352 &&
                _hoverResourceIdV222 == _formationFacingPressResourceIdV352 &&
                _hoverResourceOriginalXV222 == _formationFacingPressResourceXV352 &&
                _hoverResourceOriginalYV222 == _formationFacingPressResourceYV352;

            if (_hoverKind == C2GameplayTargetKindV1.Building &&
                _hoverConstruction != null &&
                _hoverConstruction.CanAcceptBuildersLikeOriginal &&
                CanSelectedRepairLikeOriginal())
            {
                ClearSelectedMoveOrderChainsV352ForExternalOrder();
                IssueRepairConstructionOrderLikeOriginal("mapa_release_mend_v352");
                return;
            }

            bool resourceTarget = _hoverKind == C2GameplayTargetKindV1.Tree ||
                                  _hoverKind == C2GameplayTargetKindV1.Stone ||
                                  _hoverKind == C2GameplayTargetKindV1.Field;
            if ((resourceTarget && resourceCursorStable) ||
                _hoverKind == C2GameplayTargetKindV1.Enemy)
            {
                ClearSelectedMoveOrderChainsV352ForExternalOrder();
                if (TryIssueRightClickTask(_hoverKind, _hoverWorld))
                    return;
            }

            string audit;
            int issued = C2GameplayLooseGroupMoveLikeOriginal.IssueMoveLikeOriginal(
                _selected,
                destPxX * 16.0f,
                destPxY * 16.0f,
                false,
                0,
                ResolveMoveOrdTypeV352LikeOriginal(),
                "mapa_click_release_v354",
                out audit);
            Debug.Log("[C2:RMB ORIGINAL V354] release=click nr=" + nr.ToString(CultureInfo.InvariantCulture) +
                      " rx=" + rx.ToString(CultureInfo.InvariantCulture) +
                      " ry=" + c2Ry.ToString(CultureInfo.InvariantCulture) +
                      " unityRy=" + unityRy.ToString(CultureInfo.InvariantCulture) +
                      " pressKind=" + _formationFacingPressHoverKindV352.ToString() +
                      " releaseKind=" + _hoverKind.ToString() +
                      " input=" + (_formationFacingPressFromMinimapV381 ? "minimap" : "world") +
                      " destPx=(" + destPxX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      destPxY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " issued=" + issued.ToString(CultureInfo.InvariantCulture) + " " + audit);
        }

        private void EnsureFormationFacingPreviewV322LikeOriginal()
        {
            if (_formationCommandPreviewGraphicV324 != null) return;
            GameObject canvasGo = GameObject.Find("GameplayHud_Canvas_V14_AiDatFlagsMdPorts");
            _formationCommandPreviewCanvasV324 =
                canvasGo != null ? canvasGo.GetComponent<Canvas>() : null;
            if (_formationCommandPreviewCanvasV324 == null)
                return;

            GameObject previewRoot = new GameObject("GameplayHud_Formation_Command_Preview_V326");
            previewRoot.hideFlags = HideFlags.DontSave;
            previewRoot.layer = canvasGo.layer;
            previewRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform previewRootRect = previewRoot.AddComponent<RectTransform>();
            previewRootRect.anchorMin = Vector2.zero;
            previewRootRect.anchorMax = Vector2.one;
            previewRootRect.pivot = new Vector2(0.5f, 0.5f);
            previewRootRect.offsetMin = Vector2.zero;
            previewRootRect.offsetMax = Vector2.zero;
            previewRoot.transform.SetAsLastSibling();
            _formationFacingPreviewRootV322 = previewRoot;

            GameObject graphicGo = new GameObject("C2_Formation_Command_Preview_Graphic_V324");
            graphicGo.layer = canvasGo.layer;
            graphicGo.transform.SetParent(previewRoot.transform, false);
            RectTransform rt = graphicGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _formationCommandPreviewGraphicV324 =
                graphicGo.AddComponent<C2FormationCommandPreviewGraphicV324LikeOriginal>();
            _formationCommandPreviewGraphicV324.raycastTarget = false;
            _formationCommandPreviewGraphicV324.color = Color.white;
            CanvasRenderer previewRenderer = graphicGo.GetComponent<CanvasRenderer>();
            if (previewRenderer != null)
                previewRenderer.cullTransparentMesh = false;
            return;

#pragma warning disable 162
            _formationFacingPreviewRootV322 = new GameObject("C2_Formation_Direction_Grid_V322");
            _formationFacingPreviewRootV322.hideFlags = HideFlags.DontSave;
            MeshFilter filter = _formationFacingPreviewRootV322.AddComponent<MeshFilter>();
            MeshRenderer renderer = _formationFacingPreviewRootV322.AddComponent<MeshRenderer>();
            _formationFacingPreviewMeshV322 = new Mesh();
            _formationFacingPreviewMeshV322.name = "C2_Formation_Direction_Grid_V322_Mesh";
            filter.sharedMesh = _formationFacingPreviewMeshV322;
            Shader shader = Shader.Find("C2/UnitSelectionRingAlways");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            _formationFacingPreviewMaterialV322 = new Material(shader);
            _formationFacingPreviewMaterialV322.hideFlags = HideFlags.DontSave;
            _formationFacingPreviewMaterialV322.color = new Color(1.0f, 0.86f, 0.18f, 0.95f);
            if (_formationFacingPreviewMaterialV322.HasProperty("_Color"))
                _formationFacingPreviewMaterialV322.SetColor("_Color", new Color(1.0f, 0.86f, 0.18f, 0.95f));
            if (_formationFacingPreviewMaterialV322.HasProperty("_MainTex"))
                _formationFacingPreviewMaterialV322.SetTexture("_MainTex", Texture2D.whiteTexture);
            _formationFacingPreviewMaterialV322.mainTexture = Texture2D.whiteTexture;
            if (_formationFacingPreviewMaterialV322.HasProperty("_ZTest"))
                _formationFacingPreviewMaterialV322.SetInt("_ZTest", 8);
            if (_formationFacingPreviewMaterialV322.HasProperty("_ZWrite"))
                _formationFacingPreviewMaterialV322.SetInt("_ZWrite", 0);
            _formationFacingPreviewMaterialV322.renderQueue = 5002;
            renderer.sharedMaterial = _formationFacingPreviewMaterialV322;
            renderer.sortingOrder = 32760;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _formationSlotPreviewRootV323 = new GameObject("C2_Formation_Destination_BrigRound_V323");
            _formationSlotPreviewRootV323.transform.SetParent(_formationFacingPreviewRootV322.transform, false);
            MeshFilter slotFilter = _formationSlotPreviewRootV323.AddComponent<MeshFilter>();
            MeshRenderer slotRenderer = _formationSlotPreviewRootV323.AddComponent<MeshRenderer>();
            _formationSlotPreviewMeshV323 = new Mesh();
            _formationSlotPreviewMeshV323.name = "C2_Formation_Destination_BrigRound_V323_Mesh";
            slotFilter.sharedMesh = _formationSlotPreviewMeshV323;
            Shader slotShader = Shader.Find("C2/UnitSelectionRingAlways");
            if (slotShader == null) slotShader = Shader.Find("Sprites/Default");
            _formationSlotPreviewMaterialV323 = new Material(slotShader);
            _formationSlotPreviewMaterialV323.hideFlags = HideFlags.DontSave;
            Texture2D brigRound = Resources.Load<Texture2D>("textures/selection/round4");
            if (brigRound != null)
            {
                brigRound.filterMode = FilterMode.Point;
                if (_formationSlotPreviewMaterialV323.HasProperty("_MainTex"))
                    _formationSlotPreviewMaterialV323.SetTexture("_MainTex", brigRound);
                if (_formationSlotPreviewMaterialV323.HasProperty("_BaseMap"))
                    _formationSlotPreviewMaterialV323.SetTexture("_BaseMap", brigRound);
                _formationSlotPreviewMaterialV323.mainTexture = brigRound;
            }
            Color yellow = new Color(1.0f, 0.86f, 0.10f, 0.92f);
            if (_formationSlotPreviewMaterialV323.HasProperty("_Color"))
                _formationSlotPreviewMaterialV323.SetColor("_Color", yellow);
            if (_formationSlotPreviewMaterialV323.HasProperty("_BaseColor"))
                _formationSlotPreviewMaterialV323.SetColor("_BaseColor", yellow);
            if (_formationSlotPreviewMaterialV323.HasProperty("_ZTest"))
                _formationSlotPreviewMaterialV323.SetInt("_ZTest", 8);
            if (_formationSlotPreviewMaterialV323.HasProperty("_ZWrite"))
                _formationSlotPreviewMaterialV323.SetInt("_ZWrite", 0);
            _formationSlotPreviewMaterialV323.renderQueue = 5003;
            slotRenderer.sharedMaterial = _formationSlotPreviewMaterialV323;
            slotRenderer.sortingOrder = 32761;
            slotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            slotRenderer.receiveShadows = false;

            int previewLayer = C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera
                ? C2SpriteDepthLayerLikeOriginal.LayerIndex
                : 0;
            _formationFacingPreviewRootV322.layer = previewLayer;
            _formationSlotPreviewRootV323.layer = previewLayer;
#pragma warning restore 162
        }

        private void ClearFormationFacingPreviewV322LikeOriginal()
        {
            if (_formationFacingPreviewRootV322 != null)
                Destroy(_formationFacingPreviewRootV322);
            if (_formationFacingPreviewMaterialV322 != null)
                Destroy(_formationFacingPreviewMaterialV322);
            if (_formationSlotPreviewMaterialV323 != null)
                Destroy(_formationSlotPreviewMaterialV323);
            _formationFacingPreviewRootV322 = null;
            _formationFacingPreviewMeshV322 = null;
            _formationFacingPreviewMaterialV322 = null;
            _formationSlotPreviewRootV323 = null;
            _formationSlotPreviewMeshV323 = null;
            _formationSlotPreviewMaterialV323 = null;
            _formationCommandPreviewCanvasV324 = null;
            _formationCommandPreviewGraphicV324 = null;
        }

        private void UpdateFormationFacingPreviewV322LikeOriginal()
        {
            if (!_formationFacingDragActiveV321) return;
            EnsureFormationFacingPreviewV322LikeOriginal();
            if (_formationCommandPreviewGraphicV324 != null)
            {
                UpdateFormationCommandPreviewV324LikeOriginal();
                return;
            }
            if (_formationFacingPreviewMeshV322 == null) return;

            // Legacy mesh fallback must obey the same StrelDist gate as mapa.cpp.
            // The old fallback drew an arrow immediately on RMB-down even when the
            // cursor had not moved.
            Camera fallbackCameraV354 = _formationFacingProjectionCameraV354;
            if (fallbackCameraV354 == null)
            {
                C2BattleTerrainMode fallbackModeV354 = GetBattleTerrainModeCached();
                fallbackCameraV354 =
                    fallbackModeV354 != null ? fallbackModeV354.GetActiveBattleCameraLikeOriginal() : Camera.main;
            }
            Vector2 fallbackStartV354 = _formationFacingDragStartScreenV321;
            if (fallbackCameraV354 != null)
            {
                Vector3 fallbackProjectedV354 =
                    fallbackCameraV354.WorldToScreenPoint(_formationFacingDragDestinationWorldV321);
                if (fallbackProjectedV354.z > 0.0f)
                {
                    fallbackStartV354 =
                        new Vector2(fallbackProjectedV354.x, fallbackProjectedV354.y) +
                        _formationFacingProjectionToInputOffsetV354;
                }
            }
            Vector2 fallbackMouseV354 = MousePositionBottomLeft();
            int fallbackRxV354 = Mathf.RoundToInt(fallbackMouseV354.x - fallbackStartV354.x);
            int fallbackRyV354 = Mathf.RoundToInt((fallbackMouseV354.y - fallbackStartV354.y) * 2.0f);
            if (C2OriginalMovementMathV352.EuclideanLengthTruncated(
                    fallbackRxV354, fallbackRyV354) <= 60)
            {
                _formationFacingPreviewMeshV322.Clear(false);
                if (_formationSlotPreviewMeshV323 != null)
                    _formationSlotPreviewMeshV323.Clear(false);
                return;
            }

            Vector3 center = _formationFacingDragDestinationWorldV321;
            Vector3 forward = _hoverWorld - center;
            forward.y = 0.0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 side = new Vector3(-forward.z, 0.0f, forward.x);

            center.y += 3.0f;
            List<Vector3> vertices = new List<Vector3>(12);
            List<Vector2> uv = new List<Vector2>(12);
            List<int> triangles = new List<int>(18);
            Action<Vector3, Vector3> addLine = delegate(Vector3 a, Vector3 b)
            {
                Vector3 delta = b - a;
                delta.y = 0.0f;
                if (delta.sqrMagnitude < 0.0001f)
                    return;
                Vector3 normal = new Vector3(-delta.z, 0.0f, delta.x).normalized * 1.35f;
                int index = vertices.Count;
                vertices.Add(a - normal);
                vertices.Add(a + normal);
                vertices.Add(b + normal);
                vertices.Add(b - normal);
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(0, 1));
                uv.Add(new Vector2(1, 1));
                uv.Add(new Vector2(1, 0));
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index);
                triangles.Add(index + 3);
                triangles.Add(index + 2);
            };

            Vector3 tip = _hoverWorld; tip.y = center.y;
            addLine(center, tip);
            float arrow = Mathf.Clamp(Vector3.Distance(center, tip) * 0.22f, 10.0f, 28.0f);
            addLine(tip, tip - forward * arrow + side * arrow * 0.55f);
            addLine(tip, tip - forward * arrow - side * arrow * 0.55f);

            _formationFacingPreviewMeshV322.Clear(false);
            _formationFacingPreviewMeshV322.SetVertices(vertices);
            _formationFacingPreviewMeshV322.SetUVs(0, uv);
            _formationFacingPreviewMeshV322.SetTriangles(triangles, 0);
            _formationFacingPreviewMeshV322.RecalculateBounds();
            UpdateFormationDestinationMarkersV323LikeOriginal();
        }

        private void UpdateFormationCommandPreviewV324LikeOriginal()
        {
            if (_formationCommandPreviewGraphicV324 == null)
                return;

            Camera camera = _formationFacingProjectionCameraV354;
            if (camera == null || !camera.isActiveAndEnabled)
            {
                C2BattleTerrainMode previewModeV354 = GetBattleTerrainModeCached();
                camera =
                    previewModeV354 != null ? previewModeV354.GetActiveBattleCameraLikeOriginal() : Camera.main;
            }
            if (camera == null)
            {
                _formationCommandPreviewGraphicV324.SetPreviewLikeOriginal(null, null);
                return;
            }

            List<Vector2> arrow = new List<Vector2>(8);
            Vector3 start3 = camera.WorldToScreenPoint(_formationFacingDragDestinationWorldV321);
            Vector2 start =
                new Vector2(start3.x, start3.y) +
                _formationFacingProjectionToInputOffsetV354;
            Vector2 mouse = MousePositionBottomLeft();
            float rx = mouse.x - start.x;
            float unityRy = (mouse.y - start.y) * 2.0f;
            float c2Ry = -unityRy;
            float nr = Mathf.Sqrt(rx * rx + unityRy * unityRy);

            // mapa.cpp, StrelMode: exact seven-segment yellow outline.
            if (nr > 60.0f)
            {
                float ux = rx * 20.0f / nr;
                float uy = unityRy * 20.0f / nr;
                float uxt = uy;
                float uyt = -ux;
                arrow.Add(new Vector2(start.x - uxt, start.y - uyt * 0.5f));
                arrow.Add(new Vector2(start.x + uxt, start.y + uyt * 0.5f));
                arrow.Add(new Vector2(start.x + rx - 2.0f * ux + uxt, start.y + (unityRy - 2.0f * uy + uyt) * 0.5f));
                arrow.Add(new Vector2(start.x + rx - 2.0f * ux + 2.0f * uxt, start.y + (unityRy - 2.0f * uy + 2.0f * uyt) * 0.5f));
                arrow.Add(new Vector2(start.x + rx, start.y + unityRy * 0.5f));
                arrow.Add(new Vector2(start.x + rx - 2.0f * ux - 2.0f * uxt, start.y + (unityRy - 2.0f * uy - 2.0f * uyt) * 0.5f));
                arrow.Add(new Vector2(start.x + rx - 2.0f * ux - uxt, start.y + (unityRy - 2.0f * uy - uyt) * 0.5f));
                arrow.Add(arrow[0]);
            }

            List<Vector2> positions = new List<Vector2>(256);
            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            string multiPreviewAudit = "no_mode";
            if (mode != null)
            {
                float targetPxX;
                float targetPxY;
                if (mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(
                        _formationFacingDragDestinationWorldV321, out targetPxX, out targetPxY))
                {
                    byte destinationDirection = nr > 60.0f
                        ? DirectionFromOriginalScreenDeltaV325LikeOriginal(rx, c2Ry)
                        : (byte)0;
                    List<Vector2> allSlots;
                    if (C2FormationRuntimeV167LikeOriginal.TryBuildSelectedFormationPreviewV346LikeOriginal(
                            _selected,
                            targetPxX * 16.0f,
                            targetPxY * 16.0f,
                            nr > 60.0f,
                            destinationDirection,
                            out allSlots,
                            out multiPreviewAudit))
                    {
                        for (int i = 0; i < allSlots.Count; i++)
                        {
                            Vector3 slotWorld = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                                allSlots[i].x / 16.0f, allSlots[i].y / 16.0f);
                            Vector3 screen = camera.WorldToScreenPoint(slotWorld);
                            if (screen.z > 0.0f)
                                positions.Add(new Vector2(screen.x, screen.y));
                        }
                    }
                }
            }

            _formationCommandPreviewGraphicV324.SetPreviewLikeOriginal(arrow, positions);
            if (!_formationCommandPreviewAuditLoggedV324 && nr > 60.0f)
            {
                _formationCommandPreviewAuditLoggedV324 = true;
                Debug.Log(
                    "[C2:FORMATION PREVIEW V324] active=True arrowSegments=" +
                    Mathf.Max(0, arrow.Count - 1).ToString(CultureInfo.InvariantCulture) +
                    " cyanPositions=" + positions.Count.ToString(CultureInfo.InvariantCulture) +
                    " nr=" + nr.ToString("0.0", CultureInfo.InvariantCulture) +
                    " preview=[" + multiPreviewAudit + "]" +
                    " source=mapa.cpp:StrelMode+Groups.cpp:BrigadesList::ShowPositions_all_selected");
            }
        }

        private void UpdateFormationDestinationMarkersV323LikeOriginal()
        {
            if (_formationSlotPreviewMeshV323 == null || _selected.Count == 0)
                return;

            C2NeutralPeasantUnitInfoV2LikeOriginal representative = null;
            for (int i = 0; i < _selected.Count; i++)
            {
                if (_selected[i] != null &&
                    C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(_selected[i]))
                {
                    representative = _selected[i];
                    break;
                }
            }
            List<Vector2> slots;
            byte oldDirection;
            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            if (representative == null || mode == null ||
                !C2FormationRuntimeV167LikeOriginal.TryGetFormationDestinationSlotsV323LikeOriginal(
                    representative, out slots, out oldDirection) ||
                slots == null || slots.Count == 0)
            {
                _formationSlotPreviewMeshV323.Clear(false);
                return;
            }

            float centerX = 0.0f;
            float centerY = 0.0f;
            for (int i = 0; i < slots.Count; i++)
            {
                centerX += slots[i].x;
                centerY += slots[i].y;
            }
            centerX /= slots.Count;
            centerY /= slots.Count;
            float targetCenterPxX;
            float targetCenterPxY;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(
                    _formationFacingDragDestinationWorldV321,
                    out targetCenterPxX,
                    out targetCenterPxY))
                return;
            float targetCenterRealX = targetCenterPxX * 16.0f;
            float targetCenterRealY = targetCenterPxY * 16.0f;

            byte newDirection = oldDirection;
            Camera previewCameraV352 = _formationFacingProjectionCameraV354;
            if (previewCameraV352 == null)
                previewCameraV352 = mode != null ? mode.GetActiveBattleCameraLikeOriginal() : Camera.main;
            if (previewCameraV352 != null)
            {
                Vector3 startScreenV352 =
                    previewCameraV352.WorldToScreenPoint(_formationFacingDragDestinationWorldV321);
                Vector2 startInputScreenV354 =
                    new Vector2(startScreenV352.x, startScreenV352.y) +
                    _formationFacingProjectionToInputOffsetV354;
                Vector2 mouseScreenV352 = MousePositionBottomLeft();
                int rxV352 = Mathf.RoundToInt(mouseScreenV352.x - startInputScreenV354.x);
                int unityRyV357 = Mathf.RoundToInt((mouseScreenV352.y - startInputScreenV354.y) * 2.0f);
                int c2RyV357 = -unityRyV357;
                if (C2OriginalMovementMathV352.EuclideanLengthTruncated(rxV352, c2RyV357) > 60)
                    newDirection = C2OriginalMovementMathV352.Quantize16(
                        C2OriginalMovementMathV352.GetDir(rxV352, c2RyV357));
            }
            byte previewDeltaDirV352 = (byte)(newDirection - oldDirection);
            int cos = C2OriginalMovementMathV352.TCos[previewDeltaDirV352];
            int sin = C2OriginalMovementMathV352.TSin[previewDeltaDirV352];

            List<Vector3> vertices = new List<Vector3>(slots.Count * 4);
            List<Vector2> uv = new List<Vector2>(slots.Count * 4);
            List<int> triangles = new List<int>(slots.Count * 6);
            const float halfMarkerPixels = 16.0f;
            for (int i = 0; i < slots.Count; i++)
            {
                float dx = slots[i].x - centerX;
                float dy = slots[i].y - centerY;
                float rx = targetCenterRealX + (dx * cos - dy * sin) / 256.0f;
                float ry = targetCenterRealY + (dx * sin + dy * cos) / 256.0f;
                Vector3 c = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(rx / 16.0f, ry / 16.0f);
                Vector3 bx = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                    rx / 16.0f + halfMarkerPixels, ry / 16.0f) - c;
                Vector3 by = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                    rx / 16.0f, ry / 16.0f + halfMarkerPixels) - c;
                c.y += 4.0f;
                int vi = vertices.Count;
                vertices.Add(c - bx - by);
                vertices.Add(c + bx - by);
                vertices.Add(c + bx + by);
                vertices.Add(c - bx + by);
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(1, 0));
                uv.Add(new Vector2(1, 1));
                uv.Add(new Vector2(0, 1));
                triangles.Add(vi); triangles.Add(vi + 2); triangles.Add(vi + 1);
                triangles.Add(vi); triangles.Add(vi + 3); triangles.Add(vi + 2);
            }

            _formationSlotPreviewMeshV323.Clear(false);
            _formationSlotPreviewMeshV323.SetVertices(vertices);
            _formationSlotPreviewMeshV323.SetUVs(0, uv);
            _formationSlotPreviewMeshV323.SetTriangles(triangles, 0);
            _formationSlotPreviewMeshV323.RecalculateBounds();
        }

        private static byte DirectionFromWorldDeltaV321LikeOriginal(Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.000001f)
                return 0;
            float angle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
            int raw = Mathf.RoundToInt(Mathf.Repeat(angle / 360.0f * 256.0f, 256.0f));
            return (byte)((raw + 8) & 0xF0);
        }

        private static byte DirectionFromOriginalScreenDeltaV325LikeOriginal(float dx, float dy)
        {
            return C2OriginalMovementMathV352.Quantize16(
                C2OriginalMovementMathV352.GetDir(
                    Mathf.RoundToInt(dx),
                    Mathf.RoundToInt(dy)));
        }

        private static byte DirectionFromOriginalMapDeltaV328LikeOriginal(float dx, float dy)
        {
            return C2OriginalMovementMathV352.Quantize16(
                C2OriginalMovementMathV352.GetDir(
                    Mathf.RoundToInt(dx),
                    Mathf.RoundToInt(dy)));
        }

        private bool IssueRepairConstructionOrderLikeOriginal(string source)
        {
            if (_hoverConstruction == null)
                return false;

            string audit;
            int assigned = _hoverConstruction.AssignSelectedBuildersLikeOriginal(source, out audit);
            Debug.Log("[C2:BUILD RUNTIME RIGHTCLICK MEND V260B] assigned=" + assigned.ToString(CultureInfo.InvariantCulture) +
                      " site='" + (_hoverConstruction.MdName ?? string.Empty) + "'" +
                      " source='" + (source ?? string.Empty) + "'" +
                      " audit=[" + (audit ?? string.Empty) + "]");
            return assigned > 0;
        }

        private bool IssueMoveOrderLikeOriginal(Vector3 world)
        {
            if (_selected.Count == 0) return false;

            C2BattleTerrainMode mode = null;
            for (int i = 0; i < _selected.Count && mode == null; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u != null && u.isActiveAndEnabled && u.CanReceivePlayerOrdersLikeOriginal() &&
                    C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation))
                    mode = u.OwnerMode;
            }

            if (mode != null)
            {
                float destPxX;
                float destPxY;
                if (mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(world, out destPxX, out destPxY))
                {
                    string audit;
                    CancelSelectedOrdersForManualMoveLikeOriginal("move_order_interaction_v68");
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
                if (u == null || !C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation)) continue;
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, "move_order_interaction_fallback_v68");
                u.SetMoveDestinationLikeOriginal(world);
            }

            return true;
        }

        private void CancelSelectedOrdersForManualMoveLikeOriginal(string source)
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = _selected[i];
                if (unit == null || !unit.CanReceivePlayerOrdersLikeOriginal()) continue;
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(
                    unit, source ?? "manual_move");
            }
        }

        private void RefreshSelectedCached()
        {
            // Commands must see the same selection state as the visible rings.
            // Rescan scene objects only four times/sec, but refilter the cached
            // array immediately whenever selection changes.
            float now = Time.realtimeSinceStartup;
            int selectionRevision = C2NeutralPeasantUnitInfoV2LikeOriginal.C2SelectionRevisionV346LikeOriginal;
            bool selectionChanged = selectionRevision != _lastSelectionRevisionV346;
            if (now < _nextUnitScan && !selectionChanged)
                return;

            if (now >= _nextUnitScan || _allUnitsCached == null || _allUnitsCached.Length == 0)
            {
                _nextUnitScan = now + 0.25f;
                C2NeutralPeasantUnitInfoV2LikeOriginal[] scanned =
                    C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
                _allUnitsCached = scanned ?? Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                _hasBattleUnitsCached = scanned != null && scanned.Length > 0;
            }

            _lastSelectionRevisionV346 = selectionRevision;
            _selected.Clear();
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = _allUnitsCached;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u != null && u.isActiveAndEnabled && u.IsSelected && u.CanReceivePlayerOrdersLikeOriginal() &&
                    C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation))
                    _selected.Add(u);
            }
        }

        private void RefreshHoverPickCachesLikeOriginal()
        {
            float now = Time.realtimeSinceStartup;

            // The old hover path searched and sorted the entire scene every rendered frame.
            // Cossacks II object lists are persistent; a short cache interval preserves picking
            // while removing the O(all units + all buildings) scene discovery cost from Update.
            if (now >= _nextBuildingScan)
            {
                _nextBuildingScan = now + 0.50f;
                C2SettlementBuildingSelectableV1LikeOriginal[] buildings =
                    FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
                _allBuildingsCached = buildings ?? Array.Empty<C2SettlementBuildingSelectableV1LikeOriginal>();
                Array.Sort(_allBuildingsCached, CompareBuildingsForPickLikeOriginal);
            }

            if (now >= _nextCameraScan)
            {
                _nextCameraScan = now + 0.50f;
                _pickCamerasCached = BuildBestPickCamerasLikeOriginal();
                if (_unitRenderCoreV375 == null)
                    _unitRenderCoreV375 = FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
            }
        }

        private static int CompareBuildingsForPickLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal a,
            C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            int sa = a != null ? a.SortKey : int.MinValue;
            int sb = b != null ? b.SortKey : int.MinValue;
            int c = sb.CompareTo(sa);
            if (c != 0) return c;
            int ia = a != null ? a.RecordIndex : int.MinValue;
            int ib = b != null ? b.RecordIndex : int.MinValue;
            return ib.CompareTo(ia);
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
            _hoverUnit = null;
            _hoverConstruction = null;
            _hoverResourceIdV222 = C2BattleTerrainMode.C2OriginalResourceEmptyV1LikeOriginal;
            _hoverResourceOriginalXV222 = 0;
            _hoverResourceOriginalYV222 = 0;
            _hoverResourceWorkRadiusV222 = 0;
            _hoverResourceNameV222 = string.Empty;

            // UI only cancels gameplay-hover; it must not force a resource cursor.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _hoverKind = C2GameplayTargetKindV1.None;
                C2GameplayHudV1.HideEnemyBrigadeHoverArrowV395LikeOriginal();
                return;
            }

            Vector2 mp = MousePositionBottomLeft();
            Camera[] cams = BestPickCameras();
            Vector3 world = Vector3.zero;
            Camera usedCamera;

            C2BattleTerrainMode mode = GetBattleTerrainModeCached();
            float pickedOriginalX;
            float pickedOriginalY;
            string groundPickAudit = string.Empty;
            bool exactTerrainPick = mode != null &&
                mode.C2TryGameplayGroundScreenPickLikeOriginal(
                    mp, out world, out pickedOriginalX, out pickedOriginalY, out groundPickAudit);
            if (exactTerrainPick)
            {
                usedCamera = mode.GetActiveBattleCameraLikeOriginal();
            }
            else if (!TryScreenToWorldPlaneNoPhysics(mp, cams, SelectedPlaneYLikeOriginal(), out world, out usedCamera))
            {
                _hoverKind = C2GameplayTargetKindV1.None;
                _hoverSource = "no_plane_hit_no_physics";
                return;
            }

            _hoverWorld = world;
            _formationFacingCursorGroundWorldV337 = world;
            _hoverSource = exactTerrainPick
                ? ((usedCamera != null ? usedCamera.name : "battle_camera") + ":original_terrain_pick " + groundPickAudit)
                : (usedCamera != null ? (usedCamera.name + ":plane_no_physics") : "plane_no_physics");

            if (mode == null)
            {
                _hoverKind = C2GameplayTargetKindV1.Terrain;
                return;
            }

            C2NeutralPeasantUnitInfoV2LikeOriginal hoverUnit;
            if (TryPickUnitAtScreenPointV334LikeOriginal(
                    new Vector3(mp.x, mp.y, 0.0f), cams, _allUnitsCached, out hoverUnit))
            {
                _hoverUnit = hoverUnit;
                bool friendly = C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(
                    hoverUnit.CombatNationLikeOriginal);
                _hoverKind = friendly
                    ? C2GameplayTargetKindV1.FriendlyUnit
                    : C2GameplayTargetKindV1.Enemy;
                // OneObject position is central runtime data.  Reading the
                // compatibility Transform here materialized a Unity GameObject
                // merely because a moving unit crossed the cursor.
                _hoverWorld = hoverUnit.WorldPositionLikeOriginal;
                _hoverSource = "unit_pixel_pick nation=" + hoverUnit.Nation.ToString(CultureInfo.InvariantCulture);

                if (_hoverKind == C2GameplayTargetKindV1.Enemy && _selected.Count > 0)
                {
                    // mapa.cpp:4431: OnlyOneBrig && OnlyMyUnits ->
                    // FList.AddArrowBetweenBrigades(OnlyOneBrig, enemyBR).
                    C2NeutralPeasantUnitInfoV2LikeOriginal sourceBrigadeUnit = _selected[0];
                    int sourceGroupId = -1;
                    bool oneBrigade = sourceBrigadeUnit != null &&
                        C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                            sourceBrigadeUnit, out sourceGroupId) && sourceGroupId >= 0;
                    for (int si = 1; oneBrigade && si < _selected.Count; si++)
                    {
                        int selectedGroupId;
                        if (_selected[si] == null ||
                            !C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                                _selected[si], out selectedGroupId) || selectedGroupId != sourceGroupId)
                            oneBrigade = false;
                    }
                    if (oneBrigade)
                        C2GameplayHudV1.ShowEnemyBrigadeHoverArrowV395LikeOriginal(
                            sourceBrigadeUnit, hoverUnit);
                    else
                        C2GameplayHudV1.HideEnemyBrigadeHoverArrowV395LikeOriginal();
                }
                else
                {
                    C2GameplayHudV1.HideEnemyBrigadeHoverArrowV395LikeOriginal();
                }
                return;
            }

            C2GameplayHudV1.HideEnemyBrigadeHoverArrowV395LikeOriginal();
            C2SettlementBuildingSelectableV1LikeOriginal hoverBuilding;
            float hoverBuildingDist;
            string hoverBuildingMode;
            if (TryPickBuildingAtScreenPointLikeOriginal(new Vector3(mp.x, mp.y, 0.0f), cams, out hoverBuilding, out hoverBuildingDist, out hoverBuildingMode))
            {
                _hoverBuilding = hoverBuilding;
                _hoverConstruction = hoverBuilding != null ? hoverBuilding.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>() : null;
                bool enemyBuilding = hoverBuilding != null &&
                    !C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(hoverBuilding.Nation);
                _hoverKind = enemyBuilding ? C2GameplayTargetKindV1.Enemy : C2GameplayTargetKindV1.Building;
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
            int resourceOriginalX;
            int resourceOriginalY;
            int workRadius;
            string resourceName;
            string audit;
            if (mode.C2OriginalResourceMapV1TryDetermineResourceTargetV222LikeOriginal(
                    ox,
                    oy,
                    out resourceId,
                    out resourceOriginalX,
                    out resourceOriginalY,
                    out workRadius,
                    out resourceName,
                    out audit))
            {
                C2GameplayTargetKindV1 rk = TargetKindFromOriginalResourceId(resourceId);
                if (rk != C2GameplayTargetKindV1.Unknown && rk != C2GameplayTargetKindV1.None)
                {
                    _hoverKind = rk;
                    _hoverResourceIdV222 = resourceId;
                    _hoverResourceOriginalXV222 = resourceOriginalX;
                    _hoverResourceOriginalYV222 = resourceOriginalY;
                    _hoverResourceWorkRadiusV222 = workRadius;
                    _hoverResourceNameV222 = resourceName ?? string.Empty;
                    _hoverWorld = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(resourceOriginalX, resourceOriginalY);
                    _hoverSource = "resource_lookup_no_physics_v222 " + audit;
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
                sum += u.WorldPositionLikeOriginal.y;
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

            bool isResource = kind == C2GameplayTargetKindV1.Tree ||
                              kind == C2GameplayTargetKindV1.Stone ||
                              kind == C2GameplayTargetKindV1.Field;

            // V391 melee: a brigade order must not make every selected soldier
            // converge on one OneObject. Distribute bayonet attackers across the
            // clicked hostile formation, matching the original brigade-vs-brigade
            // intent of MoveBrigadeForwardToAttack/SetEnemyForBrigade.
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> meleeVictimsV391 = null;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceSoldiersV396 = null;
            if (kind == C2GameplayTargetKindV1.Enemy && _hoverUnit != null)
            {
                int selectedMode;
                C2NeutralPeasantUnitInfoV2LikeOriginal representative =
                    _selected.Count > 0 ? _selected[0] : null;
                if (representative != null &&
                    C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                        representative, out sourceSoldiersV396) && sourceSoldiersV396 != null && sourceSoldiersV396.Count > 0)
                    representative = sourceSoldiersV396[0];
                if (C2CombatRuntimeV334LikeOriginal.TryGetCommandWeaponModeLikeOriginal(
                        representative, out selectedMode) && selectedMode == 0)
                {
                    int enemyGroup;
                    string enemyShape;
                    if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                            _hoverUnit, out meleeVictimsV391, out enemyGroup, out enemyShape) ||
                        meleeVictimsV391 == null || meleeVictimsV391.Count == 0)
                    {
                        meleeVictimsV391 = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(1);
                        meleeVictimsV391.Add(_hoverUnit);
                    }
                    else
                    {
                        List<C2NeutralPeasantUnitInfoV2LikeOriginal> enemySoldiersV396;
                        if (C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                                _hoverUnit, out enemySoldiersV396) && enemySoldiersV396 != null && enemySoldiersV396.Count > 0)
                            meleeVictimsV391 = enemySoldiersV396;
                    }
                }
            }

            bool brigadeMeleeOrderV406LikeOriginal = false;

            // Multi.cpp::AttackSelected / MoveBrigadeForwardToAttack first issue
            // one brigade movement order toward (and slightly through) the enemy
            // brigade.  Keep the formation coherent until contact instead of
            // replacing it with per-soldier approach paths.
            if (meleeVictimsV391 != null && meleeVictimsV391.Count > 0 && _selected.Count > 0)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative = _selected[0];
                C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative =
                    meleeVictimsV391[0];
                int sourceGroupId = -1;
                int enemyGroupId = -1;
                bool sourceFormation = sourceRepresentative != null &&
                    C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                        sourceRepresentative, out sourceGroupId) && sourceGroupId >= 0;
                bool enemyFormation = enemyRepresentative != null &&
                    C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                        enemyRepresentative, out enemyGroupId) && enemyGroupId >= 0;
                if (sourceFormation && enemyFormation)
                {
                    float sx, sy, ex, ey;
                    bool haveSourceCenter =
                        C2FormationRuntimeV167LikeOriginal.TryGetFormationCenterRealV406LikeOriginal(
                            sourceRepresentative, out sx, out sy);
                    bool haveEnemyCenter =
                        C2FormationRuntimeV167LikeOriginal.TryGetFormationCenterRealV406LikeOriginal(
                            enemyRepresentative, out ex, out ey);
                    if (haveSourceCenter && haveEnemyCenter)
                    {
                        // Brigade::GetCenter averages every live brigade member,
                        // including command personnel. Do not use the soldier-only
                        // fallback victim list for the destination centre.
                        float dxReal = ex - sx;
                        float dyReal = ey - sy;
                        // Multi.cpp::ShiftDestPoint uses C2 Norma, not Euclidean
                        // length: N=Norma(dx,dy); xd+=dx*120/N; yd+=dy*120/N.
                        int dReal = C2OriginalMovementMathV352.Norma(
                            Mathf.RoundToInt(dxReal), Mathf.RoundToInt(dyReal));
                        if (dReal > 0)
                        {
                            // AttackSelected::ShiftDestPoint(...,120).  Coordinates
                            // here are RealX/RealY (x16), so 120 px == 1920 real.
                            const float shiftPastEnemyReal = 120.0f * 16.0f;
                            float meleeDestX = ex + dxReal * shiftPastEnemyReal / dReal;
                            float meleeDestY = ey + dyReal * shiftPastEnemyReal / dReal;
                            byte meleeFacing =
                                C2FormationRuntimeV167LikeOriginal.GetMeleeAttackDestinationDirectionV406LikeOriginal(
                                    sourceRepresentative, enemyRepresentative);
                            string meleeMoveAudit;
                            int meleeMoveIssued = C2GameplayLooseGroupMoveLikeOriginal.IssueMoveLikeOriginal(
                                _selected, meleeDestX, meleeDestY, true, meleeFacing,
                                "Multi.cpp_AttackSelected_melee_v394", out meleeMoveAudit);
                            Debug.Log("[C2:MELEE ORDER V394] source='Multi.cpp::AttackSelected'" +
                                      " sourceGroup=" + sourceGroupId.ToString(CultureInfo.InvariantCulture) +
                                      " enemyGroup=" + enemyGroupId.ToString(CultureInfo.InvariantCulture) +
                                      " issued=" + meleeMoveIssued.ToString(CultureInfo.InvariantCulture) +
                                      " destReal=(" + meleeDestX.ToString("0", CultureInfo.InvariantCulture) +
                                      "," + meleeDestY.ToString("0", CultureInfo.InvariantCulture) + ") " +
                                      meleeMoveAudit);
                            brigadeMeleeOrderV406LikeOriginal =
                                C2FormationRuntimeV167LikeOriginal.BeginBrigadeMeleeAttackV406LikeOriginal(
                                    sourceRepresentative, enemyRepresentative, true,
                                    "Multi.cpp::AttackSelected_SetEnemyForBrigade");
                        }
                    }
                }
            }

            HashSet<int> sourceSoldierIdsV396 = null;
            if (sourceSoldiersV396 != null)
            {
                sourceSoldierIdsV396 = new HashSet<int>();
                for (int si = 0; si < sourceSoldiersV396.Count; si++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal su = sourceSoldiersV396[si];
                    if (su != null) sourceSoldierIdsV396.Add(su.GetInstanceID());
                }
            }

            int issued = 0;
            int meleeIssuedV391 = 0;
            for (int i = 0; i < _selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selected[i];
                if (u == null) continue;
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, "new_task_interaction_v222");

                C2GameplayUnitTaskV1 task = u.GetComponent<C2GameplayUnitTaskV1>();
                GameObject unitProxy = task == null ? u.EnsureUnityProxyLikeOriginal() : null;
                if (task == null && unitProxy != null) task = unitProxy.AddComponent<C2GameplayUnitTaskV1>();
                if (task == null) continue;

                if (isResource)
                {
                    Vector3 storeWorld;
                    int storeRealX;
                    int storeRealY;
                    int storeDepositRealX;
                    int storeDepositRealY;
                    Vector2[] storeConcentratorPathV228;
                    Vector2[] storeBornPathV228;
                    string storeAudit;
                    bool hasStore = TryFindNearestStorehouseV222LikeOriginal(u, out storeWorld, out storeRealX, out storeRealY, out storeDepositRealX, out storeDepositRealY, out storeConcentratorPathV228, out storeBornPathV228, out storeAudit);

                    task.BeginTakeResourceV222LikeOriginal(
                        u,
                        kind,
                        _hoverResourceIdV222,
                        _hoverResourceOriginalXV222,
                        _hoverResourceOriginalYV222,
                        _hoverResourceWorkRadiusV222,
                        _hoverWorld,
                        hasStore,
                        storeRealX,
                        storeRealY,
                        storeDepositRealX,
                        storeDepositRealY,
                        storeWorld,
                        storeConcentratorPathV228,
                        storeBornPathV228);

                    issued++;
                    if (issued <= 3)
                    {
                        Debug.Log("[C2:TAKE RESOURCE V235] ORDER unit='" + u.SourceMonsterId + "'" +
                                  " md='" + u.ResolvedMd + "'" +
                                  " kind=" + kind +
                                  " curptr=" + CursorPtrForHoverLikeOriginal(kind).ToString(CultureInfo.InvariantCulture) +
                                  " res='" + _hoverResourceNameV222 + "'" +
                                  " resourcePix=(" + _hoverResourceOriginalXV222.ToString(CultureInfo.InvariantCulture) + "," + _hoverResourceOriginalYV222.ToString(CultureInfo.InvariantCulture) + ")" +
                                  " store=" + storeAudit);
                    }
                }
                else
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal targetUnitForOrder = _hoverUnit;
                    Vector3 targetWorldForOrder = targetWorld;
                    if (kind == C2GameplayTargetKindV1.Enemy &&
                        meleeVictimsV391 != null && meleeVictimsV391.Count > 0)
                    {
                        int attempts = meleeVictimsV391.Count;
                        while (attempts-- > 0)
                        {
                            C2NeutralPeasantUnitInfoV2LikeOriginal candidate =
                                meleeVictimsV391[meleeIssuedV391 % meleeVictimsV391.Count];
                            meleeIssuedV391++;
                            if (candidate == null || candidate.IsDeadLikeOriginal || !candidate.isActiveAndEnabled)
                                continue;
                            targetUnitForOrder = candidate;
                            targetWorldForOrder = candidate.WorldPositionLikeOriginal;
                            break;
                        }
                    }
                    if (kind == C2GameplayTargetKindV1.Enemy &&
                        meleeVictimsV391 != null && meleeVictimsV391.Count > 0 &&
                        brigadeMeleeOrderV406LikeOriginal)
                    {
                        // V406: the brigade-owned Bitva order, not this UI loop, owns
                        // EnemyID assignment and retargeting.  Count soldiers as issued
                        // but do not recreate 120 private round-robin AttackObj orders.
                        if (sourceSoldierIdsV396 == null ||
                            sourceSoldierIdsV396.Contains(u.GetInstanceID()))
                            issued++;
                        continue;
                    }
                    if (kind == C2GameplayTargetKindV1.Enemy &&
                        meleeVictimsV391 != null && meleeVictimsV391.Count > 0)
                    {
                        // Fallback for loose units / no registered enemy brigade.
                        // Multi.cpp::AttackSelected writes GroundState/NewState only
                        // for BR->Memb[NBPERSONAL..NMemb). Command personnel move with
                        // the brigade but do not receive per-soldier melee AttackObj.
                        if (sourceSoldierIdsV396 != null && !sourceSoldierIdsV396.Contains(u.GetInstanceID()))
                            continue;
                        C2CombatRuntimeV334LikeOriginal.SetCommandWeaponModeLikeOriginal(u, 0);
                        C2CombatRuntimeV334LikeOriginal combat = u.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                        GameObject proxy = combat == null ? u.EnsureUnityProxyLikeOriginal() : null;
                        if (combat == null && proxy != null) combat = proxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
                        if (combat != null)
                        {
                            combat.BeginAttackForcedModeV395LikeOriginal(
                                u, targetUnitForOrder, null, targetWorldForOrder, 0);
                            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                                u, C2UnitOrderKindV325LikeOriginal.MeleeAttack,
                                "Multi.cpp_AttackSelected_melee", "attack_slot_0");
                            issued++;
                        }
                    }
                    else
                    {
                        task.Begin(
                            u, kind, targetWorldForOrder, 5.0f,
                            targetUnitForOrder, _hoverBuilding);
                        issued++;
                    }
                }
            }
            return issued > 0;
        }

        private bool TryFindNearestStorehouseV222LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out Vector3 storeWorld,
            out int storeRealX,
            out int storeRealY,
            out int storeDepositRealX,
            out int storeDepositRealY,
            out Vector2[] storeConcentratorPathV228,
            out Vector2[] storeBornPathV228,
            out string audit)
        {
            storeWorld = Vector3.zero;
            storeRealX = 0;
            storeRealY = 0;
            storeDepositRealX = 0;
            storeDepositRealY = 0;
            storeConcentratorPathV228 = null;
            storeBornPathV228 = null;
            audit = "missing";

            if (unit == null)
                return false;

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (buildings == null || buildings.Length == 0)
            {
                audit = "no_buildings";
                return false;
            }

            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;

            C2SettlementBuildingSelectableV1LikeOriginal best = null;
            float bestD2 = float.MaxValue;
            for (int i = 0; i < buildings.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                if (b == null || !b.isActiveAndEnabled || b.NotSelectable)
                    continue;

                if (unit.OwnerMode != null && b.OwnerMode != null && b.OwnerMode != unit.OwnerMode)
                    continue;

                if (!IsStorehouseBuildingV222LikeOriginal(b))
                    continue;

                float dx = b.RealX - ux;
                float dy = b.RealY - uy;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = b;
                }
            }

            if (best == null)
            {
                audit = "no_sklad_found";
                return false;
            }

            string mdPathAudit;
            if (TryBuildStorehouseBornConcentratorPathsV228LikeOriginal(best, out storeConcentratorPathV228, out storeBornPathV228, out mdPathAudit))
            {
                Vector2 entry = storeConcentratorPathV228 != null && storeConcentratorPathV228.Length > 0
                    ? storeConcentratorPathV228[0]
                    : (storeBornPathV228 != null && storeBornPathV228.Length > 1 ? storeBornPathV228[storeBornPathV228.Length - 1] : new Vector2(best.RealX, best.RealY));
                Vector2 deposit = storeConcentratorPathV228 != null && storeConcentratorPathV228.Length > 0
                    ? storeConcentratorPathV228[storeConcentratorPathV228.Length - 1]
                    : (storeBornPathV228 != null && storeBornPathV228.Length > 0 ? storeBornPathV228[0] : new Vector2(best.RealX, best.RealY));

                storeRealX = Mathf.RoundToInt(entry.x);
                storeRealY = Mathf.RoundToInt(entry.y);
                storeDepositRealX = Mathf.RoundToInt(deposit.x);
                storeDepositRealY = Mathf.RoundToInt(deposit.y);

                storeWorld = unit.OwnerMode != null
                    ? unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(storeRealX >> 4, storeRealY >> 4)
                    : best.transform.position;

                audit = "found rec=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " md='" + (best.SourceMonsterId ?? string.Empty) + "'" +
                        " kind='" + (best.KindName ?? string.Empty) + "'" +
                        " centerReal=(" + best.RealX.ToString(CultureInfo.InvariantCulture) + "," + best.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                        " entryReal=(" + storeRealX.ToString(CultureInfo.InvariantCulture) + "," + storeRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                        " depositReal=(" + storeDepositRealX.ToString(CultureInfo.InvariantCulture) + "," + storeDepositRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                        " mdPaths=" + mdPathAudit +
                        " rule=V235_BORN_TO_CONCENTRATOR_PRIORITY";
                return true;
            }

            float avx = ux - best.RealX;
            float avy = uy - best.RealY;
            float ad = Mathf.Sqrt(avx * avx + avy * avy);
            if (ad < 0.001f)
            {
                avx = 1.0f;
                avy = 0.0f;
                ad = 1.0f;
            }

            float approachRadiusPixels = Mathf.Max(StorehouseApproachRadiusOriginalPixelsV224,
                Mathf.Max(best.SelectionHalfPixelsX, best.SelectionHalfPixelsY) + 96.0f);
            storeRealX = Mathf.RoundToInt(best.RealX + (avx / ad) * approachRadiusPixels * 16.0f);
            storeRealY = Mathf.RoundToInt(best.RealY + (avy / ad) * approachRadiusPixels * 16.0f);
            storeDepositRealX = best.RealX;
            storeDepositRealY = best.RealY;

            storeWorld = unit.OwnerMode != null
                ? unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(storeRealX >> 4, storeRealY >> 4)
                : best.transform.position;

            audit = "found rec=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                    " md='" + (best.SourceMonsterId ?? string.Empty) + "'" +
                    " kind='" + (best.KindName ?? string.Empty) + "'" +
                    " centerReal=(" + best.RealX.ToString(CultureInfo.InvariantCulture) + "," + best.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " approachReal=(" + storeRealX.ToString(CultureInfo.InvariantCulture) + "," + storeRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " depositReal=(" + storeDepositRealX.ToString(CultureInfo.InvariantCulture) + "," + storeDepositRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " approachRadiusPx=" + approachRadiusPixels.ToString("0.#", CultureInfo.InvariantCulture) +
                    " mdPathsFallback=" + mdPathAudit;
            return true;
        }


        private static bool TryBuildStorehouseBornConcentratorPathsV228LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal b,
            out Vector2[] concentratorPath,
            out Vector2[] bornPath,
            out string audit)
        {
            concentratorPath = null;
            bornPath = null;
            audit = "no_md";

            if (b == null)
            {
                audit = "building_null";
                return false;
            }

            string mdName = !string.IsNullOrWhiteSpace(b.SourceMonsterId) ? b.SourceMonsterId : b.KindName;
            if (string.IsNullOrWhiteSpace(mdName))
            {
                audit = "md_empty";
                return false;
            }

            string mdText;
            string mdSource;
            if (!TryLoadMdTextV228LikeOriginal(mdName, out mdText, out mdSource) || string.IsNullOrEmpty(mdText))
            {
                audit = "md_load_failed md='" + mdName + "' source='" + (mdSource ?? string.Empty) + "'";
                return false;
            }

            int picDx = 0;
            int picDy = 0;
            bool hasLocation = false;
            List<Vector2> bornLocal = new List<Vector2>();
            List<Vector2> concLocal = new List<Vector2>();
            List<Vector2> born2Local = new List<Vector2>();
            List<Vector2> conc2Local = new List<Vector2>();

            string[] lines = mdText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripMdCommentV228LikeOriginal(lines[i]).Trim();
                if (line.Length == 0 || line[0] == '/') continue;

                string[] t = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (t == null || t.Length == 0) continue;

                string cmd = t[0].ToUpperInvariant();
                if (cmd == "LOCATION" && t.Length >= 5)
                {
                    picDx = ToIntV228LikeOriginal(t[1]);
                    picDy = ToIntV228LikeOriginal(t[2]);
                    hasLocation = true;
                }
                else if (cmd == "BORNPOINTS")
                {
                    ParseMdPointListV228LikeOriginal(t, bornLocal, false);
                }
                else if (cmd == "CONCENTRATOR")
                {
                    ParseMdPointListV228LikeOriginal(t, concLocal, false);
                }
                else if (cmd == "BORNPOINTS2")
                {
                    ParseMdPointListV228LikeOriginal(t, born2Local, true);
                }
                else if (cmd == "CONCENTRATOR2")
                {
                    ParseMdPointListV228LikeOriginal(t, conc2Local, true);
                }
            }

            List<Vector2> useConc = concLocal.Count >= 2 ? concLocal : (conc2Local.Count >= 2 ? conc2Local : null);
            List<Vector2> useBorn = bornLocal.Count >= 2 ? bornLocal : (born2Local.Count >= 2 ? born2Local : null);

            if (useConc == null && useBorn == null)
            {
                audit = "md_paths_missing md='" + mdName + "' source='" + mdSource + "'";
                return false;
            }

            // V235: do not synthesize CONCENTRATOR from BORNPOINTS before conversion.
            // In the original data these are different semantic paths:
            // BORNPOINTS = approach/exit gate, CONCENTRATOR = deeper resource deposit point.
            // Fallback is chosen only after real MD paths are converted.

            int cornerX;
            int cornerY;
            int picSX = picDx << 4;
            int picSY = picDy << 5;
            cornerX = (b.RealX + picSX) >> 8;
            cornerY = (b.RealY + picSY) >> 8;

            Vector2[] mdBornPath = LocalCellPathToRealV228LikeOriginal(useBorn, cornerX, cornerY);
            Vector2[] mdConcPath = LocalCellPathToRealV228LikeOriginal(useConc, cornerX, cornerY);

            string resourceStoreRuleV234;
            if (mdBornPath != null && mdBornPath.Length >= 2 && mdConcPath != null && mdConcPath.Length >= 1)
            {
                // V235: resource delivery must pass through the building gate and then continue
                // to the real CONCENTRATOR point. V234 used reversed BORNPOINTS as deposit path,
                // so peasants could unload at the outer edge instead of the inner storage point.
                bornPath = mdBornPath;
                concentratorPath = AppendRealPathsV235LikeOriginal(ReverseCopyRealPathV234LikeOriginal(mdBornPath), mdConcPath);
                resourceStoreRuleV234 = "V235_ORIGINAL_BORN_ENTRY_THEN_CONCENTRATOR_DEPOSIT";
            }
            else if (mdConcPath != null && mdConcPath.Length >= 1)
            {
                // Fallback for buildings that define only CONCENTRATOR.
                concentratorPath = mdConcPath;
                bornPath = ReverseCopyRealPathV234LikeOriginal(mdConcPath);
                resourceStoreRuleV234 = "V235_ORIGINAL_CONCENTRATOR_ONLY_FALLBACK";
            }
            else if (mdBornPath != null && mdBornPath.Length >= 2)
            {
                // Last fallback: old V234 behavior when no CONCENTRATOR exists in MD.
                bornPath = mdBornPath;
                concentratorPath = ReverseCopyRealPathV234LikeOriginal(mdBornPath);
                resourceStoreRuleV234 = "V235_BORNPOINTS_ONLY_FALLBACK";
            }
            else
            {
                audit = "md_paths_convert_failed md='" + mdName + "' source='" + mdSource + "'";
                return false;
            }

            string shiftAudit = string.Empty;
            ApplyRuntimeCenterShiftIfMdPathFarV229LikeOriginal(b, ref concentratorPath, ref bornPath, out shiftAudit);

            audit = "md='" + mdName + "'" +
                    " source='" + mdSource + "'" +
                    " location=" + picDx.ToString(CultureInfo.InvariantCulture) + "/" + picDy.ToString(CultureInfo.InvariantCulture) +
                    " hasLocation=" + hasLocation.ToString() +
                    " corner=" + cornerX.ToString(CultureInfo.InvariantCulture) + "/" + cornerY.ToString(CultureInfo.InvariantCulture) +
                    " rule=" + resourceStoreRuleV234 +
                    " concCount=" + (concentratorPath != null ? concentratorPath.Length : 0).ToString(CultureInfo.InvariantCulture) +
                    " bornCount=" + (bornPath != null ? bornPath.Length : 0).ToString(CultureInfo.InvariantCulture) +
                    " " + shiftAudit;
            return true;
        }

        internal static bool TryLoadMdTextV228LikeOriginal(string mdName, out string text, out string source)
        {
            text = string.Empty;
            source = string.Empty;
            string safe = (mdName ?? string.Empty).Trim();
            if (safe.EndsWith(".MD", StringComparison.OrdinalIgnoreCase))
                safe = safe.Substring(0, safe.Length - 3);
            if (string.IsNullOrWhiteSpace(safe)) return false;

            TextAsset ta = Resources.Load<TextAsset>("UnitsMD/" + safe);
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                text = ta.text;
                source = "Resources/UnitsMD/" + safe;
                return true;
            }

            string[] candidates = new[]
            {
                Path.Combine(Application.dataPath, "Resources", "UnitsMD", safe + ".MD"),
                Path.Combine(Application.dataPath, "Resources", "UnitsMD", safe + ".md"),
                Path.Combine(Application.dataPath, "Resources", safe + ".MD"),
                Path.Combine(Application.dataPath, "Resources", safe + ".md")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string p = candidates[i];
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    text = File.ReadAllText(p);
                    source = p;
                    return true;
                }
            }

            source = "not_found:" + safe;
            return false;
        }

        private static string StripMdCommentV228LikeOriginal(string line)
        {
            if (line == null) return string.Empty;
            int p = line.IndexOf("//", StringComparison.Ordinal);
            return p >= 0 ? line.Substring(0, p) : line;
        }

        private static int ToIntV228LikeOriginal(string s)
        {
            int v;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return v;
            int.TryParse(s, out v);
            return v;
        }

        private static void ParseMdPointListV228LikeOriginal(string[] t, List<Vector2> dst, bool exactPixelsV2)
        {
            if (t == null || t.Length < 2 || dst == null) return;
            int count = ToIntV228LikeOriginal(t[1]);
            int maxPairs = Mathf.Max(0, (t.Length - 2) / 2);
            if (count <= 0 || count > maxPairs) count = maxPairs;
            for (int i = 0; i < count; i++)
            {
                int ti = 2 + i * 2;
                int rawX = ToIntV228LikeOriginal(t[ti]);
                int rawY = ToIntV228LikeOriginal(t[ti + 1]);

                // Original NewMon.cpp parser:
                // BORNPOINTS/CONCENTRATOR  -> x*16+8, y*16+8
                // BORNPOINTS2/CONCENTRATOR2 -> x, y<<1
                int x = exactPixelsV2 ? rawX : rawX * 16 + 8;
                int y = exactPixelsV2 ? (rawY << 1) : rawY * 16 + 8;
                dst.Add(new Vector2(x, y));
            }
        }

        private static List<Vector2> ReverseCopyLocalPathV228LikeOriginal(List<Vector2> src)
        {
            if (src == null || src.Count == 0) return null;
            List<Vector2> dst = new List<Vector2>(src.Count);
            for (int i = src.Count - 1; i >= 0; i--)
                dst.Add(src[i]);
            return dst;
        }

        private static Vector2[] ReverseCopyRealPathV234LikeOriginal(Vector2[] src)
        {
            if (src == null || src.Length == 0) return null;
            Vector2[] dst = new Vector2[src.Length];
            for (int i = 0; i < src.Length; i++)
                dst[i] = src[src.Length - 1 - i];
            return dst;
        }

        private static Vector2[] CloneRealPathV228LikeOriginal(Vector2[] src)
        {
            if (src == null || src.Length == 0) return null;
            Vector2[] dst = new Vector2[src.Length];
            for (int i = 0; i < src.Length; i++)
                dst[i] = src[i];
            return dst;
        }

        private static Vector2[] AppendRealPathsV235LikeOriginal(Vector2[] first, Vector2[] second)
        {
            if ((first == null || first.Length == 0) && (second == null || second.Length == 0)) return null;
            if (first == null || first.Length == 0) return CloneRealPathV228LikeOriginal(second);
            if (second == null || second.Length == 0) return CloneRealPathV228LikeOriginal(first);

            List<Vector2> dst = new List<Vector2>(first.Length + second.Length);
            for (int i = 0; i < first.Length; i++)
                dst.Add(first[i]);

            int start = 0;
            Vector2 a = first[first.Length - 1];
            Vector2 b = second[0];
            if ((a - b).sqrMagnitude <= 64.0f * 64.0f)
                start = 1;

            for (int i = start; i < second.Length; i++)
                dst.Add(second[i]);

            return dst.ToArray();
        }

        private static Vector2[] LocalCellPathToRealV228LikeOriginal(List<Vector2> local, int cornerX, int cornerY)
        {
            if (local == null || local.Count == 0) return null;
            Vector2[] path = new Vector2[local.Count];
            for (int i = 0; i < local.Count; i++)
            {
                int lx = Mathf.RoundToInt(local[i].x);
                int ly = Mathf.RoundToInt(local[i].y);
                path[i] = new Vector2(((cornerX << 4) + lx) << 4,
                                      ((cornerY << 4) + ly) << 4);
            }
            return path;
        }


        private static void ApplyRuntimeCenterShiftIfMdPathFarV229LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal b,
            ref Vector2[] concentratorPath,
            ref Vector2[] bornPath,
            out string audit)
        {
            audit = "shift=none";
            if (b == null) return;

            int count = 0;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            AccumulatePathBoundsV229LikeOriginal(concentratorPath, ref count, ref minX, ref minY, ref maxX, ref maxY);
            AccumulatePathBoundsV229LikeOriginal(bornPath, ref count, ref minX, ref minY, ref maxX, ref maxY);
            if (count <= 0) return;

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float dx = b.RealX - centerX;
            float dy = b.RealY - centerY;

            float distPx = Mathf.Sqrt(dx * dx + dy * dy) / 16.0f;
            float allowedPx = Mathf.Max(192.0f, Mathf.Max(b.SelectionHalfPixelsX, b.SelectionHalfPixelsY) + 96.0f);

            if (distPx <= allowedPx)
            {
                audit = "shift=none distPx=" + distPx.ToString("0.#", CultureInfo.InvariantCulture) +
                        " allowedPx=" + allowedPx.ToString("0.#", CultureInfo.InvariantCulture);
                return;
            }

            ShiftPathV229LikeOriginal(concentratorPath, dx, dy);
            ShiftPathV229LikeOriginal(bornPath, dx, dy);

            audit = "shift=runtime_center_v229" +
                    " dx=" + dx.ToString("0.#", CultureInfo.InvariantCulture) +
                    " dy=" + dy.ToString("0.#", CultureInfo.InvariantCulture) +
                    " distPx=" + distPx.ToString("0.#", CultureInfo.InvariantCulture) +
                    " allowedPx=" + allowedPx.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static void AccumulatePathBoundsV229LikeOriginal(Vector2[] path, ref int count, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            if (path == null) return;
            for (int i = 0; i < path.Length; i++)
            {
                Vector2 p = path[i];
                minX = Mathf.Min(minX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
                count++;
            }
        }

        private static void ShiftPathV229LikeOriginal(Vector2[] path, float dx, float dy)
        {
            if (path == null) return;
            for (int i = 0; i < path.Length; i++)
                path[i] = new Vector2(path[i].x + dx, path[i].y + dy);
        }

        private static bool IsStorehouseBuildingV222LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            if (b == null) return false;
            string n = ((b.SourceMonsterId ?? string.Empty) + " " + (b.KindName ?? string.Empty)).ToLowerInvariant();

            // MD warehouse names in current data: EngSkl, AusSkl, RusSkl, FrnSkl, etc.
            if (n.IndexOf("skl", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("sklad", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("store", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("warehouse", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
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
            if (u != null)
            {
                return C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation)
                    ? C2GameplayTargetKindV1.FriendlyUnit
                    : C2GameplayTargetKindV1.Enemy;
            }

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

        private Camera[] BestPickCameras()
        {
            RefreshHoverPickCachesLikeOriginal();
            return _pickCamerasCached;
        }

        private static Camera[] BuildBestPickCamerasLikeOriginal()
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
            // COSSACKS2/mapa.cpp: int AttackPtr=1. Data/Hard/attack.cur is
            // the Cossacks II blade cursor; do not substitute C1/AC cursors.
            if (kind == C2GameplayTargetKindV1.Enemy && CanSelectedAttackLikeOriginal())
                return 1;

            // V222: original resource cursors are allowed only for selected peasants.
            if (CanSelectedTakeResourcesLikeOriginal())
            {
                if (kind == C2GameplayTargetKindV1.Stone) return 5; // Cursors/Hard/stoun.cur
                if (kind == C2GameplayTargetKindV1.Tree) return 6;  // Cursors/Hard/wood.cur
                if (kind == C2GameplayTargetKindV1.Field) return 7; // Cursors/Hard/food.cur
            }

            // V67: only enable the original repair/build cursor for unfinished construction sites.
            if (kind == C2GameplayTargetKindV1.Building &&
                _hoverConstruction != null &&
                _hoverConstruction.CanAcceptBuildersLikeOriginal &&
                CanSelectedRepairLikeOriginal())
                return 3; // Cursors/Hard/mend.cur

            return 0; // Cursors/Hard/main.cur
        }

        private bool TryPickUnitAtScreenPointV334LikeOriginal(
            Vector3 mousePosition,
            Camera[] cameras,
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units,
            out C2NeutralPeasantUnitInfoV2LikeOriginal hit)
        {
            using var unitPickProfile = UnitPickProfileV377.Auto();
            hit = null;
            LastHoverUnitCandidatesV375LikeOriginal = 0;
            if (units == null || units.Length == 0 || cameras == null)
                return false;

            IReadOnlyList<C2UnitOriginalRuntime> visible = null;
            Camera visibleCamera = null;
            bool registered = _unitRenderCoreV375 != null &&
                _unitRenderCoreV375.C2TryGetVisibleUnitsForPickingV375LikeOriginal(out visible, out visibleCamera);
            int bestSort = int.MinValue;
            float bestDistance = float.MaxValue;
            for (int c = 0; c < (registered ? 1 : cameras.Length); c++)
            {
                Camera cam = registered ? visibleCamera : cameras[c];
                if (cam == null || !cam.isActiveAndEnabled) continue;
                var projection = new C2UnitOriginalRuntimeAndRendererV1.UnitScreenProjectionV376LikeOriginal(cam);
                for (int i = 0; i < (registered ? visible.Count : units.Length); i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = registered ? visible[i].Info : units[i];
                    if (unit == null || !unit.isActiveAndEnabled || unit.NotSelectable || unit.IsDeadLikeOriginal)
                        continue;
                    LastHoverUnitCandidatesV375LikeOriginal++;
                    if (registered)
                    {
                        Rect broadRect;
                        Vector2 anchor;
                        if (!_unitRenderCoreV375.TryGetRuntimeScreenRectProjectedV376LikeOriginal(
                                visible[i], in projection, out broadRect, out anchor)) continue;
                        // Conservative rounding tolerance; final alpha hit below
                        // continues to use Unity's unchanged native projection.
                        float left = broadRect.x, bottom = broadRect.y;
                        if (mousePosition.x < left - 0.1f || mousePosition.y < bottom - 0.1f ||
                            mousePosition.x > left + broadRect.width + 0.1f ||
                            mousePosition.y > bottom + broadRect.height + 0.1f) continue;
                    }
                    float alpha;
                    Vector2 uv;
                    if (!unit.TryPixelHit(cam, mousePosition, out alpha, out uv) || alpha <= 0.01f)
                        continue;
                    Vector3 sp = cam.WorldToScreenPoint(unit.WorldPositionLikeOriginal);
                    float d = Vector2.Distance(new Vector2(mousePosition.x, mousePosition.y), new Vector2(sp.x, sp.y));
                    if (unit.SortKey > bestSort || (unit.SortKey == bestSort && d < bestDistance))
                    {
                        bestSort = unit.SortKey;
                        bestDistance = d;
                        hit = unit;
                    }
                }
            }
            return hit != null;
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

            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = _allBuildingsCached;
            if (buildings == null || buildings.Length == 0 || cameras == null)
                return false;

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
                if (u != null && u.isActiveAndEnabled && u.CanReceivePlayerOrdersLikeOriginal() &&
                    C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation))
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
                if (IsSelectedBuilderLikeOriginal(u))
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
            if (u == null || !u.isActiveAndEnabled || !u.CanReceivePlayerOrdersLikeOriginal() ||
                !C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(u.Nation))
                return false;

            return u.IsPeasantLikeOriginal();
        }

        private static bool IsSelectedBuilderLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u != null && u.isActiveAndEnabled && u.CanBuildOrRepairLikeOriginal();
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

    internal sealed class C2FormationCommandPreviewGraphicV324LikeOriginal : MaskableGraphic
    {
        private readonly List<Vector2> _arrow = new List<Vector2>(8);
        private readonly List<Vector2> _positions = new List<Vector2>(256);
        private readonly List<Image> _visibleSegments = new List<Image>(256);

        internal void SetPreviewLikeOriginal(IList<Vector2> arrow, IList<Vector2> positions)
        {
            _arrow.Clear();
            _positions.Clear();
            if (arrow != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera eventCamera =
                    canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? canvas.worldCamera
                        : null;
                for (int i = 0; i < arrow.Count; i++)
                {
                    Vector2 local;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            rectTransform, arrow[i], eventCamera, out local))
                        _arrow.Add(local);
                }
            }
            if (positions != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera eventCamera =
                    canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? canvas.worldCamera
                        : null;
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector2 local;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            rectTransform, positions[i], eventCamera, out local))
                        _positions.Add(local);
                }
            }
            SetVerticesDirty();
            SetMaterialDirty();
            RebuildVisibleSegmentsV327LikeOriginal();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        private void RebuildVisibleSegmentsV327LikeOriginal()
        {
            int used = 0;
            Color32 yellow = new Color32(255, 255, 0, 255);
            for (int i = 1; i < _arrow.Count; i++)
                SetVisibleSegmentV327LikeOriginal(
                    used++, _arrow[i - 1], _arrow[i], 2.0f, yellow);

            Color32 cyan = new Color32(0, 255, 255, 255);
            for (int i = 0; i < _positions.Count; i++)
            {
                Vector2 p = _positions[i];
                SetVisibleSegmentV327LikeOriginal(
                    used++, p + Vector2.down * 4.0f, p + Vector2.up * 4.0f, 2.0f, cyan);
                SetVisibleSegmentV327LikeOriginal(
                    used++, p + Vector2.left * 4.0f, p + Vector2.right * 4.0f, 2.0f, cyan);
            }

            for (int i = used; i < _visibleSegments.Count; i++)
                if (_visibleSegments[i] != null)
                    _visibleSegments[i].gameObject.SetActive(false);
        }

        private void SetVisibleSegmentV327LikeOriginal(
            int index,
            Vector2 a,
            Vector2 b,
            float thickness,
            Color32 color)
        {
            while (_visibleSegments.Count <= index)
            {
                GameObject go = new GameObject(
                    "FormationPreviewSegment_" + _visibleSegments.Count.ToString(CultureInfo.InvariantCulture));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                RectTransform segmentRect = go.AddComponent<RectTransform>();
                segmentRect.anchorMin = Vector2.zero;
                segmentRect.anchorMax = Vector2.zero;
                segmentRect.pivot = new Vector2(0.5f, 0.5f);
                Image image = go.AddComponent<Image>();
                image.raycastTarget = false;
                _visibleSegments.Add(image);
            }

            Image segment = _visibleSegments[index];
            if (segment == null) return;
            segment.gameObject.SetActive(true);
            segment.color = color;
            RectTransform rt = segment.rectTransform;
            Vector2 delta = b - a;
            float length = delta.magnitude;
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(Mathf.Max(1.0f, length), thickness);
            rt.localRotation = Quaternion.Euler(
                0.0f, 0.0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void AddSegment(
            VertexHelper vh,
            Vector2 a,
            Vector2 b,
            float halfWidth,
            Color32 color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * halfWidth;
            int index = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;
            v.position = a - n; vh.AddVert(v);
            v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v);
            v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(index, index + 2, index + 1);
            vh.AddTriangle(index, index + 3, index + 2);
        }
    }
}
