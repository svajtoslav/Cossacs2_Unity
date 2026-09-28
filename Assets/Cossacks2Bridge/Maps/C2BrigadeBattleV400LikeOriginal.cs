using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V407 combat cleanup.
    // Algorithm source (kept in the same order/units as retail CII 1.1):
    //   COSSACKS2/Brigade.cpp::Brigade::Bitva / CheckAttDist
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_Bitva::{ctor,AddEnemXY,Process}
    //   COSSACKS2/Multi.cpp::SetEnemyForBrigade
    //   COSSACKS2/AttackList.cpp::AttackList::GetNAttackers
    // Unity adapters are restricted to Group[]/Serial, movement and LocalOrder storage.
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        private sealed class EnemyEntryV407LikeOriginal
        {
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public int InstanceSerial;
            public byte Danger;
        }

        private sealed class BrigadeBattleStateV407LikeOriginal
        {
            public int GroupId;
            public bool Active;
            public int StartTop = 0xFFFF;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
            public readonly byte[] BitMask = new byte[8192]; // 65536 OneObject indices.
            public readonly List<EnemyEntryV407LikeOriginal> Enemies = new List<EnemyEntryV407LikeOriginal>(512);
            public float NextProcessAt;
            public string Source = string.Empty;
        }

        internal sealed class MeleeMdTraitsV407LikeOriginal
        {
            public int MaxAttackers = 12; // NewMonster constructor default.
            public int ArmRadius = 100;    // NewMonster constructor default.
            // NewMonster::VisRange is SEARCH_ENEMY_RADIUS << 4. Keep the MD value
            // in original pixels here and convert to Real units at SearchVictim.
            public int SearchEnemyRadiusPixels;
            public int BrigadeWaitingCycles; // 0 => EngSettings.BrigadeWaitingCycles (40).
            public byte KillMask;
            public byte MathMask;
            public byte LockType;
            public bool Artilery;
            public bool Mortira;
            public bool DontAnswerOnAttack;
            public bool Priest;
            public bool IsPushka;
        }

        private static readonly Dictionary<int, BrigadeBattleStateV407LikeOriginal> _brigadeBattlesV407LikeOriginal =
            new Dictionary<int, BrigadeBattleStateV407LikeOriginal>();
        private static readonly Dictionary<int, int> _onlyThisBrigadeToKillV407LikeOriginal =
            new Dictionary<int, int>();
        // Brigade::AttEnm. This is formation intent, separate from an active BITVA order.
        // AttackSelected sets it before HumanLocalSendTo; BITVA is created later by
        // OneObject::AttackObj/SearchVictim/contact, not at command issue time.
        private static readonly HashSet<int> _brigadeAttackEnemyIntentV414LikeOriginal =
            new HashSet<int>();
        // AttackSelected sets BR->AttEnm BEFORE HumanLocalSendTo(...,Prio=128).
        // Keep this transient marker only while the managed HumanLocalSendTo adapter
        // is issuing that one charge move, so ordinary later RMB moves still clear AttEnm.
        private static readonly HashSet<int> _brigadeAttackMoveIssuanceV414LikeOriginal =
            new HashSet<int>();
        // BR->AttEnm and BR->NewBOrder are independent in C2. Track the concrete
        // KeepPositions order separately so BITVA can replace it without the stale
        // AttEnm flag resurrecting KeepPositions after battle ends.
        private static readonly HashSet<int> _brigadeAttackKeepPositionsActiveV415LikeOriginal =
            new HashSet<int>();
        private static readonly Dictionary<int, int> _brigadeAttackKeepPositionsStartTickV415LikeOriginal =
            new Dictionary<int, int>();
        // V416: the same managed NewBOrder now represents ordinary HumanLocalSendTo
        // as well as AttackSelected's charge. CII stores the priority on the
        // BrigadeOrder_KeepPositions object; keep it per brigade instead of assuming 128.
        private static readonly Dictionary<int, byte> _brigadeKeepPositionsPriorityV416LikeOriginal =
            new Dictionary<int, byte>();
        private static readonly Dictionary<int, byte> _brigadeKeepPositionsOrdTypeV416LikeOriginal =
            new Dictionary<int, byte>();
        private static readonly Dictionary<string, MeleeMdTraitsV407LikeOriginal> _meleeMdTraitsV407LikeOriginal =
            new Dictionary<string, MeleeMdTraitsV407LikeOriginal>(StringComparer.OrdinalIgnoreCase);

        private const int BattleEnemyLimitV407LikeOriginal = 512;
        private const int CheckAttDistPixelsV407LikeOriginal = 800;
        private const int ForcedEnemyPixelsV407LikeOriginal = 300;

        public static byte GetMeleeAttackDestinationDirectionV406LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative,
            C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative)
        {
            RuntimeFormationV172LikeOriginal sourceGroup;
            RuntimeFormationV172LikeOriginal enemyGroup;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(sourceRepresentative, out sourceGroup) || sourceGroup == null)
                return 0;
            byte dest = sourceGroup.Direction;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(enemyRepresentative, out enemyGroup) || enemyGroup == null)
                return dest;

            if (IsLineFormationV407LikeOriginal(sourceGroup) && IsLineFormationV407LikeOriginal(enemyGroup))
            {
                int same = Math.Abs((int)unchecked((sbyte)(enemyGroup.Direction - sourceGroup.Direction)));
                if (same < 38) dest = enemyGroup.Direction;
                else
                {
                    int opposite = Math.Abs((int)unchecked((sbyte)(enemyGroup.Direction - sourceGroup.Direction - 128)));
                    if (opposite < 38) dest = unchecked((byte)(enemyGroup.Direction + 128));
                }
            }
            return dest;
        }

        public static bool TryGetFormationCenterRealV406LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            out float realX,
            out float realY)
        {
            realX = 0.0f;
            realY = 0.0f;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(representative, out group) || group == null)
                return false;
            ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out realX, out realY);
            return true;
        }

        private static bool IsLineFormationV407LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return false;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                option != null ? option.OrderTemplate : null;
            return order != null && order.SymInv != null && order.Sym4i == null;
        }

        // COSSACKS2/Brigade.cpp::Brigade::HumanLocalSendTo used by
        // Multi.cpp::AttackSelected.  This deliberately does NOT use the generic
        // HumanGlobalSendTo/shared-center path: retail creates fresh ordered places
        // and immediately installs BrigadeOrder_KeepPositions over those places.
        public static bool IssueBrigadeAttackHumanLocalSendToV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative,
            float destRealX,
            float destRealY,
            byte requestedDirection,
            out int issued,
            out string audit)
        {
            issued = 0;
            audit = "invalid_group";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(sourceRepresentative, out group) || group == null)
                return false;

            // Multi.cpp::AttackSelected reaches the same Brigade::HumanLocalSendTo
            // as ComRotateBrigade and the RR<R0 HumanGlobalSendTo branch.  Do not
            // maintain a second managed copy of its turn/membership algorithm.
            return IssueBrigadeHumanLocalSendToV4183LikeOriginal(
                group,
                destRealX,
                destRealY,
                requestedDirection,
                128,
                0,
                "Brigade.cpp::HumanLocalSendTo_AttackSelected_V4183",
                out issued,
                out audit);
        }

        public static bool PrepareBrigadeMeleeAttackMoveV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative,
            C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative,
            bool onlyThisEnemyBrigade,
            string source)
        {
            RuntimeFormationV172LikeOriginal sourceGroup;
            RuntimeFormationV172LikeOriginal enemyGroup;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(sourceRepresentative, out sourceGroup) || sourceGroup == null ||
                !TryGetRuntimeGroupByUnitV172LikeOriginal(enemyRepresentative, out enemyGroup) || enemyGroup == null ||
                sourceGroup.GroupId == enemyGroup.GroupId || sourceGroup.Nation == enemyGroup.Nation)
                return false;

            // Multi.cpp::AttackSelected exact order:
            //   SetEnemyForBrigade(...); ... BR->AttEnm=1; HumanLocalSendTo(...,128,0)
            // State/stand-ground changes happen only AFTER HumanLocalSendTo.
            SetEnemyForBrigadeV407LikeOriginal(
                sourceGroup, onlyThisEnemyBrigade ? enemyGroup.GroupId : -1,
                enemyRepresentative.CombatNationLikeOriginal);
            _brigadeAttackEnemyIntentV414LikeOriginal.Add(sourceGroup.GroupId);
            _brigadeAttackMoveIssuanceV414LikeOriginal.Add(sourceGroup.GroupId);
            return true;
        }

        public static bool FinishBrigadeMeleeAttackMoveV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative,
            C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative,
            bool onlyThisEnemyBrigade,
            string source)
        {
            RuntimeFormationV172LikeOriginal sourceGroup;
            RuntimeFormationV172LikeOriginal enemyGroup;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(sourceRepresentative, out sourceGroup) || sourceGroup == null ||
                !TryGetRuntimeGroupByUnitV172LikeOriginal(enemyRepresentative, out enemyGroup) || enemyGroup == null ||
                sourceGroup.GroupId == enemyGroup.GroupId || sourceGroup.Nation == enemyGroup.Nation)
                return false;

            _brigadeAttackMoveIssuanceV414LikeOriginal.Remove(sourceGroup.GroupId);
            _brigadeAttackEnemyIntentV414LikeOriginal.Add(sourceGroup.GroupId);

            // Exact tail after HumanLocalSendTo in Multi.cpp::AttackSelected.
            BrigadeStandGroundStateV403LikeOriginal attackStateBridge =
                GetStandGroundStateV403LikeOriginal(sourceGroup, true);
            if (attackStateBridge != null) attackStateBridge.LastOrderTime = int.MinValue;
            SetAttStateV403LikeOriginal(sourceRepresentative, false);
            SetStandStateV403LikeOriginal(sourceRepresentative, 1);
            if (attackStateBridge != null) attackStateBridge.LastOrderTime = int.MinValue;
            CancelStandGroundV403LikeOriginal(sourceRepresentative, "Multi.cpp::AttackSelected_CancelStandGround");

            // AttackSelected writes GroundState=NewState=1 only for ArmAttack soldiers
            // (NBPERSONAL..NMemb). The managed helper performs exactly that subset and
            // also selects the equivalent armed-motion posture in the animation bridge.
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> orderedMembers =
                GetFormationOrderMembersV360LikeOriginal(sourceGroup);
            int commandPrefix = ResolveOrderCommandCountV360LikeOriginal(sourceGroup, orderedMembers);
            ApplyAttackStateMoveToFormationV405BLikeOriginal(orderedMembers, commandPrefix, true);

            // The source repeats SetEnemyForBrigade after CancelStandGround.
            SetEnemyForBrigadeV407LikeOriginal(
                sourceGroup, onlyThisEnemyBrigade ? enemyGroup.GroupId : -1,
                enemyRepresentative.CombatNationLikeOriginal);
            return true;
        }

        public static void AbortBrigadeMeleeAttackMoveV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative)
        {
            RuntimeFormationV172LikeOriginal sourceGroup;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(sourceRepresentative, out sourceGroup) || sourceGroup == null)
                return;
            _brigadeAttackMoveIssuanceV414LikeOriginal.Remove(sourceGroup.GroupId);
            ClearBrigadeAttackEnemyIntentV414LikeOriginal(sourceGroup);
        }

        // Compatibility entry point. New V414 command code calls Prepare ->
        // HumanLocalSendTo adapter -> Finish so ordering remains C2 1.1 exact.
        public static bool BeginBrigadeMeleeAttackV406LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal sourceRepresentative,
            C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative,
            bool onlyThisEnemyBrigade,
            string source)
        {
            if (!PrepareBrigadeMeleeAttackMoveV414LikeOriginal(
                    sourceRepresentative, enemyRepresentative, onlyThisEnemyBrigade, source))
                return false;
            return FinishBrigadeMeleeAttackMoveV414LikeOriginal(
                sourceRepresentative, enemyRepresentative, onlyThisEnemyBrigade, source);
        }

        internal static bool IsBrigadeAttackMoveIssuanceV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null &&
                   _brigadeAttackMoveIssuanceV414LikeOriginal.Contains(group.GroupId);
        }

        // NewMon.cpp::AttackObjLink: living melee victim answers attacker.  A loose
        // unit gets a direct AttackObj; an InArmy victim enters Brigade::Bitva.
        public static void OnMeleeDamageRetaliationV406LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker)
        {
            if (!IsAliveBattleUnitV407LikeOriginal(victim) || !IsAliveBattleUnitV407LikeOriginal(attacker) ||
                victim.CombatNationLikeOriginal == attacker.CombatNationLikeOriginal)
                return;

            MeleeMdTraitsV407LikeOriginal vt = GetMeleeMdTraitsV407LikeOriginal(victim);
            if (vt.IsPushka || vt.Priest || vt.DontAnswerOnAttack) return;

            RuntimeFormationV172LikeOriginal victimGroup;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(victim, out victimGroup) || victimGroup == null)
            {
                BeginOrKeepMeleeAttackObjV407LikeOriginal(victim, attacker, true, "NewMon.cpp::AttackObjLink_answer");
                return;
            }

            BrigadeBattleStateV407LikeOriginal state;
            if (!_brigadeBattlesV407LikeOriginal.TryGetValue(victimGroup.GroupId, out state) || state == null || !state.Active)
                state = CreateBrigadeBitvaV407LikeOriginal(victimGroup, "NewMon.cpp::AttackObj_InArmy_Bitva");
            // Brigade::Bitva only installs the brigade order. Its Process executes
            // on the normal brigade tick, not recursively from AttackObjLink.
        }

        public static void TickBrigadeBattleV406LikeOriginal()
        {
            float now = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
            TickPendingBrigadeAttackKeepPositionsV414LikeOriginal();
            TickPendingBrigadeAttackSearchV414LikeOriginal();
            if (_brigadeBattlesV407LikeOriginal.Count == 0) return;
            List<int> stale = null;
            foreach (KeyValuePair<int, BrigadeBattleStateV407LikeOriginal> pair in _brigadeBattlesV407LikeOriginal)
            {
                BrigadeBattleStateV407LikeOriginal state = pair.Value;
                if (state == null || !state.Active || now < state.NextProcessAt) continue;
                RuntimeFormationV172LikeOriginal group;
                if (!_groupsByIdV172LikeOriginal.TryGetValue(pair.Key, out group) || group == null)
                {
                    if (stale == null) stale = new List<int>();
                    stale.Add(pair.Key);
                    continue;
                }
                state.NextProcessAt = now + 0.04f; // current 25-Hz simulation adapter.
                ProcessBrigadeBitvaV407LikeOriginal(group, state);
            }
            if (stale != null)
                for (int i = 0; i < stale.Count; i++) _brigadeBattlesV407LikeOriginal.Remove(stale[i]);
        }

        internal static bool HasActiveBrigadeBitvaV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            BrigadeBattleStateV407LikeOriginal state;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null &&
                   IsCurrentBrigadeNewOrderV418LikeOriginal(group, BrigadeOrderBitvaV418LikeOriginal) &&
                   _brigadeBattlesV407LikeOriginal.TryGetValue(group.GroupId, out state) &&
                   state != null && state.Active;
        }

        // NewMon.cpp::AttackObj asks the real current BR->NewBOrder. V418 must not
        // infer this from historical stand-ground flags, TurnActive, road flags or
        // implementation dictionaries. Those are processor storage, not NewBOrder.
        internal static bool HasManagedNonBitvaBrigadeOrderV415LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            byte id = GetCurrentBrigadeNewOrderIdV418LikeOriginal(group);
            return id != BrigadeOrderNoneV418LikeOriginal && id != BrigadeOrderBitvaV418LikeOriginal;
        }

        private static bool CanBreakLocalOrderV415LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return false;
            C2UnitOrderRuntimeV325LikeOriginal order =
                C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(unit);
            // Brigade.cpp::CheckIfPossibleToBreakOrder returns false only for
            // NewAttackPointLink. V408 maps that native order to PreciseAttack.
            return order == null ||
                   order.CurrentLikeOriginal != C2UnitOrderKindV325LikeOriginal.PreciseAttack;
        }

// V432 integration probe: declaration supplied by consolidated file.


        internal static bool HasBrigadeAttackEnemyIntentV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null &&
                   _brigadeAttackEnemyIntentV414LikeOriginal.Contains(group.GroupId);
        }

        internal static bool ActivateBrigadeBitvaFromAttackObjV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            BrigadeBattleStateV407LikeOriginal state;
            if (_brigadeBattlesV407LikeOriginal.TryGetValue(group.GroupId, out state) &&
                state != null && state.Active)
                return true;
            state = CreateBrigadeBitvaV407LikeOriginal(group, source ?? "Brigade::Bitva");
            if (state == null) return false;
            // COSSACKS2 Brigade::Bitva -> CreateNewBOrder(1,Bt): BITVA is pushed
            // above the previous order. KeepPositions/Road/Rifle may remain in Next
            // and resume after DeleteNewBOrder(). Do not destroy the underlying state.
            state.NextProcessAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + 0.04f;
            return true;
        }

        // Multi.cpp::MoveBrigadeForwardToAttack, no enemy branch:
        // AttEnm=true; ArmAttack members GroundState=NewState=1;
        // KeepPositions(0,129). KARE returns before setting brigade intent.
        internal static void PrepareMeleeStandWithoutEnemyV423LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(representative, out group) || group == null) return;
            var order = ResolveStandGroundOrderV403LikeOriginal(group);
            if (order == null || order.Usage == 2) return;
            _brigadeAttackEnemyIntentV414LikeOriginal.Add(group.GroupId);
            var members = GetFormationOrderMembersV360LikeOriginal(group);
            int prefix = ResolveOrderCommandCountV360LikeOriginal(group, members);
            for (int i = prefix; i < members.Count; i++)
            {
                var u = members[i]; if (!IsAliveBattleUnitV407LikeOriginal(u)) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(u);
                if (!u.ArmAttackCapableV396LikeOriginal) continue;
                u.GroundStateV396LikeOriginal = u.NewStateV396LikeOriginal = 1;
            }
            QueueBrigadeKeepPositionsV416LikeOriginal(group, 129, 0, "MoveBrigadeForwardToAttack_no_enemy");
            GetStandGroundStateV403LikeOriginal(group, true).LastOrderTime = _standGroundRealtimeV403LikeOriginal;
        }

        private static void ClearBrigadeAttackEnemyIntentV414LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return;
            _brigadeAttackEnemyIntentV414LikeOriginal.Remove(group.GroupId);
            _brigadeAttackMoveIssuanceV414LikeOriginal.Remove(group.GroupId);
            _onlyThisBrigadeToKillV407LikeOriginal.Remove(group.GroupId);
            // AttEnm is independent from BR->NewBOrder in CII. Clearing attack intent
            // must never erase the brigade-order chain. A replacing order performs its
            // own CreateNewBOrder(Type=0) destruction.
            // Do not deactivate BITVA here. AttEnm and NewBOrder are independent in
            // the original. A subsequent Type-0 brigade order will destroy the old
            // order through CreateNewBOrder; a pure AttEnm clear must not create a
            // dead BITVA head.
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (u != null) u.SearchOnlyThisBrigadeToKillV407LikeOriginal = -1;
            }
        }

        internal static void SetRifleAttackIntentV434LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            // Multi.cpp::SetArmAttackState(129): setting RifleAttack also sets
            // BR->AttEnm. Switching the rifle off does not clear brigade intent.
            RuntimeFormationV172LikeOriginal group;
            if (TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null)
                _brigadeAttackEnemyIntentV414LikeOriginal.Add(group.GroupId);
        }

        // Brigade.cpp::Brigade::KeepPositions creates one brigade NewBOrder and
        // lets that order issue member NewMonsterPreciseSendTo commands. HumanLocalSendTo
        // itself never fans out 120 independent Unity paths. This queue is shared by
        // ordinary local movement and AttackSelected charge movement.
        private static int QueueBrigadeKeepPositionsV416LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            byte priority,
            byte ordType,
            string source)
        {
            if (group == null) return 0;

            // Brigade::KeepPositions -> CreateNewBOrder(OrdType,KP). Preserve the
            // exact head/Next semantics: type 1 pushes, type 2 appends, type 0 replaces.
            int keepOrderSerialV418 = CreateBrigadeNewOrderV418LikeOriginal(
                group, BrigadeOrderKeepPositionsV418LikeOriginal, ordType, priority,
                source ?? "Brigade::KeepPositions");

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members =
                GetFormationOrderMembersV360LikeOriginal(group);
            int accepted = 0;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = members[i];
                if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                accepted++;

                // Brigade::KeepPositions: clear breakable LocalOrder unless this is an
                // aggressive soldier already executing AttackObj. Do not pre-submit a
                // replacement move here; Process() owns the exact timing.
                C2NeutralPeasantUnitInfoV2LikeOriginal attackTarget;
                bool attackingAggressive = member.ActivityStateV413LikeOriginal == 2 &&
                    C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(
                        member, out attackTarget) && attackTarget != null;
                if (!attackingAggressive && CanBreakLocalOrderV415LikeOriginal(member))
                {
                    C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(member);
                    C2CombatRuntimeV334LikeOriginal combat =
                        member.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                    if (combat != null && combat.IsActiveOrderV350LikeOriginal)
                        combat.CancelForExternalOrderLikeOriginal(source ?? "Brigade::KeepPositions");
                }

                // Brigade::KeepPositions applies NewMonster::BRandomPos here,
                // after CreateNewBOrder and after breakable LocalOrder cleanup,
                // exactly once for this KeepPositions object.  Keeping this inside
                // KeepPositions prevents RMB/rotate/AttackSelected from each having
                // their own divergent random-position copy.
                if (!member.IsDeadLikeOriginal && i < group.Slots.Count)
                {
                    C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    int brp = runtime != null && runtime.Md != null
                        ? runtime.Md.BRandomPosLikeOriginal
                        : 0;
                    if (brp > 0)
                    {
                        int ox = brp / 2 - (C2RetailRandomV407LikeOriginal.Rando(member) % brp);
                        int oy = brp / 2 - (C2RetailRandomV407LikeOriginal.Rando(member) % brp);
                        Vector2 slot = group.Slots[i];
                        group.Slots[i] = new Vector2(
                            slot.x + ox * 16,
                            slot.y + oy * 16);
                    }
                }
            }

            BindKeepPositionsProcessorToOrderV418LikeOriginal(
                group, keepOrderSerialV418, CurrentSimulationTickV403ELikeOriginal,
                priority, ordType);
            return accepted;
        }

        private static void ProcessQueuedBrigadeKeepPositionsNowV416LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return;
            TickPendingBrigadeAttackKeepPositionsV414LikeOriginal(group.GroupId);
        }

        // BrigadeOrders.cpp::BrigadeOrder_KeepPositions::Process, attack-charge path.
        // This order is BR->NewBOrder and is deliberately tracked separately from
        // BR->AttEnm. A pushed BITVA suspends it in NewBOrder->Next; AttEnm never recreates it.
        private static void TickPendingBrigadeAttackKeepPositionsV414LikeOriginal(int onlyGroupIdV416 = -1)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.KeepPositions))
            {
            if (_brigadeAttackKeepPositionsActiveV415LikeOriginal.Count == 0) return;
            List<int> ids;
            if (onlyGroupIdV416 >= 0)
            {
                if (!_brigadeAttackKeepPositionsActiveV415LikeOriginal.Contains(onlyGroupIdV416)) return;
                ids = new List<int>(1) { onlyGroupIdV416 };
            }
            else ids = new List<int>(_brigadeAttackKeepPositionsActiveV415LikeOriginal);
            for (int gi = 0; gi < ids.Count; gi++)
            {
                RuntimeFormationV172LikeOriginal group;
                if (!_groupsByIdV172LikeOriginal.TryGetValue(ids[gi], out group) || group == null)
                {
                    _brigadeAttackKeepPositionsActiveV415LikeOriginal.Remove(ids[gi]);
                    _brigadeAttackKeepPositionsStartTickV415LikeOriginal.Remove(ids[gi]);
                    _brigadeKeepPositionsPriorityV416LikeOriginal.Remove(ids[gi]);
                    _brigadeKeepPositionsOrdTypeV416LikeOriginal.Remove(ids[gi]);
                    continue;
                }

                // Only BR->NewBOrder is processed. A pushed BITVA/Rifle/Road order
                // suspends KeepPositions without deleting it; DeleteNewBOrder resumes it.
                if (!IsCurrentBrigadeNewOrderV418LikeOriginal(
                        group, BrigadeOrderKeepPositionsV418LikeOriginal))
                    continue;

                // BR->Memb and posX/posY are parallel persistent arrays. All passes
                // below already reject dead/null slots; do not clone the roster or
                // search it again for each member on every simulation tick.
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> ordered = group.Units;
                int count = Mathf.Min(ordered.Count, group.Slots.Count);
                int commandPrefix = ResolveOrderCommandCountV360LikeOriginal(group, ordered);
                byte keepPriorityV416;
                if (!_brigadeKeepPositionsPriorityV416LikeOriginal.TryGetValue(group.GroupId, out keepPriorityV416))
                    keepPriorityV416 = 128;
                byte keepOrdTypeV416;
                if (!_brigadeKeepPositionsOrdTypeV416LikeOriginal.TryGetValue(group.GroupId, out keepOrdTypeV416))
                    keepOrdTypeV416 = 0;

                // BrigadeOrders.cpp::BrigadeOrder_KeepPositions::Process, pri==128.
                // HumanLocalSendTo from AttackSelected creates this exact-priority order.
                // Retail returns from Process immediately while any live member has a
                // personal AttackObj with ActivityState==0; a ready rifle member diverts
                // the brigade through BrigadeRifleAttack instead.  Do this BEFORE the
                // posture/slot pass so KeepPositions cannot fight a soldier's attack.
                bool deferPriority128 = false;
                if (keepPriorityV416 == 128)
                for (int p = 0; p < count; p++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = ordered[p];
                    if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;

                    C2NeutralPeasantUnitInfoV2LikeOriginal personalTarget;
                    if (member.ActivityStateV413LikeOriginal == 0 &&
                        C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(
                            member, out personalTarget) && personalTarget != null)
                    {
                        deferPriority128 = true;
                        break;
                    }

                    C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(member);
                    if (member.RifleAttackV396LikeOriginal)
                    {
                        int delay, maxDelay;
                        C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(
                            member, 1, out delay, out maxDelay);
                        if (delay == 0)
                        {
                            // BrigadeRifleAttack -> CreateNewBOrder(1,RA): push the
                            // rifle order over KeepPositions and preserve KP in Next.
                            C2BrigadeRifleAttackV405LikeOriginal.StartBrigadeRifleAttackV418LikeOriginal(
                                member, "BrigadeOrder_KeepPositions::BrigadeRifleAttack");
                            deferPriority128 = true;
                            break;
                        }
                    }
                }
                if (deferPriority128) continue;

                // BrigadeOrder_KeepPositions::Process pre-pass. State starts from
                // BR->AttEnm and can be overridden by member ActivityState. When
                // fewer than six soldiers are still moving, retail waits for the
                // remaining posture transitions (TryToStand) before the position pass.
                int keepState = _brigadeAttackEnemyIntentV414LikeOriginal.Contains(group.GroupId) ? 1 : 0;
                int immediate = 0;
                for (int p = commandPrefix; p < count; p++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = ordered[p];
                    if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                    C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    if (runtime != null &&
                        (runtime.HasMoveTargetLikeOriginal || runtime.State == C2UnitOriginalState.Motion))
                        immediate++;
                    if (member.ActivityStateV413LikeOriginal == 2) keepState = 1;
                    if (member.ActivityStateV413LikeOriginal == 1) keepState = 0;
                }

                int startTick;
                if (!_brigadeAttackKeepPositionsStartTickV415LikeOriginal.TryGetValue(group.GroupId, out startTick))
                    startTick = CurrentSimulationTickV403ELikeOriginal;
                int dt = Math.Max(0, CurrentSimulationTickV403ELikeOriginal - startTick);

                if (immediate < 6)
                {
                    bool ready = true;
                    int cycles = 0;
                    for (int p = commandPrefix; p < count; p++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal member = ordered[p];
                        if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                        MeleeMdTraitsV407LikeOriginal traits = GetMeleeMdTraitsV407LikeOriginal(member);
                        cycles = traits.BrigadeWaitingCycles;

                        C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
                        C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                        bool inMotion = runtime != null &&
                            (runtime.HasMoveTargetLikeOriginal || runtime.State == C2UnitOriginalState.Motion);
                        C2NeutralPeasantUnitInfoV2LikeOriginal attackTarget;
                        bool activityAttack = member.ActivityStateV413LikeOriginal == 2 &&
                            C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(member, out attackTarget) &&
                            attackTarget != null;
                        if (activityAttack || inMotion || !CanBreakLocalOrderV415LikeOriginal(member))
                            continue;

                        int slotIndex = p;
                        if (slotIndex < 0 || slotIndex >= group.Slots.Count) continue;
                        Vector2 slot = group.Slots[slotIndex];
                        int distance = C2OriginalMovementMathV352.Norma(
                            (Mathf.RoundToInt(RealXV407LikeOriginal(member)) >> 4) - (Mathf.RoundToInt(slot.x) >> 4),
                            (Mathf.RoundToInt(RealYV407LikeOriginal(member)) >> 4) - (Mathf.RoundToInt(slot.y) >> 4));
                        if (distance <= 5) continue;

                        if (link != null)
                        {
                            link.ApplyKeepPositionsGroundStateV415LikeOriginal(
                                keepState, "BrigadeOrder_KeepPositions_wait_v415");
                            if (!link.IsKeepPositionsPostureReadyV415LikeOriginal(keepState))
                                ready = false;
                        }
                        // BrigadeOrders.cpp only posture readiness changes Ready.
                        // The inline MinRotator turn below must NOT hold the whole
                        // brigade until every soldier faces Direction first.
                        AdvanceKeepPositionsFacingV415LikeOriginal(member, group.Direction);
                    }
                    if (cycles == 0) cycles = 40; // EngineSettings.h default.
                    if (dt < cycles && !ready)
                        continue;
                }

                bool done = true;
                int nfail = 0;
                int maxD = 0;

                for (int i = 0; i < count; i++)
                {
                    bool wasDone = done; // source PD=Done before processing the member.
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = ordered[i];
                    if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;

                    int slotIndex = i;
                    if (slotIndex < 0 || slotIndex >= group.Slots.Count) continue;
                    Vector2 slot = group.Slots[slotIndex];
                    int dd = C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt(slot.x - RealXV407LikeOriginal(member)),
                        Mathf.RoundToInt(slot.y - RealYV407LikeOriginal(member)));
                    if (i >= commandPrefix && dd > maxD) maxD = dd;

                    if (!CanBreakLocalOrderV415LikeOriginal(member))
                        continue; // source skips NewAttackPointLink members entirely.

                    C2NeutralPeasantUnitInfoV2LikeOriginal enemy;
                    bool hasEnemyOrder =
                        C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(member, out enemy) &&
                        enemy != null;
                    C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    bool hasMoveOrder = C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(member) ||
                        (runtime != null && (runtime.HasMoveTargetLikeOriginal ||
                         runtime.MoveDeferredUntilNeutralStandLikeOriginal ||
                         runtime.SingleStepRotateAtPlaceActiveV352LikeOriginal));

                    // pri==128 for AttackSelected/HumanLocalSendTo, so (pri&127)==0.
                    // Native KeepPositions enters its positioning branch only when
                    // OB->LocalOrder is absent. Movement and personal AttackObj are
                    // the two represented native LocalOrder families on this path.
                    // BrigadeOrders.cpp exact gate:
                    //   (!OB->LocalOrder) || (OB->EnemyID!=0xFFFF && (pri&127))
                    // For ordinary movement (priority 142) an existing AttackObj does
                    // not block KeepPositions from servicing the formation place. For
                    // AttackSelected priority 128, (pri&127)==0 and the personal
                    // AttackObj remains authoritative.
                    bool localOrderBlocksPositioning = hasMoveOrder ||
                        (hasEnemyOrder && (keepPriorityV416 & 127) == 0);
                    if (localOrderBlocksPositioning)
                    {
                        done = false;
                        nfail++;
                    }
                    else
                    {
                        // BrigadeOrders.cpp calls CheckMotionThroughEnemyAbility at the
                        // CURRENT position before deciding whether to walk back to slot.
                        // This may create AttackObj/Brigade::Bitva; if so ENMID!=FFFF and
                        // KeepPositions does not issue another movement order.
                        C2NeutralPeasantUnitInfoV2LikeOriginal contact =
                            CheckMotionThroughEnemyAbilityV414LikeOriginal(
                                member,
                                Mathf.RoundToInt(RealXV407LikeOriginal(member)),
                                Mathf.RoundToInt(RealYV407LikeOriginal(member)));
                        if (member.ActivityStateV413LikeOriginal == 2)
                        {
                            C2NeutralPeasantUnitInfoV2LikeOriginal activityTarget;
                            if (C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(
                                    member, out activityTarget) && activityTarget != null)
                                contact = activityTarget;
                        }
                        if (contact != null)
                        {
                            // Exact ENMID!=FFFF branch: leave Done/NFAIL unchanged.
                        }
                        else if (dd > 100)
                        {
                            bool doneBeforeDistanceFail = done;
                            done = false;
                            nfail++;
                            if (dd < 768)
                            {
                                if (!IsKeepPositionsSlotOccupiedV415LikeOriginal(member, slot))
                                {
                                    C2OriginalOrderChainV352.SubmitMove(
                                        member, slot.x, slot.y, false, group.Direction, 0,
                                        "BrigadeOrder_KeepPositions::NewMonsterPreciseSendTo_v416", true, keepPriorityV416);
                                }
                                else
                                {
                                    bool rotating = AdvanceKeepPositionsFacingV415LikeOriginal(member, group.Direction);
                                    if (!rotating)
                                    {
                                        // Exact PDN restore in the occupied-slot branch.
                                        done = doneBeforeDistanceFail;
                                        nfail--;
                                    }
                                }
                            }
                            else
                            {
                                C2OriginalOrderChainV352.SubmitMove(
                                    member, slot.x, slot.y, false, group.Direction, 0,
                                    "BrigadeOrder_KeepPositions::NewMonsterPreciseSendTo_v416", true, keepPriorityV416);
                            }
                        }
                        else if (AdvanceKeepPositionsFacingV415LikeOriginal(member, group.Direction))
                        {
                            done = false;
                            nfail++;
                        }
                    }

                    // Preserve the retail KeepPositions random Done perturbation.
                    if (wasDone && !done && C2RetailRandomV407LikeOriginal.Rando(member) < 18000)
                        done = true;
                }

                // BR->DontWait is false for this player HumanLocalSendTo path.
                if (nfail < 4 || (maxD < 1600 && dt > 1200)) done = true;

                if (done)
                {
                    _brigadeAttackKeepPositionsActiveV415LikeOriginal.Remove(group.GroupId);
                    _brigadeAttackKeepPositionsStartTickV415LikeOriginal.Remove(group.GroupId);
                    _brigadeKeepPositionsPriorityV416LikeOriginal.Remove(group.GroupId);
                    _brigadeKeepPositionsOrdTypeV416LikeOriginal.Remove(group.GroupId);
                    // BrigadeOrder_KeepPositions::Process tail: DeleteNewBOrder();
                    // MakeStandGroundTemp(BR). AttEnm remains a separate brigade flag.
                    DeleteBrigadeNewOrderV418LikeOriginal(
                        group, BrigadeOrderKeepPositionsV418LikeOriginal,
                        "BrigadeOrder_KeepPositions::Process_done");
                    MakeStandGroundTempV403LikeOriginal(
                        group, "BrigadeOrder_KeepPositions::Process_done_attack_v415");
                }
            }
            }
        }

// V432 integration probe: declaration supplied by consolidated file.


        private static bool HasLiveUnitFootprintV420LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal excluded, int x, int y, int radius)
        {
            // The existing Lx=1 UnitsField adapter checks a small footprint, not
            // the whole Group[] table. Convert its EXACT integer bounds back to
            // Real coordinates, then visit only intersecting >>11 spatial cells.
            int x0 = (((x - radius) << 8) + 128) >> 11;
            int y0 = (((y - radius) << 8) + 128) >> 11;
            int x1 = (((x + radius + 1) << 8) + 127) >> 11;
            int y1 = (((y + radius + 1) << 8) + 127) >> 11;
            for (int cy = y0; cy <= y1; cy++)
            for (int cx = x0; cx <= x1; cx++)
            {
                var cell = C2LiveUnitCellIndex.GetCell(cx, cy);
                for (int i = 0; cell != null && i < cell.Count; i++)
                {
                    var unit = cell[i];
                    if (unit == null || unit == excluded) continue;
                    int ux = (Mathf.RoundToInt(RealXV407LikeOriginal(unit)) - 128) >> 8;
                    int uy = (Mathf.RoundToInt(RealYV407LikeOriginal(unit)) - 128) >> 8;
                    if (Mathf.Abs(ux - x) > radius || Mathf.Abs(uy - y) > radius) continue;
                    if (IsAliveBattleUnitV407LikeOriginal(unit) && !IsUnlimitedMotionV415LikeOriginal(unit)) return true;
                }
            }
            return false;
        }

        private static bool AdvanceKeepPositionsFacingV415LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal member, byte targetDirection)
        {
            if (member == null) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
            if (link == null || runtime == null || runtime.Md == null) return false;
            byte current = member.RealDir;
            sbyte dd = unchecked((sbyte)(targetDirection - current));
            int ad = Math.Abs((int)dd);
            if (ad == 0) return false;
            int md = Math.Max(1, runtime.Md.MinRotator);
            if (ad < md)
            {
                link.SetFacingDirectionLikeOriginal(targetDirection);
                return false;
            }
            byte next = dd > 0
                ? unchecked((byte)(current + md))
                : unchecked((byte)(current - md));
            link.SetFacingDirectionLikeOriginal(next);
            return true;
        }

        // NewMon.cpp::SearchVictim, the melee-relevant explicit-formation branch.
        // An ARMATTACK brigade with BR->AttEnm may search while KeepPositions is still
        // the active brigade order. The native scan eventually calls AttackObj(...,1)
        // when an arm target is within 100 original pixels; AttackObj then replaces
        // KeepPositions with Brigade::Bitva. Preserve that ordering here instead of
        // pre-creating BITVA at mouse-click time (the V413 regression).
        private static void TickPendingBrigadeAttackSearchV414LikeOriginal()
        {
            if (_brigadeAttackEnemyIntentV414LikeOriginal.Count == 0) return;

            // NewMon.cpp LongProcesses dispatch for player-controlled formation members:
            //   d=tmtmt&7;
            //   ... else if(((i&15)==d)) OB->SearchVictim();
            // `i` is the OneObject index, NOT nation or brigade id.
            int d = CurrentSimulationTickV403ELikeOriginal & 7;
            List<int> groups = new List<int>(_brigadeAttackEnemyIntentV414LikeOriginal);
            for (int gi = 0; gi < groups.Count; gi++)
            {
                RuntimeFormationV172LikeOriginal group;
                if (!_groupsByIdV172LikeOriginal.TryGetValue(groups[gi], out group) || group == null)
                    continue;

                BrigadeBattleStateV407LikeOriginal active;
                if (_brigadeBattlesV407LikeOriginal.TryGetValue(group.GroupId, out active) &&
                    active != null && active.Active)
                    continue; // SearchVictim returns for ARMATTACK + BRIGADEORDER_BITVA.

                int onlyGroup;
                if (!_onlyThisBrigadeToKillV407LikeOriginal.TryGetValue(group.GroupId, out onlyGroup))
                    onlyGroup = -1;
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> members =
                    GetFormationOrderMembersV360LikeOriginal(group);

                for (int mi = 0; mi < members.Count; mi++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = members[mi];
                    if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                    if ((member.C2ObjectIndexV408LikeOriginal & 15) != d) continue;

                    C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(member);
                    if (!member.ArmAttackCapableV396LikeOriginal) continue;
                    if (GetNoSearchVictimV403LikeOriginal(member)) continue;

                    // LongProcesses calls SearchVictim only when !OB->Attack.
                    C2NeutralPeasantUnitInfoV2LikeOriginal existing;
                    if (C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(member, out existing) &&
                        existing != null)
                        continue;

                    MeleeMdTraitsV407LikeOriginal mt = GetMeleeMdTraitsV407LikeOriginal(member);
                    if (mt.SearchEnemyRadiusPixels <= 0) continue;

                    int searchReal = mt.SearchEnemyRadiusPixels << 4; // NewMonster::VisRange.
                    // NewMon.cpp::SearchVictim: StandGround ARMATTACK infantry uses
                    // a hard 120-pixel victim-search radius. AttackSelected cancels
                    // StandGround before a player attack, but retain the native rule
                    // for every other path into this routine as well.
                    if (IsUnitStandGroundV403LikeOriginal(member) &&
                        !member.RifleAttackV396LikeOriginal)
                        searchReal = 120 * 16;
                    int rx1 = (searchReal >> 11) + 1;
                    // CONQUEST build in CII 1.1: short-range scan is throttled at rx1<=10.
                    if (rx1 <= 10 && C2RetailRandomV407LikeOriginal.Rando(member) > 8192) continue;

                    int minAttackPx, ignoredMaxAttackPx;
                    C2CombatCoreV408LikeOriginal.GetAttackRadiusEnvelopeV413LikeOriginal(
                        member, out minAttackPx, out ignoredMaxAttackPx);

                    C2NeutralPeasantUnitInfoV2LikeOriginal target =
                        SearchVictimShortRangeV414LikeOriginal(
                            member, onlyGroup, minAttackPx << 4, searchReal);
                    if (target == null) continue;

                    int distanceReal = C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt(RealXV407LikeOriginal(target) - RealXV407LikeOriginal(member)),
                        Mathf.RoundToInt(RealYV407LikeOriginal(target) - RealYV407LikeOriginal(member)));

                    // NewMon.cpp::SearchVictim's rifle branch must run even when
                    // the brigade has no BITVA/KeepPositions order (e.g. an idle
                    // line with the rifle button enabled). The melee-only port
                    // skipped these soldiers, leaving a ready volley armed forever.
                    int delayV434, maxDelayV434;
                    C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(
                        member, 1, out delayV434, out maxDelayV434);
                    if (member.RifleAttackV396LikeOriginal && delayV434 == 0)
                    {
                        if (C2OriginalOrderChainV352.GetLocalPriorityV434LikeOriginal(member) > 1)
                            continue;
                        if (C2CombatCoreV408LikeOriginal.UsesBrigadeRifleWeaponV434LikeOriginal(member))
                        {
                            if (C2CombatCoreV408LikeOriginal.CheckImmediateAttackAbilityV434LikeOriginal(member, target) == 1)
                                C2BrigadeRifleAttackV405LikeOriginal.StartBrigadeRifleAttackV418LikeOriginal(
                                    member, "NewMon.cpp::SearchVictim");
                        }
                        else
                            BeginOrKeepMeleeAttackObjV407LikeOriginal(member, target, true, "NewMon.cpp::SearchVictim");
                        // SearchVictim is dispatched per object. A blocked shot
                        // for this soldier must not suppress the other members.
                        continue;
                    }

                    // Only the unarmed-rifle branch has the 100-pixel melee gate.
                    if (member.RifleAttackV396LikeOriginal)
                    {
                        BeginOrKeepMeleeAttackObjV407LikeOriginal(member, target, true, "NewMon.cpp::SearchVictim_delayed_rifle");
                        continue;
                    }
                    if (distanceReal >= 100 * 16) continue;

                    bool precise;
                    C2CombatCoreV408LikeOriginal.TryAttackObjV408LikeOriginal(member, target, 1, out precise);
                    // InArmy AttackObj creates Brigade::Bitva and returns before a private
                    // AttackObj order. Once active, remaining members are serviced by BITVA.
                    if (HasActiveBrigadeBitvaV414LikeOriginal(member)) break;
                }
            }
        }

        // Exact decision semantics of the CONQUEST short-range branch used by ordinary
        // formation infantry in OneObject::SearchVictim / SearchEnemyInCell:
        // cells are exhaustive, each cell contributes the eligible enemy with the
        // smallest current attacker pressure, then the globally nearest cell winner wins.
        private static C2NeutralPeasantUnitInfoV2LikeOriginal SearchVictimShortRangeV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            int onlyGroup,
            int minDistanceReal,
            int searchRadiusReal)
        {
            if (attacker == null || searchRadiusReal <= 0) return null;
            int ax = Mathf.RoundToInt(RealXV407LikeOriginal(attacker));
            int ay = Mathf.RoundToInt(RealYV407LikeOriginal(attacker));
            int centerX = ax >> 11;
            int centerY = ay >> 11;
            int rx1 = (searchRadiusReal >> 11) + 1;

            MeleeMdTraitsV407LikeOriginal at = GetMeleeMdTraitsV407LikeOriginal(attacker);


            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            int bestDistance = int.MaxValue;
            for (int cy = centerY - rx1; cy <= centerY + rx1; cy++)
            {
                for (int cx = centerX - rx1; cx <= centerX + rx1; cx++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal cellBest = null;
                    int cellMinAttackers = int.MaxValue;
                    var all = C2LiveUnitCellIndex.GetCell(cx, cy);

                    for (int i = 0; all != null && i < all.Count; i++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal e = all[i];
                        if (!IsAliveBattleUnitV407LikeOriginal(e) || e == attacker ||
                            e.CombatNationLikeOriginal == attacker.CombatNationLikeOriginal)
                            continue;
                        if ((Mathf.RoundToInt(RealXV407LikeOriginal(e)) >> 11) != cx ||
                            (Mathf.RoundToInt(RealYV407LikeOriginal(e)) >> 11) != cy)
                            continue;

                        MeleeMdTraitsV407LikeOriginal et = GetMeleeMdTraitsV407LikeOriginal(e);
                        if ((et.MathMask & at.KillMask) == 0) continue;

                        int dist = C2OriginalMovementMathV352.Norma(
                            Mathf.RoundToInt(RealXV407LikeOriginal(e)) - ax,
                            Mathf.RoundToInt(RealYV407LikeOriginal(e)) - ay);
                        if (dist <= minDistanceReal) continue;

                        // SearchEnemyInCell(...,Brig): target brigade is preferred/restricted
                        // only beyond 100 px; any physically close enemy remains legal.
                        if (onlyGroup >= 0)
                        {
                            int eg;
                            bool inRequested = TryGetFormationGroupIdV321LikeOriginal(e, out eg) && eg == onlyGroup;
                            if (!inRequested && dist >= 100 * 16) continue;
                        }

                        int na = C2CombatCoreV408LikeOriginal.GetNAttackersV408LikeOriginal(e);
                        if (na < cellMinAttackers)
                        {
                            cellMinAttackers = na;
                            cellBest = e;
                        }
                    }

                    if (cellBest == null) continue;
                    int cellDistance = C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt(RealXV407LikeOriginal(cellBest)) - ax,
                        Mathf.RoundToInt(RealYV407LikeOriginal(cellBest)) - ay);
                    if (cellDistance < bestDistance)
                    {
                        bestDistance = cellDistance;
                        best = cellBest;
                    }
                }
            }

            return bestDistance < searchRadiusReal ? best : null;
        }

// V432 integration probe: declaration supplied by consolidated file.


        private static void SetEnemyForBrigadeV407LikeOriginal(
            RuntimeFormationV172LikeOriginal group, int enemyGroupId, int enemyNation)
        {
            if (group == null) return;
            int old = -1;
            _onlyThisBrigadeToKillV407LikeOriginal[group.GroupId] = enemyGroupId;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                member.SearchOnlyThisBrigadeToKillV407LikeOriginal = enemyGroupId;
                C2NeutralPeasantUnitInfoV2LikeOriginal target;
                if (C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(member, out target) && target != null)
                {
                    int gid;
                    if (TryGetFormationGroupIdV321LikeOriginal(target, out gid)) old = gid;
                }
            }
            // Multi.cpp::SetEnemyForBrigade checks BR->NewBOrder==BITVA but calls
            // the LEGACY BR->DeleteBOrder(), not DeleteNewBOrder(). V418 must not
            // reinterpret that line as permission to remove/deactivate NewBOrder.
            // The managed port has no parallel legacy BOrder payload here, so this is
            // intentionally a no-op for the NewBOrder chain.
            if (old != enemyGroupId && IsCurrentBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderBitvaV418LikeOriginal))
            {
                // ORIGINAL SEMANTICS PRESERVED: legacy DeleteBOrder has no managed
                // counterpart on this path; BR->NewBOrder remains untouched.
            }
        }

        private static BrigadeBattleStateV407LikeOriginal CreateBrigadeBitvaV407LikeOriginal(
            RuntimeFormationV172LikeOriginal group, string source)
        {
            if (group == null) return null;

            // Brigade::Bitva first line is authoritative: if the CURRENT
            // NewBOrder already is BITVA, return before checking ready rifles.
            if (IsCurrentBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderBitvaV418LikeOriginal))
            {
                BrigadeBattleStateV407LikeOriginal currentV418;
                _brigadeBattlesV407LikeOriginal.TryGetValue(group.GroupId, out currentV418);
                return currentV418;
            }

            // Only after that guard retail scans for a ready RifleAttack member.
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(member);
                if (!member.RifleAttackV396LikeOriginal) continue;
                int delay, maxDelay;
                C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(member, 1, out delay, out maxDelay);
                if (delay == 0)
                {
                    // Brigade::Bitva calls BrigadeRifleAttack before installing BITVA.
                    // BrigadeRifleAttack itself pushes RA with CreateNewBOrder(1,RA).
                    C2BrigadeRifleAttackV405LikeOriginal.StartBrigadeRifleAttackV418LikeOriginal(
                        member, "Brigade::Bitva::BrigadeRifleAttack");
                    return null;
                }
            }

            // Brigade::Bitva -> CreateNewBOrder(1,Bt). This is a PUSH, not replacement.
            CreateBrigadeNewOrderV418LikeOriginal(
                group, BrigadeOrderBitvaV418LikeOriginal, 1, 0,
                source ?? "Brigade::Bitva");

            BrigadeBattleStateV407LikeOriginal state = new BrigadeBattleStateV407LikeOriginal();
            state.GroupId = group.GroupId;
            state.Active = true;
            state.Source = source ?? "Brigade::Bitva";
            _brigadeBattlesV407LikeOriginal[group.GroupId] = state;
            if (group.Units.Count > 0) SetStandStateV403LikeOriginal(group.Units[0], 1);

            int top, minX, minY, maxX, maxY;
            if (!ComputeBattleBoundsV407LikeOriginal(group, out top, out minX, out minY, out maxX, out maxY))
            {
                state.Active = false;
                DeleteBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderBitvaV418LikeOriginal, "Brigade::Bitva_init_failed");
                return null;
            }

            // Brigade::Bitva initializes Bt->StartTop=0xFFFF and uses a LOCAL
            // Top only for the eager AddEnemXY fill. Therefore the first Process()
            // is forced through its range-rebuild branch even if rando()>=1024.
            state.StartTop = 0xFFFF;
            state.MinX = minX;
            state.MaxX = maxX;
            state.MinY = minY;
            state.MaxY = maxY;
            byte mask = unchecked((byte)(1 << Mathf.Clamp(group.Nation, 0, 7)));
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    AddEnemXYV407LikeOriginal(state, x, y, top, mask);
            return state;
        }

        private static bool ProcessBrigadeBitvaV407LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            BrigadeBattleStateV407LikeOriginal state)
        {
            if (group == null || state == null || !state.Active) return false;
            if (!IsCurrentBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderBitvaV418LikeOriginal))
                return true; // order is preserved in Next but is not the current BR->NewBOrder.
            C2RetailRandomV407LikeOriginal.AddRand(group.GroupId); // release addrand is intentionally NO-OP.

            // BrigadeOrder_Bitva::Process rifle escape hatch.
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(member);
                if (!member.RifleAttackV396LikeOriginal) continue;
                int delay, maxDelay;
                C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(member, 1, out delay, out maxDelay);
                if (delay == 0)
                {
                    // Exact BrigadeOrder_Bitva::Process: DeleteNewBOrder();
                    // BrigadeRifleAttack(OB); therefore the underlying order resumes
                    // before RA is pushed at the head.
                    state.Active = false;
                    DeleteBrigadeNewOrderV418LikeOriginal(
                        group, BrigadeOrderBitvaV418LikeOriginal,
                        "BrigadeOrder_Bitva::Process_rifle_escape");
                    C2BrigadeRifleAttackV405LikeOriginal.StartBrigadeRifleAttackV418LikeOriginal(
                        member, "BrigadeOrder_Bitva::Process::BrigadeRifleAttack");
                    return true;
                }
            }

            byte mask = unchecked((byte)(1 << Mathf.Clamp(group.Nation, 0, 7)));
            float centerRealX, centerRealY;
            ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out centerRealX, out centerRealY);
            int centerX = Mathf.RoundToInt(centerRealX / 16.0f);
            int centerY = Mathf.RoundToInt(centerRealY / 16.0f);

            // 1. Check/rebuild battle rectangle.  Consume exactly one rando().
            if (C2RetailRandomV407LikeOriginal.Rando(RepresentativeV407LikeOriginal(group)) < 1024 || state.StartTop == 0xFFFF)
            {
                int top, minX, minY, maxX, maxY;
                if (!ComputeBattleBoundsV407LikeOriginal(group, out top, out minX, out minY, out maxX, out maxY))
                {
                    state.Active = false;
                    DeleteBrigadeNewOrderV418LikeOriginal(
                        group, BrigadeOrderBitvaV418LikeOriginal,
                        "BrigadeOrder_Bitva::Process_invalid_bounds");
                    return true;
                }
                state.StartTop = top;
                state.MinX = minX;
                state.MaxX = maxX;
                state.MinY = minY;
                state.MaxY = maxY;
            }

            // 2. Renew enemy list.  BitMask is deliberately NOT cleared when an
            // entry is pruned, matching the original order object.
            if (C2RetailRandomV407LikeOriginal.Rando(RepresentativeV407LikeOriginal(group)) < 1024)
            {
                for (int i = 0; i < state.Enemies.Count; i++)
                {
                    EnemyEntryV407LikeOriginal e = state.Enemies[i];
                    if (e == null || !IsSameLiveObjectV407LikeOriginal(e.Unit, e.InstanceSerial))
                    {
                        state.Enemies.RemoveAt(i);
                        i--;
                    }
                }
            }

            // 3. Exactly 64 random AddEnemXY calls, two rando() per sample.
            int dxCells = state.MaxX - state.MinX + 1;
            int dyCells = state.MaxY - state.MinY + 1;
            if (dxCells <= 0 || dyCells <= 0)
            {
                state.Active = false;
                DeleteBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderBitvaV418LikeOriginal,
                    "BrigadeOrder_Bitva::Process_empty_bounds");
                return true;
            }
            for (int p = 0; p < 64; p++)
            {
                int xx = ((C2RetailRandomV407LikeOriginal.Rando(RepresentativeV407LikeOriginal(group)) * dxCells) >> 15) + state.MinX;
                int yy = ((C2RetailRandomV407LikeOriginal.Rando(RepresentativeV407LikeOriginal(group)) * dyCells) >> 15) + state.MinY;
                AddEnemXYV407LikeOriginal(state, xx, yy, state.StartTop, mask);
            }

            bool inBattle = false;
            bool morPresent = false;
            bool someoneAtt = SomeoneAttacksNearV407LikeOriginal(group);
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members = GetFormationOrderMembersV360LikeOriginal(group);

            // 4. Attack service in member order.
            for (int j = 0; j < members.Count; j++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = members[j];
                if (!IsAliveBattleUnitV407LikeOriginal(member)) continue;
                MeleeMdTraitsV407LikeOriginal mt = GetMeleeMdTraitsV407LikeOriginal(member);
                if (mt.Artilery) continue;

                C2CombatRuntimeV334LikeOriginal combat = member.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                C2NeutralPeasantUnitInfoV2LikeOriginal existing;
                if (C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(member, out existing) && existing != null)
                {
                    if (!(CheckAttDistV407LikeOriginal(centerX, centerY, existing) || member.RifleAttackV396LikeOriginal))
                    {
                        if (combat != null) combat.CancelForExternalOrderLikeOriginal("BrigadeOrder_Bitva::Process ClearOrders");
                        C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(member);
                        C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(member);
                    }
                    else inBattle = true;
                    continue;
                }

                if (!CanBitvaReplaceCurrentOrderV407LikeOriginal(member))
                    continue;

                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(member);
                byte killMask = mt.KillMask;
                int minR, maxR;
                C2CombatCoreV408LikeOriginal.GetAttackRadiusEnvelopeV413LikeOriginal(member, out minR, out maxR);
                int armRadius = mt.ArmRadius;
                if (!member.RifleAttackV396LikeOriginal)
                {
                    if (armRadius < 200) armRadius = 200;
                    maxR = armRadius;
                }
                if (maxR == 0) continue;

                int nearDist = 1000000;
                int readyDist = 1000000;
                C2NeutralPeasantUnitInfoV2LikeOriginal near = null;
                C2NeutralPeasantUnitInfoV2LikeOriginal ready = null;
                int myX = Mathf.RoundToInt(RealXV407LikeOriginal(member));
                int myY = Mathf.RoundToInt(RealYV407LikeOriginal(member));

                for (int t = 0; t < state.Enemies.Count; t++)
                {
                    EnemyEntryV407LikeOriginal ee = state.Enemies[t];
                    C2NeutralPeasantUnitInfoV2LikeOriginal enemy = ee != null ? ee.Unit : null;
                    if (!IsSameLiveObjectV407LikeOriginal(enemy, ee != null ? ee.InstanceSerial : 0)) continue;
                    MeleeMdTraitsV407LikeOriginal et = GetMeleeMdTraitsV407LikeOriginal(enemy);
                    if ((et.MathMask & killMask) == 0) continue;
                    if (et.Mortira) morPresent = true;

                    int r = C2OriginalMovementMathV352.Norma(
                        myX - Mathf.RoundToInt(RealXV407LikeOriginal(enemy)),
                        myY - Mathf.RoundToInt(RealYV407LikeOriginal(enemy))) >> 4;
                    if (r <= minR) continue;

                    int na = C2CombatCoreV408LikeOriginal.GetNAttackersV408LikeOriginal(enemy);
                    int re = r + na * 160 * 16;
                    int enemyGroup = -1;
                    TryGetFormationGroupIdV321LikeOriginal(enemy, out enemyGroup);
                    int searchOnly = member.SearchOnlyThisBrigadeToKillV407LikeOriginal;
                    bool nearGate =
                        (re < nearDist && (ee.Danger < 7 || r < maxR || morPresent)) ||
                        (re < nearDist && searchOnly >= 0 && enemyGroup == searchOnly && r < ForcedEnemyPixelsV407LikeOriginal);
                    if (nearGate)
                    {
                        bool accept = true;
                        C2UnitOriginalRuntimeLinkLikeOriginal elink = enemy.RuntimeLinkCachedLikeOriginal;
                        C2UnitOriginalRuntime eruntime = elink != null ? elink.Runtime : null;
                        if (eruntime != null && eruntime.HasMoveTargetLikeOriginal)
                        {
                            byte dr1 = C2OriginalMovementMathV352.GetDir(
                                Mathf.RoundToInt(RealXV407LikeOriginal(enemy) - RealXV407LikeOriginal(member)),
                                Mathf.RoundToInt(RealYV407LikeOriginal(enemy) - RealYV407LikeOriginal(member)));
                            int dr = Math.Abs((int)unchecked((sbyte)(enemy.RealDir - dr1)));
                            accept = dr > 64;
                        }
                        if (accept)
                        {
                            near = enemy;
                            nearDist = re;
                        }
                    }
                    if (r < maxR && re < readyDist)
                    {
                        ready = enemy;
                        readyDist = re;
                    }
                }

                if (ready == null) ready = near;
                if (ready != null && (CheckAttDistV407LikeOriginal(centerX, centerY, ready) || member.RifleAttackV396LikeOriginal))
                {
                    int r = C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt(RealXV407LikeOriginal(member) - RealXV407LikeOriginal(ready)),
                        Mathf.RoundToInt(RealYV407LikeOriginal(member) - RealYV407LikeOriginal(ready))) >> 4;
                    int readyGroup = -1;
                    TryGetFormationGroupIdV321LikeOriginal(ready, out readyGroup);
                    int brigadeOnly;
                    if (!_onlyThisBrigadeToKillV407LikeOriginal.TryGetValue(group.GroupId, out brigadeOnly))
                        brigadeOnly = -1;
                    if (member.RifleAttackV396LikeOriginal || r < 100 || (brigadeOnly >= 0 && readyGroup == brigadeOnly))
                    {
                        if (someoneAtt && brigadeOnly >= 0)
                            member.SearchOnlyThisBrigadeToKillV407LikeOriginal = brigadeOnly;
                        // Once BrigadeOrder_Bitva owns the brigade, native AttackObj
                        // creates the member's personal LocalOrder immediately. It does
                        // not wait for the old KeepPositions paths to finish.
                        bool attacked = BeginOrKeepMeleeAttackObjV407LikeOriginal(
                            member, ready, true, "BrigadeOrder_Bitva::Process");
                        if (attacked)
                        {
                            inBattle = true;
                        }
                        else ReturnMemberToFormationSlotV407LikeOriginal(group, members, j, member);
                    }
                    else ReturnMemberToFormationSlotV407LikeOriginal(group, members, j, member);
                }
                else
                {
                    C2RetailRandomV407LikeOriginal.AddRand(128 + 1); // release NO-OP.
                    ReturnMemberToFormationSlotV407LikeOriginal(group, members, j, member);
                }
            }

            if (!inBattle)
            {
                state.Active = false;
                DeleteBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderBitvaV418LikeOriginal,
                    "BrigadeOrder_Bitva::Process_done");
            }
            return true;
        }

        private static bool ComputeBattleBoundsV407LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            out int top, out int minX, out int minY, out int maxX, out int maxY)
        {
            top = 0xFFFF;
            minX = 10000000;
            minY = 10000000;
            maxX = 0;
            maxY = 0;
            if (group == null) return false;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (!IsAliveBattleUnitV407LikeOriginal(u)) continue;
                if (top == 0xFFFF)
                    top = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(
                        Mathf.RoundToInt(RealXV407LikeOriginal(u)) >> 4,
                        Mathf.RoundToInt(RealYV407LikeOriginal(u)) >> 4);
                int x = Mathf.RoundToInt(RealXV407LikeOriginal(u)) >> 11;
                int y = Mathf.RoundToInt(RealYV407LikeOriginal(u)) >> 11;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            if (maxX < minX) return false;
            minX -= 4;
            maxX += 4;
            minY -= 4;
            maxY += 4;
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            return true;
        }

        private static void AddEnemXYV407LikeOriginal(
            BrigadeBattleStateV407LikeOriginal state, int x, int y, int myTop, byte mask)
        {
            if (state == null || state.Enemies.Count >= BattleEnemyLimitV407LikeOriginal) return;
            int x0 = x << 1;
            int y0 = y << 1;
            int h1 = C2TopologyCoreV401LikeOriginal.GetTopFastV401LikeOriginal(x0, y0);
            int h2 = C2TopologyCoreV401LikeOriginal.GetTopFastV401LikeOriginal(x0 + 1, y0);
            int h3 = C2TopologyCoreV401LikeOriginal.GetTopFastV401LikeOriginal(x0, y0 + 1);
            int h4 = C2TopologyCoreV401LikeOriginal.GetTopFastV401LikeOriginal(x0 + 1, y0 + 1);
            if (h1 >= 0xFFFE && h2 >= 0xFFFE && h3 >= 0xFFFE && h4 >= 0xFFFE) return;
            int nAreas = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal();
            int ntp = myTop * nAreas;
            if (!TopPassesV407LikeOriginal(h1, myTop, ntp) ||
                !TopPassesV407LikeOriginal(h2, myTop, ntp) ||
                !TopPassesV407LikeOriginal(h3, myTop, ntp) ||
                !TopPassesV407LikeOriginal(h4, myTop, ntp)) return;

            // MCount/GetNMSL data-access adapter: the managed Group[] snapshot is
            // indexed by the same logical 128-pixel cell (RealX/RealY >> 11).
            var all = C2LiveUnitCellIndex.GetCell(x, y);
            for (int i = 0; all != null && i < all.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal enemy = all[i];
                if (enemy == null || enemy.IsDeadLikeOriginal || !enemy.isActiveAndEnabled) continue;
                int ex = Mathf.RoundToInt(RealXV407LikeOriginal(enemy)) >> 11;
                int ey = Mathf.RoundToInt(RealYV407LikeOriginal(enemy)) >> 11;
                if (ex != x || ey != y) continue;
                byte nmask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(enemy);
                if ((nmask & mask) != 0) continue;
                MeleeMdTraitsV407LikeOriginal traits = GetMeleeMdTraitsV407LikeOriginal(enemy);
                if (traits.LockType == 1) continue;

                int id = enemy.C2ObjectIndexV407LikeOriginal;
                if (id < 0 || id > 0xFFFF) continue;
                int bit = 1 << (id & 7);
                int ofs = id >> 3;
                if ((state.BitMask[ofs] & bit) != 0) continue;
                state.BitMask[ofs] = unchecked((byte)(state.BitMask[ofs] | bit));
                state.Enemies.Add(new EnemyEntryV407LikeOriginal
                {
                    Unit = enemy,
                    InstanceSerial = enemy.C2ObjectSerialLikeOriginal,
                    Danger = 0
                });
                if (state.Enemies.Count >= BattleEnemyLimitV407LikeOriginal) return;
            }
        }

        private static bool TopPassesV407LikeOriginal(int himTop, int myTop, int ntp)
        {
            if (himTop >= 0xFFFE) return true;
            if (himTop == myTop) return true;
            return C2TopologyCoreV401LikeOriginal.GetLinksDistV401LikeOriginal(himTop + ntp) <= 30;
        }

        private static bool CheckAttDistV407LikeOriginal(
            int centerXOriginalPixels, int centerYOriginalPixels,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (!IsAliveBattleUnitV407LikeOriginal(victim)) return false;
            int vx = Mathf.RoundToInt(RealXV407LikeOriginal(victim)) >> 4;
            int vy = Mathf.RoundToInt(RealYV407LikeOriginal(victim)) >> 4;
            return C2OriginalMovementMathV352.Norma(
                centerXOriginalPixels - vx, centerYOriginalPixels - vy) < CheckAttDistPixelsV407LikeOriginal;
        }

        private static bool SomeoneAttacksNearV407LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return false;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (!IsAliveBattleUnitV407LikeOriginal(u)) continue;
                C2NeutralPeasantUnitInfoV2LikeOriginal e;
                if (!C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(u, out e) || e == null) continue;
                int gid;
                if (!TryGetFormationGroupIdV321LikeOriginal(e, out gid)) continue;
                // COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_Bitva::Process:
                // EOB must belong to a brigade AND be within 120 original pixels.
                int distanceReal = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt(RealXV407LikeOriginal(e) - RealXV407LikeOriginal(u)),
                    Mathf.RoundToInt(RealYV407LikeOriginal(e) - RealYV407LikeOriginal(u)));
                if (distanceReal < 120 * 16) return true;
            }
            return false;
        }

        private static void SortBattleUnitsByObjectIndexV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all)
        {
            if (all == null || all.Length < 2) return;
            Array.Sort(all, delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a,
                                     C2NeutralPeasantUnitInfoV2LikeOriginal b)
            {
                int ai = a != null ? a.C2ObjectIndexV408LikeOriginal : int.MaxValue;
                int bi = b != null ? b.C2ObjectIndexV408LikeOriginal : int.MaxValue;
                return ai.CompareTo(bi);
            });
        }

        private static bool BeginOrKeepMeleeAttackObjV407LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            bool allowLocalApproach,
            string source)
        {
            return BeginOrKeepMeleeAttackObjWithPriorityV414LikeOriginal(
                attacker, victim, allowLocalApproach, 128 + 8, source);
        }

        private static bool BeginOrKeepMeleeAttackObjWithPriorityV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            bool allowLocalApproach,
            int priority,
            string source)
        {
            if (!IsAliveBattleUnitV407LikeOriginal(attacker) || !IsAliveBattleUnitV407LikeOriginal(victim)) return false;

            // Distinguish the native AttackObj state from the managed execution object.
            // V413 checked EnemyID AFTER TryAttackObj and, because every unit already
            // owns a C2CombatRuntime component, mistook a newly-created AttackObj for an
            // already-running one. That skipped BeginAttackLikeOriginal entirely: damage
            // could be serviced by other paths while the visible melee animation/order
            // never started.
            C2CombatRuntimeV334LikeOriginal combat = attacker.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            C2NeutralPeasantUnitInfoV2LikeOriginal runtimeTarget;
            bool runtimeAlreadyOwnsTarget = combat != null &&
                combat.TryGetLiveMeleeTargetV406LikeOriginal(out runtimeTarget) && runtimeTarget == victim;

            bool preciseAttack;
            if (!C2CombatCoreV408LikeOriginal.TryAttackObjV408LikeOriginal(
                    attacker, victim, priority, out preciseAttack)) return false;
            if (preciseAttack) return true;

            C2NeutralPeasantUnitInfoV2LikeOriginal authoritativeTarget;
            if (!C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(attacker, out authoritativeTarget))
                return true; // Native early-success path (NoSearchVictim/in-army BR->Bitva).
            if (authoritativeTarget != victim)
            {
                if (combat != null) combat.SetMeleeLocalApproachV406LikeOriginal(allowLocalApproach);
                return true;
            }

            if (runtimeAlreadyOwnsTarget)
            {
                combat.SetMeleeLocalApproachV406LikeOriginal(allowLocalApproach);
                return true;
            }

            GameObject proxy = combat == null ? attacker.EnsureUnityProxyLikeOriginal() : null;
            if (combat == null && proxy != null) combat = proxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat == null)
            {
                C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(attacker);
                return false;
            }

            // OneObject::AttackObj -> CreateOrder(OrdType=0) replaces the previous local
            // movement order, sets DestX=-1 and DeletePath(). Do the same before the
            // managed AttackObjLink starts its SetDestUnit chase.
            C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(attacker);
            combat.BeginMeleeAttackObjFromBrigadeBitvaV406LikeOriginal(attacker, victim, allowLocalApproach);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                attacker, C2UnitOrderKindV325LikeOriginal.MeleeAttack,
                source ?? "BrigadeOrder_Bitva", "AttackObj_native_local_order");

            C2NeutralPeasantUnitInfoV2LikeOriginal check;
            return combat.TryGetLiveMeleeTargetV406LikeOriginal(out check) && check == victim;
        }

        private static void ReturnMemberToFormationSlotV407LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> orderedMembers,
            int orderedIndex,
            C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            if (group == null || member == null || group.Slots == null || group.Slots.Count == 0) return;
            int slotIndex = group.Units.IndexOf(member);
            if (slotIndex < 0) slotIndex = orderedIndex;
            if (slotIndex < 0 || slotIndex >= group.Slots.Count) return;
            Vector2 slot = group.Slots[slotIndex];
            int d = C2OriginalMovementMathV352.Norma(
                Mathf.RoundToInt(slot.x - RealXV407LikeOriginal(member)),
                Mathf.RoundToInt(slot.y - RealYV407LikeOriginal(member)));
            if (d <= 16 * 16) return;
            // COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_Bitva calls
            // NewMonsterPreciseSendTo(..., 128+1, 0) here.  Despite the word
            // "Precise", that is a NORMAL LocalOrder in NewMon.cpp and does NOT
            // set OneObject::UnlimitedMotion.  V416/V417 Stage1 incorrectly
            // routed this through SetPreciseMoveDestinationRealLikeOriginal(),
            // whose flag is reserved for the BORN/OrderedUnlimitedMotion exit
            // chain.  That made every surviving brigade member temporarily
            // non-orderable/non-selectable while returning to its slot.
            C2OriginalOrderChainV352.SubmitMove(
                member, slot.x, slot.y, false, group.Direction, 0,
                "BrigadeOrder_Bitva::NewMonsterPreciseSendTo_return_v4172", true);
        }

        private static bool CanBitvaReplaceCurrentOrderV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            if (member == null) return false;
            C2UnitOrderRuntimeV325LikeOriginal order = C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(member);
            if (order == null) return true; // OB->LocalOrder == NULL.
            // NewMon.cpp::CheckIfPossibleToBreakOrder cannot break NewAttackPointLink.
            // V408 maps that native order to PreciseAttack.
            return member.ActivityStateV413LikeOriginal < 2 &&
                   !order.IsTerminalLikeOriginal &&
                   order.CurrentLikeOriginal != C2UnitOrderKindV325LikeOriginal.PreciseAttack;
        }

        private static bool IsFormationMovingV407LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return false;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (!IsAliveBattleUnitV407LikeOriginal(u)) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = u.RuntimeLinkCachedLikeOriginal;
                if (link != null && link.Runtime != null && link.Runtime.HasMoveTargetLikeOriginal) return true;
            }
            return false;
        }

        private static bool IsSameLiveObjectV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int serial)
        {
            return IsAliveBattleUnitV407LikeOriginal(unit) && unit.C2ObjectSerialLikeOriginal == (ushort)serial;
        }

        private static bool IsAliveBattleUnitV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit != null && unit.isActiveAndEnabled && !unit.IsDeadLikeOriginal;
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal RepresentativeV407LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return null;
            for (int i = 0; i < group.Units.Count; i++)
                if (IsAliveBattleUnitV407LikeOriginal(group.Units[i])) return group.Units[i];
            return null;
        }

        private static float RealXV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u != null ? (u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX) : 0.0f;
        }

        private static float RealYV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u != null ? (u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY) : 0.0f;
        }

        private static byte NationMaskV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(u);
        }

        private static MeleeMdTraitsV407LikeOriginal GetMeleeMdTraitsV407LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            var rt = C2UnitOriginalRuntime.PrepareTraitsCacheV433(unit);
            if (rt != null && rt.MeleeTraitsV433 != null) return rt.MeleeTraitsV433;
            var traits = LoadMeleeMdTraitsV433LikeOriginal(unit);
            if (rt != null) rt.MeleeTraitsV433 = traits;
            return traits;
        }

        private static MeleeMdTraitsV407LikeOriginal LoadMeleeMdTraitsV433LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string path = md.Path ?? string.Empty;
            MeleeMdTraitsV407LikeOriginal cached;
            if (_meleeMdTraitsV407LikeOriginal.TryGetValue(path, out cached) && cached != null) return cached;

            MeleeMdTraitsV407LikeOriginal r = new MeleeMdTraitsV407LikeOriginal();
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i] ?? string.Empty;
                        int c = line.IndexOf("//", StringComparison.Ordinal);
                        if (c >= 0) line = line.Substring(0, c);
                        string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length == 0) continue;
                        string cmd = p[0];
                        int n;
                        if (string.Equals(cmd, "MAXATTACKERS", StringComparison.OrdinalIgnoreCase) && p.Length > 1 &&
                            int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                            r.MaxAttackers = Mathf.Max(1, n);
                        else if (string.Equals(cmd, "ARMRADIUS", StringComparison.OrdinalIgnoreCase) && p.Length > 1 &&
                                 int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                            r.ArmRadius = Mathf.Max(0, n);
                        else if (string.Equals(cmd, "SEARCH_ENEMY_RADIUS", StringComparison.OrdinalIgnoreCase) && p.Length > 1 &&
                                 int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                            r.SearchEnemyRadiusPixels = Mathf.Max(0, n);
                        else if (string.Equals(cmd, "BRIGADEWAITINGCYCLES", StringComparison.OrdinalIgnoreCase) && p.Length > 1 &&
                                 int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                            r.BrigadeWaitingCycles = Mathf.Max(0, n);
                        else if (string.Equals(cmd, "CANKILL", StringComparison.OrdinalIgnoreCase) && p.Length > 2)
                        {
                            int count = ParseIntV407LikeOriginal(p[1]);
                            for (int k = 0; k < count && k + 2 < p.Length; k++) r.KillMask |= MaterialMaskV407LikeOriginal(p[k + 2]);
                        }
                        else if (string.Equals(cmd, "MATHERIAL", StringComparison.OrdinalIgnoreCase) && p.Length > 2)
                        {
                            int count = ParseIntV407LikeOriginal(p[1]);
                            for (int k = 0; k < count && k + 2 < p.Length; k++) r.MathMask |= MaterialMaskV407LikeOriginal(p[k + 2]);
                        }
                        else if (string.Equals(cmd, "MEDIA", StringComparison.OrdinalIgnoreCase) && p.Length > 1)
                        {
                            if (string.Equals(p[1], "WATER", StringComparison.OrdinalIgnoreCase)) r.LockType = 1;
                            else if (string.Equals(p[1], "2", StringComparison.OrdinalIgnoreCase)) r.LockType = 2;
                            else if (string.Equals(p[1], "3", StringComparison.OrdinalIgnoreCase)) r.LockType = 3;
                            else if (string.Equals(p[1], "4", StringComparison.OrdinalIgnoreCase)) r.LockType = 4;
                            else r.LockType = 0;
                        }
                        else if (string.Equals(cmd, "DONTANSWERONATTACK", StringComparison.OrdinalIgnoreCase)) r.DontAnswerOnAttack = true;
                        else if (string.Equals(cmd, "PRIEST", StringComparison.OrdinalIgnoreCase)) r.Priest = true;
                        else if (string.Equals(cmd, "USAGE", StringComparison.OrdinalIgnoreCase) && p.Length > 1)
                        {
                            string usage = p[1].ToUpperInvariant();
                            r.Mortira = usage == "MORTIRA";
                            r.IsPushka = usage == "PUSHKA";
                            r.Artilery = r.Mortira || r.IsPushka || usage == "MULTICANNON" || usage == "SUPMORT";
                        }
                    }
                }
            }
            catch { }
            _meleeMdTraitsV407LikeOriginal[path] = r;
            return r;
        }

        private static int ParseIntV407LikeOriginal(string s)
        {
            int v;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
        }

        private static byte MaterialMaskV407LikeOriginal(string value)
        {
            string s = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (s == "BODY") return 1;
            if (s == "STONE") return 2;
            if (s == "WOOD") return 4;
            if (s == "IRON") return 8;
            if (s == "FLY") return 16;
            if (s == "BUILDING") return 32;
            if (s == "WOOD_BUILDING") return 64;
            if (s == "STENA") return 128;
            return 0;
        }
    }
}
