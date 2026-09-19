using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Unity-side representation of OneObject::LocalOrder/Order1::NextOrder for move commands.
    // The insertion rules are copied from COSSACKS2/NewMon.cpp::OneObject::CreateOrder.
    internal sealed class C2OriginalOrderChainV352
    {
        // NewMon.cpp::LongProcesses walks Group[] once and executes
        // OB->LocalOrder->DoLink(OB) from that central object loop. Keeping a
        // Unity Update callback on every ordered unit adds thousands of native
        // to managed calls that do not exist in COSSACKS2.
        private static readonly List<C2OriginalOrderChainV352> ActiveChainsLikeOriginal =
            new List<C2OriginalOrderChainV352>(1024);

        private sealed class MoveOrder
        {
            internal MoveOrder NextOrder;
            internal Vector2[] PathReal;
            internal float DestRealX;
            internal float DestRealY;
            internal bool HasPath;
            internal bool HasFinalFacing;
            internal byte FinalFacing;
            internal float SpeedOriginalPixelsPerSecond;
            internal string Source;
            internal bool Started;
            internal bool PreserveCurrentAttackFrame;
            internal bool WaitForForeignOrder;
        }

        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private MoveOrder _localOrder;
        private bool _registeredActiveLikeOriginal;

        internal static C2OriginalOrderChainV352 GetOrCreate(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return null;
            C2OriginalOrderChainV352 chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain == null)
            {
                chain = new C2OriginalOrderChainV352();
                unit.C2MoveOrderChainV362LikeOriginal = chain;
            }
            chain._unit = unit;
            return chain;
        }

        internal static void TickActiveOrdersLikeOriginal()
        {
            // This is the Unity equivalent of the single Group[]/LocalOrder
            // section in COSSACKS2/NewMon.cpp::LongProcesses.
            for (int i = ActiveChainsLikeOriginal.Count - 1; i >= 0; i--)
            {
                C2OriginalOrderChainV352 chain = ActiveChainsLikeOriginal[i];
                if (chain == null || chain._localOrder == null || chain._unit == null)
                {
                    if (chain != null) chain._registeredActiveLikeOriginal = false;
                    ActiveChainsLikeOriginal.RemoveAt(i);
                    continue;
                }

                chain.TickOrderNodeLikeOriginal();
                if (chain._localOrder == null)
                {
                    chain._registeredActiveLikeOriginal = false;
                    ActiveChainsLikeOriginal.RemoveAt(i);
                }
            }
        }

        private void RegisterActiveLikeOriginal()
        {
            if (_registeredActiveLikeOriginal) return;
            _registeredActiveLikeOriginal = true;
            ActiveChainsLikeOriginal.Add(this);
        }

        internal static void ClearMoveChainForExternalOrder(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            C2OriginalOrderChainV352 chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain != null) chain._localOrder = null;
        }

        internal static void ReleaseForUnitLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            C2OriginalOrderChainV352 chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain != null)
            {
                chain._localOrder = null;
                chain._unit = null;
                if (chain._registeredActiveLikeOriginal)
                    ActiveChainsLikeOriginal.Remove(chain);
                chain._registeredActiveLikeOriginal = false;
            }
            unit.C2MoveOrderChainV362LikeOriginal = null;
            unit.C2OrderRuntimeStateV362LikeOriginal = null;
        }

        internal static bool SubmitMove(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float destRealX,
            float destRealY,
            bool hasFinalFacing,
            byte finalFacing,
            byte ordType,
            string source)
        {
            C2OriginalOrderChainV352 chain = GetOrCreate(unit);
            if (chain == null) return false;
            MoveOrder order = new MoveOrder
            {
                DestRealX = destRealX,
                DestRealY = destRealY,
                HasFinalFacing = hasFinalFacing,
                FinalFacing = finalFacing,
                SpeedOriginalPixelsPerSecond = C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                Source = source ?? "c2_move",
                HasPath = false
            };
            byte createOrderType = (byte)(ordType & 127);
            // NewMonsterSmartSendTo: CreateOrder((OrdType&127)==0 ? 3 : (OrdType&127)).
            if (createOrderType == 0) createOrderType = 3;
            chain.InsertLikeCreateOrder(order, createOrderType);
            chain.TryStartHead();
            chain.RegisterActiveLikeOriginal();
            return true;
        }

        internal static bool SubmitPath(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2[] pathReal,
            bool hasFinalFacing,
            byte finalFacing,
            byte ordType,
            string source,
            float speedOriginalPixelsPerSecond = 0.0f)
        {
            if (unit == null || pathReal == null || pathReal.Length == 0) return false;
            C2OriginalOrderChainV352 chain = GetOrCreate(unit);
            if (chain == null) return false;
            MoveOrder order = new MoveOrder
            {
                PathReal = pathReal,
                HasPath = true,
                HasFinalFacing = hasFinalFacing,
                FinalFacing = finalFacing,
                SpeedOriginalPixelsPerSecond = speedOriginalPixelsPerSecond > 0.0f
                    ? speedOriginalPixelsPerSecond
                    : C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                Source = source ?? "c2_path"
            };
            byte createOrderType = (byte)(ordType & 127);
            if (createOrderType == 0) createOrderType = 3;
            chain.InsertLikeCreateOrder(order, createOrderType);
            chain.TryStartHead();
            chain.RegisterActiveLikeOriginal();
            return true;
        }

        private void InsertLikeCreateOrder(MoveOrder order, byte type)
        {
            if (order == null) return;

            // NewMon.cpp::CreateOrder:
            // 1 -> push to LocalOrder head.
            // 2 -> append to the tail.
            // 3 -> clear, except preserve the currently executing attack order/frame.
            // default -> clear and replace.
            switch (type)
            {
                case 1:
                    order.NextOrder = _localOrder;
                    _localOrder = order;
                    break;

                case 2:
                    order.NextOrder = null;
                    if (_localOrder == null)
                    {
                        // The Unity compatibility layer also has attack/build/resource orders
                        // outside this move chain. In C2 those are LocalOrder nodes too, so a
                        // Shift move must wait behind them rather than replacing them.
                        order.WaitForForeignOrder = IsForeignOrderStillBusy();
                        _localOrder = order;
                    }
                    else
                    {
                        MoveOrder tail = _localOrder;
                        while (tail.NextOrder != null) tail = tail.NextOrder;
                        tail.NextOrder = order;
                    }
                    break;

                case 3:
                {
                    bool preserveAttack = IsCurrentAttackFrameRunning();
                    _localOrder = order;
                    order.NextOrder = null;
                    order.PreserveCurrentAttackFrame = preserveAttack;
                    if (!preserveAttack)
                        CancelForeignOrdersForReplacement(order.Source);
                    break;
                }

                default:
                    _localOrder = order;
                    order.NextOrder = null;
                    CancelForeignOrdersForReplacement(order.Source);
                    break;
            }
        }

        private bool IsCurrentAttackFrameRunning()
        {
            if (_unit == null) return false;
            C2UnitOrderRuntimeV325LikeOriginal state =
                C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(_unit);
            if (state == null) return false;
            C2UnitOrderKindV325LikeOriginal kind = state.CurrentLikeOriginal;
            bool attack = kind == C2UnitOrderKindV325LikeOriginal.Attack ||
                          kind == C2UnitOrderKindV325LikeOriginal.PreciseAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.UnitAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.RangedAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.MeleeAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.GrenadeAttack;
            if (!attack) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link == null) return false;
            int frame;
            int frameCount;
            int activeFrame;
            bool attacking;
            if (!link.TryGetAttackTimingV335LikeOriginal(out frame, out frameCount, out activeFrame, out attacking) || !attacking)
                return false;
            // NewMon.cpp::CheckIfNowAttack: NewCurSprite>ActiveFrame means the
            // current attack order is no longer protected by CreateOrder(3).
            return frame <= activeFrame;
        }

        private bool IsForeignOrderStillBusy()
        {
            if (_unit == null) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null)
            {
                if (link.Runtime.HasMoveTargetLikeOriginal ||
                    link.Runtime.MoveDeferredUntilNeutralStandLikeOriginal ||
                    link.Runtime.MovePathRealWaypointsLikeOriginal != null)
                    return true;
            }

            C2UnitOrderRuntimeV325LikeOriginal state =
                C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(_unit);
            if (state == null) return false;
            switch (state.CurrentLikeOriginal)
            {
                case C2UnitOrderKindV325LikeOriginal.Attack:
                case C2UnitOrderKindV325LikeOriginal.PreciseAttack:
                case C2UnitOrderKindV325LikeOriginal.UnitAttack:
                case C2UnitOrderKindV325LikeOriginal.RangedAttack:
                case C2UnitOrderKindV325LikeOriginal.MeleeAttack:
                case C2UnitOrderKindV325LikeOriginal.GrenadeAttack:
                case C2UnitOrderKindV325LikeOriginal.BuildApproach:
                case C2UnitOrderKindV325LikeOriginal.BuildWork:
                case C2UnitOrderKindV325LikeOriginal.ResourceApproach:
                case C2UnitOrderKindV325LikeOriginal.ResourceWork:
                case C2UnitOrderKindV325LikeOriginal.ResourceDeposit:
                case C2UnitOrderKindV325LikeOriginal.ResourceReturn:
                    return true;
            }
            return false;
        }

        private void CancelForeignOrdersForReplacement(string source)
        {
            if (_unit == null) return;
            C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(
                _unit, source ?? "c2_order_replace");
            C2GameplayUnitTaskV1 task = _unit.GetComponent<C2GameplayUnitTaskV1>();
            if (task != null && task.enabled)
                task.CancelForExternalOrderLikeOriginal(source ?? "c2_order_replace");
            C2CombatRuntimeV334LikeOriginal combat = _unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat != null && combat.enabled)
                combat.CancelForExternalOrderLikeOriginal(source ?? "c2_order_replace");
        }

        private static int s_planningFrameLikeOriginal = -1;
        private static int s_planningStartsLikeOriginal;
        private static long s_planningTicksLikeOriginal;

        private static bool HasPlanningBudgetLikeOriginal()
        {
            int frame = Time.frameCount;
            if (frame != s_planningFrameLikeOriginal)
            {
                s_planningFrameLikeOriginal = frame;
                s_planningStartsLikeOriginal = 0;
                s_planningTicksLikeOriginal = 0;
            }
            // Original topology orders run from the object loop. Spread expensive
            // local fallback searches across frames instead of doing an entire crowd
            // synchronously in the mouse handler. Accepted commands remain queued.
            return s_planningStartsLikeOriginal < 16 &&
                s_planningTicksLikeOriginal < global::System.Diagnostics.Stopwatch.Frequency / 250;
        }

        private void TryStartHead()
        {
            if (_unit == null || _localOrder == null || _localOrder.Started) return;

            if (_localOrder.PreserveCurrentAttackFrame)
            {
                if (IsCurrentAttackFrameRunning()) return;
                _localOrder.PreserveCurrentAttackFrame = false;
                CancelForeignOrdersForReplacement(_localOrder.Source);
            }
            else if (_localOrder.WaitForForeignOrder)
            {
                // Only CreateOrder(Type=2) waits behind an already existing external
                // order. Type 0/1/3 must replace/start immediately; checking the old
                // runtime move here was the V350/V351 command-eating bug.
                if (IsForeignOrderStillBusy()) return;
                _localOrder.WaitForForeignOrder = false;
            }

            if (!HasPlanningBudgetLikeOriginal()) return;
            s_planningStartsLikeOriginal++;
            long planningStarted = global::System.Diagnostics.Stopwatch.GetTimestamp();
            MoveOrder order = _localOrder;
            order.Started = true;
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                _unit,
                C2UnitOrderKindV325LikeOriginal.Move,
                order.Source,
                order.HasFinalFacing ? "c2_order_move_with_facing" : "c2_order_move");

            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (order.HasPath && order.PathReal != null && order.PathReal.Length > 0 && link != null)
            {
                link.SetMovePathRealLikeOriginal(
                    order.PathReal,
                    order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing,
                    order.FinalFacing,
                    false,
                    order.Source);
            }
            else if (link != null)
            {
                // A destination alone has no validated path. C2 SmartSend checks from
                // this object's position, including after a queued order starts.
                link.SetMoveDestinationRealLikeOriginal(
                    order.DestRealX, order.DestRealY,
                    order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing, order.FinalFacing);
            }
            else
            {
                _unit.SetMoveDestinationRealLikeOriginal(
                    order.DestRealX,
                    order.DestRealY,
                    order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing,
                    order.FinalFacing);
            }
            s_planningTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - planningStarted;
        }

        private bool HeadMoveFinished()
        {
            if (_unit == null || _localOrder == null || !_localOrder.Started) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link == null || link.Runtime == null) return true;
            C2UnitOriginalRuntime rt = link.Runtime;
            return !rt.HasMoveTargetLikeOriginal &&
                   !rt.MoveDeferredUntilNeutralStandLikeOriginal &&
                   rt.MovePathRealWaypointsLikeOriginal == null;
        }

        private void TickOrderNodeLikeOriginal()
        {
            if (_localOrder == null) return;

            if (!_localOrder.Started)
            {
                TryStartHead();
                return;
            }

            if (!HeadMoveFinished()) return;

            _localOrder = _localOrder.NextOrder;
            TryStartHead();
        }

    }
}
