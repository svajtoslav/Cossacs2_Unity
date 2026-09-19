using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Stand-ground state ported from COSSACKS2/Multi.cpp, Megapolis.cpp,
    // Groups.cpp, Brigade.cpp and VUI_Actions.cpp.
    // Unity adapters are limited to: managed state storage, runtime movement/order
    // queries, and the existing formation/order catalog instead of raw C++ pointers.
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        private const int BrigDelayBaseV403LikeOriginal = 50;   // MapDiscr.h::BRIGDELAY
        private const int GameSpeedV403LikeOriginal = 256;      // active runtime uses CII normal GameSpeed
        private const int NbPersonalFallbackV403LikeOriginal = 3;

        private sealed class BrigadeStandGroundStateV403LikeOriginal
        {
            public int BrigDelay;
            public int MaxBrigDelay;
            public int AddDamage;
            public int AddShield;
            public bool InStandGround;
            public int LastOrderTime = int.MinValue;
            public bool BrigadeOrderWasActive;
        }

        private sealed class UnitStandGroundStateV403LikeOriginal
        {
            public bool StandGround;
            public int AddDamage;
            public int AddShield;
            // COSSACKS2/NewMon.cpp: NewState==5 for SitInFormations units
            // occupying orders.lst Opt==1 ('@') slots while BR->InStandGround.
            public bool SpecialFormationState5;
        }

        // NewMon.cpp::ApplyTiring stores fatigue on individual OneObject instances,
        // but gameplay fatigue (IsTired/speed penalty/UI) is brigade-owned in Cossacks II.
        private sealed class BrigadeTiringStateV403ELikeOriginal
        {
            public bool IsTired;
            public int LastTireCheckTick = int.MinValue;
            public int AveragePercent = 100;
            public int SoldierCount;
            public int MovingSoldiers;
        }

        private static readonly Dictionary<int, BrigadeStandGroundStateV403LikeOriginal>
            _standGroundByGroupV403LikeOriginal =
                new Dictionary<int, BrigadeStandGroundStateV403LikeOriginal>();
        private static readonly Dictionary<int, BrigadeTiringStateV403ELikeOriginal>
            _tiringByGroupV403ELikeOriginal =
                new Dictionary<int, BrigadeTiringStateV403ELikeOriginal>();
        private static readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, UnitStandGroundStateV403LikeOriginal>
            _standGroundByUnitV403LikeOriginal =
                new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, UnitStandGroundStateV403LikeOriginal>();
        private static readonly Dictionary<string, int> _standGroundTimeByMdPathV403LikeOriginal =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, bool> _formLikeShootersByMdPathV403LikeOriginal =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, bool> _sitInFormationsByMdPathV403DLikeOriginal =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, bool> _noSearchVictimByUnitV403LikeOriginal =
            new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, bool>();
        private static int _standGroundRealtimeV403LikeOriginal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallStandGroundV403LikeOriginal()
        {
            Debug.Log("[C2:STANDGROUND V403E] installed source=COSSACKS2/Multi.cpp+Megapolis.cpp+Groups.cpp+Brigade.cpp+NewMon.cpp+Nature.cpp+VUI_Actions.cpp BRIGDELAY=50 GameSpeed=256 state5=Opt@+SITINFORMATIONS kareRotate=FULL_only cityCadence=1_of_8_per_nation tiring=brigade_IsTired adapters=managed_state+Unity_order_query");
        }

        private static BrigadeStandGroundStateV403LikeOriginal GetStandGroundStateV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            bool create)
        {
            if (group == null) return null;
            BrigadeStandGroundStateV403LikeOriginal state;
            if (_standGroundByGroupV403LikeOriginal.TryGetValue(group.GroupId, out state))
                return state;
            if (!create) return null;
            state = new BrigadeStandGroundStateV403LikeOriginal();
            _standGroundByGroupV403LikeOriginal[group.GroupId] = state;
            return state;
        }

        private static UnitStandGroundStateV403LikeOriginal GetUnitStandGroundStateV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            bool create)
        {
            if (unit == null) return null;
            UnitStandGroundStateV403LikeOriginal state;
            if (_standGroundByUnitV403LikeOriginal.TryGetValue(unit, out state))
                return state;
            if (!create) return null;
            state = new UnitStandGroundStateV403LikeOriginal();
            _standGroundByUnitV403LikeOriginal[unit] = state;
            return state;
        }

        private static void BeginBrigadeOrderV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            bool cancelStandGround,
            string source)
        {
            if (group == null) return;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            state.BrigadeOrderWasActive = true;
            if (cancelStandGround)
                CancelStandGroundAnywayV403LikeOriginal(group, source ?? "brigade_order_begin");
        }

        private static void ForgetBrigadeStandGroundV403LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return;
            _standGroundByGroupV403LikeOriginal.Remove(group.GroupId);
            _tiringByGroupV403ELikeOriginal.Remove(group.GroupId);
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (unit != null)
                {
                    _standGroundByUnitV403LikeOriginal.Remove(unit);
                    _noSearchVictimByUnitV403LikeOriginal.Remove(unit);
                }
            }
        }

        internal static int CurrentSimulationTickV403ELikeOriginal
        {
            get { return _standGroundRealtimeV403LikeOriginal; }
        }

        private static BrigadeTiringStateV403ELikeOriginal GetTiringStateV403ELikeOriginal(
            RuntimeFormationV172LikeOriginal group, bool create)
        {
            if (group == null) return null;
            BrigadeTiringStateV403ELikeOriginal state;
            if (_tiringByGroupV403ELikeOriginal.TryGetValue(group.GroupId, out state)) return state;
            if (!create) return null;
            state = new BrigadeTiringStateV403ELikeOriginal();
            _tiringByGroupV403ELikeOriginal[group.GroupId] = state;
            return state;
        }

        // Direct port of NewMon.cpp::ApplyTiring brigade check:
        // once every >16 tmtmt ticks, average soldier GetTired/1000 and mark
        // BR->IsTired only when average <=2% AND more than half are moving.
        internal static void RefreshFormationTiringStateV403ELikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null) return;
            BrigadeTiringStateV403ELikeOriginal state = GetTiringStateV403ELikeOriginal(group, true);
            int tick = _standGroundRealtimeV403LikeOriginal;
            if (state.LastTireCheckTick != int.MinValue && tick - state.LastTireCheckTick <= 16) return;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers;
            if (!TryGetFormationSoldierMembersV395LikeOriginal(unit, out soldiers) || soldiers == null) return;
            int sumPercent = 0;
            int alive = 0;
            int moving = 0;
            for (int i = 0; i < soldiers.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal soldier = soldiers[i];
                if (soldier == null || soldier.IsDeadLikeOriginal) continue;
                alive++;
                sumPercent += Mathf.Clamp(soldier.GetTiredLikeOriginal, 0, 100000) / 1000;
                C2UnitOriginalRuntimeLinkLikeOriginal link = soldier.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                if (runtime != null && runtime.HasMoveTargetLikeOriginal) moving++;
            }
            bool wasTired = state.IsTired;
            if (alive > 0)
            {
                state.AveragePercent = sumPercent / alive;
                state.SoldierCount = alive;
                state.MovingSoldiers = moving;
                state.IsTired = sumPercent <= alive * 2 && moving > alive / 2;
            }
            else
            {
                state.AveragePercent = 100;
                state.SoldierCount = 0;
                state.MovingSoldiers = 0;
                state.IsTired = false;
            }
            state.LastTireCheckTick = tick;
            if (wasTired != state.IsTired)
            {
                Debug.Log("[C2:TIRING V403E] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                          " isTired=" + state.IsTired +
                          " avg=" + state.AveragePercent.ToString(CultureInfo.InvariantCulture) +
                          " moving=" + moving.ToString(CultureInfo.InvariantCulture) +
                          "/" + alive.ToString(CultureInfo.InvariantCulture));
            }
        }

        internal static bool IsFormationTiredV403ELikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null) return false;
            BrigadeTiringStateV403ELikeOriginal state = GetTiringStateV403ELikeOriginal(group, false);
            return state != null && state.IsTired;
        }

        internal static int GetFormationAverageTiringPercentV403ELikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return unit != null ? Mathf.Clamp(unit.GetTiredLikeOriginal, 0, 100000) / 1000 : 100;
            BrigadeTiringStateV403ELikeOriginal state = GetTiringStateV403ELikeOriginal(group, false);
            return state != null ? state.AveragePercent : 100;
        }

        public static bool TryGetStandGroundSnapshotV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out int brigDelay,
            out int maxBrigDelay,
            out bool inStandGround,
            out int addDamage,
            out int addShield)
        {
            brigDelay = 0;
            maxBrigDelay = 0;
            inStandGround = false;
            addDamage = 0;
            addShield = 0;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, false);
            if (state == null) return true;
            brigDelay = state.BrigDelay;
            maxBrigDelay = state.MaxBrigDelay;
            inStandGround = state.InStandGround;
            addDamage = state.AddDamage;
            addShield = state.AddShield;
            return true;
        }

        public static bool IsUnitStandGroundV403LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            UnitStandGroundStateV403LikeOriginal state = GetUnitStandGroundStateV403LikeOriginal(unit, false);
            return state != null && state.StandGround;
        }

        public static bool MakeStandGroundTempV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            return MakeStandGroundTempV403LikeOriginal(group, source ?? "manual_temp");
        }

        private static bool MakeStandGroundTempV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return false;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            if (state.LastOrderTime == _standGroundRealtimeV403LikeOriginal) return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            int addDamage = 0;
            int addShield = 0;
            int bonus = 100;
            if (order != null)
            {
                addDamage = order.AddDamage2;
                addShield = order.AddShield2;
                if (HasFlagSlotV403LikeOriginal(group))
                {
                    addDamage += order.FlagAddDamage;
                    addShield += order.FlagAddShield;
                }
                bonus = order.StandGroundBonus;
            }

            int delayBase = BrigDelayBaseV403LikeOriginal;
            state.AddDamage = addDamage;
            state.AddShield = addShield;
            state.InStandGround = false;

            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                UnitStandGroundStateV403LikeOriginal unitState = GetUnitStandGroundStateV403LikeOriginal(member, true);
                unitState.StandGround = true;
                unitState.AddDamage = addDamage;
                unitState.AddShield = addShield;
                ClearSpecialFormationState5V403DLikeOriginal(member, unitState, "MakeStandGroundTemp");
                // Multi.cpp overwrites BD for every valid member; preserve that exact
                // behavior, so the last valid member supplies StandGroundTime.
                delayBase = ResolveStandGroundTimeV403LikeOriginal(member);
            }

            state.BrigDelay = delayBase * bonus;
            state.MaxBrigDelay = state.BrigDelay;
            state.LastOrderTime = _standGroundRealtimeV403LikeOriginal;

            Debug.Log("[C2:STANDGROUND V403E TEMP] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " order='" + (order != null ? order.OrderId : string.Empty) + "'" +
                      " unitTime=" + delayBase.ToString(CultureInfo.InvariantCulture) +
                      " bonus=" + bonus.ToString(CultureInfo.InvariantCulture) +
                      " delay=" + state.BrigDelay.ToString(CultureInfo.InvariantCulture) +
                      " addD=" + addDamage.ToString(CultureInfo.InvariantCulture) +
                      " addS=" + addShield.ToString(CultureInfo.InvariantCulture) +
                      " source='" + (source ?? string.Empty) + "'");
            C2GameplayHudV1.C2GameplayHudV403InvalidateStandGroundLikeOriginal();
            return true;
        }

        public static bool MakeStandGroundV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            return MakeStandGroundV403LikeOriginal(group, source ?? "manual_full");
        }

        private static bool MakeStandGroundV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return false;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            if (state.LastOrderTime == _standGroundRealtimeV403LikeOriginal) return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            int addDamage = 0;
            int addShield = 0;
            bool rotate = false;
            if (order != null)
            {
                addDamage = order.AddDamage1;
                addShield = order.AddShield1;
                if (HasFlagSlotV403LikeOriginal(group))
                {
                    addDamage += order.FlagAddDamage;
                    addShield += order.FlagAddShield;
                }
                rotate = order.Usage == 2;
            }

            // BrigadeAI.cpp::GetBrigadeDirectionByUnitPositions is the original
            // physical-orientation resolver. At the point FULL StandGround is reached
            // the formation is settled, so use it to repair a stale managed Direction
            // before directional bonuses / front-row state are applied.
            SynchronizeSettledFormationDirectionV403DLikeOriginal(group, order);

            state.AddDamage = addDamage;
            state.AddShield = addShield;
            state.InStandGround = true;
            state.BrigDelay = 0;

            int commandSlots = group.CommandSlotCount >= 0
                ? group.CommandSlotCount
                : NbPersonalFallbackV403LikeOriginal;
            long centerX = 0;
            long centerY = 0;
            int centerCount = 0;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                if (i >= commandSlots)
                {
                    centerX += Mathf.RoundToInt(CurrentRealXV403LikeOriginal(member) / 16.0f);
                    centerY += Mathf.RoundToInt(CurrentRealYV403LikeOriginal(member) / 16.0f);
                    centerCount++;
                }
                UnitStandGroundStateV403LikeOriginal unitState = GetUnitStandGroundStateV403LikeOriginal(member, true);
                unitState.StandGround = true;
                unitState.AddDamage = addDamage;
                unitState.AddShield = addShield;
            }

            state.LastOrderTime = _standGroundRealtimeV403LikeOriginal;

            // Multi.cpp MakeStandGround rotates OrdUsage==2 around the soldier center.
            if (rotate && centerCount > 0)
            {
                int cx = (int)(centerX / centerCount);
                int cy = (int)(centerY / centerCount);
                for (int i = 0; i < group.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                    if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                    int ux = Mathf.RoundToInt(CurrentRealXV403LikeOriginal(member) / 16.0f);
                    int uy = Mathf.RoundToInt(CurrentRealYV403LikeOriginal(member) / 16.0f);
                    byte d = DirectionFromDeltaV320LikeOriginal(ux - cx, uy - cy, group.Direction);
                    member.StopMoveAndFaceDirectionLikeOriginal(d);
                }
            }

            int state5CountV403D = UpdateSpecialFormationStatesV403DLikeOriginal(
                group, order, source ?? "MakeStandGround");

            Debug.Log("[C2:STANDGROUND V403E FULL] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " order='" + (order != null ? order.OrderId : string.Empty) + "'" +
                      " addD=" + addDamage.ToString(CultureInfo.InvariantCulture) +
                      " addS=" + addShield.ToString(CultureInfo.InvariantCulture) +
                      " rotate=" + rotate.ToString() +
                      " state5=" + state5CountV403D.ToString(CultureInfo.InvariantCulture) +
                      " direction=" + group.Direction.ToString(CultureInfo.InvariantCulture) +
                      " source='" + (source ?? string.Empty) + "'");
            C2GameplayHudV1.C2GameplayHudV403InvalidateStandGroundLikeOriginal();
            return true;
        }

        public static bool CancelStandGroundV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            if (state.LastOrderTime == _standGroundRealtimeV403LikeOriginal) return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            // Multi.cpp::CancelStandGround refuses OrdUsage==2 (KARE).
            if (order != null && order.Usage == 2) return false;

            ApplyCancelStandGroundV403LikeOriginal(group, state, order, false);
            state.LastOrderTime = _standGroundRealtimeV403LikeOriginal;
            state.BrigDelay = 0;
            Debug.Log("[C2:STANDGROUND V403E CANCEL] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " source='" + (source ?? string.Empty) + "'");
            C2GameplayHudV1.C2GameplayHudV403InvalidateStandGroundLikeOriginal();
            return true;
        }

        private static void CancelStandGroundAnywayV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            ApplyCancelStandGroundV403LikeOriginal(group, state, order, true);
            if (state.MaxBrigDelay > 0)
                state.BrigDelay = state.MaxBrigDelay;
            Debug.Log("[C2:STANDGROUND V403E RESET] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " delay=" + state.BrigDelay.ToString(CultureInfo.InvariantCulture) +
                      " max=" + state.MaxBrigDelay.ToString(CultureInfo.InvariantCulture) +
                      " source='" + (source ?? string.Empty) + "'");
            C2GameplayHudV1.C2GameplayHudV403InvalidateStandGroundLikeOriginal();
        }

        private static void ApplyCancelStandGroundV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            BrigadeStandGroundStateV403LikeOriginal state,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order,
            bool anyway)
        {
            int addDamage = 0;
            int addShield = 0;
            if (order != null)
            {
                addDamage = order.AddDamage2;
                addShield = order.AddShield2;
                if (HasFlagSlotV403LikeOriginal(group))
                {
                    addDamage += order.FlagAddDamage;
                    addShield += order.FlagAddShield;
                }
            }
            state.AddDamage = addDamage;
            state.AddShield = addShield;
            state.InStandGround = false;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                UnitStandGroundStateV403LikeOriginal unitState = GetUnitStandGroundStateV403LikeOriginal(member, true);
                unitState.StandGround = false;
                unitState.AddDamage = addDamage;
                unitState.AddShield = addShield;
                ClearSpecialFormationState5V403DLikeOriginal(member, unitState,
                    anyway ? "CancelStandGroundAnyway" : "CancelStandGround");
            }
        }

        // Groups.cpp::SetStandState adapter. ActivityState and LocalOrder are represented
        // by the managed combat/movement runtimes; the GroundState/NewState outputs are the
        // same persistent OneObject-like fields already used by the current combat port.
        public static void SetStandStateV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            byte stateValue)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return;
            // Groups.cpp::SetStandState begins with `if(BR->Strelki)return;`.
            // Brigade::CreateFromGroup derives Strelki from NewMonster::FormLikeShooters.
            if (IsFormLikeShootersV403LikeOriginal(group)) return;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                bool attackActive = C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(member);
                C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                bool localOrder = runtime != null && runtime.HasMoveTargetLikeOriginal;
                if (stateValue != 0)
                {
                    member.GroundStateV396LikeOriginal = attackActive ? 1 : stateValue;
                    if (!localOrder) member.NewStateV396LikeOriginal = 1;
                }
                else
                {
                    member.GroundStateV396LikeOriginal = attackActive ? 1 : 0;
                }
            }
        }

        // Multi.cpp::SetAttState. The C++ NoSearchVictim flag did not exist in
        // the Unity bridge yet, so it is kept as managed OneObject-like state.
        // When Val is true and a managed attack order exists, ClearOrders is
        // represented by CancelForExternalOrderLikeOriginal.
        public static void SetAttStateV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            bool value)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            if (state.LastOrderTime == _standGroundRealtimeV403LikeOriginal) return;

            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                // Priest exclusion is part of the original SetAttState contract.
                // Resolve it from the MD token rather than inventing a unit class.
                if (!IsPriestV403LikeOriginal(member))
                    _noSearchVictimByUnitV403LikeOriginal[member] = value;
                if (value)
                {
                    C2CombatRuntimeV334LikeOriginal combat =
                        member.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                    if (combat != null && combat.IsActiveOrderV350LikeOriginal)
                        combat.CancelForExternalOrderLikeOriginal("Multi.cpp::SetAttState");
                }
            }
            state.LastOrderTime = _standGroundRealtimeV403LikeOriginal;
        }

        public static bool GetNoSearchVictimV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            bool value;
            return unit != null && _noSearchVictimByUnitV403LikeOriginal.TryGetValue(unit, out value) && value;
        }

        internal static void ClearNoSearchVictimV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            _noSearchVictimByUnitV403LikeOriginal.Remove(unit);
        }

        public static int GetBrigadeStandGroundDamageBonusV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            C2NeutralPeasantUnitInfoV2LikeOriginal victim)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(attacker, out group) || group == null)
                return 0;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, false);
            if (state == null) return 0;
            int bonus = state.AddDamage;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            if (state.InStandGround && order != null && IsDirectionalBonusV403LikeOriginal(order) &&
                attacker != null && victim != null)
            {
                byte attackDir = DirectionFromDeltaV320LikeOriginal(
                    Mathf.RoundToInt(CurrentRealXV403LikeOriginal(victim) - CurrentRealXV403LikeOriginal(attacker)),
                    Mathf.RoundToInt(CurrentRealYV403LikeOriginal(victim) - CurrentRealYV403LikeOriginal(attacker)),
                    group.Direction);
                int delta = Math.Abs((sbyte)(attackDir - group.Direction));
                if (delta > 40 && delta < 70) bonus >>= 1;
                if (delta >= 70) bonus = 0;
            }
            return bonus;
        }

        public static int GetBrigadeStandGroundShieldBonusV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal killer)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(victim, out group) || group == null)
                return 0;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, false);
            if (state == null) return 0;
            UnitStandGroundStateV403LikeOriginal unitState = GetUnitStandGroundStateV403LikeOriginal(victim, false);
            int bonus = unitState != null ? unitState.AddShield : state.AddShield;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            if (state.InStandGround && order != null && IsDirectionalBonusV403LikeOriginal(order) &&
                killer != null && victim != null)
            {
                byte hitDir = DirectionFromDeltaV320LikeOriginal(
                    Mathf.RoundToInt(CurrentRealXV403LikeOriginal(killer) - CurrentRealXV403LikeOriginal(victim)),
                    Mathf.RoundToInt(CurrentRealYV403LikeOriginal(killer) - CurrentRealYV403LikeOriginal(victim)),
                    group.Direction);
                int delta = Math.Abs((sbyte)(hitDir - group.Direction));
                if (delta > 40 && delta < 70) bonus >>= 1;
                if (delta >= 70) bonus = 0;
            }
            return bonus;
        }

        // Multi.cpp::MakeReformation tail: KeepPositions -> CancelStandGround ->
        // LastOrderTime=0 -> MakeStandGroundTemp.
        private static void AfterReformationV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            if (order == null || order.Usage != 2)
            {
                ApplyCancelStandGroundV403LikeOriginal(group, state, order, false);
                state.BrigDelay = 0;
            }
            state.LastOrderTime = int.MinValue;
            MakeStandGroundTempV403LikeOriginal(group, source ?? "Multi.cpp::MakeReformation");
            state.BrigadeOrderWasActive = true;
        }

        // Groups.cpp::BrigadesList::SendToPositions tail. This runs after
        // HumanGlobalSendTo is issued: non-KARE cancels full stand-ground, then
        // normal player movement immediately creates the temporary stand state.
        private static void AfterHumanGlobalSendToV403LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            string source)
        {
            if (group == null) return;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order =
                ResolveStandGroundOrderV403LikeOriginal(group);
            bool kare = order != null && order.Usage == 2;
            BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, true);
            if (!kare)
                CancelStandGroundAnywayV403LikeOriginal(group, source ?? "Groups.cpp::SendToPositions");

            // Original code forces LastOrderTime away from REALTIME immediately
            // before MakeStandGroundTemp so the same command can enter temp state.
            state.LastOrderTime = int.MinValue;
            if (!(state.InStandGround && kare))
                MakeStandGroundTempV403LikeOriginal(group, source ?? "Groups.cpp::SendToPositions");
            state.BrigadeOrderWasActive = true;
        }

        internal static void TickBrigadeStandGroundV403LikeOriginal()
        {
            _standGroundRealtimeV403LikeOriginal++;
            C2CombatCoreV408LikeOriginal.TickGlobalV408LikeOriginal();
            if (_groupsByIdV172LikeOriginal.Count == 0) return;

            foreach (KeyValuePair<int, RuntimeFormationV172LikeOriginal> pair in _groupsByIdV172LikeOriginal)
            {
                RuntimeFormationV172LikeOriginal group = pair.Value;
                if (group == null) continue;
                BrigadeStandGroundStateV403LikeOriginal state = GetStandGroundStateV403LikeOriginal(group, false);
                if (state == null) continue;

                bool anyMoveOrBrigadeOrder = group.UsesRoadMovement || group.TurnActive;
                int activeNonAttack = 0;
                int count = Mathf.Min(group.Units.Count, group.Slots.Count);
                for (int i = 0; i < count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                    if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                    C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    bool localOrder = runtime != null && runtime.HasMoveTargetLikeOriginal;
                    bool attackActive = C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(member);
                    if (localOrder) anyMoveOrBrigadeOrder = true;

                    int ux = Mathf.RoundToInt(CurrentRealXV403LikeOriginal(member) / 16.0f);
                    int uy = Mathf.RoundToInt(CurrentRealYV403LikeOriginal(member) / 16.0f);
                    int sx = Mathf.RoundToInt(group.Slots[i].x / 16.0f);
                    int sy = Mathf.RoundToInt(group.Slots[i].y / 16.0f);
                    int dist = C2OriginalMovementMathV352.Norma(ux - sx, uy - sy);
                    if ((localOrder && !attackActive) || dist > 30) activeNonAttack++;
                }

                if (anyMoveOrBrigadeOrder)
                {
                    state.BrigadeOrderWasActive = true;
                    continue;
                }

                if (state.BrigadeOrderWasActive)
                {
                    state.BrigadeOrderWasActive = false;
                    MakeStandGroundTempV403LikeOriginal(group, "BrigadeOrder_KeepPositions::Process_done");
                    continue;
                }

                // NewMon.cpp executes the Opt==1 / NewState=5 test per unit every
                // simulation pass. This matters when a unit was busy/reloading on the
                // exact tick FULL was reached: it must enter PSTAND4 as soon as the
                // local order/delay clears, without restarting the brigade timer.
                if (state.InStandGround)
                    UpdateSpecialFormationStatesV403DLikeOriginal(
                        group, ResolveStandGroundOrderV403LikeOriginal(group), "NewMon.cpp_tick");

                // Ddex1.cpp dispatches exactly ONE City::ProcessCreation per tmtmt tick:
                // tmm=tmtmt&7; CITY[tmm].ProcessCreation().  Therefore a given nation's
                // City::ExecuteBrigades runs once per 8 simulation ticks, not every 40 ms.
                // Megapolis.cpp then subtracts (100*GameSpeed)>>8 when NAct<3.
                bool cityCadenceV403E =
                    ((_standGroundRealtimeV403LikeOriginal & 7) == (group.Nation & 7));
                if (cityCadenceV403E && state.BrigDelay > 0 && activeNonAttack < 3)
                {
                    int db = (100 * GameSpeedV403LikeOriginal) >> 8;
                    if (state.BrigDelay >= db) state.BrigDelay -= db;
                    else state.BrigDelay = 0;
                    if (state.BrigDelay == 0)
                        MakeStandGroundV403LikeOriginal(group, "City::ExecuteBrigades_1of8");
                }
            }
        }

        private static C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal
            ResolveStandGroundOrderV403LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return null;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            return option != null ? option.OrderTemplate : null;
        }

        public static bool IsUnitSpecialFormationState5V403DLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            UnitStandGroundStateV403LikeOriginal state =
                GetUnitStandGroundStateV403LikeOriginal(unit, false);
            return state != null && state.SpecialFormationState5;
        }

        public static bool LeaveUnitSpecialFormationState5ForReloadV403DLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            UnitStandGroundStateV403LikeOriginal state =
                GetUnitStandGroundStateV403LikeOriginal(unit, false);
            if (state == null || !state.SpecialFormationState5) return false;
            ClearSpecialFormationState5V403DLikeOriginal(unit, state, "slow_recharge");
            return true;
        }

        private static bool IsSitInFormationsV403DLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return false;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string path = md.Path ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            bool cached;
            if (_sitInFormationsByMdPathV403DLikeOriginal.TryGetValue(path, out cached))
                return cached;
            bool found = MdHasTokenV403LikeOriginal(path, "SITINFORMATIONS");
            _sitInFormationsByMdPathV403DLikeOriginal[path] = found;
            return found;
        }

        private static void ClearSpecialFormationState5V403DLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal member,
            UnitStandGroundStateV403LikeOriginal unitState,
            string source)
        {
            if (member == null || unitState == null || !unitState.SpecialFormationState5)
                return;
            // CII clears the conditions that allow NewState=5 and TryToStand then
            // returns the unit through UATTACK4/neutral stand. The managed animation
            // bridge exposes the same transition through posture type 4 -> off.
            member.SetCombatPostureV322LikeOriginal(4, false);
            unitState.SpecialFormationState5 = false;
        }

        private static bool UnitCanEnterSpecialFormationState5V403DLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            if (!IsLiveStandGroundMemberV403LikeOriginal(member)) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = member.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
            // NewMon.cpp requires OB->FrameFinished before switching NewState to 5.
            // Rechecking this from the 40 ms StandGround tick preserves the original
            // retry behavior when a unit is still finishing another stand/transition.
            if (runtime != null && !runtime.FrameFinishedLikeOriginal) return false;
            if (runtime != null && runtime.HasMoveTargetLikeOriginal) return false; // LocalOrder
            if (C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(member)) return false;
            if (link != null)
            {
                int remaining, maximum;
                bool animating;
                if (link.TryGetSlowRechargeProgressLikeOriginal(
                        1, out remaining, out maximum, out animating) &&
                    (remaining > 0 || animating))
                    return false; // OB->delay / active reload
            }
            return true;
        }

        private static int UpdateSpecialFormationStatesV403DLikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order,
            string source)
        {
            if (group == null || order == null) return 0;
            BrigadeStandGroundStateV403LikeOriginal brigadeState =
                GetStandGroundStateV403LikeOriginal(group, false);
            if (brigadeState == null) return 0;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            int commandSlots = group.CommandSlotCount >= 0
                ? group.CommandSlotCount
                : NbPersonalFallbackV403LikeOriginal;
            List<int> slotOptions = BuildTemplateSlotOptionsV327LikeOriginal(
                option, group.Units.Count, commandSlots);

            int active = 0;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                UnitStandGroundStateV403LikeOriginal unitState =
                    GetUnitStandGroundStateV403LikeOriginal(member, true);
                bool optOne = i >= commandSlots &&
                              i < slotOptions.Count &&
                              slotOptions[i] == 1;
                bool shouldUse = brigadeState.InStandGround &&
                                 unitState.StandGround &&
                                 optOne &&
                                 IsSitInFormationsV403DLikeOriginal(member);
                if (!shouldUse)
                {
                    ClearSpecialFormationState5V403DLikeOriginal(member, unitState, source);
                    continue;
                }

                if (!unitState.SpecialFormationState5 &&
                    UnitCanEnterSpecialFormationState5V403DLikeOriginal(member))
                {
                    // NewMon.cpp exact semantic mapping:
                    //   OB->NewState=5; TryToStand(OB,false)
                    // Zero-based managed posture 4 selects PATTACK4/PSTAND4 and
                    // the combat adapter maps rifle ATTACK1 -> ATTACK4 while active.
                    member.SetCombatPostureV322LikeOriginal(4, true);
                    unitState.SpecialFormationState5 = true;
                }
                if (unitState.SpecialFormationState5) active++;
            }
            return active;
        }

        private static void SynchronizeSettledFormationDirectionV403DLikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order)
        {
            if (group == null || order == null || group.Units.Count == 0) return;
            if (order.Usage == 2) return; // KARE has no single forward side once FULL rotates outward.
            int commands = group.CommandSlotCount >= 0
                ? Math.Min(group.CommandSlotCount, group.Units.Count)
                : Math.Min(NbPersonalFallbackV403LikeOriginal, group.Units.Count);
            if (group.Units.Count <= commands) return;

            var positions = new C2FormationSymmetryLikeOriginal.MemberPosition[group.Units.Count];
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveStandGroundMemberV403LikeOriginal(member)) continue;
                positions[i] = new C2FormationSymmetryLikeOriginal.MemberPosition
                {
                    Present = true,
                    RealX = Mathf.RoundToInt(CurrentRealXV403LikeOriginal(member)),
                    RealY = Mathf.RoundToInt(CurrentRealYV403LikeOriginal(member))
                };
            }
            byte physical = C2FormationSymmetryLikeOriginal.DirectionByPositions(
                order, positions, commands);
            int delta = Math.Abs((sbyte)(physical - group.Direction));
            if (delta >= 96)
            {
                byte old = group.Direction;
                group.Direction = physical;
                Debug.Log("[C2:FORMATION ORIENTATION V403E] group=" +
                          group.GroupId.ToString(CultureInfo.InvariantCulture) +
                          " old=" + old.ToString(CultureInfo.InvariantCulture) +
                          " physical=" + physical.ToString(CultureInfo.InvariantCulture) +
                          " delta=" + delta.ToString(CultureInfo.InvariantCulture) +
                          " repair=stored_direction_from_physical_members");
            }
        }

        private static bool IsDirectionalBonusV403LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order)
        {
            // Nature.cpp: ODE->DirectionalBonus = strstr(str,"#KARE") == NULL;
            return order != null &&
                   (order.OrderId ?? string.Empty).IndexOf("#KARE", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool HasFlagSlotV403LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            // Multi.cpp checks exactly Memb[2] != 0xFFFF.
            return group != null && group.Units.Count > 2 &&
                   IsLiveStandGroundMemberV403LikeOriginal(group.Units[2]);
        }

        private static bool IsLiveStandGroundMemberV403LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit != null && unit.isActiveAndEnabled && !unit.IsDeadLikeOriginal;
        }

        private static float CurrentRealXV403LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0.0f;
            return unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
        }

        private static float CurrentRealYV403LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0.0f;
            return unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
        }

        private static bool IsFormLikeShootersV403LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return false;
            C2NeutralPeasantUnitInfoV2LikeOriginal sample = null;
            int start = group.CommandSlotCount >= 0 ? group.CommandSlotCount : NbPersonalFallbackV403LikeOriginal;
            for (int i = start; i < group.Units.Count; i++)
            {
                if (IsLiveStandGroundMemberV403LikeOriginal(group.Units[i]))
                {
                    sample = group.Units[i];
                    break;
                }
            }
            if (sample == null && group.Units.Count > 0) sample = group.Units[0];
            if (sample == null) return false;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(sample);
            string path = md.Path ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            bool cached;
            if (_formLikeShootersByMdPathV403LikeOriginal.TryGetValue(path, out cached)) return cached;
            bool found = MdHasTokenV403LikeOriginal(path, "FORMLIKESHOOTERS");
            _formLikeShootersByMdPathV403LikeOriginal[path] = found;
            return found;
        }

        private static bool IsPriestV403LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return false;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string path = md.Path ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            return MdHasTokenV403LikeOriginal(path, "PRIEST");
        }

        private static bool MdHasTokenV403LikeOriginal(string path, string token)
        {
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string trim = (lines[i] ?? string.Empty).Trim();
                    if (trim.Length == 0 || trim.StartsWith("/", StringComparison.Ordinal)) continue;
                    string[] tokens = trim.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 0 && string.Equals(tokens[0], token, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static int ResolveStandGroundTimeV403LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            const int defaultTime = 50; // NewMonster ctor
            if (unit == null) return defaultTime;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string path = md.Path ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return defaultTime;
            int cached;
            if (_standGroundTimeByMdPathV403LikeOriginal.TryGetValue(path, out cached)) return cached;

            int result = defaultTime;
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    string trim = raw.Trim();
                    if (trim.StartsWith("/", StringComparison.Ordinal)) continue;
                    string[] tokens = trim.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 2 || !string.Equals(tokens[0], "STANDGROUNDTIME", StringComparison.OrdinalIgnoreCase))
                        continue;
                    int p1;
                    if (int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out p1))
                        result = p1 * 25 / 80; // NewMon.cpp exact integer conversion
                    break;
                }
            }
            catch { }
            _standGroundTimeByMdPathV403LikeOriginal[path] = result;
            return result;
        }
    }

    public sealed partial class C2GameplayHudV1
    {
        internal static void C2GameplayHudV403InvalidateStandGroundLikeOriginal()
        {
            if (_active == null) return;
            _active._nextRefresh = 0.0f;
            _active._lastSelectedCount = -999999;
        }
    }
}
