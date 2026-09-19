using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private sealed partial class ParsedMap
        {
            internal readonly bool[] DontSelectPanicers = new bool[8];
        }

        internal bool DontSelectPanicersLikeOriginal(int nation)
        {
            return _map != null && nation >= 0 && nation < _map.DontSelectPanicers.Length && _map.DontSelectPanicers[nation];
        }

        private static void ParseMoraleMapOptionsLikeOriginal(BinaryReader reader, ParsedMap map, int payloadLength)
        {
            if (payloadLength < 4) return;
            int length = reader.ReadInt32();
            if (length <= 0 || length > payloadLength - 4) return;
            string source = DecodeMapXmlV336LikeOriginal(reader.ReadBytes(length)).TrimEnd('\0');
            try
            {
                XElement options = XElement.Parse(source);
                XElement players = options.Element("Players");
                if (players == null) return;
                for (int nation = 0; nation < map.DontSelectPanicers.Length; nation++)
                {
                    XElement player = players.Element("Player" + nation.ToString(CultureInfo.InvariantCulture));
                    XElement setting = player != null ? player.Element("DontSelectPanicers") : null;
                    string value = setting != null ? setting.Value.Trim() : string.Empty;
                    map.DontSelectPanicers[nation] = value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (System.Xml.XmlException exception)
            {
                Debug.LogWarning("[C2:MORALE] Invalid map options: " + exception.Message);
            }
        }
    }

    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        internal static bool TryReadMoraleGroupIdLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, out int groupId)
        {
            groupId = -1;
            if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal) return false;
            return _groupIdByUnitInstanceV172LikeOriginal.TryGetValue(unit.GetInstanceID(), out groupId)
                && _groupsByIdV172LikeOriginal.ContainsKey(groupId);
        }

        internal static int GetFormationMemberSlotCountLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null
                ? group.Units.Count : 0;
        }

        private static void InitializeFormationMoraleLikeOriginal(RuntimeFormationV172LikeOriginal group, bool birth)
        {
            if (birth)
            {
                int firstSoldier = Math.Max(0, group.CommandSlotCount);
                for (int index = firstSoldier; index < group.Units.Count; index++)
                {
                    var soldier = group.Units[index];
                    if (!IsLiveFormationMemberV404LikeOriginal(soldier)) continue;
                    group.StartMorale = C2MoraleRuntimeV404LikeOriginal.GetStartMoraleV404LikeOriginal(soldier);
                    break;
                }
            }
            for (int index = 0; index < group.Units.Count; index++)
            {
                var member = group.Units[index];
                if (!IsLiveFormationMemberV404LikeOriginal(member)) continue;
                C2MoraleRuntimeV404LikeOriginal.InitializeFormationLikeOriginal(member, group.GroupId, birth);
                return;
            }
        }

        internal static int GetFormationStartMoraleLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) ? group.StartMorale : 50;
        }

        internal static int GetFormationAddedMaxMoraleLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) ? group.AddMaxMorale : 0;
        }

        internal static int GetFormationMoraleRecoveryBonusLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) ? group.MoraleRecoveryBonus : 0;
        }

        internal static bool ChangeFormationMoraleBonusesLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int addedMaximumFixed, int recoveryPercent)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group)) return false;
            group.AddMaxMorale += addedMaximumFixed;
            group.MoraleRecoveryBonus += recoveryPercent;
            return true;
        }

        internal static bool TryGetMoraleMembersLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out List<C2NeutralPeasantUnitInfoV2LikeOriginal> members)
        {
            RuntimeFormationV172LikeOriginal group;
            bool found = TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group);
            members = found ? group.Units : null;
            return found;
        }

        internal static bool TryGetFormationMoraleInputsV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            out int groupId,
            out int totalSoldierSlots,
            out int liveSoldiers,
            out bool officerAlive,
            out bool drummerAlive,
            out bool flagAlive,
            out int brigadeExperience)
        {
            groupId = -1;
            totalSoldierSlots = 0;
            liveSoldiers = 0;
            officerAlive = false;
            drummerAlive = false;
            flagAlive = false;
            brigadeExperience = 0;

            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(representative, out group) || group == null)
                return false;

            groupId = group.GroupId;
            int command = group.CommandSlotCount >= 0 ? group.CommandSlotCount : 3;
            if (command < 0) command = 0;
            if (command > group.Units.Count) command = group.Units.Count;
            totalSoldierSlots = Mathf.Max(0, group.Units.Count - command);

            officerAlive = command > 0 && IsLiveFormationMemberV404LikeOriginal(group.Units[0]);
            drummerAlive = command > 1 && IsLiveFormationMemberV404LikeOriginal(group.Units[1]);
            flagAlive = command > 2 && IsLiveFormationMemberV404LikeOriginal(group.Units[2]);
            for (int i = command; i < group.Units.Count; i++)
                if (IsLiveFormationMemberV404LikeOriginal(group.Units[i])) liveSoldiers++;

            brigadeExperience = group.NKills / 100;
            return true;
        }

        internal static bool TryRemoveUnitFromFormationForPanicV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out int oldGroupId,
            out int remainingMembers,
            out bool erasedFormation)
        {
            oldGroupId = -1;
            remainingMembers = 0;
            erasedFormation = false;
            if (unit == null) return false;

            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            oldGroupId = group.GroupId;
            int idx = -1;
            for (int i = 0; i < group.Units.Count; i++)
            {
                if (ReferenceEquals(group.Units[i], unit))
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0) return false;

            group.Units[idx] = null; // retail keeps the brigade slot and writes 0xFFFF.
            RemoveUnitMembershipV172LikeOriginal(unit);
            // PanicUnit -> DeleteFromSelection2 before the panic order is installed.
            unit.SetSelected(false);
            _standGroundByUnitV403LikeOriginal.Remove(unit);
            _noSearchVictimByUnitV403LikeOriginal.Remove(unit);

            for (int i = 0; i < group.Units.Count; i++)
                if (IsLiveFormationMemberV404LikeOriginal(group.Units[i])) remainingMembers++;

            // Morale.cpp::RemoveFromFormation -> EraseBrigade when NF < 5.
            if (remainingMembers < 5)
            {
                // Retail EraseBrigade semantics: the formation-level orders and
                // cached brigade state must disappear together with the brigade.
                CancelBrigadeGoOnRoadV385ALikeOriginal(group.GroupId, "panic_erase_brigade", false);
                _standGroundByGroupV403LikeOriginal.Remove(group.GroupId);
                _tiringByGroupV403ELikeOriginal.Remove(group.GroupId);
                for (int i = 0; i < group.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                    if (member != null) RemoveUnitMembershipV172LikeOriginal(member);
                }
                _groupsByIdV172LikeOriginal.Remove(group.GroupId);
                erasedFormation = true;
            }

            Debug.Log("[C2:MORALE V404 PANIC_REMOVE] group=" + oldGroupId.ToString(CultureInfo.InvariantCulture) +
                      " remaining=" + remainingMembers.ToString(CultureInfo.InvariantCulture) +
                      " erased=" + erasedFormation +
                      " unit='" + (unit.SourceMonsterId ?? string.Empty) + "'");
            return true;
        }

        internal static bool TryGetFormationAverageLifeV404LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            out int life,
            out int maxLife)
        {
            life = 0;
            maxLife = 0;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(representative, out group) || group == null)
                return false;

            int command = group.CommandSlotCount >= 0 ? group.CommandSlotCount : 3;
            command = Mathf.Clamp(command, 0, group.Units.Count);
            int sum = 0;
            int count = 0;
            for (int i = command; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsLiveFormationMemberV404LikeOriginal(member)) continue;
                sum += Mathf.Max(0, member.LifeLikeOriginal);
                if (maxLife <= 0) maxLife = Mathf.Max(1, member.MaxLifeLikeOriginal);
                count++;
            }
            if (count <= 0) return false;
            life = sum / count;
            if (maxLife <= 0) maxLife = 1;
            return true;
        }

        internal static bool TryGetFormationCenterForUiV404LikeOriginal(
            int groupId,
            out C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            out float originalX,
            out float originalY)
        {
            representative = null;
            originalX = 0.0f;
            originalY = 0.0f;
            RuntimeFormationV172LikeOriginal group;
            if (!_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) || group == null)
                return false;

            float realX, realY;
            ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out realX, out realY);
            originalX = realX / 16.0f;
            originalY = realY / 16.0f;
            for (int i = 0; i < group.Units.Count; i++)
            {
                if (!IsLiveFormationMemberV404LikeOriginal(group.Units[i])) continue;
                representative = group.Units[i];
                return true;
            }
            return false;
        }

        private static bool IsLiveFormationMemberV404LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            return member != null && member.isActiveAndEnabled && !member.IsDeadLikeOriginal;
        }
    }
}
