using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Cossacks II NEWMORALE port. Source: COSSACKS2/Morale.cpp,
    // NewMon.cpp::ApplyTiring and Data/NewMorale.dat.
    internal static class C2MoraleRuntimeV404LikeOriginal
    {
        internal struct WorldUiSnapshot
        {
            public int GroupId;
            public float Morale;
            public float MaxMorale;
            public float DisplayMorale;
            public float Alpha;
            public string DeltaText;
            public float DeltaAlpha;
        }

        private sealed class MoraleConfig
        {
            public readonly float[] Damage = new float[16];
            public readonly float[] BackDamage = new float[16];
            public float KillDec = 1.0f;
            public float FearDec = 0.25f;
            public float IncTime = 0.001f;
            public float CenterInc = 100.0f;
            public float FormIncPerUnit = 1.0f;
            public int CenterRadius = 2000;
            public float FormIncOfficer = 50.0f;
            public float FormIncDrummer = 50.0f;
            public float FormIncFlag = 50.0f;
            public float FormIncPerFrag = 5.0f;
            public float MaxDecWhenLost = 20.0f;
            public int LostCriticalPercent = 30;
            public float MoraleDecWhenLost = 20.0f;
            public int FormationShield15 = 50;
            public int FormationShield196 = 30;
            public int IncDecCoefficient = 100;
            public float MinDueToTired = 36.0f;
            public float DecWhenTired = 0.05f;
            public int MaxPanicSteps = 3;
            public bool AllowTiring = true;

            public MoraleConfig()
            {
                for (int i = 0; i < 16; i++)
                {
                    Damage[i] = 0.3f;
                    BackDamage[i] = 1.0f;
                }
            }
        }

        private sealed class FormationMoraleState
        {
            public int GroupId;
            public int MoraleFixed;
            public int MaxMoraleFixed;
            public float Morale
            {
                get { return MoraleFixed / 10000.0f; }
                set { MoraleFixed = Mathf.RoundToInt(value * 10000.0f); }
            }
            public float MaxMorale
            {
                get { return MaxMoraleFixed / 10000.0f; }
                set { MaxMoraleFixed = Mathf.RoundToInt(value * 10000.0f); }
            }
            public float LastUpdateAt;
            public bool LostCriticalApplied;
            public float NumericBaselineMorale;
            public float LastNumericAt;
            public string DeltaText = string.Empty;
            public float DeltaTextUntil;
            public float DisplayMorale;
            public float WorldVisibleStart;
            public float WorldVisibleUntil;
            public float WorldFadeStart;
            public float LastVisualAt;
            public C2NeutralPeasantUnitInfoV2LikeOriginal Representative;
        }

        private sealed class PanicState
        {
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            // Morale.cpp::Order1::info.Patrol.x
            public int Step;
            public float NextStepAt;
            // Morale.cpp::Order1::info.Patrol.dir
            public byte Direction;
        }

        private static readonly Dictionary<int, FormationMoraleState> FormationStates =
            new Dictionary<int, FormationMoraleState>();
        private static readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, PanicState> PanicByUnit =
            new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, PanicState>();
        private static readonly Dictionary<string, MoraleConfig> ConfigByDataRoot =
            new Dictionary<string, MoraleConfig>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, MoraleConfig> ConfigByMdPath =
            new Dictionary<string, MoraleConfig>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> StartMoraleByMd =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int[]> FearTypeByMd =
            new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, bool> NoMoraleByMd =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> PsixozByMdV404C =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static int SortedUnitsRevision = -1;
        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] SortedUnits = Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private static readonly Dictionary<long, byte> PanicPresenceByCell = new Dictionary<long, byte>();
        private static readonly Dictionary<long, int> PanicHiddenByCell = new Dictionary<long, int>();
        private static readonly Dictionary<long, C2SettlementBuildingSelectableV1LikeOriginal> PanicBuildingByCell =
            new Dictionary<long, C2SettlementBuildingSelectableV1LikeOriginal>();
        private static C2SettlementBuildingSelectableV1LikeOriginal[] PanicBuildings;
        private static readonly Unity.Profiling.ProfilerMarker PanicStepMarker =
            new Unity.Profiling.ProfilerMarker("C2.Morale.PanicStep");
        internal static bool LogPanicStepsLikeOriginal;

        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] GetSortedUnitsLikeOriginal()
        {
            int revision = C2NeutralPeasantUnitInfoV2LikeOriginal.C2ActiveUnitsRegistryRevisionV359LikeOriginal;
            if (revision != SortedUnitsRevision)
            {
                SortedUnits = (C2NeutralPeasantUnitInfoV2LikeOriginal[])
                    C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal().Clone();
                SortUnitsByObjectIndexV408LikeOriginal(SortedUnits);
                SortedUnitsRevision = revision;
            }
            return SortedUnits;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallV404LikeOriginal()
        {
            FormationStates.Clear();
            PanicByUnit.Clear();
            ConfigByMdPath.Clear();
            ConfigByDataRoot.Clear();
            PsixozByMdV404C.Clear();
            StartMoraleByMd.Clear();
            FearTypeByMd.Clear();
            NoMoraleByMd.Clear();
            SortedUnitsRevision = -1;
            SortedUnits = Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            PanicPresenceByCell.Clear();
            PanicHiddenByCell.Clear();
            PanicBuildingByCell.Clear();
            PanicBuildings = null;
            Debug.Log("[C2:MORALE V404] installed source=COSSACKS2/Morale.cpp NEWMORALE + Data/NewMorale.dat panic=TestUnitToEscape/PanicUnit ui=life+tiring+morale+world_delta");
            C2MoraleWorldUiV404LikeOriginal.EnsureInstalledV404LikeOriginal();
        }

        internal static void ApplyTiringMoraleStepV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.IsDeadLikeOriginal) return;
            EnsureSoloInitializedV404LikeOriginal(unit);

            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(unit);
            int groupId;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out groupId))
            {
                // NewMon.cpp::ApplyTiring solo tail. Morale regeneration and panic
                // testing belong to Morale.cpp::RemakeMaxMorale, not this function.
                if (unit.GetTiredLikeOriginal == 0)
                    unit.MoraleFixedLikeOriginal = ApplyTiredMoraleLikeOriginal(unit.MoraleFixedLikeOriginal,
                        MoraleConstantLikeOriginal(cfg.MinDueToTired), MoraleConstantLikeOriginal(cfg.DecWhenTired));
                return;
            }

            FormationMoraleState state = EnsureFormationStateV404LikeOriginal(unit, groupId);

            // NewMon.cpp::ApplyTiring is called for every brigade member.  Retail
            // does not collapse this into one deterministic brigade decrement:
            // each call consumes the global rando() stream with N=32768/NMemb.
            if (C2FormationRuntimeV167LikeOriginal.IsFormationTiredV403ELikeOriginal(unit))
            {
                int nMemb = C2FormationRuntimeV167LikeOriginal.GetFormationMemberSlotCountLikeOriginal(unit);
                if (nMemb > 0)
                {
                    int n = 32768 / nMemb;
                    if (C2RetailRandomV407LikeOriginal.Rando(unit) < n && state.Morale > cfg.MinDueToTired)
                    {
                        float before = state.Morale;
                        state.MoraleFixed -= MoraleConstantLikeOriginal(cfg.DecWhenTired);
                        AfterFormationMoraleChangedV404LikeOriginal(state, unit, before, "tired");
                    }
                }
            }
        }

        internal static bool AllowTiringLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit != null && ConfigForUnitV404LikeOriginal(unit).AllowTiring;
        }

        internal static int MoraleConstantLikeOriginal(float value)
        {
            return (int)(value * 10000.0f);
        }

        internal static int ScaleMoraleLikeOriginal(int value, int percent)
        {
            return value * percent / 100;
        }

        internal static int ApplyTiredMoraleLikeOriginal(int morale, int minimum, int decrement)
        {
            return morale > minimum ? morale - decrement : morale;
        }

        internal static int RegenerateMoraleLikeOriginal(int morale, int maximum, int increment)
        {
            return Math.Min(maximum, morale + increment);
        }

        internal static int GetMoraleSliceStrideLikeOriginal(int highWaterMark)
        {
            return highWaterMark > 32000 ? 256 : highWaterMark > 16000 ? 128 : 32;
        }

        internal static void InitializeFormationLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal member, int groupId, bool birth)
        {
            if (birth) FormationStates.Remove(groupId);
            EnsureFormationStateV404LikeOriginal(member, groupId);
        }

        internal static void ReleaseUnitLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit != null) PanicByUnit.Remove(unit);
        }

        internal static void TickGlobalV408LikeOriginal(int tick)
        {
            int highWaterMark = C2NeutralPeasantUnitInfoV2LikeOriginal.C2ObjectHighWaterMarkLikeOriginal;
            int stride = GetMoraleSliceStrideLikeOriginal(highWaterMark);
            int processedBrigadeMembers = 0;
            for (int index = tick & (stride - 1); index < highWaterMark; index += stride)
            {
                var unit = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetByObjectIndexLikeOriginal(index);
                if (unit == null || unit.IsDeadLikeOriginal) continue;
                EnsureSoloInitializedV404LikeOriginal(unit);
                int groupId;
                if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out groupId))
                {
                    if (processedBrigadeMembers < 512)
                    {
                        UpdateFormationStateV404LikeOriginal(EnsureFormationStateV404LikeOriginal(unit, groupId), unit);
                        processedBrigadeMembers++;
                    }
                }
                else
                {
                    MoraleConfig cfg = ConfigForUnitV404LikeOriginal(unit);
                    unit.MaxMoraleLikeOriginal = GetStartMoraleV404LikeOriginal(unit);
                    unit.MoraleFixedLikeOriginal = RegenerateMoraleLikeOriginal(unit.MoraleFixedLikeOriginal,
                        unit.MaxMoraleFixedLikeOriginal, MoraleConstantLikeOriginal(cfg.IncTime) * 32);
                }
                TestUnitToEscapeV404LikeOriginal(unit);
            }
            AutoSendFreeUnitsToHomeV408LikeOriginal();
        }

        internal static bool TryGetMoraleSnapshotV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out float morale,
            out float maxMorale)
        {
            morale = 0.0f;
            maxMorale = 1.0f;
            if (unit == null) return false;

            int groupId;
            if (!C2FormationRuntimeV167LikeOriginal.TryReadMoraleGroupIdLikeOriginal(unit, out groupId))
            {
                maxMorale = unit.MaxMoraleLikeOriginal;
                morale = unit.MoraleLikeOriginal;
                return true;
            }

            FormationMoraleState state;
            if (!FormationStates.TryGetValue(groupId, out state))
            {
                morale = unit.MoraleLikeOriginal;
                maxMorale = unit.MaxMoraleLikeOriginal;
                return true;
            }
            morale = state.Morale;
            maxMorale = state.MaxMorale;
            return true;
        }

        internal static void OnUnitDamageV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            int attackType)
        {
            if (victim == null || attacker == null) return;
            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(attacker);
            int fearType = GetFearTypeV404LikeOriginal(attacker, attackType);
            if (fearType < 0 || fearType >= 16) return;

            float vx = CurrentRealX(victim);
            float vy = CurrentRealY(victim);
            float ax = CurrentRealX(attacker);
            float ay = CurrentRealY(attacker);
            byte d = DirectionFromDeltaV404LikeOriginal(ax - vx, ay - vy);
            int dd = (sbyte)(byte)(d - victim.RealDir);
            float delta = Mathf.Abs(dd) < 74 ? cfg.Damage[fearType] : cfg.BackDamage[fearType];
            if (Mathf.Abs(delta) < 0.0001f) return;

            AddMoraleInRadiusV404LikeOriginal(victim, 256.0f, -delta, true, "damage");
            AddMoraleInRadiusV404LikeOriginal(attacker, 256.0f, delta, true, "damage_gain");
        }

        internal static void OnUnitDeathV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker)
        {
            if (victim == null) return;
            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(victim);
            AddMoraleInRadiusV404LikeOriginal(victim, 256.0f, -cfg.KillDec, true, "death");
            if (attacker != null)
                AddMoraleInRadiusV404LikeOriginal(attacker, 256.0f, cfg.KillDec, true, "kill_gain");
        }

        internal static void FillWorldUiSnapshotsV404LikeOriginal(List<WorldUiSnapshot> output)
        {
            if (output == null) return;
            output.Clear();
            float now = Time.realtimeSinceStartup;
            foreach (KeyValuePair<int, FormationMoraleState> kv in FormationStates)
            {
                FormationMoraleState s = kv.Value;
                if (s == null || s.MaxMorale <= 0.0f) continue;

                // Retail DrawSomethingOverBrigade keeps BR->M chasing BR->Morale while
                // the world widget is active. V404 only advanced DisplayMorale at the
                // instant morale changed, so the visual copy could remain stale.
                float visualDt = Mathf.Max(0.0f, now - s.LastVisualAt);
                s.LastVisualAt = now;
                if (visualDt > 0.0f)
                    s.DisplayMorale = Mathf.MoveTowards(s.DisplayMorale, s.Morale, visualDt * 25.0f);

                float alpha = 0.0f;
                if (now < s.WorldVisibleUntil)
                {
                    const float fade = 0.25f;
                    if (s.WorldVisibleStart > 0.0f && now < s.WorldVisibleStart + fade)
                        alpha = Mathf.Clamp01((now - s.WorldVisibleStart) / fade);
                    else if (now < s.WorldFadeStart)
                        alpha = 1.0f;
                    else
                        alpha = Mathf.Clamp01((s.WorldVisibleUntil - now) / fade);
                }

                float deltaAlpha = s.DeltaTextUntil > now
                    ? Mathf.Clamp01((s.DeltaTextUntil - now) / 1.25f)
                    : 0.0f;
                if (alpha <= 0.0f && deltaAlpha <= 0.0f) continue;
                output.Add(new WorldUiSnapshot
                {
                    GroupId = s.GroupId,
                    Morale = s.Morale,
                    MaxMorale = s.MaxMorale,
                    DisplayMorale = s.DisplayMorale,
                    Alpha = alpha,
                    DeltaText = s.DeltaText,
                    DeltaAlpha = deltaAlpha
                });
            }
        }

        private static FormationMoraleState EnsureFormationStateV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative, int groupId)
        {
            FormationMoraleState state;
            if (FormationStates.TryGetValue(groupId, out state) && state != null)
            {
                if (representative != null) state.Representative = representative;
                return state;
            }

            int maximumFixed = ComputeFormationMaxMoraleV404LikeOriginal(representative, null, false);
            float max = maximumFixed / 10000.0f;
            state = new FormationMoraleState
            {
                GroupId = groupId,
                MoraleFixed = maximumFixed,
                MaxMoraleFixed = maximumFixed,
                LastUpdateAt = Time.realtimeSinceStartup,
                NumericBaselineMorale = max,
                LastNumericAt = Time.realtimeSinceStartup - 20.0f,
                DisplayMorale = max,
                LastVisualAt = Time.realtimeSinceStartup,
                Representative = representative
            };
            FormationStates[groupId] = state;
            PropagateFormationMoraleV404LikeOriginal(representative, state);
            return state;
        }

        private static void UpdateFormationStateV404LikeOriginal(
            FormationMoraleState state,
            C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            if (state == null || representative == null) return;
            float now = Time.realtimeSinceStartup; // visual feedback timestamps only
            state.LastUpdateAt = now;
            state.Representative = representative;

            bool lostCriticalNow = false;
            state.MaxMoraleFixed = ComputeFormationMaxMoraleV404LikeOriginal(representative, state, true, out lostCriticalNow);
            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(representative);
            int bonus = C2FormationRuntimeV167LikeOriginal.GetFormationMoraleRecoveryBonusLikeOriginal(representative);
            int increment = ScaleMoraleLikeOriginal(MoraleConstantLikeOriginal(cfg.IncTime), Math.Max(0, 100 + bonus));
            float before = state.Morale;
            state.MoraleFixed = RegenerateMoraleLikeOriginal(state.MoraleFixed, state.MaxMoraleFixed, increment);
            if (state.Morale != before)
                AfterFormationMoraleChangedV404LikeOriginal(state, representative, before, "regen");
            UpdateNumericAndWorldFeedbackV404LikeOriginal(state);
            PropagateFormationMoraleV404LikeOriginal(representative, state);
        }

        private static int ComputeFormationMaxMoraleV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            FormationMoraleState state,
            bool applyLostTransition)
        {
            bool dummy;
            return ComputeFormationMaxMoraleV404LikeOriginal(representative, state, applyLostTransition, out dummy);
        }

        private static int ComputeFormationMaxMoraleV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            FormationMoraleState state,
            bool applyLostTransition,
            out bool lostCriticalNow)
        {
            lostCriticalNow = false;
            if (representative == null) return 0;
            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(representative);
            int gid, totalSoldiers, liveSoldiers, exp;
            bool officer, drummer, flag;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationMoraleInputsV404LikeOriginal(
                    representative, out gid, out totalSoldiers, out liveSoldiers,
                    out officer, out drummer, out flag, out exp))
                return GetStartMoraleV404LikeOriginal(representative) * 10000;

            int maximum = C2FormationRuntimeV167LikeOriginal.GetFormationStartMoraleLikeOriginal(representative) * 10000;
            maximum += totalSoldiers * MoraleConstantLikeOriginal(cfg.FormIncPerUnit);
            maximum += exp * MoraleConstantLikeOriginal(cfg.FormIncPerFrag);
            if (officer) maximum += MoraleConstantLikeOriginal(cfg.FormIncOfficer);
            if (drummer) maximum += MoraleConstantLikeOriginal(cfg.FormIncDrummer);
            if (flag) maximum += MoraleConstantLikeOriginal(cfg.FormIncFlag);
            maximum += C2FormationRuntimeV167LikeOriginal.GetFormationAddedMaxMoraleLikeOriginal(representative);

            int lostPercent = totalSoldiers > 0
                ? (totalSoldiers - liveSoldiers) * 100 / totalSoldiers
                : 0;
            lostCriticalNow = lostPercent > cfg.LostCriticalPercent;
            if (lostCriticalNow) maximum -= MoraleConstantLikeOriginal(cfg.MaxDecWhenLost);

            if (applyLostTransition && state != null && lostCriticalNow != state.LostCriticalApplied)
            {
                float before = state.Morale;
                int change = MoraleConstantLikeOriginal(cfg.MoraleDecWhenLost);
                state.MoraleFixed += lostCriticalNow ? -change : change;
                state.LostCriticalApplied = lostCriticalNow;
                if (Mathf.Abs(state.Morale - before) > 0.0001f)
                    AfterFormationMoraleChangedV404LikeOriginal(state, representative, before, "lost_threshold");
            }
            return maximum;
        }

        private static void ChangeFormationMoraleV404LikeOriginal(
            FormationMoraleState state,
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            int delta,
            string reason)
        {
            if (state == null || delta == 0) return;
            float before = state.Morale;
            state.MoraleFixed = Math.Min(state.MoraleFixed + delta, state.MaxMoraleFixed);
            if (Mathf.Abs(state.Morale - before) <= 0.000001f) return;
            AfterFormationMoraleChangedV404LikeOriginal(state, representative, before, reason);
        }

        private static void AfterFormationMoraleChangedV404LikeOriginal(
            FormationMoraleState state,
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            float before,
            string reason)
        {
            state.Representative = representative;
            UpdateNumericAndWorldFeedbackV404LikeOriginal(state);
            PropagateFormationMoraleV404LikeOriginal(representative, state);
        }

        private static void UpdateNumericAndWorldFeedbackV404LikeOriginal(FormationMoraleState state)
        {
            if (state == null) return;
            float now = Time.realtimeSinceStartup;
            int currentInt = Mathf.FloorToInt(state.Morale + 0.0001f);
            int baselineInt = Mathf.FloorToInt(state.NumericBaselineMorale + 0.0001f);
            int delta = currentInt - baselineInt;

            // Morale.cpp: abs(M0-BM1)>1 and 1500 ms cooldown.
            if (Mathf.Abs(delta) > 1 && now - state.LastNumericAt > 1.5f && currentInt < 500)
            {
                state.LastNumericAt = now;
                state.NumericBaselineMorale = state.Morale;
                state.DeltaText = (delta < 0 ? "-" + (-delta).ToString(CultureInfo.InvariantCulture)
                                             : "+" + delta.ToString(CultureInfo.InvariantCulture)) +
                                  " (" + currentInt.ToString(CultureInfo.InvariantCulture) + ")";
                state.DeltaTextUntil = now + 1.25f;
            }

            // DrawSomethingOverBrigade compares fixed-point BR->M and BR->Morale with dM=4;
            // in player-visible morale units that is effectively any real change.
            if (Mathf.Abs(state.DisplayMorale - state.Morale) > 0.001f)
            {
                // cvi_InterfaceSystem defaults: bbHideTime=3000 ms, bbFade=250 ms.
                // Start with the same 250 ms fade-in, hold, then 250 ms fade-out.
                if (now >= state.WorldVisibleUntil)
                    state.WorldVisibleStart = now;
                state.WorldVisibleUntil = now + 3.50f;
                state.WorldFadeStart = state.WorldVisibleUntil - 0.25f;
            }
            // DisplayMorale itself is advanced from FillWorldUiSnapshots every repaint.
            // Keeping LastVisualAt initialized avoids a first-frame jump.
            if (state.LastVisualAt <= 0.0f)
                state.LastVisualAt = now;
        }

        private static void PropagateFormationMoraleV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            FormationMoraleState state)
        {
            if (representative == null || state == null) return;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetMoraleMembersLikeOriginal(representative, out members))
            {
                representative.MoraleFixedLikeOriginal = state.MoraleFixed;
                representative.MaxMoraleFixedLikeOriginal = state.MaxMoraleFixed;
                return;
            }
            for (int i = 0; members != null && i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal m = members[i];
                if (m == null) continue;
                m.MoraleFixedLikeOriginal = state.MoraleFixed;
                m.MaxMoraleFixedLikeOriginal = state.MaxMoraleFixed;
            }
        }

        private static void AddMoraleInRadiusV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal center,
            float radiusOriginalPixels,
            float delta,
            bool throughMin,
            string reason)
        {
            if (center == null || Mathf.Abs(delta) < 0.000001f) return;
            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(center);
            int fixedDelta = MoraleConstantLikeOriginal(delta);
            if (fixedDelta > 0) fixedDelta = ScaleMoraleLikeOriginal(fixedDelta, cfg.IncDecCoefficient);

            // Morale.cpp::AddMoraleInRadius walks the 128-pixel spatial cells in
            // TopoGraf.cpp::Rarr order.  We do not have native MCount/GetNMSL in
            // Unity, so build the equivalent cell buckets from Group[] identities
            // once, preserving Group[] (OneObject::Index) order inside every cell.
            int cx = Mathf.RoundToInt(CurrentRealX(center) / 16.0f);
            int cy = Mathf.RoundToInt(CurrentRealY(center) / 16.0f);
            int cellX0 = cx >> 7;
            int cellY0 = cy >> 7;
            int rr = (Mathf.RoundToInt(radiusOriginalPixels) >> 7) + 1;
            byte mask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(center);
            HashSet<int> touchedGroups = new HashSet<int>();

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = GetSortedUnitsLikeOriginal();

            Dictionary<long, List<C2NeutralPeasantUnitInfoV2LikeOriginal>> cells =
                new Dictionary<long, List<C2NeutralPeasantUnitInfoV2LikeOriginal>>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || u.IsDeadLikeOriginal || !u.isActiveAndEnabled) continue;
                int ux = Mathf.RoundToInt(CurrentRealX(u) / 16.0f);
                int uy = Mathf.RoundToInt(CurrentRealY(u) / 16.0f);
                long key = CellKeyV408LikeOriginal(ux >> 7, uy >> 7);
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> bucket;
                if (!cells.TryGetValue(key, out bucket))
                {
                    bucket = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                    cells.Add(key, bucket);
                }
                bucket.Add(u);
            }

            int radius = Mathf.RoundToInt(radiusOriginalPixels);
            for (int v = 0; v < rr; v++)
            {
                int n = C2TopologyCoreV401LikeOriginal.GetRadioCountV407LikeOriginal(v);
                for (int ri = 0; ri < n; ri++)
                {
                    int ox, oy;
                    if (!C2TopologyCoreV401LikeOriginal.TryGetRadioOffsetV407LikeOriginal(v, ri, out ox, out oy))
                        continue;
                    List<C2NeutralPeasantUnitInfoV2LikeOriginal> bucket;
                    if (!cells.TryGetValue(CellKeyV408LikeOriginal(cellX0 + ox, cellY0 + oy), out bucket))
                        continue;

                    // AddMoraleInRadiusInCell receives D by value, but modifies that
                    // local D after each newly encountered formation.  Preserve this
                    // retail quirk for subsequent objects in the same spatial cell.
                    int cellDelta = fixedDelta;
                    for (int bi = 0; bi < bucket.Count; bi++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal u = bucket[bi];
                        int ux = Mathf.RoundToInt(CurrentRealX(u) / 16.0f);
                        int uy = Mathf.RoundToInt(CurrentRealY(u) / 16.0f);
                        if (C2OriginalMovementMathV352.Norma(cx - ux, cy - uy) >= radius) continue;
                        byte unitMask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(u);
                        if ((unitMask & mask) == 0) continue;

                        EnsureSoloInitializedV404LikeOriginal(u);
                        int gid;
                        if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(u, out gid))
                        {
                            int soldiers = GetFormationSoldierSlotCountV404LikeOriginal(u);
                            if (soldiers <= 0 || touchedGroups.Count >= 512 || !touchedGroups.Add(gid)) continue;
                            FormationMoraleState state = EnsureFormationStateV404LikeOriginal(u, gid);
                            int shield = cfg.FormationShield15;
                            if (196 != 15)
                                shield = cfg.FormationShield15 +
                                    (soldiers - 15) * (cfg.FormationShield196 - cfg.FormationShield15) / (196 - 15);
                            cellDelta = ScaleMoraleLikeOriginal(cellDelta, shield);
                            if (cellDelta < 0.0f && !throughMin && state.Morale <= 34.0f) continue;
                            ChangeFormationMoraleV404LikeOriginal(state, u, cellDelta, reason);
                        }
                        else
                        {
                            if (cellDelta < 0.0f && !throughMin && u.MoraleLikeOriginal <= 34.0f) continue;
                            u.MoraleFixedLikeOriginal += cellDelta;
                        }
                    }
                }
            }
        }

        private static long CellKeyV408LikeOriginal(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }

        private static void SortUnitsByObjectIndexV408LikeOriginal(
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

        private static int GetFormationSoldierSlotCountV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            int gid, total, live, exp;
            bool a, b, c;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationMoraleInputsV404LikeOriginal(
                    representative, out gid, out total, out live, out a, out b, out c, out exp))
                return total;
            return 0;
        }

        internal static void EnsureSoloInitializedV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.MoraleInitializedLikeOriginal) return;
            int start = GetStartMoraleV404LikeOriginal(unit);
            unit.MaxMoraleFixedLikeOriginal = start * 10000;
            unit.MoraleFixedLikeOriginal = Math.Max(330000, unit.MaxMoraleFixedLikeOriginal / 2);
            unit.MoraleInitializedLikeOriginal = true;
        }

        private static void TestUnitToEscapeV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.IsDeadLikeOriginal || IsNoMoraleV404LikeOriginal(unit)) return;
            if (PanicByUnit.ContainsKey(unit)) return;

            float morale = unit.MoraleLikeOriginal;
            int gid;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out gid))
            {
                FormationMoraleState state = EnsureFormationStateV404LikeOriginal(unit, gid);
                morale = state.Morale;
            }
            if (morale >= 32.0f) return;
            int m = (int)morale;
            int indexNibble = unit.C2ObjectIndexV408LikeOriginal & 15;
            if ((m - indexNibble) <= 16)
                StartPanicV404LikeOriginal(unit);
        }

        private static void StartPanicV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || PanicByUnit.ContainsKey(unit)) return;
            unit.SetSelected(false);
            if (unit.OwnerMode != null && unit.OwnerMode.DontSelectPanicersLikeOriginal(unit.CombatNationLikeOriginal))
                unit.NotSelectable = true;
            int oldGroup, remaining;
            bool erased;
            C2FormationRuntimeV167LikeOriginal.TryRemoveUnitFromFormationForPanicV404LikeOriginal(
                unit, out oldGroup, out remaining, out erased);
            if (erased && oldGroup >= 0) FormationStates.Remove(oldGroup);

            byte dir = unit.RealDir;
            PanicState p = new PanicState
            {
                Unit = unit,
                Step = 0,
                NextStepAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal,
                Direction = dir
            };
            PanicByUnit[unit] = p;
            // PanicUnit sets UnitSpeed/CurUnitSpeed to 64. The Unity runtime
            // keeps the same original-speed scalar on the linked unit runtime.
            C2UnitOriginalRuntimeLinkLikeOriginal panicLinkV404 = unit.RuntimeLinkCachedLikeOriginal;
            if (panicLinkV404 != null && panicLinkV404.Runtime != null)
                panicLinkV404.Runtime.OriginalUnitSpeedLikeOriginal = 64;
            IssuePanicStepV404LikeOriginal(p);
            Debug.Log("[C2:MORALE V404 PANIC] unit='" + (unit.SourceMonsterId ?? string.Empty) +
                      "' oldGroup=" + oldGroup.ToString(CultureInfo.InvariantCulture) +
                      " morale=" + unit.MoraleLikeOriginal.ToString("0.##", CultureInfo.InvariantCulture) +
                      " indexNibble=" + ((unit.C2ObjectIndexV408LikeOriginal & 15)).ToString(CultureInfo.InvariantCulture));
        }

        internal static void TickPanicV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            PanicState p;
            if (!PanicByUnit.TryGetValue(unit, out p) || p == null) return;
            if (unit.IsDeadLikeOriginal || !unit.isActiveAndEnabled)
            {
                PanicByUnit.Remove(unit);
                return;
            }

            // PanicUnitLink is an order link. It is called again only after the
            // NewMonsterSendTo child movement has finished. Do not rewrite a live
            // destination on a timer.
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.HasMoveTargetLikeOriginal())
                return;
            if (C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal < p.NextStepAt)
                return;

            RunPanicUnitLinkV404C(p);
        }

        private static void IssuePanicStepV404LikeOriginal(PanicState p)
        {
            // Entry into PanicUnit immediately installs PanicUnitLink. Run the first
            // link iteration now, exactly as the retail order does.
            RunPanicUnitLinkV404C(p);
        }

        private static void RunPanicUnitLinkV404C(PanicState p)
        {
            using (PanicStepMarker.Auto())
                RunPanicUnitLinkCoreLikeOriginal(p);
        }

        private static void RunPanicUnitLinkCoreLikeOriginal(PanicState p)
        {
            if (p == null || p.Unit == null) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal u = p.Unit;
            MoraleConfig cfg = ConfigForUnitV404LikeOriginal(u);

            // Morale.cpp::PanicUnitLink, in source order.
            p.Step++;
            if (p.Step > 1)
                AddMoraleInRadiusV404LikeOriginal(u, 256.0f, -cfg.FearDec, false, "panic_fear");

            int ps = GetPsixozV404CLikeOriginal(u) << 5;
            bool exitBranch =
                (C2RetailRandomV407LikeOriginal.Rando(u) < ps && p.Step > 2) ||
                p.Step > cfg.MaxPanicSteps;
            if (exitBranch)
            {
                // NEWMORALE has PANIC_STEPS=0. Keep the original nested branch: a
                // non-peasant may wait for a command center; exceeding MAX always
                // deletes the panic order even when no center exists.
                if ((!u.IsPeasantLikeOriginal() && p.Step > 0) || p.Step > cfg.MaxPanicSteps)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal center =
                        GetNearestCenterV407LikeOriginal(u);
                    if (center != null || p.Step > cfg.MaxPanicSteps)
                    {
                        FinishPanicV404C(p, center != null ? "nearest_center" : "max_steps");
                        if (center != null)
                            SendPanicUnitToCenterV407LikeOriginal(u, center);
                    }
                }
                else
                {
                    FinishPanicV404C(p, "psixoz_peasant");
                }
                return;
            }

            int x0 = Mathf.FloorToInt(CurrentRealX(u) / 256.0f);
            int y0 = Mathf.FloorToInt(CurrentRealY(u) / 256.0f);
            PreparePanicDensityLikeOriginal();
            int mins = 1000000;
            int maxsR = 0;
            int bx = -1;
            int by = -1;
            int chosenRawDensity = 0;

            // Morale.cpp consumes the exact TopoGraf.cpp Rarr[3] and increments
            // i by two. Do not use a reconstructed coordinate list.
            int rarr3Count = C2TopologyCoreV401LikeOriginal.GetRadioCountV407LikeOriginal(3);
            for (int i = 0; i < rarr3Count; i += 2)
            {
                int ox, oy;
                if (!C2TopologyCoreV401LikeOriginal.TryGetRadioOffsetV407LikeOriginal(3, i, out ox, out oy))
                    continue;
                int x = x0 + (ox << 2);
                int y = y0 + (oy << 2);
                if (PanicCheckBarV404C(x, y) ||
                    PanicCheckBarV404C((x + x0) / 2, (y + y0) / 2))
                    continue;

                int sr = GetEnemyDensityV404CLikeOriginal(x, y, u);
                int score = sr + (C2RetailRandomV407LikeOriginal.Rando(u) & 7);
                if (score < mins)
                {
                    mins = score;
                    bx = x;
                    by = y;
                    chosenRawDensity = sr;
                }
                if (sr > maxsR) maxsR = sr;
            }

            if (maxsR == 0)
            {
                int tc = C2OriginalMovementMathV352.TCos[p.Direction];
                int ts = C2OriginalMovementMathV352.TSin[p.Direction];
                int bx1 = x0 + (tc >> 5) + (C2RetailRandomV407LikeOriginal.Rando(u) & 31) - 15;
                int by1 = y0 + (ts >> 5) + (C2RetailRandomV407LikeOriginal.Rando(u) & 31) - 15;
                if (!PanicCheckBarV404C(bx1, by1) &&
                    !PanicCheckBarV404C((bx1 + x0) / 2, (by1 + y0) / 2))
                {
                    bx = bx1;
                    by = by1;
                    chosenRawDensity = 0;
                }
                else
                {
                    // Retail calls GetDir even when bx/by are still -1. Preserve
                    // that state transition instead of guarding it away.
                    p.Direction = DirectionFromDeltaV404LikeOriginal(bx - x0, by - y0);
                }
            }
            else
            {
                p.Direction = DirectionFromDeltaV404LikeOriginal(bx - x0, by - y0);
            }

            if (bx != -1)
            {
                bx += (C2RetailRandomV407LikeOriginal.Rando(u) & 7) - 3;
                by += (C2RetailRandomV407LikeOriginal.Rando(u) & 7) - 3;
                float txReal = bx * 256.0f;
                float tyReal = by * 256.0f;
                C2UnitOriginalRuntimeLinkLikeOriginal moveLink = u.RuntimeLinkCachedLikeOriginal;
                if (moveLink != null)
                {
                    moveLink.SetValidatedDirectMoveDestinationRealLikeOriginal(
                        txReal, tyReal,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        false, 0, false, "panic_v407_Morale.cpp::PanicUnitLink");
                }
                else
                {
                    u.SetMoveDestinationRealLikeOriginal(
                        txReal, tyReal,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        false, 0);
                }
            }

            else
            {
                FinishPanicV404C(p, "no_destination");
                return;
            }

            p.NextStepAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + 0.04f;
            if (LogPanicStepsLikeOriginal) Debug.Log("[C2:MORALE V407 PANIC_STEP] unit='" + (u.SourceMonsterId ?? string.Empty) +
                      "' patrolX=" + p.Step.ToString(CultureInfo.InvariantCulture) +
                      " patrolDir=" + p.Direction.ToString(CultureInfo.InvariantCulture) +
                      " bxby=" + bx.ToString(CultureInfo.InvariantCulture) + "/" +
                      by.ToString(CultureInfo.InvariantCulture) +
                      " rawDensity=" + chosenRawDensity.ToString(CultureInfo.InvariantCulture) +
                      " maxDensity=" + maxsR.ToString(CultureInfo.InvariantCulture));
        }

        private static void FinishPanicV404C(PanicState p, string reason)
        {
            if (p == null || p.Unit == null) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal u = p.Unit;
            u.MoraleLikeOriginal = 33.0f;
            PanicByUnit.Remove(u);
            Debug.Log("[C2:MORALE V404C PANIC_END] unit='" + (u.SourceMonsterId ?? string.Empty) +
                      "' patrolX=" + p.Step.ToString(CultureInfo.InvariantCulture) +
                      " morale=33 reason='" + (reason ?? string.Empty) + "'");
        }

        private static bool PanicCheckBarV404C(int x, int y)
        {
            // CheckBar(x-1,y-1,3,3) is a 3x3 bar centred at OneObject x/y.
            // OneObject::x/y units are RealX/RealY>>8.
            return C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
                x * 256.0f, y * 256.0f, 1);
        }

        private static int GetEnemyDensityV404CLikeOriginal(
            int x, int y, C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0;
            byte mask = NationMaskV407LikeOriginal(unit.CombatNationLikeOriginal);
            int cx0 = x >> 3;
            int cy0 = y >> 3;
            int sum = 0;
            for (int radius = 0; radius < 5; radius++)
            {
                int n = C2TopologyCoreV401LikeOriginal.GetRadioCountV407LikeOriginal(radius);
                for (int i = 0; i < n; i++)
                {
                    int ox, oy;
                    if (!C2TopologyCoreV401LikeOriginal.TryGetRadioOffsetV407LikeOriginal(radius, i, out ox, out oy))
                        continue;
                    int xx = cx0 + ox;
                    int yy = cy0 + oy;
                    if (xx < 0 || yy < 0) continue;
                    int ss = GetEnemyDensityInCellV407LikeOriginal(x, y, xx, yy, mask);
                    if (ss == -1)
                    {
                        // Preserve Morale.cpp literally: xx/yy are MCount-cell
                        // coordinates while x/y are OneObject coordinates.
                        int rr = C2OriginalMovementMathV352.Norma(xx - x, yy - y);
                        ss = 1000 * 20 / (rr + 50);
                    }
                    sum += ss;
                }
            }
            return sum;
        }

        private static void PreparePanicDensityLikeOriginal()
        {
            PanicPresenceByCell.Clear();
            PanicHiddenByCell.Clear();
            PanicBuildingByCell.Clear();
            PanicBuildings = null;
            var units = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int index = 0; index < units.Length; index++)
            {
                var unit = units[index];
                if (unit == null) continue;
                long key = CellKeyV408LikeOriginal(Mathf.RoundToInt(CurrentRealX(unit)) >> 11,
                    Mathf.RoundToInt(CurrentRealY(unit)) >> 11);
                if (unit.isActiveAndEnabled)
                {
                    byte presence;
                    PanicPresenceByCell.TryGetValue(key, out presence);
                    PanicPresenceByCell[key] = (byte)(presence | NationMaskV407LikeOriginal(unit.CombatNationLikeOriginal));
                }
                var link = unit.RuntimeLinkCachedLikeOriginal;
                if (link == null || link.Runtime == null || !link.Runtime.HiddenInsideBuildingLikeOriginal) continue;
                int count;
                PanicHiddenByCell.TryGetValue(key, out count);
                PanicHiddenByCell[key] = count + 1;
            }
        }

        private static int GetEnemyDensityInCellV407LikeOriginal(
            int x, int y, int sourceCellX, int sourceCellY, byte mask)
        {
            long key = CellKeyV408LikeOriginal(sourceCellX + 1, sourceCellY + 1);
            byte presence;
            if (PanicPresenceByCell.TryGetValue(key, out presence) && (presence & unchecked((byte)~mask)) != 0)
                return -1;
            int hidden;
            if (!PanicHiddenByCell.TryGetValue(key, out hidden) || hidden <= 0) return 0;
            if (PanicBuildings == null)
            {
                PanicBuildings = UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int index = 0; index < PanicBuildings.Length; index++)
                {
                    var building = PanicBuildings[index];
                    if (building == null || building.LifeLikeOriginal <= 0) continue;
                    long buildingKey = CellKeyV408LikeOriginal(building.RealX >> 11, building.RealY >> 11);
                    if (!PanicBuildingByCell.ContainsKey(buildingKey)) PanicBuildingByCell.Add(buildingKey, building);
                }
            }
            C2SettlementBuildingSelectableV1LikeOriginal selected;
            if (!PanicBuildingByCell.TryGetValue(key, out selected) || (NationMaskV407LikeOriginal(selected.Nation) & mask) != 0)
                return 0;
            int distance = C2OriginalMovementMathV352.Norma(x - (selected.RealX >> 8), y - (selected.RealY >> 8));
            return 20000 / (distance + 50);
        }

        private static byte NationMaskV407LikeOriginal(int nation)
        {
            return unchecked((byte)(1 << Mathf.Clamp(nation, 0, 7)));
        }

        private static void AutoSendFreeUnitsToHomeV408LikeOriginal()
        {
            // Morale.cpp consumes this random number even when no unit qualifies.
            if (C2RetailRandomV407LikeOriginal.Rando(null) >= 128) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = GetSortedUnitsLikeOriginal();
            for (int i = 0; i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || u.IsDeadLikeOriginal || !u.NotSelectable || PanicByUnit.ContainsKey(u)) continue;
                if (u.OwnerMode == null || !u.OwnerMode.DontSelectPanicersLikeOriginal(u.CombatNationLikeOriginal)) continue;
                var runtimeLink = u.RuntimeLinkCachedLikeOriginal;
                if (runtimeLink != null && runtimeLink.Runtime != null && runtimeLink.Runtime.PreciseBornPathLikeOriginal) continue;
                int gid;
                if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(u, out gid)) continue;
                C2UnitOrderRuntimeV325LikeOriginal ord = C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(u);
                C2NeutralPeasantUnitInfoV2LikeOriginal target;
                bool attacking = C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(u, out target);
                if (ord != null && ord.CurrentLikeOriginal != C2UnitOrderKindV325LikeOriginal.Stand && !attacking) continue;
                C2SettlementBuildingSelectableV1LikeOriginal center = GetNearestCenterV407LikeOriginal(u, false);
                if (center == null) continue;
                int d = C2OriginalMovementMathV352.Norma(
                    center.RealX - Mathf.RoundToInt(CurrentRealX(u)),
                    center.RealY - Mathf.RoundToInt(CurrentRealY(u)));
                if (d <= 1000 * 16) continue;
                u.MoraleLikeOriginal = 33.0f;
                SendPanicUnitToCenterV407LikeOriginal(u, center);
                u.MoraleLikeOriginal = 33.0f;
            }
        }

        private static C2SettlementBuildingSelectableV1LikeOriginal GetNearestCenterV407LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, bool matchUnitType = true)
        {
            if (unit == null) return null;
            int best = 100000000;
            C2SettlementBuildingSelectableV1LikeOriginal result = null;
            C2SettlementBuildingSelectableV1LikeOriginal[] buildings =
                UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; buildings != null && i < buildings.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                if (b == null || b.LifeLikeOriginal <= 0 || b.Nation != unit.CombatNationLikeOriginal) continue;
                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(b);
                if (!md.CommandCenter) continue;
                int rr = C2OriginalMovementMathV352.Norma(
                    b.RealX - Mathf.RoundToInt(CurrentRealX(unit)),
                    b.RealY - Mathf.RoundToInt(CurrentRealY(unit)));
                bool canProduce = md.GlobalCommandCenter || (matchUnitType && CenterCanProduceUnitV407LikeOriginal(b, unit));
                if (!canProduce) rr += 30000 * 16;
                if (rr < best)
                {
                    best = rr;
                    result = b;
                }
            }
            return result;
        }

        private static bool CenterCanProduceUnitV407LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal center,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            // PAble[NIndex] data-access adapter: the existing original produce
            // catalog was built from the same nation production lists.
            string audit;
            List<C2OriginalProduceItemV13> items =
                C2OriginalProduceCatalogV13.BuildForSelectedBuilding(center, out audit);
            string wantedId = C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(
                unit.SourceMonsterId ?? string.Empty);
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 unitMd =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string wantedMd = Path.GetFileNameWithoutExtension(unitMd.Path ?? string.Empty);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                C2OriginalProduceItemV13 item = items[i];
                if (item == null) continue;
                string itemId = C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(item.UnitId ?? string.Empty);
                if (!string.IsNullOrEmpty(wantedId) && string.Equals(itemId, wantedId, StringComparison.OrdinalIgnoreCase))
                    return true;
                string itemMd = Path.GetFileNameWithoutExtension(item.MdName ?? string.Empty);
                if (!string.IsNullOrEmpty(wantedMd) && string.Equals(itemMd, wantedMd, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void SendPanicUnitToCenterV407LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2SettlementBuildingSelectableV1LikeOriginal center)
        {
            if (unit == null || center == null) return;
            int xJitter = (C2RetailRandomV407LikeOriginal.Rando(unit) & 127) - 64;
            int yJitter = (C2RetailRandomV407LikeOriginal.Rando(unit) & 127) - 64 + 180;
            // SmartSendTo also consumes sx/sy. The current Unity movement API has no
            // equivalent velocity-hint fields, but consuming them preserves rpos.
            int sx = (C2RetailRandomV407LikeOriginal.Rando(unit) & 63) - 32;
            int sy = (C2RetailRandomV407LikeOriginal.Rando(unit) & 63) - 32;
            _ = sx;
            _ = sy;
            float tx = center.RealX + xJitter * 16.0f;
            float ty = center.RealY + yJitter * 16.0f;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null)
                link.SetValidatedDirectMoveDestinationRealLikeOriginal(
                    tx, ty, C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false, 0, false, "Morale.cpp::GetNearestCenter/NewMonsterSmartSendTo");
            else
                unit.SetMoveDestinationRealLikeOriginal(
                    tx, ty, C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false, 0);
            unit.MoraleLikeOriginal = 33.0f;
        }

        private static int GetPsixozV404CLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string md = MdPathV404LikeOriginal(unit);
            if (string.IsNullOrEmpty(md)) return 32;
            int v;
            if (PsixozByMdV404C.TryGetValue(md, out v)) return v;
            v = 32; // NewMonster default
            try
            {
                string[] lines = File.ReadAllLines(md);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = SplitV404LikeOriginal(StripCommentV404LikeOriginal(lines[i]));
                    if (parts.Length >= 2 &&
                        string.Equals(parts[0], "PSIXOZ", StringComparison.OrdinalIgnoreCase))
                    {
                        int parsed;
                        if (int.TryParse(parts[1], NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out parsed))
                            v = parsed;
                    }
                }
            }
            catch { }
            PsixozByMdV404C[md] = v;
            return v;
        }

        internal static int GetStartMoraleV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string md = MdPathV404LikeOriginal(unit);
            if (string.IsNullOrEmpty(md)) return 50;
            int cached;
            if (StartMoraleByMd.TryGetValue(md, out cached)) return cached;
            int value = 50;
            try
            {
                string[] lines = File.ReadAllLines(md);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = StripCommentV404LikeOriginal(lines[i]);
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] p = SplitV404LikeOriginal(line);
                    if (p.Length >= 2 && string.Equals(p[0], "FEARSTART", StringComparison.OrdinalIgnoreCase))
                    {
                        int v;
                        if (int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) value = v;
                    }
                }
            }
            catch { }
            StartMoraleByMd[md] = value;
            return value;
        }

        private static int GetFearTypeV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int attackType)
        {
            string md = MdPathV404LikeOriginal(unit);
            if (string.IsNullOrEmpty(md)) return -1;
            int[] values;
            if (!FearTypeByMd.TryGetValue(md, out values))
            {
                values = new int[4] { -1, -1, -1, -1 };
                try
                {
                    string[] lines = File.ReadAllLines(md);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = StripCommentV404LikeOriginal(lines[i]);
                        string[] p = SplitV404LikeOriginal(line);
                        if (p.Length < 3 || !string.Equals(p[0], "FEARTYPE", StringComparison.OrdinalIgnoreCase)) continue;
                        int idx, val;
                        if (int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out idx) &&
                            int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out val) &&
                            idx >= 0 && idx < values.Length)
                            values[idx] = val;
                    }
                }
                catch { }
                FearTypeByMd[md] = values;
            }
            return attackType >= 0 && attackType < values.Length ? values[attackType] : -1;
        }

        private static bool IsNoMoraleV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string md = MdPathV404LikeOriginal(unit);
            if (string.IsNullOrEmpty(md)) return false;
            bool cached;
            if (NoMoraleByMd.TryGetValue(md, out cached)) return cached;
            bool no = false;
            try
            {
                string[] lines = File.ReadAllLines(md);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] p = SplitV404LikeOriginal(StripCommentV404LikeOriginal(lines[i]));
                    if (p.Length > 0 && string.Equals(p[0], "NOMORALE", StringComparison.OrdinalIgnoreCase))
                    {
                        no = true;
                        break;
                    }
                }
            }
            catch { }
            NoMoraleByMd[md] = no;
            return no;
        }

        private static MoraleConfig ConfigForUnitV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string md = MdPathV404LikeOriginal(unit);
            MoraleConfig cfg;
            if (ConfigByMdPath.TryGetValue(md, out cfg)) return cfg;

            string root = ResolveDataRootV404LikeOriginal(md);
            if (string.IsNullOrEmpty(root)) root = "<default>";
            if (!ConfigByDataRoot.TryGetValue(root, out cfg))
            {
                cfg = new MoraleConfig();
                if (root != "<default>")
                {
                    LoadMoraleDatV404LikeOriginal(root, cfg);
                    string settingsPath = FindFileIgnoreCaseV404LikeOriginal(root, "EngineSettings.xml");
                    if (!string.IsNullOrEmpty(settingsPath))
                    {
                        try
                        {
                            XElement settings = XElement.Load(settingsPath);
                            XElement tiring = settings.Element("AllowTiring");
                            if (tiring != null)
                                cfg.AllowTiring = tiring.Value.Trim() == "1" || string.Equals(tiring.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase);
                        }
                        catch (Exception exception) when (exception is System.Xml.XmlException || exception is IOException || exception is UnauthorizedAccessException)
                        {
                            Debug.LogWarning("[C2:MORALE] Cannot read engine settings: " + exception.Message);
                        }
                    }
                }
                ConfigByDataRoot[root] = cfg;
            }
            ConfigByMdPath[md] = cfg;
            return cfg;
        }

        private static void LoadMoraleDatV404LikeOriginal(string root, MoraleConfig cfg)
        {
            string path = FindFileIgnoreCaseV404LikeOriginal(root, "NewMorale.dat");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] p = SplitV404LikeOriginal(StripCommentV404LikeOriginal(lines[i]));
                    if (p.Length < 2) continue;
                    float f;
                    if (!float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out f)) continue;
                    string k = p[0];
                    if (string.Equals(k, "Morale_DamageDec", StringComparison.OrdinalIgnoreCase)) cfg.Damage[0] = f;
                    else if (k.StartsWith("Morale_DamageDec", StringComparison.OrdinalIgnoreCase)) SetIndexedV404LikeOriginal(cfg.Damage, k, "Morale_DamageDec", f);
                    else if (string.Equals(k, "Morale_BackDamageDec", StringComparison.OrdinalIgnoreCase)) cfg.BackDamage[0] = f;
                    else if (k.StartsWith("Morale_BackDamageDec", StringComparison.OrdinalIgnoreCase)) SetIndexedV404LikeOriginal(cfg.BackDamage, k, "Morale_BackDamageDec", f);
                    else if (string.Equals(k, "Morale_KillDec", StringComparison.OrdinalIgnoreCase)) cfg.KillDec = f;
                    else if (string.Equals(k, "Morale_FearDec", StringComparison.OrdinalIgnoreCase)) cfg.FearDec = f;
                    else if (string.Equals(k, "Morale_IncTime", StringComparison.OrdinalIgnoreCase)) cfg.IncTime = f;
                    else if (string.Equals(k, "MaxMorale_CenterInc", StringComparison.OrdinalIgnoreCase)) cfg.CenterInc = f;
                    else if (string.Equals(k, "MaxMorale_FormIncPerUnit", StringComparison.OrdinalIgnoreCase)) cfg.FormIncPerUnit = f;
                    else if (string.Equals(k, "MaxMorale_CenterRadius", StringComparison.OrdinalIgnoreCase)) cfg.CenterRadius = (int)f;
                    else if (string.Equals(k, "MaxMorale_FormIncOfficer", StringComparison.OrdinalIgnoreCase)) cfg.FormIncOfficer = f;
                    else if (string.Equals(k, "MaxMorale_FormIncBaraban", StringComparison.OrdinalIgnoreCase)) cfg.FormIncDrummer = f;
                    else if (string.Equals(k, "MaxMorale_FormIncFlag", StringComparison.OrdinalIgnoreCase)) cfg.FormIncFlag = f;
                    else if (string.Equals(k, "MaxMorale_FormIncPerFrag", StringComparison.OrdinalIgnoreCase)) cfg.FormIncPerFrag = f;
                    else if (string.Equals(k, "MaxMorale_DecWhen30Lost", StringComparison.OrdinalIgnoreCase)) cfg.MaxDecWhenLost = f;
                    else if (string.Equals(k, "MaxMorale_LostCriticalPercent", StringComparison.OrdinalIgnoreCase)) cfg.LostCriticalPercent = (int)f;
                    else if (string.Equals(k, "Morale_DecWhen30Lost", StringComparison.OrdinalIgnoreCase)) cfg.MoraleDecWhenLost = f;
                    else if (string.Equals(k, "Morale_FormationShield15", StringComparison.OrdinalIgnoreCase)) cfg.FormationShield15 = (int)f;
                    else if (string.Equals(k, "Morale_FormationShield196", StringComparison.OrdinalIgnoreCase)) cfg.FormationShield196 = (int)f;
                    else if (string.Equals(k, "Morale_IncDecCoefficient", StringComparison.OrdinalIgnoreCase)) cfg.IncDecCoefficient = (int)f;
                    else if (string.Equals(k, "Morale_MinDueToTired", StringComparison.OrdinalIgnoreCase)) cfg.MinDueToTired = f;
                    else if (string.Equals(k, "Morale_DecWhenTired", StringComparison.OrdinalIgnoreCase)) cfg.DecWhenTired = f;
                    else if (string.Equals(k, "MAX_PANIC_STEPS", StringComparison.OrdinalIgnoreCase)) cfg.MaxPanicSteps = (int)f;
                }
                Debug.Log("[C2:MORALE V404 DATA] path='" + path + "' kill=" + cfg.KillDec.ToString("0.###", CultureInfo.InvariantCulture) +
                          " fear=" + cfg.FearDec.ToString("0.###", CultureInfo.InvariantCulture) +
                          " tiredMin=" + cfg.MinDueToTired.ToString("0.###", CultureInfo.InvariantCulture) +
                          " tiredDec=" + cfg.DecWhenTired.ToString("0.###", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:MORALE V404 DATA] failed '" + path + "': " + ex.Message);
            }
        }

        private static void SetIndexedV404LikeOriginal(float[] dst, string key, string prefix, float value)
        {
            string tail = key.Substring(prefix.Length);
            if (string.IsNullOrEmpty(tail)) { dst[0] = value; return; }
            int idx;
            if (int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out idx) && idx >= 0 && idx < dst.Length)
                dst[idx] = value;
        }

        private static string MdPathV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return string.Empty;
            try
            {
                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
                if (!string.IsNullOrEmpty(md.Path)) return md.Path;
            }
            catch { }
            return unit.ResolvedMd ?? string.Empty;
        }

        private static string ResolveDataRootV404LikeOriginal(string mdPath)
        {
            if (string.IsNullOrEmpty(mdPath)) return string.Empty;
            try
            {
                DirectoryInfo d = new FileInfo(mdPath).Directory;
                while (d != null)
                {
                    if (string.Equals(d.Name, "Data", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Name, "Data1", StringComparison.OrdinalIgnoreCase)) return d.FullName;
                    if (File.Exists(Path.Combine(d.FullName, "NewMorale.dat"))) return d.FullName;
                    d = d.Parent;
                }
            }
            catch { }
            return string.Empty;
        }

        private static string FindFileIgnoreCaseV404LikeOriginal(string dir, string name)
        {
            try
            {
                string exact = Path.Combine(dir, name);
                if (File.Exists(exact)) return exact;
                string[] files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < files.Length; i++)
                    if (string.Equals(Path.GetFileName(files[i]), name, StringComparison.OrdinalIgnoreCase)) return files[i];
            }
            catch { }
            return string.Empty;
        }

        private static string StripCommentV404LikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            int p = line.IndexOf("//", StringComparison.Ordinal);
            if (p >= 0) line = line.Substring(0, p);
            p = line.IndexOf(';');
            if (p >= 0) line = line.Substring(0, p);
            return line.Trim();
        }

        private static string[] SplitV404LikeOriginal(string line)
        {
            return string.IsNullOrWhiteSpace(line)
                ? new string[0]
                : line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static float CurrentRealX(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
        }

        private static float CurrentRealY(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
        }

        private static byte DirectionFromDeltaV404LikeOriginal(float dx, float dy)
        {
            if (dx * dx + dy * dy < 0.001f) return 0;
            return (byte)(Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(dy, dx) / (Mathf.PI * 2.0f) * 256.0f, 256.0f)) & 255);
        }
    }
}
