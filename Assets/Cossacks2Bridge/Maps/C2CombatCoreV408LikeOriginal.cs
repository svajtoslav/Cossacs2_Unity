using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V408 authoritative combat bridge. Original decision semantics are taken from:
    // COSSACKS2/NewMon.cpp::AttackObj/AttackObjLink/GetDamage/CreateRazbros,
    // COSSACKS2/AttackList.cpp, COSSACKS2/Nation.cpp::OneObject::MakeDamage and Nature.cpp.
    // Unity-only seams are object storage, movement, rendering and projectile visuals.
    internal static class C2CombatCoreV408LikeOriginal
    {
        internal sealed class MdTraits
        {
            public int MaxAttackers = 12;
            public int ArmRadius = 100;
            public byte KillMask;
            public byte MathMask;
            public bool Immortal;
            public bool UnbeatableWhenFree;
            public bool Priest;
            public bool Shaman;
            public bool Capture;
            public bool No25;
            public bool Building;
            public bool Pushka;
            public bool Artilery;
            public bool SlowRecharge;
            public byte LockType;
            public int Razbros;
            public int SkillDamageBonus;
            public int SkillDamageMask;
            public int FireLimit;
            public int DetonationForce;
            public int StrikeFlySpeed;
            public int StrikeFlyMaxSpeed;
            public int StrikeForce = 100;
            public int StrikeProbability;
            public int RotationAtPlaceSpeed;
            public int MissInsideProbability;
            public int MissHeightProbability100;
            public int MaxMissHeightProbability = 100;
            public readonly string[] WeaponKind = new string[4];
            public readonly int[] DamageDecr = { 65535, 65535, 65535, 65535 };
            public readonly Dictionary<string, int> Protection = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<int, List<ShotResource>> ShotResources = new Dictionary<int, List<ShotResource>>();
        }

        internal struct ShotResource
        {
            public int Resource;
            public int Amount;
        }

        private sealed class AttackState
        {
            public ushort OwnerSerial;
            public int EnemyIndex = -1;
            public ushort EnemySerial;
            public byte Priority;
            public bool Attack;
            public bool PreciseAttack;
        }

        private sealed class UnitDamageState
        {
            public ushort OwnerSerial;
            public int FiringStage;
            public bool InFire;
            public int FireOwner = 0xFF;
        }

        private sealed class AttackListVictim
        {
            public int VictimIndex;
            public ushort VictimSerial;
            public long Sequence;
            public readonly List<AttackListAttacker> Attackers = new List<AttackListAttacker>(8);
        }

        private struct AttackListAttacker
        {
            public int Index;
            public ushort Serial;
        }

        private static readonly Dictionary<string, MdTraits> TraitsByPath =
            new Dictionary<string, MdTraits>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> WeaponFlagsByKind =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LoadedNresPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, AttackState> AttackStateByUnit =
            new Dictionary<int, AttackState>();
        private static readonly Dictionary<int, UnitDamageState> DamageStateByUnit =
            new Dictionary<int, UnitDamageState>();
        private static readonly Dictionary<int, AttackListVictim> AttackListByVictimIndex =
            new Dictionary<int, AttackListVictim>();
        private static int _lastGlobalTick = int.MinValue;
        private static long _attackListSequenceV408LikeOriginal;
        private const int AttackListRowsV408LikeOriginal = 1791; // AttackList.h::NARows

        // Data/NRES.DAT [CONST] DAMAGEFALL from the supplied clean game files.
        private static readonly int[] DamageFall =
        {
            100,85,70,55,51,47,43,39,35,31,27,23,19,15,13,10,
            10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetV408LikeOriginal()
        {
            TraitsByPath.Clear();
            WeaponFlagsByKind.Clear();
            LoadedNresPaths.Clear();
            AttackStateByUnit.Clear();
            DamageStateByUnit.Clear();
            AttackListByVictimIndex.Clear();
            _lastGlobalTick = int.MinValue;
            _attackListSequenceV408LikeOriginal = 0;
        }

        internal static float SimulationSecondsV408LikeOriginal
        {
            get { return C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal / 25.0f; }
        }

        internal static void TickGlobalV408LikeOriginal()
        {
            int tick = C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal;
            if (tick == _lastGlobalTick) return;
            _lastGlobalTick = tick;
            RefreshAttackListV408LikeOriginal();
            TickFireV408LikeOriginal(tick);
            C2MoraleRuntimeV404LikeOriginal.TickGlobalV408LikeOriginal(tick);
        }

        internal static byte GetNMaskV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0;
            int nation = Mathf.Clamp(unit.CombatNationLikeOriginal, 0, 7);
            int mask = 1 << nation;
            // DIP_SimpleBuilding population uses GetNatNMASK(owner)|128.
            if (unit.SettlementAiControlledLikeOriginal) mask |= 128;
            return unchecked((byte)mask);
        }

        internal static bool CanAttackRelationV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (attacker == null || victim == null) return false;
            MdTraits a = GetTraitsV408LikeOriginal(attacker);
            if ((GetNMaskV408LikeOriginal(attacker) & GetNMaskV408LikeOriginal(victim)) != 0 && !a.Priest && !a.Shaman)
                return false;
            return true;
        }

        internal static bool CanDamageMaterialV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (attacker == null || victim == null) return false;
            MdTraits a = GetTraitsV408LikeOriginal(attacker);
            MdTraits v = GetTraitsV408LikeOriginal(victim);
            byte attackMask = a.KillMask;
            // Retail AttackObjLink combines KillMask with AttackMask[]; the current
            // MD bridge has no separate AttackMask storage, and CANKILL is the exact
            // material gate available in supplied MD data.
            return attackMask == 0 || v.MathMask == 0 || (attackMask & v.MathMask) != 0;
        }

        private static bool CheckAttAbilityV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (attacker == null || victim == null) return false;
            MdTraits at = GetTraitsV408LikeOriginal(attacker);
            byte amask = GetNMaskV408LikeOriginal(attacker);
            byte vmask = GetNMaskV408LikeOriginal(victim);

            if (at.Priest)
            {
                // Nation.cpp::CheckAttAbility contains `if(!EN->NMask&OB->NMask)`.
                // Preserve the compiled C++ precedence exactly: (!EN->NMask)&OB->NMask.
                if ((((vmask == 0) ? 1 : 0) & amask) != 0) return false;
                if (victim.LifeLikeOriginal >= Mathf.Max(1, victim.MaxLifeLikeOriginal))
                {
                    int gid;
                    if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(victim, out gid))
                        return false;
                    float morale, maxMorale;
                    if (!C2MoraleRuntimeV404LikeOriginal.TryGetMoraleSnapshotV404LikeOriginal(victim, out morale, out maxMorale) ||
                        morale >= maxMorale)
                        return false;
                }
                return true;
            }

            if (at.Shaman)
            {
                if (victim.LifeLikeOriginal >= Mathf.Max(1, victim.MaxLifeLikeOriginal))
                {
                    int gid;
                    if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(victim, out gid))
                        return false;
                    float morale, maxMorale;
                    if (!C2MoraleRuntimeV404LikeOriginal.TryGetMoraleSnapshotV404LikeOriginal(victim, out morale, out maxMorale) ||
                        morale >= maxMorale)
                        return false;
                }
                return true;
            }

            return (amask & vmask) == 0;
        }

        private static void SendToCaptureTargetV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (attacker == null || victim == null) return;
            attacker.SetMoveDestinationRealLikeOriginal(
                CurrentRealXV408LikeOriginal(victim), CurrentRealYV408LikeOriginal(victim),
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false, 0);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                attacker, C2UnitOrderKindV325LikeOriginal.Move,
                "NewMon.cpp::AttackObj_CheckAttAbility", "capture_approach");
        }

        internal static bool TryAttackObjV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            int prio1,
            out bool preciseAttack)
        {
            preciseAttack = false;
            if (!IsAliveV408LikeOriginal(attacker) || !IsAliveV408LikeOriginal(victim) || attacker == victim) return false;
            MdTraits at = GetTraitsV408LikeOriginal(attacker);
            MdTraits vt = GetTraitsV408LikeOriginal(victim);
            if (vt.Immortal) return false;
            if (!CanAttackRelationV408LikeOriginal(attacker, victim)) return false;
            if (!CheckAttAbilityV408LikeOriginal(attacker, victim))
            {
                // NewMon.cpp::AttackObj capture fallback after failed CheckAttAbility.
                // OID is a word; the retail `if(OID&&...)` therefore excludes Group[0].
                if (victim.C2ObjectIndexV408LikeOriginal != 0 &&
                    at.LockType != 1 && !at.Building && !at.Capture && vt.Capture)
                    SendToCaptureTargetV408LikeOriginal(attacker, victim);
                return false;
            }
            if (!CanDamageMaterialV408LikeOriginal(attacker, victim)) return false;

            int prio = prio1 == 254 ? 16 + 128 : prio1;
            int ax = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(attacker));
            int ay = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(attacker));
            int vx = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(victim));
            int vy = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(victim));
            int distPixels = C2OriginalMovementMathV352.Norma(ax - vx, ay - vy) >> 4;

            int attackerGroup;
            bool inArmy = C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(attacker, out attackerGroup);
            int victimGroup;
            bool victimInArmy = C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(victim, out victimGroup);
            int armRadius = at.ArmRadius;
            if (inArmy && !attacker.RifleAttackV396LikeOriginal && distPixels > armRadius && (prio1 & 127) < 14)
            {
                int only = attacker.SearchOnlyThisBrigadeToKillV407LikeOriginal;
                if (only < 0 || !victimInArmy || only != victimGroup) return false;
            }

            if (at.Pushka)
            {
                DeleteAttackObjV408LikeOriginal(attacker);
                AttackState ps = GetAttackStateV408LikeOriginal(attacker, true);
                ps.Priority = unchecked((byte)(prio & 127));
                ps.Attack = true;
                ps.PreciseAttack = true;
                preciseAttack = true;
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    attacker, C2UnitOrderKindV325LikeOriginal.PreciseAttack,
                    "NewMon.cpp::NewAttackPoint", "pushka_target_point");
                return true;
            }

            // AttackList.cpp overflow gate is active only for short-ranged attack sets.
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(attacker);
            int maxAttackRadius = Mathf.Max(md.AttackRadius0, Mathf.Max(md.AttackRadius1, md.AttackRadius2));
            if (maxAttackRadius < 160 && GetNAttackersV408LikeOriginal(victim) >= at.MaxAttackers)
            {
                if (distPixels > 150 && !inArmy)
                {
                    int rx = C2RetailRandomV407LikeOriginal.Rando(attacker) & 2047;
                    int ry = C2RetailRandomV407LikeOriginal.Rando(attacker) & 2047;
                    attacker.SetMoveDestinationRealLikeOriginal(
                        vx + rx - 1024, vy + ry - 1024,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        false, 0);
                }
                return false;
            }

            C2UnitOrderRuntimeV325LikeOriginal currentOrder = C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(attacker);
            bool noSearchVictim = C2FormationRuntimeV167LikeOriginal.GetNoSearchVictimV403LikeOriginal(attacker);
            if (noSearchVictim && prio < 128) return true;
            C2FormationRuntimeV167LikeOriginal.ClearNoSearchVictimV408LikeOriginal(attacker);

            AttackState state = GetAttackStateV408LikeOriginal(attacker, true);
            if ((prio & 127) < state.Priority) return false;
            if (state.Attack && (prio & 127) < 5 && state.EnemyIndex >= 0)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal old =
                    C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetByIndexSerialV408LikeOriginal(state.EnemyIndex, state.EnemySerial);
                if (old != null)
                {
                    int oldDist = C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt(CurrentRealXV408LikeOriginal(old) - CurrentRealXV408LikeOriginal(attacker)),
                        Mathf.RoundToInt(CurrentRealYV408LikeOriginal(old) - CurrentRealYV408LikeOriginal(attacker)));
                    int newDist = C2OriginalMovementMathV352.Norma(vx - ax, vy - ay);
                    int minAttackReal = Mathf.Max(0, md.AttackRadius0Min) << 4;
                    if (newDist <= minAttackReal || oldDist <= newDist) return false;
                }
            }

            if (currentOrder != null && currentOrder.IsTerminalLikeOriginal) return false;
            if (state.Attack && state.EnemyIndex >= 0)
                DelAttackerFromVictimV408LikeOriginal(attacker, state.EnemyIndex, state.EnemySerial);

            state.EnemyIndex = victim.C2ObjectIndexV408LikeOriginal;
            state.EnemySerial = victim.C2ObjectSerialLikeOriginal;
            state.Priority = unchecked((byte)(prio & 127));
            state.Attack = true;
            state.PreciseAttack = false;
            if (maxAttackRadius < 180) AddAttackerV408LikeOriginal(attacker, victim);
            return true;
        }

        internal static bool TryGetAttackObjTargetV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            out C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            victim = null;
            AttackState state = GetAttackStateV408LikeOriginal(attacker, false);
            if (state == null || !state.Attack || state.PreciseAttack || state.EnemyIndex < 0) return false;
            victim = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetByIndexSerialV408LikeOriginal(state.EnemyIndex, state.EnemySerial);
            if (!IsAliveV408LikeOriginal(victim))
            {
                DeleteAttackObjV408LikeOriginal(attacker);
                victim = null;
                return false;
            }
            return true;
        }

        internal static void DeleteAttackObjV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal attacker)
        {
            if (attacker == null) return;
            AttackState state = GetAttackStateV408LikeOriginal(attacker, false);
            if (state == null) return;
            if (state.EnemyIndex >= 0)
                DelAttackerFromVictimV408LikeOriginal(attacker, state.EnemyIndex, state.EnemySerial);
            state.EnemyIndex = -1;
            state.EnemySerial = 0;
            state.Attack = false;
            state.PreciseAttack = false;
            state.Priority = 0;
        }

        internal static void DeleteVictimFromAttackListV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (victim == null || victim.C2ObjectIndexV408LikeOriginal < 0) return;
            AttackListByVictimIndex.Remove(victim.C2ObjectIndexV408LikeOriginal);
            foreach (KeyValuePair<int, AttackState> kv in AttackStateByUnit)
            {
                AttackState st = kv.Value;
                if (st != null && st.EnemyIndex == victim.C2ObjectIndexV408LikeOriginal && st.EnemySerial == victim.C2ObjectSerialLikeOriginal)
                {
                    st.EnemyIndex = -1;
                    st.EnemySerial = 0;
                    st.Attack = false;
                }
            }
        }

        internal static int GetNAttackersV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (victim == null) return 0;
            AttackListVictim row;
            if (!AttackListByVictimIndex.TryGetValue(victim.C2ObjectIndexV408LikeOriginal, out row) || row == null ||
                row.VictimSerial != victim.C2ObjectSerialLikeOriginal) return 0;
            int n = row.Attackers.Count - (GetTraitsV408LikeOriginal(victim).MaxAttackers - 1);
            return n < 0 ? 0 : n;
        }

        private static void AddAttackerV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            if (attacker == null || victim == null) return;
            AttackListVictim row;
            int vid = victim.C2ObjectIndexV408LikeOriginal;
            if (!AttackListByVictimIndex.TryGetValue(vid, out row) || row == null || row.VictimSerial != victim.C2ObjectSerialLikeOriginal)
            {
                row = new AttackListVictim
                {
                    VictimIndex = vid,
                    VictimSerial = victim.C2ObjectSerialLikeOriginal,
                    Sequence = _attackListSequenceV408LikeOriginal++
                };
                AttackListByVictimIndex[vid] = row;
            }
            for (int i = 0; i < row.Attackers.Count; i++)
                if (row.Attackers[i].Index == attacker.C2ObjectIndexV408LikeOriginal) return;
            row.Attackers.Add(new AttackListAttacker
            {
                Index = attacker.C2ObjectIndexV408LikeOriginal,
                Serial = attacker.C2ObjectSerialLikeOriginal
            });
        }

        private static void DelAttackerFromVictimV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker, int victimIndex, ushort victimSerial)
        {
            if (attacker == null) return;
            AttackListVictim row;
            if (!AttackListByVictimIndex.TryGetValue(victimIndex, out row) || row == null || row.VictimSerial != victimSerial) return;
            for (int i = row.Attackers.Count - 1; i >= 0; i--)
            {
                if (row.Attackers[i].Index == attacker.C2ObjectIndexV408LikeOriginal)
                    row.Attackers.RemoveAt(i);
            }
            if (row.Attackers.Count == 0) AttackListByVictimIndex.Remove(victimIndex);
        }

        private static void RefreshAttackListV408LikeOriginal()
        {
            if (AttackListByVictimIndex.Count == 0) return;

            // AttackList.cpp iterates VList[0..1790], then each row in insertion
            // order.  That ordering matters because each valid victim consumes one
            // shared rando().  Dictionary enumeration is not the retail order.
            List<AttackListVictim> rows = new List<AttackListVictim>(AttackListByVictimIndex.Values);
            rows.Sort(delegate(AttackListVictim a, AttackListVictim b)
            {
                int ar = a != null ? a.VictimIndex % AttackListRowsV408LikeOriginal : int.MaxValue;
                int br = b != null ? b.VictimIndex % AttackListRowsV408LikeOriginal : int.MaxValue;
                int c = ar.CompareTo(br);
                if (c != 0) return c;
                long asq = a != null ? a.Sequence : long.MaxValue;
                long bsq = b != null ? b.Sequence : long.MaxValue;
                return asq.CompareTo(bsq);
            });

            for (int r = 0; r < rows.Count; r++)
            {
                AttackListVictim row = rows[r];
                if (row == null) continue;
                AttackListVictim liveRow;
                if (!AttackListByVictimIndex.TryGetValue(row.VictimIndex, out liveRow) || !ReferenceEquals(row, liveRow))
                    continue;
                C2NeutralPeasantUnitInfoV2LikeOriginal victim =
                    C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetByIndexSerialV408LikeOriginal(row.VictimIndex, row.VictimSerial);
                bool keepRow = victim != null && IsAliveV408LikeOriginal(victim) &&
                               C2RetailRandomV407LikeOriginal.Rando(victim) > 100;
                if (!keepRow)
                {
                    AttackListByVictimIndex.Remove(row.VictimIndex);
                    continue;
                }
                for (int i = row.Attackers.Count - 1; i >= 0; i--)
                {
                    AttackListAttacker a = row.Attackers[i];
                    C2NeutralPeasantUnitInfoV2LikeOriginal attacker =
                        C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetByIndexSerialV408LikeOriginal(a.Index, a.Serial);
                    if (!IsAliveV408LikeOriginal(attacker)) row.Attackers.RemoveAt(i);
                }
                if (row.Attackers.Count == 0) AttackListByVictimIndex.Remove(row.VictimIndex);
            }
        }

        internal static int GetDamageV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal shooter,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            int attackType)
        {
            if (!IsAliveV408LikeOriginal(shooter) || !IsAliveV408LikeOriginal(victim)) return 0;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(shooter);
            int dist = C2OriginalMovementMathV352.Norma(
                Mathf.RoundToInt(CurrentRealXV408LikeOriginal(shooter) - CurrentRealXV408LikeOriginal(victim)),
                Mathf.RoundToInt(CurrentRealYV408LikeOriginal(shooter) - CurrentRealYV408LikeOriginal(victim))) >> 4;
            int maxRadius = AttackRadiusMaxV408LikeOriginal(md, attackType);
            if (attackType != 0 && maxRadius > 0 && dist > maxRadius) return 0;
            int damage = DamageForAttackTypeV408LikeOriginal(md, attackType);
            if (attackType != 0)
                damage = GetDamFallV408LikeOriginal(dist, GetTraitsV408LikeOriginal(shooter).DamageDecr[Mathf.Clamp(attackType, 0, 3)], damage);
            return MakeDamageV408LikeOriginal(victim, damage, shooter, attackType, false);
        }

        internal static int MakeDamageV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            int persist,
            C2NeutralPeasantUnitInfoV2LikeOriginal sender,
            int attackType,
            bool act)
        {
            if (victim == null || persist <= 0 || victim == sender) return 0;
            MdTraits vt = GetTraitsV408LikeOriginal(victim);
            if (vt.Immortal) return 0;
            if (sender != null)
            {
                MdTraits st = GetTraitsV408LikeOriginal(sender);
                if (vt.UnbeatableWhenFree && st.KillMask != 0 && vt.MathMask != 0 && (st.KillMask & vt.MathMask) == 0)
                    return 0;
                if (!CanAttackRelationV408LikeOriginal(sender, victim)) return 0;
                if (!CanDamageMaterialV408LikeOriginal(sender, victim)) return 0;
            }

            // LockedInBuilding has no native OneObject container in the Unity unit bridge.
            // HiddenInsideBuildingLikeOriginal is the exact available state; preserve the
            // source random miss when an MD supplies MISSINSIDEUNITSDAMAGE.
            if (act && sender != null && victim.RuntimeLinkCachedLikeOriginal != null &&
                victim.RuntimeLinkCachedLikeOriginal.Runtime != null &&
                victim.RuntimeLinkCachedLikeOriginal.Runtime.HiddenInsideBuildingLikeOriginal &&
                vt.MissInsideProbability > 0 && vt.MissInsideProbability > (C2RetailRandomV407LikeOriginal.Rando(victim) % 100))
                return 0;

            // Nation.cpp::MakeDamage height-difference miss. RZ is the unit terrain
            // height; the Unity integration obtains the same terrain sample from the
            // active C2BattleTerrainMode rather than inventing a new combat height.
            if (act && sender != null && vt.MissHeightProbability100 > 0)
            {
                int dh = TerrainHeightV408LikeOriginal(victim) - TerrainHeightV408LikeOriginal(sender);
                if (dh > 0)
                {
                    int pr = dh * vt.MissHeightProbability100 / 100;
                    if (pr > vt.MaxMissHeightProbability) pr = vt.MaxMissHeightProbability;
                    if (pr > 0 && pr > (C2RetailRandomV407LikeOriginal.Rando(victim) % 100))
                        return 0;
                }
            }

            int dam = persist;
            if (sender != null)
            {
                int senderGroup;
                bool senderInBrigade = C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(sender, out senderGroup);
                if (senderInBrigade)
                {
                    dam += C2FormationRuntimeV167LikeOriginal.GetBrigadeExperienceDamageBonusV402LikeOriginal(sender, attackType, dam);
                    dam += C2FormationRuntimeV167LikeOriginal.GetBrigadeStandGroundDamageBonusV403LikeOriginal(sender, victim);
                }
                else
                {
                    MdTraits stSolo = GetTraitsV408LikeOriginal(sender);
                    if (stSolo.SkillDamageBonus != 0 && attackType >= 0 && attackType < 31 &&
                        (stSolo.SkillDamageMask & (1 << attackType)) != 0)
                    {
                        int kills = C2FormationRuntimeV167LikeOriginal.GetUnitKillsV402LikeOriginal(sender);
                        int ddm = kills * stSolo.SkillDamageBonus / 10;
                        int cap = dam * 19;
                        if (ddm > cap) ddm = cap;
                        dam += ddm;
                    }
                }
            }
            int shield = C2FormationRuntimeV167LikeOriginal.GetBrigadeExperienceShieldBonusV402LikeOriginal(victim, sender) +
                         C2FormationRuntimeV167LikeOriginal.GetBrigadeStandGroundShieldBonusV403LikeOriginal(victim, sender);

            // Nation.cpp fires the morale/fear damage event before weapon-kind
            // instant-kill/protection resolution. Keep that event ordering.
            if (act && sender != null)
                C2MoraleRuntimeV404LikeOriginal.OnUnitDamageV404LikeOriginal(victim, sender, attackType);

            if (sender != null && attackType >= 0 && attackType < 4)
            {
                MdTraits st = GetTraitsV408LikeOriginal(sender);
                string kind = st.WeaponKind[attackType] ?? string.Empty;
                int flags = WeaponFlagsV408LikeOriginal(kind);

                // Weapon flag B: retail only transfers damage to occupants of a
                // building and returns without damaging the building itself.
                if ((flags & 32) != 0 && act && vt.Building)
                {
                    DamageInsideV408LikeOriginal(victim, dam, sender, attackType);
                    return 0;
                }

                if ((flags & 2) != 0 && C2RetailRandomV407LikeOriginal.Rando(sender) < 1310 && !vt.Building && !vt.No25)
                {
                    if (!act) return 1000000;
                    C2MoraleRuntimeV404LikeOriginal.OnUnitDeathV404LikeOriginal(victim, sender);
                    KillUnitV408LikeOriginal(victim, sender, "Nation.cpp::MakeDamage_weapon_R");
                    return 0;
                }
                int protection;
                if (vt.Protection.TryGetValue(kind, out protection)) shield += protection;

                if (act)
                    ApplyFireFlagsV408LikeOriginal(victim, sender, vt, flags);
            }

            if (shield < 0) shield = 0;
            dam -= shield;
            if (dam <= 0) dam = 1;
            if (!act) return dam;

            MdTraits senderEffects = sender != null ? GetTraitsV408LikeOriginal(sender) : null;
            if (senderEffects != null && senderEffects.DetonationForce != 0)
                ApplyDetonationAnalogV408LikeOriginal(victim, sender, senderEffects.DetonationForce);

            bool wasAlive = IsAliveV408LikeOriginal(victim) && victim.LifeLikeOriginal > 0;
            if (victim.LifeLikeOriginal > dam)
            {
                victim.LifeLikeOriginal -= dam;
                if (senderEffects != null)
                {
                    ApplyCloseHitDisplacementV408LikeOriginal(victim, sender, vt, attackType);
                    ApplyStrikeAnalogV408LikeOriginal(victim, sender, vt, senderEffects);
                }
                return dam;
            }

            victim.LifeLikeOriginal = 0;
            if (wasAlive)
            {
                C2MoraleRuntimeV404LikeOriginal.OnUnitDeathV404LikeOriginal(victim, sender);
                KillUnitV408LikeOriginal(victim, sender, "Nation.cpp::OneObject::MakeDamage_unit_kill");
            }
            return dam;
        }

        private static UnitDamageState GetDamageStateV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, bool create)
        {
            if (unit == null) return null;
            int key = unit.C2ObjectIndexV408LikeOriginal;
            ushort serial = unit.C2ObjectSerialLikeOriginal;
            UnitDamageState st;
            if (DamageStateByUnit.TryGetValue(key, out st) && st != null && st.OwnerSerial != serial)
            {
                DamageStateByUnit.Remove(key);
                st = null;
            }
            if (st == null && create)
            {
                st = new UnitDamageState { OwnerSerial = serial };
                DamageStateByUnit[key] = st;
            }
            return st;
        }

        private static void ApplyFireFlagsV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal sender,
            MdTraits victimTraits,
            int weaponFlags)
        {
            if (victim == null || (weaponFlags & (8 | 16)) == 0) return;
            UnitDamageState st = GetDamageStateV408LikeOriginal(victim, true);
            if ((weaponFlags & 16) != 0)
            {
                st.FiringStage = victimTraits.FireLimit + 1;
                st.InFire = true;
                st.FireOwner = sender != null ? sender.CombatNationLikeOriginal : 0xFF;
            }
            else if ((weaponFlags & 8) != 0)
            {
                if (st.FiringStage < victimTraits.FireLimit) st.FiringStage++;
                else
                {
                    st.InFire = true;
                    st.FireOwner = sender != null ? sender.CombatNationLikeOriginal : 0xFF;
                }
            }
        }

        private static void TickFireV408LikeOriginal(int tick)
        {
            if (DamageStateByUnit.Count == 0) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            if (all == null || all.Length == 0) return;
            for (int i = 0; i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (!IsAliveV408LikeOriginal(u)) continue;
                UnitDamageState ds = GetDamageStateV408LikeOriginal(u, false);
                if (ds == null || !ds.InFire) continue;
                MdTraits mt = GetTraitsV408LikeOriginal(u);
                if (mt.Building)
                {
                    int dl = Mathf.Max(1, Mathf.Max(1, u.MaxLifeLikeOriginal) >> 11);
                    MakeDamageV408LikeOriginal(u, dl, null, 255, true);
                }
                else if ((tick & 31) == 0 && mt.LockType == 0 && !mt.Artilery)
                {
                    int min = Mathf.Max(1, u.MaxLifeLikeOriginal) / 10;
                    if (u.LifeLikeOriginal > min)
                        MakeDamageV408LikeOriginal(u, 1, null, 255, true);
                }
            }
        }

        private static void DamageInsideV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal building,
            int damage,
            C2NeutralPeasantUnitInfoV2LikeOriginal sender,
            int attackType)
        {
            if (building == null) return;
            // Unity has no native Inside[] owner array.  Hidden occupants in the
            // same 128-pixel spatial cell are the existing integration equivalent.
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> inside = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            int bx = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(building)) >> 11;
            int by = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(building)) >> 11;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (!IsAliveV408LikeOriginal(u) || u == building || u.RuntimeLinkCachedLikeOriginal == null ||
                    u.RuntimeLinkCachedLikeOriginal.Runtime == null ||
                    !u.RuntimeLinkCachedLikeOriginal.Runtime.HiddenInsideBuildingLikeOriginal) continue;
                int ux = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(u)) >> 11;
                int uy = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(u)) >> 11;
                if (ux == bx && uy == by) inside.Add(u);
            }
            if (inside.Count == 0) return;
            // DamageInside consumes the Promax roll before selecting an occupant.
            C2RetailRandomV407LikeOriginal.Rando(sender);
            int rp = (C2RetailRandomV407LikeOriginal.Rando(sender) * inside.Count) >> 15;
            if (rp < 0) rp = 0;
            if (rp >= inside.Count) rp = inside.Count - 1;
            MakeDamageV408LikeOriginal(inside[rp], damage, sender, attackType, true);
        }

        private static void ApplyDetonationAnalogV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal sender,
            int force)
        {
            if (victim == null || sender == null || force == 0) return;
            int dx = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(victim) - CurrentRealXV408LikeOriginal(sender));
            int dy = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(victim) - CurrentRealYV408LikeOriginal(sender));
            int n = C2OriginalMovementMathV352.Norma(dx, dy) + 1;
            int cxOff = (C2RetailRandomV407LikeOriginal.Rando(sender) & 7) - 3 + (dx * 16) / n;
            int cyOff = (C2RetailRandomV407LikeOriginal.Rando(sender) & 7) - 3 + (dy * 16) / n;
            int ddx = -cxOff;
            int ddy = -cyOff;
            int dn = Mathf.Max(1, C2OriginalMovementMathV352.Norma(ddx, ddy));
            int pushX = (ddx * force * 20) / dn / (20 + dn);
            int pushY = (ddy * force * 20) / dn / (20 + dn);
            victim.SetPreciseMoveDestinationRealLikeOriginal(
                CurrentRealXV408LikeOriginal(victim) + (pushX << 4),
                CurrentRealYV408LikeOriginal(victim) + (pushY << 4),
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false, 0);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                victim, C2UnitOrderKindV325LikeOriginal.MoveBack,
                "Weapon.cpp::DetonateUnit", "detonation_force=" + force.ToString(CultureInfo.InvariantCulture));
        }

        private static void ApplyCloseHitDisplacementV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal sender,
            MdTraits victimTraits,
            int attackType)
        {
            if (victim == null || sender == null || victimTraits.RotationAtPlaceSpeed != 0) return;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(sender);
            int rmax = AttackRadiusMaxV408LikeOriginal(md, attackType);
            if (rmax <= 0 || rmax >= 200) return;
            int rmin = AttackRadiusMinV408LikeOriginal(md, attackType);
            int ra = (rmax * 3 + rmin) << 2;
            int dx = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(sender) - CurrentRealXV408LikeOriginal(victim));
            int dy = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(sender) - CurrentRealYV408LikeOriginal(victim));
            int n = C2OriginalMovementMathV352.Norma(dx, dy);
            if (n <= 0 || n >= ra) return;
            dx = 80 * dx / n;
            dy = 80 * dy / n;
            ApplyImmediateRealPositionV408LikeOriginal(
                victim,
                CurrentRealXV408LikeOriginal(victim) - dx,
                CurrentRealYV408LikeOriginal(victim) - dy);
        }

        private static void ApplyImmediateRealPositionV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, float realX, float realY)
        {
            if (unit == null) return;
            unit.RealXFloat = realX;
            unit.RealYFloat = realY;
            unit.RealX = Mathf.RoundToInt(realX);
            unit.RealY = Mathf.RoundToInt(realY);
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null)
            {
                link.Runtime.RuntimeRealXLikeOriginal = realX;
                link.Runtime.RuntimeRealYLikeOriginal = realY;
                if (unit.OwnerMode != null)
                {
                    Vector3 world = unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(realX / 16.0f, realY / 16.0f);
                    link.Runtime.WorldPosition = world;
                    if (link.Runtime.Root != null) link.Runtime.Root.transform.position = world;
                }
            }
            else if (unit.OwnerMode != null && unit.transform != null)
            {
                unit.transform.position = unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(realX / 16.0f, realY / 16.0f);
            }
        }

        private static void ApplyStrikeAnalogV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal sender,
            MdTraits victimTraits,
            MdTraits senderTraits)
        {
            if (victim == null || sender == null || victimTraits.LockType == 1 ||
                victimTraits.StrikeFlyMaxSpeed == 0 || senderTraits.StrikeProbability <= 0) return;
            if (senderTraits.StrikeProbability <= (C2RetailRandomV407LikeOriginal.Rando(sender) % 100)) return;
            int dx = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(sender) - CurrentRealXV408LikeOriginal(victim));
            int dy = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(sender) - CurrentRealYV408LikeOriginal(victim));
            int n = C2OriginalMovementMathV352.Norma(dx, dy);
            if (n == 0) return;

            // The Unity bridge has no exposed anm_FallDown pointer.  Use the native
            // PushUnitBack branch as the integration seam and preserve its RNG/force math.
            int minF = victimTraits.StrikeFlySpeed;
            int maxF = victimTraits.StrikeFlyMaxSpeed;
            if (maxF < minF) maxF = minF;
            int f = minF + ((C2RetailRandomV407LikeOriginal.Rando(sender) * (maxF - minF)) >> 15);
            f = (f * senderTraits.StrikeForce) / 150;
            int awayX = -dx * f / n;
            int awayY = -dy * f / n;
            victim.SetPreciseMoveDestinationRealLikeOriginal(
                CurrentRealXV408LikeOriginal(victim) + (awayX << 4),
                CurrentRealYV408LikeOriginal(victim) + (awayY << 4),
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false, 0);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                victim, C2UnitOrderKindV325LikeOriginal.MoveBack,
                "Nation.cpp::PushUnitBack", "strike_force=" + f.ToString(CultureInfo.InvariantCulture));
        }

        internal static bool HasShotResourcesV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal shooter, int attackType)
        {
            if (shooter == null) return false;
            MdTraits t = GetTraitsV408LikeOriginal(shooter);
            List<ShotResource> costs;
            if (!t.ShotResources.TryGetValue(attackType, out costs) || costs == null || costs.Count == 0) return true;
            int nation = shooter.CombatNationLikeOriginal;
            for (int i = 0; i < costs.Count; i++)
                if (C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(nation, costs[i].Resource) < costs[i].Amount)
                    return false;
            return true;
        }

        internal static bool TryConsumeShotResourcesV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal shooter, int attackType)
        {
            if (!HasShotResourcesV408LikeOriginal(shooter, attackType)) return false;
            MdTraits t = GetTraitsV408LikeOriginal(shooter);
            List<ShotResource> costs;
            if (!t.ShotResources.TryGetValue(attackType, out costs) || costs == null || costs.Count == 0) return true;
            int nation = shooter.CombatNationLikeOriginal;
            for (int i = 0; i < costs.Count; i++)
                C2NationResourceEconomyV348LikeOriginal.AddResourceLikeOriginal(
                    nation, costs[i].Resource, -costs[i].Amount, "NewMon.cpp::CheckShooterAbilityToRecharge");
            return true;
        }

        internal static void ApplyRazbrosV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal shooter,
            ref float targetRealX,
            ref float targetRealY)
        {
            if (shooter == null) return;
            int razbros = GetTraitsV408LikeOriginal(shooter).Razbros;
            if (razbros == 0) return;
            int sx = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(shooter)) >> 4;
            int sy = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(shooter)) >> 4;
            int tx = Mathf.RoundToInt(targetRealX) >> 4;
            int ty = Mathf.RoundToInt(targetRealY) >> 4;
            int r = C2OriginalMovementMathV352.Norma(sx - tx, sy - ty) >> 5;
            tx += (((C2RetailRandomV407LikeOriginal.Rando(shooter) >> 5) - 512) * r * razbros) / 32000;
            ty += (((C2RetailRandomV407LikeOriginal.Rando(shooter) >> 5) - 512) * r * razbros) / 32000;
            targetRealX = tx << 4;
            targetRealY = ty << 4;
        }

        internal static int GetDamFallV408LikeOriginal(int x, int x0, int damage)
        {
            if (x0 == 0) return 0;
            int p = (x * 64) / x0;
            if (p < 0) p = 0;
            int p1 = Mathf.Clamp(p >> 3, 0, 31);
            int p2 = Mathf.Clamp(p1 + 1, 0, 31);
            int f1 = DamageFall[p1];
            int f2 = DamageFall[p2];
            int dx = p & 7;
            return damage * (f1 + (f2 - f1) * dx / 8) / 100;
        }

        internal static MdTraits GetTraitsV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string path = md.Path ?? string.Empty;
            MdTraits cached;
            if (TraitsByPath.TryGetValue(path, out cached) && cached != null) return cached;
            MdTraits t = new MdTraits { Building = md.Building };
            EnsureWeaponFlagsFromNresV408LikeOriginal(path);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                TraitsByPath[path] = t;
                return t;
            }
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++) ParseMdLineV408LikeOriginal(t, lines[i]);
            }
            catch { }
            TraitsByPath[path] = t;
            return t;
        }

        private static void ParseMdLineV408LikeOriginal(MdTraits t, string raw)
        {
            string line = raw ?? string.Empty;
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            if (comment >= 0) line = line.Substring(0, comment);
            string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 0) return;
            string cmd = p[0].ToUpperInvariant();
            int n;
            if (cmd == "MAXATTACKERS" && p.Length > 1 && TryInt(p[1], out n)) t.MaxAttackers = Mathf.Max(1, n);
            else if (cmd == "ARMRADIUS" && p.Length > 1 && TryInt(p[1], out n)) t.ArmRadius = Mathf.Max(0, n);
            else if (cmd == "IMMORTAL") t.Immortal = true;
            else if (cmd == "UNBEATABLEWHENFREE") t.UnbeatableWhenFree = true;
            else if (cmd == "PRIEST") t.Priest = true;
            else if (cmd == "SHAMAN") t.Shaman = true;
            else if (cmd == "CAPTURE") t.Capture = true;
            else if (cmd == "NO25") t.No25 = true;
            else if (cmd == "BUILDING") t.Building = true;
            else if (cmd == "SLOWRECHARGE") t.SlowRecharge = true;
            else if (cmd == "MEDIA" && p.Length > 1)
            {
                string media = p[1].ToUpperInvariant();
                if (media == "WATER") t.LockType = 1;
                else if (media == "2") t.LockType = 2;
                else if (media == "3") t.LockType = 3;
                else if (media == "4") t.LockType = 4;
                else t.LockType = 0;
            }
            else if (cmd == "RAZBROS" && p.Length > 1 && TryInt(p[1], out n)) t.Razbros = n;
            else if (cmd == "SKILLDAMAGEBONUS" && p.Length > 1 && TryInt(p[1], out n)) t.SkillDamageBonus = n;
            else if (cmd == "SKILLDAMAGEMASK" && p.Length > 1 && TryInt(p[1], out n)) t.SkillDamageMask = n;
            else if (cmd == "FIRELIMIT" && p.Length > 1 && TryInt(p[1], out n)) t.FireLimit = n;
            else if (cmd == "DETONATE" && p.Length > 1 && TryInt(p[1], out n)) t.DetonationForce = n;
            else if (cmd == "STRIKEFLYSPEED" && p.Length > 2)
            {
                if (TryInt(p[1], out n)) t.StrikeFlySpeed = n;
                if (TryInt(p[2], out n)) t.StrikeFlyMaxSpeed = n;
            }
            else if (cmd == "STRIKEFORCE" && p.Length > 1 && TryInt(p[1], out n)) t.StrikeForce = n;
            else if (cmd == "STRIKEPROBABILITY" && p.Length > 1 && TryInt(p[1], out n)) t.StrikeProbability = n;
            else if (cmd == "RPLACESPEED" && p.Length > 1 && TryInt(p[1], out n)) t.RotationAtPlaceSpeed = n;
            else if (cmd == "DAMAGEDEC" && p.Length > 2 && TryInt(p[1], out int ai) && TryInt(p[2], out n) && ai >= 0 && ai < 4) t.DamageDecr[ai] = n;
            else if (cmd == "WEAPONKIND" && p.Length > 2 && TryInt(p[1], out ai) && ai >= 0 && ai < 4) t.WeaponKind[ai] = p[2].ToUpperInvariant();
            else if (cmd == "PROTECTION" && p.Length > 1 && TryInt(p[1], out n))
            {
                int at = 2;
                for (int k = 0; k < n && at + 1 < p.Length; k++, at += 2)
                    if (TryInt(p[at + 1], out int value)) t.Protection[p[at].ToUpperInvariant()] = value;
            }
            else if (cmd == "CANKILL" && p.Length > 1 && TryInt(p[1], out n))
            {
                for (int k = 0; k < n && k + 2 < p.Length; k++) t.KillMask |= MaterialMaskV408LikeOriginal(p[k + 2]);
            }
            else if (cmd == "MATHERIAL" && p.Length > 1 && TryInt(p[1], out n))
            {
                for (int k = 0; k < n && k + 2 < p.Length; k++) t.MathMask |= MaterialMaskV408LikeOriginal(p[k + 2]);
            }
            else if (cmd == "USAGE" && p.Length > 1)
            {
                string use = p[1].ToUpperInvariant();
                t.Pushka = use == "PUSHKA";
                t.Artilery = t.Pushka || use == "MORTIRA" || use == "MULTICANNON" || use == "SUPMORT";
            }
            else if (cmd == "MISSINSIDEUNITSDAMAGE" && p.Length > 1 && TryInt(p[1], out n)) t.MissInsideProbability = n;
            else if (cmd == "MISSONHEIGHT" && p.Length > 2)
            {
                if (TryInt(p[1], out n)) t.MissHeightProbability100 = n;
                if (TryInt(p[2], out n)) t.MaxMissHeightProbability = n;
            }
            else if (cmd == "RASTRATA_NA_VISTREL" && p.Length > 2 && TryInt(p[1], out ai) && TryInt(p[2], out n))
                AddShotResourcesV408LikeOriginal(t, ai, p, 3, n);
            else if (cmd == "RASTRATA_NA_VISTREL2" && p.Length > 3 && TryInt(p[1], out int ai0) && TryInt(p[2], out int ai1) && TryInt(p[3], out n))
            {
                AddShotResourcesV408LikeOriginal(t, ai0, p, 4, n);
                AddShotResourcesV408LikeOriginal(t, ai1, p, 4, n);
            }
        }

        private static void AddShotResourcesV408LikeOriginal(MdTraits t, int attackType, string[] p, int at, int count)
        {
            List<ShotResource> list;
            if (!t.ShotResources.TryGetValue(attackType, out list))
            {
                list = new List<ShotResource>();
                t.ShotResources[attackType] = list;
            }
            for (int i = 0; i < count && at + 1 < p.Length; i++, at += 2)
            {
                int res = ResourceIdV408LikeOriginal(p[at]);
                int amount;
                if (res >= 0 && TryInt(p[at + 1], out amount) && amount > 0)
                    list.Add(new ShotResource { Resource = res, Amount = amount });
            }
        }

        private static int ResourceIdV408LikeOriginal(string s)
        {
            switch ((s ?? string.Empty).ToUpperInvariant())
            {
                case "WOOD": return 0;
                case "GOLD": return 1;
                case "STONE": return 2;
                case "FOOD": return 3;
                case "IRON": return 4;
                case "COAL": return 5;
                default: return -1;
            }
        }

        private static void EnsureWeaponFlagsFromNresV408LikeOriginal(string mdPath)
        {
            if (string.IsNullOrEmpty(mdPath)) return;
            string mdDir = Path.GetDirectoryName(mdPath);
            if (string.IsNullOrEmpty(mdDir)) return;
            DirectoryInfo parent = Directory.GetParent(mdDir);
            string dataRoot = parent != null ? parent.FullName : string.Empty;
            string[] candidates =
            {
                !string.IsNullOrEmpty(dataRoot) ? Path.Combine(dataRoot, "Nres.dat") : string.Empty,
                !string.IsNullOrEmpty(dataRoot) ? Path.Combine(dataRoot, "NRES.DAT") : string.Empty,
                Path.Combine(mdDir, "Nres.dat"),
                Path.Combine(mdDir, "NRES.DAT")
            };
            string path = string.Empty;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrEmpty(candidates[i]) && File.Exists(candidates[i]))
                {
                    path = candidates[i];
                    break;
                }
            }
            if (string.IsNullOrEmpty(path) || LoadedNresPaths.Contains(path)) return;
            LoadedNresPaths.Add(path);
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch { try { lines = File.ReadAllLines(path); } catch { return; } }
            bool weapons = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i] ?? string.Empty;
                int c = raw.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) raw = raw.Substring(0, c);
                raw = raw.Trim();
                if (raw.Length == 0) continue;
                if (raw.StartsWith("[", StringComparison.Ordinal))
                {
                    weapons = string.Equals(raw, "[WEAPONS]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!weapons || raw.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] t = raw.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length < 4) continue;
                int flags = 0;
                string f = t[3].ToUpperInvariant();
                if (f.IndexOf('H') >= 0) flags |= 1;
                if (f.IndexOf('R') >= 0) flags |= 2;
                if (f.IndexOf('O') >= 0) flags |= 4;
                if (f.IndexOf('F') >= 0) flags |= 8;
                if (f.IndexOf('X') >= 0) flags |= 16;
                if (f.IndexOf('B') >= 0) flags |= 32;
                if (f.IndexOf('I') >= 0) flags |= 64;
                WeaponFlagsByKind[t[0].ToUpperInvariant()] = flags;
            }
        }

        private static int WeaponFlagsV408LikeOriginal(string kind)
        {
            string key = (kind ?? string.Empty).ToUpperInvariant();
            int flags;
            if (WeaponFlagsByKind.TryGetValue(key, out flags)) return flags;
            // Supplied retail NRES.DAT fallback if runtime data root is not mounted yet.
            switch (key)
            {
                case "MECH": return 4;
                case "STRELA": return 1;
                case "PIKA": return 4;
                case "IADRO": return 1;
                case "VISTREL": return 3;
                case "VISTRELKARTECH": return 3;
                case "KARTECH": return 1;
                case "IADROMOR": return 0;
                case "MEDIK": return 0;
                case "OSTRELA": return 1;
                case "VISTREL2": return 1;
                case "VAFLIA": return 9;
                case "TAMAGAVK": return 1;
                case "KOPIE": return 3;
                case "KAMEN": return 3;
                case "STRELAOTRAVLENIE": return 9;
                default: return 0;
            }
        }

        private static byte MaterialMaskV408LikeOriginal(string value)
        {
            switch ((value ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "BODY": return 1;
                case "STONE": return 2;
                case "WOOD": return 4;
                case "IRON": return 8;
                case "FLY": return 16;
                case "BUILDING": return 32;
                case "WOOD_BUILDING": return 64;
                case "STENA": return 128;
                default: return 0;
            }
        }

        private static AttackState GetAttackStateV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, bool create)
        {
            if (unit == null) return null;
            int key = unit.C2ObjectIndexV408LikeOriginal;
            ushort serial = unit.C2ObjectSerialLikeOriginal;
            AttackState st;
            if (AttackStateByUnit.TryGetValue(key, out st) && st != null && st.OwnerSerial != serial)
            {
                // Group[index] was reused.  Native OneObject state disappeared with
                // the old object, so the managed mirror must disappear as well.
                AttackStateByUnit.Remove(key);
                st = null;
            }
            if (st == null && create)
            {
                st = new AttackState { OwnerSerial = serial };
                AttackStateByUnit[key] = st;
            }
            return st;
        }

        private static void KillUnitV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal killer,
            string source)
        {
            if (victim == null) return;
            victim.LifeLikeOriginal = 0;
            DeleteVictimFromAttackListV408LikeOriginal(victim);
            DeleteAttackObjV408LikeOriginal(victim);
            DamageStateByUnit.Remove(victim.C2ObjectIndexV408LikeOriginal);
            victim.PlayDeathOneShotLikeOriginal(victim.RealDir);
            if (killer != null)
                C2FormationRuntimeV167LikeOriginal.RegisterKillV402LikeOriginal(killer, source ?? "Nation.cpp::MakeDamage");
        }

        private static bool IsAliveV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u != null && u.isActiveAndEnabled && !u.IsDeadLikeOriginal && u.LifeLikeOriginal > 0;
        }

        private static float CurrentRealXV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u != null ? (u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX) : 0.0f;
        }

        private static float CurrentRealYV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u != null ? (u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY) : 0.0f;
        }

        private static int TerrainHeightV408LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.OwnerMode == null) return 0;
            int x = Mathf.RoundToInt(CurrentRealXV408LikeOriginal(unit)) >> 4;
            int y = Mathf.RoundToInt(CurrentRealYV408LikeOriginal(unit)) >> 4;
            return unit.OwnerMode.C2OriginalFogTerrainHeightV1LikeOriginal(x, y);
        }

        private static int DamageForAttackTypeV408LikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 md, int type)
        {
            if (type == 1) return Mathf.Max(0, md.Damage1);
            if (type == 2) return Mathf.Max(0, md.Damage2);
            if (type == 3) return Mathf.Max(0, md.Damage3);
            return Mathf.Max(0, md.Damage0);
        }

        private static int AttackRadiusMinV408LikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 md, int type)
        {
            if (type == 1) return md.AttackRadius1Min;
            if (type == 2) return md.AttackRadius2Min;
            return md.AttackRadius0Min;
        }

        private static int AttackRadiusMaxV408LikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 md, int type)
        {
            if (type == 1) return md.AttackRadius1;
            if (type == 2) return md.AttackRadius2;
            return md.AttackRadius0;
        }

        private static bool TryInt(string s, out int v)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
        }
    }
}
