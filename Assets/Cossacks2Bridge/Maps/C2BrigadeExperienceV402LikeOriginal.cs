using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // COSSACKS2 experience core used by Brigade.cpp / Nation.cpp / UnitsInterface.cpp.
    // This file intentionally does not port morale or StandGround.  It exposes the
    // exact brigade experience state they need so those systems can consume it later.
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        private const int DefaultExpGrowSpeedV402LikeOriginal = 100;
        private const int DefaultMaxBrigAddDamageV402LikeOriginal = 10000;
        private static int _maxBrigAddDamageV402LikeOriginal = int.MinValue;
        private static readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, int> _unitKillsV402LikeOriginal =
            new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallV402LikeOriginal()
        {
            Debug.Log("[C2:BRIG EXP V402B] installed source=COSSACKS2/Brigade.cpp+Nation.cpp+UnitsInterface.cpp+VUI_Actions.cpp state=NKills_x100/ExpGrowSpeed100 ui=va_SP_Kills+va_SP_KillsAward bonuses=SkillDamageFormation+VeteranExpert");
        }

        // Brigade.cpp::Brigade::GetBrigExp(): return NKills/100;
        public static bool TryGetBrigadeExperienceV402LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out int experience,
            out int rawNKills,
            out int expGrowSpeed,
            out float averageKills,
            out int skillStatus)
        {
            experience = 0;
            rawNKills = 0;
            expGrowSpeed = DefaultExpGrowSpeedV402LikeOriginal;
            averageKills = 0.0f;
            skillStatus = 0;

            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            rawNKills = group.NKills;
            // CII uses the field exactly as stored. ExpGrowSpeed may legitimately become
            // zero (or even negative) through BrigadeAbility; only brigade creation initializes it to 100.
            expGrowSpeed = group.ExpGrowSpeed;
            experience = group.NKills / 100;

            int commandSlots = group.CommandSlotCount >= 0 ? group.CommandSlotCount : 0;
            int soldierSlots = Mathf.Max(0, group.Units.Count - commandSlots);
            if (soldierSlots > 0)
                averageKills = (float)experience / soldierSlots;

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                !string.IsNullOrWhiteSpace(group.SoldierMemberId)
                    ? C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(group.SoldierMemberId)
                    : C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            if (soldierSlots > 0 && md.VeteranKills != 0 && md.ExpertKills != 0)
            {
                int avgInt = experience / soldierSlots;
                if (avgInt >= md.ExpertKills) skillStatus = 2;
                else if (avgInt >= md.VeteranKills) skillStatus = 1;
            }
            return true;
        }

        // Nation.cpp keeps OneObject::Kills separately from Brigade::NKills.
        // va_SP_Kills uses the unit counter for loose units/cannons and brigade experience for formations.
        public static int GetUnitKillsV402LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0;
            int kills;
            return _unitKillsV402LikeOriginal.TryGetValue(unit, out kills) ? kills : 0;
        }

        // Nation.cpp death path: Sender->Kills++; if(Sender->BrigadeID!=0xFFFF) BR->IncBrigExperience(100).
        public static void RegisterKillV402LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal killer,
            string source)
        {
            if (killer == null) return;
            int kills;
            _unitKillsV402LikeOriginal.TryGetValue(killer, out kills);
            kills++;
            _unitKillsV402LikeOriginal[killer] = kills;

            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(killer, out group) || group == null)
            {
                C2GameplayHudV1.C2GameplayHudV402InvalidateExperienceLikeOriginal();
                return;
            }

            int speed = group.ExpGrowSpeed;
            group.NKills += 100 * speed / 100;
            Debug.Log("[C2:BRIG EXP V402B] source='" + (source ?? string.Empty) +
                      "' group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " unitKills=" + kills.ToString(CultureInfo.InvariantCulture) +
                      " value=100 growSpeed=" + speed.ToString(CultureInfo.InvariantCulture) +
                      " rawNKills=" + group.NKills.ToString(CultureInfo.InvariantCulture) +
                      " exp=" + (group.NKills / 100).ToString(CultureInfo.InvariantCulture));
            C2GameplayHudV1.C2GameplayHudV402InvalidateExperienceLikeOriginal();
        }

        // Brigade.cpp::Brigade::IncBrigExperience(int Value):
        //     NKills += Value * ExpGrowSpeed / 100;
        public static bool IncBrigExperienceV402LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int value,
            string source)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            int speed = group.ExpGrowSpeed;
            group.NKills += value * speed / 100;

            Debug.Log("[C2:BRIG EXP V402] source='" + (source ?? string.Empty) +
                      "' group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " value=" + value.ToString(CultureInfo.InvariantCulture) +
                      " growSpeed=" + speed.ToString(CultureInfo.InvariantCulture) +
                      " rawNKills=" + group.NKills.ToString(CultureInfo.InvariantCulture) +
                      " exp=" + (group.NKills / 100).ToString(CultureInfo.InvariantCulture));
            C2GameplayHudV1.C2GameplayHudV402InvalidateExperienceLikeOriginal();
            return true;
        }

        // BrigadeAbility.cpp changes Brigade::ExpGrowSpeed additively while an
        // ability is active.  The ability subsystem is not ported yet, but this
        // preserves the exact field/operation for it instead of inventing a new XP rate.
        public static bool ChangeBrigadeExpGrowSpeedV402LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int delta)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            group.ExpGrowSpeed += delta;
            return true;
        }

        // Experience-dependent part of Brigade::GetBrigadeDamage().  Morale,
        // StandGround and BrigadeAbility damage are deliberately not mixed in here.
        public static int GetBrigadeExperienceDamageBonusV402LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            int attackType,
            int baseDamage)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(attacker, out group) || group == null)
                return 0;

            int exp = group.NKills / 100;
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(attacker);
            int extraDamage = 0;

            if (md.SkillDamageFormationBonus != 0 && attackType >= 0 && attackType < 31)
            {
                int mask = md.SkillDamageMask;
                if ((mask & (1 << attackType)) != 0)
                {
                    int step = md.SkillDamageFormationBonusStep;
                    int v = step != 0 ? (exp / step) * step : exp;
                    int ddm = v * md.SkillDamageFormationBonus / 100;
                    if (ddm > baseDamage * 19) ddm = baseDamage * 19;
                    int maxBrigAddDamage = ResolveMaxBrigAddDamageV402LikeOriginal();
                    if (ddm > maxBrigAddDamage) ddm = maxBrigAddDamage;
                    extraDamage += ddm;
                }
            }

            int commandSlots = group.CommandSlotCount >= 0 ? group.CommandSlotCount : 0;
            int soldierSlots = Mathf.Max(0, group.Units.Count - commandSlots);
            if (soldierSlots > 0 && md.VeteranKills != 0 && md.ExpertKills != 0)
            {
                int averageKills = exp / soldierSlots;
                if (averageKills >= md.ExpertKills) extraDamage += md.ExpertExtraDamage;
                else if (averageKills >= md.VeteranKills) extraDamage += md.VeteranExtraDamage;
            }
            return extraDamage;
        }

        // Experience-dependent part of Brigade::GetBrigadeProtection().
        public static int GetBrigadeExperienceShieldBonusV402LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal victim,
            C2NeutralPeasantUnitInfoV2LikeOriginal killer = null)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(victim, out group) || group == null)
                return 0;

            // Brigade.cpp::GetBrigadeProtection chooses GO from Killer when Killer is supplied;
            // only UI/no-killer calls fall back to the brigade MembID. Preserve that source quirk exactly.
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md = killer != null
                ? C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(killer)
                : (!string.IsNullOrWhiteSpace(group.SoldierMemberId)
                    ? C2OriginalProduceCatalogV13.LoadMdInfoForRawMemberV166LikeOriginal(group.SoldierMemberId)
                    : C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(victim));
            int commandSlots = group.CommandSlotCount >= 0 ? group.CommandSlotCount : 0;
            int soldierSlots = Mathf.Max(0, group.Units.Count - commandSlots);
            if (soldierSlots <= 0 || md.VeteranKills == 0 || md.ExpertKills == 0)
                return 0;

            int averageKills = (group.NKills / 100) / soldierSlots;
            if (averageKills >= md.ExpertKills) return md.ExpertExtraShield;
            if (averageKills >= md.VeteranKills) return md.VeteranExtraShield;
            return 0;
        }

        private static int ResolveMaxBrigAddDamageV402LikeOriginal()
        {
            if (_maxBrigAddDamageV402LikeOriginal != int.MinValue)
                return _maxBrigAddDamageV402LikeOriginal;

            int value = DefaultMaxBrigAddDamageV402LikeOriginal;
            string[] candidates =
            {
                @"C:\GSC Game World\Cossacks II\Data\EngineSettings.xml",
                @"C:\GSC Game World\Cossacks II\Data1\EngineSettings.xml",
                Path.Combine(Application.dataPath, "..", "Data", "EngineSettings.xml"),
                Path.Combine(Application.dataPath, "..", "Data1", "EngineSettings.xml"),
                Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "EngineSettings.xml")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                try
                {
                    string xml = File.ReadAllText(path);
                    Match m = Regex.Match(xml, @"<MaxBrigAddDamage>\s*(-?\d+)\s*</MaxBrigAddDamage>", RegexOptions.IgnoreCase);
                    int parsed;
                    if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        value = parsed;
                        break;
                    }
                }
                catch { }
            }

            _maxBrigAddDamageV402LikeOriginal = value;
            return value;
        }
    }

    public sealed partial class C2GameplayHudV1
    {
        internal static void C2GameplayHudV402InvalidateExperienceLikeOriginal()
        {
            if (_active == null) return;
            _active._nextRefresh = 0.0f;
            _active._lastSelectedCount = -999999;
        }
    }
}
