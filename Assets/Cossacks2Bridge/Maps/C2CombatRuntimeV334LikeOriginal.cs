using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Data-driven Cossacks II combat test runtime. Values come from the selected
    // unit's shipped MD; morale constants mirror Data/NewMorale.dat.
    public sealed class C2CombatRuntimeV334LikeOriginal : MonoBehaviour
    {
        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _targetUnit;
        private C2SettlementBuildingSelectableV1LikeOriginal _targetBuilding;
        private Vector3 _fallbackTargetWorld;
        private C2OriginalProduceCatalogV13.C2MdIconInfoV13 _md;
        private bool _active;
        private bool _artillery;
        private bool _slowRecharge;
        private bool _slowRechargeInProgress;
        private int _pendingSlowRechargeTicks;
        private bool _waitState5ExitForReloadV403DLikeOriginal;
        private float _nextAttackAt;
        private bool _attackInProgress;
        private bool _impactApplied;
        private int _attackMode;
        private float _impactRealX;
        private float _impactRealY;
        private readonly WeaponEffectLikeOriginal[] _weaponEffects =
            new WeaponEffectLikeOriginal[4];
        private ComplexCannonProfileLikeOriginal _complexCannon;
        private int _complexCannonStage;
        private float _complexCannonStageEndsAt;

        // V390: persistent weapon-cycle state used by the original-style HUD.
        // ATTACK_PAUSE starts at the active shot frame; SLOWRECHARGE then visualizes
        // that delay with #ATTACK3. Keep this independent from the current target,
        // so retargeting cannot magically refill a musket.
        private int _weaponCycleModeLikeOriginal = -1;
        private float _weaponCycleStartedAtLikeOriginal;
        private float _weaponCycleReadyAtLikeOriginal;
        private bool _weaponCycleHasShotLikeOriginal;
        private int _shotSerialLikeOriginal;
        // V395: the original has distinct MeleeAttack / Fire / ThrowGrenade orders.
        // Lock the selected weapon for the lifetime of this combat order; distance
        // must never silently convert a bayonet charge back into a rifle shot.
        private int _forcedOrderModeV395LikeOriginal = -1;
        private int _targetFormationGroupIdV395LikeOriginal = -1;
        // Legacy V399 one-volley marker retained only for compatibility with old
        // callers. V405 no longer starts rifle fire from the HUD; the actual owner is
        // BrigadeOrder_RifleAttack below, as in COSSACKS2.
        private bool _rifleButtonVolleyV399LikeOriginal;
        // V405: exact BrigadeOrder_RifleAttack ownership. Unlike V399, the
        // per-unit AttackObj remains owned by the brigade order until that order
        // deletes itself. The order destructor may allow an ATTACK frame already
        // in progress to finish once, exactly like ClearTargetObj(SN=1).
        private bool _rifleBrigadeOrderV405LikeOriginal;
        private bool _finishCurrentRifleAttackThenStopV405LikeOriginal;
        // BrigadeOrder_RifleAttack::ClearTargetObj also sees a pre-existing generic
        // AttackObj. If that object is already in ATTACK/PATTACK, retail sets SN=1
        // and allows exactly that attack animation to finish before deleting it.
        private bool _finishPreExistingAttackObjThenStopV405LikeOriginal;
        // V406: AttackSelected advances the brigade as one body; BrigadeOrder_Bitva
        // may instead let an individual AttackObj close locally on its EnemyID.
        private bool _allowLocalMeleeApproachV406LikeOriginal = true;

        private sealed class WeaponEffectLikeOriginal
        {
            public bool HasWeapon;
            public string RootName = string.Empty;
            public string DamageWeaponName = string.Empty;
            public int Damage;
            public int Radius;
            public int Speed;
            public int Propagation;
            public bool FullParent;
        }

        private sealed class WeaponDefinitionLikeOriginal
        {
            public string Name = string.Empty;
            public string AnimationName = string.Empty;
            public int Damage;
            public int Radius;
            public int Speed;
            public int Propagation;
            public bool FullParent;
            public readonly List<string> Children = new List<string>();
            public readonly List<string> Sync = new List<string>();
        }

        private sealed class ComplexCannonProfileLikeOriginal
        {
            public string ComplexId = string.Empty;
            public string QuantId = string.Empty;
            public int AttackX;
            public int AttackY;
            public int AttackZ;
            public readonly int[] FireTicks = new int[3];
            public readonly int[] ReloadTicks = new int[3];
        }

        private static readonly Dictionary<string, Dictionary<string, WeaponDefinitionLikeOriginal>>
            WeaponDefinitionsByNdsPathLikeOriginal =
                new Dictionary<string, Dictionary<string, WeaponDefinitionLikeOriginal>>(
                    StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ComplexCannonProfileLikeOriginal>
            ComplexCannonByMdPathLikeOriginal =
                new Dictionary<string, ComplexCannonProfileLikeOriginal>(
                    StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<int> GrenadeArmedUnits = new HashSet<int>();
        // BrigadeAI.cpp uses three different orders (MeleeAttack, Fire,
        // ThrowGrenade).  Keep that order choice until the next explicit weapon
        // command instead of re-selecting a weapon from distance every frame.
        private static readonly Dictionary<int, int> CommandModeByUnit = new Dictionary<int, int>();

        // NewMon.cpp::Nation::CreateNewMonsterAt (SIMPLEMANAGE):
        //   if(NM->ArmAttack) G->ArmAttack=1;
        //   G->RifleAttack=0; G->GroundState=0; G->NewState=0; delay=MaxDelay=0.
        // Keep these as persistent OneObject-like fields on the managed unit record.
        internal static void InitializeUnitCombatStateFromMdV396LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md)
        {
            if (unit == null || unit.CombatStateInitializedV396LikeOriginal) return;
            bool armAttack = MdContainsCommandLikeOriginal(md.Path, "ARMATTACK");
            unit.ArmAttackCapableV396LikeOriginal = armAttack;
            unit.ArmAttackV396LikeOriginal = armAttack;
            unit.RifleAttackV396LikeOriginal = false;
            unit.GroundStateV396LikeOriginal = 0;
            unit.NewStateV396LikeOriginal = 0;
            unit.CombatStateInitializedV396LikeOriginal = true;
            CommandModeByUnit.Remove(unit.GetInstanceID());
            GrenadeArmedUnits.Remove(unit.GetInstanceID());
        }

        public static void EnsureUnitCombatStateV396LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.CombatStateInitializedV396LikeOriginal) return;
            InitializeUnitCombatStateFromMdV396LikeOriginal(
                unit, C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit));
        }

        // Brigade.cpp::Brigade::CreateFromGroup writes RifleAttack=0 for command
        // members and every soldier added to the brigade.  It does not choose a
        // replacement weapon by range.
        internal static bool IsAttackOrderActiveV403LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return false;
            C2CombatRuntimeV334LikeOriginal combat = unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            return combat != null && combat._active;
        }

        public static void ResetRifleAttackOnFormationCreateV396LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            EnsureUnitCombatStateV396LikeOriginal(unit);
            unit.RifleAttackV396LikeOriginal = false;
            int id = unit.GetInstanceID();
            CommandModeByUnit.Remove(id);
            GrenadeArmedUnits.Remove(id);
        }

        // Multi.cpp::SetArmAttackState under SIMPLEMANAGE.  Raw values used by
        // retail UI are 1 for arm/melee and 128/129 for rifle off/on.
        public static bool SetArmAttackStateValueV396LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, int value)
        {
            if (unit == null) return false;
            EnsureUnitCombatStateV396LikeOriginal(unit);
            if (!unit.ArmAttackCapableV396LikeOriginal) return false;

            int id = unit.GetInstanceID();
            GrenadeArmedUnits.Remove(id);
            if ((value & 128) != 0)
            {
                // Multi.cpp::SetArmAttackState SIMPLEMANAGE rifle branch only
                // writes RifleAttack (and brigade AttEnm when enabling). It does
                // NOT change GroundState/NewState and does NOT start PATTACK/
                // PSTAND. The old port called SetCombatPosture here, which made
                // every rifle-button click alter the current animation and thus
                // the stamina line.
                unit.RifleAttackV396LikeOriginal = (value & 127) != 0;
                CommandModeByUnit[id] = unit.RifleAttackV396LikeOriginal ? 1 : 0;
                return true;
            }

            // Multi.cpp::SetArmAttackState: selecting arm attack clears the
            // current rifle AttackObj order when it can be broken. Do this before
            // installing the new melee state, otherwise the old #ATTACK1 active
            // frame can still fire after the bayonet click.
            C2CombatRuntimeV334LikeOriginal oldCombatV399 =
                unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (oldCombatV399 != null &&
                (unit.RifleAttackV396LikeOriginal ||
                 oldCombatV399._forcedOrderModeV395LikeOriginal == 1 ||
                 oldCombatV399._attackMode == 1 ||
                 oldCombatV399._rifleButtonVolleyV399LikeOriginal))
                oldCombatV399.CancelForExternalOrderLikeOriginal(
                    "Multi.cpp_SetArmAttackState_melee_v399");
            C2UnitOriginalRuntimeLinkLikeOriginal oldLinkV399 =
                unit.RuntimeLinkCachedLikeOriginal;
            if (oldLinkV399 != null)
                oldLinkV399.InterruptSlowRechargeAnimationForExternalOrderV399LikeOriginal(
                    "Multi.cpp_SetArmAttackState_melee_v399");

            unit.RifleAttackV396LikeOriginal = false;
            unit.ArmAttackV396LikeOriginal = true;
            unit.GroundStateV396LikeOriginal = value & 1;
            unit.NewStateV396LikeOriginal = unit.GroundStateV396LikeOriginal;
            CommandModeByUnit[id] = 0;
            unit.SetCombatPostureV322LikeOriginal(0, true);
            return true;
        }

        public static bool SetCommandWeaponModeLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, int mode)
        {
            if (unit == null || mode < 0 || mode > 2) return false;
            EnsureUnitCombatStateV396LikeOriginal(unit);
            int id = unit.GetInstanceID();

            if (mode == 2)
            {
                CommandModeByUnit[id] = 2;
                GrenadeArmedUnits.Add(id);
                unit.SetCombatPostureV322LikeOriginal(2, true);
                return true;
            }

            GrenadeArmedUnits.Remove(id);
            if (unit.ArmAttackCapableV396LikeOriginal)
                return SetArmAttackStateValueV396LikeOriginal(unit, mode == 1 ? 129 : 1);

            // Non-ARMATTACK objects (guns etc.) still use explicit weapon slots.
            CommandModeByUnit[id] = mode;
            unit.SetCombatPostureV322LikeOriginal(mode, true);
            return true;
        }

        public static bool GetFormationRifleAttackStateV398LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            if (representative == null) return false;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers;
            if (C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                    representative, out soldiers) && soldiers != null && soldiers.Count > 0)
            {
                for (int i = 0; i < soldiers.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal soldier = soldiers[i];
                    if (soldier == null || soldier.IsDeadLikeOriginal) continue;
                    EnsureUnitCombatStateV396LikeOriginal(soldier);
                    // UnitsInterface.cpp::GetBrigadeParams: BP->RifleAttack=1 if
                    // any live brigade soldier has OB->RifleAttack set.
                    if (soldier.RifleAttackV396LikeOriginal) return true;
                }
                return false;
            }
            EnsureUnitCombatStateV396LikeOriginal(representative);
            return representative.RifleAttackV396LikeOriginal;
        }

        public static void ClearFormationRifleAttackStateV399LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            string reason,
            bool cancelRangedCombatOrders)
        {
            if (representative == null) return;
            C2BrigadeRifleAttackV405LikeOriginal.OnFormationRifleStateClearedLikeOriginal(
                representative, reason ?? "rifle_state_clear_v405");
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                    representative, out soldiers) || soldiers == null || soldiers.Count == 0)
            {
                soldiers = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                soldiers.Add(representative);
            }

            for (int i = 0; i < soldiers.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal soldier = soldiers[i];
                if (soldier == null) continue;
                EnsureUnitCombatStateV396LikeOriginal(soldier);
                soldier.RifleAttackV396LikeOriginal = false;
                int id = soldier.GetInstanceID();
                int commanded;
                if (CommandModeByUnit.TryGetValue(id, out commanded) && commanded == 1)
                    CommandModeByUnit.Remove(id);

                if (cancelRangedCombatOrders)
                {
                    C2CombatRuntimeV334LikeOriginal combat =
                        soldier.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                    if (combat != null &&
                        (combat._forcedOrderModeV395LikeOriginal == 1 ||
                         combat._attackMode == 1 ||
                         combat._rifleButtonVolleyV399LikeOriginal ||
                         combat._rifleBrigadeOrderV405LikeOriginal))
                        combat.CancelForExternalOrderLikeOriginal(
                            reason ?? "rifle_state_clear_v399");
                }
            }
        }

        // Called by the common C2 order replacement path for normal RMB movement.
        // BrigadeOrder_RifleAttack is destroyed by a replacing brigade order in the
        // original; its destructor clears RifleAttack. The firearm delay itself is
        // NOT cleared: visible ATTACK3 is interrupted by movement and resumes only
        // after the unit is idle again.
        public static void OnExternalOrderReplacementV399LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string reason)
        {
            if (unit == null) return;
            C2BrigadeRifleAttackV405LikeOriginal.OnExternalBrigadeOrderReplacementLikeOriginal(
                unit, reason ?? "external_order_v405");
            EnsureUnitCombatStateV396LikeOriginal(unit);
            unit.RifleAttackV396LikeOriginal = false;
            int id = unit.GetInstanceID();
            int commanded;
            if (CommandModeByUnit.TryGetValue(id, out commanded) && commanded == 1)
                CommandModeByUnit.Remove(id);

            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null)
                link.InterruptSlowRechargeAnimationForExternalOrderV399LikeOriginal(
                    reason ?? "external_order_v399");
        }

        public static bool TryGetCommandWeaponModeLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, out int mode)
        {
            mode = -1;
            if (unit == null) return false;
            EnsureUnitCombatStateV396LikeOriginal(unit);
            if (GrenadeArmedUnits.Contains(unit.GetInstanceID()))
            {
                mode = 2;
                return true;
            }
            if (CommandModeByUnit.TryGetValue(unit.GetInstanceID(), out mode))
                return true;
            if (unit.ArmAttackCapableV396LikeOriginal)
            {
                mode = unit.RifleAttackV396LikeOriginal ? 1 : 0;
                return true;
            }
            return false;
        }

        public static bool ArmGrenadeLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || !unit.CanReceiveOrdersLikeOriginal()) return false;
            EnsureUnitCombatStateV396LikeOriginal(unit);
            int id = unit.GetInstanceID();
            GrenadeArmedUnits.Add(id);
            CommandModeByUnit[id] = 2;
            unit.SetCombatPostureV322LikeOriginal(2, true);
            return true;
        }

        public static bool TryGetMoraleSnapshotLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out float morale, out float maxMorale)
        {
            // V408: V404 is the single authoritative morale implementation.
            return C2MoraleRuntimeV404LikeOriginal.TryGetMoraleSnapshotV404LikeOriginal(
                unit, out morale, out maxMorale);
        }

        public static bool TryGetWeaponCycleV390LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int weaponType,
            out float ready01,
            out bool ready,
            out string phase)
        {
            ready01 = 1.0f;
            ready = true;
            phase = "ready";
            if (unit == null) return false;

            // V391: SLOWRECHARGE belongs to the OneObject/runtime, not to the
            // current combat target/component. This is the source-equivalent
            // delay/MaxDelay state and therefore survives move/retarget orders.
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            int remainingTicks, maximumTicks;
            bool reloadAnimation;
            if (link != null &&
                link.TryGetSlowRechargeProgressLikeOriginal(
                    weaponType, out remainingTicks, out maximumTicks, out reloadAnimation))
            {
                ready01 = maximumTicks > 0
                    ? Mathf.Clamp01((maximumTicks - remainingTicks) / (float)maximumTicks)
                    : 0.0f;
                ready = false;
                phase = reloadAnimation ? "reload_animation" : "reload_pending";
                return true;
            }

            C2CombatRuntimeV334LikeOriginal combat = unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat == null || !combat._weaponCycleHasShotLikeOriginal ||
                combat._weaponCycleModeLikeOriginal != weaponType)
                return true;

            float now = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
            float begin = combat._weaponCycleStartedAtLikeOriginal;
            float finish = combat._weaponCycleReadyAtLikeOriginal;
            if (finish > begin + 0.0001f && now < finish)
            {
                ready01 = Mathf.Clamp01((now - begin) / (finish - begin));
                ready = false;
                phase = "cooldown";
                return true;
            }

            ready01 = 1.0f;
            ready = true;
            phase = combat._attackInProgress && combat._attackMode == weaponType && !combat._impactApplied
                ? "fire" : "ready";
            return true;
        }

        // UnitsInterface.cpp::GetBrigadeParams reads OneObject::delay/MaxDelay,
        // not a normalized "is reloading" flag. This accessor keeps the HUD on
        // the same integer-tick contract.
        public static bool TryGetWeaponDelayTicksV395LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int weaponType,
            out int delayTicks,
            out int maxDelayTicks)
        {
            delayTicks = 0;
            maxDelayTicks = 0;
            if (unit == null) return false;

            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            int remaining, maximum;
            bool animationPlaying;
            if (link != null &&
                link.TryGetSlowRechargeProgressLikeOriginal(
                    weaponType, out remaining, out maximum, out animationPlaying))
            {
                delayTicks = Mathf.Max(0, remaining);
                maxDelayTicks = Mathf.Max(0, maximum);
                return true;
            }

            C2CombatRuntimeV334LikeOriginal combat = unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat != null && combat._weaponCycleHasShotLikeOriginal &&
                combat._weaponCycleModeLikeOriginal == weaponType)
            {
                float begin = combat._weaponCycleStartedAtLikeOriginal;
                float finish = combat._weaponCycleReadyAtLikeOriginal;
                maxDelayTicks = Mathf.Max(0, Mathf.RoundToInt((finish - begin) * 25.0f));
                delayTicks = Mathf.Max(0, Mathf.CeilToInt((finish - C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal) * 25.0f));
                return true;
            }
            return true;
        }


        private void BeginWeaponCycleV390LikeOriginal(int mode, int pauseTicks)
        {
            _weaponCycleModeLikeOriginal = mode;
            _weaponCycleHasShotLikeOriginal = true;
            _weaponCycleStartedAtLikeOriginal = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
            _weaponCycleReadyAtLikeOriginal =
                _weaponCycleStartedAtLikeOriginal + Mathf.Max(0, pauseTicks) / 25.0f;
            Debug.Log("[C2:COMBAT FIRE V390] unit='" +
                      (_unit != null ? _unit.SourceMonsterId : string.Empty) +
                      "' mode=" + mode.ToString(CultureInfo.InvariantCulture) +
                      " pauseTicks=" + pauseTicks.ToString(CultureInfo.InvariantCulture) +
                      " slowRecharge=" + (_slowRecharge ? "1" : "0") +
                      " readyInSec=" +
                      Mathf.Max(0.0f, _weaponCycleReadyAtLikeOriginal - _weaponCycleStartedAtLikeOriginal)
                          .ToString("0.###", CultureInfo.InvariantCulture));
        }

        public void BeginAttackLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2NeutralPeasantUnitInfoV2LikeOriginal targetUnit,
            C2SettlementBuildingSelectableV1LikeOriginal targetBuilding,
            Vector3 fallbackTargetWorld)
        {
            _unit = unit != null ? unit :
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2FindForGameObjectV365LikeOriginal(gameObject);
            _targetUnit = targetUnit;
            _targetBuilding = targetBuilding;
            _fallbackTargetWorld = fallbackTargetWorld;
            _forcedOrderModeV395LikeOriginal = -1;
            _targetFormationGroupIdV395LikeOriginal = -1;
            if (_unit == null) { _active = false; return; }
            if (_targetUnit != null)
                C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                    _targetUnit, out _targetFormationGroupIdV395LikeOriginal);

            _md = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(_unit);
            InitializeUnitCombatStateFromMdV396LikeOriginal(_unit, _md);
            EnsureUnitLifeLikeOriginal(_unit, _md);
            if (_targetUnit != null)
                EnsureUnitLifeLikeOriginal(_targetUnit, C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(_targetUnit));

            string usage = _unit.UsageLikeOriginal ?? string.Empty;
            string id = (_unit.SourceMonsterId ?? string.Empty) + " " + (_unit.ResolvedMd ?? string.Empty);
            _artillery = usage.IndexOf("PUSH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         usage.IndexOf("MORT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         id.IndexOf("ArtPus", StringComparison.OrdinalIgnoreCase) >= 0;
            _slowRecharge = MdContainsCommandLikeOriginal(_md.Path, "SLOWRECHARGE");
            for (int mode = 0; mode < _weaponEffects.Length; mode++)
                _weaponEffects[mode] = ResolveWeaponEffectLikeOriginal(_md, mode);
            _complexCannon = _artillery
                ? LoadComplexCannonProfileLikeOriginal(_md.Path)
                : null;
            _active = _targetUnit != null || _targetBuilding != null;

            // V390: explicit retarget/re-issued attack orders must not cancel a
            // musket's already-running ATTACK_PAUSE/#ATTACK3 cycle.
            C2UnitOriginalRuntimeLinkLikeOriginal beginLink = _unit.RuntimeLinkCachedLikeOriginal;
            int queuedRemaining, queuedMaximum;
            bool queuedAnimating = false;
            bool queuedSlowRecharge = beginLink != null &&
                beginLink.TryGetSlowRechargeProgressLikeOriginal(
                    1, out queuedRemaining, out queuedMaximum, out queuedAnimating);
            bool preserveWeaponCycle =
                queuedSlowRecharge ||
                _slowRechargeInProgress ||
                _pendingSlowRechargeTicks > 0 ||
                C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal < _nextAttackAt;

            if (!preserveWeaponCycle)
            {
                _nextAttackAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
                _slowRechargeInProgress = false;
                _pendingSlowRechargeTicks = 0;
                _waitState5ExitForReloadV403DLikeOriginal = false;
            }
            else if (queuedSlowRecharge)
            {
                // The runtime owns the actual C2 delay now. Do not let a new
                // target order refill the weapon.
                _pendingSlowRechargeTicks = 0;
                _slowRechargeInProgress = queuedAnimating;
            }
            _attackInProgress = false;
            _impactApplied = false;
            _complexCannonStage = 0;
            _complexCannonStageEndsAt = 0.0f;
            _rifleButtonVolleyV399LikeOriginal = false;
            _rifleBrigadeOrderV405LikeOriginal = false;
            _finishCurrentRifleAttackThenStopV405LikeOriginal = false;
            _finishPreExistingAttackObjThenStopV405LikeOriginal = false;
            _allowLocalMeleeApproachV406LikeOriginal = true;
            enabled = _active;
        }

        public void BeginAttackForcedModeV395LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2NeutralPeasantUnitInfoV2LikeOriginal targetUnit,
            C2SettlementBuildingSelectableV1LikeOriginal targetBuilding,
            Vector3 fallbackTargetWorld,
            int forcedMode)
        {
            BeginAttackLikeOriginal(unit, targetUnit, targetBuilding, fallbackTargetWorld);
            if (_unit == null) return;
            _forcedOrderModeV395LikeOriginal = Mathf.Clamp(forcedMode, 0, 2);
            SetCommandWeaponModeLikeOriginal(_unit, _forcedOrderModeV395LikeOriginal);
            if (targetUnit != null)
                C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                    targetUnit, out _targetFormationGroupIdV395LikeOriginal);
            Debug.Log("[C2:COMBAT ORDER V395] unit='" + (_unit.SourceMonsterId ?? string.Empty) +
                      "' forcedMode=" + _forcedOrderModeV395LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " targetGroup=" + _targetFormationGroupIdV395LikeOriginal.ToString(CultureInfo.InvariantCulture));
        }


        internal void BeginMeleeAttackObjFromBrigadeBitvaV406LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2NeutralPeasantUnitInfoV2LikeOriginal targetUnit,
            bool allowLocalApproach)
        {
            BeginAttackForcedModeV395LikeOriginal(
                unit, targetUnit, null,
                targetUnit != null ? targetUnit.WorldPositionLikeOriginal : Vector3.zero, 0);
            _allowLocalMeleeApproachV406LikeOriginal = allowLocalApproach;
        }

        public void BeginRifleVolleyV399LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2NeutralPeasantUnitInfoV2LikeOriginal targetUnit,
            Vector3 fallbackTargetWorld)
        {
            BeginAttackForcedModeV395LikeOriginal(
                unit, targetUnit, null, fallbackTargetWorld, 1);
            _rifleButtonVolleyV399LikeOriginal = _unit != null && targetUnit != null;
        }

        // BrigadeOrders.cpp::BrigadeOrder_RifleAttack::SetTargetObj -> AttackObj(...,1).
        // V405 keeps the brigade order and the per-unit AttackObj as separate layers,
        // matching C2 instead of treating the rifle button itself as a one-shot order.
        internal void BeginRifleAttackObjFromBrigadeOrderV405LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2NeutralPeasantUnitInfoV2LikeOriginal targetUnit,
            Vector3 fallbackTargetWorld)
        {
            BeginAttackForcedModeV395LikeOriginal(
                unit, targetUnit, null, fallbackTargetWorld, 1);
            _rifleButtonVolleyV399LikeOriginal = false;
            _rifleBrigadeOrderV405LikeOriginal = _unit != null && targetUnit != null;
            _finishCurrentRifleAttackThenStopV405LikeOriginal = false;
            // BrigadeOrder_RifleAttack owns target replacement. Do not let the generic
            // V395 formation retargeter silently pick another member after EnemyID dies.
            _targetFormationGroupIdV395LikeOriginal = -1;
        }

        internal bool HasLiveRifleAttackObjV405LikeOriginal
        {
            get
            {
                return _rifleBrigadeOrderV405LikeOriginal &&
                       (_active || _attackInProgress) && TargetAliveLikeOriginal();
            }
        }

        internal bool HasAnyRifleAttackObjV405LikeOriginal
        {
            get
            {
                return _rifleBrigadeOrderV405LikeOriginal &&
                       (_active || _attackInProgress || _slowRechargeInProgress);
            }
        }

        internal bool HasAnyAttackObjForBrigadeRifleOrderV405LikeOriginal
        {
            get { return _active || _attackInProgress; }
        }

        // Multi.cpp::SendSelectedToXY counts selected brigade members already
        // attacking an enemy nearer than 150 pixels. If more than 10% are in
        // that close fight, a move order is allowed to break attack-state march.
        internal static bool HasActiveEnemyWithinV405BLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, int radiusOriginalPixels)
        {
            if (unit == null || radiusOriginalPixels <= 0) return false;
            C2CombatRuntimeV334LikeOriginal combat = unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat == null || !combat.enabled ||
                (!combat._active && !combat._attackInProgress) || combat._targetUnit == null ||
                combat._targetUnit.IsDeadLikeOriginal || !combat._targetUnit.isActiveAndEnabled)
                return false;
            int dx = Mathf.RoundToInt(CurrentRealX(combat._targetUnit) - CurrentRealX(unit));
            int dy = Mathf.RoundToInt(CurrentRealY(combat._targetUnit) - CurrentRealY(unit));
            return C2OriginalMovementMathV352.Norma(dx, dy) < radiusOriginalPixels * 16;
        }

        // V406 BrigadeOrder_Bitva needs the managed equivalent of OneObject::EnemyID.
        // It must inspect, not replace, a live melee AttackObj so target ownership
        // stays sticky exactly as in COSSACKS2.
        internal void SetMeleeLocalApproachV406LikeOriginal(bool allow)
        {
            if (_forcedOrderModeV395LikeOriginal == 0)
                _allowLocalMeleeApproachV406LikeOriginal = allow;
        }

        internal bool TryGetLiveMeleeTargetV406LikeOriginal(
            out C2NeutralPeasantUnitInfoV2LikeOriginal target)
        {
            target = null;
            if (_forcedOrderModeV395LikeOriginal != 0 ||
                (!_active && !_attackInProgress) ||
                _targetUnit == null || _targetUnit.IsDeadLikeOriginal ||
                !_targetUnit.isActiveAndEnabled)
                return false;
            target = _targetUnit;
            return true;
        }

        // BrigadeOrders.cpp::ClearTargetObj is not limited to rifle-created AttackObj.
        // It examines any OB->Attack order already on the member before the brigade
        // rifle order starts. Preserve a currently executing ATTACK/PATTACK once;
        // otherwise DeleteLastOrder immediately.
        internal void ClearPreExistingAttackObjForBrigadeRifleOrderV405LikeOriginal(
            bool allowCurrentAttackFrameToFinish,
            string reason)
        {
            if (!_active && !_attackInProgress) return;
            if (allowCurrentAttackFrameToFinish && _attackInProgress)
            {
                _finishPreExistingAttackObjThenStopV405LikeOriginal = true;
                return;
            }
            CancelForExternalOrderLikeOriginal(reason ?? "ClearTargetObj_preexisting_attack_v405");
        }

        // BrigadeOrder_RifleAttack::ClearTargetObj. If ATTACK/PATTACK is already
        // executing, retail sets SN=1 and lets that single animation complete;
        // otherwise the local AttackObj is deleted immediately.
        internal void ClearRifleAttackObjFromBrigadeOrderV405LikeOriginal(
            bool allowCurrentAttackFrameToFinish,
            string reason)
        {
            if (!_rifleBrigadeOrderV405LikeOriginal && !_rifleButtonVolleyV399LikeOriginal)
                return;

            _rifleButtonVolleyV399LikeOriginal = false;
            if (allowCurrentAttackFrameToFinish && _attackInProgress)
            {
                _finishCurrentRifleAttackThenStopV405LikeOriginal = true;
                _rifleBrigadeOrderV405LikeOriginal = true;
                return;
            }

            _rifleBrigadeOrderV405LikeOriginal = false;
            _finishCurrentRifleAttackThenStopV405LikeOriginal = false;
            _active = false;
            _attackInProgress = false;
            _impactApplied = false;
            _targetUnit = null;
            _targetBuilding = null;
            _forcedOrderModeV395LikeOriginal = -1;
            _targetFormationGroupIdV395LikeOriginal = -1;
            C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
            // delay/#ATTACK3 is OneObject state and intentionally survives.
            _slowRechargeInProgress = false;
            _pendingSlowRechargeTicks = 0;
            _waitState5ExitForReloadV403DLikeOriginal = false;
            enabled = false;
            Debug.Log("[C2:RIFLE V405 CLEAR ATTACKOBJ] unit='" +
                      (_unit != null ? _unit.SourceMonsterId : string.Empty) +
                      "' finishCurrent=" + allowCurrentAttackFrameToFinish.ToString() +
                      " reason='" + (reason ?? string.Empty) + "'");
        }

        // Used only by BrigadeOrder_RifleAttack's destructor/explicit state clear.
        // This is deliberately separate from ClearFormationRifleAttackStateV399 so
        // the V405 order does not recursively cancel itself.
        internal static void ClearFormationRifleFlagsFromBrigadeOrderV405LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            string reason)
        {
            if (representative == null) return;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers;
            int groupIdV405;
            string shapeV405;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    representative, out soldiers, out groupIdV405, out shapeV405) ||
                soldiers == null || soldiers.Count == 0)
            {
                soldiers = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                soldiers.Add(representative);
            }
            for (int i = 0; i < soldiers.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal soldier = soldiers[i];
                if (soldier == null) continue;
                EnsureUnitCombatStateV396LikeOriginal(soldier);
                soldier.RifleAttackV396LikeOriginal = false;
                int id = soldier.GetInstanceID();
                int commanded;
                if (CommandModeByUnit.TryGetValue(id, out commanded) && commanded == 1)
                    CommandModeByUnit.Remove(id);

                C2CombatRuntimeV334LikeOriginal combat =
                    soldier.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                if (combat != null)
                    combat.ClearRifleAttackObjFromBrigadeOrderV405LikeOriginal(
                        true, reason ?? "BrigadeOrder_RifleAttack_destructor_v405");
            }
        }

        internal static void EnsureUnitLifeV405LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            EnsureUnitLifeLikeOriginal(
                unit, C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit));
        }


        public bool IsActiveOrderV350LikeOriginal
        {
            get { return _active || _attackInProgress || _complexCannonStage != 0; }
        }

        public void CancelForExternalOrderLikeOriginal(string reason)
        {
            // MoveBrigadeForwardToAttack submits a formation movement and then keeps
            // the SAME melee order alive. The asynchronous movement node used to
            // come back one frame later and cancel the just-created forcedMode=0
            // combat order. That is why the brigade charged but never contacted.
            if (_forcedOrderModeV395LikeOriginal == 0 &&
                !string.IsNullOrEmpty(reason) &&
                reason.IndexOf("MoveBrigadeForwardToAttack", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            if (!_active && !_attackInProgress && _complexCannonStage == 0)
                return;
            _active = false;
            _attackInProgress = false;
            _impactApplied = false;
            // Do not clear Runtime.RechargeTicksRemainingLikeOriginal here.
            // NewMon.cpp keeps delay/MaxDelay when movement or another order
            // interrupts the visible #ATTACK3 cycle; reload resumes later.
            _slowRechargeInProgress = false;
            _pendingSlowRechargeTicks = 0;
            _waitState5ExitForReloadV403DLikeOriginal = false;
            _complexCannonStage = 0;
            _targetUnit = null;
            _targetBuilding = null;
            _forcedOrderModeV395LikeOriginal = -1;
            _targetFormationGroupIdV395LikeOriginal = -1;
            _rifleButtonVolleyV399LikeOriginal = false;
            _rifleBrigadeOrderV405LikeOriginal = false;
            _finishCurrentRifleAttackThenStopV405LikeOriginal = false;
            _finishPreExistingAttackObjThenStopV405LikeOriginal = false;
            C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
            enabled = false;
            Debug.Log("[C2:COMBAT ORDER CANCEL] unit='" +
                      (_unit != null ? _unit.SourceMonsterId : string.Empty) +
                      "' reason='" + (reason ?? string.Empty) + "'");
        }

        private void Update()
        {
            if (_unit == null || _unit.IsDeadLikeOriginal)
            {
                if (_unit != null) C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
                _active = false;
                _attackInProgress = false;
                enabled = false;
                return;
            }

            // A killing active frame may set _active=false immediately, but Cossacks II
            // still finishes ATTACKx and then enters #ATTACK3.  Never throw away the
            // source recovery/reload transition merely because the victim died.
            if (_attackInProgress)
            {
                TickAttackActiveFrameLikeOriginal();
                return;
            }

            // NewMon.cpp slow-recharge branch: a SitInFormations unit firing from
            // NewState==5 first clears NewState to neutral/TryToStand, then enters
            // ATTACK3 on a following simulation pass. Preserve that visible
            // UATTACK4 -> neutral -> ATTACK3 sequence instead of overwriting it in
            // the same frame.
            if (_waitState5ExitForReloadV403DLikeOriginal)
            {
                C2UnitOriginalRuntimeLinkLikeOriginal state5Link =
                    _unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime state5Runtime =
                    state5Link != null ? state5Link.Runtime : null;
                if (state5Runtime != null &&
                    state5Runtime.State == C2UnitOriginalState.Transition)
                    return;

                int rechargeTicksV403D = _pendingSlowRechargeTicks;
                _pendingSlowRechargeTicks = 0;
                _waitState5ExitForReloadV403DLikeOriginal = false;
                if (_slowRecharge && rechargeTicksV403D > 0 &&
                    !C2CombatCoreV408LikeOriginal.TryConsumeShotResourcesV408LikeOriginal(_unit, 1))
                {
                    if (state5Link != null) state5Link.ClearSlowRechargeDelayLikeOriginal();
                    C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
                    _active = false;
                    enabled = false;
                    return;
                }
                if (state5Link != null && rechargeTicksV403D > 0 &&
                    state5Link.BeginSlowRechargeLikeOriginal(rechargeTicksV403D))
                {
                    _slowRechargeInProgress = true;
                    _weaponCycleReadyAtLikeOriginal =
                        C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + rechargeTicksV403D / 25.0f;
                    return;
                }
            }

            // Keep staged artillery fire/reload independent from target lifetime after
            // the firing stage has begun, for the same reason.
            if (_complexCannonStage != 0)
            {
                if (C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal < _complexCannonStageEndsAt)
                    return;
                if (_complexCannonStage == 1)
                {
                    WeaponEffectLikeOriginal stagedEffect = EffectForModeLikeOriginal(_attackMode);
                    if (!FireWeaponAtTargetLikeOriginal(
                            _attackMode, stagedEffect, _impactRealX, _impactRealY))
                    {
                        _complexCannonStage = 0;
                        C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
                        _active = false;
                        enabled = false;
                        return;
                    }
                    int reloadTicks = _complexCannon != null
                        ? Mathf.Max(0, _complexCannon.ReloadTicks[Mathf.Clamp(_attackMode, 0, 2)])
                        : 0;
                    if (reloadTicks > 0)
                    {
                        _complexCannonStage = 2;
                        _complexCannonStageEndsAt =
                            C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + reloadTicks / 25.0f;
                    }
                    else
                    {
                        _complexCannonStage = 0;
                        _nextAttackAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
                    }
                }
                else
                {
                    _complexCannonStage = 0;
                    _nextAttackAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
                }
                return;
            }

            // V406 / COSSACKS2 BrigadeOrder_Bitva: EnemyID is sticky.  Once a
            // soldier has a live victim, retail keeps it while CheckAttDist from the
            // brigade centre remains valid; it does NOT choose the nearest enemy on
            // every frame.  The old V399 block below did exactly that and made two
            // touching formations jitter/cross while soldiers swapped victims.
            // Target replacement is now owned by C2BrigadeBattleV406LikeOriginal.
            if (_active && _forcedOrderModeV395LikeOriginal != 0 && _targetUnit != null &&
                (_targetUnit.IsDeadLikeOriginal || !_targetUnit.isActiveAndEnabled) &&
                _targetFormationGroupIdV395LikeOriginal >= 0)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal replacementTarget;
                if (C2FormationRuntimeV167LikeOriginal.TryGetAliveFormationMemberByGroupIdV395LikeOriginal(
                        _targetFormationGroupIdV395LikeOriginal,
                        _unit != null ? _unit.CombatNationLikeOriginal : -1,
                        out replacementTarget) && replacementTarget != null)
                {
                    _targetUnit = replacementTarget;
                    _fallbackTargetWorld = replacementTarget.WorldPositionLikeOriginal;
                }
            }

            if (!_active || !TargetAliveLikeOriginal())
            {
                C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
                _active = false;
                enabled = false;
                return;
            }
            if (_targetUnit != null && _targetUnit.CombatNationLikeOriginal == _unit.CombatNationLikeOriginal ||
                _targetBuilding != null && _targetBuilding.Nation == _unit.CombatNationLikeOriginal)
            {
                _active = false;
                enabled = false;
                return;
            }

            // Do not let StopMoveAndFaceDirection issue a Stand animation over
            // #ATTACK3 while the original SLOWRECHARGE cycle is running.
            if (_slowRechargeInProgress)
            {
                C2UnitOriginalRuntimeLinkLikeOriginal rechargeLink =
                    _unit.RuntimeLinkCachedLikeOriginal;
                if (rechargeLink != null && rechargeLink.IsSlowRechargingLikeOriginal())
                    return;
                _slowRechargeInProgress = false;
                _nextAttackAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
                _weaponCycleReadyAtLikeOriginal = _nextAttackAt;
            }


            float tx, ty;
            GetTargetRealLikeOriginal(out tx, out ty);
            float ux = CurrentRealX(_unit);
            float uy = CurrentRealY(_unit);
            float dx = tx - ux;
            float dy = ty - uy;
            float distOriginalPixels = Mathf.Sqrt(dx * dx + dy * dy) / 16.0f;

            C2UnitOriginalRuntimeLinkLikeOriginal link =
                _unit.RuntimeLinkCachedLikeOriginal;

            int mode = SelectAttackModeLikeOriginal(distOriginalPixels, false);
            if (mode < 0)
            {
                int commandedMode;
                bool commandedMelee =
                    _forcedOrderModeV395LikeOriginal == 0 ||
                    (CommandModeByUnit.TryGetValue(_unit.GetInstanceID(), out commandedMode) &&
                     commandedMode == 0);
                // AttackSelected/MoveBrigadeForwardToAttack moves the BRIGADE.
                // Retail does not replace that with 120 private chase paths.  The
                // destination is deliberately shifted through the enemy line and
                // contact/AttackObj stops each soldier as an enemy enters ATTACK0
                // range.  Therefore a formation melee order must never fall back to
                // the generic per-unit approach code, even after one member has
                // already consumed his local move target.
                if (commandedMelee && _targetFormationGroupIdV395LikeOriginal >= 0 &&
                    !_allowLocalMeleeApproachV406LikeOriginal)
                    return;

                // NewMon.cpp::AttackObjLink uses SetDestUnit at the exact MaxR
                // when too far, and backs to (3*MaxR+MinR)/4 when too close.
                int approachMode = ResolveApproachModeLikeOriginal();
                if (approachMode < 0) return;
                float minR = AttackMinForModeLikeOriginal(approachMode);
                float maxR = AttackMaxForModeLikeOriginal(approachMode);
                if (maxR <= 0.0f) return;
                float wantedPixels = distOriginalPixels < minR
                    ? (maxR * 3.0f + minR) * 0.25f
                    : maxR;
                float dReal = Mathf.Max(1.0f, distOriginalPixels * 16.0f);
                float wantedReal = wantedPixels * 16.0f;
                float ax = tx - dx / dReal * wantedReal;
                float ay = ty - dy / dReal * wantedReal;
                _unit.SetMoveDestinationRealLikeOriginal(
                    ax, ay,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false, 0);
                return;
            }

            byte facing = DirectionFromDeltaLikeOriginal(dx, dy);
            _unit.StopMoveAndFaceDirectionLikeOriginal(facing);

            if (mode == 1 && link != null)
            {
                int reloadRemaining, reloadMaximum;
                bool reloadPlaying;
                if (link.TryGetSlowRechargeProgressLikeOriginal(
                        1, out reloadRemaining, out reloadMaximum, out reloadPlaying))
                {
                    // If movement/retargeting interrupted #ATTACK3, resume the
                    // original slow-recharge state before another shot is legal.
                    if (!reloadPlaying && reloadRemaining > 0)
                    {
                        if (link.BeginSlowRechargeLikeOriginal(reloadRemaining))
                            _slowRechargeInProgress = true;
                        else
                        {
                            link.ClearSlowRechargeDelayLikeOriginal();
                            _nextAttackAt = Mathf.Max(
                                C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal,
                                C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + reloadRemaining / 25.0f);
                        }
                    }
                    return;
                }
            }

            if (C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal < _nextAttackAt)
                return;

            _unit.SetCombatPostureV322LikeOriginal(mode, true);
            if (link != null && !link.IsCombatPostureReadyV335LikeOriginal(mode))
                return; // PATTACK/TRANSxy must finish before ATTACKx.

            if (mode == 2 && !TryConsumeGrenadeLikeOriginal())
            {
                GrenadeArmedUnits.Remove(_unit.GetInstanceID());
                return;
            }
            if (_artillery && _complexCannon != null)
            {
                int fireTicks = Mathf.Max(
                    1, _complexCannon.FireTicks[Mathf.Clamp(mode, 0, 2)]);
                _attackMode = mode;
                _impactRealX = tx;
                _impactRealY = ty;
                _complexCannonStage = 1;
                _complexCannonStageEndsAt =
                    C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + fireTicks / 25.0f;
                return;
            }
            // MD attack slots are zero based: slot 0 is #ATTACK (without a
            // suffix), slot 1 is #ATTACK1, grenade slot 2 is #ATTACK2.
            int animationModeV403D = mode;
            // NewMon.cpp: Attack5 = SitInFormations && NewState==5;
            // if(NeedState==1 && Attack5) NeedState=4.  Keep the weapon/effect
            // mode as rifle (1), but play ATTACK4 for the @ front-row soldiers
            // that reached full StandGround.
            if (mode == 1 &&
                C2FormationRuntimeV167LikeOriginal.IsUnitSpecialFormationState5V403DLikeOriginal(_unit))
                animationModeV403D = 4;
            if (!_unit.PlayAttackOneShotV325LikeOriginal(animationModeV403D)) return;
            _attackMode = mode;
            _impactRealX = tx;
            _impactRealY = ty;
            _impactApplied = false;
            _attackInProgress = true;
            if (mode == 2) GrenadeArmedUnits.Remove(_unit.GetInstanceID());
        }

        private void TickAttackActiveFrameLikeOriginal()
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link =
                _unit != null ? _unit.RuntimeLinkCachedLikeOriginal : null;
            int frame, count, activeFrame;
            bool attacking;
            if (link == null ||
                !link.TryGetAttackTimingV335LikeOriginal(
                    out frame, out count, out activeFrame, out attacking))
            {
                _attackInProgress = false;
                return;
            }
            if (!_impactApplied && attacking && frame >= activeFrame)
            {
                WeaponEffectLikeOriginal effect = EffectForModeLikeOriginal(_attackMode);
                if (!FireWeaponAtTargetLikeOriginal(
                        _attackMode, effect, _impactRealX, _impactRealY))
                {
                    _impactApplied = true;
                    _attackInProgress = false;
                    C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
                    _active = false;
                    enabled = false;
                    return;
                }
                _impactApplied = true;
                int pauseTicks = Mathf.Max(0, PauseForModeLikeOriginal(_attackMode));
                BeginWeaponCycleV390LikeOriginal(_attackMode, pauseTicks);
                if (_attackMode == 1 && _rifleButtonVolleyV399LikeOriginal &&
                    !_rifleBrigadeOrderV405LikeOriginal)
                {
                    // BrigadeOrder_RifleAttack::Process/Destructor clears the red
                    // RifleAttack state once the volley has committed to delay.
                    // Forced mode on the sibling combat orders still lets the rest
                    // of the already-issued volley finish.
                    ClearFormationRifleAttackStateV399LikeOriginal(
                        _unit, "BrigadeOrder_RifleAttack_volley_fired_v399", false);
                }
                // NewMon.cpp enters the SLOWRECHARGE/#ATTACK3 branch only for
                // firearm weapon kinds. A bayonet/melee ATTACK0 on the same unit
                // must never reload the musket.
                bool slowRechargeThisShot = _slowRecharge && _attackMode == 1;
                if (slowRechargeThisShot && pauseTicks > 0)
                {
                    // Queue delay/MaxDelay immediately on the shot frame. The HUD
                    // must go to 0-ready now, while #ATTACK1 recovery is still playing.
                    if (link != null)
                        link.QueueSlowRechargeLikeOriginal(_attackMode, pauseTicks);
                    _pendingSlowRechargeTicks = pauseTicks;
                    _nextAttackAt = float.PositiveInfinity;
                }
                else
                {
                    // Normal delay is decremented once per 40 ms simulation
                    // quantum at GameSpeed=256: 25 MD ticks per real second.
                    _nextAttackAt = C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + pauseTicks / 25.0f;
                }
            }
            // Applying damage is the active-point event, not the end of ATTACKx.
            // C2 keeps playing the recovery frames and only returns to PSTAND when
            // the complete one-shot animation reports that it has finished.
            if (!attacking)
            {
                _attackInProgress = false;
                if (_pendingSlowRechargeTicks > 0)
                {
                    int rechargeTicks = _pendingSlowRechargeTicks;
                    if (_attackMode == 1 &&
                        C2FormationRuntimeV167LikeOriginal.LeaveUnitSpecialFormationState5ForReloadV403DLikeOriginal(_unit))
                    {
                        _waitState5ExitForReloadV403DLikeOriginal = true;
                        return;
                    }
                    _pendingSlowRechargeTicks = 0;
                    if (_slowRecharge &&
                        !C2CombatCoreV408LikeOriginal.TryConsumeShotResourcesV408LikeOriginal(_unit, 1))
                    {
                        if (link != null) link.ClearSlowRechargeDelayLikeOriginal();
                        C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(_unit);
                        _active = false;
                        enabled = false;
                        return;
                    }
                    if (link.BeginSlowRechargeLikeOriginal(rechargeTicks))
                    {
                        _slowRechargeInProgress = true;
                        // This runtime starts #ATTACK3 after the firing animation has
                        // completed, so extend the HUD cycle to the actual reload end.
                        _weaponCycleReadyAtLikeOriginal =
                            C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + rechargeTicks / 25.0f;
                        Debug.Log("[C2:COMBAT RELOAD V390] unit='" +
                                  (_unit != null ? _unit.SourceMonsterId : string.Empty) +
                                  "' mode=" + _attackMode.ToString(CultureInfo.InvariantCulture) +
                                  " animation=#ATTACK3 ticks=" +
                                  rechargeTicks.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        // A malformed SLOWRECHARGE MD without #ATTACK3 cannot
                        // play the source animation. Clear the queued runtime flag
                        // and retain ATTACK_PAUSE as a normal 25 Hz delay gate.
                        if (link != null) link.ClearSlowRechargeDelayLikeOriginal();
                        _nextAttackAt =
                            C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal + rechargeTicks / 25.0f;
                        _weaponCycleReadyAtLikeOriginal = _nextAttackAt;
                    }
                }
                if (_finishPreExistingAttackObjThenStopV405LikeOriginal)
                {
                    _finishPreExistingAttackObjThenStopV405LikeOriginal = false;
                    _active = false;
                    _rifleButtonVolleyV399LikeOriginal = false;
                    _rifleBrigadeOrderV405LikeOriginal = false;
                    _forcedOrderModeV395LikeOriginal = -1;
                    _targetFormationGroupIdV395LikeOriginal = -1;
                    _targetUnit = null;
                    _targetBuilding = null;
                    // Any delay/#ATTACK3 created by the finishing shot remains on
                    // OneObject runtime exactly as it does after DeleteLastOrder.
                    _slowRechargeInProgress = false;
                    enabled = false;
                    return;
                }

                if (_finishCurrentRifleAttackThenStopV405LikeOriginal && _attackMode == 1)
                {
                    _finishCurrentRifleAttackThenStopV405LikeOriginal = false;
                    _rifleBrigadeOrderV405LikeOriginal = false;
                    _active = false;
                    _forcedOrderModeV395LikeOriginal = -1;
                    _targetFormationGroupIdV395LikeOriginal = -1;
                    _targetUnit = null;
                    _targetBuilding = null;
                    // SLOWRECHARGE was queued/started above and is runtime-owned.
                    // Stopping AttackObj here must not reset delay/MaxDelay.
                    _slowRechargeInProgress = false;
                    enabled = false;
                    return;
                }

                if (_rifleButtonVolleyV399LikeOriginal && _attackMode == 1)
                {
                    _rifleButtonVolleyV399LikeOriginal = false;
                    _active = false;
                    _forcedOrderModeV395LikeOriginal = -1;
                    _targetFormationGroupIdV395LikeOriginal = -1;
                    _targetUnit = null;
                    _targetBuilding = null;
                    // The runtime link owns delay/#ATTACK3 from this point.
                    // Disabling this targeting component must not refill the weapon.
                    enabled = false;
                    return;
                }

                // ComThrowGrenade in Cossacks II is a one-shot command.  Once
                // ATTACK2 has played through its recovery frames, do not leave
                // the unit in a permanent combat/approach loop against the old
                // target.  A subsequent grenade throw is a new explicit order.
                if (_attackMode == 2)
                {
                    _active = false;
                    enabled = false;
                }
            }
        }

        private bool TryGetNearestAliveFormationMemberV399LikeOriginal(
            int groupId,
            out C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            member = null;
            if (_unit == null || groupId < 0) return false;

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            if (all == null || all.Length == 0) return false;

            float ux = CurrentRealX(_unit);
            float uy = CurrentRealY(_unit);
            float best2 = float.MaxValue;
            for (int i = 0; i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = all[i];
                if (candidate == null || candidate == _unit || candidate.IsDeadLikeOriginal ||
                    !candidate.isActiveAndEnabled ||
                    candidate.CombatNationLikeOriginal == _unit.CombatNationLikeOriginal)
                    continue;

                int candidateGroup;
                if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                        candidate, out candidateGroup) || candidateGroup != groupId)
                    continue;

                float cx = CurrentRealX(candidate);
                float cy = CurrentRealY(candidate);
                float dx = cx - ux;
                float dy = cy - uy;
                float d2 = dx * dx + dy * dy;
                if (d2 >= best2) continue;
                best2 = d2;
                member = candidate;
            }
            return member != null;
        }

        private bool TryConsumeGrenadeLikeOriginal()
        {
            int current, maximum;
            C2FormationRuntimeV167LikeOriginal.TryGetGrenadeStateV326LikeOriginal(
                _unit, _md.MaxGrenadesInFormation, _md.GrenadeRechargeTime, out current, out maximum);
            return C2FormationRuntimeV167LikeOriginal.ConsumeGrenadesV326LikeOriginal(_unit, 1) > 0;
        }

        private int SelectAttackModeLikeOriginal(float distance, bool skipGrenade)
        {
            if (_forcedOrderModeV395LikeOriginal >= 0)
            {
                int forced = _forcedOrderModeV395LikeOriginal;
                if (forced == 0)
                    return HasAttackModeLikeOriginal(0) && InRange(distance, _md.AttackRadius0Min, _md.AttackRadius0) ? 0 : -1;
                if (forced == 1)
                    return HasAttackModeLikeOriginal(1) && InRange(distance, _md.AttackRadius1Min, _md.AttackRadius1) ? 1 : -1;
                if (forced == 2)
                    return !skipGrenade && HasAttackModeLikeOriginal(2) && InRange(distance, _md.AttackRadius2Min, _md.AttackRadius2) ? 2 : -1;
            }

            // C2 BrigadeAI has separate Fire/MeleeAttack/ThrowGrenade commands.
            // Grenade is never an automatic "best range" choice.
            bool grenadeArmed = !skipGrenade && _unit != null &&
                                GrenadeArmedUnits.Contains(_unit.GetInstanceID());
            if (grenadeArmed)
            {
                if (HasAttackModeLikeOriginal(2) && InRange(distance, _md.AttackRadius2Min, _md.AttackRadius2)) return 2;
                return -1; // ComThrowGrenade is a separate order; never silently turn it into melee/fire.
            }
            int commandedMode;
            if (_unit != null && CommandModeByUnit.TryGetValue(_unit.GetInstanceID(), out commandedMode))
            {
                if (commandedMode == 0)
                    return HasAttackModeLikeOriginal(0) && InRange(distance, _md.AttackRadius0Min, _md.AttackRadius0) ? 0 : -1;
                if (commandedMode == 1)
                    return HasAttackModeLikeOriginal(1) && InRange(distance, _md.AttackRadius1Min, _md.AttackRadius1) ? 1 : -1;
                return -1;
            }

            // NewMon.cpp + AttackSelected: ARMATTACK infantry starts in arm mode.
            // RifleAttack is a persistent OneObject flag set by command 129; range
            // never promotes an arm-order to a rifle-order by itself.
            if (_unit != null && _unit.ArmAttackCapableV396LikeOriginal)
            {
                if (_unit.RifleAttackV396LikeOriginal)
                    return HasAttackModeLikeOriginal(1) && InRange(distance, _md.AttackRadius1Min, _md.AttackRadius1) ? 1 : -1;
                if (_unit.ArmAttackV396LikeOriginal)
                    return HasAttackModeLikeOriginal(0) && InRange(distance, _md.AttackRadius0Min, _md.AttackRadius0) ? 0 : -1;
                return -1;
            }

            // Non-ARMATTACK objects keep the generic/artillery slot behaviour.
            if (HasAttackModeLikeOriginal(1) && InRange(distance, _md.AttackRadius1Min, _md.AttackRadius1)) return 1;
            if (HasAttackModeLikeOriginal(0) && InRange(distance, _md.AttackRadius0Min, _md.AttackRadius0)) return 0;
            if (_artillery && HasAttackModeLikeOriginal(0) && distance <= Mathf.Max(1, _md.AttackRadius0)) return 0;
            return -1;
        }

        private static bool InRange(float d, int min, int max)
        {
            return max > 0 && d >= Mathf.Max(0, min) && d <= max;
        }

        private float MaxAttackRadiusLikeOriginal()
        {
            return Mathf.Max(80, Mathf.Max(_md.AttackRadius0, Mathf.Max(_md.AttackRadius1, _md.AttackRadius2)));
        }

        private int ResolveApproachModeLikeOriginal()
        {
            if (_forcedOrderModeV395LikeOriginal >= 0) return _forcedOrderModeV395LikeOriginal;
            if (_unit != null && GrenadeArmedUnits.Contains(_unit.GetInstanceID())) return 2;
            int commandedMode;
            if (_unit != null && CommandModeByUnit.TryGetValue(_unit.GetInstanceID(), out commandedMode))
                return commandedMode;
            if (_unit != null && _unit.ArmAttackCapableV396LikeOriginal)
                return _unit.RifleAttackV396LikeOriginal ? 1 : 0;
            if (HasAttackModeLikeOriginal(1)) return 1;
            if (HasAttackModeLikeOriginal(0)) return 0;
            return -1;
        }

        private float AttackMinForModeLikeOriginal(int mode)
        {
            return mode == 2 ? Mathf.Max(0, _md.AttackRadius2Min) :
                (mode == 1 ? Mathf.Max(0, _md.AttackRadius1Min) : Mathf.Max(0, _md.AttackRadius0Min));
        }

        private float AttackMaxForModeLikeOriginal(int mode)
        {
            return mode == 2 ? Mathf.Max(0, _md.AttackRadius2) :
                (mode == 1 ? Mathf.Max(0, _md.AttackRadius1) : Mathf.Max(0, _md.AttackRadius0));
        }

        private bool FireWeaponAtTargetLikeOriginal(
            int mode,
            WeaponEffectLikeOriginal effect,
            float targetRealX,
            float targetRealY)
        {
            if (_unit == null) return false;
            if (effect == null) effect = EffectForModeLikeOriginal(mode);

            // NewMon.cpp consumes RASTRATA_NA_VISTREL immediately for ordinary
            // weapons. SLOWRECHARGE pays when #ATTACK3/recharge actually begins.
            if (!(_slowRecharge && mode == 1) &&
                !C2CombatCoreV408LikeOriginal.TryConsumeShotResourcesV408LikeOriginal(_unit, mode))
                return false;

            if (effect.HasWeapon)
                C2CombatCoreV408LikeOriginal.ApplyRazbrosV408LikeOriginal(
                    _unit, ref targetRealX, ref targetRealY);

            int damage = Mathf.Max(1, effect.Damage);
            int radius = Mathf.Max(0, effect.Radius);
            Vector3 start = _unit.transform.position + Vector3.up * 0.55f;
            C2UnitOriginalRuntimeLinkLikeOriginal runtimeLink =
                _unit.RuntimeLinkCachedLikeOriginal;
            Vector3 activePointStart;
            if (runtimeLink != null &&
                runtimeLink.TryGetWeaponStartWorldLikeOriginal(0, out activePointStart))
                start = activePointStart;
            Vector3 end;
            C2BattleTerrainMode modeOwner = _unit.OwnerMode != null
                ? _unit.OwnerMode : FindObjectOfType<C2BattleTerrainMode>();
            if (modeOwner != null)
                end = modeOwner.SettlementMapPointToWorldV336LikeOriginal(targetRealX / 16.0f, targetRealY / 16.0f) + Vector3.up * 0.25f;
            else if (_targetUnit != null) end = _targetUnit.transform.position + Vector3.up * 0.35f;
            else if (_targetBuilding != null) end = _targetBuilding.transform.position + Vector3.up * 0.5f;
            else end = start + _unit.transform.forward;

            int smokeSeed = unchecked(_unit.GetInstanceID().GetHashCode() * 397) ^ (++_shotSerialLikeOriginal * 7919);
            string smokeAnimation = effect != null
                ? ResolveWeaponVisualAnimationLikeOriginal(
                    _md.Path ?? string.Empty, effect.RootName, smokeSeed)
                : string.Empty;
            bool shotFogEffect = !string.IsNullOrEmpty(smokeAnimation) ||
                                 (effect != null &&
                                  !string.IsNullOrEmpty(effect.RootName) &&
                                  effect.RootName.IndexOf("SHOTFOG", StringComparison.OrdinalIgnoreCase) >= 0);
            if (shotFogEffect)
                C2CombatMuzzleSmokeV390LikeOriginal.SpawnLikeOriginal(
                    start, end, _md.Path ?? string.Empty,
                    smokeAnimation, smokeSeed);

            Action<C2SettlementBuildingSelectableV1LikeOriginal, float, float> impact =
                (interceptedBuilding, impactRealX, impactRealY) => ApplyDamageToTargetLikeOriginal(
                    damage, radius, impactRealX, impactRealY, interceptedBuilding);
            if (!effect.HasWeapon)
            {
                impact(null, targetRealX, targetRealY);
                return true;
            }

            float travelSeconds = ProjectileTravelSecondsLikeOriginal(
                effect, targetRealX, targetRealY);
            if (travelSeconds <= 0.0f)
            {
                impact(null, targetRealX, targetRealY);
                impact = null;
            }
            C2CombatProjectileVisualV336LikeOriginal.SpawnLikeOriginal(
                start, end, mode == 2, _artillery, _unit.Nation,
                travelSeconds, modeOwner, targetRealX, targetRealY, impact,
                !shotFogEffect);
            return true;
        }

        private float ProjectileTravelSecondsLikeOriginal(
            WeaponEffectLikeOriginal effect, float targetRealX, float targetRealY)
        {
            if (effect == null || effect.Speed <= 0) return 0.0f;
            if (effect.Propagation != 3 && effect.Propagation != 8)
                return effect.Propagation == 5 ? 0.55f : 0.0f;
            float dx = (targetRealX - CurrentRealX(_unit)) / 16.0f;
            float dy = (targetRealY - CurrentRealY(_unit)) / 16.0f;
            int distance = Mathf.FloorToInt(Mathf.Sqrt(dx * dx + dy * dy));
            // Cossacks II sets SpeedSh=1. Weapon.cpp uses integer
            // time=dist/(Speed<<SpeedSh), then velocity over time+1 ticks.
            int speedPerTick = effect.Speed << 1;
            int ticks = speedPerTick > 0 ? distance / speedPerTick : 0;
            return ticks > 0 ? (ticks + 1) / 25.0f : 0.0f;
        }

        private int DamageForModeLikeOriginal(int mode)
        {
            return mode == 2 ? _md.Damage2 : (mode == 1 ? _md.Damage1 : _md.Damage0);
        }

        private bool HasAttackModeLikeOriginal(int mode)
        {
            WeaponEffectLikeOriginal effect = EffectForModeLikeOriginal(mode);
            return effect.HasWeapon || effect.Damage > 0;
        }

        private WeaponEffectLikeOriginal EffectForModeLikeOriginal(int mode)
        {
            int slot = Mathf.Clamp(mode, 0, _weaponEffects.Length - 1);
            WeaponEffectLikeOriginal effect = _weaponEffects[slot];
            if (effect != null) return effect;
            return new WeaponEffectLikeOriginal
            {
                Damage = Mathf.Max(0, DamageForModeLikeOriginal(slot))
            };
        }

        private int PauseForModeLikeOriginal(int mode)
        {
            return mode == 2 ? _md.AttackPause2 : (mode == 1 ? _md.AttackPause1 : _md.AttackPause0);
        }

        private void ApplyDamageToTargetLikeOriginal(
            int damage,
            int weaponRadius,
            float tx,
            float ty,
            C2SettlementBuildingSelectableV1LikeOriginal interceptedBuilding = null)
        {
            bool killed = false;
            bool intercepted = interceptedBuilding != null;

            if (!intercepted && _targetUnit != null)
            {
                EnsureUnitLifeLikeOriginal(
                    _targetUnit, C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(_targetUnit));
                bool wasAlive = _targetUnit.LifeLikeOriginal > 0 && !_targetUnit.IsDeadLikeOriginal;

                // V408 single damage authority.  Brigade damage/shield, material masks,
                // weapon kind/protection and V404 morale are all applied inside MakeDamage.
                C2CombatCoreV408LikeOriginal.MakeDamageV408LikeOriginal(
                    _targetUnit, Mathf.Max(0, damage), _unit, _attackMode, true);

                if (_attackMode == 0 && _targetUnit.LifeLikeOriginal > 0 && !_targetUnit.IsDeadLikeOriginal)
                    C2FormationRuntimeV167LikeOriginal.OnMeleeDamageRetaliationV406LikeOriginal(
                        _targetUnit, _unit);

                killed = wasAlive && (_targetUnit.LifeLikeOriginal <= 0 || _targetUnit.IsDeadLikeOriginal);
                if (killed)
                {
                    int bombRadius, bombDamage;
                    C2OriginalProduceCatalogV13.C2MdIconInfoV13 targetMd =
                        C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(_targetUnit);
                    if (TryReadBombLikeOriginal(targetMd.Path, out bombRadius, out bombDamage))
                        ApplySplashLikeOriginal(tx, ty, bombRadius * 16.0f, bombDamage);
                }
            }
            else
            {
                C2SettlementBuildingSelectableV1LikeOriginal buildingVictim =
                    interceptedBuilding != null ? interceptedBuilding : _targetBuilding;
                if (buildingVictim != null)
                {
                    int damageWithBrigade = Mathf.Max(0, damage +
                        C2FormationRuntimeV167LikeOriginal.GetBrigadeExperienceDamageBonusV402LikeOriginal(
                            _unit, _attackMode, damage) +
                        C2FormationRuntimeV167LikeOriginal.GetBrigadeStandGroundDamageBonusV403LikeOriginal(
                            _unit, null));
                    bool handled = C2BuildingDeleteRuntimeLikeOriginal.C2BuildingDeleteRuntimeApplyCombatDamageLikeOriginal(
                        buildingVictim, damageWithBrigade, out killed);
                    if (!handled)
                    {
                        buildingVictim.LifeLikeOriginal = Mathf.Max(0, buildingVictim.LifeLikeOriginal - damageWithBrigade);
                        if (buildingVictim.LifeLikeOriginal <= 0)
                        {
                            killed = true;
                            buildingVictim.ReadyLikeOriginal = false;
                            buildingVictim.NotSelectable = true;
                        }
                    }
                }
                if (killed)
                    C2FormationRuntimeV167LikeOriginal.RegisterKillV402LikeOriginal(
                        _unit, "Nation.cpp::building_kill");
            }

            if (weaponRadius > 0)
                ApplySplashLikeOriginal(tx, ty, weaponRadius, damage);
            if (killed && (!intercepted || interceptedBuilding == _targetBuilding)) _active = false;
        }

        private void ApplySplashLikeOriginal(float centerRealX, float centerRealY, float radiusReal, int damage)
        {
            float r2 = radiusReal * radiusReal;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; units != null && i < units.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal victim = units[i];
                if (victim == null || victim == _unit || victim == _targetUnit || victim.IsDeadLikeOriginal) continue;
                if (!C2CombatCoreV408LikeOriginal.CanAttackRelationV408LikeOriginal(_unit, victim)) continue;
                float dx = CurrentRealX(victim) - centerRealX;
                float dy = CurrentRealY(victim) - centerRealY;
                if (dx * dx + dy * dy > r2) continue;
                EnsureUnitLifeLikeOriginal(victim, C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(victim));
                C2CombatCoreV408LikeOriginal.MakeDamageV408LikeOriginal(
                    victim, Mathf.Max(0, damage), _unit, _attackMode, true);
            }
        }

        public static float GetFormationTiringRemainingLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            if (representative == null) return 100.0f;

            // COSSACKS2/UnitsInterface.cpp::GetBrigadeParams loops strictly
            // from NPERSONAL to NMemb. Officer/flag/drummer are not part of
            // BP->Tiring, so use the same soldier-only formation slice here.
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationSoldierMembersV395LikeOriginal(
                    representative, out soldiers) ||
                soldiers == null || soldiers.Count == 0)
                return Mathf.Clamp(representative.GetTiredLikeOriginal / 1000.0f, 0.0f, 100.0f);

            int sum = 0;
            int count = 0;
            for (int i = 0; i < soldiers.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal soldier = soldiers[i];
                if (soldier == null || soldier.IsDeadLikeOriginal) continue;
                sum += Mathf.Clamp(soldier.GetTiredLikeOriginal, 0, 100000) / 1000;
                count++;
            }
            // Original integer averaging: T += OB->GetTired/1000; T /= N.
            return count > 0 ? (sum / count) : 100.0f;
        }

        private bool TargetAliveLikeOriginal()
        {
            if (_targetUnit != null) return !_targetUnit.IsDeadLikeOriginal && _targetUnit.LifeLikeOriginal > 0;
            return _targetBuilding != null && _targetBuilding.LifeLikeOriginal > 0;
        }

        private void GetTargetRealLikeOriginal(out float x, out float y)
        {
            if (_targetUnit != null) { x = CurrentRealX(_targetUnit); y = CurrentRealY(_targetUnit); return; }
            if (_targetBuilding != null) { x = _targetBuilding.RealX; y = _targetBuilding.RealY; return; }
            x = _fallbackTargetWorld.x; y = _fallbackTargetWorld.z;
        }

        private static float CurrentRealX(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
        }

        private static float CurrentRealY(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
        }

        private static byte DirectionFromDeltaLikeOriginal(float dx, float dy)
        {
            if (dx * dx + dy * dy < 0.001f) return 0;
            return (byte)(Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(dy, dx) / (Mathf.PI * 2.0f) * 256.0f, 256.0f)) & 255);
        }

        private static void EnsureUnitLifeLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info)
        {
            if (unit == null) return;
            int max = Mathf.Max(1, info.LifeMax);
            if (unit.MaxLifeLikeOriginal <= 0) unit.MaxLifeLikeOriginal = max;
            if (unit.LifeLikeOriginal <= 0 && !unit.IsDeadLikeOriginal) unit.LifeLikeOriginal = unit.MaxLifeLikeOriginal;
        }

        private static WeaponEffectLikeOriginal ResolveWeaponEffectLikeOriginal(
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md, int mode)
        {
            int unitDamage = mode == 2 ? md.Damage2 : (mode == 1 ? md.Damage1 : md.Damage0);
            string rootName = mode == 2 ? md.Weapon2 : (mode == 1 ? md.Weapon1 : md.Weapon0);
            WeaponEffectLikeOriginal result = new WeaponEffectLikeOriginal
            {
                HasWeapon = !string.IsNullOrWhiteSpace(rootName),
                RootName = rootName ?? string.Empty,
                Damage = Mathf.Max(0, unitDamage)
            };
            if (string.IsNullOrWhiteSpace(rootName) || string.IsNullOrWhiteSpace(md.Path))
                return result;

            string mdDirectory = Path.GetDirectoryName(md.Path);
            DirectoryInfo dataDirectory = !string.IsNullOrWhiteSpace(mdDirectory)
                ? Directory.GetParent(mdDirectory)
                : null;
            string ndsPath = dataDirectory != null
                ? Path.Combine(dataDirectory.FullName, "weapon.nds")
                : string.Empty;
            Dictionary<string, WeaponDefinitionLikeOriginal> definitions =
                LoadWeaponDefinitionsLikeOriginal(ndsPath);
            WeaponDefinitionLikeOriginal root;
            if (definitions == null || !definitions.TryGetValue(rootName, out root))
                return result;

            WeaponDefinitionLikeOriginal damageWeapon = FindPrimaryDamageWeaponLikeOriginal(
                root, definitions, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            if (damageWeapon == null) return result;
            result.DamageWeaponName = damageWeapon.Name;
            result.FullParent = damageWeapon.FullParent;
            result.Damage = damageWeapon.FullParent
                ? Mathf.Max(0, unitDamage)
                : Mathf.Max(0, damageWeapon.Damage);
            result.Radius = Mathf.Max(0, damageWeapon.Radius);
            result.Speed = Mathf.Max(0, damageWeapon.Speed);
            result.Propagation = damageWeapon.Propagation;
            return result;
        }

        private static string ResolveWeaponVisualAnimationLikeOriginal(
            string mdPath, string rootName, int seed)
        {
            if (string.IsNullOrWhiteSpace(mdPath) || string.IsNullOrWhiteSpace(rootName))
                return string.Empty;

            string mdDirectory = Path.GetDirectoryName(mdPath);
            DirectoryInfo dataDirectory = !string.IsNullOrWhiteSpace(mdDirectory)
                ? Directory.GetParent(mdDirectory)
                : null;
            string ndsPath = dataDirectory != null
                ? Path.Combine(dataDirectory.FullName, "weapon.nds")
                : string.Empty;
            Dictionary<string, WeaponDefinitionLikeOriginal> definitions =
                LoadWeaponDefinitionsLikeOriginal(ndsPath);
            if (definitions == null) return string.Empty;

            WeaponDefinitionLikeOriginal root;
            if (!definitions.TryGetValue(rootName, out root) || root == null)
                return string.Empty;

            List<string> smokeAnimations = new List<string>(4);
            CollectWeaponVisualAnimationsLikeOriginal(
                root, definitions, smokeAnimations,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            if (smokeAnimations.Count == 0) return string.Empty;

            int index = (seed & 0x7fffffff) % smokeAnimations.Count;
            return NormalizeWeaponAnimationNameLikeOriginal(smokeAnimations[index]);
        }

        private static void CollectWeaponVisualAnimationsLikeOriginal(
            WeaponDefinitionLikeOriginal current,
            Dictionary<string, WeaponDefinitionLikeOriginal> definitions,
            List<string> output,
            HashSet<string> visited,
            int depth)
        {
            if (current == null || depth > 5 || !visited.Add(current.Name ?? string.Empty))
                return;

            string animation = NormalizeWeaponAnimationNameLikeOriginal(current.AnimationName);
            if (!string.IsNullOrEmpty(animation) &&
                animation.IndexOf("SHOTFOG", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !output.Exists(x => string.Equals(x, animation, StringComparison.OrdinalIgnoreCase)))
                output.Add(animation);

            for (int i = 0; i < current.Children.Count; i++)
            {
                WeaponDefinitionLikeOriginal child;
                if (definitions.TryGetValue(current.Children[i], out child))
                    CollectWeaponVisualAnimationsLikeOriginal(
                        child, definitions, output, visited, depth + 1);
            }
            for (int i = 0; i < current.Sync.Count; i++)
            {
                WeaponDefinitionLikeOriginal child;
                if (definitions.TryGetValue(current.Sync[i], out child))
                    CollectWeaponVisualAnimationsLikeOriginal(
                        child, definitions, output, visited, depth + 1);
            }
        }

        private static string NormalizeWeaponAnimationNameLikeOriginal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string v = value.Trim();
            if (v.Length > 0 && v[0] == '@') v = "#" + v.Substring(1);
            return v;
        }

        private static Dictionary<string, WeaponDefinitionLikeOriginal>
            LoadWeaponDefinitionsLikeOriginal(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            Dictionary<string, WeaponDefinitionLikeOriginal> cached;
            if (WeaponDefinitionsByNdsPathLikeOriginal.TryGetValue(path, out cached))
                return cached;

            Dictionary<string, WeaponDefinitionLikeOriginal> definitions =
                new Dictionary<string, WeaponDefinitionLikeOriginal>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string section = string.Empty;
                string[] lines = File.ReadAllLines(path, System.Text.Encoding.Default);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = CleanDataLineLikeOriginal(lines[i]);
                    if (line.Length == 0 || line[0] == '/') continue;
                    if (line[0] == '[')
                    {
                        section = line.ToUpperInvariant();
                        continue;
                    }
                    string[] t = SplitDataTokensLikeOriginal(line);
                    if (t.Length == 0) continue;
                    if (section == "[MEMBERS]" && t.Length >= 9)
                    {
                        int damage;
                        int radius;
                        int speed;
                        if (!int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out damage) ||
                            !int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out radius) ||
                            !int.TryParse(t[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out speed))
                            continue;
                        WeaponDefinitionLikeOriginal definition = new WeaponDefinitionLikeOriginal
                        {
                            Name = t[0],
                            AnimationName = t[1],
                            Damage = damage,
                            Radius = radius << 4,
                            Speed = speed,
                            Propagation = ParsePropagationLikeOriginal(t[6])
                        };
                        definitions[definition.Name] = definition;
                    }
                    else if (section == "[CHILDWEAPON]" && t.Length >= 6)
                    {
                        WeaponDefinitionLikeOriginal parent;
                        int count;
                        if (!definitions.TryGetValue(t[0], out parent) ||
                            !int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                            continue;
                        for (int child = 0; child < count && 5 + child < t.Length; child++)
                            parent.Children.Add(t[5 + child]);
                    }
                    else if (section == "[SYNCWEAPON]" && t.Length >= 3)
                    {
                        WeaponDefinitionLikeOriginal parent;
                        int count;
                        if (!definitions.TryGetValue(t[0], out parent) ||
                            !int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                            continue;
                        for (int child = 0; child < count && 2 + child < t.Length; child++)
                            parent.Sync.Add(t[2 + child]);
                    }
                    else if (section == "[FULLPARENT]")
                    {
                        WeaponDefinitionLikeOriginal definition;
                        if (definitions.TryGetValue(t[0], out definition))
                            definition.FullParent = true;
                    }
                }
            }
            catch
            {
                return null;
            }
            WeaponDefinitionsByNdsPathLikeOriginal[path] = definitions;
            return definitions;
        }

        private static WeaponDefinitionLikeOriginal FindPrimaryDamageWeaponLikeOriginal(
            WeaponDefinitionLikeOriginal current,
            Dictionary<string, WeaponDefinitionLikeOriginal> definitions,
            HashSet<string> visited,
            int depth)
        {
            if (current == null || depth > 12 || !visited.Add(current.Name)) return null;
            WeaponDefinitionLikeOriginal best =
                current.FullParent || current.Damage > 0 ? current : null;
            int bestScore = ScoreDamageWeaponLikeOriginal(best);
            for (int group = 0; group < 2; group++)
            {
                List<string> names = group == 0 ? current.Sync : current.Children;
                for (int i = 0; i < names.Count; i++)
                {
                    WeaponDefinitionLikeOriginal child;
                    if (!definitions.TryGetValue(names[i], out child)) continue;
                    WeaponDefinitionLikeOriginal candidate = FindPrimaryDamageWeaponLikeOriginal(
                        child, definitions, visited, depth + 1);
                    int score = ScoreDamageWeaponLikeOriginal(candidate);
                    if (score > bestScore)
                    {
                        best = candidate;
                        bestScore = score;
                    }
                }
            }
            visited.Remove(current.Name);
            return best;
        }

        private static int ScoreDamageWeaponLikeOriginal(WeaponDefinitionLikeOriginal weapon)
        {
            if (weapon == null) return int.MinValue;
            int trajectory = weapon.Propagation == 3 || weapon.Propagation == 5 ||
                             weapon.Propagation == 8 ? 1000000 : 0;
            int inherited = weapon.FullParent ? 500000 : 0;
            return trajectory + inherited + Mathf.Max(0, weapon.Damage) * 100 +
                   Mathf.Min(9999, Mathf.Max(0, weapon.Radius));
        }

        private static int ParsePropagationLikeOriginal(string id)
        {
            if (string.Equals(id, "STAND", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(id, "SLIGHTUP", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(id, "RANDOM", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(id, "FLY", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(id, "IMMEDIATE", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(id, "ANGLE", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(id, "RANDOM1", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(id, "REFLECT", StringComparison.OrdinalIgnoreCase)) return 7;
            if (string.Equals(id, "SECTOR", StringComparison.OrdinalIgnoreCase)) return 8;
            return -1;
        }

        private static ComplexCannonProfileLikeOriginal LoadComplexCannonProfileLikeOriginal(string mdPath)
        {
            if (string.IsNullOrWhiteSpace(mdPath) || !File.Exists(mdPath)) return null;
            ComplexCannonProfileLikeOriginal cached;
            if (ComplexCannonByMdPathLikeOriginal.TryGetValue(mdPath, out cached))
                return cached;
            ComplexCannonProfileLikeOriginal result = null;
            try
            {
                string complexId = string.Empty;
                string[] mdLines = File.ReadAllLines(mdPath, System.Text.Encoding.Default);
                for (int i = 0; i < mdLines.Length; i++)
                {
                    string[] t = SplitDataTokensLikeOriginal(CleanDataLineLikeOriginal(mdLines[i]));
                    if (t.Length >= 2 && string.Equals(t[0], "COMPLEXOBJECT", StringComparison.OrdinalIgnoreCase))
                    {
                        complexId = t[1];
                        break;
                    }
                }
                string mdDirectory = Path.GetDirectoryName(mdPath);
                DirectoryInfo dataDirectory = !string.IsNullOrWhiteSpace(mdDirectory)
                    ? Directory.GetParent(mdDirectory)
                    : null;
                string objectsPath = dataDirectory != null
                    ? Path.Combine(dataDirectory.FullName, "ComplexObjects", "Objects.dat")
                    : string.Empty;
                if (complexId.Length == 0 || !File.Exists(objectsPath))
                {
                    ComplexCannonByMdPathLikeOriginal[mdPath] = null;
                    return null;
                }

                string[] lines = File.ReadAllLines(objectsPath, System.Text.Encoding.Default);
                string quantId = string.Empty;
                int attackX = 0;
                int attackY = 0;
                int attackZ = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] t = SplitDataTokensLikeOriginal(CleanDataLineLikeOriginal(lines[i]));
                    if (t.Length >= 3 && string.Equals(t[0], "#UNIT", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(t[1], complexId, StringComparison.OrdinalIgnoreCase))
                        quantId = t[2];
                    else if (t.Length >= 4 && string.Equals(t[0], "#ATTACK", StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(t[1], complexId, StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out attackX);
                        int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out attackZ);
                        if (t.Length >= 5)
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out attackY);
                    }
                }
                if (quantId.Length == 0) return null;

                result = new ComplexCannonProfileLikeOriginal
                {
                    ComplexId = complexId,
                    QuantId = quantId,
                    AttackX = attackX,
                    AttackY = attackY,
                    AttackZ = attackZ
                };
                ParseComplexQuantTimingsLikeOriginal(lines, quantId, result);
            }
            catch
            {
                result = null;
            }
            ComplexCannonByMdPathLikeOriginal[mdPath] = result;
            return result;
        }

        private static void ParseComplexQuantTimingsLikeOriginal(
            string[] lines, string quantId, ComplexCannonProfileLikeOriginal profile)
        {
            int index = -1;
            int directiveCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string[] t = SplitDataTokensLikeOriginal(CleanDataLineLikeOriginal(lines[i]));
                if (t.Length >= 5 && string.Equals(t[0], "#MQUANT", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t[1], quantId, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out directiveCount))
                {
                    index = i + 1;
                    break;
                }
            }
            if (index < 0 || directiveCount <= 0) return;

            Dictionary<string, int> stateParts =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int directive = 0; directive < directiveCount && index < lines.Length; directive++)
            {
                string line = NextComplexDataLineLikeOriginal(lines, ref index);
                string[] t = SplitDataTokensLikeOriginal(line);
                if (t.Length == 0) continue;
                if (t[0][0] == '$')
                {
                    int parts;
                    if (t.Length >= 2 && t[1][0] == '$')
                    {
                        stateParts.TryGetValue(t[1], out parts);
                        stateParts[t[0]] = parts;
                    }
                    else if (t.Length >= 2 && int.TryParse(
                                 t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parts))
                    {
                        stateParts[t[0]] = parts;
                        for (int part = 0; part < parts; part++)
                            NextComplexDataLineLikeOriginal(lines, ref index);
                    }
                    continue;
                }
                if (!string.Equals(t[0], "@TRANSFORM", StringComparison.OrdinalIgnoreCase) || t.Length < 3)
                    continue;

                int partCount;
                if (!stateParts.TryGetValue(t[1], out partCount)) continue;
                int maxTicks = 0;
                for (int part = 0; part < partCount; part++)
                {
                    string[] move = SplitDataTokensLikeOriginal(
                        NextComplexDataLineLikeOriginal(lines, ref index));
                    int moves;
                    if (move.Length < 2 || !string.Equals(move[0], "@MOVE", StringComparison.OrdinalIgnoreCase) ||
                        !int.TryParse(move[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out moves))
                        continue;
                    int partTicks = 0;
                    for (int m = 0; m < moves; m++)
                    {
                        string[] record = SplitDataTokensLikeOriginal(
                            NextComplexDataLineLikeOriginal(lines, ref index));
                        int ticks;
                        if (record.Length > 0 && int.TryParse(
                                record[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks))
                            partTicks += ticks;
                    }
                    maxTicks = Mathf.Max(maxTicks, partTicks);
                }
                int mode = ComplexAttackStateModeLikeOriginal(t[1], t[2], true);
                if (mode >= 0) profile.FireTicks[mode] = maxTicks;
                mode = ComplexAttackStateModeLikeOriginal(t[1], t[2], false);
                if (mode >= 0) profile.ReloadTicks[mode] = maxTicks;
            }
        }

        private static int ComplexAttackStateModeLikeOriginal(string from, string to, bool firing)
        {
            string charged = firing ? from : to;
            string uncharged = firing ? to : from;
            if (!string.Equals(uncharged, "$ATTACK_ALONE", StringComparison.OrdinalIgnoreCase)) return -1;
            if (string.Equals(charged, "$RATTACK_ALONE", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(charged, "$RATTACK1_ALONE", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(charged, "$RATTACK2_ALONE", StringComparison.OrdinalIgnoreCase)) return 2;
            return -1;
        }

        private static string NextComplexDataLineLikeOriginal(string[] lines, ref int index)
        {
            while (index < lines.Length)
            {
                string line = CleanDataLineLikeOriginal(lines[index++]);
                if (line.Length > 0) return line;
            }
            return string.Empty;
        }

        private static string CleanDataLineLikeOriginal(string source)
        {
            string line = (source ?? string.Empty).Trim();
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            if (comment >= 0) line = line.Substring(0, comment).Trim();
            return line;
        }

        private static string[] SplitDataTokensLikeOriginal(string line)
        {
            return (line ?? string.Empty).Split(
                new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool MdContainsCommandLikeOriginal(string path, string command)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = (lines[i] ?? string.Empty).Trim();
                    if (line.StartsWith(command, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TryReadBombLikeOriginal(string path, out int radius, out int damage)
        {
            radius = 0;
            damage = 0;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = (lines[i] ?? string.Empty).Trim();
                    if (!line.StartsWith("BOMB", StringComparison.OrdinalIgnoreCase)) continue;
                    string[] t = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (t.Length >= 3 &&
                        int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out radius) &&
                        int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out damage))
                        return radius > 0 && damage > 0;
                }
            }
            catch { }
            return false;
        }
    }

    // V390: original firearm smoke. WEAPON.ADS maps #SHOTFOG/#SHOTFOG1/#SHOTFOG3
    // to the shipped ShotFog*.g2d banks. Render those banks directly instead of
    // inventing a Unity particle/tracer for muskets.
    internal sealed class C2CombatMuzzleSmokeV390LikeOriginal : MonoBehaviour
    {
        private sealed class PackageLikeOriginal
        {
            public string Path = string.Empty;
            public int Dx;
            public int Dy;
        }

        private sealed class SmokeFrameLikeOriginal
        {
            public string Path = string.Empty;
            public int SpriteId;
            public int Dx;
            public int Dy;
        }

        private sealed class SmokeAnimationLikeOriginal
        {
            public string Name = string.Empty;
            public readonly List<SmokeFrameLikeOriginal> Frames = new List<SmokeFrameLikeOriginal>(200);
        }

        private sealed class SmokeBankLikeOriginal
        {
            public global::TemnyLessViewer.C2DirectSpriteBank Bank;
            public readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();
        }

        private static readonly Dictionary<string, SmokeBankLikeOriginal> Banks =
            new Dictionary<string, SmokeBankLikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<SmokeAnimationLikeOriginal>> AnimationsByDataRoot =
            new Dictionary<string, List<SmokeAnimationLikeOriginal>>(StringComparer.OrdinalIgnoreCase);
        private static Material SharedSmokeMaterialLikeOriginal;

        private SmokeAnimationLikeOriginal _animation;
        private SpriteRenderer _renderer;
        private float _born;
        private const float FramesPerSecondLikeOriginal = 25.0f;

        public static void SpawnLikeOriginal(
            Vector3 muzzleWorld,
            Vector3 targetWorld,
            string mdPath,
            string animationName,
            int seed)
        {
            string dataRoot = ResolveDataRootLikeOriginal(mdPath);
            if (string.IsNullOrEmpty(dataRoot)) return;

            List<SmokeAnimationLikeOriginal> animations = LoadAnimationsLikeOriginal(dataRoot);
            if (animations == null || animations.Count == 0) return;

            string wanted = NormalizeSmokeAnimationNameLikeOriginal(animationName);
            SmokeAnimationLikeOriginal animation = null;
            int variant = -1;
            if (!string.IsNullOrEmpty(wanted))
            {
                for (int i = 0; i < animations.Count; i++)
                {
                    if (animations[i] != null &&
                        string.Equals(animations[i].Name, wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        animation = animations[i];
                        variant = i;
                        break;
                    }
                }
            }
            if (animation == null)
            {
                variant = (seed & 0x7fffffff) % animations.Count;
                animation = animations[variant];
            }
            if (animation == null || animation.Frames.Count == 0) return;

            GameObject go = new GameObject("C2_SHOTFOG_V392_" +
                (animation.Name ?? variant.ToString(CultureInfo.InvariantCulture)));
            go.layer = 7; // same sprite-depth pass as units/buildings
            go.transform.position = muzzleWorld;
            C2CombatMuzzleSmokeV390LikeOriginal fx = go.AddComponent<C2CombatMuzzleSmokeV390LikeOriginal>();
            fx._animation = animation;
            fx._born = Time.realtimeSinceStartup;
            fx._renderer = go.AddComponent<SpriteRenderer>();
            fx._renderer.sortingOrder = 5200;
            if (SharedSmokeMaterialLikeOriginal == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    SharedSmokeMaterialLikeOriginal = new Material(shader);
                    SharedSmokeMaterialLikeOriginal.name = "C2_SHOTFOG_V392_SHARED";
                    SharedSmokeMaterialLikeOriginal.renderQueue = 3688;
                }
            }
            if (SharedSmokeMaterialLikeOriginal != null)
                fx._renderer.sharedMaterial = SharedSmokeMaterialLikeOriginal;
            fx.ApplyFrameLikeOriginal(0);
            fx.FaceBattleCameraLikeOriginal();

            Vector3 forward = targetWorld - muzzleWorld;
            forward.y = 0.0f;
            if (forward.sqrMagnitude > 0.001f)
                go.transform.position += forward.normalized * 1.5f;

            Debug.Log("[C2:SHOT EFFECT V392] animation='" + animation.Name +
                      "' frames=" + animation.Frames.Count.ToString(CultureInfo.InvariantCulture) +
                      " seed=" + seed.ToString(CultureInfo.InvariantCulture));
        }

        private void Update()
        {
            if (_animation == null || _animation.Frames.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            int frame = Mathf.FloorToInt(
                (Time.realtimeSinceStartup - _born) * FramesPerSecondLikeOriginal);
            if (frame >= _animation.Frames.Count)
            {
                Destroy(gameObject);
                return;
            }

            ApplyFrameLikeOriginal(Mathf.Max(0, frame));
            FaceBattleCameraLikeOriginal();
        }

        private void ApplyFrameLikeOriginal(int frameIndex)
        {
            if (_renderer == null || _animation == null ||
                frameIndex < 0 || frameIndex >= _animation.Frames.Count) return;

            SmokeFrameLikeOriginal source = _animation.Frames[frameIndex];
            SmokeBankLikeOriginal bank = GetBankLikeOriginal(source.Path);
            if (bank == null || bank.Bank == null) return;

            Sprite sprite;
            if (!bank.Sprites.TryGetValue(source.SpriteId, out sprite) || sprite == null)
            {
                int physical = bank.Bank.UnswizzleFrameIndexLikeOriginal(source.SpriteId);
                global::TemnyLessViewer.C2RenderedFrame rendered;
                string error;
                if (!bank.Bank.RenderFrame(physical, out rendered, out error) ||
                    rendered == null || rendered.Rgba == null ||
                    rendered.Width <= 0 || rendered.Height <= 0)
                    return;

                byte[] rgba = (byte[])rendered.Rgba.Clone();
                int row = rendered.Width * 4;
                byte[] tmp = new byte[row];
                for (int y = 0; y < rendered.Height / 2; y++)
                {
                    int a = y * row;
                    int b = (rendered.Height - 1 - y) * row;
                    Buffer.BlockCopy(rgba, a, tmp, 0, row);
                    Buffer.BlockCopy(rgba, b, rgba, a, row);
                    Buffer.BlockCopy(tmp, 0, rgba, b, row);
                }

                Texture2D tex = new Texture2D(
                    rendered.Width, rendered.Height, TextureFormat.RGBA32, false, false);
                tex.name = "C2_ShotFog_V391_" +
                           Path.GetFileNameWithoutExtension(source.Path) + "_" +
                           source.SpriteId.ToString(CultureInfo.InvariantCulture);
                tex.LoadRawTextureData(rgba);
                tex.Apply(false, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                // NewMon.cpp WEAPON.ADS: NewFrame.dx/dy are USERLC offsets.
                // Place the package's original animation anchor at the muzzle.
                float pivotX = rendered.Width > 0
                    ? Mathf.Clamp01((rendered.OriginX - source.Dx) / (float)rendered.Width)
                    : 0.5f;
                float pivotY = rendered.Height > 0
                    ? Mathf.Clamp01((rendered.Height - (rendered.OriginY - source.Dy)) / (float)rendered.Height)
                    : 0.5f;
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, rendered.Width, rendered.Height),
                    new Vector2(pivotX, pivotY),
                    1.0f,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = tex.name + "_Sprite";
                bank.Sprites[source.SpriteId] = sprite;
            }
            _renderer.sprite = sprite;
        }

        private static List<SmokeAnimationLikeOriginal> LoadAnimationsLikeOriginal(string dataRoot)
        {
            List<SmokeAnimationLikeOriginal> cached;
            if (AnimationsByDataRoot.TryGetValue(dataRoot, out cached)) return cached;

            cached = new List<SmokeAnimationLikeOriginal>(3);
            AnimationsByDataRoot[dataRoot] = cached;

            string ads = FindFileIgnoreCaseLikeOriginal(dataRoot, "weapon.ads");
            if (string.IsNullOrEmpty(ads) || !File.Exists(ads)) return cached;

            Dictionary<int, PackageLikeOriginal> packages = new Dictionary<int, PackageLikeOriginal>();
            try
            {
                string[] lines = File.ReadAllLines(ads);
                for (int li = 0; li < lines.Length; li++)
                {
                    string line = StripCommentLikeOriginal(lines[li]);
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] t = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (t.Length < 1) continue;

                    if (string.Equals(t[0], "USERLC", StringComparison.OrdinalIgnoreCase) && t.Length >= 6)
                    {
                        int index, dx, dy;
                        if (!int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out index) ||
                            !int.TryParse(t[t.Length - 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out dx) ||
                            !int.TryParse(t[t.Length - 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dy))
                            continue;
                        string path = ResolvePackagePathLikeOriginal(dataRoot, t[2]);
                        if (string.IsNullOrEmpty(path)) continue;
                        packages[index] = new PackageLikeOriginal { Path = path, Dx = dx, Dy = dy };
                        continue;
                    }

                    string animName = t[0];
                    if (!string.Equals(animName, "@SHOTFOG", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(animName, "@SHOTFOG1", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(animName, "#SHOTFOG3", StringComparison.OrdinalIgnoreCase))
                        continue;

                    SmokeAnimationLikeOriginal animation = new SmokeAnimationLikeOriginal
                    {
                        Name = NormalizeSmokeAnimationNameLikeOriginal(animName)
                    };
                    if (animName[0] == '@' && t.Length >= 5)
                    {
                        int packageIndex, from, to;
                        if (int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out packageIndex) &&
                            int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out from) &&
                            int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out to))
                        {
                            PackageLikeOriginal package;
                            if (packages.TryGetValue(packageIndex, out package))
                            {
                                int step = to >= from ? 1 : -1;
                                for (int sprite = from; ; sprite += step)
                                {
                                    animation.Frames.Add(new SmokeFrameLikeOriginal
                                    {
                                        Path = package.Path,
                                        SpriteId = sprite,
                                        Dx = package.Dx,
                                        Dy = package.Dy
                                    });
                                    if (sprite == to) break;
                                }
                            }
                        }
                    }
                    else if (animName[0] == '#' && t.Length >= 4)
                    {
                        int declaredFrames;
                        if (!int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out declaredFrames))
                            declaredFrames = 0;
                        int pairs = Math.Min(declaredFrames, (t.Length - 3) / 2);
                        for (int i = 0; i < pairs; i++)
                        {
                            int packageIndex, spriteId;
                            if (!int.TryParse(t[3 + i * 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out packageIndex) ||
                                !int.TryParse(t[4 + i * 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out spriteId))
                                continue;
                            PackageLikeOriginal package;
                            if (!packages.TryGetValue(packageIndex, out package)) continue;
                            animation.Frames.Add(new SmokeFrameLikeOriginal
                            {
                                Path = package.Path,
                                SpriteId = spriteId,
                                Dx = package.Dx,
                                Dy = package.Dy
                            });
                        }
                    }

                    if (animation.Frames.Count > 0)
                        cached.Add(animation);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:SHOT EFFECT V391] WEAPON.ADS parse failed: " + ex.Message);
            }

            Debug.Log("[C2:SHOT EFFECT V391] source='WEAPON.ADS/NewMon.cpp' variants=" +
                      cached.Count.ToString(CultureInfo.InvariantCulture) +
                      " root='" + dataRoot + "'");
            return cached;
        }

        private static string NormalizeSmokeAnimationNameLikeOriginal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string v = value.Trim();
            if (v.Length > 0 && v[0] == '@') v = "#" + v.Substring(1);
            return v;
        }

        private static string StripCommentLikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            string trimmed = line.Trim();
            if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("/", StringComparison.Ordinal)) return string.Empty;
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            return (comment >= 0 ? line.Substring(0, comment) : line).Trim();
        }

        private static string ResolvePackagePathLikeOriginal(string dataRoot, string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return string.Empty;
            string raw = packageName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string direct = Path.Combine(dataRoot, raw);
            if (File.Exists(direct)) return direct;
            if (File.Exists(direct + ".g2d")) return direct + ".g2d";
            if (File.Exists(direct + ".g16")) return direct + ".g16";

            string file = Path.GetFileName(raw);
            string found = FindFileIgnoreCaseLikeOriginal(dataRoot, file + ".g2d");
            if (!string.IsNullOrEmpty(found)) return found;
            return FindFileIgnoreCaseLikeOriginal(dataRoot, file + ".g16");
        }

        private static SmokeBankLikeOriginal GetBankLikeOriginal(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            SmokeBankLikeOriginal cached;
            if (Banks.TryGetValue(path, out cached)) return cached;

            global::TemnyLessViewer.C2DirectSpriteBank bank =
                new global::TemnyLessViewer.C2DirectSpriteBank();
            string error;
            if (!bank.Load(path, out error))
            {
                Debug.LogWarning("[C2:SHOT EFFECT V391] load failed path='" +
                                 path + "' error='" + error + "'");
                Banks[path] = null;
                return null;
            }

            cached = new SmokeBankLikeOriginal { Bank = bank };
            Banks[path] = cached;
            Debug.Log("[C2:SHOT EFFECT V391] loaded='" + Path.GetFileName(path) +
                      "' bankFrames=" + bank.FrameCount.ToString(CultureInfo.InvariantCulture) +
                      " dirs=" + bank.DirectionCount.ToString(CultureInfo.InvariantCulture) +
                      " logicalUnswizzle=1");
            return cached;
        }

        private void FaceBattleCameraLikeOriginal()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cameras = Camera.allCameras;
                for (int i = 0; cameras != null && i < cameras.Length; i++)
                {
                    Camera c = cameras[i];
                    if (c == null || !c.isActiveAndEnabled || c.targetTexture != null) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cam = c;
                        break;
                    }
                }
            }
            if (cam != null)
                transform.rotation = cam.transform.rotation;
        }

        private static string ResolveDataRootLikeOriginal(string mdPath)
        {
            if (string.IsNullOrEmpty(mdPath)) return string.Empty;
            try
            {
                DirectoryInfo dir = new FileInfo(mdPath).Directory;
                while (dir != null)
                {
                    if (string.Equals(dir.Name, "Data", StringComparison.OrdinalIgnoreCase))
                        return dir.FullName;
                    if (File.Exists(Path.Combine(dir.FullName, "weapon.nds")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch { }
            return string.Empty;
        }

        private static string FindFileIgnoreCaseLikeOriginal(string directory, string fileName)
        {
            try
            {
                string[] files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                for (int i = 0; files != null && i < files.Length; i++)
                {
                    if (string.Equals(Path.GetFileName(files[i]), fileName, StringComparison.OrdinalIgnoreCase))
                        return files[i];
                }
            }
            catch { }
            return string.Empty;
        }
    }

    internal sealed class C2CombatProjectileVisualV336LikeOriginal : MonoBehaviour
    {
        private Vector3 _start;
        private Vector3 _end;
        private float _born;
        private float _duration;
        private bool _arc;
        private LineRenderer _line;
        private Action<C2SettlementBuildingSelectableV1LikeOriginal, float, float> _onImpact;
        private C2BattleTerrainMode _mode;
        private Vector3 _previous;
        private float _targetRealX;
        private float _targetRealY;
        private bool _impactInvoked;

        public static void SpawnLikeOriginal(
            Vector3 start, Vector3 end, bool grenade, bool artillery, int nation,
            float sourceDuration,
            C2BattleTerrainMode mode,
            float targetRealX,
            float targetRealY,
            Action<C2SettlementBuildingSelectableV1LikeOriginal, float, float> onImpact,
            bool visibleLine = true)
        {
            GameObject go = new GameObject(grenade ? "C2_GRENADE_V336" : (artillery ? "C2_SHELL_V336" : "C2_SHOT_V336"));
            C2CombatProjectileVisualV336LikeOriginal visual = go.AddComponent<C2CombatProjectileVisualV336LikeOriginal>();
            visual._start = start;
            visual._end = end;
            visual._arc = grenade || artillery;
            visual._duration = sourceDuration > 0.0f
                ? sourceDuration
                : (grenade ? 0.55f : (artillery ? 0.42f : 0.085f));
            visual._onImpact = onImpact;
            visual._mode = mode;
            visual._previous = start;
            visual._targetRealX = targetRealX;
            visual._targetRealY = targetRealY;
            visual._born = Time.realtimeSinceStartup;
            if (visibleLine)
            {
                visual._line = go.AddComponent<LineRenderer>();
                visual._line.useWorldSpace = true;
                visual._line.positionCount = 2;
                visual._line.startWidth = grenade ? 0.055f : 0.025f;
                visual._line.endWidth = grenade ? 0.055f : 0.008f;
                visual._line.material = new Material(Shader.Find("Sprites/Default"));
                Color c = grenade || artillery ? new Color(0.18f, 0.16f, 0.10f, 1.0f) : new Color(1.0f, 0.88f, 0.55f, 0.95f);
                visual._line.startColor = visual._line.endColor = c;
                visual._line.SetPosition(0, start);
                visual._line.SetPosition(1, grenade || artillery ? start : end);
            }
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - _born) / Mathf.Max(0.01f, _duration));
            Vector3 p = Vector3.Lerp(_start, _end, t);
            if (_arc)
            {
                p.y += Mathf.Sin(t * Mathf.PI) * Mathf.Max(0.45f, Vector3.Distance(_start, _end) * 0.12f);
                if (_line != null)
                {
                    _line.SetPosition(0, p - Vector3.up * 0.025f);
                    _line.SetPosition(1, p + Vector3.up * 0.025f);
                }
            }
            if (_mode != null && _onImpact != null &&
                C2BuildingRuntimeInfoV247LikeOriginal.TryIntersect3DBarSegmentV378LikeOriginal(
                    _mode, _previous, p,
                    out C2SettlementBuildingSelectableV1LikeOriginal intercepted,
                    out float hitRealX,
                    out float hitRealY))
            {
                InvokeImpactV378LikeOriginal(intercepted, hitRealX, hitRealY);
                Destroy(gameObject);
                return;
            }
            _previous = p;
            if (t >= 1.0f)
            {
                InvokeImpactV378LikeOriginal(null, _targetRealX, _targetRealY);
                Destroy(gameObject);
            }
        }

        private void InvokeImpactV378LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal intercepted,
            float impactRealX,
            float impactRealY)
        {
            if (_impactInvoked) return;
            _impactInvoked = true;
            Action<C2SettlementBuildingSelectableV1LikeOriginal, float, float> impact = _onImpact;
            _onImpact = null;
            if (impact != null) impact(intercepted, impactRealX, impactRealY);
        }
    }
}
