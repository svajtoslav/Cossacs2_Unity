using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V418: single managed representation of COSSACKS2 Brigade::NewBOrder.
    //
    // ORIGINAL SEMANTICS:
    //   Brigade::CreateNewBOrder(Type, Order)
    //     Type 1 -> push at head (Order->Next = NewBOrder; NewBOrder = Order)
    //     Type 2 -> append at tail
    //     other  -> ClearNewBOrders(); NewBOrder = Order
    //   Brigade::DeleteNewBOrder() -> delete head and resume head->Next.
    //
    // UNITY ADAPTER:
    //   The concrete order processors still live in their existing managed classes,
    //   but every gameplay decision about "BR->NewBOrder" reads THIS chain only.
    //   Processor dictionaries/flags are implementation storage, never a second
    //   source of brigade-order truth.
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        internal const byte BrigadeOrderNoneV418LikeOriginal = 0;
        internal const byte BrigadeOrderRifleAttackV418LikeOriginal = 1;
        internal const byte BrigadeOrderGoOnRoadV418LikeOriginal = 2;
        internal const byte BrigadeOrderBitvaV418LikeOriginal = 3;
        internal const byte BrigadeOrderKeepPositionsV418LikeOriginal = 4;
        internal const byte BrigadeOrderHumanGlobalSendToV418LikeOriginal = 5;

        private sealed class BrigadeNewOrderNodeV418LikeOriginal
        {
            public byte OrderId;
            public byte Priority;
            public byte CreateType;
            public int Serial;
            public string Source = string.Empty;

            // UNITY ADAPTER payload for BrigadeOrder_KeepPositions::Process.
            // It belongs to this concrete CBrigadeOrder node, not to the brigade.
            // This is required because original Type 1/2 order chaining can leave
            // more than one KeepPositions object in NewBOrder->Next.
            public bool KeepPositionsProcessorActive;
            public int KeepPositionsStartTick;
            public byte KeepPositionsPriority;
            public byte KeepPositionsOrdType;

            public BrigadeGlobalMoveV419LikeOriginal GlobalMove;

            public BrigadeNewOrderNodeV418LikeOriginal Next;
        }

        private static readonly Dictionary<int, BrigadeNewOrderNodeV418LikeOriginal>
            _brigadeNewBOrderV418LikeOriginal =
                new Dictionary<int, BrigadeNewOrderNodeV418LikeOriginal>();
        private static int _brigadeNewBOrderSerialV418LikeOriginal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallBrigadeNewOrderV418LikeOriginal()
        {
            _brigadeNewBOrderV418LikeOriginal.Clear();
            _brigadeNewBOrderSerialV418LikeOriginal = 0;
            Debug.Log("[C2:NEWBORDER V418] installed source=COSSACKS2/Brigade.cpp::CreateNewBOrder/DeleteNewBOrder/ClearNewBOrders chain=head+Next");
        }

        private static int CreateBrigadeNewOrderV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            byte orderId,
            byte createType,
            byte priority,
            string source)
        {
            if (group == null || orderId == BrigadeOrderNoneV418LikeOriginal) return 0;

            BrigadeNewOrderNodeV418LikeOriginal head;
            _brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head);

            BrigadeNewOrderNodeV418LikeOriginal node = new BrigadeNewOrderNodeV418LikeOriginal();
            node.OrderId = orderId;
            node.Priority = priority;
            node.CreateType = createType;
            node.Serial = ++_brigadeNewBOrderSerialV418LikeOriginal;
            node.Source = source ?? string.Empty;

            if (createType == 1)
            {
                node.Next = head;
                _brigadeNewBOrderV418LikeOriginal[group.GroupId] = node;
            }
            else if (createType == 2)
            {
                if (head == null)
                {
                    _brigadeNewBOrderV418LikeOriginal[group.GroupId] = node;
                }
                else
                {
                    BrigadeNewOrderNodeV418LikeOriginal tail = head;
                    while (tail.Next != null) tail = tail.Next;
                    tail.Next = node;
                }
            }
            else
            {
                // C++ ClearNewBOrders destroys the complete previous chain before
                // installing the replacement. Destroy the managed processor payloads
                // too, otherwise a stale implementation dictionary can become a second
                // gameplay truth after the new head is installed.
                DisposeBrigadeNewOrderPayloadsForReplacementV418LikeOriginal(
                    group, source ?? "CreateNewBOrder_replace");
                _brigadeNewBOrderV418LikeOriginal[group.GroupId] = node;
            }

            return node.Serial;
        }


        private static void DisposeBrigadeNewOrderPayloadsForReplacementV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return;

            // CBrigadeOrder destructors are implementation-specific. The Unity bridge
            // keeps their payloads in separate processors, so Type 0 replacement must
            // destroy those payloads while NewBOrder remains the only gameplay state.
            // Type-0 replacement destroys the old CBrigadeOrder object, not only
            // its active flag. The managed BITVA payload therefore must disappear too.
            _brigadeBattlesV407LikeOriginal.Remove(group.GroupId);

            _brigadeAttackKeepPositionsActiveV415LikeOriginal.Remove(group.GroupId);
            _brigadeAttackKeepPositionsStartTickV415LikeOriginal.Remove(group.GroupId);
            _brigadeKeepPositionsPriorityV416LikeOriginal.Remove(group.GroupId);
            _brigadeKeepPositionsOrdTypeV416LikeOriginal.Remove(group.GroupId);

            if (_roadOrdersV385A.ContainsKey(group.GroupId))
                CancelBrigadeGoOnRoadV385ALikeOriginal(
                    group.GroupId, source ?? "CreateNewBOrder_replace", false);

            if (group.Units != null && group.Units.Count > 0)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal representative = null;
                for (int i = 0; i < group.Units.Count && representative == null; i++)
                    if (group.Units[i] != null) representative = group.Units[i];
                if (representative != null)
                    C2BrigadeRifleAttackV405LikeOriginal.OnExternalBrigadeOrderReplacementLikeOriginal(
                        representative, source ?? "CreateNewBOrder_replace");
            }
        }

        private static bool DeleteBrigadeNewOrderV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            byte expectedOrderId,
            string source)
        {
            if (group == null) return false;
            BrigadeNewOrderNodeV418LikeOriginal head;
            if (!_brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head) || head == null)
                return false;
            if (expectedOrderId != BrigadeOrderNoneV418LikeOriginal && head.OrderId != expectedOrderId)
                return false;

            if (head.Next != null) _brigadeNewBOrderV418LikeOriginal[group.GroupId] = head.Next;
            else _brigadeNewBOrderV418LikeOriginal.Remove(group.GroupId);

            // C++ DeleteNewBOrder() makes Next the new current order immediately.
            // Restore the Unity processor payload that belongs to that exact node.
            RestoreCurrentKeepPositionsProcessorV418LikeOriginal(group);
            return true;
        }

        private static BrigadeNewOrderNodeV418LikeOriginal FindBrigadeNewOrderNodeBySerialV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group, int serial)
        {
            if (group == null || serial <= 0) return null;
            BrigadeNewOrderNodeV418LikeOriginal head;
            if (!_brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head)) return null;
            for (BrigadeNewOrderNodeV418LikeOriginal p = head; p != null; p = p.Next)
                if (p.Serial == serial) return p;
            return null;
        }

        private static void BindKeepPositionsProcessorToOrderV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            int serial,
            int startTick,
            byte priority,
            byte ordType)
        {
            BrigadeNewOrderNodeV418LikeOriginal node =
                FindBrigadeNewOrderNodeBySerialV418LikeOriginal(group, serial);
            if (node == null || node.OrderId != BrigadeOrderKeepPositionsV418LikeOriginal) return;

            node.KeepPositionsProcessorActive = true;
            node.KeepPositionsStartTick = startTick;
            node.KeepPositionsPriority = priority;
            node.KeepPositionsOrdType = ordType;

            // Type 2 can append a non-current order. The original processor does not
            // run until that node reaches BR->NewBOrder, so do not expose its payload
            // through the group-level Unity scheduler before then.
            BrigadeNewOrderNodeV418LikeOriginal head;
            if (_brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head) &&
                object.ReferenceEquals(head, node))
                RestoreCurrentKeepPositionsProcessorV418LikeOriginal(group);
        }

        private static void RestoreCurrentKeepPositionsProcessorV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return;

            _brigadeAttackKeepPositionsActiveV415LikeOriginal.Remove(group.GroupId);
            _brigadeAttackKeepPositionsStartTickV415LikeOriginal.Remove(group.GroupId);
            _brigadeKeepPositionsPriorityV416LikeOriginal.Remove(group.GroupId);
            _brigadeKeepPositionsOrdTypeV416LikeOriginal.Remove(group.GroupId);

            BrigadeNewOrderNodeV418LikeOriginal head;
            if (!_brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head) ||
                head == null ||
                head.OrderId != BrigadeOrderKeepPositionsV418LikeOriginal ||
                !head.KeepPositionsProcessorActive)
                return;

            _brigadeAttackKeepPositionsActiveV415LikeOriginal.Add(group.GroupId);
            _brigadeAttackKeepPositionsStartTickV415LikeOriginal[group.GroupId] =
                head.KeepPositionsStartTick;
            _brigadeKeepPositionsPriorityV416LikeOriginal[group.GroupId] =
                head.KeepPositionsPriority;
            _brigadeKeepPositionsOrdTypeV416LikeOriginal[group.GroupId] =
                head.KeepPositionsOrdType;
        }

        private static void ClearBrigadeNewOrdersV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return;
            DisposeBrigadeNewOrderPayloadsForReplacementV418LikeOriginal(
                group, source ?? "ClearNewBOrders");
            _brigadeNewBOrderV418LikeOriginal.Remove(group.GroupId);
        }

        internal static byte GetCurrentBrigadeNewOrderIdV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return BrigadeOrderNoneV418LikeOriginal;
            BrigadeNewOrderNodeV418LikeOriginal head;
            return _brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head) && head != null
                ? head.OrderId
                : BrigadeOrderNoneV418LikeOriginal;
        }

        internal static byte GetCurrentBrigadeNewOrderIdV418LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null
                ? GetCurrentBrigadeNewOrderIdV418LikeOriginal(group)
                : BrigadeOrderNoneV418LikeOriginal;
        }

        internal static bool IsCurrentBrigadeNewOrderV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            byte orderId)
        {
            return GetCurrentBrigadeNewOrderIdV418LikeOriginal(group) == orderId;
        }

        internal static bool IsCurrentBrigadeNewOrderV418LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            byte orderId)
        {
            return GetCurrentBrigadeNewOrderIdV418LikeOriginal(unit) == orderId;
        }

        internal static bool HasCurrentBrigadeNewOrderV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            return GetCurrentBrigadeNewOrderIdV418LikeOriginal(group) != BrigadeOrderNoneV418LikeOriginal;
        }

        // UNITY ADAPTER payload priority. This is the Prio carried by concrete
        // movement processors such as HumanGlobalSendTo/GoOnRoad and is NOT the
        // virtual CBrigadeOrder::GetBrigadeOrderPrio() result. CII keeps these as
        // separate concepts.
        internal static byte GetCurrentBrigadeNewOrderPriorityV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return 0;
            BrigadeNewOrderNodeV418LikeOriginal head;
            return _brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head) && head != null
                ? head.Priority
                : (byte)0;
        }

        // COSSACKS2/BrigadeOrders.cpp::CBrigadeOrder::GetBrigadeOrderPrio().
        // In the supplied C2 1.1 engine source the base implementation returns 0
        // and none of BrigadeOrder_RifleAttack/GoOnRoad/Bitva/KeepPositions/
        // HumanGlobalSendTo overrides it. AttackObj must read this semantic value,
        // never the concrete order's payload Prio above.
        internal static byte GetCurrentBrigadeOrderPrioV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return 0;
            return 0;
        }

        internal static byte GetCurrentBrigadeOrderPrioV418LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null
                ? GetCurrentBrigadeOrderPrioV418LikeOriginal(group)
                : (byte)0;
        }

        internal static byte GetCurrentBrigadeNewOrderCreateTypeV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return 0;
            BrigadeNewOrderNodeV418LikeOriginal head;
            return _brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head) && head != null
                ? head.CreateType
                : (byte)0;
        }


        internal static bool CreateBrigadeNewOrderForRifleV418LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null) return false;
            CreateBrigadeNewOrderV418LikeOriginal(
                group, BrigadeOrderRifleAttackV418LikeOriginal, 1, 0,
                source ?? "BrigadeRifleAttack");
            return true;
        }

        internal static bool DeleteBrigadeNewOrderForRifleV418LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null) return false;
            return DeleteBrigadeNewOrderV418LikeOriginal(
                group, BrigadeOrderRifleAttackV418LikeOriginal, source);
        }

        internal static int GetBrigadeNewOrderDepthV418LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return 0;
            BrigadeNewOrderNodeV418LikeOriginal head;
            if (!_brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out head)) return 0;
            int depth = 0;
            for (BrigadeNewOrderNodeV418LikeOriginal p = head; p != null; p = p.Next) depth++;
            return depth;
        }
    }
}
