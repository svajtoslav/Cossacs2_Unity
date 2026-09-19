using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V385A/V385B: stateful brigade-owned road order + original-style terminal handoff.
    //
    // Source correspondence:
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::Init
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::Process/ProcessPre
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SetNextPosAndDest
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::GetUnitCoordInColumn
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SetUnitsSpeed
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SortBrigUnits
    //
    // The current Unity bridge already owns a road waypoint chain produced from RNE2.
    // V385A makes that chain a single stateful brigade order instead of converting it
    // into N unrelated unit paths.  Topology streaming/reservation (LockNextPoint) and
    // foreign-unit displacement (GoAway/AskGoAway) remain explicit MISSING dependencies;
    // they are not replaced with heuristics here.
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        private sealed class C2BrigadeOrderGoOnRoadStateV385A
        {
            public int GroupId;
            public string OriginalShape = string.Empty;
            public string Source = string.Empty;
            public byte OrdType;
            public byte FinalDirection;
            public int CommandPrefix;

            public Vector2[] PointsReal;
            public byte[] PointsDir;
            public int[] ShiftInColumn;
            public int HeadIndex;
            public int MaxPIndex;
            public int UnitsInLine;
            public int DistInColumn;
            public int PassNPoints;
            public int NLines;
            // routePoints[0] is the off-road start and the last point is the final
            // battlefield destination.  The interior interval is the actual road path.
            public int FirstRoadPointIndex;
            public int LastRoadPointIndex;
            public bool FirstStep;
            public bool OffPlaceHead;
            public C2NeutralPeasantUnitInfoV2LikeOriginal HeadUnit;

            public readonly List<Vector2> FinalSlots = new List<Vector2>();
            public float FinalCenterX;
            public float FinalCenterY;
            public float NextRecoveryAt;
            public float NextAuditAt;
            public int LastAuditHeadIndex = -1;
        }

        private static readonly Dictionary<int, C2BrigadeOrderGoOnRoadStateV385A>
            _roadOrdersV385A = new Dictionary<int, C2BrigadeOrderGoOnRoadStateV385A>();

        private const int C2RoadSpeedV385A = 96;       // BrigadeOrders.cpp: (64+32)
        private const int C2RoadMaxSpeedV385A = 240;  // BrigadeOrders.cpp: MaxSpdLimit
        private const int C2RoadMinSpeedV385A = 32;

        private static int StartBrigadeGoOnRoadV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> groupUnits,
            List<Vector2> finalSlots,
            Vector2[] roadCenterPath,
            float finalCenterX,
            float finalCenterY,
            byte finalDirection,
            byte ordType,
            string source,
            out string audit)
        {
            audit = "invalid";
            if (group == null || groupUnits == null || groupUnits.Count == 0 ||
                finalSlots == null || finalSlots.Count == 0 ||
                roadCenterPath == null || roadCenterPath.Length < 2)
                return 0;

            CancelBrigadeGoOnRoadV385ALikeOriginal(group.GroupId, "replace_road_order", false);

            Vector2[] routePoints = NormalizeRoadPointsV385ALikeOriginal(roadCenterPath);
            if (routePoints == null || routePoints.Length < 2)
            {
                audit = "road_points_collapsed";
                return 0;
            }

            int commandPrefix = Mathf.Clamp(
                ResolveOrderCommandCountV360LikeOriginal(group, groupUnits), 0, groupUnits.Count);

            C2NeutralPeasantUnitInfoV2LikeOriginal geometryReference = null;
            for (int i = commandPrefix; i < groupUnits.Count && geometryReference == null; i++)
                if (IsUsableFormationUnitV172LikeOriginal(groupUnits[i], true)) geometryReference = groupUnits[i];
            for (int i = 0; i < commandPrefix && geometryReference == null; i++)
                if (IsUsableFormationUnitV172LikeOriginal(groupUnits[i], true)) geometryReference = groupUnits[i];

            // mdParser.cpp::MDGEOMETRY::Parse stores Radius2 as Radius2<<4 in NewMonster.
            // Do not invent a fallback radius when the bridge has lost MD geometry: the
            // original order requires NewMonster::Radius2, so reject the road-order and
            // let the caller use its already-existing non-road command path.
            if (geometryReference == null)
            {
                audit = "missing_geometry_reference";
                return 0;
            }
            C2UnitOriginalRuntimeLinkLikeOriginal geometryLink = geometryReference.RuntimeLinkCachedLikeOriginal;
            if (geometryLink == null || geometryLink.Runtime == null || geometryLink.Runtime.Md == null ||
                geometryLink.Runtime.Md.GeometryRadius2 <= 0)
            {
                audit = "missing_md_geometry_radius2";
                return 0;
            }
            int radius2LikeOriginal = geometryLink.Runtime.Md.GeometryRadius2 << 4;

            int distInColumn = radius2LikeOriginal / 14 + 4;
            if (distInColumn < 1)
            {
                audit = "invalid_dist_in_column";
                return 0;
            }
            int unitsInLine = 100 / distInColumn;
            // Original BrigadeOrder_GoOnRoad owns ShiftInColumn[16].  Values outside
            // that original storage contract are a bridge/data error, not a reason to clamp.
            if (unitsInLine < 1 || unitsInLine > 16)
            {
                audit = "units_in_line_out_of_original_range value=" +
                        unitsInLine.ToString(CultureInfo.InvariantCulture);
                return 0;
            }
            int passNPoints = 1 + ((radius2LikeOriginal / 10 + 2) / 19) * 3;
            if (passNPoints < 1) passNPoints = 1;

            int[] shifts = new int[unitsInLine];
            int high = (distInColumn * (unitsInLine - 1)) / 2;
            for (int i = 0; i < unitsInLine; i++)
                shifts[i] = i * distInColumn - high;

            int soldierCount = Mathf.Max(0, groupUnits.Count - commandPrefix);
            int nLines = soldierCount / unitsInLine + 1;
            if ((soldierCount % unitsInLine) != 0) nLines++;

            // ProcessPre fills the initial PointsXY buffer behind the head when the
            // brigade is longer than the first road segment (pdd=25 original pixels).
            // Our RNE2 route is already complete, so preserve that initial history
            // explicitly and start HeadIndex at the real route origin.
            int historyPoints = Mathf.Max(1, nLines * passNPoints);
            Vector2[] points = PrependRoadHistoryV385ALikeOriginal(routePoints, historyPoints);
            int initialHeadIndex = Mathf.Clamp(historyPoints, 0, points.Length - 1);

            C2BrigadeOrderGoOnRoadStateV385A state = new C2BrigadeOrderGoOnRoadStateV385A();
            state.GroupId = group.GroupId;
            state.OriginalShape = group.Shape ?? string.Empty;
            state.Source = source ?? "BrigadeOrder_GoOnRoad_V385A";
            state.OrdType = ordType;
            state.FinalDirection = finalDirection;
            state.CommandPrefix = commandPrefix;
            state.PointsReal = points;
            state.PointsDir = BuildRoadDirectionsV385ALikeOriginal(points, finalDirection);
            state.ShiftInColumn = shifts;
            state.HeadIndex = initialHeadIndex;
            state.MaxPIndex = points.Length;
            state.UnitsInLine = unitsInLine;
            state.DistInColumn = distInColumn;
            state.PassNPoints = passNPoints;
            state.NLines = nLines;
            state.FirstRoadPointIndex = Mathf.Clamp(historyPoints + 1, 0, points.Length - 1);
            state.LastRoadPointIndex = Mathf.Clamp(historyPoints + routePoints.Length - 2, 0, points.Length - 1);
            state.FirstStep = true;
            state.OffPlaceHead = true;
            state.FinalCenterX = finalCenterX;
            state.FinalCenterY = finalCenterY;
            for (int i = 0; i < finalSlots.Count; i++) state.FinalSlots.Add(finalSlots[i]);

            // Keep the existing RuntimeFormation object.  The road order temporarily owns
            // member destinations but does not change the brigade's battlefield WarType/shape.
            group.UsesRoadMovement = true;
            group.TurnActive = false;
            group.CommandSlotCount = commandPrefix;

            int live = 0;
            for (int i = 0; i < groupUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = groupUnits[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                live++;
                C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(unit);
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(unit, state.Source);
                C2RoadUnitSpeedControllerV352.DetachLikeOriginal(unit);
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    unit,
                    C2UnitOrderKindV325LikeOriginal.FormationMove,
                    state.Source,
                    "BrigadeOrder_GoOnRoad_V385A");
            }
            if (live == 0)
            {
                group.UsesRoadMovement = false;
                audit = "no_live_members";
                return 0;
            }

            _roadOrdersV385A[group.GroupId] = state;
            SetNextPosAndDestV385ALikeOriginal(group, state, true);

            audit = "ok group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                    " members=" + live.ToString(CultureInfo.InvariantCulture) +
                    " points=" + state.MaxPIndex.ToString(CultureInfo.InvariantCulture) +
                    " unitsInLine=" + state.UnitsInLine.ToString(CultureInfo.InvariantCulture) +
                    " distInColumn=" + state.DistInColumn.ToString(CultureInfo.InvariantCulture) +
                    " passNPoints=" + state.PassNPoints.ToString(CultureInfo.InvariantCulture) +
                    " radius2NewMonster=" + radius2LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                    " shape='" + state.OriginalShape + "'";
            Debug.Log("[C2:ROAD ORDER V385A START] " + audit +
                      " topologyStreaming=MISSING roadReservation=MISSING goAway=MISSING");
            return live;
        }

        private static Vector2[] NormalizeRoadPointsV385ALikeOriginal(Vector2[] source)
        {
            if (source == null || source.Length == 0) return null;
            List<Vector2> result = new List<Vector2>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                Vector2 p = source[i];
                if (result.Count > 0 && Vector2.SqrMagnitude(result[result.Count - 1] - p) < 16.0f)
                    continue;
                result.Add(p);
            }
            return result.ToArray();
        }

        private static Vector2[] PrependRoadHistoryV385ALikeOriginal(
            Vector2[] routePoints,
            int historyPoints)
        {
            if (routePoints == null || routePoints.Length < 2 || historyPoints <= 0)
                return routePoints;

            Vector2 first = routePoints[0];
            Vector2 second = routePoints[1];
            Vector2 forward = second - first;
            float magnitude = forward.magnitude;
            if (magnitude < 0.001f) forward = Vector2.right;
            else forward /= magnitude;

            // BrigadeOrders.cpp ProcessPre uses pdd=25 original pixels.
            const float historyStepReal = 25.0f * 16.0f;
            Vector2[] result = new Vector2[routePoints.Length + historyPoints];
            for (int i = 0; i < historyPoints; i++)
            {
                int distanceSteps = historyPoints - i;
                result[i] = first - forward * (historyStepReal * distanceSteps);
            }
            Array.Copy(routePoints, 0, result, historyPoints, routePoints.Length);
            return result;
        }

        private static byte[] BuildRoadDirectionsV385ALikeOriginal(Vector2[] points, byte fallback)
        {
            byte[] result = new byte[points != null ? points.Length : 0];
            if (points == null || points.Length == 0) return result;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                result[i] = C2OriginalMovementMathV352.GetDir(
                    Mathf.RoundToInt(b.x - a.x), Mathf.RoundToInt(b.y - a.y));
            }
            result[result.Length - 1] = result.Length > 1 ? result[result.Length - 2] : fallback;
            return result;
        }

        private static void TickBrigadeGoOnRoadV385ALikeOriginal()
        {
            if (_roadOrdersV385A.Count == 0) return;

            List<int> groupIds = new List<int>(_roadOrdersV385A.Keys);
            for (int oi = 0; oi < groupIds.Count; oi++)
            {
                int groupId = groupIds[oi];
                C2BrigadeOrderGoOnRoadStateV385A state;
                if (!_roadOrdersV385A.TryGetValue(groupId, out state) || state == null) continue;

                RuntimeFormationV172LikeOriginal group;
                if (!_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) || group == null)
                {
                    CancelBrigadeGoOnRoadV385ALikeOriginal(groupId, "formation_deleted", false);
                    continue;
                }

                if (state.PointsReal == null || state.MaxPIndex <= 0)
                {
                    CancelBrigadeGoOnRoadV385ALikeOriginal(groupId, "road_points_lost", true);
                    continue;
                }

                // V385B finish handoff.
                //
                // Original BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::ProcessPre ends the
                // road order when the head has already entered DestTopZone and fewer than
                // two buffered road points remain:
                //
                //   if(HeadTopZoneIndex==DestTopZone&&((MaxPIndex-HeadIndex)<2))
                //       return false;
                //
                // The current bridge does not yet stream topology zones; instead
                // C2FormationTryBuildRoadPathRealV320LikeOriginal builds the complete road
                // point chain up front and appends the exact requested destination as its
                // last point. Therefore HeadIndex == MaxPIndex-1 is the current-runtime
                // representation of that original terminal condition.
                //
                // V385A previously waited for HeadUnit to come within the normal head
                // threshold of PointsReal[HeadIndex]. With OffPlaceHead=true the first
                // soldier row is intentionally assigned HeadIndex-1, so after advancing
                // HeadIndex onto the final point the head can stop one point behind forever.
                // Finish from the road-buffer state itself, then let the existing
                // CreateOrderedPositions/KeepPositions analogue assemble FinalSlots.
                if (state.HeadIndex >= state.MaxPIndex - 1)
                {
                    Debug.Log("[C2:ROAD ORDER V385B TAIL] group=" +
                              groupId.ToString(CultureInfo.InvariantCulture) +
                              " head=" + state.HeadIndex.ToString(CultureInfo.InvariantCulture) + "/" +
                              (state.MaxPIndex - 1).ToString(CultureInfo.InvariantCulture) +
                              " remaining=" +
                              (state.MaxPIndex - state.HeadIndex).ToString(CultureInfo.InvariantCulture) +
                              " action=restore_battlefield_shape");
                    FinishBrigadeGoOnRoadV385ALikeOriginal(
                        group, state, "route_tail_reached_like_ProcessPre");
                    continue;
                }

                C2NeutralPeasantUnitInfoV2LikeOriginal head = ResolveHeadUnitV385ALikeOriginal(group, state);
                if (head == null)
                {
                    FinishBrigadeGoOnRoadV385ALikeOriginal(group, state, "no_head_member");
                    continue;
                }

                Vector2 headPoint = state.PointsReal[Mathf.Clamp(state.HeadIndex, 0, state.MaxPIndex - 1)];
                float hx = head.RealXFloat != 0.0f ? head.RealXFloat : head.RealX;
                float hy = head.RealYFloat != 0.0f ? head.RealYFloat : head.RealY;
                int headDistancePx = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt((headPoint.x - hx) / 16.0f),
                    Mathf.RoundToInt((headPoint.y - hy) / 16.0f));

                int threshold = state.HeadIndex < 30 ? 150 : 100;
                if (headDistancePx < threshold)
                {
                    if (state.HeadIndex >= state.MaxPIndex - 1)
                    {
                        FinishBrigadeGoOnRoadV385ALikeOriginal(group, state, "head_reached_last_point");
                        continue;
                    }

                    state.HeadIndex++;
                    SetNextPosAndDestV385ALikeOriginal(group, state, false);
                }
                else
                {
                    // Original ProcessPre reissues destinations after a stalled head.
                    // Keep the same state and targets; do not eject lagging members.
                    float now = Time.realtimeSinceStartup;
                    if (now >= state.NextRecoveryAt)
                    {
                        state.NextRecoveryAt = now + 0.75f;
                        SetNextPosAndDestV385ALikeOriginal(group, state, false);
                    }
                    else
                    {
                        SetUnitsSpeedV385ALikeOriginal(group, state);
                    }
                }

                float auditNow = Time.realtimeSinceStartup;
                if (state.LastAuditHeadIndex != state.HeadIndex &&
                    (state.HeadIndex == 1 || state.HeadIndex == state.MaxPIndex - 1 || auditNow >= state.NextAuditAt))
                {
                    state.LastAuditHeadIndex = state.HeadIndex;
                    state.NextAuditAt = auditNow + 2.0f;
                    Debug.Log("[C2:ROAD ORDER V385A STEP] group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                              " head=" + state.HeadIndex.ToString(CultureInfo.InvariantCulture) + "/" +
                              (state.MaxPIndex - 1).ToString(CultureInfo.InvariantCulture) +
                              " headDistPx=" + headDistancePx.ToString(CultureInfo.InvariantCulture) +
                              " active=" + CountLiveRoadMembersV385ALikeOriginal(group).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void SetNextPosAndDestV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state,
            bool firstInstall)
        {
            if (group == null || state == null || state.PointsReal == null || state.PointsReal.Length == 0)
                return;

            if (state.FirstStep)
            {
                string orderMode = SortBrigUnitsV385ALikeOriginal(group, state);
                state.FirstStep = false;
                Debug.Log("[C2:ROAD ORDER V385A FIRSTSTEP] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                          " mode=" + orderMode +
                          " unitsInLine=" + state.UnitsInLine.ToString(CultureInfo.InvariantCulture) +
                          " distInColumn=" + state.DistInColumn.ToString(CultureInfo.InvariantCulture) +
                          " passNPoints=" + state.PassNPoints.ToString(CultureInfo.InvariantCulture) +
                          " commandPrefix=" + state.CommandPrefix.ToString(CultureInfo.InvariantCulture) +
                          " offPlaceHead=" + (state.OffPlaceHead ? "1" : "0"));
            }

            state.HeadUnit = null;
            int bestHeadDistance = int.MaxValue;
            int headColumn = state.UnitsInLine / 2;

            int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;

                int pointIndex;
                int column;
                bool waitingBeforeColumn = false;
                if (i < commandPrefix)
                {
                    int officerPoint = state.OffPlaceHead ? state.HeadIndex : Mathf.Max(state.HeadIndex - 4, 0);
                    pointIndex = Mathf.Clamp(officerPoint, 0, state.MaxPIndex - 1);
                    if (commandPrefix <= 1) column = headColumn;
                    else if (i == 0) column = 0;
                    else if (i == 1) column = headColumn;
                    else if (i == 2) column = state.UnitsInLine - 1;
                    else column = headColumn;
                }
                else
                {
                    int soldierIndex = i - commandPrefix;
                    int line = soldierIndex / state.UnitsInLine;
                    column = soldierIndex % state.UnitsInLine;
                    pointIndex = state.HeadIndex - line * state.PassNPoints;
                    if (state.OffPlaceHead) pointIndex--;
                    if (pointIndex < 0)
                    {
                        waitingBeforeColumn = true;
                        pointIndex = 0;
                    }
                    if (pointIndex >= state.MaxPIndex) pointIndex = state.MaxPIndex - 1;
                }

                Vector2 target = GetUnitCoordInColumnV385ALikeOriginal(state, pointIndex, column);
                InstallRoadDestinationV385ALikeOriginal(unit, target, state, waitingBeforeColumn);

                if (i >= commandPrefix && column == headColumn)
                {
                    float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
                    float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
                    Vector2 hp = state.PointsReal[state.HeadIndex];
                    int d = C2OriginalMovementMathV352.Norma(
                        Mathf.RoundToInt((ux - hp.x) / 16.0f),
                        Mathf.RoundToInt((uy - hp.y) / 16.0f));
                    if (d < bestHeadDistance)
                    {
                        bestHeadDistance = d;
                        state.HeadUnit = unit;
                    }
                }
            }

            if (state.HeadUnit == null)
                state.HeadUnit = ResolveNearestLiveMemberToHeadV385ALikeOriginal(group, state);

            SetUnitsSpeedV385ALikeOriginal(group, state);
        }

        private static void InstallRoadDestinationV385ALikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2 target,
            C2BrigadeOrderGoOnRoadStateV385A state,
            bool waitingBeforeColumn)
        {
            if (unit == null) return;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link == null || link.Runtime == null)
            {
                unit.SetMoveDestinationRealLikeOriginal(
                    target.x, target.y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false, 0);
                return;
            }

            // Direct DestX/DestY ownership is the closest current-runtime equivalent of
            // BrigadeOrder_GoOnRoad::SetNextPosAndDest.  We intentionally do not create
            // a new LocalOrder/SubmitPath node per member per road point.
            link.SetValidatedDirectMoveDestinationRealLikeOriginal(
                target.x,
                target.y,
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false,
                0,
                false,
                state.Source);
            // NewMon.cpp and Brigade.cpp inspect BRIGADEORDER_GOONROAD directly.
            // The managed runtime mirror must stay true for the entire road-owned phase.
            link.Runtime.OriginalGoOnRoadLikeOriginal = true;
            if (waitingBeforeColumn)
                link.Runtime.OriginalUnitSpeedLikeOriginal = C2RoadMinSpeedV385A;
        }

        // NewMon.cpp::ApplyTiring asks GetTiringBonus only while the brigade owns
        // BRIGADEORDER_GOONROAD.  In the supplied retail Roads.dat every normal road
        // descriptor keeps the source default Tiring=-64 (there are no $PHYS overrides).
        // Do not apply it during the off-road approach/final handoff.
        internal static int GetRoadTiringMultiplierV403ELikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 256;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null ||
                !group.UsesRoadMovement) return 256;
            C2BrigadeOrderGoOnRoadStateV385A state;
            if (!_roadOrdersV385A.TryGetValue(group.GroupId, out state) || state == null) return 256;

            int unitIndex = group.Units.IndexOf(unit);
            if (unitIndex < 0) return 256;
            int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
            int pointIndex;
            if (unitIndex < commandPrefix)
            {
                pointIndex = state.OffPlaceHead ? state.HeadIndex : Mathf.Max(state.HeadIndex - 4, 0);
            }
            else
            {
                int soldierIndex = unitIndex - commandPrefix;
                int line = soldierIndex / Mathf.Max(1, state.UnitsInLine);
                pointIndex = state.HeadIndex - line * state.PassNPoints;
                if (state.OffPlaceHead) pointIndex--;
            }
            pointIndex = Mathf.Clamp(pointIndex, 0, Mathf.Max(0, state.MaxPIndex - 1));
            return pointIndex >= state.FirstRoadPointIndex && pointIndex <= state.LastRoadPointIndex
                ? -64
                : 256;
        }

        private static Vector2 GetUnitCoordInColumnV385ALikeOriginal(
            C2BrigadeOrderGoOnRoadStateV385A state,
            int pointIndex,
            int columnIndex)
        {
            pointIndex = Mathf.Clamp(pointIndex, 0, state.MaxPIndex - 1);
            columnIndex = Mathf.Clamp(columnIndex, 0, state.ShiftInColumn.Length - 1);
            Vector2 basePoint = state.PointsReal[pointIndex];
            int shiftPixels = state.ShiftInColumn[columnIndex];
            int direction = state.PointsDir[pointIndex];
            int sin = C2OriginalMovementMathV352.TSin[direction];
            int cos = C2OriginalMovementMathV352.TCos[direction];
            float shiftReal = shiftPixels * 16.0f;
            return new Vector2(
                basePoint.x - shiftReal * sin / 256.0f,
                basePoint.y + shiftReal * cos / 256.0f);
        }

        private static string SortBrigUnitsV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            int firstSoldier = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
            int soldierCount = group.Units.Count - firstSoldier;
            if (soldierCount <= 1) return "keep";

            // Original OffPlaseHead checks the first command member only.
            state.OffPlaceHead = true;
            if (firstSoldier > 0 && IsUsableFormationUnitV172LikeOriginal(group.Units[0], true))
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal officer = group.Units[0];
                float ox = officer.RealXFloat != 0.0f ? officer.RealXFloat : officer.RealX;
                float oy = officer.RealYFloat != 0.0f ? officer.RealYFloat : officer.RealY;
                Vector2 headPoint = state.PointsReal[state.HeadIndex];
                int od = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt((ox - headPoint.x) / 16.0f),
                    Mathf.RoundToInt((oy - headPoint.y) / 16.0f));
                if (od > 300) state.OffPlaceHead = false;
            }

            int row = Mathf.Min(state.UnitsInLine, soldierCount);
            bool inHead = false;
            bool inBack = false;
            Vector2 firstPoint = state.PointsReal[state.HeadIndex];
            for (int i = 0; i < row; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[firstSoldier + i];
                if (DistanceUnitToPointPxV385ALikeOriginal(u, firstPoint) < 300) inHead = true;
            }
            for (int i = 0; i < row; i++)
            {
                int idx = group.Units.Count - 1 - i;
                if (idx < firstSoldier) break;
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[idx];
                if (DistanceUnitToPointPxV385ALikeOriginal(u, firstPoint) < 300) inBack = true;
            }

            if (!inHead && inBack)
            {
                group.Units.Reverse(firstSoldier, soldierCount);
                return "reverse";
            }

            if (!inHead && !inBack)
            {
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers =
                    group.Units.GetRange(firstSoldier, soldierCount);
                soldiers.Sort(delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a,
                                       C2NeutralPeasantUnitInfoV2LikeOriginal b)
                {
                    int da = DistanceUnitToPointPxV385ALikeOriginal(a, firstPoint);
                    int db = DistanceUnitToPointPxV385ALikeOriginal(b, firstPoint);
                    return da.CompareTo(db);
                });
                for (int i = 0; i < soldiers.Count; i++)
                    group.Units[firstSoldier + i] = soldiers[i];
                return "sort_distance";
            }

            return "keep";
        }

        private static int DistanceUnitToPointPxV385ALikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2 point)
        {
            if (unit == null) return int.MaxValue / 4;
            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            return C2OriginalMovementMathV352.Norma(
                Mathf.RoundToInt((ux - point.x) / 16.0f),
                Mathf.RoundToInt((uy - point.y) / 16.0f));
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal ResolveHeadUnitV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (state.HeadUnit != null && IsUsableFormationUnitV172LikeOriginal(state.HeadUnit, true))
                return state.HeadUnit;
            state.HeadUnit = ResolveNearestLiveMemberToHeadV385ALikeOriginal(group, state);
            return state.HeadUnit;
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal ResolveNearestLiveMemberToHeadV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (group == null || state == null || state.PointsReal == null || state.PointsReal.Length == 0)
                return null;
            Vector2 head = state.PointsReal[Mathf.Clamp(state.HeadIndex, 0, state.PointsReal.Length - 1)];
            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            int bestDistance = int.MaxValue;
            for (int i = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count); i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(u, true)) continue;
                int d = DistanceUnitToPointPxV385ALikeOriginal(u, head);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = u;
                }
            }
            if (best != null) return best;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (IsUsableFormationUnitV172LikeOriginal(u, true)) return u;
            }
            return null;
        }

        private static void SetUnitsSpeedV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (group == null || state == null) return;
            long averageSum = 0;
            int averageCount = 0;
            int count = group.Units.Count;

            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
                if (rt == null || !rt.HasMoveTargetLikeOriginal) continue;
                int ds = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt((rt.RuntimeRealXLikeOriginal - rt.MoveTargetRealXLikeOriginal) / 16.0f),
                    Mathf.RoundToInt((rt.RuntimeRealYLikeOriginal - rt.MoveTargetRealYLikeOriginal) / 16.0f));
                if (ds < 200)
                {
                    averageSum += ds;
                    averageCount++;
                }
            }
            if (averageCount <= 0) return;

            int average = (int)(averageSum / averageCount);
            if (average < 1) average = 1;
            int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, count);
            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
                if (rt == null || !rt.HasMoveTargetLikeOriginal) continue;
                int ds = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt((rt.RuntimeRealXLikeOriginal - rt.MoveTargetRealXLikeOriginal) / 16.0f),
                    Mathf.RoundToInt((rt.RuntimeRealYLikeOriginal - rt.MoveTargetRealYLikeOriginal) / 16.0f));
                if (i >= commandPrefix)
                {
                    int soldierIndex = i - commandPrefix;
                    int line = soldierIndex / state.UnitsInLine;
                    int pointIndex = state.HeadIndex - line * state.PassNPoints - (state.OffPlaceHead ? 1 : 0);
                    if (pointIndex < 0)
                    {
                        rt.OriginalUnitSpeedLikeOriginal = C2RoadMinSpeedV385A;
                        continue;
                    }
                }

                int speed = (C2RoadSpeedV385A * (ds + 1)) / (average + 1);
                speed = Mathf.Clamp(speed, C2RoadMinSpeedV385A, C2RoadMaxSpeedV385A);
                if (i < commandPrefix && state.HeadIndex < 15 && speed < 72) speed = 100;
                if (i >= commandPrefix)
                {
                    int soldierIndex = i - commandPrefix;
                    int line = soldierIndex / state.UnitsInLine;
                    int pointIndex = state.HeadIndex - line * state.PassNPoints - (state.OffPlaceHead ? 1 : 0);
                    if (state.HeadIndex < 30 && pointIndex < 6 && speed < 72) speed = 100;
                }
                rt.OriginalUnitSpeedLikeOriginal = speed;
            }
        }

        private static void FinishBrigadeGoOnRoadV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state,
            string reason)
        {
            if (group == null || state == null) return;

            int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
            List<Vector2> slots = new List<Vector2>(state.FinalSlots);
            if (slots.Count < group.Units.Count)
            {
                // This should only occur if the formation roster changed while marching.
                // Do not invent a road geometry fallback; rebuild the existing battlefield
                // shape at the already requested final center.
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                    ResolveRecordForGroupV320LikeOriginal(group);
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                    FindFormationOptionV320LikeOriginal(record, state.OriginalShape);
                slots = BuildFormationOrderSlotsAtV359LikeOriginal(
                    group, group.Units, option,
                    state.FinalCenterX, state.FinalCenterY, state.FinalDirection);
                OptimizeFormationSlotsForMotionFieldV347LikeOriginal(
                    slots, group.Units, state.FinalCenterX, state.FinalCenterY);
            }

            ReorderSoldiersForNearestSlotsV172LikeOriginal(group.Units, slots, commandPrefix);
            group.Slots.Clear();
            for (int i = 0; i < slots.Count; i++) group.Slots.Add(slots[i]);
            group.Shape = state.OriginalShape;
            group.Direction = state.FinalDirection;
            group.CommandSlotCount = commandPrefix;
            group.UsesRoadMovement = false;

            int issued = 0;
            int count = Mathf.Min(group.Units.Count, slots.Count);
            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                C2RoadUnitSpeedControllerV352.DetachLikeOriginal(unit);
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                if (link != null && link.Runtime != null)
                    link.Runtime.OriginalUnitSpeedLikeOriginal = 64;
                unit.SetFormationAssemblyDestinationRealLikeOriginal(
                    slots[i].x,
                    slots[i].y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    state.FinalDirection);
                issued++;
            }

            _roadOrdersV385A.Remove(group.GroupId);
            Debug.Log("[C2:ROAD ORDER V385A FINISH] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " reason=" + (reason ?? string.Empty) +
                      " members=" + issued.ToString(CultureInfo.InvariantCulture) +
                      " restoredShape='" + (group.Shape ?? string.Empty) + "'" +
                      " direction=" + state.FinalDirection.ToString(CultureInfo.InvariantCulture));
        }

        private static void CancelBrigadeGoOnRoadV385ALikeOriginal(
            int groupId,
            string reason,
            bool stopMembers)
        {
            C2BrigadeOrderGoOnRoadStateV385A state;
            if (!_roadOrdersV385A.TryGetValue(groupId, out state) || state == null) return;
            _roadOrdersV385A.Remove(groupId);

            RuntimeFormationV172LikeOriginal group;
            if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) && group != null)
            {
                group.UsesRoadMovement = false;
                for (int i = 0; i < group.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                    if (unit == null) continue;
                    C2RoadUnitSpeedControllerV352.DetachLikeOriginal(unit);
                    C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                    if (link != null && link.Runtime != null)
                        link.Runtime.OriginalUnitSpeedLikeOriginal = 64;
                    if (stopMembers && IsUsableFormationUnitV172LikeOriginal(unit, true))
                        unit.StopMoveAndFaceDirectionLikeOriginal(group.Direction);
                }
            }

            Debug.Log("[C2:ROAD ORDER V385A CANCEL] group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                      " reason=" + (reason ?? string.Empty));
        }

        private static int CountLiveRoadMembersV385ALikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return 0;
            int n = 0;
            for (int i = 0; i < group.Units.Count; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) n++;
            return n;
        }
    }
}
