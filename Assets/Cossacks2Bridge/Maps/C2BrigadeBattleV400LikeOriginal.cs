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

        private sealed class MeleeMdTraitsV407LikeOriginal
        {
            public int MaxAttackers = 12; // NewMonster constructor default.
            public int ArmRadius = 100;    // NewMonster constructor default.
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

        public static bool BeginBrigadeMeleeAttackV406LikeOriginal(
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

            // Multi.cpp::SetEnemyForBrigade.  The command adapter invokes it here;
            // the function itself is preserved below, including per-unit state and
            // deletion of an incompatible live BITVA order.
            SetEnemyForBrigadeV407LikeOriginal(
                sourceGroup, onlyThisEnemyBrigade ? enemyGroup.GroupId : -1,
                enemyRepresentative.CombatNationLikeOriginal);

            BrigadeStandGroundStateV403LikeOriginal attackStateBridge =
                GetStandGroundStateV403LikeOriginal(sourceGroup, true);
            if (attackStateBridge != null) attackStateBridge.LastOrderTime = int.MinValue;
            SetAttStateV403LikeOriginal(sourceRepresentative, false);
            SetStandStateV403LikeOriginal(sourceRepresentative, 1);
            if (attackStateBridge != null) attackStateBridge.LastOrderTime = int.MinValue;
            CancelStandGroundV403LikeOriginal(sourceRepresentative, "Multi.cpp::AttackSelected_CancelStandGround");
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> orderedMembers = GetFormationOrderMembersV360LikeOriginal(sourceGroup);
            int commandPrefix = ResolveOrderCommandCountV360LikeOriginal(sourceGroup, orderedMembers);
            ApplyAttackStateMoveToFormationV405BLikeOriginal(orderedMembers, commandPrefix, true);

            // Current Unity command integration has no native C++ NewBOrder object;
            // creating the managed order here is the adapter seam.  Its constructor,
            // initial full scan and Process body below retain retail semantics.
            BrigadeBattleStateV407LikeOriginal state;
            if (!_brigadeBattlesV407LikeOriginal.TryGetValue(sourceGroup.GroupId, out state) || state == null || !state.Active)
            {
                state = CreateBrigadeBitvaV407LikeOriginal(sourceGroup, source ?? "Brigade::Bitva");
                if (state == null) return false;
            }
            state.NextProcessAt = 0.0f;
            ProcessBrigadeBitvaV407LikeOriginal(sourceGroup, state);
            return true;
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
            if (state != null)
                ProcessBrigadeBitvaV407LikeOriginal(victimGroup, state);
        }

        public static void TickBrigadeBattleV406LikeOriginal()
        {
            if (_brigadeBattlesV407LikeOriginal.Count == 0) return;
            float now = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
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
                C2CombatRuntimeV334LikeOriginal combat = member.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                C2NeutralPeasantUnitInfoV2LikeOriginal target;
                if (combat != null && combat.TryGetLiveMeleeTargetV406LikeOriginal(out target) && target != null)
                {
                    int gid;
                    if (TryGetFormationGroupIdV321LikeOriginal(target, out gid)) old = gid;
                }
            }
            BrigadeBattleStateV407LikeOriginal state;
            if (old != enemyGroupId && _brigadeBattlesV407LikeOriginal.TryGetValue(group.GroupId, out state) && state != null)
                state.Active = false; // BR->DeleteBOrder();
        }

        private static BrigadeBattleStateV407LikeOriginal CreateBrigadeBitvaV407LikeOriginal(
            RuntimeFormationV172LikeOriginal group, string source)
        {
            if (group == null) return null;

            // Brigade::Bitva rifle escape hatch.
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
                    C2BrigadeRifleAttackV405LikeOriginal.OnRifleStateEnabledLikeOriginal(member);
                    return null;
                }
            }

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
                return null;
            }

            state.StartTop = top;
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
                    state.Active = false;
                    C2BrigadeRifleAttackV405LikeOriginal.OnRifleStateEnabledLikeOriginal(member);
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
                if (combat != null && combat.TryGetLiveMeleeTargetV406LikeOriginal(out existing) && existing != null)
                {
                    if (!(CheckAttDistV407LikeOriginal(centerX, centerY, existing) || member.RifleAttackV396LikeOriginal))
                    {
                        combat.CancelForExternalOrderLikeOriginal("BrigadeOrder_Bitva::Process ClearOrders");
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
                int minR = Mathf.Max(0, md.AttackRadius0Min);
                int maxR = Mathf.Max(0, md.AttackRadius0);
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
                        bool attacked = BeginOrKeepMeleeAttackObjV407LikeOriginal(
                            member, ready, !IsFormationMovingV407LikeOriginal(group), "BrigadeOrder_Bitva::Process");
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

            if (!inBattle) state.Active = false;
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
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            SortBattleUnitsByObjectIndexV408LikeOriginal(all);
            for (int i = 0; all != null && i < all.Length; i++)
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
                C2CombatRuntimeV334LikeOriginal combat = u.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                C2NeutralPeasantUnitInfoV2LikeOriginal e;
                if (combat == null || !combat.TryGetLiveMeleeTargetV406LikeOriginal(out e) || e == null) continue;
                int gid;
                if (!TryGetFormationGroupIdV321LikeOriginal(e, out gid)) continue;
                int d = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt(RealXV407LikeOriginal(e) - RealXV407LikeOriginal(u)),
                    Mathf.RoundToInt(RealYV407LikeOriginal(e) - RealYV407LikeOriginal(u)));
                if (d < 120 * 16) return true;
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
            if (!IsAliveBattleUnitV407LikeOriginal(attacker) || !IsAliveBattleUnitV407LikeOriginal(victim)) return false;
            bool preciseAttack;
            if (!C2CombatCoreV408LikeOriginal.TryAttackObjV408LikeOriginal(
                    attacker, victim, 128 + 15, out preciseAttack)) return false;
            if (preciseAttack) return true;

            C2NeutralPeasantUnitInfoV2LikeOriginal authoritativeTarget;
            if (!C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(attacker, out authoritativeTarget))
                return true; // NoSearchVictim early-success keeps the pre-existing order.
            if (authoritativeTarget != victim)
            {
                C2CombatRuntimeV334LikeOriginal held = attacker.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                if (held != null) held.SetMeleeLocalApproachV406LikeOriginal(allowLocalApproach);
                return true;
            }

            C2CombatRuntimeV334LikeOriginal combat = attacker.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            C2NeutralPeasantUnitInfoV2LikeOriginal existing;
            if (combat != null && combat.TryGetLiveMeleeTargetV406LikeOriginal(out existing) && existing == victim)
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
            C2CombatRuntimeV334LikeOriginal.SetCommandWeaponModeLikeOriginal(attacker, 0);
            combat.BeginMeleeAttackObjFromBrigadeBitvaV406LikeOriginal(attacker, victim, allowLocalApproach);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                attacker, C2UnitOrderKindV325LikeOriginal.MeleeAttack,
                source ?? "BrigadeOrder_Bitva", "AttackObj_slot_0");
            return combat.TryGetLiveMeleeTargetV406LikeOriginal(out existing) && existing == victim;
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
            member.SetPreciseMoveDestinationRealLikeOriginal(
                slot.x, slot.y,
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false, 0);
        }

        private static bool CanBitvaReplaceCurrentOrderV407LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            if (member == null) return false;
            C2UnitOrderRuntimeV325LikeOriginal order = C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(member);
            if (order == null) return true; // OB->LocalOrder == NULL.
            // NewMon.cpp::CheckIfPossibleToBreakOrder cannot break NewAttackPointLink.
            // V408 maps that native order to PreciseAttack.
            return !order.IsTerminalLikeOriginal &&
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
