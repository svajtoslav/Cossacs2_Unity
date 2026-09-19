// C2UnitPortStage1MissingGameplayCompatV237.cs
// Stage-1 unit port compile bridge.
// Keeps current V234/V232 map/building renderer intact; provides only the missing old gameplay symbols
// referenced by the copied unit runtime. Real passability/resources/formation/build runtime can replace this later.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private static long s_c2PathProfileTicksLikeOriginal;
        private static int s_c2PathProfileCallsLikeOriginal;
        private static int s_c2PathProfileSuccessLikeOriginal;
        private static int s_c2PathProfileNextReportTickLikeOriginal;
        private static readonly Dictionary<string, int> s_c2PathProfileCallsBySourceLikeOriginal =
            new Dictionary<string, int>(StringComparer.Ordinal);

        // Resource ids copied from the old resource map constants.
        public const byte C2OriginalResourceWoodV1LikeOriginal = 0;
        public const byte C2OriginalResourceGoldV1LikeOriginal = 1;
        public const byte C2OriginalResourceStoneV1LikeOriginal = 2;
        public const byte C2OriginalResourceFoodV1LikeOriginal = 3;
        public const byte C2OriginalResourceIronV1LikeOriginal = 4;
        public const byte C2OriginalResourceCoalV1LikeOriginal = 5;
        public const byte C2OriginalResourceNoneV1LikeOriginal = 0xFE;
        public const byte C2OriginalResourceEmptyV1LikeOriginal = 0xFF;

        public static bool C2BuildingMotionFieldV1TryBuildPathRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            float toRealX,
            float toRealY,
            out Vector2[] path,
            int maxSearchCells,
            string profileSourceLikeOriginal = null)
        {
            long started = global::System.Diagnostics.Stopwatch.GetTimestamp();
            bool success = C2BuildingRuntimeInfoV247LikeOriginal.TryBuildPathRealV247LikeOriginal(
                fromRealX, fromRealY, toRealX, toRealY, out path, maxSearchCells);
            s_c2PathProfileTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - started;
            s_c2PathProfileCallsLikeOriginal++;
            if (success) s_c2PathProfileSuccessLikeOriginal++;
            string profileSource = string.IsNullOrEmpty(profileSourceLikeOriginal)
                ? "unspecified"
                : profileSourceLikeOriginal;
            int sourceCalls;
            s_c2PathProfileCallsBySourceLikeOriginal.TryGetValue(profileSource, out sourceCalls);
            s_c2PathProfileCallsBySourceLikeOriginal[profileSource] = sourceCalls + 1;

            int now = Environment.TickCount;
            if (s_c2PathProfileNextReportTickLikeOriginal == 0)
                s_c2PathProfileNextReportTickLikeOriginal = unchecked(now + 5000);
            else if (unchecked(now - s_c2PathProfileNextReportTickLikeOriginal) >= 0)
            {
                double ms = s_c2PathProfileTicksLikeOriginal * 1000.0 /
                    global::System.Diagnostics.Stopwatch.Frequency;
                var sourceParts = new List<string>(s_c2PathProfileCallsBySourceLikeOriginal.Count);
                foreach (KeyValuePair<string, int> pair in s_c2PathProfileCallsBySourceLikeOriginal)
                    sourceParts.Add(pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture));
                sourceParts.Sort(StringComparer.Ordinal);
                Debug.Log("[C2:PATH PROFILE] windowSec=5 calls=" +
                          s_c2PathProfileCallsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " success=" + s_c2PathProfileSuccessLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " totalMs=" + ms.ToString("0.000", CultureInfo.InvariantCulture) +
                          " avgMs=" + (ms / Math.Max(1, s_c2PathProfileCallsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                          " sources=[" + string.Join(",", sourceParts.ToArray()) + "]");
                s_c2PathProfileTicksLikeOriginal = 0L;
                s_c2PathProfileCallsLikeOriginal = 0;
                s_c2PathProfileSuccessLikeOriginal = 0;
                s_c2PathProfileCallsBySourceLikeOriginal.Clear();
                s_c2PathProfileNextReportTickLikeOriginal = unchecked(now + 5000);
            }
            return success;
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathOrDirectRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            float toRealX,
            float toRealY,
            out Vector2[] path,
            out bool directTravelClear,
            int maxSearchCells,
            string profileSourceLikeOriginal = null,
            int radiusCells = 1)
        {
            long started = global::System.Diagnostics.Stopwatch.GetTimestamp();
            bool success = C2BuildingRuntimeInfoV247LikeOriginal.TryBuildPathRealV247LikeOriginal(
                fromRealX,
                fromRealY,
                toRealX,
                toRealY,
                out path,
                out directTravelClear,
                maxSearchCells, radiusCells);
            s_c2PathProfileTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - started;
            s_c2PathProfileCallsLikeOriginal++;
            if (success) s_c2PathProfileSuccessLikeOriginal++;
            string profileSource = string.IsNullOrEmpty(profileSourceLikeOriginal)
                ? "unspecified"
                : profileSourceLikeOriginal;
            int sourceCalls;
            s_c2PathProfileCallsBySourceLikeOriginal.TryGetValue(profileSource, out sourceCalls);
            s_c2PathProfileCallsBySourceLikeOriginal[profileSource] = sourceCalls + 1;

            int now = Environment.TickCount;
            if (s_c2PathProfileNextReportTickLikeOriginal == 0)
                s_c2PathProfileNextReportTickLikeOriginal = unchecked(now + 5000);
            else if (unchecked(now - s_c2PathProfileNextReportTickLikeOriginal) >= 0)
            {
                double ms = s_c2PathProfileTicksLikeOriginal * 1000.0 /
                    global::System.Diagnostics.Stopwatch.Frequency;
                var sourceParts = new List<string>(s_c2PathProfileCallsBySourceLikeOriginal.Count);
                foreach (KeyValuePair<string, int> pair in s_c2PathProfileCallsBySourceLikeOriginal)
                    sourceParts.Add(pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture));
                sourceParts.Sort(StringComparer.Ordinal);
                Debug.Log("[C2:PATH PROFILE] windowSec=5 calls=" +
                          s_c2PathProfileCallsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " success=" + s_c2PathProfileSuccessLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " totalMs=" + ms.ToString("0.000", CultureInfo.InvariantCulture) +
                          " avgMs=" + (ms / Math.Max(1, s_c2PathProfileCallsLikeOriginal)).ToString("0.000", CultureInfo.InvariantCulture) +
                          " sources=[" + string.Join(",", sourceParts.ToArray()) + "]");
                s_c2PathProfileTicksLikeOriginal = 0L;
                s_c2PathProfileCallsLikeOriginal = 0;
                s_c2PathProfileSuccessLikeOriginal = 0;
                s_c2PathProfileCallsBySourceLikeOriginal.Clear();
                s_c2PathProfileNextReportTickLikeOriginal = unchecked(now + 5000);
            }
            return success;
        }

        public static bool C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
            float realX,
            float realY,
            int radiusCells)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedForUnitRealV247LikeOriginal(realX, realY, radiusCells);
        }

        public static bool C2BuildingMotionFieldV1CanTravelStraightRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            float toRealX,
            float toRealY)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.CanTravelStraightRealV247LikeOriginal(
                fromRealX, fromRealY, toRealX, toRealY);
        }

        public static bool C2BuildingMotionFieldV1IsBlockedRealLikeOriginal(float realX, float realY)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedRealV247LikeOriginal(realX, realY);
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(
            float realX,
            float realY,
            out float freeRealX,
            out float freeRealY,
            int maxRadiusCells)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.TryFindNearestFreeRealV247LikeOriginal(
                realX,
                realY,
                out freeRealX,
                out freeRealY,
                maxRadiusCells);
        }

        public static int C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string source)
        {
            if (unit == null) return 0;
            int cancelled = 0;
            C2BuildWorkerOrderV245LikeOriginal order = unit.GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
            if (order != null && order.enabled)
            {
                order.CancelFromExternalOrderLikeOriginal(source ?? "external_order");
                cancelled++;
            }
            C2GameplayUnitTaskV1 task = unit.GetComponent<C2GameplayUnitTaskV1>();
            if (task != null && task.enabled)
            {
                task.CancelForExternalOrderLikeOriginal(source ?? "external_order");
                cancelled++;
            }
            C2CombatRuntimeV334LikeOriginal combat = unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat != null && combat.enabled)
            {
                combat.CancelForExternalOrderLikeOriginal(source ?? "external_order");
                cancelled++;
            }

            // BrigadeOrder_RifleAttack is a brigade order in retail. Any replacing
            // normal movement/order destroys it, whose destructor clears RifleAttack.
            // This must run even when the per-unit CombatRuntime is already disabled,
            // otherwise the red rifle card survives a move command.  The same hook
            // interrupts the visible #ATTACK3 animation but deliberately preserves
            // delay/MaxDelay, so the green reload progress resumes when idle.
            C2CombatRuntimeV334LikeOriginal.OnExternalOrderReplacementV399LikeOriginal(
                unit, source ?? "external_order");
            return cancelled;
        }
    }

    public static class C2PlayerColorsLikeOriginal
    {
        // Data/EngineSettings.xml <NatColor> and Data/Nres.dat NATCOLOR.
        // Slot 7 is the brown neutral nation used by settlement Owner=7.
        public static readonly Color32[] NatColors =
        {
            new Color32(0xA5, 0x00, 0x00, 0xFF),
            new Color32(0x00, 0x3C, 0xC6, 0xFF),
            new Color32(0x29, 0xB6, 0x94, 0xFF),
            new Color32(0x9C, 0x49, 0xB5, 0xFF),
            new Color32(0xF7, 0x86, 0x10, 0xFF),
            new Color32(0x29, 0x28, 0x39, 0xFF),
            new Color32(0xE7, 0xE3, 0xE7, 0xFF),
            new Color32(0x6B, 0x41, 0x10, 0xFF),
        };

        private static readonly int[] PlayerColorId = { 0, 1, 2, 3, 4, 5, 6, 7 };
        private static readonly string[] ColorCacheSuffix = BuildColorCacheSuffixesLikeOriginal();

        public static int MaxPlayers { get { return PlayerColorId.Length; } }

        public static int ClampColorId(int colorId)
        {
            if (NatColors == null || NatColors.Length == 0) return 0;
            int m = NatColors.Length;
            colorId %= m;
            if (colorId < 0) colorId += m;
            return colorId;
        }

        public static int ClampPlayerIndex(int playerIndex)
        {
            if (PlayerColorId == null || PlayerColorId.Length == 0) return 0;
            if (playerIndex < 0) return 0;
            if (playerIndex >= PlayerColorId.Length) return playerIndex % PlayerColorId.Length;
            return playerIndex;
        }

        public static int GetPlayerColorId(int playerIndex)
        {
            int p = ClampPlayerIndex(playerIndex);
            return ClampColorId(PlayerColorId[p]);
        }

        public static void SetPlayerColorId(int playerIndex, int colorId)
        {
            int p = ClampPlayerIndex(playerIndex);
            PlayerColorId[p] = ClampColorId(colorId);
        }

        public static Color32 GetNatColorByColorId(int colorId)
        {
            return NatColors[ClampColorId(colorId)];
        }

        public static Color32 GetNatColorByPlayer(int playerIndex)
        {
            return GetNatColorByColorId(GetPlayerColorId(playerIndex));
        }

        public static string CacheSuffixForPlayer(int playerIndex)
        {
            int colorId = GetPlayerColorId(playerIndex);
            return ColorCacheSuffix[colorId];
        }

        private static string[] BuildColorCacheSuffixesLikeOriginal()
        {
            string[] result = new string[NatColors.Length];
            for (int colorId = 0; colorId < result.Length; colorId++)
            {
                Color32 c = NatColors[colorId];
                result[colorId] = "nat=" + colorId.ToString(CultureInfo.InvariantCulture) + "_" +
                                  c.r.ToString(CultureInfo.InvariantCulture) + "_" +
                                  c.g.ToString(CultureInfo.InvariantCulture) + "_" +
                                  c.b.ToString(CultureInfo.InvariantCulture);
            }
            return result;
        }
    }

    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        private sealed class RuntimeFormationV172LikeOriginal
        {
            public int GroupId;
            public int Nation;
            public string Shape = string.Empty;
            public string SoldierMemberId = string.Empty;
            public byte Direction;
            public int CommandSlotCount = -1;
            public int SpacingPercent = 100;
            public int Grenades;
            public float GrenadeLastUpdateAt;
            // COSSACKS2/Brigade.cpp::Brigade::Init / IncBrigExperience / GetBrigExp.
            // NKills is fixed-point x100: one normal kill adds 100 and GetBrigExp returns NKills/100.
            public int NKills;
            public int StartMorale = 50;
            public int AddMaxMorale;
            public int MoraleRecoveryBonus;
            public int ExpGrowSpeed = 100;
            public bool TurnActive;
            public byte TurnTargetDirection;
            public float NextTurnStepAt;
            public float TurnStepDeadlineAt;
            public bool UsesRoadMovement;
            public readonly bool[] ShotLinesEnabled = new bool[3];
            public readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> Units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            public readonly List<Vector2> Slots = new List<Vector2>();
        }

        private static int _nextGroupIdV172LikeOriginal = 1;
        private static readonly Dictionary<int, RuntimeFormationV172LikeOriginal> _groupsByIdV172LikeOriginal =
            new Dictionary<int, RuntimeFormationV172LikeOriginal>();
        private static readonly Dictionary<int, int> _groupIdByUnitInstanceV172LikeOriginal =
            new Dictionary<int, int>();

        public static bool IsUnitInRuntimeFormationV168LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group);
        }

        public static string CurrentShapeOfUnitV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) ? group.Shape : string.Empty;
        }

        public static void RegisterFormationV167LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            IList<Vector2> destSlots,
            string shape)
        {
            RegisterFormationInternalV172LikeOriginal(units, destSlots, shape, string.Empty, -1);
        }

        // Battle-editor placement already knows the exact objects it has just
        // created.  It must not call CreateBrigInZone, whose original contract
        // is to collect pre-existing nearby units around an officer.  Register
        // only this explicit set and keep every member at its placed position.
        public static bool TryRegisterPlacedFormationV346LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> commandUnits,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers,
            string shape,
            string soldierMemberId,
            out int groupId,
            out string audit)
        {
            groupId = -1;
            audit = "no_placed_soldiers";
            var units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            if (commandUnits != null)
                for (int i = 0; i < commandUnits.Count; i++)
                    AddUniqueUnitV172LikeOriginal(units, commandUnits[i]);
            int commandCount = units.Count;
            if (soldiers != null)
                for (int i = 0; i < soldiers.Count; i++)
                    AddUniqueUnitV172LikeOriginal(units, soldiers[i]);
            if (units.Count == 0)
                return false;

            var slots = new List<Vector2>(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[i];
                slots.Add(new Vector2(
                    unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX,
                    unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY));
                unit.StopMoveAndFaceDirectionLikeOriginal(unit.RealDir);
            }

            groupId = RegisterFormationInternalV172LikeOriginal(
                units, slots, shape ?? string.Empty, soldierMemberId ?? string.Empty, -1, commandCount);
            audit = "ok groupId=" + groupId.ToString(CultureInfo.InvariantCulture) +
                    " exactPlacedUnits=" + units.Count.ToString(CultureInfo.InvariantCulture) +
                    " commandUnits=" + (units.Count - (soldiers != null ? soldiers.Count : 0)).ToString(CultureInfo.InvariantCulture) +
                    " no_nearby_search no_assembly_move";
            return true;
        }

        // DIP_SimpleBuilding.cpp::CreateFormationFromGroup does not manufacture
        // officers for settlement militia. It takes the available defenders and
        // chooses an orders.lst layout that fits them. Keep that path separate
        // from the barracks/command-centre formation creator, which requires the
        // normal officer, drummer and flag composition.
        public static bool TryCreateMilitiaFormationFromGroupV340LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float centerRealX,
            float centerRealY,
            byte direction,
            out List<C2NeutralPeasantUnitInfoV2LikeOriginal> formationUnits,
            out int groupId,
            out string audit)
        {
            formationUnits = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            groupId = -1;
            audit = "no_eligible_units";
            if (sourceUnits == null) return false;

            for (int i = 0; i < sourceUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = sourceUnits[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true) ||
                    IsUnitInRuntimeFormationV168LikeOriginal(unit))
                    continue;
                formationUnits.Add(unit);
            }
            if (formationUnits.Count < 20)
            {
                audit = "not_enough_defenders found=" + formationUnits.Count.ToString(CultureInfo.InvariantCulture) + " required=20";
                formationUnits.Clear();
                return false;
            }

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = null;
            if (C2FormationCreateCatalogV165LikeOriginal.TryResolveForSelectedUnit(formationUnits[0], out record) &&
                record != null)
            {
                for (int i = 0; i < record.Options.Count; i++)
                {
                    C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal candidate = record.Options[i];
                    if (candidate == null || candidate.UnitCount <= 0 || candidate.UnitCount > formationUnits.Count)
                        continue;
                    bool candidateLine = string.Equals(candidate.Shape, "LINE", StringComparison.OrdinalIgnoreCase);
                    bool currentLine = option != null && string.Equals(option.Shape, "LINE", StringComparison.OrdinalIgnoreCase);
                    if (option == null || candidateLine && !currentLine || candidateLine == currentLine && candidate.UnitCount > option.UnitCount)
                        option = candidate;
                }
            }

            int wanted = option != null ? Mathf.Max(20, option.UnitCount) : formationUnits.Count;
            if (formationUnits.Count > wanted)
                formationUnits.RemoveRange(wanted, formationUnits.Count - wanted);

            // Brigade::CreateFromGroup ignores CreateFormationFromGroup's x/y arguments.
            // Without an officer it derives RX/RY from the current members and calls
            // CreateOrderedPositions(RX,RY,Dir) + KeepPositions. Sending the village
            // militia to the settlement object's centre was a port-only mistake that
            // repeatedly gathered them inside the mill/building footprint.
            float actualCenterX = 0.0f;
            float actualCenterY = 0.0f;
            for (int i = 0; i < formationUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = formationUnits[i];
                actualCenterX += unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
                actualCenterY += unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            }
            actualCenterX /= Mathf.Max(1, formationUnits.Count);
            actualCenterY /= Mathf.Max(1, formationUnits.Count);

            List<Vector2> slots = option != null
                ? BuildTemplateSlotsDirectedV352LikeOriginal(option, formationUnits.Count, 0, formationUnits, actualCenterX, actualCenterY, 100, direction)
                : BuildFallbackSlotsV172LikeOriginal(formationUnits.Count, actualCenterX, actualCenterY);
            OptimizeFormationSlotsForMotionFieldV347LikeOriginal(
                slots, formationUnits, actualCenterX, actualCenterY);
            string shape = option != null && !string.IsNullOrWhiteSpace(option.Shape) ? option.Shape : "LINE";
            string soldierMember = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(formationUnits[0]);
            groupId = RegisterFormationInternalV172LikeOriginal(formationUnits, slots, shape, soldierMember, -1, 0);
            RuntimeFormationV172LikeOriginal group;
            if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) && group != null)
                group.Direction = direction;

            for (int i = 0; i < formationUnits.Count && i < slots.Count; i++)
                formationUnits[i].SetFormationAssemblyDestinationRealLikeOriginal(
                    slots[i].x,
                    slots[i].y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    direction);

            audit = "ok group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                    " units=" + formationUnits.Count.ToString(CultureInfo.InvariantCulture) +
                    " shape='" + shape + "'" +
                    " order='" + (option != null ? option.OrderId : "fallback") + "'" +
                    " centerReal=(" + actualCenterX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    actualCenterY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                    " source=Brigade::CreateFromGroup_current_members_center_KeepPositions";
            return true;
        }

        public static bool TryGetGroupUnitsV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            out int groupId,
            out string shape)
        {
            units = null;
            groupId = -1;
            shape = string.Empty;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group)) return false;
            groupId = group.GroupId;
            shape = group.Shape;
            units = group.Units.FindAll(member => member != null);
            return true;
        }

        // V395 - direct equivalent of Brigade::GetCenter + Brigade::GetNearesEnmBrigade
        // for the runtime-formation registry.  Do not rediscover brigades by scanning
        // every unit: the original scans CITY[].Brigs[] and compares brigade centres
        // in (RealX>>4, RealY>>4) coordinates.
        public static bool TryGetFormationCenterV395LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out int groupId,
            out int nation,
            out float centerOriginalX,
            out float centerOriginalY,
            out byte direction,
            out C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            groupId = -1;
            nation = -1;
            centerOriginalX = 0.0f;
            centerOriginalY = 0.0f;
            direction = 0;
            representative = null;

            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            float realX, realY;
            ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out realX, out realY);
            centerOriginalX = realX / 16.0f;
            centerOriginalY = realY / 16.0f;
            groupId = group.GroupId;
            nation = group.Nation;
            direction = group.Direction;

            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(candidate, true)) continue;
                representative = candidate;
                break;
            }
            return representative != null;
        }

        public static bool TryFindNearestEnemyFormationV395LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal source,
            float radiusOriginalPixels,
            out C2NeutralPeasantUnitInfoV2LikeOriginal enemyRepresentative,
            out int enemyGroupId,
            out float sourceCenterOriginalX,
            out float sourceCenterOriginalY,
            out float enemyCenterOriginalX,
            out float enemyCenterOriginalY,
            out float distanceOriginalPixels,
            out string audit)
        {
            enemyRepresentative = null;
            enemyGroupId = -1;
            sourceCenterOriginalX = 0.0f;
            sourceCenterOriginalY = 0.0f;
            enemyCenterOriginalX = 0.0f;
            enemyCenterOriginalY = 0.0f;
            distanceOriginalPixels = float.MaxValue;
            audit = "source_not_in_brigade";

            RuntimeFormationV172LikeOriginal sourceGroup;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(source, out sourceGroup) || sourceGroup == null)
                return false;

            float sourceRealX, sourceRealY;
            ComputeFormationGroupCenterV172LikeOriginal(
                sourceGroup, sourceGroup.Units, out sourceRealX, out sourceRealY);
            sourceCenterOriginalX = sourceRealX / 16.0f;
            sourceCenterOriginalY = sourceRealY / 16.0f;

            float best = float.MaxValue;
            RuntimeFormationV172LikeOriginal bestGroup = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal bestRepresentative = null;
            float bestX = 0.0f, bestY = 0.0f;
            int checkedGroups = 0;

            foreach (KeyValuePair<int, RuntimeFormationV172LikeOriginal> pair in _groupsByIdV172LikeOriginal)
            {
                RuntimeFormationV172LikeOriginal candidateGroup = pair.Value;
                if (candidateGroup == null || candidateGroup.GroupId == sourceGroup.GroupId)
                    continue;
                if (candidateGroup.Nation == sourceGroup.Nation)
                    continue;

                C2NeutralPeasantUnitInfoV2LikeOriginal candidateRepresentative = null;
                for (int i = 0; i < candidateGroup.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal member = candidateGroup.Units[i];
                    if (!IsUsableFormationUnitV172LikeOriginal(member, true)) continue;
                    candidateRepresentative = member;
                    break;
                }
                if (candidateRepresentative == null) continue;

                checkedGroups++;
                float candidateRealX, candidateRealY;
                ComputeFormationGroupCenterV172LikeOriginal(
                    candidateGroup, candidateGroup.Units,
                    out candidateRealX, out candidateRealY);
                float cx = candidateRealX / 16.0f;
                float cy = candidateRealY / 16.0f;
                float dx = cx - sourceCenterOriginalX;
                float dy = cy - sourceCenterOriginalY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance < best)
                {
                    best = distance;
                    bestGroup = candidateGroup;
                    bestRepresentative = candidateRepresentative;
                    bestX = cx;
                    bestY = cy;
                }
            }

            distanceOriginalPixels = best;
            if (bestGroup == null || bestRepresentative == null)
            {
                audit = "no_hostile_runtime_brigade checked=" + checkedGroups.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            audit = "sourceGroup=" + sourceGroup.GroupId.ToString(CultureInfo.InvariantCulture) +
                    " sourceNation=" + sourceGroup.Nation.ToString(CultureInfo.InvariantCulture) +
                    " enemyGroup=" + bestGroup.GroupId.ToString(CultureInfo.InvariantCulture) +
                    " enemyNation=" + bestGroup.Nation.ToString(CultureInfo.InvariantCulture) +
                    " dist=" + best.ToString("0.0", CultureInfo.InvariantCulture) +
                    " radius=" + radiusOriginalPixels.ToString("0.0", CultureInfo.InvariantCulture) +
                    " checked=" + checkedGroups.ToString(CultureInfo.InvariantCulture);

            if (!(best < Mathf.Max(0.0f, radiusOriginalPixels)))
                return false;

            enemyRepresentative = bestRepresentative;
            enemyGroupId = bestGroup.GroupId;
            enemyCenterOriginalX = bestX;
            enemyCenterOriginalY = bestY;
            return true;
        }

        public static bool TryGetAliveFormationMemberByGroupIdV395LikeOriginal(
            int groupId,
            int hostileToNation,
            out C2NeutralPeasantUnitInfoV2LikeOriginal member)
        {
            member = null;
            RuntimeFormationV172LikeOriginal group;
            if (groupId < 0 || !_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) || group == null)
                return false;
            if (hostileToNation >= 0 && group.Nation == hostileToNation)
                return false;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(candidate, true)) continue;
                member = candidate;
                return true;
            }
            return false;
        }

        public static bool TryGetFormationSoldierMembersV395LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers)
        {
            soldiers = null;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            soldiers = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(candidate, true)) continue;
                // Retail GetBrigadeParams iterates BR->Memb[NBPERSONAL..NMemb).
                // Slot position is authoritative; it does not re-filter by member-id.
                if (i < Mathf.Max(0, group.CommandSlotCount)) continue;
                soldiers.Add(candidate);
            }
            return true;
        }

        public static bool TryGetFormationSoldierRepresentativeV332LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            representative = null;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = group.Units[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.IsDeadLikeOriginal)
                    continue;
                if (!MatchesFormationMemberV172LikeOriginal(candidate, group.SoldierMemberId))
                    continue;
                if (representative == null || candidate.SortKey < representative.SortKey)
                    representative = candidate;
            }

            if (representative != null)
                return true;

            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal candidate = group.Units[i];
                if (candidate != null && candidate.isActiveAndEnabled && !candidate.IsDeadLikeOriginal)
                {
                    representative = candidate;
                    break;
                }
            }
            return representative != null;
        }

        public static bool TryGetGrenadeStateV326LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int maxGrenadesPerHundred,
            int rechargeTimeFromMd,
            out int grenades,
            out int grenadesMax)
        {
            grenades = 0;
            grenadesMax = 0;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            int aliveMembers = 0;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (member != null && member.isActiveAndEnabled && !member.IsDeadLikeOriginal)
                    aliveMembers++;
            }
            grenadesMax = Mathf.Max(0, aliveMembers * Mathf.Max(0, maxGrenadesPerHundred) / 100);

            float now = Time.realtimeSinceStartup;
            if (group.GrenadeLastUpdateAt <= 0.0f)
                group.GrenadeLastUpdateAt = now;

            // NewMon.cpp stores GRENADE recharge>>3. Megapolis.cpp then waits
            // storedRecharge*8 original 25-Hz simulation ticks for every grenade.
            int storedRecharge = Mathf.Max(1, rechargeTimeFromMd >> 3);
            float secondsPerGrenade = Mathf.Max(0.04f, storedRecharge * 8.0f / 25.0f);
            if (group.Grenades < grenadesMax)
            {
                float elapsed = Mathf.Max(0.0f, now - group.GrenadeLastUpdateAt);
                int produced = Mathf.FloorToInt(elapsed / secondsPerGrenade);
                if (produced > 0)
                {
                    group.Grenades = Mathf.Min(grenadesMax, group.Grenades + produced);
                    group.GrenadeLastUpdateAt += produced * secondsPerGrenade;
                }
            }
            else
            {
                group.Grenades = Mathf.Min(group.Grenades, grenadesMax);
                group.GrenadeLastUpdateAt = now;
            }

            grenades = group.Grenades;
            return true;
        }

        public static int ConsumeGrenadesV326LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int requested)
        {
            RuntimeFormationV172LikeOriginal group;
            if (requested <= 0 ||
                !TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) ||
                group == null)
                return 0;
            int consumed = Mathf.Min(group.Grenades, requested);
            group.Grenades -= consumed;
            if (consumed > 0)
                group.GrenadeLastUpdateAt = Time.realtimeSinceStartup;
            return consumed;
        }

        public static bool TryGetFormationGroupIdV321LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out int groupId)
        {
            groupId = -1;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            groupId = group.GroupId;
            return true;
        }

        public static bool AreUnitsInSameFormationV321LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal a,
            C2NeutralPeasantUnitInfoV2LikeOriginal b)
        {
            if (a == null || b == null)
                return false;
            int aGroup;
            int bGroup;
            return TryGetFormationGroupIdV321LikeOriginal(a, out aGroup) &&
                   TryGetFormationGroupIdV321LikeOriginal(b, out bGroup) &&
                   aGroup == bGroup;
        }

        public static bool TryGetFormationSummaryV321LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out int groupId,
            out int liveMembers,
            out int totalMembers,
            out string shape,
            out byte direction)
        {
            groupId = -1;
            liveMembers = 0;
            totalMembers = 0;
            shape = string.Empty;
            direction = 0;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            groupId = group.GroupId;
            shape = group.Shape ?? string.Empty;
            direction = group.Direction;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            totalMembers = option != null ? Mathf.Max(1, option.UnitCount) : 0;

            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal member = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(member, true))
                    continue;

                string memberId =
                    C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(member);
                if (string.IsNullOrEmpty(group.SoldierMemberId) ||
                    string.Equals(memberId, group.SoldierMemberId, StringComparison.OrdinalIgnoreCase))
                {
                    liveMembers++;
                }
            }

            // Original va_SP_Amount displays brigade soldiers only:
            // NLiveMembers/NMembers. Officer, drummer and flag do not enter this counter.
            if (totalMembers <= 0)
                totalMembers = liveMembers;
            liveMembers = Mathf.Min(liveMembers, totalMembers);
            return true;
        }

        public static bool TryGetFormationDestinationSlotsV323LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out List<Vector2> slots,
            out byte direction)
        {
            slots = null;
            direction = 0;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;
            slots = new List<Vector2>(group.Slots);
            direction = group.Direction;
            return slots.Count > 0;
        }

        public static bool TryGetFormationSoldierPreviewSlotsV324LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out List<Vector2> slots,
            out byte direction)
        {
            slots = null;
            direction = 0;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units = GetFormationOrderMembersV360LikeOriginal(group);
            float centerX, centerY;
            ComputeFormationGroupCenterV172LikeOriginal(group, units, out centerX, out centerY);
            var record = ResolveRecordForGroupV320LikeOriginal(group);
            var option = FindFormationOptionV320LikeOriginal(record, group.Shape);
            slots = BuildFormationSoldierPreviewAtV359LikeOriginal(
                group, units, option, centerX, centerY, group.Direction);
            direction = group.Direction;
            return slots.Count > 0;
        }

        private static void ComputeFormationCommandTargetV359LikeOriginal(
            float groupX, float groupY, float selectionX, float selectionY,
            float destinationX, float destinationY, byte groupDirection, int turnDelta,
            out float targetX, out float targetY, out byte targetDirection)
        {
            byte delta = (byte)turnDelta;
            int cos = C2OriginalMovementMathV352.TCos[delta];
            int sin = C2OriginalMovementMathV352.TSin[delta];
            float dx = groupX - selectionX;
            float dy = groupY - selectionY;
            targetX = destinationX + (dx * cos - dy * sin) / 256.0f;
            targetY = destinationY + (dx * sin + dy * cos) / 256.0f;
            targetDirection = (byte)((groupDirection + turnDelta) & 255);
        }

        private static List<Vector2> BuildFormationOrderSlotsAtV359LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            float centerX, float centerY, byte direction)
        {
            if (option != null)
                return BuildTemplateSlotsDirectedV352LikeOriginal(
                    option, units.Count, ResolveOrderCommandCountV360LikeOriginal(group, units),
                    units, centerX, centerY, group.SpacingPercent, direction);

            List<Vector2> slots = BuildTranslatedGroupSlotsV172LikeOriginal(group, units, centerX, centerY);
            RotateSlotsFromDirectionV320LikeOriginal(
                slots, centerX, centerY, group.Direction, direction);
            return slots;
        }

        private static List<Vector2> BuildFormationSoldierPreviewAtV359LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            float centerX, float centerY, byte direction)
        {
            // Groups.cpp::BrigadesList::ShowPositions calls CreateSimpleOrdPos at
            // the destination direction. Rotating an already built layout uses
            // a different length/rounding than Norma/OScale and loses MD spacing.
            List<Vector2> allSlots = BuildFormationOrderSlotsAtV359LikeOriginal(
                group, units, option, centerX, centerY, direction);
            int available = Mathf.Min(allSlots.Count, units.Count);
            int firstSoldier = Mathf.Clamp(
                ResolveOrderCommandCountV360LikeOriginal(group, units), 0, available);
            // ShowPositions excludes NBPERSONAL and does not run the obstacle
            // optimizer. Keep command places and passability out of cyan marks.
            return allSlots.GetRange(firstSoldier, available - firstSoldier);
        }

        public static bool TryGetMiniComStateV324LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out bool canFill,
            out bool[] shotLines)
        {
            canFill = false;
            shotLines = new bool[3];
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                return false;

            // UnitsInterface.cpp::GetBrigadeParams does not keep a separate UI bool.
            // It rebuilds ShotLine[] from the real OB->RifleAttack flags of the
            // soldiers in each of the three physical formation lines every refresh.
            int[] physicalLinesV405A;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal
                shotOrderV405A;
            string shotAuditV405A;
            if (TryResolveThreeShotLinesV405ALikeOriginal(
                    group, out shotOrderV405A, out physicalLinesV405A, out shotAuditV405A))
            {
                for (int i = 0; i < 3; i++)
                {
                    shotLines[i] = IsShotLineEnabledV405ALikeOriginal(
                        group, shotOrderV405A, physicalLinesV405A[i]);
                    group.ShotLinesEnabled[i] = shotLines[i];
                }
            }
            else
            {
                for (int i = 0; i < shotLines.Length; i++)
                    group.ShotLinesEnabled[i] = false;
            }

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            int wanted = option != null ? Mathf.Max(1, option.UnitCount) : group.Units.Count;
            int alive = 0;
            for (int i = 0; i < group.Units.Count; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) alive++;
            canFill = alive < wanted;
            return true;
        }

        // UnitsInterface.cpp only exposes va_MC_LineShot when CP==3: exactly three
        // non-empty actual soldier lines. The C++ button stores IDXS[p], not a
        // guessed row number. Keep the same availability contract in Unity.
        public static bool HasThreeShotLinesV405ALikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order;
            int[] physicalLines;
            string audit;
            return TryResolveThreeShotLinesV405ALikeOriginal(
                group, out order, out physicalLines, out audit);
        }

        // Multi.cpp::ComShotLine under SIMPLEMANAGE, using the same bp=NBPERSONAL
        // row walk.  The UI State bit means "currently enabled"; ComShotLine then
        // writes RifleAttack=!State only to soldiers of that physical line.
        public static bool TryToggleShotLineV324LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            int lineIndex,
            out string audit)
        {
            audit = "not_runtime_formation";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;
            if (lineIndex < 0 || lineIndex >= 3)
            {
                audit = "invalid_line=" + lineIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order;
            int[] physicalLines;
            string resolveAudit;
            if (!TryResolveThreeShotLinesV405ALikeOriginal(
                    group, out order, out physicalLines, out resolveAudit))
            {
                audit = resolveAudit;
                return false;
            }

            int physicalLine = physicalLines[lineIndex];
            bool state = IsShotLineEnabledV405ALikeOriginal(group, order, physicalLine);
            bool enable = !state;
            int firstSoldier = Mathf.Max(0, group.CommandSlotCount);
            int applied = 0;
            int cancelled = 0;
            int skippedNoArmAttack = 0;

            for (int soldierOrdinal = 0; soldierOrdinal < order.SoldierPoints.Count; soldierOrdinal++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationPointV165LikeOriginal pt =
                    order.SoldierPoints[soldierOrdinal];
                if (pt == null || pt.LineIndex - order.FirstActualLine != physicalLine) continue;

                int slot = firstSoldier + soldierOrdinal;
                if (slot < 0 || slot >= group.Units.Count) continue;
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = group.Units[slot];
                if (!IsUsableFormationUnitV172LikeOriginal(ob, true)) continue;

                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                // SIMPLEMANAGE takes this branch only for newMons->ArmAttack.
                // Rifle line buttons are only exposed for that infantry family.
                if (!ob.ArmAttackCapableV396LikeOriginal)
                {
                    skippedNoArmAttack++;
                    continue;
                }

                C2CombatRuntimeV334LikeOriginal combat =
                    ob.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                bool hadBreakableEnemyOrder = combat != null && combat.IsActiveOrderV350LikeOriginal;

                // Raw 129/128 reaches the exact SetArmAttackState rifle branch: it
                // changes RifleAttack only and does not disturb delay/MaxDelay.
                if (C2CombatRuntimeV334LikeOriginal.SetArmAttackStateValueV396LikeOriginal(
                        ob, enable ? 129 : 128))
                    applied++;

                // ComShotLine: when disabling, EnemyID!=FFFF and the order is breakable
                // -> ClearOrders(). Do not cancel a pure ATTACK3 reload: delay survives
                // and an idle reloader has no active enemy AttackObj here.
                if (!enable && hadBreakableEnemyOrder && combat != null)
                {
                    combat.CancelForExternalOrderLikeOriginal(
                        "Multi.cpp::ComShotLine_disable_v405a");
                    cancelled++;
                }
            }

            group.ShotLinesEnabled[lineIndex] = enable;
            if (enable)
            {
                // In C++ the next Brigade::Bitva/BrigadeOrder_Bitva pass notices
                // RifleAttack && delay==0. V405 is that managed brigade-order pass.
                C2BrigadeRifleAttackV405LikeOriginal.OnRifleStateEnabledLikeOriginal(selected);
            }

            audit = "ok group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                    " uiLine=" + lineIndex.ToString(CultureInfo.InvariantCulture) +
                    " physicalLine=" + physicalLine.ToString(CultureInfo.InvariantCulture) +
                    " stateBefore=" + state.ToString() +
                    " enabled=" + enable.ToString() +
                    " applied=" + applied.ToString(CultureInfo.InvariantCulture) +
                    " cancelled=" + cancelled.ToString(CultureInfo.InvariantCulture) +
                    " skippedNoArmAttack=" + skippedNoArmAttack.ToString(CultureInfo.InvariantCulture) +
                    " original=UnitsInterface.cpp::ShotLine+Multi.cpp::ComShotLine";
            return applied > 0;
        }

        private static bool TryResolveThreeShotLinesV405ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            out C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order,
            out int[] physicalLines,
            out string audit)
        {
            order = null;
            physicalLines = null;
            audit = "shot_line_no_order";
            if (group == null) return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            order = option != null ? option.OrderTemplate : null;
            if (order == null || order.SoldierPoints == null || order.SoldierPoints.Count == 0)
                return false;

            List<int> nonEmpty = new List<int>(3);
            int actual = Mathf.Max(0, order.ActualLineCount);
            for (int p = 0; p < actual; p++)
            {
                int absoluteLine = order.FirstActualLine + p;
                bool has = false;
                for (int q = 0; q < order.SoldierPoints.Count; q++)
                {
                    C2FormationCreateCatalogV165LikeOriginal.C2FormationPointV165LikeOriginal pt =
                        order.SoldierPoints[q];
                    if (pt != null && pt.LineIndex == absoluteLine)
                    {
                        has = true;
                        break;
                    }
                }
                if (has) nonEmpty.Add(p);
            }

            // UnitsInterface.cpp writes ShotLine[] only when CP==3.
            if (nonEmpty.Count != 3)
            {
                audit = "shot_line_cp=" + nonEmpty.Count.ToString(CultureInfo.InvariantCulture) +
                        " required=3 order='" + (order.OrderId ?? string.Empty) + "'";
                return false;
            }

            physicalLines = new[] { nonEmpty[0], nonEmpty[1], nonEmpty[2] };
            audit = "ok order='" + (order.OrderId ?? string.Empty) + "'";
            return true;
        }

        private static bool IsShotLineEnabledV405ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal order,
            int physicalLine)
        {
            if (group == null || order == null) return false;
            int firstSoldier = Mathf.Max(0, group.CommandSlotCount);
            for (int soldierOrdinal = 0; soldierOrdinal < order.SoldierPoints.Count; soldierOrdinal++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationPointV165LikeOriginal pt =
                    order.SoldierPoints[soldierOrdinal];
                if (pt == null || pt.LineIndex - order.FirstActualLine != physicalLine) continue;
                int slot = firstSoldier + soldierOrdinal;
                if (slot < 0 || slot >= group.Units.Count) continue;
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = group.Units[slot];
                if (!IsUsableFormationUnitV172LikeOriginal(ob, true)) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                // UnitsInterface.cpp sets LENB[CP] when ANY valid member in that
                // physical line has OB->RifleAttack.
                if (ob.RifleAttackV396LikeOriginal) return true;
            }
            return false;
        }

        public static void ReplaceFormationSlotsV172LikeOriginal(
            int groupId,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            IList<Vector2> destSlots,
            string shape)
        {
            RuntimeFormationV172LikeOriginal group;
            if (!_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) || group == null)
            {
                RegisterFormationInternalV172LikeOriginal(units, destSlots, shape, string.Empty, groupId);
                return;
            }

            for (int i = 0; i < group.Units.Count; i++)
                RemoveUnitMembershipV172LikeOriginal(group.Units[i]);

            group.Units.Clear();
            group.Slots.Clear();
            group.Shape = shape ?? string.Empty;
            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                    if (u == null) continue;
                    group.Units.Add(u);
                    _groupIdByUnitInstanceV172LikeOriginal[u.GetInstanceID()] = group.GroupId;
                }
            }
            if (destSlots != null)
            {
                for (int i = 0; i < destSlots.Count; i++)
                    group.Slots.Add(destSlots[i]);
            }
        }

        // Multi.cpp::SendSelectedToXY (COSSACKS2): a short movement command can
        // retain attack state. The whole selected group gets Prio=128 when at least
        // one brigade member is in GroundState/rifle state, unless the destination is
        // farther than RETREATRADIUS (default 500 px) or more than 10% are already
        // in close combat (<150 px). This is the retail "walk with lowered rifles"
        // behavior; it is not a cosmetic Unity shortcut.
        private static bool ShouldPreserveAttackStateForMoveV405BLikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float destRealCenterX,
            float destRealCenterY,
            out int totalBrigadeUnits,
            out int attackingMeleeNow,
            out bool farSent)
        {
            totalBrigadeUnits = 0;
            attackingMeleeNow = 0;
            farSent = false;
            bool inAttackState = false;
            if (sourceUnits == null) return false;

            for (int i = 0; i < sourceUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = sourceUnits[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                RuntimeFormationV172LikeOriginal group;
                if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null)
                    continue;

                totalBrigadeUnits++;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(unit);
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                int posture = link != null ? link.GetPostureWeaponTypeV405BLikeOriginal() : -1;
                if (unit.GroundStateV396LikeOriginal != 0 ||
                    unit.NewStateV396LikeOriginal == 2 || posture == 1)
                    inAttackState = true;

                if (C2CombatRuntimeV334LikeOriginal.HasActiveEnemyWithinV405BLikeOriginal(unit, 150))
                    attackingMeleeNow++;

                float gx;
                float gy;
                ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out gx, out gy);
                int retreatRadius = link != null ? link.GetRetreatRadiusV405BLikeOriginal() : 0;
                if (retreatRadius <= 0) retreatRadius = 500;
                int distanceReal = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt(gx - destRealCenterX),
                    Mathf.RoundToInt(gy - destRealCenterY));
                if (distanceReal > retreatRadius * 16)
                    farSent = true;
            }

            if (attackingMeleeNow > totalBrigadeUnits / 10 || farSent)
                inAttackState = false;
            return inAttackState;
        }

        private static void ApplyAttackStateMoveToFormationV405BLikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> groupUnits,
            int commandPrefix,
            bool preserveAttackState)
        {
            if (groupUnits == null) return;
            for (int i = 0; i < groupUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = groupUnits[i];
                if (unit == null) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                bool soldier = i >= commandPrefix;
                if (link != null)
                    link.SetAttackStateMovementV405BLikeOriginal(preserveAttackState && soldier);
                if (!soldier) continue;

                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(unit);
                if (preserveAttackState)
                {
                    // Brigade.cpp::B_KeepPositionsLink: BR->AttEnm => State=1 for
                    // p=NBPERSONAL..NMemb. Rifle state 2 therefore becomes the
                    // lowered/bayonet movement state while the short move executes.
                    unit.GroundStateV396LikeOriginal = 1;
                    unit.NewStateV396LikeOriginal = 1;
                    if (link != null) link.SetCombatPostureV322LikeOriginal(0, true);
                }
                else
                {
                    unit.GroundStateV396LikeOriginal = 0;
                    unit.NewStateV396LikeOriginal = 0;
                    // Do not force UATTACK here. NewMon.cpp::TryToMove notices the
                    // state mismatch and TryToStand completes the neutral transition
                    // before ordinary movement; the runtime movement layer does the same.
                }
            }
        }

        public static bool TryIssueMoveV167LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float destRealCenterX,
            float destRealCenterY,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            string cancelSource,
            out int issued,
            out string audit)
        {
            return TryIssueMoveV167LikeOriginal(
                sourceUnits, destRealCenterX, destRealCenterY,
                hasFinalFacingDir, finalFacingDir, 0, cancelSource,
                out issued, out audit);
        }

        public static bool TryIssueMoveV167LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float destRealCenterX,
            float destRealCenterY,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            byte ordType,
            string cancelSource,
            out int issued,
            out string audit)
        {
            issued = 0;
            audit = "no_source_units";
            if (sourceUnits == null || sourceUnits.Count == 0) return false;

            List<RuntimeFormationV172LikeOriginal> groups = new List<RuntimeFormationV172LikeOriginal>(4);
            HashSet<int> seenGroups = new HashSet<int>();
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> looseUnitList =
                new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            int looseUnits = 0;
            for (int i = 0; i < sourceUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = sourceUnits[i];
                if (!IsUsableFormationUnitV172LikeOriginal(u, true)) continue;

                RuntimeFormationV172LikeOriginal group;
                if (!TryGetRuntimeGroupByUnitV172LikeOriginal(u, out group) || group == null)
                {
                    looseUnits++;
                    looseUnitList.Add(u);
                    continue;
                }

                if (seenGroups.Add(group.GroupId))
                    groups.Add(group);
            }

            // Ordinary/produced soldiers are never registered as a temporary brigade.
            // A loose-only selection falls back to C2GameplayLooseGroupMoveLikeOriginal;
            // a mixed selection preserves every real brigade and gives the loose subset
            // separate grid destinations.
            if (groups.Count == 0)
            {
                audit = "no_runtime_formation_group groups=" + groups.Count.ToString(CultureInfo.InvariantCulture) +
                        " looseUnits=" + looseUnits.ToString(CultureInfo.InvariantCulture) +
                        " fallback=loose_group_rotated_grid";
                return false;
            }

            int attackMoveTotalV405B;
            int attackMoveMeleeV405B;
            bool attackMoveFarV405B;
            bool preserveAttackStateV405B = ShouldPreserveAttackStateForMoveV405BLikeOriginal(
                sourceUnits, destRealCenterX, destRealCenterY,
                out attackMoveTotalV405B, out attackMoveMeleeV405B, out attackMoveFarV405B);

            float groupsCenterX;
            float groupsCenterY;
            ComputeGroupsCenterV172LikeOriginal(groups, out groupsCenterX, out groupsCenterY);
            byte groupsAverageDirection = ComputeGroupsAverageDirectionV346LikeOriginal(groups);
            int formationTurnDelta = hasFinalFacingDir
                ? (sbyte)(finalFacingDir - groupsAverageDirection)
                : 0;
            int blockedKare = 0;
            int roadGroups = 0;
            int coherentPathGroups = 0;
            int symmetricMoveGroups = 0;
            int blockedPaths = 0;

            for (int g = 0; g < groups.Count; g++)
            {
                RuntimeFormationV172LikeOriginal group = groups[g];
                if (group == null) continue;
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal orderRecordV352 =
                    ResolveRecordForGroupV320LikeOriginal(group);
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal orderOptionV352 =
                    FindFormationOptionV320LikeOriginal(orderRecordV352, group.Shape);
                int ordUsageV352 = orderOptionV352 != null && orderOptionV352.OrderTemplate != null
                    ? orderOptionV352.OrderTemplate.Usage
                    : (string.Equals(group.Shape, "KARE", StringComparison.OrdinalIgnoreCase) ? 2 : 0);
                // Brigade.cpp::HumanGlobalSendTo: OrdUsage==2 rejects global movement.
                if (ordUsageV352 == 2)
                {
                    blockedKare++;
                    continue;
                }

                List<C2NeutralPeasantUnitInfoV2LikeOriginal> groupUnits = GetFormationOrderMembersV360LikeOriginal(group);
                if (!groupUnits.Exists(u => u != null)) continue;
                // Groups.cpp::BrigadesList::SendToPositions: only a normal
                // (Prio&127) move clears AttEnm/stand state. Prio==128 retains
                // attack-state motion.
                if (!preserveAttackStateV405B)
                {
                    for (int sg = 0; sg < groupUnits.Count; sg++)
                    {
                        if (groupUnits[sg] == null) continue;
                        SetStandStateV403LikeOriginal(groupUnits[sg], 0);
                        break;
                    }
                }

                float groupCenterX;
                float groupCenterY;
                ComputeFormationGroupCenterV172LikeOriginal(group, groupUnits, out groupCenterX, out groupCenterY);

                float targetCenterX, targetCenterY;
                byte moveDirection;
                ComputeFormationCommandTargetV359LikeOriginal(
                    groupCenterX, groupCenterY, groupsCenterX, groupsCenterY,
                    destRealCenterX, destRealCenterY, group.Direction, formationTurnDelta,
                    out targetCenterX, out targetCenterY, out moveDirection);
                float travelDx = targetCenterX - groupCenterX;
                float travelDy = targetCenterY - groupCenterY;
                int travelDistance = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt(travelDx), Mathf.RoundToInt(travelDy));
                // Brigade.cpp::HumanGlobalSendTo exact threshold selection:
                // R0=MinDistanceToEnterRoad (800); non-command MD may override it.
                // OrdUsage==0 switches to MinDistForLineFormations (1800) and ONLY
                // MinDistForLineFormations may override that value.
                int roadEntryDistancePxV352 = ordUsageV352 == 0 ? 1800 : 800;
                int commandPrefixV352 = ResolveOrderCommandCountV360LikeOriginal(group, groupUnits);
                ApplyAttackStateMoveToFormationV405BLikeOriginal(
                    groupUnits, commandPrefixV352, preserveAttackStateV405B);
                C2NeutralPeasantUnitInfoV2LikeOriginal roadReferenceV352 = null;
                for (int i = commandPrefixV352; i < groupUnits.Count && roadReferenceV352 == null; i++)
                    roadReferenceV352 = groupUnits[i];
                C2UnitOriginalRuntimeLinkLikeOriginal roadLinkV352 = roadReferenceV352 != null
                    ? roadReferenceV352.RuntimeLinkCachedLikeOriginal
                    : null;
                if (roadLinkV352 != null && roadLinkV352.Runtime != null && roadLinkV352.Runtime.Md != null)
                {
                    if (ordUsageV352 == 0)
                    {
                        if (roadLinkV352.Runtime.Md.MinDistForLineFormations > 0)
                            roadEntryDistancePxV352 = roadLinkV352.Runtime.Md.MinDistForLineFormations;
                    }
                    else if (roadLinkV352.Runtime.Md.MinDistanceToEnterRoad > 0)
                    {
                        roadEntryDistancePxV352 = roadLinkV352.Runtime.Md.MinDistanceToEnterRoad;
                    }
                }
                bool longMove = travelDistance >= roadEntryDistancePxV352 * 16.0f;

                Vector2[] roadCenterPath = null;
                string roadAudit = string.Empty;
                bool useRoad = longMove &&
                    C2BattleTerrainMode.C2FormationTryBuildRoadPathRealV320LikeOriginal(
                        groupCenterX,
                        groupCenterY,
                        targetCenterX,
                        targetCenterY,
                        out roadCenterPath,
                        out roadAudit);

                if (useRoad && roadCenterPath != null && roadCenterPath.Length > 1)
                {
                    // Brigade.cpp B_GlobalSendToLink: TopDst must be greater than
                    // MinTopDistanceToEnterRoad; the first non-command MD may override 800.
                    int minTopDistancePxV352 = 800;
                    if (roadLinkV352 != null && roadLinkV352.Runtime != null && roadLinkV352.Runtime.Md != null &&
                        roadLinkV352.Runtime.Md.MinTopDistanceToEnterRoad > 0)
                        minTopDistancePxV352 = roadLinkV352.Runtime.Md.MinTopDistanceToEnterRoad;
                    float roadDistancePxV352 = 0.0f;
                    for (int rp = 1; rp < roadCenterPath.Length; rp++)
                        roadDistancePxV352 += Vector2.Distance(roadCenterPath[rp - 1], roadCenterPath[rp]) / 16.0f;
                    if (roadDistancePxV352 <= minTopDistancePxV352)
                    {
                        useRoad = false;
                        roadCenterPath = null;
                        roadAudit += " minTopRejected=" + minTopDistancePxV352.ToString(CultureInfo.InvariantCulture);
                    }
                }

                // V385A: when a real road route exists, do NOT turn it into N independent
                // SubmitPath chains.  Original C2 creates one BrigadeOrder_GoOnRoad and that
                // order owns temporary DestX/DestY for every brigade member until it restores
                // the battlefield WarType at the end.
                List<Vector2> slots;
                string destinationShape = group.Shape;
                if (useRoad)
                {
                    // Final battlefield positions are still computed from the current shape.
                    // The temporary road column is owned only by C2BrigadeOrderGoOnRoadV385A.
                    slots = BuildFormationOrderSlotsAtV359LikeOriginal(
                        group, groupUnits, orderOptionV352,
                        targetCenterX, targetCenterY, moveDirection);
                    OptimizeFormationSlotsForMotionFieldV347LikeOriginal(
                        slots, groupUnits, targetCenterX, targetCenterY);

                    string roadOrderAuditV385A;
                    int roadIssuedV385A = StartBrigadeGoOnRoadV385ALikeOriginal(
                        group,
                        groupUnits,
                        slots,
                        roadCenterPath,
                        targetCenterX,
                        targetCenterY,
                        moveDirection,
                        ordType,
                        cancelSource ?? "BrigadeOrder_GoOnRoad_V385A",
                        out roadOrderAuditV385A);
                    if (roadIssuedV385A > 0)
                    {
                        issued += roadIssuedV385A;
                        roadGroups++;
                        coherentPathGroups++;
                        AfterHumanGlobalSendToV403LikeOriginal(
                            group, cancelSource ?? "Groups.cpp::SendToPositions_road");
                        continue;
                    }

                    // Do not silently substitute a fake road column.  A rejected stateful
                    // road order is logged, then the player command remains valid through
                    // the existing non-road HumanGlobalSendTo path.
                    Debug.LogWarning("[C2:ROAD ORDER V385A REJECT] group=" +
                        group.GroupId.ToString(CultureInfo.InvariantCulture) +
                        " " + roadOrderAuditV385A);
                    blockedPaths++;
                    useRoad = false;
                    roadCenterPath = null;
                }

                CancelBrigadeGoOnRoadV385ALikeOriginal(
                    group.GroupId, "replaced_by_nonroad_move", false);

                if (ordType == 0 && ApplyFormationSymmetricMoveV360LikeOriginal(
                        group, groupUnits, orderOptionV352 != null ? orderOptionV352.OrderTemplate : null,
                        moveDirection))
                    symmetricMoveGroups++;
                slots = BuildFormationOrderSlotsAtV359LikeOriginal(
                    group, groupUnits, orderOptionV352,
                    targetCenterX, targetCenterY, moveDirection);

                // CreateOrderedPositions immediately calls OptimiseBrigadePosition2 in the
                // original. Reject locked order places and grow alternate positions before
                // installing the non-road shared-center order.
                OptimizeFormationSlotsForMotionFieldV347LikeOriginal(
                    slots, groupUnits, targetCenterX, targetCenterY);

                Vector2[] sharedCenterPath = null;
                bool centerDirectClear;
                bool centerPathBuilt = C2BattleTerrainMode.C2BuildingMotionFieldV1TryBuildPathOrDirectRealLikeOriginal(
                    groupCenterX,
                    groupCenterY,
                    targetCenterX,
                    targetCenterY,
                    out sharedCenterPath,
                    out centerDirectClear,
                    4096,
                    "formation_shared_center");
                if (!centerPathBuilt && centerDirectClear)
                    sharedCenterPath = new Vector2[] { new Vector2(targetCenterX, targetCenterY) };
                else if (!centerPathBuilt)
                {
                    blockedPaths++;
                    sharedCenterPath = new Vector2[] { new Vector2(targetCenterX, targetCenterY) };
                }
                bool useSharedCenterPath = sharedCenterPath != null && sharedCenterPath.Length > 0;

                int groupId = group.GroupId;
                string soldierMemberId = group.SoldierMemberId;
                int spacingPercent = group.SpacingPercent;
                int commandPrefixForMoveV385A = ResolveOrderCommandCountV360LikeOriginal(group, groupUnits);
                RegisterFormationInternalV172LikeOriginal(
                    groupUnits, slots, destinationShape, soldierMemberId, groupId, commandPrefixForMoveV385A);
                RuntimeFormationV172LikeOriginal updatedGroup;
                if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out updatedGroup) && updatedGroup != null)
                {
                    updatedGroup.Direction = moveDirection;
                    updatedGroup.SpacingPercent = spacingPercent;
                    updatedGroup.UsesRoadMovement = false;
                }
                for (int i = 0; i < groupUnits.Count; i++)
                {
                    Vector2 s = slots[Mathf.Min(i, slots.Count - 1)];
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = groupUnits[i];
                    if (u == null) continue;
                    bool orderIssuedV352;
                    if (useSharedCenterPath)
                    {
                        Vector2[] memberPathV352 = BuildFormationMemberPathV352LikeOriginal(
                            u, sharedCenterPath,
                            s.x - targetCenterX, s.y - targetCenterY,
                            false, moveDirection);
                        orderIssuedV352 = C2OriginalOrderChainV352.SubmitPath(
                            u, memberPathV352, true, moveDirection, ordType,
                            cancelSource ?? "HumanGlobalSendTo_v352");
                        C2RoadUnitSpeedControllerV352.DetachLikeOriginal(u);
                    }
                    else
                    {
                        C2RoadUnitSpeedControllerV352.DetachLikeOriginal(u);
                        orderIssuedV352 = C2OriginalOrderChainV352.SubmitMove(
                            u, s.x, s.y, true, moveDirection, ordType,
                            cancelSource ?? "HumanLocalSendTo_v352");
                    }
                    if (orderIssuedV352) issued++;
                }
                if (useSharedCenterPath) coherentPathGroups++;
                AfterHumanGlobalSendToV403LikeOriginal(
                    updatedGroup != null ? updatedGroup : group,
                    cancelSource ?? "Groups.cpp::SendToPositions_nonroad");
            }

            if (looseUnitList.Count > 0)
            {
                // Groups.cpp::ExGroupSendSelectedTo handles brigade members first and then
                // sends the remaining selected objects through the same PositionOrder path.
                // Do not use the old Unity fallback slots for a mixed selection.
                string looseAuditV352;
                int looseIssuedV352 = C2GameplayLooseGroupMoveLikeOriginal.IssueMoveLikeOriginal(
                    looseUnitList,
                    destRealCenterX,
                    destRealCenterY,
                    hasFinalFacingDir,
                    finalFacingDir,
                    ordType,
                    cancelSource ?? "mixed_selection_PORD_v352",
                    out looseAuditV352);
                issued += looseIssuedV352;
            }

            if (issued == 0 && blockedKare > 0)
            {
                audit = "blocked_KARE_global_move groups=" + blockedKare.ToString(CultureInfo.InvariantCulture) +
                        " original_OrdUsage=2";
                return true;
            }
            audit = "ok issued=" + issued.ToString(CultureInfo.InvariantCulture) +
                    " groups=" + groups.Count.ToString(CultureInfo.InvariantCulture) +
                    " looseUnits=" + looseUnits.ToString(CultureInfo.InvariantCulture) +
                    " blockedKare=" + blockedKare.ToString(CultureInfo.InvariantCulture) +
                    " roadGroups=" + roadGroups.ToString(CultureInfo.InvariantCulture) +
                    " coherentPathGroups=" + coherentPathGroups.ToString(CultureInfo.InvariantCulture) +
                    " symmetricMoveGroups=" + symmetricMoveGroups.ToString(CultureInfo.InvariantCulture) +
                    " blockedPaths=" + blockedPaths.ToString(CultureInfo.InvariantCulture) +
                    " attackStateMove=" + (preserveAttackStateV405B ? "1" : "0") +
                    " attackMoveMelee=" + attackMoveMeleeV405B.ToString(CultureInfo.InvariantCulture) +
                    "/" + attackMoveTotalV405B.ToString(CultureInfo.InvariantCulture) +
                    " attackMoveFar=" + (attackMoveFarV405B ? "1" : "0") +
                    " ordType=" + ordType.ToString(CultureInfo.InvariantCulture) +
                    " roadOrderV385A=" + (roadGroups > 0 ? "stateful_brigade" : "none") +
                    " mode=C2_HumanGlobalSendTo_order_chain_v352";
            return issued > 0;
        }

        private static Vector2[] BuildFormationMemberPathV352LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2[] centerPath,
            float offsetX,
            float offsetY,
            bool rotateOffsetAlongRoad,
            byte finalDirection)
        {
            if (centerPath == null || centerPath.Length == 0)
                return new Vector2[0];
            int extraFinalSlot = rotateOffsetAlongRoad ? 1 : 0;
            Vector2[] result = new Vector2[centerPath.Length + extraFinalSlot];
            for (int i = 0; i < centerPath.Length; i++)
            {
                float pathOffsetX = offsetX;
                float pathOffsetY = offsetY;
                if (rotateOffsetAlongRoad)
                {
                    // COSSACKS2 BrigadeOrder_GoOnRoad advances the column against the
                    // current road segment, not against one world-space offset. Rotate
                    // each member's march place by the local road tangent. The final
                    // point below restores the requested battlefield facing.
                    Vector2 previous = i > 0 ? centerPath[i - 1] : centerPath[i];
                    Vector2 next = i + 1 < centerPath.Length ? centerPath[i + 1] : centerPath[i];
                    byte tangentDirection = DirectionFromDeltaV320LikeOriginal(
                        next.x - previous.x,
                        next.y - previous.y,
                        finalDirection);
                    byte deltaDirection = (byte)(tangentDirection - finalDirection);
                    int cos = C2OriginalMovementMathV352.TCos[deltaDirection];
                    int sin = C2OriginalMovementMathV352.TSin[deltaDirection];
                    pathOffsetX = (offsetX * cos - offsetY * sin) / 256.0f;
                    pathOffsetY = (offsetX * sin + offsetY * cos) / 256.0f;
                }
                result[i] = new Vector2(centerPath[i].x + pathOffsetX, centerPath[i].y + pathOffsetY);
            }
            if (rotateOffsetAlongRoad)
            {
                Vector2 finalCenter = centerPath[centerPath.Length - 1];
                result[result.Length - 1] = new Vector2(finalCenter.x + offsetX, finalCenter.y + offsetY);
            }
            return result;
        }

        private static void ComputeGroupsCenterV172LikeOriginal(
            IList<RuntimeFormationV172LikeOriginal> groups,
            out float centerX,
            out float centerY)
        {
            centerX = 0.0f;
            centerY = 0.0f;
            int n = 0;
            if (groups != null)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    RuntimeFormationV172LikeOriginal group = groups[i];
                    if (group == null) continue;

                    float gx;
                    float gy;
                    ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out gx, out gy);
                    centerX += gx;
                    centerY += gy;
                    n++;
                }
            }

            if (n > 0)
            {
                centerX /= n;
                centerY /= n;
            }
        }

        private static byte ComputeGroupsAverageDirectionV346LikeOriginal(
            IList<RuntimeFormationV172LikeOriginal> groups)
        {
            // Groups.cpp::BrigadesList::CreateFromSelected:
            // ADX += TCos[Direction]>>2; ADY += TSin[Direction]>>2;
            // AverageDir=(GetDir(ADX,ADY)+8)&0xF0.
            int adx = 0;
            int ady = 0;
            if (groups != null)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    RuntimeFormationV172LikeOriginal group = groups[i];
                    if (group == null) continue;
                    adx += C2OriginalMovementMathV352.TCos[group.Direction] >> 2;
                    ady += C2OriginalMovementMathV352.TSin[group.Direction] >> 2;
                }
            }
            return C2OriginalMovementMathV352.Quantize16(
                C2OriginalMovementMathV352.GetDir(adx, ady));
        }

        public static bool TryBuildSelectedFormationPreviewV346LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> selectedUnits,
            float destinationRealX,
            float destinationRealY,
            bool hasFacing,
            byte destinationAverageDirection,
            out List<Vector2> transformedSoldierSlots,
            out string audit)
        {
            transformedSoldierSlots = new List<Vector2>(256);
            audit = "no_selected_formations";
            if (selectedUnits == null || selectedUnits.Count == 0) return false;

            var groups = new List<RuntimeFormationV172LikeOriginal>(8);
            var seen = new HashSet<int>();
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                RuntimeFormationV172LikeOriginal group;
                if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selectedUnits[i], out group) || group == null)
                    continue;
                if (seen.Add(group.GroupId)) groups.Add(group);
            }
            if (groups.Count == 0) return false;

            float groupsCenterX;
            float groupsCenterY;
            ComputeGroupsCenterV172LikeOriginal(groups, out groupsCenterX, out groupsCenterY);
            byte averageDirection = ComputeGroupsAverageDirectionV346LikeOriginal(groups);
            int delta = hasFacing ? (sbyte)(destinationAverageDirection - averageDirection) : 0;
            int previewGroups = 0;

            for (int g = 0; g < groups.Count; g++)
            {
                RuntimeFormationV172LikeOriginal group = groups[g];
                if (group == null) continue;
                var record = ResolveRecordForGroupV320LikeOriginal(group);
                var option = FindFormationOptionV320LikeOriginal(record, group.Shape);
                int usage = option != null && option.OrderTemplate != null
                    ? option.OrderTemplate.Usage
                    : (string.Equals(group.Shape, "KARE", StringComparison.OrdinalIgnoreCase) ? 2 : 0);
                if (usage == 2) continue;

                List<C2NeutralPeasantUnitInfoV2LikeOriginal> units = GetFormationOrderMembersV360LikeOriginal(group);
                if (units.Count == 0) continue;
                float groupCenterX, groupCenterY;
                ComputeFormationGroupCenterV172LikeOriginal(group, units, out groupCenterX, out groupCenterY);
                float targetGroupX, targetGroupY;
                byte targetDirection;
                ComputeFormationCommandTargetV359LikeOriginal(
                    groupCenterX, groupCenterY, groupsCenterX, groupsCenterY,
                    destinationRealX, destinationRealY, group.Direction, delta,
                    out targetGroupX, out targetGroupY, out targetDirection);
                List<Vector2> slots = BuildFormationSoldierPreviewAtV359LikeOriginal(
                    group, units, option, targetGroupX, targetGroupY, targetDirection);
                transformedSoldierSlots.AddRange(slots);
                previewGroups++;
            }

            audit = "groups=" + groups.Count.ToString(CultureInfo.InvariantCulture) +
                    " previewGroups=" + previewGroups.ToString(CultureInfo.InvariantCulture) +
                    " soldierSlots=" + transformedSoldierSlots.Count.ToString(CultureInfo.InvariantCulture) +
                    " averageDirection=" + averageDirection.ToString(CultureInfo.InvariantCulture) +
                    " delta=" + delta.ToString(CultureInfo.InvariantCulture) +
                    " geometry=destination_integer_v359" +
                    " source=Groups.cpp:BrigadesList_CreateFromSelected_Transform_ShowPositions";
            return transformedSoldierSlots.Count > 0;
        }

        private static void ComputeFormationGroupCenterV172LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> fallbackUnits,
            out float centerX,
            out float centerY)
        {
            centerX = 0.0f;
            centerY = 0.0f;
            int n = 0;

            // Brigade::GetCenter reads the live member coordinates.  Slots are
            // brigade destinations (posX/posY), not its current centre.  Using
            // them here kept an already moved brigade anchored to its previous
            // command: the cyan preview and the next order then appeared at a
            // different, often lower, formation.
            if (fallbackUnits != null)
            {
                for (int i = 0; i < fallbackUnits.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = fallbackUnits[i];
                    if (!IsUsableFormationUnitV172LikeOriginal(u, true)) continue;
                    centerX += u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                    centerY += u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                    n++;
                }
            }

            // A just registered editor formation can be queried before all
            // runtime links are live.  Keep the registered positions only as
            // that narrow fallback; they must never override real members.
            if (n == 0 && group != null && group.Slots != null)
            {
                for (int i = 0; i < group.Slots.Count; i++)
                {
                    centerX += group.Slots[i].x;
                    centerY += group.Slots[i].y;
                    n++;
                }
            }

            if (n > 0)
            {
                centerX /= n;
                centerY /= n;
            }
        }

        private static void OptimizeFormationSlotsForMotionFieldV347LikeOriginal(
            List<Vector2> slots,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            float centerRealX,
            float centerRealY)
        {
            if (slots == null || slots.Count == 0)
                return;

            // Original formation coordinates are spaced in original pixels;
            // the runtime stores RealX/RealY (x16). A 16 px search quantum is
            // one motion-field point, while the sixteen rings match
            // OptimiseBrigadePosition2::NIter<16.
            const float motionCellReal = 16.0f * 16.0f;
            const int maxRings = 16;
            var occupied = new HashSet<long>();
            for (int i = 0; i < slots.Count; i++)
            {
                int radiusCells = 1;
                if (units != null && i < units.Count && units[i] != null)
                    radiusCells = 1;

                Vector2 desired = slots[i];
                Vector2 chosen = desired;
                bool found = IsFreeUniqueFormationSlotV347LikeOriginal(
                    desired.x, desired.y, radiusCells, occupied);
                if (!found)
                {
                    float best = float.MaxValue;
                    // Grow positions around the connected part, preferring the
                    // point closest to the requested formation centre. This is
                    // the managed equivalent of AddFirstLine/AddLastLine/
                    // AddSidePos followed by closest free State==2 selection.
                    for (int ring = 1; ring <= maxRings; ring++)
                    {
                        for (int side = -ring; side <= ring; side++)
                        {
                            TryFormationSlotCandidateV347LikeOriginal(
                                desired.x + side * motionCellReal,
                                desired.y - ring * motionCellReal,
                                centerRealX, centerRealY, radiusCells,
                                occupied, ref found, ref best, ref chosen);
                            TryFormationSlotCandidateV347LikeOriginal(
                                desired.x + side * motionCellReal,
                                desired.y + ring * motionCellReal,
                                centerRealX, centerRealY, radiusCells,
                                occupied, ref found, ref best, ref chosen);
                        }
                        for (int side = -ring + 1; side < ring; side++)
                        {
                            TryFormationSlotCandidateV347LikeOriginal(
                                desired.x - ring * motionCellReal,
                                desired.y + side * motionCellReal,
                                centerRealX, centerRealY, radiusCells,
                                occupied, ref found, ref best, ref chosen);
                            TryFormationSlotCandidateV347LikeOriginal(
                                desired.x + ring * motionCellReal,
                                desired.y + side * motionCellReal,
                                centerRealX, centerRealY, radiusCells,
                                occupied, ref found, ref best, ref chosen);
                        }
                        if (found) break;
                    }
                }

                if (found)
                {
                    slots[i] = chosen;
                    occupied.Add(FormationSlotKeyV347LikeOriginal(chosen.x, chosen.y));
                }
                else if (units != null && i < units.Count && units[i] != null)
                {
                    // OptimiseBrigadePosition2 falls back to the live member
                    // coordinate when it cannot create any connected free
                    // positions. Never preserve a target inside a building.
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[i];
                    slots[i] = new Vector2(
                        unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX,
                        unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY);
                }
            }
        }

        private static void TryFormationSlotCandidateV347LikeOriginal(
            float x,
            float y,
            float centerX,
            float centerY,
            int radiusCells,
            HashSet<long> occupied,
            ref bool found,
            ref float best,
            ref Vector2 chosen)
        {
            if (!IsFreeUniqueFormationSlotV347LikeOriginal(x, y, radiusCells, occupied))
                return;
            float dx = x - centerX;
            float dy = y - centerY;
            float score = dx * dx + dy * dy;
            if (found && score >= best) return;
            found = true;
            best = score;
            chosen = new Vector2(x, y);
        }

        private static bool IsFreeUniqueFormationSlotV347LikeOriginal(
            float realX,
            float realY,
            int radiusCells,
            HashSet<long> occupied)
        {
            if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
                    realX, realY, Mathf.Max(1, radiusCells)))
                return false;
            return occupied == null || !occupied.Contains(FormationSlotKeyV347LikeOriginal(realX, realY));
        }

        private static long FormationSlotKeyV347LikeOriginal(float realX, float realY)
        {
            // Slot identity uses integer Real coordinates (1/16 original pixel).
            // Different places may share a 16 px motion-field cell, especially
            // in diagonal ranks. Building passability is checked separately.
            int x = Mathf.RoundToInt(realX);
            int y = Mathf.RoundToInt(realY);
            return ((long)x << 32) ^ (uint)y;
        }

        public static bool TryRotateSelectedFormationsV267LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            byte targetDirection,
            string cancelSource,
            out int issued,
            out string audit)
        {
            issued = 0;
            audit = "no_selected_formation";
            if (sourceUnits == null || sourceUnits.Count == 0) return false;

            List<RuntimeFormationV172LikeOriginal> groups = new List<RuntimeFormationV172LikeOriginal>(4);
            HashSet<int> seenGroups = new HashSet<int>();
            for (int i = 0; i < sourceUnits.Count; i++)
            {
                RuntimeFormationV172LikeOriginal group;
                if (!TryGetRuntimeGroupByUnitV172LikeOriginal(sourceUnits[i], out group) || group == null) continue;
                if (seenGroups.Add(group.GroupId)) groups.Add(group);
            }
            if (groups.Count == 0) return false;

            for (int g = 0; g < groups.Count; g++)
            {
                RuntimeFormationV172LikeOriginal group = groups[g];
                if (group == null) continue;
                CancelBrigadeGoOnRoadV385ALikeOriginal(
                    group.GroupId, cancelSource ?? "formation_rotate", false);
                BeginBrigadeOrderV403LikeOriginal(
                    group, false, cancelSource ?? "Multi.cpp::ComRotateBrigade");
                group.TurnTargetDirection = targetDirection;
                // Re-evaluate physical orientation even while an earlier turn is moving.
                group.TurnActive = true;
                group.NextTurnStepAt = 0.0f;
                group.TurnStepDeadlineAt = 0.0f;
                issued += StepFormationTurnV334LikeOriginal(group, cancelSource ?? "formation_rotate");
            }

            audit = "ok issued=" + issued.ToString(CultureInfo.InvariantCulture) +
                    " groups=" + groups.Count.ToString(CultureInfo.InvariantCulture) +
                    " direction=" + targetDirection.ToString(CultureInfo.InvariantCulture) +
                    " mode=formation_turn_symmetry_v360 keepPositions=partial";
            return issued > 0;
        }

        public static void TickFormationTurnsV334LikeOriginal()
        {
            if (_groupsByIdV172LikeOriginal.Count == 0) return;
            float now = Time.realtimeSinceStartup;
            foreach (KeyValuePair<int, RuntimeFormationV172LikeOriginal> pair in _groupsByIdV172LikeOriginal)
            {
                RuntimeFormationV172LikeOriginal group = pair.Value;
                if (group == null || !group.TurnActive || now < group.NextTurnStepAt) continue;
                // BrigadeOrder_KeepPositions waits for the current +/-32 geometry
                // step before CreateOrderedPositions advances to the next one.
                // Reissuing a fresh destination every 0.10 s made ranks cross and
                // visually collapse during a turn in the Unity transition layer.
                if (now < group.TurnStepDeadlineAt && !AreFormationTurnSlotsSettledV358LikeOriginal(group))
                {
                    group.NextTurnStepAt = now + 0.04f;
                    continue;
                }
                StepFormationTurnV334LikeOriginal(group, "formation_rotate_keep_positions");
            }
        }

        public static void TickFormationKeepPositionsSpeedV358LikeOriginal()
        {
            // V385A executes the brigade-owned road order from the same central
            // simulation phase before ordinary KeepPositions speed equalisation.
            TickBrigadeGoOnRoadV385ALikeOriginal();
            // City::ExecuteBrigades stand-ground timer runs on the same 40 ms
            // CII simulation quantum as the managed unit runtime.
            TickBrigadeStandGroundV403LikeOriginal();
            // V406 COSSACKS2 BrigadeOrder_Bitva runs from the same brigade process
            // cadence and owns melee EnemyID assignment / counter-attack service.
            TickBrigadeBattleV406LikeOriginal();

            // Direct port of COSSACKS2 BrigadeOrder_KeepPositions::Process speed
            // equalisation. Independent full-speed member paths stretch the front
            // rank and make following ranks cut through it when the formation turns.
            foreach (KeyValuePair<int, RuntimeFormationV172LikeOriginal> pair in _groupsByIdV172LikeOriginal)
            {
                RuntimeFormationV172LikeOriginal group = pair.Value;
                if (group == null) continue;
                int count = Mathf.Min(group.Units.Count, group.Slots.Count);
                if (count <= 0) continue;

                bool anyMoving = false;
                int live = 0;
                int minDistancePx = int.MaxValue;
                long currentCenterX = 0;
                long currentCenterY = 0;
                long destinationCenterX = 0;
                long destinationCenterY = 0;
                for (int i = 0; i < count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                    if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal) continue;
                    C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    if (runtime == null) continue;
                    anyMoving |= runtime.HasMoveTargetLikeOriginal;
                    int ux = Mathf.RoundToInt((unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX) / 16.0f);
                    int uy = Mathf.RoundToInt((unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY) / 16.0f);
                    int sx = Mathf.RoundToInt(group.Slots[i].x / 16.0f);
                    int sy = Mathf.RoundToInt(group.Slots[i].y / 16.0f);
                    int distance = C2OriginalMovementMathV352.Norma(ux - sx, uy - sy);
                    if (distance < minDistancePx) minDistancePx = distance;
                    currentCenterX += ux;
                    currentCenterY += uy;
                    destinationCenterX += sx;
                    destinationCenterY += sy;
                    live++;
                }

                if (!anyMoving)
                {
                    // A GoOnRoad order remains authoritative while members are in a
                    // transition/stand frame between dynamic DestX/DestY updates.
                    if (group.UsesRoadMovement) continue;
                    SetFormationUnitSpeedV358LikeOriginal(group, count, 64);
                    continue;
                }
                if (group.UsesRoadMovement || live <= 0) continue;

                int centerDistancePx = C2OriginalMovementMathV352.Norma(
                    (int)(currentCenterX / live - destinationCenterX / live),
                    (int)(currentCenterY / live - destinationCenterY / live));
                if (minDistancePx == int.MaxValue) minDistancePx = 0;

                long normalizedDistanceSum = 0;
                int normalizedCount = 0;
                for (int i = 0; i < count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                    if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal) continue;
                    C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    if (runtime == null || runtime.Md == null) continue;
                    int ux = Mathf.RoundToInt((unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX) / 16.0f);
                    int uy = Mathf.RoundToInt((unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY) / 16.0f);
                    int sx = Mathf.RoundToInt(group.Slots[i].x / 16.0f);
                    int sy = Mathf.RoundToInt(group.Slots[i].y / 16.0f);
                    int distance = C2OriginalMovementMathV352.Norma(ux - sx, uy - sy);
                    if (centerDistancePx > 200) distance -= minDistancePx / 2;
                    int motionDistance = Mathf.Max(1,
                        runtime.OriginalMotionDistLikeOriginal > 0
                            ? runtime.OriginalMotionDistLikeOriginal
                            : runtime.Md.MotionDist);
                    distance = distance * 48 / motionDistance;
                    normalizedDistanceSum += distance;
                    normalizedCount++;
                }
                if (normalizedCount <= 0) continue;
                int averageNormalizedDistance = (int)(normalizedDistanceSum / normalizedCount);
                if (averageNormalizedDistance <= 0) continue;

                int averageSpeed = 0;
                int speedCount = 0;
                for (int i = 0; i < count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                    if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal) continue;
                    C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                    if (runtime == null || runtime.Md == null) continue;
                    int ux = Mathf.RoundToInt((unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX) / 16.0f);
                    int uy = Mathf.RoundToInt((unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY) / 16.0f);
                    int sx = Mathf.RoundToInt(group.Slots[i].x / 16.0f);
                    int sy = Mathf.RoundToInt(group.Slots[i].y / 16.0f);
                    int distance = C2OriginalMovementMathV352.Norma(ux - sx, uy - sy);
                    if (centerDistancePx > 200) distance -= minDistancePx / 2;
                    int motionDistance = Mathf.Max(1,
                        runtime.OriginalMotionDistLikeOriginal > 0
                            ? runtime.OriginalMotionDistLikeOriginal
                            : runtime.Md.MotionDist);
                    int normalizedDistance = distance * 48 / motionDistance;
                    int speed = Mathf.Clamp(normalizedDistance * 64 / averageNormalizedDistance, 10, 128);
                    runtime.OriginalUnitSpeedLikeOriginal = speed;
                    averageSpeed += speed;
                    speedCount++;
                }

                if (speedCount > 0 && averageSpeed / speedCount < 48)
                {
                    averageSpeed /= speedCount;
                    int boostPercent = 100 * (64 - averageSpeed) / 64;
                    for (int i = 0; i < count; i++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                        C2UnitOriginalRuntimeLinkLikeOriginal link = unit != null ? unit.RuntimeLinkCachedLikeOriginal : null;
                        C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                        if (runtime != null)
                            runtime.OriginalUnitSpeedLikeOriginal =
                                runtime.OriginalUnitSpeedLikeOriginal * (100 + boostPercent) / 100;
                    }
                }
            }
        }

        private static void SetFormationUnitSpeedV358LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            int count,
            int speed)
        {
            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit != null ? unit.RuntimeLinkCachedLikeOriginal : null;
                C2UnitOriginalRuntime runtime = link != null ? link.Runtime : null;
                if (runtime != null) runtime.OriginalUnitSpeedLikeOriginal = speed;
            }
        }

        private static bool AreFormationTurnSlotsSettledV358LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return true;
            int count = Mathf.Min(group.Units.Count, group.Slots.Count);
            const int originalKeepPositionToleranceReal = 6 * 16;
            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal) continue;
                float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
                float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
                Vector2 slot = group.Slots[i];
                if (C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt(ux - slot.x),
                        Mathf.RoundToInt(uy - slot.y)) > originalKeepPositionToleranceReal)
                    return false;
            }
            return true;
        }

        private static int StepFormationTurnV334LikeOriginal(RuntimeFormationV172LikeOriginal group, string source)
        {
            if (group == null || !group.TurnActive) return 0;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units = GetFormationOrderMembersV360LikeOriginal(group);
            if (!units.Exists(u => u != null)) { group.TurnActive = false; return 0; }
            int commandPrefix = ResolveOrderCommandCountV360LikeOriginal(group, units);
            float centerX, centerY;
            ComputeFormationGroupCenterV172LikeOriginal(group, units, out centerX, out centerY);
            var record = ResolveRecordForGroupV320LikeOriginal(group);
            var option = FindFormationOptionV320LikeOriginal(record, group.Shape);
            var template = option != null ? option.OrderTemplate : null;
            byte nextDirection;
            if (template != null &&
                units.Count - commandPrefix <= template.UnitCount)
            {
                byte physicalDirection = GetFormationPhysicalDirectionV360LikeOriginal(template, units, commandPrefix);
                int[] swap = C2FormationSymmetryLikeOriginal.SelectTurnSwap(
                    template, group.Direction, physicalDirection, group.TurnTargetDirection, out nextDirection);
                ApplyFormationTurnSwapV360LikeOriginal(units, commandPrefix, swap);
            }
            else
            {
                // Imported groups without a valid order cannot use symmetry tables.
                int delta = unchecked((sbyte)(group.TurnTargetDirection - group.Direction));
                nextDirection = unchecked((byte)(group.Direction + Mathf.Clamp(delta, -32, 32)));
            }
            List<Vector2> slots = BuildFormationOrderSlotsAtV359LikeOriginal(
                group, units, option, centerX, centerY, nextDirection);
            OptimizeFormationSlotsForMotionFieldV347LikeOriginal(slots, units, centerX, centerY);
            group.Units.Clear();
            group.Units.AddRange(units);
            group.CommandSlotCount = commandPrefix;
            group.Slots.Clear();
            group.Slots.AddRange(slots);
            group.Direction = nextDirection;
            group.TurnActive = nextDirection != group.TurnTargetDirection;
            group.NextTurnStepAt = Time.realtimeSinceStartup + 0.04f;
            // Completion timing is still the existing bridge; this patch ports
            // member assignment, not all animation/order rules of KeepPositions.
            group.TurnStepDeadlineAt = Time.realtimeSinceStartup + 1.20f;
            group.UsesRoadMovement = false;
            int issued = 0;
            for (int i = 0; i < units.Count && i < slots.Count; i++)
            {
                if (units[i] == null) continue;
                C2RoadUnitSpeedControllerV352.DetachLikeOriginal(units[i]);
                units[i].SetFormationAssemblyDestinationRealLikeOriginal(
                    slots[i].x, slots[i].y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    nextDirection);
                issued++;
            }
            return issued;
        }

        private static List<Vector2> BuildTranslatedGroupSlotsV172LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            float targetCenterX,
            float targetCenterY)
        {
            int count = units != null ? units.Count : 0;
            if (count <= 0) return new List<Vector2>(0);

            if (group != null && group.Slots != null && group.Slots.Count >= count &&
                !AreFormationSlotsCollapsedV172LikeOriginal(group.Slots, count))
            {
                float oldCenterX;
                float oldCenterY;
                ComputeFormationGroupCenterV172LikeOriginal(group, units, out oldCenterX, out oldCenterY);
                float dx = targetCenterX - oldCenterX;
                float dy = targetCenterY - oldCenterY;

                List<Vector2> translated = new List<Vector2>(count);
                for (int i = 0; i < count; i++)
                    translated.Add(new Vector2(group.Slots[i].x + dx, group.Slots[i].y + dy));
                return translated;
            }

            return BuildFallbackSlotsV172LikeOriginal(count, targetCenterX, targetCenterY);
        }

        public static bool TryCreateBrigInZoneV172LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selectedUnit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
            int amountIndex,
            int requiredAmount,
            C2SettlementBuildingSelectableV1LikeOriginal commandCenter,
            out int groupId,
            out string audit)
        {
            groupId = -1;
            audit = "not_started";
            if (selectedUnit == null)
            {
                audit = "no_selected_unit";
                return false;
            }
            if (record == null)
            {
                audit = "no_formation_record";
                return false;
            }
            // Original cvi_BrigCreate creates a brigade around the selected officer and nearby units.
            // A command center is not part of this path; keep it optional for audit compatibility.

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                SelectAmountOptionV172LikeOriginal(record, amountIndex, requiredAmount);
            if (option == null)
            {
                audit = "no_amount_option amountIndex=" + amountIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            int required = Mathf.Max(1, requiredAmount > 0 ? requiredAmount : option.UnitCount);
            // The selected object may be an officer/command unit; the NDS record defines
            // which soldier type fills the 120/45/15 formation.
            string soldierMember = record.UnitId ?? string.Empty;
            if (string.IsNullOrEmpty(soldierMember))
                soldierMember = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(selectedUnit);

            float centerRealX = selectedUnit.RealXFloat != 0.0f ? selectedUnit.RealXFloat : selectedUnit.RealX;
            float centerRealY = selectedUnit.RealYFloat != 0.0f ? selectedUnit.RealYFloat : selectedUnit.RealY;
            float radiusReal = 800.0f * 16.0f;
            float radius2 = radiusReal * radiusReal;

            bool creatorIsAlsoSoldier =
                string.Equals(record.OfficerId ?? string.Empty, soldierMember, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(record.DrummerId) &&
                string.IsNullOrEmpty(record.FlagId);

            C2NeutralPeasantUnitInfoV2LikeOriginal officer = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal drummer = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal flag = null;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(required);

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (!IsUsableFormationUnitV172LikeOriginal(u, false)) continue;
                if (u.Nation != selectedUnit.Nation) continue;

                float ux = u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                float uy = u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                float dx = ux - centerRealX;
                float dy = uy - centerRealY;
                if (dx * dx + dy * dy > radius2) continue;

                if (!creatorIsAlsoSoldier && officer == null && MatchesFormationMemberV172LikeOriginal(u, record.OfficerId))
                {
                    officer = u;
                    continue;
                }
                if (drummer == null && MatchesFormationMemberV172LikeOriginal(u, record.DrummerId))
                {
                    drummer = u;
                    continue;
                }
                if (flag == null && MatchesFormationMemberV172LikeOriginal(u, record.FlagId))
                {
                    flag = u;
                    continue;
                }
                if (soldiers.Count < required && MatchesFormationMemberV172LikeOriginal(u, soldierMember))
                    soldiers.Add(u);
            }

            if (soldiers.Count < required)
            {
                audit = "not_enough_soldiers required=" + required.ToString(CultureInfo.InvariantCulture) +
                        " found=" + soldiers.Count.ToString(CultureInfo.InvariantCulture) +
                        " soldier='" + soldierMember + "'" +
                        " commandcenter='" + (commandCenter != null ? (commandCenter.SourceMonsterId ?? string.Empty) : "optional_none") + "'";
                return false;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> groupUnits = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(required + 3);
            AddUniqueUnitV172LikeOriginal(groupUnits, officer);
            AddUniqueUnitV172LikeOriginal(groupUnits, drummer);
            AddUniqueUnitV172LikeOriginal(groupUnits, flag);
            int commandUnitCount = groupUnits.Count;
            for (int i = 0; i < soldiers.Count; i++) AddUniqueUnitV172LikeOriginal(groupUnits, soldiers[i]);

            byte formationDirectionV375 = groupUnits[0].RealDir;
            List<Vector2> slots = BuildTemplateSlotsDirectedV352LikeOriginal(option, groupUnits.Count, commandUnitCount, groupUnits, centerRealX, centerRealY, 100, formationDirectionV375);
            ReorderSoldiersForNearestSlotsV172LikeOriginal(groupUnits, slots, commandUnitCount);
            groupId = RegisterFormationInternalV172LikeOriginal(groupUnits, slots, option.Shape, soldierMember, -1, commandUnitCount);
            _groupsByIdV172LikeOriginal[groupId].Direction = formationDirectionV375;
            for (int i = 0; i < groupUnits.Count; i++)
            {
                Vector2 s = slots[Mathf.Min(i, slots.Count - 1)];
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(groupUnits[i], "create_brig_in_zone_v172");
                groupUnits[i].SetFormationAssemblyDestinationRealLikeOriginal(
                    s.x,
                    s.y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    formationDirectionV375);
            }

            audit = "ok groupId=" + groupId.ToString(CultureInfo.InvariantCulture) +
                    " shape='" + (option.Shape ?? string.Empty) + "'" +
                    " amountIndex=" + amountIndex.ToString(CultureInfo.InvariantCulture) +
                    " required=" + required.ToString(CultureInfo.InvariantCulture) +
                    " soldiers=" + soldiers.Count.ToString(CultureInfo.InvariantCulture) +
                    " officer='" + UnitLabelV172LikeOriginal(officer) + "'" +
                    " drummer='" + UnitLabelV172LikeOriginal(drummer) + "'" +
                    " flag='" + UnitLabelV172LikeOriginal(flag) + "'" +
                    " commandcenter='" + (commandCenter != null ? (commandCenter.SourceMonsterId ?? string.Empty) : "optional_none") + "'" +
                    " mode=CreateBrigInZone_stage2_runtime";
            return true;
        }

        public static bool TryCreateGlobalBrigFromCommandCenterV172LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal commandCenter,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            out int groupId,
            out string audit)
        {
            groupId = -1;
            audit = "not_started";
            if (commandCenter == null)
            {
                audit = "no_commandcenter";
                return false;
            }
            if (record == null)
            {
                audit = "no_formation_record";
                return false;
            }
            if (option == null)
            {
                option = SelectAmountOptionV172LikeOriginal(record, 0, 0);
                if (option == null)
                {
                    audit = "no_amount_option";
                    return false;
                }
            }

            int required = Mathf.Max(1, option.UnitCount);
            string soldierMember = record.UnitId ?? string.Empty;
            float centerRealX = commandCenter.RealX;
            float centerRealY = commandCenter.RealY;
            const float radiusReal = 1500.0f * 16.0f;
            const float radius2 = radiusReal * radiusReal;

            C2NeutralPeasantUnitInfoV2LikeOriginal officer = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal drummer = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal flag = null;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(required);

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (!IsUsableFormationUnitV172LikeOriginal(u, false)) continue;
                if (u.Nation != commandCenter.Nation) continue;

                float ux = u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                float uy = u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                float dx = ux - centerRealX;
                float dy = uy - centerRealY;
                if (dx * dx + dy * dy > radius2) continue;

                if (officer == null && MatchesFormationMemberV172LikeOriginal(u, record.OfficerId))
                {
                    officer = u;
                    continue;
                }
                if (drummer == null && MatchesFormationMemberV172LikeOriginal(u, record.DrummerId))
                {
                    drummer = u;
                    continue;
                }
                if (flag == null && MatchesFormationMemberV172LikeOriginal(u, record.FlagId))
                {
                    flag = u;
                    continue;
                }
                if (soldiers.Count < required && MatchesFormationMemberV172LikeOriginal(u, soldierMember))
                    soldiers.Add(u);
            }

            if (soldiers.Count < required)
            {
                audit = "not_enough_soldiers required=" + required.ToString(CultureInfo.InvariantCulture) +
                        " found=" + soldiers.Count.ToString(CultureInfo.InvariantCulture) +
                        " soldier='" + soldierMember + "'" +
                        " commandcenter='" + (commandCenter.SourceMonsterId ?? string.Empty) + "'" +
                        " radius=1500 mode=GetGlobalCreateBrigList_COMMANDCENTER";
                return false;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> groupUnits = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(required + 3);
            AddUniqueUnitV172LikeOriginal(groupUnits, officer);
            AddUniqueUnitV172LikeOriginal(groupUnits, drummer);
            AddUniqueUnitV172LikeOriginal(groupUnits, flag);
            int commandUnitCount = groupUnits.Count;
            for (int i = 0; i < soldiers.Count; i++) AddUniqueUnitV172LikeOriginal(groupUnits, soldiers[i]);

            float avgX = 0.0f;
            float avgY = 0.0f;
            for (int i = 0; i < groupUnits.Count; i++)
            {
                avgX += groupUnits[i].RealXFloat != 0.0f ? groupUnits[i].RealXFloat : groupUnits[i].RealX;
                avgY += groupUnits[i].RealYFloat != 0.0f ? groupUnits[i].RealYFloat : groupUnits[i].RealY;
            }
            if (groupUnits.Count > 0)
            {
                avgX /= groupUnits.Count;
                avgY /= groupUnits.Count;
            }
            else
            {
                avgX = centerRealX;
                avgY = centerRealY;
            }

            float formationCenterX = avgX;
            float formationCenterY = avgY;
            int rallyX;
            int rallyY;
            if (commandCenter.TryGetRallyPointRealV155LikeOriginal(out rallyX, out rallyY))
            {
                formationCenterX = rallyX;
                formationCenterY = rallyY;
            }

            byte formationDirectionV375 = groupUnits[0].RealDir;
            List<Vector2> slots = BuildTemplateSlotsDirectedV352LikeOriginal(option, groupUnits.Count, commandUnitCount, groupUnits, formationCenterX, formationCenterY, 100, formationDirectionV375);
            ReorderSoldiersForNearestSlotsV172LikeOriginal(groupUnits, slots, commandUnitCount);
            groupId = RegisterFormationInternalV172LikeOriginal(groupUnits, slots, option.Shape, soldierMember, -1, commandUnitCount);
            _groupsByIdV172LikeOriginal[groupId].Direction = formationDirectionV375;
            for (int i = 0; i < groupUnits.Count; i++)
            {
                Vector2 s = slots[Mathf.Min(i, slots.Count - 1)];
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(groupUnits[i], "create_global_brig_commandcenter_v172");
                groupUnits[i].SetFormationAssemblyDestinationRealLikeOriginal(
                    s.x,
                    s.y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    formationDirectionV375);
            }

            audit = "ok groupId=" + groupId.ToString(CultureInfo.InvariantCulture) +
                    " shape='" + (option.Shape ?? string.Empty) + "'" +
                    " required=" + required.ToString(CultureInfo.InvariantCulture) +
                    " soldiers=" + soldiers.Count.ToString(CultureInfo.InvariantCulture) +
                    " slots=" + BuildSlotAuditV321LikeOriginal(slots, groupUnits.Count) +
                    " officer='" + UnitLabelV172LikeOriginal(officer) + "'" +
                    " drummer='" + UnitLabelV172LikeOriginal(drummer) + "'" +
                    " flag='" + UnitLabelV172LikeOriginal(flag) + "'" +
                    " commandcenter='" + (commandCenter.SourceMonsterId ?? string.Empty) + "'" +
                    " radius=1500 mode=GetGlobalCreateBrigList_COMMANDCENTER_stage2_runtime";
            return true;
        }

        private static string BuildSlotAuditV321LikeOriginal(IList<Vector2> slots, int count)
        {
            if (slots == null || slots.Count == 0 || count <= 0)
                return "none ";

            int n = Mathf.Min(count, slots.Count);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float minPair = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = slots[i];
                minX = Mathf.Min(minX, a.x);
                minY = Mathf.Min(minY, a.y);
                maxX = Mathf.Max(maxX, a.x);
                maxY = Mathf.Max(maxY, a.y);
                for (int j = i + 1; j < n; j++)
                    minPair = Mathf.Min(minPair, Vector2.Distance(a, slots[j]));
            }

            if (minPair == float.MaxValue) minPair = 0.0f;
            return n.ToString(CultureInfo.InvariantCulture) +
                   " span=(" + (maxX - minX).ToString("0", CultureInfo.InvariantCulture) +
                   "," + (maxY - minY).ToString("0", CultureInfo.InvariantCulture) + ")" +
                   " minPair=" + minPair.ToString("0", CultureInfo.InvariantCulture) + " ";
        }

        private static bool TryGetRuntimeGroupByUnitV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, out RuntimeFormationV172LikeOriginal group)
        {
            group = null;
            if (unit == null) return false;
            int gid;
            if (!_groupIdByUnitInstanceV172LikeOriginal.TryGetValue(unit.GetInstanceID(), out gid)) return false;
            if (!_groupsByIdV172LikeOriginal.TryGetValue(gid, out group) || group == null)
            {
                _groupIdByUnitInstanceV172LikeOriginal.Remove(unit.GetInstanceID());
                return false;
            }
            // AI/neutral settlement formations are valid formation members even
            // though they cannot receive player orders. Command dispatch still
            // checks CanReceiveOrders; membership lookup must not destroy them.
            if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal)
            {
                RemoveUnitMembershipV172LikeOriginal(unit);
                return false;
            }
            return true;
        }

        private static int RegisterFormationInternalV172LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            IList<Vector2> destSlots,
            string shape,
            string soldierMemberId,
            int requestedGroupId,
            int commandSlotCount = -1)
        {
            int groupId = requestedGroupId > 0 ? requestedGroupId : _nextGroupIdV172LikeOriginal++;
            RuntimeFormationV172LikeOriginal old;
            bool formationBirthV396LikeOriginal = !_groupsByIdV172LikeOriginal.ContainsKey(groupId);
            byte preservedDirection = 0;
            int preservedSpacingPercent = 100;
            bool[] preservedShotLines = new bool[3];
            int preservedGrenades = 0;
            float preservedGrenadeLastUpdateAt = 0.0f;
            int preservedNKillsV402LikeOriginal = 0;
            int preservedExpGrowSpeedV402LikeOriginal = 100;
            if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out old) && old != null)
            {
                preservedDirection = old.Direction;
                preservedSpacingPercent = old.SpacingPercent;
                for (int i = 0; i < preservedShotLines.Length; i++)
                    preservedShotLines[i] = old.ShotLinesEnabled[i];
                preservedGrenades = old.Grenades;
                preservedGrenadeLastUpdateAt = old.GrenadeLastUpdateAt;
                preservedNKillsV402LikeOriginal = old.NKills;
                preservedExpGrowSpeedV402LikeOriginal = old.ExpGrowSpeed;
                for (int i = 0; i < old.Units.Count; i++) RemoveUnitMembershipV172LikeOriginal(old.Units[i]);
            }
            else if (units != null && units.Count > 0 && units[0] != null)
            {
                preservedDirection = units[0].RealDir;
            }

            RuntimeFormationV172LikeOriginal group = new RuntimeFormationV172LikeOriginal();
            group.GroupId = groupId;
            group.Shape = shape ?? string.Empty;
            group.SoldierMemberId = soldierMemberId ?? string.Empty;
            group.Direction = preservedDirection;
            group.SpacingPercent = preservedSpacingPercent;
            group.CommandSlotCount = commandSlotCount >= 0
                ? commandSlotCount
                : ResolveCommandPrefixCountV320LikeOriginal(group, units);
            group.Grenades = preservedGrenades;
            group.GrenadeLastUpdateAt = preservedGrenadeLastUpdateAt > 0.0f
                ? preservedGrenadeLastUpdateAt
                : Time.realtimeSinceStartup;
            group.NKills = preservedNKillsV402LikeOriginal;
            if (old != null)
            {
                group.StartMorale = old.StartMorale;
                group.AddMaxMorale = old.AddMaxMorale;
                group.MoraleRecoveryBonus = old.MoraleRecoveryBonus;
            }
            group.ExpGrowSpeed = preservedExpGrowSpeedV402LikeOriginal;
            for (int i = 0; i < preservedShotLines.Length; i++)
                group.ShotLinesEnabled[i] = preservedShotLines[i];
            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                    // Keep unit and destination indices aligned after casualties.
                    if (!IsUsableFormationUnitV172LikeOriginal(u, true))
                    {
                        group.Units.Add(null);
                        continue;
                    }
                    RemoveUnitMembershipV172LikeOriginal(u);
                    group.Units.Add(u);
                    group.Nation = u.Nation;
                    _groupIdByUnitInstanceV172LikeOriginal[u.GetInstanceID()] = groupId;
                    if (formationBirthV396LikeOriginal)
                        C2CombatRuntimeV334LikeOriginal.ResetRifleAttackOnFormationCreateV396LikeOriginal(u);
                }
            }
            if (destSlots != null)
            {
                for (int i = 0; i < destSlots.Count; i++)
                    group.Slots.Add(destSlots[i]);
            }
            _groupsByIdV172LikeOriginal[groupId] = group;
            InitializeFormationMoraleLikeOriginal(group, formationBirthV396LikeOriginal);
            return groupId;
        }

        private static void RemoveUnitMembershipV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            _groupIdByUnitInstanceV172LikeOriginal.Remove(unit.GetInstanceID());
        }

        private static bool IsUsableFormationUnitV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, bool allowAlreadyInFormation)
        {
            if (unit == null || !unit.isActiveAndEnabled) return false;
            if (unit.NotSelectable || unit.IsDeadLikeOriginal) return false;
            if (!unit.CanReceiveOrdersLikeOriginal()) return false;
            if (!allowAlreadyInFormation && IsUnitInRuntimeFormationV168LikeOriginal(unit)) return false;
            return true;
        }

        private static bool MatchesFormationMemberV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, string memberId)
        {
            if (unit == null || string.IsNullOrWhiteSpace(memberId)) return false;
            string resolved = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(unit);
            if (string.Equals(resolved, memberId, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(unit.SourceMonsterId ?? string.Empty, memberId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal SelectAmountOptionV172LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
            int amountIndex,
            int requiredAmount)
        {
            if (record == null || record.Options == null) return null;
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal fallback = null;
            for (int i = 0; i < record.Options.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal o = record.Options[i];
                if (o == null) continue;
                if (fallback == null) fallback = o;
                if (o.AmountIndex == amountIndex) return o;
            }
            if (requiredAmount > 0)
            {
                for (int i = 0; i < record.Options.Count; i++)
                {
                    C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal o = record.Options[i];
                    if (o != null && o.UnitCount == requiredAmount) return o;
                }
            }
            return fallback;
        }

        private static void ReorderSoldiersForNearestSlotsV172LikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            List<Vector2> slots,
            int commandUnitCount)
        {
            if (units == null || slots == null) return;
            int firstSoldier = Mathf.Clamp(commandUnitCount, 0, units.Count);
            if (firstSoldier >= units.Count || firstSoldier >= slots.Count) return;

            int count = Mathf.Min(units.Count, slots.Count) - firstSoldier;
            if (count <= 1) return;

            // Exact port of the original ResortMembByPos -> FindShotWayPoint -> ShotWay
            // assignment policy. It deliberately resolves the most constrained/farthest
            // nearest pair first instead of greedily filling destination slots in order.
            var sourceUnits = new C2NeutralPeasantUnitInfoV2LikeOriginal[count];
            var distance = new float[count, count];
            for (int source = 0; source < count; source++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[firstSoldier + source];
                sourceUnits[source] = unit;
                float ux = unit != null && unit.RealXFloat != 0.0f
                    ? unit.RealXFloat
                    : (unit != null ? unit.RealX : 0.0f);
                float uy = unit != null && unit.RealYFloat != 0.0f
                    ? unit.RealYFloat
                    : (unit != null ? unit.RealY : 0.0f);
                for (int destination = 0; destination < count; destination++)
                {
                    Vector2 slot = slots[firstSoldier + destination];
                    float dx = Mathf.Abs(ux - slot.x);
                    float dy = Mathf.Abs(uy - slot.y);
                    distance[source, destination] =
                        (Mathf.Max(dx, dy) + dx + dy) * 0.5f; // original Norma()
                }
            }

            var sourceUsed = new bool[count];
            var destinationUsed = new bool[count];
            var destinationOfSource = new int[count];
            for (int pass = 0; pass < count; pass++)
            {
                int chosenSource = 0;
                int chosenDestination = 0;
                float chosenDistance = -1.0f;

                for (int destination = 0; destination < count; destination++)
                {
                    if (destinationUsed[destination]) continue;
                    float nearest = float.MaxValue;
                    int nearestSource = 0;
                    for (int source = 0; source < count; source++)
                    {
                        if (sourceUsed[source]) continue;
                        if (distance[source, destination] <= nearest)
                        {
                            nearest = distance[source, destination];
                            nearestSource = source;
                        }
                    }
                    if (nearest >= chosenDistance)
                    {
                        chosenDistance = nearest;
                        chosenSource = nearestSource;
                        chosenDestination = destination;
                    }
                }

                for (int source = 0; source < count; source++)
                {
                    if (sourceUsed[source]) continue;
                    float nearest = float.MaxValue;
                    int nearestDestination = 0;
                    for (int destination = 0; destination < count; destination++)
                    {
                        if (destinationUsed[destination]) continue;
                        if (distance[source, destination] <= nearest)
                        {
                            nearest = distance[source, destination];
                            nearestDestination = destination;
                        }
                    }
                    if (nearest >= chosenDistance)
                    {
                        chosenDistance = nearest;
                        chosenSource = source;
                        chosenDestination = nearestDestination;
                    }
                }

                sourceUsed[chosenSource] = true;
                destinationUsed[chosenDestination] = true;
                destinationOfSource[chosenSource] = chosenDestination;
            }

            for (int source = 0; source < count; source++)
                units[firstSoldier + destinationOfSource[source]] = sourceUnits[source];
        }


        private static bool AreFormationSlotsCollapsedV172LikeOriginal(List<Vector2> slots, int count)
        {
            if (slots == null || slots.Count < count) return true;
            if (count <= 1) return false;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                Vector2 s = slots[i];
                minX = Mathf.Min(minX, s.x);
                minY = Mathf.Min(minY, s.y);
                maxX = Mathf.Max(maxX, s.x);
                maxY = Mathf.Max(maxY, s.y);
            }

            const float minSpanReal = 16.0f * 8.0f;
            if (maxX - minX < minSpanReal && maxY - minY < minSpanReal)
                return true;

            for (int i = 0; i < count; i++)
            {
                Vector2 a = slots[i];
                for (int j = i + 1; j < count; j++)
                {
                    if ((a - slots[j]).sqrMagnitude < 1.0f)
                        return true;
                }
            }
            return false;
        }

        private static List<Vector2> BuildFallbackSlotsV172LikeOriginal(int count, float centerRealX, float centerRealY)
        {
            List<Vector2> slots = new List<Vector2>(Mathf.Max(1, count));
            int cols = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, count)));
            int rows = Mathf.CeilToInt(count / (float)cols);
            // Original generic PositionOrder::CreatePositions fallback uses maxR=768 real units.
            float step = 768.0f;
            for (int i = 0; i < count; i++)
            {
                int x = i % cols;
                int y = i / cols;
                slots.Add(new Vector2(
                    centerRealX + (x - (cols - 1) * 0.5f) * step,
                    centerRealY + (y - (rows - 1) * 0.5f) * step));
            }
            return slots;
        }

        private static void AddUniqueUnitV172LikeOriginal(List<C2NeutralPeasantUnitInfoV2LikeOriginal> list, C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (list == null || unit == null) return;
            if (!list.Contains(unit)) list.Add(unit);
        }

        private static string UnitLabelV172LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit != null ? (unit.SourceMonsterId ?? unit.ResolvedMd ?? string.Empty) : string.Empty;
        }
    }

    // V247: real C2BuildingProductionCardsRuntimeV114 MonoBehaviour is restored in C2BuildingProductionCardsRuntimeV114.cs.

    public static class C2UnitPortStage1BuildingSelectableExtensionsV237
    {
        public static void SetSelected(this C2SettlementBuildingSelectableV1LikeOriginal building, bool selected)
        {
            if (building == null) return;
            building.IsSelected = selected;
        }

        public static void SetRallyPointV155LikeOriginal(
            this C2SettlementBuildingSelectableV1LikeOriginal building,
            int realX,
            int realY,
            string source)
        {
            if (building == null) return;
            // Stage 1 bridge: current building renderer owns real rally implementation later.
        }
    }
}
